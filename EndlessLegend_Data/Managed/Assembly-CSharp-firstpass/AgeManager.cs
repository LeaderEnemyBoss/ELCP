using System;
using System.Collections.Generic;
using System.IO;
using Amplitude;
using UnityEngine;

public class AgeManager : MonoBehaviour
{
	public event AgeManager.LoadDynamicTexturesDelegate OnLoadDynamicTexture;

	public static AgeManager Instance
	{
		get
		{
			if (Application.isPlaying)
			{
				return AgeManager.instance;
			}
			return UnityEngine.Object.FindObjectOfType(typeof(AgeManager)) as AgeManager;
		}
	}

	public static bool IsMouseCovered
	{
		get
		{
			return !(AgeManager.instance == null) && AgeManager.instance.MouseCovered;
		}
	}

	public static bool IsWheelGrabbed
	{
		get
		{
			return !(AgeManager.instance == null) && AgeManager.instance.WheelGrabbed;
		}
	}

	public static float LastRealtimeSinceStartup { get; private set; }

	public AgeAtlas Atlas
	{
		get
		{
			return (!Application.isPlaying) ? base.GetComponent<AgeAtlas>() : this.atlas;
		}
		private set
		{
		}
	}

	public FontAtlasRenderer FontAtlasRenderer
	{
		get
		{
			return (!Application.isPlaying) ? base.GetComponent<FontAtlasRenderer>() : this.fontAtlasRenderer;
		}
	}

	public AgeControl FocusedControl
	{
		get
		{
			return this.focusedControl;
		}
		set
		{
			if (this.focusedControl != value)
			{
				if (this.focusedControl != null)
				{
					this.focusedControl.FocusLoss();
				}
				this.focusedControl = value;
				if (this.focusedControl != null)
				{
					this.focusedControl.FocusGain();
				}
				else
				{
					this.ActiveControl = null;
					this.OverrolledTransform = null;
				}
			}
		}
	}

	public bool IsDebugMode { get; private set; }

	public bool IsInitialized { get; private set; }

	public bool ShowGui { get; set; }

	private bool ApplicationHasFocus { get; set; }

	private Dictionary<string, AgeManager.DynamicTexture> DynamicTextures { get; set; }

	public void Init()
	{
		if (!this.IsInitialized)
		{
			this.IsDebugMode = false;
			this.ShowGui = true;
			this.Atlas.Init();
			AgeRenderer[] componentsInChildren = base.GetComponentsInChildren<AgeRenderer>();
			this.rendererTransforms = new List<AgeTransform>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				this.rendererTransforms.Add(componentsInChildren[i].GetComponent<AgeTransform>());
				this.rendererTransforms[i].Init();
				componentsInChildren[i].BuildAtlasImages(this.Atlas);
				componentsInChildren[i].BuildAtlasFonts(this.Atlas);
			}
			this.rendererTransforms.Sort(this.comparer);
			this.Atlas.Build();
			this.RenderInfo.Atlas = this.Atlas;
			this.RenderInfo.RenderArea.width = (float)Screen.width;
			this.RenderInfo.RenderArea.height = (float)Screen.height;
			this.previousRenderArea = this.RenderInfo.RenderArea;
			this.IsInitialized = true;
		}
		if (Application.isPlaying)
		{
			if (this.rendererTransforms == null)
			{
				return;
			}
			for (int j = 0; j < this.rendererTransforms.Count; j++)
			{
				this.rendererTransforms[j].DirtyPosition = true;
				this.rendererTransforms[j].UpdateHierarchy(this.RenderInfo.RenderArea, true);
			}
		}
	}

	public void EditorGui(Rect renderArea, bool forceRefresh)
	{
		GL.Viewport(renderArea);
		GL.Clear(true, true, Color.grey);
		AgeRenderer[] componentsInChildren = base.GetComponentsInChildren<AgeRenderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			this.RenderInfo.Atlas = this.Atlas;
			this.RenderInfo.RenderArea = renderArea;
			if (this.previousRenderArea != this.RenderInfo.RenderArea || forceRefresh)
			{
				componentsInChildren[i].GetComponent<AgeTransform>().DirtyPosition = true;
				this.previousRenderArea = this.RenderInfo.RenderArea;
			}
			Rect renderArea2 = this.RenderInfo.RenderArea;
			renderArea2.height -= 2f;
			componentsInChildren[i].GetComponent<AgeTransform>().UpdateHierarchy(renderArea2, false);
			componentsInChildren[i].DoRender(this.RenderInfo, 0f);
		}
		GL.Viewport(Rect.MinMaxRect(0f, 0f, (float)Screen.width, (float)Screen.height));
	}

	public Texture2D FindDynamicTexture(string path, bool fastSearch = false)
	{
		Texture2D texture2D = null;
		if (string.IsNullOrEmpty(path))
		{
			Diagnostics.LogWarning("Empty texture path");
			return null;
		}
		if (fastSearch)
		{
			this.textureName = path;
		}
		else
		{
			this.textureName = Path.GetFileNameWithoutExtension(path).ToUpper();
		}
		if (string.IsNullOrEmpty(this.textureName))
		{
			Diagnostics.LogWarning("Error trying to fetch texture from path " + path);
			return null;
		}
		if (this.Atlas.TextureLookup.ContainsKey(this.textureName))
		{
			texture2D = this.Atlas.TextureLookup[this.textureName];
		}
		if (this.DynamicTextures.ContainsKey(this.textureName))
		{
			texture2D = this.DynamicTextures[this.textureName];
		}
		if (texture2D == null)
		{
			bool defaultResource = true;
			if (this.OnLoadDynamicTexture != null)
			{
				foreach (AgeManager.LoadDynamicTexturesDelegate loadDynamicTexturesDelegate in this.OnLoadDynamicTexture.GetInvocationList())
				{
					if (loadDynamicTexturesDelegate(path, out texture2D))
					{
						defaultResource = false;
						break;
					}
				}
			}
			if (texture2D == null)
			{
				texture2D = (Resources.Load(path) as Texture2D);
			}
			this.DynamicTextures[this.textureName] = new AgeManager.DynamicTexture(texture2D, defaultResource);
			if (texture2D == null)
			{
				Diagnostics.LogWarning("Failed to load image " + path);
				if (!this.Atlas.TextureLookup.ContainsKey(this.textureName))
				{
					Diagnostics.LogWarning("Image " + this.textureName + " was not found in the main atlas either ");
				}
			}
		}
		return texture2D;
	}

	public void OnResolutionChanged()
	{
		this.RenderInfo.RenderArea.width = (float)Screen.width;
		this.RenderInfo.RenderArea.height = (float)Screen.height;
		for (int i = 0; i < this.rendererTransforms.Count; i++)
		{
			if (!this.rendererTransforms[i].FixedSize)
			{
				this.rendererTransforms[i].Width = (float)Screen.width;
				this.rendererTransforms[i].Height = (float)Screen.height;
				this.rendererTransforms[i].DirtyPosition = true;
			}
		}
	}

	public void SafeReleaseDynamicTexture(string textureName)
	{
	}

	public void ReleaseDynamicTexture(string textureName)
	{
		textureName = textureName.ToUpper();
		if (this.DynamicTextures.ContainsKey(textureName))
		{
			for (int i = 0; i < this.rendererTransforms.Count; i++)
			{
				this.rendererTransforms[i].GetComponent<AgeRenderer>().MeshBuilder.ReleaseDynamicTexture(this.DynamicTextures[textureName]);
			}
			AgeManager.DynamicTexture dynamicTexture = this.DynamicTextures[textureName];
			if (dynamicTexture.IsDefaultResource)
			{
				Resources.UnloadAsset(dynamicTexture);
			}
			this.DynamicTextures.Remove(textureName);
		}
	}

	public void ReleaseDynamicTextures()
	{
		foreach (AgeManager.DynamicTexture dynamicTexture in this.DynamicTextures.Values)
		{
			for (int i = 0; i < this.rendererTransforms.Count; i++)
			{
				this.rendererTransforms[i].GetComponent<AgeRenderer>().MeshBuilder.ReleaseDynamicTexture(dynamicTexture);
			}
			if (dynamicTexture.IsDefaultResource)
			{
				Resources.UnloadAsset(dynamicTexture);
			}
		}
		this.DynamicTextures.Clear();
	}

	public void SwitchDebugMode()
	{
		this.IsDebugMode = !this.IsDebugMode;
	}

	public void SwitchHighDefinition(bool highDefinition)
	{
		if (AgeUtils.HighDefinition != highDefinition)
		{
			float num = AgeUtils.HighDefinitionFactor;
			if (!highDefinition && AgeUtils.HighDefinition)
			{
				num = 1f / num;
			}
			AgeUtils.HighDefinition = highDefinition;
			for (int i = 0; i < this.rendererTransforms.Count; i++)
			{
				if (!this.rendererTransforms[i].FixedSize)
				{
					this.rendererTransforms[i].ApplyHighDefinitionHierarchical(num);
					this.rendererTransforms[i].DirtyPosition = true;
					this.rendererTransforms[i].ApplyDirtyStructure();
				}
			}
			this.Atlas.ResetFonts();
			for (int j = 0; j < this.rendererTransforms.Count; j++)
			{
				this.rendererTransforms[j].GetComponent<AgeRenderer>().BuildAtlasFonts(this.Atlas);
			}
			this.Atlas.Build();
		}
	}

	protected void Awake()
	{
		AgeManager.instance = this;
		this.comparer = new AgeUtils.SiblingIndexComparer();
		AgeTransform.InternalComparer = this.comparer;
		this.Cursor = new Vector2(0f, 0f);
		this.atlas = base.GetComponent<AgeAtlas>();
		this.fontAtlasRenderer = base.GetComponent<FontAtlasRenderer>();
		this.DynamicTextures = new Dictionary<string, AgeManager.DynamicTexture>();
		this.ApplicationHasFocus = true;
	}

	protected virtual void LateUpdate()
	{
		if (Application.isPlaying && this.rendererTransforms == null)
		{
			return;
		}
		if (Application.isPlaying && (this.RenderInfo.RenderArea.width != (float)Screen.width || this.RenderInfo.RenderArea.height != (float)Screen.height))
		{
			this.OnResolutionChanged();
		}
		if (Application.isPlaying && AgeManager.AutoInit)
		{
			GL.Viewport(this.RenderInfo.RenderArea);
			GL.Clear(true, true, Color.gray);
		}
		Vector2 cursor = this.Cursor;
		if (this.ForceCursorPosition)
		{
			this.Cursor = this.ForcedCursorPosition;
		}
		else
		{
			this.Cursor = Input.mousePosition;
			this.Cursor.y = this.RenderInfo.RenderArea.height - 1f - this.Cursor.y;
		}
		if (this.ApplicationHasFocus)
		{
			bool flag = true;
			if (this.Cursor.x < 0f || this.Cursor.x >= this.RenderInfo.RenderArea.width || this.Cursor.y < 0f || this.Cursor.y >= this.RenderInfo.RenderArea.height)
			{
				flag = false;
			}
			if (flag)
			{
				AgeControl activeControl = this.ActiveControl;
				this.ActiveControl = null;
				this.ActiveParentTransform = null;
				this.OverrolledTransform = null;
				this.MouseCovered = false;
				this.WheelGrabbed = false;
				this.RenderInfo.LocalMatrix = false;
				if (Application.isPlaying)
				{
					for (int i = 0; i < this.rendererTransforms.Count; i++)
					{
						float num = 0f;
						Vector2 positionOffset;
						positionOffset.x = this.rendererTransforms[i].Position.x;
						positionOffset.y = this.rendererTransforms[i].Position.y;
						this.rendererTransforms[i].UpdateInteractivity(this.Cursor, positionOffset, this.rendererTransforms[i].Position, this.RenderInfo, 0f, ref num, ref this.ActiveControl, ref this.OverrolledTransform, ref this.MouseCovered, ref this.WheelGrabbed, true, false);
						if (this.IsDebugMode)
						{
							Diagnostics.Log("AgeManager - ActiveControl: {0}", new object[]
							{
								this.ActiveControl
							});
							Diagnostics.Log("AgeManager - OverrolledTransform: {0}", new object[]
							{
								this.OverrolledTransform
							});
						}
					}
				}
				if (this.ActiveControl != null && this.ActiveControl.AgeTransform != null && this.ActiveControl.AgeTransform.GetParent() != null)
				{
					this.ActiveParentTransform = this.ActiveControl.AgeTransform.GetParent();
				}
				if (this.ActiveControl != activeControl)
				{
					if (activeControl != null)
					{
						if (this.ActiveControl != null)
						{
							if (!this.ActiveControl.AgeTransform.IsChildOf(activeControl.AgeTransform))
							{
								activeControl.SendMessage("MouseLeave", this.Cursor);
							}
						}
						else
						{
							activeControl.SendMessage("MouseLeave", this.Cursor);
						}
					}
					if (this.ActiveControl != null)
					{
						this.ActiveControl.SendMessage("MouseEnter", this.Cursor);
					}
				}
				this.currentMouseEventData.Cursor = this.Cursor;
				for (int j = 0; j <= 2; j++)
				{
					this.currentMouseEventData.MouseButtonIndex = j;
					if (Input.GetMouseButtonDown(j))
					{
						if (this.ActiveControl != null)
						{
							this.ActiveControl.SendMessage("MouseDown", this.currentMouseEventData);
							if (j == 0)
							{
								this.FocusedControl = this.ActiveControl;
							}
						}
						else if (j == 0)
						{
							this.FocusedControl = null;
						}
					}
					if (Input.GetMouseButtonUp(j))
					{
						if (this.ActiveControl != null)
						{
							this.ActiveControl.SendMessage("MouseUp", this.currentMouseEventData);
						}
						if (this.FocusedControl != null && this.FocusedControl != this.ActiveControl)
						{
							this.FocusedControl.SendMessage("MouseUp", this.currentMouseEventData);
						}
					}
				}
				if (this.Cursor != cursor)
				{
					if (this.ActiveControl != null)
					{
						this.ActiveControl.SendMessage("MouseDrag", this.Cursor - cursor);
					}
					if (this.FocusedControl != null && this.FocusedControl != this.ActiveControl)
					{
						this.FocusedControl.SendMessage("MouseDrag", this.Cursor - cursor);
					}
				}
				float axis = Input.GetAxis(this.mouseWheelAxis);
				if (axis != 0f && this.ActiveControl != null)
				{
					this.ActiveControl.SendMessage("MouseWheel", axis);
				}
				if (Input.anyKeyDown)
				{
					if (this.FocusedControl != null)
					{
						this.FocusedControl.SendMessage("KeyDown");
					}
					else if (this.ActiveControl != null)
					{
						this.ActiveControl.SendMessage("KeyDown");
					}
				}
				if (this.FocusedControl != null && !string.IsNullOrEmpty(Input.inputString))
				{
					this.FocusedControl.SendMessage("KeyInput");
				}
				if (this.FocusedControl != null)
				{
					this.FocusedControl.UpdateFocused();
				}
			}
		}
		if (Application.isPlaying)
		{
			if (this.rendererTransforms == null)
			{
				return;
			}
			for (int k = 0; k < this.rendererTransforms.Count; k++)
			{
				if (this.previousRenderArea != this.RenderInfo.RenderArea)
				{
					this.rendererTransforms[k].DirtyPosition = true;
					this.previousRenderArea = this.RenderInfo.RenderArea;
				}
				this.rendererTransforms[k].UpdateHierarchy(this.RenderInfo.RenderArea, false);
			}
		}
		AgeManager.LastRealtimeSinceStartup = Time.realtimeSinceStartup;
	}

	protected virtual void OnApplicationFocus(bool focus)
	{
		this.ApplicationHasFocus = focus;
	}

	protected virtual void Start()
	{
		if (AgeManager.AutoInit)
		{
			Diagnostics.Log("Automatic initialization the age manager...");
			this.Init();
		}
	}

	public static bool AutoInit = true;

	public GUISkin GuiEditorSkin;

	public bool DrawGizmos = true;

	public AgeControl ActiveControl;

	public AgeTransform ActiveParentTransform;

	public AgeTransform OverrolledTransform;

	public Vector2 Cursor;

	public bool MouseCovered;

	public bool WheelGrabbed;

	public AgeRenderInfo RenderInfo = new AgeRenderInfo();

	public Rect CurrentOverrolledControlPosition;

	public bool ForceCursorPosition;

	public Vector3 ForcedCursorPosition;

	private static AgeManager instance;

	private AgeControl focusedControl;

	private IComparer<AgeTransform> comparer;

	private List<AgeTransform> rendererTransforms;

	private AgeAtlas atlas;

	private FontAtlasRenderer fontAtlasRenderer;

	private string mouseWheelAxis = "Mouse ScrollWheel";

	private Rect previousRenderArea;

	private string textureName;

	private AgeMouseEventData currentMouseEventData;

	private class DynamicTexture
	{
		public DynamicTexture(Texture2D texture, bool defaultResource)
		{
			this.texture = texture;
			this.IsDefaultResource = defaultResource;
		}

		public bool IsDefaultResource { get; private set; }

		public static implicit operator Texture2D(AgeManager.DynamicTexture dynamicTexture)
		{
			return dynamicTexture.texture;
		}

		private Texture2D texture;
	}

	public delegate bool LoadDynamicTexturesDelegate(string path, out Texture2D texture);
}
