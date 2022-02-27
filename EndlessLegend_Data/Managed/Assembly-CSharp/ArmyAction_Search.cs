using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation;

[ArmyActionWorldCursor(typeof(ArmyActionTargetSelectionWorldCursor))]
public class ArmyAction_Search : ArmyAction_BasePointOfInterest, IArmyActionWithTargetSelection
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
		base.PointsOfInterest.Clear();
		for (int i = 0; i < parameters.Length; i++)
		{
			if (parameters[i] is IGameEntity)
			{
				base.PointsOfInterest.Add(parameters[i] as PointOfInterest);
			}
			else if (parameters[i] is List<IGameEntity>)
			{
				List<IGameEntity> list = parameters[i] as List<IGameEntity>;
				for (int j = 0; j < list.Count; j++)
				{
					base.PointsOfInterest.Add(list[j] as PointOfInterest);
				}
			}
		}
		bool flag = false;
		this.FilterPointOfInterests(army, ref flag);
		if (base.PointsOfInterest.Count <= 0)
		{
			if (!flag)
			{
				failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			}
			else
			{
				failureFlags.Add(ArmyAction_Search.POIAlreadySearched);
			}
			return false;
		}
		if (army.IsInEncounter)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileLockedInBattle);
			return false;
		}
		if (army.IsNaval)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		if (!base.CheckActionPointsPrerequisites(army))
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileNotEnoughActionPointsLeft);
			return false;
		}
		return true;
	}

	public override void Execute(Army army, global::PlayerController playerController, out Ticket ticket, EventHandler<TicketRaisedEventArgs> ticketRaisedEventHandler, params object[] parameters)
	{
		ticket = null;
		PointOfInterest pointOfInterest = null;
		if (parameters != null && parameters.Length != 0 && parameters[0] is PointOfInterest)
		{
			pointOfInterest = (parameters[0] as PointOfInterest);
		}
		ArmyAction.FailureFlags.Clear();
		if (pointOfInterest != null)
		{
			OrderInteractWith orderInteractWith = new OrderInteractWith(army.Empire.Index, army.GUID, this.Name);
			orderInteractWith.WorldPosition = army.WorldPosition;
			orderInteractWith.Tags.AddTag("Interact");
			orderInteractWith.TargetGUID = pointOfInterest.GUID;
			orderInteractWith.ArmyActionName = this.Name;
			orderInteractWith.NumberOfActionPointsToSpend = base.GetCostInActionPoints();
			Diagnostics.Assert(playerController != null);
			playerController.PostOrder(orderInteractWith, out ticket, ticketRaisedEventHandler);
		}
	}

	public void FillTargets(Army army, List<IGameEntity> targets, ref List<StaticString> failureFlags)
	{
		base.ListNearbyPointsOfInterestOfType(army, "QuestLocation");
		bool flag = false;
		this.FilterPointOfInterests(army, ref flag);
		for (int i = 0; i < base.PointsOfInterest.Count; i++)
		{
			targets.Add(base.PointsOfInterest[i]);
		}
	}

	public override bool IsConcernedByEvent(Event gameEvent, Army army)
	{
		if (army == null || army.Empire == null)
		{
			return false;
		}
		EventTechnologyEnded eventTechnologyEnded = gameEvent as EventTechnologyEnded;
		if (eventTechnologyEnded != null && eventTechnologyEnded.Empire == army.Empire)
		{
			TechnologyDefinition technologyDefinition = eventTechnologyEnded.ConstructibleElement as TechnologyDefinition;
			if (technologyDefinition.Name == "TechnologyDefinitionMapActionArchaeology")
			{
				return true;
			}
		}
		EventInteractionComplete eventInteractionComplete = gameEvent as EventInteractionComplete;
		return (eventInteractionComplete != null && eventInteractionComplete.InstigatorGUID == army.GUID) || base.IsConcernedByEvent(gameEvent, army);
	}

	private void FilterPointOfInterests(Army army, ref bool available)
	{
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
		IQuestManagementService service2 = service.Game.Services.GetService<IQuestManagementService>();
		Diagnostics.Assert(service2 != null);
		IQuestRepositoryService service3 = service.Game.Services.GetService<IQuestRepositoryService>();
		Diagnostics.Assert(service2 != null);
		available = false;
		for (int i = base.PointsOfInterest.Count - 1; i >= 0; i--)
		{
			if (!this.CanSearch(army, base.PointsOfInterest[i], service2, service3, ref available))
			{
				base.PointsOfInterest.RemoveAt(i);
			}
		}
	}

	private bool CanSearch(Army army, PointOfInterest pointOfInterest, IQuestManagementService questManagementService, IQuestRepositoryService questRepositoryService, ref bool available)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service == null)
		{
			return false;
		}
		global::Game game = service.Game as global::Game;
		if (game == null)
		{
			return false;
		}
		if (pointOfInterest == null)
		{
			return false;
		}
		if (pointOfInterest.Type != ELCPUtilities.QuestLocation)
		{
			return false;
		}
		if (ELCPUtilities.UseELCPPeacefulCreepingNodes)
		{
			if (pointOfInterest.CreepingNodeGUID != GameEntityGUID.Zero && pointOfInterest.Empire != army.Empire)
			{
				if (pointOfInterest.Empire == null)
				{
					return false;
				}
				if (!(pointOfInterest.Empire is MajorEmpire))
				{
					return false;
				}
				DepartmentOfForeignAffairs agency = army.Empire.GetAgency<DepartmentOfForeignAffairs>();
				if (agency == null)
				{
					return false;
				}
				if (!agency.IsFriend(pointOfInterest.Empire))
				{
					return false;
				}
			}
		}
		else if (pointOfInterest.CreepingNodeGUID != GameEntityGUID.Zero && pointOfInterest.Empire != army.Empire)
		{
			return false;
		}
		bool flag = false;
		foreach (QuestMarker questMarker in questManagementService.GetMarkersByBoundTargetGUID(pointOfInterest.GUID))
		{
			Quest quest;
			if (!questMarker.IgnoreInteraction && questRepositoryService.TryGetValue(questMarker.QuestGUID, out quest) && quest.EmpireBits == army.Empire.Bits)
			{
				if (!quest.QuestDefinition.SkipLockedQuestTarget)
				{
					available = true;
					return true;
				}
				flag = true;
			}
		}
		if (pointOfInterest.UntappedDustDeposits && SimulationGlobal.GlobalTagsContains(SeasonManager.RuinDustDepositsTag))
		{
			return true;
		}
		if (pointOfInterest.Interaction.IsLocked(army.Empire.Index, this.Name))
		{
			return false;
		}
		IWorldPositionningService service2 = game.Services.GetService<IWorldPositionningService>();
		if (service2 != null && service2.IsWaterTile(pointOfInterest.WorldPosition))
		{
			return false;
		}
		global::Empire[] empires = game.Empires;
		for (int i = 0; i < empires.Length; i++)
		{
			using (IEnumerator<Army> enumerator2 = empires[i].GetAgency<DepartmentOfDefense>().Armies.GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					if (enumerator2.Current.WorldPosition == pointOfInterest.WorldPosition)
					{
						return false;
					}
				}
			}
		}
		if ((pointOfInterest.Interaction.Bits & army.Empire.Bits) == 0)
		{
			return true;
		}
		available = true;
		if (flag)
		{
			available = true;
			return true;
		}
		return false;
	}

	public static readonly StaticString POIAlreadySearched = "ArmyActionPOIAlreadySearched";
}
