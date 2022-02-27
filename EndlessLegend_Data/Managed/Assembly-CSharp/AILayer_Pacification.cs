using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AILayer_Pacification : AILayerWithObjective
{
	public AILayer_Pacification() : base("Pacification")
	{
	}

	public static Army GetMaxHostileArmy(global::Empire empire, int regionIndex)
	{
		Army army = null;
		DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
		if (agency == null)
		{
			return null;
		}
		foreach (Army army2 in Intelligence.GetVisibleArmiesInRegion(regionIndex, empire))
		{
			if (army2.Empire != empire)
			{
				if (!agency.IsInWarWithSomeone() || army2.Empire is MajorEmpire)
				{
					if (agency.CanAttack(army2) && (army == null || army2.GetPropertyValue(SimulationProperties.MilitaryPower) > army.GetPropertyValue(SimulationProperties.MilitaryPower)))
					{
						army = army2;
					}
				}
			}
		}
		return army;
	}

	public static bool RegionContainsHostileArmies(global::Empire empire, int regionIndex)
	{
		if (empire == null)
		{
			return false;
		}
		DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
		if (agency == null)
		{
			return false;
		}
		bool flag = agency.IsInWarWithSomeone();
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		Region region = service2.GetRegion(regionIndex);
		if (flag && region != null && (region.City == null || region.City.Empire != empire))
		{
			return false;
		}
		foreach (Army army in Intelligence.GetVisibleArmiesInRegion(regionIndex, empire))
		{
			if (army.Empire != empire)
			{
				if (agency.CanAttack(army))
				{
					if (flag && army.Empire is MajorEmpire)
					{
						return true;
					}
					if (!flag)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		this.worldPositionningService = gameService.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		base.AIEntity.RegisterPass(AIEntity.Passes.RefreshObjectives.ToString(), "AILayer_Pacification_RefreshObjectives", new AIEntity.AIAction(this.RefreshObjectives), this, new StaticString[]
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
		base.Release();
		this.departmentOfTheInterior = null;
		this.worldPositionningService = null;
		this.colonizationObjectives.Clear();
		this.colonizationObjectives = null;
	}

	protected override int GetCommanderLimit()
	{
		int count = this.departmentOfTheInterior.Cities.Count;
		return count + 1;
	}

	protected override bool IsObjectiveValid(StaticString objectiveType, int regionIndex, bool checkLocalPriority = false)
	{
		return AILayer_Pacification.RegionContainsHostileArmies(base.AIEntity.Empire, regionIndex);
	}

	protected override void RefreshObjectives(StaticString context, StaticString pass)
	{
		base.RefreshObjectives(context, pass);
		base.GatherObjectives(AICommanderMissionDefinition.AICommanderCategory.Pacification.ToString(), false, ref this.globalObjectiveMessages);
		base.ValidateMessages(ref this.globalObjectiveMessages);
		base.GatherObjectives(AICommanderMissionDefinition.AICommanderCategory.Colonization.ToString(), ref this.colonizationObjectives);
		this.colonizationObjectives.Sort((GlobalObjectiveMessage left, GlobalObjectiveMessage right) => -1 * left.LocalPriority.CompareTo(right.LocalPriority));
		for (int i = 0; i < this.colonizationObjectives.Count; i++)
		{
			int regionIndex = this.colonizationObjectives[i].RegionIndex;
			if (this.globalObjectiveMessages.Find((GlobalObjectiveMessage match) => match.RegionIndex == regionIndex) == null)
			{
				if (AILayer_Pacification.RegionContainsHostileArmies(base.AIEntity.Empire, regionIndex))
				{
					GlobalObjectiveMessage item = base.GenerateObjective(regionIndex);
					this.globalObjectiveMessages.Add(item);
				}
			}
		}
		for (int j = 0; j < this.departmentOfTheInterior.Cities.Count; j++)
		{
			int regionIndex = this.departmentOfTheInterior.Cities[j].Region.Index;
			if (this.globalObjectiveMessages.Find((GlobalObjectiveMessage match) => match.RegionIndex == regionIndex) == null)
			{
				if (AILayer_Pacification.RegionContainsHostileArmies(base.AIEntity.Empire, regionIndex))
				{
					GlobalObjectiveMessage item = base.GenerateObjective(regionIndex);
					this.globalObjectiveMessages.Add(item);
				}
			}
		}
		this.ComputeObjectivePriority();
	}

	private void ComputeObjectivePriority()
	{
		base.GlobalPriority.Reset();
		AILayer_Strategy layer = base.AIEntity.GetLayer<AILayer_Strategy>();
		base.GlobalPriority.Add(layer.StrategicNetwork.GetAgentValue("Pacification"), "Startegic network 'Pacification'", new object[0]);
		for (int i = 0; i < this.globalObjectiveMessages.Count; i++)
		{
			GlobalObjectiveMessage globalObjectiveMessage = this.globalObjectiveMessages[i];
			HeuristicValue heuristicValue = new HeuristicValue(0f);
			heuristicValue.Add(0.5f, "(constant)", new object[0]);
			Region region = this.worldPositionningService.GetRegion(globalObjectiveMessage.RegionIndex);
			if (region.City != null && region.City.Empire == base.AIEntity.Empire)
			{
				DepartmentOfForeignAffairs agency = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
				bool flag = agency.IsInWarWithSomeone();
				if (flag)
				{
					heuristicValue.Boost(0.5f, "At war", new object[0]);
				}
				if ((float)region.City.UnitsCount < (float)region.City.MaximumUnitSlot * 0.5f)
				{
					heuristicValue.Boost(0.2f, "City defense low", new object[0]);
				}
			}
			globalObjectiveMessage.LocalPriority = heuristicValue;
			globalObjectiveMessage.GlobalPriority = base.GlobalPriority;
			globalObjectiveMessage.TimeOut = 1;
		}
	}

	private List<GlobalObjectiveMessage> colonizationObjectives = new List<GlobalObjectiveMessage>();

	private DepartmentOfTheInterior departmentOfTheInterior;

	private IWorldPositionningService worldPositionningService;
}
