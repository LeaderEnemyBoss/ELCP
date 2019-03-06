using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Amplitude;
using Amplitude.Unity.AI;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

[PersonalityRegistryPath("AI/MajorEmpire/AIEntity_Empire/AILayer_ResourceManager", new object[]
{

})]
public class AILayer_ResourceManager : AILayer, IXmlSerializable
{
	public AILayer_ResourceManager()
	{
		this.sellInfos = new List<AILayer_ResourceManager.SellInfo>();
		this.strategicResourceMinimumStockPerCity = 5f;
		this.maxDeviationFromResourceMinimumStock = 0.5f;
		this.boosterManagers = new List<AIBoosterManager>();
		this.moneyFactor = 0.5f;
		this.productionFactor = 10f;
		this.BoostersInUse = new List<string>();
	}

	public override void ReadXml(XmlReader reader)
	{
		if (reader.ReadVersionAttribute() >= 2)
		{
			this.ResourcePolicyMessageID = reader.GetAttribute<ulong>("ResourcePolicyMessageID");
		}
		base.ReadXml(reader);
	}

	public override void WriteXml(XmlWriter writer)
	{
		if (writer.WriteVersionAttribute(2) >= 2)
		{
			writer.WriteAttributeString<ulong>("ResourcePolicyMessageID", this.ResourcePolicyMessageID);
		}
		base.WriteXml(writer);
	}

	private void InitializeResourceSell()
	{
		this.resourcesAIData = new AIData(Mathf.RoundToInt(8f * base.AIEntity.Empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier)));
		foreach (ResourceDefinition resourceDefinition in this.resourceDatabase)
		{
			if (resourceDefinition.ResourceType == ResourceDefinition.Type.Strategic || resourceDefinition.ResourceType == ResourceDefinition.Type.Luxury)
			{
				this.sellInfos.Add(new AILayer_ResourceManager.SellInfo(resourceDefinition));
			}
		}
	}

	private void EvaluateResourceSellNeeds()
	{
		this.resourcesAIData.RegisterValue(SimulationProperties.NetEmpireMoney, base.AIEntity.Empire.GetPropertyValue(SimulationProperties.NetEmpireMoney));
		this.resourcesAIData.RegisterValue(SimulationProperties.BankAccount, base.AIEntity.Empire.GetPropertyValue(SimulationProperties.BankAccount));
		for (int i = 0; i < this.sellInfos.Count; i++)
		{
			AILayer_ResourceManager.SellInfo sellInfo = this.sellInfos[i];
			float value;
			if (this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire, sellInfo.ResourceDefinition.Name, out value, true))
			{
				this.resourcesAIData.RegisterValue(string.Format("{0}Stock", sellInfo.ResourceDefinition.Name), value);
			}
			if (this.departmentOfTheTreasury.TryGetNetResourceValue(base.AIEntity.Empire, sellInfo.ResourceDefinition.Name, out value, true))
			{
				this.resourcesAIData.RegisterValue(string.Format("{0}Income", sellInfo.ResourceDefinition.Name), value);
			}
			this.resourcesAIData.RegisterValue(string.Format("{0}Expenditure", sellInfo.ResourceDefinition.Name), sellInfo.NextTurnSoldAmount);
		}
	}

	private void ExecuteResourceSellNeeds()
	{
		if (!base.AIEntity.Empire.GetAgency<DepartmentOfScience>().CanTradeResourcesAndBoosters(false))
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < this.sellInfos.Count; i++)
		{
			AILayer_ResourceManager.SellInfo sellInfo = this.sellInfos[i];
			sellInfo.AmountToSell = 0f;
			sellInfo.NextTurnSoldAmount = 0f;
			float num;
			if (this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire, sellInfo.ResourceDefinition.Name, out num, true))
			{
				float num2 = float.PositiveInfinity;
				if (sellInfo.ResourceDefinition.ResourceType == ResourceDefinition.Type.Strategic)
				{
					num2 = this.GetStrategicSellThreshold(sellInfo, num);
				}
				else if (sellInfo.ResourceDefinition.ResourceType == ResourceDefinition.Type.Luxury)
				{
					num2 = this.GetLuxurySellThreshold(sellInfo, num);
				}
				float num3 = Mathf.Max(0f, num - num2);
				if (num3 >= 1f)
				{
					sellInfo.AmountToSell = num3;
					flag = true;
				}
			}
		}
		if (flag)
		{
			AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>().RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_SellResources));
		}
	}

	private float GetLuxurySellThreshold(AILayer_ResourceManager.SellInfo sellInfo, float stockValue)
	{
		float propertyValue = base.AIEntity.Empire.GetPropertyValue(SimulationProperties.EmpireScaleFactor);
		float num = 5f * (propertyValue + 1f);
		float num2 = 0f;
		if (this.aILayer_Research.ResourcesNeededForKaijus.TryGetValue(sellInfo.ResourceDefinition.Name, out num2))
		{
			return 1.5f * num + num2;
		}
		float num3;
		this.departmentOfTheTreasury.TryGetNetResourceValue(base.AIEntity.Empire, sellInfo.ResourceDefinition.Name, out num3, true);
		if (num3 < 0.5f && stockValue < num)
		{
			if (this.departmentOfScience.CanTradeResourcesAndBoosters(false))
			{
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
					"Luxury1",
					"Luxury9",
					"Luxury15",
					"Luxury14"
				};
				float num4 = 0f;
				foreach (City city in this.departmentOfTheInterior.Cities)
				{
					num4 += city.GetPropertyValue(SimulationProperties.OverrallTradeRoutesCityDustIncome);
				}
				if (num4 < 50f)
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
				float num5;
				if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpireMoney, out num5, false))
				{
					num5 = 0f;
				}
				if (num5 < 600f || (float)this.departmentOfEducation.Heroes.Count < (float)this.departmentOfTheInterior.Cities.Count * 1.5f || base.AIEntity.Empire.GetPropertyValue(SimulationProperties.NetEmpireMoney) < 10f)
				{
					list.Remove("Luxury14");
					list.Remove("Luxury15");
					list.Remove("Luxury9");
				}
				if (list.Contains(sellInfo.ResourceDefinition.Name))
				{
					float num6 = Mathf.Ceil(num - stockValue);
					TradableResource tradableResource = this.TryGetTradableRessource(sellInfo.ResourceDefinition.Name);
					if (tradableResource != null && tradableResource.Quantity >= num6)
					{
						float priceWithSalesTaxes = TradableResource.GetPriceWithSalesTaxes(sellInfo.ResourceDefinition.Name, TradableTransactionType.Buyout, base.AIEntity.Empire, num6);
						float num7 = (num6 <= 10f) ? 2.5f : (2.5f + (num6 - 10f) / 20f);
						if (num5 >= priceWithSalesTaxes * num7)
						{
							return 1.5f * num;
						}
					}
				}
			}
			return 0f;
		}
		return 1.5f * num;
	}

	private float GetStrategicSellThreshold(AILayer_ResourceManager.SellInfo sellInfo, float stockValue)
	{
		float num;
		if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpireMoney, out num, false))
		{
			num = 0f;
		}
		bool flag = false;
		if (num >= 800f && (float)this.departmentOfEducation.Heroes.Count >= (float)this.departmentOfTheInterior.Cities.Count * 1.5f && base.AIEntity.Empire.GetPropertyValue(SimulationProperties.NetEmpireMoney) >= 10f && this.departmentOfScience.CurrentTechnologyEraNumber > 1)
		{
			flag = true;
		}
		float averageValue = this.resourcesAIData.GetAverageValue(string.Format("{0}Stock", sellInfo.ResourceDefinition.Name));
		float averageValue2 = this.resourcesAIData.GetAverageValue(string.Format("{0}Income", sellInfo.ResourceDefinition.Name));
		float averageValue3 = this.resourcesAIData.GetAverageValue(string.Format("{0}Expenditure", sellInfo.ResourceDefinition.Name));
		float num2 = averageValue + 8f * base.AIEntity.Empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier) * (averageValue2 - averageValue3);
		float b = base.AIEntity.Empire.GetPropertyValue(SimulationProperties.EmpireScaleFactor) * this.strategicResourceMinimumStockPerCity;
		float num3 = Mathf.Max(stockValue - num2, b);
		float averageValue4 = this.resourcesAIData.GetAverageValue(SimulationProperties.BankAccount);
		float averageValue5 = this.resourcesAIData.GetAverageValue(SimulationProperties.NetEmpireMoney);
		float b2 = averageValue4 + 8f * base.AIEntity.Empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier) * averageValue5;
		float quantity = stockValue - num3;
		float priceWithSalesTaxes = TradableResource.GetPriceWithSalesTaxes(sellInfo.ResourceDefinition.Name, TradableTransactionType.Sellout, base.AIEntity.Empire, quantity);
		if (priceWithSalesTaxes <= 1.401298E-45f)
		{
			return float.PositiveInfinity;
		}
		float num4 = priceWithSalesTaxes / Mathf.Max(1f, b2);
		float value = 1f / num4;
		float num5 = Mathf.Clamp01(this.maxDeviationFromResourceMinimumStock);
		float num6 = num3 * Mathf.Clamp(value, 1f - num5, 1f + num5);
		if (flag)
		{
			float b3 = (20f + ((float)this.departmentOfScience.CurrentTechnologyEraNumber - 1f) * 5f) * 1.5f;
			num6 = Mathf.Max(num6, b3);
		}
		return num6;
	}

	private SynchronousJobState SynchronousJob_SellResources()
	{
		IGameService service = Services.GetService<IGameService>();
		int turn = (service.Game as global::Game).Turn;
		ReadOnlyCollection<TradableTransaction> pastTransactions = this.tradeManagementService.GetPastTransactions();
		for (int i = 0; i < this.sellInfos.Count; i++)
		{
			AILayer_ResourceManager.SellInfo sellInfo = this.sellInfos[i];
			if (sellInfo.AmountToSell >= 1f && !pastTransactions.Any((TradableTransaction T) => T.ReferenceName == sellInfo.ResourceDefinition.Name && T.Type == TradableTransactionType.Buyout && T.Turn >= (uint)(turn - 5) && T.EmpireIndex == (uint)this.AIEntity.Empire.Index))
			{
				float amountToSell = sellInfo.AmountToSell;
				if (this.departmentOfTheTreasury.IsTransferOfResourcePossible(base.AIEntity.Empire, sellInfo.ResourceDefinition.Name, ref amountToSell) && amountToSell >= 1f)
				{
					OrderSelloutTradableResource order = new OrderSelloutTradableResource(base.AIEntity.Empire.Index, sellInfo.ResourceDefinition.Name, amountToSell, false);
					Ticket ticket;
					base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnSelloutOrder));
				}
			}
		}
		return SynchronousJobState.Success;
	}

	private void OnSelloutOrder(object sender, TicketRaisedEventArgs ticketRaisedEventArgs)
	{
		if (ticketRaisedEventArgs.Result != PostOrderResponse.Processed)
		{
			return;
		}
		OrderSelloutTradableResource orderSelloutTradableResource = ticketRaisedEventArgs.Order as OrderSelloutTradableResource;
		Diagnostics.Assert(orderSelloutTradableResource != null);
		AILayer_ResourceManager.SellInfo sellInfo = this.sellInfos.Find((AILayer_ResourceManager.SellInfo match) => match.ResourceDefinition.Name == orderSelloutTradableResource.ResourceName);
		Diagnostics.Assert(sellInfo != null);
		sellInfo.AmountToSell = 0f;
		sellInfo.NextTurnSoldAmount += orderSelloutTradableResource.Quantity;
		sellInfo.TotalSoldAmount += orderSelloutTradableResource.Quantity;
	}

	public ulong ResourcePolicyMessageID { get; private set; }

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		this.aILayer_Research = base.AIEntity.GetLayer<AILayer_Research>();
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		this.departmentOfTheTreasury = base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>();
		this.departmentOfEducation = base.AIEntity.Empire.GetAgency<DepartmentOfEducation>();
		this.departmentOfForeignAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		this.departmentOfScience = base.AIEntity.Empire.GetAgency<DepartmentOfScience>();
		IGameService service = Services.GetService<IGameService>();
		this.tradeManagementService = (service.Game as global::Game).Services.GetService<ITradeManagementService>();
		this.resourceDatabase = Databases.GetDatabase<ResourceDefinition>(true);
		foreach (AIBoosterManagerDefinition aiboosterManagerDefinition in Databases.GetDatabase<AIBoosterManagerDefinition>(false))
		{
			if (aiboosterManagerDefinition.CheckPrerequisites(base.AIEntity.Empire))
			{
				try
				{
					string boosterManagerAssemblyQualifiedName = aiboosterManagerDefinition.BoosterManagerAssemblyQualifiedName;
					if (!string.IsNullOrEmpty(boosterManagerAssemblyQualifiedName))
					{
						Type type = Type.GetType(boosterManagerAssemblyQualifiedName);
						if (type != null)
						{
							AIBoosterManager aiboosterManager = Activator.CreateInstance(type) as AIBoosterManager;
							if (aiboosterManager != null)
							{
								this.boosterManagers.Add(aiboosterManager);
							}
						}
					}
				}
				catch
				{
				}
			}
		}
		for (int i = 0; i < this.boosterManagers.Count; i++)
		{
			this.boosterManagers[i].Initialize(base.AIEntity);
		}
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_ResourceManager_CreateLocalNeedsPass", new AIEntity.AIAction(this.CreateLocalNeeds), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.EvaluateNeeds.ToString(), "AILayer_ResourceManager_EvaluateNeedsPass", new AIEntity.AIAction(this.EvaluateNeeds), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.ExecuteNeeds.ToString(), "AILayer_ResourceManager_ExecuteNeedsPass", new AIEntity.AIAction(this.ExecuteNeeds), this, new StaticString[0]);
		this.InitializeResourceSell();
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public override IEnumerator Load()
	{
		yield return base.Load();
		for (int i = 0; i < this.boosterManagers.Count; i++)
		{
			this.boosterManagers[i].Load();
		}
		yield break;
	}

	public override void Release()
	{
		base.Release();
		for (int i = 0; i < this.boosterManagers.Count; i++)
		{
			this.boosterManagers[i].Release();
		}
		this.boosterManagers.Clear();
		this.resourceDatabase = null;
		this.departmentOfTheInterior = null;
		this.departmentOfTheTreasury = null;
		this.departmentOfEducation = null;
		this.departmentOfForeignAffairs = null;
		this.departmentOfScience = null;
		this.tradeManagementService = null;
		this.BoostersInUse = null;
		this.aILayer_Research = null;
	}

	protected override void CreateLocalNeeds(StaticString context, StaticString pass)
	{
		base.CreateLocalNeeds(context, pass);
		this.RefreshResourcePolicy();
		for (int i = 0; i < this.boosterManagers.Count; i++)
		{
			this.boosterManagers[i].CreateLocals();
		}
	}

	protected override void EvaluateNeeds(StaticString context, StaticString pass)
	{
		base.EvaluateNeeds(context, pass);
		this.EvaluateResourceSellNeeds();
		for (int i = 0; i < this.boosterManagers.Count; i++)
		{
			this.boosterManagers[i].Evaluate();
		}
	}

	protected override void ExecuteNeeds(StaticString context, StaticString pass)
	{
		base.ExecuteNeeds(context, pass);
		this.ExecuteResourceSellNeeds();
		for (int i = 0; i < this.boosterManagers.Count; i++)
		{
			this.boosterManagers[i].Execute();
		}
	}

	private void RefreshResourcePolicy()
	{
		if (this.ResourcePolicyMessageID == 0UL)
		{
			ResourcePolicyMessage resourcePolicyMessage = new ResourcePolicyMessage();
			resourcePolicyMessage.TimeOut = int.MinValue;
			this.ResourcePolicyMessageID = base.AIEntity.AIPlayer.Blackboard.AddMessage(resourcePolicyMessage);
		}
		ResourcePolicyMessage resourcePolicyMessage2 = base.AIEntity.AIPlayer.Blackboard.GetMessage(this.ResourcePolicyMessageID) as ResourcePolicyMessage;
		List<AIScoring> list = new List<AIScoring>();
		AIScoring aiscoring = new AIScoring();
		aiscoring.Name = DepartmentOfTheTreasury.Resources.Production;
		aiscoring.Value = 50f;
		if (this.departmentOfTheInterior.Cities.Count > 0)
		{
			aiscoring.Value = base.AIEntity.Empire.GetPropertyValue(SimulationProperties.NetCityProduction);
			aiscoring.Value /= (float)this.departmentOfTheInterior.Cities.Count;
			aiscoring.Value *= this.productionFactor;
		}
		list.Add(aiscoring);
		AIScoring aiscoring2 = new AIScoring();
		aiscoring2.Name = DepartmentOfTheTreasury.Resources.EmpireMoney;
		aiscoring2.Value = 50f;
		float propertyValue = base.AIEntity.Empire.GetPropertyValue(SimulationProperties.NetEmpireMoney);
		float num;
		float num2;
		if (base.AIEntity.GetLayer<AILayer_AccountManager>().TryGetAccountInfos("Military", "EmpireMoney", out num, out num2))
		{
			if (propertyValue > 0f)
			{
				num += propertyValue * num2;
			}
			else
			{
				num += propertyValue;
			}
			aiscoring2.Value = num * this.moneyFactor;
		}
		list.Add(aiscoring2);
		foreach (ResourceDefinition resourceDefinition in this.resourceDatabase)
		{
			if (resourceDefinition.ResourceType == ResourceDefinition.Type.Strategic)
			{
				Diagnostics.Assert(AIScheduler.Services != null);
				float num3 = AIScheduler.Services.GetService<IUnitDesignAIHelper>().ComputeResourceAvailabilityByUnit(base.AIEntity.Empire, resourceDefinition);
				if (num3 > 0f)
				{
					list.Add(new AIScoring
					{
						Name = resourceDefinition.Name,
						Value = num3
					});
				}
			}
		}
		resourcePolicyMessage2.ResourcePolicyForUnitDesign = list;
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

	private List<AILayer_ResourceManager.SellInfo> sellInfos;

	private AIData resourcesAIData;

	[InfluencedByPersonality]
	private float strategicResourceMinimumStockPerCity;

	[InfluencedByPersonality]
	private float maxDeviationFromResourceMinimumStock;

	private List<AIBoosterManager> boosterManagers;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private IDatabase<ResourceDefinition> resourceDatabase;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	[InfluencedByPersonality]
	private float moneyFactor;

	[InfluencedByPersonality]
	private float productionFactor;

	public List<string> BoostersInUse;

	private DepartmentOfScience departmentOfScience;

	private DepartmentOfEducation departmentOfEducation;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private ITradeManagementService tradeManagementService;

	private AILayer_Research aILayer_Research;

	private class SellInfo
	{
		public SellInfo(ResourceDefinition definition)
		{
			this.ResourceDefinition = definition;
		}

		public ResourceDefinition ResourceDefinition { get; private set; }

		public float AmountToSell;

		public float NextTurnSoldAmount;

		public float TotalSoldAmount;
	}
}
