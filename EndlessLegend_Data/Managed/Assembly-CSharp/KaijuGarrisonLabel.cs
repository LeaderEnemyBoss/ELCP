using System;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using UnityEngine;

public class KaijuGarrisonLabel : MonoBehaviour
{
	public KaijuGarrison KaijuGarrison { get; private set; }

	public KaijuGuiElement KaijuGuiElement
	{
		get
		{
			IGuiPanelHelper guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
			Diagnostics.Assert(guiPanelHelper != null, "Unable to access GuiPanelHelper");
			string x = string.Empty;
			if (this.KaijuGarrison.Kaiju != null && this.KaijuGarrison.Kaiju.KaijuEmpire != null && this.KaijuGarrison.Kaiju.KaijuEmpire.Faction != null)
			{
				x = this.KaijuGarrison.Kaiju.KaijuEmpire.Faction.Name + "Icon";
			}
			GuiElement guiElement;
			if (guiPanelHelper.TryGetGuiElement(x, out guiElement))
			{
				return guiElement as KaijuGuiElement;
			}
			return null;
		}
	}

	public GuiElement KaijuFactionGuiElement
	{
		get
		{
			IGuiPanelHelper guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
			Diagnostics.Assert(guiPanelHelper != null, "Unable to access GuiPanelHelper");
			string x = string.Empty;
			if (this.KaijuGarrison.Kaiju != null && this.KaijuGarrison.Kaiju.KaijuEmpire != null && this.KaijuGarrison.Kaiju.KaijuEmpire.Faction != null)
			{
				x = this.KaijuGarrison.Kaiju.KaijuEmpire.Faction.Name;
			}
			GuiElement result;
			if (guiPanelHelper.TryGetGuiElement(x, out result))
			{
				return result;
			}
			return null;
		}
	}

	public float Height
	{
		get
		{
			return this.AgeTransform.Height;
		}
	}

	public float Width
	{
		get
		{
			return this.AgeTransform.Width;
		}
	}

	public float LeftOffset
	{
		get
		{
			return this.PinLine.AgeTransform.X;
		}
	}

	public bool IsInScreen
	{
		get
		{
			return this.WantedPosition.x + this.Width >= 0f && this.WantedPosition.x <= (float)Screen.width && this.WantedPosition.y + this.AgeTransform.Height >= 0f && this.WantedPosition.y <= (float)Screen.height;
		}
	}

	public void Bind(KaijuGarrison garrison, Amplitude.Unity.Gui.IGuiService guiService)
	{
		this.Unbind();
		this.KaijuGarrison = garrison;
		this.GuiService = (guiService as global::IGuiService);
		IGameService service = Services.GetService<IGameService>();
		this.playerControllerRepository = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
		Unit unit2 = (from unit in this.KaijuGarrison.Units
		where unit.UnitDesign.Tags.Contains(Kaiju.MonsterUnitTag)
		select unit).First<Unit>();
		if (unit2 != null)
		{
			this.KaijuGuiUnit = new GuiUnit(unit2, null);
		}
		IGuiPanelHelper guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
		Diagnostics.Assert(guiPanelHelper != null, "Unable to access GuiPanelHelper");
		Texture2D image = null;
		if (guiPanelHelper.TryGetTextureFromIcon(this.KaijuGuiElement, global::GuiPanel.IconSize.Small, out image))
		{
			this.KaijuIcon.Image = image;
		}
	}

	public void Unbind()
	{
		this.KaijuGarrison = null;
		this.GuiService = null;
		this.playerControllerRepository = null;
	}

	public void RefreshContent()
	{
		if (this.KaijuGarrison == null)
		{
			return;
		}
		if (this.KaijuGarrison.Owner != null)
		{
			if (this.KaijuGarrison.Kaiju.OnArmyMode())
			{
				this.AgeTransform.Visible = false;
				return;
			}
			this.AgeTransform.Visible = true;
			this.FactionIcon.AgeTransform.Visible = true;
			this.StatusImage.AgeTransform.Visible = false;
			this.StatusTurns.AgeTransform.Visible = false;
			this.TameCost.AgeTransform.Visible = false;
			GuiEmpire guiEmpire = new GuiEmpire(this.KaijuGarrison.Owner);
			this.FactionIcon.Image = guiEmpire.GuiFaction.GetImageTexture(global::GuiPanel.IconSize.LogoSmall, false);
			this.FactionIcon.TintColor = guiEmpire.Color;
			this.Frame.TintColor = guiEmpire.Color;
			this.PinLine.TintColor = guiEmpire.Color;
			this.KaijuIcon.TintColor = guiEmpire.Color;
			this.StatusPanel.AgeTooltip.Content = string.Empty;
		}
		else
		{
			this.AgeTransform.Visible = true;
			this.FactionIcon.AgeTransform.Visible = false;
			this.StatusImage.AgeTransform.Visible = true;
			this.StatusTurns.AgeTransform.Visible = true;
			this.Frame.TintColor = this.KaijuGarrison.Empire.Color;
			this.PinLine.TintColor = this.KaijuGarrison.Empire.Color;
			this.KaijuIcon.TintColor = this.KaijuGarrison.Empire.Color;
			if (this.KaijuGarrison.Kaiju.IsWild())
			{
				KaijuCouncil agency = this.KaijuGarrison.KaijuEmpire.GetAgency<KaijuCouncil>();
				this.StatusTurns.Text = agency.RelocationETA.ToString();
				this.StatusImage.Image = AgeManager.Instance.FindDynamicTexture("Gui/DynamicBitmaps/Icons/kaijuMoveIcon", false);
				this.StatusPanel.AgeTooltip.Content = "%KaijuTurnsToMoveDescription";
			}
			else if (this.KaijuGarrison.Kaiju.IsStunned())
			{
				this.StatusTurns.Text = this.KaijuGarrison.Kaiju.GetRemainingTurnBeforeStunFinish().ToString();
				this.StatusImage.Image = AgeManager.Instance.FindDynamicTexture("Gui/DynamicBitmaps/Icons/kaijuStunnedIcon", false);
				this.StatusPanel.AgeTooltip.Content = "%KaijuStunnedDescription";
			}
			KaijuTameCost kaijuTameCost = KaijuCouncil.GetKaijuTameCost();
			this.TameCost.AgeTransform.Visible = true;
			this.TameCost.Text = GuiFormater.FormatGui(kaijuTameCost.GetValue(), false, true, false, 0) + this.GuiService.FormatSymbol(kaijuTameCost.ResourceName);
		}
		if (this.statusPanelTooltip != null)
		{
			this.statusPanelTooltip.Content = "%KaijuSpawnedProperties";
		}
		if (this.panelTooltip != null)
		{
			GuiElement kaijuFactionGuiElement = this.KaijuFactionGuiElement;
			if (kaijuFactionGuiElement != null)
			{
				this.panelTooltip.Content = kaijuFactionGuiElement.Description;
			}
		}
		this.UnitNumber.Text = this.KaijuGarrison.UnitsCount.ToString();
		string text = (this.KaijuGuiUnit == null) ? this.KaijuGarrison.Kaiju.Name.ToString() : GuiUnitDesign.GetTruncatedTitle(this.KaijuGuiUnit.UnitDesign, this.KaijuName);
		this.KaijuName.Text = text;
	}

	private void OnDestroy()
	{
		this.Unbind();
	}

	private void OnSelectGarrisonCB()
	{
		IViewService service = Services.GetService<IViewService>();
		if (service != null)
		{
			service.SelectAndCenter(this.KaijuGarrison, true);
		}
	}

	private void OnKaijuTechCB()
	{
		if (this.GuiService.GetGuiPanel<KaijuResearchModalPanel>().IsVisible)
		{
			this.GuiService.GetGuiPanel<GameWorldScreen>().Show(new object[0]);
		}
		else
		{
			this.GuiService.Hide(typeof(CurrentQuestPanel));
			this.GuiService.GetGuiPanel<KaijuResearchModalPanel>().Show(new object[0]);
		}
	}

	private void OnMouseEnterCB()
	{
		IMouseCursorService service = Services.GetService<IMouseCursorService>();
		Diagnostics.Assert(service != null);
		service.AddKey(base.GetType().ToString());
	}

	private void OnMouseLeaveCB()
	{
		IMouseCursorService service = Services.GetService<IMouseCursorService>();
		Diagnostics.Assert(service != null);
		service.RemoveKey(base.GetType().ToString());
	}

	private const string KaijuMoveIconPath = "Gui/DynamicBitmaps/Icons/kaijuMoveIcon";

	private const string KaijuStunnedIconPath = "Gui/DynamicBitmaps/Icons/kaijuStunnedIcon";

	public AgeTransform AgeTransform;

	public AgePrimitiveImage Frame;

	public AgeTransform StatusPanel;

	public AgePrimitiveImage StatusImage;

	public AgePrimitiveLabel StatusTurns;

	public AgePrimitiveLabel KaijuName;

	public AgePrimitiveImage FactionIcon;

	public AgePrimitiveLabel UnitNumber;

	public AgePrimitiveImage PinLine;

	public AgePrimitiveImage KaijuIcon;

	public AgeTooltip statusPanelTooltip;

	public AgeTooltip panelTooltip;

	public AgePrimitiveLabel TameCost;

	public Vector2 WantedPosition;

	private IPlayerControllerRepositoryService playerControllerRepository;

	private global::IGuiService GuiService;

	private GuiUnit KaijuGuiUnit;
}
