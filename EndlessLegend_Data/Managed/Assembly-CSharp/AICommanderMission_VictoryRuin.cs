using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AICommanderMission_VictoryRuin : AICommanderMissionWithRequestArmy, IXmlSerializable
{
	public AICommanderMission_VictoryRuin()
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
	}

	public override void Load()
	{
		base.Load();
		if (this.RegionTarget == null || !this.POIGUID.IsValid)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return;
		}
		IGameService service = Services.GetService<IGameService>();
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		this.POI = this.SelectPOI();
	}

	public override void Release()
	{
		base.Release();
		this.RegionTarget = null;
		this.POI = null;
		this.gameEntityRepositoryService = null;
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
		minMilitaryPower = this.intelligenceAIHelper.EvaluateMaxMilitaryPowerOfRegion(base.Commander.Empire, this.RegionTarget.Index);
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
		return base.TryCreateArmyMission("VisitQuestRuin", new List<object>
		{
			this.RegionTarget.Index,
			this.POI
		});
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

	private IGameEntityRepositoryService gameEntityRepositoryService;
}
