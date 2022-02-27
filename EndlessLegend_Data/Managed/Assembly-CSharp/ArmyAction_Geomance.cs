using System;
using System.Collections.Generic;
using Amplitude;

[ArmyActionTerraformWorldPlacementCursor]
public class ArmyAction_Geomance : ArmyActionWithCooldown
{
	public override bool CanExecute(Army army, ref List<StaticString> failureFlags, params object[] parameters)
	{
		if (!base.CanExecute(army, ref failureFlags, parameters))
		{
			return false;
		}
		if (army.IsNaval)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		if (parameters != null && parameters.Length != 0)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		if (!this.CheckCooldownPrerequisites(army))
		{
			failureFlags.Add(ArmyActionWithCooldown.NoCanDoWhileCooldownInProgress);
			return false;
		}
		if (!base.CheckActionPointsPrerequisites(army))
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileNotEnoughActionPointsLeft);
			return false;
		}
		if (army.IsInEncounter)
		{
			if (failureFlags != null)
			{
				failureFlags.Add(ArmyAction.NoCanDoWhileLockedInBattle);
			}
			return false;
		}
		return true;
	}

	public override void Execute(Army army, PlayerController playerController, out Ticket ticket, EventHandler<TicketRaisedEventArgs> ticketRaisedEventHandler, params object[] parameters)
	{
		ticket = null;
		OrderBuyoutAndPlaceTerraformationDevice order = new OrderBuyoutAndPlaceTerraformationDevice(army.Empire.Index, army.GUID, this.Name, "TerraformDevice1");
		Diagnostics.Assert(playerController != null);
		playerController.PostOrder(order, out ticket, ticketRaisedEventHandler);
	}
}
