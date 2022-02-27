﻿using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class QuestBehaviourTreeNode_Decorator_TimerEnded : QuestBehaviourTreeNode_Decorator<EventBeginTurn>
{
	[XmlAttribute("TimerVarName")]
	public string TimerVarName { get; set; }

	[XmlAttribute("TurnCountBeforeTimeOut")]
	public int TurnCountBeforeTimeOut { get; set; }

	[XmlAttribute("TimerLocalizationKey")]
	public string TimerLocalizationKey { get; set; }

	protected override State Execute(QuestBehaviour questBehaviour, EventBeginTurn e, params object[] parameters)
	{
		int num = this.ComputeEleapsedTurn(questBehaviour);
		if (num == -1)
		{
			Diagnostics.LogError("QuestBehaviourTreeNode_Decorator_TimerEnded, Timer '{0}' isn't started, you can't check it.", new object[]
			{
				this.TimerVarName
			});
			return State.Success;
		}
		int num2 = this.TurnCountBeforeTimeOut - num;
		if (!string.IsNullOrEmpty(this.TimerLocalizationKey))
		{
			QuestInstruction_UpdateLocalizationVariable questInstruction = new QuestInstruction_UpdateLocalizationVariable(this.TimerLocalizationKey, num2.ToString());
			questBehaviour.Push(questInstruction);
		}
		Diagnostics.Log("Quest '{0}', Timer '{1}' eleapsedTurn = {2}.", new object[]
		{
			questBehaviour.Quest.QuestDefinition.Name,
			this.TimerVarName,
			num
		});
		if (num >= this.TurnCountBeforeTimeOut)
		{
			return State.Success;
		}
		return State.Running;
	}

	protected override bool Initialize(QuestBehaviour questBehaviour)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service == null || service.Game == null)
		{
			Diagnostics.LogError("Failed to retrieve the game service.");
			return false;
		}
		this.game = (service.Game as global::Game);
		if (this.game == null)
		{
			Diagnostics.LogError("Failed to cast gameService.Game to Game.");
			return false;
		}
		if (!string.IsNullOrEmpty(this.TimerLocalizationKey))
		{
			int num = this.ComputeEleapsedTurn(questBehaviour);
			if (num != -1)
			{
				int num2 = this.TurnCountBeforeTimeOut - num;
				if (num2 >= 0)
				{
					QuestInstruction_UpdateLocalizationVariable questInstruction = new QuestInstruction_UpdateLocalizationVariable(this.TimerLocalizationKey, num2.ToString());
					questBehaviour.Push(questInstruction);
				}
			}
		}
		return base.Initialize(questBehaviour);
	}

	protected int ComputeEleapsedTurn(QuestBehaviour questBehaviour)
	{
		QuestVariable questVariableByName = questBehaviour.GetQuestVariableByName(this.TimerVarName);
		if (questVariableByName == null)
		{
			return -1;
		}
		int num = (int)questVariableByName.Object;
		int turn = this.game.Turn;
		return turn - num;
	}

	protected global::Game game;
}
