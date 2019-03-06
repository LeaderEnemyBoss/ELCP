using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Extensions;
using Amplitude.IO;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Serialization;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class Marketplace : GameAncillary, Amplitude.Xml.Serialization.IXmlSerializable, IBinarySerializable, IService, IDumpable, ITradeManagementService
{
	public Marketplace()
	{
		this.TurnWhenLastCollected = -1;
	}

	public event EventHandler<TradableCollectionChangeEventArgs> CollectionChange;

	public event EventHandler<TradableTransactionCompleteEventArgs> TransactionComplete;

	void IBinarySerializable.Deserialize(BinaryReader reader)
	{
		int num = reader.ReadInt32();
		while (num-- > 0)
		{
			string key = reader.ReadString();
			int num2 = reader.ReadInt32();
			List<Tradable> list = new List<Tradable>();
			while (num2-- > 0)
			{
				string typeName = reader.ReadString();
				try
				{
					Type type = Type.GetType(typeName);
					if (type != null)
					{
						Tradable tradable = Activator.CreateInstance(type, true) as Tradable;
						if (tradable != null)
						{
							tradable.Deserialize(reader);
							list.Add(tradable);
						}
					}
				}
				catch
				{
					throw;
				}
			}
			this.tradablesPerCategory.Add(key, list);
		}
		this.OnCollectionChange(new TradableCollectionChangeEventArgs(null, null));
	}

	void IBinarySerializable.Serialize(BinaryWriter writer)
	{
		writer.Write(this.tradablesPerCategory.Count);
		foreach (KeyValuePair<string, List<Tradable>> keyValuePair in this.tradablesPerCategory)
		{
			writer.Write(keyValuePair.Key);
			writer.Write(keyValuePair.Value.Count);
			for (int i = 0; i < keyValuePair.Value.Count; i++)
			{
				ITradable tradable = keyValuePair.Value[i];
				writer.Write(tradable.GetType().AssemblyQualifiedName);
				tradable.Serialize(writer);
			}
		}
	}

	void ITradeManagementService.Clear()
	{
		this.tradablesPerCategory.Clear();
	}

	Tradable ITradeManagementService.CreateNewTradableBooster(StaticString boosterDefinitionName, float quantity, params GameEntityGUID[] gameEntityGUIDs)
	{
		if (StaticString.IsNullOrEmpty(boosterDefinitionName))
		{
			throw new ArgumentException("Booster definition name is either null or empty", "boosterDefinitionName");
		}
		StaticString staticString = "Tradable" + boosterDefinitionName;
		TradableCategoryDefinition tradableCategoryDefinition;
		if (!this.TradableCategoryDefinitions.TryGetValue(staticString, out tradableCategoryDefinition))
		{
			Diagnostics.LogError("Unable to find tradable category definition (name: '{0}').", new object[]
			{
				staticString
			});
			return null;
		}
		TradableBooster tradableBooster = new TradableBooster();
		tradableBooster.BoosterDefinitionName = boosterDefinitionName;
		tradableBooster.GameEntityGUIDs = gameEntityGUIDs;
		tradableBooster.Quantity = quantity;
		tradableBooster.EmpireExclusionBits = 0;
		tradableBooster.TradableCategoryDefinition = tradableCategoryDefinition;
		tradableBooster.TurnWhenFirstPlacedOnMarket = base.Game.Turn;
		tradableBooster.TurnWhenLastExchangedOnMarket = base.Game.Turn;
		tradableBooster.TurnWhenLastHitOnMarket = base.Game.Turn;
		Tradable tradable = tradableBooster;
		ulong uid;
		this.nextAvailableTradableUID = (uid = this.nextAvailableTradableUID) + 1UL;
		tradable.UID = uid;
		tradableBooster.Value = 0f;
		return tradableBooster;
	}

	Tradable ITradeManagementService.CreateNewTradableHero(Unit unit)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		UnitProfile unitProfile = unit.UnitDesign as UnitProfile;
		if (unitProfile == null || !unitProfile.IsHero)
		{
			throw new ArgumentException("Unit has no unit profile, or unit profile does not describe a hero.", "unit");
		}
		TradableCategoryDefinition value = this.TradableCategoryDefinitions.GetValue("TradableHero");
		if (value == null)
		{
			Diagnostics.LogError("Unable to find tradable category definition (name: 'TradableHero').");
			return null;
		}
		TradableHero tradableHero = new TradableHero();
		tradableHero.Barcode = unit.UnitDesign.Barcode;
		tradableHero.EmpireExclusionBits = 0;
		tradableHero.GameEntityGUID = unit.GUID;
		tradableHero.Level = (short)unit.Level;
		tradableHero.Quantity = 1f;
		tradableHero.TradableCategoryDefinition = value;
		tradableHero.TurnWhenFirstPlacedOnMarket = base.Game.Turn;
		tradableHero.TurnWhenLastExchangedOnMarket = base.Game.Turn;
		tradableHero.TurnWhenLastHitOnMarket = base.Game.Turn;
		Tradable tradable = tradableHero;
		ulong uid;
		this.nextAvailableTradableUID = (uid = this.nextAvailableTradableUID) + 1UL;
		tradable.UID = uid;
		tradableHero.Value = 0f;
		TradableHero tradableHero2 = tradableHero;
		tradableHero2.Value = TradableUnit.GetValue(unit);
		this.ReserveTradableUnitBarcode(tradableHero2, unit.UnitDesign);
		return tradableHero2;
	}

	Tradable ITradeManagementService.CreateNewTradableResources(StaticString resourceName, float quantity)
	{
		if (StaticString.IsNullOrEmpty(resourceName))
		{
			throw new ArgumentException("resourceName", "Resource name is either null or empty.");
		}
		if (quantity <= 0f)
		{
			throw new ArgumentException("quantity", "Quantity is either null or negative.");
		}
		StaticString staticString = "TradableResource" + resourceName;
		TradableCategoryDefinition tradableCategoryDefinition;
		if (!this.TradableCategoryDefinitions.TryGetValue(staticString, out tradableCategoryDefinition))
		{
			Diagnostics.LogError("Unable to find tradable category definition (name: '{0}').", new object[]
			{
				staticString
			});
			return null;
		}
		TradableResource tradableResource = new TradableResource();
		tradableResource.Quantity = quantity;
		tradableResource.ResourceName = resourceName;
		tradableResource.TradableCategoryDefinition = tradableCategoryDefinition;
		tradableResource.TurnWhenFirstPlacedOnMarket = base.Game.Turn;
		tradableResource.TurnWhenLastExchangedOnMarket = base.Game.Turn;
		tradableResource.TurnWhenLastHitOnMarket = base.Game.Turn;
		Tradable tradable = tradableResource;
		ulong uid;
		this.nextAvailableTradableUID = (uid = this.nextAvailableTradableUID) + 1UL;
		tradable.UID = uid;
		tradableResource.Value = 0f;
		return tradableResource;
	}

	Tradable ITradeManagementService.CreateNewTradableUnit(Unit unit)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		TradableCategoryDefinition value = this.TradableCategoryDefinitions.GetValue(TradableUnit.ReadOnlyCategory);
		if (value == null)
		{
			Diagnostics.LogError("Unable to find tradable category definition (name: 'TradableUnit').");
			return null;
		}
		TradableUnit tradableUnit = new TradableUnit();
		tradableUnit.Barcode = unit.UnitDesign.Barcode;
		tradableUnit.EmpireExclusionBits = 0;
		tradableUnit.GameEntityGUID = unit.GUID;
		tradableUnit.LastKnownOrigin = TradableOrigin.UndefinedEmpire;
		tradableUnit.Level = (short)unit.Level;
		tradableUnit.Quantity = 1f;
		tradableUnit.TradableCategoryDefinition = value;
		tradableUnit.TurnWhenFirstPlacedOnMarket = base.Game.Turn;
		tradableUnit.TurnWhenLastExchangedOnMarket = base.Game.Turn;
		tradableUnit.TurnWhenLastHitOnMarket = base.Game.Turn;
		Tradable tradable = tradableUnit;
		ulong uid;
		this.nextAvailableTradableUID = (uid = this.nextAvailableTradableUID) + 1UL;
		tradable.UID = uid;
		tradableUnit.Value = 0f;
		TradableUnit tradableUnit2 = tradableUnit;
		tradableUnit2.Value = TradableUnit.GetValue(unit);
		this.ReserveTradableUnitBarcode(tradableUnit2, unit.UnitDesign);
		return tradableUnit2;
	}

	void ITradeManagementService.Collect()
	{
		IGameService service = Services.GetService<IGameService>();
		if (service == null)
		{
			Diagnostics.LogError("Cannot collect new tradables because the marketplace cannot retrieve the game service.");
			return;
		}
		global::Game game = service.Game as global::Game;
		if (game == null)
		{
			Diagnostics.LogError("Cannot collect new tradables because the game is null.");
			return;
		}
		if (this.TradableCategoryDefinitions == null)
		{
			Diagnostics.LogError("Cannot collect new tradables because the database of tradable category definitions is null.");
			return;
		}
		if (game.Turn <= this.TurnWhenLastCollected)
		{
			return;
		}
		this.TurnWhenLastCollected = game.Turn;
		List<Tradable> list = new List<Tradable>();
		foreach (KeyValuePair<string, Marketplace.Collector> keyValuePair in this.collectors)
		{
			Marketplace.Collector value = keyValuePair.Value;
			List<Tradable> list2 = value(game);
			if (list2 != null && list2.Count > 0)
			{
				list.AddRange(list2);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			Tradable tradable = list[i];
			List<Tradable> list3;
			if (!this.tradablesPerCategory.TryGetValue(tradable.TradableCategoryDefinition.Name, out list3))
			{
				list3 = new List<Tradable>();
				list3.Add(tradable);
				this.tradablesPerCategory.Add(tradable.TradableCategoryDefinition.Name, list3);
				this.tradableCategoryTendencies.Add(tradable.TradableCategoryDefinition.Name, new TradableCategoryTendency());
				this.tradableCategoryStockFactors.Add(tradable.TradableCategoryDefinition.Name, new TradableCategoryStockFactor());
			}
			else
			{
				if (tradable is ITradableWithStacking)
				{
					bool flag = false;
					foreach (Tradable tradable2 in list3)
					{
						if (tradable2 is ITradableWithStacking)
						{
							flag |= (tradable2 as ITradableWithStacking).TryStack(tradable as ITradableWithStacking);
							if (flag)
							{
								tradable.TurnWhenLastHitOnMarket = game.Turn;
								break;
							}
						}
					}
					if (flag)
					{
						goto IL_21B;
					}
				}
				list3.Add(tradable);
			}
			IL_21B:;
		}
		foreach (List<Tradable> list4 in this.tradablesPerCategory.Values)
		{
			foreach (Tradable tradable3 in list4)
			{
				if (tradable3.UID == 0UL)
				{
					Tradable tradable4 = tradable3;
					ulong uid;
					this.nextAvailableTradableUID = (uid = this.nextAvailableTradableUID) + 1UL;
					tradable4.UID = uid;
				}
			}
		}
		foreach (List<Tradable> list5 in this.tradablesPerCategory.Values)
		{
			foreach (Tradable tradable5 in list5)
			{
				if (tradable5.Quantity >= (float)tradable5.TradableCategoryDefinition.MaximumStackSize)
				{
					tradable5.Quantity = (float)tradable5.TradableCategoryDefinition.MaximumStackSize;
				}
			}
		}
		foreach (string key in this.tradablesPerCategory.Keys.ToArray<string>())
		{
			List<Tradable> list6 = this.tradablesPerCategory[key];
			if (list6.Count != 0)
			{
				int maximumLifeTimeOnMarket = list6[0].TradableCategoryDefinition.MaximumLifeTimeOnMarket;
				if (maximumLifeTimeOnMarket > 0)
				{
					int k = 0;
					while (k < list6.Count)
					{
						int num = game.Turn - list6[k].TurnWhenFirstPlacedOnMarket;
						if (num >= maximumLifeTimeOnMarket)
						{
							this.ReleaseTradable(list6[k], true);
							list6.RemoveAt(k);
						}
						else
						{
							k++;
						}
					}
				}
			}
		}
		string[] array;
		foreach (string key2 in array)
		{
			List<Tradable> list7 = this.tradablesPerCategory[key2];
			if (list7.Count != 0)
			{
				int maximumNumberOfOccurencesOnMarket = list7[0].TradableCategoryDefinition.MaximumNumberOfOccurencesOnMarket;
				if (list7.Count > maximumNumberOfOccurencesOnMarket)
				{
					list7 = list7.Randomize(null);
					list7.Sort((Tradable left, Tradable right) => right.TurnWhenLastHitOnMarket.CompareTo(left.TurnWhenLastHitOnMarket));
					this.tradablesPerCategory[key2] = list7.Take(maximumNumberOfOccurencesOnMarket).ToList<Tradable>();
					for (int m = this.tradablesPerCategory[key2].Count; m < list7.Count; m++)
					{
						this.ReleaseTradable(list7[m], true);
					}
				}
			}
		}
		this.OnCollectionChange(new TradableCollectionChangeEventArgs(null, null));
		int num2 = 100;
		if (this.transactions.Count > num2)
		{
			this.transactions.RemoveRange(0, this.transactions.Count - num2);
		}
	}

	bool ITradeManagementService.CollectTradableBooster(TradableBooster tradableBooster)
	{
		if (tradableBooster == null)
		{
			throw new ArgumentNullException("tradableBooster");
		}
		Diagnostics.Assert(tradableBooster.TradableCategoryDefinition != null);
		List<Tradable> list;
		if (!this.tradablesPerCategory.TryGetValue(tradableBooster.TradableCategoryDefinition.Name, out list))
		{
			list = new List<Tradable>();
			this.tradablesPerCategory.Add(tradableBooster.TradableCategoryDefinition.Name, list);
		}
		TradableBooster tradableBooster2 = list.Cast<TradableBooster>().FirstOrDefault((TradableBooster iterator) => iterator.BoosterDefinitionName == tradableBooster.BoosterDefinitionName);
		if (tradableBooster2 != null)
		{
			if (tradableBooster2.GameEntityGUIDs == null)
			{
				tradableBooster2.GameEntityGUIDs = tradableBooster.GameEntityGUIDs;
				tradableBooster2.Quantity = (float)tradableBooster.GameEntityGUIDs.Length;
			}
			else
			{
				int num = tradableBooster2.GameEntityGUIDs.Length + tradableBooster.GameEntityGUIDs.Length;
				GameEntityGUID[] array = new GameEntityGUID[num];
				Array.Copy(tradableBooster2.GameEntityGUIDs, array, tradableBooster2.GameEntityGUIDs.Length);
				Array.Copy(tradableBooster.GameEntityGUIDs, 0, array, tradableBooster2.GameEntityGUIDs.Length, tradableBooster.GameEntityGUIDs.Length);
				tradableBooster2.GameEntityGUIDs = array;
				tradableBooster2.Quantity = (float)array.Length;
			}
		}
		else
		{
			list.Add(tradableBooster);
		}
		this.OnCollectionChange(new TradableCollectionChangeEventArgs(tradableBooster.TradableCategoryDefinition.Name, list.Cast<ITradable>()));
		return true;
	}

	bool ITradeManagementService.CollectTradableUnit(TradableUnit tradableUnit, Unit unit, int empireIndex)
	{
		if (tradableUnit == null)
		{
			throw new ArgumentNullException("tradableUnit");
		}
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		Diagnostics.Assert(tradableUnit.GameEntityGUID == unit.GUID);
		Diagnostics.Assert(tradableUnit.TradableCategoryDefinition != null);
		List<Tradable> list;
		if (!this.tradablesPerCategory.TryGetValue(tradableUnit.TradableCategoryDefinition.Name, out list))
		{
			list = new List<Tradable>();
			this.tradablesPerCategory.Add(tradableUnit.TradableCategoryDefinition.Name, list);
		}
		list.Add(tradableUnit);
		this.ownedCollectionOfUnits.Add(unit.GUID, unit);
		UnitDesign unitDesign;
		if (!this.ownedCollectionOfUnitDesigns.TryGetValue(tradableUnit.Barcode, out unitDesign))
		{
			unitDesign = (UnitDesign)unit.UnitDesign.Clone();
			unitDesign.Model = 0u;
			unitDesign.ModelRevision = 0u;
			unitDesign.Barcode = tradableUnit.Barcode;
			this.ownedCollectionOfUnitDesigns.Add(unitDesign.Barcode, unitDesign);
		}
		unit.UnitDesign = unitDesign;
		unit.SwitchToEmbarkedUnit(false);
		float propertyValue = unit.GetPropertyValue(SimulationProperties.MaximumHealth);
		unit.SetPropertyBaseValue(SimulationProperties.Health, propertyValue);
		unit.SetPropertyBaseValue(SimulationProperties.MovementRatio, 1f);
		unit.SetPropertyBaseValue(SimulationProperties.ActionPointsSpent, 0f);
		unit.Refresh(true);
		this.OnCollectionChange(new TradableCollectionChangeEventArgs(tradableUnit.TradableCategoryDefinition.Name, list.Cast<ITradable>()));
		return true;
	}

	bool ITradeManagementService.CollectTradableResource(TradableResource tradableResource)
	{
		if (tradableResource == null)
		{
			throw new ArgumentNullException("tradableResource");
		}
		Diagnostics.Assert(tradableResource.TradableCategoryDefinition != null);
		List<Tradable> list;
		if (!this.tradablesPerCategory.TryGetValue(tradableResource.TradableCategoryDefinition.Name, out list))
		{
			list = new List<Tradable>();
			this.tradablesPerCategory.Add(tradableResource.TradableCategoryDefinition.Name, list);
		}
		TradableResource tradableResource2 = list.FirstOrDefault((Tradable iterator) => iterator.TradableCategoryDefinition.Name == tradableResource.TradableCategoryDefinition.Name) as TradableResource;
		if (tradableResource2 != null)
		{
			tradableResource2.Quantity += tradableResource.Quantity;
		}
		else
		{
			list.Add(tradableResource);
		}
		this.OnCollectionChange(new TradableCollectionChangeEventArgs(tradableResource.TradableCategoryDefinition.Name, list.Cast<ITradable>()));
		return true;
	}

	ReadOnlyCollection<TradableTransaction> ITradeManagementService.GetPastTransactions()
	{
		return this.transactions.AsReadOnly();
	}

	void ITradeManagementService.NotifyTradableTransactionComplete(TradableTransactionType transactionType, global::Empire empire, ITradable tradable, float quantity, float price)
	{
		if (empire == null)
		{
			return;
		}
		string empty;
		if (tradable != null)
		{
			tradable.TryGetReferenceName(empire, out empty);
		}
		else
		{
			empty = string.Empty;
		}
		if (tradable is TradableHero)
		{
			quantity = 0f;
		}
		TradableTransaction tradableTransaction = new TradableTransaction(transactionType, (uint)empire.Index, (uint)base.Game.Turn, empty, quantity, price);
		this.transactions.Add(tradableTransaction);
		this.OnTransactionComplete(new TradableTransactionCompleteEventArgs(tradableTransaction));
	}

	void ITradeManagementService.RefreshEmpireExclusionBits(Tradable tradable, int bits)
	{
		if (tradable == null)
		{
			throw new ArgumentNullException("tradable");
		}
		tradable.EmpireExclusionBits |= bits;
	}

	void ITradeManagementService.ReplicateUnit(Unit unit)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		if (!this.ownedCollectionOfUnits.ContainsKey(unit.GUID))
		{
			this.ownedCollectionOfUnits.Add(unit.GUID, unit);
		}
	}

	void ITradeManagementService.ReplicateUnitDesign(UnitDesign unitDesign)
	{
		if (unitDesign == null)
		{
			throw new ArgumentNullException("unitDesign");
		}
		Diagnostics.Assert(unitDesign.Barcode != 0UL);
		if (!this.ownedCollectionOfUnitDesigns.ContainsKey(unitDesign.Barcode))
		{
			this.ownedCollectionOfUnitDesigns.Add(unitDesign.Barcode, unitDesign);
		}
	}

	void ITradeManagementService.SetTendency(StaticString tradableCategoryName, float value)
	{
		TradableCategoryTendency tradableCategoryTendency;
		if (!this.tradableCategoryTendencies.TryGetValue(tradableCategoryName, out tradableCategoryTendency))
		{
			tradableCategoryTendency = new TradableCategoryTendency();
			this.tradableCategoryTendencies.Add(tradableCategoryName, tradableCategoryTendency);
		}
		tradableCategoryTendency.Value = value;
	}

	bool ITradeManagementService.TryConsumeTradable(ulong uid, float quantity)
	{
		return ((ITradeManagementService)this).TryConsumeTradableAndAllocateTo(uid, quantity, null, new object[0]);
	}

	bool ITradeManagementService.TryConsumeTradableAndAllocateTo(ulong uid, float quantity, global::Empire empire, params object[] parameters)
	{
		foreach (KeyValuePair<string, List<Tradable>> keyValuePair in this.tradablesPerCategory)
		{
			Tradable tradable = keyValuePair.Value.FirstOrDefault((Tradable predicate) => predicate.UID == uid);
			if (tradable != null)
			{
				if (quantity <= tradable.Quantity)
				{
					tradable.Quantity -= quantity;
					tradable.ReservedQuantity -= quantity;
					if (empire != null)
					{
						tradable.TryAllocateTo(empire, quantity, parameters);
					}
					if (tradable.Quantity <= 0f && !(tradable is ITradableWithStacking))
					{
						keyValuePair.Value.Remove(tradable);
						this.ReleaseTradable(tradable, false);
					}
					this.OnCollectionChange(new TradableCollectionChangeEventArgs(keyValuePair.Key, keyValuePair.Value.Cast<ITradable>()));
					return true;
				}
				Diagnostics.LogError("Attempting to consume more quantity ({0}) of tradable (uid: {1}, name: '{2}', quantity: {3}, reserved: {A}) then available.", new object[]
				{
					quantity,
					tradable.UID,
					tradable.Quantity,
					tradable.ReservedQuantity
				});
				break;
			}
		}
		return false;
	}

	bool ITradeManagementService.TryGetTradableByUID(ulong uid, out ITradable tradable)
	{
		foreach (KeyValuePair<string, List<Tradable>> keyValuePair in this.tradablesPerCategory)
		{
			tradable = keyValuePair.Value.FirstOrDefault((Tradable predicate) => predicate.UID == uid);
			if (tradable != null)
			{
				return true;
			}
		}
		tradable = null;
		return false;
	}

	bool ITradeManagementService.TryGetTradableCategories(out List<TradableCategoryDefinition> tradableCategoryDefinitions)
	{
		tradableCategoryDefinitions = new List<TradableCategoryDefinition>();
		foreach (KeyValuePair<string, List<Tradable>> keyValuePair in this.tradablesPerCategory)
		{
			if (keyValuePair.Value.Count != 0)
			{
				Diagnostics.Assert(keyValuePair.Value[0].TradableCategoryDefinition != null);
				tradableCategoryDefinitions.Add(keyValuePair.Value[0].TradableCategoryDefinition);
			}
		}
		return true;
	}

	bool ITradeManagementService.TryGetTradableCategoryState(StaticString tradableCategoryName, out TradableCategoryTendency tradableCategoryTendency)
	{
		return this.tradableCategoryTendencies.TryGetValue(tradableCategoryName, out tradableCategoryTendency);
	}

	bool ITradeManagementService.TryGetTradableCategoryState(StaticString tradableCategoryName, out TradableCategoryTendency tradableCategoryTendency, out TradableCategoryStockFactor tradableCategoryStockFactor)
	{
		if (!this.tradableCategoryStockFactors.TryGetValue(tradableCategoryName, out tradableCategoryStockFactor))
		{
			tradableCategoryStockFactor = TradableCategoryStockFactor.Zero;
		}
		return this.tradableCategoryTendencies.TryGetValue(tradableCategoryName, out tradableCategoryTendency);
	}

	bool ITradeManagementService.TryGetTradables(StaticString tradableCategoryName, out List<ITradable> tradables)
	{
		tradables = new List<ITradable>();
		List<Tradable> source;
		if (this.tradablesPerCategory.TryGetValue(tradableCategoryName, out source))
		{
			tradables.AddRange(source.Cast<ITradable>());
			return true;
		}
		return false;
	}

	bool ITradeManagementService.TryGetTradablesByCategory(StaticString tradableCategoryCategory, out List<ITradable> tradables)
	{
		tradables = new List<ITradable>();
		foreach (KeyValuePair<string, List<Tradable>> keyValuePair in this.tradablesPerCategory)
		{
			if (keyValuePair.Value.Count != 0)
			{
				TradableCategoryDefinition tradableCategoryDefinition = keyValuePair.Value[0].TradableCategoryDefinition;
				Diagnostics.Assert(tradableCategoryDefinition != null);
				List<Tradable> source;
				if (tradableCategoryDefinition.Category == tradableCategoryCategory && this.tradablesPerCategory.TryGetValue(tradableCategoryDefinition.Name, out source))
				{
					tradables.AddRange(source.Cast<ITradable>());
				}
			}
		}
		return false;
	}

	bool ITradeManagementService.TryRetrieveUnit(GameEntityGUID gameEntityGUID, out Unit unit)
	{
		return this.ownedCollectionOfUnits.TryGetValue(gameEntityGUID, out unit);
	}

	bool ITradeManagementService.TryRetrieveUnitDesign(ulong barcode, out UnitDesign unitDesign)
	{
		return this.ownedCollectionOfUnitDesigns.TryGetValue(barcode, out unitDesign);
	}

	bool ITradeManagementService.TryReserveTradable(ulong uid, float quantity, out ITradable tradable)
	{
		foreach (KeyValuePair<string, List<Tradable>> keyValuePair in this.tradablesPerCategory)
		{
			tradable = keyValuePair.Value.FirstOrDefault((Tradable predicate) => predicate.UID == uid);
			if (tradable != null)
			{
				if (tradable.Quantity - ((Tradable)tradable).ReservedQuantity < quantity)
				{
					break;
				}
				((Tradable)tradable).ReservedQuantity += quantity;
				return true;
			}
		}
		tradable = null;
		return false;
	}

	float ITradeManagementService.UpdateTendency(TradableCategoryDefinition tradableCategoryDefinition, float amount)
	{
		Diagnostics.Assert(tradableCategoryDefinition != null);
		TradableCategoryTendency tradableCategoryTendency;
		if (!this.tradableCategoryTendencies.TryGetValue(tradableCategoryDefinition.Name, out tradableCategoryTendency))
		{
			tradableCategoryTendency = new TradableCategoryTendency();
			this.tradableCategoryTendencies.Add(tradableCategoryDefinition.Name, tradableCategoryTendency);
		}
		return this.UpdateTendency(tradableCategoryDefinition, tradableCategoryTendency, amount, true);
	}

	public void DumpAsText(StringBuilder content, string indent)
	{
		foreach (KeyValuePair<string, List<Tradable>> keyValuePair in this.tradablesPerCategory)
		{
			content.AppendFormat("{0}{1}\r\n", indent, keyValuePair.Key);
			foreach (Tradable tradable in keyValuePair.Value)
			{
				string text = tradable.UID.ToString();
				TradableUnit tradableUnit = tradable as TradableUnit;
				if (tradableUnit != null)
				{
					text = text + "/" + tradableUnit.GameEntityGUID.ToString();
					content.AppendFormat("{0}{1} Price = {2}, Qty = {3}, Level = {5}, EmpireExclusionBits = {4}\r\n", new object[]
					{
						indent + "  ",
						text,
						tradable.Value.ToString("R"),
						tradable.Quantity.ToString("R"),
						tradable.EmpireExclusionBits,
						tradableUnit.Level
					});
				}
				else
				{
					content.AppendFormat("{0}{1} Price = {2}, Qty = {3}, EmpireExclusionBits = {4}\r\n", new object[]
					{
						indent + "  ",
						text,
						tradable.Value.ToString("R"),
						tradable.Quantity.ToString("R"),
						tradable.EmpireExclusionBits
					});
				}
			}
		}
	}

	public byte[] DumpAsBytes()
	{
		MemoryStream memoryStream = new MemoryStream();
		using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
		{
			foreach (KeyValuePair<string, List<Tradable>> keyValuePair in this.tradablesPerCategory)
			{
				binaryWriter.Write(keyValuePair.Key);
				foreach (Tradable tradable in keyValuePair.Value)
				{
					if (tradable is TradableUnit)
					{
						TradableUnit tradableUnit = tradable as TradableUnit;
						binaryWriter.Write(tradableUnit.GameEntityGUID);
						binaryWriter.Write(tradableUnit.Level);
					}
					binaryWriter.Write(tradable.UID);
					binaryWriter.Write(tradable.Value);
					binaryWriter.Write(tradable.Quantity);
					binaryWriter.Write(tradable.EmpireExclusionBits);
				}
			}
		}
		byte[] result = memoryStream.ToArray();
		memoryStream.Close();
		return result;
	}

	public static StaticString[] TradableUnitCategoriesToReplicate { get; private set; } = new StaticString[]
	{
		TradableUnit.ReadOnlyNeutral,
		TradableUnit.ReadOnlyNeutralSeafaring,
		TradableUnit.ReadOnlyHero,
		TradableUnit.ReadOnlyHeroExclusive
	};

	public virtual void OnBeginTurn()
	{
		foreach (KeyValuePair<string, TradableCategoryTendency> keyValuePair in this.tradableCategoryTendencies)
		{
			TradableCategoryTendency value = keyValuePair.Value;
			int num = value.Flags & 1;
			if (num != 0)
			{
				value.Flags &= -2;
				value.Flags &= -3;
			}
		}
	}

	protected virtual void OnCollectionChange(TradableCollectionChangeEventArgs e)
	{
		this.UpdateStockFactor(e.TradableCategory);
		if (this.CollectionChange != null)
		{
			this.CollectionChange(this, e);
		}
	}

	protected virtual void OnTransactionComplete(TradableTransactionCompleteEventArgs e)
	{
		if (this.TransactionComplete != null)
		{
			this.TransactionComplete(this, e);
		}
	}

	private List<Tradable> CollectTradableBoosters(global::Game game)
	{
		if (game == null)
		{
			return null;
		}
		if (game.World == null || game.World.Regions == null)
		{
			Diagnostics.LogError("Game world is null or game world regions is null.");
			return null;
		}
		IGameEntityRepositoryService service = base.Game.Services.GetService<IGameEntityRepositoryService>();
		if (service == null)
		{
			return null;
		}
		List<TradableCategoryDefinition> list = (from definition in this.TradableCategoryDefinitions
		where definition.Category == "TradableBooster"
		select definition).ToList<TradableCategoryDefinition>();
		if (list.Count == 0)
		{
			return null;
		}
		Dictionary<string, float> dictionary = new Dictionary<string, float>();
		for (int i = 0; i < game.World.Regions.Length; i++)
		{
			Region region = game.World.Regions[i];
			if (region == null)
			{
				Diagnostics.LogError("Regions is null (index: {0}).", new object[]
				{
					i
				});
			}
			else if (!region.IsRegionColonized())
			{
				int index = UnityEngine.Random.Range(0, list.Count);
				StaticString name = list[index].Name;
				if (!dictionary.ContainsKey(name))
				{
					dictionary.Add(name, 0f);
				}
				Dictionary<string, float> dictionary3;
				Dictionary<string, float> dictionary2 = dictionary3 = dictionary;
				string key2;
				string key = key2 = name;
				float num = dictionary3[key2];
				dictionary2[key] = num + 1f;
			}
		}
		List<Tradable> list2 = new List<Tradable>();
		foreach (KeyValuePair<string, float> keyValuePair in dictionary)
		{
			string key3 = keyValuePair.Key;
			TradableCategoryDefinition tradableCategoryDefinition;
			if (!this.TradableCategoryDefinitions.TryGetValue(key3, out tradableCategoryDefinition))
			{
				Diagnostics.LogWarning("Unable to retrieve the tradable category definition (name: '{0}').", new object[]
				{
					key3
				});
			}
			else
			{
				float num2 = 1f;
				IDatabase<Amplitude.Unity.Framework.AnimationCurve> database = Databases.GetDatabase<Amplitude.Unity.Framework.AnimationCurve>(false);
				Amplitude.Unity.Framework.AnimationCurve animationCurve;
				if (database != null && database.TryGetValue("TradableBoosterCollectionModifier", out animationCurve))
				{
					num2 = animationCurve.EvaluateWithScaledAxis((float)game.Turn, this.gameSpeedMultiplier, 1f);
				}
				float num3 = keyValuePair.Value * num2 * 0.01f;
				if (num3 > 0f)
				{
					bool flag = UnityEngine.Random.Range(0f, 1f) <= num3;
					if (flag)
					{
						TradableBooster item = new TradableBooster
						{
							EmpireExclusionBits = 0,
							Quantity = 1f,
							GameEntityGUIDs = new GameEntityGUID[]
							{
								service.GenerateGUID()
							},
							BoosterDefinitionName = keyValuePair.Key.Substring(8),
							TradableCategoryDefinition = tradableCategoryDefinition,
							TurnWhenFirstPlacedOnMarket = game.Turn,
							TurnWhenLastExchangedOnMarket = game.Turn,
							TurnWhenLastHitOnMarket = game.Turn,
							Value = 0f
						};
						list2.Add(item);
					}
				}
			}
		}
		return list2;
	}

	private List<Tradable> CollectTradableExclusiveHeroes(global::Game game)
	{
		if (game == null)
		{
			return null;
		}
		IHeroManagementService service = game.Services.GetService<IHeroManagementService>();
		if (service == null)
		{
			return null;
		}
		IGameEntityRepositoryService service2 = game.Services.GetService<IGameEntityRepositoryService>();
		if (service2 == null)
		{
			return null;
		}
		string text = "TradableHeroExclusive";
		TradableCategoryDefinition tradableCategoryDefinition;
		if (!this.TradableCategoryDefinitions.TryGetValue(text, out tradableCategoryDefinition))
		{
			Diagnostics.LogWarning("Unable to retrieve the tradable category definition (name: '{0}').", new object[]
			{
				text
			});
			return null;
		}
		text = "TradableHero";
		TradableCategoryDefinition tradableCategoryDefinition2;
		if (!this.TradableCategoryDefinitions.TryGetValue(text, out tradableCategoryDefinition2))
		{
			Diagnostics.LogWarning("Unable to retrieve the tradable category definition (name: '{0}').", new object[]
			{
				text
			});
			return null;
		}
		List<Tradable> list = new List<Tradable>();
		List<Tradable> list2;
		if (!this.tradablesPerCategory.TryGetValue("TradableHeroExclusive", out list2))
		{
			list2 = new List<Tradable>();
			this.tradablesPerCategory.Add(tradableCategoryDefinition.Name, list2);
			this.tradableCategoryTendencies.Add(tradableCategoryDefinition.Name, new TradableCategoryTendency());
		}
		List<Tradable> list3;
		if (!this.tradablesPerCategory.TryGetValue("TradableHero", out list3))
		{
			list3 = new List<Tradable>();
			this.tradablesPerCategory.Add(tradableCategoryDefinition2.Name, list3);
			this.tradableCategoryTendencies.Add(tradableCategoryDefinition2.Name, new TradableCategoryTendency());
		}
		int num = 0;
		IDatabase<Amplitude.Unity.Framework.AnimationCurve> database = Databases.GetDatabase<Amplitude.Unity.Framework.AnimationCurve>(false);
		Amplitude.Unity.Framework.AnimationCurve animationCurve;
		if (database != null && database.TryGetValue(Marketplace.tradableUnitLevelModifier, out animationCurve))
		{
			float num2 = animationCurve.EvaluateWithScaledAxis((float)game.Turn, this.gameSpeedMultiplier, 1f);
			num = (int)num2;
			num = Math.Max(0, Math.Min(100, num));
		}
		for (int i = 0; i < game.Empires.Length; i++)
		{
			MajorEmpire majorEmpire = game.Empires[i] as MajorEmpire;
			if (majorEmpire == null)
			{
				break;
			}
			int empireExclusionBits = ~(1 << majorEmpire.Index);
			int num3 = (int)Math.Ceiling((double)majorEmpire.GetPropertyValue(SimulationProperties.DurationOfHeroesExclusivity));
			int num4 = (int)Math.Ceiling((double)majorEmpire.GetPropertyValue(SimulationProperties.MaximumNumberOfExclusiveHeroes));
			int num5 = 0;
			List<Tradable> list4 = new List<Tradable>();
			for (int j = 0; j < list2.Count; j++)
			{
				Tradable tradable = list2[j];
				if ((tradable.EmpireExclusionBits & 1 << majorEmpire.Index) == 0)
				{
					int num6 = game.Turn - tradable.TurnWhenFirstPlacedOnMarket;
					if (num6 >= num3)
					{
						list4.Add(tradable);
					}
					else
					{
						num5++;
					}
				}
			}
			while (num5++ < num4)
			{
				GameEntityGUID gameEntityGUID = service2.GenerateGUID();
				Unit unit;
				if (service.TryPick(gameEntityGUID, out unit))
				{
					unit.Level = num;
					service.TryAllocateSkillPoints(unit);
					unit.Refresh(true);
					this.ownedCollectionOfUnits.Add(unit.GUID, unit);
					TradableHero tradableHero = new TradableHero();
					tradableHero.Barcode = unit.UnitDesign.Barcode;
					tradableHero.EmpireExclusionBits = empireExclusionBits;
					tradableHero.GameEntityGUID = unit.GUID;
					tradableHero.LastKnownOrigin = TradableOrigin.UndefinedEmpire;
					tradableHero.Level = (short)num;
					tradableHero.Quantity = 1f;
					tradableHero.TradableCategoryDefinition = tradableCategoryDefinition;
					tradableHero.TurnWhenFirstPlacedOnMarket = base.Game.Turn;
					tradableHero.TurnWhenLastExchangedOnMarket = base.Game.Turn;
					tradableHero.TurnWhenLastHitOnMarket = base.Game.Turn;
					Tradable tradable2 = tradableHero;
					ulong uid;
					this.nextAvailableTradableUID = (uid = this.nextAvailableTradableUID) + 1UL;
					tradable2.UID = uid;
					tradableHero.Value = 0f;
					TradableHero tradableHero2 = tradableHero;
					tradableHero2.Value = TradableUnit.GetValue(unit);
					this.ReserveTradableUnitBarcode(tradableHero2, unit.UnitDesign);
					list.Add(tradableHero2);
				}
			}
			for (int k = 0; k < list4.Count; k++)
			{
				Tradable tradable3 = list4[k];
				tradable3.EmpireExclusionBits = 0;
				tradable3.TradableCategoryDefinition = tradableCategoryDefinition2;
				list2.Remove(tradable3);
				list3.Add(tradable3);
			}
		}
		return list;
	}

	private List<Tradable> CollectTradableNeutralSeafaringUnits(global::Game game)
	{
		if (game == null)
		{
			return null;
		}
		if (game.World == null || game.World.Regions == null)
		{
			Diagnostics.LogError("Game world is null or game world regions is null.");
			return null;
		}
		IGameEntityRepositoryService service = game.Services.GetService<IGameEntityRepositoryService>();
		if (service == null)
		{
			return null;
		}
		IDatabase<UnitDesign> database = Databases.GetDatabase<UnitDesign>(false);
		if (database == null)
		{
			return null;
		}
		string text = TradableUnit.ReadOnlyNeutralSeafaring;
		TradableCategoryDefinition tradableCategoryDefinition;
		if (!this.TradableCategoryDefinitions.TryGetValue(text, out tradableCategoryDefinition))
		{
			Diagnostics.LogWarning("Unable to retrieve the tradable category definition (name: '{0}').", new object[]
			{
				text
			});
			return null;
		}
		NavalEmpire navalEmpire = null;
		int num = 0;
		for (int i = 0; i < game.Empires.Length; i++)
		{
			navalEmpire = (game.Empires[i] as NavalEmpire);
			if (navalEmpire != null)
			{
				PirateCouncil agency = navalEmpire.GetAgency<PirateCouncil>();
				if (agency != null)
				{
					num = agency.Fortresses.Count((Fortress fortress) => fortress.Empire == navalEmpire);
					break;
				}
			}
		}
		if (navalEmpire == null)
		{
			return null;
		}
		if (!(navalEmpire.Faction is NavalFaction))
		{
			return null;
		}
		List<Tradable> list = new List<Tradable>();
		int a = num;
		IDatabase<Amplitude.Unity.Framework.AnimationCurve> database2 = Databases.GetDatabase<Amplitude.Unity.Framework.AnimationCurve>(false);
		if (database2 != null)
		{
			Amplitude.Unity.Framework.AnimationCurve animationCurve;
			if (database2.TryGetValue(Marketplace.tradableSeafaringUnitCollectionModifier, out animationCurve))
			{
				float num2 = animationCurve.EvaluateWithScaledAxis((float)game.Turn, this.gameSpeedMultiplier, 1f);
				a = (int)Math.Ceiling((double)((float)num * num2));
			}
			if (database2.TryGetValue(Marketplace.minimumNumberOfTradableSeafaringUnitsOverTime, out animationCurve))
			{
				float num3 = animationCurve.EvaluateWithScaledAxis((float)game.Turn, this.gameSpeedMultiplier, 1f);
				a = Mathf.Max(a, (int)Math.Round((double)num3));
			}
		}
		int num4 = 0;
		Amplitude.Unity.Framework.AnimationCurve animationCurve2;
		if (database2 != null && database2.TryGetValue(Marketplace.tradableUnitLevelModifier, out animationCurve2))
		{
			float num5 = animationCurve2.EvaluateWithScaledAxis((float)game.Turn, this.gameSpeedMultiplier, 1f);
			num4 = (int)num5;
			num4 = Math.Max(0, Math.Min(100, num4));
		}
		int num6 = 1;
		for (int j = 0; j < base.Game.Empires.Length; j++)
		{
			MajorEmpire majorEmpire = base.Game.Empires[j] as MajorEmpire;
			if (majorEmpire == null)
			{
				break;
			}
			DepartmentOfScience agency2 = majorEmpire.GetAgency<DepartmentOfScience>();
			if (agency2 == null)
			{
				break;
			}
			num6 = Math.Max(num6, agency2.CurrentTechnologyEraNumber);
		}
		num6--;
		IDatabase<Droplist> database3 = Databases.GetDatabase<Droplist>(false);
		Droplist droplist;
		if (database3 == null || !database3.TryGetValue("TradableNeutralSeafaringUnitsCollection", out droplist))
		{
			return null;
		}
		while (a-- > 0)
		{
			Droplist droplist2;
			DroppableString droppableString = droplist.Pick(null, out droplist2, new object[]
			{
				num6
			}) as DroppableString;
			if (droppableString != null && !string.IsNullOrEmpty(droppableString.Value))
			{
				string value = droppableString.Value;
				UnitDesign unitDesign;
				if (!this.ownedCopyOfDatabaseUnitDesigns.TryGetValue(value, out unitDesign))
				{
					if (!database.TryGetValue(value, out unitDesign))
					{
						continue;
					}
					unitDesign = (UnitDesign)unitDesign.Clone();
					this.ownedCopyOfDatabaseUnitDesigns.Add(value, unitDesign);
				}
				GameEntityGUID guid = service.GenerateGUID();
				Unit unit = DepartmentOfDefense.CreateUnitByDesign(guid, unitDesign);
				if (unit != null)
				{
					unit.Level = num4;
					unit.Refresh(true);
					this.ownedCollectionOfUnits.Add(unit.GUID, unit);
					TradableUnit tradableUnit = new TradableUnit();
					tradableUnit.Barcode = unit.UnitDesign.Barcode;
					tradableUnit.EmpireExclusionBits = 0;
					tradableUnit.GameEntityGUID = unit.GUID;
					tradableUnit.LastKnownOrigin = TradableOrigin.World;
					tradableUnit.Level = (short)num4;
					tradableUnit.Quantity = 1f;
					tradableUnit.TradableCategoryDefinition = tradableCategoryDefinition;
					tradableUnit.TurnWhenFirstPlacedOnMarket = base.Game.Turn;
					tradableUnit.TurnWhenLastExchangedOnMarket = base.Game.Turn;
					tradableUnit.TurnWhenLastHitOnMarket = base.Game.Turn;
					Tradable tradable = tradableUnit;
					ulong uid;
					this.nextAvailableTradableUID = (uid = this.nextAvailableTradableUID) + 1UL;
					tradable.UID = uid;
					tradableUnit.Value = 0f;
					TradableUnit tradableUnit2 = tradableUnit;
					tradableUnit2.Value = TradableUnit.GetValue(unit);
					this.ReserveTradableUnitBarcode(tradableUnit2, unitDesign);
					list.Add(tradableUnit2);
				}
			}
		}
		return list;
	}

	private List<Tradable> CollectTradableNeutralUnits(global::Game game)
	{
		if (game == null)
		{
			return null;
		}
		if (game.World == null || game.World.Regions == null)
		{
			Diagnostics.LogError("Game world is null or game world regions is null.");
			return null;
		}
		IGameEntityRepositoryService service = game.Services.GetService<IGameEntityRepositoryService>();
		if (service == null)
		{
			return null;
		}
		IDatabase<UnitDesign> database = Databases.GetDatabase<UnitDesign>(false);
		if (database == null)
		{
			return null;
		}
		string text = TradableUnit.ReadOnlyNeutral;
		TradableCategoryDefinition tradableCategoryDefinition;
		if (!this.TradableCategoryDefinitions.TryGetValue(text, out tradableCategoryDefinition))
		{
			Diagnostics.LogWarning("Unable to retrieve the tradable category definition (name: '{0}').", new object[]
			{
				text
			});
			return null;
		}
		int num = 0;
		List<MinorFaction>[] array = new List<MinorFaction>[2];
		List<int>[] array2 = new List<int>[2];
		for (int i = 0; i < 2; i++)
		{
			array[i] = new List<MinorFaction>();
			array2[i] = new List<int>();
		}
		for (int j = 0; j < game.Empires.Length; j++)
		{
			MinorEmpire minorEmpire = game.Empires[j] as MinorEmpire;
			if (minorEmpire != null)
			{
				if (minorEmpire.Region != null)
				{
					int num2 = 0;
					if (minorEmpire.Region.City != null)
					{
						num2 = 1;
					}
					BarbarianCouncil agency = minorEmpire.GetAgency<BarbarianCouncil>();
					if (agency != null)
					{
						int num3 = 0;
						foreach (Village village in agency.Villages)
						{
							if (!agency.IsVillageDestroyed(village))
							{
								num3++;
							}
						}
						if (num3 > 0)
						{
							MinorFaction minorFaction = minorEmpire.Faction as MinorFaction;
							if (minorFaction != null)
							{
								int num4 = array[num2].IndexOf(minorFaction);
								if (num4 == -1)
								{
									num4 = 0;
									array[num2].Insert(num4, minorFaction);
									array2[num2].Insert(num4, 0);
								}
								List<int> list2;
								List<int> list = list2 = array2[num2];
								int num5;
								int index = num5 = num4;
								num5 = list2[num5];
								list[index] = num5 + num3;
								if (num2 == 0)
								{
									num += num3;
								}
							}
						}
					}
				}
			}
		}
		List<Tradable> list3 = new List<Tradable>();
		int a = num;
		IDatabase<Amplitude.Unity.Framework.AnimationCurve> database2 = Databases.GetDatabase<Amplitude.Unity.Framework.AnimationCurve>(false);
		if (database2 != null)
		{
			Amplitude.Unity.Framework.AnimationCurve animationCurve;
			if (database2.TryGetValue(Marketplace.tradableUnitCollectionModifier, out animationCurve))
			{
				float num6 = animationCurve.EvaluateWithScaledAxis((float)game.Turn, this.gameSpeedMultiplier, 1f);
				a = (int)Math.Ceiling((double)((float)num * num6));
			}
			if (database2.TryGetValue(Marketplace.minimumNumberOfTradableUnitsOverTime, out animationCurve))
			{
				float num7 = animationCurve.EvaluateWithScaledAxis((float)game.Turn, this.gameSpeedMultiplier, 1f);
				a = Mathf.Max(a, (int)Math.Round((double)num7));
			}
		}
		int num8 = 0;
		Amplitude.Unity.Framework.AnimationCurve animationCurve2;
		if (database2 != null && database2.TryGetValue(Marketplace.tradableUnitLevelModifier, out animationCurve2))
		{
			float num9 = animationCurve2.EvaluateWithScaledAxis((float)game.Turn, this.gameSpeedMultiplier, 1f);
			num8 = (int)num9;
			num8 = Math.Max(0, Math.Min(100, num8));
		}
		int num10 = 1;
		for (int k = 0; k < base.Game.Empires.Length; k++)
		{
			MajorEmpire majorEmpire = base.Game.Empires[k] as MajorEmpire;
			if (majorEmpire == null)
			{
				break;
			}
			DepartmentOfScience agency2 = majorEmpire.GetAgency<DepartmentOfScience>();
			if (agency2 == null)
			{
				break;
			}
			num10 = Math.Max(num10, agency2.CurrentTechnologyEraNumber);
		}
		num10--;
		while (a-- > 0)
		{
			int num11 = 0;
			if (array[num11].Count == 0)
			{
				num11 = 1;
			}
			int num12 = UnityEngine.Random.Range(0, array[num11].Count);
			MinorFaction minorFaction2 = array[num11][num12];
			List<int> list5;
			List<int> list4 = list5 = array2[num11];
			int num5;
			int index2 = num5 = num12;
			num5 = list5[num5];
			list4[index2] = num5 - 1;
			if (array2[num11][num12] == 0)
			{
				array[num11].RemoveAt(num12);
				array2[num11].RemoveAt(num12);
			}
			if (minorFaction2.MercenaryUnitDesignReferences != null && minorFaction2.MercenaryUnitDesignReferences.Length != 0)
			{
				int num13 = UnityEngine.Random.Range(0, minorFaction2.MercenaryUnitDesignReferences.Length);
				string x = minorFaction2.MercenaryUnitDesignReferences[num13];
				if (num10 >= 0 && num10 < minorFaction2.MercenaryUnitDesignReferences.Length)
				{
					x = minorFaction2.MercenaryUnitDesignReferences[num10];
				}
				else
				{
					x = minorFaction2.MercenaryUnitDesignReferences[minorFaction2.MercenaryUnitDesignReferences.Length - 1];
				}
				UnitDesign unitDesign;
				if (!this.ownedCopyOfDatabaseUnitDesigns.TryGetValue(x, out unitDesign))
				{
					if (!database.TryGetValue(x, out unitDesign))
					{
						continue;
					}
					unitDesign = (UnitDesign)unitDesign.Clone();
					this.ownedCopyOfDatabaseUnitDesigns.Add(x, unitDesign);
				}
				GameEntityGUID guid = service.GenerateGUID();
				Unit unit = DepartmentOfDefense.CreateUnitByDesign(guid, unitDesign);
				if (unit != null)
				{
					unit.Level = num8;
					unit.Refresh(true);
					this.ownedCollectionOfUnits.Add(unit.GUID, unit);
					TradableUnit tradableUnit = new TradableUnit();
					tradableUnit.Barcode = unit.UnitDesign.Barcode;
					tradableUnit.EmpireExclusionBits = 0;
					tradableUnit.GameEntityGUID = unit.GUID;
					tradableUnit.LastKnownOrigin = TradableOrigin.World;
					tradableUnit.Level = (short)num8;
					tradableUnit.Quantity = 1f;
					tradableUnit.TradableCategoryDefinition = tradableCategoryDefinition;
					tradableUnit.TurnWhenFirstPlacedOnMarket = base.Game.Turn;
					tradableUnit.TurnWhenLastExchangedOnMarket = base.Game.Turn;
					tradableUnit.TurnWhenLastHitOnMarket = base.Game.Turn;
					Tradable tradable = tradableUnit;
					ulong uid;
					this.nextAvailableTradableUID = (uid = this.nextAvailableTradableUID) + 1UL;
					tradable.UID = uid;
					tradableUnit.Value = 0f;
					TradableUnit tradableUnit2 = tradableUnit;
					tradableUnit2.Value = TradableUnit.GetValue(unit);
					this.ReserveTradableUnitBarcode(tradableUnit2, unitDesign);
					list3.Add(tradableUnit2);
				}
			}
		}
		return list3;
	}

	private List<Tradable> CollectTradableResources(global::Game game, params string[] resourceTypes)
	{
		if (game == null)
		{
			return null;
		}
		if (resourceTypes == null || resourceTypes.Length == 0)
		{
			return null;
		}
		if (game.World == null || game.World.Regions == null)
		{
			Diagnostics.LogError("Game world is null or game world regions is null.");
			return null;
		}
		Dictionary<string, float> dictionary = new Dictionary<string, float>();
		for (int i = 0; i < game.World.Regions.Length; i++)
		{
			Region region = game.World.Regions[i];
			if (region == null)
			{
				Diagnostics.LogError("Regions is null (index: {0}).", new object[]
				{
					i
				});
			}
			else if (region.City == null)
			{
				if (region.PointOfInterests != null && region.PointOfInterests.Length != 0)
				{
					for (int j = 0; j < region.PointOfInterests.Length; j++)
					{
						PointOfInterest pointOfInterest = region.PointOfInterests[j];
						string a;
						if (pointOfInterest == null)
						{
							Diagnostics.LogError("Point of interest is null (index: {0}, region index: {1}).", new object[]
							{
								j,
								i
							});
						}
						else if (pointOfInterest.PointOfInterestDefinition.TryGetValue("Type", out a) && a == "ResourceDeposit")
						{
							string value;
							if (pointOfInterest.PointOfInterestDefinition.TryGetValue("ResourceType", out value))
							{
								if (resourceTypes.Contains(value))
								{
									string text;
									if (pointOfInterest.PointOfInterestDefinition.TryGetValue("ResourceName", out text))
									{
										if (!dictionary.ContainsKey(text))
										{
											dictionary.Add(text, 0f);
										}
										Dictionary<string, float> dictionary3;
										Dictionary<string, float> dictionary2 = dictionary3 = dictionary;
										string key2;
										string key = key2 = text;
										float num = dictionary3[key2];
										dictionary2[key] = num + 1f;
									}
								}
							}
						}
					}
				}
			}
		}
		List<Tradable> list = new List<Tradable>();
		foreach (KeyValuePair<string, float> keyValuePair in dictionary)
		{
			string text2 = "TradableResource" + keyValuePair.Key;
			TradableCategoryDefinition tradableCategoryDefinition;
			if (!this.TradableCategoryDefinitions.TryGetValue(text2, out tradableCategoryDefinition))
			{
				Diagnostics.LogWarning("Unable to retrieve the tradable category definition (name: '{0}').", new object[]
				{
					text2
				});
			}
			else
			{
				float num2 = 1f;
				IDatabase<Amplitude.Unity.Framework.AnimationCurve> database = Databases.GetDatabase<Amplitude.Unity.Framework.AnimationCurve>(false);
				if (database != null)
				{
					StaticString key3 = tradableCategoryDefinition.Category + tradableCategoryDefinition.SubCategory + "CollectionModifier";
					Amplitude.Unity.Framework.AnimationCurve animationCurve;
					if (database.TryGetValue(key3, out animationCurve))
					{
						num2 = animationCurve.EvaluateWithScaledAxis((float)game.Turn, this.gameSpeedMultiplier, 1f);
					}
				}
				float num3 = keyValuePair.Value * num2;
				if (num3 > 0f)
				{
					TradableResource item = new TradableResource
					{
						EmpireExclusionBits = 0,
						Quantity = num3,
						ResourceName = keyValuePair.Key,
						TradableCategoryDefinition = tradableCategoryDefinition,
						TurnWhenFirstPlacedOnMarket = game.Turn,
						TurnWhenLastExchangedOnMarket = game.Turn,
						TurnWhenLastHitOnMarket = game.Turn,
						Value = 0f
					};
					list.Add(item);
				}
			}
		}
		return list;
	}

	private List<Tradable> CollectTradableResourcesLuxury(global::Game game)
	{
		return this.CollectTradableResources(game, new string[]
		{
			"Luxury"
		});
	}

	private List<Tradable> CollectTradableResourcesStrategic(global::Game game)
	{
		return this.CollectTradableResources(game, new string[]
		{
			"Strategic"
		});
	}

	private void ReleaseTradable(Tradable tradable, bool disposing)
	{
		Diagnostics.Assert(tradable != null);
		string name = tradable.GetType().Name;
		if (name != null)
		{
			if (Marketplace.<>f__switch$mapD == null)
			{
				Marketplace.<>f__switch$mapD = new Dictionary<string, int>(2)
				{
					{
						"TradableHero",
						0
					},
					{
						"TradableUnit",
						0
					}
				};
			}
			int num;
			if (Marketplace.<>f__switch$mapD.TryGetValue(name, out num))
			{
				if (num == 0)
				{
					this.ReleaseTradableUnit((TradableUnit)tradable, disposing);
				}
			}
		}
	}

	private void ReleaseTradableUnit(TradableUnit tradable, bool disposing)
	{
		Unit unit;
		if (this.ownedCollectionOfUnits.TryGetValue(tradable.GameEntityGUID, out unit))
		{
			this.ownedCollectionOfUnits.Remove(tradable.GameEntityGUID);
			int num = 0;
			foreach (Unit unit2 in this.ownedCollectionOfUnits.Values)
			{
				if (unit2.UnitDesign.Barcode == unit.UnitDesign.Barcode)
				{
					num++;
					break;
				}
			}
			if (num == 0)
			{
				this.ownedCollectionOfUnitDesigns.Remove(unit.UnitDesign.Barcode);
			}
			if (disposing)
			{
				unit.Dispose();
			}
		}
	}

	private void ReserveTradableUnitBarcode(TradableUnit tradable, UnitDesign unitDesign)
	{
		if (tradable.Barcode == 0UL)
		{
			ulong barcode;
			this.nextAvailableBarcode = (barcode = this.nextAvailableBarcode) + 1UL;
			tradable.Barcode = barcode;
			if (unitDesign != null)
			{
				unitDesign.Barcode = tradable.Barcode;
				unitDesign = (UnitDesign)unitDesign.Clone();
				unitDesign.Model = 0u;
				unitDesign.ModelRevision = 0u;
				unitDesign.Barcode = tradable.Barcode;
				this.ownedCollectionOfUnitDesigns.Add(unitDesign.Barcode, unitDesign);
			}
		}
		else if (!this.ownedCollectionOfUnitDesigns.ContainsKey(tradable.Barcode))
		{
			if (tradable.Barcode >= this.nextAvailableBarcode)
			{
				Diagnostics.LogError("Non-legit barcode being used (barcode: {0}, next available: {1}).", new object[]
				{
					tradable.Barcode,
					this.nextAvailableBarcode
				});
				ulong barcode;
				this.nextAvailableBarcode = (barcode = this.nextAvailableBarcode) + 1UL;
				tradable.Barcode = barcode;
			}
			if (unitDesign != null)
			{
				unitDesign.Barcode = tradable.Barcode;
				unitDesign = (UnitDesign)unitDesign.Clone();
				unitDesign.Model = 0u;
				unitDesign.ModelRevision = 0u;
				unitDesign.Barcode = tradable.Barcode;
				this.ownedCollectionOfUnitDesigns.Add(unitDesign.Barcode, unitDesign);
			}
		}
	}

	private void UpdateStockFactor(string tradableCategory)
	{
		if (!string.IsNullOrEmpty(tradableCategory))
		{
			TradableCategoryDefinition tradableCategoryDefinition;
			if (this.TradableCategoryDefinitions.TryGetValue(tradableCategory, out tradableCategoryDefinition))
			{
				this.UpdateStockFactor(tradableCategoryDefinition);
			}
		}
		else
		{
			foreach (KeyValuePair<string, List<Tradable>> keyValuePair in this.tradablesPerCategory)
			{
				TradableCategoryDefinition tradableCategoryDefinition;
				if (this.TradableCategoryDefinitions.TryGetValue(keyValuePair.Key, out tradableCategoryDefinition))
				{
					this.UpdateStockFactor(tradableCategoryDefinition);
				}
			}
		}
	}

	private void UpdateStockFactor(TradableCategoryDefinition tradableCategoryDefinition)
	{
		if (tradableCategoryDefinition == null)
		{
			return;
		}
		if (tradableCategoryDefinition.SensitivityToStockFactor <= 0f)
		{
			return;
		}
		if ((float)tradableCategoryDefinition.MaximumStock < 1f)
		{
			Diagnostics.LogWarning("Maximum stock value should be >= 1 for tradable category (name: '{0}', value: {1}).", new object[]
			{
				tradableCategoryDefinition.Name,
				tradableCategoryDefinition.MaximumStock
			});
			return;
		}
		TradableCategoryStockFactor tradableCategoryStockFactor;
		if (!this.tradableCategoryStockFactors.TryGetValue(tradableCategoryDefinition.Name, out tradableCategoryStockFactor))
		{
			tradableCategoryStockFactor = new TradableCategoryStockFactor();
			this.tradableCategoryStockFactors.Add(tradableCategoryDefinition.Name, tradableCategoryStockFactor);
		}
		float num = 0f;
		List<Tradable> list;
		if (this.tradablesPerCategory.TryGetValue(tradableCategoryDefinition.Name, out list))
		{
			for (int i = 0; i < list.Count; i++)
			{
				num += list[i].Quantity;
			}
		}
		float num2 = Math.Max(0f, Math.Min(1f, tradableCategoryDefinition.ReferenceStockRatio));
		tradableCategoryStockFactor.Value = ((float)tradableCategoryDefinition.MaximumStock * num2 - num) / (float)tradableCategoryDefinition.MaximumStock;
	}

	private float UpdateTendency(TradableCategoryDefinition tradableCategoryDefinition, TradableCategoryTendency tradableCategoryTendency, float amount, bool update)
	{
		Diagnostics.Assert(tradableCategoryDefinition != null);
		Diagnostics.Assert(tradableCategoryTendency != null);
		float num = tradableCategoryTendency + amount;
		float referencePrice = Tradable.GetReferencePrice(tradableCategoryDefinition, 0f);
		float num2 = 0f;
		TradableCategoryStockFactor stockFactor;
		if (this.tradableCategoryStockFactors.TryGetValue(tradableCategoryDefinition.Name, out stockFactor))
		{
			num2 = stockFactor;
		}
		float num3 = referencePrice * (1f + num + num2);
		float num4 = referencePrice * Tradable.MinimumPriceMultiplier;
		if (num3 < num4)
		{
			num = num4 / referencePrice - 1f;
		}
		else
		{
			float num5 = referencePrice * Tradable.MaximumPriceMultiplier;
			if (num3 > num5)
			{
				num = num5 / referencePrice - 1f;
			}
		}
		if (update)
		{
			tradableCategoryTendency.Flags |= 1;
		}
		if (tradableCategoryTendency.Value != num)
		{
			tradableCategoryTendency.Flags |= 2;
		}
		tradableCategoryTendency.Value = num;
		return num;
	}

	public virtual void ReadXml(XmlReader reader)
	{
		this.TurnWhenLastCollected = reader.GetAttribute<int>("TurnWhenLastCollected");
		reader.ReadStartElement();
		this.tradablesPerCategory.Clear();
		int attribute = reader.GetAttribute<int>("Count");
		this.nextAvailableTradableUID = reader.GetAttribute<ulong>("NextAvailableTradableUID");
		this.nextAvailableBarcode = reader.GetAttribute<ulong>("NextAvailableBarcode");
		reader.ReadStartElement("Tradables");
		for (int i = 0; i < attribute; i++)
		{
			string attribute2 = reader.GetAttribute("TradableCategoryDefinitionName");
			int attribute3 = reader.GetAttribute<int>("Count");
			List<Tradable> list = new List<Tradable>();
			reader.ReadStartElement("Collection");
			for (int j = 0; j < attribute3; j++)
			{
				string name = reader.Reader.Name;
				Type type = null;
				try
				{
					string attribute4 = reader.GetAttribute("AssemblyQualifiedName");
					type = Type.GetType(attribute4);
				}
				catch
				{
				}
				if (type != null)
				{
					Amplitude.Xml.Serialization.IXmlSerializable xmlSerializable = Activator.CreateInstance(type, true) as Amplitude.Xml.Serialization.IXmlSerializable;
					if (xmlSerializable != null)
					{
						xmlSerializable.ReadXml(reader);
						reader.ReadEndElement(name);
					}
					else
					{
						reader.Skip(name);
					}
					Tradable tradable = xmlSerializable as Tradable;
					if (tradable != null && tradable.TradableCategoryDefinition != null)
					{
						list.Add(tradable);
					}
				}
				else
				{
					reader.Skip(name);
				}
			}
			reader.ReadEndElement("Collection");
			this.tradablesPerCategory.Add(attribute2, list);
		}
		reader.ReadEndElement("Tradables");
		reader.ReadStartElement("References");
		int attribute5 = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("UnitDesigns");
		if (attribute5 > 0)
		{
			ISerializationService service = Services.GetService<ISerializationService>();
			XmlSerializer xmlSerializer = service.GetXmlSerializer<UnitDesign>(new Type[]
			{
				typeof(UnitProfile)
			});
			for (int k = 0; k < attribute5; k++)
			{
				UnitDesign unitDesign = xmlSerializer.Deserialize(reader.Reader) as UnitDesign;
				if (unitDesign != null)
				{
					this.ownedCollectionOfUnitDesigns.Add(unitDesign.Barcode, unitDesign);
				}
			}
		}
		reader.ReadEndElement("UnitDesigns");
		int attribute6 = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("Units");
		for (int l = 0; l < attribute6; l++)
		{
			ulong attribute7 = reader.GetAttribute<ulong>("GUID");
			ulong attribute8 = reader.GetAttribute<ulong>("UnitDesignBarcode");
			Unit unit = new Unit(attribute7);
			Amplitude.Xml.Serialization.IXmlSerializable xmlSerializable2 = unit;
			reader.ReadElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>(ref xmlSerializable2);
			if (unit != null)
			{
				UnitDesign unitDesign2 = null;
				if (this.ownedCollectionOfUnitDesigns.TryGetValue(attribute8, out unitDesign2))
				{
					unit.UnitDesign = unitDesign2;
					this.ownedCollectionOfUnits.Add(attribute7, unit);
				}
			}
		}
		reader.ReadEndElement("Units");
		reader.ReadEndElement("References");
		this.tradableCategoryTendencies.Clear();
		int attribute9 = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("Tendencies");
		for (int m = 0; m < attribute9; m++)
		{
			TradableCategoryTendency tradableCategoryTendency = new TradableCategoryTendency();
			string attribute10 = reader.GetAttribute("Name");
			tradableCategoryTendency.Value = reader.GetAttribute<float>("Value");
			tradableCategoryTendency.Flags = reader.GetAttribute<int>("Flags");
			this.tradableCategoryTendencies.Add(attribute10, tradableCategoryTendency);
			reader.Skip("Tendency");
		}
		reader.ReadEndElement("Tendencies");
		int attribute11 = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("Transactions");
		for (int n = 0; n < attribute11; n++)
		{
			TradableTransaction tradableTransaction = new TradableTransaction();
			reader.ReadElementSerializable<TradableTransaction>(ref tradableTransaction);
			if (tradableTransaction != null)
			{
				this.transactions.Add(tradableTransaction);
			}
		}
		reader.ReadEndElement("Transactions");
		this.OnCollectionChange(new TradableCollectionChangeEventArgs(null, null));
	}

	public virtual void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString("AssemblyQualifiedName", base.GetType().AssemblyQualifiedName);
		writer.WriteAttributeString<int>("TurnWhenLastCollected", this.TurnWhenLastCollected);
		writer.WriteStartElement("Tradables");
		writer.WriteAttributeString<int>("Count", this.tradablesPerCategory.Count);
		writer.WriteAttributeString<ulong>("NextAvailableTradableUID", this.nextAvailableTradableUID);
		writer.WriteAttributeString<ulong>("NextAvailableBarcode", this.nextAvailableBarcode);
		foreach (KeyValuePair<string, List<Tradable>> keyValuePair in this.tradablesPerCategory)
		{
			writer.WriteStartElement("Collection");
			writer.WriteAttributeString("TradableCategoryDefinitionName", keyValuePair.Key);
			writer.WriteAttributeString<int>("Count", keyValuePair.Value.Count);
			for (int i = 0; i < keyValuePair.Value.Count; i++)
			{
				Amplitude.Xml.Serialization.IXmlSerializable xmlSerializable = keyValuePair.Value[i];
				writer.WriteElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>(ref xmlSerializable);
			}
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
		writer.WriteStartElement("References");
		writer.WriteStartElement("UnitDesigns");
		writer.WriteAttributeString<int>("Count", this.ownedCollectionOfUnitDesigns.Count);
		if (this.ownedCollectionOfUnitDesigns.Count > 0)
		{
			ISerializationService service = Services.GetService<ISerializationService>();
			XmlSerializer xmlSerializer = service.GetXmlSerializer<UnitDesign>(new Type[]
			{
				typeof(UnitProfile)
			});
			foreach (KeyValuePair<ulong, UnitDesign> keyValuePair2 in this.ownedCollectionOfUnitDesigns)
			{
				UnitDesign value = keyValuePair2.Value;
				value.XmlSerializableUnitEquipmentSet = (UnitEquipmentSet)value.UnitEquipmentSet.Clone();
				xmlSerializer.Serialize(writer.Writer, value);
			}
		}
		writer.WriteEndElement();
		writer.WriteStartElement("Units");
		writer.WriteAttributeString<int>("Count", this.ownedCollectionOfUnits.Count);
		foreach (Unit unit in this.ownedCollectionOfUnits.Values)
		{
			Amplitude.Xml.Serialization.IXmlSerializable xmlSerializable2 = unit;
			writer.WriteElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>(ref xmlSerializable2);
		}
		writer.WriteEndElement();
		writer.WriteEndElement();
		writer.WriteStartElement("Tendencies");
		writer.WriteAttributeString<int>("Count", this.tradableCategoryTendencies.Count);
		foreach (KeyValuePair<string, TradableCategoryTendency> keyValuePair3 in this.tradableCategoryTendencies)
		{
			writer.WriteStartElement("Tendency");
			writer.WriteAttributeString("Name", keyValuePair3.Key);
			writer.WriteAttributeString<float>("Value", keyValuePair3.Value);
			if (keyValuePair3.Value.Flags != 0)
			{
				writer.WriteAttributeString<int>("Flags", keyValuePair3.Value.Flags);
			}
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
		writer.WriteStartElement("Transactions");
		writer.WriteAttributeString<int>("Count", this.transactions.Count);
		for (int j = 0; j < this.transactions.Count; j++)
		{
			Amplitude.Xml.Serialization.IXmlSerializable xmlSerializable3 = this.transactions[j];
			writer.WriteElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>(ref xmlSerializable3);
		}
		writer.WriteEndElement();
	}

	public int TurnWhenLastCollected { get; private set; }

	private IDatabase<TradableCategoryDefinition> TradableCategoryDefinitions { get; set; }

	private ITradeManagementService TradeManagementService
	{
		get
		{
			return this;
		}
	}

	public override IEnumerator BindServices(IServiceContainer serviceContainer)
	{
		yield return base.BindServices(serviceContainer);
		serviceContainer.AddService<ITradeManagementService>(this);
		this.TradableCategoryDefinitions = Databases.GetDatabase<TradableCategoryDefinition>(false);
		this.collectors.Add("TradableResourcesLuxury", new Marketplace.Collector(this.CollectTradableResourcesLuxury));
		this.collectors.Add("TradableResourcesStrategic", new Marketplace.Collector(this.CollectTradableResourcesStrategic));
		this.collectors.Add("TradableUnitNeutral", new Marketplace.Collector(this.CollectTradableNeutralUnits));
		this.collectors.Add("TradableHeroExclusive", new Marketplace.Collector(this.CollectTradableExclusiveHeroes));
		this.collectors.Add("TradableBooster", new Marketplace.Collector(this.CollectTradableBoosters));
		IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
		if (downloadableContentService != null && downloadableContentService.IsShared(DownloadableContent16.ReadOnlyName))
		{
			this.collectors.Add("TradableUnitNeutralSeafaring", new Marketplace.Collector(this.CollectTradableNeutralSeafaringUnits));
		}
		Tradable.BuyoutMultiplier = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<float>("Gameplay/Ancillaries/Marketplace/Tradable/BuyoutMultiplier", 1.196f);
		Tradable.DepreciationMultiplier = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<float>("Gameplay/Ancillaries/Marketplace/Tradable/DepreciationMultiplier", -0.05f);
		Tradable.InflationMultiplier = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<float>("Gameplay/Ancillaries/Marketplace/Tradable/InflationMultiplier", 0.01f);
		Tradable.PositiveTendencyMultiplier = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<float>("Gameplay/Ancillaries/Marketplace/Tradable/PositiveTendencyMultiplier", 0.1f);
		Tradable.MaximumPriceMultiplier = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<float>("Gameplay/Ancillaries/Marketplace/Tradable/MaximumPriceMultiplier", 2f);
		Tradable.MinimumPriceMultiplier = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<float>("Gameplay/Ancillaries/Marketplace/Tradable/MinimumPriceMultiplier", 0.5f);
		Tradable.NegativeTendencyMultiplier = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<float>("Gameplay/Ancillaries/Marketplace/Tradable/NegativeTendencyMultiplier", -0.1f);
		Tradable.SelloutMultiplier = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<float>("Gameplay/Ancillaries/Marketplace/Tradable/SelloutMultiplier", 0.79f);
		Tradable.UnitLevelMultiplier = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<float>("Gameplay/Ancillaries/Marketplace/Tradable/UnitLevelMultiplier", 0.1f);
		yield break;
	}

	public override IEnumerator LoadGame(global::Game game)
	{
		yield return base.LoadGame(game);
		for (int index = game.Empires.Length - 1; index >= 0; index--)
		{
			LesserEmpire lesserEmpire = game.Empires[index] as LesserEmpire;
			if (lesserEmpire != null)
			{
				this.gameSpeedMultiplier = lesserEmpire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier);
				float gameSpeedMultiplierSensitivity = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<float>("Gameplay/Ancillaries/Marketplace/GameSpeedMultiplierSensitivity", 0f);
				if (gameSpeedMultiplierSensitivity > 0f)
				{
					if (this.gameSpeedMultiplier > 1f)
					{
						this.gameSpeedMultiplier *= gameSpeedMultiplierSensitivity;
					}
					else if (this.gameSpeedMultiplier < 1f)
					{
						this.gameSpeedMultiplier /= gameSpeedMultiplierSensitivity;
					}
				}
				break;
			}
		}
		yield break;
	}

	protected override void Releasing()
	{
		base.Releasing();
		foreach (KeyValuePair<string, List<Tradable>> keyValuePair in this.tradablesPerCategory)
		{
			for (int i = 0; i < keyValuePair.Value.Count; i++)
			{
				this.ReleaseTradable(keyValuePair.Value[i], true);
			}
		}
		this.tradablesPerCategory.Clear();
		this.tradableCategoryTendencies.Clear();
		foreach (KeyValuePair<ulong, Unit> keyValuePair2 in this.ownedCollectionOfUnits)
		{
			keyValuePair2.Value.Dispose();
		}
		this.ownedCollectionOfUnits.Clear();
		foreach (KeyValuePair<ulong, UnitDesign> keyValuePair3 in this.ownedCollectionOfUnitDesigns)
		{
			if (keyValuePair3.Value.Context != null)
			{
				keyValuePair3.Value.Context.Dispose();
			}
		}
		this.ownedCollectionOfUnitDesigns.Clear();
		this.collectors.Clear();
		this.transactions.Clear();
	}

	private static StaticString tradableSeafaringUnitCollectionModifier = new StaticString("TradableSeafaringUnitCollectionModifier");

	private static StaticString tradableUnitCollectionModifier = new StaticString("TradableUnitCollectionModifier");

	private static StaticString tradableUnitLevelModifier = new StaticString("TradableUnitLevelModifier");

	private static StaticString minimumNumberOfTradableUnitsOverTime = new StaticString("MinimumNumberOfTradableUnitsOverTime");

	private static StaticString minimumNumberOfTradableSeafaringUnitsOverTime = new StaticString("MinimumNumberOfTradableSeafaringUnitsOverTime");

	private ulong nextAvailableTradableUID = 1UL;

	private ulong nextAvailableBarcode = 1UL;

	private Dictionary<string, Marketplace.Collector> collectors = new Dictionary<string, Marketplace.Collector>();

	private Dictionary<string, List<Tradable>> tradablesPerCategory = new Dictionary<string, List<Tradable>>();

	private Dictionary<string, TradableCategoryTendency> tradableCategoryTendencies = new Dictionary<string, TradableCategoryTendency>();

	private Dictionary<string, TradableCategoryStockFactor> tradableCategoryStockFactors = new Dictionary<string, TradableCategoryStockFactor>();

	private Dictionary<ulong, Unit> ownedCollectionOfUnits = new Dictionary<ulong, Unit>();

	private Dictionary<ulong, UnitDesign> ownedCollectionOfUnitDesigns = new Dictionary<ulong, UnitDesign>();

	private Dictionary<StaticString, UnitDesign> ownedCopyOfDatabaseUnitDesigns = new Dictionary<StaticString, UnitDesign>();

	private float gameSpeedMultiplier;

	private List<TradableTransaction> transactions = new List<TradableTransaction>();

	private delegate List<Tradable> Collector(global::Game game);

	[CompilerGenerated]
	private sealed class TryReserveTradable>c__AnonStorey8C2
	{
		internal bool <>m__316(Tradable predicate)
		{
			return predicate.UID == this.uid;
		}

		internal ulong uid;
	}

	[CompilerGenerated]
	private sealed class TryGetTradableByUID>c__AnonStorey8C3
	{
		internal bool <>m__317(Tradable predicate)
		{
			return predicate.UID == this.uid;
		}

		internal ulong uid;
	}

	[CompilerGenerated]
	private sealed class TryConsumeTradableAndAllocateTo>c__AnonStorey8C4
	{
		internal bool <>m__318(Tradable predicate)
		{
			return predicate.UID == this.uid;
		}

		internal ulong uid;
	}

	[CompilerGenerated]
	private sealed class CollectTradableResource>c__AnonStorey8C5
	{
		internal bool <>m__319(Tradable iterator)
		{
			return iterator.TradableCategoryDefinition.Name == this.tradableResource.TradableCategoryDefinition.Name;
		}

		internal TradableResource tradableResource;
	}

	[CompilerGenerated]
	private sealed class CollectTradableBooster>c__AnonStorey8C6
	{
		internal bool <>m__31A(TradableBooster iterator)
		{
			return iterator.BoosterDefinitionName == this.tradableBooster.BoosterDefinitionName;
		}

		internal TradableBooster tradableBooster;
	}
}
