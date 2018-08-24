using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Action_SearchInRuin : AIBehaviorTreeNode_Action
{
	public AIBehaviorTreeNode_Action_SearchInRuin()
	{
		this.TargetVarName = string.Empty;
	}

	[XmlAttribute]
	public string TargetVarName { get; set; }

	public override void Release()
	{
		base.Release();
		this.orderTicket = null;
	}

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army2;
		base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army2);
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
		State result;
		if (this.orderTicket != null)
		{
			if (!this.orderTicket.Raised)
			{
				result = State.Running;
			}
			else
			{
				OrderInteractWith orderInteractWith = this.orderTicket.Order as OrderInteractWith;
				IGameEntity gameEntity = null;
				if (this.orderTicket.PostOrderResponse != PostOrderResponse.Processed)
				{
					if (service2.TryGetValue(orderInteractWith.TargetGUID, out gameEntity))
					{
						PointOfInterest pointOfInterest;
						if (gameEntity is Village)
						{
							pointOfInterest = (gameEntity as Village).PointOfInterest;
						}
						else
						{
							pointOfInterest = (gameEntity as PointOfInterest);
						}
						if (pointOfInterest != null)
						{
							pointOfInterest.Interaction.Bits |= 1 << orderInteractWith.EmpireIndex;
						}
					}
					aiBehaviorTree.ErrorCode = 30;
					this.orderTicket = null;
					result = State.Failure;
				}
				else
				{
					if (this.orderTicket.PostOrderResponse == PostOrderResponse.Processed && service2.TryGetValue(orderInteractWith.TargetGUID, out gameEntity) && gameEntity is PointOfInterest && orderInteractWith.Tags.Contains("Talk") && orderInteractWith.QuestRewards == null)
					{
						PointOfInterest pointOfInterest2 = gameEntity as PointOfInterest;
						Diagnostics.Log("ELCP: Empire {0} AIBehaviorTreeNode_Action_SearchInRuin parley order without quest reward: {1} ", new object[]
						{
							aiBehaviorTree.AICommander.Empire.ToString(),
							pointOfInterest2.WorldPosition
						});
						if (pointOfInterest2 != null)
						{
							pointOfInterest2.Interaction.Bits |= 1 << orderInteractWith.EmpireIndex;
						}
					}
					this.orderTicket = null;
					result = State.Success;
				}
			}
		}
		else
		{
			Army army;
			if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) > AIArmyMission.AIArmyMissionErrorCode.None)
			{
				result = State.Failure;
			}
			else
			{
				IGameEntity gameEntity2 = aiBehaviorTree.Variables[this.TargetVarName] as IGameEntity;
				if (gameEntity2 is Village)
				{
					Village village = gameEntity2 as Village;
					PointOfInterest pointOfInterest3 = village.PointOfInterest;
					if (pointOfInterest3 == null)
					{
						return State.Failure;
					}
					if (village.HasBeenConverted || village.HasBeenPacified || village.IsInEncounter || village.PointOfInterest.PointOfInterestImprovement == null)
					{
						return State.Failure;
					}
					if (!aiBehaviorTree.AICommander.Empire.GetAgency<DepartmentOfScience>().CanParley())
					{
						return State.Failure;
					}
					if (pointOfInterest3.SimulationObject.Tags.Contains(Village.DissentedVillage))
					{
						return State.Failure;
					}
					if ((pointOfInterest3.Interaction.Bits & army.Empire.Bits) != 0)
					{
						return State.Failure;
					}
					QuestMarker questMarker;
					if (service.Game.Services.GetService<IQuestManagementService>().TryGetMarkerByGUID(pointOfInterest3.GUID, out questMarker))
					{
						Diagnostics.Log("ELCP: Empire {0} AIBehaviorTreeNode_Action_SearchInRuin Questmarker active", new object[]
						{
							aiBehaviorTree.AICommander.Empire.ToString()
						});
						return State.Failure;
					}
				}
				if (!aiBehaviorTree.Variables.ContainsKey(this.TargetVarName))
				{
					aiBehaviorTree.LogError("${0} not set", new object[]
					{
						this.TargetVarName
					});
					result = State.Failure;
				}
				else
				{
					IGameEntity gameEntity3 = aiBehaviorTree.Variables[this.TargetVarName] as IGameEntity;
					if (!(gameEntity3 is IWorldPositionable) || (!(gameEntity3 is PointOfInterest) && !(gameEntity3 is Village)))
					{
						aiBehaviorTree.ErrorCode = 10;
						result = State.Failure;
					}
					else
					{
						Diagnostics.Assert(AIScheduler.Services != null);
						if (service.Game.Services.GetService<IWorldPositionningService>().GetDistance(army.WorldPosition, (gameEntity3 as IWorldPositionable).WorldPosition) != 1)
						{
							aiBehaviorTree.ErrorCode = 12;
							result = State.Failure;
						}
						else
						{
							IEncounterRepositoryService service3 = service.Game.Services.GetService<IEncounterRepositoryService>();
							if (service3 != null)
							{
								IEnumerable<Encounter> enumerable = service3;
								if (enumerable != null && enumerable.Any((Encounter encounter) => encounter.IsGarrisonInEncounter(army.GUID, false)))
								{
									return State.Running;
								}
							}
							if (gameEntity3 is Village)
							{
								PointOfInterest pointOfInterest4 = (gameEntity3 as Village).PointOfInterest;
								OrderInteractWith orderInteractWith2 = new OrderInteractWith(army.Empire.Index, army.GUID, "ArmyActionParley");
								orderInteractWith2.WorldPosition = army.WorldPosition;
								orderInteractWith2.Tags.AddTag("Talk");
								orderInteractWith2.TargetGUID = pointOfInterest4.GUID;
								orderInteractWith2.ArmyActionName = "ArmyActionParley";
								orderInteractWith2.NumberOfActionPointsToSpend = 0f;
								aiBehaviorTree.AICommander.Empire.PlayerControllers.AI.PostOrder(orderInteractWith2, out this.orderTicket, null);
							}
							else
							{
								OrderInteractWith orderInteractWith3 = new OrderInteractWith(army.Empire.Index, army.GUID, "ArmyActionSearch");
								orderInteractWith3.WorldPosition = army.WorldPosition;
								orderInteractWith3.Tags.AddTag("Interact");
								orderInteractWith3.TargetGUID = gameEntity3.GUID;
								aiBehaviorTree.AICommander.Empire.PlayerControllers.AI.PostOrder(orderInteractWith3, out this.orderTicket, null);
							}
							result = State.Running;
						}
					}
				}
			}
		}
		return result;
	}

	private Ticket orderTicket;
}
