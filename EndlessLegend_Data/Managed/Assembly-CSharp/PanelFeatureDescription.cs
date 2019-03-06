using System;
using System.Collections;
using System.Xml;
using Amplitude;
using Amplitude.Unity.Gui;
using UnityEngine;

public class PanelFeatureDescription : GuiPanelFeature
{
	public override StaticString InternalName
	{
		get
		{
			return "Description";
		}
		protected set
		{
		}
	}

	public float DefaultWidth { get; set; }

	protected override void Awake()
	{
		base.Awake();
		this.DefaultWidth = base.AgeTransform.Width;
	}

	protected override void DeserializeFeatureDescription(XmlElement featureDescription)
	{
		base.DeserializeFeatureDescription(featureDescription);
		if (featureDescription.Name == "TechnologyOnly")
		{
			bool.TryParse(featureDescription.GetAttribute("Value"), out this.technologyOnly);
		}
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		this.Description.AgeTransform.Height = 0f;
		this.Description.Text = string.Empty;
		if (this.content.StartsWith("%"))
		{
			this.Description.Text = this.content;
		}
		else if (this.technologyOnly && this.context is MultipleConstructibleTooltipData)
		{
			GuiElement guiElement;
			if (base.GuiService.GuiPanelHelper.TryGetGuiElement((this.context as MultipleConstructibleTooltipData).TechnologyDefinition.Name, out guiElement))
			{
				this.Description.Text = guiElement.Description;
			}
		}
		else if (this.context is IDescriptionFeatureProvider)
		{
			this.Description.Text = (this.context as IDescriptionFeatureProvider).Description;
		}
		else
		{
			IGuiEntity guiEntity = this.context as IGuiEntity;
			GuiElement guiElement;
			if (this.context != null && guiEntity != null && guiEntity.Gui != null)
			{
				this.Description.Text = guiEntity.Gui.Description;
			}
			else if (base.GuiService.GuiPanelHelper.TryGetGuiElement(this.content, out guiElement))
			{
				this.Description.Text = guiElement.Description;
			}
			else
			{
				this.Description.Text = this.content;
			}
		}
		if (!string.IsNullOrEmpty(this.Description.Text))
		{
			this.Description.AgeTransform.Width = this.DefaultWidth - this.Description.AgeTransform.PixelMarginLeft - this.Description.AgeTransform.PixelMarginRight;
			this.Description.ComputeText();
			base.AgeTransform.Height = this.Description.Font.LineHeight * (float)this.Description.TextLines.Count + this.Description.AgeTransform.PixelMarginTop + this.Description.AgeTransform.PixelMarginBottom;
			if (this.Description.TextLines.Count == 1)
			{
				base.AgeTransform.Width = this.Description.AgeTransform.PixelMarginLeft + this.Description.Font.ComputeTextWidth(this.Description.TextLines[0], false, true) + this.Description.AgeTransform.PixelMarginRight;
			}
			else
			{
				base.AgeTransform.Width = this.DefaultWidth;
			}
		}
		yield return base.OnShow(parameters);
		yield break;
	}

	private void OnApplyHighDefinition(float scale)
	{
		this.DefaultWidth = Mathf.Round(this.DefaultWidth * scale);
	}

	public AgePrimitiveLabel Description;

	private bool technologyOnly;
}
