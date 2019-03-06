using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AICommanderMission_VictoryRuinFinal : AICommanderMissionWithRequestArmy, IXmlSerializable
{
	public AICommanderMission_VictoryRuinFinal()
	{
		this.RegionTarget = null;
		this.POI = null;
	}

	public override void ReadXml(XmlReader reader)
	{
		int attribute = reader.GetAttribute<int>("RegionTargetIndex");
		this.RegionTarget = null;
		if (attribute > -1)
		{
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			World world = (service.Game as global::Game).World;
			this.RegionTarget = world.Regions[attribute];
			Diagnostics.Assert(this.RegionTarget != null);
		}
		this.POIGUID = reader.GetAttribute<ulong>("POIGUID");
		base.ReadXml(reader);
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<int>("RegionTargetIndex", (this.RegionTarget != null) ? this.RegionTarget.Index : -1);
		writer.WriteAttributeString<ulong>("POIGUID", this.POIGUID);
		base.WriteXml(writer);
	}

	public Region RegionTarget { get; set; }

	public PointOfInterest POI { get; set; }

	public GameEntityGUID POIGUID { get; set; }

	public override WorldPosition GetTargetPositionForTheArmy()
	{
		if (this.POI == null)
		{
			this.POI = this.SelectPOI();
		}
		if (this.POI != null)
		{
			return this.POI.WorldPosition;
		}
		if (this.RegionTarget != null)
		{
			return this.RegionTarget.Barycenter;
		}
		return WorldPosition.Invalid;
	}

	public override void Initialize(AICommander commander)
	{
		base.Initialize(commander);
		IGameService service = Services.GetService<IGameService>();
		this.pathfindingService = service.Game.Services.GetService<IPathfindingService>();
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
	}

	public override void Load()
	{
		base.Load();
		if (this.RegionTarget == null || !this.POIGUID.IsValid)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return;
		}
		this.POI = this.SelectPOI();
	}

	public override void Release()
	{
		base.Release();
		this.RegionTarget = null;
		this.POI = null;
		this.gameEntityRepositoryService = null;
		this.pathfindingService = null;
		this.worldPositionningService = null;
	}

	public override void SetParameters(AICommanderMissionDefinition missionDefinition, params object[] parameters)
	{
		base.SetParameters(missionDefinition, parameters);
		this.RegionTarget = (parameters[0] as Region);
		this.POIGUID = (GameEntityGUID)parameters[1];
	}

	protected override AICommanderMission.AICommanderMissionCompletion GetCompletionWhenSuccess(AIData_Army armyData, out TickableState tickableState)
	{
		tickableState = TickableState.Optional;
		if (this.IsMissionCompleted())
		{
			return AICommanderMission.AICommanderMissionCompletion.Success;
		}
		return AICommanderMission.AICommanderMissionCompletion.Initializing;
	}

	protected override void GetNeededArmyPower(out float minMilitaryPower, out bool isMaxPower, out bool perUnitTest)
	{
		isMaxPower = false;
		perUnitTest = false;
		minMilitaryPower = 1f;
	}

	protected override int GetNeededAvailabilityTime()
	{
		return 5;
	}

	protected override bool IsMissionCompleted()
	{
		return base.Commander.IsMissionFinished(false);
	}

	protected override void Running()
	{
		base.Running();
	}

	protected override void Success()
	{
		base.Success();
		base.SetArmyFree();
	}

	protected override bool TryComputeArmyMissionParameter()
	{
		if (this.RegionTarget == null)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return false;
		}
		base.ArmyMissionParameters.Clear();
		if (base.AIDataArmyGUID == GameEntityGUID.Zero)
		{
			return false;
		}
		this.POI = this.SelectPOI();
		if (this.POI == null)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return false;
		}
		AICommander_Victory comm = base.Commander as AICommander_Victory;
		if (comm == null)
		{
			return false;
		}
		Army army = this.aiDataRepository.GetAIData<AIData_Army>(base.AIDataArmyGUID).Army;
		int num = army.StandardUnits.Count((Unit x) => x.UnitDesign.Name.ToString().Contains(comm.VictoryDesign));
		float propertyValue = army.GetPropertyValue(SimulationProperties.Movement);
		if (propertyValue <= 0.01f)
		{
			base.State = TickableState.NoTick;
			return false;
		}
		if (num > 5 || this.worldPositionningService.IsWaterTile(army.WorldPosition))
		{
			return base.TryCreateArmyMission("VisitQuestRuinFinal", new List<object>
			{
				this.RegionTarget.Index,
				this.POI
			});
		}
		if (num == 0)
		{
			return base.TryCreateArmyMission("MajorFactionWarRoaming", new List<object>
			{
				this.RegionTarget.Index,
				true
			});
		}
		if (num <= 0 || army.StandardUnits.Count - num <= 0)
		{
			List<AICommanderMission> list = comm.Missions.FindAll((AICommanderMission x) => x is AICommanderMission_VictoryRuinFinal && x.AIDataArmyGUID.IsValid);
			List<Army> list2 = new List<Army>();
			Func<Unit, bool> <>9__4;
			foreach (AICommanderMission aicommanderMission in list)
			{
				Army army2 = this.aiDataRepository.GetAIData<AIData_Army>(aicommanderMission.AIDataArmyGUID).Army;
				if (army2 != army)
				{
					IEnumerable<Unit> standardUnits = army2.StandardUnits;
					Func<Unit, bool> selector;
					if ((selector = <>9__4) == null)
					{
						selector = (<>9__4 = ((Unit y) => y.UnitDesign.Name.ToString().Contains(comm.VictoryDesign)));
					}
					int num2 = standardUnits.Count(selector);
					if (num2 > 0 && num2 < 6)
					{
						list2.Add(army2);
					}
				}
			}
			int num3 = (int)base.Commander.Empire.GetPropertyValue(SimulationProperties.ArmyUnitSlot);
			Predicate<Unit> <>9__5;
			foreach (Army army3 in list2)
			{
				if (this.worldPositionningService.GetDistance(army.WorldPosition, army3.WorldPosition) == 1 && this.worldPositionningService.IsWaterTile(army.WorldPosition) == this.worldPositionningService.IsWaterTile(army3.WorldPosition) && this.pathfindingService.IsTransitionPassable(army.WorldPosition, army3.WorldPosition, army, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreFogOfWar, null))
				{
					float propertyValue2 = army3.GetPropertyValue(SimulationProperties.Movement);
					if ((propertyValue > propertyValue2 || (propertyValue == propertyValue2 && army.GUID < army3.GUID) || (army.StandardUnits.Count >= num3 && propertyValue > 0.01f)) && army3.StandardUnits.Count < num3)
					{
						List<Unit> list3 = army.StandardUnits.ToList<Unit>();
						Predicate<Unit> match;
						if ((match = <>9__5) == null)
						{
							match = (<>9__5 = ((Unit z) => z.UnitDesign.Name.ToString().Contains(comm.VictoryDesign)));
						}
						List<Unit> list4 = list3.FindAll(match);
						int num4 = list4.Count - 1;
						while (num4 >= 0 && list4.Count + army3.StandardUnits.Count > num3)
						{
							list4.RemoveAt(num4);
							num4--;
						}
						OrderTransferUnits order = new OrderTransferUnits(base.Commander.Empire.Index, army.GUID, army3.GUID, list4.ConvertAll<GameEntityGUID>((Unit unit) => unit.GUID).ToArray(), false);
						Ticket ticket;
						base.Commander.Empire.PlayerControllers.Server.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderTransferUnitToArmyTicketRaised));
						return true;
					}
					if (army.StandardUnits.Count < num3)
					{
						return base.TryCreateArmyMission("ELCPWait", new List<object>());
					}
				}
			}
			int num5 = int.MaxValue;
			Army army4 = null;
			foreach (Army army5 in list2)
			{
				int distance = this.worldPositionningService.GetDistance(army.WorldPosition, army5.WorldPosition);
				if (distance < num5)
				{
					num5 = distance;
					army4 = army5;
				}
			}
			if (army4 != null && army4.GUID > army.GUID && army4.GetPropertyValue(SimulationProperties.Movement) > 0.01f)
			{
				army4 = null;
			}
			if (army4 != null)
			{
				return base.TryCreateArmyMission("ReachTarget", new List<object>
				{
					army4
				});
			}
			return base.TryCreateArmyMission("ELCPWait", new List<object>());
		}
		if (army.IsInEncounter || army.IsLocked)
		{
			return base.TryCreateArmyMission("ELCPWait", new List<object>());
		}
		WorldPosition validArmySpawningPosition = AILayer_ArmyRecruitment.GetValidArmySpawningPosition(army, this.worldPositionningService, this.pathfindingService);
		if (!validArmySpawningPosition.IsValid)
		{
			return base.TryCreateArmyMission("MajorFactionWarRoaming", new List<object>
			{
				this.RegionTarget.Index,
				true
			});
		}
		List<Unit> list5 = army.StandardUnits.ToList<Unit>().FindAll((Unit x) => !x.UnitDesign.Name.ToString().Contains(comm.VictoryDesign));
		OrderTransferGarrisonToNewArmy order2 = new OrderTransferGarrisonToNewArmy(base.Commander.Empire.Index, army.GUID, list5.ConvertAll<GameEntityGUID>((Unit unit) => unit.GUID).ToArray(), validArmySpawningPosition, StaticString.Empty, false, true, true);
		Ticket ticket2;
		base.Commander.Empire.PlayerControllers.Server.PostOrder(order2, out ticket2, new EventHandler<TicketRaisedEventArgs>(this.OrderSplitUnit));
		return true;
	}

	private PointOfInterest SelectPOI()
	{
		IGameEntity gameEntity;
		if (this.gameEntityRepositoryService.TryGetValue(this.POIGUID, out gameEntity) && gameEntity is PointOfInterest)
		{
			return gameEntity as PointOfInterest;
		}
		return null;
	}

	private bool POIAccessible()
	{
		IGameService service = Services.GetService<IGameService>();
		IPathfindingService service2 = service.Game.Services.GetService<IPathfindingService>();
		IWorldPositionningService service3 = service.Game.Services.GetService<IWorldPositionningService>();
		foreach (WorldPosition worldPosition in WorldPosition.GetDirectNeighbourTiles(this.POI.WorldPosition))
		{
			if ((!service3.IsWaterTile(worldPosition) || service3.IsFrozenWaterTile(worldPosition)) && service2.IsTileStopable(worldPosition, PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.FrozenWater, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreFogOfWar) && service2.IsTransitionPassable(worldPosition, this.POI.WorldPosition, PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.FrozenWater, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreFogOfWar))
			{
				return true;
			}
		}
		return false;
	}

	private void OrderTransferUnitToArmyTicketRaised(object sender, TicketRaisedEventArgs e)
	{
		if (e.Result == PostOrderResponse.Processed)
		{
			base.Interrupt();
			return;
		}
		base.TryCreateArmyMission("MajorFactionWarRoaming", new List<object>
		{
			this.RegionTarget.Index
		});
	}

	private void OrderSplitUnit(object sender, TicketRaisedEventArgs e)
	{
		if (e.Result == PostOrderResponse.Processed)
		{
			base.TryCreateArmyMission("MajorFactionWarRoaming", new List<object>
			{
				this.RegionTarget.Index
			});
		}
	}

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private IWorldPositionningService worldPositionningService;

	private IPathfindingService pathfindingService;
}
