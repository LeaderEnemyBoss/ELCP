using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Action_GeneratePath : AIBehaviorTreeNode_Action
{
	public AIBehaviorTreeNode_Action_GeneratePath()
	{
		this.WorldPath = null;
		this.DestinationVarName = string.Empty;
		this.Output_PathVarName = string.Empty;
		this.SafePathOpportunityMax = -1f;
	}

	[XmlAttribute]
	public string DestinationVarName { get; set; }

	[XmlAttribute]
	public string Output_PathVarName { get; set; }

	public float SafePathOpportunityMax { get; set; }

	[XmlElement]
	public WorldPath WorldPath { get; set; }

	public override void Reset()
	{
		base.Reset();
		this.WorldPath = null;
	}

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army;
		AIArmyMission.AIArmyMissionErrorCode armyUnlessLocked = base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army);
		if (armyUnlessLocked != AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		if (!aiBehaviorTree.Variables.ContainsKey(this.DestinationVarName))
		{
			aiBehaviorTree.LogError("{0} not set", new object[]
			{
				this.DestinationVarName
			});
			return State.Failure;
		}
		if (army.GetPropertyValue(SimulationProperties.Movement) <= 1.401298E-45f)
		{
			aiBehaviorTree.ErrorCode = 24;
			return State.Failure;
		}
		WorldPosition worldPosition = (WorldPosition)aiBehaviorTree.Variables[this.DestinationVarName];
		aiBehaviorTree.LastPathfindTargetPosition = worldPosition;
		if (!worldPosition.IsValid)
		{
			aiBehaviorTree.ErrorCode = 3;
			return State.Failure;
		}
		if (this.WorldPath != null && this.WorldPath.Destination == worldPosition)
		{
			return State.Success;
		}
		this.currentFlags = PathfindingFlags.IgnoreFogOfWar;
		if (!aiBehaviorTree.AICommander.MayUseFrozenTiles())
		{
			this.currentFlags |= PathfindingFlags.IgnoreFrozenWaters;
		}
		this.WorldPath = new WorldPath();
		if (army.WorldPosition == worldPosition)
		{
			aiBehaviorTree.ErrorCode = 4;
			return State.Failure;
		}
		int distance = this.worldPositionningService.GetDistance(army.WorldPosition, worldPosition);
		bool flag = this.pathfindingService.IsTransitionPassable(army.WorldPosition, worldPosition, army, (PathfindingFlags)0, null);
		if (distance == 1 && flag)
		{
			bool flag2 = !this.pathfindingService.IsTileStopable(worldPosition, army, (PathfindingFlags)0, null);
			if (flag2)
			{
				aiBehaviorTree.ErrorCode = 4;
				return State.Failure;
			}
		}
		PathfindingContext pathfindingContext = army.GenerateContext();
		pathfindingContext.Greedy = true;
		PathfindingResult pathfindingResult = this.pathfindingService.FindPath(pathfindingContext, army.WorldPosition, worldPosition, PathfindingManager.RequestMode.Default, null, this.currentFlags, null);
		if (pathfindingResult == null)
		{
			aiBehaviorTree.ErrorCode = 3;
			return State.Failure;
		}
		this.WorldPath.Build(pathfindingResult, army.GetPropertyValue(SimulationProperties.MovementRatio), this.numberOfTurnForWorldPath, false);
		this.WorldPath = this.ComputeSafePathOpportunity(army, worldPosition, this.WorldPath);
		if (!this.WorldPath.IsValid)
		{
			aiBehaviorTree.ErrorCode = 3;
			return State.Failure;
		}
		if (aiBehaviorTree.Variables.ContainsKey(this.Output_PathVarName))
		{
			aiBehaviorTree.Variables[this.Output_PathVarName] = this.WorldPath;
		}
		else
		{
			aiBehaviorTree.Variables.Add(this.Output_PathVarName, this.WorldPath);
		}
		return State.Success;
	}

	protected override bool Initialize(AIBehaviorTree aiBehaviorTree)
	{
		if (this.WorldPath != null)
		{
			if (aiBehaviorTree.Variables.ContainsKey(this.Output_PathVarName))
			{
				aiBehaviorTree.Variables[this.Output_PathVarName] = this.WorldPath;
			}
			else
			{
				aiBehaviorTree.Variables.Add(this.Output_PathVarName, this.WorldPath);
			}
		}
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.pathfindingService = service.Game.Services.GetService<IPathfindingService>();
		Diagnostics.Assert(this.pathfindingService != null);
		if (this.SafePathOpportunityMax < 0f)
		{
			IPersonalityAIHelper service2 = AIScheduler.Services.GetService<IPersonalityAIHelper>();
			this.SafePathOpportunityMax = service2.GetRegistryValue<float>(aiBehaviorTree.AICommander.Empire, "AI/Behavior/GlobalSafePathOpportunity", 1.25f);
		}
		IDownloadableContentService service3 = Services.GetService<IDownloadableContentService>();
		if (!service3.IsShared(DownloadableContent13.ReadOnlyName))
		{
			this.SafePathOpportunityMax = -1f;
		}
		return base.Initialize(aiBehaviorTree);
	}

	private WorldPath ComputeSafePathOpportunity(Army army, WorldPosition destination, WorldPath unsafePath)
	{
		if (this.SafePathOpportunityMax < 1f)
		{
			return unsafePath;
		}
		bool flag = true;
		for (int i = 0; i < unsafePath.ControlPoints.Length; i++)
		{
			if (this.worldPositionningService.HasRetaliationFor(unsafePath.WorldPositions[(int)unsafePath.ControlPoints[i]], army.Empire))
			{
				flag = false;
				break;
			}
		}
		if (!flag)
		{
			PathfindingContext pathfindingContext = army.GenerateContext();
			pathfindingContext.Greedy = true;
			PathfindingResult pathfindingResult = this.pathfindingService.FindPath(pathfindingContext, army.WorldPosition, destination, PathfindingManager.RequestMode.AvoidToBeHurtByDefensiveTiles, null, this.currentFlags, null);
			if (pathfindingResult != null)
			{
				WorldPath worldPath = new WorldPath();
				worldPath.Build(pathfindingResult, army.GetPropertyValue(SimulationProperties.MovementRatio), this.numberOfTurnForWorldPath, false);
				if (worldPath.IsValid && (float)worldPath.ControlPoints.Length < this.SafePathOpportunityMax * (float)unsafePath.ControlPoints.Length)
				{
					return worldPath;
				}
			}
		}
		return unsafePath;
	}

	private IWorldPositionningService worldPositionningService;

	private IPathfindingService pathfindingService;

	private int numberOfTurnForWorldPath = 5;

	private PathfindingFlags currentFlags;
}
