﻿using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Simulation.Advanced;
using UnityEngine;

public class GuiInfiltrationActionGroup
{
	public GuiInfiltrationActionGroup(StaticString firstName)
	{
		this.FirstName = firstName;
		this.InfiltrationActions = new List<InfiltrationAction>();
		this.InfiltrationActionCostString = new List<string>();
	}

	public List<StaticString> FailuresFlags
	{
		get
		{
			return this.failuresFlags;
		}
	}

	public StaticString FirstName { get; private set; }

	public List<InfiltrationAction> InfiltrationActions { get; private set; }

	public List<string> InfiltrationActionCostString { get; private set; }

	public InfiltrationAction CurrentAction { get; private set; }

	public bool IsValid { get; set; }

	public GuiElement GuiElement
	{
		get
		{
			return this.actionGuiElement;
		}
	}

	public Texture2D SubCategoryTexture
	{
		get
		{
			return this.subCategoryTexture;
		}
	}

	public static void ComputeInfiltrationActionGuiElement(Amplitude.Unity.Gui.IGuiService guiService, InfiltrationAction infiltrationAction, out GuiElement guiElement)
	{
		if (!guiService.GuiPanelHelper.TryGetGuiElement("InfiltrationAction" + infiltrationAction.Name, out guiElement))
		{
			guiElement = null;
		}
	}

	public static void ComputeInfiltrationActionSubCategoryTexture(Amplitude.Unity.Gui.IGuiService guiService, InfiltrationAction infiltrationAction, out Texture2D subCategoryTexture)
	{
		GuiElement guiElement;
		if (!string.IsNullOrEmpty(infiltrationAction.SubCategory) && guiService.GuiPanelHelper.TryGetGuiElement("InfiltrationActionSubCategory" + infiltrationAction.SubCategory, out guiElement) && guiService.GuiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Small, out subCategoryTexture))
		{
			return;
		}
		subCategoryTexture = AgeManager.Instance.FindDynamicTexture("InfiltrationActionSubCategoryUnknown", false);
	}

	public static string GenerateInfiltrationElementDescription(Empire spyEmpire, InfiltrationAction infiltrationAction, GuiElement guiElement)
	{
		string text = AgeLocalizer.Instance.LocalizeString(guiElement.Description);
		IInfiltrationActionWithBooster infiltrationActionWithBooster = infiltrationAction as IInfiltrationActionWithBooster;
		if (infiltrationActionWithBooster != null)
		{
			DepartmentOfPlanificationAndDevelopment agency = spyEmpire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
			if (agency != null)
			{
				int duration = infiltrationActionWithBooster.Duration;
				int num = 0;
				if (duration <= 0 && infiltrationActionWithBooster.BoosterReferences != null && infiltrationActionWithBooster.BoosterReferences.Length > 0)
				{
					IDatabase<BoosterDefinition> database = Databases.GetDatabase<BoosterDefinition>(false);
					BoosterDefinition boosterDefinition;
					if (database.TryGetValue(infiltrationActionWithBooster.BoosterReferences[0], out boosterDefinition))
					{
						num = DepartmentOfPlanificationAndDevelopment.GetBoosterDurationWithBonus(spyEmpire, spyEmpire, boosterDefinition);
					}
				}
				if (num <= 0)
				{
					num = DepartmentOfPlanificationAndDevelopment.GetBoosterDurationWithBonus(spyEmpire, duration);
				}
				text = text.Replace("$Duration", num.ToString());
			}
		}
		return text;
	}

	public void RefreshActionAvailability(DepartmentOfTheTreasury departmentOfTheTreasury, int infiltrationLevel, InterpreterContext interpreterContext, Amplitude.Unity.Gui.IGuiService guiService)
	{
		this.CurrentAction = this.InfiltrationActions.FirstOrDefault((InfiltrationAction infiltrationAction) => infiltrationAction.Level == infiltrationLevel);
		if (this.CurrentAction == null)
		{
			this.CurrentAction = (from infiltrationAction in this.InfiltrationActions
			where infiltrationAction.Level < infiltrationLevel
			select infiltrationAction).LastOrDefault<InfiltrationAction>();
			if (this.CurrentAction == null)
			{
				this.CurrentAction = (from infiltrationAction in this.InfiltrationActions
				where infiltrationAction.Level > infiltrationLevel
				select infiltrationAction).FirstOrDefault<InfiltrationAction>();
			}
		}
		this.FailuresFlags.Clear();
		if (interpreterContext != null)
		{
			this.IsValid = this.CurrentAction.CanExecute(interpreterContext, ref this.failuresFlags, new object[0]);
			if (!this.IsValid)
			{
				this.IsValid = !this.failuresFlags.Contains(ConstructionFlags.Discard);
			}
			this.IsValid &= (this.CurrentAction.Level <= infiltrationLevel);
		}
		else
		{
			this.failuresFlags.Add(InfiltrationAction.NoCanDoWithoutInfiltratedSpy);
			this.IsValid = false;
		}
		GuiInfiltrationActionGroup.ComputeInfiltrationActionGuiElement(guiService, this.CurrentAction, out this.actionGuiElement);
		GuiInfiltrationActionGroup.ComputeInfiltrationActionSubCategoryTexture(guiService, this.CurrentAction, out this.subCategoryTexture);
		this.InfiltrationActionCostString.Clear();
		List<string> list = new List<string>();
		int i = 0;
		while (i < this.InfiltrationActions.Count)
		{
			list.Clear();
			if (interpreterContext == null || departmentOfTheTreasury == null)
			{
				goto IL_228;
			}
			this.InfiltrationActions[i].ComputeConstructionCost(interpreterContext);
			if (InfiltrationAction.Context.ConstructionCosts == null)
			{
				goto IL_228;
			}
			for (int j = 0; j < InfiltrationAction.Context.ConstructionCosts.Length; j++)
			{
				ConstructionCost constructionCost = InfiltrationAction.Context.ConstructionCosts[j];
				string item = ((!departmentOfTheTreasury.CanAfford(constructionCost.Value, constructionCost.ResourceName)) ? "#DF1010#" : string.Empty) + string.Format("{0}{1}", GuiFormater.FormatGui(constructionCost.Value, false, true, false, 1), guiService.FormatSymbol(constructionCost.ResourceName));
				list.Add(item);
			}
			this.InfiltrationActionCostString.Add(string.Join(" ", list.ToArray()));
			IL_238:
			i++;
			continue;
			IL_228:
			this.InfiltrationActionCostString.Add("-");
			goto IL_238;
		}
	}

	private List<StaticString> failuresFlags = new List<StaticString>();

	private GuiElement actionGuiElement;

	private Texture2D subCategoryTexture;
}
