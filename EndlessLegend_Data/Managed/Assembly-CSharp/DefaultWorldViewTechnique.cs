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
		IGameSerializationService service = Amplitude.Unity.Framework.Services.GetService<IGameSerializationService>();
		if (service != null && service.GameSaveDescriptor != null)
		{
			pathToGameArchive = service.GameSaveDescriptor.SourceFileName;
		}
		if (string.IsNullOrEmpty(pathToGameArchive))
		{
			using (WorldGenerator worldGenerator3 = new WorldGenerator())
			{
				pathToGameArchive = worldGenerator3.GetOuputPath();
			}
		}
		string pathToGeometryArchive = pathToGameArchive;
		int geometryArchiveVersionIndex = 1;
		bool forceRegenerateGeometry = false;
		bool reloadOrRegenarateGeometry = false;
		bool flag = false;
		do
		{
			reloadOrRegenarateGeometry = false;
			using (Archive archive = Archive.Open(pathToGameArchive, ArchiveMode.Open))
			{
				MemoryStream memoryStream = null;
				Guid guid = Guid.Empty;
				try
				{
					if (archive.TryGet("WorldGeneratorReport.xml", out memoryStream))
					{
						XmlDocument xmlDocument = new XmlDocument();
						xmlDocument.Load(memoryStream);
						XmlNode xmlNode = xmlDocument.DocumentElement.SelectSingleNode("//GUID");
						if (xmlNode != null)
						{
							guid = new Guid(xmlNode.InnerText);
							if (guid != Guid.Empty)
							{
								ISessionService service2 = Amplitude.Unity.Framework.Services.GetService<ISessionService>();
								if (service2 != null)
								{
									Diagnostics.Assert(service2.Session != null);
									Diagnostics.Assert(service2.Session.IsOpened);
									IAdvancedVideoSettingsService service3 = Amplitude.Unity.Framework.Services.GetService<IAdvancedVideoSettingsService>();
									Diagnostics.Assert(service3 != null);
									string worldGeometryType = service3.WorldGeometryType;
									pathToGeometryArchive = System.IO.Path.Combine(Amplitude.Unity.Framework.Application.TempDirectory, string.Format("{0}/{1}_V{2}.geo", guid, worldGeometryType, geometryArchiveVersionIndex));
								}
							}
						}
					}
				}
				catch
				{
					guid = Guid.Empty;
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
						string fullPath = Amplitude.Unity.Framework.Path.GetFullPath(worldGenerator2.ConfigurationPath);
						XmlDocument xmlDocument2 = new XmlDocument();
						if (archive.TryGet("WorldGeneratorConfiguration.xml", out memoryStream))
						{
							xmlDocument2.Load(memoryStream);
							memoryStream.Close();
							memoryStream.Dispose();
							memoryStream = null;
						}
						else
						{
							worldGenerator2.WriteConfigurationFile();
							xmlDocument2.Load(fullPath);
						}
						XmlNode xmlNode2 = xmlDocument2.SelectSingleNode("//OutputDirectory");
						if (xmlNode2 == null)
						{
							xmlNode2 = xmlDocument2.CreateElement("OutputDirectory");
							xmlDocument2.AppendChild(xmlNode2);
						}
						xmlNode2.InnerText = Amplitude.Unity.Framework.Application.TempDirectory;
						XmlNode xmlNode3 = xmlDocument2.SelectSingleNode("//OutputPath");
						if (xmlNode3 == null)
						{
							xmlNode3 = xmlDocument2.CreateElement("OutputPath");
							xmlDocument2.AppendChild(xmlNode3);
						}
						xmlNode3.InnerText = pathToGameArchive;
						XmlNode xmlNode4 = xmlDocument2.SelectSingleNode("//SettingsPath");
						if (xmlNode4 == null)
						{
							xmlNode4 = xmlDocument2.CreateElement("SettingsPath");
							xmlDocument2.AppendChild(xmlNode4);
						}
						WorldGeneratorConfiguration worldGeneratorConfiguration = new WorldGeneratorConfiguration();
						xmlNode4.InnerText = worldGeneratorConfiguration.SettingsPath;
						IAdvancedVideoSettingsService service4 = Amplitude.Unity.Framework.Services.GetService<IAdvancedVideoSettingsService>();
						Diagnostics.Assert(service4 != null);
						string worldGeometryType2 = service4.WorldGeometryType;
						XmlNode xmlNode5 = xmlDocument2.SelectSingleNode("//Properties");
						Diagnostics.Assert(xmlNode5 != null);
						StaticString staticString = "Geometry";
						XmlNode xmlNode6 = xmlNode5.SelectSingleNode(string.Format("//{0}", staticString));
						Diagnostics.Assert(xmlNode6 != null);
						xmlNode6.InnerText = worldGeometryType2;
						OptionDefinition optionDefinition;
						if (Databases.GetDatabase<OptionDefinition>(false).TryGetValue(staticString, out optionDefinition) && optionDefinition.ItemDefinitions != null)
						{
							int num = -1;
							for (int i = 0; i < optionDefinition.ItemDefinitions.Length; i++)
							{
								if (optionDefinition.ItemDefinitions[i].Name == worldGeometryType2)
								{
									num = i;
									break;
								}
							}
							OptionDefinition.ItemDefinition itemDefinition = (num < 0) ? optionDefinition.Default : optionDefinition.ItemDefinitions[num];
							for (int j = 0; j < itemDefinition.KeyValuePairs.Length; j++)
							{
								OptionDefinition.ItemDefinition.KeyValuePair keyValuePair = itemDefinition.KeyValuePairs[j];
								XmlNode xmlNode7 = xmlNode5.SelectSingleNode(string.Format("//{0}", keyValuePair.Key));
								if (xmlNode7 != null)
								{
									Diagnostics.Assert(xmlNode7 != null);
									xmlNode7.InnerText = itemDefinition.KeyValuePairs[j].Value;
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
						XmlNode xmlNode8 = xmlDocument2.SelectSingleNode("//Scenario//DirectoryName");
						if (xmlNode8 != null && !Directory.Exists(xmlNode8.InnerText))
						{
							xmlNode8.InnerText = Amplitude.Unity.Framework.Path.FullPath + "../Public/WorldGenerator/Scenarios";
						}
						XmlWriterSettings settings = new XmlWriterSettings
						{
							Encoding = Encoding.UTF8,
							Indent = true,
							IndentChars = "  ",
							NewLineChars = "\r\n",
							NewLineHandling = NewLineHandling.Replace,
							OmitXmlDeclaration = false
						};
						using (XmlWriter xmlWriter = XmlWriter.Create(fullPath, settings))
						{
							xmlDocument2.WriteTo(xmlWriter);
						}
						archive.Close();
						yield return null;
						yield return worldGenerator2.GenerateWorldGeometry();
						flag = true;
					}
					WorldGenerator worldGenerator2 = null;
				}
				archive.Close();
			}
			Archive archive = null;
			if (!File.Exists(pathToGeometryArchive))
			{
				goto IL_7C3;
			}
			using (Archive archive2 = Archive.Open(pathToGeometryArchive, ArchiveMode.Open))
			{
				MemoryStream input = null;
				if (!archive2.TryGet("Geometry2.bin", out input))
				{
					throw new InvalidDataException("Archive does not contain the geometry binary.");
				}
				using (BinaryReader binaryReader = new BinaryReader(input))
				{
					WorldMeshes worldMeshes = WorldMeshes.LoadFromFile(binaryReader);
					WorldData worldData = WorldData.LoadFromFile(binaryReader);
					if (this.CheckWorldDataUpToDate(worldData))
					{
						this.WorldMeshes = worldMeshes;
						this.WorldData = worldData;
						if (this.PatchsData == null)
						{
							this.PatchsData = new DefaultWorldViewTechnique.PatchData[this.WorldMeshes.PatchCountX * this.WorldMeshes.PatchCountY];
							for (int k = 0; k < this.WorldMeshes.PatchCountY; k++)
							{
								for (int l = 0; l < this.WorldMeshes.PatchCountX; l++)
								{
									this.PatchsData[l + k * this.WorldMeshes.PatchCountX] = new DefaultWorldViewTechnique.PatchData();
								}
							}
						}
						DefaultWorldViewTechnique.PatchData[] patchsData = this.PatchsData;
						for (int m = 0; m < patchsData.Length; m++)
						{
							patchsData[m].InitMeshList();
						}
						if (this.WorldMeshes.MeshList != null)
						{
							foreach (Mesh mesh in this.WorldMeshes.MeshList)
							{
								this.GetSurfacePatchData(mesh.PatchY, mesh.PatchX).MeshList.Add(mesh);
							}
						}
						if (this.WorldData.Cliffs != null)
						{
							foreach (WorldData.CliffData cliffData in this.WorldData.Cliffs)
							{
								this.GetSurfacePatchData(cliffData.PatchY, cliffData.PatchX).AddCliffData(cliffData);
							}
						}
						if (this.WorldData.WaterPOIs != null)
						{
							foreach (WorldData.WaterPOIData waterPOIData in this.WorldData.WaterPOIs)
							{
								this.GetSurfacePatchData(waterPOIData.PatchY, waterPOIData.PatchX).AddWaterPOIData(waterPOIData);
							}
						}
					}
					else
					{
						reloadOrRegenarateGeometry = true;
						forceRegenerateGeometry = true;
						if (flag)
						{
							throw new InvalidDataException("The WorldGenerator.exe currently used is not up to date and produces deprecated data. Please update.");
						}
					}
				}
				archive2.Close();
			}
		}
		while (reloadOrRegenarateGeometry);
		yield break;
		IL_7C3:
		throw new FileNotFoundException(string.Format("The file containing the geometry data is missing. path: {0}", pathToGeometryArchive));
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
