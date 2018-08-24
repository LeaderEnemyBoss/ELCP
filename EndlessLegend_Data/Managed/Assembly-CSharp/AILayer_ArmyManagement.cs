using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AILayer_ArmyManagement : AILayerCommanderController, IXmlSerializable
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
			}
			else
			{
				reader.Skip();
			}
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
		IGameService gameService = Services.GetService<IGameService>();
		this.worldPositionningService = gameService.Game.Services.GetService<IWorldPositionningService>();
		this.intelligenceAIHelper = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		this.aiDataRepositoryHelper = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		this.departmentOfDefense = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		this.departmentOfForeignAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		base.AIEntity.RegisterPass(AIEntity.Passes.ExecuteNeeds.ToString(), "AILayer_ArmyManagement_ExecuteNeedsPass", new AIEntity.AIAction(this.ExecuteNeeds), this, new StaticString[]
		{
			"AILayerArmyRecruitment_ExecuteNeedsPass"
		});
		this.optionalMissions.Add(AICommanderMissionDefinition.AICommanderCategory.Exploration, new List<GlobalObjectiveMessage>());
		this.optionalMissions.Add(AICommanderMissionDefinition.AICommanderCategory.Patrol, new List<GlobalObjectiveMessage>());
		this.optionalMissions.Add(AICommanderMissionDefinition.AICommanderCategory.WarPatrol, new List<GlobalObjectiveMessage>());
		IPersonalityAIHelper personalityAIHelper = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		this.empirePillageOpportunityBoost = personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_ArmyManagement.registryPath, "PillageOpportunityBoost"), this.empirePillageOpportunityBoost);
		string pillageCurveName = personalityAIHelper.GetRegistryValue<string>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_ArmyManagement.registryPath, "PillageOpportunityBoostByTurn/CurveName"), string.Empty);
		if (!string.IsNullOrEmpty(pillageCurveName))
		{
			this.pillageOpportunityBoostByTurnCurve = this.animationCurveDatabase.GetValue(pillageCurveName);
		}
		this.pillageOpportunityBoostByTurnMaximumTurn = personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_ArmyManagement.registryPath, "PillageOpportunityBoostByTurn/MaximumTurn"), this.pillageOpportunityBoostByTurnMaximumTurn);
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
		this.departmentOfDefense = null;
		this.departmentOfForeignAffairs = null;
		this.departmentOfTheInterior = null;
		this.commanderLimitByType.Clear();
		this.optionalMissions.Clear();
	}

	public void UnregisterCommanderLimitDelegate(StaticString commanderType)
	{
		this.commanderLimitByType.Remove(commanderType);
	}

	protected override void ExecuteNeeds(StaticString context, StaticString pass)
	{
		base.ExecuteNeeds(context, pass);
		ISynchronousJobRepositoryAIHelper service = AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>();
		if (base.AIEntity.Empire.SimulationObject.Tags.Contains(AILayer_Village.TagConversionTrait))
		{
			service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_ManageVillageArmies));
		}
		service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_ExecuteNeeds));
		service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_ManageFullCities));
		service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_ManageFullCamps));
		this.freeArmies.Clear();
		for (int i = 0; i < this.departmentOfDefense.Armies.Count; i++)
		{
			AIData_Army aidata = this.aiDataRepositoryHelper.GetAIData<AIData_Army>(this.departmentOfDefense.Armies[i].GUID);
			if (aidata != null && aidata.CommanderMission == null && !aidata.IsSolitary && !aidata.Army.IsSeafaring && !aidata.Army.HasCatspaw)
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
							this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.WarPatrol, true);
							this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.Patrol, true);
							this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.Exploration, true);
						}
					}
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
				this.ignoreMP = false;
			}
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
				goto IL_295;
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
				goto IL_295;
			}
			IL_35E:
			j++;
			continue;
			IL_295:
			AICommanderWithObjective aicommanderWithObjective = this.GenerateCommanderByType(this.objectives[j]);
			if (aicommanderWithObjective == null)
			{
				goto IL_35E;
			}
			if (!this.commandersByType.ContainsKey(this.objectives[j].ObjectiveType))
			{
				this.commandersByType.Add(this.objectives[j].ObjectiveType, new List<AICommanderWithObjective>());
			}
			this.commandersByType[this.objectives[j].ObjectiveType].Add(aicommanderWithObjective);
			aicommanderWithObjective.GlobalPriority = this.objectives[j].GlobalPriority;
			aicommanderWithObjective.LocalPriority = this.objectives[j].LocalPriority;
			aicommanderWithObjective.GlobalPillageModifier = this.pillageOpportunityModifier;
			this.AddCommander(aicommanderWithObjective);
			goto IL_35E;
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
			if (this.departmentOfTheInterior.Cities[i].BesiegingEmpire == null)
			{
				int num = this.departmentOfTheInterior.Cities[i].MaximumUnitSlot - this.departmentOfTheInterior.Cities[i].StandardUnits.Count;
				if (num > 2)
				{
					list.Add(this.departmentOfTheInterior.Cities[i]);
				}
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
					int num2 = this.departmentOfTheInterior.Cities[k].MaximumUnitSlot - this.departmentOfTheInterior.Cities[k].StandardUnits.Count;
					if (count <= num2)
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
		if (objective.ObjectiveType == "Village")
		{
			if (objective.SubObjectifGUID != GameEntityGUID.Zero)
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
		}
		else if (objective.ObjectiveType == AICommanderMissionDefinition.AICommanderCategory.Terraformation.ToString())
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
		DepartmentOfDefense agency = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		AIData_Unit unitData;
		foreach (EvaluableMessage_RetrofitUnit evaluableMessage_RetrofitUnit in base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_RetrofitUnit>(BlackboardLayerID.Empire))
		{
			if (evaluableMessage_RetrofitUnit.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate)
			{
				if (this.aiDataRepositoryHelper.TryGetAIData<AIData_Unit>(evaluableMessage_RetrofitUnit.ElementGuid, out unitData))
				{
					if (!(unitData.Unit.UnitDesign is UnitProfile))
					{
						UnitDesign unitDesign = agency.UnitDesignDatabase.UserDefinedUnitDesigns.FirstOrDefault((UnitDesign design) => design.Model == unitData.Unit.UnitDesign.Model);
						if (unitDesign == null || unitDesign.ModelRevision <= unitData.Unit.UnitDesign.ModelRevision)
						{
							evaluableMessage_RetrofitUnit.SetFailedToObtain();
						}
						else
						{
							DepartmentOfDefense.CheckRetrofitPrerequisitesResult checkRetrofitPrerequisitesResult = agency.CheckRetrofitPrerequisites(unitData.Unit, agency.GetRetrofitCosts(unitData.Unit, unitDesign));
							if (checkRetrofitPrerequisitesResult == DepartmentOfDefense.CheckRetrofitPrerequisitesResult.Ok)
							{
								OrderRetrofitUnit order = new OrderRetrofitUnit(base.AIEntity.Empire.Index, new GameEntityGUID[]
								{
									evaluableMessage_RetrofitUnit.ElementGuid
								});
								base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out this.currentRetrofitOrderTicket, null);
								return SynchronousJobState.Running;
							}
							evaluableMessage_RetrofitUnit.SetFailedToObtain();
						}
					}
				}
				else
				{
					evaluableMessage_RetrofitUnit.SetFailedToObtain();
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
		if (majorEmpire == null)
		{
			result = SynchronousJobState.Success;
		}
		else
		{
			float num;
			base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>().TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpireMoney, out num, false);
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
						if (num + propertyValue < 100f + (float)(majorEmpire.ConvertedVillages.Count * 25) && agency.CanTradeUnits(false))
						{
							OrderSelloutTradableUnits order = new OrderSelloutTradableUnits(base.AIEntity.Empire.Index, list.ConvertAll<GameEntityGUID>((Unit unit) => unit.GUID).ToArray());
							Ticket ticket;
							base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, null);
						}
						if (propertyValue >= 0f || num > 100f)
						{
							this.CreateVillageArmy(village, list);
						}
					}
					else if (num + propertyValue < 500f + (float)(majorEmpire.ConvertedVillages.Count * 50) && agency.CanTradeUnits(false))
					{
						OrderSelloutTradableUnits order2 = new OrderSelloutTradableUnits(base.AIEntity.Empire.Index, list.ConvertAll<GameEntityGUID>((Unit unit) => unit.GUID).ToArray());
						Ticket ticket2;
						base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order2, out ticket2, null);
					}
					else if (propertyValue >= 10f)
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
						OrderTransferGarrisonToNewArmy order = new OrderTransferGarrisonToNewArmy(village.Empire.Index, village.GUID, units.ConvertAll<GameEntityGUID>((Unit unit) => unit.GUID).ToArray(), worldPosition, StaticString.Empty, false);
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
					WorldArea worldArea = new WorldArea(new WorldPosition[]
					{
						village.WorldPosition
					});
					for (int i = 0; i < size; i++)
					{
						worldArea = worldArea.Grow(this.worldPositionningService.World.WorldParameters);
					}
					foreach (WorldPosition worldPosition in worldArea.WorldPositions)
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
							if (this.departmentOfForeignAffairs.IsInWarWithSomeone())
							{
								this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.WarPatrol, true);
								this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.Patrol, true);
								this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.Exploration, true);
								return;
							}
							this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.Exploration, true);
							this.DistributeFreeArmies(AICommanderMissionDefinition.AICommanderCategory.Patrol, true);
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
		foreach (City city in this.departmentOfTheInterior.Cities)
		{
			if (this.departmentOfDefense.Armies.Count < 8 && flag && this.AreaIsSave(city.WorldPosition, 5) && city.StandardUnits.Count > 0)
			{
				this.CreateCityArmy(city, city.StandardUnits.ToList<Unit>());
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
					List<Unit> list = new List<Unit>();
					for (int k = 0; k < num5; k++)
					{
						list.Add(city.StandardUnits[k]);
					}
					this.CreateCityArmy(city, list);
				}
			}
		}
		return SynchronousJobState.Success;
	}

	private void CreateCityArmy(City city, List<Unit> units)
	{
		List<WorldPosition> availablePositionsForArmyCreation = DepartmentOfDefense.GetAvailablePositionsForArmyCreation(city);
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
						float transitionCost = service2.GetTransitionCost(city.WorldPosition, worldPosition, unit2, PathfindingFlags.IgnoreFogOfWar, null);
						if (unit2.GetPropertyValue(SimulationProperties.Movement) < transitionCost)
						{
							flag = false;
							break;
						}
					}
					if (flag)
					{
						OrderTransferGarrisonToNewArmy order = new OrderTransferGarrisonToNewArmy(city.Empire.Index, city.GUID, units.ConvertAll<GameEntityGUID>((Unit unit) => unit.GUID).ToArray(), worldPosition, StaticString.Empty, false);
						Ticket ticket;
						base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnCreateResponse));
						return;
					}
				}
			}
		}
	}

	private SynchronousJobState SynchronousJob_ManageFullCamps()
	{
		foreach (Camp camp in this.departmentOfTheInterior.Camps)
		{
			if (!camp.IsInEncounter && AILayer_Military.GetCampDefenseLocalPriority(camp, 0.8f, AICommanderMission_GarrisonCamp.SimulatedUnitsCount) == 0f && camp.StandardUnits.Count > camp.MaximumUnitSlot / 2)
			{
				List<Unit> list = new List<Unit>();
				list.AddRange(camp.StandardUnits);
				this.CreateCampArmy(camp, list);
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
				OrderTransferGarrisonToNewArmy order = new OrderTransferGarrisonToNewArmy(camp.Empire.Index, camp.GUID, units.ConvertAll<GameEntityGUID>((Unit unit) => unit.GUID).ToArray(), worldPosition, StaticString.Empty, false);
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
}
