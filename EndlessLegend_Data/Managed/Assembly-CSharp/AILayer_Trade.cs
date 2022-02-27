using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
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
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		this.game = (gameService.Game as global::Game);
		Diagnostics.Assert(this.game != null);
		this.synchronousJobRepositoryHelper = AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>();
		Diagnostics.Assert(this.synchronousJobRepositoryHelper != null);
		this.worldPositionningService = gameService.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.unitPatternHelper = AIScheduler.Services.GetService<IUnitPatternAIHelper>();
		this.unitDesignDataRepository = AIScheduler.Services.GetService<IAIUnitDesignDataRepository>();
		ITickableRepositoryAIHelper tickableRepository = AIScheduler.Services.GetService<ITickableRepositoryAIHelper>();
		Diagnostics.Assert(tickableRepository != null);
		tickableRepository.Register(this);
		this.departmentOfScience = base.AIEntity.Empire.GetAgency<DepartmentOfScience>();
		this.departmentOfTheTreasury = base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>();
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
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
		this.tradeManagementService = null;
		this.worldPositionningService = null;
		this.unitPatternHelper = null;
		this.tradableUnitAffinities = null;
		ITickableRepositoryAIHelper service = AIScheduler.Services.GetService<ITickableRepositoryAIHelper>();
		Diagnostics.Assert(service != null);
		service.Unregister(this);
		this.accumulatedResourcesForMarket.Clear();
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
				foreach (KeyValuePair<GameEntityGUID, DecisionResult[]> keyValuePair2 in this.tradableUnitAffinities)
				{
					keyValuePair = keyValuePair2;
					if (list.Find((ITradable match) => match is TradableUnit && (match as TradableUnit).GameEntityGUID == keyValuePair.Key) == null)
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
		ISynchronousJobRepositoryAIHelper service = AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>();
		if (this.departmentOfScience.CanTradeResourcesAndBoosters(false))
		{
			this.FillResourceTradingOrdersQueue();
			if (this.queuedResourceBuyoutOrders.Count > 0)
			{
				service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_ProcessNextQueuedResourceTradingOrder));
			}
			service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_DoMagicSelloutOfProducedResources));
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
		}
		else
		{
			this.queuedHeroBuyoutOrder.Message.SetObtained();
		}
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
				Dictionary<StaticString, float> dictionary2;
				Dictionary<StaticString, float> dictionary = dictionary2 = this.accumulatedResourcesForMarket;
				StaticString key2;
				StaticString key = key2 = resourceDefinition.Name;
				float num3 = dictionary2[key2];
				dictionary[key] = num3 + num2 * num;
			}
		}
		if (--this.resourceSelloutCooldown < 0)
		{
			StaticString staticString = StaticString.Empty;
			float num4 = 1f;
			foreach (KeyValuePair<StaticString, float> keyValuePair in this.accumulatedResourcesForMarket)
			{
				if (keyValuePair.Value > num4)
				{
					num4 = keyValuePair.Value;
					staticString = keyValuePair.Key;
				}
			}
			if (staticString != StaticString.Empty)
			{
				int i = (int)Math.Floor((double)num4);
				Dictionary<StaticString, float> dictionary4;
				Dictionary<StaticString, float> dictionary3 = dictionary4 = this.accumulatedResourcesForMarket;
				StaticString key2;
				StaticString key3 = key2 = staticString;
				float num3 = dictionary4[key2];
				dictionary3[key3] = num3 - (float)i;
				this.resourceSelloutCooldown = UnityEngine.Random.Range(this.minimalMagicTimeRange, this.maximalMagicTimeRange);
				while (i > 10)
				{
					OrderSelloutTradableResource order = new OrderSelloutTradableResource(base.AIEntity.Empire.Index, staticString, 10f, true);
					base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order);
					i -= 10;
				}
				base.AIEntity.Empire.PlayerControllers.AI.PostOrder(new OrderSelloutTradableResource(base.AIEntity.Empire.Index, staticString, (float)i, true));
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
			float num = Math.Min(10f, queuedResourceBuyoutOrder.RemainingQuantityToBuy);
			float num2 = num * queuedResourceBuyoutOrder.ExpectedUnitPrice;
			float priceWithSalesTaxes = TradableResource.GetPriceWithSalesTaxes(missingResource2.ResourceName, TradableTransactionType.Buyout, base.AIEntity.Empire, num);
			if (num2 >= priceWithSalesTaxes || layer.TryMakeUnexpectedImmediateExpense(queuedResourceBuyoutOrder.Message.AccountTag, priceWithSalesTaxes - num2, 0f))
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
			}
			else
			{
				this.ExpectedUnitPrice = 0f;
			}
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
}
