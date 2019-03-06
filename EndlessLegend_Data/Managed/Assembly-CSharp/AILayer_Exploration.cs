using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AILayer_Exploration : AILayerWithObjective
{
	public AILayer_Exploration() : base(AICommanderMissionDefinition.AICommanderCategory.Exploration.ToString())
	{
	}

	public static bool IsRegionValidForExploration(global::Empire empire, Region region)
	{
		return AILayer_Exploration.IsRegionValidForExploration(empire, region.Index);
	}

	public static bool IsRegionValidForExploration(global::Empire empire, int regionIndex)
	{
		Diagnostics.Assert(AIScheduler.Services != null);
		IWorldAtlasAIHelper service = AIScheduler.Services.GetService<IWorldAtlasAIHelper>();
		Diagnostics.Assert(service != null);
		if (empire != null)
		{
			DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
			if (agency != null && !agency.CanMoveOn(regionIndex, false))
			{
				return false;
			}
		}
		DepartmentOfTheInterior agency2 = empire.GetAgency<DepartmentOfTheInterior>();
		if (agency2 != null)
		{
			bool flag = false;
			for (int i = 0; i < agency2.Cities.Count; i++)
			{
				if (agency2.Cities[i].Region.ContinentID == service.Regions[regionIndex].ContinentID)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		return !service.IsRegionExplored(empire.Index, regionIndex, 0.95f);
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		this.departmentOfForeignAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		this.worldAtlasAIHelper = AIScheduler.Services.GetService<IWorldAtlasAIHelper>();
		base.AIEntity.RegisterPass(AIEntity.Passes.RefreshObjectives.ToString(), "AILayer_Exploration_RefreshObjectives", new AIEntity.AIAction(this.RefreshObjectives), this, new StaticString[]
		{
			"AILayer_Colonization_RefreshObjectives"
		});
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public override void Release()
	{
		this.departmentOfForeignAffairs = null;
		this.departmentOfTheInterior = null;
		this.worldAtlasAIHelper = null;
		base.Release();
	}

	protected override bool IsObjectiveValid(StaticString objectiveType, int regionIndex, bool checkLocalPriority = false)
	{
		return AILayer_Exploration.IsRegionValidForExploration(base.AIEntity.Empire, regionIndex);
	}

	protected override void RefreshObjectives(StaticString context, StaticString pass)
	{
		base.RefreshObjectives(context, pass);
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		base.GatherObjectives(AICommanderMissionDefinition.AICommanderCategory.Exploration.ToString(), ref this.globalObjectiveMessages);
		base.ValidateMessages(ref this.globalObjectiveMessages);
		this.regionToExplore.Clear();
		if (!this.TrySelectRegionToExplore(base.AIEntity.Empire, ref this.regionToExplore))
		{
		}
		int index;
		for (index = 0; index < this.regionToExplore.Count; index++)
		{
			if (AILayer_Exploration.IsRegionValidForExploration(base.AIEntity.Empire, this.regionToExplore[index].Index))
			{
				if (this.globalObjectiveMessages.Find((GlobalObjectiveMessage match) => match.RegionIndex == this.regionToExplore[index].Index) == null)
				{
					GlobalObjectiveMessage item = base.GenerateObjective(this.regionToExplore[index].Index);
					this.globalObjectiveMessages.Add(item);
				}
			}
		}
		this.ComputeObjectivePriority();
	}

	private void ComputeObjectivePriority()
	{
		bool flag = false;
		if (this.departmentOfForeignAffairs.IsInWarWithSomeone())
		{
			flag = true;
		}
		else
		{
			foreach (City city in this.departmentOfTheInterior.Cities)
			{
				if (!this.worldAtlasAIHelper.IsRegionPacified(base.AIEntity.Empire, city.Region))
				{
					flag = true;
					break;
				}
				if (AILayer_Pacification.RegionContainsHostileArmies(base.AIEntity.Empire, city.Region.Index))
				{
					flag = true;
					break;
				}
			}
		}
		base.GlobalPriority.Reset();
		base.GlobalPriority.Add(0.1f, "(constant)", new object[0]);
		AILayer_Colonization layer = base.AIEntity.GetLayer<AILayer_Colonization>();
		for (int i = 0; i < this.globalObjectiveMessages.Count; i++)
		{
			GlobalObjectiveMessage globalObjectiveMessage = this.globalObjectiveMessages[i];
			HeuristicValue heuristicValue = new HeuristicValue(0f);
			heuristicValue.Add(layer.GetColonizationInterest(globalObjectiveMessage.RegionIndex), "Region colo interest", new object[0]);
			heuristicValue.Multiply(0.1f, "(constant)", new object[0]);
			globalObjectiveMessage.LocalPriority = heuristicValue;
			globalObjectiveMessage.GlobalPriority = base.GlobalPriority;
			if (i < 1 && !flag && base.AIEntity.Empire.GetAgency<DepartmentOfDefense>().Armies.Count > 2)
			{
				globalObjectiveMessage.LocalPriority.Boost(0.75f, "(constant)", new object[0]);
				globalObjectiveMessage.GlobalPriority.Boost(0.75f, "(constant)", new object[0]);
			}
			globalObjectiveMessage.TimeOut = 1;
		}
	}

	private bool TrySelectRegionToExplore(global::Empire empire, ref List<Region> regionList)
	{
		Diagnostics.Assert(AIScheduler.Services != null);
		IIntelligenceAIHelper service = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		Diagnostics.Assert(service != null);
		return service.TryGetListOfRegionToExplore(base.AIEntity.Empire, 0.95f, ref regionList);
	}

	public const float RatioOfExplorationToReach = 0.95f;

	private List<Region> regionToExplore = new List<Region>();

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private IWorldAtlasAIHelper worldAtlasAIHelper;
}
