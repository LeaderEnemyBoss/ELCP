using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;

public class QuestBehaviourTreeNode_ConditionCheck_Prerequisite : QuestBehaviourTreeNode_ConditionCheck
{
	[XmlElement("Prerequisites")]
	public QuestBehaviourPrerequisites[] Prerequisites { get; set; }

	public override State CheckCondition(QuestBehaviour questBehaviour, GameEvent gameEvent, params object[] parameters)
	{
		if (this.questManagementService == null)
		{
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null && service.Game != null);
			this.questManagementService = service.Game.Services.GetService<IQuestManagementService>();
			Diagnostics.Assert(this.questManagementService != null);
		}
		if (this.CheckPrerequisites(questBehaviour, this.questManagementService.State.Targets))
		{
			return State.Success;
		}
		return State.Failure;
	}

	public override bool Initialize(QuestBehaviour questBehaviour)
	{
		if (this.Prerequisites == null || this.Prerequisites.Length == 0)
		{
			return true;
		}
		for (int i = 0; i < this.Prerequisites.Length; i++)
		{
			if (!this.Prerequisites[i].Initialize(questBehaviour))
			{
				return false;
			}
		}
		return base.Initialize(questBehaviour);
	}

	private bool CheckPrerequisites(QuestBehaviour questBehaviour, Dictionary<StaticString, IEnumerable<SimulationObjectWrapper>> targets)
	{
		if (this.Prerequisites == null || this.Prerequisites.Length == 0)
		{
			return true;
		}
		if (this.Prerequisites.Length > 1)
		{
			Diagnostics.LogWarning("TODO: FIX THIS CODE TO SUPPORT MORE THAN 1 PREREQUISITE OF TYPE QUESTBEHAVIOURPREREQUISITES. Concerned quest is '{0}'.", new object[]
			{
				questBehaviour.Quest.Name
			});
		}
		if (global::GameManager.Preferences.QuestVerboseMode)
		{
			Diagnostics.Log("ELCP: {0} quest {1} begins prereq check", new object[]
			{
				questBehaviour.Initiator,
				questBehaviour.Quest.Name
			});
		}
		int i = 0;
		while (i < this.Prerequisites.Length)
		{
			QuestBehaviourPrerequisites questBehaviourPrerequisites = this.Prerequisites[i];
			IEnumerable<SimulationObjectWrapper> enumerable = null;
			if (questBehaviourPrerequisites.Target == null)
			{
				enumerable = new SimulationObjectWrapper[1];
			}
			else
			{
				SimulationObjectWrapper targetSimulationObjectWrapper = questBehaviourPrerequisites.GetTargetSimulationObjectWrapper(questBehaviour);
				if (targetSimulationObjectWrapper != null && targetSimulationObjectWrapper.SimulationObject != null)
				{
					enumerable = new SimulationObjectWrapper[]
					{
						targetSimulationObjectWrapper
					};
				}
				else
				{
					targets.TryGetValue(questBehaviourPrerequisites.Target, out enumerable);
				}
			}
			if (enumerable != null)
			{
				bool flag = false;
				foreach (SimulationObjectWrapper simulationObjectWrapper in enumerable)
				{
					if (global::GameManager.Preferences.QuestVerboseMode)
					{
						Diagnostics.Log("ELCP: {0} simulationObjectWrapper {1}", new object[]
						{
							questBehaviour.Initiator,
							(simulationObjectWrapper is Garrison) ? (simulationObjectWrapper as Garrison).LocalizedName : simulationObjectWrapper.GetType().ToString()
						});
					}
					flag = false;
					using (InterpreterContext.InterpreterSession interpreterSession = new InterpreterContext.InterpreterSession(simulationObjectWrapper))
					{
						foreach (QuestVariable questVariable in questBehaviour.QuestVariables)
						{
							if (questVariable.Object is float)
							{
								interpreterSession.Context.Register(questVariable.Name, questVariable.Object);
							}
						}
						int j = 0;
						while (j < questBehaviourPrerequisites.Prerequisites.Length)
						{
							if (!questBehaviourPrerequisites.Prerequisites[j].Check(interpreterSession.Context))
							{
								if (global::GameManager.Preferences.QuestVerboseMode)
								{
									Diagnostics.Log("ELCP: context check failed for {0} {1}", new object[]
									{
										j,
										questBehaviourPrerequisites.ToString()
									});
								}
								if (questBehaviourPrerequisites.AnyTarget)
								{
									flag = true;
									break;
								}
								return false;
							}
							else
							{
								j++;
							}
						}
					}
					if (!flag && questBehaviourPrerequisites.AnyTarget)
					{
						return true;
					}
				}
				if (flag)
				{
					return false;
				}
				i++;
				continue;
			}
			if (global::GameManager.Preferences.QuestVerboseMode)
			{
				Diagnostics.LogWarning("[Quest] Could not find prerequisite target {0} in quest {1}", new object[]
				{
					questBehaviourPrerequisites.Target,
					questBehaviour.Quest.Name
				});
			}
			return false;
		}
		return true;
	}

	public override object Clone()
	{
		QuestBehaviourTreeNode_ConditionCheck_Prerequisite questBehaviourTreeNode_ConditionCheck_Prerequisite = (QuestBehaviourTreeNode_ConditionCheck_Prerequisite)base.MemberwiseClone();
		if (this.Prerequisites != null)
		{
			questBehaviourTreeNode_ConditionCheck_Prerequisite.Prerequisites = new QuestBehaviourPrerequisites[this.Prerequisites.Length];
			for (int i = 0; i < this.Prerequisites.Length; i++)
			{
				questBehaviourTreeNode_ConditionCheck_Prerequisite.Prerequisites[i] = (QuestBehaviourPrerequisites)this.Prerequisites[i].Clone();
			}
		}
		return questBehaviourTreeNode_ConditionCheck_Prerequisite;
	}

	private IQuestManagementService questManagementService;
}
