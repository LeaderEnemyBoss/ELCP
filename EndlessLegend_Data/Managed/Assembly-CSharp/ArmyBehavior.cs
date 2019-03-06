using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.AI.SimpleBehaviorTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Game.Orders;

public abstract class ArmyBehavior : BehaviorTree<ArmyWithTask>
{
	public override void Initialize()
	{
		base.Initialize();
		this.endTurnService = Services.GetService<IEndTurnService>();
		this.gameService = Services.GetService<IGameService>();
		this.worldPositionService = this.gameService.Game.Services.GetService<IWorldPositionningService>();
		this.questManagementService = this.gameService.Game.Services.GetService<IQuestManagementService>();
		this.pathfindingService = this.gameService.Game.Services.GetService<IPathfindingService>();
	}

	protected BehaviorNodeReturnCode WaitForNextTick(ArmyWithTask army)
	{
		return BehaviorNodeReturnCode.Success;
	}

	protected BehaviorNodeReturnCode WaitForNextTurn(ArmyWithTask army)
	{
		army.BehaviorState = ArmyWithTask.ArmyBehaviorState.Sleep;
		return BehaviorNodeReturnCode.Success;
	}

	protected BehaviorNodeReturnCode Optional(ArmyWithTask army)
	{
		army.BehaviorState = ArmyWithTask.ArmyBehaviorState.Optional;
		return BehaviorNodeReturnCode.Success;
	}

	protected BehaviorNodeReturnCode ComputePathToMain(ArmyWithTask army)
	{
		if (army.MainAttackableTarget == null)
		{
			return BehaviorNodeReturnCode.Failure;
		}
		IGameEntityWithWorldPosition mainAttackableTarget = army.MainAttackableTarget;
		if (mainAttackableTarget == null)
		{
			return BehaviorNodeReturnCode.Failure;
		}
		WorldPosition validTileToAttack = this.GetValidTileToAttack(army, mainAttackableTarget);
		if (!validTileToAttack.IsValid)
		{
			return BehaviorNodeReturnCode.Failure;
		}
		army.PathToMainTarget = this.ComputePathToPosition(army, validTileToAttack, army.PathToMainTarget);
		if (army.PathToMainTarget == null)
		{
			return BehaviorNodeReturnCode.Failure;
		}
		return BehaviorNodeReturnCode.Success;
	}

	protected bool IsCloseEnoughToAttackMain(ArmyWithTask army)
	{
		return army.MainAttackableTarget != null && (army.MainAttackableTarget.UnitsCount != 0 || !(army.MainAttackableTarget is Army)) && this.IsCloseEnoughToMain(army);
	}

	protected bool IsCloseEnoughToMain(ArmyWithTask army)
	{
		if (army.MainAttackableTarget == null)
		{
			return false;
		}
		Army army2 = army.Garrison as Army;
		return army2 != null && this.IsCloseEnoughToAttack(army2, army.MainAttackableTarget);
	}

	protected bool HasEnoughActionPoint(ArmyWithTask army)
	{
		float propertyValue = army.Garrison.GetPropertyValue(SimulationProperties.MaximumNumberOfActionPoints);
		float propertyValue2 = army.Garrison.GetPropertyValue(SimulationProperties.ActionPointsSpent);
		return propertyValue > propertyValue2;
	}

	protected bool CanReachTargetThisTurn(ArmyWithTask army)
	{
		bool flag = false;
		if (army.MainAttackableTarget == null || !(army.Garrison is Army))
		{
			flag = true;
		}
		else
		{
			Army army2 = army.Garrison as Army;
			if ((float)this.worldPositionService.GetDistance(army.Garrison.WorldPosition, army.MainAttackableTarget.WorldPosition) <= army2.GetPropertyValue(SimulationProperties.Movement) + 1f)
			{
				flag = true;
			}
		}
		return army.PathToMainTarget != null && (army.PathToMainTarget.ControlPoints == null || army.PathToMainTarget.ControlPoints.Length == 0) && flag;
	}

	protected bool HasMovementLeft(ArmyWithTask army)
	{
		float propertyValue = army.Garrison.GetPropertyValue(SimulationProperties.Movement);
		return propertyValue > 0f;
	}

	protected bool HasTargetMovementLeft(ArmyWithTask army)
	{
		float propertyValue = army.MainAttackableTarget.GetPropertyValue(SimulationProperties.Movement);
		return propertyValue > 0f;
	}

	protected virtual bool IsMainTransfer(ArmyWithTask army)
	{
		float propertyValue = army.MainAttackableTarget.GetPropertyValue(SimulationProperties.Movement);
		float propertyValue2 = army.Garrison.GetPropertyValue(SimulationProperties.Movement);
		return (army.Garrison.Hero != null && army.MainAttackableTarget.Hero == null) || ((army.Garrison.Hero != null || army.MainAttackableTarget.Hero == null) && (propertyValue2 > propertyValue || (propertyValue2 >= propertyValue && (army.Garrison.CurrentUnitSlot < army.MainAttackableTarget.CurrentUnitSlot || (army.Garrison.CurrentUnitSlot <= army.MainAttackableTarget.CurrentUnitSlot && army.Garrison.GUID > army.MainAttackableTarget.GUID)))));
	}

	protected bool IsCloseEnoughToAttack(Army army, IGarrison target)
	{
		City city = target as City;
		District district = target as District;
		if (district != null)
		{
			city = district.City;
		}
		if (city != null && army.IsNaval)
		{
			District district2 = this.worldPositionService.GetDistrict(army.WorldPosition);
			return district2 != null && district2.City == city;
		}
		Fortress fortress = target as Fortress;
		if (fortress != null)
		{
			int distance = this.worldPositionService.GetDistance(army.WorldPosition, fortress.WorldPosition);
			if (distance == 1 && this.pathfindingService.IsTransitionPassable(army.WorldPosition, fortress.WorldPosition, army, OrderAttack.AttackFlags, null))
			{
				return true;
			}
			for (int i = 0; i < fortress.Facilities.Count; i++)
			{
				distance = this.worldPositionService.GetDistance(army.WorldPosition, fortress.Facilities[i].WorldPosition);
				if (distance == 1 && this.pathfindingService.IsTransitionPassable(army.WorldPosition, fortress.Facilities[i].WorldPosition, army, OrderAttack.AttackFlags, null))
				{
					return true;
				}
			}
		}
		IGameEntityWithWorldPosition gameEntityWithWorldPosition = target as IGameEntityWithWorldPosition;
		if (gameEntityWithWorldPosition != null)
		{
			int distance2 = this.worldPositionService.GetDistance(army.WorldPosition, gameEntityWithWorldPosition.WorldPosition);
			if (distance2 > 1)
			{
				return false;
			}
			if (this.worldPositionService.IsWaterTile(army.WorldPosition) != this.worldPositionService.IsWaterTile(gameEntityWithWorldPosition.WorldPosition))
			{
				return false;
			}
			if (this.pathfindingService.IsTransitionPassable(army.WorldPosition, gameEntityWithWorldPosition.WorldPosition, army, OrderAttack.AttackFlags, null))
			{
				return true;
			}
		}
		return false;
	}

	protected WorldPosition GetBestFacilitiesToAttack(ArmyWithTask army, Fortress fortress)
	{
		int num = this.worldPositionService.GetDistance(army.Garrison.WorldPosition, fortress.WorldPosition);
		WorldPosition worldPosition = fortress.WorldPosition;
		for (int i = 0; i < fortress.Facilities.Count; i++)
		{
			int distance = this.worldPositionService.GetDistance(army.Garrison.WorldPosition, fortress.Facilities[i].WorldPosition);
			if (distance < num)
			{
				num = distance;
				worldPosition = fortress.Facilities[i].WorldPosition;
			}
		}
		return worldPosition;
	}

	protected WorldPosition GetBestDistrictToAttack(ArmyWithTask army, City city)
	{
		float num = float.MaxValue;
		WorldPosition result = WorldPosition.Invalid;
		bool flag = army.Garrison.HasSeafaringUnits();
		if (flag)
		{
			for (int i = 0; i < city.Districts.Count; i++)
			{
				if (city.Districts[i].Type == DistrictType.Exploitation || city.Districts[i].Type == DistrictType.Improvement)
				{
					if (this.worldPositionService.IsOceanTile(city.Districts[i].WorldPosition))
					{
						int distance = this.worldPositionService.GetDistance(army.Garrison.WorldPosition, city.Districts[i].WorldPosition);
						if ((float)distance < num)
						{
							num = (float)distance;
							result = city.Districts[i].WorldPosition;
						}
					}
				}
			}
		}
		return result;
	}

	protected WorldPosition GetValidTileToAttack(ArmyWithTask navalArmy, IGameEntityWithWorldPosition entityWithPosition)
	{
		WorldPosition worldPosition = entityWithPosition.WorldPosition;
		Fortress fortress = entityWithPosition as Fortress;
		if (fortress != null)
		{
			worldPosition = this.GetBestFacilitiesToAttack(navalArmy, fortress);
		}
		City city = entityWithPosition as City;
		if (city != null)
		{
			return this.GetBestDistrictToAttack(navalArmy, city);
		}
		Army army = navalArmy.Garrison as Army;
		if (army == null)
		{
			return WorldPosition.Invalid;
		}
		bool flag = this.worldPositionService.IsWaterTile(worldPosition);
		WorldOrientation worldOrientation = this.worldPositionService.GetOrientation(worldPosition, army.WorldPosition);
		for (int i = 0; i < 6; i++)
		{
			WorldPosition neighbourTile = this.worldPositionService.GetNeighbourTile(worldPosition, worldOrientation, 1);
			if (neighbourTile.IsValid && flag == this.worldPositionService.IsWaterTile(neighbourTile) && this.pathfindingService.IsTransitionPassable(neighbourTile, worldPosition, army, OrderAttack.AttackFlags, null) && this.pathfindingService.IsTileStopableAndPassable(neighbourTile, army, PathfindingFlags.IgnoreFogOfWar, null))
			{
				return neighbourTile;
			}
			if (i % 2 == 0)
			{
				worldOrientation = worldOrientation.Rotate(-(i + 1));
			}
			else
			{
				worldOrientation = worldOrientation.Rotate(i + 1);
			}
		}
		return WorldPosition.Invalid;
	}

	protected WorldPath ComputePathToPosition(ArmyWithTask army, WorldPosition targetPosition, WorldPath currentPath)
	{
		if (currentPath != null && currentPath.Destination == targetPosition && currentPath.Origin == army.Garrison.WorldPosition)
		{
			for (int i = 0; i < currentPath.WorldPositions.Length; i++)
			{
				if (currentPath.WorldPositions[i] == army.Garrison.WorldPosition)
				{
					return currentPath;
				}
			}
		}
		IPathfindingContextProvider pathfindingContextProvider = army.Garrison as IPathfindingContextProvider;
		if (pathfindingContextProvider == null)
		{
			return null;
		}
		PathfindingContext pathfindingContext = pathfindingContextProvider.GenerateContext();
		pathfindingContext.Greedy = true;
		WorldPosition worldPosition = army.Garrison.WorldPosition;
		PathfindingResult pathfindingResult = this.pathfindingService.FindPath(pathfindingContext, worldPosition, targetPosition, PathfindingManager.RequestMode.Default, null, PathfindingFlags.IgnoreFogOfWar, null);
		if (pathfindingResult == null)
		{
			return null;
		}
		WorldPath worldPath = new WorldPath();
		worldPath.Build(pathfindingResult, army.Garrison.GetPropertyValue(SimulationProperties.MovementRatio), 1, false);
		return worldPath;
	}

	protected Amplitude.Unity.Game.Orders.Order Attack(ArmyWithTask army)
	{
		if (!this.IsCloseEnoughToAttackMain(army))
		{
			return null;
		}
		if (!this.HasEnoughActionPoint(army))
		{
			return null;
		}
		return new OrderAttack(army.Garrison.Empire.Index, army.Garrison.GUID, army.MainAttackableTarget.GUID);
	}

	protected Amplitude.Unity.Game.Orders.Order MoveMain(ArmyWithTask army)
	{
		if (!this.HasMovementLeft(army))
		{
			return null;
		}
		return this.FollowPath(army, army.PathToMainTarget);
	}

	protected Amplitude.Unity.Game.Orders.Order TransferShips(ArmyWithTask army)
	{
		if (army.MainAttackableTarget == null)
		{
			return null;
		}
		float num = (float)(army.MainAttackableTarget.MaximumUnitSlot - army.MainAttackableTarget.CurrentUnitSlot);
		if (num <= 0f)
		{
			return null;
		}
		if (army.Garrison.IsInEncounter || army.MainAttackableTarget.IsInEncounter)
		{
			return null;
		}
		List<GameEntityGUID> list = new List<GameEntityGUID>();
		int num2 = 0;
		while (num2 < army.Garrison.StandardUnits.Count && num > 0f)
		{
			list.Add(army.Garrison.StandardUnits[num2].GUID);
			num -= 1f;
			num2++;
		}
		return new OrderTransferUnits(army.Garrison.Empire.Index, army.Garrison.GUID, army.MainAttackableTarget.GUID, list.ToArray(), false);
	}

	protected Amplitude.Unity.Game.Orders.Order FollowPath(ArmyWithTask army, WorldPath path)
	{
		if (path == null || !path.IsValid)
		{
			return null;
		}
		int num = -1;
		for (int i = 0; i < path.WorldPositions.Length; i++)
		{
			if (path.WorldPositions[i] == army.Garrison.WorldPosition)
			{
				num = i;
				break;
			}
		}
		if (num == path.Length - 1)
		{
			return null;
		}
		if (num == -1)
		{
			return null;
		}
		IPathfindingContextProvider pathfindingContextProvider = army.Garrison as IPathfindingContextProvider;
		if (pathfindingContextProvider == null)
		{
			return null;
		}
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		IPathfindingService service2 = service.Game.Services.GetService<IPathfindingService>();
		Diagnostics.Assert(service2 != null);
		WorldPosition worldPosition = WorldPosition.Invalid;
		int num2 = 0;
		for (int j = num + 1; j < path.WorldPositions.Length; j++)
		{
			if (service2.IsTilePassable(path.WorldPositions[j], pathfindingContextProvider, (PathfindingFlags)0, null) && service2.IsTileStopable(path.WorldPositions[j], pathfindingContextProvider, (PathfindingFlags)0, null))
			{
				worldPosition = path.WorldPositions[j];
				num2++;
				if (num2 >= 2)
				{
					break;
				}
			}
		}
		if (worldPosition == WorldPosition.Invalid)
		{
			return null;
		}
		return new OrderGoTo(army.Garrison.Empire.Index, army.Garrison.GUID, worldPosition)
		{
			Flags = (PathfindingFlags)0
		};
	}

	protected Amplitude.Unity.Game.Orders.Order GotoAndAttackMain(ArmyWithTask army)
	{
		if (army.MainAttackableTarget == null)
		{
			return null;
		}
		if (!this.HasEnoughActionPoint(army))
		{
			return null;
		}
		if (!this.HasMovementLeft(army))
		{
			return null;
		}
		if (!this.CanReachTargetThisTurn(army))
		{
			return null;
		}
		return new OrderGoToAndAttack(army.Garrison.Empire.Index, army.Garrison.GUID, army.MainAttackableTarget.GUID, army.PathToMainTarget);
	}

	protected IWorldPositionningService worldPositionService;

	protected IQuestManagementService questManagementService;

	protected IPathfindingService pathfindingService;

	protected IEndTurnService endTurnService;

	protected IGameService gameService;
}
