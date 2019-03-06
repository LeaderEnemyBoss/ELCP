using System;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Session;
using UnityEngine;

public class AIBehaviorTreeNode_Decorator_FindArmyToSupport : AIBehaviorTreeNode_Decorator
{
	[XmlAttribute]
	public string Output_TargetVarName { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		global::Empire empire = aiBehaviorTree.AICommander.Empire;
		if (empire == null || !(empire is MajorEmpire))
		{
			aiBehaviorTree.ErrorCode = 10;
			return State.Failure;
		}
		Army army;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) > AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		GameServer gameServer = (Services.GetService<ISessionService>().Session as global::Session).GameServer as GameServer;
		AILayer_ArmyManagement ailayer_ArmyManagement = null;
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (gameServer.AIScheduler != null && gameServer.AIScheduler.TryGetMajorEmpireAIPlayer(empire as MajorEmpire, out aiplayer_MajorEmpire))
		{
			AIEntity entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
			if (entity != null)
			{
				ailayer_ArmyManagement = entity.GetLayer<AILayer_ArmyManagement>();
			}
		}
		if (ailayer_ArmyManagement == null)
		{
			return State.Failure;
		}
		int num = int.MaxValue;
		Army value = null;
		AICommander aicommander = null;
		foreach (AICommander aicommander2 in ailayer_ArmyManagement.AICommanders)
		{
			if (aicommander2 is AICommander_WarWithObjective || aicommander2 is AICommander_Victory)
			{
				foreach (AICommanderMission aicommanderMission in aicommander2.Missions)
				{
					IGameEntity gameEntity = null;
					if (aicommanderMission.AIDataArmyGUID.IsValid && this.gameEntityRepositoryService.TryGetValue(aicommanderMission.AIDataArmyGUID, out gameEntity) && gameEntity is Army)
					{
						Army army2 = gameEntity as Army;
						int distance = this.worldPositionningService.GetDistance(army.WorldPosition, army2.WorldPosition);
						if (distance < num)
						{
							num = distance;
							value = army2;
							aicommander = aicommander2;
						}
					}
				}
			}
		}
		if ((float)num > Mathf.Max(army.GetPropertyValue(SimulationProperties.MaximumMovement) * 1.5f, 6f))
		{
			return State.Failure;
		}
		if (aicommander != null && aicommander is AICommanderWithObjective)
		{
			AICommanderWithObjective aicommanderWithObjective = aicommander as AICommanderWithObjective;
			IWorldPositionable worldPositionable = null;
			if (aicommanderWithObjective.SubObjectiveGuid.IsValid)
			{
				IGameEntity gameEntity2 = null;
				if (this.gameEntityRepositoryService.TryGetValue(aicommanderWithObjective.SubObjectiveGuid, out gameEntity2) && gameEntity2 is IWorldPositionable)
				{
					worldPositionable = (gameEntity2 as IWorldPositionable);
				}
			}
			Region region = this.worldPositionningService.GetRegion(aicommanderWithObjective.RegionIndex);
			if (region.City != null)
			{
				worldPositionable = region.City;
			}
			if (worldPositionable == null)
			{
				if (aiBehaviorTree.Variables.ContainsKey(this.Output_MainTargetVarName))
				{
					aiBehaviorTree.Variables.Remove(this.Output_MainTargetVarName);
				}
				return State.Failure;
			}
			if (aiBehaviorTree.Variables.ContainsKey(this.Output_MainTargetVarName))
			{
				aiBehaviorTree.Variables[this.Output_MainTargetVarName] = worldPositionable;
			}
			else
			{
				aiBehaviorTree.Variables.Add(this.Output_MainTargetVarName, worldPositionable);
			}
		}
		if (aiBehaviorTree.Variables.ContainsKey(this.Output_TargetVarName))
		{
			aiBehaviorTree.Variables[this.Output_TargetVarName] = value;
		}
		else
		{
			aiBehaviorTree.Variables.Add(this.Output_TargetVarName, value);
		}
		return State.Success;
	}

	public override bool Initialize(BehaviourTree behaviourTree)
	{
		IGameService service = Services.GetService<IGameService>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		return base.Initialize(behaviourTree);
	}

	[XmlAttribute]
	public string Output_MainTargetVarName { get; set; }

	private IWorldPositionningService worldPositionningService;

	private IGameEntityRepositoryService gameEntityRepositoryService;
}
