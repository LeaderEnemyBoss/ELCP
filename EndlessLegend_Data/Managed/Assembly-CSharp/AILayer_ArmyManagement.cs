using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Session;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AILayer_ArmyManagement : AILayerCommanderController, IXmlSerializable, ITickable
{
	public AILayer_ArmyManagement() : base("AILayer_ArmyManagement")
	{
	}

	public override void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		base.ReadXml(reader);
		if (num >= 2)
		{
			int attribute = reader.GetAttribute<int>("Count");
			if (attribute > 0)
			{
				reader.ReadStartElement("CommandersByObjectiveType");
				for (int i = 0; i < attribute; i++)
				{
					string attribute2 = reader.GetAttribute<string>("ObjectiveType");
					int attribute3 = reader.GetAttribute<int>("Count");
					if (attribute2 != string.Empty && attribute3 > 0)
					{
						reader.ReadStartElement("CommanderGUIDs");
						List<AICommanderWithObjective> list = new List<AICommanderWithObjective>();
						for (int j = 0; j < attribute3; j++)
						{
							GameEntityGUID guid = reader.ReadElementString<ulong>("GUID");
							AICommanderWithObjective aicommanderWithObjective = this.aiCommanders.Find((AICommander match) => match.InternalGUID == guid) as AICommanderWithObjective;
							if (aicommanderWithObjective != null)
							{
								list.Add(aicommanderWithObjective);
							}
						}
						this.commandersByType.Add(attribute2, list);
						reader.ReadEndElement();
					}
					else
					{
						reader.Skip();
					}
				}
				reader.ReadEndElement();
				return;
			}
			reader.Skip();
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		int num = writer.WriteVersionAttribute(2);
		base.WriteXml(writer);
		if (num >= 2)
		{
			writer.WriteStartElement("CommandersByObjectiveType");
			writer.WriteAttributeString<int>("Count", this.commandersByType.Count);
			foreach (KeyValuePair<StaticString, List<AICommanderWithObjective>> keyValuePair in this.commandersByType)
			{
				writer.WriteStartElement("CommanderGUIDs");
				writer.WriteAttributeString<StaticString>("ObjectiveType", keyValuePair.Key);
				writer.WriteAttributeString<int>("Count", keyValuePair.Value.Count);
				for (int i = 0; i < keyValuePair.Value.Count; i++)
				{
					writer.WriteElementString<ulong>("GUID", keyValuePair.Value[i].InternalGUID);
				}
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}
	}

	public AICommanderWithObjective FindCommander(GlobalObjectiveMessage globalObjectiveMessage)
	{
		if (!this.commandersByType.ContainsKey(globalObjectiveMessage.ObjectiveType))
		{
			return null;
		}
		return this.commandersByType[globalObjectiveMessage.ObjectiveType].Find((AICommanderWithObjective match) => match.IsProcessingGlobalObjective(globalObjectiveMessage));
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		this.animationCurveDatabase = Databases.GetDatabase<AnimationCurve>(false);
		this.endturnService = Services.GetService<IEndTurnService>();
		IGameService service = Services.GetService<IGameService>();
		this.playerRepositoryService = service.Game.Services.GetService<IPlayerRepositoryService>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		this.intelligenceAIHelper = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		this.worldAtlasHelper = AIScheduler.Services.GetService<IWorldAtlasAIHelper>();
		this.aiDataRepositoryHelper = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		this.departmentOfDefense = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		this.departmentOfDefense.OnSiegeStateChange += this.CitySiegeResponse;
		this.departmentOfForeignAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		this.departmentOfScience = base.AIEntity.Empire.GetAgency<DepartmentOfScience>();
		base.AIEntity.RegisterPass(AIEntity.Passes.ExecuteNeeds.ToString(), "AILayer_ArmyManagement_ExecuteNeedsPass", new AIEntity.AIAction(this.ExecuteNeeds), this, new StaticString[]
		{
			"AILayerArmyRecruitment_ExecuteNeedsPass"
		});
		this.optionalMissions.Add(AICommanderMissionDefinition.AICommanderCategory.Exploration, new List<GlobalObjectiveMessage>());
		this.optionalMissions.Add(AICommanderMissionDefinition.AICommanderCategory.Patrol, new List<GlobalObjectiveMessage>());
		this.optionalMissions.Add(AICommanderMissionDefinition.AICommanderCategory.WarPatrol, new List<GlobalObjectiveMessage>());
		IPersonalityAIHelper service2 = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		this.empirePillageOpportunityBoost = service2.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_ArmyManagement.registryPath, "PillageOpportunityBoost"), this.empirePillageOpportunityBoost);
		string registryValue = service2.GetRegistryValue<string>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_ArmyManagement.registryPath, "PillageOpportunityBoostByTurn/CurveName"), string.Empty);
		if (!string.IsNullOrEmpty(registryValue))
		{
			this.pillageOpportunityBoostByTurnCurve = this.animationCurveDatabase.GetValue(registryValue);
		}
		this.pillageOpportunityBoostByTurnMaximumTurn = service2.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_ArmyManagement.registryPath, "PillageOpportunityBoostByTurn/MaximumTurn"), this.pillageOpportunityBoostByTurnMaximumTurn);
		ITickableRepositoryAIHelper service3 = AIScheduler.Services.GetService<ITickableRepositoryAIHelper>();
		this.convertedVillagesToManage = new List<Village>();
		Diagnostics.Assert(service3 != null);
		service3.Register(this);
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public void RegisterCommanderLimitDelegate(StaticString commanderType, Func<int> limitDelegate)
	{
		if (this.commanderLimitByType.ContainsKey(commanderType))
		{
			return;
		}
		this.commanderLimitByType.Add(commanderType, limitDelegate);
	}

	public override void Release()
	{
		base.Release();
		this.animationCurveDatabase = null;
		this.endturnService = null;
		this.worldPositionningService = null;
		this.intelligenceAIHelper = null;
		this.aiDataRepositoryHelper = null;
		this.playerRepositoryService = null;
		if (this.departmentOfDefense != null)
		{
			this.departmentOfDefense.OnSiegeStateChange -= this.CitySiegeResponse;
			this.departmentOfDefense = null;
		}
		this.departmentOfForeignAffairs = null;
		this.departmentOfTheInterior = null;
		this.departmentOfScience = null;
		this.commanderLimitByType.Clear();
		this.optionalMissions.Clear();
		this.worldAtlasHelper = null;
		ITickableRepositoryAIHelper service = AIScheduler.Services.GetService<ITickableRepositoryAIHelper>();
		Diagnostics.Assert(service != null);
		service.Unregister(this);
		this.convertedVillagesToManage.Clear();
	}

	public void UnregisterCommanderLimitDelegate(StaticString commanderType)
	{
		this.commanderLimitByType.Remove(commanderType);
	}

	protected override void ExecuteNeeds(StaticString context, StaticString pass)
	{
		base.ExecuteNeeds(context, pass);
		this.State = TickableState.NeedTick;
		this.lastTickTime = global::Game.Time;
		this.Assignjobless = false;
		if (!this.IsActive())
		{
			this.State = TickableState.NoTick;
		}
		ISynchronousJobRepositoryAIHelper service = AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>();
		service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_ExecuteNeeds));
		service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_ManageFullCities));
		if (base.AIEntity.Empire.SimulationObject.Tags.Contains(AILayer_Village.TagConversionTrait))
		{
			this.convertedVillagesToManage = this.departmentOfTheInterior.ConvertedVillages.ToList<Village>();
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				Diagnostics.Log("ELCP: {0} has {1} converted Villages", new object[]
				{
					base.AIEntity.Empire,
					this.convertedVillagesToManage.Count
				});
			}
		}
		if (DepartmentOfTheInterior.CanPlaceGolemCamps(base.AIEntity.Empire))
		{
			service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_ManageFullCamps));
		}
		this.freeArmies.Clear();
		for (int i = 0; i < this.departmentOfDefense.Armies.Count; i++)
		{
			AIData_Army aidata = this.aiDataRepositoryHelper.GetAIData<AIData_Army>(this.departmentOfDefense.Armies[i].GUID);
			if (aidata != null && aidata.CommanderMission == null && !aidata.IsSolitary && !aidata.Army.IsSeafaring && !aidata.Army.HasCatspaw && !(aidata.Army is KaijuArmy))
			{
				if (aidata.Army.IsSettler)
				{
					this.BailArmy(aidata);
				}
				else if (aidata.IsTaggedFreeForExploration() && !this.intelligenceAIHelper.IsArmyBlockedInCityUnderSiege(aidata.Army))
				{
					this.freeArmies.Add(aidata);
				}
			}
		}
		if (this.freeArmies.Count > 0)
		{
			int num = 0;
			List<GlobalObjectiveMessage> list = this.optionalMissions[AICommanderMissionDefinition.AICommanderCategory.WarPatrol];
			list.Clear();
			list.AddRange(base.AIEntity.AIPlayer.Blackboard.GetMessages<GlobalObjectiveMessage>(BlackboardLayerID.Empire, (GlobalObjectiveMessage match) => match.ObjectiveType == AICommanderMissionDefinition.AICommanderCategory.WarPatrol.ToString() && match.State != BlackboardMessage.StateValue.Message_Canceled));
			list.Sort((GlobalObjectiveMessage left, GlobalObjectiveMessage right) => -1 * left.Interest.CompareTo(right.Interest));
			num += list.Count;
			List<GlobalObjectiveMessage> list2 = this.optionalMissions[AICommanderMissionDefinition.AICommanderCategory.Patrol];
			list2.Clear();
			list2.AddRange(base.AIEntity.AIPlayer.Blackboard.GetMessages<GlobalObjectiveMessage>(BlackboardLayerID.Empire, (GlobalObjectiveMessage match) => match.ObjectiveType == AICommanderMissionDefinition.AICommanderCategory.Patrol.ToString() && match.State != BlackboardMessage.StateValue.Message_Canceled));
			list2.Sort((GlobalObjectiveMessage left, GlobalObjectiveMessage right) => -1 * left.Interest.CompareTo(right.Interest));
			num += list2.Count;
			List<GlobalObjectiveMessage> list3 = this.optionalMissions[AICommanderMissionDefinition.AICommanderCategory.Exploration];
			list3.Clear();
			list3.AddRange(base.AIEntity.AIPlayer.Blackboard.GetMessages<GlobalObjectiveMessage>(BlackboardLayerID.Empire, (GlobalObjectiveMessage match) => match.ObjectiveType == AICommanderMissionDefinition.AICommanderCategory.Exploration.ToString() && match.State != BlackboardMessage.StateValue.Message_Canceled));
			list3.Sort((GlobalObjectiveMessage left, GlobalObjectiveMessage right) => -1 * left.Interest.CompareTo(right.Interest));
			num += list3.Count;
			this.freeArmies.Sort((AIData_Army left, AIData_Army right) => -1 * left.Army.GetPropertyValue(SimulationProperties.MilitaryPower).CompareTo(right.Army.GetPropertyValue(SimulationProperties.MilitaryPower)));
			if (this.departmentOfForeignAffairs.IsInWarWithSomeone())
			{
				this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.WarPatrol, false);
				this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.Patrol, false);
				this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.Exploration, false);
				this.DistributeFreeArmiesToCities();
				if (num != 0)
				{
					for (int j = 0; j < 3; j++)
					{
						if (this.freeArmies.Count <= 0)
						{
							return;
						}
						for (int k = 0; k < this.freeArmies.Count; k++)
						{
							if (j > 0)
							{
								this.ignoreMP = true;
							}
							this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.WarPatrol, true);
							this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.Patrol, true);
							this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.Exploration, true);
						}
					}
					this.ignoreMP = false;
					return;
				}
			}
			else
			{
				this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.Exploration, false);
				this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.Patrol, false);
				this.DistributeFreeArmiesToCities();
				if (num != 0)
				{
					int num2 = 0;
					while (num2 < 3 && this.freeArmies.Count > 0)
					{
						if (num2 > 0)
						{
							this.ignoreMP = true;
						}
						for (int l = 0; l < this.freeArmies.Count; l++)
						{
							this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.Exploration, true);
							this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.Patrol, true);
						}
						num2++;
					}
				}
			}
			this.ignoreMP = false;
		}
	}

	protected override void GenerateNewCommander()
	{
		base.GenerateNewCommander();
	}

	protected override void RefreshCommanders(StaticString context, StaticString pass)
	{
		this.pillageOpportunityModifier = 1f;
		this.pillageOpportunityModifier = AILayer.Boost(this.pillageOpportunityModifier, this.empirePillageOpportunityBoost);
		if (this.pillageOpportunityBoostByTurnCurve != null)
		{
			float xValue = (float)this.endturnService.Turn / this.pillageOpportunityBoostByTurnMaximumTurn;
			float boostFactor = this.pillageOpportunityBoostByTurnCurve.Evaluate(xValue);
			this.pillageOpportunityModifier = AILayer.Boost(this.pillageOpportunityModifier, boostFactor);
		}
		this.objectives.Clear();
		this.objectives.AddRange(base.AIEntity.AIPlayer.Blackboard.GetMessages<GlobalObjectiveMessage>(BlackboardLayerID.Empire, (GlobalObjectiveMessage match) => match.State != BlackboardMessage.StateValue.Message_Canceled));
		for (int i = this.objectives.Count - 1; i >= 0; i--)
		{
			GlobalObjectiveMessage globalObjectiveMessage = this.objectives[i];
			AICommanderWithObjective aicommanderWithObjective = this.FindCommander(globalObjectiveMessage);
			if (aicommanderWithObjective != null)
			{
				if (globalObjectiveMessage.State == BlackboardMessage.StateValue.Message_Canceled)
				{
					this.RemoveCommander(aicommanderWithObjective);
				}
				aicommanderWithObjective.LocalPriority = globalObjectiveMessage.LocalPriority;
				aicommanderWithObjective.GlobalPriority = globalObjectiveMessage.GlobalPriority;
				aicommanderWithObjective.GlobalPillageModifier = this.pillageOpportunityModifier;
				this.objectives.RemoveAt(i);
			}
		}
		this.objectives.Sort((GlobalObjectiveMessage left, GlobalObjectiveMessage right) => -1 * left.Interest.CompareTo(right.Interest));
		int j = 0;
		while (j < this.objectives.Count)
		{
			int commanderLimit = this.GetCommanderLimit(this.objectives[j]);
			StaticString objectiveType = this.objectives[j].ObjectiveType;
			if (!this.commandersByType.ContainsKey(objectiveType) || commanderLimit > this.commandersByType[objectiveType].Count)
			{
				goto IL_27B;
			}
			float num = this.objectives[j].Interest;
			num /= 2f;
			bool flag = false;
			for (int k = 0; k < this.commandersByType[objectiveType].Count; k++)
			{
				if (num >= this.commandersByType[objectiveType][k].LocalPriority * this.commandersByType[this.objectives[j].ObjectiveType][k].GlobalPriority)
				{
					this.RemoveCommander(this.commandersByType[this.objectives[j].ObjectiveType][0]);
					flag = true;
					break;
				}
			}
			if (flag)
			{
				goto IL_27B;
			}
			IL_272:
			j++;
			continue;
			IL_27B:
			AICommanderWithObjective aicommanderWithObjective2 = this.GenerateCommanderByType(this.objectives[j]);
			if (aicommanderWithObjective2 != null)
			{
				if (!this.commandersByType.ContainsKey(this.objectives[j].ObjectiveType))
				{
					this.commandersByType.Add(this.objectives[j].ObjectiveType, new List<AICommanderWithObjective>());
				}
				this.commandersByType[this.objectives[j].ObjectiveType].Add(aicommanderWithObjective2);
				aicommanderWithObjective2.GlobalPriority = this.objectives[j].GlobalPriority;
				aicommanderWithObjective2.LocalPriority = this.objectives[j].LocalPriority;
				aicommanderWithObjective2.GlobalPillageModifier = this.pillageOpportunityModifier;
				this.AddCommander(aicommanderWithObjective2);
				goto IL_272;
			}
			goto IL_272;
		}
		base.RefreshCommanders(context, pass);
	}

	protected override void ReleaseCommanders()
	{
		base.ReleaseCommanders();
		this.commandersByType.Clear();
	}

	protected override void RemoveCommander(AICommander commander)
	{
		AICommanderWithObjective aicommanderWithObjective = commander as AICommanderWithObjective;
		if (aicommanderWithObjective != null)
		{
			foreach (KeyValuePair<StaticString, List<AICommanderWithObjective>> keyValuePair in this.commandersByType)
			{
				if (keyValuePair.Value.Remove(aicommanderWithObjective))
				{
					break;
				}
			}
		}
		base.RemoveCommander(commander);
	}

	private void BailArmy(AIData_Army armyData)
	{
		AICommander_SettlerBail commander = new AICommander_SettlerBail
		{
			AIPlayer = base.AIEntity.AIPlayer,
			Empire = base.AIEntity.Empire,
			ForceArmyGUID = armyData.Army.GUID
		};
		this.AddCommander(commander);
	}

	private void DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory category, bool multipleCommanders)
	{
		if (this.freeArmies.Count == 0)
		{
			return;
		}
		List<GlobalObjectiveMessage> list = this.optionalMissions[category];
		for (int i = 0; i < list.Count; i++)
		{
			GlobalObjectiveMessage globalObjectiveMessage = list[i];
			if (multipleCommanders || base.AIEntity.GetCommanderProcessingTheNeededGlobalObjective(globalObjectiveMessage.ID) == null)
			{
				float minMilitaryPower = this.GetMinMilitaryPower(category, globalObjectiveMessage.RegionIndex);
				for (int j = 0; j < this.freeArmies.Count; j++)
				{
					AIData_Army aidata_Army = this.freeArmies[j];
					if (minMilitaryPower <= aidata_Army.Army.GetPropertyValue(SimulationProperties.MilitaryPower) || this.ignoreMP)
					{
						this.freeArmies.RemoveAt(j);
						this.GenerateDefaultCommanderByType(globalObjectiveMessage, aidata_Army.Army.GUID);
						break;
					}
				}
				if (this.freeArmies.Count == 0)
				{
					return;
				}
			}
		}
	}

	private void DistributeFreeArmiesToCities()
	{
		List<City> list = new List<City>();
		for (int i = 0; i < this.departmentOfTheInterior.Cities.Count; i++)
		{
			if (this.departmentOfTheInterior.Cities[i].BesiegingEmpire == null && this.departmentOfTheInterior.Cities[i].MaximumUnitSlot - this.departmentOfTheInterior.Cities[i].StandardUnits.Count > 2)
			{
				list.Add(this.departmentOfTheInterior.Cities[i]);
			}
		}
		for (int j = this.freeArmies.Count - 1; j >= 0; j--)
		{
			int count = this.freeArmies[j].Army.StandardUnits.Count;
			int regionIndex = (int)this.worldPositionningService.GetRegionIndex(this.freeArmies[j].Army.WorldPosition);
			for (int k = 0; k < list.Count; k++)
			{
				if (list[k].Region.Index == regionIndex)
				{
					int num = this.departmentOfTheInterior.Cities[k].MaximumUnitSlot - this.departmentOfTheInterior.Cities[k].StandardUnits.Count;
					if (count <= num)
					{
						this.BailArmy(this.freeArmies[j]);
						this.freeArmies.RemoveAt(j);
					}
				}
			}
		}
	}

	private AICommanderWithObjective GenerateCommanderByType(GlobalObjectiveMessage objective)
	{
		if (objective.ObjectiveType == AICommanderMissionDefinition.AICommanderCategory.Colonization.ToString())
		{
			return new AICommander_Colonization(objective.ID, objective.RegionIndex)
			{
				AIPlayer = base.AIEntity.AIPlayer,
				RegionTarget = this.worldPositionningService.GetRegion(objective.RegionIndex),
				Empire = base.AIEntity.Empire,
				GlobalPriority = objective.GlobalPriority,
				LocalPriority = objective.LocalPriority
			};
		}
		if (objective.ObjectiveType == AICommanderMissionDefinition.AICommanderCategory.Pacification.ToString())
		{
			return new AICommander_PacifyRegion(objective.ID, objective.RegionIndex)
			{
				AIPlayer = base.AIEntity.AIPlayer,
				RegionTarget = this.worldPositionningService.GetRegion(objective.RegionIndex),
				Empire = base.AIEntity.Empire,
				GlobalPriority = objective.GlobalPriority,
				LocalPriority = objective.LocalPriority,
				SubObjectiveGuid = objective.SubObjectifGUID
			};
		}
		if (objective.ObjectiveType == AICommanderMissionDefinition.AICommanderCategory.Defense.ToString())
		{
			return new AICommander_Defense(objective.ID, objective.RegionIndex)
			{
				AIPlayer = base.AIEntity.AIPlayer,
				RegionTarget = this.worldPositionningService.GetRegion(objective.RegionIndex),
				Empire = base.AIEntity.Empire,
				GlobalPriority = objective.GlobalPriority,
				LocalPriority = objective.LocalPriority
			};
		}
		if (objective.ObjectiveType == AICommanderMissionDefinition.AICommanderCategory.War.ToString())
		{
			return new AICommander_WarWithObjective(objective.ID, objective.RegionIndex)
			{
				AIPlayer = base.AIEntity.AIPlayer,
				Empire = base.AIEntity.Empire,
				GlobalPriority = objective.GlobalPriority,
				LocalPriority = objective.LocalPriority
			};
		}
		if (objective.ObjectiveType == AICommanderMissionDefinition.AICommanderCategory.SiegeBreaker.ToString())
		{
			return new AICommander_SiegeBreaker(objective.ID, objective.RegionIndex)
			{
				AIPlayer = base.AIEntity.AIPlayer,
				RegionTarget = this.worldPositionningService.GetRegion(objective.RegionIndex),
				Empire = base.AIEntity.Empire,
				GlobalPriority = objective.GlobalPriority,
				LocalPriority = objective.LocalPriority
			};
		}
		if (objective.ObjectiveType == "Village" && objective.SubObjectifGUID != GameEntityGUID.Zero)
		{
			return new AICommander_Village(objective.ID, objective.RegionIndex)
			{
				AIPlayer = base.AIEntity.AIPlayer,
				RegionTarget = this.worldPositionningService.GetRegion(objective.RegionIndex),
				Empire = base.AIEntity.Empire,
				GlobalPriority = objective.GlobalPriority,
				LocalPriority = objective.LocalPriority,
				SubObjectiveGuid = objective.SubObjectifGUID
			};
		}
		if (objective.ObjectiveType == AICommanderMissionDefinition.AICommanderCategory.Victory.ToString() && objective.SubObjectifGUID != GameEntityGUID.Zero)
		{
			return new AICommander_Victory(objective.ID, objective.RegionIndex)
			{
				AIPlayer = base.AIEntity.AIPlayer,
				RegionTarget = this.worldPositionningService.GetRegion(objective.RegionIndex),
				Empire = base.AIEntity.Empire,
				GlobalPriority = objective.GlobalPriority,
				LocalPriority = objective.LocalPriority,
				SubObjectiveGuid = objective.SubObjectifGUID
			};
		}
		if (objective.ObjectiveType == "QuestBTController" && objective.SubObjectifGUID != GameEntityGUID.Zero)
		{
			return new AICommander_QuestBTCommander(objective.ID, objective.RegionIndex)
			{
				AIPlayer = base.AIEntity.AIPlayer,
				Empire = base.AIEntity.Empire,
				GlobalPriority = objective.GlobalPriority,
				LocalPriority = objective.LocalPriority,
				SubObjectiveGuid = objective.SubObjectifGUID,
				QuestName = objective.ObjectiveState
			};
		}
		if (objective.ObjectiveType == AICommanderMissionDefinition.AICommanderCategory.Terraformation.ToString())
		{
			return new AICommander_Terraformation(objective.ID, objective.RegionIndex)
			{
				AIPlayer = base.AIEntity.AIPlayer,
				RegionTarget = this.worldPositionningService.GetRegion(objective.RegionIndex),
				Empire = base.AIEntity.Empire,
				GlobalPriority = objective.GlobalPriority,
				LocalPriority = objective.LocalPriority
			};
		}
		if (objective.ObjectiveType == AICommanderMissionDefinition.AICommanderCategory.KaijuAdquisition.ToString())
		{
			return new AICommander_KaijuAdquisition(objective.ID, objective.RegionIndex)
			{
				AIPlayer = base.AIEntity.AIPlayer,
				Empire = base.AIEntity.Empire,
				GlobalPriority = objective.GlobalPriority,
				LocalPriority = objective.LocalPriority,
				SubObjectiveGuid = objective.SubObjectifGUID
			};
		}
		return null;
	}

	private void GenerateDefaultCommanderByType(GlobalObjectiveMessage objective, GameEntityGUID armyGUID)
	{
		AICommanderWithObjective aicommanderWithObjective = null;
		if (objective.ObjectiveType == AICommanderMissionDefinition.AICommanderCategory.Exploration.ToString())
		{
			aicommanderWithObjective = new AICommander_Exploration(objective.ID, objective.RegionIndex)
			{
				AIPlayer = base.AIEntity.AIPlayer,
				RegionTarget = this.worldPositionningService.GetRegion(objective.RegionIndex),
				Empire = base.AIEntity.Empire,
				GlobalPriority = objective.GlobalPriority,
				LocalPriority = objective.LocalPriority
			};
		}
		else if (objective.ObjectiveType == AICommanderMissionDefinition.AICommanderCategory.Patrol.ToString())
		{
			aicommanderWithObjective = new AICommander_Patrol(objective.ID, objective.RegionIndex)
			{
				AIPlayer = base.AIEntity.AIPlayer,
				RegionTarget = this.worldPositionningService.GetRegion(objective.RegionIndex),
				Empire = base.AIEntity.Empire,
				GlobalPriority = objective.GlobalPriority,
				LocalPriority = objective.LocalPriority
			};
		}
		else if (objective.ObjectiveType == AICommanderMissionDefinition.AICommanderCategory.WarPatrol.ToString())
		{
			aicommanderWithObjective = new AICommander_WarPatrol(objective.ID, objective.RegionIndex)
			{
				AIPlayer = base.AIEntity.AIPlayer,
				RegionTarget = this.worldPositionningService.GetRegion(objective.RegionIndex),
				Empire = base.AIEntity.Empire,
				GlobalPriority = objective.GlobalPriority,
				LocalPriority = objective.LocalPriority
			};
		}
		if (aicommanderWithObjective == null)
		{
			return;
		}
		if (!this.commandersByType.ContainsKey(objective.ObjectiveType))
		{
			this.commandersByType.Add(objective.ObjectiveType, new List<AICommanderWithObjective>());
		}
		this.commandersByType[objective.ObjectiveType].Add(aicommanderWithObjective);
		aicommanderWithObjective.ForceArmyGUID = armyGUID;
		aicommanderWithObjective.GlobalPriority = objective.GlobalPriority;
		aicommanderWithObjective.LocalPriority = objective.LocalPriority;
		aicommanderWithObjective.GlobalPillageModifier = this.pillageOpportunityModifier;
		this.AddCommander(aicommanderWithObjective);
	}

	private int GetCommanderLimit(GlobalObjectiveMessage objective)
	{
		if (this.commanderLimitByType.ContainsKey(objective.ObjectiveType))
		{
			return this.commanderLimitByType[objective.ObjectiveType]();
		}
		return 1;
	}

	private SynchronousJobState SynchronousJob_ExecuteNeeds()
	{
		if (this.currentRetrofitOrderTicket != null)
		{
			if (!this.currentRetrofitOrderTicket.Raised)
			{
				return SynchronousJobState.Running;
			}
			OrderRetrofitUnit retrofitOrder = this.currentRetrofitOrderTicket.Order as OrderRetrofitUnit;
			if (retrofitOrder != null)
			{
				EvaluableMessage_RetrofitUnit firstMessage = base.AIEntity.AIPlayer.Blackboard.GetFirstMessage<EvaluableMessage_RetrofitUnit>(BlackboardLayerID.Empire, (EvaluableMessage_RetrofitUnit match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate && Array.Exists<GameEntityGUID>(retrofitOrder.UnitGuids, (GameEntityGUID unitGuid) => unitGuid == match.ElementGuid));
				if (firstMessage != null)
				{
					if (this.currentRetrofitOrderTicket.PostOrderResponse == PostOrderResponse.Processed)
					{
						firstMessage.SetObtained();
					}
					else
					{
						firstMessage.SetFailedToObtain();
					}
				}
			}
			this.currentRetrofitOrderTicket = null;
		}
		bool flag = true;
		foreach (Player player in this.playerRepositoryService)
		{
			if (player.Type == PlayerType.Human && player.State != PlayerState.Ready)
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			Diagnostics.Log("ELCP: AILayer_ArmyManagement SynchronousJob_ExecuteNeeds detected all humans are ready, aborting");
			foreach (EvaluableMessage_RetrofitUnit evaluableMessage_RetrofitUnit in base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_RetrofitUnit>(BlackboardLayerID.Empire))
			{
				evaluableMessage_RetrofitUnit.SetFailedToObtain();
			}
			return SynchronousJobState.Failure;
		}
		DepartmentOfDefense agency = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		AIData_Unit unitData;
		Func<UnitDesign, bool> <>9__2;
		foreach (EvaluableMessage_RetrofitUnit evaluableMessage_RetrofitUnit2 in base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_RetrofitUnit>(BlackboardLayerID.Empire))
		{
			if (evaluableMessage_RetrofitUnit2.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate)
			{
				if (this.aiDataRepositoryHelper.TryGetAIData<AIData_Unit>(evaluableMessage_RetrofitUnit2.ElementGuid, out unitData))
				{
					if (!(unitData.Unit.UnitDesign is UnitProfile))
					{
						IEnumerable<UnitDesign> userDefinedUnitDesigns = agency.UnitDesignDatabase.UserDefinedUnitDesigns;
						Func<UnitDesign, bool> predicate;
						if ((predicate = <>9__2) == null)
						{
							predicate = (<>9__2 = ((UnitDesign design) => design.Model == unitData.Unit.UnitDesign.Model));
						}
						UnitDesign unitDesign = userDefinedUnitDesigns.FirstOrDefault(predicate);
						if (unitDesign == null || unitDesign.ModelRevision <= unitData.Unit.UnitDesign.ModelRevision)
						{
							evaluableMessage_RetrofitUnit2.SetFailedToObtain();
						}
						else
						{
							DepartmentOfDefense departmentOfDefense = agency;
							Unit unit = unitData.Unit;
							IConstructionCost[] retrofitCosts = agency.GetRetrofitCosts(unitData.Unit, unitDesign);
							IConstructionCost[] costs = retrofitCosts;
							if (departmentOfDefense.CheckRetrofitPrerequisites(unit, costs) == DepartmentOfDefense.CheckRetrofitPrerequisitesResult.Ok)
							{
								OrderRetrofitUnit order = new OrderRetrofitUnit(base.AIEntity.Empire.Index, new GameEntityGUID[]
								{
									evaluableMessage_RetrofitUnit2.ElementGuid
								});
								base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out this.currentRetrofitOrderTicket, null);
								return SynchronousJobState.Running;
							}
							evaluableMessage_RetrofitUnit2.SetFailedToObtain();
						}
					}
				}
				else
				{
					evaluableMessage_RetrofitUnit2.SetFailedToObtain();
				}
			}
		}
		return SynchronousJobState.Success;
	}

	private float GetMinMilitaryPower(AICommanderMissionDefinition.AICommanderCategory category, int regionIndex)
	{
		if (category != AICommanderMissionDefinition.AICommanderCategory.Exploration)
		{
			return 1f;
		}
		Army maxHostileArmy = AILayer_Pacification.GetMaxHostileArmy(base.AIEntity.Empire, regionIndex);
		if (maxHostileArmy == null)
		{
			return this.intelligenceAIHelper.EvaluateMaxMilitaryPowerOfRegion(base.AIEntity.Empire, regionIndex);
		}
		return this.intelligenceAIHelper.EvaluateMilitaryPowerOfGarrison(base.AIEntity.Empire, maxHostileArmy, 0);
	}

	private SynchronousJobState SynchronousJob_ManageVillageArmies()
	{
		MajorEmpire majorEmpire = base.AIEntity.Empire as MajorEmpire;
		SynchronousJobState result;
		if (majorEmpire == null || this.departmentOfDefense.Armies == null)
		{
			result = SynchronousJobState.Success;
		}
		else
		{
			int count = this.departmentOfDefense.Armies.ToList<Army>().FindAll((Army a) => !a.IsSeafaring).Count;
			int num = majorEmpire.ConvertedVillages.Count / 2 + 5;
			int num2 = (majorEmpire.ConvertedVillages.Count > 0) ? majorEmpire.ConvertedVillages[0].MaximumUnitSlot : 4;
			float num3;
			base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>().TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpireMoney, out num3, false);
			DepartmentOfScience agency = base.AIEntity.Empire.GetAgency<DepartmentOfScience>();
			float propertyValue = base.AIEntity.Empire.GetPropertyValue(SimulationProperties.NetEmpireMoney);
			foreach (Village village in majorEmpire.ConvertedVillages)
			{
				if (village.UnitsCount != 0 && !village.IsInEncounter && this.AreaIsSave(village, 7))
				{
					List<Unit> list = new List<Unit>();
					list.AddRange(village.Units);
					if (this.departmentOfForeignAffairs.IsInWarWithSomeone())
					{
						if (num3 + propertyValue < 100f + (float)(this.endturnService.Turn * 5) + (float)(majorEmpire.ConvertedVillages.Count * 40) && agency.CanTradeUnits(false))
						{
							OrderSelloutTradableUnits order = new OrderSelloutTradableUnits(base.AIEntity.Empire.Index, list.ConvertAll<GameEntityGUID>((Unit unit) => unit.GUID).ToArray());
							Ticket ticket;
							base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, null);
						}
						if (propertyValue >= 0f || num3 > 100f)
						{
							this.CreateVillageArmy(village, list);
						}
					}
					else if (num3 + propertyValue < 400f + (float)(this.endturnService.Turn * 5) + (float)(majorEmpire.ConvertedVillages.Count * 80) && agency.CanTradeUnits(false))
					{
						OrderSelloutTradableUnits order2 = new OrderSelloutTradableUnits(base.AIEntity.Empire.Index, list.ConvertAll<GameEntityGUID>((Unit unit) => unit.GUID).ToArray());
						Ticket ticket2;
						base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order2, out ticket2, null);
					}
					else if (propertyValue >= 10f && (count < num || list.Count >= num2))
					{
						this.CreateVillageArmy(village, list);
					}
				}
			}
			result = SynchronousJobState.Success;
		}
		return result;
	}

	private void CreateVillageArmy(Village village, List<Unit> units)
	{
		List<WorldPosition> availablePositionsForArmyCreation = DepartmentOfDefense.GetAvailablePositionsForArmyCreation(village);
		IGameService service = Services.GetService<IGameService>();
		if (service.Game != null)
		{
			IPathfindingService service2 = service.Game.Services.GetService<IPathfindingService>();
			if (service2 != null)
			{
				for (int i = 0; i < availablePositionsForArmyCreation.Count; i++)
				{
					WorldPosition worldPosition = availablePositionsForArmyCreation[i];
					bool flag = true;
					for (int j = 0; j < units.Count; j++)
					{
						Unit unit2 = units[j];
						float transitionCost = service2.GetTransitionCost(village.WorldPosition, worldPosition, unit2, PathfindingFlags.IgnoreFogOfWar, null);
						if (unit2.GetPropertyValue(SimulationProperties.Movement) < transitionCost)
						{
							flag = false;
							break;
						}
					}
					if (flag)
					{
						OrderTransferGarrisonToNewArmy order = new OrderTransferGarrisonToNewArmy(village.Empire.Index, village.GUID, units.ConvertAll<GameEntityGUID>((Unit unit) => unit.GUID).ToArray(), worldPosition, StaticString.Empty, false, true, true);
						Ticket ticket;
						base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnCreateResponse));
						return;
					}
				}
			}
		}
	}

	private bool AreaIsSave(Village village, int size)
	{
		bool result;
		if (size < 1)
		{
			result = true;
		}
		else
		{
			global::Empire owner = village.Region.Owner;
			if (owner == base.AIEntity.Empire)
			{
				result = true;
			}
			else if (owner != null && !this.departmentOfForeignAffairs.IsEnnemy(owner))
			{
				result = true;
			}
			else
			{
				if (owner == null || !this.departmentOfForeignAffairs.IsEnnemy(owner))
				{
					foreach (WorldPosition worldPosition in new WorldCircle(village.WorldPosition, size).GetWorldPositions(this.worldPositionningService.World.WorldParameters))
					{
						Army armyAtPosition = this.worldPositionningService.GetArmyAtPosition(worldPosition);
						if (armyAtPosition != null && !armyAtPosition.IsFomorian)
						{
							Region region = this.worldPositionningService.GetRegion(worldPosition);
							if (!(armyAtPosition.Empire is MinorEmpire) || region == village.Region)
							{
								if (armyAtPosition.IsPrivateers)
								{
									return false;
								}
								if (this.departmentOfForeignAffairs.IsEnnemy(armyAtPosition.Empire))
								{
									return false;
								}
							}
						}
						District district = this.worldPositionningService.GetDistrict(worldPosition);
						if (district != null && district.Empire != null && this.departmentOfForeignAffairs.IsEnnemy(district.Empire))
						{
							return false;
						}
					}
					return true;
				}
				result = false;
			}
		}
		return result;
	}

	protected void OnCreateResponse(object sender, TicketRaisedEventArgs args)
	{
		if (args.Result == PostOrderResponse.Processed)
		{
			OrderTransferGarrisonToNewArmy orderTransferGarrisonToNewArmy = args.Order as OrderTransferGarrisonToNewArmy;
			IGameService service = Services.GetService<IGameService>();
			if (orderTransferGarrisonToNewArmy != null && orderTransferGarrisonToNewArmy.ArmyGuid.IsValid)
			{
				IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
				IGameEntity gameEntity;
				if (service2 != null && service2.TryGetValue(orderTransferGarrisonToNewArmy.ArmyGuid, out gameEntity))
				{
					Army army = gameEntity as Army;
					if (army != null)
					{
						AIData_Army aidata = this.aiDataRepositoryHelper.GetAIData<AIData_Army>(army.GUID);
						if (aidata != null)
						{
							this.freeArmies.Add(aidata);
							this.ignoreMP = true;
							if (this.departmentOfForeignAffairs.IsInWarWithSomeone())
							{
								List<GlobalObjectiveMessage> list = this.optionalMissions[AICommanderMissionDefinition.AICommanderCategory.WarPatrol];
								if (list.Count < 1)
								{
									list.Clear();
									list.AddRange(base.AIEntity.AIPlayer.Blackboard.GetMessages<GlobalObjectiveMessage>(BlackboardLayerID.Empire, (GlobalObjectiveMessage match) => match.ObjectiveType == AICommanderMissionDefinition.AICommanderCategory.WarPatrol.ToString() && match.State != BlackboardMessage.StateValue.Message_Canceled));
									list.Sort((GlobalObjectiveMessage left, GlobalObjectiveMessage right) => -1 * left.Interest.CompareTo(right.Interest));
								}
								this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.WarPatrol, true);
								if (this.freeArmies.Count > 0)
								{
									list = this.optionalMissions[AICommanderMissionDefinition.AICommanderCategory.Patrol];
								}
								if (list.Count < 1)
								{
									list.Clear();
									list.AddRange(base.AIEntity.AIPlayer.Blackboard.GetMessages<GlobalObjectiveMessage>(BlackboardLayerID.Empire, (GlobalObjectiveMessage match) => match.ObjectiveType == AICommanderMissionDefinition.AICommanderCategory.Patrol.ToString() && match.State != BlackboardMessage.StateValue.Message_Canceled));
									list.Sort((GlobalObjectiveMessage left, GlobalObjectiveMessage right) => -1 * left.Interest.CompareTo(right.Interest));
								}
								this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.Patrol, true);
								this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.Exploration, true);
								return;
							}
							List<GlobalObjectiveMessage> list2 = this.optionalMissions[AICommanderMissionDefinition.AICommanderCategory.Patrol];
							if (list2.Count < 1)
							{
								list2.Clear();
								list2.AddRange(base.AIEntity.AIPlayer.Blackboard.GetMessages<GlobalObjectiveMessage>(BlackboardLayerID.Empire, (GlobalObjectiveMessage match) => match.ObjectiveType == AICommanderMissionDefinition.AICommanderCategory.Patrol.ToString() && match.State != BlackboardMessage.StateValue.Message_Canceled));
								list2.Sort((GlobalObjectiveMessage left, GlobalObjectiveMessage right) => -1 * left.Interest.CompareTo(right.Interest));
							}
							this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.Patrol, true);
							if (this.freeArmies.Count > 0)
							{
								list2 = this.optionalMissions[AICommanderMissionDefinition.AICommanderCategory.Exploration];
							}
							if (list2.Count < 1)
							{
								list2.Clear();
								list2.AddRange(base.AIEntity.AIPlayer.Blackboard.GetMessages<GlobalObjectiveMessage>(BlackboardLayerID.Empire, (GlobalObjectiveMessage match) => match.ObjectiveType == AICommanderMissionDefinition.AICommanderCategory.Exploration.ToString() && match.State != BlackboardMessage.StateValue.Message_Canceled));
								list2.Sort((GlobalObjectiveMessage left, GlobalObjectiveMessage right) => -1 * left.Interest.CompareTo(right.Interest));
							}
							this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.Exploration, true);
							this.ignoreMP = false;
						}
					}
				}
			}
		}
	}

	private SynchronousJobState SynchronousJob_ManageFullCities()
	{
		bool flag = false;
		if (this.departmentOfForeignAffairs.IsInWarWithSomeone())
		{
			using (IEnumerator<City> enumerator = this.departmentOfTheInterior.Cities.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.BesiegingEmpire != null)
					{
						flag = true;
						break;
					}
				}
			}
		}
		List<Army> list = new List<Army>();
		if (flag)
		{
			list = this.departmentOfDefense.Armies.ToList<Army>().FindAll((Army match) => !match.IsSeafaring && !match.IsSettler && !match.IsSolitary);
		}
		foreach (City city in this.departmentOfTheInterior.Cities)
		{
			if (!city.IsInfected)
			{
				if (flag && (list.Count < 8 || list.Count < this.departmentOfTheInterior.Cities.Count * 2) && city.StandardUnits.Count > 0 && city.BesiegingEmpire == null && AILayer_Military.AreaIsSave(city.WorldPosition, 8, this.departmentOfForeignAffairs, false))
				{
					List<Unit> units = city.StandardUnits.ToList<Unit>().FindAll((Unit x) => !x.CheckUnitAbility(UnitAbility.ReadonlyColonize, -1));
					this.CreateCityArmy(city, units);
				}
				else if (!this.departmentOfScience.CanParley() && !this.worldAtlasHelper.IsRegionPacified(base.AIEntity.Empire, city.Region) && !this.departmentOfForeignAffairs.IsInWarWithSomeone() && city.StandardUnits.Count > 1)
				{
					if (city.StandardUnits != null)
					{
						List<Unit> list2 = new List<Unit>();
						foreach (Unit unit in city.StandardUnits)
						{
							if (!unit.IsSettler)
							{
								list2.Add(unit);
							}
						}
						this.CreateCityArmy(city, list2);
					}
				}
				else
				{
					ConstructionQueue constructionQueue = base.AIEntity.Empire.GetAgency<DepartmentOfIndustry>().GetConstructionQueue(city);
					int num = 0;
					DepartmentOfTheTreasury agency = base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>();
					float num2 = 0f;
					if (!agency.TryGetResourceStockValue(city, DepartmentOfTheTreasury.Resources.Production, out num2, false))
					{
						num2 = 0f;
					}
					num2 += city.GetPropertyValue(SimulationProperties.NetCityProduction);
					num2 = Math.Max(1f, num2);
					if (constructionQueue.PendingConstructions.Count == 0 && city.BesiegingEmpire == null)
					{
						num = 1;
					}
					else
					{
						for (int i = 0; i < constructionQueue.PendingConstructions.Count; i++)
						{
							Construction construction = constructionQueue.PeekAt(i);
							float num3 = 0f;
							for (int j = 0; j < construction.CurrentConstructionStock.Length; j++)
							{
								if (construction.CurrentConstructionStock[j].PropertyName == "Production")
								{
									num3 += construction.CurrentConstructionStock[j].Stock;
									if (construction.IsBuyout)
									{
										num3 = DepartmentOfTheTreasury.GetProductionCostWithBonus(city, construction.ConstructibleElement, "Production");
									}
								}
							}
							float num4 = DepartmentOfTheTreasury.GetProductionCostWithBonus(city, construction.ConstructibleElement, "Production") - num3;
							num2 -= num4;
							if (num2 >= 0f && construction.ConstructibleElement is UnitDesign && !(construction.ConstructibleElement as UnitDesign).Tags.Contains(UnitDesign.TagSeafaring) && !(construction.ConstructibleElement as UnitDesign).Tags.Contains(DownloadableContent9.TagColossus))
							{
								num++;
							}
							if (num2 < 0f)
							{
								break;
							}
						}
					}
					if (city.StandardUnits != null && city.StandardUnits.Count > 0 && city.StandardUnits.Count + num > city.MaximumUnitSlot)
					{
						int num5 = city.StandardUnits.Count + num - city.MaximumUnitSlot;
						if (num5 < 3)
						{
							num5 = 3;
						}
						if (num5 > city.StandardUnits.Count)
						{
							num5 = city.StandardUnits.Count;
						}
						List<Unit> list3 = new List<Unit>();
						for (int k = 0; k < num5; k++)
						{
							list3.Add(city.StandardUnits[k]);
						}
						this.CreateCityArmy(city, list3);
					}
				}
			}
		}
		return SynchronousJobState.Success;
	}

	private void CreateCityArmy(City city, List<Unit> units)
	{
		if (units.Count == 0)
		{
			return;
		}
		List<WorldPosition> availablePositionsForArmyCreation = DepartmentOfDefense.GetAvailablePositionsForArmyCreation(city);
		IGameService service = Services.GetService<IGameService>();
		if (service.Game != null && service.Game.Services.GetService<IPathfindingService>() != null)
		{
			for (int i = 0; i < availablePositionsForArmyCreation.Count; i++)
			{
				WorldPosition armyPosition = availablePositionsForArmyCreation[i];
				for (int j = 0; j < units.Count; j++)
				{
					if (units[j].GetPropertyValue(SimulationProperties.Movement) < 3f)
					{
						units.RemoveAt(j);
						j--;
					}
				}
				if (units.Count > 0)
				{
					OrderTransferGarrisonToNewArmy order = new OrderTransferGarrisonToNewArmy(city.Empire.Index, city.GUID, units.ConvertAll<GameEntityGUID>((Unit unit) => unit.GUID).ToArray(), armyPosition, StaticString.Empty, false, true, true);
					Ticket ticket;
					base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnCreateResponse));
					return;
				}
			}
		}
	}

	private SynchronousJobState SynchronousJob_ManageFullCamps()
	{
		bool flag = false;
		foreach (AICommander aicommander in this.aiCommanders)
		{
			if (aicommander is AICommander_Colonization && !(aicommander as AICommander_Colonization).RegionTarget.IsRegionColonized() && aicommander.Missions.Count > 0 && !aicommander.Missions[0].AIDataArmyGUID.IsValid)
			{
				flag = true;
			}
		}
		foreach (Camp camp in this.departmentOfTheInterior.Camps)
		{
			if (!camp.IsInEncounter && camp.StandardUnits.Count > 0)
			{
				if (AILayer_Military.GetCampDefenseLocalPriority(camp, 0.8f, AICommanderMission_GarrisonCamp.SimulatedUnitsCount) == 0f && camp.StandardUnits.Count > camp.MaximumUnitSlot / 2)
				{
					List<Unit> list = new List<Unit>();
					list.AddRange(camp.StandardUnits);
					this.CreateCampArmy(camp, list);
				}
				else if (flag && this.CreateCampSettlerArmy(camp))
				{
					return SynchronousJobState.Success;
				}
			}
		}
		return SynchronousJobState.Success;
	}

	private void CreateCampArmy(Camp camp, List<Unit> units)
	{
		WorldPosition worldPosition = camp.WorldPosition;
		if (this.worldPositionningService.GetArmyAtPosition(worldPosition) == null)
		{
			bool flag = true;
			for (int i = 0; i < units.Count; i++)
			{
				if (units[i].GetPropertyValue(SimulationProperties.Movement) < 1.5f)
				{
					flag = false;
				}
			}
			if (flag)
			{
				OrderTransferGarrisonToNewArmy order = new OrderTransferGarrisonToNewArmy(camp.Empire.Index, camp.GUID, units.ConvertAll<GameEntityGUID>((Unit unit) => unit.GUID).ToArray(), worldPosition, StaticString.Empty, false, true, true);
				Ticket ticket;
				base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnCreateResponse));
				return;
			}
		}
	}

	private bool AreaIsSave(WorldPosition pos, int size)
	{
		if (size < 1)
		{
			return true;
		}
		WorldArea worldArea = new WorldArea(new WorldPosition[]
		{
			pos
		});
		for (int i = 0; i < size; i++)
		{
			worldArea = worldArea.Grow(this.worldPositionningService.World.WorldParameters);
		}
		foreach (WorldPosition worldPosition in worldArea.WorldPositions)
		{
			Army armyAtPosition = this.worldPositionningService.GetArmyAtPosition(worldPosition);
			if (armyAtPosition != null && !armyAtPosition.IsFomorian && !armyAtPosition.IsNaval && !(armyAtPosition.Empire is MinorEmpire))
			{
				if (armyAtPosition.IsPrivateers)
				{
					return false;
				}
				if (this.departmentOfForeignAffairs.IsEnnemy(armyAtPosition.Empire))
				{
					return false;
				}
			}
		}
		return true;
	}

	public void CitySiegeResponse(object sender, SiegeStateChangedEventArgs eventArgs)
	{
		Diagnostics.Assert(eventArgs.City != null);
		if (eventArgs.Attacker.IsSeafaring)
		{
			return;
		}
		if (base.AIEntity.Empire.Index == eventArgs.Attacker.Empire.Index)
		{
			GameServer gameServer = (Services.GetService<ISessionService>().Session as global::Session).GameServer as GameServer;
			AIPlayer_MajorEmpire aiplayer_MajorEmpire;
			if (gameServer.AIScheduler != null && gameServer.AIScheduler.TryGetMajorEmpireAIPlayer(eventArgs.City.Empire as MajorEmpire, out aiplayer_MajorEmpire))
			{
				AIEntity entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
				if (entity != null)
				{
					AILayer_ArmyManagement layer = entity.GetLayer<AILayer_ArmyManagement>();
					if (layer.IsActive())
					{
						layer.CitySiegeResponse(sender, eventArgs);
					}
				}
			}
		}
		if (base.AIEntity.Empire.Index != eventArgs.City.Empire.Index || !eventArgs.NewState || !this.IsActive())
		{
			return;
		}
		List<Army> list = new List<Army>();
		list = this.departmentOfDefense.Armies.ToList<Army>().FindAll((Army match) => !match.IsSeafaring && !match.IsSettler && !match.IsSolitary);
		foreach (City city in this.departmentOfTheInterior.Cities)
		{
			if ((list.Count < 8 || list.Count < this.departmentOfTheInterior.Cities.Count * 2) && city.StandardUnits.Count > 0 && ((city.BesiegingEmpire == null && AILayer_Military.AreaIsSave(city.WorldPosition, 8, this.departmentOfForeignAffairs, false)) || city.GetPropertyValue(SimulationProperties.CityDefensePoint) < DepartmentOfTheInterior.GetBesiegingPower(city, true)))
			{
				List<Unit> units = city.StandardUnits.ToList<Unit>().FindAll((Unit x) => !x.IsSettler);
				this.CreateCityArmy(city, units);
			}
		}
		if (eventArgs.City.Camp != null && eventArgs.City.Camp.GUID.IsValid && !eventArgs.City.Camp.IsInEncounter && eventArgs.City.Camp.StandardUnits.Count > 0)
		{
			List<Unit> list2 = new List<Unit>();
			list2.AddRange(eventArgs.City.Camp.StandardUnits);
			this.CreateCampArmy(eventArgs.City.Camp, list2);
		}
	}

	public void Tick()
	{
		if (this.State == TickableState.NeedTick)
		{
			this.lastTickTime = global::Game.Time;
		}
		this.State = TickableState.Optional;
		if (!this.IsActive())
		{
			this.State = TickableState.NoTick;
			return;
		}
		if (this.convertedVillagesToManage.Count > 0)
		{
			this.Tick_ManageConvertedVillages();
		}
		if (global::Game.Time - this.lastTickTime < 20.0)
		{
			return;
		}
		if (!this.Assignjobless)
		{
			this.InstantRegroup();
			this.Assignjobless = true;
			return;
		}
		this.AssignJoblessArmies();
		this.Assignjobless = false;
		this.lastTickTime = global::Game.Time;
	}

	public void AssignJoblessArmies()
	{
		this.freeArmies.Clear();
		bool flag = false;
		for (int i = 0; i < this.departmentOfDefense.Armies.Count; i++)
		{
			if (this.departmentOfDefense.Armies[i].GetPropertyBaseValue(SimulationProperties.Movement) > 0.001f)
			{
				if (this.departmentOfDefense.Armies[i].IsInEncounter)
				{
					flag = true;
				}
				else
				{
					AIData_Army aidata = this.aiDataRepositoryHelper.GetAIData<AIData_Army>(this.departmentOfDefense.Armies[i].GUID);
					if (aidata != null && !aidata.IsSolitary && !aidata.Army.IsSeafaring && !aidata.Army.HasCatspaw && !aidata.Army.IsPrivateers && !(aidata.Army is KaijuArmy))
					{
						flag = true;
						if (aidata.CommanderMission == null)
						{
							if (aidata.Army.IsSettler)
							{
								this.BailArmy(aidata);
							}
							else if (!this.intelligenceAIHelper.IsArmyBlockedInCityUnderSiege(aidata.Army))
							{
								this.freeArmies.Add(aidata);
							}
						}
					}
				}
			}
		}
		if (!flag)
		{
			this.State = TickableState.NoTick;
			return;
		}
		if (this.freeArmies.Count > 0)
		{
			this.ignoreMP = true;
			if (this.departmentOfForeignAffairs.IsInWarWithSomeone())
			{
				List<GlobalObjectiveMessage> list = this.optionalMissions[AICommanderMissionDefinition.AICommanderCategory.WarPatrol];
				if (list.Count < 1)
				{
					list.Clear();
					list.AddRange(base.AIEntity.AIPlayer.Blackboard.GetMessages<GlobalObjectiveMessage>(BlackboardLayerID.Empire, (GlobalObjectiveMessage match) => match.ObjectiveType == AICommanderMissionDefinition.AICommanderCategory.WarPatrol.ToString() && match.State != BlackboardMessage.StateValue.Message_Canceled));
					list.Sort((GlobalObjectiveMessage left, GlobalObjectiveMessage right) => -1 * left.Interest.CompareTo(right.Interest));
				}
				this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.WarPatrol, true);
				if (this.freeArmies.Count > 0)
				{
					list = this.optionalMissions[AICommanderMissionDefinition.AICommanderCategory.Patrol];
				}
				if (list.Count < 1)
				{
					list.Clear();
					list.AddRange(base.AIEntity.AIPlayer.Blackboard.GetMessages<GlobalObjectiveMessage>(BlackboardLayerID.Empire, (GlobalObjectiveMessage match) => match.ObjectiveType == AICommanderMissionDefinition.AICommanderCategory.Patrol.ToString() && match.State != BlackboardMessage.StateValue.Message_Canceled));
					list.Sort((GlobalObjectiveMessage left, GlobalObjectiveMessage right) => -1 * left.Interest.CompareTo(right.Interest));
				}
				this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.Patrol, true);
				this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.Exploration, true);
				return;
			}
			List<GlobalObjectiveMessage> list2 = this.optionalMissions[AICommanderMissionDefinition.AICommanderCategory.Exploration];
			if (list2.Count < 1)
			{
				list2.Clear();
				list2.AddRange(base.AIEntity.AIPlayer.Blackboard.GetMessages<GlobalObjectiveMessage>(BlackboardLayerID.Empire, (GlobalObjectiveMessage match) => match.ObjectiveType == AICommanderMissionDefinition.AICommanderCategory.Exploration.ToString() && match.State != BlackboardMessage.StateValue.Message_Canceled));
				list2.Sort((GlobalObjectiveMessage left, GlobalObjectiveMessage right) => -1 * left.Interest.CompareTo(right.Interest));
			}
			this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.Exploration, true);
			this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.Patrol, true);
			this.ignoreMP = false;
		}
	}

	private bool IsMercArmy(Army army)
	{
		if (army.IsPrivateers)
		{
			return true;
		}
		if (army.StandardUnits.Count == 0)
		{
			return false;
		}
		using (IEnumerator<Unit> enumerator = army.StandardUnits.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (!enumerator.Current.UnitDesign.Tags.Contains(TradableUnit.ReadOnlyMercenary))
				{
					return false;
				}
			}
		}
		return true;
	}

	private void InstantRegroup()
	{
		DepartmentOfDefense agency = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		IGameService service = Services.GetService<IGameService>();
		IPathfindingService pathfindingService = service.Game.Services.GetService<IPathfindingService>();
		int num = (int)base.AIEntity.Empire.GetPropertyValue(SimulationProperties.ArmyUnitSlot);
		Dictionary<Army, Army> dictionary = new Dictionary<Army, Army>();
		List<Army> UsedArmies = new List<Army>();
		List<Army> list = agency.Armies.ToList<Army>().FindAll((Army x) => !x.IsSolitary && !x.HasSeafaringUnits() && !x.HasCatspaw && !x.IsSettler && !x.IsPillaging && x.StandardUnits.Count < num && !x.IsInEncounter && !x.SimulationObject.Tags.Contains(DepartmentOfTheInterior.ArmyStatusBesiegerDescriptorName) && !x.IsDismantlingDevice && !(x is KaijuArmy));
		list.RemoveAll((Army x) => this.aiDataRepositoryHelper.GetAIData<AIData_Army>(x.GUID) != null && this.aiDataRepositoryHelper.GetAIData<AIData_Army>(x.GUID).CommanderMission != null && (this.aiDataRepositoryHelper.GetAIData<AIData_Army>(x.GUID).CommanderMission is AICommanderMission_VictoryRuinFinal || this.aiDataRepositoryHelper.GetAIData<AIData_Army>(x.GUID).CommanderMission is AICommanderMission_QuestsolverRuin));
		for (int i = 0; i < list.Count; i++)
		{
			Army army = list[i];
			if (!UsedArmies.Contains(army))
			{
				List<Army> list2 = list.FindAll((Army x) => !UsedArmies.Contains(x) && this.worldPositionningService.GetDistance(army.WorldPosition, x.WorldPosition) == 1 && pathfindingService.IsTransitionPassable(army.WorldPosition, x.WorldPosition, army, PathfindingFlags.IgnoreArmies, null));
				int num5 = -1;
				int num2 = int.MinValue;
				for (int j = 0; j < list2.Count; j++)
				{
					if ((army.GetPropertyValue(SimulationProperties.Movement) > 0.1f || list2[j].GetPropertyValue(SimulationProperties.Movement) > 0.1f) && (army.Hero == null || list2[j].Hero == null) && (this.IsMercArmy(army) == this.IsMercArmy(list2[j]) || (army.StandardUnits.Count == 0 && !list2[j].IsPrivateers)))
					{
						int num3 = army.StandardUnits.Count + list2[j].StandardUnits.Count;
						if (num3 == num)
						{
							num5 = j;
							break;
						}
						int num4 = num3 - num;
						if (num4 < 0 && num4 > num2)
						{
							num2 = num4;
							num5 = j;
						}
					}
				}
				if (num5 >= 0)
				{
					dictionary.Add(army, list2[num5]);
					UsedArmies.Add(army);
					UsedArmies.Add(list2[num5]);
				}
			}
		}
		foreach (KeyValuePair<Army, Army> keyValuePair in dictionary)
		{
			Army army3;
			Army army2;
			if (keyValuePair.Key.GetPropertyValue(SimulationProperties.Movement) >= keyValuePair.Value.GetPropertyValue(SimulationProperties.Movement))
			{
				army3 = keyValuePair.Key;
				army2 = keyValuePair.Value;
			}
			else
			{
				army2 = keyValuePair.Key;
				army3 = keyValuePair.Value;
			}
			AIData_Army aidata = this.aiDataRepositoryHelper.GetAIData<AIData_Army>(army3.GUID);
			if (aidata != null && aidata.CommanderMission != null)
			{
				aidata.CommanderMission.Interrupt();
			}
			OrderTransferUnits order = new OrderTransferUnits(base.AIEntity.Empire.Index, army3.GUID, army2.GUID, army3.Units.ToList<Unit>().ConvertAll<GameEntityGUID>((Unit unit) => unit.GUID).ToArray(), false);
			base.AIEntity.Empire.PlayerControllers.Server.PostOrder(order);
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				Diagnostics.Log("ELCP: {0} Merging Armies at {1} with {2}", new object[]
				{
					base.AIEntity.Empire,
					army3.WorldPosition,
					army2.WorldPosition
				});
			}
		}
	}

	public TickableState State { get; set; }

	private void Tick_ManageConvertedVillages()
	{
		MajorEmpire majorEmpire = base.AIEntity.Empire as MajorEmpire;
		if (majorEmpire == null || this.departmentOfDefense.Armies == null)
		{
			return;
		}
		Village village = null;
		for (int i = 0; i < this.convertedVillagesToManage.Count; i++)
		{
			if (this.convertedVillagesToManage[i].HasBeenConverted && this.convertedVillagesToManage[i].HasBeenConvertedByIndex == majorEmpire.Index && this.convertedVillagesToManage[i].UnitsCount != 0 && !this.convertedVillagesToManage[i].IsInEncounter)
			{
				village = this.convertedVillagesToManage[i];
				this.convertedVillagesToManage.RemoveAt(i);
				break;
			}
			this.convertedVillagesToManage.RemoveAt(i);
			i--;
		}
		if (village == null)
		{
			return;
		}
		if (this.AreaIsSave(village, 8))
		{
			int count = this.departmentOfDefense.Armies.ToList<Army>().FindAll((Army a) => !a.IsSeafaring).Count;
			int num = majorEmpire.ConvertedVillages.Count / 2 + 5;
			int num2 = (majorEmpire.ConvertedVillages.Count > 0) ? majorEmpire.ConvertedVillages[0].MaximumUnitSlot : 4;
			float num3;
			base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>().TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpireMoney, out num3, false);
			float propertyValue = base.AIEntity.Empire.GetPropertyValue(SimulationProperties.NetEmpireMoney);
			List<Unit> list = new List<Unit>();
			list.AddRange(village.Units);
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				Diagnostics.Log("ELCP: {0} managing converted Village {1} with {2} units", new object[]
				{
					base.AIEntity.Empire,
					village.WorldPosition,
					list.Count
				});
			}
			if (this.departmentOfForeignAffairs.IsInWarWithSomeone())
			{
				if (num3 + propertyValue < 100f + (float)(this.endturnService.Turn * 5) + (float)(majorEmpire.ConvertedVillages.Count * 40) && this.departmentOfScience.CanTradeUnits(false))
				{
					OrderSelloutTradableUnits order = new OrderSelloutTradableUnits(base.AIEntity.Empire.Index, list.ConvertAll<GameEntityGUID>((Unit unit) => unit.GUID).ToArray());
					Ticket ticket;
					base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, null);
				}
				if (propertyValue >= 0f || num3 > 100f)
				{
					this.CreateVillageArmy(village, list);
					return;
				}
			}
			else
			{
				if (num3 + propertyValue < 400f + (float)(this.endturnService.Turn * 5) + (float)(majorEmpire.ConvertedVillages.Count * 80) && this.departmentOfScience.CanTradeUnits(false))
				{
					OrderSelloutTradableUnits order2 = new OrderSelloutTradableUnits(base.AIEntity.Empire.Index, list.ConvertAll<GameEntityGUID>((Unit unit) => unit.GUID).ToArray());
					Ticket ticket2;
					base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order2, out ticket2, null);
					return;
				}
				if (propertyValue >= 10f && (count < num || list.Count >= num2))
				{
					this.CreateVillageArmy(village, list);
				}
			}
		}
	}

	private bool CreateCampSettlerArmy(Camp camp)
	{
		WorldPosition worldPosition = camp.WorldPosition;
		if (this.worldPositionningService.GetArmyAtPosition(worldPosition) == null)
		{
			for (int i = 0; i < camp.StandardUnits.Count; i++)
			{
				if (camp.StandardUnits[i].IsSettler && camp.StandardUnits[i].GetPropertyValue(SimulationProperties.Movement) > 0f)
				{
					OrderTransferGarrisonToNewArmy order = new OrderTransferGarrisonToNewArmy(camp.Empire.Index, camp.GUID, new GameEntityGUID[]
					{
						camp.StandardUnits[i].GUID
					}, worldPosition, StaticString.Empty, false, true, true);
					Ticket ticket;
					base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnCreateSettlerResponse));
					return true;
				}
			}
		}
		return false;
	}

	protected void OnCreateSettlerResponse(object sender, TicketRaisedEventArgs args)
	{
		if (args.Result == PostOrderResponse.Processed)
		{
			OrderTransferGarrisonToNewArmy orderTransferGarrisonToNewArmy = args.Order as OrderTransferGarrisonToNewArmy;
			IGameService service = Services.GetService<IGameService>();
			if (orderTransferGarrisonToNewArmy != null && orderTransferGarrisonToNewArmy.ArmyGuid.IsValid)
			{
				IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
				IGameEntity gameEntity;
				if (service2 != null && service2.TryGetValue(orderTransferGarrisonToNewArmy.ArmyGuid, out gameEntity))
				{
					Army army = gameEntity as Army;
					if (army != null && this.aiDataRepositoryHelper.GetAIData<AIData_Army>(army.GUID) != null)
					{
						foreach (AICommander aicommander in this.aiCommanders)
						{
							if (aicommander is AICommander_Colonization && !(aicommander as AICommander_Colonization).RegionTarget.IsRegionColonized() && aicommander.Missions.Count > 0 && !aicommander.Missions[0].AIDataArmyGUID.IsValid)
							{
								aicommander.Missions[0].AIDataArmyGUID = army.GUID;
								aicommander.Missions[0].Promote();
							}
						}
					}
				}
			}
		}
	}

	private static string registryPath = "AI/MajorEmpire/AILayer_ArmyManagement";

	private IAIDataRepositoryAIHelper aiDataRepositoryHelper;

	private Dictionary<StaticString, Func<int>> commanderLimitByType = new Dictionary<StaticString, Func<int>>();

	private Dictionary<StaticString, List<AICommanderWithObjective>> commandersByType = new Dictionary<StaticString, List<AICommanderWithObjective>>();

	private Ticket currentRetrofitOrderTicket;

	private DepartmentOfDefense departmentOfDefense;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private List<AIData_Army> freeArmies = new List<AIData_Army>();

	private IIntelligenceAIHelper intelligenceAIHelper;

	private List<GlobalObjectiveMessage> objectives = new List<GlobalObjectiveMessage>();

	private Dictionary<AICommanderMissionDefinition.AICommanderCategory, List<GlobalObjectiveMessage>> optionalMissions = new Dictionary<AICommanderMissionDefinition.AICommanderCategory, List<GlobalObjectiveMessage>>();

	private IWorldPositionningService worldPositionningService;

	private IEndTurnService endturnService;

	private IDatabase<AnimationCurve> animationCurveDatabase;

	private float pillageOpportunityModifier = 1f;

	private float empirePillageOpportunityBoost;

	private AnimationCurve pillageOpportunityBoostByTurnCurve;

	private float pillageOpportunityBoostByTurnMaximumTurn = 200f;

	private bool ignoreMP;

	private double lastTickTime;

	private AILayer_ArmyRecruitment RecruitmentLayer;

	private bool Assignjobless;

	private IPlayerRepositoryService playerRepositoryService;

	private DepartmentOfScience departmentOfScience;

	private IWorldAtlasAIHelper worldAtlasHelper;

	private List<Village> convertedVillagesToManage;
}
