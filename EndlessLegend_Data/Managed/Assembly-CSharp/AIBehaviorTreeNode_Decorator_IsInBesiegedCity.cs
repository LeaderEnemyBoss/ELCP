using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Decorator_IsInBesiegedCity : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_IsInBesiegedCity()
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
		if (aiBehaviorTree.AICommander.Empire == null)
		{
			aiBehaviorTree.ErrorCode = 10;
			return State.Failure;
		}
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(service2 != null);
		District district = service2.GetDistrict(army.WorldPosition);
		if (district != null && district.Empire.Index == army.Empire.Index && district.City.BesiegingEmpire != null)
		{
			if (this.Inverted)
			{
				return State.Failure;
			}
			City city = district.City;
			if (aiBehaviorTree.Variables.ContainsKey(this.Output_TargetVarName))
			{
				aiBehaviorTree.Variables[this.Output_TargetVarName] = city;
			}
			else
			{
				aiBehaviorTree.Variables.Add(this.Output_TargetVarName, city);
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

	[XmlAttribute]
	public string Output_TargetVarName { get; set; }
}
