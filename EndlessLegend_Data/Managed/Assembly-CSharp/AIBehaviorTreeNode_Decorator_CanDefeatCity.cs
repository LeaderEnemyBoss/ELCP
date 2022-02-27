using System;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;

public class AIBehaviorTreeNode_Decorator_CanDefeatCity : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_CanDefeatCity()
	{
		this.TargetVarName = null;
		this.Inverted = false;
	}

	[XmlAttribute]
	public bool Inverted { get; set; }

	[XmlAttribute]
	public string TargetVarName { get; set; }

	public override void Release()
	{
		base.Release();
		this.intelligenceAIHelper = null;
	}

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army;
		AIArmyMission.AIArmyMissionErrorCode armyUnlessLocked = base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army);
		if (armyUnlessLocked != AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		if (!aiBehaviorTree.Variables.ContainsKey(this.TargetVarName))
		{
			return State.Failure;
		}
		City city = (City)aiBehaviorTree.Variables[this.TargetVarName];
		if (city == null)
		{
			aiBehaviorTree.ErrorCode = 10;
			return State.Failure;
		}
		bool flag = false;
		if (city.BesiegingEmpire == army.Empire || city.BesiegingEmpire == null)
		{
			float num = 0f;
			float num2 = 0f;
			this.intelligenceAIHelper.EstimateMPInBattleground(army, city, ref num, ref num2);
			if (num >= num2 * 1.2f)
			{
				flag = true;
			}
		}
		if (this.Inverted)
		{
			if (!flag)
			{
				return State.Success;
			}
			aiBehaviorTree.ErrorCode = 14;
			return State.Failure;
		}
		else
		{
			if (flag)
			{
				return State.Success;
			}
			aiBehaviorTree.ErrorCode = 13;
			return State.Failure;
		}
	}

	protected override bool Initialize(AIBehaviorTree questBehaviour)
	{
		this.intelligenceAIHelper = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		return base.Initialize(questBehaviour);
	}

	private IIntelligenceAIHelper intelligenceAIHelper;
}
