using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Extensions;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

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
				else if (this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.Ruin)
				{
					IQuestManagementService questManagementService = service.Game.Services.GetService<IQuestManagementService>();
					IQuestRepositoryService questRepositoryService = service.Game.Services.GetService<IQuestRepositoryService>();
					Diagnostics.Assert(questManagementService != null);
					if (!this.QuestTarget)
					{
						list2 = list.FindAll((IWorldPositionable match) => this.CanSearch(army, match, questManagementService));
					}
					if (this.QuestTarget)
					{
						list2 = list.FindAll((IWorldPositionable match) => this.CanSearchQuest(army, match, questManagementService, questRepositoryService));
					}
				}
				else if (this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.Village)
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
											goto IL_38D;
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
						IL_38D:;
					}
				}
				else if (this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.Any)
				{
					list2 = new List<IWorldPositionable>(list);
				}
				if (army.Empire is MinorEmpire || army.Empire is NavalEmpire)
				{
					for (int j = list2.Count - 1; j >= 0; j--)
					{
						Garrison garrison = list2[j] as Garrison;
						if (garrison != null && garrison.Hero != null && garrison.Hero.IsSkillUnlocked("HeroSkillLeaderMap07"))
						{
							list2.RemoveAt(j);
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
				for (int k = list2.Count - 1; k >= 0; k--)
				{
					if (!AIBehaviorTreeNode_Decorator_SelectTarget.ValidateTarget(army, list2[k] as IGameEntity, departmentOfForeignAffairs, canAttack, service2, service3))
					{
						list2.RemoveAt(k);
					}
					else if (flag && list2[k] is Village && !this.ValidQuestVillage(list2[k] as Village, army))
					{
						list2.RemoveAt(k);
					}
				}
				if (list2 != null && list2.Count != 0)
				{
					IWorldPositionningService worldPositionService = service.Game.Services.GetService<IWorldPositionningService>();
					Diagnostics.Assert(worldPositionService != null);
					bool allowWaterTile = false;
					if (this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.Ruin)
					{
						allowWaterTile = army.SimulationObject.Tags.Contains("MovementCapacitySail");
					}
					else
					{
						allowWaterTile = army.IsSeafaring;
					}
					IWorldPositionable value = list2.FindLowest((IWorldPositionable element) => (float)worldPositionService.GetDistance(element.WorldPosition, army.WorldPosition), (IWorldPositionable element) => allowWaterTile == worldPositionService.IsWaterTile(element.WorldPosition));
					if (aiBehaviorTree.Variables.ContainsKey(this.Output_TargetVarName))
					{
						aiBehaviorTree.Variables[this.Output_TargetVarName] = value;
					}
					else
					{
						aiBehaviorTree.Variables.Add(this.Output_TargetVarName, value);
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
			if (garrison == null)
			{
				return false;
			}
			if (canAttack)
			{
				if (!departmentOfForeignAffairs.CanAttack(gameEntity))
				{
					return false;
				}
			}
			else if (!departmentOfForeignAffairs.IsEnnemy(garrison.Empire))
			{
				return false;
			}
			if (garrison.Empire is MinorEmpire || garrison is Village)
			{
				IWorldPositionable worldPositionable = gameEntity as IWorldPositionable;
				if (worldPositionable != null)
				{
					Region region = worldPositionningService.GetRegion(worldPositionable.WorldPosition);
					if (region != null && region.City != null && departmentOfForeignAffairs != null && departmentOfForeignAffairs.IsEnnemy(region.City.Empire))
					{
						return false;
					}
				}
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
		if ((pointOfInterest.Interaction.Bits & army.Empire.Bits) == army.Empire.Bits)
		{
			return false;
		}
		if ((pointOfInterest.Interaction.Bits & army.Empire.Bits) != 0)
		{
			using (IEnumerator<QuestMarker> enumerator = questManagementService.GetMarkersByBoundTargetGUID(pointOfInterest.GUID).GetEnumerator())
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
			else if (!army.Empire.GetAgency<DepartmentOfScience>().CanParley())
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
						if (questRepositoryService.TryGetValue(questMarker.QuestGUID, out quest) && quest.QuestDefinition.Name == "GlobalQuestCoop#0004")
						{
							QuestBehaviour questBehaviour = questRepositoryService.GetQuestBehaviour(quest.Name, army.Empire.Index);
							if (questBehaviour != null)
							{
								if (quest.QuestDefinition.Variables.First((QuestVariableDefinition p) => p.VarName == "$NameOfStrategicResourceToGather1") != null)
								{
									QuestBehaviourTreeNode_ConditionCheck_HasResourceAmount questBehaviourTreeNode_ConditionCheck_HasResourceAmount;
									if (!this.TryGetFirstNodeOfType<QuestBehaviourTreeNode_ConditionCheck_HasResourceAmount>(questBehaviour.Root as BehaviourTreeNodeController, out questBehaviourTreeNode_ConditionCheck_HasResourceAmount))
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
					return false;
				}
				result = false;
			}
		}
		return result;
	}

	private bool TryGetFirstNodeOfType<T>(BehaviourTreeNodeController controller, out T Node)
	{
		foreach (BehaviourTreeNode behaviourTreeNode in controller.Children)
		{
			if (behaviourTreeNode is T)
			{
				Node = (T)((object)behaviourTreeNode);
				return true;
			}
			if (behaviourTreeNode is BehaviourTreeNodeController)
			{
				T t = default(T);
				if (this.TryGetFirstNodeOfType<T>(behaviourTreeNode as BehaviourTreeNodeController, out t))
				{
					Node = t;
					return true;
				}
			}
			if (behaviourTreeNode is QuestBehaviourTreeNode_Decorator_InteractWith)
			{
				foreach (QuestBehaviourTreeNode_ConditionCheck questBehaviourTreeNode_ConditionCheck in (behaviourTreeNode as QuestBehaviourTreeNode_Decorator_InteractWith).ConditionChecks)
				{
					if (questBehaviourTreeNode_ConditionCheck is T)
					{
						Node = (T)((object)questBehaviourTreeNode_ConditionCheck);
						return true;
					}
				}
			}
		}
		Node = default(T);
		return false;
	}

	[XmlAttribute]
	public bool QuestTarget { get; set; }

	public enum TargetType
	{
		Army,
		Ruin,
		Village,
		Any
	}
}
