using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class ArmyAction_DestroyRegionBuilding : ArmyActionWithCooldown, IArmyActionWithMovementEffect
{
	public ArmyAction_DestroyRegionBuilding()
	{
		this.PointsOfInterest = new List<PointOfInterest>();
	}

	[XmlElement]
	public bool ZeroMovement { get; set; }

	[XmlElement("PointOfInterestCategory")]
	public string[] XmlSerializablePointOfInterestCategories
	{
		get
		{
			if (this.PointOfInterestCategories == null)
			{
				return null;
			}
			string[] array = new string[this.PointOfInterestCategories.Length];
			for (int i = 0; i < this.PointOfInterestCategories.Length; i++)
			{
				array[i] = this.PointOfInterestCategories[i].ToString();
			}
			return array;
		}
		set
		{
			this.PointOfInterestCategories = null;
			if (value != null)
			{
				this.PointOfInterestCategories = new StaticString[value.Length];
				for (int i = 0; i < value.Length; i++)
				{
					this.PointOfInterestCategories[i] = value[i];
				}
			}
		}
	}

	[XmlIgnore]
	public StaticString[] PointOfInterestCategories { get; protected set; }

	protected List<PointOfInterest> PointsOfInterest { get; set; }

	public override bool CanExecute(Army army, ref List<StaticString> failureFlags, params object[] parameters)
	{
		if (!base.CanExecute(army, ref failureFlags, parameters))
		{
			return false;
		}
		if (army.Empire == null)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		if (army.IsNaval)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		bool flag = false;
		this.PointsOfInterest.Clear();
		for (int i = 0; i < parameters.Length; i++)
		{
			if (parameters[i] is IGameEntity)
			{
				if (parameters[i] is PointOfInterest)
				{
					this.PointsOfInterest.Add(parameters[i] as PointOfInterest);
				}
				flag = true;
			}
			else if (parameters[i] is List<IGameEntity>)
			{
				List<IGameEntity> list = parameters[i] as List<IGameEntity>;
				if (list.Count > 0)
				{
					for (int j = 0; j < list.Count; j++)
					{
						PointOfInterest pointOfInterest = list[j] as PointOfInterest;
						if (pointOfInterest != null)
						{
							this.PointsOfInterest.Add(list[j] as PointOfInterest);
						}
					}
				}
			}
		}
		if (!(this is IArmyActionWithTargetSelection))
		{
			if (this.PointsOfInterest.Count == 0)
			{
				this.ListNearbyPointsOfInterest(army);
			}
		}
		if (!this.CheckCooldownPrerequisites(army))
		{
			failureFlags.Add(ArmyActionWithCooldown.NoCanDoWhileCooldownInProgress);
			return false;
		}
		this.FilterPointsOfInterest(army, ref failureFlags);
		if (this.PointsOfInterest.Count > 0)
		{
			if (army.IsInEncounter)
			{
				failureFlags.Add(ArmyAction.NoCanDoWhileLockedInBattle);
				return false;
			}
			if (!base.CheckActionPointsPrerequisites(army))
			{
				failureFlags.Add(ArmyAction.NoCanDoWhileNotEnoughActionPointsLeft);
				return false;
			}
			return failureFlags.Count == 0;
		}
		else
		{
			if (flag)
			{
				failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
				return false;
			}
			return false;
		}
	}

	public override void Execute(Army army, global::PlayerController playerController, out Ticket ticket, EventHandler<TicketRaisedEventArgs> ticketRaisedEventHandler, params object[] parameters)
	{
		ticket = null;
		ArmyAction.FailureFlags.Clear();
		if (this is IArmyActionWithTargetSelection)
		{
			PointOfInterest pointOfInterest = null;
			if (parameters != null && parameters.Length != 0 && parameters[0] is PointOfInterest)
			{
				pointOfInterest = (parameters[0] as PointOfInterest);
			}
			if (pointOfInterest != null)
			{
				OrderDestroyPointOfInterestImprovement orderDestroyPointOfInterestImprovement = new OrderDestroyPointOfInterestImprovement(army.Empire.Index, pointOfInterest.GUID);
				orderDestroyPointOfInterestImprovement.ArmyGUID = army.GUID;
				orderDestroyPointOfInterestImprovement.ArmyActionName = this.Name;
				orderDestroyPointOfInterestImprovement.NumberOfActionPointsToSpend = base.GetCostInActionPoints();
				orderDestroyPointOfInterestImprovement.ArmyActionCooldownDuration = base.ComputeCooldownDuration(army);
				Diagnostics.Assert(playerController != null);
				playerController.PostOrder(orderDestroyPointOfInterestImprovement, out ticket, ticketRaisedEventHandler);
				return;
			}
		}
		else
		{
			this.ListNearbyPointsOfInterest(army);
			this.FilterPointsOfInterest(army, ref ArmyAction.FailureFlags);
			if (ArmyAction.FailureFlags.Count == 0 && this.PointsOfInterest.Count > 0)
			{
				GameEntityGUID[] array = (from enumerator in this.PointsOfInterest
				where enumerator.PointOfInterestImprovement != null && enumerator.CreepingNodeGUID == GameEntityGUID.Zero
				select enumerator.GUID).ToArray<GameEntityGUID>();
				GameEntityGUID[] array2 = (from enumerator in this.PointsOfInterest
				where enumerator.CreepingNodeGUID != GameEntityGUID.Zero
				select enumerator.CreepingNodeGUID).ToArray<GameEntityGUID>();
				for (int i = 0; i < array2.Length; i++)
				{
					StaticString armyActionName = new StaticString();
					if (i == 0 && array.Length == 0)
					{
						armyActionName = this.Name;
					}
					OrderDismantleCreepingNodeSucceed order = new OrderDismantleCreepingNodeSucceed(army.Empire.Index, army.GUID, array2[i], armyActionName);
					playerController.PostOrder(order);
				}
				if (array.Length != 0)
				{
					OrderDestroyPointOfInterestImprovement orderDestroyPointOfInterestImprovement2 = new OrderDestroyPointOfInterestImprovement(army.Empire.Index, array);
					orderDestroyPointOfInterestImprovement2.ArmyGUID = army.GUID;
					orderDestroyPointOfInterestImprovement2.ArmyActionName = this.Name;
					orderDestroyPointOfInterestImprovement2.NumberOfActionPointsToSpend = base.GetCostInActionPoints();
					orderDestroyPointOfInterestImprovement2.ArmyActionCooldownDuration = base.ComputeCooldownDuration(army);
					Diagnostics.Assert(playerController != null);
					playerController.PostOrder(orderDestroyPointOfInterestImprovement2, out ticket, ticketRaisedEventHandler);
				}
			}
		}
	}

	public void FillTargets(Army army, List<IGameEntity> targets, ref List<StaticString> failureFlags)
	{
		this.ListNearbyPointsOfInterest(army);
		if (this.PointsOfInterest.Count == 0)
		{
			return;
		}
		this.FilterPointsOfInterest(army, ref failureFlags);
		for (int i = 0; i < this.PointsOfInterest.Count; i++)
		{
			targets.Add(this.PointsOfInterest[i]);
		}
	}

	public int ListNearbyPointsOfInterest(Army army)
	{
		if (army == null)
		{
			throw new ArgumentNullException("army");
		}
		this.PointsOfInterest.Clear();
		IGameService service = Services.GetService<IGameService>();
		if (service == null)
		{
			return 0;
		}
		global::Game game = service.Game as global::Game;
		if (game == null)
		{
			return 0;
		}
		IPathfindingService service2 = service.Game.Services.GetService<IPathfindingService>();
		IWorldPositionningService service3 = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(service3 != null);
		PointOfInterest pointOfInterest = service3.GetPointOfInterest(army.WorldPosition);
		if (pointOfInterest != null)
		{
			this.AddPOIifPossible(pointOfInterest, service2, army);
		}
		List<WorldPosition> neighbours = army.WorldPosition.GetNeighbours(game.World.WorldParameters);
		for (int i = 0; i < neighbours.Count; i++)
		{
			PointOfInterest pointOfInterest2 = service3.GetPointOfInterest(neighbours[i]);
			if (pointOfInterest2 != null)
			{
				this.AddPOIifPossible(pointOfInterest2, service2, army);
			}
		}
		return this.PointsOfInterest.Count;
	}

	private void FilterPointsOfInterest(Army army, ref List<StaticString> failureFlags)
	{
		for (int i = this.PointsOfInterest.Count - 1; i >= 0; i--)
		{
			if (this.PointsOfInterest[i].PointOfInterestImprovement == null && (!ELCPUtilities.UseELCPSymbiosisBuffs || this.PointsOfInterest[i].CreepingNodeGUID == GameEntityGUID.Zero))
			{
				this.PointsOfInterest.RemoveAt(i);
			}
			else if (this.PointsOfInterest[i].Empire != null && this.PointsOfInterest[i].Empire.Index == army.Empire.Index)
			{
				this.PointsOfInterest.RemoveAt(i);
			}
		}
	}

	private void AddPOIifPossible(PointOfInterest pointOfInterest, IPathfindingService pathfindingService, Army army)
	{
		StaticString category = ((ICategoryProvider)pointOfInterest).Category;
		if (StaticString.IsNullOrEmpty(category) && pointOfInterest.Type != ELCPUtilities.QuestLocation)
		{
			return;
		}
		if (this.PointOfInterestCategories != null && !this.PointOfInterestCategories.Contains(category) && (!ELCPUtilities.UseELCPSymbiosisBuffs || pointOfInterest.CreepingNodeGUID == GameEntityGUID.Zero))
		{
			return;
		}
		if (army.WorldPosition != pointOfInterest.WorldPosition && !pathfindingService.IsTransitionPassable(army.WorldPosition, pointOfInterest.WorldPosition, army, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreKaijuGarrisons, null))
		{
			return;
		}
		this.PointsOfInterest.Add(pointOfInterest);
	}
}
