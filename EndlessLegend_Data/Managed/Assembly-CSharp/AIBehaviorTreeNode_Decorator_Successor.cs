using System;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;

public class AIBehaviorTreeNode_Decorator_Successor : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_Successor()
	{
		this.Inverted = false;
	}

	[XmlAttribute]
	public bool Inverted { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) != AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		if (army.GetPropertyValue(SimulationProperties.Movement) < 0.001f)
		{
			aiBehaviorTree.ErrorCode = 24;
			return State.Failure;
		}
		if (this.Inverted)
		{
			return State.Failure;
		}
		return State.Success;
	}
}
