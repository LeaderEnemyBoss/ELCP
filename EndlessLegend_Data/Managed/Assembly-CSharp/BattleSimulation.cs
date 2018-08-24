using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using Amplitude;
using Amplitude.Extensions;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Session;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Unity.Xml;
using Amplitude.Utilities;
using Amplitude.Utilities.Maps;
using UnityEngine;

[Diagnostics.TagAttribute("BattleSimulation")]
[Diagnostics.TagAttribute("BattleSimulation")]
[Diagnostics.TagAttribute("BattleSimulation")]
public class BattleSimulation : IDisposable
{
	public BattleSimulation(World world, BattleEncounter encounter, Amplitude.Utilities.Random random)
	{
		if (world == null)
		{
			throw new ArgumentNullException("world");
		}
		if (encounter == null)
		{
			throw new ArgumentNullException("encounter");
		}
		this.worldAtlas = world.Atlas;
		this.WorldParameters = world.WorldParameters;
		this.BattleSimulationRandom = random;
		this.battleZone = new BattleZone_Battle(encounter, world.WorldParameters);
		this.battleZoneAnalysis = encounter.BattleZoneAnalysis;
		this.battleZoneAnalysis.ResetBattleZone(this.battleZone);
		this.pathfindingWorldContextByContenderGroup = new PathfindingWorldContext[2];
		this.pathfindingWorldContextByContenderGroup[0] = new PathfindingWorldContext(this.battleZone, new Dictionary<WorldPosition, PathfindingWorldContext.TileContext>());
		this.pathfindingWorldContextByContenderGroup[1] = new PathfindingWorldContext(this.battleZone, new Dictionary<WorldPosition, PathfindingWorldContext.TileContext>());
		this.battleActionDatabase = Databases.GetDatabase<BattleAction>(false);
		Diagnostics.Assert(this.battleActionDatabase != null, "Can't get battle action database.");
		this.battleTargetingStrategyDatabase = Databases.GetDatabase<BattleTargetingStrategy>(false);
		Diagnostics.Assert(this.battleTargetingStrategyDatabase != null);
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null && service.Game != null && service.Game.Services != null);
		this.pathfindingService = service.Game.Services.GetService<IPathfindingService>();
		if (this.pathfindingService == null)
		{
			Diagnostics.LogError("Invalid null path finding service.");
			return;
		}
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		if (this.worldPositionningService == null)
		{
			Diagnostics.LogError("Invalid null world positionning service.");
			return;
		}
		this.weatherService = service.Game.Services.GetService<IWeatherService>();
		if (this.weatherService == null)
		{
			Diagnostics.LogError("Invalid null weather service.");
			return;
		}
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		if (this.gameEntityRepositoryService == null)
		{
			Diagnostics.LogError("Failed to retrieve the game entity repository service.");
		}
		this.battleActionController = new BattleActionController(this);
		this.battleTargetingController = new BattleTargetingController(this.worldPositionningService, this.pathfindingService, this);
		this.battleUnitActionController = new BattleUnitActionController(this, this.pathfindingService, this.worldPositionningService, this.worldAtlas);
		this.battleActionsGround = new Dictionary<WorldPosition, List<BattleSimulation.GroundAction>>();
		this.armiesByGroup = new Dictionary<byte, List<BattleSimulationArmy>>();
		this.unitsSimulationAverageDataByGroup = new Dictionary<byte, List<BattleSimulation.UnitsSimulationCompositionData>>();
		List<BattleContender> battleContenders = encounter.BattleContenders;
		for (int i = 0; i < battleContenders.Count; i++)
		{
			this.AddContender(battleContenders[i], encounter);
			if (!this.unitsSimulationAverageDataByGroup.ContainsKey(battleContenders[i].Group))
			{
				this.unitsSimulationAverageDataByGroup.Add(battleContenders[i].Group, new List<BattleSimulation.UnitsSimulationCompositionData>());
				this.unitsSimulationAverageDataByGroup[battleContenders[i].Group].Add(new BattleSimulation.UnitsSimulationCompositionData(SimulationProperties.OffensiveMilitaryPower, 0f));
				this.unitsSimulationAverageDataByGroup[battleContenders[i].Group].Add(new BattleSimulation.UnitsSimulationCompositionData(SimulationProperties.DefensiveMilitaryPower, 0f));
			}
		}
		this.InitBattleSimulationTargets();
		foreach (WorldPosition worldPosition in this.battleZone.GetWorldPositions())
		{
			if (worldPosition.IsValid)
			{
				List<BattleSimulation.GroundAction> list;
				this.battleActionsGround.TryGetValue(worldPosition, out list);
				if (list == null)
				{
					list = new List<BattleSimulation.GroundAction>();
					this.battleActionsGround.Add(worldPosition, list);
				}
				IEnumerable<BattleActionGround> enumerable = this.GetBattleActionGroundForPosition(worldPosition);
				if (enumerable != null)
				{
					foreach (BattleActionGround battleAction in enumerable)
					{
						list.Add(new BattleSimulation.GroundAction(battleAction, null));
					}
				}
				foreach (KeyValuePair<byte, List<BattleSimulationArmy>> keyValuePair in this.armiesByGroup)
				{
					BattleSimulationArmy battleSimulationArmy = keyValuePair.Value[0];
					BattleSimulationUnit initiator = null;
					if (battleSimulationArmy.Units != null && battleSimulationArmy.Units.Count > 0)
					{
						initiator = battleSimulationArmy.Units[0];
					}
					enumerable = this.GetBattleActionGroundForPositionByContender(battleSimulationArmy.Contender, worldPosition);
					if (enumerable != null)
					{
						foreach (BattleActionGround battleAction2 in enumerable)
						{
							list.Add(new BattleSimulation.GroundAction(battleAction2, initiator));
						}
					}
				}
				this.pathfindingWorldContextByContenderGroup[0].SetTileContext(worldPosition, BattleZoneAnalysis.BattleNoUnitSpecification);
				this.pathfindingWorldContextByContenderGroup[1].SetTileContext(worldPosition, BattleZoneAnalysis.BattleNoUnitSpecification);
			}
		}
	}

	public bool CanBattleActionBeAppliedOnTargets(BattleAction battleAction, BattleSimulationUnit referenceUnit, BattleSimulationTarget[] currentTargets)
	{
		bool result = false;
		for (int i = 0; i < battleAction.BattleEffects.Length; i++)
		{
			BattleSimulationTarget[] array = this.FilterTargets(battleAction.BattleEffects[i].TargetsFilter, referenceUnit, currentTargets);
			if (array != null && array.Length > 0)
			{
				result = true;
				break;
			}
		}
		return result;
	}

	public bool ExecuteBattleActionUser(BattleContender battleContender, BattleActionUser userBattleAction, UnitBodyDefinition unitBodyDefinition)
	{
		if (battleContender == null)
		{
			throw new ArgumentNullException("battleContender");
		}
		if (userBattleAction == null)
		{
			throw new ArgumentNullException("userBattleAction");
		}
		if (unitBodyDefinition == null)
		{
			throw new ArgumentNullException("unitBodyDefinition");
		}
		Diagnostics.Assert(this.battleActionController != null);
		BattleAction.State battleActionState = this.battleActionController.GetBattleActionState(userBattleAction);
		if (battleActionState != BattleAction.State.Available)
		{
			return false;
		}
		if (userBattleAction.BattleEffects == null)
		{
			return false;
		}
		List<BattleSimulationUnit> list = new List<BattleSimulationUnit>();
		Diagnostics.Assert(this.battleSimulationArmies != null);
		for (int i = 0; i < this.battleSimulationArmies.Count; i++)
		{
			BattleSimulationArmy battleSimulationArmy = this.battleSimulationArmies[i];
			Diagnostics.Assert(battleSimulationArmy != null);
			if (!(battleSimulationArmy.ContenderGUID != battleContender.GUID))
			{
				Diagnostics.Assert(battleSimulationArmy.Units != null);
				list.AddRange(from battleSimulationUnit in battleSimulationArmy.Units
				where battleSimulationUnit.UnitBodyDefinitionReference == unitBodyDefinition.Name && battleSimulationUnit.Position.IsValid
				select battleSimulationUnit);
			}
		}
		if (list.Count == 0)
		{
			Diagnostics.LogWarning("The battle simulation refuse to execute the battle action {0}, all the unit {1} might be dead for contender {2}. In this case the GUI shouldn't allow you to trig the battle action.", new object[]
			{
				userBattleAction.Name,
				unitBodyDefinition.Name,
				battleContender.GUID
			});
			return false;
		}
		return this.battleActionController.ExecuteBattleAction(userBattleAction, userBattleAction.BattleEffects, list.ToArray(), null, false);
	}

	public bool ExecuteBattleActionUser(BattleContender battleContender, BattleActionUser userBattleActionUser, BattleSimulationUnit referenceUnit, WorldPosition targetPosition)
	{
		return this.battleActionController.ExecuteBattleAction(userBattleActionUser, userBattleActionUser.BattleEffects, referenceUnit, targetPosition, true);
	}

	public BattleSimulationTarget[] FilterTargets(BattleEffects.TargetFlags targetsFilter, BattleSimulationUnit referenceUnit, BattleSimulationTarget[] currentTargets)
	{
		return BattleEffects.FilterTargets(this.potentialBattleSimulationTargets, targetsFilter, referenceUnit, currentTargets);
	}

	private BattleActionUnit[] InitializeBattleActionUnits(Unit unit)
	{
		List<BattleActionUnit> list = new List<BattleActionUnit>();
		UnitBodyDefinition unitBodyDefinition = unit.UnitDesign.UnitBodyDefinition;
		if (unitBodyDefinition == null)
		{
			return null;
		}
		List<XmlNamedReference> list2 = new List<XmlNamedReference>();
		UnitAbilityReference[] abilities = unit.GetAbilities();
		IDatabase<UnitAbility> database = Databases.GetDatabase<UnitAbility>(false);
		for (int i = 0; i < abilities.Length; i++)
		{
			UnitAbility unitAbility = null;
			int level = abilities[i].Level;
			if (database.TryGetValue(abilities[i].Name, out unitAbility))
			{
				if (unitAbility.BattleActionUnitReferences != null)
				{
					list2.AddRange(unitAbility.BattleActionUnitReferences);
				}
				if (unitAbility.AbilityLevels != null && level >= 0 && level < unitAbility.AbilityLevels.Length && unitAbility.AbilityLevels[level].BattleActionUnitReferences != null)
				{
					list2.AddRange(unitAbility.AbilityLevels[level].BattleActionUnitReferences);
				}
			}
		}
		if (unit.Embarked)
		{
			IDatabase<UnitBodyDefinition> database2 = Databases.GetDatabase<UnitBodyDefinition>(false);
			UnitBodyDefinition unitBodyDefinition2;
			if (database2 != null && database2.TryGetValue("UnitBodyTransportShip", out unitBodyDefinition2))
			{
				unitBodyDefinition = unitBodyDefinition2;
			}
		}
		if (unitBodyDefinition.UnitBodyBattleParameters != null && unitBodyDefinition.UnitBodyBattleParameters.BattleActionUnitReferences != null)
		{
			list2.AddRange(unitBodyDefinition.UnitBodyBattleParameters.BattleActionUnitReferences);
		}
		if (list2.Count <= 0)
		{
			return null;
		}
		for (int j = 0; j < list2.Count; j++)
		{
			XmlNamedReference reference = list2[j];
			Diagnostics.Assert(this.battleActionDatabase != null);
			BattleAction battleAction;
			if (this.battleActionDatabase.TryGetValue(reference, out battleAction))
			{
				BattleActionUnit battleActionUnit = battleAction as BattleActionUnit;
				if (battleActionUnit != null)
				{
					list.Add(battleActionUnit);
				}
			}
		}
		return list.ToArray();
	}

	public void AddGroundAction(BattleActionGround battleActionGround, WorldPosition targetPosition, BattleSimulationUnit referenceUnit)
	{
		List<BattleSimulation.GroundAction> list;
		this.battleActionsGround.TryGetValue(targetPosition, out list);
		if (list == null)
		{
			list = new List<BattleSimulation.GroundAction>();
			this.battleActionsGround.Add(targetPosition, list);
		}
		list.Add(new BattleSimulation.GroundAction(battleActionGround, referenceUnit));
	}

	public int GetGroundActionsCount(WorldPosition position)
	{
		if (this.battleActionsGround.ContainsKey(position))
		{
			return this.battleActionsGround[position].Count;
		}
		return 0;
	}

	public int GetPositiveGroundActionsCountByGroup(WorldPosition position, byte group)
	{
		int num = 0;
		if (this.battleActionsGround.ContainsKey(position))
		{
			List<BattleSimulation.GroundAction> list = this.battleActionsGround[position];
			for (int i = 0; i < list.Count; i++)
			{
				if (!list[i].BattleAction.IsNegativeGroundEffect)
				{
					if (list[i].Initiator == null || list[i].Initiator.Contender.Group == group)
					{
						num++;
					}
				}
			}
		}
		return num;
	}

	public void RemoveGroundAction(BattleActionGround battleActionGround, WorldPosition targetPosition, BattleSimulationUnit referenceUnit)
	{
		List<BattleSimulation.GroundAction> list;
		this.battleActionsGround.TryGetValue(targetPosition, out list);
		if (list == null)
		{
			BattleSimulation.GroundAction item = list.FirstOrDefault((BattleSimulation.GroundAction match) => match.BattleAction.Name == battleActionGround.Name && ((referenceUnit == null && match.Initiator == null) || referenceUnit.UnitGUID == match.Initiator.UnitGUID));
			if (list.Contains(item))
			{
				list.Remove(item);
			}
		}
	}

	private void ExecuteGroundAction(BattleSimulationUnit unit, WorldPosition target)
	{
		foreach (BattleActionGround battleAction in unit.BattleActionsGround)
		{
			List<BattleSimulation.GroundAction> list;
			this.battleActionsGround.TryGetValue(target, out list);
			if (list == null)
			{
				list = new List<BattleSimulation.GroundAction>();
				this.battleActionsGround.Add(target, list);
			}
			list.Add(new BattleSimulation.GroundAction(battleAction, unit));
		}
	}

	private void ApplyGroundActions(BattleSimulationUnit target, IBattleUnitState unitState = null)
	{
		if (this.battleActionsGround == null || this.battleActionsGround.Count <= 0)
		{
			return;
		}
		List<BattleSimulation.GroundAction> list;
		this.battleActionsGround.TryGetValue(target.Position, out list);
		if (list == null || list.Count <= 0)
		{
			return;
		}
		list.Sort(delegate(BattleSimulation.GroundAction left, BattleSimulation.GroundAction right)
		{
			int num2 = left.BattleAction.PrioritySensitive.CompareTo(right.BattleAction.PrioritySensitive);
			if (num2 == 0)
			{
				num2 = left.BattleAction.Priority.CompareTo(right.BattleAction.Priority);
			}
			return num2;
		});
		BattleSimulationTarget[] currentTargets = new BattleSimulationTarget[]
		{
			new BattleSimulationTarget(target)
		};
		int num = 0;
		bool flag = false;
		foreach (BattleSimulation.GroundAction groundAction in list)
		{
			BattleActionGround battleAction = groundAction.BattleAction;
			Diagnostics.Assert(battleAction != null);
			BattleSimulationUnit battleSimulationUnit = groundAction.Initiator;
			if (battleSimulationUnit == null)
			{
				battleSimulationUnit = target;
			}
			if (this.CanBattleActionBeAppliedOnTargets(battleAction, battleSimulationUnit, currentTargets))
			{
				if (!flag)
				{
					if (battleAction.PrioritySensitive)
					{
						flag = true;
						num = battleAction.Priority;
					}
				}
				else if (num != battleAction.Priority)
				{
					break;
				}
				if (battleAction.BattleEffects == null)
				{
					Diagnostics.LogWarning("The battle action {0} has no battle effects.", new object[]
					{
						battleAction.Name
					});
				}
				else if (unitState != null)
				{
					unitState.UnitStateBattleActions.Add(new BattleUnitState.StateBattleAction(battleAction, battleAction.BattleEffects, true, battleSimulationUnit));
				}
				else
				{
					this.battleActionController.ExecuteBattleAction(battleAction, battleAction.BattleEffects, battleSimulationUnit, target, currentTargets, true, null);
				}
			}
		}
	}

	private void UnapplyGroundActions(BattleSimulationUnit unit, IBattleUnitState unitState)
	{
		if (this.battleActionsGround == null || this.battleActionsGround.Count <= 0)
		{
			return;
		}
		List<BattleSimulation.GroundAction> list;
		this.battleActionsGround.TryGetValue(unit.Position, out list);
		if (list == null || list.Count <= 0)
		{
			return;
		}
		this.battleActionController.SetReportCopy(unitState.AdditiveInstructions);
		foreach (BattleSimulation.GroundAction groundAction in list)
		{
			BattleAction battleAction = groundAction.BattleAction;
			Diagnostics.Assert(battleAction != null);
			if (battleAction.BattleEffects == null)
			{
				Diagnostics.LogWarning("The battle action {0} has no battle effects.", new object[]
				{
					battleAction.Name
				});
			}
			else if (unitState != null)
			{
				unit.InterruptBattleAction(battleAction, this.battleActionController);
			}
		}
		this.battleActionController.SetReportCopy(null);
	}

	public WorldPosition[] FindPath(BattleSimulationUnit unit, WorldPosition targetPosition, int range = 0)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		Diagnostics.Assert(this.pathfindingService != null);
		Diagnostics.Assert(this.pathfindingWorldContextByContenderGroup != null);
		PathfindingAStar.StopPredicate stopPredicate = delegate(WorldPosition start, WorldPosition goal, PathfindingContext context, PathfindingWorldContext pathfindingWorldContext, WorldPosition position, PathfindingFlags flags)
		{
			bool flag = this.pathfindingService.IsTileStopable(position, context, flags, pathfindingWorldContext);
			bool flag2 = range != 1 || this.pathfindingService.IsTransitionPassable(position, goal, context, flags, null);
			int distance = this.worldPositionningService.GetDistance(position, goal);
			return flag && flag2 && distance <= range;
		};
		PathfindingWorldContext.TileContext tileContext = this.pathfindingWorldContextByContenderGroup[(int)unit.Contender.Group].GetTileContext(unit.Position);
		this.pathfindingWorldContextByContenderGroup[(int)unit.Contender.Group].SetTileContext(unit.Position, BattleZoneAnalysis.BattleNoUnitSpecification);
		WorldPosition[] result = null;
		PathfindingResult pathfindingResult = this.pathfindingService.FindPath(unit, unit.Position, targetPosition, PathfindingManager.RequestMode.Default, this.pathfindingWorldContextByContenderGroup[(int)unit.Contender.Group], PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreEncounterAreas | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges, stopPredicate);
		if (pathfindingResult == null)
		{
			this.validPositions.Clear();
			this.pathfindingService.FillWithValidLocation(unit, unit.Position, new PathfindingAStar.StopPredicate(this.FillPredicate), ref this.validPositions, this.pathfindingWorldContextByContenderGroup[(int)unit.Contender.Group], PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreEncounterAreas | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges);
			pathfindingResult = this.pathfindingService.FindLocation(unit, targetPosition, new PathfindingAStar.StopPredicate(this.ChooseBestLocationPredicate), new PathfindingWorldContext(this.battleZone, null), PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreEncounterAreas | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges);
			if (pathfindingResult != null)
			{
				pathfindingResult = this.pathfindingService.FindPath(unit, unit.Position, pathfindingResult.Goal, PathfindingManager.RequestMode.Default, this.pathfindingWorldContextByContenderGroup[(int)unit.Contender.Group], PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreEncounterAreas | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges, null);
			}
		}
		if (pathfindingResult != null)
		{
			pathfindingResult.ComputeControlPoints(1f, out result, this.pathfindingWorldContextByContenderGroup[(int)unit.Contender.Group]);
		}
		this.pathfindingWorldContextByContenderGroup[(int)unit.Contender.Group].SetTileContext(unit.Position, tileContext);
		return result;
	}

	public BattleSimulationArmy GetBattleSimulationArmyByGUID(GameEntityGUID contenderGUID)
	{
		for (int i = 0; i < this.battleSimulationArmies.Count; i++)
		{
			BattleSimulationArmy battleSimulationArmy = this.battleSimulationArmies[i];
			Diagnostics.Assert(battleSimulationArmy != null);
			if (battleSimulationArmy.ContenderGUID == contenderGUID)
			{
				return battleSimulationArmy;
			}
		}
		return null;
	}

	public BattleSimulationUnit GetBattleUnit(GameEntityGUID guid)
	{
		Diagnostics.Assert(this.battleSimulationArmies != null);
		for (int i = 0; i < this.battleSimulationArmies.Count; i++)
		{
			BattleSimulationArmy battleSimulationArmy = this.battleSimulationArmies[i];
			Diagnostics.Assert(battleSimulationArmy != null && battleSimulationArmy.Units != null);
			using (IEnumerator<BattleSimulationUnit> enumerator = (from battleSimulationUnit in battleSimulationArmy.Units
			where battleSimulationUnit.UnitGUID == guid
			select battleSimulationUnit).GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					return enumerator.Current;
				}
			}
		}
		return null;
	}

	public WorldPosition GetNearestFreePosition(WorldPosition position)
	{
		BattleSimulationUnit unitFromPosition = this.GetUnitFromPosition(position, false);
		if (unitFromPosition == null || unitFromPosition.IsTraversable)
		{
			return position;
		}
		return this.GetNearestFreePositionInternal(position);
	}

	public WorldPosition GetNearestFreePositionInternal(WorldPosition position)
	{
		List<WorldPosition> neighbours = position.GetNeighbours(this.WorldParameters);
		foreach (WorldPosition worldPosition in neighbours)
		{
			if (worldPosition.IsValid)
			{
				BattleSimulationUnit unitFromPosition = this.GetUnitFromPosition(worldPosition, false);
				if (unitFromPosition == null || unitFromPosition.IsTraversable)
				{
					return worldPosition;
				}
			}
		}
		foreach (WorldPosition position2 in neighbours)
		{
			if (position2.IsValid)
			{
				WorldPosition nearestFreePositionInternal = this.GetNearestFreePositionInternal(position2);
				if (nearestFreePositionInternal.IsValid)
				{
					return nearestFreePositionInternal;
				}
			}
		}
		return new WorldPosition(-1, -1);
	}

	public List<WorldPosition> GetValidPositionsForAttack(BattleSimulationUnit unit, BattleSimulationUnit target, List<WorldPosition> reachablePositions, bool refreshHelperUnits = true)
	{
		List<WorldPosition> second = new List<WorldPosition>();
		int num = Mathf.RoundToInt(unit.GetCurrentPropertyValue(SimulationProperties.CanAttackThroughImpassableTransition));
		int num2 = (int)Math.Truncate((double)unit.GetPropertyValue(SimulationProperties.BattleRange));
		this.ResetUnitReachablePositionsHelper(refreshHelperUnits);
		this.GetReachablePositions(target.Position, (float)num2, unit, num == 0, false, true, false, ref second);
		if (reachablePositions == null)
		{
			return null;
		}
		List<WorldPosition> list = reachablePositions.Intersect(second).ToList<WorldPosition>();
		for (int i = list.Count - 1; i >= 0; i--)
		{
			WorldPosition key = list[i];
			BattleSimulationUnit unit2 = this.unitReachablePositionsHelper[key].Unit;
			if (unit2 != null && unit2 != unit)
			{
				list.RemoveAt(i);
			}
		}
		return list;
	}

	public List<WorldPosition> GetValidPositionsForAttack(BattleSimulationUnit unit, BattleSimulationUnit target)
	{
		List<WorldPosition> reachablePositions = new List<WorldPosition>();
		this.ResetUnitReachablePositionsHelper(true);
		this.GetReachablePositions(unit.Position, unit.GetPropertyValue(SimulationProperties.BattleMaximumMovement), unit, true, true, true, true, ref reachablePositions);
		return this.GetValidPositionsForAttack(unit, target, reachablePositions, false);
	}

	public void GetReachablePositions(WorldPosition startingPosition, float range, BattleSimulationUnit unit, bool takeCareOfPathfinding, bool includeStartingPosition, bool avoidEnemies, bool useRealPathfindingCost, ref List<WorldPosition> reachablePositions)
	{
		if (!this.unitReachablePositionsHelper.ContainsKey(startingPosition))
		{
			return;
		}
		this.unitReachablePositionsHelper[startingPosition].Range = range;
		if (includeStartingPosition)
		{
			bool flag = this.pathfindingService.IsTileStopable(startingPosition, unit, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreEncounterAreas | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges, null);
			if (flag)
			{
				reachablePositions.AddOnce(startingPosition);
			}
		}
		if (range <= 0f)
		{
			return;
		}
		List<WorldPosition> neighbours = this.unitReachablePositionsHelper[startingPosition].Neighbours;
		int i = 0;
		while (i < neighbours.Count)
		{
			WorldPosition worldPosition = neighbours[i];
			if (!avoidEnemies)
			{
				goto IL_BD;
			}
			BattleSimulationUnit unit2 = this.unitReachablePositionsHelper[worldPosition].Unit;
			if (unit2 == null || unit2.Contender.Group == unit.Contender.Group)
			{
				goto IL_BD;
			}
			IL_138:
			i++;
			continue;
			IL_BD:
			float num = 1f;
			if (takeCareOfPathfinding)
			{
				num = this.pathfindingService.GetTransitionCost(startingPosition, worldPosition, unit, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreEncounterAreas | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges, null);
				if (float.IsPositiveInfinity(num))
				{
					goto IL_138;
				}
			}
			if (!useRealPathfindingCost)
			{
				num = 1f;
			}
			float num2 = range - num;
			if (this.unitReachablePositionsHelper[worldPosition].Range >= num2)
			{
				goto IL_138;
			}
			this.GetReachablePositions(worldPosition, range - num, unit, takeCareOfPathfinding, true, avoidEnemies, useRealPathfindingCost, ref reachablePositions);
			goto IL_138;
		}
	}

	public ActivePeriod[] GetTargetActivePeriods(BattleSimulationTarget target)
	{
		Diagnostics.Assert(this.battleActionController != null);
		return this.battleActionController.GetTargetActivePeriods(target);
	}

	public List<WorldPosition> GetWorldPositionNeighbours(WorldPosition position)
	{
		List<WorldPosition> list = new List<WorldPosition>(6);
		int num = (int)(position.Row % 2);
		for (int i = 0; i < 6; i++)
		{
			int[] array = WorldPosition.NeighborOffsets[num][i];
			WorldPosition validPosition = WorldPosition.GetValidPosition(new WorldPosition((int)position.Row + array[0], (int)position.Column + array[1]), this.worldPositionningService.World.WorldParameters);
			if (this.battleZone.Contains(validPosition))
			{
				list.Add(validPosition);
			}
		}
		return list;
	}

	public List<BattleSimulationTarget> GetWorldPositionNeighboursAsBattleSimulationTargets(WorldPosition position)
	{
		List<BattleSimulationTarget> list = new List<BattleSimulationTarget>(6);
		int num = (int)(position.Row % 2);
		for (int i = 0; i < 6; i++)
		{
			int[] array = WorldPosition.NeighborOffsets[num][i];
			WorldPosition validPosition = WorldPosition.GetValidPosition(new WorldPosition((int)position.Row + array[0], (int)position.Column + array[1]), this.worldPositionningService.World.WorldParameters);
			if (this.battleZone.Contains(validPosition))
			{
				BattleSimulationTarget potentialTargetByPosition = this.GetPotentialTargetByPosition(validPosition);
				if (potentialTargetByPosition != null)
				{
					list.Add(potentialTargetByPosition);
				}
			}
		}
		return list;
	}

	public BattleSimulationUnit GetUnitFromPosition(WorldPosition unitPosition, bool includeDeadUnits = false)
	{
		Diagnostics.Assert(this.battleSimulationArmies != null);
		for (int i = 0; i < this.battleSimulationArmies.Count; i++)
		{
			BattleSimulationArmy battleSimulationArmy = this.battleSimulationArmies[i];
			Diagnostics.Assert(battleSimulationArmy != null);
			ReadOnlyCollection<BattleSimulationUnit> units = battleSimulationArmy.Units;
			for (int j = 0; j < units.Count; j++)
			{
				BattleSimulationUnit battleSimulationUnit = units[j];
				Diagnostics.Assert(battleSimulationUnit != null);
				if (battleSimulationUnit.Position.Equals(unitPosition) && (includeDeadUnits || !battleSimulationUnit.IsTraversable))
				{
					return battleSimulationUnit;
				}
			}
		}
		return null;
	}

	public bool IsBattleFinished()
	{
		if (this.battleSimulationArmies == null)
		{
			return true;
		}
		Dictionary<byte, bool> dictionary = new Dictionary<byte, bool>();
		for (int i = 0; i < this.battleSimulationArmies.Count; i++)
		{
			BattleSimulationArmy battleSimulationArmy = this.battleSimulationArmies[i];
			if (battleSimulationArmy.Contender.IsTakingPartInBattle)
			{
				bool flag = battleSimulationArmy.IsDead;
				if (!flag && !battleSimulationArmy.HasFightingUnitAlive && battleSimulationArmy.HasWaitingReinforcements)
				{
					flag = true;
					ReadOnlyCollection<WorldPosition> reinforcementPoints = battleSimulationArmy.ReinforcementPoints;
					for (int j = 0; j < reinforcementPoints.Count; j++)
					{
						if (this.GetUnitFromPosition(reinforcementPoints[j], false) == null)
						{
							flag = false;
							break;
						}
					}
				}
				if (!dictionary.ContainsKey(battleSimulationArmy.Group))
				{
					dictionary.Add(battleSimulationArmy.Group, flag);
				}
				else
				{
					Dictionary<byte, bool> dictionary3;
					Dictionary<byte, bool> dictionary2 = dictionary3 = dictionary;
					byte group;
					byte key = group = battleSimulationArmy.Group;
					bool flag2 = dictionary3[group];
					dictionary2[key] = (flag2 && flag);
				}
			}
		}
		int num = 0;
		using (Dictionary<byte, bool>.ValueCollection.Enumerator enumerator = dictionary.Values.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (!enumerator.Current)
				{
					num++;
				}
			}
		}
		return num <= 1;
	}

	private IEnumerable<BattleActionGround> GetBattleActionGroundForPositionByContender(BattleContender battleContender, WorldPosition position)
	{
		List<BattleActionGround> list = new List<BattleActionGround>();
		IGameEntity gameEntity;
		if (this.gameEntityRepositoryService.TryGetValue(battleContender.GUID, out gameEntity))
		{
			Army army = gameEntity as Army;
			District district = this.worldPositionningService.GetDistrict(position);
			if (army != null && district != null && district.Empire == army.Empire)
			{
				SimulationDescriptor descriptorFromType = district.GetDescriptorFromType("DistrictType");
				string x = string.Format("BattleActionGround{0}Bonus", descriptorFromType.Name);
				BattleAction battleAction;
				if (this.battleActionDatabase.TryGetValue(x, out battleAction))
				{
					BattleActionGround battleActionGround = battleAction as BattleActionGround;
					if (battleActionGround != null)
					{
						list.Add(battleAction as BattleActionGround);
					}
				}
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list;
	}

	private IEnumerable<BattleActionGround> GetBattleActionGroundForPosition(WorldPosition position)
	{
		List<BattleActionGround> list = new List<BattleActionGround>();
		BattleActionGround item = null;
		string empty = string.Empty;
		byte terrainType = this.worldPositionningService.GetTerrainType(position);
		StaticString terrainTypeMappingName = this.worldPositionningService.GetTerrainTypeMappingName(terrainType);
		if (!StaticString.IsNullOrEmpty(terrainTypeMappingName) && this.GetBattleActionGroundForName(terrainTypeMappingName, out item))
		{
			list.Add(item);
		}
		short riverId = this.worldPositionningService.GetRiverId(position);
		StaticString riverTypeMappingName = this.worldPositionningService.GetRiverTypeMappingName(riverId);
		if (!StaticString.IsNullOrEmpty(riverTypeMappingName) && this.GetBattleActionGroundForName(riverTypeMappingName, out item))
		{
			list.Add(item);
		}
		WeatherDefinition weatherDefinitionAtPosition = this.weatherService.GetWeatherDefinitionAtPosition(position);
		if (weatherDefinitionAtPosition != null && !StaticString.IsNullOrEmpty(weatherDefinitionAtPosition.Name) && this.GetBattleActionGroundForName(weatherDefinitionAtPosition.Name, out item))
		{
			list.Add(item);
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list;
	}

	private bool GetBattleActionGroundForName(StaticString name, out BattleActionGround battleActionGround)
	{
		if (!StaticString.IsNullOrEmpty(name))
		{
			BattleAction battleAction = null;
			string x = string.Format("BattleActionGround{0}Bonus", name);
			if (this.battleActionDatabase.TryGetValue(x, out battleAction) && battleAction != null && battleAction is BattleActionGround)
			{
				battleActionGround = (battleAction as BattleActionGround);
				return true;
			}
		}
		battleActionGround = null;
		return false;
	}

	private float GetUnitReinforcementScore(BattleSimulationUnit unit)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		float num = unit.GetPropertyValue(SimulationProperties.UnitExperience);
		num += unit.GetPropertyValue(SimulationProperties.Damage);
		return num + unit.GetPropertyValue(SimulationProperties.Health);
	}

	private bool FillPredicate(WorldPosition start, WorldPosition goal, PathfindingContext context, PathfindingWorldContext pathfindingWorldContext, WorldPosition position, PathfindingFlags flags)
	{
		return this.pathfindingService.IsTileStopable(position, context, flags, pathfindingWorldContext);
	}

	private bool ChooseBestLocationPredicate(WorldPosition start, WorldPosition goal, PathfindingContext context, PathfindingWorldContext pathfindingWorldContext, WorldPosition position, PathfindingFlags flags)
	{
		return this.validPositions.Contains(position);
	}

	public List<GameEntityGUID> BuildAvailableOpportunityTargetsForUnit(BattleSimulationUnit unit, WorldPosition anticipatedPosition)
	{
		List<GameEntityGUID> list = new List<GameEntityGUID>();
		List<WorldPosition> list2 = new List<WorldPosition>();
		int num = Mathf.RoundToInt(unit.GetCurrentPropertyValue(SimulationProperties.CanAttackThroughImpassableTransition));
		int num2 = (int)Math.Truncate((double)unit.GetPropertyValue(SimulationProperties.BattleRange));
		this.ResetUnitReachablePositionsHelper(true);
		this.GetReachablePositions(anticipatedPosition, (float)num2, unit, num == 0, false, false, false, ref list2);
		for (int i = 0; i < list2.Count; i++)
		{
			BattleSimulationUnit unitFromPosition = this.GetUnitFromPosition(list2[i], false);
			if (unitFromPosition != null)
			{
				if (this.CanTargetUnit(unit, unitFromPosition))
				{
					list.Add(unitFromPosition.UnitGUID);
				}
			}
		}
		return list;
	}

	public bool CanAttackTarget(BattleSimulationUnit unit, WorldPosition targetPosition)
	{
		if (unit.IsDead)
		{
			return false;
		}
		int num = (int)Math.Truncate((double)unit.GetPropertyValue(SimulationProperties.BattleRange));
		int distance = this.worldPositionningService.GetDistance(unit.Position, targetPosition);
		if (distance <= num)
		{
			int num2 = Mathf.RoundToInt(unit.GetCurrentPropertyValue(SimulationProperties.CanAttackThroughImpassableTransition));
			if (num2 > 0)
			{
				return true;
			}
			if (this.pathfindingService.IsTransitionPassable(unit.Position, targetPosition, unit, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreEncounterAreas | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges, null))
			{
				return true;
			}
		}
		return false;
	}

	public bool CanAttackTarget(BattleSimulationUnit unit, BattleSimulationTarget target)
	{
		return this.CanAttackTarget(unit, target.DynamicPosition);
	}

	public bool CanTargetUnit(BattleSimulationUnit unit, BattleSimulationUnit target)
	{
		if (unit.IsMindControlled)
		{
			return unit.Contender.Group == target.Contender.Group && unit != target;
		}
		if (unit.Contender.Group == target.Contender.Group)
		{
			return (unit.AvailableTargetFlags & EncounterUnit.TargetType.Ally) > (EncounterUnit.TargetType)0;
		}
		return (unit.AvailableTargetFlags & EncounterUnit.TargetType.Enemy) > (EncounterUnit.TargetType)0;
	}

	public void ComputeAvailableTargets(BattleSimulationUnit unit)
	{
		if (unit.IsBattleSimulated)
		{
			return;
		}
		unit.ClearAvailableTargets();
		List<WorldPosition> list = new List<WorldPosition>();
		this.ResetUnitReachablePositionsHelper(true);
		bool avoidEnemies = true;
		if (unit.Unit.SimulationObject.Tags.Contains("MovementCapacityAcrobat") || unit.Unit.SimulationObject.Tags.Contains("MovementCapacitySubmersible"))
		{
			avoidEnemies = false;
		}
		this.GetReachablePositions(unit.Position, unit.GetPropertyValue(SimulationProperties.BattleMaximumMovement), unit, true, true, avoidEnemies, true, ref list);
		for (int i = 0; i < this.aliveUnits.Count; i++)
		{
			if (!this.aliveUnits[i].IsDead)
			{
				BattleSimulationUnit target = this.aliveUnits[i];
				if (this.CanTargetUnit(unit, target))
				{
					List<WorldPosition> validPositionsForAttack = this.GetValidPositionsForAttack(unit, target, list, false);
					if (validPositionsForAttack != null && validPositionsForAttack.Count > 0)
					{
						UnitAvailableTarget.TargetAccessibilityType accessibilityType = UnitAvailableTarget.TargetAccessibilityType.AfterMoving;
						if (validPositionsForAttack.Contains(unit.Position))
						{
							accessibilityType = UnitAvailableTarget.TargetAccessibilityType.Direct;
						}
						unit.AddAvailableTarget(target, accessibilityType);
					}
				}
			}
		}
		list.RemoveAll((WorldPosition match) => this.unitReachablePositionsHelper[match].Unit != null);
		UnitReachablePositionsInstruction item = new UnitReachablePositionsInstruction(unit.UnitGUID, list);
		unit.ReportInstructions.Add(item);
	}

	public void ComputeTargetingIntention(BattleSimulationUnit unit)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		if (!unit.CanPlayBattleRound)
		{
			return;
		}
		if (unit.IsMindControlled)
		{
			return;
		}
		IDatabase<BattleTargetingStrategy> database = Databases.GetDatabase<BattleTargetingStrategy>(false);
		Diagnostics.Assert(database != null);
		BattleTargetingStrategy battleTargetingStrategy;
		if (database.TryGetValue(unit.TargetingStrategyCurrent, out battleTargetingStrategy) && !battleTargetingStrategy.PrecomputeTargetingIntentions)
		{
			return;
		}
		Diagnostics.Assert(this.battleTargetingController != null);
		Diagnostics.Assert(this.aliveUnits != null);
		UnitTargetingIntention unitTargetingIntention = unit.Contender.GetTargetingIntentionByUnit(unit.UnitGUID);
		bool flag = false;
		if (unitTargetingIntention != null)
		{
			if (unitTargetingIntention.TargetingOnUnit)
			{
				BattleSimulationUnit battleSimulationUnitByGUID = this.GetBattleSimulationUnitByGUID(unitTargetingIntention.TargetUnitGUID);
				if (battleSimulationUnitByGUID != null && this.IsUnitValidAsTargetFor(unit, battleSimulationUnitByGUID))
				{
					flag = true;
				}
			}
			else if (this.IsWorldPositionValidAsTargetFor(unit, unitTargetingIntention.TargetPosition))
			{
				flag = true;
			}
		}
		if (!flag)
		{
			BattleSimulationTarget battleSimulationTarget = this.battleTargetingController.ComputeTarget(unit, this.potentialBattleSimulationTargets);
			if (battleSimulationTarget != null)
			{
				battleSimulationTarget.AddTargeter(unit);
				unitTargetingIntention = new UnitTargetingIntention(unit.UnitGUID, battleSimulationTarget, null);
			}
			else
			{
				unitTargetingIntention = null;
			}
		}
		else if (unit.Target != null)
		{
			BattleSimulationTarget potentialTargetByPosition = this.GetPotentialTargetByPosition(unit.Target.StaticPosition);
			potentialTargetByPosition.AddTargeter(unit);
		}
		if (unitTargetingIntention != null && unitTargetingIntention.TargetingOnUnit && unitTargetingIntention.TargetUnitGUID == unit.UnitGUID)
		{
			unitTargetingIntention = null;
		}
		unit.SetTargetingIntention(unitTargetingIntention);
	}

	public BattleUnitState_Death Death(BattleSimulationUnit unit, int stateIDToSyncWith, BattleUnitState.StateSynchronization.StateSyncMode syncMode = BattleUnitState.StateSynchronization.StateSyncMode.StateBegin)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		if (!unit.IsDead)
		{
			return null;
		}
		if (unit.HasPerformedDeath)
		{
			return null;
		}
		unit.HasPerformedDeath = true;
		int num = 0;
		if (unit.SimulationObject.Tags.Contains("UnitActionParasite"))
		{
			num = 1;
		}
		BattleUnitState_Death battleUnitState_Death = new BattleUnitState_Death(unit, unit.IsComputingMainStates);
		battleUnitState_Death.SyncWithStateID(stateIDToSyncWith, syncMode);
		this.battleUnitActionController.InitStateUnitActions(battleUnitState_Death, unit, BattleActionUnit.ActionType.Death);
		this.AddUnitState(unit, battleUnitState_Death, true);
		this.pathfindingWorldContextByContenderGroup[(int)unit.Contender.Group].SetTileContext(unit.Position, BattleZoneAnalysis.BattleNoUnitSpecification);
		this.pathfindingWorldContextByContenderGroup[(int)(1 - unit.Contender.Group)].SetTileContext(unit.Position, BattleZoneAnalysis.BattleNoUnitSpecification);
		this.UnapplyGroundActions(unit, battleUnitState_Death);
		this.ExecuteUnitState(battleUnitState_Death, unit, new BattleSimulationTarget(unit), false);
		GameEntityGUID killerUnitGUID = GameEntityGUID.Zero;
		List<BattleSimulationUnit> list = unit.Attackers.FindAll((BattleSimulationUnit attacker) => !attacker.IsDead);
		if (list.Count > 0)
		{
			BattleSimulationTarget target = new BattleSimulationTarget(unit);
			for (int i = 0; i < list.Count; i++)
			{
				BattleSimulationUnit battleSimulationUnit = list[i];
				BattleUnitState_TargetDeath battleUnitState_TargetDeath = new BattleUnitState_TargetDeath(battleSimulationUnit, battleSimulationUnit.IsComputingMainStates);
				battleUnitState_TargetDeath.SyncWithStateID(battleUnitState_Death.StateID, BattleUnitState.StateSynchronization.StateSyncMode.StateBegin);
				this.AddUnitState(battleSimulationUnit, battleUnitState_TargetDeath, true);
				this.battleUnitActionController.InitStateUnitActions(battleUnitState_TargetDeath, battleSimulationUnit, BattleActionUnit.ActionType.TargetDeath);
				this.ExecuteUnitState(battleUnitState_TargetDeath, battleSimulationUnit, target, false);
				killerUnitGUID = battleSimulationUnit.UnitGUID;
			}
		}
		this.battleActionController.OnUnitDeath(unit, battleUnitState_Death.AdditivePostInstructions);
		unit.SetTargetingIntention(null);
		if (unit.IsDead)
		{
			UnitPerformDeathInstruction item = new UnitPerformDeathInstruction(unit.UnitGUID, killerUnitGUID);
			battleUnitState_Death.AdditivePostInstructions.Add(item);
			if (unit.IsBattleSimulated)
			{
				IEventService service = Services.GetService<IEventService>();
				service.Notify(new EventAutomaticBattleUnitDeath(unit.Unit.Garrison.Empire, unit.UnitGUID, killerUnitGUID));
			}
			unit.Attackers.Clear();
			this.UpdateUnitPositionInPotentialTargets(unit, WorldPosition.Invalid);
			unit.ResetBattleMorale(battleUnitState_Death.AdditivePostInstructions);
			unit.ResetAltitude(battleUnitState_Death.AdditivePostInstructions);
			Diagnostics.Assert(unit.BattleSimulationArmy != null);
			unit.BattleSimulationArmy.RefreshAttachment(unit);
		}
		else
		{
			unit.HasPerformedDeath = false;
			num = 0;
		}
		unit.Contender.DeadParasitesCount += num;
		return battleUnitState_Death;
	}

	public void ExecuteUnitState(IBattleUnitState state, BattleSimulationUnit unit, BattleSimulationTarget target, bool checkDeath = true)
	{
		BattleSimulationTarget[] targets = new BattleSimulationTarget[]
		{
			target
		};
		this.ExecuteUnitState(state, unit, targets, checkDeath);
	}

	public bool SwitchSides(BattleSimulationUnit unit, BattleSimulationUnit initiator, List<IUnitReportInstruction> instructionsList)
	{
		UnitSwitchSidesInstruction item = new UnitSwitchSidesInstruction(unit.UnitGUID, initiator.Contender.GUID);
		instructionsList.Add(item);
		unit.BattleSimulationArmy.RemoveUnit(unit);
		unit.BattleSimulationArmy = initiator.BattleSimulationArmy;
		initiator.BattleSimulationArmy.AddUnit(unit);
		unit.Refresh(true);
		unit.BattleSimulationArmy.Refresh(true);
		initiator.BattleSimulationArmy.Refresh(true);
		unit.Contender.Garrison.RemoveUnit(unit.UnitGUID);
		unit.Contender = initiator.Contender;
		initiator.Contender.Garrison.AddUnit(unit.Unit);
		for (int i = 0; i < this.aliveUnits.Count; i++)
		{
			UnitTargetingIntention targetingIntentionByUnit = this.aliveUnits[i].Contender.GetTargetingIntentionByUnit(this.aliveUnits[i].UnitGUID);
			if (targetingIntentionByUnit != null && targetingIntentionByUnit.TargetingOnUnit && targetingIntentionByUnit.TargetUnitGUID == unit.UnitGUID)
			{
				this.aliveUnits[i].Contender.SetTargetingIntentionForUnit(this.aliveUnits[i].UnitGUID, null);
			}
		}
		return true;
	}

	public void TeleportUnit(BattleSimulationUnit unit, BattleEffect_Teleport.TeleportationMode teleportationMode, List<IUnitReportInstruction> instructionsList = null)
	{
		if (teleportationMode == BattleEffect_Teleport.TeleportationMode.Nearest)
		{
			WorldPosition nearestFreePosition = this.GetNearestFreePosition(unit.Position);
			if (nearestFreePosition.IsValid)
			{
				UnitActionTeleportInstruction unitActionTeleportInstruction = new UnitActionTeleportInstruction(unit.UnitGUID);
				unitActionTeleportInstruction.Path = new WorldPosition[]
				{
					unit.Position,
					nearestFreePosition
				};
				unit.SetPosition(nearestFreePosition, instructionsList);
				unit.ReportInstructions.Add(unitActionTeleportInstruction);
			}
		}
	}

	public void CheckUnitsDeath(int stateIDToSyncWith)
	{
		bool flag = false;
		for (int i = 0; i < this.aliveUnits.Count; i++)
		{
			BattleSimulationUnit battleSimulationUnit = this.aliveUnits[i];
			if (battleSimulationUnit.Position.IsValid && battleSimulationUnit.IsDead && !battleSimulationUnit.HasPerformedDeath)
			{
				BattleUnitState_Death battleUnitState_Death = this.Death(battleSimulationUnit, stateIDToSyncWith, BattleUnitState.StateSynchronization.StateSyncMode.StateEnd);
				if (battleUnitState_Death != null)
				{
					stateIDToSyncWith = battleUnitState_Death.StateID;
					flag = true;
				}
			}
		}
		if (flag)
		{
			this.CheckUnitsDeath(stateIDToSyncWith);
		}
	}

	private void AddUnitState(BattleSimulationUnit unit, BattleUnitState state, bool computeDeactivation = true)
	{
		unit.AddUnitState(state);
	}

	private BattleUnitState_Attack BuildBattleUnitStateAttack(BattleSimulationUnit unit, BattleSimulationTarget target)
	{
		Diagnostics.Assert(this.worldPositionningService != null);
		WorldOrientation orientation = this.worldPositionningService.GetOrientation(unit.Position, target.DynamicPosition);
		BattleUnitState_Attack battleUnitState_Attack = new BattleUnitState_Attack(unit, unit.IsComputingMainStates);
		battleUnitState_Attack.Target = target;
		battleUnitState_Attack.Orientation = orientation;
		unit.Orientation = orientation;
		return battleUnitState_Attack;
	}

	private BattleUnitState_Move BuildBattleUnitStateMove(BattleSimulationUnit unit, int range = 0)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		if (unit.Target == null || !unit.Target.DynamicPosition.IsValid)
		{
			return null;
		}
		if (unit.Target.Unit == unit)
		{
			return null;
		}
		WorldPosition worldPosition = WorldPosition.Invalid;
		UnitTargetingIntention targetingIntentionByUnit = unit.Contender.GetTargetingIntentionByUnit(unit.UnitGUID);
		if (targetingIntentionByUnit != null && targetingIntentionByUnit.SecondaryIntention != null && !targetingIntentionByUnit.SecondaryIntention.TargetingOnUnit)
		{
			worldPosition = targetingIntentionByUnit.SecondaryIntention.TargetPosition;
		}
		WorldPosition targetPosition = unit.Target.DynamicPosition;
		if (unit.Target.Unit != null)
		{
			List<WorldPosition> validPositionsForAttack = this.GetValidPositionsForAttack(unit, unit.Target.Unit);
			if (validPositionsForAttack != null)
			{
				int count = validPositionsForAttack.Count;
				if (count > 0)
				{
					if (count == 1)
					{
						targetPosition = validPositionsForAttack[0];
						range = 0;
					}
					else
					{
						bool flag = false;
						if (worldPosition.IsValid && validPositionsForAttack.Contains(worldPosition))
						{
							targetPosition = worldPosition;
							range = 0;
							flag = (this.FindPath(unit, targetPosition, range) != null);
						}
						if (!flag)
						{
							WorldPosition worldPosition2 = this.battleZoneAnalysis.SelectWorldPosition(unit.Contender.Group, unit.Unit, validPositionsForAttack, unit.Position, this.pathfindingWorldContextByContenderGroup[(int)unit.Contender.Group], this.GetOpponentArmy(unit.Contender.Group));
							if (worldPosition2.IsValid)
							{
								targetPosition = worldPosition2;
								range = 0;
							}
						}
					}
				}
			}
		}
		WorldPosition[] array = this.FindPath(unit, targetPosition, range);
		if (array == null || array.Length <= 1)
		{
			return null;
		}
		WorldOrientation orientation = WorldPosition.GetOrientation(unit.Position, unit.Target.DynamicPosition, false, 0);
		return new BattleUnitState_Move(unit, unit.IsComputingMainStates)
		{
			Path = array,
			FinalOrientation = orientation,
			MovementPointUsed = 0f
		};
	}

	private void BattleSimulationUnit_PositionUpdateHandler(BattleSimulationUnit sender, WorldPosition oldPosition, WorldPosition newPosition)
	{
		this.UpdateUnitPositionInPotentialTargets(sender, newPosition);
	}

	private void ComputeActivation(BattleSimulationUnit unit, int stateIDToSyncWith)
	{
		BattleUnitState battleUnitState = new BattleUnitState_Activation(unit, true);
		battleUnitState.SyncWithStateID(stateIDToSyncWith, BattleUnitState.StateSynchronization.StateSyncMode.StateBegin);
		this.battleUnitActionController.InitStateUnitActions(battleUnitState, unit, BattleActionUnit.ActionType.Activation);
		this.AddUnitState(unit, battleUnitState, false);
		BattleSimulationTarget target = new BattleSimulationTarget(unit);
		this.ExecuteUnitState(battleUnitState, unit, target, true);
	}

	private void ComputeDeactivation(BattleSimulationUnit unit)
	{
		if (unit.IsDead)
		{
			return;
		}
		BattleUnitState state = new BattleUnitState_Deactivation(unit, true);
		this.battleUnitActionController.InitStateUnitActions(state, unit, BattleActionUnit.ActionType.Deactivation);
		this.AddUnitState(unit, state, false);
		BattleSimulationTarget target = new BattleSimulationTarget(unit);
		this.ExecuteUnitState(state, unit, target, true);
	}

	private void ComputeRetreat(BattleSimulationUnit unit)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		if (!unit.Contender.IsRetreating)
		{
			return;
		}
		BattleUnitState state = new BattleUnitState_Retreat(unit, unit.IsComputingMainStates);
		this.battleUnitActionController.InitStateUnitActions(state, unit, BattleActionUnit.ActionType.Retreat);
		this.AddUnitState(unit, state, true);
		BattleSimulationTarget target = new BattleSimulationTarget(unit);
		this.ExecuteUnitState(state, unit, target, true);
	}

	private void ComputeStates(BattleSimulationUnit unit)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		if (unit.HasUnitActions)
		{
			return;
		}
		if (unit.IsDead)
		{
			return;
		}
		this.StartComputingMainStates(unit, BattleSimulation.UnitStateInvalidID);
		if (unit.IsDead)
		{
			this.StopComputingMainStates(unit);
			return;
		}
		int count = unit.UnitStates.Count;
		if (unit.CanPlayBattleRound)
		{
			this.ComputeTarget(unit);
			if (unit.Target != null)
			{
				if (unit.Target.Unit == null || unit.Target.Unit.UnitGUID == unit.UnitGUID)
				{
					if (unit.Target.DynamicPosition.IsValid)
					{
						if (unit.GetPropertyValue(SimulationProperties.BlindAttack) > 0f)
						{
							this.ComputeAttack(unit);
						}
						else
						{
							IBattleUnitState battleUnitState = this.ComputeMovement(unit, 0);
							if (battleUnitState != null)
							{
								this.ExecuteMovement(unit, battleUnitState);
							}
							this.ComputeOpportunityAttack(unit);
						}
					}
				}
				else
				{
					int range = (int)Math.Truncate((double)unit.GetPropertyValue(SimulationProperties.BattleRange));
					if (this.CanAttackTarget(unit, unit.Target) && unit.GetPreferredPositionToAttackUnit(unit.Target.Unit) == WorldPosition.Invalid)
					{
						this.ComputeAttack(unit);
					}
					else
					{
						IBattleUnitState battleUnitState2 = this.ComputeMovement(unit, range);
						if (battleUnitState2 != null)
						{
							this.ExecuteMovement(unit, battleUnitState2);
							if (battleUnitState2 is BattleUnitState_Move)
							{
								unit.Orientation = (battleUnitState2 as BattleUnitState_Move).FinalOrientation;
							}
							if (this.CanAttackTarget(unit, unit.Target))
							{
								this.ComputeAttack(unit);
							}
							else
							{
								this.ComputeOpportunityAttack(unit);
							}
						}
						else
						{
							this.ComputeOpportunityAttack(unit);
						}
					}
				}
			}
		}
		if (unit.UnitStates.Count - count <= 0 && !unit.IsDead)
		{
			BattleUnitState state = new BattleUnitState_Wait(unit, unit.IsComputingMainStates);
			this.battleUnitActionController.InitStateUnitActions(state, unit, BattleActionUnit.ActionType.Wait);
			this.AddUnitState(unit, state, true);
			BattleSimulationTarget target = new BattleSimulationTarget(unit.Position);
			this.ExecuteUnitState(state, unit, target, true);
		}
		this.StopComputingMainStates(unit);
	}

	private void ComputeOpportunityAttack(BattleSimulationUnit unit)
	{
		if (unit.IsDead)
		{
			return;
		}
		List<BattleSimulationTarget> list = new List<BattleSimulationTarget>();
		for (int i = 0; i < this.potentialBattleSimulationTargets.Length; i++)
		{
			BattleSimulationTarget battleSimulationTarget = this.potentialBattleSimulationTargets[i];
			if (battleSimulationTarget.Unit != null && !battleSimulationTarget.Unit.IsDead && this.CanTargetUnit(unit, battleSimulationTarget.Unit) && this.CanAttackTarget(unit, battleSimulationTarget))
			{
				list.Add(battleSimulationTarget);
			}
		}
		IDatabase<BattleTargetingStrategy> database = Databases.GetDatabase<BattleTargetingStrategy>(false);
		Diagnostics.Assert(database != null);
		BattleSimulationTarget battleSimulationTarget2 = null;
		BattleSimulationUnit preferredTargetUnit = null;
		UnitTargetingIntention targetingIntentionByUnit = unit.Contender.GetTargetingIntentionByUnit(unit.UnitGUID);
		if (targetingIntentionByUnit != null && targetingIntentionByUnit.SecondaryIntention != null && targetingIntentionByUnit.SecondaryIntention.TargetingOnUnit)
		{
			preferredTargetUnit = this.GetBattleSimulationUnitByGUID(targetingIntentionByUnit.SecondaryIntention.TargetUnitGUID);
		}
		if (preferredTargetUnit != null)
		{
			if (preferredTargetUnit.IsDead)
			{
				targetingIntentionByUnit.SecondaryIntention = null;
				unit.SetTargetingIntention(targetingIntentionByUnit);
			}
			else
			{
				BattleSimulationTarget battleSimulationTarget3 = list.Find((BattleSimulationTarget match) => match.Unit == preferredTargetUnit);
				if (battleSimulationTarget3 != null)
				{
					battleSimulationTarget2 = battleSimulationTarget3;
				}
			}
		}
		if (battleSimulationTarget2 == null)
		{
			BattleTargetingStrategy[] values = database.GetValues();
			StaticString staticString = null;
			if (values.Length > 0)
			{
				foreach (BattleTargetingStrategy battleTargetingStrategy in values)
				{
					if (battleTargetingStrategy.UsedForOpportunityAttack)
					{
						staticString = battleTargetingStrategy.Name;
						break;
					}
				}
			}
			if (staticString != null)
			{
				unit.TargetingStrategyCurrent = staticString;
				unit.UpdateBattleTargetingUnitBehaviorWeight();
				battleSimulationTarget2 = this.battleTargetingController.ComputeTarget(unit, list.ToArray());
				unit.TargetingStrategyCurrent = unit.TargetingStrategyWanted;
				unit.UpdateBattleTargetingUnitBehaviorWeight();
			}
		}
		if (battleSimulationTarget2 != null)
		{
			battleSimulationTarget2.AddTargeter(unit);
			this.ComputeAttackAgainst(unit, battleSimulationTarget2.Duplicate());
		}
		if (unit.Target != null)
		{
			BattleSimulationTarget potentialTargetByPosition = this.GetPotentialTargetByPosition(unit.Target.StaticPosition);
			potentialTargetByPosition.RemoveTargeter(unit);
		}
	}

	private void ComputeAttack(BattleSimulationUnit unit)
	{
		this.ComputeAttackAgainst(unit, unit.Target);
	}

	private void ComputeAttackAgainst(BattleSimulationUnit unit, BattleSimulationTarget target)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		if (unit.IsDead)
		{
			return;
		}
		Diagnostics.Assert(unit.Target.DynamicPosition.IsValid);
		Diagnostics.Assert(this.battleUnitActionController != null);
		if (target.Unit != null && unit.IsAlliedWith(target.Unit))
		{
			this.ComputeAttackSupport(unit, target);
			return;
		}
		BattleUnitState_Attack battleUnitState_Attack = this.BuildBattleUnitStateAttack(unit, target);
		this.battleUnitActionController.InitAttackStateUnitActions(battleUnitState_Attack, unit, target, BattleActionUnit.ActionType.Attack);
		if (battleUnitState_Attack == null || battleUnitState_Attack.UnitStateBattleActions.Count == 0)
		{
			return;
		}
		if (unit.Target != null && unit.Target.Unit == unit && (target.Unit == null || target.Unit == unit))
		{
			return;
		}
		BattleUnitState battleUnitState = new BattleUnitState_PrepareAttack(unit, unit.IsComputingMainStates);
		if (unit.Target == null || unit.Target.Unit != target.Unit)
		{
			unit.SetTarget(target.Unit, battleUnitState.AdditiveInstructions);
		}
		this.battleUnitActionController.InitStateUnitActions(battleUnitState, unit, BattleActionUnit.ActionType.PrepareAttack);
		this.AddUnitState(unit, battleUnitState, true);
		this.ExecuteUnitState(battleUnitState, unit, target, true);
		bool flag = false;
		BattleSimulationUnit unit2 = target.Unit;
		if (unit2 != null)
		{
			if (unit2.IsDead)
			{
				return;
			}
			flag = (battleUnitState_Attack.Defensible && unit2.CanPlayBattleRound && unit2.CanCounter() && unit2.CanPerformNewAttack() && this.CanAttackTarget(unit2, unit.Position));
			bool flag2 = flag && !target.Unit.HasUnitMainStates;
			if (flag2)
			{
				this.StartComputingMainStates(unit2, battleUnitState.StateID);
			}
			if (!unit2.IsDead)
			{
				BattleUnitState battleUnitState2;
				if (!flag)
				{
					battleUnitState2 = new BattleUnitState_PrepareHit(unit2, unit2.IsComputingMainStates);
					this.battleUnitActionController.InitStateUnitActions(battleUnitState2, unit2, BattleActionUnit.ActionType.PrepareHit);
				}
				else
				{
					battleUnitState2 = new BattleUnitState_PrepareDefense(unit2, unit2.IsComputingMainStates);
					unit2.SetTarget(unit, battleUnitState2.AdditiveInstructions);
					this.battleUnitActionController.InitStateUnitActions(battleUnitState2, unit2, BattleActionUnit.ActionType.PrepareDefense);
				}
				if (battleUnitState2 != null)
				{
					battleUnitState2.SyncWithStateID(battleUnitState.StateID, BattleUnitState.StateSynchronization.StateSyncMode.StateBegin);
					this.AddUnitState(unit2, battleUnitState2, true);
					BattleSimulationTarget target2 = new BattleSimulationTarget(unit);
					this.ExecuteUnitState(battleUnitState2, unit2, target2, true);
					battleUnitState_Attack.SyncWithStateID(battleUnitState2.StateID, BattleUnitState.StateSynchronization.StateSyncMode.StateEnd);
				}
			}
		}
		if (unit.IsDead || unit2.IsDead)
		{
			return;
		}
		this.AddUnitState(unit, battleUnitState_Attack, true);
		this.ExecuteAttack(battleUnitState_Attack);
		if (unit2 != null)
		{
			unit2.AddAttacker(unit);
			if (unit2 != unit)
			{
				BattleUnitState_Defense battleUnitState_Defense = new BattleUnitState_Defense(unit2, unit2.IsComputingMainStates);
				battleUnitState_Defense.Attacker = unit;
				battleUnitState_Defense.SyncWithStateID(battleUnitState_Attack.StateID, BattleUnitState.StateSynchronization.StateSyncMode.StateBegin);
				if (flag)
				{
					battleUnitState_Defense.AddAttack(battleUnitState_Attack);
				}
				else
				{
					this.battleUnitActionController.InitStateUnitActions(battleUnitState_Defense, unit2, BattleActionUnit.ActionType.Hit);
				}
				this.AddUnitState(unit2, battleUnitState_Defense, true);
				this.ExecuteDefense(unit2, battleUnitState_Defense);
			}
			this.CheckUnitsDeath(battleUnitState_Attack.StateID);
			this.StopComputingMainStates(unit2);
		}
	}

	private void ComputeAttackSupport(BattleSimulationUnit unit, BattleSimulationTarget target)
	{
		BattleUnitState_Attack battleUnitState_Attack = this.BuildBattleUnitStateAttack(unit, target);
		this.battleUnitActionController.InitAttackStateUnitActions(battleUnitState_Attack, unit, target, BattleActionUnit.ActionType.Support);
		this.AddUnitState(unit, battleUnitState_Attack, true);
		this.ExecuteAttack(battleUnitState_Attack);
		if (unit != target.Unit)
		{
			BattleUnitState_Defense battleUnitState_Defense = new BattleUnitState_Defense(target.Unit, target.Unit.IsComputingMainStates);
			battleUnitState_Defense.Attacker = unit;
			battleUnitState_Defense.SyncWithStateID(battleUnitState_Attack.StateID, BattleUnitState.StateSynchronization.StateSyncMode.StateBegin);
			this.battleUnitActionController.InitStateUnitActions(battleUnitState_Defense, target.Unit, BattleActionUnit.ActionType.Hit);
			this.AddUnitState(target.Unit, battleUnitState_Defense, true);
			this.ExecuteDefense(target.Unit, battleUnitState_Defense);
		}
		this.CheckUnitsDeath(battleUnitState_Attack.StateID);
		unit.SetTargetingIntention(null);
	}

	private void ComputeMorale(BattleSimulationUnit unit)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		Diagnostics.Assert(this.battleSimulationArmies != null);
		Diagnostics.Assert(this.worldAtlas != null);
		GridMap<sbyte> gridMap = this.worldAtlas.GetMap(WorldAtlas.Maps.Height) as GridMap<sbyte>;
		Diagnostics.Assert(gridMap != null);
		float num = unit.GetPropertyValue(SimulationProperties.BattleMoraleFromGround);
		float propertyValue = unit.GetPropertyValue(SimulationProperties.BattleMoraleBonusPerAlly);
		float propertyValue2 = unit.GetPropertyValue(SimulationProperties.BattleMoraleBonusPerEnemy);
		float num2 = 0f;
		for (int i = 0; i < this.battleSimulationArmies.Count; i++)
		{
			BattleSimulationArmy battleSimulationArmy = this.battleSimulationArmies[i];
			Diagnostics.Assert(battleSimulationArmy != null && battleSimulationArmy.Units != null);
			for (int j = 0; j < battleSimulationArmy.Units.Count; j++)
			{
				BattleSimulationUnit battleSimulationUnit = battleSimulationArmy.Units[j];
				Diagnostics.Assert(battleSimulationUnit != null);
				if (battleSimulationUnit != unit)
				{
					if (!battleSimulationUnit.IsDead)
					{
						Diagnostics.Assert(this.worldPositionningService != null);
						if (this.worldPositionningService.GetDistance(unit.Position, battleSimulationUnit.Position) <= 1)
						{
							Diagnostics.Assert(battleSimulationUnit.BattleSimulationArmy != null);
							Diagnostics.Assert(unit.BattleSimulationArmy != null);
							if (battleSimulationUnit.BattleSimulationArmy.Group == unit.BattleSimulationArmy.Group)
							{
								num += propertyValue;
							}
							else
							{
								num += propertyValue2;
								int num3 = (int)gridMap.GetValue(battleSimulationUnit.Position) - (int)gridMap.GetValue(unit.Position);
								num += num2 * (float)num3;
							}
						}
					}
				}
			}
		}
		float num4 = unit.GetPropertyValue(SimulationProperties.BattleMoraleCumulated);
		if (unit.IsDefending)
		{
			num4 += unit.GetPropertyValue(SimulationProperties.BattleMoraleBonusPerDefense);
		}
		else if (unit.IsAttacking)
		{
			num4 += unit.GetPropertyValue(SimulationProperties.BattleMoraleBonusPerAttack);
		}
		unit.SetPropertyBaseValue(SimulationProperties.BattleMoraleCumulated, (float)Math.Truncate((double)num4), false, null);
		num += unit.GetPropertyValue(SimulationProperties.BattleMoraleBonus);
		unit.SetPropertyBaseValue(SimulationProperties.BattleMorale, (float)Math.Truncate((double)num), false, null);
	}

	private IBattleUnitState ComputeMovement(BattleSimulationUnit unit, int range = 0)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		if (unit.IsDead)
		{
			return null;
		}
		BattleUnitState_Move battleUnitState_Move = this.BuildBattleUnitStateMove(unit, range);
		if (battleUnitState_Move != null)
		{
			this.battleUnitActionController.InitStateUnitActions(battleUnitState_Move, unit, BattleActionUnit.ActionType.Move);
			this.AddUnitState(unit, battleUnitState_Move, true);
		}
		return battleUnitState_Move;
	}

	private void ComputeTarget(BattleSimulationUnit unit)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		Diagnostics.Assert(this.battleTargetingController != null);
		Diagnostics.Assert(this.aliveUnits != null);
		UnitTargetingIntention targetingIntentionByUnit = unit.Contender.GetTargetingIntentionByUnit(unit.UnitGUID);
		bool flag = false;
		if (targetingIntentionByUnit != null)
		{
			if (targetingIntentionByUnit.TargetingOnUnit)
			{
				BattleSimulationUnit battleSimulationUnitByGUID = this.GetBattleSimulationUnitByGUID(targetingIntentionByUnit.TargetUnitGUID);
				if (battleSimulationUnitByGUID != null && this.IsUnitValidAsTargetFor(unit, battleSimulationUnitByGUID))
				{
					unit.SetTarget(battleSimulationUnitByGUID, null);
					flag = true;
				}
			}
			else if (this.IsWorldPositionValidAsTargetFor(unit, targetingIntentionByUnit.TargetPosition))
			{
				unit.SetTarget(targetingIntentionByUnit.TargetPosition);
				flag = true;
			}
		}
		if (!flag)
		{
			if (targetingIntentionByUnit != null)
			{
				unit.Contender.SetTargetingIntentionForUnit(unit.UnitGUID, null);
			}
			BattleSimulationTarget battleSimulationTarget = this.battleTargetingController.ComputeTarget(unit, this.potentialBattleSimulationTargets);
			if (battleSimulationTarget != null)
			{
				battleSimulationTarget.AddTargeter(unit);
				unit.SetTarget(battleSimulationTarget);
			}
		}
		else if (unit.Target != null)
		{
			BattleSimulationTarget potentialTargetByPosition = this.GetPotentialTargetByPosition(unit.Target.StaticPosition);
			potentialTargetByPosition.AddTargeter(unit);
		}
	}

	private void ExecuteAttack(BattleUnitState_Attack attack)
	{
		if (attack == null)
		{
			throw new ArgumentNullException("attack");
		}
		Diagnostics.Assert(this.battleActionController != null);
		this.ExecuteUnitState(attack, attack.Unit, attack.Target, false);
		attack.Unit.SetPropertyBaseValue(SimulationProperties.AttackPerRoundDone, attack.Unit.GetPropertyValue(SimulationProperties.AttackPerRoundDone) + 1f, true, attack.AdditivePostInstructions);
		attack.Target.Unit.SetPropertyBaseValue(SimulationProperties.AttackPerRoundTaken, attack.Unit.GetPropertyValue(SimulationProperties.AttackPerRoundTaken) + 1f, true, null);
	}

	private void ExecuteDefense(BattleSimulationUnit unit, BattleUnitState_Defense defense)
	{
		int num = (int)Math.Truncate((double)unit.GetPropertyValue(SimulationProperties.BattleRange));
		if (num < 1)
		{
			return;
		}
		BattleSimulationTarget target = new BattleSimulationTarget(defense.Attacker);
		this.ExecuteUnitState(defense, unit, target, false);
		if (defense.StrikeBacks.Count > 0)
		{
			BattleUnitState_Defense.StrikeBack value = defense.StrikeBacks[0];
			BattleUnitState_Attack attack = value.Attack;
			Diagnostics.Assert(attack != null);
			BattleSimulationTarget target2 = new BattleSimulationTarget(attack.Unit);
			value.ReponseAttack = this.BuildBattleUnitStateAttack(attack.Target.Unit, target2);
			this.battleUnitActionController.InitAttackStateUnitActions(value.ReponseAttack, attack.Target.Unit, target2, BattleActionUnit.ActionType.Defense);
			defense.StrikeBacks[0] = value;
			if (value.ReponseAttack != null)
			{
				this.ExecuteAttack(value.ReponseAttack);
			}
		}
		for (int i = defense.StrikeBacks.Count - 1; i >= 1; i--)
		{
			defense.StrikeBacks.RemoveAt(i);
		}
	}

	private void ExecuteMovement(BattleSimulationUnit unit, IBattleUnitState moveState)
	{
		if (moveState is BattleUnitState_Move)
		{
			BattleUnitState_Move battleUnitState_Move = moveState as BattleUnitState_Move;
			if (battleUnitState_Move == null || battleUnitState_Move.Path == null)
			{
				return;
			}
			if (battleUnitState_Move.Path.Length == 0)
			{
				Diagnostics.LogWarning("This should not happen");
				return;
			}
			Diagnostics.Assert(this.pathfindingService != null);
			float num = unit.GetCurrentPropertyValue(SimulationProperties.BattleMovement);
			float num2 = 0f;
			for (int i = 0; i < battleUnitState_Move.Path.Length - 1; i++)
			{
				num2 += this.pathfindingService.GetTransitionCost(battleUnitState_Move.Path[i], battleUnitState_Move.Path[i + 1], unit, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreEncounterAreas | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges, null);
			}
			if (float.IsPositiveInfinity(num2))
			{
				battleUnitState_Move.SetPathLength(0);
				return;
			}
			num -= num2;
			battleUnitState_Move.MovementPointUsed += num2;
			unit.SetPropertyBaseValue(SimulationProperties.SpentBattleMovement, unit.GetCurrentPropertyValue(SimulationProperties.SpentBattleMovement) + num2, true, moveState.AdditivePostInstructions);
			unit.RefreshPathfindingContextMovementPoints();
			this.UnapplyGroundActions(unit, moveState);
			this.pathfindingWorldContextByContenderGroup[(int)unit.Contender.Group].SetTileContext(unit.Position, BattleZoneAnalysis.BattleNoUnitSpecification);
			this.pathfindingWorldContextByContenderGroup[(int)(1 - unit.Contender.Group)].SetTileContext(unit.Position, BattleZoneAnalysis.BattleNoUnitSpecification);
			unit.SetPosition(battleUnitState_Move.Path[battleUnitState_Move.Path.Length - 1], moveState.AdditivePostInstructions);
			this.pathfindingWorldContextByContenderGroup[(int)unit.Contender.Group].SetTileContext(unit.Position, BattleZoneAnalysis.BattleSameGroupUnitSpecification);
			this.pathfindingWorldContextByContenderGroup[(int)(1 - unit.Contender.Group)].SetTileContext(unit.Position, BattleZoneAnalysis.BattleEnemyGroupUnitSpecification);
			this.ExecuteGroundAction(unit, unit.Position);
			this.ApplyGroundActions(unit, moveState);
		}
		BattleSimulationTarget target = new BattleSimulationTarget(unit);
		this.ExecuteUnitState(moveState, unit, target, true);
	}

	private void ExecuteUnitState(IBattleUnitState state, BattleSimulationUnit unit, BattleSimulationTarget[] targets, bool checkDeath = true)
	{
		Diagnostics.Assert(state.UnitStateBattleActions != null);
		for (int i = 0; i < state.UnitStateBattleActions.Count; i++)
		{
			BattleUnitState.StateBattleAction stateBattleAction = state.UnitStateBattleActions[i];
			BattleSimulationUnit initiator = unit;
			if (stateBattleAction.ExternalInitiator != null)
			{
				initiator = stateBattleAction.ExternalInitiator;
			}
			Diagnostics.Assert(stateBattleAction.ReportInstructions != null);
			stateBattleAction.ReportInstructions.Clear();
			this.battleActionController.ExecuteBattleAction(stateBattleAction.BattleAction, stateBattleAction.BattleEffects, initiator, unit, targets, true, stateBattleAction.ReportInstructions);
		}
		if (checkDeath)
		{
			this.CheckUnitsDeath(state.StateID);
		}
	}

	private BattleSimulationUnit GetBattleSimulationUnitByGUID(GameEntityGUID unitGUID)
	{
		for (int i = 0; i < this.battleSimulationArmies.Count; i++)
		{
			Diagnostics.Assert(this.battleSimulationArmies[i] != null);
			ReadOnlyCollection<BattleSimulationUnit> units = this.battleSimulationArmies[i].Units;
			for (int j = 0; j < units.Count; j++)
			{
				if (units[j].UnitGUID == unitGUID)
				{
					return units[j];
				}
			}
		}
		return null;
	}

	private void InitActiveUnitCache()
	{
		Diagnostics.Assert(this.pathfindingWorldContextByContenderGroup != null);
		Diagnostics.Assert(this.fightingUnits != null);
		Diagnostics.Assert(this.aliveUnits != null);
		this.fightingUnits.Clear();
		this.aliveUnits.Clear();
		foreach (WorldPosition worldPosition in this.battleZone.GetWorldPositions())
		{
			this.pathfindingWorldContextByContenderGroup[0].SetTileContext(worldPosition, BattleZoneAnalysis.BattleNoUnitSpecification);
			this.pathfindingWorldContextByContenderGroup[1].SetTileContext(worldPosition, BattleZoneAnalysis.BattleNoUnitSpecification);
		}
		Diagnostics.Assert(this.battleSimulationArmies != null);
		for (int i = 0; i < this.battleSimulationArmies.Count; i++)
		{
			this.battleSimulationArmies[i].BestUnitRoundRank = int.MaxValue;
			Diagnostics.Assert(this.battleSimulationArmies[i] != null);
			this.battleSimulationArmies[i].Refresh(false);
			ReadOnlyCollection<BattleSimulationUnit> units = this.battleSimulationArmies[i].Units;
			for (int j = 0; j < units.Count; j++)
			{
				BattleSimulationUnit battleSimulationUnit = units[j];
				Diagnostics.Assert(battleSimulationUnit != null);
				if (battleSimulationUnit.Position.IsValid)
				{
					battleSimulationUnit.Refresh(false);
					if (!battleSimulationUnit.IsTraversable)
					{
						this.pathfindingWorldContextByContenderGroup[(int)this.battleSimulationArmies[i].Group].SetTileContext(battleSimulationUnit.Position, BattleZoneAnalysis.BattleSameGroupUnitSpecification);
						this.pathfindingWorldContextByContenderGroup[(int)(1 - this.battleSimulationArmies[i].Group)].SetTileContext(battleSimulationUnit.Position, BattleZoneAnalysis.BattleEnemyGroupUnitSpecification);
					}
					if (!battleSimulationUnit.IsDead)
					{
						this.aliveUnits.Add(battleSimulationUnit);
						if (battleSimulationUnit.GetPropertyValue(SimulationProperties.BattleRange) > 0f)
						{
							this.fightingUnits.Add(battleSimulationUnit);
						}
					}
				}
			}
		}
	}

	private bool IsUnitValidAsTargetFor(BattleSimulationUnit unit, BattleSimulationUnit target)
	{
		return !target.IsDead;
	}

	private bool IsWorldPositionValidAsTargetFor(BattleSimulationUnit unit, WorldPosition position)
	{
		return !(unit.Position == position) && this.FindPath(unit, position, 0) != null;
	}

	private void LogInfos(BattleSimulationUnit unit)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		string text = string.Empty;
		if (unit.Target != null)
		{
			text = ((unit.Target.Unit != null) ? string.Format("Army{0}.{1}", unit.Target.Unit.BattleSimulationArmy.Group, unit.Target.Unit.Name) : unit.Target.DynamicPosition.ToString());
		}
		string text2 = string.Empty;
		if (!unit.HasUnitStates)
		{
			text2 = "<none>";
		}
		else
		{
			for (int i = 0; i < unit.UnitStates.Count; i++)
			{
				text2 += unit.UnitStates[i].ToString();
				if (i < unit.UnitStates.Count - 1)
				{
					text2 += "||";
				}
			}
		}
		Diagnostics.Log("Unit state : Army{0}.{1}: Health={9}/{10} Defense={11}/{12} Range={2} Morale={3} CanBeHealed={13} CanTakePhysicalDamage={14} Movement Point={4}/{5} Position={8} Orientation={15} Target={6} Actions={7}.", new object[]
		{
			unit.BattleSimulationArmy.Group,
			unit.Name,
			unit.GetPropertyValue(SimulationProperties.BattleRange),
			unit.GetPropertyValue(SimulationProperties.BattleMorale),
			unit.GetCurrentPropertyValue(SimulationProperties.BattleMovement),
			unit.GetPropertyValue(SimulationProperties.BattleMaximumMovement),
			text,
			text2,
			unit.Position,
			unit.GetPropertyValue(SimulationProperties.Health),
			unit.GetPropertyValue(SimulationProperties.MaximumHealth),
			unit.GetPropertyValue(SimulationProperties.Armor),
			unit.GetPropertyValue(SimulationProperties.MaximumArmor),
			unit.GetPropertyValue(SimulationProperties.CanBeHealed),
			unit.GetPropertyValue(SimulationProperties.CanTakePhysicalDamage),
			unit.Orientation.ToString()
		});
	}

	private BattleSimulationArmy GetOpponentArmy(byte myGroup)
	{
		Diagnostics.Assert(myGroup == 0 || myGroup == 1);
		List<BattleSimulationArmy> list = this.armiesByGroup[myGroup ^ 1];
		Diagnostics.Assert(list.Count > 0);
		return list[0];
	}

	private void OnRoundBegin(BattleSimulationUnit unit)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		unit.OnRoundBegin();
	}

	private void OnRoundEnd(BattleSimulationUnit unit)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		unit.OnRoundEnd();
	}

	private void ReportInstructions(BattleSimulationUnit unit)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		Diagnostics.Assert(this.currentReport != null);
		Diagnostics.Assert(unit.CriticalInstructions != null);
		for (int i = 0; i < unit.CriticalInstructions.Count; i++)
		{
			this.currentReport.ReportCriticalInstruction(unit, unit.CriticalInstructions[i]);
		}
		unit.CriticalInstructions.Clear();
		if (unit.IsBattleSimulated)
		{
			if (unit.ReportInstructions != null)
			{
				for (int j = 0; j < unit.ReportInstructions.Count; j++)
				{
					if (unit.ReportInstructions[j] is UnitSpawnInstruction)
					{
						this.currentReport.ReportCriticalInstruction(unit, unit.ReportInstructions[j]);
					}
				}
			}
			for (int k = 0; k < unit.UnitStates.Count; k++)
			{
				if (unit.UnitStates[k].IsCriticalState)
				{
					IUnitReportInstruction[] array = unit.UnitStates[k].ReportUnitState();
					Diagnostics.Assert(array != null);
					for (int l = 0; l < array.Length; l++)
					{
						this.currentReport.ReportInstruction(unit, array[l], unit.UnitStates[k]);
					}
				}
			}
			return;
		}
		Diagnostics.Assert(unit.ReportInstructions != null);
		for (int m = 0; m < unit.ReportInstructions.Count; m++)
		{
			this.currentReport.DoReportInstruction(unit, unit.ReportInstructions[m], false);
		}
		unit.ReportInstructions.Clear();
		for (int n = 0; n < unit.UnitStates.Count; n++)
		{
			IUnitReportInstruction[] array2 = unit.UnitStates[n].ReportUnitState();
			Diagnostics.Assert(array2 != null);
			for (int num = 0; num < array2.Length; num++)
			{
				this.currentReport.ReportInstruction(unit, array2[num], unit.UnitStates[n]);
			}
		}
	}

	private void StartComputingMainStates(BattleSimulationUnit unit, int linkedStateID)
	{
		if (unit.HasComputedMainStates)
		{
			return;
		}
		if (unit.IsComputingMainStates)
		{
			return;
		}
		this.ComputeActivation(unit, linkedStateID);
		unit.IsComputingMainStates = true;
	}

	private void StopComputingMainStates(BattleSimulationUnit unit)
	{
		if (unit.IsDead)
		{
			return;
		}
		if (!unit.IsComputingMainStates)
		{
			return;
		}
		this.ComputeDeactivation(unit);
		unit.IsComputingMainStates = false;
		unit.HasComputedMainStates = true;
	}

	~BattleSimulation()
	{
		this.Dispose(false);
	}

	public BattleActionController BattleActionController
	{
		get
		{
			return this.battleActionController;
		}
	}

	public BattleUnitActionController BattleUnitActionController
	{
		get
		{
			return this.battleUnitActionController;
		}
	}

	public List<BattleSimulationArmy> BattleSimulationArmies
	{
		get
		{
			return this.battleSimulationArmies;
		}
	}

	public Amplitude.Utilities.Random BattleSimulationRandom { get; private set; }

	public WorldParameters WorldParameters { get; private set; }

	public BattleZone BattleZone
	{
		get
		{
			return this.battleZone;
		}
	}

	public void ApplyDescriptor(SimulationDescriptor descriptor)
	{
		if (descriptor == null)
		{
			throw new ArgumentNullException("descriptor");
		}
		Diagnostics.Assert(this.battleSimulationArmies != null);
		for (int i = 0; i < this.battleSimulationArmies.Count; i++)
		{
			BattleSimulationArmy battleSimulationArmy = this.battleSimulationArmies[i];
			Diagnostics.Assert(battleSimulationArmy != null);
			battleSimulationArmy.AddDescriptor(descriptor, false);
		}
	}

	public bool ChangeStrategy(BattleContender battleContender, StaticString strategy)
	{
		if (battleContender == null)
		{
			throw new ArgumentNullException("battleContender");
		}
		bool flag = false;
		Diagnostics.Assert(this.battleSimulationArmies != null);
		for (int i = 0; i < this.battleSimulationArmies.Count; i++)
		{
			BattleSimulationArmy battleSimulationArmy = this.battleSimulationArmies[i];
			Diagnostics.Assert(battleSimulationArmy != null);
			if (battleSimulationArmy.Group == battleContender.Group)
			{
				battleSimulationArmy.ArmyBattleTargetingWantedStrategy = strategy;
				flag = true;
				for (int j = 0; j < battleSimulationArmy.Units.Count; j++)
				{
					this.ChangeUnitStrategy(battleSimulationArmy.Units[j].UnitGUID, strategy);
				}
			}
		}
		if (!flag)
		{
			Diagnostics.Assert("No army with group {0} not found in battleSimulationArmies", new object[]
			{
				battleContender.Group
			});
			return false;
		}
		return true;
	}

	public bool ChangeUnitStrategy(GameEntityGUID unitGUID, StaticString strategy)
	{
		BattleSimulationUnit battleUnit = this.GetBattleUnit(unitGUID);
		if (battleUnit == null)
		{
			Diagnostics.LogWarning("The unit #{0} couldn't be found", new object[]
			{
				unitGUID
			});
			return false;
		}
		BattleTargetingStrategy battleTargetingStrategy;
		if (this.battleTargetingStrategyDatabase.TryGetValue(strategy, out battleTargetingStrategy))
		{
			battleUnit.TargetingStrategyWanted = strategy;
			if (battleTargetingStrategy.CancelTargetingIntentionOnSelection)
			{
				BattleContender contender = battleUnit.Contender;
				if (contender == null)
				{
					return false;
				}
				contender.SetTargetingIntentionForUnit(unitGUID, null);
			}
		}
		else
		{
			Diagnostics.LogWarning("Can't retrieve the unit battle strategy '{0}'.", new object[]
			{
				strategy
			});
		}
		return true;
	}

	public bool ChangeUnitTargeting(GameEntityGUID unitGUID, UnitTargetingIntention targetingIntention, out List<GameEntityGUID> availableOpportunityTargets)
	{
		BattleSimulationUnit battleSimulationUnitByGUID = this.GetBattleSimulationUnitByGUID(unitGUID);
		availableOpportunityTargets = null;
		if (battleSimulationUnitByGUID == null)
		{
			Diagnostics.LogWarning("The unit #{0} couldn't be found", new object[]
			{
				unitGUID
			});
			return false;
		}
		battleSimulationUnitByGUID.Contender.SetTargetingIntentionForUnit(unitGUID, targetingIntention);
		if (targetingIntention != null && !targetingIntention.TargetingOnUnit)
		{
			availableOpportunityTargets = this.BuildAvailableOpportunityTargetsForUnit(battleSimulationUnitByGUID, targetingIntention.TargetPosition);
		}
		return true;
	}

	public StaticString GetUnitStrategy(GameEntityGUID unitGUID)
	{
		BattleSimulationUnit battleUnit = this.GetBattleUnit(unitGUID);
		if (battleUnit == null)
		{
			Diagnostics.LogWarning("The unit #{0} couldn't be found", new object[]
			{
				unitGUID
			});
			return string.Empty;
		}
		return battleUnit.TargetingStrategyWanted;
	}

	public void Dispose()
	{
		this.Dispose(true);
	}

	public RoundReport DoDeployment()
	{
		this.currentReport = new RoundReportTargeting();
		this.InitActiveUnitCache();
		this.ManageUnitsSorting(false);
		Diagnostics.Assert(this.aliveUnits != null);
		for (int i = 0; i < this.aliveUnits.Count; i++)
		{
			this.aliveUnits[i].ClearUnitStates();
			this.currentReport.AddUnitReport(this.aliveUnits[i]);
			this.ReportInstructions(this.aliveUnits[i]);
		}
		return this.currentReport;
	}

	public RoundReport DoRetreat()
	{
		Diagnostics.Assert(this.battleSimulationArmies != null);
		BattleSimulation.UnitStateNextID = 0;
		this.currentReport = new RoundReport();
		Diagnostics.Assert(this.aliveUnits != null);
		this.aliveUnits.Clear();
		for (int i = 0; i < this.battleSimulationArmies.Count; i++)
		{
			if (this.battleSimulationArmies[i].Contender.IsRetreating && this.battleSimulationArmies[i].Contender.IsMainContender)
			{
				this.aliveUnits.AddRange(this.battleSimulationArmies[i].Units);
			}
		}
		for (int j = 0; j < this.aliveUnits.Count; j++)
		{
			this.aliveUnits[j].ClearUnitStates();
			this.currentReport.AddUnitReport(this.aliveUnits[j]);
			this.ComputeRetreat(this.aliveUnits[j]);
		}
		this.battleActionController.DoReportInstructions(this.currentReport, false);
		return this.currentReport;
	}

	public RoundReport DoRound(RoundReport baseReport = null, bool performRoundInitialization = true)
	{
		Diagnostics.Assert(this.battleSimulationArmies != null);
		BattleSimulation.UnitStateNextID = 0;
		if (baseReport == null)
		{
			this.currentReport = new RoundReport();
		}
		else
		{
			this.currentReport = baseReport;
		}
		if (performRoundInitialization)
		{
			this.InitRound();
		}
		Diagnostics.Assert(this.aliveUnits != null);
		Diagnostics.Assert(this.battleSimulationArmies != null);
		this.battleSimulationArmies.ForEach(new Action<BattleSimulationArmy>(this.OnRoundBegin));
		foreach (BattleSimulationArmy battleSimulationArmy in this.battleSimulationArmies)
		{
			battleSimulationArmy.ReportInstructions(this.currentReport);
		}
		this.aliveUnits.ForEach(new Action<BattleSimulationUnit>(this.OnRoundBegin));
		Diagnostics.Assert(this.battleActionController != null);
		this.battleActionController.OnRoundStart();
		for (int i = 0; i < this.aliveUnits.Count; i++)
		{
			this.ExecuteGroundAction(this.aliveUnits[i], this.aliveUnits[i].Position);
			this.ApplyGroundActions(this.aliveUnits[i], null);
		}
		this.ManageUnitsSorting(true);
		for (int j = 0; j < this.aliveUnits.Count; j++)
		{
			this.currentReport.AddUnitReport(this.aliveUnits[j]);
		}
		this.battleActionController.DoReportInstructions(this.currentReport, false);
		Diagnostics.Assert(this.aliveUnits != null);
		this.battleSimulationArmies.Sort((BattleSimulationArmy left, BattleSimulationArmy right) => left.BestUnitRoundRank.CompareTo(right.BestUnitRoundRank));
		foreach (BattleSimulationArmy battleSimulationArmy2 in this.battleSimulationArmies)
		{
			battleSimulationArmy2.ExecuteWaitingActions(this, this.currentReport);
		}
		this.aliveUnits.ForEach(new Action<BattleSimulationUnit>(this.ComputeStates));
		Diagnostics.Assert(this.aliveUnits != null);
		this.aliveUnits.ForEach(new Action<BattleSimulationUnit>(this.OnRoundEnd));
		this.aliveUnits.ForEach(new Action<BattleSimulationUnit>(this.ReportInstructions));
		Diagnostics.Assert(this.battleActionController != null);
		this.battleActionController.OnRoundEnd();
		this.battleActionController.DoReportInstructions(this.currentReport, true);
		return this.currentReport;
	}

	public RoundReport DoTargetingPhase()
	{
		this.currentReport = new RoundReportTargeting();
		this.InitRound();
		Diagnostics.Assert(this.aliveUnits != null);
		for (int i = 0; i < this.aliveUnits.Count; i++)
		{
			this.aliveUnits[i].ClearUnitStates();
			this.aliveUnits[i].RefreshTargetingStrategy();
			this.aliveUnits[i].Spread();
			this.ExecuteGroundAction(this.aliveUnits[i], this.aliveUnits[i].Position);
			this.ApplyGroundActions(this.aliveUnits[i], null);
			this.ComputeAvailableTargets(this.aliveUnits[i]);
		}
		this.ManageUnitsSorting(true);
		for (int j = 0; j < this.aliveUnits.Count; j++)
		{
			this.currentReport.AddUnitReport(this.aliveUnits[j]);
		}
		this.battleActionController.DoReportInstructions(this.currentReport, false);
		this.aliveUnits.ForEach(new Action<BattleSimulationUnit>(this.ComputeTargetingIntention));
		this.aliveUnits.ForEach(new Action<BattleSimulationUnit>(this.ReportInstructions));
		return this.currentReport;
	}

	public void EndBattle()
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		IEncounterRepositoryService service2 = service.Game.Services.GetService<IEncounterRepositoryService>();
		Encounter encounter = null;
		if (this.Encounter != null)
		{
			service2.TryGetValue(this.Encounter.EncounterGUID, out encounter);
		}
		for (int i = 0; i < this.battleSimulationArmies.Count; i++)
		{
			this.battleSimulationArmies[i].Contender.IsDead = this.battleSimulationArmies[i].IsDead;
			for (int j = 0; j < this.battleSimulationArmies[i].Units.Count; j++)
			{
				BattleSimulationUnit battleSimulationUnit = this.battleSimulationArmies[i].Units[j];
				battleSimulationUnit.PositionUpdate = (BattleSimulationUnit.PositionUpdateHandler)Delegate.Remove(battleSimulationUnit.PositionUpdate, new BattleSimulationUnit.PositionUpdateHandler(this.BattleSimulationUnit_PositionUpdateHandler));
				if (battleSimulationUnit.IsBattleSimulated)
				{
					float propertyValue = battleSimulationUnit.GetPropertyValue(SimulationProperties.Armor);
					float propertyValue2 = battleSimulationUnit.GetPropertyValue(SimulationProperties.MaximumArmor);
					if (propertyValue2 > 0f)
					{
						if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
						{
							Diagnostics.Log("ELCP Battlesimulation EndBattle() simulated for {0}/{3} with armor {1} of {2} {4}", new object[]
							{
								battleSimulationUnit.Unit.UnitDesign.LocalizedName,
								propertyValue,
								propertyValue2,
								battleSimulationUnit.UnitGUID,
								encounter == null
							});
						}
						battleSimulationUnit.SetPropertyBaseValue(SimulationProperties.MaximumArmor, propertyValue2, false, null);
						battleSimulationUnit.SetPropertyBaseValue(SimulationProperties.Armor, propertyValue, false, null);
						if (encounter != null && propertyValue < propertyValue2)
						{
							EncounterUnit encounterUnitByGUID = encounter.GetEncounterUnitByGUID(battleSimulationUnit.UnitGUID);
							if (encounterUnitByGUID != null)
							{
								encounterUnitByGUID.IsOnBattlefield = true;
							}
						}
					}
				}
			}
		}
		this.Encounter = null;
	}

	public float GetUnitsSimulationAverageData(byte group, StaticString propertyName)
	{
		if (this.unitsSimulationAverageDataByGroup.ContainsKey(group))
		{
			List<BattleSimulation.UnitsSimulationCompositionData> list = this.unitsSimulationAverageDataByGroup[group];
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].PropertyName == propertyName)
				{
					return list[i].PropertyCompositionValue;
				}
			}
		}
		return 0f;
	}

	public int GetUnitAliveAlliesCount(BattleSimulationUnit unit)
	{
		byte group = unit.Contender.Group;
		int num = 0;
		for (int i = 0; i < this.battleSimulationArmies.Count; i++)
		{
			if (this.battleSimulationArmies[i].Contender.IsTakingPartInBattle && this.battleSimulationArmies[i].Group == group)
			{
				num += this.battleSimulationArmies[i].AliveUnitsCount;
			}
		}
		return num;
	}

	public int GetUnitAliveOpponentsCount(BattleSimulationUnit unit)
	{
		byte group = unit.Contender.Group;
		int num = 0;
		for (int i = 0; i < this.battleSimulationArmies.Count; i++)
		{
			if (this.battleSimulationArmies[i].Contender.IsTakingPartInBattle && this.battleSimulationArmies[i].Group != group)
			{
				num += this.battleSimulationArmies[i].AliveUnitsCount;
			}
		}
		return num;
	}

	public float GetUnitAliveOpponentsDebufferRatio(BattleSimulationUnit unit)
	{
		byte group = unit.Contender.Group;
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < this.battleSimulationArmies.Count; i++)
		{
			if (this.battleSimulationArmies[i].Contender.IsTakingPartInBattle && this.battleSimulationArmies[i].Group != group)
			{
				for (int j = 0; j < this.battleSimulationArmies[i].Units.Count; j++)
				{
					if (!this.battleSimulationArmies[i].Units[j].IsDead)
					{
						num2++;
						if (this.battleSimulationArmies[i].Units[j].SimulationObject.Tags.Contains("UnitCanDebuff"))
						{
							num++;
						}
					}
				}
			}
		}
		if (num2 != 0)
		{
			return 0f;
		}
		return (float)num / (float)num2;
	}

	public bool LaunchSpell(BattleContender battleContender, SpellDefinition spellDefinition, WorldPosition targetPosition)
	{
		if (battleContender == null)
		{
			throw new ArgumentNullException("battleContender");
		}
		Diagnostics.Assert(this.battleSimulationArmies != null);
		List<SpellBattleAction> availableSpellBattleActions = spellDefinition.GetAvailableSpellBattleActions(battleContender.Garrison.Empire);
		for (int i = 0; i < this.battleSimulationArmies.Count; i++)
		{
			BattleSimulationArmy battleSimulationArmy = this.battleSimulationArmies[i];
			Diagnostics.Assert(battleSimulationArmy != null);
			if (battleSimulationArmy.Contender == battleContender)
			{
				for (int j = 0; j < availableSpellBattleActions.Count; j++)
				{
					BattleAction battleAction;
					if (this.battleActionDatabase.TryGetValue(availableSpellBattleActions[j].BattleActionUserDefinitionReference, out battleAction))
					{
						battleSimulationArmy.AddWaitingSpell(spellDefinition.Name, availableSpellBattleActions[j].Name, battleAction, targetPosition);
					}
				}
				return true;
			}
		}
		return false;
	}

	public RoundReport ReleaseBattle(RoundReport report = null)
	{
		if (report != null)
		{
			this.currentReport = report;
		}
		else
		{
			this.currentReport = new RoundReport();
		}
		foreach (KeyValuePair<byte, List<BattleSimulationArmy>> keyValuePair in this.armiesByGroup)
		{
			bool flag = false;
			for (int i = 0; i < keyValuePair.Value.Count; i++)
			{
				if (keyValuePair.Value[i].HasUnitsThatCanFight)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				for (int j = 0; j < keyValuePair.Value.Count; j++)
				{
					BattleSimulationArmy battleSimulationArmy = keyValuePair.Value[j];
					if (battleSimulationArmy.Units != null)
					{
						for (int k = 0; k < battleSimulationArmy.Units.Count; k++)
						{
							BattleSimulationUnit battleSimulationUnit = battleSimulationArmy.Units[k];
							if (battleSimulationUnit.Position.IsValid && !battleSimulationUnit.IsDead && battleSimulationUnit.SimulationObject.Tags.Contains("UnitActionPhoenixToEgg"))
							{
								battleSimulationUnit.SetPropertyBaseValue(SimulationProperties.Health, 0f, false, null);
							}
						}
					}
				}
			}
		}
		for (int l = 0; l < this.aliveUnits.Count; l++)
		{
			this.aliveUnits[l].Release();
		}
		this.battleActionController.ReleaseBattleActions();
		Diagnostics.Assert(this.battleActionController != null);
		this.battleActionController.DoReportInstructions(this.currentReport, false);
		this.aliveUnits.ForEach(new Action<BattleSimulationUnit>(this.ReportInstructions));
		return this.currentReport;
	}

	public void RemoveDescriptor(string descriptorName)
	{
		if (string.IsNullOrEmpty(descriptorName))
		{
			throw new ArgumentNullException("descriptorName");
		}
		Diagnostics.Assert(this.battleSimulationArmies != null);
		for (int i = 0; i < this.battleSimulationArmies.Count; i++)
		{
			BattleSimulationArmy battleSimulationArmy = this.battleSimulationArmies[i];
			Diagnostics.Assert(battleSimulationArmy != null);
			battleSimulationArmy.RemoveDescriptorByName(descriptorName);
		}
	}

	public bool SetUnitBodyAsPioritary(BattleContender battleContender, UnitBodyDefinition unitBodyDefinition)
	{
		if (battleContender == null)
		{
			throw new ArgumentNullException("battleContender");
		}
		BattleSimulationArmy battleSimulationArmy = this.battleSimulationArmies.Find((BattleSimulationArmy army) => army.ContenderGUID == battleContender.GUID);
		if (battleSimulationArmy == null)
		{
			Diagnostics.LogError("Can't set the unit body definition {0} as prioritary because it's impossible to find the contender's army.", new object[]
			{
				(unitBodyDefinition == null) ? "null" : unitBodyDefinition.Name.ToString()
			});
			return false;
		}
		return battleSimulationArmy.SetUnitBodyAsPioritary(unitBodyDefinition);
	}

	public void StartBattle()
	{
		this.ApplyDeploymentPositionsToUnits();
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (this.battleSimulationArmies != null)
			{
				foreach (BattleSimulationArmy battleSimulationArmy in this.battleSimulationArmies)
				{
					battleSimulationArmy.Dispose();
				}
				this.battleSimulationArmies.Clear();
			}
			this.battleActionController.Dispose();
			this.battleTargetingController.Dispose();
			this.battleUnitActionController.Dispose();
		}
		this.aliveUnits.Clear();
		this.battleSimulationArmies.Clear();
		this.battleActionsGround = null;
		if (disposing && this.potentialBattleSimulationTargets != null)
		{
			for (int i = 0; i < this.potentialBattleSimulationTargets.Length; i++)
			{
				this.potentialBattleSimulationTargets[i].Dispose();
			}
			this.potentialBattleSimulationTargets = null;
		}
		this.currentReport = null;
		this.fightingUnits.Clear();
		this.pathfindingWorldContextByContenderGroup[0].Clear();
		this.pathfindingWorldContextByContenderGroup[1].Clear();
		this.battleZone = null;
	}

	private void AddContender(BattleContender contender, BattleEncounter encounter)
	{
		if (contender == null)
		{
			throw new ArgumentNullException("contender");
		}
		if (contender.Deployment == null || contender.Deployment.UnitDeployment == null || contender.Deployment.DeploymentArea == null)
		{
			throw new Exception("Contender deployment is null.");
		}
		if (encounter.OrderCreateEncounter.IsAutomaticBattle)
		{
			this.Encounter = encounter;
		}
		this.battleZone.AddContenderDeployment(contender.Deployment);
		this.pathfindingWorldContextByContenderGroup[0].SearchArea = this.battleZone;
		this.pathfindingWorldContextByContenderGroup[1].SearchArea = this.battleZone;
		Diagnostics.Assert(this.battleActionDatabase != null, "this.battleActionDatabase != null");
		BattleAction battleAction;
		this.battleActionDatabase.TryGetValue(BattleSimulation.defaultBattleActionUnitReference, out battleAction);
		BattleActionUnit battleActionUnit = battleAction as BattleActionUnit;
		if (battleActionUnit == null)
		{
			Diagnostics.LogError("Can't get default battle action unit {0}.", new object[]
			{
				BattleSimulation.defaultBattleActionUnitReference
			});
		}
		Diagnostics.Assert(this.battleSimulationArmies != null);
		BattleSimulationArmy battleSimulationArmy = new BattleSimulationArmy(new SimulationObject(string.Concat(new object[]
		{
			"Contender",
			this.battleSimulationArmies.Count,
			" (",
			contender.GUID,
			")"
		})), contender);
		IGameEntity gameEntity;
		if (!this.gameEntityRepositoryService.TryGetValue(contender.Garrison.GUID, out gameEntity))
		{
			Diagnostics.LogError("Can't find the contender garrison entity. (GUID = '{0}')", new object[]
			{
				contender.Garrison.GUID
			});
			throw new SystemException("Can't find the contender garrison entity.");
		}
		Garrison garrison = gameEntity as Garrison;
		Diagnostics.Assert(garrison != null);
		battleSimulationArmy.SimulationObject.CopyTags(garrison.SimulationObject);
		Diagnostics.Assert(contender.Deployment.ReinforcementPoints != null);
		for (int i = 0; i < contender.Deployment.ReinforcementPoints.Length; i++)
		{
			WorldPosition reinforcementPointPosition = contender.Deployment.ReinforcementPoints[i];
			battleSimulationArmy.AddReinforcementPoint(reinforcementPointPosition);
		}
		Diagnostics.Assert(contender.Garrison != null && contender.Garrison.Units != null);
		List<BattleSimulationUnit> list = new List<BattleSimulationUnit>();
		Unit unit;
		Predicate<UnitDeployment> <>9__0;
		foreach (Unit unit2 in contender.Garrison.Units)
		{
			unit = unit2;
			SimulationObject simulationObject = unit.SimulationObject.DuplicateFlat(unit.Name);
			Diagnostics.Assert(simulationObject != null);
			this.descriptors.Clear();
			unit.FillDescriptorListFromType(PathfindingContext.MovementCapacityDescriptorType, ref this.descriptors);
			for (int j = 0; j < this.descriptors.Count; j++)
			{
				simulationObject.AddDescriptor(this.descriptors[j]);
			}
			BattleSimulationUnit battleSimulationUnit = new BattleSimulationUnit(simulationObject, battleSimulationArmy, unit, this.worldAtlas, this.BattleZone, contender);
			battleSimulationUnit.Refresh(false);
			if (encounter.OrderCreateEncounter.IsAutomaticBattle)
			{
				battleSimulationUnit.IsBattleSimulated = true;
			}
			SimulationDescriptor descriptor;
			if (Databases.GetDatabase<SimulationDescriptor>(false).TryGetValue("ClassBattleUnit", out descriptor))
			{
				battleSimulationUnit.SimulationObject.AddDescriptor(descriptor);
			}
			battleSimulationUnit.Refresh(false);
			UnitDeployment[] unitDeployment = contender.Deployment.UnitDeployment;
			Predicate<UnitDeployment> match;
			if ((match = <>9__0) == null)
			{
				match = (<>9__0 = ((UnitDeployment deployment) => deployment.UnitGUID == unit.GUID));
			}
			UnitDeployment unitDeployment2 = Array.Find<UnitDeployment>(unitDeployment, match);
			battleSimulationUnit.SetPosition((unitDeployment2 != null) ? unitDeployment2.WorldPosition : WorldPosition.Invalid, null);
			BattleActionUnit[] array = this.InitializeBattleActionUnits(unit);
			if (array != null)
			{
				battleSimulationUnit.AddBattleAction(array);
			}
			else
			{
				battleSimulationUnit.AddBattleAction(battleActionUnit, BattleEffect.BattleEffectApplicationMethod.Additive);
			}
			Diagnostics.Assert(battleSimulationUnit.SimulationObject != null);
			if (!BattleSimulation.ELCPFortification())
			{
				if (encounter is BattleCityAssaultEncounter)
				{
					BattleCityAssaultEncounter battleCityAssaultEncounter = encounter as BattleCityAssaultEncounter;
					IGameEntity gameEntity2 = null;
					if (this.gameEntityRepositoryService.TryGetValue(battleCityAssaultEncounter.CityGuid, out gameEntity2) && gameEntity2 is City)
					{
						City city = gameEntity2 as City;
						if (city.Empire == contender.Garrison.Empire)
						{
							float num = 1f;
							if (DepartmentOfTheInterior.FortificationRecoverByOwnershipCurve != null)
							{
								num = DepartmentOfTheInterior.FortificationRecoverByOwnershipCurve.Evaluate(city.Ownership[city.Empire.Index]);
							}
							float propertyValue = city.GetPropertyValue(SimulationProperties.CityDefensePoint);
							battleSimulationUnit.SetPropertyBaseValue(SimulationProperties.MaximumArmor, propertyValue * num, false, null);
							battleSimulationUnit.SetPropertyBaseValue(SimulationProperties.Armor, propertyValue * num, false, null);
							battleSimulationUnit.Refresh(true);
						}
					}
				}
			}
			else
			{
				List<BattleContender> list2 = encounter.BattleContenders.FindAll((BattleContender X) => X.IsMainContender);
				bool flag = false;
				City city2 = null;
				if (list2.Count == 2)
				{
					flag = BattleSimulation.IsELCPCityBattle(new List<IGarrison>
					{
						list2[0].Garrison,
						list2[1].Garrison
					}, out city2);
				}
				if (flag)
				{
					District district = this.worldPositionningService.GetDistrict(contender.WorldPosition);
					if (district != null && District.IsACityTile(district) && district.City.Empire == contender.Garrison.Empire && district.City == city2)
					{
						float propertyValue2 = district.City.GetPropertyValue(SimulationProperties.CityDefensePoint);
						float num2 = 1f;
						if (DepartmentOfTheInterior.FortificationRecoverByOwnershipCurve != null)
						{
							num2 = DepartmentOfTheInterior.FortificationRecoverByOwnershipCurve.Evaluate(district.City.Ownership[district.City.Empire.Index]);
						}
						battleSimulationUnit.SetPropertyBaseValue(SimulationProperties.MaximumArmor, propertyValue2 * num2, false, null);
						battleSimulationUnit.SetPropertyBaseValue(SimulationProperties.Armor, propertyValue2 * num2, false, null);
						battleSimulationUnit.Refresh(true);
					}
				}
			}
			battleSimulationArmy.AddChild(battleSimulationUnit);
			list.Add(battleSimulationUnit);
			BattleSimulationUnit battleSimulationUnit2 = battleSimulationUnit;
			battleSimulationUnit2.PositionUpdate = (BattleSimulationUnit.PositionUpdateHandler)Delegate.Combine(battleSimulationUnit2.PositionUpdate, new BattleSimulationUnit.PositionUpdateHandler(this.BattleSimulationUnit_PositionUpdateHandler));
		}
		battleSimulationArmy.SimulationObject.CopyFlat(garrison.SimulationObject);
		battleSimulationArmy.SetPropertyBaseValue(SimulationProperties.UnitSlotCount, 0f);
		for (int k = 0; k < list.Count; k++)
		{
			BattleSimulationUnit battleSimulationUnit3 = list[k];
			battleSimulationArmy.RemoveChild(battleSimulationUnit3);
			battleSimulationArmy.AddUnit(battleSimulationUnit3);
		}
		list.Clear();
		battleSimulationArmy.Refresh(false);
		Diagnostics.Assert(this.battleSimulationArmies != null);
		this.battleSimulationArmies.Add(battleSimulationArmy);
		if (!this.armiesByGroup.ContainsKey(contender.Group))
		{
			this.armiesByGroup.Add(contender.Group, new List<BattleSimulationArmy>());
		}
		this.armiesByGroup[contender.Group].Add(battleSimulationArmy);
	}

	private void ApplyDeploymentPositionsToUnits()
	{
		for (int i = 0; i < this.battleSimulationArmies.Count; i++)
		{
			BattleSimulationArmy battleSimulationArmy = this.battleSimulationArmies[i];
			BattleContender contender = battleSimulationArmy.Contender;
			for (int j = 0; j < battleSimulationArmy.Units.Count; j++)
			{
				BattleSimulationUnit battleSimulationUnit = battleSimulationArmy.Units[j];
				UnitDeployment unitDeployment = Array.Find<UnitDeployment>(contender.Deployment.UnitDeployment, (UnitDeployment deployment) => deployment.UnitGUID == battleSimulationUnit.UnitGUID);
				battleSimulationUnit.SetPosition((unitDeployment != null) ? unitDeployment.WorldPosition : WorldPosition.Invalid, null);
			}
		}
	}

	private BattleSimulationTarget GetPotentialTargetByPosition(WorldPosition position)
	{
		for (int i = 0; i < this.potentialBattleSimulationTargets.Length; i++)
		{
			if (this.potentialBattleSimulationTargets[i].StaticPosition == position)
			{
				return this.potentialBattleSimulationTargets[i];
			}
		}
		return null;
	}

	private void InitBattleSimulationTargets()
	{
		this.potentialBattleSimulationTargets = new BattleSimulationTarget[this.battleZone.AvailablePositionCount];
		int num = 0;
		foreach (WorldPosition worldPosition in this.battleZone.GetWorldPositions())
		{
			BattleSimulationUnit unitFromPosition = this.GetUnitFromPosition(worldPosition, false);
			if (unitFromPosition != null)
			{
				this.potentialBattleSimulationTargets[num] = new BattleSimulationTarget(unitFromPosition);
			}
			else
			{
				this.potentialBattleSimulationTargets[num] = new BattleSimulationTarget(worldPosition);
			}
			List<WorldPosition> worldPositionNeighbours = this.GetWorldPositionNeighbours(worldPosition);
			this.unitReachablePositionsHelper.Add(worldPosition, new BattleSimulation.UnitReachablePositionsHelperData(worldPositionNeighbours));
			num++;
		}
		for (int i = 0; i < this.potentialBattleSimulationTargets.Length; i++)
		{
			List<BattleSimulationTarget> worldPositionNeighboursAsBattleSimulationTargets = this.GetWorldPositionNeighboursAsBattleSimulationTargets(this.potentialBattleSimulationTargets[i].StaticPosition);
			this.potentialBattleSimulationTargets[i].Neighbours = worldPositionNeighboursAsBattleSimulationTargets;
		}
	}

	private void InitRound()
	{
		this.SpawnReinforcements();
		this.InitActiveUnitCache();
		for (int i = 0; i < this.potentialBattleSimulationTargets.Length; i++)
		{
			this.potentialBattleSimulationTargets[i].ClearTargeters();
		}
		this.UpdateUnitsSimulationAverageData();
		this.ResetBattleZone();
	}

	private void ManageUnitsSorting(bool refreshMorale = true)
	{
		if (refreshMorale)
		{
			this.aliveUnits.ForEach(new Action<BattleSimulationUnit>(this.ComputeMorale));
		}
		Diagnostics.Assert(this.aliveUnits != null);
		this.aliveUnits.Sort(delegate(BattleSimulationUnit unit1, BattleSimulationUnit unit2)
		{
			int num = unit2.GetPropertyValue(SimulationProperties.BattleInitiative).CompareTo(unit1.GetPropertyValue(SimulationProperties.BattleInitiative));
			if (num == 0)
			{
				num = unit2.GetPropertyValue(SimulationProperties.BattleMorale).CompareTo(unit1.GetPropertyValue(SimulationProperties.BattleMorale));
				if (num == 0)
				{
					num = unit1.UnitGUID.CompareTo(unit2.UnitGUID);
				}
			}
			return num;
		});
		for (int i = 0; i < this.aliveUnits.Count; i++)
		{
			this.aliveUnits[i].UnitRoundRank = i;
			if (i < this.aliveUnits[i].BattleSimulationArmy.BestUnitRoundRank)
			{
				this.aliveUnits[i].BattleSimulationArmy.BestUnitRoundRank = i;
			}
		}
	}

	private void OnRoundBegin(BattleSimulationArmy army)
	{
		if (army == null)
		{
			throw new ArgumentNullException("army");
		}
		army.OnRoundBegin();
	}

	private void ResetUnitReachablePositionsHelper(bool refreshUnits = true)
	{
		foreach (WorldPosition key in this.battleZone.GetWorldPositions())
		{
			this.unitReachablePositionsHelper[key].Range = float.NegativeInfinity;
			if (refreshUnits)
			{
				this.unitReachablePositionsHelper[key].Unit = null;
			}
		}
		if (refreshUnits)
		{
			for (int i = 0; i < this.battleSimulationArmies.Count; i++)
			{
				BattleSimulationArmy battleSimulationArmy = this.battleSimulationArmies[i];
				Diagnostics.Assert(battleSimulationArmy != null);
				ReadOnlyCollection<BattleSimulationUnit> units = battleSimulationArmy.Units;
				for (int j = 0; j < units.Count; j++)
				{
					BattleSimulationUnit battleSimulationUnit = units[j];
					Diagnostics.Assert(battleSimulationUnit != null);
					if (battleSimulationUnit.Position.IsValid && !battleSimulationUnit.IsDead && this.unitReachablePositionsHelper.ContainsKey(battleSimulationUnit.Position))
					{
						this.unitReachablePositionsHelper[battleSimulationUnit.Position].Unit = battleSimulationUnit;
					}
				}
			}
		}
	}

	private void SpawnReinforcements()
	{
		Diagnostics.Assert(this.battleSimulationArmies != null);
		List<BattleSimulationArmy> list = new List<BattleSimulationArmy>(this.battleSimulationArmies);
		list.Sort((BattleSimulationArmy left, BattleSimulationArmy right) => left.Contender.ReinforcementRanking.CompareTo(right.Contender.ReinforcementRanking));
		for (int i = 0; i < list.Count; i++)
		{
			BattleSimulationArmy battleSimulationArmy = list[i];
			Diagnostics.Assert(battleSimulationArmy != null && battleSimulationArmy.Units != null);
			if (battleSimulationArmy.Contender.IsTakingPartInBattle)
			{
				List<BattleSimulationUnit> list2 = new List<BattleSimulationUnit>();
				int num = 0;
				for (;;)
				{
					list2.Clear();
					if (battleSimulationArmy.PriorityUnitBodyForReinforcement != null)
					{
						list2.AddRange(from unit in battleSimulationArmy.Units
						where !unit.Position.IsValid && unit.UnitBodyDefinitionReference.Name == battleSimulationArmy.PriorityUnitBodyForReinforcement.Name
						select unit);
					}
					else
					{
						list2.AddRange(from unit in battleSimulationArmy.Units
						where !unit.Position.IsValid
						select unit);
					}
					if (list2.Count == 0)
					{
						if (battleSimulationArmy.PriorityUnitBodyForReinforcement == null)
						{
							break;
						}
						battleSimulationArmy.SetUnitBodyAsPioritary(null);
					}
					else
					{
						BattleSimulationArmy battleSimulationArmy3 = this.battleSimulationArmies.FirstOrDefault((BattleSimulationArmy army) => army.Group == battleSimulationArmy.Group);
						IEnumerable<BattleSimulationArmy> enumerable = from army in this.battleSimulationArmies
						where army.Group == battleSimulationArmy.Group
						select army;
						float num2 = 0f;
						foreach (BattleSimulationArmy battleSimulationArmy2 in enumerable)
						{
							battleSimulationArmy2.Refresh(false);
							num2 += battleSimulationArmy2.GetPropertyValue(SimulationProperties.UnitSlotCount);
						}
						float num3 = (float)battleSimulationArmy3.Contender.Garrison.MaximumUnitSlot;
						if (num2 >= num3)
						{
							break;
						}
						list2.Sort((BattleSimulationUnit left, BattleSimulationUnit right) => this.GetUnitReinforcementScore(right).CompareTo(this.GetUnitReinforcementScore(left)));
						ReadOnlyCollection<WorldPosition> reinforcementPoints = battleSimulationArmy.ReinforcementPoints;
						Diagnostics.Assert(reinforcementPoints != null);
						if (num >= reinforcementPoints.Count)
						{
							break;
						}
						WorldPosition worldPosition = reinforcementPoints[num];
						num++;
						BattleSimulationUnit unitFromPosition = this.GetUnitFromPosition(worldPosition, false);
						if (unitFromPosition == null)
						{
							BattleSimulationUnit battleSimulationUnit = list2[0];
							Diagnostics.Assert(battleSimulationUnit != null);
							battleSimulationUnit.Spawn(worldPosition);
							battleSimulationArmy.AddUnit(battleSimulationUnit);
							battleSimulationArmy.RefreshAttachment(battleSimulationUnit);
						}
					}
				}
			}
		}
	}

	private void ResetBattleZone()
	{
		foreach (byte group in this.unitsSimulationAverageDataByGroup.Keys)
		{
			this.battleZoneAnalysis.ResetGroup(group);
		}
	}

	private void ResetUnitsSimulationAverageData(byte group)
	{
		if (this.unitsSimulationAverageDataByGroup.ContainsKey(group))
		{
			List<BattleSimulation.UnitsSimulationCompositionData> list = this.unitsSimulationAverageDataByGroup[group];
			for (int i = 0; i < list.Count; i++)
			{
				list[i].PropertyCompositionValue = 0f;
			}
		}
	}

	private void RefreshUnitsSimulationAverageDataForGroup(byte group)
	{
		if (!this.unitsSimulationAverageDataByGroup.ContainsKey(group))
		{
			return;
		}
		this.ResetUnitsSimulationAverageData(group);
		List<BattleSimulation.UnitsSimulationCompositionData> list = this.unitsSimulationAverageDataByGroup[group];
		int num = 0;
		for (int i = 0; i < this.aliveUnits.Count; i++)
		{
			if (this.aliveUnits[i].Contender.Group == group)
			{
				for (int j = 0; j < list.Count; j++)
				{
					list[j].PropertyCompositionValue += this.aliveUnits[i].GetPropertyValue(list[j].PropertyName);
				}
				num++;
			}
		}
		for (int k = 0; k < list.Count; k++)
		{
			list[k].PropertyCompositionValue /= (float)num;
		}
	}

	private void UpdateUnitPositionInPotentialTargets(BattleSimulationUnit unit, WorldPosition newPosition)
	{
		BattleSimulationTarget battleSimulationTarget = null;
		for (int i = 0; i < this.potentialBattleSimulationTargets.Length; i++)
		{
			if (this.potentialBattleSimulationTargets[i].Unit == unit)
			{
				if (this.potentialBattleSimulationTargets[i].StaticPosition == newPosition)
				{
					return;
				}
				this.potentialBattleSimulationTargets[i].Unit = null;
				if (!newPosition.IsValid)
				{
					return;
				}
			}
			else if (this.potentialBattleSimulationTargets[i].StaticPosition == newPosition)
			{
				battleSimulationTarget = this.potentialBattleSimulationTargets[i];
			}
		}
		if (battleSimulationTarget != null)
		{
			battleSimulationTarget.Unit = unit;
		}
	}

	private void UpdateUnitsSimulationAverageData()
	{
		foreach (KeyValuePair<byte, List<BattleSimulation.UnitsSimulationCompositionData>> keyValuePair in this.unitsSimulationAverageDataByGroup)
		{
			this.RefreshUnitsSimulationAverageDataForGroup(keyValuePair.Key);
		}
	}

	public static bool ELCPFortification()
	{
		ISessionService service = Services.GetService<ISessionService>();
		Diagnostics.Assert(service != null);
		return service.Session.GetLobbyData<string>("FortificationRules", "Vanilla") == "ELCP";
	}

	public static bool IsELCPCityBattle(List<IGarrison> MainContenders, out City city)
	{
		city = null;
		IWorldPositionningService service = Services.GetService<IGameService>().Game.Services.GetService<IWorldPositionningService>();
		foreach (IGarrison garrison in MainContenders)
		{
			District district = service.GetDistrict((garrison as IWorldPositionable).WorldPosition);
			if (district != null && (District.IsACityTile(district) || district.Type == DistrictType.Exploitation))
			{
				city = district.City;
				return true;
			}
		}
		return false;
	}

	public static bool GetsFortificationBonus(IGarrison garrison, City city)
	{
		if (garrison == null || city == null)
		{
			return false;
		}
		District district = Services.GetService<IGameService>().Game.Services.GetService<IWorldPositionningService>().GetDistrict((garrison as IWorldPositionable).WorldPosition);
		return district != null && District.IsACityTile(district) && district.City == city && district.City.Empire == garrison.Empire;
	}

	public const PathfindingFlags BattlePathfindingFlags = PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreEncounterAreas | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges;

	private List<WorldPosition> validPositions = new List<WorldPosition>();

	public static int UnitStateNextID = 0;

	public static int UnitStateInvalidID = -1;

	private static StaticString defaultBattleActionUnitReference = "BattleActionUnitMeleeDefault";

	private readonly List<BattleSimulationUnit> aliveUnits = new List<BattleSimulationUnit>();

	private readonly BattleActionController battleActionController;

	private readonly IDatabase<BattleAction> battleActionDatabase;

	private readonly List<BattleSimulationArmy> battleSimulationArmies = new List<BattleSimulationArmy>();

	private readonly BattleTargetingController battleTargetingController;

	private readonly BattleUnitActionController battleUnitActionController;

	private readonly List<BattleSimulationUnit> fightingUnits = new List<BattleSimulationUnit>();

	private readonly IGameEntityRepositoryService gameEntityRepositoryService;

	private readonly IPathfindingService pathfindingService;

	private readonly WorldAtlas worldAtlas;

	private readonly IWorldPositionningService worldPositionningService;

	private readonly IWeatherService weatherService;

	private Dictionary<WorldPosition, BattleSimulation.UnitReachablePositionsHelperData> unitReachablePositionsHelper = new Dictionary<WorldPosition, BattleSimulation.UnitReachablePositionsHelperData>();

	private Dictionary<WorldPosition, List<BattleSimulation.GroundAction>> battleActionsGround;

	private BattleZone_Battle battleZone;

	private BattleZoneAnalysis battleZoneAnalysis;

	private BattleSimulationTarget[] potentialBattleSimulationTargets;

	private RoundReport currentReport;

	private Dictionary<byte, List<BattleSimulationArmy>> armiesByGroup;

	private Dictionary<byte, List<BattleSimulation.UnitsSimulationCompositionData>> unitsSimulationAverageDataByGroup;

	private PathfindingWorldContext[] pathfindingWorldContextByContenderGroup;

	private IDatabase<BattleTargetingStrategy> battleTargetingStrategyDatabase;

	private List<SimulationDescriptor> descriptors = new List<SimulationDescriptor>();

	private BattleEncounter Encounter;

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct GroundAction
	{
		public GroundAction(BattleActionGround battleAction, BattleSimulationUnit initiator)
		{
			this.BattleAction = battleAction;
			this.Initiator = initiator;
		}

		public BattleActionGround BattleAction { get; set; }

		public BattleSimulationUnit Initiator { get; set; }
	}

	private class UnitsSimulationCompositionData
	{
		public UnitsSimulationCompositionData(StaticString propertyName, float propertyValue)
		{
			this.PropertyName = propertyName;
			this.PropertyCompositionValue = propertyValue;
		}

		public StaticString PropertyName { get; private set; }

		public float PropertyCompositionValue { get; set; }
	}

	private class UnitReachablePositionsHelperData
	{
		public UnitReachablePositionsHelperData(List<WorldPosition> neighbours)
		{
			this.Range = -1f;
			this.Neighbours = neighbours;
			this.Unit = null;
		}

		public float Range { get; set; }

		public List<WorldPosition> Neighbours { get; private set; }

		public BattleSimulationUnit Unit { get; set; }
	}
}
