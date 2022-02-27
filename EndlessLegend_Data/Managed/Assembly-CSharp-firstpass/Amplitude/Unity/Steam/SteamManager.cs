using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Amplitude.Interop;
using Amplitude.Unity.Framework;

namespace Amplitude.Unity.Steam
{
	[Diagnostics.TagAttribute("Steam")]
	public class SteamManager : Manager, IService, ISteamClientService, ISteamMatchMakingService, ISteamServerService, ISteamService
	{
		public event EventHandler<SteamGameOverlayActivatedEventArgs> SteamGameOverlayActivated;

		public event EventHandler<SteamGameLobbyJoinRequestedEventArgs> SteamGameLobbyJoinRequested;

		public event EventHandler<SteamGameRichPresenceJoinRequestedEventArgs> SteamGameRichPresenceJoinRequested;

		public event EventHandler<SteamLobbyChatMsgEventArgs> SteamLobbyChatMsg;

		public event EventHandler<SteamLobbyChatUpdateEventArgs> SteamLobbyChatUpdate;

		public event EventHandler<SteamLobbyDataUpdateEventArgs> SteamLobbyDataUpdate;

		public event EventHandler<SteamLobbyEnterEventArgs> SteamLobbyEnter;

		public event EventHandler<SteamLobbyGameCreatedEventArgs> SteamLobbyGameCreated;

		public event EventHandler<SteamLobbyInviteEventArgs> SteamLobbyInvite;

		public event EventHandler<SteamLobbyKickedEventArgs> SteamLobbyKicked;

		public event EventHandler<SteamShutdownRequestedEventArgs> SteamShutdownRequested;

		public event EventHandler<SteamMatchMakingRequestLobbyListEventArgs> SteamMatchMakingRequestLobbyList;

		public event EventHandler<SteamServersConnectedEventArgs> ClientSteamServersConnected;

		public event EventHandler<SteamServerConnectFailureEventArgs> ClientSteamServerConnectFailure;

		public event EventHandler<SteamServersDisconnectedEventArgs> ClientSteamServersDisconnected;

		public event EventHandler<P2PSessionRequestEventArgs> ClientP2PSessionRequest;

		public event EventHandler<P2PSessionConnectFailEventArgs> ClientP2PSessionConnectFail;

		public event EventHandler<PersonaStateChangedEventArgs> ClientPersonaStateChanged;

		public event EventHandler<SteamServersConnectedEventArgs> ServerSteamServersConnected;

		public event EventHandler<SteamServerConnectFailureEventArgs> ServerSteamServerConnectFailure;

		public event EventHandler<SteamServersDisconnectedEventArgs> ServerSteamServersDisconnected;

		public event EventHandler<P2PSessionRequestEventArgs> ServerP2PSessionRequest;

		public event EventHandler<P2PSessionConnectFailEventArgs> ServerP2PSessionConnectFail;

		public event EventHandler<ValidateAuthTicketResponseEventArgs> ValidateAuthTicketResponse;

		public event EventHandler<PersonaStateChangedEventArgs> ServerPersonaStateChanged;

		public event EventHandler<SteamShutdownEventArgs> SteamShutdown;

		private Steamworks.Callbacks SteamCallbacks { get; set; }

		public virtual void RegisterRequestLobbyListCallback(ulong steamAPICall)
		{
			this.SteamCallbacks.Bind(Steamworks.SteamAPI_RegisterRequestLobbyListDelegate(steamAPICall, SteamManager.SteamClientCallbacks.MatchMakingRequestLobbyList));
		}

		protected virtual void OnSteamGameOverlayActivated(SteamGameOverlayActivatedEventArgs e)
		{
			if (this.SteamGameOverlayActivated != null)
			{
				this.SteamGameOverlayActivated(this, e);
			}
		}

		protected virtual void OnSteamGameLobbyJoinRequested(SteamGameLobbyJoinRequestedEventArgs e)
		{
			if (this.SteamGameLobbyJoinRequested != null)
			{
				this.SteamGameLobbyJoinRequested(this, e);
			}
		}

		protected virtual void OnSteamGameRichPresenceJoinRequested(SteamGameRichPresenceJoinRequestedEventArgs e)
		{
			if (this.SteamGameRichPresenceJoinRequested != null)
			{
				this.SteamGameRichPresenceJoinRequested(this, e);
			}
		}

		protected virtual void OnSteamLobbyChatMsg(SteamLobbyChatMsgEventArgs e)
		{
			if (this.SteamLobbyChatMsg != null)
			{
				this.SteamLobbyChatMsg(this, e);
			}
		}

		protected virtual void OnSteamLobbyChatUpdate(SteamLobbyChatUpdateEventArgs e)
		{
			if (this.SteamLobbyChatUpdate != null)
			{
				this.SteamLobbyChatUpdate(this, e);
			}
		}

		protected virtual void OnSteamLobbyDataUpdate(SteamLobbyDataUpdateEventArgs e)
		{
			if (this.SteamLobbyDataUpdate != null)
			{
				this.SteamLobbyDataUpdate(this, e);
			}
		}

		protected virtual void OnSteamLobbyEnter(SteamLobbyEnterEventArgs e)
		{
			if (this.SteamLobbyEnter != null)
			{
				this.SteamLobbyEnter(this, e);
			}
		}

		protected virtual void OnSteamLobbyGameCreated(SteamLobbyGameCreatedEventArgs e)
		{
			if (this.SteamLobbyGameCreated != null)
			{
				this.SteamLobbyGameCreated(this, e);
			}
		}

		protected virtual void OnSteamLobbyInvite(SteamLobbyInviteEventArgs e)
		{
			if (this.SteamLobbyInvite != null)
			{
				this.SteamLobbyInvite(this, e);
			}
		}

		protected virtual void OnSteamLobbyKicked(SteamLobbyKickedEventArgs e)
		{
			if (this.SteamLobbyKicked != null)
			{
				this.SteamLobbyKicked(this, e);
			}
		}

		protected virtual void OnSteamShutdownRequested(SteamShutdownRequestedEventArgs e)
		{
			if (this.SteamShutdownRequested != null)
			{
				this.SteamShutdownRequested(this, e);
			}
		}

		protected virtual void OnSteamMatchMakingRequestLobbyList(SteamMatchMakingRequestLobbyListEventArgs e)
		{
			if (this.SteamMatchMakingRequestLobbyList != null)
			{
				this.SteamMatchMakingRequestLobbyList(this, e);
			}
		}

		protected virtual void OnClientSteamServersConnected(SteamServersConnectedEventArgs e)
		{
			if (this.ClientSteamServersConnected != null)
			{
				this.ClientSteamServersConnected(this, e);
			}
		}

		protected virtual void OnServerSteamServersConnected(SteamServersConnectedEventArgs e)
		{
			if (this.ServerSteamServersConnected != null)
			{
				this.ServerSteamServersConnected(this, e);
			}
		}

		protected virtual void OnClientSteamServerConnectFailure(SteamServerConnectFailureEventArgs e)
		{
			if (this.ClientSteamServerConnectFailure != null)
			{
				this.ClientSteamServerConnectFailure(this, e);
			}
		}

		protected virtual void OnServerSteamServerConnectFailure(SteamServerConnectFailureEventArgs e)
		{
			if (this.ServerSteamServerConnectFailure != null)
			{
				this.ServerSteamServerConnectFailure(this, e);
			}
		}

		protected virtual void OnClientSteamServersDisconnected(SteamServersDisconnectedEventArgs e)
		{
			if (this.ClientSteamServersDisconnected != null)
			{
				this.ClientSteamServersDisconnected(this, e);
			}
		}

		protected virtual void OnServerSteamServersDisconnected(SteamServersDisconnectedEventArgs e)
		{
			if (this.ServerSteamServersDisconnected != null)
			{
				this.ServerSteamServersDisconnected(this, e);
			}
		}

		protected virtual void OnClientP2PSessionRequest(P2PSessionRequestEventArgs e)
		{
			if (this.ClientP2PSessionRequest != null)
			{
				this.ClientP2PSessionRequest(this, e);
			}
		}

		protected virtual void OnServerP2PSessionRequest(P2PSessionRequestEventArgs e)
		{
			if (this.ServerP2PSessionRequest != null)
			{
				this.ServerP2PSessionRequest(this, e);
			}
		}

		protected virtual void OnClientP2PSessionConnectFail(P2PSessionConnectFailEventArgs e)
		{
			if (this.ClientP2PSessionConnectFail != null)
			{
				this.ClientP2PSessionConnectFail(this, e);
			}
		}

		protected virtual void OnServerP2PSessionConnectFail(P2PSessionConnectFailEventArgs e)
		{
			if (this.ServerP2PSessionConnectFail != null)
			{
				this.ServerP2PSessionConnectFail(this, e);
			}
		}

		protected virtual void OnValidateAuthTicketResponse(ValidateAuthTicketResponseEventArgs e)
		{
			if (this.ValidateAuthTicketResponse != null)
			{
				this.ValidateAuthTicketResponse(this, e);
			}
		}

		protected virtual void OnClientPersonaStateChanged(PersonaStateChangedEventArgs e)
		{
			if (this.ClientPersonaStateChanged != null)
			{
				this.ClientPersonaStateChanged(this, e);
			}
		}

		protected virtual void OnIPCFailure(IPCFailureEventArgs e)
		{
			this.isSteamCommunicationEstablished = false;
			Application.Quit();
		}

		protected virtual void OnServerPersonaStateChanged(PersonaStateChangedEventArgs e)
		{
			if (this.ServerPersonaStateChanged != null)
			{
				this.ServerPersonaStateChanged(this, e);
			}
		}

		private IEnumerator InitializeSteamCallbacks()
		{
			if (this.IsSteamRunning)
			{
				base.SetLastError(0, "Initializing the steam client callbacks...");
				this.SteamCallbacks = new Steamworks.Callbacks();
				SteamManager.SteamClientCallbacks.GameOverlayActivated = new Steamworks.GameOverlayActivatedCallback(this.SteamClientCallbacks_GameOverlayActivated);
				SteamManager.SteamClientCallbacks.GameLobbyJoinRequested = new Steamworks.GameLobbyJoinRequestedCallback(this.SteamClientCallbacks_GameLobbyJoinRequested);
				SteamManager.SteamClientCallbacks.GameRichPresenceJoinRequested = new Steamworks.GameRichPresenceJoinRequestedCallback(this.SteamClientCallbacks_GameRichPresenceJoinRequested);
				SteamManager.SteamClientCallbacks.LobbyChatMsg = new Steamworks.LobbyChatMsgCallback(this.SteamClientCallbacks_LobbyChatMsg);
				SteamManager.SteamClientCallbacks.LobbyChatUpdate = new Steamworks.LobbyChatUpdateCallback(this.SteamClientCallbacks_LobbyChatUpdate);
				SteamManager.SteamClientCallbacks.LobbyDataUpdate = new Steamworks.LobbyDataUpdateCallback(this.SteamClientCallbacks_LobbyDataUpdate);
				SteamManager.SteamClientCallbacks.LobbyEnter = new Steamworks.LobbyEnterCallback(this.SteamClientCallbacks_LobbyEnter);
				SteamManager.SteamClientCallbacks.LobbyGameCreated = new Steamworks.LobbyGameCreatedCallback(this.SteamClientCallbacks_LobbyGameCreated);
				SteamManager.SteamClientCallbacks.LobbyInvite = new Steamworks.LobbyInviteCallback(this.SteamClientCallbacks_LobbyInvite);
				SteamManager.SteamClientCallbacks.LobbyKicked = new Steamworks.LobbyKickedCallback(this.SteamClientCallbacks_LobbyKicked);
				SteamManager.SteamClientCallbacks.SteamShutdown = new Steamworks.SteamShutdownCallback(this.SteamClientCallbacks_SteamShutdownRequested);
				SteamManager.SteamClientCallbacks.MatchMakingRequestLobbyList = new Steamworks.RequestLobbyListCallback(this.SteamClientCallbacks_SteamMatchMakingRequestLobbyList);
				SteamManager.SteamClientCallbacks.SteamServersConnected = new Steamworks.SteamServersConnectedCallback(this.SteamClientCallbacks_SteamServersConnected);
				SteamManager.SteamClientCallbacks.SteamServerConnectFailure = new Steamworks.SteamServerConnectFailureCallback(this.SteamClientCallbacks_SteamServerConnectFailure);
				SteamManager.SteamClientCallbacks.SteamServersDisconnected = new Steamworks.SteamServersDisconnectedCallback(this.SteamClientCallbacks_SteamServersDisconnected);
				SteamManager.SteamClientCallbacks.P2PSessionRequest = new Steamworks.P2PSessionRequestCallback(this.SteamClientCallback_P2PSessionRequest);
				SteamManager.SteamClientCallbacks.P2PSessionConnectFail = new Steamworks.P2PSessionConnectFailCallback(this.SteamClientCallback_P2PSessionConnectFail);
				SteamManager.SteamClientCallbacks.PersonaStateChanged = new Steamworks.PersonaStateChangedCallback(this.SteamClientCallback_PersonaStateChanged);
				SteamManager.SteamClientCallbacks.IPCFailure = new Steamworks.IPCFailureCallback(this.SteamClientCallback_IPCFailure);
				SteamManager.SteamServerCallback.SteamServersConnected = new Steamworks.SteamServersConnectedCallback(this.SteamServerCallbacks_SteamServersConnected);
				SteamManager.SteamServerCallback.SteamServerConnectFailure = new Steamworks.SteamServerConnectFailureCallback(this.SteamServerCallbacks_SteamServerConnectFailure);
				SteamManager.SteamServerCallback.SteamServersDisconnected = new Steamworks.SteamServersDisconnectedCallback(this.SteamServerCallbacks_SteamServersDisconnected);
				SteamManager.SteamServerCallback.P2PSessionRequest = new Steamworks.P2PSessionRequestCallback(this.SteamServerCallback_P2PSessionRequest);
				SteamManager.SteamServerCallback.P2PSessionConnectFail = new Steamworks.P2PSessionConnectFailCallback(this.SteamServerCallback_P2PSessionConnectFail);
				SteamManager.SteamServerCallback.ValidateAuthTicketResponse = new Steamworks.ValidateAuthTicketResponseCallback(this.SteamServerCallback_ValidateAuthTicketResponse);
				SteamManager.SteamServerCallback.PersonaStateChanged = new Steamworks.PersonaStateChangedCallback(this.SteamServerCallback_PersonaStateChanged);
				try
				{
					this.SteamCallbacks.Bind(Steamworks.SteamAPI_RegisterGameOverlayActivatedDelegate(SteamManager.SteamClientCallbacks.GameOverlayActivated, false));
					this.SteamCallbacks.Bind(Steamworks.SteamAPI_RegisterGameLobbyJoinRequestedDelegate(SteamManager.SteamClientCallbacks.GameLobbyJoinRequested, false));
					this.SteamCallbacks.Bind(Steamworks.SteamAPI_RegisterGameRichPresenceJoinRequestedDelegate(SteamManager.SteamClientCallbacks.GameRichPresenceJoinRequested, false));
					this.SteamCallbacks.Bind(Steamworks.SteamAPI_RegisterLobbyChatMsgDelegate(SteamManager.SteamClientCallbacks.LobbyChatMsg, false));
					this.SteamCallbacks.Bind(Steamworks.SteamAPI_RegisterLobbyChatUpdateDelegate(SteamManager.SteamClientCallbacks.LobbyChatUpdate, false));
					this.SteamCallbacks.Bind(Steamworks.SteamAPI_RegisterLobbyDataUpdateDelegate(SteamManager.SteamClientCallbacks.LobbyDataUpdate, false));
					this.SteamCallbacks.Bind(Steamworks.SteamAPI_RegisterLobbyEnterDelegate(SteamManager.SteamClientCallbacks.LobbyEnter, false));
					this.SteamCallbacks.Bind(Steamworks.SteamAPI_RegisterLobbyGameCreatedDelegate(SteamManager.SteamClientCallbacks.LobbyGameCreated, false));
					this.SteamCallbacks.Bind(Steamworks.SteamAPI_RegisterLobbyInviteDelegate(SteamManager.SteamClientCallbacks.LobbyInvite, false));
					this.SteamCallbacks.Bind(Steamworks.SteamAPI_RegisterLobbyKickedDelegate(SteamManager.SteamClientCallbacks.LobbyKicked, false));
					this.SteamCallbacks.Bind(Steamworks.SteamAPI_RegisterIPCFailureDelegate(SteamManager.SteamClientCallbacks.IPCFailure, false));
					this.SteamCallbacks.Bind(Steamworks.SteamAPI_RegisterSteamShutdownDelegate(SteamManager.SteamClientCallbacks.SteamShutdown, false));
					this.SteamCallbacks.Bind(Steamworks.SteamAPI_RegisterSteamServersConnectedDelegate(SteamManager.SteamClientCallbacks.SteamServersConnected, false));
					this.SteamCallbacks.Bind(Steamworks.SteamAPI_RegisterSteamServerConnectFailureDelegate(SteamManager.SteamClientCallbacks.SteamServerConnectFailure, false));
					this.SteamCallbacks.Bind(Steamworks.SteamAPI_RegisterSteamServersDisconnectedDelegate(SteamManager.SteamClientCallbacks.SteamServersDisconnected, false));
					this.SteamCallbacks.Bind(Steamworks.SteamAPI_RegisterP2PSessionRequestDelegate(SteamManager.SteamClientCallbacks.P2PSessionRequest, false));
					this.SteamCallbacks.Bind(Steamworks.SteamAPI_RegisterP2PSessionConnectFailDelegate(SteamManager.SteamClientCallbacks.P2PSessionConnectFail, false));
					this.SteamCallbacks.Bind(Steamworks.SteamAPI_RegisterPersonaStateChangedDelegate(SteamManager.SteamClientCallbacks.PersonaStateChanged, false));
					this.SteamCallbacks.Bind(Steamworks.SteamAPI_RegisterSteamServersConnectedDelegate(SteamManager.SteamServerCallback.SteamServersConnected, true));
					this.SteamCallbacks.Bind(Steamworks.SteamAPI_RegisterSteamServerConnectFailureDelegate(SteamManager.SteamServerCallback.SteamServerConnectFailure, true));
					this.SteamCallbacks.Bind(Steamworks.SteamAPI_RegisterSteamServersDisconnectedDelegate(SteamManager.SteamServerCallback.SteamServersDisconnected, true));
					this.SteamCallbacks.Bind(Steamworks.SteamAPI_RegisterP2PSessionRequestDelegate(SteamManager.SteamServerCallback.P2PSessionRequest, true));
					this.SteamCallbacks.Bind(Steamworks.SteamAPI_RegisterP2PSessionConnectFailDelegate(SteamManager.SteamServerCallback.P2PSessionConnectFail, true));
					this.SteamCallbacks.Bind(Steamworks.SteamAPI_RegisterValidateAuthTicketResponseDelegate(SteamManager.SteamServerCallback.ValidateAuthTicketResponse, true));
					this.SteamCallbacks.Bind(Steamworks.SteamAPI_RegisterPersonaStateChangedDelegate(SteamManager.SteamServerCallback.PersonaStateChanged, true));
					this.SteamShutdown = (EventHandler<SteamShutdownEventArgs>)Delegate.Combine(this.SteamShutdown, new EventHandler<SteamShutdownEventArgs>(this.SteamManager_SteamShutdown));
				}
				catch (DllNotFoundException ex)
				{
					DllNotFoundException exception = ex;
					base.SetLastError(-1, "Exception caught! check with the console log for details.");
					throw exception;
				}
				catch
				{
					base.SetLastError(-1);
					throw;
				}
			}
			yield break;
		}

		private void SteamManager_SteamShutdown(object sender, SteamShutdownEventArgs e)
		{
			if (this.SteamCallbacks != null)
			{
				this.SteamCallbacks.UnbindAll();
				this.SteamCallbacks = null;
			}
			SteamManager.SteamClientCallbacks.GameLobbyJoinRequested = null;
			SteamManager.SteamClientCallbacks.GameOverlayActivated = null;
			SteamManager.SteamClientCallbacks.GameRichPresenceJoinRequested = null;
			SteamManager.SteamClientCallbacks.SteamShutdown = null;
		}

		private void SteamClientCallbacks_GameOverlayActivated(ref Steamworks.GameOverlayActivated message)
		{
			Diagnostics.Log("SteamClientCallbacks_GameOverlayActivated");
			this.OnSteamGameOverlayActivated(new SteamGameOverlayActivatedEventArgs(message));
		}

		private void SteamClientCallbacks_GameLobbyJoinRequested(ref Steamworks.GameLobbyJoinRequested message)
		{
			Diagnostics.Log("SteamClientCallbacks_GameLobbyJoinRequested");
			this.OnSteamGameLobbyJoinRequested(new SteamGameLobbyJoinRequestedEventArgs(message));
		}

		private void SteamClientCallbacks_GameRichPresenceJoinRequested(ref Steamworks.GameRichPresenceJoinRequested message)
		{
			Diagnostics.Log("SteamClientCallbacks_GameRichPresenceJoinRequested");
			this.OnSteamGameRichPresenceJoinRequested(new SteamGameRichPresenceJoinRequestedEventArgs(message));
		}

		private void SteamClientCallbacks_LobbyChatMsg(ref Steamworks.LobbyChatMsg message)
		{
			Diagnostics.Log("SteamClientCallbacks_LobbyChatMsg");
			this.OnSteamLobbyChatMsg(new SteamLobbyChatMsgEventArgs(message));
		}

		private void SteamClientCallbacks_LobbyChatUpdate(ref Steamworks.LobbyChatUpdate message)
		{
			Diagnostics.Log("SteamClientCallbacks_LobbyChatUpdate");
			this.OnSteamLobbyChatUpdate(new SteamLobbyChatUpdateEventArgs(message));
		}

		private void SteamClientCallbacks_LobbyDataUpdate(ref Steamworks.LobbyDataUpdate message)
		{
			Diagnostics.Log("SteamClientCallbacks_LobbyDataUpdate");
			this.OnSteamLobbyDataUpdate(new SteamLobbyDataUpdateEventArgs(message));
		}

		private void SteamClientCallbacks_LobbyEnter(ref Steamworks.LobbyEnter message)
		{
			Diagnostics.Log("SteamClientCallbacks_LobbyEnter");
			this.OnSteamLobbyEnter(new SteamLobbyEnterEventArgs(message));
		}

		private void SteamClientCallbacks_LobbyGameCreated(ref Steamworks.LobbyGameCreated message)
		{
			Diagnostics.Log("SteamClientCallbacks_LobbyGameCreated");
			this.OnSteamLobbyGameCreated(new SteamLobbyGameCreatedEventArgs(message));
		}

		private void SteamClientCallbacks_LobbyInvite(ref Steamworks.LobbyInvite message)
		{
			Diagnostics.Log("SteamClientCallbacks_LobbyInvite");
			this.OnSteamLobbyInvite(new SteamLobbyInviteEventArgs(message));
		}

		private void SteamClientCallbacks_LobbyKicked(ref Steamworks.LobbyKicked message)
		{
			Diagnostics.Log("SteamClientCallbacks_LobbyKicked");
			this.OnSteamLobbyKicked(new SteamLobbyKickedEventArgs(message));
		}

		private void SteamClientCallbacks_SteamShutdownRequested(ref Steamworks.SteamShutdown message)
		{
			Diagnostics.LogWarning("SteamClientCallbacks_SteamShutdownRequested");
			this.OnSteamShutdownRequested(new SteamShutdownRequestedEventArgs(message));
		}

		[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "io stands for InputOutput")]
		private void SteamClientCallbacks_SteamMatchMakingRequestLobbyList(ref Steamworks.RequestLobbyList message, bool ioFailure)
		{
			Diagnostics.Log("SteamClientCallbacks_SteamMatchMakingRequestLobbyList");
			this.OnSteamMatchMakingRequestLobbyList(new SteamMatchMakingRequestLobbyListEventArgs(message, ioFailure));
		}

		private void SteamClientCallbacks_SteamServersConnected(ref Steamworks.SteamServersConnected message)
		{
			Diagnostics.Log("SteamClientCallbacks_SteamServersConnected");
			this.OnClientSteamServersConnected(new SteamServersConnectedEventArgs(message));
		}

		private void SteamClientCallbacks_SteamServerConnectFailure(ref Steamworks.SteamServerConnectFailure message)
		{
			Diagnostics.Log("SteamClientCallbacks_SteamServerConnectFailure");
			this.OnClientSteamServerConnectFailure(new SteamServerConnectFailureEventArgs(message));
		}

		private void SteamClientCallbacks_SteamServersDisconnected(ref Steamworks.SteamServersDisconnected message)
		{
			Diagnostics.Log("SteamClientCallbacks_SteamServersDisconnected");
			this.OnClientSteamServersDisconnected(new SteamServersDisconnectedEventArgs(message));
		}

		private void SteamClientCallback_P2PSessionRequest(ref Steamworks.P2PSessionRequest message)
		{
			Diagnostics.Log("SteamClientCallback_P2PSessionRequest");
			this.OnClientP2PSessionRequest(new P2PSessionRequestEventArgs(message));
		}

		private void SteamServerCallbacks_SteamServersConnected(ref Steamworks.SteamServersConnected message)
		{
			Diagnostics.Log("SteamServerCallbacks_SteamServersConnected");
			this.OnServerSteamServersConnected(new SteamServersConnectedEventArgs(message));
		}

		private void SteamServerCallbacks_SteamServerConnectFailure(ref Steamworks.SteamServerConnectFailure message)
		{
			Diagnostics.Log("SteamServerCallbacks_SteamServerConnectFailure");
			this.OnServerSteamServerConnectFailure(new SteamServerConnectFailureEventArgs(message));
		}

		private void SteamServerCallbacks_SteamServersDisconnected(ref Steamworks.SteamServersDisconnected message)
		{
			Diagnostics.Log("SteamServerCallbacks_SteamServersDisconnected");
			this.OnServerSteamServersDisconnected(new SteamServersDisconnectedEventArgs(message));
		}

		private void SteamServerCallback_P2PSessionRequest(ref Steamworks.P2PSessionRequest message)
		{
			Diagnostics.Log("SteamServerCallback_P2PSessionRequest");
			this.OnServerP2PSessionRequest(new P2PSessionRequestEventArgs(message));
		}

		private void SteamServerCallback_ValidateAuthTicketResponse(ref Steamworks.ValidateAuthTicketResponse message)
		{
			Diagnostics.Log("SteamServerCallback_ValidateAuthTicketResponse");
			this.OnValidateAuthTicketResponse(new ValidateAuthTicketResponseEventArgs(message));
		}

		private void SteamServerCallback_P2PSessionConnectFail(ref Steamworks.P2PSessionConnectFail message)
		{
			Diagnostics.Log("SteamServerCallback_P2PSessionConnectFail", new object[]
			{
				message.m_steamIDRemote,
				message.m_eP2PSessionError
			});
			this.OnServerP2PSessionConnectFail(new P2PSessionConnectFailEventArgs(message));
		}

		private void SteamClientCallback_P2PSessionConnectFail(ref Steamworks.P2PSessionConnectFail message)
		{
			Diagnostics.Log("SteamClientCallback_P2PSessionConnectFail", new object[]
			{
				message.m_steamIDRemote,
				message.m_eP2PSessionError
			});
			this.OnClientP2PSessionConnectFail(new P2PSessionConnectFailEventArgs(message));
		}

		private void SteamClientCallback_PersonaStateChanged(ref Steamworks.PersonaStateChange message)
		{
			this.OnClientPersonaStateChanged(new PersonaStateChangedEventArgs(message));
		}

		private void SteamClientCallback_IPCFailure(ref Steamworks.IPCFailure message)
		{
			this.OnIPCFailure(new IPCFailureEventArgs(message));
		}

		private void SteamServerCallback_PersonaStateChanged(ref Steamworks.PersonaStateChange message)
		{
			Diagnostics.Log("SteamServerCallback_PersonaStateChanged");
			this.OnServerPersonaStateChanged(new PersonaStateChangedEventArgs(message));
		}

		public bool IsSteamRunning
		{
			get
			{
				return this.isSteamCommunicationEstablished;
			}
		}

		public Steamworks.SteamApps SteamApps { get; private set; }

		public Steamworks.SteamFriends SteamFriends { get; private set; }

		public Steamworks.SteamMatchMaking SteamMatchMaking { get; private set; }

		public Steamworks.SteamUser SteamUser { get; private set; }

		public override IEnumerator BindServices()
		{
			yield return base.BindServices();
			base.SetLastError(0);
			yield return this.RestartSteamApplicationIfNecessary();
			yield return this.InitializeSteam();
			yield return this.InitializeSteamCallbacks();
			if (base.LastError == 0)
			{
				this.SteamApps = Steamworks.SteamAPI.SteamApps;
				this.SteamFriends = Steamworks.SteamAPI.SteamFriends;
				this.SteamUser = Steamworks.SteamAPI.SteamUser;
				if (this.SteamUser != null && this.SteamFriends != null)
				{
					Application.UserName = this.SteamFriends.GetFriendPersonaName(this.SteamUser.SteamID);
					Diagnostics.Log("Hello '{0}'!", new object[]
					{
						Application.UserName
					});
				}
				Services.AddService<ISteamService>(this);
			}
			if (base.LastError == 0)
			{
				this.SteamMatchMaking = Steamworks.SteamAPI.SteamMatchMaking;
				Services.AddService<ISteamMatchMakingService>(this);
				Services.AddService<ISteamServerService>(this);
				Services.AddService<ISteamClientService>(this);
			}
			yield break;
		}

		protected virtual void OnDestroy()
		{
			this.OnSteamShutdown(new SteamShutdownEventArgs());
			if (this.isSteamCommunicationEstablished)
			{
				Steamworks.SteamAPI.Shutdown();
			}
			Diagnostics.Log("Steam has been shut down.");
		}

		protected virtual void OnSteamShutdown(SteamShutdownEventArgs e)
		{
			if (this.SteamShutdown != null)
			{
				this.SteamShutdown(this, e);
			}
		}

		protected virtual void FixedUpdate()
		{
			if (this.isSteamCommunicationEstablished)
			{
				Steamworks.SteamAPI.RunCallbacks();
				if (Steamworks.SteamAPI.SteamGameServer != null)
				{
					Steamworks.SteamGameServer.RunCallbacks();
				}
			}
		}

		private IEnumerator InitializeSteam()
		{
			base.SetLastError(0, "Initializing the steam API...");
			try
			{
				if (Steamworks.SteamAPI.Init())
				{
					this.isSteamCommunicationEstablished = true;
					yield break;
				}
				Diagnostics.LogError("Failed to initialize the Steam API; aborting...");
				Application.Quit();
				yield break;
			}
			catch (DllNotFoundException ex)
			{
				DllNotFoundException exception = ex;
				base.SetLastError(-1, "Exception caught! check with the console log for details.");
				throw exception;
			}
			catch
			{
				base.SetLastError(-1);
				throw;
			}
			yield break;
		}

		private IEnumerator RestartSteamApplicationIfNecessary()
		{
			yield break;
		}

		private bool isSteamCommunicationEstablished;

		private static class SteamClientCallbacks
		{
			public static Steamworks.GameOverlayActivatedCallback GameOverlayActivated;

			public static Steamworks.GameLobbyJoinRequestedCallback GameLobbyJoinRequested;

			public static Steamworks.GameRichPresenceJoinRequestedCallback GameRichPresenceJoinRequested;

			public static Steamworks.LobbyChatMsgCallback LobbyChatMsg;

			public static Steamworks.LobbyChatUpdateCallback LobbyChatUpdate;

			public static Steamworks.LobbyDataUpdateCallback LobbyDataUpdate;

			public static Steamworks.LobbyEnterCallback LobbyEnter;

			public static Steamworks.LobbyGameCreatedCallback LobbyGameCreated;

			public static Steamworks.LobbyInviteCallback LobbyInvite;

			public static Steamworks.LobbyKickedCallback LobbyKicked;

			public static Steamworks.SteamShutdownCallback SteamShutdown;

			public static Steamworks.RequestLobbyListCallback MatchMakingRequestLobbyList;

			public static Steamworks.SteamServersConnectedCallback SteamServersConnected;

			public static Steamworks.SteamServerConnectFailureCallback SteamServerConnectFailure;

			public static Steamworks.SteamServersDisconnectedCallback SteamServersDisconnected;

			public static Steamworks.P2PSessionRequestCallback P2PSessionRequest;

			public static Steamworks.P2PSessionConnectFailCallback P2PSessionConnectFail;

			public static Steamworks.PersonaStateChangedCallback PersonaStateChanged;

			public static Steamworks.IPCFailureCallback IPCFailure;
		}

		private static class SteamServerCallback
		{
			public static Steamworks.SteamServersConnectedCallback SteamServersConnected;

			public static Steamworks.SteamServerConnectFailureCallback SteamServerConnectFailure;

			public static Steamworks.SteamServersDisconnectedCallback SteamServersDisconnected;

			public static Steamworks.P2PSessionRequestCallback P2PSessionRequest;

			public static Steamworks.P2PSessionConnectFailCallback P2PSessionConnectFail;

			public static Steamworks.ValidateAuthTicketResponseCallback ValidateAuthTicketResponse;

			public static Steamworks.PersonaStateChangedCallback PersonaStateChanged;
		}
	}
}
