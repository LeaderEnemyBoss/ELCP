using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Amplitude;
using Amplitude.IO;
using Amplitude.Unity.Achievement;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Session;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Xml;
using Amplitude.Utilities.Maps;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class Game : Amplitude.Unity.Game.Game, IXmlSerializable, IDumpable
{
	public void DumpAsText(StringBuilder content, string indent = "")
	{
		global::Empire[] array = (from e in this.Empires
		where e is MajorEmpire
		select e).ToArray<global::Empire>();
		foreach (global::Empire empire in this.Empires)
		{
			empire.Refresh(true);
		}
		Func<string, StringBuilder> func = (string s) => content.Append(string.Format("\r\n===== {0} ========================================", s.ToUpper()).Substring(0, 40) + "\r\n");
		func("application");
		content.AppendFormat("Application = \"{0}\"\r\nVersion = \"{1}\"\r\n", Amplitude.Unity.Framework.Application.Name, Amplitude.Unity.Framework.Application.Version.ToString("V{0}.{1}.{2} S{3} {4}"));
		func("players");
		IPlayerRepositoryService service = this.GetService<IPlayerRepositoryService>();
		Diagnostics.Assert(service != null);
		foreach (Player player in from p in service
		orderby p.Empire.Index
		select p)
		{
			content.AppendFormat("[Empire#{0}] {1} ({2}) {3}\r\n", new object[]
			{
				player.Empire.Index,
				(player.Type != PlayerType.Human) ? "AI" : player.LocalizedName,
				player.Type,
				(!(player.SteamID != null)) ? string.Empty : player.SteamID.ToString()
			});
		}
		func("game");
		content.AppendFormat("GameTurn = {0:D3}\r\n", this.Turn);
		foreach (global::Empire empire2 in this.Empires)
		{
			content.AppendFormat("{0}[Empire#{1:D2}] '{2}' ({3})\r\n", new object[]
			{
				indent,
				empire2.Index,
				empire2.Name,
				(!empire2.IsControlledByAI) ? "Human" : "AI"
			});
		}
		foreach (MajorEmpire majorEmpire in array)
		{
			func(majorEmpire.ToString());
			foreach (Agency agency in majorEmpire.Agencies)
			{
				content.AppendFormat("[{0}]\r\n", agency.GetType());
				agency.DumpAsText(content, indent + "  ");
			}
		}
		func("quest repository");
		IQuestRepositoryService service2 = this.GetService<IQuestRepositoryService>();
		Diagnostics.Assert(service2 != null, "IQuestRepositoryService is null.");
		IDumpable dumpable = service2 as IDumpable;
		if (dumpable != null)
		{
			dumpable.DumpAsText(content, indent);
		}
		func("marketplace");
		ITradeManagementService service3 = this.GetService<ITradeManagementService>();
		service3.DumpAsText(content, indent);
		func("simulation");
		foreach (global::Empire empire3 in this.Empires)
		{
			empire3.SimulationObject.DumpAsText(content, indent);
		}
		func("orders");
		ISessionService service4 = Amplitude.Unity.Framework.Services.GetService<ISessionService>();
		Diagnostics.Assert(service4 != null);
		(service4.Session as global::Session).GameClientDumper.DumpAsText(content, indent);
	}

	public byte[] DumpAsBytes()
	{
		ISessionService service = Amplitude.Unity.Framework.Services.GetService<ISessionService>();
		Diagnostics.Assert(service != null);
		global::Empire[] array = (from e in this.Empires
		where e is MajorEmpire
		select e).ToArray<global::Empire>();
		foreach (global::Empire empire in this.Empires)
		{
			empire.Refresh(true);
		}
		MemoryStream memoryStream = new MemoryStream();
		using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
		{
			binaryWriter.Write(Amplitude.Unity.Framework.Application.Name);
			binaryWriter.Write(Amplitude.Unity.Framework.Application.Version.ToLong());
			ITradeManagementService service2 = this.GetService<ITradeManagementService>();
			binaryWriter.Write(service2.DumpAsBytes());
			IQuestRepositoryService service3 = this.GetService<IQuestRepositoryService>();
			Diagnostics.Assert(service3 != null, "IQuestRepositoryService is null.");
			IDumpable dumpable = service3 as IDumpable;
			if (dumpable != null)
			{
				binaryWriter.Write(dumpable.DumpAsBytes());
			}
			foreach (MajorEmpire majorEmpire in array)
			{
				foreach (Agency agency in majorEmpire.Agencies)
				{
					binaryWriter.Write(agency.DumpAsBytes());
				}
			}
			binaryWriter.Write(this.Turn);
			IPlayerRepositoryService service4 = this.GetService<IPlayerRepositoryService>();
			Diagnostics.Assert(service4 != null);
			foreach (Player player in service4)
			{
				binaryWriter.Write(player.Empire.Index);
				binaryWriter.Write((int)player.Type);
			}
			foreach (global::Empire empire2 in this.Empires)
			{
				binaryWriter.Write(empire2.SimulationObject.DumpAsBytes());
			}
			binaryWriter.Write((service.Session as global::Session).GameClientDumper.DumpAsBytes());
		}
		byte[] result = memoryStream.ToArray();
		memoryStream.Close();
		return result;
	}

	public override void ReadXml(Amplitude.Xml.XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		base.ReadXml(reader);
		if (num >= 3)
		{
			this.MinorFactionDifficulty = reader.ReadElementString<string>("MinorFactionDifficulty");
		}
		else
		{
			IDatabase<OptionDefinition> database = Databases.GetDatabase<OptionDefinition>(false);
			OptionDefinition optionDefinition;
			if (database != null && database.TryGetValue("MinorFactionDifficulty", out optionDefinition))
			{
				Diagnostics.Assert(optionDefinition != null);
				this.MinorFactionDifficulty = optionDefinition.DefaultName;
			}
		}
		if (num >= 2)
		{
			this.GameDifficulty = reader.ReadElementString<string>("GameDifficulty");
			this.GameSpeed = reader.ReadElementString<string>("GameSpeed");
		}
		else
		{
			IDatabase<OptionDefinition> database2 = Databases.GetDatabase<OptionDefinition>(false);
			if (database2 != null)
			{
				OptionDefinition optionDefinition2;
				if (database2.TryGetValue("GameDifficulty", out optionDefinition2))
				{
					Diagnostics.Assert(optionDefinition2 != null);
					this.GameDifficulty = optionDefinition2.DefaultName;
				}
				if (database2.TryGetValue("GameSpeed", out optionDefinition2))
				{
					Diagnostics.Assert(optionDefinition2 != null);
					this.GameSpeed = optionDefinition2.DefaultName;
				}
			}
		}
		reader.Skip("World");
		int attribute = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("Empires");
		List<global::Empire> list = new List<global::Empire>();
		for (int i = 0; i < attribute; i++)
		{
			string attribute2 = reader.GetAttribute("AssemblyQualifiedName");
			Type type = Type.GetType(attribute2);
			global::Empire empire = Activator.CreateInstance(type, true) as global::Empire;
			if (empire != null)
			{
				reader.ReadElementSerializable<global::Empire>(ref empire);
				list.Add(empire);
			}
		}
		this.Empires = list.ToArray();
		reader.ReadEndElement("Empires");
		reader.ReadStartElement("Ancillaries");
		while (reader.Reader.Name == "Ancillary" && reader.Reader.NodeType == XmlNodeType.Element)
		{
			string assemblyQualifiedName = reader.GetAttribute("AssemblyQualifiedName");
			Ancillary ancillary = Array.Find<Ancillary>(base.Ancillaries, (Ancillary iterator) => iterator.GetType().AssemblyQualifiedName == assemblyQualifiedName);
			if (ancillary != null)
			{
				IXmlSerializable xmlSerializable = ancillary as IXmlSerializable;
				reader.ReadElementSerializable<IXmlSerializable>("Ancillary", ref xmlSerializable);
			}
			else
			{
				reader.Skip();
			}
		}
		reader.ReadEndElement("Ancillaries");
	}

	public override void WriteXml(Amplitude.Xml.XmlWriter writer)
	{
		int num = writer.WriteVersionAttribute(3);
		base.WriteXml(writer);
		if (num >= 3)
		{
			writer.WriteElementString<StaticString>("MinorFactionDifficulty", this.MinorFactionDifficulty);
		}
		if (num >= 2)
		{
			writer.WriteElementString<StaticString>("GameDifficulty", this.GameDifficulty);
			writer.WriteElementString<StaticString>("GameSpeed", this.GameSpeed);
		}
		IXmlSerializable world = this.World;
		writer.WriteElementSerializable<IXmlSerializable>(ref world);
		writer.WriteStartElement("Empires");
		writer.WriteAttributeString<int>("Count", this.Empires.Length);
		foreach (global::Empire empire in this.Empires)
		{
			IXmlSerializable xmlSerializable = empire;
			writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
		}
		writer.WriteEndElement();
		writer.WriteStartElement("Ancillaries");
		writer.WriteAttributeString<int>("Count", base.Ancillaries.Length);
		for (int j = 0; j < base.Ancillaries.Length; j++)
		{
			IXmlSerializable xmlSerializable2 = base.Ancillaries[j] as IXmlSerializable;
			if (xmlSerializable2 != null)
			{
				writer.WriteElementSerializable<IXmlSerializable>("Ancillary", ref xmlSerializable2);
			}
		}
		writer.WriteEndElement();
	}

	public static Color PrivateersColor { get; private set; } = Color.grey;

	public static double Time
	{
		get
		{
			return global::Session.Time;
		}
	}

	public global::Empire[] Empires { get; private set; }

	public StaticString GameDifficulty { get; private set; }

	public StaticString GameSpeed { get; private set; }

	public StaticString MinorFactionDifficulty { get; private set; }

	public int Turn
	{
		get
		{
			return this.turn;
		}
		set
		{
			this.turn = value;
			if (this.Empires != null)
			{
				foreach (global::Empire empire in this.Empires)
				{
					empire.SetPropertyBaseValue(SimulationProperties.CurrentTurn, (float)(value + 1));
					empire.Refresh(false);
				}
			}
			ISessionService service = Amplitude.Unity.Framework.Services.GetService<ISessionService>();
			Diagnostics.Assert(service != null);
			service.Session.SetLobbyData("_Turn", value, true);
		}
	}

	public World World { get; private set; }

	public IEnumerator Launch(Archive archive)
	{
		if (archive == null)
		{
			throw new ArgumentNullException("archive");
		}
		IGameDiagnosticsService gameDiagnosticsService = Amplitude.Unity.Framework.Services.GetService<IGameDiagnosticsService>();
		Diagnostics.Assert(gameDiagnosticsService != null);
		gameDiagnosticsService.ClearDumpFiles();
		yield return this.LoadWorld(archive);
		yield return this.LoadGame(archive);
		for (int index = 0; index < base.Ancillaries.Length; index++)
		{
			GameAncillary gameAncillary = base.Ancillaries[index] as GameAncillary;
			if (gameAncillary != null)
			{
				yield return gameAncillary.LoadGame(this);
			}
		}
		yield break;
	}

	protected override IEnumerator OnIgnite()
	{
		yield return base.OnIgnite();
		IGameSerializationService gameSerializationService = Amplitude.Unity.Framework.Services.GetService<IGameSerializationService>();
		if (gameSerializationService != null)
		{
			gameSerializationService.GameSaving += this.GameSerializationService_GameSaving;
		}
		SimulationGlobal.ClearGlobalTags(false);
		yield break;
	}

	protected override void OnRelease()
	{
		base.OnRelease();
		if (this.Empires != null)
		{
			for (int i = 0; i < this.Empires.Length; i++)
			{
				Diagnostics.Assert(this.Empires[i] != null);
				this.Empires[i].Release();
			}
			for (int j = 0; j < this.Empires.Length; j++)
			{
				Diagnostics.Assert(this.Empires[j] != null);
				this.Empires[j].Dispose();
				this.Empires[j] = null;
			}
			this.Empires = new global::Empire[0];
		}
		if (this.World != null)
		{
			this.World.Release();
			this.World = null;
		}
		IAchievementService service = Amplitude.Unity.Framework.Services.GetService<IAchievementService>();
		if (service != null)
		{
			service.Commit();
		}
		Diagnostics.Log("Game has been released.");
		IGameSerializationService service2 = Amplitude.Unity.Framework.Services.GetService<IGameSerializationService>();
		if (service2 != null)
		{
			service2.GameSaving -= this.GameSerializationService_GameSaving;
		}
		GC.Collect();
	}

	[Obsolete]
	private void CreateEmpireMainCity(global::Empire empire, Region region, WorldPosition startingPosition)
	{
		if (region == null)
		{
			GridMap<sbyte> gridMap = this.World.Atlas.GetMap(WorldAtlas.Maps.Height) as GridMap<sbyte>;
			int num = 100;
			while (region == null || region.City != null)
			{
				int num2 = UnityEngine.Random.Range(0, this.World.Regions.Length);
				region = this.World.Regions[num2];
				if (gridMap != null)
				{
					sbyte value = gridMap.GetValue(region.Borders[0].WorldPositions[0]);
					if ((int)value < 0 && num-- > 0)
					{
						region = null;
					}
				}
			}
		}
		if (!startingPosition.IsValid)
		{
			startingPosition = region.Borders[0].WorldPositions[0];
		}
		DepartmentOfTheInterior agency = empire.GetAgency<DepartmentOfTheInterior>();
		agency.CreateMainCityAtWorldPosition(startingPosition);
	}

	private void ExecuteFactionTraitCommands(global::Empire empire, FactionTrait trait)
	{
		if (trait == null || trait.Commands == null)
		{
			return;
		}
		for (int i = 0; i < trait.Commands.Length; i++)
		{
			string name = trait.Commands[i].Name;
			switch (name)
			{
			case "CreateArmy":
				if (trait.Commands[i].Arguments != null)
				{
					if (empire is MajorEmpire)
					{
						Map<WorldPosition> map = this.World.Atlas.GetMap(WorldAtlas.Tables.SpawnLocations) as Map<WorldPosition>;
						if (map != null)
						{
							IGameEntityRepositoryService service = this.Services.GetService<IGameEntityRepositoryService>();
							MajorEmpire majorEmpire = empire as MajorEmpire;
							DepartmentOfDefense agency = majorEmpire.GetAgency<DepartmentOfDefense>();
							bool flag = false;
							StaticString[] array = Array.ConvertAll<string, StaticString>(trait.Commands[i].Arguments, (string input) => input);
							DepartmentOfDefense.UnitDescriptor[] array2 = new DepartmentOfDefense.UnitDescriptor[array.Length];
							for (int j = 0; j < array2.Length; j++)
							{
								array2[j].UnitDesignName = array[j];
								uint unitDesignModel = 0u;
								if (!agency.GenerateUnitDesignModelId(array2[j].UnitDesignName, out unitDesignModel))
								{
									flag = true;
									break;
								}
								array2[j].UnitDesignModel = unitDesignModel;
							}
							if (!flag)
							{
								GameEntityGUID armyGUID = service.GenerateGUID();
								for (int k = 0; k < array2.Length; k++)
								{
									array2[k].GameEntityGUID = service.GenerateGUID();
								}
								WorldPosition worldPosition = map.Data[empire.Index];
								WorldPosition worldPosition2 = WorldPosition.Invalid;
								Queue queue = new Queue();
								queue.Enqueue(worldPosition);
								IPathfindingService service2 = this.Services.GetService<IPathfindingService>();
								GridMap<Army> gridMap = this.World.Atlas.GetMap(WorldAtlas.Maps.Armies) as GridMap<Army>;
								while (worldPosition2 == WorldPosition.Invalid && queue.Count > 0)
								{
									worldPosition2 = (WorldPosition)queue.Peek();
									queue.Dequeue();
									foreach (WorldPosition worldPosition3 in worldPosition2.GetNeighbours(this.World.WorldParameters))
									{
										queue.Enqueue(worldPosition3);
									}
									if (gridMap != null)
									{
										Army value = gridMap.GetValue(worldPosition2);
										if (value != null)
										{
											worldPosition2 = WorldPosition.Invalid;
											continue;
										}
									}
									if (service2 != null)
									{
										bool flag2 = service2.IsTileStopableAndPassable(worldPosition2, PathfindingMovementCapacity.Ground, PathfindingFlags.IgnoreFogOfWar);
										bool flag3 = service2.IsTransitionPassable(worldPosition, worldPosition2, PathfindingMovementCapacity.Ground, (PathfindingFlags)0);
										if (!flag2 || (worldPosition != worldPosition2 && !flag3))
										{
											worldPosition2 = WorldPosition.Invalid;
										}
									}
								}
								Army army;
								agency.CreateArmy(armyGUID, array2, worldPosition2, out army, false, false);
							}
						}
					}
				}
				break;
			case "CreateUnit":
				if (trait.Commands[i].Arguments != null)
				{
					if (empire is MajorEmpire)
					{
						MajorEmpire majorEmpire2 = empire as MajorEmpire;
						DepartmentOfTheInterior agency2 = majorEmpire2.GetAgency<DepartmentOfTheInterior>();
						DepartmentOfDefense agency3 = majorEmpire2.GetAgency<DepartmentOfDefense>();
						if (agency2.MainCity == null)
						{
							Diagnostics.LogWarning("ExecuteFactionTraitCommands_CreateUnit: MainCity doesn't exist yet.");
						}
						else
						{
							agency3.CreateUnitInGarrison(Array.ConvertAll<string, StaticString>(trait.Commands[i].Arguments, (string match) => match), agency2.MainCity);
						}
					}
				}
				break;
			case "CreateHero":
				if (trait.Commands[i].Arguments != null)
				{
					if (empire is MajorEmpire)
					{
						MajorEmpire majorEmpire3 = empire as MajorEmpire;
						DepartmentOfEducation agency4 = majorEmpire3.GetAgency<DepartmentOfEducation>();
						agency4.InternalCreateHero(Array.ConvertAll<string, StaticString>(trait.Commands[i].Arguments, (string match) => match));
					}
				}
				break;
			case "LoadRegionVillages":
				if (empire is MinorEmpire)
				{
					BarbarianCouncil agency5 = empire.GetAgency<BarbarianCouncil>();
					if (agency5 != null)
					{
						agency5.LoadRegionVillages((empire as MinorEmpire).Region);
					}
				}
				break;
			case "LoadFortresses":
				if (empire is NavalEmpire)
				{
					PirateCouncil agency6 = empire.GetAgency<PirateCouncil>();
					if (agency6 != null)
					{
						agency6.LoadFortresses((empire as NavalEmpire).Regions);
					}
				}
				break;
			case "TransferResources":
				if (trait.Commands[i].Arguments != null)
				{
					if (!(empire is LesserEmpire))
					{
						DepartmentOfTheTreasury agency7 = empire.GetAgency<DepartmentOfTheTreasury>();
						if (agency7 != null)
						{
							for (int l = 0; l < trait.Commands[i].Arguments.Length; l += 2)
							{
								try
								{
									StaticString resourceName = trait.Commands[i].Arguments[l];
									float amount = float.Parse(trait.Commands[i].Arguments[l + 1], NumberStyles.Any, CultureInfo.InvariantCulture);
									agency7.TryTransferResources(empire, resourceName, amount);
								}
								catch
								{
								}
							}
						}
					}
				}
				break;
			case "UnlockTechnology":
				if (trait.Commands[i].Arguments != null)
				{
					DepartmentOfScience agency8 = empire.GetAgency<DepartmentOfScience>();
					if (agency8 != null)
					{
						if (trait.Commands[i].Arguments.Length > 0 && trait.Commands[i].Arguments[0] == "All")
						{
							IDatabase<DepartmentOfScience.ConstructibleElement> database = Databases.GetDatabase<DepartmentOfScience.ConstructibleElement>(false);
							if (database != null)
							{
								DepartmentOfScience.ConstructibleElement[] values = database.GetValues();
								foreach (DepartmentOfScience.ConstructibleElement technology in values)
								{
									agency8.UnlockTechnology(technology, false);
								}
							}
							return;
						}
						foreach (string technologyName in trait.Commands[i].Arguments)
						{
							agency8.UnlockTechnology(technologyName, true);
						}
					}
				}
				break;
			}
		}
	}

	private void GameSerializationService_GameSaving(object sender, GameSavingEventArgs e)
	{
		if (e.Archive == null)
		{
			Diagnostics.LogError("Archive is null; can't proceed with game serialization.");
			return;
		}
		if (this.World != null && this.World.Atlas != null)
		{
			this.World.Atlas.SaveGame(e.Archive);
		}
	}

	private IEnumerator LoadGame(Archive archive)
	{
		if (archive == null)
		{
			throw new ArgumentNullException("archive");
		}
		Diagnostics.Assert(this.World != null);
		Diagnostics.Assert(this.World.HasBeenIgnited);
		Diagnostics.Assert(this.World.HasBeenLoaded);
		MemoryStream stream = null;
		string paletteName = Amplitude.Unity.Framework.Application.Registry.GetValue<string>("Settings/UI/EmpireColorPalette", "Standard");
		IDatabase<Palette> palettes = Databases.GetDatabase<Palette>(false);
		if (archive.TryGet(global::GameManager.GameFileName, out stream))
		{
			using (stream)
			{
				using (Amplitude.Xml.XmlReader reader = Amplitude.Xml.XmlReader.Create(stream))
				{
					reader.Reader.ReadToDescendant("Game");
					this.ReadXml(reader);
				}
			}
			Diagnostics.Log("Load game with difficulty {0} and speed {1}.", new object[]
			{
				this.GameDifficulty,
				this.GameSpeed
			});
		}
		else
		{
			ISessionService sessionService = Amplitude.Unity.Framework.Services.GetService<ISessionService>();
			Diagnostics.Assert(sessionService != null);
			Diagnostics.Assert(sessionService.Session != null);
			Diagnostics.Assert(sessionService.Session.IsOpened);
			this.GameDifficulty = sessionService.Session.GetLobbyData<string>("GameDifficulty", null);
			this.GameSpeed = sessionService.Session.GetLobbyData<string>("GameSpeed", null);
			this.MinorFactionDifficulty = sessionService.Session.GetLobbyData<string>("MinorFactionDifficulty", null);
			Diagnostics.Log("Starting new game with difficulty: {0}, speed: {1} and minor faction difficulty: {2}...", new object[]
			{
				this.GameDifficulty,
				this.GameSpeed,
				this.MinorFactionDifficulty
			});
			bool endlessDay = DownloadableContent8.EndlessDay.IsActive;
			if (endlessDay)
			{
				if (!SimulationGlobal.GlobalTagsContains(DownloadableContent8.EndlessDay.ReadOnlyTag))
				{
					SimulationGlobal.AddGlobalTag(DownloadableContent8.EndlessDay.ReadOnlyTag, false);
				}
			}
			else if (SimulationGlobal.GlobalTagsContains(DownloadableContent8.EndlessDay.ReadOnlyTag))
			{
				SimulationGlobal.RemoveGlobalTag(DownloadableContent8.EndlessDay.ReadOnlyTag, false);
			}
			Palette palette = null;
			if (palettes != null)
			{
				palettes.TryGetValue(paletteName, out palette);
			}
			if (palette == null)
			{
				Diagnostics.LogWarning("Failed to retrieve the palette (name: '{0}').", new object[]
				{
					paletteName
				});
			}
			Faction defaultFaction = null;
			IDatabase<Faction> factionDatabase = Databases.GetDatabase<Faction>(false);
			if (factionDatabase == null)
			{
				Diagnostics.LogError("Failed to retrieve the faction database.");
			}
			else
			{
				factionDatabase.TryGetValue("FactionRandom", out defaultFaction);
				if (defaultFaction == null)
				{
					Diagnostics.LogWarning("Failed to retrieve the 'Random' faction.");
					defaultFaction = factionDatabase.GetValues().FirstOrDefault((Faction iterator) => iterator.IsStandard);
					if (defaultFaction == null)
					{
						Diagnostics.LogWarning("Failed to retrieve any single 'standard' faction from the database.");
					}
				}
			}
			IDatabase<SimulationDescriptor> simulationDescriptorDatatable = Databases.GetDatabase<SimulationDescriptor>(false);
			SimulationDescriptor empireClassDescriptor = null;
			if (simulationDescriptorDatatable == null || simulationDescriptorDatatable.TryGetValue("ClassEmpire", out empireClassDescriptor))
			{
			}
			int numberOfMajorEmpires = 1;
			int numberOfRegions = this.World.Regions.Length;
			Map<WorldPosition> spawnLocations = this.World.Atlas.GetMap(WorldAtlas.Tables.SpawnLocations) as Map<WorldPosition>;
			if (spawnLocations != null)
			{
				numberOfMajorEmpires = spawnLocations.Data.Length;
			}
			int numberOfMajorFactions = sessionService.Session.GetLobbyData<int>("NumberOfMajorFactions", 0);
			Diagnostics.Assert(numberOfMajorEmpires == numberOfMajorFactions);
			List<global::Empire> empires = new List<global::Empire>();
			for (int empireIndex = 0; empireIndex < numberOfMajorEmpires; empireIndex++)
			{
				string keyLobbyDataFactionDescriptor = string.Format("Faction{0}", empireIndex);
				string lobbyDataFactionDescriptor = sessionService.Session.GetLobbyData<string>(keyLobbyDataFactionDescriptor, null);
				Faction faction = Faction.Decode(lobbyDataFactionDescriptor);
				if (faction == null)
				{
					throw new GameException(string.Format("Unable to decode faction from decsriptor (descriptor: '{0}')", lobbyDataFactionDescriptor));
				}
				string keyLobbyDataFactionColor = string.Format("Color{0}", empireIndex);
				string lobbyDataFactionColor = sessionService.Session.GetLobbyData<string>(keyLobbyDataFactionColor, null);
				int empireColorIndex = 0;
				try
				{
					empireColorIndex = int.Parse(lobbyDataFactionColor);
				}
				catch
				{
				}
				MajorEmpire majorEmpire = new MajorEmpire("Empire#" + empireIndex, (Faction)faction.Clone(), empireColorIndex);
				empires.Add(majorEmpire);
			}
			NavalEmpire navalEmpire = null;
			IDownloadableContentService downloadableContentService = Amplitude.Unity.Framework.Services.GetService<IDownloadableContentService>();
			if (downloadableContentService.IsShared(DownloadableContent16.ReadOnlyName))
			{
				Faction navalFaction = null;
				if (factionDatabase != null && !factionDatabase.TryGetValue("Fomorians", out navalFaction))
				{
					Diagnostics.LogError("Failed to retrieve the sea people faction from the database.");
				}
				if (navalFaction == null)
				{
					navalFaction = defaultFaction;
				}
				int empireColorIndex2 = 0;
				if (palette != null && palette.Colors != null)
				{
					empireColorIndex2 = UnityEngine.Random.Range(0, palette.Colors.Length);
					StaticString tag = "NavalEmpire";
					XmlColorReference xmlColorReference = palette.Colors.FirstOrDefault((XmlColorReference iterator) => iterator.Tags != null && iterator.Tags.Contains(tag));
					if (xmlColorReference != null)
					{
						empireColorIndex2 = Array.IndexOf<XmlColorReference>(palette.Colors, xmlColorReference);
					}
				}
				navalEmpire = new NavalEmpire("NavalEmpire#0", (Faction)navalFaction.Clone(), empireColorIndex2);
				empires.Add(navalEmpire);
			}
			for (int empireIndex2 = 0; empireIndex2 < numberOfRegions; empireIndex2++)
			{
				Region region = this.World.Regions[empireIndex2];
				if (downloadableContentService.IsShared(DownloadableContent16.ReadOnlyName) && region.IsOcean && navalEmpire != null && navalEmpire.Regions != null && !navalEmpire.Regions.Contains(region))
				{
					navalEmpire.Regions.Add(region);
					region.NavalEmpire = navalEmpire;
				}
				else
				{
					defaultFaction = null;
					if (!StaticString.IsNullOrEmpty(region.MinorEmpireFactionName))
					{
						if (!factionDatabase.TryGetValue(region.MinorEmpireFactionName, out defaultFaction))
						{
							Diagnostics.LogError("Unable to retrieve minor faction '{0}' for region #{1}.", new object[]
							{
								region.MinorEmpireFactionName,
								empireIndex2
							});
						}
						if (defaultFaction == null)
						{
							factionDatabase.TryGetValue("RandomMinor", out defaultFaction);
							if (defaultFaction == null)
							{
								Diagnostics.LogWarning("Failed to retrieve the 'RandomMinor' faction.");
								defaultFaction = factionDatabase.GetValues().FirstOrDefault((Faction iterator) => iterator is MinorFaction && !iterator.IsStandard && !iterator.IsRandom);
								if (defaultFaction == null)
								{
									Diagnostics.LogWarning("Failed to retrieve any single 'minor' faction from the database.");
								}
							}
						}
						if (defaultFaction != null)
						{
							MinorFaction defaultMinorFaction = defaultFaction as MinorFaction;
							if (defaultMinorFaction == null)
							{
								Diagnostics.LogError("The default faction is not a minor faction.");
							}
							else
							{
								MinorFaction empireFaction = (MinorFaction)defaultMinorFaction.Clone();
								int empireColorIndex3 = 0;
								if (palette != null && palette.Colors != null)
								{
									empireColorIndex3 = UnityEngine.Random.Range(0, palette.Colors.Length);
									StaticString tag2 = "MinorFaction";
									XmlColorReference xmlColorReference2 = palette.Colors.FirstOrDefault((XmlColorReference iterator) => iterator.Tags != null && iterator.Tags.Contains(tag2));
									if (xmlColorReference2 != null)
									{
										empireColorIndex3 = Array.IndexOf<XmlColorReference>(palette.Colors, xmlColorReference2);
									}
								}
								MinorEmpire minorEmpire = new MinorEmpire("MinorEmpire#" + empireIndex2, empireFaction, empireColorIndex3);
								empires.Add(minorEmpire);
								if (StaticString.IsNullOrEmpty(region.MinorEmpireFactionName))
								{
									region.MinorEmpireFactionName = empireFaction.Name;
								}
								minorEmpire.Region = region;
								region.MinorEmpire = minorEmpire;
							}
						}
					}
				}
			}
			Faction lesserFaction = null;
			if (factionDatabase != null && !factionDatabase.TryGetValue("Lesser-NPCs", out lesserFaction))
			{
				Diagnostics.LogError("Failed to retrieve the lesser npcs faction from the database.");
			}
			if (lesserFaction == null)
			{
				lesserFaction = defaultFaction;
			}
			int empireColorIndex4 = 0;
			if (palette != null && palette.Colors != null)
			{
				empireColorIndex4 = UnityEngine.Random.Range(0, palette.Colors.Length);
				StaticString tag3 = "LesserEmpire";
				XmlColorReference xmlColorReference3 = palette.Colors.FirstOrDefault((XmlColorReference iterator) => iterator.Tags != null && iterator.Tags.Contains(tag3));
				if (xmlColorReference3 != null)
				{
					empireColorIndex4 = Array.IndexOf<XmlColorReference>(palette.Colors, xmlColorReference3);
				}
			}
			LesserEmpire lesserEmpire = new LesserEmpire("LesserEmpire#0", (Faction)lesserFaction.Clone(), empireColorIndex4);
			empires.Add(lesserEmpire);
			this.Empires = empires.ToArray();
			IDatabase<SimulationDescriptor> simulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(true);
			SimulationDescriptor simulationDescriptor = null;
			for (int empireIndex3 = 0; empireIndex3 < this.Empires.Length; empireIndex3++)
			{
				global::Empire empire = this.Empires[empireIndex3];
				Diagnostics.Progress.SetProgress((float)empireIndex3 / (float)this.Empires.Length, string.Format("Initializing empire #{0} out of {1}...", empireIndex3, this.Empires.Length), "Loading...");
				if (empireClassDescriptor != null)
				{
					empire.AddDescriptor(empireClassDescriptor, false);
				}
				if (empire.Faction == null)
				{
					throw new InvalidOperationException();
				}
				List<FactionTrait> traits = new List<FactionTrait>(Faction.EnumerableTraits(empire.Faction));
				int traitIndex = 0;
				for (traitIndex = 0; traitIndex < traits.Count; traitIndex++)
				{
					FactionTrait trait = traits[traitIndex];
					if (trait != null && trait.SimulationDescriptorReferences != null)
					{
						for (int jndex = 0; jndex < trait.SimulationDescriptorReferences.Length; jndex++)
						{
							if (simulationDescriptorDatabase.TryGetValue(trait.SimulationDescriptorReferences[jndex], out simulationDescriptor))
							{
								empire.SimulationObject.AddDescriptor(simulationDescriptor);
							}
							else
							{
								Diagnostics.LogWarning("Failed to find the descriptor for descriptor reference (name: '{1}') on trait (name: '{0}').", new object[]
								{
									trait.Name,
									trait.SimulationDescriptorReferences[jndex]
								});
							}
						}
					}
				}
				yield return this.Empires[empireIndex3].Initialize(empireIndex3);
				Diagnostics.Assert(this.Empires[empireIndex3].HasBeenInitialized, "The initialization of the empire (index: {0}) has failed.", new object[]
				{
					empireIndex3
				});
			}
			for (int index = 0; index < this.Empires.Length; index++)
			{
				if (this.Empires[index].Faction != null)
				{
					foreach (FactionTrait trait2 in Faction.EnumerableTraits(this.Empires[index].Faction))
					{
						this.ExecuteFactionTraitCommands(this.Empires[index], trait2);
					}
				}
			}
		}
		Palette palette2;
		if (palettes != null && palettes.TryGetValue(paletteName, out palette2) && palette2.Colors != null && palette2.Colors.Length > 0)
		{
			StaticString tag4 = "MinorFaction";
			XmlColorReference xmlColorReference4 = palette2.Colors.FirstOrDefault((XmlColorReference iterator) => iterator.Tags != null && iterator.Tags.Contains(tag4));
			if (xmlColorReference4 != null)
			{
				global::Game.PrivateersColor = xmlColorReference4.ToColor();
			}
		}
		for (int index2 = 0; index2 < this.Empires.Length; index2++)
		{
			Diagnostics.Progress.SetProgress((float)index2 / (float)this.Empires.Length, string.Format("Loading empire #{0} out of {1}...", index2, this.Empires.Length), "Loading...");
			yield return this.Empires[index2].Load();
			yield return this.Empires[index2].LoadGame(this);
		}
		Diagnostics.Progress.Clear();
		yield break;
	}

	private IEnumerator LoadWorld(Archive archive)
	{
		if (archive == null)
		{
			throw new ArgumentNullException("archive");
		}
		World world = new World();
		DateTime timeStamp = DateTime.Now;
		yield return world.Ignite();
		if (!world.HasBeenIgnited)
		{
			throw new GameException("The world initialization has encoutered an unexpected error.");
		}
		Diagnostics.Log("The world has been initialized (in {0} second(s)).", new object[]
		{
			(DateTime.Now - timeStamp).TotalSeconds
		});
		yield return null;
		timeStamp = DateTime.Now;
		yield return world.Load(archive);
		if (!world.HasBeenLoaded)
		{
			throw new GameException("The world loading has encoutered an unexpected error.");
		}
		Diagnostics.Log("The world has been loaded (in {0} second(s)).", new object[]
		{
			(DateTime.Now - timeStamp).TotalSeconds
		});
		yield return null;
		for (int index = 0; index < base.Ancillaries.Length; index++)
		{
			GameAncillary gameAncillary = base.Ancillaries[index] as GameAncillary;
			if (gameAncillary != null)
			{
				timeStamp = DateTime.Now;
				yield return gameAncillary.OnWorldLoaded(world);
				Diagnostics.Log("The game ancillary (type of: {0}) has been loaded (in {1} second(s)).", new object[]
				{
					gameAncillary.GetType().Name,
					(DateTime.Now - timeStamp).TotalSeconds
				});
				yield return null;
			}
		}
		this.World = world;
		yield break;
	}

	public List<int> GetEmpireIndexesOfFaction(string factionName)
	{
		List<int> list = new List<int>();
		for (int i = 0; i < this.Empires.Length; i++)
		{
			if (this.Empires[i].Faction.Name == factionName)
			{
				list.Add(this.Empires[i].Index);
			}
		}
		return list;
	}

	private int turn;
}
