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
		if (this.DiplomacyLayer.AnyVictoryreactionNeeded && !this.DiplomacyLayer.NeedsVictoryReaction[base.EmpireWhichReceives.Index])
		{
			num *= 1.5f;
			num = Math.Max(num, 20f);
		}
		if (this.VictoryLayer.CurrentFocus == ELCPUtilities.AIVictoryFocus.Diplomacy && !this.DiplomacyLayer.NeedsVictoryReaction[base.EmpireWhichReceives.Index])
		{
			num = Math.Max(num, 35f);
			num *= 1.5f;
		}
		num *= this.multiplier;
		if (this.DiplomacyLayer.MilitaryPowerDif < 0f && base.Empire.GetPropertyValue(SimulationProperties.WarCount) > 1f && !this.DiplomacyLayer.NeedsVictoryReaction[base.EmpireWhichReceives.Index])
		{
			num = Math.Max(num, 20f);
		}
		if (this.DiplomacyLayer.MilitaryPowerDif > 0f && this.VictoryLayer.CurrentFocus != ELCPUtilities.AIVictoryFocus.Diplomacy && (!this.DiplomacyLayer.AnyVictoryreactionNeeded || this.DiplomacyLayer.NeedsVictoryReaction[base.EmpireWhichReceives.Index]))
		{
			num = Math.Min(num, 40f);
		}
		return num / 100f;
	}

	[InfluencedByPersonality]
	private float multiplier = 1f;
}
