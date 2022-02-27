using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Amplitude;
using UnityEngine;

public class InstanciedMeshHolders
{
	public InstanciedMeshHolders(int bigLinePixelCount, int bigLineCount, int smallLinePixelCount, int smallLineCount, Vector4 minBBox, Vector4 maxBBox, Vector3 minOffset, Vector3 maxOffset, InstanciedMeshHolders.GetOrCreateUnityEngineMesh meshAllocationHook, InstanciedMeshHolders.GetInstancingShader getInstancingShaderHook, bool verbose)
	{
		Diagnostics.Assert(minBBox.x <= maxBBox.x);
		Diagnostics.Assert(minBBox.y <= maxBBox.y);
		Diagnostics.Assert(minBBox.z <= maxBBox.z);
		Diagnostics.Assert(bigLinePixelCount > 0);
		Diagnostics.Assert(bigLinePixelCount % smallLinePixelCount == 0);
		Diagnostics.Assert(bigLinePixelCount >= smallLinePixelCount);
		Diagnostics.Assert(minOffset.x <= maxOffset.x);
		Diagnostics.Assert(minOffset.y <= maxOffset.y);
		Diagnostics.Assert(minOffset.z <= maxOffset.z);
		Diagnostics.Assert(getInstancingShaderHook != null);
		this.meshAllocationHook = meshAllocationHook;
		if (this.meshAllocationHook == null)
		{
			this.meshAllocationHook = new InstanciedMeshHolders.GetOrCreateUnityEngineMesh(this.DefaultGetOrCreateUnityEngineMesh);
		}
		this.getInstancingShaderHook = getInstancingShaderHook;
		this.verbose = verbose;
		this.BBoxMin = minBBox;
		this.BBoxMax = maxBBox;
		this.offsetMin = new Vector3(Math.Min(0f, minOffset.x), Math.Min(0f, minOffset.y), Math.Min(0f, minOffset.z));
		this.offsetMax = new Vector3(Math.Max(0f, maxOffset.x), Math.Max(0f, maxOffset.y), Math.Max(0f, maxOffset.z));
		float num = 65535f;
		float num2 = 255f;
		Vector4 vector = maxBBox - minBBox;
		this.Max16BitValueOverBBoxExtent = new Vector4(num / vector.x, num / vector.y, num / vector.z, num / vector.w);
		this.Max16BitValueBBoxExtentScale = num / maxBBox.w;
		this.Max8BitValueBBoxExtentScale = num2 / maxBBox.w;
		this.bigLinePixelCount = bigLinePixelCount;
		this.smallLinePixelCount = smallLinePixelCount;
		this.smallLinePerBigLine = bigLinePixelCount / smallLinePixelCount;
		this.bigLineCount = bigLineCount;
		this.smallLineCount = smallLineCount;
		this.materialPropertyBlock = new MaterialPropertyBlock();
		int num3 = smallLineCount / this.smallLinePerBigLine + ((smallLineCount % this.smallLinePerBigLine == 0) ? 0 : 1);
		int height = bigLineCount + num3;
		this.controlTexture = new Texture2D(bigLinePixelCount, height, TextureFormat.RGBA32, false);
		this.controlTexture.wrapMode = TextureWrapMode.Repeat;
		this.controlTexture.filterMode = FilterMode.Point;
		this.startUVMaterialBlockId = Shader.PropertyToID(InstanciedMeshHolders.StartUVShaderParameterName);
		this.offsetMaterialBlockId = Shader.PropertyToID(InstanciedMeshHolders.OffsetShaderParameterName);
		this.objectInfos = new List<InstanciedMeshHolders.ObjectInfo>();
		this.objectInfoTransformation = new List<Matrix4x4>();
		this.objectInfoInverseTransformation = new List<Matrix4x4>();
		this.objectInfoTransformations = new List<List<Matrix4x4>>();
		this.objectInfoInverseTransformations = new List<List<Matrix4x4>>();
		this.allocatedLines = new InstanciedMeshHolders.AllocatedLine[this.bigLineCount + this.smallLineCount];
		this.controlTextureColorBuffer = new Color32[this.controlTexture.width * this.controlTexture.height];
		this.firstNotAllocatedBigLineIndex = 0;
		for (int i = 0; i < this.bigLineCount; i++)
		{
			this.allocatedLines[i] = new InstanciedMeshHolders.AllocatedLine(this.bigLinePixelCount, this.controlTextureColorBuffer, i * this.controlTexture.width);
		}
		this.firstNotAllocatedSmallLineIndex = this.bigLineCount;
		for (int j = 0; j < this.smallLineCount; j++)
		{
			int num4 = j / this.smallLinePerBigLine + this.bigLineCount;
			int num5 = j % this.smallLinePerBigLine * this.smallLinePixelCount;
			this.allocatedLines[this.bigLineCount + j] = new InstanciedMeshHolders.AllocatedLine(this.smallLinePixelCount, this.controlTextureColorBuffer, num4 * this.controlTexture.width + num5);
		}
		this.matrix = Matrix4x4.identity;
		this.materialToMaterialInstancings = new List<InstanciedMeshHolders.MaterialToMaterialInstancing>();
		this.toDrawObjectInfos = new List<InstanciedMeshHolders.ObjectInfo>();
		InstanciedMeshBlock.PrepareInstanciedMeshBlockMemoryIFN();
		this.instanciedMeshBlockPool = new List<InstanciedMeshBlock>();
		this.meshClosed = false;
	}

	public int SmallMeshCount
	{
		get
		{
			return this.objectInfos.Count;
		}
	}

	public int BigLinePixelCount
	{
		get
		{
			return this.bigLinePixelCount;
		}
	}

	public int SmallLinePixelCount
	{
		get
		{
			return this.smallLinePixelCount;
		}
	}

	public int DrawCall
	{
		get
		{
			return this.drawCall;
		}
	}

	public int MaxPerBigLineInstance(int pixelsPerInstance)
	{
		Diagnostics.Assert(pixelsPerInstance > 0);
		return this.bigLinePixelCount / pixelsPerInstance;
	}

	public void LogDrawed(TextWriter textWriter)
	{
		textWriter.WriteLine("Mesh name; mesh capacity; draw; full capacity draw; relicat");
		for (int i = 0; i < this.toDrawObjectInfos.Count; i++)
		{
			InstanciedMeshHolders.ObjectInfo objectInfo = this.toDrawObjectInfos[i];
			int num = objectInfo.FirstAllocatedLineIndex;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			while (num != -1)
			{
				num4 += this.allocatedLines[num].InstanceCount;
				if (this.allocatedLines[num].InstanceCount < objectInfo.InstanceCapacity)
				{
					num3 = this.allocatedLines[num].InstanceCount;
				}
				else
				{
					num2++;
				}
				num = this.allocatedLines[num].NextAllocatedIndex;
			}
			string value = string.Format("{0};{1};{2};{3};{4}", new object[]
			{
				objectInfo.IdName,
				objectInfo.InstanceCapacity,
				num4,
				num2,
				num3
			});
			textWriter.WriteLine(value);
		}
	}

	public void LogContent(TextWriter textWriter)
	{
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < this.objectInfos.Count; i++)
		{
			num += this.objectInfos[i].MemoryUsedVB;
			num2 += this.objectInfos[i].MemoryUsedIB;
		}
		InstanciedMeshHolders.ObjectInfo.LogColumnName(textWriter, this.objectInfos.Count, num, num2);
		for (int j = 0; j < this.objectInfos.Count; j++)
		{
			this.objectInfos[j].LogContent(textWriter);
		}
		float num3 = (float)num / 1024f;
		float num4 = (float)num2 / 1024f;
		string value = string.Format("All;n/a;n/a;n/a;n/a;{0};{1}", num3.ToString("0.00"), num4.ToString("0.00"));
		textWriter.Write(value);
		textWriter.Write(textWriter.NewLine);
	}

	public IEnumerator CreateAllMesh(int yieldEveryMeshCount = 10)
	{
		Diagnostics.Assert(!this.meshClosed);
		Diagnostics.Assert(yieldEveryMeshCount >= 0);
		for (int i = 0; i < this.objectInfos.Count; i++)
		{
			InstanciedMeshHolders.ObjectInfo objectInfo = this.objectInfos[i];
			int maxInstancePerBigLine = this.bigLinePixelCount / objectInfo.PixelsPerInstance;
			int maxInstancePerSmallLine = this.smallLinePixelCount / objectInfo.PixelsPerInstance;
			objectInfo.CreateMesh(maxInstancePerBigLine, maxInstancePerSmallLine, this.objectInfoTransformation[i], this.objectInfoTransformations[i], this.MeshBounds(), InstanciedMeshHolders.LowerMeshCountSubdivision, this.meshAllocationHook);
			if (yieldEveryMeshCount > 0 && i % yieldEveryMeshCount == 0)
			{
				Diagnostics.Progress.SetProgress((float)(i + 1) / (float)this.objectInfos.Count);
				yield return null;
			}
		}
		this.meshClosed = true;
		Diagnostics.Progress.SetProgress(1f);
		yield break;
	}

	public void Unload()
	{
		Diagnostics.Assert(this.meshClosed);
		for (int i = 0; i < this.objectInfos.Count; i++)
		{
			this.objectInfos[i].Unload();
			this.objectInfos[i] = null;
		}
		this.objectInfos.Clear();
	}

	public int RetrieveMeshIndex(string idName, int pixelsPerInstance, Matrix4x4 transformation, Material material, int maxPerBatchSmallMeshInstance)
	{
		for (int i = 0; i < this.objectInfos.Count; i++)
		{
			if (this.objectInfos[i].IsSameMesh(idName, pixelsPerInstance, material, maxPerBatchSmallMeshInstance) && this.objectInfoTransformation[i] == transformation)
			{
				return i;
			}
		}
		return -1;
	}

	public void GetMeshInverseTransformation(int index, out Matrix4x4 inverseTransformation, out List<Matrix4x4> inverseTransformations)
	{
		Diagnostics.Assert(index >= 0);
		Diagnostics.Assert(index < this.objectInfoTransformation.Count);
		inverseTransformation = this.objectInfoInverseTransformation[index];
		inverseTransformations = this.objectInfoInverseTransformations[index];
	}

	public int GetObjectInfosMeshCount(int objectInfoIndex)
	{
		Diagnostics.Assert(objectInfoIndex >= 0);
		Diagnostics.Assert(objectInfoIndex < this.objectInfos.Count);
		return this.objectInfos[objectInfoIndex].MeshCount;
	}

	public Bounds GetMeshBounds(int objectInfoIndex, int meshIndex)
	{
		Diagnostics.Assert(objectInfoIndex >= 0);
		Diagnostics.Assert(objectInfoIndex < this.objectInfos.Count);
		Diagnostics.Assert(meshIndex < this.objectInfos[objectInfoIndex].MeshCount);
		return this.objectInfos[objectInfoIndex].GetMeshBounds(meshIndex);
	}

	public int GetPixelsPerInstance(int objectInfoIndex)
	{
		Diagnostics.Assert(objectInfoIndex >= 0);
		Diagnostics.Assert(objectInfoIndex < this.objectInfos.Count);
		return this.objectInfos[objectInfoIndex].PixelsPerInstance;
	}

	public int AddOrRetrieveSmallMesh(InstanciedMeshHolders.MeshAndSubMeshIndex smallMesh, int pixelsPerInstance, Matrix4x4 transformation, Material material, int maxPerBatchSmallMeshInstance)
	{
		int num = this.RetrieveMeshIndex(smallMesh.Mesh.name, pixelsPerInstance, transformation, material, maxPerBatchSmallMeshInstance);
		if (num == -1)
		{
			num = this.AddSmallMesh(smallMesh, pixelsPerInstance, transformation, material, maxPerBatchSmallMeshInstance);
		}
		return num;
	}

	public int AddSmallMesh(InstanciedMeshHolders.MeshAndSubMeshIndex smallMesh, int pixelsPerInstance, Matrix4x4 transformation, Material material, int maxPerBatchSmallMeshInstance)
	{
		int meshGenerationOption = 0;
		Material orCreateInstancingMaterialFromMaterial = this.GetOrCreateInstancingMaterialFromMaterial(material, pixelsPerInstance, out meshGenerationOption, smallMesh.Mesh.name);
		InstanciedMeshHolders.ObjectInfo objectInfo = new InstanciedMeshHolders.ObjectInfo(smallMesh, pixelsPerInstance, maxPerBatchSmallMeshInstance, orCreateInstancingMaterialFromMaterial, material, meshGenerationOption);
		int count = this.objectInfos.Count;
		this.objectInfos.Add(objectInfo);
		this.objectInfoTransformation.Add(transformation);
		Matrix4x4 matrix4x = Matrix4x4.Inverse(transformation);
		Diagnostics.Assert(matrix4x != Matrix4x4.zero);
		this.objectInfoInverseTransformation.Add(matrix4x);
		this.objectInfoTransformations.Add(null);
		this.objectInfoInverseTransformations.Add(null);
		if (this.meshClosed)
		{
			int maxInstancePerBigLine = this.bigLinePixelCount / objectInfo.PixelsPerInstance;
			int maxInstancePerSmallLine = this.smallLinePixelCount / objectInfo.PixelsPerInstance;
			objectInfo.CreateMesh(maxInstancePerBigLine, maxInstancePerSmallLine, this.objectInfoTransformation[count], this.objectInfoTransformations[count], this.MeshBounds(), InstanciedMeshHolders.LowerMeshCountSubdivision, this.meshAllocationHook);
		}
		return count;
	}

	public int AddSmallMeshes(string idName, List<InstanciedMeshHolders.MeshAndSubMeshIndex> meshes, int pixelsPerInstance, Matrix4x4 transformation, List<Matrix4x4> transformations, Material material, int maxPerBatchSmallMeshInstance)
	{
		Diagnostics.Assert(this.objectInfos.Count == this.objectInfoTransformation.Count);
		Diagnostics.Assert(this.objectInfos.Count == this.objectInfoTransformations.Count);
		Diagnostics.Assert(this.objectInfos.Count == this.objectInfoInverseTransformation.Count);
		Diagnostics.Assert(this.objectInfos.Count == this.objectInfoInverseTransformations.Count);
		int meshGenerationOption = 0;
		Material orCreateInstancingMaterialFromMaterial = this.GetOrCreateInstancingMaterialFromMaterial(material, pixelsPerInstance, out meshGenerationOption, idName);
		InstanciedMeshHolders.ObjectInfo objectInfo = new InstanciedMeshHolders.ObjectInfo(idName, meshes, pixelsPerInstance, maxPerBatchSmallMeshInstance, orCreateInstancingMaterialFromMaterial, material, meshGenerationOption);
		int count = this.objectInfos.Count;
		this.objectInfos.Add(objectInfo);
		this.objectInfoTransformation.Add(transformation);
		Matrix4x4 matrix4x = Matrix4x4.Inverse(transformation);
		Diagnostics.Assert(matrix4x != Matrix4x4.zero);
		this.objectInfoInverseTransformation.Add(matrix4x);
		this.objectInfoTransformations.Add(transformations);
		if (transformations != null)
		{
			List<Matrix4x4> list = new List<Matrix4x4>();
			for (int i = 0; i < transformations.Count; i++)
			{
				Matrix4x4 m = transformations[i];
				Matrix4x4 matrix4x2 = Matrix4x4.Inverse(m);
				Diagnostics.Assert(matrix4x2 != Matrix4x4.zero);
				list.Add(matrix4x2);
			}
			this.objectInfoInverseTransformations.Add(list);
		}
		else
		{
			this.objectInfoInverseTransformations.Add(null);
		}
		if (this.meshClosed)
		{
			int maxInstancePerBigLine = this.bigLinePixelCount / objectInfo.PixelsPerInstance;
			int maxInstancePerSmallLine = this.smallLinePixelCount / objectInfo.PixelsPerInstance;
			objectInfo.CreateMesh(maxInstancePerBigLine, maxInstancePerSmallLine, this.objectInfoTransformation[count], this.objectInfoTransformations[count], this.MeshBounds(), InstanciedMeshHolders.LowerMeshCountSubdivision, this.meshAllocationHook);
		}
		return count;
	}

	public void AddOccurenceExpectation(int meshIndex, double expectation)
	{
		this.objectInfos[meshIndex].OccurenceExpectation += expectation;
	}

	public double OccurrenceExpectation(int meshIndex)
	{
		return this.objectInfos[meshIndex].OccurenceExpectation;
	}

	public void OverrideMaxPerBatchSmallMeshInstance(int meshIndex, int value)
	{
		this.objectInfos[meshIndex].OverrideMaxPerBatchSmallMeshInstance(value);
	}

	public void SetMatrixAndOffset(Matrix4x4 matrix, Vector3 secondIndexOffset)
	{
		this.matrix = matrix;
		this.secondIndexOffset = secondIndexOffset;
	}

	public void ApplyParametersToMaterial(Material material, string nameForErrorReporting)
	{
		Diagnostics.Assert(material != null);
		material.SetTexture(InstanciedMeshHolders.ControlTexShaderParameterName, this.controlTexture);
		material.SetVector(InstanciedMeshHolders.MinBBoxShaderParameterName, this.BBoxMin);
		material.SetVector(InstanciedMeshHolders.MaxBBoxShaderParameterName, this.BBoxMax);
	}

	public Matrix4x4 GetMatrix()
	{
		return this.matrix;
	}

	public Vector3 GetSecondIndexOffset()
	{
		return this.secondIndexOffset;
	}

	public void AddInstanciedMeshBlock(InstanciedMeshBlock meshBlock)
	{
		Diagnostics.Assert(!meshBlock.Inserted);
		Diagnostics.Assert(meshBlock.Closed);
		Diagnostics.Assert(meshBlock.MatrixIndex >= 0);
		Diagnostics.Assert(meshBlock.MatrixIndex < 2);
		if (meshBlock.MatrixIndex == 0)
		{
			meshBlock.InsertIn(this.firstInstanciedMeshBlockMatrix0);
			this.firstInstanciedMeshBlockMatrix0 = meshBlock;
		}
		else
		{
			meshBlock.InsertIn(this.firstInstanciedMeshBlockMatrix1);
			this.firstInstanciedMeshBlockMatrix1 = meshBlock;
		}
		Diagnostics.Assert(meshBlock.Inserted);
	}

	public void RemoveInstanciedMeshBlock(InstanciedMeshBlock meshBlock)
	{
		Diagnostics.Assert(meshBlock.Inserted);
		if (this.firstInstanciedMeshBlockMatrix0 == meshBlock)
		{
			this.firstInstanciedMeshBlockMatrix0 = meshBlock.Next;
		}
		if (this.firstInstanciedMeshBlockMatrix1 == meshBlock)
		{
			this.firstInstanciedMeshBlockMatrix1 = meshBlock.Next;
		}
		meshBlock.Remove();
		Diagnostics.Assert(!meshBlock.Inserted);
	}

	public InstanciedMeshBlock GetOrCreateInstanciedMeshBlock(int matrixIndex, ulong layerMask, int pixelsPerInstance)
	{
		Diagnostics.Assert(pixelsPerInstance == 3 || pixelsPerInstance == 4);
		Diagnostics.Assert(layerMask != 0UL);
		Diagnostics.Assert(matrixIndex < 2);
		List<InstanciedMeshBlock> list = this.instanciedMeshBlockPool;
		InstanciedMeshBlock instanciedMeshBlock;
		if (list.Count > 0)
		{
			instanciedMeshBlock = list[list.Count - 1];
			list.RemoveAt(list.Count - 1);
			instanciedMeshBlock.PullBackFromPool(pixelsPerInstance);
			Diagnostics.Assert(!instanciedMeshBlock.Closed);
			Diagnostics.Assert(!instanciedMeshBlock.Inserted);
		}
		else
		{
			instanciedMeshBlock = new InstanciedMeshBlock(pixelsPerInstance);
		}
		Diagnostics.Assert(instanciedMeshBlock.PixelsPerInstance == pixelsPerInstance);
		instanciedMeshBlock.SetNormalMatrixIndex(matrixIndex, this);
		instanciedMeshBlock.SetLayerMask(layerMask);
		return instanciedMeshBlock;
	}

	public void ReleaseInstanciedMeshBlock(InstanciedMeshBlock meshBlock)
	{
		if (meshBlock.Inserted)
		{
			this.RemoveInstanciedMeshBlock(meshBlock);
		}
		meshBlock.PutBackInPool();
		this.instanciedMeshBlockPool.Add(meshBlock);
	}

	public void Close(ulong layerMask)
	{
		this.DrawInstanciedMeshBlocks(layerMask);
		for (int i = 0; i < this.firstNotAllocatedBigLineIndex; i++)
		{
			this.allocatedLines[i].ClearRemainingLine(this.objectInfos);
		}
		for (int j = this.bigLineCount; j < this.firstNotAllocatedSmallLineIndex; j++)
		{
			this.allocatedLines[j].ClearRemainingLine(this.objectInfos);
		}
		this.controlTexture.SetPixels32(this.controlTextureColorBuffer);
		this.controlTexture.Apply();
	}

	public void Clear(bool forceClearTexture)
	{
		if (forceClearTexture)
		{
			int num = this.controlTexture.width * this.controlTexture.height;
			Color32 color = new Color32(0, 0, 0, 0);
			for (int i = 0; i < num; i++)
			{
				this.controlTextureColorBuffer[i] = color;
			}
		}
		for (int j = 0; j < this.toDrawObjectInfos.Count; j++)
		{
			this.toDrawObjectInfos[j].Clear();
		}
		this.toDrawObjectInfos.Clear();
		this.firstNotAllocatedBigLineIndex = 0;
		this.firstNotAllocatedSmallLineIndex = this.bigLineCount;
		this.allocFailedMessageGenerated = false;
	}

	public void Draw(Camera camera, bool sort)
	{
		Diagnostics.Assert(this.meshClosed);
		Vector4 value = new Vector4(this.secondIndexOffset.x, this.secondIndexOffset.y, this.secondIndexOffset.z, 0f);
		this.drawCall = 0;
		if (sort)
		{
			this.toDrawObjectInfos.Sort();
		}
		for (int i = 0; i < this.toDrawObjectInfos.Count; i++)
		{
			InstanciedMeshHolders.ObjectInfo objectInfo = this.toDrawObjectInfos[i];
			int num = objectInfo.FirstAllocatedLineIndex;
			int num2 = 0;
			while (num != -1)
			{
				int drawCallCount = this.allocatedLines[num].DrawCallCount;
				for (int j = 0; j < drawCallCount; j++)
				{
					Vector4 zero = Vector4.zero;
					int num3 = j * this.allocatedLines[num].MeshInstanceCapacity;
					int num4 = num3 * this.allocatedLines[num].MeshPixelsPerInstance;
					if (num < this.bigLineCount)
					{
						zero = new Vector4((float)num4 / (float)this.controlTexture.width, (float)num / (float)this.controlTexture.height, 0f, 0f);
					}
					else
					{
						int num5 = num - this.bigLineCount;
						int num6 = this.bigLineCount + num5 / this.smallLinePerBigLine;
						int num7 = num5 % this.smallLinePerBigLine * this.smallLinePixelCount + num4;
						zero = new Vector4((float)num7 / (float)this.controlTexture.width, (float)num6 / (float)this.controlTexture.height, 0f, 0f);
					}
					this.materialPropertyBlock.Clear();
					this.materialPropertyBlock.SetVector(this.startUVMaterialBlockId, zero);
					value.w = (float)this.allocatedLines[num].FirstIndexUsingOffset - (float)num3 - 0.5f;
					this.materialPropertyBlock.SetVector(this.offsetMaterialBlockId, value);
					Diagnostics.Assert(this.allocatedLines[num].InstanceCount > 0);
					int num8 = Math.Min(this.allocatedLines[num].MeshInstanceCapacity, this.allocatedLines[num].InstanceCount - num3);
					num2 += num8;
					objectInfo.DrawPrimitive(camera, this.materialPropertyBlock, this.matrix, num8);
					this.drawCall++;
				}
				num = this.allocatedLines[num].NextAllocatedIndex;
			}
			objectInfo.LastFrameInstanceDrawed = num2;
		}
	}

	public Material GetInstancingMaterialFromMaterialIFP(Material material, int pixelsPerInstance)
	{
		for (int i = 0; i < this.materialToMaterialInstancings.Count; i++)
		{
			if (this.materialToMaterialInstancings[i].OriginalMaterial.name == material.name && this.materialToMaterialInstancings[i].PixelsPerInstance == pixelsPerInstance)
			{
				return this.materialToMaterialInstancings[i].InstancingMaterial;
			}
		}
		return null;
	}

	private void DrawInstanciedMeshBlocks(ulong layerMask)
	{
		this.DrawInstanciedMeshBlocks(layerMask, this.firstInstanciedMeshBlockMatrix0, 0);
		this.DrawInstanciedMeshBlocks(layerMask, this.firstInstanciedMeshBlockMatrix1, 1);
	}

	private void DrawInstanciedMeshBlocks(ulong layerMask, InstanciedMeshBlock firstInstanciedMeshBlock, int matrixIndex)
	{
		InstanciedMeshBlock instanciedMeshBlock = firstInstanciedMeshBlock;
		while (instanciedMeshBlock != null)
		{
			if ((instanciedMeshBlock.LayerMask & layerMask) == 0UL)
			{
				instanciedMeshBlock = instanciedMeshBlock.Next;
			}
			else
			{
				Diagnostics.Assert(instanciedMeshBlock.MatrixIndex == matrixIndex);
				if (instanciedMeshBlock.HasPerTypeIndices)
				{
					int num = instanciedMeshBlock.PerTypeIndices.Count / 2;
					int pixelsPerInstance = instanciedMeshBlock.PixelsPerInstance;
					int num2 = 0;
					for (int i = 0; i < num; i++)
					{
						int meshIndex = instanciedMeshBlock.PerTypeIndices[i * 2];
						int j = instanciedMeshBlock.PerTypeIndices[i * 2 + 1] * pixelsPerInstance;
						while (j > num2)
						{
							int orAllocLineIndex = this.GetOrAllocLineIndex(meshIndex, matrixIndex);
							if (orAllocLineIndex >= 0)
							{
								Diagnostics.Assert(!this.allocatedLines[orAllocLineIndex].Full);
								int num3 = this.allocatedLines[orAllocLineIndex].AddPixels(instanciedMeshBlock.MeshColors, num2, j - num2);
								num2 += num3;
							}
							else
							{
								num2 = j;
							}
						}
						Diagnostics.Assert(j == num2);
					}
				}
				else
				{
					int count = instanciedMeshBlock.MeshIndices.Count;
					int pixelsPerInstance2 = instanciedMeshBlock.PixelsPerInstance;
					for (int k = 0; k < count; k++)
					{
						int meshIndex2 = instanciedMeshBlock.MeshIndices[k];
						int orAllocLineIndex2 = this.GetOrAllocLineIndex(meshIndex2, matrixIndex);
						if (orAllocLineIndex2 >= 0)
						{
							int num4 = this.allocatedLines[orAllocLineIndex2].AddPixels(instanciedMeshBlock.MeshColors, k * pixelsPerInstance2, pixelsPerInstance2);
						}
					}
				}
				instanciedMeshBlock = instanciedMeshBlock.Next;
			}
		}
	}

	private int GetOrAllocLineIndex(int meshIndex, int matrixIndex)
	{
		Diagnostics.Assert(matrixIndex >= 0);
		Diagnostics.Assert(matrixIndex < 2);
		Diagnostics.Assert(this.allocatedLines.Length == this.bigLineCount + this.smallLineCount);
		if (meshIndex < 0 || meshIndex >= this.objectInfos.Count)
		{
			return -1;
		}
		InstanciedMeshHolders.ObjectInfo objectInfo = this.objectInfos[meshIndex];
		int currentAllocatedLineIndex = objectInfo.CurrentAllocatedLineIndex;
		if (currentAllocatedLineIndex == -1)
		{
			Diagnostics.Assert(objectInfo.InstanceCapacity * objectInfo.PixelsPerInstance <= this.bigLinePixelCount);
			bool flag = objectInfo.LastFrameInstanceDrawed * objectInfo.PixelsPerInstance > this.smallLinePixelCount || objectInfo.InstanceCapacityInSmallLine == 0;
			if (flag && this.firstNotAllocatedBigLineIndex < this.bigLineCount)
			{
				currentAllocatedLineIndex = this.firstNotAllocatedBigLineIndex;
				objectInfo.CurrentAllocatedLineIndex = this.firstNotAllocatedBigLineIndex;
				objectInfo.FirstAllocatedLineIndex = this.firstNotAllocatedBigLineIndex;
				this.toDrawObjectInfos.Add(objectInfo);
				Diagnostics.Assert(currentAllocatedLineIndex < this.bigLineCount);
				this.allocatedLines[currentAllocatedLineIndex].SetMeshIndex(meshIndex, objectInfo.InstanceCapacity, objectInfo.PixelsPerInstance, objectInfo.MeshCount);
				Diagnostics.Assert(this.allocatedLines[currentAllocatedLineIndex].MeshIndex == meshIndex);
				this.firstNotAllocatedBigLineIndex++;
			}
			else
			{
				if (flag || this.firstNotAllocatedSmallLineIndex - this.bigLineCount >= this.smallLineCount)
				{
					if (this.verbose && !this.allocFailedMessageGenerated)
					{
						Diagnostics.Log("InstancingMeshHolders.GetOrAllocLineIndex : alloc failed 0", new object[]
						{
							(!flag) ? "SmallLine" : "BigLine"
						});
						this.allocFailedMessageGenerated = true;
					}
					return -1;
				}
				currentAllocatedLineIndex = this.firstNotAllocatedSmallLineIndex;
				objectInfo.CurrentAllocatedLineIndex = this.firstNotAllocatedSmallLineIndex;
				objectInfo.FirstAllocatedLineIndex = this.firstNotAllocatedSmallLineIndex;
				this.toDrawObjectInfos.Add(objectInfo);
				Diagnostics.Assert(currentAllocatedLineIndex >= this.bigLineCount);
				Diagnostics.Assert(currentAllocatedLineIndex < this.bigLineCount + this.smallLineCount);
				this.allocatedLines[currentAllocatedLineIndex].SetMeshIndex(meshIndex, objectInfo.InstanceCapacityInSmallLine, objectInfo.PixelsPerInstance, objectInfo.MeshCount);
				Diagnostics.Assert(this.allocatedLines[currentAllocatedLineIndex].MeshIndex == meshIndex);
				this.firstNotAllocatedSmallLineIndex++;
			}
		}
		else if (this.allocatedLines[currentAllocatedLineIndex].Full)
		{
			bool flag2 = objectInfo.InstanceCapacity * objectInfo.PixelsPerInstance > this.smallLinePixelCount;
			if (flag2 && this.firstNotAllocatedBigLineIndex < this.bigLineCount)
			{
				this.allocatedLines[currentAllocatedLineIndex].SetNextAllocatedIndex(this.firstNotAllocatedBigLineIndex);
				currentAllocatedLineIndex = this.firstNotAllocatedBigLineIndex;
				objectInfo.CurrentAllocatedLineIndex = this.firstNotAllocatedBigLineIndex;
				Diagnostics.Assert(currentAllocatedLineIndex < this.bigLineCount);
				this.allocatedLines[currentAllocatedLineIndex].SetMeshIndex(meshIndex, objectInfo.InstanceCapacity, objectInfo.PixelsPerInstance, objectInfo.MeshCount);
				Diagnostics.Assert(this.allocatedLines[currentAllocatedLineIndex].MeshIndex == meshIndex);
				this.firstNotAllocatedBigLineIndex++;
			}
			else
			{
				if (flag2 || this.firstNotAllocatedSmallLineIndex - this.bigLineCount >= this.smallLineCount)
				{
					if (this.verbose && !this.allocFailedMessageGenerated)
					{
						Diagnostics.Log("InstancingMeshHolders.GetOrAllocLineIndex : alloc failed 1 {0}", new object[]
						{
							(!flag2) ? "SmallLine" : "BigLine"
						});
						this.allocFailedMessageGenerated = true;
					}
					return -1;
				}
				this.allocatedLines[currentAllocatedLineIndex].SetNextAllocatedIndex(this.firstNotAllocatedSmallLineIndex);
				currentAllocatedLineIndex = this.firstNotAllocatedSmallLineIndex;
				objectInfo.CurrentAllocatedLineIndex = this.firstNotAllocatedSmallLineIndex;
				Diagnostics.Assert(currentAllocatedLineIndex >= this.bigLineCount);
				Diagnostics.Assert(currentAllocatedLineIndex < this.bigLineCount + this.smallLineCount);
				this.allocatedLines[currentAllocatedLineIndex].SetMeshIndex(meshIndex, objectInfo.InstanceCapacityInSmallLine, objectInfo.PixelsPerInstance, objectInfo.MeshCount);
				Diagnostics.Assert(this.allocatedLines[currentAllocatedLineIndex].MeshIndex == meshIndex);
				this.firstNotAllocatedSmallLineIndex++;
			}
		}
		if (matrixIndex != 0)
		{
			bool flag3 = this.allocatedLines[currentAllocatedLineIndex].SetFirstIndexUsingOffsetToCurrentColumnIFN();
			Diagnostics.Assert(!flag3 || this.allocatedLines[currentAllocatedLineIndex].InstanceCount % objectInfo.MeshCount == 0);
		}
		Diagnostics.Assert(this.allocatedLines[currentAllocatedLineIndex].NextAllocatedIndex == -1);
		return currentAllocatedLineIndex;
	}

	private Material GetOrCreateInstancingMaterialFromMaterial(Material material, int pixelsPerInstance, out int meshGenerationOptions, string nameForErrorReporting)
	{
		for (int i = 0; i < this.materialToMaterialInstancings.Count; i++)
		{
			if (this.materialToMaterialInstancings[i].OriginalMaterial.name == material.name && this.materialToMaterialInstancings[i].PixelsPerInstance == pixelsPerInstance)
			{
				meshGenerationOptions = this.materialToMaterialInstancings[i].MeshGenerationOptions;
				return this.materialToMaterialInstancings[i].InstancingMaterial;
			}
		}
		Material material2 = new Material(material);
		Shader shader = null;
		meshGenerationOptions = 0;
		this.getInstancingShaderHook(material, pixelsPerInstance, out shader, out meshGenerationOptions);
		material2.shader = shader;
		this.ApplyParametersToMaterial(material2, nameForErrorReporting);
		this.materialToMaterialInstancings.Add(new InstanciedMeshHolders.MaterialToMaterialInstancing(material, material2, pixelsPerInstance, meshGenerationOptions));
		return material2;
	}

	private Bounds MeshBounds()
	{
		Vector3 vector = this.BBoxMin + this.offsetMin;
		Vector3 vector2 = this.BBoxMax + this.offsetMax;
		return new Bounds((vector + vector2) * 0.5f, vector2 - vector);
	}

	private Mesh DefaultGetOrCreateUnityEngineMesh()
	{
		return new Mesh();
	}

	public static readonly int MeshCreationOptionNoOption = 0;

	public static readonly int MeshCreationOptionDropUV2 = 1;

	public static readonly int MeshCreationOptionDropNormal = 2;

	public static readonly int MeshCreationOptionDropTangent = 4;

	public static readonly int MeshCreationOptionEncodeTangentInColor = 8;

	public static readonly int MeshCreationOptionEncodeNormalInColor = 16;

	public static readonly string[] MeshCreationOptions = new string[]
	{
		"DropUV2",
		"DropNormal",
		"DropTangent",
		"EncodeTangentInColor",
		"EncodeNormalInColor"
	};

	public readonly Vector4 BBoxMin;

	public readonly Vector4 BBoxMax;

	public readonly Vector4 Max16BitValueOverBBoxExtent;

	public readonly float Max16BitValueBBoxExtentScale;

	public readonly float Max8BitValueBBoxExtentScale;

	private static readonly string StartUVShaderParameterName = "_StartUV";

	private static readonly string OffsetShaderParameterName = "_Offset";

	private static readonly string ControlTexShaderParameterName = "_ControlTex";

	private static readonly string MinBBoxShaderParameterName = "_MinBBox";

	private static readonly string MaxBBoxShaderParameterName = "_MaxBBox";

	private static readonly int LowerMeshCountSubdivision = 8;

	private Texture2D controlTexture;

	private Color32[] controlTextureColorBuffer;

	private MaterialPropertyBlock materialPropertyBlock;

	private InstanciedMeshHolders.AllocatedLine[] allocatedLines;

	private int bigLinePixelCount;

	private int firstNotAllocatedBigLineIndex;

	private int bigLineCount;

	private int smallLinePixelCount;

	private int firstNotAllocatedSmallLineIndex;

	private int smallLineCount;

	private int smallLinePerBigLine;

	private List<InstanciedMeshHolders.ObjectInfo> objectInfos;

	private List<Matrix4x4> objectInfoTransformation;

	private List<Matrix4x4> objectInfoInverseTransformation;

	private List<List<Matrix4x4>> objectInfoTransformations;

	private List<List<Matrix4x4>> objectInfoInverseTransformations;

	private List<InstanciedMeshHolders.ObjectInfo> toDrawObjectInfos;

	private List<InstanciedMeshBlock> instanciedMeshBlockPool;

	private List<InstanciedMeshHolders.MaterialToMaterialInstancing> materialToMaterialInstancings;

	private Matrix4x4 matrix;

	private Vector3 secondIndexOffset;

	private Vector3 offsetMin;

	private Vector3 offsetMax;

	private int offsetMaterialBlockId;

	private int startUVMaterialBlockId;

	private bool meshClosed;

	private bool verbose;

	private bool allocFailedMessageGenerated;

	private int drawCall;

	private InstanciedMeshHolders.GetOrCreateUnityEngineMesh meshAllocationHook;

	private InstanciedMeshHolders.GetInstancingShader getInstancingShaderHook;

	private InstanciedMeshBlock firstInstanciedMeshBlockMatrix0;

	private InstanciedMeshBlock firstInstanciedMeshBlockMatrix1;

	public struct MeshAndSubMeshIndex
	{
		public MeshAndSubMeshIndex(Mesh mesh, int subMeshIndex)
		{
			this.Mesh = mesh;
			this.SubMeshIndex = subMeshIndex;
		}

		public Mesh Mesh;

		public int SubMeshIndex;
	}

	public struct MaterialToMaterialInstancing
	{
		public MaterialToMaterialInstancing(Material originalMaterial, Material instancingMaterial, int pixelsPerInstance, int meshGenerationOptions)
		{
			this.OriginalMaterial = originalMaterial;
			this.InstancingMaterial = instancingMaterial;
			this.PixelsPerInstance = pixelsPerInstance;
			this.MeshGenerationOptions = meshGenerationOptions;
		}

		public readonly Material OriginalMaterial;

		public readonly Material InstancingMaterial;

		public readonly int PixelsPerInstance;

		public readonly int MeshGenerationOptions;
	}

	private struct AllocatedLine
	{
		public AllocatedLine(int pixelCount, Color32[] controlTextureColorBuffer, int colorLineStartIndex)
		{
			this.instanceCapacity = 0;
			this.meshInstanceCapacity = 0;
			this.colorCapacity = 0;
			this.meshIndex = -1;
			this.meshPixelsPerInstance = 0;
			this.maxColorCapacity = pixelCount;
			this.controlTextureColorBuffer = controlTextureColorBuffer;
			this.colorLineStartIndex = colorLineStartIndex;
			Diagnostics.Assert(this.colorLineStartIndex + this.maxColorCapacity <= this.controlTextureColorBuffer.Length);
			this.currentColumn = 0;
			this.firstIndexUsingOffset = 0;
			this.nextAllocatedIndex = -1;
		}

		public int MeshIndex
		{
			get
			{
				return this.meshIndex;
			}
		}

		public int NextAllocatedIndex
		{
			get
			{
				return this.nextAllocatedIndex;
			}
		}

		public int MeshPixelsPerInstance
		{
			get
			{
				return this.meshPixelsPerInstance;
			}
		}

		public int DrawCallCount
		{
			get
			{
				return ((this.InstanceCount % this.MeshInstanceCapacity != 0) ? 1 : 0) + this.InstanceCount / this.MeshInstanceCapacity;
			}
		}

		public int InstanceCount
		{
			get
			{
				Diagnostics.Assert(this.meshPixelsPerInstance > 0);
				return this.currentColumn / this.meshPixelsPerInstance;
			}
		}

		public int MeshInstanceCapacity
		{
			get
			{
				return this.meshInstanceCapacity;
			}
		}

		public int FirstIndexUsingOffset
		{
			get
			{
				return this.firstIndexUsingOffset;
			}
		}

		public bool Full
		{
			get
			{
				return this.currentColumn >= this.colorCapacity;
			}
		}

		public void SetMeshIndex(int meshIndex, int meshInstanceCapacity, int pixelsPerInstance, int meshCount)
		{
			Diagnostics.Assert(meshInstanceCapacity * pixelsPerInstance <= this.maxColorCapacity);
			Diagnostics.Assert(meshInstanceCapacity % meshCount == 0);
			Diagnostics.Assert(meshInstanceCapacity >= meshCount);
			Diagnostics.Assert(meshCount > 0);
			Diagnostics.Assert(pixelsPerInstance > 0);
			Diagnostics.Assert(this.maxColorCapacity >= meshCount * pixelsPerInstance);
			int num = this.maxColorCapacity / (pixelsPerInstance * meshCount);
			int num2 = meshInstanceCapacity / meshCount;
			int num3 = num / num2;
			Diagnostics.Assert(num3 > 0);
			int num4 = num3 * num2;
			Diagnostics.Assert(num4 > 0);
			this.meshIndex = meshIndex;
			this.meshInstanceCapacity = meshInstanceCapacity;
			this.instanceCapacity = num4 * meshCount;
			this.meshPixelsPerInstance = pixelsPerInstance;
			this.colorCapacity = this.instanceCapacity * pixelsPerInstance;
			Diagnostics.Assert(this.colorCapacity <= this.maxColorCapacity);
			Diagnostics.Assert(this.colorCapacity > 0);
			this.currentColumn = 0;
			this.nextAllocatedIndex = -1;
			this.firstIndexUsingOffset = this.maxColorCapacity / pixelsPerInstance;
		}

		public void SetNextAllocatedIndex(int index)
		{
			this.nextAllocatedIndex = index;
		}

		public bool SetFirstIndexUsingOffsetToCurrentColumnIFN()
		{
			Diagnostics.Assert(this.currentColumn % this.meshPixelsPerInstance == 0);
			Diagnostics.Assert(this.meshPixelsPerInstance > 0);
			int num = this.currentColumn / this.meshPixelsPerInstance;
			if (num < this.firstIndexUsingOffset)
			{
				Diagnostics.Assert(this.firstIndexUsingOffset == this.maxColorCapacity / this.meshPixelsPerInstance);
				this.firstIndexUsingOffset = num;
				return true;
			}
			return false;
		}

		public int AddPixels(List<Color32> colors, int firstIndex, int count)
		{
			Diagnostics.Assert(this.meshPixelsPerInstance > 0);
			Diagnostics.Assert(count > 0);
			Diagnostics.Assert(count % this.meshPixelsPerInstance == 0);
			Diagnostics.Assert(this.colorCapacity % this.meshPixelsPerInstance == 0);
			Diagnostics.Assert(this.colorCapacity <= this.maxColorCapacity);
			Diagnostics.Assert(this.currentColumn <= this.colorCapacity);
			int num = this.colorCapacity - this.currentColumn;
			count = ((count <= num) ? count : num);
			Diagnostics.Assert(count > 0);
			Diagnostics.Assert(this.currentColumn + count <= this.colorCapacity);
			colors.CopyTo(firstIndex, this.controlTextureColorBuffer, this.currentColumn + this.colorLineStartIndex, count);
			this.currentColumn += count;
			Diagnostics.Assert(this.currentColumn <= this.colorCapacity);
			return count;
		}

		public void ClearRemainingLine(List<InstanciedMeshHolders.ObjectInfo> objectInfos)
		{
			Diagnostics.Assert(this.colorCapacity <= this.maxColorCapacity);
			Diagnostics.Assert(this.currentColumn <= this.colorCapacity);
			Diagnostics.Assert(this.InstanceCount <= this.instanceCapacity);
			Diagnostics.Assert(this.meshPixelsPerInstance > 0);
			int drawCallCount = this.DrawCallCount;
			Diagnostics.Assert(drawCallCount > 0);
			Color32 color = new Color32(0, 0, 0, 0);
			InstanciedMeshHolders.ObjectInfo objectInfo = objectInfos[this.meshIndex];
			int instanceCount = this.InstanceCount - (drawCallCount - 1) * this.MeshInstanceCapacity;
			int num = objectInfo.GetObjectCountDrawed(instanceCount) + (drawCallCount - 1) * this.MeshInstanceCapacity;
			int num2 = num * this.meshPixelsPerInstance;
			Diagnostics.Assert(num2 <= this.maxColorCapacity);
			Diagnostics.Assert(this.currentColumn <= num2);
			Diagnostics.Assert(num % objectInfo.MeshCount == 0);
			Diagnostics.Assert(this.InstanceCount % objectInfo.MeshCount == 0);
			for (int i = this.currentColumn; i < num2; i++)
			{
				this.controlTextureColorBuffer[this.colorLineStartIndex + i] = color;
			}
		}

		private int instanceCapacity;

		private int meshInstanceCapacity;

		private int colorCapacity;

		private int meshIndex;

		private int meshPixelsPerInstance;

		private int currentColumn;

		private int maxColorCapacity;

		private Color32[] controlTextureColorBuffer;

		private int colorLineStartIndex;

		private int nextAllocatedIndex;

		private int firstIndexUsingOffset;
	}

	private class ObjectInfo : IComparable<InstanciedMeshHolders.ObjectInfo>
	{
		public ObjectInfo(InstanciedMeshHolders.MeshAndSubMeshIndex smallMesh, int pixelsPerInstance, int maxPerBatchSmallMeshInstance, Material meshMaterial, Material originalMeshMaterial, int meshGenerationOption)
		{
			this.meshes = new List<InstanciedMeshHolders.MeshAndSubMeshIndex>();
			this.meshes.Add(smallMesh);
			this.pixelsPerInstance = pixelsPerInstance;
			this.idName = smallMesh.Mesh.name;
			this.CurrentAllocatedLineIndex = -1;
			this.FirstAllocatedLineIndex = -1;
			this.maxPerBatchSmallMeshInstance = maxPerBatchSmallMeshInstance;
			this.instanceCapacity = -1;
			this.instanceCapacityInSmallLine = -1;
			this.meshMaterial = meshMaterial;
			this.meshMaterialHashCode = this.meshMaterial.GetHashCode();
			this.originalMeshMaterial = originalMeshMaterial;
			this.OccurenceExpectation = 0.0;
			this.lastFrameInstanceDrawed = 0;
			this.meshGenerationOption = meshGenerationOption;
		}

		public ObjectInfo(string name, List<InstanciedMeshHolders.MeshAndSubMeshIndex> smallMeshesKeepTheList, int pixelsPerInstance, int maxPerBatchSmallMeshInstance, Material meshMaterial, Material originalMeshMaterial, int meshGenerationOption)
		{
			this.meshes = smallMeshesKeepTheList;
			this.idName = name;
			this.pixelsPerInstance = pixelsPerInstance;
			this.CurrentAllocatedLineIndex = -1;
			this.FirstAllocatedLineIndex = -1;
			this.maxPerBatchSmallMeshInstance = maxPerBatchSmallMeshInstance;
			this.instanceCapacity = -1;
			this.instanceCapacityInSmallLine = -1;
			this.meshMaterial = meshMaterial;
			this.meshMaterialHashCode = this.meshMaterial.GetHashCode();
			this.originalMeshMaterial = originalMeshMaterial;
			this.OccurenceExpectation = 0.0;
			this.lastFrameInstanceDrawed = 0;
			this.meshGenerationOption = meshGenerationOption;
		}

		public int PixelsPerInstance
		{
			get
			{
				return this.pixelsPerInstance;
			}
		}

		public int InstanceCapacity
		{
			get
			{
				return this.instanceCapacity;
			}
		}

		public int InstanceCapacityInSmallLine
		{
			get
			{
				return this.instanceCapacityInSmallLine;
			}
		}

		public int MeshCount
		{
			get
			{
				return this.meshCount;
			}
		}

		public int MemoryUsedVB
		{
			get
			{
				return this.memoryUsedVB;
			}
		}

		public int MemoryUsedIB
		{
			get
			{
				return this.memoryUsedIB;
			}
		}

		public int SortCriterion
		{
			get
			{
				return this.meshMaterialHashCode;
			}
		}

		public string IdName
		{
			get
			{
				return this.idName;
			}
		}

		public int LastFrameInstanceDrawed
		{
			get
			{
				return this.lastFrameInstanceDrawed;
			}
			set
			{
				this.lastFrameInstanceDrawed = value;
			}
		}

		public static void LogColumnName(TextWriter textWriter, int objectCount, int totalVB, int totalIB)
		{
			float num = (float)totalVB / 1024f;
			float num2 = (float)totalIB / 1024f;
			textWriter.Write(string.Format("Name {0};Vertex;Triangle;Capacity;MeshCount;VB(kb) {1};IB(kb) {2};expectation", objectCount, num.ToString("0.00"), num2.ToString("0.00")));
			textWriter.Write(textWriter.NewLine);
		}

		public void CreateMesh(int maxInstancePerBigLine, int maxInstancePerSmallLine, Matrix4x4 transformation, List<Matrix4x4> transformations, Bounds meshBounds, int lowerMeshCountSubdivision, InstanciedMeshHolders.GetOrCreateUnityEngineMesh meshAllocationHook)
		{
			Diagnostics.Assert(this.meshes != null);
			Diagnostics.Assert(this.meshes.Count > 0);
			Diagnostics.Assert(this.instanceCapacity == -1);
			Diagnostics.Assert(this.createdMesh == null);
			this.CreateMesh(this.meshes[0].Mesh.GetTopology(0), this.meshes, maxInstancePerBigLine, maxInstancePerSmallLine, transformation, transformations, meshBounds, lowerMeshCountSubdivision, meshAllocationHook);
			this.meshes = null;
		}

		public void Unload()
		{
			Diagnostics.Assert(this.instanceCapacity != -1);
			UnityEngine.Object.DestroyImmediate(this.createdMesh, true);
			this.createdMesh = null;
			this.instanceCapacity = -1;
			this.instanceCapacityInSmallLine = -1;
		}

		public int CompareTo(InstanciedMeshHolders.ObjectInfo other)
		{
			if (other.SortCriterion == this.SortCriterion)
			{
				return 0;
			}
			return (other.SortCriterion >= this.SortCriterion) ? -1 : 1;
		}

		public void Clear()
		{
			this.CurrentAllocatedLineIndex = -1;
			this.FirstAllocatedLineIndex = -1;
		}

		public void OverrideMaxPerBatchSmallMeshInstance(int value)
		{
			Diagnostics.Assert(value >= 1);
			this.maxPerBatchSmallMeshInstance = value;
		}

		public void LogContent(TextWriter textWriter)
		{
			float num = (float)this.memoryUsedVB / 1024f;
			float num2 = (float)this.memoryUsedIB / 1024f;
			textWriter.Write(string.Format("{0};{1};{2};{3};{4};{5};{6};{7}", new object[]
			{
				this.idName,
				this.verticesCount,
				this.triangleCount,
				this.InstanceCapacity / this.meshCount,
				this.meshCount,
				num.ToString("0.00"),
				num2.ToString("0.00"),
				this.OccurenceExpectation.ToString("0.00000")
			}));
			textWriter.Write(textWriter.NewLine);
		}

		public bool IsSameMesh(Mesh smallMesh, int pixelsPerInstance, Material originalMeshMaterial, int maxPerBatchSmallMeshInstance)
		{
			return this.IsSameMesh(smallMesh.name, pixelsPerInstance, originalMeshMaterial, maxPerBatchSmallMeshInstance);
		}

		public bool IsSameMesh(string idName, int pixelsPerInstance, Material originalMeshMaterial, int maxPerBatchSmallMeshInstance)
		{
			return !(this.originalMeshMaterial.name != originalMeshMaterial.name) && this.pixelsPerInstance == pixelsPerInstance && !(this.idName != idName) && this.maxPerBatchSmallMeshInstance == maxPerBatchSmallMeshInstance;
		}

		public Bounds GetMeshBounds(int meshIndex)
		{
			Diagnostics.Assert(meshIndex >= 0);
			Diagnostics.Assert(meshIndex < this.meshCount);
			return this.meshBounds[meshIndex];
		}

		public int GetObjectCountDrawed(int instanceCount)
		{
			int num = this.instanceCapacity;
			for (int i = 1; i < this.subMeshIndexToObjectCount.Length; i++)
			{
				if (this.subMeshIndexToObjectCount[i] < instanceCount)
				{
					break;
				}
				num = this.subMeshIndexToObjectCount[i];
			}
			Diagnostics.Assert(num <= this.instanceCapacity);
			Diagnostics.Assert(this.instanceCapacity == this.subMeshIndexToObjectCount[0]);
			return num;
		}

		public void DrawPrimitive(Camera camera, MaterialPropertyBlock materialPropertyBlock, Matrix4x4 matrix, int instanceCount)
		{
			Diagnostics.Assert(instanceCount % this.meshCount == 0);
			int submeshIndex = 0;
			for (int i = 1; i < this.subMeshIndexToObjectCount.Length; i++)
			{
				if (this.subMeshIndexToObjectCount[i] < instanceCount)
				{
					break;
				}
				submeshIndex = i;
			}
			Graphics.DrawMesh(this.createdMesh, matrix, this.meshMaterial, 0, camera, submeshIndex, materialPropertyBlock, true, true);
		}

		private void CreateMesh(MeshTopology meshTopology, List<InstanciedMeshHolders.MeshAndSubMeshIndex> smallMeshes, int maxInstancePerBigLine, int maxInstancePerSmallLine, Matrix4x4 transformation, List<Matrix4x4> transformations, Bounds meshBounds, int lowerMeshCountSubdivision, InstanciedMeshHolders.GetOrCreateUnityEngineMesh meshAllocationHook)
		{
			Diagnostics.Assert(smallMeshes.Count > 0);
			int num = 0;
			int num2 = 0;
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool flag4 = false;
			bool flag5 = (this.meshGenerationOption & InstanciedMeshHolders.MeshCreationOptionDropUV2) != 0;
			bool flag6 = (this.meshGenerationOption & InstanciedMeshHolders.MeshCreationOptionDropNormal) != 0;
			bool flag7 = (this.meshGenerationOption & InstanciedMeshHolders.MeshCreationOptionDropTangent) != 0;
			bool flag8 = (this.meshGenerationOption & InstanciedMeshHolders.MeshCreationOptionEncodeTangentInColor) != 0;
			bool flag9 = (this.meshGenerationOption & InstanciedMeshHolders.MeshCreationOptionEncodeNormalInColor) != 0;
			this.meshBounds = new List<Bounds>();
			for (int i = 0; i < smallMeshes.Count; i++)
			{
				Mesh mesh = smallMeshes[i].Mesh;
				Vector3[] vertices = mesh.vertices;
				Vector3[] normals = mesh.normals;
				Vector4[] tangents = mesh.tangents;
				Vector2[] uv = mesh.uv;
				Vector2[] uv2 = mesh.uv2;
				Diagnostics.Assert(mesh.subMeshCount >= smallMeshes[i].SubMeshIndex);
				int[] indices = mesh.GetIndices(smallMeshes[i].SubMeshIndex);
				num += vertices.Length;
				num2 += indices.Length;
				bool flag10 = normals != null && normals.Length > 0;
				bool flag11 = tangents != null && tangents.Length > 0;
				bool flag12 = uv != null && uv.Length > 0;
				bool flag13 = uv2 != null && uv2.Length > 0;
				if (i == 0)
				{
					flag = flag10;
					flag2 = flag11;
					flag3 = flag12;
					flag4 = flag13;
				}
				else
				{
					Diagnostics.Assert(flag == flag10);
					Diagnostics.Assert(flag2 == flag11);
					Diagnostics.Assert(flag3 == flag12);
					if (flag4 != flag13 && !flag5)
					{
						Diagnostics.LogWarning("Un mesh [{0}] a des composantes de vertex (UV2) diff�rente des autres mesh.", new object[]
						{
							mesh.name
						});
					}
					flag = (flag && flag10);
					flag2 = (flag2 && flag11);
					flag3 = (flag3 && flag12);
					flag4 = (flag4 && flag13);
				}
			}
			Diagnostics.Assert(lowerMeshCountSubdivision >= 1);
			Diagnostics.Assert(transformations == null || transformations.Count == smallMeshes.Count);
			int num3 = 32767;
			this.meshCount = smallMeshes.Count;
			int num4 = Math.Min(maxInstancePerBigLine / this.meshCount, Math.Min(num3 / num, this.maxPerBatchSmallMeshInstance));
			Diagnostics.Assert(num4 > 0);
			this.instanceCapacity = this.meshCount * num4;
			int num5 = num4 * num;
			int capacity = num4 * num2;
			this.verticesCount = num;
			this.triangleCount = num2 / 3;
			Vector3[] array = new Vector3[num5];
			Color[] array2 = new Color[num5];
			Vector3[] array3 = (!flag || flag6) ? null : new Vector3[num5];
			Vector4[] array4 = (!flag2 || flag7) ? null : new Vector4[num5];
			Vector2[] array5 = (!flag3) ? null : new Vector2[num5];
			Vector2[] array6 = (!flag4 || flag5) ? null : new Vector2[num5];
			List<int> list = new List<int>();
			list.Capacity = capacity;
			int num6 = 0;
			int num7 = 0;
			int num8 = 0;
			bool flag14 = false;
			for (int j = 0; j < num4; j++)
			{
				Vector3 vector = new Vector3(float.MinValue, float.MinValue, float.MinValue);
				Vector3 vector2 = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
				for (int k = 0; k < smallMeshes.Count; k++)
				{
					Color32 c = new Color32((byte)(num6 & 255), (byte)((num6 & 65280) >> 8), (byte)((num6 & 16711680) >> 16), 0);
					Mesh mesh2 = smallMeshes[k].Mesh;
					Vector3[] vertices2 = mesh2.vertices;
					Vector3[] normals2 = mesh2.normals;
					Vector4[] tangents2 = mesh2.tangents;
					Vector2[] uv3 = mesh2.uv;
					Vector2[] uv4 = mesh2.uv2;
					int[] indices2 = mesh2.GetIndices(smallMeshes[k].SubMeshIndex);
					Diagnostics.Assert(mesh2.subMeshCount >= smallMeshes[k].SubMeshIndex);
					Matrix4x4 matrix4x = (transformations == null) ? transformation : transformations[k];
					for (int l = 0; l < vertices2.Length; l++)
					{
						Vector3 vector3 = matrix4x.MultiplyPoint(vertices2[l]);
						array[num7 + l] = vector3;
						vector = Vector3.Max(vector, vector3);
						vector2 = Vector3.Min(vector2, vector3);
						array2[num7 + l] = c;
					}
					if (j == 0)
					{
						this.meshBounds.Add(new Bounds((vector + vector2) * 0.5f, vector - vector2));
					}
					if (flag && (array3 != null || flag9))
					{
						for (int m = 0; m < vertices2.Length; m++)
						{
							Vector3 vector4 = matrix4x.MultiplyVector(normals2[m]);
							vector4.Normalize();
							if (array3 != null)
							{
								array3[num7 + m] = vector4;
							}
							if (flag9)
							{
								Vector3 normalized = vector4.normalized;
								Color c2 = new Color(normalized.x * 0.5f + 0.5f, normalized.y * 0.5f + 0.5f, normalized.z * 0.5f + 0.5f, 0f);
								Color32 color = c2;
								Color32 c3 = array2[num7 + m];
								c3.g = color.g;
								c3.b = color.b;
								c3.a = color.r;
								array2[num7 + m] = c3;
							}
						}
					}
					if (flag2 && (array4 != null || flag8))
					{
						for (int n = 0; n < vertices2.Length; n++)
						{
							Vector3 vector5 = new Vector3(tangents2[n].x, tangents2[n].y, tangents2[n].z);
							vector5 = matrix4x.MultiplyVector(vector5);
							vector5.Normalize();
							if (array4 != null)
							{
								array4[num7 + n] = new Vector4(vector5.x, vector5.y, vector5.z, tangents2[n].w);
							}
							if (flag8)
							{
								float w = tangents2[n].w;
								bool flag15 = Math.Abs(w + 1f) < 0.01f;
								bool flag16 = Math.Abs(w - 1f) < 0.01f;
								if (!flag14 && !flag15 && !flag16)
								{
									flag14 = true;
									Diagnostics.LogWarning("Tangent Vector Is Strange w = {0} in {1}", new object[]
									{
										tangents2[n].w,
										mesh2.name
									});
								}
								Vector3 vector6 = (!flag15) ? (vector5 * 0.8f) : vector5;
								Color c4 = new Color(vector6.x * 0.5f + 0.5f, vector6.y * 0.5f + 0.5f, vector6.z * 0.5f + 0.5f, 0f);
								Color32 color2 = c4;
								Color32 c5 = array2[num7 + n] = c;
								c5.g = color2.g;
								c5.b = color2.b;
								c5.a = color2.r;
								array2[num7 + n] = c5;
							}
						}
					}
					if (array5 != null)
					{
						for (int num9 = 0; num9 < vertices2.Length; num9++)
						{
							array5[num7 + num9] = uv3[num9];
						}
					}
					if (array6 != null)
					{
						for (int num10 = 0; num10 < vertices2.Length; num10++)
						{
							array6[num7 + num10] = uv4[num10];
						}
					}
					for (int num11 = 0; num11 < indices2.Length; num11++)
					{
						int item = num7 + indices2[num11];
						Diagnostics.Assert(list.Count == num8 + num11);
						list.Add(item);
					}
					num7 += vertices2.Length;
					num8 += indices2.Length;
					num6++;
				}
			}
			this.createdMesh = meshAllocationHook();
			this.createdMesh.name = this.idName;
			this.createdMesh.vertices = array;
			this.createdMesh.colors = array2;
			this.memoryUsedVB = 16 * array.Length;
			if (array3 != null)
			{
				this.createdMesh.normals = array3;
				this.memoryUsedVB += 4 * array.Length;
			}
			if (array4 != null && !flag8)
			{
				this.createdMesh.tangents = array4;
				this.memoryUsedVB += 16 * array.Length;
			}
			if (array5 != null)
			{
				this.createdMesh.uv = array5;
				this.memoryUsedVB += 8 * array.Length;
			}
			if (array6 != null)
			{
				this.createdMesh.uv2 = array6;
				this.memoryUsedVB += 8 * array.Length;
			}
			int num12 = Math.Min(num4, maxInstancePerSmallLine / this.meshCount);
			this.instanceCapacityInSmallLine = num12 * this.meshCount;
			int num13 = Math.Min(num4, lowerMeshCountSubdivision);
			this.subMeshIndexToObjectCount = new int[num13];
			this.createdMesh.subMeshCount = this.subMeshIndexToObjectCount.Length;
			int num14 = num4;
			for (int num15 = 0; num15 < this.subMeshIndexToObjectCount.Length; num15++)
			{
				int num16 = num4 * (this.subMeshIndexToObjectCount.Length - num15) / this.subMeshIndexToObjectCount.Length;
				if (num15 > 0)
				{
					Diagnostics.Assert(num16 < num14);
					if (num16 < num12 && num14 > num12)
					{
						num16 = num12;
					}
				}
				int num17 = num16 * num2;
				if (num17 < list.Count)
				{
					list.RemoveRange(num17, list.Count - num17);
				}
				this.subMeshIndexToObjectCount[num15] = num16 * this.meshCount;
				this.createdMesh.SetIndices(list.ToArray(), meshTopology, num15);
				this.memoryUsedIB += 2 * list.Count;
				num14 = num16;
			}
			this.createdMesh.bounds = meshBounds;
		}

		public int CurrentAllocatedLineIndex;

		public int FirstAllocatedLineIndex;

		public double OccurenceExpectation;

		private string idName;

		private int maxPerBatchSmallMeshInstance;

		private int instanceCapacity;

		private int instanceCapacityInSmallLine;

		private List<InstanciedMeshHolders.MeshAndSubMeshIndex> meshes;

		private List<Bounds> meshBounds;

		private Mesh createdMesh;

		private Material meshMaterial;

		private Material originalMeshMaterial;

		private int[] subMeshIndexToObjectCount;

		private int meshCount;

		private int verticesCount;

		private int triangleCount;

		private int memoryUsedVB;

		private int memoryUsedIB;

		private int meshMaterialHashCode;

		private int lastFrameInstanceDrawed;

		private int pixelsPerInstance;

		private int meshGenerationOption;
	}

	public delegate Mesh GetOrCreateUnityEngineMesh();

	public delegate bool GetInstancingShader(Material material, int pixelsPerInstance, out Shader instancingShader, out int meshGenerationOptions);
}
