using System;
using Amplitude;
using UnityEngine;

public class AbstractDebugPatchRenderer : PatchRenderer
{
	public AbstractDebugPatchRenderer(ulong dependentLayerMask, bool hideObjectNotExplored = true) : base(dependentLayerMask, hideObjectNotExplored)
	{
	}

	public override void Clear()
	{
		this.ClearInstancingMeshBlocks();
		base.Clear();
	}

	protected void CloseAndSortAndAddDebugInstanciedMeshBlock()
	{
		if (this.debugTextInstanciedMeshBlock != null)
		{
			this.debugTextInstanciedMeshBlock.CloseAndSort();
			base.AddInstanciedMeshBlock(this.debugTextInstanciedMeshBlock, false);
		}
	}

	protected void ClearInstancingMeshBlocks()
	{
		if (this.debugTextInstanciedMeshBlock != null)
		{
			base.RemoveInstanciedMeshBlock(this.debugTextInstanciedMeshBlock, false);
			this.instanciedMeshHolders.ReleaseInstanciedMeshBlock(this.debugTextInstanciedMeshBlock);
			this.debugTextInstanciedMeshBlock = null;
		}
	}

	protected void AddText(string stringToWrite, float textSizeMultiplier, WorldPosition worldPosition)
	{
		Vector3 absoluteWorldPosition2D = AbstractGlobalPositionning.GetAbsoluteWorldPosition2D((int)worldPosition.Row, (int)worldPosition.Column);
		absoluteWorldPosition2D.y = base.GlobalPositionningService.GetAltitudeFromAbsoluteWorldPosition(new Vector3(absoluteWorldPosition2D.x, 0f, absoluteWorldPosition2D.z));
		this.AddText(stringToWrite, textSizeMultiplier, absoluteWorldPosition2D);
	}

	protected void AddText(string stringToWrite, float textSizeMultiplier, Vector3 absoluteWorldPosition)
	{
		this.CreateDebugInstanciedMeshBlockIFN();
		AgeFont ageFont = this.debugGraphicData.AgeFont;
		int orCreateMeshIndex = AbstractDebugPatchRenderer.GetOrCreateMeshIndex(this.debugGraphicData.Material, this.instanciedMeshHolders);
		float d = textSizeMultiplier * this.debugGraphicData.TextSize;
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		float num5 = 0f;
		float num6 = 0f;
		for (int i = 0; i < stringToWrite.Length; i++)
		{
			char c = stringToWrite[i];
			if (c == '\n')
			{
				num2 -= ageFont.LineHeight;
				num = 0f;
			}
			else
			{
				char nextCharCode = (i + 1 >= stringToWrite.Length) ? '\0' : stringToWrite[i];
				Vector2 vector;
				Vector2 vector2;
				Rect rect;
				float num7;
				ageFont.GetCharInfo(c, nextCharCode, out vector, out vector2, out rect, out num7);
				num3 = Math.Min(num + vector2.x, num3);
				num4 = Math.Max(num + vector2.x + vector.x, num4);
				num5 = Math.Min(num5, -vector2.y - vector.y + num2);
				num6 = Math.Max(num6, -vector2.y + num2);
				num += num7;
			}
		}
		float num8 = -(num5 + num6) * 0.5f;
		float num9 = -(num3 + num4) * 0.5f;
		float num10 = num9;
		float num11 = num8;
		for (int j = 0; j < stringToWrite.Length; j++)
		{
			char c2 = stringToWrite[j];
			if (c2 == '\n')
			{
				num11 -= ageFont.LineHeight;
				num10 = num9;
			}
			else
			{
				char nextCharCode2 = (j + 1 >= stringToWrite.Length) ? '\0' : stringToWrite[j];
				Vector2 vector3;
				Vector2 vector4;
				Rect rect2;
				float num12;
				ageFont.GetCharInfo(c2, nextCharCode2, out vector3, out vector4, out rect2, out num12);
				if (vector3.x > 0f && vector3.y > 0f)
				{
					Vector3 a = new Vector3(num10, 0f, num11 + num8) + new Vector3(vector4.x, 0f, -vector4.y) - new Vector3(0f, 0f, vector3.y);
					int minPixelIndexX = Mathf.RoundToInt(rect2.xMin * (float)this.debugGraphicData.FontTextureWidth);
					int minPixelIndexY = Mathf.RoundToInt(rect2.yMin * (float)this.debugGraphicData.FontTextureHeight);
					int pixelCountX = Mathf.RoundToInt(rect2.width * (float)this.debugGraphicData.FontTextureWidth);
					int pixelCountY = Mathf.RoundToInt(rect2.height * (float)this.debugGraphicData.FontTextureHeight);
					InstanciedMeshHelpers.AddLetterInstance(this.instanciedMeshHolders, this.debugTextInstanciedMeshBlock, absoluteWorldPosition + d * a, minPixelIndexX, minPixelIndexY, pixelCountX, pixelCountY, vector3 * d, orCreateMeshIndex, false);
				}
				num10 += num12;
			}
		}
	}

	private static Mesh CreateDebugNameMesh()
	{
		Mesh orCreateMesh = UnityGraphicObjectPool.Singleton.GetOrCreateMesh(PrimitiveLayerMask.Debug);
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
			Mesh mesh = AbstractDebugPatchRenderer.CreateDebugNameMesh();
			mesh.name = text;
			num = instanciedMeshHolders.AddSmallMesh(new InstanciedMeshHolders.MeshAndSubMeshIndex(mesh, 0), InstanciedMeshHelpers.LetterPixelsPerInstance, Matrix4x4.identity, material, instanciedMeshHolders.MaxPerBigLineInstance(InstanciedMeshHelpers.LetterPixelsPerInstance));
			Diagnostics.Assert(instanciedMeshHolders.RetrieveMeshIndex(text, InstanciedMeshHelpers.LetterPixelsPerInstance, Matrix4x4.identity, material, instanciedMeshHolders.MaxPerBigLineInstance(InstanciedMeshHelpers.LetterPixelsPerInstance)) == num);
		}
		return num;
	}

	private void CreateDebugInstanciedMeshBlockIFN()
	{
		if (this.debugTextInstanciedMeshBlock == null)
		{
			this.CreateDebugInstanciedMeshBlock();
		}
	}

	private void CreateDebugInstanciedMeshBlock()
	{
		Diagnostics.Assert(this.debugTextInstanciedMeshBlock == null);
		DefaultWorldViewTechnique defaultWorldViewTechnique = base.WorldViewTechnique as DefaultWorldViewTechnique;
		this.instanciedMeshHolders = defaultWorldViewTechnique.HxTechniqueGraphicData.InstanciedMeshHolders;
		this.debugTextInstanciedMeshBlock = this.instanciedMeshHolders.GetOrCreateInstanciedMeshBlock(0, 1073741824UL, InstanciedMeshHelpers.LetterPixelsPerInstance);
		this.debugGraphicData = defaultWorldViewTechnique.HxTechniqueGraphicData.DebugGraphicDatas;
	}

	private InstanciedMeshBlock debugTextInstanciedMeshBlock;

	private InstanciedMeshHolders instanciedMeshHolders;

	private HxTechniqueGraphicData.DebugGraphicData debugGraphicData;
}
