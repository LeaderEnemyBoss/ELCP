using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

[ArmyActionWorldCursor(typeof(ArmyActionTargetSelectionWorldCursor))]
public class ArmyAction_ToggleDismantleCreepingNode : ArmyAction, IArmyActionWithTargetSelection, IArmyActionWithToggle
{
	public bool IsToggled(Army army)
	{
		return army != null && army.IsDismantlingCreepingNode;
	}

	[XmlElement]
	public StaticString ToggledOffDescriptionOverride { get; set; }

	[XmlElement]
	public StaticString ToggledOnDescriptionOverride { get; set; }

	public override bool CanExecute(Army army, ref List<StaticString> failureFlags, params object[] parameters)
	{
		if (!base.CanExecute(army, ref failureFlags, parameters))
		{
			return false;
		}
		if (army.IsNaval || army.IsPillaging)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		if (army.IsAspirating)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileAspirating);
			return false;
		}
		if (army.IsDismantlingDevice)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileDismantlingDevice);
			return false;
		}
		if (army.IsEarthquaker)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileEarthquaking);
			return false;
		}
		if (parameters != null && parameters.Length > 0)
		{
			CreepingNode creepingNode;
			for (int i = 0; i < parameters.Length; i++)
			{
				if (parameters[i] is CreepingNode)
				{
					creepingNode = (parameters[i] as CreepingNode);
					if (creepingNode != null && this.CanToggleOverCreepingNode(army, creepingNode, ref failureFlags))
					{
						if (army.IsInEncounter)
						{
							failureFlags.Add(ArmyAction.NoCanDoWhileLockedInBattle);
							return false;
						}
						return true;
					}
				}
				else if (parameters[i] is List<IGameEntity>)
				{
					List<IGameEntity> list = parameters[i] as List<IGameEntity>;
					for (int j = 0; j < list.Count; j++)
					{
						if (list[j] is CreepingNode)
						{
							creepingNode = (list[j] as CreepingNode);
							if (creepingNode != null && this.CanToggleOverCreepingNode(army, creepingNode, ref failureFlags))
							{
								if (army.IsInEncounter)
								{
									failureFlags.Add(ArmyAction.NoCanDoWhileLockedInBattle);
									return false;
								}
								return true;
							}
						}
					}
				}
			}
			PointOfInterest pointOfInterest = null;
			if (parameters[0] is Village)
			{
				pointOfInterest = (parameters[0] as Village).PointOfInterest;
			}
			else if (parameters[0] is PointOfInterest)
			{
				pointOfInterest = (parameters[0] as PointOfInterest);
			}
			creepingNode = this.GetCreepingNodeFromPOI(pointOfInterest);
			if (creepingNode != null && this.CanToggleOverCreepingNode(army, creepingNode, ref failureFlags))
			{
				if (army.IsInEncounter)
				{
					failureFlags.Add(ArmyAction.NoCanDoWhileLockedInBattle);
					return false;
				}
				return true;
			}
		}
		failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
		return false;
	}

	public override void Execute(Army army, global::PlayerController playerController, out Ticket ticket, EventHandler<TicketRaisedEventArgs> ticketRaisedEventHandler, params object[] parameters)
	{
		ticket = null;
		if (army == null || playerController == null)
		{
			return;
		}
		CreepingNode creepingNode = null;
		if (parameters != null && parameters.Length > 0)
		{
			if (parameters[0] is CreepingNode)
			{
				creepingNode = (parameters[0] as CreepingNode);
			}
			if (parameters[0] is Village)
			{
				PointOfInterest pointOfInterest = (parameters[0] as Village).PointOfInterest;
				creepingNode = this.GetCreepingNodeFromPOI(pointOfInterest);
			}
			else if (parameters[0] is PointOfInterest)
			{
				PointOfInterest pointOfInterest = parameters[0] as PointOfInterest;
				creepingNode = this.GetCreepingNodeFromPOI(pointOfInterest);
			}
		}
		List<StaticString> list = new List<StaticString>();
		if (creepingNode == null || !this.CanToggleOverCreepingNode(army, creepingNode, ref list))
		{
			return;
		}
		if (creepingNode.DismantlingArmy == null)
		{
			if (army.IsDismantlingCreepingNode)
			{
				OrderToggleDismantleCreepingNode order = new OrderToggleDismantleCreepingNode(army.Empire.Index, army.GUID, army.DismantlingCreepingNodeTarget, false);
				playerController.PostOrder(order, out ticket, ticketRaisedEventHandler);
			}
			OrderToggleDismantleCreepingNode order2 = new OrderToggleDismantleCreepingNode(army.Empire.Index, army.GUID, creepingNode.GUID, true);
			playerController.PostOrder(order2, out ticket, ticketRaisedEventHandler);
		}
		else if (creepingNode.DismantlingArmy == army)
		{
			OrderToggleDismantleCreepingNode order3 = new OrderToggleDismantleCreepingNode(army.Empire.Index, army.GUID, creepingNode.GUID, false);
			playerController.PostOrder(order3, out ticket, ticketRaisedEventHandler);
		}
	}

	public void FillTargets(Army army, List<IGameEntity> targets, ref List<StaticString> failureFlags)
	{
		this.ListNearbyCreepingNodesFiltered(army);
		for (int i = 0; i < this.creepingNodes.Count; i++)
		{
			targets.Add(this.creepingNodes[i]);
		}
	}

	private bool CanToggleOverCreepingNode(Army army, CreepingNode creepingNode, ref List<StaticString> failureFlags)
	{
		if (army == null || creepingNode == null || creepingNode.Empire == army.Empire)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		if (creepingNode.DismantlingArmy != null)
		{
			if (creepingNode.DismantlingArmy == army)
			{
				return true;
			}
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		else
		{
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
			Diagnostics.Assert(service2 != null);
			Army armyAtPosition = service2.GetArmyAtPosition(creepingNode.WorldPosition);
			if (creepingNode != null && creepingNode.DismantlingArmy == null && armyAtPosition != null && creepingNode.Empire.Index != army.Empire.Index)
			{
				failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
				return false;
			}
			if (creepingNode.Empire != army.Empire && army.Empire is MajorEmpire)
			{
				DepartmentOfForeignAffairs agency = army.Empire.GetAgency<DepartmentOfForeignAffairs>();
				DiplomaticRelation diplomaticRelation = agency.DiplomaticRelations[creepingNode.Empire.Index];
				if (diplomaticRelation != null && diplomaticRelation.State != null && (diplomaticRelation.State.Name == DiplomaticRelationState.Names.Alliance || diplomaticRelation.State.Name == DiplomaticRelationState.Names.Peace || diplomaticRelation.State.Name == DiplomaticRelationState.Names.Truce))
				{
					failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
					return false;
				}
			}
			return true;
		}
	}

	private void ListNearbyCreepingNodesFiltered(Army army)
	{
		if (army == null)
		{
			throw new ArgumentNullException("army");
		}
		this.creepingNodes.Clear();
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		global::Game game = service.Game as global::Game;
		Diagnostics.Assert(game != null);
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(service2 != null);
		IGameEntityRepositoryService service3 = service.Game.Services.GetService<IGameEntityRepositoryService>();
		Diagnostics.Assert(service3 != null);
		List<StaticString> list = new List<StaticString>();
		List<WorldPosition> neighbours = army.WorldPosition.GetNeighbours(game.World.WorldParameters);
		for (int i = 0; i < neighbours.Count; i++)
		{
			PointOfInterest pointOfInterest = service2.GetPointOfInterest(neighbours[i]);
			if (pointOfInterest != null && pointOfInterest.CreepingNodeGUID != GameEntityGUID.Zero)
			{
				CreepingNode creepingNode = null;
				if (service3.TryGetValue<CreepingNode>(pointOfInterest.CreepingNodeGUID, out creepingNode) && this.CanToggleOverCreepingNode(army, creepingNode, ref list) && !this.creepingNodes.Contains(creepingNode))
				{
					this.creepingNodes.Add(creepingNode);
				}
			}
		}
	}

	private CreepingNode GetCreepingNodeFromPOI(PointOfInterest pointOfInterest)
	{
		CreepingNode result = null;
		if (pointOfInterest != null && pointOfInterest.CreepingNodeGUID != GameEntityGUID.Zero)
		{
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
			Diagnostics.Assert(service2 != null);
			service2.TryGetValue<CreepingNode>(pointOfInterest.CreepingNodeGUID, out result);
			return result;
		}
		return result;
	}

	public static readonly StaticString ReadOnlyName = new StaticString("ArmyActionToggleDismantleCreepingNode");

	private List<CreepingNode> creepingNodes = new List<CreepingNode>();
}
