using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Gui.SimulationEffect;
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
			GuiElement guiElement2;
			if (this.context != null && guiEntity != null && guiEntity.Gui != null)
			{
				this.Description.Text = guiEntity.Gui.Description;
			}
			else if (base.GuiService.GuiPanelHelper.TryGetGuiElement(this.content, out guiElement2))
			{
				this.Description.Text = guiElement2.Description;
			}
			else
			{
				this.Description.Text = this.content;
			}
		}
		if (this.context is CreepingNode || this.context is PointOfInterest || this.context is Village)
		{
			WorldPosition worldPosition;
			if (this.context is PointOfInterest)
			{
				worldPosition = (this.context as PointOfInterest).WorldPosition;
			}
			else if (this.context is CreepingNode)
			{
				worldPosition = (this.context as CreepingNode).WorldPosition;
			}
			else
			{
				worldPosition = (this.context as Village).WorldPosition;
			}
			Region region = Services.GetService<IGameService>().Game.Services.GetService<IWorldPositionningService>().GetRegion(worldPosition);
			if (region.IsLand && !region.IsWasteland && region.MinorEmpire != null)
			{
				BarbarianCouncil agency = region.MinorEmpire.GetAgency<BarbarianCouncil>();
				if (agency != null && agency.GetVillageAt(worldPosition) != null)
				{
					global::IGuiService service = Services.GetService<global::IGuiService>();
					List<EffectDescription> list = new List<EffectDescription>();
					MinorFaction minorFaction = region.MinorEmpire.MinorFaction;
					if (minorFaction != null)
					{
						service.GuiSimulationParser.ParseSimulationDescriptor(minorFaction, list, null, false, false);
						this.Description.Text = AgeLocalizer.Instance.LocalizeString(this.Description.Text);
						foreach (EffectDescription effectDescription in list)
						{
							if (effectDescription == list[0])
							{
								AgePrimitiveLabel description = this.Description;
								description.Text = description.Text + "\n \n#FFB43F#" + AgeLocalizer.Instance.LocalizeString("%EffectsOnEmpireTitle") + "#REVERT#";
							}
							AgePrimitiveLabel description2 = this.Description;
							description2.Text = description2.Text + "\n" + effectDescription.ToString();
						}
					}
				}
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
