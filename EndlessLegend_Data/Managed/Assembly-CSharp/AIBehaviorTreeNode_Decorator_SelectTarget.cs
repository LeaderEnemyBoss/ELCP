using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;

public class AIBehaviorTreeNode_Decorator_SelectTarget : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_SelectTarget()
	{
		this.TargetListVarName = string.Empty;
		this.TypeOfTarget = AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.Army;
		this.TypeOfDiplomaticRelation = "Any";
		this.Output_TargetVarName = string.Empty;
		this.QuestTarget = false;
	}

	[XmlAttribute]
	public bool Inverted { get; set; }

	[XmlAttribute]
	public string Output_TargetVarName { get; set; }

	[XmlAttribute]
	public string TargetListVarName { get; set; }

	[XmlAttribute]
	public string TypeOfDiplomaticRelation { get; set; }

	[XmlAttribute]
	public AIBehaviorTreeNode_Decorator_SelectTarget.TargetType TypeOfTarget { get; set; }

	[XmlAttribute]
	public string TypeOfDiplomaticRelationVariableName { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		if (this.TypeOfDiplomaticRelation == "VillageQuest" && (!(aiBehaviorTree.AICommander.Empire is MajorEmpire) || !aiBehaviorTree.AICommander.Empire.GetAgency<DepartmentOfScience>().CanParley()))
		{
			return State.Failure;
		}
		if (this.DiplomacyLayer == null && aiBehaviorTree.AICommander.Empire is MajorEmpire)
		{
			AIEntity_Empire entity = aiBehaviorTree.AICommander.AIPlayer.GetEntity<AIEntity_Empire>();
			this.DiplomacyLayer = entity.GetLayer<AILayer_Diplomacy>();
		}
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		Army army;
		State result;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) > AIArmyMission.AIArmyMissionErrorCode.None)
		{
			result = State.Failure;
		}
		else if (!aiBehaviorTree.Variables.ContainsKey(this.TargetListVarName))
		{
			result = State.Failure;
		}
		else
		{
			List<IWorldPositionable> list = aiBehaviorTree.Variables[this.TargetListVarName] as List<IWorldPositionable>;
			if (list == null)
			{
				aiBehaviorTree.ErrorCode = 10;
				result = State.Failure;
			}
			else if (list.Count == 0)
			{
				aiBehaviorTree.ErrorCode = 10;
				result = State.Failure;
			}
			else
			{
				List<IWorldPositionable> list2 = null;
				if (this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.Army)
				{
					list2 = list.FindAll((IWorldPositionable match) => match is Army);
				}
				else
				{
					if (this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.Ruin)
					{
						IQuestManagementService questManagementService = service.Game.Services.GetService<IQuestManagementService>();
						IQuestRepositoryService questRepositoryService = service.Game.Services.GetService<IQuestRepositoryService>();
						Diagnostics.Assert(questManagementService != null);
						list2 = new List<IWorldPositionable>();
						list2 = list.FindAll((IWorldPositionable match) => this.CanSearch(army, match, questManagementService));
						list2.AddRange(list.FindAll((IWorldPositionable match) => this.CanSearchQuest(army, match, questManagementService, questRepositoryService)));
					}
					if (this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.Village)
					{
						list2 = new List<IWorldPositionable>();
						for (int i = 0; i < list.Count; i++)
						{
							PointOfInterest pointOfInterest = list[i] as PointOfInterest;
							if (pointOfInterest != null && !(pointOfInterest.Type != "Village") && pointOfInterest.Region != null && pointOfInterest.Region.MinorEmpire != null)
							{
								BarbarianCouncil agency = pointOfInterest.Region.MinorEmpire.GetAgency<BarbarianCouncil>();
								if (agency != null)
								{
									Village villageAt = agency.GetVillageAt(pointOfInterest.WorldPosition);
									if (villageAt != null && !villageAt.HasBeenPacified && this.TypeOfDiplomaticRelation != "VillageConvert" && pointOfInterest.PointOfInterestImprovement != null)
									{
										list2.Add(villageAt);
									}
									else if (this.TypeOfDiplomaticRelation == "VillageConvert" && army.Empire is MajorEmpire && army.Empire.SimulationObject.Tags.Contains(AILayer_Village.TagConversionTrait) && villageAt.HasBeenPacified && DepartmentOfTheInterior.IsArmyAbleToConvert(army, true) && !villageAt.HasBeenConverted)
									{
										DepartmentOfForeignAffairs agency2 = aiBehaviorTree.AICommander.Empire.GetAgency<DepartmentOfForeignAffairs>();
										City city = villageAt.Region.City;
										if (city != null && city.Empire != aiBehaviorTree.AICommander.Empire)
										{
											DiplomaticRelation diplomaticRelation = agency2.GetDiplomaticRelation(city.Empire);
											if (diplomaticRelation == null || diplomaticRelation.State.Name != DiplomaticRelationState.Names.War || (diplomaticRelation.State.Name == DiplomaticRelationState.Names.War && pointOfInterest.PointOfInterestImprovement == null))
											{
												goto IL_433;
											}
										}
										float num;
										army.Empire.GetAgency<DepartmentOfTheTreasury>().TryGetResourceStockValue(army.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, out num, false);
										if (AILayer_Village.GetVillageConversionCost(army.Empire as MajorEmpire, villageAt) * 2f < num)
										{
											list2.Add(villageAt);
										}
									}
								}
							}
							IL_433:;
						}
					}
					else if (this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.WildKaiju)
					{
						list2 = new List<IWorldPositionable>();
						for (int j = 0; j < list.Count; j++)
						{
							Kaiju kaiju = null;
							if (list[j] is Kaiju)
							{
								kaiju = (list[j] as Kaiju);
							}
							else if (list[j] is KaijuArmy)
							{
								kaiju = (list[j] as KaijuArmy).Kaiju;
							}
							else if (list[j] is KaijuGarrison)
							{
								kaiju = (list[j] as KaijuGarrison).Kaiju;
							}
							if (kaiju != null && !kaiju.IsTamed() && kaiju.IsWild())
							{
								list2.Add(kaiju);
							}
						}
					}
					else if (this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.StunnedKaiju)
					{
						list2 = new List<IWorldPositionable>();
						for (int k = 0; k < list.Count; k++)
						{
							Kaiju kaiju2 = null;
							if (list[k] is Kaiju)
							{
								kaiju2 = (list[k] as Kaiju);
							}
							else if (list[k] is KaijuArmy)
							{
								kaiju2 = (list[k] as KaijuArmy).Kaiju;
							}
							else if (list[k] is KaijuGarrison)
							{
								kaiju2 = (list[k] as KaijuGarrison).Kaiju;
							}
							if (kaiju2 != null && !kaiju2.IsTamed() && kaiju2.IsStunned())
							{
								list2.Add(kaiju2);
							}
						}
					}
					else if (this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.TamedKaiju)
					{
						list2 = new List<IWorldPositionable>();
						for (int l = 0; l < list.Count; l++)
						{
							Kaiju kaiju3 = null;
							if (list[l] is Kaiju)
							{
								kaiju3 = (list[l] as Kaiju);
							}
							else if (list[l] is KaijuArmy)
							{
								kaiju3 = (list[l] as KaijuArmy).Kaiju;
							}
							else if (list[l] is KaijuGarrison)
							{
								kaiju3 = (list[l] as KaijuGarrison).Kaiju;
							}
							if (kaiju3 != null && kaiju3.IsTamed())
							{
								list2.Add(kaiju3);
							}
						}
					}
					else if (this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.Any)
					{
						list2 = new List<IWorldPositionable>(list);
					}
				}
				if (army.Empire is MinorEmpire || army.Empire is NavalEmpire)
				{
					for (int m = list2.Count - 1; m >= 0; m--)
					{
						Garrison garrison = list2[m] as Garrison;
						if (garrison != null && garrison.Hero != null && garrison.Hero.IsSkillUnlocked("HeroSkillLeaderMap07"))
						{
							list2.RemoveAt(m);
						}
					}
				}
				IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
				IWorldPositionningService service3 = service.Game.Services.GetService<IWorldPositionningService>();
				bool flag = false;
				if (this.TypeOfDiplomaticRelation == "VillageQuest")
				{
					flag = true;
				}
				if (!string.IsNullOrEmpty(this.TypeOfDiplomaticRelationVariableName) && aiBehaviorTree.Variables.ContainsKey(this.TypeOfDiplomaticRelationVariableName))
				{
					this.TypeOfDiplomaticRelation = (aiBehaviorTree.Variables[this.TypeOfDiplomaticRelationVariableName] as string);
				}
				DepartmentOfForeignAffairs departmentOfForeignAffairs = null;
				bool canAttack = false;
				if (this.TypeOfDiplomaticRelation == "Enemy")
				{
					departmentOfForeignAffairs = aiBehaviorTree.AICommander.Empire.GetAgency<DepartmentOfForeignAffairs>();
					canAttack = true;
				}
				else if (this.TypeOfDiplomaticRelation == "DangerForMe")
				{
					departmentOfForeignAffairs = aiBehaviorTree.AICommander.Empire.GetAgency<DepartmentOfForeignAffairs>();
					canAttack = false;
				}
				for (int n = list2.Count - 1; n >= 0; n--)
				{
					if (!AIBehaviorTreeNode_Decorator_SelectTarget.ValidateTarget(army, list2[n] as IGameEntity, departmentOfForeignAffairs, canAttack, service2, service3))
					{
						list2.RemoveAt(n);
					}
					else if (list2[n] is IGarrison && departmentOfForeignAffairs != null && this.DiplomacyLayer != null && (list2[n] as IGarrison).Empire is MajorEmpire && this.DiplomacyLayer.GetPeaceWish((list2[n] as IGarrison).Empire.Index))
					{
						if (!(list2[n] is Army) || !(list2[n] as Army).IsPrivateers)
						{
							list2.RemoveAt(n);
						}
					}
					else if (flag && list2[n] is Village && !this.ValidQuestVillage(list2[n] as Village, army))
					{
						list2.RemoveAt(n);
					}
				}
				IWorldPositionningService worldPositionService = service.Game.Services.GetService<IWorldPositionningService>();
				if (list2 != null && list2.Count != 0)
				{
					bool flag2;
					if (this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.Ruin)
					{
						flag2 = army.SimulationObject.Tags.Contains("MovementCapacitySail");
					}
					else
					{
						flag2 = army.HasSeafaringUnits();
					}
					Diagnostics.Assert(worldPositionService != null);
					if (!flag2)
					{
						list2.RemoveAll((IWorldPositionable element) => worldPositionService.IsWaterTile(element.WorldPosition));
					}
					if (army.IsSeafaring)
					{
						list2.RemoveAll((IWorldPositionable element) => !worldPositionService.IsWaterTile(element.WorldPosition));
						list2.RemoveAll((IWorldPositionable element) => worldPositionService.IsFrozenWaterTile(element.WorldPosition));
					}
				}
				if (list2 != null && list2.Count != 0)
				{
					if (list2.Count > 0)
					{
						list2.Sort((IWorldPositionable left, IWorldPositionable right) => worldPositionService.GetDistance(left.WorldPosition, army.WorldPosition).CompareTo(worldPositionService.GetDistance(right.WorldPosition, army.WorldPosition)));
					}
					if (aiBehaviorTree.Variables.ContainsKey(this.Output_TargetVarName))
					{
						aiBehaviorTree.Variables[this.Output_TargetVarName] = list2[0];
					}
					else
					{
						aiBehaviorTree.Variables.Add(this.Output_TargetVarName, list2[0]);
					}
				}
				else if (aiBehaviorTree.Variables.ContainsKey(this.Output_TargetVarName))
				{
					aiBehaviorTree.Variables.Remove(this.Output_TargetVarName);
				}
				if (this.Inverted)
				{
					if (list2 != null && list2.Count != 0)
					{
						result = State.Failure;
					}
					else
					{
						result = State.Success;
					}
				}
				else if (list2 != null && list2.Count != 0)
				{
					result = State.Success;
				}
				else
				{
					aiBehaviorTree.ErrorCode = 10;
					result = State.Failure;
				}
			}
		}
		return result;
	}

	private static bool ValidateTarget(Army myArmy, IGameEntity gameEntity, DepartmentOfForeignAffairs departmentOfForeignAffairs, bool canAttack, IGameEntityRepositoryService gameEntityRepositoryService, IWorldPositionningService worldPositionningService)
	{
		if (gameEntity == null)
		{
			return false;
		}
		if (!gameEntityRepositoryService.Contains(gameEntity.GUID))
		{
			return false;
		}
		if (departmentOfForeignAffairs != null)
		{
			IGarrison garrison = gameEntity as IGarrison;
			IWorldPositionable worldPositionable = gameEntity as IWorldPositionable;
			Region region = worldPositionningService.GetRegion(worldPositionable.WorldPosition);
			if (garrison == null || worldPositionable == null)
			{
				return false;
			}
			if (canAttack)
			{
				if (!departmentOfForeignAffairs.CanAttack(gameEntity) || garrison.Empire == myArmy.Empire)
				{
					return false;
				}
			}
			else if (!departmentOfForeignAffairs.IsEnnemy(garrison.Empire))
			{
				return false;
			}
			if ((garrison.Empire is MinorEmpire || garrison is Village) && region != null && region.IsRegionColonized() && departmentOfForeignAffairs != null && departmentOfForeignAffairs.IsEnnemy(region.Owner))
			{
				return false;
			}
		}
		Army army = gameEntity as Army;
		if (army != null)
		{
			District district = worldPositionningService.GetDistrict(army.WorldPosition);
			if (district != null && district.Type != DistrictType.Exploitation && army.Empire == district.Empire)
			{
				return false;
			}
			if (!myArmy.HasSeafaringUnits() && army.IsNaval)
			{
				return false;
			}
		}
		return true;
	}

	private bool CanSearch(Army army, IWorldPositionable item, IQuestManagementService questManagementService)
	{
		if (army.HasCatspaw)
		{
			return false;
		}
		PointOfInterest pointOfInterest = item as PointOfInterest;
		if (pointOfInterest == null)
		{
			return false;
		}
		if (pointOfInterest.Type != "QuestLocation" && pointOfInterest.Type != "NavalQuestLocation")
		{
			return false;
		}
		if (pointOfInterest.Interaction.IsLocked(army.Empire.Index, "ArmyActionSearch"))
		{
			return false;
		}
		if (pointOfInterest.CreepingNodeImprovement != null && pointOfInterest.Empire.Index != army.Empire.Index)
		{
			return false;
		}
		if ((pointOfInterest.Interaction.Bits & army.Empire.Bits) == army.Empire.Bits && !SimulationGlobal.GlobalTagsContains(SeasonManager.RuinDustDepositsTag))
		{
			return false;
		}
		if (SimulationGlobal.GlobalTagsContains(SeasonManager.RuinDustDepositsTag) && !pointOfInterest.UntappedDustDeposits && (pointOfInterest.Interaction.Bits & army.Empire.Bits) == army.Empire.Bits)
		{
			return false;
		}
		if ((pointOfInterest.Interaction.Bits & army.Empire.Bits) != 0)
		{
			foreach (QuestMarker questMarker in questManagementService.GetMarkersByBoundTargetGUID(pointOfInterest.GUID))
			{
				if (questMarker.IsVisibleFor(army.Empire))
				{
					return false;
				}
			}
			return true;
		}
		return true;
	}

	private bool ValidQuestVillage(Village village, Army army)
	{
		IGameService service = Services.GetService<IGameService>();
		bool result;
		if (service == null || service.Game == null)
		{
			result = false;
		}
		else
		{
			PointOfInterest pointOfInterest = village.PointOfInterest;
			if (pointOfInterest == null)
			{
				result = false;
			}
			else if (village.HasBeenConverted || village.HasBeenPacified || village.IsInEncounter || village.PointOfInterest.PointOfInterestImprovement == null)
			{
				result = false;
			}
			else if (pointOfInterest.Interaction.IsLocked(army.Empire.Index, "ArmyActionParley"))
			{
				result = false;
			}
			else if (pointOfInterest.SimulationObject.Tags.Contains(Village.DissentedVillage))
			{
				result = false;
			}
			else if ((pointOfInterest.Interaction.Bits & army.Empire.Bits) == army.Empire.Bits)
			{
				result = false;
			}
			else
			{
				if ((pointOfInterest.Interaction.Bits & army.Empire.Bits) != 0)
				{
					using (IEnumerator<QuestMarker> enumerator = service.Game.Services.GetService<IQuestManagementService>().GetMarkersByBoundTargetGUID(pointOfInterest.GUID).GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							if (enumerator.Current.IsVisibleFor(army.Empire))
							{
								return false;
							}
						}
					}
					return true;
				}
				result = true;
			}
		}
		return result;
	}

	private bool CanSearchQuest(Army army, IWorldPositionable item, IQuestManagementService questManagementService, IQuestRepositoryService questRepositoryService)
	{
		bool result;
		if (army.HasCatspaw)
		{
			result = false;
		}
		else
		{
			PointOfInterest pointOfInterest = item as PointOfInterest;
			if (pointOfInterest == null)
			{
				result = false;
			}
			else if (pointOfInterest.Type != "QuestLocation" && pointOfInterest.Type != "NavalQuestLocation")
			{
				result = false;
			}
			else
			{
				if (!pointOfInterest.Interaction.IsLocked(army.Empire.Index, "ArmyActionSearch"))
				{
					foreach (QuestMarker questMarker in questManagementService.GetMarkersByBoundTargetGUID(pointOfInterest.GUID))
					{
						Quest quest;
						if (questRepositoryService.TryGetValue(questMarker.QuestGUID, out quest))
						{
							QuestBehaviour questBehaviour = questRepositoryService.GetQuestBehaviour(quest.Name, army.Empire.Index);
							if (questBehaviour != null && quest.EmpireBits == army.Empire.Bits && questMarker.IsVisibleFor(army.Empire))
							{
								string a = quest.QuestDefinition.Name;
								if ((a == "GlobalQuestCompet#0001" || a == "VictoryQuest-Chapter3") && questMarker.IsVisibleInFogOfWar)
								{
									return true;
								}
								if (a == "VictoryQuest-Chapter1Alt" || a == "VictoryQuest-Chapter1")
								{
									QuestBehaviourTreeNode_ConditionCheck_Prerequisite questBehaviourTreeNode_ConditionCheck_Prerequisite;
									if (!ELCPUtilities.TryGetFirstNodeOfType<QuestBehaviourTreeNode_ConditionCheck_Prerequisite>(questBehaviour.Root as BehaviourTreeNodeController, out questBehaviourTreeNode_ConditionCheck_Prerequisite))
									{
										return false;
									}
									foreach (QuestBehaviourPrerequisites questBehaviourPrerequisites in questBehaviourTreeNode_ConditionCheck_Prerequisite.Prerequisites)
									{
										for (int j = 0; j < questBehaviourPrerequisites.Prerequisites.Length; j++)
										{
											InterpreterPrerequisite interpreterPrerequisite = questBehaviourPrerequisites.Prerequisites[j] as InterpreterPrerequisite;
											if (interpreterPrerequisite != null && !interpreterPrerequisite.Check(army))
											{
												return false;
											}
										}
									}
									return true;
								}
								else if (a == "GlobalQuestCoop#0004")
								{
									if (quest.QuestDefinition.Variables.First((QuestVariableDefinition p) => p.VarName == "$NameOfStrategicResourceToGather1") != null)
									{
										QuestBehaviourTreeNode_ConditionCheck_HasResourceAmount questBehaviourTreeNode_ConditionCheck_HasResourceAmount;
										if (!ELCPUtilities.TryGetFirstNodeOfType<QuestBehaviourTreeNode_ConditionCheck_HasResourceAmount>(questBehaviour.Root as BehaviourTreeNodeController, out questBehaviourTreeNode_ConditionCheck_HasResourceAmount))
										{
											return false;
										}
										string resourceName = questBehaviourTreeNode_ConditionCheck_HasResourceAmount.ResourceName;
										int wantedAmount = questBehaviourTreeNode_ConditionCheck_HasResourceAmount.WantedAmount;
										DepartmentOfTheTreasury agency = army.Empire.GetAgency<DepartmentOfTheTreasury>();
										float num;
										if (agency != null && agency.TryGetResourceStockValue(army.Empire.SimulationObject, resourceName, out num, false) && num >= (float)(wantedAmount * 3))
										{
											return true;
										}
									}
								}
							}
						}
					}
					return false;
				}
				result = false;
			}
		}
		return result;
	}

	[XmlAttribute]
	public bool QuestTarget { get; set; }

	private AILayer_Diplomacy DiplomacyLayer;

	public enum TargetType
	{
		Army,
		Ruin,
		Village,
		WildKaiju,
		StunnedKaiju,
		TamedKaiju,
		Any
	}
}
