using System;
using System.Collections.Generic;
using System.ComponentModel;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

[Diagnostics.TagAttribute("AI")]
public class AICommanderMission : ITickable, IXmlSerializable
{
	public AICommanderMission()
	{
		this.Completion = AICommanderMission.AICommanderMissionCompletion.Initializing;
		this.State = TickableState.NeedTick;
		this.ArmyMissionParameters = new Dictionary<StaticString, List<object>>();
		this.AIDataArmyGUID = GameEntityGUID.Zero;
		this.InternalGUID = GameEntityGUID.Zero;
	}

	public virtual void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		if (num >= 2)
		{
			this.InternalGUID = reader.GetAttribute<ulong>("InternalGUID");
		}
		if (this.InternalGUID == GameEntityGUID.Zero)
		{
			this.InternalGUID = AIScheduler.Services.GetService<IAIEntityGUIDAIHelper>().GenerateAIEntityGUID();
		}
		this.Completion = (AICommanderMission.AICommanderMissionCompletion)reader.GetAttribute<int>("Completion");
		if (this.Completion == AICommanderMission.AICommanderMissionCompletion.Running)
		{
			this.Completion = AICommanderMission.AICommanderMissionCompletion.Initializing;
		}
		this.AIDataArmyGUID = new GameEntityGUID(reader.GetAttribute<ulong>("AIDataArmyGUID"));
		IDatabase<AICommanderMissionDefinition> database = Databases.GetDatabase<AICommanderMissionDefinition>(false);
		Diagnostics.Assert(database != null);
		StaticString staticString = reader.GetAttribute("MissionDefinition");
		if (!StaticString.IsNullOrEmpty(staticString))
		{
			this.MissionDefinition = database.GetValue(staticString);
		}
		else
		{
			this.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			this.State = TickableState.NoTick;
		}
		this.IsActive = reader.GetAttribute<bool>("IsActive");
		reader.ReadStartElement();
		if (this.AIDataArmyGUID != GameEntityGUID.Zero)
		{
			AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(this.AIDataArmyGUID);
			if (aidata != null)
			{
				aidata.AssignCommanderMission(this);
			}
		}
	}

	public virtual void WriteXml(XmlWriter writer)
	{
		writer.WriteVersionAttribute(3);
		writer.WriteAttributeString("AssemblyQualifiedName", base.GetType().AssemblyQualifiedName);
		writer.WriteAttributeString<int>("Completion", (int)this.Completion);
		writer.WriteAttributeString<ulong>("AIDataArmyGUID", this.AIDataArmyGUID);
		writer.WriteAttributeString<ulong>("InternalGUID", this.InternalGUID);
		writer.WriteAttributeString("MissionDefinition", (this.MissionDefinition == null) ? string.Empty : this.MissionDefinition.Name.ToString());
		writer.WriteAttributeString<bool>("IsActive", this.IsActive);
	}

	~AICommanderMission()
	{
	}

	public StaticString SeasonToSwitchTo { get; set; }

	public GameEntityGUID AIDataArmyGUID { get; set; }

	public AICommander Commander { get; set; }

	public AICommanderMission.AICommanderMissionCompletion Completion { get; set; }

	public GameEntityGUID InternalGUID { get; set; }

	public bool IsActive { get; protected set; }

	public AICommanderMissionDefinition MissionDefinition { get; private set; }

	public float PillageBoost { get; set; }

	public float Score { get; protected set; }

	public TickableState State { get; set; }

	protected Dictionary<StaticString, List<object>> ArmyMissionParameters { get; set; }

	public virtual AIParameter.AIModifier[] GetHeroItemModifiers()
	{
		return this.MissionDefinition.HeroItemBoosts;
	}

	public virtual void Initialize(AICommander aiCommander)
	{
		this.Commander = aiCommander;
		this.departmentOfDefense = this.Commander.Empire.GetAgency<DepartmentOfDefense>();
		Diagnostics.Assert(this.departmentOfDefense != null);
		this.departmentOfDefense.ArmiesCollectionChange += this.AICommanderMission_ArmyCollectionChange;
		this.aiDataRepository = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		this.tickableRepository = AIScheduler.Services.GetService<ITickableRepositoryAIHelper>();
		if (this.InternalGUID == GameEntityGUID.Zero)
		{
			this.InternalGUID = AIScheduler.Services.GetService<IAIEntityGUIDAIHelper>().GenerateAIEntityGUID();
		}
	}

	public virtual void Interrupt()
	{
		this.SetArmyFree();
		this.Completion = AICommanderMission.AICommanderMissionCompletion.Interrupted;
		this.State = TickableState.Optional;
	}

	public virtual void Load()
	{
	}

	public virtual void Promote()
	{
		Diagnostics.Assert(this.tickableRepository != null);
		this.tickableRepository.Register(this);
		this.IsActive = true;
		this.State = TickableState.NeedTick;
	}

	public void QuestSetArmy(Army selectedArmy)
	{
		this.QuestSetArmy(selectedArmy.GUID);
	}

	public void QuestSetArmy(GameEntityGUID selectedArmyGuid)
	{
		AIData_Army aidata_Army;
		if (this.aiDataRepository.TryGetAIData<AIData_Army>(selectedArmyGuid, out aidata_Army) && aidata_Army.CommanderMission == null)
		{
			aidata_Army.AssignCommanderMission(this);
			this.AIDataArmyGUID = selectedArmyGuid;
		}
	}

	public virtual void Refresh()
	{
	}

	public virtual void Release()
	{
		this.SetArmyFree();
		this.Commander = null;
		this.aiDataRepository = null;
		if (this.departmentOfDefense != null)
		{
			this.departmentOfDefense.ArmiesCollectionChange -= this.AICommanderMission_ArmyCollectionChange;
			this.departmentOfDefense = null;
		}
		if (this.tickableRepository != null)
		{
			if (this.IsActive)
			{
				this.tickableRepository.Unregister(this);
				this.IsActive = false;
			}
			this.tickableRepository = null;
		}
	}

	public void ResetActiveFlag()
	{
		this.IsActive = false;
	}

	public void SetArmyFree()
	{
		if (this.AIDataArmyGUID != GameEntityGUID.Zero)
		{
			AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(this.AIDataArmyGUID);
			if (aidata != null && aidata.CommanderMission == this)
			{
				aidata.UnassignCommanderMission();
			}
			this.AIDataArmyGUID = GameEntityGUID.Zero;
		}
	}

	public virtual void SetParameters(AICommanderMissionDefinition missionDefinition, params object[] parameters)
	{
		Diagnostics.Assert(parameters != null);
		this.MissionDefinition = missionDefinition;
	}

	public void Tick()
	{
		this.Process();
	}

	protected virtual void ArmyLost()
	{
		this.Interrupt();
		this.State = TickableState.NoTick;
		this.AIDataArmyGUID = GameEntityGUID.Zero;
	}

	protected virtual void Fail()
	{
		this.SetArmyFree();
		this.ArmyMissionParameters.Clear();
		this.State = TickableState.NoTick;
	}

	protected virtual AICommanderMission.AICommanderMissionCompletion GetCompletionFor(AIArmyMission.AIArmyMissionErrorCode errorCode, out TickableState tickableState)
	{
		if (errorCode == AIArmyMission.AIArmyMissionErrorCode.MoveInProgress)
		{
			tickableState = TickableState.NeedTick;
			return AICommanderMission.AICommanderMissionCompletion.Running;
		}
		if (errorCode == AIArmyMission.AIArmyMissionErrorCode.NoTargetSelected)
		{
			tickableState = TickableState.Optional;
			return AICommanderMission.AICommanderMissionCompletion.Success;
		}
		if (errorCode == AIArmyMission.AIArmyMissionErrorCode.DestinationNotReached || errorCode == AIArmyMission.AIArmyMissionErrorCode.NoMovementPoint || errorCode == AIArmyMission.AIArmyMissionErrorCode.NoSavingPosition || errorCode == AIArmyMission.AIArmyMissionErrorCode.AllActionPointSpent)
		{
			tickableState = TickableState.NoTick;
			return AICommanderMission.AICommanderMissionCompletion.Running;
		}
		if (errorCode == AIArmyMission.AIArmyMissionErrorCode.TargetBesieging)
		{
			tickableState = TickableState.NoTick;
			return AICommanderMission.AICommanderMissionCompletion.Initializing;
		}
		if (errorCode == AIArmyMission.AIArmyMissionErrorCode.PathNotFound || errorCode == AIArmyMission.AIArmyMissionErrorCode.TargetLocked)
		{
			tickableState = TickableState.Optional;
			return AICommanderMission.AICommanderMissionCompletion.Running;
		}
		if (errorCode == AIArmyMission.AIArmyMissionErrorCode.OrderGoToFail || errorCode == AIArmyMission.AIArmyMissionErrorCode.AlreadyInPosition || errorCode == AIArmyMission.AIArmyMissionErrorCode.NoTargetInRange || errorCode == AIArmyMission.AIArmyMissionErrorCode.SearchFail || errorCode == AIArmyMission.AIArmyMissionErrorCode.InvalidDestination)
		{
			tickableState = TickableState.Optional;
			return AICommanderMission.AICommanderMissionCompletion.Initializing;
		}
		tickableState = TickableState.NoTick;
		return AICommanderMission.AICommanderMissionCompletion.Fail;
	}

	protected virtual AICommanderMission.AICommanderMissionCompletion GetCompletionWhenSuccess(AIData_Army armyData, out TickableState tickableState)
	{
		tickableState = TickableState.Optional;
		return AICommanderMission.AICommanderMissionCompletion.Initializing;
	}

	protected virtual void Initializing()
	{
		this.State = TickableState.Optional;
		if (this.TryGetArmyData() && this.TryComputeArmyMissionParameter())
		{
			this.Completion = AICommanderMission.AICommanderMissionCompletion.Running;
			this.State = TickableState.NeedTick;
			return;
		}
	}

	protected virtual void Interrupted()
	{
		this.SetArmyFree();
		this.State = TickableState.NoTick;
	}

	protected virtual void Pending()
	{
	}

	protected virtual void Process()
	{
		switch (this.Completion)
		{
		case AICommanderMission.AICommanderMissionCompletion.Initializing:
			this.Initializing();
			break;
		case AICommanderMission.AICommanderMissionCompletion.Running:
			this.Running();
			break;
		case AICommanderMission.AICommanderMissionCompletion.Pending:
			this.Pending();
			break;
		case AICommanderMission.AICommanderMissionCompletion.Success:
			this.Success();
			break;
		case AICommanderMission.AICommanderMissionCompletion.Fail:
			this.Fail();
			break;
		case AICommanderMission.AICommanderMissionCompletion.Interrupted:
			this.Interrupted();
			break;
		}
	}

	protected virtual void Running()
	{
		AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(this.AIDataArmyGUID);
		if (aidata == null || aidata.ArmyMission == null)
		{
			this.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return;
		}
		switch (aidata.ArmyMission.Completion)
		{
		case AIArmyMission.AIArmyMissionCompletion.Fail:
		{
			AIArmyMission.AIArmyMissionErrorCode errorCode = aidata.ArmyMission.ErrorCode;
			aidata.ArmyMission.Reset();
			TickableState state = TickableState.NoTick;
			this.Completion = this.GetCompletionFor(errorCode, out state);
			this.State = state;
			break;
		}
		case AIArmyMission.AIArmyMissionCompletion.Running:
			aidata.ArmyMission.Tick();
			break;
		case AIArmyMission.AIArmyMissionCompletion.Success:
		{
			aidata.ArmyMission.Reset();
			TickableState state2 = TickableState.NoTick;
			this.Completion = this.GetCompletionWhenSuccess(aidata, out state2);
			this.State = state2;
			break;
		}
		}
	}

	protected virtual void Success()
	{
		this.State = TickableState.NoTick;
	}

	protected virtual bool TryComputeArmyMissionParameter()
	{
		return false;
	}

	protected bool TryCreateArmyMission(string missionType, List<object> missionParameter)
	{
		if (this.AIDataArmyGUID == GameEntityGUID.Zero)
		{
			return false;
		}
		AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(this.AIDataArmyGUID);
		if (aidata == null)
		{
			this.AIDataArmyGUID = GameEntityGUID.Zero;
			return false;
		}
		IDatabase<AIArmyMissionDefinition> database = Databases.GetDatabase<AIArmyMissionDefinition>(false);
		AIArmyMissionDefinition value = database.GetValue(missionType);
		Diagnostics.Assert(value != null);
		if (value == null)
		{
			return false;
		}
		if (aidata.ArmyMission == null || aidata.ArmyMission.AIArmyMissionDefinition == null || aidata.ArmyMission.AIArmyMissionDefinition.Name != missionType)
		{
			aidata.AssignArmyMission(this.Commander, value, missionParameter.ToArray());
		}
		else if (!aidata.ArmyMission.TrySetParameters(missionParameter.ToArray()))
		{
			aidata.AssignArmyMission(this.Commander, value, missionParameter.ToArray());
		}
		return true;
	}

	protected virtual bool TryGetArmyData()
	{
		if (this.CheckArmyGuid(this.AIDataArmyGUID))
		{
			return true;
		}
		if (this.CheckArmyGuid(this.Commander.ForceArmyGUID))
		{
			this.AIDataArmyGUID = this.Commander.ForceArmyGUID;
			this.Commander.ForceArmyGUID = GameEntityGUID.Zero;
			return true;
		}
		this.AIDataArmyGUID = GameEntityGUID.Zero;
		return false;
	}

	private void AICommanderMission_ArmyCollectionChange(object sender, CollectionChangeEventArgs e)
	{
		if (e.Action == CollectionChangeAction.Remove)
		{
			Army army = e.Element as Army;
			if (this.AIDataArmyGUID == army.GUID)
			{
				this.ArmyLost();
			}
		}
	}

	private bool CheckArmyGuid(GameEntityGUID guid)
	{
		if (guid == GameEntityGUID.Zero)
		{
			return false;
		}
		AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(guid);
		if (aidata == null)
		{
			return false;
		}
		if (aidata.Army.Empire != this.Commander.Empire)
		{
			this.ArmyLost();
			return false;
		}
		if (aidata.CommanderMission != this && !aidata.AssignCommanderMission(this))
		{
			this.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return false;
		}
		return true;
	}

	protected IAIDataRepositoryAIHelper aiDataRepository;

	private DepartmentOfDefense departmentOfDefense;

	private ITickableRepositoryAIHelper tickableRepository;

	public enum AICommanderMissionCompletion
	{
		Initializing,
		Running,
		Pending,
		Success,
		Fail,
		Interrupted,
		Cancelled
	}
}
