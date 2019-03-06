using System;
using Amplitude;
using UnityEngine;

[PersonalityRegistryPath("AI/AgentDefinition/WarTermAgent/", new object[]
{

})]
public class WarTermAgent : DiplomaticTermAgent
{
	public override void Reset()
	{
		base.Reset();
		if (!base.Enable)
		{
			return;
		}
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
		if (base.DiplomaticRelation.State.Name != DiplomaticRelationState.Names.War)
		{
			num -= 0.2f * base.DiplomaticRelation.RelationStateChaosScore;
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
		float num = base.GetValueFromAttitude();
		if (base.HasOtherEmpireClosedTheirBorders())
		{
			num += this.otherEmpireClosedBordersBonus;
		}
		else if (base.AreTradeRoutesPossibleWithEmpire())
		{
			num -= this.blockedTradePenalty;
		}
		num *= this.multiplier;
		return num / 100f;
	}

	[InfluencedByPersonality]
	private float blockedTradePenalty = 40f;

	[InfluencedByPersonality]
	private float otherEmpireClosedBordersBonus = 40f;

	[InfluencedByPersonality]
	private float multiplier = 1f;
}
