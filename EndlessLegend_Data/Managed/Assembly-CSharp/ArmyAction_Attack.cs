using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

[ArmyActionWorldCursor(typeof(ArmyActionTargetSelectionWorldCursor))]
public class ArmyAction_Attack : ArmyAction_BaseVillage, IArmyActionWithTargetSelection
{
	public override bool CanExecute(Army army, ref List<StaticString> failureFlags, params object[] parameters)
	{
		if (!base.CanExecute(army, ref failureFlags, parameters))
		{
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
		bool flag = false;
		for (int i = 0; i < parameters.Length; i++)
		{
			if (parameters[i] is IGameEntity)
			{
				if (this.CanAttackGameEntity(army, parameters[i] as IGameEntity, ref flag, ref failureFlags))
				{
					if (!base.CheckActionPointsPrerequisites(army))
					{
						failureFlags.Add(ArmyAction.NoCanDoWhileNotEnoughActionPointsLeft);
						return false;
					}
					return true;
				}
			}
			else if (parameters[i] is List<IGameEntity>)
			{
				List<IGameEntity> list = parameters[i] as List<IGameEntity>;
				int j = 0;
				while (j < list.Count)
				{
					if (this.CanAttackGameEntity(army, list[j], ref flag, ref failureFlags))
					{
						if (!base.CheckActionPointsPrerequisites(army))
						{
							failureFlags.Add(ArmyAction.NoCanDoWhileNotEnoughActionPointsLeft);
							return false;
						}
						return true;
					}
					else
					{
						j++;
					}
				}
			}
		}
		if (!flag)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
		}
		return false;
	}

	public override void Execute(Army army, global::PlayerController playerController, out Ticket ticket, EventHandler<TicketRaisedEventArgs> ticketRaisedEventHandler, params object[] parameters)
	{
		ticket = null;
		IGameEntity gameEntity = null;
		ArmyAction.FailureFlags.Clear();
		for (int i = 0; i < parameters.Length; i++)
		{
			if (parameters[i] is IGameEntity)
			{
				gameEntity = (parameters[i] as IGameEntity);
				break;
			}
			if (parameters[i] is List<IGameEntity>)
			{
				List<IGameEntity> list = parameters[i] as List<IGameEntity>;
				bool flag = false;
				for (int j = 0; j < list.Count; j++)
				{
					if (this.CanAttackGameEntity(army, list[j], ref flag, ref ArmyAction.FailureFlags))
					{
						gameEntity = list[j];
						break;
					}
				}
				break;
			}
		}
		if (gameEntity != null)
		{
			DepartmentOfForeignAffairs agency = army.Empire.GetAgency<DepartmentOfForeignAffairs>();
			Diagnostics.Assert(agency != null);
			if (agency.CanAttack(gameEntity) || army.IsPrivateers)
			{
				OrderAttack orderAttack = new OrderAttack(army.Empire.Index, army.GUID, gameEntity.GUID);
				orderAttack.NumberOfActionPointsToSpend = base.GetCostInActionPoints();
				Diagnostics.Assert(playerController != null);
				playerController.PostOrder(orderAttack, out ticket, ticketRaisedEventHandler);
			}
		}
	}

	public void FillTargets(Army army, List<IGameEntity> targets, ref List<StaticString> failureFlags)
	{
		if (targets == null)
		{
			targets = new List<IGameEntity>();
		}
		IGameService service = Services.GetService<IGameService>();
		if (service == null)
		{
			return;
		}
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(service2 != null);
		IPathfindingService service3 = service.Game.Services.GetService<IPathfindingService>();
		Diagnostics.Assert(service2 != null);
		base.ListNearbyVillages(army);
		if (base.PointsOfInterest.Count != 0)
		{
			for (int i = base.PointsOfInterest.Count - 1; i >= 0; i--)
			{
				PointOfInterest pointOfInterest = base.PointsOfInterest[i];
				if (pointOfInterest.PointOfInterestImprovement != null && (pointOfInterest.Empire == null || pointOfInterest.Empire.Index != army.Empire.Index))
				{
					Region region = service2.GetRegion(pointOfInterest.WorldPosition);
					if (region != null && region.MinorEmpire != null)
					{
						Village village = region.MinorEmpire.GetAgency<BarbarianCouncil>().Villages.FirstOrDefault((Village iterator) => iterator.WorldPosition == pointOfInterest.WorldPosition);
						if (village != null)
						{
							targets.Add(village);
						}
					}
				}
			}
		}
		List<WorldPosition> list = new List<WorldPosition>();
		for (int j = 0; j < 6; j++)
		{
			WorldPosition neighbourTile = service2.GetNeighbourTile(army.WorldPosition, (WorldOrientation)j, 1);
			if (neighbourTile.IsValid && service3.IsTransitionPassable(army.WorldPosition, neighbourTile, army, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnoreZoneOfControl | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreKaijuGarrisons, null))
			{
				list.Add(neighbourTile);
			}
		}
		List<int> list2 = new List<int>();
		for (int k = 0; k < list.Count; k++)
		{
			Region region2 = service2.GetRegion(list[k]);
			if (region2 != null && !list2.Contains(region2.Index))
			{
				list2.Add(region2.Index);
				KaijuEmpire kaijuEmpire = region2.KaijuEmpire;
				if (kaijuEmpire != null)
				{
					KaijuCouncil agency = kaijuEmpire.GetAgency<KaijuCouncil>();
					if (agency != null)
					{
						Kaiju kaiju = agency.Kaiju;
						if (kaiju != null && kaiju.OnGarrisonMode())
						{
							KaijuGarrison kaijuGarrison = kaiju.KaijuGarrison;
							if (kaijuGarrison != null && list.Contains(kaijuGarrison.WorldPosition))
							{
								targets.Add(kaijuGarrison);
							}
						}
					}
				}
				if (region2.City != null && region2.City.Empire != null && region2.City.Empire.Index != army.Empire.Index)
				{
					for (int l = 0; l < region2.City.Districts.Count; l++)
					{
						District district = region2.City.Districts[l];
						if (list.Contains(district.WorldPosition) && district.Type != DistrictType.Exploitation)
						{
							targets.Add(district);
						}
					}
					if (region2.City.Camp != null && list.Contains(region2.City.Camp.WorldPosition))
					{
						targets.Add(region2.City.Camp);
					}
				}
			}
		}
		global::Game game = service.Game as global::Game;
		if (game == null)
		{
			return;
		}
		for (int m = 0; m < game.Empires.Length; m++)
		{
			if (m != army.Empire.Index)
			{
				DepartmentOfDefense agency2 = game.Empires[m].GetAgency<DepartmentOfDefense>();
				if (agency2 != null)
				{
					for (int n = 0; n < agency2.Armies.Count; n++)
					{
						Army army2 = agency2.Armies[n];
						if (list.Contains(army2.WorldPosition))
						{
							if (army2 is KaijuArmy)
							{
								KaijuArmy kaijuArmy = army2 as KaijuArmy;
								if (kaijuArmy != null && !kaijuArmy.Kaiju.OnArmyMode())
								{
									goto IL_389;
								}
							}
							targets.Add(army2);
						}
						IL_389:;
					}
				}
				DepartmentOfTheInterior agency3 = game.Empires[m].GetAgency<DepartmentOfTheInterior>();
				if (agency3 != null)
				{
					for (int num = 0; num < agency3.TamedKaijuGarrisons.Count; num++)
					{
						KaijuGarrison kaijuGarrison2 = agency3.TamedKaijuGarrisons[num];
						if (kaijuGarrison2 != null)
						{
							Kaiju kaiju2 = kaijuGarrison2.Kaiju;
							if (kaiju2 != null && kaiju2.OnGarrisonMode() && list.Contains(kaijuGarrison2.WorldPosition))
							{
								targets.Add(kaijuGarrison2);
							}
						}
					}
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
		EventDiplomaticRelationStateChange eventDiplomaticRelationStateChange = gameEvent as EventDiplomaticRelationStateChange;
		if (eventDiplomaticRelationStateChange != null && eventDiplomaticRelationStateChange.Empire == army.Empire && eventDiplomaticRelationStateChange.DiplomaticRelationStateName != DiplomaticRelationState.Names.Unknown)
		{
			return true;
		}
		EventDiplomaticContractStateChange eventDiplomaticContractStateChange = gameEvent as EventDiplomaticContractStateChange;
		if (eventDiplomaticContractStateChange != null && eventDiplomaticContractStateChange.DiplomaticContract != null && (eventDiplomaticContractStateChange.DiplomaticContract.EmpireWhichProposes == army.Empire || eventDiplomaticContractStateChange.DiplomaticContract.EmpireWhichReceives == army.Empire) && eventDiplomaticContractStateChange.DiplomaticContract.State == DiplomaticContractState.Signed)
		{
			return true;
		}
		EventSwapCity eventSwapCity = gameEvent as EventSwapCity;
		return (eventSwapCity != null && eventSwapCity.Empire == army.Empire) || base.IsConcernedByEvent(gameEvent, army);
	}

	private bool CanAttackGameEntity(Army army, IGameEntity gameEntity, ref bool displayButtonAnyway, ref List<StaticString> failureFlags)
	{
		Village village = gameEntity as Village;
		if (village != null)
		{
			displayButtonAnyway = true;
			if (!TutorialManager.IsActivated)
			{
				if (village.PointOfInterest.Interaction.IsLocked(army.Empire.Index, this.Name))
				{
					if (!failureFlags.Contains(ArmyAction_Attack.OneVillageOrManyLocked))
					{
						failureFlags.Add(ArmyAction_Attack.OneVillageOrManyLocked);
					}
					return false;
				}
			}
			else
			{
				failureFlags.Add(ArmyAction.NoCanDoWhileTutorial);
			}
			if (village.HasBeenConverted)
			{
				if (village.Converter == army.Empire)
				{
					failureFlags.Add(ArmyAction_Attack.OwnVillage);
					return false;
				}
				return true;
			}
			else
			{
				if (village.HasBeenPacified)
				{
					failureFlags.Add(ArmyAction_BaseVillage.NoCanDoWhileVillageIsPacified);
					return false;
				}
				return true;
			}
		}
		else
		{
			Camp camp = gameEntity as Camp;
			if (camp != null)
			{
				if (camp.Empire == army.Empire)
				{
					return false;
				}
				DepartmentOfForeignAffairs agency = army.Empire.GetAgency<DepartmentOfForeignAffairs>();
				if (!agency.CanAttack(gameEntity) && !army.IsPrivateers && DepartmentOfTheInterior.CanNeverDeclareWar(army.Empire))
				{
					failureFlags.Add(ArmyAction_Attack.CanNeverDeclareWar);
					return false;
				}
				return true;
			}
			else
			{
				District district = gameEntity as District;
				if (district != null)
				{
					if (district.City.Empire == army.Empire)
					{
						return false;
					}
					displayButtonAnyway = true;
					if (district.City.BesiegingEmpire != null && district.City.BesiegingEmpire != army.Empire)
					{
						return false;
					}
					if (district.Type == DistrictType.Exploitation)
					{
						return false;
					}
					DepartmentOfForeignAffairs agency2 = army.Empire.GetAgency<DepartmentOfForeignAffairs>();
					Diagnostics.Assert(agency2 != null);
					if (!agency2.CanAttack(gameEntity) && !army.IsPrivateers && DepartmentOfTheInterior.CanNeverDeclareWar(army.Empire))
					{
						failureFlags.Add(ArmyAction_Attack.CanNeverDeclareWar);
						return false;
					}
					return true;
				}
				else
				{
					Army army2 = gameEntity as Army;
					if (army2 == null)
					{
						KaijuGarrison kaijuGarrison = gameEntity as KaijuGarrison;
						return kaijuGarrison != null && (kaijuGarrison.Owner == null || kaijuGarrison.Owner.Index != army.Empire.Index) && !kaijuGarrison.Kaiju.IsStunned();
					}
					if (army2.Empire == army.Empire)
					{
						return false;
					}
					if (army2.Sails)
					{
						return false;
					}
					if (army2 is KaijuArmy)
					{
						KaijuArmy kaijuArmy = army2 as KaijuArmy;
						if (kaijuArmy != null && kaijuArmy.Kaiju.IsStunned())
						{
							return false;
						}
					}
					DepartmentOfForeignAffairs agency3 = army.Empire.GetAgency<DepartmentOfForeignAffairs>();
					Diagnostics.Assert(agency3 != null);
					if (!agency3.CanAttack(gameEntity) && !army.IsPrivateers && DepartmentOfTheInterior.CanNeverDeclareWar(army.Empire))
					{
						failureFlags.Add(ArmyAction_Attack.CanNeverDeclareWar);
						return false;
					}
					displayButtonAnyway = true;
					return true;
				}
			}
		}
	}

	public static readonly StaticString ReadOnlyName = "ArmyActionAttack";

	public static readonly StaticString CannotAttack = "ArmyActionCannotAttack";

	public static readonly StaticString OneVillageOrManyLocked = "OneVillageOrManyLocked";

	public static readonly StaticString InvalidDiplomaticAbilities = "ArmyActionAttackInvalidDiplomaticAbilities";

	public static readonly StaticString OwnVillage = "ArmyActionAttackInvalidOwnVillage";

	public static readonly StaticString CanNeverDeclareWar = "ArmyActionAttackCanNeverDeclareWar";
}
