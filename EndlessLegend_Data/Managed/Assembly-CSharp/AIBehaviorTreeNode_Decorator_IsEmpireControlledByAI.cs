using System;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Decorator_IsEmpireControlledByAI : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_IsEmpireControlledByAI()
	{
		this.Inverted = false;
	}

	[XmlAttribute]
	public bool Inverted { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		IGameService service = Services.GetService<IGameService>();
		global::Game game = service.Game as global::Game;
		if (this.Inverted)
		{
			if (!aiBehaviorTree.AICommander.Empire.IsControlledByAI)
			{
				return State.Success;
			}
			aiBehaviorTree.ErrorCode = 39;
			return State.Failure;
		}
		else
		{
			if (aiBehaviorTree.AICommander.Empire.IsControlledByAI)
			{
				return State.Success;
			}
			aiBehaviorTree.ErrorCode = 40;
			return State.Failure;
		}
	}
}
