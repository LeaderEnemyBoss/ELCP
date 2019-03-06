using System;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class NavyTask_Interception : NavyTask
{
	public NavyTask_Interception(AILayer_Navy navyLayer) : base(navyLayer)
	{
		base.Behavior = new NavyBehavior_Interception();
		base.Behavior.Initialize();
		IGameService service = Services.GetService<IGameService>();
		this.worldPositionService = service.Game.Services.GetService<IWorldPositionningService>();
		this.aiDataRepository = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		this.visibilityService = service.Game.Services.GetService<IVisibilityService>();
	}

	public AIData_Army Target
	{
		get
		{
			if (this.targetArmy == null)
			{
				this.aiDataRepository.TryGetAIData<AIData_Army>(base.TargetGuid, out this.targetArmy);
			}
			return this.targetArmy;
		}
	}

	public override bool CheckValidity()
	{
		if (this.Target == null || this.targetArmy.Army == null)
		{
			return false;
		}
		if (this.Target.Army.Empire.Index == base.Owner.Index)
		{
			return false;
		}
		if (base.Owner is MajorEmpire && this.Target.Army.Empire is MajorEmpire && base.NavyLayer.diplomacyLayer.GetPeaceWish(this.Target.Army.Empire.Index))
		{
			return false;
		}
		if (this.worldPositionService.IsFrozenWaterTile(this.Target.Army.WorldPosition))
		{
			return false;
		}
		if (!this.worldPositionService.IsWaterTile(this.Target.Army.WorldPosition))
		{
			return false;
		}
		if (this.Target.Army.IsCamouflaged && !this.visibilityService.IsWorldPositionDetectedFor(this.Target.Army.WorldPosition, base.Owner))
		{
			return false;
		}
		NavyCommander navyCommander = base.NavyLayer.FindCommanderForTaskAt(this.Target.Army.WorldPosition) as NavyCommander;
		if (navyCommander == null || navyCommander.CommanderState == NavyCommander.NavyCommanderState.Inactive)
		{
			return false;
		}
		Region region = this.worldPositionService.GetRegion(this.Target.Army.WorldPosition);
		return base.NavyLayer.MightAttackOwner(region, this.Target.Army.Empire);
	}

	public override NavyTaskEvaluation ComputeFitness(BaseNavyArmy navyGarrison)
	{
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
				WorldPosition targetPosition = this.GetTargetPosition();
				float num = (float)this.worldPositionService.GetDistance(navyGarrison.Garrison.WorldPosition, targetPosition) / navyGarrison.GetMaximumMovement();
				HeuristicValue operand = base.ComputeDistanceFitness(num, navyGarrison.Role);
				navyTaskEvaluation.Fitness.Multiply(operand, "Distance", new object[0]);
				if (num <= 1f && propertyValue > enemyPower * 1.2f)
				{
					navyTaskEvaluation.Fitness.Boost(0.9f, "Distance", new object[0]);
				}
				if (navyGarrison.Role == BaseNavyArmy.ArmyRole.Forteress)
				{
					navyTaskEvaluation.Fitness.Boost(-0.2f, "Fortress...", new object[0]);
				}
				else if (navyGarrison.Garrison.GetPropertyValue(SimulationProperties.ActionPointsSpent) > 0f)
				{
					navyTaskEvaluation.Fitness.Boost(-0.2f, "No more action point...", new object[0]);
				}
			}
		}
		if (base.AssignedArmy == navyGarrison)
		{
			navyTaskEvaluation.Fitness.Boost(0.2f, "Already assigned to the task", new object[0]);
		}
		return navyTaskEvaluation;
	}

	public WorldPosition GetTargetPosition()
	{
		if (this.Target == null || this.Target.Army == null)
		{
			return WorldPosition.Invalid;
		}
		return this.Target.Army.WorldPosition;
	}

	public override string GetDebugTitle()
	{
		return base.GetDebugTitle() + " at " + this.GetTargetPosition();
	}

	private float GetEnemyPower()
	{
		if (this.Target != null && this.Target.Army != null)
		{
			return this.Target.Army.GetPropertyValue(SimulationProperties.MilitaryPower);
		}
		return 0f;
	}

	private IWorldPositionningService worldPositionService;

	private IAIDataRepositoryAIHelper aiDataRepository;

	private AIData_Army targetArmy;

	private IVisibilityService visibilityService;
}
