using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Decorator_GetNextRoamingPosition : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_GetNextRoamingPosition()
	{
		this.visitedPositions = new List<WorldPosition>();
		this.PositionVarName = string.Empty;
		this.LastResort = false;
	}

	[XmlAttribute]
	public string Output_DestinationVarName { get; set; }

	[XmlAttribute]
	public string TargetRegionVarName { get; set; }

	public override bool Initialize(BehaviourTree behaviourTree)
	{
		IGameService service = Services.GetService<IGameService>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		this.pathfindingService = service.Game.Services.GetService<IPathfindingService>();
		return base.Initialize(behaviourTree);
	}

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		return this.DoExecute(aiBehaviorTree, parameters);
	}

	private void AddOutputPositionToBehaviorTree(AIBehaviorTree aiBehaviorTree, WorldPosition position)
	{
		if (aiBehaviorTree.Variables.ContainsKey(this.Output_DestinationVarName))
		{
			aiBehaviorTree.Variables[this.Output_DestinationVarName] = position;
		}
		else
		{
			aiBehaviorTree.Variables.Add(this.Output_DestinationVarName, position);
		}
	}

	private bool CheckIfPositionIsABorder(int roamingRegionIndex, WorldPosition startingPosition)
	{
		WorldOrientation worldOrientation = WorldOrientation.East;
		for (int i = 0; i < 6; i++)
		{
			WorldPosition neighbourTile = this.worldPositionningService.GetNeighbourTile(startingPosition, worldOrientation, 1);
			if ((int)this.worldPositionningService.GetRegionIndex(neighbourTile) != roamingRegionIndex)
			{
				return true;
			}
			worldOrientation = worldOrientation.Rotate(1);
		}
		return false;
	}

	private bool CheckPosition(Army currentArmy, int roamingRegionIndex, WorldPosition candidate)
	{
		return candidate.IsValid && this.worldPositionningService.IsWaterTile(candidate) == currentArmy.IsSeafaring && (int)this.worldPositionningService.GetRegionIndex(candidate) == roamingRegionIndex && this.pathfindingService.IsTileStopableAndPassable(candidate, currentArmy, PathfindingFlags.IgnoreFogOfWar, null);
	}

	private State DoExecute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) != AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		Region region;
		int num;
		if (this.PositionVarName != string.Empty && aiBehaviorTree.Variables.ContainsKey(this.PositionVarName))
		{
			WorldPosition position = (WorldPosition)aiBehaviorTree.Variables[this.PositionVarName];
			region = this.worldPositionningService.GetRegion(position);
			if (region == null)
			{
				aiBehaviorTree.LogError("Target region is not valid.", new object[0]);
				return State.Failure;
			}
			num = region.Index;
		}
		else
		{
			if (string.IsNullOrEmpty(this.TargetRegionVarName) || !aiBehaviorTree.Variables.ContainsKey(this.TargetRegionVarName))
			{
				aiBehaviorTree.LogError("Target region variable '{0}' not initialized.", new object[]
				{
					this.TargetRegionVarName
				});
				return State.Failure;
			}
			num = (int)aiBehaviorTree.Variables[this.TargetRegionVarName];
			region = this.worldPositionningService.World.Regions[num];
			if (region == null)
			{
				aiBehaviorTree.LogError("Target region is not valid.", new object[0]);
				return State.Failure;
			}
		}
		WorldOrientation lastOrientation = WorldOrientation.East;
		if (aiBehaviorTree.Variables.ContainsKey("LastRoamingOrientation"))
		{
			lastOrientation = (WorldOrientation)((int)aiBehaviorTree.Variables["LastRoamingOrientation"]);
		}
		int direction = -1;
		if (army.GUID % 2UL == 0UL)
		{
			direction = 1;
		}
		WorldPosition position2;
		WorldOrientation worldOrientation;
		if (!this.LastResort)
		{
			if (!this.CheckIfPositionIsABorder(num, army.WorldPosition))
			{
				this.RunForward(army, num, army.WorldPosition, lastOrientation, direction, out position2, out worldOrientation);
			}
			else
			{
				this.FollowEdges(army, num, army.WorldPosition, lastOrientation, direction, out position2, out worldOrientation);
			}
		}
		else
		{
			this.RunForwardIfPossible(army, num, army.WorldPosition, lastOrientation, direction, out position2, out worldOrientation);
			if (!position2.IsValid)
			{
				aiBehaviorTree.ErrorCode = 5;
				return State.Failure;
			}
		}
		if (position2.IsValid)
		{
			if (aiBehaviorTree.Variables.ContainsKey("LastRoamingOrientation"))
			{
				aiBehaviorTree.Variables["LastRoamingOrientation"] = worldOrientation;
			}
			else
			{
				aiBehaviorTree.Variables.Add("LastRoamingOrientation", worldOrientation);
			}
			this.AddOutputPositionToBehaviorTree(aiBehaviorTree, position2);
		}
		else
		{
			this.AddOutputPositionToBehaviorTree(aiBehaviorTree, this.GetFurthestPositionInRegion(army, region));
		}
		return State.Success;
	}

	private void ExploreBorderTile(AIBehaviorTree aiBehaviorTree, Army army, Region region, int borderTarget, int borderTileTarget, float movementPoints)
	{
		this.visitedPositions.Clear();
		WorldPosition position = this.ExploreNeighbouringTiles(army, region.Borders[borderTarget].WorldPositions[borderTileTarget], movementPoints, region.Index);
		if (position.IsValid)
		{
			this.AddOutputPositionToBehaviorTree(aiBehaviorTree, position);
		}
		else
		{
			this.AddOutputPositionToBehaviorTree(aiBehaviorTree, this.GetFurthestPositionInRegion(army, region));
		}
	}

	private WorldPosition ExploreNeighbouringTiles(Army currrentArmy, WorldPosition currentTile, float movementPoints, int regionIndex)
	{
		if (this.pathfindingService.IsTileStopableAndPassable(currentTile, currrentArmy, PathfindingFlags.IgnoreFogOfWar, null))
		{
			int distance = this.worldPositionningService.GetDistance(currrentArmy.WorldPosition, currentTile);
			if ((float)distance >= movementPoints)
			{
				return currentTile;
			}
		}
		else
		{
			this.visitedPositions.Add(currentTile);
			for (int i = 0; i < 6; i++)
			{
				WorldPosition neighbourTile = this.worldPositionningService.GetNeighbourTile(currentTile, (WorldOrientation)i, 1);
				if (!this.visitedPositions.Contains(neighbourTile))
				{
					if (regionIndex == (int)this.worldPositionningService.GetRegionIndex(neighbourTile))
					{
						WorldPosition result = this.ExploreNeighbouringTiles(currrentArmy, neighbourTile, movementPoints, regionIndex);
						if (result.IsValid)
						{
							return result;
						}
					}
				}
			}
		}
		return WorldPosition.Invalid;
	}

	private void FollowEdges(Army currentArmy, int roamingRegionIndex, WorldPosition startingPosition, WorldOrientation lastOrientation, int direction, out WorldPosition nextDestination, out WorldOrientation nextOrientation)
	{
		nextOrientation = lastOrientation.Rotate(3);
		nextOrientation = nextOrientation.Rotate(direction);
		nextDestination = this.worldPositionningService.GetNeighbourTile(startingPosition, nextOrientation, 1);
		for (int i = 0; i < 6; i++)
		{
			if (this.CheckPosition(currentArmy, roamingRegionIndex, nextDestination))
			{
				return;
			}
			nextOrientation = nextOrientation.Rotate(direction);
			nextDestination = this.worldPositionningService.GetNeighbourTile(startingPosition, nextOrientation, 1);
		}
		nextDestination = WorldPosition.Invalid;
	}

	private WorldPosition GetFurthestPositionInRegion(Army army, Region region)
	{
		int num = -1;
		int minValue = int.MinValue;
		for (int i = 0; i < region.WorldPositions.Length; i++)
		{
			if (this.worldPositionningService.IsWaterTile(region.WorldPositions[i]) == army.IsSeafaring)
			{
				if (this.pathfindingService.IsTileStopableAndPassable(region.WorldPositions[i], army, PathfindingFlags.IgnoreFogOfWar, null))
				{
					int distance = this.worldPositionningService.GetDistance(army.WorldPosition, region.WorldPositions[i]);
					if (distance > minValue)
					{
						num = i;
					}
				}
			}
		}
		if (num != -1)
		{
			return region.WorldPositions[num];
		}
		return WorldPosition.Invalid;
	}

	private void RunForward(Army currentArmy, int roamingRegionIndex, WorldPosition startingPosition, WorldOrientation lastOrientation, int direction, out WorldPosition nextDestination, out WorldOrientation nextOrientation)
	{
		nextOrientation = lastOrientation;
		nextDestination = this.worldPositionningService.GetNeighbourTile(startingPosition, nextOrientation, 1);
		for (int i = 0; i < 6; i++)
		{
			if (this.CheckPosition(currentArmy, roamingRegionIndex, nextDestination))
			{
				return;
			}
			nextOrientation = nextOrientation.Rotate(direction);
			nextDestination = this.worldPositionningService.GetNeighbourTile(startingPosition, nextOrientation, 1);
		}
		nextDestination = WorldPosition.Invalid;
	}

	[XmlAttribute]
	public string PositionVarName { get; set; }

	private void RunForwardIfPossible(Army currentArmy, int roamingRegionIndex, WorldPosition startingPosition, WorldOrientation lastOrientation, int direction, out WorldPosition nextDestination, out WorldOrientation nextOrientation)
	{
		nextOrientation = lastOrientation;
		nextDestination = this.worldPositionningService.GetNeighbourTile(startingPosition, nextOrientation, 1);
		for (int i = 0; i < 6; i++)
		{
			if (this.CheckPosition(currentArmy, roamingRegionIndex, nextDestination) && this.pathfindingService.IsTransitionPassable(startingPosition, nextDestination, currentArmy, PathfindingFlags.IgnoreFogOfWar, null))
			{
				return;
			}
			nextOrientation = nextOrientation.Rotate(direction);
			nextDestination = this.worldPositionningService.GetNeighbourTile(startingPosition, nextOrientation, 1);
		}
		nextDestination = WorldPosition.Invalid;
	}

	[XmlAttribute]
	public bool LastResort { get; set; }

	private IPathfindingService pathfindingService;

	private List<WorldPosition> visitedPositions;

	private IWorldPositionningService worldPositionningService;

	public enum DirectionEnum
	{
		Left,
		Right,
		Undefined
	}
}
