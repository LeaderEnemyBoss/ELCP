using System;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class AICommander_KaijuAdquisition : AICommanderWithObjective, IXmlSerializable
{
	public AICommander_KaijuAdquisition(ulong globalObjectiveID, int regionIndex) : base(AICommanderMissionDefinition.AICommanderCategory.KaijuAdquisition, globalObjectiveID, regionIndex)
	{
		this.kaiju = null;
	}

	public AICommander_KaijuAdquisition() : base(AICommanderMissionDefinition.AICommanderCategory.KaijuAdquisition, 0UL, 0)
	{
	}

	public override void ReadXml(XmlReader reader)
	{
		int attribute = reader.GetAttribute<int>("RegionTargetIndex");
		this.attackKaijuMessageId = reader.GetAttribute<ulong>("AttackKaijuMessageId");
		GameEntityGUID guid = reader.GetAttribute<ulong>("TargetGUID");
		if (guid.IsValid)
		{
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
			Diagnostics.Assert(service2 != null);
			IGameEntity gameEntity;
			service2.TryGetValue(guid, out gameEntity);
			this.kaiju = (gameEntity as Kaiju);
			Diagnostics.Assert(this.kaiju != null);
		}
		base.ReadXml(reader);
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<ulong>("AttackKaijuMessageId", this.attackKaijuMessageId);
		writer.WriteAttributeString<ulong>("TargetGUID", (this.Kaiju != null) ? this.Kaiju.GUID : GameEntityGUID.Zero);
		base.WriteXml(writer);
	}

	public Kaiju Kaiju
	{
		get
		{
			return this.kaiju;
		}
	}

	public bool HuntTamedKaijus
	{
		get
		{
			return this.huntTamedKaijus;
		}
		private set
		{
			this.huntTamedKaijus = value;
		}
	}

	public override void Initialize()
	{
		base.Initialize();
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.empireDataRepository = AIScheduler.Services.GetService<IAIEmpireDataAIHelper>();
		Diagnostics.Assert(this.empireDataRepository != null);
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		Diagnostics.Assert(this.gameEntityRepositoryService != null);
		this.worldPositioningService = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositioningService != null);
		this.terraformDeviceService = service.Game.Services.GetService<ITerraformDeviceService>();
		Diagnostics.Assert(this.terraformDeviceService != null);
		this.intelligenceAIHelper = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		Diagnostics.Assert(this.intelligenceAIHelper != null);
		this.personalityAIHelper = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		Diagnostics.Assert(this.personalityAIHelper != null);
		this.kaijuMilitaryPowerFactor = this.personalityAIHelper.GetRegistryValue<float>(base.Empire, string.Format("{0}/{1}", AICommander_KaijuAdquisition.registryPath, "KaijuMilitaryPowerFactor"), this.kaijuMilitaryPowerFactor);
		this.maxArmiesHuntingSameKaiju = this.personalityAIHelper.GetRegistryValue<int>(base.Empire, string.Format("{0}/{1}", AICommander_KaijuAdquisition.registryPath, "CommanderMaxArmiesHuntingSameKaiju"), this.maxArmiesHuntingSameKaiju);
		this.HuntTamedKaijus = (this.personalityAIHelper.GetRegistryValue<int>(base.Empire, string.Format("{0}/{1}", AICommander_KaijuAdquisition.registryPath, "KaijuAdquisitionIncludeTamedKaijus"), 0) != 0);
	}

	public override void PopulateMission()
	{
		Tags tags = new Tags();
		tags.AddTag(base.Category.ToString());
		GlobalObjectiveMessage globalObjectiveMessage;
		if (this.kaiju == null && base.AIPlayer.Blackboard.TryGetMessage<GlobalObjectiveMessage>(base.GlobalObjectiveID, out globalObjectiveMessage) && !this.gameEntityRepositoryService.TryGetValue<Kaiju>(globalObjectiveMessage.SubObjectifGUID, out this.kaiju))
		{
			base.AIPlayer.Blackboard.CancelMessage(globalObjectiveMessage);
			return;
		}
		for (int i = base.Missions.Count - 1; i >= 0; i--)
		{
			if (base.Missions[i].MissionDefinition.Category.Contains(tags))
			{
				return;
			}
			this.CancelMission(base.Missions[i]);
		}
		float armyMaxPower = this.GetArmyMaxPower();
		int num = Mathf.CeilToInt(this.intelligenceAIHelper.EvaluateMilitaryPowerOfKaiju(base.Empire, this.kaiju, 0) * this.kaijuMilitaryPowerFactor / armyMaxPower);
		if (num < 2)
		{
			num = 2;
		}
		else if (num > this.maxArmiesHuntingSameKaiju)
		{
			num = this.maxArmiesHuntingSameKaiju;
		}
		int num2 = 0;
		int num3 = 0;
		for (int j = 0; j < base.Missions.Count; j++)
		{
			AICommanderMission_AttackAndTameKaijuDefault aicommanderMission_AttackAndTameKaijuDefault = base.Missions[j] as AICommanderMission_AttackAndTameKaijuDefault;
			if (aicommanderMission_AttackAndTameKaijuDefault != null)
			{
				if (num2 < num)
				{
					if (!aicommanderMission_AttackAndTameKaijuDefault.AIDataArmyGUID.IsValid)
					{
						num3++;
					}
				}
				else if (num2 >= num + 1)
				{
					this.CancelMission(aicommanderMission_AttackAndTameKaijuDefault);
					goto IL_15D;
				}
				num2++;
			}
			IL_15D:;
		}
		GlobalObjectiveMessage globalObjectiveMessage3;
		if (num2 - num3 >= num)
		{
			GlobalObjectiveMessage globalObjectiveMessage2;
			if (base.AIPlayer.Blackboard.TryGetMessage<GlobalObjectiveMessage>(base.GlobalObjectiveID, out globalObjectiveMessage2))
			{
				globalObjectiveMessage2.ObjectiveState = "Attacking";
			}
		}
		else if (base.AIPlayer.Blackboard.TryGetMessage<GlobalObjectiveMessage>(base.GlobalObjectiveID, out globalObjectiveMessage3))
		{
			globalObjectiveMessage3.ObjectiveState = "Preparing";
		}
		for (int k = num2; k < num; k++)
		{
			this.SendAttackKaijuAction();
			base.PopulationFirstMissionFromCategory(tags, new object[]
			{
				this.kaiju
			});
		}
	}

	private void SendAttackKaijuAction()
	{
		EvaluableMessage_AttackAndTameKaiju evaluableMessage_AttackAndTameKaiju = base.AIPlayer.Blackboard.FindFirst<EvaluableMessage_AttackAndTameKaiju>(BlackboardLayerID.Empire, (EvaluableMessage_AttackAndTameKaiju match) => match.RegionIndex == base.RegionIndex && match.KaijuGUID == this.kaiju.GUID);
		if (evaluableMessage_AttackAndTameKaiju == null || evaluableMessage_AttackAndTameKaiju.EvaluationState == EvaluableMessage.EvaluableMessageState.Cancel)
		{
			evaluableMessage_AttackAndTameKaiju = new EvaluableMessage_AttackAndTameKaiju(base.RegionIndex, this.kaiju.GUID);
			this.attackKaijuMessageId = base.AIPlayer.Blackboard.AddMessage(evaluableMessage_AttackAndTameKaiju);
		}
		else
		{
			this.attackKaijuMessageId = evaluableMessage_AttackAndTameKaiju.ID;
		}
		evaluableMessage_AttackAndTameKaiju.TimeOut = 1;
		evaluableMessage_AttackAndTameKaiju.SetInterest(base.GlobalPriority, base.LocalPriority);
	}

	public override bool IsMissionFinished(bool forceStep)
	{
		if (this.attackKaijuMessageId != 0UL)
		{
			EvaluableMessage_AttackAndTameKaiju evaluableMessage_AttackAndTameKaiju = base.AIPlayer.Blackboard.GetMessage(this.attackKaijuMessageId) as EvaluableMessage_AttackAndTameKaiju;
			if (evaluableMessage_AttackAndTameKaiju != null && (evaluableMessage_AttackAndTameKaiju.State == BlackboardMessage.StateValue.Message_Canceled || evaluableMessage_AttackAndTameKaiju.State == BlackboardMessage.StateValue.Message_Failed || evaluableMessage_AttackAndTameKaiju.State == BlackboardMessage.StateValue.Message_Success))
			{
				return true;
			}
		}
		GlobalObjectiveMessage globalObjectiveMessage;
		return base.GlobalObjectiveID == 0UL || base.AIPlayer == null || !base.AIPlayer.Blackboard.TryGetMessage<GlobalObjectiveMessage>(base.GlobalObjectiveID, out globalObjectiveMessage) || (globalObjectiveMessage.State == BlackboardMessage.StateValue.Message_Canceled || globalObjectiveMessage.State == BlackboardMessage.StateValue.Message_Failed);
	}

	public override void Release()
	{
		GlobalObjectiveMessage globalObjectiveMessage;
		if (this.IsMissionFinished(false) && base.AIPlayer.Blackboard.TryGetMessage<GlobalObjectiveMessage>(base.GlobalObjectiveID, out globalObjectiveMessage))
		{
			globalObjectiveMessage.State = BlackboardMessage.StateValue.Message_Canceled;
		}
		if (this.attackKaijuMessageId != 0UL)
		{
			EvaluableMessage_AttackAndTameKaiju evaluableMessage_AttackAndTameKaiju = base.AIPlayer.Blackboard.GetMessage(this.attackKaijuMessageId) as EvaluableMessage_AttackAndTameKaiju;
			if (evaluableMessage_AttackAndTameKaiju != null)
			{
				if (evaluableMessage_AttackAndTameKaiju.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate || evaluableMessage_AttackAndTameKaiju.EvaluationState == EvaluableMessage.EvaluableMessageState.Obtaining || evaluableMessage_AttackAndTameKaiju.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending)
				{
					evaluableMessage_AttackAndTameKaiju.Cancel();
				}
				base.AIPlayer.Blackboard.CancelMessage(this.attackKaijuMessageId);
			}
		}
		this.kaiju = null;
		base.Release();
	}

	public override void RefreshMission()
	{
		base.RefreshMission();
		if (!this.IsMissionFinished(false))
		{
			this.PopulateMission();
			base.PromoteMission();
		}
	}

	public string GetAttackKaijuActionState()
	{
		if (this.attackKaijuMessageId == 0UL)
		{
			return "No Kaiju Attack Action";
		}
		EvaluableMessage_AttackAndTameKaiju evaluableMessage_AttackAndTameKaiju = base.AIPlayer.Blackboard.FindFirst<EvaluableMessage_AttackAndTameKaiju>(BlackboardLayerID.Empire, (EvaluableMessage_AttackAndTameKaiju match) => match.RegionIndex == base.RegionIndex && match.KaijuGUID == base.SubObjectiveGuid && match.AccountTag == AILayer_AccountManager.MilitaryAccountName);
		if (evaluableMessage_AttackAndTameKaiju == null)
		{
			return "Strangely Attack Kaiju Action is no longer valid";
		}
		string text = "Attack Kaiju Action State = " + evaluableMessage_AttackAndTameKaiju.EvaluationState.ToString();
		if (evaluableMessage_AttackAndTameKaiju.ChosenBuyEvaluation != null)
		{
			text = text + " Purchased for " + evaluableMessage_AttackAndTameKaiju.ChosenBuyEvaluation.DustCost;
		}
		else
		{
			text += " Evaluating Purchase";
		}
		return text;
	}

	private float GetArmyMaxPower()
	{
		AIEmpireData aiempireData;
		if (this.empireDataRepository.TryGet(base.Empire.Index, out aiempireData))
		{
			return aiempireData.AverageUnitDesignMilitaryPower * base.Empire.GetPropertyValue(SimulationProperties.ArmyUnitSlot) * base.Empire.GetPropertyValue(SimulationProperties.ArmyUnitSlot);
		}
		return float.MaxValue;
	}

	private static string registryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_War/AICommander";

	private IAIEmpireDataAIHelper empireDataRepository;

	private IIntelligenceAIHelper intelligenceAIHelper;

	private IPersonalityAIHelper personalityAIHelper;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private IWorldPositionningService worldPositioningService;

	private ITerraformDeviceService terraformDeviceService;

	private Kaiju kaiju;

	private float kaijuMilitaryPowerFactor = 2f;

	private int maxArmiesHuntingSameKaiju = 4;

	private bool huntTamedKaijus;

	private ulong attackKaijuMessageId;
}
