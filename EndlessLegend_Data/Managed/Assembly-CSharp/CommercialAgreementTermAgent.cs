using System;
using Amplitude;
using UnityEngine;

[PersonalityRegistryPath("AI/AgentDefinition/CommercialAgreementTermAgent/", new object[]
{

})]
public class CommercialAgreementTermAgent : DiplomaticTermAgent
{
	public float GetValueBeforeClamp()
	{
		return this.ComputeHeuristicBaseValue();
	}

	protected override void ComputeInitValue()
	{
		base.ComputeInitValue();
		base.ValueInit = Mathf.Clamp01(this.ComputeHeuristicBaseValue());
	}

	protected override void ComputeValue()
	{
		base.ComputeValue();
		float num = this.ComputeHeuristicBaseValue();
		if (!base.DiplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.CommercialAgreement))
		{
			num -= 0.2f * base.DiplomaticRelation.CommercialChaosScore;
		}
		base.Value = Mathf.Clamp01(num);
	}

	private float ComputeHeuristicBaseValue()
	{
		Diagnostics.Assert(base.AttitudeScore != null);
		Diagnostics.Assert(base.DiplomaticRelation != null && base.DiplomaticRelation.State != null);
		if (base.DiplomaticRelation.State.Name == DiplomaticRelationState.Names.Unknown)
		{
			return 0f;
		}
		if (!base.AreTradeRoutesPossibleWithEmpire())
		{
			return 0f;
		}
		float num = base.GetValueFromAttitude();
		num *= this.multiplier;
		return num / 100f;
	}

	[InfluencedByPersonality]
	private float multiplier = 1f;
}
