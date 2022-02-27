using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Collections;
using Amplitude.Unity.AI;
using Amplitude.Unity.AI.Amas;
using Amplitude.Unity.AI.Decision;
using Amplitude.Unity.AI.Decision.Diagnostics;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation.Advanced;
using UnityEngine;

public class AILayer_EmpirePlan : AILayer
{
	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		this.departmentOfPlanification = base.AIEntity.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
		Diagnostics.Assert(this.departmentOfPlanification != null);
		this.departmentOfScience = base.AIEntity.Empire.GetAgency<DepartmentOfScience>();
		this.empirePlanClasses = this.departmentOfPlanification.GetEmpirePlanClasses();
		this.wantedEmpirePlan = new EmpirePlanSimulator();
		this.empirePlanEvaluationHelper = AIScheduler.Services.GetService<IConstructibleElementEvaluationAIHelper>();
		this.decisionMaker = new SimulationDecisionMaker<ConstructibleElement>(this.empirePlanEvaluationHelper, base.AIEntity.Empire);
		this.decisionMaker.ParameterContextModifierDelegate = new Func<ConstructibleElement, StaticString, float>(this.ParameterContextModifierDelegate);
		this.decisionMaker.ScoreTransferFunctionDelegate = new Func<ConstructibleElement, float, float>(this.ScoreTransferFunctionDelegate);
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_EmpirePlan_CreateLocalNeedsPass", new AIEntity.AIAction(this.CreateLocalNeeds), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.EvaluateNeeds.ToString(), "AILayer_EmpirePlan_EvaluateNeedsPass", new AIEntity.AIAction(this.EvaluateNeeds), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.ExecuteNeeds.ToString(), "AILayer_EmpirePlan_ExecuteNeedsPass", new AIEntity.AIAction(this.ExecuteNeeds), this, new StaticString[0]);
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public override IEnumerator Load()
	{
		yield return base.Load();
		this.aiLayerAccountManager = base.AIEntity.GetLayer<AILayer_AccountManager>();
		Diagnostics.Assert(this.aiLayerAccountManager != null);
		this.aiLayerDiplomacy = base.AIEntity.GetLayer<AILayer_Diplomacy>();
		AIEntity_Amas aiEntityAmas = base.AIEntity.AIPlayer.GetEntity<AIEntity_Amas>();
		this.aiLayerResourceAmas = aiEntityAmas.GetLayer<AILayer_ResourceAmas>();
		yield break;
	}

	public override void Release()
	{
		base.Release();
		this.empirePlanEvaluationHelper = null;
		this.decisionMaker = null;
		this.wantedEmpirePlan = null;
		this.departmentOfPlanification = null;
		this.empirePlanClasses = null;
	}

	protected override void CreateLocalNeeds(StaticString context, StaticString pass)
	{
		base.CreateLocalNeeds(context, pass);
		Account account = this.aiLayerAccountManager.TryGetAccount(AILayer_AccountManager.EmpirePlanAccountName);
		if (account == null)
		{
			AILayer.LogError("Can't retrieve the empire plan account.");
			return;
		}
		Diagnostics.Assert(this.empirePlanClasses != null);
		Diagnostics.Assert(this.wantedEmpirePlan != null);
		for (int i = 0; i < this.empirePlanClasses.Length; i++)
		{
			this.wantedEmpirePlan.SubmitPlan(this.departmentOfPlanification.GetEmpirePlanDefinition(this.empirePlanClasses[i], 0));
		}
		Diagnostics.Assert(this.availableEmpirePlans != null);
		this.availableEmpirePlans.Clear();
		for (int j = 0; j < this.empirePlanClasses.Length; j++)
		{
			int empirePlanAvailableLevel = this.departmentOfPlanification.GetEmpirePlanAvailableLevel(this.empirePlanClasses[j]);
			for (int k = 1; k <= empirePlanAvailableLevel; k++)
			{
				this.availableEmpirePlans.Add(this.departmentOfPlanification.GetEmpirePlanDefinition(this.empirePlanClasses[j], k));
			}
		}
		Diagnostics.Assert(this.decisionMaker != null);
		base.AIEntity.Context.InitializeDecisionMaker<ConstructibleElement>(AILayer_Strategy.EmpirePlanParameterModifier, this.decisionMaker);
		this.decisionMaker.ClearAIParametersOverrides();
		this.FillDecisionMakerVariables();
		Diagnostics.Assert(this.decisionResults != null);
		this.decisionResults.Clear();
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			DecisionMakerEvaluationData<ConstructibleElement, InterpreterContext> decisionMakerEvaluationData;
			this.decisionMaker.EvaluateDecisions(this.availableEmpirePlans, ref this.decisionResults, out decisionMakerEvaluationData);
			IGameService service = Services.GetService<IGameService>();
			decisionMakerEvaluationData.Turn = (service.Game as global::Game).Turn;
			this.DecisionMakerEvaluationDataHistoric.Add(decisionMakerEvaluationData);
		}
		else
		{
			this.decisionMaker.EvaluateDecisions(this.availableEmpirePlans, ref this.decisionResults);
		}
		IPersonalityAIHelper service2 = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		float registryValue = service2.GetRegistryValue<float>(base.AIEntity.Empire, "AI/MajorEmpire/AIEntity_Empire/AILayer_EmpirePlan/MaximumPopulationPercentToReachObjective", 0f);
		Diagnostics.Assert(this.wantedEmpirePlan != null);
		float num = 0f;
		float num2 = float.MaxValue;
		float num3 = float.MinValue;
		for (int l = 0; l < this.decisionResults.Count; l++)
		{
			DecisionResult decisionResult = this.decisionResults[l];
			if (decisionResult.Score < 0.05f)
			{
				break;
			}
			EmpirePlanDefinition empirePlanDefinition = decisionResult.Element as EmpirePlanDefinition;
			num2 = Mathf.Min(num2, decisionResult.Score);
			num3 = Mathf.Max(num3, decisionResult.Score);
			EmpirePlanDefinition currentEmpirePlanDefinition = this.wantedEmpirePlan.GetCurrentEmpirePlanDefinition(empirePlanDefinition.EmpirePlanClass);
			if (currentEmpirePlanDefinition == null || empirePlanDefinition.EmpirePlanLevel > currentEmpirePlanDefinition.EmpirePlanLevel)
			{
				float num4 = DepartmentOfTheTreasury.GetProductionCostWithBonus(base.AIEntity.Empire.SimulationObject, empirePlanDefinition, DepartmentOfTheTreasury.Resources.EmpirePoint);
				if (currentEmpirePlanDefinition != null)
				{
					float productionCostWithBonus = DepartmentOfTheTreasury.GetProductionCostWithBonus(base.AIEntity.Empire.SimulationObject, currentEmpirePlanDefinition, DepartmentOfTheTreasury.Resources.EmpirePoint);
					num4 -= productionCostWithBonus;
				}
				float empirePointProductionStress = this.GetEmpirePointProductionStress(num + num4, account, this.departmentOfPlanification.EmpirePlanChoiceRemainingTurn);
				if (empirePointProductionStress <= registryValue)
				{
					num += num4;
					this.wantedEmpirePlan.SubmitPlan(empirePlanDefinition);
				}
			}
		}
		Diagnostics.Assert(this.empirePlanClasses != null);
		Diagnostics.Assert(this.wantedEmpirePlan != null);
		for (int m = 0; m < this.empirePlanClasses.Length; m++)
		{
			EmpirePlanDefinition empirePlan = this.wantedEmpirePlan.GetCurrentEmpirePlanDefinition(this.empirePlanClasses[m]);
			float localOpportunity = 0f;
			if (empirePlan.EmpirePlanLevel > 0)
			{
				float score = this.decisionResults.Find((DecisionResult match) => match.Element == empirePlan).Score;
				localOpportunity = ((num3 - num2 <= float.Epsilon) ? 0f : ((score - num2) / (num3 - num2)));
			}
			EvaluableMessage_EmpirePlan evaluableMessage_EmpirePlan = base.AIEntity.AIPlayer.Blackboard.FindFirst<EvaluableMessage_EmpirePlan>(BlackboardLayerID.Empire, (EvaluableMessage_EmpirePlan match) => match.State == BlackboardMessage.StateValue.Message_InProgress && match.EmpirePlanClass == empirePlan.EmpirePlanClass);
			if (evaluableMessage_EmpirePlan != null)
			{
				evaluableMessage_EmpirePlan.EmpirePlanLevel = empirePlan.EmpirePlanLevel;
				evaluableMessage_EmpirePlan.SetInterest(1f, localOpportunity);
			}
			else
			{
				evaluableMessage_EmpirePlan = new EvaluableMessage_EmpirePlan(empirePlan.EmpirePlanClass, empirePlan.EmpirePlanLevel, this.departmentOfPlanification.EmpirePlanChoiceRemainingTurn, AILayer_AccountManager.EmpirePlanAccountName);
				evaluableMessage_EmpirePlan.SetInterest(1f, localOpportunity);
				base.AIEntity.AIPlayer.Blackboard.AddMessage(evaluableMessage_EmpirePlan);
			}
		}
	}

	protected override void EvaluateNeeds(StaticString context, StaticString pass)
	{
		base.EvaluateNeeds(context, pass);
		Diagnostics.Assert(this.empirePlanMessages != null && this.empirePlanMessages.Count == 0);
		if (!this.departmentOfPlanification.IsEmpirePlanChoiceTurn)
		{
			return;
		}
		this.empirePlanMessages.AddRange(base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_EmpirePlan>(BlackboardLayerID.Empire, (EvaluableMessage_EmpirePlan match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending));
		this.empirePlanMessages.Sort((EvaluableMessage_EmpirePlan left, EvaluableMessage_EmpirePlan right) => -1 * left.Interest.CompareTo(right.Interest));
		for (int i = 0; i < this.empirePlanMessages.Count; i++)
		{
			EvaluableMessage_EmpirePlan evaluableMessage_EmpirePlan = this.empirePlanMessages[i];
			EmpirePlanDefinition empirePlanDefinition = this.departmentOfPlanification.GetEmpirePlanDefinition(evaluableMessage_EmpirePlan.EmpirePlanClass, evaluableMessage_EmpirePlan.EmpirePlanLevel);
			float productionCostWithBonus = DepartmentOfTheTreasury.GetProductionCostWithBonus(base.AIEntity.Empire.SimulationObject, empirePlanDefinition, DepartmentOfTheTreasury.Resources.EmpirePoint);
			evaluableMessage_EmpirePlan.UpdateBuyEvaluation("EmpirePlan", 0UL, productionCostWithBonus, 2, 0f, 0UL);
		}
	}

	protected override void ExecuteNeeds(StaticString context, StaticString pass)
	{
		base.ExecuteNeeds(context, pass);
		if (!this.departmentOfPlanification.IsEmpirePlanChoiceTurn)
		{
			return;
		}
		ISynchronousJobRepositoryAIHelper service = AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>();
		service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_ChangeEmpirePlan));
	}

	private float GetEmpirePointProductionStress(float empirePointAmount, Account empirePlanAccount, int numberOfTurns)
	{
		float num = 0f;
		float num2 = 0f;
		float propertyValue = base.AIEntity.Empire.GetPropertyValue(SimulationProperties.NetEmpirePoint);
		float num3 = 0f;
		DepartmentOfTheInterior agency = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		for (int i = 0; i < agency.Cities.Count; i++)
		{
			City city = agency.Cities[i];
			float propertyValue2 = city.GetPropertyValue(SimulationProperties.CityPointPopulation);
			float propertyValue3 = city.GetPropertyValue(SimulationProperties.BaseCityPointPerPopulation);
			float propertyValue4 = city.GetPropertyValue(SimulationProperties.NetCityEmpirePoint);
			float propertyValue5 = city.GetPropertyValue(SimulationProperties.Population);
			float num4 = propertyValue4 - propertyValue2 * propertyValue3;
			num3 += propertyValue4;
			num += num4 + propertyValue5 * propertyValue3;
			num2 += num4;
		}
		float num5 = propertyValue - num3;
		num2 += num5;
		num += num5;
		num2 *= empirePlanAccount.CurrentProfitPercent;
		num *= empirePlanAccount.CurrentProfitPercent;
		float estimatedBalance = empirePlanAccount.EstimatedBalance;
		float num6 = estimatedBalance + (float)numberOfTurns * num2;
		float num7 = estimatedBalance + (float)numberOfTurns * num;
		float value;
		if (num7 - num6 <= 0f)
		{
			value = (float)((empirePointAmount >= num6) ? 1 : 0);
		}
		else
		{
			value = (empirePointAmount - num6) / (num7 - num6);
		}
		return Mathf.Clamp01(value);
	}

	private void OrderChangeEmpirePlan_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		OrderChangeEmpirePlan orderEmpirePlan = e.Order as OrderChangeEmpirePlan;
		EvaluableMessage_EmpirePlan evaluableMessage_EmpirePlan = base.AIEntity.AIPlayer.Blackboard.FindFirst<EvaluableMessage_EmpirePlan>(BlackboardLayerID.Empire, (EvaluableMessage_EmpirePlan match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate && match.EmpirePlanClass == orderEmpirePlan.EmpirePlanClass);
		if (evaluableMessage_EmpirePlan != null)
		{
			if (e.Result == PostOrderResponse.Processed)
			{
				evaluableMessage_EmpirePlan.SetObtained();
			}
			else
			{
				evaluableMessage_EmpirePlan.SetFailedToObtain();
			}
		}
	}

	private float ParameterContextModifierDelegate(ConstructibleElement element, StaticString aiParameterName)
	{
		return base.AIEntity.Context.GetModifierValue(AILayer_Strategy.EmpirePlanParameterModifier, aiParameterName);
	}

	private float ScoreTransferFunctionDelegate(ConstructibleElement constructibleElement, float score)
	{
		EmpirePlanDefinition empirePlanDefinition = constructibleElement as EmpirePlanDefinition;
		Diagnostics.Assert(empirePlanDefinition != null);
		float productionCostWithBonus = DepartmentOfTheTreasury.GetProductionCostWithBonus(base.AIEntity.Empire.SimulationObject, empirePlanDefinition, DepartmentOfTheTreasury.Resources.EmpirePoint);
		return (productionCostWithBonus <= float.Epsilon) ? score : (score / productionCostWithBonus);
	}

	private void FillDecisionMakerVariables()
	{
		if (this.decisionMaker.Context == null)
		{
			return;
		}
		this.decisionMaker.Context.Register("GlobalWarNeed", this.aiLayerDiplomacy.GetGlobalWarNeed());
		this.decisionMaker.Context.Register("IndustryReferenceTurnCount", this.aiLayerResourceAmas.Amas.GetAgentCriticityMaxIntensity(AILayer_ResourceAmas.AgentNames.IndustryReferenceTurnCount));
		this.decisionMaker.Context.Register("TechnologyReferenceTurnCount", this.aiLayerResourceAmas.Amas.GetAgentCriticityMaxIntensity(AILayer_ResourceAmas.AgentNames.TechnologyReferenceTurnCount));
		this.decisionMaker.Context.Register("MoneyReferenceRatio", this.aiLayerResourceAmas.Amas.GetAgentCriticityMaxIntensity(AILayer_ResourceAmas.AgentNames.MoneyReferenceRatio));
		int num = this.departmentOfScience.CurrentTechnologyEraNumber - 1;
		int num2 = 0;
		int num3 = 0;
		foreach (DepartmentOfScience.ConstructibleElement constructibleElement in this.departmentOfScience.TechnologyDatabase)
		{
			TechnologyDefinition technologyDefinition = constructibleElement as TechnologyDefinition;
			if (technologyDefinition != null)
			{
				int technologyEraNumber = DepartmentOfScience.GetTechnologyEraNumber(technologyDefinition);
				DepartmentOfScience.ConstructibleElement.State technologyState = this.departmentOfScience.GetTechnologyState(constructibleElement);
				if (technologyState == DepartmentOfScience.ConstructibleElement.State.Available)
				{
					num3++;
					if (technologyEraNumber < num)
					{
						num2++;
					}
				}
			}
		}
		float num4 = 0f;
		if (num3 > 0)
		{
			num4 = (float)num2 / (float)num3;
		}
		this.decisionMaker.Context.Register("OldTechnologyRatio", num4);
		AILayer_Strategy layer = base.AIEntity.GetLayer<AILayer_Strategy>();
		float agentValue = layer.StrategicNetwork.GetAgentValue("Expansion");
		this.decisionMaker.Context.Register("ColonizationPriority", agentValue);
		DepartmentOfTheInterior agency = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		float num5 = 0f;
		if (agency.Cities.Count > 0)
		{
			for (int i = 0; i < agency.Cities.Count; i++)
			{
				AgentGroup cityAgentGroup = this.aiLayerResourceAmas.GetCityAgentGroup(agency.Cities[i]);
				if (cityAgentGroup != null)
				{
					num5 += cityAgentGroup.GetAgentCriticityMaxIntensity(AILayer_ResourceAmas.AgentNames.PopulationReferenceTurnCount);
				}
			}
			num5 /= (float)agency.Cities.Count;
		}
		this.decisionMaker.Context.Register("PopulationReferenceTurnCount", num5);
	}

	private SynchronousJobState SynchronousJob_ChangeEmpirePlan()
	{
		if (this.empirePlanClasses == null)
		{
			this.empirePlanMessages.Clear();
			return SynchronousJobState.Failure;
		}
		float num = 0f;
		AILayer_AccountManager layer = base.AIEntity.GetLayer<AILayer_AccountManager>();
		Diagnostics.Assert(layer != null);
		Account account = layer.TryGetAccount(AILayer_AccountManager.EmpirePlanAccountName);
		if (account == null)
		{
			this.empirePlanMessages.Clear();
			return SynchronousJobState.Failure;
		}
		if (this.empirePlanMessages.Count != this.empirePlanClasses.Length)
		{
			Diagnostics.LogError("There must be one empire plan evaluable message by empire plan class.");
			this.empirePlanMessages.Clear();
			return SynchronousJobState.Failure;
		}
		for (int i = 0; i < this.empirePlanMessages.Count; i++)
		{
			EvaluableMessage_EmpirePlan evaluableMessage_EmpirePlan = this.empirePlanMessages[i];
			if (this.departmentOfPlanification.IsEmpirePlanChoiced)
			{
				evaluableMessage_EmpirePlan.SetObtained();
			}
			else
			{
				EmpirePlanDefinition empirePlanDefinition = this.departmentOfPlanification.GetEmpirePlanDefinition(evaluableMessage_EmpirePlan.EmpirePlanClass, evaluableMessage_EmpirePlan.EmpirePlanLevel);
				if (empirePlanDefinition != null)
				{
					float productionCostWithBonus = DepartmentOfTheTreasury.GetProductionCostWithBonus(base.AIEntity.Empire.SimulationObject, empirePlanDefinition, DepartmentOfTheTreasury.Resources.EmpirePoint);
					if (num + productionCostWithBonus <= account.PromisedAmount)
					{
						num += productionCostWithBonus;
						OrderChangeEmpirePlan order = new OrderChangeEmpirePlan(base.AIEntity.Empire.Index, empirePlanDefinition.EmpirePlanClass, empirePlanDefinition.EmpirePlanLevel);
						Ticket ticket;
						base.AIEntity.Empire.PlayerControllers.Client.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderChangeEmpirePlan_TicketRaised));
					}
				}
			}
		}
		this.empirePlanMessages.Clear();
		return SynchronousJobState.Success;
	}

	public FixedSizedList<DecisionMakerEvaluationData<ConstructibleElement, InterpreterContext>> DecisionMakerEvaluationDataHistoric = new FixedSizedList<DecisionMakerEvaluationData<ConstructibleElement, InterpreterContext>>(global::Application.FantasyPreferences.AIDebugHistoricSize);

	private AILayer_AccountManager aiLayerAccountManager;

	private List<ConstructibleElement> availableEmpirePlans = new List<ConstructibleElement>();

	private SimulationDecisionMaker<ConstructibleElement> decisionMaker;

	private List<DecisionResult> decisionResults = new List<DecisionResult>();

	private DepartmentOfPlanificationAndDevelopment departmentOfPlanification;

	private StaticString[] empirePlanClasses;

	private IConstructibleElementEvaluationAIHelper empirePlanEvaluationHelper;

	private List<EvaluableMessage_EmpirePlan> empirePlanMessages = new List<EvaluableMessage_EmpirePlan>(4);

	private EmpirePlanSimulator wantedEmpirePlan;

	private AILayer_Diplomacy aiLayerDiplomacy;

	private AILayer_ResourceAmas aiLayerResourceAmas;

	private DepartmentOfScience departmentOfScience;
}
