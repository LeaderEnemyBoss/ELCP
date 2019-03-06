using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Debug;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Utilities.Maps;

public class AIBehaviorTreeNode_Decorator_GetTargetPosition : AIBehaviorTreeNode_Decorator
{
	[XmlAttribute]
	public string Output_DestinationVarName { get; set; }

	[XmlAttribute]
	public string TargetVarName { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) != AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		if (!aiBehaviorTree.Variables.ContainsKey(this.TargetVarName))
		{
			aiBehaviorTree.LogError("$Target not set {0}/{1}", new object[]
			{
				army.Empire,
				army.LocalizedName
			});
			return State.Failure;
		}
		IWorldPositionable worldPositionable = aiBehaviorTree.Variables[this.TargetVarName] as IWorldPositionable;
		if (worldPositionable == null)
		{
			aiBehaviorTree.ErrorCode = 10;
			return State.Failure;
		}
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(service2 != null);
		IPathfindingService service3 = service.Game.Services.GetService<IPathfindingService>();
		Diagnostics.Assert(service3 != null);
		WorldPosition worldPosition;
		if (worldPositionable is City)
		{
			City city = worldPositionable as City;
			if (army.Empire != city.Empire)
			{
				worldPosition = this.NewDistrictToAttackCity(army, city);
			}
			else
			{
				District nearestDistrictToReinforce = this.GetNearestDistrictToReinforce(army, city);
				if (nearestDistrictToReinforce == null)
				{
					aiBehaviorTree.ErrorCode = 2;
					return State.Failure;
				}
				worldPosition = nearestDistrictToReinforce.WorldPosition;
			}
		}
		else if (worldPositionable is Camp)
		{
			worldPosition = this.GetValidTileToAttack(service3, service2, worldPositionable.WorldPosition, army);
		}
		else if (worldPositionable is Village)
		{
			worldPosition = this.GetValidTileToAttack(service3, service2, worldPositionable.WorldPosition, army);
		}
		else if (worldPositionable is OrbSpawnInfo)
		{
			worldPosition = worldPositionable.WorldPosition;
		}
		else if (worldPositionable is MapBoostSpawnInfo)
		{
			worldPosition = worldPositionable.WorldPosition;
		}
		else if (worldPositionable is PointOfInterest)
		{
			worldPosition = this.GetValidTileToAttack(service3, service2, worldPositionable.WorldPosition, army);
		}
		else if (worldPositionable is Kaiju)
		{
			worldPosition = this.GetValidTileToAttack(service3, service2, worldPositionable.WorldPosition, army);
		}
		else if (worldPositionable is Army)
		{
			worldPosition = this.GetValidTileToAttack(service3, service2, worldPositionable.WorldPosition, army);
		}
		else if (service2.IsWaterTile(army.WorldPosition) != service2.IsWaterTile(worldPositionable.WorldPosition))
		{
			worldPosition = this.GetValidTileToAttack(service3, service2, worldPositionable.WorldPosition, army);
		}
		else
		{
			worldPosition = worldPositionable.WorldPosition;
		}
		if (worldPosition == WorldPosition.Invalid)
		{
			aiBehaviorTree.ErrorCode = 11;
			return State.Failure;
		}
		if (aiBehaviorTree.Variables.ContainsKey(this.Output_DestinationVarName))
		{
			aiBehaviorTree.Variables[this.Output_DestinationVarName] = worldPosition;
		}
		else
		{
			aiBehaviorTree.Variables.Add(this.Output_DestinationVarName, worldPosition);
		}
		return State.Success;
	}

	private bool CanIAttackTheCityFromTheDistrict(District district, GridMap<District> districtsMap, Army army, IPathfindingService pathfindingService, IWorldPositionningService worldPositionningService)
	{
		if (district == null)
		{
			return false;
		}
		if (worldPositionningService.IsWaterTile(district.WorldPosition))
		{
			return false;
		}
		using (new UnityProfilerSample("IsTileStopable"))
		{
			if (!pathfindingService.IsTileStopableAndPassable(district.WorldPosition, army, PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnoreZoneOfControl | PathfindingFlags.IgnoreSieges, null))
			{
				return false;
			}
		}
		for (int i = 0; i < 6; i++)
		{
			WorldPosition neighbourTile = worldPositionningService.GetNeighbourTile(district.WorldPosition, (WorldOrientation)i, 1);
			if (!worldPositionningService.IsWaterTile(neighbourTile))
			{
				District value = districtsMap.GetValue(neighbourTile);
				if (value != null && District.IsACityTile(value))
				{
					using (new UnityProfilerSample("IsTransitionPassable"))
					{
						if (pathfindingService.IsTransitionPassable(district.WorldPosition, neighbourTile, army, OrderAttack.AttackFlags, null))
						{
							return true;
						}
					}
				}
			}
		}
		return false;
	}

	private District GetClosestDistrictToAttackCity(City city, GridMap<District> districtsMap, Army army, IPathfindingService pathfindingService, IWorldPositionningService worldPositionningService)
	{
		float num = 2.14748365E+09f;
		District district = null;
		for (int i = 0; i < city.Districts.Count; i++)
		{
			if (city.Districts[i].Type == DistrictType.Exploitation && this.CanIAttackTheCityFromTheDistrict(city.Districts[i], districtsMap, army, pathfindingService, worldPositionningService))
			{
				int distance = worldPositionningService.GetDistance(army.WorldPosition, city.Districts[i].WorldPosition);
				if ((float)distance < num)
				{
					num = (float)distance;
					district = city.Districts[i];
				}
			}
		}
		if (district != null)
		{
			return district;
		}
		return city.Districts[0];
	}

	private WorldPosition NewDistrictToAttackCity(Army army, City city)
	{
		AIBehaviorTreeNode_Decorator_GetTargetPosition.<NewDistrictToAttackCity>c__AnonStorey795 <NewDistrictToAttackCity>c__AnonStorey = new AIBehaviorTreeNode_Decorator_GetTargetPosition.<NewDistrictToAttackCity>c__AnonStorey795();
		<NewDistrictToAttackCity>c__AnonStorey.city = city;
		<NewDistrictToAttackCity>c__AnonStorey.army = army;
		<NewDistrictToAttackCity>c__AnonStorey.<>f__this = this;
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		IWorldPositionningService worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(worldPositionningService != null);
		IPathfindingService pathfindingService = service.Game.Services.GetService<IPathfindingService>();
		Diagnostics.Assert(pathfindingService != null);
		PathfindingContext pathfindingContext2 = <NewDistrictToAttackCity>c__AnonStorey.army.GenerateContext();
		pathfindingContext2.RemoveMovementCapacity(PathfindingMovementCapacity.Air);
		pathfindingContext2.Greedy = true;
		GridMap<District> districtsMap = worldPositionningService.World.Atlas.GetMap(WorldAtlas.Maps.Districts) as GridMap<District>;
		Diagnostics.Assert(districtsMap != null);
		District value = districtsMap.GetValue(<NewDistrictToAttackCity>c__AnonStorey.army.WorldPosition);
		if (value != null && value.City == <NewDistrictToAttackCity>c__AnonStorey.city && this.CanIAttackTheCityFromTheDistrict(value, districtsMap, <NewDistrictToAttackCity>c__AnonStorey.army, pathfindingService, worldPositionningService))
		{
			return <NewDistrictToAttackCity>c__AnonStorey.army.WorldPosition;
		}
		float num = (float)worldPositionningService.GetDistance(<NewDistrictToAttackCity>c__AnonStorey.army.WorldPosition, <NewDistrictToAttackCity>c__AnonStorey.city.WorldPosition);
		float propertyValue = <NewDistrictToAttackCity>c__AnonStorey.army.GetPropertyValue(SimulationProperties.MaximumMovement);
		if (num > propertyValue * 2f)
		{
			PathfindingResult pathfindingResult = pathfindingService.FindPath(pathfindingContext2, <NewDistrictToAttackCity>c__AnonStorey.army.WorldPosition, <NewDistrictToAttackCity>c__AnonStorey.city.WorldPosition, PathfindingManager.RequestMode.Default, null, OrderAttack.AttackFlags, null);
			if (pathfindingResult != null)
			{
				WorldPosition result = <NewDistrictToAttackCity>c__AnonStorey.army.WorldPosition;
				foreach (WorldPosition worldPosition in pathfindingResult.GetCompletePath())
				{
					result = worldPosition;
					value = districtsMap.GetValue(worldPosition);
					if (value != null && value.City == <NewDistrictToAttackCity>c__AnonStorey.city)
					{
						if (value.Type == DistrictType.Exploitation)
						{
							return value.WorldPosition;
						}
						if (District.IsACityTile(value))
						{
							return result;
						}
					}
				}
			}
		}
		else
		{
			PathfindingResult pathfindingResult2 = pathfindingService.FindLocation(pathfindingContext2, <NewDistrictToAttackCity>c__AnonStorey.army.WorldPosition, delegate(WorldPosition start, WorldPosition goal, PathfindingContext pathfindingContext, PathfindingWorldContext worldContext, WorldPosition evaluatedPosition, PathfindingFlags flags)
			{
				District value2 = districtsMap.GetValue(evaluatedPosition);
				return value2 != null && value2.City == <NewDistrictToAttackCity>c__AnonStorey.city && !District.IsACityTile(value2) && this.CanIAttackTheCityFromTheDistrict(value2, districtsMap, <NewDistrictToAttackCity>c__AnonStorey.army, pathfindingService, worldPositionningService);
			}, null, PathfindingFlags.IgnoreFogOfWar);
			if (pathfindingResult2 != null)
			{
				return pathfindingResult2.Goal;
			}
		}
		return WorldPosition.Invalid;
	}

	private District GetNearestDistrictToReinforce(Army army, City city)
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(service2 != null);
		IPathfindingService service3 = service.Game.Services.GetService<IPathfindingService>();
		Diagnostics.Assert(service3 != null);
		GridMap<District> gridMap = service2.World.Atlas.GetMap(WorldAtlas.Maps.Districts) as GridMap<District>;
		Diagnostics.Assert(gridMap != null);
		District value = gridMap.GetValue(army.WorldPosition);
		if (value != null && value.City == city && District.IsACityTile(value))
		{
			return value;
		}
		int num = int.MaxValue;
		District district = null;
		for (int i = 0; i < city.Districts.Count; i++)
		{
			if (District.IsACityTile(city.Districts[i]) && service2.GetArmyAtPosition(city.Districts[i].WorldPosition) == null)
			{
				int distance = service2.GetDistance(army.WorldPosition, city.Districts[i].WorldPosition);
				if (distance < num)
				{
					num = distance;
					district = city.Districts[i];
				}
			}
		}
		if (district == null)
		{
			return null;
		}
		PathfindingResult pathfindingResult = service3.FindPath(army, army.WorldPosition, district.WorldPosition, PathfindingManager.RequestMode.Default, null, OrderAttack.AttackFlags, null);
		if (pathfindingResult == null)
		{
			return null;
		}
		District result = null;
		foreach (WorldPosition worldPosition in pathfindingResult.GetCompletePathReverse())
		{
			value = gridMap.GetValue(worldPosition);
			if (value != null && District.IsACityTile(value))
			{
				if (service2.GetArmyAtPosition(value.WorldPosition) == null)
				{
					return value;
				}
				result = value;
			}
			else if (value == null)
			{
				break;
			}
		}
		return result;
	}

	private IEnumerable<WorldPosition> GetPotentialAttackPositions(Army attacker, City target)
	{
		for (int index = 0; index < target.Districts.Count; index++)
		{
			District district = target.Districts[index];
			if (district.Type != DistrictType.Exploitation)
			{
			}
		}
		yield break;
	}

	private IEnumerable<WorldPosition> GetPotentialAttackPositions(Army attacker, Army target)
	{
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		IWorldPositionningService worldPositionningService = gameService.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(worldPositionningService != null);
		IPathfindingService pathfindingService = gameService.Game.Services.GetService<IPathfindingService>();
		Diagnostics.Assert(pathfindingService != null);
		WorldPosition targetPosition = target.WorldPosition;
		bool targetOnWater = worldPositionningService.IsWaterTile(target.WorldPosition);
		int parity = (int)(targetPosition.Row % 2);
		for (int orientation = 0; orientation < 6; orientation++)
		{
			int[] neighborOffset = WorldPosition.NeighborOffsets[parity][orientation];
			WorldPosition neighbourTile = WorldPosition.GetValidPosition(new WorldPosition((int)targetPosition.Row + neighborOffset[0], (int)targetPosition.Column + neighborOffset[1]), worldPositionningService.World.WorldParameters);
			if (neighbourTile.IsValid)
			{
				if (worldPositionningService.IsWaterTile(neighbourTile) == targetOnWater)
				{
					if (pathfindingService.IsTransitionPassable(neighbourTile, targetPosition, attacker, OrderAttack.AttackFlags, null) && pathfindingService.IsTileStopableAndPassable(neighbourTile, attacker, PathfindingFlags.IgnoreFogOfWar, null))
					{
						yield return neighbourTile;
					}
				}
			}
		}
		yield break;
	}

	private WorldPosition GetValidTileToAttack(IPathfindingService pathfindingService, IWorldPositionningService worldPositionningService, WorldPosition maintAttackedPosition, Army army)
	{
		District district = worldPositionningService.GetDistrict(army.WorldPosition);
		bool flag = worldPositionningService.IsWaterTile(maintAttackedPosition);
		WorldOrientation worldOrientation = worldPositionningService.GetOrientation(maintAttackedPosition, army.WorldPosition);
		for (int i = 0; i < 6; i++)
		{
			WorldPosition neighbourTile = worldPositionningService.GetNeighbourTile(maintAttackedPosition, worldOrientation, 1);
			if (worldPositionningService.IsWaterTile(neighbourTile) == flag && pathfindingService.IsTransitionPassable(neighbourTile, maintAttackedPosition, army, OrderAttack.AttackFlags, null) && pathfindingService.IsTileStopableAndPassable(neighbourTile, army, PathfindingFlags.IgnoreFogOfWar, null))
			{
				District district2 = worldPositionningService.GetDistrict(neighbourTile);
				if (district2 != null && district2.City.Empire.Index == army.Empire.Index && district2.City.BesiegingEmpire != null && District.IsACityTile(district2))
				{
					if (district != null && District.IsACityTile(district) && district.City.GUID == district2.City.GUID)
					{
						return neighbourTile;
					}
				}
				else
				{
					if (district == null || !District.IsACityTile(district) || district.City.BesiegingEmpire == null)
					{
						return neighbourTile;
					}
					if (district2 != null && District.IsACityTile(district2) && district.City.GUID == district2.City.GUID)
					{
						return neighbourTile;
					}
				}
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
}
