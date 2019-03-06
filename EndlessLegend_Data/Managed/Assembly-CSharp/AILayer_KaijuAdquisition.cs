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
		this.aiLayerStrategy = base.AIEntity.GetLayer<AILayer_Strategy>();
		this.empireDataRepository = AIScheduler.Services.GetService<IAIEmpireDataAIHelper>();
		this.intelligenceAIHelper = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		this.personalityAIHelper = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		this.kaijuAdquisitionFactor = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_KaijuAdquisition.registryPath, "KaijuAdquisitionFactor"), this.kaijuAdquisitionFactor);
		this.huntTamedKaijus = (this.personalityAIHelper.GetRegistryValue<int>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_KaijuAdquisition.registryPath, "KaijuAdquisitionIncludeTamedKaijus"), 0) != 0);
		base.AIEntity.RegisterPass(AIEntity.Passes.RefreshObjectives.ToString(), "AILayer_KaijuAdquisition_RefreshObjectives", new AIEntity.AIAction(this.RefreshObjectives), this, new StaticString[0]);
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
		objectiveMessage.LocalPriority = new HeuristicValue(0.6f);
		Region region = this.worldPositionningService.GetRegion(objectiveMessage.RegionIndex);
		if (region != null && this.departmentOfTheInterior.Cities.Count > 0)
		{
			int num = this.worldPositionningService.GetDistance(this.departmentOfTheInterior.Cities[0].WorldPosition, region.Barycenter);
			num = Mathf.Max(40 - num, 0);
			objectiveMessage.LocalPriority.Boost((float)num / 80f, "Close distance boost", new object[0]);
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
		if (kaiju != null && kaiju.MajorEmpire != null)
		{
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
		if (kaiju != null && !flag && kaiju.Empire.Index != base.AIEntity.Empire.Index && this.departmentOfTheInterior.Cities.Count > 0)
		{
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
		this.pathfindingService = null;
		this.aiLayerStrategy = null;
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
}
