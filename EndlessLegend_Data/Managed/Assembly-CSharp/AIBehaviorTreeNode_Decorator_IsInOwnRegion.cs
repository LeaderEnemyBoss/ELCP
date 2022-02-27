using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Decorator_IsInOwnRegion : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_IsInOwnRegion()
	{
		this.Inverted = false;
	}

	[XmlAttribute]
	public bool Inverted { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) > AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		global::Empire empire = aiBehaviorTree.AICommander.Empire;
		if (empire == null)
		{
			aiBehaviorTree.ErrorCode = 10;
			return State.Failure;
		}
		if (!(empire is MajorEmpire))
		{
			return State.Failure;
		}
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(service2 != null);
		Region region = service2.GetRegion(army.WorldPosition);
		if (region == null)
		{
			return State.Failure;
		}
		bool flag = false;
		if (region != null && region.Owner == aiBehaviorTree.AICommander.Empire && !region.IsOcean)
		{
			flag = true;
		}
		if (flag)
		{
			if (this.Inverted)
			{
				return State.Failure;
			}
			return State.Success;
		}
		else
		{
			if (this.Inverted)
			{
				return State.Success;
			}
			return State.Failure;
		}
	}
}
