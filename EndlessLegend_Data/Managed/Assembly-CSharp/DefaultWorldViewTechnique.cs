using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Amplitude;
using Amplitude.IO;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Session;
using Amplitude.Utilities.Maps;
using Hx.Geometry;

public class DefaultWorldViewTechnique : WorldViewTechnique
{
	public DefaultWorldViewTechnique.PatchData[] PatchsData { get; private set; }

	public HxTechniqueGraphicData HxTechniqueGraphicData { get; private set; }

	public UnityGraphicObjectPool UnityGraphicObjectPool { get; private set; }

	public WorldData WorldData { get; private set; }

	public WorldMeshes WorldMeshes { get; private set; }

	public override int HexaColumnPerPatch
	{
		get
		{
			Diagnostics.Assert(this.WorldData != null);
			return this.WorldMeshes.DefaultPatchWidth;
		}
	}

	public override int HexaRowPerPatch
	{
		get
		{
			Diagnostics.Assert(this.WorldData != null);
			return this.WorldMeshes.DefaultPatchHeight;
		}
	}

	public DefaultWorldViewTechnique.PatchData GetSurfacePatchData(int rowIndex, int columnIndex)
	{
		if (this.WorldMeshes != null)
		{
			int num = rowIndex * this.WorldMeshes.PatchCountX + columnIndex;
			if (num >= 0 && num < this.PatchsData.Length)
			{
				return this.PatchsData[num];
			}
		}
		return null;
	}

	public void Update()
	{
		if (UnityGraphicObjectPool.Singleton != null)
		{
			UnityGraphicObjectPool.Singleton.Update();
		}
	}

	public override IEnumerator Load()
	{
		this.UnityGraphicObjectPool = new UnityGraphicObjectPool();
		this.HxTechniqueGraphicData = base.GetComponent<HxTechniqueGraphicData>();
		yield return this.HxTechniqueGraphicData.LoadIFN(global::GameManager.Preferences.GameGraphicSettings.InstancingVerbose);
		yield return base.Load();
		yield break;
	}

	public override void Unload()
	{
		base.Unload();
		this.HxTechniqueGraphicData.UnloadIFN();
		this.HxTechniqueGraphicData = null;
		this.WorldData = null;
		this.WorldMeshes = null;
		this.UnityGraphicObjectPool.Unload();
		this.UnityGraphicObjectPool = null;
	}

	protected override IEnumerator OnIgnite()
	{
		base.OnIgnite();
		yield return this.LoadGeometry();
		yield break;
	}

	private IEnumerator LoadGeometry()
	{
		string pathToGameArchive = string.Empty;
		IGameSerializationService gameSerializationService = Amplitude.Unity.Framework.Services.GetService<IGameSerializationService>();
		if (gameSerializationService != null && gameSerializationService.GameSaveDescriptor != null)
		{
			pathToGameArchive = gameSerializationService.GameSaveDescriptor.SourceFileName;
		}
		if (string.IsNullOrEmpty(pathToGameArchive))
		{
			using (WorldGenerator worldGenerator = new WorldGenerator())
			{
				pathToGameArchive = worldGenerator.GetOuputPath();
			}
		}
		string pathToGeometryArchive = pathToGameArchive;
		int geometryArchiveVersionIndex = 1;
		bool forceRegenerateGeometry = false;
		bool reloadOrRegenarateGeometry = false;
		bool geometryWasAlreadyGeneratedOnce = false;
		for (;;)
		{
			reloadOrRegenarateGeometry = false;
			using (Archive archive = Archive.Open(pathToGameArchive, ArchiveMode.Open))
			{
				MemoryStream memoryStream = null;
				Guid mapGUID = Guid.Empty;
				try
				{
					if (archive.TryGet("WorldGeneratorReport.xml", out memoryStream))
					{
						XmlDocument document = new XmlDocument();
						document.Load(memoryStream);
						XmlNode guid = document.DocumentElement.SelectSingleNode("//GUID");
						if (guid != null)
						{
							mapGUID = new Guid(guid.InnerText);
							if (mapGUID != Guid.Empty)
							{
								ISessionService sessionService = Amplitude.Unity.Framework.Services.GetService<ISessionService>();
								if (sessionService != null)
								{
									Diagnostics.Assert(sessionService.Session != null);
									Diagnostics.Assert(sessionService.Session.IsOpened);
									IAdvancedVideoSettingsService advancedVideoSettingsService = Amplitude.Unity.Framework.Services.GetService<IAdvancedVideoSettingsService>();
									Diagnostics.Assert(advancedVideoSettingsService != null);
									string geometryValue = advancedVideoSettingsService.WorldGeometryType;
									pathToGeometryArchive = System.IO.Path.Combine(Amplitude.Unity.Framework.Application.TempDirectory, string.Format("{0}/{1}_V{2}.geo", mapGUID, geometryValue, geometryArchiveVersionIndex));
								}
							}
						}
					}
				}
				catch
				{
					mapGUID = Guid.Empty;
				}
				finally
				{
					if (memoryStream != null)
					{
						memoryStream.Close();
						memoryStream.Dispose();
						memoryStream = null;
					}
				}
				if (!File.Exists(pathToGeometryArchive) || forceRegenerateGeometry)
				{
					forceRegenerateGeometry = true;
					using (WorldGenerator worldGenerator2 = new WorldGenerator())
					{
						string worldGeneratorConfigurationPath = Amplitude.Unity.Framework.Path.GetFullPath(worldGenerator2.ConfigurationPath);
						XmlDocument worldGeneratorConfiguration = new XmlDocument();
						if (archive.TryGet("WorldGeneratorConfiguration.xml", out memoryStream))
						{
							worldGeneratorConfiguration.Load(memoryStream);
							memoryStream.Close();
							memoryStream.Dispose();
							memoryStream = null;
						}
						else
						{
							worldGenerator2.WriteConfigurationFile();
							worldGeneratorConfiguration.Load(worldGeneratorConfigurationPath);
						}
						XmlNode outputDirectory = worldGeneratorConfiguration.SelectSingleNode("//OutputDirectory");
						if (outputDirectory == null)
						{
							outputDirectory = worldGeneratorConfiguration.CreateElement("OutputDirectory");
							worldGeneratorConfiguration.AppendChild(outputDirectory);
						}
						outputDirectory.InnerText = Amplitude.Unity.Framework.Application.TempDirectory;
						XmlNode outputPath = worldGeneratorConfiguration.SelectSingleNode("//OutputPath");
						if (outputPath == null)
						{
							outputPath = worldGeneratorConfiguration.CreateElement("OutputPath");
							worldGeneratorConfiguration.AppendChild(outputPath);
						}
						outputPath.InnerText = pathToGameArchive;
						XmlNode settingsPath = worldGeneratorConfiguration.SelectSingleNode("//SettingsPath");
						if (settingsPath == null)
						{
							settingsPath = worldGeneratorConfiguration.CreateElement("SettingsPath");
							worldGeneratorConfiguration.AppendChild(settingsPath);
						}
						WorldGeneratorConfiguration localWorldGeneratorConfiguration = new WorldGeneratorConfiguration();
						settingsPath.InnerText = localWorldGeneratorConfiguration.SettingsPath;
						IAdvancedVideoSettingsService advancedVideoSettingsService2 = Amplitude.Unity.Framework.Services.GetService<IAdvancedVideoSettingsService>();
						Diagnostics.Assert(advancedVideoSettingsService2 != null);
						string geometryValue2 = advancedVideoSettingsService2.WorldGeometryType;
						XmlNode propertiesNode = worldGeneratorConfiguration.SelectSingleNode("//Properties");
						Diagnostics.Assert(propertiesNode != null);
						StaticString geometryEntryName = "Geometry";
						XmlNode geometryNode = propertiesNode.SelectSingleNode(string.Format("//{0}", geometryEntryName));
						Diagnostics.Assert(geometryNode != null);
						geometryNode.InnerText = geometryValue2;
						IDatabase<OptionDefinition> optionDefinitionsDatabase = Databases.GetDatabase<OptionDefinition>(false);
						OptionDefinition optionDefinition;
						if (optionDefinitionsDatabase.TryGetValue(geometryEntryName, out optionDefinition) && optionDefinition.ItemDefinitions != null)
						{
							int optionIndex = -1;
							for (int itemPosition = 0; itemPosition < optionDefinition.ItemDefinitions.Length; itemPosition++)
							{
								if (optionDefinition.ItemDefinitions[itemPosition].Name == geometryValue2)
								{
									optionIndex = itemPosition;
									break;
								}
							}
							OptionDefinition.ItemDefinition itemDefinition = (optionIndex < 0) ? optionDefinition.Default : optionDefinition.ItemDefinitions[optionIndex];
							for (int keyValuePairIndex = 0; keyValuePairIndex < itemDefinition.KeyValuePairs.Length; keyValuePairIndex++)
							{
								OptionDefinition.ItemDefinition.KeyValuePair keyValuePair = itemDefinition.KeyValuePairs[keyValuePairIndex];
								XmlNode valueNode = propertiesNode.SelectSingleNode(string.Format("//{0}", keyValuePair.Key));
								if (valueNode != null)
								{
									Diagnostics.Assert(valueNode != null);
									valueNode.InnerText = itemDefinition.KeyValuePairs[keyValuePairIndex].Value;
								}
								else
								{
									Diagnostics.LogWarning("Missing property {0} in XMLDocument.", new object[]
									{
										keyValuePair.Key
									});
								}
							}
						}
						XmlNode scenarioDirectoryNameNode = worldGeneratorConfiguration.SelectSingleNode("//Scenario//DirectoryName");
						if (scenarioDirectoryNameNode != null && !Directory.Exists(scenarioDirectoryNameNode.InnerText))
						{
							scenarioDirectoryNameNode.InnerText = Amplitude.Unity.Framework.Path.FullPath + "../Public/WorldGenerator/Scenarios";
						}
						XmlWriterSettings xmlWriterSettings = new XmlWriterSettings
						{
							Encoding = Encoding.UTF8,
							Indent = true,
							IndentChars = "  ",
							NewLineChars = "\r\n",
							NewLineHandling = NewLineHandling.Replace,
							OmitXmlDeclaration = false
						};
						using (XmlWriter writer = XmlWriter.Create(worldGeneratorConfigurationPath, xmlWriterSettings))
						{
							worldGeneratorConfiguration.WriteTo(writer);
						}
						archive.Close();
						yield return null;
						yield return worldGenerator2.GenerateWorldGeometry();
						geometryWasAlreadyGeneratedOnce = true;
					}
				}
				archive.Close();
			}
			if (!File.Exists(pathToGeometryArchive))
			{
				break;
			}
			using (Archive archive2 = Archive.Open(pathToGeometryArchive, ArchiveMode.Open))
			{
				MemoryStream geometry2MemoryStream = null;
				if (!archive2.TryGet("Geometry2.bin", out geometry2MemoryStream))
				{
					throw new InvalidDataException("Archive does not contain the geometry binary.");
				}
				using (BinaryReader binaryReader = new BinaryReader(geometry2MemoryStream))
				{
					WorldMeshes worldMeshes = WorldMeshes.LoadFromFile(binaryReader);
					WorldData worldData = WorldData.LoadFromFile(binaryReader);
					bool worldDataIsUpToDate = this.CheckWorldDataUpToDate(worldData);
					if (worldDataIsUpToDate)
					{
						this.WorldMeshes = worldMeshes;
						this.WorldData = worldData;
						if (this.PatchsData == null)
						{
							this.PatchsData = new DefaultWorldViewTechnique.PatchData[this.WorldMeshes.PatchCountX * this.WorldMeshes.PatchCountY];
							for (int i = 0; i < this.WorldMeshes.PatchCountY; i++)
							{
								for (int j = 0; j < this.WorldMeshes.PatchCountX; j++)
								{
									this.PatchsData[j + i * this.WorldMeshes.PatchCountX] = new DefaultWorldViewTechnique.PatchData();
								}
							}
						}
						foreach (DefaultWorldViewTechnique.PatchData onePatchData in this.PatchsData)
						{
							onePatchData.InitMeshList();
						}
						if (this.WorldMeshes.MeshList != null)
						{
							foreach (Mesh mesh in this.WorldMeshes.MeshList)
							{
								DefaultWorldViewTechnique.PatchData patchData = this.GetSurfacePatchData(mesh.PatchY, mesh.PatchX);
								patchData.MeshList.Add(mesh);
							}
						}
						if (this.WorldData.Cliffs != null)
						{
							foreach (WorldData.CliffData cliffData in this.WorldData.Cliffs)
							{
								DefaultWorldViewTechnique.PatchData patchData2 = this.GetSurfacePatchData(cliffData.PatchY, cliffData.PatchX);
								patchData2.AddCliffData(cliffData);
							}
						}
						if (this.WorldData.WaterPOIs != null)
						{
							foreach (WorldData.WaterPOIData waterPOIData in this.WorldData.WaterPOIs)
							{
								DefaultWorldViewTechnique.PatchData patchData3 = this.GetSurfacePatchData(waterPOIData.PatchY, waterPOIData.PatchX);
								patchData3.AddWaterPOIData(waterPOIData);
							}
						}
					}
					else
					{
						reloadOrRegenarateGeometry = true;
						forceRegenerateGeometry = true;
						if (geometryWasAlreadyGeneratedOnce)
						{
							throw new InvalidDataException("The WorldGenerator.exe currently used is not up to date and produces deprecated data. Please update.");
						}
					}
				}
				archive2.Close();
			}
			if (!reloadOrRegenarateGeometry)
			{
				goto Block_8;
			}
		}
		throw new FileNotFoundException(string.Format("The file containing the geometry data is missing. path: {0}", pathToGeometryArchive));
		Block_8:
		yield break;
	}

	private bool CheckWorldDataUpToDate(WorldData worldData)
	{
		WorldData.VersionData versionData = worldData.VersionDatas;
		if (worldData.VersionDatas == null)
		{
			versionData = new WorldData.VersionData();
			versionData.Version = 0;
		}
		IGameService service = Amplitude.Unity.Framework.Services.GetService<IGameService>();
		global::Game game = service.Game as global::Game;
		WorldAtlas atlas = game.World.Atlas;
		GridMap<byte> gridMap = atlas.GetMap(WorldAtlas.Maps.Anomalies) as GridMap<byte>;
		GridMap<byte> gridMap2 = atlas.GetMap(WorldAtlas.Maps.Terrain) as GridMap<byte>;
		GridMap<sbyte> gridMap3 = atlas.GetMap(WorldAtlas.Maps.Height) as GridMap<sbyte>;
		Map<PointOfInterestDefinition> map = atlas.GetMap(WorldAtlas.Tables.PointOfInterestDefinitions) as Map<PointOfInterestDefinition>;
		Map<WorldRiver> map2 = atlas.GetMap(WorldAtlas.Tables.Rivers) as Map<WorldRiver>;
		int num = 0;
		GridMap<bool> gridMap4 = atlas.GetMap(WorldAtlas.Maps.Ridges) as GridMap<bool>;
		if (gridMap4 != null)
		{
			num = gridMap4.Version;
		}
		if (versionData.Version != WorldData.VersionData.CurrentVersion)
		{
			Diagnostics.Log("Geometry is not uptodate : {0} : {1}, current version = {2}", new object[]
			{
				"Version",
				versionData.Version,
				WorldData.VersionData.CurrentVersion
			});
			return false;
		}
		if (versionData.TerrainTypeVersion != gridMap2.Version)
		{
			Diagnostics.Log("Geometry is not uptodate : {0} = {1}, atlas version = {2}", new object[]
			{
				"TerrainTypeVersion",
				versionData.TerrainTypeVersion,
				gridMap2.Version
			});
			return false;
		}
		if (versionData.AnomalyVersion != gridMap.Version)
		{
			Diagnostics.Log("Geometry is not uptodate : {0} = {1}, atlas version = {2}", new object[]
			{
				"AnomalyVersion",
				versionData.AnomalyVersion,
				gridMap.Version
			});
			return false;
		}
		if (versionData.TerrainHeightVersion != gridMap3.Version)
		{
			Diagnostics.Log("Geometry is not uptodate : {0} = {1}, atlas version = {2}", new object[]
			{
				"TerrainHeightVersion",
				versionData.TerrainHeightVersion,
				gridMap3.Version
			});
			return false;
		}
		if (versionData.POIVersion != map.Version)
		{
			Diagnostics.Log("Geometry is not uptodate : {0} = {1}, atlas version = {2}", new object[]
			{
				"POIVersion",
				versionData.POIVersion,
				map.Version
			});
			return false;
		}
		if (versionData.RidgeVersion != num && versionData.RidgeVersion > 0)
		{
			Diagnostics.Log("Geometry is not uptodate : {0} = {1}, atlas version = {2}", new object[]
			{
				"RidgeVersion",
				versionData.RidgeVersion,
				num
			});
			return false;
		}
		if (versionData.RiverVersion != map2.Version)
		{
			Diagnostics.Log("Geometry is not uptodate : {0} = {1}, atlas version = {2}", new object[]
			{
				"RiverVersion",
				versionData.RiverVersion,
				map2.Version
			});
			return false;
		}
		return true;
	}

	public class PatchData
	{
		public PatchData()
		{
			this.CliffDatas = new List<WorldData.CliffData>();
			this.WaterPOIDatas = new List<WorldData.WaterPOIData>();
		}

		public List<Mesh> MeshList { get; private set; }

		public List<WorldData.CliffData> CliffDatas { get; private set; }

		public List<WorldData.WaterPOIData> WaterPOIDatas { get; private set; }

		public void AddCliffData(WorldData.CliffData cliffData)
		{
			this.CliffDatas.Add(cliffData);
		}

		public void AddWaterPOIData(WorldData.WaterPOIData waterPOIData)
		{
			this.WaterPOIDatas.Add(waterPOIData);
		}

		public void InitMeshList()
		{
			this.MeshList = new List<Mesh>();
		}
	}
}
