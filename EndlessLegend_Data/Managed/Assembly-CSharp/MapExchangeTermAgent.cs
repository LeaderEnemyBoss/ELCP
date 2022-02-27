using System;
using Amplitude;
using UnityEngine;

[PersonalityRegistryPath("AI/AgentDefinition/MapExchangeTermAgent/", new object[]
{

})]
public class MapExchangeTermAgent : DiplomaticTermAgent
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
		if (!base.DiplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.MapExchange))
		{
			num -= 0.2f * base.DiplomaticRelation.VisionChaosScore;
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
		IWorldAtlasAIHelper service = AIScheduler.Services.GetService<IWorldAtlasAIHelper>();
		float worldExplorationRatio = service.GetWorldExplorationRatio(base.Empire);
		float worldExplorationRatio2 = service.GetWorldExplorationRatio(base.EmpireWhichReceives);
		float value = Mathf.Max(0.1f, worldExplorationRatio2) / Mathf.Max(0.1f, worldExplorationRatio) - 1f;
		num += this.explorationLeadBonus * Mathf.Clamp(value, -1f, 1f);
		num *= this.multiplier;
		return num / 100f;
	}

	[InfluencedByPersonality]
	private float explorationLeadBonus = 35f;

	[InfluencedByPersonality]
	private float multiplier = 1f;
}
