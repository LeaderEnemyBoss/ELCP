using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

[ArmyActionWorldCursor(typeof(ArmyActionTargetSelectionWorldCursor))]
public class ArmyAction_TameUnstunnedKaiju : ArmyAction, IArmyActionWithKaijuTameCost, IArmyActionWithTargetSelection
{
	public ArmyAction_TameUnstunnedKaiju()
	{
		this.TameCost = null;
	}

	public override IConstructionCost[] Costs
	{
		get
		{
			if (this.TameCost != null)
			{
				return new List<IConstructionCost>(base.Costs)
				{
					this.TameCost
				}.ToArray();
			}
			return base.Costs;
		}
	}

	[XmlElement(Type = typeof(KaijuTameCost), ElementName = "TameCost")]
	public KaijuTameCost TameCost { get; private set; }

	public override bool CanExecute(Army army, ref List<StaticString> failureFlags, params object[] parameters)
	{
		failureFlags.Clear();
		if (!base.CanExecute(army, ref failureFlags, parameters))
		{
			return false;
		}
		if (army.IsNaval)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		if (parameters == null || parameters.Length == 0)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		bool flag = false;
		for (int i = 0; i < parameters.Length; i++)
		{
			if (parameters[i] is Kaiju)
			{
				if (army.IsInEncounter)
				{
					failureFlags.Add(ArmyAction.NoCanDoWhileLockedInBattle);
					return false;
				}
				Kaiju kaiju = parameters[i] as Kaiju;
				if (!this.IsKaijuValidForTame(army, kaiju, ref failureFlags, false))
				{
					return false;
				}
				flag = true;
			}
			else if (parameters[i] is KaijuGarrison)
			{
				if (army.IsInEncounter)
				{
					failureFlags.Add(ArmyAction.NoCanDoWhileLockedInBattle);
					return false;
				}
				Kaiju kaiju2 = (parameters[i] as KaijuGarrison).Kaiju;
				if (!this.IsKaijuValidForTame(army, kaiju2, ref failureFlags, false))
				{
					return false;
				}
				flag = true;
			}
			else if (parameters[i] is List<IGameEntity>)
			{
				List<IGameEntity> list = parameters[i] as List<IGameEntity>;
				for (int j = 0; j < list.Count; j++)
				{
					if (list[j] is Kaiju)
					{
						if (army.IsInEncounter)
						{
							failureFlags.Add(ArmyAction.NoCanDoWhileLockedInBattle);
							return false;
						}
						Kaiju kaiju3 = list[j] as Kaiju;
						if (!this.IsKaijuValidForTame(army, kaiju3, ref failureFlags, false))
						{
							return false;
						}
						flag = true;
					}
				}
			}
		}
		if (!flag)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		DepartmentOfTheInterior agency = army.Empire.GetAgency<DepartmentOfTheInterior>();
		if (agency != null && agency.MainCity == null && agency.Cities.Count < 1)
		{
			failureFlags.Add(ArmyAction_TameKaiju.NoCanDoWhileMainCityIsNotSettled);
			return false;
		}
		if (!base.CheckActionPointsPrerequisites(army))
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileNotEnoughActionPointsLeft);
			return false;
		}
		if (!base.CanAfford(army))
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileCannotAffordCosts);
			return false;
		}
		return true;
	}

	public override void Execute(Army army, global::PlayerController playerController, out Ticket ticket, EventHandler<TicketRaisedEventArgs> ticketRaisedEventHandler, params object[] parameters)
	{
		ticket = null;
		ArmyAction.FailureFlags.Clear();
		Kaiju kaiju = null;
		if (parameters != null && parameters.Length != 0)
		{
			if (parameters[0] is Kaiju)
			{
				kaiju = (parameters[0] as Kaiju);
			}
			else if (parameters[0] is KaijuGarrison)
			{
				kaiju = (parameters[0] as KaijuGarrison).Kaiju;
			}
			else if (parameters[0] is KaijuArmy)
			{
				kaiju = (parameters[0] as KaijuArmy).Kaiju;
			}
		}
		if (kaiju != null)
		{
			OrderTameUnstunnedKaiju order = new OrderTameUnstunnedKaiju(army.Empire.Index, kaiju, army);
			Diagnostics.Assert(playerController != null);
			playerController.PostOrder(order, out ticket, ticketRaisedEventHandler);
		}
	}

	public void FillTargets(Army army, List<IGameEntity> gameEntities, ref List<StaticString> failureFlags)
	{
		if (army == null)
		{
			throw new ArgumentNullException("army");
		}
		IGameService service = Services.GetService<IGameService>();
		if (service == null)
		{
			return;
		}
		global::Game x = service.Game as global::Game;
		if (x == null)
		{
			return;
		}
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(service2 != null);
		for (int i = 0; i < 6; i++)
		{
			WorldPosition neighbourTile = service2.GetNeighbourTile(army.WorldPosition, (WorldOrientation)i, 1);
			Region region = service2.GetRegion(neighbourTile);
			if (region != null && region.Kaiju != null)
			{
				Kaiju kaiju = region.Kaiju;
				if (this.IsKaijuValidForTame(army, kaiju, ref failureFlags, true) && !gameEntities.Contains(kaiju))
				{
					gameEntities.Add(kaiju);
				}
			}
			Army armyAtPosition = service2.GetArmyAtPosition(neighbourTile);
			if (armyAtPosition != null && armyAtPosition is KaijuArmy)
			{
				KaijuArmy kaijuArmy = armyAtPosition as KaijuArmy;
				if (kaijuArmy != null)
				{
					Kaiju kaiju2 = kaijuArmy.Kaiju;
					if (kaiju2 != null && kaiju2.OnArmyMode() && !kaiju2.IsStunned() && !gameEntities.Contains(kaiju2))
					{
						gameEntities.Add(kaijuArmy.Kaiju);
					}
				}
			}
		}
	}

	private bool IsKaijuValidForTame(Army army, Kaiju kaiju, ref List<StaticString> failureFlags, bool checkNearbyKaijus = true)
	{
		if (army == null || kaiju == null)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		if (checkNearbyKaijus && !this.IsKaijuNearby(kaiju, army))
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		if (kaiju.IsTamed())
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		if (kaiju.IsStunned())
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		return true;
	}

	private bool IsKaijuNearby(Kaiju kaiju, Army army)
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		global::Game game = service.Game as global::Game;
		Diagnostics.Assert(game != null);
		List<WorldPosition> neighbours = army.WorldPosition.GetNeighbours(game.World.WorldParameters);
		for (int i = 0; i < neighbours.Count; i++)
		{
			if (neighbours[i] == kaiju.WorldPosition)
			{
				return true;
			}
		}
		return false;
	}

	public static readonly StaticString ReadOnlyName = "ArmyActionTameUnstunnedKaiju";

	public static readonly StaticString NoCanDoWhileCannotAfford = "ArmyActionCannotAffordTame";

	public static readonly StaticString NoCanDoWhileKaijuIsAlreadyTamed = "ArmyActionAlreadyTamed";

	public static readonly StaticString NoCanDoWhileKaijuIsStunned = "ArmyActionKaijuStunned";

	public static readonly StaticString NoCanDoWhileMainCityIsNotSettled = "ArmyActionCanNotTameKaijuMissingMainCity";
}
