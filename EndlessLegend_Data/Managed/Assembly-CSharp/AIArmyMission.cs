using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

[Diagnostics.TagAttribute("AI")]
public class AIArmyMission : ITickable, Amplitude.Xml.Serialization.IXmlSerializable
{
	public void ReadXml(XmlReader reader)
	{
		StaticString key = reader.GetAttribute("AIArmyMissionDefinitionName");
		this.Completion = (AIArmyMission.AIArmyMissionCompletion)reader.GetAttribute<int>("Completion");
		this.ErrorCode = (AIArmyMission.AIArmyMissionErrorCode)reader.GetAttribute<int>("ErrorCode");
		reader.ReadStartElement();
		IDatabase<AIArmyMissionDefinition> database = Databases.GetDatabase<AIArmyMissionDefinition>(false);
		this.AIArmyMissionDefinition = database.GetValue(key);
		this.aiBehaviorTree = reader.ReadElementSerializable<AIBehaviorTree>();
		if (this.aiBehaviorTree != null)
		{
			if (this.aiBehaviorTree.Root == null)
			{
				this.aiBehaviorTree.Release();
				this.aiBehaviorTree = null;
				this.AIArmyMissionDefinition = null;
			}
			else
			{
				this.aiBehaviorTree.AICommander = this.AICommander;
			}
		}
	}

	public void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<StaticString>("AIArmyMissionDefinitionName", this.AIArmyMissionDefinition.Name);
		writer.WriteAttributeString<int>("Completion", (int)this.Completion);
		writer.WriteAttributeString<int>("ErrorCode", (int)this.ErrorCode);
		writer.WriteElementSerializable<AIBehaviorTree>(ref this.aiBehaviorTree);
	}

	~AIArmyMission()
	{
	}

	[XmlIgnore]
	public AIArmyMissionDefinition AIArmyMissionDefinition { get; set; }

	[XmlIgnore]
	public AICommander AICommander { get; set; }

	[XmlIgnore]
	public Army Army { get; set; }

	[XmlIgnore]
	public AIArmyMission.AIArmyMissionCompletion Completion { get; set; }

	[XmlIgnore]
	public string LastDebugString
	{
		get
		{
			return this.aiBehaviorTree.LastDebugString;
		}
	}

	[XmlIgnore]
	public string LastNodeName
	{
		get
		{
			return this.aiBehaviorTree.LastNodeName;
		}
	}

	[XmlIgnore]
	public AIArmyMission.AIArmyMissionErrorCode ErrorCode { get; set; }

	[XmlIgnore]
	public WorldPosition LastPathfindTargetPosition { get; set; }

	[XmlIgnore]
	public TickableState State { get; set; }

	public void Initialize(params object[] parameters)
	{
		this.State = TickableState.NeedTick;
		this.ErrorCode = AIArmyMission.AIArmyMissionErrorCode.None;
		if (this.aiBehaviorTree == null)
		{
			this.aiBehaviorTree = new AIBehaviorTree(this.AIArmyMissionDefinition.Root)
			{
				AICommander = this.AICommander
			};
			this.aiBehaviorTree.Initialize();
		}
		this.aiBehaviorTree.Variables.Add("$Army", this.Army);
		this.TrySetParameters(parameters);
	}

	public virtual void Release()
	{
		this.AICommander = null;
		this.Army = null;
		this.AIArmyMissionDefinition = null;
		if (this.aiBehaviorTree != null)
		{
			this.aiBehaviorTree.Release();
			this.aiBehaviorTree = null;
		}
	}

	public virtual void Reset()
	{
		if (this.aiBehaviorTree != null)
		{
			this.aiBehaviorTree.Reset();
		}
		else
		{
			this.aiBehaviorTree = new AIBehaviorTree(this.AIArmyMissionDefinition.Root)
			{
				AICommander = this.AICommander
			};
			this.aiBehaviorTree.Initialize();
		}
		this.ErrorCode = AIArmyMission.AIArmyMissionErrorCode.None;
		this.Completion = AIArmyMission.AIArmyMissionCompletion.Running;
	}

	public void Tick()
	{
		if (this.Completion != AIArmyMission.AIArmyMissionCompletion.Running)
		{
			this.State = TickableState.NoTick;
			return;
		}
		this.ErrorCode = AIArmyMission.AIArmyMissionErrorCode.None;
		if (this.aiBehaviorTree != null)
		{
			if (this.aiBehaviorTree.AICommander != this.AICommander)
			{
				this.aiBehaviorTree.AICommander = this.AICommander;
			}
			State state = this.aiBehaviorTree.Execute(new object[0]);
			this.LastPathfindTargetPosition = this.aiBehaviorTree.LastPathfindTargetPosition;
			this.ErrorCode = (AIArmyMission.AIArmyMissionErrorCode)this.aiBehaviorTree.ErrorCode;
			if (state == Amplitude.Unity.AI.BehaviourTree.State.Running)
			{
				if (this.ErrorCode == AIArmyMission.AIArmyMissionErrorCode.MoveInProgress)
				{
					this.aiBehaviorTree.Reset();
				}
			}
			else if (state == Amplitude.Unity.AI.BehaviourTree.State.Failure)
			{
				this.Completion = AIArmyMission.AIArmyMissionCompletion.Fail;
			}
			else
			{
				this.Completion = AIArmyMission.AIArmyMissionCompletion.Success;
			}
		}
		else
		{
			this.Completion = AIArmyMission.AIArmyMissionCompletion.Fail;
		}
	}

	public bool TrySetParameters(params object[] parameters)
	{
		Diagnostics.Assert(this.aiBehaviorTree != null);
		if (parameters != null && this.AIArmyMissionDefinition.Parameters != null && this.AIArmyMissionDefinition.Parameters.Length > 0)
		{
			for (int i = 0; i < this.AIArmyMissionDefinition.Parameters.Length; i++)
			{
				Diagnostics.Assert(this.AIArmyMissionDefinition.Parameters[i].ParameterIndex >= 0);
				if (this.AIArmyMissionDefinition.Parameters[i].ParameterIndex >= parameters.Length)
				{
					return false;
				}
				if (this.aiBehaviorTree.Variables.ContainsKey(this.AIArmyMissionDefinition.Parameters[i].VarName))
				{
					this.aiBehaviorTree.Variables[this.AIArmyMissionDefinition.Parameters[i].VarName] = parameters[this.AIArmyMissionDefinition.Parameters[i].ParameterIndex];
				}
				else
				{
					this.aiBehaviorTree.Variables.Add(this.AIArmyMissionDefinition.Parameters[i].VarName, parameters[this.AIArmyMissionDefinition.Parameters[i].ParameterIndex]);
				}
			}
		}
		this.Reset();
		return true;
	}

	private AIBehaviorTree aiBehaviorTree;

	public enum AIArmyMissionCompletion
	{
		Fail,
		Running,
		Success
	}

	public enum AIArmyMissionErrorCode
	{
		None,
		PreprocessFailed,
		InvalidDestination,
		PathNotFound,
		AlreadyInPosition,
		DestinationNotReached,
		RegionOccupied,
		RegionIsMine,
		NoTargetInRange,
		TargetInRange,
		NoTargetSelected,
		NoAttackingPosition,
		TargetTooFar,
		CanNotDefeatTarget,
		TargetCanBeDefeated,
		NoSavingPosition,
		TargetNotLocked,
		TargetLocked,
		TargetNotBesieging,
		TargetBesieging,
		CanNotReachPositionInTurn,
		CanReachPositionInTurn,
		ArmyNotAbleToColonize,
		EnemyOnPath,
		NoMovementPoint,
		InvalidEmpire,
		WaitForHeal,
		MoveCancelled,
		MoveInProgress,
		OrderGoToFail,
		SearchFail,
		OrderBribeFail,
		OrderConvertVillageFail,
		AllActionPointSpent,
		InvalidArmy,
		MissingArmyAIData,
		OrderConvertToPrivateersFail,
		OrderTerrafomFail,
		CanNotAffordArmyAction,
		EmpireControlledByAI,
		EmpireControlledByHuman,
		Undefined
	}
}
