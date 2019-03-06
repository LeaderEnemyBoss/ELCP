using System;
using System.IO;
using Amplitude;
using Amplitude.IO;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public abstract class Tradable : IXmlSerializable, IBinarySerializable, ITradable
{
	public Tradable()
	{
	}

	public virtual void Deserialize(BinaryReader reader)
	{
		this.EmpireExclusionBits = reader.ReadInt32();
		this.Quantity = reader.ReadSingle();
		string x = reader.ReadString();
		IDatabase<TradableCategoryDefinition> database = Databases.GetDatabase<TradableCategoryDefinition>(false);
		TradableCategoryDefinition tradableCategoryDefinition;
		if (database != null && database.TryGetValue(x, out tradableCategoryDefinition))
		{
			this.TradableCategoryDefinition = tradableCategoryDefinition;
		}
		this.TurnWhenFirstPlacedOnMarket = reader.ReadInt32();
		this.TurnWhenLastExchangedOnMarket = reader.ReadInt32();
		this.TurnWhenLastHitOnMarket = reader.ReadInt32();
		this.UID = reader.ReadUInt64();
		this.Value = reader.ReadSingle();
	}

	public virtual void Serialize(BinaryWriter writer)
	{
		writer.Write(this.EmpireExclusionBits);
		writer.Write(this.Quantity);
		writer.Write(this.TradableCategoryDefinition.Name);
		writer.Write(this.TurnWhenFirstPlacedOnMarket);
		writer.Write(this.TurnWhenLastExchangedOnMarket);
		writer.Write(this.TurnWhenLastHitOnMarket);
		writer.Write(this.UID);
		writer.Write(this.Value);
	}

	public virtual void ReadXml(XmlReader reader)
	{
		this.EmpireExclusionBits = reader.GetAttribute<int>("EmpireExclusionBits");
		this.Quantity = reader.GetAttribute<float>("Quantity");
		this.TurnWhenFirstPlacedOnMarket = reader.GetAttribute<int>("TurnWhenFirstPlacedOnMarket");
		this.TurnWhenLastExchangedOnMarket = reader.GetAttribute<int>("TurnWhenLastExchangedOnMarket");
		this.TurnWhenLastHitOnMarket = reader.GetAttribute<int>("TurnWhenLastHitOnMarket");
		this.UID = reader.GetAttribute<ulong>("UID");
		this.Value = reader.GetAttribute<float>("Value");
		reader.ReadStartElement();
		string attribute = reader.GetAttribute("Name");
		reader.Skip("TradableCategoryDefinition");
		if (!string.IsNullOrEmpty(attribute))
		{
			IDatabase<TradableCategoryDefinition> database = Databases.GetDatabase<TradableCategoryDefinition>(false);
			if (database != null)
			{
				this.TradableCategoryDefinition = database.GetValue(attribute);
			}
		}
	}

	public virtual void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString("AssemblyQualifiedName", base.GetType().AssemblyQualifiedName);
		writer.WriteAttributeString<int>("EmpireExclusionBits", this.EmpireExclusionBits);
		writer.WriteAttributeString<float>("Quantity", this.Quantity);
		writer.WriteAttributeString<int>("TurnWhenFirstPlacedOnMarket", this.TurnWhenFirstPlacedOnMarket);
		writer.WriteAttributeString<int>("TurnWhenLastExchangedOnMarket", this.TurnWhenLastExchangedOnMarket);
		writer.WriteAttributeString<int>("TurnWhenLastHitOnMarket", this.TurnWhenLastHitOnMarket);
		writer.WriteAttributeString<ulong>("UID", this.UID);
		writer.WriteAttributeString<float>("Value", this.Value);
		writer.WriteStartElement("TradableCategoryDefinition");
		writer.WriteAttributeString<StaticString>("Name", this.TradableCategoryDefinition.Name);
		writer.WriteEndElement();
	}

	~Tradable()
	{
	}

	public int EmpireExclusionBits { get; set; }

	public bool Incompatible { get; set; }

	public float ReservedQuantity { get; set; }

	public float Quantity { get; set; }

	public TradableCategoryDefinition TradableCategoryDefinition { get; set; }

	public int TurnWhenFirstPlacedOnMarket { get; set; }

	public int TurnWhenLastExchangedOnMarket { get; set; }

	public int TurnWhenLastHitOnMarket { get; set; }

	public ulong UID { get; internal set; }

	public float Value { get; set; }

	public static float ApplySalesTaxes(float price, TradableTransactionType transactionType, global::Empire empire)
	{
		float num = 1f;
		if (transactionType != TradableTransactionType.Buyout)
		{
			if (transactionType == TradableTransactionType.Sellout)
			{
				num = Tradable.SelloutMultiplier;
			}
		}
		else
		{
			num = Tradable.BuyoutMultiplier;
		}
		return price * num;
	}

	public static float GetReferencePrice(TradableCategoryDefinition tradableCategoryDefinition, float value = 0f)
	{
		float num = 0f;
		IGameService service = Services.GetService<IGameService>();
		if (service != null)
		{
			global::Game game = service.Game as global::Game;
			if (game != null)
			{
				num = (float)game.Turn * Tradable.InflationMultiplier;
			}
		}
		num *= tradableCategoryDefinition.SensitivityToInflation;
		value *= tradableCategoryDefinition.ValueModifier;
		return (tradableCategoryDefinition.ReferencePrice + value) * (1f + num);
	}

	public static float GetUnitPrice(TradableCategoryDefinition tradableCategoryDefinition, float value = 0f)
	{
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		IGameService service = Services.GetService<IGameService>();
		if (service != null)
		{
			global::Game game = service.Game as global::Game;
			num3 = (float)game.Turn * Tradable.InflationMultiplier;
			ITradeManagementService service2 = game.Services.GetService<ITradeManagementService>();
			TradableCategoryTendency tendency;
			TradableCategoryStockFactor stockFactor;
			if (service2 != null && service2.TryGetTradableCategoryState(tradableCategoryDefinition.Name, out tendency, out stockFactor))
			{
				num = tendency;
				num2 = stockFactor;
			}
		}
		num3 *= tradableCategoryDefinition.SensitivityToInflation;
		num *= tradableCategoryDefinition.SensitivityToTendency;
		num2 *= tradableCategoryDefinition.SensitivityToStockFactor;
		value *= tradableCategoryDefinition.ValueModifier;
		float num4 = tradableCategoryDefinition.ReferencePrice * (1f + num3);
		num4 += value * (1f + num3);
		float val = num4 * (1f + num + num2);
		float val2 = num4 * Tradable.MinimumPriceMultiplier;
		float val3 = num4 * Tradable.MaximumPriceMultiplier;
		return Math.Max(val2, Math.Min(val3, val));
	}

	public float GetPriceWithSalesTaxes(TradableTransactionType transactionType, global::Empire empire)
	{
		float num = Tradable.GetUnitPrice(this.TradableCategoryDefinition, this.Value);
		num *= this.Quantity;
		num = Tradable.ApplySalesTaxes(num, transactionType, empire);
		if (this is TradableUnit && transactionType == TradableTransactionType.Buyout && empire is MajorEmpire && empire.GetPropertyValue(SimulationProperties.MarketplaceMercCostMultiplier) > 0f)
		{
			num *= empire.GetPropertyValue(SimulationProperties.MarketplaceMercCostMultiplier);
		}
		return this.GetPriceWithSeasonEffectModifier(num, empire);
	}

	public float GetPriceWithSalesTaxes(TradableTransactionType transactionType, global::Empire empire, float quantity)
	{
		float num = Tradable.GetUnitPrice(this.TradableCategoryDefinition, this.Value);
		num *= quantity;
		num = Tradable.ApplySalesTaxes(num, transactionType, empire);
		if (this is TradableUnit && transactionType == TradableTransactionType.Buyout && empire is MajorEmpire && empire.GetPropertyValue(SimulationProperties.MarketplaceMercCostMultiplier) > 0f)
		{
			num *= empire.GetPropertyValue(SimulationProperties.MarketplaceMercCostMultiplier);
		}
		return this.GetPriceWithSeasonEffectModifier(num, empire);
	}

	public float GetReferencePriceWithSalesTaxes(TradableTransactionType transactionType, global::Empire empire)
	{
		float price = Tradable.GetReferencePrice(this.TradableCategoryDefinition, this.Value);
		price = Tradable.ApplySalesTaxes(price, transactionType, empire);
		return this.GetPriceWithSeasonEffectModifier(price, empire);
	}

	public virtual bool IsTradableValid(global::Empire empire)
	{
		return true;
	}

	public abstract bool TryAllocateTo(global::Empire empire, float quantity, params object[] parameters);

	public abstract bool TryGetReferenceName(global::Empire empire, out string referenceName);

	private float GetPriceWithSeasonEffectModifier(float price, global::Empire empire)
	{
		if (empire == null || empire.SimulationObject == null)
		{
			return price;
		}
		if (this is TradableUnit || this is TradableHero)
		{
			if (empire.SimulationObject.Tags.Contains("SeasonEffectMarketplace1"))
			{
				price *= empire.GetPropertyValue(SimulationProperties.MarketplaceUnitCostMultiplier);
			}
		}
		else if (this is TradableResource)
		{
			if ((this as TradableResource).ResourceName.Contains("Luxury"))
			{
				price *= empire.GetPropertyValue(SimulationProperties.MarketplaceLuxuryCostMultiplier);
			}
			else if ((this as TradableResource).ResourceName.Contains("Strategic"))
			{
				price *= empire.GetPropertyValue(SimulationProperties.MarketplaceStrategicCostMultiplier);
			}
		}
		else if (this is TradableBooster)
		{
			if ((this as TradableBooster).BoosterDefinitionName == "BoosterFood")
			{
				price *= empire.GetPropertyValue(SimulationProperties.MarketplaceBoosterFoodCostMultiplier);
			}
			else if ((this as TradableBooster).BoosterDefinitionName == "BoosterIndustry")
			{
				price *= empire.GetPropertyValue(SimulationProperties.MarketplaceBoosterIndustryCostMultiplier);
			}
		}
		return price;
	}

	public static float BuyoutMultiplier = 1.2f;

	public static float DepreciationMultiplier = -0.05f;

	public static float InflationMultiplier = 0.01f;

	public static float PositiveTendencyMultiplier = 0.1f;

	public static float MaximumPriceMultiplier = 2f;

	public static float MinimumPriceMultiplier = 0.5f;

	public static float NegativeTendencyMultiplier = -0.1f;

	public static float SelloutMultiplier = 0.8f;

	public static float UnitLevelMultiplier = 0.1f;
}
