using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Amplitude;
using Amplitude.Test;
using Amplitude.Threading;
using Amplitude.Unity.Debug;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Simulation;
using Amplitude.Utilities.Maps;
using UnityEngine;

[Diagnostics.TagAttribute("UnitTests")]
[Diagnostics.TagAttribute("Pathfinding")]
public class PathfindingManager : GameAncillary, IService, IPathfindingService
{
	public event EventHandler<EventArgs> PathfindingServiceReady;

	public float GetMaximumMovementPoints(WorldPosition position, IPathfindingContextProvider pathfindingContextProvider, PathfindingFlags flags = (PathfindingFlags)0)
	{
		PathfindingContext pathfindingContext = pathfindingContextProvider.GenerateContext();
		if ((flags & PathfindingFlags.IgnoreFogOfWar) == (PathfindingFlags)0 && pathfindingContext.Empire != null && !this.visibilityService.IsWorldPositionExploredFor(position, pathfindingContext.Empire))
		{
			return pathfindingContext.CurrentMaximumMovementPoints;
		}
		PathfindingMovementCapacity tileMovementCapacity = this.GetTileMovementCapacity(position, flags);
		if (tileMovementCapacity == PathfindingMovementCapacity.Water)
		{
			return pathfindingContext.MaximumMovementPointsOnWater;
		}
		return pathfindingContext.MaximumMovementPointsOnLand;
	}

	public float GetTransitionCost(WorldPosition start, WorldPosition goal, IPathfindingContextProvider pathfindingContextProvider, PathfindingFlags flags = (PathfindingFlags)0, PathfindingWorldContext worldContext = null)
	{
		if (WorldPosition.GetDistance(start, goal, this.world.WorldParameters.IsCyclicWorld, this.world.WorldParameters.Columns) != 1)
		{
			return float.PositiveInfinity;
		}
		PathfindingContext pathfindingContext = pathfindingContextProvider.GenerateContext();
		global::Empire empire = pathfindingContext.Empire;
		DepartmentOfForeignAffairs departmentOfForeignAffairs = (empire == null) ? null : empire.GetAgency<DepartmentOfForeignAffairs>();
		if ((flags & PathfindingFlags.IgnoreFogOfWar) == (PathfindingFlags)0 && empire != null && !this.visibilityService.IsWorldPositionExploredFor(start, empire) && !this.visibilityService.IsWorldPositionExploredFor(goal, empire))
		{
			return 1.5f;
		}
		if (pathfindingContext.CurrentMovementRatio <= 0f && !this.IsTileStopable(start, pathfindingContextProvider, flags, worldContext))
		{
			return float.PositiveInfinity;
		}
		if (worldContext != null && worldContext.RegionIndexList != null && worldContext.RegionIndexList.Count > 0)
		{
			bool flag = worldContext.RegionIndexListType == PathfindingWorldContext.RegionListType.RegionBlackList;
			short value = this.regionIndexMap.GetValue(goal);
			int count = worldContext.RegionIndexList.Count;
			for (int i = 0; i < count; i++)
			{
				if ((int)value == worldContext.RegionIndexList[i])
				{
					flag = (worldContext.RegionIndexListType != PathfindingWorldContext.RegionListType.RegionBlackList);
					break;
				}
			}
			if (!flag)
			{
				return float.PositiveInfinity;
			}
		}
		if ((flags & PathfindingFlags.IgnoreArmies) == (PathfindingFlags)0 && empire != null && goal != pathfindingContext.Goal && (this.visibilityService.IsWorldPositionVisibleFor(goal, empire) || (flags & PathfindingFlags.IgnoreFogOfWar) == PathfindingFlags.IgnoreFogOfWar))
		{
			Army army = this.armiesMap.GetValue(goal);
			if ((flags & PathfindingFlags.IgnoreCamouflagedArmies) != (PathfindingFlags)0 && army != null && army.IsCamouflaged && !this.visibilityService.IsWorldPositionDetectedFor(goal, empire))
			{
				army = null;
			}
			if (army != null)
			{
				global::Empire empire2 = army.Empire;
				bool flag2;
				if (empire2.Index == empire.Index)
				{
					flag2 = true;
				}
				else if (departmentOfForeignAffairs != null && empire2 is MajorEmpire)
				{
					DiplomaticRelation diplomaticRelation = departmentOfForeignAffairs.GetDiplomaticRelation(empire2);
					flag2 = diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.PassThroughArmies);
				}
				else
				{
					flag2 = false;
				}
				if (!flag2)
				{
					return float.PositiveInfinity;
				}
			}
		}
		if ((flags & PathfindingFlags.IgnoreOtherEmpireDistrict) == (PathfindingFlags)0)
		{
			District value2 = this.districtsMap.GetValue(goal);
			if (value2 != null && value2.Type != DistrictType.Exploitation)
			{
				if (!(empire is MajorEmpire))
				{
					return float.PositiveInfinity;
				}
				if (pathfindingContext.IsPrivateers && value2.City != null && value2.City.Empire != null && value2.City.Empire.Index != empire.Index)
				{
					return float.PositiveInfinity;
				}
			}
		}
		if ((flags & PathfindingFlags.IgnoreEncounterAreas) == (PathfindingFlags)0 && empire != null && ((flags & PathfindingFlags.IgnoreFogOfWar) == PathfindingFlags.IgnoreFogOfWar || this.visibilityService.IsWorldPositionVisibleFor(goal, empire)))
		{
			Encounter value3 = this.encountersMap.GetValue(goal);
			if (value3 != null && value3.EncounterState != EncounterState.BattleHasEnded)
			{
				return float.PositiveInfinity;
			}
		}
		if ((flags & PathfindingFlags.IgnoreDiplomacy) == (PathfindingFlags)0 && empire != null && departmentOfForeignAffairs != null)
		{
			bool flag3 = departmentOfForeignAffairs.CanMoveOn(start, pathfindingContext.IsPrivateers, pathfindingContext.IsCamouflaged);
			if (!flag3)
			{
				flag3 = ((flags & PathfindingFlags.IgnoreFogOfWar) == (PathfindingFlags)0 && !this.visibilityService.IsWorldPositionExploredFor(start, empire));
			}
			if (flag3)
			{
				if (!departmentOfForeignAffairs.CanMoveOn(goal, pathfindingContext.IsPrivateers, pathfindingContext.IsCamouflaged))
				{
					return float.PositiveInfinity;
				}
			}
		}
		if ((flags & PathfindingFlags.IgnoreSieges) == (PathfindingFlags)0 && empire != null)
		{
			District value4 = this.districtsMap.GetValue(goal);
			District value5 = this.districtsMap.GetValue(start);
			bool flag4 = value4 != null && value4.City.BesiegingEmpireIndex != -1 && value4.Type != DistrictType.Exploitation;
			bool flag5 = value5 != null && value5.City.BesiegingEmpireIndex != -1 && value5.Type != DistrictType.Exploitation;
			if (flag5 != flag4)
			{
				return float.PositiveInfinity;
			}
		}
		float num = 0f;
		if ((flags & PathfindingFlags.IgnoreZoneOfControl) == (PathfindingFlags)0)
		{
			bool flag6 = this.IsInZoneOfControl(goal, empire, flags, worldContext);
			if (flag6)
			{
				num += this.zoneOfControlMovementPointMalus;
			}
		}
		int currentTileHeigh = (int)this.heightMap.GetValue(goal);
		float num2 = 0f;
		if ((flags & PathfindingFlags.IgnoreDistrict) == (PathfindingFlags)0)
		{
			bool flag7 = false;
			bool flag8 = false;
			if (empire != null)
			{
				District value6 = this.districtsMap.GetValue(start);
				if (value6 != null && value6.Empire != null && value6.Type != DistrictType.Exploitation)
				{
					if (value6.Empire.Index == empire.Index)
					{
						flag7 = true;
					}
					else if (departmentOfForeignAffairs != null)
					{
						DiplomaticRelation diplomaticRelation2 = departmentOfForeignAffairs.GetDiplomaticRelation(value6.Empire);
						Diagnostics.Assert(diplomaticRelation2 != null);
						flag7 = diplomaticRelation2.HasActiveAbility(DiplomaticAbilityDefinition.PassThroughCities);
					}
				}
				if (flag7)
				{
					District value7 = this.districtsMap.GetValue(goal);
					if (value7 != null && value7.Empire != null && value7.Type != DistrictType.Exploitation)
					{
						if (value7.Empire.Index == empire.Index)
						{
							flag8 = true;
						}
						else if (departmentOfForeignAffairs != null)
						{
							DiplomaticRelation diplomaticRelation3 = departmentOfForeignAffairs.GetDiplomaticRelation(value7.Empire);
							Diagnostics.Assert(diplomaticRelation3 != null);
							flag8 = diplomaticRelation3.HasActiveAbility(DiplomaticAbilityDefinition.PassThroughCities);
						}
					}
				}
			}
			bool flag9 = flag7 && flag8;
			if (flag9)
			{
				if (this.districtTileSpecification.IsTerrainCostOverrided(pathfindingContext.MovementCapacities))
				{
					return Mathf.Max(num + this.districtTileSpecification.GetCost(pathfindingContext.MovementCapacities, currentTileHeigh), this.minimumTransitionCost);
				}
				num2 += this.districtTileSpecification.GetCost(pathfindingContext.MovementCapacities, currentTileHeigh);
			}
		}
		num2 += this.GetTransitionCost(start, goal, pathfindingContext.MovementCapacities, flags);
		if (worldContext != null)
		{
			PathfindingRule additionalRule = worldContext.GetTileContext(goal).AdditionalRule;
			if (additionalRule != null)
			{
				num2 += additionalRule.GetCost(pathfindingContext.MovementCapacities, currentTileHeigh);
			}
		}
		if (float.IsPositiveInfinity(num2))
		{
			return float.PositiveInfinity;
		}
		if ((flags & PathfindingFlags.IgnoreRoad) == (PathfindingFlags)0 && empire != null && empire is MajorEmpire && departmentOfForeignAffairs != null)
		{
			bool flag10 = false;
			int count2 = departmentOfForeignAffairs.DiplomaticRelations.Count;
			int num3 = 0;
			while (num3 < count2 && !flag10)
			{
				if (empire.Index == num3)
				{
					flag10 = (((int)this.cadasterMap.GetValue(goal) & empire.Bits) != 0);
				}
				else
				{
					DiplomaticRelation diplomaticRelation4 = departmentOfForeignAffairs.DiplomaticRelations[num3];
					int num4 = 1 << num3;
					flag10 = (((int)this.cadasterMap.GetValue(goal) & num4) != 0 && diplomaticRelation4.HasActiveAbility(DiplomaticAbilityDefinition.ShareRoads));
				}
				num3++;
			}
			if (flag10)
			{
				DepartmentOfScience agency = empire.GetAgency<DepartmentOfScience>();
				bool flag11 = agency.HasResearchTag(PathfindingManager.HighwayTagName);
				if (flag11)
				{
					if (this.highwayTileSpecification.IsTerrainCostOverrided(pathfindingContext.MovementCapacities))
					{
						return Mathf.Max(num + this.highwayTileSpecification.GetCost(pathfindingContext.MovementCapacities, currentTileHeigh), this.minimumTransitionCost);
					}
					num2 += this.highwayTileSpecification.GetCost(pathfindingContext.MovementCapacities, currentTileHeigh);
				}
				else
				{
					if (this.roadTileSpecification.IsTerrainCostOverrided(pathfindingContext.MovementCapacities))
					{
						return Mathf.Max(num + this.roadTileSpecification.GetCost(pathfindingContext.MovementCapacities, currentTileHeigh), this.minimumTransitionCost);
					}
					num2 += this.roadTileSpecification.GetCost(pathfindingContext.MovementCapacities, currentTileHeigh);
				}
			}
		}
		num2 += num;
		List<float> worldPositionMovementCostModifiers = this.worldEffectService.GetWorldPositionMovementCostModifiers(goal);
		if (worldPositionMovementCostModifiers != null)
		{
			for (int j = 0; j < worldPositionMovementCostModifiers.Count; j++)
			{
				num2 += num2 * worldPositionMovementCostModifiers[j];
			}
		}
		if ((flags & PathfindingFlags.IgnoreCoast) == (PathfindingFlags)0 && !pathfindingContext.CanFreeEmbark)
		{
			PathfindingMovementCapacity tileMovementCapacity = this.GetTileMovementCapacity(start, flags);
			PathfindingMovementCapacity tileMovementCapacity2 = this.GetTileMovementCapacity(goal, flags);
			if ((tileMovementCapacity == PathfindingMovementCapacity.Ground || tileMovementCapacity == PathfindingMovementCapacity.FrozenWater) && tileMovementCapacity2 == PathfindingMovementCapacity.Water)
			{
				float maximumMovementPoints = this.GetMaximumMovementPoints(goal, pathfindingContext, flags);
				num2 = Mathf.Max(num2, pathfindingContext.CurrentMovementRatio * maximumMovementPoints);
			}
			if (tileMovementCapacity == PathfindingMovementCapacity.Water && (tileMovementCapacity2 == PathfindingMovementCapacity.Ground || tileMovementCapacity2 == PathfindingMovementCapacity.FrozenWater))
			{
				float maximumMovementPoints2 = this.GetMaximumMovementPoints(goal, pathfindingContext, flags);
				num2 = Mathf.Max(num2, pathfindingContext.CurrentMovementRatio * maximumMovementPoints2);
			}
		}
		return Mathf.Max(num2, this.minimumTransitionCost);
	}

	public float GetTransitionCost(WorldPosition start, WorldPosition goal, PathfindingMovementCapacity movementCapacity, PathfindingFlags flags = (PathfindingFlags)0)
	{
		Diagnostics.Assert(this.world != null);
		Diagnostics.Assert(this.terrainTypeMap != null);
		Diagnostics.Assert(this.heightMap != null);
		Diagnostics.Assert(this.ridgeMap != null);
		if ((flags & PathfindingFlags.IgnoreMovementCapacities) == PathfindingFlags.IgnoreMovementCapacities)
		{
			movementCapacity = PathfindingMovementCapacity.All;
		}
		start = WorldPosition.GetValidPosition(start, this.world.WorldParameters);
		goal = WorldPosition.GetValidPosition(goal, this.world.WorldParameters);
		Diagnostics.Assert(start.IsValid && goal.IsValid);
		if (WorldPosition.GetDistance(start, goal, this.world.WorldParameters.IsCyclicWorld, this.world.WorldParameters.Columns) != 1)
		{
			return float.PositiveInfinity;
		}
		if ((movementCapacity & PathfindingMovementCapacity.All) == PathfindingMovementCapacity.All)
		{
			return 1f;
		}
		Region region = this.world.Regions[(int)this.regionIndexMap.GetValue(goal)];
		Region region2 = this.world.Regions[(int)this.regionIndexMap.GetValue(start)];
		if (region.Index != region2.Index && region.IsWasteland)
		{
			return float.PositiveInfinity;
		}
		float num = 0f;
		Diagnostics.Assert(this.heightMap != null);
		int num2 = (int)this.heightMap.GetValue(goal);
		int value = (int)this.heightMap.GetValue(start) - num2;
		if (Math.Abs(value) > 1 && (movementCapacity & PathfindingMovementCapacity.Air) == PathfindingMovementCapacity.None)
		{
			return float.PositiveInfinity;
		}
		Diagnostics.Assert(this.ridgeMap != null && this.ridgeSpecification != null);
		if (this.ridgeMap.GetValue(goal))
		{
			float cost = this.ridgeSpecification.GetCost(movementCapacity, num2);
			if (float.IsPositiveInfinity(cost))
			{
				return float.PositiveInfinity;
			}
			num += cost;
		}
		Diagnostics.Assert(this.terrainSpecifications != null);
		byte value2 = this.terrainTypeMap.GetValue(goal);
		num += this.terrainSpecifications[(short)value2].GetCost(movementCapacity, num2);
		if (SimulationGlobal.GlobalTagsContains(DownloadableContent13.FrozenTile) && this.worldPositionningService.IsFrozenWaterTile(goal))
		{
			if ((movementCapacity & PathfindingMovementCapacity.Ground) == PathfindingMovementCapacity.None)
			{
				return float.PositiveInfinity;
			}
			num = this.frozenWaterTileSpecification.GetCost(movementCapacity, num2);
		}
		if (region.IsOcean && !this.worldPositionningService.IsFrozenWaterTile(goal))
		{
			this.weatherService.OverridePathfindingCost(goal, movementCapacity, ref num);
		}
		if (float.IsInfinity(num))
		{
			return num;
		}
		if (SimulationGlobal.GlobalTagsContains(Season.ReadOnlyHeatWave) && this.worldPositionningService.IsForestTile(goal))
		{
			if (this.forestSpecification.IsTerrainCostOverrided(movementCapacity))
			{
				return this.forestSpecification.GetCost(movementCapacity, num2);
			}
			num += this.forestSpecification.GetCost(movementCapacity, num2);
		}
		if ((flags & PathfindingFlags.IgnorePOI) == (PathfindingFlags)0)
		{
			Diagnostics.Assert(this.pointOfInterestMap != null);
			PointOfInterest value3 = this.pointOfInterestMap.GetValue(goal);
			if (value3 != null)
			{
				if (value3.Type == PathfindingManager.POITypeVillage)
				{
					Diagnostics.Assert(this.poiTypeVillageSpecification != null);
					if (this.poiTypeVillageSpecification.IsTerrainCostOverrided(movementCapacity))
					{
						return this.poiTypeVillageSpecification.GetCost(movementCapacity, num2);
					}
					num += this.poiTypeVillageSpecification.GetCost(movementCapacity, num2);
				}
				else if (value3.Type == PathfindingManager.POITypeCitadel)
				{
					Diagnostics.Assert(this.poiTypeCitadelSpecification != null);
					if (this.poiTypeCitadelSpecification.IsTerrainCostOverrided(movementCapacity))
					{
						return this.poiTypeCitadelSpecification.GetCost(movementCapacity, num2);
					}
					num += this.poiTypeCitadelSpecification.GetCost(movementCapacity, num2);
				}
				else if (value3.Type == PathfindingManager.POITypeFacility)
				{
					Diagnostics.Assert(this.poiTypeFacilitySpecification != null);
					if (this.poiTypeFacilitySpecification.IsTerrainCostOverrided(movementCapacity))
					{
						return this.poiTypeFacilitySpecification.GetCost(movementCapacity, num2);
					}
					num += this.poiTypeFacilitySpecification.GetCost(movementCapacity, num2);
				}
				else if (value3.Type == PathfindingManager.POITypeQuestLocation)
				{
					Diagnostics.Assert(this.poiTypeQuestLocationSpecification != null);
					if (this.poiTypeQuestLocationSpecification.IsTerrainCostOverrided(movementCapacity))
					{
						return this.poiTypeQuestLocationSpecification.GetCost(movementCapacity, num2);
					}
					num += this.poiTypeQuestLocationSpecification.GetCost(movementCapacity, num2);
				}
				else if (value3.Type == PathfindingManager.POITypeResourceDeposit)
				{
					Diagnostics.Assert(this.poiTypeResourceDepositSpecification != null);
					if (this.poiTypeResourceDepositSpecification.IsTerrainCostOverrided(movementCapacity))
					{
						return this.poiTypeResourceDepositSpecification.GetCost(movementCapacity, num2);
					}
					num += this.poiTypeResourceDepositSpecification.GetCost(movementCapacity, num2);
				}
				else if (value3.Type == PathfindingManager.POITypeWatchTower)
				{
					Diagnostics.Assert(this.poiTypeWatchTowerSpecification != null);
					if (this.poiTypeWatchTowerSpecification.IsTerrainCostOverrided(movementCapacity))
					{
						return this.poiTypeWatchTowerSpecification.GetCost(movementCapacity, num2);
					}
					num += this.poiTypeWatchTowerSpecification.GetCost(movementCapacity, num2);
				}
				else if (value3.Type == PathfindingManager.POITypeNavalQuestLocation)
				{
					Diagnostics.Assert(this.poiTypeNavalQuestLocationSpecification != null);
					if (this.poiTypeNavalQuestLocationSpecification.IsTerrainCostOverrided(movementCapacity))
					{
						return this.poiTypeNavalQuestLocationSpecification.GetCost(movementCapacity, num2);
					}
					num += this.poiTypeNavalQuestLocationSpecification.GetCost(movementCapacity, num2);
				}
			}
		}
		Diagnostics.Assert(this.riverIndexMap != null && this.riverSpecification != null);
		short value4 = this.riverIndexMap.GetValue(goal);
		if (value4 >= 0 && this.worldPositionningService != null && this.worldPositionningService.World != null)
		{
			WorldRiver river = this.worldPositionningService.GetRiver(this.riverIndexMap.GetValue(goal));
			if (this.riverSpecification != null && river.RiverTypeName == WorldRiver.NormalRiverTypeName)
			{
				if (this.riverSpecification.IsTerrainCostOverrided(movementCapacity))
				{
					return this.riverSpecification.GetCost(movementCapacity, num2);
				}
				num += this.riverSpecification.GetCost(movementCapacity, num2);
			}
			else if (this.lavaRiverSpecification != null && river.RiverTypeName == WorldRiver.LavaRiverTypeName)
			{
				if (this.lavaRiverSpecification.IsTerrainCostOverrided(movementCapacity))
				{
					return this.lavaRiverSpecification.GetCost(movementCapacity, num2);
				}
				num += this.lavaRiverSpecification.GetCost(movementCapacity, num2);
			}
		}
		Diagnostics.Assert(!float.IsNaN(num));
		return num;
	}

	public static bool CanFreeEmbark(global::Empire empire)
	{
		return empire.SimulationObject.Tags.Contains("FactionTraitSeaDemons1");
	}

	public bool IsTilePassable(WorldPosition tilePosition, IPathfindingContextProvider pathfindingContextProvider, PathfindingFlags flags = (PathfindingFlags)0, PathfindingWorldContext worldContext = null)
	{
		PathfindingContext pathfindingContext = pathfindingContextProvider.GenerateContext();
		global::Empire empire = pathfindingContext.Empire;
		if ((flags & PathfindingFlags.IgnoreFogOfWar) == (PathfindingFlags)0 && empire != null && !this.visibilityService.IsWorldPositionExploredFor(tilePosition, empire))
		{
			return true;
		}
		if ((flags & PathfindingFlags.IgnoreArmies) == (PathfindingFlags)0 && empire != null && tilePosition != pathfindingContext.Goal && (this.visibilityService.IsWorldPositionVisibleFor(tilePosition, empire) || (flags & PathfindingFlags.IgnoreFogOfWar) == (PathfindingFlags)0))
		{
			Army value = this.armiesMap.GetValue(tilePosition);
			if (value != null && value.Empire != null)
			{
				bool flag;
				if (value.Empire.Index == empire.Index)
				{
					flag = true;
				}
				else
				{
					DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
					if (agency != null && value.Empire is MajorEmpire)
					{
						DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(value.Empire);
						flag = diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.PassThroughArmies);
					}
					else
					{
						flag = false;
					}
				}
				if (!flag)
				{
					return false;
				}
			}
		}
		if ((flags & PathfindingFlags.IgnoreOtherEmpireDistrict) == (PathfindingFlags)0)
		{
			District value2 = this.districtsMap.GetValue(tilePosition);
			if (value2 != null && value2.Type != DistrictType.Exploitation)
			{
				if (!(empire is MajorEmpire))
				{
					return false;
				}
				if (pathfindingContext.IsPrivateers && value2.City != null && value2.City.Empire != null && value2.City.Empire.Index != empire.Index)
				{
					return false;
				}
			}
		}
		if ((flags & PathfindingFlags.IgnoreEncounterAreas) == (PathfindingFlags)0 && empire != null && ((flags & PathfindingFlags.IgnoreFogOfWar) == (PathfindingFlags)0 || this.visibilityService.IsWorldPositionVisibleFor(tilePosition, empire)))
		{
			Encounter value3 = this.encountersMap.GetValue(tilePosition);
			if (value3 != null && value3.EncounterState != EncounterState.BattleHasEnded)
			{
				return false;
			}
		}
		if ((flags & PathfindingFlags.IgnoreDiplomacy) == (PathfindingFlags)0 && empire != null && !pathfindingContext.IsPrivateers)
		{
			DepartmentOfForeignAffairs agency2 = empire.GetAgency<DepartmentOfForeignAffairs>();
			if (agency2 != null && !agency2.CanMoveOn(tilePosition, pathfindingContext.IsPrivateers, pathfindingContext.IsCamouflaged))
			{
				return false;
			}
		}
		if (worldContext != null)
		{
			int currentTileHeigh = (int)this.heightMap.GetValue(tilePosition);
			PathfindingWorldContext.TileContext tileContext = worldContext.GetTileContext(tilePosition);
			if (tileContext.AdditionalRule != null && float.IsPositiveInfinity(tileContext.AdditionalRule.GetCost(pathfindingContext.MovementCapacities, currentTileHeigh)))
			{
				return false;
			}
		}
		return this.IsTilePassable(tilePosition, pathfindingContext.MovementCapacities, flags);
	}

	public bool IsTilePassable(WorldPosition tilePosition, PathfindingMovementCapacity movementCapacity, PathfindingFlags flags = (PathfindingFlags)0)
	{
		Diagnostics.Assert(this.world != null);
		if ((flags & PathfindingFlags.IgnoreMovementCapacities) == PathfindingFlags.IgnoreMovementCapacities)
		{
			movementCapacity = PathfindingMovementCapacity.All;
		}
		WorldPosition validPosition = WorldPosition.GetValidPosition(tilePosition, this.world.WorldParameters);
		if (!validPosition.IsValid)
		{
			return false;
		}
		float num = 0f;
		Region region = this.world.Regions[(int)this.regionIndexMap.GetValue(tilePosition)];
		if (region.IsWasteland)
		{
			return false;
		}
		int currentTileHeigh = (int)this.heightMap.GetValue(tilePosition);
		if ((flags & PathfindingFlags.IgnorePOI) == (PathfindingFlags)0)
		{
			Diagnostics.Assert(this.pointOfInterestMap != null);
			PointOfInterest value = this.pointOfInterestMap.GetValue(validPosition);
			if (value != null)
			{
				if (value.Type == PathfindingManager.POITypeVillage)
				{
					Diagnostics.Assert(this.poiTypeVillageSpecification != null);
					num += this.poiTypeVillageSpecification.GetCost(movementCapacity, currentTileHeigh);
				}
				else if (value.Type == PathfindingManager.POITypeCitadel)
				{
					Diagnostics.Assert(this.poiTypeCitadelSpecification != null);
					num += this.poiTypeCitadelSpecification.GetCost(movementCapacity, currentTileHeigh);
				}
				else if (value.Type == PathfindingManager.POITypeFacility)
				{
					Diagnostics.Assert(this.poiTypeFacilitySpecification != null);
					num += this.poiTypeFacilitySpecification.GetCost(movementCapacity, currentTileHeigh);
				}
				else if (value.Type == PathfindingManager.POITypeQuestLocation)
				{
					Diagnostics.Assert(this.poiTypeQuestLocationSpecification != null);
					num += this.poiTypeQuestLocationSpecification.GetCost(movementCapacity, currentTileHeigh);
				}
				else if (value.Type == PathfindingManager.POITypeResourceDeposit)
				{
					Diagnostics.Assert(this.poiTypeResourceDepositSpecification != null);
					num += this.poiTypeResourceDepositSpecification.GetCost(movementCapacity, currentTileHeigh);
				}
				else if (value.Type == PathfindingManager.POITypeWatchTower)
				{
					Diagnostics.Assert(this.poiTypeWatchTowerSpecification != null);
					num += this.poiTypeWatchTowerSpecification.GetCost(movementCapacity, currentTileHeigh);
				}
				else if (value.Type == PathfindingManager.POITypeNavalQuestLocation)
				{
					Diagnostics.Assert(this.poiTypeNavalQuestLocationSpecification != null);
					num += this.poiTypeNavalQuestLocationSpecification.GetCost(movementCapacity, currentTileHeigh);
				}
			}
		}
		Diagnostics.Assert(this.ridgeMap != null && this.ridgeSpecification != null);
		if (this.ridgeMap.GetValue(validPosition))
		{
			num += this.ridgeSpecification.GetCost(movementCapacity, currentTileHeigh);
		}
		Diagnostics.Assert(this.riverIndexMap != null && this.riverSpecification != null);
		if (this.riverIndexMap.GetValue(validPosition) >= 0)
		{
			num += this.riverSpecification.GetCost(movementCapacity, currentTileHeigh);
		}
		byte value2 = this.terrainTypeMap.GetValue((int)validPosition.Row, (int)validPosition.Column);
		Diagnostics.Assert(this.terrainSpecifications.ContainsKey((short)value2));
		num += this.terrainSpecifications[(short)value2].GetCost(movementCapacity, currentTileHeigh);
		if (!float.IsPositiveInfinity(num))
		{
			List<float> worldPositionMovementCostModifiers = this.worldEffectService.GetWorldPositionMovementCostModifiers(validPosition);
			if (worldPositionMovementCostModifiers != null)
			{
				for (int i = 0; i < worldPositionMovementCostModifiers.Count; i++)
				{
					num += num * worldPositionMovementCostModifiers[i];
				}
			}
			num = Math.Max(num, 0f);
		}
		bool flag = SimulationGlobal.GlobalTagsContains(DownloadableContent13.FrozenTile);
		bool flag2 = this.worldPositionningService.IsFrozenWaterTile(validPosition);
		if (flag && flag2)
		{
			num = this.frozenWaterTileSpecification.GetCost(movementCapacity, currentTileHeigh);
		}
		if (float.IsPositiveInfinity(num))
		{
			return false;
		}
		if (!flag2 && region.IsOcean && this.weatherService != null)
		{
			this.weatherService.OverridePathfindingCost(tilePosition, movementCapacity, ref num);
		}
		return !float.IsPositiveInfinity(num);
	}

	public bool IsTileStopable(WorldPosition tilePosition, IPathfindingContextProvider pathfindingContextProvider, PathfindingFlags flags = (PathfindingFlags)0, PathfindingWorldContext worldContext = null)
	{
		PathfindingContext pathfindingContext = pathfindingContextProvider.GenerateContext();
		global::Empire empire = pathfindingContext.Empire;
		if ((flags & PathfindingFlags.IgnoreFogOfWar) == (PathfindingFlags)0 && empire != null && !this.visibilityService.IsWorldPositionExploredFor(tilePosition, empire))
		{
			return true;
		}
		Region region = this.world.Regions[(int)this.regionIndexMap.GetValue(tilePosition)];
		if (region.IsWasteland)
		{
			return false;
		}
		if ((flags & PathfindingFlags.IgnoreArmies) == (PathfindingFlags)0 && empire != null && (this.visibilityService.IsWorldPositionVisibleFor(tilePosition, empire) || (flags & PathfindingFlags.IgnoreFogOfWar) == (PathfindingFlags)0))
		{
			Army value = this.armiesMap.GetValue(tilePosition);
			if (value != null && value.GUID != pathfindingContext.OwnerGUID)
			{
				return false;
			}
		}
		if ((flags & PathfindingFlags.IgnoreOtherEmpireDistrict) == (PathfindingFlags)0)
		{
			District value2 = this.districtsMap.GetValue(tilePosition);
			if (value2 != null && value2.Type != DistrictType.Exploitation)
			{
				if (!(empire is MajorEmpire))
				{
					return false;
				}
				if (value2.City != null && value2.City.Empire != null && value2.City.Empire.Index != empire.Index)
				{
					return false;
				}
			}
		}
		if ((flags & PathfindingFlags.IgnoreTerraformDevices) == (PathfindingFlags)0)
		{
			ITerraformDeviceService service = base.Game.GetService<ITerraformDeviceService>();
			TerraformDevice deviceAtPosition = service.GetDeviceAtPosition(tilePosition);
			if (deviceAtPosition != null && empire != null && deviceAtPosition.EmpireIndex != empire.Index)
			{
				return false;
			}
		}
		if ((flags & PathfindingFlags.IgnoreEncounterAreas) == (PathfindingFlags)0 && empire != null && ((flags & PathfindingFlags.IgnoreFogOfWar) == (PathfindingFlags)0 || this.visibilityService.IsWorldPositionVisibleFor(tilePosition, empire)))
		{
			Encounter value3 = this.encountersMap.GetValue(tilePosition);
			if (value3 != null && value3.EncounterState != EncounterState.BattleHasEnded)
			{
				return false;
			}
		}
		if (worldContext != null)
		{
			PathfindingWorldContext.TileContext tileContext = worldContext.GetTileContext(tilePosition);
			if (tileContext.AdditionalRule != null && !tileContext.AdditionalRule.IsStopable(pathfindingContext.MovementCapacities))
			{
				return false;
			}
		}
		return (pathfindingContext.RequestMode != PathfindingManager.RequestMode.AvoidToBeHurtByDefensiveTiles || !this.worldPositionningService.HasRetaliationFor(tilePosition, empire)) && this.IsTileStopable(tilePosition, pathfindingContext.MovementCapacities, flags);
	}

	public bool IsTileStopable(WorldPosition tilePosition, PathfindingMovementCapacity movementCapacity, PathfindingFlags flags = (PathfindingFlags)0)
	{
		if ((flags & PathfindingFlags.IgnoreMovementCapacities) == PathfindingFlags.IgnoreMovementCapacities)
		{
			movementCapacity = PathfindingMovementCapacity.All;
		}
		if ((flags & PathfindingFlags.IgnorePOI) == (PathfindingFlags)0)
		{
			Diagnostics.Assert(this.pointOfInterestMap != null);
			PointOfInterest value = this.pointOfInterestMap.GetValue(tilePosition);
			if (value != null)
			{
				bool flag = true;
				if (value.Type == PathfindingManager.POITypeVillage)
				{
					Diagnostics.Assert(this.poiTypeVillageSpecification != null);
					flag = this.poiTypeVillageSpecification.IsStopable(movementCapacity);
				}
				else if (value.Type == PathfindingManager.POITypeCitadel)
				{
					Diagnostics.Assert(this.poiTypeCitadelSpecification != null);
					flag = this.poiTypeCitadelSpecification.IsStopable(movementCapacity);
				}
				else if (value.Type == PathfindingManager.POITypeFacility)
				{
					Diagnostics.Assert(this.poiTypeFacilitySpecification != null);
					flag = this.poiTypeFacilitySpecification.IsStopable(movementCapacity);
				}
				else if (value.Type == PathfindingManager.POITypeQuestLocation)
				{
					Diagnostics.Assert(this.poiTypeQuestLocationSpecification != null);
					flag = this.poiTypeQuestLocationSpecification.IsStopable(movementCapacity);
				}
				else if (value.Type == PathfindingManager.POITypeNavalQuestLocation)
				{
					Diagnostics.Assert(this.poiTypeNavalQuestLocationSpecification != null);
					flag = this.poiTypeNavalQuestLocationSpecification.IsStopable(movementCapacity);
				}
				else if (value.Type == PathfindingManager.POITypeResourceDeposit)
				{
					Diagnostics.Assert(this.poiTypeResourceDepositSpecification != null);
					flag = this.poiTypeResourceDepositSpecification.IsStopable(movementCapacity);
				}
				else if (value.Type == PathfindingManager.POITypeWatchTower)
				{
					Diagnostics.Assert(this.poiTypeWatchTowerSpecification != null);
					flag = this.poiTypeWatchTowerSpecification.IsStopable(movementCapacity);
				}
				if (!flag)
				{
					return false;
				}
			}
		}
		Diagnostics.Assert(this.ridgeSpecification != null);
		if (this.ridgeMap.GetValue(tilePosition) && !this.ridgeSpecification.IsStopable(movementCapacity))
		{
			return false;
		}
		Diagnostics.Assert(this.riverSpecification != null);
		return this.riverIndexMap.GetValue(tilePosition) < 0 || this.riverSpecification.IsStopable(movementCapacity);
	}

	public float GetTileCost(WorldPosition position, PathfindingMovementCapacity movementCapacity, MajorEmpire referenceEmpire)
	{
		Diagnostics.Assert(this.world != null);
		Diagnostics.Assert(this.terrainTypeMap != null);
		Diagnostics.Assert(this.heightMap != null);
		Diagnostics.Assert(this.ridgeMap != null);
		position = WorldPosition.GetValidPosition(position, this.world.WorldParameters);
		Diagnostics.Assert(position.IsValid, "position must be valid.");
		if (referenceEmpire != null)
		{
			DepartmentOfDefense agency = referenceEmpire.GetAgency<DepartmentOfDefense>();
			if (agency != null && agency.TechnologyDefinitionShipState == DepartmentOfScience.ConstructibleElement.State.Researched)
			{
				movementCapacity |= PathfindingMovementCapacity.Water;
			}
		}
		if (!this.IsTilePassable(position, movementCapacity, (PathfindingFlags)0))
		{
			return float.PositiveInfinity;
		}
		int currentTileHeigh = (int)this.heightMap.GetValue(position);
		float num = 0f;
		float num2 = 0f;
		if (this.IsInZoneOfControl(position, referenceEmpire, (PathfindingFlags)0, null))
		{
			num2 = this.zoneOfControlMovementPointMalus;
		}
		if (this.ridgeSpecification != null && this.ridgeMap.GetValue(position))
		{
			if (this.ridgeSpecification.IsTerrainCostOverrided(movementCapacity))
			{
				return Mathf.Max(num2 + this.ridgeSpecification.GetCost(movementCapacity, currentTileHeigh), this.minimumTransitionCost);
			}
			num += this.ridgeSpecification.GetCost(movementCapacity, currentTileHeigh);
		}
		if (this.riverIndexMap.GetValue(position) >= 0)
		{
			WorldRiver river = this.worldPositionningService.GetRiver(this.riverIndexMap.GetValue(position));
			if (this.riverSpecification != null && river.RiverTypeName == WorldRiver.NormalRiverTypeName)
			{
				if (this.riverSpecification.IsTerrainCostOverrided(movementCapacity))
				{
					return Mathf.Max(num2 + this.riverSpecification.GetCost(movementCapacity, currentTileHeigh), this.minimumTransitionCost);
				}
				num += this.riverSpecification.GetCost(movementCapacity, currentTileHeigh);
			}
			else if (this.lavaRiverSpecification != null && river.RiverTypeName == WorldRiver.LavaRiverTypeName)
			{
				if (this.lavaRiverSpecification.IsTerrainCostOverrided(movementCapacity))
				{
					return Mathf.Max(num2 + this.lavaRiverSpecification.GetCost(movementCapacity, currentTileHeigh), this.minimumTransitionCost);
				}
				num += this.lavaRiverSpecification.GetCost(movementCapacity, currentTileHeigh);
			}
		}
		District value = this.districtsMap.GetValue(position);
		if (value != null && value.Empire != null && value.Type != DistrictType.Exploitation)
		{
			bool flag = false;
			if (referenceEmpire != null)
			{
				if (value.Empire.Index == referenceEmpire.Index)
				{
					flag = true;
				}
				else
				{
					DepartmentOfForeignAffairs agency2 = referenceEmpire.GetAgency<DepartmentOfForeignAffairs>();
					DiplomaticRelation diplomaticRelation = agency2.GetDiplomaticRelation(value.Empire);
					Diagnostics.Assert(diplomaticRelation != null);
					flag = diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.PassThroughCities);
				}
			}
			if (flag)
			{
				if (this.districtTileSpecification.IsTerrainCostOverrided(movementCapacity))
				{
					return Mathf.Max(num2 + this.districtTileSpecification.GetCost(movementCapacity, currentTileHeigh), this.minimumTransitionCost);
				}
				num += this.districtTileSpecification.GetCost(movementCapacity, currentTileHeigh);
			}
		}
		if (referenceEmpire != null)
		{
			bool flag2 = false;
			DepartmentOfForeignAffairs agency3 = referenceEmpire.GetAgency<DepartmentOfForeignAffairs>();
			int num3 = 0;
			while (num3 < agency3.DiplomaticRelations.Count && !flag2)
			{
				DiplomaticRelation diplomaticRelation2 = agency3.DiplomaticRelations[num3];
				if (referenceEmpire.Index == num3)
				{
					flag2 = (((int)this.cadasterMap.GetValue(position) & referenceEmpire.Bits) != 0);
				}
				else
				{
					int num4 = 1 << num3;
					flag2 = (((int)this.cadasterMap.GetValue(position) & num4) != 0 && diplomaticRelation2.HasActiveAbility(DiplomaticAbilityDefinition.ShareRoads));
				}
				num3++;
			}
			if (flag2)
			{
				DepartmentOfScience agency4 = referenceEmpire.GetAgency<DepartmentOfScience>();
				bool flag3 = agency4.HasResearchTag(PathfindingManager.HighwayTagName);
				if (flag3)
				{
					if (this.highwayTileSpecification.IsTerrainCostOverrided(movementCapacity))
					{
						return Mathf.Max(num2 + this.highwayTileSpecification.GetCost(movementCapacity, currentTileHeigh), this.minimumTransitionCost);
					}
					num += this.highwayTileSpecification.GetCost(movementCapacity, currentTileHeigh);
				}
				if (this.roadTileSpecification.IsTerrainCostOverrided(movementCapacity))
				{
					return Mathf.Max(num2 + this.roadTileSpecification.GetCost(movementCapacity, currentTileHeigh), this.minimumTransitionCost);
				}
				num += this.roadTileSpecification.GetCost(movementCapacity, currentTileHeigh);
			}
		}
		Diagnostics.Assert(this.pointOfInterestMap != null);
		PointOfInterest value2 = this.pointOfInterestMap.GetValue(position);
		if (value2 != null)
		{
			if (value2.Type == PathfindingManager.POITypeVillage)
			{
				Diagnostics.Assert(this.poiTypeVillageSpecification != null);
				if (this.poiTypeVillageSpecification.IsTerrainCostOverrided(movementCapacity))
				{
					return Mathf.Max(num2 + this.poiTypeVillageSpecification.GetCost(movementCapacity, currentTileHeigh), this.minimumTransitionCost);
				}
				num += this.poiTypeVillageSpecification.GetCost(movementCapacity, currentTileHeigh);
			}
			else if (value2.Type == PathfindingManager.POITypeCitadel)
			{
				Diagnostics.Assert(this.poiTypeCitadelSpecification != null);
				if (this.poiTypeCitadelSpecification.IsTerrainCostOverrided(movementCapacity))
				{
					return this.poiTypeCitadelSpecification.GetCost(movementCapacity, currentTileHeigh);
				}
				num += this.poiTypeCitadelSpecification.GetCost(movementCapacity, currentTileHeigh);
			}
			else if (value2.Type == PathfindingManager.POITypeFacility)
			{
				Diagnostics.Assert(this.poiTypeFacilitySpecification != null);
				if (this.poiTypeFacilitySpecification.IsTerrainCostOverrided(movementCapacity))
				{
					return this.poiTypeFacilitySpecification.GetCost(movementCapacity, currentTileHeigh);
				}
				num += this.poiTypeFacilitySpecification.GetCost(movementCapacity, currentTileHeigh);
			}
			else if (value2.Type == PathfindingManager.POITypeQuestLocation)
			{
				Diagnostics.Assert(this.poiTypeQuestLocationSpecification != null);
				if (this.poiTypeQuestLocationSpecification.IsTerrainCostOverrided(movementCapacity))
				{
					return Mathf.Max(num2 + this.poiTypeQuestLocationSpecification.GetCost(movementCapacity, currentTileHeigh), this.minimumTransitionCost);
				}
				num += this.poiTypeQuestLocationSpecification.GetCost(movementCapacity, currentTileHeigh);
			}
			else if (value2.Type == PathfindingManager.POITypeNavalQuestLocation)
			{
				Diagnostics.Assert(this.poiTypeNavalQuestLocationSpecification != null);
				if (this.poiTypeNavalQuestLocationSpecification.IsTerrainCostOverrided(movementCapacity))
				{
					return Mathf.Max(num2 + this.poiTypeNavalQuestLocationSpecification.GetCost(movementCapacity, currentTileHeigh), this.minimumTransitionCost);
				}
				num += this.poiTypeNavalQuestLocationSpecification.GetCost(movementCapacity, currentTileHeigh);
			}
			else if (value2.Type == PathfindingManager.POITypeResourceDeposit)
			{
				Diagnostics.Assert(this.poiTypeResourceDepositSpecification != null);
				if (this.poiTypeResourceDepositSpecification.IsTerrainCostOverrided(movementCapacity))
				{
					return Mathf.Max(num2 + this.poiTypeResourceDepositSpecification.GetCost(movementCapacity, currentTileHeigh), this.minimumTransitionCost);
				}
				num += this.poiTypeResourceDepositSpecification.GetCost(movementCapacity, currentTileHeigh);
			}
			else if (value2.Type == PathfindingManager.POITypeWatchTower)
			{
				Diagnostics.Assert(this.poiTypeWatchTowerSpecification != null);
				if (this.poiTypeWatchTowerSpecification.IsTerrainCostOverrided(movementCapacity))
				{
					return Mathf.Max(num2 + this.poiTypeWatchTowerSpecification.GetCost(movementCapacity, currentTileHeigh), this.minimumTransitionCost);
				}
				num += this.poiTypeWatchTowerSpecification.GetCost(movementCapacity, currentTileHeigh);
			}
		}
		if (SimulationGlobal.GlobalTagsContains(Season.ReadOnlyHeatWave) && this.worldPositionningService.IsForestTile(position))
		{
			if (this.forestSpecification.IsTerrainCostOverrided(movementCapacity))
			{
				return Mathf.Max(num2 + this.forestSpecification.GetCost(movementCapacity, currentTileHeigh), this.minimumTransitionCost);
			}
			num += this.forestSpecification.GetCost(movementCapacity, currentTileHeigh);
		}
		Diagnostics.Assert(this.terrainSpecifications != null);
		num += this.terrainSpecifications[(short)this.terrainTypeMap.GetValue(position)].GetCost(movementCapacity, currentTileHeigh);
		if (SimulationGlobal.GlobalTagsContains(DownloadableContent13.FrozenTile) && this.worldPositionningService.IsFrozenWaterTile(position))
		{
			num = this.frozenWaterTileSpecification.GetCost(movementCapacity, currentTileHeigh);
		}
		if (!this.worldPositionningService.IsFrozenWaterTile(position))
		{
			Region region = this.world.Regions[(int)this.regionIndexMap.GetValue(position)];
			if (region.IsOcean)
			{
				this.weatherService.OverridePathfindingCost(position, movementCapacity, ref num);
			}
		}
		List<float> worldPositionMovementCostModifiers = this.worldEffectService.GetWorldPositionMovementCostModifiers(position);
		if (worldPositionMovementCostModifiers != null)
		{
			for (int i = 0; i < worldPositionMovementCostModifiers.Count; i++)
			{
				num += num * worldPositionMovementCostModifiers[i];
			}
		}
		return Mathf.Max(num2 + num, this.minimumTransitionCost);
	}

	public PathfindingMovementCapacity GetTileMovementCapacity(WorldPosition position, PathfindingFlags flags = (PathfindingFlags)0)
	{
		byte value = this.terrainTypeMap.GetValue(position);
		int currentTileHeigh = (int)this.heightMap.GetValue(position);
		float cost = this.terrainSpecifications[(short)value].GetCost(PathfindingMovementCapacity.Water, currentTileHeigh);
		if (!float.IsPositiveInfinity(cost))
		{
			if ((flags & PathfindingFlags.IgnoreFrozenWaters) == (PathfindingFlags)0 && SimulationGlobal.GlobalTagsContains(DownloadableContent13.FrozenTile) && this.worldPositionningService.IsFrozenWaterTile(position))
			{
				return PathfindingMovementCapacity.FrozenWater;
			}
			return PathfindingMovementCapacity.Water;
		}
		else
		{
			float cost2 = this.terrainSpecifications[(short)value].GetCost(PathfindingMovementCapacity.Ground, currentTileHeigh);
			if (!float.IsPositiveInfinity(cost2))
			{
				return PathfindingMovementCapacity.Ground;
			}
			return PathfindingMovementCapacity.None;
		}
	}

	public bool IsTileStopableAndPassable(WorldPosition tilePosition, IPathfindingContextProvider pathfindingContextProvider, PathfindingFlags flags = (PathfindingFlags)0, PathfindingWorldContext worldContext = null)
	{
		return this.IsTilePassable(tilePosition, pathfindingContextProvider, flags, worldContext) && this.IsTileStopable(tilePosition, pathfindingContextProvider, flags, worldContext);
	}

	public bool IsTileStopableAndPassable(WorldPosition tilePosition, PathfindingMovementCapacity movementCapacity, PathfindingFlags flags = (PathfindingFlags)0)
	{
		return this.IsTilePassable(tilePosition, movementCapacity, flags) && this.IsTileStopable(tilePosition, movementCapacity, flags);
	}

	public bool IsTransitionPassable(WorldPosition start, WorldPosition goal, PathfindingMovementCapacity movementCapacity, PathfindingFlags flags = (PathfindingFlags)0)
	{
		return !float.IsPositiveInfinity(this.GetTransitionCost(start, goal, movementCapacity, flags));
	}

	public bool IsTransitionPassable(WorldPosition start, WorldPosition goal, IPathfindingContextProvider pathfindingContextProvider, PathfindingFlags flags = (PathfindingFlags)0, PathfindingWorldContext worldContext = null)
	{
		return !float.IsPositiveInfinity(this.GetTransitionCost(start, goal, pathfindingContextProvider, flags, worldContext));
	}

	private void InitializeRules()
	{
		this.terrainSpecifications = new Dictionary<short, PathfindingRule>();
		this.visibilityService = base.Game.GetService<IVisibilityService>();
		this.worldEffectService = base.Game.GetService<IWorldEffectService>();
		this.worldPositionningService = base.Game.GetService<IWorldPositionningService>();
		this.weatherService = base.Game.GetService<IWeatherService>();
		this.terrainTypeMap = (this.world.Atlas.GetMap(WorldAtlas.Maps.Terrain) as GridMap<byte>);
		Diagnostics.Assert(this.terrainTypeMap != null);
		this.heightMap = (this.world.Atlas.GetMap(WorldAtlas.Maps.Height) as GridMap<sbyte>);
		Diagnostics.Assert(this.heightMap != null);
		this.ridgeMap = (this.world.Atlas.GetMap(WorldAtlas.Maps.Ridges) as GridMap<bool>);
		Diagnostics.Assert(this.ridgeMap != null);
		this.pointOfInterestMap = (this.world.Atlas.GetMap(WorldAtlas.Maps.PointOfInterest) as GridMap<PointOfInterest>);
		Diagnostics.Assert(this.pointOfInterestMap != null);
		this.armiesMap = (this.world.Atlas.GetMap(WorldAtlas.Maps.Armies) as GridMap<Army>);
		Diagnostics.Assert(this.armiesMap != null);
		this.districtsMap = (this.world.Atlas.GetMap(WorldAtlas.Maps.Districts) as GridMap<District>);
		Diagnostics.Assert(this.districtsMap != null);
		this.encountersMap = (this.world.Atlas.GetMap(WorldAtlas.Maps.Encounters) as GridMap<Encounter>);
		Diagnostics.Assert(this.districtsMap != null);
		this.regionIndexMap = (this.world.Atlas.GetMap(WorldAtlas.Maps.Regions) as GridMap<short>);
		Diagnostics.Assert(this.regionIndexMap != null);
		this.riverIndexMap = (this.world.Atlas.GetMap(WorldAtlas.Maps.River) as GridMap<short>);
		Diagnostics.Assert(this.riverIndexMap != null);
		this.cadasterMap = (this.world.Atlas.GetMap(WorldAtlas.Maps.Cadaster) as GridMap<byte>);
		Diagnostics.Assert(this.cadasterMap != null);
		Map<TerrainTypeName> map = this.world.Atlas.GetMap(WorldAtlas.Tables.Terrains) as Map<TerrainTypeName>;
		Diagnostics.Assert(map != null);
		IDatabase<PathfindingRule> database = Databases.GetDatabase<PathfindingRule>(false);
		Diagnostics.Assert(database != null);
		PathfindingRule value;
		if (!database.TryGetValue(PathfindingManager.DefaultSpecificationName, out value))
		{
			Diagnostics.LogError("Can't get default specification named '{0}'.", new object[]
			{
				PathfindingManager.DefaultSpecificationName
			});
		}
		if (!database.TryGetValue(PathfindingManager.ForestSpecificationName, out this.forestSpecification))
		{
			this.forestSpecification = value;
			Diagnostics.LogWarning("Can't get forest specification named '{0}', it is set to default specification '{1}'.", new object[]
			{
				PathfindingManager.ForestSpecificationName,
				PathfindingManager.DefaultSpecificationName
			});
		}
		if (!database.TryGetValue(PathfindingManager.RidgeSpecificationName, out this.ridgeSpecification))
		{
			this.ridgeSpecification = value;
			Diagnostics.LogWarning("Can't get ridge specification named '{0}', it is set to default specification '{1}'.", new object[]
			{
				PathfindingManager.RidgeSpecificationName,
				PathfindingManager.DefaultSpecificationName
			});
		}
		if (!database.TryGetValue(PathfindingManager.RiverSpecificationName, out this.riverSpecification))
		{
			this.riverSpecification = value;
			Diagnostics.LogWarning("Can't get river specification named '{0}', it is set to default specification '{1}'.", new object[]
			{
				PathfindingManager.RiverSpecificationName,
				PathfindingManager.DefaultSpecificationName
			});
		}
		if (!database.TryGetValue(PathfindingManager.LavaRiverSpecificationName, out this.lavaRiverSpecification))
		{
			this.lavaRiverSpecification = value;
			Diagnostics.LogWarning("Can't get river specification named '{0}', it is set to default specification '{1}'.", new object[]
			{
				PathfindingManager.LavaRiverSpecificationName,
				PathfindingManager.DefaultSpecificationName
			});
		}
		if (!database.TryGetValue(PathfindingManager.DistrictTileSpecificationName, out this.districtTileSpecification))
		{
			this.districtTileSpecification = value;
			Diagnostics.LogWarning("Can't get district specification named '{0}', it is set to default specification '{1}'.", new object[]
			{
				PathfindingManager.DistrictTileSpecificationName,
				PathfindingManager.DefaultSpecificationName
			});
		}
		if (!database.TryGetValue(PathfindingManager.RoadTileSpecificationName, out this.roadTileSpecification))
		{
			this.roadTileSpecification = value;
			Diagnostics.LogWarning("Can't get road specification named '{0}', it is set to default specification '{1}'.", new object[]
			{
				PathfindingManager.RoadTileSpecificationName,
				PathfindingManager.DefaultSpecificationName
			});
		}
		if (!database.TryGetValue(PathfindingManager.HighwayTileSpecificationName, out this.highwayTileSpecification))
		{
			this.highwayTileSpecification = value;
			Diagnostics.LogWarning("Can't get highway specification named '{0}', it is set to default specification '{1}'.", new object[]
			{
				PathfindingManager.HighwayTileSpecificationName,
				PathfindingManager.DefaultSpecificationName
			});
		}
		if (!database.TryGetValue(PathfindingManager.POITypeQuestLocationSpecificationName, out this.poiTypeQuestLocationSpecification))
		{
			this.poiTypeQuestLocationSpecification = value;
			Diagnostics.LogWarning("Can't get point of interest specification named '{0}', it is set to default specification '{1}'.", new object[]
			{
				PathfindingManager.POITypeQuestLocationSpecificationName,
				PathfindingManager.DefaultSpecificationName
			});
		}
		if (!database.TryGetValue(PathfindingManager.POITypeNavalQuestLocationSpecificationName, out this.poiTypeNavalQuestLocationSpecification))
		{
			this.poiTypeNavalQuestLocationSpecification = value;
			Diagnostics.LogWarning("Can't get point of interest specification named '{0}', it is set to default specification '{1}'.", new object[]
			{
				PathfindingManager.POITypeNavalQuestLocationSpecificationName,
				PathfindingManager.DefaultSpecificationName
			});
		}
		if (!database.TryGetValue(PathfindingManager.POITypeResourceDepositSpecificationName, out this.poiTypeResourceDepositSpecification))
		{
			this.poiTypeResourceDepositSpecification = value;
			Diagnostics.LogWarning("Can't get point of interest specification named '{0}', it is set to default specification '{1}'.", new object[]
			{
				PathfindingManager.POITypeResourceDepositSpecificationName,
				PathfindingManager.DefaultSpecificationName
			});
		}
		if (!database.TryGetValue(PathfindingManager.POITypeVillageSpecificationName, out this.poiTypeVillageSpecification))
		{
			this.poiTypeVillageSpecification = value;
			Diagnostics.LogWarning("Can't get point of interest specification named '{0}', it is set to default specification '{1}'.", new object[]
			{
				PathfindingManager.POITypeVillageSpecificationName,
				PathfindingManager.DefaultSpecificationName
			});
		}
		if (!database.TryGetValue(PathfindingManager.POITypeCitadelSpecificationName, out this.poiTypeCitadelSpecification))
		{
			this.poiTypeCitadelSpecification = value;
			Diagnostics.LogWarning("Can't get point of interest specification named '{0}', it is set to default specification '{1}'.", new object[]
			{
				PathfindingManager.POITypeCitadelSpecificationName,
				PathfindingManager.DefaultSpecificationName
			});
		}
		if (!database.TryGetValue(PathfindingManager.POITypeFacilitySpecificationName, out this.poiTypeFacilitySpecification))
		{
			this.poiTypeFacilitySpecification = value;
			Diagnostics.LogWarning("Can't get point of interest specification named '{0}', it is set to default specification '{1}'.", new object[]
			{
				PathfindingManager.POITypeFacilitySpecificationName,
				PathfindingManager.DefaultSpecificationName
			});
		}
		if (!database.TryGetValue(PathfindingManager.POITypeWatchTowerSpecificationName, out this.poiTypeWatchTowerSpecification))
		{
			this.poiTypeWatchTowerSpecification = value;
			Diagnostics.LogWarning("Can't get point of interest specification named '{0}', it is set to default specification '{1}'.", new object[]
			{
				PathfindingManager.POITypeWatchTowerSpecificationName,
				PathfindingManager.DefaultSpecificationName
			});
		}
		if (!database.TryGetValue(PathfindingManager.FrozenWaterTileSpecificationName, out this.frozenWaterTileSpecification))
		{
			this.frozenWaterTileSpecification = value;
			Diagnostics.LogWarning("Can't get road specification named '{0}', it is set to default specification '{1}'.", new object[]
			{
				PathfindingManager.FrozenWaterTileSpecificationName,
				PathfindingManager.DefaultSpecificationName
			});
		}
		if (map.Data != null)
		{
			for (int i = 0; i < map.Data.Length; i++)
			{
				TerrainTypeName terrainTypeName = map.Data[i];
				PathfindingRule value2;
				if (database.TryGetValue(terrainTypeName.Value, out value2))
				{
					this.terrainSpecifications.Add(terrainTypeName.TypeValue, value2);
				}
				else
				{
					this.terrainSpecifications.Add(terrainTypeName.TypeValue, value);
					Diagnostics.LogWarning("Can't get terrain type pathfinding specification named '{0}', it is set to default specification '{1}'.", new object[]
					{
						terrainTypeName.Value,
						PathfindingManager.DefaultSpecificationName
					});
				}
			}
		}
	}

	private bool IsInZoneOfControl(WorldPosition worldPosition, global::Empire empire, PathfindingFlags flags, PathfindingWorldContext worldContext)
	{
		int num = (int)(worldPosition.Row & 1);
		for (int i = 0; i < 6; i++)
		{
			int[] array = WorldPosition.NeighborOffsets[num][i];
			WorldPosition validPosition = WorldPosition.GetValidPosition(new WorldPosition((int)worldPosition.Row + array[0], (int)worldPosition.Column + array[1]), this.world.WorldParameters);
			if (validPosition.IsValid)
			{
				if (empire == null || this.visibilityService.IsWorldPositionVisibleFor(validPosition, empire) || (flags & PathfindingFlags.IgnoreFogOfWar) == (PathfindingFlags)0)
				{
					bool flag = this.IsTileProvidingZoneOfControl(validPosition, empire, worldContext);
					if (flag)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private bool IsTileProvidingZoneOfControl(WorldPosition worldPosition, global::Empire empire, PathfindingWorldContext worldContext)
	{
		if (worldContext != null && worldContext.GetTileContext(worldPosition).IsProvidingZoneOfControl)
		{
			return true;
		}
		if (empire == null)
		{
			return false;
		}
		Army value = this.armiesMap.GetValue(worldPosition);
		if (value == null || value.Empire == null)
		{
			return false;
		}
		global::Empire empire2 = value.Empire;
		bool flag;
		if (empire2.Index == empire.Index)
		{
			flag = true;
		}
		else
		{
			DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
			if (empire2 is MajorEmpire && agency != null)
			{
				DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(empire2);
				Diagnostics.Assert(diplomaticRelation != null);
				flag = diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.PassThroughArmies);
			}
			else
			{
				flag = false;
			}
		}
		return !flag;
	}

	[UnitTestMethod("Pathfinding", UnitTestMethodAttribute.Scope.Game)]
	public static void UnitTest_PathfindingGetTransitionCost(UnitTestResult unitTestResult)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service == null)
		{
			unitTestResult.State = UnitTestResult.UnitTestState.Failed;
			return;
		}
		global::Game game = service.Game as global::Game;
		if (game == null)
		{
			unitTestResult.State = UnitTestResult.UnitTestState.Failed;
			return;
		}
		IPathfindingService service2 = game.GetService<IPathfindingService>();
		if (service2 == null)
		{
			unitTestResult.State = UnitTestResult.UnitTestState.Failed;
			return;
		}
		PathfindingContext pathfindingContext = new PathfindingContext(GameEntityGUID.Zero, game.Empires[0], PathfindingMovementCapacity.Ground);
		pathfindingContext.RefreshProperties(1f, 4f, false, false, 4f, 4f);
		WorldParameters worldParameters = game.World.WorldParameters;
		using (new UnityProfilerSample("UnitTest_PathfindingGetTransitionCost"))
		{
			for (int i = 0; i < (int)game.World.WorldParameters.Rows; i++)
			{
				for (int j = 0; j < (int)worldParameters.Columns; j++)
				{
					WorldPosition start = new WorldPosition(i, j);
					int num = (int)(start.Row % 2);
					for (int k = 0; k < 6; k++)
					{
						int[] array = WorldPosition.NeighborOffsets[num][k];
						WorldPosition validPosition = WorldPosition.GetValidPosition(new WorldPosition((int)start.Row + array[0], (int)start.Column + array[1]), worldParameters);
						if (validPosition.IsValid)
						{
							service2.GetTransitionCost(start, validPosition, pathfindingContext, (PathfindingFlags)0, null);
						}
					}
				}
			}
		}
	}

	[UnitTestMethod("Pathfinding", UnitTestMethodAttribute.Scope.Game)]
	public static void UnitTest_PathfindingFindPath(UnitTestResult unitTestResult)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service == null)
		{
			unitTestResult.State = UnitTestResult.UnitTestState.Failed;
			return;
		}
		global::Game game = service.Game as global::Game;
		if (game == null)
		{
			unitTestResult.State = UnitTestResult.UnitTestState.Failed;
			return;
		}
		IPathfindingService service2 = game.GetService<IPathfindingService>();
		if (service2 == null)
		{
			unitTestResult.State = UnitTestResult.UnitTestState.Failed;
			return;
		}
		PathfindingContext pathfindingContext = new PathfindingContext(GameEntityGUID.Zero, game.Empires[0], PathfindingMovementCapacity.Ground);
		pathfindingContext.RefreshProperties(1f, 4f, false, false, 4f, 4f);
		System.Random random = new System.Random(7331);
		using (new UnityProfilerSample("UnitTest_FindPath"))
		{
			for (int i = 0; i < 100; i++)
			{
				WorldPosition start = new WorldPosition(random.Next(0, (int)game.World.WorldParameters.Rows), random.Next(0, (int)game.World.WorldParameters.Columns));
				WorldPosition goal = new WorldPosition(random.Next(0, (int)game.World.WorldParameters.Rows), random.Next(0, (int)game.World.WorldParameters.Columns));
				service2.FindPath(pathfindingContext, start, goal, PathfindingManager.RequestMode.Default, null, (PathfindingFlags)0, null);
			}
		}
	}

	public HierarchicalPathfindingAStar HierarchicalPathfinding { get; private set; }

	public bool IsReady { get; private set; }

	public PathfindingAStar Pathfinding { get; private set; }

	public override IEnumerator BindServices(IServiceContainer serviceContainer)
	{
		yield return base.BindServices(serviceContainer);
		this.internalContext = new PathfindingContext(GameEntityGUID.Zero, null, PathfindingMovementCapacity.All);
		this.internalContext.RefreshProperties(1f, float.PositiveInfinity, false, false, float.PositiveInfinity, float.PositiveInfinity);
		serviceContainer.AddService<IPathfindingService>(this);
		yield break;
	}

	public PathfindingResult FindPath(IPathfindingContextProvider pathfindingContextProvider, WorldPosition start, WorldPosition goal, PathfindingManager.RequestMode requestMode = PathfindingManager.RequestMode.Default, PathfindingWorldContext worldContext = null, PathfindingFlags flags = (PathfindingFlags)0, PathfindingAStar.StopPredicate stopPredicate = null)
	{
		if (pathfindingContextProvider == null)
		{
			throw new ArgumentNullException("pathfindingContextProvider");
		}
		PathfindingContext pathfindingContext = pathfindingContextProvider.GenerateContext();
		if (pathfindingContext == null)
		{
			Diagnostics.LogError("There is no context in the pathfinding context provider.");
			return null;
		}
		if (pathfindingContext.MovementCapacities == PathfindingMovementCapacity.None)
		{
			return null;
		}
		if (start.Row < 0 || start.Row >= this.world.WorldParameters.Rows || start.Column < 0 || start.Column >= this.world.WorldParameters.Columns)
		{
			Diagnostics.LogError("The start position {0} is invalid, the pathfinding request is canceled.", new object[]
			{
				start
			});
			return null;
		}
		if (goal.Row < 0 || goal.Row >= this.world.WorldParameters.Rows || goal.Column < 0 || goal.Column >= this.world.WorldParameters.Columns)
		{
			Diagnostics.LogError("The goal position {0} is invalid, the pathfinding request is canceled.", new object[]
			{
				goal
			});
			return null;
		}
		pathfindingContext.RequestMode = requestMode;
		if (this.Pathfinding == null)
		{
			Diagnostics.LogError("AStar is not initialized.");
			return null;
		}
		Diagnostics.Assert(this.world != null);
		if (worldContext != null && worldContext.SearchArea != null && (!worldContext.SearchArea.Contains(start, this.world.WorldParameters) || !worldContext.SearchArea.Contains(goal, this.world.WorldParameters)))
		{
			return null;
		}
		global::Empire empire = pathfindingContext.Empire;
		if (((flags & PathfindingFlags.IgnoreFogOfWar) == PathfindingFlags.IgnoreFogOfWar || (empire != null && this.visibilityService.IsWorldPositionExploredFor(goal, empire))) && (pathfindingContext.MovementCapacities & PathfindingMovementCapacity.Water) != PathfindingMovementCapacity.Water && (pathfindingContext.MovementCapacities & PathfindingMovementCapacity.FrozenWater) == PathfindingMovementCapacity.None)
		{
			byte groundCapacityConnectivityGraphIndex = this.Pathfinding.GetGroundCapacityConnectivityGraphIndex(start);
			if (groundCapacityConnectivityGraphIndex != this.Pathfinding.GetGroundCapacityConnectivityGraphIndex(goal))
			{
				return null;
			}
		}
		PathfindingResult pathfindingResult = null;
		switch (requestMode)
		{
		case PathfindingManager.RequestMode.Default:
		case PathfindingManager.RequestMode.AvoidToBeHurtByDefensiveTiles:
			pathfindingResult = this.InternalFindPath(start, goal, pathfindingContext, worldContext, flags, stopPredicate);
			break;
		case PathfindingManager.RequestMode.ApproachGoal:
		{
			if ((pathfindingContext.MovementCapacities & PathfindingMovementCapacity.Water) == PathfindingMovementCapacity.Water)
			{
				pathfindingResult = this.InternalFindPath(start, goal, pathfindingContext, worldContext, flags, stopPredicate);
			}
			byte groundCapacityConnectivityGraphIndex2 = this.Pathfinding.GetGroundCapacityConnectivityGraphIndex(start);
			if (groundCapacityConnectivityGraphIndex2 == this.Pathfinding.GetGroundCapacityConnectivityGraphIndex(goal))
			{
				pathfindingResult = this.InternalFindPath(start, goal, pathfindingContext, worldContext, flags, stopPredicate);
			}
			PathfindingResult pathfindingResult2 = null;
			object obj = this.padlock;
			lock (obj)
			{
				pathfindingResult2 = this.Pathfinding.FindPath(goal, start, this.internalContext, stopPredicate, worldContext, flags);
			}
			if (pathfindingResult2 == null || pathfindingResult2.Goal == start)
			{
				return null;
			}
			goal = pathfindingResult2.Goal;
			Diagnostics.Assert(groundCapacityConnectivityGraphIndex2 == this.Pathfinding.GetGroundCapacityConnectivityGraphIndex(goal));
			pathfindingResult = this.InternalFindPath(start, goal, pathfindingContext, worldContext, flags, stopPredicate);
			break;
		}
		}
		if (pathfindingResult != null && flags != (PathfindingFlags)0 && flags == OrderAttack.AttackFlags)
		{
			WorldPosition worldPosition = goal;
			WorldPosition position = WorldPosition.Invalid;
			if (pathfindingResult != null && worldPosition != WorldPosition.Invalid && this.worldPositionningService != null && !this.IsTileStopable(worldPosition, pathfindingContext, (PathfindingFlags)0, null))
			{
				foreach (WorldPosition worldPosition2 in pathfindingResult.GetCompletePath())
				{
					position = worldPosition2;
				}
				bool flag = this.worldPositionningService.IsWaterTile(worldPosition);
				bool flag2 = this.worldPositionningService.IsWaterTile(position);
				if (flag != flag2)
				{
					List<WorldPosition> neighbours = worldPosition.GetNeighbours(this.world.WorldParameters);
					List<WorldPosition> list = new List<WorldPosition>();
					for (int i = 0; i < neighbours.Count; i++)
					{
						if (this.worldPositionningService.IsWaterTile(neighbours[i]) == flag && !this.worldPositionningService.HasRidge(neighbours[i]) && this.IsTileStopable(neighbours[i], pathfindingContext, (PathfindingFlags)0, null))
						{
							list.Add(neighbours[i]);
						}
					}
					if (list.Count > 0)
					{
						PathfindingResult pathfindingResult3 = null;
						float num = float.MaxValue;
						for (int j = 0; j < list.Count; j++)
						{
							PathfindingResult pathfindingResult4 = this.FindPath(pathfindingContext, start, list[j], PathfindingManager.RequestMode.Default, null, flags, null);
							if (pathfindingResult4 != null && pathfindingResult4.GetCost() < num)
							{
								num = pathfindingResult4.GetCost();
								pathfindingResult3 = pathfindingResult4;
							}
						}
						if (pathfindingResult3 != null)
						{
							pathfindingResult = pathfindingResult3;
						}
					}
				}
			}
		}
		return pathfindingResult;
	}

	public PathfindingResult FindLocation(IPathfindingContextProvider pathfindingContextProvider, WorldPosition start, PathfindingAStar.StopPredicate stopPredicate, PathfindingWorldContext pathfindingWorldContext = null, PathfindingFlags flags = (PathfindingFlags)0)
	{
		if (pathfindingContextProvider == null)
		{
			throw new ArgumentNullException("pathfindingContextProvider");
		}
		PathfindingContext pathfindingContext = pathfindingContextProvider.GenerateContext();
		if (pathfindingContext == null)
		{
			Diagnostics.LogError("There is no context in the pathfinding context provider.");
			return null;
		}
		if (pathfindingContext.MovementCapacities == PathfindingMovementCapacity.None)
		{
			return null;
		}
		pathfindingContext.RequestMode = PathfindingManager.RequestMode.Default;
		if (this.Pathfinding == null)
		{
			Diagnostics.LogError("AStar is not initialized.");
			return null;
		}
		Diagnostics.Assert(this.world != null);
		if (pathfindingWorldContext != null && !pathfindingWorldContext.SearchArea.Contains(start, this.world.WorldParameters))
		{
			return null;
		}
		return this.InternalFindLocation(start, pathfindingContext, stopPredicate, pathfindingWorldContext, flags);
	}

	public void FillWithValidLocation(IPathfindingContextProvider pathfindingContextProvider, WorldPosition start, PathfindingAStar.StopPredicate stopPredicate, ref List<WorldPosition> validPositions, PathfindingWorldContext pathfindingWorldContext = null, PathfindingFlags flags = (PathfindingFlags)0)
	{
		if (pathfindingContextProvider == null)
		{
			throw new ArgumentNullException("pathfindingContextProvider");
		}
		PathfindingContext pathfindingContext = pathfindingContextProvider.GenerateContext();
		if (pathfindingContext == null)
		{
			Diagnostics.LogError("There is no context in the pathfinding context provider.");
			return;
		}
		if (pathfindingContext.MovementCapacities == PathfindingMovementCapacity.None)
		{
			return;
		}
		pathfindingContext.RequestMode = PathfindingManager.RequestMode.Default;
		if (this.Pathfinding == null)
		{
			Diagnostics.LogError("AStar is not initialized.");
			return;
		}
		Diagnostics.Assert(this.world != null);
		if (pathfindingWorldContext != null && !pathfindingWorldContext.SearchArea.Contains(start, this.world.WorldParameters))
		{
			return;
		}
		this.InternalFillWithValidLocation(start, pathfindingContext, stopPredicate, pathfindingWorldContext, flags, ref validPositions);
	}

	public override IEnumerator OnWorldLoaded(World loadedWorld)
	{
		yield return base.OnWorldLoaded(loadedWorld);
		this.world = loadedWorld;
		this.worldRect = new WorldRect(new WorldPosition(this.world.WorldParameters.Rows, 0), new WorldPosition(0, this.world.WorldParameters.Columns), this.world.WorldParameters);
		this.minimumTransitionCost = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<float>("Gameplay/Ancillaries/Pathfinding/MinimumTransitionCost", 0.25f);
		this.zoneOfControlMovementPointMalus = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<float>("Gameplay/Ancillaries/Pathfinding/ZoneOfControlMovementPointMalus", 1f);
		this.InitializeRules();
		yield return null;
		this.Pathfinding = new PathfindingAStar(this.world, this.minimumTransitionCost);
		this.Pathfinding.Load();
		this.HierarchicalPathfinding = null;
		if (this.EnableHierarchicalPathfinding)
		{
			this.HierarchicalPathfinding = new HierarchicalPathfindingAStar(this.world, this.Pathfinding, this.NumberOfAbstractLevel, (short)this.ClusterWorldSize, (short)this.ClusterSize);
			this.loadingThread = new Amplitude.Threading.Thread("Pathfinding loading", new ThreadStart(this.HierarchicalPathfinding.Load));
			this.loadingThread.Start();
		}
		else
		{
			this.OnPathfindingServiceReady();
		}
		yield break;
	}

	public void Update()
	{
		if (this.loadingThread == null || this.loadingThread.IsAlive)
		{
			return;
		}
		if (this.IsReady)
		{
			return;
		}
		this.OnPathfindingServiceReady();
	}

	protected override void Releasing()
	{
		if (this.loadingThread != null)
		{
			this.loadingThread.Abort();
			this.loadingThread.Dispose();
			this.loadingThread = null;
		}
		this.Pathfinding = null;
		if (this.HierarchicalPathfinding != null)
		{
			this.HierarchicalPathfinding.Unload();
			this.HierarchicalPathfinding = null;
		}
		this.world = null;
		this.visibilityService = null;
		this.worldEffectService = null;
		base.Releasing();
	}

	private PathfindingResult InternalFindPath(WorldPosition start, WorldPosition goal, PathfindingContext pathfindingContext, PathfindingWorldContext worldContext, PathfindingFlags flags, PathfindingAStar.StopPredicate stopPredicate = null)
	{
		if (!this.IsReady)
		{
			Diagnostics.LogWarning("Pathfinding service is not ready.");
			return null;
		}
		if (this.Pathfinding == null)
		{
			Diagnostics.LogError("AStar is not initialized.");
			return null;
		}
		if (start == goal)
		{
			return null;
		}
		pathfindingContext.Goal = goal;
		if (this.EnableHierarchicalPathfinding)
		{
			PathfindingResult pathfindingResult = null;
			int distance = WorldPosition.GetDistance(start, goal, this.world.WorldParameters.IsCyclicWorld, this.world.WorldParameters.Columns);
			if (distance < this.MinimumDistanceToUseHPAStar)
			{
				short num = (short)(distance + 2);
				WorldRect worldRect = new WorldRect(start, WorldOrientation.East, num, num, num, num, this.world.WorldParameters);
				Diagnostics.Assert(worldRect.Contains(start, this.world.WorldParameters), "Start position {0} is not contained in the search rect {1}.", new object[]
				{
					start,
					worldRect
				});
				Diagnostics.Assert(worldRect.Contains(goal, this.world.WorldParameters), "Goal position {0} is not contained in the search rect {1}.", new object[]
				{
					goal,
					worldRect
				});
				object obj = this.padlock;
				lock (obj)
				{
					pathfindingResult = this.Pathfinding.FindPath(start, goal, pathfindingContext, stopPredicate, new PathfindingWorldContext(worldRect, null), (PathfindingFlags)0);
				}
			}
			if (pathfindingResult == null)
			{
				object obj2 = this.padlock;
				lock (obj2)
				{
					Diagnostics.Assert(this.HierarchicalPathfinding != null);
					pathfindingResult = this.HierarchicalPathfinding.FindAbstractPath(this.worldRect, start, goal, pathfindingContext);
				}
			}
			return pathfindingResult;
		}
		object obj3 = this.padlock;
		PathfindingResult result;
		lock (obj3)
		{
			result = this.Pathfinding.FindPath(start, goal, pathfindingContext, stopPredicate, worldContext, flags);
		}
		return result;
	}

	private PathfindingResult InternalFindLocation(WorldPosition start, PathfindingContext pathfindingContext, PathfindingAStar.StopPredicate stopPredicate, PathfindingWorldContext pathfindingWorldContext, PathfindingFlags flags)
	{
		if (!this.IsReady)
		{
			Diagnostics.LogWarning("Pathfinding service is not ready.");
			return null;
		}
		if (this.Pathfinding == null)
		{
			Diagnostics.LogError("AStar is not initialized.");
			return null;
		}
		pathfindingContext.Goal = WorldPosition.Invalid;
		object obj = this.padlock;
		PathfindingResult result;
		lock (obj)
		{
			result = this.Pathfinding.FindLocation(start, pathfindingContext, stopPredicate, pathfindingWorldContext, flags);
		}
		return result;
	}

	private void InternalFillWithValidLocation(WorldPosition start, PathfindingContext pathfindingContext, PathfindingAStar.StopPredicate stopPredicate, PathfindingWorldContext pathfindingWorldContext, PathfindingFlags flags, ref List<WorldPosition> validPositions)
	{
		if (!this.IsReady)
		{
			Diagnostics.LogWarning("Pathfinding service is not ready.");
			return;
		}
		if (this.Pathfinding == null)
		{
			Diagnostics.LogError("AStar is not initialized.");
			return;
		}
		pathfindingContext.Goal = WorldPosition.Invalid;
		object obj = this.padlock;
		lock (obj)
		{
			this.Pathfinding.FillWithValidLocation(start, pathfindingContext, stopPredicate, pathfindingWorldContext, flags, ref validPositions);
		}
	}

	private void OnPathfindingServiceReady()
	{
		if (this.loadingThread != null)
		{
			this.loadingThread.Dispose();
			this.loadingThread = null;
		}
		this.IsReady = true;
		if (this.PathfindingServiceReady != null)
		{
			this.PathfindingServiceReady(this, new EventArgs());
		}
		Diagnostics.Log("The pathfinding service is ready.");
	}

	private static readonly StaticString DefaultSpecificationName = "Default";

	private static readonly StaticString DistrictTileSpecificationName = "DistrictTile";

	private static readonly StaticString ForestSpecificationName = "Forest";

	private static readonly StaticString HighwayTagName = "TechnologyRoadSpeedBonus1";

	private static readonly StaticString HighwayTileSpecificationName = "HighwayTile";

	private static readonly StaticString POITypeQuestLocation = "QuestLocation";

	private static readonly StaticString POITypeQuestLocationSpecificationName = "PointOfInterest_QuestLocation";

	private static readonly StaticString POITypeNavalQuestLocation = "NavalQuestLocation";

	private static readonly StaticString POITypeNavalQuestLocationSpecificationName = "PointOfInterest_NavalQuestLocation";

	private static readonly StaticString POITypeResourceDeposit = "ResourceDeposit";

	private static readonly StaticString POITypeResourceDepositSpecificationName = "PointOfInterest_ResourceDeposit";

	private static readonly StaticString POITypeVillage = "Village";

	private static readonly StaticString POITypeVillageSpecificationName = "PointOfInterest_Village";

	private static readonly StaticString POITypeCitadel = "Citadel";

	private static readonly StaticString POITypeCitadelSpecificationName = "PointOfInterest_Citadel";

	private static readonly StaticString POITypeFacility = "Facility";

	private static readonly StaticString POITypeFacilitySpecificationName = "PointOfInterest_Facility";

	private static readonly StaticString POITypeWatchTower = "WatchTower";

	private static readonly StaticString POITypeWatchTowerSpecificationName = "PointOfInterest_WatchTower";

	private static readonly StaticString RidgeSpecificationName = "Ridge";

	private static readonly StaticString RiverSpecificationName = "River";

	private static readonly StaticString LavaRiverSpecificationName = "LavaRiver";

	private static readonly StaticString RoadTileSpecificationName = "RoadTile";

	private static readonly StaticString FrozenWaterTileSpecificationName = "FrozenWaterTile";

	private GridMap<Army> armiesMap;

	private GridMap<byte> cadasterMap;

	private PathfindingRule districtTileSpecification;

	private GridMap<District> districtsMap;

	private GridMap<Encounter> encountersMap;

	private PathfindingRule forestSpecification;

	private GridMap<sbyte> heightMap;

	private PathfindingRule highwayTileSpecification;

	private float minimumTransitionCost;

	private PathfindingRule poiTypeQuestLocationSpecification;

	private PathfindingRule poiTypeNavalQuestLocationSpecification;

	private PathfindingRule poiTypeResourceDepositSpecification;

	private PathfindingRule poiTypeVillageSpecification;

	private PathfindingRule poiTypeCitadelSpecification;

	private PathfindingRule poiTypeFacilitySpecification;

	private PathfindingRule poiTypeWatchTowerSpecification;

	private GridMap<PointOfInterest> pointOfInterestMap;

	private GridMap<short> regionIndexMap;

	private GridMap<bool> ridgeMap;

	private PathfindingRule ridgeSpecification;

	private GridMap<short> riverIndexMap;

	private PathfindingRule riverSpecification;

	private PathfindingRule lavaRiverSpecification;

	private PathfindingRule roadTileSpecification;

	private PathfindingRule frozenWaterTileSpecification;

	private PathfindingRule flotsamTileSpecification;

	private PathfindingRule turbulenceTileSpecification;

	private Dictionary<short, PathfindingRule> terrainSpecifications;

	private GridMap<byte> terrainTypeMap;

	private IVisibilityService visibilityService;

	private IWorldEffectService worldEffectService;

	private IWorldPositionningService worldPositionningService;

	private IWeatherService weatherService;

	public bool EnableHierarchicalPathfinding;

	public int ClusterSize = 2;

	public int ClusterWorldSize = 10;

	public long HierarchicalPathfindingLoadingDuration = -1L;

	public int MinimumDistanceToUseHPAStar = 20;

	public byte NumberOfAbstractLevel;

	private object padlock = new object();

	private PathfindingContext internalContext;

	private Amplitude.Threading.Thread loadingThread;

	private World world;

	private WorldRect worldRect;

	private float zoneOfControlMovementPointMalus;

	public enum RequestMode
	{
		Default,
		ApproachGoal,
		AvoidToBeHurtByDefensiveTiles
	}
}
