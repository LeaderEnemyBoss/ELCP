using System;
using System.Collections;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Simulation;
using UnityEngine;

public class PanelFeatureNodeCost : GuiPanelFeature
{
	protected override IEnumerator OnLoadGame()
	{
		this.guiService = Services.GetService<Amplitude.Unity.Gui.IGuiService>();
		this.simulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
		return base.OnLoadGame();
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.guiService = null;
		base.OnUnloadGame(game);
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		ConstructibleTooltipData constructibleTooltipData = this.context as ConstructibleTooltipData;
		if (constructibleTooltipData != null && constructibleTooltipData.Empire != null && constructibleTooltipData.Constructible != null)
		{
			DepartmentOfTheTreasury agency = constructibleTooltipData.Empire.GetAgency<DepartmentOfTheTreasury>();
			DepartmentOfTheInterior agency2 = constructibleTooltipData.Empire.GetAgency<DepartmentOfTheInterior>();
			string text = string.Empty;
			if (agency2.MainCity != null)
			{
				int num;
				PanelFeatureCost.ComputeCostAndTurn(this.guiService, constructibleTooltipData.Constructible, agency, agency2.MainCity, out text, out num);
			}
			else
			{
				int num2;
				PanelFeatureCost.ComputeCostAndTurn(this.guiService, constructibleTooltipData.Constructible, agency, constructibleTooltipData.Empire, out text, out num2);
			}
			text = ((!(text == "-")) ? text : string.Empty);
			CreepingNodeImprovementDefinition creepingNodeImprovementDefinition = constructibleTooltipData.Constructible as CreepingNodeImprovementDefinition;
			SimulationObject simulationObject = new SimulationObject("DummyNode");
			SimulationDescriptor descriptor = null;
			if (this.simulationDescriptorDatabase.TryGetValue("ClassCreepingNode", out descriptor))
			{
				simulationObject.AddDescriptor(descriptor);
			}
			else
			{
				Diagnostics.LogError("Could not find the class creeping node descriptor");
			}
			float propertyBaseValue = simulationObject.GetPropertyBaseValue(creepingNodeImprovementDefinition.BaseCostPropertyName);
			float num3 = simulationObject.GetPropertyBaseValue(SimulationProperties.NodeCostIncrement);
			float propertyValue = constructibleTooltipData.Empire.GetPropertyValue(SimulationProperties.NodeCostIncrementModifier);
			num3 *= propertyValue;
			if (creepingNodeImprovementDefinition.SubCategory == "SubCategoryVillage")
			{
				num3 *= constructibleTooltipData.Empire.GetPropertyValue(SimulationProperties.NodeOvergrownVillageCostModifier);
			}
			float propertyValue2 = constructibleTooltipData.Empire.GetPropertyValue(SimulationProperties.NumberOfCreepingNodes);
			float propertyValue3 = constructibleTooltipData.Empire.GetPropertyValue(SimulationProperties.NumberOfFinishedCreepingNodes);
			int num4 = Mathf.CeilToInt(propertyBaseValue + num3 * (2f * propertyValue2 - propertyValue3 + 1f));
			float propertyValue4 = constructibleTooltipData.Empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier);
			int num5 = (int)Math.Max(0.0, Math.Ceiling((double)((float)creepingNodeImprovementDefinition.ConstructionTurns * propertyValue4)));
			string str = string.Format(AgeLocalizer.Instance.LocalizeString("%FeaturePanelNodeCost"), num4.ToString(), num5.ToString());
			text = text + " " + str;
			bool flag = this.CanAfforFoodCost(constructibleTooltipData.Empire, creepingNodeImprovementDefinition);
			if (!string.IsNullOrEmpty(text))
			{
				this.CostValue.Text = text;
				this.CostValue.TintColor = ((!flag) ? this.CantAffordColor : this.CanAffordColor);
				if (this.CostValue.AgeTransform.PixelMarginTop == this.CostTitle.AgeTransform.PixelMarginTop)
				{
					this.CostValue.AgeTransform.PixelMarginLeft = 2f * this.CostTitle.AgeTransform.PixelMarginLeft + this.CostTitle.Font.ComputeTextWidth(AgeLocalizer.Instance.LocalizeString(this.CostTitle.Text), this.CostTitle.ForceCaps, false);
				}
			}
		}
		yield return base.OnShow(parameters);
		yield break;
	}

	private bool CanAfforFoodCost(global::Empire empire, CreepingNodeImprovementDefinition nodeDefinition)
	{
		DepartmentOfTheInterior agency = empire.GetAgency<DepartmentOfTheInterior>();
		SimulationObject simulationObject = new SimulationObject("DummyNode");
		SimulationDescriptor descriptor = null;
		SimulationDescriptor descriptor2 = null;
		if (this.simulationDescriptorDatabase.TryGetValue("ClassCreepingNode", out descriptor2))
		{
			simulationObject.AddDescriptor(descriptor2);
			if (this.simulationDescriptorDatabase.TryGetValue(nodeDefinition.ConstructionCostDescriptor, out descriptor))
			{
				simulationObject.AddDescriptor(descriptor);
			}
			agency.MainCity.SimulationObject.AddChild(simulationObject);
			agency.MainCity.SimulationObject.Refresh();
			float propertyValue = agency.MainCity.GetPropertyValue("NetCityGrowth");
			agency.MainCity.SimulationObject.RemoveChild(simulationObject);
			agency.MainCity.SimulationObject.Refresh();
			return propertyValue >= 0f;
		}
		Diagnostics.LogError("Could not find the class creeping node descriptor");
		return false;
	}

	public AgePrimitiveLabel CostTitle;

	public AgePrimitiveLabel CostValue;

	public AgePrimitiveLabel TurnValue;

	public AgePrimitiveImage TurnIcon;

	public Color CantAffordColor = Color.red;

	public Color CanAffordColor = new Color(255f, 252f, 179f);

	private Amplitude.Unity.Gui.IGuiService guiService;

	private IDatabase<SimulationDescriptor> simulationDescriptorDatabase;
}
