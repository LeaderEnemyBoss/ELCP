using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using UnityEngine;

public class DefaultLayerController : WorldViewTechniqueAncillary, IService, ILayerService
{
	public float AngleAtMaximalElevation
	{
		get
		{
			return this.angleAtMaximalElevation;
		}
	}

	public float AngleAtMinimalElevation
	{
		get
		{
			return this.angleAtMinimalElevation;
		}
	}

	public float MaxAzimutDeltaAtMaximalElevation
	{
		get
		{
			return this.maxAzimutDeltaAtMaximalElevation;
		}
	}

	public float MaxAzimutDeltaAtMinimalElevation
	{
		get
		{
			return (float)this.maxAzimutDeltaAtMinimalElevation;
		}
	}

	public UnityEngine.AnimationCurve CameraElevationCurve
	{
		get
		{
			return this.cameraElevationCurve;
		}
	}

	public UnityEngine.AnimationCurve CameraOrientationCurve
	{
		get
		{
			return this.cameraOrientationCurve;
		}
	}

	public UnityEngine.AnimationCurve CameraAzimutCurve
	{
		get
		{
			return this.cameraAzimutCurve;
		}
	}

	public UnityEngine.AnimationCurve CameraSpeedCurve
	{
		get
		{
			return this.cameraSpeedCurve;
		}
	}

	public StaticString CurrentZoomDescriptorName { get; private set; }

	public float DeltaAzimut
	{
		get
		{
			return this.deltaAzimut;
		}
	}

	public Layer[] Layers { get; private set; }

	public int MaximalHorizontalTilePerScreen
	{
		get
		{
			return this.maximalHorizontalTilePerScreen;
		}
	}

	public int MinimalHorizontalTilePerScreen
	{
		get
		{
			return this.minimalHorizontalTilePerScreen;
		}
	}

	public float ZoomGap
	{
		get
		{
			return this.zoomGap;
		}
	}

	public float ZoomSmoothSpeed
	{
		get
		{
			return this.zoomSmoothSpeed;
		}
	}

	public float ZoomSpeedFactor
	{
		get
		{
			return this.zoomSpeedFactor;
		}
	}

	private NormalizedDeZoomDescriptor[] NormalizedDeZoomDescriptors { get; set; }

	public override IEnumerator BindServices(IServiceContainer serviceContainer)
	{
		yield return base.BindServices(serviceContainer);
		Component[] components = base.GetComponentsInChildren(typeof(Layer));
		if (components == null || components.Length == 0)
		{
			Diagnostics.LogWarning("The current layer controller has found no layer to manage.");
		}
		else
		{
			this.Layers = new Layer[components.Length];
			int num;
			for (int index = 0; index < this.Layers.Length; index = num + 1)
			{
				this.Layers[index] = (components[index] as Layer);
				yield return this.Layers[index].Load(base.WorldViewTechnique);
				num = index;
			}
		}
		this.NormalizedDeZoomDescriptors = null;
		if (this.Layers == null || this.Layers.Length == 0)
		{
			Diagnostics.LogWarning("The current layer controller has found no layer to manage.");
		}
		if (this.ZoomDescriptors == null || this.ZoomDescriptors.Length == 0)
		{
			Diagnostics.LogWarning("The current layer controller has no zoom descriptors defined.");
		}
		this.UpdateZoomRatioDetailsBecomeAbstract();
		this.CurrentZoomDescriptorName = StaticString.Empty;
		serviceContainer.AddService<ILayerService>(this);
		yield break;
	}

	public Layer GetLayer<T>() where T : Layer
	{
		if (this.Layers == null)
		{
			return null;
		}
		return Array.Find<Layer>(this.Layers, (Layer layer) => layer is T);
	}

	public Layer GetLayer<T>(string layerName) where T : Layer
	{
		if (this.Layers == null)
		{
			return null;
		}
		return Array.Find<Layer>(this.Layers, (Layer layer) => layer is T && layer.LayerName == layerName);
	}

	public void UpdateZoomDescriptors(float normalizedDeZoom)
	{
		if (this.ZoomDescriptors == null || this.ZoomDescriptors.Length == 0)
		{
			return;
		}
		float num = this.ZoomLimitHysteresis;
		if (this.CurrentZoomDescriptorName == StaticString.Empty || normalizedDeZoom == 0f)
		{
			num = 0f;
		}
		for (int i = 0; i < this.NormalizedDeZoomDescriptors.Length; i++)
		{
			NormalizedDeZoomDescriptor normalizedDeZoomDescriptor = this.NormalizedDeZoomDescriptors[i];
			if (normalizedDeZoomDescriptor.MinimumValue + num <= normalizedDeZoom && normalizedDeZoom < normalizedDeZoomDescriptor.MaximumValue - num && this.CurrentZoomDescriptorName != normalizedDeZoomDescriptor.ZoomDescriptor.Name)
			{
				this.ChangeCurrentZoomDescriptor(normalizedDeZoomDescriptor.ZoomDescriptor.Name);
			}
		}
	}

	public void TrimZoomDescriptors(float maximumNormalizedDeZoom)
	{
		if (this.ZoomDescriptors == null || this.ZoomDescriptors.Length == 0)
		{
			return;
		}
		if (base.WorldViewTechnique == null || base.WorldViewTechnique.WorldMovementSettings == null)
		{
			return;
		}
		if (maximumNormalizedDeZoom <= 0f)
		{
			Diagnostics.LogError("Invalid maximum normalized [de]zoom value ({0}), should be > 0.0f.", new object[]
			{
				maximumNormalizedDeZoom
			});
			maximumNormalizedDeZoom = 0f;
		}
		List<NormalizedDeZoomDescriptor> list = new List<NormalizedDeZoomDescriptor>();
		for (int i = 0; i < this.ZoomDescriptors.Length; i++)
		{
			ZoomDescriptor zoomDescriptor = this.ZoomDescriptors[i];
			try
			{
				float minimumNormalizedDeZoom = this.ConvertToNormalizedDeZoomValue(zoomDescriptor.MinimumValue, maximumNormalizedDeZoom);
				float maximumNormalizedDeZoom2 = this.ConvertToNormalizedDeZoomValue(zoomDescriptor.MaximumValue, maximumNormalizedDeZoom);
				NormalizedDeZoomDescriptor item = new NormalizedDeZoomDescriptor(zoomDescriptor, maximumNormalizedDeZoom2, minimumNormalizedDeZoom);
				list.Add(item);
			}
			catch
			{
			}
		}
		list.Sort(new DefaultLayerController.SortNormalizedDeZoomDescriptorByValue());
		int num = 12;
		bool flag = true;
		while (flag)
		{
			flag = false;
			for (int j = 0; j < list.Count; j++)
			{
				if (j == 0)
				{
					list[j].MinimumValue = 0f;
				}
				else if (list[j].MinimumValue <= 0f)
				{
					list[j].MinimumValue = list[j - 1].MaximumValue;
				}
				else if (list[j].MinimumValue < list[j - 1].MaximumValue)
				{
					if (list[j - 1].ZoomDescriptor.IsOptional ^ !list[j].ZoomDescriptor.IsOptional)
					{
						float num2 = (list[j - 1].MaximumValue + list[j].MinimumValue) * 0.5f;
						list[j - 1].MaximumValue = num2;
						list[j].MinimumValue = num2;
					}
					else if (list[j - 1].ZoomDescriptor.IsOptional)
					{
						list[j - 1].MaximumValue = list[j].MinimumValue;
					}
					else
					{
						list[j].MinimumValue = list[j - 1].MaximumValue;
					}
				}
				else if (list[j].MinimumValue > list[j - 1].MaximumValue)
				{
					if (list[j - 1].ZoomDescriptor.IsOptional ^ !list[j].ZoomDescriptor.IsOptional)
					{
						float num3 = (list[j - 1].MaximumValue + list[j].MinimumValue) * 0.5f;
						list[j - 1].MaximumValue = num3;
						list[j].MinimumValue = num3;
					}
					else if (list[j - 1].ZoomDescriptor.IsOptional)
					{
						list[j - 1].MaximumValue = list[j].MinimumValue;
					}
					else
					{
						list[j].MinimumValue = list[j - 1].MaximumValue;
					}
				}
				if (list[j].MaximumValue >= maximumNormalizedDeZoom)
				{
					list[j].MaximumValue = maximumNormalizedDeZoom;
				}
			}
			if (--num < 0)
			{
				Diagnostics.LogWarning("The maximum number of iterations has been reached; the zoom descriptors cannot be trimmed any better...");
				break;
			}
			if (list.Count > 1)
			{
				for (int k = 0; k < list.Count; k++)
				{
					if (list[k].MaximumValue - list[k].MinimumValue <= this.MinimalRange)
					{
						if (list[k].ZoomDescriptor.IsOptional)
						{
							if (k == 0)
							{
								list[k + 1].MinimumValue = 0f;
							}
							else if (k == list.Count - 1)
							{
								list[k - 1].MaximumValue = maximumNormalizedDeZoom;
							}
							else if (list[k - 1].ZoomDescriptor.IsOptional ^ !list[k + 1].ZoomDescriptor.IsOptional)
							{
								float num4 = (list[k - 1].MaximumValue + list[k + 1].MinimumValue) * 0.5f;
								list[k - 1].MaximumValue = num4;
								list[k + 1].MinimumValue = num4;
							}
							else if (list[k - 1].ZoomDescriptor.IsOptional)
							{
								list[k - 1].MaximumValue = list[k + 1].MinimumValue;
							}
							else
							{
								list[k + 1].MinimumValue = list[k - 1].MaximumValue;
							}
						}
						else
						{
							flag = true;
							if (k == 0)
							{
								float num5 = list[k].MinimumValue + this.MinimalRange;
								list[k].MaximumValue = num5;
								list[k + 1].MinimumValue = num5;
							}
							else if (k == list.Count - 1)
							{
								float num6 = list[k].MaximumValue - this.MinimalRange;
								list[k - 1].MaximumValue = num6;
								list[k].MinimumValue = num6;
							}
							else
							{
								float num7 = (list[k - 1].MaximumValue + list[k].MinimumValue) * 0.5f;
								float num8 = (list[k].MaximumValue + list[k + 1].MinimumValue) * 0.5f;
								list[k - 1].MaximumValue = num7;
								list[k].MinimumValue = num7;
								list[k].MaximumValue = num8;
								list[k + 1].MinimumValue = num8;
							}
						}
					}
				}
			}
		}
		this.NormalizedDeZoomDescriptors = list.ToArray();
		this.theMaximumNormalizedDeZoom = maximumNormalizedDeZoom;
	}

	private void ChangeCurrentZoomDescriptor(StaticString zoomDescriptorName)
	{
		StaticString currentZoomDescriptorName = this.CurrentZoomDescriptorName;
		this.CurrentZoomDescriptorName = zoomDescriptorName;
		if (this.Layers != null)
		{
			for (int i = 0; i < this.Layers.Length; i++)
			{
				this.Layers[i].CurrentZoomDescriptorChange(this.CurrentZoomDescriptorName, currentZoomDescriptorName);
			}
		}
	}

	private float ConvertToNormalizedDeZoomValue(string value, float maximumNormalizedDeZoom = 1f)
	{
		Diagnostics.Assert(base.WorldViewTechnique != null);
		Diagnostics.Assert(base.WorldViewTechnique.WorldMovementSettings != null);
		if (string.IsNullOrEmpty(value))
		{
			return 0f;
		}
		string[] array = value.Split(new char[]
		{
			':'
		});
		if (array.Length == 1)
		{
			return float.Parse(value);
		}
		if (array.Length != 2)
		{
			Diagnostics.LogError("Bad format for zoom descriptor value (string: '{0}'), expecting or '[format:]value'.", new object[]
			{
				value
			});
			throw new InvalidOperationException();
		}
		string a = array[0].Trim();
		if (a == "ndz" || a == "nz")
		{
			return float.Parse(array[1]);
		}
		if (!(a == "mndz") && !(a == "mnz"))
		{
			Diagnostics.LogError("Bad format for zoom descriptor value (input: '{0}'), expecting 'fmt:value' with fmt=[e|ne|ndz|nz|mne|mndz|mdz|h|t].", new object[]
			{
				value
			});
			throw new InvalidOperationException();
		}
		return float.Parse(array[1]) * maximumNormalizedDeZoom;
	}

	public void UpdateZoomRatioDetailsBecomeAbstract()
	{
		string text = "mnz:" + Amplitude.Unity.Framework.Application.Registry.GetValue<float>(GuiManager.Registers.ZoomRatioDetailsBecomeAbstract, 0.5f).ToString();
		for (int i = 0; i < this.ZoomDescriptors.Length; i++)
		{
			string name = this.ZoomDescriptors[i].Name;
			if (!(name == "MidLevel"))
			{
				if (name == "View2D")
				{
					this.ZoomDescriptors[i].MinimumValue = text;
				}
			}
			else
			{
				this.ZoomDescriptors[i].MaximumValue = text;
			}
		}
		if (this.theMaximumNormalizedDeZoom < 0f)
		{
			return;
		}
		this.TrimZoomDescriptors(this.theMaximumNormalizedDeZoom);
	}

	public ZoomDescriptor[] ZoomDescriptors;

	public float MinimalRange;

	public float ZoomLimitHysteresis;

	[SerializeField]
	private float deltaAzimut;

	[SerializeField]
	private UnityEngine.AnimationCurve cameraElevationCurve;

	[SerializeField]
	private UnityEngine.AnimationCurve cameraOrientationCurve;

	[SerializeField]
	private UnityEngine.AnimationCurve cameraAzimutCurve;

	[SerializeField]
	private UnityEngine.AnimationCurve cameraSpeedCurve;

	[SerializeField]
	private float angleAtMaximalElevation;

	[SerializeField]
	private float angleAtMinimalElevation;

	[SerializeField]
	private int minimalHorizontalTilePerScreen;

	[SerializeField]
	private float maxAzimutDeltaAtMaximalElevation;

	[SerializeField]
	private int maxAzimutDeltaAtMinimalElevation;

	[SerializeField]
	private int maximalHorizontalTilePerScreen;

	[SerializeField]
	private float zoomSpeedFactor;

	[SerializeField]
	private float zoomSmoothSpeed;

	[SerializeField]
	private float zoomGap;

	private float theMaximumNormalizedDeZoom = -1f;

	public class SortNormalizedDeZoomDescriptorByValue : IComparer<NormalizedDeZoomDescriptor>
	{
		public int Compare(NormalizedDeZoomDescriptor x, NormalizedDeZoomDescriptor y)
		{
			int num = x.MinimumValue.CompareTo(y.MinimumValue);
			if (num == 0)
			{
				return x.MaximumValue.CompareTo(y.MaximumValue);
			}
			return num;
		}
	}
}
