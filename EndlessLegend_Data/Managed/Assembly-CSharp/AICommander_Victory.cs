using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class AICommander_Victory : AICommanderWithObjective, IXmlSerializable
{
	public AICommander_Victory(ulong globalObjectiveID, int regionIndex) : base(AICommanderMissionDefinition.AICommanderCategory.Victory, globalObjectiveID, regionIndex)
	{
	}

	public AICommander_Victory() : base(AICommanderMissionDefinition.AICommanderCategory.Victory, 0UL, 0)
	{
	}

	public override void ReadXml(XmlReader reader)
	{
		base.ReadXml(reader);
		IWorldPositionningService service = Services.GetService<IGameService>().Game.Services.GetService<IWorldPositionningService>();
		this.RegionTarget = service.GetRegion(base.RegionIndex);
	}

	public override void WriteXml(XmlWriter writer)
	{
		base.WriteXml(writer);
	}

	public Region RegionTarget
	{
		get
		{
			return this.region;
		}
		set
		{
			this.region = value;
			if (value != null)
			{
				base.RegionIndex = value.Index;
			}
		}
	}

	public override bool IsMissionFinished(bool forceStop)
	{
		QuestBehaviourTreeNode_Decorator_TimerEnded questBehaviourTreeNode_Decorator_TimerEnded;
		if (this.ActiveVictoryQuest == "VictoryQuest-Chapter4" && this.questBehaviour.Quest.GetCurrentStepIndex() > 0 && ELCPUtilities.TryGetFirstNodeOfType<QuestBehaviourTreeNode_Decorator_TimerEnded>(this.questBehaviour.Root as BehaviourTreeNodeController, out questBehaviourTreeNode_Decorator_TimerEnded))
		{
			int num = (int)this.questBehaviour.GetQuestVariableByName(questBehaviourTreeNode_Decorator_TimerEnded.TimerVarName).Object;
			if (this.game.Turn - num < questBehaviourTreeNode_Decorator_TimerEnded.TurnCountBeforeTimeOut)
			{
				return true;
			}
		}
		foreach (QuestMarker questMarker in this.questManagementService.GetMarkersByBoundTargetGUID(base.SubObjectiveGuid))
		{
			Quest quest;
			if (this.questRepositoryService.TryGetValue(questMarker.QuestGUID, out quest) && quest.QuestDefinition.Name == this.ActiveVictoryQuest && quest.EmpireBits == base.Empire.Bits && (this.ActiveVictoryQuest != "VictoryQuest-Chapter3" || questMarker.IsVisibleInFogOfWar))
			{
				if (base.Empire.GetAgency<DepartmentOfForeignAffairs>().CanMoveOn(base.RegionIndex, false))
				{
					return false;
				}
				GlobalObjectiveMessage globalObjectiveMessage;
				return base.GlobalObjectiveID == 0UL || base.AIPlayer == null || !base.AIPlayer.Blackboard.TryGetMessage<GlobalObjectiveMessage>(base.GlobalObjectiveID, out globalObjectiveMessage) || globalObjectiveMessage.State == BlackboardMessage.StateValue.Message_Canceled || globalObjectiveMessage.State == BlackboardMessage.StateValue.Message_Failed;
			}
		}
		return true;
	}

	public override void Load()
	{
		base.Load();
		IGameService service = Services.GetService<IGameService>();
		this.game = (service.Game as global::Game);
		this.questRepositoryService = service.Game.Services.GetService<IQuestRepositoryService>();
		this.questManagementService = service.Game.Services.GetService<IQuestManagementService>();
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		this.intelligenceAIHelper = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		this.empireDataRepository = AIScheduler.Services.GetService<IAIEmpireDataAIHelper>();
		this.departmentOfForeignAffairs = base.Empire.GetAgency<DepartmentOfForeignAffairs>();
		this.aiDataRepository = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
	}

	public override void PopulateMission()
	{
		this.ActiveVictoryQuest = string.Empty;
		this.VictoryDesign = string.Empty;
		this.questBehaviour = null;
		if (this.questManagementService.IsQuestRunningForEmpire("VictoryQuest-Chapter3", base.Empire))
		{
			this.ActiveVictoryQuest = "VictoryQuest-Chapter3";
			this.questBehaviour = this.questRepositoryService.GetQuestBehaviour("VictoryQuest-Chapter3", base.Empire.Index);
		}
		else if (this.questManagementService.IsQuestRunningForEmpire("VictoryQuest-Chapter4", base.Empire))
		{
			this.ActiveVictoryQuest = "VictoryQuest-Chapter4";
			this.questBehaviour = this.questRepositoryService.GetQuestBehaviour("VictoryQuest-Chapter4", base.Empire.Index);
		}
		else if (this.questManagementService.IsQuestRunningForEmpire("VictoryQuest-Chapter5Alt", base.Empire))
		{
			this.ActiveVictoryQuest = "VictoryQuest-Chapter5Alt";
			this.VictoryDesign = "Preacher";
			this.questBehaviour = this.questRepositoryService.GetQuestBehaviour("VictoryQuest-Chapter5Alt", base.Empire.Index);
		}
		else if (this.questManagementService.IsQuestRunningForEmpire("VictoryQuest-Chapter5", base.Empire))
		{
			this.ActiveVictoryQuest = "VictoryQuest-Chapter5";
			this.VictoryDesign = "Settler";
			this.questBehaviour = this.questRepositoryService.GetQuestBehaviour("VictoryQuest-Chapter5", base.Empire.Index);
		}
		if (this.IsMissionFinished(false))
		{
			return;
		}
		IGameEntity gameEntity;
		if (this.gameEntityRepositoryService.TryGetValue(base.SubObjectiveGuid, out gameEntity) && gameEntity is PointOfInterest && this.ActiveVictoryQuest != string.Empty)
		{
			if (this.ActiveVictoryQuest != "VictoryQuest-Chapter5Alt" && this.ActiveVictoryQuest != "VictoryQuest-Chapter5")
			{
				this.PopulateRuinorBesiegingCityMission(gameEntity as PointOfInterest);
				return;
			}
			this.PopulateFinalRuinOrBesiegingCityMission(gameEntity as PointOfInterest);
		}
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

	public override void Release()
	{
		this.RegionTarget = null;
		base.Release();
		this.gameEntityRepositoryService = null;
		this.questManagementService = null;
		this.questRepositoryService = null;
		this.intelligenceAIHelper = null;
		this.empireDataRepository = null;
		this.departmentOfForeignAffairs = null;
		this.questBehaviour = null;
		this.game = null;
		this.aiDataRepository = null;
	}

	private void PopulateRuinorBesiegingCityMission(PointOfInterest POI)
	{
		float armyMaxPower = this.GetArmyMaxPower();
		int num;
		if (this.region.City == null || this.region.City.Empire.Index == base.Empire.Index || this.departmentOfForeignAffairs.IsFriend(this.region.City.Empire))
		{
			num = Mathf.CeilToInt(Intelligence.GetArmiesInRegion(this.region.Index).ToList<Army>().FindAll((Army x) => this.departmentOfForeignAffairs.CanAttack(x)).Sum((Army x) => x.GetPropertyValue(SimulationProperties.MilitaryPower)) * 2f / armyMaxPower);
			for (int i = base.Missions.Count - 1; i >= 0; i--)
			{
				AICommanderMission_BesiegeCityDefault aicommanderMission_BesiegeCityDefault = base.Missions[i] as AICommanderMission_BesiegeCityDefault;
				if (aicommanderMission_BesiegeCityDefault != null)
				{
					this.CancelMission(aicommanderMission_BesiegeCityDefault);
				}
			}
		}
		else
		{
			num = Mathf.CeilToInt(this.intelligenceAIHelper.EvaluateMilitaryPowerOfGarrison(base.Empire, this.region.City, 0) * 1.2f / armyMaxPower);
			num += Mathf.CeilToInt(Intelligence.GetArmiesInRegion(this.region.Index).ToList<Army>().FindAll((Army x) => this.departmentOfForeignAffairs.CanAttack(x)).Sum((Army x) => x.GetPropertyValue(SimulationProperties.MilitaryPower)) * 2f / armyMaxPower);
		}
		if (num == 0)
		{
			num = 1;
		}
		else if (num > 5)
		{
			num = 5;
		}
		int num2 = 0;
		int num3 = 0;
		for (int j = 0; j < base.Missions.Count; j++)
		{
			AICommanderMission aicommanderMission = base.Missions[j];
			if (aicommanderMission != null)
			{
				if (num2 < num)
				{
					if (!aicommanderMission.AIDataArmyGUID.IsValid)
					{
						num3++;
					}
				}
				else if (num2 >= num + 2)
				{
					this.CancelMission(aicommanderMission);
					goto IL_1CD;
				}
				num2++;
			}
			IL_1CD:;
		}
		Tags tags = new Tags();
		if (this.POIAccessible(POI))
		{
			tags.AddTag(base.Category.ToString());
			tags.AddTag("Ruin");
			for (int k = num2; k < num; k++)
			{
				base.PopulationFirstMissionFromCategory(tags, new object[]
				{
					this.RegionTarget,
					base.SubObjectiveGuid
				});
			}
			return;
		}
		if (this.region.City != null && this.region.City.Empire.Index != base.Empire.Index && this.departmentOfForeignAffairs.IsAtWarWith(this.region.City.Empire))
		{
			for (int l = base.Missions.Count - 1; l >= 0; l--)
			{
				AICommanderMission_VictoryRuin aicommanderMission_VictoryRuin = base.Missions[l] as AICommanderMission_VictoryRuin;
				if (aicommanderMission_VictoryRuin != null)
				{
					this.CancelMission(aicommanderMission_VictoryRuin);
				}
			}
			tags.AddTag("War");
			tags.AddTag("BesiegeCity");
			for (int m = num2; m < num; m++)
			{
				base.PopulationFirstMissionFromCategory(tags, new object[]
				{
					this.region.City
				});
			}
		}
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

	private bool POIAccessible(PointOfInterest POI)
	{
		IGameService service = Services.GetService<IGameService>();
		IPathfindingService service2 = service.Game.Services.GetService<IPathfindingService>();
		IWorldPositionningService service3 = service.Game.Services.GetService<IWorldPositionningService>();
		PathfindingContext pathfindingContext = new PathfindingContext(GameEntityGUID.Zero, base.Empire, PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.FrozenWater);
		pathfindingContext.RefreshProperties(1f, float.PositiveInfinity, false, false, float.PositiveInfinity, float.PositiveInfinity);
		foreach (WorldPosition worldPosition in WorldPosition.GetDirectNeighbourTiles(POI.WorldPosition))
		{
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				Diagnostics.Log("ELCP {0} AICommander_Victory POIAccessible checking {1}/{2}", new object[]
				{
					base.Empire,
					POI.WorldPosition,
					worldPosition
				});
			}
			if ((!service3.IsWaterTile(worldPosition) || service3.IsFrozenWaterTile(worldPosition)) && service2.IsTileStopable(worldPosition, pathfindingContext, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreFogOfWar, null) && service2.IsTransitionPassable(worldPosition, POI.WorldPosition, pathfindingContext, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnorePOI, null))
			{
				if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
				{
					Diagnostics.Log("valid");
				}
				return true;
			}
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				Diagnostics.Log("not valid");
			}
		}
		return false;
	}

	private void PopulateFinalRuinOrBesiegingCityMission(PointOfInterest POI)
	{
		float armyMaxPower = this.GetArmyMaxPower();
		int num;
		if (this.region.City == null || this.region.City.Empire.Index == base.Empire.Index || this.departmentOfForeignAffairs.IsFriend(this.region.City.Empire))
		{
			num = Mathf.CeilToInt(Intelligence.GetArmiesInRegion(this.region.Index).ToList<Army>().FindAll((Army x) => this.departmentOfForeignAffairs.CanAttack(x)).Sum((Army x) => x.GetPropertyValue(SimulationProperties.MilitaryPower)) * 2f / armyMaxPower);
			for (int i = base.Missions.Count - 1; i >= 0; i--)
			{
				AICommanderMission_BesiegeCityDefault aicommanderMission_BesiegeCityDefault = base.Missions[i] as AICommanderMission_BesiegeCityDefault;
				if (aicommanderMission_BesiegeCityDefault != null)
				{
					this.CancelMission(aicommanderMission_BesiegeCityDefault);
				}
			}
		}
		else
		{
			num = Mathf.CeilToInt(this.intelligenceAIHelper.EvaluateMilitaryPowerOfGarrison(base.Empire, this.region.City, 0) * 1.2f / armyMaxPower);
			num += Mathf.CeilToInt(Intelligence.GetArmiesInRegion(this.region.Index).ToList<Army>().FindAll((Army x) => this.departmentOfForeignAffairs.CanAttack(x)).Sum((Army x) => x.GetPropertyValue(SimulationProperties.MilitaryPower)) * 2f / armyMaxPower);
		}
		if (num == 0)
		{
			num = 1;
		}
		else if (num > 5)
		{
			num = 5;
		}
		int num2 = 0;
		int num3 = 0;
		for (int j = 0; j < base.Missions.Count; j++)
		{
			AICommanderMission aicommanderMission = base.Missions[j];
			if (aicommanderMission != null && !(aicommanderMission is AICommanderMission_VictoryRuinFinal))
			{
				if (num2 < num)
				{
					if (!aicommanderMission.AIDataArmyGUID.IsValid)
					{
						num3++;
					}
				}
				else if (num2 >= num + 2)
				{
					this.CancelMission(aicommanderMission);
					goto IL_1D6;
				}
				num2++;
			}
			IL_1D6:;
		}
		Tags tags = new Tags();
		if (this.region.City != null && this.region.City.Empire.Index != base.Empire.Index && this.departmentOfForeignAffairs.IsAtWarWith(this.region.City.Empire))
		{
			for (int k = base.Missions.Count - 1; k >= 0; k--)
			{
				AICommanderMission_DefenseRoaming aicommanderMission_DefenseRoaming = base.Missions[k] as AICommanderMission_DefenseRoaming;
				if (aicommanderMission_DefenseRoaming != null)
				{
					this.CancelMission(aicommanderMission_DefenseRoaming);
				}
			}
			tags.AddTag("War");
			tags.AddTag("BesiegeCity");
			for (int l = num2; l < num; l++)
			{
				base.PopulationFirstMissionFromCategory(tags, new object[]
				{
					this.region.City
				});
			}
		}
		else
		{
			tags.AddTag("WarPatrol");
			for (int m = num2; m < num; m++)
			{
				base.PopulationFirstMissionFromCategory(tags, new object[]
				{
					this.RegionTarget,
					true
				});
			}
		}
		if (!this.POIAccessible(POI))
		{
			for (int n = base.Missions.Count - 1; n >= 0; n--)
			{
				AICommanderMission_VictoryRuinFinal aicommanderMission_VictoryRuinFinal = base.Missions[n] as AICommanderMission_VictoryRuinFinal;
				if (aicommanderMission_VictoryRuinFinal != null)
				{
					this.CancelMission(aicommanderMission_VictoryRuinFinal);
				}
			}
			return;
		}
		List<AICommanderMission> list = base.Missions.FindAll((AICommanderMission x) => x is AICommanderMission_VictoryRuinFinal);
		int num4 = 0;
		int num5 = -1;
		for (int num6 = list.Count - 1; num6 >= 0; num6--)
		{
			num4++;
			if (list[num6].AIDataArmyGUID.IsValid)
			{
				AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(list[num6].AIDataArmyGUID);
				if (aidata != null && aidata.Army != null)
				{
					int num7 = aidata.Army.StandardUnits.Count((Unit x) => x.UnitDesign.Name.ToString().Contains(this.VictoryDesign));
					if (num7 > 5)
					{
						num5 = num6;
						break;
					}
					if (num7 == 0)
					{
						this.CancelMission(list[num6]);
					}
				}
			}
		}
		if (num5 > -1)
		{
			for (int num8 = list.Count - 1; num8 >= 0; num8--)
			{
				if (num8 != num5)
				{
					this.CancelMission(list[num8]);
				}
			}
			return;
		}
		tags.Clear();
		tags.AddTag("Final");
		tags.AddTag("Ruin");
		if (this.VictoryDesign == "Preacher")
		{
			tags.AddTag("Cult");
		}
		else
		{
			tags.AddTag("Settler");
		}
		while (num4 < 6)
		{
			base.PopulationFirstMissionFromCategory(tags, new object[]
			{
				this.RegionTarget,
				base.SubObjectiveGuid
			});
			num4++;
		}
	}

	private IQuestManagementService questManagementService;

	private IQuestRepositoryService questRepositoryService;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private Region region;

	private IAIEmpireDataAIHelper empireDataRepository;

	private IIntelligenceAIHelper intelligenceAIHelper;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private string ActiveVictoryQuest;

	private QuestBehaviour questBehaviour;

	private global::Game game;

	private IAIDataRepositoryAIHelper aiDataRepository;

	public string VictoryDesign;
}
