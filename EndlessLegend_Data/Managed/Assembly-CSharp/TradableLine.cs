using System;
using System.Text;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Gui;
using UnityEngine;

public abstract class TradableLine : SortedLine
{
	public abstract Tradable Tradable { get; }

	public string LocalizedName { get; protected set; }

	public float PriceLevel { get; protected set; }

	public float Price { get; protected set; }

	public virtual void Bind(ITradable tradable, Empire empire, TradablesListPanel parent)
	{
		this.guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
		Diagnostics.Assert(this.guiPanelHelper != null, "Unable to access GuiPanelHelper");
		this.empire = empire;
		this.parent = parent;
	}

	public virtual void Unbind()
	{
		this.parent = null;
		this.empire = null;
		this.guiPanelHelper = null;
	}

	public virtual void RefreshContent()
	{
		if (this.Tradable == null)
		{
			Diagnostics.LogError("No Tradable bound to the TradableLine");
			return;
		}
		this.SelectionToggle.State = false;
		this.AgeTransform.Enable = ((int)this.Tradable.Quantity > 0);
	}

	protected virtual void DisplayPriceLevel()
	{
		this.PriceLevelLabel.Text = GuiFormater.FormatGui(this.PriceLevel, false, false, true, 1);
		string priceLevelName = this.GetPriceLevelName();
		GuiElement guiElement;
		if (!this.guiPanelHelper.TryGetGuiElement(priceLevelName, out guiElement))
		{
			Diagnostics.LogError("Cannot find a GuiElement for the price level {0}", new object[]
			{
				priceLevelName
			});
			return;
		}
		Texture2D image;
		if (!this.guiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Large, out image))
		{
			Diagnostics.LogError("Cannot find a texture for the large icon of GuiElement {0}", new object[]
			{
				guiElement.Name
			});
			return;
		}
		this.PriceLevelImage.Image = image;
		AgeTooltip ageTooltip = this.PriceLevelGroup.GetComponent<AgeTransform>().AgeTooltip;
		if (ageTooltip != null)
		{
			float referencePriceWithSalesTaxes = this.Tradable.GetReferencePriceWithSalesTaxes(TradableTransactionType.Buyout, this.empire);
			ageTooltip.Content = string.Concat(new string[]
			{
				AgeLocalizer.Instance.LocalizeString("%MarketplaceTradableMaximumPrice"),
				": ",
				GuiFormater.FormatGui(referencePriceWithSalesTaxes * Tradable.MaximumPriceMultiplier, false, false, false, 1),
				"\n",
				AgeLocalizer.Instance.LocalizeString("%MarketplaceTradableStandardPrice"),
				": ",
				GuiFormater.FormatGui(referencePriceWithSalesTaxes, false, false, false, 1),
				"\n",
				AgeLocalizer.Instance.LocalizeString("%MarketplaceTradableMinimumPrice"),
				": ",
				GuiFormater.FormatGui(referencePriceWithSalesTaxes * Tradable.MinimumPriceMultiplier, false, false, false, 1)
			});
		}
	}

	protected void DisplayPrice()
	{
		ConstructionCost[] costs = new ConstructionCost[]
		{
			new ConstructionCost(DepartmentOfTheTreasury.Resources.EmpireMoney, this.Price, true, false)
		};
		string text = GuiFormater.FormatCost(this.empire, costs, false, 1, null);
		this.PriceLabel.Text = text;
		AgeTooltip ageTooltip = this.PriceLabel.AgeTransform.AgeTooltip;
		if (ageTooltip != null)
		{
			string formattedLine = AgeLocalizer.Instance.LocalizeString("%MarketplaceTradablePriceDescription").Replace("$TradableName", this.LocalizedName).Replace("$Price", text);
			StringBuilder stringBuilder = new StringBuilder();
			AgeUtils.CleanLine(formattedLine, ref stringBuilder);
			ageTooltip.Content = stringBuilder.ToString();
		}
	}

	protected string GetPriceLevelName()
	{
		float referencePriceWithSalesTaxes = this.Tradable.GetReferencePriceWithSalesTaxes(TradableTransactionType.Buyout, this.empire);
		float num = referencePriceWithSalesTaxes * Tradable.MinimumPriceMultiplier;
		float num2 = referencePriceWithSalesTaxes * Tradable.MaximumPriceMultiplier;
		float num3 = (referencePriceWithSalesTaxes - num) * 0.2f;
		float num4 = (num2 - referencePriceWithSalesTaxes) * 0.2f;
		float num5 = referencePriceWithSalesTaxes + num4 * 3f;
		float num6 = referencePriceWithSalesTaxes + num4;
		float num7 = referencePriceWithSalesTaxes - num3;
		float num8 = referencePriceWithSalesTaxes - num3 * 3f;
		string str = "PriceLevel";
		if (this.Price > num5)
		{
			return str + "VeryHigh";
		}
		if (this.Price > num6)
		{
			return str + "High";
		}
		if (this.Price > num7)
		{
			return str + "Normal";
		}
		if (this.Price > num8)
		{
			return str + "Low";
		}
		return str + "VeryLow";
	}

	private void OnToggleLineCB(GameObject obj)
	{
		this.parent.OnToggleLine(this);
	}

	public AgeControlToggle SelectionToggle;

	public AgeTransform PriceLevelGroup;

	public AgePrimitiveImage PriceLevelImage;

	public AgePrimitiveLabel PriceLevelLabel;

	public AgePrimitiveLabel PriceLabel;

	protected Empire empire;

	protected TradablesListPanel parent;

	protected IGuiPanelHelper guiPanelHelper;
}
