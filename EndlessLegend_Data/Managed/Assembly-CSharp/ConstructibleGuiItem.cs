using System;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Gui;
using UnityEngine;

public class ConstructibleGuiItem : MonoBehaviour
{
	public void RefreshContent(City city, CityOptionsPanel.ProductionConstruction constructibleElementDefinition, int index, GameObject client)
	{
		IGuiPanelHelper guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
		Diagnostics.Assert(guiPanelHelper != null, "Unable to access GuiPanelHelper");
		UnitDesign unitDesign = constructibleElementDefinition.ConstructibleElement as UnitDesign;
		GuiElement guiElement2;
		if (unitDesign != null)
		{
			this.ConstructibleName.Text = GuiUnitDesign.GetTruncatedTitle(unitDesign, this.ConstructibleName);
			this.ConstructibleImage.Image = null;
			GuiElement guiElement;
			Texture2D image;
			if (guiPanelHelper.TryGetGuiElement(unitDesign.UnitBodyDefinitionReference, out guiElement) && guiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Small, out image))
			{
				this.ConstructibleImage.Image = image;
			}
		}
		else if (guiPanelHelper.TryGetGuiElement(constructibleElementDefinition.ConstructibleElement.Name, out guiElement2))
		{
			AgeUtils.TruncateString(AgeLocalizer.Instance.LocalizeString(guiElement2.Title), this.ConstructibleName, out this.temp, '.');
			this.ConstructibleName.Text = this.temp;
			Texture2D image2;
			if (guiPanelHelper.TryGetTextureFromIcon(guiElement2, global::GuiPanel.IconSize.Small, out image2))
			{
				this.ConstructibleImage.Image = image2;
			}
			else
			{
				this.ConstructibleImage.Image = null;
			}
		}
		else
		{
			this.ConstructibleName.Text = string.Empty;
			this.ConstructibleImage.Image = null;
		}
		if (this.AgeTransform.AgeTooltip != null)
		{
			this.AgeTransform.AgeTooltip.Class = constructibleElementDefinition.ConstructibleElement.TooltipClass;
			this.AgeTransform.AgeTooltip.Content = constructibleElementDefinition.ConstructibleElement.Name;
			if (constructibleElementDefinition.ConstructibleElement is BoosterGeneratorDefinition)
			{
				this.AgeTransform.AgeTooltip.ClientData = new BoosterGeneratorTooltipData(city.Empire, city, constructibleElementDefinition.ConstructibleElement);
			}
			else
			{
				this.AgeTransform.AgeTooltip.ClientData = new ConstructibleTooltipData(city.Empire, city, constructibleElementDefinition.ConstructibleElement);
			}
		}
		this.ConstructibleButton.OnActivateMethod = "OnOptionSelect";
		this.ConstructibleButton.OnActivateObject = client;
		this.ConstructibleButton.OnActivateDataObject = constructibleElementDefinition.ConstructibleElement;
		this.AgeTransform.Enable = (constructibleElementDefinition.Flags.Length == 0);
	}

	protected void OnDestroy()
	{
		this.ConstructibleButton.OnActivateObject = null;
		if (this.AgeTransform.AgeTooltip != null)
		{
			this.AgeTransform.AgeTooltip.ClientData = null;
		}
	}

	public AgeTransform AgeTransform;

	public AgePrimitiveImage ConstructibleImage;

	public AgePrimitiveLabel ConstructibleName;

	public AgeControlButton ConstructibleButton;

	private string temp = string.Empty;
}
