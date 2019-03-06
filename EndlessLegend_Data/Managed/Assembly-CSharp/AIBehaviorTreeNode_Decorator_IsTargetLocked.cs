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
		Army army = aiBehaviorTree.Variables[this.TargetVarName] as Army;
		Kaiju kaiju = aiBehaviorTree.Variables[this.TargetVarName] as Kaiju;
		if (army == null && kaiju != null && kaiju.OnArmyMode())
		{
			army = kaiju.KaijuArmy;
		}
		if (army != null)
		{
			return this.ArmyExecute(army, aiBehaviorTree, parameters);
		}
		Garrison garrison = aiBehaviorTree.Variables[this.TargetVarName] as Garrison;
		if (garrison == null && kaiju != null && kaiju.OnGarrisonMode())
		{
			garrison = kaiju.KaijuGarrison;
		}
		if (garrison != null)
		{
			return this.GeneralExecute(garrison, aiBehaviorTree, parameters);
		}
		aiBehaviorTree.ErrorCode = 10;
		return State.Failure;
	}

	private State ArmyExecute(Army targetArmy, AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
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
			IEncounterRepositoryService service2 = Services.GetService<IGameService>().Game.Services.GetService<IEncounterRepositoryService>();
			if (service2 != null)
			{
				IEnumerable<Encounter> enumerable = service2;
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

	private State GeneralExecute(Garrison targetgarr, AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		bool flag = false;
		IEncounterRepositoryService service = Services.GetService<IGameService>().Game.Services.GetService<IEncounterRepositoryService>();
		if (service != null)
		{
			IEnumerable<Encounter> enumerable = service;
			if (enumerable != null)
			{
				flag = enumerable.Any((Encounter encounter) => encounter.IsGarrisonInEncounter(targetgarr.GUID, false));
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
