using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

[ArmyActionWorldCursor(typeof(ArmyActionTargetSelectionWorldCursor))]
public class ArmyAction_ToggleDismantleDevice : ArmyAction, IArmyActionWithTargetSelection, IArmyActionWithToggle
{
	public bool IsToggled(Army army)
	{
		return army != null && army.IsDismantlingDevice;
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
		if (army.IsNaval || army.IsPillaging || army.IsDismantlingCreepingNode)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		if (army.IsAspirating)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileAspirating);
			return false;
		}
		if (army.IsEarthquaker)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileEarthquaking);
			return false;
		}
		if (parameters != null && parameters.Length > 0)
		{
			for (int i = 0; i < parameters.Length; i++)
			{
				if (parameters[i] is TerraformDevice)
				{
					TerraformDevice terraformDevice = parameters[i] as TerraformDevice;
					if (terraformDevice != null && this.CanToggleOverDevice(army, terraformDevice, ref failureFlags))
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
						if (list[j] is TerraformDevice)
						{
							TerraformDevice terraformDevice2 = list[j] as TerraformDevice;
							if (terraformDevice2 != null && this.CanToggleOverDevice(army, terraformDevice2, ref failureFlags))
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
		TerraformDevice terraformDevice = null;
		if (parameters != null && parameters.Length > 0 && parameters[0] is TerraformDevice)
		{
			terraformDevice = (parameters[0] as TerraformDevice);
		}
		List<StaticString> list = new List<StaticString>();
		if (terraformDevice == null || !this.CanToggleOverDevice(army, terraformDevice, ref list))
		{
			return;
		}
		if (terraformDevice.DismantlingArmy == null)
		{
			if (army.IsDismantlingDevice)
			{
				OrderToggleDismantleDevice order = new OrderToggleDismantleDevice(army.Empire.Index, army.GUID, army.DismantlingDeviceTarget, false);
				playerController.PostOrder(order, out ticket, ticketRaisedEventHandler);
			}
			OrderToggleDismantleDevice order2 = new OrderToggleDismantleDevice(army.Empire.Index, army.GUID, terraformDevice.GUID, true);
			playerController.PostOrder(order2, out ticket, ticketRaisedEventHandler);
		}
		else if (terraformDevice.DismantlingArmy == army)
		{
			OrderToggleDismantleDevice order3 = new OrderToggleDismantleDevice(army.Empire.Index, army.GUID, terraformDevice.GUID, false);
			playerController.PostOrder(order3, out ticket, ticketRaisedEventHandler);
		}
	}

	public void FillTargets(Army army, List<IGameEntity> targets, ref List<StaticString> failureFlags)
	{
		this.ListNearbyDevicesFiltered(army);
		for (int i = 0; i < this.devices.Count; i++)
		{
			targets.Add(this.devices[i]);
		}
	}

	private bool CanToggleOverDevice(Army army, TerraformDevice device, ref List<StaticString> failureFlags)
	{
		if (army == null || device == null || device.Empire == army.Empire)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		if (device.DismantlingArmy != null)
		{
			if (device.DismantlingArmy == army)
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
			Army armyAtPosition = service2.GetArmyAtPosition(device.WorldPosition);
			if (device != null && device.DismantlingArmy == null && armyAtPosition != null && device.Empire.Index != army.Empire.Index)
			{
				failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
				return false;
			}
			if (device.Empire != army.Empire && army.Empire is MajorEmpire && !(device.Empire is LesserEmpire))
			{
				DepartmentOfForeignAffairs agency = army.Empire.GetAgency<DepartmentOfForeignAffairs>();
				DiplomaticRelation diplomaticRelation = agency.DiplomaticRelations[device.Empire.Index];
				if (diplomaticRelation != null && diplomaticRelation.State != null && (diplomaticRelation.State.Name == DiplomaticRelationState.Names.Alliance || diplomaticRelation.State.Name == DiplomaticRelationState.Names.Peace))
				{
					failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
					return false;
				}
			}
			IPathfindingService service3 = service.Game.Services.GetService<IPathfindingService>();
			Diagnostics.Assert(service3 != null);
			PathfindingFlags flags = PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreKaijuGarrisons;
			if (!service3.IsTransitionPassable(army.WorldPosition, device.WorldPosition, army, flags, null))
			{
				failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
				return false;
			}
			return true;
		}
	}

	private void ListNearbyDevicesFiltered(Army army)
	{
		if (army == null)
		{
			throw new ArgumentNullException("army");
		}
		this.devices.Clear();
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		global::Game game = service.Game as global::Game;
		Diagnostics.Assert(game != null);
		ITerraformDeviceService service2 = game.Services.GetService<ITerraformDeviceService>();
		Diagnostics.Assert(service2 != null);
		List<WorldPosition> neighbours = army.WorldPosition.GetNeighbours(game.World.WorldParameters);
		for (int i = 0; i < neighbours.Count; i++)
		{
			TerraformDevice deviceAtPosition = service2.GetDeviceAtPosition(neighbours[i]);
			List<StaticString> list = new List<StaticString>();
			if (this.CanToggleOverDevice(army, deviceAtPosition, ref list) && !this.devices.Contains(deviceAtPosition))
			{
				this.devices.Add(deviceAtPosition);
			}
		}
	}

	public static readonly StaticString ReadOnlyName = new StaticString("ArmyActionDismantleDevice");

	private List<TerraformDevice> devices = new List<TerraformDevice>();
}
