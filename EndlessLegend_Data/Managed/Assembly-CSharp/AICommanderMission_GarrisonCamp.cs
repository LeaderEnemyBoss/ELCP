using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using UnityEngine;

public class AICommanderMission_GarrisonCamp : AICommanderMission
{
	public override void ReadXml(XmlReader reader)
	{
		int attribute = reader.GetAttribute<int>("RegionTargetIndex");
		this.region = null;
		if (attribute > -1)
		{
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			global::Game game = service.Game as global::Game;
			World world = game.World;
			this.region = world.Regions[attribute];
			Diagnostics.Assert(this.region != null);
		}
		ulong attribute2 = reader.GetAttribute<ulong>("RequestGarrisonCampMessageID");
		this.requestGarrisonCamp = null;
		if (attribute2 != 0UL)
		{
			Diagnostics.Assert(base.Commander != null);
			Diagnostics.Assert(base.Commander.AIPlayer != null);
			Diagnostics.Assert(base.Commander.AIPlayer.Blackboard != null);
			this.requestGarrisonCamp = (base.Commander.AIPlayer.Blackboard.GetMessage(attribute2) as RequestGarrisonCampMessage);
		}
		base.ReadXml(reader);
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<int>("RegionTargetIndex", (this.region != null) ? this.region.Index : -1);
		writer.WriteAttributeString<ulong>("RequestGarrisonCampMessageID", (this.requestGarrisonCamp != null) ? this.requestGarrisonCamp.ID : 0UL);
		base.WriteXml(writer);
	}

	public Camp Camp
	{
		get
		{
			if (this.region == null || this.region.City == null)
			{
				return null;
			}
			return this.region.City.Camp;
		}
	}

	public override AIParameter.AIModifier[] GetHeroItemModifiers()
	{
		return null;
	}

	public float GetUnitPriorityInCamp(int slotIndex)
	{
		if (this.Camp == null || base.Commander == null)
		{
			return 0f;
		}
		float num = AILayer_Military.GetCampDefenseLocalPriority(this.Camp, this.unitRatioBoost, AICommanderMission_GarrisonCamp.SimulatedUnitsCount);
		num *= (base.Commander as AICommanderWithObjective).GlobalPriority;
		return this.militaryLayer.GetUnitPriority(this.Camp, slotIndex, num, 0.5f);
	}

	public override void Initialize(AICommander commander)
	{
		base.Initialize(commander);
		Diagnostics.Assert(AIScheduler.Services != null);
		this.intelligenceAiHelper = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.pathfindingService = service.Game.Services.GetService<IPathfindingService>();
		this.personalityAIHelper = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		this.unitInGarrisonPercent = this.personalityAIHelper.GetRegistryValue<float>(base.Commander.Empire, string.Format("{0}/{1}", AILayer_Military.RegistryPath, "UnitInGarrisonPercent"), this.unitInGarrisonPercent);
		this.unitInGarrisonPriorityMultiplierPerSlot = this.personalityAIHelper.GetRegistryValue<float>(base.Commander.Empire, string.Format("{0}/{1}", AILayer_Military.RegistryPath, "UnitInGarrisonPriorityMultiplierPerSlot"), this.unitInGarrisonPriorityMultiplierPerSlot);
		this.unitRatioBoost = this.personalityAIHelper.GetRegistryValue<float>(base.Commander.Empire, string.Format("{0}/{1}", AILayer_Military.RegistryPath, "UnitRatioBoost"), this.unitRatioBoost);
		AIEntity_Empire entity = commander.AIPlayer.GetEntity<AIEntity_Empire>();
		this.militaryLayer = entity.GetLayer<AILayer_Military>();
	}

	public override void Promote()
	{
		base.Promote();
		if (this.IsMissionValid())
		{
			this.SendGarrisonRequest();
			return;
		}
		this.requestGarrisonCamp = null;
		this.Fail();
	}

	public override void Refresh()
	{
		base.Refresh();
		if (!this.IsMissionValid())
		{
			this.requestGarrisonCamp = null;
			this.Fail();
			return;
		}
		if (this.requestGarrisonCamp != null)
		{
			if (this.requestGarrisonCamp.State == BlackboardMessage.StateValue.Message_Canceled)
			{
				this.requestGarrisonCamp = null;
				this.SendGarrisonRequest();
			}
			if (this.Camp.StandardUnits.Count < this.Camp.MaximumUnitSlot)
			{
				this.requestGarrisonCamp.SetPriority(base.Commander.GetPriority(this));
				this.requestGarrisonCamp.TimeOut = 1;
			}
		}
		else
		{
			this.SendGarrisonRequest();
		}
		AIData_Camp aidata_Camp;
		if (this.aiDataRepository.TryGetAIData<AIData_Camp>(this.Camp.GUID, out aidata_Camp))
		{
			aidata_Camp.CommanderMission = this;
		}
	}

	public override void Release()
	{
		base.Release();
		if (this.requestGarrisonCamp != null)
		{
			if (base.Commander != null && base.Commander.AIPlayer != null && base.Commander.AIPlayer.Blackboard != null)
			{
				base.Commander.AIPlayer.Blackboard.CancelMessage(this.requestGarrisonCamp);
			}
			this.requestGarrisonCamp = null;
		}
		if (this.armyPattern != null)
		{
			this.armyPattern.Release();
			this.armyPattern = null;
		}
		this.intelligenceAiHelper = null;
		this.worldPositionningService = null;
	}

	public override void SetParameters(AICommanderMissionDefinition missionDefinition, params object[] parameters)
	{
		base.SetParameters(missionDefinition, parameters);
		this.region = (parameters[0] as Region);
		Diagnostics.Assert(this.region != null);
		Diagnostics.Assert(this.region.City != null);
		Diagnostics.Assert(this.region.City.Camp != null);
		AIData_Camp aidata_Camp;
		if (this.aiDataRepository.TryGetAIData<AIData_Camp>(this.region.City.GUID, out aidata_Camp))
		{
			aidata_Camp.CommanderMission = this;
		}
	}

	protected ArmyPattern CreateArmyPattern()
	{
		return this.intelligenceAiHelper.GenerateArmyPattern(base.Commander.Empire, 1f, false, 5, base.MissionDefinition.AIArmyPattern);
	}

	protected override void Fail()
	{
		base.Fail();
		base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
	}

	protected override void Running()
	{
		if (!base.AIDataArmyGUID.IsValid)
		{
			return;
		}
		base.Running();
	}

	protected void SendGarrisonRequest()
	{
		if (this.requestGarrisonCamp == null && this.Camp.StandardUnits.Count < this.Camp.MaximumUnitSlot)
		{
			RequestGarrisonCampMessage message = new RequestGarrisonCampMessage(base.Commander.Empire.Index, null, base.Commander.GetPriority(this), this.Camp.GUID, base.Commander.Category);
			this.requestGarrisonCamp = message;
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
			this.requestGarrisonCamp.ArmyPattern = this.armyPattern;
			this.requestGarrisonCamp.SetPriority(base.Commander.GetPriority(this));
			this.requestGarrisonCamp.ForceSourceRegion = this.Camp.City.Region.Index;
			if (this.Camp != null)
			{
				this.requestGarrisonCamp.FinalPosition = this.Camp.WorldPosition;
			}
			base.Commander.AIPlayer.Blackboard.AddMessage(message);
			this.armyPattern = null;
		}
	}

	protected override bool TryComputeArmyMissionParameter()
	{
		if (this.WaitingOnArmyTicket())
		{
			return false;
		}
		if (!base.AIDataArmyGUID.IsValid)
		{
			return this.AskForArmy(this.Camp.MaximumUnitSlot - this.Camp.UnitsCount);
		}
		return this.DisbandArmy();
	}

	protected override AICommanderMission.AICommanderMissionCompletion GetCompletionFor(AIArmyMission.AIArmyMissionErrorCode errorCode, out TickableState tickableState)
	{
		return base.GetCompletionFor(errorCode, out tickableState);
	}

	protected override bool TryGetArmyData()
	{
		if (!this.IsMissionValid())
		{
			this.requestGarrisonCamp = null;
			this.Fail();
			return false;
		}
		if (this.requestGarrisonCamp == null)
		{
			this.SendGarrisonRequest();
		}
		if (base.AIDataArmyGUID.IsValid)
		{
			AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(base.AIDataArmyGUID);
			if (aidata != null && aidata.CommanderMission != null && aidata.CommanderMission != this)
			{
				base.AIDataArmyGUID = GameEntityGUID.Zero;
			}
		}
		if (base.AIDataArmyGUID.IsValid)
		{
			return true;
		}
		if (this.State == TickableState.NeedTick)
		{
			this.State = TickableState.Optional;
		}
		return true;
	}

	private bool AskForArmy(int numberOfUnits)
	{
		if (!base.AIDataArmyGUID.IsValid && numberOfUnits > 0 && this.armySpawnTicket == null)
		{
			PathfindingContext pathfindingContext = new PathfindingContext(this.Camp.City.GUID, this.Camp.City.Empire, this.Camp.City.StandardUnits);
			WorldPosition armyPosition;
			if (!DepartmentOfTheInterior.TryGetWorldPositionForNewArmyFromCity(this.Camp.City, this.pathfindingService, pathfindingContext, out armyPosition))
			{
				return false;
			}
			if (this.Camp.City.StandardUnits.Count == 0)
			{
				return false;
			}
			numberOfUnits = Mathf.Min(this.Camp.City.StandardUnits.Count, numberOfUnits);
			if (numberOfUnits > 0)
			{
				GameEntityGUID[] array = new GameEntityGUID[numberOfUnits];
				for (int i = 0; i < numberOfUnits; i++)
				{
					array[i] = this.Camp.City.StandardUnits[i].GUID;
				}
				OrderTransferGarrisonToNewArmy order = new OrderTransferGarrisonToNewArmy(base.Commander.Empire.Index, this.Camp.City.GUID, array, armyPosition, null, false);
				base.Commander.Empire.PlayerControllers.AI.PostOrder(order, out this.armySpawnTicket, null);
			}
		}
		return false;
	}

	private bool DisbandArmy()
	{
		return base.AIDataArmyGUID.IsValid && base.TryCreateArmyMission("DefendCamp_Bail", new List<object>
		{
			this.Camp.WorldPosition
		});
	}

	private bool IsMissionValid()
	{
		return this.Camp != null && this.Camp.Empire == base.Commander.Empire;
	}

	private bool WaitingOnArmyTicket()
	{
		if (this.armySpawnTicket != null && this.armySpawnTicket.Raised)
		{
			if (this.armySpawnTicket.PostOrderResponse == PostOrderResponse.Processed)
			{
				OrderTransferGarrisonToNewArmy orderTransferGarrisonToNewArmy = this.armySpawnTicket.Order as OrderTransferGarrisonToNewArmy;
				if (this.armySpawnTicket != null)
				{
					base.AIDataArmyGUID = orderTransferGarrisonToNewArmy.ArmyGuid;
				}
			}
			this.armySpawnTicket = null;
		}
		return this.armySpawnTicket != null;
	}

	public static int SimulatedUnitsCount;

	private ArmyPattern armyPattern;

	private Ticket armySpawnTicket;

	private IIntelligenceAIHelper intelligenceAiHelper;

	private AILayer_Military militaryLayer;

	private IPathfindingService pathfindingService;

	private IPersonalityAIHelper personalityAIHelper;

	private Region region;

	private RequestGarrisonCampMessage requestGarrisonCamp;

	private float unitInGarrisonPercent = 0.25f;

	private float unitInGarrisonPriorityMultiplierPerSlot = 0.5f;

	private float unitRatioBoost = 0.8f;

	private IWorldPositionningService worldPositionningService;
}
