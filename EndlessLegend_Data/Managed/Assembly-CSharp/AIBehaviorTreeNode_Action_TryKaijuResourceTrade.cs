using System;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;

public class AIBehaviorTreeNode_Action_TryKaijuResourceTrade : AIBehaviorTreeNode_Action
{
	[XmlAttribute]
	public string TargetVarName { get; set; }

	protected override bool Initialize(AIBehaviorTree aiBehaviorTree)
	{
		if (aiBehaviorTree.AICommander.Empire is MajorEmpire)
		{
			AIEntity_Empire entity = aiBehaviorTree.AICommander.AIPlayer.GetEntity<AIEntity_Empire>();
			this.aILayer_Trade = entity.GetLayer<AILayer_Trade>();
		}
		ArmyAction armyAction;
		Databases.GetDatabase<ArmyAction>(false).TryGetValue("ArmyActionTameUnstunnedKaiju", out armyAction);
		this.armyAction_TameUnstunnedKaiju = (armyAction as ArmyAction_TameUnstunnedKaiju);
		return base.Initialize(aiBehaviorTree);
	}

	public override void Release()
	{
		base.Release();
		this.aILayer_Trade = null;
		this.armyAction_TameUnstunnedKaiju = null;
	}

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		if (this.executing)
		{
			this.executing = false;
			return State.Success;
		}
		if (!(aiBehaviorTree.AICommander.Empire is MajorEmpire))
		{
			return State.Failure;
		}
		Army army;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) != AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		if (string.IsNullOrEmpty(this.TargetVarName))
		{
			return State.Failure;
		}
		string luxuryName = string.Empty;
		float value = this.armyAction_TameUnstunnedKaiju.TameCost.GetValue(army.Empire.SimulationObject);
		if (ELCPUtilities.UseELCPSymbiosisBuffs)
		{
			Kaiju kaiju = aiBehaviorTree.Variables[this.TargetVarName] as Kaiju;
			if (kaiju == null)
			{
				return State.Failure;
			}
			luxuryName = kaiju.KaijuEmpire.GetAgency<KaijuCouncil>().ELCPResourceName;
		}
		else
		{
			luxuryName = this.armyAction_TameUnstunnedKaiju.TameCost.ResourceName;
		}
		this.aILayer_Trade.ProcessKaijuTameResourceTradingOrder(luxuryName, value);
		this.executing = true;
		return State.Running;
	}

	private AILayer_Trade aILayer_Trade;

	private ArmyAction_TameUnstunnedKaiju armyAction_TameUnstunnedKaiju;

	private bool executing;
}
