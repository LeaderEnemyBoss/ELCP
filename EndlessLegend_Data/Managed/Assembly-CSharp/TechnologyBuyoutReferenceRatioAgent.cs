using System;
using Amplitude;
using Amplitude.Unity.AI.Amas;
using Amplitude.Unity.AI.Amas.Simulation;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

[PersonalityRegistryPath("AI/AgentDefinition/TechnologyBuyoutReferenceRatioAgent/", new object[]
{

})]
public class TechnologyBuyoutReferenceRatioAgent : Agent
{
	public override void Initialize(AgentDefinition agentDefinition, AgentGroup parentGroup)
	{
		base.Initialize(agentDefinition, parentGroup);
		AIPlayer_MajorEmpire aiplayer_MajorEmpire = base.ContextObject as AIPlayer_MajorEmpire;
		if (aiplayer_MajorEmpire == null)
		{
			Diagnostics.LogError("The agent context object is not an ai player.");
			return;
		}
		this.empire = aiplayer_MajorEmpire.MajorEmpire;
		if (this.empire == null)
		{
			Diagnostics.LogError("Can't retrieve the empire.");
			return;
		}
		Diagnostics.Assert(base.ParentGroup != null);
		this.netEmpireMoneyAgent = (base.ParentGroup.GetAgent("NetEmpireMoney") as SimulationNormalizedAgent);
		Diagnostics.Assert(this.netEmpireMoneyAgent != null);
		this.departmentOfScience = this.empire.GetAgency<DepartmentOfScience>();
		IGameService service = Services.GetService<IGameService>();
		this.game = (service.Game as global::Game);
		InfluencedByPersonalityAttribute.LoadFieldAndPropertyValues(this.empire, this);
	}

	public override void Release()
	{
		this.empire = null;
		this.departmentOfScience = null;
		this.netEmpireMoneyAgent = null;
		base.Release();
	}

	public override void Reset()
	{
		base.Reset();
		if (!base.Enable)
		{
			return;
		}
		if (!DepartmentOfScience.CanBuyoutResearch(this.empire))
		{
			base.Enable = false;
			return;
		}
		if (this.departmentOfScience == null)
		{
			Diagnostics.LogError("The agent {0} can't get the department of science.", new object[]
			{
				base.Name
			});
			base.Enable = false;
			return;
		}
	}

	protected override void ComputeInitValue()
	{
		base.ComputeInitValue();
		float propertyValue = this.empire.GetPropertyValue(SimulationProperties.NetEmpireMoney);
		Construction construction = this.departmentOfScience.ResearchQueue.Peek();
		float num = (construction == null) ? 0f : this.departmentOfScience.GetBuyOutTechnologyCost(construction.ConstructibleElement);
		float num2 = this.moneyIncomeBuyoutPercent * num;
		float a = (num2 <= 0f) ? 1f : ((propertyValue <= 0f) ? 0f : (propertyValue / num2));
		float num3 = (float)this.game.Turn;
		float researchPropertyValue = this.departmentOfScience.GetResearchPropertyValue("UnlockedTechnologyCount");
		float num4 = researchPropertyValue * this.idealTechnologyUnlockPeriod;
		float num5 = Mathf.Clamp((num4 - num3) / this.idealTechnologyUnlockPeriod, -1f, 1f);
		num5 = (num5 + 1f) / 2f;
		float val = Mathf.Min(a, num5);
		base.ValueInit = Math.Max(base.ValueMin, Math.Min(val, base.ValueMax));
	}

	protected override void ComputeValue()
	{
		base.ComputeValue();
		Diagnostics.Assert(this.empire != null);
		float unnormalizedValue = this.netEmpireMoneyAgent.UnnormalizedValue;
		Construction construction = this.departmentOfScience.ResearchQueue.Peek();
		float num = (construction == null) ? 0f : this.departmentOfScience.GetBuyOutTechnologyCost(construction.ConstructibleElement);
		float num2 = this.moneyIncomeBuyoutPercent * num;
		float a = (num2 <= 0f) ? 1f : ((unnormalizedValue <= 0f) ? 0f : (unnormalizedValue / num2));
		float num3 = (float)this.game.Turn;
		float researchPropertyValue = this.departmentOfScience.GetResearchPropertyValue("UnlockedTechnologyCount");
		float num4 = researchPropertyValue * this.idealTechnologyUnlockPeriod;
		float num5 = Mathf.Clamp((num4 - num3) / this.idealTechnologyUnlockPeriod, -1f, 1f);
		num5 = (num5 + 1f) / 2f;
		float val = Mathf.Min(a, num5);
		base.Value = Math.Max(base.ValueMin, Math.Min(val, base.ValueMax));
	}

	private DepartmentOfScience departmentOfScience;

	private MajorEmpire empire;

	private global::Game game;

	private SimulationNormalizedAgent netEmpireMoneyAgent;

	[InfluencedByPersonality]
	private float idealTechnologyUnlockPeriod = 5f;

	[InfluencedByPersonality]
	private float moneyIncomeBuyoutPercent = 0.33f;
}
