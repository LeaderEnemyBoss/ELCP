using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using UnityEngine;

public class BuyablePanel : GuiPlayerControllerPanel
{
	private IEndTurnService EndTurnService
	{
		get
		{
			return this.endTurnService;
		}
		set
		{
			if (this.endTurnService != null)
			{
				this.endTurnService.GameClientStateChange -= this.EndTurnService_GameClientStateChange;
			}
			this.endTurnService = value;
			if (this.endTurnService != null)
			{
				this.endTurnService.GameClientStateChange += this.EndTurnService_GameClientStateChange;
			}
		}
	}

	private DepartmentOfScience DepartmentOfScience
	{
		get
		{
			return this.departmentOfScience;
		}
		set
		{
			if (this.departmentOfScience != null)
			{
				this.departmentOfScience.TechnologyUnlocked -= this.DepartmentOfScience_TechnologyUnlocked;
			}
			this.departmentOfScience = value;
			if (this.departmentOfScience != null)
			{
				this.departmentOfScience.TechnologyUnlocked += this.DepartmentOfScience_TechnologyUnlocked;
			}
		}
	}

	private TradableLine SelectedTradableLine
	{
		get
		{
			return this.selectedTradableLine;
		}
		set
		{
			this.selectedTradableLine = value;
			if (this.selectedTradableLine != null)
			{
				this.CurrentQuantity = Mathf.Min((int)this.selectedTradableLine.Tradable.Quantity, this.previousQuantity);
				this.SelectedItemLabel.Text = this.selectedTradableLine.LocalizedName;
				this.UnitPriceLabel.Text = this.GetFormattedPrice(this.selectedTradableLine.Tradable, this.CurrentQuantity);
			}
			else
			{
				this.CurrentQuantity = 0;
				this.SelectedItemLabel.Text = string.Empty;
				this.UnitPriceLabel.Text = string.Empty;
			}
		}
	}

	private int CurrentQuantity
	{
		get
		{
			return this.currentQuantity;
		}
		set
		{
			if (this.SelectedTradableLine == null)
			{
				this.currentQuantity = value;
			}
			else
			{
				this.currentQuantity = Mathf.Min(value, (int)this.SelectedTradableLine.Tradable.Quantity);
				this.currentQuantity = Mathf.Min(this.currentQuantity, 10);
				this.previousQuantity = Mathf.Max(this.currentQuantity, 1);
			}
			this.QuantityTextField.AgePrimitiveLabel.Text = this.currentQuantity.ToString();
			this.RefreshButtons();
		}
	}

	public override void Bind(global::Empire empire)
	{
		base.Bind(empire);
		this.RetrieveSubCategories();
		this.FiltersContainer.SetContent(this.subCategories, "Marketplace", base.gameObject);
		this.SubPanelsContainer.SetContent(this.subCategories, base.gameObject);
		for (int i = 0; i < this.SubPanelsContainer.SubPanels.Count; i++)
		{
			TradablesListPanel tradablesListPanel = this.SubPanelsContainer.SubPanels[i] as TradablesListPanel;
			if (tradablesListPanel != null)
			{
				tradablesListPanel.Bind(base.Empire);
			}
		}
		this.DepartmentOfScience = base.Empire.GetAgency<DepartmentOfScience>();
		this.UpdateFiltersAvailability();
		FilterToggle filterToggle2 = this.FiltersContainer.FilterToggles.FirstOrDefault((FilterToggle filterToggle) => filterToggle.AgeTransform.Enable);
		if (filterToggle2 != null)
		{
			filterToggle2.Toggle.State = true;
			this.FiltersContainer.OnToggleFilter(filterToggle2);
		}
		base.NeedRefresh = true;
	}

	public override void Unbind()
	{
		if (base.Empire == null)
		{
			return;
		}
		this.DepartmentOfScience = null;
		for (int i = 0; i < this.SubPanelsContainer.SubPanels.Count; i++)
		{
			TradablesListPanel tradablesListPanel = this.SubPanelsContainer.SubPanels[i] as TradablesListPanel;
			if (tradablesListPanel != null)
			{
				tradablesListPanel.Unbind();
			}
		}
		this.SubPanelsContainer.UnsetContent();
		this.FiltersContainer.UnsetContent();
		if (this.subCategories != null)
		{
			this.subCategories.Clear();
			this.subCategories = null;
		}
		base.Unbind();
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		this.RefreshButtons();
	}

	public void OnToggleFilter(FilterToggle selectedFilter)
	{
		base.NeedRefresh = true;
	}

	public void OnToggleLineOff()
	{
		this.OnToggleLine(null);
	}

	public void OnToggleLine(TradableLine tradableLine)
	{
		if (tradableLine != null && tradableLine.SelectionToggle.State)
		{
			this.SelectedTradableLine = tradableLine;
		}
		else
		{
			this.SelectedTradableLine = null;
		}
		this.RefreshButtons();
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		this.interactionsAllowed = base.PlayerController.CanSendOrders();
		this.SelectedTradableLine = null;
		if (parameters.Length > 0)
		{
			string subCategoryToSelect = parameters[0] as string;
			if (subCategoryToSelect != null)
			{
				this.FiltersContainer.SelectCategory(subCategoryToSelect);
			}
		}
		this.UpdateFiltersAvailability();
		if (StaticString.IsNullOrEmpty(this.FiltersContainer.SelectedCategory))
		{
			this.FiltersContainer.SelectFirstAvailableCategory();
		}
		base.NeedRefresh = true;
		yield break;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		this.previousQuantity = 1;
		base.NeedRefresh = false;
		this.FiltersContainer.SelectCategory(null);
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		base.UseRefreshLoop = true;
		this.QuantityTextField.ValidChars = AgeLocalizer.Instance.LocalizeString("%NumbersValidChars");
		yield break;
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		this.EndTurnService = Services.GetService<IEndTurnService>();
		yield break;
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.Hide(true);
		this.EndTurnService = null;
		base.OnUnloadGame(game);
	}

	private void EndTurnService_GameClientStateChange(object sender, GameClientStateChangeEventArgs e)
	{
		if (base.IsVisible && base.PlayerController != null)
		{
			bool flag = base.PlayerController.CanSendOrders();
			if (this.interactionsAllowed != flag)
			{
				this.interactionsAllowed = flag;
				this.RefreshButtons();
				if (!this.interactionsAllowed && MessagePanel.Instance.IsVisible)
				{
					MessagePanel.Instance.Hide(false);
				}
			}
		}
	}

	private void DepartmentOfScience_TechnologyUnlocked(object sender, ConstructibleElementEventArgs e)
	{
		if (base.IsVisible)
		{
			this.UpdateFiltersAvailability();
		}
	}

	private void RetrieveSubCategories()
	{
		this.subCategories = new List<StaticString>();
		this.subCategories.Add(new StaticString("Strategic"));
		this.subCategories.Add(new StaticString("Luxury"));
		this.subCategories.Add(new StaticString("Booster"));
		this.subCategories.Add(new StaticString("Unit"));
		this.subCategories.Add(new StaticString("Hero"));
		IDatabase<TradableCategoryDefinition> database = Databases.GetDatabase<TradableCategoryDefinition>(false);
		Diagnostics.Assert(database != null, "Could not retrieve the database of TradableCategoryDefinition");
		for (int i = 0; i < database.GetValues().Length; i++)
		{
			StaticString subCategory = database.GetValues()[i].SubCategory;
			if (!this.subCategories.Contains(subCategory))
			{
				this.subCategories.Add(subCategory);
			}
		}
	}

	private void BuyTradable(ITradable tradable, Garrison destination = null)
	{
		if (base.PlayerController == null)
		{
			Diagnostics.LogError("No PlayerController on BuyablePanel.");
			return;
		}
		if (this.SelectedTradableLine == null || this.SelectedTradableLine.Tradable == null)
		{
			Diagnostics.LogError("Trying to buy a tradable while there is no tradable selected.");
			return;
		}
		if (this.SelectedTradableLine.Tradable is TradableUnit && !(this.SelectedTradableLine.Tradable is TradableHero) && destination != null)
		{
			OrderBuyoutTradableUnit order = new OrderBuyoutTradableUnit(base.Empire.Index, this.SelectedTradableLine.Tradable.UID, (float)this.CurrentQuantity, destination.GUID);
			base.PlayerController.PostOrder(order);
		}
		else
		{
			OrderBuyoutTradable order2 = new OrderBuyoutTradable(base.Empire.Index, this.SelectedTradableLine.Tradable.UID, (float)this.CurrentQuantity);
			base.PlayerController.PostOrder(order2);
		}
		this.SelectedTradableLine = null;
		this.RefreshButtons();
	}

	private void OnChangeQuantityCB(GameObject obj)
	{
		this.CheckQuantityValidity();
	}

	private void OnQuantityFocusCB(GameObject obj)
	{
		this.QuantityTextField.ReplaceInputText(string.Empty);
	}

	private void OnQuantityFocusLostCB(GameObject obj)
	{
		if (this.SelectedTradableLine == null)
		{
			return;
		}
		this.ChangeQuantityIfValid();
		this.QuantityTextField.AgePrimitiveLabel.TintColor = Color.white;
	}

	private void OnValidateQuantityCB(GameObject obj)
	{
		AgeManager.Instance.FocusedControl = null;
	}

	private void ChangeQuantityIfValid()
	{
		this.ClampEnteredQuantity();
		if (this.CheckQuantityValidity())
		{
			float f;
			float.TryParse(this.QuantityTextField.AgePrimitiveLabel.Text, out f);
			this.CurrentQuantity = Mathf.FloorToInt(f);
		}
		else
		{
			this.QuantityTextField.ReplaceInputText(GuiFormater.FormatGui(this.CurrentQuantity));
		}
	}

	private void ClampEnteredQuantity()
	{
		float num;
		if (!float.TryParse(this.QuantityTextField.AgePrimitiveLabel.Text, out num))
		{
			this.QuantityTextField.ReplaceInputText(GuiFormater.FormatGui(this.CurrentQuantity));
			return;
		}
		if (num <= 1f)
		{
			Diagnostics.Assert(this.SelectedTradableLine.Tradable.Quantity >= 1f);
			this.QuantityTextField.ReplaceInputText("1");
		}
		else if (num > this.SelectedTradableLine.Tradable.Quantity)
		{
			this.QuantityTextField.ReplaceInputText(Mathf.FloorToInt(this.SelectedTradableLine.Tradable.Quantity).ToString());
		}
	}

	private bool CheckQuantityValidity()
	{
		if (this.SelectedTradableLine == null)
		{
			return false;
		}
		float num;
		bool flag = float.TryParse(this.QuantityTextField.AgePrimitiveLabel.Text, out num);
		this.QuantityTextField.AgePrimitiveLabel.TintColor = Color.red;
		if (this.QuantityTextField.AgePrimitiveLabel.Text.Trim().Length == 0)
		{
			this.QuantityTextField.AgeTransform.AgeTooltip.Content = "%MarketplaceQuantityCannotBeEmpty";
			return false;
		}
		if (!flag)
		{
			this.QuantityTextField.AgeTransform.AgeTooltip.Content = "%MarketplaceQuantityContainsInvalidChars";
			return false;
		}
		if (num < 1f)
		{
			this.QuantityTextField.AgeTransform.AgeTooltip.Content = "%MarketplaceQuantityCannotBeEmpty";
			return false;
		}
		if (num > this.SelectedTradableLine.Tradable.Quantity)
		{
			this.QuantityTextField.AgeTransform.AgeTooltip.Content = "%MarketplaceQuantityHigherThanStock";
			return false;
		}
		if (num > 10f)
		{
			this.QuantityTextField.AgeTransform.AgeTooltip.Content = string.Format(AgeLocalizer.Instance.LocalizeString("%MarketplaceBuyQuantityPlusHardLimitReachedDescription"), 10);
			return false;
		}
		this.QuantityTextField.AgePrimitiveLabel.TintColor = Color.white;
		this.QuantityTextField.AgeTransform.AgeTooltip.Content = null;
		return true;
	}

	private void OnQuantityMinusCB(GameObject obj)
	{
		this.CurrentQuantity = (int)Mathf.Clamp((float)(this.CurrentQuantity - 1), 1f, float.PositiveInfinity);
	}

	private void OnQuantityPlusCB(GameObject obj)
	{
		this.CurrentQuantity++;
	}

	private void OnBuyCB(GameObject obj)
	{
		if (this.SelectedTradableLine == null || this.SelectedTradableLine.Tradable == null)
		{
			Diagnostics.LogWarning("Trying to buy a tradable while there is no tradable selected.");
			this.RefreshButtons();
			return;
		}
		if (this.CurrentQuantity == 0)
		{
			Diagnostics.LogWarning("Trying to buy a tradable with quantity 0.");
			this.RefreshButtons();
			return;
		}
		this.OnBuyTradableConfirmation();
	}

	private void OnBuyTradableConfirmation()
	{
		if (this.SelectedTradableLine == null || this.SelectedTradableLine.Tradable == null)
		{
			Diagnostics.LogError("No tradable selected after buy confirmation.");
			return;
		}
		if (this.SelectedTradableLine.Tradable is TradableUnit && !(this.SelectedTradableLine.Tradable is TradableHero))
		{
			base.GuiService.Show(typeof(DestinationSelectionModalPanel), new object[]
			{
				base.gameObject,
				"UnitTransfer",
				this.SelectedTradableLine.Tradable as TradableUnit
			});
		}
		else
		{
			this.BuyTradable(this.SelectedTradableLine.Tradable, null);
		}
	}

	private void ValidateDestinationChoice(Garrison garrison)
	{
		if (this.SelectedTradableLine == null || this.SelectedTradableLine.Tradable == null)
		{
			Diagnostics.LogError("No tradable unit selected after city validation.");
			return;
		}
		this.BuyTradable(this.SelectedTradableLine.Tradable, garrison);
	}

	private void RefreshButtons()
	{
		if (this.SelectedTradableLine != null && this.SelectedTradableLine.Tradable != null && this.SelectedTradableLine.Tradable.Quantity > 0f)
		{
			this.QuantityTextField.AgeTransform.Enable = true;
			this.QuantityMinusButton.AgeTransform.Enable = (this.CurrentQuantity > 1);
			if (this.CurrentQuantity >= Mathf.FloorToInt(this.SelectedTradableLine.Tradable.Quantity))
			{
				this.QuantityPlusButton.AgeTransform.Enable = false;
				this.QuantityPlusButton.AgeTransform.AgeTooltip.Content = "%MarketplaceBuyQuantityPlusTradableQuantityReachedDescription";
			}
			else if (this.CurrentQuantity >= 10)
			{
				this.QuantityPlusButton.AgeTransform.Enable = false;
				this.QuantityPlusButton.AgeTransform.AgeTooltip.Content = string.Format(AgeLocalizer.Instance.LocalizeString("%MarketplaceBuyQuantityPlusHardLimitReachedDescription"), 10);
			}
			else
			{
				this.QuantityPlusButton.AgeTransform.Enable = true;
				this.QuantityPlusButton.AgeTransform.AgeTooltip.Content = "%MarketplaceBuyQuantityPlusDescription";
			}
			string formattedPrice = this.GetFormattedPrice(this.SelectedTradableLine.Tradable, this.CurrentQuantity);
			this.BuyButtonPriceLabel.Text = formattedPrice;
			if (this.SelectedTradableLine.Tradable.Quantity >= (float)this.CurrentQuantity && this.CanAfford(this.SelectedTradableLine.Tradable, this.CurrentQuantity))
			{
				this.BuyButton.AgeTransform.Enable = this.interactionsAllowed;
				AgeTooltip ageTooltip = this.BuyButton.AgeTransform.AgeTooltip;
				if (ageTooltip != null)
				{
					string formattedLine = AgeLocalizer.Instance.LocalizeString("%MarketplaceBuyDescription").Replace("$Quantity", this.CurrentQuantity.ToString()).Replace("$TradableName", this.SelectedTradableLine.LocalizedName).Replace("$Price", formattedPrice);
					StringBuilder stringBuilder = new StringBuilder();
					AgeUtils.CleanLine(formattedLine, ref stringBuilder);
					ageTooltip.Content = stringBuilder.ToString();
					if (this.SelectedTradableLine.Tradable is TradableUnit)
					{
						ITradeManagementService service = base.Game.Services.GetService<ITradeManagementService>();
						UnitDesign unitDesign;
						if (service != null && service.TryRetrieveUnitDesign((this.SelectedTradableLine.Tradable as TradableUnit).Barcode, out unitDesign) && unitDesign.Tags.Contains(DownloadableContent9.TagColossus))
						{
							float propertyValue = base.Empire.GetPropertyValue(SimulationProperties.MaximumNumberOfColossi);
							float propertyValue2 = base.Empire.GetPropertyValue(SimulationProperties.NumberOfColossi);
							if (propertyValue - propertyValue2 < 1f)
							{
								formattedLine = AgeLocalizer.Instance.LocalizeString("%MarketplaceCannotBuyColossusDescription").Replace("$ColossiCap", propertyValue.ToString());
								stringBuilder = new StringBuilder();
								AgeUtils.CleanLine(formattedLine, ref stringBuilder);
								this.BuyButton.AgeTransform.Enable = false;
								ageTooltip.Content = stringBuilder.ToString();
							}
						}
					}
				}
			}
			else
			{
				this.BuyButton.AgeTransform.Enable = false;
				AgeTooltip ageTooltip2 = this.BuyButton.AgeTransform.AgeTooltip;
				if (ageTooltip2 != null)
				{
					string formattedLine2 = AgeLocalizer.Instance.LocalizeString("%MarketplaceCannotBuyDescription").Replace("$Quantity", this.CurrentQuantity.ToString()).Replace("$TradableName", this.SelectedTradableLine.LocalizedName);
					StringBuilder stringBuilder2 = new StringBuilder();
					AgeUtils.CleanLine(formattedLine2, ref stringBuilder2);
					ageTooltip2.Content = stringBuilder2.ToString();
				}
			}
		}
		else
		{
			this.QuantityTextField.AgeTransform.Enable = false;
			this.QuantityMinusButton.AgeTransform.Enable = false;
			this.QuantityPlusButton.AgeTransform.Enable = false;
			this.BuyButton.AgeTransform.Enable = false;
			this.BuyButtonPriceLabel.Text = string.Empty;
			AgeTooltip ageTooltip3 = this.BuyButton.AgeTransform.AgeTooltip;
			if (ageTooltip3 != null)
			{
				ageTooltip3.Content = null;
			}
		}
	}

	private void UpdateFiltersAvailability()
	{
		if (this.DepartmentOfScience.CanTradeResourcesAndBoosters(false))
		{
			this.FiltersContainer.EnableCategoryFilter("Strategic", true, string.Empty);
			this.FiltersContainer.EnableCategoryFilter("Luxury", true, string.Empty);
			this.FiltersContainer.EnableCategoryFilter("Booster", true, string.Empty);
		}
		else
		{
			string tooltipIfDisabled = string.Empty;
			GuiElement guiElement;
			if (base.GuiService.GuiPanelHelper.TryGetGuiElement("TechnologyDefinitionMarketplaceResources", out guiElement))
			{
				string arg = AgeLocalizer.Instance.LocalizeString(guiElement.Title);
				tooltipIfDisabled = string.Format(AgeLocalizer.Instance.LocalizeString("%MarketplaceFilterLockedDescription"), arg);
			}
			this.FiltersContainer.EnableCategoryFilter("Strategic", false, tooltipIfDisabled);
			this.FiltersContainer.EnableCategoryFilter("Luxury", false, tooltipIfDisabled);
			this.FiltersContainer.EnableCategoryFilter("Booster", false, tooltipIfDisabled);
		}
		if (this.DepartmentOfScience.CanTradeUnits(false))
		{
			this.FiltersContainer.EnableCategoryFilter("Unit", true, string.Empty);
		}
		else
		{
			string tooltipIfDisabled2 = string.Empty;
			GuiElement guiElement2;
			if (base.GuiService.GuiPanelHelper.TryGetGuiElement("TechnologyDefinitionMarketplaceMercenaries", out guiElement2))
			{
				string arg2 = AgeLocalizer.Instance.LocalizeString(guiElement2.Title);
				tooltipIfDisabled2 = string.Format(AgeLocalizer.Instance.LocalizeString("%MarketplaceFilterLockedDescription"), arg2);
			}
			this.FiltersContainer.EnableCategoryFilter("Unit", false, tooltipIfDisabled2);
		}
		if (this.DepartmentOfScience.CanTradeHeroes(false))
		{
			this.FiltersContainer.EnableCategoryFilter("Hero", true, string.Empty);
		}
		else
		{
			string tooltipIfDisabled3 = string.Empty;
			GuiElement guiElement3;
			if (base.GuiService.GuiPanelHelper.TryGetGuiElement("TechnologyDefinitionMarketplaceMercenaries", out guiElement3))
			{
				string arg3 = AgeLocalizer.Instance.LocalizeString(guiElement3.Title);
				tooltipIfDisabled3 = string.Format(AgeLocalizer.Instance.LocalizeString("%MarketplaceFilterLockedDescription"), arg3);
			}
			this.FiltersContainer.EnableCategoryFilter("Hero", false, tooltipIfDisabled3);
		}
	}

	private string GetFormattedPrice(Tradable tradable, int quantity)
	{
		float priceWithSalesTaxes = tradable.GetPriceWithSalesTaxes(TradableTransactionType.Buyout, base.Empire, (float)quantity);
		ConstructionCost[] costs = new ConstructionCost[]
		{
			new ConstructionCost(DepartmentOfTheTreasury.Resources.EmpireMoney, priceWithSalesTaxes, true, false)
		};
		return GuiFormater.FormatCost(base.Empire, costs, false, 1, null);
	}

	private bool CanAfford(Tradable tradable, int quantity)
	{
		DepartmentOfTheTreasury agency = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		float priceWithSalesTaxes = tradable.GetPriceWithSalesTaxes(TradableTransactionType.Buyout, base.Empire, (float)quantity);
		return agency.CanAfford(new ConstructionCost[]
		{
			new ConstructionCost(DepartmentOfTheTreasury.Resources.EmpireMoney, priceWithSalesTaxes, true, false)
		});
	}

	public const float InterPanelSpacing = 10f;

	public Transform TradablesListPanelPrefab;

	public FilterTogglesContainer FiltersContainer;

	public FilteredSubPanelsContainer SubPanelsContainer;

	public AgePrimitiveLabel SelectedItemLabel;

	public AgePrimitiveLabel UnitPriceLabel;

	public AgeControlTextField QuantityTextField;

	public AgeControlButton QuantityMinusButton;

	public AgeControlButton QuantityPlusButton;

	public AgeControlButton BuyButton;

	public AgePrimitiveLabel BuyButtonPriceLabel;

	private List<StaticString> subCategories;

	private TradableLine selectedTradableLine;

	private int currentQuantity;

	private int previousQuantity = 1;

	private DepartmentOfScience departmentOfScience;

	private IEndTurnService endTurnService;

	private bool interactionsAllowed = true;
}
