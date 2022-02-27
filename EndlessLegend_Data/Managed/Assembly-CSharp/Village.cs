using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class Village : MinorEmpireGarrison, Amplitude.Xml.Serialization.IXmlSerializable, IGarrison, IGarrisonWithPosition, IGameEntity, IGameEntityWithWorldPosition, IWorldPositionable, ICategoryProvider
{
	public Village(GameEntityGUID guid) : base("Village#" + guid.ToString(), guid)
	{
	}

	public override void ReadXml(XmlReader reader)
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		Diagnostics.Assert(service.Game != null);
		global::Game game = service.Game as global::Game;
		Diagnostics.Assert(game != null);
		this.HasBeenPacified = reader.GetAttribute<bool>("HasBeenPacified");
		int num = reader.ReadVersionAttribute();
		if (num >= 2)
		{
			this.HasBeenConvertedByIndex = reader.GetAttribute<int>("HasBeenConvertedByIndex");
			this.ConvertedUnitSpawnTurn = reader.GetAttribute<int>("ConvertedUnitSpawnTurn");
			this.HasBeenConvertedBy = null;
		}
		base.ReadXml(reader);
		int num2 = reader.ReadElementString<int>("IndexOfPointOfInterest");
		if (num >= 3)
		{
			int num3 = reader.ReadElementString<int>("IndexOfRegion");
			this.PointOfInterest = game.World.Regions[num3].PointOfInterests[num2];
		}
		else
		{
			MinorEmpire minorEmpire = this.Empire as MinorEmpire;
			Diagnostics.Assert(minorEmpire != null);
			this.PointOfInterest = minorEmpire.Region.PointOfInterests[num2];
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<bool>("HasBeenPacified", this.HasBeenPacified);
		int num = writer.WriteVersionAttribute(3);
		if (num >= 2)
		{
			writer.WriteAttributeString<int>("HasBeenConvertedByIndex", this.HasBeenConvertedByIndex);
			writer.WriteAttributeString<int>("ConvertedUnitSpawnTurn", this.ConvertedUnitSpawnTurn);
		}
		base.WriteXml(writer);
		Region region = this.PointOfInterest.Region;
		int value = Array.IndexOf<PointOfInterest>(region.PointOfInterests, this.PointOfInterest);
		writer.WriteElementString<int>("IndexOfPointOfInterest", value);
		if (num >= 3)
		{
			writer.WriteElementString<int>("IndexOfRegion", region.Index);
		}
	}

	public StaticString Category
	{
		get
		{
			if (this.PointOfInterest.PointOfInterestImprovement != null)
			{
				return this.PointOfInterest.PointOfInterestImprovement.Category;
			}
			return string.Empty;
		}
	}

	public MajorEmpire Converter
	{
		get
		{
			return this.HasBeenConvertedBy;
		}
		set
		{
			this.HasBeenConvertedBy = value;
			if (this.HasBeenConvertedBy != null)
			{
				this.HasBeenConvertedByIndex = this.HasBeenConvertedBy.Index;
			}
			else
			{
				this.HasBeenConvertedByIndex = -1;
				foreach (Unit simulationObjectWrapper in this.Units)
				{
					base.RemoveChild(simulationObjectWrapper);
				}
			}
		}
	}

	public override global::Empire Empire
	{
		get
		{
			if (this.HasBeenConvertedBy != null)
			{
				return this.HasBeenConvertedBy;
			}
			if (this.Region != null)
			{
				return this.Region.MinorEmpire;
			}
			return base.Empire;
		}
		set
		{
			base.Empire = value;
		}
	}

	public bool HasBeenConverted
	{
		get
		{
			return this.HasBeenConvertedBy != null && this.PointOfInterest.SimulationObject.Tags.Contains(Village.ConvertedVillage);
		}
	}

	public bool HasBeenInfected
	{
		get
		{
			return this.PointOfInterest.SimulationObject.Tags.Contains(DepartmentOfCreepingNodes.InfectedPointOfInterest);
		}
	}

	public bool IsInfectionComplete
	{
		get
		{
			return this.PointOfInterest.SimulationObject.Tags.Contains(DepartmentOfCreepingNodes.VillageInfectionComplete);
		}
	}

	public override int MaximumUnitSlot
	{
		get
		{
			if (this.HasBeenConverted)
			{
				return this.GetMaximumConvertedUnitSlot();
			}
			return base.MaximumUnitSlot;
		}
	}

	public int ConvertedUnitSpawnTurn { get; set; }

	public bool HasBeenPacified { get; set; }

	[XmlIgnore]
	public override string LocalizedName
	{
		get
		{
			return string.Format(AgeLocalizer.Instance.LocalizeString("%MinorFactionVillageTitle"), this.MinorEmpire.Faction.LocalizedName);
		}
	}

	public MinorEmpire MinorEmpire
	{
		get
		{
			if (this.Region != null)
			{
				return this.Region.MinorEmpire;
			}
			if (base.Empire != null && base.Empire is MinorEmpire)
			{
				return (MinorEmpire)base.Empire;
			}
			return null;
		}
	}

	public PointOfInterest PointOfInterest { get; set; }

	public Region Region { get; set; }

	public StaticString SubCategory
	{
		get
		{
			if (this.PointOfInterest.PointOfInterestImprovement != null)
			{
				return this.PointOfInterest.PointOfInterestImprovement.SubCategory;
			}
			return string.Empty;
		}
	}

	public WorldPosition WorldPosition
	{
		get
		{
			if (this.PointOfInterest == null)
			{
				return WorldPosition.Invalid;
			}
			return this.PointOfInterest.WorldPosition;
		}
	}

	public override void CleanAfterEncounter(Encounter encounter)
	{
		BarbarianCouncil agency = this.MinorEmpire.GetAgency<BarbarianCouncil>();
		agency.CleanVillageAfterEncounter(this, encounter);
		base.LeaveEncounter();
	}

	public int GetConvertedUnitProductionTimer()
	{
		if (this.Converter != null)
		{
			return Mathf.FloorToInt(this.PointOfInterest.GetPropertyValue(SimulationProperties.ConvertedVillageUnitProductionTimer) * this.Converter.GetPropertyValue(SimulationProperties.GameSpeedMultiplier));
		}
		return 0;
	}

	public int GetRemainingTurnBeforeUnitSpawn(int currentTurn)
	{
		int num = this.ConvertedUnitSpawnTurn - currentTurn;
		if (num == 0)
		{
			return this.GetConvertedUnitProductionTimer();
		}
		return num;
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		this.PointOfInterest = null;
	}

	private int GetMaximumConvertedUnitSlot()
	{
		Diagnostics.Assert(this.Converter != null);
		DepartmentOfTheInterior agency = this.Converter.GetAgency<DepartmentOfTheInterior>();
		Diagnostics.Assert(agency != null);
		Diagnostics.Assert(agency.Empire != null);
		return Mathf.FloorToInt(agency.Empire.GetPropertyValue(SimulationProperties.CityUnitSlot));
	}

	public static readonly StaticString ConvertedVillage = "ConvertedVillage";

	public static readonly StaticString PacifiedVillage = "PacifiedVillage";

	public static readonly StaticString DissentedVillage = "DissentedVillage";

	public static readonly StaticString RebuiltVillage = "RebuiltVillage";

	internal MajorEmpire HasBeenConvertedBy;

	internal int HasBeenConvertedByIndex = -1;
}
