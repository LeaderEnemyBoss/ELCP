using System;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;

public class AIBehaviorTreeNode_Action_Terraform : AIBehaviorTreeNode_Action
{
	public AIBehaviorTreeNode_Action_Terraform()
	{
		this.DeviceDefinitionName = string.Empty;
	}

	[XmlAttribute]
	public string DeviceDefinitionName { get; set; }

	public override void Release()
	{
		base.Release();
		this.ticket = null;
		this.aiBehaviorTree = null;
	}

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		this.aiBehaviorTree = aiBehaviorTree;
		if (this.ticket != null)
		{
			return State.Running;
		}
		Army army;
		AIArmyMission.AIArmyMissionErrorCode armyUnlessLocked = base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army);
		if (armyUnlessLocked != AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		if (string.IsNullOrEmpty(this.DeviceDefinitionName))
		{
			return State.Failure;
		}
		string text = aiBehaviorTree.Variables[this.DeviceDefinitionName] as string;
		if (string.IsNullOrEmpty(text))
		{
			return State.Failure;
		}
		OrderBuyoutAndPlaceTerraformationDevice order = new OrderBuyoutAndPlaceTerraformationDevice(army.Empire.Index, army.GUID, "ArmyActionTerraform", text);
		aiBehaviorTree.AICommander.Empire.PlayerControllers.AI.PostOrder(order, out this.ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderTerraform_TicketRaised));
		return State.Running;
	}

	private void OrderTerraform_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		AICommander_Terraformation commanderObjective = this.aiBehaviorTree.AICommander as AICommander_Terraformation;
		if (commanderObjective != null)
		{
			EvaluableMessage_Terraform evaluableMessage_Terraform = commanderObjective.AIPlayer.Blackboard.FindFirst<EvaluableMessage_Terraform>(BlackboardLayerID.Empire, (EvaluableMessage_Terraform match) => match.RegionIndex == commanderObjective.RegionIndex && match.TerraformPosition == commanderObjective.TerraformPosition);
			if (evaluableMessage_Terraform != null)
			{
				if (this.ticket.PostOrderResponse != PostOrderResponse.Processed)
				{
					evaluableMessage_Terraform.SetFailedToObtain();
					this.aiBehaviorTree.ErrorCode = 37;
				}
				else
				{
					evaluableMessage_Terraform.SetObtained();
				}
			}
		}
		this.ticket = null;
	}

	private AIBehaviorTree aiBehaviorTree;

	private Ticket ticket;
}
