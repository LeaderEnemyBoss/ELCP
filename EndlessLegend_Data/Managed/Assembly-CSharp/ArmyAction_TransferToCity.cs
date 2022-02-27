using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class ArmyAction_TransferToCity : ArmyAction, IArmyActionWithUnitSelection
{
	public override bool CanExecute(Army army, ref List<StaticString> failureFlags, params object[] parameters)
	{
		if (!base.CanExecute(army, ref failureFlags, parameters))
		{
			return false;
		}
		if (army.HasCatspaw)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		int num = 0;
		int num2 = 0;
		float num3 = float.MaxValue;
		bool flag = false;
		PathfindingMovementCapacity pathfindingMovementCapacity = PathfindingMovementCapacity.None;
		for (int i = 0; i < parameters.Length; i++)
		{
			if (parameters[i] is Unit)
			{
				Unit unit = parameters[i] as Unit;
				if (unit.UnitDesign != null && unit.UnitDesign.Tags.Contains(DownloadableContent9.TagSolitary))
				{
					failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
					return false;
				}
				if ((unit.UnitDesign != null && unit.UnitDesign.Tags.Contains(Kaiju.LiceUnitTag)) || unit.UnitDesign.Tags.Contains(Kaiju.MonsterUnitTag))
				{
					flag = true;
				}
				if (unit.SimulationObject.Tags.Contains(DownloadableContent16.SeafaringUnit))
				{
					failureFlags.Add(ArmyAction.NoCanDoWhileSeafaring);
					return false;
				}
				num2 += (int)unit.GetPropertyValue(SimulationProperties.UnitSlotCount);
				num++;
				pathfindingMovementCapacity = unit.GenerateContext().MovementCapacities;
			}
			else
			{
				if (!(parameters[i] is IEnumerable<Unit>))
				{
					failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
					return false;
				}
				foreach (Unit unit2 in (parameters[i] as IEnumerable<Unit>))
				{
					if (unit2.Garrison == null)
					{
						failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
						return false;
					}
					if (unit2.UnitDesign != null && unit2.UnitDesign.Tags.Contains(DownloadableContent9.TagSolitary))
					{
						failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
						return false;
					}
					if ((unit2.UnitDesign != null && unit2.UnitDesign.Tags.Contains(Kaiju.LiceUnitTag)) || unit2.UnitDesign.Tags.Contains(Kaiju.MonsterUnitTag))
					{
						flag = true;
					}
					if (unit2.SimulationObject.Tags.Contains(DownloadableContent16.SeafaringUnit))
					{
						if (!failureFlags.Contains(ArmyAction.NoCanDoWhileSeafaring))
						{
							failureFlags.Add(ArmyAction.NoCanDoWhileSeafaring);
						}
					}
					else
					{
						num2 += (int)unit2.GetPropertyValue(SimulationProperties.UnitSlotCount);
						num++;
						float propertyValue = unit2.GetPropertyValue(SimulationProperties.Movement);
						if (propertyValue < num3)
						{
							num3 = propertyValue;
						}
						if (pathfindingMovementCapacity == PathfindingMovementCapacity.None)
						{
							pathfindingMovementCapacity = unit2.GenerateContext().MovementCapacities;
						}
						else
						{
							pathfindingMovementCapacity &= unit2.GenerateContext().MovementCapacities;
						}
					}
				}
			}
		}
		if (failureFlags.Contains(ArmyAction.NoCanDoWhileSeafaring))
		{
			if (num == 0)
			{
				failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			}
			return false;
		}
		if (num == 0)
		{
			failureFlags.Add(ArmyAction_TransferUnits.NoUnitSelectedForTransfer);
			return false;
		}
		if (num3 == 0f)
		{
			failureFlags.Add(ArmyAction_TransferUnits.NotEnoughMovementToTransfer);
			return false;
		}
		if (flag)
		{
			failureFlags.Add(ArmyAction_TransferUnits.UntransferableUnitSelectedForTransfer);
			return false;
		}
		IGameService service = Services.GetService<IGameService>();
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		IPathfindingService service3 = service.Game.Services.GetService<IPathfindingService>();
		bool flag2 = false;
		bool flag3 = false;
		if (this.CanTransferTo(army, army.WorldPosition, ref flag2, ref flag3, service2, num2))
		{
			return true;
		}
		for (int j = 0; j < 6; j++)
		{
			WorldPosition neighbourTile = service2.GetNeighbourTile(army.WorldPosition, (WorldOrientation)j, 1);
			if (service3.IsTilePassable(neighbourTile, pathfindingMovementCapacity, (PathfindingFlags)0) && service3.IsTransitionPassable(army.WorldPosition, neighbourTile, pathfindingMovementCapacity, (PathfindingFlags)0))
			{
				if (this.CanTransferTo(army, neighbourTile, ref flag2, ref flag3, service2, num2))
				{
					return true;
				}
			}
		}
		if (flag2 || flag3)
		{
			failureFlags.Add(ArmyAction_TransferUnits.NotEnoughSlotsInNeighbouringGarrisonForTransfer);
		}
		else
		{
			failureFlags.Add(ArmyAction_TransferUnits.NoNeighbouringCityAvailable);
		}
		return false;
	}

	public override void Execute(Army army, global::PlayerController playerController, out Ticket ticket, EventHandler<TicketRaisedEventArgs> ticketRaisedEventHandler, params object[] parameters)
	{
		ticket = null;
		List<GameEntityGUID> list = new List<GameEntityGUID>();
		int num = 0;
		PathfindingMovementCapacity pathfindingMovementCapacity = PathfindingMovementCapacity.None;
		for (int i = 0; i < parameters.Length; i++)
		{
			if (parameters[i] is Unit)
			{
				Unit unit = parameters[i] as Unit;
				num += (int)unit.GetPropertyValue(SimulationProperties.UnitSlotCount);
				list.Add(unit.GUID);
				pathfindingMovementCapacity = unit.GenerateContext().MovementCapacities;
			}
			else
			{
				if (!(parameters[i] is IEnumerable<Unit>))
				{
					return;
				}
				foreach (Unit unit2 in (parameters[i] as IEnumerable<Unit>))
				{
					num += (int)unit2.GetPropertyValue(SimulationProperties.UnitSlotCount);
					list.Add(unit2.GUID);
					if (pathfindingMovementCapacity == PathfindingMovementCapacity.None)
					{
						pathfindingMovementCapacity = unit2.GenerateContext().MovementCapacities;
					}
					else
					{
						pathfindingMovementCapacity &= unit2.GenerateContext().MovementCapacities;
					}
				}
			}
		}
		IGameService service = Services.GetService<IGameService>();
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		IPathfindingService service3 = service.Game.Services.GetService<IPathfindingService>();
		bool flag = false;
		bool flag2 = false;
		if (this.CanTransferTo(army, army.WorldPosition, ref flag, ref flag2, service2, num) && this.TransferTo(army, army.WorldPosition, list, service2, playerController, out ticket, ticketRaisedEventHandler))
		{
			return;
		}
		for (int j = 0; j < 6; j++)
		{
			WorldPosition neighbourTile = service2.GetNeighbourTile(army.WorldPosition, (WorldOrientation)j, 1);
			if (service3.IsTilePassable(neighbourTile, pathfindingMovementCapacity, (PathfindingFlags)0) && service3.IsTransitionPassable(army.WorldPosition, neighbourTile, pathfindingMovementCapacity, (PathfindingFlags)0))
			{
				if (this.CanTransferTo(army, neighbourTile, ref flag, ref flag2, service2, num) && this.TransferTo(army, neighbourTile, list, service2, playerController, out ticket, ticketRaisedEventHandler))
				{
					return;
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
		EventSwapCity eventSwapCity = gameEvent as EventSwapCity;
		return (eventSwapCity != null && eventSwapCity.Empire.Index == army.Empire.Index) || base.IsConcernedByEvent(gameEvent, army);
	}

	private bool CanTransferTo(Army army, WorldPosition worldPosition, ref bool atLeastOneNeighbourgCityWithNotEnoughSlotsLeft, ref bool atLeastOneNeighbourgVillageWithNotEnoughSlotsLeft, IWorldPositionningService worldPositionningService, int transferringUnitSlot)
	{
		Region region = worldPositionningService.GetRegion(worldPosition);
		if (region.City != null && region.City.Empire == army.Empire)
		{
			int i = 0;
			while (i < region.City.Districts.Count)
			{
				if (region.City.Districts[i].Type != DistrictType.Exploitation && region.City.Districts[i].Type != DistrictType.Improvement && worldPosition == region.City.Districts[i].WorldPosition)
				{
					if (transferringUnitSlot + region.City.CurrentUnitSlot > region.City.MaximumUnitSlot)
					{
						atLeastOneNeighbourgCityWithNotEnoughSlotsLeft = true;
						break;
					}
					if (army.WorldPosition == worldPosition)
					{
						return true;
					}
					if (region.City.BesiegingEmpireIndex < 0 || (army.Hero != null && army.Hero.CheckUnitAbility(UnitAbility.UnitAbilityAllowAssignationUnderSiege, -1)))
					{
						return true;
					}
					break;
				}
				else
				{
					i++;
				}
			}
		}
		if (region != null && region.MinorEmpire != null)
		{
			BarbarianCouncil agency = region.MinorEmpire.GetAgency<BarbarianCouncil>();
			if (agency != null)
			{
				Village villageAt = agency.GetVillageAt(worldPosition);
				if (villageAt != null && villageAt.HasBeenConverted && villageAt.Converter == army.Empire)
				{
					if (transferringUnitSlot + villageAt.CurrentUnitSlot <= villageAt.MaximumUnitSlot)
					{
						return true;
					}
					atLeastOneNeighbourgVillageWithNotEnoughSlotsLeft = true;
				}
			}
		}
		return (region != null && region.City != null && region.City.Camp != null && worldPosition == region.City.Camp.WorldPosition && region.City.Camp.Empire == army.Empire && transferringUnitSlot + region.City.Camp.CurrentUnitSlot <= region.City.Camp.MaximumUnitSlot) || (region != null && region.Kaiju != null && region.Kaiju.KaijuGarrison != null && worldPosition == region.Kaiju.KaijuGarrison.WorldPosition && region.Kaiju.Empire.Index == army.Empire.Index && DepartmentOfScience.IsTechnologyResearched(army.Empire, "TechnologyDefinitionMimics1") && transferringUnitSlot + region.Kaiju.KaijuGarrison.CurrentUnitSlot <= region.Kaiju.KaijuGarrison.MaximumUnitSlot);
	}

	private bool TransferTo(Army army, WorldPosition worldPosition, List<GameEntityGUID> units, IWorldPositionningService worldPositionningService, global::PlayerController playerController, out Ticket ticket, EventHandler<TicketRaisedEventArgs> ticketRaisedEventHandler)
	{
		Region region = worldPositionningService.GetRegion(worldPosition);
		GameEntityGUID gameEntityGUID = GameEntityGUID.Zero;
		ticket = null;
		if (region.MinorEmpire != null)
		{
			BarbarianCouncil agency = region.MinorEmpire.GetAgency<BarbarianCouncil>();
			if (agency != null)
			{
				Village villageAt = agency.GetVillageAt(worldPosition);
				if (villageAt != null)
				{
					gameEntityGUID = villageAt.GUID;
				}
			}
		}
		if (region.City != null && region.City.Camp != null && gameEntityGUID == GameEntityGUID.Zero && region.City.Camp.WorldPosition == worldPosition)
		{
			gameEntityGUID = region.City.Camp.GUID;
		}
		if (region.City != null && gameEntityGUID == GameEntityGUID.Zero)
		{
			gameEntityGUID = region.City.GUID;
		}
		if (region.Kaiju != null && region.Kaiju.KaijuGarrison != null && gameEntityGUID == GameEntityGUID.Zero && DepartmentOfScience.IsTechnologyResearched(army.Empire, "TechnologyDefinitionMimics1") && region.Kaiju.KaijuGarrison.WorldPosition == worldPosition)
		{
			gameEntityGUID = region.Kaiju.KaijuGarrison.GUID;
		}
		if (gameEntityGUID != GameEntityGUID.Zero)
		{
			OrderTransferUnits order = new OrderTransferUnits(army.Empire.Index, army.GUID, gameEntityGUID, units.ToArray(), false);
			playerController.PostOrder(order, out ticket, ticketRaisedEventHandler);
			return true;
		}
		return false;
	}
}
