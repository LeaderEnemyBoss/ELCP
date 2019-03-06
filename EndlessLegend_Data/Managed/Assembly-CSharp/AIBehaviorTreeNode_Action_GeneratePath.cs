using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Action_GeneratePath : AIBehaviorTreeNode_Action
{
	public AIBehaviorTreeNode_Action_GeneratePath()
	{
		this.numberOfTurnForWorldPath = 5;
		this.WorldPath = null;
		this.DestinationVarName = string.Empty;
		this.Output_PathVarName = string.Empty;
		this.IgnoreArmies = false;
		this.AllowFastTravel = false;
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
		if (this.currentTicket != null)
		{
			if (!this.currentTicket.Raised)
			{
				return State.Running;
			}
			bool flag = this.currentTicket.PostOrderResponse == PostOrderResponse.PreprocessHasFailed || this.currentTicket.PostOrderResponse == PostOrderResponse.AuthenticationHasFailed;
			this.currentTicket = null;
			if (flag)
			{
				aiBehaviorTree.ErrorCode = 1;
				return State.Failure;
			}
			return State.Success;
		}
		else
		{
			Army army;
			if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) != AIArmyMission.AIArmyMissionErrorCode.None)
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
			if (this.AllowFastTravel && !(army is KaijuArmy) && aiBehaviorTree.AICommander.Empire is MajorEmpire && aiBehaviorTree.AICommander.Empire.SimulationObject.Tags.Contains("FactionTraitMimics1"))
			{
				float propertyValue = army.GetPropertyValue(SimulationProperties.MaximumNumberOfActionPoints);
				float propertyValue2 = army.GetPropertyValue(SimulationProperties.ActionPointsSpent);
				if (propertyValue > propertyValue2)
				{
					return this.MykaraExecute(aiBehaviorTree, army);
				}
				if (this.CurrentPathCollection != null)
				{
					this.CurrentPathCollection = null;
					this.WorldPath = null;
				}
			}
			if (army.GetPropertyValue(SimulationProperties.Movement) < 0.001f)
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
			bool flag2 = this.pathfindingService.IsTransitionPassable(army.WorldPosition, worldPosition, army, (PathfindingFlags)0, null);
			if (distance == 1 && flag2 && !this.pathfindingService.IsTileStopable(worldPosition, army, (PathfindingFlags)0, null))
			{
				aiBehaviorTree.ErrorCode = 4;
				return State.Failure;
			}
			PathfindingContext pathfindingContext = army.GenerateContext();
			pathfindingContext.Greedy = true;
			PathfindingResult pathfindingResult = this.pathfindingService.FindPath(pathfindingContext, army.WorldPosition, worldPosition, PathfindingManager.RequestMode.Default, null, this.currentFlags, null);
			if (pathfindingResult == null && this.IgnoreArmies)
			{
				this.currentFlags |= PathfindingFlags.IgnoreArmies;
				pathfindingResult = this.pathfindingService.FindPath(pathfindingContext, army.WorldPosition, worldPosition, PathfindingManager.RequestMode.Default, null, this.currentFlags, null);
			}
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
		if (!Services.GetService<IDownloadableContentService>().IsShared(DownloadableContent13.ReadOnlyName))
		{
			this.SafePathOpportunityMax = -1f;
		}
		if (aiBehaviorTree.AICommander.Empire is MajorEmpire)
		{
			this.departmentOfTransportation = aiBehaviorTree.AICommander.Empire.GetAgency<DepartmentOfTransportation>();
			ArmyAction armyAction;
			if (Databases.GetDatabase<ArmyAction>(false).TryGetValue("ArmyActionFastTravel", out armyAction))
			{
				this.armyAction_FastTravel = (armyAction as ArmyAction_FastTravel);
			}
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

	[XmlAttribute]
	public bool IgnoreArmies { get; set; }

	private State MykaraExecute(AIBehaviorTree aiBehaviorTree, Army army)
	{
		WorldPosition worldPosition = (WorldPosition)aiBehaviorTree.Variables[this.DestinationVarName];
		aiBehaviorTree.LastPathfindTargetPosition = worldPosition;
		if (!worldPosition.IsValid)
		{
			aiBehaviorTree.ErrorCode = 3;
			return State.Failure;
		}
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.Log("ELCP: {0}/{1} AIBehaviorTreeNode_Action_GeneratePath MykaraExecute Goal: {2}", new object[]
			{
				army.Empire,
				army.LocalizedName,
				worldPosition
			});
		}
		if (this.CurrentPathCollection != null && this.CurrentPathCollection.ExitNode != null)
		{
			IFastTravelNodeGameEntity[] entryTravelNodesFor = this.departmentOfTransportation.GetEntryTravelNodesFor(army, this.armyAction_FastTravel.EntryPrerequisites);
			if (entryTravelNodesFor.Length != 0)
			{
				if (!entryTravelNodesFor.Contains(this.CurrentPathCollection.ExitNode) && this.departmentOfTransportation.GetExitTravelNodesFor(army, this.armyAction_FastTravel.EntryPrerequisites).Contains(this.CurrentPathCollection.ExitNode))
				{
					return this.FastTravelExecute(aiBehaviorTree, army);
				}
				this.CurrentPathCollection = null;
				this.WorldPath = null;
			}
		}
		if (army.GetPropertyValue(SimulationProperties.Movement) < 0.001f)
		{
			aiBehaviorTree.ErrorCode = 24;
			return State.Failure;
		}
		if (this.CurrentPathCollection != null && (this.CurrentPathCollection.PathFromNodeExitToDestination.Destination == worldPosition || (this.WorldPath != null && this.WorldPath.Destination == worldPosition)))
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
		if (distance == 1 && flag && !this.pathfindingService.IsTileStopable(worldPosition, army, (PathfindingFlags)0, null))
		{
			aiBehaviorTree.ErrorCode = 4;
			return State.Failure;
		}
		PathfindingContext pathfindingContext = army.GenerateContext();
		pathfindingContext.Greedy = true;
		PathfindingResult pathfindingResult = this.pathfindingService.FindPath(pathfindingContext, army.WorldPosition, worldPosition, PathfindingManager.RequestMode.Default, null, this.currentFlags, null);
		this.CurrentPathCollection = new AIBehaviorTreeNode_Action_GeneratePath.MykaraPathCollection();
		this.CurrentPathCollection.NormalPath = this.WorldPath;
		this.CurrentPathCollection.PathToNodeEntrance = new WorldPath();
		this.CurrentPathCollection.PathFromNodeExitToDestination = new WorldPath();
		if (pathfindingResult != null)
		{
			this.WorldPath.Build(pathfindingResult, army.GetPropertyValue(SimulationProperties.MovementRatio), 10, false);
			this.WorldPath = this.ComputeSafePathOpportunity(army, worldPosition, this.WorldPath);
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				Diagnostics.Log("ELCP: {0}/{1}/{2} original path: {3} {4} {5}", new object[]
				{
					army.Empire,
					army.LocalizedName,
					army.WorldPosition,
					pathfindingResult.CompletPathLength,
					this.WorldPath.Length,
					this.WorldPath.ControlPoints.Length
				});
			}
		}
		bool flag2 = false;
		if (!this.WorldPath.IsValid || this.WorldPath.ControlPoints.Length > 2)
		{
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				Diagnostics.Log("ELCP: {0}/{1} trying to find alternative path to target {2}", new object[]
				{
					army.Empire,
					army.LocalizedName,
					worldPosition
				});
			}
			int num = int.MaxValue;
			int num2 = int.MaxValue;
			IFastTravelNodeGameEntity fastTravelNodeGameEntity = null;
			IFastTravelNodeGameEntity fastTravelNodeGameEntity2 = null;
			List<IFastTravelNodeGameEntity> list = this.departmentOfTransportation.GetEntryTravelNodesFor(army, this.armyAction_FastTravel.EntryPrerequisites).ToList<IFastTravelNodeGameEntity>();
			list.AddRange(this.departmentOfTransportation.GetExitTravelNodesFor(army, this.armyAction_FastTravel.EntryPrerequisites));
			foreach (IFastTravelNodeGameEntity fastTravelNodeGameEntity3 in list)
			{
				if (fastTravelNodeGameEntity3.GetTravelEntrancePositions().Length != 0)
				{
					int distance2 = this.worldPositionningService.GetDistance(fastTravelNodeGameEntity3.WorldPosition, army.WorldPosition);
					int distance3 = this.worldPositionningService.GetDistance(fastTravelNodeGameEntity3.WorldPosition, worldPosition);
					if (distance2 < num)
					{
						num = distance2;
						fastTravelNodeGameEntity = fastTravelNodeGameEntity3;
						if (distance2 == 1)
						{
							flag2 = true;
						}
					}
					if (distance3 < num2)
					{
						num2 = distance3;
						fastTravelNodeGameEntity2 = fastTravelNodeGameEntity3;
					}
				}
			}
			if (fastTravelNodeGameEntity != null && fastTravelNodeGameEntity2 != null && fastTravelNodeGameEntity != fastTravelNodeGameEntity2)
			{
				int num3 = 1;
				WorldPosition worldPosition2 = army.WorldPosition;
				if (!flag2)
				{
					worldPosition2 = this.GetValidTileForFastTravel(fastTravelNodeGameEntity.WorldPosition, army.WorldPosition, true);
				}
				if (worldPosition2.IsValid)
				{
					PathfindingResult pathfindingResult2 = this.pathfindingService.FindPath(pathfindingContext, army.WorldPosition, fastTravelNodeGameEntity.WorldPosition, PathfindingManager.RequestMode.Default, null, this.currentFlags, null);
					WorldPath worldPath = new WorldPath();
					if (pathfindingResult2 != null)
					{
						worldPath.Build(pathfindingResult2, army.GetPropertyValue(SimulationProperties.MovementRatio), 10, false);
						num3 += worldPath.ControlPoints.Length;
						if (this.GetValidTileForFastTravel(fastTravelNodeGameEntity2.WorldPosition, worldPosition, false).IsValid)
						{
							PathfindingResult pathfindingResult3 = this.pathfindingService.FindPath(pathfindingContext, worldPosition, fastTravelNodeGameEntity2.WorldPosition, PathfindingManager.RequestMode.Default, null, this.currentFlags, null);
							WorldPath worldPath2 = new WorldPath();
							if (pathfindingResult3 != null)
							{
								worldPath2.Build(pathfindingResult3, army.GetPropertyValue(SimulationProperties.MovementRatio), 10, false);
								num3 += worldPath2.ControlPoints.Length;
								if (!this.WorldPath.IsValid || num3 < this.WorldPath.ControlPoints.Length)
								{
									this.CurrentPathCollection.EntryNode = fastTravelNodeGameEntity;
									this.CurrentPathCollection.ExitNode = fastTravelNodeGameEntity2;
									this.CurrentPathCollection.PathToNodeEntrance = worldPath;
									this.CurrentPathCollection.PathFromNodeExitToDestination = worldPath2;
									if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
									{
										Diagnostics.Log("ELCP: {0}/{1} alternative path found through {2} {3}, {4} < {5}, {6}", new object[]
										{
											army.Empire,
											army.LocalizedName,
											fastTravelNodeGameEntity.WorldPosition + "/" + worldPath.Destination,
											fastTravelNodeGameEntity2.WorldPosition + "/" + worldPath2.Destination,
											num3,
											this.WorldPath.ControlPoints.Length,
											flag2
										});
									}
								}
							}
						}
					}
				}
			}
		}
		if (!this.WorldPath.IsValid && !this.CurrentPathCollection.PathToNodeEntrance.IsValid && (!flag2 || this.CurrentPathCollection.ExitNode == null))
		{
			aiBehaviorTree.ErrorCode = 3;
			return State.Failure;
		}
		WorldPath value = this.CurrentPathCollection.PathToNodeEntrance.IsValid ? this.CurrentPathCollection.PathToNodeEntrance : this.WorldPath;
		if (aiBehaviorTree.Variables.ContainsKey(this.Output_PathVarName))
		{
			aiBehaviorTree.Variables[this.Output_PathVarName] = value;
		}
		else
		{
			aiBehaviorTree.Variables.Add(this.Output_PathVarName, value);
		}
		if (flag2 && this.CurrentPathCollection.ExitNode != null && this.departmentOfTransportation.GetExitTravelNodesFor(army, this.armyAction_FastTravel.EntryPrerequisites).Contains(this.CurrentPathCollection.ExitNode))
		{
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				Diagnostics.Log("ELCP: {0}/{1} initiating instant teleport {3} {2} to {4}", new object[]
				{
					army.Empire,
					army.LocalizedName,
					this.CurrentPathCollection.EntryNode.WorldPosition,
					army.WorldPosition,
					this.CurrentPathCollection.ExitNode
				});
			}
			return this.FastTravelExecute(aiBehaviorTree, army);
		}
		return State.Success;
	}

	private WorldPosition GetValidTileForFastTravel(WorldPosition NodePosition, WorldPosition OtherPosition, bool entry = true)
	{
		WorldOrientation worldOrientation;
		if (entry)
		{
			worldOrientation = this.worldPositionningService.GetOrientation(NodePosition, OtherPosition);
		}
		else
		{
			worldOrientation = this.worldPositionningService.GetOrientation(OtherPosition, NodePosition);
		}
		for (int i = 0; i < 6; i++)
		{
			WorldPosition neighbourTile = this.worldPositionningService.GetNeighbourTile(NodePosition, worldOrientation, 1);
			if (neighbourTile.IsValid && !this.worldPositionningService.IsWaterTile(neighbourTile) && this.pathfindingService.IsTileStopable(neighbourTile, PathfindingMovementCapacity.Ground, (PathfindingFlags)0))
			{
				return neighbourTile;
			}
			if (i % 2 == 0)
			{
				worldOrientation = worldOrientation.Rotate(-(i + 1));
			}
			else
			{
				worldOrientation = worldOrientation.Rotate(i + 1);
			}
		}
		return WorldPosition.Invalid;
	}

	private State FastTravelExecute(AIBehaviorTree aiBehaviorTree, Army army)
	{
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.Assert(false, "ELCP: FastTravelExecute attempt for {0}/{1} {2} -> {3}", new object[]
			{
				army.LocalizedName,
				army.Empire,
				army.WorldPosition,
				this.CurrentPathCollection.ExitNode.WorldPosition
			});
		}
		if (this.CurrentPathCollection != null && this.CurrentPathCollection.ExitNode != null)
		{
			List<StaticString> list = new List<StaticString>();
			if (this.armyAction_FastTravel.CanExecute(army, ref list, new object[0]))
			{
				this.armyAction_FastTravel.Execute(army, aiBehaviorTree.AICommander.Empire.PlayerControllers.AI, out this.currentTicket, null, new object[]
				{
					this.CurrentPathCollection.ExitNode
				});
				if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
				{
					Diagnostics.Assert(false, "ELCP: FastTravelExecute succeded for {0}/{1} {2} -> {3}", new object[]
					{
						army.LocalizedName,
						army.Empire,
						army.WorldPosition,
						this.CurrentPathCollection.ExitNode.WorldPosition
					});
				}
				this.CurrentPathCollection = null;
				this.WorldPath = null;
				return State.Running;
			}
		}
		this.CurrentPathCollection = null;
		this.WorldPath = null;
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.LogError("ELCP: FastTravelExecute failed for {0}/{1} {2} -> {3}", new object[]
			{
				army.LocalizedName,
				army.Empire,
				army.WorldPosition,
				this.CurrentPathCollection.ExitNode.WorldPosition
			});
		}
		return State.Failure;
	}

	[XmlAttribute]
	public bool AllowFastTravel { get; set; }

	private IWorldPositionningService worldPositionningService;

	private IPathfindingService pathfindingService;

	private int numberOfTurnForWorldPath;

	private PathfindingFlags currentFlags;

	private ArmyAction_FastTravel armyAction_FastTravel;

	private Ticket currentTicket;

	private DepartmentOfTransportation departmentOfTransportation;

	private AIBehaviorTreeNode_Action_GeneratePath.MykaraPathCollection CurrentPathCollection;

	private class MykaraPathCollection
	{
		public WorldPath NormalPath;

		public WorldPath PathToNodeEntrance;

		public WorldPath PathFromNodeExitToDestination;

		public IFastTravelNodeGameEntity EntryNode;

		public IFastTravelNodeGameEntity ExitNode;
	}
}
