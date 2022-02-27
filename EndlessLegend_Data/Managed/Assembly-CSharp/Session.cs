using System;
using Amplitude;
using Amplitude.Interop;
using Amplitude.Unity.Audio;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Localization;
using Amplitude.Unity.Networking;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Session;
using Amplitude.Unity.View;

public class Session : Amplitude.Unity.Session.Session, IGameClientLauncher, IGameServerLauncher
{
	public Session()
	{
		global::Session.timeSynchronizationService = Services.GetService<ITimeSynchronizationService>();
		if (global::Session.timeSynchronizationService == null)
		{
			throw new NullReferenceException("ITimeSynchronizationService");
		}
		this.FiniteStateMachine = new FiniteStateMachine();
		this.FiniteStateMachine.RegisterInitialState(new SessionState_Opening(this));
		this.FiniteStateMachine.RegisterState(new SessionState_Opened(this));
		this.FiniteStateMachine.RegisterState(new SessionState_OpenedAndReady(this));
		this.FiniteStateMachine.RegisterState(new SessionState_OpenedAndCounting(this));
		this.FiniteStateMachine.RegisterState(new SessionState_OpenedAndLaunching(this));
		this.FiniteStateMachine.RegisterState(new SessionState_OpenedAndLaunched(this));
		this.FiniteStateMachine.RegisterState(new SessionState_ClientConnecting(this));
		this.FiniteStateMachine.RegisterState(new SessionState_ClientConnected(this));
		this.FiniteStateMachine.RegisterState(new SessionState_ClientDisconnected(this));
		this.FiniteStateMachine.RegisterState(new SessionState_Synchronizing(this));
		this.FiniteStateMachine.RegisterState(new SessionState_OpenAndWait(this));
		if (this.FiniteStateMachine.InitialStateType != null)
		{
			this.FiniteStateMachine.PostStateChange(this.FiniteStateMachine.InitialStateType, new object[0]);
		}
		Diagnostics.Log("Using late update preemption (TimeSpan = {0}, Frequency = {1}, Enable Diagnostics = {2}).", new object[]
		{
			global::Session.LateUpdatePreemption.TimeSpanInMilliseconds,
			global::Session.LateUpdatePreemption.Frequency,
			global::Session.LateUpdatePreemption.EnableDiagnostics
		});
	}

	public event global::Session.OnLocalPlayerReadyEventHandler OnLocalPlayerReady;

	public event EventHandler<LobbyOwnerChangeEventArgs> LobbyOwnerChange;

	void IGameClientLauncher.Launch()
	{
		if (this.GameClient != null)
		{
			throw new InvalidOperationException("Cannot launch a game client while another is already running.");
		}
		Diagnostics.Log("Launching the game client...");
		this.GameClient = new GameClient(this);
	}

	void IGameServerLauncher.Launch()
	{
		if (!base.IsHosting)
		{
			throw new InvalidOperationException("The session cannot launch a game server when it is not hosting the game lobby.");
		}
		if (this.GameServer != null)
		{
			throw new InvalidOperationException("Cannot launch a game server while another is already running.");
		}
		Diagnostics.Log("Launching the game server...");
		this.GameServer = new GameServer(this);
	}

	public static double Time
	{
		get
		{
			return global::Session.timeSynchronizationService.Time;
		}
	}

	public IGameClient GameClient { get; private set; }

	public IGameServer GameServer { get; private set; }

	public IDumpable GameClientDumper
	{
		get
		{
			return this.GameClient as IDumpable;
		}
	}

	public bool LocalPlayerReady
	{
		get
		{
			return this.localPlayerReady;
		}
		set
		{
			if (this.localPlayerReady != value && this.OnLocalPlayerReady != null)
			{
				this.OnLocalPlayerReady(value);
			}
			this.localPlayerReady = value;
		}
	}

	public bool ReturnToLobby { get; set; }

	private FiniteStateMachine FiniteStateMachine { get; set; }

	public override void OnError(Amplitude.Unity.Session.Session.ErrorLevel errorLevel, string text, int errorCode)
	{
		base.OnError(errorLevel, text, errorCode);
		if (!MessagePanel.Instance.IsVisible)
		{
			string text2 = text;
			if (text2.StartsWith("%"))
			{
				text2 = AgeLocalizer.Instance.LocalizeString(text);
			}
			text2 = text2.Replace("$ErrorCode", errorCode.ToString("X4"));
			MessagePanel.Instance.Show(text2, delegate(object A_1, MessagePanelResultEventArgs A_2)
			{
				if (base.SteamIDLobby == null || !base.SteamIDLobby.IsValid)
				{
					IRuntimeService service = Services.GetService<IRuntimeService>();
					if (service != null)
					{
						Diagnostics.Log("Switching to RuntimeState_OutGame.");
						service.Runtime.FiniteStateMachine.PostStateChange(typeof(RuntimeState_OutGame), new object[0]);
					}
				}
			}, (MessagePanelType)errorLevel);
		}
	}

	public void PostStateChange(Type type, params object[] parameters)
	{
		this.FiniteStateMachine.PostStateChange(type, parameters);
	}

	public override void Update()
	{
		base.Update();
		this.FiniteStateMachine.Update();
		if (this.ReturnToLobby)
		{
			if (this.GameServer != null)
			{
				this.GameServer.Dispose();
				this.GameServer = null;
			}
			if (this.GameClient != null)
			{
				this.GameClient.Dispose();
				this.GameClient = null;
			}
			Amplitude.Unity.View.IViewService service = Services.GetService<Amplitude.Unity.View.IViewService>();
			Diagnostics.Assert(service != null);
			service.PostViewChange(typeof(OutGameView), new object[]
			{
				typeof(MenuNewGameScreen)
			});
			this.PostStateChange(typeof(SessionState_Opened), new object[0]);
			string text = AgeLocalizer.Instance.LocalizeString("%RichPresenceInLobby" + base.SessionMode);
			text = text.Replace("$Name", base.GetLobbyData<string>("name", null));
			Steamworks.SteamAPI.SteamFriends.SetRichPresence("status", text);
			this.SetLobbyData("_Launching", false, true);
			this.SetLobbyData("_GameInProgress", false, true);
			this.SetLobbyData("_GameIsMigrating", false, true);
			this.SetLobbyData("_GameSyncState", SynchronizationState.Unset.ToString(), true);
			base.SteamIDServer = null;
			this.LocalPlayerReady = false;
			this.ReturnToLobby = false;
			return;
		}
		if (this.GameServer != null)
		{
			(this.GameServer as GameServer).Update();
		}
		if (this.GameClient != null)
		{
			(this.GameClient as GameClient).Update();
		}
		if (this.GameServer != null)
		{
			DateTime utcNow = DateTime.UtcNow;
			GameServer gameServer = this.GameServer as GameServer;
			int num = 0;
			while (num < 10000 && gameServer.HasPendingOrder)
			{
				gameServer.UpdateMessageBoxAndProcessOrders();
				if (this.GameClient != null)
				{
					(this.GameClient as GameClient).UpdateMessageBoxAndProcessOrders();
				}
				if (num == 0 || (num + 1) % global::Session.LateUpdatePreemption.Frequency == 0)
				{
					TimeSpan timeSpan = DateTime.UtcNow - utcNow;
					if (timeSpan.TotalMilliseconds >= global::Session.LateUpdatePreemption.TimeSpanInMilliseconds)
					{
						if (global::Session.LateUpdatePreemption.EnableDiagnostics)
						{
							Diagnostics.LogWarning("UpdateMessageBoxAndProcessOrders has been preemptively stopped after having processed {0} order(s) in {1} milliseconds.", new object[]
							{
								num + 1,
								timeSpan.TotalMilliseconds
							});
						}
						break;
					}
				}
				num++;
			}
		}
	}

	public override void SetLobbyData(StaticString key, object data, bool replicate = true)
	{
		if (base.IsHosting && !replicate && !key.ToString().StartsWith("__"))
		{
			object obj;
			if (this.lobbyData.TryGetValue(key, out obj) && data != null && obj.ToString() != data.ToString())
			{
				base.SteamMatchMakingService.SteamMatchMaking.SetLobbyData(base.SteamIDLobby, key, obj.ToString());
				Diagnostics.Log("[ELQC-2566] Fixed Lobby Data'{0}' = '{1}' instead of '{2}'.", new object[]
				{
					key,
					obj.ToString(),
					data.ToString()
				});
			}
			return;
		}
		base.SetLobbyData(key, data, replicate);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			this.FiniteStateMachine.Abort();
			if (this.GameServer != null)
			{
				this.GameServer.Dispose();
				this.GameServer = null;
			}
			if (this.GameClient != null)
			{
				this.GameClient.Dispose();
				this.GameClient = null;
			}
		}
		this.GameClient = null;
		this.GameServer = null;
		base.Dispose(disposing);
	}

	protected override void OnLobbyChatUpdate(LobbyChatUpdateEventArgs e)
	{
		base.OnLobbyChatUpdate(e);
		if (base.SteamIDLobby == e.SteamIDLobby)
		{
			string text = string.Empty;
			if ((e.ChatMemberStateChange & LobbyChatUpdateStateChange.Entered) == LobbyChatUpdateStateChange.Entered)
			{
				text = "Entered";
				IAudioEventService service = Services.GetService<IAudioEventService>();
				service.Play2DEvent("Gui/Interface/Lobby/EnterLobby");
			}
			else if ((e.ChatMemberStateChange & LobbyChatUpdateStateChange.Left) == LobbyChatUpdateStateChange.Left)
			{
				text = "Left";
				IAudioEventService service2 = Services.GetService<IAudioEventService>();
				service2.Play2DEvent("Gui/Interface/Lobby/QuitLobby");
			}
			else if ((e.ChatMemberStateChange & LobbyChatUpdateStateChange.Disconnected) == LobbyChatUpdateStateChange.Disconnected)
			{
				text = "Disconnected";
				IAudioEventService service3 = Services.GetService<IAudioEventService>();
				service3.Play2DEvent("Gui/Interface/Lobby/QuitLobby");
			}
			else if ((e.ChatMemberStateChange & LobbyChatUpdateStateChange.Kicked) == LobbyChatUpdateStateChange.Kicked)
			{
				text = "Kicked";
				IAudioEventService service4 = Services.GetService<IAudioEventService>();
				service4.Play2DEvent("Gui/Interface/Lobby/QuitLobby");
			}
			if (!string.IsNullOrEmpty(text))
			{
				ILocalizationService service5 = Services.GetService<ILocalizationService>();
				if (service5 != null)
				{
					string text2 = string.Format("%LobbyChat{0}", text);
					if (e.SteamIDUserChanged == base.SteamIDUser)
					{
						text2 += "+";
					}
					string friendPersonaName = Steamworks.SteamAPI.SteamFriends.GetFriendPersonaName(e.SteamIDUserChanged);
					string text3 = service5.Localize(text2).ToString().Replace("$Player", friendPersonaName);
					string newValue = string.Empty;
					if (e.SteamIDMakingChanges.IsValid)
					{
						newValue = Steamworks.SteamAPI.SteamFriends.GetFriendPersonaName(e.SteamIDMakingChanges);
						text3 = text3.Replace("$ChangingPlayer", newValue);
					}
					if (!string.IsNullOrEmpty(text3))
					{
						base.SendLocalChatMessage("s:/" + text3);
					}
				}
			}
		}
	}

	protected override void OnLobbyOwnerChange(LobbyOwnerChangeEventArgs e)
	{
		base.OnLobbyOwnerChange(e);
		if (e.SteamIDLobbyOwner == base.SteamIDUser)
		{
			ILocalizationService service = Services.GetService<ILocalizationService>();
			if (service != null)
			{
				base.SendLocalChatMessage("s:/" + service.Localize("%LobbyOwnerChange+"));
			}
			this.SetLobbyData("_Ready", new global::Session.LobbyReadyState(false, 0.0), true);
			this.SetLobbyData("_Launching", false, true);
			this.SetLobbyData("Owner", base.SteamIDUser, true);
			this.SetLobbyData("owner", base.SteamIDUser, true);
		}
		if (this != null)
		{
			this.LobbyOwnerChange(this, e);
		}
	}

	protected override void OnSessionChange(SessionChangeEventArgs e)
	{
		base.OnSessionChange(e);
		switch (e.Action)
		{
		case SessionChangeAction.Opened:
			this.FiniteStateMachine.PostStateChange(typeof(SessionState_Opened), new object[0]);
			break;
		}
		string text = AgeLocalizer.Instance.LocalizeString("%RichPresenceInLobby" + base.SessionMode);
		text = text.Replace("$Name", base.GetLobbyData<string>("name", null));
		Steamworks.SteamAPI.SteamFriends.SetRichPresence("status", text);
	}

	private static ITimeSynchronizationService timeSynchronizationService;

	private bool localPlayerReady;

	public static class LateUpdatePreemption
	{
		public static double TimeSpanInMilliseconds = 6.66;

		public static int Frequency = 5;

		public static bool EnableDiagnostics;
	}

	public class LobbyReadyState
	{
		public LobbyReadyState()
		{
			this.IsReady = false;
			this.LaunchTime = -1.0;
		}

		public LobbyReadyState(bool isReady, double launchTime = -1.0)
		{
			this.IsReady = isReady;
			this.LaunchTime = launchTime;
		}

		public bool IsReady { get; private set; }

		public double LaunchTime { get; private set; }

		public static global::Session.LobbyReadyState Parse(string lobbyData)
		{
			if (string.IsNullOrEmpty(lobbyData))
			{
				return new global::Session.LobbyReadyState();
			}
			string[] array = lobbyData.Split(Amplitude.String.Separators);
			if (array.Length != 2)
			{
				throw new ArgumentException();
			}
			bool isReady;
			double launchTime;
			if (!bool.TryParse(array[0], out isReady) || !double.TryParse(array[1], out launchTime))
			{
				throw new ArgumentException();
			}
			return new global::Session.LobbyReadyState(isReady, launchTime);
		}

		public override string ToString()
		{
			return string.Format("{0};{1}", this.IsReady, this.LaunchTime);
		}
	}

	public delegate void OnLocalPlayerReadyEventHandler(bool ready);
}
