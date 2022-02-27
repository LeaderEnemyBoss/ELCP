using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Action_Bribe : AIBehaviorTreeNode_Action
{
	public AIBehaviorTreeNode_Action_Bribe()
	{
		this.TargetVarName = string.Empty;
	}

	[XmlAttribute]
	public string TargetVarName { get; set; }

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
		if (!aiBehaviorTree.Variables.ContainsKey(this.TargetVarName))
		{
			aiBehaviorTree.LogError("${0} not set", new object[]
			{
				this.TargetVarName
			});
			return State.Failure;
		}
		IGameEntity target = aiBehaviorTree.Variables[this.TargetVarName] as IGameEntity;
		if (!(target is IWorldPositionable))
		{
			return State.Failure;
		}
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
		if (!service2.Contains(target.GUID))
		{
			return State.Success;
		}
		AICommanderWithObjective commanderObjective = aiBehaviorTree.AICommander as AICommanderWithObjective;
		if (commanderObjective == null)
		{
			return State.Failure;
		}
		EvaluableMessage_VillageAction evaluableMessage_VillageAction = aiBehaviorTree.AICommander.AIPlayer.Blackboard.FindFirst<EvaluableMessage_VillageAction>(BlackboardLayerID.Empire, (EvaluableMessage_VillageAction match) => match.RegionIndex == commanderObjective.RegionIndex && match.VillageGUID == target.GUID && match.AccountTag == AILayer_AccountManager.MilitaryAccountName);
		if (evaluableMessage_VillageAction == null || evaluableMessage_VillageAction.EvaluationState == EvaluableMessage.EvaluableMessageState.Cancel)
		{
			this.aiBehaviorTree.ErrorCode = 31;
			return State.Failure;
		}
		if (evaluableMessage_VillageAction.EvaluationState == EvaluableMessage.EvaluableMessageState.Obtained)
		{
			return State.Success;
		}
		if (evaluableMessage_VillageAction.ChosenBuyEvaluation == null || evaluableMessage_VillageAction.EvaluationState == EvaluableMessage.EvaluableMessageState.Cancel)
		{
			return State.Failure;
		}
		IEncounterRepositoryService service3 = service.Game.Services.GetService<IEncounterRepositoryService>();
		if (service3 != null)
		{
			IEnumerable<Encounter> enumerable = service3;
			if (enumerable != null)
			{
				bool flag = enumerable.Any((Encounter encounter) => encounter.IsGarrisonInEncounter(army.GUID, false) || encounter.IsGarrisonInEncounter(target.GUID, false));
				if (flag)
				{
					return State.Running;
				}
			}
		}
		Village village = target as Village;
		if (village == null)
		{
			aiBehaviorTree.ErrorCode = 2;
			return State.Failure;
		}
		Diagnostics.Assert(AIScheduler.Services != null);
		IWorldPositionningService service4 = service.Game.Services.GetService<IWorldPositionningService>();
		if (service4.GetDistance(army.WorldPosition, (target as IWorldPositionable).WorldPosition) != 1)
		{
			aiBehaviorTree.ErrorCode = 12;
			return State.Failure;
		}
		if (village.HasBeenPacified || village.HasBeenConverted || village.HasBeenInfected)
		{
			return State.Failure;
		}
		OrderBribeVillage order = new OrderBribeVillage(army.Empire.Index, army.GUID, (target as IWorldPositionable).WorldPosition, ArmyAction_Bribe.ReadOnlyName);
		aiBehaviorTree.AICommander.Empire.PlayerControllers.AI.PostOrder(order, out this.ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderBribeVillage_TicketRaised));
		return State.Running;
	}

	private void OrderBribeVillage_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		AICommanderWithObjective commanderObjective = this.aiBehaviorTree.AICommander as AICommanderWithObjective;
		if (commanderObjective != null)
		{
			EvaluableMessage_VillageAction evaluableMessage_VillageAction = this.aiBehaviorTree.AICommander.AIPlayer.Blackboard.FindFirst<EvaluableMessage_VillageAction>(BlackboardLayerID.Empire, (EvaluableMessage_VillageAction match) => match.RegionIndex == commanderObjective.RegionIndex && match.VillageGUID == commanderObjective.SubObjectiveGuid && match.AccountTag == AILayer_AccountManager.MilitaryAccountName);
			if (evaluableMessage_VillageAction == null)
			{
				if (this.ticket.PostOrderResponse != PostOrderResponse.Processed)
				{
					evaluableMessage_VillageAction.SetFailedToObtain();
					this.aiBehaviorTree.ErrorCode = 31;
				}
				else
				{
					evaluableMessage_VillageAction.SetObtained();
				}
			}
		}
		this.ticket = null;
	}

	private AIBehaviorTree aiBehaviorTree;

	private Ticket ticket;
}
