using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Extensions;
using Amplitude.Interop;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Session;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Steam;

public class ELCPUtilities
{
	public static bool UseELCPPeacePointRulseset
	{
		get
		{
			return ELCPUtilities.useELCPPeacePointRulseset;
		}
		set
		{
			ELCPUtilities.useELCPPeacePointRulseset = value;
		}
	}

	public static void SetupELCPSettings()
	{
		Diagnostics.Log("Setting up ELCP settings ...");
		ELCPUtilities.ELCPVerboseMode = false;
		Diagnostics.Log("ELCP Vebosemode is {0}", new object[]
		{
			ELCPUtilities.ELCPVerboseMode
		});
		ISessionService service = Services.GetService<ISessionService>();
		Diagnostics.Assert(service != null);
		ELCPUtilities.AddGlobalTags(service);
		ELCPUtilities.ELCPShackleAI = ELCPUtilities.SetupELCPOption<bool>("ShackleAI", service, false);
		ELCPUtilities.UseELCPPeacePointRulseset = ELCPUtilities.SetupELCPOption<bool>("PeacePointRulseset", service, true);
		ELCPUtilities.UseELCPFortificationPointRuleset = (ELCPUtilities.SetupELCPOption<string>("FortificationRules", service, "Vanilla") == "ELCP");
		ELCPUtilities.UseELCPCityFoundingRuleset = ELCPUtilities.SetupELCPOption<bool>("WeakerCityFoundings", service, true);
		ELCPUtilities.UseELCPStockpileRulseset = (ELCPUtilities.SetupELCPOption<string>("StockpileRules", service, "Vanilla") == "ELCP");
		ELCPUtilities.UseELCPBlackspotRuleset = ELCPUtilities.SetupELCPOption<bool>("BlackspotRules", service, true);
		ELCPUtilities.UseELCPPeacefulCreepingNodes = ELCPUtilities.SetupELCPOption<bool>("PeacefulBlooms", service, true);
		ELCPUtilities.FOWUpdateFrames = ELCPUtilities.SetupELCPOption<ushort>("FOWUpdateSpeed", service, 1);
		if (service.Session.SessionMode == SessionMode.Single)
		{
			ELCPUtilities.UseXumukMPBattleRules = false;
			ELCPUtilities.SpectatorMode = false;
		}
		else
		{
			ELCPUtilities.UseXumukMPBattleRules = (ELCPUtilities.SetupELCPOption<string>("XumukMPBattleRules", service, "Vanilla") == "Xumuk");
			ELCPUtilities.SpectatorMode = ELCPUtilities.SetupELCPOption<bool>("SpectatorMode", service, false);
		}
		string text = ELCPUtilities.SetupELCPOption<string>("ArmySpeedScaleFactor", service, "Vanilla");
		double elcparmySpeedScaleFactor = 1.0;
		if (text == "Vanilla" || !double.TryParse(text, out elcparmySpeedScaleFactor))
		{
			ELCPUtilities.ELCPArmySpeedScaleFactor = 1.0;
		}
		else
		{
			ELCPUtilities.ELCPArmySpeedScaleFactor = elcparmySpeedScaleFactor;
		}
		ELCPUtilities.UseELCPSymbiosisBuffs = ELCPUtilities.SetupELCPRegistrySetting<bool>("ELCP/UseELCPSymbiosisBuffs", true);
		ELCPUtilities.UseELCPUnitSelling = ELCPUtilities.SetupELCPRegistrySetting<bool>("ELCP/UnitSellingInOwnedTerritoryOnly", false);
		ELCPUtilities.GeomancyRadius = ELCPUtilities.SetupELCPRegistrySetting<int>("ELCP/GeomancyRadius", 1);
		ELCPUtilities.GeomancyDuration = ELCPUtilities.SetupELCPRegistrySetting<int>("ELCP/GeomancyDuration", 1);
		ELCPUtilities.SpectatorSpyFocus = -1;
	}

	public static bool CanTeleportToCity(City city, Army army, Region originRegion, IWorldPositionningService worldPositionningService, IEncounterRepositoryService encounterRepositoryService)
	{
		WorldPosition position;
		return city != null && city.GUID.IsValid && originRegion.City != null && city.Empire.Index == army.Empire.Index && city != originRegion.City && (encounterRepositoryService == null || !encounterRepositoryService.Any((Encounter encounter) => encounter.IsGarrisonInEncounter(city.GUID, false))) && army.Empire.GetAgency<DepartmentOfTransportation>().TryGetFirstCityTileAvailableForTeleport(city, out position) && position.IsValid && !worldPositionningService.IsWaterTile(position);
	}

	public static bool TryGetFirstNodeOfType<T>(BehaviourTreeNodeController controller, out T Node)
	{
		foreach (BehaviourTreeNode behaviourTreeNode in controller.Children)
		{
			if (behaviourTreeNode is T)
			{
				Node = (T)((object)behaviourTreeNode);
				return true;
			}
			if (behaviourTreeNode is BehaviourTreeNodeController)
			{
				T t = default(T);
				if (ELCPUtilities.TryGetFirstNodeOfType<T>(behaviourTreeNode as BehaviourTreeNodeController, out t))
				{
					Node = t;
					return true;
				}
			}
			if (behaviourTreeNode is QuestBehaviourTreeNode_Decorator_InteractWith)
			{
				foreach (QuestBehaviourTreeNode_ConditionCheck questBehaviourTreeNode_ConditionCheck in (behaviourTreeNode as QuestBehaviourTreeNode_Decorator_InteractWith).ConditionChecks)
				{
					if (questBehaviourTreeNode_ConditionCheck is T)
					{
						Node = (T)((object)questBehaviourTreeNode_ConditionCheck);
						return true;
					}
				}
			}
		}
		Node = default(T);
		return false;
	}

	public static bool TryGetNodeOfType<T>(BehaviourTreeNodeController controller, out T Node, ref int startvalue, int position = 1)
	{
		if (position >= 1 && startvalue < position)
		{
			foreach (BehaviourTreeNode behaviourTreeNode in controller.Children)
			{
				if (behaviourTreeNode is T)
				{
					startvalue++;
					if (startvalue == position)
					{
						Node = (T)((object)behaviourTreeNode);
						return true;
					}
				}
				if (behaviourTreeNode is BehaviourTreeNodeController)
				{
					T t = default(T);
					if (ELCPUtilities.TryGetNodeOfType<T>(behaviourTreeNode as BehaviourTreeNodeController, out t, ref startvalue, position))
					{
						Node = t;
						return true;
					}
				}
				if (behaviourTreeNode is QuestBehaviourTreeNode_Decorator_InteractWith)
				{
					foreach (QuestBehaviourTreeNode_ConditionCheck questBehaviourTreeNode_ConditionCheck in (behaviourTreeNode as QuestBehaviourTreeNode_Decorator_InteractWith).ConditionChecks)
					{
						if (questBehaviourTreeNode_ConditionCheck is T)
						{
							startvalue++;
							if (startvalue == position)
							{
								Node = (T)((object)questBehaviourTreeNode_ConditionCheck);
								return true;
							}
						}
					}
				}
			}
		}
		Node = default(T);
		return false;
	}

	public static bool UseELCPFortificationPointRuleset
	{
		get
		{
			return ELCPUtilities.useELCPFortificationPointRuleset;
		}
		set
		{
			ELCPUtilities.useELCPFortificationPointRuleset = value;
		}
	}

	public static bool UseELCPStockpileRulseset
	{
		get
		{
			return ELCPUtilities.useELCPStockpileRulseset;
		}
		set
		{
			ELCPUtilities.useELCPStockpileRulseset = value;
		}
	}

	public static double ELCPArmySpeedScaleFactor
	{
		get
		{
			return ELCPUtilities.armySpeedScaleFactor;
		}
		set
		{
			ELCPUtilities.armySpeedScaleFactor = value;
		}
	}

	public static bool ELCPShackleAI
	{
		get
		{
			return ELCPUtilities.useELCPShackleAI;
		}
		set
		{
			ELCPUtilities.useELCPShackleAI = value;
		}
	}

	public static bool UseELCPBlackspotRuleset
	{
		get
		{
			return ELCPUtilities.useELCPBlackspotRuleset;
		}
		set
		{
			ELCPUtilities.useELCPBlackspotRuleset = value;
		}
	}

	public static bool UseELCPCreepingNodeRuleset
	{
		get
		{
			return ELCPUtilities.useELCPCreepingNodeRuleset;
		}
		set
		{
			ELCPUtilities.useELCPCreepingNodeRuleset = value;
		}
	}

	public static bool UseELCPSymbiosisBuffs
	{
		get
		{
			return ELCPUtilities.useELCPSymbiosisBuffs;
		}
		set
		{
			ELCPUtilities.useELCPSymbiosisBuffs = value;
		}
	}

	public static bool UseELCPPeacefulCreepingNodes
	{
		get
		{
			return ELCPUtilities.useELCPPeacefulCreepingNodes;
		}
		set
		{
			ELCPUtilities.useELCPPeacefulCreepingNodes = value;
		}
	}

	public static bool CanSearch(global::Empire empire, IWorldPositionable item, IQuestManagementService questManagementService)
	{
		PointOfInterest pointOfInterest = item as PointOfInterest;
		if (pointOfInterest == null)
		{
			return false;
		}
		if (pointOfInterest.Type != ELCPUtilities.QuestLocation && pointOfInterest.Type != "NavalQuestLocation")
		{
			Diagnostics.Log("fail1 {0}", new object[]
			{
				pointOfInterest.Type
			});
			return false;
		}
		if (pointOfInterest.Interaction.IsLocked(empire.Index, "ArmyActionSearch"))
		{
			Diagnostics.Log("fail2");
			return false;
		}
		if (ELCPUtilities.UseELCPPeacefulCreepingNodes)
		{
			if (pointOfInterest.CreepingNodeGUID != GameEntityGUID.Zero && pointOfInterest.Empire != empire)
			{
				if (pointOfInterest.Empire == null)
				{
					return false;
				}
				if (!(pointOfInterest.Empire is MajorEmpire))
				{
					return false;
				}
				DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
				if (agency == null)
				{
					return false;
				}
				if (!agency.IsFriend(pointOfInterest.Empire))
				{
					return false;
				}
			}
		}
		else if (pointOfInterest.CreepingNodeGUID != GameEntityGUID.Zero && pointOfInterest.Empire != empire)
		{
			return false;
		}
		if ((pointOfInterest.Interaction.Bits & empire.Bits) == empire.Bits && !SimulationGlobal.GlobalTagsContains(SeasonManager.RuinDustDepositsTag))
		{
			Diagnostics.Log("fail3");
			return false;
		}
		if (SimulationGlobal.GlobalTagsContains(SeasonManager.RuinDustDepositsTag) && !pointOfInterest.UntappedDustDeposits && (pointOfInterest.Interaction.Bits & empire.Bits) == empire.Bits)
		{
			Diagnostics.Log("fail4");
			return false;
		}
		if ((pointOfInterest.Interaction.Bits & empire.Bits) != 0)
		{
			using (IEnumerator<QuestMarker> enumerator = questManagementService.GetMarkersByBoundTargetGUID(pointOfInterest.GUID).GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.IsVisibleFor(empire))
					{
						Diagnostics.Log("fail5");
						return false;
					}
				}
			}
			return true;
		}
		return true;
	}

	public static bool UseXumukMPBattleRules
	{
		get
		{
			return ELCPUtilities.useXumukMPBattleRules;
		}
		set
		{
			ELCPUtilities.useXumukMPBattleRules = value;
		}
	}

	public static bool SpectatorMode
	{
		get
		{
			return ELCPUtilities.spectatorMode;
		}
		set
		{
			ELCPUtilities.spectatorMode = value;
		}
	}

	public static int SpectatorSpyFocus
	{
		get
		{
			return ELCPUtilities.spectatorSpyFocus;
		}
		set
		{
			ELCPUtilities.spectatorSpyFocus = value;
		}
	}

	public static bool CheckCooldownPrerequisites(Army army)
	{
		if (army != null)
		{
			IGameService service = Services.GetService<IGameService>();
			if (service != null && service.Game != null)
			{
				ICooldownManagementService service2 = service.Game.Services.GetService<ICooldownManagementService>();
				if (service2 != null)
				{
					foreach (Unit unit in army.Units)
					{
						Cooldown cooldown;
						if (service2.TryGetCooldown(unit.GUID, out cooldown))
						{
							return false;
						}
					}
					return true;
				}
			}
		}
		return true;
	}

	public static bool CanELCPTameKaiju(Kaiju kaiju, KaijuTameCost tameCost, global::Empire empire)
	{
		KaijuCouncil agency = kaiju.KaijuEmpire.GetAgency<KaijuCouncil>();
		float num = -tameCost.GetValue(empire.SimulationObject);
		DepartmentOfTheTreasury agency2 = empire.GetAgency<DepartmentOfTheTreasury>();
		return agency2 != null && agency2.IsTransferOfResourcePossible(empire, agency.ELCPResourceName, ref num);
	}

	public static bool IsELCPCityBattle(IWorldPositionningService worldPositionningService, List<IGarrison> MainContenders, out List<City> cities)
	{
		cities = new List<City>();
		if (worldPositionningService == null)
		{
			Diagnostics.LogError("worldPositionningService");
		}
		foreach (IGarrison garrison in MainContenders)
		{
			if (!(garrison is IWorldPositionable))
			{
				Diagnostics.LogError("garrison {0}", new object[]
				{
					garrison.LocalizedName
				});
			}
			District district = worldPositionningService.GetDistrict((garrison as IWorldPositionable).WorldPosition);
			if (!worldPositionningService.IsWaterTile((garrison as IWorldPositionable).WorldPosition) && district != null && district.City != null && (District.IsACityTile(district) || district.Type == DistrictType.Exploitation) && !cities.Exists((City C) => C.GUID == district.City.GUID))
			{
				cities.Add(district.City);
			}
		}
		return cities.Count > 0;
	}

	public static bool GetsFortificationBonus(IWorldPositionningService worldPositionningService, IGarrison garrison, City city)
	{
		if (garrison == null || city == null)
		{
			return false;
		}
		District district = worldPositionningService.GetDistrict((garrison as IWorldPositionable).WorldPosition);
		return district != null && District.IsACityTile(district) && district.City == city && district.City.Empire == garrison.Empire;
	}

	public static bool UseELCPMultiThreading
	{
		get
		{
			return ELCPUtilities.useELCPMultiThreading;
		}
		set
		{
			ELCPUtilities.useELCPMultiThreading = value;
		}
	}

	public static ushort FOWUpdateFrames
	{
		get
		{
			return ELCPUtilities.fOWUpdateFrames;
		}
		set
		{
			ELCPUtilities.fOWUpdateFrames = value;
		}
	}

	public static void SteamMatchMaking_TryConnectingToLobby(ulong ConnectToLobbyID)
	{
		ISteamMatchMakingService service = Services.GetService<ISteamMatchMakingService>();
		if (service == null || service.SteamMatchMaking == null)
		{
			Diagnostics.LogWarning("ELCP: Steam matchmaking is null; make sure your Steam client is running...");
			return;
		}
		Steamworks.SteamID steamIDLobby = new Steamworks.SteamID(ConnectToLobbyID);
		if (service.SteamMatchMaking.RequestLobbyData(steamIDLobby))
		{
			PleaseWaitPanel.Instance.Show(AgeLocalizer.Instance.LocalizeString("%GameClientStateConnectingToServer"));
			service.SteamLobbyDataUpdate += ELCPUtilities.SteamMatchMaking_SteamLobbyDataUpdate;
			ELCPUtilities.LobbyID = ConnectToLobbyID;
			return;
		}
		Diagnostics.LogWarning("ELCP: Failed to request the list of lobbies.");
	}

	private static void SteamMatchMaking_CallbackRequestLobbyList(object sender, SteamMatchMakingRequestLobbyListEventArgs e)
	{
		PleaseWaitPanel.Instance.Hide(false);
		Diagnostics.Log("ELCP: Request Lobby List : " + e.Message.m_nLobbiesMatching + " result(s).");
		ISteamMatchMakingService service = Services.GetService<ISteamMatchMakingService>();
		Steamworks.SteamID steamID = new Steamworks.SteamID(ELCPUtilities.LobbyID);
		Diagnostics.Log("ELCP: looking for lobby: " + steamID.AccountID);
		int num = 0;
		while ((long)num < (long)((ulong)e.Message.m_nLobbiesMatching))
		{
			Steamworks.SteamID lobbyByIndex = service.SteamMatchMaking.GetLobbyByIndex(num);
			Diagnostics.Log(string.Concat(new object[]
			{
				"ELCP: lobbybyindex: ",
				lobbyByIndex.AccountID,
				" ",
				lobbyByIndex.IsValid.ToString()
			}));
			if (lobbyByIndex.IsValid && steamID.AccountID == lobbyByIndex.AccountID)
			{
				MenuJoinGameScreen.LobbyDescription lobbyDescription = new MenuJoinGameScreen.LobbyDescription(service, lobbyByIndex);
				Diagnostics.Log("[Lobby] Flags: {0}, Lobby valid? {1}", new object[]
				{
					lobbyDescription.Flags,
					lobbyDescription.IsValid
				});
				if (!lobbyDescription.IsValid)
				{
					break;
				}
				IRuntimeModuleSubscriptionService service2 = Services.GetService<IRuntimeModuleSubscriptionService>();
				if (service == null || service2 == null || !steamID.IsValid)
				{
					break;
				}
				string[] array = new string[0];
				string[] array2 = new string[0];
				RuntimeModuleConfigurationState runtimeModuleListState = service2.GetRuntimeModuleListState(lobbyDescription.RuntimeConfiguration, out array, out array2);
				Diagnostics.Log("ELCP runtimeModuleConfigurationState {0} missingWorkshopItems {1} missingNonWorkshopItems {2} RuntimeConfiguration {3}", new object[]
				{
					runtimeModuleListState + 1,
					array.Length,
					array2.Length,
					(lobbyDescription.RuntimeConfiguration == null) ? -1 : lobbyDescription.RuntimeConfiguration.Length
				});
				switch (runtimeModuleListState + 1)
				{
				case RuntimeModuleConfigurationState.Yellow:
					Services.GetService<IRuntimeService>().Runtime.FiniteStateMachine.PostStateChange(typeof(RuntimeState_Lobby), new object[]
					{
						steamID
					});
					goto IL_28B;
				case RuntimeModuleConfigurationState.Red:
					ELCPUtilities.SelectedLobby = lobbyDescription;
					if (array.Length != 0)
					{
						string text = AgeLocalizer.Instance.LocalizeString("%ConfirmDownloadModsBeforeActivation");
						MessagePanel.Instance.Show(text.Replace("$NumberOfMods", array.Length.ToString()), "%Confirmation", MessagePanelButtons.OkCancel, new MessagePanel.EventHandler(ELCPUtilities.SteamMatchMaking_OnConfirmOpenActivationPanel), MessagePanelType.IMPORTANT, new MessagePanelButton[0]);
						goto IL_28B;
					}
					ELCPUtilities.SteamMatchMaking_OnConfirmOpenActivationPanel(Amplitude.Unity.Gui.GuiScreen.CurrentScreen, new MessagePanelResultEventArgs(MessagePanelResult.Ok));
					goto IL_28B;
				case (RuntimeModuleConfigurationState)3:
					ELCPUtilities.SelectedLobby = lobbyDescription;
					ELCPUtilities.SteamMatchMaking_OnConfirmOpenActivationPanel(Amplitude.Unity.Gui.GuiScreen.CurrentScreen, new MessagePanelResultEventArgs(MessagePanelResult.Ok));
					goto IL_28B;
				default:
					goto IL_28B;
				}
			}
			else
			{
				num++;
			}
		}
		IL_28B:
		service.SteamMatchMakingRequestLobbyList -= ELCPUtilities.SteamMatchMaking_CallbackRequestLobbyList;
		ELCPUtilities.LobbyID = 0UL;
		ELCPUtilities.steamAPICall = 0UL;
	}

	private static void SteamMatchMaking_OnConfirmOpenActivationPanel(object sender, MessagePanelResultEventArgs e)
	{
		global::IGuiService service = Services.GetService<global::IGuiService>();
		if (e.Result == MessagePanelResult.Ok && service != null)
		{
			service.Show(typeof(ActivateRuntimeModulesModalPanel), new object[]
			{
				ELCPUtilities.SelectedLobby.RuntimeConfiguration,
				ELCPUtilities.SelectedLobby.SteamIDLobby
			});
		}
		ELCPUtilities.SelectedLobby = null;
	}

	private static T SetupELCPOption<T>(string name, ISessionService service, T defaultvalue)
	{
		object obj = service.Session.GetLobbyData<T>(name, defaultvalue);
		Diagnostics.Log(string.Format("{0} is {1}", name, obj));
		return (T)((object)obj);
	}

	public static bool UseELCPCityFoundingRuleset
	{
		get
		{
			return ELCPUtilities.useELCPCityFoundingRuleset;
		}
		set
		{
			ELCPUtilities.useELCPCityFoundingRuleset = value;
		}
	}

	public static bool UseELCPUnitSelling
	{
		get
		{
			return ELCPUtilities.useELCPUnitSelling;
		}
		set
		{
			ELCPUtilities.useELCPUnitSelling = value;
		}
	}

	private static void SteamMatchMaking_SteamLobbyDataUpdate(object sender, SteamLobbyDataUpdateEventArgs e)
	{
		PleaseWaitPanel.Instance.Hide(false);
		Diagnostics.Log("ELCP: LobbyDataUpdate, success? {0}, lobby {1}, expected {2}", new object[]
		{
			e.Message.m_bSuccess,
			e.Message.m_ulSteamIDLobby,
			ELCPUtilities.LobbyID
		});
		if (e.Message.m_bSuccess == 0 || ELCPUtilities.LobbyID != e.Message.m_ulSteamIDLobby)
		{
			return;
		}
		ISteamMatchMakingService service = Services.GetService<ISteamMatchMakingService>();
		Steamworks.SteamID steamID = new Steamworks.SteamID(ELCPUtilities.LobbyID);
		MenuJoinGameScreen.LobbyDescription lobbyDescription = new MenuJoinGameScreen.LobbyDescription(service, steamID);
		Diagnostics.Log("[Lobby] Flags: {0}, Lobby valid? {1}", new object[]
		{
			lobbyDescription.Flags,
			lobbyDescription.IsValid
		});
		service.SteamLobbyDataUpdate -= ELCPUtilities.SteamMatchMaking_SteamLobbyDataUpdate;
		ELCPUtilities.LobbyID = 0UL;
		if (!lobbyDescription.IsValid)
		{
			return;
		}
		IRuntimeModuleSubscriptionService service2 = Services.GetService<IRuntimeModuleSubscriptionService>();
		if (service == null || service2 == null || !steamID.IsValid)
		{
			return;
		}
		string[] array = new string[0];
		string[] array2 = new string[0];
		RuntimeModuleConfigurationState runtimeModuleListState = service2.GetRuntimeModuleListState(lobbyDescription.RuntimeConfiguration, out array, out array2);
		Diagnostics.Log("ELCP runtimeModuleConfigurationState {0} missingWorkshopItems {1} missingNonWorkshopItems {2} RuntimeConfiguration {3}", new object[]
		{
			runtimeModuleListState + 1,
			array.Length,
			array2.Length,
			(lobbyDescription.RuntimeConfiguration == null) ? -1 : lobbyDescription.RuntimeConfiguration.Length
		});
		switch (runtimeModuleListState + 1)
		{
		case RuntimeModuleConfigurationState.Yellow:
			Services.GetService<IRuntimeService>().Runtime.FiniteStateMachine.PostStateChange(typeof(RuntimeState_Lobby), new object[]
			{
				steamID
			});
			return;
		case RuntimeModuleConfigurationState.Red:
			ELCPUtilities.SelectedLobby = lobbyDescription;
			if (array.Length != 0)
			{
				string text = AgeLocalizer.Instance.LocalizeString("%ConfirmDownloadModsBeforeActivation");
				MessagePanel.Instance.Show(text.Replace("$NumberOfMods", array.Length.ToString()), "%Confirmation", MessagePanelButtons.OkCancel, new MessagePanel.EventHandler(ELCPUtilities.SteamMatchMaking_OnConfirmOpenActivationPanel), MessagePanelType.IMPORTANT, new MessagePanelButton[0]);
				return;
			}
			ELCPUtilities.SteamMatchMaking_OnConfirmOpenActivationPanel(Amplitude.Unity.Gui.GuiScreen.CurrentScreen, new MessagePanelResultEventArgs(MessagePanelResult.Ok));
			return;
		case (RuntimeModuleConfigurationState)3:
			ELCPUtilities.SelectedLobby = lobbyDescription;
			ELCPUtilities.SteamMatchMaking_OnConfirmOpenActivationPanel(Amplitude.Unity.Gui.GuiScreen.CurrentScreen, new MessagePanelResultEventArgs(MessagePanelResult.Ok));
			return;
		default:
			return;
		}
	}

	public static void SpellUsage_Register(GameEntityGUID encounterGUID, int empireIndex, StaticString spellUsed)
	{
		if (!ELCPUtilities.UseELCPSymbiosisBuffs)
		{
			return;
		}
		ELCPUtilities.SpellUsageTracker spellUsageTracker = ELCPUtilities.spellUsageTracker.Find((ELCPUtilities.SpellUsageTracker s) => s.EncounterGUID == encounterGUID && s.EmpireIndex == empireIndex);
		if (spellUsageTracker == null)
		{
			ELCPUtilities.spellUsageTracker.Add(new ELCPUtilities.SpellUsageTracker(encounterGUID, empireIndex, spellUsed));
			return;
		}
		spellUsageTracker.AddSpellUsage(spellUsed);
	}

	public static void SpellUsage_UnregisterEncounter(GameEntityGUID encounterGUID)
	{
		for (int i = 0; i < ELCPUtilities.spellUsageTracker.Count; i++)
		{
			if (ELCPUtilities.spellUsageTracker[i].EncounterGUID == encounterGUID)
			{
				ELCPUtilities.spellUsageTracker.RemoveAt(i);
				i--;
			}
		}
	}

	public static bool SpellUsage_HasSpellBeenUsed(GameEntityGUID encounterGUID, int empireIndex, StaticString spellUsed)
	{
		if (!ELCPUtilities.UseELCPSymbiosisBuffs)
		{
			return false;
		}
		ELCPUtilities.SpellUsageTracker spellUsageTracker = ELCPUtilities.spellUsageTracker.Find((ELCPUtilities.SpellUsageTracker s) => s.EncounterGUID == encounterGUID && s.EmpireIndex == empireIndex);
		return spellUsageTracker != null && spellUsageTracker.HasSpellBeenUsed(spellUsed);
	}

	public static void SpellUsage_Clear()
	{
		ELCPUtilities.spellUsageTracker.Clear();
	}

	public static int NecroCadavresPerVillage { get; set; }

	public static int NecroCadavresPerSacrifice { get; set; }

	public static int GeomancyRadius { get; set; }

	public static int GeomancyDuration { get; set; }

	private static T SetupELCPRegistrySetting<T>(string name, T defaultvalue)
	{
		object obj = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<T>(name, defaultvalue);
		Diagnostics.Log(string.Format("Registry Setting {0} is {1}", name, obj));
		return (T)((object)obj);
	}

	private static void AddGlobalTags(ISessionService service)
	{
		if (!SimulationGlobal.GlobalTagsContains("ELCP"))
		{
			SimulationGlobal.AddGlobalTag("ELCP", false);
		}
		if (service.Session.SessionMode == SessionMode.Single)
		{
			if (!SimulationGlobal.GlobalTagsContains("Singleplayer"))
			{
				SimulationGlobal.AddGlobalTag("Singleplayer", false);
			}
		}
		else if (!SimulationGlobal.GlobalTagsContains("Multiplayer"))
		{
			SimulationGlobal.AddGlobalTag("Multiplayer", false);
		}
		foreach (OptionDefinition optionDefinition in Databases.GetDatabase<OptionDefinition>(false))
		{
			GameOptionDefinition gameOptionDefinition = optionDefinition as GameOptionDefinition;
			if (gameOptionDefinition != null && gameOptionDefinition.SaveAsGlobalTag)
			{
				string text = gameOptionDefinition.Name + service.Session.GetLobbyData<string>(gameOptionDefinition.Name, gameOptionDefinition.DefaultName);
				if (!SimulationGlobal.GlobalTagsContains(text))
				{
					SimulationGlobal.AddGlobalTag(text, false);
					Diagnostics.Log("ELCP adding Global Tag {0}", new object[]
					{
						text
					});
				}
			}
		}
	}

	public static bool CanSearch(global::Empire empire, IWorldPositionable item, IQuestManagementService questManagementService, IQuestRepositoryService questRepositoryService)
	{
		PointOfInterest pointOfInterest = item as PointOfInterest;
		if (pointOfInterest == null)
		{
			return false;
		}
		if (pointOfInterest.Type != ELCPUtilities.QuestLocation && pointOfInterest.Type != "NavalQuestLocation")
		{
			return false;
		}
		if (pointOfInterest.Interaction.IsLocked(empire.Index, "ArmyActionSearch"))
		{
			return false;
		}
		if (ELCPUtilities.UseELCPPeacefulCreepingNodes)
		{
			if (pointOfInterest.CreepingNodeGUID != GameEntityGUID.Zero && pointOfInterest.Empire != empire)
			{
				if (pointOfInterest.Empire == null)
				{
					return false;
				}
				if (!(pointOfInterest.Empire is MajorEmpire))
				{
					return false;
				}
				DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
				if (agency == null)
				{
					return false;
				}
				if (!agency.IsFriend(pointOfInterest.Empire))
				{
					return false;
				}
			}
		}
		else if (pointOfInterest.CreepingNodeGUID != GameEntityGUID.Zero && pointOfInterest.Empire != empire)
		{
			return false;
		}
		if ((pointOfInterest.Interaction.Bits & empire.Bits) == empire.Bits && !SimulationGlobal.GlobalTagsContains(SeasonManager.RuinDustDepositsTag))
		{
			return false;
		}
		if (SimulationGlobal.GlobalTagsContains(SeasonManager.RuinDustDepositsTag) && !pointOfInterest.UntappedDustDeposits && (pointOfInterest.Interaction.Bits & empire.Bits) == empire.Bits)
		{
			return false;
		}
		if ((pointOfInterest.Interaction.Bits & empire.Bits) != 0)
		{
			using (IEnumerator<QuestMarker> enumerator = questManagementService.GetMarkersByBoundTargetGUID(pointOfInterest.GUID).GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Quest quest;
					if (questRepositoryService.TryGetValue(enumerator.Current.QuestGUID, out quest) && quest.EmpireBits == empire.Bits)
					{
						return false;
					}
				}
			}
			return true;
		}
		return true;
	}

	private static bool useELCPPeacePointRulseset;

	private static bool useELCPFortificationPointRuleset;

	private static bool useELCPStockpileRulseset;

	private static double armySpeedScaleFactor;

	private static bool useELCPShackleAI;

	private static bool useELCPBlackspotRuleset;

	private static bool useELCPCreepingNodeRuleset;

	private static bool useELCPSymbiosisBuffs;

	private static bool useELCPPeacefulCreepingNodes;

	private static bool useXumukMPBattleRules;

	private static bool spectatorMode;

	private static int spectatorSpyFocus = -1;

	public static int NumberOfMajorEmpires;

	public static List<int> EliminatedEmpireIndices;

	private static bool useELCPMultiThreading;

	private static ushort fOWUpdateFrames;

	private static ulong LobbyID;

	private static ulong steamAPICall;

	private static MenuJoinGameScreen.LobbyDescription SelectedLobby;

	private static bool useELCPCityFoundingRuleset;

	public static readonly StaticString ELCPNakedSettler = new StaticString("ELCPNakedSettler");

	private static bool useELCPUnitSelling;

	private static List<ELCPUtilities.SpellUsageTracker> spellUsageTracker = new List<ELCPUtilities.SpellUsageTracker>();

	public static bool ELCPVerboseMode;

	public static readonly StaticString QuestLocation = "QuestLocation";

	public static class AIVictoryFocus
	{
		public static readonly string Economy = "Economy";

		public static readonly string Diplomacy = "Diplomacy";

		public static readonly string Military = "Military";

		public static readonly string Technology = "MostTechnologiesDiscovered";
	}

	private class SpellUsageTracker
	{
		public GameEntityGUID EncounterGUID { get; set; }

		public int EmpireIndex { get; set; }

		public SpellUsageTracker(GameEntityGUID encounterGUID, int empireIndex, StaticString spellUsed)
		{
			this.EncounterGUID = encounterGUID;
			this.EmpireIndex = empireIndex;
			this.spellsUsed = new List<StaticString>();
			this.spellsUsed.Add(spellUsed);
		}

		public void AddSpellUsage(StaticString spellUsed)
		{
			this.spellsUsed.AddOnce(spellUsed);
		}

		public bool HasSpellBeenUsed(StaticString spellUsed)
		{
			using (List<StaticString>.Enumerator enumerator = this.spellsUsed.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current == spellUsed)
					{
						return true;
					}
				}
			}
			return false;
		}

		private List<StaticString> spellsUsed;
	}
}
