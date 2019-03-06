using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Action_ConvertVillage : AIBehaviorTreeNode_Action
{
	public AIBehaviorTreeNode_Action_ConvertVillage()
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
		State result;
		if (this.ticket != null)
		{
			result = State.Running;
		}
		else
		{
			Army army;
			if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) > AIArmyMission.AIArmyMissionErrorCode.None)
			{
				result = State.Failure;
			}
			else if (!aiBehaviorTree.Variables.ContainsKey(this.TargetVarName))
			{
				aiBehaviorTree.LogError("${0} not set", new object[]
				{
					this.TargetVarName
				});
				result = State.Failure;
			}
			else
			{
				IGameEntity target = aiBehaviorTree.Variables[this.TargetVarName] as IGameEntity;
				if (!(target is IWorldPositionable))
				{
					result = State.Failure;
				}
				else
				{
					IGameService service = Services.GetService<IGameService>();
					Diagnostics.Assert(service != null);
					if (!service.Game.Services.GetService<IGameEntityRepositoryService>().Contains(target.GUID))
					{
						result = State.Success;
					}
					else
					{
						AICommanderWithObjective commanderObjective = aiBehaviorTree.AICommander as AICommanderWithObjective;
						if (commanderObjective == null)
						{
							result = State.Failure;
						}
						else if (!(target is Village))
						{
							aiBehaviorTree.ErrorCode = 2;
							result = State.Failure;
						}
						else
						{
							EvaluableMessage_VillageAction evaluableMessage_VillageAction = aiBehaviorTree.AICommander.AIPlayer.Blackboard.FindFirst<EvaluableMessage_VillageAction>(BlackboardLayerID.Empire, (EvaluableMessage_VillageAction match) => match.RegionIndex == commanderObjective.RegionIndex && match.VillageGUID == target.GUID && match.AccountTag == AILayer_AccountManager.ConversionAccountName);
							if (evaluableMessage_VillageAction == null)
							{
								float num;
								army.Empire.GetAgency<DepartmentOfTheTreasury>().TryGetResourceStockValue(army.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, out num, false);
								if (AILayer_Village.GetVillageConversionCost(army.Empire as MajorEmpire, target as Village) > num || ((target as Village).HasBeenConverted && (target as Village).Converter == aiBehaviorTree.AICommander.Empire as MajorEmpire))
								{
									this.aiBehaviorTree.ErrorCode = 32;
									return State.Failure;
								}
							}
							else
							{
								if (evaluableMessage_VillageAction.EvaluationState == EvaluableMessage.EvaluableMessageState.Cancel)
								{
									this.aiBehaviorTree.ErrorCode = 32;
									return State.Failure;
								}
								if (evaluableMessage_VillageAction.EvaluationState == EvaluableMessage.EvaluableMessageState.Obtained)
								{
									return State.Success;
								}
								if (evaluableMessage_VillageAction.ChosenBuyEvaluation == null || evaluableMessage_VillageAction.ChosenBuyEvaluation.State != BuyEvaluation.EvaluationState.Purchased || evaluableMessage_VillageAction.EvaluationState != EvaluableMessage.EvaluableMessageState.Validate)
								{
									return State.Failure;
								}
							}
							IEncounterRepositoryService service2 = service.Game.Services.GetService<IEncounterRepositoryService>();
							if (service2 != null)
							{
								IEnumerable<Encounter> enumerable = service2;
								if (enumerable != null && enumerable.Any((Encounter encounter) => encounter.IsGarrisonInEncounter(army.GUID, false) || encounter.IsGarrisonInEncounter(target.GUID, false)))
								{
									return State.Running;
								}
							}
							Diagnostics.Assert(AIScheduler.Services != null);
							if (service.Game.Services.GetService<IWorldPositionningService>().GetDistance(army.WorldPosition, (target as IWorldPositionable).WorldPosition) != 1)
							{
								aiBehaviorTree.ErrorCode = 12;
								result = State.Failure;
							}
							else
							{
								OrderConvertVillage order = new OrderConvertVillage(army.Empire.Index, army.GUID, (target as IWorldPositionable).WorldPosition);
								aiBehaviorTree.AICommander.Empire.PlayerControllers.AI.PostOrder(order, out this.ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderConvertVillage_TicketRaised));
								result = State.Running;
							}
						}
					}
				}
			}
		}
		return result;
	}

	private void OrderConvertVillage_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		AICommanderWithObjective commanderObjective = this.aiBehaviorTree.AICommander as AICommanderWithObjective;
		if (commanderObjective != null)
		{
			EvaluableMessage_VillageAction evaluableMessage_VillageAction = commanderObjective.AIPlayer.Blackboard.FindFirst<EvaluableMessage_VillageAction>(BlackboardLayerID.Empire, (EvaluableMessage_VillageAction match) => match.RegionIndex == commanderObjective.RegionIndex && match.VillageGUID == commanderObjective.SubObjectiveGuid && match.AccountTag == AILayer_AccountManager.ConversionAccountName);
			if (evaluableMessage_VillageAction != null)
			{
				if (this.ticket.PostOrderResponse != PostOrderResponse.Processed)
				{
					evaluableMessage_VillageAction.SetFailedToObtain();
					this.aiBehaviorTree.ErrorCode = 32;
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
