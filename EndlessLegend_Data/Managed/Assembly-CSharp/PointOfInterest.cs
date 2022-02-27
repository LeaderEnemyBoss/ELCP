using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class PointOfInterest : SimulationObjectWrapper, IXmlSerializable, ILineOfSightEntity, IWorldPositionable, IGameEntity, IGameEntityWithLineOfSight, IGameEntityWithWorldPosition, ICategoryProvider, IPropertyEffectFeatureProvider, IWorldEntityMappingOverride
{
	public PointOfInterest()
	{
		this.lastLineOfSightRange = -1;
		this.LineOfSightActive = true;
		this.LineOfSightDirty = true;
		this.QuestStepsUsedBy = new List<StaticString>();
		base.Refreshed += this.PointOfInterest_Refreshed;
		this.Interaction = new PointOfInterestInteraction();
		SeasonManager.DustDepositsToggle += this.DustDepositsToggleEventHandler;
	}

	public PointOfInterest(PointOfInterestDefinition pointOfInterestDefinition, GameEntityGUID guid, Region region) : base("PointOfInterest#" + guid)
	{
		this.PointOfInterestDefinition = pointOfInterestDefinition;
		this.GUID = guid;
		this.Region = region;
		this.lastLineOfSightRange = this.LineOfSightVisionRange;
		this.LineOfSightActive = true;
		this.LineOfSightDirty = true;
		this.QuestStepsUsedBy = new List<StaticString>();
		base.Refreshed += this.PointOfInterest_Refreshed;
		this.Interaction = new PointOfInterestInteraction();
		this.AffinityMapping = StaticString.Empty;
		SeasonManager.DustDepositsToggle += this.DustDepositsToggleEventHandler;
	}

	StaticString ICategoryProvider.Category
	{
		get
		{
			string empty = string.Empty;
			if (this.PointOfInterestDefinition.TryGetValue("Category", out empty))
			{
				return empty;
			}
			return StaticString.Empty;
		}
	}

	int ILineOfSightEntity.EmpireInfiltrationBits
	{
		get
		{
			return this.InfiltrationBits;
		}
	}

	bool ILineOfSightEntity.IgnoreFog
	{
		get
		{
			return false;
		}
	}

	StaticString ICategoryProvider.SubCategory
	{
		get
		{
			string empty = string.Empty;
			if (this.PointOfInterestDefinition.TryGetValue("SubCategory", out empty))
			{
				return empty;
			}
			return StaticString.Empty;
		}
	}

	bool IWorldEntityMappingOverride.TryResolve(out string mappingName)
	{
		mappingName = "ClassPointOfInterest";
		return true;
	}

	bool IWorldEntityMappingOverride.TryResolve(out InterpreterContext context)
	{
		context = new InterpreterContext(base.SimulationObject);
		if (!StaticString.IsNullOrEmpty(this.AffinityMapping))
		{
			context.Register("AffinityMapping", this.AffinityMapping);
		}
		foreach (KeyValuePair<StaticString, string> keyValuePair in this.PointOfInterestDefinition.GetPropertiesAsEnumerator())
		{
			context.Register(keyValuePair.Key, keyValuePair.Value);
		}
		if (this.PointOfInterestImprovement != null && this.PointOfInterestImprovement.XmlMappingProperties != null)
		{
			for (int i = 0; i < this.PointOfInterestImprovement.XmlMappingProperties.Length; i++)
			{
				context.Register(this.PointOfInterestImprovement.XmlMappingProperties[i].Key, this.PointOfInterestImprovement.XmlMappingProperties[i].Value);
			}
		}
		return true;
	}

	SimulationObject IPropertyEffectFeatureProvider.GetSimulationObject()
	{
		return base.SimulationObject;
	}

	public override void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		this.GUID = reader.GetAttribute<ulong>("GUID");
		this.ArmyPillaging = reader.GetAttribute<ulong>("ArmyPillaging");
		base.ReadXml(reader);
		string text = reader.GetAttribute("PointOfInterestTemplateName");
		reader.ReadStartElement("PointOfInterestDefinition");
		this.PointOfInterestDefinition = new PointOfInterestDefinition();
		if (text == "QuestLocation_SunkenRuin")
		{
			text = "NavalQuestLocation_SunkenRuin";
		}
		IDatabase<PointOfInterestTemplate> database = Databases.GetDatabase<PointOfInterestTemplate>(true);
		this.PointOfInterestDefinition.PointOfInterestTemplateName = text;
		this.PointOfInterestDefinition.PointOfInterestTemplate = database.GetValue(text);
		this.PointOfInterestDefinition.WorldPosition = reader.ReadElementSerializable<WorldPosition>();
		int attribute = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("Overrides");
		for (int i = 0; i < attribute; i++)
		{
			string attribute2 = reader.GetAttribute("Key");
			string attribute3 = reader.GetAttribute("Value");
			this.PointOfInterestDefinition.Overrides.Add(attribute2, attribute3);
		}
		reader.ReadEndElement("Overrides");
		reader.ReadEndElement("PointOfInterestDefinition");
		this.Interaction.Bits = reader.GetAttribute<int>("Bits");
		int attribute4 = reader.GetAttribute<int>("InteractionLockCount");
		reader.ReadStartElement("Interaction");
		for (int j = 0; j < attribute4; j++)
		{
			int attribute5 = reader.GetAttribute<int>("Key");
			int attribute6 = reader.GetAttribute<int>("ValueCount");
			reader.ReadStartElement("InteractionLock");
			for (int k = 0; k < attribute6; k++)
			{
				string attribute7 = reader.GetAttribute<string>("Key");
				int attribute8 = reader.GetAttribute<int>("Value");
				reader.ReadStartElement("Value");
				if (!this.Interaction.InteractionLockCount.ContainsKey(attribute5))
				{
					this.Interaction.InteractionLockCount.Add(attribute5, new Dictionary<string, int>());
				}
				this.Interaction.InteractionLockCount[attribute5].Add(attribute7, attribute8);
				reader.ReadEndElement("Value");
			}
			reader.ReadEndElement("InteractionLock");
		}
		reader.ReadEndElement("Interaction");
		string text2 = reader.ReadElementString("PointOfInterestImprovement");
		if (!string.IsNullOrEmpty(text2))
		{
			IDatabase<DepartmentOfIndustry.ConstructibleElement> database2 = Databases.GetDatabase<DepartmentOfIndustry.ConstructibleElement>(true);
			DepartmentOfIndustry.ConstructibleElement pointOfInterestImprovement = null;
			if (database2.TryGetValue(text2, out pointOfInterestImprovement))
			{
				this.PointOfInterestImprovement = pointOfInterestImprovement;
				if (this.PointOfInterestImprovement != null)
				{
					for (int l = 0; l < this.PointOfInterestImprovement.Descriptors.Length; l++)
					{
						if (!base.SimulationObject.Tags.Contains(this.PointOfInterestImprovement.Descriptors[l].Name))
						{
							base.AddDescriptor(this.PointOfInterestImprovement.Descriptors[l], false);
						}
					}
				}
			}
			reader.ReadEndElement("PointOfInterestImprovement");
		}
		if (reader.IsStartElement("AffinityMapping"))
		{
			this.AffinityMapping = reader.ReadElementString("AffinityMapping");
		}
		this.QuestStepsUsedBy.Clear();
		if (reader.IsStartElement("QuestStepsUsedBy"))
		{
			int attribute9 = reader.GetAttribute<int>("QuestStepsCount");
			reader.ReadStartElement("QuestStepsUsedBy");
			for (int m = 0; m < attribute9; m++)
			{
				this.QuestStepsUsedBy.Add(reader.ReadString());
			}
			reader.ReadEndElement("QuestStepsUsedBy");
		}
		if (num < 2 && this.Type == Fortress.Citadel)
		{
			base.RemoveDescriptorByType("CitadelType");
			IDatabase<DepartmentOfIndustry.ConstructibleElement> database3 = Databases.GetDatabase<DepartmentOfIndustry.ConstructibleElement>(true);
			DepartmentOfIndustry.ConstructibleElement pointOfInterestImprovement2 = null;
			if (database3.TryGetValue(this.PointOfInterestDefinition.PointOfInterestTemplate.Properties["Improvement"], out pointOfInterestImprovement2))
			{
				this.PointOfInterestImprovement = pointOfInterestImprovement2;
				if (this.PointOfInterestImprovement != null)
				{
					for (int n = 0; n < this.PointOfInterestImprovement.Descriptors.Length; n++)
					{
						if (!base.SimulationObject.Tags.Contains(this.PointOfInterestImprovement.Descriptors[n].Name))
						{
							base.AddDescriptor(this.PointOfInterestImprovement.Descriptors[n], false);
						}
					}
				}
			}
		}
		if (num >= 3)
		{
			this.UntappedDustDeposits = reader.ReadElementString<bool>("UntappedDustDeposits");
		}
		if (num >= 4)
		{
			string text3 = reader.ReadElementString("CreepingNodeImprovement");
			if (!string.IsNullOrEmpty(text3))
			{
				IDatabase<CreepingNodeImprovementDefinition> database4 = Databases.GetDatabase<CreepingNodeImprovementDefinition>(true);
				CreepingNodeImprovementDefinition creepingNodeImprovement = null;
				if (database4.TryGetValue(text3, out creepingNodeImprovement))
				{
					this.CreepingNodeImprovement = creepingNodeImprovement;
					if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools && this.CreepingNodeImprovement != null)
					{
						for (int num2 = 0; num2 < this.CreepingNodeImprovement.Descriptors.Length; num2++)
						{
							if (base.SimulationObject.Tags.Contains(this.CreepingNodeImprovement.Descriptors[num2].Name))
							{
								base.RemoveDescriptor(this.CreepingNodeImprovement.Descriptors[num2]);
							}
						}
					}
				}
			}
			this.CreepingNodeGUID = reader.ReadElementString<ulong>("CreepingNodeGUID");
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteVersionAttribute(4);
		writer.WriteAttributeString<ulong>("GUID", this.GUID);
		writer.WriteAttributeString<ulong>("ArmyPillaging", this.ArmyPillaging);
		base.WriteXml(writer);
		writer.WriteStartElement("PointOfInterestDefinition");
		Diagnostics.Assert(this.PointOfInterestDefinition != null);
		Diagnostics.Assert(this.PointOfInterestDefinition.PointOfInterestTemplateName != null);
		writer.WriteAttributeString<StaticString>("PointOfInterestTemplateName", this.PointOfInterestDefinition.PointOfInterestTemplateName);
		IXmlSerializable xmlSerializable = this.PointOfInterestDefinition.WorldPosition;
		writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
		writer.WriteStartElement("Overrides");
		writer.WriteAttributeString<int>("Count", this.PointOfInterestDefinition.Overrides.Count);
		foreach (KeyValuePair<StaticString, string> keyValuePair in this.PointOfInterestDefinition.Overrides)
		{
			writer.WriteStartElement("OVerride");
			writer.WriteAttributeString<StaticString>("Key", keyValuePair.Key);
			writer.WriteAttributeString("Value", keyValuePair.Value);
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
		writer.WriteEndElement();
		writer.WriteStartElement("Interaction");
		writer.WriteAttributeString<int>("Bits", this.Interaction.Bits);
		writer.WriteAttributeString<int>("InteractionLockCount", this.Interaction.InteractionLockCount.Count);
		foreach (KeyValuePair<int, Dictionary<string, int>> keyValuePair2 in this.Interaction.InteractionLockCount)
		{
			writer.WriteStartElement("InteractionLock");
			writer.WriteAttributeString<int>("Key", keyValuePair2.Key);
			writer.WriteAttributeString<int>("ValueCount", this.Interaction.InteractionLockCount[keyValuePair2.Key].Count);
			foreach (KeyValuePair<string, int> keyValuePair3 in this.Interaction.InteractionLockCount[keyValuePair2.Key])
			{
				writer.WriteStartElement("Value");
				writer.WriteAttributeString<string>("Key", keyValuePair3.Key);
				writer.WriteAttributeString<int>("Value", keyValuePair3.Value);
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
		if (this.PointOfInterestImprovement == null)
		{
			writer.WriteElementString("PointOfInterestImprovement", string.Empty);
		}
		else
		{
			writer.WriteElementString<StaticString>("PointOfInterestImprovement", this.PointOfInterestImprovement.Name);
		}
		if (this.AffinityMapping == null)
		{
			writer.WriteElementString("AffinityMapping", string.Empty);
		}
		else
		{
			writer.WriteElementString<StaticString>("AffinityMapping", this.AffinityMapping);
		}
		writer.WriteStartElement("QuestStepsUsedBy");
		writer.WriteAttributeString<int>("QuestStepsCount", this.QuestStepsUsedBy.Count);
		foreach (StaticString value in this.QuestStepsUsedBy)
		{
			writer.WriteString<StaticString>(value);
		}
		writer.WriteEndElement();
		writer.WriteElementString("UntappedDustDeposits", this.UntappedDustDeposits.ToString());
		if (this.CreepingNodeImprovement == null)
		{
			writer.WriteElementString("CreepingNodeImprovement", string.Empty);
		}
		else
		{
			writer.WriteElementString<StaticString>("CreepingNodeImprovement", this.CreepingNodeImprovement.Name);
		}
		writer.WriteElementString<ulong>("CreepingNodeGUID", this.CreepingNodeGUID);
	}

	public bool UntappedDustDeposits
	{
		get
		{
			return this.untappedDustDeposits;
		}
		set
		{
			this.untappedDustDeposits = value;
			if (this.OnUntappedDustDepositsChange != null)
			{
				this.OnUntappedDustDepositsChange(this, new EventArgs());
			}
		}
	}

	~PointOfInterest()
	{
	}

	public Empire Empire { get; set; }

	public GameEntityGUID GUID { get; private set; }

	public GameEntityGUID ArmyPillaging { get; set; }

	public PointOfInterestInteraction Interaction { get; set; }

	public LineOfSightData LineOfSightData { get; set; }

	public bool LineOfSightActive { get; set; }

	public bool LineOfSightDirty { get; set; }

	public int LineOfSightDetectionRange
	{
		get
		{
			return Mathf.RoundToInt(this.GetPropertyValue(SimulationProperties.DetectionRange));
		}
	}

	public int LineOfSightVisionRange
	{
		get
		{
			return Mathf.RoundToInt(this.GetPropertyValue(SimulationProperties.VisionRange));
		}
	}

	public int LineOfSightVisionHeight
	{
		get
		{
			return Mathf.RoundToInt(this.GetPropertyValue(SimulationProperties.VisionHeight));
		}
	}

	public PointOfInterestDefinition PointOfInterestDefinition { get; private set; }

	public ConstructibleElement PointOfInterestImprovement { get; private set; }

	public ConstructibleElement CreepingNodeImprovement { get; set; }

	public GameEntityGUID CreepingNodeGUID { get; set; }

	public List<StaticString> QuestStepsUsedBy { get; private set; }

	public StaticString AffinityMapping { get; private set; }

	public Region Region { get; internal set; }

	public StaticString Type
	{
		get
		{
			if (this.type == null)
			{
				string empty = string.Empty;
				if (this.PointOfInterestDefinition == null || !this.PointOfInterestDefinition.TryGetValue("Type", out empty))
				{
					Diagnostics.Assert("Can't retrieve the point of interest type for instance (name: '{0}').", new object[]
					{
						base.Name
					});
				}
				this.type = empty;
			}
			return this.type;
		}
	}

	public VisibilityController.VisibilityAccessibility VisibilityAccessibilityLevel
	{
		get
		{
			return VisibilityController.VisibilityAccessibility.Public;
		}
	}

	public WorldPosition WorldPosition
	{
		get
		{
			return this.PointOfInterestDefinition.WorldPosition;
		}
	}

	public void RemovePointOfInterestImprovement()
	{
		if (this.PointOfInterestImprovement != null && this.PointOfInterestImprovement.Descriptors != null)
		{
			this.RemoveImprovementDescriptors(this.PointOfInterestImprovement.Descriptors);
			this.PointOfInterestImprovement = null;
		}
	}

	public void SwapPointOfInterestImprovement(ConstructibleElement constructibleElement, Empire empire = null)
	{
		this.RemovePointOfInterestImprovement();
		this.PointOfInterestImprovement = constructibleElement;
		this.RefreshEmpireAffinityMapping(empire);
		if (this.PointOfInterestImprovement != null && this.PointOfInterestImprovement.Descriptors != null)
		{
			this.AddImprovementDescriptors(this.PointOfInterestImprovement.Descriptors);
		}
		this.LineOfSightDirty = true;
	}

	private void AddImprovementDescriptors(SimulationDescriptor[] descriptors)
	{
		for (int i = 0; i < descriptors.Length; i++)
		{
			base.AddDescriptor(descriptors[i], false);
		}
	}

	private void RemoveImprovementDescriptors(SimulationDescriptor[] descriptors)
	{
		for (int i = 0; i < descriptors.Length; i++)
		{
			base.RemoveDescriptor(descriptors[i]);
		}
	}

	private void RefreshEmpireAffinityMapping(Empire empire)
	{
		if (empire != null)
		{
			this.AffinityMapping = empire.Faction.AffinityMapping.ToString();
			this.AffinityMapping = this.AffinityMapping.ToString().Replace("AffinityMapping", string.Empty);
		}
		else
		{
			this.AffinityMapping = StaticString.Empty;
		}
	}

	public void SetArmyPillaging(GameEntityGUID armyGuid)
	{
		this.ArmyPillaging = armyGuid;
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		this.Region = null;
		this.PointOfInterestImprovement = null;
		base.Refreshed -= this.PointOfInterest_Refreshed;
		SeasonManager.DustDepositsToggle -= this.DustDepositsToggleEventHandler;
	}

	private void PointOfInterest_Refreshed(object sender)
	{
		int lineOfSightVisionRange = this.LineOfSightVisionRange;
		if (lineOfSightVisionRange != this.lastLineOfSightRange)
		{
			this.LineOfSightDirty = true;
			this.lastLineOfSightRange = lineOfSightVisionRange;
		}
	}

	public void DustDepositsToggleEventHandler(bool isActive)
	{
		string a;
		this.UntappedDustDeposits = (isActive && this.PointOfInterestDefinition.TryGetValue("QuestLocationType", out a) && a == "Temple");
	}

	public bool IsResourceDeposit()
	{
		return this.type == PointOfInterest.ResourceDepositType && base.SimulationObject.Tags.Contains(PointOfInterest.PointOfInterestTypeResourceDepositTag);
	}

	public bool IsLuxuryDeposit()
	{
		string a;
		return this.type == PointOfInterest.ResourceDepositType && this.PointOfInterestDefinition.PointOfInterestTemplate.Properties.TryGetValue("ResourceType", out a) && a == "Luxury";
	}

	public int InfiltrationBits { get; set; }

	public static readonly StaticString PointOfInterestType = "PointOfInterestType";

	public static readonly StaticString VictoryQuestDescriptorName = "PointOfInterestVictoryQuest";

	public static readonly StaticString ResourceDepositType = "ResourceDeposit";

	public static readonly StaticString PointOfInterestTypeResourceDepositTag = "PointOfInterestTypeResourceDeposit";

	public EventHandler<EventArgs> OnUntappedDustDepositsChange;

	private int lastLineOfSightRange;

	private StaticString type;

	private bool untappedDustDeposits;
}
