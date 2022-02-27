using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Action_ToggleDismantleDevice : AIBehaviorTreeNode_Action
{
	public AIBehaviorTreeNode_Action_ToggleDismantleDevice()
	{
		this.TargetVarName = string.Empty;
		this.State = AIBehaviorTreeNode_Action_ToggleDismantleCreepingNode.StateType.Start;
	}

	[XmlAttribute]
	public AIBehaviorTreeNode_Action_ToggleDismantleCreepingNode.StateType State { get; set; }

	[XmlAttribute]
	public string TargetVarName { get; set; }

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
				return Amplitude.Unity.AI.BehaviourTree.State.Running;
			}
			if (this.ticket.PostOrderResponse == PostOrderResponse.PreprocessHasFailed)
			{
				this.orderExecuted = false;
				this.ticket = null;
				aiBehaviorTree.ErrorCode = 1;
				return Amplitude.Unity.AI.BehaviourTree.State.Failure;
			}
			this.orderExecuted = false;
			this.ticket = null;
			return Amplitude.Unity.AI.BehaviourTree.State.Success;
		}
		else
		{
			Army army;
			if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) != AIArmyMission.AIArmyMissionErrorCode.None)
			{
				return Amplitude.Unity.AI.BehaviourTree.State.Failure;
			}
			if (!aiBehaviorTree.Variables.ContainsKey(this.TargetVarName))
			{
				aiBehaviorTree.LogError("${0} not set", new object[]
				{
					this.TargetVarName
				});
				return Amplitude.Unity.AI.BehaviourTree.State.Failure;
			}
			IGameEntity gameEntity = aiBehaviorTree.Variables[this.TargetVarName] as IGameEntity;
			if (!(gameEntity is TerraformDevice))
			{
				aiBehaviorTree.LogError("${0} is not a device", new object[]
				{
					this.TargetVarName
				});
				return Amplitude.Unity.AI.BehaviourTree.State.Failure;
			}
			TerraformDevice terraformDevice = gameEntity as TerraformDevice;
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			service.Game.Services.GetService<IGameEntityRepositoryService>();
			AIBehaviorTreeNode_Action_ToggleDismantleCreepingNode.StateType state = this.State;
			if (state != AIBehaviorTreeNode_Action_ToggleDismantleCreepingNode.StateType.Start)
			{
				if (state == AIBehaviorTreeNode_Action_ToggleDismantleCreepingNode.StateType.Stop && (!terraformDevice.DismantlingArmyGUID.IsValid || army.GUID != terraformDevice.DismantlingArmyGUID))
				{
					return Amplitude.Unity.AI.BehaviourTree.State.Failure;
				}
			}
			else if (terraformDevice.DismantlingArmyGUID.IsValid)
			{
				return Amplitude.Unity.AI.BehaviourTree.State.Failure;
			}
			this.orderExecuted = false;
			OrderToggleDismantleDevice order = new OrderToggleDismantleDevice(army.Empire.Index, army.GUID, terraformDevice.GUID, this.State == AIBehaviorTreeNode_Action_ToggleDismantleCreepingNode.StateType.Start);
			aiBehaviorTree.AICommander.Empire.PlayerControllers.AI.PostOrder(order, out this.ticket, new EventHandler<TicketRaisedEventArgs>(this.Order_TicketRaised));
			return Amplitude.Unity.AI.BehaviourTree.State.Running;
		}
	}

	private void Order_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		this.orderExecuted = true;
	}

	private bool orderExecuted;

	private Ticket ticket;
}
