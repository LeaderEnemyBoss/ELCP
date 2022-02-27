using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Decorator_OptimizeAttackPosition : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_OptimizeAttackPosition()
	{
		this.TargetVarName = null;
		this.Inverted = false;
	}

	[XmlAttribute]
	public bool Inverted { get; set; }

	[XmlAttribute]
	public string TargetVarName { get; set; }

	[XmlAttribute]
	public string Output_BestAttackPositionVarName { get; set; }

	public override void Release()
	{
		base.Release();
		this.intelligenceAiHelper = null;
		this.pathfindingService = null;
		this.worldPositionningService = null;
	}

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army;
		AIArmyMission.AIArmyMissionErrorCode armyUnlessLocked = base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army);
		if (armyUnlessLocked != AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		if (!aiBehaviorTree.Variables.ContainsKey(this.TargetVarName))
		{
			return State.Failure;
		}
		Garrison garrison = aiBehaviorTree.Variables[this.TargetVarName] as Garrison;
		if (garrison == null)
		{
			aiBehaviorTree.ErrorCode = 10;
			return State.Failure;
		}
		if (garrison == army)
		{
			return State.Failure;
		}
		IWorldPositionable worldPositionable = garrison as IWorldPositionable;
		if (worldPositionable == null)
		{
			return State.Failure;
		}
		if (this.worldPositionningService.GetDistance(army.WorldPosition, worldPositionable.WorldPosition) > 1)
		{
			return State.Failure;
		}
		float propertyValue = army.GetPropertyValue(SimulationProperties.Movement);
		if (propertyValue <= 0f)
		{
			return State.Failure;
		}
		WorldPosition bestAttackPosition = this.GetBestAttackPosition(army, garrison, worldPositionable);
		if (bestAttackPosition != army.WorldPosition)
		{
			if (aiBehaviorTree.Variables.ContainsKey(this.Output_BestAttackPositionVarName))
			{
				aiBehaviorTree.Variables[this.Output_BestAttackPositionVarName] = bestAttackPosition;
			}
			else
			{
				aiBehaviorTree.Variables.Add(this.Output_BestAttackPositionVarName, bestAttackPosition);
			}
			return State.Success;
		}
		return State.Failure;
	}

	protected override bool Initialize(AIBehaviorTree questBehaviour)
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.pathfindingService = service.Game.Services.GetService<IPathfindingService>();
		Diagnostics.Assert(this.pathfindingService != null);
		this.intelligenceAiHelper = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		return base.Initialize(questBehaviour);
	}

	private WorldPosition GetBestAttackPosition(Army army, Garrison target, IWorldPositionable targetWorldPositionable)
	{
		bool flag = this.worldPositionningService.IsWaterTile(targetWorldPositionable.WorldPosition);
		WorldPosition worldPosition = army.WorldPosition;
		float num = 0f;
		float num2 = 0f;
		this.intelligenceAiHelper.EstimateMPInBattleground(army, worldPosition, target, ref num2, ref num);
		if (num == 0f)
		{
			num = 1f;
		}
		float num3 = num2 / num;
		float num4 = 0f;
		float num5 = 0f;
		WorldOrientation orientation = this.worldPositionningService.GetOrientation(targetWorldPositionable.WorldPosition, army.WorldPosition);
		WorldOrientation direction = orientation.Rotate(-1);
		WorldPosition neighbourTile = this.worldPositionningService.GetNeighbourTile(targetWorldPositionable.WorldPosition, direction, 1);
		if (neighbourTile.IsValid && this.worldPositionningService.IsWaterTile(neighbourTile) == flag && this.pathfindingService.IsTransitionPassable(neighbourTile, targetWorldPositionable.WorldPosition, army, OrderAttack.AttackFlags, null) && this.pathfindingService.IsTileStopableAndPassable(neighbourTile, army, PathfindingFlags.IgnoreFogOfWar, null) && this.pathfindingService.IsTransitionPassable(neighbourTile, army.WorldPosition, army, OrderAttack.AttackFlags, null))
		{
			this.intelligenceAiHelper.EstimateMPInBattleground(army, neighbourTile, target, ref num5, ref num4);
			if (num5 == 0f)
			{
				num5 = 1f;
			}
			float num6 = num5 / num4;
			if (num3 < num6)
			{
				num3 = num6;
				num2 = num5;
				num = num4;
				worldPosition = neighbourTile;
			}
		}
		WorldOrientation direction2 = orientation.Rotate(1);
		WorldPosition neighbourTile2 = this.worldPositionningService.GetNeighbourTile(targetWorldPositionable.WorldPosition, direction2, 1);
		if (neighbourTile2.IsValid && this.worldPositionningService.IsWaterTile(neighbourTile2) == flag && this.pathfindingService.IsTransitionPassable(neighbourTile2, targetWorldPositionable.WorldPosition, army, OrderAttack.AttackFlags, null) && this.pathfindingService.IsTileStopableAndPassable(neighbourTile2, army, PathfindingFlags.IgnoreFogOfWar, null) && this.pathfindingService.IsTransitionPassable(neighbourTile2, army.WorldPosition, army, OrderAttack.AttackFlags, null))
		{
			this.intelligenceAiHelper.EstimateMPInBattleground(army, neighbourTile2, target, ref num5, ref num4);
			if (num5 == 0f)
			{
				num5 = 1f;
			}
			float num7 = num5 / num4;
			if (num3 < num7)
			{
				num2 = num5;
				num = num4;
				worldPosition = neighbourTile2;
			}
		}
		return worldPosition;
	}

	private IWorldPositionningService worldPositionningService;

	private IIntelligenceAIHelper intelligenceAiHelper;

	private IPathfindingService pathfindingService;
}
