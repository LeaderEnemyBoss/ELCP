using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class ArmyAction_SettleKaiju : ArmyAction
{
	public ArmyAction_SettleKaiju()
	{
		base.ShowInModalPanel = false;
	}

	public StaticString GetKaijuTypeDefinitionName(Army army)
	{
		StaticString result = "Kaiju1";
		if (army is KaijuArmy)
		{
			KaijuArmy kaijuArmy = army as KaijuArmy;
			Kaiju kaiju = kaijuArmy.Kaiju;
			if (kaiju.SimulationObject.Tags.Contains("Kaiju1"))
			{
				result = "KaijuType1";
			}
			else if (kaiju.SimulationObject.Tags.Contains("Kaiju2"))
			{
				result = "KaijuType2";
			}
			else if (kaiju.SimulationObject.Tags.Contains("Kaiju3"))
			{
				result = "KaijuType3";
			}
		}
		return result;
	}

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
		KaijuArmy kaijuArmy = army as KaijuArmy;
		if (kaijuArmy == null)
		{
			return false;
		}
		if (!base.CanAfford(army))
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileCannotAffordCosts);
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
		Kaiju kaiju = kaijuArmy.Kaiju;
		if (Services.GetService<IGameService>().Game.Services.GetService<IWorldPositionningService>().IsWaterTile(kaijuArmy.WorldPosition))
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileOnFrozenWaterTile);
			return false;
		}
		if (kaiju.CanChangeToGarrisonMode())
		{
			return true;
		}
		failureFlags.Add(ArmyAction_SettleKaiju.NoCanDoWhileRegionIsOwned);
		return false;
	}

	public override void Execute(Army army, global::PlayerController playerController, out Ticket ticket, EventHandler<TicketRaisedEventArgs> ticketRaisedEventHandler, params object[] parameters)
	{
		ticket = null;
		ArmyAction.FailureFlags.Clear();
		if (army is KaijuArmy)
		{
			KaijuArmy kaijuArmy = army as KaijuArmy;
			if (kaijuArmy != null)
			{
				OrderKaijuChangeMode order = new OrderKaijuChangeMode(kaijuArmy.Kaiju, true, false, true);
				Diagnostics.Assert(playerController != null);
				playerController.PostOrder(order, out ticket, ticketRaisedEventHandler);
			}
		}
	}

	public static readonly StaticString ReadOnlyName = "ArmyActionSettleKaiju";

	public static readonly StaticString NoCanDoWhileRegionIsOwned = "ArmyActionSettleKaijuRegionIsOwned";
}
