using System;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Gui;
using UnityEngine;

public class DlcDescription : MonoBehaviour
{
	public void SetupContent(DownloadableContent downloadableContent, GameObject client, bool activated)
	{
		this.ActivationToggle.OnSwitchObject = client;
		this.ActivationToggle.OnSwitchData = downloadableContent.Name.ToString();
		this.StoreButton.OnActivateObject = client;
		this.StoreButton.OnActivateData = downloadableContent.Name.ToString();
		this.Background.Image = AgeManager.Instance.FindDynamicTexture("Gui/DynamicBitmaps/Notifications/NotificationBackground", false);
		IGuiPanelHelper guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
		GuiElement guiElement = null;
		if (guiPanelHelper != null)
		{
			if (guiPanelHelper.TryGetGuiElement(downloadableContent.Name, out guiElement))
			{
				this.Title.Text = guiElement.Title;
				this.Description.Text = guiElement.Description;
				this.Type.Text = "%DownloadableContentType" + downloadableContent.Type.ToString() + "Title";
				this.Type.AgeTransform.AgeTooltip.Content = "%DownloadableContentType" + downloadableContent.Type.ToString() + "Description";
				Texture2D image;
				if (guiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Large, out image))
				{
					this.LargeImage.Image = image;
				}
				if (guiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Small, out image))
				{
					this.SmallImageBackground.Visible = true;
					this.SmallImage.Image = image;
				}
				else
				{
					this.SmallImageBackground.Visible = false;
					this.SmallImage.Image = null;
				}
			}
			else
			{
				this.Title.Text = downloadableContent.Name;
				this.Description.Text = downloadableContent.Description;
				this.Type.Text = downloadableContent.Type.ToString();
				this.LargeImage.Image = null;
				this.SmallImage.Image = null;
			}
		}
		this.RefreshDownloadableContentActivation(downloadableContent, activated);
	}

	private void RefreshDownloadableContentActivation(DownloadableContent downloadableContent, bool activated)
	{
		if (downloadableContent.Type == DownloadableContentType.Addon)
		{
			this.ActivationToggle.AgeTransform.Visible = false;
			this.StoreButton.AgeTransform.Visible = false;
			this.Background.TintColor = Color.white;
			return;
		}
		if ((downloadableContent.Accessibility & DownloadableContentAccessibility.Subscribed) == DownloadableContentAccessibility.Subscribed)
		{
			this.ActivationToggle.AgeTransform.Visible = true;
			this.StoreButton.AgeTransform.Visible = false;
			this.Background.TintColor = Color.white;
			this.ActivationToggle.State = activated;
			this.ActivationToggle.AgeTransform.Enable = downloadableContent.IsDynamicActivationEnabled;
			return;
		}
		this.StoreButton.AgeTransform.Visible = true;
		this.ActivationToggle.AgeTransform.Visible = false;
		this.Background.TintColor = Color.grey;
	}

	public AgeTransform AgeTransform;

	public AgePrimitiveImage Background;

	public AgePrimitiveImage LargeImage;

	public AgeTransform SmallImageBackground;

	public AgePrimitiveImage SmallImage;

	public AgePrimitiveLabel Title;

	public AgePrimitiveLabel Type;

	public AgePrimitiveLabel Description;

	public AgeControlToggle ActivationToggle;

	public AgeControlButton StoreButton;
}
