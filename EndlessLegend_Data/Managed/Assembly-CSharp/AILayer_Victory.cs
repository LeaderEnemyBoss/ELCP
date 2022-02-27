using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Session;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

[PersonalityRegistryPath("AI/MajorEmpire/AIEntity_Empire/AILayer_Victory/", new object[]
{
	null
})]
public class AILayer_Victory : AILayerWithObjective, IXmlSerializable
{
	public AILayer_Victory() : base("Victory")
	{
		this.ActiveVictoryQuest = string.Empty;
		this.Chapter4Resource1 = string.Empty;
		this.Chapter4Resource2 = string.Empty;
		this.FocusVictoryWonder = false;
		this.TryingToBuildVictoryWonder = false;
		this.VictoryFocusMilitary = 1f;
		this.VictoryFocusDiplomacy = 1f;
		this.VictoryFocusEconomy = 1f;
		this.VictoryFocusTechnology = 1f;
		this.VictoryDiplomacyRatio = 0f;
		this.VictoryEconomicRatio = 0f;
		this.VictoryTechnologyRatio = 0f;
		this.VictoryTechnologyRatioMax = 0f;
		this.VictoryTechnologyEraMax = 0f;
		this.EarliestVictoryEvaluationTurn = 200f;
		this.currentFocusEnum = AILayer_Victory.VictoryFocus.undefined;
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.game = (service.Game as global::Game);
		this.victoryService = this.game.Services.GetService<IVictoryManagementService>();
		this.majorEmpires = Array.ConvertAll<global::Empire, MajorEmpire>(Array.FindAll<global::Empire>(this.game.Empires, (global::Empire empire) => empire is MajorEmpire), (global::Empire empire) => empire as MajorEmpire);
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.departmentOfForeignAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		this.departmentOfTheTreasury = base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>();
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		this.departmentOfIndustry = base.AIEntity.Empire.GetAgency<DepartmentOfIndustry>();
		this.departmentOfScience = base.AIEntity.Empire.GetAgency<DepartmentOfScience>();
		this.tradeManagementService = (service.Game as global::Game).Services.GetService<ITradeManagementService>();
		this.questManagementService = service.Game.Services.GetService<IQuestManagementService>();
		Diagnostics.Assert(this.questManagementService != null);
		this.questRepositoryService = service.Game.Services.GetService<IQuestRepositoryService>();
		Diagnostics.Assert(this.questRepositoryService != null);
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		Diagnostics.Assert(this.gameEntityRepositoryService != null);
		base.AIEntity.RegisterPass(AIEntity.Passes.RefreshObjectives.ToString(), "AILayer_Victory_RefreshObjectives", new AIEntity.AIAction(this.RefreshObjectives), this, new StaticString[0]);
		this.pathfindingService = service.Game.Services.GetService<IPathfindingService>();
		this.AccountManager = base.AIEntity.GetLayer<AILayer_AccountManager>();
		Diagnostics.Assert(this.AccountManager != null);
		Diagnostics.Assert(this.pathfindingService != null);
		if (Services.GetService<ISessionService>().Session.GetLobbyData<bool>("Wonder", false))
		{
			this.eventService = Services.GetService<IEventService>();
			Diagnostics.Assert(this.eventService != null);
			this.eventService.EventRaise += this.EventService_EventRaise;
			using (IEnumerator<ConstructibleElement> enumerator = Databases.GetDatabase<ConstructibleElement>(false).GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					ConstructibleElement constructibleElement = enumerator.Current;
					CityImprovementDefinition cityImprovementDefinition = constructibleElement as CityImprovementDefinition;
					if (cityImprovementDefinition != null && cityImprovementDefinition.SubCategory == "SubCategoryVictory")
					{
						this.VictoryWonder = cityImprovementDefinition;
						yield break;
					}
				}
				yield break;
			}
		}
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public override IEnumerator Load()
	{
		yield return base.Load();
		yield break;
	}

	public override void Release()
	{
		base.Release();
		this.worldPositionningService = null;
		this.departmentOfForeignAffairs = null;
		this.questManagementService = null;
		this.gameEntityRepositoryService = null;
		this.questRepositoryService = null;
		this.pathfindingService = null;
		this.departmentOfTheTreasury = null;
		this.departmentOfScience = null;
		this.departmentOfTheInterior = null;
		this.departmentOfIndustry = null;
		this.tradeManagementService = null;
		this.Chapter4Resource1 = string.Empty;
		this.Chapter4Resource2 = string.Empty;
		if (this.eventService != null)
		{
			this.eventService.EventRaise -= this.EventService_EventRaise;
			this.eventService = null;
		}
		this.VictoryWonder = null;
		this.WonderCity = null;
		this.AccountManager = null;
		this.questBehaviour = null;
		this.game = null;
		this.majorEmpires = null;
		this.victoryService = null;
	}

	protected override int GetCommanderLimit()
	{
		return 1;
	}

	protected override bool IsObjectiveValid(GlobalObjectiveMessage objective)
	{
		if (this.ActiveVictoryQuest == string.Empty)
		{
			return false;
		}
		float num = 0f;
		float num2 = 0f;
		if (this.ActiveVictoryQuest == "VictoryQuest-Chapter4")
		{
			if (this.questBehaviour.Quest.GetCurrentStepIndex() == 0)
			{
				if (this.Chapter4Resource1 == string.Empty || this.Chapter4Resource2 == string.Empty)
				{
					return false;
				}
				if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, this.Chapter4Resource1, out num, false))
				{
					num = 0f;
				}
				if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, this.Chapter4Resource2, out num2, false))
				{
					num2 = 0f;
				}
				float num3;
				if (!this.departmentOfTheTreasury.TryGetNetResourceValue(base.AIEntity.Empire.SimulationObject, this.Chapter4Resource1, out num3, false))
				{
					num3 = 0f;
				}
				float num4;
				if (!this.departmentOfTheTreasury.TryGetNetResourceValue(base.AIEntity.Empire.SimulationObject, this.Chapter4Resource2, out num4, false))
				{
					num4 = 0f;
				}
				float num5 = 0f;
				float num6 = 0f;
				if (this.departmentOfScience.CanTradeResourcesAndBoosters(false))
				{
					TradableResource tradableResource = this.TryGetTradableRessource(this.Chapter4Resource1);
					TradableResource tradableResource2 = this.TryGetTradableRessource(this.Chapter4Resource2);
					num5 = ((tradableResource == null) ? 0f : tradableResource.Quantity);
					num6 = ((tradableResource2 == null) ? 0f : tradableResource2.Quantity);
				}
				if (num + num3 + num5 < (float)this.Chapter4Resource1Amount || num2 + num4 + num6 < (float)this.Chapter4Resource2Amount)
				{
					return false;
				}
			}
			else
			{
				QuestBehaviourTreeNode_Decorator_TimerEnded questBehaviourTreeNode_Decorator_TimerEnded;
				if (!ELCPUtilities.TryGetFirstNodeOfType<QuestBehaviourTreeNode_Decorator_TimerEnded>(this.questBehaviour.Root as BehaviourTreeNodeController, out questBehaviourTreeNode_Decorator_TimerEnded))
				{
					return false;
				}
				int num7 = (int)this.questBehaviour.GetQuestVariableByName(questBehaviourTreeNode_Decorator_TimerEnded.TimerVarName).Object;
				if (this.game.Turn - num7 < questBehaviourTreeNode_Decorator_TimerEnded.TurnCountBeforeTimeOut)
				{
					return false;
				}
			}
		}
		if (objective.ObjectiveType == base.ObjectiveType && objective.SubObjectifGUID.IsValid)
		{
			foreach (QuestMarker questMarker in this.questManagementService.GetMarkersByBoundTargetGUID(objective.SubObjectifGUID))
			{
				Quest quest;
				IGameEntity gameEntity;
				if (this.questRepositoryService.TryGetValue(questMarker.QuestGUID, out quest) && quest.QuestDefinition.Name == this.ActiveVictoryQuest && quest.EmpireBits == base.AIEntity.Empire.Bits && (this.ActiveVictoryQuest != "VictoryQuest-Chapter3" || questMarker.IsVisibleInFogOfWar) && this.gameEntityRepositoryService.TryGetValue(objective.SubObjectifGUID, out gameEntity) && gameEntity is PointOfInterest && this.departmentOfForeignAffairs.CanMoveOn((gameEntity as PointOfInterest).Region.Index, false) && this.IsFreeOfBloomsOrFriendly(gameEntity as PointOfInterest))
				{
					if (this.ActiveVictoryQuest == "VictoryQuest-Chapter4" && this.departmentOfScience.CanTradeResourcesAndBoosters(false) && this.questBehaviour.Quest.GetCurrentStepIndex() == 0)
					{
						this.NeedSyncJob = true;
					}
					return true;
				}
			}
			return false;
		}
		return false;
	}

	protected override bool IsObjectiveValid(StaticString objectiveType, int regionIndex, bool checkLocalPriority = false)
	{
		return true;
	}

	protected override void RefreshObjectives(StaticString context, StaticString pass)
	{
		this.currentVictoryDesign = AILayer_Victory.VictoryDesign.none;
		this.NeedSyncJob = false;
		this.ActiveVictoryQuest = string.Empty;
		this.questBehaviour = null;
		if (this.questManagementService.IsQuestRunningForEmpire("VictoryQuest-Chapter3", base.AIEntity.Empire))
		{
			this.ActiveVictoryQuest = "VictoryQuest-Chapter3";
			this.questBehaviour = this.questRepositoryService.GetQuestBehaviour("VictoryQuest-Chapter3", base.AIEntity.Empire.Index);
		}
		else if (this.questManagementService.IsQuestRunningForEmpire("VictoryQuest-Chapter4", base.AIEntity.Empire))
		{
			this.ActiveVictoryQuest = "VictoryQuest-Chapter4";
			this.questBehaviour = this.questRepositoryService.GetQuestBehaviour("VictoryQuest-Chapter4", base.AIEntity.Empire.Index);
		}
		else if (this.questManagementService.IsQuestRunningForEmpire("VictoryQuest-Chapter5Alt", base.AIEntity.Empire))
		{
			this.ActiveVictoryQuest = "VictoryQuest-Chapter5Alt";
			this.currentVictoryDesign = AILayer_Victory.VictoryDesign.Preacher;
			this.questBehaviour = this.questRepositoryService.GetQuestBehaviour("VictoryQuest-Chapter5Alt", base.AIEntity.Empire.Index);
		}
		else if (this.questManagementService.IsQuestRunningForEmpire("VictoryQuest-Chapter5", base.AIEntity.Empire))
		{
			this.ActiveVictoryQuest = "VictoryQuest-Chapter5";
			this.currentVictoryDesign = AILayer_Victory.VictoryDesign.Settler;
			this.questBehaviour = this.questRepositoryService.GetQuestBehaviour("VictoryQuest-Chapter5", base.AIEntity.Empire.Index);
		}
		else if (this.questManagementService.IsQuestRunningForEmpire("VictoryQuest-Chapter5Alt2", base.AIEntity.Empire))
		{
			this.ActiveVictoryQuest = "VictoryQuest-Chapter5Alt2";
			this.currentVictoryDesign = AILayer_Victory.VictoryDesign.Gorgon;
			this.questBehaviour = this.questRepositoryService.GetQuestBehaviour("VictoryQuest-Chapter5", base.AIEntity.Empire.Index);
		}
		if (this.ActiveVictoryQuest != "VictoryQuest-Chapter4" || this.questBehaviour.Quest.GetCurrentStepIndex() > 0)
		{
			this.Chapter4Resource1 = string.Empty;
			this.Chapter4Resource2 = string.Empty;
		}
		base.RefreshObjectives(context, pass);
		base.GatherObjectives(AICommanderMissionDefinition.AICommanderCategory.Victory.ToString(), ref this.globalObjectiveMessages);
		base.ValidateMessages(ref this.globalObjectiveMessages);
		if (this.ActiveVictoryQuest != string.Empty && this.globalObjectiveMessages.Count == 0)
		{
			if (this.ActiveVictoryQuest == "VictoryQuest-Chapter3")
			{
				this.TryGenerateChapter3Objective();
			}
			else if (this.ActiveVictoryQuest == "VictoryQuest-Chapter4")
			{
				this.TryGenerateChapter4Objective();
			}
			else if (this.ActiveVictoryQuest.Contains("VictoryQuest-Chapter5"))
			{
				this.TryGenerateChapter5Objective();
			}
		}
		foreach (GlobalObjectiveMessage globalObjectiveMessage in this.globalObjectiveMessages)
		{
			base.GlobalPriority.Reset();
			base.GlobalPriority.Add(1f, "(constant) always high priority", new object[0]);
			Region region = this.worldPositionningService.GetRegion(globalObjectiveMessage.RegionIndex);
			float num = 1f;
			if (region.Owner != null && region.Owner is MajorEmpire && region.Owner.Index != base.AIEntity.Empire.Index && (this.departmentOfForeignAffairs.IsAtWarWith(region.Owner) || (!this.departmentOfForeignAffairs.IsFriend(region.Owner) && this.departmentOfForeignAffairs.IsInWarWithSomeone())))
			{
				num = base.AIEntity.Empire.GetPropertyValue(SimulationProperties.LandMilitaryPower) / region.Owner.GetPropertyValue(SimulationProperties.LandMilitaryPower);
				num = Mathf.Min(num / 1.2f, 1f);
			}
			globalObjectiveMessage.GlobalPriority.Value = 1f;
			globalObjectiveMessage.LocalPriority.Value = num;
			globalObjectiveMessage.TimeOut = 1;
		}
		if (this.FocusVictoryWonder)
		{
			this.TryFocusVictoryWonder();
		}
		if (this.NeedSyncJob)
		{
			AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>().RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_BuyVictoryResources));
		}
		if ((float)this.game.Turn >= this.EarliestVictoryEvaluationTurn && (this.currentFocusEnum == AILayer_Victory.VictoryFocus.undefined || this.game.Turn % 8 == base.AIEntity.Empire.Index || this.game.Turn % 8 + 8 == base.AIEntity.Empire.Index) && base.AIEntity.Empire.SimulationObject.GetPropertyValue(SimulationProperties.CurrentEra) > 3f)
		{
			this.ComputeVictoryFocus();
		}
	}

	private bool POIAccessible(PointOfInterest POI)
	{
		PathfindingContext pathfindingContext = new PathfindingContext(GameEntityGUID.Zero, base.AIEntity.Empire, PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.FrozenWater);
		pathfindingContext.RefreshProperties(1f, float.PositiveInfinity, false, false, float.PositiveInfinity, float.PositiveInfinity);
		foreach (WorldPosition worldPosition in WorldPosition.GetDirectNeighbourTiles(POI.WorldPosition))
		{
			if ((!this.worldPositionningService.IsWaterTile(worldPosition) || this.worldPositionningService.IsFrozenWaterTile(worldPosition)) && this.pathfindingService.IsTileStopable(worldPosition, pathfindingContext, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreFogOfWar, null) && this.pathfindingService.IsTransitionPassable(worldPosition, POI.WorldPosition, pathfindingContext, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnorePOI, null))
			{
				return true;
			}
		}
		return false;
	}

	private void TryGenerateChapter3Objective()
	{
		List<QuestBehaviourTreeNode_Action_AddQuestMarker> list = new List<QuestBehaviourTreeNode_Action_AddQuestMarker>(Array.FindAll<BehaviourTreeNode>((this.questBehaviour.Root as BehaviourTreeNodeController).Children, (BehaviourTreeNode x) => x is QuestBehaviourTreeNode_Action_AddQuestMarker).Cast<QuestBehaviourTreeNode_Action_AddQuestMarker>());
		List<PointOfInterest> list2 = new List<PointOfInterest>();
		foreach (QuestBehaviourTreeNode_Action_AddQuestMarker questBehaviourTreeNode_Action_AddQuestMarker in list)
		{
			QuestMarker questMarker = null;
			if (this.questManagementService.TryGetMarkerByGUID(questBehaviourTreeNode_Action_AddQuestMarker.QuestMarkerGUID, out questMarker) && questMarker.IsVisibleInFogOfWar)
			{
				PointOfInterest pointOfInterest = this.worldPositionningService.GetPointOfInterest(questMarker.WorldPosition);
				if (pointOfInterest != null && this.IsFreeOfBloomsOrFriendly(pointOfInterest) && this.departmentOfForeignAffairs.CanMoveOn(pointOfInterest.Region.Index, false) && (pointOfInterest.Region.Owner == null || pointOfInterest.Region.Owner.Index == base.AIEntity.Empire.Index || !(pointOfInterest.Region.Owner is MajorEmpire) || this.departmentOfForeignAffairs.DiplomaticRelations[pointOfInterest.Region.City.Empire.Index].State.Name == DiplomaticRelationState.Names.War || this.POIAccessible(pointOfInterest)))
				{
					list2.Add(pointOfInterest);
				}
			}
		}
		if (list2.Count > 0)
		{
			List<PointOfInterest> list3 = list2.FindAll((PointOfInterest x) => x.Region != null && x.Region.Owner != null && x.Region.Owner.Index != base.AIEntity.Empire.Index);
			if (list3.Count == 0)
			{
				list3 = list2.FindAll((PointOfInterest x) => x.Region != null && (x.Region.Owner == null || !(x.Region.Owner is MajorEmpire)));
			}
			if (list3.Count == 0)
			{
				list3 = list2;
			}
			int index = new System.Random().Next(list3.Count);
			GlobalObjectiveMessage globalObjectiveMessage = base.GenerateObjective(list3[index].Region.Index);
			globalObjectiveMessage.SubObjectifGUID = list3[index].GUID;
			this.globalObjectiveMessages.Add(globalObjectiveMessage);
		}
	}

	private void TryGenerateChapter4Objective()
	{
		if (this.questBehaviour == null)
		{
			return;
		}
		bool flag = this.questBehaviour.Quest.GetCurrentStepIndex() == 0;
		if (flag)
		{
			if (this.Chapter4Resource1 == string.Empty || this.Chapter4Resource2 == string.Empty)
			{
				int num = 0;
				QuestBehaviourTreeNode_ConditionCheck_HasResourceAmount questBehaviourTreeNode_ConditionCheck_HasResourceAmount;
				if (!ELCPUtilities.TryGetNodeOfType<QuestBehaviourTreeNode_ConditionCheck_HasResourceAmount>(this.questBehaviour.Root as BehaviourTreeNodeController, out questBehaviourTreeNode_ConditionCheck_HasResourceAmount, ref num, 1))
				{
					return;
				}
				num = 0;
				QuestBehaviourTreeNode_ConditionCheck_HasResourceAmount questBehaviourTreeNode_ConditionCheck_HasResourceAmount2;
				if (!ELCPUtilities.TryGetNodeOfType<QuestBehaviourTreeNode_ConditionCheck_HasResourceAmount>(this.questBehaviour.Root as BehaviourTreeNodeController, out questBehaviourTreeNode_ConditionCheck_HasResourceAmount2, ref num, 2))
				{
					return;
				}
				this.Chapter4Resource1 = questBehaviourTreeNode_ConditionCheck_HasResourceAmount.ResourceName;
				this.Chapter4Resource1Amount = questBehaviourTreeNode_ConditionCheck_HasResourceAmount.WantedAmount;
				this.Chapter4Resource2 = questBehaviourTreeNode_ConditionCheck_HasResourceAmount2.ResourceName;
				this.Chapter4Resource2Amount = questBehaviourTreeNode_ConditionCheck_HasResourceAmount2.WantedAmount;
			}
			float num2;
			if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, this.Chapter4Resource1, out num2, false))
			{
				num2 = 0f;
			}
			float num3;
			if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, this.Chapter4Resource2, out num3, false))
			{
				num3 = 0f;
			}
			float num4;
			if (!this.departmentOfTheTreasury.TryGetNetResourceValue(base.AIEntity.Empire.SimulationObject, this.Chapter4Resource1, out num4, false))
			{
				num4 = 0f;
			}
			float num5;
			if (!this.departmentOfTheTreasury.TryGetNetResourceValue(base.AIEntity.Empire.SimulationObject, this.Chapter4Resource2, out num5, false))
			{
				num5 = 0f;
			}
			float num6 = 0f;
			float num7 = 0f;
			if (this.departmentOfScience.CanTradeResourcesAndBoosters(false))
			{
				TradableResource tradableResource = this.TryGetTradableRessource(this.Chapter4Resource1);
				TradableResource tradableResource2 = this.TryGetTradableRessource(this.Chapter4Resource2);
				num6 = ((tradableResource == null) ? 0f : tradableResource.Quantity);
				num7 = ((tradableResource2 == null) ? 0f : tradableResource2.Quantity);
			}
			if (num2 + num4 + num6 < (float)this.Chapter4Resource1Amount || num3 + num5 + num7 < (float)this.Chapter4Resource2Amount)
			{
				return;
			}
		}
		else
		{
			QuestBehaviourTreeNode_Decorator_TimerEnded questBehaviourTreeNode_Decorator_TimerEnded;
			if (!ELCPUtilities.TryGetFirstNodeOfType<QuestBehaviourTreeNode_Decorator_TimerEnded>(this.questBehaviour.Root as BehaviourTreeNodeController, out questBehaviourTreeNode_Decorator_TimerEnded))
			{
				return;
			}
			int num8 = (int)this.questBehaviour.GetQuestVariableByName(questBehaviourTreeNode_Decorator_TimerEnded.TimerVarName).Object;
			if (this.game.Turn - num8 < questBehaviourTreeNode_Decorator_TimerEnded.TurnCountBeforeTimeOut)
			{
				return;
			}
		}
		List<QuestBehaviourTreeNode_Action_AddQuestMarker> list = new List<QuestBehaviourTreeNode_Action_AddQuestMarker>(Array.FindAll<BehaviourTreeNode>((this.questBehaviour.Root as BehaviourTreeNodeController).Children, (BehaviourTreeNode x) => x is QuestBehaviourTreeNode_Action_AddQuestMarker).Cast<QuestBehaviourTreeNode_Action_AddQuestMarker>());
		List<PointOfInterest> list2 = new List<PointOfInterest>();
		foreach (QuestBehaviourTreeNode_Action_AddQuestMarker questBehaviourTreeNode_Action_AddQuestMarker in list)
		{
			QuestMarker questMarker = null;
			if (this.questManagementService.TryGetMarkerByGUID(questBehaviourTreeNode_Action_AddQuestMarker.QuestMarkerGUID, out questMarker))
			{
				PointOfInterest pointOfInterest = this.worldPositionningService.GetPointOfInterest(questMarker.WorldPosition);
				if (pointOfInterest != null && this.IsFreeOfBloomsOrFriendly(pointOfInterest) && this.departmentOfForeignAffairs.CanMoveOn(pointOfInterest.Region.Index, false) && (pointOfInterest.Region.Owner == null || pointOfInterest.Region.Owner.Index == base.AIEntity.Empire.Index || !(pointOfInterest.Region.Owner is MajorEmpire) || this.departmentOfForeignAffairs.DiplomaticRelations[pointOfInterest.Region.City.Empire.Index].State.Name == DiplomaticRelationState.Names.War || this.POIAccessible(pointOfInterest)))
				{
					list2.Add(pointOfInterest);
				}
			}
		}
		if (list2.Count > 0)
		{
			GlobalObjectiveMessage globalObjectiveMessage = base.GenerateObjective(list2[0].Region.Index);
			globalObjectiveMessage.SubObjectifGUID = list2[0].GUID;
			this.globalObjectiveMessages.Add(globalObjectiveMessage);
			if (flag && this.departmentOfScience.CanTradeResourcesAndBoosters(false))
			{
				this.NeedSyncJob = true;
			}
		}
	}

	private TradableResource TryGetTradableRessource(StaticString resourceName)
	{
		List<ITradable> list;
		this.tradeManagementService.TryGetTradables("TradableResource" + resourceName, out list);
		for (int i = 0; i < list.Count; i++)
		{
			TradableResource tradableResource = list[i] as TradableResource;
			if (tradableResource.ResourceName == resourceName)
			{
				return tradableResource;
			}
		}
		return null;
	}

	private void EventService_EventRaise(object sender, EventRaiseEventArgs e)
	{
		EventVictoryConditionAlert eventVictoryConditionAlert = e.RaisedEvent as EventVictoryConditionAlert;
		if (eventVictoryConditionAlert == null)
		{
			return;
		}
		if (!this.IsActive())
		{
			return;
		}
		if (eventVictoryConditionAlert.Empire.Index == base.AIEntity.Empire.Index && eventVictoryConditionAlert.VictoryCondition.Name == ELCPUtilities.AIVictoryFocus.Technology && this.currentFocusEnum != AILayer_Victory.VictoryFocus.MostTechnologiesDiscovered && this.currentFocusEnum != AILayer_Victory.VictoryFocus.Economy && this.ComputeVictoryFocus_AmITheTechleader(eventVictoryConditionAlert.VictoryCondition))
		{
			this.currentFocusEnum = AILayer_Victory.VictoryFocus.MostTechnologiesDiscovered;
		}
		if (eventVictoryConditionAlert.Empire.Index == base.AIEntity.Empire.Index || this.FocusVictoryWonder)
		{
			return;
		}
		this.FocusVictoryWonder = true;
	}

	public override void WriteXml(XmlWriter writer)
	{
		if (writer.WriteVersionAttribute(3) >= 3)
		{
			writer.WriteAttributeString<bool>("FocusVictoryWonder", this.FocusVictoryWonder);
			writer.WriteAttributeString<string>("CurrentFocus", this.currentFocusEnum.ToString());
		}
		base.WriteXml(writer);
	}

	public override void ReadXml(XmlReader reader)
	{
		if (reader.ReadVersionAttribute() > 2)
		{
			this.FocusVictoryWonder = reader.GetAttribute<bool>("FocusVictoryWonder", false);
			string text = reader.GetAttribute<string>("CurrentFocus", string.Empty);
			if (text == string.Empty)
			{
				text = "undefined";
			}
			this.currentFocusEnum = (AILayer_Victory.VictoryFocus)Enum.Parse(typeof(AILayer_Victory.VictoryFocus), text);
		}
		base.ReadXml(reader);
	}

	private void TryFocusVictoryWonder()
	{
		if (this.departmentOfTheInterior.NonInfectedCities.Count == 0 || this.VictoryWonder == null || !DepartmentOfTheTreasury.CheckConstructiblePrerequisites(this.departmentOfTheInterior.NonInfectedCities[0], this.VictoryWonder, new string[]
		{
			ConstructionFlags.Prerequisite
		}))
		{
			this.TryingToBuildVictoryWonder = false;
			return;
		}
		for (int i = 0; i < this.departmentOfTheInterior.NonInfectedCities.Count; i++)
		{
			if (this.departmentOfIndustry.GetConstructionQueue(this.departmentOfTheInterior.NonInfectedCities[i]).Contains((Construction x) => x.ConstructibleElement.SubCategory == "SubCategoryVictory"))
			{
				this.TryingToBuildVictoryWonder = false;
				return;
			}
		}
		List<City> list = this.departmentOfTheInterior.NonInfectedCities.ToList<City>();
		list.Sort(delegate(City left, City right)
		{
			float productionCostWithBonus = DepartmentOfTheTreasury.GetProductionCostWithBonus(left, this.VictoryWonder, "Production");
			float productionCostWithBonus2 = DepartmentOfTheTreasury.GetProductionCostWithBonus(right, this.VictoryWonder, "Production");
			float num4 = productionCostWithBonus / left.GetPropertyValue(SimulationProperties.NetCityProduction);
			float value = productionCostWithBonus2 / right.GetPropertyValue(SimulationProperties.NetCityProduction);
			return num4.CompareTo(value);
		});
		int num = -1;
		for (int j = 0; j < list.Count; j++)
		{
			if (list[j].BesiegingEmpireIndex < 0 && AILayer_Military.AreaIsSave(list[j].WorldPosition, 5, this.departmentOfForeignAffairs, false) && DepartmentOfTheTreasury.GetProductionCostWithBonus(list[j], this.VictoryWonder, "Production") / list[j].GetPropertyValue(SimulationProperties.NetCityProduction) < 50f)
			{
				num = j;
				break;
			}
		}
		if (num >= 0)
		{
			this.WonderCity = list[num];
			this.TryingToBuildVictoryWonder = true;
			float num2;
			if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, "Strategic5", out num2, false))
			{
				num2 = 0f;
			}
			float num3;
			if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, "Strategic6", out num3, false))
			{
				num3 = 0f;
			}
			if (num2 < 40f || num3 < 40f)
			{
				if (this.departmentOfScience.CanTradeResourcesAndBoosters(false))
				{
					this.NeedSyncJob = true;
					return;
				}
			}
			else
			{
				OrderQueueConstruction order = new OrderQueueConstruction(base.AIEntity.Empire.Index, this.WonderCity.GUID, this.VictoryWonder, string.Empty);
				Ticket ticket;
				base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderQueue_TicketRaised));
			}
			return;
		}
		this.TryingToBuildVictoryWonder = false;
	}

	private void OrderQueue_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		if (e.Result == PostOrderResponse.Processed)
		{
			this.TryingToBuildVictoryWonder = false;
		}
	}

	private bool VictoryResourceTradingOrder(string resource, float Amount)
	{
		TradableResource tradableResource = this.TryGetTradableRessource(resource);
		if (tradableResource == null || tradableResource.Quantity < Amount)
		{
			return false;
		}
		float quantity = Math.Min(10f, Amount);
		float priceWithSalesTaxes = TradableResource.GetPriceWithSalesTaxes(resource, TradableTransactionType.Buyout, base.AIEntity.Empire, quantity);
		if (this.AccountManager.TryMakeUnexpectedImmediateExpense(AILayer_AccountManager.EconomyAccountName, priceWithSalesTaxes, 1f))
		{
			Ticket ticket;
			base.AIEntity.Empire.PlayerControllers.AI.PostOrder(new OrderBuyoutTradable(base.AIEntity.Empire.Index, tradableResource.UID, quantity), out ticket, new EventHandler<TicketRaisedEventArgs>(this.ResourceTradingOrder_TicketRaised));
			return true;
		}
		return false;
	}

	private void ResourceTradingOrder_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		if (e.Result == PostOrderResponse.Processed)
		{
			this.SynchronousJob_BuyVictoryResources();
			return;
		}
	}

	public SynchronousJobState SynchronousJob_BuyVictoryResources()
	{
		if (base.AIEntity.Empire.GetPropertyValue(SimulationProperties.NetEmpireMoney) > 0f && this.departmentOfScience.CanTradeResourcesAndBoosters(false))
		{
			float num = 0f;
			if (this.Chapter4Resource1 != string.Empty && this.Chapter4Resource2 != string.Empty)
			{
				if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, this.Chapter4Resource1, out num, false))
				{
					num = 0f;
				}
				if (num < (float)this.Chapter4Resource1Amount && this.VictoryResourceTradingOrder(this.Chapter4Resource1, (float)this.Chapter4Resource1Amount - num))
				{
					return SynchronousJobState.Success;
				}
				if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, this.Chapter4Resource2, out num, false))
				{
					num = 0f;
				}
				if (num < (float)this.Chapter4Resource2Amount && this.VictoryResourceTradingOrder(this.Chapter4Resource2, (float)this.Chapter4Resource2Amount - num))
				{
					return SynchronousJobState.Success;
				}
			}
			if (this.TryingToBuildVictoryWonder)
			{
				if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, "Strategic5", out num, false))
				{
					num = 0f;
				}
				if (num < 40f && this.VictoryResourceTradingOrder("Strategic5", 40f - num))
				{
					return SynchronousJobState.Success;
				}
				float num2;
				if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, "Strategic6", out num2, false))
				{
					num2 = 0f;
				}
				if (num2 < 40f && this.VictoryResourceTradingOrder("Strategic6", 40f - num2))
				{
					return SynchronousJobState.Success;
				}
				if (this.WonderCity != null && this.WonderCity.GUID.IsValid && this.WonderCity.Empire.Index == base.AIEntity.Empire.Index && this.WonderCity.BesiegingEmpire == null && num2 >= 40f && num >= 40f)
				{
					OrderQueueConstruction order = new OrderQueueConstruction(base.AIEntity.Empire.Index, this.WonderCity.GUID, this.VictoryWonder, string.Empty);
					Ticket ticket;
					base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderQueue_TicketRaised));
				}
			}
		}
		return SynchronousJobState.Success;
	}

	private void TryGenerateChapter5Objective()
	{
		if (this.questBehaviour == null)
		{
			return;
		}
		List<QuestBehaviourTreeNode_Action_AddQuestMarker> list = new List<QuestBehaviourTreeNode_Action_AddQuestMarker>(Array.FindAll<BehaviourTreeNode>((this.questBehaviour.Root as BehaviourTreeNodeController).Children, (BehaviourTreeNode x) => x is QuestBehaviourTreeNode_Action_AddQuestMarker).Cast<QuestBehaviourTreeNode_Action_AddQuestMarker>());
		List<PointOfInterest> list2 = new List<PointOfInterest>();
		foreach (QuestBehaviourTreeNode_Action_AddQuestMarker questBehaviourTreeNode_Action_AddQuestMarker in list)
		{
			QuestMarker questMarker = null;
			if (this.questManagementService.TryGetMarkerByGUID(questBehaviourTreeNode_Action_AddQuestMarker.QuestMarkerGUID, out questMarker))
			{
				PointOfInterest pointOfInterest = this.worldPositionningService.GetPointOfInterest(questMarker.WorldPosition);
				if (pointOfInterest != null && this.IsFreeOfBloomsOrFriendly(pointOfInterest) && this.departmentOfForeignAffairs.CanMoveOn(pointOfInterest.Region.Index, false) && (pointOfInterest.Region.Owner == null || pointOfInterest.Region.Owner.Index == base.AIEntity.Empire.Index || !(pointOfInterest.Region.Owner is MajorEmpire) || this.departmentOfForeignAffairs.DiplomaticRelations[pointOfInterest.Region.City.Empire.Index].State.Name == DiplomaticRelationState.Names.War || this.POIAccessible(pointOfInterest)))
				{
					list2.Add(pointOfInterest);
				}
			}
		}
		if (list2.Count > 0)
		{
			GlobalObjectiveMessage globalObjectiveMessage = base.GenerateObjective(list2[0].Region.Index);
			globalObjectiveMessage.SubObjectifGUID = list2[0].GUID;
			this.globalObjectiveMessages.Add(globalObjectiveMessage);
		}
	}

	private void ComputeVictoryFocus()
	{
		MajorEmpire majorEmpire = base.AIEntity.Empire as MajorEmpire;
		if (majorEmpire == null)
		{
			return;
		}
		float num = -1f;
		float num2 = float.MinValue;
		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		foreach (VictoryCondition victoryCondition in this.victoryService.VictoryConditionsFilteredThisGame)
		{
			if (victoryCondition.Name == ELCPUtilities.AIVictoryFocus.Diplomacy || victoryCondition.Name == ELCPUtilities.AIVictoryFocus.Economy)
			{
				float num3 = 1f;
				for (int i = 0; i < victoryCondition.Progression.Vars.Length; i++)
				{
					if (victoryCondition.Progression.Vars[i].Name == "TargetValue" && victoryCondition.Name == ELCPUtilities.AIVictoryFocus.Diplomacy)
					{
						num3 = majorEmpire.VictoryConditionStatuses["Diplomacy"].Variables[i];
					}
					else if (victoryCondition.Progression.Vars[i].Name == victoryCondition.Progression.SortVariable)
					{
						foreach (MajorEmpire majorEmpire2 in this.majorEmpires)
						{
							if (majorEmpire2.VictoryConditionStatuses.ContainsKey(victoryCondition.Name))
							{
								float num4 = majorEmpire2.VictoryConditionStatuses[victoryCondition.Name].Variables[i];
								if (num4 > num)
								{
									num = num4;
								}
								if (majorEmpire2.Index == majorEmpire.Index)
								{
									num2 = num4;
								}
							}
						}
						if (num2 == num)
						{
							list.Add(victoryCondition.Name.ToString());
						}
						else if (num2 >= num * 0.75f)
						{
							list2.Add(victoryCondition.Name.ToString());
						}
					}
				}
				if (victoryCondition.Name == ELCPUtilities.AIVictoryFocus.Diplomacy)
				{
					float num5 = 0f;
					this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, out num5, false);
					float propertyValue = base.AIEntity.Empire.GetPropertyValue(SimulationProperties.EmpirePointToPeacePointFactor);
					float num6 = num5 * propertyValue / 2f;
					num6 /= num3;
					this.VictoryDiplomacyRatio = Mathf.Max(num2 + num6, 1f);
				}
				else
				{
					this.VictoryEconomicRatio = num2;
				}
			}
			else if (victoryCondition.Name == ELCPUtilities.AIVictoryFocus.Technology)
			{
				this.VictoryTechnologyRatioMax = 0f;
				this.VictoryTechnologyEraMax = 0f;
				for (int k = 0; k < victoryCondition.Progression.Vars.Length; k++)
				{
					if (victoryCondition.Progression.Vars[k].Name == victoryCondition.Progression.SortVariable)
					{
						foreach (MajorEmpire majorEmpire3 in this.majorEmpires)
						{
							if (majorEmpire3.VictoryConditionStatuses.ContainsKey(victoryCondition.Name))
							{
								float propertyValue2 = majorEmpire3.GetPropertyValue(SimulationProperties.NetEmpireResearch);
								if (propertyValue2 > num)
								{
									num = propertyValue2;
								}
								float num7 = majorEmpire3.VictoryConditionStatuses[victoryCondition.Name].Variables[k];
								if (num7 > this.VictoryTechnologyRatioMax && majorEmpire3.Index != majorEmpire.Index)
								{
									this.VictoryTechnologyRatioMax = num7;
								}
								if (majorEmpire3.Index == majorEmpire.Index)
								{
									this.VictoryTechnologyRatio = num7;
									num2 = propertyValue2;
								}
								float propertyValue3 = majorEmpire3.SimulationObject.GetPropertyValue(SimulationProperties.CurrentEra);
								if (propertyValue3 > this.VictoryTechnologyEraMax && majorEmpire3.Index != majorEmpire.Index)
								{
									this.VictoryTechnologyEraMax = propertyValue3;
								}
							}
						}
					}
				}
				if (num2 == num || (this.VictoryTechnologyRatio > 0f && this.VictoryTechnologyRatio >= this.VictoryTechnologyRatioMax))
				{
					list.Add(victoryCondition.Name.ToString());
				}
				else if (num2 >= num * 0.75f)
				{
					list2.Add(victoryCondition.Name.ToString());
				}
			}
			num = -1f;
			num2 = float.MinValue;
		}
		foreach (MajorEmpire majorEmpire4 in this.majorEmpires)
		{
			float propertyValue4 = majorEmpire4.GetPropertyValue(SimulationProperties.LandMilitaryPower);
			if (propertyValue4 > num)
			{
				num = propertyValue4;
			}
			if (majorEmpire4.Index == majorEmpire.Index)
			{
				num2 = propertyValue4;
			}
		}
		if (num2 == num)
		{
			list.Add(ELCPUtilities.AIVictoryFocus.Military);
		}
		else if (num2 >= num * 0.75f)
		{
			list2.Add(ELCPUtilities.AIVictoryFocus.Military);
		}
		List<KeyValuePair<string, float>> orderedVictoryPreferences = this.GetOrderedVictoryPreferences();
		bool flag = false;
		foreach (KeyValuePair<string, float> keyValuePair in orderedVictoryPreferences)
		{
			if (keyValuePair.Value > 0f && list.Contains(keyValuePair.Key) && this.CallFocusFunction(keyValuePair.Key, 2))
			{
				this.currentFocusEnum = (AILayer_Victory.VictoryFocus)Enum.Parse(typeof(AILayer_Victory.VictoryFocus), keyValuePair.Key);
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			foreach (KeyValuePair<string, float> keyValuePair2 in orderedVictoryPreferences)
			{
				if (keyValuePair2.Value > 1f && list2.Contains(keyValuePair2.Key) && this.CallFocusFunction(keyValuePair2.Key, 1))
				{
					this.currentFocusEnum = (AILayer_Victory.VictoryFocus)Enum.Parse(typeof(AILayer_Victory.VictoryFocus), keyValuePair2.Key);
					flag = true;
					break;
				}
			}
		}
		if (!flag)
		{
			this.currentFocusEnum = AILayer_Victory.VictoryFocus.undefined;
			return;
		}
	}

	private bool FocusMilitary(int Rank)
	{
		float num = (base.AIEntity.Empire.SimulationObject.GetPropertyValue(SimulationProperties.CurrentEra) - 3f) * 0.3f;
		float num2 = this.VictoryFocusMilitary - 1f;
		num2 = Mathf.Clamp((num2 >= 0f) ? (num2 / 1.25f) : (num2 / 10f), -0.1f, 0.9f);
		num = AILayer.Boost(num, num2);
		num = AILayer.Boost(num, (float)(Rank - 2) * 0.15f);
		if (this.currentFocusEnum == AILayer_Victory.VictoryFocus.Military || this.currentFocusEnum == AILayer_Victory.VictoryFocus.undefined)
		{
			num = AILayer.Boost(num, 0.2f);
		}
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.Log("ELCP {0} with Victory Focus: {1} with score {2}{3}", new object[]
			{
				base.AIEntity.Empire,
				ELCPUtilities.AIVictoryFocus.Military,
				num,
				(num > 0.75f) ? " - !!! Focussing !!!" : ""
			});
		}
		return num > 0.75f;
	}

	private bool FocusDiplomacy(int Rank)
	{
		float num = base.AIEntity.Empire.GetPropertyValue(SimulationProperties.PeaceCount) * 0.1f + base.AIEntity.Empire.GetPropertyValue(SimulationProperties.AllianceCount) * 0.1f;
		num = AILayer.Boost(num, Mathf.Min(1.5f * this.VictoryDiplomacyRatio, 0.8f));
		float num2 = this.VictoryFocusDiplomacy - 1f;
		num2 = Mathf.Clamp((num2 >= 0f) ? (num2 / 2f) : (num2 / 10f), -0.1f, 0.5f);
		num = AILayer.Boost(num, num2);
		num = AILayer.Boost(num, (float)Rank * 0.15f);
		if (this.currentFocusEnum == AILayer_Victory.VictoryFocus.Diplomacy || this.currentFocusEnum == AILayer_Victory.VictoryFocus.undefined)
		{
			num = AILayer.Boost(num, 0.15f);
		}
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.Log("ELCP {0} with Victory Focus: {1} with score {2}{3}", new object[]
			{
				base.AIEntity.Empire,
				ELCPUtilities.AIVictoryFocus.Diplomacy,
				num,
				(num > 0.75f) ? " - !!! Focussing !!!" : ""
			});
		}
		return num > 0.75f;
	}

	private bool FocusEconomy(int Rank)
	{
		float num = Mathf.Min(2f * this.VictoryEconomicRatio, 0.9f);
		float num2 = this.VictoryFocusEconomy - 1f;
		num2 = Mathf.Clamp((num2 >= 0f) ? (num2 / 1.5f) : (num2 / 10f), -0.1f, 0.6f);
		num = AILayer.Boost(num, num2);
		num = AILayer.Boost(num, (float)Rank * 0.15f);
		if (this.currentFocusEnum == AILayer_Victory.VictoryFocus.Economy || this.currentFocusEnum == AILayer_Victory.VictoryFocus.undefined)
		{
			num = AILayer.Boost(num, 0.15f);
		}
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.Log("ELCP {0} with Victory Focus: {1} with score {2}{3}", new object[]
			{
				base.AIEntity.Empire,
				ELCPUtilities.AIVictoryFocus.Economy,
				num,
				(num > 0.75f) ? " - !!! Focussing !!!" : ""
			});
		}
		return num > 0.75f;
	}

	private bool FocusMostTechnologiesDiscovered(int Rank)
	{
		float propertyValue = base.AIEntity.Empire.SimulationObject.GetPropertyValue(SimulationProperties.CurrentEra);
		float num = (propertyValue - 3f) * 0.25f;
		float num2 = this.VictoryFocusTechnology - 1f;
		num2 = Mathf.Clamp((num2 >= 0f) ? (num2 / 1.25f) : (num2 / 10f), -0.1f, 0.8f);
		num = AILayer.Boost(num, num2);
		if (propertyValue == 6f)
		{
			num = AILayer.Boost(num, this.VictoryTechnologyRatio - this.VictoryTechnologyRatioMax + 0.2f);
		}
		else
		{
			num = AILayer.Boost(num, (propertyValue - this.VictoryTechnologyEraMax + 1f) * 0.2f);
		}
		num = AILayer.Boost(num, (float)Rank * 0.1f);
		if (this.currentFocusEnum == AILayer_Victory.VictoryFocus.MostTechnologiesDiscovered || this.currentFocusEnum == AILayer_Victory.VictoryFocus.undefined)
		{
			num = AILayer.Boost(num, 0.1f);
		}
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.Log("ELCP {0} with Victory Focus: {1} with score {2}{3}", new object[]
			{
				base.AIEntity.Empire,
				ELCPUtilities.AIVictoryFocus.Technology,
				num,
				(num > 0.75f) ? " - !!! Focussing !!!" : ""
			});
		}
		return num > 0.75f;
	}

	private List<KeyValuePair<string, float>> GetOrderedVictoryPreferences()
	{
		List<KeyValuePair<string, float>> list = new Dictionary<string, float>
		{
			{
				ELCPUtilities.AIVictoryFocus.Military,
				this.VictoryFocusMilitary
			},
			{
				ELCPUtilities.AIVictoryFocus.Diplomacy,
				this.VictoryFocusDiplomacy
			},
			{
				ELCPUtilities.AIVictoryFocus.Economy,
				this.VictoryFocusEconomy
			},
			{
				ELCPUtilities.AIVictoryFocus.Technology,
				this.VictoryFocusTechnology
			}
		}.ToList<KeyValuePair<string, float>>();
		list.Sort(delegate(KeyValuePair<string, float> pair1, KeyValuePair<string, float> pair2)
		{
			if ((AILayer_Victory.VictoryFocus)Enum.Parse(typeof(AILayer_Victory.VictoryFocus), pair1.Key) == this.currentFocusEnum)
			{
				return -1;
			}
			if ((AILayer_Victory.VictoryFocus)Enum.Parse(typeof(AILayer_Victory.VictoryFocus), pair2.Key) == this.currentFocusEnum)
			{
				return 1;
			}
			return -1 * pair1.Value.CompareTo(pair2.Value);
		});
		return list;
	}

	private bool CallFocusFunction(string name, int Rank)
	{
		if (name == ELCPUtilities.AIVictoryFocus.Military)
		{
			return this.FocusMilitary(Rank);
		}
		if (name == ELCPUtilities.AIVictoryFocus.Diplomacy)
		{
			return this.FocusDiplomacy(Rank);
		}
		if (name == ELCPUtilities.AIVictoryFocus.Economy)
		{
			return this.FocusEconomy(Rank);
		}
		return name == ELCPUtilities.AIVictoryFocus.Technology && this.FocusMostTechnologiesDiscovered(Rank);
	}

	public string CurrentFocus
	{
		get
		{
			return this.currentFocus;
		}
	}

	public AILayer_Victory.VictoryDesign CurrentVictoryDesign
	{
		get
		{
			return this.currentVictoryDesign;
		}
	}

	private bool IsFreeOfBloomsOrFriendly(PointOfInterest pointOfInterest)
	{
		return pointOfInterest.CreepingNodeGUID == GameEntityGUID.Zero || pointOfInterest.Empire == base.AIEntity.Empire || (ELCPUtilities.UseELCPPeacefulCreepingNodes && pointOfInterest.Empire != null && pointOfInterest.Empire is MajorEmpire && this.departmentOfForeignAffairs.IsFriend(pointOfInterest.Empire));
	}

	public AILayer_Victory.VictoryFocus CurrentFocusEnum
	{
		get
		{
			return this.currentFocusEnum;
		}
	}

	private bool ComputeVictoryFocus_AmITheTechleader(VictoryCondition TechnologyCondition)
	{
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < TechnologyCondition.Progression.Vars.Length; i++)
		{
			if (TechnologyCondition.Progression.Vars[i].Name == TechnologyCondition.Progression.SortVariable)
			{
				foreach (MajorEmpire majorEmpire in this.majorEmpires)
				{
					if (majorEmpire.VictoryConditionStatuses.ContainsKey(TechnologyCondition.Name))
					{
						float num3 = majorEmpire.VictoryConditionStatuses[TechnologyCondition.Name].Variables[i];
						if (num3 > num && majorEmpire.Index != base.AIEntity.Empire.Index)
						{
							num = num3;
						}
						if (majorEmpire.Index == base.AIEntity.Empire.Index)
						{
							num2 = num3;
						}
					}
				}
			}
		}
		return num2 > num;
	}

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private IWorldPositionningService worldPositionningService;

	private IQuestManagementService questManagementService;

	private IQuestRepositoryService questRepositoryService;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private IPathfindingService pathfindingService;

	private string ActiveVictoryQuest;

	public string Chapter4Resource1;

	public string Chapter4Resource2;

	public int Chapter4Resource1Amount;

	public int Chapter4Resource2Amount;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private DepartmentOfScience departmentOfScience;

	private ITradeManagementService tradeManagementService;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private DepartmentOfIndustry departmentOfIndustry;

	private IEventService eventService;

	private bool FocusVictoryWonder;

	private CityImprovementDefinition VictoryWonder;

	public bool TryingToBuildVictoryWonder;

	private AILayer_AccountManager AccountManager;

	private City WonderCity;

	private bool NeedSyncJob;

	private QuestBehaviour questBehaviour;

	private global::Game game;

	public bool NeedPreachers;

	public bool NeedSettlers;

	private IVictoryManagementService victoryService;

	private MajorEmpire[] majorEmpires;

	[InfluencedByPersonality]
	private float VictoryFocusMilitary;

	[InfluencedByPersonality]
	private float VictoryFocusDiplomacy;

	[InfluencedByPersonality]
	private float VictoryFocusEconomy;

	[InfluencedByPersonality]
	private float VictoryFocusTechnology;

	private string currentFocus;

	private float VictoryDiplomacyRatio;

	private float VictoryEconomicRatio;

	private float VictoryTechnologyEraMax;

	private float VictoryTechnologyRatio;

	private float VictoryTechnologyRatioMax;

	[InfluencedByPersonality]
	private float EarliestVictoryEvaluationTurn;

	private AILayer_Victory.VictoryDesign currentVictoryDesign;

	private AILayer_Victory.VictoryFocus currentFocusEnum;

	public enum VictoryDesign
	{
		none,
		Settler,
		Preacher,
		Gorgon
	}

	public enum VictoryFocus
	{
		undefined,
		Economy,
		Diplomacy,
		Military,
		MostTechnologiesDiscovered
	}
}
