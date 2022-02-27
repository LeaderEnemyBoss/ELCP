using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using UnityEngine;

public class NotificationPanelCreepingNodeConstructionCompleted : NotificationPanelBase
{
	public override void RefreshContent()
	{
		base.RefreshContent();
		if (this.guiNotification != null && this.notificationItem != null)
		{
			base.SetHeader();
			GuiNotificationCreepingNodeConstructionCompleted guiNotificationCreepingNodeConstructionCompleted = this.guiNotification as GuiNotificationCreepingNodeConstructionCompleted;
			string text = string.Empty;
			GuiElement guiElement;
			if (base.GuiService.GuiPanelHelper.TryGetGuiElement(this.guiNotification.GetGuiElementName(), out guiElement))
			{
				text = AgeLocalizer.Instance.LocalizeString(guiElement.Description);
			}
			this.DescriptionLabel.Text = text;
			this.ItemTable.Height = 0f;
			List<CreepingNode> list = new List<CreepingNode>();
			foreach (CreepingNode creepingNode in guiNotificationCreepingNodeConstructionCompleted.CreepingNodeList)
			{
				if (creepingNode != null && creepingNode.PointOfInterest != null && creepingNode.PointOfInterest.CreepingNodeImprovement != null)
				{
					list.Add(creepingNode);
				}
			}
			this.ItemTable.ReserveChildren(list.Count, this.CreepingNodeConstructionCompletedLinePrefab, "Item");
			this.ItemTable.RefreshChildrenIList<CreepingNode>(list, this.refreshTableItemDelegate, true, false);
			this.ItemTable.ArrangeChildren();
			this.ContentScrollView.ResetUp();
		}
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		yield break;
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		this.refreshTableItemDelegate = new AgeTransform.RefreshTableItem<CreepingNode>(this.RefreshCreepingNode);
		yield break;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		this.RefreshContent();
		yield break;
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.refreshTableItemDelegate = null;
		base.OnUnloadGame(game);
	}

	protected override void DefineAutoPopupStatus()
	{
		base.DefineAutoPopupStatus();
		this.autoPopupToggle.State = this.guiNotificationSettingsService.AutoPopupNotificationArmyHit;
	}

	protected override void OnSwitchAutoPopupCB(GameObject obj)
	{
		this.guiNotificationSettingsService.AutoPopupNotificationArmyHit = this.autoPopupToggle.State;
		base.DefineAutoPopupStatus();
	}

	private void OnShowLocation(WorldPosition worldPosition)
	{
		IViewService service = Services.GetService<IViewService>();
		if (service != null)
		{
			service.SelectAndCenter(worldPosition);
			this.Hide(false);
		}
	}

	private void RefreshCreepingNode(AgeTransform tableitem, CreepingNode creepingNode, int index)
	{
		CreepingNodeContructionCompletedLine component = tableitem.GetComponent<CreepingNodeContructionCompletedLine>();
		component.RefreshContent(creepingNode, base.gameObject);
	}

	public Transform CreepingNodeConstructionCompletedLinePrefab;

	public AgeControlScrollView ContentScrollView;

	public AgeTransform ItemTable;

	public AgePrimitiveLabel DescriptionLabel;

	private AgeTransform.RefreshTableItem<CreepingNode> refreshTableItemDelegate;
}
