using System;
using Amplitude;
using UnityEngine;

[PersonalityRegistryPath("AI/AgentDefinition/ResearchAgreementTermAgent/", new object[]
{

})]
public class ResearchAgreementTermAgent : DiplomaticTermAgent
{
	public override void Reset()
	{
		base.Reset();
		if (!base.Enable)
		{
			return;
		}
	}

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
		if (!base.DiplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.ResearchAgreement))
		{
			num -= 0.2f * base.DiplomaticRelation.ResearchChaosScore;
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
		if (this.VictoryLayer.CurrentFocusEnum == AILayer_Victory.VictoryFocus.MostTechnologiesDiscovered)
		{
			num += 40f;
		}
		num *= this.multiplier;
		return num / 100f;
	}

	[InfluencedByPersonality]
	private float multiplier = 1f;
}
