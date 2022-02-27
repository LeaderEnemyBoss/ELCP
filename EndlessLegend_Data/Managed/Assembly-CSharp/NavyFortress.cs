using System;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

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
	}

	public HeuristicValue WantToKeepArmyFitness { get; set; }

	public Fortress Fortress { get; set; }

	public override void UpdateRole()
	{
		base.Role = BaseNavyArmy.ArmyRole.Forteress;
		this.WantToKeepArmyFitness.Reset();
		this.WantToKeepArmyFitness.Add(0.3f, "constant", new object[0]);
		NavyRegionData navyRegionData = base.Commander.RegionData as NavyRegionData;
		if (navyRegionData.NumberOfWaterEnemy > 0)
		{
			HeuristicValue heuristicValue = new HeuristicValue(0f);
			heuristicValue.Add((float)navyRegionData.NumberOfWaterEnemy, "Number of enemy water region around.", new object[0]);
			heuristicValue.Multiply(0.1f, "boost constant", new object[0]);
			heuristicValue.Min(0.3f, "Avoid too big factor!", new object[0]);
			this.WantToKeepArmyFitness.Boost(heuristicValue, "Water region owned by enemy around.", new object[0]);
		}
		if (navyRegionData.NumberOfEnemyCityOnTheBorder > 0)
		{
			this.WantToKeepArmyFitness.Boost(0.2f, "Enemy city in the region.", new object[0]);
		}
		if (navyRegionData.EnemyNavalPower > 0f)
		{
			this.WantToKeepArmyFitness.Boost(0.2f, "Enemy roaming in the region.", new object[0]);
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
		if (base.CurrentMainTask == null)
		{
			this.State = TickableState.Optional;
			return;
		}
		if (this.Fortress.StandardUnits.Count == 0)
		{
			base.BehaviorState = ArmyWithTask.ArmyBehaviorState.Sleep;
			this.State = TickableState.NoTick;
			return;
		}
		WorldPosition armyPosition = WorldPosition.Invalid;
		Fortress fortress = base.Garrison as Fortress;
		for (int i = 0; i < 6; i++)
		{
			WorldPosition neighbourTile = this.worldPositionService.GetNeighbourTile(fortress.WorldPosition, (WorldOrientation)i, 1);
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
		float num = float.MaxValue;
		GameEntityGUID[] array = new GameEntityGUID[base.Garrison.StandardUnits.Count];
		for (int l = 0; l < base.Garrison.StandardUnits.Count; l++)
		{
			array[l] = base.Garrison.StandardUnits[l].GUID;
			float propertyValue = base.Garrison.StandardUnits[l].GetPropertyValue(SimulationProperties.Movement);
			if (num > propertyValue)
			{
				num = propertyValue;
			}
		}
		if (num == 0f)
		{
			base.BehaviorState = ArmyWithTask.ArmyBehaviorState.Sleep;
			this.State = TickableState.NoTick;
			return;
		}
		OrderTransferGarrisonToNewArmy order = new OrderTransferGarrisonToNewArmy(base.Garrison.Empire.Index, base.Garrison.GUID, array, armyPosition, null, false, true, true);
		base.Garrison.Empire.PlayerControllers.AI.PostOrder(order);
		this.Unassign();
		this.State = TickableState.NoTick;
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
		for (int j = 0; j < base.TaskEvaluations.Count; j++)
		{
			base.TaskEvaluations[j].Fitness.Subtract(this.WantToKeepArmyFitness, "Reduce the task by the fortress need for army.", new object[0]);
		}
	}

	private IWorldPositionningService worldPositionService;

	private AILayer_Navy navyLayer;
}
