using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amplitude;
using Amplitude.Unity.AI;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public abstract class AILayerCommanderController : AILayer, IXmlSerializable, IAIDrawDebugger
{
	public AILayerCommanderController(string layerName)
	{
		this.layerName = layerName;
	}

	public void DrawDebug(bool activate)
	{
		if (!activate)
		{
			return;
		}
		for (int i = 0; i < this.aiCommanders.Count; i++)
		{
			for (int j = 0; j < this.aiCommanders[i].Missions.Count; j++)
			{
				if (this.aiCommanders[i].Missions[j] != null)
				{
					IAIDrawDebugger iaidrawDebugger = this.aiCommanders[i].Missions[j] as IAIDrawDebugger;
					if (iaidrawDebugger != null)
					{
						iaidrawDebugger.DrawDebug(activate);
					}
				}
			}
		}
	}

	public override void ReadXml(XmlReader reader)
	{
		base.ReadXml(reader);
		if (!reader.IsStartElement("Commanders"))
		{
			return;
		}
		if (reader.IsStartElement("Commanders") && reader.IsEmptyElement())
		{
			reader.Skip();
			return;
		}
		reader.ReadStartElement("Commanders");
		while (reader.IsStartElement())
		{
			Type type = Type.GetType(reader.GetAttribute("AssemblyQualifiedName"));
			if (type == null)
			{
				reader.Skip();
			}
			else
			{
				AICommander aicommander = Activator.CreateInstance(type, true) as AICommander;
				if (aicommander == null)
				{
					reader.Skip();
				}
				else
				{
					aicommander.Empire = base.AIEntity.Empire;
					aicommander.AIPlayer = base.AIEntity.AIPlayer;
					aicommander.Initialize();
					reader.ReadElementSerializable<AICommander>(ref aicommander);
					this.aiCommanders.Add(aicommander);
				}
			}
		}
		reader.ReadEndElement("Commanders");
	}

	public override void WriteXml(XmlWriter writer)
	{
		base.WriteXml(writer);
		writer.WriteStartElement("Commanders");
		for (int i = 0; i < this.aiCommanders.Count; i++)
		{
			IXmlSerializable xmlSerializable = this.aiCommanders[i];
			writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
		}
		writer.WriteEndElement();
	}

	public ReadOnlyCollection<AICommander> AICommanders
	{
		get
		{
			return this.aiCommanders.AsReadOnly();
		}
	}

	public override bool CanEndTurn()
	{
		for (int i = 0; i < this.aiCommanders.Count; i++)
		{
			if (!this.aiCommanders[i].CanEndTurn())
			{
				return false;
			}
		}
		return base.CanEndTurn();
	}

	public int ComputeCommanderMissionNumber(AICommanderMissionDefinition.AICommanderCategory category)
	{
		int num = 0;
		for (int i = 0; i < this.aiCommanders.Count; i++)
		{
			AICommander aicommander = this.aiCommanders[i];
			num += aicommander.ComputeCommanderMissionNumber(category);
		}
		return num;
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		base.AIEntity.RegisterPass(AIEntity.Passes.CheckCommanders.ToString(), this.layerName + "_CheckCommanders", new AIEntity.AIAction(this.CheckCommanders), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.RefreshCommanders.ToString(), this.layerName + "_RefreshCommanders", new AIEntity.AIAction(this.RefreshCommanders), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.ExecuteNeeds.ToString(), this.layerName + "_ExecuteNeeds", new AIEntity.AIAction(this.RefreshCommanders), this, new StaticString[0]);
		base.AIEntity.AIPlayer.AIPlayerStateChange += this.AIPlayer_AIPlayerStateChange;
		this.aiDataRepository = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		this.personalityHelper = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		this.retrofitGlobalPriority = this.personalityHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}/{2}", AILayerCommanderController.RegistryPath, this.layerName, "RetrofitGlobalPriority"), this.retrofitGlobalPriority);
		yield break;
	}

	public override IEnumerator Load()
	{
		for (int index = 0; index < this.aiCommanders.Count; index++)
		{
			this.aiCommanders[index].Load();
		}
		yield break;
	}

	public override void Release()
	{
		base.AIEntity.AIPlayer.AIPlayerStateChange -= this.AIPlayer_AIPlayerStateChange;
		this.ReleaseCommanders();
		this.aiCommanders = null;
		this.aiDataRepository = null;
		this.personalityHelper = null;
		base.Release();
	}

	protected virtual void AddCommander(AICommander commander)
	{
		if (this.IsActive())
		{
			commander.Initialize();
			commander.Load();
			commander.CreateMission();
			this.aiCommanders.Add(commander);
		}
	}

	protected virtual void CheckCommanders(StaticString context, StaticString pass)
	{
		for (int i = this.aiCommanders.Count - 1; i >= 0; i--)
		{
			AICommander aicommander = this.aiCommanders[i];
			bool forceStop = false;
			aicommander.CheckObjectiveInProgress(forceStop);
			if (aicommander.IsMissionFinished(forceStop))
			{
				this.RemoveCommander(aicommander);
			}
		}
	}

	protected override void ExecuteNeeds(StaticString context, StaticString pass)
	{
		base.ExecuteNeeds(context, pass);
	}

	protected virtual void GenerateNewCommander()
	{
	}

	protected virtual void RefreshCommanders(StaticString context, StaticString pass)
	{
		for (int i = 0; i < this.aiCommanders.Count; i++)
		{
			AICommander aicommander = this.aiCommanders[i];
			aicommander.RefreshObjective();
			aicommander.RefreshMission();
			this.GenerateRetrofitUnitMessage(aicommander);
		}
		this.GenerateNewCommander();
	}

	protected virtual void ReleaseCommanders()
	{
		for (int i = 0; i < this.aiCommanders.Count; i++)
		{
			if (this.aiCommanders[i] != null)
			{
				this.aiCommanders[i].Release();
			}
		}
		this.aiCommanders.Clear();
	}

	protected virtual void RemoveCommander(AICommander commander)
	{
		commander.Release();
		this.aiCommanders.Remove(commander);
	}

	private void AIPlayer_AIPlayerStateChange(object sender, EventArgs e)
	{
		if (!this.IsActive())
		{
			this.ReleaseCommanders();
		}
	}

	private void GenerateRetrofitUnitMessage(AICommander commander)
	{
		for (int i = 0; i < commander.Missions.Count; i++)
		{
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
			Diagnostics.Assert(service2 != null);
			AICommanderMissionWithRequestArmy aicommanderMissionWithRequestArmy = commander.Missions[i] as AICommanderMissionWithRequestArmy;
			AIData_Army aidata_Army;
			if (aicommanderMissionWithRequestArmy != null && (aicommanderMissionWithRequestArmy.Completion == AICommanderMission.AICommanderMissionCompletion.Initializing || aicommanderMissionWithRequestArmy.Completion == AICommanderMission.AICommanderMissionCompletion.Pending || aicommanderMissionWithRequestArmy.Completion == AICommanderMission.AICommanderMissionCompletion.Running) && aicommanderMissionWithRequestArmy.AIDataArmyGUID.IsValid && aicommanderMissionWithRequestArmy.AllowRetrofit && this.aiDataRepository.TryGetAIData<AIData_Army>(aicommanderMissionWithRequestArmy.AIDataArmyGUID, out aidata_Army))
			{
				bool flag = true;
				Region region = service2.GetRegion(aidata_Army.Army.WorldPosition);
				if (region == null || region.Owner != aidata_Army.Army.Empire)
				{
					flag = false;
				}
				float num = 0f;
				bool flag2 = false;
				AIData_Unit unitData;
				for (int j = 0; j < aidata_Army.Army.StandardUnits.Count; j++)
				{
					if (this.aiDataRepository.TryGetAIData<AIData_Unit>(aidata_Army.Army.StandardUnits[j].GUID, out unitData) && unitData.RetrofitData.MayRetrofit && unitData.RetrofitData.MilitaryPowerDifference > 0f)
					{
						flag2 = true;
						num += unitData.RetrofitData.MilitaryPowerDifference;
					}
				}
				if (flag2)
				{
					float propertyValue = aidata_Army.Army.GetPropertyValue(SimulationProperties.MilitaryPower);
					float num2;
					bool flag3;
					bool flag4;
					aicommanderMissionWithRequestArmy.ComputeNeededArmyPower(out num2, out flag3, out flag4);
					float num3 = commander.GetPriority(aicommanderMissionWithRequestArmy);
					if (num <= 0f)
					{
						num3 = AILayer.Boost(num3, -0.9f);
					}
					else if (!flag3 && propertyValue < num2)
					{
						num3 = AILayer.Boost(num3, 0.2f);
					}
					DepartmentOfForeignAffairs agency = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
					Predicate<EvaluableMessage_RetrofitUnit> <>9__0;
					for (int k = 0; k < aidata_Army.Army.StandardUnits.Count; k++)
					{
						if (this.aiDataRepository.TryGetAIData<AIData_Unit>(aidata_Army.Army.StandardUnits[k].GUID, out unitData) && unitData.RetrofitData.MayRetrofit && unitData.RetrofitData.MilitaryPowerDifference > 0f)
						{
							Blackboard<BlackboardLayerID, BlackboardMessage> blackboard = commander.AIPlayer.Blackboard;
							BlackboardLayerID blackboardLayerID = BlackboardLayerID.Empire;
							BlackboardLayerID layerID = blackboardLayerID;
							Predicate<EvaluableMessage_RetrofitUnit> filter;
							if ((filter = <>9__0) == null)
							{
								filter = (<>9__0 = ((EvaluableMessage_RetrofitUnit match) => match.ElementGuid == unitData.Unit.GUID));
							}
							EvaluableMessage_RetrofitUnit evaluableMessage_RetrofitUnit = blackboard.FindFirst<EvaluableMessage_RetrofitUnit>(layerID, filter);
							if (evaluableMessage_RetrofitUnit == null || evaluableMessage_RetrofitUnit.State != BlackboardMessage.StateValue.Message_InProgress)
							{
								evaluableMessage_RetrofitUnit = new EvaluableMessage_RetrofitUnit(unitData.Unit.GUID);
								commander.AIPlayer.Blackboard.AddMessage(evaluableMessage_RetrofitUnit);
							}
							float num4 = 0f;
							for (int l = 0; l < unitData.RetrofitData.RetrofitCosts.Length; l++)
							{
								if (unitData.RetrofitData.RetrofitCosts[l].ResourceName == DepartmentOfTheTreasury.Resources.EmpireMoney)
								{
									num4 += unitData.RetrofitData.RetrofitCosts[l].Value;
								}
							}
							if (agency.IsInWarWithSomeone() && unitData.RetrofitData.MilitaryPowerDifference > unitData.Unit.UnitDesign.Context.GetPropertyValue(SimulationProperties.MilitaryPower) && flag)
							{
								evaluableMessage_RetrofitUnit.SetInterest(1f, 1f);
							}
							else
							{
								evaluableMessage_RetrofitUnit.SetInterest(this.retrofitGlobalPriority, num3);
							}
							evaluableMessage_RetrofitUnit.UpdateBuyEvaluation("Retrofit", 0UL, num4, (int)BuyEvaluation.MaxTurnGain, 0f, 0UL);
							evaluableMessage_RetrofitUnit.TimeOut = 1;
						}
					}
				}
			}
		}
	}

	public static string RegistryPath = "AI/MajorEmpire/AIEntity_Empire/";

	protected List<AICommander> aiCommanders = new List<AICommander>();

	private IAIDataRepositoryAIHelper aiDataRepository;

	private string layerName;

	private IPersonalityAIHelper personalityHelper;

	private float retrofitGlobalPriority = 0.8f;
}
