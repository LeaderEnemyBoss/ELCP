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
			if (guiNotificationPillageSucceed != null)
			{
				EventPillageSucceed eventPillageSucceed = guiNotificationPillageSucceed.RaisedEvent as EventPillageSucceed;
				if (eventPillageSucceed != null)
				{
					IDroppable[] droppables = (guiNotificationPillageSucceed.RaisedEvent as EventPillageSucceed).Loots;
					this.sourceText = string.Empty;
					for (int i = 0; i < droppables.Length; i++)
					{
						IDroppable droppable = droppables[i];
						if (droppable != null)
						{
							if (i != 0)
							{
								this.sourceText += "\n";
							}
							this.sourceText += droppable.ToGuiString();
						}
					}
					this.LootLabel.Text = this.sourceText;
				}
			}
		}
		yield break;
	}

	public AgePrimitiveLabel LootLabel;

	private string sourceText;
}
