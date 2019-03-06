using System;
using System.Collections;
using Amplitude.Unity.Gui;
using UnityEngine;

public class NotificationPanelHeroUpdate : NotificationPanelBase
{
	protected override void DefineAutoPopupStatus()
	{
		base.DefineAutoPopupStatus();
		if (this.guiNotification is GuiNotificationHeroAvailable)
		{
			this.autoPopupToggle.State = this.guiNotificationSettingsService.AutoPopupNotificationHeroAvailable;
		}
		else if (this.guiNotification is GuiNotificationHeroExfiltrated)
		{
			this.autoPopupToggle.State = this.guiNotificationSettingsService.AutoPopupNotificationHeroExfiltrated;
		}
		else if (this.guiNotification is GuiNotificationHeroInfiltrated)
		{
			this.autoPopupToggle.State = this.guiNotificationSettingsService.AutoPopupNotificationHeroInfiltrated;
		}
		else if (this.guiNotification is GuiNotificationHeroInjured)
		{
			this.autoPopupToggle.State = this.guiNotificationSettingsService.AutoPopupNotificationHeroInjured;
		}
		else if (this.guiNotification is GuiNotificationPrisonerCaptured)
		{
			this.autoPopupToggle.State = this.guiNotificationSettingsService.AutoPopupNotificationPrisonerCaptured;
		}
		else if (this.guiNotification is GuiNotificationPrisonerReleased)
		{
			this.autoPopupToggle.State = this.guiNotificationSettingsService.AutoPopupNotificationPrisonerReleased;
		}
		else if (this.guiNotification is GuiNotificationHeroLevelUp)
		{
			this.autoPopupToggle.State = this.guiNotificationSettingsService.AutoPopupNotificationHeroLevelUp;
		}
		else if (this.guiNotification is GuiNotificationHeroRecovered)
		{
			this.autoPopupToggle.State = this.guiNotificationSettingsService.AutoPopupNotificationHeroRecovered;
		}
		else if (this.guiNotification is GuiNotificationHeroUnassigned)
		{
			this.autoPopupToggle.State = this.guiNotificationSettingsService.AutoPopupNotificationHeroUnassigned;
		}
	}

	protected override void OnSwitchAutoPopupCB(GameObject obj)
	{
		if (this.guiNotification is GuiNotificationHeroAvailable)
		{
			this.guiNotificationSettingsService.AutoPopupNotificationHeroAvailable = this.autoPopupToggle.State;
		}
		else if (this.guiNotification is GuiNotificationHeroExfiltrated)
		{
			this.guiNotificationSettingsService.AutoPopupNotificationHeroExfiltrated = this.autoPopupToggle.State;
		}
		else if (this.guiNotification is GuiNotificationHeroInfiltrated)
		{
			this.guiNotificationSettingsService.AutoPopupNotificationHeroInfiltrated = this.autoPopupToggle.State;
		}
		else if (this.guiNotification is GuiNotificationHeroInjured)
		{
			this.guiNotificationSettingsService.AutoPopupNotificationHeroInjured = this.autoPopupToggle.State;
		}
		else if (this.guiNotification is GuiNotificationPrisonerCaptured)
		{
			this.guiNotificationSettingsService.AutoPopupNotificationPrisonerCaptured = this.autoPopupToggle.State;
		}
		else if (this.guiNotification is GuiNotificationPrisonerReleased)
		{
			this.guiNotificationSettingsService.AutoPopupNotificationPrisonerReleased = this.autoPopupToggle.State;
		}
		else if (this.guiNotification is GuiNotificationHeroLevelUp)
		{
			this.guiNotificationSettingsService.AutoPopupNotificationHeroLevelUp = this.autoPopupToggle.State;
		}
		else if (this.guiNotification is GuiNotificationHeroRecovered)
		{
			this.guiNotificationSettingsService.AutoPopupNotificationHeroRecovered = this.autoPopupToggle.State;
		}
		else if (this.guiNotification is GuiNotificationHeroUnassigned)
		{
			this.guiNotificationSettingsService.AutoPopupNotificationHeroUnassigned = this.autoPopupToggle.State;
		}
		base.DefineAutoPopupStatus();
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		if (this.guiNotification != null && this.notificationItem != null)
		{
			base.SetHeader();
			GuiNotificationHeroBase guiNotificationHeroBase = this.guiNotification as GuiNotificationHeroBase;
			if (guiNotificationHeroBase != null)
			{
				GuiElement guiElement;
				if (base.GuiService.GuiPanelHelper.TryGetGuiElement(guiNotificationHeroBase.RaisedEvent.EventName, out guiElement))
				{
					this.Description.AgeTransform.Height = this.DescriptionSW.Viewport.Height;
					this.Description.Text = guiNotificationHeroBase.FormatDescription(guiElement.Description, base.GuiService);
					this.DescriptionSW.ResetUp();
				}
				Unit hero = this.FetchHero();
				if (hero != null)
				{
					GuiHero guiHero = new GuiHero(hero, null);
					if (base.GuiService.GuiPanelHelper.TryGetGuiElement(hero.UnitDesign.Name, out guiElement))
					{
						Texture2D texture;
						if (guiHero != null && guiHero.IsShifted)
						{
							if (base.GuiService.GuiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.ShiftedLarge, out texture))
							{
								this.ImmersiveImage.Image = texture;
							}
						}
						else if (base.GuiService.GuiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Large, out texture))
						{
							this.ImmersiveImage.Image = texture;
						}
					}
					AgeTooltip ageTooltip = this.ImmersiveImage.AgeTransform.AgeTooltip;
					if (guiHero != null)
					{
						ageTooltip.Class = "Unit";
						ageTooltip.ClientData = guiHero;
						ageTooltip.Content = guiHero.Title;
					}
				}
			}
		}
		yield break;
	}

	private void OnInspectCB(GameObject obj)
	{
		if (this.guiNotification is GuiNotificationHeroLevelUp)
		{
			Amplitude.Unity.Gui.GuiPanel guiPanel = base.GuiService.GetGuiPanel<HeroInspectionModalPanel>();
			object[] array = new object[2];
			array[0] = this.FetchHero();
			guiPanel.Show(array);
		}
		else
		{
			base.GuiService.GetGuiPanel<HeroInspectionModalPanel>().Show(new object[]
			{
				this.FetchHero()
			});
		}
	}

	private void OnAcademyCB(GameObject obj)
	{
		base.GuiService.GetGuiPanel<GameAcademyScreen>().Show(new object[]
		{
			this.FetchHero()
		});
		this.Hide(false);
	}

	private Unit FetchHero()
	{
		Unit result = null;
		GuiNotificationHeroBase guiNotificationHeroBase = this.guiNotification as GuiNotificationHeroBase;
		if (guiNotificationHeroBase != null)
		{
			IGameEntityRepositoryService service = base.Game.Services.GetService<IGameEntityRepositoryService>();
			IGameEntity gameEntity = null;
			if (service.TryGetValue(guiNotificationHeroBase.HeroGuid, out gameEntity))
			{
				result = (gameEntity as Unit);
			}
		}
		return result;
	}

	public AgeControlScrollView DescriptionSW;

	public AgePrimitiveLabel Description;
}
