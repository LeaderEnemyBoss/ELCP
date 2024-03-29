﻿using System;
using Amplitude;
using UnityEngine;

[PersonalityRegistryPath("AI/AgentDefinition/ForceTruceTermAgent/", new object[]
{

})]
public class ForceTruceTermAgent : DiplomaticTermAgent
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
		if (this.VictoryLayer.CurrentFocusEnum == AILayer_Victory.VictoryFocus.Diplomacy && !this.DiplomacyLayer.NeedsVictoryReaction[base.EmpireWhichReceives.Index])
		{
			num = Math.Max(num, 35f);
			num *= 1.5f;
		}
		num *= this.multiplier;
		return num / 100f;
	}

	[InfluencedByPersonality]
	private float multiplier = 1f;
}
