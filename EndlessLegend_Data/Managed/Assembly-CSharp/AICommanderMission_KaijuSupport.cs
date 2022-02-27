using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Session;
using UnityEngine;

public class AICommanderMission_KaijuSupport : AICommanderMission
{
	public override void Initialize(AICommander commander)
	{
		base.Initialize(commander);
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.game = (service.Game as global::Game);
		this.worldPositioningService = service.Game.Services.GetService<IWorldPositionningService>();
		this.pathfindingService = service.Game.Services.GetService<IPathfindingService>();
		this.departmentOfDefense = base.Commander.Empire.GetAgency<DepartmentOfDefense>();
		this.departmentOfTheInterior = base.Commander.Empire.GetAgency<DepartmentOfTheInterior>();
		this.aiDataRepositoryHelper = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		base.AIDataArmyGUID = commander.ForceArmyGUID;
		AIData_Army aidata_Army;
		if (this.aiDataRepositoryHelper.TryGetAIData<AIData_Army>(base.AIDataArmyGUID, out aidata_Army) && aidata_Army.Army is KaijuArmy)
		{
			this.Kaiju = (aidata_Army.Army as KaijuArmy).Kaiju;
		}
		if (base.Commander.Empire != null && base.Commander.Empire is MajorEmpire)
		{
			GameServer gameServer = (Services.GetService<ISessionService>().Session as global::Session).GameServer as GameServer;
			AIPlayer_MajorEmpire aiplayer_MajorEmpire;
			if (gameServer.AIScheduler != null && gameServer.AIScheduler.TryGetMajorEmpireAIPlayer(base.Commander.Empire as MajorEmpire, out aiplayer_MajorEmpire))
			{
				AIEntity entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
				if (entity != null)
				{
					this.aILayer_KaijuManagement = entity.GetLayer<AILayer_KaijuManagement>();
					this.aILayer_War = entity.GetLayer<AILayer_War>();
				}
			}
		}
		IDatabase<GarrisonAction> database = Databases.GetDatabase<GarrisonAction>(false);
		GarrisonAction garrisonAction = null;
		if (database == null || !database.TryGetValue("GarrisonActionMigrateKaiju", out garrisonAction))
		{
			Diagnostics.LogError("AICommanderMission_KaijuSupport didnt find GarrisonActionMigrateKaiju");
			return;
		}
		this.garrisonAction_MigrateKaiju = (garrisonAction as GarrisonAction_MigrateKaiju);
	}

	public override void Release()
	{
		base.Release();
		this.aILayer_KaijuManagement = null;
		this.aILayer_War = null;
		this.aiDataRepositoryHelper = null;
		this.worldPositioningService = null;
		this.departmentOfDefense = null;
		this.departmentOfTheInterior = null;
		this.Kaiju = null;
		this.ArmyToSupport = null;
		this.ClosestNeutralRegion = null;
		this.game = null;
		this.pathfindingService = null;
		this.garrisonAction_MigrateKaiju = null;
	}

	protected override void Running()
	{
		if (!this.aiDataRepository.IsGUIDValid(base.AIDataArmyGUID) || this.Kaiju == null || !(base.Commander.Empire as MajorEmpire).TamedKaijus.Contains(this.Kaiju) || this.Kaiju.OnGarrisonMode())
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Success;
			return;
		}
		base.Running();
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
		AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(base.AIDataArmyGUID);
		if (aidata == null || aidata.Army == null)
		{
			return false;
		}
		if (this.CurrentTurn != this.game.Turn)
		{
			this.ResetParameters();
		}
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.Log("==========================================================");
			Diagnostics.Log("ELCP {0} KaijuSupport TryComputeArmyMissionParameter() {1} {2} {3} {4}", new object[]
			{
				base.Commander.Empire,
				this.Kaiju.KaijuArmy.LocalizedName,
				this.Kaiju.KaijuArmy.WorldPosition,
				this.Kaiju.WorldPosition,
				this.AtWar
			});
		}
		new List<StaticString>();
		if (this.aILayer_KaijuManagement.IsKaijuValidForSupport(this.Kaiju))
		{
			if (this.ChooseArmyToSupport())
			{
				if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
				{
					Diagnostics.Log("ELCP {0} KaijuSupport supporting {1}/{2} {3}", new object[]
					{
						base.Commander.Empire,
						this.ArmyToSupport.LocalizedName,
						this.ArmyToSupport.WorldPosition,
						this.TryToSettle
					});
				}
				if (this.TryToSettle && this.ClosestNeutralRegion != null)
				{
					if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
					{
						Diagnostics.Log("ELCP {0} KaijuSupport KaijuGotoRegion1 {1}", new object[]
						{
							base.Commander.Empire,
							this.ClosestNeutralRegion.LocalizedName
						});
					}
					return base.TryCreateArmyMission("KaijuGotoRegion", new List<object>
					{
						this.ClosestNeutralRegion.Index
					});
				}
				return base.TryCreateArmyMission("KaijuSupport", new List<object>
				{
					this.ArmyToSupport
				});
			}
			else if (this.AtWar && this.departmentOfTheInterior.Cities.Count > 0)
			{
				if (this.aILayer_War != null)
				{
					this.aILayer_War.AssignDefensiveArmyToCity(aidata.Army);
				}
				return base.TryCreateArmyMission("MajorFactionWarRoaming", new List<object>
				{
					this.departmentOfTheInterior.Cities[0].Region.Index,
					true
				});
			}
		}
		this.GetClosestReachableNeutralRegion(this.Kaiju, int.MaxValue);
		if (this.ClosestNeutralRegion != null && (this.TryToSettle || this.garrisonAction_MigrateKaiju.ComputeRemainingCooldownDuration(this.Kaiju.KaijuArmy) <= 1f))
		{
			this.TryToSettle = true;
			return base.TryCreateArmyMission("KaijuGotoRegion", new List<object>
			{
				this.ClosestNeutralRegion.Index
			});
		}
		if (this.departmentOfTheInterior.Cities.Count > 0)
		{
			return base.TryCreateArmyMission("MajorFactionRoaming", new List<object>
			{
				this.departmentOfTheInterior.Cities[0].Region.Index
			});
		}
		base.Completion = AICommanderMission.AICommanderMissionCompletion.Success;
		this.Success();
		return false;
	}

	private void ResetParameters()
	{
		this.CurrentTurn = this.game.Turn;
		this.AtWar = (base.Commander.Empire.GetPropertyValue(SimulationProperties.WarCount) >= 1f);
		this.ArmyToSupport = null;
		this.DistanceToSupportArmy = -1f;
		this.TriedToFindNeutralRegion = false;
		this.ClosestNeutralRegion = null;
		this.TryToSettle = false;
	}

	private bool ChooseArmyToSupport()
	{
		if (this.ArmyToSupport != null && this.ArmyToSupport.GUID.IsValid && this.ArmyToSupport.Empire.Index == base.Commander.Empire.Index && (!(this.ArmyToSupport is KaijuArmy) || (this.ArmyToSupport as KaijuArmy).Kaiju.OnArmyMode()))
		{
			return true;
		}
		this.ArmyToSupport = null;
		float num = 0f;
		float propertyValue = this.Kaiju.KaijuArmy.GetPropertyValue(SimulationProperties.MaximumMovement);
		for (int i = 0; i < this.departmentOfDefense.Armies.Count; i++)
		{
			AIData_Army aidata = this.aiDataRepositoryHelper.GetAIData<AIData_Army>(this.departmentOfDefense.Armies[i].GUID);
			if (aidata != null && aidata.Army.GUID != base.AIDataArmyGUID && aidata.SupportScore >= 0.5f)
			{
				float supportScore = aidata.SupportScore;
				float num2 = Mathf.Ceil((float)this.worldPositioningService.GetDistance(this.Kaiju.KaijuArmy.WorldPosition, aidata.Army.WorldPosition) / propertyValue);
				float num3 = supportScore * Mathf.Max(0.2f, (21f - num2) / 20f);
				if (num3 > num)
				{
					num = num3;
					this.ArmyToSupport = this.departmentOfDefense.Armies[i];
					this.DistanceToSupportArmy = num2;
				}
			}
		}
		if (this.ArmyToSupport != null && this.DistanceToSupportArmy > 3f && (this.garrisonAction_MigrateKaiju.ComputeRemainingCooldownDuration(this.Kaiju.KaijuArmy) <= 1f || !this.AtWar))
		{
			Region bestSupportRegionForKaiju = this.aILayer_KaijuManagement.GetBestSupportRegionForKaiju(this.Kaiju, this.ArmyToSupport, null);
			if (bestSupportRegionForKaiju != null)
			{
				float num4 = Mathf.Ceil((float)this.worldPositioningService.GetDistance(this.ArmyToSupport.WorldPosition, bestSupportRegionForKaiju.Barycenter) / propertyValue);
				this.GetClosestReachableNeutralRegion(this.Kaiju, (int)((this.DistanceToSupportArmy - num4) * propertyValue));
				if (this.ClosestNeutralRegion != null)
				{
					this.TryToSettle = true;
				}
			}
		}
		return this.ArmyToSupport != null;
	}

	private void GetClosestReachableNeutralRegion(Kaiju kaiju, int MaxDistance)
	{
		if (this.TriedToFindNeutralRegion)
		{
			return;
		}
		this.TriedToFindNeutralRegion = true;
		if (this.ClosestNeutralRegion != null && !this.ClosestNeutralRegion.IsRegionColonized())
		{
			return;
		}
		this.ClosestNeutralRegion = null;
		List<Region> list = new List<Region>();
		Region region = this.worldPositioningService.GetRegion(kaiju.KaijuArmy.WorldPosition);
		if (region != null && KaijuCouncil.IsRegionValidForSettleKaiju(region))
		{
			this.ClosestNeutralRegion = region;
			return;
		}
		for (int i = 0; i < this.worldPositioningService.World.Regions.Length; i++)
		{
			Region region2 = this.worldPositioningService.World.Regions[i];
			if (KaijuCouncil.IsRegionValidForSettleKaiju(region2) && this.worldPositioningService.GetDistance(region2.Barycenter, kaiju.KaijuArmy.WorldPosition) < MaxDistance)
			{
				list.Add(region2);
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		list.Sort((Region left, Region right) => this.worldPositioningService.GetDistance(left.Barycenter, kaiju.KaijuArmy.WorldPosition).CompareTo(this.worldPositioningService.GetDistance(right.Barycenter, kaiju.KaijuArmy.WorldPosition)));
		foreach (Region region3 in list)
		{
			if (this.pathfindingService.FindPath(kaiju.KaijuArmy, kaiju.KaijuArmy.WorldPosition, region3.Barycenter, PathfindingManager.RequestMode.Default, null, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreKaijuGarrisons, null) != null)
			{
				this.ClosestNeutralRegion = region3;
				break;
			}
		}
	}

	public override void Load()
	{
		base.Load();
		if (this.Kaiju == null)
		{
			base.AIDataArmyGUID = base.Commander.ForceArmyGUID;
			AIData_Army aidata_Army;
			if (this.aiDataRepositoryHelper.TryGetAIData<AIData_Army>(base.AIDataArmyGUID, out aidata_Army) && aidata_Army.Army is KaijuArmy)
			{
				this.Kaiju = (aidata_Army.Army as KaijuArmy).Kaiju;
			}
		}
	}

	private IAIDataRepositoryAIHelper aiDataRepositoryHelper;

	private IWorldPositionningService worldPositioningService;

	private DepartmentOfDefense departmentOfDefense;

	private AILayer_KaijuManagement aILayer_KaijuManagement;

	private Kaiju Kaiju;

	private global::Game game;

	private int CurrentTurn;

	private bool AtWar;

	private Army ArmyToSupport;

	private float DistanceToSupportArmy;

	private bool TriedToFindNeutralRegion;

	private Region ClosestNeutralRegion;

	private IPathfindingService pathfindingService;

	private bool TryToSettle;

	private GarrisonAction_MigrateKaiju garrisonAction_MigrateKaiju;

	private bool settling;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private AILayer_War aILayer_War;
}
