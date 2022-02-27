using System;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;

public class AIBehaviorTreeNode_Action : BehaviourTreeNode
{
	public override State Execute(BehaviourTree behaviourTree, params object[] parameters)
	{
		if (behaviourTree is AIBehaviorTree)
		{
			return this.Execute(behaviourTree as AIBehaviorTree, parameters);
		}
		return base.Execute(behaviourTree, parameters);
	}

	public AIArmyMission.AIArmyMissionErrorCode GetArmyUnlessLocked(AIBehaviorTree aiBehaviorTree, string variableName, out Army army)
	{
		army = null;
		if (!aiBehaviorTree.Variables.ContainsKey(variableName))
		{
			aiBehaviorTree.LogError(variableName + " not set", new object[0]);
			aiBehaviorTree.ErrorCode = 34;
			return AIArmyMission.AIArmyMissionErrorCode.InvalidArmy;
		}
		army = (aiBehaviorTree.Variables[variableName] as Army);
		IAIDataRepositoryAIHelper service = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		Diagnostics.Assert(service != null);
		AIData_Army aidata_Army;
		if (!service.TryGetAIData<AIData_Army>(army.GUID, out aidata_Army))
		{
			aiBehaviorTree.LogError("ArmyGUID {0} not found in AIDataRepository", new object[]
			{
				army.GUID
			});
			aiBehaviorTree.ErrorCode = 35;
			return AIArmyMission.AIArmyMissionErrorCode.MissingArmyAIData;
		}
		if (army.IsLocked || army.IsInEncounter)
		{
			aiBehaviorTree.Log(variableName + " locked or in encounter", new object[0]);
			aiBehaviorTree.ErrorCode = 17;
			return AIArmyMission.AIArmyMissionErrorCode.TargetLocked;
		}
		return AIArmyMission.AIArmyMissionErrorCode.None;
	}

	public override bool Initialize(BehaviourTree behaviourTree)
	{
		return behaviourTree is AIBehaviorTree && this.Initialize(behaviourTree as AIBehaviorTree);
	}

	protected virtual State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		return State.Success;
	}

	protected virtual bool Initialize(AIBehaviorTree aiBehaviorTree)
	{
		return true;
	}
}
