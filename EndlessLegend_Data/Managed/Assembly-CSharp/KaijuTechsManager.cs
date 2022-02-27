using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Utilities.Maps;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class KaijuTechsManager : GameAncillary, Amplitude.Xml.Serialization.IXmlSerializable, IService, IKaijuTechsService
{
	public KaijuTechsManager()
	{
		this.researchQueues = new Dictionary<int, ConstructionQueue>();
		this.researchedTechNames = new List<string>();
		this.researchedTechs = new List<ConstructibleElement>();
	}

	public event EventHandler<ConstructibleElementEventArgs> KaijuTechnologyUnlocked;

	public event EventHandler<ConstructionChangeEventArgs> ResearchQueueChanged;

	private XmlSerializer XmlSerializer { get; set; }

	public virtual void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		reader.ReadStartElement();
		this.researchQueues.Clear();
		int num2 = reader.ReadElementString<int>("QueuesCount");
		reader.ReadStartElement("ResearchQueues");
		for (int i = 0; i < num2; i++)
		{
			reader.ReadStartElement("Queue");
			int key = reader.ReadElementString<int>("EmpireIndex");
			ConstructionQueue constructionQueue = new ConstructionQueue();
			Amplitude.Xml.Serialization.IXmlSerializable xmlSerializable = constructionQueue;
			reader.ReadElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>("Researches", ref xmlSerializable);
			for (int j = constructionQueue.Length - 1; j >= 0; j--)
			{
				Construction construction = constructionQueue.PeekAt(j);
				if (construction.ConstructibleElement == null)
				{
					Diagnostics.LogWarning("Compatibility issue, constructible element (name: '{0}') is null.", new object[]
					{
						construction.ConstructibleElementName
					});
					constructionQueue.Remove(construction);
				}
			}
			this.researchQueues.Add(key, constructionQueue);
			reader.ReadEndElement("Queue");
		}
		reader.ReadEndElement("ResearchQueues");
		int num3 = reader.ReadElementString<int>("ResearchedTechsCount");
		reader.ReadStartElement("ResearchedTechs");
		for (int k = 0; k < num3; k++)
		{
			this.researchedTechNames.Add(reader.ReadElementString("TechName"));
		}
		reader.ReadEndElement("ResearchedTechs");
	}

	public virtual void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString("AssemblyQualifiedName", base.GetType().AssemblyQualifiedName);
		writer.WriteVersionAttribute(1);
		writer.WriteElementString<int>("QueuesCount", this.researchQueues.Count);
		writer.WriteStartElement("ResearchQueues");
		foreach (KeyValuePair<int, ConstructionQueue> keyValuePair in this.researchQueues)
		{
			writer.WriteStartElement("Queue");
			writer.WriteElementString<int>("EmpireIndex", keyValuePair.Key);
			Amplitude.Xml.Serialization.IXmlSerializable value = keyValuePair.Value;
			writer.WriteElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>("Researches", ref value);
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
		writer.WriteElementString<int>("ResearchedTechsCount", this.researchedTechNames.Count);
		writer.WriteStartElement("ResearchedTechs");
		for (int i = 0; i < this.researchedTechNames.Count; i++)
		{
			writer.WriteElementString("TechName", this.researchedTechNames[i]);
		}
		writer.WriteEndElement();
	}

	private PlayerController PlayerController
	{
		get
		{
			IPlayerControllerRepositoryService service = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
			IPlayerControllerRepositoryControl playerControllerRepositoryControl = service as IPlayerControllerRepositoryControl;
			if (playerControllerRepositoryControl == null)
			{
				Diagnostics.LogError("Fail getting PlayerController !");
			}
			return playerControllerRepositoryControl.GetPlayerControllerById("server");
		}
	}

	public override IEnumerator BindServices(IServiceContainer serviceContainer)
	{
		yield return base.BindServices(serviceContainer);
		this.eventService = Services.GetService<IEventService>();
		if (this.eventService == null)
		{
			Diagnostics.LogError("Wasn't able to find the event service.");
		}
		serviceContainer.AddService<IKaijuTechsService>(this);
		yield break;
	}

	public override IEnumerator Ignite(IServiceContainer serviceContainer)
	{
		yield return base.Ignite(serviceContainer);
		yield break;
	}

	public override IEnumerator LoadGame(Game game)
	{
		this.technologyDatabase = Databases.GetDatabase<DepartmentOfScience.ConstructibleElement>(false);
		if (this.technologyDatabase == null)
		{
			Diagnostics.LogError("Wasn't able to find the technology database.");
		}
		this.researchQueues.Clear();
		if (this.researchQueues.Count == 0)
		{
			Empire[] array = Array.FindAll<Empire>(base.Game.Empires, (Empire empire) => empire is MajorEmpire);
			for (int i = 0; i < array.Length; i++)
			{
				this.researchQueues.Add(array[i].Index, new ConstructionQueue());
			}
		}
		if (this.researchedTechNames.Count > 0)
		{
			for (int j = 0; j < this.researchedTechNames.Count; j++)
			{
				DepartmentOfScience.ConstructibleElement item = null;
				if (this.technologyDatabase.TryGetValue(this.researchedTechNames[j], out item))
				{
					this.researchedTechs.Add(item);
				}
			}
		}
		this.AssignLuxuryTypes(KaijuTechsManager.ComputeLuxuryAbundance(game));
		yield return base.LoadGame(game);
		yield break;
	}

	protected override void Releasing()
	{
		base.Releasing();
		this.eventService = null;
		this.technologyDatabase = null;
		this.researchQueues.Clear();
		this.researchedTechs.Clear();
		this.researchedTechNames.Clear();
	}

	public ConstructibleElement[] GetAvailableTechs(Empire empire)
	{
		List<ConstructibleElement> list = new List<ConstructibleElement>();
		foreach (DepartmentOfScience.ConstructibleElement constructibleElement in this.technologyDatabase.GetValues())
		{
			if (constructibleElement.TechnologyFlags == DepartmentOfScience.ConstructibleElement.TechnologyFlag.KaijuUnlock)
			{
				if (this.GetTechnologyState(constructibleElement, empire) == DepartmentOfScience.ConstructibleElement.State.Available)
				{
					list.Add(constructibleElement);
				}
			}
		}
		return list.ToArray();
	}

	public ConstructionQueue GetConstructionQueueForEmpire(Empire empire)
	{
		ConstructionQueue result = null;
		if (!this.researchQueues.TryGetValue(empire.Index, out result))
		{
			Diagnostics.LogError("The provided empire does not have a construction queue. Make sure one is created.");
		}
		return result;
	}

	public List<ConstructibleElement> GetResearchedTechs()
	{
		return this.researchedTechs;
	}

	public DepartmentOfScience.ConstructibleElement.State GetTechnologyState(ConstructibleElement technology, Empire empire)
	{
		if (technology == null)
		{
			throw new ArgumentNullException("technology");
		}
		ConstructionQueue constructionQueue = null;
		if (!this.researchQueues.TryGetValue(empire.Index, out constructionQueue))
		{
			Diagnostics.LogError("The provided empire does not have a construction queue. Make sure one is created.");
			return DepartmentOfScience.ConstructibleElement.State.NotAvailable;
		}
		if (this.researchedTechNames.Contains(technology.Name))
		{
			if (empire is MajorEmpire && !DepartmentOfTheTreasury.CheckConstructiblePrerequisites(empire, technology, new string[]
			{
				ConstructionFlags.Prerequisite
			}))
			{
				return DepartmentOfScience.ConstructibleElement.State.ResearchedButUnavailable;
			}
			return DepartmentOfScience.ConstructibleElement.State.Researched;
		}
		else
		{
			Diagnostics.Assert(constructionQueue != null);
			Construction construction = constructionQueue.Get(technology);
			if (construction != null && construction.ConstructibleElement.Name == technology.Name)
			{
				return DepartmentOfScience.ConstructibleElement.State.InProgress;
			}
			if (constructionQueue.Contains(technology))
			{
				return DepartmentOfScience.ConstructibleElement.State.Queued;
			}
			if (!DepartmentOfTheTreasury.CheckConstructiblePrerequisites(empire, technology, new string[]
			{
				ConstructionFlags.Prerequisite
			}))
			{
				return DepartmentOfScience.ConstructibleElement.State.NotAvailable;
			}
			return DepartmentOfScience.ConstructibleElement.State.Available;
		}
	}

	public void InvokeResearchQueueChanged(Construction contruction, ConstructionChangeEventAction action)
	{
		if (this.ResearchQueueChanged != null)
		{
			this.ResearchQueueChanged(this, new ConstructionChangeEventArgs(action, null, contruction));
		}
	}

	public void UnlockTechnology(ConstructibleElement technology, Empire empire)
	{
		if (technology == null)
		{
			throw new ArgumentNullException("technology");
		}
		if (this.GetTechnologyState(technology, empire) == DepartmentOfScience.ConstructibleElement.State.Researched)
		{
			return;
		}
		ConstructionQueue constructionQueueForEmpire = this.GetConstructionQueueForEmpire(empire);
		if (constructionQueueForEmpire.Contains(technology))
		{
			constructionQueueForEmpire.Remove(technology);
		}
		this.researchedTechNames.Add(technology.Name);
		this.researchedTechs.Add(technology);
		if (this.KaijuTechnologyUnlocked != null)
		{
			this.KaijuTechnologyUnlocked(this, new ConstructibleElementEventArgs(technology));
		}
	}

	private void AssignLuxuryTypes(Dictionary<string, List<PointOfInterestTemplate>> availableLuxuries)
	{
		Random random = new Random(World.Seed);
		foreach (DepartmentOfScience.ConstructibleElement constructibleElement in this.technologyDatabase)
		{
			if (constructibleElement.TechnologyFlags == DepartmentOfScience.ConstructibleElement.TechnologyFlag.KaijuUnlock)
			{
				KaijuTechnologyDefinition kaijuTechnologyDefinition = constructibleElement as KaijuTechnologyDefinition;
				KaijuUnlockCost kaijuUnlockCost = kaijuTechnologyDefinition.KaijuUnlockCost;
				if (kaijuUnlockCost != null)
				{
					List<PointOfInterestTemplate> list = new List<PointOfInterestTemplate>();
					list.AddRange(availableLuxuries[kaijuUnlockCost.LuxuryTier]);
					if (kaijuUnlockCost.LuxuryTier != "Tier3" && availableLuxuries[kaijuUnlockCost.LuxuryTier].Count <= 2)
					{
						list.AddRange((!(kaijuUnlockCost.LuxuryTier == "Tier1")) ? availableLuxuries["Tier1"] : availableLuxuries["Tier2"]);
					}
					PointOfInterestTemplate pointOfInterestTemplate = list[random.Next(list.Count)];
					string empty = string.Empty;
					if (pointOfInterestTemplate.Properties.TryGetValue("ResourceName", out empty))
					{
						kaijuTechnologyDefinition.SetKaijuUnlockCostResourceName(empty);
					}
				}
			}
		}
	}

	public static Dictionary<string, List<PointOfInterestTemplate>> ComputeLuxuryAbundance(Game game)
	{
		GridMap<PointOfInterest> gridMap = game.World.Atlas.GetMap(WorldAtlas.Maps.PointOfInterest) as GridMap<PointOfInterest>;
		List<PointOfInterestTemplate> source = (from POI in gridMap.Data
		where POI != null && POI.IsLuxuryDeposit()
		select POI.PointOfInterestDefinition.PointOfInterestTemplate).ToList<PointOfInterestTemplate>();
		Dictionary<string, List<PointOfInterestTemplate>> dictionary = new Dictionary<string, List<PointOfInterestTemplate>>();
		dictionary.Add("Tier1", new List<PointOfInterestTemplate>());
		dictionary.Add("Tier2", new List<PointOfInterestTemplate>());
		dictionary.Add("Tier3", new List<PointOfInterestTemplate>());
		foreach (PointOfInterestTemplate pointOfInterestTemplate in source.Distinct<PointOfInterestTemplate>())
		{
			string empty = string.Empty;
			pointOfInterestTemplate.Properties.TryGetValue("LuxuryTier", out empty);
			if (dictionary.ContainsKey(empty))
			{
				dictionary[empty].Add(pointOfInterestTemplate);
			}
			else
			{
				Diagnostics.LogError("Luxury resource " + pointOfInterestTemplate.Name + " doesn't have tier data!");
			}
		}
		return dictionary;
	}

	public void EmptyConstructionQueueForEmpire(Empire empire)
	{
		IPlayerControllerRepositoryService service = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
		ConstructionQueue constructionQueueForEmpire = this.GetConstructionQueueForEmpire(empire);
		for (int i = constructionQueueForEmpire.Length - 1; i >= 0; i--)
		{
			Construction construction = constructionQueueForEmpire.PeekAt(i);
			OrderCancelKaijuResearch order = new OrderCancelKaijuResearch(empire.Index, construction.GUID);
			service.ActivePlayerController.PostOrder(order);
		}
	}

	private static object[] kaijuTechCostFormula;

	private static InterpreterContext kaijuTechInterpreterContext;

	private IEventService eventService;

	private IDatabase<DepartmentOfScience.ConstructibleElement> technologyDatabase;

	private Dictionary<int, ConstructionQueue> researchQueues;

	private List<string> researchedTechNames;

	private List<ConstructibleElement> researchedTechs;
}
