using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

[ArmyActionWorldCursor(typeof(ArmyActionTargetSelectionWorldCursor))]
public class ArmyAction_Convert : ArmyAction_BaseVillage, IArmyActionWithTargetSelection
{
	public static ConstructionCost[] GetConvertionCost(MajorEmpire majorEmpire, Village village)
	{
		Diagnostics.Assert(majorEmpire != null);
		Diagnostics.Assert(village.PointOfInterest != null);
		float num;
		if (village.PointOfInterest.PointOfInterestImprovement == null)
		{
			num = village.GetPropertyValue(SimulationProperties.ConvertDestructedCost);
		}
		else if (!village.Region.IsRegionColonized() || village.Region.Owner == majorEmpire || village.Region.Owner == null || !(village.Region.Owner is MajorEmpire))
		{
			num = village.GetPropertyValue(SimulationProperties.ConvertNeutralCost);
		}
		else
		{
			num = village.GetPropertyValue(SimulationProperties.ConvertHostileCost);
		}
		num += num * (float)majorEmpire.ConvertedVillages.Count;
		return new ConstructionCost[]
		{
			new ConstructionCost(DepartmentOfTheTreasury.Resources.EmpirePoint, (float)Math.Floor((double)num), true, false)
		};
	}

	public override bool CanExecute(Army army, ref List<StaticString> failureFlags, params object[] parameters)
	{
		this.lastConvertCostDescription = string.Empty;
		if (!base.CanExecute(army, ref failureFlags, parameters))
		{
			return false;
		}
		if (army.IsNaval)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
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
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		if (service2 == null)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileSystemError);
			return false;
		}
		if (parameters == null)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		if (parameters.Length == 0)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		List<Village> list = new List<Village>();
		for (int i = 0; i < parameters.Length; i++)
		{
			if (parameters[i] is Village)
			{
				list.Add(parameters[i] as Village);
			}
			else if (parameters[i] is List<IGameEntity>)
			{
				List<IGameEntity> list2 = parameters[i] as List<IGameEntity>;
				for (int j = 0; j < list2.Count; j++)
				{
					if (list2[j] is Village)
					{
						list.Add(parameters[i] as Village);
					}
					else if (list2[j] is PointOfInterest)
					{
						PointOfInterest pointOfInterest = list2[j] as PointOfInterest;
						Region region = service2.GetRegion(pointOfInterest.WorldPosition);
						if (region != null && region.MinorEmpire != null)
						{
							BarbarianCouncil agency = region.MinorEmpire.GetAgency<BarbarianCouncil>();
							Village villageAt = agency.GetVillageAt(pointOfInterest.WorldPosition);
							if (villageAt != null)
							{
								list.Add(villageAt);
							}
						}
					}
				}
			}
		}
		if (list.Count == 0)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		if (!this.ArmyCanConvert(army, failureFlags))
		{
			return false;
		}
		DepartmentOfTheTreasury agency2 = army.Empire.GetAgency<DepartmentOfTheTreasury>();
		for (int k = list.Count - 1; k >= 0; k--)
		{
			if (!this.CanConvertVillage(army, list[k], agency2, failureFlags))
			{
				list.RemoveAt(k);
			}
		}
		if (list.Count == 0)
		{
			return false;
		}
		ArmyAction.FailureFlags.Clear();
		return true;
	}

	public override StaticString MouseCursorKey()
	{
		return ArmyAction_Convert.ActionMouseCursorKey;
	}

	public override bool MayExecuteSomewhereLater(Army army)
	{
		return base.MayExecuteSomewhereLater(army) && this.ArmyCanConvert(army, null);
	}

	public override void Execute(Army army, global::PlayerController playerController, out Ticket ticket, EventHandler<TicketRaisedEventArgs> ticketRaisedEventHandler, params object[] parameters)
	{
		ticket = null;
		PointOfInterest pointOfInterest = null;
		if (parameters != null && parameters.Length != 0 && parameters[0] is PointOfInterest)
		{
			pointOfInterest = (parameters[0] as PointOfInterest);
		}
		else if (parameters != null && parameters.Length != 0 && parameters[0] is Village)
		{
			pointOfInterest = (parameters[0] as Village).PointOfInterest;
		}
		DepartmentOfForeignAffairs agency = army.Empire.GetAgency<DepartmentOfForeignAffairs>();
		DiplomaticRelation diplomaticRelation = null;
		if (pointOfInterest != null && pointOfInterest.Empire != null)
		{
			diplomaticRelation = agency.GetDiplomaticRelation(pointOfInterest.Empire);
		}
		ArmyAction.FailureFlags.Clear();
		if (diplomaticRelation == null || diplomaticRelation.State == null || diplomaticRelation.State.Name != DiplomaticRelationState.Names.Alliance)
		{
			OrderConvertVillage orderConvertVillage = new OrderConvertVillage(army.Empire.Index, army.GUID, pointOfInterest.WorldPosition);
			orderConvertVillage.NumberOfActionPointsToSpend = base.GetCostInActionPoints();
			Diagnostics.Assert(playerController != null);
			playerController.PostOrder(orderConvertVillage, out ticket, ticketRaisedEventHandler);
		}
		else if (pointOfInterest != null && pointOfInterest.Empire != null && pointOfInterest.Empire is MajorEmpire)
		{
			IGuiService service = Services.GetService<IGuiService>();
			service.GetGuiPanel<WarDeclarationModalPanel>().Show(new object[]
			{
				pointOfInterest.Empire,
				"Convert"
			});
		}
	}

	public override string FormatDescription(string description)
	{
		description = AgeLocalizer.Instance.LocalizeString(description);
		if (!string.IsNullOrEmpty(this.lastConvertCostDescription))
		{
			description = string.Format(description, this.lastConvertCostDescription);
		}
		return description;
	}

	public void FillTargets(Army army, List<IGameEntity> gameEntities, ref List<StaticString> failureFlags)
	{
		if (!this.ArmyCanConvert(army, failureFlags))
		{
			return;
		}
		base.ListNearbyPointsOfInterestOfType(army, "Village");
		for (int i = 0; i < base.PointsOfInterest.Count; i++)
		{
			gameEntities.Add(base.PointsOfInterest[i]);
		}
	}

	public ConstructionCost[] GetConvertionCost(Army army, Village village)
	{
		if (army == null)
		{
			return null;
		}
		Diagnostics.Assert(army.Empire != null);
		Diagnostics.Assert(army.Empire is MajorEmpire);
		return ArmyAction_Convert.GetConvertionCost(army.Empire as MajorEmpire, village);
	}

	public override bool IsConcernedByResource(Army army, StaticString resourcePropertyName)
	{
		return army != null && army.Empire != null && this.ArmyCanConvert(army, null) && resourcePropertyName == SimulationProperties.EmpirePointStock;
	}

	private bool ArmyCanConvert(Army army, List<StaticString> failureFlags = null)
	{
		if (army.Empire == null || army.Empire.Faction == null)
		{
			if (failureFlags != null)
			{
				failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			}
			return false;
		}
		DepartmentOfScience agency = army.Empire.GetAgency<DepartmentOfScience>();
		bool flag = army.Empire.SimulationObject.Tags.Contains("FactionTraitCultists14");
		bool flag2 = agency.GetTechnologyState("TechnologyDefinitionCultists5") == DepartmentOfScience.ConstructibleElement.State.Researched;
		if (!flag && !flag2)
		{
			if (failureFlags != null)
			{
				failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			}
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
		if (!flag2 && flag)
		{
			if (!army.Units.Any((Unit unit) => !unit.SimulationObject.Tags.Contains("UnitFactionTypeMinorFaction") && !unit.SimulationObject.Tags.Contains(TradableUnit.ReadOnlyMercenary)))
			{
				if (failureFlags != null)
				{
					failureFlags.Add(ArmyAction_Convert.NoCanDoWhileMinorUnitsOnly);
				}
				return false;
			}
		}
		DepartmentOfTheInterior agency2 = army.Empire.GetAgency<DepartmentOfTheInterior>();
		if (agency2 != null && agency2.MainCity == null && agency2.Cities.Count < 1)
		{
			if (failureFlags != null)
			{
				failureFlags.Add(ArmyAction_Convert.NoCanDoWhileMainCityIsNotSettled);
			}
			return false;
		}
		return true;
	}

	private bool CanConvertVillage(Army army, Village village, DepartmentOfTheTreasury departmentOfTheTreasury, List<StaticString> failureFlags)
	{
		if (village == null)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileSystemError);
			return false;
		}
		if (village.HasBeenConverted)
		{
			failureFlags.Add(ArmyAction_Convert.NoCanDoWhileVillageIsAlreadyConverted);
			return false;
		}
		if (!village.HasBeenPacified)
		{
			failureFlags.Add(ArmyAction_BaseVillage.NoCanDoWhileVillageIsNotPacified);
			return false;
		}
		if (village.PointOfInterest.SimulationObject.Tags.Contains(DepartmentOfDefense.PillageStatusDescriptor))
		{
			failureFlags.Add(ArmyAction_Convert.NoCanDoWhileVillageIsPillaged);
			return false;
		}
		if (village.PointOfInterest.SimulationObject.Tags.Contains(DepartmentOfCreepingNodes.InfectedPointOfInterest))
		{
			failureFlags.Add(ArmyAction_Convert.NoCanDoWhileVillageIsInfected);
			return false;
		}
		ConstructionCost[] convertionCost = this.GetConvertionCost(army, village);
		if (!departmentOfTheTreasury.CanAfford(convertionCost))
		{
			failureFlags.Add(ArmyAction_Convert.NoCanDoWhileCannotAfford);
			this.lastConvertCostDescription = GuiFormater.FormatCost(army.Empire, convertionCost, false, 1, null);
			return false;
		}
		return true;
	}

	private Village GetVillageFromPointOfInterest(PointOfInterest pointOfInterest, IWorldPositionningService worldPositionningService = null)
	{
		if (worldPositionningService == null)
		{
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			Diagnostics.Assert(service.Game != null);
			worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
			Diagnostics.Assert(worldPositionningService != null);
		}
		Region region = worldPositionningService.GetRegion(pointOfInterest.WorldPosition);
		if (region != null && region.MinorEmpire != null)
		{
			BarbarianCouncil agency = region.MinorEmpire.GetAgency<BarbarianCouncil>();
			return agency.GetVillageAt(pointOfInterest.WorldPosition);
		}
		return null;
	}

	public static readonly StaticString ReadOnlyName = "ArmyActionConvert";

	public static readonly StaticString NoCanDoWhileCannotAfford = "ArmyActionCannotAffordConvert";

	public static readonly StaticString NoCanDoWhileVillageIsPillaged = "ArmyActionVillageIsPillaged";

	public static readonly StaticString NoCanDoWhileMainCityIsNotSettled = "ArmyActionMissingMainCity";

	public static readonly StaticString NoCanDoWhileVillageIsAlreadyConverted = "ArmyActionAlreadyConverted";

	public static readonly StaticString NoCanDoWhileMinorUnitsOnly = "ArmyActionNeedsMajorUnits";

	public static readonly StaticString NoCanDoWhileVillageIsInfected = "ArmyActionVillageIsInfected";

	private static readonly StaticString ActionMouseCursorKey = "HasArmyAction_Convert";

	private StaticString lastConvertCostDescription;
}
