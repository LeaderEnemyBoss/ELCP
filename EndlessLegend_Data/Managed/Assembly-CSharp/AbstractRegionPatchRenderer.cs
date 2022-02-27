using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Utilities.Maps;
using UnityEngine;

public class AbstractRegionPatchRenderer : PatchRenderer
{
	public AbstractRegionPatchRenderer(uint additionalLayerMask) : base((ulong)(67109376u | additionalLayerMask), true)
	{
	}

	public override void Clear()
	{
		this.ClearRegionNameInstancingMeshBlocks(base.WorldViewTechnique as DefaultWorldViewTechnique);
		this.regionNameInstanciedMeshBlocks = null;
		this.ClearUserDefinedNameChangeEventIFN();
		base.Clear();
	}

	protected void AddRegionNamesIFN(DefaultWorldViewTechnique defaultWorldViewTechnique)
	{
		this.ClearRegionNameInstancingMeshBlocks(defaultWorldViewTechnique);
		GridMap<short> map = defaultWorldViewTechnique.WorldController.WorldAtlas.GetMap(WorldAtlas.Maps.Regions) as GridMap<short>;
		int upperHexaColumn = base.WorldPatch.UpperHexaColumn;
		int upperHexaRow = base.WorldPatch.UpperHexaRow;
		this.regionNameGraphicData = defaultWorldViewTechnique.HxTechniqueGraphicData.RegionNameGraphicDatas;
		for (int i = 0; i < base.WorldPatch.RowHexaCount; i++)
		{
			int row = i + upperHexaRow;
			for (int j = 0; j < base.WorldPatch.ColumnHexaCount; j++)
			{
				int column = j + upperHexaColumn;
				WorldPosition worldPosition = new WorldPosition(row, column);
				short value = map.GetValue(worldPosition);
				HxTechniqueGraphicData.RegionNameGraphicData.RegionData regionData = this.regionNameGraphicData.RegionDatas[(int)value];
				if (regionData.Center == worldPosition)
				{
					Map<Region> map2 = defaultWorldViewTechnique.WorldController.WorldAtlas.GetMap(WorldAtlas.Tables.Regions) as Map<Region>;
					Region region = map2.Data[(int)value];
					if (!this.eventRegionNameChangeRegistered)
					{
						if (this.regionCovered == null)
						{
							this.regionCovered = new List<Region>();
						}
						this.regionCovered.Add(region);
						region.UserDefinedNameChange += this.OnRegionNameChange;
					}
					bool showName = regionData.ShowName;
					if (showName)
					{
						this.AddRegionName(defaultWorldViewTechnique, regionData, region);
					}
				}
			}
		}
		this.eventRegionNameChangeRegistered = true;
		int num = this.regionNameGraphicData.SoftwareRasterAtlasRevisionIndex;
		if (this.softwareRasterAtlasRevisionIndex != num)
		{
			this.softwareRasterAtlasRevisionIndex = num;
			Material instancingMaterialFromMaterialIFP = defaultWorldViewTechnique.HxTechniqueGraphicData.InstanciedMeshHolders.GetInstancingMaterialFromMaterialIFP(this.regionNameGraphicData.Material, 4);
			if (instancingMaterialFromMaterialIFP != null)
			{
				this.regionNameGraphicData.BindDynamicTextureAtlas(instancingMaterialFromMaterialIFP);
			}
		}
	}

	private static Mesh CreateDebugRegionNameMesh()
	{
		Mesh orCreateMesh = UnityGraphicObjectPool.Singleton.GetOrCreateMesh(PrimitiveLayerMask.RegionName);
		Vector3[] array = new Vector3[4];
		Vector2[] array2 = new Vector2[4];
		Vector2[] array3 = new Vector2[4];
		Vector3[] array4 = new Vector3[4];
		int[] indices = new int[]
		{
			0,
			2,
			1,
			1,
			2,
			3
		};
		for (int i = 0; i < array.Length; i++)
		{
			float x = (float)(i % 2);
			float y = (float)(i / 2);
			array[i] = new Vector3(0f, 0f, 0f);
			array2[i] = new Vector2(x, y);
			array3[i] = new Vector2(x, y);
			array4[i] = new Vector3(0f, 1f, 0f);
		}
		orCreateMesh.vertices = array;
		orCreateMesh.normals = array4;
		orCreateMesh.uv = array2;
		orCreateMesh.uv2 = array3;
		orCreateMesh.SetIndices(indices, MeshTopology.Triangles, 0);
		return orCreateMesh;
	}

	private static int GetOrCreateMeshIndex(Material material, InstanciedMeshHolders instanciedMeshHolders)
	{
		string text = string.Format("Letter_{0}", material.name);
		int num = instanciedMeshHolders.RetrieveMeshIndex(text, InstanciedMeshHelpers.LetterPixelsPerInstance, Matrix4x4.identity, material, instanciedMeshHolders.MaxPerBigLineInstance(InstanciedMeshHelpers.LetterPixelsPerInstance));
		if (num == -1)
		{
			Mesh mesh = AbstractRegionPatchRenderer.CreateDebugRegionNameMesh();
			mesh.name = text;
			num = instanciedMeshHolders.AddSmallMesh(new InstanciedMeshHolders.MeshAndSubMeshIndex(mesh, 0), InstanciedMeshHelpers.LetterPixelsPerInstance, Matrix4x4.identity, material, instanciedMeshHolders.MaxPerBigLineInstance(InstanciedMeshHelpers.LetterPixelsPerInstance));
			Diagnostics.Assert(instanciedMeshHolders.RetrieveMeshIndex(text, InstanciedMeshHelpers.LetterPixelsPerInstance, Matrix4x4.identity, material, instanciedMeshHolders.MaxPerBigLineInstance(InstanciedMeshHelpers.LetterPixelsPerInstance)) == num);
		}
		return num;
	}

	private void ClearRegionNameInstancingMeshBlocks(DefaultWorldViewTechnique defaultWorldViewTechnique)
	{
		if (this.regionNameInstanciedMeshBlocks != null)
		{
			for (int i = 0; i < this.regionNameInstanciedMeshBlocks.Count; i++)
			{
				base.RemoveInstanciedMeshBlock(this.regionNameInstanciedMeshBlocks[i], false);
				defaultWorldViewTechnique.HxTechniqueGraphicData.InstanciedMeshHolders.ReleaseInstanciedMeshBlock(this.regionNameInstanciedMeshBlocks[i]);
			}
			this.regionNameInstanciedMeshBlocks.Clear();
		}
	}

	private void OnRegionNameChange(object sender, EventArgs eventArg)
	{
		this.AddRegionNamesIFN(base.WorldViewTechnique as DefaultWorldViewTechnique);
	}

	private void ClearUserDefinedNameChangeEventIFN()
	{
		if (this.regionCovered != null)
		{
			for (int i = 0; i < this.regionCovered.Count; i++)
			{
				this.regionCovered[i].UserDefinedNameChange -= this.OnRegionNameChange;
			}
			this.regionCovered.Clear();
			this.regionCovered = null;
		}
		this.eventRegionNameChangeRegistered = false;
	}

	private void AddRegionName(DefaultWorldViewTechnique defaultWorldViewTechnique, HxTechniqueGraphicData.RegionNameGraphicData.RegionData regionData, Region region)
	{
		InstanciedMeshHolders instanciedMeshHolders = defaultWorldViewTechnique.HxTechniqueGraphicData.InstanciedMeshHolders;
		InstanciedMeshBlock orCreateInstanciedMeshBlock = instanciedMeshHolders.GetOrCreateInstanciedMeshBlock(0, 67108864UL, InstanciedMeshHelpers.LetterPixelsPerInstance);
		Vector3 absoluteWorldPosition2D = AbstractGlobalPositionning.GetAbsoluteWorldPosition2D((int)regionData.Center.Row, (int)regionData.Center.Column);
		absoluteWorldPosition2D.y = base.GlobalPositionningService.GetAltitudeFromAbsoluteWorldPosition(new Vector3(absoluteWorldPosition2D.x, 0f, absoluteWorldPosition2D.z));
		string localizedName = region.LocalizedName;
		AgeFont ageFont = this.regionNameGraphicData.AgeFont;
		Material material = this.regionNameGraphicData.Material;
		int orCreateMeshIndex = AbstractRegionPatchRenderer.GetOrCreateMeshIndex(material, defaultWorldViewTechnique.HxTechniqueGraphicData.InstanciedMeshHolders);
		float textSize = this.regionNameGraphicData.TextSize;
		bool disableKerning = GameManager.Preferences.GameGraphicSettings.DisableKerning;
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		float num5 = 0f;
		for (int i = 0; i < localizedName.Length; i++)
		{
			char charcode = localizedName[i];
			char nextCharCode = (disableKerning || i + 1 >= localizedName.Length) ? '\0' : localizedName[i + 1];
			Vector2 vector;
			Vector2 vector2;
			Rect rect;
			float num6;
			ageFont.GetCharInfo(charcode, nextCharCode, out vector, out vector2, out rect, out num6);
			num2 = Math.Min(num + vector2.x, num2);
			num3 = Math.Max(num + vector2.x + vector.x, num3);
			num4 = Math.Min(num4, -vector2.y - vector.y);
			num5 = Math.Max(num5, -vector2.y);
			num += num6;
		}
		float z = -(num4 + num5) * 0.5f;
		float num7 = -(num2 + num3) * 0.5f;
		float num8 = num7;
		for (int j = 0; j < localizedName.Length; j++)
		{
			char charcode2 = localizedName[j];
			char nextCharCode2 = (disableKerning || j + 1 >= localizedName.Length) ? '\0' : localizedName[j + 1];
			Vector2 a;
			Vector2 vector3;
			Rect rect2;
			float num9;
			ageFont.GetCharInfo(charcode2, nextCharCode2, out a, out vector3, out rect2, out num9);
			if (a.x > 0f && a.y > 0f)
			{
				float num10 = 128f;
				bool flag = rect2.xMin >= num10;
				int minPixelIndexX;
				int minPixelIndexY;
				int pixelCountX;
				int pixelCountY;
				if (flag)
				{
					minPixelIndexX = Mathf.RoundToInt(rect2.xMin - num10);
					minPixelIndexY = Mathf.RoundToInt(rect2.yMin - num10);
					pixelCountX = (int)rect2.width;
					pixelCountY = (int)rect2.height;
				}
				else
				{
					minPixelIndexX = Mathf.RoundToInt(rect2.xMin * (float)this.regionNameGraphicData.FontTextureWidth);
					minPixelIndexY = Mathf.RoundToInt(rect2.yMin * (float)this.regionNameGraphicData.FontTextureHeight);
					pixelCountX = Mathf.RoundToInt(rect2.width * (float)this.regionNameGraphicData.FontTextureWidth);
					pixelCountY = Mathf.RoundToInt(rect2.height * (float)this.regionNameGraphicData.FontTextureHeight);
				}
				Vector3 a2 = new Vector3(num8, 0f, z) + new Vector3(vector3.x, 0f, -vector3.y) - new Vector3(0f, 0f, a.y);
				InstanciedMeshHelpers.AddLetterInstance(instanciedMeshHolders, orCreateInstanciedMeshBlock, absoluteWorldPosition2D + textSize * a2, minPixelIndexX, minPixelIndexY, pixelCountX, pixelCountY, a * textSize, orCreateMeshIndex, flag);
			}
			num8 += num9;
		}
		orCreateInstanciedMeshBlock.CloseAndSort();
		if (this.regionNameInstanciedMeshBlocks == null)
		{
			this.regionNameInstanciedMeshBlocks = new List<InstanciedMeshBlock>();
		}
		this.regionNameInstanciedMeshBlocks.Add(orCreateInstanciedMeshBlock);
		base.AddInstanciedMeshBlock(orCreateInstanciedMeshBlock, false);
	}

	private bool eventRegionNameChangeRegistered;

	private List<InstanciedMeshBlock> regionNameInstanciedMeshBlocks;

	private List<Region> regionCovered;

	private HxTechniqueGraphicData.RegionNameGraphicData regionNameGraphicData;

	private int softwareRasterAtlasRevisionIndex;
}
