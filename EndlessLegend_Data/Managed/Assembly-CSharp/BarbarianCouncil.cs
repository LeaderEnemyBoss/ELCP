using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Amplitude;
using Amplitude.Extensions;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class BarbarianCouncil : Agency, IXmlSerializable
{
	public BarbarianCouncil(global::Empire empire) : base(empire)
	{
	}

	public event EventHandler<VillageDissentEventArgs> OnVillageDissent;

	public override void ReadXml(XmlReader reader)
	{
		base.ReadXml(reader);
		int attribute = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("Villages");
		this.villages.Clear();
		for (int i = 0; i < attribute; i++)
		{
			ulong attribute2 = reader.GetAttribute<ulong>("GUID");
			Village village = new Village(attribute2)
			{
				Empire = (base.Empire as global::Empire)
			};
			reader.ReadElementSerializable<Village>(ref village);
			if (village != null)
			{
				this.AddVillage(village);
			}
		}
		reader.ReadEndElement("Villages");
	}

	public override void WriteXml(XmlWriter writer)
	{
		base.WriteXml(writer);
		writer.WriteStartElement("Villages");
		writer.WriteAttributeString<int>("Count", this.villages.Count);
		for (int i = 0; i < this.villages.Count; i++)
		{
			if (this.villages[i].HasBeenConverted)
			{
				Diagnostics.Assert(this.villages[i].HasBeenConvertedBy != null);
				Diagnostics.Assert(this.villages[i].HasBeenConvertedByIndex != -1);
				Diagnostics.Assert(this.villages[i].HasBeenConvertedByIndex == this.villages[i].HasBeenConvertedBy.Index);
				IXmlSerializable xmlSerializable = null;
				writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
			}
			else if (this.villages[i].IsInfectionComplete)
			{
				IXmlSerializable xmlSerializable = null;
				writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
			}
			else
			{
				IXmlSerializable xmlSerializable = this.villages[i];
				writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
			}
		}
		writer.WriteEndElement();
	}

	public MinorEmpire MinorEmpire
	{
		get
		{
			return base.Empire as MinorEmpire;
		}
	}

	public bool HasAllVillagesBeenPacified
	{
		get
		{
			for (int i = 0; i < this.villages.Count; i++)
			{
				if (!this.villages[i].HasBeenPacified)
				{
					return false;
				}
			}
			return true;
		}
	}

	public bool HasAtLeastOneVillagePacified
	{
		get
		{
			for (int i = 0; i < this.villages.Count; i++)
			{
				if (this.villages[i].HasBeenPacified)
				{
					return true;
				}
			}
			return false;
		}
	}

	public bool HasAtLeastOneNonInfectedVillagePacified
	{
		get
		{
			for (int i = 0; i < this.villages.Count; i++)
			{
				if (this.villages[i].HasBeenPacified && !this.villages[i].HasBeenInfected)
				{
					return true;
				}
			}
			return false;
		}
	}

	public bool IsAnyVillageActive
	{
		get
		{
			for (int i = 0; i < this.villages.Count; i++)
			{
				if (!this.villages[i].HasBeenPacified && !this.villages[i].HasBeenConverted)
				{
					return true;
				}
			}
			return false;
		}
	}

	public ReadOnlyCollection<Village> Villages
	{
		get
		{
			if (this.readOnlyVillages == null)
			{
				this.readOnlyVillages = this.villages.AsReadOnly();
			}
			return this.readOnlyVillages;
		}
	}

	private IEndTurnService EndTurnService { get; set; }

	private IEventService EventService { get; set; }

	private IGameService GameService { get; set; }

	private IGameEntityRepositoryService GameEntityRepositoryService { get; set; }

	private IDatabase<SimulationDescriptor> SimulationDescriptorDatabase { get; set; }

	public bool CanAttackVillage(Village village, global::Empire attacker)
	{
		return !village.HasBeenPacified;
	}

	public void CleanVillageAfterEncounter(Village village, Encounter encounter)
	{
		Diagnostics.Assert(this.villages.Contains(village));
		DepartmentOfDefense agency = base.Empire.GetAgency<DepartmentOfDefense>();
		Diagnostics.Assert(agency != null);
		agency.UpdateLifeAfterEncounter(village);
		agency.CleanGarrisonAfterEncounter(village);
		bool flag = true;
		foreach (Contender contender in encounter.GetAlliedContendersFromEmpire(village.Empire))
		{
			if (contender.IsTakingPartInBattle && contender.ContenderState != ContenderState.Defeated)
			{
				flag = false;
			}
		}
		if (village.Units.Count<Unit>() == 0 && flag)
		{
			if (village.PointOfInterest != null)
			{
				village.PointOfInterest.RemovePointOfInterestImprovement();
			}
			for (int i = 0; i < encounter.Empires.Count; i++)
			{
				if (encounter.Empires[i].Index != this.MinorEmpire.Index)
				{
					this.EventService.Notify(new EventVillageDestroyed(encounter.Empires[i], village));
				}
			}
			List<global::Empire> list = new List<global::Empire>(encounter.Empires);
			list.Remove(this.MinorEmpire);
			MajorEmpire converter = village.Converter;
			this.PacifyVillage(village, list);
			this.BindMinorFactionToCity();
			if (converter != null)
			{
				if (village.Region.City != null)
				{
					if (village.Region.City.Empire.Index == converter.Index)
					{
						DepartmentOfTheInterior agency2 = converter.GetAgency<DepartmentOfTheInterior>();
						Diagnostics.Assert(agency2 != null);
						if (agency2.MainCity != null)
						{
							agency2.BindMinorFactionToCity(agency2.MainCity, this.MinorEmpire);
						}
					}
					else
					{
						DepartmentOfTheInterior agency3 = converter.GetAgency<DepartmentOfTheInterior>();
						Diagnostics.Assert(agency3 != null);
						agency3.UnbindConvertedVillage(village);
					}
				}
				else
				{
					DepartmentOfTheInterior agency4 = converter.GetAgency<DepartmentOfTheInterior>();
					Diagnostics.Assert(agency4 != null);
					agency4.UnbindConvertedVillage(village);
				}
			}
		}
		village.Refresh(false);
	}

	public void ConvertVillage(Village village, MajorEmpire converter)
	{
		if (converter == null)
		{
			throw new ArgumentNullException("converter");
		}
		SimulationDescriptor value = this.SimulationDescriptorDatabase.GetValue(BarbarianCouncil.VillageStatusConverted);
		village.SwapDescriptor(value);
		if (!village.HasBeenConverted)
		{
			if (village.HasBeenPacified)
			{
				village.HasBeenPacified = false;
			}
			SimulationDescriptor value2 = this.SimulationDescriptorDatabase.GetValue(Village.ConvertedVillage);
			if (value2 != null)
			{
				Diagnostics.Assert(village.PointOfInterest != null);
				village.PointOfInterest.SwapDescriptor(value2);
				village.SwapDescriptor(value2);
			}
			for (int i = village.StandardUnits.Count - 1; i >= 0; i--)
			{
				Unit unit = village.StandardUnits[i];
				this.GameEntityRepositoryService.Unregister(unit);
				village.RemoveUnit(unit);
				unit.Dispose();
			}
			village.Converter = converter;
			village.ConvertedUnitSpawnTurn = (this.GameService.Game as global::Game).Turn + village.GetConvertedUnitProductionTimer();
			EventVillageConverted eventToNotify = new EventVillageConverted(converter, village);
			this.EventService.Notify(eventToNotify);
			if (village.Converter.ConvertedVillages != null)
			{
				Diagnostics.Assert(!village.Converter.ConvertedVillages.Contains(village));
				village.Converter.AddConvertedVillage(village);
			}
			DepartmentOfTheInterior.GenerateFIMSEForConvertedVillage(village.Converter, village.PointOfInterest);
		}
	}

	public void AddConvertVillageOnLoad(Village village, MajorEmpire converter)
	{
		if (converter == null)
		{
			throw new ArgumentNullException("converter");
		}
		SimulationDescriptor value = this.SimulationDescriptorDatabase.GetValue(BarbarianCouncil.VillageStatusConverted);
		village.SwapDescriptor(value);
		if (!village.HasBeenConverted)
		{
			if (village.HasBeenPacified)
			{
				village.HasBeenPacified = false;
			}
			SimulationDescriptor value2 = this.SimulationDescriptorDatabase.GetValue(Village.ConvertedVillage);
			if (value2 != null)
			{
				Diagnostics.Assert(village.PointOfInterest != null);
				village.PointOfInterest.SwapDescriptor(value2);
				village.SwapDescriptor(value2);
			}
			village.Converter = converter;
			if (village.Converter.ConvertedVillages != null && village.Converter.ConvertedVillages.Contains(village))
			{
				village.Converter.AddChild(village);
			}
			DepartmentOfTheInterior.GenerateFIMSEForConvertedVillage(village.Converter, village.PointOfInterest);
		}
	}

	public void DissentPacifiedVillage(Village village, global::Empire instigator)
	{
		if (!village.HasBeenPacified)
		{
			return;
		}
		village.RemoveDescriptorByName(BarbarianCouncil.VillageStatusPacified);
		village.RemoveDescriptorByName(Village.PacifiedVillage);
		village.PointOfInterest.RemoveDescriptorByName(Village.PacifiedVillage);
		if (!village.PointOfInterest.SimulationObject.Tags.Contains(Village.DissentedVillage))
		{
			IDatabase<SimulationDescriptor> database = Databases.GetDatabase<SimulationDescriptor>(true);
			SimulationDescriptor descriptor;
			if (database.TryGetValue(Village.DissentedVillage, out descriptor))
			{
				village.PointOfInterest.SwapDescriptor(descriptor);
			}
		}
		village.HasBeenPacified = false;
		village.Refresh(false);
		if (instigator != null)
		{
			EventVillageDissent eventToNotify = new EventVillageDissent(instigator, village);
			this.EventService.Notify(eventToNotify);
		}
		if (this.OnVillageDissent != null)
		{
			this.OnVillageDissent(this, new VillageDissentEventArgs(village));
		}
	}

	public Village GetVillageAt(WorldPosition worldPosition)
	{
		for (int i = 0; i < this.villages.Count; i++)
		{
			if (this.villages[i].WorldPosition == worldPosition)
			{
				return this.villages[i];
			}
		}
		return null;
	}

	public bool IsAnyVillagesDestroyed()
	{
		for (int i = 0; i < this.Villages.Count; i++)
		{
			if (this.IsVillageDestroyed(this.Villages[i]))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsVillageDestroyed(Village village)
	{
		return village.PointOfInterest.PointOfInterestImprovement == null || village.PointOfInterest.PointOfInterestImprovement.Name == this.destroyedVillagePointOfInterestImprovement.Name;
	}

	public void LoadRegionVillages(Region region)
	{
		if (region == null)
		{
			return;
		}
		GameEntityGUID guid = this.GameEntityRepositoryService.GenerateGUID();
		SimulationDescriptor descriptor = null;
		this.SimulationDescriptorDatabase.TryGetValue("ClassMinorEmpireGarrison", out descriptor);
		DepartmentOfIndustry agency = base.Empire.GetAgency<DepartmentOfIndustry>();
		SimulationDescriptor value = this.SimulationDescriptorDatabase.GetValue("MinorEmpireVillage");
		for (int i = 0; i < region.PointOfInterests.Length; i++)
		{
			string a;
			if (region.PointOfInterests[i].PointOfInterestDefinition.TryGetValue("Type", out a) && a == "Village")
			{
				guid = this.GameEntityRepositoryService.GenerateGUID();
				Village village = new Village(guid)
				{
					Empire = (base.Empire as global::Empire)
				};
				village.PointOfInterest = region.PointOfInterests[i];
				village.AddDescriptor(descriptor, false);
				village.AddDescriptor(value, false);
				this.AddVillage(village);
				if (agency != null)
				{
					agency.AddQueueTo<Village>(village);
				}
			}
		}
		this.MinorEmpire.GenerateStartingUnits();
	}

	public int NumberOfDestroyedVillages()
	{
		int num = 0;
		for (int i = 0; i < this.Villages.Count; i++)
		{
			if (this.IsVillageDestroyed(this.Villages[i]))
			{
				num++;
			}
		}
		return num;
	}

	public void PacifyRemainingVillages(IEnumerable<global::Empire> empiresWhichHelpedPacification = null, bool ignoreIfConverted = false)
	{
		bool flag = false;
		for (int i = 0; i < this.villages.Count; i++)
		{
			if (!this.villages[i].HasBeenPacified && (!ignoreIfConverted || !this.villages[i].HasBeenConverted))
			{
				this.PacifyVillage(this.villages[i], empiresWhichHelpedPacification);
				flag = true;
			}
		}
		if (flag)
		{
			this.BindMinorFactionToCity();
		}
	}

	public void PacifyVillage(Village village, IEnumerable<global::Empire> empiresWhichHelpedPacification = null)
	{
		SimulationDescriptor value = this.SimulationDescriptorDatabase.GetValue(BarbarianCouncil.VillageStatusPacified);
		village.SwapDescriptor(value);
		if (!village.HasBeenPacified)
		{
			village.HasBeenPacified = true;
			SimulationDescriptor value2 = this.SimulationDescriptorDatabase.GetValue(Village.PacifiedVillage);
			if (value2 != null)
			{
				Diagnostics.Assert(village.PointOfInterest != null);
				village.PointOfInterest.SwapDescriptor(value2);
				village.SwapDescriptor(value2);
			}
			for (int i = village.StandardUnits.Count - 1; i >= 0; i--)
			{
				Unit unit = village.StandardUnits[i];
				this.GameEntityRepositoryService.Unregister(unit);
				village.RemoveUnit(unit);
				unit.Dispose();
			}
			if (empiresWhichHelpedPacification != null)
			{
				foreach (global::Empire empire in empiresWhichHelpedPacification)
				{
					this.EventService.Notify(new EventVillagePacified(empire, village));
				}
			}
			if (village.Converter != null && village.Converter.ConvertedVillages != null)
			{
				DepartmentOfTheInterior.ClearFIMSEOnConvertedVillage(village.Converter, village.PointOfInterest);
				village.Converter.RemoveConvertedVillage(village);
				village.Converter = null;
			}
		}
	}

	public StaticString GetUpToDateDesignName()
	{
		int maxEraNumber = DepartmentOfScience.GetMaxEraNumber();
		StaticString tag = "Era" + maxEraNumber;
		foreach (UnitDesign unitDesign in this.departmentOfDefense.UnitDesignDatabase.GetAvailableUnitDesignsAsEnumerable())
		{
			if (!unitDesign.CheckAgainstTag(DownloadableContent16.SeafaringUnit) && !unitDesign.CheckAgainstTag("ConvertedVillageUnit") && !unitDesign.CheckAgainstTag("WildLiceArmy") && unitDesign.CheckAgainstTag(tag) && !unitDesign.Name.Contains("Mercenary"))
			{
				return unitDesign.Name;
			}
		}
		return StaticString.Empty;
	}

	public int GetCurrentUnitLevel()
	{
		int maxEraNumber = DepartmentOfScience.GetMaxEraNumber();
		float propertyValue = base.Empire.GetPropertyValue("MinorEmpireDifficultyFactor");
		return Mathf.FloorToInt((float)maxEraNumber * this.eraFactorForUnitLevel * propertyValue + (float)this.EndTurnService.Turn * this.turnFactorForUnitLevel * propertyValue);
	}

	public WorldPosition GetRandomValidArmyPosition(Village village)
	{
		WorldOrientation orientation = (WorldOrientation)this.random.Next(0, 6);
		WorldPosition result = WorldPosition.Invalid;
		for (int i = 0; i < 6; i++)
		{
			WorldOrientation direction = orientation.Rotate(i);
			WorldPosition neighbourg = this.worldPositionningService.GetNeighbourTile(village.WorldPosition, direction, 1);
			int regionIndex = (int)this.worldPositionningService.GetRegionIndex(neighbourg);
			if (regionIndex == village.Region.Index)
			{
				if (DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(neighbourg, PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.Water))
				{
					global::Empire[] empires = (this.GameService.Game as global::Game).Empires;
					int j = 0;
					while (j < empires.Length)
					{
						global::Empire empire = empires[j];
						if (!(empire is KaijuEmpire))
						{
							goto IL_106;
						}
						KaijuEmpire kaijuEmpire = empire as KaijuEmpire;
						if (kaijuEmpire == null)
						{
							goto IL_106;
						}
						KaijuCouncil agency = kaijuEmpire.GetAgency<KaijuCouncil>();
						if (agency == null || agency.Kaiju == null || !(agency.Kaiju.WorldPosition == neighbourg))
						{
							goto IL_106;
						}
						IL_145:
						j++;
						continue;
						IL_106:
						if (!(empire is MajorEmpire))
						{
							goto IL_145;
						}
						MajorEmpire majorEmpire = empire as MajorEmpire;
						if (majorEmpire == null || majorEmpire.TamedKaijus.Any((Kaiju m) => m.WorldPosition == neighbourg))
						{
							goto IL_145;
						}
						goto IL_145;
					}
					District district = this.worldPositionningService.GetDistrict(neighbourg);
					if (district == null || !District.IsACityTile(district))
					{
						result = neighbourg;
						break;
					}
				}
			}
		}
		return result;
	}

	internal void AddVillage(Village village)
	{
		if (village.Empire != null && village.Empire != base.Empire)
		{
			Diagnostics.LogError("The barbarian council was asked to add a village (guid: {0}, empire: {1}) but it is still bound to another empire.", new object[]
			{
				village.GUID,
				village.Empire.Name
			});
			return;
		}
		village.Empire = (global::Empire)base.Empire;
		IWorldPositionningService service = this.GameService.Game.Services.GetService<IWorldPositionningService>();
		Region region = service.GetRegion(village.WorldPosition);
		village.Region = region;
		int num = this.villages.BinarySearch((Village match) => match.GUID.CompareTo(village.GUID));
		if (num >= 0)
		{
			Diagnostics.LogWarning("The barbarian council was asked to add a village (guid #{0}) but it is already present in its list of villages.", new object[]
			{
				village.GUID
			});
			return;
		}
		this.villages.Insert(~num, village);
		if (village.HasBeenPacified)
		{
			this.PacifyVillage(village, null);
		}
		base.Empire.AddChild(village);
		if (village.HasBeenConverted)
		{
			foreach (Unit simulationObjectWrapper in village.Units)
			{
				village.PointOfInterest.AddChild(simulationObjectWrapper);
			}
		}
	}

	protected override IEnumerator OnInitialize()
	{
		this.SimulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
		Diagnostics.Assert(this.SimulationDescriptorDatabase != null);
		this.GameService = Services.GetService<IGameService>();
		this.EventService = Services.GetService<IEventService>();
		if (this.EventService == null)
		{
			Diagnostics.LogError("Failed to retrieve the event service.");
		}
		this.EndTurnService = Services.GetService<IEndTurnService>();
		if (this.EndTurnService == null)
		{
			Diagnostics.LogError("Failed to retrieve the end turn service.");
		}
		this.GameEntityRepositoryService = this.GameService.Game.Services.GetService<IGameEntityRepositoryService>();
		if (this.GameEntityRepositoryService == null)
		{
			Diagnostics.LogError("Failed to retrieve the game entity repository service.");
		}
		this.worldPositionningService = this.GameService.Game.Services.GetService<IWorldPositionningService>();
		if (this.worldPositionningService == null)
		{
			Diagnostics.LogError("Failed to retrieve the world positionning service.");
		}
		this.seasonService = this.GameService.Game.Services.GetService<ISeasonService>();
		if (this.seasonService == null)
		{
			Diagnostics.LogError("Failed to retrieve the season service.");
		}
		else
		{
			this.seasonService.SeasonChange += this.OnSeasonChange;
		}
		DepartmentOfIndustry industry = base.Empire.GetAgency<DepartmentOfIndustry>();
		if (!industry.ConstructibleElementDatabase.TryGetValue("DestroyedVillage", out this.destroyedVillagePointOfInterestImprovement))
		{
			Diagnostics.LogError("Failed to retrieve the destroyed village constructible.");
			yield break;
		}
		this.departmentOfDefense = base.Empire.GetAgency<DepartmentOfDefense>();
		base.Empire.RegisterPass("GameClientState_Turn_Begin", "ResetExperienceRewardOnUnits", new Agency.Action(this.GameClientState_Turn_Begin_ResetExperienceRewardOnUnits), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_End", "VillageUnitHealthPerTurnGain", new Agency.Action(this.GameClientState_Turn_End_UnitHealthPerTurnGain), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_End", "VillageResetUnitsActionPoints", new Agency.Action(this.GameClientState_Turn_End_UnitHealthPerTurnGain), new string[0]);
		yield return base.OnInitialize();
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		yield break;
	}

	protected override IEnumerator OnLoadGame(Amplitude.Unity.Game.Game game)
	{
		yield return base.OnLoadGame(game);
		for (int i = 0; i < this.villages.Count; i++)
		{
			Village village = this.villages[i];
			this.GameEntityRepositoryService.Register(village);
			foreach (Unit instance in village.Units)
			{
				this.GameEntityRepositoryService.Register(instance);
			}
			if (village.HasBeenConvertedByIndex >= 0 && village.HasBeenConvertedBy == null)
			{
				global::Game game2 = game as global::Game;
				Diagnostics.Assert(game2 != null);
				Diagnostics.Assert(game2.Empires != null);
				Diagnostics.Assert(village.HasBeenConvertedByIndex < game2.Empires.Length);
				village.HasBeenConvertedBy = (game2.Empires[village.HasBeenConvertedByIndex] as MajorEmpire);
				Diagnostics.Assert(village.Converter != null);
				Diagnostics.Assert(village.Converter.Index == village.HasBeenConvertedByIndex);
			}
		}
		yield break;
	}

	protected override void OnRelease()
	{
		base.OnRelease();
		for (int i = 0; i < this.villages.Count; i++)
		{
			this.villages[i].Dispose();
		}
		this.villages.Clear();
		this.GameEntityRepositoryService = null;
		this.GameService = null;
		this.SimulationDescriptorDatabase = null;
		if (this.seasonService != null)
		{
			this.seasonService.SeasonChange -= this.OnSeasonChange;
			this.seasonService = null;
		}
	}

	private void BindMinorFactionToCity()
	{
		if (this.MinorEmpire.Region.City != null)
		{
			DepartmentOfTheInterior agency = this.MinorEmpire.Region.City.Empire.GetAgency<DepartmentOfTheInterior>();
			agency.BindMinorFactionToCity(this.MinorEmpire.Region.City, this.MinorEmpire);
			agency.VerifyOverallPopulation(this.MinorEmpire.Region.City);
		}
	}

	private void RemoveVillage(Village village)
	{
		int num = this.villages.BinarySearch((Village match) => match.GUID.CompareTo(village.GUID));
		if (num < 0)
		{
			return;
		}
		village.Region = null;
		this.villages.RemoveAt(num);
		this.GameEntityRepositoryService.Unregister(village);
		base.Empire.RemoveChild(village);
		DepartmentOfIndustry agency = base.Empire.GetAgency<DepartmentOfIndustry>();
		if (agency != null)
		{
			agency.RemoveQueueFrom<Village>(village);
		}
	}

	private IEnumerator GameClientState_Turn_End_ResetUnitsActionPoints(string context, string name)
	{
		foreach (Village village in this.villages)
		{
			foreach (Unit unit in village.Units)
			{
				unit.UpdateExperienceReward(village.Empire);
			}
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_Begin_ResetExperienceRewardOnUnits(string context, string name)
	{
		foreach (Village village in this.villages)
		{
			foreach (Unit unit in village.Units)
			{
				unit.SetPropertyBaseValue(SimulationProperties.ActionPointsSpent, 0f);
			}
			village.Refresh(false);
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_UnitHealthPerTurnGain(string context, string name)
	{
		DepartmentOfDefense defense = base.Empire.GetAgency<DepartmentOfDefense>();
		for (int index = 0; index < this.villages.Count; index++)
		{
			float regenModifier = this.ComputeVillageRegenModifier(this.villages[index]);
			foreach (Unit unit in this.villages[index].Units)
			{
				DepartmentOfDefense.RegenUnit(unit, regenModifier, 0);
			}
			this.villages[index].Refresh(false);
			defense.CleanGarrisonAfterEncounter(this.villages[index]);
		}
		yield break;
	}

	private float ComputeVillageRegenModifier(Village village)
	{
		float propertyValue = village.Empire.GetPropertyValue(SimulationProperties.InOwnedRegionUnitRegenModifier);
		float propertyValue2 = village.GetPropertyValue(SimulationProperties.InGarrisonRegenModifier);
		return propertyValue + propertyValue2;
	}

	private void OnSeasonChange(object sender, SeasonChangeEventArgs e)
	{
		if (e.NewSeason.SeasonDefinition.SeasonType != Season.ReadOnlyHeatWave)
		{
			return;
		}
		global::Game game = this.GameService.Game as global::Game;
		if (game == null)
		{
			return;
		}
		List<int> empireIndexesOfFaction = game.GetEmpireIndexesOfFaction("FactionDrakkens");
		for (int i = 0; i < empireIndexesOfFaction.Count; i++)
		{
			for (int j = 0; j < this.villages.Count; j++)
			{
				if (this.villages[j].PointOfInterest != null)
				{
					this.villages[j].PointOfInterest.Interaction.RemoveInteractionLock(i, "ArmyActionReceiveDiplomacy");
				}
			}
		}
	}

	public static readonly StaticString VillageStatusConverted = "VillageStatusConverted";

	public static readonly StaticString VillageStatusPacified = "VillageStatusPacified";

	private List<Village> villages = new List<Village>();

	private ReadOnlyCollection<Village> readOnlyVillages;

	private DepartmentOfIndustry.ConstructibleElement destroyedVillagePointOfInterestImprovement;

	private DepartmentOfDefense departmentOfDefense;

	private float eraFactorForUnitLevel = 0.5f;

	private float turnFactorForUnitLevel;

	private System.Random random = new System.Random();

	private IWorldPositionningService worldPositionningService;

	private ISeasonService seasonService;
}
