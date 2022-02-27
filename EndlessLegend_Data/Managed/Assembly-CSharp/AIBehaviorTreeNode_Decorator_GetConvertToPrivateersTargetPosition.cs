using System;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Decorator_GetConvertToPrivateersTargetPosition : AIBehaviorTreeNode_Decorator
{
	[XmlAttribute]
	public string Output_DestinationVarName { get; set; }

	public override bool Initialize(BehaviourTree behaviourTree)
	{
		IGameService service = Services.GetService<IGameService>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		return base.Initialize(behaviourTree);
	}

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army;
		AIArmyMission.AIArmyMissionErrorCode armyUnlessLocked = base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army);
		if (armyUnlessLocked != AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		if (army.Empire == null)
		{
			return State.Failure;
		}
		DepartmentOfTheInterior agency = army.Empire.GetAgency<DepartmentOfTheInterior>();
		if (agency == null || agency.Cities.Count == 0)
		{
			return State.Failure;
		}
		float num = float.MaxValue;
		City city = null;
		for (int i = 0; i < agency.Cities.Count; i++)
		{
			City city2 = agency.Cities[i];
			float num2 = (float)this.worldPositionningService.GetDistance(army.WorldPosition, city2.WorldPosition);
			if (num2 < num)
			{
				num = num2;
				city = city2;
			}
		}
		if (city == null)
		{
			return State.Failure;
		}
		this.AddOutputPositionToBehaviorTree(aiBehaviorTree, city.GetValidDistrictToTarget(army).WorldPosition);
		return State.Success;
	}

	private void AddOutputPositionToBehaviorTree(AIBehaviorTree aiBehaviorTree, WorldPosition position)
	{
		if (aiBehaviorTree.Variables.ContainsKey(this.Output_DestinationVarName))
		{
			aiBehaviorTree.Variables[this.Output_DestinationVarName] = position;
		}
		else
		{
			aiBehaviorTree.Variables.Add(this.Output_DestinationVarName, position);
		}
	}

	private IWorldPositionningService worldPositionningService;
}
