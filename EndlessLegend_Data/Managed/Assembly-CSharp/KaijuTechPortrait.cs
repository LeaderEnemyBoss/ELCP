using System;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Gui;
using UnityEngine;

public class KaijuTechPortrait : MonoBehaviour
{
	public Kaiju Kaiju { get; private set; }

	public KaijuGuiElement KaijuGuiElement
	{
		get
		{
			IGuiPanelHelper guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
			Diagnostics.Assert(guiPanelHelper != null, "Unable to access GuiPanelHelper");
			string x = string.Empty;
			if (this.Kaiju != null && this.Kaiju.KaijuEmpire != null && this.Kaiju.KaijuEmpire.Faction != null)
			{
				x = this.Kaiju.KaijuEmpire.Faction.Name + "Icon";
			}
			GuiElement guiElement;
			if (guiPanelHelper.TryGetGuiElement(x, out guiElement))
			{
				return guiElement as KaijuGuiElement;
			}
			return null;
		}
	}

	public void SetupPortrait(Kaiju kaiju, Empire playerEmpire)
	{
		this.Kaiju = kaiju;
		if (this.Portrait != null)
		{
			IGuiPanelHelper guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
			Diagnostics.Assert(guiPanelHelper != null, "Unable to access GuiPanelHelper");
			Texture2D image;
			if (guiPanelHelper.TryGetTextureFromIcon(this.KaijuGuiElement, global::GuiPanel.IconSize.Large, out image))
			{
				this.Portrait.Image = image;
			}
		}
		Amplitude.Unity.Gui.IGuiService service = Services.GetService<global::IGuiService>();
		Diagnostics.Assert(service != null);
		KaijuTameCost kaijuTameCost = KaijuCouncil.GetKaijuTameCost();
		this.CostGroup.Visible = this.Kaiju.IsWild();
		this.CostValue.Text = GuiFormater.FormatGui(kaijuTameCost.GetValue(playerEmpire), false, true, false, 0) + service.FormatSymbol(kaijuTameCost.ResourceName);
		if (this.Background != null)
		{
			this.Background.TintColor = this.Kaiju.Empire.Color;
		}
		this.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString(KaijuTechPortrait.PortraitClickTooltip);
	}

	public void OnPortraitClickedCB(GameObject obj)
	{
		global::IGuiService service = Services.GetService<global::IGuiService>();
		KaijuResearchModalPanel guiPanel = service.GetGuiPanel<KaijuResearchModalPanel>();
		if (guiPanel != null && guiPanel.IsVisible)
		{
			guiPanel.Hide(false);
		}
		GameWorldScreen guiPanel2 = service.GetGuiPanel<GameWorldScreen>();
		if (guiPanel2 != null && !guiPanel2.IsVisible)
		{
			service.GetGuiPanel<GameWorldScreen>().Show(new object[0]);
		}
		IViewService service2 = Services.GetService<IViewService>();
		Diagnostics.Assert(service2 != null);
		IWorldViewCameraController worldViewCameraController = service2.CurrentView.CameraController as IWorldViewCameraController;
		Diagnostics.Assert(worldViewCameraController != null);
		worldViewCameraController.FocusCameraAt(this.Kaiju.WorldPosition, true, true);
	}

	private void OnDestroy()
	{
		if (this.AgeTransform.AgeTooltip != null)
		{
			this.AgeTransform.AgeTooltip.ClientData = null;
		}
		this.Kaiju = null;
	}

	public static string PortraitClickTooltip = "%KaijuPortraitClickTooltip";

	public AgeTransform AgeTransform;

	public AgePrimitiveImage Background;

	public AgePrimitiveImage Portrait;

	public AgeTransform CostGroup;

	public AgePrimitiveLabel CostValue;
}
