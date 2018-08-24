using System;
using System.Collections;
using Amplitude.Unity.Gui;
using UnityEngine;

public class UnitDesignModalPanel : UnitInspectionModalPanel
{
	public override bool HandleCancelRequest()
	{
		this.forgePanel.CancelDrag();
		this.unitEquipmentPanel.CancelDrag();
		base.GuiService.GetGuiPanel<ImageDragPanel>().Hide(true);
		if (base.DesignAltered)
		{
			MessagePanel.Instance.Show("%UnitInspectionCancelConfirmDescription", "%UnitInspectionCancelConfirmTitle", MessagePanelButtons.YesNo, new MessagePanel.EventHandler(base.OnConfirmCancelRequest), MessagePanelType.WARNING, new MessagePanelButton[0]);
			return true;
		}
		return base.HandleCancelRequest();
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		if (this.unitInfoPanel.IsVisible)
		{
			this.unitInfoPanel.RefreshContent();
		}
		this.RefreshButtons();
	}

	public void OnChangeDesignName()
	{
		base.DesignAltered = true;
		this.RefreshButtons();
	}

	public override void Bind(Empire empire)
	{
		base.Bind(empire);
		this.unitInfoPanel.Bind(this, empire);
	}

	public override void Unbind()
	{
		this.unitInfoPanel.Unbind();
		base.Unbind();
	}

	public void SetNameValid(bool isNameValid)
	{
		this.isNameValid = isNameValid;
		this.RefreshButtons();
	}

	protected override void RefreshButtons()
	{
		if (base.CreateMode)
		{
			if (this.isNameValid)
			{
				this.CreateButton.Enable = this.interactionsAllowed;
				if (this.CreateButton.AgeTooltip != null)
				{
					this.CreateButton.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%UnitCreateDesignDescription");
				}
			}
			else
			{
				this.CreateButton.Enable = false;
				if (this.CreateButton.AgeTooltip != null)
				{
					this.CreateButton.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%UnitInvalidNameDescription");
				}
			}
		}
		else
		{
			this.ApplyButton.Enable = (this.isNameValid && base.DesignAltered && this.interactionsAllowed);
		}
		this.ResetButton.Enable = base.DesignAltered;
	}

	protected override IEnumerator OnHide(bool instant = false)
	{
		this.unitInfoPanel.Hide(false);
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		this.unitStatsPanel.DisplayCost = true;
		this.unitInfoPanel = Amplitude.Unity.Gui.GuiPanel.Instanciate<UnitInfoPanel>(this.DockingPanel, this.UnitInfoPanelPrefab, "2UnitInfoPanel");
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		this.unitInfoPanel.Show(new object[]
		{
			base.EditableUnitDesign
		});
		this.ApplyButton.Visible = !base.CreateMode;
		this.CreateButton.Visible = base.CreateMode;
		this.ResetButton.Visible = !base.CreateMode;
		base.DesignAltered = false;
		this.RefreshContent();
		if (base.CreateMode)
		{
			this.unitInfoPanel.UnitName.Text = string.Empty;
			this.unitInfoPanel.UnitNameTF.AgeTransform.Enable = true;
			AgeManager.Instance.FocusedControl = this.unitInfoPanel.UnitNameTF;
		}
		else
		{
			this.unitInfoPanel.UnitNameTF.AgeTransform.Enable = false;
		}
		yield break;
	}

	protected override void OnUnload()
	{
		if (this.unitInfoPanel != null)
		{
			this.unitInfoPanel.Unload();
			UnityEngine.Object.Destroy(this.unitInfoPanel);
			this.unitInfoPanel = null;
		}
		base.OnUnload();
	}

	protected override void OnResetCB(GameObject obj)
	{
		this.unitInfoPanel.Hide(false);
		base.OnResetCB(obj);
		this.unitInfoPanel.Show(new object[]
		{
			base.EditableUnitDesign
		});
		this.RefreshContent();
	}

	private void OnApplyCB(GameObject obj)
	{
		this.ApplyButton.Enable = false;
		this.CreateButton.Enable = false;
		OrderEditUnitDesign order = new OrderEditUnitDesign(base.PlayerController.Empire.Index, base.EditableUnitDesign);
		Ticket ticket;
		base.PlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnApplyOrderResponse));
	}

	private void OnApplyOrderResponse(object sender, TicketRaisedEventArgs args)
	{
		this.Hide(false);
		MetaPanelCity guiPanel = base.GuiService.GetGuiPanel<MetaPanelCity>();
		if (guiPanel != null)
		{
			guiPanel.RefreshContent();
		}
		MetaPanelArmy guiPanel2 = base.GuiService.GetGuiPanel<MetaPanelArmy>();
		if (guiPanel2 != null)
		{
			guiPanel2.RefreshContent();
		}
		MetaPanelVillage guiPanel3 = base.GuiService.GetGuiPanel<MetaPanelVillage>();
		if (guiPanel3 != null)
		{
			guiPanel3.RefreshContent();
		}
		MetaPanelCamp guiPanel4 = base.GuiService.GetGuiPanel<MetaPanelCamp>();
		if (guiPanel4 != null)
		{
			guiPanel4.RefreshContent();
		}
		MetaPanelFortress guiPanel5 = base.GuiService.GetGuiPanel<MetaPanelFortress>();
		if (guiPanel5 != null)
		{
			guiPanel5.RefreshContent();
		}
	}

	private void OnCreateCB(GameObject obj)
	{
		this.ApplyButton.Enable = false;
		this.CreateButton.Enable = false;
		OrderCreateUnitDesign order = new OrderCreateUnitDesign(base.PlayerController.Empire.Index, base.EditableUnitDesign);
		Ticket ticket;
		base.PlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnApplyOrderResponse));
	}

	public AgeTransform CreateButton;

	public Transform UnitInfoPanelPrefab;

	protected UnitInfoPanel unitInfoPanel;

	private bool isNameValid = true;
}
