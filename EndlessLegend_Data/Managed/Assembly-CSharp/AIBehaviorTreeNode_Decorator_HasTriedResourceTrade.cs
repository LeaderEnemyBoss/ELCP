using System;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;

public class AIBehaviorTreeNode_Decorator_HasTriedResourceTrade : AIBehaviorTreeNode_Decorator
{
	protected override bool Initialize(AIBehaviorTree aiBehaviorTree)
	{
		if (aiBehaviorTree.AICommander.Empire is MajorEmpire)
		{
			AIEntity_Empire entity = aiBehaviorTree.AICommander.AIPlayer.GetEntity<AIEntity_Empire>();
			this.aILayer_Trade = entity.GetLayer<AILayer_Trade>();
		}
		return base.Initialize(aiBehaviorTree);
	}

	public override void Release()
	{
		base.Release();
		this.aILayer_Trade = null;
	}

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		if (string.IsNullOrEmpty(this.TargetVarName))
		{
			return State.Success;
		}
		Kaiju kaiju = aiBehaviorTree.Variables[this.TargetVarName] as Kaiju;
		if (kaiju == null || kaiju.IsTamed())
		{
			return State.Success;
		}
		if (!(aiBehaviorTree.AICommander.Empire is MajorEmpire))
		{
			return State.Success;
		}
		Army army;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) != AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Success;
		}
		if (this.aILayer_Trade.ArmiesThatTriedKaijuTameResourceTrading.Contains(army.GUID))
		{
			return State.Success;
		}
		this.aILayer_Trade.ArmiesThatTriedKaijuTameResourceTrading.Add(army.GUID);
		return State.Failure;
	}

	[XmlAttribute]
	public string TargetVarName { get; set; }

	private AILayer_Trade aILayer_Trade;
}
