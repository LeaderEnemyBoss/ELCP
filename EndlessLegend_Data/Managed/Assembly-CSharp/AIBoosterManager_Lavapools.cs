using System;
using System.Collections.Generic;
using Amplitude.Unity.AI;
using Amplitude.Unity.AI.Decision;
using Amplitude.Unity.Framework;

public class AIBoosterManager_Lavapools : AIBoosterManager
{
	public AIBoosterManager_Lavapools()
	{
		this.decisionResults = new List<DecisionResult>();
		this.costFactorFromRegistry = 0.5f;
		this.chosenmessages = new List<EvaluableMessage_LavaboostNeeded>();
	}

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
		if (3f - propertyValue > 0f)
		{
			this.decisionResults.Clear();
			this.decisionMaker.EvaluateDecisions(this.boosterDefinitions, ref this.decisionResults);
			for (int i = 0; i < this.decisionResults.Count; i++)
			{
				BoosterDefinition boosterDefinition = this.decisionResults[i].Element as BoosterDefinition;
				if (boosterDefinition != null)
				{
					float productionCostWithBonus = DepartmentOfTheTreasury.GetProductionCostWithBonus(base.Empire, boosterDefinition, boosterDefinition.Costs[0].ResourceName);
					if (base.DepartmentOfTheTreasury.CanAfford(productionCostWithBonus, boosterDefinition.Costs[0].ResourceName))
					{
						this.CreateChosenBoosterNeedMessage(boosterDefinition);
						return;
					}
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
		this.chosenmessages.Clear();
		this.chosenmessages.AddRange(base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_LavaboostNeeded>(BlackboardLayerID.Empire, (EvaluableMessage_LavaboostNeeded match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending));
		if (this.chosenmessages.Count > 0)
		{
			AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>().RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_StartBooster));
		}
	}

	private SynchronousJobState SynchronousJob_StartBooster()
	{
		int count = this.chosenmessages.Count;
		OrderBuyoutAndActivateBooster order = new OrderBuyoutAndActivateBooster(base.Empire.Index, this.chosenmessages[count - 1].BoosterDefinition.Name, 0UL, false);
		Ticket ticket;
		base.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderBuyoutAndActivateBooster_TicketRaised));
		this.chosenmessages.RemoveAt(count - 1);
		if (count == 1)
		{
			return SynchronousJobState.Success;
		}
		return SynchronousJobState.Running;
	}

	private void OrderBuyoutAndActivateBooster_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		OrderBuyoutAndActivateBooster orderBuyoutAndActivateBooster = e.Order as OrderBuyoutAndActivateBooster;
		EvaluableMessage_LavaboostNeeded firstMessage = base.AIEntity.AIPlayer.Blackboard.GetFirstMessage<EvaluableMessage_LavaboostNeeded>(BlackboardLayerID.Empire, (EvaluableMessage_LavaboostNeeded match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending && match.BoosterDefinition.Name == orderBuyoutAndActivateBooster.BoosterDefinitionName);
		if (firstMessage != null)
		{
			if (e.Result != PostOrderResponse.Processed)
			{
				firstMessage.SetFailedToObtain();
				return;
			}
			firstMessage.SetObtained();
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
		this.chosenmessages.Clear();
	}

	private const int requiredLavapools = 3;

	private BoosterDefinition[] boosterDefinitions;

	private EvaluableMessage_LavaboostNeeded chosenBoosterMessage;

	private ISynchronousJobRepositoryAIHelper synchronousJobRepository;

	private SimulationDecisionMaker<ConstructibleElement> decisionMaker;

	private List<DecisionResult> decisionResults;

	private float costFactorFromRegistry;

	private List<EvaluableMessage_LavaboostNeeded> chosenmessages;
}
