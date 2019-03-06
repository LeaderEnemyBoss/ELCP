using System;
using Amplitude;
using Amplitude.Unity.AI.Amas;
using UnityEngine;

[PersonalityRegistryPath("AI/AgentDefinition/AllianceTermAgent/", new object[]
{

})]
public class AllianceTermAgent : DiplomaticTermAgent
{
	public override void Release()
	{
		this.myEmpireDepartmentOfPlanificationAndDevelopment = null;
		this.theirEmpireDepartmentOfPlanificationAndDevelopment = null;
		this.myEmpireDepartmentOfScience = null;
		this.theirEmpireDepartmentOfScience = null;
		this.warAgent = null;
		this.globalWarAgent = null;
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
		this.warAgent = base.ParentGroup.GetAgent(AILayer_DiplomacyAmas.AgentNames.WarTermAgent);
		AgentGroupPath agentGroupPath = new AgentGroupPath("../DiplomacyEvaluationAmas");
		this.globalWarAgent = agentGroupPath.GetFirstValidatedAgent(base.ParentGroup, "GlobalWarAgent");
		if (this.warAgent == null || this.globalWarAgent == null)
		{
			base.Enable = false;
			return;
		}
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
		if (base.DiplomaticRelation.State.Name != DiplomaticRelationState.Names.Alliance)
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
		if (!base.AreTradeRoutesPossibleWithEmpire() && base.GetAbsAttitudeScoreByName(AILayer_Attitude.AttitudeScoreDefinitionReferences.CommonFrontier) > 0f)
		{
			float num2 = ((this.myEmpireDepartmentOfScience.GetTechnologyState(TechnologyDefinition.Names.LandTrade) != DepartmentOfScience.ConstructibleElement.State.Researched) ? 0f : 1f) + ((this.myEmpireDepartmentOfScience.GetTechnologyState(TechnologyDefinition.Names.SeaTrade) != DepartmentOfScience.ConstructibleElement.State.Researched) ? 0f : 1f);
			float num3 = ((this.theirEmpireDepartmentOfScience.GetTechnologyState(TechnologyDefinition.Names.LandTrade) != DepartmentOfScience.ConstructibleElement.State.Researched) ? 0f : 1f) + ((this.theirEmpireDepartmentOfScience.GetTechnologyState(TechnologyDefinition.Names.SeaTrade) != DepartmentOfScience.ConstructibleElement.State.Researched) ? 0f : 1f);
			num += (num2 + num3) / 4f * this.openedTradeBonus;
		}
		Diagnostics.Assert(this.globalWarAgent != null);
		Diagnostics.Assert(this.warAgent != null);
		float num4 = this.globalWarAgent.Value - this.warAgent.Value;
		num += num4 * this.bonusFromDesireForWarWithOthers;
		num *= this.multiplier;
		return num / 100f;
	}

	[InfluencedByPersonality]
	private float openedTradeBonus = 40f;

	[InfluencedByPersonality]
	private float bonusFromDesireForWarWithOthers = 70f;

	[InfluencedByPersonality]
	private float multiplier = 1f;

	private DepartmentOfPlanificationAndDevelopment myEmpireDepartmentOfPlanificationAndDevelopment;

	private DepartmentOfPlanificationAndDevelopment theirEmpireDepartmentOfPlanificationAndDevelopment;

	private DepartmentOfScience myEmpireDepartmentOfScience;

	private DepartmentOfScience theirEmpireDepartmentOfScience;

	private Agent globalWarAgent;

	private Agent warAgent;
}
