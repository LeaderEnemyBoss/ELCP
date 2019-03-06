using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Extensions;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation;

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
				if (kaiju != null)
				{
					if (!kaiju.IsTamed() && kaiju.IsWild())
					{
						if (kaiju.OnArmyMode())
						{
							list2.Add(kaiju.KaijuArmy);
						}
						else if (kaiju.OnGarrisonMode())
						{
							list2.Add(kaiju.KaijuGarrison);
						}
					}
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
				if (kaiju2 != null)
				{
					if (!kaiju2.IsTamed() && kaiju2.IsStunned())
					{
						if (kaiju2.OnArmyMode())
						{
							list2.Add(kaiju2.KaijuArmy);
						}
						else if (kaiju2.OnGarrisonMode())
						{
							list2.Add(kaiju2.KaijuGarrison);
						}
					}
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
				if (kaiju3 != null)
				{
					if (kaiju3.IsTamed())
					{
						if (kaiju3.OnArmyMode())
						{
							list2.Add(kaiju3.KaijuArmy);
						}
						else if (kaiju3.OnGarrisonMode())
						{
							list2.Add(kaiju3.KaijuGarrison);
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
		}
		if (list2 != null && list2.Count != 0)
		{
			IWorldPositionningService worldPositionService = service.Game.Services.GetService<IWorldPositionningService>();
			Diagnostics.Assert(worldPositionService != null);
			bool flag;
			if (this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.Ruin)
			{
				flag = army.SimulationObject.Tags.Contains("MovementCapacitySail");
			}
			else
			{
				flag = army.IsSeafaring;
			}
			if (!flag)
			{
				list2.RemoveAll((IWorldPositionable element) => worldPositionService.IsWaterTile(element.WorldPosition));
			}
			if (army.IsSeafaring)
			{
				list2.RemoveAll((IWorldPositionable element) => !worldPositionService.IsWaterTile(element.WorldPosition));
				list2.RemoveAll((IWorldPositionable element) => worldPositionService.IsFrozenWaterTile(element.WorldPosition));
			}
			IWorldPositionable value = list2.FindLowest((IWorldPositionable element) => (float)worldPositionService.GetDistance(element.WorldPosition, army.WorldPosition), null);
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
				if (!departmentOfForeignAffairs.CanAttack(gameEntity) || garrison.Empire.Index == myArmy.Empire.Index)
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
					if (region != null && region.IsRegionColonized() && departmentOfForeignAffairs != null && departmentOfForeignAffairs.IsEnnemy(region.Owner))
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
