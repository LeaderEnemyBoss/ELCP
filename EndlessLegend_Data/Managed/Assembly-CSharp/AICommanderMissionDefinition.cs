using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.Framework;

public class AICommanderMissionDefinition : IDatatableElement
{
	[XmlElement(ElementName = "Property", Type = typeof(AICommanderMissionDefinition.StringObjectPair))]
	public AICommanderMissionDefinition.StringObjectPair[] XmlSerializableProperties
	{
		get
		{
			AICommanderMissionDefinition.StringObjectPair[] array = new AICommanderMissionDefinition.StringObjectPair[this.properties.Count];
			int num = 0;
			foreach (KeyValuePair<StaticString, string> keyValuePair in this.properties)
			{
				array[num].Key = keyValuePair.Key;
				array[num].Value = keyValuePair.Value;
				num++;
			}
			return array;
		}
		private set
		{
			if (value != null)
			{
				for (int i = 0; i < value.Length; i++)
				{
					this.properties.Add(value[i].Key, value[i].Value);
				}
			}
		}
	}

	[XmlElement]
	public AIArmyPatternDefinition AIArmyPattern { get; set; }

	[XmlIgnore]
	public Tags Category { get; set; }

	[XmlArray("HeroItemBoosts")]
	[XmlArrayItem(Type = typeof(AIParameter.AIModifier), ElementName = "Boost")]
	public AIParameter.AIModifier[] HeroItemBoosts { get; set; }

	[XmlIgnore]
	public StaticString Name { get; set; }

	[XmlIgnore]
	public Dictionary<StaticString, string> Properties
	{
		get
		{
			return this.properties;
		}
	}

	[XmlAttribute]
	public string Type { get; set; }

	[XmlAttribute("Category")]
	public string XmlSerializableCategory
	{
		get
		{
			return this.Category.ToString();
		}
		set
		{
			this.Category = new Tags();
			this.Category.Parse(value, ',');
		}
	}

	[XmlAttribute("Name")]
	public string XmlSerializableName
	{
		get
		{
			return this.Name;
		}
		set
		{
			this.Name = value;
		}
	}

	public bool TryGetValue(StaticString key, out string value)
	{
		return this.Properties.TryGetValue(key, out value);
	}

	private Dictionary<StaticString, string> properties = new Dictionary<StaticString, string>();

	public enum AICommanderCategory
	{
		Undefined = -1,
		Colonization,
		Exploration,
		Defense,
		Roaming,
		Regroup,
		Pacification,
		DefaultBehavior,
		Offense,
		War,
		Quest,
		Patrol,
		Bribe,
		SiegeBreaker,
		DefenseUnderSiege,
		WarPatrol,
		Convert,
		DefenseStressed,
		Colossus,
		Solitary,
		SettlerBail,
		MaximumNumber,
		Terraformation,
		KaijuAdquisition
	}

	public class StringObjectPair
	{
		[XmlAttribute("Name")]
		public string Key { get; set; }

		[XmlText]
		public string Value { get; set; }
	}
}
