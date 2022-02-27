using System;
using Amplitude;
using UnityEngine;

[PersonalityRegistryPath("AI/AgentDefinition/ForcePeaceTermAgent/", new object[]
{

})]
public class ForcePeaceTermAgent : DiplomaticTermAgent
{
	public override void Reset()
	{
		base.Reset();
		if (!base.Enable)
		{
			return;
		}
		if (!base.Empire.SimulationObject.Tags.Contains("AffinityDrakkens"))
		{
			base.Enable = false;
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
		if (base.DiplomaticRelation.State.Name != DiplomaticRelationState.Names.Peace)
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
		num *= this.multiplier;
		if (this.DiplomacyLayer.GetMilitaryPowerDif(false) < 0f && (base.DiplomaticRelation.State.Name == DiplomaticRelationState.Names.ColdWar || base.DiplomaticRelation.State.Name == DiplomaticRelationState.Names.Peace) && !this.DiplomacyLayer.NeedsVictoryReaction[base.EmpireWhichReceives.Index])
		{
			num = Mathf.Max(51f, num + 25f + Mathf.Abs(this.DiplomacyLayer.GetMilitaryPowerDif(false)) / base.Empire.GetPropertyValue(SimulationProperties.LandMilitaryPower) * 100f);
		}
		if (this.VictoryLayer.CurrentFocusEnum == AILayer_Victory.VictoryFocus.Diplomacy && !this.DiplomacyLayer.NeedsVictoryReaction[base.EmpireWhichReceives.Index] && (base.DiplomaticRelation.State.Name == DiplomaticRelationState.Names.ColdWar || base.DiplomaticRelation.State.Name == DiplomaticRelationState.Names.Peace))
		{
			num = Mathf.Max(51f, num + 51f);
		}
		return num / 100f;
	}

	[InfluencedByPersonality]
	private float multiplier = 1f;
}
