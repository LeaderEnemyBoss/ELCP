using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Decorator_CanDefeatTarget : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_CanDefeatTarget()
	{
		this.TargetVarName = null;
		this.Inverted = false;
	}

	[XmlAttribute]
	public bool Inverted { get; set; }

	[XmlAttribute]
	public string TargetVarName { get; set; }

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
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(service2 != null);
		IIntelligenceAIHelper service3 = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		float num = 0f;
		float num2 = 0f;
		service3.EstimateMPInBattleground(army, garrison, ref num2, ref num);
		bool flag = true;
		if (num > num2)
		{
			flag = false;
		}
		if (this.Inverted)
		{
			if (!flag)
			{
				return State.Success;
			}
			aiBehaviorTree.ErrorCode = 14;
			return State.Failure;
		}
		else
		{
			if (flag)
			{
				return State.Success;
			}
			aiBehaviorTree.ErrorCode = 13;
			return State.Failure;
		}
	}
}
