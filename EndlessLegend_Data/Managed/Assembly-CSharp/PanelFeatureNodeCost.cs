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
		ConstructibleTooltipData data = this.context as ConstructibleTooltipData;
		if (data != null && data.Empire != null && data.Constructible != null)
		{
			DepartmentOfTheTreasury departmentOfTheTreasury = data.Empire.GetAgency<DepartmentOfTheTreasury>();
			string costString = string.Empty;
			int turn;
			PanelFeatureCost.ComputeCostAndTurn(this.guiService, data.Constructible, departmentOfTheTreasury, data.Empire, out costString, out turn);
			costString = ((!(costString == "-")) ? costString : string.Empty);
			CreepingNodeImprovementDefinition definition = data.Constructible as CreepingNodeImprovementDefinition;
			int foodCost = 0;
			SimulationObject dummyNode = new SimulationObject("DummyNode");
			SimulationDescriptor classNodeDescriptor = null;
			if (this.simulationDescriptorDatabase.TryGetValue("ClassCreepingNode", out classNodeDescriptor))
			{
				dummyNode.AddDescriptor(classNodeDescriptor);
			}
			else
			{
				Diagnostics.LogError("Could not find the class creeping node descriptor");
			}
			float nodeBaseCost = dummyNode.GetPropertyBaseValue(definition.BaseCostPropertyName);
			float costIncrement = dummyNode.GetPropertyBaseValue(SimulationProperties.NodeCostIncrement);
			float numberOfNodes = data.Empire.GetPropertyValue(SimulationProperties.NumberOfCreepingNodes);
			float numberOfFinishedNodes = data.Empire.GetPropertyValue(SimulationProperties.NumberOfFinishedCreepingNodes);
			foodCost = (int)(nodeBaseCost + costIncrement * (2f * numberOfNodes - numberOfFinishedNodes + 1f));
			float gameSpeed = data.Empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier);
			int turns = (int)Math.Max(0.0, Math.Ceiling((double)((float)definition.ConstructionTurns * gameSpeed)));
			string foodCostLoc = string.Format(AgeLocalizer.Instance.LocalizeString("%FeaturePanelNodeCost"), foodCost.ToString(), turns.ToString());
			costString = costString + " " + foodCostLoc;
			bool canAffordFood = this.CanAfforFoodCost(data.Empire, definition);
			if (!string.IsNullOrEmpty(costString))
			{
				this.CostValue.Text = costString;
				this.CostValue.TintColor = ((!canAffordFood) ? this.CantAffordColor : this.CanAffordColor);
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
