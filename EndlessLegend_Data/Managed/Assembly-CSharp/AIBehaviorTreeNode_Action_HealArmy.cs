using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Action_HealArmy : AIBehaviorTreeNode_Action
{
	[XmlAttribute]
	public string TargetVarName { get; set; }

	public override void Release()
	{
		base.Release();
		this.ticket = null;
		this.armyActionHeal = null;
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
			if (!aiBehaviorTree.Variables.ContainsKey(this.TargetVarName))
			{
				aiBehaviorTree.LogError("${0} not set", new object[]
				{
					this.TargetVarName
				});
				return State.Failure;
			}
			Army army2 = aiBehaviorTree.Variables[this.TargetVarName] as Army;
			if (army2 == null)
			{
				return State.Failure;
			}
			List<StaticString> list = new List<StaticString>();
			if (!this.armyActionHeal.CanExecute(army, ref list, new object[]
			{
				army2
			}))
			{
				return State.Failure;
			}
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			IEncounterRepositoryService service2 = service.Game.Services.GetService<IEncounterRepositoryService>();
			if (service2 != null)
			{
				IEnumerable<Encounter> enumerable = service2;
				if (enumerable != null && enumerable.Any((Encounter encounter) => encounter.IsGarrisonInEncounter(army.GUID, false)))
				{
					return State.Running;
				}
			}
			this.orderExecuted = false;
			this.armyActionHeal.Execute(army, aiBehaviorTree.AICommander.Empire.PlayerControllers.AI, out this.ticket, new EventHandler<TicketRaisedEventArgs>(this.Order_TicketRaised), new object[]
			{
				army2
			});
			return State.Running;
		}
	}

	protected override bool Initialize(AIBehaviorTree aiBehaviorTree)
	{
		ArmyAction armyAction;
		if (Databases.GetDatabase<ArmyAction>(false).TryGetValue("ArmyActionHeal", out armyAction))
		{
			this.armyActionHeal = (armyAction as ArmyAction_Heal);
		}
		return base.Initialize(aiBehaviorTree);
	}

	private void Order_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		this.orderExecuted = true;
	}

	private ArmyAction_Heal armyActionHeal;

	private bool orderExecuted;

	private Ticket ticket;
}
