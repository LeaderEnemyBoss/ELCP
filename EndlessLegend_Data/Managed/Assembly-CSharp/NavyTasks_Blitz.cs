using System;
using System.Collections.Generic;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class NavyTasks_Blitz : NavyTask
{
	public NavyTasks_Blitz(AILayer_Navy navyLayer, City city) : base(navyLayer)
	{
		this.Target = city;
		base.TargetGuid = city.GUID;
		base.Behavior = new NavyBehavior_Blitz();
		base.Behavior.Initialize();
		IGameService service = Services.GetService<IGameService>();
		this.worldPositionService = service.Game.Services.GetService<IWorldPositionningService>();
	}

	public City Target { get; set; }

	public override bool CheckValidity()
	{
		return this.Target != null && this.Target.Empire != null && this.Target.Empire.Index != base.Owner.Index && base.NavyLayer.MightAttackOwner(this.Target.Region, this.Target.Empire);
	}

	public override NavyTaskEvaluation ComputeFitness(BaseNavyArmy navyGarrison)
	{
		NavyTaskEvaluation navyTaskEvaluation = new NavyTaskEvaluation();
		navyTaskEvaluation.Fitness = new HeuristicValue(0f);
		navyTaskEvaluation.Task = this;
		if (navyGarrison.Garrison.GetPropertyValue(SimulationProperties.CityDefensePointLossPerTurn) <= 0f)
		{
			navyTaskEvaluation.Fitness.Value = -1f;
			navyTaskEvaluation.Fitness.Log("Avoid using army without fortification damage.", new object[0]);
		}
		else
		{
			navyTaskEvaluation.Fitness.Add(1f, "(constant)", new object[0]);
			float propertyValue = this.Target.GetPropertyValue(SimulationProperties.DefensivePower);
			float propertyValue2 = this.Target.GetPropertyValue(SimulationProperties.CoastalDefensivePower);
			if (propertyValue + propertyValue2 > 0f)
			{
				float num = (propertyValue + propertyValue2) / (float)navyGarrison.Garrison.UnitsCount;
				using (IEnumerator<Unit> enumerator = navyGarrison.Garrison.Units.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (enumerator.Current.GetPropertyValue(SimulationProperties.Health) - num < num * 2f)
						{
							navyTaskEvaluation.Fitness.Subtract(0.2f, "(constant) Retaliation will kill units.", new object[0]);
						}
					}
				}
			}
			float numberOfTurnToReach = (float)this.worldPositionService.GetDistance(navyGarrison.Garrison.WorldPosition, this.Target.WorldPosition) / navyGarrison.GetMaximumMovement();
			navyTaskEvaluation.Fitness.Multiply(base.ComputeDistanceFitness(numberOfTurnToReach, navyGarrison.Role), "Distance", new object[0]);
		}
		return navyTaskEvaluation;
	}

	public override string GetDebugTitle()
	{
		return base.GetDebugTitle() + " at " + this.Target.WorldPosition;
	}

	private IWorldPositionningService worldPositionService;
}
