using System;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;

public abstract class AICommanderMissionWithRequestArmy : AICommanderMission
{
	public AICommanderMissionWithRequestArmy()
	{
		this.AllowRetrofit = true;
		this.MinimumNeededArmyFulfillement = 0.95f;
	}

	public bool AllowRetrofit { get; set; }

	public float MinimumNeededArmyFulfillement { get; set; }

	public void ComputeNeededArmyPower(out float minMilitaryPower, out bool isMaxPower, out bool perUnitTest)
	{
		this.GetNeededArmyPower(out minMilitaryPower, out isMaxPower, out perUnitTest);
	}

	public virtual WorldPosition GetTargetPositionForTheArmy()
	{
		return WorldPosition.Invalid;
	}

	public bool HasSentArmyRequestMessage(ulong requestMessageID)
	{
		return this.requestArmy != null && this.requestArmy.ID == requestMessageID;
	}

	public override void Initialize(AICommander commander)
	{
		base.Initialize(commander);
		this.departmentOfTheInterior = commander.Empire.GetAgency<DepartmentOfTheInterior>();
		this.intelligenceAIHelper = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		this.personalityMPTenacity = 0.8f;
		IGameService service = Services.GetService<IGameService>();
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
	}

	public override void Promote()
	{
		base.Promote();
		if (base.AIDataArmyGUID == GameEntityGUID.Zero && base.Commander.ForceArmyGUID == GameEntityGUID.Zero)
		{
			this.SendArmyRequest();
		}
	}

	public override void ReadXml(XmlReader reader)
	{
		ulong attribute = reader.GetAttribute<ulong>("RequestArmyMessageID");
		this.requestArmy = null;
		if (attribute != 0UL)
		{
			Diagnostics.Assert(base.Commander != null);
			Diagnostics.Assert(base.Commander.AIPlayer != null);
			Diagnostics.Assert(base.Commander.AIPlayer.Blackboard != null);
			this.requestArmy = (base.Commander.AIPlayer.Blackboard.GetMessage(attribute) as RequestArmyMessage);
		}
		base.ReadXml(reader);
	}

	public override void Refresh()
	{
		base.Refresh();
		if (this.requestArmy != null)
		{
			this.requestArmy.SetPriority(base.Commander.GetPriority(this));
			this.requestArmy.TimeOut = 1;
			float power;
			bool maxPower;
			bool perUnitTest;
			this.GetNeededArmyPower(out power, out maxPower, out perUnitTest);
			this.requestArmy.ArmyPattern.Refresh(power, maxPower, perUnitTest, this.GetNeededAvailabilityTime());
		}
	}

	public override void Release()
	{
		if (this.requestArmy != null)
		{
			base.Commander.AIPlayer.Blackboard.CancelMessage(this.requestArmy);
			this.requestArmy = null;
		}
		if (this.armyPattern != null)
		{
			this.armyPattern.Release();
			this.armyPattern = null;
		}
		base.Release();
		this.departmentOfTheInterior = null;
		this.intelligenceAIHelper = null;
		this.gameEntityRepositoryService = null;
	}

	public virtual void SetExtraArmyRequestInformation()
	{
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<ulong>("RequestArmyMessageID", (this.requestArmy != null) ? this.requestArmy.ID : 0UL);
		base.WriteXml(writer);
	}

	protected abstract void GetNeededArmyPower(out float minMilitaryPower, out bool isMaxPower, out bool perUnitTest);

	protected abstract int GetNeededAvailabilityTime();

	protected virtual bool IsMissionCompleted()
	{
		return false;
	}

	protected virtual bool MissionCanAcceptHero()
	{
		return true;
	}

	protected override void Running()
	{
		AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(base.AIDataArmyGUID);
		if (aidata == null || aidata.ArmyMission == null || aidata.Army == null)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Interrupted;
			return;
		}
		int num = (int)base.Commander.Empire.GetPropertyValue(SimulationProperties.ArmyUnitSlot);
		int count = aidata.Army.StandardUnits.Count;
		if (num != count)
		{
			float num2;
			bool flag;
			bool flag2;
			this.GetNeededArmyPower(out num2, out flag, out flag2);
			if (flag)
			{
				base.Completion = AICommanderMission.AICommanderMissionCompletion.Interrupted;
				return;
			}
			float num3 = 0f;
			float num4 = 0f;
			foreach (AICommanderMission aicommanderMission in base.Commander.Missions)
			{
				IGameEntity gameEntity = null;
				if (aicommanderMission.AIDataArmyGUID.IsValid && this.gameEntityRepositoryService.TryGetValue(aicommanderMission.AIDataArmyGUID, out gameEntity) && gameEntity is Army)
				{
					Army army = gameEntity as Army;
					if (army.GUID.IsValid)
					{
						float num5 = this.intelligenceAIHelper.EvaluateMilitaryPowerOfGarrison(base.Commander.Empire, army, 0);
						num3 += num5;
						if (army.GUID == aidata.Army.GUID)
						{
							num4 = num5;
						}
					}
				}
			}
			num2 *= this.personalityMPTenacity;
			if (num2 > num3 || ((float)count < 0.5f * (float)num && num2 > num4))
			{
				base.Completion = AICommanderMission.AICommanderMissionCompletion.Interrupted;
				return;
			}
		}
		if (this.IsMissionCompleted())
		{
			base.State = TickableState.NoTick;
			aidata.ResetArmyMission();
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Success;
			return;
		}
		base.Running();
	}

	protected void SendArmyRequest()
	{
		if (this.requestArmy == null)
		{
			RequestArmyMessage message = new RequestArmyMessage(base.Commander.Empire.Index, null, base.Commander.GetPriority(this), base.Commander.Category);
			this.requestArmy = message;
			if (this.armyPattern == null)
			{
				this.armyPattern = this.CreateArmyPattern();
			}
			if (this.armyPattern == null)
			{
				Diagnostics.LogError("Pattern is null. {0}", new object[]
				{
					this.ToString()
				});
				return;
			}
			this.requestArmy.ArmyPattern = this.armyPattern;
			this.requestArmy.SetPriority(base.Commander.GetPriority(this));
			this.requestArmy.MinimumNeededArmyFulfillement = this.MinimumNeededArmyFulfillement;
			this.requestArmy.FinalPosition = this.GetTargetPositionForTheArmy();
			this.requestArmy.CanAcceptHero = this.MissionCanAcceptHero();
			this.SetExtraArmyRequestInformation();
			base.Commander.AIPlayer.Blackboard.AddMessage(message);
			this.armyPattern = null;
		}
	}

	protected override bool TryGetArmyData()
	{
		if (base.TryGetArmyData())
		{
			return true;
		}
		if (this.requestArmy != null)
		{
			if (this.requestArmy.State == BlackboardMessage.StateValue.Message_Canceled)
			{
				this.requestArmy = null;
				return false;
			}
			this.requestArmy.SetPriority(base.Commander.GetPriority(this));
			if (this.requestArmy.ExecutionState == RequestUnitListMessage.RequestUnitListState.ArmyAvailable)
			{
				GameEntityGUID armyGUID = this.requestArmy.ArmyGUID;
				this.requestArmy.State = BlackboardMessage.StateValue.Message_Success;
				this.requestArmy.TimeOut = 0;
				this.requestArmy = null;
				AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(armyGUID);
				if (aidata == null)
				{
					base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
					return false;
				}
				if (!aidata.AssignCommanderMission(this))
				{
					base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
					return false;
				}
				base.AIDataArmyGUID = armyGUID;
				return true;
			}
			else if (this.requestArmy.ExecutionState == RequestUnitListMessage.RequestUnitListState.Regrouping)
			{
				base.State = TickableState.NeedTick;
			}
			else if (this.requestArmy.ExecutionState == RequestUnitListMessage.RequestUnitListState.RegroupingPending)
			{
				base.State = TickableState.NoTick;
			}
			else if (this.requestArmy.ExecutionState == RequestUnitListMessage.RequestUnitListState.Pending)
			{
				base.State = TickableState.NoTick;
			}
		}
		else
		{
			this.SendArmyRequest();
		}
		return false;
	}

	private ArmyPattern CreateArmyPattern()
	{
		float minArmyPower;
		bool flag;
		bool perUnitTest;
		this.GetNeededArmyPower(out minArmyPower, out flag, out perUnitTest);
		if (base.MissionDefinition != null)
		{
			if (flag)
			{
				return this.intelligenceAIHelper.GenerateMaxPowerArmyPattern(base.Commander.Empire, perUnitTest, this.GetNeededAvailabilityTime(), base.MissionDefinition.AIArmyPattern);
			}
			return this.intelligenceAIHelper.GenerateArmyPattern(base.Commander.Empire, minArmyPower, perUnitTest, this.GetNeededAvailabilityTime(), base.MissionDefinition.AIArmyPattern);
		}
		else
		{
			if (flag)
			{
				return this.intelligenceAIHelper.GenerateMaxPowerArmyPattern(base.Commander.Empire, perUnitTest, this.GetNeededAvailabilityTime(), null);
			}
			return this.intelligenceAIHelper.GenerateArmyPattern(base.Commander.Empire, minArmyPower, perUnitTest, this.GetNeededAvailabilityTime(), null);
		}
	}

	protected DepartmentOfTheInterior departmentOfTheInterior;

	protected IIntelligenceAIHelper intelligenceAIHelper;

	protected RequestArmyMessage requestArmy;

	private ArmyPattern armyPattern;

	private float personalityMPTenacity;

	private IGameEntityRepositoryService gameEntityRepositoryService;
}
