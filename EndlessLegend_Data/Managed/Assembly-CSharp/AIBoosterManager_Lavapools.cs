using System;
using System.Collections.Generic;
using Amplitude.Unity.AI;
using Amplitude.Unity.AI.Decision;
using Amplitude.Unity.Framework;

public class AIBoosterManager_Lavapools : AIBoosterManager
{
	protected internal override void Initialize(AIEntity aiEntity)
	{
		base.Initialize(aiEntity);
		IDatabase<ResourceDefinition> database = Databases.GetDatabase<ResourceDefinition>(false);
		IDatabase<BoosterDefinition> database2 = Databases.GetDatabase<BoosterDefinition>(false);
		List<BoosterDefinition> list = new List<BoosterDefinition>();
		foreach (ResourceDefinition resourceDefinition in database)
		{
			if (resourceDefinition.ResourceType == ResourceDefinition.Type.Strategic)
			{
				BoosterDefinition item = null;
				if (database2.TryGetValue("Booster" + resourceDefinition.Name + "Lavapool", out item))
				{
					list.Add(item);
				}
			}
		}
		this.boosterDefinitions = list.ToArray();
		IConstructibleElementEvaluationAIHelper service = AIScheduler.Services.GetService<IConstructibleElementEvaluationAIHelper>();
		this.decisionMaker = new SimulationDecisionMaker<ConstructibleElement>(service, base.Empire);
		this.decisionMaker.ScoreTransferFunctionDelegate = new Func<ConstructibleElement, float, float>(this.DecisionScoreTransferFunctionDelegate);
	}

	protected internal override void CreateLocals()
	{
		base.CreateLocals();
		float propertyValue = base.AIEntity.Empire.GetPropertyValue(SimulationProperties.LavapoolStock);
		float num = 3f - propertyValue;
		if (num > 0f)
		{
			this.decisionResults.Clear();
			this.decisionMaker.EvaluateDecisions(this.boosterDefinitions, ref this.decisionResults);
			for (int i = 0; i < this.decisionResults.Count; i++)
			{
				float productionCostWithBonus = DepartmentOfTheTreasury.GetProductionCostWithBonus(base.Empire, this.boosterDefinitions[i], this.boosterDefinitions[i].Costs[0].ResourceName);
				if (base.DepartmentOfTheTreasury.CanAfford(productionCostWithBonus, this.boosterDefinitions[i].Costs[0].ResourceName))
				{
					this.CreateChosenBoosterNeedMessage(this.boosterDefinitions[i]);
					break;
				}
			}
		}
	}

	private void CreateChosenBoosterNeedMessage(BoosterDefinition boosterDefinition)
	{
		EvaluableMessage_LavaboostNeeded evaluableMessage_LavaboostNeeded = null;
		IEnumerable<EvaluableMessage_LavaboostNeeded> messages = base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_LavaboostNeeded>(BlackboardLayerID.Empire);
		foreach (EvaluableMessage_LavaboostNeeded evaluableMessage_LavaboostNeeded2 in messages)
		{
			if (evaluableMessage_LavaboostNeeded2.State == BlackboardMessage.StateValue.Message_InProgress)
			{
				if (evaluableMessage_LavaboostNeeded == null)
				{
					evaluableMessage_LavaboostNeeded = evaluableMessage_LavaboostNeeded2;
				}
				else
				{
					evaluableMessage_LavaboostNeeded2.Cancel();
				}
			}
		}
		if (evaluableMessage_LavaboostNeeded == null)
		{
			evaluableMessage_LavaboostNeeded = new EvaluableMessage_LavaboostNeeded(1f, 1f, boosterDefinition, AILayer_AccountManager.NoAccountName);
			base.AIEntity.AIPlayer.Blackboard.AddMessage(evaluableMessage_LavaboostNeeded);
		}
		else
		{
			evaluableMessage_LavaboostNeeded.Refresh(1f, 1f, boosterDefinition);
		}
	}

	protected internal override void Execute()
	{
		base.Execute();
		this.chosenBoosterMessage = null;
		IEnumerable<EvaluableMessage_LavaboostNeeded> messages = base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_LavaboostNeeded>(BlackboardLayerID.Empire, (EvaluableMessage_LavaboostNeeded match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending);
		foreach (EvaluableMessage_LavaboostNeeded evaluableMessage_LavaboostNeeded in messages)
		{
			this.chosenBoosterMessage = evaluableMessage_LavaboostNeeded;
			ISynchronousJobRepositoryAIHelper service = AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>();
			service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_StartBooster));
		}
	}

	private SynchronousJobState SynchronousJob_StartBooster()
	{
		if (this.chosenBoosterMessage == null)
		{
			return SynchronousJobState.Failure;
		}
		OrderBuyoutAndActivateBooster order = new OrderBuyoutAndActivateBooster(base.Empire.Index, this.chosenBoosterMessage.BoosterDefinition.Name, 0UL, false);
		Ticket ticket;
		base.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderBuyoutAndActivateBooster_TicketRaised));
		return SynchronousJobState.Success;
	}

	private void OrderBuyoutAndActivateBooster_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		OrderBuyoutAndActivateBooster orderBuyoutAndActivateBooster = e.Order as OrderBuyoutAndActivateBooster;
		if (this.chosenBoosterMessage != null && orderBuyoutAndActivateBooster.BoosterDefinitionName == this.chosenBoosterMessage.BoosterDefinition.Name)
		{
			if (e.Result != PostOrderResponse.Processed)
			{
				this.chosenBoosterMessage.SetFailedToObtain();
			}
			else
			{
				this.chosenBoosterMessage.SetObtained();
			}
		}
	}

	private float DecisionScoreTransferFunctionDelegate(ConstructibleElement aiEvaluableElement, float score)
	{
		BoosterDefinition constructibleElement = aiEvaluableElement as BoosterDefinition;
		ConstructionResourceStock[] array;
		base.DepartmentOfTheTreasury.GetInstantConstructionResourceCostForBuyout(base.Empire, constructibleElement, out array);
		float num = 0f;
		for (int i = 0; i < array.Length; i++)
		{
			float num2 = 0f;
			if (!base.DepartmentOfTheTreasury.TryGetResourceStockValue(base.Empire, array[i].PropertyName, out num2, false) || num2 == 0f)
			{
				num = 1f;
				break;
			}
			float num3 = array[i].Stock / num2;
			if (num3 > num)
			{
				num = num3;
			}
		}
		score = AILayer.Boost(score, this.costFactorFromRegistry * (1f - num));
		return score;
	}

	protected internal override void Release()
	{
		base.Release();
		this.synchronousJobRepository = null;
	}

	private const int requiredLavapools = 3;

	private BoosterDefinition[] boosterDefinitions;

	private EvaluableMessage_LavaboostNeeded chosenBoosterMessage;

	private ISynchronousJobRepositoryAIHelper synchronousJobRepository;

	private SimulationDecisionMaker<ConstructibleElement> decisionMaker;

	private List<DecisionResult> decisionResults = new List<DecisionResult>();

	private float costFactorFromRegistry = 0.5f;
}
