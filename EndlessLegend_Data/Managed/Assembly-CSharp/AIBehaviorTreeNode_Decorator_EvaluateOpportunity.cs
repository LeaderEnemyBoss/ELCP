using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Decorator_EvaluateOpportunity : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_EvaluateOpportunity()
	{
		this.OpportunityType = AIBehaviorTreeNode_Decorator_EvaluateOpportunity.TargetType.Army;
		this.MainTargetType = AIBehaviorTreeNode_Decorator_EvaluateOpportunity.TargetType.Army;
		this.MinimumTurnToObjectif = 2f;
		this.MaximumDetourTurn = 2f;
	}

	[XmlAttribute]
	public string MainTargetPosition { get; set; }

	[XmlAttribute]
	public AIBehaviorTreeNode_Decorator_EvaluateOpportunity.TargetType MainTargetType { get; set; }

	[XmlAttribute]
	public float MaximumDetourTurn { get; set; }

	[XmlAttribute]
	public float MinimumTurnToObjectif { get; set; }

	[XmlAttribute]
	public string OpportunityPosition { get; set; }

	[XmlAttribute]
	public AIBehaviorTreeNode_Decorator_EvaluateOpportunity.TargetType OpportunityType { get; set; }

	[XmlAttribute]
	public string MaximumDetourTurnVariableName { get; set; }

	[XmlAttribute]
	public string MainTargetTypeVariableName { get; set; }

	public static bool IsDetourWorthCheckingFast(IWorldPositionningService worldPositionningService, Army army, WorldPosition opportunityPosition, WorldPosition mainTargetPosition, out int numberOfTurnsTillMainTarget, out int numberOfTurnsAfterDetour)
	{
		int distance = worldPositionningService.GetDistance(army.WorldPosition, opportunityPosition);
		int distance2 = worldPositionningService.GetDistance(army.WorldPosition, mainTargetPosition);
		numberOfTurnsTillMainTarget = 0;
		numberOfTurnsAfterDetour = 0;
		if (distance2 < distance)
		{
			return false;
		}
		int distance3 = worldPositionningService.GetDistance(opportunityPosition, mainTargetPosition);
		float propertyValue = army.GetPropertyValue(SimulationProperties.MaximumMovement);
		numberOfTurnsTillMainTarget = (int)((float)distance2 / propertyValue);
		if (distance < 2)
		{
			numberOfTurnsAfterDetour = numberOfTurnsTillMainTarget;
			return true;
		}
		numberOfTurnsAfterDetour = (int)((float)(distance + distance3) / propertyValue);
		return true;
	}

	public override bool Initialize(BehaviourTree behaviourTree)
	{
		IGameService service = Services.GetService<IGameService>();
		this.pathfindingService = service.Game.Services.GetService<IPathfindingService>();
		Diagnostics.Assert(this.pathfindingService != null);
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.armyActionDatabase = Databases.GetDatabase<ArmyAction>(false);
		Diagnostics.Assert(this.armyActionDatabase != null);
		return base.Initialize(behaviourTree);
	}

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
		if (!aiBehaviorTree.Variables.ContainsKey(this.OpportunityPosition) || !aiBehaviorTree.Variables.ContainsKey(this.MainTargetPosition))
		{
			return State.Failure;
		}
		WorldPosition opportunityPosition = (WorldPosition)aiBehaviorTree.Variables[this.OpportunityPosition];
		WorldPosition mainTargetPosition = (WorldPosition)aiBehaviorTree.Variables[this.MainTargetPosition];
		if (!opportunityPosition.IsValid || !mainTargetPosition.IsValid)
		{
			return State.Failure;
		}
		if (!string.IsNullOrEmpty(this.MaximumDetourTurnVariableName) && aiBehaviorTree.Variables.ContainsKey(this.MaximumDetourTurnVariableName))
		{
			this.MaximumDetourTurn = (float)aiBehaviorTree.Variables[this.MaximumDetourTurnVariableName];
		}
		if (!string.IsNullOrEmpty(this.MainTargetTypeVariableName) && aiBehaviorTree.Variables.ContainsKey(this.MainTargetTypeVariableName))
		{
			this.MainTargetType = (AIBehaviorTreeNode_Decorator_EvaluateOpportunity.TargetType)((int)Enum.Parse(typeof(AIBehaviorTreeNode_Decorator_EvaluateOpportunity.TargetType), aiBehaviorTree.Variables[this.MainTargetTypeVariableName] as string));
		}
		int num;
		int num2;
		if (!AIBehaviorTreeNode_Decorator_EvaluateOpportunity.IsDetourWorthCheckingFast(this.worldPositionningService, army, opportunityPosition, mainTargetPosition, out num, out num2))
		{
			return State.Failure;
		}
		if (num == 0)
		{
			if (num2 == 0)
			{
				float propertyValue = army.GetPropertyValue(SimulationProperties.MaximumNumberOfActionPoints);
				float propertyValue2 = army.GetPropertyValue(SimulationProperties.ActionPointsSpent);
				float num3 = propertyValue - propertyValue2;
				float neededActionPointsForTheNeededAction = this.GetNeededActionPointsForTheNeededAction(this.OpportunityType);
				float neededActionPointsForTheNeededAction2 = this.GetNeededActionPointsForTheNeededAction(this.MainTargetType);
				if (num3 - neededActionPointsForTheNeededAction >= neededActionPointsForTheNeededAction2)
				{
					return State.Success;
				}
			}
			return State.Failure;
		}
		if ((float)(num2 - num) <= this.MaximumDetourTurn)
		{
			return State.Success;
		}
		if ((float)num <= this.MinimumTurnToObjectif)
		{
			return State.Failure;
		}
		return State.Success;
	}

	protected override bool Initialize(AIBehaviorTree questBehaviour)
	{
		return base.Initialize(questBehaviour);
	}

	private float GetNeededActionPointsForTheNeededAction(AIBehaviorTreeNode_Decorator_EvaluateOpportunity.TargetType actionType)
	{
		ArmyAction armyAction = null;
		switch (actionType)
		{
		case AIBehaviorTreeNode_Decorator_EvaluateOpportunity.TargetType.Army:
			this.armyActionDatabase.TryGetValue(ArmyAction_Attack.ReadOnlyName, out armyAction);
			break;
		case AIBehaviorTreeNode_Decorator_EvaluateOpportunity.TargetType.Ruin:
			this.armyActionDatabase.TryGetValue("ArmyActionSearch", out armyAction);
			break;
		case AIBehaviorTreeNode_Decorator_EvaluateOpportunity.TargetType.City:
			this.armyActionDatabase.TryGetValue(ArmyAction_Attack.ReadOnlyName, out armyAction);
			break;
		case AIBehaviorTreeNode_Decorator_EvaluateOpportunity.TargetType.Village:
			this.armyActionDatabase.TryGetValue(ArmyAction_Attack.ReadOnlyName, out armyAction);
			break;
		case AIBehaviorTreeNode_Decorator_EvaluateOpportunity.TargetType.Kaiju:
			this.armyActionDatabase.TryGetValue(ArmyAction_TameKaiju.ReadOnlyName, out armyAction);
			break;
		}
		if (armyAction != null)
		{
			return armyAction.GetCostInActionPoints();
		}
		return 0f;
	}

	private bool IsDetourWorthChecking(Army army, WorldPosition opportunityPosition, WorldPosition mainTargetPosition, out int numberOfTurnsTillMainTarget, out int numberOfTurnsAfterDetour)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		numberOfTurnsTillMainTarget = 0;
		numberOfTurnsAfterDetour = 0;
		PathfindingContext pathfindingContext = army.GenerateContext();
		pathfindingContext.Greedy = true;
		PathfindingResult pathfindingResult = this.pathfindingService.FindPath(pathfindingContext, army.WorldPosition, opportunityPosition, PathfindingManager.RequestMode.Default, null, PathfindingFlags.IgnoreFogOfWar, null);
		if (pathfindingResult == null)
		{
			if (this.worldPositionningService.GetDistance(army.WorldPosition, opportunityPosition) != 1)
			{
				return false;
			}
		}
		else
		{
			num = pathfindingResult.CompletPathLength;
		}
		PathfindingResult pathfindingResult2 = this.pathfindingService.FindPath(pathfindingContext, army.WorldPosition, mainTargetPosition, PathfindingManager.RequestMode.Default, null, PathfindingFlags.IgnoreFogOfWar, null);
		if (pathfindingResult2 == null)
		{
			if (this.worldPositionningService.GetDistance(army.WorldPosition, mainTargetPosition) != 1)
			{
				return false;
			}
		}
		else
		{
			num2 = pathfindingResult2.CompletPathLength;
		}
		if (num2 < num)
		{
			return false;
		}
		PathfindingResult pathfindingResult3 = this.pathfindingService.FindPath(pathfindingContext, opportunityPosition, mainTargetPosition, PathfindingManager.RequestMode.Default, null, PathfindingFlags.IgnoreFogOfWar, null);
		if (pathfindingResult3 == null)
		{
			if (this.worldPositionningService.GetDistance(opportunityPosition, mainTargetPosition) != 1)
			{
				return false;
			}
		}
		else
		{
			num3 = pathfindingResult3.CompletPathLength;
		}
		float propertyValue = army.GetPropertyValue(SimulationProperties.MaximumMovement);
		numberOfTurnsTillMainTarget = (int)((float)num2 / propertyValue);
		numberOfTurnsAfterDetour = (int)((float)(num + num3) / propertyValue);
		return true;
	}

	private IDatabase<ArmyAction> armyActionDatabase;

	private IPathfindingService pathfindingService;

	private IWorldPositionningService worldPositionningService;

	public enum TargetType
	{
		Army,
		Ruin,
		City,
		Village,
		ArmySupport,
		CityToSpy,
		Kaiju
	}
}
