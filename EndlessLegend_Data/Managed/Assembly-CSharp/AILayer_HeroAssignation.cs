using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.AI;
using Amplitude.Unity.AI.Decision;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Simulation.Advanced;
using UnityEngine;

public class AILayer_HeroAssignation : AILayer, ITickable, ISimulationAIEvaluationHelper<UnitSkill>, IAIEvaluationHelper<UnitSkill, InterpreterContext>
{
	public AILayer_HeroAssignation()
	{
		this.failureFlags = new List<StaticString>();
		this.infiltrationActionToPerform = new List<InfiltrationActionData>();
		base..ctor();
	}

	public Dictionary<GameEntityGUID, List<InfiltrationActionData>> Editor_InfiltrationDataByHeroes { get; set; }

	public List<AIData_Unit> Editor_Spies { get; set; }

	private void ChooseInfiltrationActionForSpy(AIData_Unit hero)
	{
		IGarrison garrison;
		if (!this.departmentOfIntelligence.TryGetGarrisonForSpy(hero.Unit.GUID, out garrison))
		{
			return;
		}
		float num = this.ComputeUtilityThreshold(hero, hero.Unit.Garrison as SpiedGarrison, garrison as City, null);
		InterpreterContext context = InfiltrationAction.CreateContext(base.AIEntity.Empire, hero.Unit.Garrison.GUID);
		List<InfiltrationAction> list = new List<InfiltrationAction>();
		InfiltrationAction action;
		foreach (InfiltrationAction action2 in this.infiltrationActionDatabase)
		{
			action = action2;
			this.failureFlags.Clear();
			if (action.CanExecute(context, ref this.failureFlags, new object[0]))
			{
				int num2 = list.FindIndex((InfiltrationAction match) => match.FirstName == action.FirstName);
				if (num2 >= 0)
				{
					if (list[num2].Level < action.Level)
					{
						list[num2] = action;
					}
				}
				else
				{
					list.Add(action);
				}
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			InfiltrationAction infiltrationAction = list[i];
			float num3 = this.ComputeInfiltrationUtility(infiltrationAction, hero, hero.Unit.Garrison as SpiedGarrison, garrison as City);
			if (num3 >= num)
			{
				InfiltrationActionData infiltrationActionData = new InfiltrationActionData();
				infiltrationActionData.ChosenActionName = infiltrationAction.Name;
				infiltrationActionData.ChosenActionFirstName = infiltrationAction.FirstName;
				infiltrationActionData.ChosenActionUtility = num3;
				infiltrationActionData.HeroGuid = hero.Unit.GUID;
				infiltrationActionData.SpiedGarrisonGuid = hero.Unit.Garrison.GUID;
				infiltrationActionData.UtilityThreshold = num;
				this.infiltrationActionToPerform.Add(infiltrationActionData);
			}
		}
	}

	private void FilterActionToPerform()
	{
		this.infiltrationActionToPerform.Sort((InfiltrationActionData left, InfiltrationActionData right) => -1 * left.ChosenActionUtility.CompareTo(right.ChosenActionUtility));
		for (int i = 0; i < this.infiltrationActionToPerform.Count; i++)
		{
			InfiltrationActionData chosenAction = this.infiltrationActionToPerform[i];
			if (chosenAction.ChosenActionUtility < chosenAction.UtilityThreshold)
			{
				this.infiltrationActionToPerform.RemoveAt(i);
				i--;
			}
			else
			{
				this.infiltrationActionToPerform.RemoveAll((InfiltrationActionData match) => match.HeroGuid == chosenAction.HeroGuid && match.ChosenActionName != chosenAction.ChosenActionName);
				InfiltrationAction value = this.infiltrationActionDatabase.GetValue(chosenAction.ChosenActionName);
				if (value is InfiltrationActionOnEmpire)
				{
					this.infiltrationActionToPerform.ForEach(delegate(InfiltrationActionData match)
					{
						if (match.HeroGuid != chosenAction.HeroGuid)
						{
							match.ChosenActionUtility = AILayer.Boost(match.ChosenActionUtility, -0.5f);
						}
					});
					this.infiltrationActionToPerform.Sort((InfiltrationActionData left, InfiltrationActionData right) => -1 * left.ChosenActionUtility.CompareTo(right.ChosenActionUtility));
				}
				EvaluableMessage_InfiltrationAction evaluableMessage_InfiltrationAction = base.AIEntity.AIPlayer.Blackboard.FindFirst<EvaluableMessage_InfiltrationAction>(BlackboardLayerID.Empire, (EvaluableMessage_InfiltrationAction match) => match is EvaluableMessage_InfiltrationAction && match.HeroGuid == chosenAction.HeroGuid);
				if (evaluableMessage_InfiltrationAction == null || evaluableMessage_InfiltrationAction.State != BlackboardMessage.StateValue.Message_InProgress)
				{
					evaluableMessage_InfiltrationAction = new EvaluableMessage_InfiltrationAction(value.Name, chosenAction.HeroGuid);
					base.AIEntity.AIPlayer.Blackboard.AddMessage(evaluableMessage_InfiltrationAction);
				}
				evaluableMessage_InfiltrationAction.TimeOut = 1;
				evaluableMessage_InfiltrationAction.InfiltrationActionName = value.Name;
				evaluableMessage_InfiltrationAction.SetInterest(0.5f, chosenAction.ChosenActionUtility);
				InterpreterContext context = InfiltrationAction.CreateContext(base.AIEntity.Empire, chosenAction.SpiedGarrisonGuid);
				value.ComputeConstructionCost(context);
				float num = 0f;
				for (int j = 0; j < InfiltrationAction.Context.ConstructionCosts.Length; j++)
				{
					if (InfiltrationAction.Context.ConstructionCosts[j].ResourceName == DepartmentOfTheTreasury.Resources.EmpirePoint)
					{
						num += InfiltrationAction.Context.ConstructionCosts[j].Value;
					}
				}
				evaluableMessage_InfiltrationAction.UpdateBuyEvaluation("InfiltrationAction", 0UL, num, 2, 0f, 0UL);
			}
		}
	}

	private bool ExecuteInfiltrationAction(InfiltrationActionData infiltrationActionData)
	{
		InfiltrationAction infiltrationAction;
		if (this.infiltrationActionDatabase.TryGetValue(infiltrationActionData.ChosenActionName, out infiltrationAction))
		{
			EvaluableMessage_InfiltrationAction evaluableMessage_InfiltrationAction = base.AIEntity.AIPlayer.Blackboard.FindFirst<EvaluableMessage_InfiltrationAction>(BlackboardLayerID.Empire, (EvaluableMessage_InfiltrationAction match) => match is EvaluableMessage_InfiltrationAction && match.HeroGuid == infiltrationActionData.HeroGuid);
			IGarrison garrison;
			if (evaluableMessage_InfiltrationAction != null && evaluableMessage_InfiltrationAction.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate && this.departmentOfIntelligence.TryGetGarrisonForSpy(infiltrationActionData.HeroGuid, out garrison))
			{
				if (infiltrationAction is InfiltrationActionOnEmpire_StealTechnology)
				{
					object element = this.stealableTechnologiesResult[garrison.Empire.Index][0].Element;
					if (element is TechnologyDefinition)
					{
						TechnologyDefinition technologyDefinition = element as TechnologyDefinition;
						DepartmentOfScience agency = base.AIEntity.Empire.GetAgency<DepartmentOfScience>();
						bool flag = false;
						if (agency != null)
						{
							if (agency.GetTechnologyState(technologyDefinition) == DepartmentOfScience.ConstructibleElement.State.Researched)
							{
								flag = true;
							}
							else if (agency.ResearchQueue.Length > 0)
							{
								Construction construction = agency.ResearchQueue.Peek();
								DepartmentOfScience.ConstructibleElement constructibleElement;
								if (!agency.TechnologyDatabase.TryGetValue(construction.ConstructibleElement.Name, out constructibleElement))
								{
									flag = true;
								}
								else if (constructibleElement.Name == technologyDefinition.Name)
								{
									flag = true;
								}
							}
						}
						if (flag && this.stealableTechnologiesResult[garrison.Empire.Index].Count > 0 && this.stealableTechnologiesResult[garrison.Empire.Index][0].Element == technologyDefinition && this.stealableTechnologiesResult[garrison.Empire.Index].Count > 1 && this.stealableTechnologiesResult[garrison.Empire.Index][1].Element != null)
						{
							element = this.stealableTechnologiesResult[garrison.Empire.Index][1].Element;
						}
					}
					infiltrationAction.Execute(garrison as City, base.AIEntity.Empire.PlayerControllers.AI, out this.orderTicket, null, new object[]
					{
						element
					});
				}
				else
				{
					infiltrationAction.Execute(garrison as City, base.AIEntity.Empire.PlayerControllers.AI, out this.orderTicket, null, new object[0]);
				}
				evaluableMessage_InfiltrationAction.SetObtained();
			}
		}
		return false;
	}

	private void GenerateInfiltrationActions()
	{
		this.infiltrationActionToPerform.Clear();
		for (int i = 0; i < this.stealableTechnologiesResult.Length; i++)
		{
			if (this.stealableTechnologiesResult[i] != null)
			{
				this.stealableTechnologiesResult[i].Clear();
			}
		}
		List<AIData_Unit> list = new List<AIData_Unit>();
		for (int j = 0; j < this.departmentOfEducation.Heroes.Count; j++)
		{
			AIData_Unit item;
			if (this.departmentOfEducation.Heroes[j].Garrison is SpiedGarrison && this.aiDataRepositoryHelper.TryGetAIData<AIData_Unit>(this.departmentOfEducation.Heroes[j].GUID, out item))
			{
				list.Add(item);
			}
		}
		list.Sort(new Comparison<AIData_Unit>(this.SortSpyByInfiltrationPoint));
		for (int k = 0; k < list.Count; k++)
		{
			this.ChooseInfiltrationActionForSpy(list[k]);
		}
		this.FilterActionToPerform();
	}

	protected virtual void InitializeInfiltrationActionUtilities()
	{
		this.gameStatisticsManagement = Services.GetService<IGameStatisticsManagementService>();
		this.empireDataHelper = AIScheduler.Services.GetService<IAIEmpireDataAIHelper>();
		this.utilityFunctions.Add("DamageFortification", new AILayer_HeroAssignation.InfiltrationActionUtilityFunc(this.InfiltrationActionUtility_DamageFortification));
		this.utilityFunctions.Add("DecreasePopulation", new AILayer_HeroAssignation.InfiltrationActionUtilityFunc(this.InfiltrationActionUtility_DecreasePopulation));
		this.utilityFunctions.Add("DecreaseVision", new AILayer_HeroAssignation.InfiltrationActionUtilityFunc(this.InfiltrationActionUtility_DecreaseVision));
		this.utilityFunctions.Add("DiplomaticCostReduction", new AILayer_HeroAssignation.InfiltrationActionUtilityFunc(this.InfiltrationActionUtility_DiplomaticCostReduction));
		this.utilityFunctions.Add("LeechTradeRoutes", new AILayer_HeroAssignation.InfiltrationActionUtilityFunc(this.InfiltrationActionUtility_LeechTradeRoutes));
		this.utilityFunctions.Add("PoisonGovernor", new AILayer_HeroAssignation.InfiltrationActionUtilityFunc(this.InfiltrationActionUtility_PoisonGovernor));
		this.utilityFunctions.Add("ResearchCost", new AILayer_HeroAssignation.InfiltrationActionUtilityFunc(this.InfiltrationActionUtility_ResearchCost));
		this.utilityFunctions.Add("Reveal", new AILayer_HeroAssignation.InfiltrationActionUtilityFunc(this.InfiltrationActionUtility_Reveal));
		this.utilityFunctions.Add("StealTechnology", new AILayer_HeroAssignation.InfiltrationActionUtilityFunc(this.InfiltrationActionUtility_StealTechnology));
		this.utilityFunctions.Add("StealVision", new AILayer_HeroAssignation.InfiltrationActionUtilityFunc(this.InfiltrationActionUtility_StealVision));
		this.utilityFunctions.Add("BattleMorale", new AILayer_HeroAssignation.InfiltrationActionUtilityFunc(this.InfiltrationActionUtility_DecreaseMoral));
		this.utilityFunctions.Add("ProductionCost", new AILayer_HeroAssignation.InfiltrationActionUtilityFunc(this.InfiltrationActionUtility_DecreaseProduction));
		this.utilityFunctions.Add("StealEmpireMoney", new AILayer_HeroAssignation.InfiltrationActionUtilityFunc(this.InfiltrationActionUtility_StealEmpireMoney));
		string registryValue = this.personalityAIHelper.GetRegistryValue<string>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "PoisonGovernor/AssignationCurveName"), string.Empty);
		if (!string.IsNullOrEmpty(registryValue))
		{
			this.poisonGovernorAssignationCurve = this.animationCurveDatabase.GetValue(registryValue);
		}
		this.poisonGovernorHeroLevelMaximum = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "PoisonGovernor/HeroLevelMaximum"), this.poisonGovernorHeroLevelMaximum);
		string registryValue2 = this.personalityAIHelper.GetRegistryValue<string>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "PoisonGovernor/HeroLevelAnimationCurveName"), string.Empty);
		if (!string.IsNullOrEmpty(registryValue2))
		{
			this.poisonGovernorHeroLevelCurve = this.animationCurveDatabase.GetValue(registryValue2);
		}
		this.leechMaximumNumberOfTradeRoute = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "Leech/MaximumNumberOfTradeRoute"), this.leechMaximumNumberOfTradeRoute);
		string registryValue3 = this.personalityAIHelper.GetRegistryValue<string>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "Leech/NumberOfTradeRouteCurve"), string.Empty);
		if (!string.IsNullOrEmpty(registryValue3))
		{
			this.leechNumberOfTradeRouteCurve = this.animationCurveDatabase.GetValue(registryValue3);
		}
		this.damageFortificationBesiegedByMe = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "DamageFortification/BesiegedByMe"), this.damageFortificationBesiegedByMe);
		this.damageFortificationBesiegedByAlly = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "DamageFortification/BesiegedByAlly"), this.damageFortificationBesiegedByAlly);
		this.decreasePopulationMissedPopulationFactor = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "DecreasePopulation/MissedPopulationFactor"), this.decreasePopulationMissedPopulationFactor);
		this.decreasePopulationMaximumPopulation = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "DecreasePopulation/MaximumPopulation"), this.decreasePopulationMaximumPopulation);
		string registryValue4 = this.personalityAIHelper.GetRegistryValue<string>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "DecreasePopulation/PopulationCurve"), string.Empty);
		if (!string.IsNullOrEmpty(registryValue4))
		{
			this.decreasePopulationCurve = this.animationCurveDatabase.GetValue(registryValue4);
		}
		this.workerYields = new List<StaticString>();
		this.workerYields.Add(SimulationProperties.BaseFoodPerPopulation);
		this.workerYields.Add(SimulationProperties.BaseIndustryPerPopulation);
		this.workerYields.Add(SimulationProperties.BaseSciencePerPopulation);
		this.workerYields.Add(SimulationProperties.BaseDustPerPopulation);
		this.workerYields.Add(SimulationProperties.BaseCityPointPerPopulation);
		this.decreasePopulationMaximumYieldPerPopulation = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "DecreasePopulation/MaximumYieldPerPopulation"), this.decreasePopulationMaximumYieldPerPopulation);
		string registryValue5 = this.personalityAIHelper.GetRegistryValue<string>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "DecreasePopulation/YieldCurve"), string.Empty);
		if (!string.IsNullOrEmpty(registryValue5))
		{
			this.decreasePopulationYieldCurve = this.animationCurveDatabase.GetValue(registryValue5);
		}
		string registryValue6 = this.personalityAIHelper.GetRegistryValue<string>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "DecreaseVision/ArmyVisibleCurveName"), string.Empty);
		if (!string.IsNullOrEmpty(registryValue6))
		{
			this.decreaseVisionArmyVisibleBoostCurve = this.animationCurveDatabase.GetValue(registryValue6);
		}
		this.stealableTechnologiesResult = new List<DecisionResult>[this.game.Empires.Length];
		this.stealTechnologyNotAtMaxEraFactor = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "StealTechnology/NotAtMaxEraFactor"), this.stealTechnologyNotAtMaxEraFactor);
		this.factorForInterestingArmies = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "StealVision/factorForInterestingArmies"), this.factorForInterestingArmies);
		string registryValue7 = this.personalityAIHelper.GetRegistryValue<string>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "Common/MinimalSecurityCurve"), string.Empty);
		if (!string.IsNullOrEmpty(registryValue7))
		{
			this.minimalUtilitySecurityCurve = this.animationCurveDatabase.GetValue(registryValue7);
		}
		this.commonActionLevelFactor = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "Common/ActionLevelFactor"), this.commonActionLevelFactor);
		this.commonActionLevelMaximum = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "Common/ActionLevelMaximum"), this.commonActionLevelMaximum);
		this.referenceInfiltrationTurnCount = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "Common/InfiltrationLevelReferenceTurnCount"), this.referenceInfiltrationTurnCount);
		this.boostInfiltrationLevelFactor = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "Common/InfiltrationLevelFactor"), this.boostInfiltrationLevelFactor);
		this.boostEmpireActionWhenNoCities = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "Common/EmpireActionWhenLowCityCount"), this.boostEmpireActionWhenNoCities);
		this.boostThresholdByHeroHealth = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "Common/BoostThresholdByHeroHealth"), this.boostThresholdByHeroHealth);
		string registryValue8 = this.personalityAIHelper.GetRegistryValue<string>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "HurtingAllies/WantWarCurve"), string.Empty);
		if (!string.IsNullOrEmpty(registryValue8))
		{
			this.hurtingWantWarCurve = this.animationCurveDatabase.GetValue(registryValue8);
		}
		this.boostWantWarScoreFactor = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "HurtingAllies/WantWarFactor"), this.boostWantWarScoreFactor);
		this.decreaseMoralAtWarWithMe = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "DecreaseMoral/AtWarWithMe"), this.decreaseMoralAtWarWithMe);
		this.decreaseMoralAtWarWithAlly = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "DecreaseMoral/AtWarWithAlly"), this.decreaseMoralAtWarWithAlly);
		this.decreaseMoralAtWarWithSomeone = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "DecreaseMoral/AtWarWithSomeone"), this.decreaseMoralAtWarWithSomeone);
		this.decreaseMoralUnitCountMaximum = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "DecreaseMoral/UnitCountMaximum"), this.decreaseMoralUnitCountMaximum);
		this.decreaseMoralUnitCountFactor = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "DecreaseMoral/UnitCountFactor"), this.decreaseMoralUnitCountFactor);
		string registryValue9 = this.personalityAIHelper.GetRegistryValue<string>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "DecreaseMoral/UnitCountCurve"), string.Empty);
		if (!string.IsNullOrEmpty(registryValue9))
		{
			this.decreaseMoralUnitCountCurve = this.animationCurveDatabase.GetValue(registryValue9);
		}
		this.decreaseMoralMilitaryPowerFactor = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "DecreaseMoral/MilitaryPowerFactor"), this.decreaseMoralMilitaryPowerFactor);
		this.decreaseMoralMilitaryPowerClamp = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "DecreaseMoral/MilitaryPowerClamp"), this.decreaseMoralMilitaryPowerClamp);
		string registryValue10 = this.personalityAIHelper.GetRegistryValue<string>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "DecreaseMoral/MilitaryPowerCurve"), string.Empty);
		if (!string.IsNullOrEmpty(registryValue10))
		{
			this.decreaseMoralMilitaryPowerCurve = this.animationCurveDatabase.GetValue(registryValue10);
		}
		this.decreaseProductionFactor = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "DecreaseProduction/IndustryScoreFactor"), this.decreaseMoralMilitaryPowerFactor);
		this.decreaseProductionClamp = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "DecreaseProduction/IndustryScoreRatioClamp"), this.decreaseMoralMilitaryPowerClamp);
		string registryValue11 = this.personalityAIHelper.GetRegistryValue<string>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "DecreaseProduction/IndustryScoreCurve"), string.Empty);
		if (!string.IsNullOrEmpty(registryValue11))
		{
			this.decreaseProductionCurve = this.animationCurveDatabase.GetValue(registryValue11);
		}
		this.decreaseProductionSameWonderBoost = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "DecreaseProduction/SameWonderBoost"), this.decreaseProductionSameWonderBoost);
		this.decreaseProductionUnitDuringWarBoost = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "DecreaseProduction/UnitDuringWarBoost"), this.decreaseProductionUnitDuringWarBoost);
	}

	private float ApplyHurtingBoostOnEmpire(float utility, global::Empire infiltratedEmpire, DebugScoring actionScoring)
	{
		DebugScoring.SubScoring subScoring = null;
		if (actionScoring != null)
		{
			subScoring = new DebugScoring.SubScoring("HurtEnnemy", utility);
		}
		AILayer_Diplomacy layer = base.AIEntity.GetLayer<AILayer_Diplomacy>();
		float wantWarScore = layer.GetWantWarScore(infiltratedEmpire);
		float num = this.boostWantWarScoreFactor * (wantWarScore - 0.5f);
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("WantToDeclareWar", wantWarScore));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("FactorFromXML", this.boostWantWarScoreFactor));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("Boost = (Want - 0.5) * factor", num));
		}
		if (this.hurtingWantWarCurve != null)
		{
			num = this.hurtingWantWarCurve.Evaluate(num);
			if (subScoring != null)
			{
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("BoostWithCurve", num));
			}
		}
		utility = AILayer.Boost(utility, num);
		if (subScoring != null)
		{
			subScoring.GlobalBoost = num;
			subScoring.UtilityAfter = utility;
			actionScoring.SubScorings.Add(subScoring);
		}
		return utility;
	}

	private float ComputeInfiltrationUtility(InfiltrationAction action, AIData_Unit hero, SpiedGarrison spiedGarrison, City infiltratedCity)
	{
		if (infiltratedCity == null || infiltratedCity.Empire == null)
		{
			return 0f;
		}
		if (spiedGarrison == null || spiedGarrison.Empire == null)
		{
			return 0f;
		}
		float num = 0f;
		if (this.utilityFunctions.ContainsKey(action.FirstName))
		{
			num = this.utilityFunctions[action.FirstName](action, hero, spiedGarrison, infiltratedCity, null);
			if (action is InfiltrationActionOnEmpire)
			{
				DepartmentOfTheInterior agency = infiltratedCity.Empire.GetAgency<DepartmentOfTheInterior>();
				if (agency != null && agency.Cities.Count < 2)
				{
					num = AILayer.Boost(num, this.boostEmpireActionWhenNoCities);
				}
			}
			float num2 = (float)action.Level / this.commonActionLevelMaximum;
			float num3 = num2 * this.commonActionLevelFactor;
		}
		return num;
	}

	private float ComputeUtilityThreshold(AIData_Unit hero, SpiedGarrison spiedGarrison, City infiltratedCity, DebugScoring actionScoring)
	{
		float num = this.minimalUtilityBaseValue;
		DebugScoring.SubScoring subScoring = null;
		if (actionScoring != null)
		{
			subScoring = new DebugScoring.SubScoring("CitySecurity", num);
		}
		float propertyValue = infiltratedCity.GetPropertyValue(SimulationProperties.NetCityAntiSpy);
		float num2 = propertyValue / 100f;
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("CitySecurity", propertyValue));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("Boost", num2));
		}
		if (this.minimalUtilitySecurityCurve != null)
		{
			num2 = this.minimalUtilitySecurityCurve.Evaluate(num2);
			if (subScoring != null)
			{
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("BoostAfterCurve", propertyValue));
			}
		}
		else
		{
			num2 *= 0.5f;
			if (subScoring != null)
			{
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("BoostWithoutCurve", propertyValue));
			}
		}
		num = AILayer.Boost(num, num2);
		if (subScoring != null)
		{
			subScoring.UtilityAfter = num;
			subScoring.GlobalBoost = num2;
			actionScoring.SubScorings.Add(subScoring);
			subScoring = new DebugScoring.SubScoring("NextInfLevelTurnCount", num);
		}
		int num3 = this.departmentOfIntelligence.ComputeNumberOfTurnBeforeNextInfiltrationLevel(spiedGarrison);
		float num4 = 1f - Mathf.Clamp01((float)num3 / this.referenceInfiltrationTurnCount);
		num4 *= this.boostInfiltrationLevelFactor;
		num = AILayer.Boost(num, num4);
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("NumberOfTurn", (float)num3));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("ReferenceTurnCount", this.referenceInfiltrationTurnCount));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("FactorFromXML", this.boostInfiltrationLevelFactor));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("Boost", num4));
			subScoring.UtilityAfter = num;
			subScoring.GlobalBoost = num4;
			actionScoring.SubScorings.Add(subScoring);
			subScoring = new DebugScoring.SubScoring("HeroHealth", num);
		}
		float propertyValue2 = hero.Unit.GetPropertyValue(SimulationProperties.Health);
		float propertyValue3 = hero.Unit.GetPropertyValue(SimulationProperties.MaximumHealth);
		float num5 = propertyValue2 / propertyValue3;
		float num6 = this.boostThresholdByHeroHealth * (1f - num5);
		num = AILayer.Boost(num, num6);
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("HealthRatio", num5));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("FactorFromXML", this.boostThresholdByHeroHealth));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("Boost= Factor * (1 - HealthRatio)", num6));
			subScoring.UtilityAfter = num;
			subScoring.GlobalBoost = num6;
			actionScoring.SubScorings.Add(subScoring);
		}
		return num;
	}

	private float InfiltrationActionUtility_DamageFortification(InfiltrationAction action, AIData_Unit hero, SpiedGarrison spiedGarrison, City infiltratedCity, DebugScoring actionScoring)
	{
		float num = 0f;
		DebugScoring.SubScoring subScoring = null;
		if (actionScoring != null)
		{
			subScoring = new DebugScoring.SubScoring("Besieged Score", num);
		}
		if (infiltratedCity.BesiegingEmpire == base.AIEntity.Empire)
		{
			if (subScoring != null)
			{
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("BesiegedByMe", this.damageFortificationBesiegedByMe));
			}
			num = this.damageFortificationBesiegedByMe;
		}
		else if (infiltratedCity.BesiegingEmpire != null)
		{
			DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(infiltratedCity.BesiegingEmpire);
			if (diplomaticRelation != null && diplomaticRelation.State != null && (diplomaticRelation.State.Name == DiplomaticRelationState.Names.Alliance || diplomaticRelation.State.Name == DiplomaticRelationState.Names.Peace))
			{
				if (subScoring != null)
				{
					subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("BesiegedByAllied", this.damageFortificationBesiegedByMe));
				}
				num = this.damageFortificationBesiegedByAlly;
			}
		}
		if (subScoring != null)
		{
			subScoring.UtilityAfter = num;
			actionScoring.SubScorings.Add(subScoring);
		}
		return this.ApplyHurtingBoostOnEmpire(num, infiltratedCity.Empire, actionScoring);
	}

	private float InfiltrationActionUtility_DecreasePopulation(InfiltrationAction action, AIData_Unit hero, SpiedGarrison spiedGarrison, City infiltratedCity, DebugScoring actionScoring)
	{
		float num = 0f;
		DebugScoring.SubScoring subScoring = null;
		float propertyValue = infiltratedCity.GetPropertyValue(SimulationProperties.Population);
		if (propertyValue < 2f)
		{
			return 0f;
		}
		if (actionScoring != null)
		{
			subScoring = new DebugScoring.SubScoring("Population", num);
		}
		float num2 = Mathf.Clamp01(propertyValue / this.decreasePopulationMaximumPopulation);
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("Population", propertyValue));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("PopulationMax", this.decreasePopulationMaximumPopulation));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boost", num2));
		}
		if (this.decreasePopulationCurve != null)
		{
			num2 = this.decreasePopulationCurve.Evaluate(num2);
			if (subScoring != null)
			{
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boostAfterCurve", num2));
			}
		}
		num = AILayer.Boost(num, num2);
		if (subScoring != null)
		{
			subScoring.UtilityAfter = num;
			subScoring.GlobalBoost = num2;
			actionScoring.SubScorings.Add(subScoring);
			subScoring = new DebugScoring.SubScoring("WorkerYield", num);
		}
		float num3 = 0f;
		for (int i = 0; i < this.workerYields.Count; i++)
		{
			float propertyValue2 = infiltratedCity.GetPropertyValue(this.workerYields[i]);
			if (num3 < propertyValue2)
			{
				num3 = propertyValue2;
			}
		}
		float num4 = num3 / this.decreasePopulationMaximumYieldPerPopulation;
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("MaxYieldInCity", num3));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("MaxYieldFromXml", this.decreasePopulationMaximumYieldPerPopulation));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boost", num4));
		}
		if (this.decreasePopulationYieldCurve != null)
		{
			num4 = this.decreasePopulationYieldCurve.Evaluate(num4);
			if (subScoring != null)
			{
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boostAfterCurve", num4));
			}
		}
		num = AILayer.Boost(num, num4);
		if (subScoring != null)
		{
			subScoring.UtilityAfter = num;
			subScoring.GlobalBoost = num4;
			actionScoring.SubScorings.Add(subScoring);
			subScoring = new DebugScoring.SubScoring("MissedPopulation", num);
		}
		num = this.ApplyHurtingBoostOnEmpire(num, infiltratedCity.Empire, actionScoring);
		InfiltrationActionOnCity_DecreasePopulation infiltrationActionOnCity_DecreasePopulation = (InfiltrationActionOnCity_DecreasePopulation)action;
		if (infiltrationActionOnCity_DecreasePopulation != null)
		{
			float num5 = (float)infiltrationActionOnCity_DecreasePopulation.NumberOfPopulation;
			if (propertyValue < num5)
			{
				float num6 = propertyValue / num5;
				float num7 = (1f - num6) * this.decreasePopulationMissedPopulationFactor;
				num = AILayer.Boost(num, num7);
				if (subScoring != null)
				{
					subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("PopulationWhichShouldDie", num5));
					subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("Population", propertyValue));
					subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("Ratio", num6));
					subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("FactorFromXml", this.decreasePopulationMissedPopulationFactor));
					subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boost", num7));
					subScoring.UtilityAfter = num;
					subScoring.GlobalBoost = num7;
					actionScoring.SubScorings.Add(subScoring);
				}
			}
		}
		return num;
	}

	private float InfiltrationActionUtility_DecreaseVision(InfiltrationAction action, AIData_Unit hero, SpiedGarrison spiedGarrison, City infiltratedCity, DebugScoring actionScoring)
	{
		float num = 0f;
		DebugScoring.SubScoring subScoring = null;
		if (actionScoring != null)
		{
			subScoring = new DebugScoring.SubScoring("ArmyVisibleByEnnemy", num);
		}
		float num2 = 0f;
		for (int i = 0; i < this.departmentOfDefense.Armies.Count; i++)
		{
			bool flag = this.departmentOfDefense.Armies[i].GetPropertyValue(SimulationProperties.LevelOfStealth) > 0f;
			bool flag2;
			if (flag)
			{
				flag2 = this.visibilityService.IsWorldPositionDetected(this.departmentOfDefense.Armies[i].WorldPosition, infiltratedCity.Empire.Bits);
			}
			else
			{
				flag2 = this.visibilityService.IsWorldPositionVisible(this.departmentOfDefense.Armies[i].WorldPosition, infiltratedCity.Empire.Bits);
			}
			if (flag2)
			{
				num2 += 1f;
				if (flag)
				{
					num2 += 0.2f;
				}
			}
		}
		float num3 = Mathf.Min(1f, num2 / (float)this.departmentOfDefense.Armies.Count);
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("ArmyVisible", num2));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("ArmyMaxCount", (float)this.departmentOfDefense.Armies.Count));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boost", num3));
		}
		if (this.decreaseVisionArmyVisibleBoostCurve != null)
		{
			num3 = this.decreaseVisionArmyVisibleBoostCurve.Evaluate(num3);
			if (subScoring != null)
			{
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boostAfterCurve", num3));
			}
		}
		num = AILayer.Boost(num, num3);
		if (subScoring != null)
		{
			subScoring.UtilityAfter = num;
			subScoring.GlobalBoost = num3;
			actionScoring.SubScorings.Add(subScoring);
		}
		return this.ApplyHurtingBoostOnEmpire(num, infiltratedCity.Empire, actionScoring);
	}

	private float InfiltrationActionUtility_DiplomaticCostReduction(InfiltrationAction action, AIData_Unit hero, SpiedGarrison spiedGarrison, City infiltratedCity, DebugScoring actionScoring)
	{
		float num = 0.1f;
		DebugScoring.SubScoring subScoring = null;
		if (actionScoring != null)
		{
			subScoring = new DebugScoring.SubScoring("Nothing", num);
		}
		if (subScoring != null)
		{
			subScoring.UtilityAfter = num;
			actionScoring.SubScorings.Add(subScoring);
		}
		return num;
	}

	private float InfiltrationActionUtility_LeechTradeRoutes(InfiltrationAction action, AIData_Unit hero, SpiedGarrison spiedGarrison, City infiltratedCity, DebugScoring actionScoring)
	{
		float num = 0f;
		DebugScoring.SubScoring subScoring = null;
		if (actionScoring != null)
		{
			subScoring = new DebugScoring.SubScoring("TradeRouteCount", num);
		}
		float propertyValue = infiltratedCity.GetPropertyValue(SimulationProperties.NumberOfActiveTradeRoutes);
		float num2 = propertyValue / this.leechMaximumNumberOfTradeRoute;
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("TradeRouteCount", propertyValue));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("TradeRouteMax", this.leechMaximumNumberOfTradeRoute));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boost", num2));
		}
		if (this.leechNumberOfTradeRouteCurve != null)
		{
			num2 = this.leechNumberOfTradeRouteCurve.Evaluate(num2);
			if (subScoring != null)
			{
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boostAfterCurve", num2));
			}
		}
		num = AILayer.Boost(num, num2);
		if (subScoring != null)
		{
			subScoring.UtilityAfter = num;
			subScoring.GlobalBoost = num2;
			actionScoring.SubScorings.Add(subScoring);
		}
		return num;
	}

	private float InfiltrationActionUtility_PoisonGovernor(InfiltrationAction action, AIData_Unit hero, SpiedGarrison spiedGarrison, City infiltratedCity, DebugScoring actionScoring)
	{
		if (infiltratedCity.Hero == null)
		{
			return 0f;
		}
		float num = 0f;
		DebugScoring.SubScoring subScoring = null;
		if (actionScoring != null)
		{
			subScoring = new DebugScoring.SubScoring("HeroAssignation", num);
		}
		float num2 = 0f;
		AIData_Unit aidata_Unit;
		if (this.aiDataRepositoryHelper.TryGetAIData<AIData_Unit>(infiltratedCity.Hero.GUID, out aidata_Unit))
		{
			num2 = Mathf.Max(aidata_Unit.HeroData.LongTermSpecialtyFitness[0], aidata_Unit.HeroData.LongTermSpecialtyFitness[1]);
		}
		float num3 = num2;
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("HeroCityFitness", num2));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boost", num3));
		}
		if (this.poisonGovernorAssignationCurve != null)
		{
			num3 = this.poisonGovernorAssignationCurve.Evaluate(num3);
			if (subScoring != null)
			{
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boostAfterCurve", num3));
			}
		}
		num = AILayer.Boost(num, num3);
		if (subScoring != null)
		{
			subScoring.UtilityAfter = num;
			subScoring.GlobalBoost = num3;
			actionScoring.SubScorings.Add(subScoring);
			subScoring = new DebugScoring.SubScoring("HeroLevel", num);
		}
		float propertyValue = infiltratedCity.Hero.GetPropertyValue(SimulationProperties.Level);
		float num4 = propertyValue / this.poisonGovernorHeroLevelMaximum;
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("HeroLevel", propertyValue));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("HeroLevelMax", this.poisonGovernorHeroLevelMaximum));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boost", num4));
		}
		if (this.poisonGovernorHeroLevelCurve != null)
		{
			num4 = this.poisonGovernorHeroLevelCurve.Evaluate(num4);
			if (subScoring != null)
			{
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boostAfterCurve", num4));
			}
		}
		num = AILayer.Boost(num, num4);
		if (subScoring != null)
		{
			subScoring.UtilityAfter = num;
			subScoring.GlobalBoost = num4;
			actionScoring.SubScorings.Add(subScoring);
		}
		return this.ApplyHurtingBoostOnEmpire(num, infiltratedCity.Empire, actionScoring);
	}

	private float InfiltrationActionUtility_ResearchCost(InfiltrationAction action, AIData_Unit hero, SpiedGarrison spiedGarrison, City infiltratedCity, DebugScoring actionScoring)
	{
		float num = 0f;
		DebugScoring.SubScoring subScoring = null;
		if (actionScoring != null)
		{
			subScoring = new DebugScoring.SubScoring("ResearchScore", num);
		}
		float num2 = this.gameStatisticsManagement.TryReadLastTurnScore(base.AIEntity.Empire, "ScienceScore");
		float num3 = this.gameStatisticsManagement.TryReadLastTurnScore(infiltratedCity.Empire, "ScienceScore");
		float num4 = 0f;
		if (num3 * 1.2f > num2)
		{
			num4 = 0.5f;
		}
		num = AILayer.Boost(num, num4);
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("MyResearch", num2));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("TheirResearch", num3));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boost", num4));
			subScoring.UtilityAfter = num;
			subScoring.GlobalBoost = num4;
			actionScoring.SubScorings.Add(subScoring);
			subScoring = new DebugScoring.SubScoring("ScienceScore", num);
		}
		float num5 = this.gameStatisticsManagement.TryReadLastTurnScore(base.AIEntity.Empire, "SciencePerTurnScore");
		float num6 = this.gameStatisticsManagement.TryReadLastTurnScore(infiltratedCity.Empire, "SciencePerTurnScore");
		num4 = 0f;
		if (num6 * 1.2f > num5)
		{
			num4 = 0.2f;
		}
		num = AILayer.Boost(num, num4);
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("MyScience", num5));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("TheirScience", num6));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boost", num4));
			subScoring.UtilityAfter = num;
			subScoring.GlobalBoost = num4;
			actionScoring.SubScorings.Add(subScoring);
			subScoring = new DebugScoring.SubScoring("AffinityReplicants", num);
		}
		num4 = 0f;
		if (base.AIEntity.Empire.SimulationObject.Tags.Contains("AffinityReplicants"))
		{
			num4 = -0.2f;
		}
		num = AILayer.Boost(num, num4);
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boost", num4));
			subScoring.UtilityAfter = num;
			subScoring.GlobalBoost = num4;
			actionScoring.SubScorings.Add(subScoring);
		}
		return this.ApplyHurtingBoostOnEmpire(num, infiltratedCity.Empire, actionScoring);
	}

	private float InfiltrationActionUtility_Reveal(InfiltrationAction action, AIData_Unit hero, SpiedGarrison spiedGarrison, City infiltratedCity, DebugScoring actionScoring)
	{
		return 0f;
	}

	private float InfiltrationActionUtility_StealTechnology(InfiltrationAction action, AIData_Unit hero, SpiedGarrison spiedGarrison, City infiltratedCity, DebugScoring actionScoring)
	{
		if (this.stealableTechnologiesResult[infiltratedCity.Empire.Index] == null)
		{
			this.stealableTechnologiesResult[infiltratedCity.Empire.Index] = new List<DecisionResult>();
		}
		int eraMin = 1;
		int num = 1;
		InfiltrationActionOnEmpire_StealTechnology infiltrationActionOnEmpire_StealTechnology = action as InfiltrationActionOnEmpire_StealTechnology;
		if (infiltrationActionOnEmpire_StealTechnology != null)
		{
			eraMin = infiltrationActionOnEmpire_StealTechnology.EraMin;
			num = infiltrationActionOnEmpire_StealTechnology.EraMax;
		}
		AILayer_Research layer = base.AIEntity.GetLayer<AILayer_Research>();
		if (this.stealableTechnologiesResult[infiltratedCity.Empire.Index].Count == 0)
		{
			this.stealableTechnologies.Clear();
			DepartmentOfScience.FillStealableTechnology(base.AIEntity.Empire, infiltratedCity.Empire, eraMin, num, ref this.stealableTechnologies);
			if (this.stealableTechnologies.Count == 0)
			{
				this.stealableTechnologiesResult[infiltratedCity.Empire.Index].Add(new DecisionResult(null, null, -1f));
			}
			else
			{
				layer.EvaluateTechnologies(this.stealableTechnologies, ref this.stealableTechnologiesResult[infiltratedCity.Empire.Index]);
			}
		}
		float score = this.stealableTechnologiesResult[infiltratedCity.Empire.Index][0].Score;
		float num2 = layer.GetMostWantedTechnologyScore();
		if (score > num2)
		{
			num2 = score;
		}
		if (num2 == 0f)
		{
			num2 = 10f;
		}
		float num3 = 0f;
		DebugScoring.SubScoring subScoring = null;
		if (actionScoring != null)
		{
			subScoring = new DebugScoring.SubScoring("BestTechnologyScore", num3);
		}
		num3 = score / num2;
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("StealableCount", (float)this.stealableTechnologies.Count));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("BestStealableTechScore", score));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("BestTechScore", num2));
			subScoring.UtilityAfter = num3;
			actionScoring.SubScorings.Add(subScoring);
			subScoring = new DebugScoring.SubScoring("NotAtMaxEraFactor", num3);
		}
		if (this.stealableTechnologiesResult[infiltratedCity.Empire.Index][0].Element != null)
		{
			float num4 = (float)DepartmentOfScience.GetTechnologyEraNumber(this.stealableTechnologiesResult[infiltratedCity.Empire.Index][0].Element as TechnologyDefinition);
			float num5 = num4 / (float)num;
			float num6 = num5 * this.stealTechnologyNotAtMaxEraFactor;
			num3 = AILayer.Boost(num3, num6);
			if (subScoring != null)
			{
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("bestStealableTechnologyEra", num4));
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("MaxEra", (float)num));
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("FactorFromXml", this.stealTechnologyNotAtMaxEraFactor));
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("eraBoost", num6));
				subScoring.UtilityAfter = num3;
				subScoring.GlobalBoost = num6;
				actionScoring.SubScorings.Add(subScoring);
			}
		}
		return num3;
	}

	private float InfiltrationActionUtility_StealVision(InfiltrationAction action, AIData_Unit hero, SpiedGarrison spiedGarrison, City infiltratedCity, DebugScoring actionScoring)
	{
		DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(infiltratedCity.Empire);
		if (diplomaticRelation != null && (diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.MapExchange) || diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.VisionExchange)))
		{
			return 0f;
		}
		float num = 0f;
		DebugScoring.SubScoring subScoring = null;
		if (actionScoring != null)
		{
			subScoring = new DebugScoring.SubScoring("InterestingArmy", num);
		}
		float num2 = 0f;
		DepartmentOfDefense agency = infiltratedCity.Empire.GetAgency<DepartmentOfDefense>();
		for (int i = 0; i < agency.Armies.Count; i++)
		{
			if (!this.visibilityService.IsWorldPositionExploredFor(agency.Armies[i].WorldPosition, base.AIEntity.Empire))
			{
				num2 += 1f;
			}
		}
		if (agency.Armies.Count > 0)
		{
			num2 /= (float)agency.Armies.Count;
		}
		num = num2 * this.factorForInterestingArmies;
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("percentOfArmyInteresting", num2));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("FactorFromXml", this.factorForInterestingArmies));
			subScoring.UtilityAfter = num;
			actionScoring.SubScorings.Add(subScoring);
			subScoring = new DebugScoring.SubScoring("DiplomaticBoost", num);
		}
		float num3 = 0f;
		if (this.departmentOfForeignAffairs.IsAtWarWith(infiltratedCity.Empire))
		{
			num3 = 0.2f;
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("AtWarBoost", num3));
		}
		num = AILayer.Boost(num, num3);
		if (subScoring != null)
		{
			subScoring.UtilityAfter = num;
			subScoring.GlobalBoost = num3;
			actionScoring.SubScorings.Add(subScoring);
		}
		return num;
	}

	private float InfiltrationActionUtility_DecreaseMoral(InfiltrationAction action, AIData_Unit hero, SpiedGarrison spiedGarrison, City infiltratedCity, DebugScoring actionScoring)
	{
		float num = 0.1f;
		DebugScoring.SubScoring subScoring = null;
		if (actionScoring != null)
		{
			subScoring = new DebugScoring.SubScoring("AtWar", num);
		}
		bool flag = this.departmentOfForeignAffairs.IsAtWarWith(infiltratedCity.Empire);
		DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(infiltratedCity.Empire);
		if (flag)
		{
			if (subScoring != null)
			{
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("AtWarWithMeInit", this.damageFortificationBesiegedByMe));
			}
			num = this.decreaseMoralAtWarWithMe;
		}
		else
		{
			bool flag2 = false;
			bool flag3 = false;
			DepartmentOfForeignAffairs agency = infiltratedCity.Empire.GetAgency<DepartmentOfForeignAffairs>();
			foreach (DiplomaticRelation diplomaticRelation2 in agency.DiplomaticRelations)
			{
				if (diplomaticRelation2.OtherEmpireIndex != base.AIEntity.Empire.Index && diplomaticRelation2.OtherEmpireIndex != infiltratedCity.Empire.Index)
				{
					if (diplomaticRelation != null && diplomaticRelation.State != null && diplomaticRelation.State.Name == DiplomaticRelationState.Names.War)
					{
						flag3 = true;
						diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(diplomaticRelation2.OtherEmpireIndex);
						if (diplomaticRelation != null && diplomaticRelation.State != null && diplomaticRelation.State.Name == DiplomaticRelationState.Names.Alliance)
						{
							flag2 = true;
							break;
						}
					}
				}
			}
			if (flag2)
			{
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("AtWarWithAllyInit", this.decreaseMoralAtWarWithAlly));
				num = this.decreaseMoralAtWarWithAlly;
			}
			else if (flag3)
			{
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("AtWarWithSomeoneInit", this.decreaseMoralAtWarWithSomeone));
				num = this.decreaseMoralAtWarWithSomeone;
			}
		}
		if (subScoring != null)
		{
			subScoring.UtilityAfter = num;
			actionScoring.SubScorings.Add(subScoring);
			subScoring = new DebugScoring.SubScoring("UnitCount", num);
		}
		float num2 = 0f;
		AIEmpireData aiempireData;
		if (this.empireDataHelper.TryGet(infiltratedCity.Empire.Index, out aiempireData))
		{
			num2 = (float)aiempireData.MilitaryStandardUnitCount;
		}
		float num3 = num2 / this.decreaseMoralUnitCountMaximum;
		num3 = Mathf.Clamp01(num3);
		float num4 = num3 * this.decreaseMoralUnitCountFactor;
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("UnitCount", num2));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("UnitCountMax", this.decreaseMoralUnitCountMaximum));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("FactorFromXml", this.decreaseMoralUnitCountFactor));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boost", num4));
		}
		if (this.decreaseMoralUnitCountCurve != null)
		{
			num4 = this.decreaseMoralUnitCountCurve.Evaluate(num4);
			if (subScoring != null)
			{
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boostAfterCurve", num4));
			}
		}
		num = AILayer.Boost(num, num4);
		if (subScoring != null)
		{
			subScoring.UtilityAfter = num;
			subScoring.GlobalBoost = num4;
			actionScoring.SubScorings.Add(subScoring);
			subScoring = new DebugScoring.SubScoring("MilitaryPower", num);
		}
		float propertyValue = base.AIEntity.Empire.GetPropertyValue(SimulationProperties.MilitaryPower);
		float propertyValue2 = infiltratedCity.Empire.GetPropertyValue(SimulationProperties.MilitaryPower);
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("MyMilitaryPower", propertyValue));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("InfiltratedMP", propertyValue2));
		}
		num4 = 0f;
		if (propertyValue > 0f)
		{
			float num5 = propertyValue2 / propertyValue;
			num5 = Mathf.Clamp(num5 - 0.5f, -this.decreaseMoralMilitaryPowerClamp, this.decreaseMoralMilitaryPowerClamp);
			num4 = num5 * this.decreaseMoralMilitaryPowerFactor;
			if (subScoring != null)
			{
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("Ratio - 0.5", num5));
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("RatioClamp", this.decreaseMoralMilitaryPowerClamp));
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("FactorFromXml", this.decreaseMoralMilitaryPowerFactor));
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boost", num4));
			}
			if (this.decreaseMoralMilitaryPowerCurve != null)
			{
				num4 = this.decreaseMoralMilitaryPowerCurve.Evaluate(num4);
				if (subScoring != null)
				{
					subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boostAfterCurve", num4));
				}
			}
		}
		num = AILayer.Boost(num, num4);
		if (subScoring != null)
		{
			subScoring.UtilityAfter = num;
			subScoring.GlobalBoost = num4;
			actionScoring.SubScorings.Add(subScoring);
		}
		num = this.ApplyHurtingBoostOnEmpire(num, infiltratedCity.Empire, actionScoring);
		return num;
	}

	private float InfiltrationActionUtility_DecreaseProduction(InfiltrationAction action, AIData_Unit hero, SpiedGarrison spiedGarrison, City infiltratedCity, DebugScoring actionScoring)
	{
		float num = 0.1f;
		DebugScoring.SubScoring subScoring = null;
		if (actionScoring != null)
		{
			subScoring = new DebugScoring.SubScoring("IndustryPerTurnScore", num);
		}
		float num2 = this.gameStatisticsManagement.TryReadLastTurnScore(base.AIEntity.Empire, "IndustryPerTurnScore");
		float num3 = this.gameStatisticsManagement.TryReadLastTurnScore(infiltratedCity.Empire, "IndustryPerTurnScore");
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("myProductionScore", num2));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("theirProductionScore", num3));
		}
		float num4 = 0f;
		if (num2 > 0f)
		{
			float num5 = num3 / num2;
			num5 = Mathf.Clamp(num5 - 0.5f, -this.decreaseProductionClamp, this.decreaseProductionClamp);
			num4 = num5 * this.decreaseProductionFactor;
			if (subScoring != null)
			{
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("Ratio - 0.5", num5));
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("RatioClamp", this.decreaseProductionClamp));
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("FactorFromXml", this.decreaseProductionFactor));
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boost", num4));
			}
			if (this.decreaseProductionCurve != null)
			{
				num4 = this.decreaseProductionCurve.Evaluate(num4);
				if (subScoring != null)
				{
					subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boostAfterCurve", num4));
				}
			}
		}
		num = AILayer.Boost(num, num4);
		if (subScoring != null)
		{
			subScoring.UtilityAfter = num;
			subScoring.GlobalBoost = num4;
			actionScoring.SubScorings.Add(subScoring);
			subScoring = new DebugScoring.SubScoring("SameWonder", num);
		}
		ConstructionQueue constructionQueue = null;
		DepartmentOfIndustry agency = infiltratedCity.Empire.GetAgency<DepartmentOfIndustry>();
		if (agency != null)
		{
			constructionQueue = agency.GetConstructionQueue(infiltratedCity.GUID);
		}
		bool flag = false;
		float num6 = 0f;
		if (constructionQueue != null && constructionQueue.Length > 0)
		{
			for (int i = 0; i < constructionQueue.Length; i++)
			{
				Construction construction = constructionQueue.PeekAt(i);
				flag |= (construction.ConstructibleElement is UnitDesign);
				DistrictImprovementDefinition districtImprovementDefinition = construction.ConstructibleElement as DistrictImprovementDefinition;
				if (districtImprovementDefinition != null && districtImprovementDefinition.OnePerWorld)
				{
					Construction construction2 = this.departmentOfIndustry.GetConstruction(districtImprovementDefinition);
					if (construction2 != null)
					{
						num6 = this.decreaseProductionSameWonderBoost;
						break;
					}
				}
			}
		}
		num = AILayer.Boost(num, num6);
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("Boost", num6));
			subScoring.UtilityAfter = num;
			subScoring.GlobalBoost = num6;
			actionScoring.SubScorings.Add(subScoring);
			subScoring = new DebugScoring.SubScoring("UnitWhileAtWar", num);
		}
		float num7 = 0f;
		if (this.departmentOfForeignAffairs.IsAtWarWith(infiltratedCity.Empire) && flag)
		{
			num7 = this.decreaseProductionUnitDuringWarBoost;
			num = AILayer.Boost(num, num7);
		}
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("Boost", num7));
			subScoring.UtilityAfter = num;
			subScoring.GlobalBoost = num7;
			actionScoring.SubScorings.Add(subScoring);
		}
		return this.ApplyHurtingBoostOnEmpire(num, infiltratedCity.Empire, actionScoring);
	}

	private float InfiltrationActionUtility_StealEmpireMoney(InfiltrationAction action, AIData_Unit hero, SpiedGarrison spiedGarrison, City infiltratedCity, DebugScoring actionScoring)
	{
		float result = 0.1f;
		InfiltrationActionOnEmpire_StealResource infiltrationActionOnEmpire_StealResource = action as InfiltrationActionOnEmpire_StealResource;
		if (infiltrationActionOnEmpire_StealResource != null)
		{
			global::Empire empire = infiltratedCity.Empire;
			global::Empire empire2 = hero.Unit.Garrison.Empire;
			DepartmentOfTheTreasury agency = empire.GetAgency<DepartmentOfTheTreasury>();
			DepartmentOfTheTreasury agency2 = empire2.GetAgency<DepartmentOfTheTreasury>();
			float num = 0f;
			agency.TryGetResourceStockValue(empire.SimulationObject, infiltrationActionOnEmpire_StealResource.ResourceName, out num, false);
			float num2 = 0f;
			agency2.TryGetResourceStockValue(empire2.SimulationObject, infiltrationActionOnEmpire_StealResource.ResourceName, out num2, false);
			float propertyValue = empire2.SimulationObject.GetPropertyValue(SimulationProperties.NetEmpireMoney);
			float val = num * infiltrationActionOnEmpire_StealResource.AmountParameters.TargetStockPercentage + infiltrationActionOnEmpire_StealResource.AmountParameters.BaseAmount;
			float num3 = (float)Math.Floor((double)Math.Min(val, num));
			float num4 = num3 / propertyValue;
			num4 /= 2.5f;
			result = Math.Min(num4, 1f);
		}
		return result;
	}

	public IEnumerable<IAIParameterConverter<InterpreterContext>> GetAIParameterConverters(StaticString aiParameterName)
	{
		foreach (IAIParameterConverter<InterpreterContext> converter in this.constructibleElementEvaluationAIHelper.GetAIParameterConverters(aiParameterName))
		{
			yield return converter;
		}
		yield break;
	}

	public IEnumerable<IAIParameter<InterpreterContext>> GetAIParameters(UnitSkill element)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		foreach (IAIParameter<InterpreterContext> parameter in this.constructibleElementEvaluationAIHelper.GetAIParameters(element))
		{
			yield return parameter;
		}
		yield break;
	}

	public IEnumerable<IAIPrerequisite<InterpreterContext>> GetAIPrerequisites(UnitSkill constructibleElement)
	{
		if (constructibleElement == null)
		{
			throw new ArgumentNullException("constructibleElement");
		}
		yield break;
	}

	public static string[] HeroAssignationTypeNames
	{
		get
		{
			if (AILayer_HeroAssignation.heroAssignationTypeNames == null)
			{
				AILayer_HeroAssignation.heroAssignationTypeNames = Enum.GetNames(typeof(AILayer_HeroAssignation.HeroAssignationType));
			}
			return AILayer_HeroAssignation.heroAssignationTypeNames;
		}
	}

	public TickableState State { get; set; }

	public float GetAssignationScore(IGarrison garrison)
	{
		for (int i = 0; i < this.assignationData.Count; i++)
		{
			if (this.assignationData[i].Garrison.GUID == garrison.GUID)
			{
				return (this.assignationData[i].WantedHeroAIData == null) ? 0f : this.assignationData[i].WantedHeroAIData.HeroData.WantedAssignationFitness;
			}
		}
		return 0f;
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		this.empireSpecialtyNeed = new float[AILayer_HeroAssignation.HeroAssignationTypeNames.Length];
		this.departmentOfDefense = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		this.departmentOfEducation = base.AIEntity.Empire.GetAgency<DepartmentOfEducation>();
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		this.departmentOfIntelligence = base.AIEntity.Empire.GetAgency<DepartmentOfIntelligence>();
		this.departmentOfForeignAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		this.departmentOfTheTreasury = base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>();
		this.departmentOfIndustry = base.AIEntity.Empire.GetAgency<DepartmentOfIndustry>();
		this.departmentOfScience = base.AIEntity.Empire.GetAgency<DepartmentOfScience>();
		this.aiDataRepositoryHelper = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		this.constructibleElementEvaluationAIHelper = AIScheduler.Services.GetService<IConstructibleElementEvaluationAIHelper>();
		this.tickableRepositoryHelper = AIScheduler.Services.GetService<ITickableRepositoryAIHelper>();
		this.tradeDataRepository = AIScheduler.Services.GetService<ITradeDataRepository>();
		this.personalityAIHelper = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		this.visibilityService = gameService.Game.Services.GetService<IVisibilityService>();
		this.animationCurveDatabase = Databases.GetDatabase<Amplitude.Unity.Framework.AnimationCurve>(false);
		this.infiltrationActionDatabase = Databases.GetDatabase<InfiltrationAction>(false);
		this.game = (gameService.Game as global::Game);
		this.tradeManagementService = gameService.Game.Services.GetService<ITradeManagementService>();
		Diagnostics.Assert(this.tradeManagementService != null);
		this.endTurnService = Services.GetService<IEndTurnService>();
		this.downloadableContentService = Services.GetService<IDownloadableContentService>();
		this.tickableRepositoryHelper.Register(this);
		this.levelUpDecisionMaker = new SimulationDecisionMaker<UnitSkill>(this, null);
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_HeroesAssignation_CreateLocalNeedsPass", new AIEntity.AIAction(this.CreateLocalNeeds), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.EvaluateNeeds.ToString(), "AILayer_Heroes_EvaluateNeedsPass", new AIEntity.AIAction(this.EvaluateNeeds), this, new StaticString[]
		{
			"AILayer_Research_EvaluateNeedsPass"
		});
		this.assignationNeedFunctions.Add(AILayer_HeroAssignation.HeroAssignationTypeNames[0], new Func<float>(this.ComputeGovernorCityNeed));
		this.assignationNeedFunctions.Add(AILayer_HeroAssignation.HeroAssignationTypeNames[1], new Func<float>(this.ComputeGovernorEmpireNeed));
		this.assignationNeedFunctions.Add(AILayer_HeroAssignation.HeroAssignationTypeNames[2], new Func<float>(this.ComputeArmySupportNeed));
		this.assignationNeedFunctions.Add(AILayer_HeroAssignation.HeroAssignationTypeNames[3], new Func<float>(this.ComputeArmyHeroNeed));
		this.assignationNeedFunctions.Add(AILayer_HeroAssignation.HeroAssignationTypeNames[4], new Func<float>(this.ComputeSpyNeed));
		this.heroCountBySpecialty = new int[AILayer_HeroAssignation.HeroAssignationTypeNames.Length];
		this.maximalHeroCount = 5f;
		this.maximalTurnForHero = 200f;
		IDatabase<Amplitude.Unity.Framework.AnimationCurve> curveDatabase = Databases.GetDatabase<Amplitude.Unity.Framework.AnimationCurve>(false);
		this.globalAssignationCurve = curveDatabase.GetValue("HeroNeed");
		this.maximalHeroCount = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.RegistryPath, "MaximumHeroCount"), this.maximalHeroCount);
		this.maximalTurnForHero = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.RegistryPath, "MaximumTurnForHero"), this.maximalTurnForHero);
		this.maximumFractionOfHeroesSpy = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.RegistryPath, "MaximumFractionOfHeroesSpy"), this.maximumFractionOfHeroesSpy);
		this.minimalTurnBeforeSpying = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.RegistryPath, "MinimalTurnBeforeSpying"), this.minimalTurnBeforeSpying);
		this.priceMarginToChooseBestHero = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.RegistryPath, "PriceMarginToChooseBestHero"), this.priceMarginToChooseBestHero);
		this.boostForNonSpecialityToChooseBestHero = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.RegistryPath, "BoostForNonSpecialityToChooseBestHero"), this.boostForNonSpecialityToChooseBestHero);
		this.limitToExfiltrate = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.RegistryPath, "LimitToExfiltrate"), this.limitToExfiltrate);
		this.InitializeInfiltrationActionUtilities();
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public bool IsSpyWaitingForVisibilityOn(IGarrison garrison)
	{
		if (garrison.Empire == base.AIEntity.Empire)
		{
			return false;
		}
		for (int i = 0; i < this.assignationData.Count; i++)
		{
			if (this.assignationData[i].Garrison.GUID == garrison.GUID)
			{
				GameEntityGUID x = (this.assignationData[i].WantedHeroAIData == null) ? GameEntityGUID.Zero : this.assignationData[i].WantedHeroAIData.Unit.GUID;
				GameEntityGUID y = (this.assignationData[i].CurrentHeroAIData == null) ? GameEntityGUID.Zero : this.assignationData[i].CurrentHeroAIData.Unit.GUID;
				return x != y && x.IsValid;
			}
		}
		return false;
	}

	public override void Release()
	{
		base.Release();
		if (this.tickableRepositoryHelper != null)
		{
			this.tickableRepositoryHelper.Unregister(this);
			this.tickableRepositoryHelper = null;
		}
		this.utilityFunctions.Clear();
		this.assignationNeedFunctions.Clear();
		this.animationCurveDatabase = null;
		this.infiltrationActionDatabase = null;
		this.visibilityService = null;
		this.endTurnService = null;
		this.downloadableContentService = null;
		this.tradeManagementService = null;
		this.aiDataRepositoryHelper = null;
		this.constructibleElementEvaluationAIHelper = null;
		this.tickableRepositoryHelper = null;
		this.tradeDataRepository = null;
		this.personalityAIHelper = null;
		this.departmentOfDefense = null;
		this.departmentOfEducation = null;
		this.departmentOfTheInterior = null;
		this.departmentOfIntelligence = null;
		this.departmentOfForeignAffairs = null;
		this.departmentOfTheTreasury = null;
		this.departmentOfIndustry = null;
		this.departmentOfScience = null;
		this.levelUpDecisionMaker = null;
		this.game = null;
	}

	public void Tick()
	{
		if (this.orderTicket != null)
		{
			if (!this.orderTicket.Raised)
			{
				return;
			}
			this.orderTicket = null;
		}
		if (this.heroesToLevelUp.Count > 0)
		{
			if (!this.HeroLevelUp(this.heroesToLevelUp[0]))
			{
				this.heroesToLevelUp.RemoveAt(0);
			}
		}
		else if (this.infiltrationActionToPerform.Count > 0)
		{
			for (int i = this.infiltrationActionToPerform.Count - 1; i >= 0; i--)
			{
				if (!this.ExecuteInfiltrationAction(this.infiltrationActionToPerform[i]))
				{
					this.infiltrationActionToPerform.RemoveAt(i);
				}
			}
		}
		else if (this.heroAssignations.Count > 0)
		{
			for (int j = this.heroAssignations.Count - 1; j >= 0; j--)
			{
				if (!this.ExecuteChanges(this.heroAssignations[j]))
				{
					this.heroAssignations.RemoveAt(j);
				}
			}
		}
		else
		{
			this.State = TickableState.NoTick;
		}
	}

	protected override void CreateLocalNeeds(StaticString context, StaticString pass)
	{
		base.CreateLocalNeeds(context, pass);
		this.State = TickableState.NeedTick;
		for (int i = 0; i < this.heroCountBySpecialty.Length; i++)
		{
			this.heroCountBySpecialty[i] = 0;
		}
		this.heroesToLevelUp.Clear();
		for (int j = 0; j < this.departmentOfEducation.Heroes.Count; j++)
		{
			AIData_Unit aidata_Unit;
			if (this.aiDataRepositoryHelper.TryGetAIData<AIData_Unit>(this.departmentOfEducation.Heroes[j].GUID, out aidata_Unit))
			{
				this.heroesToLevelUp.Add(aidata_Unit);
				if (aidata_Unit.HeroData.ChosenSpecialty >= 0)
				{
					this.heroCountBySpecialty[aidata_Unit.HeroData.ChosenSpecialty]++;
				}
			}
		}
		this.GenerateAllPossibleAssignation();
		this.PickHeroOnMarket();
	}

	protected override void EvaluateNeeds(StaticString context, StaticString pass)
	{
		base.EvaluateNeeds(context, pass);
		this.OptimizeHeroAssignation();
		if (this.departmentOfScience.CanTradeHeroes(false))
		{
			IEnumerable<EvaluableMessage_HeroNeed> messages = base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_HeroNeed>(BlackboardLayerID.Empire, (EvaluableMessage_HeroNeed match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending);
			foreach (EvaluableMessage_HeroNeed evaluableMessage_HeroNeed in messages)
			{
				ITradable tradable;
				if (this.tradeManagementService.TryGetTradableByUID(evaluableMessage_HeroNeed.TradableUID, out tradable) && tradable is TradableUnit)
				{
					float dustCost = this.ComputeHeroCostProjection(tradable as TradableUnit);
					evaluableMessage_HeroNeed.UpdateBuyEvaluation("Trade", 0UL, dustCost, (int)BuyEvaluation.MaxTurnGain, 0f, tradable.UID);
				}
			}
		}
		this.GenerateInfiltrationActions();
	}

	private float ComputeArmyHeroNeed()
	{
		float normalizedScore = 0.5f;
		return AILayer.Boost(normalizedScore, this.GetAssignationBestScoreFor(3) - 0.5f);
	}

	private float ComputeArmySupportNeed()
	{
		float normalizedScore = 0.5f;
		return AILayer.Boost(normalizedScore, this.GetAssignationBestScoreFor(2) - 0.5f);
	}

	private float ComputeBoostFromOtherEmpires()
	{
		int num = 0;
		for (int i = 0; i < this.game.Empires.Length; i++)
		{
			DepartmentOfEducation agency = this.game.Empires[i].GetAgency<DepartmentOfEducation>();
			if (agency != null)
			{
				int count = agency.Heroes.Count;
				if (num < count)
				{
					num = count;
				}
			}
		}
		if (this.departmentOfEducation.Heroes.Count < num)
		{
			return (float)(1 - this.departmentOfEducation.Heroes.Count / num);
		}
		return 0f;
	}

	private float ComputeGlobalHeroNeed()
	{
		float xValue = (float)this.endTurnService.Turn / this.maximalTurnForHero;
		float num = this.globalAssignationCurve.Evaluate(xValue);
		float num2 = num * this.maximalHeroCount;
		if (num2 == 0f)
		{
			return 0f;
		}
		float num3 = (float)this.departmentOfEducation.Heroes.Count;
		return Mathf.Max(0f, 1f - num3 / num2);
	}

	private float ComputeGovernorCityNeed()
	{
		int count = this.departmentOfTheInterior.Cities.Count;
		int num = this.heroCountBySpecialty[0] + this.heroCountBySpecialty[1];
		float num2 = Mathf.Max(1f, (float)count * 0.3f);
		if ((float)num >= num2)
		{
			return 0f;
		}
		float normalizedScore = 0.6f;
		return AILayer.Boost(normalizedScore, this.GetAssignationBestScoreFor(0) - 0.5f);
	}

	private float ComputeGovernorEmpireNeed()
	{
		int count = this.departmentOfTheInterior.Cities.Count;
		int num = this.heroCountBySpecialty[0] + this.heroCountBySpecialty[1];
		float num2 = Mathf.Max(1f, (float)count * 0.3f);
		if ((float)num >= num2)
		{
			return 0f;
		}
		float normalizedScore = 0.6f;
		return AILayer.Boost(normalizedScore, this.GetAssignationBestScoreFor(1) - 0.5f);
	}

	private float ComputeHeroCostProjection(TradableUnit tradableUnit)
	{
		Unit unit;
		if (!this.tradeManagementService.TryRetrieveUnit(tradableUnit.GameEntityGUID, out unit))
		{
			return float.MaxValue;
		}
		float num = (float)(unit.Level + 1) * AILayer_HeroAssignation.heroUpkeepProjectionCostByLevel * AILayer_HeroAssignation.heroUpkeepProjectionTurnCount;
		float priceWithSalesTaxes = tradableUnit.GetPriceWithSalesTaxes(TradableTransactionType.Buyout, base.AIEntity.Empire, 1f);
		return priceWithSalesTaxes + num;
	}

	private float ComputeSpyNeed()
	{
		if (!this.downloadableContentService.IsShared(DownloadableContent11.ReadOnlyName))
		{
			return 0f;
		}
		int num = this.heroCountBySpecialty[4];
		float num2 = this.maximumFractionOfHeroesSpy * this.maximalHeroCount;
		if ((float)num >= num2 || (int)num2 <= 0)
		{
			return 0f;
		}
		DepartmentOfForeignAffairs agency = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		bool flag = false;
		for (int i = 0; i < this.game.Empires.Length; i++)
		{
			if (this.game.Empires[i] is MajorEmpire)
			{
				DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(this.game.Empires[i]);
				if (diplomaticRelation != null && diplomaticRelation.State != null && diplomaticRelation.State.Name != DiplomaticRelationState.Names.Unknown)
				{
					flag = true;
					break;
				}
			}
		}
		if (!flag)
		{
			return 0f;
		}
		float normalizedScore = 1f - (float)num / num2;
		return AILayer.Boost(normalizedScore, this.GetAssignationBestScoreFor(4) - 0.5f);
	}

	private bool ExecuteChanges(AIData_Unit heroData)
	{
		GameEntityGUID gameEntityGUID = (heroData.HeroData.WantedHeroAssignation == null) ? GameEntityGUID.Zero : heroData.HeroData.WantedHeroAssignation.Garrison.GUID;
		GameEntityGUID y = (heroData.HeroData.CurrentHeroAssignation == null) ? GameEntityGUID.Zero : heroData.HeroData.CurrentHeroAssignation.Garrison.GUID;
		if (gameEntityGUID != y && gameEntityGUID.IsValid)
		{
			if (heroData.HeroData.WantedHeroAssignation is AssignationData_Spy)
			{
				float num;
				if (this.departmentOfIntelligence == null || !this.departmentOfIntelligence.CanInfiltrate(heroData.Unit, heroData.HeroData.WantedHeroAssignation.Garrison, false, out num, true))
				{
					if (heroData.HeroData.CurrentHeroAssignation is AssignationData_Spy && heroData.Unit.Garrison != null && heroData.HeroData.CurrentAssignationFitness < this.limitToExfiltrate)
					{
						OrderChangeHeroAssignment order = new OrderChangeHeroAssignment(base.AIEntity.Empire.Index, heroData.Unit.GUID, GameEntityGUID.Zero);
						base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out this.orderTicket, null);
					}
					return true;
				}
				OrderToggleInfiltration order2 = new OrderToggleInfiltration(base.AIEntity.Empire.Index, heroData.Unit.GUID, gameEntityGUID, false, true);
				base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order2, out this.orderTicket, null);
			}
			else
			{
				OrderChangeHeroAssignment order3 = new OrderChangeHeroAssignment(base.AIEntity.Empire.Index, heroData.Unit.GUID, gameEntityGUID);
				base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order3, out this.orderTicket, null);
			}
		}
		return false;
	}

	private void GenerateAllPossibleAssignation()
	{
		for (int i = this.assignationData.Count - 1; i >= 0; i--)
		{
			if (!this.assignationData[i].CheckValidity(base.AIEntity.Empire))
			{
				this.assignationData.RemoveAt(i);
			}
			else
			{
				this.assignationData[i].CurrentHeroAIData = null;
				this.assignationData[i].WantedHeroAIData = null;
			}
		}
		int index2;
		for (index2 = 0; index2 < this.departmentOfTheInterior.Cities.Count; index2++)
		{
			if (!this.assignationData.Exists((AssignationData match) => match.Garrison == this.departmentOfTheInterior.Cities[index2]))
			{
				this.assignationData.Add(new AssignationData_City(this.departmentOfTheInterior.Cities[index2]));
			}
		}
		int index;
		for (index = 0; index < this.departmentOfDefense.Armies.Count; index++)
		{
			if (!this.assignationData.Exists((AssignationData match) => match.Garrison == this.departmentOfDefense.Armies[index]))
			{
				AIData_Army aidata_Army;
				if (this.aiDataRepositoryHelper.TryGetAIData<AIData_Army>(this.departmentOfDefense.Armies[index].GUID, out aidata_Army))
				{
					if (!aidata_Army.IsColossus && !aidata_Army.IsSolitary && !aidata_Army.Army.HasCatspaw)
					{
						this.assignationData.Add(new AssignationData_Army(aidata_Army));
					}
				}
			}
		}
		this.GenerateSpyAssignation();
		for (int j = this.assignationData.Count - 1; j >= 0; j--)
		{
			this.assignationData[j].ComputeSpecialtyNeed();
		}
		this.IncreaseAssignationImportance();
		for (int k = this.assignationData.Count - 1; k >= 0; k--)
		{
			this.assignationData[k].ComputeHeroNeed();
		}
		this.assignationData.RemoveAll((AssignationData match) => match.GarrisonHeroNeed < 0f);
	}

	private void GenerateSpyAssignation()
	{
		if (!this.downloadableContentService.IsShared(DownloadableContent11.ReadOnlyName))
		{
			return;
		}
		if ((float)this.endTurnService.Turn < this.minimalTurnBeforeSpying)
		{
			return;
		}
		for (int i = 0; i < this.game.Empires.Length; i++)
		{
			if (i != base.AIEntity.Empire.Index)
			{
				DepartmentOfTheInterior foreignDepartmentOfTheInterior = this.game.Empires[i].GetAgency<DepartmentOfTheInterior>();
				if (foreignDepartmentOfTheInterior != null)
				{
					int cityIndex;
					for (cityIndex = 0; cityIndex < foreignDepartmentOfTheInterior.Cities.Count; cityIndex++)
					{
						if (!this.assignationData.Exists((AssignationData match) => match.Garrison == foreignDepartmentOfTheInterior.Cities[cityIndex]))
						{
							bool flag = false;
							AIData_City aidata_City;
							if (this.aiDataRepositoryHelper.TryGetAIData<AIData_City>(foreignDepartmentOfTheInterior.Cities[cityIndex].GUID, out aidata_City))
							{
								flag = aidata_City.IsCityExploredFor(base.AIEntity.Empire);
							}
							if (flag)
							{
								this.assignationData.Add(new AssignationData_Spy(base.AIEntity.Empire, foreignDepartmentOfTheInterior.Cities[cityIndex]));
							}
						}
					}
				}
			}
		}
	}

	private float GetAssignationBestScoreFor(int specialtyIndex)
	{
		float num = 0f;
		for (int i = 0; i < this.assignationData.Count; i++)
		{
			if (num < this.assignationData[i].GarrisonSpecialtyNeed[specialtyIndex])
			{
				num = this.assignationData[i].GarrisonSpecialtyNeed[specialtyIndex];
			}
		}
		return num;
	}

	private bool HeroLevelUp(AIData_Unit heroData)
	{
		float num = heroData.Unit.GetPropertyValue(SimulationProperties.MaximumSkillPoints) - heroData.Unit.GetPropertyValue(SimulationProperties.SkillPointsSpent);
		if (num > 0f)
		{
			this.unitSkills.Clear();
			DepartmentOfEducation.FillAvailableUnitSkills(heroData.Unit, ref this.unitSkills);
			for (int i = this.unitSkills.Count - 1; i >= 0; i--)
			{
				if (!DepartmentOfEducation.IsUnitSkillUpgradable(heroData.Unit, this.unitSkills[i]))
				{
					this.unitSkills.RemoveAt(i);
				}
			}
			if (this.unitSkills.Count == 0)
			{
				return false;
			}
			this.levelUpDecisionMaker.Context.SimulationObject = heroData.Unit;
			this.levelUpDecisionMaker.UnregisterAllOutput();
			this.levelUpDecisionMaker.RegisterOutput(AILayer_HeroAssignation.heroAssignationTypeNames[heroData.HeroData.ChosenSpecialty]);
			this.decisionResults.Clear();
			this.levelUpDecisionMaker.EvaluateDecisions(this.unitSkills, ref this.decisionResults);
			if (this.decisionResults[0].Score < 0.05f)
			{
				this.levelUpDecisionMaker.UnregisterAllOutput();
				for (int j = 0; j < AILayer_HeroAssignation.heroAssignationTypeNames.Length; j++)
				{
					this.levelUpDecisionMaker.RegisterOutput(AILayer_HeroAssignation.heroAssignationTypeNames[j]);
				}
				this.decisionResults.Clear();
				this.levelUpDecisionMaker.EvaluateDecisions(this.unitSkills, ref this.decisionResults);
			}
			UnitSkill unitSkill = null;
			int unitSkillLevel = 0;
			if (this.decisionResults.Count > 0)
			{
				unitSkill = (this.decisionResults[0].Element as UnitSkill);
				if (heroData.Unit.IsSkillUnlocked(unitSkill.Name))
				{
					unitSkillLevel = heroData.Unit.GetSkillLevel(unitSkill.Name) + 1;
				}
			}
			if (unitSkill != null)
			{
				OrderUnlockUnitSkillLevel order = new OrderUnlockUnitSkillLevel(base.AIEntity.Empire.Index, heroData.Unit.GUID, unitSkill.Name, unitSkillLevel);
				base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out this.orderTicket, null);
				return true;
			}
		}
		return false;
	}

	private void IncreaseAssignationImportance()
	{
	}

	private void OptimizeHeroAssignation()
	{
		this.heroAssignations.Clear();
		for (int i = 0; i < this.departmentOfEducation.Heroes.Count; i++)
		{
			AIData_Unit aidata_Unit;
			if (this.aiDataRepositoryHelper.TryGetAIData<AIData_Unit>(this.departmentOfEducation.Heroes[i].GUID, out aidata_Unit))
			{
				aidata_Unit.HeroData.WantedAssignationFitness = aidata_Unit.HeroData.CurrentAssignationFitness;
				IGarrison assignation = aidata_Unit.Unit.Garrison;
				if (this.departmentOfIntelligence != null)
				{
					InfiltrationProcessus.InfiltrationState heroInfiltrationProcessusState = this.departmentOfIntelligence.GetHeroInfiltrationProcessusState(aidata_Unit.GameEntity.GUID);
					if (heroInfiltrationProcessusState == InfiltrationProcessus.InfiltrationState.OnGoing)
					{
						this.departmentOfIntelligence.TryGetGarrisonForSpy(aidata_Unit.Unit.GUID, out assignation, out heroInfiltrationProcessusState);
					}
				}
				if (assignation != null)
				{
					float num = 0f;
					aidata_Unit.HeroData.CurrentHeroAssignation = this.assignationData.Find((AssignationData match) => match.Garrison.GUID == assignation.GUID);
					if (aidata_Unit.HeroData.CurrentHeroAssignation != null)
					{
						num = aidata_Unit.HeroData.ComputeFitness(aidata_Unit.HeroData.CurrentHeroAssignation.GarrisonSpecialtyNeed);
						aidata_Unit.HeroData.CurrentHeroAssignation.CurrentHeroAIData = aidata_Unit;
						aidata_Unit.HeroData.CurrentHeroAssignation.WantedHeroAIData = aidata_Unit;
					}
					aidata_Unit.HeroData.CurrentAssignationFitness = num;
					aidata_Unit.HeroData.WantedAssignationFitness = num * 1.1f;
					aidata_Unit.HeroData.WantedHeroAssignation = aidata_Unit.HeroData.CurrentHeroAssignation;
				}
				if (DepartmentOfEducation.CanAssignHero(aidata_Unit.Unit))
				{
					this.heroAssignations.Add(aidata_Unit);
				}
			}
		}
		this.heroAssignations.Sort((AIData_Unit left, AIData_Unit right) => -1 * left.HeroData.WantMySpecialtyScore.CompareTo(right.HeroData.WantMySpecialtyScore));
		this.assignationData.Sort((AssignationData left, AssignationData right) => -1 * left.GarrisonHeroNeed.CompareTo(right.GarrisonHeroNeed));
		int num2 = 0;
		bool flag;
		do
		{
			flag = false;
			for (int j = 0; j < this.heroAssignations.Count; j++)
			{
				AIData_Unit aidata_Unit = this.heroAssignations[j];
				for (int k = 0; k < this.assignationData.Count; k++)
				{
					float num3 = aidata_Unit.HeroData.ComputeFitness(this.assignationData[k].GarrisonSpecialtyNeed);
					if (aidata_Unit.HeroData.WantedHeroAssignation == null || num3 > aidata_Unit.HeroData.WantedAssignationFitness)
					{
						if (this.assignationData[k].WantedHeroAIData == null || this.assignationData[k].WantedHeroAIData.HeroData.WantedAssignationFitness < num3)
						{
							if (aidata_Unit.HeroData.WantedHeroAssignation != null)
							{
								aidata_Unit.HeroData.WantedHeroAssignation.WantedHeroAIData = null;
							}
							aidata_Unit.HeroData.WantedHeroAssignation = this.assignationData[k];
							aidata_Unit.HeroData.WantedAssignationFitness = num3;
							if (this.assignationData[k].WantedHeroAIData != null)
							{
								this.assignationData[k].WantedHeroAIData.HeroData.WantedHeroAssignation = null;
							}
							this.assignationData[k].WantedHeroAIData = aidata_Unit;
							flag = true;
						}
					}
				}
			}
			num2++;
		}
		while (flag && num2 < this.assignationData.Count * this.heroAssignations.Count);
		for (int l = this.heroAssignations.Count - 1; l >= 0; l--)
		{
			GameEntityGUID x = (this.heroAssignations[l].HeroData.WantedHeroAssignation == null) ? GameEntityGUID.Zero : this.heroAssignations[l].HeroData.WantedHeroAssignation.Garrison.GUID;
			GameEntityGUID y = (this.heroAssignations[l].HeroData.CurrentHeroAssignation == null) ? GameEntityGUID.Zero : this.heroAssignations[l].HeroData.CurrentHeroAssignation.Garrison.GUID;
			if (x == y)
			{
				this.heroAssignations.RemoveAt(l);
			}
		}
	}

	private void PickHeroOnMarket()
	{
		if (this.departmentOfScience.CanTradeHeroes(false))
		{
			AILayer_AccountManager layer = base.AIEntity.GetLayer<AILayer_AccountManager>();
			layer.SetMaximalAccount(AILayer_AccountManager.HeroAccountName, -1f);
			float num = this.ComputeGlobalHeroNeed();
			num = AILayer.Boost(num, this.ComputeBoostFromOtherEmpires());
			float num2 = 0f;
			int num3 = -1;
			for (int i = 0; i < this.empireSpecialtyNeed.Length; i++)
			{
				this.empireSpecialtyNeed[i] = this.assignationNeedFunctions[AILayer_HeroAssignation.HeroAssignationTypeNames[i]]();
				if (num2 < this.empireSpecialtyNeed[i])
				{
					num2 = this.empireSpecialtyNeed[i];
					num3 = i;
				}
			}
			for (int j = 0; j < this.empireSpecialtyNeed.Length; j++)
			{
				if (num3 != j)
				{
					this.empireSpecialtyNeed[j] = AILayer.Boost(this.empireSpecialtyNeed[j], this.boostForNonSpecialityToChooseBestHero);
				}
			}
			if (num > 0f)
			{
				TradableUnit tradableUnit = null;
				float num4 = float.MaxValue;
				float num5 = 0f;
				int num6 = 1 << base.AIEntity.Empire.Index;
				List<ITradable> list;
				this.tradeManagementService.TryGetTradablesByCategory(TradableUnit.ReadOnlyHeroCategory, out list);
				for (int k = 0; k < list.Count; k++)
				{
					TradableUnit tradableUnit2 = list[k] as TradableUnit;
					if ((tradableUnit2.EmpireExclusionBits & num6) == 0)
					{
						float num7 = 0f;
						AIData_TradableUnit aidata_TradableUnit;
						if (this.tradeDataRepository.TryGetAIData<AIData_TradableUnit>(tradableUnit2.UID, out aidata_TradableUnit))
						{
							num7 = aidata_TradableUnit.HeroData.ComputeFitness(this.empireSpecialtyNeed);
							num7 = AILayer.Boost(num7, 0.3f);
						}
						float num8 = this.ComputeHeroCostProjection(tradableUnit2);
						if (num8 < num4 * this.priceMarginToChooseBestHero && num7 > num5)
						{
							tradableUnit = tradableUnit2;
							num4 = num8;
							num5 = num7;
						}
					}
				}
				if (tradableUnit != null)
				{
					AILayer_Trade.UpdateHeroNeed(num, num5, tradableUnit, base.AIEntity.AIPlayer.Blackboard);
				}
			}
		}
		else
		{
			AILayer_AccountManager layer2 = base.AIEntity.GetLayer<AILayer_AccountManager>();
			layer2.SetMaximalAccount(AILayer_AccountManager.HeroAccountName, 0f);
		}
	}

	private int SortSpyByInfiltrationPoint(AIData_Unit left, AIData_Unit right)
	{
		float num = 0f;
		this.departmentOfTheTreasury.TryGetResourceStockValue(left.Unit.Garrison as SpiedGarrison, DepartmentOfTheTreasury.Resources.InfiltrationPoint, out num, false);
		float value = 0f;
		this.departmentOfTheTreasury.TryGetResourceStockValue(left.Unit.Garrison as SpiedGarrison, DepartmentOfTheTreasury.Resources.InfiltrationPoint, out value, false);
		return -1 * num.CompareTo(value);
	}

	private List<StaticString> failureFlags;

	private List<InfiltrationActionData> infiltrationActionToPerform;

	private static string utilityRegistryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_HeroAssignation/Utility";

	private IAIEmpireDataAIHelper empireDataHelper;

	private float damageFortificationBesiegedByAlly = 0.5f;

	private float damageFortificationBesiegedByMe = 0.8f;

	private Amplitude.Unity.Framework.AnimationCurve decreasePopulationCurve;

	private float decreasePopulationMaximumPopulation = 30f;

	private float decreasePopulationMaximumYieldPerPopulation = 15f;

	private Amplitude.Unity.Framework.AnimationCurve decreasePopulationYieldCurve;

	private float decreasePopulationMissedPopulationFactor = -0.2f;

	private Amplitude.Unity.Framework.AnimationCurve decreaseVisionArmyVisibleBoostCurve;

	private float factorForInterestingArmies = 0.5f;

	private IGameStatisticsManagementService gameStatisticsManagement;

	private Amplitude.Unity.Framework.AnimationCurve hurtingWantWarCurve;

	private float leechMaximumNumberOfTradeRoute = 10f;

	private Amplitude.Unity.Framework.AnimationCurve leechNumberOfTradeRouteCurve;

	private float minimalUtilityBaseValue = 0.05f;

	private Amplitude.Unity.Framework.AnimationCurve minimalUtilitySecurityCurve;

	private Amplitude.Unity.Framework.AnimationCurve poisonGovernorAssignationCurve;

	private Amplitude.Unity.Framework.AnimationCurve poisonGovernorHeroLevelCurve;

	private float poisonGovernorHeroLevelMaximum = 100f;

	private List<DepartmentOfScience.ConstructibleElement> stealableTechnologies = new List<DepartmentOfScience.ConstructibleElement>();

	private List<DecisionResult>[] stealableTechnologiesResult;

	private Dictionary<StaticString, AILayer_HeroAssignation.InfiltrationActionUtilityFunc> utilityFunctions = new Dictionary<StaticString, AILayer_HeroAssignation.InfiltrationActionUtilityFunc>();

	private List<StaticString> workerYields;

	private float referenceInfiltrationTurnCount = 5f;

	private float boostInfiltrationLevelFactor = 0.5f;

	private float boostWantWarScoreFactor = 2f;

	private float boostEmpireActionWhenNoCities = -0.2f;

	private float boostThresholdByHeroHealth = -0.2f;

	private float decreaseMoralAtWarWithMe = 0.8f;

	private float decreaseMoralAtWarWithAlly = 0.5f;

	private float decreaseMoralAtWarWithSomeone = 0.3f;

	private float decreaseMoralUnitCountMaximum = 50f;

	private float decreaseMoralUnitCountFactor = 0.5f;

	private Amplitude.Unity.Framework.AnimationCurve decreaseMoralUnitCountCurve;

	private float decreaseMoralMilitaryPowerFactor = 0.5f;

	private float decreaseMoralMilitaryPowerClamp = 2f;

	private Amplitude.Unity.Framework.AnimationCurve decreaseMoralMilitaryPowerCurve;

	private float decreaseProductionFactor = 0.5f;

	private float decreaseProductionClamp = 2f;

	private Amplitude.Unity.Framework.AnimationCurve decreaseProductionCurve;

	private float decreaseProductionSameWonderBoost = 0.5f;

	private float decreaseProductionUnitDuringWarBoost = 0.5f;

	private float commonActionLevelMaximum = 5f;

	private float commonActionLevelFactor = 0.2f;

	private float stealTechnologyNotAtMaxEraFactor = -0.2f;

	public static string RegistryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_HeroAssignation";

	private static string[] heroAssignationTypeNames;

	private static float heroUpkeepProjectionCostByLevel = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<float>("AI/AILayers/AILayer_Heroes/HeroUpkeepProjectionCostByLevel");

	private static float heroUpkeepProjectionTurnCount = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<float>("AI/AILayers/AILayer_Heroes/HeroUpkeepProjectionTurnCount");

	private IAIDataRepositoryAIHelper aiDataRepositoryHelper;

	private IDatabase<Amplitude.Unity.Framework.AnimationCurve> animationCurveDatabase;

	private List<AssignationData> assignationData = new List<AssignationData>();

	private Dictionary<string, Func<float>> assignationNeedFunctions = new Dictionary<string, Func<float>>();

	private IConstructibleElementEvaluationAIHelper constructibleElementEvaluationAIHelper;

	private List<DecisionResult> decisionResults = new List<DecisionResult>();

	private DepartmentOfDefense departmentOfDefense;

	private DepartmentOfEducation departmentOfEducation;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private DepartmentOfIntelligence departmentOfIntelligence;

	private DepartmentOfScience departmentOfScience;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private DepartmentOfIndustry departmentOfIndustry;

	private float[] empireSpecialtyNeed;

	private IEndTurnService endTurnService;

	private global::Game game;

	private Amplitude.Unity.Framework.AnimationCurve globalAssignationCurve;

	private List<AIData_Unit> heroAssignations = new List<AIData_Unit>();

	private int[] heroCountBySpecialty;

	private List<AIData_Unit> heroesToLevelUp = new List<AIData_Unit>();

	private IDatabase<InfiltrationAction> infiltrationActionDatabase;

	private SimulationDecisionMaker<UnitSkill> levelUpDecisionMaker;

	private float maximalHeroCount;

	private float maximalTurnForHero;

	private float maximumFractionOfHeroesSpy = 0.3f;

	private Ticket orderTicket;

	private IPersonalityAIHelper personalityAIHelper;

	private ITickableRepositoryAIHelper tickableRepositoryHelper;

	private ITradeDataRepository tradeDataRepository;

	private ITradeManagementService tradeManagementService;

	private List<UnitSkill> unitSkills = new List<UnitSkill>();

	private IVisibilityService visibilityService;

	private IDownloadableContentService downloadableContentService;

	private float minimalTurnBeforeSpying = 10f;

	private float priceMarginToChooseBestHero = 1.2f;

	private float boostForNonSpecialityToChooseBestHero = -0.5f;

	private float limitToExfiltrate = 0.2f;

	public enum HeroAssignationType
	{
		GovernorCity,
		GovernorEmpire,
		ArmySupport,
		ArmyHero,
		Spy
	}

	protected delegate float InfiltrationActionUtilityFunc(InfiltrationAction action, AIData_Unit hero, SpiedGarrison spiedGarrison, City infiltratedCity, DebugScoring actionScoring);
}
