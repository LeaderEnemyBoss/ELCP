using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Decorator_DestinationReached : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_DestinationReached()
	{
		this.TypeOfCheck = AIBehaviorTreeNode_Decorator_DestinationReached.DestinationReachAlgorithm.Regular;
		this.Range = 3;
	}

	[XmlAttribute]
	public string DestinationVarName { get; set; }

	[XmlAttribute]
	public int Range { get; set; }

	[XmlAttribute]
	public AIBehaviorTreeNode_Decorator_DestinationReached.DestinationReachAlgorithm TypeOfCheck { get; set; }

	public override void Release()
	{
		base.Release();
		this.pathfindingService = null;
	}

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) != AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		if (!aiBehaviorTree.Variables.ContainsKey(this.DestinationVarName))
		{
			return State.Failure;
		}
		WorldPosition worldPosition = (WorldPosition)aiBehaviorTree.Variables[this.DestinationVarName];
		if (!worldPosition.IsValid)
		{
			aiBehaviorTree.ErrorCode = 2;
			return State.Failure;
		}
		IWorldPositionEvaluationAIHelper service = AIScheduler.Services.GetService<IWorldPositionEvaluationAIHelper>();
		Diagnostics.Assert(service != null);
		switch (this.TypeOfCheck)
		{
		case AIBehaviorTreeNode_Decorator_DestinationReached.DestinationReachAlgorithm.Regular:
			if (army.WorldPosition == worldPosition)
			{
				return State.Success;
			}
			break;
		case AIBehaviorTreeNode_Decorator_DestinationReached.DestinationReachAlgorithm.InVisionRangeDestination:
			if (service.IsPositionInRange(army.WorldPosition, worldPosition, army.LineOfSightVisionRange))
			{
				return State.Success;
			}
			break;
		case AIBehaviorTreeNode_Decorator_DestinationReached.DestinationReachAlgorithm.Attack:
			if (service.IsPositionInRange(army.WorldPosition, worldPosition, 1) && !army.Sails)
			{
				if (this.pathfindingService == null)
				{
					IGameService service2 = Services.GetService<IGameService>();
					this.pathfindingService = service2.Game.Services.GetService<IPathfindingService>();
				}
				if (this.pathfindingService.IsTransitionPassable(army.WorldPosition, worldPosition, army, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnoreZoneOfControl | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreKaijuGarrisons, null))
				{
					return State.Success;
				}
				if (army.WorldPosition == worldPosition)
				{
					return State.Success;
				}
			}
			break;
		case AIBehaviorTreeNode_Decorator_DestinationReached.DestinationReachAlgorithm.InRange:
			if (service.IsPositionInRange(army.WorldPosition, worldPosition, this.Range))
			{
				return State.Success;
			}
			break;
		}
		if (army.GetPropertyValue(SimulationProperties.Movement) < 0.001f)
		{
			aiBehaviorTree.ErrorCode = 24;
			return State.Failure;
		}
		aiBehaviorTree.ErrorCode = 5;
		return State.Failure;
	}

	protected override bool Initialize(AIBehaviorTree questBehaviour)
	{
		return base.Initialize(questBehaviour);
	}

	private IPathfindingService pathfindingService;

	public enum DestinationReachAlgorithm
	{
		Regular,
		InVisionRangeDestination,
		Attack,
		InRange
	}
}
