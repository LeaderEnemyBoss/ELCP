using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using UnityEngine;

public class AICommanderMission_Garrison : AICommanderMission
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
		ulong attribute2 = reader.GetAttribute<ulong>("RequestGarrisonMessageID");
		this.requestGarrison = null;
		if (attribute2 != 0UL)
		{
			Diagnostics.Assert(base.Commander != null);
			Diagnostics.Assert(base.Commander.AIPlayer != null);
			Diagnostics.Assert(base.Commander.AIPlayer.Blackboard != null);
			this.requestGarrison = (base.Commander.AIPlayer.Blackboard.GetMessage(attribute2) as RequestGarrisonMessage);
		}
		base.ReadXml(reader);
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<int>("RegionTargetIndex", (this.region != null) ? this.region.Index : -1);
		writer.WriteAttributeString<ulong>("RequestGarrisonMessageID", (this.requestGarrison != null) ? this.requestGarrison.ID : 0UL);
		base.WriteXml(writer);
	}

	public City City
	{
		get
		{
			if (this.region == null)
			{
				return null;
			}
			return this.region.City;
		}
	}

	public override AIParameter.AIModifier[] GetHeroItemModifiers()
	{
		return null;
	}

	public float GetUnitPriorityInCity(int slotIndex)
	{
		if (this.City == null || base.Commander == null)
		{
			return 0f;
		}
		float num = AILayer_Military.GetCityDefenseLocalPriority(this.City, this.unitRatioBoost, AICommanderMission_Garrison.SimulatedUnitsCount);
		num *= (base.Commander as AICommanderWithObjective).GlobalPriority;
		return this.militaryLayer.GetUnitPriority(this.City, slotIndex, num, 0.5f);
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
		this.requestGarrison = null;
		this.Fail();
	}

	public override void Refresh()
	{
		base.Refresh();
		if (!this.IsMissionValid())
		{
			this.requestGarrison = null;
			this.Fail();
			return;
		}
		if (this.requestGarrison != null)
		{
			if (this.requestGarrison.State == BlackboardMessage.StateValue.Message_Canceled)
			{
				this.requestGarrison = null;
				this.SendGarrisonRequest();
			}
			if (this.City.StandardUnits.Count < this.region.City.MaximumUnitSlot)
			{
				this.requestGarrison.SetPriority(base.Commander.GetPriority(this));
				this.requestGarrison.TimeOut = 1;
			}
		}
		else
		{
			this.SendGarrisonRequest();
		}
		AIData_City aidata_City;
		if (this.aiDataRepository.TryGetAIData<AIData_City>(this.region.City.GUID, out aidata_City))
		{
			aidata_City.CommanderMission = this;
		}
	}

	public override void Release()
	{
		base.Release();
		if (this.requestGarrison != null)
		{
			if (base.Commander != null && base.Commander.AIPlayer != null && base.Commander.AIPlayer.Blackboard != null)
			{
				base.Commander.AIPlayer.Blackboard.CancelMessage(this.requestGarrison);
			}
			this.requestGarrison = null;
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
		AIData_City aidata_City;
		if (this.aiDataRepository.TryGetAIData<AIData_City>(this.region.City.GUID, out aidata_City))
		{
			aidata_City.CommanderMission = this;
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
		if (this.requestGarrison == null && this.City.StandardUnits.Count < this.region.City.MaximumUnitSlot)
		{
			RequestGarrisonMessage message = new RequestGarrisonMessage(base.Commander.Empire.Index, null, base.Commander.GetPriority(this), this.City.GUID, base.Commander.Category);
			this.requestGarrison = message;
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
			this.requestGarrison.ArmyPattern = this.armyPattern;
			this.requestGarrison.SetPriority(base.Commander.GetPriority(this));
			this.requestGarrison.ForceSourceRegion = this.City.Region.Index;
			if (this.City != null)
			{
				this.requestGarrison.FinalPosition = this.City.GetValidDistrictToTarget(null).WorldPosition;
			}
			else
			{
				this.requestGarrison.FinalPosition = WorldPosition.Invalid;
			}
			base.Commander.AIPlayer.Blackboard.AddMessage(message);
			this.armyPattern = null;
		}
	}

	protected override bool TryComputeArmyMissionParameter()
	{
		bool flag = false;
		int numberOfUnits = this.City.StandardUnits.Count;
		this.besiegers = null;
		if (this.WaitingOnArmyTicket())
		{
			return false;
		}
		if (this.City.BesiegingEmpire != null)
		{
		}
		if (!flag)
		{
			int num = this.City.UnitsCount;
			if (base.AIDataArmyGUID.IsValid)
			{
				AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(base.AIDataArmyGUID);
				if (aidata != null)
				{
					num += aidata.Army.UnitsCount;
				}
			}
			if ((float)num > (float)this.City.MaximumUnitSlot * 0.8f)
			{
				flag = true;
				numberOfUnits = Mathf.CeilToInt((float)this.City.StandardUnits.Count * 0.3f);
			}
		}
		if (!flag)
		{
			return this.DisbandArmy();
		}
		if (!base.AIDataArmyGUID.IsValid)
		{
			return this.AskForArmy(numberOfUnits);
		}
		return base.TryCreateArmyMission("MajorFactionRoaming", new List<object>
		{
			this.City.Region.Index,
			false
		});
	}

	protected override AICommanderMission.AICommanderMissionCompletion GetCompletionFor(AIArmyMission.AIArmyMissionErrorCode errorCode, out TickableState tickableState)
	{
		return base.GetCompletionFor(errorCode, out tickableState);
	}

	protected override bool TryGetArmyData()
	{
		if (!this.IsMissionValid())
		{
			this.requestGarrison = null;
			this.Fail();
			return false;
		}
		if (this.requestGarrison == null)
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
		if (this.City.BesiegingEmpire != null)
		{
			return true;
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
		if (!base.AIDataArmyGUID.IsValid && this.armySpawnTicket == null)
		{
			if (this.City.BesiegingEmpire != null)
			{
				int maxValue = int.MaxValue;
				for (int i = 0; i < this.City.Districts.Count; i++)
				{
					if (District.IsACityTile(this.City.Districts[i]))
					{
						Army armyAtPosition = this.worldPositionningService.GetArmyAtPosition(this.City.Districts[i].WorldPosition);
						if (armyAtPosition != null && armyAtPosition.Empire == this.City.Empire)
						{
							int distanceToBesieger = this.GetDistanceToBesieger(this.City.Districts[i].WorldPosition);
							if (distanceToBesieger < maxValue)
							{
								base.AIDataArmyGUID = armyAtPosition.GUID;
							}
							if (distanceToBesieger == 1)
							{
								return false;
							}
						}
					}
				}
				if (base.AIDataArmyGUID.IsValid)
				{
					return false;
				}
			}
			PathfindingContext pathfindingContext = new PathfindingContext(this.City.GUID, this.City.Empire, this.City.StandardUnits);
			WorldPosition armyPosition;
			if (!DepartmentOfTheInterior.TryGetWorldPositionForNewArmyFromCity(this.City, this.pathfindingService, pathfindingContext, out armyPosition))
			{
				return false;
			}
			if (this.City.StandardUnits.Count == 0)
			{
				return false;
			}
			numberOfUnits = Mathf.Min(this.City.StandardUnits.Count, numberOfUnits);
			GameEntityGUID[] array = new GameEntityGUID[numberOfUnits];
			for (int j = 0; j < numberOfUnits; j++)
			{
				array[j] = this.City.StandardUnits[j].GUID;
			}
			OrderTransferGarrisonToNewArmy order = new OrderTransferGarrisonToNewArmy(base.Commander.Empire.Index, this.City.GUID, array, armyPosition, null, false, true, true);
			base.Commander.Empire.PlayerControllers.AI.PostOrder(order, out this.armySpawnTicket, null);
		}
		return false;
	}

	private bool DisbandArmy()
	{
		return base.AIDataArmyGUID.IsValid && base.TryCreateArmyMission("DefendCity_Bail", new List<object>
		{
			this.City
		});
	}

	private bool IsMissionValid()
	{
		return this.City != null && this.City.Empire == base.Commander.Empire;
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

	private int GetDistanceToBesieger(WorldPosition position)
	{
		if (this.besiegers == null)
		{
			return 0;
		}
		int num = int.MaxValue;
		for (int i = 0; i < this.besiegers.Length; i++)
		{
			int distance = this.worldPositionningService.GetDistance(position, this.besiegers[i].WorldPosition);
			if (distance < num)
			{
				num = distance;
			}
		}
		return num;
	}

	public static int SimulatedUnitsCount;

	private ArmyPattern armyPattern;

	private Ticket armySpawnTicket;

	private IIntelligenceAIHelper intelligenceAiHelper;

	private AILayer_Military militaryLayer;

	private IPathfindingService pathfindingService;

	private IPersonalityAIHelper personalityAIHelper;

	private Region region;

	private RequestGarrisonMessage requestGarrison;

	private float unitInGarrisonPercent = 0.25f;

	private float unitInGarrisonPriorityMultiplierPerSlot = 0.5f;

	private float unitRatioBoost = 0.8f;

	private IWorldPositionningService worldPositionningService;

	private Army[] besiegers;
}
