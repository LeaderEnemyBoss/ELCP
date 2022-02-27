using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

[ArmyActionWorldCursor(typeof(ArmyActionTeleportWorldCursor))]
public class ArmyAction_Teleport : ArmyAction, IArmyActionWithTargetSelection, IArmyActionWithUnitSelection
{
	[XmlAttribute]
	public bool AllowTransferToHeroLedArmy
	{
		get
		{
			IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
			return service.IsShared(DownloadableContent19.ReadOnlyName) && this.allowTransferToHeroLedArmy;
		}
		set
		{
			this.allowTransferToHeroLedArmy = value;
		}
	}

	public override bool CanExecute(Army army, ref List<StaticString> failureFlags, params object[] parameters)
	{
		if (!base.CanExecute(army, ref failureFlags, parameters))
		{
			return false;
		}
		IGameService service = Services.GetService<IGameService>();
		if (service == null)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileSystemError);
			return false;
		}
		global::Game x = service.Game as global::Game;
		if (x == null)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileSystemError);
			return false;
		}
		if (army.Empire == null)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		if (!army.Empire.SimulationObject.Tags.Contains("FactionTraitAffinityStrategic"))
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		if (army.IsNaval)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		for (int i = 0; i < parameters.Length; i++)
		{
			if (parameters[i] is IGameEntity)
			{
				failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
				return false;
			}
		}
		if (army.IsInEncounter)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileLockedInBattle);
			return false;
		}
		if (!army.Empire.SimulationObject.Tags.Contains("BoosterTeleport"))
		{
			failureFlags.Add(ArmyAction_Teleport.NoBoosterForTeleport);
			return false;
		}
		if (army.GetPropertyValue(SimulationProperties.Movement) <= 0f)
		{
			failureFlags.Add(ArmyAction_Teleport.NotEnoughMovementToTeleport);
			return false;
		}
		if (army.StandardUnits.Any((Unit unit) => unit.SimulationObject.Tags.Contains(Unit.ReadOnlyColossus)))
		{
			failureFlags.Add(ArmyAction_Teleport.NoColossusAllowed);
			return false;
		}
		Region region = this.GetRegion(army);
		if (!this.IsOnCityTile(army, region))
		{
			failureFlags.Add(ArmyAction_Teleport.NotOnOwnCity);
			return false;
		}
		bool flag = false;
		bool flag2 = false;
		DepartmentOfTheInterior agency = army.Empire.GetAgency<DepartmentOfTheInterior>();
		List<City> list = agency.Cities.ToList<City>();
		list.Remove(region.City);
		if (list.Count > 0)
		{
			flag = true;
		}
		if (this.AllowTransferToHeroLedArmy && army.StandardUnits.Count > 0)
		{
			DepartmentOfDefense agency2 = army.Empire.GetAgency<DepartmentOfDefense>();
			List<Army> list2 = agency2.GetHeroLedArmies().ToList<Army>();
			list2.Remove(army);
			if (list2.Count > 0)
			{
				flag2 = true;
			}
		}
		if (!flag)
		{
			if (!this.AllowTransferToHeroLedArmy)
			{
				failureFlags.Add(ArmyAction_Teleport.NoCityToTeleportTo);
				return false;
			}
			if (!flag2)
			{
				failureFlags.Add(ArmyAction_Teleport.NoCityOrArmyToTeleportTo);
				return false;
			}
		}
		else if (!this.HasGotAnyCityToTeleportTo(list, army, region) && !flag2)
		{
			failureFlags.Add(ArmyAction_Teleport.NoFreeCityTileToTeleportTo);
			return false;
		}
		if (!base.CheckActionPointsPrerequisites(army))
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileNotEnoughActionPointsLeft);
			return false;
		}
		list.Clear();
		return true;
	}

	public override void Execute(Army army, global::PlayerController playerController, out Ticket ticket, EventHandler<TicketRaisedEventArgs> ticketRaisedEventHandler, params object[] parameters)
	{
		ticket = null;
		if (parameters.Length > 0)
		{
			City city = parameters[0] as City;
			if (city != null)
			{
				Diagnostics.Log("Teleporting to {0}", new object[]
				{
					city.Name
				});
				OrderTeleportArmyToCity order = new OrderTeleportArmyToCity(army.Empire.Index, army.GUID, city.GUID);
				Diagnostics.Assert(playerController != null);
				playerController.PostOrder(order, out ticket, ticketRaisedEventHandler);
				return;
			}
			Army army2 = parameters[0] as Army;
			if (army2 != null)
			{
				GameEntityGUID[] array = parameters[1] as GameEntityGUID[];
				if (array != null)
				{
					Diagnostics.Log("Transferring units to {0} by teleport.", new object[]
					{
						army2.Name
					});
					OrderTransferUnits order2 = new OrderTransferUnits(army.Empire.Index, army.GUID, army2.GUID, array, true);
					Diagnostics.Assert(playerController != null);
					playerController.PostOrder(order2, out ticket, ticketRaisedEventHandler);
					IEventService service = Services.GetService<IEventService>();
					if (service != null)
					{
						EventUnitsTeleported eventToNotify = new EventUnitsTeleported(army.Empire, array.Length);
						service.Notify(eventToNotify);
					}
					return;
				}
			}
		}
	}

	public void FillTargets(Army army, List<IGameEntity> targets, ref List<StaticString> failureFlags)
	{
		DepartmentOfTheInterior agency = army.Empire.GetAgency<DepartmentOfTheInterior>();
		List<City> list = agency.Cities.ToList<City>();
		Region region = this.GetRegion(army);
		list.Remove(region.City);
		for (int i = 0; i < list.Count; i++)
		{
			if (this.CanTeleportToCity(list[i], army, region))
			{
				targets.Add(list[i]);
			}
		}
		if (this.AllowTransferToHeroLedArmy)
		{
			DepartmentOfDefense agency2 = army.Empire.GetAgency<DepartmentOfDefense>();
			ReadOnlyCollection<Army> heroLedArmies = agency2.GetHeroLedArmies();
			for (int j = 0; j < heroLedArmies.Count; j++)
			{
				Army army2 = heroLedArmies[j];
				if (army2 != army)
				{
					targets.Add(army2);
				}
			}
		}
	}

	public override bool IsConcernedByEvent(Event gameEvent, Army army)
	{
		if (army == null || army.Empire == null)
		{
			return false;
		}
		if (!army.Empire.SimulationObject.Tags.Contains("FactionTraitAffinityStrategic"))
		{
			return false;
		}
		EventBoosterStarted eventBoosterStarted = gameEvent as EventBoosterStarted;
		if (eventBoosterStarted != null && eventBoosterStarted.Empire == army.Empire && eventBoosterStarted.Booster.BoosterDefinition.Name.ToString().StartsWith("BoosterStrategic"))
		{
			return true;
		}
		EventBoosterEnded eventBoosterEnded = gameEvent as EventBoosterEnded;
		if (eventBoosterEnded != null && eventBoosterEnded.Empire == army.Empire && eventBoosterEnded.BoosterDefinition.Name.ToString().StartsWith("BoosterStrategic"))
		{
			return true;
		}
		EventColonize eventColonize = gameEvent as EventColonize;
		if (eventColonize != null && eventColonize.Empire == army.Empire)
		{
			return true;
		}
		EventSwapCity eventSwapCity = gameEvent as EventSwapCity;
		return (eventSwapCity != null && eventSwapCity.Empire == army.Empire) || base.IsConcernedByEvent(gameEvent, army);
	}

	private Region GetRegion(Army army)
	{
		IGameService service = Services.GetService<IGameService>();
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		return service2.GetRegion(army.WorldPosition);
	}

	private bool IsOnCityTile(Army army, Region originRegion)
	{
		if (originRegion.City == null || originRegion.City.Empire != army.Empire)
		{
			return false;
		}
		for (int i = 0; i < originRegion.City.Districts.Count; i++)
		{
			if (originRegion.City.Districts[i].Type != DistrictType.Exploitation && originRegion.City.Districts[i].WorldPosition == army.WorldPosition)
			{
				return true;
			}
		}
		return false;
	}

	private bool HasGotAnyCityToTeleportTo(List<City> cities, Army army, Region regionOfOrigin)
	{
		for (int i = 0; i < cities.Count; i++)
		{
			if (this.CanTeleportToCity(cities[i], army, regionOfOrigin))
			{
				return true;
			}
		}
		return false;
	}

	private bool CanTeleportToCity(City city, Army army, Region originRegion)
	{
		if (city == null)
		{
			return false;
		}
		if (city == originRegion.City)
		{
			return false;
		}
		IEncounterRepositoryService service = Services.GetService<IGameService>().Game.Services.GetService<IEncounterRepositoryService>();
		if (service != null)
		{
			IEnumerable<Encounter> enumerable = service;
			if (enumerable != null && enumerable.Any((Encounter encounter) => encounter.IsGarrisonInEncounter(city.GUID, false)))
			{
				return false;
			}
		}
		DepartmentOfTransportation agency = army.Empire.GetAgency<DepartmentOfTransportation>();
		WorldPosition worldPosition;
		return agency.TryGetFirstCityTileAvailableForTeleport(city, out worldPosition) && worldPosition.IsValid;
	}

	public static readonly StaticString NoBoosterForTeleport = "ArmyActionNoBoosterActivatedForTeleport";

	public static readonly StaticString NotOnOwnCity = "ArmyActionNotOnOwnCity";

	public static readonly StaticString NoCityToTeleportTo = "ArmyActionNoCityToTeleportTo";

	public static readonly StaticString NoCityOrArmyToTeleportTo = "ArmyActionNoCityOrArmyToTeleportTo";

	public static readonly StaticString NoFreeCityTileToTeleportTo = "ArmyActionNoFreeCityTileToTeleportTo";

	public static readonly StaticString NotEnoughMovementToTeleport = "ArmyActionNotEnoughMovementToTeleport";

	public static readonly StaticString NoColossusAllowed = "ArmyActionNoColossusAllowed";

	private bool allowTransferToHeroLedArmy;
}
