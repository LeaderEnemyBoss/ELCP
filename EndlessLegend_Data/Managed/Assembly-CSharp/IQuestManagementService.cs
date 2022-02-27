using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Simulation;

public interface IQuestManagementService : IService
{
	QuestManagerState State { get; }

	bool AddStepRewards(QuestDefinition questDefinition, int stepIndex, List<QuestReward> rewards, List<IDroppable> droppedRewards, Quest quest = null);

	List<QuestReward> ComputeRewards(Quest quest, QuestDefinition questDefinition, int stepIndex, List<IDroppable> droppedRewards = null);

	void ApplyQuestWorldEffect(OrderQuestWorldEffect order);

	void CheckForQuestCompletion(GameEntityGUID questGUID);

	void ExecuteQuest(QuestBehaviour questBehaviour);

	void ExecuteRunningQuests(Event e);

	void ForceQuestCompletion(GameEntityGUID questGUID, QuestState questState);

	StaticString ForceTrigger(QuestDefinition questDefinition, Empire empire, bool stopIfError);

	int GetGlobalQuestRank(Quest quest, ref int questHolderCount, string questStepName);

	StaticString GetGlobalProgressionString(Quest quest, StaticString questStepName);

	IEnumerable<QuestMarker> GetMarkersByQuestGUID(GameEntityGUID questGuid);

	IEnumerable<QuestMarker> GetMarkersByBoundTargetGUID(GameEntityGUID targetGameEntityGuid);

	QuestVariable GetQuestVariableByName(StaticString varName);

	void InitState(Tags tags, Empire empire, WorldPosition worldPosition);

	void InitState(StaticString eventName, Empire empire, WorldPosition worldPosition);

	bool IsALockedQuestTargetGameEntity(GameEntityGUID guid, Empire empire);

	bool IsDroppableRewaredByQuestsInProgress(Empire empire, IDroppable droppable);

	bool IsQuestRunningForEmpire(StaticString questDefinitionName, Empire empire);

	void LockQuestTarget(QuestTarget questTarget, Empire empire);

	void Register(QuestBehaviour behaviour);

	void Register(QuestMarker questMarker);

	void RegisterQuestWorldEffect(StaticString effectName, int affectEmpire, List<SimulationObject> effectSimulationObjects);

	void SendPendingInstructions(QuestBehaviour questBehaviour);

	bool Trigger(Empire empire, QuestDefinition questDefinition, QuestVariable[] questVariables, QuestInstruction[] pendingInstructions, QuestReward[] questRewards, Dictionary<Region, List<string>> regionQuestLocalizationVariableDefinitionLocalizationKey, IGameEntity questGiver = null, bool canShare = true);

	bool TryGetMarkerByGUID(GameEntityGUID markerGuid, out QuestMarker questMarker);

	bool TryTrigger(out QuestDefinition questDefinition, out QuestVariable[] questVariables, out QuestInstruction[] pendingInstructions, out Dictionary<Region, List<string>> regionQuestLocalizationVariableDefinitionLocalizationKey);

	bool TryTrigger(out QuestDefinition questDefinition, out QuestVariable[] questVariables, out QuestInstruction[] pendingInstructions, out QuestReward[] questRewards, out Dictionary<Region, List<string>> regionQuestLocalizationVariableDefinitionLocalizationKey);

	void UnlockQuestTarget(QuestTarget questTarget, Empire empire);

	void Unregister(QuestBehaviour behaviour);

	void Unregister(QuestMarker questMarker);

	void UnregisterQuestBehaviour(Quest quest);

	StaticString ForceSideQuestVillageTrigger(string sideQuestVillageName);
}
