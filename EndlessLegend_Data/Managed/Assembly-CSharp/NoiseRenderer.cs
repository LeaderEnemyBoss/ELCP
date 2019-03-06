using System;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class NoiseRenderer : MonoBehaviour
{
	public bool AutoRefresh
	{
		get
		{
			return this.autoRefresh;
		}
	}

	public void GenerateTexture()
	{
		NoiseHelper.BufferGenerationData bufferGenerationData = default(NoiseHelper.BufferGenerationData);
		NoiseHelper.BufferGenerationData bufferGenerationData2 = bufferGenerationData;
		bufferGenerationData2.F0Settings = new Vector4(0f, 0f, this.noiseData.F0Settings.frequency, this.noiseData.F0Settings.amplitude);
		bufferGenerationData2.F1Settings = new Vector4(0f, 0f, this.noiseData.F1Settings.frequency, this.noiseData.F1Settings.amplitude);
		bufferGenerationData2.F2Settings = new Vector4(0f, 0f, this.noiseData.F2Settings.frequency, this.noiseData.F2Settings.amplitude);
		bufferGenerationData2.F3Settings = new Vector4(0f, 0f, this.noiseData.F3Settings.frequency, this.noiseData.F3Settings.amplitude);
		bufferGenerationData = bufferGenerationData2;
		if (this.useRandomSeed)
		{
			this.seed = DateTime.Now.Millisecond;
		}
		NoiseHelper.ApplyRandomOffsetToBufferGenerationData(this.seed, ref bufferGenerationData);
		this.size = new Vector2(Mathf.Ceil(Mathf.Abs(this.size.x)), Mathf.Ceil(Mathf.Abs(this.size.y)));
		int num = (int)this.size.x;
		int num2 = (int)this.size.y;
		float[,] array = new float[num, num2];
		float num3 = float.MaxValue;
		float num4 = float.MinValue;
		for (int i = 0; i < num2; i++)
		{
			for (int j = 0; j < num; j++)
			{
				Vector2 uv = new Vector2((float)j / this.size.x, (float)i / this.size.y);
				float noiseCyclicMap = NoiseHelper.GetNoiseCyclicMap(uv, bufferGenerationData, this.size);
				if (noiseCyclicMap < num3)
				{
					num3 = noiseCyclicMap;
				}
				if (noiseCyclicMap > num4)
				{
					num4 = noiseCyclicMap;
				}
				array[j, i] = noiseCyclicMap;
			}
		}
		for (int k = 0; k < num2; k++)
		{
			for (int l = 0; l < num; l++)
			{
				float num5 = (!this.normalizedNoise) ? array[l, k] : Mathf.InverseLerp(num3, num4, array[l, k]);
				if (num5 < this.threshold)
				{
					num5 = 0f;
				}
				else if (this.binaryResult)
				{
					num5 = 1f;
				}
				array[l, k] = num5;
			}
		}
		Color[] array2 = new Color[num * num2];
		for (int m = 0; m < num2; m++)
		{
			for (int n = 0; n < num; n++)
			{
				array2[m * num + n] = Color.Lerp(Color.black, Color.white, array[n, m]);
			}
		}
		Texture2D texture2D = new Texture2D(num, num2);
		texture2D.SetPixels(array2);
		texture2D.filterMode = FilterMode.Point;
		texture2D.Apply();
		MeshRenderer component = base.GetComponent<MeshRenderer>();
		component.sharedMaterial.mainTexture = texture2D;
		component.transform.localScale = new Vector3((float)num, (float)num2, 1f);
	}

	[Header("Tool Settings")]
	[SerializeField]
	private bool autoRefresh = true;

	[SerializeField]
	private bool binaryResult = true;

	[SerializeField]
	private bool useRandomSeed;

	[Header("Generation Settings")]
	[SerializeField]
	[Space]
	private int seed = 2018;

	[SerializeField]
	private Vector2 size = new Vector2(90f, 60f);

	[SerializeField]
	private bool normalizedNoise = true;

	[Header("Noise Settings")]
	[SerializeField]
	[Space]
	private float threshold = 0.8f;

	[SerializeField]
	private NoiseRenderer.NoiseData noiseData = new NoiseRenderer.NoiseData
	{
		F0Settings = new NoiseRenderer.NoiseFunctionSetting
		{
			frequency = 11f,
			amplitude = 1f
		},
		F1Settings = new NoiseRenderer.NoiseFunctionSetting
		{
			frequency = 19f,
			amplitude = 0.8f
		},
		F2Settings = new NoiseRenderer.NoiseFunctionSetting
		{
			frequency = 23f,
			amplitude = 0.6f
		},
		F3Settings = new NoiseRenderer.NoiseFunctionSetting
		{
			frequency = 29f,
			amplitude = 0.4f
		}
	};

	[Serializable]
	private struct NoiseData
	{
		public NoiseRenderer.NoiseFunctionSetting F0Settings;

		public NoiseRenderer.NoiseFunctionSetting F1Settings;

		public NoiseRenderer.NoiseFunctionSetting F2Settings;

		public NoiseRenderer.NoiseFunctionSetting F3Settings;
	}

	[Serializable]
	private struct NoiseFunctionSetting
	{
		public float frequency;

		public float amplitude;
	}
}
