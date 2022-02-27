using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Session;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Utilities.Maps;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class KaijuCouncil : Agency, IXmlSerializable
{
	public KaijuCouncil(global::Empire empire) : base(empire)
	{
	}

	public static WorldPosition GetValidKaijuPosition(Region targetRegion, bool randomFallback = false)
	{
		KaijuCouncil.attractivenessMap = (KaijuCouncil.world.Atlas.GetMap(WorldAtlas.Maps.KaijuAttractiveness) as GridMap<bool>);
		WorldPosition[] array = (from position in targetRegion.WorldPositions
		where KaijuCouncil.IsPositionValidForSettleKaiju(position, null)
		select position).ToArray<WorldPosition>();
		WorldPosition result = WorldPosition.Invalid;
		if (array.Length > 0)
		{
			result = array[KaijuCouncil.random.Next(0, array.Length)];
		}
		else if (randomFallback)
		{
			Diagnostics.LogError("Could not find suitable position in starting region!... Picking a random one...");
			result = targetRegion.WorldPositions[KaijuCouncil.random.Next(0, targetRegion.WorldPositions.Length)];
		}
		if (!result.IsValid)
		{
			Diagnostics.LogError("Could not find a valid kaiju position!");
		}
		return result;
	}

	private static float GetTotalIndustrySpent()
	{
		float num = 0f;
		for (int i = 0; i < KaijuCouncil.majorEmpires.Length; i++)
		{
			num += KaijuCouncil.majorEmpires[i].GetPropertyValue("TotalIndustrySpent");
		}
		return num;
	}

	public static Region GetValidKaijuRegion()
	{
		Region[] array = (from region in KaijuCouncil.world.Regions
		where KaijuCouncil.IsRegionValidForSettleKaiju(region)
		select region).ToArray<Region>();
		if (array.Length > 0)
		{
			return array[KaijuCouncil.random.Next(0, array.Length)];
		}
		return null;
	}

	public static bool IsPositionValidForSettleKaiju(WorldPosition worldPosition, Kaiju kaiju = null)
	{
		if (!KaijuCouncil.IsRegionValidForSettleKaiju(KaijuCouncil.worldPositionService.GetRegion(worldPosition)))
		{
			return false;
		}
		Army armyAtPosition = KaijuCouncil.worldPositionService.GetArmyAtPosition(worldPosition);
		return KaijuCouncil.attractivenessMap.GetValue(worldPosition) && (armyAtPosition == null || (kaiju != null && armyAtPosition.Empire.Index == kaiju.Empire.Index && armyAtPosition.GUID == kaiju.KaijuArmy.GUID)) && KaijuCouncil.worldPositionService.GetDistrict(worldPosition) == null;
	}

	public static bool IsRegionValidForSettleKaiju(Region region)
	{
		return region != null && region.Owner == null && region.IsLand && !region.IsRegionColonized();
	}

	public static void UpdateTameCosts()
	{
		IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
		Diagnostics.Assert(database != null);
		int maxEraNumber = DepartmentOfScience.GetMaxEraNumber();
		for (int i = 0; i < database.Count<ArmyAction>(); i++)
		{
			ArmyAction armyAction = database.ElementAt(i);
			if (armyAction is IArmyActionWithKaijuTameCost)
			{
				KaijuTameCost tameCost = (armyAction as IArmyActionWithKaijuTameCost).TameCost;
				tameCost.UpdateActiveCost(maxEraNumber);
			}
		}
	}

	public static KaijuTameCost GetKaijuTameCost()
	{
		IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
		ArmyAction armyAction = null;
		if (database == null || !database.TryGetValue(ArmyAction_TameUnstunnedKaiju.ReadOnlyName, out armyAction))
		{
			return null;
		}
		return (armyAction as IArmyActionWithKaijuTameCost).TameCost;
	}

	private static void StaticInitialize()
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null, "Failed to retrieve game service");
		global::Game game = service.Game as global::Game;
		Diagnostics.Assert(game != null, "Failed to retrieve game reference");
		KaijuCouncil.attractivenessMap = null;
		KaijuCouncil.majorEmpires = null;
		KaijuCouncil.attractivenessDatabase = Databases.GetDatabase<KaijuAttractivenessRule>(false);
		Diagnostics.Assert(KaijuCouncil.attractivenessDatabase != null, "Failed to retrieve attractiveness database");
		KaijuCouncil.world = game.World;
		Diagnostics.Assert(KaijuCouncil.world != null, "Failed to retrieve game world");
		KaijuCouncil.worldPositionService = game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(KaijuCouncil.worldPositionService != null, "Failed to retrieve world positioning service");
		if (game.Empires != null)
		{
			KaijuCouncil.majorEmpires = Array.ConvertAll<global::Empire, MajorEmpire>(Array.FindAll<global::Empire>(game.Empires, (global::Empire match) => match is MajorEmpire), (global::Empire empire) => empire as MajorEmpire);
			if (KaijuCouncil.majorEmpires.Length == 0)
			{
				Diagnostics.LogError("No MajorEmpires were retrieved");
			}
		}
		KaijuCouncil.random = new Random(World.Seed);
	}

	private static void PreComputeAttractiveness()
	{
		KaijuCouncil.attractivenessMap = (KaijuCouncil.world.Atlas.GetMap(WorldAtlas.Maps.KaijuAttractiveness) as GridMap<bool>);
		if (KaijuCouncil.attractivenessMap == null)
		{
			bool[,] array = new bool[(int)KaijuCouncil.world.WorldParameters.Rows, (int)KaijuCouncil.world.WorldParameters.Columns];
			GridMap<bool> gridMap = KaijuCouncil.world.Atlas.GetMap(WorldAtlas.Maps.Ridges) as GridMap<bool>;
			GridMap<byte> gridMap2 = KaijuCouncil.world.Atlas.GetMap(WorldAtlas.Maps.WaterType) as GridMap<byte>;
			GridMap<byte> map = KaijuCouncil.world.Atlas.GetMap(WorldAtlas.Maps.Anomalies) as GridMap<byte>;
			Map<AnomalyTypeDefinition> map2 = KaijuCouncil.world.Atlas.GetMap(WorldAtlas.Tables.Anomalies) as Map<AnomalyTypeDefinition>;
			GridMap<PointOfInterest> map3 = KaijuCouncil.world.Atlas.GetMap(WorldAtlas.Maps.PointOfInterest) as GridMap<PointOfInterest>;
			for (int i = 0; i < (int)KaijuCouncil.world.WorldParameters.Rows; i++)
			{
				for (int j = 0; j < (int)KaijuCouncil.world.WorldParameters.Columns; j++)
				{
					array[i, j] = true;
					WorldPosition worldPosition = new WorldPosition(i, j);
					array[i, j] &= (gridMap2.GetValue(i, j) == 0);
					if (array[i, j])
					{
						array[i, j] &= !gridMap.GetValue(i, j);
						if (array[i, j])
						{
							StaticString key = string.Empty;
							KaijuAttractivenessRule kaijuAttractivenessRule;
							if (map2.Data.TryGetValue((int)map.GetValue(worldPosition), ref key) && KaijuCouncil.attractivenessDatabase.TryGetValue(key, out kaijuAttractivenessRule))
							{
								array[i, j] &= ((byte)kaijuAttractivenessRule.Value != 0);
								if (!array[i, j])
								{
									goto IL_243;
								}
							}
							PointOfInterest value = map3.GetValue(worldPosition);
							if (value != null && KaijuCouncil.attractivenessDatabase.TryGetValue(value.PointOfInterestDefinition.PointOfInterestTemplateName, out kaijuAttractivenessRule))
							{
								array[i, j] &= ((byte)kaijuAttractivenessRule.Value != 0);
								if (!array[i, j])
								{
								}
							}
						}
					}
					IL_243:;
				}
			}
			KaijuCouncil.attractivenessMap = new GridMap<bool>(WorldAtlas.Maps.KaijuAttractiveness, (int)KaijuCouncil.world.WorldParameters.Columns, (int)KaijuCouncil.world.WorldParameters.Rows, array);
			KaijuCouncil.world.Atlas.RegisterMapInstance<GridMap<bool>>(KaijuCouncil.attractivenessMap);
		}
	}

	public override void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		base.ReadXml(reader);
		GameEntityGUID guid = new GameEntityGUID(reader.GetAttribute<ulong>("GUID"));
		Kaiju kaiju = new Kaiju(this.KaijuEmpire, guid);
		reader.ReadElementSerializable<Kaiju>(ref kaiju);
		if (kaiju != null)
		{
			this.Kaiju = kaiju;
		}
		this.relocationETA = reader.ReadElementString<int>("relocationETA");
		this.kaijuSpawnTurn = reader.ReadElementString<int>("kaijuSpawnTurn");
		this.lastLiceArmySpawnTurn = reader.ReadElementString<int>("lastLiceArmySpawnTurn");
		this.liceArmies.Clear();
		this.liceArmiesCache = null;
		if (num >= 2)
		{
			int attribute = reader.GetAttribute<int>("liceArmiesCount");
			reader.ReadStartElement("liceArmies");
			for (int i = 0; i < attribute; i++)
			{
				this.liceArmies.Add(new GameEntityGUID(reader.ReadElementString<ulong>("liceArmyGUID")));
			}
			reader.ReadEndElement("liceArmies");
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteVersionAttribute(2);
		base.WriteXml(writer);
		IXmlSerializable xmlSerializable = null;
		if (this.Kaiju != null)
		{
			xmlSerializable = this.Kaiju;
		}
		writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
		writer.WriteElementString<int>("relocationETA", this.relocationETA);
		writer.WriteElementString<int>("kaijuSpawnTurn", this.kaijuSpawnTurn);
		writer.WriteElementString<int>("lastLiceArmySpawnTurn", this.lastLiceArmySpawnTurn);
		writer.WriteStartElement("liceArmies");
		writer.WriteAttributeString<int>("liceArmiesCount", this.LiceArmies.Length);
		for (int i = 0; i < this.LiceArmies.Length; i++)
		{
			writer.WriteElementString<ulong>("liceArmyGUID", this.LiceArmies[i]);
		}
		writer.WriteEndElement();
	}

	public KaijuEmpire KaijuEmpire
	{
		get
		{
			return base.Empire as KaijuEmpire;
		}
	}

	public Kaiju Kaiju { get; private set; }

	public GameEntityGUID[] LiceArmies
	{
		get
		{
			if (this.liceArmiesCache == null)
			{
				this.liceArmiesCache = new GameEntityGUID[this.liceArmies.Count];
				this.liceArmies.CopyTo(this.liceArmiesCache);
			}
			return this.liceArmiesCache;
		}
	}

	public int RelocationETA
	{
		get
		{
			return this.relocationETA;
		}
	}

	public static bool PlayWithKaiju
	{
		get
		{
			return Amplitude.Unity.Framework.Application.Registry.GetValue<bool>(KaijuCouncil.PlayWithKaijuGameplaySetting, true);
		}
	}

	public void CleanAfterEncounter(KaijuArmy kaijuArmy, Encounter encounter)
	{
		this.CleanAfterEncounter(kaijuArmy.Kaiju, kaijuArmy.GUID, encounter);
	}

	public void CleanAfterEncounter(KaijuGarrison garrison, Encounter encounter)
	{
		this.CleanAfterEncounter(garrison.Kaiju, garrison.GUID, encounter);
	}

	private void CleanAfterEncounter(Kaiju kaiju, GameEntityGUID garrisonGUID, Encounter encounter)
	{
		bool setKaijuFree = false;
		bool flag = false;
		foreach (Contender contender in encounter.GetAlliedContendersFromEmpire(kaiju.Empire))
		{
			if (contender.IsTakingPartInBattle && contender.GUID == garrisonGUID && contender.ContenderState == ContenderState.Defeated)
			{
				flag = true;
				break;
			}
		}
		if (kaiju.IsTamed())
		{
			switch (encounter.GetEncounterResultForEmpire(kaiju.Empire))
			{
			case EncounterResult.Draw:
				setKaijuFree = true;
				break;
			case EncounterResult.Victory:
				setKaijuFree = true;
				break;
			}
		}
		if (flag)
		{
			global::Empire stunner = null;
			IEnumerable<Contender> enemiesContenderFromEmpire = encounter.GetEnemiesContenderFromEmpire(kaiju.Empire);
			if (enemiesContenderFromEmpire.Count<Contender>() > 0)
			{
				Contender contender2 = enemiesContenderFromEmpire.FirstOrDefault<Contender>();
				if (contender2.Empire != null)
				{
					stunner = encounter.GetEnemiesContenderFromEmpire(kaiju.Empire).FirstOrDefault<Contender>().Empire;
				}
			}
			this.KaijuLostEncounter(kaiju, stunner, setKaijuFree);
		}
	}

	private void KaijuLostEncounter(Kaiju kaiju, global::Empire stunner, bool setKaijuFree)
	{
		bool flag = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<bool>("Gameplay/Kaiju/KaijuAutoTameBeforeLoseEncounter", true);
		bool relocate = false;
		if (setKaijuFree)
		{
			flag = false;
			relocate = true;
		}
		global::PlayerController server = this.KaijuEmpire.PlayerControllers.Server;
		if (kaiju.IsTamed())
		{
			MajorEmpire majorEmpire = kaiju.MajorEmpire;
			if (server != null)
			{
				OrderUntameKaiju order = new OrderUntameKaiju(kaiju, relocate, stunner.Index, flag);
				server.PostOrder(order);
			}
		}
		else if (flag)
		{
			if (server != null)
			{
				OrderTameKaiju order2 = new OrderTameKaiju(stunner.Index, kaiju, null);
				server.PostOrder(order2);
			}
		}
		else
		{
			this.Kaiju.ChangeToStunState(stunner);
			this.ResetRelocationETA();
		}
	}

	public float GetKaijuRelocationFrequency()
	{
		if (KaijuCouncil.RelocationFrequencyFormula == null)
		{
			string value = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<string>("Gameplay/Agencies/KaijuCouncil/RelocationFrequencyFormula");
			KaijuCouncil.RelocationFrequencyFormula = Interpreter.InfixTransform(value);
		}
		if (KaijuCouncil.RelocationInterpreterContext == null)
		{
			KaijuCouncil.RelocationInterpreterContext = new InterpreterContext(null);
		}
		KaijuCouncil.RelocationInterpreterContext.SimulationObject = base.Empire.SimulationObject;
		KaijuCouncil.RelocationInterpreterContext.Register("BaseRelocationFrequency", this.KaijuEmpire.KaijuFaction.RelocationFrequency);
		float num = (float)Interpreter.Execute(KaijuCouncil.RelocationFrequencyFormula, KaijuCouncil.RelocationInterpreterContext);
		Diagnostics.Log("[Kaiju] Kaiju Empire [{0}] Relocation Formula Result: [{1}]", new object[]
		{
			this.KaijuEmpire.KaijuFaction.LocalizedName,
			num
		});
		return num;
	}

	public float GetIndustryNeededToSpawn()
	{
		string value = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<string>(this.KaijuEmpire.SpawnFormulaPath);
		object[] rpn = Interpreter.InfixTransform(value);
		InterpreterContext interpreterContext = new InterpreterContext(null);
		interpreterContext.SimulationObject = base.Empire.SimulationObject;
		interpreterContext.Register("NumberOfPlayers", KaijuCouncil.majorEmpires.Length);
		float num = (float)Interpreter.Execute(rpn, interpreterContext);
		Diagnostics.Log("[Kaiju] Kaiju Empire [{0}] formula = n * (NumberOfPlayers = {1}) * (GameSpeedMultiplier = {2}) * (GameDifficultyKaijuSpawnMultiplier = {3})", new object[]
		{
			this.KaijuEmpire.KaijuFaction.LocalizedName,
			KaijuCouncil.majorEmpires.Length,
			this.KaijuEmpire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier),
			this.KaijuEmpire.GetPropertyValue("GameDifficultyKaijuSpawnMultiplier")
		});
		Diagnostics.Log("[Kaiju] Kaiju Empire [{0}] Industry needed to Spawn: [{1}]", new object[]
		{
			this.KaijuEmpire.KaijuFaction.LocalizedName,
			num
		});
		return num;
	}

	public void MajorEmpireTameKaiju(MajorEmpire owner, bool clearMilitias = false)
	{
		if (owner == null)
		{
			throw new ArgumentNullException("Empire trying to tame Kaiju is null");
		}
		if (clearMilitias)
		{
			this.Kaiju.ClearMilitias();
		}
		this.departmentOfDefense.RemoveArmy(this.Kaiju.KaijuArmy, false);
		this.Kaiju.SetOwner(owner);
		owner.AddTamedKaiju(this.Kaiju);
		this.Kaiju.ChangeToTamedState(owner);
		this.KaijuEmpire.RemoveKaiju(this.Kaiju);
		this.KaijuEmpire.Refresh(false);
		this.Kaiju = null;
	}

	public void MajorEmpireUntameKaiju(Kaiju kaiju, int instigatorEmpireIndex, bool clearMilitias = false)
	{
		if (kaiju.MajorEmpire == null || !(kaiju.MajorEmpire is MajorEmpire))
		{
			Diagnostics.LogError("Kaiju has an invalid owner.");
		}
		if (this.KaijuEmpire.Index != kaiju.KaijuEmpire.Index)
		{
			Diagnostics.LogError("KaijuCouncil KaijuEmpire must be the same as Kaiju KaijuEmpire!");
		}
		if (clearMilitias)
		{
			kaiju.ClearMilitias();
		}
		kaiju.RemoveOwner(instigatorEmpireIndex);
		this.departmentOfDefense.AddArmy(kaiju.KaijuArmy);
		kaiju.ChangeToWildState();
		this.KaijuEmpire.AddChild(kaiju.KaijuGarrison);
		if (kaiju.KaijuArmy != null)
		{
			this.KaijuEmpire.AddChild(kaiju.KaijuArmy);
		}
		this.KaijuEmpire.AddKaiju(kaiju);
		this.KaijuEmpire.Refresh(false);
		this.Kaiju = kaiju;
	}

	public void TryRelocateKaijuOrResetETA()
	{
		global::PlayerController server = this.KaijuEmpire.PlayerControllers.Server;
		Region validKaijuRegion = KaijuCouncil.GetValidKaijuRegion();
		if (validKaijuRegion == null)
		{
			if (this.Kaiju != null)
			{
				this.ResetRelocationETA();
			}
			Diagnostics.LogWarning("Unable to find suitable region. Reset Kaiju ETA!");
			return;
		}
		WorldPosition validKaijuPosition = KaijuCouncil.GetValidKaijuPosition(validKaijuRegion, false);
		if (validKaijuPosition == WorldPosition.Zero)
		{
			if (this.Kaiju != null)
			{
				this.ResetRelocationETA();
			}
			Diagnostics.LogWarning("Unable to find suitable position in target region. Reset Kaiju ETA.");
			return;
		}
		if (server != null && this.Kaiju != null && this.Kaiju.OnGarrisonMode() && this.Kaiju.IsWild())
		{
			IPlayerControllerRepositoryService service = this.gameService.Game.Services.GetService<IPlayerControllerRepositoryService>();
			OrderRelocateKaiju order = new OrderRelocateKaiju(this.Kaiju.GUID, validKaijuPosition);
			server.PostOrder(order);
		}
	}

	public void ResetRelocationETA()
	{
		this.relocationETA = (int)Math.Ceiling((double)this.GetKaijuRelocationFrequency());
	}

	public void SpawnKaiju(WorldPosition targetPosition, GameEntityGUID kaijuGUID, GameEntityGUID garrisonGUID, GameEntityGUID armyGUID, GameEntityGUID monsterGUID, GameEntityGUID[] licesGUIDs)
	{
		Kaiju kaiju = new Kaiju(base.Empire as KaijuEmpire, kaijuGUID);
		string[] kaijuDescriptors = this.KaijuEmpire.KaijuFaction.KaijuDescriptors;
		for (int i = 0; i < kaijuDescriptors.Length; i++)
		{
			SimulationDescriptor descriptor = null;
			if (this.descriptorsDatabase.TryGetValue(kaijuDescriptors[i], out descriptor))
			{
				kaiju.AddDescriptor(descriptor, false);
			}
		}
		KaijuGarrison kaijuGarrison = new KaijuGarrison(garrisonGUID, targetPosition);
		SimulationDescriptor value = this.descriptorsDatabase.GetValue(Kaiju.ClassKaijuGarrison);
		kaijuGarrison.AddDescriptor(value, false);
		kaijuGarrison.SetPropertyBaseValue(SimulationProperties.MaximumUnitSlotCount, (float)(licesGUIDs.Length + 1));
		kaiju.SetGarrison(kaijuGarrison);
		KaijuArmy kaijuArmy = this.departmentOfDefense.CreateKaijuArmy(armyGUID, targetPosition, true);
		SimulationDescriptor value2 = this.descriptorsDatabase.GetValue(Kaiju.ClassKaijuArmy);
		kaijuArmy.AddDescriptor(value2, false);
		kaijuArmy.SetPropertyBaseValue(SimulationProperties.MaximumUnitSlotCount, (float)(licesGUIDs.Length + 1));
		kaiju.SetArmy(kaijuArmy);
		this.gameEntityRepositoryService.Register(kaijuArmy);
		this.gameEntityRepositoryService.Register(kaiju);
		kaiju.ChangeToWildState();
		kaiju.ChangeToGarrisonMode(false);
		Unit unit = DepartmentOfDefense.CreateUnitByDesign(monsterGUID, this.KaijuEmpire.FindMonsterDesign(true));
		kaiju.AddUnit(unit);
		for (int j = 0; j < licesGUIDs.Length; j++)
		{
			Unit unit2 = DepartmentOfDefense.CreateUnitByDesign(licesGUIDs[j], this.KaijuEmpire.FindLiceDesign(true));
			kaiju.AddUnit(unit2);
		}
		this.lastLiceArmySpawnTurn = (this.gameService.Game as global::Game).Turn;
		this.Kaiju = kaiju;
		this.KaijuEmpire.AddKaiju(kaiju);
		kaiju.Refresh(false);
		foreach (global::Empire empire in (this.gameService.Game as global::Game).Empires)
		{
			if (empire is MajorEmpire)
			{
				this.eventService.Notify(new EventKaijuSpawned(empire, this.Kaiju));
			}
		}
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (this.gameEntityRepositoryService != null && this.Kaiju != null)
		{
			if (this.Kaiju.KaijuGarrison != null)
			{
				this.Kaiju.ClearAllUnits();
			}
			this.gameEntityRepositoryService.Unregister(this.Kaiju);
			this.Kaiju = null;
		}
	}

	protected override IEnumerator OnInitialize()
	{
		this.descriptorsDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
		Diagnostics.Assert(this.descriptorsDatabase != null);
		this.gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(this.gameService != null);
		this.endTurnService = Services.GetService<IEndTurnService>();
		Diagnostics.Assert(this.endTurnService != null);
		this.eventService = Services.GetService<IEventService>();
		Diagnostics.Assert(this.eventService != null);
		this.eventService.EventRaise += this.EventService_EventRaise;
		this.gameEntityRepositoryService = this.gameService.Game.Services.GetService<IGameEntityRepositoryService>();
		Diagnostics.Assert(this.gameEntityRepositoryService != null);
		this.gameEntityRepositoryService.GameEntityUnregistered += this.GameEntityRepositoryService_GameEntityUnregistered;
		this.seasonService = this.gameService.Game.Services.GetService<ISeasonService>();
		Diagnostics.Assert(this.seasonService != null);
		this.worldPositionningService = this.gameService.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		KaijuCouncil.StaticInitialize();
		KaijuCouncil.PreComputeAttractiveness();
		base.Empire.RegisterPass("GameServerState_Turn_Begin", "KaijuCouncilServerTurnBegin", new Agency.Action(this.GameServerState_Turn_Begin), new string[0]);
		base.Empire.RegisterPass("GameServerState_Turn_End", "KaijuCouncilServerTurnEnd", new Agency.Action(this.GameServerState_Turn_End), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_Begin", "KaijuCouncilClientTurnBegin", new Agency.Action(this.GameClientState_Turn_Begin), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_End", "KaijuCouncilClientTurnEnd", new Agency.Action(this.GameClientState_Turn_End), new string[0]);
		this.departmentOfDefense = base.Empire.GetAgency<DepartmentOfDefense>();
		this.departmentOfEducation = base.Empire.GetAgency<DepartmentOfEducation>();
		this.ResetRelocationETA();
		yield return base.OnInitialize();
		yield break;
	}

	protected override IEnumerator OnLoadGame(Amplitude.Unity.Game.Game game)
	{
		yield return base.OnLoadGame(game);
		KaijuCouncil.StaticInitialize();
		KaijuCouncil.PreComputeAttractiveness();
		if (this.Kaiju != null)
		{
			yield return this.Kaiju.OnLoadGame(game);
			this.gameEntityRepositoryService.Register(this.Kaiju);
			base.Empire.AddChild(this.Kaiju);
			base.Empire.Refresh(false);
		}
		this.ComputeTameCosts(game as global::Game);
		KaijuCouncil.UpdateTameCosts();
		yield break;
	}

	private void ComputeTameCosts(global::Game game)
	{
		Random random = new Random(World.Seed);
		IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
		Diagnostics.Assert(database != null);
		Dictionary<string, List<PointOfInterestTemplate>> dictionary = KaijuTechsManager.ComputeLuxuryAbundance(game);
		for (int i = 0; i < database.Count<ArmyAction>(); i++)
		{
			ArmyAction armyAction = database.ElementAt(i);
			if (armyAction is IArmyActionWithKaijuTameCost)
			{
				KaijuTameCost tameCost = (armyAction as IArmyActionWithKaijuTameCost).TameCost;
				for (int j = 0; j < tameCost.CostDefinitions.Length; j++)
				{
					KaijuTameCost.CostDefinition costDefinition = tameCost.CostDefinitions[j];
					List<PointOfInterestTemplate> list = new List<PointOfInterestTemplate>();
					list.AddRange(dictionary[costDefinition.LuxuryTier]);
					PointOfInterestTemplate pointOfInterestTemplate = list[random.Next(list.Count)];
					string empty = string.Empty;
					if (pointOfInterestTemplate.Properties.TryGetValue("ResourceName", out empty))
					{
						costDefinition.SetResourceName(empty);
					}
				}
			}
		}
	}

	protected override void OnRelease()
	{
		base.OnRelease();
		if (this.eventService != null)
		{
			this.eventService.EventRaise -= this.EventService_EventRaise;
			this.eventService = null;
		}
		if (this.gameEntityRepositoryService != null)
		{
			this.gameEntityRepositoryService.GameEntityUnregistered -= this.GameEntityRepositoryService_GameEntityUnregistered;
			this.gameEntityRepositoryService = null;
		}
		this.gameService = null;
		this.descriptorsDatabase = null;
		if (this.Kaiju != null)
		{
			this.Kaiju.Release();
			this.Kaiju = null;
		}
		this.liceArmies.Clear();
		this.liceArmiesCache = null;
	}

	private void Client_TurnBegin_CheckKaijuStatus()
	{
		if (this.Kaiju != null)
		{
			if (this.Kaiju.IsStunned())
			{
				if (this.Kaiju.NextTurnToRecoverFromStun > 0 && (this.gameService.Game as global::Game).Turn == this.Kaiju.NextTurnToRecoverFromStun)
				{
					this.Kaiju.ChangeToWildState();
				}
			}
			else if (this.Kaiju.IsWild() && this.Kaiju.OnArmyMode())
			{
				global::PlayerController server = this.KaijuEmpire.PlayerControllers.Server;
				if (server != null)
				{
					Region validKaijuRegion = KaijuCouncil.GetValidKaijuRegion();
					if (validKaijuRegion == null)
					{
						Diagnostics.LogWarning("Unable to find suitable region");
						return;
					}
					WorldPosition validKaijuPosition = KaijuCouncil.GetValidKaijuPosition(validKaijuRegion, false);
					if (validKaijuPosition == WorldPosition.Zero)
					{
						Diagnostics.LogWarning("Unable to find suitable position in target region");
						return;
					}
					OrderRelocateKaiju order = new OrderRelocateKaiju(this.Kaiju.GUID, validKaijuPosition);
					server.PostOrder(order);
				}
			}
		}
	}

	private void Client_TurnBegin_CheckIndustryThreshold()
	{
		if (this.kaijuSpawnTurn == -1 && !this.KaijuEmpire.HasSpawnedAnyKaiju)
		{
			float totalIndustrySpent = KaijuCouncil.GetTotalIndustrySpent();
			float industryNeededToSpawn = this.GetIndustryNeededToSpawn();
			Diagnostics.Log("[Kaiju] Total Industry Spent: [{0}]", new object[]
			{
				totalIndustrySpent
			});
			if (totalIndustrySpent >= industryNeededToSpawn)
			{
				this.kaijuSpawnTurn = (this.gameService.Game as global::Game).Turn + Amplitude.Unity.Runtime.Runtime.Registry.GetValue<int>(KaijuCouncil.TurnsToSpawnPath);
				if (this.KaijuEmpire.GetPropertyValue(SimulationProperties.SpawnedKaijusGlobalCounter) <= 0f)
				{
					global::PlayerController server = this.KaijuEmpire.PlayerControllers.Server;
					if (server != null)
					{
						OrderAnnounceFirstKaiju order = new OrderAnnounceFirstKaiju();
						server.PostOrder(order);
					}
				}
			}
		}
	}

	private void Client_TurnBegin_CheckKaijuSpawn()
	{
		if ((this.gameService.Game as global::Game).Turn == this.kaijuSpawnTurn && !this.KaijuEmpire.HasSpawnedAnyKaiju)
		{
			global::PlayerController server = this.KaijuEmpire.PlayerControllers.Server;
			if (server != null)
			{
				OrderSpawnKaiju order = new OrderSpawnKaiju(this.KaijuEmpire.Index);
				server.PostOrder(order);
			}
		}
	}

	private void Client_TurnEnded_CheckKaijuRelocation()
	{
		if (this.Kaiju != null && this.Kaiju.OnGarrisonMode() && this.Kaiju.IsWild() && this.seasonService.GetCurrentSeason().SeasonDefinition.SeasonType != Season.ReadOnlyWinter)
		{
			this.relocationETA--;
			if (this.relocationETA <= 0)
			{
				this.TryRelocateKaijuOrResetETA();
			}
		}
	}

	private void EventService_EventRaise(object sender, EventRaiseEventArgs e)
	{
		if (e.RaisedEvent.EventName == EventNewEraGlobal.Name)
		{
			KaijuCouncil.UpdateTameCosts();
		}
	}

	private IEnumerator GameClientState_Turn_Begin(string context, string name)
	{
		if (this.Kaiju != null)
		{
			this.Kaiju.GameClientState_Turn_Begin();
		}
		this.Client_TurnBegin_CheckIndustryThreshold();
		this.Client_TurnBegin_CheckKaijuSpawn();
		this.Client_TurnBegin_CheckKaijuStatus();
		yield break;
	}

	private IEnumerator GameClientState_Turn_End(string context, string name)
	{
		if (this.Kaiju != null)
		{
			this.Kaiju.GameClientState_Turn_Ended();
		}
		this.Client_TurnEnded_CheckKaijuRelocation();
		yield break;
	}

	private void GameEntityRepositoryService_GameEntityUnregistered(object sender, GameEntityUnregisteredEventArgs e)
	{
		if (this.liceArmies.Contains(e.GUID))
		{
			this.liceArmies.Remove(e.GUID);
			this.liceArmiesCache = null;
		}
	}

	private IEnumerator GameServerState_Turn_Begin(string context, string name)
	{
		if (this.Kaiju != null)
		{
			this.Kaiju.GameServerState_Turn_Begin();
			this.Server_TurnBegin_CheckLiceArmySpawn();
		}
		yield break;
	}

	private IEnumerator GameServerState_Turn_End(string context, string name)
	{
		if (this.Kaiju != null)
		{
			this.Kaiju.GameServerState_Turn_End();
		}
		yield break;
	}

	private void OrderSpawnLiceArmies_TicketRaised(object sender, TicketRaisedEventArgs args)
	{
		if (args.Result != PostOrderResponse.Processed)
		{
			return;
		}
		OrderSpawnArmies orderSpawnArmies = (OrderSpawnArmies)args.Order;
		this.liceArmies.UnionWith(orderSpawnArmies.ArmiesGUIDs);
		this.liceArmiesCache = null;
		global::Game game = this.gameService.Game as global::Game;
		this.lastLiceArmySpawnTurn = game.Turn;
	}

	private void Server_TurnBegin_CheckLiceArmySpawn()
	{
		global::Game game = this.gameService.Game as global::Game;
		Diagnostics.Assert(game != null);
		ISessionService service = Services.GetService<ISessionService>();
		Diagnostics.Assert(service != null);
		global::Session session = service.Session as global::Session;
		Diagnostics.Assert(session != null);
		KaijuCouncil.WildLiceArmyPreferences.LoadRegistryValues(session);
		if (this.LiceArmies.Length >= KaijuCouncil.WildLiceArmyPreferences.MaxArmiesAlive)
		{
			return;
		}
		int turn = game.Turn;
		int num = (int)Math.Ceiling((double)((float)KaijuCouncil.WildLiceArmyPreferences.SpawningCooldown * base.Empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier)));
		if (turn < this.lastLiceArmySpawnTurn + num)
		{
			return;
		}
		int num2 = this.Kaiju.OnGarrisonMode() ? KaijuCouncil.WildLiceArmyPreferences.ArmiesToSpawnCount : 0;
		if (num2 <= 0)
		{
			return;
		}
		List<WorldPosition> list = new List<WorldPosition>();
		for (int i = 0; i < this.Kaiju.Region.WorldPositions.Length; i++)
		{
			WorldPosition worldPosition = this.Kaiju.Region.WorldPositions[i];
			if (DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(worldPosition, PathfindingMovementCapacity.Ground) && worldPosition != this.Kaiju.WorldPosition)
			{
				list.Add(worldPosition);
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		Random randomizer = new Random(World.Seed + turn);
		IEnumerable<WorldPosition> source = from position in list
		orderby this.worldPositionningService.GetDistance(this.Kaiju.WorldPosition, position), randomizer.NextDouble()
		select position;
		list = source.ToList<WorldPosition>();
		WorldPosition[] worldPositions;
		if (list.Count > num2)
		{
			worldPositions = list.Take(Math.Min(list.Count, num2)).ToArray<WorldPosition>();
		}
		else
		{
			worldPositions = list.ToArray();
		}
		List<StaticString> list2 = new List<StaticString>();
		StaticString name = this.KaijuEmpire.FindLiceDesign(true).Name;
		int unitsPerArmy = KaijuCouncil.WildLiceArmyPreferences.UnitsPerArmy;
		for (int j = 0; j < unitsPerArmy; j++)
		{
			list2.Add(name);
		}
		List<StaticString> list3 = new List<StaticString>();
		list3.Add(KaijuCouncil.WildLiceArmyPreferences.ArmyTag);
		List<StaticString> list4 = new List<StaticString>();
		list4.Add(KaijuCouncil.WildLiceArmyPreferences.UnitTag);
		MinorEmpire minorEmpire = null;
		for (int k = 0; k < game.Empires.Length; k++)
		{
			if (game.Empires[k] is MinorEmpire)
			{
				minorEmpire = (game.Empires[k] as MinorEmpire);
				break;
			}
		}
		OrderSpawnArmies order = new OrderSpawnArmies(minorEmpire.Index, worldPositions, list2.ToArray(), KaijuCouncil.WildLiceArmyPreferences.ArmiesToSpawnCount, list3.ToArray(), list4.ToArray(), Math.Max(0, DepartmentOfScience.GetMaxEraNumber() - 1), QuestArmyObjective.QuestBehaviourType.Offense);
		Ticket ticket = null;
		minorEmpire.PlayerControllers.Server.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderSpawnLiceArmies_TicketRaised));
	}

	public static string FindKaijuFactionIconPath(global::Empire empire, StaticString size)
	{
		string text = KaijuCouncil.KaijuGenericIconName;
		string text2 = size;
		if (text2 == "LogoLarge")
		{
			text2 = "Large";
		}
		else if (text2 == "LogoSmall")
		{
			text2 = "Small";
		}
		if (empire.SimulationObject.Tags.Contains("AffinityKaijus1"))
		{
			text = "Kaiju1Icon";
		}
		else if (empire.SimulationObject.Tags.Contains("AffinityKaijus2"))
		{
			text = "Kaiju2Icon";
		}
		else if (empire.SimulationObject.Tags.Contains("AffinityKaijus3"))
		{
			text = "Kaiju3Icon";
		}
		global::IGuiService service = Services.GetService<global::IGuiService>();
		GuiElement guiElement;
		if (service.GuiPanelHelper.TryGetGuiElement(text, out guiElement))
		{
			text = guiElement.Icons[text2];
		}
		return text;
	}

	public static string FindKaijuFactionIconPath(Army army, StaticString size)
	{
		string text = KaijuCouncil.KaijuGenericIconName;
		string text2 = size;
		if (text2 == "LogoLarge")
		{
			text2 = "Large";
		}
		else if (text2 == "LogoSmall")
		{
			text2 = "Small";
		}
		if (army.Units.Any((Unit m) => m.UnitDesign.Tags.Contains("AffinityKaijus1")))
		{
			text = "Kaiju1Icon";
		}
		else if (army.Units.Any((Unit m) => m.UnitDesign.Tags.Contains("AffinityKaijus2")))
		{
			text = "Kaiju2Icon";
		}
		else if (army.Units.Any((Unit m) => m.UnitDesign.Tags.Contains("AffinityKaijus3")))
		{
			text = "Kaiju3Icon";
		}
		global::IGuiService service = Services.GetService<global::IGuiService>();
		GuiElement guiElement;
		if (service.GuiPanelHelper.TryGetGuiElement(text, out guiElement))
		{
			text = guiElement.Icons[text2];
		}
		return text;
	}

	private static IDatabase<KaijuAttractivenessRule> attractivenessDatabase;

	private static GridMap<bool> attractivenessMap;

	private static World world;

	private static IWorldPositionningService worldPositionService;

	private static MajorEmpire[] majorEmpires;

	private static Random random;

	public static readonly StaticString PlayWithKaijuGameplaySetting = "Settings/Game/PlayWithKaiju";

	public static readonly StaticString KaijuWinterEffectsDescriptor = "KaijuWinterEffects";

	private static readonly StaticString KaijuGenericIconName = "kaijuFactionLogoSmall";

	private static object[] RelocationFrequencyFormula;

	private static InterpreterContext RelocationInterpreterContext;

	private static readonly string TurnsToSpawnPath = "Gameplay/Agencies/KaijuCouncil/KaijuSpawnTurns";

	private DepartmentOfDefense departmentOfDefense;

	private DepartmentOfEducation departmentOfEducation;

	private IDatabase<SimulationDescriptor> descriptorsDatabase;

	private IEndTurnService endTurnService;

	private IEventService eventService;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private IGameService gameService;

	private int lastLiceArmySpawnTurn = -1;

	private readonly HashSet<GameEntityGUID> liceArmies = new HashSet<GameEntityGUID>();

	private GameEntityGUID[] liceArmiesCache;

	private int kaijuSpawnTurn = -1;

	private int relocationETA = -1;

	private ISeasonService seasonService;

	private IWorldPositionningService worldPositionningService;

	public static class WildLiceArmyPreferences
	{
		public static int ArmiesToSpawnCount { get; private set; }

		public static int SpawningCooldown { get; private set; }

		public static int UnitsPerArmy { get; private set; }

		public static int MaxArmiesAlive { get; private set; }

		public static StaticString ArmyTag { get; private set; }

		public static StaticString UnitTag { get; private set; }

		public static void LoadRegistryValues(global::Session session)
		{
			KaijuCouncil.WildLiceArmyPreferences.ArmiesToSpawnCount = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<int>("Gameplay/Kaiju/LiceArmy/ArmiesToSpawnCount", 1);
			KaijuCouncil.WildLiceArmyPreferences.SpawningCooldown = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<int>("Gameplay/Kaiju/LiceArmy/SpawningCooldown", 5);
			KaijuCouncil.WildLiceArmyPreferences.UnitsPerArmy = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<int>("Gameplay/Kaiju/LiceArmy/UnitsPerArmy", 4);
			KaijuCouncil.WildLiceArmyPreferences.ArmyTag = new StaticString(Amplitude.Unity.Runtime.Runtime.Registry.GetValue<string>("Gameplay/Kaiju/LiceArmy/ArmyTag", "WildLiceArmy"));
			KaijuCouncil.WildLiceArmyPreferences.UnitTag = new StaticString(Amplitude.Unity.Runtime.Runtime.Registry.GetValue<string>("Gameplay/Kaiju/LiceArmy/UnitTag", "WildLiceUnit"));
			string text = string.Empty;
			OptionDefinition optionDefinition = null;
			IDatabase<WorldGeneratorOptionDefinition> database = Databases.GetDatabase<WorldGeneratorOptionDefinition>(false);
			foreach (OptionDefinition optionDefinition2 in database)
			{
				if (optionDefinition2.Name.Equals("WorldSize"))
				{
					optionDefinition = optionDefinition2;
					break;
				}
			}
			if (optionDefinition != null)
			{
				text = session.GetLobbyData<string>(optionDefinition.Name, string.Empty);
			}
			if (!string.IsNullOrEmpty(text))
			{
				KaijuCouncil.WildLiceArmyPreferences.MaxArmiesAlive = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<int>("Gameplay/Kaiju/LiceArmy/MaxArmiesAlive/World" + text, 3);
			}
			else
			{
				KaijuCouncil.WildLiceArmyPreferences.MaxArmiesAlive = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<int>("Gameplay/Kaiju/LiceArmy/MaxArmiesAlive/Default" + text, 3);
			}
		}

		public const string defaultArmyTag = "WildLiceArmy";

		public const string defaultUnitTag = "WildLiceUnit";

		private const int defaultArmiesToSpawnCount = 1;

		private const int defaultSpawningCooldown = 5;

		private const int defaultUnitsPerArmy = 4;

		private const int defaultMaxArmiesAlive = 3;
	}
}
