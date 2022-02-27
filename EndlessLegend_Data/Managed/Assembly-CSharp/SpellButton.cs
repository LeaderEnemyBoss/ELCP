using System;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Gui;
using UnityEngine;

public class SpellButton : MonoBehaviour
{
	public AgeTransform AgeTransform
	{
		get
		{
			AgeTransform component = base.GetComponent<AgeTransform>();
			if (component == null)
			{
				Diagnostics.Assert(component != null, "The SpellButton does not contain a AgeTransform component");
			}
			return component;
		}
	}

	public SpellDefinition SpellDefinition { get; private set; }

	public Empire Empire { get; private set; }

	public void SetContent(SpellDefinition spellDefinition, Empire empire, GameObject client, bool isInTargetingPhase, bool thereAlreadyIsASpellCasted, Encounter encounter)
	{
		if (empire == null)
		{
			return;
		}
		this.SpellDefinition = spellDefinition;
		this.Empire = empire;
		this.client = client;
		this.ShowIcon();
		this.AgeTransform.AgeTooltip.Content = this.SpellDefinition.Name;
		this.AgeTransform.AgeTooltip.Class = "Spell";
		this.AgeTransform.AgeTooltip.ClientData = new SpellDefinitionTooltipData(this.Empire, this.Empire, this.SpellDefinition, encounter);
		bool enable;
		if (!isInTargetingPhase)
		{
			enable = false;
		}
		else if (thereAlreadyIsASpellCasted)
		{
			enable = false;
		}
		else
		{
			DepartmentOfTheTreasury agency = this.Empire.GetAgency<DepartmentOfTheTreasury>();
			ConstructionResourceStock[] array;
			enable = agency.GetInstantConstructionResourceCostForBuyout(empire, spellDefinition, out array);
		}
		this.AgeTransform.Enable = enable;
	}

	public void UnsetContent()
	{
		this.client = null;
		this.Empire = null;
		this.SpellDefinition = null;
	}

	public void OnCastSpellCB(GameObject obj = null)
	{
		if (this.client != null)
		{
			this.client.SendMessage("OnCastSpell", this.SpellDefinition);
		}
	}

	private void ShowIcon()
	{
		IGuiPanelHelper guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
		Diagnostics.Assert(guiPanelHelper != null, "Unable to access GuiPanelHelper");
		GuiElement guiElement;
		if (!guiPanelHelper.TryGetGuiElement(this.SpellDefinition.Name, out guiElement))
		{
			Diagnostics.LogWarning("Cannot find a GuiElement for the {0} GuiElement", new object[]
			{
				this.SpellDefinition.Name
			});
			return;
		}
		Texture2D image;
		if (guiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Small, out image))
		{
			this.IconImage.Image = image;
		}
	}

	public AgeControlButton CastButton;

	public AgePrimitiveImage IconImage;

	private GameObject client;
}
