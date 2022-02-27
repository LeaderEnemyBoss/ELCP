using System;
using Amplitude;
using Hx.Geometry;
using UnityEngine;

public class InstanciedMeshHelpers
{
	public static void AddInstance(InstanciedMeshHolders instanciedMeshHolders, InstanciedMeshBlock meshBlock, UnityEngine.Vector3 position, UnityEngine.Vector2 forward2D, float scaleY, int meshIndex)
	{
		Diagnostics.Assert(!meshBlock.Closed);
		Diagnostics.Assert(meshBlock.PixelsPerInstance == InstanciedMeshHelpers.PositionForwardScaleZPixelsPerInstance);
		Diagnostics.Assert(meshBlock.MeshColors.Count % InstanciedMeshHelpers.PositionForwardScaleZPixelsPerInstance == 0);
		Diagnostics.Assert(instanciedMeshHolders.GetPixelsPerInstance(meshIndex) == InstanciedMeshHelpers.PositionForwardScaleZPixelsPerInstance);
		int val = 65535;
		int num = Math.Max(0, Math.Min(val, (int)((position.x - instanciedMeshHolders.BBoxMin.x) * instanciedMeshHolders.Max16BitValueOverBBoxExtent.x)));
		int num2 = Math.Max(0, Math.Min(val, (int)((position.y - instanciedMeshHolders.BBoxMin.y) * instanciedMeshHolders.Max16BitValueOverBBoxExtent.y)));
		int num3 = Math.Max(0, Math.Min(val, (int)((position.z - instanciedMeshHolders.BBoxMin.z) * instanciedMeshHolders.Max16BitValueOverBBoxExtent.z)));
		UnityEngine.Vector2 vector = forward2D / scaleY;
		int num4 = Math.Max(0, Math.Min(val, (int)((vector.x - instanciedMeshHolders.BBoxMin.w) * instanciedMeshHolders.Max16BitValueOverBBoxExtent.w)));
		int num5 = Math.Max(0, Math.Min(val, (int)((vector.y - instanciedMeshHolders.BBoxMin.w) * instanciedMeshHolders.Max16BitValueOverBBoxExtent.w)));
		int num6 = Math.Max(0, Math.Min(val, (int)(scaleY * instanciedMeshHolders.Max16BitValueBBoxExtentScale)));
		int num7 = 255;
		int num8 = 65280;
		meshBlock.MeshColors.Add(new Color32((byte)(num & num7), (byte)((num & num8) >> 8), (byte)(num2 & num7), (byte)((num2 & num8) >> 8)));
		meshBlock.MeshColors.Add(new Color32((byte)(num3 & num7), (byte)((num3 & num8) >> 8), (byte)(num4 & num7), (byte)((num4 & num8) >> 8)));
		meshBlock.MeshColors.Add(new Color32((byte)(num5 & num7), (byte)((num5 & num8) >> 8), (byte)(num6 & num7), (byte)((num6 & num8) >> 8)));
		meshBlock.MeshIndices.Add(meshIndex);
		Diagnostics.Assert(meshBlock.MeshColors.Count % InstanciedMeshHelpers.PositionForwardScaleZPixelsPerInstance == 0);
	}

	public static void AddInstance(InstanciedMeshHolders instanciedMeshHolders, InstanciedMeshBlock meshBlock, UnityEngine.Vector3 position, UnityEngine.Vector2 forward2D, float scaleY, Color32 color, int meshIndex)
	{
		Diagnostics.Assert(!meshBlock.Closed);
		Diagnostics.Assert(meshBlock.PixelsPerInstance == InstanciedMeshHelpers.PositionForwardScaleZColorPixelsPerInstance);
		Diagnostics.Assert(meshBlock.MeshColors.Count % InstanciedMeshHelpers.PositionForwardScaleZColorPixelsPerInstance == 0);
		Diagnostics.Assert(instanciedMeshHolders.GetPixelsPerInstance(meshIndex) == InstanciedMeshHelpers.PositionForwardScaleZColorPixelsPerInstance);
		int val = 65535;
		int num = Math.Max(0, Math.Min(val, (int)((position.x - instanciedMeshHolders.BBoxMin.x) * instanciedMeshHolders.Max16BitValueOverBBoxExtent.x)));
		int num2 = Math.Max(0, Math.Min(val, (int)((position.y - instanciedMeshHolders.BBoxMin.y) * instanciedMeshHolders.Max16BitValueOverBBoxExtent.y)));
		int num3 = Math.Max(0, Math.Min(val, (int)((position.z - instanciedMeshHolders.BBoxMin.z) * instanciedMeshHolders.Max16BitValueOverBBoxExtent.z)));
		UnityEngine.Vector2 vector = forward2D / scaleY;
		int num4 = Math.Max(0, Math.Min(val, (int)((vector.x - instanciedMeshHolders.BBoxMin.w) * instanciedMeshHolders.Max16BitValueOverBBoxExtent.w)));
		int num5 = Math.Max(0, Math.Min(val, (int)((vector.y - instanciedMeshHolders.BBoxMin.w) * instanciedMeshHolders.Max16BitValueOverBBoxExtent.w)));
		int num6 = Math.Max(0, Math.Min(val, (int)(scaleY * instanciedMeshHolders.Max16BitValueBBoxExtentScale)));
		int num7 = 255;
		int num8 = 65280;
		meshBlock.MeshColors.Add(new Color32((byte)(num & num7), (byte)((num & num8) >> 8), (byte)(num2 & num7), (byte)((num2 & num8) >> 8)));
		meshBlock.MeshColors.Add(new Color32((byte)(num3 & num7), (byte)((num3 & num8) >> 8), (byte)(num4 & num7), (byte)((num4 & num8) >> 8)));
		meshBlock.MeshColors.Add(new Color32((byte)(num5 & num7), (byte)((num5 & num8) >> 8), (byte)(num6 & num7), (byte)((num6 & num8) >> 8)));
		meshBlock.MeshColors.Add(color);
		meshBlock.MeshIndices.Add(meshIndex);
		Diagnostics.Assert(meshBlock.MeshColors.Count % InstanciedMeshHelpers.PositionForwardScaleZColorPixelsPerInstance == 0);
	}

	public static void AddInstance(InstanciedMeshHolders instanciedMeshHolders, InstanciedMeshBlock meshBlock, UnityEngine.Vector3 position, float scale, Color32 color, int meshIndex)
	{
		Diagnostics.Assert(!meshBlock.Closed);
		Diagnostics.Assert(meshBlock.PixelsPerInstance == InstanciedMeshHelpers.PositionScaleColorPixelsPerInstance);
		Diagnostics.Assert(meshBlock.MeshColors.Count % InstanciedMeshHelpers.PositionScaleColorPixelsPerInstance == 0);
		Diagnostics.Assert(instanciedMeshHolders.GetPixelsPerInstance(meshIndex) == InstanciedMeshHelpers.PositionScaleColorPixelsPerInstance);
		int val = 65535;
		int num = Math.Max(0, Math.Min(val, (int)((position.x - instanciedMeshHolders.BBoxMin.x) * instanciedMeshHolders.Max16BitValueOverBBoxExtent.x)));
		int num2 = Math.Max(0, Math.Min(val, (int)((position.y - instanciedMeshHolders.BBoxMin.y) * instanciedMeshHolders.Max16BitValueOverBBoxExtent.y)));
		int num3 = Math.Max(0, Math.Min(val, (int)((position.z - instanciedMeshHolders.BBoxMin.z) * instanciedMeshHolders.Max16BitValueOverBBoxExtent.z)));
		int num4 = Math.Max(0, Math.Min(val, (int)((scale - instanciedMeshHolders.BBoxMin.w) * instanciedMeshHolders.Max16BitValueOverBBoxExtent.w)));
		int num5 = 255;
		int num6 = 65280;
		meshBlock.MeshColors.Add(new Color32((byte)(num & num5), (byte)((num & num6) >> 8), (byte)(num2 & num5), (byte)((num2 & num6) >> 8)));
		meshBlock.MeshColors.Add(new Color32((byte)(num3 & num5), (byte)((num3 & num6) >> 8), (byte)(num4 & num5), (byte)((num4 & num6) >> 8)));
		meshBlock.MeshColors.Add(color);
		meshBlock.MeshIndices.Add(meshIndex);
		Diagnostics.Assert(meshBlock.MeshColors.Count % InstanciedMeshHelpers.PositionScaleColorPixelsPerInstance == 0);
	}

	public static void AddIconInstance(InstanciedMeshHolders instanciedMeshHolders, InstanciedMeshBlock meshBlock, UnityEngine.Vector3 position, UnityEngine.Vector2 forward2D, UnityEngine.Vector2 texCoord, float worldScale, float textureScale, int meshIndex, byte customData0 = 0, byte customData1 = 0)
	{
		Diagnostics.Assert(meshBlock.PixelsPerInstance == InstanciedMeshHelpers.PositionTexCoordWorldScaleTextureScalePixelsPerInstance);
		Diagnostics.Assert(meshBlock.MeshColors.Count % InstanciedMeshHelpers.PositionTexCoordWorldScaleTextureScalePixelsPerInstance == 0);
		Diagnostics.Assert(instanciedMeshHolders.GetPixelsPerInstance(meshIndex) == InstanciedMeshHelpers.PositionTexCoordWorldScaleTextureScalePixelsPerInstance);
		Diagnostics.Assert(meshBlock != null);
		Diagnostics.Assert(!meshBlock.Closed);
		Diagnostics.Assert(texCoord.x >= 0f);
		Diagnostics.Assert(texCoord.x <= 1f);
		Diagnostics.Assert(texCoord.y >= 0f);
		Diagnostics.Assert(texCoord.y <= 1f);
		Diagnostics.Assert(forward2D.x >= -1f);
		Diagnostics.Assert(forward2D.y >= -1f);
		Diagnostics.Assert(forward2D.x <= 1f);
		Diagnostics.Assert(forward2D.y <= 1f);
		int num = 65535;
		int num2 = Math.Max(0, Math.Min(num, (int)((position.x - instanciedMeshHolders.BBoxMin.x) * instanciedMeshHolders.Max16BitValueOverBBoxExtent.x)));
		int num3 = Math.Max(0, Math.Min(num, (int)((position.y - instanciedMeshHolders.BBoxMin.y) * instanciedMeshHolders.Max16BitValueOverBBoxExtent.y)));
		int num4 = Math.Max(0, Math.Min(num, (int)((position.z - instanciedMeshHolders.BBoxMin.z) * instanciedMeshHolders.Max16BitValueOverBBoxExtent.z)));
		int num5 = Math.Max(0, Math.Min(255, (int)(texCoord.x * 255f)));
		int num6 = Math.Max(0, Math.Min(255, (int)(texCoord.y * 255f)));
		int num7 = Math.Max(0, Math.Min(num, (int)(worldScale * instanciedMeshHolders.Max16BitValueBBoxExtentScale)));
		int num8 = Math.Max(0, Math.Min(num, (int)(textureScale * (float)num)));
		int num9 = Math.Max(0, Math.Min(255, (int)((0.5f * forward2D.x + 0.5f) * 255f)));
		int num10 = Math.Max(0, Math.Min(255, (int)((0.5f * forward2D.y + 0.5f) * 255f)));
		int num11 = 255;
		int num12 = 65280;
		meshBlock.MeshColors.Add(new Color32((byte)(num2 & num11), (byte)((num2 & num12) >> 8), (byte)(num3 & num11), (byte)((num3 & num12) >> 8)));
		meshBlock.MeshColors.Add(new Color32((byte)(num4 & num11), (byte)((num4 & num12) >> 8), (byte)num5, (byte)num6));
		meshBlock.MeshColors.Add(new Color32((byte)(num7 & num11), (byte)((num7 & num12) >> 8), (byte)(num8 & num11), (byte)((num8 & num12) >> 8)));
		meshBlock.MeshColors.Add(new Color32((byte)num9, (byte)num10, customData0, customData1));
		meshBlock.MeshIndices.Add(meshIndex);
		Diagnostics.Assert(meshBlock.MeshColors.Count % InstanciedMeshHelpers.PositionTexCoordWorldScaleTextureScalePixelsPerInstance == 0);
	}

	public static void AddLetterInstance(InstanciedMeshHolders instanciedMeshHolders, InstanciedMeshBlock meshBlock, UnityEngine.Vector3 position, int minPixelIndexX, int minPixelIndexY, int pixelCountX, int pixelCountY, UnityEngine.Vector2 worldScale, int meshIndex, bool useDynamicAtlas)
	{
		Diagnostics.Assert(meshBlock.PixelsPerInstance == InstanciedMeshHelpers.LetterPixelsPerInstance);
		Diagnostics.Assert(meshBlock.MeshColors.Count % InstanciedMeshHelpers.LetterPixelsPerInstance == 0);
		Diagnostics.Assert(instanciedMeshHolders.GetPixelsPerInstance(meshIndex) == InstanciedMeshHelpers.LetterPixelsPerInstance);
		Diagnostics.Assert(meshBlock != null);
		Diagnostics.Assert(!meshBlock.Closed);
		Diagnostics.Assert(minPixelIndexX >= 0);
		Diagnostics.Assert(minPixelIndexY >= 0);
		Diagnostics.Assert(pixelCountX >= 0);
		Diagnostics.Assert((float)pixelCountX <= 255f);
		Diagnostics.Assert(pixelCountY >= 0);
		Diagnostics.Assert((float)pixelCountY <= 255f);
		int val = 65535;
		int num = Math.Max(0, Math.Min(val, (int)((position.x - instanciedMeshHolders.BBoxMin.x) * instanciedMeshHolders.Max16BitValueOverBBoxExtent.x)));
		int num2 = Math.Max(0, Math.Min(val, (int)((position.y - instanciedMeshHolders.BBoxMin.y) * instanciedMeshHolders.Max16BitValueOverBBoxExtent.y)));
		int num3 = Math.Max(0, Math.Min(val, (int)((position.z - instanciedMeshHolders.BBoxMin.z) * instanciedMeshHolders.Max16BitValueOverBBoxExtent.z)));
		int num4 = 4096;
		int num5 = minPixelIndexX + ((!useDynamicAtlas) ? 0 : num4);
		int num6 = Math.Max(0, Math.Min(val, (int)(worldScale.x * instanciedMeshHolders.Max16BitValueBBoxExtentScale)));
		int num7 = Math.Max(0, Math.Min(val, (int)(worldScale.y * instanciedMeshHolders.Max16BitValueBBoxExtentScale)));
		int num8 = 255;
		int num9 = 65280;
		meshBlock.MeshColors.Add(new Color32((byte)(num & num8), (byte)((num & num9) >> 8), (byte)(num2 & num8), (byte)((num2 & num9) >> 8)));
		meshBlock.MeshColors.Add(new Color32((byte)(num3 & num8), (byte)((num3 & num9) >> 8), (byte)(num5 & num8), (byte)((num5 & num9) >> 8)));
		meshBlock.MeshColors.Add(new Color32((byte)(num6 & num8), (byte)((num6 & num9) >> 8), (byte)(num7 & num8), (byte)((num7 & num9) >> 8)));
		meshBlock.MeshColors.Add(new Color32((byte)(minPixelIndexY & num8), (byte)((minPixelIndexY & num9) >> 8), (byte)pixelCountX, (byte)pixelCountY));
		meshBlock.MeshIndices.Add(meshIndex);
		Diagnostics.Assert(meshBlock.MeshColors.Count % InstanciedMeshHelpers.LetterPixelsPerInstance == 0);
	}

	public static void ConvertBoundsToEncodingValues(InstanciedMeshHolders instanciedMeshHolders, Bounds meshBounds, out Int3 center, out Int3 size)
	{
		int x = (int)(meshBounds.center.x * instanciedMeshHolders.Max16BitValueOverBBoxExtent.x);
		int y = (int)(meshBounds.center.y * instanciedMeshHolders.Max16BitValueOverBBoxExtent.y);
		int z = (int)(meshBounds.center.z * instanciedMeshHolders.Max16BitValueOverBBoxExtent.z);
		int x2 = (int)(meshBounds.size.x * instanciedMeshHolders.Max16BitValueOverBBoxExtent.x);
		int y2 = (int)(meshBounds.size.y * instanciedMeshHolders.Max16BitValueOverBBoxExtent.y);
		int z2 = (int)(meshBounds.size.z * instanciedMeshHolders.Max16BitValueOverBBoxExtent.z);
		center = new Int3(x, y, z);
		size = new Int3(x2, y2, z2);
	}

	public static Int3 ConvertAbsoluteWorldPosToEncodingValues(InstanciedMeshHolders instanciedMeshHolders, UnityEngine.Vector3 position)
	{
		int val = 65535;
		int x = Math.Max(0, Math.Min(val, (int)((position.x - instanciedMeshHolders.BBoxMin.x) * instanciedMeshHolders.Max16BitValueOverBBoxExtent.x)));
		int y = Math.Max(0, Math.Min(val, (int)((position.y - instanciedMeshHolders.BBoxMin.y) * instanciedMeshHolders.Max16BitValueOverBBoxExtent.y)));
		int z = Math.Max(0, Math.Min(val, (int)((position.z - instanciedMeshHolders.BBoxMin.z) * instanciedMeshHolders.Max16BitValueOverBBoxExtent.z)));
		return new Int3(x, y, z);
	}

	public static int ConvertAbsoluteWorldPosYToEncodingValues(InstanciedMeshHolders instanciedMeshHolders, float y)
	{
		int val = 65535;
		return Math.Max(0, Math.Min(val, (int)((y - instanciedMeshHolders.BBoxMin.y) * instanciedMeshHolders.Max16BitValueOverBBoxExtent.y)));
	}

	public static readonly int PositionForwardScaleZPixelsPerInstance = 3;

	public static readonly int PositionForwardScaleZColorPixelsPerInstance = 4;

	public static readonly int PositionScaleColorPixelsPerInstance = 3;

	public static readonly int PositionTexCoordWorldScaleTextureScalePixelsPerInstance = 4;

	public static readonly int LetterPixelsPerInstance = 4;
}
