using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation;
using Amplitude.Utilities.Maps;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class City : Garrison, Amplitude.Xml.Serialization.IXmlSerializable, IGarrison, IRegionalEffectsConcerned<IRegionalEffectsConcernedGameEntity>, IGarrisonWithPosition, IFastTravelNode, IFastTravelNodeGameEntity, IGameEntity, IGameEntityWithEmpire, IGameEntityWithWorldPosition, IRegionalEffectsConcernedGameEntity, IWorldPositionable, ICategoryProvider, IDescriptorEffectProvider, IPropertyEffectFeatureProvider
{
	public City(GameEntityGUID guid) : base("City#" + guid)
	{
		this.guid = guid;
		this.AdministrationSpeciality = StaticString.Empty;
		this.gameService = Services.GetService<IGameService>();
		this.BesiegingEmpireIndex = -1;
		this.CadastralMap = new CadastralMap();
		this.TradeRoutes = new List<TradeRoute>(4);
		this.Ownership = new float[8];
		this.DryDockPosition = WorldPosition.Invalid;
		this.cityImprovementsReadOnlyCollection = this.cityImprovements.AsReadOnly();
		this.districtsReadOnlyCollection = this.districts.AsReadOnly();
		for (int i = 0; i < this.Ownership.Length; i++)
		{
			this.Ownership[i] = 0f;
		}
	}

	public event City.OnCityCampChangedDelegate OnCityCampChanged;

	public event Action<IRegionalEffectsConcernedGameEntity> RefreshAppliedRegionEffects;

	public event EventHandler CitySiegeChange;

	public event City.OnCityDisposedDelegate OnCityDisposed;

	public event EventHandler AdministrationSpecialityChange;

	public event CollectionChangeEventHandler CityDistrictCollectionChange;

	public event CollectionChangeEventHandler CityImprovementCollectionChange;

	public event EventHandler CityOwnerChange;

	public event EventHandler CityCadastralChange;

	StaticString ICategoryProvider.Category
	{
		get
		{
			return City.ReadOnlyCategory;
		}
	}

	StaticString ICategoryProvider.SubCategory
	{
		get
		{
			return StaticString.Empty;
		}
	}

	SimulationObject IPropertyEffectFeatureProvider.GetSimulationObject()
	{
		return base.SimulationObject;
	}

	IEnumerable<SimulationDescriptor> IDescriptorEffectProvider.GetDescriptors()
	{
		foreach (SimulationDescriptorHolder holder in base.SimulationObject.DescriptorHolders)
		{
			if (!(holder.Descriptor.Name == "ClassCity"))
			{
				yield return holder.Descriptor;
			}
		}
		yield break;
	}

	public Camp Camp
	{
		get
		{
			return this.camp;
		}
		set
		{
			if (this.camp != null)
			{
				this.Empire.RemoveChild(this.camp);
				this.Refresh(false);
			}
			this.camp = value;
			if (value != null)
			{
				this.Empire.AddChild(value);
				this.Refresh(false);
			}
			if (this.OnCityCampChanged != null)
			{
				this.OnCityCampChanged();
			}
		}
	}

	public bool IsUnderEarthquake
	{
		get
		{
			return DepartmentOfTheInterior.GetCityEarthquakeInstigators(this).Length > 0;
		}
	}

	public override void CleanAfterEncounter(Encounter encounter)
	{
		base.CleanAfterEncounter(encounter);
	}

	public int TravelAllowedBitsMask
	{
		get
		{
			if (this.Empire != null)
			{
				return this.Empire.Bits;
			}
			return 0;
		}
	}

	public WorldPosition[] GetTravelEntrancePositions()
	{
		List<WorldPosition> list = new List<WorldPosition>();
		for (int i = 0; i < this.Districts.Count; i++)
		{
			District district = this.Districts[i];
			if (district.Type != DistrictType.Camp && district.Type != DistrictType.Exploitation)
			{
				list.Add(district.WorldPosition);
			}
		}
		return list.ToArray();
	}

	public WorldPosition[] GetTravelExitPositions()
	{
		DepartmentOfTransportation agency = this.Empire.GetAgency<DepartmentOfTransportation>();
		WorldPosition invalid = WorldPosition.Invalid;
		if (agency.TryGetFirstCityTileAvailableForTeleport(this, out invalid))
		{
			return new WorldPosition[]
			{
				invalid
			};
		}
		return new WorldPosition[0];
	}

	public SimulationObject GetTravelNodeContext()
	{
		return this;
	}

	public override void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		this.guid = reader.GetAttribute<ulong>("GUID");
		this.userDefinedName = reader.GetAttribute("UserDefinedString");
		this.TurnWhenToProceedWithRazing = reader.GetAttribute<int>("TurnWhenToProceedWithRazing");
		this.ShouldRazeRegionBuildingWithSelf = reader.GetAttribute<bool>("ShouldRazeRegionBuildingWithSelf");
		this.ShouldInjureSpyOnRaze = reader.GetAttribute<bool>("ShouldInjureSpyOnRaze");
		this.AdministrationSpeciality = reader.GetAttribute<string>("AdministrationSpeciality");
		if (!StaticString.IsNullOrEmpty(this.AdministrationSpeciality))
		{
			IDatabase<AICityState> database = Databases.GetDatabase<AICityState>(false);
			AICityState aicityState;
			if (!database.TryGetValue(this.AdministrationSpeciality, out aicityState) || !aicityState.IsGuiCompliant)
			{
				this.AdministrationSpeciality = StaticString.Empty;
			}
		}
		base.ReadXml(reader);
		int attribute = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("Districts");
		this.districts.Clear();
		for (int i = 0; i < attribute; i++)
		{
			ulong attribute2 = reader.GetAttribute<ulong>("GUID");
			District district = new District(attribute2);
			reader.ReadElementSerializable<District>(ref district);
			if (district != null)
			{
				this.AddDistrict(district);
			}
		}
		reader.ReadEndElement("Districts");
		if (num >= 2)
		{
			if (reader.IsNullElement())
			{
				reader.Skip();
			}
			else
			{
				GameEntityGUID gameEntityGUID = reader.GetAttribute<ulong>("GUID");
				Militia militia = new Militia(gameEntityGUID);
				militia.Empire = this.Empire;
				reader.ReadElementSerializable<Militia>(ref militia);
				this.Militia = militia;
				if (this.Militia != null)
				{
					base.AddChild(this.Militia);
				}
			}
		}
		int attribute3 = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("Improvements");
		this.cityImprovements.Clear();
		for (int j = 0; j < attribute3; j++)
		{
			ulong attribute4 = reader.GetAttribute<ulong>("GUID");
			CityImprovement cityImprovement = new CityImprovement(attribute4);
			reader.ReadElementSerializable<CityImprovement>(ref cityImprovement);
			if (cityImprovement != null)
			{
				this.AddCityImprovement(cityImprovement, false);
			}
		}
		reader.ReadEndElement("Improvements");
		this.WorldPosition = reader.ReadElementSerializable<WorldPosition>();
		if (num >= 2 && this.Militia != null)
		{
			this.Militia.WorldPosition = this.WorldPosition;
		}
		if (reader.IsStartElement("BesiegingEmpireIndex"))
		{
			int num2 = reader.ReadElementString<int>("BesiegingEmpireIndex");
			this.BesiegingEmpireIndex = num2;
		}
		if (num >= 3)
		{
			if (reader.IsNullElement())
			{
				reader.Skip();
			}
			else
			{
				CadastralMap cadastralMap = new CadastralMap();
				reader.ReadElementSerializable<CadastralMap>(ref cadastralMap);
				this.CadastralMap = cadastralMap;
			}
		}
		if (num >= 4)
		{
			this.TradeRoutes.Clear();
			int attribute5 = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("TradeRoutes");
			for (int k = 0; k < attribute5; k++)
			{
				TradeRoute item = new TradeRoute();
				reader.ReadElementSerializable<TradeRoute>(ref item);
				this.TradeRoutes.Add(item);
			}
			base.SetPropertyBaseValue(SimulationProperties.IntermediateTradeRoutesCount, 0f);
			base.SetPropertyBaseValue(SimulationProperties.IntermediateTradeRoutesDistance, 0f);
			base.SetPropertyBaseValue(SimulationProperties.IntermediateTradeRoutesGain, 0f);
			reader.ReadEndElement("TradeRoutes");
		}
		if (num >= 5)
		{
			int attribute6 = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("Ownerships");
			for (int l = 0; l < attribute6; l++)
			{
				this.Ownership[l] = reader.ReadElementString<float>("Ownership");
			}
			reader.ReadEndElement("Ownerships");
		}
		if (num >= 6)
		{
			this.DryDockPosition = reader.ReadElementSerializable<WorldPosition>();
		}
		if (num >= 7)
		{
			ulong attribute7 = reader.GetAttribute<ulong>(Camp.SerializableNames.CampGUID);
			Camp camp = new Camp(attribute7)
			{
				Empire = this.Empire
			};
			reader.ReadElementSerializable<Camp>(ref camp);
			if (camp != null)
			{
				for (int m = 0; m < camp.Districts.Count; m++)
				{
					camp.Districts[m].City = this;
					base.AddChild(camp.Districts[m]);
					camp.Districts[m].Refresh(false);
				}
				this.Camp = camp;
			}
		}
		if (num >= 8)
		{
			this.lastNonInfectedOwnerIndex = reader.ReadElementString<int>("LastNonInfectedOwnerIndex");
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		int num = writer.WriteVersionAttribute(8);
		writer.WriteAttributeString<GameEntityGUID>("GUID", this.GUID);
		writer.WriteAttributeString("UserDefinedString", this.userDefinedName);
		if (this.TurnWhenToProceedWithRazing != 0)
		{
			writer.WriteAttributeString<int>("TurnWhenToProceedWithRazing", this.TurnWhenToProceedWithRazing);
		}
		writer.WriteAttributeString<bool>("ShouldRazeRegionBuildingWithSelf", this.ShouldRazeRegionBuildingWithSelf);
		writer.WriteAttributeString<bool>("ShouldInjureSpyOnRaze", this.ShouldInjureSpyOnRaze);
		writer.WriteAttributeString<StaticString>("AdministrationSpeciality", this.AdministrationSpeciality);
		base.WriteXml(writer);
		writer.WriteStartElement("Districts");
		writer.WriteAttributeString("Count", this.districts.Count.ToString());
		for (int i = 0; i < this.districts.Count; i++)
		{
			Amplitude.Xml.Serialization.IXmlSerializable xmlSerializable = this.districts[i];
			writer.WriteElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>(ref xmlSerializable);
		}
		writer.WriteEndElement();
		if (num >= 2)
		{
			Amplitude.Xml.Serialization.IXmlSerializable militia = this.Militia;
			writer.WriteElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>(ref militia);
		}
		writer.WriteStartElement("Improvements");
		writer.WriteAttributeString("Count", this.cityImprovements.Count.ToString());
		for (int j = 0; j < this.cityImprovements.Count; j++)
		{
			Amplitude.Xml.Serialization.IXmlSerializable xmlSerializable2 = this.cityImprovements[j];
			writer.WriteElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>(ref xmlSerializable2);
		}
		writer.WriteEndElement();
		Amplitude.Xml.Serialization.IXmlSerializable xmlSerializable3 = this.WorldPosition;
		writer.WriteElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>(ref xmlSerializable3);
		if (this.BesiegingEmpire != null)
		{
			writer.WriteElementString<int>("BesiegingEmpireIndex", this.besiegingEmpireIndex);
		}
		if (num >= 3)
		{
			Amplitude.Xml.Serialization.IXmlSerializable cadastralMap = this.CadastralMap;
			writer.WriteElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>(ref cadastralMap);
		}
		if (num >= 4)
		{
			writer.WriteStartElement("TradeRoutes");
			writer.WriteAttributeString<int>("Count", this.TradeRoutes.Count);
			for (int k = 0; k < this.TradeRoutes.Count; k++)
			{
				Amplitude.Xml.Serialization.IXmlSerializable xmlSerializable4 = this.TradeRoutes[k];
				writer.WriteElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>(ref xmlSerializable4);
			}
			writer.WriteEndElement();
		}
		if (num >= 5)
		{
			writer.WriteStartElement("Ownerships");
			writer.WriteAttributeString<int>("Count", this.Ownership.Length);
			for (int l = 0; l < this.Ownership.Length; l++)
			{
				writer.WriteElementString<float>("Ownership", this.Ownership[l]);
			}
			writer.WriteEndElement();
		}
		if (num >= 6)
		{
			Amplitude.Xml.Serialization.IXmlSerializable xmlSerializable5 = this.DryDockPosition;
			writer.WriteElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>(ref xmlSerializable5);
		}
		if (num >= 7)
		{
			Amplitude.Xml.Serialization.IXmlSerializable xmlSerializable6 = this.Camp;
			writer.WriteElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>(ref xmlSerializable6);
		}
		if (num >= 8)
		{
			writer.WriteElementString<int>("LastNonInfectedOwnerIndex", this.lastNonInfectedOwnerIndex);
		}
	}

	public void AddRegionalEffect(RegionalEffect effect)
	{
		this.regionalEffects.Add(effect);
		for (int i = 0; i < effect.Definition.Descriptors.Length; i++)
		{
			base.AddDescriptor(effect.Definition.Descriptors[i], true);
		}
		this.Refresh(false);
		IEventService service = Services.GetService<IEventService>();
		if (service != null)
		{
			IGameEntityRepositoryService service2 = this.gameService.Game.Services.GetService<IGameEntityRepositoryService>();
			if (service2 != null)
			{
				Kaiju kaiju = null;
				service2.TryGetValue<Kaiju>(effect.OwnerGUID, out kaiju);
				if (kaiju != null)
				{
					EventCityAddRegionalEffects eventToNotify = new EventCityAddRegionalEffects(this.Empire, this, kaiju);
					service.Notify(eventToNotify);
				}
			}
		}
	}

	public void CallRefreshAppliedRegionEffects()
	{
		if (this.RefreshAppliedRegionEffects != null)
		{
			this.RefreshAppliedRegionEffects(this);
		}
	}

	public void ClearRegionalEffects()
	{
		for (int i = 0; i < this.regionalEffects.Count; i++)
		{
			RegionalEffect regionalEffect = this.regionalEffects[i];
			for (int j = 0; j < regionalEffect.Definition.Descriptors.Length; j++)
			{
				base.RemoveDescriptor(regionalEffect.Definition.Descriptors[j]);
			}
		}
		this.regionalEffects.Clear();
	}

	public IEnumerable<RegionalEffect> GetRegionalEffects()
	{
		return this.regionalEffects;
	}

	public SimulationObjectWrapper GetRegionAffectableContext()
	{
		return this;
	}

	public int BesiegingEmpireIndex
	{
		get
		{
			return this.besiegingEmpireIndex;
		}
		set
		{
			if (this.besiegingEmpireIndex != value)
			{
				this.besiegingEmpireIndex = value;
				this.OnCitySiegeChange();
			}
		}
	}

	public global::Empire BesiegingEmpire
	{
		get
		{
			if (this.BesiegingEmpireIndex >= 0 && this.besiegingEmpire == null)
			{
				global::Game game = this.gameService.Game as global::Game;
				if (game != null)
				{
					this.besiegingEmpire = game.Empires[this.BesiegingEmpireIndex];
				}
			}
			else if (this.BesiegingEmpireIndex < 0)
			{
				this.besiegingEmpire = null;
			}
			return this.besiegingEmpire;
		}
	}

	public List<Army> BesiegingSeafaringArmies
	{
		get
		{
			return this.besiegingSeafaringArmies;
		}
	}

	internal void OnCitySiegeChange()
	{
		if (this.CitySiegeChange != null)
		{
			this.CitySiegeChange(this, new EventArgs());
		}
	}

	public StaticString AdministrationSpeciality { get; set; }

	public CadastralMap CadastralMap { get; private set; }

	public override global::Empire Empire
	{
		get
		{
			return base.Empire;
		}
		set
		{
			if (base.Empire != null)
			{
				this.EmpireInfiltrationBits &= ~(1 << base.Empire.Index);
			}
			base.Empire = value;
			if (value != null)
			{
				this.EmpireInfiltrationBits |= 1 << value.Index;
			}
		}
	}

	public global::Empire LastNonInfectedOwner
	{
		get
		{
			if (this.lastNonInfectedOwnerIndex != -1 && this.lastNonInfectedOwner == null)
			{
				global::Game game = this.gameService.Game as global::Game;
				this.lastNonInfectedOwner = game.GetEmpireByIndex(this.lastNonInfectedOwnerIndex);
			}
			return this.lastNonInfectedOwner;
		}
		set
		{
			this.lastNonInfectedOwnerIndex = ((value != null) ? value.Index : -1);
			this.lastNonInfectedOwner = value;
		}
	}

	public int LastNonInfectedOwnerIndex
	{
		get
		{
			return this.lastNonInfectedOwnerIndex;
		}
	}

	public int EmpireInfiltrationBits { get; set; }

	public override GameEntityGUID GUID
	{
		get
		{
			return this.guid;
		}
	}

	public ReadOnlyCollection<CityImprovement> CityImprovements
	{
		get
		{
			return this.cityImprovementsReadOnlyCollection;
		}
	}

	public ReadOnlyCollection<District> Districts
	{
		get
		{
			return this.districtsReadOnlyCollection;
		}
	}

	public int ExtensionCount { get; set; }

	public StaticString DefaultClass
	{
		get
		{
			return "ClassCity";
		}
	}

	public bool IsInfected
	{
		get
		{
			return base.SimulationObject != null && base.SimulationObject.Tags.Contains(City.TagCityStatusInfected);
		}
	}

	public bool IsIntegrated
	{
		get
		{
			return base.SimulationObject != null && base.SimulationObject.Tags.Contains(City.TagCityStatusIntegrated);
		}
	}

	public bool IsRoundingUp
	{
		get
		{
			return (int)this.GetPropertyBaseValue(SimulationProperties.RoundUpProgress) >= 0;
		}
	}

	public override string LocalizedName
	{
		get
		{
			if (!string.IsNullOrEmpty(this.userDefinedName))
			{
				return this.userDefinedName;
			}
			if (this.Region != null)
			{
				return this.Region.LocalizedName;
			}
			return base.Name;
		}
	}

	public Militia Militia { get; set; }

	public float[] Ownership { get; set; }

	public Region Region { get; set; }

	public List<TradeRoute> TradeRoutes { get; set; }

	public int TurnWhenToProceedWithRazing { get; set; }

	public bool ShouldRazeRegionBuildingWithSelf { get; set; }

	public bool ShouldInjureSpyOnRaze { get; set; }

	[XmlElement(ElementName = "UserDefinedName")]
	public string UserDefinedName
	{
		get
		{
			return this.userDefinedName;
		}
		set
		{
			this.userDefinedName = value;
		}
	}

	public WorldPosition WorldPosition { get; set; }

	public WorldPosition DryDockPosition { get; set; }

	public override string GetLocalizedName(global::Empire empireLooking)
	{
		return this.LocalizedName;
	}

	public void AddCityImprovement(CityImprovement cityImprovement, bool applyDeportedDescriptor = true)
	{
		if (cityImprovement == null)
		{
			throw new ArgumentNullException("cityImprovement");
		}
		if (cityImprovement.CityImprovementDefinition == null)
		{
			return;
		}
		base.AddChild(cityImprovement);
		this.cityImprovements.Add(cityImprovement);
		cityImprovement.City = this;
		if (cityImprovement.CityImprovementDefinition.DeportedSimulationDescriptors != null && applyDeportedDescriptor)
		{
			cityImprovement.ApplyDeportedDescriptor(cityImprovement.CityImprovementDefinition.DeportedSimulationDescriptors);
		}
		this.OnCityImprovementCollectionChange(CollectionChangeAction.Add, cityImprovement);
	}

	public void AddDistrict(District district)
	{
		if (district == null)
		{
			throw new ArgumentNullException("district");
		}
		if (!district.GUID.IsValid)
		{
			Diagnostics.LogError("Cannot add district with invalid guid.");
			return;
		}
		if (this.districts.Exists((District item) => item.GUID == district.GUID))
		{
			Diagnostics.Assert(district.City == this);
			Diagnostics.LogWarning("Cannot add district twice to the same city, ignoring...");
			return;
		}
		if (district.City != null)
		{
			Diagnostics.Assert(district.City != this);
			Diagnostics.LogWarning("Cannot add district already linked to another city, ignoring...");
			return;
		}
		this.districts.Add(district);
		district.City = this;
		base.AddChild(district);
		if (district.Type != DistrictType.Exploitation)
		{
			this.ExtensionCount++;
		}
		if (this.DryDockPosition == WorldPosition.Invalid)
		{
			IWorldPositionningService service = this.gameService.Game.Services.GetService<IWorldPositionningService>();
			if (service.IsWaterTile(district.WorldPosition) && !service.IsFrozenWaterTile(district.WorldPosition))
			{
				GridMap<byte> map = (this.gameService.Game as global::Game).World.Atlas.GetMap(WorldAtlas.Maps.Terrain) as GridMap<byte>;
				Map<TerrainTypeName> map2 = (this.gameService.Game as global::Game).World.Atlas.GetMap(WorldAtlas.Tables.Terrains) as Map<TerrainTypeName>;
				byte value = map.GetValue(district.WorldPosition);
				StaticString empty = StaticString.Empty;
				if (map2.Data.TryGetValue((int)value, ref empty) && empty.ToString().IndexOf("InlandWater") == -1)
				{
					this.DryDockPosition = district.WorldPosition;
				}
			}
		}
		this.OnCityDistrictCollectionChange(CollectionChangeAction.Add, district);
	}

	public void ChangeAdministrationSpeciality(StaticString newAdministrationSpeciality)
	{
		this.AdministrationSpeciality = newAdministrationSpeciality;
		this.OnAdministrationSpecialityChange();
	}

	public District GetCityCenter()
	{
		Diagnostics.Assert(this.districts != null);
		for (int i = 0; i < this.districts.Count; i++)
		{
			District district = this.districts[i];
			Diagnostics.Assert(district != null);
			if (district.Type == DistrictType.Center)
			{
				return district;
			}
		}
		return null;
	}

	public void GiveFullOwnershipToEmpire(int empireIndex)
	{
		if (empireIndex < 0 || empireIndex >= this.Ownership.Length)
		{
			Diagnostics.LogError("Invalid empire index");
			return;
		}
		for (int i = 0; i < this.Ownership.Length; i++)
		{
			this.Ownership[i] = 0f;
		}
		this.Ownership[empireIndex] = 1f;
		base.SetPropertyBaseValue(SimulationProperties.Ownership, 1f);
		this.Refresh(false);
	}

	public void NotifyCityOwnerChange()
	{
		if (this.CityOwnerChange != null)
		{
			this.CityOwnerChange(this, new EventArgs());
		}
	}

	public void NotifyCityCadastralChange()
	{
		if (this.CityCadastralChange != null)
		{
			this.CityCadastralChange(this, new EventArgs());
		}
	}

	public void RemoveDistrict(District district)
	{
		if (district == null || !district.GUID.IsValid || !this.districts.Contains(district))
		{
			return;
		}
		if (district.City != this)
		{
			Diagnostics.Assert(district.City != this);
			Diagnostics.LogWarning("Cannot remove district linked to another city, ignoring...");
			return;
		}
		if (this.DryDockPosition == district.WorldPosition)
		{
			this.DryDockPosition = WorldPosition.Invalid;
		}
		if (district.Type != DistrictType.Exploitation)
		{
			this.ExtensionCount--;
		}
		base.RemoveChild(district);
		district.City = null;
		this.districts.Remove(district);
		this.OnCityDistrictCollectionChange(CollectionChangeAction.Remove, district);
	}

	public void RemoveCityImprovement(CityImprovement cityImprovement)
	{
		if (cityImprovement == null)
		{
			return;
		}
		if (cityImprovement.CityImprovementDefinition.DeportedSimulationDescriptors != null)
		{
			cityImprovement.RemoveDeportedDescriptor(cityImprovement.CityImprovementDefinition.DeportedSimulationDescriptors);
		}
		this.cityImprovements.Remove(cityImprovement);
		base.RemoveChild(cityImprovement);
		cityImprovement.City = null;
		this.OnCityImprovementCollectionChange(CollectionChangeAction.Remove, cityImprovement);
	}

	public override string ToString()
	{
		if (base.Name == null)
		{
			return base.GetType().ToString();
		}
		return string.Format("{0}:{1}", base.Name, this.LocalizedName);
	}

	public void UpdateInfectionStatus()
	{
		IDatabase<SimulationDescriptor> database = Databases.GetDatabase<SimulationDescriptor>(false);
		SimulationDescriptor descriptor = null;
		if (!database.TryGetValue(City.TagCityStatusInfected, out descriptor))
		{
			Diagnostics.LogError("Infection Status descriptor could not be retrieved.");
			return;
		}
		if (this.LastNonInfectedOwner == null)
		{
			this.LastNonInfectedOwner = this.Empire;
		}
		else if (this.Empire == this.LastNonInfectedOwner)
		{
			if (this.IsInfected)
			{
				base.RemoveDescriptor(descriptor);
			}
		}
		else if (this.Empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitMimics3))
		{
			if (!this.IsInfected)
			{
				base.AddDescriptor(descriptor, false);
			}
		}
		else
		{
			if (this.IsInfected)
			{
				base.RemoveDescriptor(descriptor);
			}
			this.LastNonInfectedOwner = this.Empire;
		}
	}

	public District GetValidDistrictToTarget(Army army = null)
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(service2 != null);
		IPathfindingService service3 = service.Game.Services.GetService<IPathfindingService>();
		Diagnostics.Assert(service3 != null);
		for (int i = 0; i < this.districts.Count; i++)
		{
			District district = this.districts[i];
			if (District.IsACityTile(district))
			{
				for (int j = 0; j < 6; j++)
				{
					WorldPosition neighbourTile = service2.GetNeighbourTile(district.WorldPosition, (WorldOrientation)j, 1);
					if (!service2.IsWaterTile(neighbourTile))
					{
						if (army == null || service3.IsTileStopable(neighbourTile, army, PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnoreZoneOfControl, null))
						{
							for (int k = 0; k < this.districts.Count; k++)
							{
								District district2 = this.districts[k];
								if (district2.WorldPosition == neighbourTile)
								{
									if (service3.IsTransitionPassable(district2.WorldPosition, district.WorldPosition, PathfindingMovementCapacity.Ground, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnoreZoneOfControl | PathfindingFlags.IgnoreSieges))
									{
										Army armyAtPosition = service2.GetArmyAtPosition(district2.WorldPosition);
										if (armyAtPosition == null || armyAtPosition == army)
										{
											return district2;
										}
									}
									break;
								}
							}
						}
					}
				}
			}
		}
		return this.districts[0];
	}

	public District GetDistrictCenter()
	{
		foreach (District district in this.Districts)
		{
			if (district.Type == DistrictType.Center)
			{
				return district;
			}
		}
		return null;
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (disposing)
		{
			for (int i = 0; i < this.districts.Count; i++)
			{
				this.districts[i].Dispose();
			}
		}
		this.districts.Clear();
		this.Region = null;
		if (disposing)
		{
			for (int j = 0; j < this.cityImprovements.Count; j++)
			{
				this.cityImprovements[j].Dispose();
			}
		}
		this.cityImprovements.Clear();
		if (disposing && this.Militia != null)
		{
			this.Militia.Dispose();
		}
		this.Militia = null;
		this.Empire = null;
		this.besiegingEmpire = null;
		if (this.OnCityDisposed != null)
		{
			this.OnCityDisposed();
		}
	}

	private void OnCityDistrictCollectionChange(CollectionChangeAction action, object element)
	{
		if (this.CityDistrictCollectionChange != null)
		{
			this.CityDistrictCollectionChange(this, new CollectionChangeEventArgs(action, element));
		}
	}

	private void OnCityImprovementCollectionChange(CollectionChangeAction action, object element)
	{
		if (this.CityImprovementCollectionChange != null)
		{
			this.CityImprovementCollectionChange(this, new CollectionChangeEventArgs(action, element));
		}
	}

	private void OnAdministrationSpecialityChange()
	{
		if (this.AdministrationSpecialityChange != null)
		{
			this.AdministrationSpecialityChange(this, new EventArgs());
		}
	}

	private Camp camp;

	private List<RegionalEffect> regionalEffects = new List<RegionalEffect>();

	private int besiegingEmpireIndex = -1;

	private global::Empire besiegingEmpire;

	private List<Army> besiegingSeafaringArmies = new List<Army>();

	public static readonly string ReadOnlyCategory = "City";

	public static readonly StaticString TagCityStatusInfected = new StaticString("CityStatusInfected");

	public static readonly StaticString TagCityStatusRazed = new StaticString("CityStatusRazed");

	public static readonly StaticString TagMainCity = new StaticString("MainCity");

	public static readonly StaticString MimicsCity = new StaticString("MimicsCity");

	public static readonly StaticString TagCityStatusIntegrated = new StaticString("CityStatusIntegrated");

	private List<CityImprovement> cityImprovements = new List<CityImprovement>();

	private List<District> districts = new List<District>();

	private GameEntityGUID guid;

	private int lastNonInfectedOwnerIndex = -1;

	private global::Empire lastNonInfectedOwner;

	private string userDefinedName;

	private IGameService gameService;

	private ReadOnlyCollection<CityImprovement> cityImprovementsReadOnlyCollection;

	private ReadOnlyCollection<District> districtsReadOnlyCollection;

	public delegate void OnCityCampChangedDelegate();

	public delegate void OnCityDisposedDelegate();
}
