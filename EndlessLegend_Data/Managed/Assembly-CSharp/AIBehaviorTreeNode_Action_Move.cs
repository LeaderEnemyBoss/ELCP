using System;
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
		this.currentTicket = null;
	}

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army;
		AIArmyMission.AIArmyMissionErrorCode armyUnlessLocked = base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army);
		if (armyUnlessLocked != AIArmyMission.AIArmyMissionErrorCode.None)
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
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			IPathfindingService service2 = service.Game.Services.GetService<IPathfindingService>();
			Diagnostics.Assert(service2 != null);
			WorldPosition worldPosition = WorldPosition.Invalid;
			int num2 = 0;
			for (int j = num + 1; j < worldPath.WorldPositions.Length; j++)
			{
				if (service2.IsTilePassable(worldPath.WorldPositions[j], army, (PathfindingFlags)0, null) && service2.IsTileStopable(worldPath.WorldPositions[j], army, (PathfindingFlags)0, null))
				{
					worldPosition = worldPath.WorldPositions[j];
					num2++;
					if (num2 > 1)
					{
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
			orderGoTo.Flags = (PathfindingFlags)0;
			aiBehaviorTree.AICommander.Empire.PlayerControllers.AI.PostOrder(orderGoTo, out this.currentTicket, null);
			return State.Running;
		}
	}

	private bool IsEnemyDetectedOnPath(Army army)
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		global::Game game = service.Game as global::Game;
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(service2 != null);
		for (int i = 0; i < game.Empires.Length; i++)
		{
			DepartmentOfDefense agency = game.Empires[i].GetAgency<DepartmentOfDefense>();
			if (agency != null && army.Empire != game.Empires[i])
			{
				for (int j = 0; j < agency.Armies.Count; j++)
				{
					Army army2 = agency.Armies[j];
					if (service2.GetDistance(army2.WorldPosition, army.WorldPosition) <= army.LineOfSightVisionRange)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private Ticket currentTicket;
}
