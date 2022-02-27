using System;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class QuestBehaviourTreeNode_Action_UpdateVar : QuestBehaviourTreeNode_Action
{
	private QuestBehaviourTreeNode_Action_UpdateVar()
	{
		this.EmpireIndex = -1;
	}

	[XmlAttribute("EmpireVarName")]
	public string EmpireVarName { get; set; }

	[XmlElement]
	public int EmpireIndex { get; set; }

	protected override bool Initialize(QuestBehaviour questBehaviour)
	{
		Diagnostics.Log("UpdateVariable.Initialize");
		global::Game game = Services.GetService<IGameService>().Game as global::Game;
		if (game == null)
		{
			Diagnostics.LogError("Cannot retrieve game service, Action_uptateVariable");
		}
		if (this.EmpireIndex != -1)
		{
			QuestVariable questVariable = questBehaviour.QuestVariables.FirstOrDefault((QuestVariable match) => match.Name == this.EmpireVarName);
			if (questVariable == null)
			{
				questVariable = new QuestVariable(this.EmpireVarName);
				questBehaviour.QuestVariables.Add(questVariable);
			}
			questVariable.Object = game.Empires[this.EmpireIndex];
		}
		MajorEmpire majorEmpire;
		if (this.EmpireIndex == -1 && this.EmpireVarName != string.Empty && questBehaviour.TryGetQuestVariableValueByName<MajorEmpire>(this.EmpireVarName, out majorEmpire))
		{
			this.EmpireIndex = majorEmpire.Index;
		}
		return true;
	}

	protected override State Execute(QuestBehaviour questBehaviour, params object[] parameters)
	{
		Diagnostics.Log("UpdateVariable.Execute");
		if (this.EmpireIndex == -1 && this.EmpireVarName != string.Empty)
		{
			this.Initialize(questBehaviour);
		}
		if (this.EmpireIndex != -1 && this.EmpireVarName == string.Empty)
		{
			this.Initialize(questBehaviour);
		}
		return State.Success;
	}
}
