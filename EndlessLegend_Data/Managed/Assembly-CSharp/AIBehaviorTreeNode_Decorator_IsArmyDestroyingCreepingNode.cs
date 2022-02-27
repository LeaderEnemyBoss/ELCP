using System;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;

public class AIBehaviorTreeNode_Decorator_IsArmyDestroyingCreepingNode : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_IsArmyDestroyingCreepingNode()
	{
		this.Inverted = false;
	}

	[XmlAttribute]
	public bool Inverted { get; set; }

	[XmlAttribute]
	public string TargetVarName { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army = aiBehaviorTree.Variables[this.TargetVarName] as Army;
		if (army == null)
		{
			aiBehaviorTree.ErrorCode = 10;
			return State.Failure;
		}
		bool flag = army.DismantlingCreepingNodeTarget.IsValid || army.DismantlingDeviceTarget.IsValid;
		if (this.Inverted)
		{
			if (!flag)
			{
				return State.Success;
			}
			return State.Failure;
		}
		else
		{
			if (flag)
			{
				return State.Success;
			}
			return State.Failure;
		}
	}
}
