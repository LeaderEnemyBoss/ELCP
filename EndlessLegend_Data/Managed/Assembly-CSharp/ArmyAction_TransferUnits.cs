using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

[TransferUnitWorldPlacementCursor]
public class ArmyAction_TransferUnits : ArmyAction, IArmyActionWithUnitSelection
{
	public ArmyAction_TransferUnits()
	{
		this.AllowedTransferTarget = ArmyAction_TransferUnits.AllowedTransferTargetType.All;
	}

	[XmlAttribute]
	public ArmyAction_TransferUnits.AllowedTransferTargetType AllowedTransferTarget { get; set; }

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
				num2 += (int)unit.GetPropertyValue(SimulationProperties.UnitSlotCount);
				num++;
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
					if (unit2.UnitDesign != null && unit2.UnitDesign.Tags.Contains(DownloadableContent9.TagSolitary))
					{
						failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
						return false;
					}
					if ((unit2.UnitDesign != null && unit2.UnitDesign.Tags.Contains(Kaiju.LiceUnitTag)) || unit2.UnitDesign.Tags.Contains(Kaiju.MonsterUnitTag))
					{
						flag = true;
					}
					num2 += (int)unit2.GetPropertyValue(SimulationProperties.UnitSlotCount);
					num++;
					float propertyValue = unit2.GetPropertyValue(SimulationProperties.Movement);
					if (propertyValue < num3)
					{
						num3 = propertyValue;
					}
				}
			}
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
		PathfindingMovementCapacity pathfindingMovementCapacity = army.GenerateContext().MovementCapacities;
		for (int j = 0; j < parameters.Length; j++)
		{
			if (parameters[j] is Unit)
			{
				pathfindingMovementCapacity = (parameters[j] as Unit).GenerateContext().MovementCapacities;
			}
			else if (parameters[j] is IEnumerable<Unit>)
			{
				pathfindingMovementCapacity = PathfindingMovementCapacity.All;
				foreach (Unit unit3 in (parameters[j] as IEnumerable<Unit>))
				{
					if (unit3.Garrison != null)
					{
						pathfindingMovementCapacity &= unit3.GenerateContext().MovementCapacities;
					}
				}
			}
		}
		for (int k = 0; k < 6; k++)
		{
			bool flag4 = false;
			WorldPosition neighbourTile = service2.GetNeighbourTile(army.WorldPosition, (WorldOrientation)k, 1);
			if (service3.IsTilePassable(neighbourTile, pathfindingMovementCapacity, (PathfindingFlags)0) && service3.IsTransitionPassable(army.WorldPosition, neighbourTile, army, PathfindingFlags.IgnoreArmies, null) && service3.IsTileStopable(neighbourTile, pathfindingMovementCapacity, PathfindingFlags.IgnoreArmies))
			{
				Region region = service2.GetRegion(neighbourTile);
				if (region.City != null && this.AllowedTransferTarget != ArmyAction_TransferUnits.AllowedTransferTargetType.Army)
				{
					for (int l = 0; l < region.City.Districts.Count; l++)
					{
						if (region.City.Districts[l].Type != DistrictType.Exploitation && neighbourTile == region.City.Districts[l].WorldPosition)
						{
							if (this.AllowedTransferTarget != ArmyAction_TransferUnits.AllowedTransferTargetType.Army)
							{
								if (num2 + region.City.CurrentUnitSlot <= region.City.MaximumUnitSlot)
								{
									return true;
								}
								flag2 = true;
							}
							flag4 = true;
							break;
						}
					}
				}
				if (this.AllowedTransferTarget != ArmyAction_TransferUnits.AllowedTransferTargetType.Army && flag2)
				{
					failureFlags.Add(ArmyAction_TransferUnits.NotEnoughSlotsInNeighbouringGarrisonForTransfer);
				}
				Army armyAtPosition = service2.GetArmyAtPosition(neighbourTile);
				if (armyAtPosition != null && this.AllowedTransferTarget != ArmyAction_TransferUnits.AllowedTransferTargetType.City && armyAtPosition.Empire == army.Empire && !armyAtPosition.HasCatspaw)
				{
					if (armyAtPosition.SimulationObject.Tags.Contains(KaijuArmy.ClassKaijuArmy) && !DepartmentOfScience.IsTechnologyResearched(army.Empire, "TechnologyDefinitionMimics1"))
					{
						failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
						return false;
					}
					if (num2 + armyAtPosition.CurrentUnitSlot <= armyAtPosition.MaximumUnitSlot)
					{
						return true;
					}
					flag3 = true;
				}
				else if (!flag4 && this.AllowedTransferTarget != ArmyAction_TransferUnits.AllowedTransferTargetType.City && armyAtPosition == null && num != army.StandardUnits.Count && service3.IsTileStopable(neighbourTile, army, (PathfindingFlags)0, null))
				{
					return true;
				}
			}
		}
		if (this.AllowedTransferTarget > ArmyAction_TransferUnits.AllowedTransferTargetType.City && flag3)
		{
			failureFlags.Add(ArmyAction_TransferUnits.NotEnoughSlotsInNeighbouringArmyForTransfer);
		}
		if (this.AllowedTransferTarget != ArmyAction_TransferUnits.AllowedTransferTargetType.Army)
		{
			failureFlags.Add(ArmyAction_TransferUnits.NoNeighbouringCityAvailable);
		}
		if (this.AllowedTransferTarget != ArmyAction_TransferUnits.AllowedTransferTargetType.City)
		{
			failureFlags.Add(ArmyAction_TransferUnits.NoNeighbouringArmyAvailable);
		}
		ArmyAction_TransferUnits.AllowedTransferTargetType allowedTransferTarget = this.AllowedTransferTarget;
		return false;
	}

	public override void Execute(Army army, global::PlayerController playerController, out Ticket ticket, EventHandler<TicketRaisedEventArgs> ticketRaisedEventHandler, params object[] parameters)
	{
		ticket = null;
	}

	public static readonly StaticString NoUnitSelectedForTransfer = "ArmyActionNoUnitSelectedForTransfer";

	public static readonly StaticString NotEnoughMovementToTransfer = "ArmyActionNotEnoughMovementToTransfer";

	public static readonly StaticString NotEnoughSlotsInNeighbouringGarrisonForTransfer = "ArmyActionNotEnoughSlotsInNeighbouringGarrisonForTransfer";

	public static readonly StaticString NotEnoughSlotsInNeighbouringArmyForTransfer = "ArmyActionNotEnoughSlotsInNeighbouringArmyForTransfer";

	public static readonly StaticString NoNeighbouringCityAvailable = "ArmyActionNoNeighbouringCityAvailable";

	public static readonly StaticString NoNeighbouringArmyAvailable = "ArmyActionNoNeighbouringArmyAvailable";

	public static readonly StaticString CannotMergeSeafaringWithTransportShip = "CanNotMergeSeafaringWithTransportShip";

	public static readonly StaticString UntransferableUnitSelectedForTransfer = "ArmyActionUntransferableUnitSelectedForTransfer";

	public enum AllowedTransferTargetType
	{
		City,
		Army,
		All
	}
}
