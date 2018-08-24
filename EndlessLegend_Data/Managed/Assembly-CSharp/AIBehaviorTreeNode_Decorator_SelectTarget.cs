using System;
using System.Collections.Generic;
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
		AIArmyMission.AIArmyMissionErrorCode armyUnlessLocked = base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army);
		if (armyUnlessLocked != AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		if (!aiBehaviorTree.Variables.ContainsKey(this.TargetListVarName))
		{
			return State.Failure;
		}
		List<IWorldPositionable> list = aiBehaviorTree.Variables[this.TargetListVarName] as List<IWorldPositionable>;
		if (list == null)
		{
			aiBehaviorTree.ErrorCode = 10;
			return State.Failure;
		}
		if (list.Count == 0)
		{
			aiBehaviorTree.ErrorCode = 10;
			return State.Failure;
		}
		List<IWorldPositionable> list2 = null;
		if (this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.Army)
		{
			list2 = list.FindAll((IWorldPositionable match) => match is Army);
		}
		else if (this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.Ruin)
		{
			IQuestManagementService questManagementService = service.Game.Services.GetService<IQuestManagementService>();
			Diagnostics.Assert(questManagementService != null);
			list2 = list.FindAll((IWorldPositionable match) => this.CanSearch(army, match, questManagementService));
		}
		else if (this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.Village)
		{
			list2 = new List<IWorldPositionable>();
			for (int i = 0; i < list.Count; i++)
			{
				PointOfInterest pointOfInterest = list[i] as PointOfInterest;
				if (pointOfInterest != null)
				{
					if (pointOfInterest.PointOfInterestImprovement != null)
					{
						if (!(pointOfInterest.Type != "Village"))
						{
							if (pointOfInterest.Region != null && pointOfInterest.Region.MinorEmpire != null)
							{
								BarbarianCouncil agency = pointOfInterest.Region.MinorEmpire.GetAgency<BarbarianCouncil>();
								if (agency != null)
								{
									Village villageAt = agency.GetVillageAt(pointOfInterest.WorldPosition);
									if (villageAt != null && !villageAt.HasBeenPacified)
									{
										list2.Add(villageAt);
									}
								}
							}
						}
					}
				}
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
				return State.Failure;
			}
			return State.Success;
		}
		else
		{
			if (list2 != null && list2.Count != 0)
			{
				return State.Success;
			}
			aiBehaviorTree.ErrorCode = 10;
			return State.Failure;
		}
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

	public enum TargetType
	{
		Army,
		Ruin,
		Village,
		Any
	}
}
