using System;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class NavyTask_FillFortress : NavyTask
{
	public NavyTask_FillFortress(AILayer_Navy navyLayer, NavyFortress fortress) : base(navyLayer)
	{
		base.TargetGuid = fortress.Garrison.GUID;
		this.NavyFortress = fortress;
		base.Behavior = new NavyBehavior_Reinforcement();
		base.Behavior.Initialize();
		IGameService service = Services.GetService<IGameService>();
		this.worldPositionService = service.Game.Services.GetService<IWorldPositionningService>();
	}

	public NavyFortress NavyFortress { get; set; }

	public int ReinforcementSlots { get; set; }

	public override bool CheckValidity()
	{
		if (this.NavyFortress == null || this.NavyFortress.Garrison == null)
		{
			return false;
		}
		this.ReinforcementSlots = this.NavyFortress.Garrison.MaximumUnitSlot - this.NavyFortress.Garrison.CurrentUnitSlot;
		return this.ReinforcementSlots > 0 && (base.AssignedArmy == null || base.AssignedArmy.Garrison.StandardUnits.Count != 0);
	}

	public override NavyTaskEvaluation ComputeFitness(BaseNavyArmy navyGarrison)
	{
		HeuristicValue taskFitness = this.GetTaskFitness(navyGarrison);
		return new NavyTaskEvaluation
		{
			Fitness = taskFitness,
			Task = this
		};
	}

	private HeuristicValue GetTaskFitness(BaseNavyArmy navyGarrison)
	{
		HeuristicValue heuristicValue = new HeuristicValue(0f);
		if (navyGarrison.Garrison.GUID == base.TargetGuid)
		{
			heuristicValue.Value = -1f;
			heuristicValue.Log("Cannot reinforce itself.", new object[0]);
		}
		else if (navyGarrison.Role == BaseNavyArmy.ArmyRole.Land || navyGarrison.Role == BaseNavyArmy.ArmyRole.Forteress || navyGarrison.Role == BaseNavyArmy.ArmyRole.Convoi)
		{
			heuristicValue.Value = -1f;
			heuristicValue.Log("Role is not valid for the task. Role={0}.", new object[]
			{
				navyGarrison.Role.ToString()
			});
		}
		else if (navyGarrison.Garrison.StandardUnits.Count == 0)
		{
			heuristicValue.Value = -1f;
			heuristicValue.Log("Army is empty, cannot be used as reinforcement.", new object[0]);
		}
		else
		{
			heuristicValue.Add(this.NavyFortress.WantToKeepArmyFitness, "Fortress army need", new object[0]);
			if (navyGarrison.Role == BaseNavyArmy.ArmyRole.TaskForce)
			{
				heuristicValue.Boost(-0.4f, "constant avoid reinforce while task force.", new object[0]);
				float num = (float)navyGarrison.Garrison.CurrentUnitSlot;
				float operand = Math.Abs((float)this.ReinforcementSlots - num);
				HeuristicValue heuristicValue2 = new HeuristicValue(0f);
				heuristicValue2.Add(operand, "ABS(TaskSlotNeeded - armySize)", new object[0]);
				heuristicValue2.Divide(num, "Army size", new object[0]);
				heuristicValue.Subtract(heuristicValue2, "Size ratio", new object[0]);
			}
			if (heuristicValue.Value > 0f)
			{
				if (this.NavyFortress.ArmySize < BaseNavyArmy.ArmyState.Medium)
				{
					heuristicValue.Boost(0.2f, "(constant)Under medium", new object[0]);
				}
				else if (this.NavyFortress.ArmySize < BaseNavyArmy.ArmyState.High)
				{
					heuristicValue.Boost(0.1f, "(constant)Under high", new object[0]);
				}
				float num2 = (float)this.worldPositionService.GetDistance(navyGarrison.Garrison.WorldPosition, this.NavyFortress.Garrison.WorldPosition);
				float numberOfTurnToReach = num2 / navyGarrison.GetMaximumMovement();
				heuristicValue.Multiply(base.ComputeDistanceFitness(numberOfTurnToReach, navyGarrison.Role), "Distance", new object[0]);
			}
		}
		return heuristicValue;
	}

	private IWorldPositionningService worldPositionService;
}
