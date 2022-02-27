using System;
using Amplitude;
using Amplitude.Unity.AI.Amas;
using Amplitude.Unity.AI.Amas.Simulation;
using UnityEngine;

[PersonalityRegistryPath("AI/AgentDefinition/MoneyReferenceRatioAgent/", new object[]
{

})]
public class MoneyReferenceRatioAgent : SimulationAgent
{
	public override void Initialize(AgentDefinition agentDefinition, AgentGroup parentGroup)
	{
		base.Initialize(agentDefinition, parentGroup);
		AIPlayer_MajorEmpire aiPlayer = base.ContextObject as AIPlayer_MajorEmpire;
		if (aiPlayer == null)
		{
			Diagnostics.LogError("The agent context object is not an ai player.");
			return;
		}
		this.empire = aiPlayer.MajorEmpire;
		if (this.empire == null)
		{
			Diagnostics.LogError("Can't retrieve the empire.");
			return;
		}
		Diagnostics.Assert(this.empire != null);
		this.departmentOfTheInterior = this.empire.GetAgency<DepartmentOfTheInterior>();
		Diagnostics.Assert(this.departmentOfTheInterior != null);
		this.departmentOfTheTreasury = this.empire.GetAgency<DepartmentOfTheTreasury>();
		Diagnostics.Assert(this.departmentOfTheTreasury != null);
		DepartmentOfIndustry agency = this.empire.GetAgency<DepartmentOfIndustry>();
		Diagnostics.Assert(agency != null);
		DepartmentOfIndustry.ConstructibleElement[] availableConstructibleElements = ((IConstructibleElementDatabase)agency).GetAvailableConstructibleElements(new StaticString[]
		{
			DistrictImprovementDefinition.ReadOnlyCategory
		});
		Diagnostics.Assert(availableConstructibleElements != null);
		this.districtImprovement = Array.Find<DepartmentOfIndustry.ConstructibleElement>(availableConstructibleElements, (DepartmentOfIndustry.ConstructibleElement element) => element.Name == aiPlayer.AIData_Faction.DistrictImprovement);
		Diagnostics.Assert(this.districtImprovement != null && this.districtImprovement.Costs != null);
		InfluencedByPersonalityAttribute.LoadFieldAndPropertyValues(this.empire, this);
	}

	public override void Release()
	{
		this.empire = null;
		this.netCityMoneyAgents = null;
		this.departmentOfTheTreasury = null;
		this.departmentOfTheInterior = null;
		this.contextSimulationObject = null;
		this.districtImprovement = null;
		this.aILayer_Victory = null;
		base.Release();
	}

	public override void Reset()
	{
		base.Reset();
		if (!base.Enable)
		{
			return;
		}
		if (this.departmentOfTheInterior == null)
		{
			Diagnostics.LogError("The agent {0} can't get the department of interior.", new object[]
			{
				base.Name
			});
			base.Enable = false;
			return;
		}
		Agent[] validatedAgents = new AgentGroupPath("ResourceEvaluationAmas/CityAgentGroup").GetValidatedAgents(base.ParentGroup, "NetCityMoney");
		SimulationNormalizedAgent[] array;
		if (validatedAgents != null)
		{
			array = Array.ConvertAll<Agent, SimulationNormalizedAgent>(validatedAgents, (Agent agent) => agent as SimulationNormalizedAgent);
		}
		else
		{
			array = new SimulationNormalizedAgent[0];
		}
		this.netCityMoneyAgents = array;
		Diagnostics.Assert(this.netCityMoneyAgents != null);
		AIEntity_Empire entity = (base.ContextObject as AIPlayer_MajorEmpire).GetEntity<AIEntity_Empire>();
		this.aILayer_Victory = entity.GetLayer<AILayer_Victory>();
	}

	protected override void ComputeInitValue()
	{
		base.ComputeInitValue();
		float propertyValue = this.empire.GetPropertyValue(SimulationProperties.NetEmpireMoney);
		float propertyValue2 = this.empire.GetPropertyValue(SimulationProperties.EmpireMoneyUpkeep);
		float propertyValue3 = this.empire.GetPropertyValue(SimulationProperties.BankAccount);
		float b = propertyValue2 * this.moneyStockToMaintainUpkeepTurnCount;
		float a = (propertyValue >= 0f) ? 0f : (-propertyValue * this.moneyStockToMaintainUpkeepTurnCount);
		float num = Mathf.Max(a, b);
		float num2 = this.moneyIncomeGrowthPercent * propertyValue3;
		float num3 = (propertyValue3 <= 0f) ? 0f : (propertyValue3 / num);
		float num4 = (propertyValue <= 0f) ? 0f : (propertyValue / num2);
		float val = (num3 + num4) / 2f;
		base.ValueInit = Math.Max(base.ValueMin, Math.Min(val, base.ValueMax));
	}

	protected override void ComputeValue()
	{
		base.ComputeValue();
		Diagnostics.Assert(this.netCityMoneyAgents != null);
		float num = 0f;
		for (int i = 0; i < this.netCityMoneyAgents.Length; i++)
		{
			SimulationNormalizedAgent simulationNormalizedAgent = this.netCityMoneyAgents[i];
			Diagnostics.Assert(simulationNormalizedAgent != null);
			City city = simulationNormalizedAgent.ContextObject as City;
			Diagnostics.Assert(city != null);
			float propertyValue = city.GetPropertyValue(SimulationProperties.NetCityMoney);
			num += simulationNormalizedAgent.UnnormalizedValue - propertyValue;
		}
		float num2 = this.empire.GetPropertyValue(SimulationProperties.NetEmpireMoney) + num;
		float propertyValue2 = this.empire.GetPropertyValue(SimulationProperties.EmpireMoneyUpkeep);
		float propertyValue3 = this.empire.GetPropertyValue(SimulationProperties.BankAccount);
		float b = propertyValue2 * this.moneyStockToMaintainUpkeepTurnCount;
		float num3 = Mathf.Max((num2 >= 0f) ? 0f : (-num2 * this.moneyStockToMaintainUpkeepTurnCount), b);
		float num4 = this.moneyIncomeGrowthPercent * propertyValue3;
		float num5 = (propertyValue3 <= 0f) ? 0f : (propertyValue3 / num3);
		float num6 = (num2 <= 0f) ? 0f : (num2 / num4);
		float num7 = (num5 + num6) / 2f;
		if (this.aILayer_Victory.CurrentFocusEnum == AILayer_Victory.VictoryFocus.Economy)
		{
			num7 = Math.Max(0.5f, num7 * 1.5f);
		}
		base.Value = Math.Max(base.ValueMin, Math.Min(num7, base.ValueMax));
	}

	private DepartmentOfTheInterior departmentOfTheInterior;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private DepartmentOfIndustry.ConstructibleElement districtImprovement;

	private Empire empire;

	private SimulationNormalizedAgent[] netCityMoneyAgents;

	[InfluencedByPersonality]
	private float moneyIncomeGrowthPercent = 0.1f;

	[InfluencedByPersonality]
	private float moneyStockToMaintainUpkeepTurnCount = 10f;

	private AILayer_Victory aILayer_Victory;
}
