using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Amplitude;
using Amplitude.Extensions;
using Amplitude.Path;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Game.Orders;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

[OrderProcessor(typeof(OrderBuyoutAndActivateBooster), "BuyoutAndActivateBooster")]
[OrderProcessor(typeof(OrderBuyoutTradableUnit), "BuyoutTradable")]
[OrderProcessor(typeof(OrderBuyoutAndActivateBoosterByInfiltration), "BuyoutAndActivateBoosterByInfiltration")]
[OrderProcessor(typeof(OrderSelloutTradableBooster), "SelloutTradableBooster")]
[OrderProcessor(typeof(OrderSelloutTradableHero), "SelloutTradableHero")]
[OrderProcessor(typeof(OrderSelloutTradableResource), "SelloutTradableResource")]
[OrderProcessor(typeof(OrderSelloutTradableUnits), "SelloutTradableUnits")]
[OrderProcessor(typeof(OrderChangeEmpirePlan), "ChangeEmpirePlan")]
[OrderProcessor(typeof(OrderBuyoutTradable), "BuyoutTradable")]
[OrderProcessor(typeof(OrderBuyoutAndActivateBoosterThroughArmy), "BuyoutAndActivateBoosterThroughArmy")]
[OrderProcessor(typeof(OrderSwitchCheatMode), "SwitchCheatMode")]
public class DepartmentOfPlanificationAndDevelopment : Agency, IXmlSerializable, IEmpirePlanProvider
{
	public DepartmentOfPlanificationAndDevelopment(global::Empire empire) : base(empire)
	{
		this.TurnWhenTradeRoutesWereLastCreated = -1;
	}

	public event EventHandler<EventArgs> BoosterActivated;

	public event EventHandler<BoosterCollectionChangeEventArgs> BoosterCollectionChange;

	public event EventHandler<ConstructionChangeEventArgs> EmpirePlanQueueChanged;

	public event EventHandler<ConstructibleElementEventArgs> EmpirePlanUnlocked;

	public event EventHandler<TradeRouteChangedEventArgs> TradeRouteChanged;

	public static int GetBoosterDurationWithBonus(Amplitude.Unity.Game.Empire empire, int boosterDuration)
	{
		float propertyValue = empire.GetPropertyValue(SimulationProperties.BoosterDurationMultiplier);
		int a = Mathf.RoundToInt((float)boosterDuration * propertyValue);
		return Mathf.Max(a, 1);
	}

	public static int GetBoosterDurationWithBonus(Amplitude.Unity.Game.Empire empire, SimulationObject context, BoosterDefinition boosterDefinition)
	{
		if (boosterDefinition == null)
		{
			throw new ArgumentNullException("boosterDefinition");
		}
		float duration = boosterDefinition.GetDuration(context);
		float propertyValue = empire.GetPropertyValue(SimulationProperties.BoosterDurationMultiplier);
		int a = Mathf.RoundToInt(duration * propertyValue);
		return Mathf.Max(a, 1);
	}

	public void ActivateBooster(BoosterDefinition boosterDefinition, GameEntityGUID boosterGuid, int boosterDuration, SimulationObjectWrapper context, GameEntityGUID targetGuid, int instigatorEmpireIndex)
	{
		DepartmentOfEducation agency = base.Empire.GetAgency<DepartmentOfEducation>();
		Booster booster = this.boosters.Values.FirstOrDefault((Booster match) => match.BoosterDefinition.Name == boosterDefinition.Name && match.Context == context && match.InstigatorEmpireIndex == instigatorEmpireIndex);
		if (booster != null)
		{
			booster.Duration = boosterDuration;
			booster.Activate();
			agency.DestroyVaultItem(boosterGuid);
		}
		else
		{
			booster = new Booster(boosterGuid, boosterDefinition, base.Empire as global::Empire, context, targetGuid, boosterDuration, instigatorEmpireIndex);
			booster.Activate();
			agency.DestroyVaultItem(boosterGuid);
			if (booster.BoosterDefinition.BoosterType != BoosterDefinition.Type.Instant)
			{
				this.gameEntityRepositoryService.Register(booster);
				this.boosters.Add(booster.GUID, booster);
				this.OnBoosterCollectionChange(new BoosterCollectionChangeEventArgs(BoosterCollectionChangeAction.Add, booster));
				if (this.eventService != null && base.Empire.Index == instigatorEmpireIndex)
				{
					this.eventService.Notify(new EventBoosterStarted(base.Empire, booster));
				}
			}
		}
		StaticString boosterActivationsPropertyName = SimulationProperties.GetBoosterActivationsPropertyName(boosterDefinition);
		if (base.Empire.SimulationObject.ContainsProperty(boosterActivationsPropertyName))
		{
			float propertyBaseValue = base.Empire.SimulationObject.GetPropertyBaseValue(boosterActivationsPropertyName);
			base.Empire.SimulationObject.SetPropertyBaseValue(boosterActivationsPropertyName, propertyBaseValue + 1f);
		}
		else
		{
			Diagnostics.LogWarning("Booster {0} does not have an activations counter property named '{1}'.", new object[]
			{
				boosterDefinition.Name,
				boosterActivationsPropertyName
			});
		}
	}

	public int ComputePlanDepth(StaticString empirePlanClass)
	{
		if (StaticString.IsNullOrEmpty(empirePlanClass))
		{
			throw new ArgumentNullException("empirePlanClass");
		}
		Diagnostics.Assert(this.empirePlanDefinitionsByClass != null);
		if (!this.empirePlanDefinitionsByClass.ContainsKey(empirePlanClass))
		{
			return -1;
		}
		EmpirePlanDefinition[] array = this.empirePlanDefinitionsByClass[empirePlanClass];
		if (array == null)
		{
			return -1;
		}
		return array.Max((EmpirePlanDefinition empirePlan) => empirePlan.EmpirePlanLevel);
	}

	public int CountActiveBoosters(Predicate<Booster> predicate = null)
	{
		int num = 0;
		if (this.boosters != null)
		{
			num += this.boosters.Values.Count((Booster booster) => predicate == null || predicate(booster));
		}
		return num;
	}

	public int CountBoosters(Predicate<BoosterDefinition> predicate = null)
	{
		DepartmentOfEducation agency = base.Empire.GetAgency<DepartmentOfEducation>();
		Diagnostics.Assert(agency != null);
		IEnumerable<VaultItem> vaultItems = agency.GetVaultItems((VaultItem match) => match != null && match.Constructible is BoosterDefinition);
		if (vaultItems == null)
		{
			return 0;
		}
		int num = 0;
		foreach (VaultItem vaultItem in vaultItems)
		{
			Diagnostics.Assert(vaultItem != null);
			BoosterDefinition boosterDefinition = vaultItem.Constructible as BoosterDefinition;
			Diagnostics.Assert(boosterDefinition != null);
			if (predicate == null || predicate(boosterDefinition))
			{
				num++;
			}
		}
		return num;
	}

	public EmpirePlanDefinition GetProjectedEmpirePlanDefinition(StaticString empirePlanClass)
	{
		if (StaticString.IsNullOrEmpty(empirePlanClass))
		{
			throw new ArgumentNullException("empirePlanClass");
		}
		Diagnostics.Assert(this.currentEmpirePlanDefinitionByClass != null);
		if (!this.currentEmpirePlanDefinitionByClass.ContainsKey(empirePlanClass))
		{
			return null;
		}
		if (!this.IsEmpirePlanChoiceTurn)
		{
			throw new Exception("The method must not be called if it's not the empire plan choice turn.");
		}
		ReadOnlyCollection<Construction> pendingConstructions = this.empirePlanQueue.PendingConstructions;
		if (pendingConstructions != null)
		{
			for (int i = 0; i < pendingConstructions.Count; i++)
			{
				EmpirePlanDefinition empirePlanDefinition = pendingConstructions[i].ConstructibleElement as EmpirePlanDefinition;
				Diagnostics.Assert(empirePlanDefinition != null);
				if (empirePlanDefinition.EmpirePlanClass == empirePlanClass)
				{
					return empirePlanDefinition;
				}
			}
		}
		return this.GetEmpirePlanDefinition(empirePlanClass, 0);
	}

	public EmpirePlanDefinition GetCurrentEmpirePlanDefinition(StaticString empirePlanClass)
	{
		if (StaticString.IsNullOrEmpty(empirePlanClass))
		{
			throw new ArgumentNullException("empirePlanClass");
		}
		Diagnostics.Assert(this.currentEmpirePlanDefinitionByClass != null);
		if (!this.currentEmpirePlanDefinitionByClass.ContainsKey(empirePlanClass))
		{
			return null;
		}
		return this.currentEmpirePlanDefinitionByClass[empirePlanClass];
	}

	public EmpirePlanDefinition GetCurrentProjectedEmpirePlanDefinition(StaticString empirePlanClass)
	{
		if (StaticString.IsNullOrEmpty(empirePlanClass))
		{
			throw new ArgumentNullException("empirePlanClass");
		}
		Diagnostics.Assert(this.currentEmpirePlanDefinitionByClass != null);
		if (!this.currentEmpirePlanDefinitionByClass.ContainsKey(empirePlanClass))
		{
			return null;
		}
		if (this.IsEmpirePlanChoiceTurn)
		{
			ReadOnlyCollection<Construction> pendingConstructions = this.empirePlanQueue.PendingConstructions;
			if (pendingConstructions != null)
			{
				for (int i = 0; i < pendingConstructions.Count; i++)
				{
					EmpirePlanDefinition empirePlanDefinition = pendingConstructions[i].ConstructibleElement as EmpirePlanDefinition;
					Diagnostics.Assert(empirePlanDefinition != null);
					if (empirePlanDefinition.EmpirePlanClass == empirePlanClass)
					{
						return empirePlanDefinition;
					}
				}
			}
		}
		return this.currentEmpirePlanDefinitionByClass[empirePlanClass];
	}

	public EmpirePlanDefinition GetProjectedOrCurrentEmpirePlanDefinition(StaticString empirePlanClass)
	{
		if (StaticString.IsNullOrEmpty(empirePlanClass))
		{
			throw new ArgumentNullException("empirePlanClass");
		}
		Diagnostics.Assert(this.currentEmpirePlanDefinitionByClass != null);
		if (!this.currentEmpirePlanDefinitionByClass.ContainsKey(empirePlanClass))
		{
			return null;
		}
		if (this.IsEmpirePlanChoiceTurn)
		{
			return this.GetProjectedEmpirePlanDefinition(empirePlanClass);
		}
		return this.GetCurrentEmpirePlanDefinition(empirePlanClass);
	}

	public float GetCurrentEmpirePlanInvestment(StaticString resourceName, StaticString empirePlanClass = null)
	{
		float num = 0f;
		for (int i = 0; i < this.empirePlanQueue.PendingConstructions.Count; i++)
		{
			Construction construction = this.empirePlanQueue.PendingConstructions[i];
			Diagnostics.Assert(construction != null);
			if (construction.ConstructibleElement != null && construction.CurrentConstructionStock != null)
			{
				if (!(empirePlanClass != null) || !(construction.ConstructibleElement.Category != empirePlanClass))
				{
					for (int j = 0; j < construction.CurrentConstructionStock.Length; j++)
					{
						ConstructionResourceStock constructionResourceStock = construction.CurrentConstructionStock[j];
						if (!(constructionResourceStock.PropertyName != resourceName))
						{
							num += constructionResourceStock.Stock;
						}
					}
				}
			}
		}
		return num;
	}

	public int GetEmpirePlanAvailableLevel(StaticString empirePlanClass)
	{
		int num = this.ComputePlanDepth(empirePlanClass);
		for (int i = 0; i <= num; i++)
		{
			EmpirePlanDefinition empirePlanDefinition = this.GetEmpirePlanDefinition(empirePlanClass, i);
			Diagnostics.Assert(empirePlanDefinition != null);
			if (!DepartmentOfTheTreasury.CheckConstructiblePrerequisites(base.Empire, empirePlanDefinition, new string[0]))
			{
				return i - 1;
			}
		}
		return num;
	}

	public StaticString[] GetEmpirePlanClasses()
	{
		Diagnostics.Assert(this.empirePlanDefinitionsByClass != null);
		return this.empirePlanDefinitionsByClass.Keys.ToArray<StaticString>();
	}

	public EmpirePlanDefinition GetEmpirePlanDefinition(StaticString empirePlanClass, int level)
	{
		if (StaticString.IsNullOrEmpty(empirePlanClass))
		{
			throw new ArgumentNullException("empirePlanClass");
		}
		Diagnostics.Assert(this.empirePlanDefinitionsByClass != null);
		if (!this.empirePlanDefinitionsByClass.ContainsKey(empirePlanClass))
		{
			return null;
		}
		EmpirePlanDefinition[] array = this.empirePlanDefinitionsByClass[empirePlanClass];
		if (array == null)
		{
			return null;
		}
		return array.FirstOrDefault((EmpirePlanDefinition planDefinition) => planDefinition.EmpirePlanLevel == level);
	}

	public EmpirePlanDefinition[] GetEmpirePlanDefinitionByClass(StaticString empirePlanClass)
	{
		if (StaticString.IsNullOrEmpty(empirePlanClass))
		{
			throw new ArgumentNullException("empirePlanClass");
		}
		Diagnostics.Assert(this.empirePlanDefinitionsByClass != null);
		if (!this.empirePlanDefinitionsByClass.ContainsKey(empirePlanClass))
		{
			return null;
		}
		return this.empirePlanDefinitionsByClass[empirePlanClass];
	}

	public float GetTradeRouteIncomeAmount(StaticString propertyName, MajorEmpire opponent = null)
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		Diagnostics.Assert(service.Game != null);
		Diagnostics.Assert(service.Game is global::Game);
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		if (service2 == null)
		{
			return 0f;
		}
		DepartmentOfTheInterior agency = base.Empire.GetAgency<DepartmentOfTheInterior>();
		Diagnostics.Assert(agency != null);
		float num = 0f;
		for (int i = 0; i < agency.Cities.Count; i++)
		{
			City city = agency.Cities[i];
			for (int j = 0; j < city.TradeRoutes.Count; j++)
			{
				TradeRoute tradeRoute = city.TradeRoutes[j];
				if (tradeRoute != null && tradeRoute.SimulationObject != null)
				{
					Region region = service2.GetRegion((int)tradeRoute.ToRegionIndex);
					if (region != null)
					{
						if (region.City != null && region.City.Empire != null)
						{
							Diagnostics.Assert(region.City != null && region.City.Empire != null);
							if (opponent == null || region.City.Empire.Index == opponent.Index)
							{
								num += tradeRoute.SimulationObject.GetPropertyValue(propertyName);
							}
						}
					}
				}
			}
		}
		return num;
	}

	public EmpirePlanDefinition[] GetUnlockedEmpirePlan()
	{
		Diagnostics.Assert(this.currentEmpirePlanDefinitionByClass != null);
		List<EmpirePlanDefinition> list = new List<EmpirePlanDefinition>();
		list.AddRange(this.currentEmpirePlanDefinitionByClass.Values);
		if (this.IsEmpirePlanChoiceTurn)
		{
			ReadOnlyCollection<Construction> pendingConstructions = this.empirePlanQueue.PendingConstructions;
			if (pendingConstructions != null)
			{
				for (int i = 0; i < pendingConstructions.Count; i++)
				{
					EmpirePlanDefinition empirePlanDefinition = pendingConstructions[i].ConstructibleElement as EmpirePlanDefinition;
					Diagnostics.Assert(empirePlanDefinition != null);
					list.RemoveAll((EmpirePlanDefinition plan) => plan.EmpirePlanClass == empirePlanDefinition.EmpirePlanClass);
					list.Add(empirePlanDefinition);
				}
			}
		}
		return list.ToArray();
	}

	public IEnumerable<EmpirePlanDefinition> GetUnlockedEmpirePlanForEra(StaticString empirePlanClass)
	{
		int empirePlanAvailableLevel = this.GetEmpirePlanAvailableLevel(empirePlanClass);
		foreach (EmpirePlanDefinition empirePlanDefinition in this.GetEmpirePlanDefinitionByClass(empirePlanClass))
		{
			if (empirePlanDefinition.EmpirePlanLevel <= empirePlanAvailableLevel)
			{
				yield return empirePlanDefinition;
			}
		}
		yield break;
	}

	public void SubmitPlan(EmpirePlanDefinition empirePlanDefinition)
	{
	}

	public override void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		this.turnWithoutPillar = reader.GetAttribute<int>("TurnWithoutPillar");
		base.ReadXml(reader);
		reader.ReadStartElement("EmpirePlans");
		reader.ReadStartElement("EmpirePlanChoiceRemainingTurn");
		this.EmpirePlanChoiceRemainingTurn = reader.ReadString<int>();
		reader.ReadEndElement("EmpirePlanChoiceRemainingTurn");
		int attribute = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("CurrentEmpirePlan");
		this.currentEmpirePlanDefinitionByClass.Clear();
		for (int i = 0; i < attribute; i++)
		{
			string attribute2 = reader.GetAttribute<string>("Key");
			string attribute3 = reader.GetAttribute<string>("Value");
			reader.ReadStartElement("KeyValuePair");
			reader.ReadEndElement("KeyValuePair");
			DepartmentOfPlanificationAndDevelopment.ConstructibleElement constructibleElement;
			this.empirePlanDatabase.TryGetValue(attribute3, out constructibleElement);
			EmpirePlanDefinition empirePlanDefinition = constructibleElement as EmpirePlanDefinition;
			if (empirePlanDefinition == null)
			{
				Diagnostics.LogError("Can't load empire plan definition {0}.", new object[]
				{
					attribute3
				});
			}
			this.currentEmpirePlanDefinitionByClass.Add(attribute2, empirePlanDefinition);
		}
		reader.ReadEndElement("CurrentEmpirePlan");
		reader.ReadStartElement("Queue");
		Diagnostics.Assert(this.empirePlanQueue != null && this.empirePlanQueue.Length == 0);
		IXmlSerializable xmlSerializable = this.empirePlanQueue;
		reader.ReadElementSerializable<IXmlSerializable>("Constructions", ref xmlSerializable);
		reader.ReadEndElement("Queue");
		reader.ReadEndElement("EmpirePlans");
		this.turnWithoutBooster = reader.GetAttribute<int>("TurnWithoutBooster");
		int attribute4 = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("Boosters");
		for (int j = 0; j < attribute4; j++)
		{
			DepartmentOfPlanificationAndDevelopment.DeserializedBoosterInfo item;
			item.BoosterDefinitionName = reader.GetAttribute("BoosterDefinitionName");
			item.GUID = reader.GetAttribute<ulong>("GUID");
			item.TargetGUID = reader.GetAttribute<ulong>("TargetGUID");
			item.RemainingTime = reader.GetAttribute<int>("RemainingTime");
			item.Duration = reader.GetAttribute<int>("Duration");
			item.TurnWhenStarted = reader.GetAttribute<int>("TurnWhenStarted");
			item.InstigatorEmpireIndex = reader.GetAttribute<int>("InstigatorEmpireIndex", base.Empire.Index);
			this.deserializedBoosterInfo.Add(item);
			reader.Skip("Booster");
		}
		reader.ReadEndElement("Boosters");
		this.empirePlanSimulationWrapper.SimulationObject.RemoveAllDescriptors();
		reader.ReadElementSerializable<SimulationObjectWrapper>(ref this.empirePlanSimulationWrapper);
		if (num >= 2)
		{
			this.TurnWhenTradeRoutesWereLastCreated = reader.GetAttribute<int>("TurnWhenTradeRoutesWereLastCreated");
			reader.ReadStartElement("TradeRoutes");
			reader.ReadEndElement("TradeRoutes");
		}
		if (num >= 3 && num < 5)
		{
			int.Parse(reader.ReadElementString("LocalSelectedSeasonEffectIndex"));
			int attribute5 = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("LocalSeasonEffectVotes");
			for (int k = 0; k < attribute5; k++)
			{
				reader.Skip();
			}
			reader.ReadEndElement("LocalSeasonEffectVotes");
		}
		if (num >= 4)
		{
			this.LocalWinterImmunityWinnerIndex = int.Parse(reader.ReadElementString("LocalWinterImmunityWinnerIndex"));
			this.LocalWinterImmunityWinnerBid = int.Parse(reader.ReadElementString("LocalWinterImmunityWinnerBid"));
		}
		if (reader.IsStartElement("Statistics"))
		{
			reader.ReadStartElement("Statistics");
			this.StatConstructionBuilt = reader.ReadElementString<int>("StatConstructionBuilt");
			this.StatNecrophagesCadaverBoosterUsed = reader.ReadElementString<int>("StatNecrophagesCadaverBoosterUsed");
			if (num >= 6)
			{
				this.StatNecrophagesBattlebornBoosterUsed = reader.ReadElementString<int>("StatNecrophagesBattlebornBoosterUsed");
			}
			this.StatWarDeclarations = reader.ReadElementString<int>("StatWarDeclarations");
			if (reader.IsStartElement("StatCityConqueredCurrentWinter"))
			{
				this.StatCityConqueredCurrentWinter = reader.ReadElementString<int>("StatCityConqueredCurrentWinter");
			}
			if (reader.IsStartElement("StatLongestUnbrokenAlliance"))
			{
				reader.Skip();
			}
			if (reader.IsStartElement("StatAllianceDeclarations"))
			{
				this.StatAllianceDeclarations = reader.ReadElementString<int>("StatAllianceDeclarations");
				this.StatPeaceDeclarations = reader.ReadElementString<int>("StatPeaceDeclarations");
				this.StatQuestCompleted = reader.ReadElementString<int>("StatQuestCompleted");
				this.StatDustSpentMarket = reader.ReadElementString<float>("StatDustSpentMarket");
			}
			reader.ReadEndElement("Statistics");
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		int num = writer.WriteVersionAttribute(6);
		writer.WriteAttributeString<int>("TurnWithoutPillar", this.turnWithoutPillar);
		base.WriteXml(writer);
		writer.WriteStartElement("EmpirePlans");
		writer.WriteElementString<int>("EmpirePlanChoiceRemainingTurn", this.EmpirePlanChoiceRemainingTurn);
		writer.WriteStartElement("CurrentEmpirePlan");
		writer.WriteAttributeString<int>("Count", this.currentEmpirePlanDefinitionByClass.Count);
		foreach (KeyValuePair<StaticString, EmpirePlanDefinition> keyValuePair in this.currentEmpirePlanDefinitionByClass)
		{
			writer.WriteStartElement("KeyValuePair");
			writer.WriteAttributeString<StaticString>("Key", keyValuePair.Key);
			writer.WriteAttributeString<StaticString>("Value", keyValuePair.Value.Name);
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
		writer.WriteStartElement("Queue");
		IXmlSerializable xmlSerializable = this.empirePlanQueue;
		writer.WriteElementSerializable<IXmlSerializable>("Constructions", ref xmlSerializable);
		writer.WriteEndElement();
		writer.WriteEndElement();
		writer.WriteStartElement("Boosters");
		writer.WriteAttributeString<int>("TurnWithoutBooster", this.turnWithoutBooster);
		writer.WriteAttributeString<int>("Count", this.boosters.Count);
		foreach (KeyValuePair<GameEntityGUID, Booster> keyValuePair2 in this.boosters)
		{
			writer.WriteStartElement("Booster");
			writer.WriteAttributeString<GameEntityGUID>("GUID", keyValuePair2.Value.GUID);
			writer.WriteAttributeString<GameEntityGUID>("TargetGUID", keyValuePair2.Value.TargetGUID);
			writer.WriteAttributeString<StaticString>("BoosterDefinitionName", keyValuePair2.Value.BoosterDefinition.Name);
			writer.WriteAttributeString<int>("Duration", keyValuePair2.Value.Duration);
			writer.WriteAttributeString<int>("TurnWhenStarted", keyValuePair2.Value.TurnWhenStarted);
			writer.WriteAttributeString<int>("RemainingTime", keyValuePair2.Value.RemainingTime);
			writer.WriteAttributeString<int>("InstigatorEmpireIndex", keyValuePair2.Value.InstigatorEmpireIndex);
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
		IXmlSerializable xmlSerializable2 = this.empirePlanSimulationWrapper;
		writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable2);
		if (num >= 2)
		{
			writer.WriteStartElement("TradeRoutes");
			writer.WriteAttributeString<int>("TurnWhenTradeRoutesWereLastCreated", this.TurnWhenTradeRoutesWereLastCreated);
			writer.WriteEndElement();
		}
		if (num >= 4)
		{
			writer.WriteElementString<int>("LocalWinterImmunityWinnerIndex", this.LocalWinterImmunityWinnerIndex);
			writer.WriteElementString<int>("LocalWinterImmunityWinnerBid", this.LocalWinterImmunityWinnerBid);
		}
		writer.WriteStartElement("Statistics");
		writer.WriteElementString<int>("StatConstructionBuilt", this.StatConstructionBuilt);
		writer.WriteElementString<int>("StatNecrophagesCadaverBoosterUsed", this.StatNecrophagesCadaverBoosterUsed);
		writer.WriteElementString<int>("StatNecrophagesBattlebornBoosterUsed", this.StatNecrophagesBattlebornBoosterUsed);
		writer.WriteElementString<int>("StatWarDeclarations", this.StatWarDeclarations);
		writer.WriteElementString<int>("StatCityConqueredCurrentWinter", this.StatCityConqueredCurrentWinter);
		writer.WriteElementString<int>("StatAllianceDeclarations", this.StatAllianceDeclarations);
		writer.WriteElementString<int>("StatPeaceDeclarations", this.StatPeaceDeclarations);
		writer.WriteElementString<int>("StatQuestCompleted", this.StatQuestCompleted);
		writer.WriteElementString<float>("StatDustSpentMarket", this.StatDustSpentMarket);
		writer.WriteEndElement();
	}

	private void LoadDeserializedBoosters()
	{
		IDatabase<BoosterDefinition> database = Databases.GetDatabase<BoosterDefinition>(false);
		if (this.deserializedBoosterInfo.Count == 0 || this.gameEntityRepositoryService == null || database == null)
		{
			return;
		}
		foreach (DepartmentOfPlanificationAndDevelopment.DeserializedBoosterInfo deserializedBoosterInfo in this.deserializedBoosterInfo)
		{
			BoosterDefinition boosterDefinition;
			if (database.TryGetValue(deserializedBoosterInfo.BoosterDefinitionName, out boosterDefinition))
			{
				GameEntityGUID boosterGUID = deserializedBoosterInfo.GUID;
				GameEntityGUID gameEntityGUID = deserializedBoosterInfo.TargetGUID;
				SimulationObjectWrapper simulationObjectWrapper = base.Empire;
				if (gameEntityGUID != GameEntityGUID.Zero)
				{
					IGameEntity gameEntity;
					if (this.gameEntityRepositoryService.TryGetValue(gameEntityGUID, out gameEntity))
					{
						if (gameEntity is SimulationObjectWrapper)
						{
							simulationObjectWrapper = (gameEntity as SimulationObjectWrapper);
						}
						else
						{
							Diagnostics.LogError("Target (guid = '{0}' isn't a SimulationObjectWrapper.", new object[]
							{
								gameEntityGUID
							});
						}
					}
					else
					{
						Diagnostics.LogError("Cannot retrieve target (guid = '{0}').", new object[]
						{
							gameEntityGUID
						});
					}
				}
				Booster booster = new Booster(boosterGUID, boosterDefinition, base.Empire as global::Empire, simulationObjectWrapper, gameEntityGUID, 0, -1);
				booster.ApplyClassTimedDescriptor();
				booster.RemainingTime = deserializedBoosterInfo.RemainingTime;
				booster.Duration = deserializedBoosterInfo.Duration;
				booster.TurnWhenStarted = deserializedBoosterInfo.TurnWhenStarted;
				booster.InstigatorEmpireIndex = deserializedBoosterInfo.InstigatorEmpireIndex;
				this.boosters.Add(booster.GUID, booster);
				this.gameEntityRepositoryService.Register(booster);
				simulationObjectWrapper.AddChild(booster);
				booster.ApplyDescriptors(false);
				booster.ApplyEffects();
				simulationObjectWrapper.Refresh(false);
			}
		}
		this.deserializedBoosterInfo.Clear();
		this.OnBoosterCollectionChange(new BoosterCollectionChangeEventArgs(BoosterCollectionChangeAction.Undefined, null));
	}

	private bool BuyoutAndActivateBoosterPreprocessor(OrderBuyoutAndActivateBooster order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (StaticString.IsNullOrEmpty(order.BoosterDefinitionName))
		{
			Diagnostics.LogError("Order preprocessor failed because booster definition name is either null or empty.");
			return false;
		}
		if (base.Empire.GetAgency<DepartmentOfTheInterior>() == null)
		{
			Diagnostics.LogError("Order preprocessor failed because the department of the interior is null.");
			return false;
		}
		IDatabase<BoosterDefinition> database = Databases.GetDatabase<BoosterDefinition>(false);
		if (database != null)
		{
			BoosterDefinition boosterDefinition;
			if (database.TryGetValue(order.BoosterDefinitionName, out boosterDefinition))
			{
				SimulationObjectWrapper simulationObjectWrapper;
				if (order.TargetGUID != GameEntityGUID.Zero)
				{
					IGameEntity gameEntity;
					if (!this.gameEntityRepositoryService.TryGetValue(order.TargetGUID, out gameEntity))
					{
						Diagnostics.LogError("Wasn't able to retrieve the target entity (guid = '{0}')", new object[]
						{
							order.TargetGUID
						});
						return false;
					}
					simulationObjectWrapper = (gameEntity as SimulationObjectWrapper);
					if (simulationObjectWrapper == null)
					{
						Diagnostics.LogError("The target entity isn't a simulation object wrapper (guid = '{0}')", new object[]
						{
							order.TargetGUID
						});
						return false;
					}
				}
				else
				{
					simulationObjectWrapper = base.Empire;
				}
				int boosterDurationWithBonus = DepartmentOfPlanificationAndDevelopment.GetBoosterDurationWithBonus(base.Empire, simulationObjectWrapper.SimulationObject, boosterDefinition);
				if (boosterDurationWithBonus < 0)
				{
					Diagnostics.LogError("Order preprocessor failed because booster duraction is invalid (booster definition name: '{0}', duration: {1}).", new object[]
					{
						boosterDefinition.Name,
						boosterDurationWithBonus
					});
					return false;
				}
				if (order.Duration <= 0)
				{
					order.Duration = boosterDurationWithBonus;
				}
				if (order.BoosterGameEntityGUID != GameEntityGUID.Zero)
				{
					DepartmentOfEducation agency = base.Empire.GetAgency<DepartmentOfEducation>();
					if (agency != null && agency.Exist(order.BoosterGameEntityGUID))
					{
						return true;
					}
				}
				ConstructionResourceStock[] array = null;
				bool flag = order.IsFree || this.departmentOfTheTreasury.GetInstantConstructionResourceCostForBuyout(base.Empire, boosterDefinition, out array);
				if (flag)
				{
					if (order.BoosterGameEntityGUID == GameEntityGUID.Zero)
					{
						order.BoosterGameEntityGUID = this.gameEntityRepositoryService.GenerateGUID();
					}
					if (array != null)
					{
						order.ConstructionResourceStocks = array;
					}
					return true;
				}
				Diagnostics.LogWarning("Order preprocessor failed because booster cost is not affordable (booster definition name: '{0}').", new object[]
				{
					boosterDefinition.Name
				});
			}
			else
			{
				Diagnostics.LogWarning("Order preprocessor failed because booster name is invalid (booster definition name: '{0}').", new object[]
				{
					order.BoosterDefinitionName
				});
			}
		}
		return false;
	}

	private IEnumerator BuyoutAndActivateBoosterProcessor(OrderBuyoutAndActivateBooster order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		DepartmentOfTheInterior departmentOfTheInterior = base.Empire.GetAgency<DepartmentOfTheInterior>();
		if (departmentOfTheInterior == null)
		{
			Diagnostics.LogError("Order processor failed because the department of the interior is null.");
			yield break;
		}
		IEventService eventService = Services.GetService<IEventService>();
		IDatabase<BoosterDefinition> boosterDefinitions = Databases.GetDatabase<BoosterDefinition>(false);
		BoosterDefinition boosterDefinition;
		if (boosterDefinitions.TryGetValue(order.BoosterDefinitionName, out boosterDefinition))
		{
			if (order.ConstructionResourceStocks != null && order.ConstructionResourceStocks.Length > 0)
			{
				for (int index = 0; index < order.ConstructionResourceStocks.Length; index++)
				{
					if (order.ConstructionResourceStocks[index] != null)
					{
						if (order.ConstructionResourceStocks[index].Stock != 0f)
						{
							if (boosterDefinition.Costs != null && index < boosterDefinition.Costs.Length)
							{
								this.departmentOfTheTreasury.TryTransferResources(base.Empire, boosterDefinition.Costs[index].ResourceName, -order.ConstructionResourceStocks[index].Stock);
							}
						}
					}
				}
			}
			SimulationObjectWrapper context = null;
			if (order.TargetGUID != GameEntityGUID.Zero)
			{
				IGameEntity gameEntity;
				if (!this.gameEntityRepositoryService.TryGetValue(order.TargetGUID, out gameEntity))
				{
					Diagnostics.LogError("Wasn't able to retrieve the target entity (guid = '{0}')", new object[]
					{
						order.TargetGUID
					});
					yield break;
				}
				context = (gameEntity as SimulationObjectWrapper);
				if (context == null)
				{
					Diagnostics.LogError("The target entity isn't a simulation object wrapper (guid = '{0}')", new object[]
					{
						order.TargetGUID
					});
					yield break;
				}
			}
			else
			{
				context = base.Empire;
			}
			DepartmentOfEducation departmentOfEducation = base.Empire.GetAgency<DepartmentOfEducation>();
			bool canActivateBooster = Booster.CanActivate(boosterDefinition, context);
			bool activatedThroughVault = departmentOfEducation.Exist(order.BoosterGameEntityGUID);
			if ((order.IsFree || activatedThroughVault || boosterDefinition.AutoActivation) && canActivateBooster)
			{
				if (DepartmentOfTheInterior.CanUseAffinityStrategicResource(base.Empire as global::Empire))
				{
					StaticString boosterResourceName = this.GetBoosterResourceName(order.BoosterDefinitionName);
					if (!StaticString.IsNullOrEmpty(boosterResourceName) && DepartmentOfTheInterior.CanUseResourceAsAffinityResource(boosterResourceName))
					{
						this.SelectAffinityStrategicResource(boosterResourceName);
						if (eventService != null)
						{
							eventService.Notify(new EventAffinityStrategicResourceChanged(base.Empire));
						}
					}
				}
				this.ActivateBooster(boosterDefinition, order.BoosterGameEntityGUID, order.Duration, context, order.TargetGUID, order.InstigatorEmpireIndex);
				if (context is City)
				{
					City city = context as City;
					departmentOfTheInterior.ComputeCityPopulation(city, false);
				}
			}
			else if (!activatedThroughVault)
			{
				departmentOfEducation.AddVaultItem(new VaultItem(order.BoosterGameEntityGUID, boosterDefinition));
			}
			else
			{
				Diagnostics.LogWarning("Booster {0} can't be activated.", new object[]
				{
					boosterDefinition.Name
				});
			}
		}
		yield break;
	}

	private StaticString GetBoosterResourceName(string boosterDefinitionName)
	{
		string text = "Booster";
		if (boosterDefinitionName.StartsWith(text))
		{
			StaticString staticString = boosterDefinitionName.Substring(text.Length, boosterDefinitionName.Length - text.Length);
			IDatabase<ResourceDefinition> database = Databases.GetDatabase<ResourceDefinition>(true);
			Diagnostics.Assert(database != null);
			ResourceDefinition resourceDefinition;
			if (database != null && database.TryGetValue(staticString, out resourceDefinition))
			{
				return staticString;
			}
		}
		return StaticString.Empty;
	}

	private void SelectAffinityStrategicResource(string resourceName)
	{
		ResourceDefinition affinityStrategicResource = this.departmentOfTheTreasury.GetAffinityStrategicResource();
		if (affinityStrategicResource != null)
		{
			this.RemoveOldAffinityStrategicResource(affinityStrategicResource.XmlSerializableName);
		}
		StaticString staticString = new StaticString("Affinity" + resourceName);
		IDatabase<SimulationDescriptor> database = Databases.GetDatabase<SimulationDescriptor>(false);
		SimulationDescriptor descriptor;
		if (database.TryGetValue(staticString, out descriptor))
		{
			base.Empire.AddDescriptor(descriptor, false);
		}
		else
		{
			base.Empire.SimulationObject.Tags.AddTag(staticString);
		}
		staticString = OrderSelectAffinityStrategicResource.AffinityResourceChosenDescriptor;
		if (database.TryGetValue(staticString, out descriptor))
		{
			base.Empire.AddDescriptor(descriptor, false);
		}
		else
		{
			base.Empire.SimulationObject.Tags.AddTag(staticString);
		}
	}

	private void RemoveOldAffinityStrategicResource(string affinityResourceName)
	{
		StaticString staticString = new StaticString("Affinity" + affinityResourceName);
		IDatabase<SimulationDescriptor> database = Databases.GetDatabase<SimulationDescriptor>(false);
		SimulationDescriptor descriptor;
		if (database.TryGetValue(staticString, out descriptor))
		{
			base.Empire.RemoveDescriptor(descriptor);
		}
		else
		{
			base.Empire.SimulationObject.Tags.RemoveTag(staticString);
		}
		staticString = OrderRemoveAffinityStrategicResource.AffinityResourceChosenDescriptor;
		if (database.TryGetValue(staticString, out descriptor))
		{
			base.Empire.RemoveDescriptor(descriptor);
		}
		else
		{
			base.Empire.SimulationObject.Tags.RemoveTag(staticString);
		}
	}

	private bool BuyoutAndActivateBoosterByInfiltrationPreprocessor(OrderBuyoutAndActivateBoosterByInfiltration order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (order.BoosterDeclarations == null || order.BoosterDeclarations.Length <= 0)
		{
			return false;
		}
		IGameEntity gameEntity;
		if (!this.gameEntityRepositoryService.TryGetValue(order.InfiltratedCityGUID, out gameEntity))
		{
			return false;
		}
		bool? flag = new bool?(true);
		InfiltrationAction.ComputeConstructionCost((global::Empire)base.Empire, order, ref flag);
		if (!flag.Value)
		{
			return false;
		}
		DepartmentOfPlanificationAndDevelopment departmentOfPlanificationAndDevelopment = this;
		City city = gameEntity as City;
		if (!order.ActivateOnInstigator && city != null && city.Empire != null)
		{
			departmentOfPlanificationAndDevelopment = city.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
			if (departmentOfPlanificationAndDevelopment == null)
			{
				return false;
			}
		}
		IDatabase<BoosterDefinition> database = Databases.GetDatabase<BoosterDefinition>(false);
		if (database != null)
		{
			int num = 0;
			for (int i = 0; i < order.BoosterDeclarations.Length; i++)
			{
				BoosterDefinition boosterDefinition;
				if (!database.TryGetValue(order.BoosterDeclarations[i].BoosterDefinitionName, out boosterDefinition))
				{
					return false;
				}
				int boosterDurationWithBonus = DepartmentOfPlanificationAndDevelopment.GetBoosterDurationWithBonus(base.Empire, departmentOfPlanificationAndDevelopment.Empire, boosterDefinition);
				if (boosterDurationWithBonus < 0)
				{
					order.BoosterDeclarations[i].Duration = 0;
				}
				else
				{
					if (order.BoosterDeclarations[i].Duration <= 0)
					{
						order.BoosterDeclarations[i].Duration = boosterDurationWithBonus;
					}
					else
					{
						order.BoosterDeclarations[i].Duration = DepartmentOfPlanificationAndDevelopment.GetBoosterDurationWithBonus(departmentOfPlanificationAndDevelopment.Empire, order.BoosterDeclarations[i].Duration);
					}
					num += boosterDurationWithBonus;
					order.BoosterDeclarations[i].GameEntityGUID = this.gameEntityRepositoryService.GenerateGUID();
				}
			}
			if (num <= 0)
			{
				return false;
			}
		}
		float antiSpyParameter;
		order.AntiSpyResult = DepartmentOfIntelligence.GetAntiSpyResult(city, out antiSpyParameter);
		order.AntiSpyParameter = antiSpyParameter;
		return true;
	}

	private IEnumerator BuyoutAndActivateBoosterByInfiltrationProcessor(OrderBuyoutAndActivateBoosterByInfiltration order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		InfiltrationAction.TryTransferResources((global::Empire)base.Empire, order);
		InfiltrationAction.TryTransferLevelCost(base.Empire as global::Empire, order);
		InfiltrationAction.ApplyExperienceReward(base.Empire as global::Empire, order);
		DepartmentOfPlanificationAndDevelopment host = this;
		City infiltratedCity = null;
		IGameEntity infiltratedCityAsEntity;
		if (this.gameEntityRepositoryService.TryGetValue(order.InfiltratedCityGUID, out infiltratedCityAsEntity))
		{
			infiltratedCity = (infiltratedCityAsEntity as City);
		}
		if (!order.ActivateOnInstigator && infiltratedCity != null && infiltratedCity.Empire != null)
		{
			host = infiltratedCity.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
		}
		if (host != null && order.BoosterDeclarations != null)
		{
			IDatabase<BoosterDefinition> boosterDefinitionDatabase = Databases.GetDatabase<BoosterDefinition>(false);
			if (boosterDefinitionDatabase != null)
			{
				for (int index = 0; index < order.BoosterDeclarations.Length; index++)
				{
					BoosterDefinition boosterDefinition;
					if (boosterDefinitionDatabase.TryGetValue(order.BoosterDeclarations[index].BoosterDefinitionName, out boosterDefinition))
					{
						bool canActivateBooster = Booster.CanActivate(boosterDefinition, host.Empire);
						if (canActivateBooster && order.BoosterDeclarations[index].GameEntityGUID.IsValid)
						{
							SimulationObjectWrapper context = null;
							GameEntityGUID targetGuid = GameEntityGUID.Zero;
							switch (boosterDefinition.Target)
							{
							case BoosterDefinition.TargetType.Empire:
								goto IL_26B;
							case BoosterDefinition.TargetType.City:
								context = infiltratedCity;
								targetGuid = infiltratedCity.GUID;
								break;
							case BoosterDefinition.TargetType.Hero:
								if (infiltratedCity.Hero != null)
								{
									Diagnostics.LogError("The booster cannot be created because there is no hero on the city.");
								}
								else
								{
									context = infiltratedCity.Hero;
									targetGuid = infiltratedCity.Hero.GUID;
								}
								break;
							default:
								goto IL_26B;
							}
							IL_281:
							if (context != null)
							{
								host.ActivateBooster(boosterDefinition, order.BoosterDeclarations[index].GameEntityGUID, order.BoosterDeclarations[index].Duration, context, targetGuid, base.Empire.Index);
								goto IL_2EF;
							}
							goto IL_2EF;
							IL_26B:
							context = host.Empire;
							goto IL_281;
						}
					}
					IL_2EF:;
				}
			}
			InfiltrationAction.TryNotifyInfiltrationActionResult((global::Empire)base.Empire, (global::Empire)base.Empire, order);
			InfiltrationAction.TryNotifyInfiltrationActionResult(infiltratedCity.Empire, (global::Empire)base.Empire, order);
		}
		DepartmentOfIntelligence departmentOfIntelligence = base.Empire.GetAgency<DepartmentOfIntelligence>();
		if (departmentOfIntelligence != null)
		{
			departmentOfIntelligence.ExecuteAntiSpy(order.InfiltratedCityGUID, order.AntiSpyResult, order.AntiSpyParameter, false);
		}
		yield break;
	}

	private bool BuyoutAndActivateBoosterThroughArmyPreprocessor(OrderBuyoutAndActivateBoosterThroughArmy order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		IGameEntity gameEntity;
		if (this.gameEntityRepositoryService == null || !this.gameEntityRepositoryService.TryGetValue(order.ArmyGUID, out gameEntity))
		{
			return false;
		}
		Army army = gameEntity as Army;
		if (army == null)
		{
			return false;
		}
		if (order.BoosterDeclarations == null || order.BoosterDeclarations.Length <= 0)
		{
			return false;
		}
		IGameEntity gameEntity2;
		if (!this.gameEntityRepositoryService.TryGetValue(order.TargetGUID, out gameEntity2))
		{
			return false;
		}
		DepartmentOfPlanificationAndDevelopment departmentOfPlanificationAndDevelopment = this;
		if (!order.ActivateOnInstigator)
		{
			IGameService service = Services.GetService<IGameService>();
			if (service != null)
			{
				global::Game game = service.Game as global::Game;
				if (game != null && game.Empires != null && order.TargetEmpireIndex >= 0 && order.TargetEmpireIndex < game.Empires.Length)
				{
					departmentOfPlanificationAndDevelopment = game.Empires[order.TargetEmpireIndex].GetAgency<DepartmentOfPlanificationAndDevelopment>();
				}
			}
		}
		if (departmentOfPlanificationAndDevelopment == null)
		{
			return false;
		}
		IDatabase<BoosterDefinition> database = Databases.GetDatabase<BoosterDefinition>(false);
		if (database != null)
		{
			int num = 0;
			for (int i = 0; i < order.BoosterDeclarations.Length; i++)
			{
				BoosterDefinition boosterDefinition;
				if (!database.TryGetValue(order.BoosterDeclarations[i].BoosterDefinitionName, out boosterDefinition))
				{
					return false;
				}
				int boosterDurationWithBonus = DepartmentOfPlanificationAndDevelopment.GetBoosterDurationWithBonus(base.Empire, departmentOfPlanificationAndDevelopment.Empire, boosterDefinition);
				if (boosterDurationWithBonus < 0)
				{
					order.BoosterDeclarations[i].Duration = 0;
				}
				else
				{
					if (order.BoosterDeclarations[i].Duration <= 0)
					{
						order.BoosterDeclarations[i].Duration = boosterDurationWithBonus;
					}
					else
					{
						order.BoosterDeclarations[i].Duration = DepartmentOfPlanificationAndDevelopment.GetBoosterDurationWithBonus(departmentOfPlanificationAndDevelopment.Empire, order.BoosterDeclarations[i].Duration);
					}
					num += boosterDurationWithBonus;
					order.BoosterDeclarations[i].GameEntityGUID = this.gameEntityRepositoryService.GenerateGUID();
				}
			}
			if (num <= 0)
			{
				return false;
			}
		}
		if (order.NumberOfActionPointsToSpend < 0f && !StaticString.IsNullOrEmpty(order.ArmyActionName))
		{
			ArmyAction armyAction = null;
			IDatabase<ArmyAction> database2 = Databases.GetDatabase<ArmyAction>(false);
			if (database2 != null && database2.TryGetValue(order.ArmyActionName, out armyAction))
			{
				order.NumberOfActionPointsToSpend = armyAction.GetCostInActionPoints();
			}
		}
		if (order.NumberOfActionPointsToSpend > 0f)
		{
			SimulationObjectWrapper simulationObjectWrapper = army;
			if (simulationObjectWrapper != null)
			{
				float propertyValue = simulationObjectWrapper.GetPropertyValue(SimulationProperties.MaximumNumberOfActionPoints);
				float propertyValue2 = simulationObjectWrapper.GetPropertyValue(SimulationProperties.ActionPointsSpent);
				if (order.NumberOfActionPointsToSpend > propertyValue - propertyValue2)
				{
					Diagnostics.LogWarning("Not enough action points.");
					return false;
				}
			}
		}
		return true;
	}

	private IEnumerator BuyoutAndActivateBoosterThroughArmyProcessor(OrderBuyoutAndActivateBoosterThroughArmy order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (!order.ArmyGUID.IsValid)
		{
			Diagnostics.LogError("Game entity guid is not valid: {0}.", new object[]
			{
				order.ArmyGUID.ToString()
			});
			yield break;
		}
		IGameEntity gameEntity;
		if (this.gameEntityRepositoryService == null || !this.gameEntityRepositoryService.TryGetValue(order.ArmyGUID, out gameEntity))
		{
			Diagnostics.LogError("Cannot find army game entity: {0}.", new object[]
			{
				order.ArmyGUID.ToString()
			});
			yield break;
		}
		Army army = gameEntity as Army;
		if (army == null)
		{
			Diagnostics.LogError("Cannot cast army game entity: {0}.", new object[]
			{
				order.ArmyGUID.ToString()
			});
			yield break;
		}
		ArmyAction armyAction = null;
		bool zeroMovement = true;
		if (!StaticString.IsNullOrEmpty(order.ArmyActionName))
		{
			IDatabase<ArmyAction> armyActionDatabase = Databases.GetDatabase<ArmyAction>(false);
			if (armyActionDatabase != null && armyActionDatabase.TryGetValue(order.ArmyActionName, out armyAction) && armyAction is IArmyActionWithMovementEffect)
			{
				zeroMovement = (armyAction as IArmyActionWithMovementEffect).ZeroMovement;
			}
		}
		if (zeroMovement)
		{
			foreach (Unit unit in army.Units)
			{
				unit.SetPropertyBaseValue(SimulationProperties.MovementRatio, 0f);
				unit.Refresh(false);
			}
		}
		if (order.NumberOfActionPointsToSpend > 0f)
		{
			ArmyAction.SpendSomeNumberOfActionPoints(army, order.NumberOfActionPointsToSpend);
		}
		if (order.ArmyActionCooldownDuration > 0f)
		{
			ArmyActionWithCooldown.ApplyCooldown(army, order.ArmyActionCooldownDuration);
		}
		army.Refresh(false);
		DepartmentOfPlanificationAndDevelopment host = this;
		if (!order.ActivateOnInstigator)
		{
			IGameService gameService = Services.GetService<IGameService>();
			if (gameService != null)
			{
				global::Game game = gameService.Game as global::Game;
				if (game != null && game.Empires != null && order.TargetEmpireIndex >= 0 && order.TargetEmpireIndex < game.Empires.Length)
				{
					host = game.Empires[order.TargetEmpireIndex].GetAgency<DepartmentOfPlanificationAndDevelopment>();
				}
			}
			this.gameEntityRepositoryService.TryGetValue(order.TargetGUID, out gameEntity);
		}
		if (host != null && gameEntity != null && order.BoosterDeclarations != null)
		{
			IDatabase<BoosterDefinition> boosterDefinitionDatabase = Databases.GetDatabase<BoosterDefinition>(false);
			if (boosterDefinitionDatabase != null)
			{
				bool armyActionHasBeenNotified = false;
				for (int index = 0; index < order.BoosterDeclarations.Length; index++)
				{
					BoosterDefinition boosterDefinition;
					if (boosterDefinitionDatabase.TryGetValue(order.BoosterDeclarations[index].BoosterDefinitionName, out boosterDefinition))
					{
						bool canActivateBooster = Booster.CanActivate(boosterDefinition, host.Empire);
						if (canActivateBooster && order.BoosterDeclarations[index].GameEntityGUID.IsValid)
						{
							SimulationObjectWrapper context = gameEntity as SimulationObjectWrapper;
							host.ActivateBooster(boosterDefinition, order.BoosterDeclarations[index].GameEntityGUID, order.BoosterDeclarations[index].Duration, context, gameEntity.GUID, base.Empire.Index);
							if (armyAction != null && !armyActionHasBeenNotified && gameEntity is IGameEntityWithWorldPosition)
							{
								armyActionHasBeenNotified = true;
								army.OnArmyAction(armyAction, gameEntity as IGameEntityWithWorldPosition);
							}
						}
					}
				}
			}
		}
		yield break;
	}

	private bool BuyoutTradablePreprocessor(OrderBuyoutTradable order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (order.TradableUID == 0UL)
		{
			return false;
		}
		Diagnostics.Assert(this.tradeManagementService != null);
		ITradable tradable = null;
		if (order is OrderBuyoutTradableUnit)
		{
			GameEntityGUID destination = (order as OrderBuyoutTradableUnit).Destination;
			IGameEntity gameEntity;
			if (!this.gameEntityRepositoryService.TryGetValue(destination, out gameEntity))
			{
				Diagnostics.LogError("Unable to retrieve the destination city (guid: {0}).", new object[]
				{
					destination.ToString()
				});
				return false;
			}
			if (!(gameEntity is Garrison))
			{
				Diagnostics.LogError("Invalid destination for bought unit (guid: {0}, typeof: '{1}').", new object[]
				{
					destination.ToString(),
					gameEntity.GetType().ToString()
				});
				return false;
			}
			UnitDesign unitDesign;
			if (this.tradeManagementService.TryGetTradableByUID(order.TradableUID, out tradable) && tradable is TradableUnit && this.tradeManagementService.TryRetrieveUnitDesign((tradable as TradableUnit).Barcode, out unitDesign))
			{
				if (unitDesign.Tags.Contains(DownloadableContent9.TagColossus) || unitDesign.Tags.Contains(DownloadableContent9.TagSolitary))
				{
					if (unitDesign.Tags.Contains(DownloadableContent9.TagColossus))
					{
						float propertyValue = base.Empire.GetPropertyValue(SimulationProperties.MaximumNumberOfColossi);
						float propertyValue2 = base.Empire.GetPropertyValue(SimulationProperties.NumberOfColossi);
						if (propertyValue - propertyValue2 < 1f)
						{
							Diagnostics.LogWarning("Cancelling the buyout because the empire cannot afford another Colossus.");
							return false;
						}
					}
					if (gameEntity is City)
					{
						List<WorldPosition> availablePositionsForArmyCreation = DepartmentOfDefense.GetAvailablePositionsForArmyCreation(gameEntity as City);
						if (availablePositionsForArmyCreation == null || availablePositionsForArmyCreation.Count == 0)
						{
							Diagnostics.LogWarning("Cancelling the buyout because there is no army spawn location nearby (where to spawn the Colossus).");
							return false;
						}
						(order as OrderBuyoutTradableUnit).SpawnLocation = availablePositionsForArmyCreation[0];
						(order as OrderBuyoutTradableUnit).SpawnNewArmyGUID = this.gameEntityRepositoryService.GenerateGUID();
					}
					else
					{
						if (!(gameEntity is Fortress))
						{
							Diagnostics.LogError("Invalid destination for bought unit (guid: {0}, typeof: '{1}').", new object[]
							{
								destination.ToString(),
								gameEntity.GetType().ToString()
							});
							return false;
						}
						List<WorldPosition> availablePositionsForArmyCreation2 = DepartmentOfDefense.GetAvailablePositionsForArmyCreation(gameEntity as Fortress);
						if (availablePositionsForArmyCreation2 == null || availablePositionsForArmyCreation2.Count == 0)
						{
							Diagnostics.LogWarning("Cancelling the buyout because there is no army spawn location nearby (where to spawn the Colossus).");
							return false;
						}
						(order as OrderBuyoutTradableUnit).SpawnLocation = availablePositionsForArmyCreation2[0];
						(order as OrderBuyoutTradableUnit).SpawnNewArmyGUID = this.gameEntityRepositoryService.GenerateGUID();
					}
				}
				if (unitDesign.UnitBodyDefinition.Tags.Contains(DownloadableContent16.TagSeafaring))
				{
					if (gameEntity is City)
					{
						List<WorldPosition> availablePositionsForSeafaringArmyCreation = DepartmentOfDefense.GetAvailablePositionsForSeafaringArmyCreation(gameEntity as City);
						if (availablePositionsForSeafaringArmyCreation == null || availablePositionsForSeafaringArmyCreation.Count == 0)
						{
							Diagnostics.LogWarning("Cancelling the buyout because there is no army spawn location nearby (where to spawn the Seafring unit).");
							return false;
						}
						(order as OrderBuyoutTradableUnit).SpawnLocation = availablePositionsForSeafaringArmyCreation[0];
						IGameService service = Services.GetService<IGameService>();
						IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
						if (service2.GetArmyAtPosition(availablePositionsForSeafaringArmyCreation[0]) == null)
						{
							(order as OrderBuyoutTradableUnit).SpawnNewArmyGUID = this.gameEntityRepositoryService.GenerateGUID();
						}
					}
					else if (!(gameEntity is Fortress))
					{
						Diagnostics.LogError("Invalid destination for bought unit (guid: {0}, typeof: '{1}').", new object[]
						{
							destination.ToString(),
							gameEntity.GetType().ToString()
						});
						return false;
					}
				}
				else if (gameEntity is Fortress)
				{
				}
			}
			Garrison garrison = gameEntity as Garrison;
			if (garrison.CurrentUnitSlot >= garrison.MaximumUnitSlot)
			{
				Diagnostics.LogWarning("Cancelling the buyout because there is no room left for the unit in the target garrison (count: {0}, capacity: {1}, garisson name: '{2}', localized: '{3}').", new object[]
				{
					garrison.CurrentUnitSlot,
					garrison.MaximumUnitSlot,
					garrison.Name,
					garrison.LocalizedName
				});
				return false;
			}
		}
		if (tradable != null || this.tradeManagementService.TryGetTradableByUID(order.TradableUID, out tradable))
		{
			if (tradable.Quantity < order.Quantity)
			{
				return false;
			}
			float price = tradable.GetPriceWithSalesTaxes(TradableTransactionType.Buyout, (global::Empire)base.Empire, order.Quantity) * -1f;
			bool flag = this.departmentOfTheTreasury.IsTransferOfResourcePossible(base.Empire, DepartmentOfTheTreasury.Resources.EmpireMoney, ref price);
			if (flag)
			{
				order.Price = price;
				order.UpdatedTradableCategoryTendency = this.tradeManagementService.UpdateTendency(tradable.TradableCategoryDefinition, Tradable.PositiveTendencyMultiplier * order.Quantity);
				bool flag2 = this.tradeManagementService.TryReserveTradable(order.TradableUID, order.Quantity, out tradable);
				if (flag2)
				{
					return true;
				}
			}
		}
		return false;
	}

	private IEnumerator BuyoutTradableProcessor(OrderBuyoutTradable order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (order.TradableUID == 0UL)
		{
			yield break;
		}
		Diagnostics.Assert(this.tradeManagementService != null);
		ITradable tradable;
		if (this.tradeManagementService.TryGetTradableByUID(order.TradableUID, out tradable))
		{
			if (tradable.Quantity < order.Quantity)
			{
				Diagnostics.LogError("Order processing failed because the tradable is not available in the requested quantity.");
				yield break;
			}
			bool transferOfResourcePossible = this.departmentOfTheTreasury.TryTransferResources(base.Empire, DepartmentOfTheTreasury.Resources.EmpireMoney, order.Price);
			if (transferOfResourcePossible)
			{
				object[] parameters = null;
				if (order is OrderBuyoutTradableUnit)
				{
					GameEntityGUID destination = (order as OrderBuyoutTradableUnit).Destination;
					WorldPosition spawnLocation = (order as OrderBuyoutTradableUnit).SpawnLocation;
					GameEntityGUID spawnNewArmyGUID = (order as OrderBuyoutTradableUnit).SpawnNewArmyGUID;
					IGameEntity garrison;
					if (this.gameEntityRepositoryService.TryGetValue(destination, out garrison))
					{
						parameters = new object[]
						{
							garrison,
							spawnLocation,
							spawnNewArmyGUID
						};
					}
				}
				this.tradeManagementService.SetTendency(tradable.TradableCategoryDefinition.Name, order.UpdatedTradableCategoryTendency);
				this.tradeManagementService.TryConsumeTradableAndAllocateTo(order.TradableUID, order.Quantity, (global::Empire)base.Empire, parameters);
				this.tradeManagementService.NotifyTradableTransactionComplete(TradableTransactionType.Buyout, (global::Empire)base.Empire, tradable, order.Quantity, order.Price);
				yield break;
			}
		}
		Diagnostics.LogError("Order processing failed.");
		yield break;
	}

	private bool ChangeEmpirePlanPreprocessor(OrderChangeEmpirePlan order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (StaticString.IsNullOrEmpty(order.EmpirePlanClass))
		{
			Diagnostics.LogError("Order preprocessor failed because empire plan class is null or empty.");
			return false;
		}
		if (!this.IsEmpirePlanChoiceTurn)
		{
			Diagnostics.LogError("Order preprocessor failed because you can't change the empire plan this turn.");
			return false;
		}
		EmpirePlanDefinition empirePlanDefinition = this.GetEmpirePlanDefinition(order.EmpirePlanClass, order.EmpirePlanLevel);
		if (empirePlanDefinition == null || StaticString.IsNullOrEmpty(empirePlanDefinition.Name))
		{
			Diagnostics.LogError("Order preprocessor failed because empire plan constructible is null or empty.");
			return false;
		}
		if (!DepartmentOfTheTreasury.CheckConstructiblePrerequisites(base.Empire, empirePlanDefinition, new string[0]))
		{
			Diagnostics.LogError("Order preprocessor failed because empire plan constructible ({0}) prerequisites fails.", new object[]
			{
				empirePlanDefinition.Name
			});
			return false;
		}
		order.ConstructibleElementName = empirePlanDefinition.Name;
		if (empirePlanDefinition.Costs == null || empirePlanDefinition.Costs.Length == 0)
		{
			order.ResourceStocks = new ConstructionResourceStock[0];
		}
		else
		{
			order.ResourceStocks = new ConstructionResourceStock[empirePlanDefinition.Costs.Length];
			for (int i = 0; i < empirePlanDefinition.Costs.Length; i++)
			{
				Diagnostics.Assert(empirePlanDefinition.Costs[i] != null);
				StaticString resourceName = empirePlanDefinition.Costs[i].ResourceName;
				order.ResourceStocks[i] = new ConstructionResourceStock(resourceName, base.Empire);
				if (empirePlanDefinition.Costs[i].Instant)
				{
					Diagnostics.Assert(base.Empire != null);
					float productionCostWithBonus = DepartmentOfTheTreasury.GetProductionCostWithBonus(base.Empire.SimulationObject, empirePlanDefinition, empirePlanDefinition.Costs[i], false);
					Diagnostics.Assert(this.departmentOfTheTreasury != null);
					float num;
					if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.Empire.SimulationObject, resourceName, out num, false))
					{
						Diagnostics.LogError("Order preprocessing failed because the constructible element (name: '{0}') asks for instant resource (name: '{1}') that can't be retrieved.", new object[]
						{
							empirePlanDefinition.Name,
							resourceName
						});
					}
					else
					{
						float currentEmpirePlanInvestment = this.GetCurrentEmpirePlanInvestment(resourceName, order.EmpirePlanClass);
						if (productionCostWithBonus > num + currentEmpirePlanInvestment)
						{
							Diagnostics.LogWarning("Order preprocessing failed because the constructible element (name: '{0}') asks for (amount: {1}, resource name: '{2}') and the available stock is only of {3}.", new object[]
							{
								empirePlanDefinition.Name,
								productionCostWithBonus,
								resourceName,
								num
							});
							return false;
						}
						order.ResourceStocks[i].Stock = productionCostWithBonus;
					}
				}
			}
		}
		Diagnostics.Assert(this.gameEntityRepositoryService != null);
		order.ConstructionGameEntityGUID = this.gameEntityRepositoryService.GenerateGUID();
		return true;
	}

	private IEnumerator ChangeEmpirePlanProcessor(OrderChangeEmpirePlan order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (order.ConstructionGameEntityGUID == GameEntityGUID.Zero)
		{
			Diagnostics.LogError("Skipping change empire plan process because the game entity guid is null.");
			yield break;
		}
		DepartmentOfPlanificationAndDevelopment.ConstructibleElement constructibleElement = null;
		Diagnostics.Assert(this.empirePlanDatabase != null);
		if (!this.empirePlanDatabase.TryGetValue(order.ConstructibleElementName, out constructibleElement))
		{
			Diagnostics.LogError("Skipping change empire plan process because the constructible element {0} is not in the constructible element database.", new object[]
			{
				order.ConstructibleElementName
			});
			yield break;
		}
		Diagnostics.Assert(constructibleElement != null);
		Diagnostics.Assert(this.empirePlanQueue != null);
		ReadOnlyCollection<Construction> empirePlanQueueItems = this.empirePlanQueue.PendingConstructions;
		if (empirePlanQueueItems != null)
		{
			for (int index = empirePlanQueueItems.Count - 1; index >= 0; index--)
			{
				Construction pendingConstruction = empirePlanQueueItems[index];
				Diagnostics.Assert(pendingConstruction != null);
				EmpirePlanDefinition empirePlanDefinition = pendingConstruction.ConstructibleElement as EmpirePlanDefinition;
				if (empirePlanDefinition == null)
				{
					Diagnostics.LogError("The construction should be a empire plan definition.");
				}
				else if (!(empirePlanDefinition.EmpirePlanClass != order.EmpirePlanClass))
				{
					for (int stockIndex = 0; stockIndex < pendingConstruction.CurrentConstructionStock.Length; stockIndex++)
					{
						ConstructionResourceStock stock = pendingConstruction.CurrentConstructionStock[stockIndex];
						if (stock.Stock > 0f)
						{
							if (!this.departmentOfTheTreasury.TryTransferResources(base.Empire.SimulationObject, stock.PropertyName, stock.Stock))
							{
								Diagnostics.LogError("Order preprocessing failed because the constructible element '{0}' ask for instant resource '{1}' that can't be retrieve.", new object[]
								{
									constructibleElement.Name,
									stock.PropertyName
								});
								yield break;
							}
						}
					}
					Diagnostics.Assert(this.gameEntityRepositoryService != null);
					this.gameEntityRepositoryService.Unregister(pendingConstruction.GUID);
					this.empirePlanQueue.Remove(pendingConstruction);
				}
			}
		}
		global::Empire empire = base.Empire as global::Empire;
		Diagnostics.Assert(empire != null);
		Diagnostics.Assert(empire.Faction != null, "Invalid null faction for empire '{0}'.", new object[]
		{
			empire.Index
		});
		Diagnostics.Assert(empire.Faction.AffinityMapping != null, "Invalid null Affinity mapping for faction '{0}'.", new object[]
		{
			empire.Faction.Name
		});
		Construction construction = new Construction(constructibleElement, order.ConstructionGameEntityGUID, empire.Faction.AffinityMapping.Name, empire);
		IDatabase<SimulationDescriptor> simulationDescriptorDatatable = Databases.GetDatabase<SimulationDescriptor>(false);
		SimulationDescriptor classImprovementDescriptor;
		if (simulationDescriptorDatatable != null && simulationDescriptorDatatable.TryGetValue("ClassConstruction", out classImprovementDescriptor))
		{
			construction.AddDescriptor(classImprovementDescriptor, false);
		}
		Diagnostics.Assert(this.departmentOfTheTreasury != null);
		if (constructibleElement.Costs != null && constructibleElement.Costs.Length > 0)
		{
			Diagnostics.Assert(construction.CurrentConstructionStock != null && construction.CurrentConstructionStock.Length == constructibleElement.Costs.Length);
			Diagnostics.Assert(order.ResourceStocks != null && order.ResourceStocks.Length == constructibleElement.Costs.Length);
			for (int index2 = 0; index2 < constructibleElement.Costs.Length; index2++)
			{
				Diagnostics.Assert(constructibleElement.Costs[index2] != null);
				Diagnostics.Assert(construction.CurrentConstructionStock[index2] != null);
				Diagnostics.Assert(order.ResourceStocks[index2] != null);
				construction.CurrentConstructionStock[index2].Stock = order.ResourceStocks[index2].Stock;
				if (order.ResourceStocks[index2].Stock > 0f && !this.departmentOfTheTreasury.TryTransferResources(base.Empire.SimulationObject, constructibleElement.Costs[index2].ResourceName, -order.ResourceStocks[index2].Stock))
				{
					Diagnostics.LogError("Order preprocessing failed because the constructible element '{0}' ask for instant resource '{1}' that can't be retrieve.", new object[]
					{
						constructibleElement.Name,
						constructibleElement.Costs[index2].ResourceName
					});
					yield break;
				}
			}
		}
		Diagnostics.Assert(this.empirePlanQueue != null);
		this.empirePlanQueue.Enqueue(construction);
		Diagnostics.Assert(this.gameEntityRepositoryService != null);
		this.gameEntityRepositoryService.Register(construction);
		this.OnEmpirePlanQueueChanged(construction, ConstructionChangeEventAction.Started);
		Diagnostics.Log("Process order: {0}.", new object[]
		{
			order.ToString()
		});
		yield break;
	}

	private bool SelloutTradableBoosterPreprocessor(OrderSelloutTradableBooster order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (StaticString.IsNullOrEmpty(order.BoosterDefinitionName))
		{
			Diagnostics.LogError("Preprocessor failed because the booster definition name is either null or empty.");
			return false;
		}
		if (order.GameEntityGUIDs == null)
		{
			Diagnostics.LogError("Preprocessor failed because the array of game entity guids is null.");
			return false;
		}
		if (order.GameEntityGUIDs.Length == 0)
		{
			return false;
		}
		DepartmentOfEducation agency = base.Empire.GetAgency<DepartmentOfEducation>();
		if (agency != null)
		{
			int index;
			for (index = 0; index < order.GameEntityGUIDs.Length; index++)
			{
				if (agency.VaultItems.FirstOrDefault((VaultItem predicate) => predicate.GUID == order.GameEntityGUIDs[index]) == null)
				{
					Diagnostics.LogError("Preprocessor failed because one of the listed game entity guids does not correspond to an existing vault item.");
					return false;
				}
			}
		}
		if (order.Quantity != (float)order.GameEntityGUIDs.Length)
		{
			Diagnostics.LogError("Preprocessor failed because the quantity of resources is not appropriate (booster definition name: '{0}', quantity: {1}, number of game entity guids: {2}).", new object[]
			{
				order.BoosterDefinitionName,
				order.Quantity,
				order.GameEntityGUIDs.Length
			});
			return false;
		}
		if (this.tradeManagementService == null)
		{
			return false;
		}
		Tradable tradable = this.tradeManagementService.CreateNewTradableBooster(order.BoosterDefinitionName, order.Quantity, order.GameEntityGUIDs);
		if (tradable == null)
		{
			return false;
		}
		order.Tradables = new ITradable[]
		{
			tradable
		};
		order.Price = tradable.GetPriceWithSalesTaxes(TradableTransactionType.Sellout, (global::Empire)base.Empire);
		order.UpdatedTradableCategoryTendency = this.tradeManagementService.UpdateTendency(tradable.TradableCategoryDefinition, Tradable.NegativeTendencyMultiplier * order.Quantity);
		return true;
	}

	private IEnumerator SelloutTradableBoosterProcessor(OrderSelloutTradableBooster order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		DepartmentOfEducation departmentOfEducation = base.Empire.GetAgency<DepartmentOfEducation>();
		if (departmentOfEducation != null)
		{
			for (int index = 0; index < order.GameEntityGUIDs.Length; index++)
			{
				departmentOfEducation.DestroyVaultItem(order.GameEntityGUIDs[index]);
			}
		}
		this.departmentOfTheTreasury.TryTransferResources(base.Empire, SimulationProperties.EmpireMoney, order.Price);
		Diagnostics.Assert(order.Tradables != null);
		Diagnostics.Assert(order.Tradables.Length == 1);
		TradableBooster tradableBooster = order.Tradables[0] as TradableBooster;
		if (tradableBooster != null && this.tradeManagementService != null)
		{
			bool collected = this.tradeManagementService.CollectTradableBooster(tradableBooster);
			if (collected)
			{
				this.tradeManagementService.SetTendency(tradableBooster.TradableCategoryDefinition.Name, order.UpdatedTradableCategoryTendency);
				this.tradeManagementService.NotifyTradableTransactionComplete(TradableTransactionType.Sellout, (global::Empire)base.Empire, tradableBooster, order.Quantity, order.Price);
			}
		}
		yield break;
	}

	private bool SelloutTradableHeroPreprocessor(OrderSelloutTradableHero order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (!order.GameEntityGUID.IsValid)
		{
			Diagnostics.LogError("Preprocessor failed because game entity guid is invalid.");
			return false;
		}
		IGameEntity gameEntity;
		if (!this.gameEntityRepositoryService.TryGetValue(order.GameEntityGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the game entity (guid: {0}) does not exist.", new object[]
			{
				order.GameEntityGUID
			});
			return false;
		}
		Unit unit = gameEntity as Unit;
		if (unit == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the game entity is not of type 'Unit'.");
			return false;
		}
		DepartmentOfEducation agency = base.Empire.GetAgency<DepartmentOfEducation>();
		if (agency != null && agency.Heroes.Contains(unit))
		{
			if (unit.CheckUnitAbility(UnitAbility.ReadonlyUnsalable, -1))
			{
				Diagnostics.LogError("Order preprocessing failed because the hero has the 'unsalable' ability.");
				return false;
			}
			if (this.tradeManagementService != null)
			{
				Tradable tradable = this.tradeManagementService.CreateNewTradableHero(unit);
				this.tradeManagementService.RefreshEmpireExclusionBits(tradable, base.Empire.Bits);
				order.Tradables = new ITradable[]
				{
					tradable
				};
				DepartmentOfScience agency2 = base.Empire.GetAgency<DepartmentOfScience>();
				if (agency2.CanTradeHeroes(false))
				{
					order.Price = tradable.GetPriceWithSalesTaxes(TradableTransactionType.Sellout, (global::Empire)base.Empire);
				}
				else
				{
					order.Price = 0f;
				}
				order.UpdatedTradableCategoryTendency = this.tradeManagementService.UpdateTendency(tradable.TradableCategoryDefinition, Tradable.NegativeTendencyMultiplier * order.Quantity);
				return true;
			}
		}
		return false;
	}

	private IEnumerator SelloutTradableHeroProcessor(OrderSelloutTradableHero order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		Diagnostics.Assert(order.GameEntityGUID.IsValid);
		IGameEntity gameEntity;
		if (this.gameEntityRepositoryService.TryGetValue(order.GameEntityGUID, out gameEntity))
		{
			Unit unit = gameEntity as Unit;
			if (unit != null)
			{
				DepartmentOfEducation departmentOfEducation = base.Empire.GetAgency<DepartmentOfEducation>();
				departmentOfEducation.InternalRemoveHero(unit);
				this.gameEntityRepositoryService.Unregister(unit);
				this.departmentOfTheTreasury.TryTransferResources(base.Empire, SimulationProperties.EmpireMoney, order.Price);
				Diagnostics.Assert(order.Tradables != null);
				Diagnostics.Assert(order.Tradables.Length == 1);
				TradableUnit tradableUnit = order.Tradables[0] as TradableUnit;
				if (tradableUnit != null && this.tradeManagementService != null)
				{
					UnitDesign unitDesign = unit.UnitDesign;
					bool collected = this.tradeManagementService.CollectTradableUnit(tradableUnit, unit, base.Empire.Index);
					if (collected)
					{
						this.tradeManagementService.SetTendency(tradableUnit.TradableCategoryDefinition.Name, order.UpdatedTradableCategoryTendency);
						this.tradeManagementService.NotifyTradableTransactionComplete(TradableTransactionType.Sellout, (global::Empire)base.Empire, tradableUnit, order.Quantity, order.Price);
						unitDesign.Barcode = tradableUnit.Barcode;
					}
				}
			}
		}
		yield break;
	}

	private bool SelloutTradableResourcePreprocessor(OrderSelloutTradableResource order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (string.IsNullOrEmpty(order.ResourceName))
		{
			Diagnostics.LogError("Preprocessor failed because the resource name is either null or empty.");
			return false;
		}
		if (order.Quantity <= 0f)
		{
			Diagnostics.LogError("Preprocessor failed because the quantity of resources is not appropriate (resource name: '{0}', quantity: {1}).", new object[]
			{
				order.ResourceName,
				order.Quantity
			});
			return false;
		}
		StaticString resourceName = order.ResourceName;
		if (!order.IsMagicSellout)
		{
			float num = -order.Quantity;
			bool flag = this.departmentOfTheTreasury.IsTransferOfResourcePossible(base.Empire, resourceName, ref num);
			if (!flag || num != -order.Quantity)
			{
				Diagnostics.LogError("Preprocessor failed because the transfer of resources is not possible (resource name: '{0}', amount: {1}).", new object[]
				{
					order.ResourceName,
					num
				});
				return false;
			}
		}
		Diagnostics.Assert(this.tradeManagementService != null);
		Tradable tradable = this.tradeManagementService.CreateNewTradableResources(resourceName, order.Quantity);
		order.Tradables = new ITradable[]
		{
			tradable
		};
		order.Price = tradable.GetPriceWithSalesTaxes(TradableTransactionType.Sellout, (global::Empire)base.Empire, order.Quantity);
		order.UpdatedTradableCategoryTendency = this.tradeManagementService.UpdateTendency(tradable.TradableCategoryDefinition, Tradable.NegativeTendencyMultiplier * order.Quantity);
		return true;
	}

	private IEnumerator SelloutTradableResourceProcessor(OrderSelloutTradableResource order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (!order.IsMagicSellout)
		{
			this.departmentOfTheTreasury.TryTransferResources(base.Empire, order.ResourceName, -order.Quantity);
			this.departmentOfTheTreasury.TryTransferResources(base.Empire, SimulationProperties.EmpireMoney, order.Price);
		}
		Diagnostics.Assert(order.Tradables != null);
		Diagnostics.Assert(order.Tradables.Length == 1);
		TradableResource tradableResource = order.Tradables[0] as TradableResource;
		if (tradableResource != null)
		{
			Diagnostics.Assert(this.tradeManagementService != null);
			bool collected = this.tradeManagementService.CollectTradableResource(tradableResource);
			if (collected)
			{
				this.tradeManagementService.SetTendency(tradableResource.TradableCategoryDefinition.Name, order.UpdatedTradableCategoryTendency);
				this.tradeManagementService.NotifyTradableTransactionComplete(TradableTransactionType.Sellout, (global::Empire)base.Empire, tradableResource, order.Quantity, order.Price);
			}
		}
		yield break;
	}

	private bool SelloutTradableUnitsPreprocessor(OrderSelloutTradableUnits order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (order.GameEntityGUIDs == null || order.GameEntityGUIDs.Length == 0)
		{
			return false;
		}
		for (int i = 0; i < order.GameEntityGUIDs.Length; i++)
		{
			if (!order.GameEntityGUIDs[i].IsValid)
			{
				Diagnostics.LogError("Preprocessor failed because game entity guid is invalid.");
				return false;
			}
			IGameEntity gameEntity;
			if (!this.gameEntityRepositoryService.TryGetValue(order.GameEntityGUIDs[i], out gameEntity))
			{
				Diagnostics.LogError("Order preprocessing failed because the game entity (guid: {0}) does not exist.", new object[]
				{
					order.GameEntityGUIDs[i]
				});
				return false;
			}
			Unit unit = gameEntity as Unit;
			if (unit == null)
			{
				Diagnostics.LogError("Order preprocessing failed because the game entity is not of type 'Unit'.");
				return false;
			}
			if (unit.CheckUnitAbility(UnitAbility.ReadonlyUnsalable, -1))
			{
				Diagnostics.LogError("Order preprocessing failed because unit (name: '{0}') has the 'unsalable' ability.", new object[]
				{
					unit.Name
				});
				return false;
			}
		}
		DepartmentOfTransportation agency = base.Empire.GetAgency<DepartmentOfTransportation>();
		List<ArmyGoToInstruction> armiesWithPendingGoToInstructions = agency.ArmiesWithPendingGoToInstructions;
		for (int j = 0; j < order.GameEntityGUIDs.Length; j++)
		{
			IGameEntity gameEntity2;
			if (this.gameEntityRepositoryService.TryGetValue(order.GameEntityGUIDs[j], out gameEntity2))
			{
				Unit unit2 = gameEntity2 as Unit;
				Diagnostics.Assert(unit2 != null);
				if (unit2.Garrison != null)
				{
					if (unit2.Garrison.IsInEncounter)
					{
						return false;
					}
					GameEntityGUID garrisonGUID = unit2.Garrison.GUID;
					Diagnostics.Assert(armiesWithPendingGoToInstructions != null);
					ArmyGoToInstruction armyGoToInstruction = armiesWithPendingGoToInstructions.Find((ArmyGoToInstruction match) => match.ArmyGUID == garrisonGUID);
					if (armyGoToInstruction != null)
					{
						armyGoToInstruction.Cancel(true);
						armiesWithPendingGoToInstructions.Remove(armyGoToInstruction);
					}
				}
			}
		}
		if (this.tradeManagementService != null)
		{
			order.Tradables = new ITradable[order.GameEntityGUIDs.Length];
			order.Price = 0f;
			DepartmentOfScience agency2 = base.Empire.GetAgency<DepartmentOfScience>();
			bool flag = agency2.CanTradeUnits(false);
			float num = 0f;
			Tradable tradable = null;
			for (int k = 0; k < order.GameEntityGUIDs.Length; k++)
			{
				IGameEntity gameEntity3;
				if (this.gameEntityRepositoryService.TryGetValue(order.GameEntityGUIDs[k], out gameEntity3))
				{
					Tradable tradable2 = this.tradeManagementService.CreateNewTradableUnit(gameEntity3 as Unit);
					if (tradable2 != null)
					{
						(tradable2 as TradableUnit).LastKnownOrigin = (TradableOrigin)base.Empire.Index;
						this.tradeManagementService.RefreshEmpireExclusionBits(tradable2, base.Empire.Bits);
						order.Tradables[k] = tradable2;
						num += 1f;
						if (flag)
						{
							order.Price += tradable2.GetPriceWithSalesTaxes(TradableTransactionType.Sellout, (global::Empire)base.Empire);
						}
						tradable = tradable2;
					}
				}
			}
			if (tradable != null)
			{
				order.UpdatedTradableCategoryTendency = this.tradeManagementService.UpdateTendency(tradable.TradableCategoryDefinition, Tradable.NegativeTendencyMultiplier * num);
			}
			return true;
		}
		return false;
	}

	private IEnumerator SelloutTradableUnitsProcessor(OrderSelloutTradableUnits order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		Diagnostics.Assert(order.GameEntityGUIDs != null);
		Diagnostics.Assert(order.Tradables != null);
		Diagnostics.Assert(order.Tradables.Length == order.GameEntityGUIDs.Length);
		List<Army> besiegingSeafaringArmies = DepartmentOfTheInterior.GetBesiegingSeafaringArmies(order.GameEntityGUIDs);
		DepartmentOfScience departmentOfScience = base.Empire.GetAgency<DepartmentOfScience>();
		bool canTradeUnits = departmentOfScience.CanTradeUnits(false);
		DepartmentOfDefense departmentOfDefense = base.Empire.GetAgency<DepartmentOfDefense>();
		TradableUnit tradableUnit = null;
		TradableUnit anyTradableUnitForTheTendencyUpdate = null;
		for (int index = 0; index < order.GameEntityGUIDs.Length; index++)
		{
			IGameEntity gameEntity;
			if (this.gameEntityRepositoryService.TryGetValue(order.GameEntityGUIDs[index], out gameEntity))
			{
				Unit unit = gameEntity as Unit;
				if (unit != null)
				{
					Army army = unit.Garrison as Army;
					if (army != null)
					{
						army.SetWorldPathWithEstimatedTimeOfArrival(null, global::Game.Time);
					}
					if (unit.Garrison != null)
					{
						IGarrison garrison = unit.Garrison;
						garrison.RemoveUnit(unit);
						if (garrison is Army)
						{
							if (garrison.IsEmpty)
							{
								departmentOfDefense.RemoveArmy(garrison as Army);
								if (besiegingSeafaringArmies != null)
								{
									besiegingSeafaringArmies.Remove(garrison as Army);
								}
							}
							else
							{
								army.SetSails();
								army.Refresh(false);
							}
						}
					}
					this.gameEntityRepositoryService.Unregister(unit);
					tradableUnit = (order.Tradables[index] as TradableUnit);
					if (tradableUnit != null)
					{
						if (this.tradeManagementService != null)
						{
							UnitDesign unitDesign = unit.UnitDesign;
							bool collected = this.tradeManagementService.CollectTradableUnit(tradableUnit, unit, base.Empire.Index);
							if (collected)
							{
								if (canTradeUnits)
								{
									this.tradeManagementService.NotifyTradableTransactionComplete(TradableTransactionType.Sellout, (global::Empire)base.Empire, tradableUnit, order.Quantity, order.Price);
								}
								unitDesign.Barcode = tradableUnit.Barcode;
							}
						}
						anyTradableUnitForTheTendencyUpdate = tradableUnit;
					}
				}
			}
		}
		if (anyTradableUnitForTheTendencyUpdate != null)
		{
			this.tradeManagementService.SetTendency(anyTradableUnitForTheTendencyUpdate.TradableCategoryDefinition.Name, order.UpdatedTradableCategoryTendency);
		}
		this.departmentOfTheTreasury.TryTransferResources(base.Empire, SimulationProperties.EmpireMoney, order.Price);
		if (besiegingSeafaringArmies != null)
		{
			DepartmentOfTheInterior.CheckBesiegingSeafaringArmyStatus(besiegingSeafaringArmies, DepartmentOfTheInterior.BesiegingSeafaringArmyStatus.CityDefensePointLossPerTurn);
		}
		yield break;
	}

	private bool SwitchCheatModePreprocessor(OrderSwitchCheatMode order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (!Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools && Amplitude.Unity.Framework.Application.Version.Accessibility > Accessibility.Internal)
		{
			Diagnostics.LogWarning("You don't have the right to do that ! It's only for developement purpose.");
			return false;
		}
		return true;
	}

	private IEnumerator SwitchCheatModeProcessor(OrderSwitchCheatMode order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		Diagnostics.Log("[Order] {0}", new object[]
		{
			order
		});
		Diagnostics.Assert(base.Empire != null);
		if (order.CheatState)
		{
			IDatabase<SimulationDescriptor> simulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
			Diagnostics.Assert(simulationDescriptorDatabase != null);
			SimulationDescriptor cheatDescriptor;
			if (!simulationDescriptorDatabase.TryGetValue("IAmACheater", out cheatDescriptor))
			{
				Diagnostics.LogError("Order processing failed because we can't find the cheat descriptor.");
			}
			base.Empire.SwapDescriptor(cheatDescriptor);
		}
		else
		{
			base.Empire.RemoveDescriptorByType("Cheat");
		}
		yield break;
	}

	public int StatAllianceDeclarations { get; set; }

	public int StatCityConqueredCurrentWinter { get; set; }

	public int StatConstructionBuilt { get; set; }

	public float StatDustSpentMarket { get; set; }

	public int StatQuestCompleted { get; set; }

	public int StatNecrophagesCadaverBoosterUsed { get; set; }

	public int StatNecrophagesBattlebornBoosterUsed { get; set; }

	public int StatPeaceDeclarations { get; set; }

	public int StatWarDeclarations { get; set; }

	public ISeasonService SeasonService
	{
		get
		{
			return this.seasonService;
		}
		set
		{
			if (this.seasonService != null)
			{
				this.seasonService.SeasonChange -= this.SeasonService_SeasonChange;
			}
			this.seasonService = value;
			if (this.seasonService != null)
			{
				this.seasonService.SeasonChange += this.SeasonService_SeasonChange;
			}
		}
	}

	public Dictionary<GameEntityGUID, Booster> Boosters
	{
		get
		{
			return this.boosters;
		}
	}

	public int EmpirePlanChoiceRemainingTurn { get; private set; }

	public bool IsEmpirePlanChoiced
	{
		get
		{
			global::Empire empire = base.Empire as global::Empire;
			Diagnostics.Assert(empire != null);
			return empire.IsControlledByAI || this.isEmpirePlanChoiced;
		}
		set
		{
			this.isEmpirePlanChoiced = value;
		}
	}

	public bool IsEmpirePlanChoiceTurn
	{
		get
		{
			return this.EmpirePlanChoiceRemainingTurn == 0;
		}
	}

	public int EmpirePlanPeriod
	{
		get
		{
			return this.empirePlanPeriod;
		}
	}

	public SimulationObjectWrapper EmpirePlanSimulationWrapper
	{
		get
		{
			return this.empirePlanSimulationWrapper;
		}
	}

	public int TurnWhenTradeRoutesWereLastCreated { get; set; }

	public int LocalWinterImmunityWinnerBid { get; set; }

	public int LocalWinterImmunityWinnerIndex
	{
		get
		{
			return this.localWinterImmunityWinnerIndex;
		}
		set
		{
			this.localWinterImmunityWinnerIndex = value;
		}
	}

	public Booster GetActiveBooster(StaticString boosterDefinitionName)
	{
		return this.boosters.Values.FirstOrDefault((Booster match) => match.BoosterDefinition.Name == boosterDefinitionName);
	}

	public Booster[] GetActiveBoosters()
	{
		return this.boosters.Values.ToArray<Booster>();
	}

	public bool IsThereSomeActiveStrategicResourceBooster()
	{
		foreach (Booster booster in this.boosters.Values)
		{
			if (booster.BoosterDefinition.XmlSerializableName.Contains("Strategic"))
			{
				return true;
			}
		}
		return false;
	}

	public void RemoveBoostersFromTarget(GameEntityGUID targetGuid, int instigatorEmpire)
	{
		if (this.boosters.Count > 0)
		{
			List<Booster> list = new List<Booster>();
			foreach (Booster booster in this.boosters.Values)
			{
				if (booster.TargetGUID == targetGuid && (instigatorEmpire == -1 || booster.InstigatorEmpireIndex == instigatorEmpire))
				{
					list.Add(booster);
				}
			}
			IEventService service = Services.GetService<IEventService>();
			foreach (Booster booster2 in list)
			{
				this.boosters.Remove(booster2.GUID);
				this.OnBoosterCollectionChange(new BoosterCollectionChangeEventArgs(BoosterCollectionChangeAction.Remove, booster2));
				this.gameEntityRepositoryService.Unregister(booster2);
				if (service != null && booster2.InstigatorEmpireIndex == base.Empire.Index)
				{
					service.Notify(new EventBoosterEnded(base.Empire, booster2.BoosterDefinition));
				}
				booster2.Dispose();
			}
		}
	}

	protected virtual void OnBoosterCollectionChange(BoosterCollectionChangeEventArgs e)
	{
		if (this.BoosterCollectionChange != null)
		{
			this.BoosterCollectionChange(this, e);
		}
	}

	protected override IEnumerator OnInitialize()
	{
		yield return base.OnInitialize();
		this.EmpirePlanChoiceRemainingTurn = int.MaxValue;
		this.simulationDescriptorsDatatable = Databases.GetDatabase<SimulationDescriptor>(false);
		this.empirePlanDatabase = Databases.GetDatabase<DepartmentOfPlanificationAndDevelopment.ConstructibleElement>(false);
		this.departmentOfTheTreasury = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		Diagnostics.Assert(this.departmentOfTheTreasury != null);
		this.departmentOfTheInterior = base.Empire.GetAgency<DepartmentOfTheInterior>();
		Diagnostics.Assert(this.departmentOfTheInterior != null);
		this.eventService = Services.GetService<IEventService>();
		IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
		Diagnostics.Assert(this.eventService != null);
		Diagnostics.Assert(base.Empire != null);
		base.Empire.RegisterPass("GameClientState_Turn_Begin", "NotifyEmpirePlanChoiceTurn", new Agency.Action(this.GameClientState_Turn_Begin_NotifyEmpirePlanChoiceTurn), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_Begin", "CreateNewTradeRoutes", new Agency.Action(this.GameClientState_Turn_Begin_CreateNewTradeRoutes), new string[]
		{
			"ClearAllTradeRoutes"
		});
		base.Empire.RegisterPass("GameClientState_Turn_Begin", "ApplyTradeRoutesSimulation", new Agency.Action(this.GameClientState_Turn_Begin_ApplyTradeRoutesSimulation), new string[]
		{
			"CreateNewTradeRoutes"
		});
		base.Empire.RegisterPass("GameClientState_Turn_Begin", "BoostersNotification", new Agency.Action(this.GameClientState_Turn_Begin_BoostersNotification), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_Begin", "NumberOfTurnsSinceSeasonStarted", new Agency.Action(this.GameClientState_Turn_Begin_NumberOfTurnsSinceSeasonStart), new string[0]);
		if (DepartmentOfTheInterior.CanInvokePillarsAndSpells(base.Empire as global::Empire))
		{
			base.Empire.RegisterPass("GameClientState_Turn_Begin", "PillarsNotification", new Agency.Action(this.GameClientState_Turn_Begin_PillarsNotification), new string[0]);
		}
		if (downloadableContentService != null && downloadableContentService.IsShared(DownloadableContent13.ReadOnlyName))
		{
			base.Empire.RegisterPass("GameClientState_Turn_End", "UpdateLocalWinterImmunity", new Agency.Action(this.GameClientState_Turn_End_UpdateLocalWinterImmunity), new string[0]);
		}
		base.Empire.RegisterPass("GameClientState_Turn_End", "ResetEmpirePlan", new Agency.Action(this.GameClientState_Turn_End_ResetEmpirePlan), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_End", "ComputeEmpirePoint", new Agency.Action(this.GameClientState_Turn_End_ComputeEmpirePoint), new string[]
		{
			"CollectResources",
			"ResetEmpirePlan"
		});
		base.Empire.RegisterPass("GameClientState_Turn_End", "ApplyEmpirePlan", new Agency.Action(this.GameClientState_Turn_End_ApplyEmpirePlan), new string[]
		{
			"ComputeEmpirePoint"
		});
		base.Empire.RegisterPass("GameClientState_Turn_End", "RefreshEmpirePlanChoiceRemainingTurn", new Agency.Action(this.GameClientState_Turn_End_RefreshEmpirePlanChoiceRemainingTurn), new string[]
		{
			"ApplyEmpirePlan"
		});
		base.Empire.RegisterPass("GameClientState_Turn_End", "RefreshBoosters", new Agency.Action(this.GameClientState_Turn_End_RefreshBoosters), new string[]
		{
			"CollectResources"
		});
		base.Empire.RegisterPass("GameClientState_Turn_End", "BoostersAutoBuyOut", new Agency.Action(this.GameClientState_Turn_End_BoostersAutoBuyOut), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_End", "ClearAllTradeRoutes", new Agency.Action(this.GameClientState_Turn_End_ClearAllTradeRoutes), new string[]
		{
			"CollectResources"
		});
		this.empirePlanSimulationWrapper = new SimulationObjectWrapper("EmpirePlan");
		Diagnostics.Assert(this.simulationDescriptorsDatatable != null);
		SimulationDescriptor descriptor;
		if (!this.simulationDescriptorsDatatable.TryGetValue("ClassEmpirePlan", out descriptor))
		{
			Diagnostics.LogError("Can't found Class Empire Plan descriptor.");
		}
		this.empirePlanSimulationWrapper.AddDescriptor(descriptor, false);
		if (!this.simulationDescriptorsDatatable.TryGetValue("ClassTimedBonus", out descriptor))
		{
			Diagnostics.LogError("Can't found Class Timed bonus descriptor.");
		}
		this.empirePlanSimulationWrapper.AddDescriptor(descriptor, false);
		Diagnostics.Assert(base.Empire != null);
		base.Empire.AddChild(this.empirePlanSimulationWrapper);
		Diagnostics.Assert(this.empirePlanDefinitionsByClass != null);
		Diagnostics.Assert(this.empirePlanDatabase != null);
		this.empirePlanDefinitionsByClass.Clear();
		DepartmentOfPlanificationAndDevelopment.ConstructibleElement[] empirePlans = (from predicate in this.empirePlanDatabase
		where predicate is EmpirePlanDefinition
		select predicate).ToArray<DepartmentOfPlanificationAndDevelopment.ConstructibleElement>();
		for (int index = 0; index < empirePlans.Length; index++)
		{
			EmpirePlanDefinition empirePlan = empirePlans[index] as EmpirePlanDefinition;
			if (empirePlan != null && !(empirePlan.EmpirePlanClass == null) && !this.empirePlanDefinitionsByClass.ContainsKey(empirePlan.EmpirePlanClass))
			{
				DepartmentOfPlanificationAndDevelopment.ConstructibleElement[] constructibleElements = (from empirePlanDefinition in empirePlans
				where (empirePlanDefinition as EmpirePlanDefinition).EmpirePlanClass == empirePlan.EmpirePlanClass
				select empirePlanDefinition).ToArray<DepartmentOfPlanificationAndDevelopment.ConstructibleElement>();
				List<EmpirePlanDefinition> empirePlanDefinitions = Array.ConvertAll<DepartmentOfPlanificationAndDevelopment.ConstructibleElement, EmpirePlanDefinition>(constructibleElements, (DepartmentOfPlanificationAndDevelopment.ConstructibleElement input) => input as EmpirePlanDefinition).ToList<EmpirePlanDefinition>();
				for (int i = 0; i < constructibleElements.Length; i++)
				{
					if (!this.CheckEmpireplanPrerequisite(constructibleElements[i] as EmpirePlanDefinition))
					{
						empirePlanDefinitions.Remove(constructibleElements[i] as EmpirePlanDefinition);
					}
				}
				this.empirePlanDefinitionsByClass.Add(empirePlan.EmpirePlanClass, empirePlanDefinitions.ToArray());
			}
		}
		Diagnostics.Assert(this.currentEmpirePlanDefinitionByClass != null);
		this.currentEmpirePlanDefinitionByClass.Clear();
		StaticString[] empirePlanClasses = this.GetEmpirePlanClasses();
		if (empirePlanClasses != null)
		{
			foreach (StaticString empirePlanClass in empirePlanClasses)
			{
				Diagnostics.Assert(empirePlanClass != null);
				EmpirePlanDefinition currentPlan = this.GetEmpirePlanDefinition(empirePlanClass, 0);
				Diagnostics.Assert(this.currentEmpirePlanDefinitionByClass != null);
				Diagnostics.Assert(currentPlan != null, "There is no level 0 for empire plan {0}.", new object[]
				{
					empirePlanClass
				});
				this.UnlockEmpirePlan(currentPlan);
			}
		}
		this.departmentOfTheTreasury.ResourcePropertyChange += this.DepartmentOfTheTreasury_ResourcePropertyChange;
		IEventService eventService = Services.GetService<IEventService>();
		if (eventService != null)
		{
			eventService.EventRaise += this.EventService_EventRaise;
		}
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		yield break;
	}

	protected override IEnumerator OnLoadGame(Amplitude.Unity.Game.Game game)
	{
		yield return base.OnLoadGame(game);
		if (game == null)
		{
			throw new ArgumentNullException("game");
		}
		this.pillarRepositoryService = game.Services.GetService<IPillarRepositoryService>();
		this.gameEntityRepositoryService = game.Services.GetService<IGameEntityRepositoryService>();
		if (this.gameEntityRepositoryService == null)
		{
			Diagnostics.LogError("Failed to retrieve the game entity repository service.");
		}
		this.tradeManagementService = game.Services.GetService<ITradeManagementService>();
		if (this.tradeManagementService == null)
		{
			Diagnostics.LogError("Failed to retrieve the trade management service.");
		}
		this.playerControllerRepositoryService = game.Services.GetService<IPlayerControllerRepositoryService>();
		if (this.playerControllerRepositoryService == null)
		{
			Diagnostics.LogError("Failed to retrieve the player controller repository service.");
		}
		this.SeasonService = game.Services.GetService<ISeasonService>();
		if (this.SeasonService == null)
		{
			Diagnostics.LogError("Failed to retrieve the season service.");
		}
		this.downloadableContentService = Services.GetService<IDownloadableContentService>();
		if (this.downloadableContentService == null)
		{
			Diagnostics.LogError("Failed to retrieve the downloadable content service.");
		}
		this.LoadDeserializedBoosters();
		string empirePlanPeriodFormulaString = Amplitude.Unity.Runtime.Runtime.Registry.GetValue("Gameplay/Agencies/DepartmentOfPlanificationAndDevelopment/EmpirePlanPeriod");
		object[] empirePlanPeriodFormulaTokens = Interpreter.InfixTransform(empirePlanPeriodFormulaString);
		InterpreterContext interpreterContext = new InterpreterContext(base.Empire.SimulationObject);
		object result = Interpreter.Execute(empirePlanPeriodFormulaTokens, interpreterContext);
		if (result is float)
		{
			this.empirePlanPeriod = Mathf.RoundToInt((float)result) - 1;
		}
		else
		{
			Diagnostics.LogWarning("Can't get the EmpirePlanPeriod value in registry, default value will be 20 turns.");
		}
		this.empirePlanImminentNotificationTurnCount = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<int>("Gameplay/Agencies/DepartmentOfPlanificationAndDevelopment/EmpirePlanImminentNotificationTurnCount", 5);
		this.empirePlanImminentNotificationTurnCount = Mathf.RoundToInt((float)this.empirePlanImminentNotificationTurnCount * base.Empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier));
		this.maximalTurnWithoutBooster = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<int>("Gameplay/Agencies/DepartmentOfPlanificationAndDevelopment/MaximalTurnWithoutBooster", this.maximalTurnWithoutBooster);
		this.maximalTurnWithoutPillar = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<int>("Gameplay/Agencies/DepartmentOfPlanificationAndDevelopment/MaximalTurnWithoutPillar", this.maximalTurnWithoutPillar);
		if (this.EmpirePlanChoiceRemainingTurn == 2147483647)
		{
			this.EmpirePlanChoiceRemainingTurn = this.empirePlanPeriod;
		}
		if ((game as global::Game).Turn == 0)
		{
			this.StatCityConqueredCurrentWinter = 0;
			this.StatConstructionBuilt = 0;
			this.StatNecrophagesCadaverBoosterUsed = 0;
			this.StatNecrophagesBattlebornBoosterUsed = 0;
			this.StatWarDeclarations = 0;
			this.StatAllianceDeclarations = 0;
			this.StatPeaceDeclarations = 0;
			this.StatQuestCompleted = 0;
			this.StatDustSpentMarket = 0f;
		}
		yield break;
	}

	protected override void OnRelease()
	{
		base.OnRelease();
		if (this.empirePlanSimulationWrapper != null)
		{
			this.empirePlanSimulationWrapper.Dispose();
			this.empirePlanSimulationWrapper = null;
		}
		this.empirePlanDefinitionsByClass.Clear();
		this.currentEmpirePlanDefinitionByClass.Clear();
		if (this.empirePlanQueue != null)
		{
			this.empirePlanQueue.Dispose();
			this.empirePlanQueue = null;
		}
		this.simulationDescriptorsDatatable = null;
		this.empirePlanDatabase = null;
		this.eventService = null;
		this.gameEntityRepositoryService = null;
		this.SeasonService = null;
		if (this.departmentOfTheTreasury != null)
		{
			this.departmentOfTheTreasury.ResourcePropertyChange -= this.DepartmentOfTheTreasury_ResourcePropertyChange;
			this.departmentOfTheTreasury = null;
		}
		IEventService service = Services.GetService<IEventService>();
		if (service != null)
		{
			service.EventRaise -= this.EventService_EventRaise;
		}
	}

	private void EventService_EventRaise(object sender, EventRaiseEventArgs e)
	{
		if (e.RaisedEvent.EventName == EventCitySiegeUpdate.Name)
		{
			EventCitySiegeUpdate eventCitySiegeUpdate = e.RaisedEvent as EventCitySiegeUpdate;
			DepartmentOfTheInterior agency = base.Empire.GetAgency<DepartmentOfTheInterior>();
			Diagnostics.Assert(agency != null);
			foreach (City city in agency.Cities)
			{
				bool flag = false;
				Diagnostics.Assert(city.TradeRoutes != null);
				for (int i = 0; i < city.TradeRoutes.Count; i++)
				{
					TradeRoute tradeRoute = city.TradeRoutes[i];
					bool flag2 = false;
					if (city.Region.Index == eventCitySiegeUpdate.City.Region.Index)
					{
						flag2 = true;
					}
					else
					{
						for (int j = 0; j < tradeRoute.IntermediateRegions.Count; j++)
						{
							short num = tradeRoute.IntermediateRegions[j];
							if ((int)num == eventCitySiegeUpdate.City.Region.Index)
							{
								flag2 = true;
								break;
							}
						}
					}
					if (flag2)
					{
						if (tradeRoute.SimulationObject != null)
						{
							tradeRoute.SimulationObject.RemoveDescriptorByName(TradeRoute.TradeRouteStatusSiegeBlocked);
							flag2 &= this.CheckWhetherTradeRouteIsRealyAffectedBySiege(eventCitySiegeUpdate.City);
							SimulationDescriptor descriptor;
							if (flag2 && eventCitySiegeUpdate.City.BesiegingEmpireIndex != -1 && this.simulationDescriptorsDatatable.TryGetValue(TradeRoute.TradeRouteStatusSiegeBlocked, out descriptor))
							{
								tradeRoute.SimulationObject.AddDescriptor(descriptor);
							}
							tradeRoute.SimulationObject.Refresh();
							flag = true;
							this.OnTradeRouteChanged(tradeRoute);
						}
						if (flag2)
						{
							bool positive = eventCitySiegeUpdate.City.BesiegingEmpireIndex == -1;
							this.UpdateIntermediateRegions(tradeRoute, positive);
						}
					}
				}
				if (flag)
				{
					city.Refresh(false);
				}
			}
		}
		if (e.RaisedEvent.EventName == EventCityDestroyed.Name)
		{
			DepartmentOfTheInterior agency2 = base.Empire.GetAgency<DepartmentOfTheInterior>();
			Diagnostics.Assert(agency2 != null);
			DepartmentOfForeignAffairs agency3 = base.Empire.GetAgency<DepartmentOfForeignAffairs>();
			Diagnostics.Assert(agency3 != null);
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			Diagnostics.Assert(service.Game != null);
			Diagnostics.Assert(service.Game is global::Game);
			IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
			foreach (City city2 in agency2.Cities)
			{
				bool flag3 = false;
				Diagnostics.Assert(city2.TradeRoutes != null);
				for (int k = 0; k < city2.TradeRoutes.Count; k++)
				{
					TradeRoute tradeRoute2 = city2.TradeRoutes[k];
					bool flag4 = false;
					for (int l = 0; l < tradeRoute2.IntermediateRegions.Count; l++)
					{
						short regionIndex = tradeRoute2.IntermediateRegions[l];
						Region region = service2.GetRegion((int)regionIndex);
						if (region == null || region.City == null)
						{
							flag4 = true;
							break;
						}
					}
					if (flag4)
					{
						SimulationDescriptor descriptor2;
						if (tradeRoute2.SimulationObject != null && this.simulationDescriptorsDatatable.TryGetValue(TradeRoute.TradeRouteStatusBroken, out descriptor2) && !tradeRoute2.SimulationObject.Tags.Contains(TradeRoute.TradeRouteStatusBroken))
						{
							tradeRoute2.SimulationObject.AddDescriptor(descriptor2);
							tradeRoute2.SimulationObject.Refresh();
							flag3 = true;
							this.OnTradeRouteChanged(tradeRoute2);
						}
						this.UpdateIntermediateRegions(tradeRoute2, false);
					}
				}
				if (flag3)
				{
					city2.Refresh(false);
				}
			}
		}
		if (e.RaisedEvent.EventName == EventSwapCity.Name)
		{
			EventSwapCity eventSwapCity = e.RaisedEvent as EventSwapCity;
			DepartmentOfTheInterior agency4 = base.Empire.GetAgency<DepartmentOfTheInterior>();
			Diagnostics.Assert(agency4 != null);
			DepartmentOfForeignAffairs agency5 = base.Empire.GetAgency<DepartmentOfForeignAffairs>();
			Diagnostics.Assert(agency5 != null);
			IGameService service3 = Services.GetService<IGameService>();
			Diagnostics.Assert(service3 != null);
			Diagnostics.Assert(service3.Game != null);
			Diagnostics.Assert(service3.Game is global::Game);
			IWorldPositionningService service4 = service3.Game.Services.GetService<IWorldPositionningService>();
			foreach (City city3 in agency4.Cities)
			{
				bool flag5 = false;
				if (city3.Hero != null)
				{
					flag5 = city3.Hero.IsSkillUnlocked(DepartmentOfPlanificationAndDevelopment.HeroSkillGovernor25);
				}
				bool flag6 = false;
				Diagnostics.Assert(city3.TradeRoutes != null);
				for (int m = 0; m < city3.TradeRoutes.Count; m++)
				{
					TradeRoute tradeRoute3 = city3.TradeRoutes[m];
					bool flag7 = false;
					for (int n = 0; n < tradeRoute3.IntermediateRegions.Count; n++)
					{
						short regionIndex2 = tradeRoute3.IntermediateRegions[n];
						Region region2 = service4.GetRegion((int)regionIndex2);
						if (region2 != null && region2.City != null)
						{
							if (eventSwapCity.City == region2.City)
							{
								Diagnostics.Assert(region2.City.Empire != null);
								DiplomaticRelation diplomaticRelation = agency5.GetDiplomaticRelation(region2.City.Empire);
								if (!flag5 && !diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.TradeRoute))
								{
									flag7 = true;
									break;
								}
							}
						}
					}
					if ((tradeRoute3.PathfindingMovementCapacity & PathfindingMovementCapacity.Water) == PathfindingMovementCapacity.Water && tradeRoute3.IntermediateRegionsOfTypeOceanic != null && tradeRoute3.IntermediateRegionsOfTypeOceanic.Count != 0)
					{
						bool flag8 = this.UpdateRelationForTradeRouteWithIntermediateRegionsOfTypeOceanic(city3, tradeRoute3, ref flag7);
						flag6 = (flag6 || flag8);
					}
					if (flag7)
					{
						SimulationDescriptor descriptor3;
						if (tradeRoute3.SimulationObject != null && this.simulationDescriptorsDatatable.TryGetValue(TradeRoute.TradeRouteStatusRelationBlocked, out descriptor3) && !tradeRoute3.SimulationObject.Tags.Contains(TradeRoute.TradeRouteStatusRelationBlocked))
						{
							tradeRoute3.SimulationObject.AddDescriptor(descriptor3);
							tradeRoute3.SimulationObject.Refresh();
							flag6 = true;
							this.OnTradeRouteChanged(tradeRoute3);
						}
						this.UpdateIntermediateRegions(tradeRoute3, false);
					}
				}
				if (flag6)
				{
					city3.Refresh(false);
				}
			}
		}
		if (e.RaisedEvent.EventName == EventDiplomaticRelationStateChange.Name || e.RaisedEvent.EventName == EventDiplomaticContractStateChange.Name || e.RaisedEvent.EventName == EventDiplomaticTermProposalSigned.Name)
		{
			global::Empire empire = null;
			EventDiplomaticRelationStateChange eventDiplomaticRelationStateChange = e.RaisedEvent as EventDiplomaticRelationStateChange;
			if (eventDiplomaticRelationStateChange != null)
			{
				if (eventDiplomaticRelationStateChange.EmpireWithWhichTheStatusChange.Index != base.Empire.Index)
				{
					return;
				}
				empire = (eventDiplomaticRelationStateChange.Empire as global::Empire);
			}
			EventDiplomaticContractStateChange eventDiplomaticContractStateChange = e.RaisedEvent as EventDiplomaticContractStateChange;
			if (eventDiplomaticContractStateChange != null)
			{
				if (eventDiplomaticContractStateChange.DiplomaticContract.EmpireWhichReceives.Index == base.Empire.Index)
				{
					empire = eventDiplomaticContractStateChange.DiplomaticContract.EmpireWhichProposes;
				}
				else
				{
					if (eventDiplomaticContractStateChange.DiplomaticContract.EmpireWhichProposes.Index != base.Empire.Index)
					{
						return;
					}
					empire = eventDiplomaticContractStateChange.DiplomaticContract.EmpireWhichReceives;
				}
			}
			EventDiplomaticTermProposalSigned eventDiplomaticTermProposalSigned = e.RaisedEvent as EventDiplomaticTermProposalSigned;
			if (eventDiplomaticTermProposalSigned != null)
			{
				if (eventDiplomaticTermProposalSigned.Empire.Index != base.Empire.Index)
				{
					return;
				}
				empire = (eventDiplomaticTermProposalSigned.Empire as global::Empire);
			}
			Diagnostics.Assert(empire != null);
			DepartmentOfTheInterior agency6 = base.Empire.GetAgency<DepartmentOfTheInterior>();
			Diagnostics.Assert(agency6 != null);
			DepartmentOfForeignAffairs agency7 = base.Empire.GetAgency<DepartmentOfForeignAffairs>();
			Diagnostics.Assert(agency7 != null);
			IGameService service5 = Services.GetService<IGameService>();
			Diagnostics.Assert(service5 != null);
			Diagnostics.Assert(service5.Game != null);
			Diagnostics.Assert(service5.Game is global::Game);
			DiplomaticRelation diplomaticRelation2 = agency7.GetDiplomaticRelation(empire);
			if (diplomaticRelation2 != null)
			{
				if (diplomaticRelation2.HasInactiveAbility(DiplomaticAbilityDefinition.CommercialAgreement))
				{
					((IForeignAffairsManagment)agency7).RemoveDiplomaticRelationAbility(empire, DiplomaticAbilityDefinition.CommercialAgreement);
				}
				if (diplomaticRelation2.HasInactiveAbility(DiplomaticAbilityDefinition.ResearchAgreement))
				{
					((IForeignAffairsManagment)agency7).RemoveDiplomaticRelationAbility(empire, DiplomaticAbilityDefinition.ResearchAgreement);
				}
			}
			IWorldPositionningService service6 = service5.Game.Services.GetService<IWorldPositionningService>();
			foreach (City city4 in agency6.Cities)
			{
				bool flag9 = false;
				if (city4.Hero != null)
				{
					flag9 = city4.Hero.IsSkillUnlocked(DepartmentOfPlanificationAndDevelopment.HeroSkillGovernor25);
				}
				bool flag10 = false;
				Diagnostics.Assert(city4.TradeRoutes != null);
				for (int num2 = 0; num2 < city4.TradeRoutes.Count; num2++)
				{
					TradeRoute tradeRoute4 = city4.TradeRoutes[num2];
					bool flag11 = false;
					for (int num3 = 0; num3 < tradeRoute4.IntermediateRegions.Count; num3++)
					{
						short regionIndex3 = tradeRoute4.IntermediateRegions[num3];
						Region region3 = service6.GetRegion((int)regionIndex3);
						if (region3 != null && region3.City != null)
						{
							if (empire.Index == region3.City.Empire.Index)
							{
								Diagnostics.Assert(region3.City.Empire != null);
								DiplomaticRelation diplomaticRelation3 = agency7.GetDiplomaticRelation(region3.City.Empire);
								if (!flag9 && !diplomaticRelation3.HasActiveAbility(DiplomaticAbilityDefinition.TradeRoute))
								{
									flag11 = true;
									break;
								}
							}
						}
					}
					if ((tradeRoute4.PathfindingMovementCapacity & PathfindingMovementCapacity.Water) == PathfindingMovementCapacity.Water && tradeRoute4.IntermediateRegionsOfTypeOceanic != null && tradeRoute4.IntermediateRegionsOfTypeOceanic.Count != 0)
					{
						bool flag12 = this.UpdateRelationForTradeRouteWithIntermediateRegionsOfTypeOceanic(city4, tradeRoute4, ref flag11);
						flag10 = (flag10 || flag12);
					}
					if (flag11)
					{
						SimulationDescriptor descriptor4;
						if (tradeRoute4.SimulationObject != null && this.simulationDescriptorsDatatable.TryGetValue(TradeRoute.TradeRouteStatusRelationBlocked, out descriptor4) && !tradeRoute4.SimulationObject.Tags.Contains(TradeRoute.TradeRouteStatusRelationBlocked))
						{
							tradeRoute4.SimulationObject.AddDescriptor(descriptor4);
							tradeRoute4.SimulationObject.Refresh();
							flag10 = true;
							this.OnTradeRouteChanged(tradeRoute4);
						}
						this.UpdateIntermediateRegions(tradeRoute4, false);
					}
				}
				if (flag10)
				{
					city4.Refresh(false);
				}
			}
		}
		if (e.RaisedEvent.EventName == EventHeroAssignment.Name)
		{
			EventHeroAssignment eventHeroAssignment = e.RaisedEvent as EventHeroAssignment;
			if (eventHeroAssignment.Empire.Index != base.Empire.Index)
			{
				return;
			}
			if (eventHeroAssignment.LastAssignment == null)
			{
				return;
			}
			City city5 = eventHeroAssignment.LastAssignment as City;
			if (city5 == null)
			{
				return;
			}
			DepartmentOfTheInterior agency8 = base.Empire.GetAgency<DepartmentOfTheInterior>();
			Diagnostics.Assert(agency8 != null);
			DepartmentOfForeignAffairs agency9 = base.Empire.GetAgency<DepartmentOfForeignAffairs>();
			Diagnostics.Assert(agency9 != null);
			IGameService service7 = Services.GetService<IGameService>();
			Diagnostics.Assert(service7 != null);
			Diagnostics.Assert(service7.Game != null);
			Diagnostics.Assert(service7.Game is global::Game);
			IWorldPositionningService service8 = service7.Game.Services.GetService<IWorldPositionningService>();
			foreach (City city6 in agency8.Cities)
			{
				if (!(city6.GUID != city5.GUID))
				{
					bool flag13 = false;
					if (city6.Hero != null)
					{
						flag13 = city6.Hero.IsSkillUnlocked(DepartmentOfPlanificationAndDevelopment.HeroSkillGovernor25);
						if (flag13)
						{
							continue;
						}
					}
					bool flag14 = false;
					Diagnostics.Assert(city6.TradeRoutes != null);
					for (int num4 = 0; num4 < city6.TradeRoutes.Count; num4++)
					{
						TradeRoute tradeRoute5 = city6.TradeRoutes[num4];
						bool flag15 = false;
						for (int num5 = 0; num5 < tradeRoute5.IntermediateRegions.Count; num5++)
						{
							short regionIndex4 = tradeRoute5.IntermediateRegions[num5];
							Region region4 = service8.GetRegion((int)regionIndex4);
							if (region4 != null && region4.City != null)
							{
								if (eventHeroAssignment.Empire.Index != region4.City.Empire.Index)
								{
									Diagnostics.Assert(region4.City.Empire != null);
									DiplomaticRelation diplomaticRelation4 = agency9.GetDiplomaticRelation(region4.City.Empire);
									if (!flag13 && !diplomaticRelation4.HasActiveAbility(DiplomaticAbilityDefinition.TradeRoute))
									{
										flag15 = true;
										break;
									}
								}
							}
						}
						if ((tradeRoute5.PathfindingMovementCapacity & PathfindingMovementCapacity.Water) == PathfindingMovementCapacity.Water && tradeRoute5.IntermediateRegionsOfTypeOceanic != null && tradeRoute5.IntermediateRegionsOfTypeOceanic.Count != 0)
						{
							bool flag16 = this.UpdateRelationForTradeRouteWithIntermediateRegionsOfTypeOceanic(city6, tradeRoute5, ref flag15);
							flag14 = (flag14 || flag16);
						}
						if (flag15)
						{
							SimulationDescriptor descriptor5;
							if (tradeRoute5.SimulationObject != null && this.simulationDescriptorsDatatable.TryGetValue(TradeRoute.TradeRouteStatusRelationBlocked, out descriptor5) && !tradeRoute5.SimulationObject.Tags.Contains(TradeRoute.TradeRouteStatusRelationBlocked))
							{
								tradeRoute5.SimulationObject.AddDescriptor(descriptor5);
								tradeRoute5.SimulationObject.Refresh();
								flag14 = true;
								this.OnTradeRouteChanged(tradeRoute5);
							}
							this.UpdateIntermediateRegions(tradeRoute5, false);
						}
					}
					if (flag14)
					{
						city6.Refresh(false);
					}
				}
			}
		}
		if (e.RaisedEvent.EventName == EventBoosterActivated.Name && this.BoosterActivated != null)
		{
			this.BoosterActivated(this, new EventArgs());
		}
		if (e.RaisedEvent.EventName == EventFortressOccupantSwapped.Name)
		{
			DepartmentOfTheInterior agency10 = base.Empire.GetAgency<DepartmentOfTheInterior>();
			Diagnostics.Assert(agency10 != null);
			DepartmentOfForeignAffairs agency11 = base.Empire.GetAgency<DepartmentOfForeignAffairs>();
			Diagnostics.Assert(agency11 != null);
			IGameService service9 = Services.GetService<IGameService>();
			Diagnostics.Assert(service9 != null);
			Diagnostics.Assert(service9.Game != null);
			Diagnostics.Assert(service9.Game is global::Game);
			foreach (City city7 in agency10.Cities)
			{
				if (city7.Hero != null)
				{
					bool flag17 = city7.Hero.IsSkillUnlocked(DepartmentOfPlanificationAndDevelopment.HeroSkillGovernor25);
					if (flag17)
					{
						continue;
					}
				}
				bool flag18 = false;
				Diagnostics.Assert(city7.TradeRoutes != null);
				for (int num6 = 0; num6 < city7.TradeRoutes.Count; num6++)
				{
					TradeRoute tradeRoute6 = city7.TradeRoutes[num6];
					bool flag19 = false;
					if ((tradeRoute6.PathfindingMovementCapacity & PathfindingMovementCapacity.Water) == PathfindingMovementCapacity.Water && tradeRoute6.IntermediateRegionsOfTypeOceanic != null && tradeRoute6.IntermediateRegionsOfTypeOceanic.Count != 0)
					{
						bool flag20 = this.UpdateRelationForTradeRouteWithIntermediateRegionsOfTypeOceanic(city7, tradeRoute6, ref flag19);
						flag18 = (flag18 || flag20);
					}
					if (flag19)
					{
						SimulationDescriptor descriptor6;
						if (tradeRoute6.SimulationObject != null && this.simulationDescriptorsDatatable.TryGetValue(TradeRoute.TradeRouteStatusRelationBlocked, out descriptor6) && !tradeRoute6.SimulationObject.Tags.Contains(TradeRoute.TradeRouteStatusRelationBlocked))
						{
							tradeRoute6.SimulationObject.AddDescriptor(descriptor6);
							tradeRoute6.SimulationObject.Refresh();
							flag18 = true;
							this.OnTradeRouteChanged(tradeRoute6);
						}
						this.UpdateIntermediateRegions(tradeRoute6, false);
					}
				}
				if (flag18)
				{
					city7.Refresh(false);
				}
			}
		}
	}

	private void SeasonService_SeasonChange(object sender, SeasonChangeEventArgs e)
	{
		if (e.OldSeason != null)
		{
			if (e.OldSeason.SeasonDefinition.SeasonType == Season.ReadOnlyWinter)
			{
				this.LocalWinterImmunityWinnerIndex = -1;
				this.LocalWinterImmunityWinnerBid = 0;
				base.Empire.SetPropertyBaseValue("NumberOfPastWinters", base.Empire.GetPropertyValue("NumberOfPastWinters") + 1f);
			}
			base.Empire.SetPropertyBaseValue("NumberOfPastSeasons", base.Empire.GetPropertyValue("NumberOfPastSeasons") + 1f);
			if (e.NewSeason != null && e.NewSeason.SeasonDefinition.SeasonType == Season.ReadOnlyWinter)
			{
				base.Empire.SetPropertyBaseValue("NumberOfWinters", base.Empire.GetPropertyValue("NumberOfWinters") + 1f);
			}
		}
	}

	private IEnumerator GameClientState_Turn_End_ClearAllTradeRoutes(string context, string name)
	{
		DepartmentOfTheInterior departmentOfTheInterior = base.Empire.GetAgency<DepartmentOfTheInterior>();
		Diagnostics.Assert(departmentOfTheInterior != null);
		foreach (City city in departmentOfTheInterior.Cities)
		{
			float overrallTradeRoutesCityDustIncome = city.GetPropertyValue(SimulationProperties.OverrallTradeRoutesCityDustIncome);
			city.SetPropertyBaseValue(SimulationProperties.LastOverrallTradeRoutesCityDustIncome, overrallTradeRoutesCityDustIncome);
			float overrallTradeRoutesCityScienceIncome = city.GetPropertyValue(SimulationProperties.OverrallTradeRoutesCityScienceIncome);
			city.SetPropertyBaseValue(SimulationProperties.LastOverrallTradeRoutesCityScienceIncome, overrallTradeRoutesCityScienceIncome);
			Diagnostics.Assert(city.TradeRoutes != null);
			city.TradeRoutes.Clear();
			city.SetPropertyBaseValue(SimulationProperties.IntermediateTradeRoutesCount, 0f);
			city.SetPropertyBaseValue(SimulationProperties.IntermediateTradeRoutesDistance, 0f);
			city.SetPropertyBaseValue(SimulationProperties.IntermediateTradeRoutesGain, 0f);
		}
		this.OnTradeRouteChanged(null);
		yield break;
	}

	private IEnumerator GameClientState_Turn_Begin_ApplyTradeRoutesSimulation(string context, string name)
	{
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		Diagnostics.Assert(gameService.Game != null);
		Diagnostics.Assert(gameService.Game is global::Game);
		IWorldPositionningService worldPositionningService = gameService.Game.Services.GetService<IWorldPositionningService>();
		float gameTurn = (float)((global::Game)gameService.Game).Turn;
		float tradeRoutesBaseMultiplier = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<float>(DepartmentOfPlanificationAndDevelopment.Registers.TradeRoutesBaseMultiplier, 0.5f);
		float tradeRoutesDistanceMultiplier = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<float>(DepartmentOfPlanificationAndDevelopment.Registers.TradeRoutesDistanceMultiplier, 0.25f);
		DepartmentOfTheInterior departmentOfTheInterior = base.Empire.GetAgency<DepartmentOfTheInterior>();
		Diagnostics.Assert(departmentOfTheInterior != null);
		SimulationPath simulationPath = new SimulationPath("ClassCity/ClassTradeRoute");
		StaticString tradeRouteStatus = new StaticString("TradeRouteStatus");
		StaticString tradeRouteMovementCapacity = new StaticString("TradeRouteMovementCapacity");
		foreach (City city in departmentOfTheInterior.Cities)
		{
			Diagnostics.Assert(city.TradeRoutes != null);
			bool heroSkillGovernor25 = false;
			if (city.Hero != null)
			{
				heroSkillGovernor25 = city.Hero.IsSkillUnlocked(DepartmentOfPlanificationAndDevelopment.HeroSkillGovernor25);
			}
			List<SimulationObject> objects = simulationPath.GetValidatedObjects(city, PathNavigatorSemantic.WriteModifier).ToList<SimulationObject>();
			while (objects.Count < city.TradeRoutes.Count)
			{
				string otherName = "TradeRoute#" + objects.Count;
				SimulationObject other = new SimulationObject(otherName);
				SimulationDescriptor simulationDescriptor;
				if (this.simulationDescriptorsDatatable.TryGetValue("ClassTradeRoute", out simulationDescriptor))
				{
					other.AddDescriptor(simulationDescriptor);
				}
				objects.Add(other);
				int childIndex = city.SimulationObject.Children.BinarySearch((SimulationObject match) => match.Name.CompareTo(otherName));
				if (childIndex >= 0)
				{
					SimulationObject child = city.SimulationObject.Children[childIndex];
					city.SimulationObject.RemoveChild(child);
				}
				city.SimulationObject.AddChild(other);
			}
			int index = 0;
			while (index < objects.Count)
			{
				objects[index].RemoveDescriptorsByType(tradeRouteStatus);
				objects[index].RemoveDescriptorsByType(tradeRouteMovementCapacity);
				objects[index].RemoveDescriptorByName(TradeRoute.TradeRoutePassingThroughOceanicRegionOfMine);
				objects[index].RemoveDescriptorByName(TradeRoute.TradeRoutePassingThroughOceanicRegionOfSomeAllyOfMine);
				objects[index].ModifierForward = ModifierForwardType.ChildrenOnly;
				if (index >= city.TradeRoutes.Count)
				{
					goto IL_C50;
				}
				TradeRoute tradeRoute = city.TradeRoutes[index];
				objects[index].ModifierForward = ModifierForwardType.Both;
				if (worldPositionningService == null)
				{
					goto IL_C50;
				}
				Region toRegion = worldPositionningService.GetRegion((int)tradeRoute.ToRegionIndex);
				if (toRegion == null)
				{
					goto IL_C50;
				}
				tradeRoute.SimulationObject = objects[index];
				tradeRoute.SimulationObject.SetPropertyBaseValue(SimulationProperties.TradeRouteGain, 0f);
				bool tradeRouteIsAffectedBySiege = false;
				bool tradeRouteIsAffectedByDiplomaticRelation = false;
				SimulationDescriptor simulationDescriptor2;
				if ((tradeRoute.PathfindingMovementCapacity & PathfindingMovementCapacity.Ground) == PathfindingMovementCapacity.Ground && this.simulationDescriptorsDatatable.TryGetValue(TradeRoute.TradeRouteMovementCapacityGround, out simulationDescriptor2))
				{
					tradeRoute.SimulationObject.AddDescriptor(simulationDescriptor2);
				}
				SimulationDescriptor simulationDescriptor3;
				if ((tradeRoute.PathfindingMovementCapacity & PathfindingMovementCapacity.Water) == PathfindingMovementCapacity.Water && this.simulationDescriptorsDatatable.TryGetValue(TradeRoute.TradeRouteMovementCapacityWater, out simulationDescriptor3))
				{
					tradeRoute.SimulationObject.AddDescriptor(simulationDescriptor3);
				}
				SimulationDescriptor simulationDescriptor4;
				if ((tradeRoute.TradeRouteFlags & TradeRouteFlags.PassingThroughOceanicRegionOfMine) == TradeRouteFlags.PassingThroughOceanicRegionOfMine && this.simulationDescriptorsDatatable.TryGetValue(TradeRoute.TradeRoutePassingThroughOceanicRegionOfMine, out simulationDescriptor4))
				{
					tradeRoute.SimulationObject.AddDescriptor(simulationDescriptor4);
				}
				SimulationDescriptor simulationDescriptor5;
				if ((tradeRoute.TradeRouteFlags & TradeRouteFlags.PassingThroughOceanicRegionOfSomeAllyOfMine) == TradeRouteFlags.PassingThroughOceanicRegionOfSomeAllyOfMine && this.simulationDescriptorsDatatable.TryGetValue(TradeRoute.TradeRoutePassingThroughOceanicRegionOfSomeAllyOfMine, out simulationDescriptor5))
				{
					tradeRoute.SimulationObject.AddDescriptor(simulationDescriptor5);
				}
				bool tradeRouteIsBroken = false;
				if (toRegion.City == null)
				{
					tradeRouteIsBroken = true;
				}
				else
				{
					float population = toRegion.City.GetPropertyValue(SimulationProperties.Population);
					float distance = (float)tradeRoute.Distance;
					float tradeRouteGain = 1f + (population + distance * tradeRoutesDistanceMultiplier) * tradeRoutesBaseMultiplier;
					tradeRoute.SimulationObject.SetPropertyBaseValue(SimulationProperties.TradeRouteGain, tradeRouteGain);
					for (int intermediateRegionsIndex = 0; intermediateRegionsIndex < tradeRoute.IntermediateRegions.Count; intermediateRegionsIndex++)
					{
						Region intermediateRegion = worldPositionningService.GetRegion((int)tradeRoute.IntermediateRegions[intermediateRegionsIndex]);
						if (intermediateRegion == null || intermediateRegion.City == null)
						{
							tradeRouteIsBroken = true;
							break;
						}
					}
					if (!tradeRouteIsBroken)
					{
						if (city.BesiegingEmpireIndex != -1)
						{
							tradeRouteIsAffectedBySiege = this.CheckWhetherTradeRouteIsRealyAffectedBySiege(city);
						}
						else
						{
							for (int intermediateRegionsIndex2 = 0; intermediateRegionsIndex2 < tradeRoute.IntermediateRegions.Count; intermediateRegionsIndex2++)
							{
								Region intermediateRegion2 = worldPositionningService.GetRegion((int)tradeRoute.IntermediateRegions[intermediateRegionsIndex2]);
								if (intermediateRegion2 != null && intermediateRegion2.City != null && intermediateRegion2.City.BesiegingEmpireIndex != -1)
								{
									tradeRouteIsAffectedBySiege = this.CheckWhetherTradeRouteIsRealyAffectedBySiege(intermediateRegion2.City);
									if (tradeRouteIsAffectedBySiege)
									{
										break;
									}
								}
							}
						}
						SimulationDescriptor simulationDescriptor6;
						if (tradeRouteIsAffectedBySiege && !tradeRoute.SimulationObject.Tags.Contains(TradeRoute.TradeRouteStatusSiegeBlocked) && this.simulationDescriptorsDatatable.TryGetValue(TradeRoute.TradeRouteStatusSiegeBlocked, out simulationDescriptor6))
						{
							tradeRoute.SimulationObject.AddDescriptor(simulationDescriptor6);
						}
						DepartmentOfForeignAffairs departmentOfForeignAffairs = base.Empire.GetAgency<DepartmentOfForeignAffairs>();
						Diagnostics.Assert(departmentOfForeignAffairs != null);
						for (int intermediateRegionsIndex3 = 0; intermediateRegionsIndex3 < tradeRoute.IntermediateRegions.Count; intermediateRegionsIndex3++)
						{
							Region intermediateRegion3 = worldPositionningService.GetRegion((int)tradeRoute.IntermediateRegions[intermediateRegionsIndex3]);
							if (intermediateRegion3 != null && intermediateRegion3.City != null && intermediateRegion3.City.Empire.Index != base.Empire.Index)
							{
								DiplomaticRelation diplomaticRelation = departmentOfForeignAffairs.GetDiplomaticRelation(intermediateRegion3.City.Empire);
								if (!heroSkillGovernor25 && !diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.TradeRoute))
								{
									tradeRouteIsAffectedByDiplomaticRelation = true;
									break;
								}
							}
						}
						SimulationDescriptor simulationDescriptor7;
						if (tradeRouteIsAffectedByDiplomaticRelation && !tradeRoute.SimulationObject.Tags.Contains(TradeRoute.TradeRouteStatusRelationBlocked) && this.simulationDescriptorsDatatable.TryGetValue(TradeRoute.TradeRouteStatusRelationBlocked, out simulationDescriptor7))
						{
							tradeRoute.SimulationObject.AddDescriptor(simulationDescriptor7);
						}
						if (tradeRouteIsAffectedBySiege || tradeRouteIsAffectedByDiplomaticRelation)
						{
							tradeRoute.SimulationObject.Refresh();
						}
					}
				}
				if (tradeRouteIsBroken)
				{
					if (!tradeRoute.SimulationObject.Tags.Contains(TradeRoute.TradeRouteStatusBroken))
					{
						SimulationDescriptor simulationDescriptor8;
						if (this.simulationDescriptorsDatatable.TryGetValue(TradeRoute.TradeRouteStatusBroken, out simulationDescriptor8))
						{
							tradeRoute.SimulationObject.AddDescriptor(simulationDescriptor8);
						}
						tradeRoute.SimulationObject.Refresh();
					}
				}
				else if (!tradeRouteIsAffectedBySiege && !tradeRouteIsAffectedByDiplomaticRelation)
				{
					DepartmentOfForeignAffairs departmentOfForeignAffairs2 = base.Empire.GetAgency<DepartmentOfForeignAffairs>();
					Diagnostics.Assert(departmentOfForeignAffairs2 != null);
					DiplomaticRelation diplomaticRelation2 = departmentOfForeignAffairs2.GetDiplomaticRelation(toRegion.City.Empire);
					Diagnostics.Assert(diplomaticRelation2 != null);
					bool commercialAgreement = diplomaticRelation2.HasActiveAbility(DiplomaticAbilityDefinition.CommercialAgreement);
					if (commercialAgreement)
					{
						SimulationDescriptor simulationDescriptor9;
						if (!tradeRoute.SimulationObject.Tags.Contains(TradeRoute.TradeRouteCommercialAgreement) && this.simulationDescriptorsDatatable.TryGetValue(TradeRoute.TradeRouteCommercialAgreement, out simulationDescriptor9))
						{
							tradeRoute.SimulationObject.AddDescriptor(simulationDescriptor9);
						}
						int commercialAggreementBeginingTurn = diplomaticRelation2.GetAbilityActivationTurn(DiplomaticAbilityDefinition.CommercialAgreement);
						tradeRoute.SimulationObject.SetPropertyBaseValue(SimulationProperties.NumberOfTurnSinceCommercialAggreementBegining, gameTurn - (float)commercialAggreementBeginingTurn);
						tradeRoute.SimulationObject.Refresh();
					}
					else if (tradeRoute.SimulationObject.Tags.Contains(TradeRoute.TradeRouteCommercialAgreement))
					{
						tradeRoute.SimulationObject.RemoveDescriptorByName(TradeRoute.TradeRouteCommercialAgreement);
					}
					bool researchAgreement = diplomaticRelation2.HasActiveAbility(DiplomaticAbilityDefinition.ResearchAgreement);
					if (researchAgreement)
					{
						SimulationDescriptor simulationDescriptor10;
						if (!tradeRoute.SimulationObject.Tags.Contains(TradeRoute.TradeRouteResearchAgreement) && this.simulationDescriptorsDatatable.TryGetValue(TradeRoute.TradeRouteResearchAgreement, out simulationDescriptor10))
						{
							tradeRoute.SimulationObject.AddDescriptor(simulationDescriptor10);
						}
						int researchAggreementBeginingTurn = diplomaticRelation2.GetAbilityActivationTurn(DiplomaticAbilityDefinition.ResearchAgreement);
						tradeRoute.SimulationObject.SetPropertyBaseValue(SimulationProperties.NumberOfTurnSinceResearchAgreementBegining, gameTurn - (float)researchAggreementBeginingTurn);
						tradeRoute.SimulationObject.Refresh();
					}
					else if (tradeRoute.SimulationObject.Tags.Contains(TradeRoute.TradeRouteResearchAgreement))
					{
						tradeRoute.SimulationObject.RemoveDescriptorByName(TradeRoute.TradeRouteResearchAgreement);
					}
					this.UpdateIntermediateRegions(tradeRoute, true);
				}
				IL_C70:
				index++;
				continue;
				IL_C50:
				objects[index].SetPropertyBaseValue(SimulationProperties.TradeRouteGain, 0f);
				goto IL_C70;
			}
			city.Refresh(false);
		}
		base.Empire.Refresh(false);
		this.eventService.Notify(new EventTradeRoutesUpdated(base.Empire));
		yield break;
	}

	private IEnumerator GameClientState_Turn_Begin_CreateNewTradeRoutes(string context, string name)
	{
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		Diagnostics.Assert(gameService.Game != null);
		Diagnostics.Assert(gameService.Game is global::Game);
		if (this.TurnWhenTradeRoutesWereLastCreated >= ((global::Game)gameService.Game).Turn)
		{
			yield break;
		}
		this.TurnWhenTradeRoutesWereLastCreated = ((global::Game)gameService.Game).Turn;
		ICadasterService cadasterService = gameService.Game.Services.GetService<ICadasterService>();
		if (cadasterService == null)
		{
			yield break;
		}
		IWorldPositionningService worldPositionningService = gameService.Game.Services.GetService<IWorldPositionningService>();
		if (worldPositionningService == null)
		{
			yield break;
		}
		DepartmentOfTheInterior departmentOfTheInterior = base.Empire.GetAgency<DepartmentOfTheInterior>();
		Diagnostics.Assert(departmentOfTheInterior != null);
		DepartmentOfForeignAffairs departmentOfForeignAffairs = base.Empire.GetAgency<DepartmentOfForeignAffairs>();
		Diagnostics.Assert(departmentOfForeignAffairs != null);
		foreach (City city in departmentOfTheInterior.Cities)
		{
			Diagnostics.Assert(city.TradeRoutes != null);
			Diagnostics.Assert(city.TradeRoutes.Count == 0, "Trade routes must have been cleared at the end of the previous turn, or musn't they?");
			Diagnostics.Assert(city.CadastralMap != null);
			city.TradeRoutes.Clear();
			if (city.CadastralMap.ConnectedMovementCapacity != PathfindingMovementCapacity.None)
			{
				if (city.BesiegingEmpireIndex != -1)
				{
					bool tradeRouteIsAffectedBySiege = this.CheckWhetherTradeRouteIsRealyAffectedBySiege(city);
					if (tradeRouteIsAffectedBySiege)
					{
						continue;
					}
				}
				int maximumNumberOfTradeRoutes = Mathf.FloorToInt(city.GetPropertyValue(SimulationProperties.MaximumNumberOfTradeRoutes));
				if (maximumNumberOfTradeRoutes > 0)
				{
					bool heroSkillGovernor25 = false;
					if (city.Hero != null)
					{
						heroSkillGovernor25 = city.Hero.IsSkillUnlocked(DepartmentOfPlanificationAndDevelopment.HeroSkillGovernor25);
					}
					List<Region> openListOfRegions = new List<Region>();
					List<Region> closedListOfRegions = new List<Region>();
					List<TradeRoute> existingTradeRoutes = new List<TradeRoute>();
					closedListOfRegions.Add(city.Region);
					for (int closedListOfRegionsIndex = 0; closedListOfRegionsIndex < closedListOfRegions.Count; closedListOfRegionsIndex++)
					{
						Region fromRegion = closedListOfRegions[closedListOfRegionsIndex];
						Diagnostics.Assert(fromRegion != null);
						Diagnostics.Assert(fromRegion.City != null);
						Diagnostics.Assert(fromRegion.City.CadastralMap != null);
						if (fromRegion.City.CadastralMap.Roads != null)
						{
							TradeRoute existingTradeRouteFromRegion = null;
							for (int index = 0; index < existingTradeRoutes.Count; index++)
							{
								if ((int)existingTradeRoutes[index].ToRegionIndex == fromRegion.Index)
								{
									existingTradeRouteFromRegion = existingTradeRoutes[index];
									break;
								}
							}
							for (int roadIndex = 0; roadIndex < fromRegion.City.CadastralMap.Roads.Count; roadIndex++)
							{
								Road road = cadasterService[fromRegion.City.CadastralMap.Roads[roadIndex]];
								short toRegionIndex = road.ToRegion;
								if (fromRegion.Index == (int)road.ToRegion)
								{
									toRegionIndex = road.FromRegion;
								}
								Region toRegion = worldPositionningService.GetRegion((int)toRegionIndex);
								if (toRegion != null)
								{
									if (!closedListOfRegions.Contains(toRegion))
									{
										bool regionCanBeTraversed = true;
										if (toRegion.City == null || toRegion.City.CadastralMap == null)
										{
											regionCanBeTraversed = false;
										}
										else if (toRegion.City.CadastralMap.ConnectedMovementCapacity == PathfindingMovementCapacity.None)
										{
											regionCanBeTraversed = false;
										}
										else if (toRegion.City.BesiegingEmpireIndex != -1)
										{
											bool tradeRouteIsAffectedBySiege2 = this.CheckWhetherTradeRouteIsRealyAffectedBySiege(toRegion.City);
											if (tradeRouteIsAffectedBySiege2)
											{
												regionCanBeTraversed = false;
											}
										}
										else if (toRegion.City.Empire.Index != base.Empire.Index)
										{
											DiplomaticRelation diplomaticRelation = departmentOfForeignAffairs.GetDiplomaticRelation(toRegion.City.Empire);
											regionCanBeTraversed = (heroSkillGovernor25 || diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.TradeRoute));
											regionCanBeTraversed &= ((toRegion.City.CadastralMap.ConnectedMovementCapacity & fromRegion.City.CadastralMap.ConnectedMovementCapacity) != PathfindingMovementCapacity.None);
										}
										List<short> intermediateRegions = new List<short>();
										List<short> intermediateRegionsOfTypeOceanic = new List<short>();
										TradeRouteFlags tradeRouteFlags = TradeRouteFlags.None;
										if (regionCanBeTraversed && (road.PathfindingMovementCapacity & PathfindingMovementCapacity.Water) == PathfindingMovementCapacity.Water)
										{
											for (int positionIndex = 0; positionIndex < road.WorldPositions.Length; positionIndex++)
											{
												short regionIndex = worldPositionningService.GetRegionIndex(road.WorldPositions[positionIndex]);
												if (!intermediateRegions.Contains(regionIndex))
												{
													intermediateRegions.Add(regionIndex);
													Region region = worldPositionningService.GetRegion((int)regionIndex);
													if (region != null)
													{
														if (region.IsOcean)
														{
															intermediateRegionsOfTypeOceanic.Add(regionIndex);
															global::Empire regionFortressesOwner = region.Owner;
															if (regionFortressesOwner is MajorEmpire)
															{
																if (regionFortressesOwner == base.Empire)
																{
																	tradeRouteFlags |= TradeRouteFlags.PassingThroughOceanicRegionOfMine;
																}
																else
																{
																	DiplomaticRelation diplomaticRelation2 = departmentOfForeignAffairs.GetDiplomaticRelation(regionFortressesOwner);
																	bool hasTradeRouteDiplomaticAbility = diplomaticRelation2.HasActiveAbility(DiplomaticAbilityDefinition.TradeRoute);
																	regionCanBeTraversed &= (heroSkillGovernor25 || hasTradeRouteDiplomaticAbility);
																	if (hasTradeRouteDiplomaticAbility)
																	{
																		tradeRouteFlags |= TradeRouteFlags.PassingThroughOceanicRegionOfSomeAllyOfMine;
																	}
																}
															}
														}
													}
												}
											}
										}
										if (!regionCanBeTraversed)
										{
											closedListOfRegions.Insert(closedListOfRegionsIndex, toRegion);
											closedListOfRegionsIndex++;
										}
										else
										{
											if (!openListOfRegions.Contains(toRegion))
											{
												openListOfRegions.Add(toRegion);
											}
											TradeRoute tradeRoute4 = new TradeRoute
											{
												Distance = 0,
												IntermediateRegions = new List<short>(),
												PathfindingMovementCapacity = PathfindingMovementCapacity.None,
												IntermediateRegionsOfTypeOceanic = new List<short>(),
												TradeRouteFlags = TradeRouteFlags.None
											};
											if (existingTradeRouteFromRegion != null)
											{
												TradeRoute tradeRoute2 = tradeRoute4;
												tradeRoute2.Distance += existingTradeRouteFromRegion.Distance;
												tradeRoute4.IntermediateRegions.AddRange(existingTradeRouteFromRegion.IntermediateRegions);
												tradeRoute4.IntermediateRegionsOfTypeOceanic.AddRange(existingTradeRouteFromRegion.IntermediateRegionsOfTypeOceanic);
												tradeRoute4.TradeRouteFlags |= existingTradeRouteFromRegion.TradeRouteFlags;
											}
											TradeRoute tradeRoute3 = tradeRoute4;
											tradeRoute3.Distance += (short)road.WorldPositions.Length;
											tradeRoute4.IntermediateRegions.Add(toRegionIndex);
											tradeRoute4.IntermediateRegionsOfTypeOceanic.AddRange(intermediateRegionsOfTypeOceanic);
											tradeRoute4.TradeRouteFlags |= tradeRouteFlags;
											tradeRoute4.PathfindingMovementCapacity |= road.PathfindingMovementCapacity;
											int existingTradeRouteToRegionIndex = -1;
											for (int index2 = 0; index2 < existingTradeRoutes.Count; index2++)
											{
												if (existingTradeRoutes[index2].ToRegionIndex == toRegionIndex)
												{
													if (existingTradeRoutes[index2].Distance < tradeRoute4.Distance)
													{
														existingTradeRoutes[index2] = tradeRoute4;
													}
													existingTradeRouteToRegionIndex = index2;
													break;
												}
											}
											if (existingTradeRouteToRegionIndex == -1)
											{
												existingTradeRoutes.Add(tradeRoute4);
											}
										}
									}
								}
							}
							closedListOfRegions.AddRange(openListOfRegions);
							openListOfRegions.Clear();
						}
					}
					int empireBit = 1 << base.Empire.Index;
					existingTradeRoutes.RemoveAll(delegate(TradeRoute tradeRoute)
					{
						Region region2 = worldPositionningService.GetRegion((int)tradeRoute.ToRegionIndex);
						return (worldPositionningService.GetExplorationBits(region2.City.WorldPosition) & empireBit) == 0;
					});
					existingTradeRoutes.Sort(new DepartmentOfPlanificationAndDevelopment.TradeRouteComparer());
					city.TradeRoutes = existingTradeRoutes.Take(maximumNumberOfTradeRoutes).ToList<TradeRoute>();
					this.OnTradeRouteChanged(null);
				}
			}
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_Begin_NotifyEmpirePlanChoiceTurn(string context, string name)
	{
		this.IsEmpirePlanChoiced = false;
		if (this.IsEmpirePlanChoiceTurn)
		{
			this.eventService.Notify(new EventEmpirePlanNeeded(base.Empire));
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_ResetEmpirePlan(string context, string name)
	{
		if (!this.IsEmpirePlanChoiceTurn)
		{
			yield break;
		}
		StaticString[] empirePlanClasses = this.GetEmpirePlanClasses();
		if (empirePlanClasses != null)
		{
			foreach (StaticString empirePlanClass in empirePlanClasses)
			{
				Diagnostics.Assert(empirePlanClass != null);
				EmpirePlanDefinition currentPlan = this.GetEmpirePlanDefinition(empirePlanClass, 0);
				Diagnostics.Assert(currentPlan != null, "There is no level 0 for empire plan {0}.", new object[]
				{
					empirePlanClass
				});
				this.UnlockEmpirePlan(currentPlan);
			}
			yield break;
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_ApplyEmpirePlan(string context, string name)
	{
		if (!this.IsEmpirePlanChoiceTurn)
		{
			yield break;
		}
		Diagnostics.Assert(this.empirePlanQueue != null);
		while (this.empirePlanQueue.Length > 0)
		{
			Construction construction = this.empirePlanQueue.Peek();
			Diagnostics.Assert(construction != null);
			DepartmentOfPlanificationAndDevelopment.ConstructibleElement constructibleElement = construction.ConstructibleElement as DepartmentOfPlanificationAndDevelopment.ConstructibleElement;
			Diagnostics.Assert(constructibleElement != null);
			if (!DepartmentOfTheTreasury.CheckConstructiblePrerequisites(base.Empire, constructibleElement, new string[0]))
			{
				break;
			}
			bool constructionFinished = true;
			if (construction.ConstructibleElement.Costs != null && construction.ConstructibleElement.Costs.Length > 0)
			{
				for (int costIndex = 0; costIndex < construction.ConstructibleElement.Costs.Length; costIndex++)
				{
					Diagnostics.Assert(construction.ConstructibleElement.Costs[costIndex] != null);
					if (!construction.ConstructibleElement.Costs[costIndex].Instant && !construction.ConstructibleElement.Costs[costIndex].InstantOnCompletion)
					{
						Diagnostics.Assert(construction.CurrentConstructionStock != null);
						ConstructionResourceStock constructionResourceStock = construction.CurrentConstructionStock[costIndex];
						Diagnostics.Assert(constructionResourceStock != null);
						float constructibleCost = DepartmentOfTheTreasury.GetProductionCostWithBonus(base.Empire.SimulationObject, construction.ConstructibleElement, construction.ConstructibleElement.Costs[costIndex], false);
						constructionFinished &= (constructibleCost <= constructionResourceStock.Stock);
					}
				}
			}
			this.empirePlanQueue.Dequeue();
			Diagnostics.Assert(this.gameEntityRepositoryService != null);
			this.gameEntityRepositoryService.Unregister(construction);
			for (int costIndex2 = 0; costIndex2 < construction.ConstructibleElement.Costs.Length; costIndex2++)
			{
				if (construction.ConstructibleElement.Costs[costIndex2].InstantOnCompletion)
				{
					IConstructionCost currentCost = construction.ConstructibleElement.Costs[costIndex2];
					float resourceCost = -DepartmentOfTheTreasury.GetProductionCostWithBonus(base.Empire.SimulationObject, construction.ConstructibleElement, currentCost, false);
					if (!this.departmentOfTheTreasury.IsTransferOfResourcePossible(base.Empire, currentCost.ResourceName, ref resourceCost))
					{
						constructionFinished = false;
						break;
					}
				}
			}
			if (constructionFinished)
			{
				for (int costIndex3 = 0; costIndex3 < construction.ConstructibleElement.Costs.Length; costIndex3++)
				{
					if (construction.ConstructibleElement.Costs[costIndex3].InstantOnCompletion)
					{
						IConstructionCost currentCost2 = construction.ConstructibleElement.Costs[costIndex3];
						float resourceCost2 = -DepartmentOfTheTreasury.GetProductionCostWithBonus(base.Empire.SimulationObject, construction.ConstructibleElement, currentCost2, false);
						this.departmentOfTheTreasury.TryTransferResources(base.Empire, currentCost2.ResourceName, resourceCost2);
					}
				}
				this.OnEmpirePlanQueueChanged(construction, ConstructionChangeEventAction.Completed);
				Diagnostics.Assert(base.Empire != null);
				base.Empire.Refresh(false);
			}
			else
			{
				this.OnEmpirePlanQueueChanged(construction, ConstructionChangeEventAction.Cancelled);
				EmpirePlanDefinition empirePlan = construction.ConstructibleElement as EmpirePlanDefinition;
				Diagnostics.Assert(empirePlan != null);
				EmpirePlanDefinition level0Definition = this.GetEmpirePlanDefinition(empirePlan.EmpirePlanClass, 0);
				if (level0Definition != null)
				{
					this.OnEmpirePlanUnlocked(level0Definition);
				}
				else
				{
					Diagnostics.Assert(this.currentEmpirePlanDefinitionByClass != null && this.currentEmpirePlanDefinitionByClass.ContainsKey(empirePlan.EmpirePlanClass));
					this.currentEmpirePlanDefinitionByClass[empirePlan.EmpirePlanClass] = null;
					Diagnostics.LogError("The empire level 0 doesn't exist for class {0}.", new object[]
					{
						empirePlan.EmpirePlanClass
					});
				}
				Diagnostics.LogWarning("Can't apply the empire plan choiced {0}, it is replaced by empire plan {1}.", new object[]
				{
					empirePlan.Name,
					(level0Definition == null) ? "-" : level0Definition.Name.ToString()
				});
			}
		}
		Diagnostics.Assert(this.empirePlanQueue.Length == 0);
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_RefreshEmpirePlanChoiceRemainingTurn(string context, string name)
	{
		this.EmpirePlanChoiceRemainingTurn--;
		if (this.EmpirePlanChoiceRemainingTurn < 0)
		{
			this.EmpirePlanChoiceRemainingTurn = this.empirePlanPeriod;
		}
		this.empirePlanSimulationWrapper.SetPropertyBaseValue(SimulationProperties.RemainingTime, (float)(this.EmpirePlanChoiceRemainingTurn + 1));
		if (this.EmpirePlanChoiceRemainingTurn == this.empirePlanImminentNotificationTurnCount)
		{
			EventEmpirePlanImminent eventEmpirePlanImminent = new EventEmpirePlanImminent(base.Empire, this.EmpirePlanChoiceRemainingTurn);
			this.eventService.Notify(eventEmpirePlanImminent);
		}
		yield break;
	}

	private IEnumerator GameServerState_Turn_Begin_QueueDefaultPlan(string context, string name)
	{
		if (!this.IsEmpirePlanChoiceTurn)
		{
			yield break;
		}
		Diagnostics.Assert(this.currentEmpirePlanDefinitionByClass != null);
		Diagnostics.Assert(base.Empire != null);
		using (Dictionary<StaticString, EmpirePlanDefinition>.Enumerator enumerator = this.currentEmpirePlanDefinitionByClass.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				KeyValuePair<StaticString, EmpirePlanDefinition> empirePlanDefinition = enumerator.Current;
				if (!this.empirePlanQueue.Contains((Construction construction) => (construction.ConstructibleElement as EmpirePlanDefinition).EmpirePlanClass == empirePlanDefinition.Key))
				{
					OrderChangeEmpirePlan order = new OrderChangeEmpirePlan(base.Empire.Index, empirePlanDefinition.Key, (empirePlanDefinition.Value == null) ? 0 : empirePlanDefinition.Value.EmpirePlanLevel);
					global::Empire empire = base.Empire as global::Empire;
					Diagnostics.Assert(empire != null && empire.PlayerControllers.Server != null);
					empire.PlayerControllers.Server.PostOrder(order);
				}
			}
			yield break;
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_BoostersAutoBuyOut(string context, string name)
	{
		IDatabase<BoosterDefinition> boosterDefinitionDatabase = Databases.GetDatabase<BoosterDefinition>(false);
		if (boosterDefinitionDatabase != null)
		{
			foreach (BoosterDefinition currentBoosterDefinition in boosterDefinitionDatabase.GetValues())
			{
				if (currentBoosterDefinition != null && currentBoosterDefinition.AutoBuyOut && DepartmentOfTheTreasury.CheckConstructiblePrerequisites(base.Empire, currentBoosterDefinition, new string[]
				{
					ConstructionFlags.Prerequisite
				}) && this.departmentOfTheTreasury.CheckConstructibleInstantCosts(base.Empire, currentBoosterDefinition))
				{
					OrderBuyoutAndActivateBooster order = new OrderBuyoutAndActivateBooster(base.Empire.Index, currentBoosterDefinition.Name, 0UL, false);
					global::Empire empire = base.Empire as global::Empire;
					empire.PlayerControllers.Client.PostOrder(order);
				}
			}
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_Begin_BoostersNotification(string context, string name)
	{
		if (this.playerControllerRepositoryService.ActivePlayerController.Empire != base.Empire)
		{
			yield break;
		}
		if (this.boosters.Count > 0)
		{
			foreach (KeyValuePair<GameEntityGUID, Booster> keyValuePair in this.boosters)
			{
				if (!keyValuePair.Value.BoosterDefinition.AutoBuyOut)
				{
					this.turnWithoutBooster = 0;
					yield break;
				}
			}
		}
		IDatabase<BoosterDefinition> boosterDefinitionDatabase = Databases.GetDatabase<BoosterDefinition>(false);
		if (boosterDefinitionDatabase != null)
		{
			foreach (BoosterDefinition currentBoosterDefinition in boosterDefinitionDatabase.GetValues())
			{
				if (currentBoosterDefinition != null && currentBoosterDefinition.Costs != null && currentBoosterDefinition.Costs.Length > 0 && currentBoosterDefinition.NotifyUnused && Booster.CanActivate(currentBoosterDefinition, base.Empire) && this.departmentOfTheTreasury.CheckConstructibleInstantCosts(base.Empire, currentBoosterDefinition))
				{
					this.turnWithoutBooster++;
					if (this.turnWithoutBooster > this.maximalTurnWithoutBooster)
					{
						this.turnWithoutBooster = 0;
						this.eventService.Notify(new EventBoosterUnused(base.Empire));
					}
					yield break;
				}
			}
		}
		this.turnWithoutBooster = 0;
		yield break;
	}

	private IEnumerator GameClientState_Turn_Begin_PillarsNotification(string context, string name)
	{
		if (this.playerControllerRepositoryService.ActivePlayerController.Empire != base.Empire)
		{
			yield break;
		}
		foreach (Pillar pillar in this.pillarRepositoryService.AsEnumerable(base.Empire.Index))
		{
			if (!pillar.IsExpired())
			{
				this.turnWithoutPillar = 0;
				yield break;
			}
		}
		IDatabase<PillarDefinition> pillarDefinitionDatabase = Databases.GetDatabase<PillarDefinition>(false);
		if (pillarDefinitionDatabase != null)
		{
			foreach (PillarDefinition pillarDefinition in pillarDefinitionDatabase)
			{
				if (this.departmentOfTheTreasury.CheckConstructibleInstantCosts(base.Empire, pillarDefinition) && DepartmentOfTheTreasury.CheckConstructiblePrerequisites(base.Empire, pillarDefinition, new string[]
				{
					ConstructionFlags.Prerequisite
				}))
				{
					this.turnWithoutPillar++;
					if (this.turnWithoutPillar > this.maximalTurnWithoutPillar)
					{
						this.turnWithoutPillar = 0;
						this.eventService.Notify(new EventPillarUnused(base.Empire));
					}
					yield break;
				}
			}
		}
		this.turnWithoutBooster = 0;
		yield break;
	}

	private IEnumerator GameClientState_Turn_Begin_NumberOfTurnsSinceSeasonStart(string contex, string name)
	{
		if (base.Empire == null || this.SeasonService == null || this.SeasonService.GetCurrentSeason() == null)
		{
			yield break;
		}
		IGameService gameService = Services.GetService<IGameService>();
		float numberOfTurnsSinceSeasonStart = (float)((gameService.Game as global::Game).Turn - this.SeasonService.GetExactSeasonStartTurn(this.SeasonService.GetCurrentSeason()));
		base.Empire.SetPropertyBaseValue("NumberOfTurnsSinceSeasonStart", numberOfTurnsSinceSeasonStart);
		float numberOfTurnsSinceSummerStart = (float)((gameService.Game as global::Game).Turn - this.SeasonService.GetExactInitialSummerStartTurn());
		base.Empire.SetPropertyBaseValue("NumberOfTurnsSinceSummerStart", numberOfTurnsSinceSummerStart);
		yield break;
	}

	private void DepartmentOfTheTreasury_ResourcePropertyChange(object sender, ResourcePropertyChangeEventArgs e)
	{
		string text = string.Empty;
		float boosterCost = 0f;
		float stockCount = 0f;
		if (e.ResourcePropertyName == SimulationProperties.CadaverStock)
		{
			text = "BoosterCadavers";
			boosterCost = base.Empire.GetPropertyValue(SimulationProperties.CadaverCountNeededToObtainBooster);
			stockCount = base.Empire.GetPropertyValue(SimulationProperties.CadaverStock);
		}
		else if (e.ResourcePropertyName == SimulationProperties.SiegeDamageStock)
		{
			DepartmentOfScience agency = base.Empire.GetAgency<DepartmentOfScience>();
			Diagnostics.Assert(agency != null);
			DepartmentOfScience.ConstructibleElement.State technologyState = agency.GetTechnologyState("TechnologyDefinitionFlames9");
			if (technologyState == DepartmentOfScience.ConstructibleElement.State.Researched)
			{
				text = "FlamesIndustryBooster";
				boosterCost = base.Empire.GetPropertyValue(SimulationProperties.SiegeDamageNeededToObtainBooster);
				stockCount = base.Empire.GetPropertyValue(SimulationProperties.SiegeDamageStock);
			}
		}
		if (!string.IsNullOrEmpty(text))
		{
			this.AdquireBooster(text, boosterCost, stockCount);
		}
	}

	private void AdquireBooster(string boosterName, float boosterCost, float stockCount)
	{
		IDatabase<BoosterDefinition> database = Databases.GetDatabase<BoosterDefinition>(false);
		BoosterDefinition boosterDefinition;
		if (database != null && database.TryGetValue(boosterName, out boosterDefinition) && boosterDefinition != null && DepartmentOfTheTreasury.CheckConstructiblePrerequisites(base.Empire, boosterDefinition, new string[]
		{
			ConstructionFlags.Prerequisite
		}) && boosterCost > 0f)
		{
			int num = (int)Math.Truncate((double)(stockCount / boosterCost));
			for (int i = 0; i < num; i++)
			{
				OrderBuyoutAndActivateBooster order = new OrderBuyoutAndActivateBooster(base.Empire.Index, boosterDefinition.Name, 0UL, false);
				global::Empire empire = base.Empire as global::Empire;
				empire.PlayerControllers.Client.PostOrder(order);
			}
		}
	}

	private IEnumerator GameClientState_Turn_End_RefreshBoosters(string context, string name)
	{
		if (this.boosters.Count > 0)
		{
			ResourceDefinition affinityStrategicResource = this.departmentOfTheTreasury.GetAffinityStrategicResource();
			foreach (KeyValuePair<GameEntityGUID, Booster> keyValuePair2 in this.boosters)
			{
				keyValuePair2.Value.OnEndTurn();
			}
			List<Booster> expired = new List<Booster>(from keyValuePair in this.boosters
			where keyValuePair.Value.IsExpired()
			select keyValuePair.Value);
			foreach (Booster booster in expired)
			{
				StaticString boosterResourceName = this.GetBoosterResourceName(booster.BoosterDefinition.XmlSerializableName);
				if (!StaticString.IsNullOrEmpty(boosterResourceName) && affinityStrategicResource != null && affinityStrategicResource.Name == boosterResourceName)
				{
					OrderRemoveAffinityStrategicResource orderRemoveAffinityStrategicResource = new OrderRemoveAffinityStrategicResource(base.Empire.Index, boosterResourceName);
					global::Empire empire = base.Empire as global::Empire;
					empire.PlayerControllers.Client.PostOrder(orderRemoveAffinityStrategicResource);
				}
				this.boosters.Remove(booster.GUID);
				this.OnBoosterCollectionChange(new BoosterCollectionChangeEventArgs(BoosterCollectionChangeAction.Remove, booster));
				this.gameEntityRepositoryService.Unregister(booster);
				IEventService eventService = Services.GetService<IEventService>();
				if (eventService != null)
				{
					eventService.Notify(new EventBoosterEnded(base.Empire, booster.BoosterDefinition));
				}
				booster.Dispose();
			}
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_ComputeEmpirePoint(string context, string name)
	{
		if (!this.IsEmpirePlanChoiceTurn)
		{
			yield break;
		}
		Diagnostics.Assert(this.empirePlanQueue != null);
		ReadOnlyCollection<Construction> pendingPlans = this.empirePlanQueue.PendingConstructions;
		Diagnostics.Assert(pendingPlans != null);
		for (int pendingConstructionIndex = 0; pendingConstructionIndex < pendingPlans.Count; pendingConstructionIndex++)
		{
			Construction pendingEmpirePlan = pendingPlans[pendingConstructionIndex];
			if (pendingEmpirePlan == null)
			{
				Diagnostics.LogError("A pending research is null.");
			}
			else if (pendingEmpirePlan.ConstructibleElement == null)
			{
				Diagnostics.LogError("A pending researched technology is null.");
			}
			else if (pendingEmpirePlan.ConstructibleElement.Costs != null && pendingEmpirePlan.ConstructibleElement.Costs.Length != 0)
			{
				bool constructionComplete = true;
				for (int index = 0; index < pendingEmpirePlan.ConstructibleElement.Costs.Length; index++)
				{
					Diagnostics.Assert(pendingEmpirePlan.ConstructibleElement.Costs[index] != null);
					if (!pendingEmpirePlan.ConstructibleElement.Costs[index].Instant && !pendingEmpirePlan.ConstructibleElement.Costs[index].InstantOnCompletion)
					{
						Diagnostics.Assert(pendingEmpirePlan.CurrentConstructionStock != null);
						ConstructionResourceStock constructionResourceStock = pendingEmpirePlan.CurrentConstructionStock[index];
						StaticString resourceName = pendingEmpirePlan.ConstructibleElement.Costs[index].ResourceName;
						Diagnostics.Assert(constructionResourceStock != null);
						float accumulatedStock = constructionResourceStock.Stock;
						float constructibleCost = DepartmentOfTheTreasury.GetProductionCostWithBonus(base.Empire.SimulationObject, pendingEmpirePlan.ConstructibleElement, pendingEmpirePlan.ConstructibleElement.Costs[index], false);
						float remainingCost = constructibleCost - accumulatedStock;
						if (remainingCost > 0f)
						{
							Diagnostics.Assert(base.Empire != null);
							Diagnostics.Assert(this.departmentOfTheTreasury != null);
							float resourceStock;
							if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.Empire.SimulationObject, resourceName, out resourceStock, false))
							{
								Diagnostics.LogError("Can't get resource stock value {0} on empire.", new object[]
								{
									resourceName
								});
							}
							else if (remainingCost <= resourceStock)
							{
								float usedStock = Math.Min(remainingCost, resourceStock);
								if (this.departmentOfTheTreasury.TryTransferResources(base.Empire.SimulationObject, resourceName, -usedStock))
								{
									constructionResourceStock.Stock += usedStock;
								}
								else
								{
									Diagnostics.LogWarning("Transfer of resource '{0}' is not possible.", new object[]
									{
										pendingEmpirePlan.ConstructibleElement.Costs[index].ResourceName
									});
								}
								remainingCost = constructibleCost - constructionResourceStock.Stock;
								constructionComplete &= (remainingCost <= 0f);
							}
						}
					}
				}
				if (!constructionComplete)
				{
					break;
				}
			}
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_UpdateLocalWinterImmunity(string context, string name)
	{
		if (this.SeasonService != null)
		{
			int maxBid = this.SeasonService.ImmunityBids.Max();
			List<int> potentialWinners = new List<int>();
			if (this.SeasonService.GetCurrentSeason().SeasonDefinition.SeasonType == Season.ReadOnlySummer)
			{
				this.LocalWinterImmunityWinnerIndex = -1;
				int index = 0;
				int length = this.SeasonService.ImmunityBids.Length;
				while (index < length)
				{
					if (this.SeasonService.ImmunityBids[index] == maxBid && maxBid > 0)
					{
						potentialWinners.Add(index);
					}
					index++;
				}
				if (potentialWinners.Count > 1)
				{
					int randomPick = UnityEngine.Random.Range(0, potentialWinners.Count);
					this.LocalWinterImmunityWinnerIndex = randomPick;
				}
				else if (potentialWinners.Count > 0)
				{
					this.LocalWinterImmunityWinnerIndex = potentialWinners[0];
				}
				if (this.LocalWinterImmunityWinnerIndex >= 0)
				{
					this.LocalWinterImmunityWinnerBid = this.SeasonService.GetImmunityBid(this.LocalWinterImmunityWinnerIndex);
				}
				else
				{
					this.LocalWinterImmunityWinnerBid = 0;
				}
			}
		}
		yield break;
	}

	private void OnEmpirePlanQueueChanged(Construction contruction, ConstructionChangeEventAction action)
	{
		if (contruction == null)
		{
			throw new ArgumentNullException("contruction");
		}
		if (action == ConstructionChangeEventAction.Completed)
		{
			this.OnEmpirePlanUnlocked((DepartmentOfPlanificationAndDevelopment.ConstructibleElement)contruction.ConstructibleElement);
		}
		if (this.EmpirePlanQueueChanged != null)
		{
			this.EmpirePlanQueueChanged(this, new ConstructionChangeEventArgs(action, null, contruction));
		}
	}

	private void OnEmpirePlanUnlocked(DepartmentOfPlanificationAndDevelopment.ConstructibleElement empirePlan)
	{
		if (empirePlan == null)
		{
			throw new ArgumentNullException("empirePlan");
		}
		this.UnlockEmpirePlan(empirePlan);
		if (this.EmpirePlanUnlocked != null)
		{
			this.EmpirePlanUnlocked(this, new ConstructibleElementEventArgs(empirePlan));
		}
	}

	private void OnTradeRouteChanged(TradeRoute changedTradeRoute = null)
	{
		if (this.TradeRouteChanged != null)
		{
			this.TradeRouteChanged(this, new TradeRouteChangedEventArgs(changedTradeRoute));
		}
	}

	private void UnlockEmpirePlan(DepartmentOfPlanificationAndDevelopment.ConstructibleElement empirePlan)
	{
		if (empirePlan == null)
		{
			throw new ArgumentNullException("empirePlan");
		}
		EmpirePlanDefinition empirePlanDefinition = empirePlan as EmpirePlanDefinition;
		Diagnostics.Assert(empirePlanDefinition != null && empirePlanDefinition.EmpirePlanClass != null);
		Diagnostics.Assert(this.currentEmpirePlanDefinitionByClass != null);
		Diagnostics.Assert(this.empirePlanSimulationWrapper != null);
		this.empirePlanSimulationWrapper.RemoveDescriptorByType(empirePlanDefinition.EmpirePlanClass);
		this.currentEmpirePlanDefinitionByClass[empirePlanDefinition.EmpirePlanClass] = empirePlanDefinition;
		this.ApplyEmpirePlanDescriptors(empirePlanDefinition);
		Diagnostics.Assert(base.Empire != null);
		base.Empire.Refresh(false);
	}

	private void UpdateIntermediateRegions(TradeRoute tradeRoute, bool positive)
	{
		if (tradeRoute == null)
		{
			throw new ArgumentNullException("tradeRoute");
		}
		if (tradeRoute.SimulationObject == null)
		{
			return;
		}
		if (positive)
		{
			SimulationDescriptor descriptorFromType = tradeRoute.SimulationObject.GetDescriptorFromType("TradeRouteStatus");
			if (descriptorFromType != null)
			{
				return;
			}
		}
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		Diagnostics.Assert(service.Game != null);
		Diagnostics.Assert(service.Game is global::Game);
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		for (int i = 0; i < tradeRoute.IntermediateRegions.Count - 1; i++)
		{
			Region region = service2.GetRegion((int)tradeRoute.IntermediateRegions[i]);
			if (region != null && region.City != null)
			{
				float num = region.City.GetPropertyBaseValue(SimulationProperties.IntermediateTradeRoutesCount);
				float num2 = region.City.GetPropertyBaseValue(SimulationProperties.IntermediateTradeRoutesDistance);
				float num3 = region.City.GetPropertyBaseValue(SimulationProperties.IntermediateTradeRoutesGain);
				if (positive)
				{
					num += 1f;
					num2 += (float)tradeRoute.Distance;
					num3 += tradeRoute.SimulationObject.GetPropertyBaseValue(SimulationProperties.TradeRouteGain);
				}
				else
				{
					num -= 1f;
					num2 -= (float)tradeRoute.Distance;
					num3 -= tradeRoute.SimulationObject.GetPropertyBaseValue(SimulationProperties.TradeRouteGain);
				}
				region.City.SetPropertyBaseValue(SimulationProperties.IntermediateTradeRoutesCount, num);
				region.City.SetPropertyBaseValue(SimulationProperties.IntermediateTradeRoutesDistance, num2);
				region.City.SetPropertyBaseValue(SimulationProperties.IntermediateTradeRoutesGain, num3);
				region.City.Refresh(true);
			}
		}
	}

	private void ApplyEmpirePlanDescriptors(EmpirePlanDefinition empirePlanDefinition)
	{
		if (empirePlanDefinition == null)
		{
			throw new ArgumentNullException("empirePlanDefinition");
		}
		SimulationDescriptor[] descriptors = empirePlanDefinition.Descriptors;
		if (descriptors != null)
		{
			foreach (SimulationDescriptor descriptor in descriptors)
			{
				Diagnostics.Assert(this.empirePlanSimulationWrapper != null);
				this.empirePlanSimulationWrapper.AddDescriptor(descriptor, false);
			}
		}
		EmpirePlanDefinition[] empirePlanDefinitionUnlocks = empirePlanDefinition.EmpirePlanDefinitionUnlocks;
		if (empirePlanDefinitionUnlocks != null)
		{
			foreach (EmpirePlanDefinition empirePlanDefinition2 in empirePlanDefinitionUnlocks)
			{
				if (empirePlanDefinition2 != null)
				{
					this.ApplyEmpirePlanDescriptors(empirePlanDefinition2);
				}
			}
		}
	}

	private bool CheckWhetherTradeRouteIsRealyAffectedByRelation(City city)
	{
		Diagnostics.Assert(city != null);
		return city.Hero == null || !city.Hero.IsSkillUnlocked(DepartmentOfPlanificationAndDevelopment.HeroSkillGovernor25);
	}

	private bool CheckWhetherTradeRouteIsRealyAffectedBySiege(City city)
	{
		Diagnostics.Assert(city != null);
		return city.Hero == null || !city.Hero.IsSkillUnlocked(DepartmentOfPlanificationAndDevelopment.HeroSkillGovernor28);
	}

	private bool CheckEmpireplanPrerequisite(EmpirePlanDefinition empirePlan)
	{
		bool result = true;
		for (int i = 0; i < empirePlan.Prerequisites.Length; i++)
		{
			bool flag = empirePlan.Prerequisites[i].Check(base.Empire.SimulationObject);
			bool flag2 = empirePlan.Prerequisites[i].Flags.Contains(DepartmentOfPlanificationAndDevelopment.AlternateEmpirePlan);
			if (!flag && flag2)
			{
				result = false;
			}
		}
		return result;
	}

	private bool UpdateRelationForTradeRouteWithIntermediateRegionsOfTypeOceanic(City city, TradeRoute tradeRoute, ref bool tradeRouteIsAffectedByDiplomaticRelationChange)
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		Diagnostics.Assert(service.Game != null);
		Diagnostics.Assert(service.Game is global::Game);
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		DepartmentOfForeignAffairs agency = base.Empire.GetAgency<DepartmentOfForeignAffairs>();
		Diagnostics.Assert(agency != null);
		bool result = false;
		bool flag = false;
		if (city.Hero != null)
		{
			flag = city.Hero.IsSkillUnlocked(DepartmentOfPlanificationAndDevelopment.HeroSkillGovernor25);
		}
		TradeRouteFlags tradeRouteFlags = TradeRouteFlags.None;
		for (int i = 0; i < tradeRoute.IntermediateRegionsOfTypeOceanic.Count; i++)
		{
			short regionIndex = tradeRoute.IntermediateRegionsOfTypeOceanic[i];
			Region region = service2.GetRegion((int)regionIndex);
			Diagnostics.Assert(region != null);
			Diagnostics.Assert(region.IsOcean);
			global::Empire owner = region.Owner;
			if (owner is MajorEmpire)
			{
				if (owner == base.Empire)
				{
					tradeRouteFlags |= TradeRouteFlags.PassingThroughOceanicRegionOfMine;
				}
				else
				{
					DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(owner);
					bool flag2 = diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.TradeRoute);
					bool flag3 = flag || flag2;
					if (flag2)
					{
						tradeRouteFlags |= TradeRouteFlags.PassingThroughOceanicRegionOfSomeAllyOfMine;
					}
					if (!flag3)
					{
						tradeRouteIsAffectedByDiplomaticRelationChange = true;
					}
				}
			}
		}
		if (tradeRouteFlags != tradeRoute.TradeRouteFlags)
		{
			if ((tradeRoute.TradeRouteFlags & TradeRouteFlags.PassingThroughOceanicRegionOfMine) != (tradeRouteFlags & TradeRouteFlags.PassingThroughOceanicRegionOfMine))
			{
				if ((tradeRouteFlags & TradeRouteFlags.PassingThroughOceanicRegionOfMine) == TradeRouteFlags.PassingThroughOceanicRegionOfMine)
				{
					SimulationDescriptor descriptor;
					if (this.simulationDescriptorsDatatable.TryGetValue(TradeRoute.TradeRoutePassingThroughOceanicRegionOfMine, out descriptor) && !tradeRoute.SimulationObject.Tags.Contains(TradeRoute.TradeRoutePassingThroughOceanicRegionOfMine))
					{
						tradeRoute.SimulationObject.AddDescriptor(descriptor);
						tradeRoute.SimulationObject.Refresh();
					}
				}
				else
				{
					tradeRoute.SimulationObject.RemoveDescriptorByName(TradeRoute.TradeRoutePassingThroughOceanicRegionOfMine);
				}
			}
			if ((tradeRoute.TradeRouteFlags & TradeRouteFlags.PassingThroughOceanicRegionOfSomeAllyOfMine) != (tradeRouteFlags & TradeRouteFlags.PassingThroughOceanicRegionOfSomeAllyOfMine))
			{
				if ((tradeRouteFlags & TradeRouteFlags.PassingThroughOceanicRegionOfSomeAllyOfMine) == TradeRouteFlags.PassingThroughOceanicRegionOfSomeAllyOfMine)
				{
					SimulationDescriptor descriptor;
					if (this.simulationDescriptorsDatatable.TryGetValue(TradeRoute.TradeRoutePassingThroughOceanicRegionOfSomeAllyOfMine, out descriptor) && !tradeRoute.SimulationObject.Tags.Contains(TradeRoute.TradeRoutePassingThroughOceanicRegionOfSomeAllyOfMine))
					{
						tradeRoute.SimulationObject.AddDescriptor(descriptor);
						tradeRoute.SimulationObject.Refresh();
					}
				}
				else
				{
					tradeRoute.SimulationObject.RemoveDescriptorByName(TradeRoute.TradeRoutePassingThroughOceanicRegionOfSomeAllyOfMine);
				}
			}
			tradeRoute.TradeRouteFlags = tradeRouteFlags;
			result = true;
			this.OnTradeRouteChanged(tradeRoute);
		}
		return result;
	}

	public static readonly StaticString HeroSkillGovernor25 = new StaticString("HeroSkillGovernor25");

	public static readonly StaticString HeroSkillGovernor28 = new StaticString("HeroSkillGovernor28");

	public static readonly StaticString AlternateEmpirePlan = new StaticString("AlternateEmpirePlan");

	private readonly Dictionary<StaticString, EmpirePlanDefinition[]> empirePlanDefinitionsByClass = new Dictionary<StaticString, EmpirePlanDefinition[]>();

	private readonly Dictionary<StaticString, EmpirePlanDefinition> currentEmpirePlanDefinitionByClass = new Dictionary<StaticString, EmpirePlanDefinition>();

	private readonly Dictionary<GameEntityGUID, Booster> boosters = new Dictionary<GameEntityGUID, Booster>();

	private readonly List<DepartmentOfPlanificationAndDevelopment.DeserializedBoosterInfo> deserializedBoosterInfo = new List<DepartmentOfPlanificationAndDevelopment.DeserializedBoosterInfo>();

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private IDatabase<DepartmentOfPlanificationAndDevelopment.ConstructibleElement> empirePlanDatabase;

	private IEventService eventService;

	private int empirePlanPeriod = 19;

	private ConstructionQueue empirePlanQueue = new ConstructionQueue();

	private SimulationObjectWrapper empirePlanSimulationWrapper;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private ITradeManagementService tradeManagementService;

	private IPlayerControllerRepositoryService playerControllerRepositoryService;

	private IPillarRepositoryService pillarRepositoryService;

	private ISeasonService seasonService;

	private IDownloadableContentService downloadableContentService;

	private IDatabase<SimulationDescriptor> simulationDescriptorsDatatable;

	private int empirePlanImminentNotificationTurnCount;

	private bool isEmpirePlanChoiced;

	private int turnWithoutBooster;

	private int maximalTurnWithoutBooster = 10;

	private int turnWithoutPillar;

	private int maximalTurnWithoutPillar = 10;

	private int localWinterImmunityWinnerIndex = -1;

	public abstract class ConstructibleElement : global::ConstructibleElement
	{
	}

	protected struct DeserializedBoosterInfo
	{
		public string BoosterDefinitionName;

		public ulong GUID;

		public ulong TargetGUID;

		public int RemainingTime;

		public int Duration;

		public int TurnWhenStarted;

		public int InstigatorEmpireIndex;
	}

	public static class Registers
	{
		public static StaticString TradeRoutesBaseMultiplier = new StaticString("Gameplay/Agencies/DepartmentOfPlanificationAndDevelopment/TradeRoutes/BaseMultiplier");

		public static StaticString TradeRoutesDistanceMultiplier = new StaticString("Gameplay/Agencies/DepartmentOfPlanificationAndDevelopment/TradeRoutes/DistanceMultiplier");
	}

	private class TradeRouteComparer : IComparer<TradeRoute>
	{
		int IComparer<TradeRoute>.Compare(TradeRoute x, TradeRoute y)
		{
			if (x.IntermediateRegions.Count != y.IntermediateRegions.Count)
			{
				return y.IntermediateRegions.Count - x.IntermediateRegions.Count;
			}
			return (int)(x.Distance - y.Distance);
		}
	}
}
