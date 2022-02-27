using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class Fortress : Garrison, Amplitude.Xml.Serialization.IXmlSerializable, IGarrison, IGameEntity, IGarrisonWithPosition, IGameEntityWithWorldPosition, IWorldPositionable, ICategoryProvider, IDescriptorEffectProvider, IPropertyEffectFeatureProvider
{
	public Fortress(GameEntityGUID guid) : base("Fortress#" + guid)
	{
		this.guid = guid;
		this.gameService = Services.GetService<IGameService>();
	}

	public event CollectionChangeEventHandler FortressDistrictCollectionChange;

	public event EventHandler OccupantChange;

	StaticString ICategoryProvider.Category
	{
		get
		{
			return Fortress.ReadOnlyCategory;
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
			if (!(holder.Descriptor.Name == "ClassPointOfInterest"))
			{
				yield return holder.Descriptor;
			}
		}
		yield break;
	}

	public override void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		Diagnostics.Assert(service.Game != null);
		global::Game game = service.Game as global::Game;
		Diagnostics.Assert(game != null);
		this.guid = reader.GetAttribute<ulong>("GUID");
		this.userDefinedName = reader.GetAttribute("UserDefinedString");
		if (num >= 2)
		{
			this.orientation = (Fortress.CardinalOrientation)((int)Enum.Parse(typeof(Fortress.CardinalOrientation), reader.GetAttribute("Orientation")));
		}
		base.ReadXml(reader);
		int num2 = reader.ReadElementString<int>("IndexOfPointOfInterest");
		int num3 = reader.ReadElementString<int>("IndexOfRegion");
		this.Region = game.World.Regions[num3];
		this.PointOfInterest = game.World.Regions[num3].PointOfInterests[num2];
		if (this.PointOfInterest != null)
		{
			base.AddChild(this.PointOfInterest);
		}
		this.isOccupiedByIndex = reader.ReadElementString<int>("OccupantIndex");
		int attribute = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("Facilities");
		this.facilities.Clear();
		for (int i = 0; i < attribute; i++)
		{
			num2 = reader.ReadElementString<int>("IndexOfPointOfInterest");
			num3 = reader.ReadElementString<int>("IndexOfRegion");
			if (num3 > 0 && num3 < game.World.Regions.Length && num2 > 0 && num2 < game.World.Regions.Length)
			{
				PointOfInterest pointOfInterest = game.World.Regions[num3].PointOfInterests[num2];
				if (pointOfInterest != null)
				{
					this.facilities.Add(pointOfInterest);
					base.AddChild(pointOfInterest);
				}
			}
		}
		reader.ReadEndElement("Facilities");
		this.WorldPosition = reader.ReadElementSerializable<WorldPosition>();
	}

	public override void WriteXml(XmlWriter writer)
	{
		int num = writer.WriteVersionAttribute(2);
		writer.WriteAttributeString<GameEntityGUID>("GUID", this.GUID);
		writer.WriteAttributeString("UserDefinedString", this.userDefinedName);
		if (num >= 2)
		{
			writer.WriteAttributeString("Orientation", this.orientation.ToString());
		}
		base.WriteXml(writer);
		Region region = this.PointOfInterest.Region;
		int value = Array.IndexOf<PointOfInterest>(region.PointOfInterests, this.PointOfInterest);
		writer.WriteElementString<int>("IndexOfPointOfInterest", value);
		writer.WriteElementString<int>("IndexOfRegion", region.Index);
		writer.WriteElementString<int>("OccupantIndex", this.isOccupiedByIndex);
		writer.WriteStartElement("Facilities");
		writer.WriteAttributeString<int>("Count", this.facilities.Count);
		for (int i = 0; i < this.facilities.Count; i++)
		{
			region = this.facilities[i].Region;
			writer.WriteElementString<int>("IndexOfPointOfInterest", Array.IndexOf<PointOfInterest>(region.PointOfInterests, this.facilities[i]));
			writer.WriteElementString<int>("IndexOfRegion", region.Index);
		}
		writer.WriteEndElement();
		Amplitude.Xml.Serialization.IXmlSerializable xmlSerializable = this.WorldPosition;
		writer.WriteElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>(ref xmlSerializable);
	}

	public Fortress.CardinalOrientation Orientation
	{
		get
		{
			return this.orientation;
		}
		set
		{
			this.orientation = value;
		}
	}

	public MajorEmpire Occupant
	{
		get
		{
			if (this.isOccupiedBy == null && this.isOccupiedByIndex >= 0)
			{
				global::Game game = this.gameService.Game as global::Game;
				if (game.Empires != null)
				{
					this.isOccupiedBy = (game.Empires[this.isOccupiedByIndex] as MajorEmpire);
				}
			}
			return this.isOccupiedBy;
		}
		set
		{
			this.isOccupiedBy = value;
			if (this.isOccupiedBy != null)
			{
				this.isOccupiedByIndex = this.isOccupiedBy.Index;
				if (ELCPUtilities.SpectatorMode && this.PointOfInterest != null)
				{
					global::Empire[] empires = (this.gameService.Game as global::Game).Empires;
					for (int i = 0; i < empires.Length; i++)
					{
						MajorEmpire majorEmpire = empires[i] as MajorEmpire;
						if (majorEmpire == null)
						{
							return;
						}
						if (majorEmpire.Index != this.isOccupiedBy.Index && majorEmpire.ELCPIsEliminated)
						{
							this.PointOfInterest.InfiltrationBits |= 1 << majorEmpire.Index;
						}
					}
					return;
				}
			}
			else
			{
				if (ELCPUtilities.SpectatorMode && this.PointOfInterest != null)
				{
					this.PointOfInterest.InfiltrationBits = 0;
				}
				this.isOccupiedByIndex = -1;
			}
		}
	}

	public override global::Empire Empire
	{
		get
		{
			if (this.isOccupiedBy != null)
			{
				return this.isOccupiedBy;
			}
			if (this.Region != null)
			{
				return this.Region.NavalEmpire;
			}
			return base.Empire;
		}
		set
		{
			base.Empire = value;
		}
	}

	public bool IsOccupied
	{
		get
		{
			return this.isOccupiedBy != null;
		}
	}

	public override GameEntityGUID GUID
	{
		get
		{
			return this.guid;
		}
	}

	public List<PointOfInterest> Facilities
	{
		get
		{
			return this.facilities;
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

	public NavalEmpire NavalEmpire
	{
		get
		{
			if (this.Region != null && this.Region.IsOcean)
			{
				return this.Region.NavalEmpire;
			}
			if (base.Empire != null && base.Empire is NavalEmpire)
			{
				return (NavalEmpire)base.Empire;
			}
			return null;
		}
	}

	public PointOfInterest PointOfInterest { get; set; }

	public Region Region { get; set; }

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

	public StaticString DefaultClass
	{
		get
		{
			return new StaticString("ClassPointOfInterest");
		}
	}

	public static Fortress.CardinalOrientation BestOrientation(Fortress fortress, WorldPosition barycenter)
	{
		return Fortress.BestOrientations(fortress, barycenter)[0];
	}

	public static List<Fortress.CardinalOrientation> BestOrientations(Fortress fortress, WorldPosition barycenter)
	{
		List<Fortress.CardinalOrientation> list = new List<Fortress.CardinalOrientation>(Enum.GetNames(typeof(Fortress.CardinalOrientation)).Length);
		if (fortress.PointOfInterest.WorldPosition == barycenter)
		{
			list.Add(Fortress.CardinalOrientation.Center);
			return list;
		}
		int num = (int)(fortress.PointOfInterest.WorldPosition.Column - barycenter.Column);
		int num2 = (int)(fortress.PointOfInterest.WorldPosition.Row - barycenter.Row);
		int num3 = 3;
		if (Mathf.Abs(num) <= num3 && Mathf.Abs(num2) <= num3)
		{
			list.Add(Fortress.CardinalOrientation.Center);
		}
		bool flag = Mathf.Abs(num) > Mathf.Abs(num2) || (Mathf.Abs(num) >= Mathf.Abs(num2) && UnityEngine.Random.Range(0, 2) == 0);
		if (flag)
		{
			if (num > 0)
			{
				list.Add(Fortress.CardinalOrientation.East);
			}
			else
			{
				list.Add(Fortress.CardinalOrientation.West);
			}
			if (num2 > 0)
			{
				list.Add(Fortress.CardinalOrientation.North);
				list.Add(Fortress.CardinalOrientation.South);
			}
			else if (num2 < 0)
			{
				list.Add(Fortress.CardinalOrientation.South);
				list.Add(Fortress.CardinalOrientation.North);
			}
			else if (UnityEngine.Random.Range(0, 2) == 0)
			{
				list.Add(Fortress.CardinalOrientation.North);
				list.Add(Fortress.CardinalOrientation.South);
			}
			else
			{
				list.Add(Fortress.CardinalOrientation.South);
				list.Add(Fortress.CardinalOrientation.North);
			}
			if (num > 0)
			{
				list.Add(Fortress.CardinalOrientation.West);
			}
			else
			{
				list.Add(Fortress.CardinalOrientation.East);
			}
		}
		else
		{
			if (num2 > 0)
			{
				list.Add(Fortress.CardinalOrientation.North);
			}
			else
			{
				list.Add(Fortress.CardinalOrientation.South);
			}
			if (num > 0)
			{
				list.Add(Fortress.CardinalOrientation.East);
				list.Add(Fortress.CardinalOrientation.West);
			}
			else if (num < 0)
			{
				list.Add(Fortress.CardinalOrientation.West);
				list.Add(Fortress.CardinalOrientation.East);
			}
			else if (UnityEngine.Random.Range(0, 2) == 0)
			{
				list.Add(Fortress.CardinalOrientation.East);
				list.Add(Fortress.CardinalOrientation.West);
			}
			else
			{
				list.Add(Fortress.CardinalOrientation.West);
				list.Add(Fortress.CardinalOrientation.East);
			}
			if (num2 > 0)
			{
				list.Add(Fortress.CardinalOrientation.South);
			}
			else
			{
				list.Add(Fortress.CardinalOrientation.North);
			}
		}
		return list;
	}

	public static bool IsFacilityUnique(PointOfInterest facility)
	{
		string a;
		return facility != null && facility.PointOfInterestDefinition.TryGetValue("IsUniqueFacility", out a) && a == "true";
	}

	public override string GetLocalizedName(global::Empire empireLooking)
	{
		return this.LocalizedName;
	}

	public void NotifyOccupantChange()
	{
		if (this.OccupantChange != null)
		{
			this.OccupantChange(this, new EventArgs());
		}
	}

	public override string ToString()
	{
		if (base.Name == null)
		{
			return base.GetType().ToString();
		}
		return string.Format("{0}:{1}", base.Name, this.LocalizedName);
	}

	public void ClearFortressUnits()
	{
		IGameEntityRepositoryService service = this.gameService.Game.Services.GetService<IGameEntityRepositoryService>();
		if (this.StandardUnits.Count > 0)
		{
			Diagnostics.LogWarning("We are swapping a city which is not empty; this should not happen. All remaining units will be scraped.");
			while (this.StandardUnits.Count > 0)
			{
				Unit unit = this.StandardUnits[0];
				this.RemoveUnit(unit);
				service.Unregister(unit);
				unit.Dispose();
			}
		}
		if (this.Hero != null)
		{
			DepartmentOfEducation agency = this.Empire.GetAgency<DepartmentOfEducation>();
			if (agency != null)
			{
				agency.UnassignHero(this.Hero);
			}
		}
	}

	public override void CleanAfterEncounter(Encounter encounter)
	{
		global::Empire occupant = this.Occupant;
		bool flag = false;
		global::Empire empire = null;
		Contender contender = null;
		foreach (Contender contender2 in base.Encounter.Contenders)
		{
			if (!contender2.IsAttacking && contender2.Garrison.GUID == this.GUID)
			{
				DepartmentOfDefense agency = this.Empire.GetAgency<DepartmentOfDefense>();
				agency.UpdateLifeAfterEncounter(this);
				agency.CleanGarrisonAfterEncounter(this);
				if (base.Units.Count<Unit>() <= 0 && contender2.IsMainContender)
				{
					flag = true;
					foreach (Contender contender3 in base.Encounter.GetAlliedContendersFromContender(contender2))
					{
						if (contender3.IsTakingPartInBattle && contender3.ContenderState != ContenderState.Defeated)
						{
							flag = false;
						}
					}
				}
			}
			if (contender2.IsMainContender && contender2.IsAttacking)
			{
				empire = contender2.Empire;
				contender = contender2;
			}
		}
		if (flag && empire != null)
		{
			DepartmentOfTheInterior departmentOfTheInterior = null;
			if (empire is MajorEmpire)
			{
				departmentOfTheInterior = (empire as MajorEmpire).GetAgency<DepartmentOfTheInterior>();
			}
			else if (occupant is MajorEmpire)
			{
				departmentOfTheInterior = (occupant as MajorEmpire).GetAgency<DepartmentOfTheInterior>();
			}
			if (departmentOfTheInterior != null)
			{
				departmentOfTheInterior.SwapFortressOccupant(this, empire, new object[]
				{
					contender.Garrison as Army
				});
			}
		}
		base.LeaveEncounter();
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		this.facilities.Clear();
		this.Region = null;
		this.Empire = null;
	}

	private void OnFortressDistrictCollectionChange(CollectionChangeAction action, object element)
	{
		if (this.FortressDistrictCollectionChange != null)
		{
			this.FortressDistrictCollectionChange(this, new CollectionChangeEventArgs(action, element));
		}
	}

	public static readonly string ReadOnlyCategory = "Fortress";

	public static readonly string Citadel = "Citadel";

	public static readonly string Facility = "Facility";

	public static readonly string RevealedFacility = "RevealedFacility";

	public int MaximumNumberOfUnitsInGarrison = int.MaxValue;

	private MajorEmpire isOccupiedBy;

	private int isOccupiedByIndex = -1;

	private List<PointOfInterest> facilities = new List<PointOfInterest>();

	private Fortress.CardinalOrientation orientation = Fortress.CardinalOrientation.Undefined;

	private GameEntityGUID guid;

	private string userDefinedName;

	private IGameService gameService;

	public enum CardinalOrientation
	{
		Undefined = -1,
		North,
		South,
		East,
		West,
		Center
	}

	public class UniqueFacilityNames
	{
		public static readonly StaticString FacilityUnique1 = new StaticString("FacilityUnique1");

		public static readonly StaticString FacilityUnique2 = new StaticString("FacilityUnique2");

		public static readonly StaticString FacilityUnique3 = new StaticString("FacilityUnique3");

		public static readonly StaticString StockPile1 = new StaticString("FacilityUnique4");

		public static readonly StaticString StockPile2 = new StaticString("FacilityUnique5");

		public static readonly StaticString StockPile3 = new StaticString("FacilityUnique6");

		public static readonly StaticString FacilityUnique7 = new StaticString("FacilityUnique7");

		public static readonly StaticString FacilityUnique8 = new StaticString("FacilityUnique8");

		public static readonly StaticString FacilityUnique9 = new StaticString("FacilityUnique9");

		public static readonly StaticString WeatherControl = new StaticString("FacilityUnique10");
	}
}
