using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Action_Ward : AIBehaviorTreeNode_Action
{
	public AIBehaviorTreeNode_Action_Ward()
	{
		this.failuresFlags = new List<StaticString>();
	}

	public override void Release()
	{
		base.Release();
		this.ticket = null;
	}

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		if (this.ticket != null)
		{
			if (!this.orderExecuted)
			{
				return State.Running;
			}
			if (this.ticket.PostOrderResponse == PostOrderResponse.PreprocessHasFailed)
			{
				this.orderExecuted = false;
				this.ticket = null;
				aiBehaviorTree.ErrorCode = 1;
				return State.Failure;
			}
			this.orderExecuted = false;
			this.ticket = null;
			return State.Success;
		}
		else
		{
			Army army;
			if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) != AIArmyMission.AIArmyMissionErrorCode.None)
			{
				return State.Failure;
			}
			if (this.worldPositionningService.IsWaterTile(army.WorldPosition))
			{
				return State.Failure;
			}
			float propertyValue = army.GetPropertyValue(SimulationProperties.MaximumNumberOfActionPoints);
			float propertyValue2 = army.GetPropertyValue(SimulationProperties.ActionPointsSpent);
			float costInActionPoints = this.armyActionWard.GetCostInActionPoints();
			if (propertyValue < propertyValue2 + costInActionPoints)
			{
				aiBehaviorTree.ErrorCode = 33;
				return State.Failure;
			}
			this.failuresFlags.Clear();
			if (!this.armyActionWard.CanExecute(army, ref this.failuresFlags, new object[0]))
			{
				return State.Failure;
			}
			this.orderExecuted = false;
			this.armyActionWard.Execute(army, aiBehaviorTree.AICommander.Empire.PlayerControllers.AI, out this.ticket, new EventHandler<TicketRaisedEventArgs>(this.Order_TicketRaised), new object[0]);
			return State.Running;
		}
	}

	protected override bool Initialize(AIBehaviorTree aiBehaviorTree)
	{
		ArmyAction armyAction;
		if (Databases.GetDatabase<ArmyAction>(false).TryGetValue("ArmyActionWard", out armyAction))
		{
			this.armyActionWard = (armyAction as ArmyAction_Ward);
		}
		IGameService service = Services.GetService<IGameService>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		return base.Initialize(aiBehaviorTree);
	}

	private void Order_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		this.orderExecuted = true;
	}

	private ArmyAction_Ward armyActionWard;

	private List<StaticString> failuresFlags;

	private bool orderExecuted;

	private Ticket ticket;

	private IWorldPositionningService worldPositionningService;
}
