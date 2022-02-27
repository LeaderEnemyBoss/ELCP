using System;
using Amplitude;
using Amplitude.Unity.AI.Amas;
using Amplitude.Unity.AI.Amas.Simulation;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

[PersonalityRegistryPath("AI/AgentDefinition/TechnologyReferenceTurnCountAgent/", new object[]
{

})]
public class TechnologyReferenceTurnCountAgent : SimulationAgent
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
		this.departmentOfScience = this.empire.GetAgency<DepartmentOfScience>();
		this.departmentOfTheTreasury = this.empire.GetAgency<DepartmentOfTheTreasury>();
		Diagnostics.Assert(this.departmentOfScience != null && this.departmentOfTheTreasury != null);
		IGameService service = Services.GetService<IGameService>();
		this.game = (service.Game as global::Game);
		InfluencedByPersonalityAttribute.LoadFieldAndPropertyValues(this.empire, this);
	}

	public override void Release()
	{
		this.empire = null;
		this.departmentOfScience = null;
		this.departmentOfTheTreasury = null;
		this.netCityResearchAgents = null;
		this.game = null;
		base.Release();
	}

	public override void Reset()
	{
		base.Reset();
		if (!base.Enable)
		{
			return;
		}
		if (DepartmentOfScience.CanBuyoutResearch(this.empire))
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
		AgentGroupPath agentGroupPath = new AgentGroupPath("ResourceEvaluationAmas/CityAgentGroup");
		Agent[] validatedAgents = agentGroupPath.GetValidatedAgents(base.ParentGroup, "NetCityResearch");
		SimulationNormalizedAgent[] array;
		if (validatedAgents != null)
		{
			array = Array.ConvertAll<Agent, SimulationNormalizedAgent>(validatedAgents, (Agent agent) => agent as SimulationNormalizedAgent);
		}
		else
		{
			array = new SimulationNormalizedAgent[0];
		}
		this.netCityResearchAgents = array;
		Diagnostics.Assert(this.netCityResearchAgents != null);
	}

	protected override void ComputeInitValue()
	{
		base.ComputeInitValue();
		Diagnostics.Assert(this.departmentOfScience != null && this.departmentOfTheTreasury != null);
		float num = (float)this.game.Turn;
		float propertyValue = this.empire.GetPropertyValue(SimulationProperties.NetEmpireResearch);
		float researchPropertyValue = this.departmentOfScience.GetResearchPropertyValue("UnlockedTechnologyCount");
		float num2 = 0f;
		int num3 = 0;
		Construction construction = this.departmentOfScience.ResearchQueue.Peek();
		if (construction != null)
		{
			num3 = 1;
			float num4 = this.departmentOfTheTreasury.ComputeConstructionRemainingCost(this.empire, construction, DepartmentOfTheTreasury.Resources.EmpireResearch);
			num2 = ((propertyValue <= 0f) ? float.MaxValue : (num4 / propertyValue));
		}
		float num5 = researchPropertyValue + (float)num3;
		float num6 = num + num2;
		float num7 = num5 * this.idealTechnologyUnlockPeriod;
		float val = (num6 - num7) / this.maximumPeriodGap;
		base.ValueInit = Math.Max(base.ValueMin, Math.Min(val, base.ValueMax));
	}

	protected override void ComputeValue()
	{
		base.ComputeValue();
		Diagnostics.Assert(this.departmentOfScience != null);
		Diagnostics.Assert(this.netCityResearchAgents != null);
		float num = (float)this.game.Turn;
		float num2 = 0f;
		for (int i = 0; i < this.netCityResearchAgents.Length; i++)
		{
			SimulationNormalizedAgent simulationNormalizedAgent = this.netCityResearchAgents[i];
			Diagnostics.Assert(simulationNormalizedAgent != null);
			City city = simulationNormalizedAgent.ContextObject as City;
			Diagnostics.Assert(city != null);
			float propertyValue = city.GetPropertyValue(SimulationProperties.NetCityResearch);
			num2 += simulationNormalizedAgent.UnnormalizedValue - propertyValue;
		}
		float num3 = this.empire.GetPropertyValue(SimulationProperties.NetEmpireResearch) + num2;
		float researchPropertyValue = this.departmentOfScience.GetResearchPropertyValue("UnlockedTechnologyCount");
		float num4 = 0f;
		int num5 = 0;
		Construction construction = this.departmentOfScience.ResearchQueue.Peek();
		if (construction != null)
		{
			num5 = 1;
			float num6 = this.departmentOfTheTreasury.ComputeConstructionRemainingCost(this.empire, construction, DepartmentOfTheTreasury.Resources.EmpireResearch);
			num4 = ((num3 <= 0f) ? float.MaxValue : (num6 / num3));
		}
		float num7 = researchPropertyValue + (float)num5;
		float num8 = num + num4;
		float num9 = num7 * this.idealTechnologyUnlockPeriod;
		float val = (num8 - num9) / this.maximumPeriodGap;
		base.Value = Math.Max(base.ValueMin, Math.Min(val, base.ValueMax));
	}

	private DepartmentOfScience departmentOfScience;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private global::Empire empire;

	private global::Game game;

	private SimulationNormalizedAgent[] netCityResearchAgents;

	[InfluencedByPersonality]
	private float idealTechnologyUnlockPeriod = 5f;

	[InfluencedByPersonality]
	private float maximumPeriodGap = 15f;
}
