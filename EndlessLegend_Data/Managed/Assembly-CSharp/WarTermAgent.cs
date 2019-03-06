using System;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Session;
using UnityEngine;

[PersonalityRegistryPath("AI/AgentDefinition/WarTermAgent/", new object[]
{

})]
public class WarTermAgent : DiplomaticTermAgent
{
	public override void Reset()
	{
		this.departmentOfForeignAffairs = base.Empire.GetAgency<DepartmentOfForeignAffairs>();
		ISessionService service = Services.GetService<ISessionService>();
		this.SharedVictory = service.Session.GetLobbyData<bool>("Shared", true);
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
		if (this.VictoryLayer.CurrentFocus == ELCPUtilities.AIVictoryFocus.Military && !this.departmentOfForeignAffairs.IsInWarWithSomeone() && base.Empire.GetPropertyValue(SimulationProperties.LandMilitaryPower) > base.EmpireWhichReceives.GetPropertyValue(SimulationProperties.LandMilitaryPower) && (!this.SharedVictory || base.DiplomaticRelation.State.Name != DiplomaticRelationState.Names.Alliance))
		{
			num = Mathf.Max(70f, num + 70f);
		}
		num *= this.multiplier;
		if (this.DiplomacyLayer.AnyVictoryreactionNeeded && !this.DiplomacyLayer.NeedsVictoryReaction[base.EmpireWhichReceives.Index] && this.VictoryLayer.CurrentFocus != ELCPUtilities.AIVictoryFocus.Military)
		{
			num /= 1.5f;
			num = Math.Min(num, 70f);
		}
		if (this.VictoryLayer.CurrentFocus == ELCPUtilities.AIVictoryFocus.Diplomacy && !this.DiplomacyLayer.NeedsVictoryReaction[base.EmpireWhichReceives.Index])
		{
			num = Mathf.Min(60f, num);
		}
		float propertyValue = base.Empire.GetPropertyValue(SimulationProperties.WarCount);
		if (this.DiplomacyLayer.MilitaryPowerDif < 0f && ((base.DiplomaticRelation.State.Name == DiplomaticRelationState.Names.War && propertyValue > 1f) || (base.DiplomaticRelation.State.Name != DiplomaticRelationState.Names.War && propertyValue > 0f)) && !this.DiplomacyLayer.NeedsVictoryReaction[base.EmpireWhichReceives.Index])
		{
			num = Mathf.Min(50f, num / 2f);
		}
		return num / 100f;
	}

	public override void Release()
	{
		this.departmentOfForeignAffairs = null;
		base.Release();
	}

	[InfluencedByPersonality]
	private float blockedTradePenalty = 40f;

	[InfluencedByPersonality]
	private float otherEmpireClosedBordersBonus = 40f;

	[InfluencedByPersonality]
	private float multiplier = 1f;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private bool SharedVictory;
}
