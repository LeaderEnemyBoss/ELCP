using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Amplitude;
using Amplitude.Extensions;
using Amplitude.IO;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation;
using Amplitude.Utilities.Maps;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class World : IXmlSerializable
{
	public World()
	{
		this.TemporaryTerraformations = new List<World.TemporaryTerraformation>();
	}

	public virtual void ReadXml(Amplitude.Xml.XmlReader reader)
	{
		reader.ReadStartElement("World");
		int attribute = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("Regions");
		for (int i = 0; i < attribute; i++)
		{
			IXmlSerializable xmlSerializable = this.Regions[i];
			reader.ReadElementSerializable<IXmlSerializable>(ref xmlSerializable);
		}
		reader.ReadEndElement("Regions");
	}

	public virtual void WriteXml(Amplitude.Xml.XmlWriter writer)
	{
		writer.WriteStartElement("Regions");
		writer.WriteAttributeString<int>("Count", this.Regions.Length);
		for (int i = 0; i < this.Regions.Length; i++)
		{
			IXmlSerializable xmlSerializable = this.Regions[i];
			writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
		}
		writer.WriteEndElement();
	}

	public bool TryGetTerraformMapping(WorldPosition worldPosition, out TerrainTypeMapping mappedTerrain)
	{
		if (!worldPosition.IsValid)
		{
			mappedTerrain = null;
			return false;
		}
		byte value = this.TerrainMap.GetValue(worldPosition);
		StaticString empty = StaticString.Empty;
		if (!this.TerrainTypeNameMap.Data.TryGetValue((int)value, ref empty))
		{
			mappedTerrain = null;
			return false;
		}
		IDatabase<TerrainTypeMapping> database = Databases.GetDatabase<TerrainTypeMapping>(false);
		Diagnostics.Assert(database != null, "Terrain Type Mapping database can't be 'null'.");
		TerrainTypeMapping sourceTerrain = null;
		if (!database.TryGetValue(empty, out sourceTerrain))
		{
			mappedTerrain = null;
			return false;
		}
		return this.TryGetTerraformMapping(sourceTerrain, worldPosition, out mappedTerrain);
	}

	public bool TryGetTerraformMapping(TerrainTypeMapping sourceTerrain, WorldPosition worldPosition, out TerrainTypeMapping mappedTerrain)
	{
		if (sourceTerrain == null)
		{
			mappedTerrain = null;
			return false;
		}
		SimulationLayer[] layers = sourceTerrain.Layers;
		if (layers == null || layers.Length == 0)
		{
			mappedTerrain = null;
			return false;
		}
		IDatabase<TerrainTypeMapping> database = Databases.GetDatabase<TerrainTypeMapping>(false);
		Diagnostics.Assert(database != null, "Terrain Type Mapping database can't be 'null'.");
		for (int i = 0; i < sourceTerrain.Layers.Length; i++)
		{
			if (sourceTerrain.Layers[i] != null)
			{
				string text = sourceTerrain.Layers[i].Name;
				if (!string.IsNullOrEmpty(text) && string.Equals(text, World.TerraformationLayerName))
				{
					SimulationLayer simulationLayer = sourceTerrain.Layers[i];
					if (simulationLayer.Samples != null && simulationLayer.Samples.Length != 0)
					{
						int num = 0;
						for (int j = 0; j < simulationLayer.Samples.Length; j++)
						{
							num += simulationLayer.Samples[j].Weight;
						}
						int seed = World.Seed + (int)(worldPosition.Row * this.WorldParameters.Rows) + (int)worldPosition.Column;
						System.Random random = new System.Random(seed);
						int num2 = random.Next(0, num);
						for (int k = 0; k < simulationLayer.Samples.Length; k++)
						{
							SimulationLayer.Sample sample = simulationLayer.Samples[k];
							if (sample != null && !string.IsNullOrEmpty(sample.Value) && num2 < sample.Weight && database.TryGetValue(sample.Value, out mappedTerrain))
							{
								return true;
							}
							num2 -= simulationLayer.Samples[k].Weight;
						}
					}
				}
			}
		}
		mappedTerrain = null;
		return false;
	}

	public WorldPosition[] PerformTerraformation(WorldPosition[] positions, bool ReverseTerraform = false)
	{
		if (positions == null)
		{
			Diagnostics.LogError("World.PerformTerraformation has received some invalid parameter(s).");
			return new WorldPosition[0];
		}
		List<KeyValuePair<WorldPosition, TerrainTypeMapping>> list = new List<KeyValuePair<WorldPosition, TerrainTypeMapping>>();
		List<int> list2 = new List<int>();
		for (int i = 0; i < positions.Length; i++)
		{
			WorldPosition worldPosition = positions[i];
			if (worldPosition.IsValid)
			{
				TerrainTypeMapping value = null;
				if (!ReverseTerraform)
				{
					if (this.TemporaryTerraformations.Exists((World.TemporaryTerraformation tt) => tt.worldPosition == worldPosition))
					{
						this.TemporaryTerraformations.RemoveAll((World.TemporaryTerraformation tt) => tt.worldPosition == worldPosition);
						list2.Add(i);
					}
					if (this.TryGetTerraformMapping(worldPosition, out value))
					{
						list.Add(new KeyValuePair<WorldPosition, TerrainTypeMapping>(worldPosition, value));
					}
				}
				else if (ReverseTerraform && this.TryGetOriginalTerrainTypMapping(worldPosition, out value))
				{
					list.Add(new KeyValuePair<WorldPosition, TerrainTypeMapping>(worldPosition, value));
				}
			}
		}
		if (list.Count > 0)
		{
			WorldPosition[] array = this.PerformTerraformation(list.ToArray(), ReverseTerraform);
			if (list2.Count > 0)
			{
				List<WorldPosition> list3 = array.ToList<WorldPosition>();
				foreach (int num in list2)
				{
					list3.AddOnce(positions[num]);
				}
				array = list3.ToArray();
			}
			return array;
		}
		return new WorldPosition[0];
	}

	public WorldPosition[] PerformTerraformation(KeyValuePair<WorldPosition, TerrainTypeMapping>[] terraformPairs, bool ReverseTerraform = false)
	{
		if (terraformPairs == null)
		{
			Diagnostics.LogError("World.PerformTerraformation has received some invalid parameter(s).");
			return new WorldPosition[0];
		}
		List<WorldPosition> list = new List<WorldPosition>();
		for (int i = 0; i < terraformPairs.Length; i++)
		{
			if (terraformPairs[i].Key.IsValid && terraformPairs[i].Value != null && this.PerformTerraformation(terraformPairs[i], ReverseTerraform))
			{
				list.Add(terraformPairs[i].Key);
			}
		}
		return list.ToArray();
	}

	public bool PerformTerraformation(KeyValuePair<WorldPosition, TerrainTypeMapping> terraformPair, bool ReverseTerraform = false)
	{
		if (!terraformPair.Key.IsValid || terraformPair.Value == null)
		{
			Diagnostics.LogError("World.PerformTerraformation has received some invalid parameter(s).");
			return false;
		}
		WorldPosition key = terraformPair.Key;
		TerrainTypeMapping value = terraformPair.Value;
		if (this.TerrainTypeValuesByName.ContainsKey(value.Name))
		{
			byte value2 = this.TerrainMap.GetValue(key);
			byte b = (byte)this.TerrainTypeValuesByName[value.Name];
			if (value2 != b)
			{
				this.TerrainMap.SetValue(key, b);
				GridMap<byte> gridMap = this.Atlas.GetMap(WorldAtlas.Maps.TerraformState) as GridMap<byte>;
				if (!ReverseTerraform)
				{
					gridMap.SetValue((int)key.Row, (int)key.Column, 2);
				}
				else
				{
					gridMap.SetValue((int)key.Row, (int)key.Column, 0);
				}
				GridMap<byte> gridMap2 = this.TerrainMap;
				int version = gridMap2.Version;
				gridMap2.Version = version + 1;
				return true;
			}
		}
		return false;
	}

	public void UpdateTerraformStateMap(bool checkAgainstTerrainMapVersion = true)
	{
		GridMap<byte> gridMap = this.Atlas.GetMap(WorldAtlas.Maps.TerraformState) as GridMap<byte>;
		if (checkAgainstTerrainMapVersion && this.TerrainMap.Version == gridMap.Version)
		{
			return;
		}
		for (int i = 0; i < gridMap.Data.Length; i++)
		{
			if (gridMap.Data[i] == 1)
			{
				gridMap.Data[i] = 0;
			}
		}
		for (int j = 0; j < gridMap.Height; j++)
		{
			for (int k = 0; k < gridMap.Width; k++)
			{
				WorldPosition worldPosition = new WorldPosition(j, k);
				byte value = gridMap.GetValue((int)worldPosition.Row, (int)worldPosition.Column);
				if (value == 2)
				{
					WorldRect worldRect = new WorldRect(worldPosition, WorldOrientation.East, 1, 1, 1, 1, this.WorldParameters);
					WorldPosition[] worldPositions = worldRect.GetWorldPositions(this.WorldParameters);
					for (int l = 0; l < worldPositions.Length; l++)
					{
						WorldPosition validPosition = WorldPosition.GetValidPosition(worldPositions[l], this.WorldParameters);
						if (validPosition.IsValid && validPosition != worldPosition && gridMap.GetValue((int)validPosition.Row, (int)validPosition.Column) == 0)
						{
							gridMap.SetValue((int)validPosition.Row, (int)validPosition.Column, 1);
						}
					}
				}
			}
		}
		gridMap.Version = this.TerrainMap.Version;
	}

	public void ComputeFreezingTiles(GridMap<byte> waterStateMap, int range)
	{
		for (int i = 0; i < (int)this.WorldParameters.Rows; i++)
		{
			for (int j = 0; j < (int)this.WorldParameters.Columns; j++)
			{
				WorldPosition worldPosition = new WorldPosition(i, j);
				if (waterStateMap.GetValue((int)worldPosition.Row, (int)worldPosition.Column) == 100)
				{
					this.FreezeUntilRange(worldPosition, range, waterStateMap);
				}
			}
		}
	}

	public void ComputeUnfreezeTiles(GridMap<byte> waterStateMap)
	{
		IGameService service = Services.GetService<IGameService>();
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		for (int i = 0; i < (int)this.WorldParameters.Rows; i++)
		{
			for (int j = 0; j < (int)this.WorldParameters.Columns; j++)
			{
				if (service2.IsWaterTile(new WorldPosition(i, j)))
				{
					waterStateMap.SetValue(i, j, 0);
				}
				else
				{
					waterStateMap.SetValue(i, j, 100);
				}
			}
		}
	}

	private void InitializeWaterMap()
	{
		List<WorldPosition> list = new List<WorldPosition>();
		GridMap<sbyte> terrainHeightMap = this.Atlas.GetMap(WorldAtlas.Maps.Height) as GridMap<sbyte>;
		GridMap<byte> terrainTypeMap = this.Atlas.GetMap(WorldAtlas.Maps.Terrain) as GridMap<byte>;
		Map<TerrainTypeName> terrainTypeNames = this.Atlas.GetMap(WorldAtlas.Tables.Terrains) as Map<TerrainTypeName>;
		bool[,] array = new bool[(int)this.WorldParameters.Rows, (int)this.WorldParameters.Columns];
		sbyte[,] array2 = new sbyte[(int)this.WorldParameters.Rows, (int)this.WorldParameters.Columns];
		byte[,] array3 = new byte[(int)this.WorldParameters.Rows, (int)this.WorldParameters.Columns];
		byte[,] array4 = new byte[(int)this.WorldParameters.Rows, (int)this.WorldParameters.Columns];
		for (int i = 0; i < (int)this.WorldParameters.Rows; i++)
		{
			for (int j = 0; j < (int)this.WorldParameters.Columns; j++)
			{
				WorldPosition worldPosition = new WorldPosition(i, j);
				World.WaterType waterTileType = this.GetWaterTileType(worldPosition, terrainTypeMap, terrainTypeNames);
				array4[(int)worldPosition.Row, (int)worldPosition.Column] = (byte)waterTileType;
				switch (waterTileType)
				{
				case World.WaterType.Water:
					array2[i, j] = -1;
					array3[(int)worldPosition.Row, (int)worldPosition.Column] = 0;
					break;
				case World.WaterType.Ocean:
				case World.WaterType.CoastalWaters:
					array2[i, j] = 0;
					array3[(int)worldPosition.Row, (int)worldPosition.Column] = 0;
					break;
				case World.WaterType.InlandWater:
					if (!array[(int)worldPosition.Row, (int)worldPosition.Column])
					{
						list.Clear();
						sbyte b = (sbyte)((int)this.FindHighestHeightInConnectedTile(worldPosition, array, terrainTypeMap, terrainTypeNames, terrainHeightMap, ref list) + 1);
						for (int k = 0; k < list.Count; k++)
						{
							WorldPosition worldPosition2 = list[k];
							array2[(int)worldPosition2.Row, (int)worldPosition2.Column] = b;
						}
					}
					array3[(int)worldPosition.Row, (int)worldPosition.Column] = 0;
					break;
				default:
					array2[i, j] = -1;
					array3[(int)worldPosition.Row, (int)worldPosition.Column] = 100;
					break;
				}
			}
		}
		if (!(this.Atlas.GetMap(WorldAtlas.Maps.WaterHeight) is GridMap<sbyte>))
		{
			GridMap<sbyte> mapInstance = new GridMap<sbyte>(WorldAtlas.Maps.WaterHeight, (int)this.WorldParameters.Columns, (int)this.WorldParameters.Rows, array2);
			this.Atlas.RegisterMapInstance<GridMap<sbyte>>(mapInstance);
		}
		if (!(this.Atlas.GetMap(WorldAtlas.Maps.WaterState) is GridMap<byte>))
		{
			GridMap<byte> mapInstance2 = new GridMap<byte>(WorldAtlas.Maps.WaterState, (int)this.WorldParameters.Columns, (int)this.WorldParameters.Rows, array3);
			this.Atlas.RegisterMapInstance<GridMap<byte>>(mapInstance2);
		}
		if (!(this.Atlas.GetMap(WorldAtlas.Maps.WaterType) is GridMap<byte>))
		{
			GridMap<byte> mapInstance3 = new GridMap<byte>(WorldAtlas.Maps.WaterType, (int)this.WorldParameters.Columns, (int)this.WorldParameters.Rows, array4);
			this.Atlas.RegisterMapInstance<GridMap<byte>>(mapInstance3);
		}
	}

	private void FreezeUntilRange(WorldPosition worldPosition, int iterationLeft, GridMap<byte> waterStateMap)
	{
		iterationLeft--;
		WorldPosition[] directNeighbourTiles = WorldPosition.GetDirectNeighbourTiles(worldPosition);
		for (int i = 0; i < directNeighbourTiles.Length; i++)
		{
			if (directNeighbourTiles[i].Row < 0)
			{
				WorldPosition[] array = directNeighbourTiles;
				int num = i;
				array[num].Row = array[num].Row + this.WorldParameters.Rows;
			}
			if (directNeighbourTiles[i].Row >= this.WorldParameters.Rows)
			{
				WorldPosition[] array2 = directNeighbourTiles;
				int num2 = i;
				array2[num2].Row = array2[num2].Row - this.WorldParameters.Rows;
			}
			if (directNeighbourTiles[i].Column < 0)
			{
				WorldPosition[] array3 = directNeighbourTiles;
				int num3 = i;
				array3[num3].Column = array3[num3].Column + this.WorldParameters.Columns;
			}
			if (directNeighbourTiles[i].Column >= this.WorldParameters.Columns)
			{
				WorldPosition[] array4 = directNeighbourTiles;
				int num4 = i;
				array4[num4].Column = array4[num4].Column - this.WorldParameters.Columns;
			}
			int value = (int)waterStateMap.GetValue((int)directNeighbourTiles[i].Row, (int)directNeighbourTiles[i].Column);
			if (value != 100)
			{
				if (iterationLeft >= 0)
				{
					if (waterStateMap.GetValue(directNeighbourTiles[i]) != 75)
					{
						waterStateMap.SetValue((int)directNeighbourTiles[i].Row, (int)directNeighbourTiles[i].Column, 75);
					}
					this.FreezeUntilRange(directNeighbourTiles[i], iterationLeft, waterStateMap);
				}
				else if (waterStateMap.GetValue(directNeighbourTiles[i]) != 75)
				{
					waterStateMap.SetValue((int)directNeighbourTiles[i].Row, (int)directNeighbourTiles[i].Column, 50);
				}
			}
		}
	}

	private sbyte FindHighestHeightInConnectedTile(WorldPosition worldPosition, bool[,] isHeightEvaluated, GridMap<byte> terrainTypeMap, Map<TerrainTypeName> terrainTypeNames, GridMap<sbyte> terrainHeightMap, ref List<WorldPosition> result)
	{
		sbyte b = terrainHeightMap.GetValue(worldPosition);
		isHeightEvaluated[(int)worldPosition.Row, (int)worldPosition.Column] = true;
		result.Add(worldPosition);
		for (int i = 0; i < 6; i++)
		{
			WorldPosition validPosition = WorldPosition.GetValidPosition(WorldPosition.GetNeighbourTile(worldPosition, (WorldOrientation)i, 1), this.WorldParameters);
			if (validPosition.IsValid)
			{
				if (!isHeightEvaluated[(int)validPosition.Row, (int)validPosition.Column])
				{
					if (this.GetWaterTileType(validPosition, terrainTypeMap, terrainTypeNames) == World.WaterType.InlandWater)
					{
						b = (sbyte)Mathf.Max((int)b, (int)this.FindHighestHeightInConnectedTile(validPosition, isHeightEvaluated, terrainTypeMap, terrainTypeNames, terrainHeightMap, ref result));
					}
				}
			}
		}
		return b;
	}

	private World.WaterType GetWaterTileType(WorldPosition position, GridMap<byte> terrainTypeMap, Map<TerrainTypeName> terrainTypeNames)
	{
		byte value = terrainTypeMap.GetValue(position);
		StaticString staticString = StaticString.Empty;
		StaticString empty = StaticString.Empty;
		if (terrainTypeNames.Data.TryGetValue((int)value, ref empty))
		{
			staticString = empty;
		}
		Diagnostics.Assert(!StaticString.IsNullOrEmpty(staticString));
		TerrainTypeMapping terrainTypeMapping = null;
		IDatabase<TerrainTypeMapping> database = Databases.GetDatabase<TerrainTypeMapping>(false);
		Diagnostics.Assert(database != null);
		if (!database.TryGetValue(staticString, out terrainTypeMapping))
		{
			return World.WaterType.None;
		}
		if (terrainTypeMapping.Layers == null || terrainTypeMapping.Layers.Length <= 0)
		{
			return World.WaterType.None;
		}
		for (int i = 0; i < terrainTypeMapping.Layers.Length; i++)
		{
			SimulationLayer simulationLayer = terrainTypeMapping.Layers[i];
			if (!(simulationLayer.Type != WorldPositionning.LayerTypeSimulation))
			{
				for (int j = 0; j < simulationLayer.Samples.Length; j++)
				{
					SimulationLayer.Sample sample = simulationLayer.Samples[j];
					if (sample.Value == "TerrainTypeOcean")
					{
						return World.WaterType.Ocean;
					}
					if (sample.Value == "TerrainTypeCoastalWaters")
					{
						return World.WaterType.CoastalWaters;
					}
					if (sample.Value == "TerrainTypeInlandWater")
					{
						return World.WaterType.InlandWater;
					}
					if (sample.Value == "TerrainTagWater")
					{
						return World.WaterType.Water;
					}
				}
			}
		}
		return World.WaterType.None;
	}

	~World()
	{
	}

	public static int Seed { get; set; }

	public float AverageRegionSize { get; set; }

	public WorldAtlas Atlas { get; private set; }

	public Continent[] Continents { get; private set; }

	public bool HasBeenIgnited { get; private set; }

	public bool HasBeenLoaded { get; private set; }

	public float Hypotenuse
	{
		get
		{
			double num = Math.Pow((double)this.WorldParameters.Columns, 2.0);
			double num2 = Math.Pow((double)this.WorldParameters.Rows, 2.0);
			return (float)Math.Sqrt(num + num2);
		}
	}

	public Region[] Regions { get; private set; }

	public WorldParameters WorldParameters { get; private set; }

	public bool WorldWrap { get; private set; }

	public GridMap<byte> TerrainMap
	{
		get
		{
			GridMap<byte> result;
			if ((result = this.terrainMap) == null)
			{
				result = (this.terrainMap = (this.Atlas.GetMap(WorldAtlas.Maps.Terrain) as GridMap<byte>));
			}
			return result;
		}
	}

	public Map<TerrainTypeName> TerrainTypeNameMap
	{
		get
		{
			Map<TerrainTypeName> result;
			if ((result = this.terrainTypeNameMap) == null)
			{
				result = (this.terrainTypeNameMap = (this.Atlas.GetMap(WorldAtlas.Tables.Terrains) as Map<TerrainTypeName>));
			}
			return result;
		}
	}

	public Dictionary<StaticString, short> TerrainTypeValuesByName
	{
		get
		{
			if (this.terrainTypeValuesByName == null)
			{
				this.terrainTypeValuesByName = new Dictionary<StaticString, short>();
				foreach (TerrainTypeName terrainTypeName in this.TerrainTypeNameMap.Data)
				{
					this.terrainTypeValuesByName.Add(terrainTypeName.Value, terrainTypeName.TypeValue);
				}
			}
			return this.terrainTypeValuesByName;
		}
	}

	public virtual IEnumerator Ignite()
	{
		this.HasBeenIgnited = true;
		this.Regions = new Region[0];
		yield break;
	}

	public bool IsMultiContinent()
	{
		return this.Continents.Length > 3;
	}

	public virtual IEnumerator Load(Archive archive)
	{
		if (archive == null)
		{
			throw new ArgumentNullException("The archive is null.");
		}
		this.Atlas = new WorldAtlas();
		this.Atlas.Deserialize(archive);
		GridMap<sbyte> heightMap = this.Atlas.GetMap(WorldAtlas.Maps.Height) as GridMap<sbyte>;
		if (heightMap == null)
		{
			Diagnostics.LogError("Can't find the height map in the current archive.");
			yield break;
		}
		yield return this.LoadWorldGeneratorConfiguration(archive);
		this.WorldParameters = new WorldParameters((short)heightMap.Width, (short)heightMap.Height, this.WorldWrap);
		yield return this.LoadRegionData(archive);
		yield return this.LoadRidgeData(archive);
		yield return this.LoadRiverData(archive);
		MemoryStream memoryStream = null;
		if (archive.TryGet(global::GameManager.GameFileName, out memoryStream))
		{
			using (memoryStream)
			{
				using (Amplitude.Xml.XmlReader xmlReader = Amplitude.Xml.XmlReader.Create(memoryStream))
				{
					xmlReader.Reader.ReadToDescendant("World");
					this.ReadXml(xmlReader);
					xmlReader.ReadEndElement("World");
					goto IL_1CC;
				}
			}
		}
		yield return this.LoadPointOfInterestData(archive);
		IL_1CC:
		try
		{
			this.InitializeContinentData();
			this.InitializePointOfInterestMap();
			this.InitializeArmiesMap();
			this.InitializeDistrictsMap();
			this.InitializeEncountersMap();
			this.InitializeWaterMap();
			this.InitializeTerraformStateMap();
			World.Seed = 0;
			if (archive.TryGet("WorldGeneratorReport.xml", out memoryStream))
			{
				using (memoryStream)
				{
					XmlDocument xmlDocument = new XmlDocument();
					xmlDocument.Load(memoryStream);
					XmlNode xmlNode = xmlDocument.DocumentElement.SelectSingleNode("//Seed");
					if (xmlNode != null)
					{
						int seed = 0;
						if (int.TryParse(xmlNode.InnerText, out seed))
						{
							World.Seed = seed;
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			Diagnostics.LogError("An exception has been raised. Exception = {0}.", new object[]
			{
				ex.ToString()
			});
			yield break;
		}
		this.HasBeenLoaded = true;
		yield break;
	}

	public virtual IEnumerator Load(string path)
	{
		World.Seed = 0;
		if (!File.Exists(path))
		{
			throw new FileNotFoundException("Invalid path.", path);
		}
		Archive archive = Archive.Open(path, ArchiveMode.Open);
		if (archive == null)
		{
			throw new FileLoadException("Failed to open the archive.", path);
		}
		yield return this.Load(archive);
		archive.Close();
		yield break;
	}

	public virtual void Release()
	{
		for (int i = 0; i < this.Regions.Length; i++)
		{
			this.Regions[i].Dispose();
		}
		this.Regions = new Region[0];
		this.Atlas = null;
		this.Continents = new Continent[0];
		this.HasBeenLoaded = false;
		this.HasBeenIgnited = false;
		this.TemporaryTerraformations.Clear();
	}

	public PointOfInterest CreatePointOfInterest(PointOfInterestDefinition pointOfInterestDefinition)
	{
		IDatabase<BiomeTypeMapping> database = Databases.GetDatabase<BiomeTypeMapping>(false);
		GridMap<byte> gridMap = this.Atlas.GetMap(WorldAtlas.Maps.Biomes) as GridMap<byte>;
		Map<BiomeTypeName> map = this.Atlas.GetMap(WorldAtlas.Tables.Biomes) as Map<BiomeTypeName>;
		Diagnostics.Assert(database != null);
		Diagnostics.Assert(gridMap != null);
		Diagnostics.Assert(map != null && map.Data != null);
		IDatabase<DepartmentOfIndustry.ConstructibleElement> database2 = Databases.GetDatabase<DepartmentOfIndustry.ConstructibleElement>(false);
		IDatabase<SimulationDescriptor> database3 = Databases.GetDatabase<SimulationDescriptor>(false);
		IGameService service = Services.GetService<IGameService>();
		IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
		GridMap<short> regionsMap = (GridMap<short>)this.Atlas.GetMap(WorldAtlas.Maps.Regions);
		return this.CreatePointOfInterest(pointOfInterestDefinition, service2, gridMap, regionsMap, map, database3, database, database2);
	}

	private void InitializeArmiesMap()
	{
		if (!(this.Atlas.GetMap(WorldAtlas.Maps.Armies) is GridMap<Army>))
		{
			GridMap<Army> mapInstance = new GridMap<Army>(WorldAtlas.Maps.Armies, (int)this.WorldParameters.Columns, (int)this.WorldParameters.Rows, null);
			this.Atlas.RegisterMapInstance<GridMap<Army>>(mapInstance);
		}
	}

	private void InitializeEncountersMap()
	{
		if (!(this.Atlas.GetMap(WorldAtlas.Maps.Encounters) is GridMap<Encounter>))
		{
			GridMap<Encounter> mapInstance = new GridMap<Encounter>(WorldAtlas.Maps.Encounters, (int)this.WorldParameters.Columns, (int)this.WorldParameters.Rows, null);
			this.Atlas.RegisterMapInstance<GridMap<Encounter>>(mapInstance);
		}
	}

	private void InitializeTerraformStateMap()
	{
		if (!(this.Atlas.GetMap(WorldAtlas.Maps.TerraformState) is GridMap<byte>))
		{
			GridMap<byte> mapInstance = new GridMap<byte>(WorldAtlas.Maps.TerraformState, (int)this.WorldParameters.Columns, (int)this.WorldParameters.Rows, null);
			this.Atlas.RegisterMapInstance<GridMap<byte>>(mapInstance);
		}
	}

	private void InitializeContinentData()
	{
		this.AverageRegionSize = 0f;
		int num = 0;
		for (int i = 0; i < this.Regions.Length; i++)
		{
			Region region = this.Regions[i];
			this.AverageRegionSize += (float)region.WorldPositions.Length;
			if (region.ContinentID != 255 && region.ContinentID >= num)
			{
				num = region.ContinentID + 1;
			}
		}
		Diagnostics.Assert(num < 255);
		num++;
		this.AverageRegionSize /= (float)this.Regions.Length;
		this.Continents = new Continent[num];
		for (int j = 0; j < this.Continents.Length; j++)
		{
			List<int> list = new List<int>();
			bool flag = false;
			bool flag2 = false;
			int k = 0;
			while (k < this.Regions.Length)
			{
				if (j == this.Continents.Length - 1 && this.Regions[k].ContinentID == 255)
				{
					this.Regions[k].ConvertContinentID(j);
					goto IL_F9;
				}
				if (j == this.Regions[k].ContinentID)
				{
					goto IL_F9;
				}
				IL_F1:
				k++;
				continue;
				IL_F9:
				list.Add(k);
				flag2 = (flag2 || this.Regions[k].IsWasteland);
				flag = (flag || this.Regions[k].IsOcean);
				goto IL_F1;
			}
			this.Continents[j] = new Continent(j, flag, flag2);
			this.Continents[j].RegionList = list.ToArray();
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				string text = "Land";
				if (flag)
				{
					text = "Ocean";
				}
				if (flag2)
				{
					text = "Wasteland";
				}
				Diagnostics.Log("ELCP InitializeContinentData, Continent {0}, {1}, Number of Regions {2}", new object[]
				{
					j,
					text,
					this.Continents[j].RegionList.Length
				});
			}
		}
		for (int l = 0; l < this.Continents.Length; l++)
		{
			List<int> list2 = new List<int>();
			if (this.Continents[l].IsOcean || this.Continents[l].IsWasteland)
			{
				this.Continents[l].CostalRegionList = list2.ToArray();
			}
			else
			{
				for (int m = 0; m < this.Continents[l].RegionList.Length; m++)
				{
					Region region2 = this.Regions[this.Continents[l].RegionList[m]];
					if (region2.IsLand)
					{
						for (int n = 0; n < region2.Borders.Length; n++)
						{
							if (this.Regions[region2.Borders[n].NeighbourRegionIndex].IsOcean && !list2.Contains(region2.Index))
							{
								list2.Add(region2.Index);
								break;
							}
						}
					}
				}
				this.Continents[l].CostalRegionList = list2.ToArray();
			}
		}
	}

	private void InitializeDistrictsMap()
	{
		if (!(this.Atlas.GetMap(WorldAtlas.Maps.Districts) is GridMap<District>))
		{
			GridMap<District> mapInstance = new GridMap<District>(WorldAtlas.Maps.Districts, (int)this.WorldParameters.Columns, (int)this.WorldParameters.Rows, null);
			this.Atlas.RegisterMapInstance<GridMap<District>>(mapInstance);
		}
	}

	private void InitializePointOfInterestMap()
	{
		GridMap<PointOfInterest> gridMap = this.Atlas.GetMap(WorldAtlas.Maps.PointOfInterest) as GridMap<PointOfInterest>;
		if (gridMap == null)
		{
			gridMap = new GridMap<PointOfInterest>(WorldAtlas.Maps.PointOfInterest, (int)this.WorldParameters.Columns, (int)this.WorldParameters.Rows, null);
			this.Atlas.RegisterMapInstance<GridMap<PointOfInterest>>(gridMap);
		}
		if (!(this.Atlas.GetMap(WorldAtlas.Maps.DefensiveTower) is GridMap<byte>))
		{
			GridMap<byte> mapInstance = new GridMap<byte>(WorldAtlas.Maps.DefensiveTower, (int)this.WorldParameters.Columns, (int)this.WorldParameters.Rows, null);
			this.Atlas.RegisterMapInstance<GridMap<byte>>(mapInstance);
		}
		for (int i = 0; i < this.Regions.Length; i++)
		{
			Region region = this.Regions[i];
			for (int j = 0; j < region.PointOfInterests.Length; j++)
			{
				PointOfInterest pointOfInterest = region.PointOfInterests[j];
				gridMap.SetValue((int)pointOfInterest.WorldPosition.Row, (int)pointOfInterest.WorldPosition.Column, pointOfInterest);
			}
		}
	}

	private IEnumerator LoadPointOfInterestData(Archive archive)
	{
		try
		{
			Map<PointOfInterestDefinition> map = (Map<PointOfInterestDefinition>)this.Atlas.GetMap(WorldAtlas.Tables.PointOfInterestDefinitions);
			if (map == null)
			{
				Diagnostics.LogError("Unable to retrieve the map of point of interest definitions from the world atlas.");
				yield break;
			}
			GridMap<short> regions = (GridMap<short>)this.Atlas.GetMap(WorldAtlas.Maps.Regions);
			IDatabase<SimulationDescriptor> simulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
			IDatabase<PointOfInterestTemplate> pointOfInterestTemplateDatabase = Databases.GetDatabase<PointOfInterestTemplate>(false);
			IDatabase<DepartmentOfIndustry.ConstructibleElement> constructibleElementDatabase = Databases.GetDatabase<DepartmentOfIndustry.ConstructibleElement>(false);
			IGameService gameService = Services.GetService<IGameService>();
			IGameEntityRepositoryService gameEntityRepository = gameService.Game.Services.GetService<IGameEntityRepositoryService>();
			List<PointOfInterest>[] pointsOfInterest = new List<PointOfInterest>[this.Regions.Length];
			for (int index = 0; index < pointsOfInterest.Length; index++)
			{
				pointsOfInterest[index] = new List<PointOfInterest>();
			}
			IDatabase<BiomeTypeMapping> biomeMappingDatabase = Databases.GetDatabase<BiomeTypeMapping>(false);
			GridMap<byte> biomesMap = this.Atlas.GetMap(WorldAtlas.Maps.Biomes) as GridMap<byte>;
			Map<BiomeTypeName> biomeTypeNames = this.Atlas.GetMap(WorldAtlas.Tables.Biomes) as Map<BiomeTypeName>;
			Diagnostics.Assert(biomeMappingDatabase != null);
			Diagnostics.Assert(biomesMap != null);
			Diagnostics.Assert(biomeTypeNames != null && biomeTypeNames.Data != null);
			IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
			int index2 = 0;
			while (index2 < map.Data.Length)
			{
				PointOfInterestDefinition pointOfInterestDefinition = map.Data[index2];
				short regionIndex = -1;
				try
				{
					regionIndex = regions.GetValue((int)pointOfInterestDefinition.WorldPosition.Row, (int)pointOfInterestDefinition.WorldPosition.Column);
				}
				catch
				{
					goto IL_337;
				}
				goto IL_230;
				IL_337:
				index2++;
				continue;
				IL_230:
				pointOfInterestDefinition.PointOfInterestTemplate = pointOfInterestTemplateDatabase.GetValue(pointOfInterestDefinition.PointOfInterestTemplateName);
				if (pointOfInterestDefinition.PointOfInterestTemplate == null)
				{
					Diagnostics.LogError("Unable to resolve template '{0}' for point of interest definition.", new object[]
					{
						pointOfInterestDefinition.PointOfInterestTemplateName
					});
					goto IL_337;
				}
				bool result = false;
				if (downloadableContentService != null && downloadableContentService.TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory.PointOfInterestTemplate, pointOfInterestDefinition.PointOfInterestTemplateName, out result) && !result)
				{
					goto IL_337;
				}
				PointOfInterest pointOfInterest = this.CreatePointOfInterest(pointOfInterestDefinition, gameEntityRepository, biomesMap, regions, biomeTypeNames, simulationDescriptorDatabase, biomeMappingDatabase, constructibleElementDatabase);
				if (pointOfInterest == null)
				{
					goto IL_337;
				}
				pointsOfInterest[(int)regionIndex].Add(pointOfInterest);
				goto IL_337;
			}
			for (int index3 = 0; index3 < this.Regions.Length; index3++)
			{
				this.Regions[index3].PointOfInterests = pointsOfInterest[index3].ToArray();
			}
		}
		catch (Exception ex)
		{
			Exception exception = ex;
			Diagnostics.LogError("Exception caught --> {0}.", new object[]
			{
				exception.ToString()
			});
		}
		yield break;
	}

	private PointOfInterest CreatePointOfInterest(PointOfInterestDefinition pointOfInterestDefinition, IGameEntityRepositoryService gameEntityRepository, GridMap<byte> biomesMap, GridMap<short> regionsMap, Map<BiomeTypeName> biomeTypeNames, IDatabase<SimulationDescriptor> simulationDescriptorDatabase, IDatabase<BiomeTypeMapping> biomeMappingDatabase, IDatabase<DepartmentOfIndustry.ConstructibleElement> constructibleElementDatabase)
	{
		Diagnostics.Assert(pointOfInterestDefinition != null);
		Diagnostics.Assert(pointOfInterestDefinition.PointOfInterestTemplate != null);
		Diagnostics.Assert(pointOfInterestDefinition.PointOfInterestTemplateName == pointOfInterestDefinition.PointOfInterestTemplate.Name);
		Diagnostics.Assert(pointOfInterestDefinition.WorldPosition.Row >= 0);
		Diagnostics.Assert(pointOfInterestDefinition.WorldPosition.Column >= 0);
		Diagnostics.Assert(pointOfInterestDefinition.WorldPosition.Row < this.WorldParameters.Rows);
		Diagnostics.Assert(pointOfInterestDefinition.WorldPosition.Column < this.WorldParameters.Columns);
		short num = -1;
		try
		{
			num = regionsMap.GetValue((int)pointOfInterestDefinition.WorldPosition.Row, (int)pointOfInterestDefinition.WorldPosition.Column);
		}
		catch
		{
			return null;
		}
		if (num == -1)
		{
			return null;
		}
		GameEntityGUID guid = gameEntityRepository.GenerateGUID();
		PointOfInterest pointOfInterest = new PointOfInterest(pointOfInterestDefinition, guid, this.Regions[(int)num]);
		SimulationDescriptor value = simulationDescriptorDatabase.GetValue("ClassPointOfInterest");
		if (value != null)
		{
			pointOfInterest.AddDescriptor(value, false);
		}
		byte biomeType = biomesMap.GetValue(pointOfInterestDefinition.WorldPosition);
		BiomeTypeName biomeTypeName = biomeTypeNames.Data.First((BiomeTypeName typeName) => typeName.TypeValue == (int)biomeType);
		BiomeTypeMapping biomeTypeMapping;
		if (biomeTypeName != null && !StaticString.IsNullOrEmpty(biomeTypeName.Value) && biomeMappingDatabase.TryGetValue(biomeTypeName.Value, out biomeTypeMapping) && biomeTypeMapping.Layers != null)
		{
			for (int i = 0; i < biomeTypeMapping.Layers.Length; i++)
			{
				if (!(biomeTypeMapping.Layers[i].Name != DepartmentOfTheInterior.SimulationLayerName))
				{
					if (!(biomeTypeMapping.Layers[i].Type != DepartmentOfTheInterior.SimulationLayerType))
					{
						SimulationLayer.Sample[] samples = biomeTypeMapping.Layers[i].Samples;
						for (int j = 0; j < samples.Length; j++)
						{
							SimulationDescriptor descriptor;
							if (simulationDescriptorDatabase.TryGetValue(samples[j].Value, out descriptor))
							{
								pointOfInterest.AddDescriptor(descriptor, false);
							}
							else
							{
								Diagnostics.LogWarning("Unable to retrieve the '{0}' descriptor from the database.", new object[]
								{
									samples[j].Value
								});
							}
						}
					}
				}
			}
		}
		string text;
		if (pointOfInterestDefinition.TryGetValue("Type", out text))
		{
			string x = "PointOfInterestType" + text;
			SimulationDescriptor descriptor2;
			if (simulationDescriptorDatabase.TryGetValue(x, out descriptor2))
			{
				pointOfInterest.AddDescriptor(descriptor2, false);
			}
			string text2;
			if (text == "Facility" && pointOfInterestDefinition.TryGetValue("Category", out text2))
			{
				string text3 = text2;
				if (text3 != null)
				{
					if (World.<>f__switch$map19 == null)
					{
						World.<>f__switch$map19 = new Dictionary<string, int>(2)
						{
							{
								"FacilityResourceDeposit_Luxury",
								0
							},
							{
								"FacilityResourceDeposit_Strategic",
								0
							}
						};
					}
					int num2;
					if (World.<>f__switch$map19.TryGetValue(text3, out num2))
					{
						if (num2 == 0)
						{
							SimulationDescriptor descriptor3;
							if (simulationDescriptorDatabase.TryGetValue(text2, out descriptor3))
							{
								pointOfInterest.AddDescriptor(descriptor3, false);
							}
							else
							{
								pointOfInterest.SimulationObject.Tags.AddTag(text2);
							}
							string str;
							if (pointOfInterestDefinition.TryGetValue("ResourceName", out str))
							{
								string text4 = "FacilityResourceDeposit" + "_" + str;
								if (simulationDescriptorDatabase.TryGetValue(text4, out descriptor3))
								{
									pointOfInterest.AddDescriptor(descriptor3, false);
								}
								else
								{
									pointOfInterest.SimulationObject.Tags.AddTag(text4);
								}
							}
							string str2;
							if (pointOfInterestDefinition.TryGetValue("ResourceType", out str2))
							{
								string text5 = "FacilityResourceDeposit" + "_" + str2;
								if (simulationDescriptorDatabase.TryGetValue(text5, out descriptor3))
								{
									pointOfInterest.AddDescriptor(descriptor3, false);
								}
								else
								{
									pointOfInterest.SimulationObject.Tags.AddTag(text5);
								}
							}
						}
					}
				}
			}
			string str3;
			if (text == "Fortress" && pointOfInterestDefinition.TryGetValue("Affinity", out str3))
			{
				string text6 = text + "Type" + str3;
				SimulationDescriptor descriptor4;
				if (simulationDescriptorDatabase.TryGetValue(text6, out descriptor4))
				{
					pointOfInterest.AddDescriptor(descriptor4, false);
				}
				else
				{
					pointOfInterest.SimulationObject.Tags.AddTag(text6);
				}
			}
			string str4;
			if ((text == "QuestLocation" || text == "NavalQuestLocation") && pointOfInterestDefinition.TryGetValue("QuestLocationType", out str4))
			{
				string text7 = text + "Type" + str4;
				SimulationDescriptor descriptor5;
				if (simulationDescriptorDatabase.TryGetValue(text7, out descriptor5))
				{
					pointOfInterest.AddDescriptor(descriptor5, false);
				}
				else
				{
					pointOfInterest.SimulationObject.Tags.AddTag(text7);
				}
			}
			if (text == "ResourceDeposit")
			{
				string str5;
				if (pointOfInterestDefinition.TryGetValue("ResourceName", out str5))
				{
					string text8 = text + "Type" + str5;
					SimulationDescriptor descriptor6;
					if (simulationDescriptorDatabase.TryGetValue(text8, out descriptor6))
					{
						pointOfInterest.AddDescriptor(descriptor6, false);
					}
					else
					{
						pointOfInterest.SimulationObject.Tags.AddTag(text8);
					}
				}
				string str6;
				if (pointOfInterestDefinition.TryGetValue("ResourceType", out str6))
				{
					string text9 = "ResourceType" + str6;
					SimulationDescriptor descriptor7;
					if (simulationDescriptorDatabase.TryGetValue(text9, out descriptor7))
					{
						pointOfInterest.AddDescriptor(descriptor7, false);
					}
					else
					{
						pointOfInterest.SimulationObject.Tags.AddTag(text9);
					}
				}
			}
			string str7;
			if (text == "Village" && pointOfInterestDefinition.TryGetValue("Affinity", out str7))
			{
				string text10 = text + "Type" + str7;
				SimulationDescriptor descriptor8;
				if (simulationDescriptorDatabase.TryGetValue(text10, out descriptor8))
				{
					pointOfInterest.AddDescriptor(descriptor8, false);
				}
				else
				{
					pointOfInterest.SimulationObject.Tags.AddTag(text10);
				}
			}
		}
		string text11;
		if (pointOfInterestDefinition.TryGetValue("Improvement", out text11))
		{
			DepartmentOfIndustry.ConstructibleElement constructibleElement;
			if (constructibleElementDatabase.TryGetValue(text11, out constructibleElement))
			{
				pointOfInterest.SwapPointOfInterestImprovement(constructibleElement, null);
			}
			else
			{
				Diagnostics.LogWarning("Cannot find the improvement '{0}' for point of interest template '{1}'.", new object[]
				{
					text11,
					pointOfInterestDefinition.PointOfInterestTemplateName
				});
			}
		}
		string text12;
		if (pointOfInterestDefinition.TryGetValue("SimulationDescriptor", out text12))
		{
			SimulationDescriptor descriptor9;
			if (simulationDescriptorDatabase.TryGetValue(text12, out descriptor9))
			{
				pointOfInterest.SwapDescriptor(descriptor9);
			}
			else
			{
				Diagnostics.LogWarning("Cannot find the simulation descriptor '{0}' for point of interest template'{1}'.", new object[]
				{
					text12,
					pointOfInterestDefinition.PointOfInterestTemplateName
				});
			}
		}
		pointOfInterest.Refresh(true);
		gameEntityRepository.Register(pointOfInterest);
		return pointOfInterest;
	}

	private IEnumerator LoadRegionData(Archive archive)
	{
		try
		{
			Map<Region> map = (Map<Region>)this.Atlas.GetMap(WorldAtlas.Tables.Regions);
			this.Regions = map.Data;
			GridMap<short> regionIndexPerWorldPosition = this.Atlas.GetMap(WorldAtlas.Maps.Regions) as GridMap<short>;
			List<WorldPosition>[] worldPositionPerRegion = new List<WorldPosition>[this.Regions.Length];
			for (int index = 0; index < this.Regions.Length; index++)
			{
				worldPositionPerRegion[index] = new List<WorldPosition>();
			}
			for (int y = 0; y < regionIndexPerWorldPosition.Height; y++)
			{
				for (int x = 0; x < regionIndexPerWorldPosition.Width; x++)
				{
					int regionIndex = (int)regionIndexPerWorldPosition.GetValue(y, x);
					worldPositionPerRegion[regionIndex].Add(new WorldPosition(y, x));
				}
			}
			for (int index2 = 0; index2 < this.Regions.Length; index2++)
			{
				this.Regions[index2].WorldPositions = worldPositionPerRegion[index2].ToArray();
				this.CheckRegionBorders(this.Regions[index2]);
			}
		}
		catch (Exception ex)
		{
			Exception exception = ex;
			Diagnostics.LogError("An exception has been raised. Exception = {0}.", new object[]
			{
				exception.ToString()
			});
			this.Regions = new Region[0];
		}
		yield break;
	}

	private IEnumerator LoadRidgeData(Archive archive)
	{
		try
		{
			GridMap<bool> ridgeMap = this.Atlas.GetMap(WorldAtlas.Maps.Ridges) as GridMap<bool>;
			if (ridgeMap != null && ridgeMap.Version > 0)
			{
				yield break;
			}
			Map<WorldRidge> ridgeTable = this.Atlas.GetMap(WorldAtlas.Tables.LegacyDoNotUseRidges) as Map<WorldRidge>;
			Diagnostics.Assert(ridgeTable != null);
			if (ridgeTable == null)
			{
				yield break;
			}
			if (ridgeMap == null)
			{
				Diagnostics.Log("Creating ridges grid map.");
				ridgeMap = new GridMap<bool>(WorldAtlas.Maps.Ridges, (int)this.WorldParameters.Columns, (int)this.WorldParameters.Rows, null);
				this.Atlas.RegisterMapInstance<GridMap<bool>>(ridgeMap);
			}
			else if (ridgeMap.Version == 0)
			{
				Diagnostics.Log("Invalid ridges grid map.");
			}
			int ridgeCount = 0;
			for (int ridgeIndex = 0; ridgeIndex < ridgeTable.Data.Length; ridgeIndex++)
			{
				WorldRidge worldRidge = ridgeTable.Data[ridgeIndex];
				for (int positionIndex = 0; positionIndex < worldRidge.RidgePositions.Length; positionIndex++)
				{
					WorldPosition worldPosition = worldRidge.RidgePositions[positionIndex];
					ridgeMap.SetValue(worldPosition, true);
					Diagnostics.Assert(ridgeMap.GetValue(worldPosition));
					ridgeCount++;
				}
			}
			ridgeMap.Version = 1;
			Diagnostics.Log("Regenerating ridges grid map from ridge Table version number = {0} and {1} ridges hexas. ridgeTable.Data.Length = {2}", new object[]
			{
				ridgeMap.Version,
				ridgeCount,
				ridgeTable.Data.Length
			});
			yield break;
		}
		catch (Exception ex)
		{
			Exception exception = ex;
			Diagnostics.LogError("An exception has been raised. Exception = {0}.", new object[]
			{
				exception.ToString()
			});
			yield break;
		}
		yield break;
	}

	private IEnumerator LoadRiverData(Archive archive)
	{
		try
		{
			Map<WorldRiver> riverTable = this.Atlas.GetMap(WorldAtlas.Tables.Rivers) as Map<WorldRiver>;
			if (riverTable == null)
			{
				yield break;
			}
			short[,] riverData = new short[(int)this.WorldParameters.Rows, (int)this.WorldParameters.Columns];
			for (int row = 0; row < (int)this.WorldParameters.Rows; row++)
			{
				for (int column = 0; column < (int)this.WorldParameters.Columns; column++)
				{
					riverData[row, column] = -1;
				}
			}
			for (int riverIndex = 0; riverIndex < riverTable.Data.Length; riverIndex++)
			{
				WorldRiver worldRiver = riverTable.Data[riverIndex];
				short riverId = riverTable.Data[riverIndex].Id;
				for (int positionIndex = 0; positionIndex < worldRiver.RiverPositions.Length; positionIndex++)
				{
					WorldPosition worldPosition = worldRiver.RiverPositions[positionIndex];
					riverData[(int)worldPosition.Row, (int)worldPosition.Column] = riverId;
				}
				Diagnostics.Log(string.Concat(new object[]
				{
					"Processing river ",
					riverId,
					" of type ",
					worldRiver.RiverTypeName
				}));
			}
			GridMap<short> riverMap = new GridMap<short>(WorldAtlas.Maps.River, (int)this.WorldParameters.Columns, (int)this.WorldParameters.Rows, riverData);
			this.Atlas.RegisterMapInstance<GridMap<short>>(riverMap);
			yield break;
		}
		catch (Exception ex)
		{
			Exception exception = ex;
			Diagnostics.LogError("An exception has been raised. Exception = {0}.", new object[]
			{
				exception.ToString()
			});
			yield break;
		}
		yield break;
	}

	private IEnumerator LoadWorldGeneratorConfiguration(Archive archive)
	{
		this.WorldWrap = false;
		try
		{
			MemoryStream memoryStream;
			if (archive.TryGet("WorldGeneratorConfiguration.xml", out memoryStream))
			{
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.Load(memoryStream);
				XmlNode xmlNode = xmlDocument.DocumentElement.SelectSingleNode("Properties/Wraps");
				if (xmlNode != null)
				{
					this.WorldWrap = bool.Parse(xmlNode.InnerText);
				}
				XmlNode xmlNode2 = xmlDocument.DocumentElement.SelectSingleNode("Properties/MinorFactionDifficulty");
				if (xmlNode2 != null)
				{
					this.MinorFactionDifficulty = xmlNode2.InnerText;
				}
				else
				{
					this.MinorFactionDifficulty = "Normal";
				}
				memoryStream.Close();
				memoryStream.Dispose();
			}
		}
		catch (Exception ex)
		{
			Diagnostics.LogError("An exception has been raised while loading the world generator settings; exception = {0}.", new object[]
			{
				ex.ToString()
			});
		}
		yield return null;
		yield break;
	}

	private void CheckRegionBorders(Region region)
	{
		for (int i = 0; i < region.Borders.Length; i++)
		{
			bool flag = true;
			int num = (i - 1 + region.Borders.Length) % region.Borders.Length;
			if (region.Borders[i].WorldPositions[0] != region.Borders[num].WorldPositions[region.Borders[num].WorldPositions.Length - 1])
			{
				flag = false;
			}
			else
			{
				int num2 = (i + 1) % region.Borders.Length;
				if (region.Borders[i].WorldPositions[region.Borders[i].WorldPositions.Length - 1] != region.Borders[num2].WorldPositions[0])
				{
					flag = false;
				}
			}
			if (!flag)
			{
				int num3 = 0;
				int num4 = region.Borders[i].WorldPositions.Length;
				while (WorldPosition.GetDistance(region.Borders[i].WorldPositions[0], region.Borders[i].WorldPositions[num4 - 1], this.WorldParameters.IsCyclicWorld, this.WorldParameters.Columns) == 1 && num3++ < region.Borders[i].WorldPositions.Length)
				{
					WorldPosition worldPosition = region.Borders[i].WorldPositions[0];
					for (int j = 0; j < num4 - 1; j++)
					{
						region.Borders[i].WorldPositions[j] = region.Borders[i].WorldPositions[j + 1];
					}
					region.Borders[i].WorldPositions[num4 - 1] = worldPosition;
				}
			}
		}
	}

	private bool TerrainPositionHasTag(string tag, WorldPosition position, GridMap<byte> terrainTypeMap, Map<TerrainTypeName> terrainTypeNames)
	{
		byte value = terrainTypeMap.GetValue(position);
		StaticString staticString = StaticString.Empty;
		StaticString empty = StaticString.Empty;
		if (terrainTypeNames.Data.TryGetValue((int)value, ref empty))
		{
			staticString = empty;
		}
		Diagnostics.Assert(!StaticString.IsNullOrEmpty(staticString));
		TerrainTypeMapping terrainTypeMapping = null;
		IDatabase<TerrainTypeMapping> database = Databases.GetDatabase<TerrainTypeMapping>(false);
		Diagnostics.Assert(database != null);
		if (!database.TryGetValue(staticString, out terrainTypeMapping))
		{
			return false;
		}
		if (terrainTypeMapping.Layers == null || terrainTypeMapping.Layers.Length <= 0)
		{
			return false;
		}
		for (int i = 0; i < terrainTypeMapping.Layers.Length; i++)
		{
			SimulationLayer simulationLayer = terrainTypeMapping.Layers[i];
			if (!(simulationLayer.Type != WorldPositionning.LayerTypeSimulation))
			{
				for (int j = 0; j < simulationLayer.Samples.Length; j++)
				{
					SimulationLayer.Sample sample = simulationLayer.Samples[j];
					if (sample.Value == tag)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private bool TryGetOriginalTerrainTypMapping(WorldPosition worldPosition, out TerrainTypeMapping mappedTerrain)
	{
		mappedTerrain = null;
		if (!worldPosition.IsValid)
		{
			return false;
		}
		World.TemporaryTerraformation temporaryTerraformation = this.TemporaryTerraformations.Find((World.TemporaryTerraformation t) => t.worldPosition == worldPosition);
		if (temporaryTerraformation == null)
		{
			Diagnostics.LogError("ELCP couldnt find temporaryTerraformation at {0}", new object[]
			{
				worldPosition
			});
			return false;
		}
		IDatabase<TerrainTypeMapping> database = Databases.GetDatabase<TerrainTypeMapping>(false);
		Diagnostics.Assert(database != null, "Terrain Type Mapping database can't be 'null'.");
		if (!database.TryGetValue(temporaryTerraformation.terrainName, out mappedTerrain))
		{
			Diagnostics.LogError("ELCP couldnt find {0}", new object[]
			{
				temporaryTerraformation.terrainName
			});
			return false;
		}
		return true;
	}

	public WorldPosition[] PerformReversibleTerraformation(WorldPosition[] positions, bool ReverseTerraform = false, int duration = 0)
	{
		if (positions == null)
		{
			Diagnostics.LogError("World.PerformTerraformation has received some invalid parameter(s).");
			return new WorldPosition[0];
		}
		List<KeyValuePair<WorldPosition, TerrainTypeMapping>> list = new List<KeyValuePair<WorldPosition, TerrainTypeMapping>>();
		Dictionary<WorldPosition, StaticString> dictionary = new Dictionary<WorldPosition, StaticString>();
		for (int i = 0; i < positions.Length; i++)
		{
			WorldPosition worldPosition = positions[i];
			if (worldPosition.IsValid)
			{
				TerrainTypeMapping value = null;
				StaticString value2 = null;
				if (!ReverseTerraform)
				{
					World.TemporaryTerraformation temporaryTerraformation = this.TemporaryTerraformations.Find((World.TemporaryTerraformation tt) => tt.worldPosition == worldPosition);
					if (temporaryTerraformation != null)
					{
						temporaryTerraformation.turnsRemaing = ((duration > temporaryTerraformation.turnsRemaing) ? duration : temporaryTerraformation.turnsRemaing);
						if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
						{
							Diagnostics.Log("ELCP Altering TemporaryTerraformation {0}", new object[]
							{
								temporaryTerraformation
							});
						}
					}
					else if (this.TryGetReversibleTerraformMapping(worldPosition, out value, out value2))
					{
						list.Add(new KeyValuePair<WorldPosition, TerrainTypeMapping>(worldPosition, value));
						dictionary.Add(worldPosition, value2);
					}
				}
				else if (ReverseTerraform && this.TryGetOriginalTerrainTypMapping(worldPosition, out value))
				{
					list.Add(new KeyValuePair<WorldPosition, TerrainTypeMapping>(worldPosition, value));
				}
			}
		}
		if (list.Count > 0)
		{
			WorldPosition[] array = this.PerformTerraformation(list.ToArray(), ReverseTerraform);
			if (!ReverseTerraform)
			{
				foreach (WorldPosition worldPosition2 in array)
				{
					World.TemporaryTerraformation temporaryTerraformation2 = new World.TemporaryTerraformation(worldPosition2, dictionary[worldPosition2], duration);
					this.TemporaryTerraformations.Add(temporaryTerraformation2);
					if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
					{
						Diagnostics.Log("ELCP Adding TemporaryTerraformation {0}", new object[]
						{
							temporaryTerraformation2
						});
					}
				}
			}
			else
			{
				for (int k = 0; k < positions.Length; k++)
				{
					WorldPosition pos = positions[k];
					this.TemporaryTerraformations.RemoveAll((World.TemporaryTerraformation tt) => tt.worldPosition == pos);
				}
				if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
				{
					Diagnostics.Log("ELCP {0} TemporaryTerraformations left", new object[]
					{
						this.TemporaryTerraformations.Count
					});
				}
			}
			return array;
		}
		return new WorldPosition[0];
	}

	public bool TryGetReversibleTerraformMapping(WorldPosition worldPosition, out TerrainTypeMapping mappedTerrain, out StaticString OriginalName)
	{
		OriginalName = null;
		mappedTerrain = null;
		if (!worldPosition.IsValid)
		{
			return false;
		}
		byte value = this.TerrainMap.GetValue(worldPosition);
		StaticString empty = StaticString.Empty;
		if (!this.TerrainTypeNameMap.Data.TryGetValue((int)value, ref empty))
		{
			return false;
		}
		IDatabase<TerrainTypeMapping> database = Databases.GetDatabase<TerrainTypeMapping>(false);
		Diagnostics.Assert(database != null, "Terrain Type Mapping database can't be 'null'.");
		TerrainTypeMapping terrainTypeMapping = null;
		if (!database.TryGetValue(empty, out terrainTypeMapping))
		{
			return false;
		}
		OriginalName = terrainTypeMapping.Name;
		return this.TryGetTerraformMapping(terrainTypeMapping, worldPosition, out mappedTerrain);
	}

	public string MinorFactionDifficulty { get; private set; }

	private static readonly StaticString TerraformationLayerName = new StaticString("Terraformation");

	private GridMap<byte> terrainMap;

	private Map<TerrainTypeName> terrainTypeNameMap;

	private Dictionary<StaticString, short> terrainTypeValuesByName;

	public List<World.TemporaryTerraformation> TemporaryTerraformations;

	public static class TerraformState
	{
		public const byte NotTerraformedState = 0;

		public const byte TransitionToTerraformedState = 1;

		public const byte TerraformedState = 2;
	}

	public enum WaterTileFreezeState
	{
		None = 100,
		Normal = 0,
		CloseToFrozenTile = 50,
		Frozen = 75
	}

	public enum WaterType
	{
		None,
		Water,
		Ocean,
		CoastalWaters,
		InlandWater
	}

	public class TemporaryTerraformation : IXmlSerializable
	{
		public TemporaryTerraformation(WorldPosition worldposition, StaticString terrainname, int duration)
		{
			this.worldPosition = worldposition;
			this.terrainName = terrainname;
			this.turnsRemaing = duration;
		}

		public override string ToString()
		{
			return string.Format("{0}:{1},{2}", this.worldPosition, this.terrainName, this.turnsRemaing);
		}

		public void WriteXml(Amplitude.Xml.XmlWriter writer)
		{
			writer.WriteVersionAttribute(1);
			writer.WriteAttributeString<short>("worldPositionRow", this.worldPosition.Row);
			writer.WriteAttributeString<short>("worldPositionCol", this.worldPosition.Column);
			writer.WriteAttributeString<StaticString>("terrainName", this.terrainName);
			writer.WriteAttributeString<int>("turnsRemaing", this.turnsRemaing);
		}

		public void ReadXml(Amplitude.Xml.XmlReader reader)
		{
			reader.ReadVersionAttribute();
			short attribute = reader.GetAttribute<short>("worldPositionRow", -1);
			short attribute2 = reader.GetAttribute<short>("worldPositionCol", -1);
			this.worldPosition = new WorldPosition(attribute, attribute2);
			this.terrainName = reader.GetAttribute<string>("terrainName", string.Empty);
			this.turnsRemaing = reader.GetAttribute<int>("turnsRemaing", 1);
			reader.ReadStartElement();
		}

		public WorldPosition worldPosition;

		public StaticString terrainName;

		public int turnsRemaing;
	}
}
