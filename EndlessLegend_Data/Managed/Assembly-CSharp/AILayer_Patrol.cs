using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AILayer_Patrol : AILayerWithObjective
{
	public AILayer_Patrol() : base(AICommanderMissionDefinition.AICommanderCategory.Patrol.ToString())
	{
	}

	public static bool IsPatrolValid(global::Empire empire, Region region)
	{
		if (region == null)
		{
			return false;
		}
		if (empire is MajorEmpire)
		{
			MajorEmpire majorEmpire = empire as MajorEmpire;
			Village village = majorEmpire.ConvertedVillages.Find((Village match) => match.Region == region);
			if (village != null)
			{
				return true;
			}
		}
		return region.City != null && region.City.Empire == empire;
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		this.worldPositionningService = gameService.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.worldAtlasHelper = AIScheduler.Services.GetService<IWorldAtlasAIHelper>();
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		this.departmentOfForeignAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		IPersonalityAIHelper personalityAIHelper = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		this.unitRatioBoost = personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_Patrol.RegistryPath, "UnitRatioBoost"), this.unitRatioBoost);
		base.AIEntity.RegisterPass(AIEntity.Passes.RefreshObjectives.ToString(), "AILayer_Patrol_RefreshObjectives", new AIEntity.AIAction(this.RefreshObjectives), this, new StaticString[0]);
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public override IEnumerator Load()
	{
		yield return base.Load();
		yield break;
	}

	public override void Release()
	{
		base.Release();
		this.worldPositionningService = null;
		this.worldAtlasHelper = null;
		this.departmentOfTheInterior = null;
		this.departmentOfForeignAffairs = null;
	}

	protected override bool IsObjectiveValid(StaticString objectiveType, int regionIndex, bool checkLocalPriority = false)
	{
		if (objectiveType == AICommanderMissionDefinition.AICommanderCategory.WarPatrol.ToString())
		{
			if (!this.departmentOfForeignAffairs.IsInWarWithSomeone())
			{
				return false;
			}
		}
		else if (objectiveType == AICommanderMissionDefinition.AICommanderCategory.Patrol.ToString())
		{
		}
		return AILayer_Patrol.IsPatrolValid(base.AIEntity.Empire, this.worldPositionningService.GetRegion(regionIndex));
	}

	protected override void RefreshObjectives(StaticString context, StaticString pass)
	{
		base.RefreshObjectives(context, pass);
		AILayer_Military layer = base.AIEntity.GetLayer<AILayer_Military>();
		base.GlobalPriority.Reset();
		AILayer_Strategy layer2 = base.AIEntity.GetLayer<AILayer_Strategy>();
		base.GlobalPriority.Add(layer2.StrategicNetwork.GetAgentValue("InternalMilitary"), "Strategic network 'InternalMilitary'", new object[0]);
		base.GlobalPriority.Boost(-0.5f, "Avoid patrol to be high", new object[0]);
		base.GatherObjectives(AICommanderMissionDefinition.AICommanderCategory.Patrol.ToString(), ref this.patrolObjectives);
		base.ValidateMessages(ref this.patrolObjectives);
		if (base.AIEntity.Empire is MajorEmpire)
		{
			MajorEmpire majorEmpire = base.AIEntity.Empire as MajorEmpire;
			for (int i = 0; i < majorEmpire.ConvertedVillages.Count; i++)
			{
				Village village = majorEmpire.ConvertedVillages[i];
				if (this.worldAtlasHelper.IsRegionExplored(base.AIEntity.Empire, village.Region, 0.95f))
				{
					GlobalObjectiveMessage globalObjectiveMessage = this.patrolObjectives.Find((GlobalObjectiveMessage match) => match.RegionIndex == village.Region.Index);
					if (globalObjectiveMessage == null)
					{
						globalObjectiveMessage = base.GenerateObjective(village.Region.Index);
						this.patrolObjectives.Add(globalObjectiveMessage);
					}
					globalObjectiveMessage.TimeOut = 1;
					globalObjectiveMessage.GlobalPriority = base.GlobalPriority;
					HeuristicValue heuristicValue = new HeuristicValue(0f);
					heuristicValue.Add(layer.GetVillageUnitPriority(village, village.StandardUnits.Count), "Village unit priority", new object[0]);
					globalObjectiveMessage.LocalPriority = heuristicValue;
				}
			}
		}
		for (int j = 0; j < this.departmentOfTheInterior.Cities.Count; j++)
		{
			City city = this.departmentOfTheInterior.Cities[j];
			if (this.worldAtlasHelper.IsRegionExplored(base.AIEntity.Empire, city.Region, 0.95f))
			{
				GlobalObjectiveMessage globalObjectiveMessage2 = this.patrolObjectives.Find((GlobalObjectiveMessage match) => match.RegionIndex == city.Region.Index);
				if (globalObjectiveMessage2 == null)
				{
					globalObjectiveMessage2 = base.GenerateObjective(city.Region.Index);
					this.patrolObjectives.Add(globalObjectiveMessage2);
				}
				globalObjectiveMessage2.TimeOut = 1;
				globalObjectiveMessage2.GlobalPriority = base.GlobalPriority;
				HeuristicValue heuristicValue2 = new HeuristicValue(0f);
				heuristicValue2.Add(AILayer_Military.GetCityDefenseLocalPriority(city, this.unitRatioBoost, 0), "City defense local priority", new object[0]);
				globalObjectiveMessage2.LocalPriority = heuristicValue2;
			}
		}
		bool flag = this.departmentOfForeignAffairs.IsInWarWithSomeone();
		if (flag)
		{
			base.GatherObjectives(AICommanderMissionDefinition.AICommanderCategory.WarPatrol.ToString(), ref this.warPatrolObjectives);
			base.ValidateMessages(ref this.warPatrolObjectives);
			if (base.AIEntity.Empire is MajorEmpire)
			{
				MajorEmpire majorEmpire2 = base.AIEntity.Empire as MajorEmpire;
				for (int k = 0; k < majorEmpire2.ConvertedVillages.Count; k++)
				{
					Village village = majorEmpire2.ConvertedVillages[k];
					GlobalObjectiveMessage globalObjectiveMessage3 = this.warPatrolObjectives.Find((GlobalObjectiveMessage match) => match.RegionIndex == village.Region.Index);
					if (globalObjectiveMessage3 == null)
					{
						globalObjectiveMessage3 = base.GenerateObjective(village.Region.Index);
						globalObjectiveMessage3.ObjectiveType = AICommanderMissionDefinition.AICommanderCategory.WarPatrol.ToString();
						this.warPatrolObjectives.Add(globalObjectiveMessage3);
					}
					globalObjectiveMessage3.TimeOut = 1;
					globalObjectiveMessage3.GlobalPriority = base.GlobalPriority;
					HeuristicValue heuristicValue3 = new HeuristicValue(0f);
					heuristicValue3.Add(layer.GetVillageUnitPriority(village, village.StandardUnits.Count), "Village unit priority", new object[0]);
					globalObjectiveMessage3.LocalPriority = heuristicValue3;
				}
			}
			for (int l = 0; l < this.departmentOfTheInterior.Cities.Count; l++)
			{
				City city = this.departmentOfTheInterior.Cities[l];
				GlobalObjectiveMessage globalObjectiveMessage4 = this.warPatrolObjectives.Find((GlobalObjectiveMessage match) => match.RegionIndex == city.Region.Index);
				if (globalObjectiveMessage4 == null)
				{
					globalObjectiveMessage4 = base.GenerateObjective(city.Region.Index);
					globalObjectiveMessage4.ObjectiveType = AICommanderMissionDefinition.AICommanderCategory.WarPatrol.ToString();
					this.warPatrolObjectives.Add(globalObjectiveMessage4);
				}
				globalObjectiveMessage4.TimeOut = 1;
				globalObjectiveMessage4.GlobalPriority = base.GlobalPriority;
				HeuristicValue heuristicValue4 = new HeuristicValue(0f);
				heuristicValue4.Add(AILayer_Military.GetCityDefenseLocalPriority(city, this.unitRatioBoost, 0), "City defense local priority", new object[0]);
				AIRegionData regionData = this.worldAtlasHelper.GetRegionData(city.Empire.Index, city.Region.Index);
				if (regionData.BorderWithEnnemy > 0)
				{
					heuristicValue4.Boost(0.2f, "Border with enemy!", new object[0]);
				}
				globalObjectiveMessage4.LocalPriority = heuristicValue4;
			}
			bool flag2 = false;
			for (int m = 0; m < this.warPatrolObjectives.Count; m++)
			{
				GlobalObjectiveMessage warPatrolObjective = this.warPatrolObjectives[m];
				if (base.AIEntity.GetCommanderProcessingTheNeededGlobalObjective(warPatrolObjective.ID) == null)
				{
					GlobalObjectiveMessage globalObjectiveMessage5 = this.patrolObjectives.Find((GlobalObjectiveMessage match) => match.RegionIndex == warPatrolObjective.RegionIndex);
					if (globalObjectiveMessage5 != null)
					{
						AICommander commanderProcessingTheNeededGlobalObjective = base.AIEntity.GetCommanderProcessingTheNeededGlobalObjective(globalObjectiveMessage5.ID);
						if (commanderProcessingTheNeededGlobalObjective != null)
						{
							commanderProcessingTheNeededGlobalObjective.Release();
							flag2 = true;
						}
					}
				}
			}
			if (flag2)
			{
				base.AIEntity.KillAllCommanders("AICommander_Exploration");
			}
		}
	}

	public static string RegistryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_Patrol";

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private List<GlobalObjectiveMessage> patrolObjectives = new List<GlobalObjectiveMessage>();

	private float unitRatioBoost = 0.8f;

	private List<GlobalObjectiveMessage> warPatrolObjectives = new List<GlobalObjectiveMessage>();

	private IWorldAtlasAIHelper worldAtlasHelper;

	private IWorldPositionningService worldPositionningService;
}
