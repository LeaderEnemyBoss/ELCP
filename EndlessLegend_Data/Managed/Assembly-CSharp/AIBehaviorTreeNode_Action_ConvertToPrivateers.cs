using System;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Action_ConvertToPrivateers : AIBehaviorTreeNode_Action
{
	public override void Release()
	{
		base.Release();
		this.orderTicket = null;
	}

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		if (this.orderTicket != null)
		{
			if (!this.orderTicket.Raised)
			{
				return State.Running;
			}
			bool flag = this.orderTicket.PostOrderResponse != PostOrderResponse.Processed;
			if (flag)
			{
				aiBehaviorTree.ErrorCode = 36;
				return State.Failure;
			}
			this.orderTicket = null;
			return State.Success;
		}
		else
		{
			Army army;
			AIArmyMission.AIArmyMissionErrorCode armyUnlessLocked = base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army);
			if (armyUnlessLocked != AIArmyMission.AIArmyMissionErrorCode.None)
			{
				return State.Failure;
			}
			if (army.IsPrivateers)
			{
				return State.Success;
			}
			IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
			Region region = service2.GetRegion(army.WorldPosition);
			if (region != null && region.City != null && region.City.Empire == army.Empire)
			{
				OrderTogglePrivateers order = new OrderTogglePrivateers(army.Empire.Index, army.GUID, true);
				aiBehaviorTree.AICommander.Empire.PlayerControllers.AI.PostOrder(order, out this.orderTicket, null);
				return State.Running;
			}
			aiBehaviorTree.ErrorCode = 36;
			return State.Failure;
		}
	}

	private Ticket orderTicket;
}
