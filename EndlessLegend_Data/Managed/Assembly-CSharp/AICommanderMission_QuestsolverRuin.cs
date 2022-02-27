using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AICommanderMission_QuestsolverRuin : AICommanderMission
{
	public override void Initialize(AICommander commander)
	{
		base.Initialize(commander);
		IGameService service = Services.GetService<IGameService>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		this.pathfindingService = service.Game.Services.GetService<IPathfindingService>();
		base.AIDataArmyGUID = commander.ForceArmyGUID;
	}

	public override void SetParameters(AICommanderMissionDefinition missionDefinition, params object[] parameters)
	{
		base.SetParameters(missionDefinition, parameters);
		this.POIGUID = (GameEntityGUID)parameters[0];
	}

	public override void Load()
	{
		base.Load();
		if (!this.POIGUID.IsValid)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return;
		}
		this.POI = this.SelectPOI();
		this.RegionTarget = this.worldPositionningService.GetRegion(this.POI.WorldPosition);
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

	public override void Release()
	{
		base.Release();
		this.RegionTarget = null;
		this.POI = null;
		this.gameEntityRepositoryService = null;
		this.worldPositionningService = null;
		this.pathfindingService = null;
	}

	protected override void Running()
	{
		if (!base.AIDataArmyGUID.IsValid)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Success;
			return;
		}
		Army army = this.aiDataRepository.GetAIData<AIData_Army>(base.AIDataArmyGUID).Army;
		if (army != null)
		{
			if (army.StandardUnits.Count((Unit x) => x.UnitDesign.UnitBodyDefinition.SubCategory == "SubCategoryFlying" && x.GetPropertyValue(SimulationProperties.LevelDisplayed) >= 4f) != 0)
			{
				if (base.Commander.IsMissionFinished(false))
				{
					base.Completion = AICommanderMission.AICommanderMissionCompletion.Success;
					return;
				}
				base.Running();
				return;
			}
		}
		base.Completion = AICommanderMission.AICommanderMissionCompletion.Success;
	}

	protected override void Success()
	{
		base.Success();
		base.SetArmyFree();
	}

	protected override bool TryComputeArmyMissionParameter()
	{
		base.ArmyMissionParameters.Clear();
		if (base.AIDataArmyGUID == GameEntityGUID.Zero)
		{
			return false;
		}
		if (this.POI == null)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return false;
		}
		Army army = this.aiDataRepository.GetAIData<AIData_Army>(base.AIDataArmyGUID).Army;
		if (army.Hero != null)
		{
			OrderChangeHeroAssignment orderChangeHeroAssignment = new OrderChangeHeroAssignment(base.Commander.Empire.Index, army.Hero.GUID, GameEntityGUID.Zero);
			orderChangeHeroAssignment.IgnoreCooldown = true;
			base.Commander.Empire.PlayerControllers.AI.PostOrder(orderChangeHeroAssignment);
			return true;
		}
		if (army.StandardUnits.Count((Unit x) => x.UnitDesign.UnitBodyDefinition.SubCategory == "SubCategoryFlying" && x.GetPropertyValue(SimulationProperties.LevelDisplayed) >= 4f) == 0)
		{
			this.Success();
		}
		if (army.StandardUnits.Count <= 1)
		{
			return base.TryCreateArmyMission("VisitQuestRuin", new List<object>
			{
				this.RegionTarget.Index,
				this.POI
			});
		}
		Unit item = army.StandardUnits.First((Unit x) => x.UnitDesign.UnitBodyDefinition.SubCategory == "SubCategoryFlying" && x.GetPropertyValue(SimulationProperties.LevelDisplayed) >= 4f);
		List<Unit> second = new List<Unit>
		{
			item
		};
		List<Unit> list = army.StandardUnits.Except(second).ToList<Unit>();
		WorldPosition validArmySpawningPosition = AILayer_ArmyRecruitment.GetValidArmySpawningPosition(army, this.worldPositionningService, this.pathfindingService);
		if (!validArmySpawningPosition.IsValid || list.Count == 0)
		{
			return base.TryCreateArmyMission("MajorFactionRoaming", new List<object>
			{
				this.RegionTarget.Index,
				true
			});
		}
		OrderTransferGarrisonToNewArmy order = new OrderTransferGarrisonToNewArmy(base.Commander.Empire.Index, army.GUID, list.ConvertAll<GameEntityGUID>((Unit unit) => unit.GUID).ToArray(), validArmySpawningPosition, StaticString.Empty, false, true, true);
		base.Commander.Empire.PlayerControllers.Server.PostOrder(order);
		return true;
	}

	private PointOfInterest POI { get; set; }

	private GameEntityGUID POIGUID { get; set; }

	private Region RegionTarget { get; set; }

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private IWorldPositionningService worldPositionningService;

	private IPathfindingService pathfindingService;
}
