using System;
using Amplitude;
using UnityEngine;

[PersonalityRegistryPath("AI/AgentDefinition/PeaceTermAgent/", new object[]
{

})]
public class PeaceTermAgent : DiplomaticTermAgent
{
	public override void Release()
	{
		this.myEmpireDepartmentOfPlanificationAndDevelopment = null;
		this.theirEmpireDepartmentOfPlanificationAndDevelopment = null;
		this.myEmpireDepartmentOfScience = null;
		this.theirEmpireDepartmentOfScience = null;
		base.Release();
	}

	public override void Reset()
	{
		this.myEmpireDepartmentOfPlanificationAndDevelopment = base.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
		this.theirEmpireDepartmentOfPlanificationAndDevelopment = base.EmpireWhichReceives.GetAgency<DepartmentOfPlanificationAndDevelopment>();
		this.myEmpireDepartmentOfScience = base.Empire.GetAgency<DepartmentOfScience>();
		this.theirEmpireDepartmentOfScience = base.EmpireWhichReceives.GetAgency<DepartmentOfScience>();
		Diagnostics.Assert(this.myEmpireDepartmentOfPlanificationAndDevelopment != null);
		Diagnostics.Assert(this.theirEmpireDepartmentOfPlanificationAndDevelopment != null);
		Diagnostics.Assert(this.myEmpireDepartmentOfScience != null);
		Diagnostics.Assert(this.theirEmpireDepartmentOfScience != null);
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
		if (!base.DiplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.CloseBorders) && !base.HasOtherEmpireClosedTheirBorders() && base.GetAbsAttitudeScoreByName(AILayer_Attitude.AttitudeScoreDefinitionReferences.CommonFrontier) > 0f)
		{
			float num2 = ((this.myEmpireDepartmentOfScience.GetTechnologyState(TechnologyDefinition.Names.LandTrade) != DepartmentOfScience.ConstructibleElement.State.Researched) ? 0f : 1f) + ((this.myEmpireDepartmentOfScience.GetTechnologyState(TechnologyDefinition.Names.SeaTrade) != DepartmentOfScience.ConstructibleElement.State.Researched) ? 0f : 1f);
			float num3 = ((this.theirEmpireDepartmentOfScience.GetTechnologyState(TechnologyDefinition.Names.LandTrade) != DepartmentOfScience.ConstructibleElement.State.Researched) ? 0f : 1f) + ((this.theirEmpireDepartmentOfScience.GetTechnologyState(TechnologyDefinition.Names.SeaTrade) != DepartmentOfScience.ConstructibleElement.State.Researched) ? 0f : 1f);
			num += (num2 + num3) / 4f * this.openedTradeBonus;
		}
		if (this.DiplomacyLayer.MilitaryPowerDif < 0f && (base.DiplomaticRelation.State.Name == DiplomaticRelationState.Names.ColdWar || base.DiplomaticRelation.State.Name == DiplomaticRelationState.Names.Peace) && !this.DiplomacyLayer.NeedsVictoryReaction[base.EmpireWhichReceives.Index])
		{
			num = Mathf.Max(50f, num + 50f);
		}
		num *= this.multiplier;
		if (this.VictoryLayer.CurrentFocus == ELCPUtilities.AIVictoryFocus.Diplomacy && !this.DiplomacyLayer.NeedsVictoryReaction[base.EmpireWhichReceives.Index] && (base.DiplomaticRelation.State.Name == DiplomaticRelationState.Names.ColdWar || base.DiplomaticRelation.State.Name == DiplomaticRelationState.Names.Peace))
		{
			num = Mathf.Max(50f, num + 50f);
		}
		return num / 100f;
	}

	[InfluencedByPersonality]
	private float openedTradeBonus = 40f;

	[InfluencedByPersonality]
	private float multiplier = 1f;

	private DepartmentOfPlanificationAndDevelopment myEmpireDepartmentOfPlanificationAndDevelopment;

	private DepartmentOfPlanificationAndDevelopment theirEmpireDepartmentOfPlanificationAndDevelopment;

	private DepartmentOfScience myEmpireDepartmentOfScience;

	private DepartmentOfScience theirEmpireDepartmentOfScience;
}
