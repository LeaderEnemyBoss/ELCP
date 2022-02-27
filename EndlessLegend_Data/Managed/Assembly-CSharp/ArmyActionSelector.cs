using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using UnityEngine;

public class ArmyActionSelector : MonoBehaviour
{
	public Army Army { get; private set; }

	public ArmyAction ArmyAction { get; private set; }

	public GameObject Client { get; private set; }

	public IWorldPositionable Target { get; private set; }

	public IEnumerable<Unit> UnitsSelection { get; private set; }

	public void Bind(Army army, ArmyAction armyAction, GameObject client, int index, IWorldPositionable target, IEnumerable<Unit> unitsSelection)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service == null || service.Game == null || this.Army != null || this.ArmyAction != null)
		{
			this.Unbind();
		}
		this.Army = army;
		this.ArmyAction = armyAction;
		this.Client = client;
		this.Target = target;
		this.UnitsSelection = unitsSelection;
		this.ActionButton.OnActivateObject = this.Client;
		IGuiPanelHelper guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
		Diagnostics.Assert(guiPanelHelper != null, "Unable to access GuiPanelHelper");
		GuiElement guiElement;
		if (guiPanelHelper.TryGetGuiElement(armyAction.Name, out guiElement))
		{
			this.ActionTitle.Text = guiElement.Title;
			this.ActionDescription.Text = guiElement.Description;
			Texture2D image;
			if (guiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Large, out image))
			{
				this.ActionImage.Image = image;
			}
		}
		this.AgeTransform.AgeTooltip.Anchor = this.AgeTransform;
		this.RefreshCosts();
		this.RefreshCanExecute();
	}

	public void RefreshCanExecute()
	{
		if (this.ArmyAction != null)
		{
			this.failure.Clear();
			bool flag = this.ArmyAction.CanExecute(this.Army, ref this.failure, new object[]
			{
				this.Target,
				this.UnitsSelection
			}) && !this.failure.Contains(ArmyAction.NoCanDoWhileMoving) && !this.failure.Contains(ArmyAction.NoCanDoWhileTutorial);
			if (flag)
			{
				ArmyAction_Bribe armyAction_Bribe = this.ArmyAction as ArmyAction_Bribe;
				if (armyAction_Bribe != null)
				{
					Village village = this.Target as Village;
					if (village != null)
					{
						DepartmentOfTheTreasury agency = this.Army.Empire.GetAgency<DepartmentOfTheTreasury>();
						ConstructionCost[] bribeCosts = armyAction_Bribe.GetBribeCosts(this.Army, village);
						flag = agency.CanAfford(bribeCosts);
					}
				}
			}
			this.AgeTransform.Enable = flag;
			if (this.failure.Count > 0)
			{
				this.AgeTransform.AgeTooltip.Content = "%" + this.failure[0] + "Description";
				if (this.ArmyAction is ArmyActionWithCooldown)
				{
					string text = AgeLocalizer.Instance.LocalizeString(this.AgeTransform.AgeTooltip.Content);
					if (!string.IsNullOrEmpty(text))
					{
						float num = (this.ArmyAction as ArmyActionWithCooldown).ComputeRemainingCooldownDuration(this.Army);
						this.AgeTransform.AgeTooltip.Content = text.Replace("$RemainingCooldownDuration", num.ToString());
					}
				}
			}
			else
			{
				this.AgeTransform.AgeTooltip.Content = string.Empty;
			}
		}
	}

	public void Unbind()
	{
		if (this.Client != null)
		{
			this.Client = null;
		}
		if (this.Army != null)
		{
			this.Army = null;
		}
		if (this.ArmyAction != null)
		{
			this.ArmyAction = null;
		}
	}

	private void OnDestroy()
	{
		this.Unbind();
	}

	private void RefreshCosts()
	{
		IConstructionCost[] array = this.ArmyAction.Costs;
		ArmyAction_Bribe armyAction_Bribe = this.ArmyAction as ArmyAction_Bribe;
		ArmyAction_Convert armyAction_Convert = this.ArmyAction as ArmyAction_Convert;
		if (armyAction_Bribe != null || armyAction_Convert != null)
		{
			Village village = this.Target as Village;
			if (village != null)
			{
				IConstructionCost[] array2 = null;
				if (armyAction_Bribe != null)
				{
					IConstructionCost[] array3 = armyAction_Bribe.GetBribeCosts(this.Army, village);
					array2 = array3;
				}
				else if (armyAction_Convert != null)
				{
					IConstructionCost[] array3 = armyAction_Convert.GetConvertionCost(this.Army, village);
					array2 = array3;
				}
				if (array2 != null && array2.Length != 0)
				{
					if (array == null)
					{
						array = array2;
					}
					else
					{
						Array.Resize<IConstructionCost>(ref array, array.Length + array2.Length);
						Array.Copy(array2, 0, array, array.Length - array2.Length, array2.Length);
					}
				}
			}
		}
		if (this.Army != null && this.Army.Empire != null && array != null && array.Length != 0)
		{
			this.ActionCostLabel.AgeTransform.Visible = true;
			if (ELCPUtilities.UseELCPSymbiosisBuffs && this.ArmyAction is ArmyAction_TameUnstunnedKaiju)
			{
				KaijuGarrison kaijuGarrison = this.Target as KaijuGarrison;
				if (kaijuGarrison != null)
				{
					KaijuCouncil agency = kaijuGarrison.KaijuEmpire.GetAgency<KaijuCouncil>();
					if (agency != null)
					{
						ConstructionCost constructionCost = new ConstructionCost(agency.ELCPResourceName, KaijuCouncil.GetKaijuTameCost().GetValue(this.Army.Empire), true, true);
						array = new IConstructionCost[]
						{
							constructionCost
						};
					}
				}
			}
			this.ActionCostLabel.Text = GuiFormater.FormatCost(this.Army.Empire, array, false, 1, this.Army);
			this.ActionDescription.AgeTransform.PixelMarginBottom = this.ActionCostLabel.AgeTransform.PixelMarginBottom + this.ActionCostLabel.AgeTransform.Height;
			return;
		}
		this.ActionCostLabel.AgeTransform.Visible = false;
		this.ActionCostLabel.Text = string.Empty;
		this.ActionDescription.AgeTransform.PixelMarginBottom = this.ActionCostLabel.AgeTransform.PixelMarginBottom;
	}

	public AgeTransform AgeTransform;

	public AgePrimitiveLabel ActionTitle;

	public AgePrimitiveImage ActionImage;

	public AgePrimitiveLabel ActionDescription;

	public AgePrimitiveLabel ActionCostLabel;

	public AgeControlButton ActionButton;

	private List<StaticString> failure = new List<StaticString>();
}
