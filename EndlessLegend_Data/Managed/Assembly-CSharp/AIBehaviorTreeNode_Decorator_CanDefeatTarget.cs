using System;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Decorator_CanDefeatTarget : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_CanDefeatTarget()
	{
		this.TargetVarName = null;
		this.Inverted = false;
		this.StrongestAttacker = false;
	}

	[XmlAttribute]
	public bool Inverted { get; set; }

	[XmlAttribute]
	public string TargetVarName { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army;
		State result;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) > AIArmyMission.AIArmyMissionErrorCode.None)
		{
			result = State.Failure;
		}
		else if (this.StrongestAttacker)
		{
			result = this.StrongestAttackerExecute(army);
		}
		else if (!aiBehaviorTree.Variables.ContainsKey(this.TargetVarName))
		{
			result = State.Failure;
		}
		else
		{
			Garrison garrison = aiBehaviorTree.Variables[this.TargetVarName] as Garrison;
			if (garrison == null)
			{
				aiBehaviorTree.ErrorCode = 10;
				result = State.Failure;
			}
			else if (garrison == army)
			{
				result = State.Failure;
			}
			else
			{
				IGameService service = Services.GetService<IGameService>();
				Diagnostics.Assert(service != null);
				Diagnostics.Assert(service.Game.Services.GetService<IWorldPositionningService>() != null);
				IIntelligenceAIHelper service2 = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
				float num = 0f;
				float num2 = 0f;
				service2.EstimateMPInBattleground(army, garrison, ref num2, ref num);
				bool flag = true;
				if (num > num2)
				{
					flag = false;
				}
				if (this.Inverted)
				{
					if (!flag)
					{
						result = State.Success;
					}
					else
					{
						aiBehaviorTree.ErrorCode = 14;
						result = State.Failure;
					}
				}
				else if (flag)
				{
					result = State.Success;
				}
				else
				{
					aiBehaviorTree.ErrorCode = 13;
					result = State.Failure;
				}
			}
		}
		return result;
	}

	private State StrongestAttackerExecute(Army army)
	{
		AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		Region region = service.Game.Services.GetService<IWorldPositionningService>().GetRegion(army.WorldPosition);
		if (region != null)
		{
			foreach (Army army2 in Intelligence.GetArmiesInRegion(region.Index).ToList<Army>())
			{
				if (army2.Empire == army.Empire && army2.GetPropertyValue(SimulationProperties.MilitaryPower) > army.GetPropertyValue(SimulationProperties.MilitaryPower))
				{
					if (!this.Inverted)
					{
						return State.Failure;
					}
					if (this.Inverted)
					{
						return State.Success;
					}
				}
			}
			if (!this.Inverted)
			{
				return State.Success;
			}
			return State.Failure;
		}
		return State.Failure;
	}

	[XmlAttribute]
	public bool StrongestAttacker { get; set; }
}
