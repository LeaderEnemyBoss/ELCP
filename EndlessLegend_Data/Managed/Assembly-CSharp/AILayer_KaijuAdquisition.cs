using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Runtime;
using UnityEngine;

[Diagnostics.TagAttribute("AI")]
public class AILayer_KaijuAdquisition : AILayerWithObjective
{
	public AILayer_KaijuAdquisition() : base(AICommanderMissionDefinition.AICommanderCategory.KaijuAdquisition.ToString())
	{
	}

	private List<Kaiju> GetFilterKaijuTargets(bool includeTamedKaijus)
	{
		List<Kaiju> list = new List<Kaiju>();
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		foreach (global::Empire empire in (service.Game as global::Game).Empires)
		{
			if (empire is KaijuEmpire)
			{
				KaijuEmpire kaijuEmpire = empire as KaijuEmpire;
				if (kaijuEmpire != null && kaijuEmpire.Region != null)
				{
					KaijuCouncil agency = kaijuEmpire.GetAgency<KaijuCouncil>();
					if (agency != null && agency.Kaiju != null)
					{
						list.Add(agency.Kaiju);
					}
				}
			}
			else if (includeTamedKaijus && empire is MajorEmpire && empire.Index != base.AIEntity.Empire.Index)
			{
				MajorEmpire majorEmpire = empire as MajorEmpire;
				foreach (Kaiju item in majorEmpire.TamedKaijus)
				{
					if (!this.departmentOfForeignAffairs.IsFriend(majorEmpire))
					{
						list.Add(item);
					}
				}
			}
		}
		return list;
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.pathfindingService = service.Game.Services.GetService<IPathfindingService>();
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		this.departmentOfDefense = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		this.departmentOfForeignAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		this.departmentOfCreepingNodes = base.AIEntity.Empire.GetAgency<DepartmentOfCreepingNodes>();
		this.aiLayerStrategy = base.AIEntity.GetLayer<AILayer_Strategy>();
		this.empireDataRepository = AIScheduler.Services.GetService<IAIEmpireDataAIHelper>();
		this.intelligenceAIHelper = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		this.personalityAIHelper = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		this.kaijuAdquisitionFactor = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_KaijuAdquisition.registryPath, "KaijuAdquisitionFactor"), this.kaijuAdquisitionFactor);
		this.huntTamedKaijus = (this.personalityAIHelper.GetRegistryValue<int>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_KaijuAdquisition.registryPath, "KaijuAdquisitionIncludeTamedKaijus"), 0) != 0);
		base.AIEntity.RegisterPass(AIEntity.Passes.RefreshObjectives.ToString(), "AILayer_KaijuAdquisition_RefreshObjectives", new AIEntity.AIAction(this.RefreshObjectives), this, new StaticString[0]);
		this.aILayer_ArmyManagement = base.AIEntity.GetLayer<AILayer_ArmyManagement>();
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	protected override void RefreshObjectives(StaticString context, StaticString pass)
	{
		base.RefreshObjectives(context, pass);
		base.GatherObjectives(base.ObjectiveType, true, ref this.globalObjectiveMessages);
		base.ValidateMessages(ref this.globalObjectiveMessages);
		List<Kaiju> filterKaijuTargets = this.GetFilterKaijuTargets(this.huntTamedKaijus);
		this.ComputeGlobalPriority();
		for (int i = 0; i < filterKaijuTargets.Count; i++)
		{
			Kaiju kaiju = filterKaijuTargets[i];
			if (this.IsKaijuValidForObjective(kaiju))
			{
				Region region = this.worldPositionningService.GetRegion(kaiju.WorldPosition);
				GlobalObjectiveMessage globalObjectiveMessage = base.AIEntity.AIPlayer.Blackboard.FindFirst<GlobalObjectiveMessage>(BlackboardLayerID.Empire, (GlobalObjectiveMessage match) => match.State != BlackboardMessage.StateValue.Message_Canceled && match.RegionIndex == region.Index && match.SubObjectifGUID == kaiju.GUID && match.ObjectiveType == this.ObjectiveType);
				if (globalObjectiveMessage == null)
				{
					globalObjectiveMessage = base.GenerateObjective(base.ObjectiveType, region.Index, kaiju.GUID);
				}
				this.RefreshMessagePriority(globalObjectiveMessage);
			}
		}
	}

	private void CancelMessagesFor(int regionIndex, GameEntityGUID kaijuGUID)
	{
		foreach (GlobalObjectiveMessage message in base.AIEntity.AIPlayer.Blackboard.GetMessages<GlobalObjectiveMessage>(BlackboardLayerID.Empire, (GlobalObjectiveMessage match) => match.RegionIndex == regionIndex && match.SubObjectifGUID == kaijuGUID && match.ObjectiveType == this.ObjectiveType))
		{
			base.AIEntity.AIPlayer.Blackboard.CancelMessage(message);
		}
	}

	private void CancelMessagesFor(int regionIndex)
	{
		foreach (GlobalObjectiveMessage message in base.AIEntity.AIPlayer.Blackboard.GetMessages<GlobalObjectiveMessage>(BlackboardLayerID.Empire, (GlobalObjectiveMessage match) => match.RegionIndex == regionIndex && match.ObjectiveType == this.ObjectiveType))
		{
			base.AIEntity.AIPlayer.Blackboard.CancelMessage(message);
		}
	}

	private void CreateObjectiveFor(int regionIndex, Kaiju kaiju)
	{
		GlobalObjectiveMessage globalObjectiveMessage = base.GenerateObjective(base.ObjectiveType, regionIndex, kaiju.GUID);
		this.globalObjectiveMessages.Add(globalObjectiveMessage);
		this.RefreshMessagePriority(globalObjectiveMessage);
	}

	private void RefreshMessagePriority(GlobalObjectiveMessage objectiveMessage)
	{
		objectiveMessage.GlobalPriority = base.GlobalPriority;
		objectiveMessage.LocalPriority = new HeuristicValue(0.35f);
		List<int> list = new List<int>();
		if (!DepartmentOfTheInterior.CanNeverDeclareWar(base.AIEntity.Empire) || this.departmentOfForeignAffairs.IsInWarWithSomeone())
		{
			foreach (AICommander aicommander in this.aILayer_ArmyManagement.AICommanders)
			{
				AICommander_WarWithObjective aicommander_WarWithObjective = aicommander as AICommander_WarWithObjective;
				if (aicommander_WarWithObjective != null)
				{
					Region region = this.worldPositionningService.GetRegion(aicommander_WarWithObjective.RegionIndex);
					if (region.Owner != null && region.Owner.Index != base.AIEntity.Empire.Index)
					{
						list.Add(region.Owner.Index);
					}
				}
			}
		}
		if (list.Count == 0)
		{
			objectiveMessage.LocalPriority.Boost(0.4f, "NoWarBoost", new object[0]);
		}
		Region region2 = this.worldPositionningService.GetRegion(objectiveMessage.RegionIndex);
		if (region2.Kaiju != null && region2.Kaiju.IsTamed() && list.Contains(region2.Kaiju.OwnerEmpireIndex) && this.departmentOfForeignAffairs.IsAtWarWith(region2.Kaiju.MajorEmpire))
		{
			objectiveMessage.LocalPriority.Boost(0.2f, "At war boost", new object[0]);
		}
		if (region2 != null && region2.Kaiju != null && this.departmentOfTheInterior.Cities.Count > 0)
		{
			int num = int.MaxValue;
			foreach (City city in this.departmentOfTheInterior.Cities)
			{
				int distance = this.worldPositionningService.GetDistance(city.WorldPosition, region2.Kaiju.WorldPosition);
				if (distance < num)
				{
					num = distance;
				}
			}
			if (this.departmentOfCreepingNodes != null)
			{
				foreach (CreepingNode creepingNode in this.departmentOfCreepingNodes.Nodes)
				{
					if (creepingNode.Region.Index == region2.Index && !creepingNode.IsUnderConstruction && AILayer_Exploration.IsTravelAllowedInNode(base.AIEntity.Empire, creepingNode))
					{
						int distance2 = this.worldPositionningService.GetDistance(creepingNode.WorldPosition, region2.Kaiju.WorldPosition);
						if (distance2 < num)
						{
							num = distance2;
						}
					}
				}
			}
			num = Mathf.Max(30 - num, 0);
			objectiveMessage.LocalPriority.Boost((float)num / 60f, "Close distance boost", new object[0]);
		}
		if (list.Count > 0 && !this.departmentOfForeignAffairs.IsInWarWithSomeone() && this.aiLayerStrategy.WantWarWithSomeone() && region2.Kaiju != null && region2.Kaiju.IsTamed() && (region2.Owner == null || region2.Owner.Index != base.AIEntity.Empire.Index) && !list.Contains(region2.Kaiju.OwnerEmpireIndex))
		{
			objectiveMessage.LocalPriority.Boost(-0.6f, "Not our target!", new object[0]);
		}
		objectiveMessage.TimeOut = 1;
	}

	protected override bool IsObjectiveValid(GlobalObjectiveMessage objective)
	{
		if (!(objective.ObjectiveType == base.ObjectiveType))
		{
			return false;
		}
		ArmyAction armyAction = null;
		IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
		if (database == null || !database.TryGetValue("ArmyActionTameKaiju", out armyAction))
		{
			return false;
		}
		if (!Amplitude.Unity.Runtime.Runtime.Registry.GetValue<bool>("Gameplay/Kaiju/KaijuAutoTameBeforeLoseEncounter", true) && !armyAction.CanAfford(base.AIEntity.Empire))
		{
			return false;
		}
		Region region = this.worldPositionningService.GetRegion(objective.RegionIndex);
		Kaiju kaiju = region.Kaiju;
		return kaiju != null && this.IsKaijuValidForObjective(kaiju) && !(kaiju.GUID != objective.SubObjectifGUID) && objective.State != BlackboardMessage.StateValue.Message_Canceled;
	}

	protected override bool IsObjectiveValid(StaticString objectiveType, int regionIndex, bool checkLocalPriority = false)
	{
		return true;
	}

	private bool IsKaijuValidForObjective(Kaiju kaiju)
	{
		bool flag = false;
		if (kaiju == null)
		{
			return flag;
		}
		int regionIndex = (int)this.worldPositionningService.GetRegionIndex(kaiju.WorldPosition);
		if (!this.departmentOfForeignAffairs.CanMoveOn(regionIndex, false))
		{
			return false;
		}
		if (kaiju.MajorEmpire != null)
		{
			if (!this.departmentOfForeignAffairs.CanAttack(kaiju.GetActiveTroops()))
			{
				return false;
			}
			if (kaiju.OnArmyMode())
			{
				District district = this.worldPositionningService.GetDistrict(kaiju.WorldPosition);
				if (district != null && District.IsACityTile(district))
				{
					return false;
				}
			}
			if (this.departmentOfForeignAffairs.IsInWarWithSomeone())
			{
				flag = !this.departmentOfForeignAffairs.IsAtWarWith(kaiju.MajorEmpire);
			}
			else
			{
				flag = this.departmentOfForeignAffairs.IsFriend(kaiju.MajorEmpire);
			}
		}
		if (!flag && kaiju.Empire.Index != base.AIEntity.Empire.Index && this.departmentOfTheInterior.Cities.Count > 0)
		{
			if (this.departmentOfCreepingNodes != null)
			{
				Region region = this.worldPositionningService.GetRegion(kaiju.WorldPosition);
				foreach (CreepingNode creepingNode in this.departmentOfCreepingNodes.Nodes)
				{
					if (!creepingNode.IsUnderConstruction && AILayer_Exploration.IsTravelAllowedInNode(base.AIEntity.Empire, creepingNode) && creepingNode.Region.Index == region.Index)
					{
						return true;
					}
				}
			}
			foreach (Army army in this.departmentOfDefense.Armies)
			{
				if (!army.IsSeafaring)
				{
					if (this.pathfindingService.FindPath(army, this.departmentOfTheInterior.Cities[0].WorldPosition, kaiju.WorldPosition, PathfindingManager.RequestMode.Default, null, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreKaijuGarrisons, null) == null)
					{
						return false;
					}
					return true;
				}
			}
			return false;
		}
		return false;
	}

	public override void Release()
	{
		base.Release();
		this.worldPositionningService = null;
		this.departmentOfTheInterior = null;
		this.departmentOfDefense = null;
		this.departmentOfForeignAffairs = null;
		this.departmentOfCreepingNodes = null;
		this.pathfindingService = null;
		this.aiLayerStrategy = null;
		this.aILayer_ArmyManagement = null;
	}

	protected override int GetCommanderLimit()
	{
		return this.GetFilterKaijuTargets(this.huntTamedKaijus).Count;
	}

	private void ComputeGlobalPriority()
	{
		base.GlobalPriority.Reset();
		if (this.departmentOfTheInterior.Cities.Count == 0)
		{
			base.GlobalPriority.Subtract(1f, "We should focus on getting a city!", new object[0]);
		}
		else
		{
			AILayer_Strategy layer = base.AIEntity.GetLayer<AILayer_Strategy>();
			base.GlobalPriority.Add(layer.StrategicNetwork.GetAgentValue("Expansion"), "Expansion strategic network score", new object[0]);
			float num = 1f;
			if (this.kaijuAdquisitionFactor != 0f)
			{
				num = this.kaijuAdquisitionFactor;
			}
			base.GlobalPriority.Add(layer.StrategicNetwork.GetAgentValue("Expansion") * num, "Want war but not at war", new object[0]);
			if (this.aiLayerStrategy.IsAtWar())
			{
				base.GlobalPriority.Boost(0.4f, "At war", new object[0]);
			}
			else if (this.aiLayerStrategy.WantWarWithSomeone())
			{
				base.GlobalPriority.Boost(0.2f, "Want war but not at war", new object[0]);
			}
		}
	}

	private static string registryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_War/AICommander";

	private AILayer_Strategy aiLayerStrategy;

	private IWorldPositionningService worldPositionningService;

	private IAIEmpireDataAIHelper empireDataRepository;

	private IIntelligenceAIHelper intelligenceAIHelper;

	private IPersonalityAIHelper personalityAIHelper;

	public float kaijuAdquisitionFactor = 1f;

	private bool huntTamedKaijus;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private DepartmentOfDefense departmentOfDefense;

	private IPathfindingService pathfindingService;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private DepartmentOfCreepingNodes departmentOfCreepingNodes;

	private AILayer_ArmyManagement aILayer_ArmyManagement;
}
