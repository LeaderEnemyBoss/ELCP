using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.AI.SimpleBehaviorTree;
using Amplitude.Unity.Game.Orders;
using UnityEngine;

public abstract class NavyBehavior : BaseNavyBehavior
{
	public BehaviorNodeReturnCode ComputePathToSecondary(BaseNavyArmy army)
	{
		if (!this.HasSecondaryTarget(army))
		{
			return BehaviorNodeReturnCode.Failure;
		}
		NavyArmy navyArmy = army as NavyArmy;
		if (navyArmy == null)
		{
			return BehaviorNodeReturnCode.Failure;
		}
		navyArmy.PathToSecondaryTarget = base.ComputePathToPosition(army, navyArmy.SecondaryTarget.OpportunityPosition, navyArmy.PathToSecondaryTarget);
		if (navyArmy.PathToSecondaryTarget == null)
		{
			navyArmy.SecondaryTarget = null;
			return BehaviorNodeReturnCode.Failure;
		}
		return BehaviorNodeReturnCode.Success;
	}

	public BehaviorNodeReturnCode GatherOpportunities(BaseNavyArmy army)
	{
		NavyArmy navyArmy = army as NavyArmy;
		if (navyArmy == null)
		{
			return BehaviorNodeReturnCode.Failure;
		}
		navyArmy.Opportunities.Clear();
		this.ComputeOpportunityScore_PointOfInterest(army);
		this.ComputeOpportunityScore_Orbs(army);
		navyArmy.Opportunities.Sort((BehaviorOpportunity left, BehaviorOpportunity right) => -1 * left.Score.CompareTo(right.Score));
		return BehaviorNodeReturnCode.Success;
	}

	public BehaviorNodeReturnCode SelectBestOpportunity(BaseNavyArmy army)
	{
		NavyArmy navyArmy = army as NavyArmy;
		if (navyArmy == null)
		{
			return BehaviorNodeReturnCode.Failure;
		}
		float num = 0.1f;
		BehaviorOpportunity secondaryTarget = null;
		for (int i = 0; i < navyArmy.Opportunities.Count; i++)
		{
			if (navyArmy.Opportunities[i].Score > num)
			{
				num = navyArmy.Opportunities[i].Score;
				secondaryTarget = navyArmy.Opportunities[i];
			}
		}
		navyArmy.SecondaryTarget = secondaryTarget;
		if (navyArmy.SecondaryTarget == null)
		{
			return BehaviorNodeReturnCode.Failure;
		}
		return BehaviorNodeReturnCode.Success;
	}

	public BehaviorNodeReturnCode SelectSafePosition(BaseNavyArmy army)
	{
		NavyArmy navyArmy = army as NavyArmy;
		if (navyArmy == null)
		{
			return BehaviorNodeReturnCode.Failure;
		}
		Region region;
		if (navyArmy.Commander != null)
		{
			region = navyArmy.Commander.RegionData.WaterRegion;
		}
		else
		{
			region = this.worldPositionService.GetRegion(army.Garrison.WorldPosition);
		}
		int num = -1;
		float num2 = 0f;
		for (int i = 0; i < region.Borders.Length; i++)
		{
			Region region2 = this.worldPositionService.GetRegion(region.Borders[i].NeighbourRegionIndex);
			if (!region2.IsOcean)
			{
				if (region2.City == null || region2.City.Empire == army.Garrison.Empire)
				{
					float num3 = 1f;
					if (region2.City != null)
					{
						num3 += 10f;
					}
					if (num3 > num2)
					{
						num2 = num3;
						num = i;
					}
				}
			}
		}
		if (num < 0)
		{
			return BehaviorNodeReturnCode.Failure;
		}
		Region region3 = this.worldPositionService.GetRegion(region.Borders[num].NeighbourRegionIndex);
		int num4 = -1;
		float num5 = float.MaxValue;
		for (int j = 0; j < region3.Borders.Length; j++)
		{
			if (region3.Borders[j].NeighbourRegionIndex == region.Index)
			{
				for (int k = 0; k < region3.Borders[j].WorldPositions.Length; k++)
				{
					int distance = this.worldPositionService.GetDistance(army.Garrison.WorldPosition, region3.Borders[j].WorldPositions[k]);
					if ((float)distance < num5)
					{
						num5 = (float)distance;
						num4 = k;
						num = j;
					}
				}
			}
		}
		if (num4 < 0)
		{
			return BehaviorNodeReturnCode.Failure;
		}
		navyArmy.SafePosition = region3.Borders[num].WorldPositions[num4];
		if (!navyArmy.SafePosition.IsValid)
		{
			return BehaviorNodeReturnCode.Failure;
		}
		return BehaviorNodeReturnCode.Success;
	}

	public BehaviorNodeReturnCode ComputePathToSafe(BaseNavyArmy army)
	{
		if (!this.HasSafeTarget(army))
		{
			return BehaviorNodeReturnCode.Failure;
		}
		NavyArmy navyArmy = army as NavyArmy;
		if (navyArmy == null)
		{
			return BehaviorNodeReturnCode.Failure;
		}
		navyArmy.PathToSafePosition = base.ComputePathToPosition(army, navyArmy.SafePosition, navyArmy.PathToSafePosition);
		if (navyArmy.PathToSafePosition == null)
		{
			navyArmy.SafePosition = WorldPosition.Invalid;
			return BehaviorNodeReturnCode.Failure;
		}
		return BehaviorNodeReturnCode.Success;
	}

	public BehaviorNodeReturnCode ChooseRoamingPosition(BaseNavyArmy army)
	{
		WorldPosition randomRoamingPosition = this.GetRandomRoamingPosition(army, null);
		if (randomRoamingPosition.IsValid)
		{
			army.RoamingNextPosition = randomRoamingPosition;
			return BehaviorNodeReturnCode.Success;
		}
		return BehaviorNodeReturnCode.Failure;
	}

	public BehaviorNodeReturnCode ComputePathToRoaming(BaseNavyArmy army)
	{
		if (!this.HasValidRoamingPosition(army))
		{
			return BehaviorNodeReturnCode.Failure;
		}
		army.PathToRoamingPosition = base.ComputePathToPosition(army, army.RoamingNextPosition, army.PathToRoamingPosition);
		if (army.PathToRoamingPosition == null)
		{
			army.RoamingNextPosition = WorldPosition.Invalid;
			return BehaviorNodeReturnCode.Failure;
		}
		return BehaviorNodeReturnCode.Success;
	}

	private WorldPosition GetRandomRoamingPosition(BaseNavyArmy army, Func<WorldPosition, bool> whiteFilter = null)
	{
		int num = 0;
		WorldPosition result = WorldPosition.Invalid;
		int num2 = (int)army.Garrison.GUID;
		System.Random random = new System.Random(num2 + army.Commander.RegionData.WaterRegionIndex);
		Region region = this.worldPositionService.GetRegion(army.Commander.RegionData.WaterRegionIndex);
		for (int i = 0; i < region.Borders.Length; i++)
		{
			int num3 = random.Next(3, 10);
			Region.Border border = region.Borders[i];
			for (int j = 0; j < border.WorldPositions.Length; j += num3)
			{
				if (whiteFilter == null || whiteFilter(border.WorldPositions[j]))
				{
					int distance = this.worldPositionService.GetDistance(army.Garrison.WorldPosition, border.WorldPositions[j]);
					if (num < distance)
					{
						num = distance;
						result = border.WorldPositions[j];
					}
				}
			}
		}
		return result;
	}

	protected bool HasSecondaryTarget(BaseNavyArmy army)
	{
		NavyArmy navyArmy = army as NavyArmy;
		if (navyArmy == null)
		{
			return false;
		}
		if (navyArmy.SecondaryTarget == null)
		{
			return false;
		}
		if (navyArmy.SecondaryTarget.Type == BehaviorOpportunity.OpportunityType.Ruin)
		{
			PointOfInterest pointOfInterest = this.worldPositionService.GetPointOfInterest(navyArmy.SecondaryTarget.OpportunityPosition);
			if (!this.CouldSearch(army, pointOfInterest))
			{
				navyArmy.SecondaryTarget = null;
			}
		}
		else if (navyArmy.SecondaryTarget.Type == BehaviorOpportunity.OpportunityType.Orbs)
		{
			IOrbAIHelper service = AIScheduler.Services.GetService<IOrbAIHelper>();
			OrbSpawnInfo orbSpawnInfo;
			if (!service.TryGetOrbSpawnInfoAt(navyArmy.SecondaryTarget.OpportunityPosition, out orbSpawnInfo) || orbSpawnInfo.CurrentOrbCount == 0f)
			{
				navyArmy.SecondaryTarget = null;
			}
		}
		else if (navyArmy.SecondaryTarget.Score <= 0.1f)
		{
			navyArmy.SecondaryTarget = null;
		}
		return navyArmy.SecondaryTarget != null;
	}

	protected bool CouldSearch(BaseNavyArmy army, PointOfInterest pointOfInterest)
	{
		if (pointOfInterest == null)
		{
			return false;
		}
		if (pointOfInterest.Type != "QuestLocation" && pointOfInterest.Type != "NavalQuestLocation")
		{
			return false;
		}
		if (pointOfInterest.Interaction.IsLocked(army.Garrison.Empire.Index, "ArmyActionSearch"))
		{
			return false;
		}
		if ((pointOfInterest.Interaction.Bits & army.Garrison.Empire.Bits) == army.Garrison.Empire.Bits)
		{
			return false;
		}
		if ((pointOfInterest.Interaction.Bits & army.Garrison.Empire.Bits) != 0)
		{
			foreach (QuestMarker questMarker in this.questManagementService.GetMarkersByBoundTargetGUID(pointOfInterest.GUID))
			{
				if (questMarker.IsVisibleFor(army.Garrison.Empire))
				{
					return false;
				}
			}
			return true;
		}
		return true;
	}

	protected bool IsOpportunityGoodEnough(BaseNavyArmy army)
	{
		if (!this.HasSecondaryTarget(army))
		{
			return false;
		}
		NavyArmy navyArmy = army as NavyArmy;
		return navyArmy != null && this.IsDetourWorthChecking(army, navyArmy.SecondaryTarget.OpportunityPosition);
	}

	protected bool HasRuinClose(BaseNavyArmy army)
	{
		PointOfInterest pointOfInterest = this.worldPositionService.GetPointOfInterest(army.Garrison.WorldPosition);
		return pointOfInterest != null && this.CouldSearch(army, pointOfInterest);
	}

	protected bool HasReinforcementClose(BaseNavyArmy army)
	{
		NavyArmy navyArmy = army as NavyArmy;
		if (navyArmy == null || navyArmy.NavyLayer == null)
		{
			return false;
		}
		NavyTask_Reinforcement navyTask_Reinforcement = navyArmy.NavyLayer.FindTask<NavyTask_Reinforcement>((NavyTask_Reinforcement match) => match.TargetGuid == army.Garrison.GUID);
		if (navyTask_Reinforcement != null && navyTask_Reinforcement.AssignedArmy != null)
		{
			float num = (float)this.worldPositionService.GetDistance(navyTask_Reinforcement.AssignedArmy.Garrison.WorldPosition, army.Garrison.WorldPosition);
			BaseNavyArmy baseNavyArmy = navyTask_Reinforcement.AssignedArmy as BaseNavyArmy;
			float num2 = num / baseNavyArmy.GetMaximumMovement();
			if (num2 < 2f)
			{
				return true;
			}
		}
		return false;
	}

	protected bool IsSafe(BaseNavyArmy army)
	{
		return this.IsSafe(army.Garrison.Empire, army.Garrison.WorldPosition) && this.NearCommanderRegion(army, army.Garrison.WorldPosition);
	}

	protected bool HasSafeTarget(BaseNavyArmy army)
	{
		NavyArmy navyArmy = army as NavyArmy;
		return navyArmy != null && navyArmy.SafePosition.IsValid && this.IsSafe(army.Garrison.Empire, navyArmy.SafePosition) && this.NearCommanderRegion(army, navyArmy.SafePosition);
	}

	protected bool IsFortress(BaseNavyArmy army)
	{
		return army is NavyFortress;
	}

	protected bool IsMixed(BaseNavyArmy army)
	{
		return army.Garrison.HasSeafaringUnits() && !army.Garrison.HasOnlySeafaringUnits(false);
	}

	protected bool IsMainTargetUnderBombardment(BaseNavyArmy army)
	{
		City city = army.MainAttackableTarget as City;
		return city != null && city.BesiegingSeafaringArmies.Count != 0 && city.BesiegingSeafaringArmies.Exists((Army match) => match.Empire == army.Garrison.Empire);
	}

	protected bool MayEndureRetaliationAnotherTurn(BaseNavyArmy army)
	{
		Army army2 = army.Garrison as Army;
		if (army2 != null && army2.IsPrivateers)
		{
			return true;
		}
		District district = this.worldPositionService.GetDistrict(army.Garrison.WorldPosition);
		if (district != null)
		{
			if (district.City.Empire == army.Garrison.Empire)
			{
				return true;
			}
			float propertyValue = district.City.GetPropertyValue(SimulationProperties.DefensivePower);
			float propertyValue2 = district.City.GetPropertyValue(SimulationProperties.CoastalDefensivePower);
			if (propertyValue + propertyValue2 <= 0f)
			{
				return true;
			}
			DepartmentOfForeignAffairs agency = district.City.Empire.GetAgency<DepartmentOfForeignAffairs>();
			if (agency != null)
			{
				DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(army.Garrison.Empire);
				if (diplomaticRelation != null && diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.ImmuneToDefensiveImprovements))
				{
					return true;
				}
			}
			float num = (propertyValue + propertyValue2) / (float)army.Garrison.UnitsCount;
			foreach (Unit unit in army.Garrison.Units)
			{
				float propertyValue3 = unit.GetPropertyValue(SimulationProperties.Health);
				if (propertyValue3 - num < num * 2f)
				{
					return false;
				}
			}
			return true;
		}
		return true;
	}

	protected override bool IsMainTransfer(ArmyWithTask army)
	{
		NavyArmy navyArmy = army as NavyArmy;
		if (navyArmy != null)
		{
			NavyTask_Reinforcement navyTask_Reinforcement = navyArmy.NavyLayer.FindTask<NavyTask_Reinforcement>((NavyTask_Reinforcement match) => match.TargetGuid == army.Garrison.GUID);
			if (navyTask_Reinforcement == null || navyTask_Reinforcement.AssignedArmy == null || navyTask_Reinforcement.AssignedArmy.Garrison == null || navyTask_Reinforcement.AssignedArmy.Garrison.GUID != army.MainAttackableTarget.GUID)
			{
				return true;
			}
		}
		return base.IsMainTransfer(army);
	}

	protected bool HasValidRoamingPosition(BaseNavyArmy army)
	{
		return army != null && army.RoamingNextPosition.IsValid;
	}

	private bool IsSafe(Empire armyEmpire, WorldPosition position)
	{
		Region region = this.worldPositionService.GetRegion(position);
		return !region.IsOcean && (region.City == null || region.City.Empire == armyEmpire);
	}

	private bool NearCommanderRegion(BaseNavyArmy army, WorldPosition position)
	{
		BaseNavyCommander commander = army.Commander;
		if (commander == null || commander.RegionData == null)
		{
			return true;
		}
		Region positionRegion = this.worldPositionService.GetRegion(position);
		return commander.RegionData.WaterRegionIndex == positionRegion.Index || commander.RegionData.NeighbouringLandRegions.Exists((Region match) => match.Index == positionRegion.Index) || commander.RegionData.NeighbouringWaterRegions.Exists((Region match) => match.Index == positionRegion.Index);
	}

	private void ComputeOpportunityScore_PointOfInterest(BaseNavyArmy army)
	{
		NavyArmy navyArmy = army as NavyArmy;
		if (navyArmy == null)
		{
			return;
		}
		Army army2 = army.Garrison as Army;
		if (army2 != null && army2.HasCatspaw)
		{
			return;
		}
		for (int i = 0; i < this.worldPositionService.World.Regions.Length; i++)
		{
			if (this.worldPositionService.World.Regions[i].IsOcean)
			{
				for (int j = 0; j < this.worldPositionService.World.Regions[i].PointOfInterests.Length; j++)
				{
					PointOfInterest pointOfInterest = this.worldPositionService.World.Regions[i].PointOfInterests[j];
					float num = this.ComputeOpportunityTurnOverhead(army, pointOfInterest.WorldPosition);
					if ((army.CurrentMainTask == null || this.IsDetourWorthChecking(army, num)) && this.CouldSearch(army, pointOfInterest) && this.IsCloseEnoughToOrigin(army, pointOfInterest.WorldPosition, 2f))
					{
						HeuristicValue heuristicValue = new HeuristicValue(0f);
						heuristicValue.Add(1f, "constant", new object[0]);
						HeuristicValue heuristicValue2 = new HeuristicValue(0f);
						float operand = 1f;
						heuristicValue2.Add(operand, "Factor from xml(constant for now)", new object[0]);
						heuristicValue2.Multiply(num, "Nb turn added by opportunity", new object[0]);
						heuristicValue2.Add(1f, "Constant to avoid divide by 0", new object[0]);
						heuristicValue.Divide(heuristicValue2, "Distance factor", new object[0]);
						navyArmy.Opportunities.Add(new BehaviorOpportunity
						{
							OpportunityPosition = pointOfInterest.WorldPosition,
							Score = heuristicValue,
							Type = BehaviorOpportunity.OpportunityType.Ruin
						});
					}
				}
			}
		}
	}

	private void ComputeOpportunityScore_Orbs(BaseNavyArmy army)
	{
		NavyArmy navyArmy = army as NavyArmy;
		if (navyArmy == null)
		{
			return;
		}
		IOrbAIHelper service = AIScheduler.Services.GetService<IOrbAIHelper>();
		for (int i = 0; i < service.OrbSpawns.Count; i++)
		{
			OrbSpawnInfo orbSpawnInfo = service.OrbSpawns[i];
			if (orbSpawnInfo != null && orbSpawnInfo.CurrentOrbCount != 0f)
			{
				HeuristicValue heuristicValue = orbSpawnInfo.EmpireNeedModifier[army.Garrison.Empire.Index];
				if (heuristicValue > 0f)
				{
					float num = this.ComputeOpportunityTurnOverhead(army, orbSpawnInfo.WorldPosition);
					if ((army.CurrentMainTask == null || this.IsDetourWorthChecking(army, num)) && this.worldPositionService.IsOceanTile(orbSpawnInfo.WorldPosition) && this.IsCloseEnoughToOrigin(army, orbSpawnInfo.WorldPosition, 1f))
					{
						HeuristicValue heuristicValue2 = new HeuristicValue(0f);
						heuristicValue2.Add(heuristicValue, "Orb position eval", new object[0]);
						float orbDistanceExponent = service.GetOrbDistanceExponent(army.Garrison.Empire);
						HeuristicValue heuristicValue3 = new HeuristicValue(0f);
						heuristicValue3.Add(num, "Nb turn added by opportunity", new object[0]);
						heuristicValue3.Power(orbDistanceExponent, "From xml registry", new object[0]);
						heuristicValue3.Add(1f, "avoid divide by 0", new object[0]);
						heuristicValue2.Divide(heuristicValue3, "DistanceFactor", new object[0]);
						navyArmy.Opportunities.Add(new BehaviorOpportunity
						{
							OpportunityPosition = orbSpawnInfo.WorldPosition,
							Score = heuristicValue2,
							Type = BehaviorOpportunity.OpportunityType.Orbs
						});
					}
				}
			}
		}
	}

	private float ComputeOpportunityTurnOverhead(BaseNavyArmy army, WorldPosition opportunityPosition)
	{
		float propertyValue = army.Garrison.GetPropertyValue(SimulationProperties.MaximumMovement);
		if (army.MainAttackableTarget == null || army.PathToMainTarget == null)
		{
			float num = (float)this.worldPositionService.GetDistance(opportunityPosition, army.Garrison.WorldPosition);
			return num / propertyValue;
		}
		float num2 = (float)army.PathToMainTarget.WorldPositions.Length;
		float num3 = (float)this.worldPositionService.GetDistance(opportunityPosition, army.PathToMainTarget.Destination);
		float num4 = (float)this.worldPositionService.GetDistance(opportunityPosition, army.Garrison.WorldPosition);
		float num5 = num4 / propertyValue;
		if (num2 < num5)
		{
			return float.MaxValue;
		}
		return num4 + num3 - num2;
	}

	private bool IsDetourWorthChecking(BaseNavyArmy army, WorldPosition opportunityPosition)
	{
		float detourTime = this.ComputeOpportunityTurnOverhead(army, opportunityPosition);
		return this.IsDetourWorthChecking(army, detourTime);
	}

	private bool IsDetourWorthChecking(BaseNavyArmy army, float detourTime)
	{
		float num = 2f;
		if (army.CurrentMainTask != null && army.PathToMainTarget != null)
		{
			float num2 = (float)army.PathToMainTarget.WorldPositions.Length / army.Garrison.GetPropertyValue(SimulationProperties.MaximumMovement);
			num = Mathf.Min(new float[]
			{
				num2,
				num,
				army.CurrentMainTask.EstimatedTurnEnd - (float)(this.gameService.Game as Game).Turn
			});
		}
		return detourTime <= 0f || detourTime <= num;
	}

	private bool IsCloseEnoughToOrigin(BaseNavyArmy army, WorldPosition opportunityPosition)
	{
		float propertyValue = army.Garrison.GetPropertyValue(SimulationProperties.MaximumMovement);
		float num;
		if (army.Commander == null)
		{
			num = (float)this.worldPositionService.GetDistance(opportunityPosition, army.Garrison.WorldPosition);
		}
		else
		{
			num = (float)this.worldPositionService.GetDistance(opportunityPosition, army.Commander.RegionData.WaterRegion.Barycenter);
		}
		return num / propertyValue <= 2f;
	}

	public Amplitude.Unity.Game.Orders.Order MoveSecondary(BaseNavyArmy army)
	{
		if (!base.HasMovementLeft(army))
		{
			return null;
		}
		NavyArmy navyArmy = army as NavyArmy;
		if (navyArmy == null)
		{
			return null;
		}
		return base.FollowPath(army, navyArmy.PathToSecondaryTarget);
	}

	public Amplitude.Unity.Game.Orders.Order Search(BaseNavyArmy army)
	{
		PointOfInterest pointOfInterest = this.worldPositionService.GetPointOfInterest(army.Garrison.WorldPosition);
		if (pointOfInterest != null && this.CouldSearch(army, pointOfInterest))
		{
			OrderInteractWith orderInteractWith = new OrderInteractWith(army.Garrison.Empire.Index, army.Garrison.GUID, FleetAction_Dive.ReadOnlyName);
			orderInteractWith.WorldPosition = army.Garrison.WorldPosition;
			orderInteractWith.Tags.AddTag("NavalInteract");
			orderInteractWith.TargetGUID = pointOfInterest.GUID;
			orderInteractWith.ArmyActionName = FleetAction_Dive.ReadOnlyName;
			return orderInteractWith;
		}
		return null;
	}

	public Amplitude.Unity.Game.Orders.Order MoveToSafe(BaseNavyArmy army)
	{
		if (!base.HasMovementLeft(army))
		{
			return null;
		}
		NavyArmy navyArmy = army as NavyArmy;
		if (navyArmy == null)
		{
			return null;
		}
		return base.FollowPath(army, navyArmy.PathToSafePosition);
	}

	public Amplitude.Unity.Game.Orders.Order MoveToRoaming(BaseNavyArmy army)
	{
		if (!base.HasMovementLeft(army))
		{
			return null;
		}
		return base.FollowPath(army, army.PathToRoamingPosition);
	}

	public Amplitude.Unity.Game.Orders.Order CreateArmyFromFortress(BaseNavyArmy army)
	{
		if (!this.IsFortress(army))
		{
			return null;
		}
		WorldPosition armyPosition = WorldPosition.Invalid;
		WorldOrientation orientation = WorldOrientation.East;
		if (army.MainAttackableTarget != null)
		{
			orientation = this.worldPositionService.GetOrientation(army.Garrison.WorldPosition, army.MainAttackableTarget.WorldPosition);
		}
		Fortress fortress = army.Garrison as Fortress;
		for (int i = 0; i < 6; i++)
		{
			WorldPosition neighbourTile = this.worldPositionService.GetNeighbourTile(fortress.WorldPosition, orientation.Rotate(i), 1);
			if (DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(neighbourTile, PathfindingMovementCapacity.Water))
			{
				armyPosition = neighbourTile;
				break;
			}
		}
		if (!armyPosition.IsValid)
		{
			for (int j = 0; j < fortress.Facilities.Count; j++)
			{
				for (int k = 0; k < 6; k++)
				{
					WorldPosition neighbourTile2 = this.worldPositionService.GetNeighbourTile(fortress.Facilities[j].WorldPosition, (WorldOrientation)k, 1);
					if (DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(neighbourTile2, PathfindingMovementCapacity.Water))
					{
						armyPosition = neighbourTile2;
						break;
					}
				}
			}
		}
		GameEntityGUID[] array = new GameEntityGUID[army.Garrison.StandardUnits.Count];
		for (int l = 0; l < army.Garrison.StandardUnits.Count; l++)
		{
			array[l] = army.Garrison.StandardUnits[l].GUID;
		}
		return new OrderTransferGarrisonToNewArmy(army.Garrison.Empire.Index, army.Garrison.GUID, array, armyPosition, null, false, true, true);
	}

	public Amplitude.Unity.Game.Orders.Order CreateArmyFromMixedArmy(BaseNavyArmy army)
	{
		if (!this.IsMixed(army))
		{
			return null;
		}
		if (!base.HasMovementLeft(army))
		{
			return null;
		}
		WorldPosition armyPosition = WorldPosition.Invalid;
		PathfindingMovementCapacity movementCapacity = PathfindingMovementCapacity.Water;
		for (int i = 0; i < 6; i++)
		{
			WorldPosition neighbourTile = this.worldPositionService.GetNeighbourTile(army.Garrison.WorldPosition, (WorldOrientation)i, 1);
			if (DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(neighbourTile, movementCapacity))
			{
				armyPosition = neighbourTile;
				break;
			}
		}
		bool flag = false;
		List<GameEntityGUID> list = new List<GameEntityGUID>();
		for (int j = 0; j < army.Garrison.StandardUnits.Count; j++)
		{
			if (flag == army.Garrison.StandardUnits[j].IsSeafaring)
			{
				list.Add(army.Garrison.StandardUnits[j].GUID);
			}
		}
		return new OrderTransferGarrisonToNewArmy(army.Garrison.Empire.Index, army.Garrison.GUID, list.ToArray(), armyPosition, null, false, true, true);
	}

	public Amplitude.Unity.Game.Orders.Order Bombard(BaseNavyArmy army)
	{
		if (army.MainAttackableTarget == null)
		{
			return null;
		}
		return new OrderToggleNavalSiege(army.Garrison.Empire.Index, army.MainAttackableTarget.GUID, army.Garrison.GUID, true);
	}

	protected BehaviorNode<BaseNavyArmy> CreateArmyFromFortressSequence()
	{
		Condition<BaseNavyArmy> condition = new Condition<BaseNavyArmy>(new Func<BaseNavyArmy, bool>(this.IsFortress));
		OrderAction<BaseNavyArmy> orderAction = new OrderAction<BaseNavyArmy>(new Func<BaseNavyArmy, Amplitude.Unity.Game.Orders.Order>(this.CreateArmyFromFortress));
		return new Sequence<BaseNavyArmy>(new BehaviorNode<BaseNavyArmy>[]
		{
			condition,
			orderAction
		});
	}

	protected BehaviorNode<BaseNavyArmy> SearchRuinSequence()
	{
		Condition<BaseNavyArmy> condition = new Condition<BaseNavyArmy>(new Func<BaseNavyArmy, bool>(this.HasRuinClose));
		OrderAction<BaseNavyArmy> orderAction = new OrderAction<BaseNavyArmy>(new Func<BaseNavyArmy, Amplitude.Unity.Game.Orders.Order>(this.Search));
		return new Sequence<BaseNavyArmy>(new BehaviorNode<BaseNavyArmy>[]
		{
			condition,
			orderAction
		});
	}

	protected BehaviorNode<BaseNavyArmy> MoveMainWithOpportunity()
	{
		Condition<BaseNavyArmy> condition = new Condition<BaseNavyArmy>(new Func<BaseNavyArmy, bool>(base.CanReachTargetThisTurn));
		Condition<BaseNavyArmy> condition2 = new Condition<BaseNavyArmy>(new Func<BaseNavyArmy, bool>(base.HasMovementLeft));
		Amplitude.Unity.AI.SimpleBehaviorTree.Action<BaseNavyArmy> action = new Amplitude.Unity.AI.SimpleBehaviorTree.Action<BaseNavyArmy>(new Func<BaseNavyArmy, BehaviorNodeReturnCode>(base.ComputePathToMain));
		OrderAction<BaseNavyArmy> orderAction = new OrderAction<BaseNavyArmy>(new Func<BaseNavyArmy, Amplitude.Unity.Game.Orders.Order>(base.MoveMain));
		BehaviorNode<BaseNavyArmy> behaviorNode = this.OpportunitySequence();
		BehaviorNode<BaseNavyArmy> behaviorNode2 = this.OpportunityAttackSequence();
		Sequence<BaseNavyArmy> sequence = new Sequence<BaseNavyArmy>(new BehaviorNode<BaseNavyArmy>[]
		{
			condition,
			orderAction
		});
		Selector<BaseNavyArmy> selector = new Selector<BaseNavyArmy>(new BehaviorNode<BaseNavyArmy>[]
		{
			sequence,
			behaviorNode2,
			behaviorNode,
			orderAction
		});
		Selector<BaseNavyArmy> selector2 = new Selector<BaseNavyArmy>(new BehaviorNode<BaseNavyArmy>[]
		{
			condition2,
			behaviorNode2
		});
		return new Sequence<BaseNavyArmy>(new BehaviorNode<BaseNavyArmy>[]
		{
			selector2,
			action,
			selector
		});
	}

	protected BehaviorNode<BaseNavyArmy> OpportunitySequence()
	{
		Condition<BaseNavyArmy> condition = new Condition<BaseNavyArmy>(new Func<BaseNavyArmy, bool>(this.HasSecondaryTarget));
		Condition<BaseNavyArmy> condition2 = new Condition<BaseNavyArmy>(new Func<BaseNavyArmy, bool>(this.IsOpportunityGoodEnough));
		Amplitude.Unity.AI.SimpleBehaviorTree.Action<BaseNavyArmy> action = new Amplitude.Unity.AI.SimpleBehaviorTree.Action<BaseNavyArmy>(new Func<BaseNavyArmy, BehaviorNodeReturnCode>(this.GatherOpportunities));
		Amplitude.Unity.AI.SimpleBehaviorTree.Action<BaseNavyArmy> action2 = new Amplitude.Unity.AI.SimpleBehaviorTree.Action<BaseNavyArmy>(new Func<BaseNavyArmy, BehaviorNodeReturnCode>(this.SelectBestOpportunity));
		Amplitude.Unity.AI.SimpleBehaviorTree.Action<BaseNavyArmy> action3 = new Amplitude.Unity.AI.SimpleBehaviorTree.Action<BaseNavyArmy>(new Func<BaseNavyArmy, BehaviorNodeReturnCode>(this.ComputePathToSecondary));
		OrderAction<BaseNavyArmy> orderAction = new OrderAction<BaseNavyArmy>(new Func<BaseNavyArmy, Amplitude.Unity.Game.Orders.Order>(this.MoveSecondary));
		Sequence<BaseNavyArmy> sequence = new Sequence<BaseNavyArmy>(new BehaviorNode<BaseNavyArmy>[]
		{
			condition,
			condition2
		});
		Sequence<BaseNavyArmy> sequence2 = new Sequence<BaseNavyArmy>(new BehaviorNode<BaseNavyArmy>[]
		{
			action,
			action2,
			condition2
		});
		Selector<BaseNavyArmy> selector = new Selector<BaseNavyArmy>(new BehaviorNode<BaseNavyArmy>[]
		{
			sequence,
			sequence2
		});
		return new Sequence<BaseNavyArmy>(new BehaviorNode<BaseNavyArmy>[]
		{
			selector,
			action3,
			orderAction
		});
	}

	protected BehaviorNode<BaseNavyArmy> OpportunityAttackSequence()
	{
		Condition<BaseNavyArmy> condition = new Condition<BaseNavyArmy>(new Func<BaseNavyArmy, bool>(base.HasEnoughActionPoint));
		Amplitude.Unity.AI.SimpleBehaviorTree.Action<BaseNavyArmy> action = new Amplitude.Unity.AI.SimpleBehaviorTree.Action<BaseNavyArmy>(new Func<BaseNavyArmy, BehaviorNodeReturnCode>(this.CollectOpportunityArmies));
		OrderAction<BaseNavyArmy> orderAction = new OrderAction<BaseNavyArmy>(new Func<BaseNavyArmy, Amplitude.Unity.Game.Orders.Order>(this.GotoAndAttackOpportunity));
		return new Sequence<BaseNavyArmy>(new BehaviorNode<BaseNavyArmy>[]
		{
			condition,
			action,
			orderAction
		});
	}

	protected BehaviorNodeReturnCode CollectOpportunityArmies(BaseNavyArmy army)
	{
		Army navy = army.Garrison as Army;
		NavyArmy navyArmy = army as NavyArmy;
		if (navyArmy == null || navy == null || !(navy.Empire is MajorEmpire))
		{
			return BehaviorNodeReturnCode.Failure;
		}
		float num = navy.GetPropertyValue(SimulationProperties.Movement);
		if (num < 0.01f)
		{
			num = 1f;
		}
		List<IGarrison> list = new List<IGarrison>();
		DepartmentOfForeignAffairs agency = navy.Empire.GetAgency<DepartmentOfForeignAffairs>();
		AILayer_Military.HasSaveAttackableTargetsNearby(navy, Mathf.CeilToInt(num), agency, out list, true);
		if (list.Count == 0)
		{
			return BehaviorNodeReturnCode.Failure;
		}
		list.Sort((IGarrison left, IGarrison right) => this.worldPositionService.GetDistance((left as IWorldPositionable).WorldPosition, navy.WorldPosition).CompareTo(this.worldPositionService.GetDistance((right as IWorldPositionable).WorldPosition, navy.WorldPosition)));
		foreach (IGarrison garrison in list)
		{
			IGameEntityWithWorldPosition gameEntityWithWorldPosition = garrison as IGameEntityWithWorldPosition;
			IGarrisonWithPosition garrisonWithPosition = garrison as IGarrisonWithPosition;
			if (gameEntityWithWorldPosition != null && garrisonWithPosition != null)
			{
				WorldPosition validTileToAttack = base.GetValidTileToAttack(army, gameEntityWithWorldPosition);
				navyArmy.PathToSecondaryTarget = base.ComputePathToPosition(army, validTileToAttack, navyArmy.PathToSecondaryTarget);
				if (navyArmy.PathToSecondaryTarget != null)
				{
					if (navyArmy.PathToSecondaryTarget.ControlPoints != null && navyArmy.PathToSecondaryTarget.ControlPoints.Length != 0)
					{
						return BehaviorNodeReturnCode.Failure;
					}
					Diagnostics.Log("ELCP {0}/{1} found opportunitytarget {2} with path {3}", new object[]
					{
						navy.Empire,
						navy.LocalizedName,
						garrison.LocalizedName,
						navyArmy.PathToSecondaryTarget
					});
					navyArmy.OpportunityAttackableTarget = garrisonWithPosition;
					return BehaviorNodeReturnCode.Success;
				}
			}
		}
		return BehaviorNodeReturnCode.Failure;
	}

	protected Amplitude.Unity.Game.Orders.Order GotoAndAttackOpportunity(ArmyWithTask army)
	{
		NavyArmy navyArmy = army as NavyArmy;
		if (navyArmy == null || navyArmy.OpportunityAttackableTarget == null)
		{
			return null;
		}
		Army army2 = navyArmy.Garrison as Army;
		Diagnostics.Log("ELCP {0}/{1} succesfull GotoAndAttackOpportunity", new object[]
		{
			army2.Empire,
			army2.LocalizedName
		});
		return new OrderGoToAndAttack(army.Garrison.Empire.Index, army.Garrison.GUID, navyArmy.OpportunityAttackableTarget.GUID, navyArmy.PathToSecondaryTarget);
	}

	private bool IsCloseEnoughToOrigin(BaseNavyArmy army, WorldPosition opportunityPosition, float maxturns = 2f)
	{
		float propertyValue = army.Garrison.GetPropertyValue(SimulationProperties.MaximumMovement);
		return (float)this.worldPositionService.GetDistance(opportunityPosition, army.Garrison.WorldPosition) / propertyValue <= maxturns;
	}

	protected BehaviorNodeReturnCode InvalidateBehavior(ArmyWithTask army)
	{
		BaseNavyTask baseNavyTask = army.CurrentMainTask as BaseNavyTask;
		if (baseNavyTask == null || army.Garrison == null)
		{
			return BehaviorNodeReturnCode.Failure;
		}
		if (army.MainAttackableTarget != null && army.MainAttackableTarget.IsInEncounter)
		{
			return BehaviorNodeReturnCode.Failure;
		}
		baseNavyTask.ForbiddenGUIDs.Add(army.Garrison.GUID);
		army.BehaviorState = ArmyWithTask.ArmyBehaviorState.Succeed;
		return BehaviorNodeReturnCode.Success;
	}
}
