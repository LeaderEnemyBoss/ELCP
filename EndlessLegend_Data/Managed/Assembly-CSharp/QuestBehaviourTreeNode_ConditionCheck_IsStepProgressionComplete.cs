using System;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;
using UnityEngine;

public class QuestBehaviourTreeNode_ConditionCheck_IsStepProgressionComplete : QuestBehaviourTreeNode_ConditionCheck
{
	public QuestBehaviourTreeNode_ConditionCheck_IsStepProgressionComplete()
	{
		this.StepName = string.Empty;
		this.EmpireIndex = -1;
	}

	[XmlAttribute("StepName")]
	public string StepName { get; set; }

	[XmlAttribute]
	public string InterpretedValue { get; set; }

	[XmlAttribute]
	public string InterpretedVarName { get; set; }

	public override State CheckCondition(QuestBehaviour questBehaviour, GameEvent gameEvent, params object[] parameters)
	{
		if (!StaticString.IsNullOrEmpty(this.StepName))
		{
			int num = -1;
			if (!string.IsNullOrEmpty(this.InterpretedValue))
			{
				if (this.compiledExpression == null)
				{
					this.compiledExpression = Interpreter.InfixTransform(this.InterpretedValue);
				}
				SimulationObject simulationObject;
				if (this.EmpireIndex >= 0)
				{
					simulationObject = (Services.GetService<IGameService>().Game as global::Game).Empires[this.EmpireIndex].SimulationObject;
				}
				else if (gameEvent != null)
				{
					simulationObject = gameEvent.Empire.SimulationObject;
				}
				else
				{
					simulationObject = questBehaviour.Initiator.SimulationObject;
				}
				using (InterpreterContext.InterpreterSession interpreterSession = new InterpreterContext.InterpreterSession(simulationObject))
				{
					foreach (QuestVariable questVariable in questBehaviour.QuestVariables)
					{
						if (questVariable.Object is float)
						{
							interpreterSession.Context.Register(questVariable.Name, (float)Convert.ChangeType(questVariable.Object, typeof(float)));
						}
					}
					object obj = Interpreter.Execute(this.compiledExpression, interpreterSession.Context);
					if (obj is float)
					{
						num = Mathf.FloorToInt((float)Convert.ChangeType(obj, typeof(float)));
					}
					else
					{
						if (!(obj is int))
						{
							if (global::GameManager.Preferences.QuestVerboseMode)
							{
								Diagnostics.LogWarning("[Quest] IsStepProgressionComplete expression or variable '{0}' returned {1} wich is not a number (in quest {2} of empire {3}).", new object[]
								{
									string.IsNullOrEmpty(this.InterpretedValue) ? this.InterpretedVarName : this.InterpretedValue,
									obj ?? "null",
									questBehaviour.Quest.QuestDefinition.Name,
									questBehaviour.Initiator.Index
								});
							}
							return State.Failure;
						}
						num = (int)Convert.ChangeType(obj, typeof(int));
					}
					QuestVariable questVariable2 = questBehaviour.QuestVariables.FirstOrDefault((QuestVariable match) => match.Name == this.StepName);
					if (questVariable2 != null)
					{
						QuestRegisterVariable questRegisterVariable = questVariable2.Object as QuestRegisterVariable;
						if (questRegisterVariable != null)
						{
							questRegisterVariable.Value = num;
							questBehaviour.Push(new QuestInstruction_UpdateRegisterVariable(this.StepName, questRegisterVariable.Value));
						}
						if (questBehaviour.Quest.QuestDefinition.IsGlobal)
						{
							StaticString personnalStepName = this.StepName + Quest.PersonnalProgressStepSuffix;
							questVariable2 = questBehaviour.QuestVariables.FirstOrDefault((QuestVariable match) => match.Name == personnalStepName);
							if (questVariable2 != null)
							{
								questRegisterVariable = (questVariable2.Object as QuestRegisterVariable);
								if (questRegisterVariable != null)
								{
									questRegisterVariable.Value = num;
									questBehaviour.Push(new QuestInstruction_UpdateRegisterVariable(personnalStepName, questRegisterVariable.Value));
								}
							}
						}
					}
					goto IL_2DB;
				}
			}
			num = questBehaviour.Quest.GetStepProgressionValueByName(this.StepName);
			IL_2DB:
			QuestStepProgressionRange stepProgressionRangeByName = questBehaviour.Quest.GetStepProgressionRangeByName(this.StepName);
			if (stepProgressionRangeByName == null)
			{
				Diagnostics.LogError("[Quest] ConditionCheck_IsStepProgressionComplete: step '{0}' doesn't have a progression range. (in quest {1})", new object[]
				{
					this.StepName,
					questBehaviour.Quest.Name
				});
				return State.Failure;
			}
			if (stepProgressionRangeByName.EndValueVarName != string.Empty)
			{
				questBehaviour.UpdateProgressionVariables();
				stepProgressionRangeByName = questBehaviour.Quest.GetStepProgressionRangeByName(this.StepName);
			}
			if (num >= stepProgressionRangeByName.EndValue)
			{
				return State.Success;
			}
		}
		return State.Failure;
	}

	public override bool Initialize(QuestBehaviour questBehaviour)
	{
		if (!string.IsNullOrEmpty(this.InterpretedVarName))
		{
			Diagnostics.LogError("InterpretedVarName is deprecated, use InterpretedValue instead");
		}
		global::Empire empire;
		if (this.EmpireIndex == -1 && questBehaviour.TryGetQuestVariableValueByName<global::Empire>(this.EmpireVarName, out empire) && empire != null)
		{
			this.EmpireIndex = empire.Index;
		}
		return base.Initialize(questBehaviour);
	}

	[XmlAttribute("EmpireVarName")]
	public string EmpireVarName { get; set; }

	[XmlElement]
	public int EmpireIndex { get; set; }

	private object[] compiledExpression;
}
