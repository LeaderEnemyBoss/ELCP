using System;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;

public class QuestDefinition : DatatableElement, IDatatableElementWithCategory, ICategoryProvider
{
	public QuestDefinition()
	{
		this.Cooldown = 0;
		this.NumberOfConcurrentInstances = 1;
		this.NumberOfOccurencesPerEmpire = 0;
		this.NumberOfOccurencesPerGame = 0;
		this.NumberOfGlobalQuestConcurrentInstances = -1;
		this.ChanceOfTriggering = 1f;
		this.Variables = new QuestVariableDefinition[0];
		this.Tags = new Tags();
	}

	[XmlIgnore]
	public StaticString Category { get; protected set; }

	[XmlAttribute]
	public float ChanceOfTriggering { get; set; }

	[XmlAttribute]
	public int Cooldown { get; set; }

	[XmlAttribute]
	public GlobalCooldownLiability GlobalCooldownLiability { get; set; }

	[XmlAttribute]
	public bool IsGlobal { get; set; }

	[XmlAttribute]
	public int GlobalCooldown { get; set; }

	[XmlAttribute]
	public GlobalQuestWinner GlobalWinner { get; set; }

	[XmlAttribute]
	public bool SingleCheckPerTurn { get; set; }

	[XmlAttribute]
	public int NumberOfConcurrentInstances { get; set; }

	[XmlAttribute]
	public int NumberOfGlobalQuestConcurrentInstances { get; set; }

	[XmlAttribute]
	public int NumberOfOccurencesPerEmpire { get; set; }

	[XmlAttribute]
	public int NumberOfOccurencesPerGame { get; set; }

	[XmlElement(Type = typeof(QuestPrerequisites), ElementName = "Prerequisites")]
	public QuestPrerequisites[] Prerequisites { get; set; }

	[XmlElement(Type = typeof(BehaviourTreeNode_Sequence), ElementName = "Controller_Sequence")]
	[XmlElement(Type = typeof(BehaviourTreeNode_Parallel), ElementName = "Controller_Parallel")]
	public BehaviourTreeNode Root { get; set; }

	[XmlAttribute]
	public bool SkipLockedQuestTarget { get; set; }

	[XmlArray("Steps")]
	[XmlArrayItem(Type = typeof(QuestStep), ElementName = "Step")]
	public QuestStep[] Steps { get; set; }

	[XmlIgnore]
	public virtual StaticString SubCategory { get; protected set; }

	public Tags Tags { get; set; }

	[XmlElement("Triggers")]
	public QuestDefinitionTriggers Triggers { get; set; }

	[XmlAttribute]
	public bool TriggersAchievementStatistic { get; set; }

	[XmlArray("Vars")]
	[XmlArrayItem("Var", typeof(QuestVariableDefinition))]
	[XmlArrayItem("InterpretedVar", typeof(QuestInterpretedVariableDefinition))]
	[XmlArrayItem("DropListVar", typeof(QuestDropListVariableDefinition))]
	[XmlArrayItem("LocalizationVar", typeof(QuestLocalizationVariableDefinition))]
	public QuestVariableDefinition[] Variables { get; set; }

	[XmlAttribute("Category")]
	public string XmlSerializableCategory
	{
		get
		{
			return this.Category;
		}
		set
		{
			this.Category = value;
		}
	}

	[XmlAttribute("SubCategory")]
	public string XmlSerializableSubCategory
	{
		get
		{
			return this.SubCategory;
		}
		set
		{
			this.SubCategory = value;
		}
	}

	public bool IsMainQuest()
	{
		return this.Category == QuestDefinition.CategoryMainQuest;
	}

	public bool IsVictoryQuest()
	{
		return this.Category == QuestDefinition.CategoryVictoryQuest;
	}

	public bool IsMainQuestLastChapter()
	{
		return this.IsMainQuest() && this.Triggers != null && this.Triggers.OnQuestCompleted != null && this.Triggers.OnQuestCompleted.Tags != null && !this.Triggers.OnQuestCompleted.Tags.Contains(QuestDefinition.TagMainQuest);
	}

	public QuestVariableDefinition[] GetPrerequisiteVariables()
	{
		if (this.prerequisiteVariableDefinitions == null)
		{
			this.prerequisiteVariableDefinitions = (from variable in this.Variables
			where variable.UsedInPrerequisites
			select variable).ToArray<QuestVariableDefinition>();
		}
		return this.prerequisiteVariableDefinitions;
	}

	public QuestVariableDefinition[] GetNonPrerequisiteVariables()
	{
		if (this.nonPrerequisiteVariableDefinitions == null)
		{
			this.nonPrerequisiteVariableDefinitions = (from variable in this.Variables
			where !variable.UsedInPrerequisites
			select variable).ToArray<QuestVariableDefinition>();
		}
		return this.nonPrerequisiteVariableDefinitions;
	}

	public static readonly StaticString CategoryMainQuest = new StaticString("MainQuest");

	public static readonly StaticString CategoryMedal = new StaticString("Medal");

	public static readonly StaticString CategoryVictoryQuest = new StaticString("VictoryQuest");

	public static readonly StaticString CategoryWonder = new StaticString("WonderMedal");

	public static readonly StaticString TagMainQuest = new StaticString("MainQuest");

	public static readonly StaticString TagBeginTurn = new StaticString("BeginTurn");

	public static readonly StaticString TagExclusive = new StaticString("Exclusive");

	public static readonly StaticString TagHidden = new StaticString("Hidden");

	public static readonly StaticString WinnerVariableName = new StaticString("WinnerEmpireIndex");

	[XmlIgnore]
	private QuestVariableDefinition[] prerequisiteVariableDefinitions;

	[XmlIgnore]
	private QuestVariableDefinition[] nonPrerequisiteVariableDefinitions;
}
