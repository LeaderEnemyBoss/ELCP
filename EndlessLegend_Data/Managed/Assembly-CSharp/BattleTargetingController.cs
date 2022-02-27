using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Simulation;
using UnityEngine;

public class BattleTargetingController : IDisposable
{
	public BattleTargetingController(IWorldPositionningService worldPositionningService, IPathfindingService pathfindingService, BattleSimulation battleSimulation)
	{
		if (worldPositionningService == null)
		{
			throw new ArgumentNullException("worldPositionningService");
		}
		if (pathfindingService == null)
		{
			throw new ArgumentNullException("pathfindingService");
		}
		this.worldPositionningService = worldPositionningService;
		this.pathfindingService = pathfindingService;
		this.targettingAnimationCurveDatabase = Databases.GetDatabase<Amplitude.Unity.Framework.AnimationCurve>(false);
		if (this.targettingAnimationCurveDatabase == null)
		{
			Diagnostics.Assert("Can't retrieve the animationCurve database.", new object[0]);
		}
		this.battleTargetingUnitBehaviorWeightDatabase = Databases.GetDatabase<BattleTargetingUnitBehaviorWeight>(false);
		if (this.battleTargetingUnitBehaviorWeightDatabase == null)
		{
			Diagnostics.Assert("Can't retrieve the unit behavior weight database, please reimport BehaviorWeight/BattleTargetingUnitBehaviorWeight.xls.", new object[0]);
		}
		this.battleSimulation = battleSimulation;
		this.paramsComputationDelegates = new Dictionary<StaticString, BattleTargetingController.ParamComputationDelegate>();
		this.paramsComputationDelegates.Add("TurnsToReachTargetWithCapacities", new BattleTargetingController.ParamComputationDelegate(this.GetNumberOfTurnToReachTargetWithCapacities));
		this.paramsComputationDelegates.Add("TurnsToReachTargetAsCrowFlies", new BattleTargetingController.ParamComputationDelegate(this.GetNumberOfTurnToReachTargetByAir));
		this.paramsComputationDelegates.Add("TargetHPRatio", new BattleTargetingController.ParamComputationDelegate(this.GetTargetHPRatio));
		this.paramsComputationDelegates.Add("DoesTargetHaveFullHP", new BattleTargetingController.ParamComputationDelegate(this.GetDoesTargetHaveFullHP));
		this.paramsComputationDelegates.Add("TargetLostHPRatio", new BattleTargetingController.ParamComputationDelegate(this.GetTargetLostHPRatio));
		this.paramsComputationDelegates.Add("TargetRatioOfOffensiveMilitaryPowerToBattleAverage", new BattleTargetingController.ParamComputationDelegate(this.GetTargetOffensiveMilitaryPowerRatio));
		this.paramsComputationDelegates.Add("TargetRatioOfDefensiveMilitaryPowerToBattleAverage", new BattleTargetingController.ParamComputationDelegate(this.GetTargetDefensiveMilitaryPowerRatio));
		this.paramsComputationDelegates.Add("TargetMorale", new BattleTargetingController.ParamComputationDelegate(this.GetTargetMorale));
		this.paramsComputationDelegates.Add("RatioOfTargetSpeedToBattleArea", new BattleTargetingController.ParamComputationDelegate(this.GetRatioOfTargetSpeedToBattleArea));
		this.paramsComputationDelegates.Add("WithinAttackRange", new BattleTargetingController.ParamComputationDelegate(this.GetWithinAttackRange));
		this.paramsComputationDelegates.Add("WithinMovementRange", new BattleTargetingController.ParamComputationDelegate(this.GetWithinMovementRange));
		this.paramsComputationDelegates.Add("WithinAttackAndMoveRange", new BattleTargetingController.ParamComputationDelegate(this.GetWithinAttackAndMoveRange));
		this.paramsComputationDelegates.Add("RatioDistanceToAttackAndMoveRange", new BattleTargetingController.ParamComputationDelegate(this.GetRatioDistanceToAttackAndMoveRange));
		this.paramsComputationDelegates.Add("NumberOfNegativeGroundEffectsAtTargetPosition", new BattleTargetingController.ParamComputationDelegate(this.GetNumberOfNegativeGroundBattleActionsAtTargetPosition));
		this.paramsComputationDelegates.Add("NumberOfPositiveGroundEffectsAtTargetPosition", new BattleTargetingController.ParamComputationDelegate(this.GetNumberOfPositiveGroundBattleActionsAtTargetPosition));
		this.paramsComputationDelegates.Add("IsMyBattleEffectAppliedOnTarget", new BattleTargetingController.ParamComputationDelegate(this.IsMyBattleEffectAppliedOnTarget));
		this.paramsComputationDelegates.Add("IsTargetUnitClassMelee", new BattleTargetingController.ParamComputationDelegate(this.GetIsTargetUnitClassMelee));
		this.paramsComputationDelegates.Add("IsTargetUnitClassCavalry", new BattleTargetingController.ParamComputationDelegate(this.GetIsTargetUnitClassCavalry));
		this.paramsComputationDelegates.Add("IsTargetUnitClassRanged", new BattleTargetingController.ParamComputationDelegate(this.GetIsTargetUnitClassRanged));
		this.paramsComputationDelegates.Add("IsTargetUnitClassFlying", new BattleTargetingController.ParamComputationDelegate(this.GetIsTargetUnitClassFlying));
		this.paramsComputationDelegates.Add("IsTargetUnitClassSupport", new BattleTargetingController.ParamComputationDelegate(this.GetIsTargetUnitClassSupport));
		this.paramsComputationDelegates.Add("IsTargetUnitClassInterceptor", new BattleTargetingController.ParamComputationDelegate(this.GetIsTargetUnitClassInterceptor));
		this.paramsComputationDelegates.Add("IsTargetUnitClassFrigate", new BattleTargetingController.ParamComputationDelegate(this.GetIsTargetUnitClassFrigate));
		this.paramsComputationDelegates.Add("IsTargetUnitClassJuggernaut", new BattleTargetingController.ParamComputationDelegate(this.GetIsTargetUnitClassJuggernaut));
		this.paramsComputationDelegates.Add("IsTargetUnitClassSubmersible", new BattleTargetingController.ParamComputationDelegate(this.GetIsTargetUnitClassSubmersible));
		this.paramsComputationDelegates.Add("IsTargetTransportShipUnit", new BattleTargetingController.ParamComputationDelegate(this.GetIsTargetTransportShipUnit));
		this.paramsComputationDelegates.Add("DoesTargetPlaysBeforeMe", new BattleTargetingController.ParamComputationDelegate(this.GetDoesTargetPlaysBeforeMe));
		this.paramsComputationDelegates.Add("RatioOfOpponentsAroundTargetPosition", new BattleTargetingController.ParamComputationDelegate(this.GetRatioOfOpponentsAroundTargetPosition));
		this.paramsComputationDelegates.Add("RatioOfOpponentsAroundTargetPositionWhoPlayAfterMe", new BattleTargetingController.ParamComputationDelegate(this.RatioOfOpponentsAroundTargetPositionWhoPlayAfterMe));
		this.paramsComputationDelegates.Add("IsNoEnemyAroundTargetPositionWhoPlaysAfterMe", new BattleTargetingController.ParamComputationDelegate(this.IsNoEnemyAroundTargetPositionWhoPlaysAfterMe));
		this.paramsComputationDelegates.Add("RatioOfAlliesAroundTargetPosition", new BattleTargetingController.ParamComputationDelegate(this.GetRatioOfAlliesAroundTargetPosition));
		this.paramsComputationDelegates.Add("IsTargetHigher", new BattleTargetingController.ParamComputationDelegate(this.GetIsTargetHigher));
		this.paramsComputationDelegates.Add("IsTargetLower", new BattleTargetingController.ParamComputationDelegate(this.GetIsTargetLower));
		this.paramsComputationDelegates.Add("RatioOfTargetAltitudeToMyAltitude", new BattleTargetingController.ParamComputationDelegate(this.GetRatioOfTargetAltitudeToMyAltitude));
		this.paramsComputationDelegates.Add("RatioMyAttackToTargetDefense", new BattleTargetingController.ParamComputationDelegate(this.GetRatioMyAttackToTargetDefense));
		this.paramsComputationDelegates.Add("RatioMyDefenseToTargetAttack", new BattleTargetingController.ParamComputationDelegate(this.GetRatioMyDefenseToTargetAttack));
		this.paramsComputationDelegates.Add("RatioOfHigherNeighboursAroundTargetPosition", new BattleTargetingController.ParamComputationDelegate(this.GetRatioOfHigherNeighboursAroundTargetPosition));
		this.paramsComputationDelegates.Add("RatioOfLowerNeighboursAroundTargetPosition", new BattleTargetingController.ParamComputationDelegate(this.GetRatioOfLowerNeighboursAroundTargetPosition));
		this.paramsComputationDelegates.Add("RatioOfWalkableNeighboursAroundTargetPosition", new BattleTargetingController.ParamComputationDelegate(this.GetRatioOfWalkableNeighboursAroundTargetPosition));
		this.paramsComputationDelegates.Add("RatioOfImpassableNeighboursAroundTargetPosition", new BattleTargetingController.ParamComputationDelegate(this.GetRatioOfImpassableNeighboursAroundTargetPosition));
		this.paramsComputationDelegates.Add("RatioOfAllyTargettedNeighboursAroundTargetPosition", new BattleTargetingController.ParamComputationDelegate(this.GetRatioOfAllyTargettedNeighboursAroundTargetPosition));
		this.paramsComputationDelegates.Add("RatioOfEnemyTargettedNeighboursAroundTargetPosition", new BattleTargetingController.ParamComputationDelegate(this.GetRatioOfEnemyTargettedNeighboursAroundTargetPosition));
		this.paramsComputationDelegates.Add("TargetedByAlliesCount", new BattleTargetingController.ParamComputationDelegate(this.GetTargetedByAlliesCount));
		this.paramsComputationDelegates.Add("TargetedByAlliesWithMyBodyCount", new BattleTargetingController.ParamComputationDelegate(this.GetTargetedByAlliesWithMyBodyCount));
		this.paramsComputationDelegates.Add("NumberOfAliveOpponents", new BattleTargetingController.ParamComputationDelegate(this.GetNumberOfAliveOpponents));
		this.paramsComputationDelegates.Add("OpponentsDebufferRatio", new BattleTargetingController.ParamComputationDelegate(this.GetOpponentsDebufferRatio));
		this.paramsComputationDelegates.Add("RatioOfOpponentsToAllies", new BattleTargetingController.ParamComputationDelegate(this.RatioOfOpponentsToAllies));
		this.paramsComputationDelegates.Add("RatioOfAlliesToOpponents", new BattleTargetingController.ParamComputationDelegate(this.RatioOfAlliesToOpponents));
		this.filtersComputationDelegates = new Dictionary<StaticString, BattleTargetingController.FilterComputationDelegate>();
		this.filtersComputationDelegates.Add("CanAttackWithoutMoving", new BattleTargetingController.FilterComputationDelegate(this.CanAttackWithoutMoving));
		this.filtersComputationDelegates.Add("IsEnemy", new BattleTargetingController.FilterComputationDelegate(this.IsEnemy));
		this.filtersComputationDelegates.Add("IsAlly", new BattleTargetingController.FilterComputationDelegate(this.IsAlly));
		this.filtersComputationDelegates.Add("IsGroundUnit", new BattleTargetingController.FilterComputationDelegate(this.IsGroundUnit));
		this.filtersComputationDelegates.Add("IsFreePosition", new BattleTargetingController.FilterComputationDelegate(this.IsFreePosition));
		this.filtersComputationDelegates.Add("IsUnit", new BattleTargetingController.FilterComputationDelegate(this.IsUnit));
		this.filtersComputationDelegates.Add("IsNotMe", new BattleTargetingController.FilterComputationDelegate(this.IsNotMe));
		this.filtersComputationDelegates.Add("IAmCombatUnit", new BattleTargetingController.FilterComputationDelegate(this.IAmCombatUnit));
		this.filtersComputationDelegates.Add("IAmInLightForm", new BattleTargetingController.FilterComputationDelegate(this.IAmInLightForm));
		this.filtersComputationDelegates.Add("IAmInDarkForm", new BattleTargetingController.FilterComputationDelegate(this.IAmInDarkForm));
		this.filtersComputationDelegates.Add("AlliesArePresent", new BattleTargetingController.FilterComputationDelegate(this.AlliesArePresent));
		this.filtersComputationDelegates.Add("IsTargetWithinMovementRange", new BattleTargetingController.FilterComputationDelegate(this.IsTargetWithinMovementRange));
		this.filtersComputationDelegates.Add("CanTargetBeDebuffed", new BattleTargetingController.FilterComputationDelegate(this.CanTargetBeDebuffed));
		this.filtersComputationDelegates.Add("IsTargetNotMindControlled", new BattleTargetingController.FilterComputationDelegate(this.IsTargetNotMindControlled));
		this.filtersComputationDelegates.Add("AnyAliveOpponents", new BattleTargetingController.FilterComputationDelegate(this.AnyAliveOpponents));
		this.filtersComputationDelegates.Add("IAmTransportShip", new BattleTargetingController.FilterComputationDelegate(this.IAmTransportShip));
		this.filtersComputationDelegates.Add("IAmNotTransportShip", new BattleTargetingController.FilterComputationDelegate(this.IAmNotTransportShip));
	}

	~BattleTargetingController()
	{
		this.Dispose(false);
	}

	public BattleSimulationTarget ComputeTarget(BattleSimulationUnit unit, BattleSimulationTarget[] battleSimulationTargets)
	{
		return this.ComputeTargetingWithWeight(unit, battleSimulationTargets);
	}

	public void Dispose()
	{
		this.Dispose(true);
	}

	public bool FiltersAreVerified(BattleTargetingUnitBehaviorWeight unitBehaviorWeight, BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		if (unitBehaviorWeight == null)
		{
			throw new ArgumentNullException("unitBehaviorWeight");
		}
		return unitBehaviorWeight.Filters == null || unitBehaviorWeight.Filters.Count == 0 || unitBehaviorWeight.Filters.All((StaticString filter) => this.GetFilterValueByName(filter, unit, potentialTarget));
	}

	protected bool CanAttackWithoutMoving(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return this.battleSimulation.CanAttackTarget(unit, potentialTarget);
	}

	protected BattleSimulationTarget ComputeTargetingWithWeight(BattleSimulationUnit unit, BattleSimulationTarget[] battleSimulationTargets)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		List<BattleTargetingUnitBehaviorWeight> battleTargetingUnitBehaviorWeights = unit.BattleTargetingUnitBehaviorWeights;
		if (battleTargetingUnitBehaviorWeights == null)
		{
			return null;
		}
		bool flag = unit.GetPropertyValue(SimulationProperties.BattleRange) > 1.1f;
		BattleSimulationTarget result = null;
		float num = float.NegativeInfinity;
		for (int i = 0; i < battleTargetingUnitBehaviorWeights.Count; i++)
		{
			BattleTargetingUnitBehaviorWeight battleTargetingUnitBehaviorWeight = battleTargetingUnitBehaviorWeights[i];
			foreach (BattleSimulationTarget battleSimulationTarget in battleSimulationTargets)
			{
				if ((battleSimulationTarget.Unit == null || !battleSimulationTarget.Unit.IsDead) && this.FiltersAreVerified(battleTargetingUnitBehaviorWeight, unit, battleSimulationTarget))
				{
					float num2 = 0f;
					if (battleTargetingUnitBehaviorWeight.Weights != null && battleTargetingUnitBehaviorWeight.Weights.Length != 0)
					{
						for (int k = 0; k < battleTargetingUnitBehaviorWeight.Weights.Length; k++)
						{
							BattleTargetingUnitBehaviorWeight.Weight weight = battleTargetingUnitBehaviorWeight.Weights[k];
							if (weight.ValueAsFloat != 0f)
							{
								float paramValueByName = this.GetParamValueByName(weight.Name, unit, battleSimulationTarget);
								Amplitude.Unity.Framework.AnimationCurve animationCurve;
								this.targettingAnimationCurveDatabase.TryGetValue(weight.NormalizationCurveName, out animationCurve);
								float num3;
								if (animationCurve != null)
								{
									num3 = animationCurve.Evaluate(paramValueByName) * weight.ValueAsFloat;
								}
								else
								{
									num3 = paramValueByName * weight.ValueAsFloat;
								}
								num2 += num3;
							}
						}
						num2 /= (float)battleTargetingUnitBehaviorWeight.Weights.Length;
					}
					if (flag && float.IsNegativeInfinity(num2) && battleSimulationTarget.Unit != null)
					{
						num2 = -1E+07f;
					}
					if (num2 > num)
					{
						result = battleSimulationTarget;
						num = num2;
					}
				}
			}
		}
		return result;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
		}
		this.battleTargetingUnitBehaviorWeightDatabase = null;
		this.filtersComputationDelegates.Clear();
		this.paramsComputationDelegates.Clear();
		this.targettingAnimationCurveDatabase = null;
		this.worldPositionningService = null;
		this.pathfindingService = null;
	}

	protected float GetNumberOfTurnToReachTargetByAir(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float result = 0f;
		float propertyValue = unit.GetPropertyValue(SimulationProperties.BattleMaximumMovement);
		if (propertyValue != 0f)
		{
			result = (float)this.worldPositionningService.GetDistance(unit.Position, potentialTarget.StaticPosition) / propertyValue;
		}
		return result;
	}

	protected float GetNumberOfTurnToReachTargetWithCapacities(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float result = 0f;
		float propertyValue = unit.GetPropertyValue(SimulationProperties.BattleMaximumMovement);
		if (propertyValue != 0f)
		{
			result = unit.GetDistanceByGround(potentialTarget.StaticPosition) / propertyValue;
		}
		return result;
	}

	protected bool GetFilterValueByName(StaticString filterName, BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		BattleTargetingController.FilterComputationDelegate filterComputationDelegate;
		if (this.filtersComputationDelegates.TryGetValue(filterName, out filterComputationDelegate))
		{
			return filterComputationDelegate(unit, potentialTarget);
		}
		Diagnostics.Assert("This filter {0} evaluation doesn't seem to be implemented. Did you modify the filter name?", new object[]
		{
			filterName
		});
		return false;
	}

	protected float GetDoesTargetPlaysBeforeMe(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		if (potentialTarget.Unit != null && (potentialTarget.Unit.HasUnitActions || potentialTarget.Unit.UnitRoundRank < unit.UnitRoundRank))
		{
			return 1f;
		}
		return 0f;
	}

	protected float GetNumberOfNegativeGroundBattleActionsAtTargetPosition(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		int positiveGroundActionsCountByGroup = this.battleSimulation.GetPositiveGroundActionsCountByGroup(potentialTarget.StaticPosition, unit.Contender.Group);
		int groundActionsCount = this.battleSimulation.GetGroundActionsCount(potentialTarget.StaticPosition);
		return (float)(groundActionsCount - positiveGroundActionsCountByGroup);
	}

	protected float GetNumberOfPositiveGroundBattleActionsAtTargetPosition(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		int positiveGroundActionsCountByGroup = this.battleSimulation.GetPositiveGroundActionsCountByGroup(potentialTarget.StaticPosition, unit.Contender.Group);
		return (float)positiveGroundActionsCountByGroup;
	}

	protected float IsMyBattleEffectAppliedOnTarget(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		if (potentialTarget.Unit != null)
		{
			IEnumerable<BattleUnitActionController.UnitAction> availableActions;
			if (potentialTarget.Unit.Contender.Group == unit.Contender.Group)
			{
				availableActions = this.battleSimulation.BattleUnitActionController.GetAvailableActions(unit, BattleActionUnit.ActionType.Support);
			}
			else
			{
				availableActions = this.battleSimulation.BattleUnitActionController.GetAvailableActions(unit, BattleActionUnit.ActionType.Attack);
			}
			if (availableActions != null)
			{
				foreach (BattleUnitActionController.UnitAction unitAction in availableActions)
				{
					for (int i = 0; i < unitAction.BattleAction.BattleEffects.Length; i++)
					{
						BattleEffects battleEffects = unitAction.BattleAction.BattleEffects[i];
						if (battleEffects != null && battleEffects.BattleEffectList != null)
						{
							for (int j = 0; j < battleEffects.BattleEffectList.Length; j++)
							{
								BattleEffectContext contextFromBattleEffect = unit.GetContextFromBattleEffect(battleEffects.BattleEffectList[j]);
								if (contextFromBattleEffect != null && contextFromBattleEffect.Target.Unit == potentialTarget.Unit)
								{
									return 1f;
								}
							}
						}
					}
				}
			}
		}
		return 0f;
	}

	protected float GetIsTargetUnitClassCavalry(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return (potentialTarget.Unit == null) ? 0f : this.GetIsUnitClassWeight(potentialTarget.Unit, "UnitClassCavalry");
	}

	protected float GetIsTargetUnitClassFlying(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return (potentialTarget.Unit == null) ? 0f : this.GetIsUnitClassWeight(potentialTarget.Unit, "UnitClassFlying");
	}

	protected float GetIsTargetUnitClassMelee(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return (potentialTarget.Unit == null) ? 0f : this.GetIsUnitClassWeight(potentialTarget.Unit, "UnitClassInfantry");
	}

	protected float GetIsTargetUnitClassRanged(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return (potentialTarget.Unit == null) ? 0f : this.GetIsUnitClassWeight(potentialTarget.Unit, "UnitClassRanged");
	}

	protected float GetIsTargetUnitClassSupport(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return (potentialTarget.Unit == null) ? 0f : this.GetIsUnitClassWeight(potentialTarget.Unit, "UnitClassSupport");
	}

	protected float GetIsTargetUnitClassInterceptor(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return (potentialTarget.Unit == null) ? 0f : this.GetIsUnitClassWeight(potentialTarget.Unit, "UnitClassInterceptor");
	}

	protected float GetIsTargetUnitClassFrigate(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return (potentialTarget.Unit == null) ? 0f : this.GetIsUnitClassWeight(potentialTarget.Unit, "UnitClassFrigate");
	}

	protected float GetIsTargetUnitClassJuggernaut(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return (potentialTarget.Unit == null) ? 0f : this.GetIsUnitClassWeight(potentialTarget.Unit, "UnitClassJuggernaut");
	}

	protected float GetIsTargetUnitClassSubmersible(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return (potentialTarget.Unit == null) ? 0f : this.GetIsUnitClassWeight(potentialTarget.Unit, "UnitClassSubmersible");
	}

	protected float GetIsTargetTransportShipUnit(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		if (potentialTarget.Unit != null && potentialTarget.Unit.SimulationObject.Tags.Contains(DownloadableContent16.TransportShipUnit))
		{
			return 1f;
		}
		return 0f;
	}

	protected float GetIsUnitClassWeight(BattleSimulationUnit potentialTarget, string classToCheck)
	{
		return (!this.IsUnitClass(potentialTarget, classToCheck)) ? 0f : 1f;
	}

	protected float GetParamValueByName(StaticString weightName, BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		BattleTargetingController.ParamComputationDelegate paramComputationDelegate;
		if (this.paramsComputationDelegates.TryGetValue(weightName, out paramComputationDelegate))
		{
			return paramComputationDelegate(unit, potentialTarget);
		}
		Diagnostics.Assert("This criteria {0} evaluation doesn't seem to be implemented. Did you modify the criteria name?", new object[]
		{
			weightName
		});
		return 0f;
	}

	protected float GetTargetOffensiveMilitaryPowerRatio(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float result = 0f;
		if (potentialTarget.Unit != null)
		{
			float propertyValue = potentialTarget.Unit.GetPropertyValue(SimulationProperties.OffensiveMilitaryPower);
			float unitsSimulationAverageData = this.battleSimulation.GetUnitsSimulationAverageData(potentialTarget.Unit.Contender.Group, SimulationProperties.OffensiveMilitaryPower);
			if (unitsSimulationAverageData != 0f)
			{
				result = propertyValue / unitsSimulationAverageData;
			}
		}
		return result;
	}

	protected float GetTargetDefensiveMilitaryPowerRatio(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float result = 0f;
		if (potentialTarget.Unit != null)
		{
			float propertyValue = potentialTarget.Unit.GetPropertyValue(SimulationProperties.DefensiveMilitaryPower);
			float unitsSimulationAverageData = this.battleSimulation.GetUnitsSimulationAverageData(potentialTarget.Unit.Contender.Group, SimulationProperties.DefensiveMilitaryPower);
			if (unitsSimulationAverageData != 0f)
			{
				result = propertyValue / unitsSimulationAverageData;
			}
		}
		return result;
	}

	protected float GetTargetLostHPRatio(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float targetHPRatio = this.GetTargetHPRatio(unit, potentialTarget);
		return 1f - targetHPRatio;
	}

	protected float GetDoesTargetHaveFullHP(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float targetHPRatio = this.GetTargetHPRatio(unit, potentialTarget);
		return (targetHPRatio < 1f) ? 0f : 1f;
	}

	protected float GetTargetHPRatio(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float result = 0f;
		if (potentialTarget.Unit != null)
		{
			float propertyValue = potentialTarget.Unit.GetPropertyValue(SimulationProperties.Health);
			float propertyValue2 = potentialTarget.Unit.GetPropertyValue(SimulationProperties.MaximumHealth);
			if (propertyValue2 != 0f)
			{
				result = propertyValue / propertyValue2;
			}
		}
		return result;
	}

	protected float GetTargetMorale(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return (potentialTarget.Unit == null) ? 0f : potentialTarget.Unit.GetPropertyValue(SimulationProperties.BattleMorale);
	}

	protected float GetRatioOfTargetSpeedToBattleArea(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float result = 0f;
		if (potentialTarget.Unit != null)
		{
			float propertyValue = potentialTarget.Unit.GetPropertyValue(SimulationProperties.BattleMaximumMovement);
			int num = potentialTarget.Unit.Contender.Deployment.DeploymentArea.MaxColumn - potentialTarget.Unit.Contender.Deployment.DeploymentArea.MinColumn + 1;
			result = propertyValue / (float)num;
		}
		return result;
	}

	protected float GetWithinAttackRange(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		int distance = this.worldPositionningService.GetDistance(unit.Position, potentialTarget.StaticPosition);
		return ((float)distance <= unit.GetPropertyValue(SimulationProperties.BattleRange)) ? 1f : 0f;
	}

	protected float GetWithinMovementRange(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float distanceByGround = unit.GetDistanceByGround(potentialTarget.StaticPosition);
		return (distanceByGround <= unit.GetPropertyValue(SimulationProperties.BattleMovement)) ? 1f : 0f;
	}

	protected float GetRatioOfOpponentsAroundTargetPosition(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float result = 0f;
		if (potentialTarget.Neighbours == null)
		{
			return result;
		}
		int count = potentialTarget.Neighbours.Count;
		int num = 0;
		if (count > 0)
		{
			for (int i = 0; i < count; i++)
			{
				BattleSimulationUnit unit2 = potentialTarget.Neighbours[i].Unit;
				if (unit2 != null && unit2.Contender.Group != unit.Contender.Group)
				{
					num++;
				}
			}
			result = (float)num / (float)count;
		}
		return result;
	}

	protected float RatioOfOpponentsAroundTargetPositionWhoPlayAfterMe(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float result = 0f;
		if (potentialTarget.Neighbours == null)
		{
			return result;
		}
		int count = potentialTarget.Neighbours.Count;
		int num = 0;
		if (count > 0)
		{
			for (int i = 0; i < count; i++)
			{
				BattleSimulationUnit unit2 = potentialTarget.Neighbours[i].Unit;
				if (unit2 != null && unit2.Contender.Group != unit.Contender.Group && (unit.HasUnitActions || unit.UnitRoundRank < unit2.UnitRoundRank) && this.pathfindingService.IsTransitionPassable(potentialTarget.StaticPosition, unit2.Position, unit, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreEncounterAreas | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreKaijuGarrisons, null))
				{
					num++;
				}
			}
			result = (float)num / (float)count;
		}
		return result;
	}

	protected float IsNoEnemyAroundTargetPositionWhoPlaysAfterMe(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return (this.RatioOfOpponentsAroundTargetPositionWhoPlayAfterMe(unit, potentialTarget) <= 0f) ? 1f : 0f;
	}

	protected float GetRatioOfAlliesAroundTargetPosition(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float result = 0f;
		if (potentialTarget.Neighbours == null)
		{
			return result;
		}
		int count = potentialTarget.Neighbours.Count;
		int num = 0;
		if (count > 0)
		{
			for (int i = 0; i < count; i++)
			{
				BattleSimulationUnit unit2 = potentialTarget.Neighbours[i].Unit;
				if (unit2 != null && unit2.Contender.Group == unit.Contender.Group)
				{
					num++;
				}
			}
			result = (float)num / (float)count;
		}
		return result;
	}

	protected float GetIsTargetHigher(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float propertyValue = unit.GetPropertyValue(SimulationProperties.Altitude);
		float num = (float)unit.ElevationMap.GetValue(potentialTarget.StaticPosition);
		if (propertyValue < num)
		{
			return 1f;
		}
		return 0f;
	}

	protected float GetIsTargetLower(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float propertyValue = unit.GetPropertyValue(SimulationProperties.Altitude);
		float num = (float)unit.ElevationMap.GetValue(potentialTarget.StaticPosition);
		if (propertyValue > num)
		{
			return 1f;
		}
		return 0f;
	}

	protected float GetRatioOfTargetAltitudeToMyAltitude(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float result = 0f;
		float propertyValue = unit.GetPropertyValue(SimulationProperties.Altitude);
		if (propertyValue != 0f)
		{
			float num = (float)unit.ElevationMap.GetValue(potentialTarget.StaticPosition);
			result = num / propertyValue;
		}
		return result;
	}

	protected float GetRatioMyAttackToTargetDefense(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float result = 2f;
		BattleSimulationUnit unit2 = potentialTarget.Unit;
		if (unit2 != null)
		{
			float propertyValue = unit2.GetPropertyValue(SimulationProperties.Defense);
			if (propertyValue != 0f)
			{
				result = Mathf.Clamp(unit.GetPropertyValue(SimulationProperties.Attack) / propertyValue, 0.5f, 2f);
			}
		}
		return result;
	}

	protected float GetRatioMyDefenseToTargetAttack(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float result = 2f;
		BattleSimulationUnit unit2 = potentialTarget.Unit;
		if (unit2 != null)
		{
			float propertyValue = unit2.GetPropertyValue(SimulationProperties.Attack);
			if (propertyValue != 0f)
			{
				result = Mathf.Clamp(unit.GetPropertyValue(SimulationProperties.Defense) / propertyValue, 0.5f, 2f);
			}
		}
		return result;
	}

	protected float GetRatioOfHigherNeighboursAroundTargetPosition(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float result = 0f;
		if (potentialTarget.Neighbours == null)
		{
			return result;
		}
		int count = potentialTarget.Neighbours.Count;
		float num = (float)unit.ElevationMap.GetValue(potentialTarget.StaticPosition);
		int num2 = 0;
		if (count > 0)
		{
			for (int i = 0; i < count; i++)
			{
				float num3 = (float)unit.ElevationMap.GetValue(potentialTarget.Neighbours[i].StaticPosition);
				if (num3 > num)
				{
					num2++;
				}
			}
			result = (float)num2 / (float)count;
		}
		return result;
	}

	protected float GetRatioOfLowerNeighboursAroundTargetPosition(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float result = 0f;
		if (potentialTarget.Neighbours == null)
		{
			return result;
		}
		int count = potentialTarget.Neighbours.Count;
		float num = (float)unit.ElevationMap.GetValue(potentialTarget.StaticPosition);
		int num2 = 0;
		if (count > 0)
		{
			for (int i = 0; i < count; i++)
			{
				float num3 = (float)unit.ElevationMap.GetValue(potentialTarget.Neighbours[i].StaticPosition);
				if (num3 < num)
				{
					num2++;
				}
			}
			result = (float)num2 / (float)count;
		}
		return result;
	}

	protected float GetRatioOfWalkableNeighboursAroundTargetPosition(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float result = 0f;
		if (potentialTarget.Neighbours == null)
		{
			return result;
		}
		int count = potentialTarget.Neighbours.Count;
		int num = 0;
		if (count > 0)
		{
			for (int i = 0; i < count; i++)
			{
				if (this.pathfindingService.IsTilePassable(potentialTarget.Neighbours[i].StaticPosition, unit, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreEncounterAreas | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreKaijuGarrisons, null))
				{
					num++;
				}
			}
			result = (float)num / (float)count;
		}
		return result;
	}

	protected float GetRatioOfImpassableNeighboursAroundTargetPosition(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float result = 0f;
		if (potentialTarget.Neighbours == null)
		{
			return result;
		}
		int count = potentialTarget.Neighbours.Count;
		int num = 0;
		if (count > 0 && potentialTarget.Unit != null)
		{
			for (int i = 0; i < count; i++)
			{
				if (!this.pathfindingService.IsTransitionPassable(potentialTarget.StaticPosition, potentialTarget.Neighbours[i].StaticPosition, potentialTarget.Unit, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreEncounterAreas | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreKaijuGarrisons, null))
				{
					num++;
				}
			}
			result = (float)num / (float)count;
		}
		return result;
	}

	protected float GetRatioOfAllyTargettedNeighboursAroundTargetPosition(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float result = 0f;
		if (potentialTarget.Neighbours == null)
		{
			return result;
		}
		int count = potentialTarget.Neighbours.Count;
		int num = 0;
		if (count > 0 && potentialTarget.Unit != null)
		{
			for (int i = 0; i < count; i++)
			{
				if (potentialTarget.Neighbours[i].GetTargetersCountByGroup(unit.Contender.Group) > 0)
				{
					num++;
				}
			}
			result = (float)num / (float)count;
		}
		return result;
	}

	protected float GetRatioOfEnemyTargettedNeighboursAroundTargetPosition(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float result = 0f;
		if (potentialTarget.Neighbours == null)
		{
			return result;
		}
		int count = potentialTarget.Neighbours.Count;
		int num = 0;
		if (count > 0 && potentialTarget.Unit != null)
		{
			for (int i = 0; i < count; i++)
			{
				if (potentialTarget.Neighbours[i].Targeters != null)
				{
					int count2 = potentialTarget.Neighbours[i].Targeters.Count;
					if (count2 - potentialTarget.Neighbours[i].GetTargetersCountByGroup(unit.Contender.Group) > 0)
					{
						num++;
					}
				}
			}
			result = (float)num / (float)count;
		}
		return result;
	}

	protected float GetWithinAttackAndMoveRange(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float result = 0f;
		float distanceByGround = unit.GetDistanceByGround(potentialTarget.StaticPosition);
		if (distanceByGround > 0f)
		{
			float propertyValue = unit.GetPropertyValue(SimulationProperties.BattleMaximumMovement);
			float propertyValue2 = unit.GetPropertyValue(SimulationProperties.BattleRange);
			if (distanceByGround <= propertyValue + propertyValue2)
			{
				result = 1f;
			}
		}
		return result;
	}

	protected float GetRatioDistanceToAttackAndMoveRange(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float result = 0f;
		float distanceByGround = unit.GetDistanceByGround(potentialTarget.StaticPosition);
		if (distanceByGround > 0f)
		{
			float propertyValue = unit.GetPropertyValue(SimulationProperties.BattleMaximumMovement);
			float propertyValue2 = unit.GetPropertyValue(SimulationProperties.BattleRange);
			result = distanceByGround / (propertyValue + propertyValue2);
		}
		return result;
	}

	protected float GetTargetedByAlliesCount(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float result = 0f;
		List<BattleSimulationUnit> targeters = potentialTarget.Targeters;
		if (potentialTarget.Unit != null)
		{
			targeters = potentialTarget.Unit.Targeters;
		}
		if (targeters != null)
		{
			int num = 0;
			for (int i = 0; i < targeters.Count; i++)
			{
				if (targeters[i].Contender.Group == unit.Contender.Group)
				{
					num++;
				}
			}
			result = (float)num;
		}
		return result;
	}

	protected float GetTargetedByAlliesWithMyBodyCount(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float result = 0f;
		List<BattleSimulationUnit> targeters = potentialTarget.Targeters;
		if (potentialTarget.Unit != null)
		{
			targeters = potentialTarget.Unit.Targeters;
		}
		if (targeters != null)
		{
			int num = 0;
			for (int i = 0; i < targeters.Count; i++)
			{
				if (targeters[i].Contender.Group == unit.Contender.Group && targeters[i].UnitGUID != unit.UnitGUID && targeters[i].Unit.UnitDesign.UnitBodyDefinition == unit.Unit.UnitDesign.UnitBodyDefinition)
				{
					num++;
				}
			}
			result = (float)num;
		}
		return result;
	}

	protected float GetNumberOfAliveOpponents(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return (float)this.battleSimulation.GetUnitAliveOpponentsCount(unit);
	}

	protected float GetOpponentsDebufferRatio(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return this.battleSimulation.GetUnitAliveOpponentsDebufferRatio(unit);
	}

	protected float RatioOfAlliesToOpponents(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return (float)this.battleSimulation.GetUnitAliveAlliesCount(unit) / Mathf.Max(1f, (float)this.battleSimulation.GetUnitAliveOpponentsCount(unit));
	}

	protected float RatioOfOpponentsToAllies(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return (float)this.battleSimulation.GetUnitAliveOpponentsCount(unit) / Mathf.Max(1f, (float)this.battleSimulation.GetUnitAliveAlliesCount(unit));
	}

	protected bool AnyAliveOpponents(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return this.battleSimulation.GetUnitAliveOpponentsCount(unit) > 0;
	}

	protected bool IsAlly(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return potentialTarget.Unit != null && unit.BattleSimulationArmy.Group == potentialTarget.Unit.BattleSimulationArmy.Group && (!unit.IsMindControlled || unit != potentialTarget.Unit);
	}

	protected bool IsEnemy(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return potentialTarget.Unit != null && unit.BattleSimulationArmy.Group != potentialTarget.Unit.BattleSimulationArmy.Group;
	}

	protected bool IsFreePosition(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return potentialTarget.Unit == null || potentialTarget.Unit.UnitGUID == unit.UnitGUID;
	}

	protected bool IsNotMe(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return potentialTarget.Unit == null || unit.UnitGUID != potentialTarget.Unit.UnitGUID;
	}

	protected bool IAmCombatUnit(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return unit.Unit != null && !this.IsUnitClass(unit, "UnitClassSupport") && !this.IsUnitClass(unit, "UnitClassRanged");
	}

	protected bool IAmInLightForm(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		if (unit.Unit == null)
		{
			return false;
		}
		SimulationProperty property = unit.Unit.SimulationObject.GetProperty(SimulationProperties.ShiftingForm);
		return property != null && property.Value == 0f;
	}

	protected bool IAmInDarkForm(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		if (unit.Unit == null)
		{
			return false;
		}
		SimulationProperty property = unit.Unit.SimulationObject.GetProperty(SimulationProperties.ShiftingForm);
		return property != null && property.Value == 1f;
	}

	protected bool IAmTransportShip(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return unit.Unit != null && unit.Unit.SimulationObject.Tags.Contains(DownloadableContent16.TransportShipUnit);
	}

	protected bool IAmNotTransportShip(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return !this.IAmTransportShip(unit, potentialTarget);
	}

	protected bool AlliesArePresent(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return this.battleSimulation.GetUnitAliveAlliesCount(unit) > 1;
	}

	protected bool IsTargetWithinMovementRange(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		float distanceByGround = unit.GetDistanceByGround(potentialTarget.StaticPosition);
		return distanceByGround <= unit.GetPropertyValue(SimulationProperties.BattleMovement);
	}

	protected bool CanTargetBeDebuffed(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return potentialTarget.Unit != null && potentialTarget.Unit.SimulationObject.GetPropertyValue("CantBeDebuffed") <= 0f;
	}

	protected bool IsTargetNotMindControlled(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return potentialTarget.Unit != null && potentialTarget.Unit.SimulationObject.GetPropertyValue(SimulationProperties.MindControlCounter) <= 0f;
	}

	protected bool IsUnit(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		return !this.IsFreePosition(unit, potentialTarget);
	}

	protected bool IsGroundUnit(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget)
	{
		if (potentialTarget.Unit == null)
		{
			return false;
		}
		return potentialTarget.Unit.SimulationObject.DescriptorHolders.Exists((SimulationDescriptorHolder match) => match.Descriptor.Name == PathfindingContext.MovementCapacityWalkDescriptor);
	}

	protected bool IsUnitClass(BattleSimulationUnit potentialTarget, string classToCheck)
	{
		return potentialTarget.SimulationObject.Tags.Contains(classToCheck);
	}

	protected IDatabase<BattleTargetingUnitBehaviorWeight> battleTargetingUnitBehaviorWeightDatabase;

	protected Dictionary<StaticString, BattleTargetingController.FilterComputationDelegate> filtersComputationDelegates;

	protected Dictionary<StaticString, BattleTargetingController.ParamComputationDelegate> paramsComputationDelegates;

	protected IDatabase<Amplitude.Unity.Framework.AnimationCurve> targettingAnimationCurveDatabase;

	private readonly BattleSimulation battleSimulation;

	private IWorldPositionningService worldPositionningService;

	private IPathfindingService pathfindingService;

	protected delegate bool FilterComputationDelegate(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget);

	protected delegate float ParamComputationDelegate(BattleSimulationUnit unit, BattleSimulationTarget potentialTarget);
}
