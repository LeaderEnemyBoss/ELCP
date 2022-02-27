using System;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;

public class QuestBehaviourTreeNode_Decorator_DiplomaticContract : QuestBehaviourTreeNode_Decorator<EventDiplomaticContractStateChange>
{
	public QuestBehaviourTreeNode_Decorator_DiplomaticContract()
	{
		this.TargetEmpireIndex = -1;
		this.State = QuestBehaviourTreeNode_Decorator_DiplomaticContract.QuestDiplomaticContractState.Any;
	}

	[XmlAttribute]
	public string DiplomaticTermDefinitionName { get; set; }

	[XmlAttribute]
	public string TargetEmpireVarName { get; set; }

	[XmlAttribute]
	public bool IsMutual { get; set; }

	[XmlAttribute]
	public QuestBehaviourTreeNode_Decorator_DiplomaticContract.QuestDiplomaticContractState State { get; set; }

	public int TargetEmpireIndex { get; set; }

	protected override State Execute(QuestBehaviour questBehaviour, EventDiplomaticContractStateChange e, params object[] parameters)
	{
		Empire empireWhichInitiated = e.DiplomaticContract.EmpireWhichInitiated;
		Empire empireWhichReceives = e.DiplomaticContract.EmpireWhichReceives;
		if (base.CheckAgainstQuestInitiatorFilter(questBehaviour, empireWhichInitiated, base.QuestInitiatorFilter) || (this.IsMutual && base.CheckAgainstQuestInitiatorFilter(questBehaviour, empireWhichReceives, base.QuestInitiatorFilter)))
		{
			if (this.State != QuestBehaviourTreeNode_Decorator_DiplomaticContract.QuestDiplomaticContractState.Any && !e.DiplomaticContract.State.ToString().Equals(this.State.ToString()))
			{
				return Amplitude.Unity.AI.BehaviourTree.State.Running;
			}
			if (!string.IsNullOrEmpty(this.TargetEmpireVarName) || this.TargetEmpireIndex != -1)
			{
				if (this.TargetEmpireIndex == -1)
				{
					MajorEmpire majorEmpire;
					if (questBehaviour.TryGetQuestVariableValueByName<MajorEmpire>(this.TargetEmpireVarName, out majorEmpire) && ((!this.IsMutual && empireWhichReceives != majorEmpire) || (this.IsMutual && empireWhichReceives != majorEmpire && empireWhichInitiated != majorEmpire)))
					{
						return Amplitude.Unity.AI.BehaviourTree.State.Running;
					}
				}
				else if ((!this.IsMutual && empireWhichReceives.Index != this.TargetEmpireIndex) || (this.IsMutual && empireWhichReceives.Index != this.TargetEmpireIndex && empireWhichInitiated.Index != this.TargetEmpireIndex))
				{
					return Amplitude.Unity.AI.BehaviourTree.State.Running;
				}
			}
			for (int i = 0; i < e.DiplomaticContract.Terms.Count; i++)
			{
				DiplomaticTerm diplomaticTerm = e.DiplomaticContract.Terms[i];
				if (diplomaticTerm.Definition.Name == this.DiplomaticTermDefinitionName)
				{
					return Amplitude.Unity.AI.BehaviourTree.State.Success;
				}
			}
		}
		return Amplitude.Unity.AI.BehaviourTree.State.Running;
	}

	protected override bool Initialize(QuestBehaviour questBehaviour)
	{
		if (string.IsNullOrEmpty(this.DiplomaticTermDefinitionName))
		{
			return false;
		}
		MajorEmpire majorEmpire;
		if (this.TargetEmpireVarName != string.Empty && questBehaviour.TryGetQuestVariableValueByName<MajorEmpire>(this.TargetEmpireVarName, out majorEmpire))
		{
			this.TargetEmpireIndex = majorEmpire.Index;
		}
		return base.Initialize(questBehaviour);
	}

	public enum QuestDiplomaticContractState
	{
		Any,
		Negotiation,
		Proposed,
		Signed,
		Refused,
		Ignored
	}
}
