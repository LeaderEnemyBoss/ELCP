﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
		this.damageFortificationBesiegedByAlly = 0.5f;
		this.damageFortificationBesiegedByMe = 0.8f;
		this.decreasePopulationMaximumPopulation = 30f;
		this.decreasePopulationMaximumYieldPerPopulation = 15f;
		this.decreasePopulationMissedPopulationFactor = -0.2f;
		this.factorForInterestingArmies = 0.5f;
		this.leechMaximumNumberOfTradeRoute = 10f;
		this.minimalUtilityBaseValue = 0.2f;
		this.poisonGovernorHeroLevelMaximum = 100f;
		this.stealableTechnologies = new List<DepartmentOfScience.ConstructibleElement>();
		this.utilityFunctions = new Dictionary<StaticString, AILayer_HeroAssignation.InfiltrationActionUtilityFunc>();
		this.referenceInfiltrationTurnCount = 5f;
		this.boostInfiltrationLevelFactor = 0.5f;
		this.boostWantWarScoreFactor = 2f;
		this.boostEmpireActionWhenNoCities = -0.2f;
		this.boostThresholdByHeroHealth = -0.2f;
		this.decreaseMoralAtWarWithMe = 0.8f;
		this.decreaseMoralAtWarWithAlly = 0.5f;
		this.decreaseMoralAtWarWithSomeone = 0.3f;
		this.decreaseMoralUnitCountMaximum = 50f;
		this.decreaseMoralUnitCountFactor = 0.5f;
		this.decreaseMoralMilitaryPowerFactor = 0.5f;
		this.decreaseMoralMilitaryPowerClamp = 2f;
		this.decreaseProductionFactor = 0.5f;
		this.decreaseProductionClamp = 2f;
		this.decreaseProductionSameWonderBoost = 0.5f;
		this.decreaseProductionUnitDuringWarBoost = 0.5f;
		this.commonActionLevelMaximum = 5f;
		this.commonActionLevelFactor = 0.2f;
		this.stealTechnologyNotAtMaxEraFactor = -0.2f;
		this.assignationData = new List<AssignationData>();
		this.assignationNeedFunctions = new Dictionary<string, Func<float>>();
		this.decisionResults = new List<DecisionResult>();
		this.heroAssignations = new List<AIData_Unit>();
		this.heroesToLevelUp = new List<AIData_Unit>();
		this.maximumFractionOfHeroesSpy = 0.3f;
		this.unitSkills = new List<UnitSkill>();
		this.minimalTurnBeforeSpying = 10f;
		this.priceMarginToChooseBestHero = 1.2f;
		this.boostForNonSpecialityToChooseBestHero = -0.5f;
		this.limitToExfiltrate = 0.2f;
		this.failureFlags = new List<StaticString>();
		this.infiltrationActionToPerform = new List<InfiltrationActionData>();
		this.VisibleSpyGarrisons = new List<GameEntityGUID>();
		this.DebugInfiltrationActionInfo = new List<InfiltrationActionData>();
		this.joblessHeros = new Dictionary<GameEntityGUID, int>();
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
		Predicate<InfiltrationAction> <>9__0;
		foreach (InfiltrationAction action2 in this.infiltrationActionDatabase)
		{
			action = action2;
			this.failureFlags.Clear();
			if (action.CanExecute(context, ref this.failureFlags, new object[0]))
			{
				List<InfiltrationAction> list2 = list;
				Predicate<InfiltrationAction> match2;
				if ((match2 = <>9__0) == null)
				{
					match2 = (<>9__0 = ((InfiltrationAction match) => match.FirstName == action.FirstName));
				}
				int num2 = list2.FindIndex(match2);
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
		int num3 = 0;
		int num4 = 0;
		this.departmentOfIntelligence.GetHeroinfiltrationLevel(hero.Unit, out num3, out num4);
		if (AILayer_War.IsWarTarget(base.AIEntity, garrison as City))
		{
			num = 1f;
		}
		else if (num3 == num4)
		{
			num = 0f;
		}
		else if (base.AIEntity.Empire.SimulationObject.Tags.Contains("FactionTraitReplicants4"))
		{
			for (int i = 0; i < list.Count; i++)
			{
				InfiltrationActionOnEmpire_StealTechnology infiltrationActionOnEmpire_StealTechnology = list[i] as InfiltrationActionOnEmpire_StealTechnology;
				if (infiltrationActionOnEmpire_StealTechnology != null)
				{
					int currentTechnologyEraNumber = base.AIEntity.Empire.GetAgency<DepartmentOfScience>().CurrentTechnologyEraNumber;
					int currentTechnologyEraNumber2 = garrison.Empire.GetAgency<DepartmentOfScience>().CurrentTechnologyEraNumber;
					List<DepartmentOfScience.ConstructibleElement> list3 = new List<DepartmentOfScience.ConstructibleElement>();
					DepartmentOfScience.FillStealableTechnology(base.AIEntity.Empire, garrison.Empire, 1, currentTechnologyEraNumber2, ref list3);
					if (list3.Count == 0)
					{
						break;
					}
					if (currentTechnologyEraNumber <= currentTechnologyEraNumber2 && infiltrationActionOnEmpire_StealTechnology.EraMax < currentTechnologyEraNumber2)
					{
						num = AILayer.Boost(num, this.BoostThresholdForBetterTechsteal);
					}
				}
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			InfiltrationAction infiltrationAction = list[j];
			float num5 = this.ComputeInfiltrationUtility(infiltrationAction, hero, hero.Unit.Garrison as SpiedGarrison, garrison as City);
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				InfiltrationActionData infiltrationActionData = new InfiltrationActionData();
				infiltrationActionData.ChosenActionName = infiltrationAction.Name;
				infiltrationActionData.ChosenActionFirstName = infiltrationAction.FirstName;
				infiltrationActionData.ChosenActionUtility = num5;
				infiltrationActionData.HeroGuid = hero.Unit.GUID;
				infiltrationActionData.SpiedGarrisonGuid = hero.Unit.Garrison.GUID;
				infiltrationActionData.UtilityThreshold = num;
				this.DebugInfiltrationActionInfo.Add(infiltrationActionData);
				Diagnostics.Log("ELCP: Empire {0}, Hero {1} {5} Infiltration Action {2} has utility {3}, Threshold: {4}.", new object[]
				{
					base.AIEntity.Empire.ToString(),
					hero.Unit.UnitDesign.LocalizedName,
					infiltrationAction.Name,
					num5,
					num,
					infiltrationActionData.HeroGuid
				});
			}
			if (num5 >= num)
			{
				InfiltrationActionData infiltrationActionData2 = new InfiltrationActionData();
				infiltrationActionData2.ChosenActionName = infiltrationAction.Name;
				infiltrationActionData2.ChosenActionFirstName = infiltrationAction.FirstName;
				infiltrationActionData2.ChosenActionUtility = num5;
				infiltrationActionData2.HeroGuid = hero.Unit.GUID;
				infiltrationActionData2.SpiedGarrisonGuid = hero.Unit.Garrison.GUID;
				infiltrationActionData2.UtilityThreshold = num;
				this.infiltrationActionToPerform.Add(infiltrationActionData2);
			}
		}
	}

	private void FilterActionToPerform()
	{
		Services.GetService<IGameService>().Game.Services.GetService<IGameEntityRepositoryService>();
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
				if (value is InfiltrationActionOnEmpire && !(value is InfiltrationActionOnEmpire_StealTechnology))
				{
					IGarrison garrison;
					this.departmentOfIntelligence.TryGetGarrisonForSpy(this.infiltrationActionToPerform[i].HeroGuid, out garrison);
					for (int j = 0; j < this.infiltrationActionToPerform.Count; j++)
					{
						if (!(this.infiltrationActionToPerform[i].HeroGuid == this.infiltrationActionToPerform[j].HeroGuid) && !(this.infiltrationActionToPerform[i].ChosenActionName != this.infiltrationActionToPerform[j].ChosenActionName))
						{
							IGarrison garrison2;
							this.departmentOfIntelligence.TryGetGarrisonForSpy(this.infiltrationActionToPerform[j].HeroGuid, out garrison2);
							if (garrison.Empire == garrison2.Empire)
							{
								this.infiltrationActionToPerform.RemoveAt(j);
								j--;
							}
						}
					}
				}
				EvaluableMessage_InfiltrationAction evaluableMessage_InfiltrationAction = base.AIEntity.AIPlayer.Blackboard.FindFirst<EvaluableMessage_InfiltrationAction>(BlackboardLayerID.Empire, (EvaluableMessage_InfiltrationAction match) => match != null && match.HeroGuid == chosenAction.HeroGuid);
				if (evaluableMessage_InfiltrationAction == null || evaluableMessage_InfiltrationAction.State != BlackboardMessage.StateValue.Message_InProgress)
				{
					evaluableMessage_InfiltrationAction = new EvaluableMessage_InfiltrationAction(value.Name, chosenAction.HeroGuid);
					base.AIEntity.AIPlayer.Blackboard.AddMessage(evaluableMessage_InfiltrationAction);
				}
				evaluableMessage_InfiltrationAction.TimeOut = 1;
				evaluableMessage_InfiltrationAction.InfiltrationActionName = value.Name;
				evaluableMessage_InfiltrationAction.SetInterest(0.5f, chosenAction.ChosenActionUtility);
				InfiltrationAction infiltrationAction;
				if (this.infiltrationActionDatabase.TryGetValue(chosenAction.ChosenActionName, out infiltrationAction) && (infiltrationAction.Level == 5 || infiltrationAction.FirstName == "StealTechnology"))
				{
					evaluableMessage_InfiltrationAction.SetInterest(1f, 1f);
				}
				InterpreterContext context = InfiltrationAction.CreateContext(base.AIEntity.Empire, chosenAction.SpiedGarrisonGuid);
				value.ComputeConstructionCost(context);
				float num = 0f;
				for (int k = 0; k < InfiltrationAction.Context.ConstructionCosts.Length; k++)
				{
					if (InfiltrationAction.Context.ConstructionCosts[k].ResourceName == DepartmentOfTheTreasury.Resources.EmpirePoint)
					{
						num += InfiltrationAction.Context.ConstructionCosts[k].Value;
					}
				}
				evaluableMessage_InfiltrationAction.UpdateBuyEvaluation("InfiltrationAction", 0UL, num, 2, 0f, 0UL);
			}
		}
	}

	private bool ExecuteInfiltrationAction(InfiltrationActionData infiltrationActionData)
	{
		AIData_Unit aidata_Unit;
		this.aiDataRepositoryHelper.TryGetAIData<AIData_Unit>(infiltrationActionData.HeroGuid, out aidata_Unit);
		Diagnostics.Log("ELCP: Empire{0}, Hero {1} tries infiltrationaction {2}", new object[]
		{
			base.AIEntity.Empire.ToString(),
			aidata_Unit.Unit.UnitDesign.LocalizedName,
			infiltrationActionData.ChosenActionName
		});
		InfiltrationAction infiltrationAction;
		if (this.infiltrationActionDatabase.TryGetValue(infiltrationActionData.ChosenActionName, out infiltrationAction))
		{
			EvaluableMessage_InfiltrationAction evaluableMessage_InfiltrationAction = base.AIEntity.AIPlayer.Blackboard.FindFirst<EvaluableMessage_InfiltrationAction>(BlackboardLayerID.Empire, (EvaluableMessage_InfiltrationAction match) => match != null && match.HeroGuid == infiltrationActionData.HeroGuid);
			IGarrison garrison;
			if (evaluableMessage_InfiltrationAction != null && evaluableMessage_InfiltrationAction.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate && this.departmentOfIntelligence.TryGetGarrisonForSpy(infiltrationActionData.HeroGuid, out garrison))
			{
				if (infiltrationAction is InfiltrationActionOnEmpire_StealTechnology)
				{
					int eraMax = (infiltrationAction as InfiltrationActionOnEmpire_StealTechnology).EraMax;
					int num = 0;
					bool flag = false;
					for (int i = 0; i < this.stealableTechnologiesResult[garrison.Empire.Index].Count; i++)
					{
						TechnologyDefinition technologyDefinition = this.stealableTechnologiesResult[garrison.Empire.Index][i].Element as TechnologyDefinition;
						if (technologyDefinition != null && DepartmentOfScience.GetTechnologyEraNumber(technologyDefinition) <= eraMax)
						{
							if (this.departmentOfScience.GetTechnologyState(technologyDefinition) != DepartmentOfScience.ConstructibleElement.State.Researched)
							{
								Diagnostics.Log("ELCP: Empire{0}, Hero {1} steals tech {2}", new object[]
								{
									base.AIEntity.Empire.ToString(),
									aidata_Unit.Unit.UnitDesign.LocalizedName,
									technologyDefinition.Name
								});
								flag = true;
								break;
							}
							Diagnostics.Log("ELCP: Empire{0}, Hero {1} technology {2} already researched", new object[]
							{
								base.AIEntity.Empire.ToString(),
								aidata_Unit.Unit.UnitDesign.LocalizedName,
								technologyDefinition.Name
							});
							num++;
						}
						else
						{
							num++;
						}
					}
					if (flag)
					{
						infiltrationAction.Execute(garrison as City, base.AIEntity.Empire.PlayerControllers.AI, out this.orderTicket, null, new object[]
						{
							this.stealableTechnologiesResult[garrison.Empire.Index][num].Element
						});
						this.stealableTechnologiesResult[garrison.Empire.Index].RemoveAt(num);
					}
					else
					{
						Diagnostics.Log("ELCP: Empire{0}, Hero {1} found no valid tech to steal", new object[]
						{
							base.AIEntity.Empire.ToString(),
							aidata_Unit.Unit.UnitDesign.LocalizedName
						});
					}
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
				this.stealableTechnologiesIndex[i] = 0;
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
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			this.DebugInfiltrationActionInfo.Clear();
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
		this.stealableTechnologiesIndex = new int[this.game.Empires.Length];
		this.stealTechnologyNotAtMaxEraFactor = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "StealTechnology/NotAtMaxEraFactor"), this.stealTechnologyNotAtMaxEraFactor);
		this.BoostThresholdForBetterTechsteal = 0.9f;
		this.BoostThresholdForBetterTechsteal = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.utilityRegistryPath, "StealTechnology/BoostThresholdForBetterTechsteal"), this.BoostThresholdForBetterTechsteal);
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
		float num = this.DiplomacyLayer.GetWantWarScore(infiltratedEmpire);
		if (this.departmentOfForeignAffairs.IsAtWarWith(infiltratedEmpire))
		{
			num = 0.7f;
		}
		float num2 = this.boostWantWarScoreFactor * (num - 0.5f);
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("WantToDeclareWar", num));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("FactorFromXML", this.boostWantWarScoreFactor));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("Boost = (Want - 0.5) * factor", num2));
		}
		if (this.hurtingWantWarCurve != null)
		{
			num2 = this.hurtingWantWarCurve.Evaluate(num2);
			if (subScoring != null)
			{
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("BoostWithCurve", num2));
			}
		}
		utility = AILayer.Boost(utility, num2);
		if (subScoring != null)
		{
			subScoring.GlobalBoost = num2;
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
			float num3 = this.commonActionLevelFactor;
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
		if (this.DiplomacyLayer.GetPeaceWish(infiltratedCity.Empire.Index))
		{
			return 0f;
		}
		DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(infiltratedCity.Empire);
		if (AILayer_War.IsWarTarget(base.AIEntity, infiltratedCity) || (diplomaticRelation != null && diplomaticRelation.State != null && diplomaticRelation.State.Name == DiplomaticRelationState.Names.War))
		{
			using (IEnumerator<Army> enumerator = Intelligence.GetArmiesInRegion(infiltratedCity.Region.Index).GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.Empire == base.AIEntity.Empire && infiltratedCity.GetPropertyValue(SimulationProperties.CityDefensePoint) > 90f)
					{
						return 1f;
					}
				}
			}
		}
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
			diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(infiltratedCity.BesiegingEmpire);
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
		if (this.DiplomacyLayer.GetPeaceWish(infiltratedCity.Empire.Index))
		{
			return 0f;
		}
		float num = 0f;
		DebugScoring.SubScoring subScoring = null;
		float propertyValue = infiltratedCity.GetPropertyValue(SimulationProperties.Population);
		int num2;
		int num3;
		this.departmentOfIntelligence.GetHeroinfiltrationLevel(hero.Unit, out num2, out num3);
		if (((!infiltratedCity.Empire.SimulationObject.Tags.Contains(DepartmentOfTheInterior.FactionTraitBuyOutPopulation) && propertyValue < 13f) | propertyValue < 10f) && num2 < num3)
		{
			return 0f;
		}
		if (actionScoring != null)
		{
			subScoring = new DebugScoring.SubScoring("Population", num);
		}
		float num4 = Mathf.Clamp01(propertyValue / this.decreasePopulationMaximumPopulation);
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("Population", propertyValue));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("PopulationMax", this.decreasePopulationMaximumPopulation));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boost", num4));
		}
		if (this.decreasePopulationCurve != null)
		{
			num4 = this.decreasePopulationCurve.Evaluate(num4);
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
			subScoring = new DebugScoring.SubScoring("WorkerYield", num);
		}
		float num5 = 0f;
		for (int i = 0; i < this.workerYields.Count; i++)
		{
			float propertyValue2 = infiltratedCity.GetPropertyValue(this.workerYields[i]);
			if (num5 < propertyValue2)
			{
				num5 = propertyValue2;
			}
		}
		float num6 = num5 / this.decreasePopulationMaximumYieldPerPopulation;
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("MaxYieldInCity", num5));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("MaxYieldFromXml", this.decreasePopulationMaximumYieldPerPopulation));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boost", num6));
		}
		if (this.decreasePopulationYieldCurve != null)
		{
			num6 = this.decreasePopulationYieldCurve.Evaluate(num6);
			if (subScoring != null)
			{
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boostAfterCurve", num6));
			}
		}
		num = AILayer.Boost(num, num6);
		if (subScoring != null)
		{
			subScoring.UtilityAfter = num;
			subScoring.GlobalBoost = num6;
			actionScoring.SubScorings.Add(subScoring);
			subScoring = new DebugScoring.SubScoring("MissedPopulation", num);
		}
		num = this.ApplyHurtingBoostOnEmpire(num, infiltratedCity.Empire, actionScoring);
		InfiltrationActionOnCity_DecreasePopulation infiltrationActionOnCity_DecreasePopulation = (InfiltrationActionOnCity_DecreasePopulation)action;
		if (infiltrationActionOnCity_DecreasePopulation != null)
		{
			float num7 = (float)infiltrationActionOnCity_DecreasePopulation.NumberOfPopulation;
			if (propertyValue < num7)
			{
				float num8 = propertyValue / num7;
				float num9 = (1f - num8) * this.decreasePopulationMissedPopulationFactor;
				num = AILayer.Boost(num, num9);
				if (subScoring != null)
				{
					subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("PopulationWhichShouldDie", num7));
					subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("Population", propertyValue));
					subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("Ratio", num8));
					subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("FactorFromXml", this.decreasePopulationMissedPopulationFactor));
					subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boost", num9));
					subScoring.UtilityAfter = num;
					subScoring.GlobalBoost = num9;
					actionScoring.SubScorings.Add(subScoring);
				}
			}
		}
		return num;
	}

	private float InfiltrationActionUtility_DecreaseVision(InfiltrationAction action, AIData_Unit hero, SpiedGarrison spiedGarrison, City infiltratedCity, DebugScoring actionScoring)
	{
		if (this.DiplomacyLayer.GetPeaceWish(infiltratedCity.Empire.Index))
		{
			return 0f;
		}
		DepartmentOfPlanificationAndDevelopment agency = infiltratedCity.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
		if (agency.GetActiveBooster("BoosterDecreaseWatchtowerVisionByInfiltration") != null || agency.GetActiveBooster("BoosterDecreaseBuildingsAndCitiesVisionByInfiltration") != null || agency.GetActiveBooster("BoosterDecreaseVisionByInfiltration") != null)
		{
			return 0f;
		}
		if (action.Level == 5 && this.departmentOfForeignAffairs.IsAtWarWith(infiltratedCity.Empire))
		{
			return 0.999f;
		}
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
		if (this.departmentOfPlanificationAndDevelopment.GetActiveBooster(string.Format("BoosterDiplomaticCostReductionByInfiltration{0}{1}", infiltratedCity.Empire.Index, base.AIEntity.Empire.Index)) != null)
		{
			return 0f;
		}
		if (this.DiplomacyLayer.GetPeaceWish(infiltratedCity.Empire.Index))
		{
			return 0.25f + (float)action.Level * 0.1f;
		}
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
		if (this.DiplomacyLayer.GetPeaceWish(infiltratedCity.Empire.Index))
		{
			return 0f;
		}
		if (infiltratedCity.Hero == null)
		{
			return 0f;
		}
		int num;
		int num2;
		this.departmentOfIntelligence.GetHeroinfiltrationLevel(hero.Unit, out num, out num2);
		if (num < num2 && infiltratedCity.Empire.SimulationObject.GetPropertyValue(SimulationProperties.NetEmpireMoney) > 100f)
		{
			return 0f;
		}
		float num3 = 0f;
		DebugScoring.SubScoring subScoring = null;
		if (actionScoring != null)
		{
			subScoring = new DebugScoring.SubScoring("HeroAssignation", num3);
		}
		float num4 = 0f;
		AIData_Unit aidata_Unit;
		if (this.aiDataRepositoryHelper.TryGetAIData<AIData_Unit>(infiltratedCity.Hero.GUID, out aidata_Unit))
		{
			num4 = Mathf.Max(aidata_Unit.HeroData.LongTermSpecialtyFitness[0], aidata_Unit.HeroData.LongTermSpecialtyFitness[1]);
		}
		float num5 = num4;
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("HeroCityFitness", num4));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boost", num5));
		}
		if (this.poisonGovernorAssignationCurve != null)
		{
			num5 = this.poisonGovernorAssignationCurve.Evaluate(num5);
			if (subScoring != null)
			{
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boostAfterCurve", num5));
			}
		}
		num3 = AILayer.Boost(num3, num5);
		if (subScoring != null)
		{
			subScoring.UtilityAfter = num3;
			subScoring.GlobalBoost = num5;
			actionScoring.SubScorings.Add(subScoring);
			subScoring = new DebugScoring.SubScoring("HeroLevel", num3);
		}
		float propertyValue = infiltratedCity.Hero.GetPropertyValue(SimulationProperties.Level);
		float num6 = propertyValue / this.poisonGovernorHeroLevelMaximum;
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("HeroLevel", propertyValue));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("HeroLevelMax", this.poisonGovernorHeroLevelMaximum));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boost", num6));
		}
		if (this.poisonGovernorHeroLevelCurve != null)
		{
			num6 = this.poisonGovernorHeroLevelCurve.Evaluate(num6);
			if (subScoring != null)
			{
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boostAfterCurve", num6));
			}
		}
		num3 = AILayer.Boost(num3, num6);
		if (subScoring != null)
		{
			subScoring.UtilityAfter = num3;
			subScoring.GlobalBoost = num6;
			actionScoring.SubScorings.Add(subScoring);
		}
		return this.ApplyHurtingBoostOnEmpire(num3, infiltratedCity.Empire, actionScoring);
	}

	private float InfiltrationActionUtility_ResearchCost(InfiltrationAction action, AIData_Unit hero, SpiedGarrison spiedGarrison, City infiltratedCity, DebugScoring actionScoring)
	{
		if (this.DiplomacyLayer.GetPeaceWish(infiltratedCity.Empire.Index))
		{
			return 0f;
		}
		if (infiltratedCity.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>().GetActiveBooster("BoosterResearchCostByInfiltration2") != null)
		{
			return 0f;
		}
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
			num4 = 0.4f;
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
			num4 = 0.1f;
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
		return 0.25f;
	}

	private float InfiltrationActionUtility_StealTechnology(InfiltrationAction action, AIData_Unit hero, SpiedGarrison spiedGarrison, City infiltratedCity, DebugScoring actionScoring)
	{
		if (this.stealableTechnologiesResult[infiltratedCity.Empire.Index] == null)
		{
			this.stealableTechnologiesResult[infiltratedCity.Empire.Index] = new List<DecisionResult>();
		}
		int num = Mathf.Max(2, base.AIEntity.Empire.GetAgency<DepartmentOfScience>().CurrentTechnologyEraNumber - 2);
		int num2 = 2;
		int num3 = 2;
		InfiltrationActionOnEmpire_StealTechnology infiltrationActionOnEmpire_StealTechnology = action as InfiltrationActionOnEmpire_StealTechnology;
		if (infiltrationActionOnEmpire_StealTechnology != null)
		{
			num = Mathf.Max(num, infiltrationActionOnEmpire_StealTechnology.EraMin);
			if (infiltratedCity.Empire.GetAgency<DepartmentOfScience>().CurrentTechnologyEraNumber < num3 || infiltrationActionOnEmpire_StealTechnology.EraMax < num2)
			{
				return 0f;
			}
			num2 = infiltrationActionOnEmpire_StealTechnology.EraMax;
			num3 = infiltratedCity.Empire.GetAgency<DepartmentOfScience>().CurrentTechnologyEraNumber;
		}
		AILayer_Research layer = base.AIEntity.GetLayer<AILayer_Research>();
		if (this.stealableTechnologiesResult[infiltratedCity.Empire.Index].Count == 0)
		{
			this.stealableTechnologies.Clear();
			DepartmentOfScience.FillStealableTechnology(base.AIEntity.Empire, infiltratedCity.Empire, num, num3, ref this.stealableTechnologies);
			if (this.stealableTechnologies.Count == 0)
			{
				this.stealableTechnologiesResult[infiltratedCity.Empire.Index].Add(new DecisionResult(null, null, -1f));
			}
			else
			{
				layer.EvaluateTechnologies(this.stealableTechnologies, ref this.stealableTechnologiesResult[infiltratedCity.Empire.Index]);
			}
		}
		float num4 = 0f;
		DecisionResult decisionResult = new DecisionResult(null, null, -1f);
		if (this.stealableTechnologiesResult[infiltratedCity.Empire.Index].Count > this.stealableTechnologiesIndex[infiltratedCity.Empire.Index])
		{
			num4 = this.stealableTechnologiesResult[infiltratedCity.Empire.Index][this.stealableTechnologiesIndex[infiltratedCity.Empire.Index]].Score;
			int i = this.stealableTechnologiesIndex[infiltratedCity.Empire.Index];
			while (i < this.stealableTechnologiesResult[infiltratedCity.Empire.Index].Count)
			{
				TechnologyDefinition technologyDefinition = this.stealableTechnologiesResult[infiltratedCity.Empire.Index][i].Element as TechnologyDefinition;
				if (technologyDefinition != null && DepartmentOfScience.GetTechnologyEraNumber(technologyDefinition) <= num2)
				{
					decisionResult = this.stealableTechnologiesResult[infiltratedCity.Empire.Index][i];
					if (i == this.stealableTechnologiesIndex[infiltratedCity.Empire.Index])
					{
						this.stealableTechnologiesIndex[infiltratedCity.Empire.Index]++;
						break;
					}
					break;
				}
				else
				{
					i++;
				}
			}
		}
		float num5 = 0f;
		if (decisionResult.Score > 0f)
		{
			num5 = decisionResult.Score;
		}
		if (num4 <= 0f)
		{
			num4 = 1f;
		}
		float num6 = 0f;
		DebugScoring.SubScoring subScoring = null;
		if (actionScoring != null)
		{
			subScoring = new DebugScoring.SubScoring("BestTechnologyScore", num6);
		}
		num6 = num5 / num4;
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("StealableCount", (float)this.stealableTechnologies.Count));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("BestStealableTechScore", num5));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("BestTechScore", num4));
			subScoring.UtilityAfter = num6;
			actionScoring.SubScorings.Add(subScoring);
			subScoring = new DebugScoring.SubScoring("NotAtMaxEraFactor", num6);
		}
		if (this.stealableTechnologiesResult[infiltratedCity.Empire.Index][0].Element != null)
		{
			float num7 = (float)DepartmentOfScience.GetTechnologyEraNumber(this.stealableTechnologiesResult[infiltratedCity.Empire.Index][0].Element as TechnologyDefinition);
			float num8 = num7 / (float)num2 * this.stealTechnologyNotAtMaxEraFactor;
			num6 = AILayer.Boost(num6, num8);
			if (subScoring != null)
			{
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("bestStealableTechnologyEra", num7));
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("MaxEra", (float)num2));
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("FactorFromXml", this.stealTechnologyNotAtMaxEraFactor));
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("eraBoost", num8));
				subScoring.UtilityAfter = num6;
				subScoring.GlobalBoost = num8;
				actionScoring.SubScorings.Add(subScoring);
			}
		}
		return num6;
	}

	private float InfiltrationActionUtility_StealVision(InfiltrationAction action, AIData_Unit hero, SpiedGarrison spiedGarrison, City infiltratedCity, DebugScoring actionScoring)
	{
		DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(infiltratedCity.Empire);
		if (diplomaticRelation != null && (diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.MapExchange) || diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.VisionExchange)))
		{
			return 0f;
		}
		if (this.departmentOfPlanificationAndDevelopment.GetActiveBooster(string.Format("BoosterStealVisionOverArmiesByInfiltration{0}", infiltratedCity.Empire.Index)) != null || this.departmentOfPlanificationAndDevelopment.GetActiveBooster(string.Format("BoosterStealVisionByInfiltration{0}", infiltratedCity.Empire.Index)) != null)
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
		DepartmentOfTheInterior agency2 = infiltratedCity.Empire.GetAgency<DepartmentOfTheInterior>();
		int num3 = 0;
		int num4 = 0;
		foreach (City city in agency2.Cities)
		{
			if (!this.departmentOfIntelligence.IsGarrisonVisible(city))
			{
				num3++;
				if (!this.visibilityService.IsWorldPositionExploredFor(city.WorldPosition, base.AIEntity.Empire))
				{
					num4++;
				}
			}
		}
		int num5 = this.heroCountBySpecialty[4];
		if (this.maximumFractionOfHeroesSpy * (float)this.departmentOfEducation.Heroes.Count > (float)num5 + 1f && num3 > 2 && agency.Armies.Count > 4)
		{
			for (int i = 0; i < agency.Armies.Count; i++)
			{
				if (!this.visibilityService.IsWorldPositionVisibleFor(agency.Armies[i].WorldPosition, base.AIEntity.Empire))
				{
					num2 += 1f;
				}
			}
			if (agency.Armies.Count > 0)
			{
				num2 /= (float)agency.Armies.Count;
			}
			num = num2 * this.factorForInterestingArmies;
		}
		if (action.Level == 5)
		{
			num = AILayer.Boost(num, (float)num4 / (float)agency2.Cities.Count * 0.6f);
		}
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("percentOfArmyInteresting", num2));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("FactorFromXml", this.factorForInterestingArmies));
			subScoring.UtilityAfter = num;
			actionScoring.SubScorings.Add(subScoring);
			subScoring = new DebugScoring.SubScoring("DiplomaticBoost", num);
		}
		float num6 = 0f;
		if (this.departmentOfForeignAffairs.IsAtWarWith(infiltratedCity.Empire))
		{
			num6 = 0.2f;
			if (subScoring != null)
			{
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("AtWarBoost", num6));
			}
		}
		num = AILayer.Boost(num, num6);
		if (subScoring != null)
		{
			subScoring.UtilityAfter = num;
			subScoring.GlobalBoost = num6;
			actionScoring.SubScorings.Add(subScoring);
		}
		return num;
	}

	private float InfiltrationActionUtility_DecreaseMoral(InfiltrationAction action, AIData_Unit hero, SpiedGarrison spiedGarrison, City infiltratedCity, DebugScoring actionScoring)
	{
		if (this.DiplomacyLayer.GetPeaceWish(infiltratedCity.Empire.Index))
		{
			return 0f;
		}
		DepartmentOfPlanificationAndDevelopment agency = infiltratedCity.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
		if (agency.GetActiveBooster("BoosterDecreaseBattleMoraleByInfiltration3") != null || agency.GetActiveBooster("BoosterDecreaseBattleMoraleByInfiltration4") != null || (action.Level == 5 && agency.GetActiveBooster("BoosterDecreaseBattleMoraleByInfiltration5") != null))
		{
			return 0f;
		}
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
			foreach (DiplomaticRelation diplomaticRelation2 in infiltratedCity.Empire.GetAgency<DepartmentOfForeignAffairs>().DiplomaticRelations)
			{
				if (diplomaticRelation2.OtherEmpireIndex != base.AIEntity.Empire.Index && diplomaticRelation2.OtherEmpireIndex != infiltratedCity.Empire.Index && diplomaticRelation != null && diplomaticRelation.State != null && diplomaticRelation.State.Name == DiplomaticRelationState.Names.War)
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
		float num3 = Mathf.Clamp01(num2 / this.decreaseMoralUnitCountMaximum) * this.decreaseMoralUnitCountFactor;
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("UnitCount", num2));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("UnitCountMax", this.decreaseMoralUnitCountMaximum));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("FactorFromXml", this.decreaseMoralUnitCountFactor));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boost", num3));
		}
		if (this.decreaseMoralUnitCountCurve != null)
		{
			num3 = this.decreaseMoralUnitCountCurve.Evaluate(num3);
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
			subScoring = new DebugScoring.SubScoring("MilitaryPower", num);
		}
		float propertyValue = base.AIEntity.Empire.GetPropertyValue(SimulationProperties.MilitaryPower);
		float propertyValue2 = infiltratedCity.Empire.GetPropertyValue(SimulationProperties.MilitaryPower);
		if (subScoring != null)
		{
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("MyMilitaryPower", propertyValue));
			subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("InfiltratedMP", propertyValue2));
		}
		num3 = 0f;
		if (propertyValue > 0f)
		{
			float num4 = propertyValue2 / propertyValue;
			num4 = Mathf.Clamp(num4 - 0.5f, -this.decreaseMoralMilitaryPowerClamp, this.decreaseMoralMilitaryPowerClamp);
			num3 = num4 * this.decreaseMoralMilitaryPowerFactor;
			if (subScoring != null)
			{
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("Ratio - 0.5", num4));
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("RatioClamp", this.decreaseMoralMilitaryPowerClamp));
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("FactorFromXml", this.decreaseMoralMilitaryPowerFactor));
				subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boost", num3));
			}
			if (this.decreaseMoralMilitaryPowerCurve != null)
			{
				num3 = this.decreaseMoralMilitaryPowerCurve.Evaluate(num3);
				if (subScoring != null)
				{
					subScoring.ScoringInstructions.Add(new DebugScoring.ScoringInstruction("boostAfterCurve", num3));
				}
			}
		}
		num = AILayer.Boost(num, num3);
		if (subScoring != null)
		{
			subScoring.UtilityAfter = num;
			subScoring.GlobalBoost = num3;
			actionScoring.SubScorings.Add(subScoring);
		}
		num = this.ApplyHurtingBoostOnEmpire(num, infiltratedCity.Empire, actionScoring);
		return num;
	}

	private float InfiltrationActionUtility_DecreaseProduction(InfiltrationAction action, AIData_Unit hero, SpiedGarrison spiedGarrison, City infiltratedCity, DebugScoring actionScoring)
	{
		if (this.DiplomacyLayer.GetPeaceWish(infiltratedCity.Empire.Index))
		{
			return 0f;
		}
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
				if (districtImprovementDefinition != null && districtImprovementDefinition.OnePerWorld && this.departmentOfIndustry.GetConstruction(districtImprovementDefinition) != null)
				{
					num6 = this.decreaseProductionSameWonderBoost;
					break;
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
		if (this.DiplomacyLayer.GetPeaceWish(infiltratedCity.Empire.Index))
		{
			return 0f;
		}
		float result = 0.1f;
		InfiltrationActionOnEmpire_StealResource infiltrationActionOnEmpire_StealResource = action as InfiltrationActionOnEmpire_StealResource;
		if (infiltrationActionOnEmpire_StealResource != null)
		{
			global::Empire empire = infiltratedCity.Empire;
			DepartmentOfTheTreasury agency = empire.GetAgency<DepartmentOfTheTreasury>();
			float num = 0f;
			agency.TryGetResourceStockValue(empire.SimulationObject, infiltrationActionOnEmpire_StealResource.ResourceName, out num, false);
			float propertyValue = base.AIEntity.Empire.SimulationObject.GetPropertyValue(SimulationProperties.NetEmpireMoney);
			result = Mathf.Min(Mathf.Floor(Mathf.Min(num * infiltrationActionOnEmpire_StealResource.AmountParameters.TargetStockPercentage + infiltrationActionOnEmpire_StealResource.AmountParameters.BaseAmount, num)) / propertyValue / 2f, 1f);
		}
		return result;
	}

	public IEnumerable<IAIParameterConverter<InterpreterContext>> GetAIParameterConverters(StaticString aiParameterName)
	{
		foreach (IAIParameterConverter<InterpreterContext> iaiparameterConverter in this.constructibleElementEvaluationAIHelper.GetAIParameterConverters(aiParameterName))
		{
			yield return iaiparameterConverter;
		}
		IEnumerator<IAIParameterConverter<InterpreterContext>> enumerator = null;
		yield break;
		yield break;
	}

	public IEnumerable<IAIParameter<InterpreterContext>> GetAIParameters(UnitSkill element)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		foreach (IAIParameter<InterpreterContext> iaiparameter in this.constructibleElementEvaluationAIHelper.GetAIParameters(element))
		{
			yield return iaiparameter;
		}
		IEnumerator<IAIParameter<InterpreterContext>> enumerator = null;
		yield break;
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
		int i = 0;
		while (i < this.assignationData.Count)
		{
			if (this.assignationData[i].Garrison.GUID == garrison.GUID)
			{
				if (this.assignationData[i].WantedHeroAIData != null)
				{
					return this.assignationData[i].WantedHeroAIData.HeroData.WantedAssignationFitness;
				}
				return 0f;
			}
			else
			{
				i++;
			}
		}
		return 0f;
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		this.HeroSkillDecisions = new List<KeyValuePair<string, string>>();
		this.empireSpecialtyNeed = new float[AILayer_HeroAssignation.HeroAssignationTypeNames.Length];
		this.departmentOfDefense = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		this.departmentOfEducation = base.AIEntity.Empire.GetAgency<DepartmentOfEducation>();
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		this.departmentOfIntelligence = base.AIEntity.Empire.GetAgency<DepartmentOfIntelligence>();
		this.departmentOfForeignAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		this.departmentOfTheTreasury = base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>();
		this.departmentOfIndustry = base.AIEntity.Empire.GetAgency<DepartmentOfIndustry>();
		this.departmentOfScience = base.AIEntity.Empire.GetAgency<DepartmentOfScience>();
		this.departmentOfPlanificationAndDevelopment = base.AIEntity.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
		this.aiDataRepositoryHelper = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		this.constructibleElementEvaluationAIHelper = AIScheduler.Services.GetService<IConstructibleElementEvaluationAIHelper>();
		this.tickableRepositoryHelper = AIScheduler.Services.GetService<ITickableRepositoryAIHelper>();
		this.tradeDataRepository = AIScheduler.Services.GetService<ITradeDataRepository>();
		this.personalityAIHelper = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.visibilityService = service.Game.Services.GetService<IVisibilityService>();
		this.animationCurveDatabase = Databases.GetDatabase<Amplitude.Unity.Framework.AnimationCurve>(false);
		this.infiltrationActionDatabase = Databases.GetDatabase<InfiltrationAction>(false);
		this.game = (service.Game as global::Game);
		this.tradeManagementService = service.Game.Services.GetService<ITradeManagementService>();
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
		this.MinMoneyforRestore = 1.5f;
		this.TurnThresholdforRestore = 5f;
		IDatabase<Amplitude.Unity.Framework.AnimationCurve> database = Databases.GetDatabase<Amplitude.Unity.Framework.AnimationCurve>(false);
		this.globalAssignationCurve = database.GetValue("HeroNeed");
		this.MinMoneyforRestore = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.RegistryPath, "MinMoneyforRestore"), this.MinMoneyforRestore);
		this.TurnThresholdforRestore = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.RegistryPath, "TurnThresholdforRestore"), this.TurnThresholdforRestore);
		this.maximalHeroCount = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.RegistryPath, "MaximumHeroCount"), this.maximalHeroCount);
		this.maximalTurnForHero = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.RegistryPath, "MaximumTurnForHero"), this.maximalTurnForHero);
		this.maximumFractionOfHeroesSpy = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.RegistryPath, "MaximumFractionOfHeroesSpy"), this.maximumFractionOfHeroesSpy);
		this.minimalTurnBeforeSpying = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.RegistryPath, "MinimalTurnBeforeSpying"), this.minimalTurnBeforeSpying);
		this.priceMarginToChooseBestHero = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.RegistryPath, "PriceMarginToChooseBestHero"), this.priceMarginToChooseBestHero);
		this.boostForNonSpecialityToChooseBestHero = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.RegistryPath, "BoostForNonSpecialityToChooseBestHero"), this.boostForNonSpecialityToChooseBestHero);
		this.limitToExfiltrate = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_HeroAssignation.RegistryPath, "LimitToExfiltrate"), this.limitToExfiltrate);
		this.VictoryLayer = base.AIEntity.GetLayer<AILayer_Victory>();
		this.DiplomacyLayer = base.AIEntity.GetLayer<AILayer_Diplomacy>();
		this.unitSkillDatabase = Databases.GetDatabase<UnitSkill>(false);
		this.majorEmpire = (base.AIEntity.Empire as MajorEmpire);
		this.InitializeInfiltrationActionUtilities();
		yield break;
	}

	public override bool IsActive()
	{
		return !this.majorEmpire.ELCPIsEliminated && base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
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
		this.heroesToLevelUp.Clear();
		this.heroAssignations.Clear();
		this.infiltrationActionToPerform.Clear();
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
		this.departmentOfPlanificationAndDevelopment = null;
		this.levelUpDecisionMaker = null;
		this.game = null;
		this.VictoryLayer = null;
		this.DiplomacyLayer = null;
		this.unitSkillDatabase = null;
		this.HeroSkillDecisions.Clear();
		this.VisibleSpyGarrisons.Clear();
		this.DebugInfiltrationActionInfo.Clear();
		this.joblessHeros.Clear();
		this.majorEmpire = null;
	}

	public void Tick()
	{
		if (!this.IsActive())
		{
			this.State = TickableState.NoTick;
			return;
		}
		if (this.assignJoblessHeros)
		{
			this.AssignJoblessHeros();
			this.assignJoblessHeros = false;
			return;
		}
		if (this.orderTicket != null)
		{
			if (!this.orderTicket.Raised)
			{
				return;
			}
			this.orderTicket = null;
		}
		if (this.RestoreHeroes())
		{
			return;
		}
		if (this.heroesToLevelUp.Count > 0)
		{
			if (!this.HeroLevelUp(this.heroesToLevelUp[0]))
			{
				this.heroesToLevelUp.RemoveAt(0);
				return;
			}
		}
		else
		{
			if (this.infiltrationActionToPerform.Count > 0)
			{
				for (int i = this.infiltrationActionToPerform.Count - 1; i >= 0; i--)
				{
					if (!this.ExecuteInfiltrationAction(this.infiltrationActionToPerform[i]))
					{
						this.infiltrationActionToPerform.RemoveAt(i);
					}
				}
				return;
			}
			if (this.heroAssignations.Count > 0)
			{
				for (int j = this.heroAssignations.Count - 1; j >= 0; j--)
				{
					if (!this.ExecuteChanges(this.heroAssignations[j]))
					{
						this.heroAssignations.RemoveAt(j);
					}
				}
				return;
			}
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
			foreach (EvaluableMessage_HeroNeed evaluableMessage_HeroNeed in base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_HeroNeed>(BlackboardLayerID.Empire, (EvaluableMessage_HeroNeed match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending))
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
		this.assignJoblessHeros = true;
	}

	private float ComputeArmyHeroNeed()
	{
		return AILayer.Boost(0.5f, this.GetAssignationBestScoreFor(3) - 0.5f);
	}

	private float ComputeArmySupportNeed()
	{
		return AILayer.Boost(0.5f, this.GetAssignationBestScoreFor(2) - 0.5f);
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
		float num = this.globalAssignationCurve.Evaluate(xValue) * this.maximalHeroCount;
		if (num == 0f)
		{
			return 0f;
		}
		float num2 = (float)this.departmentOfEducation.Heroes.Count;
		int num3 = 0;
		using (IEnumerator<Unit> enumerator = this.departmentOfEducation.Heroes.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.Garrison is City || enumerator.Current.Garrison == null)
				{
					num3++;
				}
			}
		}
		float num4 = 0f;
		base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>().TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpireMoney, out num4, false);
		int count = this.departmentOfTheInterior.NonInfectedCities.Count;
		if (base.AIEntity.Empire.GetPropertyValue(SimulationProperties.NetEmpireMoney) > 10f && num4 > 400f + (float)this.departmentOfEducation.Heroes.Count * 250f && (num3 < count - 1 || this.departmentOfEducation.Heroes.Count < count + 5))
		{
			return 1f;
		}
		return Mathf.Max(0f, 1f - num2 / num);
	}

	private float ComputeGovernorCityNeed()
	{
		int count = this.departmentOfTheInterior.NonInfectedCities.Count;
		int num = 0;
		using (IEnumerator<Unit> enumerator = this.departmentOfEducation.Heroes.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.Garrison is City || enumerator.Current.Garrison == null)
				{
					num++;
				}
			}
		}
		if (num < count - 1)
		{
			return 1f;
		}
		float num2 = (float)(this.heroCountBySpecialty[0] + this.heroCountBySpecialty[1]);
		float num3 = Mathf.Max(1f, (float)count * 0.5f);
		if (num2 >= num3)
		{
			return 0f;
		}
		return AILayer.Boost(0.6f, this.GetAssignationBestScoreFor(0) - 0.5f);
	}

	private float ComputeGovernorEmpireNeed()
	{
		int count = this.departmentOfTheInterior.NonInfectedCities.Count;
		int num = 0;
		using (IEnumerator<Unit> enumerator = this.departmentOfEducation.Heroes.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.Garrison is City || enumerator.Current.Garrison == null)
				{
					num++;
				}
			}
		}
		if (num < count - 1)
		{
			return 1f;
		}
		float num2 = (float)(this.heroCountBySpecialty[0] + this.heroCountBySpecialty[1]);
		float num3 = Mathf.Max(1f, (float)count * 0.5f);
		if (num2 >= num3)
		{
			return 0f;
		}
		return AILayer.Boost(0.6f, this.GetAssignationBestScoreFor(1) - 0.5f);
	}

	private float ComputeHeroCostProjection(TradableUnit tradableUnit)
	{
		Unit unit;
		if (!this.tradeManagementService.TryRetrieveUnit(tradableUnit.GameEntityGUID, out unit))
		{
			return float.MaxValue;
		}
		float num = (float)(unit.Level + 1) * AILayer_HeroAssignation.heroUpkeepProjectionCostByLevel * AILayer_HeroAssignation.heroUpkeepProjectionTurnCount;
		return tradableUnit.GetPriceWithSalesTaxes(TradableTransactionType.Buyout, base.AIEntity.Empire, 1f) + num;
	}

	private float ComputeSpyNeed()
	{
		if (!this.downloadableContentService.IsShared(DownloadableContent11.ReadOnlyName))
		{
			return 0f;
		}
		int num = this.heroCountBySpecialty[4];
		float num2 = this.maximumFractionOfHeroesSpy * (float)this.departmentOfEducation.Heroes.Count;
		if ((float)num >= num2 || num2 <= 0f)
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
		return AILayer.Boost(1f - (float)num / num2, this.GetAssignationBestScoreFor(4));
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
				if (!this.VisibleSpyGarrisons.Contains(gameEntityGUID) || (this.departmentOfIntelligence != null && !this.departmentOfIntelligence.CanInfiltrateIgnoreVision(heroData.Unit, heroData.HeroData.WantedHeroAssignation.Garrison, false, out num, true)))
				{
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
					OrderToggleInfiltration orderToggleInfiltration = new OrderToggleInfiltration(base.AIEntity.Empire.Index, heroData.Unit.GUID, gameEntityGUID, false, true);
					orderToggleInfiltration.IgnoreVision = true;
					base.AIEntity.Empire.PlayerControllers.AI.PostOrder(orderToggleInfiltration, out this.orderTicket, null);
				}
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
		int num;
		for (index2 = 0; index2 < this.departmentOfTheInterior.NonInfectedCities.Count; index2 = num + 1)
		{
			if (!this.assignationData.Exists((AssignationData match) => match.Garrison == this.departmentOfTheInterior.NonInfectedCities[index2]))
			{
				this.assignationData.Add(new AssignationData_City(this.departmentOfTheInterior.NonInfectedCities[index2]));
			}
			num = index2;
		}
		int index;
		Predicate<AssignationData> <>9__2;
		for (index = 0; index < this.departmentOfDefense.Armies.Count; index = num + 1)
		{
			List<AssignationData> list = this.assignationData;
			Predicate<AssignationData> match2;
			if ((match2 = <>9__2) == null)
			{
				match2 = (<>9__2 = ((AssignationData match) => match.Garrison == this.departmentOfDefense.Armies[index]));
			}
			AIData_Army aidata_Army;
			if (!list.Exists(match2) && this.aiDataRepositoryHelper.TryGetAIData<AIData_Army>(this.departmentOfDefense.Armies[index].GUID, out aidata_Army) && !aidata_Army.IsColossus && !aidata_Army.IsSolitary && !aidata_Army.Army.HasCatspaw && !(aidata_Army.Army is KaijuArmy))
			{
				this.assignationData.Add(new AssignationData_Army(aidata_Army));
			}
			num = index;
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
					int cityIndex2;
					int cityIndex;
					for (cityIndex = 0; cityIndex < foreignDepartmentOfTheInterior.Cities.Count; cityIndex = cityIndex2 + 1)
					{
						if (!this.assignationData.Exists((AssignationData match) => match.Garrison == foreignDepartmentOfTheInterior.Cities[cityIndex]))
						{
							bool flag = false;
							AIData_City aidata_City;
							if (this.aiDataRepositoryHelper.TryGetAIData<AIData_City>(foreignDepartmentOfTheInterior.Cities[cityIndex].GUID, out aidata_City))
							{
								flag = aidata_City.IsCityExploredFor(base.AIEntity.Empire);
							}
							bool flag2 = false;
							if (this.downloadableContentService.IsShared(DownloadableContent20.ReadOnlyName) && aidata_City != null && aidata_City.City != null)
							{
								flag2 = aidata_City.City.IsInfected;
							}
							if (flag && !flag2)
							{
								this.assignationData.Add(new AssignationData_Spy(base.AIEntity.Empire, foreignDepartmentOfTheInterior.Cities[cityIndex]));
							}
						}
						cityIndex2 = cityIndex;
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
		if (heroData.Unit.GetPropertyValue(SimulationProperties.MaximumSkillPoints) - heroData.Unit.GetPropertyValue(SimulationProperties.SkillPointsSpent) > 0f)
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
			IGarrison garrison = heroData.Unit.Garrison;
			if (garrison != null && garrison is City && DepartmentOfEducation.IsUnitSkillUnlockable(heroData.Unit, "HeroSkillGovernor42") && this.IsVolcanicCity(garrison as City))
			{
				this.levelUpDecisionMaker.RegisterOutput(AILayer_HeroAssignation.heroAssignationTypeNames[0]);
			}
			else if (garrison != null && garrison is SpiedGarrison)
			{
				this.levelUpDecisionMaker.RegisterOutput(AILayer_HeroAssignation.heroAssignationTypeNames[4]);
			}
			else if ((garrison != null && garrison is Army) || heroData.HeroData.ChosenSpecialty < 0)
			{
				this.levelUpDecisionMaker.RegisterOutput(AILayer_HeroAssignation.heroAssignationTypeNames[2]);
			}
			else
			{
				this.levelUpDecisionMaker.RegisterOutput(AILayer_HeroAssignation.heroAssignationTypeNames[heroData.HeroData.ChosenSpecialty]);
			}
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
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				this.HeroSkillDecisions.RemoveAll((KeyValuePair<string, string> h) => h.Key == heroData.Unit.GUID.ToString());
				foreach (DecisionResult decisionResult in this.decisionResults)
				{
					this.HeroSkillDecisions.Add(new KeyValuePair<string, string>(heroData.Unit.GUID.ToString(), AgeLocalizer.Instance.LocalizeString("%" + decisionResult.Element.ToString() + "Title") + " - " + decisionResult.Score));
				}
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
				base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out this.orderTicket, new EventHandler<TicketRaisedEventArgs>(this.OnHeroLevelUp));
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
		this.VisibleSpyGarrisons.Clear();
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
					aidata_Unit.HeroData.WantedAssignationFitness = num * 1.2f;
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
		foreach (AssignationData assignationData in this.assignationData)
		{
			if (assignationData is AssignationData_Spy && this.departmentOfIntelligence != null && this.departmentOfIntelligence.IsGarrisonVisible(assignationData.Garrison))
			{
				this.VisibleSpyGarrisons.Add(assignationData.Garrison.GUID);
			}
		}
		int num2 = 0;
		bool flag;
		do
		{
			flag = false;
			for (int j = 0; j < this.heroAssignations.Count; j++)
			{
				AIData_Unit aidata_Unit2 = this.heroAssignations[j];
				List<UnitSkill> source = new List<UnitSkill>();
				DepartmentOfEducation.FillAvailableUnitSkills(aidata_Unit2.Unit, ref source);
				int k = 0;
				while (k < this.assignationData.Count)
				{
					bool flag2 = false;
					bool flag3 = false;
					bool flag4 = false;
					if (this.assignationData[k] is AssignationData_City && aidata_Unit2.HeroData.ChosenSpecialty < 2)
					{
						if (source.Any((UnitSkill s) => s.Name == "HeroSkillGovernor42") && this.IsVolcanicCity((this.assignationData[k] as AssignationData_City).Garrison as City))
						{
							flag3 = true;
						}
					}
					if (this.assignationData[k].CurrentHeroAIData == null || !(this.assignationData[k] is AssignationData_Spy))
					{
						goto IL_403;
					}
					if (!DepartmentOfIntelligence.IsGarrisonAlreadyUnderInfiltrationProcessus(this.assignationData[k].Garrison.GUID, base.AIEntity.Empire) || this.assignationData[k].CurrentHeroAIData.Unit == aidata_Unit2.Unit)
					{
						if (DepartmentOfIntelligence.IsGarrisonAlreadyUnderInfiltrationProcessus(this.assignationData[k].Garrison.GUID, base.AIEntity.Empire) && this.assignationData[k].CurrentHeroAIData.Unit == aidata_Unit2.Unit && this.assignationData[k].GarrisonSpecialtyNeed[4] > 0f)
						{
							flag3 = true;
							goto IL_403;
						}
						goto IL_403;
					}
					IL_3F8:
					k++;
					continue;
					IL_403:
					if ((this.assignationData[k] is AssignationData_City && (this.assignationData[k].Garrison as City).BesiegingEmpireIndex >= 0 && this.assignationData[k].Garrison.Empire == base.AIEntity.Empire && (this.assignationData[k].CurrentHeroAIData == null || this.assignationData[k].CurrentHeroAIData.Unit != aidata_Unit2.Unit)) || (this.assignationData[k] is AssignationData_Army && this.assignationData[k].Garrison is Army && (this.assignationData[k].Garrison as Army).IsPrivateers))
					{
						goto IL_3F8;
					}
					if (this.assignationData[k] is AssignationData_Army && this.assignationData[k].Garrison is Army && (this.assignationData[k].Garrison as Army).HasSeafaringUnits() && aidata_Unit2.Unit.UnitDesign.UnitBodyDefinitionReference.Name != "UnitBodySeaDemonsHero")
					{
						flag4 = true;
					}
					float num3 = aidata_Unit2.HeroData.ComputeFitness(this.assignationData[k].GarrisonSpecialtyNeed);
					if (flag2)
					{
						num3 = AILayer.Boost(num3, 0.5f);
					}
					if (flag3)
					{
						num3 = AILayer.Boost(num3, 0.9f);
					}
					if (flag4)
					{
						num3 = AILayer.Boost(num3, -0.5f);
					}
					if (this.VictoryLayer.CurrentVictoryDesign != AILayer_Victory.VictoryDesign.none && this.assignationData[k] is AssignationData_Army && this.assignationData[k].Garrison is Army)
					{
						AIData_Army aidata = this.aiDataRepositoryHelper.GetAIData<AIData_Army>((this.assignationData[k].Garrison as Army).GUID);
						if (aidata != null && aidata.Army != null)
						{
							string victorystring = "Settler";
							if (this.VictoryLayer.CurrentVictoryDesign == AILayer_Victory.VictoryDesign.Preacher)
							{
								victorystring = "Preacher";
							}
							else if (this.VictoryLayer.CurrentVictoryDesign == AILayer_Victory.VictoryDesign.Gorgon)
							{
								victorystring = "MimicsUnit2";
							}
							if (aidata.Army.StandardUnits.Count((Unit x) => x.UnitDesign.Name.ToString().Contains(victorystring)) > 5)
							{
								num3 = AILayer.Boost(num3, 0.9f);
							}
						}
					}
					if ((aidata_Unit2.HeroData.WantedHeroAssignation == null || num3 > aidata_Unit2.HeroData.WantedAssignationFitness) && (this.assignationData[k].WantedHeroAIData == null || this.assignationData[k].WantedHeroAIData.HeroData.WantedAssignationFitness < num3))
					{
						if (aidata_Unit2.HeroData.WantedHeroAssignation != null)
						{
							aidata_Unit2.HeroData.WantedHeroAssignation.WantedHeroAIData = null;
						}
						aidata_Unit2.HeroData.WantedHeroAssignation = this.assignationData[k];
						aidata_Unit2.HeroData.WantedAssignationFitness = num3;
						if (this.assignationData[k].WantedHeroAIData != null)
						{
							this.assignationData[k].WantedHeroAIData.HeroData.WantedHeroAssignation = null;
						}
						this.assignationData[k].WantedHeroAIData = aidata_Unit2;
						flag = true;
						goto IL_3F8;
					}
					goto IL_3F8;
				}
			}
			num2++;
		}
		while (flag && num2 < this.assignationData.Count * this.heroAssignations.Count);
		for (int l = this.heroAssignations.Count - 1; l >= 0; l--)
		{
			GameEntityGUID x2 = (this.heroAssignations[l].HeroData.WantedHeroAssignation == null) ? GameEntityGUID.Zero : this.heroAssignations[l].HeroData.WantedHeroAssignation.Garrison.GUID;
			GameEntityGUID y = (this.heroAssignations[l].HeroData.CurrentHeroAssignation == null) ? GameEntityGUID.Zero : this.heroAssignations[l].HeroData.CurrentHeroAssignation.Garrison.GUID;
			if (x2 == y)
			{
				this.heroAssignations.RemoveAt(l);
			}
		}
	}

	private void PickHeroOnMarket()
	{
		if (this.departmentOfScience.CanTradeHeroes(false))
		{
			base.AIEntity.GetLayer<AILayer_AccountManager>().SetMaximalAccount(AILayer_AccountManager.HeroAccountName, -1f);
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
				bool flag = false;
				int num7 = 0;
				using (IEnumerator<Unit> enumerator = this.departmentOfEducation.Heroes.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (enumerator.Current.Garrison is City)
						{
							num7++;
						}
					}
				}
				float num8 = 0f;
				base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>().TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpireMoney, out num8, false);
				if (num8 > 600f + (float)this.departmentOfEducation.Heroes.Count * 250f && ((float)num7 < (float)this.departmentOfTheInterior.NonInfectedCities.Count * 0.6f || this.departmentOfEducation.Heroes.Count < 5))
				{
					flag = true;
				}
				for (int k = 0; k < list.Count; k++)
				{
					TradableUnit tradableUnit2 = list[k] as TradableUnit;
					if ((tradableUnit2.EmpireExclusionBits & num6) == 0)
					{
						float num9 = 0f;
						AIData_TradableUnit aidata_TradableUnit;
						if (this.tradeDataRepository.TryGetAIData<AIData_TradableUnit>(tradableUnit2.UID, out aidata_TradableUnit))
						{
							num9 = aidata_TradableUnit.HeroData.ComputeFitness(this.empireSpecialtyNeed);
							num9 = AILayer.Boost(num9, 0.3f);
							Unit hero;
							if (this.tradeManagementService.TryRetrieveUnit(tradableUnit2.GameEntityGUID, out hero) && num3 < 2 && this.IsBadGovernorForFaction(hero))
							{
								num9 = AILayer.Boost(num9, -0.9f);
							}
						}
						float num10 = this.ComputeHeroCostProjection(tradableUnit2);
						if (num10 < num4 * this.priceMarginToChooseBestHero && num9 > num5)
						{
							tradableUnit = tradableUnit2;
							num4 = num10;
							num5 = num9;
						}
					}
				}
				if (tradableUnit != null)
				{
					if (flag)
					{
						num5 = 1f;
					}
					AILayer_Trade.UpdateHeroNeed(num, num5, tradableUnit, base.AIEntity.AIPlayer.Blackboard);
					return;
				}
			}
		}
		else
		{
			base.AIEntity.GetLayer<AILayer_AccountManager>().SetMaximalAccount(AILayer_AccountManager.HeroAccountName, 0f);
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

	private bool RestoreHeroes()
	{
		float num;
		this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpireMoney, out num, false);
		for (int i = 0; i < this.departmentOfEducation.Heroes.Count; i++)
		{
			Unit unit = this.departmentOfEducation.Heroes[i];
			if (DepartmentOfEducation.IsInjured(unit) && (float)GuiHero.ComputeTurnsBeforRecovery(unit.GetPropertyValue(SimulationProperties.CurrentInjuredValue), unit.GetPropertyValue(SimulationProperties.InjuredRecoveryPerTurn)) > this.TurnThresholdforRestore)
			{
				float propertyValue = unit.GetPropertyValue(SimulationProperties.CurrentInjuredValue);
				if (propertyValue > 0f && propertyValue * unit.GetPropertyValue(SimulationProperties.InjuredValueToEmpireMoneyConversion) * this.MinMoneyforRestore <= num)
				{
					OrderRestoreHero order = new OrderRestoreHero(base.AIEntity.Empire.Index, unit.GUID);
					base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out this.orderTicket, null);
					return true;
				}
			}
		}
		return false;
	}

	private bool IsVolcanicCity(City city)
	{
		if (city == null)
		{
			return false;
		}
		int num = 0;
		IWorldPositionningService service = Services.GetService<IGameService>().Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(service != null);
		for (int i = 0; i < city.Districts.Count; i++)
		{
			WorldPosition worldPosition = city.Districts[i].WorldPosition;
			if (service.ContainsTerrainTag(worldPosition, "TerrainTagVolcanic") && !service.IsWaterTile(worldPosition))
			{
				num++;
			}
			if (num > 6)
			{
				return true;
			}
		}
		return false;
	}

	private void AssignJoblessHeros()
	{
		if (base.AIEntity.Empire.GetPropertyValue(SimulationProperties.NetEmpireMoney) > 50f)
		{
			int i = 0;
			using (IEnumerator<Unit> enumerator = this.departmentOfEducation.Heroes.GetEnumerator())
			{
				IL_1CC:
				while (enumerator.MoveNext())
				{
					Unit unit = enumerator.Current;
					if (unit != null)
					{
						if (unit.Garrison == null && !DepartmentOfEducation.IsInjured(unit) && !DepartmentOfEducation.IsCaptured(unit))
						{
							if (!this.joblessHeros.ContainsKey(unit.GUID))
							{
								this.joblessHeros.Add(unit.GUID, this.game.Turn);
							}
							else if (this.joblessHeros[unit.GUID] < this.game.Turn)
							{
								AIData_Unit aidata_Unit;
								this.aiDataRepositoryHelper.TryGetAIData<AIData_Unit>(unit.GUID, out aidata_Unit);
								if (aidata_Unit != null && aidata_Unit.HeroData != null)
								{
									if (aidata_Unit.HeroData.WantedHeroAssignation == null || aidata_Unit.HeroData.WantedHeroAssignation.Garrison == null || aidata_Unit.HeroData.WantedHeroAssignation.Garrison.Empire != base.AIEntity.Empire)
									{
										while (i < this.departmentOfTheInterior.Cities.Count)
										{
											City city = this.departmentOfTheInterior.Cities[i];
											List<WorldPosition> availablePositionsForArmyCreation = DepartmentOfDefense.GetAvailablePositionsForArmyCreation(city);
											if (availablePositionsForArmyCreation != null && availablePositionsForArmyCreation.Count != 0 && !city.IsInEncounter)
											{
												OrderTransferAcademyToNewArmy order = new OrderTransferAcademyToNewArmy(base.AIEntity.Empire.Index, city.GUID, unit.GUID);
												base.AIEntity.Empire.PlayerController.PostOrder(order);
												i++;
												goto IL_1CC;
											}
											i++;
										}
										break;
									}
								}
							}
						}
						else
						{
							this.joblessHeros.Remove(unit.GUID);
						}
					}
				}
			}
		}
	}

	public List<string> GetHeroSkillDecisions(AIData_Unit heroData)
	{
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, string> keyValuePair in this.HeroSkillDecisions)
		{
			if (keyValuePair.Key == heroData.Unit.GUID.ToString())
			{
				list.Add(keyValuePair.Value);
			}
		}
		if (list.Count == 0)
		{
			List<UnitSkill> list2 = new List<UnitSkill>();
			DepartmentOfEducation.FillAvailableUnitSkills(heroData.Unit, ref list2);
			for (int i = list2.Count - 1; i >= 0; i--)
			{
				if (!DepartmentOfEducation.IsUnitSkillUpgradable(heroData.Unit, list2[i]))
				{
					list2.RemoveAt(i);
				}
			}
			if (list2.Count > 0)
			{
				this.levelUpDecisionMaker.Context.SimulationObject = heroData.Unit;
				this.levelUpDecisionMaker.UnregisterAllOutput();
				IGarrison garrison = heroData.Unit.Garrison;
				if (garrison != null && garrison is City && DepartmentOfEducation.IsUnitSkillUnlockable(heroData.Unit, "HeroSkillGovernor42") && this.IsVolcanicCity(garrison as City))
				{
					this.levelUpDecisionMaker.RegisterOutput(AILayer_HeroAssignation.heroAssignationTypeNames[0]);
				}
				else if (garrison != null && garrison is SpiedGarrison)
				{
					this.levelUpDecisionMaker.RegisterOutput(AILayer_HeroAssignation.heroAssignationTypeNames[4]);
				}
				else if (garrison != null && garrison is Army)
				{
					this.levelUpDecisionMaker.RegisterOutput(AILayer_HeroAssignation.heroAssignationTypeNames[2]);
				}
				else
				{
					this.levelUpDecisionMaker.RegisterOutput(AILayer_HeroAssignation.heroAssignationTypeNames[heroData.HeroData.ChosenSpecialty]);
				}
				this.decisionResults.Clear();
				this.levelUpDecisionMaker.EvaluateDecisions(list2, ref this.decisionResults);
				if (this.decisionResults[0].Score < 0.05f)
				{
					this.levelUpDecisionMaker.UnregisterAllOutput();
					for (int j = 0; j < AILayer_HeroAssignation.heroAssignationTypeNames.Length; j++)
					{
						this.levelUpDecisionMaker.RegisterOutput(AILayer_HeroAssignation.heroAssignationTypeNames[j]);
					}
					this.decisionResults.Clear();
					this.levelUpDecisionMaker.EvaluateDecisions(list2, ref this.decisionResults);
				}
				foreach (DecisionResult decisionResult in this.decisionResults)
				{
					list.Add(AgeLocalizer.Instance.LocalizeString("%" + decisionResult.Element.ToString() + "Title") + " - " + decisionResult.Score);
				}
			}
		}
		return list;
	}

	public List<InfiltrationActionData> GetDebugInfiltrationInfo(GameEntityGUID heroguid)
	{
		List<InfiltrationActionData> list = new List<InfiltrationActionData>();
		list.AddRange(this.DebugInfiltrationActionInfo.FindAll((InfiltrationActionData L) => L.HeroGuid == heroguid));
		return list;
	}

	private bool IsBadGovernorForFaction(Unit Hero)
	{
		if (base.AIEntity.Empire.SimulationObject.Tags.Contains(DepartmentOfTheInterior.FactionTraitBuyOutPopulation))
		{
			return Hero.UnitDesign.UnitBodyDefinitionReference.Name == "UnitBodyMimicsHero" || Hero.UnitDesign.UnitBodyDefinitionReference.Name == "UnitBodyNecrophagesHero";
		}
		return base.AIEntity.Empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitReplicants1) && (Hero.UnitDesign.UnitBodyDefinitionReference.Name == "UnitBodyRageWizardsHero" || Hero.UnitDesign.UnitBodyDefinitionReference.Name == "UnitBodyVaultersHero" || Hero.UnitDesign.UnitBodyDefinitionReference.Name == "UnitBodyMezariHero" || Hero.UnitDesign.UnitBodyDefinitionReference.Name == "UnitBodyHauntsHero");
	}

	protected void OnHeroLevelUp(object sender, TicketRaisedEventArgs args)
	{
		if (args.Result == PostOrderResponse.Processed)
		{
			OrderUnlockUnitSkillLevel orderUnlockUnitSkillLevel = args.Order as OrderUnlockUnitSkillLevel;
			UnitSkill value = this.unitSkillDatabase.GetValue(orderUnlockUnitSkillLevel.UnitSkillName);
			AIData_Unit aidata = this.aiDataRepositoryHelper.GetAIData<AIData_Unit>(orderUnlockUnitSkillLevel.UnitGuid);
			InterpreterContext.InterpreterSession interpreterSession = new InterpreterContext.InterpreterSession(aidata.Unit);
			foreach (IAIParameter<InterpreterContext> iaiparameter in this.constructibleElementEvaluationAIHelper.GetAIParameters(value))
			{
				int num = Array.IndexOf<string>(AILayer_HeroAssignation.HeroAssignationTypeNames, iaiparameter.Name.ToString());
				if (num >= 0)
				{
					float num2 = iaiparameter.GetValue(interpreterSession.Context);
					if (orderUnlockUnitSkillLevel.UnitSkillLevel > 0)
					{
						num2 *= 0.5f;
					}
					aidata.HeroData.LongTermSpecialtyFitness[num] = AILayer.Boost(aidata.HeroData.LongTermSpecialtyFitness[num], num2);
				}
			}
		}
	}

	private List<StaticString> failureFlags;

	private List<InfiltrationActionData> infiltrationActionToPerform;

	private static string utilityRegistryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_HeroAssignation/Utility";

	private IAIEmpireDataAIHelper empireDataHelper;

	private float damageFortificationBesiegedByAlly;

	private float damageFortificationBesiegedByMe;

	private Amplitude.Unity.Framework.AnimationCurve decreasePopulationCurve;

	private float decreasePopulationMaximumPopulation;

	private float decreasePopulationMaximumYieldPerPopulation;

	private Amplitude.Unity.Framework.AnimationCurve decreasePopulationYieldCurve;

	private float decreasePopulationMissedPopulationFactor;

	private Amplitude.Unity.Framework.AnimationCurve decreaseVisionArmyVisibleBoostCurve;

	private float factorForInterestingArmies;

	private IGameStatisticsManagementService gameStatisticsManagement;

	private Amplitude.Unity.Framework.AnimationCurve hurtingWantWarCurve;

	private float leechMaximumNumberOfTradeRoute;

	private Amplitude.Unity.Framework.AnimationCurve leechNumberOfTradeRouteCurve;

	private float minimalUtilityBaseValue;

	private Amplitude.Unity.Framework.AnimationCurve minimalUtilitySecurityCurve;

	private Amplitude.Unity.Framework.AnimationCurve poisonGovernorAssignationCurve;

	private Amplitude.Unity.Framework.AnimationCurve poisonGovernorHeroLevelCurve;

	private float poisonGovernorHeroLevelMaximum;

	private List<DepartmentOfScience.ConstructibleElement> stealableTechnologies;

	private List<DecisionResult>[] stealableTechnologiesResult;

	private Dictionary<StaticString, AILayer_HeroAssignation.InfiltrationActionUtilityFunc> utilityFunctions;

	private List<StaticString> workerYields;

	private float referenceInfiltrationTurnCount;

	private float boostInfiltrationLevelFactor;

	private float boostWantWarScoreFactor;

	private float boostEmpireActionWhenNoCities;

	private float boostThresholdByHeroHealth;

	private float decreaseMoralAtWarWithMe;

	private float decreaseMoralAtWarWithAlly;

	private float decreaseMoralAtWarWithSomeone;

	private float decreaseMoralUnitCountMaximum;

	private float decreaseMoralUnitCountFactor;

	private Amplitude.Unity.Framework.AnimationCurve decreaseMoralUnitCountCurve;

	private float decreaseMoralMilitaryPowerFactor;

	private float decreaseMoralMilitaryPowerClamp;

	private Amplitude.Unity.Framework.AnimationCurve decreaseMoralMilitaryPowerCurve;

	private float decreaseProductionFactor;

	private float decreaseProductionClamp;

	private Amplitude.Unity.Framework.AnimationCurve decreaseProductionCurve;

	private float decreaseProductionSameWonderBoost;

	private float decreaseProductionUnitDuringWarBoost;

	private float commonActionLevelMaximum;

	private float commonActionLevelFactor;

	private float stealTechnologyNotAtMaxEraFactor;

	public static string RegistryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_HeroAssignation";

	private static string[] heroAssignationTypeNames;

	private static float heroUpkeepProjectionCostByLevel = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<float>("AI/AILayers/AILayer_Heroes/HeroUpkeepProjectionCostByLevel");

	private static float heroUpkeepProjectionTurnCount = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<float>("AI/AILayers/AILayer_Heroes/HeroUpkeepProjectionTurnCount");

	private IAIDataRepositoryAIHelper aiDataRepositoryHelper;

	private IDatabase<Amplitude.Unity.Framework.AnimationCurve> animationCurveDatabase;

	private List<AssignationData> assignationData;

	private Dictionary<string, Func<float>> assignationNeedFunctions;

	private IConstructibleElementEvaluationAIHelper constructibleElementEvaluationAIHelper;

	private List<DecisionResult> decisionResults;

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

	private List<AIData_Unit> heroAssignations;

	private int[] heroCountBySpecialty;

	private List<AIData_Unit> heroesToLevelUp;

	private IDatabase<InfiltrationAction> infiltrationActionDatabase;

	private SimulationDecisionMaker<UnitSkill> levelUpDecisionMaker;

	private float maximalHeroCount;

	private float maximalTurnForHero;

	private float maximumFractionOfHeroesSpy;

	private Ticket orderTicket;

	private IPersonalityAIHelper personalityAIHelper;

	private ITickableRepositoryAIHelper tickableRepositoryHelper;

	private ITradeDataRepository tradeDataRepository;

	private ITradeManagementService tradeManagementService;

	private List<UnitSkill> unitSkills;

	private IVisibilityService visibilityService;

	private IDownloadableContentService downloadableContentService;

	private float minimalTurnBeforeSpying;

	private float priceMarginToChooseBestHero;

	private float boostForNonSpecialityToChooseBestHero;

	private float limitToExfiltrate;

	private float BoostThresholdForBetterTechsteal;

	private float MinMoneyforRestore;

	private float TurnThresholdforRestore;

	private int[] stealableTechnologiesIndex;

	private bool assignJoblessHeros;

	private AILayer_Victory VictoryLayer;

	private List<KeyValuePair<string, string>> HeroSkillDecisions;

	private List<GameEntityGUID> VisibleSpyGarrisons;

	private List<InfiltrationActionData> DebugInfiltrationActionInfo;

	private AILayer_Diplomacy DiplomacyLayer;

	private Dictionary<GameEntityGUID, int> joblessHeros;

	private DepartmentOfPlanificationAndDevelopment departmentOfPlanificationAndDevelopment;

	private IDatabase<UnitSkill> unitSkillDatabase;

	private MajorEmpire majorEmpire;

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
