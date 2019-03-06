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
			if (army.Hero != null)
			{
				DepartmentOfEducation agency = army.Empire.GetAgency<DepartmentOfEducation>();
				if (agency != null)
				{
					agency.UnassignHero(army.Hero);
					return State.Running;
				}
			}
			if (army.Hero != null)
			{
				return State.Failure;
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
}
