using System;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;

public class AIBehaviorTreeNode_Decorator_CanAffordArmyAction : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_CanAffordArmyAction()
	{
		this.ArmyActionReadOnlyName = ArmyAction_TameKaiju.ReadOnlyName;
		this.Inverted = false;
		this.TargetVarName = string.Empty;
	}

	[XmlAttribute]
	public string Output_ArmyActionVarName { get; set; }

	[XmlAttribute]
	public bool Inverted { get; set; }

	[XmlAttribute]
	public string ArmyActionReadOnlyName { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) != AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		ArmyAction armyAction = null;
		IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
		if (database == null || !database.TryGetValue(this.ArmyActionReadOnlyName, out armyAction))
		{
			return State.Failure;
		}
		bool flag = false;
		ArmyAction_TameUnstunnedKaiju armyAction_TameUnstunnedKaiju = armyAction as ArmyAction_TameUnstunnedKaiju;
		if (ELCPUtilities.UseELCPSymbiosisBuffs && armyAction_TameUnstunnedKaiju != null && this.TargetVarName != string.Empty)
		{
			Kaiju kaiju = (aiBehaviorTree.Variables[this.TargetVarName] as IGameEntity) as Kaiju;
			if (kaiju != null)
			{
				flag = ELCPUtilities.CanELCPTameKaiju(kaiju, armyAction_TameUnstunnedKaiju.TameCost, army.Empire);
			}
		}
		else
		{
			flag = armyAction.CanAfford(army.Empire);
		}
		if (flag)
		{
			if (aiBehaviorTree.Variables.ContainsKey(this.Output_ArmyActionVarName))
			{
				aiBehaviorTree.Variables[this.Output_ArmyActionVarName] = this.ArmyActionReadOnlyName;
			}
			else
			{
				aiBehaviorTree.Variables.Add(this.Output_ArmyActionVarName, this.ArmyActionReadOnlyName);
			}
			return State.Success;
		}
		aiBehaviorTree.ErrorCode = 38;
		return State.Failure;
	}

	[XmlAttribute]
	public string TargetVarName { get; set; }
}
