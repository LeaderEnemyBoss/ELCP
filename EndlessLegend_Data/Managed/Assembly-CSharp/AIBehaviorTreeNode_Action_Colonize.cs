using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;

public class AIBehaviorTreeNode_Action_Colonize : AIBehaviorTreeNode_Action
{
	[XmlAttribute]
	public string PathVarName { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		if (this.orderPosted)
		{
			if (this.orderExecuted)
			{
				this.orderExecuted = false;
				this.orderPosted = false;
				return State.Success;
			}
			return State.Running;
		}
		else
		{
			Army army;
			AIArmyMission.AIArmyMissionErrorCode armyUnlessLocked = base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army);
			if (armyUnlessLocked != AIArmyMission.AIArmyMissionErrorCode.None)
			{
				return State.Failure;
			}
			ArmyAction armyAction = null;
			IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
			List<ArmyAction> list = new List<ArmyAction>(database.GetValues());
			List<ArmyAction> list2 = new List<ArmyAction>(list.FindAll((ArmyAction match) => match is ArmyAction_Colonization));
			List<StaticString> list3 = new List<StaticString>();
			for (int i = 0; i < list2.Count; i++)
			{
				ArmyAction armyAction2 = list2[i];
				if (armyAction2.CanExecute(army, ref list3, new object[0]))
				{
					armyAction = list2[i];
				}
			}
			if (armyAction != null)
			{
				this.orderExecuted = false;
				this.orderPosted = true;
				Ticket ticket;
				armyAction.Execute(army, aiBehaviorTree.AICommander.Empire.PlayerControllers.AI, out ticket, new EventHandler<TicketRaisedEventArgs>(this.ArmyAction_TicketRaised), new object[0]);
				return State.Running;
			}
			aiBehaviorTree.ErrorCode = 22;
			return State.Failure;
		}
	}

	protected override bool Initialize(AIBehaviorTree aiBehaviorTree)
	{
		return base.Initialize(aiBehaviorTree);
	}

	private void ArmyAction_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		this.orderExecuted = true;
	}

	private bool orderExecuted;

	private bool orderPosted;
}
