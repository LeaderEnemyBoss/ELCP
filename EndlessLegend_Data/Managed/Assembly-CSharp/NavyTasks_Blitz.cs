using System;
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
		float propertyValue = navyGarrison.Garrison.GetPropertyValue(SimulationProperties.CityDefensePointLossPerTurn);
		if (propertyValue <= 0f)
		{
			navyTaskEvaluation.Fitness.Value = -1f;
			navyTaskEvaluation.Fitness.Log("Avoid using army without fortification damage.", new object[0]);
		}
		else
		{
			navyTaskEvaluation.Fitness.Add(0.8f, "(constant)", new object[0]);
			float propertyValue2 = this.Target.GetPropertyValue(SimulationProperties.DefensivePower);
			float propertyValue3 = this.Target.GetPropertyValue(SimulationProperties.CoastalDefensivePower);
			if (propertyValue2 + propertyValue3 > 0f)
			{
				float num = (propertyValue2 + propertyValue3) / (float)navyGarrison.Garrison.UnitsCount;
				foreach (Unit unit in navyGarrison.Garrison.Units)
				{
					float propertyValue4 = unit.GetPropertyValue(SimulationProperties.Health);
					if (propertyValue4 - num < num * 2f)
					{
						navyTaskEvaluation.Fitness.Subtract(0.2f, "(constant) Retaliation will kill units.", new object[0]);
					}
				}
			}
			float num2 = (float)this.worldPositionService.GetDistance(navyGarrison.Garrison.WorldPosition, this.Target.WorldPosition);
			float numberOfTurnToReach = num2 / navyGarrison.GetMaximumMovement();
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
