using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.Simulation.Advanced;

public class QuestPrerequisites
{
	[XmlIgnore]
	public StaticString Target { get; set; }

	[XmlElement(Type = typeof(DownloadableContentPrerequisite), ElementName = "DownloadableContentPrerequisite")]
	[XmlElement(Type = typeof(TechnologyPrerequisite), ElementName = "TechnologyPrerequisite")]
	[XmlElement(Type = typeof(QuestStatePrerequisite), ElementName = "QuestStatePrerequisite")]
	[XmlElement(Type = typeof(PathPrerequisite), ElementName = "PathPrerequisite")]
	[XmlElement(Type = typeof(InterpreterPrerequisite), ElementName = "InterpreterPrerequisite")]
	[XmlElement(Type = typeof(AchievementPrerequisite), ElementName = "AchievementPrerequisite")]
	[XmlElement(Type = typeof(GlobalEventPrerequisite), ElementName = "GlobalEventPrerequisite")]
	public Prerequisite[] Prerequisites { get; set; }

	[XmlAttribute("Target")]
	public string XmlSerializableTarget
	{
		get
		{
			return this.Target;
		}
		set
		{
			this.Target = value;
		}
	}

	[XmlAttribute("AnyTarget")]
	public bool AnyTarget { get; set; }

	[XmlAttribute("AnyPrerequisite")]
	public bool AnyPrerequisite { get; set; }

	public virtual object Clone()
	{
		return base.MemberwiseClone();
	}
}
