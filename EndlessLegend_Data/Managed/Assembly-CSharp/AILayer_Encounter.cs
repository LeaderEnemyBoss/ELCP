using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Collections;
using Amplitude.Unity.AI;
using Amplitude.Unity.AI.Decision;
using Amplitude.Unity.AI.Decision.Diagnostics;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Utilities.Maps;
using UnityEngine;

[PersonalityRegistryPath("AI/Battle/AILayer_Encounter/", new object[]
{

})]
public class AILayer_Encounter : AILayer, ISimulationAIEvaluationHelper<AIEncounterStrategyDefinition>, IAIEvaluationHelper<AIEncounterStrategyDefinition, InterpreterContext>
{
	public AILayer_Encounter()
	{
		this.DecisionMakerEvaluationDataHistoric = new FixedSizedList<DecisionMakerEvaluationData<AIEncounterStrategyDefinition, InterpreterContext>>(global::Application.FantasyPreferences.AIDebugHistoricSize);
		this.decisionResults = new List<DecisionResult>();
		this.lastEncounterAnalysis = new List<AILayer_Encounter.AIEncounterBattleGroundAnalysis>();
		this.fearfulness = 0.2f;
		this.balanceOfPowerDecreaseFactor = 0.25f;
		this.balanceOfPowerIncreaseFactor = 0.5f;
		base..ctor();
	}

	public event EventHandler<RoundUpdateEventArgs> EncounterRoundUpdate;

	public IEnumerable<AILayer_Encounter.AIEncounterBattleGroundAnalysis> Debug_LastEncounterAnalysis
	{
		get
		{
			return this.lastEncounterAnalysis;
		}
	}

	private IEnumerable<AILayer_Encounter.AIEncounterBattleGroundAnalysis> AnalyseContenders(Encounter encounter, ref FeedbackMessage_Battle battleFeedback, bool analyseContenders, bool fillSpellScoringGrids)
	{
		Diagnostics.Assert(this.lastEncounterAnalysis != null);
		this.lastEncounterAnalysis.Clear();
		battleFeedback.BattleState = FeedbackMessage_Battle.BattleStateType.Unknown;
		battleFeedback.IsAttacking = false;
		battleFeedback.AllyUnitCount = 0;
		battleFeedback.AllyHeroCount = 0;
		battleFeedback.AllyUnitPatternPower = 0f;
		battleFeedback.AllyMilitaryPower = 0f;
		battleFeedback.OpponentUnitCount = 0;
		battleFeedback.OpponentHeroCount = 0;
		battleFeedback.OpponentUnitPatternPower = 0f;
		battleFeedback.OpponentMilitaryPower = 0f;
		battleFeedback.UnitPatternStats = new Dictionary<StaticString, FeedbackMessage_Battle.UnitPatternStatistics>();
		List<StaticString> unitPatternCategories = this.unitPatternHelper.GetUnitPatternCategories();
		for (int i = 0; i < unitPatternCategories.Count; i++)
		{
			battleFeedback.UnitPatternStats.Add(unitPatternCategories[i], new FeedbackMessage_Battle.UnitPatternStatistics());
		}
		this.battleZone = null;
		BattleEncounter battleEncounter;
		if (analyseContenders && fillSpellScoringGrids && this.battleEncounterRepositoryService.TryGetValue(encounter.GUID, out battleEncounter))
		{
			this.battleZone = battleEncounter.BattleZoneAnalysis.BattleZone;
			for (int j = 0; j < this.spellAffinityDefinitions.Count; j++)
			{
				this.ResetSpellScoringGrid(this.spellAffinityDefinitions[j]);
			}
		}
		foreach (Contender contender in encounter.Contenders)
		{
			bool flag = contender.Empire == base.AIEntity.Empire;
			if (contender.IsTakingPartInBattle)
			{
				if (flag)
				{
					battleFeedback.IsAttacking = contender.IsAttacking;
				}
				else
				{
					battleFeedback.OpponentEmpireIndex = contender.Empire.Index;
				}
				foreach (EncounterUnit encounterUnit in contender.EncounterUnits)
				{
					Diagnostics.Assert(encounterUnit.Unit != null);
					float propertyValue = encounterUnit.GetPropertyValue(SimulationProperties.Health);
					if (propertyValue > 0f)
					{
						if (!analyseContenders || encounterUnit.WorldPosition.IsValid)
						{
							if (this.battleZone != null)
							{
								for (int k = 0; k < this.spellAffinityDefinitions.Count; k++)
								{
									AIEncounterSpellAffinityDefinition aiencounterSpellAffinityDefinition = this.spellAffinityDefinitions[k];
									if (flag && aiencounterSpellAffinityDefinition.Target == "Ally")
									{
										this.UpdateSpellScoringGrid(aiencounterSpellAffinityDefinition, encounterUnit);
									}
									else if (!flag && aiencounterSpellAffinityDefinition.Target == "Enemy")
									{
										this.UpdateSpellScoringGrid(aiencounterSpellAffinityDefinition, encounterUnit);
									}
								}
							}
							this.decisionResults.Clear();
							this.unitPatternHelper.ComputeAllEncounterUnitPatternAffinities(encounterUnit, ref this.decisionResults);
							AIUnitPatternDefinition aiunitPatternDefinition = this.decisionResults[0].Element as AIUnitPatternDefinition;
							if (analyseContenders && flag)
							{
								if (this.lastEncounterAnalysis.Count == 0 || this.lastEncounterAnalysis[this.lastEncounterAnalysis.Count - 1].ContenderGUID != contender.GUID)
								{
									this.lastEncounterAnalysis.Add(new AILayer_Encounter.AIEncounterBattleGroundAnalysis(contender.GUID));
								}
								if (encounterUnit.CanPlayBattleRound)
								{
									this.lastEncounterAnalysis[this.lastEncounterAnalysis.Count - 1].AllyUnitPlayingNextBattleRound.Add(new AILayer_Encounter.AIEncounterUnitAnalysis(encounterUnit.Unit.GUID, aiunitPatternDefinition.Name, aiunitPatternDefinition.Category));
								}
							}
							if (flag)
							{
								if (encounterUnit.Unit.IsHero())
								{
									battleFeedback.AllyHeroCount++;
								}
								else
								{
									battleFeedback.AllyUnitCount++;
								}
								battleFeedback.UnitPatternStats[aiunitPatternDefinition.Category].AllyCount++;
								battleFeedback.UnitPatternStats[aiunitPatternDefinition.Category].AllyPower += this.decisionResults[0].Score;
								battleFeedback.AllyUnitPatternPower += this.decisionResults[0].Score;
								battleFeedback.AllyMilitaryPower += encounterUnit.Unit.GetPropertyValue(SimulationProperties.MilitaryPower);
							}
							else
							{
								if (encounterUnit.Unit.IsHero())
								{
									battleFeedback.OpponentHeroCount++;
								}
								else
								{
									battleFeedback.OpponentUnitCount++;
								}
								battleFeedback.UnitPatternStats[aiunitPatternDefinition.Category].OpponentCount++;
								battleFeedback.UnitPatternStats[aiunitPatternDefinition.Category].OpponentPower += this.decisionResults[0].Score;
								battleFeedback.OpponentUnitPatternPower += this.decisionResults[0].Score;
								battleFeedback.OpponentMilitaryPower += encounterUnit.Unit.GetPropertyValue(SimulationProperties.MilitaryPower);
							}
						}
					}
				}
			}
		}
		return this.lastEncounterAnalysis;
	}

	private void ApplyStrategy(Encounter encounter, AIEncounterStrategyDefinition strategy, IEnumerable<AILayer_Encounter.AIEncounterBattleGroundAnalysis> analysisByContender)
	{
		foreach (Contender contender in encounter.Contenders)
		{
			if (contender.IsTakingPartInBattle && contender.Empire == base.AIEntity.Empire)
			{
				foreach (AILayer_Encounter.AIEncounterBattleGroundAnalysis aiencounterBattleGroundAnalysis in analysisByContender)
				{
					Diagnostics.Assert(aiencounterBattleGroundAnalysis != null);
					if (!(aiencounterBattleGroundAnalysis.ContenderGUID != contender.GUID))
					{
						OrderChangeUnitsTargetingAndStrategy orderChangeUnitsTargetingAndStrategy = new OrderChangeUnitsTargetingAndStrategy(encounter.GUID, contender.GUID, aiencounterBattleGroundAnalysis.AllyUnitPlayingNextBattleRound.Count);
						for (int i = 0; i < aiencounterBattleGroundAnalysis.AllyUnitPlayingNextBattleRound.Count; i++)
						{
							AILayer_Encounter.AIEncounterUnitAnalysis aiencounterUnitAnalysis = aiencounterBattleGroundAnalysis.AllyUnitPlayingNextBattleRound[i];
							bool flag = false;
							for (int j = 0; j < strategy.Behaviors.Length; j++)
							{
								AIEncounterStrategyDefinition.ArchetypeBehavior archetypeBehavior = strategy.Behaviors[j];
								if (archetypeBehavior.Archetype == aiencounterUnitAnalysis.UnitPattern)
								{
									orderChangeUnitsTargetingAndStrategy.SetUnitStrategy(i, aiencounterUnitAnalysis.UnitGUID, archetypeBehavior.Strategy);
									flag = true;
									break;
								}
							}
							if (!flag)
							{
								AILayer.LogError("[DATA] There is no behavior defined for the archetype '{0}' in encounter strategy '{1}'. TODO: define the behavior in file Public/AI/EncounterStrategies.xml", new object[]
								{
									aiencounterUnitAnalysis.UnitPattern,
									strategy.Name
								});
								orderChangeUnitsTargetingAndStrategy.SetUnitStrategy(i, aiencounterUnitAnalysis.UnitGUID, strategy.Name);
							}
						}
						base.AIEntity.Empire.PlayerControllers.AI.PostOrder(orderChangeUnitsTargetingAndStrategy);
					}
				}
			}
		}
	}

	private void ChooseAndApplyStrategy(Encounter encounter, Contender contender)
	{
		bool flag = encounter.BattlePhaseIndex == 0 && DepartmentOfTheInterior.CanInvokePillarsAndSpells(base.AIEntity.Empire);
		FeedbackMessage_Battle feedbackMessage_Battle = new FeedbackMessage_Battle();
		IEnumerable<AILayer_Encounter.AIEncounterBattleGroundAnalysis> enumerable = this.AnalyseContenders(encounter, ref feedbackMessage_Battle, true, flag);
		feedbackMessage_Battle.BattleState = FeedbackMessage_Battle.BattleStateType.Targeting;
		this.FillInterpreterContext(this.strategyDecisionMaker.Context, feedbackMessage_Battle);
		List<DecisionResult> strategyDecisionResults = feedbackMessage_Battle.StrategyDecisionResults;
		strategyDecisionResults.Clear();
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			DecisionMakerEvaluationData<AIEncounterStrategyDefinition, InterpreterContext> decisionMakerEvaluationData;
			this.strategyDecisionMaker.EvaluateDecisions(this.encounterStrategyDatabase, ref strategyDecisionResults, out decisionMakerEvaluationData);
			IGameService service = Services.GetService<IGameService>();
			decisionMakerEvaluationData.Turn = (service.Game as global::Game).Turn;
			this.DecisionMakerEvaluationDataHistoric.Add(decisionMakerEvaluationData);
		}
		else
		{
			this.strategyDecisionMaker.EvaluateDecisions(this.encounterStrategyDatabase, ref strategyDecisionResults);
		}
		if (feedbackMessage_Battle.StrategyDecisionResults.Count > 0 && enumerable != null)
		{
			AIEncounterStrategyDefinition strategy = feedbackMessage_Battle.StrategyDecisionResults[0].Element as AIEncounterStrategyDefinition;
			this.ApplyStrategy(encounter, strategy, enumerable);
		}
		base.AIEntity.AIPlayer.Blackboard.AddMessage(feedbackMessage_Battle);
		if (flag)
		{
			this.ChooseAndCastSpell(encounter, contender);
		}
	}

	private void ChooseAndCastSpell(Encounter encounter, Contender contender)
	{
		WorldPosition targetPosition = WorldPosition.Invalid;
		SpellDefinition spellDefinition = null;
		float num = 0f;
		foreach (AIEncounterSpellAffinityDefinition aiencounterSpellAffinityDefinition in this.spellAffinityDefinitions)
		{
			IDatabase<SpellDefinition> database = Databases.GetDatabase<SpellDefinition>(false);
			SpellDefinition spellDefinition2;
			if (database == null)
			{
				AILayer.LogError("Can't retrieve the spellDefinition's database.");
			}
			else if (!database.TryGetValue(aiencounterSpellAffinityDefinition.Name, out spellDefinition2))
			{
				AILayer.LogError("Can't retrieve the spellDefinition '{0}'.", new object[]
				{
					spellDefinition.Name
				});
			}
			else
			{
				bool flag = false;
				foreach (Prerequisite prerequisite in spellDefinition2.Prerequisites)
				{
					if (!prerequisite.Check(base.AIEntity.Empire))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					GridMap<float> gridMap = this.spellAffinityGridMaps[aiencounterSpellAffinityDefinition.Name];
					for (int j = 0; j < gridMap.Height; j++)
					{
						for (int k = 0; k < gridMap.Width; k++)
						{
							WorldPosition worldPosition = new WorldPosition(j, k);
							float value = gridMap.GetValue(worldPosition);
							if (value >= num)
							{
								targetPosition = this.battleZone.ConvertFromGridToWorldPosition(worldPosition);
								spellDefinition = spellDefinition2;
								num = value;
							}
						}
					}
				}
			}
		}
		if (num > 0f && base.AIEntity.GetLayer<AILayer_AccountManager>().TryMakeUnexpectedImmediateExpense(AILayer_AccountManager.MilitaryAccountName, this.GetSpellDustCost(spellDefinition), 0f))
		{
			OrderBuyoutSpellAndPlayBattleAction order = new OrderBuyoutSpellAndPlayBattleAction(encounter.GUID, contender.GUID, targetPosition, spellDefinition);
			base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order);
		}
	}

	private bool ContenderContainsSettler(Contender contender)
	{
		if (contender.EncounterUnits.Count > 3)
		{
			return false;
		}
		bool result = false;
		for (int i = 0; i < contender.EncounterUnits.Count; i++)
		{
			EncounterUnit encounterUnit = contender.EncounterUnits[i];
			if (encounterUnit.Unit.CheckUnitAbility(UnitAbility.ReadonlyColonize, -1))
			{
				result = true;
				break;
			}
		}
		return result;
	}

	private float DecisionParameterContextModifierDelegate(AIEncounterStrategyDefinition aiEvaluableElement, StaticString aiParameterName)
	{
		for (int i = 0; i < aiEvaluableElement.Modifiers.Length; i++)
		{
			AIParameter.AIModifier aimodifier = aiEvaluableElement.Modifiers[i];
			if (aimodifier.Name == aiParameterName)
			{
				return aimodifier.Value;
			}
		}
		return 0f;
	}

	private void FillInterpreterContext(InterpreterContext interpreterContext, FeedbackMessage_Battle battleFeedback)
	{
		interpreterContext.Clear();
		interpreterContext.Register("IsAttacking", (!battleFeedback.IsAttacking) ? 0f : 1f);
		interpreterContext.Register("Ally_UnitCount", (float)battleFeedback.AllyUnitCount);
		interpreterContext.Register("Ally_HeroCount", (float)battleFeedback.AllyHeroCount);
		interpreterContext.Register("Ally_Power", battleFeedback.AllyUnitPatternPower);
		interpreterContext.Register("Opponent_UnitCount", (float)battleFeedback.OpponentUnitCount);
		interpreterContext.Register("Opponent_HeroCount", (float)battleFeedback.OpponentHeroCount);
		interpreterContext.Register("Opponent_Power", battleFeedback.OpponentUnitPatternPower);
		foreach (KeyValuePair<StaticString, FeedbackMessage_Battle.UnitPatternStatistics> keyValuePair in battleFeedback.UnitPatternStats)
		{
			interpreterContext.Register("Ally_" + keyValuePair.Key + "_Count", (float)keyValuePair.Value.AllyCount);
			interpreterContext.Register("Ally_" + keyValuePair.Key + "_Power", keyValuePair.Value.AllyPower);
			interpreterContext.Register("Opponent_" + keyValuePair.Key + "_Count", (float)keyValuePair.Value.OpponentCount);
			interpreterContext.Register("Opponent_" + keyValuePair.Key + "_Power", keyValuePair.Value.OpponentPower);
		}
	}

	private float GetSpellDustCost(SpellDefinition spellDefinition)
	{
		DepartmentOfTheTreasury agency = base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>();
		ConstructionResourceStock[] array;
		agency.GetInstantConstructionResourceCostForBuyout(base.AIEntity.Empire, spellDefinition, out array);
		float result = 0f;
		if (array != null)
		{
			for (int i = 0; i < array.Length; i++)
			{
				if (!(array[i].PropertyName == SimulationProperties.EmpireMoney))
				{
					return float.PositiveInfinity;
				}
				result = array[i].Stock;
			}
		}
		return result;
	}

	private void InitializeSpellDefinitions()
	{
		Diagnostics.Assert(this.encounterStrategyAiParameters == null);
		IDatabase<AIEncounterSpellAffinityDefinition> database = Databases.GetDatabase<AIEncounterSpellAffinityDefinition>(false);
		Diagnostics.Assert(database != null);
		this.spellAffinityDefinitions = new List<AIEncounterSpellAffinityDefinition>();
		foreach (AIEncounterSpellAffinityDefinition aiencounterSpellAffinityDefinition in database)
		{
			if (aiencounterSpellAffinityDefinition.CheckProportions())
			{
				this.spellAffinityDefinitions.Add(aiencounterSpellAffinityDefinition);
			}
			else
			{
				AILayer.LogWarning("[SCORING] Spell Affinity Definition {0} has been ignored because the sum of its proportions is not 100", new object[]
				{
					aiencounterSpellAffinityDefinition.Name
				});
			}
		}
		this.spellAffinityGridMaps = new Dictionary<StaticString, GridMap<float>>();
	}

	private void InitializeStrategies()
	{
		this.encounterStrategyDatabase = Databases.GetDatabase<AIEncounterStrategyDefinition>(false);
		this.unitPatternHelper = AIScheduler.Services.GetService<IUnitPatternAIHelper>();
		this.intelligenceAIHelper = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.battleEncounterRepositoryService = service.Game.Services.GetService<IBattleEncounterRepositoryService>();
		Diagnostics.Assert(this.battleEncounterRepositoryService != null);
		this.InitializeSpellDefinitions();
		this.InitializeAiParameters();
		this.strategyDecisionMaker = new SimulationDecisionMaker<AIEncounterStrategyDefinition>(this, null);
		this.strategyDecisionMaker.ParameterContextModifierDelegate = new Func<AIEncounterStrategyDefinition, StaticString, float>(this.DecisionParameterContextModifierDelegate);
		this.strategyDecisionMaker.UnregisterAllOutput();
		for (int i = 0; i < this.encounterStrategyAiParameters.Length; i++)
		{
			this.strategyDecisionMaker.RegisterOutput(this.encounterStrategyAiParameters[i].Name);
		}
	}

	private void ReleaseStrategies()
	{
		this.unitPatternHelper = null;
		this.intelligenceAIHelper = null;
		this.encounterStrategyDatabase = null;
		this.strategyDecisionMaker = null;
		this.worldPositionningService = null;
		this.battleEncounterRepositoryService = null;
		this.spellAffinityDefinitions = null;
		this.spellAffinityGridMaps = null;
	}

	private void ReportBattleInitialState(Encounter encounter, Contender contender)
	{
		if (!(base.AIEntity.Empire is MajorEmpire))
		{
			return;
		}
		if (contender.Empire != base.AIEntity.Empire || !contender.IsMainContender)
		{
			return;
		}
	}

	private void ReportBattleResult(Encounter encounter, Contender contender)
	{
		if (!(base.AIEntity.Empire is MajorEmpire))
		{
			return;
		}
		if (contender.Empire != base.AIEntity.Empire || !contender.IsMainContender)
		{
			return;
		}
		this.UpdateStrengths(encounter);
		FeedbackMessage_Battle feedbackMessage_Battle = new FeedbackMessage_Battle();
		this.AnalyseContenders(encounter, ref feedbackMessage_Battle, false, false);
		if (feedbackMessage_Battle.AllyUnitCount + feedbackMessage_Battle.AllyHeroCount == 0)
		{
			feedbackMessage_Battle.BattleState = FeedbackMessage_Battle.BattleStateType.Finished_Lost;
		}
		else if (feedbackMessage_Battle.OpponentUnitCount + feedbackMessage_Battle.OpponentHeroCount == 0)
		{
			feedbackMessage_Battle.BattleState = FeedbackMessage_Battle.BattleStateType.Finished_Won;
		}
		else
		{
			feedbackMessage_Battle.BattleState = FeedbackMessage_Battle.BattleStateType.Finished_Draw;
		}
		base.AIEntity.AIPlayer.Blackboard.AddMessage(feedbackMessage_Battle);
	}

	private void ResetSpellScoringGrid(AIEncounterSpellAffinityDefinition spellAffinityDefinition)
	{
		GridMap<float> gridMap;
		if (!this.spellAffinityGridMaps.TryGetValue(spellAffinityDefinition.Name, out gridMap) || gridMap == null || gridMap.Height != this.battleZone.Height || gridMap.Width != this.battleZone.Width)
		{
			this.spellAffinityGridMaps.Remove(spellAffinityDefinition.Name);
			this.spellAffinityGridMaps.Add(spellAffinityDefinition.Name, new GridMap<float>(spellAffinityDefinition.Name, this.battleZone.Width, this.battleZone.Height, null));
		}
		else
		{
			for (int i = 0; i < gridMap.Data.Length; i++)
			{
				gridMap.Data[i] = 0f;
			}
		}
	}

	private void UpdateSpellScoringGrid(AIEncounterSpellAffinityDefinition spellAffinityDefinition, EncounterUnit encounterUnit)
	{
		GridMap<float> gridMap = this.spellAffinityGridMaps[spellAffinityDefinition.Name];
		this.spellAffinityGridMaps.TryGetValue(spellAffinityDefinition.Name, out gridMap);
		float num = this.unitPatternHelper.ComputeEncounterUnitAffinity(encounterUnit, spellAffinityDefinition);
		if (num > 0f)
		{
			foreach (WorldPosition worldPosition in WorldPosition.ParseTilesInRange(encounterUnit.WorldPosition, spellAffinityDefinition.Range, this.worldPositionningService.World.WorldParameters))
			{
				WorldPosition worldPosition2 = this.battleZone.ConvertFromWorldToGridPosition(worldPosition);
				if (worldPosition2.IsValid && gridMap.Height > (int)worldPosition2.Row && gridMap.Width > (int)worldPosition2.Column)
				{
					gridMap.SetValue(worldPosition2, gridMap.GetValue(worldPosition2) + num);
				}
			}
		}
	}

	private void UpdateStrengths(Encounter encounter)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		float num5 = 0.01f;
		float num6 = 0.01f;
		float num7 = 0.01f;
		float num8 = 0.01f;
		foreach (Contender contender in encounter.Contenders)
		{
			bool flag = contender.Empire == base.AIEntity.Empire;
			if (contender.IsTakingPartInBattle)
			{
				foreach (EncounterUnit encounterUnit in contender.EncounterUnits)
				{
					float num9 = Math.Max(0f, encounterUnit.GetPropertyValue(SimulationProperties.Health));
					if (encounterUnit.WorldPosition.IsValid || num9 <= 0f)
					{
						Diagnostics.Assert(encounterUnit.Unit != null);
						float propertyValue = encounterUnit.Unit.GetPropertyValue(SimulationProperties.MilitaryPower);
						float propertyValue2 = encounterUnit.GetPropertyValue(SimulationProperties.MaximumHealth);
						float num10 = Math.Max(0f, encounterUnit.Unit.GetPropertyValue(SimulationProperties.Health));
						float num11 = (num9 - num10) / propertyValue2;
						float num12 = propertyValue + propertyValue * num11;
						if (flag)
						{
							num++;
							if (num9 > 0f)
							{
								num2++;
							}
							num5 += propertyValue;
							num7 += num12;
						}
						else
						{
							num3++;
							if (num9 > 0f)
							{
								num4++;
							}
							num6 += propertyValue;
							num8 += num12;
						}
					}
				}
			}
		}
		num5 = Math.Max(num5, num5 * (float)num);
		num7 = Math.Max(num7, num7 * (float)num2);
		num6 = Math.Max(num6, num6 * (float)num3);
		num8 = Math.Max(num8, num8 * (float)num4);
		num5 *= 1f + Math.Max(-0.99f, this.fearfulness);
		float num13 = (num5 <= num6) ? ((num5 - num6) / num6) : ((num5 - num6) / num5);
		float num14 = (num7 <= num8) ? ((num7 - num8) / Math.Max(num6, num8)) : ((num7 - num8) / Math.Max(num5, num7));
		float num15 = (num13 - num14) / 2f;
		float num16 = Math.Max(0f, this.balanceOfPowerDecreaseFactor);
		float num17 = Math.Max(0f, this.balanceOfPowerIncreaseFactor);
		float boost = (num15 >= 0f) ? (num15 * num17 / (float)num3) : (num15 * num16 / (float)num3);
		foreach (Contender contender2 in encounter.Contenders)
		{
			bool flag2 = contender2.Empire == base.AIEntity.Empire;
			if (contender2.IsTakingPartInBattle && !flag2)
			{
				foreach (EncounterUnit encounterUnit2 in contender2.EncounterUnits)
				{
					float num18 = Math.Max(0f, encounterUnit2.GetPropertyValue(SimulationProperties.Health));
					if (encounterUnit2.WorldPosition.IsValid || num18 <= 0f)
					{
						this.intelligenceAIHelper.UpdateAIStrengthBelief(base.AIEntity.Empire.Index, encounterUnit2.Unit.UnitDesign.UnitBodyDefinition.Name, boost);
					}
				}
			}
		}
	}

	public IEnumerable<IAIParameterConverter<InterpreterContext>> GetAIParameterConverters(StaticString aiParameterName)
	{
		Diagnostics.Assert(this.aiParameterConverterDatabase != null);
		AIParameterConverter aiParameterConverter;
		if (!this.aiParameterConverterDatabase.TryGetValue(aiParameterName, out aiParameterConverter))
		{
			yield break;
		}
		Diagnostics.Assert(aiParameterConverter != null);
		if (aiParameterConverter.ToAIParameters == null)
		{
			yield break;
		}
		for (int index = 0; index < aiParameterConverter.ToAIParameters.Length; index++)
		{
			yield return aiParameterConverter.ToAIParameters[index];
		}
		yield break;
	}

	public IEnumerable<IAIParameter<InterpreterContext>> GetAIParameters(AIEncounterStrategyDefinition strategyElement)
	{
		Diagnostics.Assert(this.encounterStrategyAiParameters != null);
		for (int index = 0; index < this.encounterStrategyAiParameters.Length; index++)
		{
			yield return this.encounterStrategyAiParameters[index];
		}
		yield break;
	}

	public IEnumerable<IAIPrerequisite<InterpreterContext>> GetAIPrerequisites(AIEncounterStrategyDefinition element)
	{
		yield break;
	}

	private void InitializeAiParameters()
	{
		Diagnostics.Assert(this.encounterStrategyAiParameters == null);
		IDatabase<AIParameterDatatableElement> database = Databases.GetDatabase<AIParameterDatatableElement>(false);
		Diagnostics.Assert(database != null);
		this.aiParameterConverterDatabase = Databases.GetDatabase<AIParameterConverter>(false);
		AIParameterDatatableElement aiparameterDatatableElement;
		if (database.TryGetValue("EncounterStrategyEvaluation", out aiparameterDatatableElement))
		{
			Diagnostics.Assert(aiparameterDatatableElement != null);
			if (aiparameterDatatableElement.AIParameters != null)
			{
				this.encounterStrategyAiParameters = new IAIParameter<InterpreterContext>[aiparameterDatatableElement.AIParameters.Length];
				for (int i = 0; i < aiparameterDatatableElement.AIParameters.Length; i++)
				{
					AIParameterDatatableElement.AIParameter aiparameter = aiparameterDatatableElement.AIParameters[i];
					Diagnostics.Assert(aiparameter != null);
					this.encounterStrategyAiParameters[i] = aiparameter.Instantiate();
				}
			}
			else
			{
				AILayer.LogWarning("[DATA] EncounterStrategyEvaluation has no AI Parameters.");
			}
		}
		else
		{
			AILayer.LogWarning("[DATA] EncounterStrategyEvaluation has no AI Parameters.");
		}
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		this.encounterRepositoryService = gameService.Game.Services.GetService<IEncounterRepositoryService>();
		Diagnostics.Assert(this.encounterRepositoryService != null);
		this.weatherService = gameService.Game.Services.GetService<IWeatherService>();
		this.encounterRepositoryService.EncounterRepositoryChange += this.EncounterRepositoryService_EncounterRepositoryChange;
		aiEntity.AIPlayer.AIPlayerStateChange += this.AIPlayer_AIPlayerStateChange;
		this.InitializeStrategies();
		yield break;
	}

	public override bool IsActive()
	{
		return true;
	}

	public override void Release()
	{
		this.ReleaseStrategies();
		base.AIEntity.AIPlayer.AIPlayerStateChange -= this.AIPlayer_AIPlayerStateChange;
		base.Release();
		this.weatherService = null;
		if (this.encounterRepositoryService != null)
		{
			this.encounterRepositoryService.EncounterRepositoryChange -= this.EncounterRepositoryService_EncounterRepositoryChange;
			this.encounterRepositoryService = null;
		}
	}

	private void AIPlayer_AIPlayerStateChange(object sender, EventArgs eventArgs)
	{
		foreach (Encounter encounter in this.encounterRepositoryService)
		{
			for (int i = 0; i < encounter.Contenders.Count; i++)
			{
				Contender contender = encounter.Contenders[i];
				if (contender.Empire == base.AIEntity.Empire)
				{
					switch (contender.ContenderState)
					{
					case ContenderState.Setup:
						if (!encounter.Empires.Contains(base.AIEntity.Empire))
						{
							this.RemoveEncounter(encounter);
						}
						else
						{
							this.DefineContenderStateAtSetup(encounter);
						}
						i = encounter.Contenders.Count;
						break;
					case ContenderState.Deployment:
						this.PostReadyForBattleOrder(encounter, contender);
						break;
					case ContenderState.ReadyForBattle:
						if (encounter.EncounterState == EncounterState.BattleIsPending)
						{
							this.PostOrderSetDeploymentFinishedOrder(encounter, contender);
						}
						break;
					case ContenderState.TargetingPhaseInProgress:
						this.PostReadyForNextPhaseOrder(encounter, contender);
						break;
					case ContenderState.RoundInProgress:
						this.PostReadyForNextRoundOrder(encounter, contender);
						break;
					}
				}
			}
		}
	}

	private bool CanPostOrdersForContender(Contender contender)
	{
		if (contender == null)
		{
			throw new ArgumentNullException("contender");
		}
		if (!this.IsActive())
		{
			return false;
		}
		Diagnostics.Assert(base.AIEntity != null && base.AIEntity.AIPlayer != null);
		if (contender.Empire != base.AIEntity.Empire)
		{
			return false;
		}
		if (base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI)
		{
			return true;
		}
		if (base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.WaitingForDeconexionConfirmation)
		{
			return true;
		}
		if (base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByHuman && contender.ContenderState == ContenderState.Setup)
		{
			return false;
		}
		switch (contender.ContenderEncounterOptionChoice)
		{
		case EncounterOptionChoice.Manual:
		case EncounterOptionChoice.Retreat:
			return false;
		case EncounterOptionChoice.Simulated:
		case EncounterOptionChoice.Spectator:
			return true;
		default:
			return false;
		}
	}

	private bool CanPostReadyOrdersForContender(Contender contender)
	{
		if (contender == null)
		{
			throw new ArgumentNullException("contender");
		}
		if (!this.CanPostOrdersForContender(contender))
		{
			return false;
		}
		if (base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI)
		{
			return true;
		}
		switch (contender.ContenderEncounterOptionChoice)
		{
		case EncounterOptionChoice.Manual:
		case EncounterOptionChoice.Spectator:
			return false;
		case EncounterOptionChoice.Simulated:
		case EncounterOptionChoice.Retreat:
			return true;
		default:
			return false;
		}
	}

	private void ClearMyEncounters()
	{
		while (this.myEncounters.Count > 0)
		{
			this.RemoveEncounter(this.myEncounters[0]);
		}
	}

	private bool DoIWantToIncludeContenderInEncounter(Encounter encounter, Contender contender)
	{
		return !this.ContenderContainsSettler(contender) || (contender.IsMainContender && !contender.IsAttacking && !(contender is Contender_City));
	}

	private bool DoContenderWantToRetreat(Encounter encounter, Contender contender)
	{
		if (!Array.Exists<GameEntityGUID>(encounter.OrderCreateEncounter.ContenderGUIDs, (GameEntityGUID match) => match == contender.GUID))
		{
			return false;
		}
		if (contender.IsAttacking)
		{
			return false;
		}
		if (contender is Contender_City)
		{
			return false;
		}
		District district = this.worldPositionningService.GetDistrict(contender.WorldPosition);
		bool flag = district != null && district.Empire == contender.Empire && district.City.BesiegingEmpire != null;
		if (flag)
		{
			return false;
		}
		bool flag2 = district != null && district.City.BesiegingEmpire == contender.Empire;
		return !flag2 && this.ContenderContainsSettler(contender) && contender.HasEnoughActionPoint(1f);
	}

	private void Encounter_ContenderCollectionChange(object sender, ContenderCollectionChangeEventArgs eventArgs)
	{
		switch (eventArgs.Action)
		{
		}
	}

	private void DefineContenderStateAtSetup(Encounter encounter)
	{
		if (!(base.AIEntity.Empire is MajorEmpire) || global::Application.FantasyPreferences.ForceReinforcementToParticipate)
		{
			for (int i = 0; i < encounter.Contenders.Count; i++)
			{
				Contender contender6 = encounter.Contenders[i];
				if (contender6.Empire == base.AIEntity.Empire)
				{
					if (!contender6.IsMainContender)
					{
						this.PostIncludeContenderInEncounterOrder(encounter, contender6, contender6.IsTakingPartInBattle);
					}
					this.PostReadyForDeploymentOrder(encounter, contender6, false);
				}
			}
			return;
		}
		Contender contender2 = null;
		Contender contender3 = null;
		List<IGarrison> list = new List<IGarrison>();
		List<IGarrison> list2 = new List<IGarrison>();
		DeploymentArea deploymentArea = null;
		DeploymentArea deploymentArea2 = null;
		Contender contender4 = null;
		bool flag = false;
		bool flag2 = false;
		for (int j = 0; j < encounter.Contenders.Count; j++)
		{
			Contender contender = encounter.Contenders[j];
			if (contender.Empire != base.AIEntity.Empire)
			{
				if (Array.Exists<GameEntityGUID>(encounter.OrderCreateEncounter.ContenderGUIDs, (GameEntityGUID match) => match == contender.GUID))
				{
					flag2 = this.DoContenderWantToRetreat(encounter, contender);
					if (flag2)
					{
						break;
					}
				}
				if (contender.IsMainContender)
				{
					contender3 = contender;
					deploymentArea2 = contender.Deployment.DeploymentArea;
				}
				else if (this.DoIWantToIncludeContenderInEncounter(encounter, contender) && contender.IsTakingPartInBattle)
				{
					list2.Add(contender.Garrison);
				}
			}
			else
			{
				if (Array.Exists<GameEntityGUID>(encounter.OrderCreateEncounter.ContenderGUIDs, (GameEntityGUID match) => match == contender.GUID))
				{
					flag = this.DoContenderWantToRetreat(encounter, contender);
					contender4 = contender;
					if (flag)
					{
						break;
					}
				}
				if (contender.IsMainContender)
				{
					contender2 = contender;
					deploymentArea = contender.Deployment.DeploymentArea;
				}
				else if (!this.DoIWantToIncludeContenderInEncounter(encounter, contender))
				{
					this.PostIncludeContenderInEncounterOrder(encounter, contender, false);
				}
				else if (contender.IsTakingPartInBattle)
				{
					list.Add(contender.Garrison);
				}
			}
		}
		if (contender2 == null)
		{
			flag = true;
		}
		if (flag || flag2)
		{
			this.AskForNoReinforcement(encounter, flag);
			return;
		}
		if (!this.CanPostOrdersForContender(contender2))
		{
			return;
		}
		list.Sort(delegate(IGarrison left, IGarrison right)
		{
			if (left.UnitsCount == 0)
			{
				return -1;
			}
			if (right.UnitsCount == 0)
			{
				return 1;
			}
			float num9 = left.GetPropertyValue(SimulationProperties.MilitaryPower) / (float)left.UnitsCount;
			float value = right.GetPropertyValue(SimulationProperties.MilitaryPower) / (float)right.UnitsCount;
			return -1 * num9.CompareTo(value);
		});
		float num = 0f;
		float num2 = 0f;
		int num3 = (int)contender2.Garrison.GetPropertyValue(SimulationProperties.ReinforcementPointCount);
		int num4 = (int)contender3.Garrison.GetPropertyValue(SimulationProperties.ReinforcementPointCount);
		int num5 = Mathf.Min(num3 * this.intelligenceAIHelper.NumberOfBattleRound + contender2.Garrison.UnitsCount, deploymentArea.Count);
		int availableTile = Mathf.Min(num4 * this.intelligenceAIHelper.NumberOfBattleRound + contender3.Garrison.UnitsCount, deploymentArea2.Count);
		this.intelligenceAIHelper.ComputeMPBasedOnBattleArea(contender2.Garrison, list, num5, ref num);
		this.intelligenceAIHelper.ComputeMPBasedOnBattleArea(contender3.Garrison, list2, availableTile, ref num2);
		float num6 = 2f;
		if (num2 > 0f)
		{
			num6 = num / num2;
		}
		if ((double)num6 < 0.8)
		{
			District district = this.worldPositionningService.GetDistrict(contender2.WorldPosition);
			bool flag3 = false;
			bool flag4 = false;
			if (district != null && district.City.BesiegingEmpire != null)
			{
				if (district.City.Empire == base.AIEntity.Empire)
				{
					flag3 = true;
				}
				else
				{
					flag4 = true;
				}
			}
			if (!flag3 || !(contender2.Garrison is Army))
			{
				flag = false;
				if (!contender2.IsAttacking && !(contender4 is Contender_City) && !flag4 && contender2.HasEnoughActionPoint(1f))
				{
					float num7 = Mathf.Clamp01(contender2.Garrison.GetPropertyValue(SimulationProperties.Health) / contender2.Garrison.GetPropertyValue(SimulationProperties.MaximumHealth));
					flag = (num7 > 0.6f);
					num7 -= 0.5f;
					if (contender2.Garrison.HasTag(WeatherManager.LightningTarget))
					{
						float propertyValue = contender2.Garrison.GetPropertyValue(SimulationProperties.Movement);
						if (propertyValue <= 0f && num7 <= this.weatherService.LightningDamageInPercent)
						{
							flag = false;
						}
					}
				}
				this.AskForNoReinforcement(encounter, flag);
				return;
			}
			num6 = 2f;
		}
		if (num6 <= 1f)
		{
			list.Sort(delegate(IGarrison left, IGarrison right)
			{
				if (left.UnitsCount == 0)
				{
					return -1;
				}
				if (right.UnitsCount == 0)
				{
					return 1;
				}
				if (left is City && !(right is City))
				{
					return -1;
				}
				if (!(left is City) && right is City)
				{
					return 1;
				}
				float num9 = left.GetPropertyValue(SimulationProperties.MilitaryPower) / (float)left.UnitsCount;
				float value = right.GetPropertyValue(SimulationProperties.MilitaryPower) / (float)right.UnitsCount;
				return -1 * num9.CompareTo(value);
			});
		}
		if (list.Count > 1)
		{
			int[] array = new int[list.Count];
			GameEntityGUID[] array2 = new GameEntityGUID[list.Count];
			for (int k = 0; k < list.Count; k++)
			{
				array[k] = k;
				array2[k] = list[k].GUID;
			}
			OrderChangeContenderReinforcementRanking order = new OrderChangeContenderReinforcementRanking(encounter.GUID, array2, array);
			base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order);
		}
		float num8 = contender2.Garrison.GetPropertyValue(SimulationProperties.MilitaryPower);
		num5 -= contender2.Garrison.UnitsCount;
		for (int l = 0; l < list.Count; l++)
		{
			bool include = true;
			if (num5 <= 0)
			{
				include = false;
			}
			else if (num8 > num2 * 2f)
			{
				include = false;
			}
			OrderIncludeContenderInEncounter order2 = new OrderIncludeContenderInEncounter(encounter.GUID, list[l].GUID, include);
			base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order2);
			num5 -= list[l].UnitsCount;
			num8 += list[l].GetPropertyValue(SimulationProperties.MilitaryPower);
		}
		for (int m = 0; m < encounter.Contenders.Count; m++)
		{
			Contender contender5 = encounter.Contenders[m];
			if (contender5.Empire == base.AIEntity.Empire)
			{
				this.PostReadyForDeploymentOrder(encounter, contender5, false);
			}
		}
	}

	private void AskForNoReinforcement(Encounter encounter, bool doIRetreat)
	{
		for (int i = 0; i < encounter.Contenders.Count; i++)
		{
			Contender contender = encounter.Contenders[i];
			if (contender.Empire == base.AIEntity.Empire)
			{
				if (!contender.IsMainContender)
				{
					OrderIncludeContenderInEncounter order = new OrderIncludeContenderInEncounter(encounter.GUID, contender.GUID, false);
					base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order);
				}
				this.PostReadyForDeploymentOrder(encounter, contender, doIRetreat);
			}
		}
	}

	private void Encounter_EncounterStateChange(object sender, EncounterStateChangeEventArgs eventArgs)
	{
		if (eventArgs.EncounterState != EncounterState.Setup)
		{
			for (int i = 0; i < eventArgs.Encounter.Contenders.Count; i++)
			{
				Contender contender = eventArgs.Encounter.Contenders[i];
				if (contender.Empire == base.AIEntity.Empire)
				{
					switch (eventArgs.EncounterState)
					{
					case EncounterState.Deployment:
						this.ReportBattleInitialState(eventArgs.Encounter, contender);
						this.PostReadyForBattleOrder(eventArgs.Encounter, contender);
						break;
					case EncounterState.BattleIsPending:
						this.PostOrderSetDeploymentFinishedOrder(eventArgs.Encounter, contender);
						break;
					case EncounterState.BattleIsReporting:
						this.ReportBattleResult(eventArgs.Encounter, contender);
						break;
					}
				}
			}
			return;
		}
		if (!eventArgs.Encounter.Empires.Contains(base.AIEntity.Empire))
		{
			this.RemoveEncounter(eventArgs.Encounter);
			return;
		}
		this.DefineContenderStateAtSetup(eventArgs.Encounter);
	}

	private void Encounter_RoundUpdate(object sender, RoundUpdateEventArgs eventArgs)
	{
		if (eventArgs.Encounter == null)
		{
			return;
		}
		Diagnostics.Assert(eventArgs.Encounter.Contenders != null);
		for (int i = 0; i < eventArgs.Encounter.Contenders.Count; i++)
		{
			Contender contender = eventArgs.Encounter.Contenders[i];
			if (contender.Empire == base.AIEntity.Empire)
			{
				this.PostReadyForNextRoundOrder(eventArgs.Encounter, contender);
			}
		}
		if (this.EncounterRoundUpdate != null)
		{
			this.EncounterRoundUpdate(this, eventArgs);
		}
	}

	private void Encounter_TargetingPhaseUpdate(object sender, TargetingPhaseStartEventArgs eventArgs)
	{
		if (eventArgs.Encounter == null)
		{
			return;
		}
		for (int i = 0; i < eventArgs.Encounter.Contenders.Count; i++)
		{
			Contender contender = eventArgs.Encounter.Contenders[i];
			if (contender.Empire == base.AIEntity.Empire)
			{
				this.PostReadyForNextPhaseOrder(eventArgs.Encounter, contender);
			}
		}
	}

	private void EncounterRepositoryService_EncounterRepositoryChange(object sender, EncounterRepositoryChangeEventArgs eventArgs)
	{
		Encounter encounter;
		if (!this.encounterRepositoryService.TryGetValue(eventArgs.EncounterGUID, out encounter))
		{
			return;
		}
		Diagnostics.Assert(encounter != null);
		Diagnostics.Assert(this.myEncounters != null);
		if (eventArgs.Action == EncounterRepositoryChangeAction.Add)
		{
			this.myEncounters.Add(encounter);
			encounter.ContenderCollectionChange += this.Encounter_ContenderCollectionChange;
			encounter.EncounterStateChange += this.Encounter_EncounterStateChange;
			encounter.RoundUpdate += this.Encounter_RoundUpdate;
			encounter.TargetingPhaseUpdate += this.Encounter_TargetingPhaseUpdate;
		}
		else if (eventArgs.Action == EncounterRepositoryChangeAction.Remove)
		{
			this.RemoveEncounter(encounter);
		}
		else if (eventArgs.Action == EncounterRepositoryChangeAction.Clear)
		{
			this.ClearMyEncounters();
		}
	}

	private void PostIncludeContenderInEncounterOrder(Encounter encounter, Contender contender, bool include)
	{
		if (!this.CanPostOrdersForContender(contender))
		{
			return;
		}
		OrderIncludeContenderInEncounter order = new OrderIncludeContenderInEncounter(encounter.GUID, contender.GUID, include);
		base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order);
	}

	private void PostOrderSetDeploymentFinishedOrder(Encounter encounter, Contender contender)
	{
		if (!encounter.OrderCreateEncounter.IsAutomaticBattle && !this.CanPostOrdersForContender(contender))
		{
			return;
		}
		OrderSetDeploymentFinished order = new OrderSetDeploymentFinished(encounter.GUID, contender.GUID);
		base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order);
	}

	private void PostReadyForBattleOrder(Encounter encounter, Contender contender)
	{
		if (!encounter.OrderCreateEncounter.IsAutomaticBattle && !this.CanPostOrdersForContender(contender))
		{
			return;
		}
		OrderReadyForBattle order = new OrderReadyForBattle(encounter.GUID, contender.GUID);
		base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order);
	}

	private void PostReadyForDeploymentOrder(Encounter encounter, Contender contender, bool retreat)
	{
		if (!this.CanPostOrdersForContender(contender))
		{
			return;
		}
		EncounterOptionChoice contenderEncounterOptionChoice = EncounterOptionChoice.Simulated;
		if (!encounter.OrderCreateEncounter.Instant && global::Application.FantasyPreferences.EnableAIBattleSpectatorMode)
		{
			contenderEncounterOptionChoice = EncounterOptionChoice.Spectator;
		}
		OrderReadyForDeployment order = new OrderReadyForDeployment(encounter.GUID, contender.GUID, contenderEncounterOptionChoice, retreat);
		base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order);
	}

	private void PostReadyForDeploymentOrder(Encounter encounter, GameEntityGUID contenderGuid, bool retreat)
	{
		EncounterOptionChoice contenderEncounterOptionChoice = EncounterOptionChoice.Simulated;
		if (!encounter.OrderCreateEncounter.Instant && global::Application.FantasyPreferences.EnableAIBattleSpectatorMode)
		{
			contenderEncounterOptionChoice = EncounterOptionChoice.Spectator;
		}
		OrderReadyForDeployment order = new OrderReadyForDeployment(encounter.GUID, contenderGuid, contenderEncounterOptionChoice, retreat);
		base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order);
	}

	private void PostReadyForNextPhaseOrder(Encounter encounter, Contender contender)
	{
		if (!this.CanPostOrdersForContender(contender))
		{
			return;
		}
		if (base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI && contender.IsMainContender)
		{
			this.ChooseAndApplyStrategy(encounter, contender);
		}
		OrderReadyForNextPhase order = new OrderReadyForNextPhase(encounter.GUID, contender.GUID);
		base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order);
	}

	private void PostReadyForNextRoundOrder(Encounter encounter, Contender contender)
	{
		if (!this.CanPostReadyOrdersForContender(contender))
		{
			return;
		}
		OrderReadyForNextRound order = new OrderReadyForNextRound(encounter.GUID, contender.GUID);
		base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order);
	}

	private void RemoveEncounter(Encounter encounter)
	{
		if (this.myEncounters.Remove(encounter))
		{
			encounter.ContenderCollectionChange -= this.Encounter_ContenderCollectionChange;
			encounter.EncounterStateChange -= this.Encounter_EncounterStateChange;
			encounter.RoundUpdate -= this.Encounter_RoundUpdate;
			encounter.TargetingPhaseUpdate -= this.Encounter_TargetingPhaseUpdate;
		}
	}

	public FixedSizedList<DecisionMakerEvaluationData<AIEncounterStrategyDefinition, InterpreterContext>> DecisionMakerEvaluationDataHistoric;

	private IBattleEncounterRepositoryService battleEncounterRepositoryService;

	private IBattleZone battleZone;

	private List<DecisionResult> decisionResults;

	private IDatabase<AIEncounterStrategyDefinition> encounterStrategyDatabase;

	private IIntelligenceAIHelper intelligenceAIHelper;

	private List<AILayer_Encounter.AIEncounterBattleGroundAnalysis> lastEncounterAnalysis;

	private List<AIEncounterSpellAffinityDefinition> spellAffinityDefinitions;

	private Dictionary<StaticString, GridMap<float>> spellAffinityGridMaps;

	private SimulationDecisionMaker<AIEncounterStrategyDefinition> strategyDecisionMaker;

	private IUnitPatternAIHelper unitPatternHelper;

	private IWorldPositionningService worldPositionningService;

	[InfluencedByPersonality]
	private float fearfulness;

	[InfluencedByPersonality]
	private float balanceOfPowerDecreaseFactor;

	[InfluencedByPersonality]
	private float balanceOfPowerIncreaseFactor;

	private IDatabase<AIParameterConverter> aiParameterConverterDatabase;

	private IAIParameter<InterpreterContext>[] encounterStrategyAiParameters;

	private IEncounterRepositoryService encounterRepositoryService;

	private IWeatherService weatherService;

	private List<Encounter> myEncounters = new List<Encounter>();

	public class AIEncounterBattleGroundAnalysis
	{
		public AIEncounterBattleGroundAnalysis(GameEntityGUID contenderGUID)
		{
			this.ContenderGUID = contenderGUID;
			this.AllyUnitPlayingNextBattleRound = new List<AILayer_Encounter.AIEncounterUnitAnalysis>();
		}

		public List<AILayer_Encounter.AIEncounterUnitAnalysis> AllyUnitPlayingNextBattleRound { get; set; }

		public GameEntityGUID ContenderGUID { get; set; }
	}

	public class AIEncounterUnitAnalysis
	{
		public AIEncounterUnitAnalysis(GameEntityGUID unitGUID, StaticString unitPattern, StaticString unitPatternCategory)
		{
			this.UnitGUID = unitGUID;
			this.UnitPattern = unitPattern;
			this.UnitPatternCategory = unitPatternCategory;
		}

		public GameEntityGUID UnitGUID { get; set; }

		public StaticString UnitPattern { get; set; }

		public StaticString UnitPatternCategory { get; set; }
	}
}
