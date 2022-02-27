using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class NavyFortress : BaseNavyArmy
{
	public NavyFortress(AILayer_Navy navyLayer, Fortress fortress)
	{
		this.Fortress = fortress;
		base.Garrison = fortress;
		this.navyLayer = navyLayer;
		IGameService service = Services.GetService<IGameService>();
		this.worldPositionService = service.Game.Services.GetService<IWorldPositionningService>();
		this.WantToKeepArmyFitness = new HeuristicValue(0f);
		this.lastcheckTime = 0.0;
	}

	public HeuristicValue WantToKeepArmyFitness { get; set; }

	public Fortress Fortress { get; set; }

	public override void UpdateRole()
	{
		base.Role = BaseNavyArmy.ArmyRole.Forteress;
		this.WantToKeepArmyFitness.Reset();
		NavyRegionData navyRegionData = base.Commander.RegionData as NavyRegionData;
		this.WantToKeepArmyFitness.Add(0.3f, "constant", new object[0]);
		if (navyRegionData.NumberOfWaterEnemy > 0)
		{
			HeuristicValue heuristicValue = new HeuristicValue(0f);
			heuristicValue.Add((float)navyRegionData.NumberOfWaterEnemy, "Number of enemy water region around.", new object[0]);
			heuristicValue.Multiply(0.1f, "boost constant", new object[0]);
			heuristicValue.Min(0.5f, "Avoid too big factor!", new object[0]);
			this.WantToKeepArmyFitness.Boost(heuristicValue, "Water region owned by enemy around.", new object[0]);
		}
		if (navyRegionData.NumberOfEnemyCityOnTheBorder > 0)
		{
			this.WantToKeepArmyFitness.Boost(0.2f, "Enemy city in the region.", new object[0]);
		}
		if (navyRegionData.EnemyNavalPower > 0f)
		{
			this.WantToKeepArmyFitness.Boost(0.9f, "Enemy roaming in the region.", new object[0]);
		}
		MajorEmpire occupant = this.Fortress.Occupant;
		if (occupant != null && !AILayer_Military.AreaIsSave(this.Fortress.WorldPosition, 10, occupant.GetAgency<DepartmentOfForeignAffairs>(), true))
		{
			this.WantToKeepArmyFitness.Boost(0.9f, "Enemy roaming in the region.", new object[0]);
		}
	}

	public override void AssignCommander(BaseNavyCommander commander)
	{
		if (base.Commander != null)
		{
			NavyCommander navyCommander = base.Commander as NavyCommander;
			if (navyCommander != null)
			{
				navyCommander.NavyFortresses.Remove(this);
			}
		}
		base.AssignCommander(commander);
		if (base.Commander != null)
		{
			NavyCommander navyCommander2 = base.Commander as NavyCommander;
			if (navyCommander2 != null)
			{
				navyCommander2.NavyFortresses.Add(this);
			}
		}
	}

	public override float GetMaximumMovement()
	{
		float num = float.MaxValue;
		for (int i = 0; i < base.Garrison.StandardUnits.Count; i++)
		{
			float propertyValue = base.Garrison.StandardUnits[i].GetPropertyValue(SimulationProperties.MaximumMovementOnWater);
			if (num > propertyValue)
			{
				num = propertyValue;
			}
		}
		if (num == 3.40282347E+38f)
		{
			num = 1f;
		}
		return num;
	}

	protected override ArmyBehavior GetDefaultBehavior()
	{
		return null;
	}

	protected override void ExecuteMainTask()
	{
		if (this.Fortress.StandardUnits.Count == 0)
		{
			base.BehaviorState = ArmyWithTask.ArmyBehaviorState.Sleep;
			base.State = TickableState.NoTick;
			return;
		}
		float num = float.MaxValue;
		GameEntityGUID[] array = new GameEntityGUID[base.Garrison.StandardUnits.Count];
		for (int i = 0; i < base.Garrison.StandardUnits.Count; i++)
		{
			array[i] = base.Garrison.StandardUnits[i].GUID;
			float propertyValue = base.Garrison.StandardUnits[i].GetPropertyValue(SimulationProperties.Movement);
			if (num > propertyValue)
			{
				num = propertyValue;
			}
		}
		if (num < 2f)
		{
			base.BehaviorState = ArmyWithTask.ArmyBehaviorState.Sleep;
			base.State = TickableState.NoTick;
			return;
		}
		if (base.CurrentMainTask == null)
		{
			base.State = TickableState.Optional;
			return;
		}
		WorldPosition armyPosition = WorldPosition.Invalid;
		Fortress fortress = base.Garrison as Fortress;
		for (int j = 0; j < 6; j++)
		{
			WorldPosition neighbourTile = this.worldPositionService.GetNeighbourTile(fortress.WorldPosition, (WorldOrientation)j, 1);
			if (DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(neighbourTile, PathfindingMovementCapacity.Water))
			{
				armyPosition = neighbourTile;
				break;
			}
		}
		if (!armyPosition.IsValid)
		{
			for (int k = 0; k < fortress.Facilities.Count; k++)
			{
				for (int l = 0; l < 6; l++)
				{
					WorldPosition neighbourTile2 = this.worldPositionService.GetNeighbourTile(fortress.Facilities[k].WorldPosition, (WorldOrientation)l, 1);
					if (DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(neighbourTile2, PathfindingMovementCapacity.Water))
					{
						armyPosition = neighbourTile2;
						break;
					}
				}
			}
		}
		OrderTransferGarrisonToNewArmy order = new OrderTransferGarrisonToNewArmy(base.Garrison.Empire.Index, base.Garrison.GUID, array, armyPosition, null, false, true, true);
		base.Garrison.Empire.PlayerControllers.AI.PostOrder(order);
		this.Unassign();
		this.WantToKeepArmyFitness.Reset();
		base.State = TickableState.NoTick;
		base.BehaviorState = ArmyWithTask.ArmyBehaviorState.Succeed;
	}

	protected override void FilterTasks()
	{
		base.TaskEvaluations.Clear();
		if (base.Garrison.CurrentUnitSlot > 0)
		{
			for (int i = 0; i < this.navyLayer.NavyTasks.Count; i++)
			{
				if (this.navyLayer.NavyTasks[i].CheckValidity())
				{
					NavyTaskEvaluation navyTaskEvaluation = this.navyLayer.NavyTasks[i].ComputeFitness(this);
					if (base.CurrentMainTask == this.navyLayer.NavyTasks[i] || this.navyLayer.NavyTasks[i].AssignedArmy == this)
					{
						navyTaskEvaluation.Fitness.Boost(0.2f, "Already assigned", new object[0]);
					}
					base.TaskEvaluations.Add(navyTaskEvaluation);
				}
			}
		}
		List<IGarrison> source = new List<IGarrison>();
		if (global::Game.Time > this.lastcheckTime + 10.0)
		{
			source = this.GetNearbyTargets();
			this.lastcheckTime = global::Game.Time;
		}
		int j;
		int k;
		for (j = 0; j < base.TaskEvaluations.Count; j = k + 1)
		{
			if (!(base.TaskEvaluations[j].Task is NavyTask_Takeover) || !source.Any((IGarrison x) => x.GUID == this.TaskEvaluations[j].Task.TargetGuid))
			{
				NavyTask_Interception interc = base.TaskEvaluations[j].Task as NavyTask_Interception;
				if (interc == null || !source.Any((IGarrison x) => x.GUID == interc.Target.Army.GUID))
				{
					base.TaskEvaluations[j].Fitness.Subtract(this.WantToKeepArmyFitness, "Reduce the task by the fortress need for army.", new object[0]);
				}
			}
			k = j;
		}
	}

	private List<IGarrison> GetNearbyTargets()
	{
		List<IGarrison> result = new List<IGarrison>();
		if (this.Fortress.Occupant == null)
		{
			return result;
		}
		float propertyValue = this.Fortress.GetPropertyValue(SimulationProperties.MaximumNumberOfActionPoints);
		float propertyValue2 = this.Fortress.GetPropertyValue(SimulationProperties.ActionPointsSpent);
		if (propertyValue <= propertyValue2)
		{
			return result;
		}
		DepartmentOfForeignAffairs agency = this.Fortress.Occupant.GetAgency<DepartmentOfForeignAffairs>();
		float num = float.MaxValue;
		foreach (Unit unit in this.Fortress.StandardUnits)
		{
			num = Mathf.Min(unit.GetPropertyValue(SimulationProperties.Movement), num);
		}
		if (num == 3.40282347E+38f || num < 3f)
		{
			return result;
		}
		AILayer_Military.HasSaveAttackableTargetsNearby(this.Fortress, Mathf.CeilToInt(num + 1f), agency, out result, true);
		return result;
	}

	private IWorldPositionningService worldPositionService;

	private AILayer_Navy navyLayer;

	private double lastcheckTime;
}
