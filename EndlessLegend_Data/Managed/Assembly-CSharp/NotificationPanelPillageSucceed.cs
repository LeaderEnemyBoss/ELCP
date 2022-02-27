using System;
using System.Collections;
using UnityEngine;

public class NotificationPanelPillageSucceed : NotificationPanelPillageUpdate
{
	protected override void DefineAutoPopupStatus()
	{
		base.DefineAutoPopupStatus();
		this.autoPopupToggle.State = this.guiNotificationSettingsService.AutoPopupNotificationPillageSucceed;
	}

	protected override void OnSwitchAutoPopupCB(GameObject obj)
	{
		this.guiNotificationSettingsService.AutoPopupNotificationPillageSucceed = this.autoPopupToggle.State;
		base.DefineAutoPopupStatus();
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		if (this.guiNotification != null && this.notificationItem != null)
		{
			GuiNotificationPillageSucceed guiNotificationPillageSucceed = this.guiNotification as GuiNotificationPillageSucceed;
			if (guiNotificationPillageSucceed != null && guiNotificationPillageSucceed.RaisedEvent is EventPillageSucceed)
			{
				IDroppable[] loots = (guiNotificationPillageSucceed.RaisedEvent as EventPillageSucceed).Loots;
				this.sourceText = string.Empty;
				bool flag = true;
				foreach (IDroppable droppable in loots)
				{
					if ((droppable != null && !(droppable is DroppableResource)) || (droppable as DroppableResource).Quantity > 0)
					{
						if (!flag)
						{
							this.sourceText += "\n";
						}
						this.sourceText += droppable.ToGuiString();
						flag = false;
					}
				}
				this.LootLabel.Text = this.sourceText;
			}
		}
		yield break;
	}

	public AgePrimitiveLabel LootLabel;

	private string sourceText;
}
