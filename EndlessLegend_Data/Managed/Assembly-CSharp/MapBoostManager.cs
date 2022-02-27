using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Simulation;
using Amplitude.Utilities.Maps;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class MapBoostManager : GameAncillary, Amplitude.Xml.Serialization.IXmlSerializable, IService, IEnumerable, IMapBoostRepositoryService, IMapBoostService, IRepositoryService<MapBoost>, IEnumerable<MapBoost>, IEnumerable<KeyValuePair<ulong, MapBoost>>
{
	public event EventHandler<MapBoostRepositoryChangeEventArgs> MapBoostRepositoryChange;

	IEnumerable<MapBoost> IMapBoostRepositoryService.AsEnumerable(int empireIndex)
	{
		foreach (MapBoost mapBoost in this.mapBoosts.Values)
		{
			yield return mapBoost;
		}
		yield break;
	}

	IEnumerator<MapBoost> IEnumerable<MapBoost>.GetEnumerator()
	{
		foreach (MapBoost mapBoost in this.mapBoosts.Values)
		{
			yield return mapBoost;
		}
		yield break;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.mapBoosts.GetEnumerator();
	}

	public int Count
	{
		get
		{
			return this.mapBoosts.Count;
		}
	}

	public IGameEntity this[GameEntityGUID guid]
	{
		get
		{
			return this.mapBoosts[guid];
		}
	}

	public bool Contains(GameEntityGUID guid)
	{
		return this.mapBoosts.ContainsKey(guid);
	}

	public IEnumerator<KeyValuePair<ulong, MapBoost>> GetEnumerator()
	{
		return this.mapBoosts.GetEnumerator();
	}

	public void Register(MapBoost mapBoost)
	{
		if (mapBoost == null)
		{
			throw new ArgumentNullException("Map boost is null and trying to be registered");
		}
		this.mapBoosts.Add(mapBoost.GUID, mapBoost);
		int regionIndex = (int)this.worldPositionService.GetRegionIndex(mapBoost.WorldPosition);
		List<MapBoost> list = new List<MapBoost>();
		if (!this.mapBoostsPerRegion.ContainsKey(regionIndex))
		{
			this.mapBoostsPerRegion.Add(regionIndex, list);
		}
		else
		{
			list = this.mapBoostsPerRegion[regionIndex];
			list.Add(mapBoost);
			this.mapBoostsPerRegion[regionIndex] = list;
		}
		this.OnMapBoostRepositoryChange(MapBoostRepositoryChangeAction.Add, mapBoost.GUID);
		this.gameEntityRepositoryService.Register(mapBoost);
	}

	public bool TryGetValue(GameEntityGUID guid, out MapBoost gameEntity)
	{
		return this.mapBoosts.TryGetValue(guid, out gameEntity);
	}

	public void Unregister(MapBoost mapBoost)
	{
		if (mapBoost == null)
		{
			throw new ArgumentNullException("pillar");
		}
		this.Unregister(mapBoost.GUID);
	}

	public void Unregister(IGameEntity gameEntity)
	{
		if (gameEntity == null)
		{
			throw new ArgumentNullException("gameEntity");
		}
		this.Unregister(gameEntity.GUID);
	}

	public void Unregister(GameEntityGUID guid)
	{
		if (this.mapBoosts.Remove(guid))
		{
			this.OnMapBoostRepositoryChange(MapBoostRepositoryChangeAction.Remove, guid);
		}
		this.gameEntityRepositoryService.Unregister(guid);
	}

	private void OnMapBoostRepositoryChange(MapBoostRepositoryChangeAction action, ulong gameEntityGuid)
	{
		if (this.MapBoostRepositoryChange != null)
		{
			this.MapBoostRepositoryChange(this, new MapBoostRepositoryChangeEventArgs(action, gameEntityGuid));
		}
	}

	private XmlSerializer XmlSerializer { get; set; }

	public virtual void ReadXml(XmlReader reader)
	{
		reader.ReadStartElement();
		this.TurnWhenLastBegun = reader.ReadElementString<int>("TurnWhenLastBegun");
		this.OverallAttractiveness = reader.ReadElementString<int>("OverallAttractiveness");
		if (reader.IsStartElement("MapBoosts"))
		{
			int attribute = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("MapBoosts");
			for (int i = 0; i < attribute; i++)
			{
				GameEntityGUID guid = reader.GetAttribute<ulong>("GUID");
				WorldPosition position;
				position.Row = reader.GetAttribute<short>("Row");
				position.Column = reader.GetAttribute<short>("Column");
				string attribute2 = reader.GetAttribute("MapBoostDefinitionName");
				int attribute3 = reader.GetAttribute<int>("RemainingTurns");
				bool attribute4 = reader.GetAttribute<bool>("IsBuffAvailable");
				int attribute5 = reader.GetAttribute<int>("AffectedEmpireIndex");
				MapBoostDefinition mapBoostDefinition;
				if (this.mapBoostDefinitionDatabase.TryGetValue(attribute2, out mapBoostDefinition))
				{
					MapBoost mapBoost = this.AddMapBoost(guid, mapBoostDefinition, position);
					mapBoost.RemainingTurns = attribute3;
					mapBoost.IsBuffAvailable = attribute4;
					mapBoost.AffectedEmpireIndex = attribute5;
					reader.ReadStartElement("MapBoost");
					reader.IsStartElement("AffectedUnits");
					int attribute6 = reader.GetAttribute<int>("Count");
					reader.ReadStartElement();
					if (attribute6 != 0)
					{
						mapBoost.AffectedUnitsData = new UnitData[attribute6];
						for (int j = 0; j < attribute6; j++)
						{
							GameEntityGUID guid2 = reader.ReadElementString<ulong>("GUID");
							int empireIndex = reader.ReadElementString<int>("EmpireIndex");
							mapBoost.AffectedUnitsData[j].guid = guid2;
							mapBoost.AffectedUnitsData[j].empireIndex = empireIndex;
						}
					}
					reader.ReadEndElement();
					reader.ReadEndElement("MapBoost");
				}
			}
		}
		reader.ReadEndElement("MapBoosts");
	}

	public virtual void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString("AssemblyQualifiedName", base.GetType().AssemblyQualifiedName);
		writer.WriteElementString<int>("TurnWhenLastBegun", this.TurnWhenLastBegun);
		writer.WriteElementString<int>("OverallAttractiveness", this.OverallAttractiveness);
		writer.WriteStartElement("MapBoosts");
		writer.WriteAttributeString<int>("Count", this.mapBoosts.Values.Count);
		foreach (MapBoost mapBoost in this.mapBoosts.Values)
		{
			writer.WriteStartElement("MapBoost");
			writer.WriteAttributeString<ulong>("GUID", mapBoost.GUID);
			writer.WriteAttributeString<short>("Row", mapBoost.WorldPosition.Row);
			writer.WriteAttributeString<short>("Column", mapBoost.WorldPosition.Column);
			writer.WriteAttributeString<int>("RemainingTurns", mapBoost.RemainingTurns);
			writer.WriteAttributeString<bool>("IsBuffAvailable", mapBoost.IsBuffAvailable);
			writer.WriteAttributeString<string>("MapBoostDefinitionName", mapBoost.MapBoostDefinition.XmlSerializableName);
			writer.WriteAttributeString<int>("AffectedEmpireIndex", mapBoost.AffectedEmpireIndex);
			writer.WriteStartElement("AffectedUnits");
			int num = (mapBoost.AffectedUnitsData == null) ? 0 : mapBoost.AffectedUnitsData.Length;
			writer.WriteAttributeString<int>("Count", num);
			if (num != 0)
			{
				for (int i = 0; i < num; i++)
				{
					writer.WriteElementString<ulong>("GUID", mapBoost.AffectedUnitsData[i].guid);
					writer.WriteElementString<int>("EmpireIndex", mapBoost.AffectedUnitsData[i].empireIndex);
				}
			}
			writer.WriteEndElement();
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
	}

	private int TurnWhenLastBegun { get; set; }

	private int OverallAttractiveness { get; set; }

	private global::PlayerController PlayerController
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

	public Dictionary<ulong, MapBoost> MapBoosts
	{
		get
		{
			return this.mapBoosts;
		}
	}

	public override IEnumerator BindServices(IServiceContainer serviceContainer)
	{
		yield return base.BindServices(serviceContainer);
		yield return base.BindService<IGameEntityRepositoryService>(serviceContainer, delegate(IGameEntityRepositoryService service)
		{
			this.gameEntityRepositoryService = service;
		});
		yield return base.BindService<IWorldPositionSimulationEvaluatorService>(serviceContainer, delegate(IWorldPositionSimulationEvaluatorService service)
		{
			this.worldPositionSimulationEvaluatorService = service;
		});
		yield return base.BindService<IWorldPositionningService>(serviceContainer, delegate(IWorldPositionningService service)
		{
			this.worldPositionService = service;
		});
		yield return base.BindService<IPathfindingService>(serviceContainer, delegate(IPathfindingService service)
		{
			this.pathfindingService = service;
		});
		if (this.worldPositionService == null)
		{
			Diagnostics.LogError("Wasn't able to find the world positionning service.");
		}
		this.eventService = Services.GetService<IEventService>();
		if (this.eventService == null)
		{
			Diagnostics.LogError("Wasn't able to find the event service.");
		}
		this.attractivenessDatabase = Databases.GetDatabase<MapBoostAttractivenessRule>(false);
		Diagnostics.Assert(this.attractivenessDatabase != null);
		this.mapBoostDefinitionDatabase = Databases.GetDatabase<MapBoostDefinition>(false);
		Diagnostics.Assert(this.mapBoostDefinitionDatabase != null);
		this.simulationDescriptorsDatabase = Databases.GetDatabase<SimulationDescriptor>(true);
		Diagnostics.Assert(this.simulationDescriptorsDatabase != null);
		serviceContainer.AddService<IMapBoostService>(this);
		serviceContainer.AddService<IMapBoostRepositoryService>(this);
		yield break;
	}

	public override IEnumerator Ignite(IServiceContainer serviceContainer)
	{
		yield return base.Ignite(serviceContainer);
		yield break;
	}

	public override IEnumerator LoadGame(global::Game game)
	{
		yield return base.LoadGame(game);
		foreach (MapBoost boost in this.mapBoosts.Values)
		{
			if (boost.AffectedUnitsData != null && boost.AffectedUnitsData.Length > 0 && boost.AffectedUnits == null)
			{
				this.FindUnitsForBoost(boost);
			}
		}
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		this.game = (gameService.Game as global::Game);
		this.orbService = gameService.Game.Services.GetService<IOrbService>();
		Diagnostics.Assert(this.orbService != null);
		this.mapBoostSpawnPoints = new List<WorldPosition>();
		this.ComputeAttractiveness();
		yield break;
	}

	protected override void Releasing()
	{
		base.Releasing();
		this.mapBoosts.Clear();
		this.mapBoostsPerRegion.Clear();
		this.game = null;
		this.worldPositionService = null;
		this.eventService = null;
		this.worldPositionSimulationEvaluatorService = null;
		this.gameEntityRepositoryService = null;
		this.pathfindingService = null;
		this.orbService = null;
	}

	public void GameClient_OnBeginTurn()
	{
		if (base.Game.Turn <= this.TurnWhenLastBegun)
		{
			return;
		}
		this.TurnWhenLastBegun = base.Game.Turn;
		foreach (MapBoost mapBoost in this.mapBoosts.Values)
		{
			mapBoost.OnBeginTurn();
		}
	}

	public void SpawnMapBoosts()
	{
		this.PreselectMapBoostSpawns(DateTime.Now.Millisecond);
		this.DistributeMapBoosts();
	}

	public void PreselectMapBoostSpawns(int seed)
	{
		this.mapBoostSpawnPoints.Clear();
		float value = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<float>(this.worldContinentalVortexSpawnTilePercentage, 0.2f);
		float value2 = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<float>(this.worldOceanicVortexSpawnTilePercentage, 0.05f);
		UnityEngine.Random.seed = seed;
		for (int i = 0; i < base.Game.World.Regions.Length; i++)
		{
			int num;
			if (!base.Game.World.Regions[i].IsOcean)
			{
				num = (int)((float)base.Game.World.Regions[i].WorldPositions.Length * value);
			}
			else
			{
				num = (int)((float)base.Game.World.Regions[i].WorldPositions.Length * value2);
			}
			int regionAttractiveness = this.GetRegionAttractiveness(base.Game.World.Regions[i]);
			for (int j = 0; j < num; j++)
			{
				int num2 = UnityEngine.Random.Range(0, regionAttractiveness);
				for (int k = 0; k < base.Game.World.Regions[i].WorldPositions.Length; k++)
				{
					WorldPosition worldPosition = base.Game.World.Regions[i].WorldPositions[k];
					if (this.orbService.GetOrbValueAtPosition(worldPosition) <= 0 && !this.IsPositionOnCityTile(worldPosition, base.Game.World.Regions[i]))
					{
						num2 -= (int)this.attractivenessMap.GetValue(worldPosition);
						if (num2 < 0 && !this.mapBoostSpawnPoints.Contains(worldPosition))
						{
							this.mapBoostSpawnPoints.Add(worldPosition);
							this.OverallAttractiveness += (int)this.attractivenessMap.GetValue(worldPosition);
							break;
						}
					}
				}
			}
		}
		if (this.OverallAttractiveness < 1)
		{
			this.OverallAttractiveness = 1;
		}
	}

	public void DistributeMapBoosts()
	{
		List<WorldPosition> list = new List<WorldPosition>();
		List<string> list2 = new List<string>();
		List<int> list3 = new List<int>();
		float value = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<float>(this.mapBoostPerTile, 0.3f);
		int num = (int)((float)(base.Game.World.WorldParameters.Rows * base.Game.World.WorldParameters.Columns) * value);
		int num2 = 0;
		for (int i = 0; i < base.Game.World.Regions.Length; i++)
		{
			List<WorldPosition> regionSpawns = this.GetRegionSpawns(base.Game.World.Regions[i]);
			int regionAttractiveness = this.GetRegionAttractiveness(regionSpawns);
			if (this.OverallAttractiveness > 0)
			{
				num2 = num * regionAttractiveness / this.OverallAttractiveness;
			}
			else
			{
				Diagnostics.LogWarning("overall attractiveness is equal to zero 0, it should not be.");
			}
			for (int j = 0; j < num2; j++)
			{
				int num3 = UnityEngine.Random.Range(0, regionAttractiveness);
				for (int k = 0; k < regionSpawns.Count; k++)
				{
					if (!list.Contains(regionSpawns[k]))
					{
						num3 -= (int)this.attractivenessMap.GetValue(regionSpawns[k]);
						if (num3 < 0)
						{
							MapBoostDefinition randomDefinitionForPosition = this.GetRandomDefinitionForPosition(regionSpawns[k]);
							if (randomDefinitionForPosition != null)
							{
								list.Add(regionSpawns[k]);
								list2.Add(randomDefinitionForPosition.XmlSerializableName);
								break;
							}
						}
					}
				}
			}
		}
		OrderSpawnMapBoosts order = new OrderSpawnMapBoosts(list.ToArray(), list2.ToArray());
		this.PlayerController.PostOrder(order);
	}

	public void AddMapBoosts(GameEntityGUID[] GUIDs, string[] mapBoostDefinitionNames, WorldPosition[] positions)
	{
		if (positions.Length != GUIDs.Length || positions.Length != mapBoostDefinitionNames.Length)
		{
			return;
		}
		IDatabase<MapBoostDefinition> database = Databases.GetDatabase<MapBoostDefinition>(false);
		for (int i = 0; i < positions.Length; i++)
		{
			MapBoostDefinition mapBoostDefinition = null;
			if (!database.TryGetValue(mapBoostDefinitionNames[i], out mapBoostDefinition))
			{
				Diagnostics.LogError("MapBoostManager::AddMapBoosts  Boost definition could not be found");
			}
			else
			{
				this.AddMapBoost(GUIDs[i], mapBoostDefinition, positions[i]);
			}
		}
		this.CheckArmiesOverMapBoostAfterDistribution();
	}

	protected MapBoost AddMapBoost(GameEntityGUID guid, MapBoostDefinition mapBoostDefinition, WorldPosition position)
	{
		MapBoost mapBoost = new MapBoost(guid, position, mapBoostDefinition, this);
		this.Register(mapBoost);
		this.worldPositionSimulationEvaluatorService.SetSomethingChangedOnRegion(this.worldPositionService.GetRegionIndex(mapBoost.WorldPosition));
		return mapBoost;
	}

	public void ApplyBoostToArmy(Army army, MapBoost boost)
	{
		if (!boost.IsBuffAvailable)
		{
			return;
		}
		boost.IsBuffAvailable = false;
		MapBoostDefinition mapBoostDefinition = boost.MapBoostDefinition;
		Unit[] array = army.Units.ToArray<Unit>();
		this.AddEffectsToUnits(mapBoostDefinition, array, army.Empire);
		boost.AffectedUnits = array;
		boost.AffectedEmpireIndex = army.Empire.Index;
		boost.CreateAffectedUnitsData();
		army.CheckMapBoostOnUnits();
		this.eventService.Notify(new EventMapBoostApplied(army.Empire, army.GUID, boost.MapBoostDefinition));
	}

	public void CheckArmiesOverMapBoostAfterDistribution()
	{
		for (int i = 0; i < base.Game.Empires.Length; i++)
		{
			if (base.Game.Empires[i] is MajorEmpire)
			{
				DepartmentOfDefense agency = base.Game.Empires[i].GetAgency<DepartmentOfDefense>();
				if (agency != null)
				{
					agency.CheckArmiesOnMapBoost();
				}
			}
		}
	}

	public MapBoost GetMapBoostAtPosition(WorldPosition position)
	{
		foreach (MapBoost mapBoost in this.mapBoosts.Values)
		{
			if (mapBoost.WorldPosition == position)
			{
				return mapBoost;
			}
		}
		return null;
	}

	public MapBoost[] GetMapBoostsAtRegion(Region region)
	{
		List<MapBoost> list = new List<MapBoost>();
		if (this.mapBoostsPerRegion.ContainsKey(region.Index))
		{
			list = this.mapBoostsPerRegion[region.Index];
		}
		return list.ToArray();
	}

	public void OrderMapBoostRemoval()
	{
		OrderRemoveMapBoosts order = new OrderRemoveMapBoosts();
		this.PlayerController.PostOrder(order);
	}

	public void RemoveMapBoosts()
	{
		this.mapBoostsPerRegion.Clear();
		this.RemoveAllBoostsEffects();
		if (this.mapBoosts.Count > 0)
		{
			this.UnregisterMapBoosts();
		}
	}

	private void UnregisterMapBoosts()
	{
		List<MapBoost> list = new List<MapBoost>();
		List<short> list2 = new List<short>();
		foreach (MapBoost mapBoost in this.mapBoosts.Values)
		{
			if (mapBoost == null)
			{
				Diagnostics.LogError("Map boost is 'null'.");
			}
			else
			{
				if (!list.Contains(mapBoost))
				{
					list.Add(mapBoost);
				}
				short regionIndex = this.worldPositionService.GetRegionIndex(mapBoost.WorldPosition);
				if (!list2.Contains(regionIndex))
				{
					list2.Add(regionIndex);
				}
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			this.Unregister(list[i].GUID);
		}
		for (int j = 0; j < list2.Count; j++)
		{
			this.worldPositionSimulationEvaluatorService.SetSomethingChangedOnRegion(list2[j]);
		}
	}

	private void AddEffectsToUnits(MapBoostDefinition definition, Unit[] units, global::Empire empire)
	{
		List<MapBoostEffect> list = new List<MapBoostEffect>();
		definition.GetValidEffects(empire, out list);
		if (list != null)
		{
			for (int i = 0; i < units.Length; i++)
			{
				for (int j = 0; j < list.Count; j++)
				{
					SimulationDescriptorReference[] simulationDescriptorReferences = list[j].SimulationDescriptorReferences;
					for (int k = 0; k < simulationDescriptorReferences.Length; k++)
					{
						SimulationDescriptor simulationDescriptor;
						if (this.simulationDescriptorsDatabase.TryGetValue(simulationDescriptorReferences[k].Name, out simulationDescriptor) && !units[i].SimulationObject.Tags.Contains(simulationDescriptor.Name))
						{
							units[i].AddMapBoost(definition.Name, simulationDescriptor);
							units[i].HasMapBoost = true;
						}
					}
					if (list[j].ExperienceReward > 0f)
					{
						units[i].GainXp(list[j].ExperienceReward, false, true);
					}
				}
			}
		}
	}

	private void RemoveAllBoostsEffects()
	{
		foreach (MapBoost mapBoost in this.mapBoosts.Values)
		{
			if (mapBoost.AffectedUnits != null && mapBoost.AffectedUnits.Length > 0)
			{
				global::Empire empire = this.game.Empires[mapBoost.AffectedEmpireIndex];
				this.RemoveEffectFromUnits(mapBoost.MapBoostDefinition, mapBoost.AffectedUnits, empire);
				mapBoost.ClearAffectedUnits();
			}
		}
	}

	public void RemoveEffectFromUnits(MapBoostDefinition definition, Unit[] units, global::Empire empire)
	{
		List<SimulationDescriptorReference> list = new List<SimulationDescriptorReference>();
		definition.GetValidDescriptors(empire, out list);
		if (list != null)
		{
			for (int i = 0; i < list.Count; i++)
			{
				SimulationDescriptor simulationDescriptor;
				if (this.simulationDescriptorsDatabase.TryGetValue(list[i].Name, out simulationDescriptor))
				{
					for (int j = 0; j < units.Length; j++)
					{
						if (units[j] != null)
						{
							units[j].RemoveMapBoost(definition.Name, simulationDescriptor);
							if (units[j].AppliedBoosts.Count <= 0)
							{
								units[j].HasMapBoost = false;
							}
						}
					}
				}
			}
		}
	}

	private void FindUnitsForBoost(MapBoost boost)
	{
		int num = boost.AffectedUnitsData.Length;
		int affectedEmpireIndex = boost.AffectedEmpireIndex;
		global::Empire empire = base.Game.Empires[affectedEmpireIndex];
		List<Unit> list = new List<Unit>();
		for (int i = 0; i < num; i++)
		{
			DepartmentOfDefense agency = empire.GetAgency<DepartmentOfDefense>();
			Unit unitByGUID = agency.GetUnitByGUID(boost.AffectedUnitsData[i].guid);
			if (unitByGUID != null)
			{
				list.Add(unitByGUID);
			}
			else
			{
				Diagnostics.LogWarning("MapBoostManager::FindUnitsForBoost. Could not find unit with GUID: " + boost.AffectedUnitsData[i].guid.ToString());
			}
		}
		boost.AffectedUnits = list.ToArray();
		boost.CreateAffectedUnitsData();
	}

	private MapBoostDefinition GetRandomDefinitionForPosition(WorldPosition position)
	{
		List<MapBoostDefinition> list = new List<MapBoostDefinition>();
		foreach (MapBoostDefinition mapBoostDefinition in this.mapBoostDefinitionDatabase)
		{
			if (this.GetTerrainCompatibilityAtPosition(mapBoostDefinition, position))
			{
				list.Add(mapBoostDefinition);
			}
		}
		if (list.Count > 1)
		{
			int index = UnityEngine.Random.Range(0, list.Count);
			return list[index];
		}
		if (list.Count == 1)
		{
			return list[0];
		}
		return null;
	}

	private bool GetTerrainCompatibilityAtPosition(MapBoostDefinition mapBoostDefinition, WorldPosition worldPosition)
	{
		if (mapBoostDefinition.TerrainTypeMappings != null && mapBoostDefinition.TerrainTypeMappings.Length > 0)
		{
			byte terrainType = this.worldPositionService.GetTerrainType(worldPosition);
			StaticString terrainTypeMappingName = this.worldPositionService.GetTerrainTypeMappingName(terrainType);
			bool flag = false;
			for (int i = 0; i < mapBoostDefinition.TerrainTypeMappings.Length; i++)
			{
				if (terrainTypeMappingName.Equals(mapBoostDefinition.TerrainTypeMappings[i]))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		return this.pathfindingService.IsTileStopable(worldPosition, PathfindingMovementCapacity.All, (PathfindingFlags)0);
	}

	private bool IsPositionOnCityTile(WorldPosition worldPosition, Region region)
	{
		if (region != null && region.City != null)
		{
			for (int i = 0; i < region.City.Districts.Count; i++)
			{
				if (region.City.Districts[i].Type != DistrictType.Exploitation && region.City.Districts[i].WorldPosition == worldPosition)
				{
					return true;
				}
			}
		}
		return false;
	}

	private List<WorldPosition> GetRegionSpawns(Region region)
	{
		List<WorldPosition> list = new List<WorldPosition>();
		for (int i = 0; i < this.mapBoostSpawnPoints.Count; i++)
		{
			if (region.WorldPositions.Contains(this.mapBoostSpawnPoints[i]))
			{
				list.Add(this.mapBoostSpawnPoints[i]);
			}
		}
		return list;
	}

	private int GetRegionAttractiveness(Region region)
	{
		int num = 0;
		for (int i = 0; i < region.WorldPositions.Length; i++)
		{
			num += (int)this.attractivenessMap.GetValue(region.WorldPositions[i]);
		}
		return num;
	}

	private int GetRegionAttractiveness(List<WorldPosition> regionSpawns)
	{
		int num = 0;
		for (int i = 0; i < regionSpawns.Count; i++)
		{
			num += (int)this.attractivenessMap.GetValue(regionSpawns[i]);
		}
		return num;
	}

	private void ComputeAttractiveness()
	{
		GridMap<byte> map = base.Game.World.Atlas.GetMap(WorldAtlas.Maps.Anomalies) as GridMap<byte>;
		Map<AnomalyTypeDefinition> map2 = base.Game.World.Atlas.GetMap(WorldAtlas.Tables.Anomalies) as Map<AnomalyTypeDefinition>;
		GridMap<bool> gridMap = base.Game.World.Atlas.GetMap(WorldAtlas.Maps.Ridges) as GridMap<bool>;
		GridMap<PointOfInterest> map3 = base.Game.World.Atlas.GetMap(WorldAtlas.Maps.PointOfInterest) as GridMap<PointOfInterest>;
		byte[,] array = new byte[(int)base.Game.World.WorldParameters.Rows, (int)base.Game.World.WorldParameters.Columns];
		for (int i = 0; i < (int)base.Game.World.WorldParameters.Rows; i++)
		{
			int j = 0;
			while (j < (int)base.Game.World.WorldParameters.Columns)
			{
				WorldPosition worldPosition = new WorldPosition(i, j);
				byte terrainType = this.worldPositionService.GetTerrainType(worldPosition);
				StaticString terrainTypeMappingName = this.worldPositionService.GetTerrainTypeMappingName(terrainType);
				MapBoostAttractivenessRule mapBoostAttractivenessRule;
				if (!this.attractivenessDatabase.TryGetValue(terrainTypeMappingName, out mapBoostAttractivenessRule))
				{
					goto IL_13E;
				}
				array[i, j] = (byte)mapBoostAttractivenessRule.Value;
				if (mapBoostAttractivenessRule.Value != 0)
				{
					goto IL_13E;
				}
				IL_21D:
				j++;
				continue;
				IL_13E:
				StaticString key = string.Empty;
				if (map2.Data.TryGetValue((int)map.GetValue(worldPosition), ref key) && this.attractivenessDatabase.TryGetValue(key, out mapBoostAttractivenessRule))
				{
					ref byte ptr = ref array[i, j];
					ptr *= (byte)mapBoostAttractivenessRule.Value;
					if (mapBoostAttractivenessRule.Value == 0)
					{
						goto IL_21D;
					}
				}
				if (gridMap != null && gridMap.GetValue(worldPosition))
				{
					array[i, j] = 0;
					goto IL_21D;
				}
				PointOfInterest value = map3.GetValue(worldPosition);
				if (value == null || !this.attractivenessDatabase.TryGetValue(value.PointOfInterestDefinition.PointOfInterestTemplateName, out mapBoostAttractivenessRule))
				{
					goto IL_21D;
				}
				ref byte ptr2 = ref array[i, j];
				ptr2 *= (byte)mapBoostAttractivenessRule.Value;
				if (mapBoostAttractivenessRule.Value == 0)
				{
					goto IL_21D;
				}
				goto IL_21D;
			}
		}
		this.attractivenessMap = (base.Game.World.Atlas.GetMap(WorldAtlas.Maps.VortexAttractiveness) as GridMap<byte>);
		if (this.attractivenessMap == null)
		{
			this.attractivenessMap = new GridMap<byte>(WorldAtlas.Maps.VortexAttractiveness, (int)base.Game.World.WorldParameters.Columns, (int)base.Game.World.WorldParameters.Rows, array);
			base.Game.World.Atlas.RegisterMapInstance<GridMap<byte>>(this.attractivenessMap);
		}
	}

	private IEventService eventService;

	private IWorldPositionningService worldPositionService;

	private IWorldPositionSimulationEvaluatorService worldPositionSimulationEvaluatorService;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private IPathfindingService pathfindingService;

	private IOrbService orbService;

	private global::Game game;

	private IDatabase<SimulationDescriptor> simulationDescriptorsDatabase;

	private IDatabase<MapBoostDefinition> mapBoostDefinitionDatabase;

	private IDatabase<MapBoostAttractivenessRule> attractivenessDatabase;

	private GridMap<byte> attractivenessMap;

	private GridMap<byte> vortexMap;

	private Dictionary<ulong, MapBoost> mapBoosts = new Dictionary<ulong, MapBoost>();

	private readonly Dictionary<int, List<MapBoost>> mapBoostsPerRegion = new Dictionary<int, List<MapBoost>>();

	private List<WorldPosition> mapBoostSpawnPoints;

	private StaticString worldContinentalVortexSpawnTilePercentage = "Gameplay/Ancillaries/MapBoost/WorldContinentalMapBoostSpawnTilePercentage";

	private StaticString worldOceanicVortexSpawnTilePercentage = "Gameplay/Ancillaries/MapBoost/WorldOceanicMapBoostSpawnTilePercentage";

	private StaticString mapBoostPerTile = "Gameplay/Ancillaries/Orb/MapBoostPerTile";
}
