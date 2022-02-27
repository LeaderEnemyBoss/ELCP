using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Gui.SimulationEffect;
using Amplitude.Unity.Simulation;
using UnityEngine;

public class MultipleConstructibleItem : Amplitude.Unity.Framework.Behaviour
{
	public AgeTransform AgeTransform { get; private set; }

	public void Initialize(Amplitude.Unity.Gui.IGuiService guiService, IGuiSimulationParser simulationEffectParser)
	{
		this.guiService = guiService;
		this.simulationEffectParser = simulationEffectParser;
		this.EffectMapper.Initialize(guiService, true);
		this.AgeTransform = base.GetComponent<AgeTransform>();
	}

	public void Show(ConstructibleTooltipData constructibleData)
	{
		this.ConstructibleTitle.Text = AgeLocalizer.Instance.LocalizeString(constructibleData.Title);
		this.effectDescriptions.Clear();
		this.simulationEffectParser.ParseSimulationDescriptor(constructibleData, this.effectDescriptions, null, false, false);
		this.EffectMapper.LoadEffects(this.effectDescriptions, true);
		if (this.EffectMapper.EffectsList.Height == 0f)
		{
			this.EffectTitle.Text = AgeLocalizer.Instance.LocalizeString("%FeatureNoEffectsTitle");
		}
		else
		{
			this.EffectTitle.Text = AgeLocalizer.Instance.LocalizeString("%FeatureEffectsTitle");
		}
		this.EffectGroup.Height = this.EffectMapper.EffectsList.PixelOffsetTop + this.EffectMapper.EffectsList.PixelMarginTop + this.EffectMapper.EffectsList.Height + this.EffectMapper.EffectsList.PixelMarginBottom + this.EffectMapper.EffectsList.PixelOffsetBottom;
		this.CostGroup.Visible = true;
		this.CostGroup.Y = this.ConstructibleTitle.AgeTransform.Height + this.EffectGroup.Height;
		this.AgeTransform.Height = this.CostGroup.Y;
		if (constructibleData != null && ((ICostFeatureProvider)constructibleData).Constructible != null && ((ICostFeatureProvider)constructibleData).Empire != null)
		{
			DepartmentOfTheTreasury agency = ((ICostFeatureProvider)constructibleData).Empire.GetAgency<DepartmentOfTheTreasury>();
			SimulationObjectWrapper context = (((ICostFeatureProvider)constructibleData).Context == null) ? ((ICostFeatureProvider)constructibleData).Empire : ((ICostFeatureProvider)constructibleData).Context;
			string text = string.Empty;
			int num;
			PanelFeatureCost.ComputeCostAndTurn(this.guiService, ((ICostFeatureProvider)constructibleData).Constructible, agency, context, out text, out num);
			this.TurnIcon.AgeTransform.Visible = false;
			this.TurnValue.AgeTransform.Visible = false;
			if (((ICostFeatureProvider)constructibleData).Constructible is CreepingNodeImprovementDefinition)
			{
				IDatabase<SimulationDescriptor> database = Databases.GetDatabase<SimulationDescriptor>(false);
				text = ((!(text == "-")) ? text : string.Empty);
				CreepingNodeImprovementDefinition creepingNodeImprovementDefinition = ((ICostFeatureProvider)constructibleData).Constructible as CreepingNodeImprovementDefinition;
				SimulationObject simulationObject = new SimulationObject("DummyNode");
				SimulationDescriptor descriptor = null;
				if (database.TryGetValue("ClassCreepingNode", out descriptor))
				{
					simulationObject.AddDescriptor(descriptor);
				}
				else
				{
					Diagnostics.LogError("Could not find the class creeping node descriptor");
				}
				float propertyBaseValue = simulationObject.GetPropertyBaseValue(creepingNodeImprovementDefinition.BaseCostPropertyName);
				float num2 = simulationObject.GetPropertyBaseValue(SimulationProperties.NodeCostIncrement);
				float propertyValue = ((ICostFeatureProvider)constructibleData).Empire.GetPropertyValue(SimulationProperties.NodeCostIncrementModifier);
				num2 *= propertyValue;
				if (creepingNodeImprovementDefinition.SubCategory == "SubCategoryVillage")
				{
					num2 *= ((ICostFeatureProvider)constructibleData).Empire.GetPropertyValue(SimulationProperties.NodeOvergrownVillageCostModifier);
				}
				float propertyValue2 = ((ICostFeatureProvider)constructibleData).Empire.GetPropertyValue(SimulationProperties.NumberOfCreepingNodes);
				float propertyValue3 = ((ICostFeatureProvider)constructibleData).Empire.GetPropertyValue(SimulationProperties.NumberOfFinishedCreepingNodes);
				int num3 = Mathf.CeilToInt(propertyBaseValue + num2 * (2f * propertyValue2 - propertyValue3 + 1f));
				float propertyValue4 = ((ICostFeatureProvider)constructibleData).Empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier);
				int num4 = (int)Math.Max(0.0, Math.Ceiling((double)((float)creepingNodeImprovementDefinition.ConstructionTurns * propertyValue4)));
				string str = string.Format(AgeLocalizer.Instance.LocalizeString("%FeaturePanelNodeCost"), num3.ToString(), num4.ToString());
				text = text + " " + str;
			}
			if (!string.IsNullOrEmpty(text))
			{
				this.CostValue.Text = text;
				if (this.CostValue.AgeTransform.PixelMarginTop == this.CostTitle.AgeTransform.PixelMarginTop)
				{
					this.CostValue.AgeTransform.PixelMarginLeft = 2f * this.CostTitle.AgeTransform.PixelMarginLeft + this.CostTitle.Font.ComputeTextWidth(AgeLocalizer.Instance.LocalizeString(this.CostTitle.Text), this.CostTitle.ForceCaps, false);
				}
				this.AgeTransform.Height += this.CostTitle.AgeTransform.Height;
				return;
			}
		}
		else
		{
			this.CostGroup.Visible = false;
		}
	}

	public AgePrimitiveLabel EffectTitle;

	public GuiEffectMapper EffectMapper;

	public AgePrimitiveLabel ConstructibleTitle;

	public AgeTransform EffectGroup;

	public AgeTransform CostGroup;

	public AgePrimitiveLabel CostTitle;

	public AgePrimitiveLabel CostValue;

	public AgePrimitiveLabel TurnValue;

	public AgePrimitiveImage TurnIcon;

	protected IGuiSimulationParser simulationEffectParser;

	private List<EffectDescription> effectDescriptions = new List<EffectDescription>();

	private Amplitude.Unity.Gui.IGuiService guiService;
}
