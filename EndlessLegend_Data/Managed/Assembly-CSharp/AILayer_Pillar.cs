using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.AI;
using Amplitude.Unity.AI.Decision;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation.Advanced;

[Diagnostics.TagAttribute("AI")]
public class AILayer_Pillar : AILayer, ISimulationAIEvaluationHelper<PillarDefinition>, IAIEvaluationHelper<PillarDefinition, InterpreterContext>
{
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

	public IEnumerable<IAIParameter<InterpreterContext>> GetAIParameters(PillarDefinition pillarDefinition)
	{
		if (pillarDefinition == null)
		{
			throw new ArgumentNullException("pillarDefinition");
		}
		IEnumerable<WorldEffectDefinition> enumerator = this.pillarService.GetActiveWorldEffectDefinitions(pillarDefinition.Name, base.AIEntity.Empire);
		if (enumerator != null)
		{
			foreach (WorldEffectDefinition worldEffectDefinition in enumerator)
			{
				IAIParameter<InterpreterContext>[] worldEffectAiParameters;
				if (!this.pillarEffectsAiParameters.TryGetValue(worldEffectDefinition.Name, out worldEffectAiParameters))
				{
					IDatabase<AIParameterDatatableElement> aiParameterDatabase = Databases.GetDatabase<AIParameterDatatableElement>(false);
					Diagnostics.Assert(aiParameterDatabase != null);
					AIParameterDatatableElement aiParameterDatatableElement;
					if (aiParameterDatabase.TryGetValue(worldEffectDefinition.Name, out aiParameterDatatableElement))
					{
						if (aiParameterDatatableElement.AIParameters != null)
						{
							worldEffectAiParameters = new IAIParameter<InterpreterContext>[aiParameterDatatableElement.AIParameters.Length];
							for (int index = 0; index < aiParameterDatatableElement.AIParameters.Length; index++)
							{
								AIParameterDatatableElement.AIParameter parameterDefinition = aiParameterDatatableElement.AIParameters[index];
								Diagnostics.Assert(parameterDefinition != null);
								worldEffectAiParameters[index] = parameterDefinition.Instantiate();
							}
						}
					}
					else
					{
						AILayer.LogError("No AIParameters for WorldEffectDefinition {0}", new object[]
						{
							worldEffectDefinition.Name
						});
					}
				}
				if (worldEffectAiParameters != null)
				{
					for (int index2 = 0; index2 < worldEffectAiParameters.Length; index2++)
					{
						yield return worldEffectAiParameters[index2];
					}
				}
			}
		}
		yield break;
	}

	public IEnumerable<IAIPrerequisite<InterpreterContext>> GetAIPrerequisites(PillarDefinition pillarDefinition)
	{
		if (pillarDefinition == null)
		{
			throw new ArgumentNullException("pillarDefinition");
		}
		for (int index = 0; index < pillarDefinition.Prerequisites.Length; index++)
		{
			yield return pillarDefinition.Prerequisites[index];
		}
		yield break;
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		if (!DepartmentOfTheInterior.CanInvokePillarsAndSpells(base.AIEntity.Empire))
		{
			yield break;
		}
		this.aiEntityCity = (base.AIEntity as AIEntity_City);
		IGameService gameService = Services.GetService<IGameService>();
		this.pillarService = gameService.Game.Services.GetService<IPillarService>();
		this.worldPositioningService = gameService.Game.Services.GetService<IWorldPositionningService>();
		this.decisionMaker = new SimulationDecisionMaker<PillarDefinition>(this, this.aiEntityCity.City.Empire.SimulationObject);
		this.decisionMaker.ParameterContextModifierDelegate = new Func<PillarDefinition, StaticString, float>(this.DecisionParameterContextModifier);
		this.decisionMaker.ParameterContextNormalizationDelegate = new DecisionMaker<PillarDefinition, InterpreterContext>.GetNormalizationRangeDelegate(this.DecisionParameterContextNormalization);
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "LayerPillar_CreateLocalNeedsPass", new AIEntity.AIAction(this.CreateLocalNeeds), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.ExecuteNeeds.ToString(), "LayerPillar_ExecuteNeedsPass", new AIEntity.AIAction(this.ExecuteNeeds), this, new StaticString[0]);
		this.pillarEffectsAiParameters = new Dictionary<StaticString, IAIParameter<InterpreterContext>[]>();
		this.aiParameterConverterDatabase = Databases.GetDatabase<AIParameterConverter>(false);
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public override void Release()
	{
		base.Release();
		this.validatedPillarNeed = null;
		this.pillarService = null;
		this.worldPositioningService = null;
		this.decisionMaker = null;
		this.PillarDatabase = null;
		this.departmentOfForeignAffairs = null;
		this.departmentOfTheTreasury = null;
	}

	protected WorldPosition ComputeMostOverlappingPositionForRange(PillarDefinition pillarDefinition, ref float pillarScore)
	{
		int activeRange = this.pillarService.GetActiveRange(pillarDefinition.Name, base.AIEntity.Empire);
		District district = null;
		int num = 0;
		for (int i = 0; i < this.aiEntityCity.City.Districts.Count; i++)
		{
			District district2 = this.aiEntityCity.City.Districts[i];
			if (this.pillarService.IsPositionValidForPillar(base.AIEntity.Empire, district2.WorldPosition))
			{
				WorldArea worldArea = new WorldArea(new List<WorldPosition>
				{
					district2.WorldPosition
				});
				for (int j = 0; j < activeRange; j++)
				{
					worldArea = worldArea.Grow();
				}
				int num2 = 0;
				for (int k = 0; k < worldArea.WorldPositions.Count; k++)
				{
					if (this.worldPositioningService.GetDistrict(WorldPosition.GetValidPosition(worldArea.WorldPositions[k], this.worldPositioningService.World.WorldParameters)) != null)
					{
						num2++;
					}
				}
				if (num2 > num)
				{
					num = num2;
					district = district2;
				}
			}
		}
		if (district == null)
		{
			return WorldPosition.Invalid;
		}
		while (--num >= 0)
		{
			pillarScore = AILayer.Boost(pillarScore, 0.05f);
		}
		return district.WorldPosition;
	}

	protected override void CreateLocalNeeds(StaticString context, StaticString pass)
	{
		base.CreateLocalNeeds(context, pass);
		IEnumerable<EvaluableMessage_PillarNeed> messages = base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_PillarNeed>(BlackboardLayerID.Empire, (EvaluableMessage_PillarNeed match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending);
		if (messages.Any<EvaluableMessage_PillarNeed>())
		{
			foreach (EvaluableMessage_PillarNeed evaluableMessage_PillarNeed in messages)
			{
				evaluableMessage_PillarNeed.Cancel();
			}
		}
	}

	protected float DecisionParameterContextModifier(PillarDefinition aiEvaluableElement, StaticString aiParameterName)
	{
		return base.AIEntity.Context.GetModifierValue(AICityState.ProductionParameterModifier, aiParameterName);
	}

	protected void DecisionParameterContextNormalization(StaticString aiParameterName, out float minimumValue, out float maximumValue)
	{
		minimumValue = 0f;
		maximumValue = base.AIEntity.Context.GetMaximalValue(aiParameterName);
		if (maximumValue < 1f)
		{
			maximumValue = 1f;
		}
	}

	protected override void ExecuteNeeds(StaticString context, StaticString pass)
	{
		base.ExecuteNeeds(context, pass);
		if (!DepartmentOfTheInterior.CanInvokePillarsAndSpells(base.AIEntity.Empire))
		{
			return;
		}
		this.MoneyAtTurnBegin = 0f;
		if (base.AIEntity.Empire.GetPropertyValue("PillarsCount") < 1f)
		{
			this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpireMoney, out this.MoneyAtTurnBegin, false);
			AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>().RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_StartValidatedBoosters));
		}
	}

	private float GetPillarDustCost(PillarDefinition pillarDefinition)
	{
		ConstructionResourceStock[] array;
		this.departmentOfTheTreasury.GetInstantConstructionResourceCostForBuyout(base.AIEntity.Empire, pillarDefinition, out array);
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

	private void PillarOrder_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		OrderBuyoutAndActivatePillar order = e.Order as OrderBuyoutAndActivatePillar;
		IEnumerable<EvaluableMessage_PillarNeed> messages = base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_PillarNeed>(BlackboardLayerID.Empire, (EvaluableMessage_PillarNeed match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate && match.WorldPosition == order.TargetPosition && match.PillarDefinition.Name == order.PillarDefinitionName);
		if (messages.Any<EvaluableMessage_PillarNeed>())
		{
			using (IEnumerator<EvaluableMessage_PillarNeed> enumerator = messages.GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					EvaluableMessage_PillarNeed evaluableMessage_PillarNeed = enumerator.Current;
					if (e.Result != PostOrderResponse.Processed)
					{
						evaluableMessage_PillarNeed.SetFailedToObtain();
					}
					else
					{
						evaluableMessage_PillarNeed.SetObtained();
					}
				}
			}
		}
	}

	private SynchronousJobState SynchronousJob_StartValidatedBoosters()
	{
		if (this.aiEntityCity.City == null || !this.aiEntityCity.City.GUID.IsValid || this.aiEntityCity.City.Empire.Index != base.AIEntity.Empire.Index || this.aiEntityCity.City.BesiegingEmpireIndex >= 0)
		{
			return SynchronousJobState.Success;
		}
		float num = 0f;
		this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpireMoney, out num, false);
		if (num <= this.MoneyAtTurnBegin * (this.departmentOfForeignAffairs.IsInWarWithSomeone() ? 0.5f : 0.25f))
		{
			return SynchronousJobState.Success;
		}
		this.decisionMaker.UnregisterAllOutput();
		this.decisionMaker.ClearAIParametersOverrides();
		base.AIEntity.Context.InitializeDecisionMaker<PillarDefinition>(AICityState.ProductionParameterModifier, this.decisionMaker);
		List<PillarDefinition> elements = (from pillarDefinition in this.PillarDatabase.GetValues()
		where DepartmentOfTheTreasury.CheckConstructiblePrerequisites(base.AIEntity.Empire, pillarDefinition, new string[]
		{
			ConstructionFlags.Prerequisite
		})
		select pillarDefinition).ToList<PillarDefinition>();
		this.decisionResults.Clear();
		this.decisionMaker.EvaluateDecisions(elements, ref this.decisionResults);
		if (this.decisionResults != null && this.decisionResults.Count > 0 && this.decisionResults[0].Score > 0f)
		{
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools && ELCPUtilities.ELCPVerboseMode)
			{
				foreach (DecisionResult decisionResult in this.decisionResults)
				{
					PillarDefinition pillarDefinition3 = decisionResult.Element as PillarDefinition;
					Diagnostics.Log("ELCP {0}/{1} AILayer_Pillar evaluations {2} {3}", new object[]
					{
						base.AIEntity.Empire,
						this.aiEntityCity.City.LocalizedName,
						pillarDefinition3.Name,
						decisionResult.Score
					});
				}
			}
			PillarDefinition pillarDefinition2 = this.decisionResults[0].Element as PillarDefinition;
			if (pillarDefinition2.Name == AILayer_Pillar.DustPillarName && pillarDefinition2.GetPillarCount(base.AIEntity.Empire) > 2f)
			{
				if (this.decisionResults.Count <= 1 || this.decisionResults[1].Score <= 0f)
				{
					return SynchronousJobState.Failure;
				}
				pillarDefinition2 = (this.decisionResults[1].Element as PillarDefinition);
			}
			int num2 = 0;
			WorldPosition targetPosition = this.ComputeMostOverlappingPositionForRange(pillarDefinition2, out num2);
			if (num2 < 5)
			{
				return SynchronousJobState.Success;
			}
			float pillarDustCost = this.GetPillarDustCost(pillarDefinition2);
			if (!float.IsPositiveInfinity(pillarDustCost) && num >= pillarDustCost && targetPosition.IsValid)
			{
				OrderBuyoutAndActivatePillar order = new OrderBuyoutAndActivatePillar(base.AIEntity.Empire.Index, pillarDefinition2.Name, targetPosition);
				base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order);
				return SynchronousJobState.Running;
			}
		}
		return SynchronousJobState.Success;
	}

	protected WorldPosition ComputeMostOverlappingPositionForRange(PillarDefinition pillarDefinition, out int NumberOfTiles)
	{
		int activeRange = this.pillarService.GetActiveRange(pillarDefinition.Name, base.AIEntity.Empire);
		District district = null;
		NumberOfTiles = 0;
		for (int i = 0; i < this.aiEntityCity.City.Districts.Count; i++)
		{
			District district2 = this.aiEntityCity.City.Districts[i];
			if (this.pillarService.IsPositionValidForPillar(base.AIEntity.Empire, district2.WorldPosition))
			{
				WorldArea worldArea = new WorldArea(new List<WorldPosition>
				{
					district2.WorldPosition
				});
				for (int j = 0; j < activeRange; j++)
				{
					worldArea = worldArea.Grow();
				}
				int num = 0;
				for (int k = 0; k < worldArea.WorldPositions.Count; k++)
				{
					if (this.worldPositioningService.GetDistrict(WorldPosition.GetValidPosition(worldArea.WorldPositions[k], this.worldPositioningService.World.WorldParameters)) != null)
					{
						num++;
					}
				}
				if (num > NumberOfTiles)
				{
					NumberOfTiles = num;
					district = district2;
				}
			}
		}
		if (district == null)
		{
			return WorldPosition.Invalid;
		}
		return district.WorldPosition;
	}

	public override IEnumerator Load()
	{
		yield return base.Load();
		this.PillarDatabase = Databases.GetDatabase<PillarDefinition>(false);
		this.departmentOfForeignAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		this.departmentOfTheTreasury = base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>();
		yield break;
	}

	private IDatabase<AIParameterConverter> aiParameterConverterDatabase;

	private Dictionary<StaticString, IAIParameter<InterpreterContext>[]> pillarEffectsAiParameters;

	private AIEntity_City aiEntityCity;

	private SimulationDecisionMaker<PillarDefinition> decisionMaker;

	private List<DecisionResult> decisionResults = new List<DecisionResult>();

	private IPillarService pillarService;

	private EvaluableMessage_PillarNeed validatedPillarNeed;

	private IWorldPositionningService worldPositioningService;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private float MoneyAtTurnBegin;

	private IDatabase<PillarDefinition> PillarDatabase;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private static StaticString DustPillarName = "Pillar4";
}
