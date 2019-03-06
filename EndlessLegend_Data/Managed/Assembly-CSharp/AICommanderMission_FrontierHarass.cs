using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AICommanderMission_FrontierHarass : AICommanderMissionWithRequestArmy, IXmlSerializable
{
	public AICommanderMission_FrontierHarass()
	{
		base.SeasonToSwitchTo = Season.ReadOnlyWinter;
	}

	public override void ReadXml(XmlReader reader)
	{
		int attribute = reader.GetAttribute<int>("RegionTargetIndex");
		this.RegionTarget = null;
		if (attribute > -1)
		{
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			global::Game game = service.Game as global::Game;
			World world = game.World;
			this.RegionTarget = world.Regions[attribute];
			Diagnostics.Assert(this.RegionTarget != null);
		}
		base.ReadXml(reader);
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<int>("RegionTargetIndex", (this.RegionTarget != null) ? this.RegionTarget.Index : -1);
		base.WriteXml(writer);
	}

	public Region RegionTarget { get; set; }

	public bool ArrivedToDestination()
	{
		AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(base.AIDataArmyGUID);
		if (aidata == null)
		{
			return false;
		}
		Region region = this.worldPositionningService.GetRegion(aidata.Army.WorldPosition);
		if (this.RegionTarget == region)
		{
			return true;
		}
		this.CalculateRegroupPositionForWaitToAttack();
		return !this.targetFrontierPosition.IsValid || aidata.Army.WorldPosition == this.targetFrontierPosition;
	}

	public override WorldPosition GetTargetPositionForTheArmy()
	{
		if (this.RegionTarget == null)
		{
			return WorldPosition.Invalid;
		}
		if (this.RegionTarget.City != null)
		{
			return this.RegionTarget.City.GetValidDistrictToTarget(null).WorldPosition;
		}
		return this.RegionTarget.Barycenter;
	}

	public override void Initialize(AICommander commander)
	{
		base.Initialize(commander);
		IGameService service = Services.GetService<IGameService>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		this.pathfindingService = service.Game.Services.GetService<IPathfindingService>();
		this.departmentOfScience = base.Commander.Empire.GetAgency<DepartmentOfScience>();
		this.departmentOfForeignAffairs = base.Commander.Empire.GetAgency<DepartmentOfForeignAffairs>();
	}

	public override void Release()
	{
		base.Release();
		this.RegionTarget = null;
	}

	public override void SetParameters(AICommanderMissionDefinition missionDefinition, params object[] parameters)
	{
		base.SetParameters(missionDefinition, parameters);
		this.RegionTarget = (parameters[0] as Region);
	}

	protected override void GetNeededArmyPower(out float minMilitaryPower, out bool isMaxPower, out bool perUnitTest)
	{
		isMaxPower = true;
		perUnitTest = false;
		minMilitaryPower = float.MaxValue;
	}

	protected override int GetNeededAvailabilityTime()
	{
		return 5;
	}

	protected override bool IsMissionCompleted()
	{
		if (this.RegionTarget == null || this.RegionTarget.City == null || this.RegionTarget.City.Empire == base.Commander.Empire)
		{
			return true;
		}
		AIData_City aidata = this.aiDataRepository.GetAIData<AIData_City>(this.RegionTarget.City.GUID);
		return aidata == null;
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
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return false;
		}
		AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(base.AIDataArmyGUID);
		if (aidata == null || aidata.Army == null)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return false;
		}
		if (this.departmentOfScience.CanPillage() && this.departmentOfForeignAffairs.CanMoveOn(this.RegionTarget.Index, aidata.Army.IsPrivateers))
		{
			if (base.TryCreateArmyMission("ExploreAt", new List<object>
			{
				this.RegionTarget.Index
			}))
			{
				return true;
			}
		}
		else
		{
			List<object> list = new List<object>();
			this.CalculateRegroupPositionForWaitToAttack();
			if (this.targetFrontierPosition.IsValid)
			{
				list.Add(this.targetFrontierPosition);
			}
			else
			{
				list.Add(this.RegionTarget.City.WorldPosition);
			}
			if (base.TryCreateArmyMission("ScoutRegion", list))
			{
				return true;
			}
		}
		return false;
	}

	private void CalculateRegroupPositionForWaitToAttack()
	{
		if (this.RegionTarget == null || this.RegionTarget.City == null || this.RegionTarget.City.Empire == base.Commander.Empire || !base.AIDataArmyGUID.IsValid)
		{
			return;
		}
		AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(base.AIDataArmyGUID);
		if (aidata == null)
		{
			return;
		}
		PathfindingContext pathfindingContext = aidata.Army.GenerateContext();
		pathfindingContext.Greedy = true;
		PathfindingResult pathfindingResult;
		if (this.targetFrontierPosition.IsValid)
		{
			Army armyAtPosition = this.worldPositionningService.GetArmyAtPosition(this.targetFrontierPosition);
			if (armyAtPosition == null)
			{
				pathfindingResult = this.pathfindingService.FindPath(pathfindingContext, aidata.Army.WorldPosition, this.targetFrontierPosition, PathfindingManager.RequestMode.Default, null, PathfindingFlags.IgnoreFogOfWar, null);
				if (pathfindingResult != null)
				{
					return;
				}
			}
			else if (armyAtPosition.GUID == base.AIDataArmyGUID)
			{
				return;
			}
		}
		Region region = this.worldPositionningService.GetRegion(aidata.Army.WorldPosition);
		if (region == this.RegionTarget)
		{
			this.targetFrontierPosition = aidata.Army.WorldPosition;
			return;
		}
		pathfindingResult = this.pathfindingService.FindPath(pathfindingContext, aidata.Army.WorldPosition, this.RegionTarget.City.WorldPosition, PathfindingManager.RequestMode.Default, null, PathfindingFlags.IgnoreFogOfWar, null);
		if (pathfindingResult == null)
		{
			this.targetFrontierPosition = aidata.Army.WorldPosition;
			return;
		}
		WorldPosition worldPosition = aidata.Army.WorldPosition;
		foreach (WorldPosition worldPosition2 in pathfindingResult.GetCompletePath())
		{
			if (!(worldPosition2 == pathfindingResult.Start) && !(worldPosition2 == pathfindingResult.Goal))
			{
				if (this.pathfindingService.IsTileStopableAndPassable(worldPosition2, aidata.Army, PathfindingFlags.IgnoreFogOfWar, null))
				{
					int regionIndex = (int)this.worldPositionningService.GetRegionIndex(worldPosition2);
					if (regionIndex == this.RegionTarget.Index)
					{
						break;
					}
					worldPosition = worldPosition2;
				}
			}
		}
		this.targetFrontierPosition = worldPosition;
	}

	private IPathfindingService pathfindingService;

	private WorldPosition targetFrontierPosition = WorldPosition.Invalid;

	private IWorldPositionningService worldPositionningService;

	private DepartmentOfScience departmentOfScience;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;
}
