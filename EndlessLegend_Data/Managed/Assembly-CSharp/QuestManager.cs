using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Extensions;
using Amplitude.Query;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Session;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Utilities.Maps;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class QuestManager : GameAncillary, Amplitude.Xml.Serialization.IXmlSerializable, IService, IEnumerable, IDumpable, IQuestManagementService, IQuestRepositoryService, IEnumerable<Quest>, IRepositoryService<Quest>, IEnumerable<KeyValuePair<ulong, Quest>>, IQuestRewardRepositoryService
{
	public event EventHandler<QuestRepositoryChangeEventArgs> QuestRepositoryChange;

	bool IQuestManagementService.AddStepRewards(QuestDefinition questDefinition, int stepIndex, List<QuestReward> rewards, List<IDroppable> droppedRewards, Quest quest)
	{
		QuestStep questStep = questDefinition.Steps[stepIndex];
		bool flag = quest != null;
		if (questStep.QuestRewardDefinitions == null || questStep.QuestRewardDefinitions.Length == 0)
		{
			return true;
		}
		if (rewards == null)
		{
			rewards = new List<QuestReward>();
		}
		if (droppedRewards == null)
		{
			droppedRewards = new List<IDroppable>();
		}
		for (int i = 0; i < questStep.QuestRewardDefinitions.Length; i++)
		{
			QuestRewardDefinition questRewardDefinition = questStep.QuestRewardDefinitions[i];
			if (questRewardDefinition.Dynamic == flag)
			{
				int picks;
				if (questRewardDefinition.Progressive)
				{
					StaticString staticString = questStep.Name;
					if (questDefinition.GlobalWinner == GlobalQuestWinner.Participants)
					{
						staticString += Quest.PersonnalProgressStepSuffix;
					}
					QuestRegisterVariable questRegisterVariable;
					if (!quest.QuestVariables.TryGetValue(staticString, out questRegisterVariable))
					{
						goto IL_24A;
					}
					picks = questRegisterVariable.Value;
				}
				else
				{
					picks = questRewardDefinition.Picks;
				}
				IEnumerable<IDroppable> enumerable = null;
				if (!string.IsNullOrEmpty(questRewardDefinition.DropVar))
				{
					QuestVariable questVariable = this.questManagementService.State.QuestVariables.FirstOrDefault((QuestVariable var) => var.Name == questRewardDefinition.DropVar);
					if (questVariable == null)
					{
						Diagnostics.LogError("DropVar {0} cannot be found for reward of quest {1}", new object[]
						{
							questRewardDefinition.DropVar,
							questDefinition.Name
						});
					}
					else
					{
						enumerable = (questVariable.Object as IEnumerable<IDroppable>);
					}
				}
				else
				{
					Empire empire = (quest == null) ? null : base.Game.Empires.FirstOrDefault((Empire emp) => emp.Bits == quest.EmpireBits);
					enumerable = this.Drop(questRewardDefinition.Droplist, picks, droppedRewards, empire, quest);
				}
				if (enumerable != null)
				{
					QuestReward questReward = new QuestReward(stepIndex, questRewardDefinition.Hidden, enumerable.ToArray<IDroppable>(), questRewardDefinition.LocalizationKey, questRewardDefinition.JustForShow, questRewardDefinition.MinimumRank);
					rewards.Add(questReward);
					droppedRewards.AddRange(questReward.Droppables);
				}
			}
			IL_24A:;
		}
		return true;
	}

	List<QuestReward> IQuestManagementService.ComputeRewards(Quest quest, QuestDefinition questDefinition, int stepIndex, List<IDroppable> droppedRewards)
	{
		List<QuestReward> list = new List<QuestReward>();
		if (droppedRewards == null)
		{
			droppedRewards = new List<IDroppable>();
		}
		QuestStep questStep = questDefinition.Steps[stepIndex];
		Empire empire = (quest == null) ? null : base.Game.Empires.FirstOrDefault((Empire emp) => emp.Bits == quest.EmpireBits);
		for (int i = 0; i < questStep.QuestRewardDefinitions.Length; i++)
		{
			QuestRewardDefinition questRewardDefinition = questStep.QuestRewardDefinitions[i];
			IEnumerable<IDroppable> enumerable = this.Drop(questRewardDefinition.Droplist, questRewardDefinition.Picks, droppedRewards, empire, quest);
			if (enumerable != null)
			{
				QuestReward item = new QuestReward(stepIndex, questRewardDefinition.Hidden, enumerable.ToArray<IDroppable>(), questRewardDefinition.LocalizationKey, questRewardDefinition.JustForShow, questRewardDefinition.MinimumRank);
				list.Add(item);
			}
		}
		return list;
	}

	void IQuestManagementService.ApplyQuestWorldEffect(OrderQuestWorldEffect order)
	{
		if (order.Duration == 0 && order.AddEffect)
		{
			return;
		}
		Empire[] array;
		if (order.EmpireIndex == -1)
		{
			array = base.Game.Empires;
		}
		else
		{
			if (order.EmpireIndex >= base.Game.Empires.Length)
			{
				Diagnostics.LogError("Cannot find empire n°{0}", new object[]
				{
					order.EmpireIndex
				});
				return;
			}
			array = new Empire[]
			{
				base.Game.Empires[order.EmpireIndex]
			};
		}
		bool flag;
		if (order.AddEffect)
		{
			flag = true;
			if (this.descriptorDatabase == null)
			{
				this.descriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
			}
			if (this.descriptorDatabase == null)
			{
				Diagnostics.LogError("Failed to retrieve SimulationDescriptor database");
				return;
			}
			SimulationDescriptor value = this.descriptorDatabase.GetValue(order.WorldEffectName);
			if (value == null)
			{
				Diagnostics.LogError("Failed to retrieve world effect descriptor {0}", new object[]
				{
					order.WorldEffectName
				});
				return;
			}
			SimulationDescriptor value2 = this.descriptorDatabase.GetValue("ClassTimedBonus");
			if (value2 == null)
			{
				Diagnostics.LogError("Failed to retrieve ClassTimedBonus descriptor", new object[]
				{
					order.WorldEffectName
				});
				return;
			}
			Diagnostics.Log("[Quest] Applying quest world effect {0} for {1} turns", new object[]
			{
				order.WorldEffectName,
				order.Duration
			});
			this.RemoveQuestWorldEffect(array, order);
			List<SimulationObject> list = new List<SimulationObject>();
			for (int i = 0; i < array.Length; i++)
			{
				SimulationObject simulationObject = array[i].SimulationObject;
				if (simulationObject != null)
				{
					SimulationObject simulationObject2 = new SimulationObject(order.WorldEffectName);
					simulationObject.AddChild(simulationObject2);
					simulationObject2.AddDescriptor(value);
					if (order.Duration >= 0)
					{
						simulationObject2.AddDescriptor(value2);
						simulationObject2.SetPropertyBaseValue(SimulationProperties.RemainingTime, (float)order.Duration);
						simulationObject2.Refresh();
					}
					list.Add(simulationObject2);
				}
			}
			this.questManagementService.RegisterQuestWorldEffect(order.WorldEffectName, order.EmpireIndex, list);
		}
		else
		{
			flag = this.RemoveQuestWorldEffect(array, order);
			Diagnostics.Log("[Quest] Removing quest world effect: {0}", new object[]
			{
				order.WorldEffectName
			});
		}
		if (flag && this.EventService != null)
		{
			for (int j = 0; j < array.Length; j++)
			{
				this.EventService.Notify(new EventQuestGlobalEvent(array[j], order.WorldEffectName, order.AddEffect, order.EmpireIndex, order.Silent));
			}
		}
	}

	void IQuestManagementService.CheckForQuestCompletion(GameEntityGUID questGUID)
	{
		QuestBehaviour questBehaviour;
		if (questGUID.IsValid && this.isHosting && this.questBehaviours.TryGetValue(questGUID, out questBehaviour))
		{
			switch (questBehaviour.Quest.QuestState)
			{
			case QuestState.Completed:
			case QuestState.Failed:
				this.questBehaviours.Remove(questGUID);
				if (this.PlayerController != null)
				{
					OrderCompleteQuest order = new OrderCompleteQuest(questBehaviour.Quest);
					this.PlayerController.PostOrder(order);
				}
				break;
			}
		}
	}

	void IQuestManagementService.ExecuteQuest(QuestBehaviour questBehaviour)
	{
		Diagnostics.Assert(questBehaviour != null);
		State state = questBehaviour.Execute(new object[0]);
		this.questManagementService.SendPendingInstructions(questBehaviour);
		if (state != Amplitude.Unity.AI.BehaviourTree.State.Running && questBehaviour.PendingInstructions.Count == 0 && questBehaviour.DelayedInstructions.Count == 0 && questBehaviour.Quest.QuestState != QuestState.InProgress)
		{
			this.questBehaviours.Remove(questBehaviour.Quest.GUID);
			if (this.PlayerController != null)
			{
				OrderCompleteQuest order = new OrderCompleteQuest(questBehaviour.Quest);
				this.PlayerController.PostOrder(order);
			}
			if (GameManager.Preferences.QuestVerboseMode)
			{
				Diagnostics.Log("ELCP: IQuestManagementService.ExecuteQuest {0} {1}", new object[]
				{
					questBehaviour.Quest.Name,
					state
				});
			}
			if (state == Amplitude.Unity.AI.BehaviourTree.State.Success && questBehaviour.Quest != null && questBehaviour.Quest.QuestDefinition != null && questBehaviour.Quest.QuestDefinition.Triggers != null && questBehaviour.Quest.QuestDefinition.Triggers.OnQuestCompleted != null && questBehaviour.Quest.QuestDefinition.Triggers.OnQuestCompleted.Tags != null)
			{
				this.questManagementService.InitState(questBehaviour.Quest.QuestDefinition.Triggers.OnQuestCompleted.Tags, questBehaviour.Initiator, WorldPosition.Invalid);
				QuestDefinition questDefinition;
				QuestVariable[] collection;
				QuestInstruction[] pendingInstructions;
				QuestReward[] questRewards;
				Dictionary<Region, List<string>> regionQuestLocalizationVariableDefinitionLocalizationKey;
				if (this.TryTrigger(out questDefinition, out collection, out pendingInstructions, out questRewards, out regionQuestLocalizationVariableDefinitionLocalizationKey))
				{
					List<QuestVariable> list = new List<QuestVariable>(collection);
					list.AddRange(this.State.GlobalQuestVariablesCurrentEmpire);
					this.Trigger(this.State.Empire, questDefinition, list.ToArray(), pendingInstructions, questRewards, regionQuestLocalizationVariableDefinitionLocalizationKey, null, true);
				}
			}
		}
	}

	void IQuestManagementService.ExecuteRunningQuests(Amplitude.Unity.Event.Event e)
	{
		List<ulong> list = new List<ulong>();
		Dictionary<StaticString, List<QuestManager.QuestBehaviourDelayedInstructions>> dictionary = new Dictionary<StaticString, List<QuestManager.QuestBehaviourDelayedInstructions>>();
		Dictionary<ulong, QuestBehaviour> dictionary2 = new Dictionary<ulong, QuestBehaviour>(this.questBehaviours);
		foreach (KeyValuePair<ulong, QuestBehaviour> keyValuePair in dictionary2)
		{
			QuestBehaviour value = keyValuePair.Value;
			State state = value.Execute(new object[]
			{
				e
			});
			List<QuestInstruction> list2 = new List<QuestInstruction>();
			if (value.Quest.QuestDefinition.IsGlobal && value.Quest.QuestDefinition.GlobalWinner == GlobalQuestWinner.First && state == Amplitude.Unity.AI.BehaviourTree.State.Success)
			{
				List<QuestInstruction> list3 = new List<QuestInstruction>();
				foreach (QuestInstruction questInstruction in value.PendingInstructions)
				{
					if (questInstruction.MustBeDelayed)
					{
						list2.Add(questInstruction);
					}
					else
					{
						list3.Add(questInstruction);
					}
				}
				value.PendingInstructions = new Queue<QuestInstruction>(list3);
			}
			if (value.PendingInstructions.Count > 0 && list2.Count > 0)
			{
				foreach (QuestInstruction questInstruction2 in value.PendingInstructions)
				{
					if (questInstruction2.CanBeBypassOnDelay)
					{
						questInstruction2.Execute(value.Quest);
					}
				}
				this.questManagementService.SendPendingInstructions(value);
			}
			else if (list2.Count == 0)
			{
				this.CompleteQuestBehaviour(value, list);
			}
			if (list2.Count > 0)
			{
				List<QuestManager.QuestBehaviourDelayedInstructions> list4;
				if (!dictionary.TryGetValue(value.Quest.Name, out list4))
				{
					list4 = new List<QuestManager.QuestBehaviourDelayedInstructions>();
					dictionary.Add(value.Quest.Name, list4);
				}
				list4.Add(new QuestManager.QuestBehaviourDelayedInstructions
				{
					QuestBehaviour = value,
					DelayedInstructions = list2
				});
				if (GameManager.Preferences.QuestVerboseMode)
				{
					Diagnostics.Log("[Quest] Delaying quest {0} for empire {1} in case of ties", new object[]
					{
						value.Quest.Name,
						value.Initiator.Index
					});
				}
			}
		}
		foreach (KeyValuePair<StaticString, List<QuestManager.QuestBehaviourDelayedInstructions>> keyValuePair2 in dictionary)
		{
			List<QuestManager.QuestBehaviourDelayedInstructions> list5 = keyValuePair2.Value.Randomize(null);
			list5 = (from questBehaviourDelayedInstructions in list5
			orderby questBehaviourDelayedInstructions.QuestBehaviour.Quest.GetCurrentProgression(false) descending
			select questBehaviourDelayedInstructions).ToList<QuestManager.QuestBehaviourDelayedInstructions>();
			QuestBehaviour questBehaviour = list5[0].QuestBehaviour;
			questBehaviour.PendingInstructions = new Queue<QuestInstruction>(list5[0].DelayedInstructions);
			this.CompleteQuestBehaviour(questBehaviour, list);
			if (GameManager.Preferences.QuestVerboseMode && list5.Count > 1)
			{
				Diagnostics.Log("[Quest] Empire {1} won the tie for delayed quest {0}", new object[]
				{
					keyValuePair2.Key,
					questBehaviour.Initiator.Index
				});
			}
		}
		foreach (ulong key in list)
		{
			this.questBehaviours.Remove(key);
		}
	}

	void IQuestManagementService.ForceQuestCompletion(GameEntityGUID questGUID, QuestState questState)
	{
		if (!questGUID.IsValid)
		{
			Diagnostics.LogError("ForceQuestCompletion: questGUID is invalid.");
			return;
		}
		QuestBehaviour questBehaviour;
		if (!this.questBehaviours.TryGetValue(questGUID, out questBehaviour))
		{
			Diagnostics.LogError("ForceQuestCompletion: The quest you are trying to complete is not in progress (questGUID = '{0}').", new object[]
			{
				questGUID.ToString()
			});
			return;
		}
		if (questBehaviour == null)
		{
			Diagnostics.LogError("ForceQuestCompletion: questBehaviour is null.");
			return;
		}
		if (questBehaviour.Quest == null)
		{
			Diagnostics.LogError("ForceQuestCompletion: questBehaviour.Quest is null.");
			return;
		}
		if (questState != QuestState.Failed && questState != QuestState.Completed)
		{
			Diagnostics.LogError("ForceQuestCompletion: quest state is invalid : '{0}' instead of Failed or Completed.", new object[]
			{
				questState.ToString()
			});
			return;
		}
		if (questState == QuestState.Completed)
		{
			for (int i = 0; i < questBehaviour.Quest.StepStates.Length; i++)
			{
				if (questBehaviour.Quest.StepStates[i] != QuestState.Completed)
				{
					QuestInstruction_UpdateStep questInstruction = new QuestInstruction_UpdateStep(i, QuestState.Completed, false);
					questBehaviour.Push(questInstruction);
				}
			}
		}
		else
		{
			int num = Array.IndexOf<QuestState>(questBehaviour.Quest.StepStates, questBehaviour.Quest.StepStates.FirstOrDefault((QuestState match) => match == QuestState.Failed || match == QuestState.InProgress));
			if (num != -1)
			{
				QuestInstruction_UpdateStep questInstruction2 = new QuestInstruction_UpdateStep(num, QuestState.Failed, false);
				questBehaviour.Push(questInstruction2);
			}
		}
		QuestInstruction_UpdateQuest questInstruction3 = new QuestInstruction_UpdateQuest(questState);
		questBehaviour.Push(questInstruction3);
		questBehaviour.OnForceCompletion();
		this.questManagementService.SendPendingInstructions(questBehaviour);
	}

	StaticString IQuestManagementService.GetGlobalProgressionString(Quest quest, StaticString questStepName)
	{
		int stepIndexByName = quest.GetStepIndexByName(questStepName);
		if (stepIndexByName < 0 || stepIndexByName >= quest.QuestDefinition.Steps.Length || quest.QuestDefinition.Steps[stepIndexByName].ProgressionRange == null)
		{
			return string.Empty;
		}
		Diagnostics.Assert(base.Game != null && base.Game.Empires != null);
		if (base.Game.Empires.FirstOrDefault((Empire empire) => empire.Bits == quest.EmpireBits) == null)
		{
			Diagnostics.LogWarning("Failed to find active empire of quest {0}", new object[]
			{
				quest.QuestDefinition.Name
			});
			return string.Empty;
		}
		bool flag = quest.QuestDefinition.GlobalWinner == GlobalQuestWinner.Participants;
		StaticString staticString = AgeLocalizer.Instance.LocalizeString("%QuestProgression") + quest.GetNonGlobalProgressionString(questStepName, flag);
		if (!flag)
		{
			int num = -1;
			int globalQuestRank = this.questManagementService.GetGlobalQuestRank(quest, ref num, questStepName);
			staticString = string.Concat(new object[]
			{
				staticString,
				Environment.NewLine,
				AgeLocalizer.Instance.LocalizeString("%QuestRank"),
				globalQuestRank + 1,
				"/",
				num
			});
		}
		return staticString;
	}

	int IQuestManagementService.GetGlobalQuestRank(Quest quest, ref int questHolderCount, string questStepName)
	{
		int i = -1;
		if (questStepName == null)
		{
			int currentStepIndex = quest.GetCurrentStepIndex();
			if (currentStepIndex < 0)
			{
				return i;
			}
			questStepName = quest.QuestDefinition.Steps[currentStepIndex].Name;
		}
		IEnumerable<Quest> questsInProgressNamed = this.GetQuestsInProgressNamed(quest.Name);
		if (questsInProgressNamed != null)
		{
			StaticString realStepName = (quest.QuestDefinition.GlobalWinner != GlobalQuestWinner.Participants) ? questStepName : (questStepName + Quest.PersonnalProgressStepSuffix);
			KeyValuePair<int, int>[] array = (from q in questsInProgressNamed
			select new KeyValuePair<int, int>(q.EmpireBits, q.GetStepProgressionValueByName(realStepName))).ToArray<KeyValuePair<int, int>>();
			array = (from kvp in array
			orderby kvp.Value descending
			select kvp).ToArray<KeyValuePair<int, int>>();
			questHolderCount = array.Length;
			int value = array.FirstOrDefault((KeyValuePair<int, int> kvp) => kvp.Key == quest.EmpireBits).Value;
			for (i = 0; i < array.Length; i++)
			{
				if (array[i].Value == value)
				{
					break;
				}
			}
		}
		return i;
	}

	IEnumerable<QuestMarker> IQuestManagementService.GetMarkersByQuestGUID(GameEntityGUID questGuid)
	{
		foreach (List<QuestMarker> markersByTarget in this.questMarkers.Values)
		{
			for (int markerIndex = 0; markerIndex < markersByTarget.Count; markerIndex++)
			{
				if (markersByTarget[markerIndex].QuestGUID == questGuid)
				{
					yield return markersByTarget[markerIndex];
				}
			}
		}
		yield break;
	}

	IEnumerable<QuestMarker> IQuestManagementService.GetMarkersByBoundTargetGUID(GameEntityGUID targetGameEntityGuid)
	{
		List<QuestMarker> markersByTarget;
		if (this.questMarkers.TryGetValue(targetGameEntityGuid, out markersByTarget))
		{
			for (int markerIndex = 0; markerIndex < markersByTarget.Count; markerIndex++)
			{
				yield return markersByTarget[markerIndex];
			}
		}
		yield break;
	}

	void IQuestManagementService.InitState(Tags tags, Empire empire, WorldPosition worldPosition)
	{
		this.InitStateCommon(empire, worldPosition);
		this.State.Tags.AddTag(tags);
	}

	void IQuestManagementService.InitState(StaticString eventName, Empire empire, WorldPosition worldPosition)
	{
		this.InitStateCommon(empire, worldPosition);
		this.State.Tags.AddTag(eventName);
	}

	bool IQuestManagementService.IsALockedQuestTargetGameEntity(GameEntityGUID guid, Empire empire)
	{
		QuestTargetGameEntity target = new QuestTargetGameEntity(guid);
		if (empire == null)
		{
			foreach (int key in this.questTargetsLocked.Keys)
			{
				List<QuestTarget> list;
				if (this.questTargetsLocked.TryGetValue(key, out list) && list.Exists((QuestTarget match) => match.Equals(target)))
				{
					return true;
				}
			}
			return false;
		}
		List<QuestTarget> list2;
		if (this.questTargetsLocked.TryGetValue(empire.Index, out list2))
		{
			return list2.Exists((QuestTarget match) => match.Equals(target));
		}
		return false;
	}

	bool IQuestManagementService.IsDroppableRewaredByQuestsInProgress(Empire empire, IDroppable droppable)
	{
		return this.questBehaviours != null && droppable != null && this.questBehaviours.Values.Any((QuestBehaviour questBehaviour) => questBehaviour.Initiator.Index == empire.Index && questBehaviour.Quest != null && questBehaviour.Quest.QuestRewards != null && questBehaviour.Quest.QuestState == QuestState.InProgress && questBehaviour.Quest.QuestRewards.Any((QuestReward reward) => reward.Droppables != null && reward.Droppables.Any((IDroppable droppableReward) => droppableReward.Equals(droppable))));
	}

	bool IQuestManagementService.IsQuestRunningForEmpire(StaticString questDefinitionName, Empire empire)
	{
		return this.questBehaviours.Any((KeyValuePair<ulong, QuestBehaviour> match) => match.Value.Initiator.Index == empire.Index && match.Value.Quest != null && match.Value.Quest.QuestDefinition.Name == questDefinitionName);
	}

	void IQuestManagementService.LockQuestTarget(QuestTarget questTarget, Empire empire)
	{
		List<QuestTarget> list;
		if (this.questTargetsLocked.TryGetValue(empire.Index, out list) && !list.Any((QuestTarget match) => match.Equals(questTarget)))
		{
			list.Add(questTarget);
		}
	}

	void IQuestManagementService.Register(QuestBehaviour questBehaviour)
	{
		if (questBehaviour == null)
		{
			throw new ArgumentNullException("behaviour");
		}
		if (questBehaviour.Quest == null)
		{
			throw new ArgumentNullException("behaviour.Quest");
		}
		this.questBehaviours.Add(questBehaviour.Quest.GUID, questBehaviour);
		QuestOccurence questOccurence;
		if (!this.questOccurences.TryGetValue(questBehaviour.Quest.QuestDefinition.Name, out questOccurence))
		{
			questOccurence = new QuestOccurence();
			this.questOccurences.Add(questBehaviour.Quest.QuestDefinition.Name, questOccurence);
		}
		questOccurence.LastStartedOnTurn = base.Game.Turn;
		questOccurence.NumberOfOccurencesThisGame++;
		if (questBehaviour.Initiator.Index < questOccurence.NumberOfOccurrencesForThisEmpireSoFar.Length)
		{
			questOccurence.NumberOfOccurrencesForThisEmpireSoFar[questBehaviour.Initiator.Index]++;
		}
		if (questBehaviour.Quest.QuestDefinition.IsGlobal && !questBehaviour.Quest.IsMainQuest())
		{
			this.nextPossibleGlobalQuestTurn = Mathf.Max(this.nextPossibleGlobalQuestTurn, base.Game.Turn);
		}
	}

	void IQuestManagementService.Register(QuestMarker questMarker)
	{
		if (questMarker == null)
		{
			throw new ArgumentNullException("questMarker");
		}
		if (!questMarker.BoundTargetGUID.IsValid)
		{
			throw new ArgumentException("questMarker.TargetGUID");
		}
		List<QuestMarker> list;
		if (!this.questMarkers.TryGetValue(questMarker.BoundTargetGUID, out list))
		{
			list = new List<QuestMarker>();
			this.questMarkers.Add(questMarker.BoundTargetGUID, list);
		}
		list.Add(questMarker);
	}

	void IQuestManagementService.RegisterQuestWorldEffect(StaticString effectName, int affectedEmpire, List<SimulationObject> effectSimulationObjects)
	{
		KeyValuePair<int, List<SimulationObject>> value = new KeyValuePair<int, List<SimulationObject>>(affectedEmpire, effectSimulationObjects);
		if (this.questWorldEffects.ContainsKey(effectName))
		{
			this.questWorldEffects[effectName] = value;
			Diagnostics.LogWarning("QuestWorldEffect {0} is already applied", new object[]
			{
				effectName
			});
		}
		else
		{
			this.questWorldEffects.Add(effectName, value);
		}
	}

	void IQuestManagementService.SendPendingInstructions(QuestBehaviour questBehaviour)
	{
		if (questBehaviour.PendingInstructions.Count != 0)
		{
			if (this.PlayerController != null)
			{
				OrderUpdateQuest order = new OrderUpdateQuest(questBehaviour.Quest, questBehaviour.PendingInstructions);
				this.PlayerController.PostOrder(order);
			}
			questBehaviour.PendingInstructions.Clear();
		}
	}

	StaticString IQuestManagementService.ForceTrigger(QuestDefinition questDefinition, Empire empire, bool stopIfError)
	{
		List<QuestVariable> list = new List<QuestVariable>();
		List<QuestInstruction> list2 = new List<QuestInstruction>();
		Dictionary<Region, List<string>> regionQuestLocalizationVariableDefinitionLocalizationKey = new Dictionary<Region, List<string>>();
		this.InitStateCommon(empire, WorldPosition.Invalid);
		bool flag = this.CheckVariables(questDefinition.GetPrerequisiteVariables(), questDefinition, list, list2, regionQuestLocalizationVariableDefinitionLocalizationKey);
		bool flag2 = this.CheckPrerequisites(questDefinition, this.State.Targets);
		bool flag3 = this.CheckVariables(questDefinition.GetNonPrerequisiteVariables(), questDefinition, list, list2, regionQuestLocalizationVariableDefinitionLocalizationKey);
		QuestReward[] questRewards;
		bool flag4 = this.CheckRewards(questDefinition, out questRewards);
		bool flag5 = false;
		if (!stopIfError || (flag && flag2 && flag3 && flag4))
		{
			flag5 = this.Trigger(empire, questDefinition, list.ToArray(), list2.ToArray(), questRewards, regionQuestLocalizationVariableDefinitionLocalizationKey, null, true);
		}
		string text = "Triggering ";
		if (flag5)
		{
			text += "successfull.";
		}
		else if (stopIfError)
		{
			text += "stoped.";
		}
		else
		{
			text += "failed.";
		}
		if (!flag)
		{
			text += " Prerequisite Variables failed.";
		}
		if (!flag2)
		{
			text += " Prerequisites failed.";
		}
		if (!flag3)
		{
			text += " Variables failed.";
		}
		if (!flag4)
		{
			text += " Rewards failed.";
		}
		return text;
	}

	bool IQuestManagementService.TryGetMarkerByGUID(GameEntityGUID markerGuid, out QuestMarker questMarker)
	{
		foreach (List<QuestMarker> list in this.questMarkers.Values)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].GUID == markerGuid)
				{
					questMarker = list[i];
					return true;
				}
			}
		}
		questMarker = null;
		return false;
	}

	void IQuestManagementService.UnlockQuestTarget(QuestTarget questTarget, Empire empire)
	{
		List<QuestTarget> list;
		if (this.questTargetsLocked.TryGetValue(empire.Index, out list))
		{
			list.RemoveAll((QuestTarget match) => match.Equals(questTarget));
		}
	}

	void IQuestManagementService.Unregister(QuestBehaviour questBehaviour)
	{
		if (questBehaviour == null)
		{
			throw new ArgumentNullException("behaviour");
		}
		if (questBehaviour.Quest == null)
		{
			throw new ArgumentNullException("behaviour.Quest");
		}
		this.questBehaviours.Remove(questBehaviour.Quest.GUID);
		QuestOccurence questOccurence;
		if (this.questOccurences.TryGetValue(questBehaviour.Quest.QuestDefinition.Name, out questOccurence))
		{
			questOccurence.LastCompletedOnTurn = base.Game.Turn;
		}
		questBehaviour.OnUnregister();
	}

	void IQuestManagementService.Unregister(QuestMarker questMarker)
	{
		if (questMarker == null)
		{
			throw new ArgumentNullException("questMarker");
		}
		if (!questMarker.BoundTargetGUID.IsValid)
		{
			throw new ArgumentException("questMarker.TargetGUID");
		}
		List<QuestMarker> list;
		if (this.questMarkers.TryGetValue(questMarker.BoundTargetGUID, out list))
		{
			list.Remove(questMarker);
			if (list.Count == 0)
			{
				this.questMarkers.Remove(questMarker.BoundTargetGUID);
			}
		}
	}

	void IQuestManagementService.UnregisterQuestBehaviour(Quest quest)
	{
		if (quest == null)
		{
			throw new ArgumentNullException("quest");
		}
		this.questBehaviours.Remove(quest.GUID);
		QuestOccurence questOccurence;
		if (this.questOccurences.TryGetValue(quest.QuestDefinition.Name, out questOccurence))
		{
			questOccurence.LastCompletedOnTurn = base.Game.Turn;
		}
	}

	IEnumerable<Quest> IQuestRepositoryService.AsEnumerable(int empireIndex)
	{
		foreach (Quest quest in this.quests.Values)
		{
			if ((quest.EmpireBits & 1 << empireIndex) != 0)
			{
				yield return quest;
			}
		}
		yield break;
	}

	IEnumerator<Quest> IEnumerable<Quest>.GetEnumerator()
	{
		foreach (Quest quest in this.quests.Values)
		{
			yield return quest;
		}
		yield break;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.quests.GetEnumerator();
	}

	public void DumpAsText(StringBuilder content, string indent)
	{
		Empire empire;
		foreach (Empire empire2 in from e in base.Game.Empires
		where e is MajorEmpire
		select e)
		{
			empire = empire2;
			content.AppendFormat("[Empire#{0}]\r\n", empire.Index);
			foreach (Quest quest in this.quests.Values.Where((Quest q) => (q.EmpireBits & 1 << empire.Index) != 0).OrderBy((Quest q) => q.GUID))
			{
				content.AppendFormat("{5}Quest#{0} {1}/{2}/{3} {4}\r\n", new object[]
				{
					quest.GUID.ToString(),
					quest.QuestDefinition.Category,
					quest.QuestDefinition.SubCategory,
					quest.QuestDefinition.Name,
					quest.QuestState,
					indent + "  "
				});
				for (int i = 0; i < quest.QuestRewards.Length; i++)
				{
					string arg = "NoDrop";
					if (quest.QuestRewards[i].Droppables != null && quest.QuestRewards[i].Droppables.Length != 0)
					{
						arg = quest.QuestRewards[i].Droppables.Aggregate(string.Empty, (string current, IDroppable t) => current + ((current.Length <= 0) ? string.Empty : ", ") + t.ToString());
					}
					content.AppendFormat("{0}Step{1} {2}\r\n", indent + "    ", quest.QuestRewards[i].StepNumber, arg);
				}
			}
		}
	}

	public byte[] DumpAsBytes()
	{
		MemoryStream memoryStream = new MemoryStream();
		using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
		{
			Empire empire;
			foreach (Empire empire2 in from e in base.Game.Empires
			where e is MajorEmpire
			select e)
			{
				empire = empire2;
				foreach (Quest quest in this.quests.Values.Where((Quest q) => (q.EmpireBits & 1 << empire.Index) != 0).OrderBy((Quest q) => q.GUID))
				{
					binaryWriter.Write(quest.GUID);
					binaryWriter.Write(quest.QuestDefinition.Category);
					binaryWriter.Write(quest.QuestDefinition.SubCategory);
					binaryWriter.Write(quest.QuestDefinition.Name);
					binaryWriter.Write(quest.QuestState.ToString());
					for (int i = 0; i < quest.QuestRewards.Length; i++)
					{
						string value = string.Empty;
						if (quest.QuestRewards[i].Droppables != null && quest.QuestRewards[i].Droppables.Length != 0)
						{
							value = quest.QuestRewards[i].Droppables.Aggregate(string.Empty, (string current, IDroppable t) => current + t.ToString());
						}
						binaryWriter.Write(quest.QuestRewards[i].StepNumber);
						binaryWriter.Write(value);
					}
				}
			}
		}
		byte[] result = memoryStream.ToArray();
		memoryStream.Close();
		return result;
	}

	public QuestManagerState State { get; private set; }

	public QuestVariable GetQuestVariableByName(StaticString varName)
	{
		QuestVariable questVariable = this.State.QuestVariables.FirstOrDefault((QuestVariable match) => match.Name == varName);
		if (questVariable != null)
		{
			return questVariable;
		}
		return this.State.GlobalQuestVariablesCurrentEmpire.FirstOrDefault((QuestVariable match) => match.Name == varName);
	}

	public bool Trigger(Empire empire, QuestDefinition questDefinition, QuestVariable[] questVariables, QuestInstruction[] pendingInstructions, QuestReward[] questRewards, Dictionary<Region, List<string>> regionQuestLocalizationVariableDefinitionLocalizationKey, IGameEntity questGiver = null, bool canShared = true)
	{
		if (GameManager.Preferences.QuestVerboseMode)
		{
			Diagnostics.Log("[Quest] Triggering quest {0} for empire {1}", new object[]
			{
				questDefinition.Name,
				empire.Index
			});
		}
		if (empire == null)
		{
			throw new ArgumentNullException("empire");
		}
		if (questDefinition == null)
		{
			throw new ArgumentNullException("questDefinition");
		}
		Diagnostics.Assert(questVariables != null, "Array of quest variables is null.");
		Diagnostics.Assert(pendingInstructions != null, "Array of pending instructions is null.");
		Diagnostics.Assert(questRewards != null, "Array of quest rewards is null.");
		for (int i = 0; i < questVariables.Length; i++)
		{
			IEnumerable enumerable = questVariables[i].Object as IEnumerable;
			if (enumerable != null)
			{
				StringBuilder stringBuilder = new StringBuilder();
				List<object> list = enumerable.Cast<object>().ToList<object>();
				stringBuilder.AppendFormat("IEnumerable[{0}] = {{ ", list.Count);
				for (int j = 0; j < list.Count; j++)
				{
					if (list[j] != null)
					{
						stringBuilder.AppendFormat("'{0}', ", list[j].ToString());
					}
				}
				stringBuilder.Append("}};");
			}
		}
		IGameEntityRepositoryService service = base.Game.Services.GetService<IGameEntityRepositoryService>();
		if (service == null)
		{
			Diagnostics.LogError("Unable to retrieve the game entity repository service.");
			return false;
		}
		GameEntityGUID questGUID = service.GenerateGUID();
		Quest quest = new Quest(questGUID, questDefinition, questRewards, (questGiver == null) ? GameEntityGUID.Zero : questGiver.GUID);
		quest.EmpireBits = 1 << empire.Index;
		IQuestRepositoryService service2 = base.Game.Services.GetService<IQuestRepositoryService>();
		service2.Register(quest);
		QuestBehaviour questBehaviour = new QuestBehaviour(quest, empire, questVariables, pendingInstructions, regionQuestLocalizationVariableDefinitionLocalizationKey);
		if (questBehaviour.Root != null)
		{
			bool flag = questBehaviour.Root.Initialize(questBehaviour);
			if (flag)
			{
				questBehaviour.Root.Reset();
				this.questManagementService.Register(questBehaviour);
				if (this.PlayerController != null)
				{
					GameEntityGUID questGiverGUID = (questGiver == null) ? GameEntityGUID.Zero : questGiver.GUID;
					OrderBeginQuest order = new OrderBeginQuest(empire.Index, quest, questGiverGUID);
					this.PlayerController.PostOrder(order);
				}
				this.questManagementService.SendPendingInstructions(questBehaviour);
				this.questManagementService.ExecuteQuest(questBehaviour);
				if (questDefinition.GlobalCooldown > 0)
				{
					int b = base.Game.Turn + Mathf.FloorToInt((float)questDefinition.GlobalCooldown * empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier));
					this.nextPossibleGlobalQuestTurn = Mathf.Max(this.nextPossibleGlobalQuestTurn, b);
				}
				if (canShared && questDefinition.IsGlobal)
				{
					for (int k = 0; k < base.Game.Empires.Length; k++)
					{
						if (base.Game.Empires[k].Index != empire.Index && base.Game.Empires[k] is MajorEmpire)
						{
							QuestReward[] questRewards2 = (from reward in questRewards
							select new QuestReward(reward.StepNumber, reward.Hidden, reward.Droppables, reward.LocalizationKey, reward.JustForShow, reward.MinimumRank)).ToArray<QuestReward>();
							this.Trigger(base.Game.Empires[k], questDefinition, questVariables, pendingInstructions, questRewards2, regionQuestLocalizationVariableDefinitionLocalizationKey, questGiver, false);
						}
					}
				}
				return true;
			}
		}
		else
		{
			Diagnostics.LogError("Quest behaviour is not rooted.");
		}
		return false;
	}

	public bool TryTrigger(out QuestDefinition questDefinition, out QuestVariable[] questVariables, out QuestInstruction[] pendingInstructions, out Dictionary<Region, List<string>> regionQuestLocalizationVariableDefinitionLocalizationKey)
	{
		foreach (QuestDefinition questDefinition2 in this.QuestDefinitions.Shuffle())
		{
			if (questDefinition2.SingleCheckPerTurn)
			{
				if (this.questsCheckedThisTurn.Contains(questDefinition2))
				{
					continue;
				}
				this.questsCheckedThisTurn.Add(questDefinition2);
			}
			this.State.QuestDefinition = questDefinition2;
			questDefinition = questDefinition2;
			if (questDefinition.Root != null)
			{
				if (this.CheckTags(questDefinition, this.State.Tags))
				{
					if (this.CheckRepetition(questDefinition))
					{
						if (this.CheckRandom(questDefinition))
						{
							List<QuestVariable> list = new List<QuestVariable>();
							List<QuestInstruction> list2 = new List<QuestInstruction>();
							regionQuestLocalizationVariableDefinitionLocalizationKey = new Dictionary<Region, List<string>>();
							if (this.CheckVariables(questDefinition.GetPrerequisiteVariables(), questDefinition, list, list2, regionQuestLocalizationVariableDefinitionLocalizationKey))
							{
								if (this.CheckPrerequisites(questDefinition, this.State.Targets))
								{
									if (this.CheckVariables(questDefinition.GetNonPrerequisiteVariables(), questDefinition, list, list2, regionQuestLocalizationVariableDefinitionLocalizationKey))
									{
										if (this.CheckForcedByCheatCommand(questDefinition))
										{
											questVariables = list.ToArray();
											pendingInstructions = list2.ToArray();
											return true;
										}
									}
									else if (GameManager.Preferences.QuestVerboseMode)
									{
										Diagnostics.LogWarning("[Quest] {0} didn't trigger because variables failed", new object[]
										{
											questDefinition2.Name
										});
									}
								}
								else if (GameManager.Preferences.QuestVerboseMode)
								{
									Diagnostics.LogWarning("[Quest] {0} didn't trigger because prerequisites failed", new object[]
									{
										questDefinition2.Name
									});
								}
							}
							else if (GameManager.Preferences.QuestVerboseMode)
							{
								Diagnostics.LogWarning("[Quest] {0} didn't trigger because prerequisite variables failed", new object[]
								{
									questDefinition2.Name
								});
							}
						}
						else if (GameManager.Preferences.QuestVerboseMode)
						{
							Diagnostics.LogWarning("[Quest] {0} didn't trigger because random triggering chance failed", new object[]
							{
								questDefinition2.Name
							});
						}
					}
				}
				else if (GameManager.Preferences.QuestVerboseMode)
				{
					Diagnostics.LogWarning("[Quest] {0} didn't trigger because of tag mismatch (Quest: [{1}] and State: [{2}])", new object[]
					{
						questDefinition2.Name,
						questDefinition2.Tags,
						this.State.Tags
					});
				}
			}
		}
		questDefinition = null;
		questVariables = null;
		pendingInstructions = null;
		regionQuestLocalizationVariableDefinitionLocalizationKey = null;
		return false;
	}

	public bool TryTrigger(out QuestDefinition questDefinition, out QuestVariable[] questVariables, out QuestInstruction[] pendingInstructions, out QuestReward[] questRewards, out Dictionary<Region, List<string>> regionQuestLocalizationVariableDefinitionLocalizationKey)
	{
		if (this.TryTrigger(out questDefinition, out questVariables, out pendingInstructions, out regionQuestLocalizationVariableDefinitionLocalizationKey))
		{
			bool flag = this.CheckRewards(questDefinition, out questRewards);
			if (flag)
			{
				return true;
			}
			if (GameManager.Preferences.QuestVerboseMode)
			{
				Diagnostics.LogWarning("[Quest] {0} didn't trigger because of a failure in rewards", new object[]
				{
					questDefinition
				});
			}
		}
		questRewards = null;
		return false;
	}

	private void CompleteQuestBehaviour(QuestBehaviour questBehaviour, List<ulong> completed)
	{
		this.questManagementService.SendPendingInstructions(questBehaviour);
		if (questBehaviour.State != Amplitude.Unity.AI.BehaviourTree.State.Running && questBehaviour.PendingInstructions.Count == 0 && questBehaviour.DelayedInstructions.Count == 0 && questBehaviour.Quest.QuestState != QuestState.InProgress)
		{
			if (this.PlayerController != null)
			{
				OrderCompleteQuest order = new OrderCompleteQuest(questBehaviour.Quest);
				this.PlayerController.PostOrder(order);
			}
			completed.Add(questBehaviour.Quest.GUID);
			this.CheckForQuestDefinitionTriggers(questBehaviour);
		}
	}

	private void InitStateCommon(Empire empire, WorldPosition worldPosition)
	{
		this.State.Tags.Clear();
		this.State.Targets.Clear();
		this.State.Empire = empire;
		this.State.WorldPosition = worldPosition;
		this.State.AddTargets("$(Empire)", empire);
		this.State.AddTargets("$(Empires)", (from emp in base.Game.Empires
		where emp is MajorEmpire && !(emp as MajorEmpire).IsEliminated
		select emp).ToArray<Empire>());
	}

	private bool RemoveQuestWorldEffect(Empire[] targetEmpires, OrderQuestWorldEffect order)
	{
		bool result = false;
		foreach (Empire empire in targetEmpires)
		{
			if (empire.SimulationObject != null)
			{
				foreach (SimulationObject simulationObject in empire.SimulationObject.Children)
				{
					if (simulationObject.Tags.Contains(order.WorldEffectName))
					{
						result = true;
						empire.SimulationObject.RemoveChild(simulationObject);
						simulationObject.Dispose();
						break;
					}
				}
			}
		}
		return result;
	}

	public int Count
	{
		get
		{
			return this.quests.Count;
		}
	}

	public IGameEntity this[GameEntityGUID guid]
	{
		get
		{
			return this.quests[guid];
		}
	}

	public bool Contains(GameEntityGUID guid)
	{
		return this.quests.ContainsKey(guid);
	}

	public Empire GetEmpireForQuest(GameEntityGUID questGuid)
	{
		QuestBehaviour questBehaviour;
		if (this.questBehaviours.TryGetValue(questGuid, out questBehaviour))
		{
			return questBehaviour.Initiator;
		}
		return null;
	}

	public IEnumerator<KeyValuePair<ulong, Quest>> GetEnumerator()
	{
		return this.quests.GetEnumerator();
	}

	public void Register(Quest quest)
	{
		if (quest == null)
		{
			throw new ArgumentNullException("quest");
		}
		if (quest.QuestDefinition == null)
		{
			Diagnostics.LogError("Quest {0} has no definition", new object[]
			{
				quest.GUID
			});
			return;
		}
		this.quests.Add(quest.GUID, quest);
		this.OnQuestRepositoryChange(QuestRepositoryChangeAction.Add, quest.GUID);
	}

	public bool TryGetValue(GameEntityGUID guid, out Quest gameEntity)
	{
		return this.quests.TryGetValue(guid, out gameEntity);
	}

	public QuestBehaviour GetQuestBehaviour(StaticString questName, int empireIndex)
	{
		return this.questBehaviours.Values.FirstOrDefault((QuestBehaviour questBehaviour) => questBehaviour.Initiator.Index == empireIndex && questBehaviour.Quest.Name == questName);
	}

	public void Unregister(Quest quest)
	{
		if (quest == null)
		{
			throw new ArgumentNullException("quest");
		}
		this.Unregister(quest.GUID);
	}

	public void Unregister(IGameEntity gameEntity)
	{
		if (gameEntity == null)
		{
			throw new ArgumentNullException("gameEntity");
		}
		this.Unregister(gameEntity.GUID);
	}

	public void Unregister(GameEntityGUID guid)
	{
		if (this.quests.Remove(guid))
		{
			this.OnQuestRepositoryChange(QuestRepositoryChangeAction.Remove, guid);
		}
	}

	private void OnQuestRepositoryChange(QuestRepositoryChangeAction action, ulong gameEntityGuid)
	{
		if (this.QuestRepositoryChange != null)
		{
			this.QuestRepositoryChange(this, new QuestRepositoryChangeEventArgs(action, gameEntityGuid));
		}
	}

	public Dictionary<ulong, IDroppableWithRewardAllocation> ArmiesKilledRewards { get; private set; }

	public Dictionary<ulong, Droplist> ArmiesKillRewards
	{
		get
		{
			return this.armiesKillRewards;
		}
	}

	public void AddRewardForArmyKill(GameEntityGUID armyGUID, StaticString droplistName)
	{
		if (this.Droplists != null)
		{
			Droplist value = this.Droplists.GetValue(droplistName);
			if (value == null)
			{
				Diagnostics.LogError("Can't find droplist {0}", new object[]
				{
					droplistName
				});
			}
			else
			{
				this.armiesKillRewards.Add(armyGUID, value);
			}
		}
	}

	private IDroppable PickRewardForArmyKill(GameEntityGUID armyGUID, Empire killerEmpire)
	{
		Droplist droplist = null;
		if (this.armiesKillRewards.TryGetValue(armyGUID, out droplist))
		{
			Droplist droplist2 = null;
			IDroppable result;
			do
			{
				result = droplist.Pick(killerEmpire, out droplist2, new object[0]);
				if (droplist2 != null)
				{
					droplist = droplist2;
				}
			}
			while (droplist2 != null);
			return result;
		}
		return null;
	}

	private void GiveQuestArmyKillReward(Empire killerEmpire, Encounter encounter)
	{
		IEnumerable<Contender> enemiesContenderFromEmpire = encounter.GetEnemiesContenderFromEmpire(killerEmpire);
		foreach (Contender contender in enemiesContenderFromEmpire)
		{
			if (contender.ContenderState == ContenderState.Defeated && contender.Garrison != null)
			{
				IDroppable droppable = this.PickRewardForArmyKill(contender.Garrison.GUID, killerEmpire);
				if (droppable != null)
				{
					IDroppableWithRewardAllocation droppableWithRewardAllocation = droppable as IDroppableWithRewardAllocation;
					if (droppableWithRewardAllocation == null)
					{
						Diagnostics.LogError("Can't reward army {0} with {1} as it is not a IDroppableWithRewardAllocation", new object[]
						{
							contender.Garrison.GUID,
							droppable
						});
					}
					else
					{
						DroppableResource droppableResource = droppableWithRewardAllocation as DroppableResource;
						if (droppableResource == null)
						{
							Diagnostics.LogError("Quest army kill reward other than resources have not been implemented yet");
						}
						else
						{
							OrderTransferResources order = new OrderTransferResources(killerEmpire.Index, droppableResource.ResourceName, (float)droppableResource.Quantity, 0UL);
							this.PlayerController.PostOrder(order);
							this.armiesKillRewards.Remove(contender.Garrison.GUID);
							this.ArmiesKilledRewards.Add(contender.Garrison.GUID, droppableWithRewardAllocation);
							encounter.RegisterRewards(killerEmpire.Index, droppableResource);
							global::IGuiService service = Services.GetService<global::IGuiService>();
							if (service != null)
							{
								NotificationPanelEncounterEnded guiPanel = service.GetGuiPanel<NotificationPanelEncounterEnded>();
								if (guiPanel != null)
								{
									guiPanel.RefreshContent();
								}
							}
						}
					}
				}
			}
		}
	}

	private XmlSerializer XmlSerializer { get; set; }

	public virtual void ReadXml(XmlReader reader)
	{
		this.TurnWhenLastBegun = reader.GetAttribute<int>("TurnWhenLastBegun");
		reader.ReadStartElement();
		ISessionService service = Services.GetService<ISessionService>();
		Diagnostics.Assert(service != null);
		IGameEntityRepositoryService service2 = base.Game.GetService<IGameEntityRepositoryService>();
		Diagnostics.Assert(service2 != null);
		if (service.Session.IsHosting)
		{
			int attribute = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("QuestBehaviours");
			try
			{
				QuestBehaviour.XmlSerializer = this.CreateXmlSerializerForTypeofQuestBehaviour();
				for (int i = 0; i < attribute; i++)
				{
					ulong attribute2 = reader.GetAttribute<ulong>("QuestGUID");
					QuestBehaviour questBehaviour = reader.ReadElementSerializable<QuestBehaviour>("QuestBehaviour");
					if (questBehaviour != null && questBehaviour.Root != null)
					{
						bool flag = true;
						if (flag)
						{
							this.questBehaviours.Add(attribute2, questBehaviour);
						}
						else
						{
							Diagnostics.LogWarning("Quest#{0}'s questBehaviour initialization failed, it will be ignored.", new object[]
							{
								attribute2
							});
						}
					}
				}
			}
			catch
			{
				throw;
			}
			finally
			{
				QuestBehaviour.XmlSerializer = null;
			}
			reader.ReadEndElement("QuestBehaviours");
		}
		else
		{
			reader.Skip("QuestBehaviours");
			Diagnostics.Log("[XML] Skipping <QuestBehaviours>");
		}
		int attribute3 = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("QuestMarkers");
		for (int j = 0; j < attribute3; j++)
		{
			QuestMarker questMarker = reader.ReadElementSerializable<QuestMarker>();
			if (questMarker != null)
			{
				List<QuestMarker> list;
				if (!this.questMarkers.TryGetValue(questMarker.BoundTargetGUID, out list))
				{
					list = new List<QuestMarker>();
					this.questMarkers.Add(questMarker.BoundTargetGUID, list);
				}
				list.Add(questMarker);
			}
		}
		reader.ReadEndElement("QuestMarkers");
		if (service.Session.IsHosting)
		{
			int attribute4 = reader.GetAttribute<int>("Count");
			this.nextPossibleGlobalQuestTurn = reader.GetAttribute<int>("LastGlobalQuestOccurenceTurn");
			reader.ReadStartElement("QuestOccurences");
			for (int k = 0; k < attribute4; k++)
			{
				StaticString key = reader.GetAttribute("QuestDefinitionName");
				QuestOccurence questOccurence = reader.ReadElementSerializable<QuestOccurence>("QuestOccurence");
				if (questOccurence != null)
				{
					this.questOccurences.Add(key, questOccurence);
				}
			}
			reader.ReadEndElement("QuestOccurences");
		}
		else
		{
			reader.Skip("QuestOccurences");
			Diagnostics.Log("[XML] Skipping <QuestOccurences>");
		}
		if (service.Session.IsHosting)
		{
			if (reader.IsStartElement("QuestTargetsLocked"))
			{
				int attribute5 = reader.GetAttribute<int>("Count");
				reader.ReadStartElement("QuestTargetsLocked");
				for (int l = 0; l < attribute5; l++)
				{
					if (!this.questTargetsLocked.ContainsKey(l))
					{
						this.questTargetsLocked.Add(l, new List<QuestTarget>());
					}
					int attribute6 = reader.GetAttribute<int>("Count");
					reader.ReadStartElement("QuestTargetsLockedByEmpire");
					for (int m = 0; m < attribute6; m++)
					{
						Type type = null;
						try
						{
							string attribute7 = reader.GetAttribute("AssemblyQualifiedName");
							type = Type.GetType(attribute7);
						}
						catch
						{
						}
						reader.ReadStartElement("QuestTarget");
						if (type != null)
						{
							QuestTarget questTarget = Activator.CreateInstance(type, true) as QuestTarget;
							if (questTarget != null)
							{
								questTarget.ReadXml(reader);
								this.questTargetsLocked[l].Add(questTarget);
							}
						}
						if (type != null)
						{
							reader.ReadEndElement();
						}
					}
					if (attribute6 > 0)
					{
						reader.ReadEndElement("QuestTargetsLockedByEmpire");
					}
				}
				reader.ReadEndElement("QuestTargetsLocked");
			}
		}
		else
		{
			reader.Skip("QuestTargetsLocked");
			Diagnostics.Log("[XML] Skipping <QuestTargetsLocked>");
		}
		if (service.Session.IsHosting)
		{
			if (reader.IsStartElement("QuestGlobalVariables"))
			{
				int attribute8 = reader.GetAttribute<int>("Count");
				reader.ReadStartElement("QuestGlobalVariables");
				for (int n = 0; n < attribute8; n++)
				{
					int attribute9 = reader.GetAttribute<int>("EmpireIndex");
					int attribute10 = reader.GetAttribute<int>("Count");
					reader.ReadStartElement("GlobalVariables");
					List<QuestVariable> list2 = new List<QuestVariable>();
					for (int num = 0; num < attribute10; num++)
					{
						reader.ReadStartElement("QuestVariable");
						string name = reader.ReadElementString("VariableName");
						GameEntityGUID guid = reader.ReadElementString<ulong>("GUID");
						IGameEntity target = null;
						if (!service2.TryGetValue(guid, out target))
						{
							list2.Add(new QuestVariable(name, guid));
						}
						else
						{
							list2.Add(new QuestVariable(name, target));
						}
						reader.ReadEndElement("QuestVariable");
					}
					this.State.GlobalQuestVariables.Add(attribute9, list2);
					if (attribute10 > 0)
					{
						reader.ReadEndElement("GlobalVariables");
					}
				}
				if (attribute8 > 0)
				{
					reader.ReadEndElement("QuestGlobalVariables");
				}
			}
		}
		else
		{
			reader.Skip("QuestGlobalVariables");
		}
		if (service.Session.IsHosting)
		{
			if (reader.IsStartElement("QuestRewardRepository"))
			{
				int attribute11 = reader.GetAttribute<int>("Count");
				reader.ReadStartElement("QuestRewardRepository");
				for (int num2 = 0; num2 < attribute11; num2++)
				{
					reader.ReadStartElement("GlobalReward");
					ulong x = reader.ReadElementString<ulong>("GUID");
					string x2 = reader.ReadElementString("DroplistName");
					this.AddRewardForArmyKill(x, x2);
					reader.ReadEndElement("GlobalReward");
				}
				if (attribute11 > 0)
				{
					reader.ReadEndElement("QuestRewardRepository");
				}
			}
		}
		else
		{
			reader.Skip("QuestRewardRepository");
		}
		if (reader.IsStartElement("QuestWorldEffects"))
		{
			int attribute12 = reader.GetAttribute<int>("Count");
			this.questWorldEffectOrders = new OrderQuestWorldEffect[attribute12];
			reader.ReadStartElement("QuestWorldEffects");
			for (int num3 = 0; num3 < attribute12; num3++)
			{
				int attribute13 = reader.GetAttribute<int>("AffectedEmpire");
				int attribute14 = reader.GetAttribute<int>("Duration");
				reader.ReadStartElement("WorldEffect");
				StaticString x3 = reader.ReadElementString("Name");
				this.questWorldEffectOrders[num3] = new OrderQuestWorldEffect(x3, true, true, attribute13, attribute14);
				reader.ReadEndElement();
			}
			reader.ReadEndElement();
		}
	}

	public virtual void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString("AssemblyQualifiedName", base.GetType().AssemblyQualifiedName);
		writer.WriteAttributeString<int>("TurnWhenLastBegun", this.TurnWhenLastBegun);
		writer.WriteStartElement("QuestBehaviours");
		try
		{
			QuestBehaviour.XmlSerializer = this.CreateXmlSerializerForTypeofQuestBehaviour();
			writer.WriteAttributeString<int>("Count", this.questBehaviours.Count);
			foreach (KeyValuePair<ulong, QuestBehaviour> keyValuePair in this.questBehaviours)
			{
				Amplitude.Xml.Serialization.IXmlSerializable value = keyValuePair.Value;
				writer.WriteElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>(ref value);
			}
		}
		catch
		{
			throw;
		}
		finally
		{
			QuestBehaviour.XmlSerializer = null;
		}
		writer.WriteEndElement();
		writer.WriteStartElement("QuestMarkers");
		int num = 0;
		foreach (List<QuestMarker> list in this.questMarkers.Values)
		{
			num += list.Count;
		}
		writer.WriteAttributeString<int>("Count", num);
		foreach (List<QuestMarker> list2 in this.questMarkers.Values)
		{
			for (int i = 0; i < list2.Count; i++)
			{
				Amplitude.Xml.Serialization.IXmlSerializable xmlSerializable = list2[i];
				writer.WriteElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>(ref xmlSerializable);
			}
		}
		writer.WriteEndElement();
		writer.WriteStartElement("QuestOccurences");
		writer.WriteAttributeString<int>("Count", this.questOccurences.Count);
		writer.WriteAttributeString<int>("LastGlobalQuestOccurenceTurn", this.nextPossibleGlobalQuestTurn);
		foreach (KeyValuePair<StaticString, QuestOccurence> keyValuePair2 in this.questOccurences)
		{
			writer.WriteStartElement("QuestOccurence");
			writer.WriteAttributeString("QuestDefinitionName", keyValuePair2.Key.ToString());
			((Amplitude.Xml.Serialization.IXmlSerializable)keyValuePair2.Value).WriteXml(writer);
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
		writer.WriteStartElement("QuestTargetsLocked");
		writer.WriteAttributeString<int>("Count", this.questTargetsLocked.Count);
		foreach (KeyValuePair<int, List<QuestTarget>> keyValuePair3 in this.questTargetsLocked)
		{
			writer.WriteStartElement("QuestTargetsLockedByEmpire");
			List<QuestTarget> value2 = keyValuePair3.Value;
			writer.WriteAttributeString<int>("Count", value2.Count);
			for (int j = 0; j < value2.Count; j++)
			{
				writer.WriteStartElement("QuestTarget");
				value2[j].WriteXml(writer);
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
		writer.WriteStartElement("QuestGlobalVariables");
		writer.WriteAttributeString<int>("Count", this.State.GlobalQuestVariables.Count);
		foreach (KeyValuePair<int, List<QuestVariable>> keyValuePair4 in this.State.GlobalQuestVariables)
		{
			writer.WriteStartElement("GlobalVariables");
			List<QuestVariable> value3 = keyValuePair4.Value;
			writer.WriteAttributeString<int>("EmpireIndex", keyValuePair4.Key);
			writer.WriteAttributeString<int>("Count", value3.Count);
			for (int k = 0; k < value3.Count; k++)
			{
				QuestVariable questVariable = value3[k];
				IGameEntity gameEntity = questVariable.Object as IGameEntity;
				GameEntityGUID guid;
				if (gameEntity != null)
				{
					guid = gameEntity.GUID;
				}
				else
				{
					try
					{
						guid = (ulong)questVariable.Object;
					}
					catch
					{
						guid = GameEntityGUID.Zero;
						Diagnostics.LogWarning("[Quest] Global variable {0} of empire {1} needs a GUI to be serialized", new object[]
						{
							value3[k].Name,
							keyValuePair4.Key
						});
					}
				}
				writer.WriteStartElement("QuestVariable");
				writer.WriteElementString("VariableName", questVariable.Name);
				writer.WriteElementString<ulong>("GUID", guid);
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
		writer.WriteStartElement("QuestRewardRepository");
		writer.WriteAttributeString<int>("Count", this.armiesKillRewards.Count);
		foreach (KeyValuePair<ulong, Droplist> keyValuePair5 in this.armiesKillRewards)
		{
			writer.WriteStartElement("GlobalReward");
			writer.WriteElementString<ulong>("GUID", keyValuePair5.Key);
			writer.WriteElementString<StaticString>("DroplistName", keyValuePair5.Value.Name);
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
		writer.WriteStartElement("QuestWorldEffects");
		bool flag = this.endTurnController.LastGameClientState.ToString() == "GameClientState_Turn_Dump_Finished";
		writer.WriteAttributeString<int>("Count", this.questWorldEffects.Count);
		foreach (KeyValuePair<StaticString, KeyValuePair<int, List<SimulationObject>>> keyValuePair6 in this.questWorldEffects)
		{
			writer.WriteStartElement("WorldEffect");
			int num2 = -1;
			SimulationObject simulationObject = keyValuePair6.Value.Value[0];
			if (simulationObject != null && simulationObject.Tags.Contains("ClassTimedBonus"))
			{
				num2 = Mathf.FloorToInt(keyValuePair6.Value.Value[0].GetPropertyBaseValue(SimulationProperties.RemainingTime));
				if (flag)
				{
					num2--;
				}
			}
			writer.WriteAttributeString<int>("AffectedEmpire", keyValuePair6.Value.Key);
			writer.WriteAttributeString<int>("Duration", num2);
			writer.WriteElementString<StaticString>("Name", keyValuePair6.Key);
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
	}

	private XmlSerializer CreateXmlSerializerForTypeofQuestBehaviour()
	{
		if (this.XmlSerializer != null)
		{
			return this.XmlSerializer;
		}
		Database<QuestDefinition> database = Databases.GetDatabase<QuestDefinition>(false) as Database<QuestDefinition>;
		if (database != null)
		{
			XmlExtraType[] extraTypes = null;
			XmlAttributeOverride[] overrides = database.Overrides;
			Type[] extraTypes2 = this.GenerateExtraTypesForTypeofQuestBehaviour(extraTypes);
			XmlAttributeOverrides overrides2 = this.GenerateOverridesForTypeofQuestBehaviour(overrides, extraTypes);
			XmlRootAttribute root = new XmlRootAttribute
			{
				ElementName = "Root",
				Namespace = string.Empty
			};
			this.XmlSerializer = new XmlSerializer(typeof(BehaviourTreeNode), overrides2, extraTypes2, root, string.Empty);
			return this.XmlSerializer;
		}
		return null;
	}

	private Type[] GenerateExtraTypesForTypeofQuestBehaviour(XmlExtraType[] extraTypes)
	{
		List<Type> list = new List<Type>();
		MemberInfo[] member = typeof(QuestDefinition).GetMember("Root", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (member != null)
		{
			object[] customAttributes = member[0].GetCustomAttributes(typeof(XmlElementAttribute), true);
			if (customAttributes != null)
			{
				for (int i = 0; i < customAttributes.Length; i++)
				{
					Type type = ((XmlElementAttribute)customAttributes[i]).Type;
					list.Add(type);
				}
			}
		}
		return list.ToArray();
	}

	private XmlAttributeOverrides GenerateOverridesForTypeofQuestBehaviour(XmlAttributeOverride[] overrides, XmlExtraType[] extraTypes)
	{
		XmlAttributeOverrides xmlAttributeOverrides = new XmlAttributeOverrides();
		XmlAttributes xmlAttributes = new XmlAttributes();
		string text = string.Empty;
		Type type = typeof(QuestBehaviour);
		if (overrides != null)
		{
			foreach (XmlAttributeOverride xmlAttributeOverride in overrides)
			{
				if (xmlAttributeOverride.DataType != null)
				{
					if (!string.IsNullOrEmpty(xmlAttributeOverride.Name))
					{
						if (xmlAttributeOverride.ExtraTypes != null)
						{
							xmlAttributes = new XmlAttributes();
							MemberInfo[] member = xmlAttributeOverride.DataType.GetMember(xmlAttributeOverride.Name);
							if (member != null)
							{
								object[] customAttributes = member[0].GetCustomAttributes(typeof(XmlElementAttribute), true);
								if (customAttributes != null)
								{
									for (int j = 0; j < customAttributes.Length; j++)
									{
										xmlAttributes.XmlElements.Add((XmlElementAttribute)customAttributes[j]);
									}
								}
							}
							for (int k = 0; k < xmlAttributeOverride.ExtraTypes.Length; k++)
							{
								type = xmlAttributeOverride.ExtraTypes[k].DataType;
								XmlTypeAttribute xmlTypeAttribute = Attribute.GetCustomAttribute(type, typeof(XmlTypeAttribute)) as XmlTypeAttribute;
								if (xmlTypeAttribute != null)
								{
									text = xmlTypeAttribute.TypeName;
								}
								else if (!string.IsNullOrEmpty(xmlAttributeOverride.ExtraTypes[k].Name))
								{
									text = xmlAttributeOverride.ExtraTypes[k].Name;
								}
								else
								{
									text = type.Name;
								}
								xmlAttributes.XmlElements.Add(new XmlElementAttribute(text, type));
							}
							type = xmlAttributeOverride.DataType;
							text = xmlAttributeOverride.Name;
							if (string.IsNullOrEmpty(text))
							{
								XmlTypeAttribute xmlTypeAttribute2 = Attribute.GetCustomAttribute(type, typeof(XmlTypeAttribute)) as XmlTypeAttribute;
								if (xmlTypeAttribute2 != null)
								{
									text = xmlTypeAttribute2.TypeName;
								}
								else
								{
									text = type.Name;
								}
							}
							xmlAttributeOverrides.Add(type, text, xmlAttributes);
							if (member != null)
							{
								object[] customAttributes2 = member[0].GetCustomAttributes(typeof(XmlElementAttribute), true);
								if (customAttributes2 != null)
								{
									for (int l = 0; l < customAttributes2.Length; l++)
									{
										type = ((XmlElementAttribute)customAttributes2[l]).Type;
										xmlAttributeOverrides.Add(type, text, xmlAttributes);
									}
								}
							}
						}
					}
				}
			}
		}
		return xmlAttributeOverrides;
	}

	private IEnumerable<Region> QueryAllRegions(StaticString keyword)
	{
		Diagnostics.Assert(base.Game != null);
		Diagnostics.Assert(base.Game.World != null);
		Diagnostics.Assert(base.Game.World.Atlas != null);
		Diagnostics.Assert(base.Game.World.Regions != null);
		List<Region> regions = new List<Region>(base.Game.World.Regions);
		regions = regions.Randomize(null);
		for (int regionIndex = 0; regionIndex < regions.Count; regionIndex++)
		{
			if (this.IsQuestTargetFree(new QuestTargetRegion(regions[regionIndex]), this.State.Empire))
			{
				yield return regions[regionIndex];
			}
		}
		yield break;
	}

	private IEnumerable<Region> QueryCurrentRegion(StaticString keyword)
	{
		Diagnostics.Assert(base.Game != null);
		Diagnostics.Assert(base.Game.World != null);
		Diagnostics.Assert(base.Game.World.Atlas != null);
		Diagnostics.Assert(base.Game.World.Regions != null);
		if (this.State.WorldPosition.IsValid)
		{
			GridMap<short> regions = (GridMap<short>)base.Game.World.Atlas.GetMap(WorldAtlas.Maps.Regions);
			int regionIndex = (int)regions.GetValue(this.State.WorldPosition);
			if (this.IsQuestTargetFree(new QuestTargetRegion(base.Game.World.Regions[regionIndex]), this.State.Empire))
			{
				yield return base.Game.World.Regions[regionIndex];
			}
		}
		yield break;
	}

	private IEnumerable<Region> QueryCurrentRegionWithNeighbourRegions(StaticString keyword)
	{
		IEnumerable<Region> currentEnumerable = this.QueryCurrentRegion(keyword);
		if (currentEnumerable != null)
		{
			foreach (Region region in currentEnumerable)
			{
				if (region != null)
				{
					yield return region;
				}
			}
		}
		currentEnumerable = this.QueryCurrentRegion(keyword);
		if (currentEnumerable != null)
		{
			foreach (Region region2 in currentEnumerable)
			{
				if (region2 != null)
				{
					yield return region2;
				}
			}
		}
		yield break;
	}

	private IEnumerable<PointOfInterest> QueryCurrentTargetPointsOfInterest(StaticString keyword)
	{
		PointOfInterest pointOfInterest;
		if (this.TryGetTarget<PointOfInterest>("$(Target)", out pointOfInterest) && this.IsQuestTargetFree(new QuestTargetGameEntity(pointOfInterest), this.State.Empire))
		{
			yield return pointOfInterest;
		}
		yield break;
	}

	private IEnumerable<Village> QueryCurrentTargetVillage(StaticString keyword)
	{
		PointOfInterest pointOfInterest;
		if (this.TryGetTarget<PointOfInterest>("$(Target)", out pointOfInterest) && pointOfInterest.Region.MinorEmpire != null)
		{
			BarbarianCouncil barbarianCouncil = pointOfInterest.Region.MinorEmpire.GetAgency<BarbarianCouncil>();
			if (barbarianCouncil != null && barbarianCouncil.Villages != null)
			{
				foreach (Village village in barbarianCouncil.Villages)
				{
					if (village.PointOfInterest == pointOfInterest)
					{
						if (this.IsQuestTargetFree(new QuestTargetGameEntity(village), this.State.Empire))
						{
							yield return village;
						}
						break;
					}
				}
			}
		}
		yield break;
	}

	private IEnumerable<object> QueryQuestVariable(StaticString keyword)
	{
		if (this.State.QuestVariables != null)
		{
			QuestVariable questVariable = this.State.QuestVariables.FirstOrDefault((QuestVariable match) => match.Name == keyword);
			if (questVariable != null)
			{
				IEnumerable enumerable = questVariable.Object as IEnumerable;
				if (enumerable != null)
				{
					foreach (object obj in enumerable)
					{
						yield return obj;
					}
				}
				else
				{
					yield return questVariable.Object;
				}
			}
		}
		yield break;
	}

	private IEnumerable<Region> QueryCurrentNeighbourRegions(StaticString keyword)
	{
		Diagnostics.Assert(base.Game != null);
		Diagnostics.Assert(base.Game.World != null);
		Diagnostics.Assert(base.Game.World.Atlas != null);
		Diagnostics.Assert(base.Game.World.Regions != null);
		if (this.State.WorldPosition.IsValid)
		{
			GridMap<short> regions = (GridMap<short>)base.Game.World.Atlas.GetMap(WorldAtlas.Maps.Regions);
			int regionIndex = (int)regions.GetValue(this.State.WorldPosition);
			Region region = base.Game.World.Regions[regionIndex];
			foreach (Region.Border border in region.Borders)
			{
				if (border.NeighbourRegionIndex > 0 && border.NeighbourRegionIndex < base.Game.World.Regions.Length && this.IsQuestTargetFree(new QuestTargetRegion(base.Game.World.Regions[border.NeighbourRegionIndex]), this.State.Empire))
				{
					yield return base.Game.World.Regions[border.NeighbourRegionIndex];
				}
			}
		}
		yield break;
	}

	private IEnumerable<Empire> QueryEmpires(StaticString keyword)
	{
		Diagnostics.Assert(base.Game != null);
		Diagnostics.Assert(base.Game.Empires != null);
		foreach (Empire empire in base.Game.Empires)
		{
			if (this.IsQuestTargetFree(new QuestTargetEmpire(empire), this.State.Empire))
			{
				yield return empire;
			}
		}
		yield break;
	}

	private IEnumerable<MajorEmpire> QueryCurrentEmpire(StaticString keyword)
	{
		MajorEmpire currentEmpire;
		if (this.TryGetTarget<MajorEmpire>("$(Empire)", out currentEmpire) && this.IsQuestTargetFree(new QuestTargetEmpire(currentEmpire), this.State.Empire))
		{
			yield return currentEmpire;
		}
		yield break;
	}

	private IEnumerable<SimulationObjectWrapper> QueryCurrentInstigator(StaticString keyword)
	{
		SimulationObjectWrapper currentInstigator;
		if (this.TryGetTarget<SimulationObjectWrapper>("$(Instigator)", out currentInstigator))
		{
			yield return currentInstigator;
		}
		yield break;
	}

	private IEnumerable<MajorEmpire> QueryOtherMajorEmpires(StaticString keyword)
	{
		MajorEmpire currentEmpire;
		if (this.TryGetTarget<MajorEmpire>("$(Empire)", out currentEmpire))
		{
			IEnumerable<Empire> empires = this.QueryEmpires(keyword);
			foreach (Empire empire in empires)
			{
				if (empire.Index != currentEmpire.Index)
				{
					MajorEmpire majorEmpire = empire as MajorEmpire;
					if (majorEmpire != null)
					{
						yield return majorEmpire;
					}
				}
			}
		}
		yield break;
	}

	private IEnumerable<StaticString> QueryBiomeTypeNameFromPosition(WorldPosition position)
	{
		Diagnostics.Assert(position.IsValid);
		byte biomeType = this.WorldPositionningService.GetBiomeType(position);
		StaticString biomeTypeName = this.WorldPositionningService.GetBiomeTypeMappingName(biomeType);
		yield return biomeTypeName;
		yield break;
	}

	private IEnumerable<City> QueryCitiesFromEmpire(MajorEmpire empire)
	{
		Diagnostics.Assert(empire != null);
		DepartmentOfTheInterior departmentOfTheInterior = empire.GetAgency<DepartmentOfTheInterior>();
		Diagnostics.Assert(departmentOfTheInterior != null);
		ReadOnlyCollection<City> cities = departmentOfTheInterior.Cities;
		if (cities != null)
		{
			for (int index = 0; index < cities.Count; index++)
			{
				if (this.IsQuestTargetFree(new QuestTargetGameEntity(cities[index]), this.State.Empire) && !cities[index].SimulationObject.Tags.Contains(City.TagCityStatusRazed))
				{
					yield return cities[index];
				}
			}
		}
		yield break;
	}

	private IEnumerable<City> QueryCityFromRegion(Region region)
	{
		Diagnostics.Assert(region != null);
		if (region.City != null && this.IsQuestTargetFree(new QuestTargetGameEntity(region.City), this.State.Empire) && !region.City.SimulationObject.Tags.Contains(City.TagCityStatusRazed))
		{
			yield return region.City;
		}
		yield break;
	}

	private IEnumerable<ConstructibleElement> QueryAvailableCityImprovementsFromCity(City city)
	{
		Diagnostics.Assert(city != null);
		DepartmentOfIndustry departmentOfIndustry = city.Empire.GetAgency<DepartmentOfIndustry>();
		Diagnostics.Assert(departmentOfIndustry != null);
		IEnumerable<DepartmentOfIndustry.ConstructibleElement> constructibleElements = departmentOfIndustry.ConstructibleElementDatabase.GetAvailableConstructibleElementsAsEnumerable(new StaticString[0]);
		List<StaticString> lastFailureFlags = new List<StaticString>();
		foreach (DepartmentOfIndustry.ConstructibleElement constructibleElement in constructibleElements)
		{
			if (constructibleElement.Category == CityImprovementDefinition.ReadOnlyCategory)
			{
				lastFailureFlags.Clear();
				departmentOfIndustry.GetConstructibleState(city, constructibleElement, ref lastFailureFlags);
				if (!lastFailureFlags.Contains(ConstructionFlags.Discard))
				{
					yield return constructibleElement;
				}
			}
		}
		yield break;
	}

	private IEnumerable<string> QueryFactionNameFromMinorEmpire(MinorEmpire minorEmpire)
	{
		Diagnostics.Assert(minorEmpire != null);
		Diagnostics.Assert(minorEmpire.Faction != null);
		yield return minorEmpire.Faction.Name;
		yield break;
	}

	private IEnumerable<string> QueryFactionNameFromNavalEmpire(NavalEmpire navalEmpire)
	{
		Diagnostics.Assert(navalEmpire != null);
		Diagnostics.Assert(navalEmpire.Faction != null);
		yield return navalEmpire.Faction.Name;
		yield break;
	}

	private IEnumerable<Unit> QueryHeroesFromEmpire(MajorEmpire majorEmpire)
	{
		Diagnostics.Assert(majorEmpire != null);
		DepartmentOfEducation departmentOfEducation = majorEmpire.GetAgency<DepartmentOfEducation>();
		Diagnostics.Assert(departmentOfEducation != null);
		for (int heroIndex = 0; heroIndex < departmentOfEducation.Heroes.Count; heroIndex++)
		{
			if (this.IsQuestTargetFree(new QuestTargetGameEntity(departmentOfEducation.Heroes[heroIndex]), this.State.Empire))
			{
				yield return departmentOfEducation.Heroes[heroIndex];
			}
		}
		yield break;
	}

	private IEnumerable<string> QueryNameFromCityImprovementDefinition(CityImprovementDefinition cityImprovementDefinition)
	{
		Diagnostics.Assert(cityImprovementDefinition != null);
		yield return cityImprovementDefinition.Name;
		yield break;
	}

	private IEnumerable<Region> QueryNeighbourRegionsFromRegion(Region region)
	{
		Diagnostics.Assert(base.Game != null);
		Diagnostics.Assert(base.Game.World != null);
		Diagnostics.Assert(base.Game.World.Atlas != null);
		Diagnostics.Assert(base.Game.World.Regions != null);
		foreach (Region.Border border in region.Borders)
		{
			if (border.NeighbourRegionIndex > 0 && border.NeighbourRegionIndex < base.Game.World.Regions.Length && this.IsQuestTargetFree(new QuestTargetRegion(base.Game.World.Regions[border.NeighbourRegionIndex]), this.State.Empire))
			{
				yield return base.Game.World.Regions[border.NeighbourRegionIndex];
			}
		}
		yield break;
	}

	private IEnumerable<MajorEmpire> QueryMajorEmpireFromRegion(Region region)
	{
		Diagnostics.Assert(region != null);
		if (region.City != null && !region.City.SimulationObject.Tags.Contains(City.TagCityStatusRazed))
		{
			Diagnostics.Assert(region.City.Empire != null);
			MajorEmpire majorEmpire = region.City.Empire as MajorEmpire;
			Diagnostics.Assert(majorEmpire != null);
			if (this.IsQuestTargetFree(new QuestTargetEmpire(majorEmpire), this.State.Empire))
			{
				yield return majorEmpire;
			}
		}
		yield break;
	}

	private IEnumerable<MinorEmpire> QueryMinorEmpireFromRegion(Region region)
	{
		Diagnostics.Assert(region != null);
		if (region.MinorEmpire != null && this.IsQuestTargetFree(new QuestTargetEmpire(region.MinorEmpire), this.State.Empire))
		{
			yield return region.MinorEmpire;
		}
		yield break;
	}

	private IEnumerable<MinorEmpire> QueryMinorEmpireFromVillage(Village village)
	{
		Diagnostics.Assert(village != null);
		Diagnostics.Assert(village.MinorEmpire != null);
		if (this.IsQuestTargetFree(new QuestTargetEmpire(village.MinorEmpire), this.State.Empire))
		{
			yield return village.MinorEmpire;
		}
		yield break;
	}

	private IEnumerable<NavalEmpire> QueryNavalEmpireFromRegion(Region region)
	{
		Diagnostics.Assert(region != null);
		if (region.NavalEmpire != null && this.IsQuestTargetFree(new QuestTargetEmpire(region.NavalEmpire), this.State.Empire))
		{
			yield return region.NavalEmpire;
		}
		yield break;
	}

	private IEnumerable<NavalEmpire> QueryNavalEmpireFromFortress(Fortress fortress)
	{
		Diagnostics.Assert(fortress != null);
		Diagnostics.Assert(fortress.NavalEmpire != null);
		if (this.IsQuestTargetFree(new QuestTargetEmpire(fortress.NavalEmpire), this.State.Empire))
		{
			yield return fortress.NavalEmpire;
		}
		yield break;
	}

	private IEnumerable<string> QueryLocalizedNameFromCityImprovementDefinition(CityImprovementDefinition cityImprovementDefinition)
	{
		Diagnostics.Assert(cityImprovementDefinition != null);
		string localizedName = string.Empty;
		IDatabase<GuiElement> guiElementDatabase = Databases.GetDatabase<GuiElement>(false);
		GuiElement guiElement;
		if (guiElementDatabase != null && guiElementDatabase.TryGetValue(cityImprovementDefinition.Name, out guiElement))
		{
			localizedName = guiElement.Title;
		}
		if (string.IsNullOrEmpty(localizedName))
		{
			yield return cityImprovementDefinition.Name;
		}
		else
		{
			yield return localizedName;
		}
		yield break;
	}

	private IEnumerable<string> QueryLocalizedNameFromCity(City city)
	{
		Diagnostics.Assert(city != null);
		Diagnostics.Assert(city.Region != null);
		yield return city.Region.LocalizedName;
		yield break;
	}

	private IEnumerable<string> QueryLocalizedNameFromFortress(Fortress fortress)
	{
		Diagnostics.Assert(fortress != null);
		Diagnostics.Assert(fortress.Region != null);
		yield return fortress.LocalizedName;
		yield break;
	}

	private IEnumerable<string> QueryLocalizedNameFromDroppableInteger(DroppableInteger droppableInteger)
	{
		Diagnostics.Assert(droppableInteger != null);
		yield return droppableInteger.Value.ToString();
		yield break;
	}

	private IEnumerable<string> QueryLocalizedNameFromDroppableResource(DroppableResource droppableResource)
	{
		Diagnostics.Assert(droppableResource != null);
		yield return droppableResource.ToGuiString();
		yield break;
	}

	private IEnumerable<string> QueryLocalizedNameFromRegion(Region region)
	{
		Diagnostics.Assert(region != null);
		yield return region.LocalizedName;
		yield break;
	}

	private IEnumerable<string> QueryLocalizedNameFromDroppableString(DroppableString droppableString)
	{
		Diagnostics.Assert(droppableString != null);
		yield return droppableString.Value;
		yield break;
	}

	private IEnumerable<string> QueryLocalizedNameFromMinorEmpire(MinorEmpire minorEmpire)
	{
		Diagnostics.Assert(minorEmpire != null);
		Diagnostics.Assert(minorEmpire.Faction != null);
		yield return minorEmpire.Faction.Name;
		yield break;
	}

	private IEnumerable<string> QueryLocalizedNameFromNavalEmpire(NavalEmpire navalEmpire)
	{
		Diagnostics.Assert(navalEmpire != null);
		Diagnostics.Assert(navalEmpire.Faction != null);
		yield return navalEmpire.Faction.Name;
		yield break;
	}

	private IEnumerable<string> QueryLocalizedNameFromMajorEmpire(MajorEmpire majorEmpire)
	{
		Diagnostics.Assert(majorEmpire != null);
		string result = majorEmpire.LocalizedName;
		string hexaColor;
		AgeUtils.ColorToHexaKey(majorEmpire.Color, out hexaColor);
		result = hexaColor + result + "#REVERT#";
		yield return result;
		yield break;
	}

	private IEnumerable<string> QueryLocalizedNameFromPointOfInterest(PointOfInterest pointOfInterest)
	{
		Diagnostics.Assert(pointOfInterest != null);
		Diagnostics.Assert(pointOfInterest.PointOfInterestDefinition != null);
		Diagnostics.Assert(pointOfInterest.PointOfInterestDefinition.PointOfInterestTemplate != null);
		string localizedName = string.Empty;
		IDatabase<GuiElement> guiElementDatabase = Databases.GetDatabase<GuiElement>(false);
		Diagnostics.Assert(guiElementDatabase != null);
		string guiElementName = pointOfInterest.PointOfInterestDefinition.PointOfInterestTemplate.Name;
		string pointOfInterestDefinitionType;
		string resourceName;
		if (pointOfInterest.PointOfInterestDefinition.TryGetValue("Type", out pointOfInterestDefinitionType) && pointOfInterestDefinitionType == "ResourceDeposit" && pointOfInterest.PointOfInterestDefinition.TryGetValue("ResourceName", out resourceName))
		{
			guiElementName = resourceName;
		}
		GuiElement guiElement;
		if (guiElementDatabase.TryGetValue(guiElementName, out guiElement))
		{
			localizedName = guiElement.Title;
		}
		if (string.IsNullOrEmpty(localizedName))
		{
			Diagnostics.LogWarning("GuiElement '{0}' is missing.", new object[]
			{
				guiElementName
			});
			yield return guiElementName;
		}
		else
		{
			yield return localizedName;
		}
		yield break;
	}

	private IEnumerable<string> QueryLocalizedNameFromStaticString(StaticString staticString)
	{
		Diagnostics.Assert(staticString != null);
		string localizedName = staticString.ToString();
		IDatabase<GuiElement> guiElementDatabase = Databases.GetDatabase<GuiElement>(false);
		GuiElement guiElement;
		if (guiElementDatabase != null && guiElementDatabase.TryGetValue(staticString, out guiElement))
		{
			localizedName = guiElement.Title;
		}
		yield return localizedName;
		yield break;
	}

	private IEnumerable<string> QueryLocalizedNameFromQuestRegisterVariable(QuestRegisterVariable questRegisterVariable)
	{
		Diagnostics.Assert(questRegisterVariable != null);
		yield return questRegisterVariable.Value.ToString();
		yield break;
	}

	private IEnumerable<PointOfInterest> QueryPointsOfInterestFromRegion(Region region)
	{
		Diagnostics.Assert(region != null);
		if (region.PointOfInterests != null)
		{
			for (int index = 0; index < region.PointOfInterests.Length; index++)
			{
				if (this.IsQuestTargetFree(new QuestTargetGameEntity(region.PointOfInterests[index]), this.State.Empire))
				{
					yield return region.PointOfInterests[index];
				}
			}
		}
		yield break;
	}

	private IEnumerable<PointOfInterest> QueryPointOfInterestFromFortress(Fortress fortress)
	{
		Diagnostics.Assert(fortress != null);
		if (this.IsQuestTargetFree(new QuestTargetGameEntity(fortress.PointOfInterest), this.State.Empire))
		{
			yield return fortress.PointOfInterest;
		}
		yield break;
	}

	private IEnumerable<PointOfInterest> QueryPointsOfInterestFromFortress(Fortress fortress)
	{
		Diagnostics.Assert(fortress != null);
		if (this.IsQuestTargetFree(new QuestTargetGameEntity(fortress.PointOfInterest), this.State.Empire))
		{
			yield return fortress.PointOfInterest;
			for (int index = 0; index < fortress.Facilities.Count; index++)
			{
				yield return fortress.Facilities[index];
			}
		}
		yield break;
	}

	private IEnumerable<PointOfInterest> QueryPointOfInterestFromVillage(Village village)
	{
		Diagnostics.Assert(village != null);
		if (this.IsQuestTargetFree(new QuestTargetGameEntity(village.PointOfInterest), this.State.Empire))
		{
			yield return village.PointOfInterest;
		}
		yield break;
	}

	private IEnumerable<PointOfInterest> QueryPointOfInterestFromWorldPosition(WorldPosition position)
	{
		if (position.IsValid)
		{
			Region region = this.WorldPositionningService.GetRegion(position);
			if (region.PointOfInterests != null)
			{
				for (int index = 0; index < region.PointOfInterests.Length; index++)
				{
					if (region.PointOfInterests[index].WorldPosition == position && this.IsQuestTargetFree(new QuestTargetGameEntity(region.PointOfInterests[index]), this.State.Empire))
					{
						yield return region.PointOfInterests[index];
						break;
					}
				}
			}
		}
		yield break;
	}

	private IEnumerable<PointOfInterest> QueryQuestPointsOfInterestFromRegion(Region region)
	{
		Diagnostics.Assert(region != null);
		if (region.PointOfInterests != null)
		{
			for (int index = 0; index < region.PointOfInterests.Length; index++)
			{
				if (region.PointOfInterests[index].SimulationObject.DescriptorHolders.FirstOrDefault((SimulationDescriptorHolder match) => match.Descriptor != null && match.Descriptor.Name.ToString().Contains("QuestLocation")) != null && this.IsQuestTargetFree(new QuestTargetGameEntity(region.PointOfInterests[index]), this.State.Empire))
				{
					yield return region.PointOfInterests[index];
				}
			}
		}
		yield break;
	}

	private IEnumerable<Village> QueryVillagesFromMinorEmpire(MinorEmpire minorEmpire)
	{
		Diagnostics.Assert(minorEmpire != null);
		BarbarianCouncil barbarianCouncil = minorEmpire.GetAgency<BarbarianCouncil>();
		if (barbarianCouncil != null && barbarianCouncil.Villages != null)
		{
			foreach (Village village in barbarianCouncil.Villages)
			{
				if (this.IsQuestTargetFree(new QuestTargetGameEntity(village), this.State.Empire))
				{
					yield return village;
				}
			}
		}
		yield break;
	}

	private IEnumerable<int> QueryIndexFromEmpire(Empire empire)
	{
		Diagnostics.Assert(empire != null);
		if (this.IsQuestTargetFree(new QuestTargetEmpire(empire), this.State.Empire))
		{
			yield return empire.Index;
		}
		yield break;
	}

	private IEnumerable<Village> QueryVillagesFromRegion(Region region)
	{
		Diagnostics.Assert(region != null);
		if (region.MinorEmpire != null)
		{
			BarbarianCouncil barbarianCouncil = region.MinorEmpire.GetAgency<BarbarianCouncil>();
			if (barbarianCouncil != null && barbarianCouncil.Villages != null)
			{
				foreach (Village village in barbarianCouncil.Villages)
				{
					if (this.IsQuestTargetFree(new QuestTargetGameEntity(village), this.State.Empire))
					{
						yield return village;
					}
				}
			}
		}
		yield break;
	}

	private IEnumerable<Fortress> QueryFortressesFromRegion(Region region)
	{
		Diagnostics.Assert(region != null);
		if (region.NavalEmpire != null)
		{
			PirateCouncil pirateCouncil = region.NavalEmpire.GetAgency<PirateCouncil>();
			if (pirateCouncil != null && pirateCouncil.Fortresses != null)
			{
				for (int index = 0; index < pirateCouncil.Fortresses.Count; index++)
				{
					Fortress fortress = pirateCouncil.Fortresses[index];
					if (fortress.Region.Index == region.Index)
					{
						yield return fortress;
					}
				}
			}
		}
		yield break;
	}

	private IEnumerable<WorldPosition> QueryWorldPositionsFromCity(City city)
	{
		Diagnostics.Assert(city != null);
		if (this.IsQuestTargetFree(new QuestTargetWorldPosition(city.WorldPosition), this.State.Empire) && !city.SimulationObject.Tags.Contains(City.TagCityStatusRazed))
		{
			yield return city.WorldPosition;
		}
		yield break;
	}

	private IEnumerable<WorldPosition> QueryWorldPositionFromGUID(ulong guid)
	{
		Diagnostics.Assert(guid != GameEntityGUID.Zero);
		IGameEntity gameEntity = null;
		if (this.gameEntityRepositoryService.TryGetValue(guid, out gameEntity))
		{
			IWorldPositionable positionableEntity = gameEntity as IWorldPositionable;
			if (positionableEntity != null)
			{
				yield return positionableEntity.WorldPosition;
			}
		}
		yield break;
	}

	private IEnumerable<WorldPosition> QueryWorldPositionFromPointOfInterest(PointOfInterest poi)
	{
		Diagnostics.Assert(poi != null);
		if (this.IsQuestTargetFree(new QuestTargetWorldPosition(poi.WorldPosition), this.State.Empire))
		{
			yield return poi.WorldPosition;
		}
		yield break;
	}

	private IEnumerable<WorldPosition> QueryWorldPositionFromRegion(Region region)
	{
		Diagnostics.Assert(region != null);
		if (region.Barycenter.IsValid && this.IsQuestTargetFree(new QuestTargetWorldPosition(region.Barycenter), this.State.Empire))
		{
			yield return region.Barycenter;
		}
		yield break;
	}

	private IEnumerable<WorldPosition> QueryWorldPositionsFromRegion(Region region)
	{
		Diagnostics.Assert(region != null);
		for (int positionIndex = 0; positionIndex < region.WorldPositions.Length; positionIndex++)
		{
			if (this.IsQuestTargetFree(new QuestTargetWorldPosition(region.WorldPositions[positionIndex]), this.State.Empire))
			{
				yield return region.WorldPositions[positionIndex];
			}
		}
		yield break;
	}

	private IEnumerable<Region> QueryRegionFromCity(City city)
	{
		Diagnostics.Assert(city != null);
		if (this.IsQuestTargetFree(new QuestTargetRegion(city.Region), this.State.Empire) && !city.SimulationObject.Tags.Contains(City.TagCityStatusRazed))
		{
			yield return city.Region;
		}
		yield break;
	}

	private IEnumerable<Region> QueryRegionFromFortress(Fortress fortress)
	{
		Diagnostics.Assert(fortress != null);
		if (this.IsQuestTargetFree(new QuestTargetRegion(fortress.Region), this.State.Empire))
		{
			yield return fortress.Region;
		}
		yield break;
	}

	private IEnumerable<Region> QueryRegionWithNeighbourRegionsFromRegion(Region region)
	{
		yield return region;
		IEnumerable<Region> currentEnumerable = this.QueryNeighbourRegionsFromRegion(region);
		if (currentEnumerable != null)
		{
			foreach (Region neighbourRegion in currentEnumerable)
			{
				if (neighbourRegion != null)
				{
					yield return neighbourRegion;
				}
			}
		}
		yield break;
	}

	private IEnumerable<Region> QueryRegionFromWorldPosition(WorldPosition position)
	{
		Diagnostics.Assert(position != WorldPosition.Invalid);
		Region region = this.WorldPositionningService.GetRegion(position);
		if (this.IsQuestTargetFree(new QuestTargetRegion(region), this.State.Empire))
		{
			yield return region;
		}
		yield break;
	}

	private IEnumerable<string> QueryResourceNameFromPointOfInterest(PointOfInterest pointOfInterest)
	{
		Diagnostics.Assert(pointOfInterest != null);
		string pointOfInterestDefinitionType;
		string resourceName;
		if (pointOfInterest.PointOfInterestDefinition.TryGetValue("Type", out pointOfInterestDefinitionType) && pointOfInterestDefinitionType == "ResourceDeposit" && pointOfInterest.PointOfInterestDefinition.TryGetValue("ResourceName", out resourceName))
		{
			yield return resourceName;
		}
		yield break;
	}

	private IEnumerable<string> QueryResourceNameFromFloat(float value)
	{
		yield return Mathf.FloorToInt(value).ToString();
		yield break;
	}

	private bool FilterConstructibleElementByCategory(QuestFilterConstructibleElementByCategory filter, ConstructibleElement constructibleElement)
	{
		return filter.Check(constructibleElement);
	}

	private bool FilterConstructibleElementBySubCategory(QuestFilterConstructibleElementBySubCategory filter, ConstructibleElement constructibleElement)
	{
		return filter.Check(constructibleElement);
	}

	private bool FilterSimulationObjectByPathPrerequisite(PathPrerequisite pathPrerequisite, SimulationObject simulationObject)
	{
		return pathPrerequisite.Check(simulationObject);
	}

	private bool FilterSimulationObjectWrapperByPathPrerequisite(PathPrerequisite pathPrerequisite, SimulationObjectWrapper simulationObjectWrapper)
	{
		return pathPrerequisite.Check(simulationObjectWrapper.SimulationObject);
	}

	private bool FilterSimulationObjectByInterpreterPrerequisite(InterpreterPrerequisite interpreterPrerequisite, SimulationObject simulationObject)
	{
		return interpreterPrerequisite.Check(simulationObject);
	}

	private bool FilterSimulationObjectWrapperByInterpreterPrerequisite(InterpreterPrerequisite interpreterPrerequisite, SimulationObjectWrapper simulationObjectWrapper)
	{
		return interpreterPrerequisite.Check(simulationObjectWrapper.SimulationObject);
	}

	private bool FilterCityByEmpire(QuestFilterCityByEmpire questFilterCityByEmpire, City city)
	{
		if (string.IsNullOrEmpty(questFilterCityByEmpire.EmpireVarName))
		{
			Diagnostics.LogWarning("QuestFilterCityByEmpire: EmpireVarName is null or empty");
			return false;
		}
		MajorEmpire cityOwner;
		if (this.TryGetQuestVariableValueByName<MajorEmpire>(questFilterCityByEmpire.EmpireVarName, out cityOwner) || this.TryGetTarget<MajorEmpire>(questFilterCityByEmpire.EmpireVarName, out cityOwner))
		{
			return questFilterCityByEmpire.Check(city, cityOwner);
		}
		Empire cityOwner2;
		if (this.TryGetQuestVariableValueByName<Empire>(questFilterCityByEmpire.EmpireVarName, out cityOwner2) || this.TryGetTarget<Empire>(questFilterCityByEmpire.EmpireVarName, out cityOwner2))
		{
			return questFilterCityByEmpire.Check(city, cityOwner2);
		}
		Diagnostics.LogError("QuestFilterCityByEmpire: EmpireVarName ('{0}') has not been found in quest variables (or this variable doesn't contains any Empire).", new object[]
		{
			questFilterCityByEmpire.EmpireVarName
		});
		return false;
	}

	private bool FilterFortressByDistance(QuestFilterWorldPositionByDistance questFilterWorldPositionByDistance, Fortress fortress)
	{
		if (string.IsNullOrEmpty(questFilterWorldPositionByDistance.PositionToCompareVarName))
		{
			Diagnostics.LogWarning("QuestFilterWorldPositionByDistance: PositionToCompareVarName is null or empty");
			return false;
		}
		WorldPosition referencePosition;
		if (this.TryGetQuestVariableValueByName<WorldPosition>(questFilterWorldPositionByDistance.PositionToCompareVarName, out referencePosition))
		{
			return questFilterWorldPositionByDistance.Check(fortress.WorldPosition, referencePosition, this.WorldPositionningService);
		}
		Diagnostics.LogError("QuestFilterWorldPositionByDistance: PositionToCompareVarName ('{0}') has not been found in quest variables (or this variable doesn't contains a WorldPosition).", new object[]
		{
			questFilterWorldPositionByDistance.PositionToCompareVarName
		});
		return false;
	}

	private bool FilterFortressByEmpire(QuestFilterFortressByEmpire questFilterFortressByEmpire, Fortress fortress)
	{
		if (string.IsNullOrEmpty(questFilterFortressByEmpire.EmpireVarName))
		{
			Diagnostics.LogWarning("QuestFilterFortressByEmpire: EmpireVarName is null or empty");
			return false;
		}
		MajorEmpire owner;
		if (this.TryGetQuestVariableValueByName<MajorEmpire>(questFilterFortressByEmpire.EmpireVarName, out owner) || this.TryGetTarget<MajorEmpire>(questFilterFortressByEmpire.EmpireVarName, out owner))
		{
			return questFilterFortressByEmpire.Check(fortress, owner);
		}
		Empire owner2;
		if (this.TryGetQuestVariableValueByName<Empire>(questFilterFortressByEmpire.EmpireVarName, out owner2) || this.TryGetTarget<Empire>(questFilterFortressByEmpire.EmpireVarName, out owner2))
		{
			return questFilterFortressByEmpire.Check(fortress, owner2);
		}
		Diagnostics.LogError("QuestFilterFortressByEmpire: EmpireVarName ('{0}') has not been found in quest variables (or this variable doesn't contains any Empire).", new object[]
		{
			questFilterFortressByEmpire.EmpireVarName
		});
		return false;
	}

	private bool FilterMinorEmpireByDiplomaticRelationState(QuestFilterEmpireByDiplomaticRelationState questFilterDiplomaticRelationState, MinorEmpire minorEmpire)
	{
		MajorEmpire currentMajorEmpire;
		return this.TryGetTarget<MajorEmpire>("$(Empire)", out currentMajorEmpire) && questFilterDiplomaticRelationState.Check(currentMajorEmpire, minorEmpire);
	}

	private bool FilterNavalEmpireByDiplomaticRelationState(QuestFilterEmpireByDiplomaticRelationState questFilterDiplomaticRelationState, NavalEmpire navalEmpire)
	{
		MajorEmpire currentMajorEmpire;
		return this.TryGetTarget<MajorEmpire>("$(Empire)", out currentMajorEmpire) && questFilterDiplomaticRelationState.Check(currentMajorEmpire, navalEmpire);
	}

	private bool FilterRegionByDistance(QuestFilterWorldPositionByDistance questFilterWorldPositionByDistance, Region region)
	{
		if (region == null)
		{
			Diagnostics.LogWarning("FilterRegionByDistance: region is null.");
			return false;
		}
		if (!region.Barycenter.IsValid)
		{
			Diagnostics.LogWarning("Region barycenter is invalid.");
			return false;
		}
		WorldPosition referencePosition;
		if (this.TryGetQuestVariableValueByName<WorldPosition>(questFilterWorldPositionByDistance.PositionToCompareVarName, out referencePosition))
		{
			return questFilterWorldPositionByDistance.Check(region.Barycenter, referencePosition, this.WorldPositionningService);
		}
		Diagnostics.LogError("QuestFilterRegionByDistance: PositionToCompareVarName ('{0}') has not been found in quest variables (or this variable doesn't contains a WorldPosition).", new object[]
		{
			questFilterWorldPositionByDistance.PositionToCompareVarName
		});
		return false;
	}

	private bool FilterRegionByEmpire(QuestFilterRegionByEmpire questFilterRegionByEmpire, Region region)
	{
		if (string.IsNullOrEmpty(questFilterRegionByEmpire.EmpireVarName))
		{
			Diagnostics.LogWarning("QuestFilterCityByEmpire: EmpireVarName is null or empty");
			return false;
		}
		MajorEmpire regionOwner;
		if (this.TryGetQuestVariableValueByName<MajorEmpire>(questFilterRegionByEmpire.EmpireVarName, out regionOwner) || this.TryGetTarget<MajorEmpire>(questFilterRegionByEmpire.EmpireVarName, out regionOwner))
		{
			return questFilterRegionByEmpire.Check(region, regionOwner);
		}
		Empire regionOwner2;
		if (this.TryGetQuestVariableValueByName<Empire>(questFilterRegionByEmpire.EmpireVarName, out regionOwner2) || this.TryGetTarget<Empire>(questFilterRegionByEmpire.EmpireVarName, out regionOwner2))
		{
			return questFilterRegionByEmpire.Check(region, regionOwner2);
		}
		Diagnostics.LogError("QuestFilterRegionByEmpire: EmpireVarName ('{0}') has not been found in quest variables (or this variable doesn't contains any Empire).", new object[]
		{
			questFilterRegionByEmpire.EmpireVarName
		});
		return false;
	}

	private bool FilterRegionIsOcean(QuestFilterRegionIsOcean questFilterRegionIsOcean, Region region)
	{
		if (region == null)
		{
			Diagnostics.LogWarning("FilterRegionIsOcean: region is null.");
			return false;
		}
		return questFilterRegionIsOcean.Check(region);
	}

	private bool FilterRegionIsOceanWithPOI(QuestFilterRegionIsOceanWithPOI questFilterRegionIsOceanWithPOI, Region region)
	{
		if (region == null)
		{
			Diagnostics.LogWarning("FilterRegionIsOcean: region is null.");
			return false;
		}
		return questFilterRegionIsOceanWithPOI.Check(region);
	}

	private bool FilterRegionIsOceanWithFortresses(QuestFilterRegionIsOceanWithFortresses questFilterRegionIsOcean, Region region)
	{
		if (region == null)
		{
			Diagnostics.LogWarning("FilterRegionIsOceanWithFortresses: region is null.");
			return false;
		}
		return questFilterRegionIsOcean.Check(region);
	}

	private bool FilterRegionIsContinent(QuestFilterRegionIsContinent questFilterRegionIsContinent, Region region)
	{
		if (region == null)
		{
			Diagnostics.LogWarning("FilterRegionIsContinent: region is null.");
			return false;
		}
		return questFilterRegionIsContinent.Check(region);
	}

	private bool FilterRegionIsColonized(QuestFilterRegionIsColonized questFilterRegionIsColonized, Region region)
	{
		if (region == null)
		{
			Diagnostics.LogWarning("FilterRegionIsColonized: region is null.");
			return false;
		}
		return questFilterRegionIsColonized.Check(region);
	}

	private bool FilterRegionIsOnSameContinent(QuestFilterRegionIsOnSameContinent questFilterRegionIsOnSameContinent, Region regionToCheck)
	{
		if (regionToCheck == null)
		{
			Diagnostics.LogWarning("FilterRegionIsContinent: regionToCheck is null.");
			return false;
		}
		if (string.IsNullOrEmpty(questFilterRegionIsOnSameContinent.RegionContextVarName))
		{
			Diagnostics.LogWarning("QuestFilterRegionIsOnSameContinent: RegionContextVarName is null or empty");
			return false;
		}
		Region regionContext;
		if (this.TryGetQuestVariableValueByName<Region>(questFilterRegionIsOnSameContinent.RegionContextVarName, out regionContext))
		{
			return questFilterRegionIsOnSameContinent.Check(regionToCheck, regionContext);
		}
		Diagnostics.LogError("QuestFilterRegionIsOnSameContinent: RegionContextVarName ('{0}') has not been found in quest variables (or this variable doesn't contains a Region).", new object[]
		{
			questFilterRegionIsOnSameContinent.RegionContextVarName
		});
		return false;
	}

	private bool FilterWorldPositionByDistance(QuestFilterWorldPositionByDistance questFilterWorldPositionByDistance, WorldPosition worldPosition)
	{
		if (string.IsNullOrEmpty(questFilterWorldPositionByDistance.PositionToCompareVarName))
		{
			Diagnostics.LogWarning("QuestFilterWorldPositionByDistance: PositionToCompareVarName is null or empty");
			return false;
		}
		WorldPosition referencePosition;
		if (this.TryGetQuestVariableValueByName<WorldPosition>(questFilterWorldPositionByDistance.PositionToCompareVarName, out referencePosition))
		{
			return questFilterWorldPositionByDistance.Check(worldPosition, referencePosition, this.WorldPositionningService);
		}
		Diagnostics.LogError("QuestFilterWorldPositionByDistance: PositionToCompareVarName ('{0}') has not been found in quest variables (or this variable doesn't contains a WorldPosition).", new object[]
		{
			questFilterWorldPositionByDistance.PositionToCompareVarName
		});
		return false;
	}

	private IEnumerable<Fortress> SortFortressByDistance(QuestSorterFortressByDistance sorter, IEnumerable<Fortress> fortresses)
	{
		if (string.IsNullOrEmpty(sorter.PositionToCompareVarName))
		{
			Diagnostics.LogWarning("QuestSorterFortressByDistance: PositionToCompareVarName is null or empty");
			return null;
		}
		WorldPosition referencePosition;
		if (this.TryGetQuestVariableValueByName<WorldPosition>(sorter.PositionToCompareVarName, out referencePosition))
		{
			return sorter.Sort(fortresses, referencePosition, this.WorldPositionningService);
		}
		Diagnostics.LogError("QuestSorterVillageByDistance: PositionToCompareVarName ('{0}') has not been found in quest variables (or this variable doesn't contains a WorldPosition).", new object[]
		{
			sorter.PositionToCompareVarName
		});
		return null;
	}

	private IEnumerable<Region> SortRegionByDistance(QuestSorterRegionByDistance sorter, IEnumerable<Region> regions)
	{
		if (string.IsNullOrEmpty(sorter.PositionToCompareVarName))
		{
			Diagnostics.LogWarning("QuestSorterRegionByDistance: PositionToCompareVarName is null or empty");
			return null;
		}
		WorldPosition referencePosition;
		if (this.TryGetQuestVariableValueByName<WorldPosition>(sorter.PositionToCompareVarName, out referencePosition))
		{
			return sorter.Sort(regions, referencePosition, this.WorldPositionningService);
		}
		Diagnostics.LogError("QuestSorterRegionByDistance: PositionToCompareVarName ('{0}') has not been found in quest variables (or this variable doesn't contains a WorldPosition).", new object[]
		{
			sorter.PositionToCompareVarName
		});
		return null;
	}

	private IEnumerable<PointOfInterest> SortPointOfInterestByDistance(QuestSorterPointOfInterestByDistance sorter, IEnumerable<PointOfInterest> pointOfInterests)
	{
		if (string.IsNullOrEmpty(sorter.PositionToCompareVarName))
		{
			Diagnostics.LogWarning("QuestSorterPointOfInterestByDistance: PositionToCompareVarName is null or empty");
			return null;
		}
		WorldPosition referencePosition;
		if (this.TryGetQuestVariableValueByName<WorldPosition>(sorter.PositionToCompareVarName, out referencePosition))
		{
			return sorter.Sort(pointOfInterests, referencePosition, this.WorldPositionningService);
		}
		Diagnostics.LogError("QuestSorterPointOfInterestByDistance: PositionToCompareVarName ('{0}') has not been found in quest variables (or this variable doesn't contains a WorldPosition).", new object[]
		{
			sorter.PositionToCompareVarName
		});
		return null;
	}

	private IEnumerable<Village> SortVillageByDistance(QuestSorterVillageByDistance sorter, IEnumerable<Village> villages)
	{
		if (string.IsNullOrEmpty(sorter.PositionToCompareVarName))
		{
			Diagnostics.LogWarning("QuestSorterVillageByDistance: PositionToCompareVarName is null or empty");
			return null;
		}
		WorldPosition referencePosition;
		if (this.TryGetQuestVariableValueByName<WorldPosition>(sorter.PositionToCompareVarName, out referencePosition))
		{
			return sorter.Sort(villages, referencePosition, this.WorldPositionningService);
		}
		Diagnostics.LogError("QuestSorterVillageByDistance: PositionToCompareVarName ('{0}') has not been found in quest variables (or this variable doesn't contains a WorldPosition).", new object[]
		{
			sorter.PositionToCompareVarName
		});
		return null;
	}

	private IEnumerable<City> SortCitiesBySimulationProperty(QuestSorterCitiesBySimulationProperty sorter, IEnumerable<City> cities)
	{
		if (sorter == null)
		{
			Diagnostics.LogError("SorterBySimulationProperty is null");
			return null;
		}
		return sorter.Sort(cities);
	}

	public IDatabase<Droplist> Droplists { get; private set; }

	public IDatabase<QuestDefinition> QuestDefinitions { get; private set; }

	private Engine Engine { get; set; }

	private IEventService EventService { get; set; }

	private int TurnWhenLastBegun { get; set; }

	[Ancillary]
	private IWorldPositionningService WorldPositionningService { get; set; }

	private PlayerController PlayerController
	{
		get
		{
			if (this.playerController == null && base.Game != null)
			{
				if (this.playerControllerRepositoryService == null)
				{
					this.playerControllerRepositoryService = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
				}
				IPlayerControllerRepositoryControl playerControllerRepositoryControl = this.playerControllerRepositoryService as IPlayerControllerRepositoryControl;
				if (playerControllerRepositoryControl != null)
				{
					this.playerController = playerControllerRepositoryControl.GetPlayerControllerById("server");
				}
			}
			return this.playerController;
		}
		set
		{
			this.playerController = value;
		}
	}

	private List<StaticString> QuestTriggeringEvents { get; set; }

	private List<StaticString> QuestLinkedEvents { get; set; }

	public override IEnumerator BindServices(IServiceContainer serviceContainer)
	{
		yield return base.BindService<IWorldPositionningService>(serviceContainer, delegate(IWorldPositionningService service)
		{
			this.WorldPositionningService = service;
		});
		yield return base.BindServices(serviceContainer);
		serviceContainer.AddService<IQuestManagementService>(this);
		serviceContainer.AddService<IQuestRepositoryService>(this);
		serviceContainer.AddService<IQuestRewardRepositoryService>(this);
		this.questManagementService = this;
		this.Engine = new Engine();
		this.State = new QuestManagerState();
		this.Engine.RegisterEnumerable("$Empire", new Engine.Enumerable(this.QueryCurrentEmpire));
		this.Engine.RegisterEnumerable("$Instigator", new Engine.Enumerable(this.QueryCurrentInstigator));
		this.Engine.RegisterEnumerable("$NeighbourRegions", new Engine.Enumerable(this.QueryCurrentNeighbourRegions));
		this.Engine.RegisterEnumerable("$OtherMajorEmpires", new Engine.Enumerable(this.QueryOtherMajorEmpires));
		this.Engine.RegisterEnumerable("$Region", new Engine.Enumerable(this.QueryCurrentRegion));
		this.Engine.RegisterEnumerable("$Regions", new Engine.Enumerable(this.QueryAllRegions));
		this.Engine.RegisterEnumerable("$RegionWithNeighbourRegions", new Engine.Enumerable(this.QueryCurrentRegionWithNeighbourRegions));
		this.Engine.RegisterEnumerable("$TargetPointsOfInterest", new Engine.Enumerable(this.QueryCurrentTargetPointsOfInterest));
		this.Engine.RegisterEnumerable("$TargetVillage", new Engine.Enumerable(this.QueryCurrentTargetVillage));
		this.Engine.RegisterEnumerable("$RandomVillage", new Engine.Enumerable(this.QueryRandomTargetVillage));
		this.Engine.RegisterEnumerable("$Empires", new Engine.Enumerable(this.QueryEmpires));
		this.Engine.RegisterParametizedEnumerable<City>("$AvailableCityImprovements", new Engine.ParametizedEnumerable<City>(this.QueryAvailableCityImprovementsFromCity));
		this.Engine.RegisterParametizedEnumerable<City>("$Position", new Engine.ParametizedEnumerable<City>(this.QueryWorldPositionsFromCity));
		this.Engine.RegisterParametizedEnumerable<City>("$Region", new Engine.ParametizedEnumerable<City>(this.QueryRegionFromCity));
		this.Engine.RegisterParametizedEnumerable<CityImprovementDefinition>("$Name", new Engine.ParametizedEnumerable<CityImprovementDefinition>(this.QueryNameFromCityImprovementDefinition));
		this.Engine.RegisterParametizedEnumerable<ulong>("$Position", new Engine.ParametizedEnumerable<ulong>(this.QueryWorldPositionFromGUID));
		this.Engine.RegisterParametizedEnumerable<Fortress>("$Region", new Engine.ParametizedEnumerable<Fortress>(this.QueryRegionFromFortress));
		this.Engine.RegisterParametizedEnumerable<Fortress>("$PointOfInterest", new Engine.ParametizedEnumerable<Fortress>(this.QueryPointOfInterestFromFortress));
		this.Engine.RegisterParametizedEnumerable<Fortress>("$PointsOfInterest", new Engine.ParametizedEnumerable<Fortress>(this.QueryPointsOfInterestFromFortress));
		this.Engine.RegisterParametizedEnumerable<MajorEmpire>("$Cities", new Engine.ParametizedEnumerable<MajorEmpire>(this.QueryCitiesFromEmpire));
		this.Engine.RegisterParametizedEnumerable<MajorEmpire>("$Heroes", new Engine.ParametizedEnumerable<MajorEmpire>(this.QueryHeroesFromEmpire));
		this.Engine.RegisterParametizedEnumerable<MinorEmpire>("$FactionName", new Engine.ParametizedEnumerable<MinorEmpire>(this.QueryFactionNameFromMinorEmpire));
		this.Engine.RegisterParametizedEnumerable<MinorEmpire>("$Villages", new Engine.ParametizedEnumerable<MinorEmpire>(this.QueryVillagesFromMinorEmpire));
		this.Engine.RegisterParametizedEnumerable<NavalEmpire>("$FactionName", new Engine.ParametizedEnumerable<NavalEmpire>(this.QueryFactionNameFromNavalEmpire));
		this.Engine.RegisterParametizedEnumerable<PointOfInterest>("$Position", new Engine.ParametizedEnumerable<PointOfInterest>(this.QueryWorldPositionFromPointOfInterest));
		this.Engine.RegisterParametizedEnumerable<PointOfInterest>("$ResourceName", new Engine.ParametizedEnumerable<PointOfInterest>(this.QueryResourceNameFromPointOfInterest));
		this.Engine.RegisterParametizedEnumerable<Region>("$City", new Engine.ParametizedEnumerable<Region>(this.QueryCityFromRegion));
		this.Engine.RegisterParametizedEnumerable<Region>("$Fortresses", new Engine.ParametizedEnumerable<Region>(this.QueryFortressesFromRegion));
		this.Engine.RegisterParametizedEnumerable<Region>("$MajorEmpire", new Engine.ParametizedEnumerable<Region>(this.QueryMajorEmpireFromRegion));
		this.Engine.RegisterParametizedEnumerable<Region>("$MinorEmpire", new Engine.ParametizedEnumerable<Region>(this.QueryMinorEmpireFromRegion));
		this.Engine.RegisterParametizedEnumerable<Region>("$NeighbourRegions", new Engine.ParametizedEnumerable<Region>(this.QueryNeighbourRegionsFromRegion));
		this.Engine.RegisterParametizedEnumerable<Region>("$PointsOfInterest", new Engine.ParametizedEnumerable<Region>(this.QueryPointsOfInterestFromRegion));
		this.Engine.RegisterParametizedEnumerable<Region>("$QuestPointsOfInterest", new Engine.ParametizedEnumerable<Region>(this.QueryQuestPointsOfInterestFromRegion));
		this.Engine.RegisterParametizedEnumerable<Region>("$Position", new Engine.ParametizedEnumerable<Region>(this.QueryWorldPositionFromRegion));
		this.Engine.RegisterParametizedEnumerable<Region>("$Positions", new Engine.ParametizedEnumerable<Region>(this.QueryWorldPositionsFromRegion));
		this.Engine.RegisterParametizedEnumerable<Region>("$RegionWithNeighbourRegions", new Engine.ParametizedEnumerable<Region>(this.QueryRegionWithNeighbourRegionsFromRegion));
		this.Engine.RegisterParametizedEnumerable<Region>("$Villages", new Engine.ParametizedEnumerable<Region>(this.QueryVillagesFromRegion));
		this.Engine.RegisterParametizedEnumerable<Village>("$MinorEmpire", new Engine.ParametizedEnumerable<Village>(this.QueryMinorEmpireFromVillage));
		this.Engine.RegisterParametizedEnumerable<Village>("$PointOfInterest", new Engine.ParametizedEnumerable<Village>(this.QueryPointOfInterestFromVillage));
		this.Engine.RegisterParametizedEnumerable<WorldPosition>("$BiomeTypeName", new Engine.ParametizedEnumerable<WorldPosition>(this.QueryBiomeTypeNameFromPosition));
		this.Engine.RegisterParametizedEnumerable<WorldPosition>("$PointOfInterest", new Engine.ParametizedEnumerable<WorldPosition>(this.QueryPointOfInterestFromWorldPosition));
		this.Engine.RegisterParametizedEnumerable<WorldPosition>("$Region", new Engine.ParametizedEnumerable<WorldPosition>(this.QueryRegionFromWorldPosition));
		this.Engine.RegisterParametizedEnumerable<City>("$LocalizedName", new Engine.ParametizedEnumerable<City>(this.QueryLocalizedNameFromCity));
		this.Engine.RegisterParametizedEnumerable<Fortress>("$LocalizedName", new Engine.ParametizedEnumerable<Fortress>(this.QueryLocalizedNameFromFortress));
		this.Engine.RegisterParametizedEnumerable<Region>("$LocalizedName", new Engine.ParametizedEnumerable<Region>(this.QueryLocalizedNameFromRegion));
		this.Engine.RegisterParametizedEnumerable<MinorEmpire>("$LocalizedName", new Engine.ParametizedEnumerable<MinorEmpire>(this.QueryLocalizedNameFromMinorEmpire));
		this.Engine.RegisterParametizedEnumerable<NavalEmpire>("$LocalizedName", new Engine.ParametizedEnumerable<NavalEmpire>(this.QueryLocalizedNameFromNavalEmpire));
		this.Engine.RegisterParametizedEnumerable<MajorEmpire>("$LocalizedName", new Engine.ParametizedEnumerable<MajorEmpire>(this.QueryLocalizedNameFromMajorEmpire));
		this.Engine.RegisterParametizedEnumerable<PointOfInterest>("$LocalizedName", new Engine.ParametizedEnumerable<PointOfInterest>(this.QueryLocalizedNameFromPointOfInterest));
		this.Engine.RegisterParametizedEnumerable<DroppableString>("$LocalizedName", new Engine.ParametizedEnumerable<DroppableString>(this.QueryLocalizedNameFromDroppableString));
		this.Engine.RegisterParametizedEnumerable<DroppableInteger>("$LocalizedName", new Engine.ParametizedEnumerable<DroppableInteger>(this.QueryLocalizedNameFromDroppableInteger));
		this.Engine.RegisterParametizedEnumerable<DroppableResource>("$LocalizedName", new Engine.ParametizedEnumerable<DroppableResource>(this.QueryLocalizedNameFromDroppableResource));
		this.Engine.RegisterParametizedEnumerable<StaticString>("$LocalizedName", new Engine.ParametizedEnumerable<StaticString>(this.QueryLocalizedNameFromStaticString));
		this.Engine.RegisterParametizedEnumerable<QuestRegisterVariable>("$LocalizedName", new Engine.ParametizedEnumerable<QuestRegisterVariable>(this.QueryLocalizedNameFromQuestRegisterVariable));
		this.Engine.RegisterParametizedEnumerable<CityImprovementDefinition>("$LocalizedName", new Engine.ParametizedEnumerable<CityImprovementDefinition>(this.QueryLocalizedNameFromCityImprovementDefinition));
		this.Engine.RegisterParametizedEnumerable<float>("$LocalizedName", new Engine.ParametizedEnumerable<float>(this.QueryResourceNameFromFloat));
		this.Engine.RegisterParametizedFilter<PathPrerequisite, SimulationObject>(new Engine.ParametizedFilter<PathPrerequisite, SimulationObject>(this.FilterSimulationObjectByPathPrerequisite));
		this.Engine.RegisterParametizedFilter<PathPrerequisite, SimulationObjectWrapper>(new Engine.ParametizedFilter<PathPrerequisite, SimulationObjectWrapper>(this.FilterSimulationObjectWrapperByPathPrerequisite));
		this.Engine.RegisterParametizedFilter<InterpreterPrerequisite, SimulationObject>(new Engine.ParametizedFilter<InterpreterPrerequisite, SimulationObject>(this.FilterSimulationObjectByInterpreterPrerequisite));
		this.Engine.RegisterParametizedFilter<InterpreterPrerequisite, SimulationObjectWrapper>(new Engine.ParametizedFilter<InterpreterPrerequisite, SimulationObjectWrapper>(this.FilterSimulationObjectWrapperByInterpreterPrerequisite));
		this.Engine.RegisterParametizedFilter<QuestFilterEmpireByDiplomaticRelationState, MinorEmpire>(new Engine.ParametizedFilter<QuestFilterEmpireByDiplomaticRelationState, MinorEmpire>(this.FilterMinorEmpireByDiplomaticRelationState));
		this.Engine.RegisterParametizedFilter<QuestFilterConstructibleElementByCategory, ConstructibleElement>(new Engine.ParametizedFilter<QuestFilterConstructibleElementByCategory, ConstructibleElement>(this.FilterConstructibleElementByCategory));
		this.Engine.RegisterParametizedFilter<QuestFilterConstructibleElementBySubCategory, ConstructibleElement>(new Engine.ParametizedFilter<QuestFilterConstructibleElementBySubCategory, ConstructibleElement>(this.FilterConstructibleElementBySubCategory));
		this.Engine.RegisterParametizedFilter<QuestFilterWorldPositionByDistance, Region>(new Engine.ParametizedFilter<QuestFilterWorldPositionByDistance, Region>(this.FilterRegionByDistance));
		this.Engine.RegisterParametizedFilter<QuestFilterWorldPositionByDistance, Fortress>(new Engine.ParametizedFilter<QuestFilterWorldPositionByDistance, Fortress>(this.FilterFortressByDistance));
		this.Engine.RegisterParametizedFilter<QuestFilterRegionIsOcean, Region>(new Engine.ParametizedFilter<QuestFilterRegionIsOcean, Region>(this.FilterRegionIsOcean));
		this.Engine.RegisterParametizedFilter<QuestFilterRegionIsOceanWithPOI, Region>(new Engine.ParametizedFilter<QuestFilterRegionIsOceanWithPOI, Region>(this.FilterRegionIsOceanWithPOI));
		this.Engine.RegisterParametizedFilter<QuestFilterRegionIsOceanWithFortresses, Region>(new Engine.ParametizedFilter<QuestFilterRegionIsOceanWithFortresses, Region>(this.FilterRegionIsOceanWithFortresses));
		this.Engine.RegisterParametizedFilter<QuestFilterRegionIsColonized, Region>(new Engine.ParametizedFilter<QuestFilterRegionIsColonized, Region>(this.FilterRegionIsColonized));
		this.Engine.RegisterParametizedFilter<QuestFilterRegionIsNamed, Region>(new Engine.ParametizedFilter<QuestFilterRegionIsNamed, Region>(this.FilterRegionIsNamed));
		this.Engine.RegisterParametizedFilter<QuestFilterRegionIsContinent, Region>(new Engine.ParametizedFilter<QuestFilterRegionIsContinent, Region>(this.FilterRegionIsContinent));
		this.Engine.RegisterParametizedFilter<QuestFilterRegionIsOnSameContinent, Region>(new Engine.ParametizedFilter<QuestFilterRegionIsOnSameContinent, Region>(this.FilterRegionIsOnSameContinent));
		this.Engine.RegisterParametizedFilter<QuestFilterWorldPositionByDistance, WorldPosition>(new Engine.ParametizedFilter<QuestFilterWorldPositionByDistance, WorldPosition>(this.FilterWorldPositionByDistance));
		this.Engine.RegisterParametizedFilter<QuestFilterCityByEmpire, City>(new Engine.ParametizedFilter<QuestFilterCityByEmpire, City>(this.FilterCityByEmpire));
		this.Engine.RegisterParametizedFilter<QuestFilterFortressByEmpire, Fortress>(new Engine.ParametizedFilter<QuestFilterFortressByEmpire, Fortress>(this.FilterFortressByEmpire));
		this.Engine.RegisterParametizedFilter<QuestFilterRegionByEmpire, Region>(new Engine.ParametizedFilter<QuestFilterRegionByEmpire, Region>(this.FilterRegionByEmpire));
		this.Engine.RegisterParametizedFilter<QuestFilterRegionHasRuins, Region>(new Engine.ParametizedFilter<QuestFilterRegionHasRuins, Region>(this.FilterRegionHasRuins));
		this.Engine.RegisterParametizedFilter<QuestFilterPositionValidForDevice, WorldPosition>(new Engine.ParametizedFilter<QuestFilterPositionValidForDevice, WorldPosition>(this.FilterPositionValidForDevice));
		this.Engine.RegisterParametizedSorter<QuestSorterCitiesBySimulationProperty, City>(new Engine.ParametizedSorter<QuestSorterCitiesBySimulationProperty, City>(this.SortCitiesBySimulationProperty));
		this.Engine.RegisterParametizedSorter<QuestSorterFortressByDistance, Fortress>(new Engine.ParametizedSorter<QuestSorterFortressByDistance, Fortress>(this.SortFortressByDistance));
		this.Engine.RegisterParametizedSorter<QuestSorterRegionByDistance, Region>(new Engine.ParametizedSorter<QuestSorterRegionByDistance, Region>(this.SortRegionByDistance));
		this.Engine.RegisterParametizedSorter<QuestSorterPointOfInterestByDistance, PointOfInterest>(new Engine.ParametizedSorter<QuestSorterPointOfInterestByDistance, PointOfInterest>(this.SortPointOfInterestByDistance));
		this.Engine.RegisterParametizedSorter<QuestSorterVillageByDistance, Village>(new Engine.ParametizedSorter<QuestSorterVillageByDistance, Village>(this.SortVillageByDistance));
		ISessionService service2 = Services.GetService<ISessionService>();
		this.isHosting = service2.Session.IsHosting;
		this.EventService = Services.GetService<IEventService>();
		if (this.EventService != null)
		{
			this.EventService.EventRaise += this.EventService_EventRaise;
		}
		else
		{
			Diagnostics.LogError("Failed to retrieve the event service.");
		}
		this.RegisterQuestEvents();
		yield break;
	}

	public override IEnumerator Ignite(IServiceContainer serviceContainer)
	{
		yield return base.Ignite(serviceContainer);
		this.QuestDefinitions = Databases.GetDatabase<QuestDefinition>(false);
		if (this.QuestDefinitions == null)
		{
			Diagnostics.LogError("Failed to retrieve the database of quest definitions.");
		}
		this.Droplists = Databases.GetDatabase<Droplist>(false);
		if (this.Droplists == null)
		{
			Diagnostics.LogError("Failed to retrieve the database of droplists.");
		}
		this.playerControllerRepositoryService = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
		this.gameEntityRepositoryService = base.Game.Services.GetService<IGameEntityRepositoryService>();
		this.endTurnController = (Services.GetService<IEndTurnService>() as IEndTurnControl);
		Diagnostics.Assert(this.playerControllerRepositoryService != null);
		Diagnostics.Assert(this.gameEntityRepositoryService != null);
		Diagnostics.Assert(this.endTurnController != null);
		yield break;
	}

	public bool IsQuestTargetFree(QuestTarget questTarget, Empire empire)
	{
		List<QuestTarget> list;
		return this.State.QuestDefinition == null || !this.State.QuestDefinition.SkipLockedQuestTarget || !this.questTargetsLocked.TryGetValue(empire.Index, out list) || !list.Exists((QuestTarget match) => match.Equals(questTarget));
	}

	public override IEnumerator LoadGame(Game game)
	{
		this.ArmiesKilledRewards = new Dictionary<ulong, IDroppableWithRewardAllocation>();
		this.departmentsOfInternalAffairs = from empire in game.Empires
		where empire is MajorEmpire
		select empire.GetAgency<DepartmentOfInternalAffairs>();
		if (this.questBehaviours != null)
		{
			List<ulong> invalidQuestBehavioursGUIDs = new List<ulong>();
			foreach (KeyValuePair<ulong, QuestBehaviour> keyValuePairs in this.questBehaviours)
			{
				try
				{
					QuestBehaviour questBehaviour2 = keyValuePairs.Value;
					bool hasBeenInitialized = questBehaviour2.Root.Initialize(questBehaviour2);
					if (hasBeenInitialized && this.quests.ContainsKey(keyValuePairs.Key))
					{
						questBehaviour2.Quest = this.quests[keyValuePairs.Key];
						questBehaviour2.UpdateProgressionVariables();
					}
					else
					{
						Diagnostics.LogError("Failed to initialize quest behaviour (guid: {0}).", new object[]
						{
							keyValuePairs.Key
						});
						invalidQuestBehavioursGUIDs.Add(keyValuePairs.Key);
					}
				}
				catch (Exception ex)
				{
					Exception e = ex;
					Diagnostics.LogError("Exception caught while initializing quest (guid: {0}) for behaviour: " + e.ToString(), new object[]
					{
						keyValuePairs.Key
					});
					invalidQuestBehavioursGUIDs.Add(keyValuePairs.Key);
				}
			}
			for (int index = 0; index < invalidQuestBehavioursGUIDs.Count; index++)
			{
				this.questBehaviours.Remove(invalidQuestBehavioursGUIDs[index]);
			}
		}
		if (this.questMarkers != null)
		{
			IGameEntityRepositoryService gameEntityRepositoryService = game.Services.GetService<IGameEntityRepositoryService>();
			if (gameEntityRepositoryService != null)
			{
				foreach (List<QuestMarker> markersByTarget in this.questMarkers.Values)
				{
					for (int markerIndex = 0; markerIndex < markersByTarget.Count; markerIndex++)
					{
						gameEntityRepositoryService.Register(markersByTarget[markerIndex]);
					}
				}
			}
		}
		if (this.questTargetsLocked.Count == 0)
		{
			for (int empireIndex = 0; empireIndex < game.Empires.Length; empireIndex++)
			{
				if (game.Empires[empireIndex] is MajorEmpire)
				{
					this.questTargetsLocked.Add(empireIndex, new List<QuestTarget>());
				}
			}
		}
		if (this.quests != null && this.questBehaviours != null && this.isHosting)
		{
			foreach (Quest quest in this.quests.Values)
			{
				if (!this.questBehaviours.Any((KeyValuePair<ulong, QuestBehaviour> questBehaviour) => questBehaviour.Value.Quest.GUID == quest.GUID))
				{
					Diagnostics.LogError("Quest {0} has been found but not its behaviour", new object[]
					{
						quest.QuestDefinition.Name
					});
				}
			}
		}
		yield return base.LoadGame(game);
		yield break;
	}

	public void OnBeginTurn()
	{
		this.questsCheckedThisTurn.Clear();
		if (this.questBehaviours != null)
		{
			foreach (KeyValuePair<ulong, QuestBehaviour> keyValuePair in this.questBehaviours)
			{
				QuestBehaviour value = keyValuePair.Value;
				this.questManagementService.SendPendingInstructions(value);
			}
		}
	}

	protected void EventService_EventRaise(object sender, EventRaiseEventArgs e)
	{
		if (e.RaisedEvent is EventBeginTurn)
		{
			this.UpdateQuestWorldEffectTimers();
			if (this.questWorldEffectOrders != null && (this.PlayerController != null || !this.isHosting))
			{
				for (int i = 0; i < this.questWorldEffectOrders.Length; i++)
				{
					if (this.isHosting)
					{
						this.PlayerController.PostOrder(this.questWorldEffectOrders[i]);
					}
					else
					{
						this.questManagementService.ApplyQuestWorldEffect(this.questWorldEffectOrders[i]);
					}
				}
				this.questWorldEffectOrders = null;
			}
			if (this.isHosting && base.Game.Turn <= this.TurnWhenLastBegun)
			{
				Diagnostics.Log("QuestManager: Skipping EventBeginTurn on loading.");
				return;
			}
			this.TurnWhenLastBegun = base.Game.Turn;
		}
		else
		{
			EventQuestGlobalEvent eventQuestGlobalEvent = e.RaisedEvent as EventQuestGlobalEvent;
			if (eventQuestGlobalEvent != null && !eventQuestGlobalEvent.AddEffect)
			{
				this.questWorldEffects.Remove(eventQuestGlobalEvent.QuestWorldEffectName);
			}
		}
		if (this.isHosting)
		{
			if (this.QuestTriggeringEvents.Contains(e.RaisedEvent.EventName) && e.RaisedEvent is GameEvent)
			{
				this.TryTriggerQuestsFromGameEvent(e.RaisedEvent as GameEvent);
			}
			if (this.QuestLinkedEvents.Contains(e.RaisedEvent.EventName))
			{
				EventQuestUpdated eventQuestUpdated = e.RaisedEvent as EventQuestUpdated;
				if (eventQuestUpdated == null || eventQuestUpdated.Quest.QuestState == QuestState.Completed || eventQuestUpdated.Quest.QuestState == QuestState.Failed)
				{
					this.questManagementService.ExecuteRunningQuests(e.RaisedEvent);
				}
			}
			if (e.RaisedEvent is EventEncounterStateChange)
			{
				this.CheckForArmyKill(e.RaisedEvent as EventEncounterStateChange);
			}
			else if (e.RaisedEvent is EventQuestComplete)
			{
				this.CheckForCompetitiveQuestComplete(e.RaisedEvent as EventQuestComplete);
			}
		}
	}

	protected void RegisterQuestEvents()
	{
		this.QuestTriggeringEvents = new List<StaticString>();
		this.QuestTriggeringEvents.Add(EventHeroCreated.Name);
		this.QuestTriggeringEvents.Add(EventTutorialGameStarted.Name);
		this.QuestTriggeringEvents.Add(EventFirstColossusCreated.Name);
		this.QuestLinkedEvents = new List<StaticString>();
		this.QuestLinkedEvents.Add(EventArmyDestroyed.Name);
		this.QuestLinkedEvents.Add(EventArmyTransferred.Name);
		this.QuestLinkedEvents.Add(EventArmyDisbanded.Name);
		this.QuestLinkedEvents.Add(EventBeginTurn.Name);
		this.QuestLinkedEvents.Add(EventBoosterActivated.Name);
		this.QuestLinkedEvents.Add(EventCatspaw.Name);
		this.QuestLinkedEvents.Add(EventCityRazed.Name);
		this.QuestLinkedEvents.Add(EventCityDestroyed.Name);
		this.QuestLinkedEvents.Add(EventColonize.Name);
		this.QuestLinkedEvents.Add(EventConstructionEnded.Name);
		this.QuestLinkedEvents.Add(EventDistrictLevelUp.Name);
		this.QuestLinkedEvents.Add(EventDiplomaticContractStateChange.Name);
		this.QuestLinkedEvents.Add(EventEncounterStateChange.Name);
		this.QuestLinkedEvents.Add(EventEndTurn.Name);
		this.QuestLinkedEvents.Add(EventFactionAssimilated.Name);
		this.QuestLinkedEvents.Add(EventHeroAssignment.Name);
		this.QuestLinkedEvents.Add(EventInteractWith.Name);
		this.QuestLinkedEvents.Add(EventInfiltrationActionResult.Name);
		this.QuestLinkedEvents.Add(EventFortressOccupantSwapped.Name);
		this.QuestLinkedEvents.Add(EventQuestComplete.Name);
		this.QuestLinkedEvents.Add(EventQuestUpdated.Name);
		this.QuestLinkedEvents.Add(EventSeasonChange.Name);
		this.QuestLinkedEvents.Add(EventSwapCity.Name);
		this.QuestLinkedEvents.Add(EventTechnologyEnded.Name);
		this.QuestLinkedEvents.Add(EventTradeRoutesUpdated.Name);
		this.QuestLinkedEvents.Add(EventUnitsKilledInAction.Name);
		this.QuestLinkedEvents.Add(EventUnitSkillUnlocked.Name);
		this.QuestLinkedEvents.Add(EventVillageDestroyed.Name);
		this.QuestLinkedEvents.Add(EventVillageDissent.Name);
		this.QuestLinkedEvents.Add(EventVillagePacified.Name);
		this.QuestLinkedEvents.Add(QuestBehaviourTreeNode_Decorator_Debug_PressKey.Name);
		this.QuestLinkedEvents.Add(EventPillageSucceed.Name);
		this.QuestLinkedEvents.Add(EventEmpireWorldTerraformed.Name);
		this.QuestLinkedEvents.Add(EventKaijuTamed.Name);
		this.QuestLinkedEvents.Add(EventFactionIntegrated.Name);
		this.QuestLinkedEvents.Add(EventCreepingNodeUpgradeComplete.Name);
		this.QuestLinkedEvents.Add(EventArmySpawned.Name);
		this.QuestLinkedEvents.Add(EventDismantleDeviceSucceed.Name);
		this.QuestLinkedEvents.Add(EventTerraformationDeviceActivated.Name);
		this.QuestLinkedEvents.Add(EventTerraformDeviceEntityCreated.Name);
		this.QuestLinkedEvents.Add(EventTutorialBuyout.Name);
		this.QuestLinkedEvents.Add(EventTutorialConstructionQueued.Name);
		this.QuestLinkedEvents.Add(EventTutorialFIDSDisplayed.Name);
		this.QuestLinkedEvents.Add(EventTutorialGameStarted.Name);
		this.QuestLinkedEvents.Add(EventTutorialInstructionNextClicked.Name);
		this.QuestLinkedEvents.Add(EventTutorialPanelShowed.Name);
		this.QuestLinkedEvents.Add(EventTutorialPanelHidden.Name);
		this.QuestLinkedEvents.Add(EventTutorialWorkerDragged.Name);
	}

	protected override void Releasing()
	{
		base.Releasing();
		foreach (KeyValuePair<ulong, QuestBehaviour> keyValuePair in this.questBehaviours)
		{
			keyValuePair.Value.Release();
		}
		this.questBehaviours.Clear();
		this.questMarkers.Clear();
		this.questOccurences.Clear();
		this.quests.Clear();
		this.QuestLinkedEvents.Clear();
		this.nextPossibleGlobalQuestTurn = -1;
		this.Engine = null;
		this.XmlSerializer = null;
		if (this.EventService != null)
		{
			this.EventService.EventRaise -= this.EventService_EventRaise;
			this.EventService = null;
		}
		this.WorldPositionningService = null;
		this.PlayerController = null;
	}

	private void CheckForArmyKill(EventEncounterStateChange encounterEvent)
	{
		if (encounterEvent.EventArgs.Encounter != null && encounterEvent.EventArgs.EncounterState == EncounterState.BattleHasEnded)
		{
			IEnumerable<Empire> source = from contender in encounterEvent.EventArgs.Encounter.Contenders
			select contender.Empire into empire
			where empire is MajorEmpire
			select empire;
			Empire empire1 = source.FirstOrDefault<Empire>();
			if (empire1 != null)
			{
				this.GiveQuestArmyKillReward(empire1, encounterEvent.EventArgs.Encounter);
				Empire empire2 = source.FirstOrDefault((Empire empire) => empire != empire1);
				if (empire2 != null)
				{
					this.GiveQuestArmyKillReward(empire2, encounterEvent.EventArgs.Encounter);
				}
			}
		}
	}

	private void CheckForCompetitiveQuestComplete(EventQuestComplete questCompleteEvent)
	{
		if (questCompleteEvent.Quest != null && questCompleteEvent.Quest.QuestDefinition.IsGlobal && questCompleteEvent.Quest.QuestDefinition.GlobalWinner == GlobalQuestWinner.First && questCompleteEvent.Quest.QuestState == QuestState.Completed)
		{
			Quest[] array = (from quest in this.quests
			where quest.Value.EmpireBits != questCompleteEvent.Empire.Bits && (quest.Value.QuestState == QuestState.InProgress || quest.Value.QuestState == QuestState.Completed) && quest.Value.QuestDefinition.Name == questCompleteEvent.Quest.QuestDefinition.Name
			select quest.Value).ToArray<Quest>();
			foreach (Quest quest2 in array)
			{
				QuestInstruction_UpdateQuest questInstruction_UpdateQuest = new QuestInstruction_UpdateQuest(QuestState.Failed);
				QuestInstruction_UpdateRegisterVariable questInstruction_UpdateRegisterVariable = new QuestInstruction_UpdateRegisterVariable(QuestDefinition.WinnerVariableName, questCompleteEvent.Empire.Index);
				OrderUpdateQuest order = new OrderUpdateQuest(quest2, new QuestInstruction[]
				{
					questInstruction_UpdateQuest,
					questInstruction_UpdateRegisterVariable
				});
				this.PlayerController.PostOrder(order);
				OrderCompleteQuest order2 = new OrderCompleteQuest(quest2);
				this.PlayerController.PostOrder(order2);
			}
		}
	}

	private bool CheckForQuestDefinitionTriggers(QuestBehaviour questBehaviour)
	{
		if (questBehaviour == null)
		{
			throw new ArgumentNullException("questBehaviour");
		}
		if (questBehaviour.Quest == null)
		{
			throw new ArgumentException("questBehaviour.Quest");
		}
		if (questBehaviour.Quest.QuestDefinition.Triggers != null)
		{
			Tags tags = null;
			QuestState questState = questBehaviour.Quest.QuestState;
			if (questState != QuestState.Completed)
			{
				if (questState == QuestState.Failed)
				{
					if (questBehaviour.Quest.QuestDefinition.Triggers.OnQuestFailed == null)
					{
						return false;
					}
					tags = questBehaviour.Quest.QuestDefinition.Triggers.OnQuestFailed.Tags;
				}
			}
			else
			{
				if (questBehaviour.Quest.QuestDefinition.Triggers.OnQuestCompleted == null)
				{
					return false;
				}
				tags = questBehaviour.Quest.QuestDefinition.Triggers.OnQuestCompleted.Tags;
			}
			if (tags != null)
			{
				this.questManagementService.InitState(tags, questBehaviour.Initiator, WorldPosition.Invalid);
				QuestDefinition questDefinition;
				QuestVariable[] collection;
				QuestInstruction[] pendingInstructions;
				QuestReward[] questRewards;
				Dictionary<Region, List<string>> regionQuestLocalizationVariableDefinitionLocalizationKey;
				if (this.TryTrigger(out questDefinition, out collection, out pendingInstructions, out questRewards, out regionQuestLocalizationVariableDefinitionLocalizationKey))
				{
					List<QuestVariable> list = new List<QuestVariable>(collection);
					list.AddRange(this.State.GlobalQuestVariablesCurrentEmpire);
					return this.Trigger(this.State.Empire, questDefinition, list.ToArray(), pendingInstructions, questRewards, regionQuestLocalizationVariableDefinitionLocalizationKey, null, true);
				}
			}
		}
		return false;
	}

	private bool CheckPrerequisites(QuestDefinition questDefinition, Dictionary<StaticString, IEnumerable<SimulationObjectWrapper>> targets)
	{
		if (questDefinition.Prerequisites == null)
		{
			return true;
		}
		for (int i = 0; i < questDefinition.Prerequisites.Length; i++)
		{
			QuestPrerequisites questPrerequisites = questDefinition.Prerequisites[i];
			IEnumerable<SimulationObjectWrapper> enumerable = null;
			if (questPrerequisites.Target == null)
			{
				enumerable = new SimulationObjectWrapper[1];
			}
			if (enumerable == null && !targets.TryGetValue(questPrerequisites.Target, out enumerable))
			{
				if (GameManager.Preferences.QuestVerboseMode)
				{
					Diagnostics.LogWarning("[Quest] Could not find prerequisite target {0} in quest {1}", new object[]
					{
						questPrerequisites.Target,
						questDefinition.Name
					});
				}
				return false;
			}
			if (questPrerequisites.Prerequisites != null)
			{
				bool flag = false;
				bool flag2 = false;
				for (int j = 0; j < questPrerequisites.Prerequisites.Length; j++)
				{
					foreach (SimulationObjectWrapper simulationObjectWrapper in enumerable)
					{
						using (InterpreterContext.InterpreterSession interpreterSession = QuestVariableDefinition.CreateSession(simulationObjectWrapper, this.State.QuestVariables))
						{
							bool flag3 = !questPrerequisites.Prerequisites[j].Check(interpreterSession.Context);
							flag = (flag || flag3);
							flag2 |= !flag3;
							if (GameManager.Preferences.QuestVerboseMode)
							{
								Diagnostics.Log("[Quest] Prerequisite n°{0} of quest {1} returned {2} on target {3}", new object[]
								{
									j + 1,
									questDefinition.Name,
									!flag3,
									(simulationObjectWrapper == null) ? "null" : simulationObjectWrapper.Name.ToString()
								});
							}
							if ((questPrerequisites.AnyTarget && !flag3) || (!questPrerequisites.AnyTarget && flag3))
							{
								flag = flag3;
								flag2 = !flag3;
								if (GameManager.Preferences.QuestVerboseMode && enumerable.Count<SimulationObjectWrapper>() > 1)
								{
									Diagnostics.Log("[Quest] Prerequisite n°{0} of quest {1} did not check further targets because AnyPrerequisite = {2}", new object[]
									{
										j + 1,
										questDefinition.Name,
										questPrerequisites.AnyTarget
									});
								}
								break;
							}
						}
					}
					if (questPrerequisites.AnyPrerequisite && flag2)
					{
						if (GameManager.Preferences.QuestVerboseMode)
						{
							Diagnostics.Log("[Quest] Prerequisite n°{0} of quest {1} returned true preemptively because AnyPrerequisite = true", new object[]
							{
								j + 1,
								questDefinition.Name
							});
						}
						return true;
					}
					if (!questPrerequisites.AnyPrerequisite && flag)
					{
						return false;
					}
				}
				return !flag;
			}
		}
		return true;
	}

	private bool CheckRandom(QuestDefinition questDefinition)
	{
		if (questDefinition.ChanceOfTriggering >= 1f)
		{
			return true;
		}
		int num = UnityEngine.Random.Range(0, 101);
		if (this.gameSpeedMultiplier == 0f)
		{
			for (int i = base.Game.Empires.Length - 1; i >= 0; i--)
			{
				LesserEmpire lesserEmpire = base.Game.Empires[i] as LesserEmpire;
				if (lesserEmpire != null)
				{
					this.gameSpeedMultiplier = lesserEmpire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier);
					break;
				}
			}
			if (this.gameSpeedMultiplier <= 0f)
			{
				this.gameSpeedMultiplier = 1f;
			}
		}
		if (GameManager.Preferences.QuestVerboseMode)
		{
			Diagnostics.LogWarning("[Quest] {0} CheckRandom {1} / {3} >= {2} ?", new object[]
			{
				questDefinition.Name,
				questDefinition.ChanceOfTriggering * 100f,
				(float)num,
				this.gameSpeedMultiplier
			});
		}
		return questDefinition.ChanceOfTriggering * 100f / this.gameSpeedMultiplier >= (float)num;
	}

	private bool CheckRepetition(QuestDefinition questDefinition)
	{
		if (this.State.Empire == null)
		{
			return false;
		}
		QuestOccurence questOccurence;
		if (this.questOccurences.TryGetValue(questDefinition.Name, out questOccurence))
		{
			if (questDefinition.NumberOfOccurencesPerGame > 0 && questOccurence.NumberOfOccurencesThisGame >= questDefinition.NumberOfOccurencesPerGame)
			{
				if (GameManager.Preferences.QuestVerboseMode)
				{
					Diagnostics.LogWarning("[Quest] {0} didn't trigger because of repetition, NumberOfOccurencesPerGame = {1} and NumberOfOccurencesThisGame = {2}", new object[]
					{
						questDefinition.Name,
						questDefinition.NumberOfOccurencesPerGame,
						questOccurence.NumberOfOccurencesThisGame
					});
				}
				return false;
			}
			if (questDefinition.NumberOfOccurencesPerEmpire > 0 && questOccurence.NumberOfOccurrencesForThisEmpireSoFar[this.State.Empire.Index] >= questDefinition.NumberOfOccurencesPerEmpire)
			{
				if (GameManager.Preferences.QuestVerboseMode)
				{
					Diagnostics.LogWarning("[Quest] {0} didn't trigger because of repetition, NumberOfOccurencesPerEmpire = {1} and NumberOfOccurrencesForThisEmpireSoFar = {2}", new object[]
					{
						questDefinition.Name,
						questDefinition.NumberOfOccurencesPerEmpire,
						questOccurence.NumberOfOccurrencesForThisEmpireSoFar[this.State.Empire.Index]
					});
				}
				return false;
			}
			if (questDefinition.Cooldown > 0 && questOccurence.LastCompletedOnTurn >= 0)
			{
				int num = base.Game.Turn - questOccurence.LastCompletedOnTurn;
				if (questDefinition.Cooldown >= num)
				{
					if (GameManager.Preferences.QuestVerboseMode)
					{
						Diagnostics.LogWarning("[Quest] {0} didn't trigger because of repetition, Cooldown = {1} and ElapsedTime = {2}", new object[]
						{
							questDefinition.Name,
							questDefinition.Cooldown,
							num
						});
					}
					return false;
				}
			}
			if (questDefinition.NumberOfConcurrentInstances > 0)
			{
				int num2 = questDefinition.NumberOfConcurrentInstances;
				foreach (QuestBehaviour questBehaviour in this.questBehaviours.Values)
				{
					if (questBehaviour.Quest.QuestDefinition.Name == questDefinition.Name && --num2 <= 0)
					{
						if (GameManager.Preferences.QuestVerboseMode)
						{
							Diagnostics.LogWarning("[Quest] {0} didn't trigger because of repetition, their are too many concurrent instances (NumberOfMaxConcurrentInstances = {1})", new object[]
							{
								questDefinition.Name,
								questDefinition.NumberOfConcurrentInstances
							});
						}
						return false;
					}
				}
			}
		}
		if (questDefinition.GlobalCooldownLiability != GlobalCooldownLiability.ForceIgnore && (questDefinition.GlobalCooldownLiability == GlobalCooldownLiability.ForceCheck || (questDefinition.GlobalCooldown >= 0 && questDefinition.IsGlobal)) && this.nextPossibleGlobalQuestTurn > base.Game.Turn)
		{
			if (GameManager.Preferences.QuestVerboseMode)
			{
				Diagnostics.LogWarning("[Quest] {0} didn't trigger because of repetition, NextPossibleGlobalQuestTurn = {1} & CurrentTurn = {2}", new object[]
				{
					questDefinition.Name,
					this.nextPossibleGlobalQuestTurn,
					base.Game.Turn
				});
			}
			return false;
		}
		if (questDefinition.IsGlobal && questDefinition.NumberOfGlobalQuestConcurrentInstances >= 0 && this.playerControllerRepositoryService.ActivePlayerController != null && this.playerControllerRepositoryService.ActivePlayerController.Empire != null && this.quests.Count((KeyValuePair<ulong, Quest> quest) => quest.Value.EmpireBits == this.playerControllerRepositoryService.ActivePlayerController.Empire.Bits && quest.Value.QuestDefinition.IsGlobal && !quest.Value.IsMainQuest()) > questDefinition.NumberOfGlobalQuestConcurrentInstances)
		{
			if (GameManager.Preferences.QuestVerboseMode)
			{
				Diagnostics.LogWarning("[Quest] {0} didn't trigger because of repetition, their are too many global instances (NumberOfMaxGlobalQuestConcurrentInstances = {1})", new object[]
				{
					questDefinition.Name,
					questDefinition.NumberOfGlobalQuestConcurrentInstances
				});
			}
			return false;
		}
		return true;
	}

	private bool CheckTags(QuestDefinition questDefinition, Tags tags)
	{
		return tags == null || tags.IsNullOrEmpty || (questDefinition.Tags != null && !questDefinition.Tags.IsNullOrEmpty && (!TutorialManager.IsActivated || questDefinition.Tags.Contains(TutorialManager.TutorialQuestMandatoryTag)) && questDefinition.Tags.Contains(tags));
	}

	private bool CheckRewards(QuestDefinition questDefinition, out QuestReward[] questRewards)
	{
		if (questDefinition.Steps == null)
		{
			questRewards = new QuestReward[0];
			return true;
		}
		List<QuestReward> list = new List<QuestReward>();
		List<IDroppable> droppedRewards = new List<IDroppable>();
		for (int i = 0; i < questDefinition.Steps.Length; i++)
		{
			this.questManagementService.AddStepRewards(questDefinition, i, list, droppedRewards, null);
		}
		questRewards = list.ToArray();
		return true;
	}

	private bool CheckVariables(QuestVariableDefinition[] questVariableDefinitions, QuestDefinition questDefinition, List<QuestVariable> vars, List<QuestInstruction> pendingInstructions, Dictionary<Region, List<string>> regionQuestLocalizationVariableDefinitionLocalizationKey)
	{
		if (questVariableDefinitions == null)
		{
			return true;
		}
		List<QuestInstruction> list = new List<QuestInstruction>(pendingInstructions);
		List<QuestVariable> list2 = new List<QuestVariable>(vars);
		this.State.QuestVariables = list2;
		foreach (QuestVariableDefinition questVariableDefinition in questVariableDefinitions)
		{
			QuestVariable questVariable = new QuestVariable(questVariableDefinition.VarName);
			if (questVariableDefinition is QuestDropListVariableDefinition)
			{
				QuestDropListVariableDefinition questDropListVariableDefinition = questVariableDefinition as QuestDropListVariableDefinition;
				if (string.IsNullOrEmpty(questDropListVariableDefinition.DropList))
				{
					Diagnostics.LogError("QuestManager: DropList is null for variable '{0}' in quest '{1}'.", new object[]
					{
						questVariable.Name,
						questDefinition.Name
					});
					this.Engine.FlushTemporaryEnumerables();
					return false;
				}
				questVariable.Object = this.Drop(questDropListVariableDefinition.DropList, 0, null, null, null);
			}
			else if (questVariableDefinition is QuestInterpretedVariableDefinition)
			{
				QuestInterpretedVariableDefinition questInterpretedVariableDefinition = questVariableDefinition as QuestInterpretedVariableDefinition;
				questVariable.Object = questInterpretedVariableDefinition.Evaluate(this.State.Targets, this.State.QuestVariables);
				if (questVariable.Object == null || !(questVariable.Object is float))
				{
					if (GameManager.Preferences.QuestVerboseMode)
					{
						Diagnostics.LogWarning("[Quest] InterpretedVariable {0} returned {2} which is not a number (in quest '{1}')", new object[]
						{
							questVariableDefinition.VarName,
							questDefinition.Name,
							(questVariable.Object != null) ? questVariable.Object : "null"
						});
					}
					this.Engine.FlushTemporaryEnumerables();
					return false;
				}
			}
			else if (questVariableDefinition.FromGlobal)
			{
				questVariable = this.GetQuestVariableByName(questVariableDefinition.VarName);
				if (questVariable == null)
				{
					Diagnostics.LogError("Can't find global variable {0}", new object[]
					{
						questVariableDefinition.VarName
					});
					this.Engine.FlushTemporaryEnumerables();
					return false;
				}
			}
			else
			{
				if (questVariableDefinition.Query != null)
				{
					try
					{
						questVariable.Object = questVariableDefinition.Query.Execute(this.Engine);
						List<object> list3 = questVariable.Object as List<object>;
						if (list3 == null || list3.Count == 0)
						{
							if (GameManager.Preferences.QuestVerboseMode)
							{
								Diagnostics.LogWarning("[Quest] Query returned null or empty list (type: {0}, expression: '{1}') for variable (name: '{2}') in quest {3}.", new object[]
								{
									questVariableDefinition.Query.GetType().ToString(),
									questVariableDefinition.Query.ToString(),
									questVariable.Name,
									questDefinition.Name
								});
							}
							this.Engine.FlushTemporaryEnumerables();
							return false;
						}
						goto IL_2B5;
					}
					catch (Exception ex)
					{
						Diagnostics.LogError("Exception caught while executing query (type: {0}, expression: '{1}') for variable (name: '{2}').", new object[]
						{
							questVariableDefinition.Query.GetType().ToString(),
							questVariableDefinition.Query.ToString(),
							questVariable.Name
						});
						Diagnostics.LogError(ex.ToString());
						this.Engine.FlushTemporaryEnumerables();
						return false;
					}
				}
				questVariable.Object = new QuestRegisterVariable(questVariableDefinition.Value);
			}
			IL_2B5:
			if (!(questVariableDefinition is QuestLocalizationVariableDefinition))
			{
				this.Engine.RegisterTemporaryEnumerable(questVariable.Name, new Engine.Enumerable(this.QueryQuestVariable));
			}
			list2.Add(questVariable);
			if (questVariableDefinition.ToGlobal && this.questManagementService != null)
			{
				this.questManagementService.State.AddGlobalVariable(this.State.Empire.Index, questVariable);
			}
			if (questVariableDefinition is QuestLocalizationVariableDefinition && questVariable.Object != null && questVariable.Object is IEnumerable<object>)
			{
				IEnumerable<object> enumerable = questVariable.Object as IEnumerable<object>;
				if (enumerable != null)
				{
					IEnumerator<object> enumerator = enumerable.GetEnumerator();
					if (enumerator.MoveNext() && enumerator.Current is string)
					{
						QuestLocalizationVariableDefinition questLocalizationVariableDefinition = questVariableDefinition as QuestLocalizationVariableDefinition;
						list.Add(new QuestInstruction_UpdateLocalizationVariable(questLocalizationVariableDefinition.LocalizationKey, enumerator.Current as string));
						Region key;
						City city;
						if (this.TryGetQuestVariableValueByName<Region>(questLocalizationVariableDefinition.XmlSerializableSource, out key))
						{
							if (!regionQuestLocalizationVariableDefinitionLocalizationKey.ContainsKey(key))
							{
								regionQuestLocalizationVariableDefinitionLocalizationKey.Add(key, new List<string>());
							}
							regionQuestLocalizationVariableDefinitionLocalizationKey[key].Add(questLocalizationVariableDefinition.LocalizationKey);
						}
						else if (this.TryGetQuestVariableValueByName<City>(questLocalizationVariableDefinition.XmlSerializableSource, out city) && city != null && city.Region != null)
						{
							if (!regionQuestLocalizationVariableDefinitionLocalizationKey.ContainsKey(city.Region))
							{
								regionQuestLocalizationVariableDefinitionLocalizationKey.Add(city.Region, new List<string>());
							}
							regionQuestLocalizationVariableDefinitionLocalizationKey[city.Region].Add(questLocalizationVariableDefinition.LocalizationKey);
						}
					}
				}
			}
		}
		vars.AddRange(list2);
		pendingInstructions.AddRange(list);
		this.State.QuestVariables = vars;
		this.Engine.FlushTemporaryEnumerables();
		return true;
	}

	private IEnumerable<IDroppable> Drop(StaticString droplistName, int picks = 0, List<IDroppable> alreadyDroppedRewards = null, Empire empire = null, Quest quest = null)
	{
		List<IDroppable> list = new List<IDroppable>();
		Droplist droplist;
		if (this.Droplists != null && !StaticString.IsNullOrEmpty(droplistName) && this.Droplists.TryGetValue(droplistName, out droplist))
		{
			if (picks == 0)
			{
				picks = droplist.Picks;
			}
			for (int i = 0; i < picks; i++)
			{
				Droplist droplist2;
				IDroppable droppable = droplist.Pick(empire ?? this.State.Empire, out droplist2, new object[]
				{
					quest
				});
				if (droppable != null)
				{
					if (!droppable.CanHaveDuplicate && ((alreadyDroppedRewards != null && alreadyDroppedRewards.Any((IDroppable reward) => reward.Equals(droppable))) || list.Any((IDroppable reward) => reward.Equals(droppable)) || this.IsThereACurrentQuestWithThisReward(droppable)))
					{
						if (droplist2 == null)
						{
							return list;
						}
						droplist = droplist2;
						i--;
					}
					else
					{
						list.Add(droppable);
					}
				}
			}
		}
		return list;
	}

	private IEnumerable<Quest> GetQuestsInProgressNamed(StaticString questName)
	{
		if (this.isHosting)
		{
			return from quest in this.quests
			where quest.Value.Name == questName
			select quest.Value;
		}
		List<Quest> list = new List<Quest>();
		foreach (DepartmentOfInternalAffairs departmentOfInternalAffairs in this.departmentsOfInternalAffairs)
		{
			if (departmentOfInternalAffairs.QuestJournal != null)
			{
				ReadOnlyCollection<Quest> readOnlyCollection = departmentOfInternalAffairs.QuestJournal.Read(QuestState.Completed);
				if (readOnlyCollection != null)
				{
					list.AddRange(from quest in readOnlyCollection
					where quest.Name == questName
					select quest);
				}
				readOnlyCollection = departmentOfInternalAffairs.QuestJournal.Read(QuestState.Failed);
				if (readOnlyCollection != null)
				{
					list.AddRange(from quest in readOnlyCollection
					where quest.Name == questName
					select quest);
				}
				readOnlyCollection = departmentOfInternalAffairs.QuestJournal.Read(QuestState.InProgress);
				if (readOnlyCollection != null)
				{
					list.AddRange(from quest in readOnlyCollection
					where quest.Name == questName
					select quest);
				}
			}
		}
		return list;
	}

	private bool IsThereACurrentQuestWithThisReward(IDroppable droppable)
	{
		IQuestRepositoryService service = base.Game.Services.GetService<IQuestRepositoryService>();
		return service.AsEnumerable(this.State.Empire.Index).Any((Quest quest) => (quest.QuestState == QuestState.Unstarted || quest.QuestState == QuestState.InProgress) && quest.QuestRewards != null && quest.QuestRewards.Any((QuestReward reward) => reward.Droppables != null && reward.Droppables.Any((IDroppable currentDroppable) => currentDroppable.Equals(droppable))));
	}

	private bool TryGetTarget<T>(StaticString targetName, out T target) where T : SimulationObjectWrapper
	{
		IEnumerable<SimulationObjectWrapper> source;
		if (this.State.Targets != null && this.State.Targets.TryGetValue(targetName, out source))
		{
			SimulationObjectWrapper simulationObjectWrapper = source.FirstOrDefault<SimulationObjectWrapper>();
			if (simulationObjectWrapper != null && simulationObjectWrapper is T)
			{
				target = (T)((object)simulationObjectWrapper);
				return true;
			}
		}
		target = (T)((object)null);
		return false;
	}

	private bool TryGetTargets<T>(StaticString targetName, out T target) where T : IEnumerable<SimulationObjectWrapper>
	{
		IEnumerable<SimulationObjectWrapper> enumerable;
		if (this.State.Targets != null && this.State.Targets.TryGetValue(targetName, out enumerable) && enumerable is T)
		{
			target = (T)((object)enumerable);
			return true;
		}
		target = default(T);
		return false;
	}

	private bool TryGetQuestVariableValueByName<T>(StaticString varName, out T varValue)
	{
		QuestVariable questVariableByName = this.GetQuestVariableByName(varName);
		if (questVariableByName != null)
		{
			if (questVariableByName.Object is T)
			{
				varValue = (T)((object)questVariableByName.Object);
				return true;
			}
			if (questVariableByName.Object is IEnumerable<T>)
			{
				IEnumerable<T> enumerable = questVariableByName.Object as IEnumerable<T>;
				if (enumerable != null)
				{
					IEnumerator<T> enumerator = enumerable.GetEnumerator();
					if (enumerator.MoveNext())
					{
						varValue = enumerator.Current;
						return true;
					}
				}
			}
			if (questVariableByName.Object is IEnumerable<object>)
			{
				IEnumerable<object> source = questVariableByName.Object as IEnumerable<object>;
				try
				{
					IEnumerable<T> enumerable2 = source.Cast<T>();
					if (enumerable2 != null)
					{
						IEnumerator<T> enumerator2 = enumerable2.GetEnumerator();
						if (enumerator2.MoveNext())
						{
							varValue = enumerator2.Current;
							return true;
						}
					}
				}
				catch
				{
					varValue = default(T);
					return false;
				}
			}
		}
		varValue = default(T);
		return false;
	}

	private bool TryGetQuestVariableValueByName<T>(StaticString varName, out IEnumerable<T> varValue) where T : SimulationObjectWrapper
	{
		QuestVariable questVariableByName = this.GetQuestVariableByName(varName);
		if (questVariableByName == null)
		{
			varValue = null;
			return false;
		}
		if (questVariableByName.Object is IEnumerable<T>)
		{
			IEnumerable<T> enumerable = questVariableByName.Object as IEnumerable<T>;
			if (enumerable != null)
			{
				varValue = enumerable;
				return true;
			}
		}
		if (questVariableByName.Object is IEnumerable<object>)
		{
			IEnumerable<object> source = questVariableByName.Object as IEnumerable<object>;
			try
			{
				IEnumerable<T> enumerable2 = source.Cast<T>();
				if (enumerable2 != null)
				{
					varValue = enumerable2;
					return true;
				}
			}
			catch
			{
				varValue = null;
				return false;
			}
		}
		varValue = null;
		return false;
	}

	private void TryTriggerQuestsFromGameEvent(GameEvent gameEvent)
	{
		if (!(gameEvent.Empire is MajorEmpire))
		{
			return;
		}
		IQuestManagementService questManagementService = this.questManagementService;
		Empire empire = gameEvent.Empire as Empire;
		this.questManagementService.InitState(gameEvent.EventName, empire, WorldPosition.Invalid);
		if (GameManager.Preferences.QuestVerboseMode)
		{
			Diagnostics.Log("[Quest] Trying to trigger a quest for event {0}", new object[]
			{
				gameEvent.EventName
			});
		}
		QuestDefinition questDefinition;
		QuestVariable[] questVariables;
		QuestInstruction[] pendingInstructions;
		QuestReward[] questRewards;
		Dictionary<Region, List<string>> regionQuestLocalizationVariableDefinitionLocalizationKey;
		if (questManagementService.TryTrigger(out questDefinition, out questVariables, out pendingInstructions, out questRewards, out regionQuestLocalizationVariableDefinitionLocalizationKey))
		{
			questManagementService.Trigger(empire, questDefinition, questVariables, pendingInstructions, questRewards, regionQuestLocalizationVariableDefinitionLocalizationKey, null, true);
		}
	}

	private void UpdateQuestWorldEffectTimers()
	{
		if (this.questWorldEffects != null)
		{
			Dictionary<StaticString, KeyValuePair<int, List<SimulationObject>>> dictionary = new Dictionary<StaticString, KeyValuePair<int, List<SimulationObject>>>(this.questWorldEffects);
			foreach (KeyValuePair<StaticString, KeyValuePair<int, List<SimulationObject>>> keyValuePair in dictionary)
			{
				foreach (SimulationObject simulationObject in keyValuePair.Value.Value)
				{
					if (simulationObject != null && simulationObject.Tags.Contains("ClassTimedBonus"))
					{
						float propertyValue = simulationObject.GetPropertyValue(SimulationProperties.RemainingTime);
						if (propertyValue <= 1f)
						{
							this.questWorldEffects.Remove(keyValuePair.Key);
							if (this.isHosting)
							{
								OrderQuestWorldEffect order = new OrderQuestWorldEffect(keyValuePair.Key, false, false, keyValuePair.Value.Key, 0);
								this.PlayerController.PostOrder(order);
							}
							break;
						}
						simulationObject.SetPropertyBaseValue(SimulationProperties.RemainingTime, propertyValue - 1f);
					}
				}
			}
		}
	}

	StaticString IQuestManagementService.ForceSideQuestVillageTrigger(string sideQuestVillageName)
	{
		string x = "I'll try to trigger '" + sideQuestVillageName + "' in the next parley with a minor faction.";
		QuestManager._GlobalVillageName = sideQuestVillageName;
		return x;
	}

	private Empire RandomMinorEmpire()
	{
		return (from min in base.Game.Empires
		where min is MinorEmpire
		select min).ToArray<Empire>()[0];
	}

	private IEnumerable<Village> QueryRandomTargetVillage(StaticString keyword)
	{
		List<Region> list = new List<Region>(base.Game.World.Regions);
		list = list.Randomize(null);
		foreach (Region region in list)
		{
			if (region.MinorEmpire != null)
			{
				BarbarianCouncil agency = region.MinorEmpire.GetAgency<BarbarianCouncil>();
				if (agency != null && agency.Villages != null)
				{
					foreach (Village village in agency.Villages)
					{
						if (village.Empire.LocalizedName.ToLower().StartsWith(QuestManager._GlobalVillageName) && this.IsQuestTargetFree(new QuestTargetGameEntity(village), this.State.Empire))
						{
							yield return village;
							break;
						}
					}
					IEnumerator<Village> enumerator2 = null;
				}
			}
		}
		List<Region>.Enumerator enumerator = default(List<Region>.Enumerator);
		yield break;
		yield break;
	}

	private bool CheckForcedByCheatCommand(QuestDefinition questDefinition)
	{
		if (QuestManager._GlobalVillageName == null)
		{
			return true;
		}
		if (questDefinition.Name.ToString().StartsWith(QuestManager._GlobalVillageName))
		{
			QuestManager._GlobalVillageName = null;
			return true;
		}
		return false;
	}

	private bool FilterRegionHasRuins(QuestFilterRegionHasRuins questFilterRegionHasRuins, Region region)
	{
		if (region == null)
		{
			Diagnostics.LogWarning("FilterRegionHasRuins: region is null.");
			return false;
		}
		return questFilterRegionHasRuins.Check(region);
	}

	private bool FilterPositionValidForDevice(QuestFilterPositionValidForDevice questFilterPositionValidForDevice, WorldPosition position)
	{
		if (!position.IsValid)
		{
			Diagnostics.LogError("FilterPositionValidForDevice: Invalid world position.");
			return false;
		}
		if (string.IsNullOrEmpty(questFilterPositionValidForDevice.EmpireVarName))
		{
			Diagnostics.LogError("FilterPositionValidForDevice: EmpireVarName is null or empty");
			return false;
		}
		Empire empire;
		if (this.TryGetQuestVariableValueByName<Empire>(questFilterPositionValidForDevice.EmpireVarName, out empire) || this.TryGetTarget<Empire>(questFilterPositionValidForDevice.EmpireVarName, out empire))
		{
			return questFilterPositionValidForDevice.Check(position, empire);
		}
		Diagnostics.LogError("FilterPositionValidForDevice: EmpireVarName ('{0}') has not been found in quest variables (or this variable doesn't contains any Empire).", new object[]
		{
			questFilterPositionValidForDevice.EmpireVarName
		});
		return false;
	}

	private bool FilterRegionIsNamed(QuestFilterRegionIsNamed questFilterRegionIsNamed, Region region)
	{
		if (region == null)
		{
			Diagnostics.LogWarning("FilterRegionIsNamed: region is null.");
			return false;
		}
		return questFilterRegionIsNamed.Check(region);
	}

	private IQuestManagementService questManagementService;

	private List<QuestDefinition> questsCheckedThisTurn = new List<QuestDefinition>();

	private Dictionary<StaticString, KeyValuePair<int, List<SimulationObject>>> questWorldEffects = new Dictionary<StaticString, KeyValuePair<int, List<SimulationObject>>>();

	private IDatabase<SimulationDescriptor> descriptorDatabase;

	private Dictionary<ulong, Droplist> armiesKillRewards = new Dictionary<ulong, Droplist>();

	private OrderQuestWorldEffect[] questWorldEffectOrders;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private PlayerController playerController;

	private IPlayerControllerRepositoryService playerControllerRepositoryService;

	private IEndTurnControl endTurnController;

	private Dictionary<ulong, Quest> quests = new Dictionary<ulong, Quest>();

	private Dictionary<ulong, QuestBehaviour> questBehaviours = new Dictionary<ulong, QuestBehaviour>();

	private Dictionary<ulong, List<QuestMarker>> questMarkers = new Dictionary<ulong, List<QuestMarker>>();

	private Dictionary<int, List<QuestTarget>> questTargetsLocked = new Dictionary<int, List<QuestTarget>>();

	private Dictionary<StaticString, QuestOccurence> questOccurences = new Dictionary<StaticString, QuestOccurence>();

	private IEnumerable<DepartmentOfInternalAffairs> departmentsOfInternalAffairs;

	private int nextPossibleGlobalQuestTurn = -1;

	private bool isHosting;

	private float gameSpeedMultiplier;

	public static string _GlobalVillageName;

	private struct QuestBehaviourDelayedInstructions
	{
		public QuestBehaviour QuestBehaviour;

		public List<QuestInstruction> DelayedInstructions;
	}

	[CompilerGenerated]
	private sealed class UnlockQuestTarget>c__AnonStorey8D8
	{
		internal bool <>m__33D(QuestTarget match)
		{
			return match.Equals(this.questTarget);
		}

		internal QuestTarget questTarget;
	}

	[CompilerGenerated]
	private sealed class LockQuestTarget>c__AnonStorey8D9
	{
		internal bool <>m__33E(QuestTarget match)
		{
			return match.Equals(this.questTarget);
		}

		internal QuestTarget questTarget;
	}

	[CompilerGenerated]
	private sealed class IsQuestRunningForEmpire>c__AnonStorey8DA
	{
		internal bool <>m__33F(KeyValuePair<ulong, QuestBehaviour> match)
		{
			return match.Value.Initiator.Index == this.empire.Index && match.Value.Quest != null && match.Value.Quest.QuestDefinition.Name == this.questDefinitionName;
		}

		internal Empire empire;

		internal StaticString questDefinitionName;
	}

	[CompilerGenerated]
	private sealed class IsDroppableRewaredByQuestsInProgress>c__AnonStorey8DB
	{
		internal bool <>m__340(QuestBehaviour questBehaviour)
		{
			return questBehaviour.Initiator.Index == this.empire.Index && questBehaviour.Quest != null && questBehaviour.Quest.QuestRewards != null && questBehaviour.Quest.QuestState == QuestState.InProgress && questBehaviour.Quest.QuestRewards.Any((QuestReward reward) => reward.Droppables != null && reward.Droppables.Any((IDroppable droppableReward) => droppableReward.Equals(this.droppable)));
		}

		internal bool <>m__368(QuestReward reward)
		{
			return reward.Droppables != null && reward.Droppables.Any((IDroppable droppableReward) => droppableReward.Equals(this.droppable));
		}

		internal bool <>m__369(IDroppable droppableReward)
		{
			return droppableReward.Equals(this.droppable);
		}

		internal Empire empire;

		internal IDroppable droppable;
	}

	[CompilerGenerated]
	private sealed class IsALockedQuestTargetGameEntity>c__AnonStorey8DC
	{
		internal bool <>m__341(QuestTarget match)
		{
			return match.Equals(this.target);
		}

		internal bool <>m__342(QuestTarget match)
		{
			return match.Equals(this.target);
		}

		internal QuestTargetGameEntity target;
	}

	[CompilerGenerated]
	private sealed class GetGlobalQuestRank>c__AnonStorey8DE
	{
		internal Quest quest;
	}

	[CompilerGenerated]
	private sealed class GetGlobalQuestRank>c__AnonStorey8DD
	{
		internal KeyValuePair<int, int> <>m__343(Quest q)
		{
			return new KeyValuePair<int, int>(q.EmpireBits, q.GetStepProgressionValueByName(this.realStepName));
		}

		internal bool <>m__345(KeyValuePair<int, int> kvp)
		{
			return kvp.Key == this.<>f__ref$2270.quest.EmpireBits;
		}

		internal StaticString realStepName;

		internal QuestManager.GetGlobalQuestRank>c__AnonStorey8DE <>f__ref$2270;
	}

	[CompilerGenerated]
	private sealed class GetGlobalProgressionString>c__AnonStorey8DF
	{
		internal bool <>m__346(Empire empire)
		{
			return empire.Bits == this.quest.EmpireBits;
		}

		internal Quest quest;
	}

	[CompilerGenerated]
	private sealed class ComputeRewards>c__AnonStorey8E0
	{
		internal bool <>m__349(Empire emp)
		{
			return emp.Bits == this.quest.EmpireBits;
		}

		internal Quest quest;
	}

	[CompilerGenerated]
	private sealed class AddStepRewards>c__AnonStorey8E2
	{
		internal Quest quest;
	}

	[CompilerGenerated]
	private sealed class AddStepRewards>c__AnonStorey8E1
	{
		internal bool <>m__34A(QuestVariable var)
		{
			return var.Name == this.questRewardDefinition.DropVar;
		}

		internal bool <>m__34B(Empire emp)
		{
			return emp.Bits == this.<>f__ref$2274.quest.EmpireBits;
		}

		internal QuestRewardDefinition questRewardDefinition;

		internal QuestManager.AddStepRewards>c__AnonStorey8E2 <>f__ref$2274;
	}
}
