using System;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;

public class AIBehaviorTreeNode_Decorator_IsEmbarked : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_IsEmbarked()
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
		bool sails = army.Sails;
		if (this.Inverted)
		{
			if (sails)
			{
				return State.Failure;
			}
			return State.Success;
		}
		else
		{
			if (sails)
			{
				return State.Success;
			}
			return State.Failure;
		}
	}
}
