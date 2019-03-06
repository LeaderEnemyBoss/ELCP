using System;
using Amplitude;
using UnityEngine;

[PersonalityRegistryPath("AI/AgentDefinition/TruceTermAgent/", new object[]
{

})]
public class TruceTermAgent : DiplomaticTermAgent
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
		if (base.DiplomaticRelation.State.Name != DiplomaticRelationState.Names.Truce)
		{
			num -= 0.1f * base.DiplomaticRelation.RelationStateChaosScore;
		}
		base.Value = Mathf.Clamp01(num);
	}

	private float ComputeHeuristicBaseValue()
	{
		Diagnostics.Assert(base.AttitudeScore != null);
		Diagnostics.Assert(base.DiplomaticRelation != null && base.DiplomaticRelation.State != null);
		if (base.DiplomaticRelation.State.Name != DiplomaticRelationState.Names.War)
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
