using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AILayer_Pacification : AILayerWithObjective
{
	public AILayer_Pacification() : base("Pacification")
	{
	}

	public static Army GetMaxHostileArmy(global::Empire empire, int regionIndex)
	{
		Services.GetService<IGameService>().Game.Services.GetService<IQuestManagementService>();
		Army army = null;
		DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
		if (agency == null)
		{
			return null;
		}
		foreach (Army army2 in Intelligence.GetVisibleArmiesInRegion(regionIndex, empire))
		{
			if (army2.Empire != empire && !army2.IsSeafaring && agency.CanAttack(army2) && (army == null || army2.GetPropertyValue(SimulationProperties.MilitaryPower) > army.GetPropertyValue(SimulationProperties.MilitaryPower)))
			{
				army = army2;
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
		service.Game.Services.GetService<IQuestManagementService>();
		Diagnostics.Assert(service != null);
		Region region = service.Game.Services.GetService<IWorldPositionningService>().GetRegion(regionIndex);
		if (flag && region != null && (region.City == null || region.City.Empire != empire))
		{
			if (region.City == null)
			{
				foreach (Army army in Intelligence.GetArmiesInRegion(regionIndex))
				{
					if ((army.Empire is MinorEmpire || army.Empire is LesserEmpire) && agency.CanAttack(army))
					{
						return true;
					}
				}
			}
			return false;
		}
		foreach (Army army2 in Intelligence.GetVisibleArmiesInRegion(regionIndex, empire))
		{
			if (army2.Empire != empire && agency.CanAttack(army2))
			{
				return true;
			}
		}
		return false;
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
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
		int num = this.departmentOfTheInterior.Cities.Count + 1;
		if (num < base.AIEntity.Empire.GetAgency<DepartmentOfDefense>().Armies.Count / 4)
		{
			num = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>().Armies.Count / 4;
		}
		return num;
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
			if (this.globalObjectiveMessages.Find((GlobalObjectiveMessage match) => match.RegionIndex == regionIndex) == null && AILayer_Pacification.RegionContainsHostileArmies(base.AIEntity.Empire, regionIndex))
			{
				GlobalObjectiveMessage item = base.GenerateObjective(regionIndex);
				this.globalObjectiveMessages.Add(item);
			}
		}
		for (int j = 0; j < this.departmentOfTheInterior.Cities.Count; j++)
		{
			int regionIndex = this.departmentOfTheInterior.Cities[j].Region.Index;
			if (this.globalObjectiveMessages.Find((GlobalObjectiveMessage match) => match.RegionIndex == regionIndex) == null && AILayer_Pacification.RegionContainsHostileArmies(base.AIEntity.Empire, regionIndex))
			{
				GlobalObjectiveMessage item2 = base.GenerateObjective(regionIndex);
				this.globalObjectiveMessages.Add(item2);
			}
		}
		this.ComputeObjectivePriority();
		IGameService service = Services.GetService<IGameService>();
		IQuestManagementService service2 = service.Game.Services.GetService<IQuestManagementService>();
		if (service2.IsQuestRunningForEmpire("VictoryQuest-Chapter2", base.AIEntity.Empire))
		{
			QuestBehaviour questBehaviour = service.Game.Services.GetService<IQuestRepositoryService>().GetQuestBehaviour("VictoryQuest-Chapter2", base.AIEntity.Empire.Index);
			QuestBehaviourTreeNode_Action_SpawnArmy questBehaviourTreeNode_Action_SpawnArmy;
			if (questBehaviour != null && ELCPUtilities.TryGetFirstNodeOfType<QuestBehaviourTreeNode_Action_SpawnArmy>(questBehaviour.Root as BehaviourTreeNodeController, out questBehaviourTreeNode_Action_SpawnArmy))
			{
				Region region = this.worldPositionningService.GetRegion(questBehaviourTreeNode_Action_SpawnArmy.SpawnLocations[0]);
				int regionIndex = region.Index;
				GlobalObjectiveMessage globalObjectiveMessage = this.globalObjectiveMessages.Find((GlobalObjectiveMessage match) => match.RegionIndex == regionIndex);
				if (globalObjectiveMessage == null && AILayer_Pacification.RegionContainsHostileArmies(base.AIEntity.Empire, regionIndex) && (region.City == null || region.City.Empire == base.AIEntity.Empire))
				{
					globalObjectiveMessage = base.GenerateObjective(regionIndex);
					this.globalObjectiveMessages.Add(globalObjectiveMessage);
				}
				if (globalObjectiveMessage != null)
				{
					globalObjectiveMessage.GlobalPriority.Value = 1f;
					globalObjectiveMessage.LocalPriority.Value = 1f;
				}
			}
		}
		if (service2.IsQuestRunningForEmpire("GlobalQuestCompet#0004", base.AIEntity.Empire))
		{
			QuestBehaviour questBehaviour2 = service.Game.Services.GetService<IQuestRepositoryService>().GetQuestBehaviour("GlobalQuestCompet#0004", base.AIEntity.Empire.Index);
			QuestBehaviourTreeNode_Decorator_KillArmy questBehaviourTreeNode_Decorator_KillArmy;
			if (questBehaviour2 != null && ELCPUtilities.TryGetFirstNodeOfType<QuestBehaviourTreeNode_Decorator_KillArmy>(questBehaviour2.Root as BehaviourTreeNodeController, out questBehaviourTreeNode_Decorator_KillArmy))
			{
				IGameEntityRepositoryService service3 = service.Game.Services.GetService<IGameEntityRepositoryService>();
				foreach (ulong x in questBehaviourTreeNode_Decorator_KillArmy.EnemyArmyGUIDs)
				{
					IGameEntity gameEntity = null;
					if (service3.TryGetValue(x, out gameEntity) && gameEntity is Army)
					{
						Army army = gameEntity as Army;
						Region region2 = this.worldPositionningService.GetRegion(army.WorldPosition);
						int regionIndex = region2.Index;
						GlobalObjectiveMessage globalObjectiveMessage2 = this.globalObjectiveMessages.Find((GlobalObjectiveMessage match) => match.RegionIndex == regionIndex);
						if (globalObjectiveMessage2 != null)
						{
							globalObjectiveMessage2.GlobalPriority.Value = 0.9f;
							globalObjectiveMessage2.LocalPriority.Value = 0.9f;
							return;
						}
						if (AILayer_Pacification.RegionContainsHostileArmies(base.AIEntity.Empire, regionIndex))
						{
							using (IEnumerator<Army> enumerator = Intelligence.GetArmiesInRegion(regionIndex).GetEnumerator())
							{
								while (enumerator.MoveNext())
								{
									if (enumerator.Current.Empire == base.AIEntity.Empire)
									{
										globalObjectiveMessage2 = base.GenerateObjective(regionIndex);
										this.globalObjectiveMessages.Add(globalObjectiveMessage2);
										globalObjectiveMessage2.GlobalPriority.Value = 0.9f;
										globalObjectiveMessage2.LocalPriority.Value = 0.9f;
										return;
									}
								}
							}
						}
					}
				}
			}
		}
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
				if (base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>().IsInWarWithSomeone())
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
