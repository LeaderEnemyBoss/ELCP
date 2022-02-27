using System;
using Amplitude;
using UnityEngine;

[PersonalityRegistryPath("AI/AgentDefinition/MoneyPrintNullificationTermAgent/", new object[]
{

})]
public class MoneyPrintNullificationTermAgent : DiplomaticTermAgent
{
	public override void Reset()
	{
		this.myDepartmentOfTheInterior = base.Empire.GetAgency<DepartmentOfTheInterior>();
		base.Reset();
		bool enable = base.Enable;
	}

	protected override void ComputeInitValue()
	{
		base.ComputeInitValue();
		base.ValueInit = Mathf.Clamp01(this.ComputeHeuristicBaseValue());
	}

	protected override void ComputeValue()
	{
		base.ComputeValue();
		base.Value = Mathf.Clamp01(this.ComputeHeuristicBaseValue());
	}

	private float ComputeHeuristicBaseValue()
	{
		Diagnostics.Assert(base.AttitudeScore != null);
		Diagnostics.Assert(base.DiplomaticRelation != null && base.DiplomaticRelation.State != null);
		if (base.DiplomaticRelation.State.Name != DiplomaticRelationState.Names.War)
		{
			return 0f;
		}
		return Math.Min(this.GetAllTradeDustIncomeShare() * 1.8f, 0.75f);
	}

	private float GetAllTradeDustIncomeShare()
	{
		float num = 0f;
		foreach (City city in this.myDepartmentOfTheInterior.Cities)
		{
			if (city.HasProperty(MoneyPrintNullificationTermAgent.TradeRouteCityDustIncome))
			{
				num += city.GetPropertyValue(MoneyPrintNullificationTermAgent.TradeRouteCityDustIncome);
			}
		}
		float num2 = Math.Max(1f, base.Empire.GetPropertyValue(SimulationProperties.EmpireMoney));
		return num / num2;
	}

	public override void Release()
	{
		this.myDepartmentOfTheInterior = null;
		base.Release();
	}

	private DepartmentOfTheInterior myDepartmentOfTheInterior;

	public static StaticString TradeRouteCityDustIncome = "TradeRouteCityDustIncome";
}
