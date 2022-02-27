using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;

public class AICommanderMission_GarrisonBreakSiege : AICommanderMission
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
	}

	public override void Promote()
	{
		base.Promote();
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
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Initializing;
			return;
		}
		base.Running();
	}

	protected override bool TryComputeArmyMissionParameter()
	{
		this.besiegers = null;
		if (this.City.BesiegingEmpire != null)
		{
			this.besiegers = DepartmentOfTheInterior.GetBesiegers(this.City);
			if (this.besiegers.Length == 0)
			{
				return false;
			}
			float num = 0f;
			float num2 = 0f;
			Garrison defender = this.besiegers[0];
			this.intelligenceAiHelper.EstimateMPInBattleground(this.City, defender, ref num, ref num2);
			if (num > num2)
			{
				return this.AskForArmy(true);
			}
			float propertyValue = this.City.GetPropertyValue(SimulationProperties.CityDefensePoint);
			float besiegingPower = DepartmentOfTheInterior.GetBesiegingPower(this.City, true);
			if (propertyValue <= besiegingPower)
			{
				return this.AskForArmy(true);
			}
		}
		else
		{
			if (base.AIDataArmyGUID.IsValid)
			{
				return this.DisbandArmy();
			}
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Success;
		}
		return false;
	}

	protected override bool TryGetArmyData()
	{
		if (!this.IsMissionValid())
		{
			this.requestGarrison = null;
			this.Fail();
			return false;
		}
		if (this.City.BesiegingEmpire != null)
		{
			return true;
		}
		if (base.AIDataArmyGUID.IsValid)
		{
			return true;
		}
		base.Completion = AICommanderMission.AICommanderMissionCompletion.Success;
		return false;
	}

	private bool AskForArmy(bool mustAttack = false)
	{
		if (!base.AIDataArmyGUID.IsValid)
		{
			if (this.armySpawnTicket == null)
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
				WorldPosition invalid = WorldPosition.Invalid;
				PathfindingContext pathfindingContext = new PathfindingContext(this.City.GUID, this.City.Empire, this.City.StandardUnits);
				if (!DepartmentOfTheInterior.TryGetWorldPositionForNewArmyFromCity(this.City, this.pathfindingService, pathfindingContext, out invalid))
				{
					return false;
				}
				if (!base.AIDataArmyGUID.IsValid && this.City.StandardUnits.Count > 0)
				{
					List<GameEntityGUID> list = new List<GameEntityGUID>();
					for (int j = 0; j < this.City.StandardUnits.Count; j++)
					{
						if (this.City.StandardUnits[j].GetPropertyValue(SimulationProperties.Movement) > 0f && !this.City.StandardUnits[j].IsSettler)
						{
							list.Add(this.City.StandardUnits[j].GUID);
						}
					}
					if (list.Count == 0)
					{
						return false;
					}
					OrderTransferGarrisonToNewArmy order = new OrderTransferGarrisonToNewArmy(base.Commander.Empire.Index, this.City.GUID, list.ToArray(), invalid, null, false, true, true);
					base.Commander.Empire.PlayerControllers.AI.PostOrder(order, out this.armySpawnTicket, null);
				}
			}
			else if (this.armySpawnTicket.Raised)
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
		}
		if (!base.AIDataArmyGUID.IsValid)
		{
			return false;
		}
		List<object> list2 = new List<object>();
		list2.Add(this.City);
		if (mustAttack)
		{
			return base.TryCreateArmyMission("DefendCity_BreakSiegeNow", list2);
		}
		return base.TryCreateArmyMission("DefendCity_BreakSiege", list2);
	}

	private bool DisbandArmy()
	{
		AIData_Army aidata_Army;
		return base.AIDataArmyGUID.IsValid && this.aiDataRepository.TryGetAIData<AIData_Army>(base.AIDataArmyGUID, out aidata_Army) && aidata_Army.Army.StandardUnits.Count > 0 && this.City.StandardUnits.Count + aidata_Army.Army.StandardUnits.Count < this.City.MaximumUnitSlot && base.TryCreateArmyMission("DefendCity_Bail", new List<object>
		{
			this.City
		});
	}

	private bool IsMissionValid()
	{
		return this.City != null && this.City.Empire == base.Commander.Empire;
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
