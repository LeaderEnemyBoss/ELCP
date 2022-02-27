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
							IQuestRepositoryService service3 = service.Game.Services.GetService<IQuestRepositoryService>();
							foreach (QuestMarker questMarker in service.Game.Services.GetService<IQuestManagementService>().GetMarkersByBoundTargetGUID(pointOfInterest.GUID))
							{
								Quest quest;
								if (service3.TryGetValue(questMarker.QuestGUID, out quest) && quest.QuestDefinition.Name == "GlobalQuestCoop#0004" && quest.EmpireBits == army2.Empire.Bits && questMarker.IsVisibleFor(army2.Empire))
								{
									this.orderTicket = null;
									QuestBehaviour questBehaviour = service3.GetQuestBehaviour(quest.Name, army2.Empire.Index);
									if (questBehaviour != null)
									{
										if (quest.QuestDefinition.Variables.First((QuestVariableDefinition p) => p.VarName == "$NameOfStrategicResourceToGather1") != null)
										{
											QuestBehaviourTreeNode_ConditionCheck_HasResourceAmount questBehaviourTreeNode_ConditionCheck_HasResourceAmount;
											if (!ELCPUtilities.TryGetFirstNodeOfType<QuestBehaviourTreeNode_ConditionCheck_HasResourceAmount>(questBehaviour.Root as BehaviourTreeNodeController, out questBehaviourTreeNode_ConditionCheck_HasResourceAmount))
											{
												break;
											}
											string resourceName = questBehaviourTreeNode_ConditionCheck_HasResourceAmount.ResourceName;
											int wantedAmount = questBehaviourTreeNode_ConditionCheck_HasResourceAmount.WantedAmount;
											DepartmentOfTheTreasury agency = army2.Empire.GetAgency<DepartmentOfTheTreasury>();
											if (agency == null)
											{
												break;
											}
											float num;
											if (agency != null && agency.TryGetResourceStockValue(army2.Empire.SimulationObject, resourceName, out num, false) && num >= (float)wantedAmount)
											{
												return State.Running;
											}
											break;
										}
									}
								}
							}
							if ((pointOfInterest.Interaction.Bits & aiBehaviorTree.AICommander.Empire.Bits) != aiBehaviorTree.AICommander.Empire.Bits)
							{
								pointOfInterest.Interaction.Bits |= 1 << orderInteractWith.EmpireIndex;
							}
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
				if (!aiBehaviorTree.Variables.ContainsKey(this.TargetVarName))
				{
					aiBehaviorTree.LogError("${0} not set", new object[]
					{
						this.TargetVarName
					});
					return State.Failure;
				}
				IGameEntity gameEntity2 = aiBehaviorTree.Variables[this.TargetVarName] as IGameEntity;
				if (!(gameEntity2 is IWorldPositionable) || (!(gameEntity2 is PointOfInterest) && !(gameEntity2 is Village)))
				{
					aiBehaviorTree.ErrorCode = 10;
					return State.Failure;
				}
				if (gameEntity2 is Village)
				{
					Village village = gameEntity2 as Village;
					Diagnostics.Log("ELCP {0} {1} AIBehaviorTreeNode_Action_SearchInRuin village {2} {3}", new object[]
					{
						aiBehaviorTree.AICommander.Empire,
						army.LocalizedName,
						village.WorldPosition,
						this.QuestVillage
					});
					PointOfInterest pointOfInterest3 = village.PointOfInterest;
					if (pointOfInterest3 == null)
					{
						return State.Failure;
					}
					if (!this.QuestVillage)
					{
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
							Diagnostics.Log("fail");
							return State.Failure;
						}
						QuestMarker questMarker2;
						if (service.Game.Services.GetService<IQuestManagementService>().TryGetMarkerByGUID(pointOfInterest3.GUID, out questMarker2))
						{
							Diagnostics.Log("ELCP: Empire {0} AIBehaviorTreeNode_Action_SearchInRuin Questmarker active", new object[]
							{
								aiBehaviorTree.AICommander.Empire.ToString()
							});
							return State.Failure;
						}
					}
				}
				Diagnostics.Assert(AIScheduler.Services != null);
				if (service.Game.Services.GetService<IWorldPositionningService>().GetDistance(army.WorldPosition, (gameEntity2 as IWorldPositionable).WorldPosition) != 1)
				{
					aiBehaviorTree.ErrorCode = 12;
					result = State.Failure;
				}
				else
				{
					IEncounterRepositoryService service4 = service.Game.Services.GetService<IEncounterRepositoryService>();
					if (service4 != null)
					{
						IEnumerable<Encounter> enumerable = service4;
						if (enumerable != null && enumerable.Any((Encounter encounter) => encounter.IsGarrisonInEncounter(army.GUID, false)))
						{
							return State.Running;
						}
					}
					if (gameEntity2 is Village)
					{
						Diagnostics.Log("ELCP {0} {1} AIBehaviorTreeNode_Action_SearchInRuin2 village", new object[]
						{
							aiBehaviorTree.AICommander.Empire,
							army.LocalizedName
						});
						PointOfInterest pointOfInterest4 = (gameEntity2 as Village).PointOfInterest;
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
						if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
						{
							Diagnostics.Log("~~~~~ ELCP: {0}/{1} ArmyActionSearch at {2}, {3} {4} ~~~~~", new object[]
							{
								army.LocalizedName,
								army.Empire,
								(gameEntity2 as PointOfInterest).WorldPosition,
								((gameEntity2 as PointOfInterest).Interaction.Bits & army.Empire.Bits) == army.Empire.Bits,
								(gameEntity2 as PointOfInterest).UntappedDustDeposits
							});
						}
						OrderInteractWith orderInteractWith3 = new OrderInteractWith(army.Empire.Index, army.GUID, "ArmyActionSearch");
						orderInteractWith3.WorldPosition = army.WorldPosition;
						orderInteractWith3.Tags.AddTag("Interact");
						orderInteractWith3.TargetGUID = gameEntity2.GUID;
						aiBehaviorTree.AICommander.Empire.PlayerControllers.AI.PostOrder(orderInteractWith3, out this.orderTicket, null);
					}
					result = State.Running;
				}
			}
		}
		return result;
	}

	[XmlAttribute]
	public bool QuestVillage { get; set; }

	private Ticket orderTicket;
}
