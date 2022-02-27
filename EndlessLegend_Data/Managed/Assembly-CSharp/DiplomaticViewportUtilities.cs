using System;
using System.Collections.Generic;
using UnityEngine;

public static class DiplomaticViewportUtilities
{
	public static Mesh[] SortMeshes(Vector3 cameraPos, GameObject gameObject)
	{
		List<Mesh> list = new List<Mesh>();
		foreach (MeshFilter meshFilter in gameObject.GetComponentsInChildren<MeshFilter>(true))
		{
			Mesh mesh = meshFilter.mesh;
			if (UnityGraphicObjectPool.Singleton != null)
			{
				UnityGraphicObjectPool.Singleton.AddExternalMeshIFN(mesh);
			}
			list.Add(mesh);
			Matrix4x4 localToWorldMatrix = meshFilter.transform.localToWorldMatrix;
			DiplomaticViewportUtilities.SortOneMesh(cameraPos, localToWorldMatrix, mesh);
			meshFilter.mesh = mesh;
		}
		foreach (SkinnedMeshRenderer skinnedMeshRenderer in gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true))
		{
			Mesh mesh2 = UnityEngine.Object.Instantiate<Mesh>(skinnedMeshRenderer.sharedMesh);
			list.Add(mesh2);
			if (UnityGraphicObjectPool.Singleton != null)
			{
				UnityGraphicObjectPool.Singleton.AddExternalMeshIFN(mesh2);
			}
			Matrix4x4 localToWorldMatrix2 = skinnedMeshRenderer.transform.localToWorldMatrix;
			DiplomaticViewportUtilities.SortOneMesh(cameraPos, localToWorldMatrix2, mesh2);
			skinnedMeshRenderer.sharedMesh = mesh2;
		}
		return list.ToArray();
	}

	public static void ReleaseSortedMesh(ref Mesh[] duplicatedMeshes)
	{
		int num = 0;
		while (duplicatedMeshes != null && num < duplicatedMeshes.Length)
		{
			Mesh mesh = duplicatedMeshes[num];
			if (UnityGraphicObjectPool.Singleton != null)
			{
				UnityGraphicObjectPool.Singleton.RemoveExternalMeshIFN(mesh);
			}
			UnityEngine.Object.DestroyImmediate(mesh, true);
			num++;
		}
		duplicatedMeshes = null;
	}

	public static void OffsetMaterialOrder(Renderer[] renderers, int offset)
	{
		foreach (Renderer renderer in renderers)
		{
			for (int j = 0; j < renderer.materials.Length; j++)
			{
				Material material = renderer.materials[j];
				material.renderQueue += offset;
			}
		}
	}

	public static void SetStatusMaterialProperty(GameObject gameObject, Vector4 status)
	{
		Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>(true);
		DiplomaticViewportUtilities.SetStatusMaterialProperty(componentsInChildren, status);
	}

	public static void SetStatusMaterialProperty(Renderer[] renderers, Vector4 status)
	{
		if (DiplomaticViewportUtilities.statusMaterialPropertyId == -1)
		{
			DiplomaticViewportUtilities.statusMaterialPropertyId = Shader.PropertyToID(DiplomaticViewportUtilities.statusMaterialPropertyName);
		}
		int num = 0;
		while (renderers != null && num < renderers.Length)
		{
			Renderer renderer = renderers[num];
			for (int i = 0; i < renderer.materials.Length; i++)
			{
				Material material = renderer.materials[i];
				if (material.HasProperty(DiplomaticViewportUtilities.statusMaterialPropertyId))
				{
					material.SetVector(DiplomaticViewportUtilities.statusMaterialPropertyId, status);
				}
			}
			num++;
		}
	}

	public static void SetFactionColorMaterialProperty(GameObject gameObject, Color factionColor)
	{
		Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>(true);
		DiplomaticViewportUtilities.SetFactionColorMaterialProperty(componentsInChildren, factionColor);
	}

	public static void SetFactionColorMaterialProperty(Renderer[] renderers, Color factionColor)
	{
		int num = 0;
		while (renderers != null && num < renderers.Length)
		{
			Renderer renderer = renderers[num];
			for (int i = 0; i < renderer.materials.Length; i++)
			{
				Material oneMaterial = renderer.materials[i];
				DiplomaticViewportUtilities.SetFactionColorMaterialProperty(oneMaterial, factionColor);
			}
			num++;
		}
	}

	public static void SetFactionColorMaterialProperty(Material oneMaterial, Color factionColor)
	{
		if (DiplomaticViewportUtilities.factionColorMaterialPropertyId == -1)
		{
			DiplomaticViewportUtilities.factionColorMaterialPropertyId = Shader.PropertyToID(DiplomaticViewportUtilities.factionColorMaterialPropertyName);
		}
		if (oneMaterial != null && oneMaterial.HasProperty(DiplomaticViewportUtilities.factionColorMaterialPropertyId))
		{
			oneMaterial.SetColor(DiplomaticViewportUtilities.factionColorMaterialPropertyId, factionColor);
		}
	}

	public static void SetShiftingFormMaterialProperty(GameObject gameObject, Vector4 shiftingForm)
	{
		Renderer[] componentsInChildren = gameObject.GetComponentsInChildren<Renderer>(true);
		DiplomaticViewportUtilities.SetShiftingFormMaterialProperty(componentsInChildren, shiftingForm);
	}

	public static void SetShiftingFormMaterialProperty(Renderer[] renderers, Vector4 shiftingForm)
	{
		int num = 0;
		while (renderers != null && num < renderers.Length)
		{
			Renderer renderer = renderers[num];
			for (int i = 0; i < renderer.materials.Length; i++)
			{
				Material oneMaterial = renderer.materials[i];
				DiplomaticViewportUtilities.SetShiftingFormMaterialProperty(oneMaterial, shiftingForm);
			}
			num++;
		}
	}

	public static void SetShiftingFormMaterialProperty(Material oneMaterial, Vector4 shiftingForm)
	{
		if (DiplomaticViewportUtilities.shiftingFormMaterialPropertyId == -1)
		{
			DiplomaticViewportUtilities.shiftingFormMaterialPropertyId = Shader.PropertyToID(DiplomaticViewportUtilities.shiftingFormMaterialPropertyName);
		}
		if (oneMaterial != null && oneMaterial.HasProperty(DiplomaticViewportUtilities.shiftingFormMaterialPropertyId))
		{
			oneMaterial.SetVector(DiplomaticViewportUtilities.shiftingFormMaterialPropertyId, shiftingForm);
		}
	}

	private static void SortOneMesh(Vector3 cameraPos, Matrix4x4 worldMatrix, Mesh duplicatedMesh)
	{
		List<KeyValuePair<int, float>> list = new List<KeyValuePair<int, float>>();
		Vector3[] vertices = duplicatedMesh.vertices;
		for (int i = 0; i < duplicatedMesh.subMeshCount; i++)
		{
			MeshTopology topology = duplicatedMesh.GetTopology(i);
			if (topology == MeshTopology.Triangles)
			{
				int[] indices = duplicatedMesh.GetIndices(i);
				int num = indices.Length / 3;
				for (int j = 0; j < num; j++)
				{
					Vector3 b4 = worldMatrix.MultiplyPoint(vertices[indices[j * 3]]);
					Vector3 b2 = worldMatrix.MultiplyPoint(vertices[indices[j * 3 + 1]]);
					Vector3 b3 = worldMatrix.MultiplyPoint(vertices[indices[j * 3 + 2]]);
					float magnitude = (cameraPos - b4).magnitude;
					float magnitude2 = (cameraPos - b2).magnitude;
					float magnitude3 = (cameraPos - b3).magnitude;
					float value = (magnitude + magnitude2 + magnitude3) / 3f;
					list.Add(new KeyValuePair<int, float>(j, value));
				}
				list.Sort((KeyValuePair<int, float> a, KeyValuePair<int, float> b) => b.Value.CompareTo(a.Value));
				int[] array = new int[indices.Length];
				for (int k = 0; k < num; k++)
				{
					int key = list[k].Key;
					array[k * 3] = indices[key * 3];
					array[k * 3 + 1] = indices[key * 3 + 1];
					array[k * 3 + 2] = indices[key * 3 + 2];
				}
				duplicatedMesh.SetIndices(array, MeshTopology.Triangles, i);
			}
		}
	}

	public static float SeasonChangeShiftingDelay = 1f;

	private static string statusMaterialPropertyName = "_Status";

	private static int statusMaterialPropertyId = -1;

	private static string factionColorMaterialPropertyName = "_FactionColor";

	private static int factionColorMaterialPropertyId = -1;

	private static string shiftingFormMaterialPropertyName = "_ShiftingFormStatus";

	private static int shiftingFormMaterialPropertyId = -1;
}
