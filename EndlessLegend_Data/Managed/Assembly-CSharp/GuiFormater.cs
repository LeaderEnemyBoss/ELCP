using System;
using System.Text;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Localization;
using Amplitude.Unity.Simulation;
using UnityEngine;

public class GuiFormater
{
	public static string FormatGui(float value, bool isPercent = false, bool needCeil = false, bool showSign = false, int decimals = 1)
	{
		if (isPercent)
		{
			return value.ToString("####0%");
		}
		if (needCeil)
		{
			value = (float)((value <= 0f) ? Mathf.FloorToInt(value) : Mathf.CeilToInt(value));
		}
		else if (decimals == 0)
		{
			value = (float)((value <= 0f) ? Mathf.CeilToInt(value) : Mathf.FloorToInt(value));
		}
		string text = "######0";
		if (decimals > 0)
		{
			text = text + "." + new string('#', decimals);
		}
		if (isPercent)
		{
			text += "%";
		}
		if (showSign)
		{
			text = string.Concat(new string[]
			{
				"+",
				text,
				";-",
				text,
				";+0"
			});
			if (isPercent)
			{
				text += "%";
			}
		}
		return value.ToString(text);
	}

	public static string FormatGui(int value)
	{
		return value.ToString("######0");
	}

	public static string FormatTooltip(float value, bool isPercent = false)
	{
		if (isPercent)
		{
			return value.ToString("######0.#%");
		}
		return value.ToString("######0.#");
	}

	public static string FormatTooltip(int value)
	{
		return value.ToString("######0");
	}

	public static string FormatCost(Empire empire, IConstructionCost[] costs, bool monochromatic = false, int decimals = 1, SimulationObject context = null)
	{
		DepartmentOfTheTreasury agency = empire.GetAgency<DepartmentOfTheTreasury>();
		if (context == null)
		{
			context = empire.SimulationObject;
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (costs != null)
		{
			bool flag = false;
			for (int i = 0; i < costs.Length; i++)
			{
				float value = costs[i].GetValue(empire);
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append(" ");
				}
				float num;
				if (!agency.TryGetResourceStockValue(context, costs[i].ResourceName, out num, false))
				{
					Diagnostics.Log("Can't get resource stock value {0} on simulation object {1}.", new object[]
					{
						costs[i].ResourceName,
						empire.SimulationObject.Name
					});
				}
				if (!monochromatic && (costs[i].Instant || costs[i].InstantOnCompletion) && num < value)
				{
					AgeUtils.ColorToHexaKey(Color.red, ref stringBuilder, false);
					flag = true;
				}
				stringBuilder.Append(GuiFormater.FormatGui(value, false, decimals == 0, false, decimals));
				if (!monochromatic && flag)
				{
					stringBuilder.Append("#REVERT#");
					flag = false;
				}
				stringBuilder.Append(" ");
				stringBuilder.Append(Services.GetService<IGuiService>().FormatSymbol(costs[i].ResourceName));
			}
		}
		return stringBuilder.ToString();
	}

	public static string FormatInstantCost(Empire empire, float cost, StaticString resourceName, bool monochromatic = false, int decimals = 1)
	{
		DepartmentOfTheTreasury agency = empire.GetAgency<DepartmentOfTheTreasury>();
		float num;
		if (!agency.TryGetResourceStockValue(empire.SimulationObject, resourceName, out num, false))
		{
			Diagnostics.Log("Can't get resource stock value {0} on simulation object {1}.", new object[]
			{
				resourceName,
				empire.SimulationObject.Name
			});
		}
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		if (!monochromatic && num < cost)
		{
			AgeUtils.ColorToHexaKey(Color.red, ref stringBuilder, false);
			flag = true;
		}
		stringBuilder.Append(GuiFormater.FormatGui(cost, false, decimals == 0, false, decimals));
		if (!monochromatic && flag)
		{
			stringBuilder.Append("#REVERT#");
		}
		stringBuilder.Append(" ");
		stringBuilder.Append(Services.GetService<IGuiService>().FormatSymbol(resourceName));
		return stringBuilder.ToString();
	}

	public static string FormatQuantity(float quantity, StaticString resourceName, int decimals = 1)
	{
		return GuiFormater.FormatGui(quantity, false, false, false, decimals) + " " + Services.GetService<IGuiService>().FormatSymbol(resourceName);
	}

	public static string FormatStock(float stockValue, StaticString resourceName, int decimals = 1, bool useKilo = false)
	{
		if (useKilo)
		{
			return string.Format("{0} {1}", (stockValue < 1000f) ? GuiFormater.FormatGui(Mathf.Floor(stockValue), false, false, false, 0) : ((stockValue < 10000f) ? (GuiFormater.FormatGui(Mathf.Floor(stockValue / 1000f), false, false, false, 0) + "." + GuiFormater.FormatGui(Mathf.Floor(stockValue % 1000f / 100f), false, false, false, 0) + "k") : (GuiFormater.FormatGui(Mathf.Floor(stockValue / 1000f), false, false, false, 0) + "k")), Services.GetService<IGuiService>().FormatSymbol(resourceName));
		}
		return string.Format("{0} {1}", GuiFormater.FormatGui(Mathf.Floor(stockValue), false, false, true, 0), Services.GetService<IGuiService>().FormatSymbol(resourceName));
	}

	public static string FormatStockAndNet(float stockValue, float netValue, StaticString resourceName, bool useKilo = false)
	{
		if (useKilo)
		{
			return string.Format("{0} {1}({2})", Services.GetService<IGuiService>().FormatSymbol(resourceName), (stockValue < 1000f) ? GuiFormater.FormatGui(Mathf.Floor(stockValue), false, false, false, 0) : ((stockValue < 10000f) ? (GuiFormater.FormatGui(Mathf.Floor(stockValue / 1000f), false, false, false, 0) + "." + GuiFormater.FormatGui(Mathf.Floor(stockValue % 1000f / 100f), false, false, false, 0) + "k") : (GuiFormater.FormatGui(Mathf.Floor(stockValue / 1000f), false, false, false, 0) + "k")), (netValue < 1000f) ? GuiFormater.FormatGui(netValue, false, false, true, 0) : ((netValue < 10000f) ? (GuiFormater.FormatGui(netValue / 1000f, false, false, true, 0) + "." + GuiFormater.FormatGui(netValue % 1000f / 100f, false, false, false, 0) + "k") : (GuiFormater.FormatGui(netValue / 1000f, false, false, true, 0) + "k")));
		}
		return string.Format("{0} {1}({2})", Services.GetService<IGuiService>().FormatSymbol(resourceName), GuiFormater.FormatGui(Mathf.Floor(stockValue), false, false, false, 0), GuiFormater.FormatGui(netValue, false, false, true, 0));
	}

	public static string FormatStockCost(Empire empire, ConstructionResourceStock[] stockCosts, bool monochromatic = false, int decimals = 1)
	{
		DepartmentOfTheTreasury agency = empire.GetAgency<DepartmentOfTheTreasury>();
		StringBuilder stringBuilder = new StringBuilder();
		if (stockCosts != null)
		{
			bool flag = false;
			for (int i = 0; i < stockCosts.Length; i++)
			{
				float stock = stockCosts[i].Stock;
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append(" ");
				}
				float num;
				if (!agency.TryGetResourceStockValue(empire.SimulationObject, stockCosts[i].PropertyName, out num, false))
				{
					Diagnostics.Log("Can't get resource stock value {0} on simulation object {1}.", new object[]
					{
						stockCosts[i].PropertyName,
						empire.SimulationObject.Name
					});
				}
				if (!monochromatic)
				{
					AgeUtils.ColorToHexaKey(Color.red, ref stringBuilder, false);
					flag = true;
				}
				stringBuilder.Append(GuiFormater.FormatGui(stock, false, decimals == 0, false, decimals));
				if (!monochromatic && flag)
				{
					stringBuilder.Append("#REVERT#");
					flag = false;
				}
				stringBuilder.Append(" ");
				stringBuilder.Append(Services.GetService<IGuiService>().FormatSymbol(stockCosts[i].PropertyName));
			}
		}
		return stringBuilder.ToString();
	}

	public static string FormatDiplomaticInteraction(string interactionType, MajorEmpire empire)
	{
		string text = "%DiplomaticInteraction" + interactionType;
		ILocalizationService service = Services.GetService<ILocalizationService>();
		string text2 = empire.Faction.Affinity.Name.ToString();
		if (text2.StartsWith("Affinity") && text2.Length > 8)
		{
			text2 = text2.Substring(8);
		}
		string text3 = service.Localize(text + "_" + text2);
		if (string.IsNullOrEmpty(text3))
		{
			text3 = service.Localize(text);
			if (string.IsNullOrEmpty(text3))
			{
				text3 = text;
			}
		}
		return text3;
	}

	private const string GuiNumberFormat = "######0";

	private const string GuiPercentFormat = "####0%";

	private const string TooltipNumberFormat = "######0.#";

	private const string TooltipPercentFormat = "######0.#%";

	private const string GuiNumberShowSignFormat = "+#;-#;0";

	private const string DiplomaticFormat = "%DiplomaticInteraction";

	public static char Infinite = '∞';

	public static Color ColorDarkRed = new Color(1f, 0.31f, 0.31f);

	public static Color ColorGold = new Color(1f, 0.7f, 0.25f);
}
