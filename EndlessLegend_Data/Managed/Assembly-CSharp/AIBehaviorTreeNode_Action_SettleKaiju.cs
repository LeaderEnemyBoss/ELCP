using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;

public class AIBehaviorTreeNode_Action_SettleKaiju : AIBehaviorTreeNode_Action
{
	public AIBehaviorTreeNode_Action_SettleKaiju()
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
			if (!(army is KaijuArmy))
			{
				return State.Failure;
			}
			Kaiju kaiju = (army as KaijuArmy).Kaiju;
			if (kaiju == null || !(army.Empire is MajorEmpire) || kaiju.OnGarrisonMode() || !KaijuCouncil.IsPositionValidForSettleKaiju(army.WorldPosition, kaiju))
			{
				return State.Failure;
			}
			this.failuresFlags.Clear();
			if (!this.armyAction_SettleKaiju.CanExecute(army, ref this.failuresFlags, new object[0]))
			{
				return State.Failure;
			}
			this.orderExecuted = false;
			this.armyAction_SettleKaiju.Execute(army, aiBehaviorTree.AICommander.Empire.PlayerControllers.AI, out this.ticket, new EventHandler<TicketRaisedEventArgs>(this.Order_TicketRaised), new object[0]);
			return State.Running;
		}
	}

	protected override bool Initialize(AIBehaviorTree aiBehaviorTree)
	{
		IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
		ArmyAction armyAction = null;
		if (database == null || !database.TryGetValue(ArmyAction_SettleKaiju.ReadOnlyName, out armyAction))
		{
			Diagnostics.LogError("AIBehaviorTreeNode_Action_SettleKaiju didnt find " + ArmyAction_SettleKaiju.ReadOnlyName);
		}
		else
		{
			this.armyAction_SettleKaiju = (armyAction as ArmyAction_SettleKaiju);
		}
		return base.Initialize(aiBehaviorTree);
	}

	private void Order_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		this.orderExecuted = true;
	}

	private ArmyAction_SettleKaiju armyAction_SettleKaiju;

	private List<StaticString> failuresFlags;

	private bool orderExecuted;

	private Ticket ticket;
}
