using System;
using System.Collections.Generic;
using Amplitude;
using UnityEngine;

public class AgeTransform : MonoBehaviour
{
	public AgeAudio AgeAudio
	{
		get
		{
			if (Application.isPlaying)
			{
				return this.privateAudio;
			}
			return base.GetComponent<AgeAudio>();
		}
		set
		{
			this.privateAudio = value;
		}
	}

	public AgeControl AgeControl
	{
		get
		{
			if (Application.isPlaying)
			{
				return this.privateControl;
			}
			return base.GetComponent<AgeControl>();
		}
		set
		{
			this.privateControl = value;
		}
	}

	public AgeModifier[] AgeModifiers
	{
		get
		{
			if (Application.isPlaying)
			{
				return this.modifiers;
			}
			return base.GetComponents<AgeModifier>();
		}
		set
		{
			this.modifiers = value;
		}
	}

	public AgePrimitive AgePrimitive
	{
		get
		{
			if (Application.isPlaying)
			{
				return this.privatePrimitive;
			}
			return base.GetComponent<AgePrimitive>();
		}
		private set
		{
		}
	}

	public AgeTooltip AgeTooltip
	{
		get
		{
			if (Application.isPlaying)
			{
				return this.privateTooltip;
			}
			return base.GetComponent<AgeTooltip>();
		}
		private set
		{
		}
	}

	public AgeMeshBuilder AgeMeshBuilder { get; private set; }

	public AgeAtlas AgeAtlas
	{
		get
		{
			if (AgeManager.Instance != null)
			{
				return AgeManager.Instance.GetComponent<AgeAtlas>();
			}
			return null;
		}
		private set
		{
		}
	}

	public bool DirtyPosition
	{
		get
		{
			return this.dirtyPosition;
		}
		set
		{
			if (value)
			{
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public bool PropagateDirty
	{
		get
		{
			return this.propagateDirty;
		}
		set
		{
			if (this.propagateDirty != value)
			{
				this.propagateDirty = value;
			}
		}
	}

	public bool HasModifiers
	{
		get
		{
			return this.modifiers != null && this.modifiers.Length > 0;
		}
		private set
		{
		}
	}

	public bool ModifiersRunning
	{
		get
		{
			if (this.modifiers == null)
			{
				return false;
			}
			if (this.modifiers.Length > 0)
			{
				bool result = false;
				for (int i = 0; i < this.modifiers.Length; i++)
				{
					if (this.modifiers[i].IsStarted() && !this.modifiers[i].IsComplete())
					{
						result = true;
						break;
					}
				}
				return result;
			}
			return false;
		}
		private set
		{
		}
	}

	public bool StartNewMesh
	{
		get
		{
			return this.startNewMesh;
		}
		set
		{
			if (this.startNewMesh != value)
			{
				this.startNewMesh = value;
			}
		}
	}

	public bool Visible
	{
		get
		{
			return this.visible;
		}
		set
		{
			if (this.visible != value)
			{
				this.visible = value;
				if (Application.isPlaying)
				{
					if (this.AgeMeshBuilder != null)
					{
						this.AgeMeshBuilder.NotifyStructureChange();
					}
					if (this.AgePrimitive != null)
					{
						this.AgePrimitive.OnVisible(this.visible);
					}
					if (this.AgeControl != null)
					{
						this.AgeControl.OnVisible(this.visible);
					}
					if (this.AgeAudio != null)
					{
						this.AgeAudio.OnVisible(this.visible);
					}
					if (this.modifiers != null)
					{
						for (int i = 0; i < this.modifiers.Length; i++)
						{
							this.modifiers[i].OnVisible(this.visible);
						}
					}
				}
				if (this.visible)
				{
					this.ApplyDirtyPosition(false);
				}
			}
		}
	}

	public bool Enable
	{
		get
		{
			return this.enable;
		}
		set
		{
			if (this.enable != value)
			{
				this.enable = value;
				if (Application.isPlaying && this.FadeOnDisable)
				{
					this.ApplyDirtyAlpha();
				}
			}
		}
	}

	public bool Anchored
	{
		get
		{
			return this.anchored;
		}
		set
		{
			if (this.anchored != value)
			{
				this.anchored = value;
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public Rect Position
	{
		get
		{
			return this.basePosition;
		}
		set
		{
			if (this.basePosition != value)
			{
				this.basePosition = value;
				this.basePosition.x = Mathf.Round(this.basePosition.x);
				this.basePosition.y = Mathf.Round(this.basePosition.y);
				this.basePosition.width = Mathf.Round(this.basePosition.width);
				this.basePosition.height = Mathf.Round(this.basePosition.height);
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public float X
	{
		get
		{
			return this.basePosition.x;
		}
		set
		{
			if (this.basePosition.x != value)
			{
				this.basePosition.x = Mathf.Round(value);
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public float Y
	{
		get
		{
			return this.basePosition.y;
		}
		set
		{
			if (this.basePosition.y != value)
			{
				this.basePosition.y = Mathf.Round(value);
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public float Width
	{
		get
		{
			return this.basePosition.width;
		}
		set
		{
			if (this.basePosition.width != value)
			{
				this.basePosition.width = Mathf.Round(value);
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public float Height
	{
		get
		{
			return this.basePosition.height;
		}
		set
		{
			if (this.basePosition.height != value)
			{
				this.basePosition.height = Mathf.Round(value);
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public float Alpha
	{
		get
		{
			return this.privateAlpha;
		}
		set
		{
			if (this.privateAlpha != value)
			{
				this.privateAlpha = value;
				if (Application.isPlaying)
				{
					this.ApplyDirtyAlpha();
				}
			}
		}
	}

	public float Z
	{
		get
		{
			return this.privateZ;
		}
		set
		{
			if (this.privateZ != value)
			{
				this.privateZ = value;
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public bool AttachLeft
	{
		get
		{
			return this.attachLeft;
		}
		set
		{
			if (this.attachLeft != value)
			{
				this.attachLeft = value;
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public float PercentLeft
	{
		get
		{
			return this.percentLeft;
		}
		set
		{
			if (this.percentLeft != value)
			{
				this.percentLeft = value;
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public float PixelMarginLeft
	{
		get
		{
			return this.pixelMarginLeft;
		}
		set
		{
			if (this.pixelMarginLeft != value)
			{
				this.pixelMarginLeft = Mathf.Round(value);
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public float PixelOffsetLeft
	{
		get
		{
			return this.pixelOffsetLeft;
		}
		set
		{
			if (this.pixelOffsetLeft != value)
			{
				this.pixelOffsetLeft = Mathf.Round(value);
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public bool AttachRight
	{
		get
		{
			return this.attachRight;
		}
		set
		{
			if (this.attachRight != value)
			{
				this.attachRight = value;
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public float PercentRight
	{
		get
		{
			return this.percentRight;
		}
		set
		{
			if (this.percentRight != value)
			{
				this.percentRight = value;
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public float PixelMarginRight
	{
		get
		{
			return this.pixelMarginRight;
		}
		set
		{
			if (this.pixelMarginRight != value)
			{
				this.pixelMarginRight = Mathf.Round(value);
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public float PixelOffsetRight
	{
		get
		{
			return this.pixelOffsetRight;
		}
		set
		{
			if (this.pixelOffsetRight != value)
			{
				this.pixelOffsetRight = Mathf.Round(value);
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public bool AttachTop
	{
		get
		{
			return this.attachTop;
		}
		set
		{
			if (this.attachTop != value)
			{
				this.attachTop = value;
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public float PercentTop
	{
		get
		{
			return this.percentTop;
		}
		set
		{
			if (this.percentTop != value)
			{
				this.percentTop = value;
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public float PixelMarginTop
	{
		get
		{
			return this.pixelMarginTop;
		}
		set
		{
			if (this.pixelMarginTop != value)
			{
				this.pixelMarginTop = Mathf.Round(value);
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public float PixelOffsetTop
	{
		get
		{
			return this.pixelOffsetTop;
		}
		set
		{
			if (this.pixelOffsetTop != value)
			{
				this.pixelOffsetTop = Mathf.Round(value);
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public bool AttachBottom
	{
		get
		{
			return this.attachBottom;
		}
		set
		{
			if (this.attachBottom != value)
			{
				this.attachBottom = value;
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public float PercentBottom
	{
		get
		{
			return this.percentBottom;
		}
		set
		{
			if (this.percentBottom != value)
			{
				this.percentBottom = value;
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public float PixelMarginBottom
	{
		get
		{
			return this.pixelMarginBottom;
		}
		set
		{
			if (this.pixelMarginBottom != value)
			{
				this.pixelMarginBottom = Mathf.Round(value);
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public float PixelOffsetBottom
	{
		get
		{
			return this.pixelOffsetBottom;
		}
		set
		{
			if (this.pixelOffsetBottom != value)
			{
				this.pixelOffsetBottom = Mathf.Round(value);
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public bool LocalMatrix
	{
		get
		{
			return this.localMatrix;
		}
		set
		{
			if (this.localMatrix != value)
			{
				this.localMatrix = value;
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public AgePivotMode PivotMode
	{
		get
		{
			return this.pivotMode;
		}
		set
		{
			if (this.pivotMode != value)
			{
				this.pivotMode = value;
				if (this.LocalMatrix)
				{
					this.ApplyDirtyPosition(false);
				}
			}
		}
	}

	public Vector2 PivotOffset
	{
		get
		{
			return this.pivotOffset;
		}
		set
		{
			if (this.pivotOffset != value)
			{
				this.pivotOffset = value;
				if (this.LocalMatrix)
				{
					this.ApplyDirtyPosition(false);
				}
			}
		}
	}

	public float TiltAngle
	{
		get
		{
			return this.tiltAngle;
		}
		set
		{
			if (this.tiltAngle != value)
			{
				this.tiltAngle = value;
				if (this.LocalMatrix)
				{
					this.ApplyDirtyPosition(false);
				}
			}
		}
	}

	public float UniformScale
	{
		get
		{
			return this.uniformScale;
		}
		set
		{
			if (this.uniformScale != value)
			{
				this.uniformScale = value;
				if (this.LocalMatrix)
				{
					this.ApplyDirtyPosition(false);
				}
			}
		}
	}

	public IComparer<AgeTransform> ChildrenComparer
	{
		get
		{
			return this.comparer;
		}
		set
		{
			this.comparer = value;
		}
	}

	public float ChildWidth { get; private set; }

	public float ChildHeight { get; private set; }

	public bool TableArrangement
	{
		get
		{
			return this.tableArrangement;
		}
		set
		{
			if (this.tableArrangement != value)
			{
				this.tableArrangement = value;
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public AgeTableSortOrder FirstOrder
	{
		get
		{
			return this.firstOrder;
		}
		set
		{
			if (this.firstOrder != value)
			{
				this.firstOrder = value;
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public AgeTableSortOrder SecondOrder
	{
		get
		{
			return this.secondOrder;
		}
		set
		{
			if (this.secondOrder != value)
			{
				this.secondOrder = value;
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public float HorizontalMargin
	{
		get
		{
			return this.horizontalMargin;
		}
		set
		{
			if (this.horizontalMargin != value)
			{
				this.horizontalMargin = Mathf.Round(value);
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public float HorizontalSpacing
	{
		get
		{
			return this.horizontalSpacing;
		}
		set
		{
			if (this.horizontalSpacing != value)
			{
				this.horizontalSpacing = Mathf.Round(value);
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public float VerticalMargin
	{
		get
		{
			return this.verticalMargin;
		}
		set
		{
			if (this.verticalMargin != value)
			{
				this.verticalMargin = Mathf.Round(value);
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public float VerticalSpacing
	{
		get
		{
			return this.verticalSpacing;
		}
		set
		{
			if (this.verticalSpacing != value)
			{
				this.verticalSpacing = Mathf.Round(value);
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public bool AutoResizeWidth
	{
		get
		{
			return this.autoResizeWidth;
		}
		set
		{
			if (this.autoResizeWidth != value)
			{
				this.autoResizeWidth = value;
				if (this.autoResizeWidth)
				{
					this.autoResizeHeight = false;
				}
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public bool AutoResizeHeight
	{
		get
		{
			return this.autoResizeHeight;
		}
		set
		{
			if (this.autoResizeHeight != value)
			{
				this.autoResizeHeight = value;
				if (this.autoResizeHeight)
				{
					this.autoResizeWidth = false;
				}
				this.ApplyDirtyPosition(false);
			}
		}
	}

	public void ArrangeChildren()
	{
		if (base.transform.childCount > 0)
		{
			this.CheckOrderValidity();
			if (this.VerifyChildren())
			{
				float num = 0f;
				float num2 = 0f;
				float num3 = 0f;
				float num4 = 0f;
				this.InitializePlacement(this.firstOrder, ref num, ref num2);
				this.InitializePlacement(this.secondOrder, ref num, ref num2);
				if (!Application.isPlaying)
				{
					if (this.comparer == null)
					{
						this.comparer = new AgeUtils.NameComparer();
					}
					List<AgeTransform> list = new List<AgeTransform>();
					foreach (object obj in base.transform)
					{
						Transform transform = (Transform)obj;
						AgeTransform component = transform.GetComponent<AgeTransform>();
						if (component != null && component.Visible && component.Alpha > 0f)
						{
							list.Add(transform.GetComponent<AgeTransform>());
						}
					}
					list.Sort(this.comparer);
					for (int i = 0; i < list.Count; i++)
					{
						AgeTransform ageTransform = list[i];
						if (ageTransform != null && ageTransform.visible)
						{
							ageTransform.X = num;
							ageTransform.Y = num2;
							this.ComputeDisplacement(this.firstOrder, ref num3, ref num4);
							if (this.CheckOverflow(num, num2, num3, num4, this.firstOrder))
							{
								this.InitializePlacement(this.firstOrder, ref num, ref num2);
								this.ComputeDisplacement(this.secondOrder, ref num3, ref num4);
							}
							num += num3;
							num2 += num4;
						}
					}
				}
				else
				{
					int num5 = 1;
					int num6 = 1;
					bool flag = false;
					bool flag2 = false;
					bool flag3 = false;
					for (int j = 0; j < this.GetChildren().Count; j++)
					{
						AgeTransform ageTransform2 = this.GetChildren()[j];
						if (ageTransform2.Visible && ageTransform2.Alpha > 0f)
						{
							flag = true;
							ageTransform2.X = num;
							ageTransform2.Y = num2;
							this.ComputeDisplacement(this.firstOrder, ref num3, ref num4);
							if (this.CheckOverflow(num, num2, num3, num4, this.firstOrder))
							{
								this.InitializePlacement(this.firstOrder, ref num, ref num2);
								this.ComputeDisplacement(this.secondOrder, ref num3, ref num4);
								if (num3 != 0f)
								{
									num6++;
									flag3 = true;
								}
								else if (num4 != 0f)
								{
									num5++;
									flag2 = true;
								}
							}
							else
							{
								flag2 = false;
								flag3 = false;
							}
							num += num3;
							num2 += num4;
						}
					}
					if (!flag)
					{
						num5 = 0;
						num6 = 0;
					}
					else if (flag3)
					{
						num6--;
					}
					else if (flag2)
					{
						num5--;
					}
					if (this.AutoResizeHeight)
					{
						float num7 = 2f * this.verticalMargin + (float)num5 * this.ChildHeight + (float)(num5 - 1) * this.verticalSpacing;
						if (this.Height < num7)
						{
							this.Height = num7;
						}
					}
					if (this.AutoResizeWidth)
					{
						float num8 = 2f * this.horizontalMargin + (float)num6 * this.ChildWidth + (float)(num6 - 1) * this.horizontalSpacing;
						if (this.Width < num8)
						{
							this.Width = num8;
						}
					}
				}
			}
		}
	}

	public int ComputeVisibleChildren()
	{
		int num = 0;
		for (int i = 0; i < this.children.Count; i++)
		{
			if (this.children[i].Visible && this.children[i].Alpha > 0f)
			{
				num++;
			}
		}
		return num;
	}

	public void RefreshChildrenArray<T>(T[] refTable, AgeTransform.RefreshTableItem<T> refreshDelegate, bool enableTheVisible = true, bool strictVisibility = false)
	{
		int count = this.GetChildren().Count;
		if (refTable != null)
		{
			if (refTable.Length > count)
			{
				Diagnostics.LogWarning("Not enough items in array to Refresh table <" + base.name + ">. Use ReserveTableItems before");
			}
			for (int i = 0; i < refTable.Length; i++)
			{
				this.GetChildren()[i].Alpha = 1f;
				this.GetChildren()[i].Enable = enableTheVisible;
				refreshDelegate(this.GetChildren()[i], refTable[i], i);
				if (strictVisibility)
				{
					this.GetChildren()[i].Visible = true;
				}
			}
			for (int j = refTable.Length; j < count; j++)
			{
				this.GetChildren()[j].Alpha = 0f;
				this.GetChildren()[j].Enable = false;
				if (strictVisibility)
				{
					this.GetChildren()[j].Visible = false;
				}
			}
		}
		else
		{
			for (int k = 0; k < count; k++)
			{
				this.GetChildren()[k].Alpha = 0f;
				this.GetChildren()[k].Enable = false;
				if (strictVisibility)
				{
					this.GetChildren()[k].Visible = false;
				}
			}
		}
		this.DirtyPosition = true;
	}

	public void RefreshChildrenIList<T>(IList<T> refCollection, AgeTransform.RefreshTableItem<T> refreshDelegate, bool enableTheVisible = true, bool strictVisibility = false)
	{
		List<AgeTransform> list = this.GetChildren();
		int count = list.Count;
		if (refCollection != null)
		{
			if (refCollection.Count > count)
			{
				Diagnostics.LogWarning("Not enough items in list to Refresh table <{0}>. Use ReserveTableItems before", new object[]
				{
					base.name
				});
			}
			for (int i = 0; i < refCollection.Count; i++)
			{
				list[i].Alpha = 1f;
				list[i].Enable = enableTheVisible;
				refreshDelegate(list[i], refCollection[i], i);
				if (strictVisibility)
				{
					list[i].Visible = true;
				}
			}
			for (int j = refCollection.Count; j < count; j++)
			{
				list[j].Alpha = 0f;
				list[j].Enable = false;
				if (strictVisibility)
				{
					list[j].Visible = false;
				}
			}
		}
		else
		{
			for (int k = 0; k < count; k++)
			{
				list[k].Alpha = 0f;
				list[k].Enable = false;
				if (strictVisibility)
				{
					list[k].Visible = false;
				}
			}
		}
		this.DirtyPosition = true;
	}

	public AgeTransform InstanciateChild(Transform prefab, string nameTemplate = "Child")
	{
		Transform transform = UnityEngine.Object.Instantiate<Transform>(prefab);
		transform.parent = base.transform;
		transform.name = nameTemplate;
		AgeTransform component = transform.GetComponent<AgeTransform>();
		component.Init();
		return component;
	}

	public void ReserveChildren(int wantedNumber, Transform prefab, string nameTemplate = "Item")
	{
		int count = this.GetChildren().Count;
		for (int i = count; i < wantedNumber; i++)
		{
			Transform transform = UnityEngine.Object.Instantiate<Transform>(prefab);
			transform.parent = base.transform;
			transform.name = nameTemplate + i.ToString("D3");
			transform.GetComponent<AgeTransform>().Init();
		}
	}

	public void Sort()
	{
		if (this.comparer == null)
		{
			this.comparer = new AgeUtils.NameComparer();
		}
		this.GetChildren().Sort(this.comparer);
	}

	private void CheckOrderValidity()
	{
		if (this.firstOrder == AgeTableSortOrder.LEFT_TO_RIGHT && (this.secondOrder == AgeTableSortOrder.LEFT_TO_RIGHT || this.secondOrder == AgeTableSortOrder.RIGHT_TO_LEFT))
		{
			this.secondOrder = AgeTableSortOrder.TOP_TO_BOTTOM;
		}
		else if (this.firstOrder == AgeTableSortOrder.RIGHT_TO_LEFT && (this.secondOrder == AgeTableSortOrder.LEFT_TO_RIGHT || this.secondOrder == AgeTableSortOrder.RIGHT_TO_LEFT))
		{
			this.secondOrder = AgeTableSortOrder.TOP_TO_BOTTOM;
		}
		else if (this.firstOrder == AgeTableSortOrder.TOP_TO_BOTTOM && (this.secondOrder == AgeTableSortOrder.TOP_TO_BOTTOM || this.secondOrder == AgeTableSortOrder.BOTTOM_TO_TOP))
		{
			this.secondOrder = AgeTableSortOrder.LEFT_TO_RIGHT;
		}
		else if (this.firstOrder == AgeTableSortOrder.BOTTOM_TO_TOP && (this.secondOrder == AgeTableSortOrder.TOP_TO_BOTTOM || this.secondOrder == AgeTableSortOrder.BOTTOM_TO_TOP))
		{
			this.secondOrder = AgeTableSortOrder.LEFT_TO_RIGHT;
		}
	}

	private bool VerifyChildren()
	{
		bool result = true;
		if (!Application.isPlaying)
		{
			bool flag = true;
			foreach (object obj in base.transform)
			{
				Transform transform = (Transform)obj;
				AgeTransform component = transform.GetComponent<AgeTransform>();
				if (component != null)
				{
					if (flag)
					{
						this.ChildWidth = component.Width;
						this.ChildHeight = component.Height;
						flag = false;
					}
					else if (this.ChildWidth != component.Width || this.ChildHeight != component.Height)
					{
						Diagnostics.LogError(string.Concat(new string[]
						{
							"Object <",
							component.name,
							"> does not have the same size as other children of table object <",
							base.name,
							">"
						}));
						result = false;
						break;
					}
				}
			}
		}
		else
		{
			bool flag2 = true;
			this.GetChildren<AgeTransform>(ref this.ageTransformChildren, true);
			for (int i = 0; i < this.ageTransformChildren.Count; i++)
			{
				AgeTransform ageTransform = this.ageTransformChildren[i];
				if (flag2)
				{
					this.ChildWidth = ageTransform.Width;
					this.ChildHeight = ageTransform.Height;
					flag2 = false;
				}
				else if (this.ChildWidth != ageTransform.Width || this.ChildHeight != ageTransform.Height)
				{
					if (ageTransform.enable)
					{
						Diagnostics.LogWarning(string.Concat(new string[]
						{
							"Object <",
							ageTransform.name,
							"> does not have the same size as other children of table object <",
							base.name,
							">"
						}));
					}
					result = false;
					break;
				}
			}
		}
		return result;
	}

	private void InitializePlacement(AgeTableSortOrder order, ref float x, ref float y)
	{
		if (order == AgeTableSortOrder.LEFT_TO_RIGHT)
		{
			x = this.horizontalMargin;
		}
		else if (order == AgeTableSortOrder.RIGHT_TO_LEFT)
		{
			x = this.Width - this.horizontalMargin - this.ChildWidth;
		}
		if (order == AgeTableSortOrder.TOP_TO_BOTTOM)
		{
			y = this.verticalMargin;
		}
		else if (order == AgeTableSortOrder.BOTTOM_TO_TOP)
		{
			y = this.Height - this.verticalMargin - this.ChildHeight;
		}
	}

	private void ComputeDisplacement(AgeTableSortOrder order, ref float deltaX, ref float deltaY)
	{
		if (order == AgeTableSortOrder.LEFT_TO_RIGHT)
		{
			deltaX = this.ChildWidth + this.horizontalSpacing;
			deltaY = 0f;
		}
		else if (order == AgeTableSortOrder.RIGHT_TO_LEFT)
		{
			deltaX = -this.ChildWidth - this.horizontalSpacing;
			deltaY = 0f;
		}
		else if (order == AgeTableSortOrder.TOP_TO_BOTTOM)
		{
			deltaX = 0f;
			deltaY = this.ChildHeight + this.verticalSpacing;
		}
		else if (order == AgeTableSortOrder.BOTTOM_TO_TOP)
		{
			deltaX = 0f;
			deltaY = -this.ChildHeight - this.verticalSpacing;
		}
	}

	private bool CheckOverflow(float x, float y, float deltaX, float deltaY, AgeTableSortOrder order)
	{
		bool result = false;
		if (order == AgeTableSortOrder.LEFT_TO_RIGHT)
		{
			result = (x + deltaX + this.ChildWidth > this.Width);
		}
		else if (order == AgeTableSortOrder.RIGHT_TO_LEFT)
		{
			result = (x + deltaX < 0f);
		}
		else if (order == AgeTableSortOrder.TOP_TO_BOTTOM)
		{
			result = (y + deltaY + this.ChildHeight > this.Height);
		}
		else if (order == AgeTableSortOrder.BOTTOM_TO_TOP)
		{
			result = (y + deltaY < 0f);
		}
		return result;
	}

	public bool FixedSize
	{
		get
		{
			return this.fixedSize;
		}
		set
		{
			this.fixedSize = value;
		}
	}

	public void ApplyHighDefinitionHierarchical(float scale)
	{
		base.SendMessage("OnApplyHighDefinition", scale, SendMessageOptions.DontRequireReceiver);
		if (!this.FixedSize)
		{
			for (int i = 0; i < this.children.Count; i++)
			{
				this.children[i].ApplyHighDefinitionHierarchical(scale);
			}
		}
	}

	public AgeTransform GetParent()
	{
		if (Application.isPlaying)
		{
			if (!this.initialized)
			{
				this.Init();
			}
			return this.parent;
		}
		if (base.transform.parent == null)
		{
			return null;
		}
		return base.transform.parent.GetComponent<AgeTransform>();
	}

	public List<AgeTransform> GetChildren()
	{
		return this.children;
	}

	public List<T> GetChildren<T>(bool visibleOnly = true) where T : Component
	{
		List<T> result = new List<T>();
		this.GetChildren<T>(ref result, visibleOnly);
		return result;
	}

	public List<T> GetChildren<T>(ref List<T> childrenComponents, bool visibleOnly = true) where T : Component
	{
		if (childrenComponents == null)
		{
			childrenComponents = new List<T>();
		}
		else
		{
			childrenComponents.Clear();
		}
		for (int i = 0; i < this.children.Count; i++)
		{
			if (this.children[i] != null)
			{
				bool flag = this.children[i].Visible && this.children[i].Alpha > 0f;
				if (!visibleOnly || flag)
				{
					T component = this.children[i].GetComponent<T>();
					if (component != null)
					{
						childrenComponents.Add(component);
					}
				}
			}
		}
		return childrenComponents;
	}

	public Rect GetRenderedPosition()
	{
		return this.renderedPosition;
	}

	public bool IsChildOf(AgeTransform relative)
	{
		bool result = false;
		AgeTransform ageTransform = this.GetParent();
		while (ageTransform != null)
		{
			if (ageTransform != relative)
			{
				ageTransform = ageTransform.GetParent();
			}
			else
			{
				result = true;
				ageTransform = null;
			}
		}
		return result;
	}

	public void SetPrivateAlpha(float value)
	{
		this.privateAlpha = value;
	}

	public void Init()
	{
		if (!this.initialized)
		{
			this.initialized = true;
			if (base.transform.parent != null)
			{
				this.parent = base.transform.parent.GetComponent<AgeTransform>();
				if (this.parent != null)
				{
					this.parent.AddChild(this);
				}
			}
			foreach (object obj in base.transform)
			{
				Transform transform = (Transform)obj;
				AgeTransform component = transform.GetComponent<AgeTransform>();
				if (component != null && !component.initialized)
				{
					component.Init();
				}
			}
			if (this.AgeControl != null)
			{
				base.SendMessage("OnResetInteraction", SendMessageOptions.DontRequireReceiver);
			}
			if (this.modifiers != null)
			{
				for (int i = 0; i < this.modifiers.Length; i++)
				{
					this.modifiers[i].Init();
				}
			}
			this.AgeMeshBuilder = null;
			AgeTransform ageTransform = this;
			while (ageTransform != null && this.AgeMeshBuilder == null)
			{
				AgeMeshBuilder component2 = ageTransform.GetComponent<AgeMeshBuilder>();
				if (component2 != null)
				{
					this.AgeMeshBuilder = component2;
				}
				else
				{
					Transform transform2 = ageTransform.transform.parent;
					if (transform2 != null)
					{
						ageTransform = transform2.GetComponent<AgeTransform>();
					}
					else
					{
						ageTransform = null;
					}
				}
			}
		}
	}

	public void DestroyAllChildren()
	{
		for (int i = 0; i < this.children.Count; i++)
		{
			UnityEngine.Object.Destroy(this.children[i].gameObject);
		}
		this.children.Clear();
	}

	public void BuildAtlasImages(AgeAtlas atlas)
	{
		base.SendMessage("OnBuildAtlasImages", atlas, SendMessageOptions.DontRequireReceiver);
		List<AgeTransform> list = this.children;
		for (int i = 0; i < list.Count; i++)
		{
			list[i].BuildAtlasImages(atlas);
		}
	}

	public void BuildAtlasFonts(AgeAtlas atlas)
	{
		base.SendMessage("OnBuildAtlasFonts", atlas, SendMessageOptions.DontRequireReceiver);
		List<AgeTransform> list = this.children;
		for (int i = 0; i < list.Count; i++)
		{
			list[i].BuildAtlasFonts(atlas);
		}
	}

	public void ApplyDirtyStructure()
	{
		if (this.AgePrimitive != null && this.AgePrimitive.AgeMesh != null)
		{
			this.AgePrimitive.AgeMesh.MarkDirtyStructure();
		}
		List<AgeTransform> list = this.children;
		for (int i = 0; i < list.Count; i++)
		{
			list[i].ApplyDirtyStructure();
		}
	}

	public void ApplyDirtyAlpha()
	{
		if (this.AgePrimitive != null && this.AgePrimitive.AgeMesh != null)
		{
			this.AgePrimitive.AgeMesh.MarkDirtyColor();
		}
		List<AgeTransform> list = this.children;
		for (int i = 0; i < list.Count; i++)
		{
			list[i].ApplyDirtyAlpha();
		}
	}

	public void ComputeExtents(Rect renderArea)
	{
		AgeTransform ageTransform = this.GetParent();
		float width = renderArea.width;
		float height = renderArea.height;
		if (ageTransform != null)
		{
			width = ageTransform.Width;
			height = ageTransform.Height;
		}
		if (this.attachLeft && !this.attachRight)
		{
			this.X = Mathf.Round(this.pixelMarginLeft + (width - this.pixelMarginLeft) * 0.01f * this.percentLeft + this.pixelOffsetLeft);
		}
		else if (!this.attachLeft && this.attachRight)
		{
			this.X = Mathf.Round((width - this.pixelMarginRight) * 0.01f * this.percentRight - this.Width - this.pixelOffsetRight);
		}
		else if (this.attachLeft && this.attachRight)
		{
			if (this.percentLeft <= this.percentRight)
			{
				this.X = Mathf.Round(this.pixelMarginLeft + (width - this.pixelMarginLeft - this.pixelMarginRight) * 0.01f * this.percentLeft + this.pixelOffsetLeft);
				this.Width = Mathf.Round((width - this.pixelMarginLeft - this.pixelMarginRight) * 0.01f * (this.percentRight - this.percentLeft) - this.pixelOffsetRight - this.pixelOffsetLeft);
			}
		}
		if (this.attachTop && !this.attachBottom)
		{
			this.Y = Mathf.Round(this.pixelMarginTop + (height - this.pixelMarginTop) * 0.01f * this.percentTop + this.pixelOffsetTop);
		}
		else if (!this.attachTop && this.attachBottom)
		{
			this.Y = Mathf.Round((height - this.pixelMarginBottom) * 0.01f * this.percentBottom - this.Height - this.pixelOffsetBottom);
		}
		else if (this.attachTop && this.attachBottom)
		{
			if (this.percentTop <= this.percentBottom)
			{
				this.Y = Mathf.Round(this.pixelMarginTop + (height - this.pixelMarginTop - this.pixelMarginBottom) * 0.01f * this.percentTop + this.pixelOffsetTop);
				this.Height = Mathf.Round((height - this.pixelMarginTop - this.pixelMarginBottom) * 0.01f * (this.percentBottom - this.percentTop) - this.pixelOffsetTop - this.pixelOffsetBottom);
			}
		}
	}

	public void ComputeGlobalPosition(out Rect globalPosition)
	{
		globalPosition = this.basePosition;
		AgeTransform ageTransform = this.GetParent();
		while (ageTransform != null)
		{
			globalPosition.x += ageTransform.basePosition.x;
			globalPosition.y += ageTransform.basePosition.y;
			ageTransform = ageTransform.GetParent();
		}
	}

	public bool IsGloballyVisible()
	{
		if (!this.visible)
		{
			return false;
		}
		AgeTransform ageTransform = this.GetParent();
		return !(ageTransform != null) || ageTransform.IsGloballyVisible();
	}

	public void ForceHeight(float height, bool applyDirtyPosition = false)
	{
		this.basePosition.height = height;
		if (applyDirtyPosition)
		{
			this.ApplyDirtyPosition(false);
		}
		else
		{
			this.dirtyPosition = false;
		}
	}

	public void UpdateHierarchy(Rect renderArea, bool forceUpdate = false)
	{
		if (this.visible || forceUpdate)
		{
			this.UpdateModifiers();
			if (this.dirtyPosition || forceUpdate)
			{
				if (this.anchored)
				{
					this.ComputeExtents(renderArea);
				}
				if (this.TableArrangement)
				{
					this.ArrangeChildren();
				}
				if (this.AgeControl != null)
				{
					this.AgeControl.OnPositionRecomputed();
				}
				if (this.AgePrimitive != null)
				{
					this.AgePrimitive.OnPositionRecomputed(forceUpdate);
				}
				this.dirtyPosition = false;
			}
			if (Application.isPlaying)
			{
				List<AgeTransform> list = this.children;
				for (int i = 0; i < list.Count; i++)
				{
					list[i].UpdateHierarchy(renderArea, forceUpdate);
				}
			}
			else
			{
				foreach (object obj in base.transform)
				{
					Transform transform = (Transform)obj;
					AgeTransform component = transform.GetComponent<AgeTransform>();
					if (component != null)
					{
						component.UpdateHierarchy(renderArea, false);
					}
				}
			}
		}
	}

	public void UpdateInteractivity(Vector2 cursor, Vector2 positionOffset, Rect viewport, AgeRenderInfo renderInfo, float combinedZ, ref float activeZ, ref AgeControl activeControl, ref AgeTransform overrolledTransform, ref bool mouseCovered, ref bool wheelGrabbed, bool continueActive, bool stopSearchingForTooltips)
	{
		if (this.Visible && this.Alpha > 0f && !this.NoOverroll)
		{
			if (!this.Enable)
			{
				continueActive = false;
			}
			if (this.LocalMatrix && (this.TiltAngle != 0f || this.UniformScale != 1f))
			{
				renderInfo.LocalMatrix = true;
				Matrix4x4 inverse = this.BuildLocalMatrix(positionOffset, false).inverse;
				renderInfo.MatrixList.Insert(0, inverse);
			}
			Vector2 vector = cursor;
			if (renderInfo.LocalMatrix)
			{
				for (int i = renderInfo.MatrixList.Count - 1; i >= 0; i--)
				{
					Matrix4x4 matrix4x = renderInfo.MatrixList[i];
					float x = matrix4x[0, 0] * vector.x + matrix4x[0, 1] * vector.y + matrix4x[0, 2];
					float y = matrix4x[1, 0] * vector.x + matrix4x[1, 1] * vector.y + matrix4x[1, 2];
					vector.x = x;
					vector.y = y;
				}
			}
			combinedZ = Mathf.Clamp(combinedZ + this.Z, 0f, 0.99f);
			if (this.AgeControl != null)
			{
				if (this.AgeControl.Contains(vector - positionOffset) && (viewport.Contains(cursor) || this.OverrideClip) && combinedZ >= activeZ)
				{
					if (!stopSearchingForTooltips && (this.AgeTooltip != null || this.CoversMouse))
					{
						overrolledTransform = this;
					}
					if (!mouseCovered && this.CoversMouse)
					{
						mouseCovered = true;
					}
					if (!wheelGrabbed && this.GrabsWheel)
					{
						wheelGrabbed = true;
					}
					if (continueActive && this.enable)
					{
						activeControl = this.AgeControl;
						activeZ = combinedZ;
					}
					else
					{
						continueActive = false;
					}
				}
			}
			else
			{
				Rect rect = this.basePosition;
				rect.x += positionOffset.x;
				rect.y += positionOffset.y;
				if (rect.Contains(vector) && (viewport.Contains(cursor) || this.OverrideClip) && combinedZ >= activeZ)
				{
					if (!stopSearchingForTooltips && (this.AgeTooltip != null || this.CoversMouse))
					{
						overrolledTransform = this;
					}
					if (!mouseCovered && this.CoversMouse)
					{
						mouseCovered = true;
					}
					if (!wheelGrabbed && this.GrabsWheel)
					{
						wheelGrabbed = true;
					}
				}
			}
			if (this.ClipContent)
			{
				Rect position = this.Position;
				position.x += positionOffset.x;
				position.y += positionOffset.y;
				viewport = AgeUtils.ClipRectangle(viewport, position);
			}
			positionOffset.x += this.X;
			positionOffset.y += this.Y;
			List<AgeTransform> list = this.children;
			for (int j = 0; j < list.Count; j++)
			{
				list[j].UpdateInteractivity(cursor, positionOffset, viewport, renderInfo, combinedZ, ref activeZ, ref activeControl, ref overrolledTransform, ref mouseCovered, ref wheelGrabbed, continueActive, stopSearchingForTooltips || this.StopSearchingForTooltips);
			}
			if (this.LocalMatrix && (this.TiltAngle != 0f || this.UniformScale != 1f))
			{
				renderInfo.MatrixList.RemoveAt(0);
				if (renderInfo.MatrixList.Count == 0)
				{
					renderInfo.LocalMatrix = false;
				}
			}
		}
	}

	public void Render(AgeRenderInfo renderInfo, Vector2 positionOffset, float combinedAlpha, float combinedZ, float zBias)
	{
		if (this.visible)
		{
			if (this.LocalMatrix && (this.TiltAngle != 0f || this.UniformScale != 1f))
			{
				renderInfo.LocalMatrix = true;
				Matrix4x4 item = this.BuildLocalMatrix(positionOffset, true);
				renderInfo.MatrixList.Add(item);
			}
			Rect clip = renderInfo.Clip;
			if (this.ClipContent)
			{
				Rect position = this.Position;
				position.x = positionOffset.x + position.x;
				position.y = positionOffset.y + position.y;
				renderInfo.Clip = AgeUtils.ClipRectangle(renderInfo.Clip, position);
			}
			if (this.OverrideClip)
			{
				renderInfo.Clip = renderInfo.RenderArea;
			}
			positionOffset.x += this.X;
			positionOffset.y += this.Y;
			combinedAlpha *= this.Alpha;
			if (!this.Enable && this.FadeOnDisable)
			{
				combinedAlpha *= 0.5f;
			}
			combinedZ = Mathf.Clamp(combinedZ + this.Z + zBias, 0f, 0.99f);
			renderInfo.RenderPosition.x = positionOffset.x;
			renderInfo.RenderPosition.y = positionOffset.y;
			renderInfo.RenderPosition.width = this.Width;
			renderInfo.RenderPosition.height = this.Height;
			if (Application.isPlaying && this.StartNewMesh)
			{
				renderInfo.MeshBuilder.RequestNewMesh();
			}
			if (!Application.isPlaying && this.AgeControl != null)
			{
				this.AgeControl.EditorRender(renderInfo, combinedAlpha, combinedZ, zBias);
			}
			if (this.AgePrimitive != null)
			{
				this.AgePrimitive.Render(renderInfo, combinedAlpha, combinedZ, zBias);
			}
			this.renderedPosition = renderInfo.RenderPosition;
			if (Application.isPlaying)
			{
				List<AgeTransform> list = this.children;
				for (int i = 0; i < list.Count; i++)
				{
					list[i].Render(renderInfo, positionOffset, combinedAlpha, combinedZ, zBias);
				}
			}
			else
			{
				foreach (object obj in base.transform)
				{
					Transform transform = (Transform)obj;
					AgeTransform component = transform.GetComponent<AgeTransform>();
					if (component != null)
					{
						component.Render(renderInfo, positionOffset, combinedAlpha, combinedZ, zBias);
					}
				}
			}
			if (this.ClipContent)
			{
				renderInfo.Clip = clip;
			}
			if (this.LocalMatrix && (this.TiltAngle != 0f || this.UniformScale != 1f))
			{
				renderInfo.MatrixList.RemoveAt(renderInfo.MatrixList.Count - 1);
				if (renderInfo.MatrixList.Count == 0)
				{
					renderInfo.LocalMatrix = false;
				}
			}
		}
	}

	public bool HaveModifiersRunning()
	{
		for (int i = 0; i < this.modifiers.Length; i++)
		{
			if (this.modifiers[i].IsStarted() && !this.modifiers[i].IsComplete())
			{
				return true;
			}
		}
		return false;
	}

	public void StartAllModifiers(bool forward = true, bool recursive = false)
	{
		for (int i = 0; i < this.modifiers.Length; i++)
		{
			if (forward)
			{
				this.modifiers[i].StartAnimation();
			}
			else
			{
				this.modifiers[i].StartAnimationReverse();
			}
		}
		if (recursive)
		{
			List<AgeTransform> list = this.children;
			for (int j = 0; j < list.Count; j++)
			{
				list[j].StartAllModifiers(forward, recursive);
			}
		}
	}

	public void ResetAllModifiers(bool toStart = true, bool recursive = false)
	{
		for (int i = 0; i < this.modifiers.Length; i++)
		{
			if (toStart)
			{
				this.modifiers[i].Reset();
			}
			else
			{
				this.modifiers[i].ResetToEnd();
			}
		}
		if (recursive)
		{
			List<AgeTransform> list = this.children;
			for (int j = 0; j < list.Count; j++)
			{
				list[j].ResetAllModifiers(toStart, recursive);
			}
		}
	}

	protected virtual void Start()
	{
		this.Init();
	}

	protected virtual void Awake()
	{
		this.children = new List<AgeTransform>();
		this.privateAudio = base.GetComponent<AgeAudio>();
		this.privateControl = base.GetComponent<AgeControl>();
		this.modifiers = base.GetComponents<AgeModifier>();
		this.privatePrimitive = base.GetComponent<AgePrimitive>();
		this.privateTooltip = base.GetComponent<AgeTooltip>();
		if (AgeUtils.HighDefinition)
		{
			this.OnApplyHighDefinition(AgeUtils.HighDefinitionFactor);
		}
	}

	protected virtual void OnDestroy()
	{
		if (this.GetParent() != null)
		{
			this.GetParent().RemoveChild(this);
		}
	}

	protected void AddChild(AgeTransform child)
	{
		if (base.enabled)
		{
			this.children.Add(child);
			AgeMeshBuilder ageMeshBuilder = this.AgeMeshBuilder;
			if (ageMeshBuilder != null)
			{
				ageMeshBuilder.NotifyStructureChange();
			}
			this.ApplyDirtyPosition(false);
			this.children.Sort(AgeTransform.InternalComparer);
		}
	}

	protected void RemoveChild(AgeTransform child)
	{
		this.children.Remove(child);
		AgeMeshBuilder ageMeshBuilder = this.AgeMeshBuilder;
		if (ageMeshBuilder != null)
		{
			ageMeshBuilder.NotifyStructureChange();
		}
		this.ApplyDirtyPosition(false);
	}

	private void ApplyDirtyPosition(bool updateClipping = false)
	{
		this.dirtyPosition = true;
		if (updateClipping && this.AgePrimitive != null && this.AgePrimitive.AgeMesh != null)
		{
			this.AgePrimitive.AgeMesh.MarkDirtyUv2();
		}
		if (this.PropagateDirty)
		{
			if (this.ClipContent)
			{
				updateClipping = true;
			}
			if (Application.isPlaying)
			{
				List<AgeTransform> list = this.children;
				for (int i = 0; i < list.Count; i++)
				{
					list[i].ApplyDirtyPosition(updateClipping);
				}
			}
			else
			{
				foreach (object obj in base.transform)
				{
					Transform transform = (Transform)obj;
					AgeTransform component = transform.GetComponent<AgeTransform>();
					if (component != null)
					{
						component.ApplyDirtyPosition(false);
					}
				}
			}
		}
	}

	private Matrix4x4 BuildLocalMatrix(Vector2 positionOffset, bool invert)
	{
		float num = this.UniformScale * Mathf.Cos(-0.0174532924f * this.TiltAngle);
		float num2 = this.UniformScale * Mathf.Sin(-0.0174532924f * this.TiltAngle);
		float num3 = positionOffset.x + this.X + this.PivotOffset.x;
		float num4 = positionOffset.y + this.Y + this.PivotOffset.y;
		switch (this.PivotMode)
		{
		case AgePivotMode.TOP_CENTER:
			num3 += this.Width * 0.5f;
			break;
		case AgePivotMode.TOP_RIGHT:
			num3 += this.Width;
			break;
		case AgePivotMode.RIGHT_CENTER:
			num3 += this.Width;
			num4 += this.Height * 0.5f;
			break;
		case AgePivotMode.BOTTOM_RIGHT:
			num3 += this.Width;
			num4 += this.Height;
			break;
		case AgePivotMode.BOTTOM_CENTER:
			num3 += this.Width * 0.5f;
			num4 += this.Height;
			break;
		case AgePivotMode.BOTTOM_LEFT:
			num4 += this.Height;
			break;
		case AgePivotMode.LEFT_CENTER:
			num4 += this.Height * 0.5f;
			break;
		case AgePivotMode.CENTER:
			num3 += this.Width * 0.5f;
			num4 += this.Height * 0.5f;
			break;
		}
		if (invert)
		{
			num4 = (float)(Screen.height + 1) - num4;
		}
		Matrix4x4 identity = Matrix4x4.identity;
		Vector4 zero = Vector4.zero;
		zero.Set(num, -num2, -(num3 * num) + num4 * num2 + num3, 0f);
		identity.SetRow(0, zero);
		zero.Set(num2, num, -(num3 * num2) - num4 * num + num4, 0f);
		identity.SetRow(1, zero);
		zero.Set(0f, 0f, 1f, 0f);
		identity.SetRow(2, zero);
		zero.Set(0f, 0f, 0f, 1f);
		identity.SetRow(3, zero);
		return identity;
	}

	private void OnApplyHighDefinition(float scale)
	{
		if (!this.FixedSize)
		{
			this.basePosition.x = Mathf.Round(scale * this.basePosition.x);
			this.basePosition.y = Mathf.Round(scale * this.basePosition.y);
			this.basePosition.width = Mathf.Round(scale * this.basePosition.width);
			this.basePosition.height = Mathf.Round(scale * this.basePosition.height);
			this.pixelMarginRight = Mathf.Round(scale * this.pixelMarginRight);
			this.pixelOffsetRight = Mathf.Round(scale * this.pixelOffsetRight);
			this.pixelMarginLeft = Mathf.Round(scale * this.pixelMarginLeft);
			this.pixelOffsetLeft = Mathf.Round(scale * this.pixelOffsetLeft);
			this.pixelMarginTop = Mathf.Round(scale * this.pixelMarginTop);
			this.pixelOffsetTop = Mathf.Round(scale * this.pixelOffsetTop);
			this.pixelMarginBottom = Mathf.Round(scale * this.pixelMarginBottom);
			this.pixelOffsetBottom = Mathf.Round(scale * this.pixelOffsetBottom);
			this.verticalMargin = Mathf.Round(scale * this.verticalMargin);
			this.verticalSpacing = Mathf.Round(scale * this.verticalSpacing);
			this.HorizontalMargin = Mathf.Round(scale * this.horizontalMargin);
			this.HorizontalSpacing = Mathf.Round(scale * this.horizontalSpacing);
		}
	}

	private void OnApplyImageHierarchical(Texture2D image)
	{
		base.gameObject.SendMessage("OnApplyImage", image, SendMessageOptions.DontRequireReceiver);
		List<AgeTransform> list = this.children;
		for (int i = 0; i < list.Count; i++)
		{
			list[i].gameObject.SendMessage("OnApplyImageHierarchical", image, SendMessageOptions.DontRequireReceiver);
		}
	}

	private void OnApplyColorSpecialHierarchical(Color color)
	{
		base.gameObject.SendMessage("OnApplyColorSpecial", color, SendMessageOptions.DontRequireReceiver);
		List<AgeTransform> list = this.children;
		for (int i = 0; i < list.Count; i++)
		{
			list[i].gameObject.SendMessage("OnApplyColorSpecialHierarchical", color, SendMessageOptions.DontRequireReceiver);
		}
	}

	private void OnApplyImageSpecialHierarchical(Texture2D image)
	{
		base.gameObject.SendMessage("OnApplyImageSpecial", image, SendMessageOptions.DontRequireReceiver);
		List<AgeTransform> list = this.children;
		for (int i = 0; i < list.Count; i++)
		{
			list[i].gameObject.SendMessage("OnApplyImageSpecialHierarchical", image, SendMessageOptions.DontRequireReceiver);
		}
	}

	private void OnApplyTextHierarchical(string text)
	{
		base.gameObject.SendMessage("OnApplyText", text, SendMessageOptions.DontRequireReceiver);
		List<AgeTransform> list = this.children;
		for (int i = 0; i < list.Count; i++)
		{
			list[i].gameObject.SendMessage("OnApplyTextHierarchical", text, SendMessageOptions.DontRequireReceiver);
		}
	}

	private void OnResetInteraction()
	{
		if (Application.isPlaying)
		{
			List<AgeTransform> list = this.GetChildren();
			for (int i = 0; i < list.Count; i++)
			{
				list[i].gameObject.SendMessage("OnResetInteraction", SendMessageOptions.DontRequireReceiver);
			}
		}
	}

	private void OnRaisePositionRecomputed()
	{
		if (Application.isPlaying && this.GetParent() != null)
		{
			this.GetParent().SendMessage("OnRaisePositionRecomputed", SendMessageOptions.DontRequireReceiver);
		}
	}

	private void UpdateModifiers()
	{
		if (this.modifiers != null)
		{
			for (int i = 0; i < this.modifiers.Length; i++)
			{
				this.modifiers[i].UpdateModifier();
			}
		}
	}

	public void SetupCustomELCPScaling(float scale)
	{
		if (this.customELCPscaling == scale)
		{
			return;
		}
		if (this.customELCPscaling > 0f)
		{
			this.ApplyHighDefinitionHierarchical(1f / this.customELCPscaling);
		}
		this.customELCPscaling = scale;
		this.ApplyHighDefinitionHierarchical(this.customELCPscaling);
	}

	public const float FadeOnDisableFactor = 0.5f;

	public const float MaxZ = 0.99f;

	public const float ScaleProximity = 0.01f;

	[SerializeField]
	private bool tableArrangement;

	[SerializeField]
	private AgeTableSortOrder firstOrder;

	[SerializeField]
	private AgeTableSortOrder secondOrder = AgeTableSortOrder.TOP_TO_BOTTOM;

	[SerializeField]
	private float horizontalMargin;

	[SerializeField]
	private float horizontalSpacing;

	[SerializeField]
	private float verticalMargin;

	[SerializeField]
	private float verticalSpacing;

	[SerializeField]
	private bool autoResizeHeight;

	[SerializeField]
	private bool autoResizeWidth;

	private IComparer<AgeTransform> comparer;

	private List<AgeTransform> ageTransformChildren;

	public static IComparer<AgeTransform> InternalComparer;

	public bool ClipContent;

	public bool OverrideClip;

	public bool FadeOnDisable;

	public bool CoversMouse;

	public bool GrabsWheel;

	public bool NoOverroll;

	public bool StopSearchingForTooltips;

	private Rect renderedPosition = default(Rect);

	private AgeAudio privateAudio;

	private AgeControl privateControl;

	private AgePrimitive privatePrimitive;

	private AgeTooltip privateTooltip;

	private AgeModifier[] modifiers;

	[SerializeField]
	private Rect basePosition = Rect.MinMaxRect(0f, 0f, 50f, 50f);

	[SerializeField]
	private bool visible = true;

	[SerializeField]
	private bool enable = true;

	[SerializeField]
	private float privateZ;

	[SerializeField]
	private float privateAlpha = 1f;

	[SerializeField]
	private bool anchored;

	[SerializeField]
	private bool attachLeft;

	[SerializeField]
	private float percentLeft;

	[SerializeField]
	private float pixelMarginLeft;

	[SerializeField]
	private float pixelOffsetLeft;

	[SerializeField]
	private bool attachRight;

	[SerializeField]
	private float percentRight = 100f;

	[SerializeField]
	private float pixelMarginRight;

	[SerializeField]
	private float pixelOffsetRight;

	[SerializeField]
	private bool attachTop;

	[SerializeField]
	private float percentTop;

	[SerializeField]
	private float pixelMarginTop;

	[SerializeField]
	private float pixelOffsetTop;

	[SerializeField]
	private bool attachBottom;

	[SerializeField]
	private float percentBottom = 100f;

	[SerializeField]
	private float pixelMarginBottom;

	[SerializeField]
	private float pixelOffsetBottom;

	[SerializeField]
	private bool localMatrix;

	[SerializeField]
	private AgePivotMode pivotMode = AgePivotMode.CENTER;

	[SerializeField]
	private Vector2 pivotOffset;

	[SerializeField]
	private float tiltAngle;

	[SerializeField]
	private float uniformScale = 1f;

	[SerializeField]
	private bool startNewMesh;

	[SerializeField]
	private bool fixedSize;

	private AgeTransform parent;

	private List<AgeTransform> children;

	private bool initialized;

	private bool dirtyPosition = true;

	private bool propagateDirty = true;

	private float customELCPscaling;

	public delegate void RefreshTableItem<T>(AgeTransform tableItem, T reference, int index);
}
