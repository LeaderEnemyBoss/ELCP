using System;
using Amplitude;
using Amplitude.Path;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Xml;
using UnityEngine;

public class UnitGuiItem : MonoBehaviour
{
	public AgeTransform AgeTransform
	{
		get
		{
			AgeTransform component = base.GetComponent<AgeTransform>();
			if (component == null)
			{
				Diagnostics.Assert(component != null, "The UnitGuiItem does not contain a AgeTransform component");
			}
			return component;
		}
	}

	public GuiUnit GuiUnit { get; private set; }

	public UnitBodyDefinition UnitBody { get; private set; }

	public UnitDesign UnitDesign { get; private set; }

	public void RefreshContent(GuiUnit guiUnit, GameObject client, AgeTransform anchor)
	{
		this.Reset();
		if (guiUnit.UnitDesign == null)
		{
			this.DestroyedCrossImage.Alpha = 0f;
			this.LevelUpImage.Alpha = 0f;
			this.LastStandImage.Alpha = 0f;
			this.UnitName.Text = "Unknown";
			if (this.tooltip != null && guiUnit.UnitSnapshot != null)
			{
				this.tooltip.Class = "Simple";
				this.tooltip.Content = string.Format("Unknown Unit Design: {0}, {1}, {2} (in Empire {3})", new object[]
				{
					guiUnit.UnitSnapshot.UnitDesignDefinitionName,
					guiUnit.UnitSnapshot.UnitDesignModel,
					guiUnit.UnitSnapshot.UnitDesignModelRevision,
					guiUnit.Empire.Index
				});
			}
			return;
		}
		this.GuiUnit = guiUnit;
		if (this.tooltip != null)
		{
			this.tooltip.Anchor = anchor;
			this.tooltip.Class = "Unit";
			this.tooltip.ClientData = this.GuiUnit;
			this.tooltip.Content = this.GuiUnit.UnitDesign.UnitBodyDefinitionReference;
		}
		this.UnitName.Text = GuiUnitDesign.GetTruncatedTitle(this.GuiUnit.UnitDesign, this.UnitName);
		this.UnitPortrait.Image = this.GuiUnit.GetPortraitTexture(global::GuiPanel.IconSize.Small);
		this.UnitLevel.Text = this.GuiUnit.LevelDisplayed.ToString();
		IGuiPanelHelper guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
		Diagnostics.Assert(guiPanelHelper != null, "Unable to access GuiPanelHelper");
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		GuiElement guiElement;
		Texture2D image;
		if (service != null && service.IsShared(DownloadableContent13.ReadOnlyName) && this.GuiUnit.IsShifted && guiPanelHelper.TryGetGuiElement(this.GuiUnit.UnitDesign.Name, out guiElement) && guiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.ShiftedSmall, out image))
		{
			this.UnitPortrait.Image = image;
		}
		this.UnitToggle.State = false;
		if (client != null)
		{
			this.UnitToggle.OnSwitchMethod = "OnUnitToggle";
			this.UnitToggle.OnMiddleClickMethod = "OnUnitToggle";
			this.UnitToggle.OnRightClickMethod = "OnUnitToggle";
			this.UnitToggle.OnSwitchObject = client;
			this.UnitToggle.AgeTransform.Enable = true;
		}
		else
		{
			this.UnitToggle.OnSwitchMethod = string.Empty;
			this.UnitToggle.OnSwitchObject = null;
			this.UnitToggle.AgeTransform.Enable = false;
		}
		if (this.PrivateerGroup != null)
		{
			this.PrivateerGroup.Visible = this.IsMercenary(this.GuiUnit);
		}
		this.LifeGauge.AgeTransform.PercentTop = 100f * (1f - this.GuiUnit.HealthRatio);
		this.LifeGauge.TintColor = this.GuiUnit.HealthColor;
		this.LifeGauge.AgeTransform.Alpha = 1f;
		this.DestroyedCrossImage.Alpha = ((this.GuiUnit.Health <= 0f) ? 1f : 0f);
		this.LevelUpImage.Alpha = ((!this.GuiUnit.HasJustLeveledUp) ? 0f : 1f);
		this.LastStandImage.Alpha = ((!this.GuiUnit.HasBeenLastStanding) ? 0f : 1f);
		this.UndeadImage.Alpha = ((!this.GuiUnit.HasRisenFromTheDead) ? 0f : 1f);
		if (this.EmbarkedGroup != null && (guiUnit.Unit != null || guiUnit.UnitSnapshot != null))
		{
			this.EmbarkedGroup.Visible = ((guiUnit.Unit != null && guiUnit.Unit.Embarked) || (guiUnit.UnitSnapshot != null && guiUnit.UnitSnapshot.Embarked));
		}
	}

	public void RefreshContent(global::Empire empire, UnitDesign design, GameObject client, AgeTransform anchor, bool allowDoubleClick)
	{
		this.Reset();
		IGuiPanelHelper guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
		Diagnostics.Assert(guiPanelHelper != null, "Unable to access GuiPanelHelper");
		this.UnitDesign = design;
		if (this.tooltip != null)
		{
			this.tooltip.Anchor = anchor;
			this.tooltip.Class = this.UnitDesign.TooltipClass;
			this.tooltip.ClientData = new ConstructibleTooltipData(empire, null, this.UnitDesign);
			this.tooltip.Content = this.UnitDesign.Name;
		}
		this.UnitName.Text = GuiUnitDesign.GetTruncatedTitle(this.UnitDesign, this.UnitName);
		if (this.PrivateerGroup != null)
		{
			this.PrivateerGroup.Visible = this.IsMercenary(empire, this.UnitDesign);
		}
		GuiElement guiElement;
		if (guiPanelHelper.TryGetGuiElement(this.UnitDesign.UnitBodyDefinition.Name, out guiElement))
		{
			Texture2D image;
			if (guiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Small, out image))
			{
				this.UnitPortrait.Image = image;
			}
		}
		else
		{
			this.UnitPortrait.Image = null;
		}
		this.UnitToggle.State = false;
		this.UnitToggle.OnSwitchMethod = "OnUnitDesignToggle";
		this.UnitToggle.OnSwitchObject = client;
		if (this.DoubleClickButton != null)
		{
			if (allowDoubleClick)
			{
				this.DoubleClickButton.AgeTransform.Visible = true;
				this.DoubleClickButton.OnDoubleClickMethod = "OnDoubleClickDesignCB";
				this.DoubleClickButton.OnDoubleClickObject = client;
			}
			else
			{
				this.DoubleClickButton.AgeTransform.Visible = false;
			}
		}
		this.LifeGauge.AgeTransform.Alpha = 0f;
	}

	public void RefreshContent(global::Empire empire, UnitBodyDefinition unitBody, GameObject client, AgeTransform anchor)
	{
		this.Reset();
		IGuiPanelHelper guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
		Diagnostics.Assert(guiPanelHelper != null, "Unable to access GuiPanelHelper");
		this.UnitBody = unitBody;
		this.GenerateUnit(empire);
		if (this.tooltip != null)
		{
			this.tooltip.Anchor = anchor;
			this.tooltip.Class = this.UnitBody.TooltipClass;
			this.tooltip.ClientData = this.temporaryUnit;
			this.tooltip.Content = this.UnitBody.Name;
		}
		GuiElement guiElement;
		if (guiPanelHelper.TryGetGuiElement(this.UnitBody.Name, out guiElement))
		{
			AgeUtils.TruncateString(AgeLocalizer.Instance.LocalizeString(guiElement.Title), this.UnitName, out this.temp, '.');
			this.UnitName.Text = this.temp;
			Texture2D image;
			if (guiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Small, out image))
			{
				this.UnitPortrait.Image = image;
			}
		}
		else
		{
			this.UnitName.Text = string.Empty;
			this.UnitPortrait.Image = null;
		}
		if (this.PrivateerGroup != null)
		{
			this.PrivateerGroup.Visible = this.IsMercenary(empire, this.GuiUnit);
		}
		this.UnitToggle.State = false;
		this.UnitToggle.OnSwitchMethod = "OnUnitBodyToggle";
		this.UnitToggle.OnSwitchObject = client;
		this.LifeGauge.AgeTransform.Alpha = 0f;
	}

	protected virtual void OnDestroy()
	{
		this.Reset();
		this.GuiUnit = null;
		this.UnitDesign = null;
		this.UnitBody = null;
		this.empire = null;
		AgeTooltip component = base.GetComponent<AgeTooltip>();
		if (component != null)
		{
			component.ClientData = null;
		}
	}

	private bool IsMercenary(GuiUnit guiUnit)
	{
		return guiUnit != null && guiUnit.Empire != null && this.IsMercenary(guiUnit.Empire, guiUnit);
	}

	private bool IsMercenary(global::Empire onlooker, GuiUnit guiUnit)
	{
		if (guiUnit == null)
		{
			return false;
		}
		Army army = guiUnit.TryGetUnitGarrison() as Army;
		return (army == null || !army.HasCatspaw) && this.IsMercenary(onlooker, guiUnit.UnitDesign);
	}

	private bool IsMercenary(global::Empire designEmpire, UnitDesign unitDesign)
	{
		return designEmpire != null && this.playerControllerRepositoryService != null && this.playerControllerRepositoryService.ActivePlayerController != null && this.playerControllerRepositoryService.ActivePlayerController.Empire.Index == designEmpire.Index && unitDesign.Tags.Contains(TradableUnit.ReadOnlyMercenary);
	}

	private void GenerateUnit(global::Empire empire)
	{
		if (this.temporaryUnit != null)
		{
			this.Reset();
		}
		this.empire = empire;
		UnitDesign unitDesign = new UnitDesign();
		unitDesign.UnitBodyDefinitionReference = new XmlNamedReference(this.UnitBody.Name);
		DepartmentOfDefense agency = empire.GetAgency<DepartmentOfDefense>();
		if (!UnitGuiItem.temporaryUnitGuid.IsValid)
		{
			IGameService service = Services.GetService<IGameService>();
			IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
			UnitGuiItem.temporaryUnitGuid = service2.GenerateGUID();
		}
		this.temporaryUnit = agency.CreateTemporaryUnit(UnitGuiItem.temporaryUnitGuid, unitDesign);
		this.temporaryUnit.Rename(this.temporaryUnit.Name + "#" + this.UnitBody.Name);
		this.temporaryUnit.SimulationObject.ModifierForward = ModifierForwardType.ChildrenOnly;
		this.empire.AddChild(this.temporaryUnit);
	}

	private void Reset()
	{
		if (this.temporaryUnit != null)
		{
			if (this.empire.SimulationObject != null)
			{
				this.empire.RemoveChild(this.temporaryUnit);
			}
			this.temporaryUnit.Dispose();
			this.temporaryUnit = null;
		}
		if (this.tooltip == null)
		{
			this.tooltip = base.GetComponent<AgeTooltip>();
		}
		if (this.playerControllerRepositoryService == null)
		{
			IGameService service = Services.GetService<IGameService>();
			if (service != null && service.Game != null && service.Game is global::Game)
			{
				this.playerControllerRepositoryService = (service.Game as global::Game).GetService<IPlayerControllerRepositoryService>();
			}
		}
	}

	public AgePrimitiveImage UnitPortrait;

	public AgePrimitiveLabel UnitName;

	public AgeControlToggle UnitToggle;

	public AgeControlButton DoubleClickButton;

	public AgePrimitiveImage LifeGauge;

	public AgeTransform DestroyedCrossImage;

	public AgeTransform LevelUpImage;

	public AgeTransform LastStandImage;

	public AgeTransform UndeadImage;

	public AgePrimitiveLabel UnitLevel;

	public AgeTransform PrivateerGroup;

	public AgeTransform EmbarkedGroup;

	private static GameEntityGUID temporaryUnitGuid;

	private string temp = string.Empty;

	private IPlayerControllerRepositoryService playerControllerRepositoryService;

	private Unit temporaryUnit;

	private global::Empire empire;

	private AgeTooltip tooltip;
}
