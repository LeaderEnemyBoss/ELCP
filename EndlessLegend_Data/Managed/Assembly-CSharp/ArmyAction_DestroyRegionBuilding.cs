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
			}
		}
		else
		{
			this.ListNearbyPointsOfInterest(army);
			this.FilterPointsOfInterest(army, ref ArmyAction.FailureFlags);
			if (ArmyAction.FailureFlags.Count == 0 && this.PointsOfInterest.Count > 0)
			{
				GameEntityGUID[] pointsOfInterestGUIDs = (from enumerator in this.PointsOfInterest
				select enumerator.GUID).ToArray<GameEntityGUID>();
				OrderDestroyPointOfInterestImprovement orderDestroyPointOfInterestImprovement2 = new OrderDestroyPointOfInterestImprovement(army.Empire.Index, pointsOfInterestGUIDs);
				orderDestroyPointOfInterestImprovement2.ArmyGUID = army.GUID;
				orderDestroyPointOfInterestImprovement2.ArmyActionName = this.Name;
				orderDestroyPointOfInterestImprovement2.NumberOfActionPointsToSpend = base.GetCostInActionPoints();
				orderDestroyPointOfInterestImprovement2.ArmyActionCooldownDuration = base.ComputeCooldownDuration(army);
				Diagnostics.Assert(playerController != null);
				playerController.PostOrder(orderDestroyPointOfInterestImprovement2, out ticket, ticketRaisedEventHandler);
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
		if (!(this is IArmyActionWithTargetSelection))
		{
			Region region = service3.GetRegion(army.WorldPosition);
			int i = 0;
			while (i < region.PointOfInterests.Length)
			{
				PointOfInterest pointOfInterest = region.PointOfInterests[i];
				if (pointOfInterest.WorldPosition == army.WorldPosition)
				{
					StaticString category = ((ICategoryProvider)pointOfInterest).Category;
					if (StaticString.IsNullOrEmpty(category))
					{
						break;
					}
					if (this.PointOfInterestCategories != null && !this.PointOfInterestCategories.Contains(category))
					{
						break;
					}
					this.PointsOfInterest.Add(pointOfInterest);
					break;
				}
				else
				{
					i++;
				}
			}
		}
		List<WorldPosition> neighbours = army.WorldPosition.GetNeighbours(game.World.WorldParameters);
		for (int j = 0; j < neighbours.Count; j++)
		{
			Region region2 = service3.GetRegion(neighbours[j]);
			int k = 0;
			while (k < region2.PointOfInterests.Length)
			{
				PointOfInterest pointOfInterest2 = region2.PointOfInterests[k];
				if (pointOfInterest2.WorldPosition == neighbours[j])
				{
					StaticString category2 = ((ICategoryProvider)pointOfInterest2).Category;
					if (StaticString.IsNullOrEmpty(category2))
					{
						break;
					}
					if (this.PointOfInterestCategories != null && !this.PointOfInterestCategories.Contains(category2))
					{
						break;
					}
					if (service2 != null && !service2.IsTransitionPassable(army.WorldPosition, pointOfInterest2.WorldPosition, army, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreKaijuGarrisons, null))
					{
						break;
					}
					this.PointsOfInterest.Add(pointOfInterest2);
					break;
				}
				else
				{
					k++;
				}
			}
		}
		return this.PointsOfInterest.Count;
	}

	private void FilterPointsOfInterest(Army army, ref List<StaticString> failureFlags)
	{
		for (int i = this.PointsOfInterest.Count - 1; i >= 0; i--)
		{
			if (this.PointsOfInterest[i].PointOfInterestImprovement == null)
			{
				this.PointsOfInterest.RemoveAt(i);
			}
			else if (this.PointsOfInterest[i].Empire != null && this.PointsOfInterest[i].Empire.Index == army.Empire.Index)
			{
				this.PointsOfInterest.RemoveAt(i);
			}
		}
	}
}
