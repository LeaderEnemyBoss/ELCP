using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class KaijuGarrisonLabelLayerPanel : LabelLayerPanel<KaijuGarrisonLabelLayer>
{
	public override void Bind(global::Empire empire)
	{
		base.Bind(empire);
		this.filteredList = new List<IWorldEntityWithCulling>();
	}

	public override void Unbind()
	{
		if (this.filteredList != null)
		{
			this.filteredList.Clear();
			this.filteredList = null;
		}
		base.Unbind();
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		yield break;
	}

	protected override void OnUnload()
	{
		base.OnUnload();
	}

	protected override void OnUnloadGame(IGame game)
	{
		if (this.filteredList != null)
		{
			this.filteredList.Clear();
		}
		base.OnUnloadGame(game);
	}

	protected override void RefreshWorldEntityBinding(AgeTransform tableitem, IWorldEntityWithCulling worldEntity, int index)
	{
		WorldKaiju worldKaiju = worldEntity as WorldKaiju;
		KaijuGarrisonLabel component = tableitem.GetComponent<KaijuGarrisonLabel>();
		if (base.GuiService.GetGuiPanel<GameWorldScreen>().CurrentMetaPanel is MetaPanelCity)
		{
			if (component.KaijuGarrison != null)
			{
				component.Unbind();
			}
			tableitem.Visible = false;
			return;
		}
		if (base.VisibilityService != null && !base.VisibilityService.IsWorldPositionVisibleFor(worldKaiju.WorldPosition, base.Empire))
		{
			if (component.KaijuGarrison != null)
			{
				component.Unbind();
			}
			tableitem.Visible = false;
			return;
		}
		bool flag = base.Counter % 10 == 0;
		if (Services.GetService<IDownloadableContentService>().IsShared(DownloadableContent20.ReadOnlyName) && component.KaijuGarrison != worldKaiju.Kaiju.KaijuGarrison)
		{
			component.Bind(worldKaiju.Kaiju.KaijuGarrison, base.GuiService, base.Empire);
			tableitem.Visible = true;
			flag = true;
		}
		Vector3 anchorPosition = ((IWorldEntityWithInterfaceConstraintCulling)worldKaiju).AnchorPosition;
		Vector3 vector = this.cameraController.Camera.WorldToScreenPoint(anchorPosition + Vector3.up * 1.5f);
		Vector3 vector2 = this.cameraController.Camera.WorldToScreenPoint(anchorPosition);
		Vector2 vector3 = new Vector3(vector.x - component.LeftOffset, (float)Screen.height - vector.y - component.Height);
		component.AgeTransform.X = vector3.x;
		component.AgeTransform.Y = vector3.y;
		component.PinLine.AgeTransform.Height = vector.y - vector2.y;
		if (flag)
		{
			component.RefreshContent();
		}
	}

	protected override void RefreshLayerLabel()
	{
		if (!base.IsVisible)
		{
			return;
		}
		if (base.WorldEntityCullingService != null)
		{
			this.filteredList.Clear();
			ReadOnlyCollection<IWorldEntityWithCulling> readOnlyCollection;
			if (base.WorldEntityCullingService.TryGetVisibleEntities<WorldKaiju>(out readOnlyCollection))
			{
				for (int i = 0; i < readOnlyCollection.Count; i++)
				{
					WorldKaiju worldKaiju = readOnlyCollection[i] as WorldKaiju;
					if (worldKaiju != null && worldKaiju.Kaiju.KaijuGarrison != null)
					{
						this.filteredList.Add(worldKaiju);
					}
				}
				this.LabelsTable.ReserveChildren(this.filteredList.Count, this.LabelPrefab, "DeviceLabel");
				this.LabelsTable.RefreshChildrenIList<IWorldEntityWithCulling>(this.filteredList, this.refreshWorldEntityBinding, true, false);
				this.UnbindAndHideLabels(this.filteredList.Count);
			}
		}
		if (base.LabelLayer != null)
		{
			base.AgeTransform.Alpha = base.LabelLayer.Opacity;
		}
	}

	protected override void UnbindAndHideLabels(int startIndex = 0)
	{
		for (int i = startIndex; i < this.LabelsTable.GetChildren().Count; i++)
		{
			this.LabelsTable.GetChildren()[i].GetComponent<KaijuGarrisonLabel>().Unbind();
			this.LabelsTable.GetChildren()[i].Visible = false;
		}
	}

	public override void Show(params object[] parameters)
	{
		if (!(base.GuiService.GetGuiPanel<GameWorldScreen>().CurrentMetaPanel is MetaPanelBattle))
		{
			base.Show(parameters);
		}
	}

	private List<IWorldEntityWithCulling> filteredList;
}
