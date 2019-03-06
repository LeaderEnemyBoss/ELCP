using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Action_Move : AIBehaviorTreeNode_Action
{
	public AIBehaviorTreeNode_Action_Move()
	{
		this.TypeOfMove = "Regular";
	}

	[XmlAttribute]
	public string PathVarName { get; set; }

	[XmlAttribute]
	public string TypeOfMove { get; set; }

	public override void Release()
	{
		base.Release();
		this.game = null;
		this.currentTicket = null;
		this.worldPositionningService = null;
		this.pathfindingService = null;
		this.visibilityService = null;
		this.armyAction_TeleportInRange = null;
	}

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) != AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
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
				aiBehaviorTree.ErrorCode = 29;
				aiBehaviorTree.LogWarning("OrderGotoFail", new object[0]);
				return State.Failure;
			}
			if (army.GetPropertyValue(SimulationProperties.Movement) > 1.401298E-45f)
			{
				aiBehaviorTree.ErrorCode = 28;
				return State.Running;
			}
			return State.Success;
		}
		else
		{
			if (army.IsMoving)
			{
				return State.Running;
			}
			if (!aiBehaviorTree.Variables.ContainsKey(this.PathVarName))
			{
				aiBehaviorTree.LogError("{0} not set", new object[]
				{
					this.PathVarName
				});
				return State.Failure;
			}
			WorldPath worldPath = aiBehaviorTree.Variables[this.PathVarName] as WorldPath;
			if (worldPath == null || worldPath.Length < 2)
			{
				aiBehaviorTree.LogError("Path is null.", new object[0]);
				aiBehaviorTree.ErrorCode = 3;
				return State.Failure;
			}
			if (!worldPath.IsValid)
			{
				aiBehaviorTree.ErrorCode = 3;
				return State.Failure;
			}
			if (this.TypeOfMove.Equals("AvoidEnemy") && this.IsEnemyDetectedOnPath(army))
			{
				aiBehaviorTree.ErrorCode = 23;
				return State.Failure;
			}
			int num = -1;
			for (int i = 0; i < worldPath.WorldPositions.Length; i++)
			{
				if (worldPath.WorldPositions[i] == army.WorldPosition)
				{
					num = i;
					break;
				}
			}
			if (num == worldPath.Length - 1)
			{
				return State.Success;
			}
			if (num == -1)
			{
				return State.Failure;
			}
			if (this.ArmyCanTeleport == AIBehaviorTreeNode_Action_Move.TeleportState.NotChecked)
			{
				this.ArmyCanTeleport = AIBehaviorTreeNode_Action_Move.TeleportState.HasTeleport;
				if (!army.IsSolitary)
				{
					this.ArmyCanTeleport = AIBehaviorTreeNode_Action_Move.TeleportState.NoTeleport;
				}
				else
				{
					using (IEnumerator<Unit> enumerator = army.Units.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							if (!enumerator.Current.CheckUnitAbility("UnitAbilityTeleportInRange", -1))
							{
								this.ArmyCanTeleport = AIBehaviorTreeNode_Action_Move.TeleportState.NoTeleport;
								break;
							}
						}
					}
				}
			}
			if (this.ArmyCanTeleport == AIBehaviorTreeNode_Action_Move.TeleportState.HasTeleport && Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				Diagnostics.Log("ELCP {0} Army {1} has teleport {2} {3} {4} {5}", new object[]
				{
					army.Empire,
					army.LocalizedName,
					worldPath.Destination,
					worldPath.Length,
					worldPath.ControlPoints.Length,
					num
				});
			}
			if (this.ArmyCanTeleport == AIBehaviorTreeNode_Action_Move.TeleportState.HasTeleport && worldPath.ControlPoints.Length != 0 && worldPath.ControlPoints.Any((ushort cp) => cp > (ushort)num) && this.TryTeleportInRange(aiBehaviorTree, army, worldPath, num))
			{
				return State.Running;
			}
			WorldPosition worldPosition = WorldPosition.Invalid;
			int num3 = -1;
			int num2 = 0;
			int k;
			int j;
			for (j = num + 1; j < worldPath.WorldPositions.Length; j = k + 1)
			{
				if (this.pathfindingService.IsTilePassable(worldPath.WorldPositions[j], army, (PathfindingFlags)0, null) && this.pathfindingService.IsTileStopable(worldPath.WorldPositions[j], army, (PathfindingFlags)0, null))
				{
					worldPosition = worldPath.WorldPositions[j];
					num2++;
					if (num2 > 1)
					{
						break;
					}
				}
				else if (worldPath.ControlPoints.Length != 0 && Array.Exists<ushort>(worldPath.ControlPoints, (ushort p) => (int)p == j))
				{
					num3 = j;
				}
				k = j;
			}
			if (num3 >= 0)
			{
				List<WorldPosition> neighbours = worldPath.WorldPositions[num3].GetNeighbours(this.game.World.WorldParameters);
				WorldPosition worldPosition2 = (num3 > 0) ? worldPath.WorldPositions[num3 - 1] : army.WorldPosition;
				List<WorldPosition> neighbours2 = worldPosition2.GetNeighbours(this.game.World.WorldParameters);
				PathfindingFlags flags = PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreEncounterAreas | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnoreZoneOfControl | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreDistrict;
				if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
				{
					Diagnostics.Log("ELCP {0}/{1} AIBehaviorTreeNode_Action_Move posses: {2}/{3}", new object[]
					{
						army.Empire,
						army.LocalizedName,
						worldPosition2,
						worldPath.WorldPositions[num3]
					});
				}
				foreach (WorldPosition worldPosition3 in neighbours)
				{
					if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
					{
						Diagnostics.Log("ELCP {0}/{1} AIBehaviorTreeNode_Action_Move checking neighbor {2} {3} {4} {5} {6}", new object[]
						{
							army.Empire,
							army.LocalizedName,
							worldPosition3,
							neighbours2.Contains(worldPosition3),
							this.pathfindingService.IsTileStopable(worldPosition3, army, (PathfindingFlags)0, null),
							this.pathfindingService.IsTransitionPassable(worldPosition2, worldPosition3, army, flags, null),
							this.pathfindingService.IsTransitionPassable(worldPosition3, worldPath.WorldPositions[num3], army, flags, null)
						});
					}
					if (neighbours2.Contains(worldPosition3) && this.pathfindingService.IsTileStopable(worldPosition3, army, (PathfindingFlags)0, null) && this.pathfindingService.IsTransitionPassable(worldPosition2, worldPosition3, army, flags, null) && this.pathfindingService.IsTransitionPassable(worldPosition3, worldPath.WorldPositions[num3], army, flags, null))
					{
						if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
						{
							Diagnostics.Log("ELCP {0}/{1} AIBehaviorTreeNode_Action_Move {2} is a suitable alternative goal", new object[]
							{
								army.Empire,
								army.LocalizedName,
								worldPosition3
							});
						}
						worldPosition = worldPosition3;
						break;
					}
				}
			}
			if (worldPosition == WorldPosition.Invalid)
			{
				aiBehaviorTree.ErrorCode = 3;
				return State.Failure;
			}
			OrderGoTo orderGoTo = new OrderGoTo(army.Empire.Index, army.GUID, worldPosition);
			orderGoTo.Flags = PathfindingFlags.IgnoreFogOfWar;
			aiBehaviorTree.AICommander.Empire.PlayerControllers.AI.PostOrder(orderGoTo, out this.currentTicket, null);
			return State.Running;
		}
	}

	private bool IsEnemyDetectedOnPath(Army army)
	{
		for (int i = 0; i < this.game.Empires.Length; i++)
		{
			DepartmentOfDefense agency = this.game.Empires[i].GetAgency<DepartmentOfDefense>();
			if (agency != null && army.Empire != this.game.Empires[i])
			{
				for (int j = 0; j < agency.Armies.Count; j++)
				{
					Army army2 = agency.Armies[j];
					if (this.worldPositionningService.GetDistance(army2.WorldPosition, army.WorldPosition) <= army.LineOfSightVisionRange)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private bool TryTeleportInRange(AIBehaviorTree aiBehaviorTree, Army army, WorldPath path, int CurrentIndex)
	{
		List<StaticString> list = new List<StaticString>();
		if (!this.armyAction_TeleportInRange.CanExecute(army, ref list, new object[0]))
		{
			return false;
		}
		float teleportationRange = this.armyAction_TeleportInRange.GetTeleportationRange(army);
		PathfindingContext pathfindingContext = army.GenerateContext();
		WorldPosition worldPosition = WorldPosition.Invalid;
		int i = path.WorldPositions.Length - 1;
		while (i >= CurrentIndex)
		{
			WorldPosition worldPosition2 = path.WorldPositions[i];
			int distance = this.worldPositionningService.GetDistance(worldPosition2, army.WorldPosition);
			if (distance < 6)
			{
				return false;
			}
			if (distance <= (int)teleportationRange && this.armyAction_TeleportInRange.CanTeleportTo(army, worldPosition2, this.pathfindingService, pathfindingContext, this.visibilityService, this.worldPositionningService))
			{
				worldPosition = worldPosition2;
				if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
				{
					Diagnostics.Log("executing from {0} to {1}, {2}", new object[]
					{
						army.WorldPosition,
						worldPosition,
						distance
					});
					break;
				}
				break;
			}
			else
			{
				i--;
			}
		}
		this.armyAction_TeleportInRange.Execute(army, aiBehaviorTree.AICommander.Empire.PlayerControllers.AI, out this.currentTicket, null, new object[]
		{
			worldPosition
		});
		return true;
	}

	protected override bool Initialize(AIBehaviorTree aiBehaviorTree)
	{
		IGameService service = Services.GetService<IGameService>();
		this.game = (service.Game as global::Game);
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		this.pathfindingService = service.Game.Services.GetService<IPathfindingService>();
		this.visibilityService = service.Game.Services.GetService<IVisibilityService>();
		ArmyAction armyAction;
		if (aiBehaviorTree.AICommander.Empire is MajorEmpire && Databases.GetDatabase<ArmyAction>(false).TryGetValue("ArmyActionTeleportInRange", out armyAction))
		{
			this.armyAction_TeleportInRange = (armyAction as ArmyAction_TeleportInRange);
		}
		this.ArmyCanTeleport = AIBehaviorTreeNode_Action_Move.TeleportState.NotChecked;
		return base.Initialize(aiBehaviorTree);
	}

	private Ticket currentTicket;

	private IWorldPositionningService worldPositionningService;

	private global::Game game;

	private IPathfindingService pathfindingService;

	private IVisibilityService visibilityService;

	private ArmyAction_TeleportInRange armyAction_TeleportInRange;

	private AIBehaviorTreeNode_Action_Move.TeleportState ArmyCanTeleport;

	private enum TeleportState
	{
		NotChecked,
		NoTeleport,
		HasTeleport
	}
}
