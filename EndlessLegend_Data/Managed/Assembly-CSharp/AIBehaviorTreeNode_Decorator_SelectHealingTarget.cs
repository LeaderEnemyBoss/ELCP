using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Decorator_SelectHealingTarget : AIBehaviorTreeNode_Decorator
{
	[XmlAttribute]
	public string Healing_TargetVarName { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) > AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		if (this.worldPositionningService.IsWaterTile(army.WorldPosition))
		{
			return State.Failure;
		}
		List<Army> targets = this.GetTargets(army);
		if (targets.Count > 0)
		{
			if (aiBehaviorTree.Variables.ContainsKey(this.Healing_TargetVarName))
			{
				aiBehaviorTree.Variables[this.Healing_TargetVarName] = targets[0];
			}
			else
			{
				aiBehaviorTree.Variables.Add(this.Healing_TargetVarName, targets[0]);
			}
			return State.Success;
		}
		return State.Failure;
	}

	private List<Army> GetTargets(Army army)
	{
		List<Army> list = new List<Army>();
		if (army == null)
		{
			return list;
		}
		List<WorldPosition> neighbours = army.WorldPosition.GetNeighbours(this.game.World.WorldParameters);
		for (int i = 0; i < neighbours.Count; i++)
		{
			Army armyAtPosition = this.worldPositionningService.GetArmyAtPosition(neighbours[i]);
			if (armyAtPosition != null && !this.worldPositionningService.IsWaterTile(armyAtPosition.WorldPosition))
			{
				if (this.encounterRepositoryService != null)
				{
					IEnumerable<Encounter> enumerable = this.encounterRepositoryService;
					if (enumerable != null && enumerable.Any((Encounter encounter) => encounter.IsGarrisonInEncounter(armyAtPosition.GUID, false)))
					{
						goto IL_137;
					}
				}
				if (this.gameEntityRepositoryService.Contains(armyAtPosition.GUID) && armyAtPosition.Empire.Index == army.Empire.Index && armyAtPosition.GetPropertyValue(SimulationProperties.MaximumHealth) * 0.9f > armyAtPosition.GetPropertyValue(SimulationProperties.Health) && this.pathfindingService.IsTransitionPassable(army.WorldPosition, armyAtPosition.WorldPosition, army, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnorePOI, null))
				{
					list.Add(armyAtPosition);
				}
			}
			IL_137:;
		}
		list.Sort((Army left, Army right) => (left.GetPropertyValue(SimulationProperties.Health) - left.GetPropertyValue(SimulationProperties.MaximumHealth)).CompareTo(right.GetPropertyValue(SimulationProperties.Health) - right.GetPropertyValue(SimulationProperties.MaximumHealth)));
		return list;
	}

	public override bool Initialize(BehaviourTree behaviourTree)
	{
		IGameService service = Services.GetService<IGameService>();
		this.encounterRepositoryService = service.Game.Services.GetService<IEncounterRepositoryService>();
		this.game = (service.Game as global::Game);
		this.pathfindingService = service.Game.Services.GetService<IPathfindingService>();
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		return base.Initialize(behaviourTree);
	}

	public override void Release()
	{
		base.Release();
		this.encounterRepositoryService = null;
		this.worldPositionningService = null;
		this.pathfindingService = null;
		this.gameEntityRepositoryService = null;
		this.game = null;
	}

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private IWorldPositionningService worldPositionningService;

	private IPathfindingService pathfindingService;

	private global::Game game;

	private IEncounterRepositoryService encounterRepositoryService;
}
