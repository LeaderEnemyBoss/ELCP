using System;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Decorator_ArmyGroupInRange : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_ArmyGroupInRange()
	{
		this.Range = 3;
	}

	[XmlAttribute]
	public bool Inverted { get; set; }

	[XmlAttribute]
	public int Range { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) > AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		bool flag = false;
		foreach (AICommanderMission aicommanderMission in aiBehaviorTree.AICommander.Missions)
		{
			IGameEntity gameEntity = null;
			if (aicommanderMission.AIDataArmyGUID.IsValid && aicommanderMission.AIDataArmyGUID != army.GUID && this.gameEntityRepositoryService.TryGetValue(aicommanderMission.AIDataArmyGUID, out gameEntity) && gameEntity is Army)
			{
				Army army2 = gameEntity as Army;
				if (army2.GUID.IsValid && this.worldPositionningService.GetDistance(army.WorldPosition, army2.WorldPosition) > this.Range)
				{
					flag = true;
					break;
				}
			}
		}
		if (this.Inverted)
		{
			if (flag)
			{
				return State.Success;
			}
			return State.Failure;
		}
		else
		{
			if (!flag)
			{
				return State.Success;
			}
			return State.Failure;
		}
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
