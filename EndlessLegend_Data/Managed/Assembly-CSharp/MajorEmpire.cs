using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class MajorEmpire : global::Empire
{
	public MajorEmpire(StaticString name, Faction faction, int color) : base(name, faction, color)
	{
		this.players = new List<Player>();
		this.TurnWhenLastBegun = -1;
	}

	protected MajorEmpire()
	{
		this.players = new List<Player>();
	}

	public event CollectionChangeEventHandler TamedKaijusCollectionChanged;

	public event CollectionChangeEventHandler ConvertedVillagesCollectionChanged;

	public event MajorEmpire.PlayerBondEventHandler OnPlayerBond;

	public override void ReadXml(XmlReader reader)
	{
		this.TurnWhenLastBegun = reader.GetAttribute<int>("TurnWhenLastBegun");
		int num = reader.ReadVersionAttribute();
		base.ReadXml(reader);
		if (reader.IsStartElement("VictoryConditionStatuses"))
		{
			int attribute = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("VictoryConditionStatuses");
			this.VictoryConditionStatuses.Clear();
			for (int i = 0; i < attribute; i++)
			{
				string attribute2 = reader.GetAttribute("Name");
				MajorEmpire.VictoryConditionStatus victoryConditionStatus = new MajorEmpire.VictoryConditionStatus();
				reader.ReadStartElement("VictoryConditionStatus");
				int attribute3 = reader.GetAttribute<int>("Count");
				victoryConditionStatus.LastTurnWhenAlertWasTriggered = new int[attribute3];
				reader.ReadStartElement("Alerts");
				for (int j = 0; j < attribute3; j++)
				{
					victoryConditionStatus.LastTurnWhenAlertWasTriggered[j] = reader.ReadElementString<int>("LastTurnWhenAlertWasTriggered");
				}
				reader.ReadEndElement("Alerts");
				int attribute4 = reader.GetAttribute<int>("Count");
				victoryConditionStatus.Variables = new float[attribute4];
				reader.ReadStartElement("Variables");
				for (int k = 0; k < attribute4; k++)
				{
					victoryConditionStatus.Variables[k] = reader.ReadElementString<float>("Value");
				}
				reader.ReadEndElement("Variables");
				reader.ReadEndElement();
				this.VictoryConditionStatuses.Add(attribute2, victoryConditionStatus);
			}
			reader.ReadEndElement("VictoryConditionStatuses");
		}
		if (num >= 2 && reader.IsStartElement("ConvertedVillages"))
		{
			this.convertedVillages = new List<Village>();
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			Diagnostics.Assert(service.Game != null);
			Diagnostics.Assert(service.Game is global::Game);
			int attribute5 = reader.GetAttribute<int>("Count");
			if (attribute5 > 0)
			{
				reader.ReadStartElement("ConvertedVillages");
				for (int l = 0; l < attribute5; l++)
				{
					ulong attribute6 = reader.GetAttribute<ulong>("GUID");
					Village village = new Village(attribute6)
					{
						Empire = this
					};
					reader.ReadElementSerializable<Village>(ref village);
					if (village != null)
					{
						this.convertedVillages.Add(village);
					}
				}
				reader.ReadEndElement("ConvertedVillages");
			}
			else
			{
				reader.Skip("ConvertedVillages");
			}
		}
		if (num >= 3)
		{
			if (reader.IsStartElement("TamedKaijus"))
			{
				this.tamedKaijus = new List<Kaiju>();
				IGameService service2 = Services.GetService<IGameService>();
				Diagnostics.Assert(service2 != null);
				Diagnostics.Assert(service2.Game != null);
				Diagnostics.Assert(service2.Game is global::Game);
				int attribute7 = reader.GetAttribute<int>("Count");
				if (attribute7 > 0)
				{
					reader.ReadStartElement("TamedKaijus");
					for (int m = 0; m < attribute7; m++)
					{
						Kaiju kaiju = new Kaiju();
						kaiju.MajorEmpire = this;
						kaiju.Empire = this;
						reader.ReadElementSerializable<Kaiju>(ref kaiju);
						if (kaiju != null)
						{
							this.tamedKaijus.Add(kaiju);
						}
					}
					reader.ReadEndElement("TamedKaijus");
				}
				else
				{
					reader.Skip("TamedKaijus");
				}
			}
			if (reader.IsStartElement("InfectedVillages"))
			{
				this.infectedVillages = new List<Village>();
				IGameService service3 = Services.GetService<IGameService>();
				Diagnostics.Assert(service3 != null);
				Diagnostics.Assert(service3.Game != null);
				Diagnostics.Assert(service3.Game is global::Game);
				int attribute8 = reader.GetAttribute<int>("Count");
				if (attribute8 > 0)
				{
					reader.ReadStartElement("InfectedVillages");
					for (int n = 0; n < attribute8; n++)
					{
						ulong attribute9 = reader.GetAttribute<ulong>("GUID");
						Village village2 = new Village(attribute9)
						{
							Empire = this
						};
						reader.ReadElementSerializable<Village>(ref village2);
						if (village2 != null)
						{
							this.InfectedVillages.Add(village2);
						}
					}
					reader.ReadEndElement("InfectedVillages");
				}
				else
				{
					reader.Skip("InfectedVillages");
				}
			}
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<int>("TurnWhenLastBegun", this.TurnWhenLastBegun);
		int num = writer.WriteVersionAttribute(3);
		base.WriteXml(writer);
		writer.WriteStartElement("VictoryConditionStatuses");
		if (this.VictoryConditionStatuses != null)
		{
			writer.WriteAttributeString<int>("Count", this.VictoryConditionStatuses.Count);
			foreach (KeyValuePair<string, MajorEmpire.VictoryConditionStatus> keyValuePair in this.VictoryConditionStatuses)
			{
				writer.WriteStartElement("VictoryConditionStatus");
				writer.WriteAttributeString("Name", keyValuePair.Key);
				writer.WriteStartElement("Alerts");
				if (keyValuePair.Value.LastTurnWhenAlertWasTriggered != null)
				{
					writer.WriteAttributeString<int>("Count", keyValuePair.Value.LastTurnWhenAlertWasTriggered.Length);
					for (int i = 0; i < keyValuePair.Value.LastTurnWhenAlertWasTriggered.Length; i++)
					{
						writer.WriteElementString<int>("LastTurnWhenAlertWasTriggered", keyValuePair.Value.LastTurnWhenAlertWasTriggered[i]);
					}
				}
				else
				{
					writer.WriteAttributeString<int>("Count", 0);
				}
				writer.WriteEndElement();
				writer.WriteStartElement("Variables");
				if (keyValuePair.Value.Variables != null)
				{
					writer.WriteAttributeString<int>("Count", keyValuePair.Value.Variables.Length);
					for (int j = 0; j < keyValuePair.Value.Variables.Length; j++)
					{
						writer.WriteElementString<float>("Value", keyValuePair.Value.Variables[j]);
					}
				}
				else
				{
					writer.WriteAttributeString<int>("Count", 0);
				}
				writer.WriteEndElement();
				writer.WriteEndElement();
			}
		}
		else
		{
			writer.WriteAttributeString<int>("Count", 0);
		}
		writer.WriteEndElement();
		if (num >= 2)
		{
			writer.WriteStartElement("ConvertedVillages");
			if (this.convertedVillages != null)
			{
				writer.WriteAttributeString<int>("Count", this.convertedVillages.Count);
				for (int k = 0; k < this.convertedVillages.Count; k++)
				{
					IXmlSerializable xmlSerializable = this.convertedVillages[k];
					writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
				}
			}
			else
			{
				writer.WriteAttributeString<int>("Count", 0);
			}
			writer.WriteEndElement();
		}
		if (num >= 3)
		{
			writer.WriteStartElement("TamedKaijus");
			if (this.tamedKaijus != null)
			{
				writer.WriteAttributeString<int>("Count", this.tamedKaijus.Count);
				for (int l = 0; l < this.tamedKaijus.Count; l++)
				{
					IXmlSerializable xmlSerializable2 = this.tamedKaijus[l];
					writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable2);
				}
			}
			else
			{
				writer.WriteAttributeString<int>("Count", 0);
			}
			writer.WriteEndElement();
			writer.WriteStartElement("InfectedVillages");
			if (this.InfectedVillages != null)
			{
				writer.WriteAttributeString<int>("Count", this.InfectedVillages.Count);
				for (int m = 0; m < this.InfectedVillages.Count; m++)
				{
					IXmlSerializable xmlSerializable3 = this.InfectedVillages[m];
					writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable3);
				}
			}
			else
			{
				writer.WriteAttributeString<int>("Count", 0);
			}
			writer.WriteEndElement();
		}
	}

	public List<Kaiju> TamedKaijus
	{
		get
		{
			if (this.tamedKaijus == null)
			{
				this.tamedKaijus = new List<Kaiju>();
			}
			return this.tamedKaijus;
		}
	}

	public ReadOnlyCollection<Kaiju> RootedKaijus
	{
		get
		{
			List<Kaiju> list = new List<Kaiju>();
			for (int i = 0; i < this.tamedKaijus.Count; i++)
			{
				if (this.tamedKaijus[i].OnGarrisonMode())
				{
					list.Add(this.tamedKaijus[i]);
				}
			}
			return list.AsReadOnly();
		}
	}

	public void AddTamedKaiju(Kaiju kaiju)
	{
		if (!this.TamedKaijus.Contains(kaiju))
		{
			IDatabase<SimulationDescriptor> database = Databases.GetDatabase<SimulationDescriptor>(false);
			SimulationDescriptor descriptor = null;
			if (database.TryGetValue(kaiju.KaijuEmpire.KaijuFaction.EmpireTamedKaijuDescriptor, out descriptor))
			{
				base.SimulationObject.AddDescriptor(descriptor);
			}
			this.TamedKaijus.Add(kaiju);
			base.AddChild(kaiju);
			if (kaiju.KaijuGarrison != null)
			{
				base.AddChild(kaiju.KaijuGarrison);
			}
			if (kaiju.KaijuArmy != null)
			{
				base.AddChild(kaiju.KaijuArmy);
				base.GetAgency<DepartmentOfDefense>().AddArmy(kaiju.KaijuArmy);
			}
			kaiju.Refresh(false);
			this.Refresh(false);
			if (this.TamedKaijusCollectionChanged != null)
			{
				this.TamedKaijusCollectionChanged(this, new CollectionChangeEventArgs(CollectionChangeAction.Add, kaiju));
			}
		}
	}

	public void ServerUntameAllKaijus()
	{
		global::PlayerController server = base.PlayerControllers.Server;
		if (server == null)
		{
			return;
		}
		for (int i = this.TamedKaijus.Count - 1; i >= 0; i--)
		{
			OrderUntameKaiju order = new OrderUntameKaiju(this.TamedKaijus[i], true);
			server.PostOrder(order);
		}
	}

	public void RelocateKaiju(Kaiju kaiju, WorldPosition targetPosition, StaticString garrisonActionName)
	{
		if (!this.TamedKaijus.Contains(kaiju))
		{
			return;
		}
		OrderRelocateKaiju orderRelocateKaiju;
		if (!StaticString.IsNullOrEmpty(garrisonActionName))
		{
			orderRelocateKaiju = new OrderRelocateKaiju(kaiju.GUID, targetPosition, garrisonActionName);
		}
		else
		{
			orderRelocateKaiju = new OrderRelocateKaiju(kaiju.GUID, targetPosition);
		}
		if (orderRelocateKaiju != null)
		{
			IGameService service = Services.GetService<IGameService>();
			IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
			service2.ActivePlayerController.PostOrder(orderRelocateKaiju);
		}
	}

	public void RemoveTamedKaiju(Kaiju kaiju)
	{
		if (this.TamedKaijus.Contains(kaiju))
		{
			IDatabase<SimulationDescriptor> database = Databases.GetDatabase<SimulationDescriptor>(false);
			SimulationDescriptor descriptor = null;
			if (database.TryGetValue(kaiju.KaijuEmpire.KaijuFaction.EmpireTamedKaijuDescriptor, out descriptor))
			{
				base.SimulationObject.RemoveDescriptor(descriptor);
			}
			this.TamedKaijus.Remove(kaiju);
			base.RemoveChild(kaiju);
			if (kaiju.KaijuGarrison != null)
			{
				base.RemoveChild(kaiju.KaijuGarrison);
			}
			if (kaiju.KaijuArmy != null)
			{
				base.RemoveChild(kaiju.KaijuArmy);
				base.GetAgency<DepartmentOfDefense>().RemoveArmy(kaiju.KaijuArmy, false);
			}
			kaiju.Refresh(false);
			this.Refresh(false);
			if (this.TamedKaijusCollectionChanged != null)
			{
				this.TamedKaijusCollectionChanged(this, new CollectionChangeEventArgs(CollectionChangeAction.Remove, kaiju));
			}
		}
	}

	private IEnumerator GameClientState_Turn_Begin_TamedKaijus(string context, string pass)
	{
		foreach (Kaiju kaiju in this.TamedKaijus)
		{
			kaiju.GameClientState_Turn_Begin();
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_TamedKaijus(string context, string pass)
	{
		foreach (Kaiju kaiju in this.TamedKaijus)
		{
			kaiju.GameClientState_Turn_Ended();
		}
		yield break;
	}

	private IEnumerator GameServerState_Turn_Begin_TamedKaijus(string context, string pass)
	{
		foreach (Kaiju kaiju in this.TamedKaijus)
		{
			kaiju.GameServerState_Turn_Begin();
		}
		yield break;
	}

	private IEnumerator GameServerState_Turn_Ended_TamedKaijus(string context, string pass)
	{
		foreach (Kaiju kaiju in this.TamedKaijus)
		{
			kaiju.GameServerState_Turn_End();
		}
		yield break;
	}

	public Dictionary<string, MajorEmpire.VictoryConditionStatus> VictoryConditionStatuses
	{
		get
		{
			return this.victoryConditionStatuses;
		}
	}

	public List<Village> ConvertedVillages
	{
		get
		{
			if (this.convertedVillages == null)
			{
				this.convertedVillages = new List<Village>();
			}
			return this.convertedVillages;
		}
	}

	public List<Village> InfectedVillages
	{
		get
		{
			if (this.infectedVillages == null)
			{
				this.infectedVillages = new List<Village>();
			}
			return this.infectedVillages;
		}
	}

	public int ArmiesInfiltrationBits { get; set; }

	public ReadOnlyCollection<Player> Players
	{
		get
		{
			return this.players.AsReadOnly();
		}
	}

	public GameScores GameScores { get; private set; }

	public bool IsEliminated
	{
		get
		{
			return base.SimulationObject.Tags.Contains(global::Empire.TagEmpireEliminated);
		}
	}

	public override string LocalizedName
	{
		get
		{
			string text = string.Empty;
			foreach (Player player in this.players)
			{
				PlayerType type = player.Type;
				if (type != PlayerType.AI)
				{
					if (type != PlayerType.Human)
					{
						throw new ArgumentOutOfRangeException();
					}
					string key = (text.Length != 0) ? "%EmpireNameFormatAdditionnalHuman" : "%EmpireNameFormatHuman";
					text += AgeLocalizer.Instance.LocalizeString(key).Replace("$PlayerName", player.LocalizedName);
				}
				else
				{
					text = MajorEmpire.GenerateAIName(base.Faction.Affinity.Name, base.Index);
				}
			}
			return text;
		}
		set
		{
			throw new InvalidOperationException();
		}
	}

	public int TurnWhenLastBegun { get; private set; }

	public static string GenerateBasicAIName(int index)
	{
		return AgeLocalizer.Instance.LocalizeString("%EmpireNameFormatAISimple").Replace("$Index", index.ToString());
	}

	public static string GenerateAIName(string affinityName, int index)
	{
		return AgeLocalizer.Instance.LocalizeString("%EmpireNameFormatAI").Replace("$Affinity", AgeLocalizer.Instance.LocalizeString("%AILeader" + affinityName + index.ToString()));
	}

	public void AddConvertedVillage(Village village)
	{
		if (!this.ConvertedVillages.Contains(village))
		{
			this.ConvertedVillages.Add(village);
			base.AddChild(village);
			if (this.ConvertedVillagesCollectionChanged != null)
			{
				this.ConvertedVillagesCollectionChanged(this, new CollectionChangeEventArgs(CollectionChangeAction.Add, village));
			}
		}
	}

	public void RemoveConvertedVillage(Village village)
	{
		if (this.ConvertedVillages.Contains(village))
		{
			this.ConvertedVillages.Remove(village);
			base.RemoveChild(village);
			if (this.ConvertedVillagesCollectionChanged != null)
			{
				this.ConvertedVillagesCollectionChanged(this, new CollectionChangeEventArgs(CollectionChangeAction.Remove, village));
			}
		}
	}

	public void AddInfectedVillage(Village village)
	{
		if (!this.InfectedVillages.Contains(village))
		{
			this.InfectedVillages.Add(village);
		}
	}

	public void RemoveInfectedVillage(Village village)
	{
		if (this.InfectedVillages.Contains(village))
		{
			this.InfectedVillages.Remove(village);
		}
	}

	public Village GetInfectedVillageAt(WorldPosition worldPosition)
	{
		for (int i = 0; i < this.InfectedVillages.Count; i++)
		{
			if (this.InfectedVillages[i].WorldPosition == worldPosition)
			{
				return this.InfectedVillages[i];
			}
		}
		return null;
	}

	public void BindPlayer(Player player)
	{
		Diagnostics.Assert(player != null);
		switch (player.Type)
		{
		case PlayerType.Unset:
			throw new GameException("Player type is unset.");
		case PlayerType.AI:
			base.IsControlledByAI = true;
			break;
		case PlayerType.Human:
			base.IsControlledByAI = false;
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		global::Game game = service.Game as global::Game;
		Diagnostics.Assert(game != null);
		base.UpdateGameModifiers(game);
		this.players.Add(player);
		IPlayerRepositoryService service2 = game.GetService<IPlayerRepositoryService>();
		Diagnostics.Assert(service2 != null);
		service2.Register(player);
		if (this.OnPlayerBond != null)
		{
			this.OnPlayerBond(this, player);
		}
	}

	public void UnbindPlayer(Player player)
	{
		this.players.Remove(player);
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		IPlayerRepositoryService service2 = (service.Game as global::Game).GetService<IPlayerRepositoryService>();
		Diagnostics.Assert(service2 != null);
		service2.Unregister(player);
	}

	public void UnconvertAndPacifyAllConvertedVillages()
	{
		for (int i = this.ConvertedVillages.Count - 1; i >= 0; i--)
		{
			Village village = this.ConvertedVillages[i];
			if (village != null && village.Region != null)
			{
				BarbarianCouncil agency = village.Region.MinorEmpire.GetAgency<BarbarianCouncil>();
				if (agency != null)
				{
					agency.PacifyVillage(village, null);
				}
				DepartmentOfTheInterior agency2 = base.GetAgency<DepartmentOfTheInterior>();
				Diagnostics.Assert(agency2 != null);
				if (village.Region.City != null && agency2 != null && agency2.MainCity != village.Region.City)
				{
					DepartmentOfTheInterior agency3 = village.Region.City.Empire.GetAgency<DepartmentOfTheInterior>();
					Diagnostics.Assert(agency3 != null);
					agency3.BindMinorFactionToCity(village.Region.City, village.Region.MinorEmpire);
					agency3.VerifyOverallPopulation(village.Region.City);
				}
				else
				{
					agency2.UnbindConvertedVillage(village);
				}
			}
		}
	}

	internal override void OnEmpireEliminated(global::Empire empire, bool authorized)
	{
		if (empire.Index == base.Index && this.ConvertedVillages.Count > 0)
		{
			this.UnconvertAndPacifyAllConvertedVillages();
		}
		base.OnEmpireEliminated(empire, authorized);
	}

	protected override void CreateAgencies()
	{
		base.CreateAgencies();
		this.AddAgency(new DepartmentOfDefense(this));
		this.AddAgency(new DepartmentOfIndustry(this));
		this.AddAgency(new DepartmentOfTheTreasury(this));
		this.AddAgency(new DepartmentOfTransportation(this));
		this.AddAgency(new DepartmentOfTheInterior(this));
		this.AddAgency(new DepartmentOfForeignAffairs(this));
		this.AddAgency(new DepartmentOfHealth(this));
		this.AddAgency(new DepartmentOfScience(this));
		this.AddAgency(new DepartmentOfPlanificationAndDevelopment(this));
		this.AddAgency(new DepartmentOfEducation(this));
		this.AddAgency(new DepartmentOfInternalAffairs(this));
		if (this.downloadableContentService.IsShared(DownloadableContent11.ReadOnlyName))
		{
			this.AddAgency(new DepartmentOfIntelligence(this));
		}
		if (this.downloadableContentService.IsShared(DownloadableContent20.ReadOnlyName))
		{
			this.AddAgency(new DepartmentOfKaijuTechs(this));
			this.AddAgency(new DepartmentOfCreepingNodes(this));
		}
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
	}

	protected override IEnumerator OnInitialize()
	{
		this.GameScores = new GameScores();
		base.RegisterPass("GameServerState_Turn_Begin", "Interact", new Agency.Action(this.GameServerState_Turn_Begin_Interact), new string[0]);
		base.RegisterPass("GameServerState_Turn_Begin", "SpawnConvertedVillageUnits", new Agency.Action(this.GameServerState_Turn_Begin_SpawnConvertedVillageUnits), new string[0]);
		base.RegisterPass("GameServerState_Turn_Begin", "ServerMajorEmpireTurnBeginKaijus", new Agency.Action(this.GameServerState_Turn_Begin_TamedKaijus), new string[0]);
		base.RegisterPass("GameServerState_Turn_Begin", "ServerMajorEmpireTurnEndedKaijus", new Agency.Action(this.GameServerState_Turn_Ended_TamedKaijus), new string[0]);
		base.RegisterPass("GameClientState_Turn_Begin", "ClientMajorEmpireTurnBeginKaijus", new Agency.Action(this.GameClientState_Turn_Begin_TamedKaijus), new string[0]);
		base.RegisterPass("GameClientState_Turn_End", "ClientMajorEmpireTurnEndedKaijus", new Agency.Action(this.GameClientState_Turn_End_TamedKaijus), new string[0]);
		this.downloadableContentService = Services.GetService<IDownloadableContentService>();
		yield return base.OnInitialize();
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		if (this.convertedVillages != null)
		{
			for (int index = 0; index < this.convertedVillages.Count; index++)
			{
				Village village = this.convertedVillages[index];
				village.Empire = null;
				Diagnostics.Assert(village.PointOfInterest != null);
				Diagnostics.Assert(village.PointOfInterest.Region != null);
				MinorEmpire minorEmpire = village.PointOfInterest.Region.MinorEmpire;
				Diagnostics.Assert(minorEmpire != null);
				BarbarianCouncil barbarianCouncil = minorEmpire.GetAgency<BarbarianCouncil>();
				Diagnostics.Assert(barbarianCouncil != null);
				barbarianCouncil.AddVillage(village);
				barbarianCouncil.AddConvertVillageOnLoad(village, this);
				DepartmentOfTheInterior.GenerateFIMSEForConvertedVillage(this, village.PointOfInterest);
			}
		}
		if (this.InfectedVillages != null)
		{
			for (int index2 = 0; index2 < this.InfectedVillages.Count; index2++)
			{
				Village village2 = this.InfectedVillages[index2];
				village2.Empire = null;
				Diagnostics.Assert(village2.PointOfInterest != null);
				Diagnostics.Assert(village2.PointOfInterest.Region != null);
				MinorEmpire minorEmpire2 = village2.PointOfInterest.Region.MinorEmpire;
				Diagnostics.Assert(minorEmpire2 != null);
				BarbarianCouncil barbarianCouncil2 = minorEmpire2.GetAgency<BarbarianCouncil>();
				Diagnostics.Assert(barbarianCouncil2 != null);
				barbarianCouncil2.AddVillage(village2);
			}
		}
		yield return base.OnLoad();
		yield break;
	}

	protected override IEnumerator OnLoadGame(Amplitude.Unity.Game.Game game)
	{
		IGameEntityRepositoryService gameEntityRepositoryService = game.Services.GetService<IGameEntityRepositoryService>();
		Diagnostics.Assert(gameEntityRepositoryService != null);
		IWorldPositionningService worldPositionningService = game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(worldPositionningService != null);
		if (this.TamedKaijus != null)
		{
			for (int index = 0; index < this.TamedKaijus.Count; index++)
			{
				Kaiju kaiju = this.TamedKaijus[index];
				if (kaiju != null)
				{
					KaijuEmpire kaijuEmpire = (game as global::Game).Empires[kaiju.KaijuEmpireIndex] as KaijuEmpire;
					kaiju.KaijuEmpire = kaijuEmpire;
					yield return kaiju.OnLoadGame(game);
					kaiju.MajorEmpire = this;
					base.AddChild(kaiju);
					gameEntityRepositoryService.Register(kaiju);
					DepartmentOfTheInterior.GenerateResourcesLeechingForTamedKaijus(kaiju);
				}
			}
			this.Refresh(false);
		}
		yield return base.OnLoadGame(game);
		yield break;
	}

	protected override void OnRelease()
	{
		base.OnRelease();
		if (this.TamedKaijus != null && this.TamedKaijus.Count > 0)
		{
			for (int i = 0; i < this.TamedKaijus.Count; i++)
			{
				this.TamedKaijus[i].Release();
			}
		}
	}

	private IEnumerator GameServerState_Turn_Begin_Interact(string context, string pass)
	{
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		Diagnostics.Assert(gameService.Game != null);
		global::Game game = gameService.Game as global::Game;
		if (game != null && game.Turn > this.TurnWhenLastBegun)
		{
			this.TurnWhenLastBegun = game.Turn;
			OrderInteractWith order = new OrderInteractWith(base.Index, GameEntityGUID.Zero, "BeginTurn");
			order.WorldPosition = WorldPosition.Invalid;
			order.Tags.AddTag("BeginTurn");
			if (global::GameManager.Preferences.QuestVerboseMode)
			{
				Diagnostics.Log("[Quest] Trying to trigger quests with event BeginTurn for empire {0}", new object[]
				{
					order.EmpireIndex
				});
			}
			IPlayerControllerRepositoryControl playerControllerRepositoryService = gameService.Game.Services.GetService<IPlayerControllerRepositoryService>() as IPlayerControllerRepositoryControl;
			playerControllerRepositoryService.GetPlayerControllerById("server").PostOrder(order);
		}
		yield return null;
		yield break;
	}

	private IEnumerator GameServerState_Turn_Begin_SpawnConvertedVillageUnits(string context, string pass)
	{
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		Diagnostics.Assert(gameService.Game != null);
		int currentTurn = (gameService.Game as global::Game).Turn;
		WeakReference<ISeasonService> seasonService = new WeakReference<ISeasonService>(gameService.Game.Services.GetService<ISeasonService>());
		foreach (Village village in this.ConvertedVillages)
		{
			if (village.ConvertedUnitSpawnTurn == currentTurn)
			{
				OrderSpawnConvertedVillageUnit order = new OrderSpawnConvertedVillageUnit(village.Converter.Index, 1, village.GUID);
				village.Converter.PlayerControllers.Server.PostOrder(order);
			}
			village.Refresh(false);
		}
		yield return null;
		yield break;
	}

	private List<Kaiju> tamedKaijus;

	private Dictionary<string, MajorEmpire.VictoryConditionStatus> victoryConditionStatuses = new Dictionary<string, MajorEmpire.VictoryConditionStatus>();

	private List<Player> players;

	private IDownloadableContentService downloadableContentService;

	private List<Village> convertedVillages;

	private List<Village> infectedVillages;

	public class VictoryConditionStatus
	{
		public int[] LastTurnWhenAlertWasTriggered { get; set; }

		public float[] Variables { get; set; }
	}

	public delegate void PlayerBondEventHandler(MajorEmpire empire, Player player);
}
