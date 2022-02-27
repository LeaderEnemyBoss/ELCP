using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.AI.Amas;
using Amplitude.Unity.AI.Amas.Simulation;
using Amplitude.Unity.Simulation;
using UnityEngine;

[PersonalityRegistryPath("AI/AgentDefinition/IndustryReferenceTurnCountAgent/", new object[]
{

})]
public class IndustryReferenceTurnCountAgent : SimulationAgent
{
	public override void Initialize(AgentDefinition agentDefinition, AgentGroup parentGroup)
	{
		base.Initialize(agentDefinition, parentGroup);
		City city = base.ContextObject as City;
		if (city == null)
		{
			Diagnostics.LogError("The agent context object is not a city");
			return;
		}
		Diagnostics.Assert(city.Empire != null);
		this.departmentOfTheTreasury = city.Empire.GetAgency<DepartmentOfTheTreasury>();
		Diagnostics.Assert(this.departmentOfTheTreasury != null);
		DepartmentOfIndustry agency = city.Empire.GetAgency<DepartmentOfIndustry>();
		Diagnostics.Assert(agency != null);
		this.aiPlayer = (base.ParentGroup.Parent.ContextObject as AIPlayer_MajorEmpire);
		if (this.aiPlayer == null)
		{
			Diagnostics.LogError("The agent context object is not an ai player.");
			return;
		}
		this.constructionQueue = agency.GetConstructionQueue(city);
		Diagnostics.Assert(this.constructionQueue != null);
		InfluencedByPersonalityAttribute.LoadFieldAndPropertyValues(city.Empire, this);
	}

	public override void Release()
	{
		this.departmentOfTheTreasury = null;
		this.industryPopulationAgent = null;
		this.constructionQueue = null;
		this.aiPlayer = null;
		base.Release();
	}

	public override void Reset()
	{
		base.Reset();
		if (!base.Enable)
		{
			return;
		}
		if (this.aiPlayer == null)
		{
			base.Enable = false;
			return;
		}
		if (this.constructionQueue == null)
		{
			Diagnostics.LogError("The agent {0} can't get the city construction queue.", new object[]
			{
				base.Name
			});
			base.Enable = false;
			return;
		}
		Diagnostics.Assert(base.ParentGroup != null);
		this.industryPopulationAgent = (base.ParentGroup.GetAgent("IndustryPopulation") as SimulationNormalizedAgent);
		Diagnostics.Assert(this.industryPopulationAgent != null);
	}

	protected override void ComputeInitValue()
	{
		base.ComputeInitValue();
		Diagnostics.Assert(this.contextSimulationObject != null);
		float propertyValue = this.contextSimulationObject.GetPropertyValue(SimulationProperties.NetCityProduction);
		Diagnostics.Assert(this.constructionQueue != null);
		Construction construction = this.constructionQueue.Peek();
		ConstructibleElement constructibleElement = (construction == null) ? null : construction.ConstructibleElement;
		this.currentCostWithReduction = ((constructibleElement == null) ? 0f : DepartmentOfTheTreasury.GetProductionCostWithBonus(this.contextSimulationObject, constructibleElement, DepartmentOfTheTreasury.Resources.Production));
		this.currentProductionStress = 1f;
		UnitDesign unitDesign = constructibleElement as UnitDesign;
		if (unitDesign != null)
		{
			this.currentProductionStress = 0f;
			bool flag = false;
			IEnumerable<EvaluableMessageWithUnitDesign> messages = this.aiPlayer.Blackboard.GetMessages<EvaluableMessageWithUnitDesign>(BlackboardLayerID.Empire, (EvaluableMessageWithUnitDesign match) => match.UnitDesign != null && match.UnitDesign.Model == unitDesign.Model);
			foreach (EvaluableMessageWithUnitDesign evaluableMessageWithUnitDesign in messages)
			{
				if (float.IsNaN(evaluableMessageWithUnitDesign.Interest))
				{
					Diagnostics.LogWarning("Unit request interest is at value NaN, it will be skipped.");
				}
				else
				{
					this.currentProductionStress = Mathf.Max(this.currentProductionStress, evaluableMessageWithUnitDesign.Interest);
					flag = true;
				}
			}
			if (!flag)
			{
				this.currentProductionStress = 1f;
			}
		}
		this.currentProductionStress = Mathf.Clamp01(this.currentProductionStress);
		float num = this.currentCostWithReduction * this.currentProductionStress;
		float num2 = (propertyValue <= 0f) ? this.maximumTurnCount : (num / propertyValue);
		base.ValueInit = ((this.maximumTurnCount <= 0f) ? base.ValueMax : Mathf.Clamp(Mathf.Max(num2 - 1f, 0f) / Mathf.Max(1f, this.maximumTurnCount - 1f), base.ValueMin, base.ValueMax));
	}

	protected override void ComputeValue()
	{
		base.ComputeValue();
		if (this.contextSimulationObject.Children.Exists((SimulationObject C) => C.Tags.Contains("BoosterDecreaseCityProduction4") || C.Tags.Contains("BoosterDecreaseCityProduction3") || C.Tags.Contains("BoosterDecreaseCityProduction2")))
		{
			base.Value = base.ValueMin;
			return;
		}
		Diagnostics.Assert(this.industryPopulationAgent != null);
		float unnormalizedValue = this.industryPopulationAgent.UnnormalizedValue;
		float propertyValue = this.contextSimulationObject.GetPropertyValue(SimulationProperties.IndustryPopulation);
		Diagnostics.Assert(this.contextSimulationObject != null);
		float num = unnormalizedValue - propertyValue;
		float propertyValue2 = this.contextSimulationObject.GetPropertyValue(SimulationProperties.BaseIndustryPerPopulation);
		float num2 = this.contextSimulationObject.GetPropertyValue(SimulationProperties.NetCityProduction) + num * propertyValue2;
		float num3 = this.currentCostWithReduction * Mathf.Max(this.currentProductionStress, this.productionStressMinimumValue);
		float num4 = (num2 <= 0f) ? this.maximumTurnCount : (num3 / num2);
		base.Value = ((this.maximumTurnCount <= 0f) ? base.ValueMax : Mathf.Clamp(Mathf.Max(num4 - 1f, 0f) / Mathf.Max(1f, this.maximumTurnCount - 1f), base.ValueMin, base.ValueMax));
	}

	private AIPlayer_MajorEmpire aiPlayer;

	private ConstructionQueue constructionQueue;

	private float currentCostWithReduction;

	private float currentProductionStress;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private SimulationNormalizedAgent industryPopulationAgent;

	[InfluencedByPersonality]
	private float maximumTurnCount = 15f;

	[InfluencedByPersonality]
	private float productionStressMinimumValue = 0.5f;
}
