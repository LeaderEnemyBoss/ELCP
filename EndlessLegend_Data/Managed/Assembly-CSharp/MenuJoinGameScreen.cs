using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Interop;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Session;
using Amplitude.Unity.Steam;
using UnityEngine;

public class MenuJoinGameScreen : GuiMenuScreen
{
	public override string MenuName
	{
		get
		{
			return "%BreadcrumbJoinGameTitle";
		}
		set
		{
		}
	}

	private ISteamMatchMakingService MatchMakingService { get; set; }

	private List<MenuJoinGameScreen.LobbyDescription> LobbyDescriptions { get; set; }

	private MenuOnlineSessionLine SelectedSessionLine
	{
		get
		{
			return this.selectedSessionLine;
		}
		set
		{
			this.selectedSessionLine = value;
			this.JoinButton.AgeTransform.Enable = (this.selectedSessionLine != null);
		}
	}

	private MenuJoinGameScreen.LobbyDescription SelectedLobby
	{
		get
		{
			if (this.SelectedSessionLine != null)
			{
				return this.SelectedSessionLine.LobbyDescription;
			}
			return null;
		}
	}

	[Service]
	private ISessionService SessionService { get; set; }

	public override bool HandleCancelRequest()
	{
		if (base.IsShowing)
		{
			base.StartCoroutine(this.HandleCancelRequestWhenShowingFinished());
			return true;
		}
		base.SetupDepthDown();
		IRuntimeService service = Services.GetService<IRuntimeService>();
		if (service != null)
		{
			service.Runtime.FiniteStateMachine.PostStateChange(typeof(RuntimeState_OutGame), new object[0]);
		}
		return true;
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		this.GameSessionsTable.Height = 0f;
		this.GameSessionsTable.ReserveChildren(this.LobbyDescriptions.Count, this.GameSessionLinePrefab, "Item");
		this.GameSessionsTable.RefreshChildrenIList<MenuJoinGameScreen.LobbyDescription>(this.LobbyDescriptions, this.setupGameSessionLineDelegate, true, false);
		SortedLinesTable component = this.GameSessionsTable.GetComponent<SortedLinesTable>();
		if (component != null)
		{
			component.SortLines();
		}
		this.GameSessionsTable.ArrangeChildren();
		this.GameSessionsScrollView.OnPositionRecomputed();
		this.EnforceRadio();
		this.JoinButton.AgeTransform.Enable = false;
	}

	public void OnDoubleClickLine(MenuOnlineSessionLine gameSessionLine)
	{
		this.SelectedSessionLine = gameSessionLine;
		this.EnforceRadio();
		if (this.SelectedLobby != null)
		{
			this.JoinSelectedGame();
		}
	}

	public void OnToggleLine(MenuOnlineSessionLine gameSessionLine)
	{
		if (gameSessionLine != null && gameSessionLine.SelectionToggle.State)
		{
			this.SelectedSessionLine = gameSessionLine;
			this.EnforceRadio();
		}
		else
		{
			this.SelectedSessionLine = null;
		}
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		this.SetBreadcrumb(base.GuiService.GetGuiPanel<MenuMainScreen>().MenuBreadcrumb.Text);
		this.SortsContainer.SetContent(this.GameSessionLinePrefab, "JoinGame", null);
		this.LobbyDescriptions.Clear();
		this.RefreshContent();
		this.RequestLobbyList();
		yield break;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		this.SortsContainer.UnsetContent();
		if (this.steamAPICall != 0UL)
		{
			this.MatchMakingService.SteamMatchMakingRequestLobbyList -= this.SteamMatchMaking_CallbackRequestLobbyList;
			this.steamAPICall = 0UL;
		}
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		this.setupGameSessionLineDelegate = new AgeTransform.RefreshTableItem<MenuJoinGameScreen.LobbyDescription>(this.SetupGameSessionLine);
		this.MatchMakingService = Services.GetService<ISteamMatchMakingService>();
		Diagnostics.Assert(this.MatchMakingService != null, "ISteamMatchMakingService is null");
		this.LobbyDescriptions = new List<MenuJoinGameScreen.LobbyDescription>();
		yield break;
	}

	protected override void OnUnload()
	{
		this.setupGameSessionLineDelegate = null;
		base.OnUnload();
	}

	private void EnforceRadio()
	{
		for (int i = 0; i < this.GameSessionsTable.GetChildren().Count; i++)
		{
			MenuOnlineSessionLine component = this.GameSessionsTable.GetChildren()[i].GetComponent<MenuOnlineSessionLine>();
			component.SelectionToggle.State = (component.LobbyDescription == this.SelectedLobby);
		}
	}

	private void JoinSelectedGame()
	{
		IRuntimeService service = Services.GetService<IRuntimeService>();
		if (service != null)
		{
			RuntimeModuleConfigurationState gameSaveRuntimeConfigurationState = this.SelectedSessionLine.GameSaveRuntimeConfigurationState;
			switch (gameSaveRuntimeConfigurationState + 1)
			{
			case RuntimeModuleConfigurationState.Yellow:
				service.Runtime.FiniteStateMachine.PostStateChange(typeof(RuntimeState_Lobby), new object[]
				{
					this.SelectedLobby.SteamIDLobby
				});
				break;
			case RuntimeModuleConfigurationState.Red:
			{
				IRuntimeModuleSubscriptionService service2 = Services.GetService<IRuntimeModuleSubscriptionService>();
				string[] array = new string[0];
				string[] array2 = new string[0];
				if (service2 != null)
				{
					service2.GetRuntimeModuleListState(this.SelectedLobby.RuntimeConfiguration, out array, out array2);
				}
				if (array.Length > 0)
				{
					string text = AgeLocalizer.Instance.LocalizeString("%ConfirmDownloadModsBeforeActivation");
					MessagePanel.Instance.Show(text.Replace("$NumberOfMods", array.Length.ToString()), "%Confirmation", MessagePanelButtons.OkCancel, new MessagePanel.EventHandler(this.OnConfirmOpenActivationPanel), MessagePanelType.IMPORTANT, new MessagePanelButton[0]);
				}
				else
				{
					this.OnConfirmOpenActivationPanel(this, new MessagePanelResultEventArgs(MessagePanelResult.Ok));
				}
				break;
			}
			case (RuntimeModuleConfigurationState)3:
				this.OnConfirmOpenActivationPanel(this, new MessagePanelResultEventArgs(MessagePanelResult.Ok));
				break;
			}
		}
	}

	private void OnCancelCB(GameObject gameObject)
	{
		this.HandleCancelRequest();
	}

	private void OnConfirmOpenActivationPanel(object sender, MessagePanelResultEventArgs e)
	{
		if (e.Result == MessagePanelResult.Ok)
		{
			base.GuiService.Show(typeof(ActivateRuntimeModulesModalPanel), new object[]
			{
				this.SelectedLobby.RuntimeConfiguration,
				this.SelectedLobby.SteamIDLobby
			});
		}
	}

	private void OnJoinCB(GameObject gameObject)
	{
		if (this.SelectedLobby != null)
		{
			this.JoinSelectedGame();
		}
	}

	private void OnRefreshCB(GameObject gameObject)
	{
		this.SelectedSessionLine = null;
		this.RequestLobbyList();
	}

	private IEnumerator HandleCancelRequestWhenShowingFinished()
	{
		while (base.IsShowing)
		{
			yield return null;
		}
		this.HandleCancelRequest();
		yield break;
	}

	private void SetupGameSessionLine(AgeTransform tableItem, MenuJoinGameScreen.LobbyDescription lobbyDescription, int index)
	{
		MenuOnlineSessionLine component = tableItem.GetComponent<MenuOnlineSessionLine>();
		if (component == null)
		{
			Diagnostics.LogError("In the MenuJoinGameScreen, trying to refresh a table item that is not a GameSessionLine");
			return;
		}
		component.LobbyDescription = lobbyDescription;
		component.Parent = this;
		component.RefreshContent();
	}

	private void RequestLobbyList()
	{
		this.GameSessionsTable.Enable = false;
		if (this.MatchMakingService != null && this.MatchMakingService.SteamMatchMaking != null)
		{
			this.MatchMakingService.SteamMatchMaking.AddRequestLobbyListResultCountFilter(Amplitude.Unity.Runtime.Runtime.Registry.GetValue<int>("Steam/LobbyListResultCount", int.MaxValue));
			this.MatchMakingService.SteamMatchMaking.AddRequestLobbyListDistanceFilter(Steamworks.SteamMatchMaking.ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
			this.steamAPICall = this.MatchMakingService.SteamMatchMaking.RequestLobbyList();
			if (this.steamAPICall != 0UL)
			{
				this.RefreshButton.AgeTransform.Enable = false;
				this.StatusLabel.AgeTransform.Visible = true;
				this.StatusLabel.Text = "%RetrievingTheListOfOnlineGamesTitle";
				this.MatchMakingService.SteamMatchMakingRequestLobbyList += this.SteamMatchMaking_CallbackRequestLobbyList;
				this.MatchMakingService.RegisterRequestLobbyListCallback(this.steamAPICall);
			}
			else
			{
				Diagnostics.LogWarning("Failed to request the list of lobbies.");
				this.StatusLabel.AgeTransform.Visible = true;
				this.StatusLabel.Text = "%FailedToRetrieveTheListOfOnlineGamesTitle";
			}
		}
		else
		{
			Diagnostics.LogWarning("Steam matchmaking is null; make sure your Steam client is running...");
			this.StatusLabel.AgeTransform.Visible = true;
			this.StatusLabel.Text = "%SteamSeemsToBeOffline";
		}
	}

	private void SteamMatchMaking_CallbackRequestLobbyList(object sender, SteamMatchMakingRequestLobbyListEventArgs e)
	{
		Diagnostics.Log("Request Lobby List : " + e.Message.m_nLobbiesMatching + " result(s).");
		this.GameSessionsTable.Enable = true;
		this.RefreshButton.AgeTransform.Enable = true;
		this.StatusLabel.AgeTransform.Visible = false;
		this.StatusLabel.Text = string.Empty;
		this.LobbyDescriptions.Clear();
		int num = 0;
		while ((long)num < (long)((ulong)e.Message.m_nLobbiesMatching))
		{
			Steamworks.SteamID lobbyByIndex = this.MatchMakingService.SteamMatchMaking.GetLobbyByIndex(num);
			if (lobbyByIndex.IsValid)
			{
				MenuJoinGameScreen.LobbyDescription lobbyDescription = new MenuJoinGameScreen.LobbyDescription(this.MatchMakingService, lobbyByIndex);
				Diagnostics.Log("[Lobby] {0}", new object[]
				{
					lobbyDescription.Flags
				});
				if (lobbyDescription.IsValid)
				{
					if (!lobbyDescription.HasFlag(MenuJoinGameScreen.LobbyDescription.LobbyFlag.GameHasEnded) && !lobbyDescription.HasFlag(MenuJoinGameScreen.LobbyDescription.LobbyFlag.GameIsMigrating))
					{
						if (!lobbyDescription.HasFlag(MenuJoinGameScreen.LobbyDescription.LobbyFlag.FriendsOnly))
						{
							this.LobbyDescriptions.Add(lobbyDescription);
						}
						else if (lobbyDescription.HasFlag(MenuJoinGameScreen.LobbyDescription.LobbyFlag.FriendsOnly | MenuJoinGameScreen.LobbyDescription.LobbyFlag.FriendlyLobby))
						{
							this.LobbyDescriptions.Add(lobbyDescription);
						}
					}
				}
			}
			num++;
		}
		this.MatchMakingService.SteamMatchMakingRequestLobbyList -= this.SteamMatchMaking_CallbackRequestLobbyList;
		this.steamAPICall = 0UL;
		this.RefreshContent();
	}

	public Transform GameSessionLinePrefab;

	public SortButtonsContainer SortsContainer;

	public AgeTransform GameSessionsTable;

	public AgeControlScrollView GameSessionsScrollView;

	public AgePrimitiveLabel StatusLabel;

	public AgeControlButton JoinButton;

	public AgeControlButton RefreshButton;

	private MenuOnlineSessionLine selectedSessionLine;

	private ulong steamAPICall;

	private AgeTransform.RefreshTableItem<MenuJoinGameScreen.LobbyDescription> setupGameSessionLineDelegate;

	public class LobbyDescription
	{
		public LobbyDescription(ISteamMatchMakingService matchMakingService, Steamworks.SteamID steamIDLobby)
		{
			this.IsValid = true;
			this.SteamIDLobby = steamIDLobby;
			this.MajorFactions = this.TryGetLobbyData<int>(matchMakingService, steamIDLobby, "NumberOfMajorFactions", true);
			this.Name = this.TryGetLobbyData<string>(matchMakingService, steamIDLobby, "name", true);
			this.AdvancedSeasons = this.TryGetLobbyData<bool>(matchMakingService, steamIDLobby, "AdvancedSeasons", true);
			this.DownloadableContentSharedByServer = this.TryGetLobbyData<uint>(matchMakingService, steamIDLobby, "sbs", false);
			this.EmpireInfoAccessibility = this.TryGetLobbyData<string>(matchMakingService, steamIDLobby, "EmpireInfoAccessibility", true);
			this.FreeSlots = this.TryGetLobbyData<int>(matchMakingService, steamIDLobby, "FreeSlots", true);
			this.GameDifficulty = this.TryGetLobbyData<string>(matchMakingService, steamIDLobby, "GameDifficulty", true);
			this.GameHasEnded = this.TryGetLobbyData<bool>(matchMakingService, steamIDLobby, "_GameHasEnded", false);
			this.GameInProgress = this.TryGetLobbyData<bool>(matchMakingService, steamIDLobby, "_GameInProgress", false);
			this.GameIsMigrating = this.TryGetLobbyData<bool>(matchMakingService, steamIDLobby, "_GameIsMigrating", false);
			this.GameSpeed = this.TryGetLobbyData<string>(matchMakingService, steamIDLobby, "GameSpeed", true);
			this.Hash = this.TryGetLobbyData<string>(matchMakingService, steamIDLobby, "runtimehash", true);
			this.IsMultiplayerSave = this.TryGetLobbyData<bool>(matchMakingService, steamIDLobby, "_IsSavedGame", false);
			this.Launching = this.TryGetLobbyData<bool>(matchMakingService, steamIDLobby, "_Launching", false);
			this.OccupiedSlots = this.TryGetLobbyData<int>(matchMakingService, steamIDLobby, "OccupiedSlots", true);
			string text = this.TryGetLobbyData<string>(matchMakingService, steamIDLobby, "runtimeconfiguration", true);
			this.RuntimeConfiguration = ((!string.IsNullOrEmpty(text)) ? text.Split(Amplitude.String.Separators) : null);
			this.SeasonDifficulty = this.TryGetLobbyData<string>(matchMakingService, steamIDLobby, "SeasonDifficulty", true);
			this.PlayWithMadSeason = this.TryGetLobbyData<bool>(matchMakingService, steamIDLobby, "PlayWithMadSeason", true);
			this.PlayWithKaiju = this.TryGetLobbyData<bool>(matchMakingService, steamIDLobby, "PlayWithKaiju", true);
			this.MadSeasonType = this.TryGetLobbyData<string>(matchMakingService, steamIDLobby, "MadSeasonType", true);
			this.SessionMode = this.TryGetLobbyData<SessionMode>(matchMakingService, steamIDLobby, "SessionMode", true);
			this.SteamIDOwner = this.TryGetLobbyData<ulong>(matchMakingService, steamIDLobby, "Owner", true);
			this.TimedEncounters = this.TryGetLobbyData<bool>(matchMakingService, steamIDLobby, "TimedEncounter", true);
			this.TimedTurns = this.TryGetLobbyData<bool>(matchMakingService, steamIDLobby, "TimedTurn", true);
			this.Turn = this.TryGetLobbyData<int>(matchMakingService, steamIDLobby, "_Turn", true);
			long version = this.TryGetLobbyData<long>(matchMakingService, steamIDLobby, "Version", true);
			this.Version = new Amplitude.Unity.Framework.Version(version);
			this.WithCustomFactions = this.TryGetLobbyData<bool>(matchMakingService, steamIDLobby, "CustomFactions", true);
			this.WorldShape = this.TryGetLobbyData<string>(matchMakingService, steamIDLobby, "WorldShape", true);
			this.WorldSize = this.TryGetLobbyData<string>(matchMakingService, steamIDLobby, "WorldSize", true);
			this.WorldTemperature = this.TryGetLobbyData<string>(matchMakingService, steamIDLobby, "Temperature", true);
			this.WorldWrap = this.TryGetLobbyData<string>(matchMakingService, steamIDLobby, "WorldWrap", true);
			this.VictoryConditions = null;
			IDatabase<VictoryCondition> database = Databases.GetDatabase<VictoryCondition>(false);
			if (database != null)
			{
				List<VictoryCondition> list = new List<VictoryCondition>();
				foreach (VictoryCondition victoryCondition in database)
				{
					bool flag = this.TryGetLobbyData<bool>(matchMakingService, steamIDLobby, victoryCondition.Name, false);
					if (flag)
					{
						list.Add(victoryCondition);
					}
				}
				this.VictoryConditions = list.ToArray();
			}
			this.InitializeFlags();
		}

		public bool AdvancedSeasons { get; private set; }

		public uint DownloadableContentSharedByServer { get; set; }

		public string EmpireInfoAccessibility { get; private set; }

		public MenuJoinGameScreen.LobbyDescription.LobbyFlag Flags { get; private set; }

		public int FreeSlots { get; private set; }

		public string GameDifficulty { get; private set; }

		public bool GameHasEnded { get; private set; }

		public bool GameInProgress { get; private set; }

		public bool GameIsMigrating { get; private set; }

		public string GameSpeed { get; private set; }

		public string Hash { get; private set; }

		public bool IsMultiplayerSave { get; private set; }

		public bool IsValid { get; private set; }

		public bool Launching { get; private set; }

		public int MajorFactions { get; private set; }

		public string Name { get; private set; }

		public int OccupiedSlots { get; private set; }

		public string[] RuntimeConfiguration { get; private set; }

		public string SeasonDifficulty { get; private set; }

		public bool PlayWithMadSeason { get; private set; }

		public string MadSeasonType { get; private set; }

		public bool PlayWithKaiju { get; private set; }

		public SessionMode SessionMode { get; private set; }

		public Steamworks.SteamID SteamIDLobby { get; private set; }

		public ulong SteamIDOwner { get; private set; }

		public bool TimedEncounters { get; private set; }

		public bool TimedTurns { get; private set; }

		public int Turn { get; private set; }

		public Amplitude.Unity.Framework.Version Version { get; private set; }

		public VictoryCondition[] VictoryConditions { get; private set; }

		public bool WithCustomFactions { get; private set; }

		public string WorldShape { get; private set; }

		public string WorldSize { get; private set; }

		public string WorldTemperature { get; private set; }

		public string WorldWrap { get; private set; }

		public bool HasFlag(MenuJoinGameScreen.LobbyDescription.LobbyFlag flag)
		{
			return (this.Flags & flag) == flag;
		}

		private void InitializeFlags()
		{
			if (this.GameInProgress)
			{
				this.Flags |= MenuJoinGameScreen.LobbyDescription.LobbyFlag.GameInProgress;
			}
			if (this.GameHasEnded)
			{
				this.Flags |= MenuJoinGameScreen.LobbyDescription.LobbyFlag.GameHasEnded;
			}
			if (this.GameIsMigrating)
			{
				this.Flags |= MenuJoinGameScreen.LobbyDescription.LobbyFlag.GameIsMigrating;
			}
			if (this.IsMultiplayerSave)
			{
				this.Flags |= MenuJoinGameScreen.LobbyDescription.LobbyFlag.FromSavedGame;
			}
			if (this.Version != Amplitude.Unity.Framework.Application.Version)
			{
				this.Flags |= MenuJoinGameScreen.LobbyDescription.LobbyFlag.VersionMismatch;
			}
			if (this.SessionMode == SessionMode.Protected)
			{
				this.Flags |= MenuJoinGameScreen.LobbyDescription.LobbyFlag.FriendsOnly;
			}
			Steamworks.SteamID steamID = new Steamworks.SteamID(this.SteamIDOwner);
			if (steamID.IsValid)
			{
				Steamworks.EFriendRelationship friendRelationship = Steamworks.SteamAPI.SteamFriends.GetFriendRelationship(steamID);
				if (friendRelationship == Steamworks.EFriendRelationship.k_EFriendRelationshipFriend)
				{
					this.Flags |= MenuJoinGameScreen.LobbyDescription.LobbyFlag.FriendlyLobby;
				}
			}
			this.Flags |= MenuJoinGameScreen.LobbyDescription.LobbyFlag.RuntimeConfigurationMismatch;
			IRuntimeService service = Services.GetService<IRuntimeService>();
			if (service != null && service.Runtime != null && service.Runtime.RuntimeModules != null && this.RuntimeConfiguration != null)
			{
				string[] array = (from runtimeModule in service.Runtime.RuntimeModules
				select runtimeModule.Name).ToArray<string>();
				string[] array2 = array.Except(this.RuntimeConfiguration).ToArray<string>();
				string[] array3 = this.RuntimeConfiguration.Except(array).ToArray<string>();
				if (array2.Length + array3.Length == 0)
				{
					this.Flags &= ~MenuJoinGameScreen.LobbyDescription.LobbyFlag.RuntimeConfigurationMismatch;
				}
			}
		}

		private T TryGetLobbyData<T>(ISteamMatchMakingService matchMakingService, Steamworks.SteamID steamIDLobby, string lobbyDataKey, bool validationRequired = true)
		{
			string lobbyData = matchMakingService.SteamMatchMaking.GetLobbyData(steamIDLobby, lobbyDataKey);
			if (!string.IsNullOrEmpty(lobbyData))
			{
				try
				{
					if (typeof(T).IsEnum)
					{
						return (T)((object)Enum.Parse(typeof(T), lobbyData));
					}
					object obj = Convert.ChangeType(lobbyData, typeof(T));
					return (T)((object)obj);
				}
				catch
				{
				}
			}
			if (validationRequired)
			{
				this.IsValid = false;
			}
			return default(T);
		}

		[Flags]
		public enum LobbyFlag
		{
			Null = 0,
			FriendsOnly = 1,
			FriendlyLobby = 2,
			VersionMismatch = 4,
			ModdedServer = 8,
			ModMismatch = 16,
			FromSavedGame = 32,
			GameInProgress = 64,
			GameHasEnded = 128,
			GameIsMigrating = 256,
			RuntimeConfigurationMismatch = 512
		}
	}
}
