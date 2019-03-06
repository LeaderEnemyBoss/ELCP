using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Decorator_IsTargetLocked : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_IsTargetLocked()
	{
		this.Inverted = false;
	}

	[XmlAttribute]
	public bool Inverted { get; set; }

	[XmlAttribute]
	public string TargetVarName { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army targetArmy = aiBehaviorTree.Variables[this.TargetVarName] as Army;
		if (targetArmy == null)
		{
			aiBehaviorTree.ErrorCode = 10;
			return State.Failure;
		}
		IAIDataRepositoryAIHelper service = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		Diagnostics.Assert(service != null);
		AIData_Army aidata_Army;
		if (!service.TryGetAIData<AIData_Army>(targetArmy.GUID, out aidata_Army))
		{
			aiBehaviorTree.LogError("ArmyGUID {O} not found in AIDataRepository", new object[]
			{
				targetArmy.GUID
			});
			return State.Failure;
		}
		bool flag = targetArmy.IsLocked;
		if (!flag)
		{
			IGameService service2 = Services.GetService<IGameService>();
			IEncounterRepositoryService service3 = service2.Game.Services.GetService<IEncounterRepositoryService>();
			if (service3 != null)
			{
				IEnumerable<Encounter> enumerable = service3;
				if (enumerable != null)
				{
					flag = enumerable.Any((Encounter encounter) => encounter.IsGarrisonInEncounter(targetArmy.GUID, false));
				}
			}
		}
		if (this.Inverted)
		{
			if (!flag)
			{
				return State.Success;
			}
			aiBehaviorTree.ErrorCode = 17;
			return State.Failure;
		}
		else
		{
			if (flag)
			{
				return State.Success;
			}
			aiBehaviorTree.ErrorCode = 16;
			return State.Failure;
		}
	}
}
