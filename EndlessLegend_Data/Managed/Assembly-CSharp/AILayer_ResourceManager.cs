using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.AI;
using Amplitude.Unity.Framework;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

[PersonalityRegistryPath("AI/MajorEmpire/AIEntity_Empire/AILayer_ResourceManager", new object[]
{

})]
public class AILayer_ResourceManager : AILayer, IXmlSerializable
{
	public override void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		if (num >= 2)
		{
			this.ResourcePolicyMessageID = reader.GetAttribute<ulong>("ResourcePolicyMessageID");
		}
		base.ReadXml(reader);
	}

	public override void WriteXml(XmlWriter writer)
	{
		int num = writer.WriteVersionAttribute(2);
		if (num >= 2)
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
		DepartmentOfScience agency = base.AIEntity.Empire.GetAgency<DepartmentOfScience>();
		if (!agency.CanTradeResourcesAndBoosters(false))
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
			ISynchronousJobRepositoryAIHelper service = AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>();
			service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_SellResources));
		}
	}

	private float GetLuxurySellThreshold(AILayer_ResourceManager.SellInfo sellInfo, float stockValue)
	{
		float propertyValue = base.AIEntity.Empire.GetPropertyValue(SimulationProperties.EmpireScaleFactor);
		float num = 5f * (propertyValue + 1f);
		float num2;
		this.departmentOfTheTreasury.TryGetNetResourceValue(base.AIEntity.Empire, sellInfo.ResourceDefinition.Name, out num2, true);
		if (num2 < 0.5f && stockValue < num)
		{
			return 0f;
		}
		return 2f * num;
	}

	private float GetStrategicSellThreshold(AILayer_ResourceManager.SellInfo sellInfo, float stockValue)
	{
		float averageValue = this.resourcesAIData.GetAverageValue(string.Format("{0}Stock", sellInfo.ResourceDefinition.Name));
		float averageValue2 = this.resourcesAIData.GetAverageValue(string.Format("{0}Income", sellInfo.ResourceDefinition.Name));
		float averageValue3 = this.resourcesAIData.GetAverageValue(string.Format("{0}Expenditure", sellInfo.ResourceDefinition.Name));
		float num = averageValue + 8f * base.AIEntity.Empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier) * (averageValue2 - averageValue3);
		float propertyValue = base.AIEntity.Empire.GetPropertyValue(SimulationProperties.EmpireScaleFactor);
		float b = propertyValue * this.strategicResourceMinimumStockPerCity;
		float num2 = Mathf.Max(stockValue - num, b);
		float averageValue4 = this.resourcesAIData.GetAverageValue(SimulationProperties.BankAccount);
		float averageValue5 = this.resourcesAIData.GetAverageValue(SimulationProperties.NetEmpireMoney);
		float b2 = averageValue4 + 8f * base.AIEntity.Empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier) * averageValue5;
		float quantity = stockValue - num2;
		float priceWithSalesTaxes = TradableResource.GetPriceWithSalesTaxes(sellInfo.ResourceDefinition.Name, TradableTransactionType.Sellout, base.AIEntity.Empire, quantity);
		if (priceWithSalesTaxes <= 1.401298E-45f)
		{
			return float.PositiveInfinity;
		}
		float num3 = priceWithSalesTaxes / Mathf.Max(1f, b2);
		float value = 1f / num3;
		float num4 = Mathf.Clamp01(this.maxDeviationFromResourceMinimumStock);
		return num2 * Mathf.Clamp(value, 1f - num4, 1f + num4);
	}

	private SynchronousJobState SynchronousJob_SellResources()
	{
		for (int i = 0; i < this.sellInfos.Count; i++)
		{
			AILayer_ResourceManager.SellInfo sellInfo = this.sellInfos[i];
			if (sellInfo.AmountToSell >= 1f)
			{
				float amountToSell = sellInfo.AmountToSell;
				if (this.departmentOfTheTreasury.IsTransferOfResourcePossible(base.AIEntity.Empire, sellInfo.ResourceDefinition.Name, ref amountToSell))
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
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		this.departmentOfTheTreasury = base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>();
		this.resourceDatabase = Databases.GetDatabase<ResourceDefinition>(true);
		IDatabase<AIBoosterManagerDefinition> boosterManagerDatabase = Databases.GetDatabase<AIBoosterManagerDefinition>(false);
		foreach (AIBoosterManagerDefinition boosterManagerDefinition in boosterManagerDatabase)
		{
			if (boosterManagerDefinition.CheckPrerequisites(base.AIEntity.Empire))
			{
				try
				{
					string assemblyQualifiedName = boosterManagerDefinition.BoosterManagerAssemblyQualifiedName;
					if (!string.IsNullOrEmpty(assemblyQualifiedName))
					{
						Type assemblyQualifiedType = Type.GetType(assemblyQualifiedName);
						if (assemblyQualifiedType != null)
						{
							AIBoosterManager boosterManager = Activator.CreateInstance(assemblyQualifiedType) as AIBoosterManager;
							if (boosterManager != null)
							{
								this.boosterManagers.Add(boosterManager);
							}
						}
					}
				}
				catch
				{
				}
			}
		}
		for (int index = 0; index < this.boosterManagers.Count; index++)
		{
			this.boosterManagers[index].Initialize(base.AIEntity);
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
		for (int index = 0; index < this.boosterManagers.Count; index++)
		{
			this.boosterManagers[index].Load();
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
		AILayer_AccountManager layer = base.AIEntity.GetLayer<AILayer_AccountManager>();
		float num;
		float num2;
		if (layer.TryGetAccountInfos("Military", "EmpireMoney", out num, out num2))
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
				IUnitDesignAIHelper service = AIScheduler.Services.GetService<IUnitDesignAIHelper>();
				float num3 = service.ComputeResourceAvailabilityByUnit(base.AIEntity.Empire, resourceDefinition);
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

	private List<AILayer_ResourceManager.SellInfo> sellInfos = new List<AILayer_ResourceManager.SellInfo>();

	private AIData resourcesAIData;

	[InfluencedByPersonality]
	private float strategicResourceMinimumStockPerCity = 5f;

	[InfluencedByPersonality]
	private float maxDeviationFromResourceMinimumStock = 0.5f;

	private List<AIBoosterManager> boosterManagers = new List<AIBoosterManager>();

	private DepartmentOfTheInterior departmentOfTheInterior;

	private IDatabase<ResourceDefinition> resourceDatabase;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	[InfluencedByPersonality]
	private float moneyFactor = 0.5f;

	[InfluencedByPersonality]
	private float productionFactor = 10f;

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
