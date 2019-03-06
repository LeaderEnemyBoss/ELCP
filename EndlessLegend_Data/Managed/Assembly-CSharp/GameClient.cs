﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Amplitude;
using Amplitude.Extensions;
using Amplitude.Interop;
using Amplitude.IO;
using Amplitude.Unity.Audio;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Game.Orders;
using Amplitude.Unity.Localization;
using Amplitude.Unity.Messaging;
using Amplitude.Unity.Session;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Steam;
using Amplitude.Unity.Xml;
using ICSharpCode.SharpZipLib.BZip2;
using UnityEngine;

[OrderProcessor(typeof(OrderPlayBattleAction), "PlayBattleAction")]
[OrderProcessor(typeof(OrderPacifyMinorFaction), "PacifyMinorFaction")]
[OrderProcessor(typeof(OrderGetAIAttitude), "GetAIAttitude")]
[OrderProcessor(typeof(OrderReplicateMarketplaceUnitDesign), "ReplicateMarketplaceUnitDesign")]
[OrderProcessor(typeof(OrderChangeContenderState), "ChangeContenderState")]
[OrderProcessor(typeof(OrderOrbsChange), "OrbsChange")]
[OrderProcessor(typeof(OrderRemoveMapBoosts), "RemoveMapBoosts")]
[OrderProcessor(typeof(OrderNotifyEncounter), "NotifyEncounter")]
[OrderProcessor(typeof(OrderReplicateMarketplaceUnits), "ReplicateMarketplaceUnits")]
[OrderProcessor(typeof(OrderChangeDiplomaticContractState), "ChangeDiplomaticContractState")]
[OrderProcessor(typeof(OrderChangeDiplomaticContractTermsCollection), "ChangeDiplomaticContractTermsCollection")]
[OrderProcessor(typeof(OrderChangeDiplomaticRelationState), "ChangeDiplomaticRelationState")]
[OrderProcessor(typeof(OrderPrepareForBattle), "PrepareForBattle")]
[OrderProcessor(typeof(OrderChangeReinforcementPriority), "ChangeReinforcementPriority")]
[OrderProcessor(typeof(OrderGenerateNewWeather), "GenerateNewWeather")]
[OrderProcessor(typeof(OrderEndEncounter), "EndEncounter")]
[OrderProcessor(typeof(OrderEncounterTargetingPhaseUpdate), "EncounterTargetingPhaseUpdate")]
[OrderProcessor(typeof(OrderChangeAdministrationSpeciality), "ChangeAdministrationSpeciality")]
[OrderProcessor(typeof(OrderCancelKaijuResearch), "CancelKaijuResearch")]
[OrderProcessor(typeof(OrderBuyoutSpellAndPlayBattleAction), "BuyoutSpellAndPlayBattleAction")]
[OrderProcessor(typeof(OrderBuyoutAndPlaceTerraformationDevice), "BuyoutAndPlaceTerraformationDevice")]
[OrderProcessor(typeof(OrderBuyoutAndActivatePillarThroughArmy), "BuyoutAndActivatePillarThroughArmy")]
[OrderProcessor(typeof(OrderBuyoutAndActivatePillar), "BuyoutAndActivatePillar")]
[OrderProcessor(typeof(OrderChangeSeason), "ChangeSeason")]
[OrderProcessor(typeof(OrderBuyOutKaijuTechnology), "BuyOutKaijuTechnology")]
[OrderProcessor(typeof(OrderBeginQuest), "BeginQuest")]
[OrderProcessor(typeof(OrderChangeDeployment), "ChangeDeployment")]
[OrderProcessor(typeof(OrderAnnounceFirstKaiju), "AnnounceKaiju")]
[OrderProcessor(typeof(OrderAllocateEncounterDroppableTo), "AllocateEncounterDroppableTo")]
[OrderProcessor(typeof(OrderChangeContenderReinforcementRanking), "ChangeContenderReinforcementRanking")]
[OrderProcessor(typeof(OrderQuestWorldEffect), "QuestWorldEffect")]
[OrderProcessor(typeof(OrderQueueKaijuResearch), "QueueKaijuResearch")]
[OrderProcessor(typeof(OrderRazePointOfInterest), "RazePointOfInterest")]
[OrderProcessor(typeof(OrderChangeStrategy), "ChangeStrategy")]
[OrderProcessor(typeof(OrderReadyForBattle), "ReadyForBattle")]
[OrderProcessor(typeof(OrderReadyForDeployment), "ReadyForDeployment")]
[OrderProcessor(typeof(OrderReadyForNextPhase), "ReadyForNextPhase")]
[OrderProcessor(typeof(OrderReadyForNextRound), "ReadyForNextRound")]
[OrderProcessor(typeof(OrderChangeUnitDeployment), "ChangeUnitDeployment")]
[OrderProcessor(typeof(OrderChangeUnitStrategy), "ChangeUnitStrategy")]
[OrderProcessor(typeof(OrderSetOrbSpawn), "SetOrbSpawn")]
[OrderProcessor(typeof(OrderChangeUnitTargeting), "ChangeUnitTargeting")]
[OrderProcessor(typeof(OrderSetMapBoostSpawn), "SetMapBoostSpawn")]
[OrderProcessor(typeof(OrderSetEncounterDeployementEndTime), "SetEncounterDeployementEndTime")]
[OrderProcessor(typeof(OrderSetWindPreferences), "SetWindPreferences")]
[OrderProcessor(typeof(OrderChangeContenderEncounterOption), "ChangeContenderEncounterOption")]
[OrderProcessor(typeof(OrderRelocateKaiju), "RelocateKaiju")]
[OrderProcessor(typeof(OrderReplicateMarketplaceUnitProfile), "ReplicateMarketplaceUnitProfile")]
[OrderProcessor(typeof(OrderSetDeploymentFinished), "SetDeploymentFinished")]
[OrderProcessor(typeof(OrderRegisterRegionalEffects), "RegisterRegionalEffects")]
[OrderProcessor(typeof(OrderChangeUnitsTargetingAndStrategy), "OrderChangeUnitsStrategies")]
[OrderProcessor(typeof(OrderClaimDiplomacyPoints), "ClaimDiplomacyPoints")]
[OrderProcessor(typeof(OrderCompleteQuest), "CompleteQuest")]
[OrderProcessor(typeof(OrderRefreshMarketplace), "RefreshMarketplace")]
[OrderProcessor(typeof(OrderWinterImmunityBid), "WinterImmunityBid")]
[OrderProcessor(typeof(OrderBeginEncounter), "BeginEncounter")]
[OrderProcessor(typeof(OrderSpawnKaiju), "SpawnKaiju")]
[OrderProcessor(typeof(OrderCreateDiplomaticContract), "CreateDiplomaticContract")]
[OrderProcessor(typeof(OrderVoteForSeasonEffect), "VoteForSeasonEffect")]
[OrderProcessor(typeof(OrderCreateEncounter), "CreateEncounter")]
[OrderProcessor(typeof(OrderReportEncounter), "ReportEncounter")]
[OrderProcessor(typeof(OrderResetPointOfInterestInteractionBits), "ResetPointOfInterestInteractionBits")]
[OrderProcessor(typeof(OrderSpawnMapBoosts), "SpawnMapBoosts")]
[OrderProcessor(typeof(OrderNotifyEmpireDiscovery), "NotifyEmpireDiscovery")]
[OrderProcessor(typeof(OrderRunTerraformationForDevice), "RunTerraformationForDevice")]
[OrderProcessor(typeof(OrderCreateCityAssaultEncounter), "CreateCityAssaultEncounter")]
[OrderProcessor(typeof(OrderDebugInfo), "DebugInfo")]
[OrderProcessor(typeof(OrderRemoveAffinityStrategicResource), "RemoveAffinityStrategicResource")]
[OrderProcessor(typeof(OrderLockInteraction), "LockInteraction")]
[OrderProcessor(typeof(OrderLockEncounterExternalArmies), "LockEncounterExternalArmies")]
[OrderProcessor(typeof(OrderDestroyEncounter), "DestroyEncounter")]
[OrderProcessor(typeof(OrderEliminateEmpire), "EliminateEmpire")]
[OrderProcessor(typeof(OrderEncounterDeploymentStart), "EncounterDeploymentStart")]
[OrderProcessor(typeof(OrderEncounterDeploymentUpdate), "EncounterDeploymentUpdate")]
[OrderProcessor(typeof(OrderEncounterRoundUpdate), "EncounterRoundUpdate")]
[OrderProcessor(typeof(OrderActivateWeatherControl), "ActivateWeatherControl")]
[OrderProcessor(typeof(OrderSelectAffinityStrategicResource), "SelectAffinityStrategicResource")]
[OrderProcessor(typeof(OrderSendAIAttitudeFeedback), "SendAIAttitudeFeedback")]
[OrderProcessor(typeof(OrderGetAIDiplomaticContractEvaluation), "GetAIDiplomaticContractEvaluation")]
[OrderProcessor(typeof(OrderGetAIDiplomaticTermEvaluation), "GetAIDiplomaticTermEvaluation")]
[OrderProcessor(typeof(OrderEncounterRoundEnd), "EncounterRoundEnd")]
[OrderProcessor(typeof(OrderIncludeContenderInEncounter), "IncludeContenderInEncounter")]
[OrderProcessor(typeof(OrderUpdateWinterImmunityBids), "UpdateWinterImmunityBids")]
[OrderProcessor(typeof(OrderUpdateQuest), "UpdateQuest")]
[OrderProcessor(typeof(OrderToggleEndlessDay), "ToggleEndlessDay")]
[OrderProcessor(typeof(OrderInteractWith), "InteractWith")]
[OrderProcessor(typeof(OrderSwapUnitDeployment), "SwapUnitDeployment")]
[OrderProcessor(typeof(OrderJoinEncounter), "JoinEncounter")]
[OrderProcessor(typeof(OrderToggleRuinDustDeposits), "ToggleRuinDustDeposits")]
[OrderProcessor(typeof(OrderSwitchContendersReinforcementRanking), "SwitchContendersReinforcementRanking")]
public class GameClient : GameInterface, IDisposable, IDumpable, IGameInterface, IGameClient
{
	public GameClient(global::Session session) : base(session)
	{
		base.FiniteStateMachine.RegisterInitialState(new GameClientState_WaitingForServer(this));
		base.FiniteStateMachine.RegisterState(new GameClientState_ConnectingToServer(this));
		base.FiniteStateMachine.RegisterState(new GameClientState_Authentication(this));
		base.FiniteStateMachine.RegisterState(new GameClientState_AuthenticationFailed(this));
		base.FiniteStateMachine.RegisterState(new GameClientState_ConnectedToServer(this));
		base.FiniteStateMachine.RegisterState(new GameClientState_DisconnectedFromServer(this));
		base.FiniteStateMachine.RegisterState(new GameClientState_DownloadGame(this));
		base.FiniteStateMachine.RegisterState(new GameClientState_LaunchGame(this));
		base.FiniteStateMachine.RegisterState(new GameClientState_GameLaunched(this));
		base.FiniteStateMachine.RegisterState(new GameClientState_GameLaunchedAndReady(this));
		base.FiniteStateMachine.RegisterState(new GameClientState_ShowScreens(this));
		base.FiniteStateMachine.RegisterState(new GameClientState_DealWithGameEndingConditions(this));
		base.FiniteStateMachine.RegisterState(new GameClientState_DealtWithGameEndingConditions(this));
		base.FiniteStateMachine.RegisterState(new GameClientState_Turn_Begin(this));
		base.FiniteStateMachine.RegisterState(new GameClientState_IntroductionVideo(this));
		base.FiniteStateMachine.RegisterState(new GameClientState_Turn_End(this));
		base.FiniteStateMachine.RegisterState(new GameClientState_Turn_Ended(this));
		base.FiniteStateMachine.RegisterState(new GameClientState_Turn_Finished(this));
		base.FiniteStateMachine.RegisterState(new GameClientState_Turn_FinishedAndLocked(this));
		base.FiniteStateMachine.RegisterState(new GameClientState_Turn_Main(this));
		base.FiniteStateMachine.RegisterState(new GameClientState_Turn_Dump(this));
		base.FiniteStateMachine.RegisterState(new GameClientState_Turn_Dump_Finished(this));
		base.FiniteStateMachine.RegisterState(new GameClientState_Defeated(this));
		this.PostStateChange(typeof(GameClientState_WaitingForServer), new object[0]);
		ISteamNetworkingService service = Services.GetService<ISteamNetworkingService>();
		service.RegisterMessageClass<GameServerInitiateConnectionResponseMessage>();
		service.RegisterMessageClass<GameServerAuthTicketResponseMessage>();
		service.RegisterMessageClass<GameServerLeaveMessage>();
		service.RegisterMessageClass<GameServerDownloadDumpRequestMessage>();
		service.RegisterMessageClass<GameServerPlayerUpdateMessage>();
		service.RegisterMessageClass<GameServerPingMessage>();
		service.RegisterMessageClass<GameServerClearOrderHistoryMessage>();
		if (Steamworks.SteamAPI.IsSteamRunning)
		{
		}
		this.endTurnService = Services.GetService<IEndTurnService>();
		Diagnostics.Assert(this.endTurnService != null);
		this.endTurnService.EndTurnValidated += this.EndTurnService_EndTurnValidated;
		this.endTurnService.RegisterCanExecuteValidator(new Func<bool>(this.CanExecuteEndTurn));
		this.endTurnService.RegisterValidator(new Func<bool, bool>(this.ValidateEndTurn));
		this.ordersHistory = new Queue<Amplitude.Unity.Game.Orders.Order>();
		base.Session.LobbyDataChange += this.Session_LobbyDatachange;
	}

	public event GameClient.ProcessingOrderEventHandler OnProcessingOrder;

	public event GameClient.SendingMessageToServerEventHandler OnSendingMessageToServer;

	public event EventHandler<GameClientConnectionStateChangeEventArgs> GameClientConnectionStateChange;

	public event GameClient.ChatMessageReceivedEventHandler OnChatMessageReceived;

	public void DumpAsText(StringBuilder content, string indent)
	{
		int num = 0;
		foreach (Amplitude.Unity.Game.Orders.Order order in this.ordersHistory)
		{
			content.AppendFormat("{0}{1:D3} {2} {3}\r\n", new object[]
			{
				indent,
				num++,
				order.GetType(),
				(!(order is global::Order)) ? string.Empty : string.Format("(Empire#{0})", ((global::Order)order).EmpireIndex.ToString())
			});
		}
		this.ordersHistory.Clear();
	}

	public byte[] DumpAsBytes()
	{
		MemoryStream memoryStream = new MemoryStream();
		using (System.IO.BinaryWriter binaryWriter = new System.IO.BinaryWriter(memoryStream))
		{
			foreach (Amplitude.Unity.Game.Orders.Order order in this.ordersHistory)
			{
				global::Order order2 = order as global::Order;
				if (order2 != null)
				{
					binaryWriter.Write(order2.EmpireIndex);
				}
				binaryWriter.Write(order.GetType().ToString());
			}
		}
		this.ordersHistory.Clear();
		byte[] result = memoryStream.ToArray();
		memoryStream.Close();
		return result;
	}

	private IEnumerator ActivateWeatherControlProcessor(OrderActivateWeatherControl order)
	{
		IWeatherService weatherService = this.Game.GetService<IWeatherService>();
		if (weatherService != null)
		{
			weatherService.UpdateWeatherControlValues(order.EmpireIndex, order.PresetName);
		}
		yield break;
	}

	private IEnumerator AllocateEncounterDroppableToProcessor(OrderAllocateEncounterDroppableTo order)
	{
		if (order.EncounterGuid.IsValid)
		{
			IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
			Diagnostics.Assert(encounterRepositoryService != null);
			Encounter encounter;
			if (encounterRepositoryService != null && encounterRepositoryService.TryGetValue(order.EncounterGuid, out encounter))
			{
				for (int empireIndex = 0; empireIndex < order.EmpireIndexes.Length; empireIndex++)
				{
					global::Empire empire = this.Game.Empires[order.EmpireIndexes[empireIndex]];
					IDroppable[] droppables = order.DroppableByEmpireIndex[empireIndex];
					if (droppables != null)
					{
						for (int index = 0; index < droppables.Length; index++)
						{
							IDroppableWithRewardAllocation droppableWithAllocation = droppables[index] as IDroppableWithRewardAllocation;
							if (droppableWithAllocation != null)
							{
								droppableWithAllocation.AllocateRewardTo(empire, new object[0]);
								encounter.RegisterRewards(empire.Index, droppables[index]);
							}
						}
					}
				}
				IGuiService guiService = Services.GetService<IGuiService>();
				if (guiService != null)
				{
					NotificationPanelEncounterEnded notificationPanelEncounterEnded = guiService.GetGuiPanel<NotificationPanelEncounterEnded>();
					if (notificationPanelEncounterEnded != null && notificationPanelEncounterEnded.Encounter != null && notificationPanelEncounterEnded.Encounter.GUID == order.EncounterGuid)
					{
						notificationPanelEncounterEnded.RefreshContent();
					}
				}
			}
		}
		else
		{
			Diagnostics.LogError("The encounter game entity guid should be valid.");
		}
		yield break;
	}

	private IEnumerator AnnounceKaijuProcessor(OrderAnnounceFirstKaiju order)
	{
		IEventService EventService = Services.GetService<IEventService>();
		if (EventService == null)
		{
			Diagnostics.LogError("Failed to retrieve the event service.");
			yield break;
		}
		foreach (global::Empire empire in (base.GameService.Game as global::Game).Empires)
		{
			if (empire is MajorEmpire)
			{
				EventService.Notify(new EventFirstKaijuAnnouncement(empire));
			}
		}
		yield break;
	}

	private IEnumerator BeginEncounterProcessor(OrderBeginEncounter order)
	{
		if (order.EncounterGUID.IsValid)
		{
			IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
			Diagnostics.Assert(encounterRepositoryService != null);
			Encounter encounter;
			if (encounterRepositoryService != null && encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
			{
				for (int contenderIndex = 0; contenderIndex < encounter.Contenders.Count; contenderIndex++)
				{
					Contender contender = encounter.Contenders[contenderIndex];
					if (contender.IsTakingPartInBattle && contender.Garrison is Army)
					{
						Army contenderArmy = contender.Garrison as Army;
						if (contenderArmy.IsEarthquaker)
						{
							contenderArmy.SetEarthquakerStatus(false, false, null);
						}
					}
				}
				if (!order.Instant && !order.Simulated)
				{
					IGameEntityRepositoryService gameEntityRepositoryService = this.Game.GetService<IGameEntityRepositoryService>();
					Diagnostics.Assert(gameEntityRepositoryService != null);
					if (gameEntityRepositoryService != null)
					{
						gameEntityRepositoryService.Register(encounter);
					}
				}
				encounter.OrderBeginEncounter = order;
				encounter.OrderCreateEncounter.EncounterMode = order.EncounterMode;
			}
		}
		else
		{
			Diagnostics.LogError("The encounter game entity guid should be valid.");
		}
		yield break;
	}

	private IEnumerator BeginQuestProcessor(OrderBeginQuest order)
	{
		if (order.QuestGUID.IsValid)
		{
			IDatabase<QuestDefinition> questDefinitions = Databases.GetDatabase<QuestDefinition>(true);
			QuestDefinition questDefinition;
			if (questDefinitions.TryGetValue(order.QuestDefinition, out questDefinition))
			{
				IQuestRepositoryService questRepositoryService = this.Game.GetService<IQuestRepositoryService>();
				Diagnostics.Assert(questRepositoryService != null);
				if (questRepositoryService != null)
				{
					Quest quest;
					if (!questRepositoryService.TryGetValue(order.QuestGUID, out quest))
					{
						quest = new Quest(order.QuestGUID, questDefinition);
						quest.EmpireBits = 1 << order.EmpireIndex;
						quest.QuestGiverGUID = order.QuestGiverGUID;
						questRepositoryService.Register(quest);
					}
					quest.QuestRewards = order.QuestRewards;
					quest.QuestState = QuestState.InProgress;
					quest.TurnWhenStarted = this.Game.Turn;
					IGameEntityRepositoryService gameEntityRepositoryService = this.Game.GetService<IGameEntityRepositoryService>();
					Diagnostics.Assert(gameEntityRepositoryService != null);
					if (gameEntityRepositoryService != null)
					{
						gameEntityRepositoryService.Register(quest);
					}
					try
					{
						global::Empire empire = this.Game.Empires[order.EmpireIndex];
						DepartmentOfInternalAffairs departmentOfInternalAffairs = empire.GetAgency<DepartmentOfInternalAffairs>();
						if (departmentOfInternalAffairs != null)
						{
							departmentOfInternalAffairs.QuestJournal.Register(quest);
						}
					}
					catch (Exception ex)
					{
						Exception exception = ex;
						Diagnostics.LogError("Unable to find the recipient (empire: #{0}). Exception: {1}, Callstack: {2}", new object[]
						{
							order.EmpireIndex,
							exception.Message,
							exception.StackTrace
						});
					}
					IEventService eventService = Services.GetService<IEventService>();
					if (eventService != null)
					{
						try
						{
							global::Empire empire2 = this.Game.Empires[order.EmpireIndex];
							eventService.Notify(new EventQuestBegun(empire2, quest));
						}
						catch
						{
						}
					}
				}
			}
		}
		else
		{
			Diagnostics.LogError("The quest entity guid should be valid.");
		}
		yield break;
	}

	private IEnumerator BuyOutKaijuTechnologyProcessor(OrderBuyOutKaijuTechnology order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (order.TechnologyName == string.Empty)
		{
			Diagnostics.LogWarning("Order preprocessing failed because the technology is not defined.");
			yield break;
		}
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		IDatabase<DepartmentOfScience.ConstructibleElement> technologyDatabase = Databases.GetDatabase<DepartmentOfScience.ConstructibleElement>(false);
		Diagnostics.Assert(technologyDatabase != null);
		IGameEntityRepositoryService gameEntityRepositoryService = gameService.Game.Services.GetService<IGameEntityRepositoryService>();
		Diagnostics.Assert(gameEntityRepositoryService != null);
		IKaijuTechsService kaijuTechsService = gameService.Game.Services.GetService<IKaijuTechsService>();
		Diagnostics.Assert(kaijuTechsService != null);
		global::Game game = gameService.Game as global::Game;
		global::Empire empire = game.Empires[order.EmpireIndex];
		if (empire == null)
		{
			yield break;
		}
		DepartmentOfScience.ConstructibleElement technology;
		if (!technologyDatabase.TryGetValue(order.TechnologyName, out technology))
		{
			yield break;
		}
		DepartmentOfTheTreasury departmentOfTheTreasury = empire.GetAgency<DepartmentOfTheTreasury>();
		for (int index = 0; index < technology.Costs.Length; index++)
		{
			if (technology.Costs[index] != null)
			{
				float cost = -technology.Costs[index].GetValue(empire.SimulationObject);
				if (cost != 0f)
				{
					if (!departmentOfTheTreasury.IsTransferOfResourcePossible(game.Empires[order.EmpireIndex], technology.Costs[index].ResourceName, ref cost))
					{
						Diagnostics.LogWarning("Order preprocessing failed because we don't have enough resources.");
						yield break;
					}
					if (!departmentOfTheTreasury.TryTransferResources(game.Empires[order.EmpireIndex], technology.Costs[index].ResourceName, cost))
					{
						Diagnostics.LogError("Order preprocessing failed because OrbUnlock costs transfer failed.");
						yield break;
					}
				}
			}
		}
		if (!(technology is KaijuTechnologyDefinition))
		{
			yield break;
		}
		kaijuTechsService.UnlockTechnology(technology, empire);
		yield break;
	}

	private IEnumerator BuyoutAndActivatePillarProcessor(OrderBuyoutAndActivatePillar order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		global::Empire empire = null;
		try
		{
			empire = this.Game.Empires[order.EmpireIndex];
		}
		catch
		{
			Diagnostics.LogError("Order processor failed because empire index is invalid.");
			yield break;
		}
		DepartmentOfTheTreasury departmentOfTheTreasury = empire.GetAgency<DepartmentOfTheTreasury>();
		if (departmentOfTheTreasury == null)
		{
			Diagnostics.LogError("Cannot retreive departmentOfTheTreasury.");
			yield break;
		}
		IDatabase<PillarDefinition> pillarDatabase = Databases.GetDatabase<PillarDefinition>(false);
		PillarDefinition pillarDefinition;
		if (pillarDatabase.TryGetValue(order.PillarDefinitionName, out pillarDefinition))
		{
			if (order.ConstructionResourceStocks != null && order.ConstructionResourceStocks.Length > 0)
			{
				for (int index = 0; index < order.ConstructionResourceStocks.Length; index++)
				{
					if (order.ConstructionResourceStocks[index] != null)
					{
						if (order.ConstructionResourceStocks[index].Stock != 0f)
						{
							if (pillarDefinition.Costs != null && index < pillarDefinition.Costs.Length)
							{
								departmentOfTheTreasury.TryTransferResources(empire, pillarDefinition.Costs[index].ResourceName, -order.ConstructionResourceStocks[index].Stock);
							}
						}
					}
				}
			}
			IPillarService pillarService = this.Game.Services.GetService<IPillarService>();
			pillarService.AddPillar(order.PillarGameEntityGUID, pillarDefinition, order.TargetPosition, empire);
		}
		yield break;
	}

	private IEnumerator BuyoutAndActivatePillarThroughArmyProcessor(OrderBuyoutAndActivatePillarThroughArmy order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		yield return this.BuyoutAndActivatePillarProcessor(order);
		if (!order.ArmyGUID.IsValid)
		{
			Diagnostics.LogError("Game entity guid is not valid: {0}.", new object[]
			{
				order.ArmyGUID.ToString()
			});
			yield break;
		}
		global::Empire empire = null;
		try
		{
			empire = this.Game.Empires[order.EmpireIndex];
			Army army = empire.GetAgency<DepartmentOfDefense>().GetArmy(order.ArmyGUID);
			if (army != null)
			{
				ArmyAction armyAction = null;
				bool zeroMovement = true;
				if (!StaticString.IsNullOrEmpty(order.ArmyActionName))
				{
					IDatabase<ArmyAction> armyActionDatabase = Databases.GetDatabase<ArmyAction>(false);
					if (armyActionDatabase != null && armyActionDatabase.TryGetValue(order.ArmyActionName, out armyAction) && armyAction is IArmyActionWithMovementEffect)
					{
						zeroMovement = (armyAction as IArmyActionWithMovementEffect).ZeroMovement;
					}
				}
				if (zeroMovement)
				{
					foreach (Unit unit in army.Units)
					{
						unit.SetPropertyBaseValue(SimulationProperties.MovementRatio, 0f);
						unit.Refresh(false);
					}
				}
				if (order.NumberOfActionPointsToSpend > 0f)
				{
					ArmyAction.SpendSomeNumberOfActionPoints(army, order.NumberOfActionPointsToSpend);
				}
				if (order.ArmyActionCooldownDuration > 0f)
				{
					ArmyActionWithCooldown.ApplyCooldown(army, order.ArmyActionCooldownDuration);
				}
				if (armyAction != null)
				{
					army.OnArmyAction(armyAction, army);
				}
				army.Refresh(false);
			}
		}
		catch
		{
			yield break;
		}
		yield break;
	}

	private IEnumerator BuyoutAndPlaceTerraformationDeviceProcessor(OrderBuyoutAndPlaceTerraformationDevice order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		IGameEntityRepositoryService gameEntityRepositoryService = this.Game.Services.GetService<IGameEntityRepositoryService>();
		if (gameEntityRepositoryService == null)
		{
			Diagnostics.LogError("Cannot retreive the gameEntityRepositoryService.");
			yield break;
		}
		global::Empire empire = null;
		try
		{
			empire = this.Game.Empires[order.EmpireIndex];
		}
		catch
		{
			Diagnostics.LogError("Order processor failed because empire index is invalid.");
			yield break;
		}
		IGameEntity gameEntity;
		if (!gameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity))
		{
			yield break;
		}
		Army army = gameEntity as Army;
		if (army == null)
		{
			yield break;
		}
		IDatabase<ArmyAction> armyActionDatabase = Databases.GetDatabase<ArmyAction>(false);
		ArmyAction armyAction = armyActionDatabase.GetValue(order.ArmyActionName);
		if (armyAction == null)
		{
			yield break;
		}
		float actionPointsCost = armyAction.GetCostInActionPoints();
		if (actionPointsCost > 0f)
		{
			ArmyAction.SpendSomeNumberOfActionPoints(army, actionPointsCost);
		}
		for (int costIndex = 0; costIndex < armyAction.Costs.Length; costIndex++)
		{
			IConstructionCost constructionCost = armyAction.Costs[costIndex];
			string resourceCostName = constructionCost.ResourceName;
			if (!string.IsNullOrEmpty(resourceCostName) && !resourceCostName.Equals(DepartmentOfTheTreasury.Resources.ActionPoint))
			{
				float resourceCostValue = armyAction.GetCostForResource(resourceCostName, empire);
				if (resourceCostValue != 0f)
				{
					DepartmentOfTheTreasury departmentOfTheTreasury = empire.GetAgency<DepartmentOfTheTreasury>();
					bool transferResourceResult = departmentOfTheTreasury.TryTransferResources(empire, resourceCostName, -resourceCostValue);
					Diagnostics.Assert(transferResourceResult, "Transfer of resource (name: '{0}', amount: {1}) has failed.", new object[]
					{
						resourceCostName,
						-resourceCostValue
					});
				}
			}
		}
		IDatabase<TerraformDeviceDefinition> terraformDeviceDatabase = Databases.GetDatabase<TerraformDeviceDefinition>(false);
		TerraformDeviceDefinition terraformDeviceDefinition;
		if (terraformDeviceDatabase.TryGetValue(order.TerraformDeviceDefinitionName, out terraformDeviceDefinition))
		{
			ITerraformDeviceService terraformDeviceService = this.Game.Services.GetService<ITerraformDeviceService>();
			terraformDeviceService.AddDevice(order.TerraformDeviceGameEntityGUID, terraformDeviceDefinition, army.WorldPosition, empire, army.IsPrivateers);
		}
		yield break;
	}

	private IEnumerator BuyoutSpellAndPlayBattleActionProcessor(OrderBuyoutSpellAndPlayBattleAction order)
	{
		if (!order.EncounterGUID.IsValid)
		{
			Diagnostics.LogError("Encounter GUID is not valid.");
			yield break;
		}
		IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
		if (encounterRepositoryService == null)
		{
			Diagnostics.LogError("Can't found encounter repository service.");
			yield break;
		}
		Encounter encounter = null;
		if (!encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
		{
			Diagnostics.LogError("Can't found encounter.");
			yield break;
		}
		if (encounter.Contenders == null)
		{
			Diagnostics.LogError("Can't found encounter contenders.");
			yield break;
		}
		Contender foundContender = encounter.Contenders.Find((Contender contender) => contender.GUID == order.ContenderGUID);
		if (foundContender == null)
		{
			Diagnostics.LogError("Can't found encounter contender.");
			yield break;
		}
		IDatabase<SpellDefinition> spellDefinitions = Databases.GetDatabase<SpellDefinition>(false);
		if (spellDefinitions == null)
		{
			Diagnostics.LogError("Can't retrieve the spellDefinition's database.");
			yield break;
		}
		SpellDefinition spellDefinition;
		if (!spellDefinitions.TryGetValue(order.SpellDefinitionName, out spellDefinition))
		{
			Diagnostics.LogError("Can't retrieve the spellDefinition '{0}'.", new object[]
			{
				order.SpellDefinitionName
			});
			yield break;
		}
		if (order.ConstructionResourceStocks != null && order.ConstructionResourceStocks.Length > 0)
		{
			DepartmentOfTheTreasury departmentOfTheTreasury = foundContender.Garrison.Empire.GetAgency<DepartmentOfTheTreasury>();
			if (departmentOfTheTreasury == null)
			{
				Diagnostics.LogError("Can't retrieve the departmentOfTheTreasury.");
				yield break;
			}
			for (int index = 0; index < order.ConstructionResourceStocks.Length; index++)
			{
				if (order.ConstructionResourceStocks[index] != null && order.ConstructionResourceStocks[index].Stock != 0f)
				{
					if (spellDefinition.Costs != null && index < spellDefinition.Costs.Length)
					{
						departmentOfTheTreasury.TryTransferResources(foundContender.Garrison.Empire, spellDefinition.Costs[index].ResourceName, -order.ConstructionResourceStocks[index].Stock);
					}
				}
			}
		}
		Diagnostics.Assert(encounter != null);
		if (Databases.GetDatabase<BattleAction>(false) == null)
		{
			Diagnostics.LogError("Can't retrieve the battle action database.");
			yield break;
		}
		List<BattleActionUser> availableBattleActions = spellDefinition.GetAvailableBattleActionUser(foundContender.Garrison.Empire);
		for (int battleActionIndex = 0; battleActionIndex < availableBattleActions.Count; battleActionIndex++)
		{
			BattleAction battleActionUser = availableBattleActions[battleActionIndex];
			if (battleActionUser == null)
			{
				yield break;
			}
			foundContender.ReportBattleAction(battleActionUser.Name, BattleAction.State.Selected);
		}
		yield break;
	}

	private IEnumerator CancelKaijuResearchProcessor(OrderCancelKaijuResearch order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		IGameEntityRepositoryService gameEntityRepositoryService = gameService.Game.Services.GetService<IGameEntityRepositoryService>();
		Diagnostics.Assert(gameEntityRepositoryService != null);
		IKaijuTechsService kaijuTechsService = gameService.Game.Services.GetService<IKaijuTechsService>();
		Diagnostics.Assert(kaijuTechsService != null);
		global::Game game = gameService.Game as global::Game;
		global::Empire empire = game.Empires[order.EmpireIndex];
		if (empire == null)
		{
			Diagnostics.LogError("CancelKaijuResearchProcessor: Trying to cancel a research that does not have a valid empire");
			yield break;
		}
		IGameEntity gameEntity;
		if (!gameEntityRepositoryService.TryGetValue(order.ConstructionGameEntityGUID, out gameEntity))
		{
			Diagnostics.LogError("CancelKaijuResearchProcessor: Skipping cancel construction because the target game entity is not valid.");
			yield break;
		}
		Construction construction = gameEntity as Construction;
		if (construction == null)
		{
			Diagnostics.LogError("CancelKaijuResearchProcessor: Skipping cancel construction because the target game entity is not a Construction.");
			yield break;
		}
		ConstructionQueue selectedQueue = kaijuTechsService.GetConstructionQueueForEmpire(empire);
		Diagnostics.Assert(selectedQueue != null);
		selectedQueue.Remove(construction);
		gameEntityRepositoryService.Unregister(construction);
		kaijuTechsService.InvokeResearchQueueChanged(construction, ConstructionChangeEventAction.Cancelled);
		yield break;
	}

	private IEnumerator ChangeAdministrationSpecialityProcessor(OrderChangeAdministrationSpeciality order)
	{
		Diagnostics.Log(order);
		IGameEntityRepositoryService gameEntityRepositoryService = this.Game.Services.GetService<IGameEntityRepositoryService>();
		IGameEntity gameEntity = null;
		if (gameEntityRepositoryService.TryGetValue(order.CityGuid, out gameEntity) && gameEntity is City)
		{
			City city = gameEntity as City;
			if (order.EnableAdministrationByAI)
			{
				city.SimulationObject.Tags.AddTag(OrderChangeAdministrationSpeciality.AdministrationByAIActivated);
				city.ChangeAdministrationSpeciality(order.AdministrationSpeciality);
			}
			else
			{
				city.SimulationObject.Tags.RemoveTag(OrderChangeAdministrationSpeciality.AdministrationByAIActivated);
				city.ChangeAdministrationSpeciality(StaticString.Empty);
			}
		}
		yield break;
	}

	private IEnumerator ChangeContenderEncounterOptionProcessor(OrderChangeContenderEncounterOption order)
	{
		if (order.EncounterGUID.IsValid)
		{
			IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
			Diagnostics.Assert(encounterRepositoryService != null);
			if (encounterRepositoryService != null)
			{
				Encounter encounter = null;
				if (encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
				{
					encounter.SetContenderOptionChoice(order.ContenderGUID, order.ContenderEncounterOptionChoice);
				}
			}
		}
		yield break;
	}

	private IEnumerator ChangeContenderReinforcementRankingProcessor(OrderChangeContenderReinforcementRanking order)
	{
		if (!order.EncounterGUID.IsValid)
		{
			Diagnostics.LogError("Encounter GUID is not valid.");
			yield break;
		}
		IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
		if (encounterRepositoryService == null)
		{
			Diagnostics.LogError("Can't found encounter repository service.");
			yield break;
		}
		Encounter encounter = null;
		if (!encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
		{
			Diagnostics.LogError("Can't found encounter.");
			yield break;
		}
		Diagnostics.Assert(encounter != null);
		for (int index = 0; index < order.FirstContenderGUIDs.Length; index++)
		{
			encounter.ChangeContenderReinforcementRanking(order.FirstContenderGUIDs[index], order.NewIndexes[index]);
		}
		yield break;
	}

	private IEnumerator ChangeContenderStateProcessor(OrderChangeContenderState order)
	{
		if (order.EncounterGUID.IsValid)
		{
			IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
			Diagnostics.Assert(encounterRepositoryService != null);
			if (encounterRepositoryService != null)
			{
				Encounter encounter = null;
				if (encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
				{
					if (order.ContenderState == ContenderState.Deployment)
					{
						EncounterState encounterState = encounter.EncounterState;
						if (encounterState == EncounterState.Setup)
						{
							encounter.EncounterState = EncounterState.Deployment;
						}
					}
					encounter.SetContenderState(order.ContenderGUID, order.ContenderState);
				}
			}
		}
		yield break;
	}

	private IEnumerator ChangeDeploymentProcessor(OrderChangeDeployment order)
	{
		if (!order.EncounterGUID.IsValid)
		{
			yield break;
		}
		IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
		Diagnostics.Assert(encounterRepositoryService != null);
		if (encounterRepositoryService == null)
		{
			yield break;
		}
		Encounter encounter = null;
		if (!encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
		{
			yield break;
		}
		encounter.ChangeDeployment(order.ContenderGUID, order.Deployment);
		yield break;
	}

	private IEnumerator ChangeDiplomaticContractStateProcessor(OrderChangeDiplomaticContractState order)
	{
		Diagnostics.Assert(order != null);
		if (!order.ContractGUID.IsValid)
		{
			Diagnostics.LogError("Contract GUID is not valid.");
			yield break;
		}
		IDiplomaticContractRepositoryService diplomaticContractRepositoryService = this.Game.Services.GetService<IDiplomaticContractRepositoryService>();
		Diagnostics.Assert(diplomaticContractRepositoryService != null);
		DiplomaticContract diplomaticContract;
		if (!diplomaticContractRepositoryService.TryGetValue(order.ContractGUID, out diplomaticContract))
		{
			Diagnostics.LogError("Can't retrieve the contract {0}.", new object[]
			{
				order.ContractGUID
			});
			yield break;
		}
		Diagnostics.Assert(diplomaticContract != null && diplomaticContract.EmpireWhichProposes != null && diplomaticContract.EmpireWhichReceives != null);
		DepartmentOfTheTreasury departmentOfTheTreasury = diplomaticContract.EmpireWhichProposes.GetAgency<DepartmentOfTheTreasury>();
		Diagnostics.Assert(departmentOfTheTreasury != null);
		if (!departmentOfTheTreasury.TryTransferResources(diplomaticContract.EmpireWhichProposes.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, -order.EmpireWhichProposesEmpirePointCost))
		{
			Diagnostics.LogError("Transfer resources failed for empire {0}.", new object[]
			{
				diplomaticContract.EmpireWhichProposes
			});
		}
		float amount = order.EmpireWhichProposesPeacePoint * diplomaticContract.EmpireWhichProposes.GetPropertyValue(SimulationProperties.PeacePointGainMultiplier);
		if (!departmentOfTheTreasury.TryTransferResources(diplomaticContract.EmpireWhichProposes.SimulationObject, DepartmentOfTheTreasury.Resources.PeacePoint, amount))
		{
			Diagnostics.LogError("Transfer resources failed for empire {0}.", new object[]
			{
				diplomaticContract.EmpireWhichProposes
			});
		}
		departmentOfTheTreasury = diplomaticContract.EmpireWhichReceives.GetAgency<DepartmentOfTheTreasury>();
		Diagnostics.Assert(departmentOfTheTreasury != null);
		if (!departmentOfTheTreasury.TryTransferResources(diplomaticContract.EmpireWhichReceives.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, -order.EmpireWhichReceivesEmpirePointCost))
		{
			Diagnostics.LogError("Transfer resources failed for empire {0}.", new object[]
			{
				diplomaticContract.EmpireWhichReceives
			});
		}
		amount = order.EmpireWhichReceivesPeacePoint * diplomaticContract.EmpireWhichReceives.GetPropertyValue(SimulationProperties.PeacePointGainMultiplier);
		if (!departmentOfTheTreasury.TryTransferResources(diplomaticContract.EmpireWhichReceives.SimulationObject, DepartmentOfTheTreasury.Resources.PeacePoint, amount))
		{
			Diagnostics.LogError("Transfer resources failed for empire {0}.", new object[]
			{
				diplomaticContract.EmpireWhichReceives
			});
		}
		if (order.EmpireWhichProposesPeacePoint != 0f)
		{
			diplomaticContract.EmpireWhichProposes.SetPropertyBaseValue(SimulationProperties.EmpirePointSpentInSignificantDiplomacy, order.EmpireWhichProposesEmpirePointCost + diplomaticContract.EmpireWhichProposes.GetPropertyBaseValue(SimulationProperties.EmpirePointSpentInSignificantDiplomacy));
		}
		if (order.EmpireWhichReceivesPeacePoint != 0f)
		{
			diplomaticContract.EmpireWhichReceives.SetPropertyBaseValue(SimulationProperties.EmpirePointSpentInSignificantDiplomacy, order.EmpireWhichReceivesEmpirePointCost + diplomaticContract.EmpireWhichReceives.GetPropertyBaseValue(SimulationProperties.EmpirePointSpentInSignificantDiplomacy));
		}
		IDiplomacyControl diplomacyControl = this.Game.Services.GetService<IDiplomacyService>() as IDiplomacyControl;
		Diagnostics.Assert(diplomacyControl != null);
		diplomacyControl.SetDiplomaticContractState(diplomaticContract, order.DiplomaticContractNewState);
		if (diplomaticContract.State == DiplomaticContractState.Signed)
		{
			this.NotifyIfMarketBan(diplomaticContract);
		}
		DepartmentOfForeignAffairs departmentOfForeignAffairs = diplomaticContract.EmpireWhichProposes.GetAgency<DepartmentOfForeignAffairs>();
		DiplomaticRelation diplomaticRelation;
		if (departmentOfForeignAffairs != null)
		{
			diplomaticRelation = departmentOfForeignAffairs.GetDiplomaticRelation(diplomaticContract.EmpireWhichReceives);
			if (diplomaticRelation != null)
			{
				diplomaticRelation.RefreshDiplomaticAbilities();
			}
		}
		departmentOfForeignAffairs = diplomaticContract.EmpireWhichReceives.GetAgency<DepartmentOfForeignAffairs>();
		if (departmentOfForeignAffairs == null)
		{
			yield break;
		}
		diplomaticRelation = departmentOfForeignAffairs.GetDiplomaticRelation(diplomaticContract.EmpireWhichProposes);
		if (diplomaticRelation != null)
		{
			diplomaticRelation.RefreshDiplomaticAbilities();
			yield break;
		}
		yield break;
	}

	private void NotifyIfMarketBan(DiplomaticContract diplomaticContract)
	{
		ITradeManagementService service = this.Game.Services.GetService<ITradeManagementService>();
		for (int i = 0; i < diplomaticContract.Terms.Count; i++)
		{
			DiplomaticTerm diplomaticTerm = diplomaticContract.Terms[i];
			if (diplomaticTerm.Definition.Name == "MarketBan")
			{
				service.NotifyTradableTransactionComplete(TradableTransactionType.Ban, diplomaticTerm.EmpireWhichReceives, null, 0f, 0f);
			}
			else if (diplomaticTerm.Definition.Name == "MarketBanRemoval")
			{
				service.NotifyTradableTransactionComplete(TradableTransactionType.Unban, diplomaticTerm.EmpireWhichReceives, null, 0f, 0f);
			}
			else if (diplomaticTerm.Definition.Name == "MarketBanNullification")
			{
				service.NotifyTradableTransactionComplete(TradableTransactionType.Unban, diplomaticTerm.EmpireWhichProposes, null, 0f, 0f);
			}
		}
	}

	private IEnumerator ChangeDiplomaticContractTermsCollectionProcessor(OrderChangeDiplomaticContractTermsCollection order)
	{
		Diagnostics.Assert(order != null);
		if (!order.ContractGUID.IsValid)
		{
			Diagnostics.LogError("Contract GUID is not valid.");
			yield break;
		}
		if (order.DiplomaticTermChanges == null || order.DiplomaticTermChanges.Length == 0)
		{
			Diagnostics.LogError("OrderChangeDiplomaticContractTermsCollection constains no diplomatic term changes.");
			yield break;
		}
		IDiplomaticContractRepositoryService diplomaticContractRepositoryService = this.Game.Services.GetService<IDiplomaticContractRepositoryService>();
		Diagnostics.Assert(diplomaticContractRepositoryService != null);
		DiplomaticContract diplomaticContract;
		if (!diplomaticContractRepositoryService.TryGetValue(order.ContractGUID, out diplomaticContract))
		{
			Diagnostics.LogError("Can't retrieve the contract {0}.", new object[]
			{
				order.ContractGUID
			});
			yield break;
		}
		IDiplomaticContractManagement diplomaticContractManagement = diplomaticContract;
		Diagnostics.Assert(diplomaticContractManagement != null);
		for (int index = 0; index < order.DiplomaticTermChanges.Length; index++)
		{
			DiplomaticTermChange diplomaticTermChange = order.DiplomaticTermChanges[index];
			diplomaticContractManagement.ApplyDiplomaticTermChange(diplomaticTermChange);
		}
		yield break;
	}

	private IEnumerator ChangeDiplomaticRelationStateProcessor(OrderChangeDiplomaticRelationState order)
	{
		global::Empire initiatorEmpire;
		global::Empire targetEmpire;
		try
		{
			initiatorEmpire = this.Game.Empires[order.InitiatorEmpireIndex];
			targetEmpire = this.Game.Empires[order.TargetEmpireIndex];
		}
		catch
		{
			Diagnostics.LogError("OrderChangeDiplomaticRelationState, Empire index are invalid.");
			yield break;
		}
		IForeignAffairsManagment initiatorEmpireDiplomaticRelationManagment = initiatorEmpire.GetAgency<DepartmentOfForeignAffairs>();
		if (initiatorEmpireDiplomaticRelationManagment != null)
		{
			initiatorEmpireDiplomaticRelationManagment.SetDiplomaticRelationState(targetEmpire, order.NewDiplomaticRelationStateName, true);
		}
		IForeignAffairsManagment targetEmpireDiplomaticRelationManagment = targetEmpire.GetAgency<DepartmentOfForeignAffairs>();
		if (targetEmpireDiplomaticRelationManagment != null)
		{
			targetEmpireDiplomaticRelationManagment.SetDiplomaticRelationState(initiatorEmpire, order.NewDiplomaticRelationStateName, true);
		}
		DepartmentOfForeignAffairs departmentOfForeignAffairs = initiatorEmpire.GetAgency<DepartmentOfForeignAffairs>();
		if (departmentOfForeignAffairs != null)
		{
			DiplomaticRelation diplomaticRelation = departmentOfForeignAffairs.GetDiplomaticRelation(targetEmpire);
			if (diplomaticRelation != null)
			{
				diplomaticRelation.RefreshDiplomaticAbilities();
			}
		}
		departmentOfForeignAffairs = targetEmpire.GetAgency<DepartmentOfForeignAffairs>();
		if (departmentOfForeignAffairs != null)
		{
			DiplomaticRelation diplomaticRelation = departmentOfForeignAffairs.GetDiplomaticRelation(initiatorEmpire);
			if (diplomaticRelation != null)
			{
				diplomaticRelation.RefreshDiplomaticAbilities();
			}
		}
		yield break;
	}

	private IEnumerator ChangeReinforcementPriorityProcessor(OrderChangeReinforcementPriority order)
	{
		if (!order.EncounterGUID.IsValid)
		{
			Diagnostics.LogError("Encounter GUID is not valid.");
			yield break;
		}
		IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
		if (encounterRepositoryService == null)
		{
			Diagnostics.LogError("Can't found encounter repository service.");
			yield break;
		}
		Encounter encounter = null;
		if (!encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
		{
			Diagnostics.LogError("Can't found encounter.");
			yield break;
		}
		Diagnostics.Assert(encounter != null);
		if (encounter.Contenders == null)
		{
			Diagnostics.LogError("Can't found encounter contender.");
			yield break;
		}
		Contender foundContender = encounter.Contenders.Find((Contender contender) => contender.GUID == order.ContenderGUID);
		if (foundContender == null)
		{
			Diagnostics.LogError("Can't found encounter contender.");
			yield break;
		}
		foundContender.ReportReinforcementPriorityChange(order.PriorityBodyName, BattleReinforcementPriorityChangeEventArgs.State.Selected);
		yield break;
	}

	private IEnumerator ChangeSeasonProcessor(OrderChangeSeason order)
	{
		ISeasonService seasonService = this.Game.GetService<ISeasonService>();
		SeasonManager seasonManager = seasonService as SeasonManager;
		if (seasonManager != null)
		{
			seasonManager.OverrideSeasonSettings(order.Seasons, order.NewSeasonName, order.OldSeasonName, order.FollowingSeasonName, order.PickedSeasonEffectName, order.PreselectedSeasonEffectNames, order.PreselectedSeasonEffectBaseVotes, order.CurrentSeasonIntensityName);
		}
		yield break;
	}

	private IEnumerator ChangeStrategyProcessor(OrderChangeStrategy order)
	{
		if (!order.EncounterGUID.IsValid)
		{
			Diagnostics.LogError("Encounter GUID is not valid.");
			yield break;
		}
		IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
		if (encounterRepositoryService == null)
		{
			Diagnostics.LogError("Can't found encounter repository service.");
			yield break;
		}
		Encounter encounter = null;
		if (!encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
		{
			Diagnostics.LogError("Can't found encounter.");
			yield break;
		}
		Diagnostics.Assert(encounter != null);
		if (encounter.Contenders == null)
		{
			Diagnostics.LogError("Can't found encounter contender.");
			yield break;
		}
		Contender foundContender = encounter.Contenders.Find((Contender contender) => contender.GUID == order.ContenderGUID);
		if (foundContender == null)
		{
			Diagnostics.LogError("Can't found encounter contender.");
			yield break;
		}
		foundContender.ReportStrategyChange(order.Strategy);
		for (int unitIndex = 0; unitIndex < foundContender.EncounterUnits.Count; unitIndex++)
		{
			foundContender.EncounterUnits[unitIndex].ReportStrategyChange(order.Strategy);
		}
		yield break;
	}

	private IEnumerator ChangeUnitDeploymentProcessor(OrderChangeUnitDeployment order)
	{
		if (!order.EncounterGUID.IsValid)
		{
			yield break;
		}
		IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
		Diagnostics.Assert(encounterRepositoryService != null);
		if (encounterRepositoryService == null)
		{
			yield break;
		}
		Encounter encounter = null;
		if (!encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
		{
			yield break;
		}
		encounter.ChangeUnitDeployment(order.ContenderGUID, order.UnitGUID, order.WorldPosition);
		yield break;
	}

	private IEnumerator ChangeUnitStrategyProcessor(OrderChangeUnitStrategy order)
	{
		if (!order.EncounterGUID.IsValid)
		{
			Diagnostics.LogError("Encounter GUID is not valid.");
			yield break;
		}
		IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
		if (encounterRepositoryService == null)
		{
			Diagnostics.LogError("Can't found encounter repository service.");
			yield break;
		}
		Encounter encounter = null;
		if (!encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
		{
			Diagnostics.LogError("Can't found encounter.");
			yield break;
		}
		Diagnostics.Assert(encounter != null);
		if (encounter.Contenders == null)
		{
			Diagnostics.LogError("Can't found encounter contender.");
			yield break;
		}
		Contender foundContender = encounter.Contenders.Find((Contender contender) => contender.GUID == order.ContenderGUID);
		if (foundContender == null)
		{
			Diagnostics.LogError("Can't found encounter contender.");
			yield break;
		}
		EncounterUnit foundUnit = foundContender.EncounterUnits.Find((EncounterUnit unit) => unit.Unit.GUID == order.UnitGUID);
		if (foundUnit == null)
		{
			Diagnostics.LogError("Can't found encounter unit.");
			yield break;
		}
		foundUnit.ReportStrategyChange(order.Strategy);
		yield break;
	}

	private IEnumerator ChangeUnitTargetingProcessor(OrderChangeUnitTargeting order)
	{
		if (!order.EncounterGUID.IsValid)
		{
			yield break;
		}
		IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
		Diagnostics.Assert(encounterRepositoryService != null);
		if (encounterRepositoryService == null)
		{
			yield break;
		}
		Encounter encounter = null;
		if (!encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
		{
			yield break;
		}
		Contender contender = encounter.Contenders.Find((Contender match) => match.GUID == order.ContenderGUID);
		if (contender == null)
		{
			yield break;
		}
		UnitTargetingIntention intention = order.TargetingIntention;
		contender.SetTargetingIntentionForUnit(order.UnitGUID, intention, order.AvailableOpportunityTargets);
		yield break;
	}

	private IEnumerator OrderChangeUnitsStrategiesProcessor(OrderChangeUnitsTargetingAndStrategy order)
	{
		if (!order.EncounterGUID.IsValid)
		{
			Diagnostics.LogError("Encounter GUID is not valid.");
			yield break;
		}
		IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
		if (encounterRepositoryService == null)
		{
			Diagnostics.LogError("Can't found encounter repository service.");
			yield break;
		}
		Encounter encounter = null;
		if (!encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
		{
			Diagnostics.LogError("Can't found encounter.");
			yield break;
		}
		Diagnostics.Assert(encounter != null);
		if (encounter.Contenders == null)
		{
			Diagnostics.LogError("Can't found encounter contender.");
			yield break;
		}
		Contender foundContender = encounter.Contenders.Find((Contender contender) => contender.GUID == order.ContenderGUID);
		if (foundContender == null)
		{
			Diagnostics.LogError("Can't found encounter contender.");
			yield break;
		}
		for (int indexStrategy = 0; indexStrategy < order.UnitStrategies.Length; indexStrategy++)
		{
			OrderChangeUnitsTargetingAndStrategy.UnitTargetingData unitTargetingData = order.UnitStrategies[indexStrategy];
			if (unitTargetingData != null)
			{
				EncounterUnit foundUnit = foundContender.EncounterUnits.Find((EncounterUnit unit) => unit.Unit.GUID == unitTargetingData.UnitGUID);
				if (foundUnit == null)
				{
					Diagnostics.LogError("Can't found encounter unit.");
					yield break;
				}
				foundUnit.ReportStrategyChange(unitTargetingData.Strategy);
			}
		}
		yield break;
	}

	private IEnumerator ClaimDiplomacyPointsProcessor(OrderClaimDiplomacyPoints order)
	{
		IGameEntityRepositoryService gameEntityRepositoryService = this.Game.Services.GetService<IGameEntityRepositoryService>();
		if (gameEntityRepositoryService == null)
		{
			Diagnostics.LogError("Unable to retrieve the game entity repository service.");
			yield break;
		}
		IEventService eventService = Services.GetService<IEventService>();
		IPlayerControllerRepositoryService playerControllerRepositoryService = this.Game.GetService<IPlayerControllerRepositoryService>();
		bool notify = playerControllerRepositoryService.ActivePlayerController.Empire.Index == order.EmpireIndex;
		global::Empire empire = this.Game.Empires[order.EmpireIndex];
		if (order.QuestRewards != null && order.QuestRewards.Length > 0)
		{
			if (order.QuestRewards[0].Droppables == null || order.QuestRewards[0].Droppables.Length == 0)
			{
			}
			foreach (IDroppable droppable in order.QuestRewards[0].Droppables)
			{
				IDroppableWithRewardAllocation droppableWithRewardAllocation = droppable as IDroppableWithRewardAllocation;
				if (droppableWithRewardAllocation != null)
				{
					try
					{
						droppableWithRewardAllocation.AllocateRewardTo(empire, new object[0]);
					}
					catch
					{
					}
				}
			}
			if (notify && eventService != null)
			{
				eventService.Notify(new EventDiplomacyClaimSucceeded(order.InstigatorGUID, order.QuestRewards[0].Droppables));
			}
			IGameEntity gameEntity;
			if (order.NumberOfActionPointsToSpend > 0f && gameEntityRepositoryService.TryGetValue(order.InstigatorGUID, out gameEntity))
			{
				ArmyAction.SpendSomeNumberOfActionPoints(gameEntity, order.NumberOfActionPointsToSpend);
			}
			if (order.EmpirePointsCost > 0f)
			{
				DepartmentOfTheTreasury departmentOfTheTreasury = empire.GetAgency<DepartmentOfTheTreasury>();
				departmentOfTheTreasury.TryTransferResources(empire, DepartmentOfTheTreasury.Resources.EmpirePoint, order.EmpirePointsCost * -1f);
			}
			IGameEntity gameEntity2;
			if (order.TargetGUID.IsValid && gameEntityRepositoryService.TryGetValue(order.TargetGUID, out gameEntity2) && gameEntity2 is PointOfInterest)
			{
				PointOfInterest pointOfInterest = gameEntity2 as PointOfInterest;
				pointOfInterest.Interaction.AddInteractionLock(order.EmpireIndex, order.InteractionName);
			}
			if (!StaticString.IsNullOrEmpty(order.ArmyActionName))
			{
				ArmyAction armyAction = null;
				IDatabase<ArmyAction> armyActionDatabase = Databases.GetDatabase<ArmyAction>(false);
				IGameEntity gameEntity3;
				if (armyActionDatabase.TryGetValue(order.ArmyActionName, out armyAction) && gameEntityRepositoryService.TryGetValue(order.InstigatorGUID, out gameEntity3) && gameEntity3 is Army)
				{
					Army army = gameEntity3 as Army;
					if (army.Hero != null && armyAction.ExperienceReward > 0f)
					{
						army.Hero.GainXp(armyAction.ExperienceReward, false, true);
					}
				}
			}
			yield break;
		}
		yield break;
	}

	private IEnumerator CompleteQuestProcessor(OrderCompleteQuest order)
	{
		if (order.QuestGUID.IsValid)
		{
			IQuestRepositoryService questRepositoryService = this.Game.Services.GetService<IQuestRepositoryService>();
			Diagnostics.Assert(questRepositoryService != null);
			Quest quest;
			if (questRepositoryService != null && questRepositoryService.TryGetValue(order.QuestGUID, out quest))
			{
				switch (quest.QuestState)
				{
				case QuestState.Completed:
				case QuestState.Failed:
					goto IL_C7;
				}
				quest.QuestState = order.QuestState;
				IL_C7:
				questRepositoryService.Unregister(quest);
				quest.TurnWhenCompleted = this.Game.Turn;
				for (int index = 0; index < 32; index++)
				{
					if ((quest.EmpireBits & 1 << index) != 0)
					{
						int empireIndex = index;
						try
						{
							global::Empire empire = this.Game.Empires[empireIndex];
							DepartmentOfInternalAffairs departmentOfInternalAffairs = empire.GetAgency<DepartmentOfInternalAffairs>();
							if (departmentOfInternalAffairs != null)
							{
								departmentOfInternalAffairs.QuestJournal.Complete(quest);
							}
							IEventService eventService = Services.GetService<IEventService>();
							if (eventService != null)
							{
								if (quest.QuestState == QuestState.Completed)
								{
									eventService.Notify(new EventQuestComplete(empire, quest));
									if (quest.Category == QuestDefinition.CategoryMedal)
									{
										eventService.Notify(new EventMedalCompleted(empire, quest));
									}
								}
								else
								{
									int failedStepIndex = Array.IndexOf<QuestState>(quest.StepStates, quest.StepStates.FirstOrDefault((QuestState match) => match == QuestState.Failed || match == QuestState.InProgress));
									if (failedStepIndex != -1)
									{
										quest.StepStates[failedStepIndex] = QuestState.Failed;
									}
									eventService.Notify(new EventQuestFailed(empire, quest, failedStepIndex));
									if (quest.Category == QuestDefinition.CategoryMedal)
									{
										eventService.Notify(new EventMedalFailed(empire, quest, failedStepIndex));
									}
								}
							}
							if (quest.QuestState == QuestState.Completed && (quest.IsMainQuest() || quest.QuestDefinition.IsVictoryQuest()) && !quest.QuestDefinition.Tags.Contains(QuestDefinition.TagHidden))
							{
								IDatabase<SimulationDescriptor> simulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
								Diagnostics.Assert(simulationDescriptorDatabase != null);
								SimulationDescriptor mainQuestCompletedDescriptor = null;
								Diagnostics.Assert(simulationDescriptorDatabase.TryGetValue("MainQuestCompletedCountIncrement", out mainQuestCompletedDescriptor));
								DepartmentOfScience departmentOfScience = (empire as MajorEmpire).GetAgency<DepartmentOfScience>();
								if (departmentOfScience != null)
								{
									departmentOfScience.ResearchSimulationObjectWrapper.AddDescriptor(mainQuestCompletedDescriptor, false);
								}
							}
						}
						catch (Exception ex)
						{
							Exception e = ex;
							Diagnostics.LogError("Unable to find the recipient (empire: #{0}). \n\n Error: {1}", new object[]
							{
								empireIndex,
								e.Message
							});
						}
					}
				}
			}
		}
		else
		{
			Diagnostics.LogError("The quest entity guid should be valid.");
		}
		yield break;
	}

	private IEnumerator CreateDiplomaticContractProcessor(OrderCreateDiplomaticContract order)
	{
		if (!order.ContractGUID.IsValid)
		{
			Diagnostics.LogError("Contract GUID is not valid.");
			yield break;
		}
		if (order.EmpireWhichInitiatedIndex < 0 || order.EmpireWhichInitiatedIndex >= this.Game.Empires.Length || order.EmpireWhichReceivesIndex < 0 || order.EmpireWhichReceivesIndex >= this.Game.Empires.Length)
		{
			Diagnostics.LogError("Invalid empires index.");
			yield break;
		}
		global::Empire empireWhichInitiated = this.Game.Empires[order.EmpireWhichInitiatedIndex];
		global::Empire empireWhichReceives = this.Game.Empires[order.EmpireWhichReceivesIndex];
		DiplomaticContract diplomaticContract = new DiplomaticContract(order.ContractGUID, empireWhichInitiated, empireWhichReceives);
		IGameEntityRepositoryService gameEntityRepositoryService = this.Game.Services.GetService<IGameEntityRepositoryService>();
		gameEntityRepositoryService.Register(diplomaticContract);
		IDiplomaticContractRepositoryService diplomaticContractRepositoryService = this.Game.Services.GetService<IDiplomaticContractRepositoryService>();
		Diagnostics.Assert(diplomaticContractRepositoryService != null && diplomaticContractRepositoryService is IDiplomaticContractRepositoryControl);
		(diplomaticContractRepositoryService as IDiplomaticContractRepositoryControl).Register(diplomaticContract);
		yield break;
	}

	private IEnumerator CreateEncounterProcessor(OrderCreateEncounter order)
	{
		if (order.EncounterGUID.IsValid)
		{
			Encounter encounter = new Encounter(order.EncounterGUID);
			encounter.OrderCreateEncounter = order;
			IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
			Diagnostics.Assert(encounterRepositoryService != null);
			if (encounterRepositoryService != null)
			{
				encounterRepositoryService.Register(encounter);
			}
			encounter.EncounterState = EncounterState.Initialize;
		}
		else
		{
			Diagnostics.LogError("The encounter game entity guid should be valid.");
		}
		yield break;
	}

	private IEnumerator CreateCityAssaultEncounterProcessor(OrderCreateCityAssaultEncounter order)
	{
		yield return this.CreateEncounterProcessor(order);
		yield break;
	}

	private IEnumerator DebugInfoProcessor(OrderDebugInfo order)
	{
		yield break;
	}

	private IEnumerator DestroyEncounterProcessor(OrderDestroyEncounter order)
	{
		if (order.EncounterGUID.IsValid)
		{
			IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
			Diagnostics.Assert(encounterRepositoryService != null);
			if (encounterRepositoryService != null)
			{
				Encounter encounter = null;
				if (encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
				{
					encounterRepositoryService.Unregister(encounter);
					IGameEntityRepositoryService gameEntityRepositoryService = this.Game.GetService<IGameEntityRepositoryService>();
					Diagnostics.Assert(gameEntityRepositoryService != null);
					if (gameEntityRepositoryService != null)
					{
						gameEntityRepositoryService.Unregister(encounter);
					}
					encounter.Dispose();
				}
			}
		}
		yield break;
	}

	private IEnumerator EliminateEmpireProcessor(OrderEliminateEmpire order)
	{
		if (order.EmpireIndex >= 0 && order.EmpireIndex < this.Game.Empires.Length)
		{
			global::Empire empire = this.Game.Empires[order.EmpireIndex];
			Diagnostics.Assert(empire.SimulationObject != null);
			if (!empire.SimulationObject.Tags.Contains(global::Empire.TagEmpireEliminated))
			{
				SimulationDescriptor descriptor = null;
				IDatabase<SimulationDescriptor> simulationDescriptors = Databases.GetDatabase<SimulationDescriptor>(false);
				if (simulationDescriptors != null)
				{
					simulationDescriptors.TryGetValue(global::Empire.TagEmpireEliminated, out descriptor);
				}
				if (descriptor != null)
				{
					empire.AddDescriptor(descriptor, false);
					empire.Refresh(true);
				}
				else
				{
					empire.SimulationObject.Tags.AddTag(global::Empire.TagEmpireEliminated);
				}
			}
			empire.SetPropertyBaseValue(global::Empire.TagEmpireEliminated, (float)this.Game.Turn);
			foreach (global::Empire other in this.Game.Empires)
			{
				other.OnEmpireEliminated(empire, false);
			}
			IEventService eventService = Services.GetService<IEventService>();
			if (eventService != null)
			{
				IPlayerControllerRepositoryService playerControllerRepositoryService = this.Game.Services.GetService<IPlayerControllerRepositoryService>();
				if (playerControllerRepositoryService != null && playerControllerRepositoryService.ActivePlayerController != null && playerControllerRepositoryService.ActivePlayerController.Empire != null && playerControllerRepositoryService.ActivePlayerController.Empire.Index == order.EmpireIndex)
				{
					IGameSerializationService gameSerializationService = Services.GetService<IGameSerializationService>();
					if (gameSerializationService != null)
					{
						string directory = global::Application.GameSaveDirectory;
						string extension = ".sav";
						string fileNameWithoutExtension = "%EliminationSaveFileName";
						ILocalizationService localizationService = Services.GetService<ILocalizationService>();
						if (localizationService != null && fileNameWithoutExtension.StartsWith("%"))
						{
							fileNameWithoutExtension = localizationService.Localize(fileNameWithoutExtension, "Game Finished by Elimination - Turn $Turn");
						}
						fileNameWithoutExtension = fileNameWithoutExtension.Replace("$Turn", this.Game.Turn.ToString());
						string outputFileName = System.IO.Path.Combine(directory, fileNameWithoutExtension) + extension;
						Amplitude.Coroutine coroutine = Amplitude.Coroutine.StartCoroutine(gameSerializationService.SaveGameAsync(fileNameWithoutExtension, outputFileName, GameSaveOptions.Closed), null);
						coroutine.RunUntilIsFinished();
						if (coroutine.LastException != null)
						{
							Diagnostics.LogWarning("Exception caught while saving the game after elimination: " + coroutine.LastException.ToString());
						}
					}
					EventGameEnded eventGameEnded = new EventGameEnded();
					eventService.Notify(eventGameEnded);
					yield break;
				}
				EventEmpireEliminated eventEmpireEliminated = new EventEmpireEliminated(empire);
				eventService.Notify(eventEmpireEliminated);
			}
		}
		yield break;
	}

	private IEnumerator EncounterDeploymentStartProcessor(OrderEncounterDeploymentStart order)
	{
		if (order.EncounterGUID.IsValid)
		{
			IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
			Diagnostics.Assert(encounterRepositoryService != null);
			if (encounterRepositoryService != null)
			{
				Encounter encounter = null;
				if (encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
				{
					encounter.EncounterState = EncounterState.Deployment;
					for (int contenderIndex = 0; contenderIndex < encounter.Contenders.Count; contenderIndex++)
					{
						encounter.SetContenderState(encounter.Contenders[contenderIndex].GUID, ContenderState.Deployment);
					}
				}
			}
		}
		yield break;
	}

	private IEnumerator EncounterDeploymentUpdateProcessor(OrderEncounterDeploymentUpdate order)
	{
		if (order.EncounterGUID.IsValid)
		{
			IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
			Diagnostics.Assert(encounterRepositoryService != null);
			if (encounterRepositoryService != null)
			{
				Encounter encounter = null;
				if (encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
				{
					encounter.ForwardDeploymentUpdate(order.Report);
				}
			}
		}
		yield break;
	}

	private IEnumerator EncounterRoundEndProcessor(OrderEncounterRoundEnd order)
	{
		if (order.EncounterGUID.IsValid)
		{
			IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
			Diagnostics.Assert(encounterRepositoryService != null);
			if (encounterRepositoryService != null)
			{
				Encounter encounter = null;
				if (encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
				{
					encounter.ForwardRoundEnd();
				}
			}
		}
		yield break;
	}

	private IEnumerator EncounterRoundUpdateProcessor(OrderEncounterRoundUpdate order)
	{
		if (order.EncounterGUID.IsValid)
		{
			IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
			Diagnostics.Assert(encounterRepositoryService != null);
			if (encounterRepositoryService != null)
			{
				Encounter encounter = null;
				if (encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
				{
					encounter.SetBattleInProgress(true);
					encounter.ForwardRoundUpdate(order.Report, order.ImmediateReportParsing);
				}
			}
		}
		yield break;
	}

	private IEnumerator EncounterTargetingPhaseUpdateProcessor(OrderEncounterTargetingPhaseUpdate order)
	{
		if (order.EncounterGUID.IsValid)
		{
			IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
			Diagnostics.Assert(encounterRepositoryService != null);
			if (encounterRepositoryService != null)
			{
				Encounter encounter = null;
				if (encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
				{
					encounter.SetBattleInProgress(false);
					encounter.SetTargetingPhaseInProgress();
					encounter.TargetingPhaseTime = new Encounter.PhaseTime(order.EndPhaseDateTime, order.PhaseDuration);
					encounter.ForwardTargetingPhaseUpdate(order.Report, order.EndPhaseDateTime);
				}
			}
		}
		yield break;
	}

	private IEnumerator EndEncounterProcessor(OrderEndEncounter order)
	{
		if (!order.EncounterGUID.IsValid)
		{
			yield break;
		}
		IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
		Diagnostics.Assert(encounterRepositoryService != null);
		Encounter encounter = null;
		if (!encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
		{
			Diagnostics.LogError("Can't retrieve encounter {0}.", new object[]
			{
				order.EncounterGUID
			});
			yield break;
		}
		IGameService gameService = Services.GetService<IGameService>();
		global::Game game = gameService.Game as global::Game;
		global::Empire[] majorEmpires = Array.FindAll<global::Empire>(game.Empires, (global::Empire match) => match is MajorEmpire);
		float siegePointsDone = 0f;
		this.ApplyFortificationLoss(encounter, out siegePointsDone);
		Dictionary<byte, GameClient.EndEncounterContenderInformations> contenderInformationsByGroup = new Dictionary<byte, GameClient.EndEncounterContenderInformations>();
		for (int index = 0; index < encounter.Contenders.Count; index++)
		{
			Contender contender4 = encounter.Contenders[index];
			this.SetContenderInformation(contender4, contenderInformationsByGroup);
			for (int jndex = 0; jndex < contender4.EncounterUnits.Count; jndex++)
			{
				contender4.EncounterUnits[jndex].Unit.PathfindingContextMode = PathfindingContextMode.Default;
			}
			if (!order.HasBeenCanceled)
			{
				if (!order.DoNotSubtractActionPoints)
				{
					contender4.UpdateActionPoints();
				}
				if (contender4.IsTakingPartInBattle && contender4.Garrison.Hero != null)
				{
					DepartmentOfIntelligence departmentOfIntelligence = contender4.Empire.GetAgency<DepartmentOfIntelligence>();
					if (departmentOfIntelligence != null)
					{
						departmentOfIntelligence.StopInfiltration(contender4.Garrison.Hero, true, true);
					}
				}
			}
			if (contender4.DeadParasitesCount > 0)
			{
				Contender enemyContender = encounter.GetEnemyContenderWithAbilityFromContender(contender4, UnitAbility.ReadonlyParasite);
				enemyContender.UndeadUnitsToCreateCount += contender4.DeadParasitesCount;
			}
		}
		int deadUnitsCount = this.ApplyKills(encounter, contenderInformationsByGroup);
		List<int> empiresWhichAlreadyReceivedCadavers = new List<int>();
		List<int> empiresWhichAlreadyReceivedSiegePoints = new List<int>();
		int remainingCadaverRecyclersCount = (from contender in encounter.Contenders
		where contender.ContenderState != ContenderState.Defeated && DepartmentOfTheInterior.CanRecycleCadavers(contender.Empire)
		select contender.Empire).Distinct<global::Empire>().Count<global::Empire>();
		int empireAlreadyRewardedInEmpirePoint = 0;
		for (int index2 = 0; index2 < encounter.Contenders.Count; index2++)
		{
			Contender contender2 = encounter.Contenders[index2];
			Diagnostics.Assert(contender2 != null);
			Diagnostics.Assert(contender2.Garrison != null);
			Diagnostics.Assert(contenderInformationsByGroup.ContainsKey(contender2.Group));
			Diagnostics.Assert(contenderInformationsByGroup[contender2.Group] != null);
			if (contender2 != null && contender2.Garrison != null && contender2.Garrison.GUID.IsValid && !contender2.Garrison.IsEmpty)
			{
				if (contender2.IsTakingPartInBattle)
				{
					float overallExperienceRewards = 0f;
					int overallEnemyPrestigeKills = 0;
					float overallEnemyKillsBountyReward = 0f;
					foreach (KeyValuePair<byte, GameClient.EndEncounterContenderInformations> otherContenderInformations in contenderInformationsByGroup)
					{
						if (otherContenderInformations.Key != contender2.Group)
						{
							overallExperienceRewards += otherContenderInformations.Value.ExperienceRewardOnKillPoint;
							if ((contender2.Empire.Bits & empireAlreadyRewardedInEmpirePoint) == 0)
							{
								Contender otherContender = encounter.Contenders.Find((Contender match) => match.Group == otherContenderInformations.Key);
								Diagnostics.Assert(otherContender != null);
								if (contender2.Empire is MajorEmpire && otherContender.Empire is MajorEmpire && !otherContender.IsPrivateers)
								{
									overallEnemyKillsBountyReward += this.CountBountyKillsReward(otherContender.Empire, otherContenderInformations.Value);
									foreach (global::Empire thirdEmpire in majorEmpires)
									{
										overallEnemyPrestigeKills += this.CountPrestigeKillsForThirdEmpire(contender2.Empire, otherContender.Empire, otherContenderInformations.Value, thirdEmpire);
									}
									empireAlreadyRewardedInEmpirePoint |= contender2.Empire.Bits;
								}
							}
						}
					}
					if (contenderInformationsByGroup.Count == 2)
					{
						foreach (KeyValuePair<byte, GameClient.EndEncounterContenderInformations> otherContenderInformations2 in contenderInformationsByGroup)
						{
							if (otherContenderInformations2.Value.UnitsKilledInAction.Count != 0)
							{
								IEventService eventService = Services.GetService<IEventService>();
								Diagnostics.Assert(eventService != null);
								if (eventService != null)
								{
									global::Empire addressee = (otherContenderInformations2.Key != encounter.Contenders[0].Group) ? encounter.Contenders[0].Empire : encounter.Contenders[1].Empire;
									eventService.Notify(new EventUnitsKilledInAction(addressee, encounter, otherContenderInformations2.Value.UnitsKilledInAction));
								}
							}
						}
					}
					this.ApplyBountyDustGain(contender2.Empire, overallEnemyKillsBountyReward);
					float empirePointsGained = this.ApplyEmpirePointsGain(contender2.Empire, overallEnemyPrestigeKills);
					float dustGained = this.ApplyDustGain(contender2.Empire, overallExperienceRewards);
					int cadaverCountBefore = Mathf.RoundToInt(contender2.Empire.GetPropertyValue(SimulationProperties.CadaverStock));
					int cadaversGained = Mathf.RoundToInt(this.ApplyCadaversGain(contender2.Empire, deadUnitsCount, remainingCadaverRecyclersCount, empiresWhichAlreadyReceivedCadavers));
					int cadaversPerBooster = Mathf.RoundToInt(contender2.Empire.GetPropertyValue(SimulationProperties.CadaverCountNeededToObtainBooster));
					int cadaverBoostersGained = (cadaversPerBooster == 0) ? 0 : ((cadaverCountBefore + cadaversGained) / cadaversPerBooster);
					int siegePointsCountBefore = Mathf.RoundToInt(contender2.Empire.GetPropertyValue(SimulationProperties.SiegeDamageStock));
					int siegePointsGained = Mathf.RoundToInt(this.ApplySiegePointsGain(contender2.Empire, siegePointsDone, empiresWhichAlreadyReceivedSiegePoints));
					int siegePointsPerBooster = Mathf.RoundToInt(contender2.Empire.GetPropertyValue(SimulationProperties.SiegeDamageNeededToObtainBooster));
					int industryStockpilesGained = (siegePointsPerBooster == 0) ? 0 : ((siegePointsCountBefore + siegePointsGained) / siegePointsPerBooster);
					int undeadUnitsGained = contender2.UndeadUnitsToCreateCount;
					int unitCountBeforeBattle = contenderInformationsByGroup[contender2.Group].UnitCountBeforeBattle;
					float experiencePerUnitGained = (unitCountBeforeBattle != 0) ? (overallExperienceRewards / (float)unitCountBeforeBattle) : 0f;
					foreach (Unit unit in contender2.Garrison.Units)
					{
						if (unit.GetPropertyValue(SimulationProperties.Health) > 0f)
						{
							unit.GainXp(experiencePerUnitGained, false, true);
						}
					}
					contender2.Empire.Refresh(false);
					contender2.Gains = new Contender.ContenderGains(experiencePerUnitGained, empirePointsGained, dustGained + overallEnemyKillsBountyReward, (float)cadaversGained, undeadUnitsGained, cadaverBoostersGained, (float)siegePointsGained, industryStockpilesGained);
				}
			}
		}
		encounter.EncounterState = EncounterState.BattleHasEnded;
		for (int index3 = 0; index3 < encounter.Contenders.Count; index3++)
		{
			this.CleanAndHealContender(encounter, encounter.Contenders[index3]);
		}
		if (!order.HasBeenCanceled)
		{
			for (int index4 = 0; index4 < encounter.Empires.Count; index4++)
			{
				EncounterResult empireResult = encounter.GetEncounterResultForEmpire(encounter.Empires[index4]);
				global::PlayerController serverPlayerController = encounter.Empires[index4].PlayerControllers.Server;
				if (empireResult == EncounterResult.Defeat && serverPlayerController != null)
				{
					foreach (Contender contender3 in encounter.GetAlliedContendersFromEmpire(encounter.Empires[index4]))
					{
						if (contender3.Garrison is Camp && contender3.IsMainContender)
						{
							OrderDestroyCamp destroyCampOrder = new OrderDestroyCamp(encounter.Empires[index4].Index, (contender3.Garrison as Camp).GUID);
							serverPlayerController.PostOrder(destroyCampOrder);
							index4 += encounter.Empires.Count;
							break;
						}
					}
				}
			}
		}
		IGameEntityRepositoryService gameEntityRepositoryService = this.Game.GetService<IGameEntityRepositoryService>();
		Diagnostics.Assert(gameEntityRepositoryService != null);
		if (gameEntityRepositoryService != null)
		{
			gameEntityRepositoryService.Unregister(encounter);
			yield break;
		}
		yield break;
	}

	private void SetContenderInformation(Contender contender, Dictionary<byte, GameClient.EndEncounterContenderInformations> contenderInformationsByGroup)
	{
		if (!contenderInformationsByGroup.ContainsKey(contender.Group))
		{
			contenderInformationsByGroup.Add(contender.Group, new GameClient.EndEncounterContenderInformations());
		}
		if (contender.IsTakingPartInBattle)
		{
			contenderInformationsByGroup[contender.Group].UnitCountBeforeBattle += contender.Garrison.UnitsCount;
		}
		bool flag = contender.IsRetreating && contender.IsMainContender;
		bool flag2 = true;
		foreach (EncounterUnit encounterUnit in contender.EncounterUnits)
		{
			if (encounterUnit.UnitDuplicatedSimulationObject != null && encounterUnit.GetPropertyValue(SimulationProperties.Health) <= 0f && encounterUnit.UnitDuplicatedSimulationObject.Tags.Contains("UnitActionParasite"))
			{
				contender.DeadParasitesCount++;
			}
			encounterUnit.ApplyBattleSimulationModification();
			bool flag3 = true;
			if (encounterUnit.Unit.GetPropertyValue(SimulationProperties.Health) > 0f)
			{
				flag2 = false;
				contender.ContenderState = ContenderState.Survived;
				if (!flag)
				{
					flag3 = false;
				}
			}
			if (flag3)
			{
				contenderInformationsByGroup[contender.Group].ExperienceRewardOnKillPoint += encounterUnit.Unit.GetPropertyValue(SimulationProperties.ExperienceReward);
			}
		}
		if (flag2)
		{
			contender.ContenderState = ContenderState.Defeated;
		}
	}

	private int ApplyKills(Encounter encounter, Dictionary<byte, GameClient.EndEncounterContenderInformations> contenderInformationsByGroup)
	{
		int num = 0;
		for (int i = 0; i < encounter.Contenders.Count; i++)
		{
			Contender contender = encounter.Contenders[i];
			Diagnostics.Assert(contender != null && contender.Garrison != null && contender.Garrison.Empire != null);
			if (contender != null && contender.Garrison != null && contender.Garrison.Empire != null)
			{
				List<Unit> collection = (from unit in contender.Garrison.Units
				where unit.GetPropertyValue(SimulationProperties.Health) <= 0f
				select unit).ToList<Unit>();
				contenderInformationsByGroup[contender.Group].UnitsKilledInAction.AddRange(collection);
				contender.Garrison.UpdateLifeAfterEncounter(encounter);
				GameClient.EndEncounterContenderInformations endEncounterContenderInformations = contenderInformationsByGroup[contender.Group];
				endEncounterContenderInformations.UnitCountAfterBattle += contender.Garrison.Units.Count((Unit match) => match.GetPropertyValue(SimulationProperties.Health) > 0f);
				num += contender.Garrison.Units.Count((Unit match) => match.GetPropertyValue(SimulationProperties.Health) <= 0f);
			}
		}
		return num;
	}

	private void ApplyFortificationLoss(Encounter encounter, out float siegePoints)
	{
		siegePoints = 0f;
		for (int i = 0; i < encounter.Contenders.Count; i++)
		{
			Contender contender = encounter.Contenders[i];
			Diagnostics.Assert(contender != null && contender.Garrison != null && contender.Garrison.Empire != null);
			if (contender != null && contender.Garrison != null && contender.Garrison.Empire != null)
			{
				if (contender.Garrison is EncounterCityGarrison && contender.Garrison.UnitsCount >= 1 && !contender.IsAttacking && contender.IsMainContender)
				{
					float num = 0f;
					if (contender.Garrison is EncounterCityGarrison)
					{
						City city = (contender.Garrison as EncounterCityGarrison).City;
						float propertyValue = city.GetPropertyValue(SimulationProperties.CityDefensePoint);
						float num2 = city.GetPropertyValue(SimulationProperties.MaximumCityDefensePoint);
						float num3 = propertyValue * DepartmentOfTheInterior.FortificationRecoverByOwnershipCurve.Evaluate(city.Ownership[city.Empire.Index]);
						if (num2 <= 0f)
						{
							num2 = propertyValue;
						}
						foreach (EncounterUnit encounterUnit in contender.EncounterUnits)
						{
							if (encounterUnit != null && encounterUnit.UnitDuplicatedSimulationObject != null)
							{
								float propertyValue2 = encounterUnit.UnitDuplicatedSimulationObject.GetPropertyValue(SimulationProperties.Armor);
								num += num3 - propertyValue2;
							}
						}
						float num4 = num / (float)contender.Garrison.UnitsCount;
						if (propertyValue > 0f)
						{
							float num5 = propertyValue - num4;
							if (num5 > 0f)
							{
								siegePoints = Mathf.Round(num4 * 100f / num2);
							}
							else
							{
								siegePoints = Mathf.Round(propertyValue * 100f / num2);
							}
						}
					}
					float propertyBaseValue = (contender.Garrison as EncounterCityGarrison).City.GetPropertyBaseValue(SimulationProperties.CityDefensePoint);
					float value = propertyBaseValue - num / (float)contender.Garrison.UnitsCount;
					(contender.Garrison as EncounterCityGarrison).City.SetPropertyBaseValue(SimulationProperties.CityDefensePoint, value);
				}
			}
		}
	}

	private int CountPrestigeKillsForThirdEmpire(global::Empire contenderEmpire, global::Empire otherContenderEmpire, GameClient.EndEncounterContenderInformations otherContenderInformations, global::Empire thirdEmpire)
	{
		Diagnostics.Assert(thirdEmpire is MajorEmpire);
		DepartmentOfForeignAffairs agency = contenderEmpire.GetAgency<DepartmentOfForeignAffairs>();
		Diagnostics.Assert(agency != null);
		if (thirdEmpire.Index == contenderEmpire.Index || thirdEmpire.Index == contenderEmpire.Index)
		{
			return 0;
		}
		DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(thirdEmpire);
		Diagnostics.Assert(diplomaticRelation != null);
		if (!diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.PrestigeForKill))
		{
			return 0;
		}
		DepartmentOfForeignAffairs agency2 = thirdEmpire.GetAgency<DepartmentOfForeignAffairs>();
		if (agency2 == null)
		{
			return 0;
		}
		DiplomaticRelation diplomaticRelation2 = agency2.GetDiplomaticRelation(otherContenderEmpire);
		Diagnostics.Assert(diplomaticRelation2 != null);
		if (diplomaticRelation2.State == null || (diplomaticRelation2.State.Name != DiplomaticRelationState.Names.War && diplomaticRelation2.State.Name != DiplomaticRelationState.Names.ColdWar))
		{
			return 0;
		}
		return otherContenderInformations.UnitCountBeforeBattle - otherContenderInformations.UnitCountAfterBattle;
	}

	private float CountBountyKillsReward(global::Empire otherContenderEmpire, GameClient.EndEncounterContenderInformations otherContenderInformations)
	{
		if (!(otherContenderEmpire is MajorEmpire))
		{
			return 0f;
		}
		if (!otherContenderEmpire.SimulationObject.Tags.Contains(DiplomaticAbilityDefinition.BlackSpotVictim))
		{
			return 0f;
		}
		float propertyValue = otherContenderEmpire.GetPropertyValue(SimulationProperties.DiplomaticAbilityBountyRewardOnKill);
		return (float)otherContenderInformations.UnitsKilledInAction.Count * propertyValue;
	}

	private float ApplyEmpirePointsGain(global::Empire empire, int overallEnemyPrestigeKills)
	{
		DepartmentOfTheTreasury agency = empire.GetAgency<DepartmentOfTheTreasury>();
		float num = 0f;
		if (overallEnemyPrestigeKills > 0 && agency != null)
		{
			float propertyValue = empire.GetPropertyValue(SimulationProperties.DiplomaticAbilityPrestigeRewardOnKill);
			num = (float)overallEnemyPrestigeKills * propertyValue;
			agency.TryTransferResources(empire, DepartmentOfTheTreasury.Resources.EmpirePoint, num);
		}
		return num;
	}

	private void ApplyBountyDustGain(global::Empire empire, float value)
	{
		DepartmentOfTheTreasury agency = empire.GetAgency<DepartmentOfTheTreasury>();
		if (value > 0f && agency != null)
		{
			agency.TryTransferResources(empire, DepartmentOfTheTreasury.Resources.EmpireMoney, value);
		}
	}

	private float ApplyDustGain(global::Empire empire, float overallExperienceRewards)
	{
		DepartmentOfTheTreasury agency = empire.GetAgency<DepartmentOfTheTreasury>();
		float num = 0f;
		if (DepartmentOfTheInterior.CanExtractDustFromExperience(empire) && agency != null)
		{
			float propertyValue = empire.GetPropertyValue(SimulationProperties.TraitBonusExperienceToDustConversionFactor);
			num = overallExperienceRewards * propertyValue;
			agency.TryTransferResources(empire, DepartmentOfTheTreasury.Resources.EmpireMoney, num);
		}
		return num;
	}

	private float ApplyCadaversGain(global::Empire empire, int deadUnitsCount, int cadaverRecyclerContenderAliveCount, List<int> empiresWhichAlreadyReceivedCadavers)
	{
		DepartmentOfTheTreasury agency = empire.GetAgency<DepartmentOfTheTreasury>();
		Diagnostics.Assert(agency != null);
		float num = 0f;
		if (cadaverRecyclerContenderAliveCount > 0 && DepartmentOfTheInterior.CanRecycleCadavers(empire) && !empiresWhichAlreadyReceivedCadavers.Contains(empire.Index))
		{
			num = (float)deadUnitsCount / (float)cadaverRecyclerContenderAliveCount;
			DepartmentOfScience agency2 = empire.GetAgency<DepartmentOfScience>();
			Diagnostics.Assert(agency2 != null);
			ISeasonService service = this.Game.Services.GetService<ISeasonService>();
			Diagnostics.Assert(service != null);
			if (service.GetCurrentSeason().SeasonDefinition.Name.ToString().Contains(Season.ReadOnlyWinter))
			{
				DepartmentOfScience.ConstructibleElement.State technologyState = agency2.GetTechnologyState("TechnologyDefinitionNecrophages11");
				if (technologyState == DepartmentOfScience.ConstructibleElement.State.Researched)
				{
					num *= 2f;
				}
			}
			else if (service.GetCurrentSeason().SeasonDefinition.Name.ToString().Contains(Season.ReadOnlyHeatWave) && empire.SimulationObject.Tags.Contains("FactionTraitNecrophagesHeatWave"))
			{
				num *= 2f;
			}
			agency.TryTransferResources(empire, DepartmentOfTheTreasury.Resources.Cadaver, num);
			empiresWhichAlreadyReceivedCadavers.Add(empire.Index);
		}
		return num;
	}

	private float ApplySiegePointsGain(global::Empire empire, float siegeDamage, List<int> empiresWhichAlreadyReceivedSiegePoints)
	{
		float result = 0f;
		DepartmentOfScience agency = empire.GetAgency<DepartmentOfScience>();
		if (agency != null && agency.GetTechnologyState("TechnologyDefinitionFlames9") == DepartmentOfScience.ConstructibleElement.State.Researched && !empiresWhichAlreadyReceivedSiegePoints.Contains(empire.Index))
		{
			DepartmentOfTheTreasury agency2 = empire.GetAgency<DepartmentOfTheTreasury>();
			if (agency2.TryTransferResources(empire, DepartmentOfTheTreasury.Resources.SiegeDamage, siegeDamage))
			{
				result = siegeDamage;
				empiresWhichAlreadyReceivedSiegePoints.Add(empire.Index);
			}
		}
		return result;
	}

	private void CleanAndHealContender(Encounter encounter, Contender contender)
	{
		Diagnostics.Assert(contender != null && contender.Garrison != null);
		if (contender == null || contender.Garrison == null)
		{
			return;
		}
		contender.Garrison.CleanAfterEncounter(encounter);
		if (contender.IsRetreating)
		{
			return;
		}
		DepartmentOfScience agency = contender.Empire.GetAgency<DepartmentOfScience>();
		if (agency != null)
		{
			DepartmentOfScience.ConstructibleElement.State technologyState = agency.GetTechnologyState("TechnologyDefinitionBrokenLords8");
			if (technologyState == DepartmentOfScience.ConstructibleElement.State.Researched)
			{
				DepartmentOfDefense.HealUnits(contender.Garrison.Units);
			}
		}
		contender.Empire.Refresh(false);
	}

	private IEnumerator GenerateNewWeatherProcessor(OrderGenerateNewWeather order)
	{
		IWeatherService weatherService = this.Game.GetService<IWeatherService>();
		string oldPresetName = string.Empty;
		if (weatherService != null)
		{
			oldPresetName = weatherService.PresetName;
			weatherService.GenerateWeatherMap(order.Seed, order.PresetName);
			weatherService.PresetName = order.PresetName;
		}
		bool wasControlled = !string.IsNullOrEmpty(oldPresetName);
		bool isControlled = !string.IsNullOrEmpty(order.PresetName);
		bool controlStartedThisTurn = weatherService.WeatherControlStartTurn == this.Game.Turn;
		IEventService eventService = Services.GetService<IEventService>();
		if (eventService != null)
		{
			for (int i = 0; i < this.Game.Empires.Length; i++)
			{
				if (this.Game.Empires[i] is MajorEmpire)
				{
					if (wasControlled && !isControlled && !controlStartedThisTurn)
					{
						eventService.Notify(new EventWeatherControlEnded(this.Game.Empires[i], oldPresetName));
					}
					else if (isControlled)
					{
						eventService.Notify(new EventWeatherControlStarted(this.Game.Empires[i], order.PresetName));
					}
				}
			}
		}
		yield break;
	}

	private IEnumerator GetAIAttitudeProcessor(OrderGetAIAttitude order)
	{
		yield break;
	}

	private IEnumerator GetAIDiplomaticContractEvaluationProcessor(OrderGetAIDiplomaticContractEvaluation order)
	{
		Diagnostics.Assert(order != null);
		if (!order.ContractGUID.IsValid)
		{
			Diagnostics.LogError("Contract GUID is not valid.");
			yield break;
		}
		IDiplomaticContractRepositoryService diplomaticContractRepositoryService = this.Game.Services.GetService<IDiplomaticContractRepositoryService>();
		Diagnostics.Assert(diplomaticContractRepositoryService != null);
		DiplomaticContract diplomaticContract;
		if (!diplomaticContractRepositoryService.TryGetValue(order.ContractGUID, out diplomaticContract))
		{
			Diagnostics.LogError("Can't retrieve the contract {0}.", new object[]
			{
				order.ContractGUID
			});
			yield break;
		}
		yield break;
	}

	private IEnumerator GetAIDiplomaticTermEvaluationProcessor(OrderGetAIDiplomaticTermEvaluation order)
	{
		yield break;
	}

	private IEnumerator IncludeContenderInEncounterProcessor(OrderIncludeContenderInEncounter order)
	{
		if (!order.EncounterGUID.IsValid)
		{
			Diagnostics.LogError("Encounter GUID is not valid.");
			yield break;
		}
		IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
		if (encounterRepositoryService == null)
		{
			Diagnostics.LogError("Can't found encounter repository service.");
			yield break;
		}
		Encounter encounter = null;
		if (!encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
		{
			Diagnostics.LogError("Can't found encounter.");
			yield break;
		}
		Diagnostics.Assert(encounter != null);
		encounter.IncludeContenderInEncounter(order.ContenderGUID, order.Include);
		yield break;
	}

	private IEnumerator InteractWithProcessor(OrderInteractWith order)
	{
		IGameEntityRepositoryService gameEntityRepositoryService = this.Game.Services.GetService<IGameEntityRepositoryService>();
		if (gameEntityRepositoryService == null)
		{
			Diagnostics.LogError("Unable to retrieve the game entity repository service.");
			yield break;
		}
		global::Empire empire = null;
		try
		{
			empire = this.Game.Empires[order.EmpireIndex];
		}
		catch
		{
		}
		if (order.Rewarding)
		{
			IEventService eventService = Services.GetService<IEventService>();
			IPlayerControllerRepositoryService playerControllerRepositoryService = this.Game.GetService<IPlayerControllerRepositoryService>();
			bool notify = playerControllerRepositoryService.ActivePlayerController.Empire.Index == order.EmpireIndex;
			if (order.QuestRewards != null && order.QuestRewards.Length > 0)
			{
				if (order.QuestRewards[0].Droppables == null || order.QuestRewards[0].Droppables.Length == 0)
				{
				}
				foreach (IDroppable droppable in order.QuestRewards[0].Droppables)
				{
					if (!(droppable is DroppableString))
					{
						IDroppableWithRewardAllocation droppableWithRewardAllocation = droppable as IDroppableWithRewardAllocation;
						if (droppableWithRewardAllocation != null && empire != null)
						{
							droppableWithRewardAllocation.AllocateRewardTo(empire, new object[0]);
						}
					}
				}
				if (notify && eventService != null)
				{
					bool IsDustDeposit = false;
					IGameEntity gameEntity;
					if (order.TargetGUID.IsValid && gameEntityRepositoryService.TryGetValue(order.TargetGUID, out gameEntity) && gameEntity is PointOfInterest)
					{
						PointOfInterest pointOfInterest = gameEntity as PointOfInterest;
						if (pointOfInterest.UntappedDustDeposits && SimulationGlobal.GlobalTagsContains(SeasonManager.RuinDustDepositsTag))
						{
							IsDustDeposit = true;
						}
					}
					if (IsDustDeposit)
					{
						eventService.Notify(new EventInteractionSucceeded(order.InstigatorGUID, order.QuestRewards[0].Droppables, EventInteractionSucceeded.DustEclipseOverride));
					}
					else
					{
						eventService.Notify(new EventInteractionSucceeded(order.InstigatorGUID, order.QuestRewards[0].Droppables, EventInteractionSucceeded.Name));
					}
				}
			}
			else
			{
				if (order.Tags.Contains("Talk") || order.Tags.Contains("NavalTalk"))
				{
					if (notify)
					{
						QuickWarningPanel.Show("%WarningNoVillageQuestTriggered");
					}
					yield break;
				}
				if (notify && eventService != null)
				{
					eventService.Notify(new EventInteractionFailed(order.InstigatorGUID));
				}
			}
		}
		IGameEntity gameEntity2;
		if (order.NumberOfActionPointsToSpend > 0f && gameEntityRepositoryService.TryGetValue(order.InstigatorGUID, out gameEntity2))
		{
			ArmyAction.SpendSomeNumberOfActionPoints(gameEntity2, order.NumberOfActionPointsToSpend);
		}
		IGameEntity gameEntity3;
		if (order.TargetGUID.IsValid && gameEntityRepositoryService.TryGetValue(order.TargetGUID, out gameEntity3) && gameEntity3 is PointOfInterest)
		{
			PointOfInterest pointOfInterest2 = gameEntity3 as PointOfInterest;
			if (pointOfInterest2.UntappedDustDeposits && SimulationGlobal.GlobalTagsContains(SeasonManager.RuinDustDepositsTag))
			{
				pointOfInterest2.UntappedDustDeposits = false;
				pointOfInterest2.Interaction.Bits = pointOfInterest2.Interaction.Bits;
				if (empire != null)
				{
					IEventService eventService2 = Services.GetService<IEventService>();
					if (eventService2 != null)
					{
						EventDustDepositSearched eventDustDepositSearched = new EventDustDepositSearched(empire);
						eventService2.Notify(eventDustDepositSearched);
					}
				}
			}
			else
			{
				pointOfInterest2.Interaction.Bits |= 1 << order.EmpireIndex;
			}
		}
		if (!StaticString.IsNullOrEmpty(order.ArmyActionName))
		{
			ArmyAction armyAction = null;
			IDatabase<ArmyAction> armyActionDatabase = Databases.GetDatabase<ArmyAction>(false);
			IGameEntity gameEntity4;
			if (armyActionDatabase.TryGetValue(order.ArmyActionName, out armyAction) && gameEntityRepositoryService.TryGetValue(order.InstigatorGUID, out gameEntity4) && gameEntity4 is Army)
			{
				Army army = gameEntity4 as Army;
				if (army.Hero != null && armyAction.ExperienceReward > 0f)
				{
					army.Hero.GainXp(armyAction.ExperienceReward, false, true);
				}
				if (order.ArmyActionName == FleetAction_Dive.ReadOnlyName)
				{
					foreach (Unit unit in army.Units)
					{
						unit.SetPropertyBaseValue(SimulationProperties.MovementRatio, 0f);
						unit.Refresh(false);
					}
					IGuiService guiService = Services.GetService<IGuiService>();
					guiService.GetGuiPanel<EndTurnPanel>().Refresh(new object[0]);
					army.Refresh(false);
				}
			}
		}
		yield break;
	}

	private IEnumerator JoinEncounterProcessor(OrderJoinEncounter order)
	{
		if (order.EncounterGUID.IsValid)
		{
			IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
			Diagnostics.Assert(encounterRepositoryService != null);
			if (encounterRepositoryService != null)
			{
				Encounter encounter = null;
				if (encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
				{
					for (int contenderIndex = 0; contenderIndex < order.ContenderInfos.Count; contenderIndex++)
					{
						OrderJoinEncounter.ContenderInfo currentContender = order.ContenderInfos[contenderIndex];
						if (currentContender.IsValid)
						{
							Contender contender = null;
							if (encounter.Join(currentContender.ContenderGUID, currentContender.IsCity, currentContender.IsCamp, currentContender.IsVillage, currentContender.Group, currentContender.IsReinforcement, currentContender.Deployment, currentContender.ReinforcementRanking, out contender))
							{
								contender.IsTakingPartInBattle = currentContender.IsTakingPartInBattle;
								contender.CanTakePartInBattle = currentContender.IsTakingPartInBattle;
							}
						}
					}
				}
			}
		}
		yield break;
	}

	private IEnumerator LockEncounterExternalArmiesProcessor(OrderLockEncounterExternalArmies order)
	{
		if (order.EncounterGUID.IsValid)
		{
			IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
			Diagnostics.Assert(encounterRepositoryService != null);
			if (encounterRepositoryService != null)
			{
				Encounter encounter = null;
				if (encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
				{
					for (int armyIndex = 0; armyIndex < order.ArmyGuids.Count; armyIndex++)
					{
						encounter.JoinAsSpectator(order.ArmyGuids[armyIndex], order.Locked);
					}
				}
			}
		}
		yield break;
	}

	private IEnumerator LockInteractionProcessor(OrderLockInteraction order)
	{
		IGameService gameService = Services.GetService<IGameService>();
		if (gameService == null)
		{
			Diagnostics.LogError("Failed to retrieve the game service.");
			yield break;
		}
		IGameEntityRepositoryService gameEntityRepositoryService = gameService.Game.Services.GetService<IGameEntityRepositoryService>();
		if (gameEntityRepositoryService == null)
		{
			Diagnostics.LogError("Failed to retrieve the game entity repository service.");
			yield break;
		}
		List<PointOfInterest> pointOfInterests = new List<PointOfInterest>();
		for (int targetIndex = 0; targetIndex < order.TargetsGUID.Length; targetIndex++)
		{
			ulong currentGUID = order.TargetsGUID[targetIndex];
			if (currentGUID == 0UL)
			{
				Diagnostics.LogError("OrderLockInteraction: Target's GUID isn't valid");
				yield break;
			}
			IGameEntity targetGameEntity;
			if (!gameEntityRepositoryService.TryGetValue(currentGUID, out targetGameEntity))
			{
				Diagnostics.LogError("OrderLockInteraction: Cannot retrieve target game entity (GUID = '{0}')", new object[]
				{
					currentGUID
				});
				yield break;
			}
			PointOfInterest pointOfInterest = targetGameEntity as PointOfInterest;
			if (pointOfInterest == null)
			{
				Diagnostics.LogError("OrderLockInteraction: target game entity isn't a point of interest (GUID = '{0}')", new object[]
				{
					currentGUID
				});
				yield break;
			}
			pointOfInterests.Add(pointOfInterest);
		}
		for (int targetIndex2 = 0; targetIndex2 < pointOfInterests.Count; targetIndex2++)
		{
			if (order.ShouldLock)
			{
				pointOfInterests[targetIndex2].Interaction.AddInteractionLock(order.EmpireIndex, order.InteractionName);
			}
			else
			{
				pointOfInterests[targetIndex2].Interaction.RemoveInteractionLock(order.EmpireIndex, order.InteractionName);
			}
		}
		yield break;
	}

	private IEnumerator NotifyEmpireDiscoveryProcessor(OrderNotifyEmpireDiscovery order)
	{
		global::Empire initiatorEmpire;
		global::Empire discoveredEmpire;
		try
		{
			initiatorEmpire = this.Game.Empires[order.InitiatorEmpireIndex];
			discoveredEmpire = this.Game.Empires[order.DiscoveredEmpireIndex];
		}
		catch
		{
			Diagnostics.LogError("OrderNotifyEmpireDiscovery, Empire index are invalid.");
			yield break;
		}
		IForeignAffairsManagment initiatorEmpireDiplomaticRelationManagment = initiatorEmpire.GetAgency<DepartmentOfForeignAffairs>();
		if (initiatorEmpireDiplomaticRelationManagment != null)
		{
			initiatorEmpireDiplomaticRelationManagment.SetDiplomaticRelationState(discoveredEmpire, DiplomaticRelationState.Names.ColdWar, false);
		}
		IForeignAffairsManagment targetEmpireDiplomaticRelationManagment = discoveredEmpire.GetAgency<DepartmentOfForeignAffairs>();
		if (targetEmpireDiplomaticRelationManagment != null)
		{
			targetEmpireDiplomaticRelationManagment.SetDiplomaticRelationState(initiatorEmpire, DiplomaticRelationState.Names.ColdWar, false);
		}
		DepartmentOfForeignAffairs departmentOfForeignAffairs = initiatorEmpire.GetAgency<DepartmentOfForeignAffairs>();
		if (departmentOfForeignAffairs != null)
		{
			DiplomaticRelation diplomaticRelation = departmentOfForeignAffairs.GetDiplomaticRelation(discoveredEmpire);
			if (diplomaticRelation != null)
			{
				diplomaticRelation.RefreshDiplomaticAbilities();
			}
		}
		departmentOfForeignAffairs = discoveredEmpire.GetAgency<DepartmentOfForeignAffairs>();
		if (departmentOfForeignAffairs != null)
		{
			DiplomaticRelation diplomaticRelation = departmentOfForeignAffairs.GetDiplomaticRelation(initiatorEmpire);
			if (diplomaticRelation != null)
			{
				diplomaticRelation.RefreshDiplomaticAbilities();
			}
		}
		IEventService eventService = Services.GetService<IEventService>();
		Diagnostics.Assert(eventService != null);
		eventService.Notify(new EventMajorFactionDiscovery(initiatorEmpire, discoveredEmpire, order.DiscoveredEmpirePosition));
		eventService.Notify(new EventMajorFactionDiscovery(discoveredEmpire, initiatorEmpire, order.InitiatorEmpirePosition));
		yield break;
	}

	private IEnumerator NotifyEncounterProcessor(OrderNotifyEncounter order)
	{
		if (order.EncounterGUID.IsValid)
		{
			IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
			Diagnostics.Assert(encounterRepositoryService != null);
			if (encounterRepositoryService != null)
			{
				Encounter encounter = null;
				if (encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
				{
					encounter.Instant = order.Instant;
					encounter.SetupPhaseTime = new Encounter.PhaseTime(order.SetupEndTime, order.SetupDuration);
					encounter.EncounterState = EncounterState.Setup;
				}
			}
		}
		yield break;
	}

	private IEnumerator OrbsChangeProcessor(OrderOrbsChange order)
	{
		IOrbService orbService = this.Game.GetService<IOrbService>();
		if (orbService != null)
		{
			orbService.ApplyOrbsChange(order.WorldPositions, order.OrbsQuantity);
			orbService.CheckArmiesOverOrbsAfterDistribution();
			IEventService eventService = Services.GetService<IEventService>();
			if (!order.IsDestruction)
			{
				eventService.Notify(new EventOrbChange(null, order.WorldPositions, order.OrbsQuantity));
			}
			else
			{
				eventService.Notify(new EventOrbDestroyed(null, order.WorldPositions, order.OrbsQuantity));
			}
		}
		yield break;
	}

	private IEnumerator PacifyMinorFactionProcessor(OrderPacifyMinorFaction order)
	{
		global::Empire targetEmpire = this.Game.Empires[order.MinorEmpireIndex];
		global::Empire empire = this.Game.Empires[order.EmpireIndex];
		if (targetEmpire is MinorEmpire)
		{
			BarbarianCouncil council = targetEmpire.GetAgency<BarbarianCouncil>();
			council.PacifyRemainingVillages(new global::Empire[]
			{
				empire
			}, order.IgnoreIfConverted);
		}
		yield break;
	}

	private IEnumerator PlayBattleActionProcessor(OrderPlayBattleAction order)
	{
		if (!order.EncounterGUID.IsValid)
		{
			Diagnostics.LogError("Encounter GUID is not valid.");
			yield break;
		}
		IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
		if (encounterRepositoryService == null)
		{
			Diagnostics.LogError("Can't found encounter repository service.");
			yield break;
		}
		Encounter encounter = null;
		if (!encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
		{
			Diagnostics.LogError("Can't found encounter.");
			yield break;
		}
		Diagnostics.Assert(encounter != null);
		IDatabase<BattleAction> battleActionDatabase = Databases.GetDatabase<BattleAction>(false);
		if (battleActionDatabase == null)
		{
			Diagnostics.LogError("Can't retrieve the battle action database.");
			yield break;
		}
		BattleAction battleAction;
		if (!battleActionDatabase.TryGetValue(order.BattleActionUserName, out battleAction))
		{
			Diagnostics.LogError("Can't retrieve the battle action {0}.", new object[]
			{
				order.BattleActionUserName
			});
			yield break;
		}
		if (!(battleAction is BattleActionUser))
		{
			Diagnostics.LogError("The battle action {0} is not a user battle action.", new object[]
			{
				order.BattleActionUserName
			});
			yield break;
		}
		if (encounter.Contenders == null)
		{
			Diagnostics.LogError("Can't found encounter contender.");
			yield break;
		}
		Contender foundContender = encounter.Contenders.Find((Contender contender) => contender.GUID == order.ContenderGUID);
		if (foundContender == null)
		{
			Diagnostics.LogError("Can't found encounter contender.");
			yield break;
		}
		foundContender.ReportBattleAction(order.BattleActionUserName, BattleAction.State.Selected);
		yield break;
	}

	private IEnumerator PrepareForBattleProcessor(OrderPrepareForBattle order)
	{
		if (order.EncounterGUID.IsValid)
		{
			IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
			Diagnostics.Assert(encounterRepositoryService != null);
			if (encounterRepositoryService != null)
			{
				Encounter encounter = null;
				if (encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
				{
					encounter.EncounterState = EncounterState.BattleIsPending;
				}
			}
		}
		yield break;
	}

	private IEnumerator QuestWorldEffectProcessor(OrderQuestWorldEffect order)
	{
		IQuestManagementService questManagementService = this.Game.GetService<IQuestManagementService>();
		Diagnostics.Assert(questManagementService != null);
		questManagementService.ApplyQuestWorldEffect(order);
		yield break;
	}

	private IEnumerator QueueKaijuResearchProcessor(OrderQueueKaijuResearch order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (order.ConstructionGameEntityGUID == GameEntityGUID.Zero)
		{
			Diagnostics.LogError("Skipping queue construction process because the game entity guid is null.");
			yield break;
		}
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		IDatabase<DepartmentOfScience.ConstructibleElement> technologyDatabase = Databases.GetDatabase<DepartmentOfScience.ConstructibleElement>(false);
		Diagnostics.Assert(technologyDatabase != null);
		IGameEntityRepositoryService gameEntityRepositoryService = gameService.Game.Services.GetService<IGameEntityRepositoryService>();
		Diagnostics.Assert(gameEntityRepositoryService != null);
		IKaijuTechsService kaijuTechsService = gameService.Game.Services.GetService<IKaijuTechsService>();
		Diagnostics.Assert(kaijuTechsService != null);
		global::Game game = gameService.Game as global::Game;
		global::Empire empire = game.Empires[order.EmpireIndex];
		if (empire == null)
		{
			Diagnostics.LogError("QueueKaijuResearchProcessor: Skipping queue construction process because the empire is not valid");
			yield break;
		}
		Diagnostics.Assert(technologyDatabase != null);
		DepartmentOfScience.ConstructibleElement constructibleElement;
		if (!technologyDatabase.TryGetValue(order.ConstructibleElementName, out constructibleElement))
		{
			Diagnostics.LogError("QueueKaijuResearchProcessor: Skipping queue construction process because the constructible element {0} is not in the constructible element database.", new object[]
			{
				order.ConstructibleElementName
			});
			yield break;
		}
		DepartmentOfScience.ConstructibleElement.State technologyState = kaijuTechsService.GetTechnologyState(constructibleElement, empire);
		if (technologyState != DepartmentOfScience.ConstructibleElement.State.Available)
		{
			Diagnostics.LogError("QueueKaijuResearchProcessor: Skipping queue construction process because the constructible element {0} is not available ({1}).", new object[]
			{
				order.ConstructibleElementName,
				technologyState
			});
			yield break;
		}
		Diagnostics.Assert(empire != null && empire.Faction != null && empire.Faction.AffinityMapping != null);
		Construction construction = new Construction(constructibleElement, order.ConstructionGameEntityGUID, empire.Faction.AffinityMapping.Name, empire);
		IDatabase<SimulationDescriptor> simulationDescriptorDatatable = Databases.GetDatabase<SimulationDescriptor>(false);
		SimulationDescriptor classImprovementDescriptor;
		if (simulationDescriptorDatatable != null && simulationDescriptorDatatable.TryGetValue("ClassConstruction", out classImprovementDescriptor))
		{
			construction.AddDescriptor(classImprovementDescriptor, false);
		}
		ConstructionQueue selectedQueue = kaijuTechsService.GetConstructionQueueForEmpire(empire);
		Diagnostics.Assert(selectedQueue != null);
		selectedQueue.Enqueue(construction);
		gameEntityRepositoryService.Register(construction);
		kaijuTechsService.InvokeResearchQueueChanged(construction, ConstructionChangeEventAction.Started);
		yield break;
	}

	private IEnumerator RazePointOfInterestProcessor(OrderRazePointOfInterest order)
	{
		IGameEntityRepositoryService gameEntityRepositoryService = this.Game.Services.GetService<IGameEntityRepositoryService>();
		Diagnostics.Assert(gameEntityRepositoryService != null);
		IGameEntity gameEntity;
		if (!gameEntityRepositoryService.TryGetValue(order.PointOfInterestGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			yield break;
		}
		if (!(gameEntity is PointOfInterest))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a point of interest.");
			yield break;
		}
		PointOfInterest pointOfInterest = gameEntity as PointOfInterest;
		if (pointOfInterest.PointOfInterestImprovement != null)
		{
			pointOfInterest.RemovePointOfInterestImprovement();
			IWorldPositionningService worldPositionningService = this.Game.Services.GetService<IWorldPositionningService>();
			Diagnostics.Assert(worldPositionningService != null);
			Region region = worldPositionningService.GetRegion(pointOfInterest.WorldPosition);
			if (region != null && region.City != null)
			{
				region.City.Refresh(false);
				pointOfInterest.LineOfSightDirty = true;
			}
		}
		yield break;
	}

	private IEnumerator ReadyForBattleProcessor(OrderReadyForBattle order)
	{
		if (order.EncounterGUID.IsValid)
		{
			IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
			Diagnostics.Assert(encounterRepositoryService != null);
			if (encounterRepositoryService != null)
			{
				Encounter encounter = null;
				if (encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
				{
					encounter.SetReadyForBattle(order.ContenderGUID);
				}
			}
		}
		yield break;
	}

	private IEnumerator ReadyForDeploymentProcessor(OrderReadyForDeployment order)
	{
		if (order.EncounterGUID.IsValid)
		{
			IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
			Diagnostics.Assert(encounterRepositoryService != null);
			if (encounterRepositoryService != null)
			{
				Encounter encounter = null;
				if (encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
				{
					encounter.SetContenderIsRetreating(order.ContenderGUID, order.IsRetreating);
					encounter.SetContenderOptionChoice(order.ContenderGUID, order.ContenderEncounterOptionChoice);
					encounter.SetReadyForDeployment(order.ContenderGUID);
					encounter.ChangeDeployment(order.ContenderGUID, order.Deployment);
				}
			}
		}
		yield break;
	}

	private IEnumerator ReadyForNextPhaseProcessor(OrderReadyForNextPhase order)
	{
		if (order.EncounterGUID.IsValid)
		{
			IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
			Diagnostics.Assert(encounterRepositoryService != null);
			if (encounterRepositoryService != null)
			{
				Encounter encounter = null;
				if (encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
				{
					encounter.SetReadyForNextPhase(order.ContenderGUID);
				}
			}
		}
		yield break;
	}

	private IEnumerator ReadyForNextRoundProcessor(OrderReadyForNextRound order)
	{
		if (order.EncounterGUID.IsValid)
		{
			IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
			Diagnostics.Assert(encounterRepositoryService != null);
			if (encounterRepositoryService != null)
			{
				Encounter encounter = null;
				if (encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
				{
					encounter.SetReadyForNextRound(order.ContenderGUID);
				}
			}
		}
		yield break;
	}

	private IEnumerator RefreshMarketplaceProcessor(OrderRefreshMarketplace order)
	{
		if (order.Bytes != null && order.Bytes.Length > 0)
		{
			IGameService gameService = Services.GetService<IGameService>();
			if (gameService != null && gameService.Game != null)
			{
				ITradeManagementService tradeManagementService = gameService.Game.Services.GetService<ITradeManagementService>();
				if (tradeManagementService != null)
				{
					IBinarySerializable binarySerializable = tradeManagementService as IBinarySerializable;
					if (binarySerializable != null)
					{
						tradeManagementService.Clear();
						using (MemoryStream stream = new MemoryStream(order.Bytes))
						{
							using (System.IO.BinaryReader reader = new System.IO.BinaryReader(stream))
							{
								binarySerializable.Deserialize(reader);
							}
						}
					}
				}
			}
		}
		yield break;
	}

	private IEnumerator RegisterRegionalEffectsProcessor(OrderRegisterRegionalEffects order)
	{
		IGameEntityRepositoryService gameEntityRepository = base.GameService.Game.Services.GetService<IGameEntityRepositoryService>();
		IWorldPositionningService worldPositionningService = base.GameService.Game.Services.GetService<IWorldPositionningService>();
		IRegionalEffectsService regionalEffectsService = base.GameService.Game.Services.GetService<IRegionalEffectsService>();
		IDatabase<RegionalEffectDefinition> effectsDatabase = Databases.GetDatabase<RegionalEffectDefinition>(false);
		IRegionalEffectsProviderGameEntity entity = null;
		if (!gameEntityRepository.TryGetValue<IRegionalEffectsProviderGameEntity>(order.EffectsProviderGUID, out entity))
		{
			Diagnostics.LogError("Order processor failed because the GameEntity could not be found. GUID: {0}.", new object[]
			{
				order.EffectsProviderGUID
			});
			yield break;
		}
		for (int dataIndex = 0; dataIndex < order.EffectsData.Length; dataIndex++)
		{
			OrderRegisterRegionalEffects.EffectData effectData = order.EffectsData[dataIndex];
			RegionalEffectDefinition effectDefinition = null;
			if (effectsDatabase.TryGetValue(effectData.DefinitionName, out effectDefinition))
			{
				worldPositionningService.GetRegion(effectData.TargetRegionIndex).Effects.Register(new RegionalEffect(order.EffectsProviderGUID, entity.Empire.Index, effectData.GUID, effectDefinition));
			}
			else
			{
				Diagnostics.LogError("Order processor could not find the RegionalEffectDefinition. Name: {0}.", new object[]
				{
					effectData.DefinitionName
				});
			}
		}
		regionalEffectsService.OrderRegisterRegionalEffectsProcessed(order);
		yield break;
	}

	private IEnumerator RelocateKaijuProcessor(OrderRelocateKaiju order)
	{
		IGameEntityRepositoryService gameEntityRepositoryService = this.Game.Services.GetService<IGameEntityRepositoryService>();
		if (gameEntityRepositoryService == null)
		{
			Diagnostics.LogError("Cannot retreive the gameEntityRepositoryService.");
			yield break;
		}
		Kaiju kaiju = null;
		if (gameEntityRepositoryService.TryGetValue<Kaiju>(order.KaijuGUID, out kaiju))
		{
			kaiju.MoveToRegion(order.TargetPosition);
			kaiju.KaijuEmpire.GetAgency<KaijuCouncil>().ResetRelocationETA();
			IEventService eventService = Services.GetService<IEventService>();
			eventService.Notify(new EventKaijuRelocated(kaiju));
		}
		if (order.EmpireCosts.Length != 0)
		{
			MajorEmpire kaijuOwner = kaiju.MajorEmpire;
			if (kaijuOwner == null)
			{
				Diagnostics.LogError("Order processor failed because the Kaiju's Owner is not a Major Empire.");
				yield break;
			}
			DepartmentOfTheTreasury departmentOfTheTreasury = kaijuOwner.GetAgency<DepartmentOfTheTreasury>();
			if (departmentOfTheTreasury == null)
			{
				Diagnostics.LogError("Order processor failed because the Kaiju's Owner does not have a Department of the Treasury.");
				yield break;
			}
			for (int costIndex = 0; costIndex < order.EmpireCosts.Length; costIndex++)
			{
				KeyValuePair<StaticString, float> cost = order.EmpireCosts[costIndex];
				if (!departmentOfTheTreasury.TryTransferResources(kaijuOwner, cost.Key, cost.Value))
				{
					Diagnostics.LogError("Resources transfer failed. Resource Name: {0}, Amount: {1}", new object[]
					{
						cost.Key,
						cost.Value
					});
				}
			}
		}
		if (order.NumberOfActionPointsToSpend > 0f)
		{
			GarrisonAction.SpendSomeNumberOfActionPoints(kaiju.KaijuGarrison, order.NumberOfActionPointsToSpend);
		}
		if (order.GarrisonActionCooldownDuration > 0f)
		{
			GarrisonActionWithCooldown.ApplyCooldown(kaiju.KaijuGarrison, order.GarrisonActionCooldownDuration);
		}
		yield break;
	}

	private IEnumerator RemoveAffinityStrategicResourceProcessor(OrderRemoveAffinityStrategicResource order)
	{
		global::Empire empire = this.Game.Empires[order.EmpireIndex];
		StaticString descriptorName = new StaticString("Affinity" + order.ResourceName);
		IDatabase<SimulationDescriptor> simulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
		SimulationDescriptor descriptor;
		if (simulationDescriptorDatabase.TryGetValue(descriptorName, out descriptor))
		{
			empire.RemoveDescriptor(descriptor);
		}
		else
		{
			empire.SimulationObject.Tags.RemoveTag(descriptorName);
		}
		descriptorName = OrderRemoveAffinityStrategicResource.AffinityResourceChosenDescriptor;
		if (simulationDescriptorDatabase.TryGetValue(descriptorName, out descriptor))
		{
			empire.RemoveDescriptor(descriptor);
		}
		else
		{
			empire.SimulationObject.Tags.RemoveTag(descriptorName);
		}
		IEventService eventService = Services.GetService<IEventService>();
		if (eventService != null)
		{
			eventService.Notify(new EventAffinityStrategicResourceChanged(empire));
		}
		yield break;
	}

	private IEnumerator RemoveMapBoostsProcessor(OrderRemoveMapBoosts order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		IMapBoostService terraformDeviceService = this.Game.Services.GetService<IMapBoostService>();
		terraformDeviceService.RemoveMapBoosts();
		yield break;
	}

	private IEnumerator ReplicateMarketplaceUnitDesignProcessor(OrderReplicateMarketplaceUnitDesign order)
	{
		ITradeManagementService tradeManagementService = this.Game.Services.GetService<ITradeManagementService>();
		UnitDesign unitDesign;
		if (!tradeManagementService.TryRetrieveUnitDesign(order.Barcode, out unitDesign))
		{
			IDatabase<UnitBodyDefinition> unitBodyDefinitions = Databases.GetDatabase<UnitBodyDefinition>(false);
			Diagnostics.Assert(unitBodyDefinitions != null);
			UnitBodyDefinition unitBodyDefinition = unitBodyDefinitions.GetValue(order.UnitBodyDefinitionReference);
			Diagnostics.Assert(unitBodyDefinition != null);
			unitDesign = new UnitDesign(order.UnitDesignName);
			unitDesign.UnitBodyDefinitionReference = new XmlNamedReference(order.UnitBodyDefinitionReference);
			unitDesign.XmlSerializableUnitEquipmentSet = (order.UnitEquipmentSet.Clone() as UnitEquipmentSet);
			unitDesign.Barcode = order.Barcode;
			unitDesign.Model = 0u;
			unitDesign.ModelRevision = 0u;
			unitDesign.Tags.AddTag(TradableUnit.ReadOnlyMercenary);
			tradeManagementService.ReplicateUnitDesign(unitDesign);
		}
		else
		{
			Diagnostics.Assert(unitDesign.Barcode == order.Barcode);
		}
		yield break;
	}

	private IEnumerator ReplicateMarketplaceUnitProfileProcessor(OrderReplicateMarketplaceUnitProfile order)
	{
		ITradeManagementService tradeManagementService = this.Game.Services.GetService<ITradeManagementService>();
		UnitDesign unitDesign;
		if (!tradeManagementService.TryRetrieveUnitDesign(order.Barcode, out unitDesign))
		{
			IDatabase<UnitBodyDefinition> unitBodyDefinitions = Databases.GetDatabase<UnitBodyDefinition>(false);
			Diagnostics.Assert(unitBodyDefinitions != null);
			UnitBodyDefinition unitBodyDefinition = unitBodyDefinitions.GetValue(order.UnitBodyDefinitionReference);
			Diagnostics.Assert(unitBodyDefinition != null);
			IDatabase<UnitProfile> unitProfileDatabase = Databases.GetDatabase<UnitProfile>(false);
			Diagnostics.Assert(unitProfileDatabase != null);
			UnitProfile unitProfile;
			if (unitProfileDatabase.TryGetValue(order.UnitProfileName, out unitProfile))
			{
				unitProfile = (UnitProfile)unitProfile.Clone();
				unitProfile.UnitBodyDefinitionReference = new XmlNamedReference(order.UnitBodyDefinitionReference);
				unitProfile.XmlSerializableUnitEquipmentSet = (order.UnitEquipmentSet.Clone() as UnitEquipmentSet);
				unitProfile.Barcode = order.Barcode;
				unitProfile.LocalizationKey = order.LocalizationKey;
				unitProfile.Model = 0u;
				unitProfile.ModelRevision = 0u;
				unitProfile.UserDefinedName = order.UserDefinedName;
				unitProfile.Tags.AddTag(TradableUnit.ReadOnlyHero);
				unitProfile.Tags.AddTag(TradableUnit.ReadOnlyHeroExclusive);
				tradeManagementService.ReplicateUnitDesign(unitProfile);
			}
		}
		else
		{
			Diagnostics.Assert(unitDesign.Barcode == order.Barcode);
		}
		yield break;
	}

	private IEnumerator ReplicateMarketplaceUnitsProcessor(OrderReplicateMarketplaceUnits order)
	{
		Diagnostics.Assert(order.Barcodes != null);
		Diagnostics.Assert(order.GameEntityGUIDs != null);
		Diagnostics.Assert(order.Levels != null);
		Diagnostics.Assert(order.UnlockedSkills != null);
		int count = order.Barcodes.Length;
		Diagnostics.Assert(order.GameEntityGUIDs.Length == count);
		Diagnostics.Assert(order.Levels.Length == count);
		Diagnostics.Assert(order.UnlockedSkills.Length == count);
		ITradeManagementService tradeManagementService = this.Game.Services.GetService<ITradeManagementService>();
		for (int index = 0; index < order.Barcodes.Length; index++)
		{
			UnitDesign unitDesign;
			if (tradeManagementService.TryRetrieveUnitDesign(order.Barcodes[index], out unitDesign))
			{
				Unit unit;
				if (!tradeManagementService.TryRetrieveUnit(order.GameEntityGUIDs[index], out unit))
				{
					unit = DepartmentOfDefense.CreateUnitByDesign(order.GameEntityGUIDs[index], unitDesign);
					unit.Level = (int)order.Levels[index];
					tradeManagementService.ReplicateUnit(unit);
					foreach (KeyValuePair<StaticString, int> kvp in order.UnlockedSkills[index])
					{
						unit.UnlockSkill(kvp.Key, kvp.Value);
					}
				}
			}
			else
			{
				Diagnostics.LogError("Failed to retrieve the unit design (barcode: {0}) from the marketplace.", new object[]
				{
					order.Barcodes[index]
				});
			}
		}
		yield break;
	}

	private IEnumerator ReportEncounterProcessor(OrderReportEncounter order)
	{
		if (order.EncounterGUID.IsValid)
		{
			IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
			Diagnostics.Assert(encounterRepositoryService != null);
			if (encounterRepositoryService != null)
			{
				Encounter encounter = null;
				if (encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
				{
					encounter.EncounterState = EncounterState.BattleIsReporting;
				}
			}
		}
		yield break;
	}

	private IEnumerator ResetPointOfInterestInteractionBitsProcessor(OrderResetPointOfInterestInteractionBits order)
	{
		int inversedBitsToRemove = ~order.BitsToRemove;
		for (int regionIndex = 0; regionIndex < this.Game.World.Regions.Length; regionIndex++)
		{
			Region currentRegion = this.Game.World.Regions[regionIndex];
			if (currentRegion.PointOfInterests != null)
			{
				for (int pointOfInterestIndex = 0; pointOfInterestIndex < currentRegion.PointOfInterests.Length; pointOfInterestIndex++)
				{
					currentRegion.PointOfInterests[pointOfInterestIndex].Interaction.Bits &= inversedBitsToRemove;
				}
			}
		}
		yield break;
	}

	private IEnumerator RunTerraformationForDeviceProcessor(OrderRunTerraformationForDevice order)
	{
		ITerraformDeviceService terraformDeviceService = this.Game.Services.GetService<ITerraformDeviceService>();
		Diagnostics.Assert(terraformDeviceService != null);
		TerraformDeviceManager terraformDeviceManager = terraformDeviceService as TerraformDeviceManager;
		Diagnostics.Assert(terraformDeviceManager != null);
		TerraformDevice terraformDevice = terraformDeviceManager[order.DeviceGUID] as TerraformDevice;
		if (terraformDeviceService == null)
		{
			Diagnostics.LogError("Terraform device is 'null'.");
			yield break;
		}
		terraformDevice.OnActivation();
		WorldPosition[] terraformedTiles = this.Game.World.PerformTerraformation(terraformDevice.GetTilesInRange(), false);
		if (terraformedTiles.Length > 0)
		{
			this.Game.World.UpdateTerraformStateMap(true);
			global::Empire terraformingEmpire = terraformDevice.Empire;
			float tilesTerraformedCount = terraformingEmpire.GetPropertyValue(SimulationProperties.TilesTerraformed);
			terraformingEmpire.SetPropertyBaseValue(SimulationProperties.TilesTerraformed, tilesTerraformedCount + (float)terraformedTiles.Length);
			terraformingEmpire.Refresh(false);
			IEventService eventService = Services.GetService<IEventService>();
			eventService.Notify(new EventEmpireWorldTerraformed(terraformDevice.Empire, terraformedTiles));
		}
		terraformDeviceManager.DestroyDevice(terraformDevice);
		yield break;
	}

	private IEnumerator SelectAffinityStrategicResourceProcessor(OrderSelectAffinityStrategicResource order)
	{
		global::Empire empire = this.Game.Empires[order.EmpireIndex];
		StaticString descriptorName = new StaticString("Affinity" + order.ResourceName);
		IDatabase<SimulationDescriptor> simulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
		SimulationDescriptor descriptor;
		if (simulationDescriptorDatabase.TryGetValue(descriptorName, out descriptor))
		{
			empire.AddDescriptor(descriptor, false);
		}
		else
		{
			empire.SimulationObject.Tags.AddTag(descriptorName);
		}
		descriptorName = OrderSelectAffinityStrategicResource.AffinityResourceChosenDescriptor;
		if (simulationDescriptorDatabase.TryGetValue(descriptorName, out descriptor))
		{
			empire.AddDescriptor(descriptor, false);
		}
		else
		{
			empire.SimulationObject.Tags.AddTag(descriptorName);
		}
		IEventService eventService = Services.GetService<IEventService>();
		if (eventService != null)
		{
			eventService.Notify(new EventAffinityStrategicResourceChanged(empire));
		}
		yield break;
	}

	private IEnumerator SendAIAttitudeFeedbackProcessor(OrderSendAIAttitudeFeedback order)
	{
		global::Empire referenceEmpire;
		global::Empire targetEmpire;
		try
		{
			referenceEmpire = this.Game.Empires[order.ReferenceEmpireIndex];
			targetEmpire = this.Game.Empires[order.TargetEmpireIndex];
		}
		catch
		{
			Diagnostics.LogError("OrderSendAIAttitudeFeedback, Empire indexes are invalid.");
			yield break;
		}
		IEventService eventService = Services.GetService<IEventService>();
		eventService.Notify(new EventAIAttitudeFeedbackReceived(targetEmpire, referenceEmpire, order.FeedbackLocalizationString));
		yield break;
	}

	private IEnumerator SetDeploymentFinishedProcessor(OrderSetDeploymentFinished order)
	{
		if (order.EncounterGUID.IsValid)
		{
			IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
			Diagnostics.Assert(encounterRepositoryService != null);
			if (encounterRepositoryService != null)
			{
				Encounter encounter = null;
				if (encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
				{
					encounter.SetDeploymentFinished(order.ContenderGUID);
				}
			}
		}
		yield break;
	}

	private IEnumerator SetEncounterDeployementEndTimeProcessor(OrderSetEncounterDeployementEndTime order)
	{
		if (order.EncounterGUID.IsValid)
		{
			IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
			Diagnostics.Assert(encounterRepositoryService != null);
			if (encounterRepositoryService != null)
			{
				Encounter encounter = null;
				if (encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
				{
					encounter.DeployementPhaseTime = new Encounter.PhaseTime(order.EndTime, order.Duration);
					encounter.NotifyDeployementPhaseTimeChanged();
				}
			}
		}
		yield break;
	}

	private IEnumerator SetMapBoostSpawnProcessor(OrderSetMapBoostSpawn order)
	{
		IMapBoostService mapBoostService = this.Game.GetService<IMapBoostService>();
		if (mapBoostService != null)
		{
			mapBoostService.PreselectMapBoostSpawns(order.Seed);
		}
		yield break;
	}

	private IEnumerator SetOrbSpawnProcessor(OrderSetOrbSpawn order)
	{
		IOrbService orbService = this.Game.GetService<IOrbService>();
		if (orbService != null)
		{
			orbService.PreselectOrbSpawns(order.Seed);
		}
		yield break;
	}

	private IEnumerator SetWindPreferencesProcessor(OrderSetWindPreferences order)
	{
		IWeatherService weatherService = this.Game.GetService<IWeatherService>();
		if (weatherService != null)
		{
			weatherService.ControlledWind = new Wind(0, order.WindStrength);
			weatherService.OnWindChange();
		}
		yield break;
	}

	private IEnumerator SpawnKaijuProcessor(OrderSpawnKaiju order)
	{
		KaijuEmpire kaijuEmpire = this.Game.GetEmpireByIndex(order.EmpireIndex) as KaijuEmpire;
		if (kaijuEmpire == null)
		{
			Diagnostics.LogError("Order processor failed because KaijuEmpire is not valid.");
			yield break;
		}
		KaijuCouncil kaijuCouncil = kaijuEmpire.GetAgency<KaijuCouncil>();
		if (kaijuCouncil == null)
		{
			Diagnostics.LogError("Order processor failed because KaijuCouncil is not valid.");
			yield break;
		}
		kaijuCouncil.SpawnKaiju(order.KaijuPosition, order.KaijuGUID, order.GarrisonGUID, order.ArmyGUID, order.MonsterGUID, order.LicesGUIDs);
		float currentKaijusCount = kaijuEmpire.GetPropertyValue(SimulationProperties.SpawnedKaijusCounter) + 1f;
		kaijuEmpire.SetPropertyBaseValue(SimulationProperties.SpawnedKaijusCounter, currentKaijusCount);
		kaijuEmpire.Refresh(false);
		float currentKaijusGlobalCount = kaijuEmpire.GetPropertyValue(SimulationProperties.SpawnedKaijusGlobalCounter) + 1f;
		for (int empireIndex = 0; empireIndex < this.Game.Empires.Length; empireIndex++)
		{
			this.Game.Empires[empireIndex].SetPropertyBaseValue(SimulationProperties.SpawnedKaijusGlobalCounter, currentKaijusGlobalCount);
			this.Game.Empires[empireIndex].Refresh(false);
		}
		yield break;
	}

	private IEnumerator SpawnMapBoostsProcessor(OrderSpawnMapBoosts order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (this.Game.Services.GetService<IGameEntityRepositoryService>() == null)
		{
			Diagnostics.LogError("Cannot retreive the gameEntityRepositoryService.");
			yield break;
		}
		if (order.WorldPositions.Length != order.MapBoostsGUIDs.Length || order.WorldPositions.Length != order.MapBoostDefinitionNames.Length)
		{
			Diagnostics.LogError("Spawning map boosts failed. Check that world positions and GUIDs have the same lenght");
			yield break;
		}
		IMapBoostService terraformDeviceService = this.Game.Services.GetService<IMapBoostService>();
		terraformDeviceService.AddMapBoosts(order.MapBoostsGUIDs, order.MapBoostDefinitionNames, order.WorldPositions);
		yield break;
	}

	private IEnumerator SwapUnitDeploymentProcessor(OrderSwapUnitDeployment order)
	{
		if (!order.EncounterGUID.IsValid)
		{
			yield break;
		}
		IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
		Diagnostics.Assert(encounterRepositoryService != null);
		if (encounterRepositoryService == null)
		{
			yield break;
		}
		Encounter encounter = null;
		if (!encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
		{
			yield break;
		}
		encounter.SwapUnitDeployment(order.ContenderGUID, order.Unit1GUID, order.FinalUnit1Position, order.Unit2GUID, order.FinalUnit2Position);
		yield break;
	}

	private IEnumerator SwitchContendersReinforcementRankingProcessor(OrderSwitchContendersReinforcementRanking order)
	{
		if (base.Session.IsHosting)
		{
			yield break;
		}
		if (!order.EncounterGUID.IsValid)
		{
			Diagnostics.LogError("Encounter GUID is not valid.");
			yield break;
		}
		IEncounterRepositoryService encounterRepositoryService = this.Game.GetService<IEncounterRepositoryService>();
		if (encounterRepositoryService == null)
		{
			Diagnostics.LogError("Can't found encounter repository service.");
			yield break;
		}
		Encounter encounter = null;
		if (!encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
		{
			Diagnostics.LogError("Can't found encounter.");
			yield break;
		}
		Diagnostics.Assert(encounter != null);
		encounter.SwitchContendersReinforcementRanking(order.FirstContenderGUID, order.SecondContenderGUID);
		yield break;
	}

	private IEnumerator ToggleEndlessDayProcessor(OrderToggleEndlessDay order)
	{
		if (order.EndlessDay)
		{
			if (!SimulationGlobal.GlobalTagsContains(DownloadableContent8.EndlessDay.ReadOnlyTag))
			{
				SimulationGlobal.AddGlobalTag(DownloadableContent8.EndlessDay.ReadOnlyTag, false);
			}
		}
		else if (SimulationGlobal.GlobalTagsContains(DownloadableContent8.EndlessDay.ReadOnlyTag))
		{
			SimulationGlobal.RemoveGlobalTag(DownloadableContent8.EndlessDay.ReadOnlyTag, false);
		}
		DownloadableContent8.EndlessDay.Notify(order.EndlessDay);
		yield break;
	}

	private IEnumerator ToggleRuinDustDepositsProcessor(OrderToggleRuinDustDeposits order)
	{
		if (order.RuinDustDeposits)
		{
			if (!SimulationGlobal.GlobalTagsContains(SeasonManager.RuinDustDepositsTag))
			{
				SimulationGlobal.AddGlobalTag(SeasonManager.RuinDustDepositsTag, false);
			}
		}
		else if (SimulationGlobal.GlobalTagsContains(SeasonManager.RuinDustDepositsTag))
		{
			SimulationGlobal.RemoveGlobalTag(SeasonManager.RuinDustDepositsTag, false);
		}
		SeasonManager.NotifyDustDepositsToggle(order.RuinDustDeposits);
		yield break;
	}

	private IEnumerator UpdateQuestProcessor(OrderUpdateQuest order)
	{
		if (order.QuestGUID.IsValid)
		{
			IQuestRepositoryService questRepositoryService = this.Game.Services.GetService<IQuestRepositoryService>();
			Diagnostics.Assert(questRepositoryService != null);
			Quest quest;
			if (questRepositoryService != null && questRepositoryService.TryGetValue(order.QuestGUID, out quest))
			{
				int step0 = 0;
				int step = 0;
				foreach (QuestInstruction instruction in order.QuestInstructions)
				{
					instruction.Execute(quest);
					QuestInstruction_UpdateStep questInstruction_UpdateStep = instruction as QuestInstruction_UpdateStep;
					if (questInstruction_UpdateStep != null)
					{
						switch (questInstruction_UpdateStep.State)
						{
						case QuestState.InProgress:
							step = questInstruction_UpdateStep.StepNumber;
							break;
						case QuestState.Completed:
						case QuestState.Failed:
							step0 = questInstruction_UpdateStep.StepNumber;
							break;
						}
					}
				}
				IEventService eventService = Services.GetService<IEventService>();
				if (eventService != null)
				{
					for (int empireIndex = 0; empireIndex < this.Game.Empires.Length; empireIndex++)
					{
						global::Empire empire = this.Game.Empires[empireIndex];
						if (!(empire is MajorEmpire))
						{
							break;
						}
						if ((quest.EmpireBits & 1 << empireIndex) != 0)
						{
							if (step > step0)
							{
								eventService.Notify(new EventQuestStepChanged(empire, quest, step0, step));
							}
							eventService.Notify(new EventQuestUpdated(empire, quest));
						}
					}
				}
				switch (quest.QuestState)
				{
				case QuestState.Completed:
				case QuestState.Failed:
				{
					IQuestManagementService questManagementService = this.Game.Services.GetService<IQuestManagementService>();
					if (questManagementService != null)
					{
						questManagementService.CheckForQuestCompletion(order.QuestGUID);
					}
					break;
				}
				}
			}
		}
		else
		{
			Diagnostics.LogError("The quest entity guid should be valid.");
		}
		yield break;
	}

	private IEnumerator UpdateWinterImmunityBidsProcessor(OrderUpdateWinterImmunityBids order)
	{
		ISeasonService seasonService = this.Game.GetService<ISeasonService>();
		if (seasonService != null)
		{
			seasonService.UpdateWinterImmunityBids(order.StartWinter);
		}
		yield break;
	}

	private IEnumerator VoteForSeasonEffectProcessor(OrderVoteForSeasonEffect order)
	{
		ISeasonService seasonService = this.Game.GetService<ISeasonService>();
		List<SeasonEffect> preselectedSeasonEffect = seasonService.GetCandidateEffectsForSeasonType(Season.ReadOnlyWinter);
		SeasonEffect targetedSeasonEffect = preselectedSeasonEffect.Find((SeasonEffect seasonEffect) => seasonEffect.SeasonEffectDefinition.Name == order.SeasonEffectName);
		DepartmentOfTheTreasury departmentOfTheTreasury = this.Game.Empires[order.EmpireIndex].GetAgency<DepartmentOfTheTreasury>();
		if (departmentOfTheTreasury == null || !departmentOfTheTreasury.TryTransferResources(this.Game.Empires[order.EmpireIndex].SimulationObject, DepartmentOfTheTreasury.Resources.Orb, order.VoteCost))
		{
			yield break;
		}
		targetedSeasonEffect.VoteCount += order.VoteCount;
		targetedSeasonEffect.DisplayedVoteCountByEmpire[order.EmpireIndex] += order.VoteCount;
		yield break;
	}

	private IEnumerator WinterImmunityBidProcessor(OrderWinterImmunityBid order)
	{
		ISeasonService seasonService = this.Game.GetService<ISeasonService>();
		if (seasonService != null)
		{
			DepartmentOfTheTreasury departmentOfTreasury = this.Game.Empires[order.EmpireIndex].GetAgency<DepartmentOfTheTreasury>();
			if (departmentOfTreasury == null)
			{
				Diagnostics.LogError("Fail to get department of treasury of Empire " + order.EmpireIndex);
				yield break;
			}
			int previousBid = seasonService.GetImmunityBid(order.EmpireIndex);
			if (previousBid > 0 && !departmentOfTreasury.TryTransferResources(this.Game.Empires[order.EmpireIndex], DepartmentOfTheTreasury.Resources.Orb, (float)previousBid))
			{
				Diagnostics.LogError("Fail refund previous bid of Empire " + order.EmpireIndex);
				yield break;
			}
			if (!departmentOfTreasury.TryTransferResources(this.Game.Empires[order.EmpireIndex], DepartmentOfTheTreasury.Resources.Orb, (float)order.Value * -1f))
			{
				Diagnostics.LogError("Fail immunity payment of Empire " + order.EmpireIndex);
				yield break;
			}
			seasonService.WinterImmunityBid(order.EmpireIndex, order.Value);
		}
		yield break;
	}

	~GameClient()
	{
		this.Dispose(false);
		UnityEngine.Debug.Log("Game client has been deleted.");
	}

	public GameClientConnectionState GameClientConnectionState
	{
		get
		{
			return this.connectionState;
		}
		protected set
		{
			if (this.connectionState != value)
			{
				this.connectionState = value;
				this.OnGameClientConnectionStateChange(new GameClientConnectionStateChangeEventArgs(this.connectionState));
			}
		}
	}

	public Steamworks.SteamID SteamIDServer { get; private set; }

	public void ClearOrderHistory()
	{
		if (this.ordersHistory != null)
		{
			this.ordersHistory.Clear();
		}
	}

	public bool HasPendingOrders
	{
		get
		{
			return this.orderQueue.Count > 0;
		}
	}

	public bool PendingConnection { get; set; }

	public void OnSteamGameServerReady()
	{
		this.steamNetworkingProxy = new SteamNetworkingProxy(Steamworks.SteamAPI.SteamNetworking, false);
		base.MessageBox = new MessageBox(this.steamNetworkingProxy);
		this.steamClientService = Services.GetService<ISteamClientService>();
		this.steamClientService.ClientP2PSessionRequest += this.ISteamClientService_P2PSessionRequest;
		this.steamClientService.ClientP2PSessionConnectFail += this.ISteamClientService_P2PSessionConnectFail;
	}

	public override void SendMessageToServer(ref Message message)
	{
		if (message.ID == 2101)
		{
			GameClientPostOrderMessage gameClientPostOrderMessage = message as GameClientPostOrderMessage;
			Diagnostics.Assert(gameClientPostOrderMessage != null);
			switch (base.Session.SessionMode)
			{
			case SessionMode.Single:
			case SessionMode.Private:
			case SessionMode.Protected:
			case SessionMode.Public:
			{
				GameClientState gameClientState = base.FiniteStateMachine.CurrentState as GameClientState;
				if (gameClientState != null && !gameClientState.CanSendOrder)
				{
					Diagnostics.LogWarning("[Net,Client] Dropped order {0} ({1})", new object[]
					{
						gameClientPostOrderMessage.Order.GetType(),
						gameClientState.GetType()
					});
					base.OnTicketRaised(gameClientPostOrderMessage.Order.TicketNumber, PostOrderResponse.Blocked, gameClientPostOrderMessage.Order);
					return;
				}
				break;
			}
			default:
				throw new ArgumentOutOfRangeException();
			}
		}
		if (this.SteamIDServer != null && base.MessageBox != null)
		{
			base.MessageBox.SendMessage(ref message, new Steamworks.SteamID[]
			{
				this.SteamIDServer
			});
		}
	}

	public void Disconnect(GameDisconnectionReason reason = GameDisconnectionReason.Default, int errorCode = 0)
	{
		if (reason == GameDisconnectionReason.HostLeft && base.Session.IsHosting)
		{
			base.Session.SetLobbyData("_GameIsMigrating", true, true);
		}
		if (base.Session.SessionMode == SessionMode.Single)
		{
			this.GameClientConnectionState = GameClientConnectionState.DisconnectedFromServer;
			return;
		}
		if (this.GameClientConnectionState == GameClientConnectionState.DisconnectedFromServer && reason != GameDisconnectionReason.TimedOut)
		{
			return;
		}
		Diagnostics.Log("[GameClient][Net] Disconnecting... reason={0}", new object[]
		{
			reason
		});
		if ((this.GameClientConnectionState == GameClientConnectionState.AuthenticatingToServer || this.GameClientConnectionState == GameClientConnectionState.AuthenticatedToServer) && this.authTicketHandle != 0u)
		{
			Diagnostics.Log("[GameClient][Net] Disconnecting, cancel auth ticket {0}", new object[]
			{
				this.authTicketHandle
			});
			Steamworks.SteamUser steamUser = Steamworks.SteamAPI.SteamUser;
			steamUser.CancelAuthTicket(this.authTicketHandle);
			this.authTicketHandle = 0u;
			this.GameClientConnectionState = GameClientConnectionState.ConnectedToServer;
		}
		if (this.GameClientConnectionState == GameClientConnectionState.ConnectedToServer || this.GameClientConnectionState == GameClientConnectionState.ConnectingToServer || this.GameClientConnectionState == GameClientConnectionState.AuthenticationHasFailed)
		{
			if (this.SteamIDServer != null && this.SteamIDServer.IsValid)
			{
				Diagnostics.Log("[GameClient][Net] Closing P2PSession with server.");
				if (reason == GameDisconnectionReason.ClientLeft)
				{
					Message message = new GameClientLeaveMessage();
					base.MessageBox.SendMessage(ref message, new Steamworks.SteamID[0]);
				}
				this.steamNetworkingProxy.CloseP2PSessionWithUser(this.SteamIDServer);
			}
			this.GameClientConnectionState = GameClientConnectionState.DisconnectedFromServer;
		}
		if (this.SteamIDServer != null)
		{
			Amplitude.Unity.Session.Session.IgnoreP2PSessionConnectFail = this.SteamIDServer;
		}
		Diagnostics.Log("[GameClient][Net] Disconnected.");
		this.PostStateChange(typeof(GameClientState_DisconnectedFromServer), new object[]
		{
			this.GameClientConnectionState,
			reason,
			errorCode
		});
	}

	public override void UpdateMessageBoxAndProcessOrders()
	{
		base.UpdateMessageBoxAndProcessOrders();
		int num = 10;
		while (this.HasPendingOrders && num-- > 0)
		{
			this.ProcessOrders();
		}
	}

	internal void Authentify()
	{
		switch (this.GameClientConnectionState)
		{
		case GameClientConnectionState.ConnectedToServer:
		{
			byte[] array = new byte[1024];
			uint num = 0u;
			Steamworks.SteamUser steamUser = Steamworks.SteamAPI.SteamUser;
			this.authTicketHandle = steamUser.GetAuthSessionTicket(ref array, (uint)array.Length, out num);
			Diagnostics.Log("[Net][Auth] TicketHandle={2}, TicketLength = {0}, Ticket={1}", new object[]
			{
				num,
				GameClientState_Authentication.TicketToString(array, num),
				this.authTicketHandle
			});
			Message message = new GameClientAuthTicketMessage(array, num);
			this.SendMessageToServer(ref message);
			this.GameClientConnectionState = GameClientConnectionState.AuthenticatingToServer;
			break;
		}
		case GameClientConnectionState.ConnectingToServer:
		case GameClientConnectionState.ConnectionToServerHasFailed:
		case GameClientConnectionState.ConnectionToServerHasTimedOut:
		case GameClientConnectionState.AuthenticatingToServer:
		case GameClientConnectionState.AuthenticatedToServer:
		case GameClientConnectionState.AuthenticationHasFailed:
		case GameClientConnectionState.DisconnectedFromServer:
			Diagnostics.LogError("[Net][Auth] Wrong state to authentify.");
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	internal void Connect()
	{
		this.SteamIDServer = Steamworks.SteamID.Zero;
		if (base.Session.SteamIDServer != null && base.Session.SteamIDServer.IsValid)
		{
			this.SteamIDServer = base.Session.SteamIDServer;
		}
		switch (this.GameClientConnectionState)
		{
		case GameClientConnectionState.ConnectedToServer:
			return;
		case GameClientConnectionState.ConnectingToServer:
		{
			Message message = new GameClientInitiateConnectionMessage();
			this.SendMessageToServer(ref message);
			return;
		}
		case GameClientConnectionState.ConnectionToServerHasFailed:
		case GameClientConnectionState.ConnectionToServerHasTimedOut:
		case GameClientConnectionState.DisconnectedFromServer:
		case GameClientConnectionState.ConnectingToServerRetry:
		{
			if (base.Session.IsHosting)
			{
				this.SteamIDServer = this.SteamIDUser;
				GameServer gameServer = base.Session.GameServer as GameServer;
				if (gameServer == null)
				{
					Diagnostics.LogError("The game server is null.");
					this.GameClientConnectionState = GameClientConnectionState.ConnectionToServerHasFailed;
					return;
				}
				if (gameServer.MessageBox == null)
				{
					this.GameClientConnectionState = GameClientConnectionState.ConnectingToServerRetry;
					return;
				}
				base.MessageBox.Connect(this.SteamIDUser, gameServer.MessageBox.MessagePipe);
			}
			Message message2 = new GameClientInitiateConnectionMessage();
			this.SendMessageToServer(ref message2);
			this.GameClientConnectionState = GameClientConnectionState.ConnectingToServer;
			return;
		}
		}
		throw new NotImplementedException();
	}

	protected override void Dispose(bool disposing)
	{
		if (!this.disposed)
		{
			if (disposing)
			{
				this.Disconnect(GameDisconnectionReason.ClientLeft, 0);
				if (this.steamClientService != null)
				{
					this.steamClientService.ClientP2PSessionRequest -= this.ISteamClientService_P2PSessionRequest;
					this.steamClientService.ClientP2PSessionConnectFail -= this.ISteamClientService_P2PSessionConnectFail;
				}
				if (Steamworks.SteamAPI.IsSteamRunning)
				{
				}
				IGameService service = Services.GetService<IGameService>();
				if (service != null)
				{
					service.ReleaseGame();
				}
				if (this.endTurnService != null)
				{
					this.endTurnService.EndTurnValidated -= this.EndTurnService_EndTurnValidated;
					this.endTurnService.UnregisterCanExecuteValidator(new Func<bool>(this.CanExecuteEndTurn));
					this.endTurnService.UnregisterValidator(new Func<bool, bool>(this.ValidateEndTurn));
					this.endTurnService = null;
				}
				if (base.Session != null)
				{
					base.Session.LobbyDataChange -= this.Session_LobbyDatachange;
				}
				this.orderQueue.Clear();
				this.ordersHistory.Clear();
			}
			this.disposed = true;
		}
		base.Dispose(disposing);
	}

	protected virtual void OnGameClientConnectionStateChange(GameClientConnectionStateChangeEventArgs e)
	{
		if (this.GameClientConnectionStateChange != null)
		{
			this.GameClientConnectionStateChange(this, e);
		}
	}

	protected override void ProcessMessage(ref IMessage message, ref Steamworks.SteamID steamIDRemote)
	{
		if (steamIDRemote != this.SteamIDServer && steamIDRemote != this.SteamIDUser)
		{
			Diagnostics.LogWarning("Skipping processing of message from unknown origin (message id: {0}, remote steam id:{1}).", new object[]
			{
				message.ID,
				steamIDRemote.ToString()
			});
			return;
		}
		short id = message.ID;
		switch (id)
		{
		case 1101:
		{
			if (this.PendingConnection)
			{
				Diagnostics.Log("[Net][Gameclient] PendingConnection. Dropping message {0}", new object[]
				{
					(GameServerMessageID)message.ID
				});
				return;
			}
			Diagnostics.Assert(message is GameServerPostOrderMessage, "Invalid message type. {0}", new object[]
			{
				message.GetType()
			});
			GameServerPostOrderMessage gameServerPostOrderMessage = message as GameServerPostOrderMessage;
			Amplitude.Unity.Game.Orders.Order order = gameServerPostOrderMessage.Order;
			this.orderQueue.Enqueue(order);
			break;
		}
		case 1102:
		{
			if (this.PendingConnection)
			{
				Diagnostics.Log("[Net][Gameclient] PendingConnection. Dropping message {0}", new object[]
				{
					(GameServerMessageID)message.ID
				});
				return;
			}
			Diagnostics.Assert(message is GameServerPostOrderResponseMessage, "Invalid message type");
			GameServerPostOrderResponseMessage gameServerPostOrderResponseMessage = message as GameServerPostOrderResponseMessage;
			base.OnTicketRaised(gameServerPostOrderResponseMessage.Order.TicketNumber, gameServerPostOrderResponseMessage.Response, gameServerPostOrderResponseMessage.Order);
			break;
		}
		case 1103:
		{
			GameServerPostStateChangeMessage gameServerPostStateChangeMessage = message as GameServerPostStateChangeMessage;
			try
			{
				Type type = Type.GetType(gameServerPostStateChangeMessage.AssemblyQualifiedName);
				if (type.Name != base.FiniteStateMachine.CurrentState.GetType().Name)
				{
					this.PostStateChange(type, new object[0]);
				}
			}
			catch
			{
				throw;
			}
			break;
		}
		case 1104:
		{
			GameServerPlayerUpdateMessage gameServerPlayerUpdateMessage = message as GameServerPlayerUpdateMessage;
			if (gameServerPlayerUpdateMessage != null)
			{
				int num = -1;
				if (this.Game != null && this.Game.HasBeenInitialized)
				{
					this.ProcessPlayerUpdateMessage(gameServerPlayerUpdateMessage);
					IPlayerRepositoryService service = this.Game.GetService<IPlayerRepositoryService>();
					Diagnostics.Assert(service != null);
					Player playerBySteamID = service.GetPlayerBySteamID(gameServerPlayerUpdateMessage.SteamIDUser);
					num = ((playerBySteamID == null) ? -1 : playerBySteamID.Empire.Index);
				}
				if (num == -1)
				{
					int num2 = 0;
					for (;;)
					{
						string x = string.Format("Empire{0}", num2);
						string lobbyData = base.Session.GetLobbyData<string>(x, null);
						if (string.IsNullOrEmpty(lobbyData))
						{
							break;
						}
						if (lobbyData.Contains(gameServerPlayerUpdateMessage.SteamIDUser.ToString()))
						{
							goto Block_25;
						}
						num2++;
					}
					goto IL_41C;
					Block_25:
					num = num2;
				}
				IL_41C:
				Diagnostics.MultiplayerProgress.SetProgress(gameServerPlayerUpdateMessage.SteamIDUser, num, gameServerPlayerUpdateMessage.AssemblyQualifiedName.TypeNameFromAssemblyQualifiedName());
			}
			break;
		}
		case 1105:
		{
			GameServerEndTurnTimerMessage gameServerEndTurnTimerMessage = message as GameServerEndTurnTimerMessage;
			if (gameServerEndTurnTimerMessage != null)
			{
				IEndTurnService service2 = Services.GetService<IEndTurnService>();
				Diagnostics.Assert(service2 != null);
				service2.ChangeEndTurnTime(gameServerEndTurnTimerMessage.EndTurnTime, gameServerEndTurnTimerMessage.Duration);
			}
			break;
		}
		default:
			if (id != 1001)
			{
				if (id != 1002)
				{
					if (id != 1990)
					{
						if (id != 1991)
						{
							if (id != 1010)
							{
								if (id != 1201)
								{
									if (id != 1301)
									{
										if (id == 1901)
										{
											GameServerDownloadDumpRequestMessage gameServerDownloadDumpRequestMessage = message as GameServerDownloadDumpRequestMessage;
											if (gameServerDownloadDumpRequestMessage != null)
											{
												this.SendDumpFileToServer(gameServerDownloadDumpRequestMessage.Turn, gameServerDownloadDumpRequestMessage.Tag);
											}
										}
									}
									else
									{
										if (base.Session.IsHosting)
										{
											return;
										}
										IGameSerializationService service3 = Services.GetService<IGameSerializationService>();
										if (service3 != null)
										{
											Diagnostics.Log("Download game message has been acknowledged by server.");
											GameServerDownloadGameResponseMessage gameServerDownloadGameResponseMessage = message as GameServerDownloadGameResponseMessage;
											if (gameServerDownloadGameResponseMessage != null && gameServerDownloadGameResponseMessage.Binary != null)
											{
												Diagnostics.Log("Download game message has been responded to by server.");
												string text = string.Empty;
												string str = ".sav";
												string text2 = string.Empty;
												ILocalizationService service4 = Services.GetService<ILocalizationService>();
												switch (gameServerDownloadGameResponseMessage.RequestedSaveType)
												{
												case SaveType.InitialGameState:
													text = Amplitude.Unity.Framework.Application.TempDirectory;
													text2 = "%DownloadSaveFileName";
													break;
												case SaveType.LastAutoSave:
												{
													text = global::Application.GameSaveDirectory;
													text2 = "AutoSave";
													int num3 = service3.ProcessIncremental(text, text2);
													text2 = string.Format("{0} {1}", text2, num3);
													break;
												}
												case SaveType.OnUserRequest:
													text = global::Application.GameSaveDirectory;
													text2 = gameServerDownloadGameResponseMessage.Title;
													break;
												case SaveType.Victory:
													text = global::Application.GameSaveDirectory;
													text2 = gameServerDownloadGameResponseMessage.Title;
													if (service4 != null && text2.StartsWith("%"))
													{
														text2 = service4.Localize(text2, "Game ended at turn $Turn");
													}
													text2 = text2.Replace("$Turn", this.Game.Turn.ToString());
													break;
												default:
													throw new ArgumentOutOfRangeException();
												}
												if (service4 != null && text2.StartsWith("%"))
												{
													text2 = service4.Localize(text2, "QuickSave");
												}
												string text3 = System.IO.Path.Combine(text, text2) + str;
												File.WriteAllBytes(text3, gameServerDownloadGameResponseMessage.Binary);
												Diagnostics.Log("Game saved @ '{0}'", new object[]
												{
													text3
												});
												GameSaveDescriptor gameSaveDescriptor;
												service3.TryExtractGameSaveDescriptorFromFile(text3, out gameSaveDescriptor, true);
											}
										}
									}
								}
								else
								{
									if (this.PendingConnection)
									{
										Diagnostics.Log("[Net][Gameclient] PendingConnection. Dropping message {0}", new object[]
										{
											(GameServerMessageID)message.ID
										});
										return;
									}
									GameServerChatMessage gameServerChatMessage = message as GameServerChatMessage;
									if (gameServerChatMessage != null && this.OnChatMessageReceived != null)
									{
										this.OnChatMessageReceived(this, gameServerChatMessage.Message);
									}
								}
							}
							else
							{
								GameServerAuthTicketResponseMessage gameServerAuthTicketResponseMessage = message as GameServerAuthTicketResponseMessage;
								Diagnostics.Assert(gameServerAuthTicketResponseMessage != null, "Invalid message type. Expected GameServerAuthTicketResponseMessage.");
								switch (gameServerAuthTicketResponseMessage.Response)
								{
								case Steamworks.EAuthSessionResponse.k_EAuthSessionResponseOK:
									Diagnostics.Log("[Net] [GameClient,Auth] Received an auth ticket response from server, we're good to go.");
									this.GameClientConnectionState = GameClientConnectionState.AuthenticatedToServer;
									break;
								case Steamworks.EAuthSessionResponse.k_EAuthSessionResponseUserNotConnectedToSteam:
								case Steamworks.EAuthSessionResponse.k_EAuthSessionResponseNoLicenseOrExpired:
								case Steamworks.EAuthSessionResponse.k_EAuthSessionResponseVACBanned:
								case Steamworks.EAuthSessionResponse.k_EAuthSessionResponseLoggedInElseWhere:
								case Steamworks.EAuthSessionResponse.k_EAuthSessionResponseVACCheckTimedOut:
								case Steamworks.EAuthSessionResponse.k_EAuthSessionResponseAuthTicketCanceled:
								case Steamworks.EAuthSessionResponse.k_EAuthSessionResponseAuthTicketInvalidAlreadyUsed:
								case Steamworks.EAuthSessionResponse.k_EAuthSessionResponseAuthTicketInvalid:
									Diagnostics.LogWarning("[Net] [GameClient,Auth] Received an auth ticket response from server, it's failed (error: 0x{0:x4}).", new object[]
									{
										(int)gameServerAuthTicketResponseMessage.Response
									});
									this.Disconnect(GameDisconnectionReason.AuthFailure, 0);
									break;
								default:
									throw new ArgumentOutOfRangeException();
								}
							}
						}
						else
						{
							GameServerClearOrderHistoryMessage gameServerClearOrderHistoryMessage = message as GameServerClearOrderHistoryMessage;
							if (gameServerClearOrderHistoryMessage != null)
							{
								this.ClearOrderHistory();
							}
						}
					}
					else
					{
						GameServerPingMessage gameServerPingMessage = message as GameServerPingMessage;
						if (gameServerPingMessage != null)
						{
							IPingServiceManager service5 = this.Game.GetService<IPingServiceManager>();
							Diagnostics.Assert(service5 != null);
							service5.ProcessPingResponse(gameServerPingMessage);
							IPlayerRepositoryService service6 = this.Game.GetService<IPlayerRepositoryService>();
							Diagnostics.Assert(service6 != null);
							for (int i = 0; i < gameServerPingMessage.PlayersSteamIDs.Count; i++)
							{
								Player playerBySteamID2 = service6.GetPlayerBySteamID(gameServerPingMessage.PlayersSteamIDs[i]);
								if (playerBySteamID2 != null)
								{
									playerBySteamID2.Latency = gameServerPingMessage.PlayersLatencies[i];
								}
							}
						}
					}
				}
			}
			else
			{
				Diagnostics.Log("[Net] [GameClient] Got initiate connection response from server.");
				this.GameClientConnectionState = GameClientConnectionState.ConnectedToServer;
				GameServerInitiateConnectionResponseMessage gameServerInitiateConnectionResponseMessage = message as GameServerInitiateConnectionResponseMessage;
				if (gameServerInitiateConnectionResponseMessage != null)
				{
					this.PendingConnection = gameServerInitiateConnectionResponseMessage.PendingConnection;
					if (this.PendingConnection)
					{
						Diagnostics.Log("[Net][Gameclient] Flagged as Pendingconnection.");
					}
				}
			}
			break;
		}
	}

	protected override void ProcessOrders()
	{
		if (this.orderProcessing == null)
		{
			if (this.orderQueue.Count > 0 && ((base.FiniteStateMachine.CurrentState as GameClientState).CanProcessOrder || base.Session.GameServer != null))
			{
				Amplitude.Unity.Game.Orders.Order order = this.orderQueue.Dequeue();
				this.orderProcessing = Amplitude.Coroutine.StartCoroutine(this.ProcessOrder(order), null);
			}
		}
		else
		{
			this.orderProcessing.Run();
		}
		if (this.orderProcessing != null)
		{
			if (this.orderProcessing.LastException != null)
			{
				Diagnostics.LogError("The order processing has raised an exception. Exception: {0}.", new object[]
				{
					this.orderProcessing.LastException.ToString()
				});
				this.orderProcessing = null;
			}
			else if (this.orderProcessing.IsFinished)
			{
				this.orderProcessing = null;
			}
		}
	}

	protected override void Session_SessionChange(object sender, SessionChangeEventArgs e)
	{
		base.Session_SessionChange(sender, e);
		SessionChangeAction action = e.Action;
		if (action == SessionChangeAction.OwnerChanged)
		{
			Diagnostics.Log("[Net][GameClient] Owner changed, server is dead in the water, disconnecting...");
			if (base.Session.GetLobbyData<bool>("_GameHasEnded", false))
			{
				Diagnostics.Log("[Net][GameClient] ... but the game has ended.");
				this.Disconnect(GameDisconnectionReason.GameHasEnded, 0);
			}
			else
			{
				this.Disconnect(GameDisconnectionReason.HostLeft, 256);
			}
		}
	}

	private void EndTurnService_EndTurnValidated(object sender, EventArgs e)
	{
		this.PostStateChange(typeof(GameClientState_Turn_Finished), new object[0]);
	}

	private bool CanExecuteEndTurn()
	{
		return this.Game != null && base.FiniteStateMachine.CurrentState != null && base.FiniteStateMachine.CurrentState is GameClientState_Turn_Main;
	}

	private IEnumerator ProcessOrder(Amplitude.Unity.Game.Orders.Order order)
	{
		if (base.Session.SessionMode != SessionMode.Single)
		{
			this.ordersHistory.Enqueue(order);
		}
		OrderProcessorInfo orderProcessorInfo;
		if (order is global::Order)
		{
			global::Empire empire = this.Game.Empires[((global::Order)order).EmpireIndex];
			yield return empire.ProcessOrder(order);
		}
		else if (this.orderProcessorInfoByType.TryGetValue(order.GetType(), out orderProcessorInfo))
		{
			yield return orderProcessorInfo.Process(order);
		}
		Message aknowledge = new GameClientPostOrderResponseMessage(order.Serial);
		this.SendMessageToServer(ref aknowledge);
		base.OnTicketRaised(order.TicketNumber, PostOrderResponse.Processed, order);
		yield break;
	}

	private void ISteamClientService_P2PSessionRequest(object sender, P2PSessionRequestEventArgs e)
	{
		if (base.FiniteStateMachine != null && base.FiniteStateMachine.CurrentState.GetType() != typeof(GameClientState_DisconnectedFromServer))
		{
			Diagnostics.Log("[Net] ISteamClientService_P2PSessionRequest SteamIDRemote=0x{0:x8}", new object[]
			{
				e.Message.m_steamIDRemote
			});
			Steamworks.SteamAPI.SteamNetworking.AcceptP2PSessionWithUser(e.Message.m_steamIDRemote);
		}
	}

	private void ISteamClientService_P2PSessionConnectFail(object sender, P2PSessionConnectFailEventArgs e)
	{
		switch (this.GameClientConnectionState)
		{
		case GameClientConnectionState.ConnectedToServer:
		case GameClientConnectionState.AuthenticatingToServer:
		case GameClientConnectionState.AuthenticatedToServer:
			Diagnostics.Log("[Net] ISteamClientService_P2PSessionConnectFail SteamIDRemote=0x{0:x8} error=0x{1:x4}", new object[]
			{
				e.Message.m_steamIDRemote,
				e.Message.m_eP2PSessionError
			});
			if (e.Message.m_steamIDRemote == this.SteamIDServer)
			{
				this.Disconnect(GameDisconnectionReason.P2PFailure, (int)e.Message.m_eP2PSessionError);
			}
			break;
		case GameClientConnectionState.ConnectingToServer:
			Diagnostics.Log("[Net] ISteamClientService_P2PSessionConnectFail SteamIDRemote=0x{0:x8} error=0x{1:x4}", new object[]
			{
				e.Message.m_steamIDRemote,
				e.Message.m_eP2PSessionError
			});
			if (e.Message.m_steamIDRemote == this.SteamIDServer)
			{
				Diagnostics.LogWarning("[Net] Not disconnecting just yet, giving it another chance...");
			}
			break;
		case GameClientConnectionState.ConnectionToServerHasFailed:
		case GameClientConnectionState.ConnectionToServerHasTimedOut:
		case GameClientConnectionState.AuthenticationHasFailed:
		case GameClientConnectionState.DisconnectedFromServer:
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	private void SendDumpFileToServer(int turn, string tag)
	{
		IGameDiagnosticsService service = Services.GetService<IGameDiagnosticsService>();
		Diagnostics.Assert(service != null);
		string text = service.RetrieveDumpPath(string.Format("{0:D3}{1}", this.Game.Turn, "TurnEnded"));
		if (string.IsNullOrEmpty(text))
		{
			throw new FileNotFoundException();
		}
		try
		{
			string fileName = System.IO.Path.GetFileName(text);
			byte[] array = File.ReadAllBytes(text);
			MemoryStream memoryStream = new MemoryStream();
			BZip2OutputStream bzip2OutputStream = new BZip2OutputStream(memoryStream);
			bzip2OutputStream.Write(array, 0, array.Length);
			bzip2OutputStream.Flush();
			bzip2OutputStream.Close();
			Diagnostics.Log("[Dump][BZip2] length={0} compressedLength={1}", new object[]
			{
				array.Length,
				memoryStream.ToArray().Length
			});
			Message message = new GameClientDownloadDumpResponseMessage(fileName, (long)array.Length, memoryStream.ToArray());
			memoryStream.Close();
			this.SendMessageToServer(ref message);
		}
		catch (Exception ex)
		{
			Diagnostics.LogError("Exception raised while sending dump to server: {0}\n{1}", new object[]
			{
				ex.Message,
				ex.StackTrace
			});
		}
	}

	private void ProcessPlayerUpdateMessage(GameServerPlayerUpdateMessage message)
	{
		IPlayerRepositoryService service = this.Game.GetService<IPlayerRepositoryService>();
		Diagnostics.Assert(service != null);
		switch (message.Action)
		{
		case GameServerPlayerUpdateMessage.PlayerAction.None:
			Diagnostics.LogError("[GameClient] ProcessPlayerUpdateMessage: PlayerAction is unset.");
			break;
		case GameServerPlayerUpdateMessage.PlayerAction.StateUpdate:
		{
			Player playerBySteamID = service.GetPlayerBySteamID(message.SteamIDUser);
			if (playerBySteamID != null)
			{
				playerBySteamID.GameClientState = message.AssemblyQualifiedName.Split(new char[]
				{
					','
				})[0];
				if (!string.IsNullOrEmpty(message.Name))
				{
					playerBySteamID.LocalizedName = message.Name;
				}
				service.NotifyChange(PlayerRepositoryChangeAction.Change, playerBySteamID);
			}
			break;
		}
		case GameServerPlayerUpdateMessage.PlayerAction.Joined:
		{
			if (message.SteamIDUser == this.SteamIDUser)
			{
				return;
			}
			if (service.GetPlayerBySteamID(message.SteamIDUser) != null)
			{
				Diagnostics.LogError("[Net] Player is already registered in this game.");
				return;
			}
			MajorEmpire majorEmpire = this.Game.Empires[message.EmpireIndex] as MajorEmpire;
			string localizedName = "%DefaultPlayerName";
			if (!string.IsNullOrEmpty(message.Name))
			{
				localizedName = message.Name;
			}
			else if (Steamworks.SteamAPI.IsSteamRunning)
			{
				localizedName = Steamworks.SteamAPI.SteamFriends.GetFriendPersonaName(message.SteamIDUser);
			}
			if (majorEmpire.Players[0].Type == PlayerType.AI)
			{
				majorEmpire.UnbindPlayer(majorEmpire.Players[0]);
			}
			Player player = new Player(majorEmpire)
			{
				Type = PlayerType.Human,
				Location = PlayerLocation.Remote,
				LocalizedName = localizedName,
				SteamID = message.SteamIDUser
			};
			majorEmpire.BindPlayer(player);
			break;
		}
		case GameServerPlayerUpdateMessage.PlayerAction.Left:
		{
			Player playerBySteamID2 = service.GetPlayerBySteamID(message.SteamIDUser);
			if (playerBySteamID2 != null)
			{
				MajorEmpire majorEmpire2 = playerBySteamID2.Empire as MajorEmpire;
				if (majorEmpire2 != null)
				{
					majorEmpire2.UnbindPlayer(playerBySteamID2);
				}
				if (majorEmpire2.Players.Count == 0)
				{
					majorEmpire2.IsControlledByAI = true;
					majorEmpire2.BindPlayer(new Player(majorEmpire2)
					{
						Type = PlayerType.AI,
						Location = ((base.Session.GameServer == null) ? PlayerLocation.Remote : PlayerLocation.Local),
						LocalizedName = MajorEmpire.GenerateBasicAIName(majorEmpire2.Index)
					});
				}
			}
			break;
		}
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	private bool ValidateEndTurn(bool force)
	{
		NotificationListPanel guiPanel = Services.GetService<IGuiService>().GetGuiPanel<NotificationListPanel>();
		List<NotificationItem> children = guiPanel.NotificationTable.GetChildren<NotificationItem>(true);
		for (int i = 0; i < children.Count; i++)
		{
			NotificationItem notificationItem = children[i];
			if (notificationItem.GuiNotification != null && !notificationItem.GuiNotification.CanProcessEndTurn())
			{
				notificationItem.Icon.AgeTransform.StartAllModifiers(true, false);
				Services.GetService<IAudioEventService>().Play2DEvent("Gui/Jingles/MissingResearch");
				return false;
			}
		}
		return true;
	}

	[Conditional("UNITY_EDITOR")]
	private void NotifyProcessingOrder(Amplitude.Unity.Game.Orders.Order order)
	{
		if (this.OnProcessingOrder != null)
		{
			int? empireIndex = null;
			if (order is global::Order)
			{
				empireIndex = new int?((order as global::Order).EmpireIndex);
			}
			this.OnProcessingOrder(this, order, empireIndex);
		}
	}

	[Conditional("UNITY_EDITOR")]
	private void NotifySendingOrder(Amplitude.Unity.Game.Orders.Order order)
	{
		if (this.OnSendingMessageToServer != null)
		{
			int? empireIndex = null;
			if (order is global::Order)
			{
				empireIndex = new int?((order as global::Order).EmpireIndex);
			}
			this.OnSendingMessageToServer(this, order, empireIndex);
		}
	}

	private void Session_LobbyDatachange(object sender, LobbyDataChangeEventArgs e)
	{
		if (e.Key == "_GameSyncState" || e.Key == "_PlayerIsTransition")
		{
			SynchronizationState synchronizationState = (SynchronizationState)((int)Enum.Parse(typeof(SynchronizationState), base.Session.GetLobbyData<string>("_GameSyncState", SynchronizationState.Unset.ToString()), true));
			bool lobbyData = base.Session.GetLobbyData<bool>("_PlayerIsTransition", false);
			if (synchronizationState == SynchronizationState.Rematch)
			{
				this.Disconnect(GameDisconnectionReason.Desync, 0);
			}
			Diagnostics.Log("[Dump] SynchronizationState = {0}", new object[]
			{
				synchronizationState
			});
			Diagnostics.Log("[Trans] {0}", new object[]
			{
				lobbyData
			});
			ISynchronizationService service = Services.GetService<ISynchronizationService>();
			Diagnostics.Assert(service != null);
			service.NotifySynchronizationStateChanged(this, new SynchronizationStateChangedArgs(synchronizationState, 0, new bool?(lobbyData)));
		}
	}

	public global::Empire GetClientEmpire()
	{
		string x = "player#" + base.Session.SteamIDUser.AccountID;
		global::PlayerController playerControllerById = this.GetPlayerControllerById(x);
		if (playerControllerById != null && playerControllerById.Empire != null)
		{
			return playerControllerById.Empire as global::Empire;
		}
		return null;
	}

	private const int MaxOrderProcessingTicksPerFrame = 10;

	private bool disposed;

	private GameClientConnectionState connectionState = GameClientConnectionState.DisconnectedFromServer;

	private uint authTicketHandle;

	private Queue<Amplitude.Unity.Game.Orders.Order> orderQueue = new Queue<Amplitude.Unity.Game.Orders.Order>();

	private Amplitude.Coroutine orderProcessing;

	private IEndTurnService endTurnService;

	private ISteamClientService steamClientService;

	private SteamNetworkingProxy steamNetworkingProxy;

	private Queue<Amplitude.Unity.Game.Orders.Order> ordersHistory;

	private class EndEncounterContenderInformations
	{
		public EndEncounterContenderInformations()
		{
			this.UnitCountBeforeBattle = 0;
			this.ExperienceRewardOnKillPoint = 0f;
			this.UnitCountAfterBattle = 0;
			this.BountyReward = 0f;
		}

		public int UnitCountAfterBattle;

		public int UnitCountBeforeBattle;

		public List<Unit> UnitsKilledInAction = new List<Unit>();

		public float ExperienceRewardOnKillPoint;

		public float BountyReward;
	}

	public delegate void ChatMessageReceivedEventHandler(object sender, ChatMessageDescriptor message);

	public delegate void ProcessingOrderEventHandler(object sender, object order, int? empireIndex);

	public delegate void SendingMessageToServerEventHandler(object sender, object order, int? empireIndex);
}
