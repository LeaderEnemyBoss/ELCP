using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation;

public abstract class QuestBehaviourTreeNode_Decorator<T> : BehaviourTreeNode_Decorator where T : Event
{
	public QuestBehaviourTreeNode_Decorator()
	{
		this.QuestInitiatorFilter = QuestInitiatorFilter.AllEmpires;
		this.ProgressionIncrement = 1;
		this.PrerequisiteNotVerifiedMessage = string.Empty;
	}

	[XmlAttribute("Initiator")]
	public QuestInitiatorFilter QuestInitiatorFilter { get; set; }

	[XmlArray("InterpretedVarsToUpdate")]
	[XmlArrayItem(Type = typeof(string), ElementName = "Var")]
	public string[] InterpretedVarsToUpdate { get; set; }

	[XmlElement(Type = typeof(QuestBehaviourTreeNode_ConditionCheck_FactionAssimilated), ElementName = "ConditionCheck_FactionAssimilated")]
	[XmlElement(Type = typeof(QuestBehaviourTreeNode_ConditionCheck_TimerEnded), ElementName = "ConditionCheck_TimerEnded")]
	[XmlElement(Type = typeof(QuestBehaviourTreeNode_ConditionCheck_VillagesConverted), ElementName = "ConditionCheck_VillagesConverted")]
	[XmlElement(Type = typeof(QuestBehaviourTreeNode_ConditionCheck_IsArmyOnWeatherTile), ElementName = "ConditionCheck_IsArmyOnWeatherTile")]
	[XmlElement(Type = typeof(QuestBehaviourTreeNode_ConditionCheck_IsStepProgressionComplete), ElementName = "ConditionCheck_IsStepProgressionComplete")]
	[XmlElement(Type = typeof(QuestBehaviourTreeNode_ConditionCheck_IsStepOnState), ElementName = "ConditionCheck_IsStepOnState")]
	[XmlElement(Type = typeof(QuestBehaviourTreeNode_ConditionCheck_IsArmyAtCity), ElementName = "ConditionCheck_IsArmyAtCity")]
	[XmlElement(Type = typeof(QuestBehaviourTreeNode_ConditionCheck_RegionContainsEnemy), ElementName = "ConditionCheck_RegionContainsEnemy")]
	[XmlElement(Type = typeof(QuestBehaviourTreeNode_ConditionCheck_RegionContainsArmy), ElementName = "ConditionCheck_RegionContainsArmy")]
	[XmlElement(Type = typeof(QuestBehaviourTreeNode_ConditionCheck_DistanceArmyToLocation), ElementName = "ConditionCheck_DistanceArmyToLocation")]
	[XmlElement(Type = typeof(QuestBehaviourTreeNode_ConditionCheck_RegionIsColonized), ElementName = "ConditionCheck_RegionIsColonized")]
	[XmlElement(Type = typeof(QuestBehaviourTreeNode_ConditionCheck_RegionIsOwnedByEmpire), ElementName = "ConditionCheck_RegionIsOwnedByEmpire")]
	[XmlElement(Type = typeof(QuestBehaviourTreeNode_ConditionCheck_VillagesPacified), ElementName = "ConditionCheck_VillagesPacified")]
	[XmlElement(Type = typeof(QuestBehaviourTreeNode_ConditionCheck_TilesTerraformed), ElementName = "ConditionCheck_TilesTerraformed")]
	[XmlElement(Type = typeof(QuestBehaviourTreeNode_ConditionCheck_Tutorial_IsGuiElementVisible), ElementName = "ConditionCheck_IsGuiElementVisible")]
	[XmlElement(Type = typeof(QuestBehaviourTreeNode_ConditionCheck_IsEmpireBlackspottedBy), ElementName = "ConditionCheck_IsEmpireBlackspottedBy")]
	[XmlElement(Type = typeof(QuestBehaviourTreeNode_ConditionCheck_Infiltrate), ElementName = "ConditionCheck_Infiltrate")]
	[XmlElement(Type = typeof(QuestBehaviourTreeNode_ConditionCheck_IsFortressOwnedByEmpire), ElementName = "ConditionCheck_IsFortressOwnedByEmpire")]
	[XmlElement(Type = typeof(QuestBehaviourTreeNode_ConditionCheck_IsArmyAlive), ElementName = "ConditionCheck_IsArmyAlive")]
	[XmlElement(Type = typeof(QuestBehaviourTreeNode_ConditionCheck_HasResourceAmount), ElementName = "ConditionCheck_HasResourceAmount")]
	[XmlElement(Type = typeof(QuestBehaviourTreeNode_ConditionCheck_Prerequisite), ElementName = "ConditionCheck_Prerequisite")]
	[XmlElement(Type = typeof(QuestBehaviourTreeNode_ConditionCheck_AllRegionRuinsSearchedByEmpire), ElementName = "ConditionCheck_AllRegionRuinsSearchedByEmpire")]
	[XmlElement(Type = typeof(QuestBehaviourTreeNode_ConditionCheck_HasEmpirePlan), ElementName = "ConditionCheck_HasEmpirePlan")]
	public QuestBehaviourTreeNode_ConditionCheck[] ConditionChecks { get; set; }

	[XmlIgnore]
	public StaticString LinkedStepProgression { get; private set; }

	[XmlIgnore]
	public StaticString PersonnalLinkedStepProgression { get; private set; }

	[XmlAttribute]
	public string PrerequisiteVerifiedMessage { get; private set; }

	[XmlAttribute("PrerequisiteNotVerifiedMessage")]
	public string PrerequisiteNotVerifiedMessage { get; set; }

	[XmlAttribute("LinkedStepProgression")]
	public string XmlSerializableLinkedStepProgression
	{
		get
		{
			return this.LinkedStepProgression;
		}
		private set
		{
			this.LinkedStepProgression = value;
			this.PersonnalLinkedStepProgression = this.LinkedStepProgression + Quest.PersonnalProgressStepSuffix;
		}
	}

	[XmlIgnore]
	protected int ProgressionIncrement { get; set; }

	public bool CheckAgainstQuestInitiatorFilter(QuestBehaviour questBehavior, GameEvent e, QuestInitiatorFilter questInitiatorFilter)
	{
		Diagnostics.Assert(questBehavior.Initiator is MajorEmpire);
		MajorEmpire majorEmpire = e.Empire as MajorEmpire;
		MajorEmpire majorEmpire2 = questBehavior.Initiator as MajorEmpire;
		switch (questInitiatorFilter)
		{
		case QuestInitiatorFilter.AllEmpires:
			return true;
		case QuestInitiatorFilter.Allies:
		{
			if (majorEmpire == null)
			{
				return false;
			}
			bool bits = majorEmpire.Bits != 0;
			int bits2 = majorEmpire2.Bits;
			if (((bits ? 1 : 0) & bits2) != 0)
			{
				return true;
			}
			break;
		}
		case QuestInitiatorFilter.Empire:
			if (e.Empire.Index == questBehavior.Initiator.Index)
			{
				return true;
			}
			break;
		case QuestInitiatorFilter.Enemies:
			if (e.Empire.Index != questBehavior.Initiator.Index)
			{
				return true;
			}
			break;
		case QuestInitiatorFilter.OtherEmpires:
			if (e.Empire.Index != questBehavior.Initiator.Index)
			{
				return true;
			}
			break;
		}
		return false;
	}

	public bool CheckAgainstQuestInitiatorFilter(QuestBehaviour questBehavior, global::Empire empire, QuestInitiatorFilter questInitiatorFilter)
	{
		Diagnostics.Assert(questBehavior.Initiator is MajorEmpire);
		MajorEmpire majorEmpire = empire as MajorEmpire;
		MajorEmpire majorEmpire2 = questBehavior.Initiator as MajorEmpire;
		switch (questInitiatorFilter)
		{
		case QuestInitiatorFilter.AllEmpires:
			return true;
		case QuestInitiatorFilter.Allies:
		{
			if (majorEmpire == null)
			{
				return false;
			}
			bool bits = majorEmpire.Bits != 0;
			int bits2 = majorEmpire2.Bits;
			if (((bits ? 1 : 0) & bits2) != 0)
			{
				return true;
			}
			break;
		}
		case QuestInitiatorFilter.Empire:
			if (empire.Index == questBehavior.Initiator.Index)
			{
				return true;
			}
			break;
		case QuestInitiatorFilter.Enemies:
			if (empire.Index != questBehavior.Initiator.Index)
			{
				return true;
			}
			break;
		case QuestInitiatorFilter.OtherEmpires:
			if (empire.Index != questBehavior.Initiator.Index)
			{
				return true;
			}
			break;
		}
		return false;
	}

	public State CheckConditions(QuestBehaviour questBehaviour, GameEvent gameEvent, params object[] parameters)
	{
		if (this.ConditionChecks == null)
		{
			return State.Success;
		}
		if (this.InterpretedVarsToUpdate != null && this.InterpretedVarsToUpdate.Length != 0)
		{
			Dictionary<StaticString, IEnumerable<SimulationObjectWrapper>> dictionary = new Dictionary<StaticString, IEnumerable<SimulationObjectWrapper>>();
			dictionary.Add("$(Empire)", new SimulationObjectWrapper[]
			{
				questBehaviour.Initiator
			});
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null && service.Game != null);
			dictionary.Add("$(Empires)", (from emp in (service.Game as global::Game).Empires
			where emp is MajorEmpire && !(emp as MajorEmpire).IsEliminated
			select emp).ToArray<global::Empire>());
			string[] interpretedVarsToUpdate = this.InterpretedVarsToUpdate;
			for (int i = 0; i < interpretedVarsToUpdate.Length; i++)
			{
				string variable = interpretedVarsToUpdate[i];
				QuestVariableDefinition questVariableDefinition = questBehaviour.Quest.QuestDefinition.Variables.FirstOrDefault((QuestVariableDefinition varDefinition) => varDefinition is QuestInterpretedVariableDefinition && varDefinition.VarName == variable);
				if (questVariableDefinition != null)
				{
					QuestVariable questVariable = questBehaviour.QuestVariables.FirstOrDefault((QuestVariable questVar) => questVar.Name == variable);
					if (questVariable != null)
					{
						questVariable.Object = (questVariableDefinition as QuestInterpretedVariableDefinition).Evaluate(dictionary, questBehaviour.QuestVariables);
						if (global::GameManager.Preferences.QuestVerboseMode)
						{
							Diagnostics.Log("[Quest] Updated value of '{0}' to '{1}' in quest '{2}'", new object[]
							{
								variable,
								questVariable.Object,
								questBehaviour.Quest.QuestDefinition.Name
							});
						}
					}
					else
					{
						Diagnostics.LogError("[Quest] InterpretedVarToUpdate '{0}' is not registered in quest behaviour '{1}'", new object[]
						{
							variable,
							questBehaviour.Quest.QuestDefinition.Name
						});
					}
				}
				else
				{
					Diagnostics.LogError("[Quest] Could not find InterpretedVarToUpdate '{0}' in quest '{1}'", new object[]
					{
						variable,
						questBehaviour.Quest.QuestDefinition.Name
					});
				}
			}
		}
		bool flag = false;
		for (int j = 0; j < this.ConditionChecks.Length; j++)
		{
			QuestBehaviourTreeNode_ConditionCheck questBehaviourTreeNode_ConditionCheck = this.ConditionChecks[j];
			State state = questBehaviourTreeNode_ConditionCheck.CheckCondition(questBehaviour, gameEvent, parameters);
			if (questBehaviourTreeNode_ConditionCheck.Inverted)
			{
				state = base.Inverse(state);
			}
			if (state == State.Failure)
			{
				if (questBehaviourTreeNode_ConditionCheck.IsFailureCondition)
				{
					return State.Failure;
				}
				flag = true;
			}
		}
		if (flag)
		{
			return State.Running;
		}
		return State.Success;
	}

	public override object Clone()
	{
		QuestBehaviourTreeNode_Decorator<T> questBehaviourTreeNode_Decorator = (QuestBehaviourTreeNode_Decorator<T>)base.MemberwiseClone();
		if (this.ConditionChecks != null)
		{
			questBehaviourTreeNode_Decorator.ConditionChecks = new QuestBehaviourTreeNode_ConditionCheck[this.ConditionChecks.Length];
			for (int i = 0; i < this.ConditionChecks.Length; i++)
			{
				questBehaviourTreeNode_Decorator.ConditionChecks[i] = (QuestBehaviourTreeNode_ConditionCheck)this.ConditionChecks[i].Clone();
			}
		}
		return questBehaviourTreeNode_Decorator;
	}

	public override State Execute(BehaviourTree behaviourTree, params object[] parameters)
	{
		QuestBehaviour questBehaviour = behaviourTree as QuestBehaviour;
		if (questBehaviour != null)
		{
			if (parameters == null)
			{
				return State.Running;
			}
			if (parameters.Length == 0)
			{
				return State.Running;
			}
			T t = parameters[0] as T;
			if (t != null)
			{
				GameEvent gameEvent = t as GameEvent;
				if (gameEvent != null && !this.CheckAgainstQuestInitiatorFilter(questBehaviour, gameEvent, this.QuestInitiatorFilter))
				{
					return State.Running;
				}
				if (this.questRepositoryService == null || this.questManagementService == null)
				{
					IGameService service = Services.GetService<IGameService>();
					Diagnostics.Assert(service != null && service.Game != null);
					this.questManagementService = service.Game.Services.GetService<IQuestManagementService>();
					Diagnostics.Assert(this.questManagementService != null);
					this.questRepositoryService = service.Game.Services.GetService<IQuestRepositoryService>();
					Diagnostics.Assert(this.questRepositoryService != null);
				}
				State state = this.Execute(questBehaviour, t, new object[0]);
				if (state == State.Success)
				{
					state = this.CheckConditions(questBehaviour, gameEvent, parameters);
					if (state == State.Success)
					{
						this.IncrementProgression(questBehaviour, t as GameEvent);
						if (!string.IsNullOrEmpty(this.PrerequisiteVerifiedMessage) && gameEvent.Empire.Index == questBehaviour.Initiator.Index)
						{
							QuestInstruction_ShowMessagePanel questInstruction = new QuestInstruction_ShowMessagePanel(this.PrerequisiteVerifiedMessage);
							questBehaviour.Push(questInstruction);
						}
					}
					else if (!string.IsNullOrEmpty(this.PrerequisiteNotVerifiedMessage) && gameEvent.Empire.Index == questBehaviour.Initiator.Index)
					{
						QuestInstruction_ShowMessagePanel questInstruction2 = new QuestInstruction_ShowMessagePanel(this.PrerequisiteNotVerifiedMessage);
						questBehaviour.Push(questInstruction2);
					}
				}
				return state;
			}
		}
		return State.Running;
	}

	public override bool Initialize(BehaviourTree behaviourTree)
	{
		QuestBehaviour questBehaviour = behaviourTree as QuestBehaviour;
		return questBehaviour != null && this.Initialize(questBehaviour);
	}

	protected abstract State Execute(QuestBehaviour questBehaviour, T e, params object[] parameters);

	protected void IncrementProgression(QuestBehaviour questBehaviour, GameEvent e)
	{
		if (!StaticString.IsNullOrEmpty(this.LinkedStepProgression))
		{
			QuestVariable questVariable = questBehaviour.QuestVariables.FirstOrDefault((QuestVariable match) => match.Name == this.LinkedStepProgression);
			if (questVariable != null)
			{
				QuestRegisterVariable questRegisterVariable = questVariable.Object as QuestRegisterVariable;
				if (questRegisterVariable != null)
				{
					questRegisterVariable.Value += this.ProgressionIncrement;
					questBehaviour.Push(new QuestInstruction_UpdateRegisterVariable(this.LinkedStepProgression, questRegisterVariable.Value));
					if (e != null && questBehaviour.Quest.QuestDefinition.IsGlobal && questBehaviour.Initiator.Index == 0)
					{
						QuestBehaviour questBehaviour2 = this.questRepositoryService.GetQuestBehaviour(questBehaviour.Quest.Name, e.Empire.Index);
						if (questBehaviour2 != null)
						{
							QuestVariable questVariable2 = questBehaviour2.QuestVariables.FirstOrDefault((QuestVariable match) => match.Name == this.PersonnalLinkedStepProgression);
							if (questVariable2 != null)
							{
								QuestRegisterVariable questRegisterVariable2 = questVariable2.Object as QuestRegisterVariable;
								if (questRegisterVariable2 != null)
								{
									questRegisterVariable2.Value += this.ProgressionIncrement;
									questBehaviour2.Push(new QuestInstruction_UpdateRegisterVariable(this.PersonnalLinkedStepProgression, questRegisterVariable2.Value));
									this.questManagementService.SendPendingInstructions(questBehaviour2);
									return;
								}
							}
						}
						else
						{
							Diagnostics.LogError("Cannot find quest behaviour of global quest '{0}' for empire {1}", new object[]
							{
								questBehaviour.Quest.Name,
								e.Empire.Index
							});
						}
					}
				}
			}
		}
	}

	protected virtual bool Initialize(QuestBehaviour questBehaviour)
	{
		if (this.ConditionChecks != null)
		{
			for (int i = 0; i < this.ConditionChecks.Length; i++)
			{
				if (!this.ConditionChecks[i].Initialize(questBehaviour))
				{
					return false;
				}
			}
		}
		return true;
	}

	protected void UpdateQuestVariable(QuestBehaviour questBehaviour, string name, object value)
	{
		if (!string.IsNullOrEmpty(name))
		{
			QuestVariable questVariable = questBehaviour.GetQuestVariableByName(name);
			if (questVariable == null)
			{
				questVariable = new QuestVariable(name);
				questBehaviour.QuestVariables.Add(questVariable);
			}
			questVariable.Object = value;
		}
	}

	[XmlIgnore]
	private IQuestManagementService questManagementService;

	[XmlIgnore]
	private IQuestRepositoryService questRepositoryService;
}
