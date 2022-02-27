using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Decorator_IsFurthestFromTarget : AIBehaviorTreeNode_Decorator
{
	[XmlAttribute]
	public string TargetVarName { get; set; }

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
		IWorldPositionable worldPositionable = null;
		if (string.IsNullOrEmpty(this.TargetVarName))
		{
			return State.Failure;
		}
		IGameEntity gameEntity = aiBehaviorTree.Variables[this.TargetVarName] as IGameEntity;
		if (!(gameEntity is IWorldPositionable))
		{
			aiBehaviorTree.LogError("${0} is not a IWorldPositionable", new object[]
			{
				this.TargetVarName
			});
			return State.Failure;
		}
		worldPositionable = (gameEntity as IWorldPositionable);
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		service.Game.Services.GetService<IGameEntityRepositoryService>();
		if (!worldPositionable.WorldPosition.IsValid)
		{
			aiBehaviorTree.LogError("${0} doesn't exists", new object[]
			{
				this.TargetVarName
			});
			aiBehaviorTree.ErrorCode = 10;
			return State.Failure;
		}
		int num = 0;
		foreach (AICommanderMission aicommanderMission in aiBehaviorTree.AICommander.Missions)
		{
			IGameEntity gameEntity2 = null;
			if (aicommanderMission.AIDataArmyGUID.IsValid && this.gameEntityRepositoryService.TryGetValue(aicommanderMission.AIDataArmyGUID, out gameEntity2) && gameEntity2 is Army)
			{
				Army army2 = gameEntity2 as Army;
				if (army != army2 && army2.GUID.IsValid)
				{
					int distance = this.worldPositionningService.GetDistance(worldPositionable.WorldPosition, army2.WorldPosition);
					if (distance > num)
					{
						num = distance;
					}
				}
			}
		}
		if (num <= this.worldPositionningService.GetDistance(army.WorldPosition, worldPositionable.WorldPosition))
		{
			return State.Success;
		}
		return State.Failure;
	}

	public override bool Initialize(BehaviourTree behaviourTree)
	{
		IGameService service = Services.GetService<IGameService>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		return base.Initialize(behaviourTree);
	}

	private IWorldPositionningService worldPositionningService;

	private IGameEntityRepositoryService gameEntityRepositoryService;
}
