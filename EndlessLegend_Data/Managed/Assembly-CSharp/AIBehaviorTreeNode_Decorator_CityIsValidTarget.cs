using System;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;

public class AIBehaviorTreeNode_Decorator_CityIsValidTarget : AIBehaviorTreeNode_Decorator
{
	[XmlAttribute]
	public bool Inverted { get; set; }

	[XmlAttribute]
	public string TargetVarName { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) > AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		if (!aiBehaviorTree.Variables.ContainsKey(this.TargetVarName))
		{
			aiBehaviorTree.LogError("city not set", new object[0]);
			return State.Failure;
		}
		City city = aiBehaviorTree.Variables[this.TargetVarName] as City;
		if (city == null || !city.GUID.IsValid || city.Empire == null)
		{
			aiBehaviorTree.ErrorCode = 8;
			return State.Failure;
		}
		bool flag = false;
		if (!(army.Empire is MajorEmpire) || army.IsPrivateers || army.HasCatspaw)
		{
			flag = true;
		}
		else if (city.Empire == army.Empire)
		{
			flag = false;
		}
		else if (this.departmentOfForeignAffairs.CanBesiegeCity(city))
		{
			flag = true;
		}
		if (this.Inverted)
		{
			if (flag)
			{
				return State.Failure;
			}
			return State.Success;
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

	protected override bool Initialize(AIBehaviorTree aiBehaviorTree)
	{
		if (aiBehaviorTree.AICommander.Empire is MajorEmpire)
		{
			this.departmentOfForeignAffairs = aiBehaviorTree.AICommander.Empire.GetAgency<DepartmentOfForeignAffairs>();
		}
		return base.Initialize(aiBehaviorTree);
	}

	public override void Release()
	{
		this.departmentOfForeignAffairs = null;
		base.Release();
	}

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;
}
