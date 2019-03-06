using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Amplitude;
using Amplitude.Extensions;
using Amplitude.Unity.AI;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using UnityEngine;

[PersonalityRegistryPath("AI/MajorEmpire/AIEntity_Empire/AILayer_Trade/", new object[]
{

})]
public class AILayer_Trade : AILayer, ITickable
{
	public override void ReadXml(XmlReader reader)
	{
		this.resourceSelloutCooldown = reader.GetAttribute<int>("ResourceSelloutCoolDown");
		base.ReadXml(reader);
		if (reader.IsStartElement("AccumulatedResources"))
		{
			int attribute = reader.GetAttribute<int>("Count");
			reader.ReadStartElement();
			for (int i = 0; i < attribute; i++)
			{
				string attribute2 = reader.GetAttribute("Name");
				float attribute3 = reader.GetAttribute<float>("Value");
				if (this.accumulatedResourcesForMarket.ContainsKey(attribute2))
				{
					this.accumulatedResourcesForMarket[attribute2] = attribute3;
				}
				else
				{
					this.accumulatedResourcesForMarket.Add(attribute2, attribute3);
				}
				reader.Skip();
			}
			reader.ReadEndElement();
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		base.WriteXml(writer);
		writer.WriteAttributeString<int>("ResourceSelloutCoolDown", this.resourceSelloutCooldown);
		if (this.accumulatedResourcesForMarket.Count > 0)
		{
			writer.WriteStartElement("AccumulatedResources");
			writer.WriteAttributeString<int>("Count", this.accumulatedResourcesForMarket.Count);
			foreach (KeyValuePair<StaticString, float> keyValuePair in this.accumulatedResourcesForMarket)
			{
				writer.WriteStartElement("Resource");
				writer.WriteAttributeString<StaticString>("Name", keyValuePair.Key);
				writer.WriteAttributeString<float>("Value", keyValuePair.Value);
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}
	}

	public TickableState State { get; set; }

	public static void CancelResourceNeed(StaticString itemName, Blackboard blackboard)
	{
		IEnumerable<EvaluableMessage_ResourceNeed> messages = blackboard.GetMessages<EvaluableMessage_ResourceNeed>(BlackboardLayerID.Empire, (EvaluableMessage_ResourceNeed match) => match.ReferenceItem == itemName);
		if (messages != null)
		{
			foreach (EvaluableMessage_ResourceNeed evaluableMessage_ResourceNeed in messages)
			{
				evaluableMessage_ResourceNeed.Cancel();
			}
		}
	}

	public static void UpdateHeroNeed(float globalMotivation, float localOpportunity, TradableUnit tradableUnit, Blackboard blackboard)
	{
		bool flag = false;
		IEnumerable<EvaluableMessage_HeroNeed> messages = blackboard.GetMessages<EvaluableMessage_HeroNeed>(BlackboardLayerID.Empire);
		if (messages != null)
		{
			foreach (EvaluableMessage_HeroNeed evaluableMessage_HeroNeed in messages)
			{
				if (evaluableMessage_HeroNeed.State == BlackboardMessage.StateValue.Message_InProgress)
				{
					flag = true;
					evaluableMessage_HeroNeed.UpdateHeroNeeds(globalMotivation, localOpportunity, tradableUnit.UID);
				}
			}
		}
		if (!flag)
		{
			EvaluableMessage_HeroNeed evaluableMessage_HeroNeed2 = new EvaluableMessage_HeroNeed(globalMotivation, localOpportunity, tradableUnit.UID, AILayer_AccountManager.HeroAccountName);
			evaluableMessage_HeroNeed2.SetInterest(globalMotivation, localOpportunity);
			blackboard.AddMessage(evaluableMessage_HeroNeed2);
		}
	}

	public static void UpdateResourceNeed(float globalMotivation, float localOpportunity, List<MissingResource> missingResourceNeeds, StaticString itemName, Blackboard blackboard)
	{
		bool flag = false;
		IEnumerable<EvaluableMessage_ResourceNeed> messages = blackboard.GetMessages<EvaluableMessage_ResourceNeed>(BlackboardLayerID.Empire, (EvaluableMessage_ResourceNeed match) => match.ReferenceItem == itemName);
		if (messages != null)
		{
			foreach (EvaluableMessage_ResourceNeed evaluableMessage_ResourceNeed in messages)
			{
				flag = true;
				evaluableMessage_ResourceNeed.UpdateResourceNeeds(globalMotivation, localOpportunity, missingResourceNeeds);
			}
		}
		if (!flag)
		{
			EvaluableMessage_ResourceNeed evaluableMessage_ResourceNeed2 = new EvaluableMessage_ResourceNeed(globalMotivation, localOpportunity, missingResourceNeeds, AILayer_AccountManager.EconomyAccountName, itemName);
			evaluableMessage_ResourceNeed2.SetInterest(globalMotivation, localOpportunity);
			blackboard.AddMessage(evaluableMessage_ResourceNeed2);
		}
	}

	public override IEnumerator Initialize(AIEntity ai)
	{
		yield return base.Initialize(ai);
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.game = (service.Game as global::Game);
		Diagnostics.Assert(this.game != null);
		this.synchronousJobRepositoryHelper = AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>();
		Diagnostics.Assert(this.synchronousJobRepositoryHelper != null);
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.unitPatternHelper = AIScheduler.Services.GetService<IUnitPatternAIHelper>();
		this.unitDesignDataRepository = AIScheduler.Services.GetService<IAIUnitDesignDataRepository>();
		ITickableRepositoryAIHelper service2 = AIScheduler.Services.GetService<ITickableRepositoryAIHelper>();
		Diagnostics.Assert(service2 != null);
		service2.Register(this);
		this.departmentOfScience = base.AIEntity.Empire.GetAgency<DepartmentOfScience>();
		this.departmentOfTheTreasury = base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>();
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		this.departmentOfEducation = base.AIEntity.Empire.GetAgency<DepartmentOfEducation>();
		this.departmentOfForeignAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		this.departmentOfPlanificationAndDevelopment = base.AIEntity.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
		this.tradableUnitAffinities = new Dictionary<GameEntityGUID, DecisionResult[]>();
		this.tradeManagementService = this.game.Services.GetService<ITradeManagementService>();
		this.queuedResourceBuyoutOrders = new List<AILayer_Trade.QueuedResourceBuyoutOrder>();
		this.queuedUnitBuyoutOrders = new List<AILayer_Trade.QueuedUnitBuyoutOrder>();
		this.hostCities = new List<City>();
		base.AIEntity.RegisterPass(AIEntity.Passes.EvaluateNeeds.ToString(), "LayerTrade_EvaluateNeedsPass", new AIEntity.AIAction(this.EvaluateNeeds), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.ExecuteNeeds.ToString(), "LayerTrade_ExecuteNeedsPass", new AIEntity.AIAction(this.ExecuteNeeds), this, new StaticString[0]);
		this.accumulatedResourcesForMarket = new Dictionary<StaticString, float>();
		this.resourceDefinitionDatabase = Databases.GetDatabase<ResourceDefinition>(false);
		foreach (ResourceDefinition resourceDefinition in this.resourceDefinitionDatabase)
		{
			if (resourceDefinition.ResourceType == ResourceDefinition.Type.Strategic || resourceDefinition.ResourceType == ResourceDefinition.Type.Luxury)
			{
				this.accumulatedResourcesForMarket.Add(resourceDefinition.Name, 0f);
			}
		}
		this.questManagementService = service.Game.Services.GetService<IQuestManagementService>();
		this.aILayer_Research = base.AIEntity.GetLayer<AILayer_Research>();
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public override void Release()
	{
		this.resourceDefinitionDatabase = null;
		this.queuedUnitBuyoutOrders = null;
		this.queuedResourceBuyoutOrders = null;
		this.hostCities = null;
		this.synchronousJobRepositoryHelper = null;
		this.departmentOfTheTreasury = null;
		this.departmentOfTheInterior = null;
		this.departmentOfScience = null;
		this.departmentOfEducation = null;
		this.departmentOfForeignAffairs = null;
		this.departmentOfPlanificationAndDevelopment = null;
		this.tradeManagementService = null;
		this.worldPositionningService = null;
		this.questManagementService = null;
		this.unitPatternHelper = null;
		this.tradableUnitAffinities = null;
		this.MercsInNeedOfReinforcements = null;
		ITickableRepositoryAIHelper service = AIScheduler.Services.GetService<ITickableRepositoryAIHelper>();
		Diagnostics.Assert(service != null);
		service.Unregister(this);
		this.accumulatedResourcesForMarket.Clear();
		this.aILayer_Research = null;
		base.Release();
	}

	public void Tick()
	{
		if (this.ProcessNextQueuedUnitTradingOrder())
		{
			this.State = TickableState.NoTick;
		}
	}

	protected override void EvaluateNeeds(StaticString context, StaticString pass)
	{
		if (this.departmentOfScience.CanTradeResourcesAndBoosters(false))
		{
			IEnumerable<EvaluableMessage_ResourceNeed> messages = base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_ResourceNeed>(BlackboardLayerID.Empire, (EvaluableMessage_ResourceNeed match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending || match.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate);
			if (messages.Any<EvaluableMessage_ResourceNeed>())
			{
				foreach (EvaluableMessage_ResourceNeed evaluableMessage_ResourceNeed in messages)
				{
					float dustCost;
					int turnGain;
					if (evaluableMessage_ResourceNeed.MissingResources == null)
					{
						AILayer.LogWarning("[SCORING] EvaluableMessage {0} has been stated Pending but the missing resources have not been filled", new object[]
						{
							evaluableMessage_ResourceNeed.ID
						});
					}
					else if (this.ComputeResourceMetrics(evaluableMessage_ResourceNeed, out dustCost, out turnGain))
					{
						evaluableMessage_ResourceNeed.UpdateBuyEvaluation("Trade", 0UL, dustCost, turnGain, 0f, 0UL);
					}
					else
					{
						evaluableMessage_ResourceNeed.CancelBuyEvaluation("Trade", 0UL);
					}
				}
			}
		}
		if (this.departmentOfScience.CanTradeUnits(false))
		{
			IEnumerable<EvaluableMessageWithUnitDesign> messages2 = base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessageWithUnitDesign>(BlackboardLayerID.Empire, (EvaluableMessageWithUnitDesign match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending);
			if (messages2.Any<EvaluableMessageWithUnitDesign>())
			{
				List<ITradable> list;
				this.tradeManagementService.TryGetTradablesByCategory(TradableUnit.ReadOnlyCategory, out list);
				List<GameEntityGUID> list2 = new List<GameEntityGUID>();
				KeyValuePair<GameEntityGUID, DecisionResult[]> keyValuePair;
				Predicate<ITradable> <>9__2;
				foreach (KeyValuePair<GameEntityGUID, DecisionResult[]> keyValuePair2 in this.tradableUnitAffinities)
				{
					keyValuePair = keyValuePair2;
					List<ITradable> list3 = list;
					Predicate<ITradable> match2;
					if ((match2 = <>9__2) == null)
					{
						match2 = (<>9__2 = ((ITradable match) => match is TradableUnit && (match as TradableUnit).GameEntityGUID == keyValuePair.Key));
					}
					if (list3.Find(match2) == null)
					{
						list2.Add(keyValuePair.Key);
					}
				}
				for (int i = 0; i < list2.Count; i++)
				{
					this.tradableUnitAffinities.Remove(list2[i]);
				}
				for (int j = 0; j < list.Count; j++)
				{
					TradableUnit tradableUnit = list[j] as TradableUnit;
					Unit unit;
					DecisionResult[] value;
					if (this.tradeManagementService.TryRetrieveUnit(tradableUnit.GameEntityGUID, out unit) && !this.tradableUnitAffinities.TryGetValue(tradableUnit.GameEntityGUID, out value))
					{
						this.affinitiesDecisionResults.Clear();
						this.unitPatternHelper.ComputeAllUnitPatternAffinities(unit, ref this.affinitiesDecisionResults);
						value = this.affinitiesDecisionResults.ToArray();
						this.tradableUnitAffinities.Add(tradableUnit.GameEntityGUID, value);
					}
				}
				foreach (EvaluableMessageWithUnitDesign evaluableMessageWithUnitDesign in messages2)
				{
					float dustCost2;
					int turnGain2;
					float distance;
					ulong tradableUID;
					if (this.ComputeUnitMetrics(list, evaluableMessageWithUnitDesign, out dustCost2, out turnGain2, out distance, out tradableUID))
					{
						evaluableMessageWithUnitDesign.UpdateBuyEvaluation("Trade", 0UL, dustCost2, turnGain2, distance, tradableUID);
					}
					else
					{
						evaluableMessageWithUnitDesign.CancelBuyEvaluation("Trade", 0UL);
					}
				}
			}
		}
	}

	protected override void ExecuteNeeds(StaticString context, StaticString pass)
	{
		base.ExecuteNeeds(context, pass);
		this.SaveMoney = false;
		if (this.questManagementService.IsQuestRunningForEmpire("VictoryQuest-Chapter4", base.AIEntity.Empire) || this.questManagementService.IsQuestRunningForEmpire("VictoryQuest-Chapter5Alt", base.AIEntity.Empire) || this.questManagementService.IsQuestRunningForEmpire("VictoryQuest-Chapter5", base.AIEntity.Empire))
		{
			this.SaveMoney = true;
		}
		ISynchronousJobRepositoryAIHelper service = AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>();
		if (this.departmentOfScience.CanTradeResourcesAndBoosters(false))
		{
			this.FillResourceTradingOrdersQueue();
			if (this.queuedResourceBuyoutOrders.Count > 0)
			{
				service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_ProcessNextQueuedResourceTradingOrder));
			}
			service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_DoMagicSelloutOfProducedResources));
			service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_ProcessStockpileSellingOrder));
			this.RichboyResourceTick = 0;
			this.queuedRichboyResource = null;
			if (!this.SaveMoney)
			{
				service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_ProcessRichboyResourceTradingOrder));
			}
		}
		if (this.departmentOfScience.CanTradeHeroes(false))
		{
			IEnumerable<EvaluableMessage_HeroNeed> messages = base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_HeroNeed>(BlackboardLayerID.Empire, (EvaluableMessage_HeroNeed match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate);
			if (messages.Any<EvaluableMessage_HeroNeed>())
			{
				foreach (EvaluableMessage_HeroNeed evaluableMessage_HeroNeed in messages)
				{
					if (evaluableMessage_HeroNeed.ChosenBuyEvaluation != null && !(evaluableMessage_HeroNeed.ChosenBuyEvaluation.LayerTag != "Trade"))
					{
						if (this.queuedHeroBuyoutOrder == null)
						{
							this.queuedHeroBuyoutOrder = new AILayer_Trade.QueuedHeroBuyoutOrder();
						}
						this.queuedHeroBuyoutOrder.Message = evaluableMessage_HeroNeed;
						service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_ProcessNextQueuedHeroTradingOrder));
					}
				}
			}
		}
		this.State = TickableState.NoTick;
		if (this.departmentOfScience.CanTradeUnits(false))
		{
			this.FillUnitTradingOrdersQueue();
			if (this.queuedUnitBuyoutOrders.Count > 0)
			{
				this.State = TickableState.NeedTick;
			}
			if (base.AIEntity.Empire.SimulationObject.Tags.Contains(AILayer_War.TagNoWarTrait) && !base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>().IsInWarWithSomeone() && this.departmentOfScience.CanCreatePrivateers() && base.AIEntity.Empire.GetPropertyValue(SimulationProperties.NetEmpireMoney) > 100f && !this.SaveMoney)
			{
				this.privateertick = 0;
				service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_ProcessPrivateerOrders));
			}
		}
	}

	private bool ComputeResourceMetrics(EvaluableMessage_ResourceNeed evaluableMessage, out float totalBuyoutCost, out int totalTurnGain)
	{
		if (evaluableMessage.ExpectedBuyoutUnitPrices == null)
		{
			evaluableMessage.ExpectedBuyoutUnitPrices = new List<float>();
		}
		evaluableMessage.ExpectedBuyoutUnitPrices.Clear();
		totalBuyoutCost = 0f;
		totalTurnGain = 0;
		for (int i = 0; i < evaluableMessage.MissingResources.Count; i++)
		{
			MissingResource missingResource = evaluableMessage.MissingResources[i];
			TradableResource tradableResource = this.TryGetTradableRessource(missingResource.ResourceName);
			float missingResourceValue = missingResource.MissingResourceValue;
			if (missingResourceValue <= 0f || tradableResource == null || tradableResource.Quantity < missingResourceValue)
			{
				return false;
			}
			float priceWithSalesTaxes = TradableResource.GetPriceWithSalesTaxes(missingResource.ResourceName, TradableTransactionType.Buyout, base.AIEntity.Empire, missingResourceValue);
			totalBuyoutCost += priceWithSalesTaxes;
			evaluableMessage.ExpectedBuyoutUnitPrices.Add(priceWithSalesTaxes / missingResourceValue);
			float num;
			if (!this.departmentOfTheTreasury.TryGetNetResourceValue(base.AIEntity.Empire.SimulationObject, missingResource.ResourceName, out num, false) || num <= 0f)
			{
				totalTurnGain += (int)Math.Ceiling((double)BuyEvaluation.MaxTurnGain);
			}
			else
			{
				totalTurnGain += (int)Math.Ceiling((double)(missingResource.MissingResourceValue / num));
			}
		}
		return true;
	}

	private bool ComputeUnitMetrics(List<ITradable> tradableUnits, EvaluableMessageWithUnitDesign evaluableMessage, out float bestBuyoutCost, out int turnGain, out float bestDistance, out ulong bestTradableUID)
	{
		float num = 0f;
		float num2 = float.MinValue;
		bestTradableUID = 0UL;
		bestDistance = -1f;
		bestBuyoutCost = 0f;
		EvaluableMessage_UnitRequest evaluableMessage_UnitRequest = evaluableMessage as EvaluableMessage_UnitRequest;
		if (evaluableMessage_UnitRequest != null)
		{
			if (evaluableMessage.UnitDesign != null)
			{
				AIUnitDesignData aiunitDesignData = null;
				if (this.unitDesignDataRepository.TryGetUnitDesignData(base.AIEntity.Empire.Index, evaluableMessage.UnitDesign.Model, out aiunitDesignData) && aiunitDesignData.UnitPatternAffinities != null)
				{
					num = UnitPatternAIEvaluationHelper.ComputeUnitPatternCategoryAffinity(aiunitDesignData.UnitPatternAffinities, evaluableMessage_UnitRequest.WantedUnitPatternCategory);
					if (num < 0f)
					{
						num = 0f;
					}
				}
			}
			bestBuyoutCost = float.MaxValue;
			for (int i = 0; i < tradableUnits.Count; i++)
			{
				TradableUnit tradableUnit = tradableUnits[i] as TradableUnit;
				DecisionResult[] affinities;
				if (evaluableMessage_UnitRequest.WantedUnitPatternCategory != StaticString.Empty && this.tradableUnitAffinities.TryGetValue(tradableUnit.GameEntityGUID, out affinities))
				{
					float priceWithSalesTaxes = tradableUnit.GetPriceWithSalesTaxes(TradableTransactionType.Buyout, base.AIEntity.Empire, 1f);
					float num3 = UnitPatternAIEvaluationHelper.ComputeUnitPatternCategoryAffinity(affinities, evaluableMessage_UnitRequest.WantedUnitPatternCategory);
					if (num3 >= 0f)
					{
						float num4 = AILayer.ComputeBoost(num, num3);
						float num5 = num4 / priceWithSalesTaxes;
						if (num5 > num2)
						{
							num2 = num5;
							bestDistance = num4;
							bestBuyoutCost = priceWithSalesTaxes;
							bestTradableUID = tradableUnit.UID;
						}
					}
				}
			}
		}
		turnGain = (int)BuyEvaluation.MaxTurnGain;
		if (bestTradableUID != 0UL)
		{
			for (int j = 0; j < evaluableMessage.ProductionEvaluations.Count; j++)
			{
				int productionDurationInTurn = evaluableMessage.ProductionEvaluations[j].ProductionDurationInTurn;
				if (productionDurationInTurn < turnGain)
				{
					turnGain = productionDurationInTurn;
				}
			}
			return true;
		}
		return false;
	}

	private void FillResourceTradingOrdersQueue()
	{
		this.queuedResourceBuyoutOrders.Clear();
		this.currentResourceBuyoutOrder = 0;
		IEnumerable<EvaluableMessage_ResourceNeed> messages = base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_ResourceNeed>(BlackboardLayerID.Empire, (EvaluableMessage_ResourceNeed match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate && match.ChosenBuyEvaluation != null && match.ChosenBuyEvaluation.LayerTag == "Trade");
		if (messages.Any<EvaluableMessage_ResourceNeed>())
		{
			foreach (EvaluableMessage_ResourceNeed evaluableMessage_ResourceNeed in messages)
			{
				for (int i = 0; i < evaluableMessage_ResourceNeed.MissingResources.Count; i++)
				{
					this.queuedResourceBuyoutOrders.Add(new AILayer_Trade.QueuedResourceBuyoutOrder(evaluableMessage_ResourceNeed, i));
				}
			}
		}
		this.queuedResourceBuyoutOrders.Sort((AILayer_Trade.QueuedResourceBuyoutOrder left, AILayer_Trade.QueuedResourceBuyoutOrder right) => -1 * left.Message.ChosenBuyEvaluation.BuyoutFinalScore.CompareTo(right.Message.ChosenBuyEvaluation.BuyoutFinalScore));
	}

	private void FillUnitTradingOrdersQueue()
	{
		this.queuedUnitBuyoutOrders.Clear();
		this.currentUnitBuyoutOrder = 0;
		IEnumerable<EvaluableMessageWithUnitDesign> messages = base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessageWithUnitDesign>(BlackboardLayerID.Empire, (EvaluableMessageWithUnitDesign match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate && match.ChosenBuyEvaluation != null && match.ChosenBuyEvaluation.LayerTag == "Trade");
		if (messages.Any<EvaluableMessageWithUnitDesign>())
		{
			foreach (EvaluableMessageWithUnitDesign evaluableMessageWithUnitDesign in messages)
			{
				EvaluableMessage_UnitRequest message = (EvaluableMessage_UnitRequest)evaluableMessageWithUnitDesign;
				this.queuedUnitBuyoutOrders.Add(new AILayer_Trade.QueuedUnitBuyoutOrder(message));
			}
		}
		this.queuedUnitBuyoutOrders.Sort((AILayer_Trade.QueuedUnitBuyoutOrder left, AILayer_Trade.QueuedUnitBuyoutOrder right) => -1 * left.Message.ChosenBuyEvaluation.BuyoutFinalScore.CompareTo(right.Message.ChosenBuyEvaluation.BuyoutFinalScore));
	}

	private void HeroTradingOrder_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		if (e.Result != PostOrderResponse.Processed)
		{
			this.queuedHeroBuyoutOrder.Message.SetFailedToObtain();
			return;
		}
		this.queuedHeroBuyoutOrder.Message.SetObtained();
	}

	private bool ProcessNextQueuedUnitTradingOrder()
	{
		if (this.currentUnitBuyoutOrder < 0)
		{
			return false;
		}
		if (this.currentUnitBuyoutOrder >= this.queuedUnitBuyoutOrders.Count)
		{
			return true;
		}
		AILayer_Trade.QueuedUnitBuyoutOrder queuedUnitBuyoutOrder = this.queuedUnitBuyoutOrders[this.currentUnitBuyoutOrder];
		if (queuedUnitBuyoutOrder.TryCount > 0)
		{
			queuedUnitBuyoutOrder.TryCount--;
			ITradable tradable;
			if (this.tradeManagementService.TryGetTradableByUID(queuedUnitBuyoutOrder.Message.ChosenBuyEvaluation.TradableUID, out tradable))
			{
				EvaluableMessage_UnitRequest evaluableMessage_UnitRequest = queuedUnitBuyoutOrder.Message as EvaluableMessage_UnitRequest;
				if (evaluableMessage_UnitRequest != null && evaluableMessage_UnitRequest.RequestUnitListMessageID != 0UL)
				{
					RequestUnitListMessage requestUnitListMessage = (RequestUnitListMessage)base.AIEntity.AIPlayer.Blackboard.GetMessage(evaluableMessage_UnitRequest.RequestUnitListMessageID);
					if (requestUnitListMessage != null && requestUnitListMessage.FinalPosition.IsValid)
					{
						this.hostCities.Clear();
						for (int i = 0; i < this.departmentOfTheInterior.Cities.Count; i++)
						{
							City city = this.departmentOfTheInterior.Cities[i];
							if (city.UnitsCount < city.MaximumUnitSlot)
							{
								this.hostCities.Add(city);
							}
						}
						if (this.hostCities.Count > 0)
						{
							int index = -1;
							float num = float.MaxValue;
							for (int j = 0; j < this.hostCities.Count; j++)
							{
								float num2 = (float)this.worldPositionningService.GetDistance(requestUnitListMessage.FinalPosition, this.hostCities[j].WorldPosition);
								if (num2 < num)
								{
									num = num2;
									index = j;
								}
							}
							this.currentUnitBuyoutOrder = -(this.currentUnitBuyoutOrder + 1);
							OrderBuyoutTradableUnit order = new OrderBuyoutTradableUnit(base.AIEntity.Empire.Index, tradable.UID, 1f, this.hostCities[index].GUID);
							Ticket ticket;
							base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.UnitTradingOrder_TicketRaised));
							return false;
						}
						return false;
					}
				}
			}
		}
		this.currentUnitBuyoutOrder++;
		return false;
	}

	private void ResourceTradingOrder_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		AILayer_Trade.QueuedResourceBuyoutOrder queuedResourceBuyoutOrder = this.queuedResourceBuyoutOrders[this.currentResourceBuyoutOrder];
		if (e.Result != PostOrderResponse.Processed)
		{
			queuedResourceBuyoutOrder.Message.SetFailedToObtain();
			this.currentResourceBuyoutOrder += queuedResourceBuyoutOrder.Message.MissingResources.Count - 1 - queuedResourceBuyoutOrder.IndexResource;
		}
		else if (queuedResourceBuyoutOrder.IndexResource == queuedResourceBuyoutOrder.Message.MissingResources.Count - 1)
		{
			queuedResourceBuyoutOrder.Message.SetObtained();
		}
		if (queuedResourceBuyoutOrder.RemainingQuantityToBuy == 0f)
		{
			this.currentResourceBuyoutOrder++;
		}
		this.SynchronousJob_ProcessNextQueuedResourceTradingOrder();
	}

	private SynchronousJobState SynchronousJob_DoMagicSelloutOfProducedResources()
	{
		float num = Mathf.Clamp(this.magicResourceSellingProportion, 0f, 1f);
		foreach (ResourceDefinition resourceDefinition in this.resourceDefinitionDatabase)
		{
			float num2;
			if ((resourceDefinition.ResourceType == ResourceDefinition.Type.Strategic || resourceDefinition.ResourceType == ResourceDefinition.Type.Luxury) && (this.departmentOfTheTreasury.TryGetNetResourceValue(base.AIEntity.Empire.SimulationObject, resourceDefinition.Name, out num2, false) || num2 <= 0f))
			{
				Dictionary<StaticString, float> dictionary = this.accumulatedResourcesForMarket;
				StaticString name;
				float num3 = dictionary[name = resourceDefinition.Name];
				dictionary[name] = num3 + num2 * num;
			}
		}
		int num4 = this.resourceSelloutCooldown - 1;
		this.resourceSelloutCooldown = num4;
		if (num4 < 0)
		{
			StaticString staticString = StaticString.Empty;
			float num5 = 1f;
			foreach (KeyValuePair<StaticString, float> keyValuePair in this.accumulatedResourcesForMarket)
			{
				if (keyValuePair.Value > num5)
				{
					num5 = keyValuePair.Value;
					staticString = keyValuePair.Key;
				}
			}
			if (staticString != StaticString.Empty)
			{
				int i = (int)Math.Floor((double)num5);
				Dictionary<StaticString, float> dictionary2 = this.accumulatedResourcesForMarket;
				StaticString key;
				float num6 = dictionary2[key = staticString];
				dictionary2[key] = num6 - (float)i;
				this.resourceSelloutCooldown = UnityEngine.Random.Range(this.minimalMagicTimeRange, this.maximalMagicTimeRange);
				while (i > 10)
				{
					OrderSelloutTradableResource order = new OrderSelloutTradableResource(base.AIEntity.Empire.Index, staticString, 10f, true);
					base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order);
					i -= 10;
				}
				if (i >= 1)
				{
					base.AIEntity.Empire.PlayerControllers.AI.PostOrder(new OrderSelloutTradableResource(base.AIEntity.Empire.Index, staticString, (float)i, true));
				}
			}
		}
		return SynchronousJobState.Success;
	}

	private SynchronousJobState SynchronousJob_ProcessNextQueuedHeroTradingOrder()
	{
		this.queuedHeroBuyoutOrder.Message.SetBeingObtained(this.queuedHeroBuyoutOrder.Message.TradableUID);
		OrderBuyoutTradable order = new OrderBuyoutTradable(base.AIEntity.Empire.Index, this.queuedHeroBuyoutOrder.Message.TradableUID, 1f);
		Ticket ticket;
		base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.HeroTradingOrder_TicketRaised));
		return SynchronousJobState.Success;
	}

	private SynchronousJobState SynchronousJob_ProcessNextQueuedResourceTradingOrder()
	{
		int turn = this.game.Turn;
		ReadOnlyCollection<TradableTransaction> pastTransactions = this.tradeManagementService.GetPastTransactions();
		AILayer_Trade.QueuedResourceBuyoutOrder queuedResourceBuyoutOrder = null;
		bool flag = false;
		while (this.currentResourceBuyoutOrder < this.queuedResourceBuyoutOrders.Count && !flag)
		{
			queuedResourceBuyoutOrder = this.queuedResourceBuyoutOrders[this.currentResourceBuyoutOrder];
			flag = true;
			if (queuedResourceBuyoutOrder.IndexResource == 0)
			{
				for (int i = 0; i < queuedResourceBuyoutOrder.Message.MissingResources.Count; i++)
				{
					MissingResource missingResource = queuedResourceBuyoutOrder.Message.MissingResources[i];
					TradableResource tradableResource = this.TryGetTradableRessource(missingResource.ResourceName);
					if (tradableResource == null || tradableResource.Quantity < missingResource.MissingResourceValue)
					{
						flag = false;
						this.currentResourceBuyoutOrder += queuedResourceBuyoutOrder.Message.MissingResources.Count;
						break;
					}
				}
			}
		}
		if (queuedResourceBuyoutOrder != null && flag)
		{
			AILayer_AccountManager layer = base.AIEntity.GetLayer<AILayer_AccountManager>();
			MissingResource missingResource2 = queuedResourceBuyoutOrder.Message.MissingResources[queuedResourceBuyoutOrder.IndexResource];
			TradableResource tradableResource2 = this.TryGetTradableRessource(missingResource2.ResourceName);
			if (tradableResource2 == null)
			{
				return SynchronousJobState.Failure;
			}
			float num = Math.Min(10f, queuedResourceBuyoutOrder.RemainingQuantityToBuy);
			float num2 = num * queuedResourceBuyoutOrder.ExpectedUnitPrice;
			float priceWithSalesTaxes = TradableResource.GetPriceWithSalesTaxes(missingResource2.ResourceName, TradableTransactionType.Buyout, base.AIEntity.Empire, num);
			if ((num2 >= priceWithSalesTaxes || layer.TryMakeUnexpectedImmediateExpense(queuedResourceBuyoutOrder.Message.AccountTag, priceWithSalesTaxes - num2, 0f)) && !pastTransactions.Any((TradableTransaction T) => T.ReferenceName == missingResource2.ResourceName && T.Type == TradableTransactionType.Sellout && T.Turn >= (uint)(turn - 2) && T.EmpireIndex == (uint)this.AIEntity.Empire.Index))
			{
				Ticket ticket;
				base.AIEntity.Empire.PlayerControllers.AI.PostOrder(new OrderBuyoutTradable(base.AIEntity.Empire.Index, tradableResource2.UID, num), out ticket, new EventHandler<TicketRaisedEventArgs>(this.ResourceTradingOrder_TicketRaised));
				queuedResourceBuyoutOrder.RemainingQuantityToBuy -= num;
			}
			else
			{
				this.currentResourceBuyoutOrder++;
				this.SynchronousJob_ProcessNextQueuedResourceTradingOrder();
			}
		}
		return SynchronousJobState.Success;
	}

	private TradableResource TryGetTradableRessource(StaticString resourceName)
	{
		List<ITradable> list;
		this.tradeManagementService.TryGetTradables("TradableResource" + resourceName, out list);
		for (int i = 0; i < list.Count; i++)
		{
			TradableResource tradableResource = list[i] as TradableResource;
			if (tradableResource.ResourceName == resourceName)
			{
				return tradableResource;
			}
		}
		return null;
	}

	private void UnitTradingOrder_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		if (this.currentUnitBuyoutOrder >= 0)
		{
			AILayer.LogError("[AILayer_Trade] Receiving a ticket while the buyout order index being processed is unknown: " + this.currentUnitBuyoutOrder);
			return;
		}
		this.currentUnitBuyoutOrder = -(this.currentUnitBuyoutOrder + 1);
		if (this.currentUnitBuyoutOrder < 0 || this.currentUnitBuyoutOrder >= this.queuedUnitBuyoutOrders.Count)
		{
			AILayer.LogError("[AILayer_Trade] Receiving a ticket while the buyout order index is out of range: " + this.currentUnitBuyoutOrder);
			return;
		}
		if (e.Result != PostOrderResponse.Processed)
		{
			this.queuedUnitBuyoutOrders[this.currentUnitBuyoutOrder].Message.SetFailedToObtain();
		}
		else
		{
			this.queuedUnitBuyoutOrders[this.currentUnitBuyoutOrder].Message.SetObtained();
		}
		this.currentUnitBuyoutOrder++;
	}

	private SynchronousJobState SynchronousJob_ProcessStockpileSellingOrder()
	{
		if (!this.departmentOfScience.CanTradeResourcesAndBoosters(false))
		{
			return SynchronousJobState.Failure;
		}
		float num = 0f;
		if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpireMoney, out num, false))
		{
			num = 0f;
		}
		List<string> list = new List<string>();
		string a = "";
		int num2 = 0;
		DepartmentOfPlanificationAndDevelopment agency = base.AIEntity.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
		using (List<string>.Enumerator enumerator = new List<string>
		{
			"BoosterIndustry",
			"BoosterScience",
			"BoosterFood"
		}.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				string boosterdef = enumerator.Current;
				int num3 = agency.CountBoosters((BoosterDefinition match) => match.Name == boosterdef);
				if (num3 == 0)
				{
					list.Add(boosterdef);
				}
				else if (((num3 > 6 && num + base.AIEntity.Empire.GetPropertyValue(SimulationProperties.NetEmpireMoney) < 600f) || num3 > 14) && num3 > num2)
				{
					num2 = num3;
					a = boosterdef;
				}
				else if (boosterdef == "BoosterScience" && base.AIEntity.Empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitReplicants1) && num3 > 0)
				{
					a = boosterdef;
				}
				else if (boosterdef == "BoosterFood" && base.AIEntity.Empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitBrokenLords2) && num3 > 0)
				{
					a = boosterdef;
				}
			}
		}
		DepartmentOfEducation agency2 = base.AIEntity.Empire.GetAgency<DepartmentOfEducation>();
		AILayer_ResourceManager layer = base.AIEntity.GetLayer<AILayer_ResourceManager>();
		if (a != string.Empty && agency2 != null && layer != null)
		{
			List<VaultItem> vaultItems = agency2.GetVaultItems<BoosterDefinition>();
			BoosterDefinition boosterDefinition = null;
			int index = 0;
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				foreach (string text in layer.BoostersInUse)
				{
					Diagnostics.Log("ELCP: Empire {0} SynchronousJob_ProcessStockpileSellingOrder found boosters in use: {1}", new object[]
					{
						base.AIEntity.Empire.Index,
						text
					});
				}
			}
			for (int i = 0; i < vaultItems.Count; i++)
			{
				if (layer.BoostersInUse.Contains(vaultItems[i].GUID.ToString()) && Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
				{
					Diagnostics.Log("ELCP: Empire {1} SynchronousJob_ProcessStockpileSellingOrder booster {0} cant be sold", new object[]
					{
						vaultItems[i].GUID,
						base.AIEntity.Empire.Index
					});
				}
				else
				{
					BoosterDefinition boosterDefinition2 = vaultItems[i].Constructible as BoosterDefinition;
					if (boosterDefinition2 != null && a == boosterDefinition2.Name.ToString())
					{
						boosterDefinition = boosterDefinition2;
						index = i;
						break;
					}
				}
			}
			if (boosterDefinition != null)
			{
				OrderSelloutTradableBooster order = new OrderSelloutTradableBooster(base.AIEntity.Empire.Index, boosterDefinition.Name, new GameEntityGUID[]
				{
					vaultItems[index].GUID
				});
				layer.BoostersInUse.Add(vaultItems[index].GUID.ToString());
				Ticket ticket;
				base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, null);
			}
		}
		List<ITradable> list2;
		this.tradeManagementService.TryGetTradablesByCategory("TradableBooster", out list2);
		if ((this.departmentOfScience.GetTechnologyState("TechnologyDefinitionAllBoosterLevel1") == DepartmentOfScience.ConstructibleElement.State.Researched || this.departmentOfScience.GetTechnologyState("TechnologyDefinitionAllBoosterLevel2") == DepartmentOfScience.ConstructibleElement.State.Researched) && list2.Count > 0 && list.Count > 0 && !this.SaveMoney)
		{
			using (List<string>.Enumerator enumerator3 = list.GetEnumerator())
			{
				while (enumerator3.MoveNext())
				{
					string value = enumerator3.Current;
					if (list2.Any((ITradable match) => match is TradableBooster && (match as TradableBooster).BoosterDefinitionName == value && (match as TradableBooster).Quantity > 0f))
					{
						TradableBooster tradableBooster = list2.First((ITradable match) => match is TradableBooster && (match as TradableBooster).BoosterDefinitionName == value && (match as TradableBooster).Quantity > 0f) as TradableBooster;
						float priceWithSalesTaxes = TradableBooster.GetPriceWithSalesTaxes(value, TradableTransactionType.Buyout, base.AIEntity.Empire, 1f);
						if (tradableBooster.IsTradableValid(base.AIEntity.Empire) && num >= 2f * priceWithSalesTaxes)
						{
							Ticket ticket2;
							base.AIEntity.Empire.PlayerControllers.AI.PostOrder(new OrderBuyoutTradable(base.AIEntity.Empire.Index, tradableBooster.UID, 1f), out ticket2, null);
							break;
						}
					}
				}
			}
		}
		return SynchronousJobState.Success;
	}

	private SynchronousJobState SynchronousJob_ProcessPrivateerOrders()
	{
		if (this.privateertick > 3)
		{
			return SynchronousJobState.Failure;
		}
		this.privateertick++;
		AILayerCommanderController layer = base.AIEntity.GetLayer<AILayer_ArmyManagement>();
		IWorldPositionningService service = Services.GetService<IGameService>().Game.Services.GetService<IWorldPositionningService>();
		if (layer == null || service == null)
		{
			return SynchronousJobState.Failure;
		}
		AICommander_WarWithObjective aicommander_WarWithObjective = null;
		foreach (AICommander aicommander in layer.AICommanders)
		{
			if (aicommander is AICommander_WarWithObjective)
			{
				if (aicommander.Missions.Any((AICommanderMission match) => match is AICommanderMission_PrivateersHarass && !match.AIDataArmyGUID.IsValid))
				{
					aicommander_WarWithObjective = (aicommander as AICommander_WarWithObjective);
					break;
				}
			}
		}
		if (aicommander_WarWithObjective == null)
		{
			return SynchronousJobState.Success;
		}
		Region region = service.GetRegion(aicommander_WarWithObjective.RegionIndex);
		if (region == null || region.City == null)
		{
			return SynchronousJobState.Failure;
		}
		int num = (int)base.AIEntity.Empire.GetPropertyValue(SimulationProperties.ArmyUnitSlot);
		WorldPosition worldPosition = region.City.WorldPosition;
		if (this.privateertick == 1)
		{
			this.MercsInNeedOfReinforcements = this.GetSmallMercArmy();
		}
		if (this.MercsInNeedOfReinforcements != null)
		{
			num -= this.MercsInNeedOfReinforcements.StandardUnits.Count;
			worldPosition = this.MercsInNeedOfReinforcements.WorldPosition;
		}
		int num2 = int.MaxValue;
		City city = null;
		for (int i = 0; i < this.departmentOfTheInterior.Cities.Count; i++)
		{
			int distance = this.worldPositionningService.GetDistance(worldPosition, this.departmentOfTheInterior.Cities[i].WorldPosition);
			if (distance < num2 && this.departmentOfTheInterior.Cities[i].BesiegingEmpire == null)
			{
				city = this.departmentOfTheInterior.Cities[i];
				num2 = distance;
			}
		}
		if (city == null)
		{
			return SynchronousJobState.Failure;
		}
		List<ITradable> list = new List<ITradable>();
		this.tradeManagementService.TryGetTradablesByCategory(TradableUnit.ReadOnlyCategory, out list);
		for (int j = list.Count - 1; j >= 0; j--)
		{
			TradableUnit tradableUnit = list[j] as TradableUnit;
			Unit unit;
			if (tradableUnit == null)
			{
				list.RemoveAt(j);
			}
			else if (!Services.GetService<IGameService>().Game.Services.GetService<ITradeManagementService>().TryRetrieveUnit(tradableUnit.GameEntityGUID, out unit))
			{
				list.RemoveAt(j);
			}
			else if (unit.IsSeafaring)
			{
				list.RemoveAt(j);
			}
		}
		if (list.Count < num)
		{
			return SynchronousJobState.Success;
		}
		float num3 = 0f;
		if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpireMoney, out num3, false))
		{
			num3 = 0f;
		}
		float num4 = 0f;
		int k = 0;
		while (k < num)
		{
			num4 += list[k].GetPriceWithSalesTaxes(TradableTransactionType.Buyout, base.AIEntity.Empire);
			if (num3 < 1.3f * num4)
			{
				if (k < 4)
				{
					return SynchronousJobState.Success;
				}
				num = k;
				break;
			}
			else
			{
				k++;
			}
		}
		if (city.StandardUnits.Count <= 0)
		{
			for (int l = 0; l < num; l++)
			{
				OrderBuyoutTradableUnit order = new OrderBuyoutTradableUnit(base.AIEntity.Empire.Index, list[l].UID, 1f, city.GUID);
				if (l == num - 1)
				{
					Ticket ticket;
					base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.BuyPrivateers_TicketRaised));
				}
				else
				{
					Ticket ticket2;
					base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket2, null);
				}
			}
			return SynchronousJobState.Success;
		}
		this.CreateCityArmy(city, city.StandardUnits.ToList<Unit>());
		if (this.privateertick < 3)
		{
			return SynchronousJobState.Running;
		}
		return SynchronousJobState.Failure;
	}

	private void CreateCityArmy(City city, List<Unit> units)
	{
		List<WorldPosition> availablePositionsForArmyCreation = DepartmentOfDefense.GetAvailablePositionsForArmyCreation(city);
		IGameService service = Services.GetService<IGameService>();
		if (service.Game != null)
		{
			IPathfindingService service2 = service.Game.Services.GetService<IPathfindingService>();
			if (service2 != null)
			{
				for (int i = 0; i < availablePositionsForArmyCreation.Count; i++)
				{
					WorldPosition worldPosition = availablePositionsForArmyCreation[i];
					bool flag = true;
					for (int j = 0; j < units.Count; j++)
					{
						Unit unit2 = units[j];
						float transitionCost = service2.GetTransitionCost(city.WorldPosition, worldPosition, unit2, PathfindingFlags.IgnoreFogOfWar, null);
						if (unit2.GetPropertyValue(SimulationProperties.Movement) < transitionCost)
						{
							flag = false;
							break;
						}
					}
					if (flag)
					{
						OrderTransferGarrisonToNewArmy order = new OrderTransferGarrisonToNewArmy(city.Empire.Index, city.GUID, units.ConvertAll<GameEntityGUID>((Unit unit) => unit.GUID).ToArray(), worldPosition, StaticString.Empty, false, true, true);
						Ticket ticket;
						base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnCreateResponse));
						return;
					}
				}
			}
		}
	}

	protected void OnCreateResponse(object sender, TicketRaisedEventArgs args)
	{
		if (args.Result != PostOrderResponse.Processed)
		{
			this.privateertick = 99;
		}
		if (args.Result == PostOrderResponse.Processed && this.MercsInNeedOfReinforcements != null)
		{
			AILayer_ArmyRecruitment layer = base.AIEntity.GetLayer<AILayer_ArmyRecruitment>();
			if (layer == null)
			{
				return;
			}
			OrderTransferGarrisonToNewArmy orderTransferGarrisonToNewArmy = args.Order as OrderTransferGarrisonToNewArmy;
			IGameService service = Services.GetService<IGameService>();
			if (orderTransferGarrisonToNewArmy != null && orderTransferGarrisonToNewArmy.ArmyGuid.IsValid)
			{
				IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
				IGameEntity gameEntity;
				if (service2 != null && service2.TryGetValue(orderTransferGarrisonToNewArmy.ArmyGuid, out gameEntity))
				{
					Army army = gameEntity as Army;
					if (army != null)
					{
						foreach (Unit unit in army.StandardUnits)
						{
							Diagnostics.Assert(unit.UnitDesign != null);
							if (!unit.UnitDesign.Tags.Contains(TradableUnit.ReadOnlyMercenary))
							{
								return;
							}
						}
						IAIDataRepositoryAIHelper service3 = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
						List<GameEntityGUID> list = new List<GameEntityGUID>();
						int num = (int)base.AIEntity.Empire.GetPropertyValue(SimulationProperties.ArmyUnitSlot);
						AIData_Army aidata = service3.GetAIData<AIData_Army>(this.MercsInNeedOfReinforcements.GUID);
						if (aidata == null || aidata.CommanderMission != null)
						{
							aidata.CommanderMission.Interrupt();
						}
						foreach (Unit unit2 in this.MercsInNeedOfReinforcements.StandardUnits)
						{
							AIData_Unit aidata_Unit;
							if (service3.TryGetAIData<AIData_Unit>(unit2.GUID, out aidata_Unit))
							{
								aidata_Unit.ReservationExtraTag = AIData_Unit.AIDataReservationExtraTag.None;
								list.Add(aidata_Unit.Unit.GUID);
							}
						}
						foreach (Unit unit3 in army.StandardUnits)
						{
							AIData_Unit aidata_Unit2;
							if (service3.TryGetAIData<AIData_Unit>(unit3.GUID, out aidata_Unit2))
							{
								aidata_Unit2.ReservationExtraTag = AIData_Unit.AIDataReservationExtraTag.None;
								list.Add(aidata_Unit2.Unit.GUID);
								if (list.Count >= num)
								{
									break;
								}
							}
						}
						layer.CreateNewCommanderRegroup(list.ToArray());
						this.MercsInNeedOfReinforcements = null;
					}
				}
			}
		}
	}

	protected void BuyPrivateers_TicketRaised(object sender, TicketRaisedEventArgs args)
	{
		OrderBuyoutTradableUnit orderBuyoutTradableUnit = args.Order as OrderBuyoutTradableUnit;
		IGameService service = Services.GetService<IGameService>();
		if (orderBuyoutTradableUnit != null && orderBuyoutTradableUnit.Destination.IsValid)
		{
			IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
			IGameEntity gameEntity;
			if (service2 != null && service2.TryGetValue(orderBuyoutTradableUnit.Destination, out gameEntity))
			{
				City city = gameEntity as City;
				if (city != null && city.StandardUnits.Count > 0)
				{
					this.CreateCityArmy(city, city.StandardUnits.ToList<Unit>());
				}
			}
		}
	}

	private Army GetSmallMercArmy()
	{
		DepartmentOfDefense agency = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		IAIDataRepositoryAIHelper service = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		for (int i = 0; i < agency.Armies.Count; i++)
		{
			Army army = agency.Armies[i];
			AIData_Army aidata = service.GetAIData<AIData_Army>(army.GUID);
			if (aidata == null || !(aidata.CommanderMission is AICommanderMission_RegroupArmyAt))
			{
				if (army.IsPrivateers)
				{
					return army;
				}
				using (IEnumerator<Unit> enumerator = army.StandardUnits.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (!enumerator.Current.UnitDesign.Tags.Contains(TradableUnit.ReadOnlyMercenary))
						{
							goto IL_A1;
						}
					}
				}
				return army;
			}
			IL_A1:;
		}
		return null;
	}

	private SynchronousJobState SynchronousJob_ProcessRichboyResourceTradingOrder()
	{
		if (!this.departmentOfScience.CanTradeResourcesAndBoosters(false))
		{
			return SynchronousJobState.Failure;
		}
		if (this.RichboyResourceTick < 3)
		{
			this.RichboyResourceTick++;
			return SynchronousJobState.Running;
		}
		int turn = this.game.Turn;
		ReadOnlyCollection<TradableTransaction> pastTransactions = this.tradeManagementService.GetPastTransactions();
		float num;
		if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpireMoney, out num, false))
		{
			num = 0f;
		}
		if (this.queuedRichboyResource == null)
		{
			IDatabase<BoosterDefinition> database = Databases.GetDatabase<BoosterDefinition>(false);
			List<string> list = new List<string>
			{
				"Luxury5",
				"Luxury11",
				"Luxury12",
				"Luxury7",
				"Luxury3",
				"Luxury13",
				"Luxury6",
				"Luxury2",
				"Luxury10",
				"Luxury4",
				"Luxury8",
				"Strategic1",
				"Strategic2",
				"Strategic3",
				"Strategic4",
				"Strategic5",
				"Strategic6",
				"Luxury1",
				"Luxury9",
				"Luxury15",
				"Luxury14"
			};
			float num2 = 0f;
			foreach (City city in this.departmentOfTheInterior.Cities)
			{
				num2 += city.GetPropertyValue(SimulationProperties.OverrallTradeRoutesCityDustIncome);
			}
			if (num2 < 50f)
			{
				list.Remove("Luxury7");
			}
			if (this.departmentOfEducation.Heroes.Count < 5)
			{
				list.Remove("Luxury13");
			}
			if (base.AIEntity.Empire.GetPropertyValue(SimulationProperties.NetEmpireApproval) > 90f)
			{
				list.Remove("Luxury14");
				list.Remove("Luxury5");
			}
			if (!this.departmentOfForeignAffairs.IsInWarWithSomeone())
			{
				list.Remove("Luxury6");
				list.Remove("Luxury2");
			}
			if (base.AIEntity.Empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitReplicants1))
			{
				list.Remove("Luxury8");
			}
			if (base.AIEntity.Empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitBrokenLords2))
			{
				list.Remove("Luxury4");
			}
			if (num < 1000f || (float)this.departmentOfEducation.Heroes.Count < (float)this.departmentOfTheInterior.Cities.Count * 1.5f || base.AIEntity.Empire.GetPropertyValue(SimulationProperties.NetEmpireMoney) < 100f || (float)this.departmentOfScience.CurrentTechnologyEraNumber < 2f)
			{
				list.RemoveAll((string y) => y.Contains("Strategic"));
				list.Remove("Luxury14");
				list.Remove("Luxury15");
				list.Remove("Luxury9");
			}
			foreach (KeyValuePair<StaticString, float> keyValuePair in this.aILayer_Research.ResourcesNeededForKaijus)
			{
				list.AddOnce(keyValuePair.Key.ToString());
			}
			using (List<string>.Enumerator enumerator3 = list.GetEnumerator())
			{
				while (enumerator3.MoveNext())
				{
					string text = enumerator3.Current;
					ResourceDefinition resourceDefinition = ResourceDefinition.GetResourceDefinition(text);
					float num3 = 0f;
					bool saveThis = false;
					if (resourceDefinition.ResourceType == ResourceDefinition.Type.Luxury)
					{
						if (this.aILayer_Research.ResourcesNeededForKaijus.TryGetValue(text, out num3) && num3 > 0f)
						{
							saveThis = true;
						}
						else if (this.departmentOfPlanificationAndDevelopment.GetActiveBooster("Booster" + text) == null)
						{
							BoosterDefinition value = database.GetValue("Booster" + text);
							if (value != null)
							{
								num3 = value.Costs[0].GetValue(base.AIEntity.Empire.SimulationObject);
							}
						}
					}
					else
					{
						num3 = 20f + ((float)this.departmentOfScience.CurrentTechnologyEraNumber - 1f) * 5f;
					}
					float num4;
					if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, text, out num4, false))
					{
						num4 = 0f;
					}
					num3 -= num4;
					num3 = Mathf.Ceil(num3);
					TradableResource tradableResource = this.TryGetTradableRessource(text);
					if (num3 >= 1f && tradableResource != null && tradableResource.Quantity >= num3 && !pastTransactions.Any((TradableTransaction T) => T.ReferenceName == text && T.Type == TradableTransactionType.Sellout && T.Turn >= (uint)(turn - 2) && T.EmpireIndex == (uint)this.AIEntity.Empire.Index))
					{
						float priceWithSalesTaxes = TradableResource.GetPriceWithSalesTaxes(text, TradableTransactionType.Buyout, base.AIEntity.Empire, num3);
						float num5 = (num3 <= 10f) ? 1.5f : (1.5f + (num3 - 10f) / 20f);
						if (num >= priceWithSalesTaxes * num5)
						{
							this.queuedRichboyResource = new AILayer_Trade.QueuedRichboyResource(text, num3);
							this.queuedRichboyResource.SaveThis = saveThis;
							break;
						}
					}
				}
			}
		}
		if (this.queuedRichboyResource != null)
		{
			base.AIEntity.GetLayer<AILayer_AccountManager>();
			TradableResource tradableResource2 = this.TryGetTradableRessource(this.queuedRichboyResource.Resource);
			float quantity = Math.Min(10f, this.queuedRichboyResource.RemainingQuantityToBuy);
			float priceWithSalesTaxes2 = TradableResource.GetPriceWithSalesTaxes(this.queuedRichboyResource.Resource, TradableTransactionType.Buyout, base.AIEntity.Empire, quantity);
			if (num >= priceWithSalesTaxes2)
			{
				Ticket ticket;
				base.AIEntity.Empire.PlayerControllers.AI.PostOrder(new OrderBuyoutTradable(base.AIEntity.Empire.Index, tradableResource2.UID, quantity), out ticket, new EventHandler<TicketRaisedEventArgs>(this.RichboyTradingOrder_TicketRaised));
			}
			else
			{
				this.queuedRichboyResource = null;
			}
		}
		return SynchronousJobState.Success;
	}

	private void RichboyTradingOrder_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		if (e.Result == PostOrderResponse.Processed)
		{
			OrderBuyoutTradable orderBuyoutTradable = e.Order as OrderBuyoutTradable;
			this.queuedRichboyResource.RemainingQuantityToBuy -= orderBuyoutTradable.Quantity;
			if (this.queuedRichboyResource.RemainingQuantityToBuy <= 0f)
			{
				if (ResourceDefinition.GetResourceDefinition(this.queuedRichboyResource.Resource).ResourceType == ResourceDefinition.Type.Luxury && !this.queuedRichboyResource.SaveThis)
				{
					OrderBuyoutAndActivateBooster order = new OrderBuyoutAndActivateBooster(base.AIEntity.Empire.Index, "Booster" + this.queuedRichboyResource.Resource, 0UL, false);
					Ticket ticket;
					base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.RichboyBoosterActivate_TicketRaised));
					return;
				}
				this.queuedRichboyResource = null;
			}
			this.SynchronousJob_ProcessRichboyResourceTradingOrder();
			return;
		}
		this.queuedRichboyResource = null;
	}

	private void RichboyBoosterActivate_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		this.queuedRichboyResource = null;
		this.SynchronousJob_ProcessRichboyResourceTradingOrder();
	}

	public static string RegistryPath = string.Empty;

	private Dictionary<StaticString, float> accumulatedResourcesForMarket;

	private List<DecisionResult> affinitiesDecisionResults = new List<DecisionResult>();

	private int currentResourceBuyoutOrder;

	private int currentUnitBuyoutOrder;

	private DepartmentOfScience departmentOfScience;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private global::Game game;

	private List<City> hostCities;

	private AILayer_Trade.QueuedHeroBuyoutOrder queuedHeroBuyoutOrder;

	private List<AILayer_Trade.QueuedResourceBuyoutOrder> queuedResourceBuyoutOrders;

	private List<AILayer_Trade.QueuedUnitBuyoutOrder> queuedUnitBuyoutOrders;

	private IDatabase<ResourceDefinition> resourceDefinitionDatabase;

	private int resourceSelloutCooldown;

	private ISynchronousJobRepositoryAIHelper synchronousJobRepositoryHelper;

	private Dictionary<GameEntityGUID, DecisionResult[]> tradableUnitAffinities;

	private ITradeManagementService tradeManagementService;

	private IAIUnitDesignDataRepository unitDesignDataRepository;

	private IUnitPatternAIHelper unitPatternHelper;

	private IWorldPositionningService worldPositionningService;

	[InfluencedByPersonality]
	private float magicResourceSellingProportion = 0.5f;

	[InfluencedByPersonality]
	private int minimalMagicTimeRange = 4;

	[InfluencedByPersonality]
	private int maximalMagicTimeRange = 10;

	private int privateertick;

	private Army MercsInNeedOfReinforcements;

	private IQuestManagementService questManagementService;

	private bool SaveMoney;

	private DepartmentOfEducation departmentOfEducation;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private DepartmentOfPlanificationAndDevelopment departmentOfPlanificationAndDevelopment;

	private int RichboyResourceTick;

	private AILayer_Trade.QueuedRichboyResource queuedRichboyResource;

	private AILayer_Research aILayer_Research;

	private class QueuedHeroBuyoutOrder
	{
		public EvaluableMessage_HeroNeed Message;
	}

	private class QueuedResourceBuyoutOrder
	{
		public QueuedResourceBuyoutOrder(EvaluableMessage_ResourceNeed message, int indexRessource)
		{
			this.Message = message;
			this.IndexResource = indexRessource;
			this.RemainingQuantityToBuy = message.MissingResources[indexRessource].MissingResourceValue;
			if (message.ExpectedBuyoutUnitPrices != null && indexRessource < message.ExpectedBuyoutUnitPrices.Count)
			{
				this.ExpectedUnitPrice = message.ExpectedBuyoutUnitPrices[indexRessource];
				return;
			}
			this.ExpectedUnitPrice = 0f;
		}

		public float ExpectedUnitPrice;

		public int IndexResource;

		public EvaluableMessage_ResourceNeed Message;

		public float RemainingQuantityToBuy;
	}

	private class QueuedUnitBuyoutOrder
	{
		public QueuedUnitBuyoutOrder(EvaluableMessageWithUnitDesign message)
		{
			this.TryCount = 3;
			this.Message = message;
		}

		public EvaluableMessageWithUnitDesign Message;

		public int TryCount;
	}

	private class QueuedRichboyResource
	{
		public QueuedRichboyResource(string resource, float quantity)
		{
			this.Resource = resource;
			this.RemainingQuantityToBuy = quantity;
			this.SaveThis = false;
		}

		public string Resource;

		public float RemainingQuantityToBuy;

		public bool SaveThis;
	}
}
