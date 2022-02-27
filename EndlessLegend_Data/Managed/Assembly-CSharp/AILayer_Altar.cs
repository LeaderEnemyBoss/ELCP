using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Amplitude;
using Amplitude.Collections;
using Amplitude.Unity.AI;
using Amplitude.Unity.AI.Decision;
using Amplitude.Unity.AI.Evaluation;
using Amplitude.Unity.AI.Evaluation.Diagnostics;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation.Advanced;
using UnityEngine;

[PersonalityRegistryPath("AI/MajorEmpire/AIEntity_Empire/AILayer_Altar", new object[]
{

})]
public class AILayer_Altar : AILayer, IAIEvaluationHelper<SeasonEffect, InterpreterContext>
{
	[SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1513:ClosingCurlyBracketMustBeFollowedByBlankLine", Justification = "For AI heuristic scope we want to have EndScope call just after the closing bracket.")]
	private float DecisionParameterContextModifier(SeasonEffect aievaluableelement, InterpreterContext context, StaticString outputName, AIHeuristicAnalyser.Context debugContext)
	{
		float num = 1f;
		float num2 = 0f;
		num2 = this.ComputeAttitudeBoostValue(outputName, debugContext, num2);
		num = AILayer.Boost(num, num2);
		float num3 = 1f;
		string regitryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_Altar/ElementEvaluatorContextMultiplier/" + outputName;
		num3 = this.personalityHelper.GetRegistryValue<float>(base.AIEntity.Empire, regitryPath, num3);
		return num * num3;
	}

	private float ComputeAttitudeBoostValue(StaticString outputName, AIHeuristicAnalyser.Context debugContext, float attitudeBoost)
	{
		if (outputName == "AIEmpirePillageDefense")
		{
			float attitudeValue = this.GetAttitudeValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.PillageSucceed, debugContext);
			float attitudeValue2 = this.GetAttitudeValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.Pillaging, debugContext);
			attitudeBoost = Mathf.Max(attitudeValue, attitudeValue2);
		}
		else if (outputName == "AIEmpireAntiSpy")
		{
			attitudeBoost = this.GetAttitudeValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.Spy, debugContext);
		}
		return attitudeBoost;
	}

	private float GetAttitudeValue(StaticString modifierName, AIHeuristicAnalyser.Context debugContext)
	{
		float num = 0f;
		IGameService service = Services.GetService<IGameService>();
		global::Game game = service.Game as global::Game;
		for (int i = 0; i < game.Empires.Length; i++)
		{
			MajorEmpire majorEmpire = game.Empires[i] as MajorEmpire;
			if (majorEmpire != null && majorEmpire.Index != base.AIEntity.Empire.Index)
			{
				AILayer_Attitude.Attitude attitude = this.aiLayerAttitude.GetAttitude(majorEmpire);
				num = Mathf.Max(num, attitude.Score.GetNormalizedScoreByName(modifierName));
			}
		}
		return num;
	}

	public IEnumerable<IAIParameter<InterpreterContext>> GetAIParameters(SeasonEffect element)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		Diagnostics.Assert(this.aiParametersByElement != null);
		IAIParameter<InterpreterContext>[] aiParameters;
		if (this.aiParametersByElement.TryGetValue(element.SeasonEffectDefinition.Name, out aiParameters))
		{
			for (int index = 0; index < aiParameters.Length; index++)
			{
				yield return aiParameters[index];
			}
		}
		yield break;
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

	public IEnumerable<IAIPrerequisite<InterpreterContext>> GetAIPrerequisites(SeasonEffect element)
	{
		yield break;
	}

	private void InitializeAIEvaluableElement(SeasonEffectDefinition seasonEffectDefinition)
	{
		if (seasonEffectDefinition == null)
		{
			throw new ArgumentNullException("seasonEffect");
		}
		Diagnostics.Assert(seasonEffectDefinition.Name != null);
		Diagnostics.Assert(this.aiParametersByElement != null);
		if (this.aiParametersByElement.ContainsKey(seasonEffectDefinition.Name))
		{
			return;
		}
		List<IAIParameter<InterpreterContext>> list = new List<IAIParameter<InterpreterContext>>();
		if (this.aiParameterDatabase != null && this.aiParameterDatabase.ContainsKey(seasonEffectDefinition.Name))
		{
			AIParameterDatatableElement value = this.aiParameterDatabase.GetValue(seasonEffectDefinition.Name);
			if (value == null)
			{
				Diagnostics.LogWarning("Cannot retrieve ai parameters for constructible element '{0}'", new object[]
				{
					seasonEffectDefinition.Name
				});
				return;
			}
			if (value.AIParameters == null)
			{
				Diagnostics.LogWarning("There aren't any parameters in aiParameters '{0}'", new object[]
				{
					seasonEffectDefinition.Name
				});
				return;
			}
			for (int i = 0; i < value.AIParameters.Length; i++)
			{
				AIParameterDatatableElement.AIParameter aiparameter = value.AIParameters[i];
				Diagnostics.Assert(aiparameter != null);
				list.Add(aiparameter.Instantiate());
			}
		}
		if (this.orbUnlockDefinitions == null)
		{
			this.aiParametersByElement.Add(seasonEffectDefinition.Name, list.ToArray());
			IEnumerable<DepartmentOfScience.ConstructibleElement> database = Databases.GetDatabase<DepartmentOfScience.ConstructibleElement>(false);
			List<TechnologyDefinition> list2 = new List<TechnologyDefinition>();
			foreach (DepartmentOfScience.ConstructibleElement constructibleElement in database)
			{
				TechnologyDefinition technologyDefinition = constructibleElement as TechnologyDefinition;
				if (technologyDefinition != null && technologyDefinition.Visibility == TechnologyDefinitionVisibility.Visible && (technologyDefinition.TechnologyFlags & DepartmentOfScience.ConstructibleElement.TechnologyFlag.OrbUnlock) == DepartmentOfScience.ConstructibleElement.TechnologyFlag.OrbUnlock)
				{
					list2.Add(technologyDefinition);
				}
			}
			this.orbUnlockDefinitions = list2.ToArray();
		}
	}

	[UtilityFunction("AIEmpireGrowth")]
	private static float UtilityFunc_EmpireGrowth(SeasonEffect aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		float num = Mathf.Max(1f, AILayer_Altar.GetCityPropertySumValue(empire, SimulationProperties.NetCityGrowth));
		float utility = aiParameterValue / num;
		return AILayer_Altar.Normalize(debugContext, 0f, 1.5f, utility);
	}

	[UtilityFunction("AIEmpireProduction")]
	private static float UtilityFunc_EmpireProduction(SeasonEffect aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		float num = Mathf.Max(1f, AILayer_Altar.GetCityPropertySumValue(empire, SimulationProperties.NetCityProduction));
		float utility = aiParameterValue / num;
		return AILayer_Altar.Normalize(debugContext, 0f, 0.7f, utility);
	}

	[UtilityFunction("AIEmpireMoney")]
	private static float UtilityFunc_EmpireMoney(SeasonEffect aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		float num = Mathf.Max(1f, empire.GetPropertyValue(SimulationProperties.NetEmpireMoney));
		float utility = aiParameterValue / num;
		return AILayer_Altar.Normalize(debugContext, 0f, 2f, utility);
	}

	[UtilityFunction("AIEmpireResearch")]
	private static float UtilityFunc_EmpireResearch(SeasonEffect aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		if (empire.SimulationObject.Tags.Contains("AffinityReplicants"))
		{
			return 0f;
		}
		float num = Mathf.Max(1f, empire.GetPropertyValue(SimulationProperties.NetEmpireResearch));
		float utility = aiParameterValue / num;
		return AILayer_Altar.Normalize(debugContext, 0f, 1.7f, utility);
	}

	[UtilityFunction("AIEmpireEmpirePoint")]
	private static float UtilityFunc_EmpirePoint(SeasonEffect aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		float num = Mathf.Max(1f, empire.GetPropertyValue(SimulationProperties.NetEmpirePoint));
		float utility = aiParameterValue / num;
		return AILayer_Altar.Normalize(debugContext, 0.5f, 4f, utility);
	}

	[UtilityFunction("AIEmpireStrategicResource")]
	private static float UtilityFunc_EmpireStrategic(SeasonEffect aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		DepartmentOfScience agency = empire.GetAgency<DepartmentOfScience>();
		DepartmentOfTheInterior agency2 = empire.GetAgency<DepartmentOfTheInterior>();
		float num;
		float num2;
		AILayer_Altar.GetResourceUnitMarketPrice(empire, out num, out num2);
		float b = 0f;
		if (agency.GetTechnologyState(TechnologyDefinition.Names.MarketplaceResources) == DepartmentOfScience.ConstructibleElement.State.Researched)
		{
			float propertyValue = empire.GetPropertyValue(SimulationProperties.NetEmpireMoney);
			b = num / Mathf.Max(propertyValue, 1f);
		}
		float technologyUnlockedCount = agency.GetTechnologyUnlockedCount();
		float a = (float)agency2.Cities.Count;
		float a2 = technologyUnlockedCount / (15f * Mathf.Max(a, 1f));
		float num3 = Mathf.Max(a2, b);
		return aiParameterValue * num3;
	}

	[UtilityFunction("AIEmpireLuxuryResource")]
	private static float UtilityFunc_EmpireLuxury(SeasonEffect aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		DepartmentOfScience agency = empire.GetAgency<DepartmentOfScience>();
		DepartmentOfTheInterior agency2 = empire.GetAgency<DepartmentOfTheInterior>();
		float num;
		float num2;
		AILayer_Altar.GetResourceUnitMarketPrice(empire, out num, out num2);
		float b = 0f;
		if (agency.GetTechnologyState(TechnologyDefinition.Names.MarketplaceResources) == DepartmentOfScience.ConstructibleElement.State.Researched)
		{
			float propertyValue = empire.GetPropertyValue(SimulationProperties.NetEmpireMoney);
			b = num2 / Mathf.Max(propertyValue, 1f);
		}
		float technologyUnlockedCount = agency.GetTechnologyUnlockedCount();
		float a = (float)agency2.Cities.Count;
		float a2 = technologyUnlockedCount / (15f * Mathf.Max(a, 1f));
		float num3 = Mathf.Max(a2, b);
		return aiParameterValue * num3;
	}

	[UtilityFunction("AIEmpireApproval")]
	private static float UtilityFunc_EmpireApproval(SeasonEffect aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		float propertyValue = empire.GetPropertyValue(SimulationProperties.EmpireApproval);
		return aiParameterValue / Mathf.Max(1f, propertyValue);
	}

	[UtilityFunction("AIEmpireMilitaryPower")]
	private static float UtilityFunc_EmpireMilitaryPower(SeasonEffect aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		float num = Mathf.Max(1f, empire.GetPropertyValue(SimulationProperties.MilitaryPower));
		return aiParameterValue / num;
	}

	[UtilityFunction("AIEmpireAntiSpy")]
	private static float UtilityFunc_EmpireAntiSpy(SeasonEffect aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		DepartmentOfTheInterior agency = empire.GetAgency<DepartmentOfTheInterior>();
		Diagnostics.Assert(agency != null);
		float num = Mathf.Max(1f, AILayer_Altar.GetCityPropertySumValue(empire, SimulationProperties.CityAntiSpy)) / (float)agency.Cities.Count;
		if (float.IsNaN(num))
		{
			return 0f;
		}
		float utility = aiParameterValue / Mathf.Max(1f, num);
		return AILayer_Altar.Normalize(debugContext, 0f, 3f, utility);
	}

	[UtilityFunction("AIEmpireCityDefense")]
	private static float UtilityFunc_EmpireCityDefense(SeasonEffect aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		DepartmentOfTheInterior agency = empire.GetAgency<DepartmentOfTheInterior>();
		Diagnostics.Assert(agency != null);
		float num = Mathf.Max(1f, AILayer_Altar.GetCityPropertySumValue(empire, SimulationProperties.CityDefensePoint)) / (float)agency.Cities.Count;
		if (float.IsNaN(num))
		{
			return 0f;
		}
		float utility = aiParameterValue / num;
		return AILayer_Altar.Normalize(debugContext, 0f, 3f, utility);
	}

	[UtilityFunction("AIEmpirePillageDefense")]
	private static float UtilityFunc_EmpirePillageDefense(SeasonEffect aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		DepartmentOfTheInterior agency = empire.GetAgency<DepartmentOfTheInterior>();
		Diagnostics.Assert(agency != null);
		int num = 0;
		float num2 = 0f;
		int count = agency.Cities.Count;
		for (int i = 0; i < count; i++)
		{
			for (int j = 0; j < agency.Cities[i].Region.PointOfInterests.Length; j++)
			{
				float propertyValue = agency.Cities[i].Region.PointOfInterests[j].GetPropertyValue(SimulationProperties.MaximumPillageDefense);
				num2 += propertyValue;
				num++;
			}
		}
		float b = num2 / (float)num;
		return aiParameterValue / Mathf.Max(1f, b);
	}

	[UtilityFunction("AIWorldNavy")]
	private static float UtilityFunc_WorldNavy(SeasonEffect aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		if (!service.IsShared(DownloadableContent16.ReadOnlyName))
		{
			return 0f;
		}
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		float num = AILayer_Altar.ComputeMyNavyImportance(empire);
		return aiParameterValue * (num - 1f);
	}

	[UtilityFunction("AIWorldSpy")]
	private static float UtilityFunc_WorldSpy(SeasonEffect aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		float num = 0f;
		DepartmentOfEducation agency = empire.GetAgency<DepartmentOfEducation>();
		for (int i = 0; i < agency.Heroes.Count; i++)
		{
			num += agency.Heroes[i].GetPropertyValue("NetInfiltrationPoint");
		}
		float num2 = num * 0.05f;
		return aiParameterValue * num2;
	}

	[UtilityFunction("AIWorldWar")]
	private static float UtilityFunc_WorldWar(SeasonEffect aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		global::Game game = service.Game as global::Game;
		Diagnostics.Assert(game != null);
		float propertyValue = empire.GetPropertyValue(SimulationProperties.MilitaryPower);
		float num = 0f;
		for (int i = 0; i < game.Empires.Length; i++)
		{
			num += game.Empires[i].GetPropertyValue(SimulationProperties.MilitaryPower);
		}
		num /= (float)game.Empires.Length;
		float num2 = propertyValue / Mathf.Max(100f, num) - 1f;
		return aiParameterValue * num2;
	}

	[UtilityFunction("AIWorldPeace")]
	private static float UtilityFunc_WorldPeace(SeasonEffect aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		float propertyValue = empire.GetPropertyValue(SimulationProperties.PeaceCount);
		float propertyValue2 = empire.GetPropertyValue(SimulationProperties.AllianceCount);
		float num = (2f * propertyValue2 + propertyValue) / 7f;
		return aiParameterValue * num;
	}

	[UtilityFunction("AIWorldStrategicMarketPrice")]
	private static float UtilityFunc_WorldStrategicMarketPrice(SeasonEffect aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		return aiParameterValue;
	}

	[UtilityFunction("AIWorldLuxuryMarketPrice")]
	private static float UtilityFunc_WorldLuxuryMarketPrice(SeasonEffect aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		return aiParameterValue;
	}

	[UtilityFunction("AIWorldMercenaryMarketPrice")]
	private static float UtilityFunc_WorldMercenaryMarketPrice(SeasonEffect aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		return aiParameterValue;
	}

	private static float Normalize(AIHeuristicAnalyser.Context debugContext, float minimumUtilityValue, float maximumUtilityValue, float utility)
	{
		Diagnostics.Assert(maximumUtilityValue > minimumUtilityValue);
		utility = Mathf.Sign(utility) * Mathf.Clamp01((Mathf.Abs(utility) - minimumUtilityValue) / (maximumUtilityValue - minimumUtilityValue));
		return utility;
	}

	private static float GetCityPropertySumValue(global::Empire empire, StaticString resourcePropertyName)
	{
		DepartmentOfTheInterior agency = empire.GetAgency<DepartmentOfTheInterior>();
		float num = 0f;
		for (int i = 0; i < agency.Cities.Count; i++)
		{
			num += agency.Cities[i].GetPropertyValue(resourcePropertyName);
		}
		return num;
	}

	private static void GetResourceUnitMarketPrice(global::Empire empire, out float averageStrategicUnitPrice, out float averageLuxuryUnitPrice)
	{
		DepartmentOfTheTreasury agency = empire.GetAgency<DepartmentOfTheTreasury>();
		float num = 0f;
		int num2 = 0;
		float num3 = 0f;
		int num4 = 0;
		IDatabase<ResourceDefinition> database = Databases.GetDatabase<ResourceDefinition>(false);
		foreach (ResourceDefinition resourceDefinition in database)
		{
			if (resourceDefinition.ResourceType == ResourceDefinition.Type.Strategic || resourceDefinition.ResourceType == ResourceDefinition.Type.Luxury)
			{
				float num5;
				agency.TryGetResourceStockValue(empire.SimulationObject, resourceDefinition.Name, out num5, true);
				float priceWithSalesTaxes = TradableResource.GetPriceWithSalesTaxes(resourceDefinition.Name, TradableTransactionType.Sellout, empire, 1f);
				if (resourceDefinition.ResourceType == ResourceDefinition.Type.Strategic)
				{
					num += priceWithSalesTaxes;
					num2++;
				}
				else if (resourceDefinition.ResourceType == ResourceDefinition.Type.Luxury)
				{
					num3 += priceWithSalesTaxes;
					num4++;
				}
			}
		}
		averageStrategicUnitPrice = ((num2 <= 0) ? 0f : (num / (float)num2));
		averageLuxuryUnitPrice = ((num4 <= 0) ? 0f : (num3 / (float)num4));
	}

	private static float ComputeMyNavyImportance(global::Empire empire)
	{
		DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		global::Game game = service.Game as global::Game;
		Diagnostics.Assert(game != null);
		IAIEmpireDataAIHelper service2 = AIScheduler.Services.GetService<IAIEmpireDataAIHelper>();
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < game.Empires.Length; i++)
		{
			AIEmpireData aiempireData;
			if (service2.TryGet(i, out aiempireData))
			{
				float navalMilitaryPower = aiempireData.NavalMilitaryPower;
				if (game.Empires[i] == empire)
				{
					num += navalMilitaryPower;
				}
				else if (agency.IsFriend(game.Empires[i]))
				{
					num += navalMilitaryPower;
				}
				else
				{
					num2 += navalMilitaryPower;
				}
			}
		}
		float result = 2f;
		if (num2 != 0f)
		{
			result = num / num2;
		}
		return result;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		IGameService gameService = Services.GetService<IGameService>();
		this.seasonService = new WeakReference<ISeasonService>(gameService.Game.Services.GetService<ISeasonService>());
		this.aiParameterDatabase = Databases.GetDatabase<AIParameterDatatableElement>(false);
		this.aiParameterConverterDatabase = Databases.GetDatabase<AIParameterConverter>(false);
		this.aiLayerAttitude = base.AIEntity.GetLayer<AILayer_Attitude>();
		Diagnostics.Assert(this.aiLayerAttitude != null);
		this.personalityHelper = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		Diagnostics.Assert(this.personalityHelper != null);
		IDatabase<SeasonEffectDefinition> seasonEffectDefinitions = Databases.GetDatabase<SeasonEffectDefinition>(false);
		foreach (SeasonEffectDefinition seasonEffectDefinition in seasonEffectDefinitions)
		{
			this.InitializeAIEvaluableElement(seasonEffectDefinition);
		}
		InterpreterContext interpreterContext = new InterpreterContext(base.AIEntity.Empire.SimulationObject);
		interpreterContext.Register("Empire", base.AIEntity.Empire);
		this.elementEvaluator = new ElementEvaluator<SeasonEffect, InterpreterContext>(this, interpreterContext);
		this.elementEvaluator.ContextWeightDelegate = new ElementEvaluator<SeasonEffect, InterpreterContext>.ContextWeightFunc(this.DecisionParameterContextModifier);
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_Altar_CreateLocalNeedsPass", new AIEntity.AIAction(this.CreateLocalNeeds), this, new StaticString[]
		{
			"LayerAmasEmpire_CreateLocalNeedsPass"
		});
		base.AIEntity.RegisterPass(AIEntity.Passes.EvaluateNeeds.ToString(), "AILayer_Altar_EvaluateNeedsPass", new AIEntity.AIAction(this.EvaluateNeeds), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.ExecuteNeeds.ToString(), "AILayer_Altar_ExecuteNeedsPass", new AIEntity.AIAction(this.ExecuteNeeds), this, new StaticString[0]);
		InfluencedByPersonalityAttribute.LoadFieldAndPropertyValues(base.AIEntity.Empire, this);
		yield break;
	}

	public override void Release()
	{
		base.Release();
		this.seasonService = null;
		this.aiParameterDatabase = null;
		this.aiParameterConverterDatabase = null;
		this.aiLayerAttitude = null;
		this.personalityHelper = null;
		if (this.elementEvaluator != null)
		{
			this.elementEvaluator.Context.Clear();
			this.elementEvaluator.ContextWeightDelegate = null;
			this.elementEvaluator = null;
		}
		this.aiParametersByElement.Clear();
	}

	protected override void CreateLocalNeeds(StaticString context, StaticString pass)
	{
		base.CreateLocalNeeds(context, pass);
		base.AIEntity.Context.InitializeElementEvaluator<SeasonEffect, InterpreterContext>(AILayer_Altar.contextGroupName, typeof(AILayer_Altar), this.elementEvaluator);
	}

	protected override void EvaluateNeeds(StaticString context, StaticString pass)
	{
		base.EvaluateNeeds(context, pass);
		if (!this.CanUseAltar())
		{
			return;
		}
		if (!this.seasonService.IsAlive || this.seasonService.Target == null)
		{
			return;
		}
		Season winterSeason = this.seasonService.Target.GetWinterSeason();
		if (this.seasonService.Target.IsCurrentSeason(winterSeason))
		{
			return;
		}
		global::Game game = Services.GetService<IGameService>().Game as global::Game;
		List<SeasonEffect> candidateEffectsForSeasonType = this.seasonService.Target.GetCandidateEffectsForSeasonType(Season.ReadOnlyWinter);
		Diagnostics.Assert(this.elementEvaluator != null);
		this.decisions.Clear();
		this.RegisterInterpreterContextData();
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			EvaluationData<SeasonEffect, InterpreterContext> evaluationData = new EvaluationData<SeasonEffect, InterpreterContext>();
			this.elementEvaluator.Evaluate(candidateEffectsForSeasonType, ref this.decisions, evaluationData);
			evaluationData.Turn = game.Turn;
			this.SeasonEffectsEvaluationDataHistoric.Add(evaluationData);
		}
		else
		{
			this.elementEvaluator.Evaluate(candidateEffectsForSeasonType, ref this.decisions, null);
		}
		if (this.decisions.Count <= 0)
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < candidateEffectsForSeasonType.Count; i++)
		{
			SeasonEffect seasonEffect = candidateEffectsForSeasonType[i];
			int seasonEffectDisplayedScoreForEmpire = this.seasonService.Target.GetSeasonEffectDisplayedScoreForEmpire(base.AIEntity.Empire, seasonEffect);
			if (seasonEffectDisplayedScoreForEmpire > num)
			{
				num = seasonEffectDisplayedScoreForEmpire;
				num2 = 1;
			}
			else if (seasonEffectDisplayedScoreForEmpire == num)
			{
				num2++;
			}
		}
		DecisionResult decisionResult = this.decisions[0];
		float score = decisionResult.Score;
		float score2 = this.decisions[this.decisions.Count - 1].Score;
		float num3 = score - score2;
		num3 = Mathf.Clamp01(num3 / this.maximumGainFromXml);
		SeasonEffect seasonEffect2 = decisionResult.Element as SeasonEffect;
		Diagnostics.Assert(seasonEffect2 != null);
		int seasonEffectScore = this.seasonService.Target.GetSeasonEffectScore(seasonEffect2);
		if (seasonEffectScore == num && num2 == 1)
		{
			return;
		}
		int neededVoteCount = num - seasonEffectScore + 1;
		List<EvaluableMessage_VoteForSeasonEffect> list = new List<EvaluableMessage_VoteForSeasonEffect>(base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_VoteForSeasonEffect>(BlackboardLayerID.Empire, (EvaluableMessage_VoteForSeasonEffect message) => message.EvaluationState != EvaluableMessage.EvaluableMessageState.Obtained && message.EvaluationState != EvaluableMessage.EvaluableMessageState.Cancel));
		EvaluableMessage_VoteForSeasonEffect evaluableMessage_VoteForSeasonEffect;
		if (list.Count == 0)
		{
			evaluableMessage_VoteForSeasonEffect = new EvaluableMessage_VoteForSeasonEffect(seasonEffect2.SeasonEffectDefinition.Name, 1, AILayer_AccountManager.OrbAccountName);
			base.AIEntity.AIPlayer.Blackboard.AddMessage(evaluableMessage_VoteForSeasonEffect);
		}
		else
		{
			evaluableMessage_VoteForSeasonEffect = list[0];
			if (seasonEffect2.SeasonEffectDefinition.Name != evaluableMessage_VoteForSeasonEffect.SeasonEffectReference)
			{
				Diagnostics.Log("AI don't want to vote for the season effect {0} anymore. AI now wants season effect {1}.", new object[]
				{
					evaluableMessage_VoteForSeasonEffect.SeasonEffectReference,
					seasonEffect2.SeasonEffectDefinition.Name
				});
				evaluableMessage_VoteForSeasonEffect.Cancel();
				evaluableMessage_VoteForSeasonEffect = new EvaluableMessage_VoteForSeasonEffect(seasonEffect2.SeasonEffectDefinition.Name, 1, AILayer_AccountManager.OrbAccountName);
				base.AIEntity.AIPlayer.Blackboard.AddMessage(evaluableMessage_VoteForSeasonEffect);
			}
		}
		float num4 = this.seasonService.Target.ComputePrayerOrbCost(base.AIEntity.Empire);
		float num5 = 10f * base.AIEntity.Empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier);
		int num6 = this.seasonService.Target.GetExactSeasonStartTurn(winterSeason) - game.Turn;
		float num7 = Mathf.Clamp01((num5 - (float)num6) / num5);
		float num8 = num3 * (0.5f + num7 / 2f);
		num8 = Mathf.Clamp01(num8);
		int num9 = this.ComputeWantedNumberOfOrbs(neededVoteCount);
		DepartmentOfScience agency = base.AIEntity.Empire.GetAgency<DepartmentOfScience>();
		float num10 = 0f;
		float num11 = 0f;
		for (int j = 0; j < this.orbUnlockDefinitions.Length; j++)
		{
			TechnologyDefinition technologyDefinition = this.orbUnlockDefinitions[j];
			if (DepartmentOfTheTreasury.CheckConstructiblePrerequisites(base.AIEntity.Empire, technologyDefinition, new string[]
			{
				ConstructionFlags.Prerequisite
			}))
			{
				DepartmentOfScience.ConstructibleElement.State technologyState = agency.GetTechnologyState(technologyDefinition);
				if (technologyState != DepartmentOfScience.ConstructibleElement.State.Researched && technologyState != DepartmentOfScience.ConstructibleElement.State.NotAvailable)
				{
					num11 += 1f;
				}
				else if (technologyState == DepartmentOfScience.ConstructibleElement.State.Researched)
				{
					num10 += 1f;
				}
			}
		}
		float num12 = num11 / (num11 + num10);
		num12 = Mathf.Min(1f, Mathf.Max(0f, 2f * num12 - 0.6f));
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.Log("[ELCP: AILayer_Altar] {0} has reasearched {1} of {2} Orb Techs", new object[]
			{
				base.AIEntity.Empire,
				num10,
				num11 + num10
			});
			Diagnostics.Log("... Season Effect Voting score altered from {0} to {1}", new object[]
			{
				num8,
				num8 * num12
			});
		}
		evaluableMessage_VoteForSeasonEffect.VoteCount = num9;
		evaluableMessage_VoteForSeasonEffect.Refresh(0.5f, num8 * num12, (float)num9 * num4, int.MaxValue);
	}

	protected override void ExecuteNeeds(StaticString context, StaticString pass)
	{
		base.ExecuteNeeds(context, pass);
		int num = base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_VoteForSeasonEffect>(BlackboardLayerID.Empire, (EvaluableMessage_VoteForSeasonEffect match) => match.State == BlackboardMessage.StateValue.Message_InProgress).Count<EvaluableMessage_VoteForSeasonEffect>();
		if (num > 0)
		{
			ISynchronousJobRepositoryAIHelper service = AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>();
			service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_VoteForSeasonEffect));
		}
	}

	private int ComputeWantedNumberOfOrbs(int neededVoteCount)
	{
		float num = this.seasonService.Target.ComputePrayerOrbCost(base.AIEntity.Empire);
		if (num <= 1.401298E-45f)
		{
			return neededVoteCount;
		}
		float num2 = 0f;
		DepartmentOfTheTreasury agency = base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>();
		if (!agency.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.Orb, out num2, true))
		{
			return 1;
		}
		return Mathf.Max(neededVoteCount, Mathf.FloorToInt(0.1f * num2 / num));
	}

	private void RegisterInterpreterContextData()
	{
		float num = 1f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		float num5 = 0f;
		float num6 = 0f;
		float num7 = 0f;
		float num8 = 0f;
		float num9 = 0f;
		try
		{
			DepartmentOfDefense agency = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
			num = (float)agency.UnitDesignDatabase.AvailableUnitBodyDefinitions.Count;
			foreach (UnitBodyDefinition unitBodyDefinition in agency.UnitDesignDatabase.AvailableUnitBodyDefinitions)
			{
				if (!unitBodyDefinition.Tags.Contains("Hidden"))
				{
					if (unitBodyDefinition.Tags.Contains("Colossus"))
					{
						num7 += 1f;
					}
					else
					{
						if (unitBodyDefinition.Tags.Contains("UnitFactionTypeMinorFaction"))
						{
							num8 += 1f;
						}
						else if (!unitBodyDefinition.Tags.Contains("UnitTypeGuardianKiller"))
						{
							num9 += 1f;
						}
						if (unitBodyDefinition.SubCategory == "SubCategoryInfantry")
						{
							num2 += 1f;
						}
						else if (unitBodyDefinition.SubCategory == "SubCategoryCavalry")
						{
							num3 += 1f;
						}
						else if (unitBodyDefinition.SubCategory == "SubCategoryRanged")
						{
							num4 += 1f;
						}
						else if (unitBodyDefinition.SubCategory == "SubCategorySupport")
						{
							num5 += 1f;
						}
						else if (unitBodyDefinition.SubCategory == "SubCategoryFlying")
						{
							num6 += 1f;
						}
					}
				}
			}
		}
		catch (KeyNotFoundException)
		{
		}
		this.elementEvaluator.Context.Register("NumberOfAvailableUnitBodies", num);
		this.elementEvaluator.Context.Register("NumberOfAvailableInfantryBodies", num2);
		this.elementEvaluator.Context.Register("NumberOfAvailableCavalryBodies", num3);
		this.elementEvaluator.Context.Register("NumberOfAvailableRangedBodies", num4);
		this.elementEvaluator.Context.Register("NumberOfAvailableSupportBodies", num5);
		this.elementEvaluator.Context.Register("NumberOfAvailableFlyingBodies", num6);
		this.elementEvaluator.Context.Register("NumberOfAvailableGuardianBodies", num7);
		this.elementEvaluator.Context.Register("NumberOfAvailableMinorFactionBodies", num8);
		this.elementEvaluator.Context.Register("NumberOfAvailableMajorFactionBodies", num9);
	}

	private bool CanUseAltar()
	{
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		if (service == null || !service.IsShared(DownloadableContent13.ReadOnlyName))
		{
			return false;
		}
		DepartmentOfTheInterior agency = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		return agency.IsAltarBuilt();
	}

	private SynchronousJobState SynchronousJob_VoteForSeasonEffect()
	{
		List<EvaluableMessage_VoteForSeasonEffect> list = new List<EvaluableMessage_VoteForSeasonEffect>(base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_VoteForSeasonEffect>(BlackboardLayerID.Empire, (EvaluableMessage_VoteForSeasonEffect match) => match.State == BlackboardMessage.StateValue.Message_InProgress));
		if (list.Count != 0)
		{
			if (list.Count > 1)
			{
				AILayer.LogWarning("There should not be several VoteForSeasonEffect EvaluableMessages for the same empire ({0})", new object[]
				{
					base.AIEntity.Empire.Index
				});
			}
			EvaluableMessage_VoteForSeasonEffect evaluableMessage_VoteForSeasonEffect = list[0];
			if (evaluableMessage_VoteForSeasonEffect.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate)
			{
				OrderVoteForSeasonEffect order = new OrderVoteForSeasonEffect(base.AIEntity.Empire.Index, evaluableMessage_VoteForSeasonEffect.SeasonEffectReference, evaluableMessage_VoteForSeasonEffect.VoteCount);
				Ticket ticket;
				base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.VoteForSeasonEffect_TicketRaised));
				return SynchronousJobState.Success;
			}
		}
		return SynchronousJobState.Success;
	}

	private void VoteForSeasonEffect_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		OrderVoteForSeasonEffect orderVoteForSeasonEffect = e.Order as OrderVoteForSeasonEffect;
		Diagnostics.Assert(orderVoteForSeasonEffect != null);
		List<EvaluableMessage_VoteForSeasonEffect> list = new List<EvaluableMessage_VoteForSeasonEffect>(base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_VoteForSeasonEffect>(BlackboardLayerID.Empire, (EvaluableMessage_VoteForSeasonEffect message) => message.SeasonEffectReference == orderVoteForSeasonEffect.SeasonEffectName));
		if (list.Count == 0)
		{
			return;
		}
		if (list.Count > 1)
		{
			AILayer.LogWarning("There should not be several PopulationBuyout EvaluableMessages for the same city");
		}
		EvaluableMessage_VoteForSeasonEffect evaluableMessage_VoteForSeasonEffect = list[0];
		if (e.Result == PostOrderResponse.Processed)
		{
			evaluableMessage_VoteForSeasonEffect.SetObtained();
		}
		else
		{
			evaluableMessage_VoteForSeasonEffect.SetFailedToObtain();
		}
	}

	public const string RegistryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_Altar";

	private readonly Dictionary<StaticString, IAIParameter<InterpreterContext>[]> aiParametersByElement = new Dictionary<StaticString, IAIParameter<InterpreterContext>[]>();

	private IDatabase<AIParameterConverter> aiParameterConverterDatabase;

	private IDatabase<AIParameterDatatableElement> aiParameterDatabase;

	public FixedSizedList<EvaluationData<SeasonEffect, InterpreterContext>> SeasonEffectsEvaluationDataHistoric = new FixedSizedList<EvaluationData<SeasonEffect, InterpreterContext>>(global::Application.FantasyPreferences.AIDebugHistoricSize);

	private static StaticString contextGroupName = "SeasonEffects";

	private WeakReference<ISeasonService> seasonService;

	private ElementEvaluator<SeasonEffect, InterpreterContext> elementEvaluator;

	private List<DecisionResult> decisions = new List<DecisionResult>();

	private AILayer_Attitude aiLayerAttitude;

	private IPersonalityAIHelper personalityHelper;

	[InfluencedByPersonality]
	private float maximumGainFromXml = 20f;

	private TechnologyDefinition[] orbUnlockDefinitions;

	private static class OutputAIParameterNames
	{
		public const string EmpireGrowth = "AIEmpireGrowth";

		public const string EmpireProduction = "AIEmpireProduction";

		public const string EmpireMoney = "AIEmpireMoney";

		public const string EmpireResearch = "AIEmpireResearch";

		public const string EmpireEmpirePoint = "AIEmpireEmpirePoint";

		public const string EmpireStrategicResource = "AIEmpireStrategicResource";

		public const string EmpireLuxuryResource = "AIEmpireLuxuryResource";

		public const string EmpireApproval = "AIEmpireApproval";

		public const string EmpireMilitaryPower = "AIEmpireMilitaryPower";

		public const string EmpireAntiSpy = "AIEmpireAntiSpy";

		public const string EmpireCityDefense = "AIEmpireCityDefense";

		public const string EmpirePillageDefense = "AIEmpirePillageDefense";

		public const string WorldNavy = "AIWorldNavy";

		public const string WorldSpy = "AIWorldSpy";

		public const string WorldWar = "AIWorldWar";

		public const string WorldPeace = "AIWorldPeace";

		public const string WorldStrategicMarketPrice = "AIWorldStrategicMarketPrice";

		public const string WorldLuxuryMarketPrice = "AIWorldLuxuryMarketPrice";

		public const string WorldMercenaryMarketPrice = "AIWorldMercenaryMarketPrice";
	}
}
