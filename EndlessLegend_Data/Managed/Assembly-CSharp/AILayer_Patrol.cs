using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class AILayer_Patrol : AILayerWithObjective
{
	public AILayer_Patrol() : base(AICommanderMissionDefinition.AICommanderCategory.Patrol.ToString())
	{
	}

	public static bool IsPatrolValid(global::Empire empire, Region region)
	{
		return region != null && ((empire is MajorEmpire && (empire as MajorEmpire).ConvertedVillages.Find((Village match) => match.Region == region) != null) || (region.IsLand && region.IsRegionColonized() && region.Owner == empire));
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
			for (int j = 0; j < majorEmpire.TamedKaijus.Count; j++)
			{
				Kaiju kaiju = majorEmpire.TamedKaijus[j];
				if (kaiju.OnGarrisonMode())
				{
					GlobalObjectiveMessage globalObjectiveMessage2 = this.patrolObjectives.Find((GlobalObjectiveMessage match) => match.RegionIndex == kaiju.Region.Index);
					if (globalObjectiveMessage2 == null)
					{
						globalObjectiveMessage2 = base.GenerateObjective(kaiju.Region.Index);
						this.patrolObjectives.Add(globalObjectiveMessage2);
					}
					globalObjectiveMessage2.TimeOut = 1;
					globalObjectiveMessage2.GlobalPriority = base.GlobalPriority;
					HeuristicValue heuristicValue2 = new HeuristicValue(0.6f);
					AIRegionData regionData = this.worldAtlasHelper.GetRegionData(base.AIEntity.Empire.Index, kaiju.Region.Index);
					if (regionData != null)
					{
						float operand = Mathf.Min(1f, 0.1f * (float)regionData.BorderWithNeutral + 0.2f * (float)regionData.BorderWithEnnemy);
						heuristicValue2.Boost(operand, "Border with enemy!", new object[0]);
					}
					globalObjectiveMessage2.LocalPriority = heuristicValue2;
				}
			}
		}
		for (int k = 0; k < this.departmentOfTheInterior.Cities.Count; k++)
		{
			City city = this.departmentOfTheInterior.Cities[k];
			if (this.worldAtlasHelper.IsRegionExplored(base.AIEntity.Empire, city.Region, 0.95f))
			{
				GlobalObjectiveMessage globalObjectiveMessage3 = this.patrolObjectives.Find((GlobalObjectiveMessage match) => match.RegionIndex == city.Region.Index);
				if (globalObjectiveMessage3 == null)
				{
					globalObjectiveMessage3 = base.GenerateObjective(city.Region.Index);
					this.patrolObjectives.Add(globalObjectiveMessage3);
				}
				globalObjectiveMessage3.TimeOut = 1;
				globalObjectiveMessage3.GlobalPriority = base.GlobalPriority;
				HeuristicValue heuristicValue3 = new HeuristicValue(0f);
				heuristicValue3.Add(AILayer_Military.GetCityDefenseLocalPriority(city, this.unitRatioBoost, 0), "City defense local priority", new object[0]);
				globalObjectiveMessage3.LocalPriority = heuristicValue3;
			}
		}
		if (this.departmentOfForeignAffairs.IsInWarWithSomeone())
		{
			base.GatherObjectives(AICommanderMissionDefinition.AICommanderCategory.WarPatrol.ToString(), ref this.warPatrolObjectives);
			base.ValidateMessages(ref this.warPatrolObjectives);
			if (base.AIEntity.Empire is MajorEmpire)
			{
				MajorEmpire majorEmpire2 = base.AIEntity.Empire as MajorEmpire;
				for (int l = 0; l < majorEmpire2.ConvertedVillages.Count; l++)
				{
					Village village = majorEmpire2.ConvertedVillages[l];
					GlobalObjectiveMessage globalObjectiveMessage4 = this.warPatrolObjectives.Find((GlobalObjectiveMessage match) => match.RegionIndex == village.Region.Index);
					if (globalObjectiveMessage4 == null)
					{
						globalObjectiveMessage4 = base.GenerateObjective(village.Region.Index);
						globalObjectiveMessage4.ObjectiveType = AICommanderMissionDefinition.AICommanderCategory.WarPatrol.ToString();
						this.warPatrolObjectives.Add(globalObjectiveMessage4);
					}
					globalObjectiveMessage4.TimeOut = 1;
					globalObjectiveMessage4.GlobalPriority = base.GlobalPriority;
					HeuristicValue heuristicValue4 = new HeuristicValue(0f);
					heuristicValue4.Add(layer.GetVillageUnitPriority(village, village.StandardUnits.Count), "Village unit priority", new object[0]);
					globalObjectiveMessage4.LocalPriority = heuristicValue4;
				}
				for (int m = 0; m < majorEmpire2.TamedKaijus.Count; m++)
				{
					Kaiju kaiju = majorEmpire2.TamedKaijus[m];
					if (kaiju.OnGarrisonMode())
					{
						GlobalObjectiveMessage globalObjectiveMessage5 = this.warPatrolObjectives.Find((GlobalObjectiveMessage match) => match.RegionIndex == kaiju.Region.Index);
						if (globalObjectiveMessage5 == null)
						{
							globalObjectiveMessage5 = base.GenerateObjective(kaiju.Region.Index);
							this.warPatrolObjectives.Add(globalObjectiveMessage5);
						}
						globalObjectiveMessage5.TimeOut = 1;
						globalObjectiveMessage5.GlobalPriority = base.GlobalPriority;
						HeuristicValue heuristicValue5 = new HeuristicValue(0.8f);
						AIRegionData regionData2 = this.worldAtlasHelper.GetRegionData(base.AIEntity.Empire.Index, kaiju.Region.Index);
						if (regionData2 != null)
						{
							float operand2 = Mathf.Min(1f, 0.2f * (float)regionData2.BorderWithNeutral + 0.3f * (float)regionData2.BorderWithEnnemy);
							heuristicValue5.Boost(operand2, "Border with enemy!", new object[0]);
						}
						globalObjectiveMessage5.LocalPriority = heuristicValue5;
					}
				}
			}
			for (int n = 0; n < this.departmentOfTheInterior.Cities.Count; n++)
			{
				City city = this.departmentOfTheInterior.Cities[n];
				GlobalObjectiveMessage globalObjectiveMessage6 = this.warPatrolObjectives.Find((GlobalObjectiveMessage match) => match.RegionIndex == city.Region.Index);
				if (globalObjectiveMessage6 == null)
				{
					globalObjectiveMessage6 = base.GenerateObjective(city.Region.Index);
					globalObjectiveMessage6.ObjectiveType = AICommanderMissionDefinition.AICommanderCategory.WarPatrol.ToString();
					this.warPatrolObjectives.Add(globalObjectiveMessage6);
				}
				globalObjectiveMessage6.TimeOut = 1;
				globalObjectiveMessage6.GlobalPriority = base.GlobalPriority;
				HeuristicValue heuristicValue6 = new HeuristicValue(0f);
				heuristicValue6.Add(AILayer_Military.GetCityDefenseLocalPriority(city, this.unitRatioBoost, 0), "City defense local priority", new object[0]);
				if (this.worldAtlasHelper.GetRegionData(city.Empire.Index, city.Region.Index).BorderWithEnnemy > 0)
				{
					heuristicValue6.Boost(0.2f, "Border with enemy!", new object[0]);
				}
				globalObjectiveMessage6.LocalPriority = heuristicValue6;
			}
			bool flag = false;
			for (int num = 0; num < this.warPatrolObjectives.Count; num++)
			{
				GlobalObjectiveMessage warPatrolObjective = this.warPatrolObjectives[num];
				if (base.AIEntity.GetCommanderProcessingTheNeededGlobalObjective(warPatrolObjective.ID) == null)
				{
					GlobalObjectiveMessage globalObjectiveMessage7 = this.patrolObjectives.Find((GlobalObjectiveMessage match) => match.RegionIndex == warPatrolObjective.RegionIndex);
					if (globalObjectiveMessage7 != null)
					{
						AICommander commanderProcessingTheNeededGlobalObjective = base.AIEntity.GetCommanderProcessingTheNeededGlobalObjective(globalObjectiveMessage7.ID);
						if (commanderProcessingTheNeededGlobalObjective != null)
						{
							commanderProcessingTheNeededGlobalObjective.Release();
							flag = true;
						}
					}
				}
			}
			if (flag)
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
