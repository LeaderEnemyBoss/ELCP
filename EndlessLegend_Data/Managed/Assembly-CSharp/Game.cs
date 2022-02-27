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
		global::Empire[] array2 = this.Empires;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].Refresh(true);
		}
		Func<string, StringBuilder> func = (string s) => content.Append(string.Format("\r\n===== {0} ========================================", s.ToUpper()).Substring(0, 40) + "\r\n");
		func("application");
		content.AppendFormat("Application = \"{0}\"\r\nVersion = \"{1}\"\r\n", Amplitude.Unity.Framework.Application.Name, Amplitude.Unity.Framework.Application.Version.ToString("V{0}.{1}.{2} S{3} {4}"));
		func("players");
		IPlayerRepositoryService service = base.GetService<IPlayerRepositoryService>();
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
		foreach (global::Empire empire in this.Empires)
		{
			content.AppendFormat("{0}[Empire#{1:D2}] '{2}' ({3})\r\n", new object[]
			{
				indent,
				empire.Index,
				empire.Name,
				(!empire.IsControlledByAI) ? "Human" : "AI"
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
		IQuestRepositoryService service2 = base.GetService<IQuestRepositoryService>();
		Diagnostics.Assert(service2 != null, "IQuestRepositoryService is null.");
		IDumpable dumpable = service2 as IDumpable;
		if (dumpable != null)
		{
			dumpable.DumpAsText(content, indent);
		}
		func("marketplace");
		base.GetService<ITradeManagementService>().DumpAsText(content, indent);
		func("simulation");
		array2 = this.Empires;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].SimulationObject.DumpAsText(content, indent);
		}
		func("orders");
		ISessionService service3 = Amplitude.Unity.Framework.Services.GetService<ISessionService>();
		Diagnostics.Assert(service3 != null);
		(service3.Session as global::Session).GameClientDumper.DumpAsText(content, indent);
	}

	public byte[] DumpAsBytes()
	{
		ISessionService service = Amplitude.Unity.Framework.Services.GetService<ISessionService>();
		Diagnostics.Assert(service != null);
		global::Empire[] array = (from e in this.Empires
		where e is MajorEmpire
		select e).ToArray<global::Empire>();
		global::Empire[] array2 = this.Empires;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].Refresh(true);
		}
		MemoryStream memoryStream = new MemoryStream();
		using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
		{
			binaryWriter.Write(Amplitude.Unity.Framework.Application.Name);
			binaryWriter.Write(Amplitude.Unity.Framework.Application.Version.ToLong());
			ITradeManagementService service2 = base.GetService<ITradeManagementService>();
			binaryWriter.Write(service2.DumpAsBytes());
			IQuestRepositoryService service3 = base.GetService<IQuestRepositoryService>();
			Diagnostics.Assert(service3 != null, "IQuestRepositoryService is null.");
			IDumpable dumpable = service3 as IDumpable;
			if (dumpable != null)
			{
				binaryWriter.Write(dumpable.DumpAsBytes());
			}
			array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				foreach (Agency agency in ((MajorEmpire)array2[i]).Agencies)
				{
					binaryWriter.Write(agency.DumpAsBytes());
				}
			}
			binaryWriter.Write(this.Turn);
			IPlayerRepositoryService service4 = base.GetService<IPlayerRepositoryService>();
			Diagnostics.Assert(service4 != null);
			foreach (Player player in service4)
			{
				binaryWriter.Write(player.Empire.Index);
				binaryWriter.Write((int)player.Type);
			}
			foreach (global::Empire empire in this.Empires)
			{
				binaryWriter.Write(empire.SimulationObject.DumpAsBytes());
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
			global::Empire empire = Activator.CreateInstance(Type.GetType(reader.GetAttribute("AssemblyQualifiedName")), true) as global::Empire;
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
		foreach (global::Empire xmlSerializable in this.Empires)
		{
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

	public int MajorEmpiresCount
	{
		get
		{
			int num = 0;
			for (int i = 0; i < this.Empires.Length; i++)
			{
				if (this.Empires[i] is MajorEmpire)
				{
					num++;
				}
			}
			return num;
		}
	}

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

	public global::Empire GetEmpireByIndex(int empireIndex)
	{
		for (int i = 0; i < this.Empires.Length; i++)
		{
			if (this.Empires[i].Index == empireIndex)
			{
				return this.Empires[i];
			}
		}
		return null;
	}

	public T GetEmpireByIndex<T>(int empireIndex) where T : global::Empire
	{
		return this.GetEmpireByIndex(empireIndex) as T;
	}

	public MajorEmpire[] GetMajorEmpiresFromBitMask(short bitsMask)
	{
		List<MajorEmpire> list = new List<MajorEmpire>();
		for (int i = 0; i < this.Empires.Length; i++)
		{
			MajorEmpire majorEmpire = this.Empires[i] as MajorEmpire;
			if (majorEmpire != null && ((int)bitsMask & majorEmpire.Bits) == majorEmpire.Bits)
			{
				list.Add(this.Empires[i] as MajorEmpire);
			}
		}
		return list.ToArray();
	}

	public IEnumerator Launch(Archive archive)
	{
		if (archive == null)
		{
			throw new ArgumentNullException("archive");
		}
		IGameDiagnosticsService service = Amplitude.Unity.Framework.Services.GetService<IGameDiagnosticsService>();
		Diagnostics.Assert(service != null);
		service.ClearDumpFiles();
		yield return this.LoadWorld(archive);
		yield return this.LoadGame(archive);
		int num;
		for (int index = 0; index < base.Ancillaries.Length; index = num + 1)
		{
			GameAncillary gameAncillary = base.Ancillaries[index] as GameAncillary;
			if (gameAncillary != null)
			{
				yield return gameAncillary.LoadGame(this);
			}
			num = index;
		}
		for (int i = 0; i < this.World.Regions.Length; i++)
		{
			Region region = this.World.Regions[i];
			if (region.IsLand && region.MinorEmpire != null)
			{
				BarbarianCouncil agency = region.MinorEmpire.GetAgency<BarbarianCouncil>();
				if (agency != null)
				{
					foreach (Village village in agency.Villages)
					{
						if (village.Converter != null && village.Converter.IsEliminated)
						{
							int index2 = village.Converter.Index;
							agency.PacifyVillage(village, null);
							if (region.City != null && region.City.Empire.Index != index2)
							{
								DepartmentOfTheInterior agency2 = region.City.Empire.GetAgency<DepartmentOfTheInterior>();
								Diagnostics.Assert(agency2 != null);
								agency2.BindMinorFactionToCity(region.City, region.MinorEmpire);
								agency2.VerifyOverallPopulation(region.City);
							}
						}
					}
				}
			}
		}
		yield break;
	}

	protected override IEnumerator OnIgnite()
	{
		yield return base.OnIgnite();
		IGameSerializationService service = Amplitude.Unity.Framework.Services.GetService<IGameSerializationService>();
		if (service != null)
		{
			service.GameSaving += this.GameSerializationService_GameSaving;
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

	private void ExecuteFactionTraitCommands(global::Empire empire, FactionTrait trait)
	{
		if (trait == null || trait.Commands == null)
		{
			return;
		}
		for (int i = 0; i < trait.Commands.Length; i++)
		{
			string name = trait.Commands[i].Name;
			if (name == "TransferResources")
			{
				if (trait.Commands[i].Arguments != null && !(empire is LesserEmpire))
				{
					DepartmentOfTheTreasury agency = empire.GetAgency<DepartmentOfTheTreasury>();
					if (agency != null)
					{
						for (int j = 0; j < trait.Commands[i].Arguments.Length; j += 2)
						{
							try
							{
								StaticString resourceName = trait.Commands[i].Arguments[j];
								float amount = float.Parse(trait.Commands[i].Arguments[j + 1], NumberStyles.Any, CultureInfo.InvariantCulture);
								agency.TryTransferResources(empire, resourceName, amount);
							}
							catch
							{
							}
						}
					}
				}
			}
			else if (name == "UnlockTechnology")
			{
				if (trait.Commands[i].Arguments != null)
				{
					DepartmentOfScience agency2 = empire.GetAgency<DepartmentOfScience>();
					if (agency2 != null)
					{
						if (trait.Commands[i].Arguments.Length != 0 && trait.Commands[i].Arguments[0] == "All")
						{
							IDatabase<DepartmentOfScience.ConstructibleElement> database = Databases.GetDatabase<DepartmentOfScience.ConstructibleElement>(false);
							if (database != null)
							{
								foreach (DepartmentOfScience.ConstructibleElement technology in database.GetValues())
								{
									agency2.UnlockTechnology(technology, false);
								}
							}
							return;
						}
						foreach (string technologyName in trait.Commands[i].Arguments)
						{
							agency2.UnlockTechnology(technologyName, true);
						}
					}
				}
			}
			else if (name == "CreateArmy" && trait.Commands[i].Arguments != null && empire is MajorEmpire)
			{
				Map<WorldPosition> map = this.World.Atlas.GetMap(WorldAtlas.Tables.SpawnLocations) as Map<WorldPosition>;
				if (map != null)
				{
					IGameEntityRepositoryService service = base.Services.GetService<IGameEntityRepositoryService>();
					DepartmentOfDefense agency3 = (empire as MajorEmpire).GetAgency<DepartmentOfDefense>();
					bool flag = false;
					StaticString[] array = Array.ConvertAll<string, StaticString>(trait.Commands[i].Arguments, (string input) => input);
					DepartmentOfDefense.UnitDescriptor[] array2 = new DepartmentOfDefense.UnitDescriptor[array.Length];
					for (int l = 0; l < array2.Length; l++)
					{
						array2[l].UnitDesignName = array[l];
						uint unitDesignModel = 0u;
						if (!agency3.GenerateUnitDesignModelId(array2[l].UnitDesignName, out unitDesignModel))
						{
							flag = true;
							break;
						}
						array2[l].UnitDesignModel = unitDesignModel;
					}
					if (!flag)
					{
						int num = 0;
						int num2 = 0;
						while (num2 < this.Empires.Length && num2 != empire.Index)
						{
							if (this.Empires[num2].Faction != null && this.Empires[num2].Faction.Name == "FactionELCPSpectator")
							{
								num++;
							}
							num2++;
						}
						GameEntityGUID armyGUID = service.GenerateGUID();
						for (int m = 0; m < array2.Length; m++)
						{
							array2[m].GameEntityGUID = service.GenerateGUID();
						}
						WorldPosition worldPosition = map.Data[empire.Index - num];
						WorldPosition worldPosition2 = WorldPosition.Invalid;
						Queue queue = new Queue();
						queue.Enqueue(worldPosition);
						IPathfindingService service2 = base.Services.GetService<IPathfindingService>();
						GridMap<Army> gridMap = this.World.Atlas.GetMap(WorldAtlas.Maps.Armies) as GridMap<Army>;
						while (worldPosition2 == WorldPosition.Invalid && queue.Count > 0)
						{
							worldPosition2 = (WorldPosition)queue.Peek();
							queue.Dequeue();
							foreach (WorldPosition worldPosition3 in worldPosition2.GetNeighbours(this.World.WorldParameters))
							{
								queue.Enqueue(worldPosition3);
							}
							if (gridMap != null && gridMap.GetValue(worldPosition2) != null)
							{
								worldPosition2 = WorldPosition.Invalid;
							}
							else if (service2 != null)
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
						agency3.CreateArmy(armyGUID, array2, worldPosition2, out army, false, false);
					}
				}
			}
			else if (name == "LoadFortresses")
			{
				if (empire is NavalEmpire)
				{
					PirateCouncil agency4 = empire.GetAgency<PirateCouncil>();
					if (agency4 != null)
					{
						agency4.LoadFortresses((empire as NavalEmpire).Regions);
					}
				}
			}
			else if (name == "CreateUnit" && trait.Commands[i].Arguments != null && empire is MajorEmpire)
			{
				MajorEmpire majorEmpire = empire as MajorEmpire;
				DepartmentOfTheInterior agency5 = majorEmpire.GetAgency<DepartmentOfTheInterior>();
				DepartmentOfDefense agency6 = majorEmpire.GetAgency<DepartmentOfDefense>();
				if (agency5.MainCity == null)
				{
					Diagnostics.LogWarning("ExecuteFactionTraitCommands_CreateUnit: MainCity doesn't exist yet.");
				}
				else
				{
					agency6.CreateUnitInGarrison(Array.ConvertAll<string, StaticString>(trait.Commands[i].Arguments, (string match) => match), agency5.MainCity);
				}
			}
			else if (name == "LoadRegionVillages")
			{
				if (empire is MinorEmpire)
				{
					BarbarianCouncil agency7 = empire.GetAgency<BarbarianCouncil>();
					if (agency7 != null)
					{
						agency7.LoadRegionVillages((empire as MinorEmpire).Region);
					}
				}
			}
			else if (name == "CreateHero" && trait.Commands[i].Arguments != null && empire is MajorEmpire)
			{
				(empire as MajorEmpire).GetAgency<DepartmentOfEducation>().InternalCreateHero(Array.ConvertAll<string, StaticString>(trait.Commands[i].Arguments, (string match) => match));
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
			global::Game.ELCPRevertTempTerraformationsToOriginal(this.World, false);
			this.World.Atlas.SaveGame(e.Archive);
			global::Game.ELCPRevertTempTerraformationsToOriginal(this.World, true);
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
		MemoryStream memoryStream = null;
		string paletteName = Amplitude.Unity.Framework.Application.Registry.GetValue<string>("Settings/UI/EmpireColorPalette", "Standard");
		IDatabase<Palette> palettes = Databases.GetDatabase<Palette>(false);
		ELCPUtilities.SetupELCPSettings();
		ELCPUtilities.EliminatedEmpireIndices = new List<int>();
		ELCPUtilities.SpellUsage_Clear();
		int num4;
		if (archive.TryGet(global::GameManager.GameFileName, out memoryStream))
		{
			ISessionService service = Amplitude.Unity.Framework.Services.GetService<ISessionService>();
			Diagnostics.Assert(service != null);
			Diagnostics.Assert(service.Session != null);
			Diagnostics.Assert(service.Session.IsOpened);
			ELCPUtilities.NumberOfMajorEmpires = service.Session.GetLobbyData<int>("NumberOfMajorFactions", 0);
			Diagnostics.Log("ELCP Setting global empire count to " + ELCPUtilities.NumberOfMajorEmpires);
			using (memoryStream)
			{
				using (Amplitude.Xml.XmlReader xmlReader = Amplitude.Xml.XmlReader.Create(memoryStream))
				{
					xmlReader.Reader.ReadToDescendant("Game");
					this.ReadXml(xmlReader);
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
			ISessionService service2 = Amplitude.Unity.Framework.Services.GetService<ISessionService>();
			Diagnostics.Assert(service2 != null);
			Diagnostics.Assert(service2.Session != null);
			Diagnostics.Assert(service2.Session.IsOpened);
			this.GameDifficulty = service2.Session.GetLobbyData<string>("GameDifficulty", null);
			this.GameSpeed = service2.Session.GetLobbyData<string>("GameSpeed", null);
			this.MinorFactionDifficulty = this.World.MinorFactionDifficulty;
			Diagnostics.Log("Starting new game with difficulty: {0}, speed: {1} and minor faction difficulty: {2}...", new object[]
			{
				this.GameDifficulty,
				this.GameSpeed,
				this.MinorFactionDifficulty
			});
			if (DownloadableContent8.EndlessDay.IsActive)
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
			Faction faction5 = null;
			IDatabase<Faction> database = Databases.GetDatabase<Faction>(false);
			if (database == null)
			{
				Diagnostics.LogError("Failed to retrieve the faction database.");
			}
			else
			{
				database.TryGetValue("FactionRandom", out faction5);
				if (faction5 == null)
				{
					Diagnostics.LogWarning("Failed to retrieve the 'Random' faction.");
					faction5 = database.GetValues().FirstOrDefault((Faction iterator) => iterator.IsStandard);
					if (faction5 == null)
					{
						Diagnostics.LogWarning("Failed to retrieve any single 'standard' faction from the database.");
					}
				}
			}
			IDatabase<SimulationDescriptor> database2 = Databases.GetDatabase<SimulationDescriptor>(false);
			SimulationDescriptor empireClassDescriptor = null;
			if (database2 != null)
			{
				database2.TryGetValue("ClassEmpire", out empireClassDescriptor);
			}
			int num = 1;
			int num2 = this.World.Regions.Length;
			Map<WorldPosition> map = this.World.Atlas.GetMap(WorldAtlas.Tables.SpawnLocations) as Map<WorldPosition>;
			if (map != null)
			{
				num = map.Data.Length;
			}
			int lobbyData = service2.Session.GetLobbyData<int>("NumberOfMajorFactions", 0);
			Diagnostics.Assert(num == lobbyData, string.Concat(new object[]
			{
				"There are ",
				num,
				" spawn locations when there should be ",
				lobbyData,
				". Spectators used?"
			}));
			List<global::Empire> list = new List<global::Empire>();
			for (int i = 0; i < lobbyData; i++)
			{
				string x = string.Format("Faction{0}", i);
				string lobbyData2 = service2.Session.GetLobbyData<string>(x, null);
				Faction faction2 = Faction.Decode(lobbyData2);
				if (faction2 == null)
				{
					throw new GameException(string.Format("Unable to decode faction from decsriptor (descriptor: '{0}')", lobbyData2));
				}
				string x2 = string.Format("Color{0}", i);
				string lobbyData3 = service2.Session.GetLobbyData<string>(x2, null);
				int color = 0;
				try
				{
					color = int.Parse(lobbyData3);
				}
				catch
				{
				}
				MajorEmpire item = new MajorEmpire("Empire#" + i, (Faction)faction2.Clone(), color);
				list.Add(item);
			}
			ELCPUtilities.NumberOfMajorEmpires = list.Count;
			Diagnostics.Log("ELCP Setting global empire count to " + ELCPUtilities.NumberOfMajorEmpires);
			NavalEmpire navalEmpire = null;
			IDownloadableContentService service3 = Amplitude.Unity.Framework.Services.GetService<IDownloadableContentService>();
			if (service3.IsShared(DownloadableContent16.ReadOnlyName))
			{
				Faction faction3 = null;
				if (database != null && !database.TryGetValue("Fomorians", out faction3))
				{
					Diagnostics.LogError("Failed to retrieve the sea people faction from the database.");
				}
				if (faction3 == null)
				{
					faction3 = faction5;
				}
				int colorIndex = 0;
				if (palette != null && palette.Colors != null)
				{
					colorIndex = UnityEngine.Random.Range(0, palette.Colors.Length);
					StaticString tag = "NavalEmpire";
					XmlColorReference xmlColorReference = palette.Colors.FirstOrDefault((XmlColorReference iterator) => iterator.Tags != null && iterator.Tags.Contains(tag));
					if (xmlColorReference != null)
					{
						colorIndex = Array.IndexOf<XmlColorReference>(palette.Colors, xmlColorReference);
					}
				}
				navalEmpire = new NavalEmpire("NavalEmpire#0", (Faction)faction3.Clone(), colorIndex);
				list.Add(navalEmpire);
			}
			for (int j = 0; j < num2; j++)
			{
				Region region = this.World.Regions[j];
				if (service3.IsShared(DownloadableContent16.ReadOnlyName) && region.IsOcean && navalEmpire != null && navalEmpire.Regions != null && !navalEmpire.Regions.Contains(region))
				{
					navalEmpire.Regions.Add(region);
					region.NavalEmpire = navalEmpire;
				}
				else
				{
					faction5 = null;
					if (!StaticString.IsNullOrEmpty(region.MinorEmpireFactionName))
					{
						if (!database.TryGetValue(region.MinorEmpireFactionName, out faction5))
						{
							Diagnostics.LogError("Unable to retrieve minor faction '{0}' for region #{1}.", new object[]
							{
								region.MinorEmpireFactionName,
								j
							});
						}
						if (faction5 == null)
						{
							database.TryGetValue("RandomMinor", out faction5);
							if (faction5 == null)
							{
								Diagnostics.LogWarning("Failed to retrieve the 'RandomMinor' faction.");
								faction5 = database.GetValues().FirstOrDefault((Faction iterator) => iterator is MinorFaction && !iterator.IsStandard && !iterator.IsRandom);
								if (faction5 == null)
								{
									Diagnostics.LogWarning("Failed to retrieve any single 'minor' faction from the database.");
								}
							}
						}
						if (faction5 != null)
						{
							MinorFaction minorFaction = faction5 as MinorFaction;
							if (minorFaction == null)
							{
								Diagnostics.LogError("The default faction is not a minor faction.");
							}
							else
							{
								MinorFaction minorFaction2 = (MinorFaction)minorFaction.Clone();
								int colorIndex2 = 0;
								if (palette != null && palette.Colors != null)
								{
									colorIndex2 = UnityEngine.Random.Range(0, palette.Colors.Length);
									StaticString tag2 = "MinorFaction";
									XmlColorReference xmlColorReference2 = palette.Colors.FirstOrDefault((XmlColorReference iterator) => iterator.Tags != null && iterator.Tags.Contains(tag2));
									if (xmlColorReference2 != null)
									{
										colorIndex2 = Array.IndexOf<XmlColorReference>(palette.Colors, xmlColorReference2);
									}
								}
								MinorEmpire minorEmpire = new MinorEmpire("MinorEmpire#" + j, minorFaction2, colorIndex2);
								list.Add(minorEmpire);
								if (StaticString.IsNullOrEmpty(region.MinorEmpireFactionName))
								{
									region.MinorEmpireFactionName = minorFaction2.Name;
								}
								minorEmpire.Region = region;
								region.MinorEmpire = minorEmpire;
							}
						}
					}
				}
			}
			if (service3.IsShared(DownloadableContent20.ReadOnlyName) && KaijuCouncil.PlayWithKaiju)
			{
				List<string> list2 = new List<string>
				{
					"Gameplay/Agencies/KaijuCouncil/FirstKaijuSpawnFormula",
					"Gameplay/Agencies/KaijuCouncil/SecondKaijuSpawnFormula",
					"Gameplay/Agencies/KaijuCouncil/ThirdKaijuSpawnFormula"
				};
				Faction[] array = (from faction in database.GetValues()
				where faction is KaijuFaction
				select faction).ToArray<Faction>();
				for (int k = 0; k < array.Length; k++)
				{
					int num3 = UnityEngine.Random.Range(0, this.World.Regions.Length);
					Region region2 = this.World.Regions[num3];
					if (region2.KaijuEmpire != null || region2.IsOcean || region2.IsWasteland)
					{
						for (int l = 0; l < this.World.Regions.Length; l++)
						{
							if (this.World.Regions[l].KaijuEmpire == null && !this.World.Regions[l].IsOcean && !this.World.Regions[l].IsWasteland)
							{
								region2 = this.World.Regions[l];
								break;
							}
						}
					}
					int colorIndex3 = 0;
					if (palette != null && palette.Colors != null)
					{
						colorIndex3 = UnityEngine.Random.Range(0, palette.Colors.Length);
						StaticString tag3 = "MinorFaction";
						XmlColorReference xmlColorReference3 = palette.Colors.FirstOrDefault((XmlColorReference iterator) => iterator.Tags != null && iterator.Tags.Contains(tag3));
						if (xmlColorReference3 != null)
						{
							colorIndex3 = Array.IndexOf<XmlColorReference>(palette.Colors, xmlColorReference3);
						}
					}
					int index = (list2.Count <= 1) ? 0 : UnityEngine.Random.Range(0, list2.Count);
					string spawnFormulaPath = list2[index];
					list2.RemoveAt(index);
					KaijuEmpire item2 = new KaijuEmpire("KaijuEmpire#" + k, (Faction)array[k].Clone(), colorIndex3, spawnFormulaPath);
					list.Add(item2);
				}
			}
			Faction faction4 = null;
			if (database != null && !database.TryGetValue("Lesser-NPCs", out faction4))
			{
				Diagnostics.LogError("Failed to retrieve the lesser npcs faction from the database.");
			}
			if (faction4 == null)
			{
				faction4 = faction5;
			}
			int color2 = 0;
			if (palette != null && palette.Colors != null)
			{
				color2 = UnityEngine.Random.Range(0, palette.Colors.Length);
				StaticString tag4 = "LesserEmpire";
				XmlColorReference xmlColorReference4 = palette.Colors.FirstOrDefault((XmlColorReference iterator) => iterator.Tags != null && iterator.Tags.Contains(tag4));
				if (xmlColorReference4 != null)
				{
					color2 = Array.IndexOf<XmlColorReference>(palette.Colors, xmlColorReference4);
				}
			}
			LesserEmpire item3 = new LesserEmpire("LesserEmpire#0", (Faction)faction4.Clone(), color2);
			list.Add(item3);
			this.Empires = list.ToArray();
			IDatabase<SimulationDescriptor> simulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(true);
			SimulationDescriptor descriptor = null;
			for (int empireIndex4 = 0; empireIndex4 < this.Empires.Length; empireIndex4 = num4 + 1)
			{
				global::Empire empire = this.Empires[empireIndex4];
				Diagnostics.Progress.SetProgress((float)empireIndex4 / (float)this.Empires.Length, string.Format("Initializing empire #{0} out of {1}...", empireIndex4, this.Empires.Length), "Loading...");
				if (empireClassDescriptor != null)
				{
					empire.AddDescriptor(empireClassDescriptor, false);
				}
				if (empire.Faction == null)
				{
					throw new InvalidOperationException();
				}
				List<FactionTrait> list3 = new List<FactionTrait>(Faction.EnumerableTraits(empire.Faction));
				for (int m = 0; m < list3.Count; m++)
				{
					FactionTrait factionTrait = list3[m];
					if (factionTrait != null && factionTrait.SimulationDescriptorReferences != null)
					{
						for (int n = 0; n < factionTrait.SimulationDescriptorReferences.Length; n++)
						{
							if (simulationDescriptorDatabase.TryGetValue(factionTrait.SimulationDescriptorReferences[n], out descriptor))
							{
								empire.SimulationObject.AddDescriptor(descriptor);
							}
							else
							{
								Diagnostics.LogWarning("Failed to find the descriptor for descriptor reference (name: '{1}') on trait (name: '{0}').", new object[]
								{
									factionTrait.Name,
									factionTrait.SimulationDescriptorReferences[n]
								});
							}
						}
					}
				}
				yield return this.Empires[empireIndex4].Initialize(empireIndex4);
				Diagnostics.Assert(this.Empires[empireIndex4].HasBeenInitialized, "The initialization of the empire (index: {0}) has failed.", new object[]
				{
					empireIndex4
				});
				num4 = empireIndex4;
			}
			for (int num5 = 0; num5 < this.Empires.Length; num5++)
			{
				if (this.Empires[num5].Faction != null)
				{
					foreach (FactionTrait trait in Faction.EnumerableTraits(this.Empires[num5].Faction))
					{
						this.ExecuteFactionTraitCommands(this.Empires[num5], trait);
					}
				}
			}
			empireClassDescriptor = null;
			simulationDescriptorDatabase = null;
			empireClassDescriptor = null;
			simulationDescriptorDatabase = null;
			empireClassDescriptor = null;
			simulationDescriptorDatabase = null;
			empireClassDescriptor = null;
			simulationDescriptorDatabase = null;
			empireClassDescriptor = null;
			simulationDescriptorDatabase = null;
			empireClassDescriptor = null;
			simulationDescriptorDatabase = null;
			empireClassDescriptor = null;
			simulationDescriptorDatabase = null;
		}
		Palette palette2;
		if (palettes != null && palettes.TryGetValue(paletteName, out palette2) && palette2.Colors != null && palette2.Colors.Length != 0)
		{
			StaticString tag5 = "MinorFaction";
			XmlColorReference xmlColorReference5 = palette2.Colors.FirstOrDefault((XmlColorReference iterator) => iterator.Tags != null && iterator.Tags.Contains(tag5));
			if (xmlColorReference5 != null)
			{
				global::Game.PrivateersColor = xmlColorReference5.ToColor();
			}
		}
		for (int empireIndex4 = 0; empireIndex4 < this.Empires.Length; empireIndex4 = num4 + 1)
		{
			Diagnostics.Progress.SetProgress((float)empireIndex4 / (float)this.Empires.Length, string.Format("Loading empire #{0} out of {1}...", empireIndex4, this.Empires.Length), "Loading...");
			yield return this.Empires[empireIndex4].Load();
			yield return this.Empires[empireIndex4].LoadGame(this);
			num4 = empireIndex4;
		}
		Diagnostics.Progress.Clear();
		IAchievementService service4 = Amplitude.Unity.Framework.Services.GetService<IAchievementService>();
		if (service4 != null)
		{
			AchievementManager achievementManager = service4 as AchievementManager;
			ISessionService service5 = Amplitude.Unity.Framework.Services.GetService<ISessionService>();
			if (service5 != null && service5.Session != null)
			{
				bool flag = false;
				int num6 = 0;
				while (num6 < this.Empires.Length && this.Empires[num6] is MajorEmpire)
				{
					string x3 = string.Format("Handicap{0}", num6);
					string x4 = string.Format("Empire{0}", num6);
					string lobbyData4 = service5.Session.GetLobbyData<string>(x4, null);
					if (!string.IsNullOrEmpty(lobbyData4))
					{
						if (!lobbyData4.StartsWith("AI"))
						{
							if (service5.Session.GetLobbyData<int>(x3, 5) > 5 && !Amplitude.Unity.Framework.Application.Preferences.ELCPDevMode)
							{
								if (!achievementManager.IsDisabled)
								{
									achievementManager.Disable(true);
								}
								flag = true;
								break;
							}
						}
						else if (service5.Session.GetLobbyData<int>(x3, 5) < 5 && !Amplitude.Unity.Framework.Application.Preferences.ELCPDevMode)
						{
							if (!achievementManager.IsDisabled)
							{
								achievementManager.Disable(true);
							}
							flag = true;
							break;
						}
					}
					num6++;
				}
				if (!flag && !Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools && achievementManager.IsDisabled)
				{
					achievementManager.Disable(false);
				}
			}
		}
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
		int num;
		for (int index = 0; index < base.Ancillaries.Length; index = num + 1)
		{
			GameAncillary gameAncillary = base.Ancillaries[index] as GameAncillary;
			if (gameAncillary != null)
			{
				Diagnostics.Log("trying (type of: {0}) ", new object[]
				{
					gameAncillary.GetType().Name
				});
				timeStamp = DateTime.Now;
				yield return gameAncillary.OnWorldLoaded(world);
				Diagnostics.Log("The game ancillary (type of: {0}) has been loaded (in {1} second(s)).", new object[]
				{
					gameAncillary.GetType().Name,
					(DateTime.Now - timeStamp).TotalSeconds
				});
				yield return null;
			}
			gameAncillary = null;
			num = index;
			gameAncillary = null;
			gameAncillary = null;
			gameAncillary = null;
		}
		Diagnostics.Log("got here");
		yield return Amplitude.Coroutine.WaitForSeconds(1f);
		Diagnostics.Log("got here2");
		try
		{
			this.World = world;
			yield break;
		}
		catch (Exception ex)
		{
			Diagnostics.LogError("Exception: {0}", new object[]
			{
				ex.ToString()
			});
			yield break;
		}
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

	public static void ELCPRevertTempTerraformationsToOriginal(World world, bool undo)
	{
		foreach (World.TemporaryTerraformation temporaryTerraformation in world.TemporaryTerraformations)
		{
			if (temporaryTerraformation.worldPosition.IsValid && temporaryTerraformation.turnsRemaing >= 0)
			{
				StaticString staticString = StaticString.Empty;
				if (undo)
				{
					TerrainTypeMapping terrainTypeMapping = null;
					if (world.TryGetTerraformMapping(temporaryTerraformation.worldPosition, out terrainTypeMapping))
					{
						staticString = terrainTypeMapping.Name;
					}
				}
				else
				{
					staticString = temporaryTerraformation.terrainName;
				}
				if (!StaticString.IsNullOrEmpty(staticString))
				{
					if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
					{
						byte value = world.TerrainMap.GetValue(temporaryTerraformation.worldPosition);
						StaticString empty = StaticString.Empty;
						world.TerrainTypeNameMap.Data.TryGetValue((int)value, ref empty);
						Diagnostics.Log("ELCP ELCPRevertTempTerraformationsToOriginal position {0} changed from {2} to {1}", new object[]
						{
							temporaryTerraformation.worldPosition,
							staticString,
							empty
						});
					}
					byte value2 = (byte)world.TerrainTypeValuesByName[staticString];
					world.TerrainMap.SetValue(temporaryTerraformation.worldPosition, value2);
				}
			}
		}
	}

	private int turn;
}
