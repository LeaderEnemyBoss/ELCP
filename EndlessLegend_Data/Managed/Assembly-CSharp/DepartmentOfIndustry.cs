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
using Amplitude.Unity.Debug;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Game.Orders;
using Amplitude.Unity.Session;
using Amplitude.Unity.Simulation;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

[OrderProcessor(typeof(OrderBuyoutConstruction), "BuyoutConstruction")]
[OrderProcessor(typeof(OrderCancelConstruction), "CancelConstruction")]
[OrderProcessor(typeof(OrderQueueConstruction), "QueueConstruction")]
[OrderProcessor(typeof(OrderMoveConstruction), "MoveConstruction")]
public class DepartmentOfIndustry : Agency, Amplitude.Xml.Serialization.IXmlSerializable, IConstructibleElementDatabase
{
	public DepartmentOfIndustry(global::Empire empire) : base(empire)
	{
	}

	public event DepartmentOfIndustry.ConstructionChangeEventHandler OnConstructionChange;

	DepartmentOfIndustry.ConstructibleElement[] IConstructibleElementDatabase.GetAvailableConstructibleElements(params StaticString[] categories)
	{
		if (categories == null || categories.Length == 0)
		{
			return this.constructibleElementDatabase.GetValues();
		}
		return (from contructibleElement in ((IConstructibleElementDatabase)this).GetAvailableConstructibleElementsAsEnumerable(new StaticString[0])
		where categories.Contains(contructibleElement.Category)
		select contructibleElement).ToArray<DepartmentOfIndustry.ConstructibleElement>();
	}

	void IConstructibleElementDatabase.FillAvailableConstructibleElements(ref List<DepartmentOfIndustry.ConstructibleElement> list, params StaticString[] categories)
	{
		if (categories == null || categories.Length == 0)
		{
			DepartmentOfIndustry.ConstructibleElement[] collection = ((IConstructibleElementDatabase)this).GetAvailableConstructibleElementsAsEnumerable(new StaticString[0]).ToArray<DepartmentOfIndustry.ConstructibleElement>();
			list.AddRange(collection);
		}
		else
		{
			DepartmentOfIndustry.ConstructibleElement[] collection2 = (from contructibleElement in ((IConstructibleElementDatabase)this).GetAvailableConstructibleElementsAsEnumerable(new StaticString[0])
			where categories.Contains(contructibleElement.Category)
			select contructibleElement).ToArray<DepartmentOfIndustry.ConstructibleElement>();
			list.AddRange(collection2);
		}
	}

	void IConstructibleElementDatabase.GetAvailableConstructibleElements(StaticString[] categories, out DepartmentOfIndustry.ConstructibleElement[][] collections)
	{
		collections = null;
		if (categories == null || categories.Length == 0)
		{
			collections = new DepartmentOfIndustry.ConstructibleElement[1][];
			collections[0] = ((IConstructibleElementDatabase)this).GetAvailableConstructibleElementsAsEnumerable(new StaticString[0]).ToArray<DepartmentOfIndustry.ConstructibleElement>();
			return;
		}
		collections = new DepartmentOfIndustry.ConstructibleElement[categories.Length][];
		for (int i = 0; i < categories.Length; i++)
		{
			StaticString staticString = categories[i];
			collections[i] = ((IConstructibleElementDatabase)this).GetAvailableConstructibleElementsAsEnumerable(new StaticString[]
			{
				staticString
			}).ToArray<DepartmentOfIndustry.ConstructibleElement>();
		}
	}

	IEnumerable<DepartmentOfIndustry.ConstructibleElement> IConstructibleElementDatabase.GetAvailableConstructibleElementsAsEnumerable(params StaticString[] categories)
	{
		foreach (DepartmentOfIndustry.ConstructibleElement constructibleElement in this.constructibleElementDatabase.GetValues())
		{
			if (categories == null || categories.Length <= 0 || categories.Contains(constructibleElement.Category))
			{
				yield return constructibleElement;
			}
		}
		if (this.UnitDesignDatabase != null && (categories == null || categories.Length == 0 || categories.Contains(new StaticString(UnitDesign.ReadOnlyCategory))))
		{
			foreach (UnitDesign unitDesign in this.UnitDesignDatabase.GetUserDefinedUnitDesignsAsEnumerable())
			{
				yield return unitDesign;
			}
		}
		yield break;
	}

	bool IConstructibleElementDatabase.TryGetValue(StaticString constructibleElementName, out DepartmentOfIndustry.ConstructibleElement constructibleElement)
	{
		if (this.constructibleElementDatabase == null)
		{
			this.constructibleElementDatabase = Databases.GetDatabase<DepartmentOfIndustry.ConstructibleElement>(true);
			Diagnostics.Assert(this.constructibleElementDatabase != null);
		}
		if (this.constructibleElementDatabase.TryGetValue(constructibleElementName, out constructibleElement))
		{
			return true;
		}
		UnitDesign unitDesign;
		if (this.UnitDesignDatabase != null && this.UnitDesignDatabase.TryGetValue(constructibleElementName, out unitDesign, true))
		{
			constructibleElement = unitDesign;
			return true;
		}
		constructibleElement = null;
		return false;
	}

	public static int ComputeTurn(Construction construction, DepartmentOfTheTreasury departmentOfTheTreasury, SimulationObjectWrapper context)
	{
		DepartmentOfIndustry.turnByResource.Clear();
		int num = 0;
		if (construction.ConstructibleElement.Costs != null)
		{
			for (int i = 0; i < construction.ConstructibleElement.Costs.Length; i++)
			{
				if (!construction.ConstructibleElement.Costs[i].Instant && !construction.ConstructibleElement.Costs[i].InstantOnCompletion)
				{
					float num2 = DepartmentOfTheTreasury.GetProductionCostWithBonus(context, construction.ConstructibleElement, construction.ConstructibleElement.Costs[i], false);
					num2 -= construction.CurrentConstructionStock[i].Stock;
					float num3;
					if (!departmentOfTheTreasury.TryGetResourceStockValue(context.SimulationObject, construction.ConstructibleElement.Costs[i].ResourceName, out num3, true))
					{
						num3 = 0f;
					}
					float num4;
					if (!departmentOfTheTreasury.TryGetNetResourceValue(context.SimulationObject, construction.ConstructibleElement.Costs[i].ResourceName, out num4, true))
					{
						num4 = 0f;
					}
					if (num2 > num3)
					{
						if (num4 <= 0f)
						{
							return int.MaxValue;
						}
						int num5 = Mathf.CeilToInt((num2 - num3) / num4);
						if (DepartmentOfIndustry.turnByResource.ContainsKey(construction.ConstructibleElement.Costs[i].ResourceName))
						{
							Dictionary<StaticString, int> dictionary2;
							Dictionary<StaticString, int> dictionary = dictionary2 = DepartmentOfIndustry.turnByResource;
							StaticString resourceName;
							StaticString key = resourceName = construction.ConstructibleElement.Costs[i].ResourceName;
							int num6 = dictionary2[resourceName];
							dictionary[key] = num6 + num5;
						}
						else
						{
							DepartmentOfIndustry.turnByResource.Add(construction.ConstructibleElement.Costs[i].ResourceName, num5);
						}
						if (DepartmentOfIndustry.turnByResource[construction.ConstructibleElement.Costs[i].ResourceName] > num)
						{
							num = DepartmentOfIndustry.turnByResource[construction.ConstructibleElement.Costs[i].ResourceName];
						}
					}
				}
			}
		}
		return num;
	}

	public IConstructibleElementDatabase ConstructibleElementDatabase
	{
		get
		{
			return this;
		}
	}

	private IUnitDesignDatabase UnitDesignDatabase { get; set; }

	public override void DumpAsText(StringBuilder content, string indent = "")
	{
		base.DumpAsText(content, indent);
		foreach (KeyValuePair<GameEntityGUID, ConstructionQueue> keyValuePair in from kvp in this.constructionQueues
		orderby kvp.Key
		select kvp)
		{
			content.AppendFormat("{0}City#{1}\r\n", indent, keyValuePair.Key);
			for (int i = 0; i < keyValuePair.Value.Length; i++)
			{
				content.AppendFormat("{0}{1} {2}\r\n", indent + "  ", i, keyValuePair.Value.PeekAt(i).ConstructibleElement.Name);
			}
		}
	}

	public override byte[] DumpAsBytes()
	{
		MemoryStream memoryStream = new MemoryStream();
		using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
		{
			binaryWriter.Write(base.DumpAsBytes());
			foreach (KeyValuePair<GameEntityGUID, ConstructionQueue> keyValuePair in from kvp in this.constructionQueues
			orderby kvp.Key
			select kvp)
			{
				binaryWriter.Write(keyValuePair.Key);
				for (int i = 0; i < keyValuePair.Value.Length; i++)
				{
					binaryWriter.Write(keyValuePair.Value.PeekAt(i).ConstructibleElement.Name);
				}
			}
		}
		byte[] result = memoryStream.ToArray();
		memoryStream.Close();
		return result;
	}

	public override void ReadXml(XmlReader reader)
	{
		base.ReadXml(reader);
		int attribute = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("Queues");
		this.constructionQueues.Clear();
		for (int i = 0; i < attribute; i++)
		{
			ulong attribute2 = reader.GetAttribute<ulong>("GUID");
			ConstructionQueue constructionQueue = new ConstructionQueue();
			this.constructionQueues.Add(attribute2, constructionQueue);
			reader.ReadStartElement("Queue");
			reader.ReadElementSerializable<ConstructionQueue>("Constructions", ref constructionQueue);
			for (int j = constructionQueue.Length - 1; j >= 0; j--)
			{
				Construction construction = constructionQueue.PeekAt(j);
				DepartmentOfIndustry.ConstructibleElement constructibleElement;
				if (this.ConstructibleElementDatabase.TryGetValue(construction.ConstructibleElementName, out constructibleElement))
				{
					construction.ConstructibleElement = constructibleElement;
				}
			}
			reader.ReadEndElement("Queue");
		}
		reader.ReadEndElement("Queues");
		if (reader.IsStartElement("ForbiddenConstructiblesUniqueInWorld"))
		{
			attribute = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("ForbiddenConstructiblesUniqueInWorld");
			if (reader.IsStartElement("Name"))
			{
				for (int k = 0; k < attribute; k++)
				{
					StaticString key = reader.ReadElementString("Name");
					int value = -1;
					if (reader.IsStartElement("EmpireBuilderIndex"))
					{
						value = reader.ReadElementString<int>("EmpireBuilderIndex");
					}
					if (!this.ForbiddenConstructiblesUniqueInWorld.ContainsKey(key))
					{
						this.ForbiddenConstructiblesUniqueInWorld.Add(key, value);
					}
				}
			}
			else
			{
				reader.Skip();
			}
			reader.ReadEndElement("ForbiddenConstructiblesUniqueInWorld");
		}
		if (reader.IsStartElement("ConstructiblesWaitingToBeCanceled"))
		{
			attribute = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("ConstructiblesWaitingToBeCanceled");
			for (int l = 0; l < attribute; l++)
			{
				GameEntityGUID queueGUID = reader.GetAttribute<ulong>("QueueGUID");
				GameEntityGUID constructionGUID = reader.GetAttribute<ulong>("ConstructionGUID");
				int attribute3 = reader.GetAttribute<int>("Refund");
				reader.ReadStartElement("ConstructibleIds");
				reader.ReadEndElement("ConstructibleIds");
				this.ConstructiblesWaitingToBeCanceled.Add(new DepartmentOfIndustry.ConstructionToCancel
				{
					QueueGUID = queueGUID,
					ConstructionGUID = constructionGUID,
					DustRefund = (float)attribute3
				});
			}
			reader.ReadEndElement("ConstructiblesWaitingToBeCanceled");
		}
		if (reader.IsStartElement("NotifyColossus"))
		{
			this.NotifyFirstColossus = reader.ReadElementString<bool>("NotifyColossus");
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		base.WriteXml(writer);
		writer.WriteStartElement("Queues");
		writer.WriteAttributeString<int>("Count", this.constructionQueues.Count);
		foreach (KeyValuePair<GameEntityGUID, ConstructionQueue> keyValuePair in this.constructionQueues)
		{
			writer.WriteStartElement("Queue");
			writer.WriteAttributeString<ulong>("GUID", keyValuePair.Key);
			Amplitude.Xml.Serialization.IXmlSerializable value = keyValuePair.Value;
			writer.WriteElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>("Constructions", ref value);
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
		writer.WriteStartElement("ForbiddenConstructiblesUniqueInWorld");
		writer.WriteAttributeString<int>("Count", this.ForbiddenConstructiblesUniqueInWorld.Count);
		foreach (KeyValuePair<StaticString, int> keyValuePair2 in this.ForbiddenConstructiblesUniqueInWorld)
		{
			writer.WriteElementString<StaticString>("Name", keyValuePair2.Key);
			writer.WriteElementString<int>("EmpireBuilderIndex", keyValuePair2.Value);
		}
		writer.WriteEndElement();
		writer.WriteStartElement("ConstructiblesWaitingToBeCanceled");
		writer.WriteAttributeString<int>("Count", this.ConstructiblesWaitingToBeCanceled.Count);
		for (int i = 0; i < this.ConstructiblesWaitingToBeCanceled.Count; i++)
		{
			writer.WriteStartElement("ConstructibleIds");
			writer.WriteAttributeString<ulong>("QueueGUID", this.ConstructiblesWaitingToBeCanceled[i].QueueGUID);
			writer.WriteAttributeString<ulong>("ConstructionGUID", this.ConstructiblesWaitingToBeCanceled[i].ConstructionGUID);
			writer.WriteAttributeString<int>("Refund", (int)this.ConstructiblesWaitingToBeCanceled[i].DustRefund);
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
		writer.WriteElementString<bool>("NotifyColossus", this.NotifyFirstColossus);
	}

	private bool BuyoutConstructionPreprocessor(OrderBuyoutConstruction order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ContextGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			return false;
		}
		if (!(gameEntity is SimulationObjectWrapper))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a simulation object wrapper.");
			return false;
		}
		SimulationObjectWrapper context = gameEntity as SimulationObjectWrapper;
		ConstructionQueue constructionQueue = null;
		if (!this.constructionQueues.TryGetValue(gameEntity.GUID, out constructionQueue))
		{
			Diagnostics.LogError("Order preprocessing failed because the context has no construction queue.");
			return false;
		}
		if (!this.GameEntityRepositoryService.TryGetValue(order.ConstructionGameEntityGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			return false;
		}
		if (!(gameEntity is Construction))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a Construction.");
			return false;
		}
		Construction construction = gameEntity as Construction;
		if (!constructionQueue.Contains(construction))
		{
			Diagnostics.LogError("Order preprocessing failed because the city construction queue does not contains the construction.");
			return false;
		}
		if (construction.ConstructibleElement.Tags.Contains(global::ConstructibleElement.TagNoBuyout))
		{
			Diagnostics.LogError("Order preprocessing failed because the construction cannot be bought out (tags: '{0}').", new object[]
			{
				construction.ConstructibleElement.Tags.ToString()
			});
			return false;
		}
		order.BuyoutCost = DepartmentOfTheTreasury.GetBuyoutCostWithBonus(context, construction);
		DepartmentOfTheTreasury agency = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		Diagnostics.Assert(agency != null, "Can't find the department of treasury.");
		float num = -order.BuyoutCost;
		if (!agency.IsTransferOfResourcePossible(base.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.Buyout, ref num))
		{
			Diagnostics.LogError("Order preprocessing failed because the transfer of money is not possible to buyout the construction #{0}.", new object[]
			{
				order.ConstructionGameEntityGUID
			});
			return false;
		}
		if (construction.IsBuyout)
		{
			Diagnostics.LogError("Order preprocessing failed because construction #{0} is already buyout.", new object[]
			{
				order.ConstructionGameEntityGUID
			});
			return false;
		}
		return true;
	}

	private IEnumerator BuyoutConstructionProcessor(OrderBuyoutConstruction order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ContextGUID, out gameEntity))
		{
			Diagnostics.LogError("Skipping buyout construction because the target game entity is not valid.");
			yield break;
		}
		SimulationObjectWrapper simulationObjectWrapper = gameEntity as SimulationObjectWrapper;
		ConstructionQueue constructionQueue = null;
		if (!this.constructionQueues.TryGetValue(gameEntity.GUID, out constructionQueue))
		{
			Diagnostics.LogError("Skipping buyout construction because the context has no construction queue.");
			yield break;
		}
		IGameEntity constructionGameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ConstructionGameEntityGUID, out constructionGameEntity))
		{
			Diagnostics.LogError("Skipping buyout construction because the target game entity is not valid.");
			yield break;
		}
		if (!(constructionGameEntity is Construction))
		{
			Diagnostics.LogError("Skipping buyout construction because the target game entity is not a Construction.");
			yield break;
		}
		Construction construction = constructionGameEntity as Construction;
		construction.IsBuyout = true;
		DepartmentOfTheTreasury departmentOfTheTreasury = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		Diagnostics.Assert(departmentOfTheTreasury != null, "Can't find the department of treasury.");
		if (!departmentOfTheTreasury.TryTransferResources(base.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.Buyout, -order.BuyoutCost))
		{
			Diagnostics.LogError("Buyout construction processor encounter a problem when transert of money application.");
		}
		if (construction.ConstructibleElement.Costs != null && construction.ConstructibleElement.Costs.Length > 0)
		{
			for (int index = 0; index < construction.ConstructibleElement.Costs.Length; index++)
			{
				if (!construction.ConstructibleElement.Costs[index].Instant && !construction.ConstructibleElement.Costs[index].InstantOnCompletion)
				{
					ConstructionResourceStock constructionResourceStock = construction.CurrentConstructionStock[index];
					float constructibleCost = DepartmentOfTheTreasury.GetProductionCostWithBonus(simulationObjectWrapper.SimulationObject, construction.ConstructibleElement, construction.ConstructibleElement.Costs[index], false);
					if (constructibleCost > constructionResourceStock.Stock)
					{
						if (construction.ConstructibleElement.Costs[index].ResourceName == DepartmentOfTheTreasury.Resources.Production)
						{
							float missingProductionCost = constructibleCost - constructionResourceStock.Stock;
							base.Empire.SetPropertyBaseValue("TotalIndustrySpent", base.Empire.GetPropertyValue("TotalIndustrySpent") + missingProductionCost);
							base.Empire.Refresh(false);
						}
						constructionResourceStock.Stock = constructibleCost;
					}
				}
			}
		}
		constructionQueue.Move(construction, 0);
		this.UpdateConstructionsProgress(gameEntity);
		if (TutorialManager.IsActivated)
		{
			IEventService eventService = Services.GetService<IEventService>();
			Diagnostics.Assert(eventService != null);
			IGameService gameService = Services.GetService<IGameService>();
			Diagnostics.Assert(gameService != null);
			global::Game game = gameService.Game as global::Game;
			Diagnostics.Assert(game != null);
			eventService.Notify(new EventTutorialBuyout(base.Empire, construction.ConstructibleElementName));
			yield break;
		}
		yield break;
	}

	private bool CancelConstructionPreprocessor(OrderCancelConstruction order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ContextGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			return false;
		}
		if (!(gameEntity is SimulationObjectWrapper))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a simulation object wrapper.");
			return false;
		}
		ConstructionQueue constructionQueue = null;
		if (!this.constructionQueues.TryGetValue(gameEntity.GUID, out constructionQueue))
		{
			Diagnostics.LogError("Order preprocessing failed because the context has no construction queue.");
			return false;
		}
		IGameEntity gameEntity2;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ConstructionGameEntityGUID, out gameEntity2))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			return false;
		}
		if (!(gameEntity2 is Construction))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a Construction.");
			return false;
		}
		Construction construction = gameEntity2 as Construction;
		if (!constructionQueue.Contains(construction))
		{
			Diagnostics.LogError("Order preprocessing failed because the context construction queue does not contains the construction.");
			return false;
		}
		return true;
	}

	private IEnumerator CancelConstructionProcessor(OrderCancelConstruction order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ContextGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			yield break;
		}
		if (!(gameEntity is SimulationObjectWrapper))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a simulation object wrapper.");
			yield break;
		}
		IGameEntity context = gameEntity;
		SimulationObjectWrapper simulationObjectWrapper = context as SimulationObjectWrapper;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ConstructionGameEntityGUID, out gameEntity))
		{
			Diagnostics.LogError("Skipping cancel construction because the target game entity is not valid.");
			yield break;
		}
		if (!(gameEntity is Construction))
		{
			Diagnostics.LogError("Skipping cancel construction because the target game entity is not a Construction.");
			yield break;
		}
		Construction construction = gameEntity as Construction;
		ConstructionQueue constructionQueue = null;
		if (!this.constructionQueues.TryGetValue(context.GUID, out constructionQueue))
		{
			Diagnostics.LogError("Skipping cancel construction because the context has no construction queue.");
			yield break;
		}
		if (construction.ConstructibleElement.Costs != null && construction.ConstructibleElement.Costs.Length > 0)
		{
			for (int index = 0; index < construction.CurrentConstructionStock.Length; index++)
			{
				if (construction.CurrentConstructionStock[index].Stock > 0f && construction.ConstructibleElement.Costs[index].Instant && !this.DepartmentOfTheTreasury.TryTransferResources(simulationObjectWrapper.SimulationObject, construction.CurrentConstructionStock[index].PropertyName, construction.CurrentConstructionStock[index].Stock))
				{
					Diagnostics.LogError("Order processing failed because the constructible element '{0}' ask for instant resource '{1}' that can't be retrieve.", new object[]
					{
						construction.ConstructibleElement.Name,
						construction.ConstructibleElement.Costs[index].ResourceName
					});
					yield break;
				}
			}
		}
		constructionQueue.Remove(construction);
		this.GameEntityRepositoryService.Unregister(construction);
		this.UpdateConstructionsProgress(context);
		if (construction.IsInProgress)
		{
			this.RemoveConstructionQueueDescriptors(simulationObjectWrapper, construction);
		}
		this.OnConstructionChanged(context, construction, ConstructionChangeEventAction.Cancelled);
		yield break;
	}

	private bool MoveConstructionPreprocessor(OrderMoveConstruction order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ContextGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			return false;
		}
		ConstructionQueue constructionQueue = null;
		if (!this.constructionQueues.TryGetValue(gameEntity.GUID, out constructionQueue))
		{
			Diagnostics.LogError("Order preprocessing failed because the context has no construction queue.");
			return false;
		}
		if (!this.GameEntityRepositoryService.TryGetValue(order.ConstructionGameEntityGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			return false;
		}
		if (!(gameEntity is Construction))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a Construction.");
			return false;
		}
		Construction construction = gameEntity as Construction;
		if (!constructionQueue.Contains(construction))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not queued.");
			return false;
		}
		order.WantedPositionIndex = Math.Max(0, order.WantedPositionIndex);
		order.WantedPositionIndex = Math.Min(order.WantedPositionIndex, constructionQueue.Length - 1);
		return true;
	}

	private IEnumerator MoveConstructionProcessor(OrderMoveConstruction order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ContextGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			yield break;
		}
		IGameEntity context = gameEntity;
		ConstructionQueue queue = this.GetConstructionQueue(context);
		Diagnostics.Assert(queue != null);
		if (!(gameEntity is SimulationObjectWrapper))
		{
			Diagnostics.LogError("Order preprocessing failed because the context game entity is not a SimulationObjectWrapper.");
			yield break;
		}
		if (!this.GameEntityRepositoryService.TryGetValue(order.ConstructionGameEntityGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			yield break;
		}
		if (!(gameEntity is Construction))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a Construction.");
			yield break;
		}
		Construction construction = gameEntity as Construction;
		queue.Move(construction, order.WantedPositionIndex);
		this.UpdateConstructionsProgress(context);
		yield break;
	}

	private bool QueueConstructionPreprocessor(OrderQueueConstruction order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ContextGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			return false;
		}
		if (!(gameEntity is SimulationObjectWrapper))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a simulation object wrapper.");
			return false;
		}
		Diagnostics.Assert(this.constructionQueues != null);
		if (!this.constructionQueues.ContainsKey(gameEntity.GUID))
		{
			Diagnostics.LogError("Order preprocessing failed because the context has no construction queue.");
			return false;
		}
		Diagnostics.Assert(this.ConstructibleElementDatabase != null);
		DepartmentOfIndustry.ConstructibleElement constructibleElement;
		if (!this.ConstructibleElementDatabase.TryGetValue(order.ConstructibleElementName, out constructibleElement))
		{
			Diagnostics.LogError("Order preprocessing failed because the constructible element {0} is not in the constructible element database.", new object[]
			{
				order.ConstructibleElementName
			});
			return false;
		}
		City city = gameEntity as City;
		if (city != null && city.IsInfected && constructibleElement.SubCategory != DepartmentOfTheInterior.InfectionAllowedSubcategory)
		{
			return false;
		}
		object[] customAttributes = constructibleElement.GetType().GetCustomAttributes(typeof(WorldPlacementCursorAttribute), true);
		if (customAttributes != null && customAttributes.Length != 0)
		{
			if (!order.WorldPosition.IsValid)
			{
				return false;
			}
		}
		else if (constructibleElement.Tags.Contains(DownloadableContent9.TagSolitary) && !order.WorldPosition.IsValid)
		{
			return false;
		}
		SimulationObjectWrapper context = gameEntity as SimulationObjectWrapper;
		Diagnostics.Assert(this.DepartmentOfTheTreasury != null);
		if (!this.CheckConstructiblePrerequisites(context, constructibleElement))
		{
			Diagnostics.LogWarning("Order preprocessing failed because the constructible element {0} is not allowed to be queued.", new object[]
			{
				order.ConstructibleElementName
			});
			return false;
		}
		order.ConstructionGameEntityGUID = this.GameEntityRepositoryService.GenerateGUID();
		bool result = true;
		if (constructibleElement.Costs == null || constructibleElement.Costs.Length == 0)
		{
			order.ResourceStocks = new ConstructionResourceStock[0];
		}
		else
		{
			ConstructionResourceStock[] resourceStocks = new ConstructionResourceStock[constructibleElement.Costs.Length];
			result = this.DepartmentOfTheTreasury.TryGetInstantConstructionResourceCost(context, constructibleElement, true, out resourceStocks);
			order.ResourceStocks = resourceStocks;
		}
		return result;
	}

	private IEnumerator QueueConstructionProcessor(OrderQueueConstruction order)
	{
		IGameService gameService = Services.GetService<IGameService>();
		if (gameService == null)
		{
			Diagnostics.LogError("Order preprocessing failed because we cannot retrieve the game service.");
			yield break;
		}
		global::Game game = gameService.Game as global::Game;
		if (game == null)
		{
			Diagnostics.LogError("gameService.Game isn't an instance of Game.");
			yield break;
		}
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ContextGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			yield break;
		}
		if (!(gameEntity is SimulationObjectWrapper))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a simulation object wrapper.");
			yield break;
		}
		IGameEntity context = gameEntity;
		SimulationObjectWrapper simulationObjectWrapper = context as SimulationObjectWrapper;
		if (order.ConstructionGameEntityGUID == GameEntityGUID.Zero)
		{
			Diagnostics.LogError("Skipping queue construction process because the game entity guid is null.");
			yield break;
		}
		DepartmentOfIndustry.ConstructibleElement constructibleElement;
		if (!this.ConstructibleElementDatabase.TryGetValue(order.ConstructibleElementName, out constructibleElement))
		{
			Diagnostics.LogError("Skipping queue construction process because the constructible element {0} is not in the constructible element database.", new object[]
			{
				order.ConstructibleElementName
			});
			yield break;
		}
		global::Empire empire;
		try
		{
			empire = game.Empires[order.EmpireIndex];
		}
		catch
		{
			Diagnostics.LogError("Order processor failed because empire index is invalid.");
			yield break;
		}
		ConstructionQueue constructionQueue = this.GetConstructionQueue(context);
		Diagnostics.Assert(constructionQueue != null);
		Construction construction = new Construction(constructibleElement, order.ConstructionGameEntityGUID, (base.Empire as global::Empire).Faction.AffinityMapping.Name, empire);
		IDatabase<SimulationDescriptor> simulationDescriptorDatatable = Databases.GetDatabase<SimulationDescriptor>(false);
		if (simulationDescriptorDatatable != null)
		{
			SimulationDescriptor classImprovementDescriptor = null;
			if (simulationDescriptorDatatable.TryGetValue("ClassConstruction", out classImprovementDescriptor))
			{
				construction.AddDescriptor(classImprovementDescriptor, false);
			}
		}
		construction.WorldPosition = order.WorldPosition;
		if (constructibleElement.Costs != null && constructibleElement.Costs.Length > 0)
		{
			for (int index = 0; index < constructibleElement.Costs.Length; index++)
			{
				construction.CurrentConstructionStock[index].Stock = order.ResourceStocks[index].Stock;
				if (order.ResourceStocks[index].Stock > 0f && !this.DepartmentOfTheTreasury.TryTransferResources(simulationObjectWrapper.SimulationObject, constructibleElement.Costs[index].ResourceName, -order.ResourceStocks[index].Stock))
				{
					Diagnostics.LogError("Order processing failed because the constructible element '{0}' ask for instant resource '{1}' that can't be retrieve.", new object[]
					{
						constructibleElement.Name,
						constructibleElement.Costs[index].ResourceName
					});
					yield break;
				}
			}
		}
		constructionQueue.Enqueue(construction);
		if (order.InsertAtFirstPlace)
		{
			constructionQueue.Move(construction, 0);
		}
		this.GameEntityRepositoryService.Register(construction);
		if (TutorialManager.IsActivated)
		{
			IEventService eventService = Services.GetService<IEventService>();
			Diagnostics.Assert(eventService != null);
			eventService.Notify(new EventTutorialConstructionQueued(empire, construction.ConstructibleElementName));
		}
		this.UpdateConstructionsProgress(context);
		using (new UnityProfilerSample("Notify"))
		{
			this.OnConstructionChanged(context, construction, ConstructionChangeEventAction.Started);
			yield break;
		}
		yield break;
	}

	public DepartmentOfTheTreasury DepartmentOfTheTreasury { get; private set; }

	public List<DepartmentOfIndustry.ConstructionToCancel> ConstructiblesWaitingToBeCanceled { get; private set; }

	public Dictionary<StaticString, int> ForbiddenConstructiblesUniqueInWorld { get; private set; }

	public List<StaticString> ALready { get; private set; }

	public DepartmentOfIndustry.ConstructibleElement[] OrderedWonders { get; private set; }

	internal bool NotifyFirstColossus { get; set; }

	private DepartmentOfTheInterior DepartmentOfTheInterior { get; set; }

	private IEventService EventService { get; set; }

	private IGameEntityRepositoryService GameEntityRepositoryService { get; set; }

	private IPlayerControllerRepositoryService PlayerControllerRepositoryService { get; set; }

	public void AddConstructionChangeEventHandler<T>(DepartmentOfIndustry.ConstructionChangeEventHandler handler) where T : DepartmentOfIndustry.ConstructibleElement
	{
		if (handler == null)
		{
			throw new ArgumentNullException("handler");
		}
		List<DepartmentOfIndustry.ConstructionChangeEventHandler> list;
		if (!this.constructibleElementTypeHandlers.TryGetValue(typeof(T), out list))
		{
			list = new List<DepartmentOfIndustry.ConstructionChangeEventHandler>();
			this.constructibleElementTypeHandlers.Add(typeof(T), list);
		}
		list.Add(handler);
	}

	public void AddQueueTo<T>(T context) where T : SimulationObjectWrapper, IGameEntity
	{
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		if (this.constructionQueues.ContainsKey(context.GUID))
		{
			throw new ArgumentException("The context already has a construction queue.", "context");
		}
		this.constructionQueues.Add(context.GUID, new ConstructionQueue());
	}

	public bool CheckConstructiblePrerequisites(SimulationObjectWrapper context, DepartmentOfIndustry.ConstructibleElement constructibleElement)
	{
		if (constructibleElement == null)
		{
			throw new ArgumentNullException("constructibleElement");
		}
		Diagnostics.Assert(base.Empire != null);
		bool flag = DepartmentOfTheTreasury.CheckConstructiblePrerequisites(context, constructibleElement, new string[]
		{
			ConstructionFlags.Prerequisite
		});
		if (flag)
		{
			if (constructibleElement.OnePerWorld && this.ForbiddenConstructiblesUniqueInWorld.Keys.Contains(constructibleElement.Name))
			{
				flag = false;
			}
			else
			{
				IGameEntity gameEntity = context as IGameEntity;
				if (gameEntity != null)
				{
					Diagnostics.Assert(this.constructionQueues != null);
					Diagnostics.Assert(this.constructionQueues.ContainsKey(gameEntity.GUID) && this.constructionQueues[gameEntity.GUID] != null);
					if (constructibleElement.IsUnique && this.constructionQueues[gameEntity.GUID].Contains(constructibleElement))
					{
						flag = false;
					}
				}
			}
		}
		return flag;
	}

	public void CheckConstructiblePrerequisites(SimulationObjectWrapper context, DepartmentOfIndustry.ConstructibleElement constructibleElement, ref List<StaticString> failureFlags)
	{
		if (constructibleElement == null)
		{
			throw new ArgumentNullException("constructibleElement");
		}
		if (constructibleElement.OnePerWorld && this.ForbiddenConstructiblesUniqueInWorld.Keys.Contains(constructibleElement.Name))
		{
			failureFlags.AddOnce(ConstructionFlags.Discard);
			return;
		}
		DepartmentOfTheTreasury.CheckConstructiblePrerequisites(context, constructibleElement, ref failureFlags, new string[]
		{
			ConstructionFlags.Prerequisite
		});
		IGameEntity gameEntity = context as IGameEntity;
		if (gameEntity != null)
		{
			Diagnostics.Assert(this.constructionQueues != null);
			Diagnostics.Assert(this.constructionQueues.ContainsKey(gameEntity.GUID) && this.constructionQueues[gameEntity.GUID] != null);
			if (constructibleElement.IsUnique && this.constructionQueues[gameEntity.GUID].Contains(constructibleElement))
			{
				failureFlags.AddOnce(ConstructionFlags.Prerequisite);
				if (constructibleElement.IsUniqueToDisableNotDiscard)
				{
					failureFlags.AddOnce(ConstructionFlags.Disable);
				}
				else
				{
					failureFlags.AddOnce(ConstructionFlags.Discard);
				}
			}
		}
	}

	public Construction GetConstruction(DepartmentOfIndustry.ConstructibleElement constructibleElement)
	{
		foreach (KeyValuePair<GameEntityGUID, ConstructionQueue> keyValuePair in this.constructionQueues)
		{
			Construction construction = keyValuePair.Value.Get(constructibleElement);
			if (construction != null)
			{
				return construction;
			}
		}
		return null;
	}

	public Construction GetConstruction(DepartmentOfIndustry.ConstructibleElement constructibleElement, out City city)
	{
		city = null;
		foreach (KeyValuePair<GameEntityGUID, ConstructionQueue> keyValuePair in this.constructionQueues)
		{
			Construction construction = keyValuePair.Value.Get(constructibleElement);
			if (construction != null)
			{
				IGameEntity gameEntity;
				if (this.GameEntityRepositoryService.TryGetValue(keyValuePair.Key, out gameEntity))
				{
					city = (gameEntity as City);
				}
				return construction;
			}
		}
		return null;
	}

	public void GetConstructibleState(SimulationObjectWrapper context, DepartmentOfIndustry.ConstructibleElement constructibleElement, ref List<StaticString> failureFlags)
	{
		this.CheckConstructiblePrerequisites(context, constructibleElement, ref failureFlags);
		if (failureFlags.Count == 0)
		{
			Diagnostics.Assert(base.Empire != null);
			DepartmentOfTheTreasury agency = base.Empire.GetAgency<DepartmentOfTheTreasury>();
			Diagnostics.Assert(agency != null);
			if (!agency.CheckConstructibleInstantCosts(context, constructibleElement))
			{
				failureFlags.AddOnce(ConstructionFlags.InstantCost);
			}
		}
	}

	public ConstructionQueue GetConstructionQueue(IGameEntity gameEntity)
	{
		if (gameEntity == null)
		{
			throw new ArgumentNullException("gameEntity");
		}
		ConstructionQueue result = null;
		if (!this.constructionQueues.TryGetValue(gameEntity.GUID, out result))
		{
			return null;
		}
		return result;
	}

	public ConstructionQueue GetConstructionQueue(GameEntityGUID guid)
	{
		if (!guid.IsValid)
		{
			throw new ArgumentException("Invalid guid.");
		}
		ConstructionQueue result = null;
		if (!this.constructionQueues.TryGetValue(guid, out result))
		{
			return null;
		}
		return result;
	}

	public void RemoveConstructionChangeEventHandler<T>(DepartmentOfIndustry.ConstructionChangeEventHandler handler) where T : DepartmentOfIndustry.ConstructibleElement
	{
		if (handler == null)
		{
			throw new ArgumentNullException("handler");
		}
		List<DepartmentOfIndustry.ConstructionChangeEventHandler> list;
		if (this.constructibleElementTypeHandlers.TryGetValue(typeof(T), out list))
		{
			list.Remove(handler);
		}
	}

	public void RemoveQueueFrom<T>(T context) where T : SimulationObjectWrapper, IGameEntity
	{
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		IGameEntity gameEntity = context;
		ConstructionQueue constructionQueue = null;
		if (this.constructionQueues.TryGetValue(gameEntity.GUID, out constructionQueue))
		{
			while (constructionQueue.Length > 0)
			{
				Construction construction = constructionQueue.Dequeue();
				if (construction.IsInProgress)
				{
					this.RemoveConstructionQueueDescriptors(context, construction);
				}
				this.GameEntityRepositoryService.Unregister(construction);
			}
			this.constructionQueues.Remove(gameEntity.GUID);
			Diagnostics.Log("[DepartmentOfIndustry] A construction queue has been removed (empire: #{0}, context: '{1}'.", new object[]
			{
				base.Empire.Index,
				context.Name
			});
		}
	}

	public void UpdateConstructionsProgress(IGameEntity gameEntity)
	{
		if (gameEntity == null)
		{
			Diagnostics.LogError("UpdateConstructionsProgress parameter gemEntity can't be null");
			return;
		}
		SimulationObjectWrapper simulationObjectWrapper = gameEntity as SimulationObjectWrapper;
		ConstructionQueue constructionQueue = this.GetConstructionQueue(gameEntity);
		if (constructionQueue == null)
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < constructionQueue.PendingConstructions.Count; i++)
		{
			Construction construction = constructionQueue.PendingConstructions[i];
			Diagnostics.Assert(construction != null);
			DepartmentOfIndustry.ConstructibleElement constructibleElement = construction.ConstructibleElement as DepartmentOfIndustry.ConstructibleElement;
			Diagnostics.Assert(constructibleElement != null);
			if (flag)
			{
				construction.IsInProgress = false;
			}
			else if (constructibleElement.OnePerWorld && this.ForbiddenConstructiblesUniqueInWorld.Keys.Contains(constructibleElement.Name))
			{
				construction.IsInProgress = false;
			}
			else if (!DepartmentOfTheTreasury.CheckConstructiblePrerequisites(simulationObjectWrapper, constructibleElement, new string[]
			{
				ConstructionFlags.Prerequisite
			}))
			{
				construction.IsInProgress = false;
			}
			else
			{
				construction.IsInProgress = true;
				flag = true;
			}
			if (!construction.WasInProgress && construction.IsInProgress)
			{
				this.ApplyConstructionQueueDescriptors(simulationObjectWrapper, construction);
			}
			else if (construction.WasInProgress && !construction.IsInProgress)
			{
				this.RemoveConstructionQueueDescriptors(simulationObjectWrapper, construction);
			}
			construction.WasInProgress = construction.IsInProgress;
		}
	}

	public void QueueIntegrationIFN(City city)
	{
		if (!base.Empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitMimics3) || !city.IsInfected)
		{
			return;
		}
		DepartmentOfPlanificationAndDevelopment agency = base.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
		DepartmentOfIndustry.ConstructibleElement constructibleElement = null;
		IEnumerable<DepartmentOfIndustry.ConstructibleElement> availableConstructibleElementsAsEnumerable = this.ConstructibleElementDatabase.GetAvailableConstructibleElementsAsEnumerable(new StaticString[0]);
		IEnumerable<DepartmentOfIndustry.ConstructibleElement> enumerable = from element in availableConstructibleElementsAsEnumerable
		where element is CityConstructibleActionDefinition
		select element;
		foreach (DepartmentOfIndustry.ConstructibleElement constructibleElement2 in enumerable)
		{
			CityConstructibleActionDefinition cityConstructibleActionDefinition = constructibleElement2 as CityConstructibleActionDefinition;
			if (cityConstructibleActionDefinition.Action.Name == "IntegrateFaction")
			{
				if (!string.IsNullOrEmpty(cityConstructibleActionDefinition.InfectedAffinityConstraint) && cityConstructibleActionDefinition.InfectedAffinityConstraint.Equals(city.LastNonInfectedOwner.Faction.Affinity.Name))
				{
					constructibleElement = constructibleElement2;
					break;
				}
			}
		}
		bool flag = false;
		if (agency.HasIntegratedFaction(city.LastNonInfectedOwner.Faction))
		{
			flag = true;
		}
		else if (city.Empire.Faction.Affinity.Name != city.LastNonInfectedOwner.Faction.Affinity.Name)
		{
			OrderQueueConstruction order = new OrderQueueConstruction(base.Empire.Index, city.GUID, constructibleElement, string.Empty);
			this.PlayerControllerRepositoryService.ActivePlayerController.PostOrder(order);
		}
		else if (city.Empire.Faction.Affinity.Name == city.LastNonInfectedOwner.Faction.Affinity.Name)
		{
			flag = true;
		}
		SimulationDescriptor descriptor;
		if (flag && this.simulationDescriptorDatabase.TryGetValue(City.TagCityStatusIntegrated, out descriptor))
		{
			city.AddDescriptor(descriptor, false);
		}
	}

	internal void RemoveConstructionQueueDescriptors(SimulationObjectWrapper context, Construction construction)
	{
		for (int i = 0; i < construction.ConstructibleElement.ConstructionQueueDescriptors.Length; i++)
		{
			SimulationDescriptor simulationDescriptor = construction.ConstructibleElement.ConstructionQueueDescriptors[i];
			if (simulationDescriptor != null)
			{
				context.RemoveDescriptor(simulationDescriptor);
			}
		}
		context.Refresh(false);
	}

	protected override IEnumerator OnInitialize()
	{
		yield return base.OnInitialize();
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		this.EventService = Services.GetService<IEventService>();
		Diagnostics.Assert(this.EventService != null);
		this.EventService.EventRaise += this.EventService_EventRaise;
		ISessionService sessionService = Services.GetService<ISessionService>();
		Diagnostics.Assert(sessionService != null);
		this.GameEntityRepositoryService = gameService.Game.Services.GetService<IGameEntityRepositoryService>();
		if (this.GameEntityRepositoryService == null)
		{
			Diagnostics.LogError("Failed to retrieve the game entity repository service.");
		}
		this.PlayerControllerRepositoryService = gameService.Game.Services.GetService<IPlayerControllerRepositoryService>();
		if (this.PlayerControllerRepositoryService == null)
		{
			Diagnostics.LogError("Failed to retrieve the player controller repository service.");
		}
		this.constructibleElementDatabase = Databases.GetDatabase<DepartmentOfIndustry.ConstructibleElement>(true);
		this.simulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
		DepartmentOfDefense departmentOfDefense = base.Empire.GetAgency<DepartmentOfDefense>();
		if (departmentOfDefense != null)
		{
			this.UnitDesignDatabase = departmentOfDefense.UnitDesignDatabase;
		}
		this.DepartmentOfTheTreasury = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		Diagnostics.Assert(this.DepartmentOfTheTreasury != null);
		this.DepartmentOfTheInterior = base.Empire.GetAgency<DepartmentOfTheInterior>();
		IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
		if (downloadableContentService != null && downloadableContentService.IsShared(DownloadableContent9.ReadOnlyName))
		{
			DepartmentOfIndustry.ConstructibleElement[] technologies = this.constructibleElementDatabase.GetValues();
			if (technologies != null)
			{
				this.OrderedWonders = (from definition in technologies
				where !(definition is FreeDistrictImprovementDefinition) && definition.SubCategory == DistrictImprovementDefinition.WonderSubCategory
				orderby definition.Name
				select definition).ToArray<DepartmentOfIndustry.ConstructibleElement>();
			}
		}
		this.ConstructiblesWaitingToBeCanceled = new List<DepartmentOfIndustry.ConstructionToCancel>();
		this.ForbiddenConstructiblesUniqueInWorld = new Dictionary<StaticString, int>();
		this.NotifyFirstColossus = true;
		if (downloadableContentService == null || !downloadableContentService.IsShared(DownloadableContent9.ReadOnlyName))
		{
			this.NotifyFirstColossus = false;
		}
		base.Empire.RegisterPass("GameClientState_Turn_End", "UpgradeConstruction", new Agency.Action(this.GameClientState_Turn_End_UpgradeConstruction), new string[]
		{
			"CollectResources",
			"ComputeCityDefensePoint",
			"ComputeUnlockResearches"
		});
		base.Empire.RegisterPass("GameClientState_Turn_End", "ComputeProduction", new Agency.Action(this.GameClientState_Turn_End_ComputeProduction), new string[]
		{
			"UpgradeConstruction"
		});
		base.Empire.RegisterPass("GameClientState_Turn_End", "ComputeBuildConstruction", new Agency.Action(this.GameClientState_Turn_End_ComputeBuildConstruction), new string[]
		{
			"ComputeProduction"
		});
		if (sessionService.Session != null && sessionService.Session.IsHosting)
		{
			base.Empire.RegisterPass("GameClientState_Turn_Begin", "SendWaitingOrders", new Agency.Action(this.GameClientState_Turn_Begin_SendWaitingOrders), new string[0]);
		}
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		if (this.eventHandlers == null)
		{
			this.RegisterEventHandlers();
		}
		yield break;
	}

	protected override IEnumerator OnLoadGame(Amplitude.Unity.Game.Game game)
	{
		yield return base.OnLoadGame(game);
		foreach (KeyValuePair<GameEntityGUID, ConstructionQueue> keyValuePair in this.constructionQueues)
		{
			City city = null;
			if (this.DepartmentOfTheInterior != null && this.DepartmentOfTheInterior.Cities != null)
			{
				for (int index = 0; index < this.DepartmentOfTheInterior.Cities.Count; index++)
				{
					if (this.DepartmentOfTheInterior.Cities[index].GUID == keyValuePair.Key)
					{
						city = this.DepartmentOfTheInterior.Cities[index];
						break;
					}
				}
			}
			ConstructionQueue constructionQueue = keyValuePair.Value;
			Diagnostics.Assert(constructionQueue != null);
			for (int index2 = constructionQueue.Length - 1; index2 >= 0; index2--)
			{
				Construction construction = constructionQueue.PeekAt(index2);
				if (!this.IsConstructionValid(construction))
				{
					Diagnostics.LogWarning("Compatibility issue with constructible element (name: '{0}').", new object[]
					{
						construction.ConstructibleElementName
					});
					if (city != null && construction.CurrentConstructionStock != null)
					{
						for (int stockIndex = 0; stockIndex < construction.CurrentConstructionStock.Length; stockIndex++)
						{
							ConstructionResourceStock constructionResourceStock = construction.CurrentConstructionStock[stockIndex];
							Diagnostics.Assert(constructionResourceStock != null);
							if (constructionResourceStock.Stock > 1.401298E-45f)
							{
								if (this.DepartmentOfTheTreasury.TryTransferResources(city.SimulationObject, constructionResourceStock.PropertyName, constructionResourceStock.Stock))
								{
									Diagnostics.LogWarning("Compatibility issue, succeed to refund constructible element (name: '{0}') {1} cost (amount: '{2}').", new object[]
									{
										construction.ConstructibleElementName,
										constructionResourceStock.PropertyName,
										constructionResourceStock.Stock
									});
								}
								else
								{
									Diagnostics.LogWarning("Compatibility issue, failed to refund constructible element (name: '{0}') {1} cost (amount: '{2}').", new object[]
									{
										construction.ConstructibleElementName,
										constructionResourceStock.PropertyName,
										constructionResourceStock.Stock
									});
								}
							}
						}
					}
					constructionQueue.Remove(construction);
				}
				else
				{
					this.GameEntityRepositoryService.Register(construction);
				}
			}
			if (city != null)
			{
				this.UpdateConstructionsProgress(city);
				this.VerifyConstructionQueueDescriptors(city);
			}
		}
		this.game = (game as global::Game);
		if (base.Empire is MajorEmpire)
		{
			for (int index3 = 0; index3 < this.game.Empires.Length; index3++)
			{
				global::Empire empire = this.game.Empires[index3];
				if (empire is MajorEmpire)
				{
					DepartmentOfIndustry departmentOfIndustry = empire.GetAgency<DepartmentOfIndustry>();
					if (departmentOfIndustry != null)
					{
						this.departmentOfIndustries.Add(departmentOfIndustry);
					}
				}
			}
		}
		yield break;
	}

	protected override void OnRelease()
	{
		base.OnRelease();
		this.GameEntityRepositoryService = null;
		this.PlayerControllerRepositoryService = null;
		this.constructibleElementDatabase = null;
		this.constructibleElementTypeHandlers.Clear();
		if (this.EventService != null)
		{
			this.EventService.EventRaise -= this.EventService_EventRaise;
			this.EventService = null;
		}
		if (this.eventHandlers != null)
		{
			this.eventHandlers.Clear();
			this.eventHandlers = null;
		}
		foreach (ConstructionQueue constructionQueue in this.constructionQueues.Values)
		{
			constructionQueue.Dispose();
		}
		this.constructionQueues.Clear();
	}

	private void ApplyConstructionQueueDescriptors(SimulationObjectWrapper context, Construction construction)
	{
		for (int i = 0; i < construction.ConstructibleElement.ConstructionQueueDescriptors.Length; i++)
		{
			SimulationDescriptor simulationDescriptor = construction.ConstructibleElement.ConstructionQueueDescriptors[i];
			if (simulationDescriptor != null)
			{
				context.AddDescriptor(simulationDescriptor, false);
			}
		}
		context.Refresh(false);
	}

	private DepartmentOfIndustry.ConstructionToCancel ComputeConstructionToCancel(DepartmentOfIndustry.ConstructibleElement constructibleElement)
	{
		float num = 0f;
		bool constructionFinished = false;
		foreach (KeyValuePair<GameEntityGUID, ConstructionQueue> keyValuePair in this.constructionQueues)
		{
			Construction construction = keyValuePair.Value.Get(constructibleElement);
			IGameEntity gameEntity;
			if (construction != null && this.GameEntityRepositoryService.TryGetValue(keyValuePair.Key, out gameEntity))
			{
				City city = gameEntity as City;
				if (city != null)
				{
					int num2;
					float num3;
					float num4;
					bool flag;
					QueueGuiItem.GetConstructionTurnInfos(city, construction, new List<ConstructionResourceStock>(), out num2, out num3, out num4, out flag);
					if (num2 <= 1)
					{
						constructionFinished = true;
					}
					for (int i = 0; i < construction.ConstructibleElement.Costs.Length; i++)
					{
						IConstructionCost constructionCost = construction.ConstructibleElement.Costs[i];
						if (!constructionCost.Instant && !constructionCost.InstantOnCompletion && !(constructionCost.ResourceName != DepartmentOfTheTreasury.Resources.Production))
						{
							ConstructionResourceStock constructionResourceStock = construction.CurrentConstructionStock[i];
							if (constructionResourceStock.Stock > 0f)
							{
								num = DepartmentOfTheTreasury.ConvertCostsTo(DepartmentOfTheTreasury.Resources.Refund, constructionCost.ResourceName, constructionResourceStock.Stock, base.Empire);
								num = DepartmentOfTheTreasury.ComputeCostWithReduction(base.Empire, num, DepartmentOfTheTreasury.Resources.Refund, CostReduction.ReductionType.Refund);
							}
							break;
						}
					}
					return new DepartmentOfIndustry.ConstructionToCancel
					{
						City = city,
						ConstructionGUID = construction.GUID,
						DustRefund = num,
						QueueGUID = keyValuePair.Key,
						ConstructionFinished = constructionFinished
					};
				}
			}
		}
		return default(DepartmentOfIndustry.ConstructionToCancel);
	}

	private DepartmentOfIndustry.ConstructionToCancel ComputeConstructionToCancel(DepartmentOfIndustry.ConstructibleElement constructibleElement, ConstructionQueue constructionQueue, GameEntityGUID queueGUID)
	{
		float num = 0f;
		bool constructionFinished = false;
		Construction construction = constructionQueue.Get(constructibleElement);
		IGameEntity gameEntity;
		if (construction != null && this.GameEntityRepositoryService.TryGetValue(queueGUID, out gameEntity))
		{
			City city = gameEntity as City;
			if (city != null)
			{
				int num2;
				float num3;
				float num4;
				bool flag;
				QueueGuiItem.GetConstructionTurnInfos(city, construction, new List<ConstructionResourceStock>(), out num2, out num3, out num4, out flag);
				if (num2 <= 1)
				{
					constructionFinished = true;
				}
				for (int i = 0; i < construction.ConstructibleElement.Costs.Length; i++)
				{
					IConstructionCost constructionCost = construction.ConstructibleElement.Costs[i];
					if (!constructionCost.Instant && !constructionCost.InstantOnCompletion && !(constructionCost.ResourceName != DepartmentOfTheTreasury.Resources.Production))
					{
						ConstructionResourceStock constructionResourceStock = construction.CurrentConstructionStock[i];
						if (constructionResourceStock.Stock > 0f)
						{
							num = DepartmentOfTheTreasury.ConvertCostsTo(DepartmentOfTheTreasury.Resources.Refund, constructionCost.ResourceName, constructionResourceStock.Stock, base.Empire);
							num = DepartmentOfTheTreasury.ComputeCostWithReduction(base.Empire, num, DepartmentOfTheTreasury.Resources.Refund, CostReduction.ReductionType.Refund);
						}
						break;
					}
				}
				return new DepartmentOfIndustry.ConstructionToCancel
				{
					City = city,
					ConstructionGUID = construction.GUID,
					DustRefund = num,
					QueueGUID = queueGUID,
					ConstructionFinished = constructionFinished
				};
			}
		}
		return default(DepartmentOfIndustry.ConstructionToCancel);
	}

	private IEnumerator GameClientState_Turn_Begin_SendWaitingOrders(string context, string name)
	{
		for (int index = 0; index < this.ConstructiblesWaitingToBeCanceled.Count; index++)
		{
			ConstructionQueue queue = null;
			if (this.constructionQueues.TryGetValue(this.ConstructiblesWaitingToBeCanceled[index].QueueGUID, out queue))
			{
				Construction construction = queue.PendingConstructions.FirstOrDefault((Construction ctr) => ctr.GUID == this.ConstructiblesWaitingToBeCanceled[index].ConstructionGUID);
				if (construction != null)
				{
					this.PlayerControllerRepositoryService.ActivePlayerController.PostOrder(new OrderCancelConstruction(base.Empire.Index, this.ConstructiblesWaitingToBeCanceled[index].QueueGUID, this.ConstructiblesWaitingToBeCanceled[index].ConstructionGUID));
					this.PlayerControllerRepositoryService.ActivePlayerController.PostOrder(new OrderTransferResources(base.Empire.Index, DepartmentOfTheTreasury.Resources.EmpireMoney, this.ConstructiblesWaitingToBeCanceled[index].DustRefund, 0UL));
					Diagnostics.Log("Refunding {0} of empire {1} for {2} empire money", new object[]
					{
						construction.ConstructibleElementName,
						base.Empire.Index,
						this.ConstructiblesWaitingToBeCanceled[index].DustRefund
					});
				}
			}
		}
		this.ConstructiblesWaitingToBeCanceled.Clear();
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_ComputeBuildConstruction(string context, string name)
	{
		List<Construction> finishedConstructions = new List<Construction>();
		List<string> empireEndTurnConstructionNames = new List<string>();
		Diagnostics.Assert(this.constructionQueues != null);
		foreach (KeyValuePair<GameEntityGUID, ConstructionQueue> keyValuePair in this.constructionQueues)
		{
			IGameEntity gameEntity = null;
			if (!this.GameEntityRepositoryService.TryGetValue(keyValuePair.Key, out gameEntity))
			{
				Diagnostics.LogError("The game entity repository service cannot return a reference for entity guid #{0}.", new object[]
				{
					keyValuePair.Key.ToString()
				});
			}
			else
			{
				SimulationObjectWrapper simulationObjectWrapper = gameEntity as SimulationObjectWrapper;
				int unfinishedConstructionQueueHead = 0;
				List<WorldPosition> worldPositionPickedByPreviousSolitaryUnit = new List<WorldPosition>();
				List<WorldPosition> waterTileFilledWithSeafaring = new List<WorldPosition>();
				int seafaringUnitCount = -1;
				WorldPosition seafaringSpawnPoint = WorldPosition.Invalid;
				Diagnostics.Assert(keyValuePair.Value != null);
				ReadOnlyCollection<Construction> pendingConstructions = keyValuePair.Value.PendingConstructions;
				Diagnostics.Assert(pendingConstructions != null);
				for (int pendingConstructionIndex = 0; pendingConstructionIndex < pendingConstructions.Count; pendingConstructionIndex++)
				{
					Construction pendingConstruction = pendingConstructions[pendingConstructionIndex];
					Diagnostics.Assert(pendingConstruction != null);
					DepartmentOfIndustry.ConstructibleElement constructibleElement = pendingConstruction.ConstructibleElement as DepartmentOfIndustry.ConstructibleElement;
					Diagnostics.Assert(constructibleElement != null);
					if (!constructibleElement.OnePerWorld || !this.ForbiddenConstructiblesUniqueInWorld.Keys.Contains(constructibleElement.Name))
					{
						if (!constructibleElement.OnePerEmpire || !empireEndTurnConstructionNames.Contains(constructibleElement.Name))
						{
							if (gameEntity is City && constructibleElement is CityConstructibleActionDefinition)
							{
								City city = gameEntity as City;
								CityConstructibleActionDefinition cityAction = constructibleElement as CityConstructibleActionDefinition;
								if (cityAction.Action.Name == "IntegrateFaction" && city.IsInfected)
								{
									DepartmentOfPlanificationAndDevelopment departmentOfPlanificationAndDevelopment = base.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
									if (departmentOfPlanificationAndDevelopment == null)
									{
										goto IL_F80;
									}
									if (string.IsNullOrEmpty(cityAction.InfectedAffinityConstraint) || !cityAction.InfectedAffinityConstraint.Equals(city.LastNonInfectedOwner.Faction.Affinity.Name))
									{
										goto IL_F80;
									}
									if (city.LastNonInfectedOwner.Faction.GetIntegrationDescriptorsCount() <= 0 || departmentOfPlanificationAndDevelopment.HasIntegratedFaction(city.LastNonInfectedOwner.Faction))
									{
										goto IL_F80;
									}
								}
							}
							Diagnostics.Assert(this.DepartmentOfTheTreasury != null);
							if (!DepartmentOfTheTreasury.CheckConstructiblePrerequisites(simulationObjectWrapper, constructibleElement, new string[]
							{
								ConstructionFlags.Prerequisite
							}))
							{
								if (unfinishedConstructionQueueHead - pendingConstructionIndex == 0 || constructibleElement.OnePerWorld)
								{
									this.EventService.Notify(new EventConstructionStopped(base.Empire, EventConstructionStopped.StopCause.FalsePrerequisites, gameEntity.GUID, pendingConstruction.ConstructibleElement));
								}
							}
							else
							{
								bool constructionFinished = true;
								if (!pendingConstruction.IsBuyout && pendingConstruction.ConstructibleElement.Costs != null && pendingConstruction.ConstructibleElement.Costs.Length > 0)
								{
									for (int costIndex = 0; costIndex < pendingConstruction.ConstructibleElement.Costs.Length; costIndex++)
									{
										if (!pendingConstruction.ConstructibleElement.Costs[costIndex].Instant && !pendingConstruction.ConstructibleElement.Costs[costIndex].InstantOnCompletion)
										{
											ConstructionResourceStock constructionResourceStock = pendingConstruction.CurrentConstructionStock[costIndex];
											float constructibleCost = DepartmentOfTheTreasury.GetProductionCostWithBonus(simulationObjectWrapper.SimulationObject, pendingConstruction.ConstructibleElement, pendingConstruction.ConstructibleElement.Costs[costIndex], false);
											constructionFinished &= (constructibleCost <= constructionResourceStock.Stock);
										}
									}
								}
								if (constructionFinished)
								{
									if (pendingConstruction.ConstructibleElement is UnitDesign)
									{
										City garrison = gameEntity as City;
										if (!seafaringSpawnPoint.IsValid && seafaringUnitCount < 0)
										{
											seafaringUnitCount = 1;
											seafaringSpawnPoint = garrison.DryDockPosition;
										}
										if (pendingConstruction.ConstructibleElement.Tags.Contains(DownloadableContent9.TagSolitary))
										{
											if (garrison != null && garrison.BesiegingEmpireIndex != -1)
											{
												if (unfinishedConstructionQueueHead - pendingConstructionIndex == 0)
												{
													this.EventService.Notify(new EventConstructionStopped(base.Empire, EventConstructionStopped.StopCause.Besiege, gameEntity.GUID, pendingConstruction.ConstructibleElement));
												}
												goto IL_F80;
											}
											if (!DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(pendingConstruction.WorldPosition, PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.Water) && pendingConstruction.ConstructibleElement.Tags.Contains(DownloadableContent9.TagColossus))
											{
												if (unfinishedConstructionQueueHead - pendingConstructionIndex == 0)
												{
													this.EventService.Notify(new EventConstructionStopped(base.Empire, EventConstructionStopped.StopCause.OccupiedSpawnLocation, gameEntity.GUID, pendingConstruction.ConstructibleElement));
												}
												goto IL_F80;
											}
											if (garrison != null && !pendingConstruction.ConstructibleElement.Tags.Contains(DownloadableContent9.TagColossus) && pendingConstruction.ConstructibleElement.Tags.Contains(DownloadableContent9.TagSolitary))
											{
												pendingConstruction.WorldPosition = garrison.GetCityCenter().WorldPosition;
												if (!DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(pendingConstruction.WorldPosition, PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.Water) || worldPositionPickedByPreviousSolitaryUnit.Contains(pendingConstruction.WorldPosition))
												{
													pendingConstruction.WorldPosition = WorldPosition.Invalid;
												}
												for (int index = 0; index < garrison.Districts.Count; index++)
												{
													if (pendingConstruction.WorldPosition != WorldPosition.Invalid)
													{
														break;
													}
													if (garrison.Districts[index].Type == DistrictType.Extension)
													{
														pendingConstruction.WorldPosition = garrison.Districts[index].WorldPosition;
														if (!DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(pendingConstruction.WorldPosition, PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.Water) || worldPositionPickedByPreviousSolitaryUnit.Contains(pendingConstruction.WorldPosition))
														{
															pendingConstruction.WorldPosition = WorldPosition.Invalid;
														}
													}
												}
												if (pendingConstruction.WorldPosition == WorldPosition.Invalid)
												{
													if (unfinishedConstructionQueueHead - pendingConstructionIndex == 0)
													{
														this.EventService.Notify(new EventConstructionStopped(base.Empire, EventConstructionStopped.StopCause.OccupiedSpawnLocation, gameEntity.GUID, pendingConstruction.ConstructibleElement));
													}
													goto IL_F80;
												}
												worldPositionPickedByPreviousSolitaryUnit.Add(pendingConstruction.WorldPosition);
											}
										}
										if (pendingConstruction.ConstructibleElement.Tags.Contains(DownloadableContent16.TagSeafaring))
										{
											if (garrison == null)
											{
												Diagnostics.LogWarning("PendingConstruction's garrison should not be null");
												goto IL_F80;
											}
											float fleetUnitSlot = garrison.GetPropertyValue(SimulationProperties.MaximumUnitSlotCount);
											if (!DepartmentOfDefense.CheckWhetherTargetPositionIsValidAsSeafaringSpawnLocation(seafaringSpawnPoint, garrison.Empire.Index, seafaringUnitCount) || fleetUnitSlot < (float)seafaringUnitCount)
											{
												waterTileFilledWithSeafaring.Add(seafaringSpawnPoint);
												seafaringSpawnPoint = WorldPosition.Invalid;
												seafaringUnitCount = 1;
												IWorldPositionningService worldPositionningService = this.game.Services.GetService<IWorldPositionningService>();
												for (int index2 = 0; index2 < garrison.Districts.Count; index2++)
												{
													if (!(garrison.Districts[index2].WorldPosition == seafaringSpawnPoint) && !waterTileFilledWithSeafaring.Contains(garrison.Districts[index2].WorldPosition))
													{
														if (worldPositionningService.IsWaterTile(garrison.Districts[index2].WorldPosition) && !worldPositionningService.IsFrozenWaterTile(garrison.Districts[index2].WorldPosition))
														{
															if (DepartmentOfDefense.CheckWhetherTargetPositionIsValidAsSeafaringSpawnLocation(garrison.Districts[index2].WorldPosition, garrison.Empire.Index, seafaringUnitCount))
															{
																seafaringSpawnPoint = garrison.Districts[index2].WorldPosition;
																break;
															}
														}
													}
												}
											}
											if (!seafaringSpawnPoint.IsValid)
											{
												pendingConstruction.WorldPosition = WorldPosition.Invalid;
												if (unfinishedConstructionQueueHead - pendingConstructionIndex == 0)
												{
													this.EventService.Notify(new EventConstructionStopped(base.Empire, EventConstructionStopped.StopCause.OccupiedSpawnLocation, gameEntity.GUID, pendingConstruction.ConstructibleElement));
												}
												goto IL_F80;
											}
											seafaringUnitCount++;
											pendingConstruction.WorldPosition = seafaringSpawnPoint;
										}
									}
									bool haveEnoughResources = true;
									for (int costIndex2 = 0; costIndex2 < pendingConstruction.ConstructibleElement.Costs.Length; costIndex2++)
									{
										if (pendingConstruction.ConstructibleElement.Costs[costIndex2].InstantOnCompletion)
										{
											IConstructionCost currentCost = pendingConstruction.ConstructibleElement.Costs[costIndex2];
											float resourceCost = -DepartmentOfTheTreasury.GetProductionCostWithBonus(simulationObjectWrapper.SimulationObject, pendingConstruction.ConstructibleElement, currentCost, false);
											if (!this.DepartmentOfTheTreasury.IsTransferOfResourcePossible(simulationObjectWrapper, currentCost.ResourceName, ref resourceCost))
											{
												haveEnoughResources = false;
												break;
											}
										}
									}
									if (haveEnoughResources)
									{
										if (constructibleElement.OnePerWorld && !this.departmentOfIndustries.Any((DepartmentOfIndustry departmentOfIndustry) => departmentOfIndustry.ForbiddenConstructiblesUniqueInWorld.Keys.Contains(constructibleElement.Name)))
										{
											this.OnConstructibleUniqueInWorldCompleted(constructibleElement);
											if (this.ForbiddenConstructiblesUniqueInWorld.Keys.Contains(constructibleElement.Name))
											{
												goto IL_F80;
											}
										}
										for (int costIndex3 = 0; costIndex3 < pendingConstruction.ConstructibleElement.Costs.Length; costIndex3++)
										{
											if (pendingConstruction.ConstructibleElement.Costs[costIndex3].InstantOnCompletion)
											{
												IConstructionCost currentCost2 = pendingConstruction.ConstructibleElement.Costs[costIndex3];
												float resourceCost2 = -DepartmentOfTheTreasury.GetProductionCostWithBonus(simulationObjectWrapper.SimulationObject, pendingConstruction.ConstructibleElement, currentCost2, false);
												this.DepartmentOfTheTreasury.TryTransferResources(simulationObjectWrapper, currentCost2.ResourceName, resourceCost2);
											}
										}
										if (this.DepartmentOfTheInterior != null && gameEntity is City)
										{
											bool shouldNotNotify = pendingConstruction.ConstructibleElement.Name.ToString().Contains("Raze") || pendingConstruction.ConstructibleElement.Name.ToString().Contains("Migrate") || (pendingConstruction.ConstructibleElement is UnitDesign && (pendingConstruction.ConstructibleElement as UnitDesign).CheckUnitAbility(UnitAbility.ReadonlyColonize, -1));
											this.DepartmentOfTheInterior.ComputeCityPopulation(gameEntity as City, !shouldNotNotify);
										}
										finishedConstructions.Add(pendingConstruction);
										empireEndTurnConstructionNames.Add(pendingConstruction.ConstructibleElement.Name);
										this.GameEntityRepositoryService.Unregister(pendingConstruction);
										simulationObjectWrapper.Refresh(true);
										this.UpdateConstructionsProgress(gameEntity);
										if (pendingConstruction.IsInProgress)
										{
											this.RemoveConstructionQueueDescriptors(simulationObjectWrapper, pendingConstruction);
										}
										this.OnConstructionChanged(gameEntity, pendingConstruction, ConstructionChangeEventAction.Completed);
										if (!pendingConstruction.ConstructibleElement.Tags.Contains(global::ConstructibleElement.TagNoNotification))
										{
											this.EventService.Notify(new EventConstructionEnded(base.Empire, gameEntity.GUID, pendingConstruction.ConstructibleElement));
										}
										unfinishedConstructionQueueHead++;
										base.Empire.Refresh(true);
										if (constructibleElement.Name == "DistrictAltarOfAuriga")
										{
											this.EventService.Notify(new EventAltarOfAuriga(base.Empire));
										}
									}
								}
							}
						}
					}
					IL_F80:;
				}
				foreach (Construction finishedConstruction in finishedConstructions)
				{
					this.constructionQueues[gameEntity.GUID].Remove(finishedConstruction);
				}
				this.UpdateConstructionsProgress(gameEntity);
			}
		}
		base.Empire.Refresh(true);
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_ComputeProduction(string context, string name)
	{
		foreach (KeyValuePair<GameEntityGUID, ConstructionQueue> keyValuePair in this.constructionQueues)
		{
			IGameEntity gameEntity = null;
			if (!this.GameEntityRepositoryService.TryGetValue(keyValuePair.Key, out gameEntity))
			{
				Diagnostics.LogError("The game entity repository service cannot return a reference for entity (guid: {0}).", new object[]
				{
					keyValuePair.Key.ToString()
				});
			}
			else
			{
				SimulationObjectWrapper wrapper = gameEntity as SimulationObjectWrapper;
				ReadOnlyCollection<Construction> pendingConstructions = keyValuePair.Value.PendingConstructions;
				for (int pendingConstructionIndex = 0; pendingConstructionIndex < pendingConstructions.Count; pendingConstructionIndex++)
				{
					Construction pendingConstruction = pendingConstructions[pendingConstructionIndex];
					if (pendingConstruction.ConstructibleElement.Costs != null && pendingConstruction.ConstructibleElement.Costs.Length != 0)
					{
						Diagnostics.Assert(this.DepartmentOfTheTreasury != null);
						if (DepartmentOfTheTreasury.CheckConstructiblePrerequisites(wrapper, pendingConstruction.ConstructibleElement, new string[]
						{
							ConstructionFlags.Prerequisite
						}))
						{
							bool constructionComplete = true;
							for (int index = 0; index < pendingConstruction.ConstructibleElement.Costs.Length; index++)
							{
								if (!pendingConstruction.ConstructibleElement.Costs[index].Instant && !pendingConstruction.ConstructibleElement.Costs[index].InstantOnCompletion)
								{
									ConstructionResourceStock constructionResourceStock = pendingConstruction.CurrentConstructionStock[index];
									StaticString resourceName = pendingConstruction.ConstructibleElement.Costs[index].ResourceName;
									float accumulatedStock = constructionResourceStock.Stock;
									float constructibleCost = DepartmentOfTheTreasury.GetProductionCostWithBonus(wrapper.SimulationObject, pendingConstruction.ConstructibleElement, pendingConstruction.ConstructibleElement.Costs[index], false);
									float remainingCost = constructibleCost - accumulatedStock;
									if (remainingCost > 0f)
									{
										float resourceStock;
										if (!this.DepartmentOfTheTreasury.TryGetResourceStockValue(wrapper.SimulationObject, resourceName, out resourceStock, false))
										{
											Diagnostics.LogError("Can't get resource stock value {0} on simulation object {1}.", new object[]
											{
												resourceName,
												wrapper.SimulationObject.Name
											});
										}
										else
										{
											float usedStock = Math.Min(remainingCost, resourceStock);
											if (this.DepartmentOfTheTreasury.TryTransferResources(wrapper.SimulationObject, resourceName, -usedStock))
											{
												constructionResourceStock.Stock += usedStock;
												if (resourceName == DepartmentOfTheTreasury.Resources.Production)
												{
													base.Empire.SetPropertyBaseValue("TotalIndustrySpent", base.Empire.GetPropertyValue("TotalIndustrySpent") + usedStock);
													base.Empire.Refresh(false);
												}
											}
											else
											{
												Diagnostics.LogWarning("Transfer of resource '{0}' is not possible.", new object[]
												{
													pendingConstruction.ConstructibleElement.Costs[index].ResourceName
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
				}
			}
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_UpgradeConstruction(string context, string name)
	{
		List<StaticString> lastFailureFlags = new List<StaticString>();
		foreach (KeyValuePair<GameEntityGUID, ConstructionQueue> keyValuePair in this.constructionQueues)
		{
			IGameEntity gameEntity = null;
			if (!this.GameEntityRepositoryService.TryGetValue(keyValuePair.Key, out gameEntity))
			{
				Diagnostics.LogError("The game entity repository service cannot return a reference for entity (guid: {0}).", new object[]
				{
					keyValuePair.Key.ToString()
				});
			}
			else
			{
				SimulationObjectWrapper wrapper = gameEntity as SimulationObjectWrapper;
				if (wrapper != null)
				{
					ConstructionQueue queue = keyValuePair.Value;
					for (int index = 0; index < queue.Length; index++)
					{
						Construction construction = queue.PeekAt(index);
						DepartmentOfIndustry.ConstructibleElement constructibleElement = construction.ConstructibleElement as DepartmentOfIndustry.ConstructibleElement;
						DepartmentOfIndustry.ConstructibleElement nextUpgradeElement;
						if (constructibleElement != null && !StaticString.IsNullOrEmpty(constructibleElement.NextUpgradeName) && this.constructibleElementDatabase.TryGetValue(constructibleElement.NextUpgradeName, out nextUpgradeElement))
						{
							lastFailureFlags.Clear();
							DepartmentOfTheTreasury.CheckConstructiblePrerequisites(wrapper, nextUpgradeElement, ref lastFailureFlags, new string[]
							{
								ConstructionFlags.Prerequisite
							});
							if (!lastFailureFlags.Contains(ConstructionFlags.Discard))
							{
								construction.SwapConstructibleElement(nextUpgradeElement);
							}
						}
					}
				}
			}
		}
		yield break;
	}

	private void OnConstructionChanged(IGameEntity context, Construction construction, ConstructionChangeEventAction action)
	{
		if (construction == null)
		{
			throw new ArgumentNullException("contruction");
		}
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		List<DepartmentOfIndustry.ConstructionChangeEventHandler> list;
		if (this.constructibleElementTypeHandlers.TryGetValue(construction.ConstructibleElement.GetType(), out list))
		{
			Diagnostics.Assert(list != null, "Event handler array must be instantiated.");
			ConstructionChangeEventArgs e = new ConstructionChangeEventArgs(action, context, construction);
			for (int i = 0; i < list.Count; i++)
			{
				Diagnostics.Assert(list[i] != null);
				list[i](this, e);
			}
		}
		if (action == ConstructionChangeEventAction.Completed)
		{
			DepartmentOfIndustry.ConstructibleElement constructibleElement = construction.ConstructibleElement as DepartmentOfIndustry.ConstructibleElement;
			if (constructibleElement != null && constructibleElement.OnePerWorld && !this.ForbiddenConstructiblesUniqueInWorld.ContainsKey(constructibleElement.Name))
			{
				this.ForbiddenConstructiblesUniqueInWorld.Add(constructibleElement.Name, base.Empire.Index);
			}
			this.RewardExperienceOnContructionFinished(context, construction);
			this.RewardEmpirePointsOnContructionFinished(context, construction);
		}
		if (this.OnConstructionChange != null)
		{
			this.OnConstructionChange(this, new ConstructionChangeEventArgs(action, context, construction));
		}
	}

	private bool IsConstructionValid(Construction construction)
	{
		if (construction == null)
		{
			throw new ArgumentNullException("construction");
		}
		return construction.ConstructibleElement != null && (construction.ConstructibleElement.Costs != null || construction.CurrentConstructionStock == null) && (construction.ConstructibleElement.Costs == null || construction.CurrentConstructionStock != null) && (construction.ConstructibleElement.Costs == null || construction.CurrentConstructionStock == null || construction.CurrentConstructionStock.Length == construction.ConstructibleElement.Costs.Length);
	}

	private void OnConstructibleUniqueInWorldCompleted(DepartmentOfIndustry.ConstructibleElement constructibleElement)
	{
		bool flag = constructibleElement.SubCategory == DistrictImprovementDefinition.WonderSubCategory;
		List<DepartmentOfIndustry.ConstructionToCancel> list = new List<DepartmentOfIndustry.ConstructionToCancel>();
		for (int i = 0; i < this.departmentOfIndustries.Count; i++)
		{
			DepartmentOfIndustry.ConstructionToCancel item = this.departmentOfIndustries[i].ComputeConstructionToCancel(constructibleElement);
			if (item.ConstructionGUID != GameEntityGUID.Zero)
			{
				item.DepartmentOfIndustry = this.departmentOfIndustries[i];
				list.Add(item);
			}
		}
		int num = 0;
		if (list.Count > 1)
		{
			float num2 = -1f;
			List<int> list2 = new List<int>();
			for (int j = 0; j < list.Count; j++)
			{
				if (list[j].ConstructionFinished)
				{
					float propertyValue = list[j].City.GetPropertyValue(SimulationProperties.NetCityProduction);
					if (num2 < propertyValue)
					{
						num2 = propertyValue;
						list2.Clear();
						list2.Add(j);
					}
					else if (num2 == propertyValue)
					{
						list2.Add(j);
					}
				}
			}
			if (list2.Count > 0)
			{
				int index3 = this.game.Turn % list2.Count;
				num = list2[index3];
			}
		}
		for (int k = 0; k < list.Count; k++)
		{
			if (k != num)
			{
				list[k].DepartmentOfIndustry.ConstructiblesWaitingToBeCanceled.Add(list[k]);
			}
		}
		int index2;
		if (num >= 0)
		{
			index2 = list[num].DepartmentOfIndustry.Empire.Index;
		}
		else
		{
			index2 = base.Empire.Index;
		}
		int index;
		for (index = 0; index < this.departmentOfIndustries.Count; index++)
		{
			EventWonder.EventWonderType eventWonderType = (this.departmentOfIndustries[index].Empire.Index == index2) ? EventWonder.EventWonderType.Completed : EventWonder.EventWonderType.Failed;
			if (eventWonderType == EventWonder.EventWonderType.Failed)
			{
				this.departmentOfIndustries[index].ForbiddenConstructiblesUniqueInWorld.Add(constructibleElement.Name, index2);
			}
			if (flag)
			{
				float dustRefund = list.FirstOrDefault((DepartmentOfIndustry.ConstructionToCancel construction) => construction.DepartmentOfIndustry.Empire.Index == this.departmentOfIndustries[index].Empire.Index).DustRefund;
				this.EventService.Notify(new EventWonder(this.departmentOfIndustries[index].Empire, constructibleElement, eventWonderType, null, dustRefund));
			}
		}
	}

	private void RewardEmpirePointsOnContructionFinished(IGameEntity context, Construction construction)
	{
		string x = "TraitBonusEmpirePointsOn" + construction.ConstructibleElement.Category + "Complete";
		float propertyValue = base.Empire.GetPropertyValue(x);
		if (propertyValue > 0f)
		{
			this.DepartmentOfTheTreasury.TryTransferResources(base.Empire, DepartmentOfTheTreasury.Resources.EmpirePoint, propertyValue);
		}
	}

	private void RewardExperienceOnContructionFinished(IGameEntity context, Construction construction)
	{
		if (context is IGarrison)
		{
			IGarrison garrison = context as IGarrison;
			if (garrison.Hero == null)
			{
				return;
			}
			float num = construction.ConstructibleElement.ExperienceReward;
			if (num <= 0f)
			{
				num = DepartmentOfTheTreasury.ConvertCostsTo(SimulationProperties.ExperienceReward, construction, context as SimulationObjectWrapper);
			}
			garrison.Hero.GainXp(num, false, true);
		}
	}

	private void VerifyConstructionQueueDescriptors(IGameEntity gameEntity)
	{
		ConstructionQueue queue = this.GetConstructionQueue(gameEntity);
		if (queue == null)
		{
			return;
		}
		Construction construction = null;
		for (int i = 0; i < queue.PendingConstructions.Count; i++)
		{
			if (queue.PendingConstructions[i].IsInProgress)
			{
				construction = queue.PendingConstructions[i];
				break;
			}
		}
		SimulationObjectWrapper simulationObjectWrapper = gameEntity as SimulationObjectWrapper;
		while (simulationObjectWrapper.RemoveDescriptorByName("CityStatusProducingSettler"))
		{
		}
		int index;
		for (index = 0; index < queue.PendingConstructions.Count; index++)
		{
			if (!queue.PendingConstructions[index].IsInProgress && queue.PendingConstructions[index].ConstructibleElement.ConstructionQueueDescriptors != null)
			{
				for (int j = 0; j < queue.PendingConstructions[index].ConstructibleElement.ConstructionQueueDescriptors.Length; j++)
				{
					int k = 0;
					if (construction != null && construction.ConstructibleElement.ConstructionQueueDescriptors != null && construction.ConstructibleElement.ConstructionQueueDescriptors.Length > 0)
					{
						for (int l = 0; l < construction.ConstructibleElement.ConstructionQueueDescriptors.Length; l++)
						{
							if (construction.ConstructibleElement.ConstructionQueueDescriptors[l].Name == queue.PendingConstructions[index].ConstructibleElement.ConstructionQueueDescriptors[j].Name)
							{
								k--;
							}
						}
					}
					for (int m = 0; m < simulationObjectWrapper.SimulationObject.DescriptorHolders.Count; m++)
					{
						if (simulationObjectWrapper.SimulationObject.DescriptorHolders[m].Descriptor.Name == queue.PendingConstructions[index].ConstructibleElement.ConstructionQueueDescriptors[j].Name)
						{
							k++;
						}
					}
					while (k > 0)
					{
						k--;
						simulationObjectWrapper.RemoveDescriptorByName(queue.PendingConstructions[index].ConstructibleElement.ConstructionQueueDescriptors[j].Name);
					}
				}
			}
			else
			{
				int descriptorIndex;
				for (descriptorIndex = 0; descriptorIndex < queue.PendingConstructions[index].ConstructibleElement.ConstructionQueueDescriptors.Length; descriptorIndex++)
				{
					if (!simulationObjectWrapper.SimulationObject.DescriptorHolders.Exists((SimulationDescriptorHolder match) => match.Descriptor.Name == queue.PendingConstructions[index].ConstructibleElement.ConstructionQueueDescriptors[descriptorIndex].Name))
					{
						simulationObjectWrapper.AddDescriptor(queue.PendingConstructions[index].ConstructibleElement.ConstructionQueueDescriptors[descriptorIndex], true);
					}
				}
			}
		}
	}

	private void EventService_EventRaise(object sender, EventRaiseEventArgs e)
	{
		DepartmentOfIndustry.EventHandler eventHandler;
		if (this.eventHandlers != null && e.RaisedEvent != null && this.eventHandlers.TryGetValue(e.RaisedEvent.EventName, out eventHandler))
		{
			eventHandler(e.RaisedEvent);
		}
	}

	private void RegisterEventHandlers()
	{
		this.eventHandlers = new Dictionary<StaticString, DepartmentOfIndustry.EventHandler>();
		this.eventHandlers.Add(EventFactionIntegrated.Name, new DepartmentOfIndustry.EventHandler(this.OnEventFactionIntegrated));
	}

	private void OnEventFactionIntegrated(Amplitude.Unity.Event.Event eventRaised)
	{
		if (!(eventRaised is EventFactionIntegrated))
		{
			return;
		}
		EventFactionIntegrated eventFactionIntegrated = eventRaised as EventFactionIntegrated;
		if (eventFactionIntegrated.Empire.Index != base.Empire.Index)
		{
			return;
		}
		foreach (KeyValuePair<GameEntityGUID, ConstructionQueue> keyValuePair in this.constructionQueues)
		{
			IGameEntity gameEntity = null;
			if (!this.GameEntityRepositoryService.TryGetValue(keyValuePair.Key, out gameEntity))
			{
				Diagnostics.LogError("The game entity repository service cannot return a reference for entity guid #{0}.", new object[]
				{
					keyValuePair.Key.ToString()
				});
			}
			else
			{
				Diagnostics.Assert(keyValuePair.Value != null);
				ReadOnlyCollection<Construction> pendingConstructions = keyValuePair.Value.PendingConstructions;
				Diagnostics.Assert(pendingConstructions != null);
				for (int i = 0; i < pendingConstructions.Count; i++)
				{
					Construction construction = pendingConstructions[i];
					Diagnostics.Assert(construction != null);
					DepartmentOfIndustry.ConstructibleElement constructibleElement = construction.ConstructibleElement as DepartmentOfIndustry.ConstructibleElement;
					Diagnostics.Assert(constructibleElement != null);
					if (gameEntity is City && constructibleElement is CityConstructibleActionDefinition)
					{
						City city = gameEntity as City;
						CityConstructibleActionDefinition cityConstructibleActionDefinition = constructibleElement as CityConstructibleActionDefinition;
						if (cityConstructibleActionDefinition.Action.Name == "IntegrateFaction" && city.IsInfected)
						{
							DepartmentOfPlanificationAndDevelopment agency = base.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
							if (agency != null)
							{
								if (agency.HasIntegratedFaction(city.LastNonInfectedOwner.Faction))
								{
									DepartmentOfIndustry.ConstructionToCancel item = this.ComputeConstructionToCancel(constructibleElement, keyValuePair.Value, keyValuePair.Key);
									if (item.ConstructionGUID != GameEntityGUID.Zero)
									{
										item.DepartmentOfIndustry = this;
										if (!this.ConstructiblesWaitingToBeCanceled.Contains(item))
										{
											this.ConstructiblesWaitingToBeCanceled.Add(item);
										}
									}
								}
							}
						}
					}
				}
			}
		}
	}

	private static Dictionary<StaticString, int> turnByResource = new Dictionary<StaticString, int>();

	private IDatabase<DepartmentOfIndustry.ConstructibleElement> constructibleElementDatabase;

	private readonly Dictionary<GameEntityGUID, ConstructionQueue> constructionQueues = new Dictionary<GameEntityGUID, ConstructionQueue>();

	private global::Game game;

	private Dictionary<Type, List<DepartmentOfIndustry.ConstructionChangeEventHandler>> constructibleElementTypeHandlers = new Dictionary<Type, List<DepartmentOfIndustry.ConstructionChangeEventHandler>>();

	private List<DepartmentOfIndustry> departmentOfIndustries = new List<DepartmentOfIndustry>();

	private IDatabase<SimulationDescriptor> simulationDescriptorDatabase;

	private Dictionary<StaticString, DepartmentOfIndustry.EventHandler> eventHandlers;

	public abstract class ConstructibleElement : global::ConstructibleElement
	{
		protected ConstructibleElement()
		{
			base.TooltipClass = DepartmentOfIndustry.ConstructibleElement.ReadOnlyDefaultTooltipClass;
		}

		[XmlAttribute]
		public bool IsUnique { get; protected set; }

		[XmlAttribute]
		public bool OnePerWorld { get; protected set; }

		[XmlAttribute]
		public bool OnePerEmpire { get; protected set; }

		[XmlAttribute]
		public bool IsUniqueToDisableNotDiscard { get; set; }

		[XmlIgnore]
		public StaticString NextUpgradeName { get; set; }

		[XmlElement("NextUpgradeName")]
		public string XmlSerializableNextUpgradeName
		{
			get
			{
				return this.NextUpgradeName;
			}
			set
			{
				this.NextUpgradeName = value;
			}
		}

		public static readonly string ReadOnlyDefaultTooltipClass = "Constructible";
	}

	public struct ConstructionToCancel
	{
		public City City;

		public GameEntityGUID ConstructionGUID;

		public DepartmentOfIndustry DepartmentOfIndustry;

		public float DustRefund;

		public GameEntityGUID QueueGUID;

		public bool ConstructionFinished;
	}

	private delegate void EventHandler(Amplitude.Unity.Event.Event raisedEvent);

	public delegate void ConstructionChangeEventHandler(object sender, ConstructionChangeEventArgs e);

	[CompilerGenerated]
	private sealed class FillAvailableConstructibleElements>c__AnonStorey920
	{
		internal bool <>m__3D9(DepartmentOfIndustry.ConstructibleElement contructibleElement)
		{
			return this.categories.Contains(contructibleElement.Category);
		}

		internal StaticString[] categories;
	}

	[CompilerGenerated]
	private sealed class GetAvailableConstructibleElements>c__AnonStorey921
	{
		internal bool <>m__3DA(DepartmentOfIndustry.ConstructibleElement contructibleElement)
		{
			return this.categories.Contains(contructibleElement.Category);
		}

		internal StaticString[] categories;
	}
}
