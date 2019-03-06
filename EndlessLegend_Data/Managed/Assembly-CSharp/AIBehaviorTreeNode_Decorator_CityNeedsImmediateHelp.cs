using System;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;

public class AIBehaviorTreeNode_Decorator_CityNeedsImmediateHelp : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_CityNeedsImmediateHelp()
	{
		this.Inverted = false;
	}

	[XmlAttribute]
	public bool Inverted { get; set; }

	[XmlAttribute]
	public string CityUnderSiege { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) > AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		if (!aiBehaviorTree.Variables.ContainsKey(this.CityUnderSiege))
		{
			aiBehaviorTree.LogError("city not set", new object[0]);
			return State.Failure;
		}
		City city = aiBehaviorTree.Variables[this.CityUnderSiege] as City;
		if (city == null)
		{
			aiBehaviorTree.ErrorCode = 8;
			return State.Failure;
		}
		if (city.BesiegingEmpire != null)
		{
			float propertyValue = city.GetPropertyValue(SimulationProperties.CityDefensePoint);
			float besiegingPower = DepartmentOfTheInterior.GetBesiegingPower(city, true);
			if (propertyValue <= besiegingPower)
			{
				if (this.Inverted)
				{
					return State.Failure;
				}
				return State.Success;
			}
		}
		if (this.Inverted)
		{
			return State.Success;
		}
		return State.Failure;
	}
}
