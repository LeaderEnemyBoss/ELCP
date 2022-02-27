using System;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation;
using Amplitude.Utilities.Maps;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class District : SimulationObjectWrapper, IXmlSerializable, ILineOfSightEntity, IGameEntity, IGameEntityWithLineOfSight, IGameEntityWithWorldPosition, IWorldPositionable, ICategoryProvider, IPropertyEffectFeatureProvider
{
	public District(GameEntityGUID guid) : base("District#" + guid)
	{
		this.GUID = guid;
		this.Level = 0;
		this.ResourceOnMigration = string.Empty;
		this.lastLineOfSightRange = this.LineOfSightVisionRange;
		this.LineOfSightDirty = true;
		this.LineOfSightActive = true;
		base.Refreshed += this.District_Refreshed;
		this.eventService = Services.GetService<IEventService>();
		this.eventService.EventRaise += this.EventService_EventRaise;
	}

	public event EventHandler LevelChange;

	int ILineOfSightEntity.EmpireInfiltrationBits
	{
		get
		{
			if (this.City == null)
			{
				return 0;
			}
			return this.City.EmpireInfiltrationBits | this.EmpireEarthquakeBits;
		}
	}

	bool ILineOfSightEntity.IgnoreFog
	{
		get
		{
			return false;
		}
	}

	StaticString ICategoryProvider.Category
	{
		get
		{
			return District.ReadOnlyCategory;
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

	public override void ReadXml(XmlReader reader)
	{
		base.ReadXml(reader);
		this.Level = reader.ReadElementString<int>("Level");
		this.Type = (DistrictType)reader.ReadElementString<int>("Type");
		if (reader.IsStartElement("ResourceOnMigration"))
		{
			this.ResourceOnMigration = reader.ReadElementString<string>("ResourceOnMigration");
		}
		this.WorldPosition = reader.ReadElementSerializable<WorldPosition>();
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<ulong>("GUID", this.GUID);
		base.WriteXml(writer);
		writer.WriteElementString<int>("Level", this.Level);
		writer.WriteElementString<int>("Type", (int)this.Type);
		writer.WriteElementString<string>("ResourceOnMigration", this.ResourceOnMigration);
		IXmlSerializable xmlSerializable = this.WorldPosition;
		writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
	}

	public City City { get; set; }

	public global::Empire Empire
	{
		get
		{
			if (this.City == null)
			{
				return null;
			}
			return this.City.Empire;
		}
	}

	public int EmpireEarthquakeBits { get; set; }

	public StaticString ResourceOnMigration { get; set; }

	public GameEntityGUID GUID { get; private set; }

	public int Level { get; private set; }

	public bool LineOfSightActive { get; set; }

	public LineOfSightData LineOfSightData { get; set; }

	public bool LineOfSightDirty { get; set; }

	public int LineOfSightDetectionRange
	{
		get
		{
			return Mathf.RoundToInt(this.GetPropertyValue(SimulationProperties.DetectionRange));
		}
	}

	public int LineOfSightVisionRange
	{
		get
		{
			return Mathf.RoundToInt(this.GetPropertyValue(SimulationProperties.VisionRange));
		}
	}

	public int LineOfSightVisionHeight
	{
		get
		{
			return Mathf.RoundToInt(this.GetPropertyValue(SimulationProperties.VisionHeight));
		}
	}

	public DistrictType Type
	{
		get
		{
			return this.districtType;
		}
		set
		{
			this.districtType = value;
		}
	}

	public VisibilityController.VisibilityAccessibility VisibilityAccessibilityLevel
	{
		get
		{
			return VisibilityController.VisibilityAccessibility.Public;
		}
	}

	public bool WasTerraformed
	{
		get
		{
			return this.GetPropertyValue(SimulationProperties.TerraformState) != 0f;
		}
	}

	public WorldPosition WorldPosition
	{
		get
		{
			return this.worldPosition;
		}
		set
		{
			if (this.worldPosition != value)
			{
				WorldPosition lastPosition = this.worldPosition;
				this.worldPosition = value;
				IGameService service = Services.GetService<IGameService>();
				IWorldPositionningService service2 = ((global::Game)service.Game).GetService<IWorldPositionningService>();
				this.OnWorldPositionChange(lastPosition, this.worldPosition);
			}
		}
	}

	public static bool IsACityTile(District district)
	{
		return district != null && (district.Type == DistrictType.Extension || district.Type == DistrictType.Center);
	}

	public static bool IsACityTile(DistrictType districtType)
	{
		return districtType == DistrictType.Extension || districtType == DistrictType.Center;
	}

	public void RefreshTerrainDescriptors()
	{
		if (base.SimulationObject == null)
		{
			return;
		}
		IGameService service = Services.GetService<IGameService>();
		IWorldPositionningService service2 = ((global::Game)service.Game).GetService<IWorldPositionningService>();
		DepartmentOfTheInterior.RemoveAnyTerrainTypeDescriptor(base.SimulationObject);
		byte terrainType = service2.GetTerrainType(this.WorldPosition);
		StaticString terrainTypeMappingName = service2.GetTerrainTypeMappingName(terrainType);
		DepartmentOfTheInterior.ApplyTerrainTypeDescriptor(base.SimulationObject, terrainTypeMappingName);
		DepartmentOfTheInterior.RemoveAnyBiomeTypeDescriptor(base.SimulationObject);
		byte biomeType = service2.GetBiomeType(this.WorldPosition);
		StaticString biomeTypeMappingName = service2.GetBiomeTypeMappingName(biomeType);
		DepartmentOfTheInterior.ApplyBiomeTypeDescriptor(base.SimulationObject, biomeTypeMappingName);
		DepartmentOfTheInterior.RemoveAnyAnomalyDescriptor(base.SimulationObject);
		byte anomalyType = service2.GetAnomalyType(this.WorldPosition);
		StaticString anomalyTypeMappingName = service2.GetAnomalyTypeMappingName(anomalyType);
		DepartmentOfTheInterior.ApplyAnomalyDescriptor(base.SimulationObject, anomalyTypeMappingName);
		DepartmentOfTheInterior.RemoveAnyRiverTypeDescriptor(base.SimulationObject);
		short riverId = service2.GetRiverId(this.WorldPosition);
		StaticString riverTypeMappingName = service2.GetRiverTypeMappingName(riverId);
		DepartmentOfTheInterior.ApplyRiverTypeDescriptor(base.SimulationObject, riverTypeMappingName);
		this.Refresh(false);
	}

	public void SetLevel(int level, bool silent = true)
	{
		if (this.Level != level)
		{
			int level2 = this.Level;
			this.Level = level;
			base.SetPropertyBaseValue(SimulationProperties.Level, (float)level);
			if (!silent)
			{
				this.OnLevelChange(new EventArgs());
				this.eventService.Notify(new EventDistrictLevelUp(this, level2, this.Level, this.City));
			}
		}
	}

	protected override void Dispose(bool disposing)
	{
		this.eventService.EventRaise -= this.EventService_EventRaise;
		base.Dispose(disposing);
		if (disposing && this.districtsMap != null)
		{
			this.districtsMap.SetValue(this.worldPosition, null);
		}
		this.districtsMap = null;
		this.City = null;
	}

	private void EventService_EventRaise(object sender, EventRaiseEventArgs e)
	{
		Amplitude.Unity.Event.Event raisedEvent = e.RaisedEvent;
		Diagnostics.Assert(raisedEvent != null);
		if (raisedEvent is EventEmpireWorldTerraformed)
		{
			this.OnWorldTerraformed(raisedEvent as EventEmpireWorldTerraformed);
		}
	}

	protected virtual void OnLevelChange(EventArgs e)
	{
		if (this.LevelChange != null)
		{
			this.LevelChange(this, e);
		}
	}

	private void District_Refreshed(object sender)
	{
		int lineOfSightVisionRange = this.LineOfSightVisionRange;
		if (lineOfSightVisionRange != this.lastLineOfSightRange)
		{
			this.LineOfSightDirty = true;
			this.lastLineOfSightRange = lineOfSightVisionRange;
		}
	}

	private void OnWorldTerraformed(EventEmpireWorldTerraformed raisedEvent)
	{
		for (int i = 0; i < raisedEvent.TerraformedTiles.Length; i++)
		{
			if (raisedEvent.TerraformedTiles[i] == this.WorldPosition)
			{
				this.RefreshTerrainDescriptors();
				base.SetPropertyBaseValue(SimulationProperties.TerraformState, 1f);
				this.Refresh(false);
				break;
			}
		}
	}

	private void OnWorldPositionChange(WorldPosition lastPosition, WorldPosition newPosition)
	{
		if (this.districtsMap == null)
		{
			IGameService service = Services.GetService<IGameService>();
			global::Game game = service.Game as global::Game;
			this.districtsMap = (game.World.Atlas.GetMap(WorldAtlas.Maps.Districts) as GridMap<District>);
			Diagnostics.Assert(this.districtsMap != null);
			Diagnostics.Assert(this.districtsMap.GetValue(this.worldPosition) == null);
		}
		this.districtsMap.SetValue(newPosition, this);
		this.LineOfSightDirty = true;
	}

	public static readonly string ReadOnlyCategory = "District";

	private DistrictType districtType = DistrictType.Exploitation;

	private IEventService eventService;

	private WorldPosition worldPosition;

	private int lastLineOfSightRange;

	private GridMap<District> districtsMap;
}
