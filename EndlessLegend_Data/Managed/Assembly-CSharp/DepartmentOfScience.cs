using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Path;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Game.Orders;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

[OrderProcessor(typeof(OrderBuyOutTechnology), "BuyOutTechnology")]
[OrderProcessor(typeof(OrderUnlockTechnologyByInfiltration), "UnlockTechnologyByInfiltration")]
[OrderProcessor(typeof(OrderCancelResearch), "CancelResearch")]
[OrderProcessor(typeof(OrderQueueResearch), "QueueResearch")]
[OrderProcessor(typeof(OrderForceUnlockTechnology), "ForceUnlockTechnology")]
public class DepartmentOfScience : Agency, Amplitude.Xml.Serialization.IXmlSerializable
{
	public DepartmentOfScience(global::Empire empire) : base(empire)
	{
		this.CurrentTechnologyEraNumber = 1;
	}

	public event DepartmentOfScience.ConstructionChangeEventHandler ResearchQueueChanged;

	public event DepartmentOfScience.ConstructibleElementEventHandler TechnologyUnlocked;

	public static int GetMaxEraNumber()
	{
		int num = 0;
		IGameService service = Services.GetService<IGameService>();
		if (service == null)
		{
			throw new ArgumentNullException("no game service");
		}
		if (service.Game == null)
		{
			throw new ArgumentNullException("no game");
		}
		global::Game game = service.Game as global::Game;
		foreach (global::Empire empire in game.Empires)
		{
			if (empire is MajorEmpire)
			{
				num = Math.Max(num, empire.GetAgency<DepartmentOfScience>().CurrentTechnologyEraNumber);
			}
		}
		return num;
	}

	public static void BuildTechnologyTooltip(TechnologyDefinition technologyDefinition, global::Empire empire, AgeTooltip tooltip, MultipleConstructibleTooltipData.TechnologyState state = MultipleConstructibleTooltipData.TechnologyState.Normal)
	{
		string @class;
		string content;
		object clientData;
		DepartmentOfScience.BuildTechnologyTooltip(technologyDefinition, empire, out @class, out content, out clientData, state);
		tooltip.Class = @class;
		tooltip.Content = content;
		tooltip.ClientData = clientData;
	}

	public static void BuildTechnologyTooltipWithAffinity(TechnologyDefinition technologyDefinition, StaticString affinity, AgeTooltip tooltip, MultipleConstructibleTooltipData.TechnologyState state = MultipleConstructibleTooltipData.TechnologyState.Normal)
	{
		List<global::ConstructibleElement> unlocksByTechnology = DepartmentOfScience.FilterUnlocks(technologyDefinition, affinity);
		string @class;
		string content;
		object clientData;
		DepartmentOfScience.BuildTechnologyTooltip(technologyDefinition, null, unlocksByTechnology, out @class, out content, out clientData, state);
		tooltip.Class = @class;
		tooltip.Content = content;
		tooltip.ClientData = clientData;
	}

	public static void BuildTechnologyTooltip(TechnologyDefinition technologyDefinition, global::Empire empire, out string tooltipClass, out string tooltipContent, out object clientData, MultipleConstructibleTooltipData.TechnologyState state = MultipleConstructibleTooltipData.TechnologyState.Normal)
	{
		List<global::ConstructibleElement> unlocksByTechnology = DepartmentOfScience.FilterUnlocks(technologyDefinition, empire);
		DepartmentOfScience.BuildTechnologyTooltip(technologyDefinition, empire, unlocksByTechnology, out tooltipClass, out tooltipContent, out clientData, state);
	}

	public static Texture2D GetCategoryIcon(TechnologyDefinition technologyDefinition, StaticString iconSize)
	{
		IGuiPanelHelper guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
		Diagnostics.Assert(guiPanelHelper != null, "Unable to access GuiPanelHelper");
		GuiElement guiElement;
		Texture2D result;
		if (technologyDefinition != null && guiPanelHelper.TryGetGuiElement(technologyDefinition.Category, out guiElement) && guiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Small, out result))
		{
			return result;
		}
		return null;
	}

	public static string GetCategoryTitle(TechnologyDefinition technologyDefinition)
	{
		IGuiPanelHelper guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
		Diagnostics.Assert(guiPanelHelper != null, "Unable to access GuiPanelHelper");
		GuiElement guiElement;
		if (technologyDefinition != null && guiPanelHelper.TryGetGuiElement(technologyDefinition.Category, out guiElement))
		{
			return guiElement.Title;
		}
		return null;
	}

	public static Texture2D GetSubCategoryIcon(TechnologyDefinition technologyDefinition, StaticString iconSize)
	{
		IGuiPanelHelper guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
		Diagnostics.Assert(guiPanelHelper != null, "Unable to access GuiPanelHelper");
		GuiElement guiElement;
		Texture2D result;
		if (technologyDefinition != null && !string.IsNullOrEmpty(technologyDefinition.SubCategory) && guiPanelHelper.TryGetGuiElement(technologyDefinition.SubCategory, out guiElement) && guiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Small, out result))
		{
			return result;
		}
		return null;
	}

	public static string GetSubCategoryTitle(TechnologyDefinition technologyDefinition)
	{
		IGuiPanelHelper guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
		Diagnostics.Assert(guiPanelHelper != null, "Unable to access GuiPanelHelper");
		GuiElement guiElement;
		if (technologyDefinition != null && !string.IsNullOrEmpty(technologyDefinition.SubCategory) && guiPanelHelper.TryGetGuiElement(technologyDefinition.SubCategory, out guiElement))
		{
			return guiElement.Title;
		}
		return null;
	}

	public static int GetTechnologyEraNumber(TechnologyDefinition technologyDefinition)
	{
		IGuiPanelHelper guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
		Diagnostics.Assert(guiPanelHelper != null, "Unable to access GuiPanelHelper");
		GuiElement guiElement;
		if (guiPanelHelper.TryGetGuiElement(technologyDefinition.Name, out guiElement))
		{
			TechnologyGuiElement technologyGuiElement = guiElement as TechnologyGuiElement;
			if (technologyGuiElement != null && guiPanelHelper.TryGetGuiElement(technologyGuiElement.TechnologyEraReference, out guiElement))
			{
				TechnologyEraGuiElement technologyEraGuiElement = guiElement as TechnologyEraGuiElement;
				if (technologyEraGuiElement != null)
				{
					return technologyEraGuiElement.TechnologyEraNumber;
				}
			}
		}
		return -1;
	}

	public static string GetTechnologyTitle(TechnologyDefinition technologyDefinition)
	{
		IGuiPanelHelper guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
		Diagnostics.Assert(guiPanelHelper != null, "Unable to access GuiPanelHelper");
		List<global::ConstructibleElement> unlocksByTechnology = technologyDefinition.GetUnlocksByTechnology();
		GuiElement guiElement2;
		if (unlocksByTechnology != null && unlocksByTechnology.Count == 1)
		{
			GuiElement guiElement;
			if (guiPanelHelper.TryGetGuiElement(unlocksByTechnology[0].Name, out guiElement))
			{
				return guiElement.Title;
			}
		}
		else if (guiPanelHelper.TryGetGuiElement(technologyDefinition.Name, out guiElement2))
		{
			return guiElement2.Title;
		}
		return null;
	}

	public static Texture2D GetTechnologyImage(TechnologyDefinition technologyDefinition, StaticString iconSize)
	{
		IGuiPanelHelper guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
		Diagnostics.Assert(guiPanelHelper != null, "Unable to access GuiPanelHelper");
		List<global::ConstructibleElement> unlocksByTechnology = technologyDefinition.GetUnlocksByTechnology();
		GuiElement guiElement2;
		Texture2D result2;
		if (unlocksByTechnology != null && unlocksByTechnology.Count == 1)
		{
			GuiElement guiElement;
			Texture2D result;
			if (guiPanelHelper.TryGetGuiElement(unlocksByTechnology[0].Name, out guiElement) && guiPanelHelper.TryGetTextureFromIcon(guiElement, iconSize, out result))
			{
				return result;
			}
		}
		else if (guiPanelHelper.TryGetGuiElement(technologyDefinition.Name, out guiElement2) && guiElement2.Icons != null && guiElement2.Icons.IconDefinitions != null && guiElement2.Icons.IconDefinitions.Length > 0 && guiPanelHelper.TryGetTextureFromIcon(guiElement2, iconSize, out result2))
		{
			return result2;
		}
		return null;
	}

	public static void FillStealableTechnology(global::Empire empireWhichReceive, global::Empire empireWhichProvide, int eraMin, int eraMax, ref List<DepartmentOfScience.ConstructibleElement> stealableTechnologies)
	{
		IDatabase<DepartmentOfScience.ConstructibleElement> database = Databases.GetDatabase<DepartmentOfScience.ConstructibleElement>(false);
		DepartmentOfScience.ConstructibleElement[] values = database.GetValues();
		DepartmentOfScience agency = empireWhichReceive.GetAgency<DepartmentOfScience>();
		DepartmentOfScience agency2 = empireWhichProvide.GetAgency<DepartmentOfScience>();
		for (int i = 0; i < values.Length; i++)
		{
			TechnologyDefinition technologyDefinition = values[i] as TechnologyDefinition;
			if (technologyDefinition != null)
			{
				if (technologyDefinition.Visibility == TechnologyDefinitionVisibility.Visible)
				{
					if (agency.GetTechnologyState(technologyDefinition) != DepartmentOfScience.ConstructibleElement.State.Researched)
					{
						if (agency2.GetTechnologyState(technologyDefinition) == DepartmentOfScience.ConstructibleElement.State.Researched)
						{
							int technologyEraNumber = DepartmentOfScience.GetTechnologyEraNumber(technologyDefinition);
							if (technologyEraNumber >= eraMin && technologyEraNumber <= eraMax)
							{
								if (DepartmentOfTheTreasury.CheckConstructiblePrerequisites(empireWhichReceive, technologyDefinition, new string[]
								{
									ConstructionFlags.DiplomaticTradable,
									ConstructionFlags.DiplomaticTradableForEmpireWhichReceives
								}))
								{
									if (DepartmentOfTheTreasury.CheckConstructiblePrerequisites(empireWhichProvide, technologyDefinition, new string[]
									{
										ConstructionFlags.DiplomaticTradable,
										ConstructionFlags.DiplomaticTradableForEmpireWhichProvides
									}))
									{
										stealableTechnologies.Add(technologyDefinition);
									}
								}
							}
						}
					}
				}
			}
		}
	}

	public bool CanBribe()
	{
		return this.GetTechnologyState("TechnologyDefinitionMapActionParley") == DepartmentOfScience.ConstructibleElement.State.Researched;
	}

	public bool CanCreatePrivateers()
	{
		return this.GetTechnologyState("TechnologyDefinitionPrivateers") == DepartmentOfScience.ConstructibleElement.State.Researched || this.GetTechnologyState("TechnologyDefinitionPrivateersRovingClans") == DepartmentOfScience.ConstructibleElement.State.Researched;
	}

	public bool CanParley()
	{
		return this.GetTechnologyState("TechnologyDefinitionMapActionParley") == DepartmentOfScience.ConstructibleElement.State.Researched;
	}

	public bool CanTradeHeroes(bool ignoreBan = false)
	{
		if (ignoreBan)
		{
			return this.HasResearchTag("TechnologyMarketplaceHeroes");
		}
		return this.HasResearchTag("TechnologyMarketplaceHeroes") && !this.departmentOfForeignAffairs.IsBannedFromMarket();
	}

	public bool CanTradeUnits(bool ignoreBan = false)
	{
		if (ignoreBan)
		{
			return this.HasResearchTag("TechnologyMarketplaceMercenaries");
		}
		return this.HasResearchTag("TechnologyMarketplaceMercenaries") && !this.departmentOfForeignAffairs.IsBannedFromMarket();
	}

	public bool CanTradeResourcesAndBoosters(bool ignoreBan = false)
	{
		if (ignoreBan)
		{
			return this.HasResearchTag("TechnologyMarketplaceResources");
		}
		return this.HasResearchTag("TechnologyMarketplaceResources") && !this.departmentOfForeignAffairs.IsBannedFromMarket();
	}

	public bool CanSacrificePopulation()
	{
		return this.GetTechnologyState("TechnologyDefinitionNecrophages8") == DepartmentOfScience.ConstructibleElement.State.Researched;
	}

	public float GetResearchPropertyValue(StaticString propertyName)
	{
		if (propertyName == null)
		{
			throw new ArgumentNullException("propertyName");
		}
		Diagnostics.Assert(this.researchSimulationObjectWrapper != null);
		return this.researchSimulationObjectWrapper.GetPropertyValue(propertyName);
	}

	public bool HasResearchTag(StaticString tagName)
	{
		if (StaticString.IsNullOrEmpty(tagName))
		{
			throw new ArgumentNullException("tagName");
		}
		Diagnostics.Assert(this.researchSimulationObjectWrapper != null);
		return this.researchSimulationObjectWrapper.SimulationObject.Tags.Contains(tagName);
	}

	public bool HaveResearchedAtLeastOneTradeTechnology()
	{
		return this.GetTechnologyState("TechnologyDefinitionMarketplaceResources") == DepartmentOfScience.ConstructibleElement.State.Researched || this.GetTechnologyState("TechnologyDefinitionMarketplaceMercenaries") == DepartmentOfScience.ConstructibleElement.State.Researched || this.GetTechnologyState("TechnologyDefinitionMarketplaceHeroes") == DepartmentOfScience.ConstructibleElement.State.Researched;
	}

	public bool HaveResearchedShipTechnology()
	{
		return this.GetTechnologyState("TechnologyDefinitionShip") == DepartmentOfScience.ConstructibleElement.State.Researched;
	}

	public int GetTechnologyRemainingTurn(TechnologyDefinition technologyDefinition)
	{
		int result = int.MaxValue;
		if (!base.Empire.SimulationObject.Tags.Contains("AffinityReplicants"))
		{
			Diagnostics.Assert(this.ResearchQueue != null);
			Construction construction = this.ResearchQueue.Get(technologyDefinition);
			if (construction != null)
			{
				Diagnostics.Assert(this.departmentOfTheTreasury != null);
				result = this.departmentOfTheTreasury.ComputeConstructionRemainingTurn(base.Empire, construction);
			}
			float productionCostWithBonus = DepartmentOfTheTreasury.GetProductionCostWithBonus(base.Empire.SimulationObject, technologyDefinition, DepartmentOfTheTreasury.Resources.EmpireResearch);
			Diagnostics.Assert(this.departmentOfTheTreasury != null);
			float num;
			if (this.departmentOfTheTreasury.TryGetNetResourceValue(base.Empire, DepartmentOfTheTreasury.Resources.EmpireResearch, out num, false) && num > 0f)
			{
				result = Mathf.CeilToInt(productionCostWithBonus / num);
			}
		}
		else
		{
			float num2 = float.MaxValue;
			IDatabase<DepartmentOfScience.ConstructibleElement> database = Databases.GetDatabase<DepartmentOfScience.ConstructibleElement>(false);
			DepartmentOfScience.ConstructibleElement technology;
			if (database.TryGetValue(technologyDefinition.Name, out technology))
			{
				num2 = this.GetBuyOutTechnologyCost(technology);
			}
			Diagnostics.Assert(this.departmentOfTheTreasury != null);
			float num3;
			if (this.departmentOfTheTreasury.TryGetNetResourceValue(base.Empire, DepartmentOfTheTreasury.Resources.EmpireMoney, out num3, false) && num3 > 0f)
			{
				result = Mathf.CeilToInt(num2 / num3);
			}
		}
		return result;
	}

	public float GetBuyOutTechnologyCost(global::ConstructibleElement technology)
	{
		if (technology == null)
		{
			return float.MaxValue;
		}
		float productionCostWithBonus = DepartmentOfTheTreasury.GetProductionCostWithBonus(base.Empire.SimulationObject, technology, DepartmentOfTheTreasury.Resources.EmpireResearch);
		return DepartmentOfTheTreasury.ConvertCostsTo(DepartmentOfTheTreasury.Resources.TechnologiesBuyOut, SimulationProperties.EmpireResearch, productionCostWithBonus, base.Empire);
	}

	public float GetTechnologyUnlockedCount()
	{
		return this.GetResearchPropertyValue("UnlockedTechnologyCount");
	}

	public bool CanPillage()
	{
		return this.GetTechnologyState("TechnologyDefinitionMapActionPillage") == DepartmentOfScience.ConstructibleElement.State.Researched;
	}

	private static List<global::ConstructibleElement> FilterUnlocks(TechnologyDefinition technologyDefinition, global::Empire empire)
	{
		List<global::ConstructibleElement> unlocksByTechnology = technologyDefinition.GetUnlocksByTechnology();
		if (unlocksByTechnology == null)
		{
			return null;
		}
		List<global::ConstructibleElement> list = null;
		if (empire != null)
		{
			for (int i = unlocksByTechnology.Count - 1; i >= 0; i--)
			{
				if (!DepartmentOfTheTreasury.CheckConstructiblePrerequisites(empire, unlocksByTechnology[i], new string[]
				{
					ConstructionFlags.Affinity
				}))
				{
					if (list == null)
					{
						list = new List<global::ConstructibleElement>(unlocksByTechnology);
					}
					list.RemoveAt(i);
				}
			}
		}
		if (list == null)
		{
			return unlocksByTechnology;
		}
		return list;
	}

	private static List<global::ConstructibleElement> FilterUnlocks(TechnologyDefinition technologyDefinition, StaticString affinity)
	{
		List<global::ConstructibleElement> unlocksByTechnology = technologyDefinition.GetUnlocksByTechnology();
		if (unlocksByTechnology == null)
		{
			return null;
		}
		Tags tags = new Tags();
		tags.AddTag(affinity);
		List<global::ConstructibleElement> list = null;
		for (int i = unlocksByTechnology.Count - 1; i >= 0; i--)
		{
			for (int j = 0; j < unlocksByTechnology[i].Prerequisites.Length; j++)
			{
				PathPrerequisite pathPrerequisite = unlocksByTechnology[i].Prerequisites[j] as PathPrerequisite;
				if (pathPrerequisite != null)
				{
					if (pathPrerequisite.Flags != null && pathPrerequisite.Flags.Length != 0)
					{
						if (Array.Exists<StaticString>(pathPrerequisite.Flags, (StaticString match) => match == ConstructionFlags.Affinity))
						{
							if (pathPrerequisite.CurrentSimulationPath.ValidateTagsAt(tags, pathPrerequisite.CurrentSimulationPath.Length - 1) == pathPrerequisite.Inverted)
							{
								if (list == null)
								{
									list = new List<global::ConstructibleElement>(unlocksByTechnology);
								}
								list.RemoveAt(i);
							}
						}
					}
				}
			}
		}
		if (list == null)
		{
			return unlocksByTechnology;
		}
		return list;
	}

	private static void BuildTechnologyTooltip(TechnologyDefinition technologyDefinition, global::Empire empire, List<global::ConstructibleElement> unlocksByTechnology, out string tooltipClass, out string tooltipContent, out object clientData, MultipleConstructibleTooltipData.TechnologyState state = MultipleConstructibleTooltipData.TechnologyState.Normal)
	{
		clientData = null;
		tooltipClass = string.Empty;
		tooltipContent = string.Empty;
		if (unlocksByTechnology != null)
		{
			if (unlocksByTechnology.Count == 1)
			{
				global::ConstructibleElement constructibleElement = unlocksByTechnology[0];
				if (constructibleElement.Tags.Contains(DownloadableContent9.TagColossus))
				{
					tooltipClass = "Technology" + constructibleElement.TooltipClass + "_Colossus";
				}
				else if (constructibleElement.Tags.Contains(DownloadableContent16.TransportShipUnit))
				{
					tooltipClass = "Technology" + constructibleElement.TooltipClass + "_TransportShipUnit";
				}
				else if (constructibleElement.Tags.Contains(DownloadableContent16.TagSeafaring))
				{
					tooltipClass = "Technology" + constructibleElement.TooltipClass + "_SeafaringUnit";
				}
				else
				{
					tooltipClass = "Technology" + constructibleElement.TooltipClass;
				}
				tooltipContent = constructibleElement.Name;
				clientData = new MultipleConstructibleTooltipData(empire, null, constructibleElement, technologyDefinition, state);
			}
			else if (unlocksByTechnology.Count > 1)
			{
				tooltipClass = "Multiple" + unlocksByTechnology[0].TooltipClass;
				tooltipContent = technologyDefinition.Name;
				clientData = new MultipleConstructibleTooltipData(empire, null, unlocksByTechnology, technologyDefinition, state);
			}
		}
		if (clientData == null)
		{
			tooltipClass = technologyDefinition.TooltipClass;
			tooltipContent = technologyDefinition.Name;
			clientData = new MultipleConstructibleTooltipData(empire, null, technologyDefinition, technologyDefinition, state);
		}
	}

	public override void DumpAsText(StringBuilder content, string indent = "")
	{
		base.DumpAsText(content, indent);
		for (int i = 0; i < this.ResearchQueue.Length; i++)
		{
			content.AppendFormat("{0}{1} {2}\r\n", indent, i, this.ResearchQueue.PeekAt(i).ConstructibleElement.Name);
		}
	}

	public override byte[] DumpAsBytes()
	{
		MemoryStream memoryStream = new MemoryStream();
		using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
		{
			binaryWriter.Write(base.DumpAsBytes());
			for (int i = 0; i < this.ResearchQueue.Length; i++)
			{
				binaryWriter.Write(this.ResearchQueue.PeekAt(i).ConstructibleElement.Name);
			}
		}
		byte[] result = memoryStream.ToArray();
		memoryStream.Close();
		return result;
	}

	public override void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		base.ReadXml(reader);
		if (num >= 2)
		{
			this.CurrentTechnologyEraNumber = reader.ReadElementString<int>("CurrentTechnologyEraNumber");
		}
		reader.ReadStartElement("Queue");
		this.ResearchQueue = new ConstructionQueue();
		Amplitude.Xml.Serialization.IXmlSerializable researchQueue = this.ResearchQueue;
		reader.ReadElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>("Researches", ref researchQueue);
		for (int i = this.ResearchQueue.Length - 1; i >= 0; i--)
		{
			Construction construction = this.ResearchQueue.PeekAt(i);
			if (construction.ConstructibleElement == null)
			{
				Diagnostics.LogWarning("Compatibility issue, constructible element (name: '{0}') is null.", new object[]
				{
					construction.ConstructibleElementName
				});
				this.ResearchQueue.Remove(construction);
			}
		}
		reader.ReadEndElement("Queue");
		if (num >= 4)
		{
			reader.ReadStartElement("OrbUnlockQueue");
			this.OrbUnlockQueue = new ConstructionQueue();
			Amplitude.Xml.Serialization.IXmlSerializable orbUnlockQueue = this.OrbUnlockQueue;
			reader.ReadElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>("OrbUnlocks", ref orbUnlockQueue);
			for (int j = this.OrbUnlockQueue.Length - 1; j >= 0; j--)
			{
				Construction construction2 = this.OrbUnlockQueue.PeekAt(j);
				if (construction2.ConstructibleElement == null)
				{
					Diagnostics.LogWarning("Compatibility issue, constructible element (name: '{0}') is null.", new object[]
					{
						construction2.ConstructibleElementName
					});
					this.OrbUnlockQueue.Remove(construction2);
				}
			}
			reader.ReadEndElement("OrbUnlockQueue");
		}
		if (num >= 3)
		{
			int attribute = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("ResearchInProgress");
			for (int k = 0; k < attribute; k++)
			{
				Construction construction3 = new Construction();
				Amplitude.Xml.Serialization.IXmlSerializable xmlSerializable = construction3;
				reader.ReadElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>("Research", ref xmlSerializable);
				Diagnostics.Assert(this.researchInProgress != null);
				if (construction3 != null && construction3.ConstructibleElement != null)
				{
					this.researchInProgress.Add(construction3);
				}
				else if (construction3 != null)
				{
					Diagnostics.LogWarning("Compatibility issue, constructible element (name: '{0}') is null.", new object[]
					{
						construction3.ConstructibleElementName
					});
				}
			}
			reader.ReadEndElement("ResearchInProgress");
		}
		this.researchSimulationObjectWrapper.SimulationObject.RemoveAllDescriptors();
		reader.ReadElementSerializable<SimulationObjectWrapper>(ref this.researchSimulationObjectWrapper);
	}

	public override void WriteXml(XmlWriter writer)
	{
		int num = writer.WriteVersionAttribute(4);
		base.WriteXml(writer);
		writer.WriteElementString<int>("CurrentTechnologyEraNumber", this.CurrentTechnologyEraNumber);
		writer.WriteStartElement("Queue");
		Amplitude.Xml.Serialization.IXmlSerializable researchQueue = this.ResearchQueue;
		writer.WriteElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>("Researches", ref researchQueue);
		writer.WriteEndElement();
		writer.WriteStartElement("OrbUnlockQueue");
		Amplitude.Xml.Serialization.IXmlSerializable orbUnlockQueue = this.OrbUnlockQueue;
		writer.WriteElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>("OrbUnlocks", ref orbUnlockQueue);
		writer.WriteEndElement();
		writer.WriteStartElement("ResearchInProgress");
		writer.WriteAttributeString<int>("Count", this.researchInProgress.Count);
		for (int i = 0; i < this.researchInProgress.Count; i++)
		{
			Amplitude.Xml.Serialization.IXmlSerializable xmlSerializable = this.researchInProgress[i];
			writer.WriteElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>("Research", ref xmlSerializable);
		}
		writer.WriteEndElement();
		Amplitude.Xml.Serialization.IXmlSerializable xmlSerializable2 = this.researchSimulationObjectWrapper;
		writer.WriteElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>(ref xmlSerializable2);
	}

	private bool BuyOutTechnologyPreprocessor(OrderBuyOutTechnology order)
	{
		IGameService service = Services.GetService<IGameService>();
		global::Game game = service.Game as global::Game;
		if (order.TechnologyName == string.Empty)
		{
			return false;
		}
		DepartmentOfScience.ConstructibleElement constructibleElement;
		if (!this.technologyDatabase.TryGetValue(order.TechnologyName, out constructibleElement))
		{
			return false;
		}
		DepartmentOfScience.ConstructibleElement.State technologyState = this.GetTechnologyState(constructibleElement);
		if (technologyState == DepartmentOfScience.ConstructibleElement.State.Researched || technologyState == DepartmentOfScience.ConstructibleElement.State.NotAvailable)
		{
			Diagnostics.LogError("Order preprocessing failed because the technology {0} is not available. State = {1}", new object[]
			{
				order.TechnologyName,
				technologyState.ToString()
			});
			return false;
		}
		if (!constructibleElement.HasTechnologyFlag(DepartmentOfScience.ConstructibleElement.TechnologyFlag.OrbUnlock))
		{
			float num = -this.GetBuyOutTechnologyCost(constructibleElement);
			if (!this.departmentOfTheTreasury.IsTransferOfResourcePossible(game.Empires[order.EmpireIndex], DepartmentOfTheTreasury.Resources.TechnologiesBuyOut, ref num))
			{
				Diagnostics.LogWarning("Order preprocessing failed because we don't have enough resources.");
				return false;
			}
			order.Cost = (int)num;
		}
		else
		{
			for (int i = 0; i < constructibleElement.Costs.Length; i++)
			{
				if (constructibleElement.Costs[i] != null)
				{
					float num2 = -constructibleElement.Costs[i].GetValue(base.Empire.SimulationObject);
					if (num2 != 0f)
					{
						if (!this.departmentOfTheTreasury.IsTransferOfResourcePossible(game.Empires[order.EmpireIndex], constructibleElement.Costs[i].ResourceName, ref num2))
						{
							Diagnostics.LogWarning("Order preprocessing failed because we don't have enough resources.");
							return false;
						}
					}
				}
			}
		}
		return true;
	}

	private IEnumerator BuyOutTechnologyProcessor(OrderBuyOutTechnology order)
	{
		if (order.TechnologyName == string.Empty)
		{
			Diagnostics.LogWarning("Order preprocessing failed because the technology is not defined.");
			yield break;
		}
		IGameService gameService = Services.GetService<IGameService>();
		global::Game game = gameService.Game as global::Game;
		DepartmentOfScience.ConstructibleElement technology;
		if (!this.technologyDatabase.TryGetValue(order.TechnologyName, out technology))
		{
			yield break;
		}
		if (!technology.HasTechnologyFlag(DepartmentOfScience.ConstructibleElement.TechnologyFlag.OrbUnlock))
		{
			float cost = (float)order.Cost;
			if (!this.departmentOfTheTreasury.IsTransferOfResourcePossible(game.Empires[order.EmpireIndex], DepartmentOfTheTreasury.Resources.TechnologiesBuyOut, ref cost))
			{
				Diagnostics.LogWarning("Order preprocessing failed because we don't have enough resources.");
				yield break;
			}
			if (!this.departmentOfTheTreasury.TryTransferResources(game.Empires[order.EmpireIndex], DepartmentOfTheTreasury.Resources.TechnologiesBuyOut, cost))
			{
				Diagnostics.LogError("Order preprocessing failed because growth transfert failed.");
				yield break;
			}
		}
		else
		{
			for (int index = 0; index < technology.Costs.Length; index++)
			{
				if (technology.Costs[index] != null)
				{
					float cost2 = -technology.Costs[index].GetValue(base.Empire.SimulationObject);
					if (cost2 != 0f)
					{
						if (!this.departmentOfTheTreasury.IsTransferOfResourcePossible(game.Empires[order.EmpireIndex], technology.Costs[index].ResourceName, ref cost2))
						{
							Diagnostics.LogWarning("Order preprocessing failed because we don't have enough resources.");
							yield break;
						}
						if (!this.departmentOfTheTreasury.TryTransferResources(game.Empires[order.EmpireIndex], technology.Costs[index].ResourceName, cost2))
						{
							Diagnostics.LogError("Order preprocessing failed because OrbUnlock costs transfert failed.");
							yield break;
						}
					}
				}
			}
		}
		this.UnlockTechnology(technology, false);
		yield break;
	}

	private bool CancelResearchPreprocessor(OrderCancelResearch order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		Diagnostics.Assert(this.gameEntityRepositoryService != null);
		IGameEntity gameEntity;
		if (!this.gameEntityRepositoryService.TryGetValue(order.ConstructionGameEntityGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid (GUID: {0}).", new object[]
			{
				order.ConstructionGameEntityGUID
			});
			return false;
		}
		Construction construction = gameEntity as Construction;
		if (construction == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a Construction.");
			return false;
		}
		ConstructionQueue constructionQueue = this.ResearchQueue;
		TechnologyDefinition technologyDefinition = construction.ConstructibleElement as TechnologyDefinition;
		if (technologyDefinition != null && technologyDefinition.HasTechnologyFlag(DepartmentOfScience.ConstructibleElement.TechnologyFlag.OrbUnlock))
		{
			constructionQueue = this.OrbUnlockQueue;
		}
		Diagnostics.Assert(constructionQueue != null);
		if (!constructionQueue.Contains(construction))
		{
			Diagnostics.LogError("Order preprocessing failed because the context construction queue does not contains the construction.");
			return false;
		}
		return true;
	}

	private IEnumerator CancelResearchProcessor(OrderCancelResearch order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		Diagnostics.Assert(this.gameEntityRepositoryService != null);
		IGameEntity gameEntity;
		if (!this.gameEntityRepositoryService.TryGetValue(order.ConstructionGameEntityGUID, out gameEntity))
		{
			Diagnostics.LogError("Skipping cancel construction because the target game entity is not valid.");
			yield break;
		}
		Construction construction = gameEntity as Construction;
		if (construction == null)
		{
			Diagnostics.LogError("Skipping cancel construction because the target game entity is not a Construction.");
			yield break;
		}
		Diagnostics.Assert(base.Empire != null);
		Diagnostics.Assert(this.departmentOfTheTreasury != null);
		Diagnostics.Assert(construction.ConstructibleElement != null);
		if (construction.ConstructibleElement.Costs != null && construction.ConstructibleElement.Costs.Length > 0)
		{
			Diagnostics.Assert(construction.CurrentConstructionStock != null && construction.CurrentConstructionStock.Length == construction.ConstructibleElement.Costs.Length);
			for (int index = 0; index < construction.CurrentConstructionStock.Length; index++)
			{
				Diagnostics.Assert(construction.ConstructibleElement.Costs[index] != null);
				Diagnostics.Assert(construction.CurrentConstructionStock[index] != null);
				if (construction.CurrentConstructionStock[index].Stock > 0f && construction.ConstructibleElement.Costs[index].Instant && !this.departmentOfTheTreasury.TryTransferResources(base.Empire.SimulationObject, construction.CurrentConstructionStock[index].PropertyName, construction.CurrentConstructionStock[index].Stock))
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
		ConstructionQueue selectedQueue = this.ResearchQueue;
		TechnologyDefinition technologyDefinition = construction.ConstructibleElement as TechnologyDefinition;
		if (technologyDefinition != null && technologyDefinition.HasTechnologyFlag(DepartmentOfScience.ConstructibleElement.TechnologyFlag.OrbUnlock))
		{
			selectedQueue = this.OrbUnlockQueue;
		}
		Diagnostics.Assert(selectedQueue != null);
		selectedQueue.Remove(construction);
		this.OnResearchQueueChanged(construction, ConstructionChangeEventAction.Cancelled);
		yield break;
	}

	private bool ForceUnlockTechnologyPreprocessor(OrderForceUnlockTechnology order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		Diagnostics.Assert(this.technologyDatabase != null);
		for (int i = 0; i < order.TechnologyDefinitionNames.Length; i++)
		{
			DepartmentOfScience.ConstructibleElement constructibleElement;
			if (!this.technologyDatabase.TryGetValue(order.TechnologyDefinitionNames[i], out constructibleElement))
			{
				Diagnostics.LogError("Order preprocessing failed because the technology {0} is not in the technology database.", new object[]
				{
					order.TechnologyDefinitionNames[i]
				});
				return false;
			}
		}
		return true;
	}

	private IEnumerator ForceUnlockTechnologyProcessor(OrderForceUnlockTechnology order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		bool silent = order.TechnologyDefinitionNames.Length > 0;
		for (int index = 0; index < order.TechnologyDefinitionNames.Length; index++)
		{
			this.UnlockTechnology(order.TechnologyDefinitionNames[index], silent);
		}
		yield break;
	}

	private bool QueueResearchPreprocessor(OrderQueueResearch order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		Diagnostics.Assert(this.technologyDatabase != null);
		DepartmentOfScience.ConstructibleElement constructibleElement;
		if (!this.technologyDatabase.TryGetValue(order.ConstructibleElementName, out constructibleElement))
		{
			Diagnostics.LogError("Order preprocessing failed because the technology {0} is not in the technology database.", new object[]
			{
				order.ConstructibleElementName
			});
			return false;
		}
		DepartmentOfScience.ConstructibleElement.State technologyState = this.GetTechnologyState(constructibleElement);
		if (technologyState != DepartmentOfScience.ConstructibleElement.State.Available)
		{
			Diagnostics.LogError("Order preprocessing failed because the technology {0} is not available. State = {1}", new object[]
			{
				order.ConstructibleElementName,
				technologyState.ToString()
			});
			return false;
		}
		Construction construction = this.researchInProgress.Find((Construction match) => match.ConstructibleElement.Name == constructibleElement.Name);
		Diagnostics.Assert(constructibleElement != null);
		if (constructibleElement.Costs == null || constructibleElement.Costs.Length == 0)
		{
			order.ResourceStocks = new ConstructionResourceStock[0];
		}
		else
		{
			order.ResourceStocks = new ConstructionResourceStock[constructibleElement.Costs.Length];
			for (int i = 0; i < constructibleElement.Costs.Length; i++)
			{
				Diagnostics.Assert(constructibleElement.Costs[i] != null);
				order.ResourceStocks[i] = new ConstructionResourceStock(constructibleElement.Costs[i].ResourceName, base.Empire);
				if (construction != null)
				{
					Diagnostics.Assert(construction.CurrentConstructionStock != null && construction.CurrentConstructionStock.Length == constructibleElement.Costs.Length && construction.CurrentConstructionStock[i] != null);
					order.ResourceStocks[i].Stock = construction.CurrentConstructionStock[i].Stock;
				}
				if (constructibleElement.Costs[i].Instant)
				{
					Diagnostics.Assert(base.Empire != null);
					float productionCostWithBonus = DepartmentOfTheTreasury.GetProductionCostWithBonus(base.Empire.SimulationObject, constructibleElement, constructibleElement.Costs[i], false);
					Diagnostics.Assert(this.departmentOfTheTreasury != null);
					float num;
					if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.Empire.SimulationObject, constructibleElement.Costs[i].ResourceName, out num, false))
					{
						Diagnostics.LogError("Order preprocessing failed because the constructible element '{0}' ask for instant resource '{1}' that can't be retrieve.", new object[]
						{
							constructibleElement.Name,
							constructibleElement.Costs[i].ResourceName
						});
					}
					else
					{
						if (productionCostWithBonus > num)
						{
							Diagnostics.LogError("Order preprocessing failed because the constructible element {0} ask for '{1}' '{2}' and the resource stock has only '{3}'.", new object[]
							{
								order.ConstructibleElementName,
								productionCostWithBonus,
								constructibleElement.Costs[i].ResourceName,
								num
							});
							return false;
						}
						order.ResourceStocks[i].Stock = productionCostWithBonus;
					}
				}
			}
		}
		if (construction == null)
		{
			Diagnostics.Assert(this.gameEntityRepositoryService != null);
			order.ConstructionGameEntityGUID = this.gameEntityRepositoryService.GenerateGUID();
		}
		else
		{
			order.ConstructionGameEntityGUID = construction.GUID;
			if (!this.gameEntityRepositoryService.Contains(construction.GUID))
			{
				Diagnostics.LogError("Order preprocessing failed because the game entity {0} is not in game entity repository service.", new object[]
				{
					construction.GUID
				});
				return false;
			}
		}
		return true;
	}

	private IEnumerator QueueResearchProcessor(OrderQueueResearch order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (order.ConstructionGameEntityGUID == GameEntityGUID.Zero)
		{
			Diagnostics.LogError("Skipping queue construction process because the game entity guid is null.");
			yield break;
		}
		Diagnostics.Assert(this.technologyDatabase != null);
		DepartmentOfScience.ConstructibleElement constructibleElement;
		if (!this.technologyDatabase.TryGetValue(order.ConstructibleElementName, out constructibleElement))
		{
			Diagnostics.LogError("Skipping queue construction process because the constructible element {0} is not in the constructible element database.", new object[]
			{
				order.ConstructibleElementName
			});
			yield break;
		}
		Diagnostics.Assert(constructibleElement != null);
		global::Empire empire = base.Empire as global::Empire;
		DepartmentOfScience departmentOfScience = empire.GetAgency<DepartmentOfScience>();
		DepartmentOfScience.ConstructibleElement.State technologyState = departmentOfScience.GetTechnologyState(constructibleElement);
		if (technologyState != DepartmentOfScience.ConstructibleElement.State.Available)
		{
			Diagnostics.LogError("Skipping queue construction process because the constructible element {0} is not available ({1}).", new object[]
			{
				order.ConstructibleElementName,
				technologyState
			});
			yield break;
		}
		Diagnostics.Assert(this.researchInProgress != null);
		Construction construction = this.researchInProgress.Find((Construction match) => match.GUID == order.ConstructionGameEntityGUID);
		if (construction == null)
		{
			Diagnostics.Assert(empire != null && empire.Faction != null && empire.Faction.AffinityMapping != null);
			construction = new Construction(constructibleElement, order.ConstructionGameEntityGUID, empire.Faction.AffinityMapping.Name, empire);
			IDatabase<SimulationDescriptor> simulationDescriptorDatatable = Databases.GetDatabase<SimulationDescriptor>(false);
			SimulationDescriptor classImprovementDescriptor;
			if (simulationDescriptorDatatable != null && simulationDescriptorDatatable.TryGetValue("ClassConstruction", out classImprovementDescriptor))
			{
				construction.AddDescriptor(classImprovementDescriptor, false);
			}
		}
		Diagnostics.Assert(this.departmentOfTheTreasury != null);
		if (constructibleElement.Costs != null && constructibleElement.Costs.Length > 0)
		{
			Diagnostics.Assert(construction.CurrentConstructionStock != null && construction.CurrentConstructionStock.Length == constructibleElement.Costs.Length);
			Diagnostics.Assert(order.ResourceStocks != null && order.ResourceStocks.Length == constructibleElement.Costs.Length);
			for (int index = 0; index < constructibleElement.Costs.Length; index++)
			{
				Diagnostics.Assert(constructibleElement.Costs[index] != null);
				Diagnostics.Assert(construction.CurrentConstructionStock[index] != null);
				Diagnostics.Assert(order.ResourceStocks[index] != null);
				construction.CurrentConstructionStock[index].Stock = order.ResourceStocks[index].Stock;
				if (constructibleElement.Costs[index].Instant)
				{
					if (order.ResourceStocks[index].Stock > 0f && !this.departmentOfTheTreasury.TryTransferResources(base.Empire.SimulationObject, constructibleElement.Costs[index].ResourceName, -order.ResourceStocks[index].Stock))
					{
						Diagnostics.LogError("Order preprocessing failed because the constructible element '{0}' ask for instant resource '{1}' that can't be retrieve.", new object[]
						{
							constructibleElement.Name,
							constructibleElement.Costs[index].ResourceName
						});
						yield break;
					}
				}
			}
		}
		ConstructionQueue selectedQueue = this.ResearchQueue;
		TechnologyDefinition technologyDefinition = construction.ConstructibleElement as TechnologyDefinition;
		if (technologyDefinition != null && technologyDefinition.HasTechnologyFlag(DepartmentOfScience.ConstructibleElement.TechnologyFlag.OrbUnlock))
		{
			selectedQueue = this.OrbUnlockQueue;
		}
		Diagnostics.Assert(selectedQueue != null);
		selectedQueue.Enqueue(construction);
		Diagnostics.Assert(this.researchInProgress != null);
		if (!this.researchInProgress.Any((Construction match) => match.GUID == construction.GUID))
		{
			this.researchInProgress.Add(construction);
			Diagnostics.Assert(this.gameEntityRepositoryService != null);
			this.gameEntityRepositoryService.Register(construction);
		}
		else if (!this.gameEntityRepositoryService.Contains(construction.GUID))
		{
			Diagnostics.LogError("Game entity {0} is not in game entity repository service.", new object[]
			{
				construction.GUID
			});
		}
		this.OnResearchQueueChanged(construction, ConstructionChangeEventAction.Started);
		yield break;
	}

	private bool UnlockTechnologyByInfiltrationPreprocessor(OrderUnlockTechnologyByInfiltration order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		bool? flag = new bool?(true);
		InfiltrationAction.ComputeConstructionCost((global::Empire)base.Empire, order, ref flag);
		if (!flag.Value)
		{
			return false;
		}
		Diagnostics.Assert(this.technologyDatabase != null);
		for (int i = 0; i < order.TechnologyDefinitionNames.Length; i++)
		{
			DepartmentOfScience.ConstructibleElement constructibleElement;
			if (!this.technologyDatabase.TryGetValue(order.TechnologyDefinitionNames[i], out constructibleElement))
			{
				Diagnostics.LogError("Order preprocessing failed because the technology {0} is not in the technology database.", new object[]
				{
					order.TechnologyDefinitionNames[i]
				});
				return false;
			}
		}
		IGameEntity gameEntity;
		if (!this.gameEntityRepositoryService.TryGetValue(order.InfiltratedCityGUID, out gameEntity))
		{
			return false;
		}
		float antiSpyParameter;
		order.AntiSpyResult = DepartmentOfIntelligence.GetAntiSpyResult(gameEntity as City, out antiSpyParameter);
		order.AntiSpyParameter = antiSpyParameter;
		return true;
	}

	private IEnumerator UnlockTechnologyByInfiltrationProcessor(OrderUnlockTechnologyByInfiltration order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		InfiltrationAction.TryTransferResources(base.Empire as global::Empire, order);
		InfiltrationAction.TryTransferLevelCost(base.Empire as global::Empire, order);
		InfiltrationAction.ApplyExperienceReward(base.Empire as global::Empire, order);
		for (int index = 0; index < order.TechnologyDefinitionNames.Length; index++)
		{
			this.UnlockTechnology(order.TechnologyDefinitionNames[index], false);
		}
		IGameEntity gameEntity = null;
		if (!this.gameEntityRepositoryService.TryGetValue(order.InfiltratedCityGUID, out gameEntity))
		{
			throw new ArgumentNullException();
		}
		City infiltratedCity = gameEntity as City;
		if (infiltratedCity == null)
		{
			throw new ArgumentNullException();
		}
		InfiltrationAction.TryNotifyInfiltrationActionResult((global::Empire)base.Empire, (global::Empire)base.Empire, order);
		InfiltrationAction.TryNotifyInfiltrationActionResult(infiltratedCity.Empire, (global::Empire)base.Empire, order);
		DepartmentOfIntelligence departmentOfIntelligence = base.Empire.GetAgency<DepartmentOfIntelligence>();
		if (departmentOfIntelligence != null)
		{
			departmentOfIntelligence.ExecuteAntiSpy(order.InfiltratedCityGUID, order.AntiSpyResult, order.AntiSpyParameter, false);
		}
		EventTechnologyStealed eventTechnologyStealed = new EventTechnologyStealed(base.Empire);
		this.eventService.Notify(eventTechnologyStealed);
		yield break;
	}

	public StaticString ArcheologyTechnologyDefinitionName { get; private set; }

	public bool AllResearchCompleted { get; private set; }

	public int CurrentTechnologyEraNumber { get; private set; }

	public ConstructionQueue ResearchQueue { get; private set; }

	public ConstructionQueue OrbUnlockQueue { get; private set; }

	public SimulationObjectWrapper ResearchSimulationObjectWrapper
	{
		get
		{
			return this.researchSimulationObjectWrapper;
		}
	}

	public IDatabase<DepartmentOfScience.ConstructibleElement> TechnologyDatabase
	{
		get
		{
			return this.technologyDatabase;
		}
	}

	public static bool CanBuyoutResearch(global::Empire empire)
	{
		return empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitReplicants1);
	}

	public DepartmentOfScience.ConstructibleElement.State GetTechnologyState(string technologyName)
	{
		DepartmentOfScience.ConstructibleElement technology;
		if (this.technologyDatabase.TryGetValue(technologyName, out technology))
		{
			return this.GetTechnologyState(technology);
		}
		return DepartmentOfScience.ConstructibleElement.State.NotAvailable;
	}

	public DepartmentOfScience.ConstructibleElement.State GetTechnologyState(DepartmentOfScience.ConstructibleElement technology)
	{
		if (technology == null)
		{
			throw new ArgumentNullException("technology");
		}
		ConstructionQueue constructionQueue = this.ResearchQueue;
		TechnologyDefinition technologyDefinition = technology as TechnologyDefinition;
		if (technologyDefinition != null && technologyDefinition.HasTechnologyFlag(DepartmentOfScience.ConstructibleElement.TechnologyFlag.OrbUnlock))
		{
			constructionQueue = this.OrbUnlockQueue;
		}
		if (technology.IsResearched(this.researchSimulationObjectWrapper))
		{
			return DepartmentOfScience.ConstructibleElement.State.Researched;
		}
		Diagnostics.Assert(constructionQueue != null);
		Construction construction = constructionQueue.Peek();
		if (construction != null && construction.ConstructibleElement.Name == technology.Name)
		{
			return DepartmentOfScience.ConstructibleElement.State.InProgress;
		}
		if (constructionQueue.Contains(technology))
		{
			return DepartmentOfScience.ConstructibleElement.State.Queued;
		}
		Diagnostics.Assert(this.departmentOfTheTreasury != null);
		if (!DepartmentOfTheTreasury.CheckConstructiblePrerequisites(base.Empire, technology, new string[]
		{
			ConstructionFlags.Prerequisite
		}))
		{
			return DepartmentOfScience.ConstructibleElement.State.NotAvailable;
		}
		return DepartmentOfScience.ConstructibleElement.State.Available;
	}

	public void UnlockTechnology(string technologyName, bool silent = false)
	{
		DepartmentOfScience.ConstructibleElement technology;
		if (!string.IsNullOrEmpty(technologyName) && this.technologyDatabase.TryGetValue(technologyName, out technology))
		{
			this.UnlockTechnology(technology, silent);
		}
	}

	public void UnlockTechnology(DepartmentOfScience.ConstructibleElement technology, bool silent = false)
	{
		if (technology == null)
		{
			throw new ArgumentNullException("technology");
		}
		if (this.GetTechnologyState(technology) == DepartmentOfScience.ConstructibleElement.State.Researched)
		{
			return;
		}
		SimulationDescriptorReference[] simulationDescriptorReferences = technology.SimulationDescriptorReferences;
		if (simulationDescriptorReferences == null)
		{
			return;
		}
		ConstructionQueue constructionQueue = this.ResearchQueue;
		TechnologyDefinition technologyDefinition = technology as TechnologyDefinition;
		if (technologyDefinition != null && technologyDefinition.HasTechnologyFlag(DepartmentOfScience.ConstructibleElement.TechnologyFlag.OrbUnlock))
		{
			constructionQueue = this.OrbUnlockQueue;
		}
		Diagnostics.Assert(this.simulationDescriptorsDatatable != null);
		foreach (SimulationDescriptorReference reference in simulationDescriptorReferences)
		{
			SimulationDescriptor descriptor;
			if (this.simulationDescriptorsDatatable.TryGetValue(reference, out descriptor))
			{
				Diagnostics.Assert(this.researchSimulationObjectWrapper != null);
				this.researchSimulationObjectWrapper.AddDescriptor(descriptor, false);
			}
		}
		if (technology is TechnologyEraDefinition)
		{
			this.CurrentTechnologyEraNumber = Math.Max(this.CurrentTechnologyEraNumber, (technology as TechnologyEraDefinition).TechnologyEraNumber);
		}
		if (constructionQueue.Contains(technology))
		{
			constructionQueue.Remove(technology);
		}
		Diagnostics.Assert(this.researchInProgress != null);
		this.researchInProgress.RemoveAll((Construction match) => match.ConstructibleElement.Name == technology.Name);
		Diagnostics.Assert(base.Empire != null);
		base.Empire.Refresh(false);
		this.OnTechnologyUnlocked(technology, silent);
		this.CheckErasUnlock(false);
	}

	protected override IEnumerator OnInitialize()
	{
		yield return base.OnInitialize();
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		this.eventService = Services.GetService<IEventService>();
		Diagnostics.Assert(this.eventService != null);
		this.endTurnService = Services.GetService<IEndTurnService>();
		Diagnostics.Assert(this.endTurnService != null);
		this.endTurnService.RegisterValidator(new Func<bool, bool>(this.EndTurnValidator));
		this.gameEntityRepositoryService = gameService.Game.Services.GetService<IGameEntityRepositoryService>();
		if (this.gameEntityRepositoryService == null)
		{
			Diagnostics.LogError("Failed to retrieve the game entity repository service.");
		}
		this.playerControllerRepositoryService = gameService.Game.Services.GetService<IPlayerControllerRepositoryService>();
		if (this.playerControllerRepositoryService == null)
		{
			Diagnostics.LogError("Failed to retrieve the player controller repository service.");
		}
		this.ArcheologyTechnologyDefinitionName = Amplitude.Unity.Runtime.Runtime.Registry.GetValue("Gameplay/Ancillaries/Science/ArcheologyTechnologyDefinitionName");
		if (StaticString.IsNullOrEmpty(this.ArcheologyTechnologyDefinitionName))
		{
			Diagnostics.LogWarning("ArcheologyTechnologyDefinitionName is null or empty in the registry.");
		}
		this.ResearchQueue = new ConstructionQueue();
		this.OrbUnlockQueue = new ConstructionQueue();
		Diagnostics.Assert(base.Empire != null);
		base.Empire.RegisterPass("GameClientState_Turn_End", "ComputeResearch", new Agency.Action(this.GameClientState_Turn_End_ComputeResearch), new string[]
		{
			"CollectResources"
		});
		base.Empire.RegisterPass("GameClientState_Turn_End", "ComputeUnlockResearches", new Agency.Action(this.GameClientState_Turn_End_ComputeUnlockResearches), new string[]
		{
			"ComputeResearch"
		});
		this.technologyDatabase = Databases.GetDatabase<DepartmentOfScience.ConstructibleElement>(true);
		this.simulationDescriptorsDatatable = Databases.GetDatabase<SimulationDescriptor>(true);
		this.departmentOfTheTreasury = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		Diagnostics.Assert(this.departmentOfTheTreasury != null);
		this.departmentOfForeignAffairs = base.Empire.GetAgency<DepartmentOfForeignAffairs>();
		Diagnostics.Assert(this.departmentOfForeignAffairs != null);
		this.departmentOfIndustry = base.Empire.GetAgency<DepartmentOfIndustry>();
		Diagnostics.Assert(this.departmentOfIndustry != null);
		this.researchSimulationObjectWrapper = new SimulationObjectWrapper("Research");
		SimulationDescriptor researchClassDescriptor;
		if (this.simulationDescriptorsDatatable.TryGetValue("ClassResearch", out researchClassDescriptor))
		{
			this.researchSimulationObjectWrapper.AddDescriptor(researchClassDescriptor, false);
		}
		else
		{
			Diagnostics.LogError("Cannot find the 'ClassResearch' simulation object descriptor.");
		}
		base.Empire.AddChild(this.researchSimulationObjectWrapper);
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		DepartmentOfScience.ConstructibleElement.InitializeUnlocksByTechnology();
		Diagnostics.Assert(this.technologyEras != null);
		DepartmentOfScience.ConstructibleElement[] technologyDefinitions = this.technologyDatabase.GetValues();
		if (technologyDefinitions != null)
		{
			foreach (DepartmentOfScience.ConstructibleElement constructibleElement in technologyDefinitions)
			{
				TechnologyEraDefinition technologyEraDefinition = constructibleElement as TechnologyEraDefinition;
				if (technologyEraDefinition != null)
				{
					this.technologyEras.Add(technologyEraDefinition);
				}
			}
		}
		this.CheckErasUnlock(true);
		yield break;
	}

	protected override IEnumerator OnLoadGame(Amplitude.Unity.Game.Game game)
	{
		yield return base.OnLoadGame(game);
		DepartmentOfScience.ConstructibleElement.ReleaseUnlocksByTechnology();
		this.game = (game as global::Game);
		for (int index = 0; index < this.ResearchQueue.Length; index++)
		{
			Construction construction = this.ResearchQueue.PeekAt(index);
			if (!this.researchInProgress.Any((Construction match) => match.GUID == construction.GUID))
			{
				Diagnostics.LogWarning("The research in progress list does not contains the construction {0}.", new object[]
				{
					construction
				});
				this.gameEntityRepositoryService.Register(construction);
			}
		}
		for (int index2 = 0; index2 < this.researchInProgress.Count; index2++)
		{
			Construction construction2 = this.researchInProgress[index2];
			if (!this.gameEntityRepositoryService.Contains(construction2.GUID))
			{
				this.gameEntityRepositoryService.Register(construction2);
			}
			else
			{
				Construction repositoryConstruction = this.gameEntityRepositoryService[construction2.GUID] as Construction;
				if (repositoryConstruction == null || repositoryConstruction.ConstructibleElement == null || repositoryConstruction.ConstructibleElement.Name != construction2.ConstructibleElement.Name)
				{
					Diagnostics.LogError("The game entity {0} in already present in the game entity repository and is different from the wanted one.", new object[]
					{
						construction2.GUID
					});
				}
				else
				{
					Diagnostics.LogWarning("The game entity {0} in already present in the game entity repository.", new object[]
					{
						construction2.GUID
					});
				}
			}
		}
		if (!this.AllResearchCompleted)
		{
			this.AllResearchCompleted = true;
			foreach (DepartmentOfScience.ConstructibleElement constructibleElement in this.technologyDatabase.GetValues())
			{
				TechnologyDefinition technologyDefinition = constructibleElement as TechnologyDefinition;
				if (technologyDefinition != null)
				{
					if (technologyDefinition.Visibility == TechnologyDefinitionVisibility.Visible)
					{
						if (DepartmentOfTheTreasury.CheckConstructiblePrerequisites(base.Empire, constructibleElement, new string[0]))
						{
							DepartmentOfScience.ConstructibleElement.State state = this.GetTechnologyState(technologyDefinition);
							if (state == DepartmentOfScience.ConstructibleElement.State.Available || state == DepartmentOfScience.ConstructibleElement.State.InProgress || state == DepartmentOfScience.ConstructibleElement.State.Queued)
							{
								this.AllResearchCompleted = false;
								break;
							}
						}
					}
				}
			}
		}
		yield break;
	}

	protected override void OnRelease()
	{
		base.OnRelease();
		DepartmentOfScience.ConstructibleElement.ReleaseUnlocksByTechnology();
		this.technologyEras.Clear();
		this.departmentOfTheTreasury = null;
		this.departmentOfForeignAffairs = null;
		this.gameEntityRepositoryService = null;
		this.playerControllerRepositoryService = null;
		this.eventService = null;
		if (this.researchSimulationObjectWrapper != null)
		{
			this.researchSimulationObjectWrapper.Dispose();
			this.researchSimulationObjectWrapper = null;
		}
		this.simulationDescriptorsDatatable = null;
		this.technologyDatabase = null;
		if (this.ResearchQueue != null)
		{
			this.ResearchQueue.Dispose();
			this.ResearchQueue = null;
		}
		if (this.OrbUnlockQueue != null)
		{
			this.OrbUnlockQueue.Dispose();
			this.OrbUnlockQueue = null;
		}
		if (this.endTurnService != null)
		{
			this.endTurnService.UnregisterValidator(new Func<bool, bool>(this.EndTurnValidator));
			this.endTurnService = null;
		}
	}

	private void CheckErasUnlock(bool silent = false)
	{
		Diagnostics.Assert(this.technologyEras != null);
		for (int i = 0; i < this.technologyEras.Count; i++)
		{
			TechnologyEraDefinition technology = this.technologyEras[i];
			DepartmentOfScience.ConstructibleElement.State technologyState = this.GetTechnologyState(technology);
			if (technologyState == DepartmentOfScience.ConstructibleElement.State.Available)
			{
				this.UnlockTechnology(technology, silent);
			}
		}
	}

	private bool EndTurnValidator(bool force)
	{
		if (this.playerControllerRepositoryService.ActivePlayerController.Empire != base.Empire || force || this.ResearchQueue.Length != 0)
		{
			return true;
		}
		DepartmentOfTheInterior agency = base.Empire.GetAgency<DepartmentOfTheInterior>();
		if (agency != null && agency.Cities.Count == 0)
		{
			return true;
		}
		if (this.AllResearchCompleted)
		{
			return true;
		}
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		if (service != null && service.IsShared(DownloadableContent11.ReadOnlyName) && base.Empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitReplicants1))
		{
			return true;
		}
		this.eventService.Notify(new EventTechnologyNeeded(base.Empire));
		return false;
	}

	private IEnumerator GameClientState_Turn_End_ComputeResearch(string context, string name)
	{
		Diagnostics.Assert(this.ResearchQueue != null);
		ReadOnlyCollection<Construction> pendingResearches = this.ResearchQueue.PendingConstructions;
		Diagnostics.Assert(pendingResearches != null);
		for (int pendingConstructionIndex = 0; pendingConstructionIndex < pendingResearches.Count; pendingConstructionIndex++)
		{
			Construction pendingResearch = pendingResearches[pendingConstructionIndex];
			if (pendingResearch == null)
			{
				Diagnostics.LogError("A pending research is null.");
			}
			else if (pendingResearch.ConstructibleElement == null)
			{
				Diagnostics.LogError("A pending researched technology is null.");
			}
			else if (pendingResearch.ConstructibleElement.Costs != null && pendingResearch.ConstructibleElement.Costs.Length != 0)
			{
				bool constructionComplete = true;
				for (int index = 0; index < pendingResearch.ConstructibleElement.Costs.Length; index++)
				{
					Diagnostics.Assert(pendingResearch.ConstructibleElement.Costs[index] != null);
					if (!pendingResearch.ConstructibleElement.Costs[index].Instant && !pendingResearch.ConstructibleElement.Costs[index].InstantOnCompletion)
					{
						Diagnostics.Assert(pendingResearch.CurrentConstructionStock != null);
						ConstructionResourceStock constructionResourceStock = pendingResearch.CurrentConstructionStock[index];
						StaticString resourceName = pendingResearch.ConstructibleElement.Costs[index].ResourceName;
						Diagnostics.Assert(constructionResourceStock != null);
						float accumulatedStock = constructionResourceStock.Stock;
						float constructibleCost = DepartmentOfTheTreasury.GetProductionCostWithBonus(base.Empire.SimulationObject, pendingResearch.ConstructibleElement, pendingResearch.ConstructibleElement.Costs[index], false);
						float remainingCost = constructibleCost - accumulatedStock;
						if (remainingCost > 0f)
						{
							Diagnostics.Assert(base.Empire != null);
							Diagnostics.Assert(this.departmentOfTheTreasury != null);
							float resourceStock;
							if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.Empire.SimulationObject, resourceName, out resourceStock, false))
							{
								Diagnostics.LogError("Can't get resource stock value {0} on simulation object {1}.", new object[]
								{
									resourceName,
									base.Empire.SimulationObject.Name
								});
							}
							else
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
										pendingResearch.ConstructibleElement.Costs[index].ResourceName
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

	private IEnumerator GameClientState_Turn_End_ComputeUnlockResearches(string context, string name)
	{
		Diagnostics.Assert(this.ResearchQueue != null);
		while (this.ResearchQueue.Length > 0)
		{
			Construction pendingResearch = this.ResearchQueue.Peek();
			Diagnostics.Assert(pendingResearch != null);
			DepartmentOfScience.ConstructibleElement constructibleElement = pendingResearch.ConstructibleElement as DepartmentOfScience.ConstructibleElement;
			Diagnostics.Assert(constructibleElement != null);
			if (!DepartmentOfTheTreasury.CheckConstructiblePrerequisites(base.Empire, constructibleElement, new string[]
			{
				ConstructionFlags.Prerequisite
			}))
			{
				break;
			}
			bool constructionFinished = true;
			if (pendingResearch.ConstructibleElement.Costs != null && pendingResearch.ConstructibleElement.Costs.Length > 0)
			{
				for (int costIndex = 0; costIndex < pendingResearch.ConstructibleElement.Costs.Length; costIndex++)
				{
					Diagnostics.Assert(pendingResearch.ConstructibleElement.Costs[costIndex] != null);
					if (!pendingResearch.ConstructibleElement.Costs[costIndex].Instant && !pendingResearch.ConstructibleElement.Costs[costIndex].InstantOnCompletion)
					{
						Diagnostics.Assert(pendingResearch.CurrentConstructionStock != null);
						ConstructionResourceStock constructionResourceStock = pendingResearch.CurrentConstructionStock[costIndex];
						Diagnostics.Assert(constructionResourceStock != null);
						float constructibleCost = DepartmentOfTheTreasury.GetProductionCostWithBonus(base.Empire.SimulationObject, pendingResearch.ConstructibleElement, pendingResearch.ConstructibleElement.Costs[costIndex], false);
						constructionFinished &= (constructibleCost <= constructionResourceStock.Stock);
					}
				}
			}
			if (!constructionFinished)
			{
				break;
			}
			bool haveEnoughResources = true;
			for (int costIndex2 = 0; costIndex2 < pendingResearch.ConstructibleElement.Costs.Length; costIndex2++)
			{
				if (pendingResearch.ConstructibleElement.Costs[costIndex2].InstantOnCompletion)
				{
					IConstructionCost currentCost = pendingResearch.ConstructibleElement.Costs[costIndex2];
					float resourceCost = -DepartmentOfTheTreasury.GetProductionCostWithBonus(base.Empire.SimulationObject, pendingResearch.ConstructibleElement, currentCost, false);
					if (!this.departmentOfTheTreasury.IsTransferOfResourcePossible(base.Empire, currentCost.ResourceName, ref resourceCost))
					{
						haveEnoughResources = false;
						break;
					}
				}
			}
			if (!haveEnoughResources)
			{
				break;
			}
			for (int costIndex3 = 0; costIndex3 < pendingResearch.ConstructibleElement.Costs.Length; costIndex3++)
			{
				if (pendingResearch.ConstructibleElement.Costs[costIndex3].InstantOnCompletion)
				{
					IConstructionCost currentCost2 = pendingResearch.ConstructibleElement.Costs[costIndex3];
					float resourceCost2 = -DepartmentOfTheTreasury.GetProductionCostWithBonus(base.Empire.SimulationObject, pendingResearch.ConstructibleElement, currentCost2, false);
					this.departmentOfTheTreasury.TryTransferResources(base.Empire, currentCost2.ResourceName, resourceCost2);
				}
			}
			this.ResearchQueue.Dequeue();
			Diagnostics.Assert(this.gameEntityRepositoryService != null);
			this.gameEntityRepositoryService.Unregister(pendingResearch);
			Diagnostics.Assert(base.Empire != null);
			base.Empire.Refresh(false);
			this.OnResearchQueueChanged(pendingResearch, ConstructionChangeEventAction.Completed);
		}
		if (!this.AllResearchCompleted)
		{
			this.AllResearchCompleted = true;
			foreach (DepartmentOfScience.ConstructibleElement constructibleElement2 in this.technologyDatabase.GetValues())
			{
				TechnologyDefinition technologyDefinition = constructibleElement2 as TechnologyDefinition;
				if (technologyDefinition != null)
				{
					if (technologyDefinition.Visibility == TechnologyDefinitionVisibility.Visible)
					{
						if (DepartmentOfTheTreasury.CheckConstructiblePrerequisites(base.Empire, constructibleElement2, new string[0]))
						{
							DepartmentOfScience.ConstructibleElement.State state = this.GetTechnologyState(technologyDefinition);
							if (state == DepartmentOfScience.ConstructibleElement.State.Available || state == DepartmentOfScience.ConstructibleElement.State.InProgress || state == DepartmentOfScience.ConstructibleElement.State.Queued)
							{
								this.AllResearchCompleted = false;
								break;
							}
						}
					}
				}
			}
		}
		yield break;
	}

	private void OnResearchQueueChanged(Construction contruction, ConstructionChangeEventAction action)
	{
		if (contruction == null)
		{
			throw new ArgumentNullException("contruction");
		}
		if (action == ConstructionChangeEventAction.Completed)
		{
			this.UnlockTechnology((DepartmentOfScience.ConstructibleElement)contruction.ConstructibleElement, false);
		}
		if (this.ResearchQueueChanged != null)
		{
			this.ResearchQueueChanged(this, new ConstructionChangeEventArgs(action, null, contruction));
		}
	}

	private void OnTechnologyUnlocked(DepartmentOfScience.ConstructibleElement technology, bool silent = false)
	{
		if (technology == null)
		{
			throw new ArgumentNullException("technology");
		}
		Diagnostics.Assert(this.eventService != null);
		if (technology is TechnologyEraDefinition && !silent)
		{
			TechnologyEraDefinition technologyEraDefinition = technology as TechnologyEraDefinition;
			if (technologyEraDefinition.TechnologyEraNumber > 1)
			{
				this.eventService.Notify(new EventNewEra(base.Empire, technologyEraDefinition));
				if (this.playerControllerRepositoryService != null && this.playerControllerRepositoryService.ActivePlayerController != null && this.game.Empires.Count((global::Empire empire) => empire.Index != base.Empire.Index && empire.SimulationObject.GetPropertyValue(SimulationProperties.CurrentEra) >= (float)this.CurrentTechnologyEraNumber) == 0)
				{
					this.eventService.Notify(new EventNewEraGlobal(this.playerControllerRepositoryService.ActivePlayerController.Empire, technologyEraDefinition, this.CurrentTechnologyEraNumber, base.Empire));
				}
				IDatabase<PointOfInterestTemplate> database = Databases.GetDatabase<PointOfInterestTemplate>(false);
				IDatabase<ResourceDefinition> database2 = Databases.GetDatabase<ResourceDefinition>(false);
				if (database != null && database2 != null)
				{
					foreach (PointOfInterestTemplate pointOfInterestTemplate in database)
					{
						string a;
						if (pointOfInterestTemplate.Properties.TryGetValue("Type", out a))
						{
							if (!(a != "ResourceDeposit"))
							{
								string x;
								if (pointOfInterestTemplate.Properties.TryGetValue("VisibilityTechnology", out x))
								{
									if (!(x != technologyEraDefinition.Name))
									{
										string x2;
										ResourceDefinition resourceDefinition;
										if (pointOfInterestTemplate.Properties.TryGetValue("ResourceName", out x2) && database2.TryGetValue(x2, out resourceDefinition))
										{
											ResourceDefinition.Type resourceType = resourceDefinition.ResourceType;
											if (resourceType != ResourceDefinition.Type.Strategic)
											{
												if (resourceType == ResourceDefinition.Type.Luxury)
												{
													EventResourceTypeLuxuryDiscovered eventToNotify = new EventResourceTypeLuxuryDiscovered((global::Empire)base.Empire, resourceDefinition);
													this.eventService.Notify(eventToNotify);
												}
											}
											else
											{
												EventResourceTypeStrategicDiscovered eventToNotify2 = new EventResourceTypeStrategicDiscovered((global::Empire)base.Empire, resourceDefinition);
												this.eventService.Notify(eventToNotify2);
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
		if (technology.Name == this.ArcheologyTechnologyDefinitionName)
		{
			IGameService service = Services.GetService<IGameService>();
			if (service != null)
			{
				global::Game game = service.Game as global::Game;
				if (game != null)
				{
					int num = ~base.Empire.Bits;
					for (int i = 0; i < game.World.Regions.Length; i++)
					{
						Region region = game.World.Regions[i];
						if (region.PointOfInterests != null)
						{
							for (int j = 0; j < region.PointOfInterests.Length; j++)
							{
								region.PointOfInterests[j].Interaction.Bits &= num;
							}
						}
					}
				}
			}
		}
		if (technology.Name == TechnologyDefinition.Names.WinterEffectImmunity)
		{
			IGameService service2 = Services.GetService<IGameService>();
			Diagnostics.Assert(service2 != null);
			ISeasonService service3 = service2.Game.Services.GetService<ISeasonService>();
			if (service3 != null)
			{
				service3.RefreshDescriptorForImmuneEmpire(base.Empire as global::Empire);
			}
		}
		if (this.TechnologyUnlocked != null)
		{
			this.TechnologyUnlocked(this, new ConstructibleElementEventArgs(technology));
		}
		silent |= technology.HasTechnologyFlag(DepartmentOfScience.ConstructibleElement.TechnologyFlag.OrbUnlock);
		if (technology is TechnologyDefinition)
		{
			this.eventService.Notify(new EventTechnologyEnded(base.Empire, technology, silent));
		}
	}

	public string[] TechnologiesToUnlockMarketplace = new string[]
	{
		"TechnologyDefinitionMarketplaceHeroes",
		"TechnologyDefinitionMarketplaceMercenaries",
		"TechnologyDefinitionMarketplaceResources"
	};

	private readonly List<TechnologyEraDefinition> technologyEras = new List<TechnologyEraDefinition>();

	private readonly List<Construction> researchInProgress = new List<Construction>();

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private DepartmentOfIndustry departmentOfIndustry;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private IPlayerControllerRepositoryService playerControllerRepositoryService;

	private IEventService eventService;

	private IEndTurnService endTurnService;

	private global::Game game;

	private SimulationObjectWrapper researchSimulationObjectWrapper;

	private IDatabase<SimulationDescriptor> simulationDescriptorsDatatable;

	private IDatabase<DepartmentOfScience.ConstructibleElement> technologyDatabase;

	public abstract class ConstructibleElement : global::ConstructibleElement
	{
		[XmlAttribute("TechnologyFlags")]
		public DepartmentOfScience.ConstructibleElement.TechnologyFlag TechnologyFlags { get; protected set; }

		public static void InitializeUnlocksByTechnology()
		{
			IDatabase<DepartmentOfScience.ConstructibleElement> database = Databases.GetDatabase<DepartmentOfScience.ConstructibleElement>(false);
			if (DepartmentOfScience.ConstructibleElement.hasBeenInitialized)
			{
				return;
			}
			DepartmentOfScience.ConstructibleElement.hasBeenInitialized = true;
			DepartmentOfScience.ConstructibleElement.unlocksByTechnologies = new Dictionary<StaticString, List<global::ConstructibleElement>>();
			Diagnostics.Assert(database != null);
			DepartmentOfScience.ConstructibleElement[] values = database.GetValues();
			IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
			IDatabase<global::ConstructibleElement> database2 = Databases.GetDatabase<global::ConstructibleElement>(false);
			Diagnostics.Assert(database2 != null);
			global::ConstructibleElement[] values2 = database2.GetValues();
			if (values2 != null && values != null)
			{
				foreach (global::ConstructibleElement constructibleElement in values2)
				{
					Diagnostics.Assert(constructibleElement != null);
					if (constructibleElement.Prerequisites != null)
					{
						if (service != null)
						{
							bool flag = true;
							for (int j = 0; j < constructibleElement.Prerequisites.Length; j++)
							{
								if (constructibleElement.Prerequisites[j] is DownloadableContentPrerequisite)
								{
									DownloadableContentPrerequisite downloadableContentPrerequisite = constructibleElement.Prerequisites[j] as DownloadableContentPrerequisite;
									bool flag2 = service.IsShared(downloadableContentPrerequisite.DownloadableContentName);
									if (downloadableContentPrerequisite.Inverted)
									{
										flag2 = !flag2;
									}
									if (!flag2)
									{
										flag = false;
										break;
									}
								}
							}
							if (!flag)
							{
								goto IL_270;
							}
						}
						Prerequisite[] array = Array.FindAll<Prerequisite>(constructibleElement.Prerequisites, (Prerequisite prerequisite) => prerequisite is TechnologyPrerequisite);
						for (int k = 0; k < array.Length; k++)
						{
							Prerequisite prerequisite2 = array[k];
							TechnologyPrerequisite technologyPrerequisite = prerequisite2 as TechnologyPrerequisite;
							Diagnostics.Assert(technologyPrerequisite != null);
							foreach (DepartmentOfScience.ConstructibleElement constructibleElement2 in values)
							{
								Diagnostics.Assert(constructibleElement2 != null);
								if (constructibleElement2.SimulationDescriptorReferences != null)
								{
									if (Array.Exists<SimulationDescriptorReference>(constructibleElement2.SimulationDescriptorReferences, (SimulationDescriptorReference descriptorReference) => descriptorReference.Name == technologyPrerequisite.SimulationDescriptorReference))
									{
										Diagnostics.Assert(DepartmentOfScience.ConstructibleElement.unlocksByTechnologies != null);
										if (!DepartmentOfScience.ConstructibleElement.unlocksByTechnologies.ContainsKey(constructibleElement2.Name))
										{
											DepartmentOfScience.ConstructibleElement.unlocksByTechnologies.Add(constructibleElement2.Name, new List<global::ConstructibleElement>());
										}
										Diagnostics.Assert(DepartmentOfScience.ConstructibleElement.unlocksByTechnologies.ContainsKey(constructibleElement2.Name));
										Diagnostics.Assert(DepartmentOfScience.ConstructibleElement.unlocksByTechnologies[constructibleElement2.Name] != null);
										DepartmentOfScience.ConstructibleElement.unlocksByTechnologies[constructibleElement2.Name].Add(constructibleElement);
									}
								}
							}
						}
					}
					IL_270:;
				}
			}
		}

		public static void ReleaseUnlocksByTechnology()
		{
			if (DepartmentOfScience.ConstructibleElement.unlocksByTechnologies != null)
			{
				DepartmentOfScience.ConstructibleElement.unlocksByTechnologies.Clear();
			}
			DepartmentOfScience.ConstructibleElement.unlocksByTechnologies = null;
			DepartmentOfScience.ConstructibleElement.hasBeenInitialized = false;
		}

		public bool HasTechnologyFlag(DepartmentOfScience.ConstructibleElement.TechnologyFlag flag)
		{
			return (this.TechnologyFlags & flag) == flag;
		}

		public List<global::ConstructibleElement> GetUnlocksByTechnology()
		{
			if (!DepartmentOfScience.ConstructibleElement.hasBeenInitialized)
			{
				DepartmentOfScience.ConstructibleElement.InitializeUnlocksByTechnology();
			}
			if (DepartmentOfScience.ConstructibleElement.unlocksByTechnologies == null || !DepartmentOfScience.ConstructibleElement.unlocksByTechnologies.ContainsKey(this.Name))
			{
				return null;
			}
			return DepartmentOfScience.ConstructibleElement.unlocksByTechnologies[this.Name];
		}

		public bool IsResearched(SimulationObjectWrapper researchSimulationObjectWrapper)
		{
			bool flag = true;
			if (base.SimulationDescriptorReferences != null)
			{
				if (this.simulationPaths == null)
				{
					this.simulationPaths = new SimulationPath[base.SimulationDescriptorReferences.Length];
					for (int i = 0; i < base.SimulationDescriptorReferences.Length; i++)
					{
						this.simulationPaths[i] = new SimulationPath(base.SimulationDescriptorReferences[i]);
					}
				}
				for (int j = 0; j < this.simulationPaths.Length; j++)
				{
					Diagnostics.Assert(this.simulationPaths[j] != null);
					flag &= this.simulationPaths[j].IsSimulationObjectValid(researchSimulationObjectWrapper, PathNavigatorSemantic.CheckValidity);
				}
			}
			return flag;
		}

		private static bool hasBeenInitialized;

		private static Dictionary<StaticString, List<global::ConstructibleElement>> unlocksByTechnologies;

		private SimulationPath[] simulationPaths;

		public enum State
		{
			NotAvailable,
			Available,
			Queued,
			InProgress,
			Researched
		}

		[Flags]
		public enum TechnologyFlag
		{
			Affinity = 1,
			Quest = 2,
			Medal = 4,
			Unique = 8,
			OrbUnlock = 16
		}
	}

	public delegate void ConstructibleElementEventHandler(object sender, ConstructibleElementEventArgs e);

	public delegate void ConstructionChangeEventHandler(object sender, ConstructionChangeEventArgs e);
}
