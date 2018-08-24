using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Amplitude;
using Amplitude.Interop;
using Amplitude.IO;
using Amplitude.Unity.AI;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Game.Orders;
using Amplitude.Unity.Messaging;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Session;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Steam;
using ICSharpCode.SharpZipLib.BZip2;
using UnityEngine;

[OrderProcessor(typeof(OrderSendAIAttitudeFeedback), "SendAIAttitudeFeedback")]
[OrderProcessor(typeof(OrderChangeUnitDeployment), "ChangeUnitDeployment")]
[OrderProcessor(typeof(OrderSelectAffinityStrategicResource), "SelectAffinityStrategicResource")]
[OrderProcessor(typeof(OrderRunTerraformationForDevice), "RunTerraformationForDevice")]
[OrderProcessor(typeof(OrderResetPointOfInterestInteractionBits), "ResetPointOfInterestInteractionBits")]
[OrderProcessor(typeof(OrderChangeDeployment), "ChangeDeployment")]
[OrderProcessor(typeof(OrderChangeUnitStrategy), "ChangeUnitStrategy")]
[OrderProcessor(typeof(OrderReportEncounter), "ReportEncounter")]
[OrderProcessor(typeof(OrderReplicateMarketplaceUnits), "ReplicateMarketplaceUnits")]
[OrderProcessor(typeof(OrderChangeContenderState), "ChangeContenderState")]
[OrderProcessor(typeof(OrderReplicateMarketplaceUnitProfile), "ReplicateMarketplaceUnitProfile")]
[OrderProcessor(typeof(OrderLockInteraction), "LockInteraction")]
[OrderProcessor(typeof(OrderChangeStrategy), "ChangeStrategy")]
[OrderProcessor(typeof(OrderChangeSeason), "ChangeSeason")]
[OrderProcessor(typeof(OrderReplicateMarketplaceUnitDesign), "ReplicateMarketplaceUnitDesign")]
[OrderProcessor(typeof(OrderJoinEncounter), "JoinEncounter")]
[OrderProcessor(typeof(OrderInteractWith), "InteractWith")]
[OrderProcessor(typeof(OrderIncludeContenderInEncounter), "IncludeContenderInEncounter")]
[OrderProcessor(typeof(OrderSetDeploymentFinished), "SetDeploymentFinished")]
[OrderProcessor(typeof(OrderChangeContenderReinforcementRanking), "ChangeContenderReinforcementRanking")]
[OrderProcessor(typeof(OrderChangeContenderEncounterOption), "ChangeContenderEncounterOption")]
[OrderProcessor(typeof(OrderRemoveMapBoosts), "RemoveMapBoosts")]
[OrderProcessor(typeof(OrderVoteForSeasonEffect), "VoteForSeasonEffect")]
[OrderProcessor(typeof(OrderNotifyEmpireDiscovery), "NotifyEmpireDiscovery")]
[OrderProcessor(typeof(OrderWinterImmunityBid), "WinterImmunityBid")]
[OrderProcessor(typeof(OrderChangeDiplomaticRelationState), "ChangeDiplomaticRelationState")]
[OrderProcessor(typeof(OrderChangeDiplomaticContractTermsCollection), "ChangeDiplomaticContractTermsCollection")]
[OrderProcessor(typeof(OrderGetAIDiplomaticContractEvaluation), "GetAIDiplomaticContractEvaluation")]
[OrderProcessor(typeof(OrderGetAIAttitude), "GetAIAttitude")]
[OrderProcessor(typeof(OrderGenerateNewWeather), "GenerateNewWeather")]
[OrderProcessor(typeof(OrderEndEncounter), "EndEncounter")]
[OrderProcessor(typeof(OrderSetEncounterDeployementEndTime), "SetEncounterDeployementEndTime")]
[OrderProcessor(typeof(OrderRefreshMarketplace), "RefreshMarketplace")]
[OrderProcessor(typeof(OrderChangeUnitTargeting), "ChangeUnitTargeting")]
[OrderProcessor(typeof(OrderEncounterTargetingPhaseUpdate), "EncounterTargetingPhaseUpdate")]
[OrderProcessor(typeof(OrderEncounterRoundUpdate), "EncounterRoundUpdate")]
[OrderProcessor(typeof(OrderBuyoutSpellAndPlayBattleAction), "BuyoutSpellAndPlayBattleAction")]
[OrderProcessor(typeof(OrderReadyForNextRound), "ReadyForNextRound")]
[OrderProcessor(typeof(OrderReadyForNextPhase), "ReadyForNextPhase")]
[OrderProcessor(typeof(OrderEncounterDeploymentStart), "EncounterDeploymentStart")]
[OrderProcessor(typeof(OrderReadyForDeployment), "ReadyForDeployment")]
[OrderProcessor(typeof(OrderGetAIDiplomaticTermEvaluation), "GetAIDiplomaticTermEvaluation")]
[OrderProcessor(typeof(OrderSwapUnitDeployment), "SwapUnitDeployment")]
[OrderProcessor(typeof(OrderChangeDiplomaticContractState), "ChangeDiplomaticContractState")]
[OrderProcessor(typeof(OrderUpdateWinterImmunityBids), "UpdateWinterImmunityBids")]
[OrderProcessor(typeof(OrderToggleEndlessDay), "ToggleEndlessDay")]
[OrderProcessor(typeof(OrderBuyoutAndPlaceTerraformationDevice), "BuyoutAndPlaceTerraformationDevice")]
[OrderProcessor(typeof(OrderBuyoutAndActivatePillarThroughArmy), "BuyoutAndActivatePillarThroughArmy")]
[OrderProcessor(typeof(OrderReadyForBattle), "ReadyForBattle")]
[OrderProcessor(typeof(OrderSetMapBoostSpawn), "SetMapBoostSpawn")]
[OrderProcessor(typeof(OrderChangeUnitsTargetingAndStrategy), "OrderChangeUnitsStrategies")]
[OrderProcessor(typeof(OrderCityEncounterEnd), "CityEncounterEnd")]
[OrderProcessor(typeof(OrderNotifyEncounter), "NotifyEncounter")]
[OrderProcessor(typeof(OrderOrbsChange), "OrbsChange")]
[OrderProcessor(typeof(OrderRemoveAffinityStrategicResource), "RemoveAffinityStrategicResource")]
[OrderProcessor(typeof(OrderClaimDiplomacyPoints), "ClaimDiplomacyPoints")]
[OrderProcessor(typeof(OrderCompleteQuest), "CompleteQuest")]
[OrderProcessor(typeof(OrderChangeAdministrationSpeciality), "ChangeAdministrationSpeciality")]
[OrderProcessor(typeof(OrderSwitchContendersReinforcementRanking), "SwitchContendersReinforcementRanking")]
[OrderProcessor(typeof(OrderCreateDiplomaticContract), "CreateDiplomaticContract")]
[OrderProcessor(typeof(OrderCreateEncounter), "CreateEncounter")]
[OrderProcessor(typeof(OrderCreateCityAssaultEncounter), "CreateCityAssaultEncounter")]
[OrderProcessor(typeof(OrderBuyoutAndActivatePillar), "BuyoutAndActivatePillar")]
[OrderProcessor(typeof(OrderPlayBattleAction), "PlayBattleAction")]
[OrderProcessor(typeof(OrderSetWindPreferences), "SetWindPreferences")]
[OrderProcessor(typeof(OrderSpawnMapBoosts), "SpawnMapBoosts")]
[OrderProcessor(typeof(OrderChangeReinforcementPriority), "ChangeReinforcementPriority")]
[OrderProcessor(typeof(OrderRazePointOfInterest), "RazePointOfInterest")]
[OrderProcessor(typeof(OrderDebugInfo), "DebugInfo")]
[OrderProcessor(typeof(OrderBeginEncounter), "BeginEncounter")]
[OrderProcessor(typeof(OrderAllocateEncounterDroppableTo), "AllocateEncounterDroppableTo")]
[OrderProcessor(typeof(OrderPacifyMinorFaction), "PacifyMinorFaction")]
[OrderProcessor(typeof(OrderQuestWorldEffect), "QuestWorldEffect")]
[OrderProcessor(typeof(OrderSetOrbSpawn), "SetOrbSpawn")]
[OrderProcessor(typeof(OrderJoinEncounterAcknowledge), "JoinEncounterAcknowledge")]
[OrderProcessor(typeof(OrderDestroyEncounter), "DestroyEncounter")]
[OrderProcessor(typeof(OrderActivateWeatherControl), "ActivateWeatherControl")]
public class GameServer : GameInterface, IDisposable, IService, IEnumerable, IBattleEncounterRepositoryService, IEnumerable<BattleEncounter>, IRepositoryService<BattleEncounter>, IGameInterface, IGameServer
{
	public GameServer(global::Session session) : base(session)
	{
		base.FiniteStateMachine.RegisterState(new GameServerState_InitializeServer(this));
		base.FiniteStateMachine.RegisterState(new GameServerState_LaunchGame(this));
		base.FiniteStateMachine.RegisterState(new GameServerState_GameLaunchedAndReady(this));
		base.FiniteStateMachine.RegisterState(new GameServerState_Turn_Begin(this));
		base.FiniteStateMachine.RegisterState(new GameServerState_Turn_CheckForGameEndingConditions(this));
		base.FiniteStateMachine.RegisterState(new GameServerState_Turn_DealWithGameEndingConditions(this));
		base.FiniteStateMachine.RegisterState(new GameServerState_Turn_AI(this));
		base.FiniteStateMachine.RegisterState(new GameServerState_Turn_Main(this));
		base.FiniteStateMachine.RegisterState(new GameServerState_Turn_Finished(this));
		base.FiniteStateMachine.RegisterState(new GameServerState_Turn_End(this));
		base.FiniteStateMachine.RegisterState(new GameServerState_Turn_Ended(this));
		base.FiniteStateMachine.RegisterState(new GameServerState_Autosave(this));
		base.FiniteStateMachine.RegisterState(new GameServerState_Transition(this));
		this.PostStateChange(typeof(GameServerState_InitializeServer), new object[0]);
		ISteamNetworkingService service = Services.GetService<ISteamNetworkingService>();
		service.RegisterMessageClass<GameClientInitiateConnectionMessage>();
		service.RegisterMessageClass<GameClientDownloadGameMessage>();
		service.RegisterMessageClass<GameClientAuthTicketMessage>();
		service.RegisterMessageClass<GameClientLeaveMessage>();
		service.RegisterMessageClass<GameClientDiagnosticsInfoMessage>();
		service.RegisterMessageClass<GameClientDownloadDumpResponseMessage>();
		service.RegisterMessageClass<GameClientPingMessage>();
		this.GameClientConnections = new Dictionary<ulong, GameClientConnection>();
		this.saveGameCoroutineQueue = new Queue<Amplitude.Coroutine>();
		this.PendingOrders = new Queue<GameClientPostOrderMessage>();
		this.PendingOrder = null;
		this.nextPendingOrderSerial = 0UL;
		if (global::Application.FantasyPreferences.ActivateAIScheduler)
		{
			this.InitializeAIScheduler();
		}
	}

	void IBattleEncounterRepositoryService.Clear()
	{
		this.battleEncounters.Clear();
	}

	IEnumerator<BattleEncounter> IEnumerable<BattleEncounter>.GetEnumerator()
	{
		foreach (BattleEncounter battleEncounter in this.battleEncounters.Values)
		{
			yield return battleEncounter;
		}
		yield break;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.battleEncounters.GetEnumerator();
	}

	public SynchronizationState SyncState
	{
		get
		{
			return this.syncState;
		}
		private set
		{
			this.syncState = value;
		}
	}

	public int CoolingOffPeriod { get; private set; }

	public void DownloadDumps(Steamworks.SteamID[] steamIDUsers)
	{
		this.dumpFilesToGather = steamIDUsers.Length + 1;
		PleaseWaitPanel.Instance.Show(string.Format("{0} ({1}/{2})", AgeLocalizer.Instance.LocalizeString("%GeneratingDesyncReport"), 0, this.dumpFilesToGather));
		Message message = new GameServerDownloadDumpRequestMessage(this.Game.Turn, "TurnEnded");
		base.SendMessage(ref message, steamIDUsers);
		base.SendMessage(ref message, new Steamworks.SteamID[]
		{
			this.SteamIDUser
		});
		this.dumpGuid = Guid.NewGuid();
		string path = System.IO.Path.Combine(global::Application.DumpFilesDirectory, string.Format("DesyncReport.{0:D3}.{1}", this.Game.Turn, this.dumpGuid.ToString()));
		Directory.CreateDirectory(path);
	}

	private void ProcessClientDiagnisticsInfoMessage(GameClientDiagnosticsInfoMessage message, Steamworks.SteamID steamIDRemote)
	{
		Diagnostics.Assert(message != null);
		Player playerBySteamID = this.playerRepository.GetPlayerBySteamID(steamIDRemote);
		if (playerBySteamID != null)
		{
			playerBySteamID.GameDiagnosticsInfo = new GameDiagnosticsInfo(message.Turn, message.Checksum, message.DumpingMethod);
			this.RefreshSynchronizationState();
		}
	}

	private void RefreshSynchronizationState()
	{
		switch (base.Session.SessionMode)
		{
		case SessionMode.Single:
			this.SyncState = SynchronizationState.Synchronized;
			if (this.synchronizationService != null)
			{
				this.synchronizationService.NotifySynchronizationStateChanged(this, new SynchronizationStateChangedArgs(this.syncState, 0, null));
			}
			break;
		case SessionMode.Private:
		case SessionMode.Protected:
		case SessionMode.Public:
		{
			GameDiagnosticsInfo gameDiagnosticsInfo = this.playerRepository.GetPlayerBySteamID(this.SteamIDUser).GameDiagnosticsInfo;
			foreach (Player player in from p in this.playerRepository
			where p.Type == PlayerType.Human
			select p)
			{
				SynchronizationState synchronizationState;
				if (player.GameDiagnosticsInfo.DumpingMethod != gameDiagnosticsInfo.DumpingMethod)
				{
					synchronizationState = SynchronizationState.MethodMismatch;
				}
				else if (player.GameDiagnosticsInfo.Turn != gameDiagnosticsInfo.Turn)
				{
					synchronizationState = SynchronizationState.TurnMismatch;
				}
				else if (player.GameDiagnosticsInfo.Checksum != gameDiagnosticsInfo.Checksum)
				{
					synchronizationState = SynchronizationState.ChecksumMismatch;
				}
				else
				{
					synchronizationState = SynchronizationState.Synchronized;
				}
				player.SynchronizationState = synchronizationState;
			}
			this.SyncState = (from p in this.playerRepository
			where p.Type == PlayerType.Human
			select p).Max((Player p) => p.SynchronizationState);
			switch (this.syncState)
			{
			case SynchronizationState.Synchronized:
				this.CoolingOffPeriod = global::GameManager.DumpingCoolingOffPeriod;
				break;
			case SynchronizationState.ChecksumMismatch:
				if (--this.CoolingOffPeriod >= 0)
				{
					this.syncState = SynchronizationState.ChecksumMismatchCoolingOffPeriod;
				}
				break;
			}
			if (this.synchronizationService != null)
			{
				this.synchronizationService.NotifySynchronizationStateChanged(this, new SynchronizationStateChangedArgs(this.SyncState, this.CoolingOffPeriod, null));
			}
			break;
		}
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	private void SaveUserDump(string filename, long uncompressedLength, byte[] buffer)
	{
		try
		{
			string text = System.IO.Path.Combine(global::Application.DumpFilesDirectory, string.Format("DesyncReport.{0:D3}.{1}", this.Game.Turn, this.dumpGuid.ToString()));
			string path = System.IO.Path.Combine(text, filename);
			using (FileStream fileStream = File.Create(path))
			{
				MemoryStream memoryStream = new MemoryStream(buffer);
				BZip2InputStream bzip2InputStream = new BZip2InputStream(memoryStream);
				memoryStream.Flush();
				bzip2InputStream.Flush();
				byte[] array = new byte[uncompressedLength];
				bzip2InputStream.Read(array, 0, array.Length);
				bzip2InputStream.Close();
				memoryStream.Close();
				fileStream.Write(array, 0, array.Length);
			}
			string[] files = Directory.GetFiles(text);
			if (files.Length >= this.dumpFilesToGather)
			{
				PleaseWaitPanel.Instance.Hide(false);
				this.dumpFilesToGather = 0;
			}
			else
			{
				PleaseWaitPanel.Instance.Show(string.Format("{0} ({1}/{2})", AgeLocalizer.Instance.LocalizeString("%GeneratingDesyncReport"), files.Length, this.dumpFilesToGather));
			}
		}
		catch (Exception ex)
		{
			Diagnostics.LogError("Exception raised while saving user's dump: {0}\n{1}", new object[]
			{
				ex.Message,
				ex.StackTrace
			});
		}
	}

	private void ISynchronizationService_SynchronizationStateChanged(object sender, SynchronizationStateChangedArgs e)
	{
		base.Session.SetLobbyData("_GameSyncState", e.SyncState.ToString(), true);
	}

	public void Register(BattleEncounter battleEncounter)
	{
		if (battleEncounter == null)
		{
			throw new ArgumentNullException("battleEncounter");
		}
		this.battleEncounters.Add(battleEncounter.EncounterGUID, battleEncounter);
	}

	public bool TryGetValue(GameEntityGUID guid, out BattleEncounter battleEncounter)
	{
		return this.battleEncounters.TryGetValue(guid, out battleEncounter);
	}

	public void Unregister(BattleEncounter battleEncounter)
	{
		if (battleEncounter == null)
		{
			throw new ArgumentNullException("battleEncounter");
		}
		this.battleEncounters.Remove(battleEncounter.EncounterGUID);
	}

	private bool ActivateWeatherControlPreprocessor(OrderActivateWeatherControl order)
	{
		global::Empire empire = this.Game.Empires[order.EmpireIndex];
		DepartmentOfTheInterior agency = empire.GetAgency<DepartmentOfTheInterior>();
		return agency != null && agency.OwnTheUniqueFacility(Fortress.UniqueFacilityNames.WeatherControl);
	}

	private bool AllocateEncounterDroppableToPreprocessor(OrderAllocateEncounterDroppableTo order)
	{
		bool result = false;
		if (this.Game.Services.GetService<IGameEntityRepositoryService>() == null)
		{
			Diagnostics.LogError("Unable to retrieve the game entity repository service.");
			return false;
		}
		IEncounterRepositoryService service = this.Game.GetService<IEncounterRepositoryService>();
		Diagnostics.Assert(service != null);
		Encounter encounter;
		if (service != null && service.TryGetValue(order.EncounterGuid, out encounter))
		{
			List<int> list = new List<int>();
			List<int>[] array = new List<int>[]
			{
				new List<int>(),
				new List<int>()
			};
			for (int i = 0; i < encounter.Contenders.Count; i++)
			{
				if (!array[(int)encounter.Contenders[i].Group].Contains(encounter.Contenders[i].Empire.Index))
				{
					array[(int)encounter.Contenders[i].Group].Add(encounter.Contenders[i].Empire.Index);
					list.Add(encounter.Contenders[i].Empire.Index);
				}
			}
			order.EmpireIndexes = list.ToArray();
			List<IDroppable>[] array2 = new List<IDroppable>[list.Count];
			for (int j = 0; j < array2.Length; j++)
			{
				array2[j] = new List<IDroppable>();
			}
			for (int k = 0; k < encounter.Contenders.Count; k++)
			{
				if (encounter.Contenders[k].ContenderState == ContenderState.Defeated)
				{
					this.AllocateRewardFromQuest(encounter.Contenders[k], array, array2);
					this.AllocateRewardFromFactionTraits(encounter.Contenders[k], array, array2);
					this.AllocateRewardFromMirages(encounter.Contenders[k], array, array2);
				}
			}
			order.DroppableByEmpireIndex = new IDroppable[array2.Length][];
			for (int l = 0; l < array2.Length; l++)
			{
				if (array2[l].Count > 0)
				{
					order.DroppableByEmpireIndex[l] = array2[l].ToArray();
					result = true;
				}
			}
		}
		return result;
	}

	private void AllocateRewardFromQuest(Contender contender, List<int>[] empireIndexByGroup, List<IDroppable>[] listByEmpireIndex)
	{
		IQuestRewardRepositoryService service = this.Game.Services.GetService<IQuestRewardRepositoryService>();
		if (service == null)
		{
			Diagnostics.LogError("Unable to retrieve the quest reward repository service.");
			return;
		}
		Droplist droplist;
		if (!service.ArmiesKillRewards.TryGetValue(contender.GUID, out droplist))
		{
			return;
		}
		int group = (int)contender.Group;
		int num = 0;
		for (int i = 0; i < empireIndexByGroup.Length; i++)
		{
			if (i == group)
			{
				num += empireIndexByGroup[i].Count;
			}
			else
			{
				int j = 0;
				while (j < empireIndexByGroup[i].Count)
				{
					global::Empire empire = this.Game.Empires[empireIndexByGroup[i][j]];
					IDroppable reward = Droplist.GetReward(empire, droplist);
					if (reward != null)
					{
						listByEmpireIndex[num].Add(reward);
					}
					j++;
					num++;
				}
			}
		}
	}

	private void AllocateRewardFromFactionTraits(Contender defeatedContender, List<int>[] empireIndexByGroup, List<IDroppable>[] listByEmpireIndex)
	{
		int group = (int)defeatedContender.Group;
		int num = 0;
		for (int i = 0; i < empireIndexByGroup.Length; i++)
		{
			if (i == group)
			{
				num += empireIndexByGroup[i].Count;
			}
			else
			{
				int j = 0;
				while (j < empireIndexByGroup[i].Count)
				{
					global::Empire empire = this.Game.Empires[empireIndexByGroup[i][j]];
					if (DepartmentOfTheInterior.CanLootVillages(empire) && defeatedContender.Garrison is Village && !(defeatedContender.Empire is MajorEmpire))
					{
						string value = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<string>("Gameplay/FactionTrait/Replicants/VillageRewardDroplist");
						Droplist droplist = null;
						if (this.TryGetDroplist(value, out droplist))
						{
							IDroppable reward = Droplist.GetReward(empire, droplist);
							if (reward != null)
							{
								listByEmpireIndex[num].Add(reward);
							}
						}
					}
					if (DepartmentOfDefense.CanLootArmies(empire, defeatedContender.Encounter))
					{
						string value2 = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<string>("Gameplay/FactionTrait/BrokenLords/EncounterRewardDroplist");
						Droplist droplist2 = null;
						if (this.TryGetDroplist(value2, out droplist2))
						{
							IDroppable droppable = Droplist.GetReward(empire, droplist2);
							bool flag = false;
							int num2 = 0;
							for (int k = 0; k < defeatedContender.EncounterUnits.Count; k++)
							{
								UnitSnapShot unitCurrentSnapshot = defeatedContender.EncounterUnits[k].UnitCurrentSnapshot;
								if (unitCurrentSnapshot != null && unitCurrentSnapshot.GetPropertyValue(SimulationProperties.Health) <= 0f)
								{
									num2++;
								}
							}
							if (droppable is DroppableResource)
							{
								DroppableResource droppableResource = droppable as DroppableResource;
								string resourceName = droppableResource.ResourceName;
								int quantity = droppableResource.Quantity * num2;
								droppable = new DroppableResource(resourceName, quantity);
								flag = true;
							}
							if (!flag)
							{
								Diagnostics.LogWarning("Couldn't multiply the encounter reward.");
							}
							listByEmpireIndex[num].Add(droppable);
						}
					}
					j++;
					num++;
				}
			}
		}
	}

	private void AllocateRewardFromMirages(Contender defeatedContender, List<int>[] empireIndexByGroup, List<IDroppable>[] listByEmpireIndex)
	{
		IMiragesService service = this.Game.GetService<IMiragesService>();
		IGarrison garrison = defeatedContender.Garrison;
		StaticString staticString = StaticString.Empty;
		for (int i = 0; i < defeatedContender.ContenderSnapShots.Count; i++)
		{
			ContenderSnapShot contenderSnapShot = defeatedContender.ContenderSnapShots[i];
			for (int j = 0; j < contenderSnapShot.UnitSnapShots.Count; j++)
			{
				UnitSnapShot unitSnapShot = contenderSnapShot.UnitSnapShots[j];
				if (!StaticString.IsNullOrEmpty(unitSnapShot.MirageDefinitionName))
				{
					staticString = unitSnapShot.MirageDefinitionName;
					i += defeatedContender.ContenderSnapShots.Count;
					break;
				}
			}
		}
		if (StaticString.IsNullOrEmpty(staticString))
		{
			return;
		}
		MirageDefinition mirageDefinition = service.GetMirageDefinition(staticString);
		if (mirageDefinition == null)
		{
			return;
		}
		Droplist droplist = null;
		if (this.TryGetDroplist(mirageDefinition.DroplistName, out droplist))
		{
			int group = (int)defeatedContender.Group;
			int num = 0;
			for (int k = 0; k < empireIndexByGroup.Length; k++)
			{
				if (k == group)
				{
					num += empireIndexByGroup[k].Count;
				}
				else
				{
					int l = 0;
					while (l < empireIndexByGroup[k].Count)
					{
						global::Empire empire = this.Game.Empires[empireIndexByGroup[k][l]];
						IDroppable reward = Droplist.GetReward(empire, droplist);
						if (reward != null)
						{
							listByEmpireIndex[num].Add(reward);
						}
						l++;
						num++;
					}
				}
			}
		}
	}

	private bool TryGetDroplist(string droplistName, out Droplist droplist)
	{
		if (string.IsNullOrEmpty(droplistName))
		{
			droplist = null;
			return false;
		}
		IDatabase<Droplist> database = Databases.GetDatabase<Droplist>(false);
		if (database == null)
		{
			Diagnostics.LogError("Failed to retrieve the database of droplists.");
			droplist = null;
			return false;
		}
		droplist = database.GetValue(droplistName);
		return droplist != null;
	}

	private bool BeginEncounterPreprocessor(OrderBeginEncounter order)
	{
		return true;
	}

	private bool BuyoutAndActivatePillarPreprocessor(OrderBuyoutAndActivatePillar order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (StaticString.IsNullOrEmpty(order.PillarDefinitionName))
		{
			Diagnostics.LogError("Order preprocessor failed because pillar definition name is either null or empty.");
			return false;
		}
		IGameEntityRepositoryService service = this.Game.Services.GetService<IGameEntityRepositoryService>();
		if (service == null)
		{
			Diagnostics.LogError("Cannot retreive the gameEntityRepositoryService.");
			return false;
		}
		IPillarService service2 = this.Game.Services.GetService<IPillarService>();
		if (service2 == null)
		{
			Diagnostics.LogError("Cannot retreive the pillar service.");
			return false;
		}
		global::Empire empire = null;
		try
		{
			empire = this.Game.Empires[order.EmpireIndex];
		}
		catch
		{
			Diagnostics.LogError("Order preprocessor failed because empire index is invalid.");
			return false;
		}
		if (!service2.IsPositionValidForPillar(empire, order.TargetPosition))
		{
			Diagnostics.LogError("Order preprocessor failed because the position is invalid.");
			return false;
		}
		IDatabase<PillarDefinition> database = Databases.GetDatabase<PillarDefinition>(false);
		PillarDefinition pillarDefinition;
		if (database != null && database.TryGetValue(order.PillarDefinitionName, out pillarDefinition))
		{
			float duration = pillarDefinition.GetDuration(empire);
			if (duration <= 1.401298E-45f)
			{
				Diagnostics.LogError("Order preprocessor failed because pillar duration is invalid (pillar definition name: '{0}', duration: {1}).", new object[]
				{
					pillarDefinition.Name,
					pillarDefinition.GetDuration(empire)
				});
				return false;
			}
			DepartmentOfTheTreasury agency = empire.GetAgency<DepartmentOfTheTreasury>();
			if (agency != null)
			{
				ConstructionResourceStock[] constructionResourceStocks;
				bool instantConstructionResourceCostForBuyout = agency.GetInstantConstructionResourceCostForBuyout(empire, pillarDefinition, out constructionResourceStocks);
				if (instantConstructionResourceCostForBuyout)
				{
					if (order.PillarGameEntityGUID == GameEntityGUID.Zero)
					{
						order.PillarGameEntityGUID = service.GenerateGUID();
					}
					order.ConstructionResourceStocks = constructionResourceStocks;
					return true;
				}
			}
		}
		return false;
	}

	private bool BuyoutAndActivatePillarThroughArmyPreprocessor(OrderBuyoutAndActivatePillarThroughArmy order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (!this.BuyoutAndActivatePillarPreprocessor(order))
		{
			return false;
		}
		IGameEntityRepositoryService service = this.Game.Services.GetService<IGameEntityRepositoryService>();
		if (service == null)
		{
			Diagnostics.LogError("Cannot retreive the gameEntityRepositoryService.");
			return false;
		}
		if (!order.ArmyGUID.IsValid)
		{
			return false;
		}
		IGameEntity gameEntity;
		if (!service.TryGetValue(order.ArmyGUID, out gameEntity))
		{
			return false;
		}
		Army army = gameEntity as Army;
		if (army == null)
		{
			return false;
		}
		if (order.NumberOfActionPointsToSpend < 0f && !StaticString.IsNullOrEmpty(order.ArmyActionName))
		{
			ArmyAction armyAction = null;
			IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
			if (database != null && database.TryGetValue(order.ArmyActionName, out armyAction))
			{
				order.NumberOfActionPointsToSpend = armyAction.GetCostInActionPoints();
			}
		}
		if (order.NumberOfActionPointsToSpend > 0f)
		{
			SimulationObjectWrapper simulationObjectWrapper = army;
			if (simulationObjectWrapper != null)
			{
				float propertyValue = simulationObjectWrapper.GetPropertyValue(SimulationProperties.MaximumNumberOfActionPoints);
				float propertyValue2 = simulationObjectWrapper.GetPropertyValue(SimulationProperties.ActionPointsSpent);
				if (order.NumberOfActionPointsToSpend > propertyValue - propertyValue2)
				{
					Diagnostics.LogWarning("Not enough action points.");
					return false;
				}
			}
		}
		return true;
	}

	private bool BuyoutAndPlaceTerraformationDevicePreprocessor(OrderBuyoutAndPlaceTerraformationDevice order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (StaticString.IsNullOrEmpty(order.TerraformDeviceDefinitionName))
		{
			Diagnostics.LogError("Order preprocessor failed because terraform device definition name is either null or empty.");
			return false;
		}
		IGameEntityRepositoryService service = this.Game.Services.GetService<IGameEntityRepositoryService>();
		if (service == null)
		{
			Diagnostics.LogError("Cannot retreive the gameEntityRepositoryService.");
			return false;
		}
		ITerraformDeviceService service2 = this.Game.Services.GetService<ITerraformDeviceService>();
		if (service2 == null)
		{
			Diagnostics.LogError("Cannot retreive the terraform device service.");
			return false;
		}
		global::Empire empire = null;
		try
		{
			empire = this.Game.Empires[order.EmpireIndex];
		}
		catch
		{
			Diagnostics.LogError("Order preprocessor failed because empire index is invalid.");
			return false;
		}
		if (!order.ArmyGuid.IsValid)
		{
			return false;
		}
		IGameEntity gameEntity;
		if (!service.TryGetValue(order.ArmyGuid, out gameEntity))
		{
			return false;
		}
		Army army = gameEntity as Army;
		if (army == null)
		{
			return false;
		}
		if (!service2.IsPositionValidForDevice(army.Empire, army.WorldPosition))
		{
			return false;
		}
		IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
		ArmyAction value = database.GetValue(order.ArmyActionName);
		if (value == null)
		{
			return false;
		}
		float costInActionPoints = value.GetCostInActionPoints();
		if (costInActionPoints > 0f)
		{
			SimulationObjectWrapper simulationObjectWrapper = army;
			if (simulationObjectWrapper != null)
			{
				float propertyValue = simulationObjectWrapper.GetPropertyValue(SimulationProperties.MaximumNumberOfActionPoints);
				float propertyValue2 = simulationObjectWrapper.GetPropertyValue(SimulationProperties.ActionPointsSpent);
				if (costInActionPoints > propertyValue - propertyValue2)
				{
					Diagnostics.LogWarning("Order preprocessor failed because army does not have enough action points.");
					return false;
				}
			}
		}
		for (int i = 0; i < value.Costs.Length; i++)
		{
			IConstructionCost constructionCost = value.Costs[i];
			string text = constructionCost.ResourceName;
			if (!string.IsNullOrEmpty(text) && !text.Equals(DepartmentOfTheTreasury.Resources.ActionPoint))
			{
				float costForResource = value.GetCostForResource(text);
				if (costForResource != 0f)
				{
					DepartmentOfTheTreasury agency = empire.GetAgency<DepartmentOfTheTreasury>();
					float num = 0f;
					if (!agency.TryGetResourceStockValue(empire.SimulationObject, text, out num, false))
					{
						Diagnostics.LogWarning("Order preprocessor failed because could not get the resource '{0}' stock value.", new object[]
						{
							text
						});
						return false;
					}
					if (!agency.CanAfford(costForResource, text))
					{
						Diagnostics.LogWarning("Order preprocessor failed because empire can not afford the army action cost.");
						return false;
					}
				}
			}
		}
		if (order.TerraformDeviceGameEntityGUID == GameEntityGUID.Zero)
		{
			order.TerraformDeviceGameEntityGUID = service.GenerateGUID();
		}
		IEventService service3 = Services.GetService<IEventService>();
		service3.Notify(new EventEmpireTerraformDevicePlaced(army.Empire, army.WorldPosition));
		return true;
	}

	private bool BuyoutSpellAndPlayBattleActionPreprocessor(OrderBuyoutSpellAndPlayBattleAction order)
	{
		if (!order.EncounterGUID.IsValid)
		{
			Diagnostics.LogError("The encounter GUID is invalid.");
			return false;
		}
		BattleEncounter battleEncounter = null;
		if (!this.TryGetValue(order.EncounterGUID, out battleEncounter))
		{
			Diagnostics.LogError("Can't retrieve battle encounter {0:X8}", new object[]
			{
				order.EncounterGUID
			});
			return false;
		}
		BattleContender battleContender = battleEncounter.BattleContenders.Find((BattleContender match) => match.GUID == order.ContenderGUID);
		if (battleContender == null)
		{
			Diagnostics.LogError("Can't retrieve battle contender {0:X8}.", new object[]
			{
				order.ContenderGUID
			});
			return false;
		}
		if (Databases.GetDatabase<BattleAction>(false) == null)
		{
			Diagnostics.LogError("Can't retrieve the battle action database.");
			return false;
		}
		DepartmentOfTheTreasury agency = battleContender.Garrison.Empire.GetAgency<DepartmentOfTheTreasury>();
		if (agency == null)
		{
			Diagnostics.LogError("Can't retrieve the departmentOfTheTreasury.");
			return false;
		}
		IDatabase<SpellDefinition> database = Databases.GetDatabase<SpellDefinition>(false);
		if (database == null)
		{
			Diagnostics.LogError("Can't retrieve the spellDefinition's database.");
			return false;
		}
		SpellDefinition spellDefinition;
		if (!database.TryGetValue(order.SpellDefinitionName, out spellDefinition))
		{
			Diagnostics.LogError("Can't retrieve the spellDefinition '{0}'.", new object[]
			{
				order.SpellDefinitionName
			});
			return false;
		}
		ConstructionResourceStock[] constructionResourceStocks;
		if (!agency.GetInstantConstructionResourceCostForBuyout(battleContender.Garrison.Empire, spellDefinition, out constructionResourceStocks))
		{
			Diagnostics.LogWarning("Order preprocessor failed because booster cost is not affordable (booster definition name: '{0}').", new object[]
			{
				order.SpellDefinitionName
			});
			return false;
		}
		order.ConstructionResourceStocks = constructionResourceStocks;
		return battleEncounter.LaunchSpell(battleContender, spellDefinition, order.TargetPosition);
	}

	private bool ChangeAdministrationSpecialityPreprocessor(OrderChangeAdministrationSpeciality order)
	{
		if (this.AIScheduler == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the AI is not active.");
			return false;
		}
		if (!string.IsNullOrEmpty(order.AdministrationSpeciality))
		{
			IDatabase<AICityState> database = Databases.GetDatabase<AICityState>(false);
			if (!database.ContainsKey(order.AdministrationSpeciality))
			{
				Diagnostics.LogError("Order preprocessing failed because the new administration speciality is not valid.");
				return false;
			}
		}
		if (!order.CityGuid.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the entity guid is not valid.");
			return false;
		}
		IGameEntityRepositoryService service = this.Game.Services.GetService<IGameEntityRepositoryService>();
		IGameEntity gameEntity = null;
		if (!service.TryGetValue(order.CityGuid, out gameEntity) || !(gameEntity is City))
		{
			Diagnostics.LogError("Order preprocessing failed because the entity guid is not a city.");
			return false;
		}
		return true;
	}

	private bool ChangeContenderEncounterOptionPreprocessor(OrderChangeContenderEncounterOption order)
	{
		if (order.EncounterGUID.IsValid)
		{
			BattleEncounter battleEncounter = null;
			if (this.TryGetValue(order.EncounterGUID, out battleEncounter))
			{
				battleEncounter.SetContenderOptionChoice(order.ContenderGUID, order.ContenderEncounterOptionChoice);
				return true;
			}
		}
		return false;
	}

	private bool ChangeContenderReinforcementRankingPreprocessor(OrderChangeContenderReinforcementRanking order)
	{
		if (order.EncounterGUID.IsValid)
		{
			BattleEncounter battleEncounter = null;
			if (this.TryGetValue(order.EncounterGUID, out battleEncounter))
			{
				for (int i = 0; i < order.FirstContenderGUIDs.Length; i++)
				{
					battleEncounter.ChangeContenderReinforcementRanking(order.FirstContenderGUIDs[i], order.NewIndexes[i]);
				}
				return true;
			}
		}
		return false;
	}

	private bool ChangeContenderStatePreprocessor(OrderChangeContenderState order)
	{
		if (order.EncounterGUID.IsValid)
		{
			BattleEncounter battleEncounter = null;
			if (this.TryGetValue(order.EncounterGUID, out battleEncounter))
			{
				battleEncounter.SetContenderState(order.ContenderGUID, order.ContenderState);
				return true;
			}
		}
		return false;
	}

	private bool ChangeDeploymentPreprocessor(OrderChangeDeployment order)
	{
		if (!order.EncounterGUID.IsValid)
		{
			return false;
		}
		BattleEncounter battleEncounter = null;
		if (!this.TryGetValue(order.EncounterGUID, out battleEncounter))
		{
			return false;
		}
		BattleContender battleContender = battleEncounter.BattleContenders.Find((BattleContender match) => match.GUID == order.ContenderGUID);
		if (battleContender == null)
		{
			return false;
		}
		if (battleContender.ContenderState != ContenderState.Deployment)
		{
			return false;
		}
		if (!battleContender.IsMainContender)
		{
			return false;
		}
		switch (order.Action)
		{
		case OrderChangeDeployment.ChangeAction.Reset:
			if (!battleEncounter.Deploy(battleContender, false))
			{
				return false;
			}
			break;
		case OrderChangeDeployment.ChangeAction.Forward:
			battleContender.Deployment.Forward();
			break;
		case OrderChangeDeployment.ChangeAction.Backward:
			battleContender.Deployment.Backward();
			break;
		}
		order.Deployment = battleContender.Deployment;
		return true;
	}

	private bool ChangeDiplomaticContractStatePreprocessor(OrderChangeDiplomaticContractState order)
	{
		Diagnostics.Assert(order != null);
		if (!order.ContractGUID.IsValid)
		{
			Diagnostics.LogError("ContractGUID is invalid.");
			return false;
		}
		IDiplomaticContractRepositoryService service = this.Game.Services.GetService<IDiplomaticContractRepositoryService>();
		Diagnostics.Assert(service != null);
		DiplomaticContract diplomaticContract;
		if (!service.TryGetValue(order.ContractGUID, out diplomaticContract))
		{
			Diagnostics.LogError("Can't retrieve the contract {0}.", new object[]
			{
				order.ContractGUID
			});
			return false;
		}
		Diagnostics.Assert(diplomaticContract.Terms != null);
		if (diplomaticContract.Terms.Count == 0 && (diplomaticContract.ContractRevisionNumber == 0 || (order.DiplomaticContractNewState != DiplomaticContractState.Refused && order.DiplomaticContractNewState != DiplomaticContractState.Ignored)))
		{
			Diagnostics.LogError("The contract {0} is empty.", new object[]
			{
				order.ContractGUID
			});
			return false;
		}
		if (order.DiplomaticContractNewState == DiplomaticContractState.Proposed)
		{
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			float num4 = 0f;
			for (int i = 0; i < diplomaticContract.Terms.Count; i++)
			{
				DiplomaticTerm diplomaticTerm = diplomaticContract.Terms[i];
				IDiplomaticCost[] diplomaticCosts = diplomaticTerm.DiplomaticCosts;
				if (diplomaticCosts != null)
				{
					foreach (IDiplomaticCost diplomaticCost in diplomaticCosts)
					{
						if (!(diplomaticCost.ResourceName != DepartmentOfTheTreasury.Resources.EmpirePoint))
						{
							float valueFor = diplomaticCost.GetValueFor(diplomaticContract.EmpireWhichProposes, diplomaticTerm);
							num += valueFor;
							float valueFor2 = diplomaticCost.GetValueFor(diplomaticContract.EmpireWhichReceives, diplomaticTerm);
							num2 += valueFor2;
							if (diplomaticCost.CanBeConvertedToPeacePoint)
							{
								num3 += valueFor;
								num4 += valueFor2;
							}
						}
					}
				}
			}
			num3 *= diplomaticContract.EmpireWhichProposes.GetPropertyValue(SimulationProperties.EmpirePointToPeacePointFactor);
			num4 *= diplomaticContract.EmpireWhichReceives.GetPropertyValue(SimulationProperties.EmpirePointToPeacePointFactor);
			Diagnostics.Assert(diplomaticContract.EmpireWhichProposes != null && diplomaticContract.EmpireWhichReceives != null);
			DepartmentOfTheTreasury agency = diplomaticContract.EmpireWhichProposes.GetAgency<DepartmentOfTheTreasury>();
			Diagnostics.Assert(agency != null);
			float num5 = -num;
			if (!agency.IsTransferOfResourcePossible(diplomaticContract.EmpireWhichProposes.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, ref num5))
			{
				Diagnostics.LogError("Can't change the status of the diplomatic contract {0} to {1} because the empire which proposes can't afford the cost of {2} empire points.", new object[]
				{
					diplomaticContract.GUID,
					order.DiplomaticContractNewState,
					num5
				});
				return false;
			}
			agency = diplomaticContract.EmpireWhichReceives.GetAgency<DepartmentOfTheTreasury>();
			Diagnostics.Assert(agency != null);
			num5 = -num2;
			if (!agency.IsTransferOfResourcePossible(diplomaticContract.EmpireWhichReceives.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, ref num5))
			{
				Diagnostics.LogError("Can't change the status of the diplomatic contract {0} to {1} because the empire which receives can't afford the cost of {2} empire points.", new object[]
				{
					diplomaticContract.GUID,
					order.DiplomaticContractNewState,
					num5
				});
				return false;
			}
			order.EmpireWhichProposesEmpirePointCost = num - diplomaticContract.EmpireWhichProposesEmpirePointInvestment;
			order.EmpireWhichReceivesEmpirePointCost = num2 - diplomaticContract.EmpireWhichReceivesEmpirePointInvestment;
			diplomaticContract.EmpireWhichProposesEmpirePointInvestment = num;
			diplomaticContract.EmpireWhichReceivesEmpirePointInvestment = num2;
			order.EmpireWhichProposesPeacePoint = num3 - diplomaticContract.EmpireWhichProposesPeacePointGain;
			order.EmpireWhichReceivesPeacePoint = num4 - diplomaticContract.EmpireWhichReceivesPeacePointGain;
			diplomaticContract.EmpireWhichProposesPeacePointGain = num3;
			diplomaticContract.EmpireWhichReceivesPeacePointGain = num4;
		}
		if (order.DiplomaticContractNewState == DiplomaticContractState.Proposed && diplomaticContract.GetPropositionMethod() == DiplomaticTerm.PropositionMethod.Declaration)
		{
			order.DiplomaticContractNewState = DiplomaticContractState.Signed;
		}
		if (order.DiplomaticContractNewState == DiplomaticContractState.Signed && !diplomaticContract.IsTransitionPossible(DiplomaticContractState.Signed))
		{
			if (diplomaticContract.IsTransitionPossible(DiplomaticContractState.Ignored))
			{
				Diagnostics.LogWarning("The transition between state {0} and state {1} is invalid for contract {2} because it can't be applied, the new diplomatic contract state will be set to Ignored.", new object[]
				{
					diplomaticContract.State,
					order.DiplomaticContractNewState,
					order.ContractGUID
				});
				order.DiplomaticContractNewState = DiplomaticContractState.Ignored;
			}
			else
			{
				Diagnostics.LogWarning("The transition between state {0} and state {1} is invalid for contract {2} because it can't be applied, the new diplomatic contract state will be set to Refused.", new object[]
				{
					diplomaticContract.State,
					order.DiplomaticContractNewState,
					order.ContractGUID
				});
				order.DiplomaticContractNewState = DiplomaticContractState.Refused;
			}
		}
		if (order.DiplomaticContractNewState == DiplomaticContractState.Negotiation)
		{
		}
		if (order.DiplomaticContractNewState == DiplomaticContractState.Refused || order.DiplomaticContractNewState == DiplomaticContractState.Ignored)
		{
			order.EmpireWhichProposesEmpirePointCost = -diplomaticContract.EmpireWhichProposesEmpirePointInvestment;
			order.EmpireWhichReceivesEmpirePointCost = -diplomaticContract.EmpireWhichReceivesEmpirePointInvestment;
			order.EmpireWhichProposesPeacePoint = -diplomaticContract.EmpireWhichProposesPeacePointGain;
			order.EmpireWhichReceivesPeacePoint = -diplomaticContract.EmpireWhichReceivesPeacePointGain;
			diplomaticContract.EmpireWhichProposesEmpirePointInvestment = 0f;
			diplomaticContract.EmpireWhichReceivesEmpirePointInvestment = 0f;
		}
		if (diplomaticContract.Terms.Count > 0 && !diplomaticContract.IsTransitionPossible(order.DiplomaticContractNewState))
		{
			Diagnostics.LogError("The transition between state {0} and state {1} is invalid for contract {2}.", new object[]
			{
				diplomaticContract.State,
				order.DiplomaticContractNewState,
				order.ContractGUID
			});
			return false;
		}
		return true;
	}

	private bool ChangeDiplomaticContractTermsCollectionPreprocessor(OrderChangeDiplomaticContractTermsCollection order)
	{
		Diagnostics.Assert(order != null);
		if (!order.ContractGUID.IsValid)
		{
			Diagnostics.LogError("ContractGUID is invalid.");
			return false;
		}
		if (order.DiplomaticTermChanges == null || order.DiplomaticTermChanges.Length == 0)
		{
			Diagnostics.LogError("OrderChangeDiplomaticContractTermsCollection constains no diplomatic term changes.");
			return false;
		}
		IDiplomaticContractRepositoryService service = this.Game.Services.GetService<IDiplomaticContractRepositoryService>();
		Diagnostics.Assert(service != null);
		DiplomaticContract diplomaticContract;
		if (!service.TryGetValue(order.ContractGUID, out diplomaticContract))
		{
			Diagnostics.LogError("Can't retrieve the contract {0}.", new object[]
			{
				order.ContractGUID
			});
			return false;
		}
		Diagnostics.Assert(diplomaticContract != null);
		if (diplomaticContract.State != DiplomaticContractState.Negotiation)
		{
			Diagnostics.LogError("The contract {0} is not in negotiation state ({1}).", new object[]
			{
				order.ContractGUID,
				diplomaticContract.State
			});
			return false;
		}
		for (int i = 0; i < order.DiplomaticTermChanges.Length; i++)
		{
			DiplomaticTermChange diplomaticTermChange = order.DiplomaticTermChanges[i];
			Diagnostics.Assert(diplomaticTermChange != null);
			switch (diplomaticTermChange.Action)
			{
			case CollectionChangeAction.Add:
				if (diplomaticTermChange.Term is IDiplomaticTermWithSignaturePreprocessorEffect)
				{
					(diplomaticTermChange.Term as IDiplomaticTermWithSignaturePreprocessorEffect).ApplySignaturePreprocessorEffects();
				}
				break;
			}
		}
		return true;
	}

	private bool ChangeDiplomaticRelationStatePreprocessor(OrderChangeDiplomaticRelationState order)
	{
		if (order.NewDiplomaticRelationStateName == DiplomaticRelationState.Names.Unknown)
		{
			Diagnostics.LogWarning("GameServer.ChangeDiplomaticRelationStatePreprocessor failed: trying to change the relation between empire {0} and empire {1} to an unknown one.", new object[]
			{
				order.InitiatorEmpireIndex,
				order.TargetEmpireIndex
			});
			return false;
		}
		return true;
	}

	private bool ChangeReinforcementPriorityPreprocessor(OrderChangeReinforcementPriority order)
	{
		if (!order.EncounterGUID.IsValid)
		{
			Diagnostics.LogError("The encounter GUID is invalid.");
			return false;
		}
		BattleEncounter battleEncounter = null;
		if (!this.TryGetValue(order.EncounterGUID, out battleEncounter))
		{
			Diagnostics.LogError("Can't retrieve battle encounter {0:X8}", new object[]
			{
				order.EncounterGUID
			});
			return false;
		}
		BattleContender battleContender = battleEncounter.BattleContenders.Find((BattleContender match) => match.GUID == order.ContenderGUID);
		if (battleContender == null)
		{
			Diagnostics.LogError("Can't retrieve battle contender {0:X8}.", new object[]
			{
				order.ContenderGUID
			});
			return false;
		}
		UnitBodyDefinition unitBodyDefinition = null;
		if (!StaticString.IsNullOrEmpty(order.PriorityBodyName))
		{
			IDatabase<UnitBodyDefinition> database = Databases.GetDatabase<UnitBodyDefinition>(false);
			if (database == null)
			{
				Diagnostics.LogError("Can't retrieve the unit body definition database.");
				return false;
			}
			if (!database.TryGetValue(order.PriorityBodyName, out unitBodyDefinition))
			{
				Diagnostics.LogError("Can't retrieve the unit body definition {0}.", new object[]
				{
					order.PriorityBodyName
				});
				return false;
			}
		}
		if (!battleEncounter.SetUnitBodyAsPioritary(battleContender, unitBodyDefinition))
		{
			Diagnostics.LogWarning("The battle simulation refuse to set the unit body definition {0} as priority body for reinforcement. In this case the GUI shouldn't allow you to trig the battle action.", new object[]
			{
				order.PriorityBodyName
			});
			return false;
		}
		return true;
	}

	private bool ChangeSeasonPreprocessor(OrderChangeSeason order)
	{
		if (string.IsNullOrEmpty(order.NewSeasonName))
		{
			Diagnostics.LogWarning("GameServer.OrderChangeSeason failed: new season's name is null or empty.");
			return false;
		}
		return true;
	}

	private bool ChangeStrategyPreprocessor(OrderChangeStrategy order)
	{
		if (!order.EncounterGUID.IsValid)
		{
			Diagnostics.LogError("The encounter GUID is invalid.");
			return false;
		}
		BattleEncounter battleEncounter = null;
		if (!this.TryGetValue(order.EncounterGUID, out battleEncounter))
		{
			Diagnostics.LogError("Can't retrieve battle encounter {0:X8}", new object[]
			{
				order.EncounterGUID
			});
			return false;
		}
		BattleContender battleContender = battleEncounter.BattleContenders.Find((BattleContender match) => match.GUID == order.ContenderGUID);
		if (battleContender == null)
		{
			Diagnostics.LogError("Can't retrieve battle contender {0:X8}.", new object[]
			{
				order.ContenderGUID
			});
			return false;
		}
		if (!battleEncounter.ChangeStrategy(battleContender, order.Strategy))
		{
			Diagnostics.LogError("The battle simulation refuse to switch to strategy {0}.", new object[]
			{
				order.Strategy
			});
			return false;
		}
		return true;
	}

	private bool ChangeUnitDeploymentPreprocessor(OrderChangeUnitDeployment order)
	{
		if (!order.EncounterGUID.IsValid)
		{
			return false;
		}
		BattleEncounter battleEncounter = null;
		if (!this.TryGetValue(order.EncounterGUID, out battleEncounter))
		{
			return false;
		}
		BattleContender battleContender = battleEncounter.BattleContenders.Find((BattleContender match) => match.GUID == order.ContenderGUID);
		if (battleContender == null)
		{
			return false;
		}
		for (int i = 0; i < battleContender.Deployment.UnitDeployment.Length; i++)
		{
			if (battleContender.Deployment.UnitDeployment[i].UnitGUID == order.UnitGUID)
			{
				battleContender.Deployment.UnitDeployment[i].WorldPosition = order.WorldPosition;
				return true;
			}
		}
		return false;
	}

	private bool ChangeUnitStrategyPreprocessor(OrderChangeUnitStrategy order)
	{
		if (!order.EncounterGUID.IsValid)
		{
			Diagnostics.LogError("The encounter GUID is invalid.");
			return false;
		}
		BattleEncounter battleEncounter = null;
		if (!this.TryGetValue(order.EncounterGUID, out battleEncounter))
		{
			Diagnostics.LogError("Can't retrieve battle encounter {0:X8}", new object[]
			{
				order.EncounterGUID
			});
			return false;
		}
		if (!battleEncounter.ChangeUnitStrategy(order.UnitGUID, order.Strategy))
		{
			Diagnostics.LogError("The battle simulation refuse to switch to strategy {0}.", new object[]
			{
				order.Strategy
			});
			return false;
		}
		return true;
	}

	private bool ChangeUnitTargetingPreprocessor(OrderChangeUnitTargeting order)
	{
		if (!order.EncounterGUID.IsValid)
		{
			return false;
		}
		BattleEncounter battleEncounter = null;
		if (!this.TryGetValue(order.EncounterGUID, out battleEncounter))
		{
			return false;
		}
		List<GameEntityGUID> availableOpportunityTargets = null;
		if (battleEncounter.ChangeUnitTargeting(order.UnitGUID, order.TargetingIntention, out availableOpportunityTargets))
		{
			order.AvailableOpportunityTargets = availableOpportunityTargets;
			return true;
		}
		return false;
	}

	private bool OrderChangeUnitsStrategiesPreprocessor(OrderChangeUnitsTargetingAndStrategy order)
	{
		if (!order.EncounterGUID.IsValid)
		{
			Diagnostics.LogError("The encounter GUID is invalid.");
			return false;
		}
		BattleEncounter battleEncounter = null;
		if (!this.TryGetValue(order.EncounterGUID, out battleEncounter))
		{
			Diagnostics.LogError("Can't retrieve battle encounter {0:X8}", new object[]
			{
				order.EncounterGUID
			});
			return false;
		}
		for (int i = 0; i < order.UnitStrategies.Length; i++)
		{
			OrderChangeUnitsTargetingAndStrategy.UnitTargetingData unitTargetingData = order.UnitStrategies[i];
			if (unitTargetingData != null && !battleEncounter.ChangeUnitStrategy(unitTargetingData.UnitGUID, unitTargetingData.Strategy))
			{
				Diagnostics.LogError("The battle simulation refuse to switch to strategy {0}.", new object[]
				{
					unitTargetingData.Strategy
				});
				return false;
			}
		}
		return true;
	}

	private bool CityEncounterEndPreprocessor(OrderCityEncounterEnd order)
	{
		if (order.CityGUID.IsValid)
		{
			IGameEntityRepositoryService service = this.Game.Services.GetService<IGameEntityRepositoryService>();
			IGameEntity gameEntity = null;
			if (service.TryGetValue(order.CityGUID, out gameEntity) && gameEntity is City)
			{
				City city = gameEntity as City;
				if (city.StandardUnits.Count == 0 && !order.CityIsStillDefended)
				{
					global::Empire empire = this.Game.Empires[order.AttackerEmpireIndex];
					IPlayerControllerRepositoryService service2 = base.GameService.Game.Services.GetService<IPlayerControllerRepositoryService>();
					IPlayerControllerRepositoryControl playerControllerRepositoryControl = service2 as IPlayerControllerRepositoryControl;
					if (playerControllerRepositoryControl != null)
					{
						global::PlayerController playerControllerById = playerControllerRepositoryControl.GetPlayerControllerById("server");
						if (playerControllerById != null)
						{
							bool flag = !(empire is MajorEmpire);
							bool flag2 = empire.SimulationObject.Tags.Contains("FactionTraitCultists9");
							bool flag3 = false;
							if (order.AttackerGarrisonGUID.IsValid)
							{
								IGameEntity gameEntity2 = null;
								if (service.TryGetValue(order.AttackerGarrisonGUID, out gameEntity2) && gameEntity2 is Army && ((Army)gameEntity2).IsPrivateers)
								{
									flag3 = true;
								}
							}
							IEventService service3 = Services.GetService<IEventService>();
							if (flag || flag2 || flag3)
							{
								if (flag2)
								{
									float propertyValue = city.GetPropertyValue(SimulationProperties.Population);
									DepartmentOfScience agency = empire.GetAgency<DepartmentOfScience>();
									bool flag4 = agency != null && agency.GetTechnologyState("TechnologyDefinitionCultists12") == DepartmentOfScience.ConstructibleElement.State.Researched;
									int num = 0;
									while ((float)num < propertyValue)
									{
										OrderBuyoutAndActivateBooster order2 = new OrderBuyoutAndActivateBooster(empire.Index, "BoosterIndustry", 0UL, false);
										playerControllerById.PostOrder(order2);
										if (flag4)
										{
											order2 = new OrderBuyoutAndActivateBooster(empire.Index, "BoosterScience", 0UL, false);
											playerControllerById.PostOrder(order2);
										}
										num++;
									}
								}
								OrderDestroyCity order3 = new OrderDestroyCity(city.Empire.Index, city.GUID, true, true, order.AttackerEmpireIndex);
								playerControllerById.PostOrder(order3);
								if (service3 != null)
								{
									EventCityRazed eventToNotify = new EventCityRazed(city.Empire, city.Region, empire, flag3);
									service3.Notify(eventToNotify);
								}
								return true;
							}
							OrderSwapCityOwner order4 = new OrderSwapCityOwner(city.Empire.Index, city.GUID, empire.Index);
							playerControllerById.PostOrder(order4);
							if (service3 != null)
							{
								EventCityCaptured eventToNotify2 = new EventCityCaptured(city.Empire, city, empire);
								service3.Notify(eventToNotify2);
							}
						}
					}
				}
			}
			return true;
		}
		return false;
	}

	private bool ClaimDiplomacyPointsPreprocessor(OrderClaimDiplomacyPoints order)
	{
		IGameEntity gameEntity = null;
		IGameEntityRepositoryService service = this.Game.Services.GetService<IGameEntityRepositoryService>();
		if (service == null)
		{
			Diagnostics.LogError("Unable to retrieve the game entity repository service.");
			return false;
		}
		global::Empire empire;
		try
		{
			empire = this.Game.Empires[order.EmpireIndex];
		}
		catch
		{
			Diagnostics.LogError("Unable to retrieve the empire.");
			return false;
		}
		if (order.InstigatorGUID.IsValid && service.TryGetValue(order.InstigatorGUID, out gameEntity))
		{
			ArmyAction armyAction = null;
			if (!StaticString.IsNullOrEmpty(order.ArmyActionName))
			{
				IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
				if (database != null)
				{
					database.TryGetValue(order.ArmyActionName, out armyAction);
				}
			}
			if (armyAction != null)
			{
				if (order.NumberOfActionPointsToSpend < 0f)
				{
					order.NumberOfActionPointsToSpend = armyAction.GetCostInActionPoints();
				}
				if (order.EmpirePointsCost <= 0f)
				{
					float num = empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier);
					if (num <= 0f)
					{
						num = 1f;
					}
					order.EmpirePointsCost = armyAction.GetCostForResource(DepartmentOfTheTreasury.Resources.EmpirePoint) * num;
				}
			}
			else
			{
				Diagnostics.LogWarning("GameServer.OrderClaimDiplomacyPoints could not find an ArmyAction. Check that the ArmyActionName is set correctly");
			}
			if (order.NumberOfActionPointsToSpend > 0f)
			{
				SimulationObjectWrapper simulationObjectWrapper = gameEntity as SimulationObjectWrapper;
				if (simulationObjectWrapper != null)
				{
					float propertyValue = simulationObjectWrapper.GetPropertyValue(SimulationProperties.MaximumNumberOfActionPoints);
					float propertyValue2 = simulationObjectWrapper.GetPropertyValue(SimulationProperties.ActionPointsSpent);
					if (order.NumberOfActionPointsToSpend > propertyValue - propertyValue2)
					{
						Diagnostics.LogWarning("Not enough action points.");
						return false;
					}
				}
			}
			if (order.EmpirePointsCost > 0f)
			{
				float num2 = order.EmpirePointsCost * -1f;
				DepartmentOfTheTreasury agency = empire.GetAgency<DepartmentOfTheTreasury>();
				if (!agency.IsTransferOfResourcePossible(empire, DepartmentOfTheTreasury.Resources.EmpirePoint, ref num2))
				{
					return false;
				}
			}
		}
		if (order.TargetGUID.IsValid && service.TryGetValue(order.TargetGUID, out gameEntity))
		{
			IDatabase<Droplist> database2 = Databases.GetDatabase<Droplist>(false);
			Droplist droplist;
			if (database2 != null && database2.TryGetValue(order.DroplistName, out droplist))
			{
				List<IDroppable> list = new List<IDroppable>();
				for (int i = 0; i < droplist.Picks; i++)
				{
					Droplist droplist2;
					IDroppable item = droplist.Pick(empire, out droplist2, new object[0]);
					list.Add(item);
				}
				list.RemoveAll((IDroppable match) => match == null);
				if (list.Count > 0)
				{
					order.QuestRewards = new QuestReward[1];
					order.QuestRewards[0] = new QuestReward();
					order.QuestRewards[0].Droppables = list.ToArray();
					order.QuestRewards[0].StepNumber = -1;
				}
			}
		}
		return true;
	}

	private bool CompleteQuestPreprocessor(OrderCompleteQuest order)
	{
		if (order.QuestGUID.IsValid)
		{
			IQuestRepositoryService service = this.Game.Services.GetService<IQuestRepositoryService>();
			if (service == null)
			{
				return false;
			}
			IQuestManagementService service2 = this.Game.Services.GetService<IQuestManagementService>();
			if (service2 == null)
			{
				return false;
			}
			Quest quest;
			if (service.TryGetValue(order.QuestGUID, out quest))
			{
				service2.UnregisterQuestBehaviour(quest);
				return true;
			}
		}
		return false;
	}

	private bool CreateDiplomaticContractPreprocessor(OrderCreateDiplomaticContract order)
	{
		if (order.EmpireWhichInitiatedIndex < 0 || order.EmpireWhichInitiatedIndex >= this.Game.Empires.Length || order.EmpireWhichReceivesIndex < 0 || order.EmpireWhichReceivesIndex >= this.Game.Empires.Length)
		{
			Diagnostics.LogError("Invalid empires index.");
			return false;
		}
		global::Empire empire = this.Game.Empires[order.EmpireWhichInitiatedIndex];
		global::Empire empire2 = this.Game.Empires[order.EmpireWhichReceivesIndex];
		IDiplomacyService service = this.Game.Services.GetService<IDiplomacyService>();
		Diagnostics.Assert(service != null);
		DiplomaticContract diplomaticContract;
		if (service.TryGetActiveDiplomaticContract(empire, empire2, out diplomaticContract))
		{
			Diagnostics.LogError("Can't create a new contract between {0} and {1} because an active contract already exist.", new object[]
			{
				empire,
				empire2
			});
			return false;
		}
		IGameEntityRepositoryService service2 = this.Game.Services.GetService<IGameEntityRepositoryService>();
		order.ContractGUID = service2.GenerateGUID();
		return true;
	}

	private bool CreateEncounterPreprocessor(OrderCreateEncounter order)
	{
		IBattleEncounterRepositoryService service = base.GameService.Game.Services.GetService<IBattleEncounterRepositoryService>();
		if (service != null)
		{
			int index;
			for (index = 0; index < order.ContenderGUIDs.Length; index++)
			{
				bool flag = service.Any((BattleEncounter encounter) => encounter.IsGarrisonInEncounter(order.ContenderGUIDs[index]));
				if (flag)
				{
					return false;
				}
			}
		}
		if (order.EncounterGUID.IsValid)
		{
			BattleEncounter battleEncounter = BattleEncounter.Decode(order);
			this.Register(battleEncounter);
			return true;
		}
		return false;
	}

	private bool CreateCityAssaultEncounterPreprocessor(OrderCreateCityAssaultEncounter order)
	{
		return this.CreateEncounterPreprocessor(order);
	}

	private bool DebugInfoPreprocessor(OrderDebugInfo order)
	{
		return true;
	}

	private bool DestroyEncounterPreprocessor(OrderDestroyEncounter order)
	{
		if (order.EncounterGUID.IsValid)
		{
			BattleEncounter battleEncounter = null;
			if (this.battleEncounters.TryGetValue(order.EncounterGUID, out battleEncounter))
			{
				this.Unregister(battleEncounter);
			}
			return true;
		}
		return false;
	}

	private bool EncounterDeploymentStartPreprocessor(OrderEncounterDeploymentStart order)
	{
		if (order.EncounterGUID.IsValid)
		{
			BattleEncounter battleEncounter = null;
			if (this.TryGetValue(order.EncounterGUID, out battleEncounter))
			{
				for (int i = 0; i < battleEncounter.BattleContenders.Count; i++)
				{
					BattleContender battleContender = battleEncounter.BattleContenders[i];
					battleEncounter.SetContenderState(battleContender.GUID, ContenderState.Deployment);
				}
				return true;
			}
		}
		return false;
	}

	private bool EncounterRoundUpdatePreprocessor(OrderEncounterRoundUpdate order)
	{
		if (order.EncounterGUID.IsValid)
		{
			BattleEncounter battleEncounter = null;
			if (this.TryGetValue(order.EncounterGUID, out battleEncounter))
			{
				for (int i = 0; i < battleEncounter.BattleContenders.Count; i++)
				{
					battleEncounter.SetContenderState(battleEncounter.BattleContenders[i].GUID, ContenderState.RoundInProgress);
				}
				return true;
			}
		}
		return false;
	}

	private bool EncounterTargetingPhaseUpdatePreprocessor(OrderEncounterTargetingPhaseUpdate order)
	{
		if (order.EncounterGUID.IsValid)
		{
			BattleEncounter battleEncounter = null;
			if (this.TryGetValue(order.EncounterGUID, out battleEncounter))
			{
				for (int i = 0; i < battleEncounter.BattleContenders.Count; i++)
				{
					BattleContender battleContender = battleEncounter.BattleContenders[i];
					battleEncounter.SetContenderState(battleContender.GUID, ContenderState.TargetingPhaseInProgress);
				}
				return true;
			}
		}
		return false;
	}

	private bool EndEncounterPreprocessor(OrderEndEncounter order)
	{
		if (!order.EncounterGUID.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the battle encounter GUID is not valid.");
			return false;
		}
		IEncounterRepositoryService service = this.Game.GetService<IEncounterRepositoryService>();
		Encounter encounter = null;
		if (!service.TryGetValue(order.EncounterGUID, out encounter) || encounter == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the encounter is not valid.");
			return false;
		}
		IPlayerControllerRepositoryService service2 = base.GameService.Game.Services.GetService<IPlayerControllerRepositoryService>();
		IPlayerControllerRepositoryControl playerControllerRepositoryControl = service2 as IPlayerControllerRepositoryControl;
		global::PlayerController playerControllerById = playerControllerRepositoryControl.GetPlayerControllerById("server");
		if (playerControllerById == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the server player controller is not valid.");
		}
		OrderAllocateEncounterDroppableTo order2 = new OrderAllocateEncounterDroppableTo(order.EncounterGUID);
		playerControllerById.PostOrder(order2);
		return true;
	}

	private bool GenerateNewWeatherPreprocessor(OrderGenerateNewWeather order)
	{
		return true;
	}

	private bool GetAIAttitudePreprocessor(OrderGetAIAttitude order)
	{
		Diagnostics.Assert(order != null);
		if (order.ReferenceEmpireIndex < 0 || order.ReferenceEmpireIndex >= this.Game.Empires.Length || order.TargetedEmpireIndex < 0 || order.TargetedEmpireIndex >= this.Game.Empires.Length)
		{
			Diagnostics.LogError("Order preprocessing failed because the empire indexes are invalid ({0}, {1}).", new object[]
			{
				order.ReferenceEmpireIndex,
				order.TargetedEmpireIndex
			});
			return false;
		}
		MajorEmpire majorEmpire = this.Game.Empires[order.ReferenceEmpireIndex] as MajorEmpire;
		if (majorEmpire == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the empire found at index {0} is not a major empire.", new object[]
			{
				order.ReferenceEmpireIndex
			});
			return false;
		}
		MajorEmpire majorEmpire2 = this.Game.Empires[order.TargetedEmpireIndex] as MajorEmpire;
		if (majorEmpire2 == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the empire found at index {0} is not a major empire.", new object[]
			{
				order.TargetedEmpireIndex
			});
			return false;
		}
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (this.AIScheduler != null && this.AIScheduler.TryGetMajorEmpireAIPlayer(majorEmpire, out aiplayer_MajorEmpire))
		{
			AIEntity entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
			if (entity == null)
			{
				Diagnostics.LogError("Order preprocessing failed because the empire {0} AI has no AI entity empire.", new object[]
				{
					majorEmpire.Index
				});
				return false;
			}
			AILayer_Attitude layer = entity.GetLayer<AILayer_Attitude>();
			if (layer == null)
			{
				Diagnostics.LogError("Order preprocessing failed because the empire {0} AI has no AI attitude layer.", new object[]
				{
					majorEmpire.Index
				});
				return false;
			}
			StaticString empty = StaticString.Empty;
			float categoryScore = -1f;
			if (layer.TryGetMainAttitudeCategory(majorEmpire2, ref empty, ref categoryScore))
			{
				AttitudeStateDefinition attitudeStateFromAttitudeCategory = layer.GetAttitudeStateFromAttitudeCategory(empty, categoryScore);
				order.AttitudeStateReference = ((attitudeStateFromAttitudeCategory == null) ? StaticString.Empty : attitudeStateFromAttitudeCategory.Name);
				if (!StaticString.IsNullOrEmpty(empty))
				{
					IEnumerable<DiplomaticRelationScore.ModifiersData> mainAttitudeModifiersFromCategory = layer.GetMainAttitudeModifiersFromCategory(majorEmpire2, empty, categoryScore);
					int num = mainAttitudeModifiersFromCategory.Count<DiplomaticRelationScore.ModifiersData>();
					order.MainAttitudeCategoryModifiers = new OrderGetAIAttitude.AttitudeModifierData[num];
					int num2 = 0;
					foreach (DiplomaticRelationScore.ModifiersData modifiersData in mainAttitudeModifiersFromCategory)
					{
						order.MainAttitudeCategoryModifiers[num2] = new OrderGetAIAttitude.AttitudeModifierData(modifiersData.Name, modifiersData.TotalValue);
						num2++;
					}
					Array.Sort<OrderGetAIAttitude.AttitudeModifierData>(order.MainAttitudeCategoryModifiers, (OrderGetAIAttitude.AttitudeModifierData left, OrderGetAIAttitude.AttitudeModifierData right) => Mathf.Abs(right.Value).CompareTo(Mathf.Abs(left.Value)));
				}
			}
		}
		return true;
	}

	private bool GetAIDiplomaticContractEvaluationPreprocessor(OrderGetAIDiplomaticContractEvaluation order)
	{
		Diagnostics.Assert(order != null);
		if (!order.ContractGUID.IsValid)
		{
			Diagnostics.LogError("ContractGUID is invalid.");
			return false;
		}
		IDiplomaticContractRepositoryService service = this.Game.Services.GetService<IDiplomaticContractRepositoryService>();
		Diagnostics.Assert(service != null);
		DiplomaticContract diplomaticContract;
		if (!service.TryGetValue(order.ContractGUID, out diplomaticContract))
		{
			Diagnostics.LogError("Can't retrieve the contract {0}.", new object[]
			{
				order.ContractGUID
			});
			return false;
		}
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (this.AIScheduler != null && this.AIScheduler.TryGetMajorEmpireAIPlayer(diplomaticContract.EmpireWhichReceives as MajorEmpire, out aiplayer_MajorEmpire))
		{
			AIEntity entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
			if (entity != null)
			{
				AILayer_Diplomacy layer = entity.GetLayer<AILayer_Diplomacy>();
				if (layer != null)
				{
					order.AIEvaluationScore = layer.AnalyseContractProposition(diplomaticContract);
				}
			}
		}
		return true;
	}

	private bool GetAIDiplomaticTermEvaluationPreprocessor(OrderGetAIDiplomaticTermEvaluation order)
	{
		Diagnostics.Assert(order != null);
		if (order.ReferenceEmpireIndex < 0 || order.ReferenceEmpireIndex >= this.Game.Empires.Length || order.TargetedEmpireIndex < 0 || order.TargetedEmpireIndex >= this.Game.Empires.Length)
		{
			Diagnostics.LogError("Order preprocessing failed because the empire indexes are invalid ({0}, {1}).", new object[]
			{
				order.ReferenceEmpireIndex,
				order.TargetedEmpireIndex
			});
			return false;
		}
		MajorEmpire majorEmpire = this.Game.Empires[order.ReferenceEmpireIndex] as MajorEmpire;
		if (majorEmpire == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the empire found at index {0} is not a major empire.", new object[]
			{
				order.ReferenceEmpireIndex
			});
			return false;
		}
		MajorEmpire majorEmpire2 = this.Game.Empires[order.TargetedEmpireIndex] as MajorEmpire;
		if (majorEmpire2 == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the empire found at index {0} is not a major empire.", new object[]
			{
				order.TargetedEmpireIndex
			});
			return false;
		}
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (this.AIScheduler != null && this.AIScheduler.TryGetMajorEmpireAIPlayer(majorEmpire, out aiplayer_MajorEmpire))
		{
			AIEntity entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
			if (entity == null)
			{
				Diagnostics.LogError("Order preprocessing failed because the empire {0} AI has no AI entity empire.", new object[]
				{
					majorEmpire.Index
				});
				return false;
			}
			AILayer_Diplomacy layer = entity.GetLayer<AILayer_Diplomacy>();
			if (layer == null)
			{
				Diagnostics.LogError("Order preprocessing failed because the empire {0} AI has no AI diplomacy layer.", new object[]
				{
					majorEmpire.Index
				});
				return false;
			}
			order.DiplomaticTermEvaluations = new float[order.DiplomaticTerms.Length];
			for (int i = 0; i < order.DiplomaticTerms.Length; i++)
			{
				DiplomaticTerm term = order.DiplomaticTerms[i];
				DecisionResult decisionResult = layer.EvaluateDecision(term, majorEmpire2, null);
				order.DiplomaticTermEvaluations[i] = decisionResult.Score;
			}
		}
		return true;
	}

	private bool IncludeContenderInEncounterPreprocessor(OrderIncludeContenderInEncounter order)
	{
		if (!order.EncounterGUID.IsValid)
		{
			Diagnostics.LogError("The encounter GUID is invalid.");
			return false;
		}
		BattleEncounter battleEncounter = null;
		if (!this.TryGetValue(order.EncounterGUID, out battleEncounter))
		{
			Diagnostics.LogError("Can't retrieve battle encounter {0:X8}", new object[]
			{
				order.EncounterGUID
			});
			return false;
		}
		return battleEncounter.IncludeContenderInEncounter(order.ContenderGUID, order.Include);
	}

	private bool InteractWithPreprocessor(OrderInteractWith order)
	{
		IGameEntity gameEntity = null;
		IGameEntityRepositoryService service = this.Game.Services.GetService<IGameEntityRepositoryService>();
		if (service == null)
		{
			Diagnostics.LogError("Unable to retrieve the game entity repository service.");
			return false;
		}
		if (this.Game.Services.GetService<IPlayerControllerRepositoryService>() == null)
		{
			Diagnostics.LogError("Unable to retrieve the player controller repository service.");
			return false;
		}
		IEventService service2 = Services.GetService<IEventService>();
		if (service2 == null)
		{
			Diagnostics.LogError("Unable to retrieve the event service.");
			return false;
		}
		IGameEntity gameEntity2 = null;
		bool flag = false;
		IQuestManagementService service3 = this.Game.Services.GetService<IQuestManagementService>();
		if (service3 != null)
		{
			global::Empire empire;
			try
			{
				empire = this.Game.Empires[order.EmpireIndex];
			}
			catch
			{
				Diagnostics.LogError("Unable to retrieve the empire.");
				return false;
			}
			service3.InitState(order.Tags, empire, order.WorldPosition);
			IGameEntity gameEntity3 = null;
			if (order.InstigatorGUID.IsValid)
			{
				if (service.TryGetValue(order.InstigatorGUID, out gameEntity))
				{
					if (gameEntity is SimulationObjectWrapper)
					{
						service3.State.AddTargets("$(Instigator)", gameEntity as SimulationObjectWrapper);
						service3.State.AddTargets("$(" + gameEntity.GetType().Name + ")", gameEntity as SimulationObjectWrapper);
					}
					if (order.NumberOfActionPointsToSpend < 0f && !StaticString.IsNullOrEmpty(order.ArmyActionName))
					{
						ArmyAction armyAction = null;
						IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
						if (database != null && database.TryGetValue(order.ArmyActionName, out armyAction))
						{
							order.NumberOfActionPointsToSpend = armyAction.GetCostInActionPoints();
						}
					}
					if (order.NumberOfActionPointsToSpend > 0f)
					{
						SimulationObjectWrapper simulationObjectWrapper = gameEntity as SimulationObjectWrapper;
						if (simulationObjectWrapper != null)
						{
							float propertyValue = simulationObjectWrapper.GetPropertyValue(SimulationProperties.MaximumNumberOfActionPoints);
							float propertyValue2 = simulationObjectWrapper.GetPropertyValue(SimulationProperties.ActionPointsSpent);
							if (order.NumberOfActionPointsToSpend > propertyValue - propertyValue2)
							{
								Diagnostics.LogWarning("Not enough action points.");
								return false;
							}
						}
					}
				}
				gameEntity3 = gameEntity;
			}
			if (order.TargetGUID.IsValid && service.TryGetValue(order.TargetGUID, out gameEntity))
			{
				gameEntity2 = gameEntity;
				IQuestRepositoryService service4 = this.Game.Services.GetService<IQuestRepositoryService>();
				if (service4 == null)
				{
					Diagnostics.LogError("Unable to retrieve the quest repository service.");
					return false;
				}
				IEnumerable<QuestMarker> markersByBoundTargetGUID = service3.GetMarkersByBoundTargetGUID(order.TargetGUID);
				foreach (QuestMarker questMarker in markersByBoundTargetGUID)
				{
					Quest quest;
					if (service4.TryGetValue(questMarker.QuestGUID, out quest))
					{
						if (!(quest.QuestDefinition.Category != QuestDefinition.CategoryMainQuest) || !(quest.QuestDefinition.Category != QuestDefinition.CategoryVictoryQuest))
						{
							if (quest.EmpireBits == empire.Bits)
							{
								EventInteractWith eventToNotify = new EventInteractWith(empire, gameEntity3, gameEntity, questMarker, order.InteractionName);
								service2.Notify(eventToNotify);
								if (!questMarker.IgnoreInteraction)
								{
									return false;
								}
							}
						}
					}
				}
				foreach (QuestMarker questMarker2 in markersByBoundTargetGUID)
				{
					Quest quest2;
					if (service4.TryGetValue(questMarker2.QuestGUID, out quest2))
					{
						if (!(quest2.QuestDefinition.Category == QuestDefinition.CategoryMainQuest) && !(quest2.QuestDefinition.Category == QuestDefinition.CategoryVictoryQuest))
						{
							if (quest2.EmpireBits == empire.Bits)
							{
								EventInteractWith eventToNotify2 = new EventInteractWith(empire, gameEntity3, gameEntity, questMarker2, order.InteractionName);
								service2.Notify(eventToNotify2);
								if (!questMarker2.IgnoreInteraction)
								{
									return false;
								}
							}
						}
					}
				}
				if (gameEntity is PointOfInterest)
				{
					PointOfInterest pointOfInterest = gameEntity as PointOfInterest;
					if (pointOfInterest.Interaction.IsLocked(empire.Index, order.InteractionName))
					{
						Diagnostics.LogWarning("Point of interest has another interaction in progress.");
						return false;
					}
					int bits = pointOfInterest.Interaction.Bits;
					if ((bits & 1 << order.EmpireIndex) != 0 && (!pointOfInterest.UntappedDustDeposits || !SimulationGlobal.GlobalTagsContains(SeasonManager.RuinDustDepositsTag)))
					{
						Diagnostics.LogWarning("Point of interest has already been interacted with.");
						return false;
					}
					flag = true;
					EventInteractWith eventToNotify3 = new EventInteractWith(empire, gameEntity3, gameEntity2, null, order.InteractionName);
					service2.Notify(eventToNotify3);
				}
				if (gameEntity is PointOfInterest && gameEntity is SimulationObjectWrapper)
				{
					order.Rewarding = true;
					SimulationObjectWrapper simulationObjectWrapper2 = gameEntity as SimulationObjectWrapper;
					PointOfInterest pointOfInterest2 = gameEntity as PointOfInterest;
					if (pointOfInterest2 != null && pointOfInterest2.UntappedDustDeposits)
					{
						this.ProcessReward(order, empire, SeasonManager.DustDepositsDroplistName, false);
						return true;
					}
					int num = (int)simulationObjectWrapper2.GetPropertyValue(SimulationProperties.InteractOddsTriggeringAQuest);
					int num2 = (int)simulationObjectWrapper2.GetPropertyValue(SimulationProperties.InteractOddsLooting);
					int num3 = (int)simulationObjectWrapper2.GetPropertyValue(SimulationProperties.InteractOddsLootingNothing);
					if (gameEntity3 != null && gameEntity3 is SimulationObjectWrapper)
					{
						SimulationObjectWrapper simulationObjectWrapper3 = gameEntity3 as SimulationObjectWrapper;
						num += (int)simulationObjectWrapper3.GetPropertyValue(SimulationProperties.InteractOddsTriggeringAQuest);
						num2 += (int)simulationObjectWrapper3.GetPropertyValue(SimulationProperties.InteractOddsLooting);
						num3 += (int)simulationObjectWrapper3.GetPropertyValue(SimulationProperties.InteractOddsLootingNothing);
					}
					int max = num + num2 + num3;
					int num4 = UnityEngine.Random.Range(0, max);
					if (num4 >= num)
					{
						num4 -= num;
						if (num4 < num2)
						{
							string droplistName = "DroplistInteractWith" + simulationObjectWrapper2.GetDescriptorNameFromType("QuestLocationType");
							this.ProcessReward(order, empire, droplistName, true);
						}
						return true;
					}
				}
				if (gameEntity is SimulationObjectWrapper)
				{
					SimulationObjectWrapper targets = gameEntity as SimulationObjectWrapper;
					service3.State.AddTargets("$(Target)", targets);
					service3.State.AddTargets("$(" + gameEntity.GetType().Name + ")", targets);
				}
			}
			service3.State.WorldPosition = order.WorldPosition;
			bool flag2;
			do
			{
				flag2 = false;
				QuestDefinition questDefinition;
				QuestVariable[] questVariables;
				QuestInstruction[] pendingInstructions;
				QuestReward[] questRewards;
				Dictionary<Region, List<string>> regionQuestLocalizationVariableDefinitionLocalizationKey;
				if (service3.TryTrigger(out questDefinition, out questVariables, out pendingInstructions, out questRewards, out regionQuestLocalizationVariableDefinitionLocalizationKey))
				{
					flag2 = service3.Trigger(empire, questDefinition, questVariables, pendingInstructions, questRewards, regionQuestLocalizationVariableDefinitionLocalizationKey, gameEntity, true);
					if (flag2 && order.Rewarding)
					{
						order.QuestRewards = new QuestReward[1];
						order.QuestRewards[0] = new QuestReward();
						order.QuestRewards[0].Droppables = new IDroppable[]
						{
							new DroppableString
							{
								Value = questDefinition.Name
							}
						};
					}
				}
				else if (order.Tags.Contains("Talk") || order.Tags.Contains("NavalTalk"))
				{
					flag = false;
				}
			}
			while (flag2 && service3.State.Tags.Contains("BeginTurn"));
			if (flag && gameEntity2 != null && gameEntity2 is PointOfInterest)
			{
				PointOfInterest pointOfInterest3 = gameEntity2 as PointOfInterest;
				pointOfInterest3.Interaction.Bits |= 1 << order.EmpireIndex;
			}
		}
		return order.Rewarding || !StaticString.IsNullOrEmpty(order.ArmyActionName);
	}

	private void ProcessReward(OrderInteractWith order, global::Empire empire, string droplistName, bool applyFactionTags = true)
	{
		IDatabase<Droplist> database = Databases.GetDatabase<Droplist>(false);
		if (database == null)
		{
			return;
		}
		Droplist droplist;
		if (database.TryGetValue(droplistName, out droplist))
		{
			List<IDroppable> list = new List<IDroppable>();
			for (int i = 0; i < droplist.Picks; i++)
			{
				Droplist droplist2;
				IDroppable item = droplist.Pick(empire, out droplist2, new object[0]);
				list.Add(item);
			}
			if (applyFactionTags)
			{
				if (this.Game.Empires[order.EmpireIndex].SimulationObject.Tags.Contains("FactionTraitWinterShifters2") && database.TryGetValue("DroplistOrbSupplement", out droplist))
				{
					Droplist droplist3;
					IDroppable item2 = droplist.Pick(empire, out droplist3, new object[0]);
					list.Add(item2);
				}
				if (this.Game.Empires[order.EmpireIndex].SimulationObject.Tags.Contains("FactionTraitFlames8") && database.TryGetValue("DroplistFlamesScience", out droplist))
				{
					Droplist droplist4;
					IDroppable item3 = droplist.Pick(empire, out droplist4, new object[0]);
					list.Add(item3);
				}
			}
			list.RemoveAll((IDroppable match) => match == null);
			if (list.Count > 0)
			{
				order.QuestRewards = new QuestReward[1];
				order.QuestRewards[0] = new QuestReward();
				order.QuestRewards[0].Droppables = list.ToArray();
				order.QuestRewards[0].StepNumber = -1;
			}
		}
	}

	private bool JoinEncounterPreprocessor(OrderJoinEncounter order)
	{
		GameServer.<JoinEncounterPreprocessor>c__AnonStorey9C5 <JoinEncounterPreprocessor>c__AnonStorey9C = new GameServer.<JoinEncounterPreprocessor>c__AnonStorey9C5();
		if (!order.EncounterGUID.IsValid)
		{
			return false;
		}
		<JoinEncounterPreprocessor>c__AnonStorey9C.battleEncounter = null;
		if (!this.TryGetValue(order.EncounterGUID, out <JoinEncounterPreprocessor>c__AnonStorey9C.battleEncounter))
		{
			return false;
		}
		<JoinEncounterPreprocessor>c__AnonStorey9C.battleEncounter.IncommingJoinContendersCount--;
		IGameEntityRepositoryService service = this.Game.GetService<IGameEntityRepositoryService>();
		Diagnostics.Assert(service != null);
		IBattleEncounterRepositoryService service2 = base.GameService.Game.Services.GetService<IBattleEncounterRepositoryService>();
		BattleContender battleContender = null;
		for (int i = 0; i < order.ContenderInfos.Count; i++)
		{
			GameServer.<JoinEncounterPreprocessor>c__AnonStorey9C6 <JoinEncounterPreprocessor>c__AnonStorey9C2 = new GameServer.<JoinEncounterPreprocessor>c__AnonStorey9C6();
			<JoinEncounterPreprocessor>c__AnonStorey9C2.<>f__ref$2501 = <JoinEncounterPreprocessor>c__AnonStorey9C;
			<JoinEncounterPreprocessor>c__AnonStorey9C2.currentContender = order.ContenderInfos[i];
			<JoinEncounterPreprocessor>c__AnonStorey9C2.currentContender.IsTakingPartInBattle = true;
			<JoinEncounterPreprocessor>c__AnonStorey9C2.currentContender.IsValid = true;
			if (!service.TryGetValue(<JoinEncounterPreprocessor>c__AnonStorey9C2.currentContender.ContenderGUID, out <JoinEncounterPreprocessor>c__AnonStorey9C2.gameEntity))
			{
				<JoinEncounterPreprocessor>c__AnonStorey9C2.currentContender.IsValid = false;
			}
			else
			{
				if (<JoinEncounterPreprocessor>c__AnonStorey9C2.gameEntity is Garrison)
				{
					Garrison garrison = <JoinEncounterPreprocessor>c__AnonStorey9C2.gameEntity as Garrison;
					if (garrison.IsInEncounter)
					{
						<JoinEncounterPreprocessor>c__AnonStorey9C2.currentContender.IsTakingPartInBattle = false;
					}
				}
				bool flag = service2.Any((BattleEncounter encounter) => encounter.EncounterGUID != <JoinEncounterPreprocessor>c__AnonStorey9C2.<>f__ref$2501.battleEncounter.EncounterGUID && encounter.IsGarrisonInEncounter(<JoinEncounterPreprocessor>c__AnonStorey9C2.currentContender.ContenderGUID));
				if (flag)
				{
					<JoinEncounterPreprocessor>c__AnonStorey9C2.currentContender.IsTakingPartInBattle = false;
				}
				Army army = <JoinEncounterPreprocessor>c__AnonStorey9C2.gameEntity as Army;
				if (army != null)
				{
					DepartmentOfTransportation agency = army.Empire.GetAgency<DepartmentOfTransportation>();
					ArmyGoToInstruction armyGoToInstruction = agency.ArmiesWithPendingGoToInstructions.Find((ArmyGoToInstruction match) => match.ArmyGUID == <JoinEncounterPreprocessor>c__AnonStorey9C2.gameEntity.GUID);
					if (armyGoToInstruction != null && armyGoToInstruction.IsMoving)
					{
						armyGoToInstruction.Cancel(false);
					}
				}
				City city = <JoinEncounterPreprocessor>c__AnonStorey9C2.gameEntity as City;
				Camp camp = <JoinEncounterPreprocessor>c__AnonStorey9C2.gameEntity as Camp;
				Village village = <JoinEncounterPreprocessor>c__AnonStorey9C2.gameEntity as Village;
				<JoinEncounterPreprocessor>c__AnonStorey9C2.currentContender.IsCity = (city != null);
				<JoinEncounterPreprocessor>c__AnonStorey9C2.currentContender.IsCamp = (camp != null);
				<JoinEncounterPreprocessor>c__AnonStorey9C2.currentContender.IsVillage = (village != null);
				if (!<JoinEncounterPreprocessor>c__AnonStorey9C.battleEncounter.Join(<JoinEncounterPreprocessor>c__AnonStorey9C2.currentContender.ContenderGUID, <JoinEncounterPreprocessor>c__AnonStorey9C2.currentContender.IsCity, <JoinEncounterPreprocessor>c__AnonStorey9C2.currentContender.IsCamp, <JoinEncounterPreprocessor>c__AnonStorey9C2.currentContender.IsVillage, <JoinEncounterPreprocessor>c__AnonStorey9C2.currentContender.Group, <JoinEncounterPreprocessor>c__AnonStorey9C2.currentContender.IsReinforcement, <JoinEncounterPreprocessor>c__AnonStorey9C2.currentContender.ReinforcementRanking, <JoinEncounterPreprocessor>c__AnonStorey9C2.currentContender.IsTakingPartInBattle, out battleContender))
				{
					<JoinEncounterPreprocessor>c__AnonStorey9C2.currentContender.IsValid = false;
				}
				else
				{
					<JoinEncounterPreprocessor>c__AnonStorey9C2.currentContender.Deployment = battleContender.Deployment;
					<JoinEncounterPreprocessor>c__AnonStorey9C2.currentContender.IsReinforcement = !battleContender.IsMainContender;
					GameServer.<JoinEncounterPreprocessor>c__AnonStorey9C6 <JoinEncounterPreprocessor>c__AnonStorey9C3 = <JoinEncounterPreprocessor>c__AnonStorey9C2;
					<JoinEncounterPreprocessor>c__AnonStorey9C3.currentContender.IsTakingPartInBattle = (<JoinEncounterPreprocessor>c__AnonStorey9C3.currentContender.IsTakingPartInBattle & battleContender.IsTakingPartInBattle);
				}
			}
			order.ContenderInfos[i] = <JoinEncounterPreprocessor>c__AnonStorey9C2.currentContender;
		}
		return true;
	}

	private bool JoinEncounterAcknowledgePreprocessor(OrderJoinEncounterAcknowledge order)
	{
		if (order.EncounterGUID.IsValid)
		{
			BattleEncounter battleEncounter = null;
			if (this.TryGetValue(order.EncounterGUID, out battleEncounter))
			{
				for (int i = 0; i < battleEncounter.BattleContenders.Count; i++)
				{
					BattleContender battleContender = battleEncounter.BattleContenders[i];
					Diagnostics.Assert(battleContender != null);
					if (battleContender.GUID == order.ContenderGUID)
					{
						if (!battleContender.ContenderJoinAcknowledges.Contains(order.SteamIDUser))
						{
							battleContender.ContenderJoinAcknowledges.Add(order.SteamIDUser);
						}
						break;
					}
				}
			}
		}
		return false;
	}

	private bool LockInteractionPreprocessor(OrderLockInteraction order)
	{
		if (order.EmpireIndex < 0 || order.EmpireIndex >= this.Game.Empires.Length)
		{
			return false;
		}
		IGameService service = Services.GetService<IGameService>();
		if (service == null)
		{
			Diagnostics.LogError("Failed to retrieve the game service.");
			return false;
		}
		IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
		if (service2 == null)
		{
			Diagnostics.LogError("Failed to retrieve the game entity repository service.");
			return false;
		}
		for (int i = 0; i < order.TargetsGUID.Length; i++)
		{
			ulong num = order.TargetsGUID[i];
			if (num == 0UL)
			{
				Diagnostics.LogError("OrderLockInteraction: Target's GUID isn't valid");
				return false;
			}
			IGameEntity gameEntity;
			if (!service2.TryGetValue(num, out gameEntity))
			{
				Diagnostics.LogError("OrderLockInteraction: Cannot retrieve target game entity (GUID = '{0}')", new object[]
				{
					num
				});
				return false;
			}
			if (!(gameEntity is PointOfInterest))
			{
				Diagnostics.LogError("OrderLockInteraction: target game entity isn't a point of interest (GUID = '{0}')", new object[]
				{
					num
				});
				return false;
			}
		}
		return true;
	}

	private bool NotifyEmpireDiscoveryPreprocessor(OrderNotifyEmpireDiscovery order)
	{
		if (order.InitiatorEmpireIndex < 0 || order.InitiatorEmpireIndex >= this.Game.Empires.Length)
		{
			Diagnostics.LogWarning("GameServer.NotifyEmpireDiscoveryPreprocessor failed: InitiatorEmpireIndex is invalid");
			return false;
		}
		if (order.DiscoveredEmpireIndex < 0 || order.DiscoveredEmpireIndex >= this.Game.Empires.Length)
		{
			Diagnostics.LogWarning("GameServer.NotifyEmpireDiscoveryPreprocessor failed: InitiatorEmpireIndex is invalid");
			return false;
		}
		global::Empire empire = this.Game.Empires[order.InitiatorEmpireIndex];
		global::Empire empire2 = this.Game.Empires[order.DiscoveredEmpireIndex];
		DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
		Diagnostics.Assert(agency != null);
		DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(empire2);
		return diplomaticRelation != null && diplomaticRelation.State != null && !(diplomaticRelation.State.Name != DiplomaticRelationState.Names.Unknown);
	}

	private bool NotifyEncounterPreprocessor(OrderNotifyEncounter order)
	{
		if (order.EncounterGUID.IsValid)
		{
			BattleEncounter battleEncounter = null;
			if (this.TryGetValue(order.EncounterGUID, out battleEncounter))
			{
				return true;
			}
		}
		return false;
	}

	private bool OrbsChangePreprocessor(OrderOrbsChange order)
	{
		if (order.WorldPositions.Length != order.OrbsQuantity.Length)
		{
			Diagnostics.LogError("world positions and orbs quantity don't have the same length");
			return false;
		}
		return true;
	}

	private bool PacifyMinorFactionPreprocessor(OrderPacifyMinorFaction order)
	{
		if (order.MinorEmpireIndex < 0 || order.MinorEmpireIndex >= this.Game.Empires.Length || order.EmpireIndex < 0 || order.EmpireIndex >= this.Game.Empires.Length)
		{
			return false;
		}
		global::Empire empire = this.Game.Empires[order.MinorEmpireIndex];
		return empire is MinorEmpire;
	}

	private bool PlayBattleActionPreprocessor(OrderPlayBattleAction order)
	{
		if (!order.EncounterGUID.IsValid)
		{
			Diagnostics.LogError("The encounter GUID is invalid.");
			return false;
		}
		BattleEncounter battleEncounter = null;
		if (!this.TryGetValue(order.EncounterGUID, out battleEncounter))
		{
			Diagnostics.LogError("Can't retrieve battle encounter {0:X8}", new object[]
			{
				order.EncounterGUID
			});
			return false;
		}
		BattleContender battleContender = battleEncounter.BattleContenders.Find((BattleContender match) => match.GUID == order.ContenderGUID);
		if (battleContender == null)
		{
			Diagnostics.LogError("Can't retrieve battle contender {0:X8}.", new object[]
			{
				order.ContenderGUID
			});
			return false;
		}
		IDatabase<BattleAction> database = Databases.GetDatabase<BattleAction>(false);
		if (database == null)
		{
			Diagnostics.LogError("Can't retrieve the battle action database.");
			return false;
		}
		BattleAction battleAction;
		if (!database.TryGetValue(order.BattleActionUserName, out battleAction))
		{
			Diagnostics.LogError("Can't retrieve the battle action {0}.", new object[]
			{
				order.BattleActionUserName
			});
			return false;
		}
		BattleActionUser battleActionUser = battleAction as BattleActionUser;
		if (battleActionUser == null)
		{
			Diagnostics.LogError("The battle action {0} is not a user battle action.", new object[]
			{
				order.BattleActionUserName
			});
			return false;
		}
		IDatabase<UnitBodyDefinition> database2 = Databases.GetDatabase<UnitBodyDefinition>(false);
		if (database2 == null)
		{
			Diagnostics.LogError("Can't retrieve the unit body definition database.");
			return false;
		}
		UnitBodyDefinition unitBodyDefinition;
		if (!database2.TryGetValue(order.InitiatorsBodyName, out unitBodyDefinition))
		{
			Diagnostics.LogError("Can't retrieve the unit body definition {0}.", new object[]
			{
				order.InitiatorsBodyName
			});
			return false;
		}
		return battleEncounter.ExecuteBattleAction(battleContender, battleActionUser, unitBodyDefinition);
	}

	private bool QuestWorldEffectPreprocessor(OrderQuestWorldEffect order)
	{
		if (string.IsNullOrEmpty(order.WorldEffectName))
		{
			Diagnostics.LogWarning("GameServer.OrderQuestWorldEffect failed: world effect name is null or empty.");
			return false;
		}
		return true;
	}

	private bool RazePointOfInterestPreprocessor(OrderRazePointOfInterest order)
	{
		IGameEntityRepositoryService service = this.Game.Services.GetService<IGameEntityRepositoryService>();
		if (service == null)
		{
			Diagnostics.LogError("Unable to retrieve the game entity repository service.");
			return false;
		}
		IGameEntity gameEntity;
		if (!service.TryGetValue(order.InstigatorGUID, out gameEntity))
		{
			Diagnostics.LogError("Unable to retrieve the instigator (guid: {0}) from the entity repository service.", new object[]
			{
				order.InstigatorGUID
			});
			return false;
		}
		Army army = gameEntity as Army;
		if (army == null)
		{
			Diagnostics.LogError("Raze instigator is not an army.", new object[]
			{
				order.InstigatorGUID
			});
			return false;
		}
		if (!service.TryGetValue(order.PointOfInterestGUID, out gameEntity))
		{
			Diagnostics.LogError("Unable to retrieve the target point of interest (guid: {0}) from the entity repository service.", new object[]
			{
				order.PointOfInterestGUID
			});
			return false;
		}
		PointOfInterest pointOfInterest = gameEntity as PointOfInterest;
		if (pointOfInterest == null)
		{
			Diagnostics.LogError("Invalid point of interest.");
			return false;
		}
		IPathfindingService service2 = this.Game.Services.GetService<IPathfindingService>();
		if (service2 == null)
		{
			Diagnostics.LogError("Unable to retrieve the pathfinding service.");
			return false;
		}
		if (!service2.IsTransitionPassable(army.WorldPosition, pointOfInterest.WorldPosition, army, PathfindingFlags.IgnorePOI, null))
		{
			Diagnostics.LogError("Army cannot reach point of interest.");
			return false;
		}
		return true;
	}

	private bool ReadyForBattlePreprocessor(OrderReadyForBattle order)
	{
		if (order.EncounterGUID.IsValid)
		{
			BattleEncounter battleEncounter = null;
			if (this.TryGetValue(order.EncounterGUID, out battleEncounter))
			{
				return battleEncounter.SetReadyForBattle(order.ContenderGUID);
			}
		}
		return false;
	}

	private bool ReadyForDeploymentPreprocessor(OrderReadyForDeployment order)
	{
		if (order.EncounterGUID.IsValid)
		{
			BattleEncounter battleEncounter = null;
			if (this.TryGetValue(order.EncounterGUID, out battleEncounter))
			{
				battleEncounter.SetContenderIsRetreating(order.ContenderGUID, order.IsRetreating);
				battleEncounter.SetContenderOptionChoice(order.ContenderGUID, order.ContenderEncounterOptionChoice);
				order.Deployment = battleEncounter.ValidateDeployment(order.ContenderGUID);
				return battleEncounter.SetReadyForDeployment(order.ContenderGUID);
			}
		}
		return false;
	}

	private bool ReadyForNextPhasePreprocessor(OrderReadyForNextPhase order)
	{
		if (order.EncounterGUID.IsValid)
		{
			BattleEncounter battleEncounter = null;
			if (this.TryGetValue(order.EncounterGUID, out battleEncounter))
			{
				return battleEncounter.SetReadyForNextPhase(order.ContenderGUID);
			}
		}
		return false;
	}

	private bool ReadyForNextRoundPreprocessor(OrderReadyForNextRound order)
	{
		if (order.EncounterGUID.IsValid)
		{
			BattleEncounter battleEncounter = null;
			if (this.TryGetValue(order.EncounterGUID, out battleEncounter))
			{
				return battleEncounter.SetReadyForNextRound(order.ContenderGUID);
			}
		}
		return false;
	}

	private bool RefreshMarketplacePreprocessor(OrderRefreshMarketplace order)
	{
		order.Bytes = new byte[0];
		IGameService service = Services.GetService<IGameService>();
		if (service != null && service.Game != null)
		{
			ITradeManagementService service2 = service.Game.Services.GetService<ITradeManagementService>();
			if (service2 != null)
			{
				IBinarySerializable binarySerializable = service2 as IBinarySerializable;
				if (binarySerializable != null)
				{
					using (MemoryStream memoryStream = new MemoryStream())
					{
						using (System.IO.BinaryWriter binaryWriter = new System.IO.BinaryWriter(memoryStream))
						{
							binarySerializable.Serialize(binaryWriter);
							order.Bytes = memoryStream.ToArray();
						}
					}
				}
				return true;
			}
		}
		return false;
	}

	private bool RemoveAffinityStrategicResourcePreprocessor(OrderRemoveAffinityStrategicResource order)
	{
		if (string.IsNullOrEmpty(order.ResourceName))
		{
			Diagnostics.LogError("Resource name is null or empty.");
			return false;
		}
		IDatabase<ResourceDefinition> database = Databases.GetDatabase<ResourceDefinition>(true);
		Diagnostics.Assert(database != null);
		ResourceDefinition resourceDefinition;
		if (!database.TryGetValue(order.ResourceName, out resourceDefinition))
		{
			Diagnostics.LogError("Unable to retrieve the resource '{0}' in the resource database.", new object[]
			{
				order.ResourceName
			});
			return false;
		}
		return true;
	}

	private bool RemoveMapBoostsPreprocessor(OrderRemoveMapBoosts order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		return true;
	}

	private bool ReplicateMarketplaceUnitDesignPreprocessor(OrderReplicateMarketplaceUnitDesign order)
	{
		return true;
	}

	private bool ReplicateMarketplaceUnitProfilePreprocessor(OrderReplicateMarketplaceUnitProfile order)
	{
		return true;
	}

	private bool ReplicateMarketplaceUnitsPreprocessor(OrderReplicateMarketplaceUnits order)
	{
		return true;
	}

	private bool ReportEncounterPreprocessor(OrderReportEncounter order)
	{
		return true;
	}

	private bool ResetPointOfInterestInteractionBitsPreprocessor(OrderResetPointOfInterestInteractionBits order)
	{
		if (order.BitsToRemove == 0)
		{
			Diagnostics.LogError("The interaction bits to remove are invalid.");
			return false;
		}
		return true;
	}

	private bool RunTerraformationForDevicePreprocessor(OrderRunTerraformationForDevice order)
	{
		ITerraformDeviceService service = this.Game.Services.GetService<ITerraformDeviceService>();
		Diagnostics.Assert(service != null);
		TerraformDeviceManager terraformDeviceManager = service as TerraformDeviceManager;
		Diagnostics.Assert(terraformDeviceManager != null);
		TerraformDevice terraformDevice = terraformDeviceManager[order.DeviceGUID] as TerraformDevice;
		if (service == null)
		{
			Diagnostics.LogError("Terraform device does not exist.");
			return false;
		}
		return true;
	}

	private bool SelectAffinityStrategicResourcePreprocessor(OrderSelectAffinityStrategicResource order)
	{
		if (string.IsNullOrEmpty(order.ResourceName))
		{
			Diagnostics.LogError("Resource name is null or empty.");
			return false;
		}
		IDatabase<ResourceDefinition> database = Databases.GetDatabase<ResourceDefinition>(true);
		Diagnostics.Assert(database != null);
		ResourceDefinition resourceDefinition;
		if (!database.TryGetValue(order.ResourceName, out resourceDefinition))
		{
			Diagnostics.LogError("Unable to retrieve the resource '{0}' in the resource database.", new object[]
			{
				order.ResourceName
			});
			return false;
		}
		return true;
	}

	private bool SendAIAttitudeFeedbackPreprocessor(OrderSendAIAttitudeFeedback order)
	{
		return true;
	}

	private bool SetDeploymentFinishedPreprocessor(OrderSetDeploymentFinished order)
	{
		if (order.EncounterGUID.IsValid)
		{
			BattleEncounter battleEncounter = null;
			if (this.TryGetValue(order.EncounterGUID, out battleEncounter))
			{
				return battleEncounter.SetDeploymentFinished(order.ContenderGUID);
			}
		}
		return false;
	}

	private bool SetEncounterDeployementEndTimePreprocessor(OrderSetEncounterDeployementEndTime order)
	{
		if (order.EncounterGUID.IsValid)
		{
			BattleEncounter battleEncounter = null;
			if (this.TryGetValue(order.EncounterGUID, out battleEncounter))
			{
				return true;
			}
		}
		return false;
	}

	private bool SetMapBoostSpawnPreprocessor(OrderSetMapBoostSpawn order)
	{
		return true;
	}

	private bool SetOrbSpawnPreprocessor(OrderSetOrbSpawn order)
	{
		return true;
	}

	private bool SetWindPreferencesPreprocessor(OrderSetWindPreferences order)
	{
		if (order.WindDirection < 0)
		{
			Diagnostics.LogError("GameServer.SetWindPreferencesPreprocessor - wind direction to apply can't be negatif.");
			return false;
		}
		if (order.WindStrength < 0)
		{
			Diagnostics.LogError("GameServer.SetWindPreferencesPreprocessor - wind strength to apply can't be negatif.");
			return false;
		}
		return true;
	}

	private bool SpawnMapBoostsPreprocessor(OrderSpawnMapBoosts order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		IGameEntityRepositoryService service = this.Game.Services.GetService<IGameEntityRepositoryService>();
		if (service == null)
		{
			Diagnostics.LogError("Cannot retreive the gameEntityRepositoryService.");
			return false;
		}
		for (int i = 0; i < order.WorldPositions.Length; i++)
		{
			if (order.MapBoostsGUIDs[i] == GameEntityGUID.Zero)
			{
				order.MapBoostsGUIDs[i] = service.GenerateGUID();
			}
		}
		return true;
	}

	private bool SwapUnitDeploymentPreprocessor(OrderSwapUnitDeployment order)
	{
		if (!order.EncounterGUID.IsValid)
		{
			return false;
		}
		BattleEncounter battleEncounter = null;
		if (!this.TryGetValue(order.EncounterGUID, out battleEncounter))
		{
			return false;
		}
		BattleContender battleContender = battleEncounter.BattleContenders.Find((BattleContender match) => match.GUID == order.ContenderGUID);
		if (battleContender == null)
		{
			return false;
		}
		UnitDeployment[] unitDeployment = battleContender.Deployment.UnitDeployment;
		WorldPosition worldPosition = WorldPosition.Invalid;
		WorldPosition worldPosition2 = WorldPosition.Invalid;
		foreach (UnitDeployment unitDeployment2 in unitDeployment)
		{
			if (unitDeployment2.UnitGUID == order.Unit1GUID)
			{
				worldPosition = unitDeployment2.WorldPosition;
			}
			else if (unitDeployment2.UnitGUID == order.Unit2GUID)
			{
				worldPosition2 = unitDeployment2.WorldPosition;
			}
		}
		if (!worldPosition.IsValid || !worldPosition2.IsValid)
		{
			Diagnostics.LogError("Can't swap unit deployment positions because one of the position is invalid (unit1 position: {0}, unit2 position: {1}).", new object[]
			{
				worldPosition,
				worldPosition2
			});
			return false;
		}
		order.FinalUnit1Position = worldPosition2;
		order.FinalUnit2Position = worldPosition;
		if (!order.FinalUnit1Position.IsValid || !order.FinalUnit2Position.IsValid)
		{
			Diagnostics.LogError("Can't swap unit deployment positions because one of the position is invalid (unit1 position: {0}, unit2 position: {1}).", new object[]
			{
				order.FinalUnit1Position,
				order.FinalUnit2Position
			});
			return false;
		}
		IEncounterRepositoryService service = this.Game.GetService<IEncounterRepositoryService>();
		if (service == null)
		{
			Diagnostics.LogError("Can't swap unit deployment positions because it's impossible to retrieve encounter repository service.");
			return false;
		}
		Encounter encounter = null;
		if (!service.TryGetValue(order.EncounterGUID, out encounter))
		{
			Diagnostics.LogError("Can't swap unit deployment positions because it's impossible to retrieve encounter {0}.", new object[]
			{
				order.EncounterGUID
			});
			return false;
		}
		Contender contender = encounter.Contenders.Find((Contender match) => match.GUID == order.ContenderGUID);
		if (contender == null || contender.EncounterUnits == null)
		{
			Diagnostics.LogError("Can't swap unit deployment positions because it's impossible to retrieve contender {0}.", new object[]
			{
				order.ContenderGUID
			});
			return false;
		}
		EncounterUnit encounterUnit = contender.EncounterUnits.Find((EncounterUnit match) => match.Unit.GUID == order.Unit1GUID);
		EncounterUnit encounterUnit2 = contender.EncounterUnits.Find((EncounterUnit match) => match.Unit.GUID == order.Unit2GUID);
		IPathfindingService service2 = this.Game.GetService<IPathfindingService>();
		if (service2 == null)
		{
			Diagnostics.LogError("Can't swap unit deployment positions because it's impossible to retrieve pathfinding service.");
			return false;
		}
		IPathfindingArea battleZone = battleEncounter.BattleController.BattleSimulation.BattleZone;
		PathfindingResult pathfindingResult = service2.FindPath(encounterUnit.Unit, worldPosition, order.FinalUnit1Position, PathfindingManager.RequestMode.Default, new PathfindingWorldContext(battleZone, null), PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreEncounterAreas | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges, null);
		PathfindingResult pathfindingResult2 = service2.FindPath(encounterUnit2.Unit, worldPosition2, order.FinalUnit2Position, PathfindingManager.RequestMode.Default, new PathfindingWorldContext(battleZone, null), PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreEncounterAreas | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges, null);
		if (pathfindingResult == null || pathfindingResult2 == null)
		{
			Diagnostics.LogError("Can't swap unit deployment positions because one of the unit can't found a path the its destination.");
			return false;
		}
		foreach (UnitDeployment unitDeployment3 in unitDeployment)
		{
			if (unitDeployment3.UnitGUID == order.Unit1GUID)
			{
				unitDeployment3.WorldPosition = order.FinalUnit1Position;
			}
			else if (unitDeployment3.UnitGUID == order.Unit2GUID)
			{
				unitDeployment3.WorldPosition = order.FinalUnit2Position;
			}
		}
		return true;
	}

	private bool SwitchContendersReinforcementRankingPreprocessor(OrderSwitchContendersReinforcementRanking order)
	{
		if (!order.EncounterGUID.IsValid)
		{
			Diagnostics.LogError("The encounter GUID is invalid.");
			return false;
		}
		BattleEncounter battleEncounter = null;
		if (!this.TryGetValue(order.EncounterGUID, out battleEncounter))
		{
			Diagnostics.LogError("Can't retrieve battle encounter {0:X8}", new object[]
			{
				order.EncounterGUID
			});
			return false;
		}
		return battleEncounter.SwitchContendersReinforcementRanking(order.FirstContenderGUID, order.SecondContenderGUID);
	}

	private bool ToggleEndlessDayPreprocessor(OrderToggleEndlessDay order)
	{
		return true;
	}

	private bool UpdateWinterImmunityBidsPreprocessor(OrderUpdateWinterImmunityBids order)
	{
		return true;
	}

	private bool VoteForSeasonEffectPreprocessor(OrderVoteForSeasonEffect order)
	{
		ISeasonService service = this.Game.GetService<ISeasonService>();
		if (service == null)
		{
			Diagnostics.LogError("Error Impossible to get SeasonService.");
			return false;
		}
		List<SeasonEffect> candidateEffectsForSeasonType = service.GetCandidateEffectsForSeasonType(Season.ReadOnlyWinter);
		if (candidateEffectsForSeasonType == null || candidateEffectsForSeasonType.Count <= 0)
		{
			Diagnostics.LogError("Error SeasonEffect preselection is null or empty.");
			return false;
		}
		if (candidateEffectsForSeasonType.Find((SeasonEffect seasonEffect) => seasonEffect.SeasonEffectDefinition.Name == order.SeasonEffectName) == null)
		{
			Diagnostics.LogError("Error Impossible to find SeasonEffect : " + order.SeasonEffectName);
			return false;
		}
		DepartmentOfTheInterior agency = this.Game.Empires[order.EmpireIndex].GetAgency<DepartmentOfTheInterior>();
		if (agency == null)
		{
			Diagnostics.LogError("Error fail getting DepartmentOfTheInterior");
			return false;
		}
		bool flag = false;
		foreach (City city in agency.Cities)
		{
			int i = 0;
			int count = city.Districts.Count;
			while (i < count)
			{
				if (city.Districts[i].SimulationObject.Tags.Contains(GameAltarOfAurigaScreen.DistrictImprovementAltarOfAurigaTagName))
				{
					flag = true;
				}
				i++;
			}
		}
		if (!flag)
		{
			return false;
		}
		order.VoteCost = -(service.ComputePrayerOrbCost(this.Game.Empires[order.EmpireIndex]) * (float)order.VoteCount);
		DepartmentOfTheTreasury agency2 = this.Game.Empires[order.EmpireIndex].GetAgency<DepartmentOfTheTreasury>();
		if (agency2 == null || !agency2.CanAfford(order.VoteCost, DepartmentOfTheTreasury.Resources.Orb))
		{
			Diagnostics.LogError("Can't afford vote price");
			return false;
		}
		return true;
	}

	private bool WinterImmunityBidPreprocessor(OrderWinterImmunityBid order)
	{
		float num = (float)order.Value * -1f;
		DepartmentOfTheTreasury agency = this.Game.Empires[order.EmpireIndex].GetAgency<DepartmentOfTheTreasury>();
		if (agency == null || !agency.IsTransferOfResourcePossible(this.Game.Empires[order.EmpireIndex], DepartmentOfTheTreasury.Resources.Orb, ref num))
		{
			Diagnostics.Log("Error Impossible to get DeparmtentOfTreasury for empire " + order.EmpireIndex + " or impossible to transfer resource");
			return false;
		}
		return true;
	}

	~GameServer()
	{
		this.Dispose(false);
		Debug.Log("Game server has been deleted.");
	}

	public AIScheduler AIScheduler { get; private set; }

	public Dictionary<ulong, GameClientConnection> GameClientConnections { get; private set; }

	public bool HasPendingOrder
	{
		get
		{
			return this.PendingOrder != null;
		}
	}

	public bool HasPendingOrders
	{
		get
		{
			return this.PendingOrders.Count > 0 || this.PendingOrder != null;
		}
	}

	public bool IsPublic
	{
		get
		{
			return base.Session.SessionMode == SessionMode.Public || base.Session.SessionMode == SessionMode.Protected;
		}
	}

	public bool GameInProgress
	{
		get
		{
			return this.gameInProgress;
		}
		set
		{
			this.gameInProgress = value;
			base.Session.SetLobbyData("_GameInProgress", value, true);
		}
	}

	public Type PendingOrderType
	{
		get
		{
			return this.PendingOrder.GetType();
		}
	}

	public GameSaveDescriptor LastAutoGameSaveDescriptor { get; set; }

	private Amplitude.Unity.Game.Orders.Order PendingOrder { get; set; }

	private Queue<GameClientPostOrderMessage> PendingOrders { get; set; }

	public void OnSteamGameServerCreated()
	{
		this.steamGameServer = Steamworks.SteamAPI.SteamGameServer;
		this.steamNetworkingProxy = new SteamNetworkingProxy(Steamworks.SteamAPI.SteamGameServerNetworking, true);
		base.MessageBox = new MessageBox(this.steamNetworkingProxy);
		this.steamServersService = Services.GetService<ISteamServerService>();
		this.steamServersService.ServerP2PSessionRequest += this.ISteamServersService_P2PSessionRequest;
		this.steamServersService.ServerP2PSessionConnectFail += this.ISteamServersService_P2PSessionConnectFail;
		this.steamServersService.ValidateAuthTicketResponse += this.ISteamServersService_ValidateAuthTicketResponse;
		this.steamServersService.ServerSteamServerConnectFailure += this.ISteamServersService_SteamServerConnectFailure;
		this.synchronizationService = Services.GetService<ISynchronizationService>();
		Diagnostics.Assert(this.synchronizationService != null);
		this.synchronizationService.SynchronizationStateChanged += this.ISynchronizationService_SynchronizationStateChanged;
		IGameDiagnosticsService service = Services.GetService<IGameDiagnosticsService>();
		Diagnostics.Assert(service != null);
		service.InjectTestDesync = false;
	}

	public void InitializeAIScheduler()
	{
		if (this.AIScheduler != null)
		{
			this.ReleaseAIScheduler();
		}
		this.AIScheduler = new AIScheduler();
		this.AIScheduler.IsThreaded = global::Application.FantasyPreferences.EnableRunAIsOnThreads;
	}

	public void ReleaseAIScheduler()
	{
		if (this.AIScheduler == null)
		{
			return;
		}
		this.AIScheduler.Release();
		this.AIScheduler = null;
	}

	public void SendMessageToClients(ref Message message, bool ignorePendingConnection)
	{
		if (base.MessageBox != null)
		{
			Steamworks.SteamID[] array = new Steamworks.SteamID[this.GameClientConnections.Count];
			int num = 0;
			foreach (KeyValuePair<ulong, GameClientConnection> keyValuePair in this.GameClientConnections)
			{
				if (keyValuePair.Value.GameClientConnectionState != GameClientConnectionState.DisconnectedFromServer)
				{
					if (!ignorePendingConnection || (keyValuePair.Value.GameClientConnectionFlags & GameClientConnectionFlags.PendingConnection) == GameClientConnectionFlags.Zero)
					{
						array[num++] = keyValuePair.Value.SteamIDRemote;
					}
				}
			}
			base.MessageBox.SendMessage(ref message, array);
		}
	}

	public void SendMessageToClients(ref Message message)
	{
		if (base.MessageBox != null)
		{
			Steamworks.SteamID[] array = new Steamworks.SteamID[this.GameClientConnections.Count];
			int num = 0;
			foreach (KeyValuePair<ulong, GameClientConnection> keyValuePair in this.GameClientConnections)
			{
				if (keyValuePair.Value.GameClientConnectionState != GameClientConnectionState.DisconnectedFromServer)
				{
					array[num++] = keyValuePair.Value.SteamIDRemote;
				}
			}
			base.MessageBox.SendMessage(ref message, array);
		}
	}

	public override void SendMessageToServer(ref Message message)
	{
		if (base.MessageBox != null)
		{
			base.MessageBox.PushMessage(ref message, this.SteamIDUser);
		}
	}

	public void Disconnect()
	{
		if (this.steamGameServer == null)
		{
			Diagnostics.Log("[GameServer][Net] SteamGameServer is null. Forcing shutdown anyway...");
			Steamworks.SteamGameServer.Shutdown();
			return;
		}
		Diagnostics.Log("[GameServer][Net] Disconnecting...");
		if (base.MessageBox != null)
		{
			Message message = new GameServerLeaveMessage();
			this.SendMessageToClients(ref message);
			Diagnostics.Log("[GameServer][Net] Leave message sent.");
		}
		foreach (KeyValuePair<ulong, GameClientConnection> keyValuePair in this.GameClientConnections)
		{
			if (keyValuePair.Key != this.SteamIDUser)
			{
				switch (keyValuePair.Value.GameClientConnectionState)
				{
				case GameClientConnectionState.ConnectedToServer:
				case GameClientConnectionState.ConnectingToServer:
				case GameClientConnectionState.AuthenticatingToServer:
				case GameClientConnectionState.AuthenticatedToServer:
				case GameClientConnectionState.AuthenticationHasFailed:
					this.RemoveUserFromServer(keyValuePair.Key, GameDisconnectionReason.HostLeft);
					break;
				case GameClientConnectionState.ConnectionToServerHasFailed:
				case GameClientConnectionState.ConnectionToServerHasTimedOut:
				case GameClientConnectionState.DisconnectedFromServer:
					break;
				default:
					throw new ArgumentOutOfRangeException();
				}
			}
		}
		this.GameClientConnections.Clear();
		Diagnostics.Log("[GameServer][Net] Logging off SteamGameServer.");
		this.steamGameServer.EnableHeartbeats(false);
		this.steamGameServer.LogOff();
		Diagnostics.Log("[GameServer][Net] Shutting down SteamGameServer.");
		Steamworks.SteamGameServer.Shutdown();
		this.steamGameServer = null;
		ISteamMatchMakingService service = Services.GetService<ISteamMatchMakingService>();
		service.SteamMatchMaking.SetLobbyGameServer(base.Session.SteamIDLobby, 0u, 0, Steamworks.SteamID.Zero);
		Diagnostics.Log("[GameServer][Net] Disconnected.");
	}

	public override void Update()
	{
		base.Update();
		if (this.saveGameCoroutineQueue.Count > 0)
		{
			this.saveGameCoroutineQueue.Peek().Run();
			if (this.saveGameCoroutineQueue.Peek().IsFinished)
			{
				this.saveGameCoroutineQueue.Dequeue();
			}
		}
	}

	internal void BindMajorEmpires()
	{
		foreach (global::Empire empire in from e in this.Game.Empires
		where e is MajorEmpire
		select e)
		{
			MajorEmpire majorEmpire = (MajorEmpire)empire;
			majorEmpire.OnPlayerBond += this.MajorEmpire_OnEmpireBond;
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (!this.disposed)
		{
			if (disposing)
			{
				this.Disconnect();
				if (this.steamServersService != null)
				{
					this.steamServersService.ServerP2PSessionRequest -= this.ISteamServersService_P2PSessionRequest;
					this.steamServersService.ServerP2PSessionConnectFail -= this.ISteamServersService_P2PSessionConnectFail;
					this.steamServersService.ValidateAuthTicketResponse -= this.ISteamServersService_ValidateAuthTicketResponse;
					this.steamServersService.ServerSteamServerConnectFailure -= this.ISteamServersService_SteamServerConnectFailure;
				}
				if (this.synchronizationService != null)
				{
					this.synchronizationService.SynchronizationStateChanged -= this.ISynchronizationService_SynchronizationStateChanged;
				}
				if (this.Game != null)
				{
					foreach (global::Empire empire in from e in this.Game.Empires
					where e is MajorEmpire
					select e)
					{
						MajorEmpire majorEmpire = (MajorEmpire)empire;
						if (majorEmpire != null)
						{
							majorEmpire.OnPlayerBond -= this.MajorEmpire_OnEmpireBond;
						}
					}
				}
				if (base.Session != null)
				{
					base.Session.LobbyDataChange -= this.Session_LobbyDataChange;
				}
				this.ReleaseAIScheduler();
				this.PendingOrders.Clear();
				this.PendingOrder = null;
			}
			this.disposed = true;
		}
		base.Dispose(disposing);
	}

	protected override void GameService_GameChange(object sender, GameChangeEventArgs e)
	{
		base.GameService_GameChange(sender, e);
		switch (e.Action)
		{
		case GameChangeAction.Created:
			if (base.Session.SessionMode != SessionMode.Single)
			{
				base.Session.LobbyDataChange += this.Session_LobbyDataChange;
			}
			this.playerRepository = this.Game.GetService<IPlayerRepositoryService>();
			Diagnostics.Assert(this.playerRepository != null);
			this.victoryManagement = this.Game.GetService<IVictoryManagementService>();
			Diagnostics.Assert(this.victoryManagement != null);
			this.victoryManagement.VictoryConditionRaised += this.IVictoryManagementService_VictoryConditionRaised;
			break;
		case GameChangeAction.Releasing:
		case GameChangeAction.Released:
			if (base.Session != null)
			{
				base.Session.LobbyDataChange -= this.Session_LobbyDataChange;
			}
			if (this.victoryManagement != null)
			{
				this.victoryManagement.VictoryConditionRaised -= this.IVictoryManagementService_VictoryConditionRaised;
				this.victoryManagement = null;
			}
			break;
		}
	}

	protected override void ProcessMessage(ref IMessage message, ref Steamworks.SteamID steamIDRemote)
	{
		short id = message.ID;
		switch (id)
		{
		case 2101:
			Diagnostics.Assert(message is GameClientPostOrderMessage, "Invalid message type. We expect a GameClienPostOrderMessage.");
			this.PendingOrders.Enqueue(message as GameClientPostOrderMessage);
			break;
		case 2102:
		{
			if (steamIDRemote == this.SteamIDUser)
			{
				base.OnTicketRaised(this.PendingOrder.TicketNumber, PostOrderResponse.Processed, this.PendingOrder);
				this.PendingOrder = null;
			}
			GameClientConnection gameClientConnection;
			if (this.GameClientConnections.TryGetValue(steamIDRemote, out gameClientConnection))
			{
				GameClientPostOrderResponseMessage gameClientPostOrderResponseMessage = message as GameClientPostOrderResponseMessage;
				Diagnostics.Assert(gameClientConnection.LastAcknowledgedOrderSerial == 0UL || gameClientConnection.LastAcknowledgedOrderSerial == gameClientPostOrderResponseMessage.OrderSerial - 1UL, string.Format("[Net] Missing order(s) detected between serials {0} and {1}", gameClientConnection.LastAcknowledgedOrderSerial, gameClientPostOrderResponseMessage.OrderSerial));
				gameClientConnection.LastAcknowledgedOrderSerial = gameClientPostOrderResponseMessage.OrderSerial;
			}
			break;
		}
		case 2103:
		{
			GameClientConnection gameClientConnection2;
			if (this.GameClientConnections.TryGetValue(steamIDRemote, out gameClientConnection2))
			{
				gameClientConnection2.GameClientState = (message as GameClientStateMessage).AssemblyQualifiedName;
				string name = "%DefaultPlayerName";
				if (Steamworks.SteamAPI.IsSteamRunning)
				{
					name = Steamworks.SteamAPI.SteamFriends.GetFriendPersonaName(steamIDRemote);
				}
				Message message2 = new GameServerPlayerUpdateMessage(steamIDRemote, (message as GameClientStateMessage).AssemblyQualifiedName, GameServerPlayerUpdateMessage.PlayerAction.StateUpdate)
				{
					Name = name
				};
				this.SendMessageToClients(ref message2);
			}
			break;
		}
		default:
			if (id != 2001)
			{
				if (id != 2002)
				{
					if (id != 2901)
					{
						if (id != 2902)
						{
							if (id != 2010)
							{
								if (id != 2201)
								{
									if (id != 2301)
									{
										if (id == 2990)
										{
											GameClientPingMessage gameClientPingMessage = message as GameClientPingMessage;
											if (gameClientPingMessage != null)
											{
												double halvedServerDelta = (double)(Mathf.Max(Time.fixedDeltaTime, Time.deltaTime) * 0.5f);
												GameServerPingMessage gameServerPingMessage = new GameServerPingMessage(gameClientPingMessage.SessionTime, halvedServerDelta);
												IPlayerRepositoryService service = this.Game.GetService<IPlayerRepositoryService>();
												Diagnostics.Assert(service != null);
												Player playerBySteamID = service.GetPlayerBySteamID(steamIDRemote);
												if (playerBySteamID != null)
												{
													playerBySteamID.Latency = gameClientPingMessage.LastLatency;
												}
												foreach (Player player in service)
												{
													if (player.Type == PlayerType.Human)
													{
														gameServerPingMessage.PlayersSteamIDs.Add(player.SteamID);
														gameServerPingMessage.PlayersLatencies.Add(player.Latency);
													}
												}
												Message message3 = gameServerPingMessage;
												base.SendMessage(ref message3, new Steamworks.SteamID[]
												{
													steamIDRemote
												});
											}
										}
									}
									else
									{
										Diagnostics.Log("[Net] [GameServer] Download game request received from: '{0}'.", new object[]
										{
											steamIDRemote.ToString()
										});
										GameClientDownloadGameMessage gameClientDownloadGameMessage = message as GameClientDownloadGameMessage;
										if (gameClientDownloadGameMessage != null)
										{
											switch (gameClientDownloadGameMessage.RequestedSaveType)
											{
											case SaveType.InitialGameState:
											{
												GameClientConnection gameClientConnection3;
												if (this.GameClientConnections.TryGetValue(steamIDRemote, out gameClientConnection3))
												{
													gameClientConnection3.GameClientConnectionFlags |= GameClientConnectionFlags.DownloadGameRequestReceived;
													gameClientConnection3.GameClientConnectionFlags |= GameClientConnectionFlags.DownloadGameRequestAcknowledged;
													Message message4 = new GameServerDownloadGameResponseMessage();
													base.MessageBox.SendMessage(ref message4, new Steamworks.SteamID[]
													{
														steamIDRemote
													});
													Diagnostics.Log("[Net] [GameServer] Download game request received from: '{0}', and acknowledged.", new object[]
													{
														steamIDRemote.ToString()
													});
												}
												break;
											}
											case SaveType.LastAutoSave:
												if (this.LastAutoGameSaveDescriptor == null)
												{
													Diagnostics.LogError("[Net] Client {0} requested the last auto save, which is null.", new object[]
													{
														steamIDRemote
													});
												}
												else
												{
													Message message5 = new GameServerDownloadGameResponseMessage(this.LastAutoGameSaveDescriptor, gameClientDownloadGameMessage.RequestedSaveType, gameClientDownloadGameMessage.Title);
													base.SendMessage(ref message5, new Steamworks.SteamID[]
													{
														steamIDRemote
													});
													Diagnostics.Log("[Net] Download Game Request from {0} has been responded to (immediate-mode).", new object[]
													{
														steamIDRemote
													});
												}
												break;
											case SaveType.OnUserRequest:
												this.saveGameCoroutineQueue.Enqueue(Amplitude.Coroutine.StartCoroutine(this.SaveGameOnUserDemandAsync(gameClientDownloadGameMessage.Title, steamIDRemote), null));
												break;
											default:
												throw new ArgumentOutOfRangeException();
											}
										}
									}
								}
								else
								{
									GameClientChatMessage gameClientChatMessage = message as GameClientChatMessage;
									if (gameClientChatMessage != null)
									{
										this.ProcessChatMessage(gameClientChatMessage, steamIDRemote);
									}
								}
							}
							else
							{
								GameClientAuthTicketMessage gameClientAuthTicketMessage = message as GameClientAuthTicketMessage;
								Diagnostics.Assert(gameClientAuthTicketMessage != null, "Invalid message type. Expected GameClientAuthTicketMessage.");
								Diagnostics.Log("[Net] [GameServer,Auth] Received auth ticket from: '{0}', with length: {1}, ticket = {2}.", new object[]
								{
									steamIDRemote.ToString(),
									gameClientAuthTicketMessage.Length,
									GameClientState_Authentication.TicketToString(gameClientAuthTicketMessage.Ticket, gameClientAuthTicketMessage.Length)
								});
								Steamworks.SteamGameServer steamGameServer = Steamworks.SteamAPI.SteamGameServer;
								byte[] ticket = gameClientAuthTicketMessage.Ticket;
								Steamworks.EBeginAuthSessionResult ebeginAuthSessionResult = steamGameServer.BeginAuthSession(ref ticket, gameClientAuthTicketMessage.Length, steamIDRemote);
								switch (ebeginAuthSessionResult)
								{
								case Steamworks.EBeginAuthSessionResult.k_EBeginAuthSessionResultOK:
									Diagnostics.Log("[Net] [GameServer,Auth] BeginAuthSession succeeded for member: '{0}'.", new object[]
									{
										steamIDRemote.ToString()
									});
									break;
								case Steamworks.EBeginAuthSessionResult.k_EBeginAuthSessionResultInvalidTicket:
								case Steamworks.EBeginAuthSessionResult.k_EBeginAuthSessionResultDuplicateRequest:
								case Steamworks.EBeginAuthSessionResult.k_EBeginAuthSessionResultInvalidVersion:
								case Steamworks.EBeginAuthSessionResult.k_EBeginAuthSessionResultGameMismatch:
								case Steamworks.EBeginAuthSessionResult.k_EBeginAuthSessionResultExpiredTicket:
									Diagnostics.LogError("[Net] [GameServer,Auth] BeginAuthSession failed with error: 0x{0:x4}, for member '{1}'.", new object[]
									{
										(int)ebeginAuthSessionResult,
										steamIDRemote.ToString()
									});
									break;
								default:
									throw new ArgumentOutOfRangeException();
								}
							}
						}
						else
						{
							GameClientDownloadDumpResponseMessage gameClientDownloadDumpResponseMessage = message as GameClientDownloadDumpResponseMessage;
							if (gameClientDownloadDumpResponseMessage != null)
							{
								Diagnostics.Log("[Dump] [GameServer] Dump download response received: '{0}'.", new object[]
								{
									gameClientDownloadDumpResponseMessage.Filename
								});
								this.SaveUserDump(gameClientDownloadDumpResponseMessage.Filename, gameClientDownloadDumpResponseMessage.UncompressedLength, gameClientDownloadDumpResponseMessage.Buffer);
							}
						}
					}
					else
					{
						this.ProcessClientDiagnisticsInfoMessage(message as GameClientDiagnosticsInfoMessage, steamIDRemote);
					}
				}
				else
				{
					Diagnostics.Log("[Net] [GameServer] User '{0}' has left the game.", new object[]
					{
						steamIDRemote.ToString()
					});
					this.RemoveUserFromServer(steamIDRemote, GameDisconnectionReason.ClientLeft);
				}
			}
			else
			{
				Diagnostics.Log("[Net] [GameServer] Initiate connection to server with steamIDRemote: '{0}', steamIDUser: '{1}'.", new object[]
				{
					steamIDRemote.ToString(),
					this.SteamIDUser.ToString()
				});
				GameClientConnection gameClientConnection4;
				if (!this.GameClientConnections.TryGetValue(steamIDRemote, out gameClientConnection4))
				{
					gameClientConnection4 = new GameClientConnection
					{
						GameClientConnectionFlags = GameClientConnectionFlags.Zero,
						GameClientConnectionState = GameClientConnectionState.ConnectedToServer,
						GameClientState = string.Empty,
						SteamIDRemote = steamIDRemote
					};
					this.GameClientConnections.Add(steamIDRemote, gameClientConnection4);
					if (steamIDRemote == this.SteamIDUser)
					{
						GameClient gameClient = base.Session.GameClient as GameClient;
						if (gameClient == null)
						{
							Diagnostics.LogError("The game client is null.");
							break;
						}
						base.MessageBox.Connect(steamIDRemote, gameClient.MessageBox.MessagePipe);
						gameClientConnection4.GameClientConnectionState = GameClientConnectionState.AuthenticatedToServer;
					}
				}
				else
				{
					gameClientConnection4.GameClientConnectionFlags = GameClientConnectionFlags.Zero;
					gameClientConnection4.GameClientConnectionState = GameClientConnectionState.ConnectedToServer;
					gameClientConnection4.GameClientState = string.Empty;
					gameClientConnection4.LastAcknowledgedOrderSerial = 0UL;
				}
				if (this.GameInProgress)
				{
					Diagnostics.Log("[Net][GameServer] client {0} is flagged as PendingConnection.", new object[]
					{
						steamIDRemote
					});
					gameClientConnection4.GameClientConnectionFlags |= GameClientConnectionFlags.PendingConnection;
					base.Session.SetLobbyData("_PlayerIsTransition", true, true);
					string newValue = string.Empty;
					if (Steamworks.SteamAPI.IsSteamRunning)
					{
						newValue = Steamworks.SteamAPI.SteamFriends.GetFriendPersonaName(steamIDRemote);
					}
					string text = AgeLocalizer.Instance.LocalizeString("%PlayerWaitingTransition").Replace("$Name", newValue);
					Message message6 = new GameServerChatMessage(ChatMessageType.Server, global::Session.Time, this.SteamIDUser, null, text);
					this.SendMessageToClients(ref message6);
				}
				Message message7 = new GameServerInitiateConnectionResponseMessage(this.GameInProgress);
				base.MessageBox.SendMessage(ref message7, new Steamworks.SteamID[]
				{
					steamIDRemote
				});
				foreach (GameClientConnection gameClientConnection5 in this.GameClientConnections.Values)
				{
					Message message8 = new GameServerPlayerUpdateMessage(gameClientConnection5.SteamIDRemote, gameClientConnection5.GameClientState, GameServerPlayerUpdateMessage.PlayerAction.StateUpdate);
					this.SendMessageToClients(ref message8);
				}
			}
			break;
		}
	}

	protected override void ProcessOrders()
	{
		if (this.PendingOrder == null)
		{
			while (this.PendingOrders.Count > 0)
			{
				GameClientPostOrderMessage gameClientPostOrderMessage = this.PendingOrders.Dequeue();
				Amplitude.Unity.Game.Orders.Order order = gameClientPostOrderMessage.Order;
				if (order != null)
				{
					bool flag = true;
					PostOrderResponse postOrderResponse;
					if (flag)
					{
						bool flag2 = true;
						OrderProcessorInfo orderProcessorInfo;
						if (order is global::Order)
						{
							global::Empire empire = this.Game.Empires[((global::Order)order).EmpireIndex];
							try
							{
								flag2 = empire.PreprocessOrder(order);
							}
							catch (Exception ex)
							{
								Diagnostics.LogError("An exception has been thrown during the preprocessor: Exception:\n {0}.", new object[]
								{
									ex.ToString()
								});
								flag2 = false;
							}
						}
						else if (this.orderProcessorInfoByType.TryGetValue(order.GetType(), out orderProcessorInfo))
						{
							flag2 = orderProcessorInfo.Preprocess(order);
						}
						if (flag2)
						{
							this.PendingOrder = order;
							Amplitude.Unity.Game.Orders.Order pendingOrder = this.PendingOrder;
							ulong serial;
							this.nextPendingOrderSerial = (serial = this.nextPendingOrderSerial) + 1UL;
							pendingOrder.Serial = serial;
							Message message = new GameServerPostOrderMessage(this.PendingOrder);
							this.SendMessageToClients(ref message, true);
							break;
						}
						postOrderResponse = PostOrderResponse.PreprocessHasFailed;
					}
					else
					{
						postOrderResponse = PostOrderResponse.AuthenticationHasFailed;
					}
					if (order.TicketNumber != 0UL)
					{
						Message message2 = new GameServerPostOrderResponseMessage(order, postOrderResponse);
						this.SendMessageToClients(ref message2, true);
						base.OnTicketRaised(order.TicketNumber, postOrderResponse, order);
					}
				}
			}
		}
	}

	private IEnumerator SaveGameOnUserDemandAsync(string title, Steamworks.SteamID steamIDUser)
	{
		IGameSerializationService gameSerializationService = Services.GetService<IGameSerializationService>();
		Diagnostics.Assert(gameSerializationService != null);
		string tmpFile = System.IO.Path.Combine(Amplitude.Unity.Framework.Application.TempDirectory, string.Format("{0}-{1}.sav", "TempSave", steamIDUser));
		yield return Amplitude.Coroutine.StartCoroutine(gameSerializationService.SaveGameAsync(title, tmpFile, GameSaveOptions.None), null);
		Message response = new GameServerDownloadGameResponseMessage(gameSerializationService.GameSaveDescriptor, SaveType.OnUserRequest, title);
		base.SendMessage(ref response, new Steamworks.SteamID[]
		{
			steamIDUser
		});
		Diagnostics.Log("[Net] Download Game Request from {0} has been responded to (queued-mode).", new object[]
		{
			steamIDUser
		});
		yield break;
	}

	private void ProcessChatMessage(GameClientChatMessage clientChatMessage, Steamworks.SteamID steamIDRemote)
	{
		GameServerChatMessage gameServerChatMessage = new GameServerChatMessage();
		gameServerChatMessage.Message.Type = clientChatMessage.Type;
		gameServerChatMessage.Message.Time = global::Session.Time;
		gameServerChatMessage.Message.From = steamIDRemote;
		gameServerChatMessage.Message.Text = clientChatMessage.Command;
		gameServerChatMessage.Message.To = clientChatMessage.Recipent;
		Message message = gameServerChatMessage;
		switch (gameServerChatMessage.Message.Type)
		{
		case ChatMessageType.Global:
		case ChatMessageType.Server:
			this.SendMessageToClients(ref message, true);
			break;
		case ChatMessageType.Group:
			throw new NotImplementedException();
		case ChatMessageType.Empire:
		{
			Diagnostics.Assert(gameServerChatMessage.Message.From != null);
			Diagnostics.Assert(gameServerChatMessage.Message.To != null);
			MajorEmpire majorEmpire = this.playerRepository.GetPlayerBySteamID(gameServerChatMessage.Message.From).Empire as MajorEmpire;
			MajorEmpire majorEmpire2 = this.playerRepository.GetPlayerBySteamID(gameServerChatMessage.Message.To).Empire as MajorEmpire;
			Steamworks.SteamID[] steamIDRemotes = (from p in majorEmpire.Players.Union(majorEmpire2.Players)
			select p.SteamID).ToArray<Steamworks.SteamID>();
			base.SendMessage(ref message, steamIDRemotes);
			break;
		}
		case ChatMessageType.Player:
			Diagnostics.Assert(gameServerChatMessage.Message.To != null);
			if (gameServerChatMessage.Message.To.IsValid)
			{
				base.SendMessage(ref message, new Steamworks.SteamID[]
				{
					gameServerChatMessage.Message.To
				});
			}
			if (gameServerChatMessage.Message.From != gameServerChatMessage.Message.To && gameServerChatMessage.Message.From.IsValid)
			{
				base.SendMessage(ref message, new Steamworks.SteamID[]
				{
					gameServerChatMessage.Message.From
				});
			}
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	private void RemoveUserFromServer(ulong steamIDUser, GameDisconnectionReason reason = GameDisconnectionReason.Default)
	{
		this.RemoveUserFromServer(new Steamworks.SteamID(steamIDUser), reason);
	}

	private void RemoveUserFromServer(Steamworks.SteamID steamIDUser, GameDisconnectionReason reason = GameDisconnectionReason.Default)
	{
		GameClientConnection gameClientConnection;
		if (!this.GameClientConnections.TryGetValue(steamIDUser, out gameClientConnection))
		{
			Diagnostics.Log("[Net] RemoveUserFromServer 0x{0:x16} has no connection.", new object[]
			{
				steamIDUser
			});
			return;
		}
		if (gameClientConnection.GameClientConnectionState == GameClientConnectionState.DisconnectedFromServer)
		{
			return;
		}
		Diagnostics.Log("[GameServer][Net] Removing user 0x{0:x16} from server. reason={1}.", new object[]
		{
			steamIDUser,
			reason
		});
		if (gameClientConnection.GameClientConnectionState == GameClientConnectionState.AuthenticatedToServer || gameClientConnection.GameClientConnectionState == GameClientConnectionState.ConnectionToServerHasFailed)
		{
			Diagnostics.Log("[GameServer][Net] Ending auth session for user 0x{0:x16}.", new object[]
			{
				steamIDUser
			});
			Steamworks.SteamAPI.SteamGameServer.EndAuthSession(steamIDUser);
		}
		Diagnostics.Log("[GameServer][Net] Closing P2P session for user 0x{0:x16}.", new object[]
		{
			steamIDUser
		});
		this.steamNetworkingProxy.CloseP2PSessionWithUser(steamIDUser);
		gameClientConnection.GameClientConnectionState = GameClientConnectionState.DisconnectedFromServer;
		if ((gameClientConnection.GameClientConnectionFlags & GameClientConnectionFlags.PendingConnection) == GameClientConnectionFlags.PendingConnection)
		{
			gameClientConnection.GameClientConnectionFlags ^= GameClientConnectionFlags.PendingConnection;
		}
		if (this.Game != null && this.Game.HasBeenInitialized)
		{
			Message message = new GameServerPlayerUpdateMessage(steamIDUser, typeof(GameClientState_DisconnectedFromServer).AssemblyQualifiedName, GameServerPlayerUpdateMessage.PlayerAction.Left);
			this.SendMessageToClients(ref message);
			IPlayerRepositoryService service = this.Game.GetService<IPlayerRepositoryService>();
			Diagnostics.Assert(service != null);
			Player playerBySteamID = service.GetPlayerBySteamID(steamIDUser);
			if (playerBySteamID != null)
			{
				string text = AgeLocalizer.Instance.LocalizeString("%PlayerLeftGameInProgress").Replace("$EmpireColor", playerBySteamID.Empire.Color.GetAgeCompatibleColorCode(false)).Replace("$PlayerName", playerBySteamID.LocalizedName);
				Message message2 = new GameServerChatMessage(ChatMessageType.Server, global::Session.Time, this.SteamIDUser, steamIDUser, text);
				this.SendMessageToClients(ref message2);
			}
		}
	}

	private void ISteamServersService_P2PSessionRequest(object sender, P2PSessionRequestEventArgs e)
	{
		Diagnostics.Log("[Net] ISteamServersService_P2PSessionRequest SteamIDRemote=0x{0:x16}", new object[]
		{
			e.Message.m_steamIDRemote
		});
		Steamworks.SteamAPI.SteamGameServerNetworking.AcceptP2PSessionWithUser(e.Message.m_steamIDRemote);
	}

	private void ISteamServersService_P2PSessionConnectFail(object sender, P2PSessionConnectFailEventArgs e)
	{
		Diagnostics.Log("[Net] ISteamServersService_P2PSessionConnectFail SteamIDRemote=0x{0:x16} error=0x{1:x4}", new object[]
		{
			e.Message.m_steamIDRemote,
			e.Message.m_eP2PSessionError
		});
		GameClientConnection gameClientConnection;
		if (!this.GameClientConnections.TryGetValue(e.Message.m_steamIDRemote, out gameClientConnection))
		{
			Diagnostics.LogWarning("[Net] GameClient 0x{0:x16} has no connection.", new object[]
			{
				e.Message.m_steamIDRemote
			});
			return;
		}
		if (gameClientConnection.GameClientConnectionState != GameClientConnectionState.DisconnectedFromServer)
		{
			gameClientConnection.GameClientConnectionState = GameClientConnectionState.ConnectionToServerHasFailed;
			this.RemoveUserFromServer(e.Message.m_steamIDRemote, GameDisconnectionReason.P2PFailure);
		}
	}

	private void ISteamServersService_SteamServerConnectFailure(object sender, SteamServerConnectFailureEventArgs e)
	{
		Diagnostics.Log("[Net] ISteamServersService_SteamSererConnectFailure 0x{0:x4}", new object[]
		{
			(int)e.Message.m_eResult
		});
		((GameClient)base.Session.GameClient).Disconnect(GameDisconnectionReason.SteamServerFailure, (int)e.Message.m_eResult);
		this.Disconnect();
	}

	private void ISteamServersService_ValidateAuthTicketResponse(object sender, ValidateAuthTicketResponseEventArgs e)
	{
		GameClientConnection gameClientConnection;
		if (!this.GameClientConnections.TryGetValue(e.Message.m_SteamID, out gameClientConnection))
		{
			Diagnostics.LogError("[Net][Auth] GameClient 0x{0:x16} has no connection.", new object[]
			{
				e.Message.m_SteamID
			});
			return;
		}
		switch (e.Message.m_eAuthSessionResponse)
		{
		case Steamworks.EAuthSessionResponse.k_EAuthSessionResponseOK:
			Diagnostics.Log("[Net][Auth] ValidateAuthTicketResponse OK for member 0x{1:x16}", new object[]
			{
				(int)e.Message.m_eAuthSessionResponse,
				e.Message.m_SteamID
			});
			gameClientConnection.GameClientConnectionState = GameClientConnectionState.AuthenticatedToServer;
			break;
		case Steamworks.EAuthSessionResponse.k_EAuthSessionResponseUserNotConnectedToSteam:
		case Steamworks.EAuthSessionResponse.k_EAuthSessionResponseNoLicenseOrExpired:
		case Steamworks.EAuthSessionResponse.k_EAuthSessionResponseVACBanned:
		case Steamworks.EAuthSessionResponse.k_EAuthSessionResponseLoggedInElseWhere:
		case Steamworks.EAuthSessionResponse.k_EAuthSessionResponseVACCheckTimedOut:
		case Steamworks.EAuthSessionResponse.k_EAuthSessionResponseAuthTicketInvalidAlreadyUsed:
		case Steamworks.EAuthSessionResponse.k_EAuthSessionResponseAuthTicketInvalid:
			Diagnostics.Log("[Net][Auth] ValidateAuthTicketResponse Error 0x{0:x4} for member 0x{1:x16}", new object[]
			{
				(int)e.Message.m_eAuthSessionResponse,
				e.Message.m_SteamID
			});
			gameClientConnection.GameClientConnectionState = GameClientConnectionState.AuthenticationHasFailed;
			this.RemoveUserFromServer(e.Message.m_SteamID, GameDisconnectionReason.AuthFailure);
			break;
		case Steamworks.EAuthSessionResponse.k_EAuthSessionResponseAuthTicketCanceled:
			Diagnostics.Log("[Net][Auth] ValidateAuthTicketResponse Canceled for member 0x{1:x16}", new object[]
			{
				(int)e.Message.m_eAuthSessionResponse,
				e.Message.m_SteamID
			});
			gameClientConnection.GameClientConnectionState = GameClientConnectionState.ConnectedToServer;
			this.RemoveUserFromServer(e.Message.m_SteamID, GameDisconnectionReason.ClientLeft);
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		Message message = new GameServerAuthTicketResponseMessage(e.Message.m_eAuthSessionResponse);
		base.SendMessage(ref message, new Steamworks.SteamID[]
		{
			new Steamworks.SteamID(e.Message.m_SteamID)
		});
	}

	private void Session_LobbyDataChange(object sender, LobbyDataChangeEventArgs e)
	{
	}

	private void MajorEmpire_OnEmpireBond(MajorEmpire majorEmpire, Player player)
	{
		AIPlayer.PlayerState playerState = (player.Type != PlayerType.AI) ? AIPlayer.PlayerState.EmpireControlledByHuman : AIPlayer.PlayerState.EmpireControlledByAI;
		AIPlayer.PlayerState playerState2;
		if (this.AIScheduler != null && this.AIScheduler.TryGetMajorEmpireAIState(majorEmpire, out playerState2))
		{
			Diagnostics.Log("[GameServer][Net] Changing ai state of empire (index: {0}, from: '{1}', to: '{2}').", new object[]
			{
				majorEmpire.Index,
				playerState2,
				playerState
			});
			this.AIScheduler.ChangeMajorEmpireAIState(majorEmpire, playerState);
		}
	}

	private void IVictoryManagementService_VictoryConditionRaised(object sender, VictoryConditionRaisedEventArgs e)
	{
		base.Session.SetLobbyData("_GameHasEnded", true, true);
	}

	private ISynchronizationService synchronizationService;

	private SynchronizationState syncState;

	private Guid dumpGuid;

	private int dumpFilesToGather;

	private Dictionary<ulong, BattleEncounter> battleEncounters = new Dictionary<ulong, BattleEncounter>();

	private IPlayerRepositoryService playerRepository;

	private IVictoryManagementService victoryManagement;

	private bool disposed;

	private ulong nextPendingOrderSerial;

	private Steamworks.SteamGameServer steamGameServer;

	private ISteamServerService steamServersService;

	private SteamNetworkingProxy steamNetworkingProxy;

	private bool gameInProgress;

	private Queue<Amplitude.Coroutine> saveGameCoroutineQueue;
}
