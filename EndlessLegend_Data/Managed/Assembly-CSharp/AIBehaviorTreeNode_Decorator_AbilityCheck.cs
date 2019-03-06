using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;

public class AIBehaviorTreeNode_Decorator_AbilityCheck : AIBehaviorTreeNode_Decorator
{
	[XmlAttribute]
	public bool Inverted { get; set; }

	[XmlAttribute]
	public string AbilityName { get; set; }

	protected override State Execute(AIBehaviorTree behaviorTree, params object[] parameters)
	{
		Army army;
		if (base.GetArmyUnlessLocked(behaviorTree, "$Army", out army) != AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		if (string.IsNullOrEmpty(this.AbilityName))
		{
			return State.Failure;
		}
		bool flag = true;
		using (IEnumerator<Unit> enumerator = army.Units.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (!enumerator.Current.CheckUnitAbility(this.AbilityName, -1))
				{
					flag = false;
					break;
				}
			}
		}
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
			if (!flag)
			{
				return State.Failure;
			}
			return State.Success;
		}
	}
}
