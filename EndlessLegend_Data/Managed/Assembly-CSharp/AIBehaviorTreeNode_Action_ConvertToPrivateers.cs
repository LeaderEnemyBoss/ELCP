using System;
using System.Collections.Generic;
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
			if (this.orderTicket.PostOrderResponse != PostOrderResponse.Processed)
			{
				aiBehaviorTree.ErrorCode = 36;
				return State.Failure;
			}
			this.orderTicket = null;
			return State.Success;
		}
		else if (this.heroTicket != null)
		{
			if (!this.heroTicket.Raised)
			{
				return State.Running;
			}
			if (this.heroTicket.PostOrderResponse != PostOrderResponse.Processed)
			{
				aiBehaviorTree.ErrorCode = 36;
				return State.Failure;
			}
			this.heroTicket = null;
			return State.Running;
		}
		else
		{
			Army army;
			if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) != AIArmyMission.AIArmyMissionErrorCode.None)
			{
				return State.Failure;
			}
			if (army.IsPrivateers)
			{
				return State.Success;
			}
			using (IEnumerator<Unit> enumerator = army.StandardUnits.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (!enumerator.Current.UnitDesign.Tags.Contains(TradableUnit.ReadOnlyMercenary))
					{
						return State.Failure;
					}
				}
			}
			if (army.Hero != null)
			{
				OrderChangeHeroAssignment orderChangeHeroAssignment = new OrderChangeHeroAssignment(aiBehaviorTree.AICommander.Empire.Index, army.Hero.GUID, GameEntityGUID.Zero);
				orderChangeHeroAssignment.IgnoreCooldown = true;
				aiBehaviorTree.AICommander.Empire.PlayerControllers.AI.PostOrder(orderChangeHeroAssignment, out this.heroTicket, null);
				return State.Running;
			}
			Region region = service.Game.Services.GetService<IWorldPositionningService>().GetRegion(army.WorldPosition);
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

	private Ticket heroTicket;
}
