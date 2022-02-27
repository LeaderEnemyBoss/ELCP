using System;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class NavyTask_Takeover : NavyTask
{
	public NavyTask_Takeover(AILayer_Navy navyLayer, Fortress fortress) : base(navyLayer)
	{
		base.TargetGuid = fortress.GUID;
		this.FortressPosition = fortress.WorldPosition;
		base.Behavior = new NavyBehavior_Interception();
		base.Behavior.Initialize();
	}

	public WorldPosition FortressPosition { get; set; }

	public override bool CheckValidity()
	{
		IGameService service = Services.GetService<IGameService>();
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		Region region = service2.GetRegion(this.FortressPosition);
		if (region.NavalEmpire == null)
		{
			return false;
		}
		PirateCouncil agency = region.NavalEmpire.GetAgency<PirateCouncil>();
		if (agency == null)
		{
			return false;
		}
		Fortress fortressAt = agency.GetFortressAt(this.FortressPosition);
		if (fortressAt == null)
		{
			return false;
		}
		if (fortressAt.Occupant != null && !base.NavyLayer.MightAttackOwner(region, fortressAt.Occupant))
		{
			return false;
		}
		if (base.AssignedArmy != null && base.AssignedArmy.Garrison == null)
		{
			base.AssignedArmy = null;
			base.CurrentAssignationFitness.Reset();
		}
		if (base.AssignedArmy != null)
		{
			float enemyPower = this.GetEnemyPower();
			float propertyValue = base.AssignedArmy.Garrison.GetPropertyValue(SimulationProperties.MilitaryPower);
			if (propertyValue < enemyPower * 0.8f)
			{
				return false;
			}
		}
		return true;
	}

	public override NavyTaskEvaluation ComputeFitness(BaseNavyArmy navyGarrison)
	{
		IGameService service = Services.GetService<IGameService>();
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		NavyTaskEvaluation navyTaskEvaluation = new NavyTaskEvaluation();
		navyTaskEvaluation.Fitness = new HeuristicValue(0f);
		navyTaskEvaluation.Task = this;
		if (navyGarrison.Role == BaseNavyArmy.ArmyRole.Land || navyGarrison.Role == BaseNavyArmy.ArmyRole.Convoi)
		{
			navyTaskEvaluation.Fitness.Value = -1f;
			navyTaskEvaluation.Fitness.Log("Role is not valid for the task. Role={0}.", new object[]
			{
				navyGarrison.Role.ToString()
			});
		}
		else
		{
			float enemyPower = this.GetEnemyPower();
			float propertyValue = navyGarrison.Garrison.GetPropertyValue(SimulationProperties.MilitaryPower);
			navyTaskEvaluation.Fitness.Add(base.ComputePowerFitness(enemyPower, propertyValue), "MilitaryPower", new object[0]);
			if (navyTaskEvaluation.Fitness > 0f)
			{
				float num = (float)service2.GetDistance(navyGarrison.Garrison.WorldPosition, this.FortressPosition);
				float numberOfTurnToReach = num / navyGarrison.GetMaximumMovement();
				navyTaskEvaluation.Fitness.Multiply(base.ComputeDistanceFitness(numberOfTurnToReach, navyGarrison.Role), "Distance", new object[0]);
				if (navyGarrison.Role == BaseNavyArmy.ArmyRole.Forteress)
				{
					navyTaskEvaluation.Fitness.Boost(-0.2f, "Fortress...", new object[0]);
				}
				float propertyValue2 = navyGarrison.Garrison.GetPropertyValue(SimulationProperties.ActionPointsSpent);
				if (propertyValue2 > 0f)
				{
					navyTaskEvaluation.Fitness.Boost(-0.2f, "No more action point...", new object[0]);
				}
			}
		}
		return navyTaskEvaluation;
	}

	public override string GetDebugTitle()
	{
		return base.GetDebugTitle() + " at " + this.FortressPosition.ToString();
	}

	private float GetEnemyPower()
	{
		IGameService service = Services.GetService<IGameService>();
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		Region region = service2.GetRegion(this.FortressPosition);
		if (region.NavalEmpire == null)
		{
			return 0f;
		}
		PirateCouncil agency = region.NavalEmpire.GetAgency<PirateCouncil>();
		if (agency == null)
		{
			return 0f;
		}
		Fortress fortressAt = agency.GetFortressAt(this.FortressPosition);
		if (fortressAt == null)
		{
			return 0f;
		}
		return fortressAt.GetPropertyValue(SimulationProperties.MilitaryPower);
	}
}
