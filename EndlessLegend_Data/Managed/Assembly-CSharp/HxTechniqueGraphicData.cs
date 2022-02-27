using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Localization;
using Amplitude.Unity.View;
using Amplitude.Utilities.Maps;
using Hx.Geometry;
using UnityEngine;

public class HxTechniqueGraphicData : Amplitude.Unity.Framework.Behaviour
{
	public byte[] TerrainIdInGeometryToTerrainMappingId { get; private set; }

	public byte[] TerrainIdInGeometryToSoundAmbianceNameIndex { get; private set; }

	public byte[] TerrainIdInGeometryToRoadIndex { get; private set; }

	public HxTechniqueGraphicData.OneTerrainTypeGraphicData[] OneTerrainTypeGraphicDatas { get; private set; }

	public HxTechniqueGraphicData.AllResourceFidsGraphicData AllResourceFidsGraphicDatas
	{
		get
		{
			return this.allResourceFidsGraphicDatas;
		}
	}

	public HxTechniqueGraphicData.RegionNameGraphicData RegionNameGraphicDatas
	{
		get
		{
			return this.regionNameGraphicDatas;
		}
	}

	public HxTechniqueGraphicData.DebugGraphicData DebugGraphicDatas
	{
		get
		{
			return this.debugGraphicDatas;
		}
	}

	public InstanciedMeshHolders InstanciedMeshHolders
	{
		get
		{
			return this.instanciedMeshHolders;
		}
	}

	public HxTechniqueGraphicData.EdgeOfTheWorldData EdgeOfTheWorldDatas
	{
		get
		{
			return this.edgeOfTheWorldDatas;
		}
	}

	public HxTechniqueGraphicData.AllOrbGraphicData AllOrbGraphicDatas
	{
		get
		{
			return this.allOrbGraphicDatas;
		}
	}

	public HxTechniqueGraphicData.AllWeatherGraphicData AllWeatherGraphicDatas
	{
		get
		{
			return this.allWeatherGraphicDatas;
		}
	}

	public HxTechniqueGraphicData.TradeRouteGraphicData TradeRouteGraphicDatas { get; private set; }

	public int RoadLevelPerRoadType
	{
		get
		{
			return 2;
		}
	}

	public HxTechniqueGraphicData.RoadGraphicData[] PackedRoadGraphicDatas
	{
		get
		{
			return this.packedRoadGraphicDatas;
		}
	}

	public List<StaticString> SoundAmbianceNames
	{
		get
		{
			return this.soundAmbianceNames;
		}
	}

	public void UnloadIFN()
	{
		if (!this.loaded)
		{
			return;
		}
		if (this.OneTerrainTypeGraphicDatas != null)
		{
			foreach (HxTechniqueGraphicData.OneTerrainTypeGraphicData oneTerrainTypeGraphicData in this.OneTerrainTypeGraphicDatas)
			{
				oneTerrainTypeGraphicData.Unload();
			}
			this.OneTerrainTypeGraphicDatas = null;
		}
		if (this.oneAnomalyTypeGraphicDatas != null)
		{
			foreach (HxTechniqueGraphicData.OneAnomalyTypeGraphicData oneAnomalyTypeGraphicData in this.oneAnomalyTypeGraphicDatas)
			{
				oneAnomalyTypeGraphicData.Unload();
			}
			this.oneAnomalyTypeGraphicDatas = null;
		}
		if (this.allOrbGraphicDatas != null)
		{
			this.allOrbGraphicDatas.Unload();
			this.allOrbGraphicDatas = null;
		}
		if (this.allWeatherGraphicDatas != null)
		{
			this.allWeatherGraphicDatas.Unload();
			this.allWeatherGraphicDatas = null;
		}
		if (this.edgeOfTheWorldDatas != null)
		{
			this.edgeOfTheWorldDatas.Unload();
			this.edgeOfTheWorldDatas = null;
		}
		if (this.allResourceFidsGraphicDatas != null)
		{
			this.allResourceFidsGraphicDatas.Unload();
			this.allResourceFidsGraphicDatas = null;
		}
		this.stringToPrefabDico.Clear();
		if (this.instanciedGameObjectFather != null)
		{
			UnityEngine.Object.DestroyImmediate(this.instanciedGameObjectFather);
			this.instanciedGameObjectFather = null;
		}
		if (this.roadGraphicDatas != null)
		{
			for (int k = 0; k < this.roadGraphicDatas.Length; k++)
			{
				if (this.roadGraphicDatas[k] != null)
				{
					this.roadGraphicDatas[k].Unload();
				}
			}
			this.roadGraphicDatas = null;
		}
		if (this.packedRoadGraphicDatas != null)
		{
			for (int l = 0; l < this.packedRoadGraphicDatas.Length; l++)
			{
				if (this.packedRoadGraphicDatas[l] != null)
				{
					this.packedRoadGraphicDatas[l].Unload();
				}
			}
			this.packedRoadGraphicDatas = null;
		}
		if (this.TradeRouteGraphicDatas != null)
		{
			this.TradeRouteGraphicDatas.Unload();
		}
		if (this.instanciedMeshHolders != null)
		{
			this.instanciedMeshHolders.Unload();
			this.instanciedMeshHolders = null;
		}
		this.stringToPrefabDico = null;
		this.soundAmbianceNames = null;
		this.roadNames = null;
		this.loaded = false;
	}

	public void LogGraphicData(TextWriter writer)
	{
	}

	public HxTechniqueGraphicData.OneTerrainTypeGraphicData GetOneTerrainTypeGraphicDataFromGeometryIndex(int indexInGeometry)
	{
		int num = (int)this.terrainIdInGeometryToOneTerrainTypeGraphicDatas[indexInGeometry];
		if (num < this.OneTerrainTypeGraphicDatas.Length)
		{
			return this.OneTerrainTypeGraphicDatas[num];
		}
		return null;
	}

	public HxTechniqueGraphicData.OneAnomalyTypeGraphicData GetAnomalyTypeGraphicData(byte anomalyIndexInAtlas)
	{
		Diagnostics.Assert((int)anomalyIndexInAtlas < this.anomalyIdInGeometryToOneAnomalyTypeGraphicDatas.Length);
		int num = (int)this.anomalyIdInGeometryToOneAnomalyTypeGraphicDatas[(int)anomalyIndexInAtlas];
		if (num < this.oneAnomalyTypeGraphicDatas.Length)
		{
			return this.oneAnomalyTypeGraphicDatas[num];
		}
		Diagnostics.Assert(num == 255);
		return null;
	}

	public GameObject GetOrCreatePrefabCopy(string value, bool forceCopy)
	{
		KeyValuePair<GameObject, bool> value2;
		if (!this.stringToPrefabDico.TryGetValue(value, out value2))
		{
			UnityEngine.Object @object = Resources.Load(value);
			GameObject gameObject = @object as GameObject;
			if (gameObject == null)
			{
				return null;
			}
			Transform transform = gameObject.transform;
			if (forceCopy && @object != null)
			{
				gameObject = (UnityEngine.Object.Instantiate(@object) as GameObject);
				gameObject.transform.position = UnityEngine.Vector3.zero;
				gameObject.transform.parent = this.instanciedGameObjectFather.transform;
				gameObject.SetActive(false);
			}
			value2 = new KeyValuePair<GameObject, bool>(gameObject, forceCopy);
			this.stringToPrefabDico.Add(value, value2);
		}
		bool value3 = value2.Value;
		Diagnostics.Assert(value3 == forceCopy);
		return value2.Key;
	}

	public IEnumerator ForgetDataNotUsedInMainRender()
	{
		if (this.OneTerrainTypeGraphicDatas != null)
		{
			foreach (HxTechniqueGraphicData.OneTerrainTypeGraphicData oneTerrainTypeGraphicData in this.OneTerrainTypeGraphicDatas)
			{
				oneTerrainTypeGraphicData.ForgetDataNotUsedInMainRender();
			}
		}
		if (this.allResourceFidsGraphicDatas != null)
		{
			this.allResourceFidsGraphicDatas.ForgetDataNotUsedInMainRender();
		}
		if (this.oneAnomalyTypeGraphicDatas != null)
		{
			foreach (HxTechniqueGraphicData.OneAnomalyTypeGraphicData oneAnomalyTypeGraphicData in this.oneAnomalyTypeGraphicDatas)
			{
				oneAnomalyTypeGraphicData.ForgetDataNotUsedInMainRender();
			}
		}
		if (this.edgeOfTheWorldDatas != null)
		{
			this.edgeOfTheWorldDatas.ForgetDataNotUsedInMainRender();
		}
		if (this.roadGraphicDatas != null)
		{
			for (int i = 0; i < this.roadGraphicDatas.Length; i++)
			{
				if (this.roadGraphicDatas[i] != null)
				{
					this.roadGraphicDatas[i].ForgetDataNotUsedInMainRender();
				}
			}
		}
		if (this.packedRoadGraphicDatas != null)
		{
			for (int j = 0; j < this.packedRoadGraphicDatas.Length; j++)
			{
				if (this.packedRoadGraphicDatas[j] != null)
				{
					this.packedRoadGraphicDatas[j].ForgetDataNotUsedInMainRender();
				}
			}
		}
		if (this.TradeRouteGraphicDatas != null)
		{
			this.TradeRouteGraphicDatas.ForgetDataNotUsedInMainRender();
		}
		string[] stringToPrefabDicoKeys = this.stringToPrefabDico.Keys.ToArray<string>();
		for (int k = 0; k < stringToPrefabDicoKeys.Length; k++)
		{
			if (!this.stringToPrefabDico[stringToPrefabDicoKeys[k]].Value)
			{
				this.stringToPrefabDico.Remove(stringToPrefabDicoKeys[k]);
			}
		}
		Diagnostics.Progress.SetProgress(0.33f);
		GC.Collect();
		Diagnostics.Progress.SetProgress(0.66f);
		Resources.UnloadUnusedAssets();
		yield return null;
		yield break;
	}

	public IEnumerator LoadIFN(bool verbose)
	{
		if (this.loaded)
		{
			yield break;
		}
		Diagnostics.Log("HxTechniqueGraphicData.Load Begin");
		this.verbose = verbose;
		this.EvaluateMeshGenerationOptions();
		ILocalizationService localizationService = Services.GetService<ILocalizationService>();
		Amplitude.Unity.View.IViewService viewService = Services.GetService<Amplitude.Unity.View.IViewService>();
		WorldView worldView = viewService.CurrentView as WorldView;
		WorldController worldController = worldView.GetComponent<WorldController>();
		Map<TerrainTypeName> terrainTypeNames = worldController.WorldAtlas.GetMap(WorldAtlas.Tables.Terrains) as Map<TerrainTypeName>;
		Diagnostics.Assert(terrainTypeNames != null);
		IDatabase<TerrainTypeMapping> terrainTypeMappingDatabase = Databases.GetDatabase<TerrainTypeMapping>(false);
		Diagnostics.Assert(terrainTypeMappingDatabase != null);
		float bboxMarginX = worldController.WorldPatchSize.x;
		float bboxMarginZ = worldController.WorldPatchSize.z;
		this.instanciedMeshHolders = new InstanciedMeshHolders(256 * InstanciedMeshHelpers.PositionForwardScaleZPixelsPerInstance, 300, 64 * InstanciedMeshHelpers.PositionForwardScaleZPixelsPerInstance, 512, new UnityEngine.Vector4(-bboxMarginX, -3f, -bboxMarginZ, -10f), new UnityEngine.Vector4(worldController.WorldRectangle.width + bboxMarginX, 5f, worldController.WorldRectangle.height + bboxMarginZ, 10f), new UnityEngine.Vector3(-worldController.WorldRectangle.width, 0f, 0f), new UnityEngine.Vector3(worldController.WorldRectangle.width, 0f, 0f), new InstanciedMeshHolders.GetOrCreateUnityEngineMesh(this.InstancingMeshAllocationHook), new InstanciedMeshHolders.GetInstancingShader(this.GetInstancingShader), this.verbose);
		this.stringToPrefabDico = new Dictionary<string, KeyValuePair<GameObject, bool>>();
		this.soundAmbianceNames = new List<StaticString>();
		this.roadNames = new List<string>();
		this.instanciedGameObjectFather = new GameObject();
		this.instanciedGameObjectFather.name = "HxTechniqueGraphicData.instanciedGameObjectFather";
		this.instanciedGameObjectFather.SetActive(false);
		if (this.instanciedGameObjectFather.transform == null)
		{
			this.instanciedGameObjectFather.AddComponent<Transform>();
		}
		yield return this.BuildTerrainIdInGeometryToTerrainMappingId(terrainTypeNames, terrainTypeMappingDatabase, localizationService, this.verbose);
		yield return this.BuildAnomalyTypeGraphicData(worldController, localizationService, this.verbose);
		this.allResourceFidsGraphicDatas = new HxTechniqueGraphicData.AllResourceFidsGraphicData();
		yield return this.allResourceFidsGraphicDatas.Load(new HxTechniqueGraphicData.GetOrCreatePrefabCopyDelegate(this.GetOrCreatePrefabCopy), new DecorationPrefabData.GetOrCreateInstanciedMeshIndexDelegate(this.GetOrCreateObjectInfo), new DecorationPrefabData.RetrieveInstanciedMeshIndexDelegate(this.RetrieveMeshIndex), this.instanciedGameObjectFather, this.verbose);
		this.CalculOccurenceExpectationAndSetMeshPerBatch(worldController);
		yield return null;
		this.regionNameGraphicDatas = new HxTechniqueGraphicData.RegionNameGraphicData(worldController, this.regionNameFont, this.regionNameMaterial, this.regionTextSize);
		this.edgeOfTheWorldDatas = new HxTechniqueGraphicData.EdgeOfTheWorldData();
		yield return this.edgeOfTheWorldDatas.Load(new HxTechniqueGraphicData.GetOrCreatePrefabCopyDelegate(this.GetOrCreatePrefabCopy), new DecorationPrefabData.GetOrCreateInstanciedMeshIndexDelegate(this.GetOrCreateObjectInfo), new DecorationPrefabData.RetrieveInstanciedMeshIndexDelegate(this.RetrieveMeshIndex), this.instanciedGameObjectFather, this.verbose);
		this.debugGraphicDatas = new HxTechniqueGraphicData.DebugGraphicData(worldController, this.debugTextFont, this.debugTextMaterial, this.debugTextSize);
		this.roadGraphicDatas = new HxTechniqueGraphicData.RoadGraphicData[this.RoadLevelPerRoadType * this.roadNames.Count];
		this.packedRoadGraphicDatas = new HxTechniqueGraphicData.RoadGraphicData[this.RoadLevelPerRoadType * this.roadNames.Count];
		int roadCount = this.roadNames.Count * this.RoadLevelPerRoadType;
		string packedRoadPrefabFormat = "Prefabs/Environments/Roads/Packed/{0}/{1}_Level{2}_00";
		string loadingRoadMessage = localizationService.Localize("%LoadingRoad", "Loading Road.");
		for (int i = 0; i < this.roadNames.Count; i++)
		{
			for (int j = 0; j < this.RoadLevelPerRoadType; j++)
			{
				string packedPrefabFormat = string.Format(packedRoadPrefabFormat, this.roadNames[i], "{0}", j.ToString("00"));
				this.packedRoadGraphicDatas[i * this.RoadLevelPerRoadType + j] = new HxTechniqueGraphicData.RoadGraphicData();
				yield return this.packedRoadGraphicDatas[i * this.RoadLevelPerRoadType + j].Load(packedPrefabFormat, new HxTechniqueGraphicData.GetOrCreatePrefabCopyDelegate(this.GetOrCreatePrefabCopy), new DecorationPrefabData.GetOrCreateInstanciedMeshIndexDelegate(this.GetOrCreateObjectInfo), new DecorationPrefabData.RetrieveInstanciedMeshIndexDelegate(this.RetrieveMeshIndex), this.instanciedGameObjectFather, this.verbose);
				Diagnostics.Progress.SetProgress((float)(i * this.RoadLevelPerRoadType + j) / (float)(roadCount + 1), loadingRoadMessage);
			}
		}
		this.TradeRouteGraphicDatas = new HxTechniqueGraphicData.TradeRouteGraphicData();
		yield return this.TradeRouteGraphicDatas.Load(new HxTechniqueGraphicData.GetOrCreatePrefabCopyDelegate(this.GetOrCreatePrefabCopy), new DecorationPrefabData.GetOrCreateInstanciedMeshIndexDelegate(this.GetOrCreateObjectInfo), new DecorationPrefabData.RetrieveInstanciedMeshIndexDelegate(this.RetrieveMeshIndex), this.instanciedGameObjectFather, this.verbose);
		IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
		if (downloadableContentService != null && downloadableContentService.IsShared(DownloadableContent13.ReadOnlyName))
		{
			this.allOrbGraphicDatas = new HxTechniqueGraphicData.AllOrbGraphicData();
			yield return this.allOrbGraphicDatas.Load(localizationService, new HxTechniqueGraphicData.GetOrCreatePrefabCopyDelegate(this.GetOrCreatePrefabCopy), new DecorationPrefabData.GetOrCreateInstanciedMeshIndexDelegate(this.GetOrCreateObjectInfo), new DecorationPrefabData.RetrieveInstanciedMeshIndexDelegate(this.RetrieveMeshIndex), new DecorationPrefabData.AddSmallMeshesDelegate(this.AddSmallMeshes), this.instanciedGameObjectFather, verbose);
		}
		if ((downloadableContentService != null && downloadableContentService.IsShared(DownloadableContent16.ReadOnlyName)) || downloadableContentService.IsShared(DownloadableContent19.ReadOnlyName))
		{
			this.allWeatherGraphicDatas = new HxTechniqueGraphicData.AllWeatherGraphicData();
			yield return this.allWeatherGraphicDatas.Load(localizationService, new HxTechniqueGraphicData.GetOrCreatePrefabCopyDelegate(this.GetOrCreatePrefabCopy), new DecorationPrefabData.GetOrCreateInstanciedMeshIndexDelegate(this.GetOrCreateObjectInfo), new DecorationPrefabData.RetrieveInstanciedMeshIndexDelegate(this.RetrieveMeshIndex), new DecorationPrefabData.AddSmallMeshesDelegate(this.AddSmallMeshes), this.instanciedGameObjectFather, verbose);
		}
		string createInstancingMeshMessage = localizationService.Localize("%CreateInstancingMesh", "Create Instancing Mesh.");
		Diagnostics.Progress.SetProgress(0f, createInstancingMeshMessage);
		yield return this.instanciedMeshHolders.CreateAllMesh(10);
		yield return new WaitForEndOfFrame();
		string clearingUnusedMemoryMessage = localizationService.Localize("%ClearingUnusedMemory", "Clearing unused memory.");
		if (!global::GameManager.Preferences.GameGraphicSettings.EnableAlternativeRendering)
		{
			Diagnostics.Progress.SetProgress(0f, clearingUnusedMemoryMessage);
			yield return new WaitForEndOfFrame();
			yield return this.ForgetDataNotUsedInMainRender();
			Diagnostics.Progress.SetProgress(1f, clearingUnusedMemoryMessage);
			yield return new WaitForEndOfFrame();
		}
		this.loaded = true;
		string allTerrainDataLoadedMessage = localizationService.Localize("%AllTerrainDataLoaded", "All terrain data loaded.");
		Diagnostics.Progress.SetProgress(0f, allTerrainDataLoadedMessage);
		yield return new WaitForEndOfFrame();
		yield break;
	}

	private int RetrieveMeshIndex(string name, int pixelsPerInstance, Matrix4x4 matrix, Material material, int additionalMaxPerBatchSmallMeshInstance)
	{
		int num = this.GetMaxPerBatchSmallMeshInstance(name);
		if (additionalMaxPerBatchSmallMeshInstance > 0)
		{
			num = Math.Min(num, additionalMaxPerBatchSmallMeshInstance);
		}
		return this.instanciedMeshHolders.RetrieveMeshIndex(name, pixelsPerInstance, matrix, material, num);
	}

	private int GetOrCreateObjectInfo(UnityEngine.Mesh mesh, int pixelsPerInstance, int subMeshIndex, Matrix4x4 matrix, Material material, int additionalMaxPerBatchSmallMeshInstance)
	{
		int num = this.GetMaxPerBatchSmallMeshInstance(mesh.name);
		if (additionalMaxPerBatchSmallMeshInstance > 0)
		{
			num = Math.Min(num, additionalMaxPerBatchSmallMeshInstance);
		}
		return this.instanciedMeshHolders.AddOrRetrieveSmallMesh(new InstanciedMeshHolders.MeshAndSubMeshIndex(mesh, subMeshIndex), pixelsPerInstance, matrix, material, num);
	}

	private int AddSmallMeshes(string idName, List<InstanciedMeshHolders.MeshAndSubMeshIndex> meshes, int pixelsPerInstance, Matrix4x4 transformation, List<Matrix4x4> transformations, Material material, int maxPerBatchSmallMeshInstance)
	{
		return this.instanciedMeshHolders.AddSmallMeshes(idName, meshes, pixelsPerInstance, transformation, transformations, material, maxPerBatchSmallMeshInstance);
	}

	private int GetMaxPerBatchSmallMeshInstance(string meshName)
	{
		for (int i = 0; i < this.instancingPolicyMeshNames.Length; i++)
		{
			if (meshName.IndexOf(this.instancingPolicyMeshNames[i], 0) == 0)
			{
				return this.instancingPolicyMeshCount[i];
			}
		}
		Diagnostics.LogWarning("Unable to find instancing policy for mesh [{0}].", new object[]
		{
			meshName
		});
		return 16;
	}

	private UnityEngine.Mesh InstancingMeshAllocationHook()
	{
		return UnityGraphicObjectPool.Singleton.GetOrCreateMesh(PrimitiveLayerMask.Default);
	}

	private bool GetInstancingShader(Material material, int pixelsPerInstance, out Shader instancingShader, out int meshGenerationOption)
	{
		Shader shader = material.shader;
		string name = shader.name;
		string text = string.Concat(new object[]
		{
			"Instancing",
			pixelsPerInstance.ToString(),
			'/',
			name
		});
		instancingShader = Shader.Find(text);
		if (instancingShader == null)
		{
			string message = string.Format("Unable to find instancing shader [{0}] in material [{1}]", text, material.name);
			Debug.LogError(message);
		}
		else if (this.verbose)
		{
			string message2 = string.Format("Found instancing shader [{0}] to replace [{1}] in material [{2}]", instancingShader.name, name, material.name);
			Debug.Log(message2);
		}
		meshGenerationOption = 0;
		bool flag = false;
		for (int i = 0; i < this.meshGenerationPolicyShaders.Length; i++)
		{
			if (this.meshGenerationPolicyShaders[i] == shader)
			{
				meshGenerationOption = this.meshGenerationPolicyOptionMasks[i];
				if (flag)
				{
					Diagnostics.LogError("Mesh generation instancing policy duplicate for shader [{0}].", new object[]
					{
						shader.name
					});
				}
				flag = true;
			}
		}
		if (!flag)
		{
			Diagnostics.LogWarning("Unable to find mesh generation instancing policy for shader [{0}].", new object[]
			{
				shader.name
			});
		}
		return instancingShader != null;
	}

	private IEnumerator BuildAnomalyTypeGraphicData(WorldController worldController, ILocalizationService localizationService, bool verbose)
	{
		Diagnostics.Assert(this.instanciedGameObjectFather != null);
		IDatabase<AnomalyTypeMapping> anomalyTypeMappingDatabase = Databases.GetDatabase<AnomalyTypeMapping>(true);
		Map<AnomalyTypeDefinition> anomaliesTypeDefinitions = worldController.WorldAtlas.GetMap(WorldAtlas.Tables.Anomalies) as Map<AnomalyTypeDefinition>;
		AnomalyTypeMapping[] anomalyTypeMappings = anomalyTypeMappingDatabase.GetValues();
		Dictionary<string, int> stringToAnomalyIndex = new Dictionary<string, int>();
		string loadingAnomalyMessage = localizationService.Localize("%LoadingAnomaly", "Loading anomaly");
		Diagnostics.Progress.SetProgress(0f, loadingAnomalyMessage);
		this.oneAnomalyTypeGraphicDatas = new HxTechniqueGraphicData.OneAnomalyTypeGraphicData[anomalyTypeMappings.Length];
		for (int i = 0; i < anomalyTypeMappings.Length; i++)
		{
			HxTechniqueGraphicData.OneAnomalyTypeGraphicData oneAnomalyTypeGraphicData = new HxTechniqueGraphicData.OneAnomalyTypeGraphicData(anomalyTypeMappings[i], new HxTechniqueGraphicData.GetOrCreatePrefabCopyDelegate(this.GetOrCreatePrefabCopy), new DecorationPrefabData.GetOrCreateInstanciedMeshIndexDelegate(this.GetOrCreateObjectInfo), new DecorationPrefabData.RetrieveInstanciedMeshIndexDelegate(this.RetrieveMeshIndex), new DecorationPrefabData.AddSmallMeshesDelegate(this.AddSmallMeshes), this.instanciedGameObjectFather, verbose);
			this.oneAnomalyTypeGraphicDatas[i] = oneAnomalyTypeGraphicData;
			stringToAnomalyIndex.Add(oneAnomalyTypeGraphicData.AnomalyTypeName, i);
			Diagnostics.Progress.SetProgress((float)(i + 1) / (float)anomalyTypeMappings.Length);
			yield return null;
		}
		this.anomalyIdInGeometryToOneAnomalyTypeGraphicDatas = new byte[256];
		for (int j = 0; j < this.anomalyIdInGeometryToOneAnomalyTypeGraphicDatas.Length; j++)
		{
			StaticString anomalyTypeName = null;
			if (anomaliesTypeDefinitions.Data.TryGetValue(j, ref anomalyTypeName))
			{
				int index = 255;
				bool found = stringToAnomalyIndex.TryGetValue(anomalyTypeName, out index);
				Diagnostics.Assert(found);
				this.anomalyIdInGeometryToOneAnomalyTypeGraphicDatas[j] = (byte)index;
			}
			else
			{
				this.anomalyIdInGeometryToOneAnomalyTypeGraphicDatas[j] = byte.MaxValue;
			}
		}
		Diagnostics.Progress.SetProgress(1f);
		yield break;
	}

	private IEnumerator BuildTerrainIdInGeometryToTerrainMappingId(Map<TerrainTypeName> terrainTypeNames, IDatabase<TerrainTypeMapping> terrainTypeMappingDatabase, ILocalizationService localizationService, bool verbose)
	{
		Diagnostics.Assert(this.instanciedGameObjectFather != null);
		int textureCountPerRowInAtlas = 8;
		this.TerrainIdInGeometryToTerrainMappingId = new byte[256];
		this.TerrainIdInGeometryToSoundAmbianceNameIndex = new byte[256];
		this.terrainIdInGeometryToOneTerrainTypeGraphicDatas = new byte[256];
		this.TerrainIdInGeometryToRoadIndex = new byte[256];
		for (int i = 0; i < 256; i++)
		{
			this.TerrainIdInGeometryToTerrainMappingId[i] = byte.MaxValue;
			this.TerrainIdInGeometryToSoundAmbianceNameIndex[i] = byte.MaxValue;
			this.terrainIdInGeometryToOneTerrainTypeGraphicDatas[i] = byte.MaxValue;
			this.TerrainIdInGeometryToRoadIndex[i] = 0;
		}
		int terrainTypeCount = terrainTypeMappingDatabase.Count<TerrainTypeMapping>();
		this.OneTerrainTypeGraphicDatas = new HxTechniqueGraphicData.OneTerrainTypeGraphicData[terrainTypeCount];
		terrainTypeCount = 0;
		string loadingTerrainTypeMessage = localizationService.Localize("%LoadingTerrainType", "Loading Terrain type.");
		Diagnostics.Progress.SetProgress(0f, loadingTerrainTypeMessage);
		yield return null;
		this.roadNames.Add("Rocks");
		this.roadNames.Add("Volcano");
		this.roadNames.Add("Arctic");
		this.roadNames.Add("Tundra");
		this.roadNames.Add("Chilly");
		this.roadNames.Add("Tropical");
		this.roadNames.Add("Desert");
		this.roadNames.Add("Temperate");
		int terrainCount = terrainTypeMappingDatabase.Count<TerrainTypeMapping>();
		foreach (TerrainTypeMapping terrainTypeMapping in terrainTypeMappingDatabase)
		{
			this.OneTerrainTypeGraphicDatas[terrainTypeCount] = new HxTechniqueGraphicData.OneTerrainTypeGraphicData(terrainTypeMapping, new HxTechniqueGraphicData.GetOrCreatePrefabCopyDelegate(this.GetOrCreatePrefabCopy), new DecorationPrefabData.GetOrCreateInstanciedMeshIndexDelegate(this.GetOrCreateObjectInfo), new DecorationPrefabData.RetrieveInstanciedMeshIndexDelegate(this.RetrieveMeshIndex), this.instanciedGameObjectFather, this.soundAmbianceNames, this.roadNames, verbose);
			terrainTypeCount++;
			Diagnostics.Progress.SetProgress((float)(terrainTypeCount + 1) / (float)terrainCount);
			yield return null;
		}
		foreach (TerrainTypeName terrainTypeName in terrainTypeNames.Data)
		{
			Diagnostics.Assert(terrainTypeName.TypeValue < 256);
			TerrainTypeMapping terrainTypeMapping2;
			if (terrainTypeMappingDatabase.TryGetValue(terrainTypeName.Value, out terrainTypeMapping2))
			{
				int currentTerrainTileId = (int)terrainTypeMapping2.TileAtlasIndex.Y * textureCountPerRowInAtlas + (int)terrainTypeMapping2.TileAtlasIndex.X;
				Diagnostics.Assert(currentTerrainTileId < 256);
				this.TerrainIdInGeometryToTerrainMappingId[(int)terrainTypeName.TypeValue] = (byte)currentTerrainTileId;
			}
			else
			{
				Diagnostics.LogWarning(string.Format("Unable to find terrain type {0} in Database", terrainTypeName.Value));
			}
			for (int j = 0; j < this.OneTerrainTypeGraphicDatas.Length; j++)
			{
				if (this.OneTerrainTypeGraphicDatas[j].TerrainTypeMapping.Name == terrainTypeName.Value)
				{
					this.terrainIdInGeometryToOneTerrainTypeGraphicDatas[(int)terrainTypeName.TypeValue] = (byte)j;
					break;
				}
			}
			if (this.terrainIdInGeometryToOneTerrainTypeGraphicDatas[(int)terrainTypeName.TypeValue] == 255)
			{
				Diagnostics.LogWarning(string.Format("Unable to find terrain type {0} in Database", terrainTypeName.Value));
			}
		}
		for (int k = 0; k < 256; k++)
		{
			int oneTerrainTypeGraphicDataIndex = (int)this.terrainIdInGeometryToOneTerrainTypeGraphicDatas[k];
			if (oneTerrainTypeGraphicDataIndex != 255)
			{
				this.TerrainIdInGeometryToSoundAmbianceNameIndex[k] = this.OneTerrainTypeGraphicDatas[oneTerrainTypeGraphicDataIndex].SoundAmbianceIndex;
				this.TerrainIdInGeometryToRoadIndex[k] = this.OneTerrainTypeGraphicDatas[oneTerrainTypeGraphicDataIndex].RoadIndex;
			}
		}
		if (verbose)
		{
			string concatenedSoundAmbianceNames = string.Empty;
			for (int l = 0; l < this.SoundAmbianceNames.Count; l++)
			{
				if (l > 0)
				{
					concatenedSoundAmbianceNames += ";";
				}
				concatenedSoundAmbianceNames += this.SoundAmbianceNames[l];
			}
			Diagnostics.Log("Found sound ambiances : [{0}]", new object[]
			{
				concatenedSoundAmbianceNames
			});
		}
		if (verbose)
		{
			string concatenedRoadNames = string.Empty;
			for (int m = 0; m < this.roadNames.Count; m++)
			{
				if (m > 0)
				{
					concatenedRoadNames += ";";
				}
				concatenedRoadNames += this.roadNames[m];
			}
			Diagnostics.Log("Found road : [{0}]", new object[]
			{
				concatenedRoadNames
			});
		}
		yield break;
	}

	private void CalculOccurenceExpectationAndSetMeshPerBatch(WorldController worldController)
	{
		GridMap<byte> gridMap = worldController.WorldAtlas.GetMap(WorldAtlas.Maps.Terrain) as GridMap<byte>;
		int[] array = new int[256];
		for (int i = 0; i < gridMap.Height; i++)
		{
			for (int j = 0; j < gridMap.Width; j++)
			{
				byte value = gridMap.GetValue(new WorldPosition(i, j));
				array[(int)value] = array[(int)value] + 1;
			}
		}
		double num = (double)gridMap.Height * (double)gridMap.Width;
		for (int k = 0; k < array.Length; k++)
		{
			HxTechniqueGraphicData.OneTerrainTypeGraphicData oneTerrainTypeGraphicDataFromGeometryIndex = this.GetOneTerrainTypeGraphicDataFromGeometryIndex(k);
			if (oneTerrainTypeGraphicDataFromGeometryIndex != null)
			{
				oneTerrainTypeGraphicDataFromGeometryIndex.SetProbability((double)array[k] / num, this.instanciedMeshHolders);
			}
		}
		for (int l = 0; l < this.instanciedMeshHolders.SmallMeshCount; l++)
		{
			double num2 = this.instanciedMeshHolders.OccurrenceExpectation(l);
			if (num2 > 0.0)
			{
				int val = (int)(num2 * 2500.0);
				int value2 = Math.Min(this.instanciedMeshHolders.MaxPerBigLineInstance(InstanciedMeshHelpers.PositionForwardScaleZPixelsPerInstance), Math.Max(8, val));
				this.instanciedMeshHolders.OverrideMaxPerBatchSmallMeshInstance(l, value2);
			}
		}
	}

	private void EvaluateMeshGenerationOptions()
	{
		this.meshGenerationPolicyOptionMasks = new int[this.meshGenerationPolicyOptions.Length];
		for (int i = 0; i < this.meshGenerationPolicyOptions.Length; i++)
		{
			int num = 0;
			string[] array = this.meshGenerationPolicyOptions[i].Split(new char[]
			{
				';'
			});
			for (int j = 0; j < array.Length; j++)
			{
				int num2 = 1;
				bool flag = false;
				for (int k = 0; k < InstanciedMeshHolders.MeshCreationOptions.Length; k++)
				{
					if (array[j].Equals(InstanciedMeshHolders.MeshCreationOptions[k]))
					{
						num |= num2;
						flag = true;
						break;
					}
					num2 *= 2;
				}
				if (!flag)
				{
					Diagnostics.LogWarning("Unknown options {0} for shader {1}", new object[]
					{
						array[j],
						this.meshGenerationPolicyShaders[i].name
					});
				}
			}
			this.meshGenerationPolicyOptionMasks[i] = num;
		}
	}

	private Dictionary<string, KeyValuePair<GameObject, bool>> stringToPrefabDico;

	private HxTechniqueGraphicData.OneAnomalyTypeGraphicData[] oneAnomalyTypeGraphicDatas;

	private List<StaticString> soundAmbianceNames;

	private List<string> roadNames;

	private InstanciedMeshHolders instanciedMeshHolders;

	private byte[] terrainIdInGeometryToOneTerrainTypeGraphicDatas;

	private byte[] anomalyIdInGeometryToOneAnomalyTypeGraphicDatas;

	private GameObject instanciedGameObjectFather;

	private HxTechniqueGraphicData.AllResourceFidsGraphicData allResourceFidsGraphicDatas;

	private HxTechniqueGraphicData.AllOrbGraphicData allOrbGraphicDatas;

	private HxTechniqueGraphicData.AllWeatherGraphicData allWeatherGraphicDatas;

	private HxTechniqueGraphicData.RegionNameGraphicData regionNameGraphicDatas;

	private HxTechniqueGraphicData.EdgeOfTheWorldData edgeOfTheWorldDatas;

	private HxTechniqueGraphicData.DebugGraphicData debugGraphicDatas;

	private HxTechniqueGraphicData.RoadGraphicData[] roadGraphicDatas;

	private HxTechniqueGraphicData.RoadGraphicData[] packedRoadGraphicDatas;

	private int[] meshGenerationPolicyOptionMasks;

	private bool verbose;

	private bool loaded;

	[SerializeField]
	private AgeFont regionNameFont;

	[SerializeField]
	private float regionTextSize;

	[SerializeField]
	private Material regionNameMaterial;

	[SerializeField]
	private string[] instancingPolicyMeshNames;

	[SerializeField]
	private int[] instancingPolicyMeshCount;

	[SerializeField]
	private Shader[] meshGenerationPolicyShaders;

	[SerializeField]
	private string[] meshGenerationPolicyOptions;

	[SerializeField]
	private AgeFont debugTextFont;

	[SerializeField]
	private float debugTextSize;

	[SerializeField]
	private Material debugTextMaterial;

	public class TradeRouteGraphicData
	{
		public IEnumerator Load(HxTechniqueGraphicData.GetOrCreatePrefabCopyDelegate getOrCreatePrefabCopyDelegate, DecorationPrefabData.GetOrCreateInstanciedMeshIndexDelegate getOrCreateInstanciedMeshIndexDelegate, DecorationPrefabData.RetrieveInstanciedMeshIndexDelegate retrieveInstanciedMeshIndexDelegate, GameObject fatherOfInstanciedGameObject, bool verbose)
		{
			Diagnostics.Progress.SetProgress(0f);
			string tradeRouteDotLinePrefabFormat = "Prefabs/TradeRoutes/TradeRoute_DotLine_00";
			string tradeRouteStartCityCenterPrefabFormat = "Prefabs/TradeRoutes/TradeRoute_StartCityCenter_00";
			string tradeRouteEndCityCenterPrefabFormat = "Prefabs/TradeRoutes/TradeRoute_EndCityCenter_00";
			Matrix4x4 defaultRotationMatrix = Matrix4x4.TRS(UnityEngine.Vector3.zero, Quaternion.Euler(new UnityEngine.Vector3(270f, 0f, 0f)), new UnityEngine.Vector3(1f, 1f, 1f));
			Matrix4x4 inverseDefaultRotationMatrix = Matrix4x4.TRS(UnityEngine.Vector3.zero, Quaternion.Euler(new UnityEngine.Vector3(-270f, 0f, 0f)), new UnityEngine.Vector3(1f, 1f, 1f));
			this.LoadDecorationPrefabData(ref this.StartCityCenter00, tradeRouteStartCityCenterPrefabFormat, defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreatePrefabCopyDelegate, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, fatherOfInstanciedGameObject, true, verbose);
			this.LoadDecorationPrefabData(ref this.EndCityCenter00, tradeRouteEndCityCenterPrefabFormat, defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreatePrefabCopyDelegate, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, fatherOfInstanciedGameObject, true, verbose);
			this.LoadDecorationPrefabData(ref this.DotLine00, tradeRouteDotLinePrefabFormat, defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreatePrefabCopyDelegate, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, fatherOfInstanciedGameObject, true, verbose);
			IDatabase<GuiTradeRouteIncomesDefinition> tradeRouteIncomesDefinitionDatatable = Databases.GetDatabase<GuiTradeRouteIncomesDefinition>(false);
			Diagnostics.Assert(tradeRouteIncomesDefinitionDatatable != null);
			IDatabase<GuiElement> guiElementDatabase = Databases.GetDatabase<GuiElement>(false);
			Diagnostics.Assert(guiElementDatabase != null);
			GuiTradeRouteIncomesDefinition tradeRouteIncomesDefinition = tradeRouteIncomesDefinitionDatatable.GetValue("OneTradeRoute");
			Diagnostics.Assert(tradeRouteIncomesDefinition != null);
			this.TradeRouteIncomePropertyNames = (from tradeRouteIncome in tradeRouteIncomesDefinition.TradeRouteIncomes
			select tradeRouteIncome.Value).ToArray<StaticString>();
			this.FidsIncomePrefabData = new DecorationPrefabData[this.TradeRouteIncomePropertyNames.Length];
			this.NegativeFidsIncomePrefabData = new DecorationPrefabData[this.TradeRouteIncomePropertyNames.Length];
			for (int i = 0; i < this.TradeRouteIncomePropertyNames.Length; i++)
			{
				GuiElement guiElement = guiElementDatabase.GetValue(this.TradeRouteIncomePropertyNames[i]);
				ExtendedGuiElement extendedGuiElement = guiElement as ExtendedGuiElement;
				Color color = Color.red;
				if (extendedGuiElement != null)
				{
					color = extendedGuiElement.Color;
				}
				string incomePrefabDataPrefabName = string.Format("Prefabs/TradeRoutes/{0}/Fids", this.TradeRouteIncomePropertyNames[i]);
				string negativeIncomePrefabDataPrefabName = string.Format("Prefabs/TradeRoutes/{0}/NegativeFids", this.TradeRouteIncomePropertyNames[i]);
				if (verbose)
				{
					Color32 color2 = color;
					Diagnostics.Log("Loading trade routes fids prefab {0}, color( {1}, {2}, {3} )", new object[]
					{
						incomePrefabDataPrefabName,
						color2.r,
						color2.g,
						color2.b
					});
				}
				GameObject prefab = getOrCreatePrefabCopyDelegate(incomePrefabDataPrefabName, false);
				if (prefab != null)
				{
					this.ModifyPrefabMaterialColor("_ResourceColor", color, prefab);
					this.FidsIncomePrefabData[i] = new DecorationPrefabData(0, incomePrefabDataPrefabName, prefab, defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, string.Empty, null, fatherOfInstanciedGameObject, verbose, 0);
				}
				else
				{
					Diagnostics.LogWarning("Unable to load prefab {0}", new object[]
					{
						incomePrefabDataPrefabName
					});
				}
				GameObject negativePrefab = getOrCreatePrefabCopyDelegate(negativeIncomePrefabDataPrefabName, false);
				if (negativePrefab != null)
				{
					this.ModifyPrefabMaterialColor("_ResourceColor", color, negativePrefab);
					this.NegativeFidsIncomePrefabData[i] = new DecorationPrefabData(0, negativeIncomePrefabDataPrefabName, negativePrefab, defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, string.Empty, null, fatherOfInstanciedGameObject, verbose, 0);
				}
				else
				{
					if (verbose)
					{
						Diagnostics.Log("Unable to load optional prefab {0} for negative income using same than for positive income", new object[]
						{
							negativeIncomePrefabDataPrefabName
						});
					}
					this.NegativeFidsIncomePrefabData[i] = this.FidsIncomePrefabData[i];
				}
				Diagnostics.Progress.SetProgress((float)(i + 1) / (float)(this.TradeRouteIncomePropertyNames.Length + 1));
				yield return null;
			}
			yield return null;
			yield break;
		}

		public void ForgetDataNotUsedInMainRender()
		{
			this.DotLine00.ForgetDataNotUsedInMainRender();
			this.StartCityCenter00.ForgetDataNotUsedInMainRender();
			this.EndCityCenter00.ForgetDataNotUsedInMainRender();
			for (int i = 0; i < ((this.FidsIncomePrefabData == null) ? 0 : this.FidsIncomePrefabData.Length); i++)
			{
				this.FidsIncomePrefabData[i].ForgetDataNotUsedInMainRender();
			}
		}

		public void Unload()
		{
		}

		private void LoadDecorationPrefabData(ref DecorationPrefabData decorationPrefabData, string prefabName, Matrix4x4 defaultRotationMatrix, Matrix4x4 inverseDefaultRotationMatrix, HxTechniqueGraphicData.GetOrCreatePrefabCopyDelegate getOrCreatePrefabCopyDelegate, DecorationPrefabData.GetOrCreateInstanciedMeshIndexDelegate getOrCreateInstanciedMeshIndexDelegate, DecorationPrefabData.RetrieveInstanciedMeshIndexDelegate retrieveInstanciedMeshIndexDelegate, GameObject fatherOfInstanciedGameObject, bool warnIfNoPrefabFound, bool verbose)
		{
			GameObject gameObject = getOrCreatePrefabCopyDelegate(prefabName, false);
			if (gameObject != null)
			{
				decorationPrefabData = new DecorationPrefabData(0, prefabName, gameObject, defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, string.Empty, null, fatherOfInstanciedGameObject, verbose, 0);
			}
			else if (warnIfNoPrefabFound)
			{
				Diagnostics.LogWarning("Unable to load prefab {0}", new object[]
				{
					prefabName
				});
			}
		}

		private void ModifyPrefabMaterialColor(string resourcePropertyName, Color fidsColor, GameObject prefab)
		{
			Renderer[] componentsInChildren = prefab.GetComponentsInChildren<Renderer>(true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				for (int j = 0; j < componentsInChildren[i].sharedMaterials.Length; j++)
				{
					Material material = componentsInChildren[i].sharedMaterials[j];
					if (material == null)
					{
						Diagnostics.LogWarning("prefab {0} contains a renderer with a null material", new object[]
						{
							prefab.name
						});
					}
					else if (material.HasProperty(resourcePropertyName))
					{
						material.SetColor(resourcePropertyName, fidsColor);
					}
				}
			}
		}

		public DecorationPrefabData DotLine00;

		public DecorationPrefabData StartCityCenter00;

		public DecorationPrefabData EndCityCenter00;

		public StaticString[] TradeRouteIncomePropertyNames;

		public DecorationPrefabData[] FidsIncomePrefabData;

		public DecorationPrefabData[] NegativeFidsIncomePrefabData;
	}

	public class RoadGraphicData
	{
		public IEnumerator Load(string roadDecorationPrefabFormat, HxTechniqueGraphicData.GetOrCreatePrefabCopyDelegate getOrCreatePrefabCopyDelegate, DecorationPrefabData.GetOrCreateInstanciedMeshIndexDelegate getOrCreateInstanciedMeshIndexDelegate, DecorationPrefabData.RetrieveInstanciedMeshIndexDelegate retrieveInstanciedMeshIndexDelegate, GameObject fatherOfInstanciedGameObject, bool verbose)
		{
			Matrix4x4 defaultRotationMatrix = Matrix4x4.TRS(UnityEngine.Vector3.zero, Quaternion.Euler(new UnityEngine.Vector3(270f, 0f, 0f)), new UnityEngine.Vector3(1f, 1f, 1f));
			Matrix4x4 inverseDefaultRotationMatrix = Matrix4x4.TRS(UnityEngine.Vector3.zero, Quaternion.Euler(new UnityEngine.Vector3(-270f, 0f, 0f)), new UnityEngine.Vector3(1f, 1f, 1f));
			yield return null;
			this.LoadDecorationPrefabData(ref this.Road60, string.Format(roadDecorationPrefabFormat, "Road_60"), defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreatePrefabCopyDelegate, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, fatherOfInstanciedGameObject, true, verbose, 0, 8);
			this.LoadDecorationPrefabData(ref this.Road120, string.Format(roadDecorationPrefabFormat, "Road_120"), defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreatePrefabCopyDelegate, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, fatherOfInstanciedGameObject, true, verbose, 1, 8);
			yield return null;
			this.LoadDecorationPrefabData(ref this.Road180, string.Format(roadDecorationPrefabFormat, "Road_180"), defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreatePrefabCopyDelegate, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, fatherOfInstanciedGameObject, true, verbose, 2, 8);
			this.LoadDecorationPrefabData(ref this.Cross120And180, string.Format(roadDecorationPrefabFormat, "Road_120_180"), defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreatePrefabCopyDelegate, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, fatherOfInstanciedGameObject, false, verbose, 3, 8);
			yield return null;
			this.LoadDecorationPrefabData(ref this.Cross120And240, string.Format(roadDecorationPrefabFormat, "Road_120_240"), defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreatePrefabCopyDelegate, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, fatherOfInstanciedGameObject, false, verbose, 4, 8);
			this.LoadDecorationPrefabData(ref this.Cross180And240, string.Format(roadDecorationPrefabFormat, "Road_180_240"), defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreatePrefabCopyDelegate, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, fatherOfInstanciedGameObject, false, verbose, 5, 8);
			yield return null;
			this.LoadDecorationPrefabData(ref this.HalfRoad, string.Format(roadDecorationPrefabFormat, "Road_Half"), defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreatePrefabCopyDelegate, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, fatherOfInstanciedGameObject, true, verbose, 6, 8);
			this.LoadDecorationPrefabData(ref this.RoundAbout, string.Format(roadDecorationPrefabFormat, "Road_RoundAbout"), defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreatePrefabCopyDelegate, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, fatherOfInstanciedGameObject, true, verbose, 7, 8);
			yield return null;
			yield break;
		}

		public void ForgetDataNotUsedInMainRender()
		{
			this.Road60.ForgetDataNotUsedInMainRender();
			this.Road120.ForgetDataNotUsedInMainRender();
			this.Road180.ForgetDataNotUsedInMainRender();
			this.Cross120And180.ForgetDataNotUsedInMainRender();
			this.Cross120And240.ForgetDataNotUsedInMainRender();
			this.Cross180And240.ForgetDataNotUsedInMainRender();
			this.HalfRoad.ForgetDataNotUsedInMainRender();
			this.RoundAbout.ForgetDataNotUsedInMainRender();
		}

		public void Unload()
		{
		}

		private void LoadDecorationPrefabData(ref DecorationPrefabData decorationPrefabData, string prefabName, Matrix4x4 defaultRotationMatrix, Matrix4x4 inverseDefaultRotationMatrix, HxTechniqueGraphicData.GetOrCreatePrefabCopyDelegate getOrCreatePrefabCopyDelegate, DecorationPrefabData.GetOrCreateInstanciedMeshIndexDelegate getOrCreateInstanciedMeshIndexDelegate, DecorationPrefabData.RetrieveInstanciedMeshIndexDelegate retrieveInstanciedMeshIndexDelegate, GameObject fatherOfInstanciedGameObject, bool warnIfNoPrefabFound, bool verbose, int index, int count)
		{
			GameObject gameObject = getOrCreatePrefabCopyDelegate(prefabName, false);
			if (gameObject != null)
			{
				decorationPrefabData = new DecorationPrefabData(0, prefabName, gameObject, defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, string.Empty, null, fatherOfInstanciedGameObject, verbose, 0);
			}
			else if (warnIfNoPrefabFound)
			{
				Diagnostics.LogWarning("Unable to load prefab {0}", new object[]
				{
					prefabName
				});
			}
		}

		public DecorationPrefabData Road60;

		public DecorationPrefabData Road120;

		public DecorationPrefabData Road180;

		public DecorationPrefabData Cross120And180;

		public DecorationPrefabData Cross120And240;

		public DecorationPrefabData Cross180And240;

		public DecorationPrefabData HalfRoad;

		public DecorationPrefabData RoundAbout;
	}

	public class RegionNameGraphicData
	{
		public RegionNameGraphicData(WorldController worldController, AgeFont ageFont, Material material, float textSize)
		{
			Diagnostics.Assert(ageFont != null);
			Diagnostics.Assert(material != null);
			this.AgeFont = ageFont;
			this.Material = material;
			this.TextSize = textSize / (float)ageFont.FontSize;
			Texture texture = ageFont.Material.GetTexture("_MainTex");
			this.FontTextureWidth = texture.width;
			this.FontTextureHeight = texture.height;
			this.Material.SetTexture("_MainTex", texture);
			this.BindDynamicTextureAtlas(this.Material);
			this.revision = 0;
			this.InitRegionNamePosition(worldController);
		}

		public int Revision
		{
			get
			{
				return this.revision;
			}
		}

		public int SoftwareRasterAtlasRevisionIndex
		{
			get
			{
				return AgeManager.Instance.FontAtlasRenderer.SoftwareRasterAtlasRevisionIndex;
			}
		}

		public HxTechniqueGraphicData.RegionNameGraphicData.RegionData[] RegionDatas
		{
			get
			{
				return this.regionDatas;
			}
		}

		public void IncRevision()
		{
			this.revision++;
		}

		public void BindDynamicTextureAtlas(Material material)
		{
			FontAtlasRenderer fontAtlasRenderer = AgeManager.Instance.FontAtlasRenderer;
			Texture texture = fontAtlasRenderer.Texture();
			if (texture != null)
			{
				material.SetTexture("_DynamicAtlasTex", texture);
			}
			else
			{
				Diagnostics.Assert(false);
			}
		}

		private void InitRegionNamePosition(WorldController worldController)
		{
			Map<Region> map = worldController.WorldAtlas.GetMap(WorldAtlas.Tables.Regions) as Map<Region>;
			GridMap<short> gridMap = worldController.WorldAtlas.GetMap(WorldAtlas.Maps.Regions) as GridMap<short>;
			int num = map.Data.Length;
			this.regionDatas = new HxTechniqueGraphicData.RegionNameGraphicData.RegionData[num];
			for (int i = 0; i < map.Data.Length; i++)
			{
				Region region = map.Data[i];
				bool showName = true;
				if (region.IsWasteland)
				{
					showName = false;
				}
				else if (region.IsOcean)
				{
					showName = (region.PointOfInterests != null && region.PointOfInterests.Length > 0);
				}
				this.regionDatas[i] = new HxTechniqueGraphicData.RegionNameGraphicData.RegionData(region.VisualCenter, showName);
			}
			this.ComputeRegionVisualCenters(gridMap, map);
			for (int j = 0; j < this.regionDatas.Length; j++)
			{
				HxTechniqueGraphicData.RegionNameGraphicData.RegionData regionData = this.regionDatas[j];
				Diagnostics.Assert(regionData.Center.IsValid);
				Diagnostics.Assert((int)gridMap.GetValue(regionData.Center) == j);
			}
		}

		private void ComputeRegionVisualCenters(GridMap<short> regionsMap, Map<Region> regionsTable)
		{
			float[] array = new float[this.regionDatas.Length];
			for (int i = 0; i < this.regionDatas.Length; i++)
			{
				Region region = regionsTable.Data[i];
				if (region.VisualCenter.Row < 0 || (int)region.VisualCenter.Row >= regionsMap.Height || region.VisualCenter.Column < 0 || (int)region.VisualCenter.Column >= regionsMap.Width || (int)regionsMap.GetValue(region.VisualCenter) != i)
				{
					array[i] = 0f;
				}
				else
				{
					array[i] = float.MaxValue;
				}
			}
			int num = regionsMap.Width / 2;
			for (int j = 0; j < regionsMap.Height; j++)
			{
				for (int k = 0; k < regionsMap.Width; k++)
				{
					WorldPosition worldPosition = new WorldPosition(j, k);
					short value = regionsMap.GetValue(worldPosition);
					float num2 = array[(int)value];
					if (num2 != 3.40282347E+38f)
					{
						UnityEngine.Vector3 absoluteWorldPosition2D = AbstractGlobalPositionning.GetAbsoluteWorldPosition2D((int)worldPosition.Row, (int)worldPosition.Column);
						float num3 = float.MaxValue;
						Region region2 = regionsTable.Data[(int)value];
						for (int l = 0; l < region2.Borders.Length; l++)
						{
							Region.Border border = region2.Borders[l];
							for (int m = 0; m < border.WorldPositions.Length; m++)
							{
								WorldPosition worldPosition2 = border.WorldPositions[m];
								int num4 = (int)worldPosition2.Column;
								if (num4 > (int)worldPosition.Column + num)
								{
									num4 -= regionsMap.Width;
								}
								else if (num4 < (int)worldPosition.Column - num)
								{
									num4 += regionsMap.Width;
								}
								UnityEngine.Vector3 absoluteWorldPosition2D2 = AbstractGlobalPositionning.GetAbsoluteWorldPosition2D((int)worldPosition2.Row, num4);
								float sqrMagnitude = (absoluteWorldPosition2D2 - absoluteWorldPosition2D).sqrMagnitude;
								num3 = Math.Min(num3, sqrMagnitude);
							}
							if (num3 < num2)
							{
								break;
							}
						}
						if (num3 >= num2)
						{
							num2 = num3;
							array[(int)value] = num2;
							this.regionDatas[(int)value].Center = worldPosition;
						}
					}
				}
			}
		}

		public readonly AgeFont AgeFont;

		public readonly Material Material;

		public readonly int FontTextureWidth;

		public readonly int FontTextureHeight;

		public readonly float TextSize;

		private HxTechniqueGraphicData.RegionNameGraphicData.RegionData[] regionDatas;

		private int revision;

		public struct RegionData
		{
			public RegionData(WorldPosition center, bool showName)
			{
				this.Center = center;
				this.ShowName = showName;
			}

			public WorldPosition Center;

			public bool ShowName;
		}
	}

	public class EdgeOfTheWorldData
	{
		public IEnumerator Load(HxTechniqueGraphicData.GetOrCreatePrefabCopyDelegate getOrCreatePrefabCopyDelegate, DecorationPrefabData.GetOrCreateInstanciedMeshIndexDelegate getOrCreateInstanciedMeshIndexDelegate, DecorationPrefabData.RetrieveInstanciedMeshIndexDelegate retrieveInstanciedMeshIndexDelegate, GameObject fatherOfInstanciedGameObject, bool verbose)
		{
			yield return null;
			List<DecorationPrefabData> decorationPrefabDataList = new List<DecorationPrefabData>();
			HxTechniqueGraphicData.EdgeOfTheWorldData.FillDecorationPrefabDatas(decorationPrefabDataList, "Prefabs/Environments/EdgesOfTheWorld/Patch/EdgesOfTheWorld_Patch_North_{0}", getOrCreatePrefabCopyDelegate, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, fatherOfInstanciedGameObject, verbose);
			this.northDecorationPrefabDatas = decorationPrefabDataList.ToArray();
			decorationPrefabDataList.Clear();
			HxTechniqueGraphicData.EdgeOfTheWorldData.FillDecorationPrefabDatas(decorationPrefabDataList, "Prefabs/Environments/EdgesOfTheWorld/Patch/EdgesOfTheWorld_Patch_EastWest_{0}", getOrCreatePrefabCopyDelegate, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, fatherOfInstanciedGameObject, verbose);
			this.eastWestDecorationPrefabDatas = decorationPrefabDataList.ToArray();
			yield return null;
			yield break;
		}

		public void ForgetDataNotUsedInMainRender()
		{
			DecorationPrefabData.ForgetDataNotUsedInMainRender(this.northDecorationPrefabDatas);
			DecorationPrefabData.ForgetDataNotUsedInMainRender(this.eastWestDecorationPrefabDatas);
		}

		public void Unload()
		{
		}

		public DecorationPrefabData GetNorthPrefabData(int rowOrColIndex)
		{
			if (rowOrColIndex >= 0)
			{
				return this.northDecorationPrefabDatas[rowOrColIndex % this.northDecorationPrefabDatas.Length];
			}
			return this.northDecorationPrefabDatas[this.northDecorationPrefabDatas.Length - 1 + rowOrColIndex % this.northDecorationPrefabDatas.Length];
		}

		public DecorationPrefabData GetEastWestPrefabData(int rowOrColIndex)
		{
			if (rowOrColIndex >= 0)
			{
				return this.eastWestDecorationPrefabDatas[rowOrColIndex % this.eastWestDecorationPrefabDatas.Length];
			}
			return this.eastWestDecorationPrefabDatas[this.eastWestDecorationPrefabDatas.Length - 1 + rowOrColIndex % this.eastWestDecorationPrefabDatas.Length];
		}

		private static void FillDecorationPrefabDatas(List<DecorationPrefabData> decorationPrefabDataList, string prefabFormat, HxTechniqueGraphicData.GetOrCreatePrefabCopyDelegate getOrCreatePrefabCopyDelegate, DecorationPrefabData.GetOrCreateInstanciedMeshIndexDelegate getOrCreateInstanciedMeshIndexDelegate, DecorationPrefabData.RetrieveInstanciedMeshIndexDelegate retrieveInstanciedMeshIndexDelegate, GameObject fatherOfInstanciedGameObject, bool verbose)
		{
			int num = 10;
			Matrix4x4 instanciedMeshBaseTransformationMatrix = Matrix4x4.TRS(UnityEngine.Vector3.zero, Quaternion.Euler(new UnityEngine.Vector3(270f, 0f, 0f)), new UnityEngine.Vector3(1f, 1f, 1f));
			Matrix4x4 inverseInstanciedMeshBaseTransformationMatrix = Matrix4x4.TRS(UnityEngine.Vector3.zero, Quaternion.Euler(new UnityEngine.Vector3(-270f, 0f, 0f)), new UnityEngine.Vector3(1f, 1f, 1f));
			for (int i = 0; i < num; i++)
			{
				string text = string.Format(prefabFormat, (i + 1).ToString("00"));
				GameObject gameObject = getOrCreatePrefabCopyDelegate(text, false);
				if (gameObject == null)
				{
					break;
				}
				DecorationPrefabData item = new DecorationPrefabData(0, text, gameObject, instanciedMeshBaseTransformationMatrix, inverseInstanciedMeshBaseTransformationMatrix, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, string.Empty, null, fatherOfInstanciedGameObject, verbose, 0);
				decorationPrefabDataList.Add(item);
			}
		}

		private DecorationPrefabData[] northDecorationPrefabDatas;

		private DecorationPrefabData[] eastWestDecorationPrefabDatas;
	}

	public class DebugGraphicData
	{
		public DebugGraphicData(WorldController worldController, AgeFont ageFont, Material material, float textSize)
		{
			Diagnostics.Assert(ageFont != null);
			Diagnostics.Assert(material != null);
			this.AgeFont = ageFont;
			this.Material = material;
			this.TextSize = textSize / (float)ageFont.FontSize;
			Texture texture = ageFont.Material.GetTexture("_MainTex");
			this.FontTextureWidth = texture.width;
			this.FontTextureHeight = texture.height;
			this.Material.SetTexture("_MainTex", texture);
		}

		public readonly AgeFont AgeFont;

		public readonly Material Material;

		public readonly int FontTextureWidth;

		public readonly int FontTextureHeight;

		public readonly float TextSize;
	}

	public class OneAnomalyTypeGraphicData
	{
		public OneAnomalyTypeGraphicData(AnomalyTypeMapping anomalyTypeMapping, HxTechniqueGraphicData.GetOrCreatePrefabCopyDelegate getOrCreatePrefabCopyDelegate, DecorationPrefabData.GetOrCreateInstanciedMeshIndexDelegate getOrCreateInstanciedMeshIndexDelegate, DecorationPrefabData.RetrieveInstanciedMeshIndexDelegate retrieveInstanciedMeshIndexDelegate, DecorationPrefabData.AddSmallMeshesDelegate addSmallMeshesDelegate, GameObject fatherOfInstanciedGameObject, bool verbose)
		{
			this.anomalyTypeName = anomalyTypeMapping.Name;
			if (anomalyTypeMapping.Layers != null)
			{
				for (int i = 0; i < anomalyTypeMapping.Layers.Length; i++)
				{
					if (!(anomalyTypeMapping.Layers[i].Name != HxTechniqueGraphicData.OneAnomalyTypeGraphicData.LayerName))
					{
						if (!(anomalyTypeMapping.Layers[i].Type != HxTechniqueGraphicData.OneAnomalyTypeGraphicData.LayerType))
						{
							this.AddPrefabDataFromLayer(anomalyTypeMapping.Layers[i], getOrCreatePrefabCopyDelegate, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, addSmallMeshesDelegate, fatherOfInstanciedGameObject, verbose);
						}
					}
				}
			}
		}

		public string AnomalyTypeName
		{
			get
			{
				return this.anomalyTypeName;
			}
		}

		public void ForgetDataNotUsedInMainRender()
		{
			DecorationPrefabData.ForgetDataNotUsedInMainRender(this.decorationPrefabDatas);
		}

		public void Unload()
		{
		}

		public DecorationPrefabData GetPrefabData(int randomValue)
		{
			return this.decorationPrefabDatas[DecorationPrefabData.GetPrefabDataIndex(randomValue, this.decorationPrefabDatas, this.decorationWeigthSum)];
		}

		private void AddPrefabDataFromLayer(SimulationLayer layer, HxTechniqueGraphicData.GetOrCreatePrefabCopyDelegate getOrCreatePrefabCopyDelegate, DecorationPrefabData.GetOrCreateInstanciedMeshIndexDelegate getOrCreateInstanciedMeshIndexDelegate, DecorationPrefabData.RetrieveInstanciedMeshIndexDelegate retrieveInstanciedMeshIndexDelegate, DecorationPrefabData.AddSmallMeshesDelegate addSmallMeshesDelegate, GameObject fatherOfInstanciedGameObject, bool verbose)
		{
			if (layer.Samples == null || layer.Samples.Length == 0)
			{
				return;
			}
			this.decorationPrefabDatas = new DecorationPrefabData[layer.Samples.Length];
			Matrix4x4 instanciedMeshBaseTransformationMatrix = Matrix4x4.TRS(UnityEngine.Vector3.zero, Quaternion.Euler(new UnityEngine.Vector3(270f, 0f, 0f)), new UnityEngine.Vector3(1f, 1f, 1f));
			Matrix4x4 inverseInstanciedMeshBaseTransformationMatrix = Matrix4x4.TRS(UnityEngine.Vector3.zero, Quaternion.Euler(new UnityEngine.Vector3(-270f, 0f, 0f)), new UnityEngine.Vector3(1f, 1f, 1f));
			for (int i = 0; i < layer.Samples.Length; i++)
			{
				SimulationLayer.Sample sample = layer.Samples[i];
				this.decorationWeigthSum += sample.Weight;
				this.decorationPrefabDatas[i] = new DecorationPrefabData(sample.Weight, sample.Value, getOrCreatePrefabCopyDelegate(sample.Value, false), instanciedMeshBaseTransformationMatrix, inverseInstanciedMeshBaseTransformationMatrix, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, sample.Value, addSmallMeshesDelegate, fatherOfInstanciedGameObject, verbose, 2);
			}
			if (this.decorationWeigthSum == 0)
			{
				this.decorationWeigthSum = 1;
			}
		}

		private static readonly StaticString LayerName = new StaticString("Anomalies");

		private static readonly StaticString LayerType = new StaticString("Geometry");

		private DecorationPrefabData[] decorationPrefabDatas;

		private int decorationWeigthSum;

		private string anomalyTypeName;
	}

	public class OneTerrainTypeGraphicData
	{
		public OneTerrainTypeGraphicData(TerrainTypeMapping terrainTypeMapping, HxTechniqueGraphicData.GetOrCreatePrefabCopyDelegate getOrCreatePrefabCopyDelegate, DecorationPrefabData.GetOrCreateInstanciedMeshIndexDelegate getOrCreateInstanciedMeshIndexDelegate, DecorationPrefabData.RetrieveInstanciedMeshIndexDelegate retrieveInstanciedMeshIndexDelegate, GameObject fatherOfInstanciedGameObject, List<StaticString> soundAmbianceNames, List<string> roadNames, bool verbose)
		{
			this.TerrainTypeMapping = terrainTypeMapping;
			List<GameObject> prefabs = new List<GameObject>();
			List<int> weights = new List<int>();
			Matrix4x4 defaultRotationMatrix = Matrix4x4.TRS(UnityEngine.Vector3.zero, Quaternion.Euler(new UnityEngine.Vector3(270f, 0f, 0f)), new UnityEngine.Vector3(1f, 1f, 1f));
			Matrix4x4 inverseDefaultRotationMatrix = Matrix4x4.TRS(UnityEngine.Vector3.zero, Quaternion.Euler(new UnityEngine.Vector3(-270f, 0f, 0f)), new UnityEngine.Vector3(1f, 1f, 1f));
			this.FillDecorationPrefabData(prefabs, weights, HxTechniqueGraphicData.OneTerrainTypeGraphicData.GeometryTypeMappingLayerType, HxTechniqueGraphicData.OneTerrainTypeGraphicData.CliffDefaultSizeTypeMappingLayerName, getOrCreatePrefabCopyDelegate);
			HxTechniqueGraphicData.OneTerrainTypeGraphicData.FillPrefabDataArray(ref this.defaultSizeCliffPrefabData, ref this.defaultSizeCliffPrefabDataWeigthSum, prefabs, weights, defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, fatherOfInstanciedGameObject, true, true, terrainTypeMapping.Name, "Default Cliff", verbose);
			this.FillDecorationPrefabData(prefabs, weights, HxTechniqueGraphicData.OneTerrainTypeGraphicData.GeometryTypeMappingLayerType, HxTechniqueGraphicData.OneTerrainTypeGraphicData.CliffSmallSizeTypeMappingLayerName, getOrCreatePrefabCopyDelegate);
			HxTechniqueGraphicData.OneTerrainTypeGraphicData.FillPrefabDataArray(ref this.smallSizeCliffPrefabData, ref this.smallSizeCliffPrefabDataWeightSum, prefabs, weights, defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, fatherOfInstanciedGameObject, true, true, terrainTypeMapping.Name, "Small Cliff", verbose);
			this.FillDecorationPrefabData(prefabs, weights, HxTechniqueGraphicData.OneTerrainTypeGraphicData.GeometryTypeMappingLayerType, HxTechniqueGraphicData.OneTerrainTypeGraphicData.CliffDefaultSize2DTypeMappingLayerName, getOrCreatePrefabCopyDelegate);
			HxTechniqueGraphicData.OneTerrainTypeGraphicData.FillPrefabDataArray(ref this.defaultSizeCliff2DPrefabData, ref this.defaultSizeCliff2DPrefabDataWeigthSum, prefabs, weights, defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, fatherOfInstanciedGameObject, true, true, terrainTypeMapping.Name, "Default Cliff 2D", verbose);
			this.FillDecorationPrefabData(prefabs, weights, HxTechniqueGraphicData.OneTerrainTypeGraphicData.GeometryTypeMappingLayerType, HxTechniqueGraphicData.OneTerrainTypeGraphicData.CliffSmallSize2DTypeMappingLayerName, getOrCreatePrefabCopyDelegate);
			HxTechniqueGraphicData.OneTerrainTypeGraphicData.FillPrefabDataArray(ref this.smallSizeCliff2DPrefabData, ref this.smallSizeCliff2DPrefabDataWeightSum, prefabs, weights, defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, fatherOfInstanciedGameObject, true, true, terrainTypeMapping.Name, "Small Cliff 2D", verbose);
			this.FillDecorationPrefabData(prefabs, weights, HxTechniqueGraphicData.OneTerrainTypeGraphicData.GeometryTypeMappingLayerType, HxTechniqueGraphicData.OneTerrainTypeGraphicData.VegetationTypeMappingLayerName, getOrCreatePrefabCopyDelegate);
			HxTechniqueGraphicData.OneTerrainTypeGraphicData.FillPrefabDataArray(ref this.vegetationDecorationPrefabData, ref this.vegetationDecorationPrefabDataWeightSum, prefabs, weights, defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, fatherOfInstanciedGameObject, false, false, terrainTypeMapping.Name, "Vegetation", verbose);
			this.FillDecorationPrefabData(prefabs, weights, HxTechniqueGraphicData.OneTerrainTypeGraphicData.GeometryTypeMappingLayerType, HxTechniqueGraphicData.OneTerrainTypeGraphicData.EdgesOfTheWorldTypeMappingLayerName, getOrCreatePrefabCopyDelegate);
			HxTechniqueGraphicData.OneTerrainTypeGraphicData.FillPrefabDataArray(ref this.edgesOfTheWorldDecorationPrefabData, ref this.edgesOfTheWorldDecorationPrefabDataWeightSum, prefabs, weights, defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, fatherOfInstanciedGameObject, false, true, terrainTypeMapping.Name, "EdgeOfTheWorlds", verbose);
			this.FillDecorationPrefabData(prefabs, weights, HxTechniqueGraphicData.OneTerrainTypeGraphicData.GeometryTypeMappingLayerType, HxTechniqueGraphicData.OneTerrainTypeGraphicData.RidgeTypeMappingLayerName, getOrCreatePrefabCopyDelegate);
			HxTechniqueGraphicData.OneTerrainTypeGraphicData.FillPrefabDataArray(ref this.rigdePrefabData, ref this.rigdePrefabDataWeigthSum, prefabs, weights, defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, fatherOfInstanciedGameObject, false, false, terrainTypeMapping.Name, "Ridge", verbose);
			this.EvaluatePickPolicy("PickPolicy", "Ground", "GroundNoStair", "Water", "Underwater");
			this.EvaluateTerrainTypeCategory("Category", "Ground", "InlandWater", "Ocean");
			this.EvaluateSoundAmbiance("SoundAmbiance", soundAmbianceNames);
			this.EvaluateRoadName("RoadBiome", roadNames);
		}

		public TerrainTypeMapping TerrainTypeMapping { get; private set; }

		public int PickMask { get; private set; }

		public int IncludePickMask { get; private set; }

		public float PickHeightTolerance { get; private set; }

		public HxTechniqueGraphicData.OneTerrainTypeGraphicData.TerrainTypeCategory Category
		{
			get
			{
				return this.category;
			}
		}

		public StaticString SoundAmbianceName
		{
			get
			{
				return this.soundAmbianceName;
			}
		}

		public byte SoundAmbianceIndex
		{
			get
			{
				return this.soundAmbianceIndex;
			}
		}

		public string RoadName
		{
			get
			{
				return this.roadName;
			}
		}

		public byte RoadIndex
		{
			get
			{
				return this.roadIndex;
			}
		}

		public void SetProbability(double probability, InstanciedMeshHolders instanciedMeshHolders)
		{
			this.probability = probability;
			HxTechniqueGraphicData.OneTerrainTypeGraphicData.SetProbabilityOnDecorationPrefabs(probability, instanciedMeshHolders, this.vegetationDecorationPrefabData, (float)this.vegetationDecorationPrefabDataWeightSum);
			HxTechniqueGraphicData.OneTerrainTypeGraphicData.SetProbabilityOnDecorationPrefabs(probability, instanciedMeshHolders, this.edgesOfTheWorldDecorationPrefabData, (float)this.edgesOfTheWorldDecorationPrefabDataWeightSum);
		}

		public void LogContent(TextWriter writer)
		{
			writer.WriteLine(string.Format("{0};{1}", this.TerrainTypeMapping.Name, this.probability));
		}

		public void ForgetDataNotUsedInMainRender()
		{
			DecorationPrefabData.ForgetDataNotUsedInMainRender(this.defaultSizeCliffPrefabData);
			DecorationPrefabData.ForgetDataNotUsedInMainRender(this.smallSizeCliffPrefabData);
			DecorationPrefabData.ForgetDataNotUsedInMainRender(this.defaultSizeCliff2DPrefabData);
			DecorationPrefabData.ForgetDataNotUsedInMainRender(this.smallSizeCliff2DPrefabData);
			DecorationPrefabData.ForgetDataNotUsedInMainRender(this.vegetationDecorationPrefabData);
			DecorationPrefabData.ForgetDataNotUsedInMainRender(this.edgesOfTheWorldDecorationPrefabData);
			DecorationPrefabData.ForgetDataNotUsedInMainRender(this.rigdePrefabData);
		}

		public void Unload()
		{
			this.defaultSizeCliffPrefabData = null;
			this.defaultSizeCliffPrefabDataWeigthSum = 0;
			this.smallSizeCliffPrefabData = null;
			this.smallSizeCliffPrefabDataWeightSum = 0;
			this.defaultSizeCliff2DPrefabData = null;
			this.defaultSizeCliff2DPrefabDataWeigthSum = 0;
			this.smallSizeCliff2DPrefabData = null;
			this.smallSizeCliff2DPrefabDataWeightSum = 0;
			this.vegetationDecorationPrefabData = null;
			this.vegetationDecorationPrefabDataWeightSum = 0;
			this.edgesOfTheWorldDecorationPrefabData = null;
			this.edgesOfTheWorldDecorationPrefabDataWeightSum = 0;
			this.rigdePrefabData = null;
			this.rigdePrefabDataWeigthSum = 0;
		}

		public DecorationPrefabData GetCliffPrefabData(int randomValue, bool smallCliff, bool cliff2D)
		{
			if (cliff2D)
			{
				if (smallCliff)
				{
					return this.smallSizeCliff2DPrefabData[DecorationPrefabData.GetPrefabDataIndex(randomValue, this.smallSizeCliff2DPrefabData, this.smallSizeCliff2DPrefabDataWeightSum)];
				}
				return this.defaultSizeCliff2DPrefabData[DecorationPrefabData.GetPrefabDataIndex(randomValue, this.defaultSizeCliff2DPrefabData, this.defaultSizeCliff2DPrefabDataWeigthSum)];
			}
			else
			{
				if (smallCliff)
				{
					return this.smallSizeCliffPrefabData[DecorationPrefabData.GetPrefabDataIndex(randomValue, this.smallSizeCliffPrefabData, this.smallSizeCliffPrefabDataWeightSum)];
				}
				return this.defaultSizeCliffPrefabData[DecorationPrefabData.GetPrefabDataIndex(randomValue, this.defaultSizeCliffPrefabData, this.defaultSizeCliffPrefabDataWeigthSum)];
			}
		}

		public DecorationPrefabData GetVegetationPrefabData(int randomValue)
		{
			return this.vegetationDecorationPrefabData[DecorationPrefabData.GetPrefabDataIndex(randomValue, this.vegetationDecorationPrefabData, this.vegetationDecorationPrefabDataWeightSum)];
		}

		public DecorationPrefabData GetEdgesOfTheWorldPrefabData(int randomValue)
		{
			return this.edgesOfTheWorldDecorationPrefabData[DecorationPrefabData.GetPrefabDataIndex(randomValue, this.edgesOfTheWorldDecorationPrefabData, this.edgesOfTheWorldDecorationPrefabDataWeightSum)];
		}

		public DecorationPrefabData GetRidgePrefabData(int randomValue)
		{
			return this.rigdePrefabData[DecorationPrefabData.GetPrefabDataIndex(randomValue, this.rigdePrefabData, this.rigdePrefabDataWeigthSum)];
		}

		private static void FillPrefabDataArray(ref DecorationPrefabData[] prefabArray, ref int prefabWeightSum, List<GameObject> prefabs, List<int> weights, Matrix4x4 defaultRotationMatrix, Matrix4x4 inverseDefaultRotationMatrix, DecorationPrefabData.GetOrCreateInstanciedMeshIndexDelegate getOrCreateInstanciedMeshIndexDelegate, DecorationPrefabData.RetrieveInstanciedMeshIndexDelegate retrieveInstanciedMeshIndexDelegate, GameObject fatherOfInstanciedGameObject, bool warnIfEmpty, bool warnIfNotFullyDescribedByFoundComponent, string terrainName, string category, bool verbose)
		{
			Diagnostics.Assert(weights.Count == prefabs.Count);
			prefabArray = new DecorationPrefabData[prefabs.Count];
			prefabWeightSum = 0;
			for (int i = 0; i < prefabs.Count; i++)
			{
				prefabArray[i] = new DecorationPrefabData(weights[i], "Unknown", prefabs[i], defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, string.Empty, null, fatherOfInstanciedGameObject, verbose, 0);
				prefabWeightSum += weights[i];
				if (warnIfNotFullyDescribedByFoundComponent && !prefabArray[i].FullyDescribedByFoundComponent)
				{
					Diagnostics.LogWarning("In Terrain {0}, the {1} prefab {2} contains components not known by the renderer", new object[]
					{
						terrainName,
						category,
						prefabs[i].name
					});
				}
				if (warnIfEmpty && (prefabArray[i].InstancingMeshInfos == null || prefabArray[i].InstancingMeshInfos.Length == 0))
				{
					Diagnostics.LogWarning("In Terrain {0}, the {1} prefab {2} has no instancing infos.", new object[]
					{
						terrainName,
						category,
						(!(prefabs[i] != null)) ? "null" : prefabs[i].name
					});
				}
			}
		}

		private static void SetProbabilityOnDecorationPrefabs(double probability, InstanciedMeshHolders instanciedMeshHolders, DecorationPrefabData[] prefabs, float prefabWeightSum)
		{
			for (int i = 0; i < prefabs.Length; i++)
			{
				double num = (double)prefabs[i].Weight / (double)prefabWeightSum;
				if (prefabs[i].InstancingMeshInfos != null)
				{
					for (int j = 0; j < prefabs[i].InstancingMeshInfos.Length; j++)
					{
						instanciedMeshHolders.AddOccurenceExpectation(prefabs[i].InstancingMeshInfos[j].MeshIndex, num * probability);
					}
				}
			}
		}

		private void EvaluatePickPolicy(string mappingLayerType, string groundPickPolicyName, string groundNoStairPickPolicyName, string waterPickPolicyName, string underWaterPickPolicyName)
		{
			this.PickMask = WorldMeshes.EncodingValues.PickMaskGround + WorldMeshes.EncodingValues.PickMaskWater;
			this.IncludePickMask = WorldMeshes.EncodingValues.PickMaskGround;
			this.PickHeightTolerance = 1.1f;
			if (this.TerrainTypeMapping.Layers == null)
			{
				return;
			}
			for (int i = 0; i < this.TerrainTypeMapping.Layers.Length; i++)
			{
				SimulationLayer simulationLayer = this.TerrainTypeMapping.Layers[i];
				if (!(simulationLayer.Type != mappingLayerType))
				{
					if (simulationLayer.Name == groundPickPolicyName)
					{
						this.PickMask = WorldMeshes.EncodingValues.PickMaskGround + WorldMeshes.EncodingValues.PickMaskWater;
						this.IncludePickMask = WorldMeshes.EncodingValues.PickMaskGround;
						return;
					}
					if (simulationLayer.Name == groundNoStairPickPolicyName)
					{
						this.PickMask = WorldMeshes.EncodingValues.PickMaskGround + WorldMeshes.EncodingValues.PickMaskWater;
						this.IncludePickMask = WorldMeshes.EncodingValues.PickMaskGround;
						this.PickHeightTolerance = 0.1f;
						return;
					}
					if (simulationLayer.Name == waterPickPolicyName)
					{
						this.PickMask = WorldMeshes.EncodingValues.PickMaskGround + WorldMeshes.EncodingValues.PickMaskWater;
						this.IncludePickMask = WorldMeshes.EncodingValues.PickMaskWater;
						this.PickHeightTolerance = 100f;
						return;
					}
					if (simulationLayer.Name == underWaterPickPolicyName)
					{
						this.PickMask = WorldMeshes.EncodingValues.PickMaskGround;
						this.IncludePickMask = WorldMeshes.EncodingValues.PickMaskGround;
						return;
					}
					Diagnostics.LogWarning("Unable to find a meaning for '{0}' in evaluating PickPolicy. Valid value are in '{1}', '{2}', '{3}' or '{4}'.", new object[]
					{
						simulationLayer.Name,
						groundPickPolicyName,
						waterPickPolicyName,
						groundNoStairPickPolicyName,
						underWaterPickPolicyName
					});
				}
			}
		}

		private void EvaluateTerrainTypeCategory(string mappingLayerType, string groundTerrainTypeCategoryName, string inlandWaterTerrainTypeCategoryName, string oceanTerrainTypeCategoryName)
		{
			this.category = HxTechniqueGraphicData.OneTerrainTypeGraphicData.TerrainTypeCategory.Ground;
			if (this.TerrainTypeMapping.Layers == null)
			{
				return;
			}
			for (int i = 0; i < this.TerrainTypeMapping.Layers.Length; i++)
			{
				SimulationLayer simulationLayer = this.TerrainTypeMapping.Layers[i];
				if (!(simulationLayer.Type != mappingLayerType))
				{
					if (simulationLayer.Name == groundTerrainTypeCategoryName)
					{
						this.category = HxTechniqueGraphicData.OneTerrainTypeGraphicData.TerrainTypeCategory.Ground;
						return;
					}
					if (simulationLayer.Name == inlandWaterTerrainTypeCategoryName)
					{
						this.category = HxTechniqueGraphicData.OneTerrainTypeGraphicData.TerrainTypeCategory.InlandWater;
						return;
					}
					if (simulationLayer.Name == oceanTerrainTypeCategoryName)
					{
						this.category = HxTechniqueGraphicData.OneTerrainTypeGraphicData.TerrainTypeCategory.Ocean;
						return;
					}
					Diagnostics.LogWarning("Unable to find a meaning for '{0}' in evaluating TerrainTypeCategory. Valid value are '{1}', '{2}' or '{3}'.", new object[]
					{
						simulationLayer.Name,
						groundTerrainTypeCategoryName,
						inlandWaterTerrainTypeCategoryName,
						oceanTerrainTypeCategoryName
					});
				}
			}
		}

		private void EvaluateSoundAmbiance(string mappingLayerType, List<StaticString> soundAmbianceNames)
		{
			Diagnostics.Assert(soundAmbianceNames != null);
			this.soundAmbianceName = string.Empty;
			if (this.TerrainTypeMapping.Layers == null)
			{
				return;
			}
			bool flag = false;
			for (int i = 0; i < this.TerrainTypeMapping.Layers.Length; i++)
			{
				SimulationLayer simulationLayer = this.TerrainTypeMapping.Layers[i];
				if (!(simulationLayer.Type != mappingLayerType))
				{
					this.soundAmbianceName = simulationLayer.Name;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				Diagnostics.LogWarning("In Terrain Type '{0}', unable to find mapping for '{1}'.", new object[]
				{
					this.TerrainTypeMapping.Name,
					mappingLayerType
				});
			}
			else if (!StaticString.IsNullOrEmpty(this.soundAmbianceName))
			{
				int num = soundAmbianceNames.IndexOf(this.soundAmbianceName);
				Diagnostics.Assert(num < 255);
				if (num < 0)
				{
					Diagnostics.Assert(soundAmbianceNames.Count < 255);
					this.soundAmbianceIndex = (byte)soundAmbianceNames.Count;
					soundAmbianceNames.Add(this.soundAmbianceName);
				}
				else
				{
					this.soundAmbianceIndex = (byte)num;
				}
			}
		}

		private void EvaluateRoadName(string mappingLayerType, List<string> roadNames)
		{
			Diagnostics.Assert(roadNames != null);
			this.roadName = string.Empty;
			if (this.TerrainTypeMapping.Layers == null)
			{
				return;
			}
			bool flag = false;
			for (int i = 0; i < this.TerrainTypeMapping.Layers.Length; i++)
			{
				SimulationLayer simulationLayer = this.TerrainTypeMapping.Layers[i];
				if (!(simulationLayer.Type != mappingLayerType))
				{
					this.roadName = simulationLayer.Name;
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				Diagnostics.LogWarning("In Terrain Type '{0}', unable to find mapping for '{1}'.", new object[]
				{
					this.TerrainTypeMapping.Name,
					mappingLayerType
				});
			}
			else if (this.roadName != string.Empty)
			{
				int num = roadNames.IndexOf(this.roadName);
				Diagnostics.Assert(num < 255);
				if (num < 0)
				{
					Diagnostics.Assert(roadNames.Count < 255);
					this.roadIndex = (byte)roadNames.Count;
					roadNames.Add(this.roadName);
				}
				else
				{
					this.roadIndex = (byte)num;
				}
			}
		}

		private void FillDecorationPrefabData(List<GameObject> prefabs, List<int> weights, StaticString mappingTypeName, StaticString mappingLayerName, HxTechniqueGraphicData.GetOrCreatePrefabCopyDelegate getOrCreatePrefabCopyDelegate)
		{
			prefabs.Clear();
			weights.Clear();
			if (this.TerrainTypeMapping.Layers == null)
			{
				prefabs.Add(null);
				weights.Add(100);
				return;
			}
			for (int i = 0; i < this.TerrainTypeMapping.Layers.Length; i++)
			{
				SimulationLayer simulationLayer = this.TerrainTypeMapping.Layers[i];
				if (!(simulationLayer.Name != mappingLayerName))
				{
					if (!(simulationLayer.Type != mappingTypeName))
					{
						for (int j = 0; j < simulationLayer.Samples.Length; j++)
						{
							SimulationLayer.Sample sample = simulationLayer.Samples[j];
							weights.Add(sample.Weight);
							if (string.IsNullOrEmpty(sample.Value))
							{
								prefabs.Add(null);
							}
							else
							{
								prefabs.Add(getOrCreatePrefabCopyDelegate(sample.Value, false));
							}
						}
					}
				}
			}
			if (prefabs.Count == 0)
			{
				prefabs.Add(null);
				weights.Add(100);
			}
		}

		public static readonly StaticString CliffDefaultSizeTypeMappingLayerName = new StaticString("CliffDefaultSize");

		public static readonly StaticString CliffSmallSizeTypeMappingLayerName = new StaticString("CliffSmallSize");

		public static readonly StaticString CliffDefaultSize2DTypeMappingLayerName = new StaticString("CliffDefaultSize2D");

		public static readonly StaticString CliffSmallSize2DTypeMappingLayerName = new StaticString("CliffSmallSize2D");

		public static readonly StaticString VegetationTypeMappingLayerName = new StaticString("Vegetation");

		public static readonly StaticString EdgesOfTheWorldTypeMappingLayerName = new StaticString("EdgesOfTheWorld");

		public static readonly StaticString GeometryTypeMappingLayerType = new StaticString("Geometry");

		public static readonly StaticString RidgeTypeMappingLayerName = new StaticString("Ridge");

		private DecorationPrefabData[] defaultSizeCliffPrefabData;

		private int defaultSizeCliffPrefabDataWeigthSum;

		private DecorationPrefabData[] smallSizeCliffPrefabData;

		private int smallSizeCliffPrefabDataWeightSum;

		private DecorationPrefabData[] defaultSizeCliff2DPrefabData;

		private int defaultSizeCliff2DPrefabDataWeigthSum;

		private DecorationPrefabData[] smallSizeCliff2DPrefabData;

		private int smallSizeCliff2DPrefabDataWeightSum;

		private DecorationPrefabData[] vegetationDecorationPrefabData;

		private int vegetationDecorationPrefabDataWeightSum;

		private DecorationPrefabData[] edgesOfTheWorldDecorationPrefabData;

		private int edgesOfTheWorldDecorationPrefabDataWeightSum;

		private DecorationPrefabData[] rigdePrefabData;

		private int rigdePrefabDataWeigthSum;

		private double probability;

		private StaticString soundAmbianceName;

		private byte soundAmbianceIndex = byte.MaxValue;

		private string roadName;

		private byte roadIndex = byte.MaxValue;

		private HxTechniqueGraphicData.OneTerrainTypeGraphicData.TerrainTypeCategory category;

		public enum TerrainTypeCategory
		{
			Ground,
			InlandWater,
			Ocean
		}
	}

	public class AllResourceFidsGraphicData
	{
		public DecorationPrefabData CityExploitationHexaData
		{
			get
			{
				return this.cityExploitationHexaData;
			}
		}

		public IEnumerator Load(HxTechniqueGraphicData.GetOrCreatePrefabCopyDelegate getOrCreatePrefabCopyDelegate, DecorationPrefabData.GetOrCreateInstanciedMeshIndexDelegate getOrCreateInstanciedMeshIndexDelegate, DecorationPrefabData.RetrieveInstanciedMeshIndexDelegate retrieveInstanciedMeshIndexDelegate, GameObject fatherOfInstanciedGameObject, bool verbose)
		{
			IGameService gameService = Services.GetService<IGameService>();
			Diagnostics.Assert(gameService != null);
			IWorldPositionSimulationEvaluatorService worldPositionSimulationEvaluatorService = gameService.Game.Services.GetService<IWorldPositionSimulationEvaluatorService>();
			Diagnostics.Assert(worldPositionSimulationEvaluatorService != null);
			StaticString[] worldPositionResourceNames = worldPositionSimulationEvaluatorService.GetWorldPositionResourceNames();
			IDatabase<GuiElement> guiElementDatabase = Databases.GetDatabase<GuiElement>(false);
			Matrix4x4 defaultRotationMatrix = Matrix4x4.TRS(UnityEngine.Vector3.zero, Quaternion.Euler(new UnityEngine.Vector3(270f, 0f, 0f)), new UnityEngine.Vector3(1f, 1f, 1f));
			Matrix4x4 inverseDefaultRotationMatrix = Matrix4x4.TRS(UnityEngine.Vector3.zero, Quaternion.Euler(new UnityEngine.Vector3(-270f, 0f, 0f)), new UnityEngine.Vector3(1f, 1f, 1f));
			this.FidsIncomePrefabData = new DecorationPrefabData[worldPositionResourceNames.Length];
			this.NegativeFidsIncomePrefabData = new DecorationPrefabData[worldPositionResourceNames.Length];
			for (int i = 0; i < worldPositionResourceNames.Length; i++)
			{
				GuiElement guiElement = guiElementDatabase.GetValue(worldPositionResourceNames[i]);
				ExtendedGuiElement extendedGuiElement = guiElement as ExtendedGuiElement;
				Color color = Color.red;
				if (extendedGuiElement != null)
				{
					color = extendedGuiElement.Color;
				}
				string incomePrefabDataPrefabName = string.Format("Prefabs/FidsRenderer/{0}/Fids", worldPositionResourceNames[i]);
				string negativeIncomePrefabDataPrefabName = string.Format("Prefabs/FidsRenderer/{0}/NegativeFids", worldPositionResourceNames[i]);
				GameObject prefab = getOrCreatePrefabCopyDelegate(incomePrefabDataPrefabName, false);
				if (prefab != null)
				{
					this.ModifyPrefabMaterialColor("_ResourceColor", color, prefab);
					this.FidsIncomePrefabData[i] = new DecorationPrefabData(0, incomePrefabDataPrefabName, prefab, defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, string.Empty, null, fatherOfInstanciedGameObject, verbose, 0);
				}
				else
				{
					Diagnostics.LogWarning("Unable to load prefab {0}", new object[]
					{
						incomePrefabDataPrefabName
					});
				}
				GameObject negativePrefab = getOrCreatePrefabCopyDelegate(negativeIncomePrefabDataPrefabName, false);
				if (negativePrefab != null)
				{
					this.ModifyPrefabMaterialColor("_ResourceColor", color, negativePrefab);
					this.NegativeFidsIncomePrefabData[i] = new DecorationPrefabData(0, negativeIncomePrefabDataPrefabName, negativePrefab, defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, string.Empty, null, fatherOfInstanciedGameObject, verbose, 0);
				}
				else
				{
					if (verbose)
					{
						Diagnostics.Log("Unable to load optional prefab {0} for negative income using same than for positive income", new object[]
						{
							negativeIncomePrefabDataPrefabName
						});
					}
					this.NegativeFidsIncomePrefabData[i] = this.FidsIncomePrefabData[i];
				}
				if (verbose)
				{
					Color32 color2 = color;
					Diagnostics.Log("Loading fids prefab {0}, {1}, color( {2}, {3}, {4} )", new object[]
					{
						incomePrefabDataPrefabName,
						negativeIncomePrefabDataPrefabName,
						color2.r,
						color2.g,
						color2.b
					});
				}
				yield return null;
			}
			string prefabName = "Prefabs/FidsRenderer/Fids_Exploitation_Hexagon_01";
			GameObject prefab2 = getOrCreatePrefabCopyDelegate(prefabName, false);
			this.cityExploitationHexaData = new DecorationPrefabData(1, prefabName, prefab2, defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, string.Empty, null, fatherOfInstanciedGameObject, verbose, 64);
			if (!this.cityExploitationHexaData.FullyDescribedByFoundComponent)
			{
				Diagnostics.LogWarning("The fids prefab {0} contains components not known by the renderer.", new object[]
				{
					prefab2.name
				});
			}
			yield break;
		}

		public void ForgetDataNotUsedInMainRender()
		{
			for (int i = 0; i < this.FidsIncomePrefabData.Length; i++)
			{
				this.FidsIncomePrefabData[i].ForgetDataNotUsedInMainRender();
			}
			for (int j = 0; j < this.NegativeFidsIncomePrefabData.Length; j++)
			{
				this.NegativeFidsIncomePrefabData[j].ForgetDataNotUsedInMainRender();
			}
			this.cityExploitationHexaData.ForgetDataNotUsedInMainRender();
		}

		public void Unload()
		{
		}

		private void ModifyPrefabMaterialColor(string resourcePropertyName, Color fidsColor, GameObject prefab)
		{
			Renderer[] componentsInChildren = prefab.GetComponentsInChildren<Renderer>(true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				for (int j = 0; j < componentsInChildren[i].sharedMaterials.Length; j++)
				{
					Material material = componentsInChildren[i].sharedMaterials[j];
					if (material.HasProperty(resourcePropertyName))
					{
						material.SetColor(resourcePropertyName, fidsColor);
					}
				}
			}
		}

		public DecorationPrefabData[] FidsIncomePrefabData;

		public DecorationPrefabData[] NegativeFidsIncomePrefabData;

		private DecorationPrefabData cityExploitationHexaData;
	}

	public class AllOrbGraphicData
	{
		public DecorationPrefabData GetPrefabData(int index, out int prefabDataIndex)
		{
			Diagnostics.Assert(index >= 0);
			int num = Math.Max(0, Math.Min(this.orbValueToPrefabDataIndex.Length - 1, index));
			prefabDataIndex = this.orbValueToPrefabDataIndex[num];
			if (prefabDataIndex >= 0 && prefabDataIndex < this.orbPrefabDatas.Length)
			{
				return this.orbPrefabDatas[prefabDataIndex];
			}
			return this.invalidData;
		}

		public GameObject GetPrefabFx(int prefabDataIndex)
		{
			if (prefabDataIndex >= 0 && prefabDataIndex < this.orbPrefabDatas.Length)
			{
				return this.harvestFx[prefabDataIndex];
			}
			return null;
		}

		public DecorationPrefabData GetPrefabData(int index)
		{
			int num;
			return this.GetPrefabData(index, out num);
		}

		public IEnumerator Load(ILocalizationService localizationService, HxTechniqueGraphicData.GetOrCreatePrefabCopyDelegate getOrCreatePrefabCopyDelegate, DecorationPrefabData.GetOrCreateInstanciedMeshIndexDelegate getOrCreateInstanciedMeshIndexDelegate, DecorationPrefabData.RetrieveInstanciedMeshIndexDelegate retrieveInstanciedMeshIndexDelegate, DecorationPrefabData.AddSmallMeshesDelegate addSmallMeshesDelegate, GameObject fatherOfInstanciedGameObject, bool verbose)
		{
			IDatabase<OrbTypeMapping> orbTypeMappingDatabase = Databases.GetDatabase<OrbTypeMapping>(true);
			OrbTypeMapping[] orbTypeMappings = orbTypeMappingDatabase.GetValues();
			if (orbTypeMappings == null || orbTypeMappings.Length == 0)
			{
				Diagnostics.LogError("Empty [{0}] data base ", new object[]
				{
					typeof(OrbTypeMapping)
				});
			}
			Matrix4x4 defaultRotationMatrix = Matrix4x4.TRS(UnityEngine.Vector3.zero, Quaternion.Euler(new UnityEngine.Vector3(270f, 0f, 0f)), new UnityEngine.Vector3(1f, 1f, 1f));
			Matrix4x4 inverseDefaultRotationMatrix = Matrix4x4.TRS(UnityEngine.Vector3.zero, Quaternion.Euler(new UnityEngine.Vector3(-270f, 0f, 0f)), new UnityEngine.Vector3(1f, 1f, 1f));
			string loadingOrbMessage = localizationService.Localize("%LoadingOrb", "Loading Orbs");
			Diagnostics.Progress.SetProgress(0f, loadingOrbMessage);
			this.orbPrefabDatas = new DecorationPrefabData[orbTypeMappings.Length];
			this.harvestFx = new GameObject[orbTypeMappings.Length];
			List<int> orbValueToPrefabDataIndexList = new List<int>();
			for (int i = 0; i < orbTypeMappings.Length; i++)
			{
				OrbTypeMapping orbTypeMapping = orbTypeMappings[i];
				if (orbTypeMapping.Layers != null)
				{
					for (int index = 0; index < orbTypeMapping.Layers.Length; index++)
					{
						SimulationLayer layer = orbTypeMapping.Layers[index];
						if (!(layer.Name != HxTechniqueGraphicData.AllOrbGraphicData.LayerName))
						{
							if (layer.Type == HxTechniqueGraphicData.AllOrbGraphicData.GeometryLayerType && layer.Samples != null && layer.Samples.Length > 0)
							{
								SimulationLayer.Sample sample = layer.Samples[0];
								this.orbPrefabDatas[i] = new DecorationPrefabData(sample.Weight, sample.Value, getOrCreatePrefabCopyDelegate(sample.Value, false), defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, string.Empty, null, fatherOfInstanciedGameObject, verbose, 2);
							}
							if (layer.Type == HxTechniqueGraphicData.AllOrbGraphicData.FxLayerType && layer.Samples != null && layer.Samples.Length > 0)
							{
								SimulationLayer.Sample sample2 = layer.Samples[0];
								this.harvestFx[i] = getOrCreatePrefabCopyDelegate(sample2.Value, false);
							}
							if (layer.Type == HxTechniqueGraphicData.AllOrbGraphicData.OrbValueMinMaxLayerType && layer.Samples.Length > 0)
							{
								int minIndex = layer.Samples[0].Weight;
								int maxIndex = minIndex;
								for (int sampleIndex = 1; sampleIndex < layer.Samples.Length; sampleIndex++)
								{
									int weight = layer.Samples[sampleIndex].Weight;
									minIndex = Math.Min(weight, minIndex);
									maxIndex = Math.Max(weight, maxIndex);
								}
								maxIndex = Math.Min(Math.Max(0, maxIndex), 255);
								minIndex = Math.Min(Math.Max(0, minIndex), 255);
								Diagnostics.Assert(maxIndex >= minIndex);
								while (maxIndex >= orbValueToPrefabDataIndexList.Count)
								{
									orbValueToPrefabDataIndexList.Add(-1);
								}
								for (int indexIndex = minIndex; indexIndex <= maxIndex; indexIndex++)
								{
									orbValueToPrefabDataIndexList[indexIndex] = i;
								}
							}
						}
					}
				}
				Diagnostics.Progress.SetProgress((float)(i + 1) / (float)orbTypeMappings.Length);
				yield return null;
			}
			for (int j = 0; j < this.orbPrefabDatas.Length; j++)
			{
				Diagnostics.Assert(this.orbPrefabDatas[j].NotEmpty);
			}
			this.orbValueToPrefabDataIndex = orbValueToPrefabDataIndexList.ToArray();
			for (int k = 1; k < this.orbValueToPrefabDataIndex.Length; k++)
			{
				Diagnostics.Assert(this.orbValueToPrefabDataIndex[k] >= 0);
				Diagnostics.Assert(this.orbValueToPrefabDataIndex[k] < this.orbPrefabDatas.Length);
			}
			yield return null;
			yield break;
		}

		public void Unload()
		{
		}

		private static readonly StaticString LayerName = new StaticString("Orb");

		private static readonly StaticString GeometryLayerType = new StaticString("Geometry");

		private static readonly StaticString FxLayerType = new StaticString("Fx");

		private static readonly StaticString OrbValueMinMaxLayerType = new StaticString("OrbValueMinMax");

		private DecorationPrefabData[] orbPrefabDatas;

		private GameObject[] harvestFx;

		private int[] orbValueToPrefabDataIndex;

		private DecorationPrefabData invalidData = default(DecorationPrefabData);
	}

	public class AllWeatherGraphicData
	{
		public int WeatherFxRadius(int weatherType)
		{
			return this.weatherFx[weatherType].WeatherFxRadius;
		}

		public DecorationPrefabData GetWeatherDecorationPrefabData(int weatherIndex, int randomData)
		{
			if (weatherIndex < 0 || weatherIndex >= this.weatherFx.Length)
			{
				return default(DecorationPrefabData);
			}
			int num = this.weatherFx[weatherIndex].Icons.Length;
			if (num > 0)
			{
				int num2 = randomData % num;
				return this.weatherFx[weatherIndex].Icons[num2];
			}
			return default(DecorationPrefabData);
		}

		public IEnumerator Load(ILocalizationService localizationService, HxTechniqueGraphicData.GetOrCreatePrefabCopyDelegate getOrCreatePrefabCopyDelegate, DecorationPrefabData.GetOrCreateInstanciedMeshIndexDelegate getOrCreateInstanciedMeshIndexDelegate, DecorationPrefabData.RetrieveInstanciedMeshIndexDelegate retrieveInstanciedMeshIndexDelegate, DecorationPrefabData.AddSmallMeshesDelegate addSmallMeshesDelegate, GameObject fatherOfInstanciedGameObject, bool verbose)
		{
			Dictionary<string, int> weatherNameToValue;
			int weatherTypeMaxValue;
			this.CreateWeatherNameToWeatherTypeDictionary(out weatherNameToValue, out weatherTypeMaxValue);
			this.deactivatedWeatherFather = new GameObject();
			this.deactivatedWeatherFather.name = "WeatherFxPool";
			this.deactivatedWeatherFather.transform.parent = fatherOfInstanciedGameObject.transform;
			this.deactivatedWeatherFather.SetActive(false);
			this.MaxWeatherType = weatherTypeMaxValue;
			IDatabase<WeatherTypeMapping> weatherTypeMappingDatabase = Databases.GetDatabase<WeatherTypeMapping>(true);
			WeatherTypeMapping[] weatherTypeMappings = weatherTypeMappingDatabase.GetValues();
			if (weatherTypeMappings == null || weatherTypeMappings.Length == 0)
			{
				Diagnostics.LogError("Empty [{0}] data base ", new object[]
				{
					typeof(WeatherTypeMapping)
				});
			}
			string loadingMessage = localizationService.Localize("%LoadingWeather", "Loading Weather");
			Diagnostics.Progress.SetProgress(0f, loadingMessage);
			this.weatherFx = new HxTechniqueGraphicData.AllWeatherGraphicData.OneWeatherType[weatherTypeMaxValue + 1];
			for (int i = 0; i < this.weatherFx.Length; i++)
			{
				this.weatherFx[i] = new HxTechniqueGraphicData.AllWeatherGraphicData.OneWeatherType(i);
			}
			Matrix4x4 defaultRotationMatrix = Matrix4x4.TRS(UnityEngine.Vector3.zero, Quaternion.Euler(new UnityEngine.Vector3(270f, 0f, 0f)), new UnityEngine.Vector3(1f, 1f, 1f));
			Matrix4x4 inverseDefaultRotationMatrix = Matrix4x4.TRS(UnityEngine.Vector3.zero, Quaternion.Euler(new UnityEngine.Vector3(-270f, 0f, 0f)), new UnityEngine.Vector3(1f, 1f, 1f));
			for (int j = 0; j < weatherTypeMappings.Length; j++)
			{
				WeatherTypeMapping weatherTypeMapping = weatherTypeMappings[j];
				int weatherTypeIndex = -1;
				weatherNameToValue.TryGetValue(weatherTypeMapping.Name, out weatherTypeIndex);
				if (weatherTypeIndex == -1)
				{
					Diagnostics.LogError("Can't find a weather definition of name [{0}]", new object[]
					{
						weatherTypeMapping.Name
					});
				}
				else
				{
					if (weatherTypeMapping.Layers != null)
					{
						for (int index = 0; index < weatherTypeMapping.Layers.Length; index++)
						{
							SimulationLayer layer = weatherTypeMapping.Layers[index];
							if (!(layer.Name != HxTechniqueGraphicData.AllWeatherGraphicData.LayerName))
							{
								if (layer.Type == HxTechniqueGraphicData.AllWeatherGraphicData.FxLayerType && layer.Samples != null && layer.Samples.Length > 0)
								{
									SimulationLayer.Sample sample = layer.Samples[0];
									int radius = (sample.Weight <= 0) ? 2 : sample.Weight;
									Diagnostics.Assert(radius >= 1);
									Diagnostics.Assert(radius <= 2);
									GameObject weatherFxPrefab = getOrCreatePrefabCopyDelegate(sample.Value, false);
									GameObject weatherFxPrefabCopy = (!(weatherFxPrefab != null)) ? null : UnityEngine.Object.Instantiate<GameObject>(weatherFxPrefab);
									this.weatherFx[weatherTypeIndex].SetWeatherFx(weatherFxPrefabCopy, radius);
									if (weatherFxPrefabCopy != null)
									{
										foreach (Renderer subRenderer in weatherFxPrefabCopy.GetComponentsInChildren<Renderer>(true))
										{
											if (subRenderer.sharedMaterials != null && subRenderer.sharedMaterials.Length > 1)
											{
												for (int materialIndex = 0; materialIndex < subRenderer.materials.Length; materialIndex++)
												{
													this.ModifyMaterialForWeatherSupport(subRenderer.materials[materialIndex], weatherTypeIndex);
												}
											}
											else
											{
												this.ModifyMaterialForWeatherSupport(subRenderer.material, weatherTypeIndex);
											}
										}
										if (weatherFxPrefabCopy.activeSelf)
										{
											weatherFxPrefabCopy.SetActive(false);
										}
										weatherFxPrefabCopy.transform.SetParent(fatherOfInstanciedGameObject.transform, true);
									}
									else
									{
										Diagnostics.LogError("Unable to load prefab [{0}]", new object[]
										{
											sample.Value
										});
									}
								}
								if (layer.Type == HxTechniqueGraphicData.AllWeatherGraphicData.IconLayerType && layer.Samples != null && layer.Samples.Length > 0)
								{
									this.weatherFx[weatherTypeIndex].Icons = new DecorationPrefabData[layer.Samples.Length];
									for (int layerIndex = 0; layerIndex < layer.Samples.Length; layerIndex++)
									{
										SimulationLayer.Sample sample2 = layer.Samples[layerIndex];
										GameObject weatherFxPrefab2 = getOrCreatePrefabCopyDelegate(sample2.Value, false);
										this.weatherFx[weatherTypeIndex].Icons[layerIndex] = new DecorationPrefabData(sample2.Weight, sample2.Value, weatherFxPrefab2, defaultRotationMatrix, inverseDefaultRotationMatrix, getOrCreateInstanciedMeshIndexDelegate, retrieveInstanciedMeshIndexDelegate, "Icons", null, fatherOfInstanciedGameObject, verbose, 0);
										if (!this.weatherFx[weatherTypeIndex].Icons[layerIndex].NotEmpty)
										{
											Diagnostics.LogError("The prefab [{0}] in layer [{1}] should contain data", new object[]
											{
												sample2.Value,
												layer.Type
											});
										}
									}
								}
							}
						}
					}
					Diagnostics.Progress.SetProgress((float)(j + 1) / (float)weatherTypeMappings.Length);
					yield return null;
				}
			}
			foreach (KeyValuePair<string, int> keyValuePair in weatherNameToValue)
			{
				if (this.weatherFx[keyValuePair.Value].WeatherFx == null)
				{
					Diagnostics.LogError("Can't find a weather visual for weather definition [{0}]", new object[]
					{
						keyValuePair.Key
					});
				}
			}
			yield break;
		}

		public void Unload()
		{
			for (int i = 0; i < this.weatherFx.Length; i++)
			{
				this.weatherFx[i].Unload();
			}
			this.weatherFx = null;
			this.deactivatedWeatherFather = null;
		}

		public GameObject GetOrCreateFx(int fxIndex)
		{
			if (fxIndex < 0 || fxIndex >= this.weatherFx.Length)
			{
				Diagnostics.LogWarning("Weather type [{0}] is incorrect (max weather type = [{1}]).", new object[]
				{
					fxIndex,
					this.weatherFx.Length
				});
				return null;
			}
			int count = this.weatherFx[fxIndex].WeatherFxPool.Count;
			if (count > 0)
			{
				GameObject gameObject = this.weatherFx[fxIndex].WeatherFxPool[count - 1];
				gameObject.SetActive(true);
				this.weatherFx[fxIndex].WeatherFxPool.RemoveAt(count - 1);
				return gameObject;
			}
			GameObject gameObject2 = this.weatherFx[fxIndex].WeatherFx;
			if (gameObject2 == null)
			{
				Diagnostics.LogWarning("Weather type [{0}] is no fx defined.", new object[]
				{
					fxIndex,
					this.weatherFx.Length
				});
			}
			return (!(gameObject2 != null)) ? null : UnityEngine.Object.Instantiate<GameObject>(gameObject2);
		}

		public void ReleaseOrKeepFx(GameObject fx, int fxIndex)
		{
			if (fx != null)
			{
				fx.SetActive(false);
				fx.transform.parent = this.deactivatedWeatherFather.transform;
				this.weatherFx[fxIndex].WeatherFxPool.Add(fx);
			}
		}

		private void ModifyMaterialForWeatherSupport(Material material, int weatherTypeIndex)
		{
			material.SetFloat("_WeatherTypeIndex", (float)weatherTypeIndex);
			string[] shaderKeywords = material.shaderKeywords;
			string[] array;
			if (shaderKeywords != null && shaderKeywords.Length > 0)
			{
				array = new string[shaderKeywords.Length + HxTechniqueGraphicData.AllWeatherGraphicData.DefaultMaterialKeywords.Length];
				for (int i = 0; i < shaderKeywords.Length; i++)
				{
					array[i] = shaderKeywords[i];
				}
				for (int j = 0; j < HxTechniqueGraphicData.AllWeatherGraphicData.DefaultMaterialKeywords.Length; j++)
				{
					array[j + shaderKeywords.Length] = HxTechniqueGraphicData.AllWeatherGraphicData.DefaultMaterialKeywords[j];
				}
			}
			else
			{
				array = HxTechniqueGraphicData.AllWeatherGraphicData.DefaultMaterialKeywords;
			}
			material.shaderKeywords = array;
		}

		private void CreateWeatherNameToWeatherTypeDictionary(out Dictionary<string, int> weatherNameToValue, out int weatherTypeMaxValue)
		{
			weatherNameToValue = new Dictionary<string, int>();
			weatherTypeMaxValue = -1;
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null && service.Game != null && service.Game.Services != null);
			IWeatherService service2 = service.Game.Services.GetService<IWeatherService>();
			int weatherDefinitionCount = service2.WeatherDefinitionCount;
			for (int i = 0; i < weatherDefinitionCount; i++)
			{
				WeatherDefinition weatherDefinition = service2.GetWeatherDefinition(i);
				weatherNameToValue.Add(weatherDefinition.Name, weatherDefinition.Value);
				Diagnostics.Assert(weatherDefinition.Value > 0);
				if (weatherDefinition.Value > weatherTypeMaxValue)
				{
					weatherTypeMaxValue = weatherDefinition.Value;
				}
			}
		}

		public int MaxWeatherType = -1;

		private static readonly StaticString LayerName = new StaticString("Weather");

		private static readonly StaticString FxLayerType = new StaticString("Fx");

		private static readonly StaticString IconLayerType = new StaticString("Icon");

		private static readonly string[] DefaultMaterialKeywords = new string[]
		{
			"WEATHER_ON"
		};

		private HxTechniqueGraphicData.AllWeatherGraphicData.OneWeatherType[] weatherFx;

		private GameObject deactivatedWeatherFather;

		public struct OneWeatherType
		{
			public OneWeatherType(int weatherIndex)
			{
				this.WeatherFx = null;
				this.WeatherFxRadius = -1;
				this.Icons = null;
				this.WeatherFxPool = new List<GameObject>();
			}

			public void SetWeatherFx(GameObject weatherFx, int weatherFxRadius)
			{
				this.WeatherFx = weatherFx;
				this.WeatherFxRadius = weatherFxRadius;
			}

			public void Unload()
			{
				this.WeatherFx = null;
				this.WeatherFxRadius = 0;
				if (this.WeatherFxPool != null)
				{
					for (int i = 0; i < this.WeatherFxPool.Count; i++)
					{
						UnityEngine.Object.DestroyImmediate(this.WeatherFxPool[i]);
					}
					this.WeatherFxPool.Clear();
					this.WeatherFxPool = null;
				}
			}

			public GameObject WeatherFx;

			public int WeatherFxRadius;

			public DecorationPrefabData[] Icons;

			public List<GameObject> WeatherFxPool;
		}
	}

	public delegate GameObject GetOrCreatePrefabCopyDelegate(string value, bool forceCopy);
}
