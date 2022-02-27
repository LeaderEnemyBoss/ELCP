using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using UnityEngine;

public class CityInfoPanel : global::GuiPanel
{
	public City City
	{
		get
		{
			return this.city;
		}
		private set
		{
			if (this.city != null)
			{
				this.city.Refreshed -= this.City_Refreshed;
			}
			this.city = value;
			if (this.city != null)
			{
				this.city.Refreshed += this.City_Refreshed;
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

	private DepartmentOfTheTreasury DepartmentOfTheTreasury
	{
		get
		{
			return this.departmentOfTheTreasury;
		}
		set
		{
			if (this.departmentOfTheTreasury != null)
			{
				this.departmentOfTheTreasury.ResourcePropertyChange -= this.DepartmentOfTheTreasury_ResourcePropertyChange;
			}
			this.departmentOfTheTreasury = value;
			if (this.departmentOfTheTreasury != null)
			{
				this.departmentOfTheTreasury.ResourcePropertyChange += this.DepartmentOfTheTreasury_ResourcePropertyChange;
			}
		}
	}

	public static void RefreshPopulationBuyoutButton(Amplitude.Unity.Game.Empire observer, City city, AgeControlButton populationBuyoutButton)
	{
		if (observer != city.Empire)
		{
			populationBuyoutButton.AgeTransform.Enable = false;
			return;
		}
		float propertyValue = city.GetPropertyValue(SimulationProperties.CityGrowthStock);
		float propertyValue2 = city.GetPropertyValue(SimulationProperties.NetCityGrowth);
		float propertyValue3 = city.GetPropertyValue(SimulationProperties.Population);
		DepartmentOfTheInterior agency = city.Empire.GetAgency<DepartmentOfTheInterior>();
		float num;
		float num2;
		agency.GetGrowthLimits(propertyValue3, out num, out num2);
		if (propertyValue + propertyValue2 >= num2)
		{
			populationBuyoutButton.AgeTransform.Enable = false;
			populationBuyoutButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%CityBuyoutPopulationNotNeededDescription");
			return;
		}
		float propertyValue4 = city.Empire.GetPropertyValue(SimulationProperties.PopulationBuyoutCooldown);
		if (propertyValue4 > 0f)
		{
			populationBuyoutButton.AgeTransform.Enable = false;
			populationBuyoutButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%CityBuyoutPopulationCooldownDescription").Replace("$Cooldown", propertyValue4.ToString());
			return;
		}
		float num3 = -DepartmentOfTheTreasury.GetPopulationBuyOutCost(city);
		ResourceDefinition resourceDefinition = ResourceDefinition.GetResourceDefinition(DepartmentOfTheTreasury.Resources.PopulationBuyout);
		string newValue = GuiFormater.FormatInstantCost(city.Empire, -num3, resourceDefinition.GetName(city.Empire), true, 0);
		DepartmentOfTheTreasury agency2 = city.Empire.GetAgency<DepartmentOfTheTreasury>();
		if (agency2.IsTransferOfResourcePossible(city.Empire, DepartmentOfTheTreasury.Resources.PopulationBuyout, ref num3))
		{
			populationBuyoutButton.AgeTransform.Enable = CityInfoPanel.interactionsAllowed;
			populationBuyoutButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%CityBuyoutPopulationDescription").Replace("$Cost", newValue);
		}
		else
		{
			populationBuyoutButton.AgeTransform.Enable = false;
			populationBuyoutButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%CityCannotBuyoutPopulationDescription").Replace("$Cost", newValue);
		}
	}

	public static void RefreshPopulationSacrificeButton(Amplitude.Unity.Game.Empire observer, City city, AgeControlButton populationSacrificeButton)
	{
		if (DepartmentOfTheInterior.CanSacrificePopulation(city) && observer == city.Empire)
		{
			populationSacrificeButton.AgeTransform.Enable = CityInfoPanel.interactionsAllowed;
			populationSacrificeButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%CityPopulationSacrificeDescription");
			populationSacrificeButton.AgeTransform.AgeTooltip.Content = populationSacrificeButton.AgeTransform.AgeTooltip.Content.Replace("1 \\7708\\", ((int)(city.Empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier) * 2f)).ToString() + " \\7708\\");
			return;
		}
		populationSacrificeButton.AgeTransform.Enable = false;
		populationSacrificeButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%CityCannotSacrificePopulationDescription");
	}

	public static void ShowHidePopulationBuyoutButton(Amplitude.Unity.Game.Empire observer, City city, AgeTransform populationBuyoutButton, AgeTransform populationGaugeBackground, AgeTransform nextPopulationTurns, AgeTransform nextPopulationHourglass, AgeTransform gaugePopulationTooltip)
	{
		bool flag = false;
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		if (service != null && service.IsShared(DownloadableContent11.ReadOnlyName))
		{
			DepartmentOfIntelligence agency = observer.GetAgency<DepartmentOfIntelligence>();
			if (agency != null)
			{
				flag = agency.IsEmpireInfiltrated(city.Empire);
			}
		}
		if (DepartmentOfTheInterior.CanBuyoutPopulation(city) && city.Empire == observer)
		{
			populationBuyoutButton.Visible = true;
			if (populationGaugeBackground != null)
			{
				populationGaugeBackground.Visible = false;
			}
			gaugePopulationTooltip.Visible = false;
			nextPopulationTurns.Visible = false;
			nextPopulationHourglass.Visible = false;
		}
		else if (DepartmentOfTheInterior.CanBuyoutPopulation(city) && flag)
		{
			populationBuyoutButton.Visible = false;
			if (populationGaugeBackground != null)
			{
				populationGaugeBackground.Visible = false;
			}
			gaugePopulationTooltip.Visible = false;
			nextPopulationTurns.Visible = false;
			nextPopulationHourglass.Visible = false;
		}
		else
		{
			populationBuyoutButton.Visible = false;
			if (populationGaugeBackground != null)
			{
				populationGaugeBackground.Visible = true;
			}
			gaugePopulationTooltip.Visible = true;
			nextPopulationTurns.Visible = true;
			nextPopulationHourglass.Visible = true;
		}
	}

	public static void ShowHidePopulationSacrificeButton(Amplitude.Unity.Game.Empire observer, DepartmentOfScience departmentOfScience, AgeControlButton populationSacrificeButton)
	{
		if (departmentOfScience.CanSacrificePopulation() && departmentOfScience.Empire == observer)
		{
			populationSacrificeButton.AgeTransform.Visible = true;
		}
		else
		{
			populationSacrificeButton.AgeTransform.Visible = false;
		}
	}

	public void Bind(City city)
	{
		GuiElement guiElement = null;
		this.City = city;
		this.DepartmentOfScience = this.City.Empire.GetAgency<DepartmentOfScience>();
		this.DepartmentOfTheTreasury = this.City.Empire.GetAgency<DepartmentOfTheTreasury>();
		base.GuiService.GetGuiPanel<CityManagementPanel>().Bind(this.City);
		this.CityWorkersPanel.Bind(this.City);
		for (int i = 0; i < this.typesFIDS.Count; i++)
		{
			base.GuiService.GuiPanelHelper.TryGetGuiElement(this.typesFIDS[i], out guiElement, this.City.Empire.Faction.Name);
			if (guiElement != null)
			{
				ExtendedGuiElement extendedGuiElement = (ExtendedGuiElement)guiElement;
				if (i < this.TotalValuesTable.GetChildren().Count)
				{
					AgeTransform ageTransform = this.TotalValuesTable.GetChildren()[i];
					for (int j = 0; j < ageTransform.GetChildren().Count; j++)
					{
						AgeTooltip component = ageTransform.GetComponent<AgeTooltip>();
						if (component != null)
						{
							component.Class = "Simple";
							component.Content = extendedGuiElement.Description;
						}
						AgeTransform ageTransform2 = ageTransform.GetChildren()[j];
						if (ageTransform2.name == "1Symbol")
						{
							Texture2D image;
							if (base.GuiService.GuiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Small, out image))
							{
								ageTransform2.GetComponent<AgePrimitiveImage>().Image = image;
								ageTransform2.GetComponent<AgePrimitiveImage>().TintColor = extendedGuiElement.Color;
							}
							break;
						}
					}
				}
			}
			SimulationPropertyTooltipData simulationPropertyTooltipData = this.valuesFIDS[i].AgeTransform.AgeTooltip.ClientData as SimulationPropertyTooltipData;
			if (simulationPropertyTooltipData != null)
			{
				simulationPropertyTooltipData.Context = this.City;
			}
		}
		for (int k = 0; k < this.typesFIDS.Count; k++)
		{
			this.previousFIDS[k] = this.City.GetPropertyValue(this.typesFIDS[k]);
			if (k < this.valuesFIDS.Count)
			{
				this.valuesFIDS[k].Text = GuiFormater.FormatGui(this.previousFIDS[k], false, true, false, 1);
			}
		}
		if (this.ApprovalGaugeTooltip.AgeTooltip != null)
		{
			SimulationPropertyTooltipData simulationPropertyTooltipData = this.ApprovalGaugeTooltip.AgeTooltip.ClientData as SimulationPropertyTooltipData;
			if (simulationPropertyTooltipData != null)
			{
				simulationPropertyTooltipData.Context = this.City;
			}
		}
		if (this.ApprovalState.AgeTransform.AgeTooltip != null)
		{
			this.ApprovalState.AgeTransform.AgeTooltip.ClientData = this.City;
		}
		if (this.GaugePopulationTooltip.AgeTooltip != null)
		{
			SimulationPropertyTooltipData simulationPropertyTooltipData = this.GaugePopulationTooltip.AgeTooltip.ClientData as SimulationPropertyTooltipData;
			if (simulationPropertyTooltipData != null)
			{
				simulationPropertyTooltipData.Context = this.City;
			}
		}
		if (this.PopulationCountGroup != null && this.PopulationCountGroup.AgeTooltip != null)
		{
			SimulationPropertyTooltipData simulationPropertyTooltipData = this.PopulationCountGroup.AgeTooltip.ClientData as SimulationPropertyTooltipData;
			if (simulationPropertyTooltipData != null)
			{
				simulationPropertyTooltipData.Context = this.City;
			}
		}
		CityInfoPanel.ShowHidePopulationBuyoutButton(this.playerControllerRepository.ActivePlayerController.Empire, this.City, this.PopulationBuyoutButton.AgeTransform, this.GaugePopulationBackground, this.NextPopulationTurns.AgeTransform, this.NextPopulationHourglass, this.GaugePopulationTooltip);
		CityInfoPanel.ShowHidePopulationSacrificeButton(this.playerControllerRepository.ActivePlayerController.Empire, this.DepartmentOfScience, this.PopulationSacrificeButton);
	}

	public override bool HandleCancelRequest()
	{
		bool result = false;
		if (this.CityWorkersPanel.IsVisible)
		{
			result = true;
			this.PopulationControlToggle.State = false;
			this.OnPopulationControlCB(this.PopulationControlToggle.gameObject);
		}
		return result;
	}

	public void Unbind()
	{
		this.DepartmentOfScience = null;
		this.DepartmentOfTheTreasury = null;
		if (this.City != null)
		{
			this.CityWorkersPanel.Unbind();
			base.GuiService.GetGuiPanel<CityManagementPanel>().Unbind();
			for (int i = 0; i < this.valuesFIDS.Count; i++)
			{
				SimulationPropertyTooltipData simulationPropertyTooltipData = this.valuesFIDS[i].AgeTransform.AgeTooltip.ClientData as SimulationPropertyTooltipData;
				if (simulationPropertyTooltipData != null)
				{
					simulationPropertyTooltipData.Context = null;
				}
			}
			if (this.ApprovalGaugeTooltip.AgeTooltip != null)
			{
				SimulationPropertyTooltipData simulationPropertyTooltipData = this.ApprovalGaugeTooltip.AgeTooltip.ClientData as SimulationPropertyTooltipData;
				if (simulationPropertyTooltipData != null)
				{
					simulationPropertyTooltipData.Context = null;
				}
			}
			if (this.ApprovalState.AgeTransform.AgeTooltip != null)
			{
				this.ApprovalState.AgeTransform.AgeTooltip.ClientData = null;
			}
			if (this.GaugePopulationTooltip.AgeTooltip != null)
			{
				SimulationPropertyTooltipData simulationPropertyTooltipData = this.GaugePopulationTooltip.AgeTooltip.ClientData as SimulationPropertyTooltipData;
				if (simulationPropertyTooltipData != null)
				{
					simulationPropertyTooltipData.Context = null;
				}
			}
			if (this.PopulationCountGroup != null && this.PopulationCountGroup.AgeTooltip != null)
			{
				SimulationPropertyTooltipData simulationPropertyTooltipData = this.PopulationCountGroup.AgeTooltip.ClientData as SimulationPropertyTooltipData;
				if (simulationPropertyTooltipData != null)
				{
					simulationPropertyTooltipData.Context = null;
				}
			}
			this.City = null;
		}
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		float propertyValue = this.City.GetPropertyValue(SimulationProperties.NetCityApproval);
		this.ApprovalGauge.PercentRight = Mathf.Clamp(propertyValue, 0f, 100f);
		this.ApprovalValue.Text = GuiFormater.FormatGui(propertyValue * 0.01f, true, false, false, 1);
		StaticString descriptorNameFromType = this.City.GetDescriptorNameFromType("ApprovalStatus");
		GuiElement guiElement;
		if (base.GuiService.GuiPanelHelper.TryGetGuiElement(descriptorNameFromType, out guiElement, this.City.Empire.Faction.Name))
		{
			this.ApprovalState.Text = guiElement.Title;
			ExtendedGuiElement extendedGuiElement = guiElement as ExtendedGuiElement;
			if (extendedGuiElement != null)
			{
				this.ApprovalGauge.AgePrimitive.TintColor = extendedGuiElement.Color;
				this.ApprovalState.TintColor = extendedGuiElement.Color;
			}
		}
		else
		{
			this.ApprovalState.Text = "%ApprovalStatusUnknown";
		}
		if (this.ApprovalState.AgeTransform.AgeTooltip != null)
		{
			this.ApprovalState.AgeTransform.AgeTooltip.Content = descriptorNameFromType.ToString();
		}
		this.RefreshPopulationCount();
		this.RefreshPopulationGrowth();
		int num = Mathf.CeilToInt(this.City.GetPropertyValue(SimulationProperties.CityMoneyUpkeep));
		this.UpkeepValue.Text = GuiFormater.FormatQuantity((float)(-(float)num), SimulationProperties.CityMoneyUpkeep, 1);
		AgeTooltip ageTooltip = this.UpkeepValue.AgeTransform.AgeTooltip;
		ageTooltip.Content = string.Format(AgeLocalizer.Instance.LocalizeString("%BuildingsUpkeepFormatDescription"), this.UpkeepValue.Text);
		this.UpkeepIcon1.AgeTooltip.Copy(ageTooltip);
		this.UpkeepIcon2.AgeTooltip.Copy(ageTooltip);
		for (int i = 0; i < this.typesFIDS.Count; i++)
		{
			if (i < this.valuesFIDS.Count && i < this.deltaFIDS.Count)
			{
				if (!this.valuesFIDS[i].AgeTransform.ModifiersRunning)
				{
					this.valuesFIDS[i].AgeTransform.Alpha = 1f;
					this.valuesFIDS[i].AgeTransform.ResetAllModifiers(true, false);
					this.deltaFIDS[i].AgeTransform.Alpha = 0f;
					this.deltaFIDS[i].AgeTransform.ResetAllModifiers(true, false);
				}
				float propertyValue2 = this.City.GetPropertyValue(this.typesFIDS[i]);
				this.valuesFIDS[i].Text = GuiFormater.FormatGui(propertyValue2, false, false, false, 0);
				if (propertyValue2 != this.previousFIDS[i])
				{
					this.AnimateFIDSChange(this.valuesFIDS[i], this.deltaFIDS[i], propertyValue2 - this.previousFIDS[i]);
					this.previousFIDS[i] = propertyValue2;
				}
			}
		}
		for (int j = 0; j < this.workerTypes.Count; j++)
		{
			if (j < this.workersFIDS.Count)
			{
				this.workersFIDS[j].Text = this.City.GetPropertyValue(this.workerTypes[j]).ToString();
			}
		}
		if (this.CityWorkersPanel.IsVisible)
		{
			this.CityWorkersPanel.RefreshContent();
		}
		this.RefreshButtons();
	}

	protected override IEnumerator OnHide(bool instant)
	{
		base.NeedRefresh = false;
		for (int i = 0; i < this.typesFIDS.Count; i++)
		{
			if (i < this.valuesFIDS.Count && i < this.deltaFIDS.Count)
			{
				this.valuesFIDS[i].AgeTransform.ResetAllModifiers(true, false);
				this.deltaFIDS[i].AgeTransform.Alpha = 0f;
				this.deltaFIDS[i].AgeTransform.ResetAllModifiers(true, false);
			}
		}
		this.CityWorkersPanel.Hide(false);
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		base.UseRefreshLoop = true;
		this.valuesFIDS = new List<AgePrimitiveLabel>();
		this.deltaFIDS = new List<AgePrimitiveLabel>();
		this.workersFIDS = new List<AgePrimitiveLabel>();
		this.previousFIDS = new List<float>();
		this.typesFIDS = new List<StaticString>();
		this.typesFIDS.Add(SimulationProperties.NetCityGrowth);
		this.typesFIDS.Add(SimulationProperties.NetCityProduction);
		this.typesFIDS.Add(SimulationProperties.NetCityResearch);
		this.typesFIDS.Add(SimulationProperties.NetCityMoney);
		this.typesFIDS.Add(SimulationProperties.NetCityEmpirePoint);
		this.workerTypes = new List<StaticString>();
		this.workerTypes.Add(SimulationProperties.FoodPopulation);
		this.workerTypes.Add(SimulationProperties.IndustryPopulation);
		this.workerTypes.Add(SimulationProperties.SciencePopulation);
		this.workerTypes.Add(SimulationProperties.DustPopulation);
		this.workerTypes.Add(SimulationProperties.CityPointPopulation);
		for (int i = 0; i < this.typesFIDS.Count; i++)
		{
			GuiElement guiElement;
			base.GuiService.GuiPanelHelper.TryGetGuiElement(this.typesFIDS[i], out guiElement);
			if (guiElement != null)
			{
				ExtendedGuiElement extendedGuiElement = (ExtendedGuiElement)guiElement;
				if (i < this.TotalValuesTable.GetChildren().Count)
				{
					AgeTransform element = this.TotalValuesTable.GetChildren()[i];
					for (int j = 0; j < element.GetChildren().Count; j++)
					{
						AgeTransform child = element.GetChildren()[j];
						if (child.name == "1Symbol")
						{
							Texture2D texture;
							if (base.GuiService.GuiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Small, out texture))
							{
								child.GetComponent<AgePrimitiveImage>().Image = texture;
								child.GetComponent<AgePrimitiveImage>().TintColor = extendedGuiElement.Color;
							}
						}
						else if (child.name == "2Value")
						{
							this.valuesFIDS.Add(child.GetComponent<AgePrimitiveLabel>());
							child.GetComponent<AgePrimitiveLabel>().TintColor = extendedGuiElement.Color;
						}
						else if (child.name == "3Modifier")
						{
							this.deltaFIDS.Add(child.GetComponent<AgePrimitiveLabel>());
						}
						else if (child.name == "5PopulationValue")
						{
							this.workersFIDS.Add(child.GetComponent<AgePrimitiveLabel>());
						}
					}
				}
			}
			this.previousFIDS.Add(0f);
		}
		Diagnostics.Assert(this.typesFIDS.Count == this.valuesFIDS.Count, "CITY INFO PANEL : Invalid number of value FIDS");
		Diagnostics.Assert(this.deltaFIDS.Count == this.valuesFIDS.Count, "CITY INFO PANEL : Invalid number of delta FIDS");
		this.CityWorkersPanel.Load();
		base.GuiService.GetGuiPanel<CityManagementPanel>().Load();
		for (int k = 0; k < this.valuesFIDS.Count; k++)
		{
			if (this.valuesFIDS[k].AgeTransform.AgeTooltip == null)
			{
				break;
			}
			this.valuesFIDS[k].AgeTransform.AgeTooltip.Class = "FIDS";
			this.valuesFIDS[k].AgeTransform.AgeTooltip.Content = this.typesFIDS[k];
			this.valuesFIDS[k].AgeTransform.AgeTooltip.ClientData = new SimulationPropertyTooltipData(this.typesFIDS[k], GuiSimulation.Instance.FIMSTooltipTotal[k], this.City);
		}
		if (this.ApprovalGaugeTooltip.AgeTooltip != null)
		{
			this.ApprovalGaugeTooltip.AgeTooltip.Class = "FIDS";
			this.ApprovalGaugeTooltip.AgeTooltip.Content = "none";
			this.ApprovalGaugeTooltip.AgeTooltip.ClientData = new SimulationPropertyTooltipData(SimulationProperties.NetCityApproval, string.Format("{0},{1},!{2}", SimulationProperties.NetCityApproval, SimulationProperties.CityApproval, SimulationProperties.CityApprovalUpkeep), this.City);
		}
		if (this.ApprovalState.AgeTransform.AgeTooltip != null)
		{
			this.ApprovalState.AgeTransform.AgeTooltip.Class = "Descriptor";
			this.ApprovalState.AgeTransform.AgeTooltip.ClientData = this.City;
		}
		if (this.GaugePopulationTooltip.AgeTooltip != null)
		{
			this.GaugePopulationTooltip.AgeTooltip.Class = "CityGrowth";
			this.GaugePopulationTooltip.AgeTooltip.Content = "none";
			this.GaugePopulationTooltip.AgeTooltip.ClientData = new SimulationPropertyTooltipData(SimulationProperties.NetCityGrowth, string.Format("{0},{1},!{2}", SimulationProperties.WorkedFood, SimulationProperties.CityGrowth, SimulationProperties.CityGrowthUpkeep), this.City);
		}
		if (this.PopulationCountGroup != null && this.PopulationCountGroup.AgeTooltip != null)
		{
			this.PopulationCountGroup.AgeTooltip.Class = "Population";
			this.PopulationCountGroup.AgeTooltip.Content = "Population";
			this.PopulationCountGroup.AgeTooltip.ClientData = new SimulationPropertyTooltipData(SimulationProperties.Workers, SimulationProperties.Workers, this.City);
		}
		yield break;
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		this.playerControllerRepository = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
		Diagnostics.Assert(this.playerControllerRepository != null);
		this.EndTurnService = Services.GetService<IEndTurnService>();
		this.firstShow = true;
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		CityInfoPanel.interactionsAllowed = this.playerControllerRepository.ActivePlayerController.CanSendOrders();
		this.CityWorkersPanel.Hide(true);
		this.BackgroundPopulation.Visible = true;
		this.TotalValuesTable.Visible = true;
		if (this.firstShow)
		{
			this.PopulationControlToggle.State = true;
			this.firstShow = false;
		}
		if (this.PopulationControlToggle.State)
		{
			this.OnPopulationControlCB(this.PopulationControlToggle.gameObject);
		}
		yield break;
	}

	protected override void OnUnload()
	{
		this.CityWorkersPanel.Unload();
		for (int i = 0; i < this.valuesFIDS.Count; i++)
		{
			if (this.valuesFIDS[i].AgeTransform.AgeTooltip == null)
			{
				break;
			}
			this.valuesFIDS[i].AgeTransform.AgeTooltip.ClientData = null;
		}
		this.previousFIDS = null;
		this.valuesFIDS = null;
		this.deltaFIDS = null;
		this.workersFIDS = null;
		this.typesFIDS = null;
		if (this.ApprovalGaugeTooltip.AgeTooltip != null)
		{
			this.ApprovalGaugeTooltip.AgeTooltip.ClientData = null;
		}
		if (this.ApprovalState.AgeTransform.AgeTooltip != null)
		{
			this.ApprovalState.AgeTransform.AgeTooltip.ClientData = null;
		}
		if (this.GaugePopulationTooltip.AgeTooltip != null)
		{
			this.GaugePopulationTooltip.AgeTooltip.ClientData = null;
		}
		if (this.PopulationCountGroup != null && this.PopulationCountGroup.AgeTooltip != null)
		{
			this.PopulationCountGroup.AgeTooltip.ClientData = null;
		}
		base.OnUnload();
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.Hide(true);
		this.Unbind();
		this.EndTurnService = null;
		this.playerControllerRepository = null;
		base.OnUnloadGame(game);
	}

	private void City_Refreshed(object sender)
	{
		base.NeedRefresh = true;
	}

	private void EndTurnService_GameClientStateChange(object sender, GameClientStateChangeEventArgs e)
	{
		if (base.IsVisible && this.playerControllerRepository != null && this.playerControllerRepository.ActivePlayerController != null)
		{
			bool flag = this.playerControllerRepository.ActivePlayerController.CanSendOrders();
			if (CityInfoPanel.interactionsAllowed != flag)
			{
				CityInfoPanel.interactionsAllowed = flag;
				if (this.City != null)
				{
					this.RefreshButtons();
				}
				if (!CityInfoPanel.interactionsAllowed && MessagePanel.Instance.IsVisible)
				{
					MessagePanel.Instance.Hide(false);
				}
			}
		}
	}

	private void DepartmentOfScience_TechnologyUnlocked(object sender, ConstructibleElementEventArgs e)
	{
		if (base.IsVisible && this.City != null && e.ConstructibleElement.Name == "TechnologyDefinitionNecrophages8")
		{
			CityInfoPanel.ShowHidePopulationSacrificeButton(this.playerControllerRepository.ActivePlayerController.Empire, this.DepartmentOfScience, this.PopulationSacrificeButton);
			CityInfoPanel.RefreshPopulationSacrificeButton(this.playerControllerRepository.ActivePlayerController.Empire, this.City, this.PopulationSacrificeButton);
		}
	}

	private void DepartmentOfTheTreasury_ResourcePropertyChange(object sender, ResourcePropertyChangeEventArgs e)
	{
		if (base.IsVisible && this.City != null && e.ResourcePropertyName == SimulationProperties.BankAccount && this.PopulationBuyoutButton.AgeTransform.Visible)
		{
			CityInfoPanel.RefreshPopulationBuyoutButton(this.playerControllerRepository.ActivePlayerController.Empire, this.City, this.PopulationBuyoutButton);
		}
	}

	private void OnBrowseCB(GameObject obj)
	{
		if (base.GuiService.GetGuiPanel<CityManagementPanel>().IsVisible)
		{
			base.GuiService.GetGuiPanel<CityManagementPanel>().Hide(false);
		}
		else
		{
			base.GuiService.GetGuiPanel<CityManagementPanel>().RefreshContent();
			base.GuiService.GetGuiPanel<CityManagementPanel>().Show(new object[0]);
		}
	}

	private void OnPopulationSacrificeCB(GameObject obj)
	{
		OrderSacrificePopulation order = new OrderSacrificePopulation(this.City.Empire.Index, this.City.GUID);
		this.playerControllerRepository.ActivePlayerController.PostOrder(order);
	}

	private void OnPopulationBuyoutCB(GameObject obj)
	{
		OrderBuyOutPopulation order = new OrderBuyOutPopulation(this.City.Empire.Index, this.City.GUID);
		this.playerControllerRepository.ActivePlayerController.PostOrder(order);
	}

	private void OnPopulationControlCB(GameObject obj)
	{
		AgeControlToggle component = obj.GetComponent<AgeControlToggle>();
		if (component.State)
		{
			if (!this.CityWorkersPanel.IsVisible)
			{
				this.CityWorkersPanel.Show(new object[0]);
			}
			else
			{
				this.CityWorkersPanel.RefreshContent();
			}
		}
		else if (this.CityWorkersPanel.IsVisible)
		{
			this.CityWorkersPanel.Hide(false);
		}
		this.BackgroundPopulation.Visible = !component.State;
		this.TotalValuesTable.Visible = !component.State;
	}

	private void AnimateFIDSChange(AgePrimitiveLabel control, AgePrimitiveLabel modifier, float delta)
	{
		modifier.Text = GuiFormater.FormatGui(delta, false, true, true, 1);
		modifier.TintColor = ((delta < 0f) ? Color.red : Color.green);
		control.AgeTransform.StartAllModifiers(true, false);
		modifier.AgeTransform.StartAllModifiers(true, false);
	}

	private void RefreshPopulationCount()
	{
		float propertyValue = this.City.GetPropertyValue(SimulationProperties.Population);
		float propertyValue2 = this.City.GetPropertyValue(SimulationProperties.PopulationBonus);
		this.PopulationCount.Text = GuiFormater.FormatGui(propertyValue, false, true, false, 1) + " + " + GuiFormater.FormatGui(propertyValue2, false, true, false, 1);
	}

	private void RefreshPopulationGrowth()
	{
		float propertyValue = this.City.GetPropertyValue(SimulationProperties.Population);
		float propertyValue2 = this.City.GetPropertyValue(SimulationProperties.CityGrowthStock);
		float propertyValue3 = this.City.GetPropertyValue(SimulationProperties.NetCityGrowth);
		DepartmentOfTheInterior agency = this.City.Empire.GetAgency<DepartmentOfTheInterior>();
		float num;
		float num2;
		agency.GetGrowthLimits(propertyValue, out num, out num2);
		this.GaugePopulation.PercentRight = Mathf.Clamp((propertyValue2 - num) / (num2 - num) * 100f, 0f, 100f);
		float num3 = Mathf.Round(100f * propertyValue3 / (num2 - num));
		if (num3 > 0f)
		{
			this.GaugeProgress.Visible = true;
			this.GaugeProgress.GetComponent<AgePrimitiveImage>().TintColor = this.GrowingPopulationColor;
			this.GaugeProgress.PercentLeft = this.GaugePopulation.PercentRight;
			this.GaugeProgress.PercentRight = Mathf.Min(100f, this.GaugeProgress.PercentLeft + num3);
			this.NextPopulationTurns.Text = GuiFormater.FormatGui((float)Mathf.Max(1, Mathf.CeilToInt((num2 - propertyValue2) / propertyValue3)), false, true, false, 1);
		}
		else if (num3 < 0f)
		{
			this.GaugeProgress.Visible = true;
			this.GaugeProgress.GetComponent<AgePrimitiveImage>().TintColor = this.LosingPopulationColor;
			this.GaugeProgress.PercentRight = this.GaugePopulation.PercentRight;
			this.GaugeProgress.PercentLeft = Mathf.Max(0f, this.GaugeProgress.PercentRight + num3);
			this.NextPopulationTurns.Text = GuiFormater.FormatGui((float)Mathf.Max(1, Mathf.CeilToInt((propertyValue2 - num) / -propertyValue3)), false, true, false, 1);
		}
		else
		{
			this.GaugeProgress.Visible = false;
			this.NextPopulationTurns.Text = "-";
		}
	}

	private void RefreshButtons()
	{
		if (this.PopulationBuyoutButton.AgeTransform.Visible)
		{
			CityInfoPanel.RefreshPopulationBuyoutButton(this.playerControllerRepository.ActivePlayerController.Empire, this.City, this.PopulationBuyoutButton);
		}
		if (this.PopulationSacrificeButton.AgeTransform.Visible)
		{
			CityInfoPanel.RefreshPopulationSacrificeButton(this.playerControllerRepository.ActivePlayerController.Empire, this.City, this.PopulationSacrificeButton);
		}
	}

	public AgeTransform BackgroundPopulation;

	public CityWorkersPanel CityWorkersPanel;

	public AgeControlToggle PopulationControlToggle;

	public AgePrimitiveLabel ApprovalState;

	public AgeTransform ApprovalGauge;

	public AgeTransform ApprovalGaugeTooltip;

	public AgePrimitiveLabel ApprovalValue;

	public AgeTransform PopulationCountGroup;

	public AgePrimitiveLabel PopulationCount;

	public AgeTransform GaugePopulationBackground;

	public AgeTransform GaugePopulation;

	public AgeTransform GaugeProgress;

	public AgeTransform NextPopulationHourglass;

	public AgePrimitiveLabel NextPopulationTurns;

	public AgeControlButton PopulationBuyoutButton;

	public AgeControlButton PopulationSacrificeButton;

	public AgeTransform GaugePopulationTooltip;

	public AgeTransform UpkeepIcon1;

	public AgeTransform UpkeepIcon2;

	public AgePrimitiveLabel UpkeepValue;

	public AgeTransform TotalValuesTable;

	public Color GrowingPopulationColor;

	public Color LosingPopulationColor;

	public float WorkersPanelFadeDuration = 0.5f;

	private static bool interactionsAllowed = true;

	private List<StaticString> typesFIDS;

	private List<StaticString> workerTypes;

	private List<AgePrimitiveLabel> valuesFIDS;

	private List<AgePrimitiveLabel> deltaFIDS;

	private List<AgePrimitiveLabel> workersFIDS;

	private List<float> previousFIDS;

	private DepartmentOfScience departmentOfScience;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private IPlayerControllerRepositoryService playerControllerRepository;

	private IEndTurnService endTurnService;

	private bool firstShow;

	private City city;
}
