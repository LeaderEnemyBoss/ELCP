using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Amplitude.Extensions;

namespace Amplitude.Interop
{
	public class Steamworks
	{
		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_OnAchievementStoredDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.AchievementStoredCallback callback);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_OnUserStatsReceivedDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.UserStatsReceivedCallback callback);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_OnUserStatsStoredDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.UserStatsStoredCallback callback);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterGameOverlayActivatedDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.GameOverlayActivatedCallback callback, bool server);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterGameLobbyJoinRequestedDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.GameLobbyJoinRequestedCallback callback, bool server);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterGameRichPresenceJoinRequestedDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.GameRichPresenceJoinRequestedCallback callback, bool server);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterGSPolicyResponseDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.GSPolicyResponseCallback callback, bool server);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterIPCFailureDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.IPCFailureCallback callback, bool server);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterLobbyDataUpdateDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.LobbyDataUpdateCallback callback, bool server);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterLobbyChatMsgDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.LobbyChatMsgCallback callback, bool server);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterLobbyChatUpdateDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.LobbyChatUpdateCallback callback, bool server);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterLobbyEnterDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.LobbyEnterCallback callback, bool server);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterLobbyGameCreatedDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.LobbyGameCreatedCallback callback, bool server);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterLobbyInviteDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.LobbyInviteCallback callback, bool server);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterLobbyKickedDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.LobbyKickedCallback callback, bool server);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterP2PSessionConnectFailDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.P2PSessionConnectFailCallback callback, bool server);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterP2PSessionRequestDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.P2PSessionRequestCallback callback, bool server);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterPersonaStateChangedDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.PersonaStateChangedCallback callback, bool server);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterSteamServersConnectedDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.SteamServersConnectedCallback callback, bool server);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterSteamServerConnectFailureDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.SteamServerConnectFailureCallback callback, bool server);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterSteamServersDisconnectedDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.SteamServersDisconnectedCallback callback, bool server);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterSteamShutdownDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.SteamShutdownCallback callback, bool server);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterValidateAuthTicketResponseDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.ValidateAuthTicketResponseCallback callback, bool server);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterUserStatsReceivedDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.UserStatsReceivedCallback callback, bool server);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern void SteamAPI_UnregisterDelegate(HandleRef handle);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern bool AmplitudeWrapper_IsIDValid(ulong steamId);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterLobbyCreatedDelegate(ulong steamAPICall, [MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.LobbyCreatedCallback callback);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterRequestLobbyListDelegate(ulong steamAPICall, [MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.RequestLobbyListCallback callback);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterRemoteStoragePublishedFileSubscribedDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.RemoteStoragePublishedFileSubscribedCallback callback, bool server);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterRemoteStoragePublishedFileUnsubscribedDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.RemoteStoragePublishedFileUnsubscribedCallback callback, bool server);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterCreateItemDelegate(ulong steamAPICall, [MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.CreateItemCallback callback);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterSteamUGCQueryCompletedDelegate(ulong steamAPICall, [MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.SteamUGCQueryCompletedCallback callback);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterSubmitItemUpdateDelegate(ulong steamAPICall, [MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.SubmitItemUpdateCallback callback);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterDownloadItemResultDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.DownloadItemResultCallback callback, bool server);

		[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr SteamAPI_RegisterItemInstalledDelegate([MarshalAs(UnmanagedType.FunctionPtr)] Steamworks.ItemInstalledCallback callback, bool server);

		public const uint k_uAppIdInvalid = 0u;

		public const int k_iSteamUserCallbacks = 100;

		public const int k_iSteamGameServerCallbacks = 200;

		public const int k_iSteamFriendsCallbacks = 300;

		public const int k_iSteamBillingCallbacks = 400;

		public const int k_iSteamMatchmakingCallbacks = 500;

		public const int k_iSteamContentServerCallbacks = 600;

		public const int k_iSteamUtilsCallbacks = 700;

		public const int k_iClientFriendsCallbacks = 800;

		public const int k_iClientUserCallbacks = 900;

		public const int k_iSteamAppsCallbacks = 1000;

		public const int k_iSteamUserStatsCallbacks = 1100;

		public const int k_iSteamNetworkingCallbacks = 1200;

		public const int k_iClientRemoteStorageCallbacks = 1300;

		public const int k_iSteamUserItemsCallbacks = 1400;

		public const int k_iSteamGameServerItemsCallbacks = 1500;

		public const int k_iClientUtilsCallbacks = 1600;

		public const int k_iSteamGameCoordinatorCallbacks = 1700;

		public const int k_iSteamGameServerStatsCallbacks = 1800;

		public const int k_iSteam2AsyncCallbacks = 1900;

		public const int k_iSteamGameStatsCallbacks = 2000;

		public const int k_iClientHTTPCallbacks = 2100;

		public const int k_iClientScreenshotsCallbacks = 2200;

		public const int k_iSteamScreenshotsCallbacks = 2300;

		public const int k_iClientAudioCallbacks = 2400;

		public const int k_iClientUnifiedMessagesCallbacks = 2500;

		public const int k_iSteamStreamLauncherCallbacks = 2600;

		public const int k_iClientControllerCallbacks = 2700;

		public const int k_iSteamControllerCallbacks = 2800;

		public const int k_iClientParentalSettingsCallbacks = 2900;

		public const int k_iClientDeviceAuthCallbacks = 3000;

		public const int k_iClientNetworkDeviceManagerCallbacks = 3100;

		public const int k_iClientMusicCallbacks = 3200;

		public const int k_iClientRemoteClientManagerCallbacks = 3300;

		public const int k_iClientUGCCallbacks = 3400;

		public const int k_iSteamStreamClientCallbacks = 3500;

		public const int k_IClientProductBuilderCallbacks = 3600;

		public const int k_iClientShortcutsCallbacks = 3700;

		public const int k_iClientRemoteControlManagerCallbacks = 3800;

		public const int k_iSteamAppListCallbacks = 3900;

		public const int k_iSteamMusicCallbacks = 4000;

		public const int k_iSteamMusicRemoteCallbacks = 4100;

		public const int k_iClientVRCallbacks = 4200;

		public const int k_iClientReservedCallbacks = 4300;

		public const int k_iSteamReservedCallbacks = 4400;

		public const int k_iSteamHTMLSurfaceCallbacks = 4500;

		public const int k_iClientVideoCallbacks = 4600;

		public const int k_iClientInventoryCallbacks = 4700;

		public const uint k_cubChatMetadataMax = 8192u;

		public const int k_cchMaxRichPresenceKeys = 20;

		public const int k_cchMaxRichPresenceKeyLength = 64;

		public const int k_cchMaxRichPresenceValueLength = 256;

		public const ushort MASTERSERVERUPDATERPORT_USEGAMESOCKETSHARE = 65535;

		private const int k_cchStatNameMax = 128;

		internal const string Dll = "steam_api_dotnetwrapper64";

		internal const int Packing = 8;

		public static class SteamAPI
		{
			public static bool IsSteamRunning
			{
				get
				{
					return Steamworks.SteamAPI.SteamAPI_IsSteamRunning();
				}
			}

			public static Steamworks.SteamApps SteamApps
			{
				get
				{
					IntPtr intPtr = Steamworks.SteamAPI.SteamAPI_SteamApps();
					return (!(IntPtr.Zero != intPtr)) ? null : new Steamworks.SteamApps(intPtr);
				}
			}

			public static Steamworks.SteamClient SteamClient
			{
				get
				{
					IntPtr intPtr = Steamworks.SteamAPI.SteamAPI_SteamClient();
					return (!(IntPtr.Zero != intPtr)) ? null : new Steamworks.SteamClient(intPtr);
				}
			}

			public static Steamworks.SteamFriends SteamFriends
			{
				get
				{
					IntPtr intPtr = Steamworks.SteamAPI.SteamAPI_SteamFriends();
					return (!(IntPtr.Zero != intPtr)) ? null : new Steamworks.SteamFriends(intPtr);
				}
			}

			public static Steamworks.SteamGameServer SteamGameServer
			{
				get
				{
					IntPtr intPtr = Steamworks.SteamAPI.SteamAPI_SteamGameServer();
					return (!(IntPtr.Zero != intPtr)) ? null : new Steamworks.SteamGameServer(intPtr);
				}
			}

			public static Steamworks.SteamNetworking SteamGameServerNetworking
			{
				get
				{
					IntPtr intPtr = Steamworks.SteamAPI.SteamAPI_SteamGameServerNetworking();
					return (!(IntPtr.Zero != intPtr)) ? null : new Steamworks.SteamNetworking(intPtr);
				}
			}

			public static Steamworks.SteamMatchMaking SteamMatchMaking
			{
				get
				{
					IntPtr intPtr = Steamworks.SteamAPI.SteamAPI_SteamMatchMaking();
					return (!(IntPtr.Zero != intPtr)) ? null : new Steamworks.SteamMatchMaking(intPtr);
				}
			}

			public static Steamworks.SteamMatchMakingServers SteamMatchMakingServers
			{
				get
				{
					IntPtr intPtr = Steamworks.SteamAPI.SteamAPI_SteamMatchMakingServers();
					return (!(IntPtr.Zero != intPtr)) ? null : new Steamworks.SteamMatchMakingServers(intPtr);
				}
			}

			public static Steamworks.SteamNetworking SteamNetworking
			{
				get
				{
					IntPtr intPtr = Steamworks.SteamAPI.SteamAPI_SteamNetworking();
					return (!(IntPtr.Zero != intPtr)) ? null : new Steamworks.SteamNetworking(intPtr);
				}
			}

			public static Steamworks.SteamRemoteStorage SteamRemoteStorage
			{
				get
				{
					IntPtr intPtr = Steamworks.SteamAPI.SteamAPI_SteamRemoteStorage();
					return (!(IntPtr.Zero != intPtr)) ? null : new Steamworks.SteamRemoteStorage(intPtr);
				}
			}

			public static Steamworks.SteamUGC SteamUGC
			{
				get
				{
					IntPtr intPtr = Steamworks.SteamAPI.SteamAPI_SteamUGC();
					return (!(IntPtr.Zero != intPtr)) ? null : new Steamworks.SteamUGC(intPtr);
				}
			}

			public static Steamworks.SteamUser SteamUser
			{
				get
				{
					IntPtr intPtr = Steamworks.SteamAPI.SteamAPI_SteamUser();
					return (!(IntPtr.Zero != intPtr)) ? null : new Steamworks.SteamUser(intPtr);
				}
			}

			public static Steamworks.SteamUserStats SteamUserStats
			{
				get
				{
					IntPtr intPtr = Steamworks.SteamAPI.SteamAPI_SteamUserStats();
					return (!(IntPtr.Zero != intPtr)) ? null : new Steamworks.SteamUserStats(intPtr);
				}
			}

			public static Steamworks.SteamUtils SteamUtils
			{
				get
				{
					IntPtr intPtr = Steamworks.SteamAPI.SteamAPI_SteamUtils();
					return (!(IntPtr.Zero != intPtr)) ? null : new Steamworks.SteamUtils(intPtr);
				}
			}

			public static string GetSteamInstallPath()
			{
				IntPtr ptr = Steamworks.SteamAPI.SteamAPI_GetSteamInstallPath();
				return Marshal.PtrToStringAnsi(ptr);
			}

			public static bool Init()
			{
				return Steamworks.SteamAPI.SteamAPI_Init();
			}

			public static bool RestartAppIfNecessary(uint unOwnAppID)
			{
				return Steamworks.SteamAPI.SteamAPI_RestartAppIfNecessary(unOwnAppID);
			}

			public static void RunCallbacks()
			{
				Steamworks.SteamAPI.SteamAPI_RunCallbacks();
			}

			public static void Shutdown()
			{
				Steamworks.SteamAPI.SteamAPI_Shutdown();
			}

			[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr SteamAPI_GetSteamInstallPath();

			[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool SteamAPI_Init();

			[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool SteamAPI_IsSteamRunning();

			[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool SteamAPI_RestartAppIfNecessary(uint unOwnAppID);

			[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SteamAPI_RunCallbacks();

			[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SteamAPI_Shutdown();

			[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamApps")]
			internal static extern IntPtr SteamAPI_SteamApps();

			[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamClient")]
			internal static extern IntPtr SteamAPI_SteamClient();

			[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamFriends")]
			internal static extern IntPtr SteamAPI_SteamFriends();

			[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamGameServer")]
			internal static extern IntPtr SteamAPI_SteamGameServer();

			[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamGameServerNetworking")]
			internal static extern IntPtr SteamAPI_SteamGameServerNetworking();

			[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamMatchmakingServers")]
			internal static extern IntPtr SteamAPI_SteamMatchMakingServers();

			[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamMatchmaking")]
			internal static extern IntPtr SteamAPI_SteamMatchMaking();

			[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamNetworking")]
			internal static extern IntPtr SteamAPI_SteamNetworking();

			[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamRemoteStorage")]
			internal static extern IntPtr SteamAPI_SteamRemoteStorage();

			[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamUGC")]
			internal static extern IntPtr SteamAPI_SteamUGC();

			[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamUser")]
			internal static extern IntPtr SteamAPI_SteamUser();

			[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamUserStats")]
			internal static extern IntPtr SteamAPI_SteamUserStats();

			[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SteamUtils")]
			internal static extern IntPtr SteamAPI_SteamUtils();

			internal const string Dll = "steam_api64";
		}

		public class Callback : IDisposable
		{
			public Callback(IntPtr ptr)
			{
				this.handle = new HandleRef(this, ptr);
			}

			~Callback()
			{
				this.Dispose(false);
			}

			public void Dispose()
			{
				this.Dispose(true);
			}

			protected virtual void Dispose(bool disposing)
			{
				if (!this.disposed)
				{
					if (disposing)
					{
						Steamworks.SteamAPI_UnregisterDelegate(this.handle);
						this.handle = default(HandleRef);
					}
					this.disposed = true;
				}
			}

			private HandleRef handle;

			private bool disposed;
		}

		public class Callbacks
		{
			public IntPtr Bind(IntPtr ptr)
			{
				if (IntPtr.Zero != ptr)
				{
					this.callbacks.Add(new Steamworks.Callback(ptr));
				}
				return ptr;
			}

			public void Unbind()
			{
			}

			public void UnbindAll()
			{
				foreach (Steamworks.Callback callback in this.callbacks)
				{
					callback.Dispose();
				}
				this.callbacks.Clear();
			}

			private List<Steamworks.Callback> callbacks = new List<Steamworks.Callback>();
		}

		public class CallResult : IDisposable
		{
			public CallResult(IntPtr ptr)
			{
				this.handle = new HandleRef(this, ptr);
			}

			~CallResult()
			{
				this.Dispose(false);
			}

			public void Dispose()
			{
				this.Dispose(true);
			}

			protected virtual void Dispose(bool disposing)
			{
				if (!this.disposed)
				{
					if (disposing)
					{
						Steamworks.SteamAPI_UnregisterDelegate(this.handle);
						this.handle = default(HandleRef);
					}
					this.disposed = true;
				}
			}

			private HandleRef handle;

			private bool disposed;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct DlcInstalled
		{
			public const int k_iCallback = 1005;

			public uint m_nAppID;
		}

		public class SteamApps
		{
			internal SteamApps(IntPtr ptr)
			{
				this.handle = new HandleRef(this, ptr);
			}

			private IntPtr Handle
			{
				get
				{
					return this.handle.Handle;
				}
			}

			public bool BIsDlcInstalled(uint downloadableContentID)
			{
				return Steamworks.SteamApps.ISteamApps_BIsDlcInstalled(this.Handle, downloadableContentID);
			}

			public bool BIsSubscribedApp(uint downloadableContentID)
			{
				return Steamworks.SteamApps.ISteamApps_BIsSubscribedApp(this.Handle, downloadableContentID);
			}

			public string GetAvailableGameLanguages()
			{
				IntPtr ptr = Steamworks.SteamApps.ISteamApps_GetAvailableGameLanguages(this.Handle);
				return Marshal.PtrToStringAnsi(ptr);
			}

			public string GetCurrentGameLanguage()
			{
				IntPtr ptr = Steamworks.SteamApps.ISteamApps_GetCurrentGameLanguage(this.Handle);
				return Marshal.PtrToStringAnsi(ptr);
			}

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamApps_BIsDlcInstalled(IntPtr ptr, uint appID);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamApps_BIsSubscribedApp(IntPtr ptr, uint appID);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern IntPtr ISteamApps_GetAvailableGameLanguages(IntPtr ptr);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern IntPtr ISteamApps_GetCurrentGameLanguage(IntPtr ptr);

			private HandleRef handle;
		}

		public class SteamClient
		{
			internal SteamClient(IntPtr ptr)
			{
				this.handle = new HandleRef(this, ptr);
			}

			private IntPtr Handle
			{
				get
				{
					return this.handle.Handle;
				}
			}

			public void SetWarningMessageHook(Steamworks.SteamClient.SetWarningMessageHookCallback callback)
			{
				Steamworks.SteamClient.ISteamClient_SetWarningMessageHook(this.Handle, callback);
			}

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamClient_SetWarningMessageHook(IntPtr ptr, Steamworks.SteamClient.SetWarningMessageHookCallback callback);

			private HandleRef handle;

			[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
			public delegate void SetWarningMessageHookCallback(int severity, [MarshalAs(UnmanagedType.LPStr)] string message);
		}

		public enum EFriendRelationship
		{
			k_EFriendRelationshipNone,
			k_EFriendRelationshipBlocked,
			k_EFriendRelationshipRequestRecipient,
			k_EFriendRelationshipFriend,
			k_EFriendRelationshipRequestInitiator,
			k_EFriendRelationshipIgnored,
			k_EFriendRelationshipIgnoredFriend,
			k_EFriendRelationshipSuggested
		}

		[Flags]
		public enum EFriendFlags
		{
			k_EFriendFlagNone = 0,
			k_EFriendFlagBlocked = 1,
			k_EFriendFlagFriendshipRequested = 2,
			k_EFriendFlagImmediate = 4,
			k_EFriendFlagClanMember = 8,
			k_EFriendFlagOnGameServer = 16,
			k_EFriendFlagRequestingFriendship = 128,
			k_EFriendFlagRequestingInfo = 256,
			k_EFriendFlagIgnored = 512,
			k_EFriendFlagIgnoredFriend = 1024,
			k_EFriendFlagSuggested = 2048,
			k_EFriendFlagAll = 65535
		}

		public enum EOverlayToStoreFlag
		{
			k_EOverlayToStoreFlag_None,
			k_EOverlayToStoreFlag_AddToCart,
			k_EOverlayToStoreFlag_AddToCartAndShow
		}

		[Flags]
		public enum EPersonaChange
		{
			k_EPersonaChangeName = 1,
			k_EPersonaChangeStatus = 2,
			k_EPersonaChangeComeOnline = 4,
			k_EPersonaChangeGoneOffline = 8,
			k_EPersonaChangeGamePlayed = 16,
			k_EPersonaChangeGameServer = 32,
			k_EPersonaChangeAvatar = 64,
			k_EPersonaChangeJoinedSource = 128,
			k_EPersonaChangeLeftSource = 256,
			k_EPersonaChangeRelationshipChanged = 512,
			k_EPersonaChangeNameFirstSet = 1024,
			k_EPersonaChangeFacebookInfo = 2048
		}

		public enum EPersonaState
		{
			k_EPersonaStateOffline,
			k_EPersonaStateOnline,
			k_EPersonaStateBusy,
			k_EPersonaStateAway,
			k_EPersonaStateSnooze,
			k_EPersonaStateLookingToTrade,
			k_EPersonaStateLookingToPlay,
			k_EPersonaStateMax
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct GameOverlayActivated
		{
			public const int k_iCallback = 331;

			public byte m_bActive;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct GameLobbyJoinRequested
		{
			public const int k_iCallback = 333;

			public ulong m_steamIDLobby;

			public ulong m_steamIDFriend;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct GameRichPresenceJoinRequested
		{
			public const int k_iCallback = 337;

			public ulong m_steamIDFriend;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string m_rgchConnect;
		}

		public class SteamFriends
		{
			internal SteamFriends(IntPtr ptr)
			{
				this.handle = new HandleRef(this, ptr);
			}

			private IntPtr Handle
			{
				get
				{
					return this.handle.Handle;
				}
			}

			public void ActivateGameOverlay(string pchDialog)
			{
				Steamworks.SteamFriends.ISteamFriends_ActivateGameOverlay(this.Handle, pchDialog);
			}

			public void ActivateGameOverlayInviteDialog(Steamworks.SteamID steamIDLobby)
			{
				Steamworks.SteamFriends.ISteamFriends_ActivateGameOverlayInviteDialog(this.Handle, steamIDLobby);
			}

			public void ActivateGameOverlayToStore(uint nAppId, Steamworks.EOverlayToStoreFlag eFlag)
			{
				Steamworks.SteamFriends.ISteamFriends_ActivateGameOverlayToStore(this.Handle, nAppId, (int)eFlag);
			}

			public void ActivateGameOverlayToWebPage(string pchURL)
			{
				Steamworks.SteamFriends.ISteamFriends_ActivateGameOverlayToWebPage(this.Handle, pchURL);
			}

			public Steamworks.SteamID GetFriendByIndex(int friendIndex, Steamworks.EFriendFlags friendFlags)
			{
				return new Steamworks.SteamID(Steamworks.SteamFriends.ISteamFriends_GetFriendByIndex(this.Handle, friendIndex, (int)friendFlags));
			}

			public int GetFriendCount(Steamworks.EFriendFlags friendFlags)
			{
				return Steamworks.SteamFriends.ISteamFriends_GetFriendCount(this.Handle, (int)friendFlags);
			}

			public string GetFriendPersonaName(Steamworks.SteamID steamID)
			{
				IntPtr ptr = Steamworks.SteamFriends.ISteamFriends_GetFriendPersonaName(this.Handle, steamID);
				return Marshal.PtrToStringAnsi(ptr);
			}

			public Steamworks.EPersonaState GetFriendPersonaState(Steamworks.SteamID steamIDFriend)
			{
				return Steamworks.SteamFriends.ISteamFriends_GetFriendPersonaState(this.Handle, steamIDFriend);
			}

			public Steamworks.EFriendRelationship GetFriendRelationship(Steamworks.SteamID steamID)
			{
				return Steamworks.SteamFriends.ISteamFriends_GetFriendRelationship(this.Handle, steamID);
			}

			public bool RequestUserInformation(Steamworks.SteamID steamIDUser, bool requireNameOnly)
			{
				return Steamworks.SteamFriends.ISteamFriends_RequestUserInformation(this.Handle, steamIDUser, requireNameOnly);
			}

			public bool SetRichPresence(string pchKey, string pchValue)
			{
				return Steamworks.SteamFriends.ISteamFriends_SetRichPresence(this.Handle, pchKey, pchValue);
			}

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamFriends_ActivateGameOverlay(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchDialog);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamFriends_ActivateGameOverlayInviteDialog(IntPtr ptr, ulong steamIDLobby);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamFriends_ActivateGameOverlayToStore(IntPtr ptr, uint nAppId, int eFlag);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamFriends_ActivateGameOverlayToWebPage(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchURL);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamFriends_GetFriendByIndex(IntPtr ptr, int iFriend, int iFriendFlag);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern int ISteamFriends_GetFriendCount(IntPtr ptr, int iFriendFlag);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern IntPtr ISteamFriends_GetFriendPersonaName(IntPtr ptr, ulong steamID);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern Steamworks.EPersonaState ISteamFriends_GetFriendPersonaState(IntPtr ptr, ulong steamIDFriend);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern Steamworks.EFriendRelationship ISteamFriends_GetFriendRelationship(IntPtr ptr, ulong steamID);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamFriends_RequestUserInformation(IntPtr ptr, ulong steamIDUser, bool bRequireNameOnly);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamFriends_SetRichPresence(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchKey, [MarshalAs(UnmanagedType.LPStr)] string pchValue);

			private HandleRef handle;
		}

		public enum EServerMode
		{
			eServerModeInvalid,
			eServerModeNoAuthentication,
			eServerModeAuthentication,
			eServerModeAuthenticationAndSecure
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct GSPolicyResponse
		{
			public const int k_iCallback = 115;

			public byte m_bSecure;
		}

		public class SteamGameServer
		{
			internal SteamGameServer(IntPtr ptr)
			{
				this.handle = new HandleRef(this, ptr);
			}

			public Steamworks.SteamID SteamID
			{
				get
				{
					ulong num = Steamworks.SteamGameServer.ISteamGameServer_GetSteamID(this.Handle);
					if (num != 0UL)
					{
						return new Steamworks.SteamID(num);
					}
					return Steamworks.SteamID.Zero;
				}
			}

			private IntPtr Handle
			{
				get
				{
					return this.handle.Handle;
				}
			}

			public static bool Init(uint unIP, ushort usSteamPort, ushort usGamePort, ushort usQueryPort, Steamworks.EServerMode eServerMode, string pchVersionString)
			{
				return Steamworks.SteamGameServer.SteamGameServer_Init(unIP, usSteamPort, usGamePort, usQueryPort, eServerMode, pchVersionString);
			}

			public static void RunCallbacks()
			{
				Steamworks.SteamGameServer.SteamGameServer_RunCallbacks();
			}

			public static void Shutdown()
			{
				Steamworks.SteamGameServer.SteamGameServer_Shutdown();
			}

			public Steamworks.EBeginAuthSessionResult BeginAuthSession(ref byte[] pAuthTicket, uint cbAuthTicket, ulong steamID)
			{
				GCHandle gchandle = GCHandle.Alloc(pAuthTicket, GCHandleType.Pinned);
				Steamworks.EBeginAuthSessionResult result;
				try
				{
					result = Steamworks.SteamGameServer.ISteamGameServer_BeginAuthSession(this.Handle, gchandle.AddrOfPinnedObject(), cbAuthTicket, steamID);
				}
				finally
				{
					gchandle.Free();
				}
				return result;
			}

			public bool BLoggedOn(IntPtr ptr)
			{
				return Steamworks.SteamGameServer.ISteamGameServer_BLoggedOn(ptr);
			}

			public bool BSecure()
			{
				return Steamworks.SteamGameServer.ISteamGameServer_BSecure(this.Handle);
			}

			public bool EnableHeartbeats(bool bActive)
			{
				return Steamworks.SteamGameServer.ISteamGameServer_EnableHeartbeats(this.Handle, bActive);
			}

			public void EndAuthSession(ulong steamID)
			{
				Steamworks.SteamGameServer.ISteamGameServer_EndAuthSession(this.Handle, steamID);
			}

			public uint GetPublicIP()
			{
				return Steamworks.SteamGameServer.ISteamGameServer_GetPublicIP(this.Handle);
			}

			public void LogOn(string pszAccountName)
			{
				Steamworks.SteamGameServer.ISteamGameServer_LogOn(this.Handle, pszAccountName);
			}

			public void LogOnAnonymous()
			{
				Steamworks.SteamGameServer.ISteamGameServer_LogOnAnonymous(this.Handle);
			}

			public void LogOff()
			{
				Steamworks.SteamGameServer.ISteamGameServer_LogOff(this.Handle);
			}

			public void SetGameData(string pchGameData)
			{
				Steamworks.SteamGameServer.ISteamGameServer_SetGameData(this.Handle, pchGameData);
			}

			public void SetGameDescription(string pszGameDescription)
			{
				Steamworks.SteamGameServer.ISteamGameServer_SetGameDescription(this.Handle, pszGameDescription);
			}

			public void SetGameTags(string pchGameTags)
			{
				Steamworks.SteamGameServer.ISteamGameServer_SetGameTags(this.Handle, pchGameTags);
			}

			public void SetKeyValue(string pKey, string pValue)
			{
				Steamworks.SteamGameServer.ISteamGameServer_SetKeyValue(this.Handle, pKey, pValue);
			}

			public void SetMapName(string pchMapName)
			{
				Steamworks.SteamGameServer.ISteamGameServer_SetMapName(this.Handle, pchMapName);
			}

			public void SetMaxPlayerCount(int cPlayersMax)
			{
				Steamworks.SteamGameServer.ISteamGameServer_SetMaxPlayerCount(this.Handle, cPlayersMax);
			}

			public void SetModDir(string pszModDir)
			{
				Steamworks.SteamGameServer.ISteamGameServer_SetModDir(this.Handle, pszModDir);
			}

			public void SetPasswordProtected(bool bPasswordProtected)
			{
				Steamworks.SteamGameServer.ISteamGameServer_SetPasswordProtected(this.Handle, bPasswordProtected);
			}

			public void SetProduct(string pszProduct)
			{
				Steamworks.SteamGameServer.ISteamGameServer_SetProduct(this.Handle, pszProduct);
			}

			public void SetServerName(string pszServerName)
			{
				Steamworks.SteamGameServer.ISteamGameServer_SetServerName(this.Handle, pszServerName);
			}

			[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl)]
			internal static extern bool SteamGameServer_Init(uint unIP, ushort usSteamPort, ushort usGamePort, ushort usQueryPort, [MarshalAs(UnmanagedType.I4)] Steamworks.EServerMode eServerMode, [MarshalAs(UnmanagedType.LPStr)] string pchVersionString);

			[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SteamGameServer_RunCallbacks();

			[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void SteamGameServer_Shutdown();

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern Steamworks.EBeginAuthSessionResult ISteamGameServer_BeginAuthSession(IntPtr ptr, [In] IntPtr pAuthTicket, uint cbAuthTicket, ulong steamID);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamGameServer_BLoggedOn(IntPtr ptr);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamGameServer_BSecure(IntPtr ptr);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamGameServer_EnableHeartbeats(IntPtr ptr, bool bActive);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamGameServer_EndAuthSession(IntPtr ptr, ulong steamID);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern uint ISteamGameServer_GetPublicIP(IntPtr ptr);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamGameServer_GetSteamID(IntPtr ptr);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamGameServer_LogOn(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pszAccountName);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamGameServer_LogOnAnonymous(IntPtr ptr);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamGameServer_LogOff(IntPtr ptr);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamGameServer_SetGameData(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchGameData);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamGameServer_SetGameDescription(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pszGameDescription);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamGameServer_SetGameTags(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchGameTags);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamGameServer_SetKeyValue(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pKey, [MarshalAs(UnmanagedType.LPStr)] string pValue);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamGameServer_SetMapName(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pszMapName);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamGameServer_SetMaxPlayerCount(IntPtr ptr, int cPlayersMax);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamGameServer_SetModDir(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pszModDir);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamGameServer_SetPasswordProtected(IntPtr ptr, bool bPasswordProtected);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamGameServer_SetProduct(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pszProduct);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamGameServer_SetServerName(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pszServerName);

			private HandleRef handle;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct LobbyChatMsg
		{
			public const int k_iCallback = 507;

			public ulong m_ulSteamIDLobby;

			public ulong m_ulSteamIDUser;

			public byte m_eChatEntryType;

			public uint m_iChatID;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct LobbyChatUpdate
		{
			public const int k_iCallback = 506;

			public ulong m_ulSteamIDLobby;

			public ulong m_ulSteamIDUserChanged;

			public ulong m_ulSteamIDMakingChange;

			public uint m_rgfChatMemberStateChange;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct LobbyCreated
		{
			public const int k_iCallback = 513;

			[MarshalAs(UnmanagedType.I4)]
			public Steamworks.EResult m_eResult;

			public ulong m_ulSteamIDLobby;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct LobbyDataUpdate
		{
			public const int k_iCallback = 505;

			public ulong m_ulSteamIDLobby;

			public ulong m_ulSteamIDMember;

			public byte m_bSuccess;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct LobbyEnter
		{
			public const int k_iCallback = 504;

			public ulong m_ulSteamIDLobby;

			public uint m_rgfChatPermissions;

			public bool m_bLocked;

			public uint m_EChatRoomEnterResponse;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct LobbyKicked
		{
			public const int k_iCallback = 512;

			public ulong m_ulSteamIDLobby;

			public ulong m_ulSteamIDAdmin;

			public byte m_bKickedDueToDisconnect;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct LobbyGameCreated
		{
			public const int k_iCallback = 509;

			public ulong m_ulSteamIDLobby;

			public ulong m_ulSteamIDGameServer;

			public uint m_unIP;

			public ushort m_usPort;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct LobbyInvite
		{
			public const int k_iCallback = 503;

			public ulong m_ulSteamIDUser;

			public ulong m_ulSteamIDLobby;

			public ulong m_ulGameID;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct RequestLobbyList
		{
			public const int k_iCallback = 510;

			public uint m_nLobbiesMatching;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct PersonaStateChange
		{
			public const int k_iCallback = 304;

			public ulong m_ulSteamID;

			public int m_nChangeFlags;
		}

		public class SteamMatchMaking
		{
			internal SteamMatchMaking(IntPtr ptr)
			{
				this.handle = new HandleRef(this, ptr);
			}

			private IntPtr Handle
			{
				get
				{
					return this.handle.Handle;
				}
			}

			public void AddRequestLobbyListDistanceFilter(Steamworks.SteamMatchMaking.ELobbyDistanceFilter lobbyDistanceFilter)
			{
				Steamworks.SteamMatchMaking.ISteamMatchmaking_AddRequestLobbyListDistanceFilter(this.Handle, lobbyDistanceFilter);
			}

			public void AddRequestLobbyListResultCountFilter(int cMaxResults)
			{
				Steamworks.SteamMatchMaking.ISteamMatchmaking_AddRequestLobbyListResultCountFilter(this.Handle, cMaxResults);
			}

			public ulong CreateLobby(Steamworks.SteamMatchMaking.ELobbyType lobbyType, int cMaxMembers)
			{
				return Steamworks.SteamMatchMaking.ISteamMatchmaking_CreateLobby(this.Handle, lobbyType, cMaxMembers);
			}

			public bool DeleteLobbyData(Steamworks.SteamID steamIDLobby, string key)
			{
				return Steamworks.SteamMatchMaking.ISteamMatchmaking_DeleteLobbyData(this.Handle, steamIDLobby, key);
			}

			public Steamworks.SteamID GetLobbyByIndex(int index)
			{
				return new Steamworks.SteamID(Steamworks.SteamMatchMaking.ISteamMatchmaking_GetLobbyByIndex(this.Handle, index));
			}

			public int GetLobbyChatEntry(Steamworks.SteamID steamIDLobby, uint chatEntry, out Steamworks.SteamID steamIDUser, ref byte[] buffer)
			{
				ulong value = 0UL;
				int result = Steamworks.SteamMatchMaking.ISteamMatchmaking_GetLobbyChatEntry(this.Handle, steamIDLobby, chatEntry, ref value, Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0), (uint)buffer.Length);
				steamIDUser = new Steamworks.SteamID(value);
				return result;
			}

			public string GetLobbyData(Steamworks.SteamID steamIDLobby, string key)
			{
				IntPtr ptr = Steamworks.SteamMatchMaking.ISteamMatchmaking_GetLobbyData(this.Handle, steamIDLobby, key);
				return Marshal.PtrToStringAnsi(ptr);
			}

			public bool GetLobbyDataByIndex(Steamworks.SteamID steamIDLobby, int index, out string key, out string data)
			{
				GCHandle gchandle = GCHandle.Alloc(Steamworks.SteamMatchMaking.getLobbyDataByIndex_KeyBuffer, GCHandleType.Pinned);
				GCHandle gchandle2 = GCHandle.Alloc(Steamworks.SteamMatchMaking.getLobbyDataByIndex_DataBuffer, GCHandleType.Pinned);
				bool flag = false;
				key = string.Empty;
				data = string.Empty;
				try
				{
					flag = Steamworks.SteamMatchMaking.ISteamMatchmaking_GetLobbyDataByIndex(this.Handle, steamIDLobby, index, gchandle.AddrOfPinnedObject(), Steamworks.SteamMatchMaking.getLobbyDataByIndex_KeyBuffer.Length, gchandle2.AddrOfPinnedObject(), Steamworks.SteamMatchMaking.getLobbyDataByIndex_DataBuffer.Length);
					if (flag)
					{
						key = Marshal.PtrToStringAnsi(gchandle.AddrOfPinnedObject());
						data = Marshal.PtrToStringAnsi(gchandle2.AddrOfPinnedObject());
					}
				}
				finally
				{
					gchandle.Free();
					gchandle2.Free();
				}
				return flag;
			}

			public int GetLobbyDataCount(Steamworks.SteamID steamIDLobby)
			{
				return Steamworks.SteamMatchMaking.ISteamMatchmaking_GetLobbyDataCount(this.Handle, steamIDLobby);
			}

			public void GetLobbyGameServer(Steamworks.SteamID steamIDLobby, out uint gameServerIP, out ushort gameServerPort, out Steamworks.SteamID steamIDGameServer)
			{
				gameServerIP = 0u;
				gameServerPort = 0;
				gameServerIP = 0u;
				ulong value = 0UL;
				Steamworks.SteamMatchMaking.ISteamMatchmaking_GetLobbyGameServer(this.Handle, steamIDLobby, ref gameServerIP, ref gameServerPort, ref value);
				steamIDGameServer = new Steamworks.SteamID(value);
			}

			public Steamworks.SteamID GetLobbyMemberByIndex(Steamworks.SteamID steamIDLobby, int memberIndex)
			{
				return new Steamworks.SteamID(Steamworks.SteamMatchMaking.ISteamMatchmaking_GetLobbyMemberByIndex(this.Handle, steamIDLobby, memberIndex));
			}

			public Steamworks.SteamID GetLobbyOwner(Steamworks.SteamID steamIDLobby)
			{
				return new Steamworks.SteamID(Steamworks.SteamMatchMaking.ISteamMatchmaking_GetLobbyOwner(this.Handle, steamIDLobby));
			}

			public string GetLobbyMemberData(Steamworks.SteamID steamIDLobby, Steamworks.SteamID steamIDMember, string key)
			{
				IntPtr ptr = Steamworks.SteamMatchMaking.ISteamMatchmaking_GetLobbyMemberData(this.Handle, steamIDLobby, steamIDMember, key);
				return Marshal.PtrToStringAnsi(ptr);
			}

			public int GetNumLobbyMembers(Steamworks.SteamID steamIDLobby)
			{
				return Steamworks.SteamMatchMaking.ISteamMatchmaking_GetNumLobbyMembers(this.Handle, steamIDLobby);
			}

			public void JoinLobby(Steamworks.SteamID steamIDLobby)
			{
				Steamworks.SteamMatchMaking.ISteamMatchmaking_JoinLobby(this.Handle, steamIDLobby);
			}

			public void LeaveLobby(Steamworks.SteamID steamIDLobby)
			{
				Steamworks.SteamMatchMaking.ISteamMatchmaking_LeaveLobby(this.Handle, steamIDLobby);
			}

			public bool RequestLobbyData(Steamworks.SteamID steamIDLobby)
			{
				return Steamworks.SteamMatchMaking.ISteamMatchmaking_RequestLobbyData(this.Handle, steamIDLobby);
			}

			public ulong RequestLobbyList()
			{
				return Steamworks.SteamMatchMaking.ISteamMatchmaking_RequestLobbyList(this.Handle);
			}

			public void SendLobbyChatMessage(Steamworks.SteamID steamIDLobby, byte[] bytes)
			{
				GCHandle gchandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
				Steamworks.SteamMatchMaking.ISteamMatchmaking_SendLobbyChatMessage(this.Handle, steamIDLobby, gchandle.AddrOfPinnedObject(), bytes.Length);
				gchandle.Free();
			}

			public void SendLobbyChatMessage(Steamworks.SteamID steamIDLobby, string message)
			{
				byte[] bytes = Encoding.UTF8.GetBytes(message);
				GCHandle gchandle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
				Steamworks.SteamMatchMaking.ISteamMatchmaking_SendLobbyChatMessage(this.Handle, steamIDLobby, gchandle.AddrOfPinnedObject(), bytes.Length);
				gchandle.Free();
			}

			public bool SetLobbyData(Steamworks.SteamID steamIDLobby, string key, string value)
			{
				return Steamworks.SteamMatchMaking.ISteamMatchmaking_SetLobbyData(this.Handle, steamIDLobby, key, value);
			}

			public void SetLobbyGameServer(Steamworks.SteamID steamIDLobby, uint unGameServerIP, ushort unGameServerPort, Steamworks.SteamID steamIDGameServer)
			{
				Steamworks.SteamMatchMaking.ISteamMatchmaking_SetLobbyGameServer(this.Handle, steamIDLobby, unGameServerIP, unGameServerPort, steamIDGameServer);
			}

			public bool SetLobbyJoinable(Steamworks.SteamID steamIDLobby, bool joinable)
			{
				return Steamworks.SteamMatchMaking.ISteamMatchmaking_SetLobbyJoinable(this.Handle, steamIDLobby, joinable);
			}

			public void SetLobbyMemberData(Steamworks.SteamID steamIDLobby, string key, string value)
			{
				Steamworks.SteamMatchMaking.ISteamMatchmaking_SetLobbyMemberData(this.Handle, steamIDLobby, key, value);
			}

			public bool SetLobbyMemberLimit(Steamworks.SteamID steamIDLobby, int maximumNumberOfMembers)
			{
				return Steamworks.SteamMatchMaking.ISteamMatchmaking_SetLobbyMemberLimit(this.Handle, steamIDLobby, maximumNumberOfMembers);
			}

			public bool SetLobbyType(Steamworks.SteamID steamIDLobby, Steamworks.SteamMatchMaking.ELobbyType eLobbyType)
			{
				return Steamworks.SteamMatchMaking.ISteamMatchmaking_SetLobbyType(this.Handle, steamIDLobby, eLobbyType);
			}

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamMatchmaking_AddRequestLobbyListDistanceFilter(IntPtr ptr, [MarshalAs(UnmanagedType.I4)] Steamworks.SteamMatchMaking.ELobbyDistanceFilter lobbyDistanceFilter);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamMatchmaking_AddRequestLobbyListResultCountFilter(IntPtr ptr, int cMaxResults);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamMatchmaking_CreateLobby(IntPtr ptr, [MarshalAs(UnmanagedType.I4)] Steamworks.SteamMatchMaking.ELobbyType eLobbyType, int cMaxMembers);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamMatchmaking_DeleteLobbyData(IntPtr ptr, ulong steamIDLobby, [MarshalAs(UnmanagedType.LPStr)] string pchKey);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamMatchmaking_GetLobbyByIndex(IntPtr ptr, int iLobby);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern int ISteamMatchmaking_GetLobbyChatEntry(IntPtr ptr, ulong steamIDLobby, uint iChatID, ref ulong steamIDUser, IntPtr pvData, uint cubData);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern IntPtr ISteamMatchmaking_GetLobbyData(IntPtr ptr, ulong steamIDLobby, [MarshalAs(UnmanagedType.LPStr)] string pchKey);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamMatchmaking_GetLobbyDataByIndex(IntPtr ptr, ulong steamIDLobby, int iLobbyData, IntPtr pchKey, int cchKeyBufferSize, IntPtr pchValue, int cchValueBufferSize);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern int ISteamMatchmaking_GetLobbyDataCount(IntPtr ptr, ulong steamIDLobby);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamMatchmaking_GetLobbyGameServer(IntPtr ptr, ulong steamIDLobby, ref uint punGameServerIP, ref ushort punGameServerPort, ref ulong psteamIDGameServer);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamMatchmaking_GetLobbyMemberByIndex(IntPtr ptr, ulong steamIDLobby, int iMember);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern IntPtr ISteamMatchmaking_GetLobbyMemberData(IntPtr ptr, ulong steamIDLobby, ulong steamIDMember, [MarshalAs(UnmanagedType.LPStr)] string pchKey);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamMatchmaking_GetLobbyOwner(IntPtr ptr, ulong steamIDLobby);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern int ISteamMatchmaking_GetNumLobbyMembers(IntPtr ptr, ulong steamIDLobby);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamMatchmaking_JoinLobby(IntPtr ptr, ulong steamIDLobby);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamMatchmaking_LeaveLobby(IntPtr ptr, ulong steamIDLobby);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamMatchmaking_RequestLobbyData(IntPtr ptr, ulong steamIDLobby);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamMatchmaking_RequestLobbyList(IntPtr ptr);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamMatchmaking_SendLobbyChatMessage(IntPtr ptr, ulong steamIDLobby, IntPtr pvMsgBody, int cubMsgBody);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamMatchmaking_SetLobbyData(IntPtr ptr, ulong steamIDLobby, [MarshalAs(UnmanagedType.LPStr)] string pchKey, [MarshalAs(UnmanagedType.LPStr)] string pchValue);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamMatchmaking_SetLobbyGameServer(IntPtr ptr, ulong steamIDLobby, uint unGameServerIP, ushort unGameServerPort, ulong steamIDGameServer);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamMatchmaking_SetLobbyJoinable(IntPtr ptr, ulong steamIDLobby, bool bLobbyJoinable);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamMatchmaking_SetLobbyMemberData(IntPtr ptr, ulong steamIDLobby, [MarshalAs(UnmanagedType.LPStr)] string pchKey, [MarshalAs(UnmanagedType.LPStr)] string pchValue);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamMatchmaking_SetLobbyMemberLimit(IntPtr ptr, ulong steamIDLobby, int cMaxMembers);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamMatchmaking_SetLobbyType(IntPtr ptr, ulong steamIDLobby, [MarshalAs(UnmanagedType.I4)] Steamworks.SteamMatchMaking.ELobbyType eLobbyType);

			public const int k_cbMaxGameServerGameDir = 32;

			public const int k_cbMaxGameServerMapName = 32;

			public const int k_cbMaxGameServerGameDescription = 64;

			public const int k_cbMaxGameServerName = 64;

			public const int k_cbMaxGameServerTags = 128;

			public const int k_cbMaxGameServerGameData = 128;

			private static byte[] getLobbyDataByIndex_KeyBuffer = new byte[256];

			private static byte[] getLobbyDataByIndex_DataBuffer = new byte[4096];

			private HandleRef handle;

			public enum EChatMemberStateChange : uint
			{
				k_EChatMemberStateChangeEntered = 1u,
				k_EChatMemberStateChangeLeft,
				k_EChatMemberStateChangeDisconnected = 4u,
				k_EChatMemberStateChangeKicked = 8u,
				k_EChatMemberStateChangeBanned = 16u
			}

			public enum EChatRoomEnterResponse
			{
				k_EChatRoomEnterResponseSuccess = 1,
				k_EChatRoomEnterResponseDoesntExist,
				k_EChatRoomEnterResponseNotAllowed,
				k_EChatRoomEnterResponseFull,
				k_EChatRoomEnterResponseError,
				k_EChatRoomEnterResponseBanned,
				k_EChatRoomEnterResponseLimited,
				k_EChatRoomEnterResponseClanDisabled,
				k_EChatRoomEnterResponseCommunityBan,
				k_EChatRoomEnterResponseMemberBlockedYou,
				k_EChatRoomEnterResponseYouBlockedMember,
				k_EChatRoomEnterResponseNoRankingDataLobby,
				k_EChatRoomEnterResponseNoRankingDataUser,
				k_EChatRoomEnterResponseRankOutOfRange
			}

			public enum ELobbyDistanceFilter
			{
				k_ELobbyDistanceFilterClose,
				k_ELobbyDistanceFilterDefault,
				k_ELobbyDistanceFilterFar,
				k_ELobbyDistanceFilterWorldwide
			}

			public enum ELobbyType
			{
				k_ELobbyTypePrivate,
				k_ELobbyTypeFriendsOnly,
				k_ELobbyTypePublic,
				k_ELobbyTypeInvisible
			}

			public enum EMatchMakingServerResponse
			{
				eServerResponded,
				eServerFailedToRespond,
				eNoServersListedOnMasterServer
			}

			public struct ServerInfo
			{
				public uint m_nAppID;

				public ulong m_steamID;

				public Steamworks.NetworkAddress m_NetAdr;

				public int m_nPing;

				public bool m_bHadSuccessfulResponse;

				public bool m_bDoNotRefresh;

				public bool m_bPassword;

				public bool m_bSecure;

				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
				public string m_szServerName;

				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
				public string m_szGameTags;

				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
				public string m_szGameDir;

				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
				public string m_szGameDescription;

				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
				public string m_szMap;

				public int m_nPlayers;

				public int m_nMaxPlayers;

				public int m_nBotPlayers;

				public uint m_ulTimeLastPlayed;

				public int m_nServerVersion;
			}

			public struct Filter
			{
				public Filter(string key, string value)
				{
					this.m_szKey = key;
					this.m_szValue = value;
				}

				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
				public string m_szKey;

				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
				public string m_szValue;
			}

			public class ServerBrowser : IDisposable
			{
				public ServerBrowser()
				{
					if (Steamworks.SteamAPI.SteamMatchMakingServers != null)
					{
						this.handle = new HandleRef(this, Steamworks.SteamMatchMakingServers.ISteamMatchmakingServers_CreateServerListResponse());
					}
				}

				~ServerBrowser()
				{
					this.Dispose(false);
				}

				public Steamworks.SteamMatchMaking.ServerBrowser.ServerRespondedCallback ServerResponded
				{
					set
					{
						this.serverRespondedCallback = value;
						Steamworks.SteamMatchMakingServers.ISteamMatchmakingServers_RegisterServerRespondedDelegate(this.handle.Handle, this.serverRespondedCallback);
					}
				}

				public Steamworks.SteamMatchMaking.ServerBrowser.ServersRefreshCompleteCallback ServersRefreshComplete
				{
					set
					{
						this.serversRefreshCompleteCallback = value;
						Steamworks.SteamMatchMakingServers.ISteamMatchmakingServers_RegisterServersRefreshCompleteDelegate(this.handle.Handle, this.serversRefreshCompleteCallback);
					}
				}

				public void Dispose()
				{
					this.Dispose(true);
				}

				public void RequestFriendsServerList(Steamworks.SteamMatchMaking.Filter[] filters)
				{
					Steamworks.SteamMatchMakingServers.ISteamMatchmakingServers_RequestFriendsServerList(this.handle.Handle, ref filters, filters.Length);
				}

				public void RequestInternetServerList(Steamworks.SteamMatchMaking.Filter[] filters)
				{
					Steamworks.SteamMatchMakingServers.ISteamMatchmakingServers_RequestInternetServerList(this.handle.Handle, ref filters, filters.Length);
				}

				public void RequestLANServerList()
				{
					Steamworks.SteamMatchMakingServers.ISteamMatchmakingServers_RequestLANServerList(this.handle.Handle);
				}

				protected virtual void Dispose(bool disposing)
				{
					if (!this.disposed)
					{
						Diagnostics.Log(string.Format("[ServerBrowser] Disposing({0})...", disposing.ToString()));
						if (disposing)
						{
							Steamworks.SteamMatchMakingServers.ISteamMatchmakingServers_ReleaseServerListResponse(this.handle.Handle);
							this.handle = default(HandleRef);
						}
						this.disposed = true;
					}
				}

				private bool disposed;

				private HandleRef handle;

				private Steamworks.SteamMatchMaking.ServerBrowser.ServerRespondedCallback serverRespondedCallback;

				private Steamworks.SteamMatchMaking.ServerBrowser.ServersRefreshCompleteCallback serversRefreshCompleteCallback;

				[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
				public delegate void ServerRespondedCallback(ref Steamworks.SteamMatchMaking.ServerInfo response);

				[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
				public delegate void ServersRefreshCompleteCallback(Steamworks.SteamMatchMaking.EMatchMakingServerResponse response);
			}

			public class PingResponse
			{
				public Steamworks.NetworkAddress RemoteAddress { get; private set; }

				public int Value { get; private set; }

				public Steamworks.SteamID SteamIDRemote { get; private set; }

				public IEnumerator Ping(uint unIP, ushort usPort)
				{
					bool responded = false;
					this.RemoteAddress = new Steamworks.NetworkAddress
					{
						m_unIP = unIP,
						m_usConnectionPort = usPort
					};
					Steamworks.SteamMatchMaking.PingResponse.ServerRespondedCallback callback = delegate(ulong steamIDRemote, int ping)
					{
						Diagnostics.Log(string.Concat(new object[]
						{
							"------------------------> 0x",
							steamIDRemote.ToString("x16"),
							", ",
							ping
						}));
						this.SteamIDRemote = new Steamworks.SteamID(steamIDRemote);
						this.Value = ping;
						responded = true;
					};
					HandleRef handle = new HandleRef(this, Steamworks.SteamMatchMakingServers.ISteamMatchmakingServers_CreatePingResponseEx(this.RemoteAddress.m_unIP, this.RemoteAddress.m_usConnectionPort, callback));
					while (!responded)
					{
						yield return null;
					}
					Steamworks.SteamMatchMakingServers.ISteamMatchmakingServers_ReleasePingResponse(handle.Handle);
					handle = default(HandleRef);
					yield break;
				}

				public static implicit operator int(Steamworks.SteamMatchMaking.PingResponse ping)
				{
					return ping.Value;
				}

				public static implicit operator Steamworks.SteamID(Steamworks.SteamMatchMaking.PingResponse ping)
				{
					return ping.SteamIDRemote;
				}

				public static implicit operator ulong(Steamworks.SteamMatchMaking.PingResponse ping)
				{
					return ping.SteamIDRemote;
				}

				[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
				public delegate void ServerRespondedCallback(ulong steamIDRemote, int ping);
			}
		}

		public struct NetworkAddress
		{
			public bool IsValid
			{
				get
				{
					return this.m_unIP != 0u && this.m_usConnectionPort != 0;
				}
			}

			public override string ToString()
			{
				return string.Format("{0}.{1}.{2}.{3}:{4}", new object[]
				{
					this.m_unIP >> 24 & 255u,
					this.m_unIP >> 16 & 255u,
					this.m_unIP >> 8 & 255u,
					this.m_unIP & 255u,
					this.m_usConnectionPort
				});
			}

			public ushort m_usConnectionPort;

			public ushort m_usQueryPort;

			public uint m_unIP;
		}

		public class SteamMatchMakingServers
		{
			internal SteamMatchMakingServers(IntPtr ptr)
			{
				this.handle = new HandleRef(this, ptr);
			}

			private IntPtr Handle
			{
				get
				{
					return this.handle.Handle;
				}
			}

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr ISteamMatchmakingServers_CreatePingResponseEx(uint unIP, ushort usPort, Steamworks.SteamMatchMaking.PingResponse.ServerRespondedCallback serverRespondedCallback);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr ISteamMatchmakingServers_CreateServerListResponse();

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr ISteamMatchmakingServers_CreateServerListResponseEx(Steamworks.SteamMatchMaking.ServerBrowser.ServerRespondedCallback serverRespondedCallback, Steamworks.SteamMatchMaking.ServerBrowser.ServersRefreshCompleteCallback serversRefreshCompleteCallback);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void ISteamMatchmakingServers_RegisterServerRespondedDelegate(IntPtr ptr, Steamworks.SteamMatchMaking.ServerBrowser.ServerRespondedCallback serverRespondedCallback);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			internal static extern void ISteamMatchmakingServers_RegisterServersRefreshCompleteDelegate(IntPtr ptr, Steamworks.SteamMatchMaking.ServerBrowser.ServersRefreshCompleteCallback serversRefreshCompleteCallback);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr ISteamMatchmakingServers_ReleasePingResponse(IntPtr ptr);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr ISteamMatchmakingServers_ReleaseServerListResponse(IntPtr ptr);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr ISteamMatchmakingServers_RequestFriendsServerList(IntPtr ptr, ref Steamworks.SteamMatchMaking.Filter[] filters, int numberOfFilters);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr ISteamMatchmakingServers_RequestInternetServerList(IntPtr ptr, ref Steamworks.SteamMatchMaking.Filter[] filters, int numberOfFilters);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			internal static extern IntPtr ISteamMatchmakingServers_RequestLANServerList(IntPtr ptr);

			private HandleRef handle;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct P2PSessionRequest
		{
			public ulong m_steamIDRemote;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct P2PSessionConnectFail
		{
			public ulong m_steamIDRemote;

			public byte m_eP2PSessionError;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct P2PSessionState
		{
			public byte m_bConnectionActive;

			public byte m_bConnecting;

			public byte m_eP2PSessionError;

			public byte m_bUsingRelay;

			public int m_nBytesQueuedForSend;

			public int m_nPacketsQueuedForSend;

			public uint m_nRemoteIP;

			public ushort m_nRemotePort;
		}

		public class SteamNetworking
		{
			internal SteamNetworking(IntPtr ptr)
			{
				this.handle = new HandleRef(this, ptr);
			}

			private IntPtr Handle
			{
				get
				{
					return this.handle.Handle;
				}
			}

			public bool AcceptP2PSessionWithUser(ulong steamID)
			{
				return Steamworks.SteamNetworking.ISteamNetworking_AcceptP2PSessionWithUser(this.Handle, steamID);
			}

			public bool CloseP2PSessionWithUser(ulong steamID)
			{
				return Steamworks.SteamNetworking.ISteamNetworking_CloseP2PSessionWithUser(this.Handle, steamID);
			}

			public bool GetP2PSessionState(ulong steamIDRemote, ref Steamworks.P2PSessionState pConnectionState)
			{
				return Steamworks.SteamNetworking.ISteamNetworking_GetP2PSessionState(this.Handle, steamIDRemote, ref pConnectionState);
			}

			public bool IsP2PPacketAvailable(ref uint pcubMsgSize, int nChannel = 0)
			{
				return Steamworks.SteamNetworking.ISteamNetworking_IsP2PPacketAvailable(this.Handle, ref pcubMsgSize, nChannel);
			}

			public bool ReadP2PPacket(ref GCHandle handle, uint cubDest, out uint pcubMsgSize, out Steamworks.SteamID psteamIDRemote, int nChannel = 0)
			{
				ulong value = 0UL;
				if (Steamworks.SteamNetworking.ISteamNetworking_ReadP2PPacket(this.Handle, handle.AddrOfPinnedObject(), cubDest, out pcubMsgSize, out value, nChannel))
				{
					psteamIDRemote = new Steamworks.SteamID(value);
					return true;
				}
				psteamIDRemote = null;
				return false;
			}

			public bool ReadP2PPacket(ref byte[] buffer, out uint pcubMsgSize, out Steamworks.SteamID psteamIDRemote, int nChannel = 0)
			{
				ulong value = 0UL;
				if (Steamworks.SteamNetworking.ISteamNetworking_ReadP2PPacket(this.Handle, Marshal.UnsafeAddrOfPinnedArrayElement(buffer, 0), (uint)buffer.Length, out pcubMsgSize, out value, nChannel))
				{
					psteamIDRemote = new Steamworks.SteamID(value);
					return true;
				}
				psteamIDRemote = null;
				return false;
			}

			public bool SendP2PPacket(ulong steamIDRemote, IntPtr pubData, uint cubData, Steamworks.SteamNetworking.EP2PSend eP2PSendType, int nChannel = 0)
			{
				return Steamworks.SteamNetworking.ISteamNetworking_SendP2PPacket(this.Handle, steamIDRemote, pubData, cubData, eP2PSendType, nChannel);
			}

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamNetworking_AcceptP2PSessionWithUser(IntPtr ptr, ulong steamID);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamNetworking_CloseP2PSessionWithUser(IntPtr ptr, ulong steamID);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamNetworking_GetP2PSessionState(IntPtr ptr, ulong steamIDRemote, ref Steamworks.P2PSessionState pConnectionState);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamNetworking_IsP2PPacketAvailable(IntPtr ptr, ref uint pcubMsgSize, int nChannel);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamNetworking_ReadP2PPacket(IntPtr ptr, [In] [Out] IntPtr pubDest, uint cubDest, out uint pcubMsgSize, out ulong psteamIDRemote, int nChannel);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamNetworking_SendP2PPacket(IntPtr ptr, ulong steamID, IntPtr pubData, uint cubData, [MarshalAs(UnmanagedType.I4)] Steamworks.SteamNetworking.EP2PSend eP2PSendType, int cChannel);

			private HandleRef handle;

			public enum EP2PSend
			{
				k_EP2PSendUnreliable,
				k_EP2PSendUnreliableNoDelay,
				k_EP2PSendReliable,
				k_EP2PSendReliableWithBuffering
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct RemoteStoragePublishedFileSubscribed
		{
			public const int k_iCallback = 1321;

			public ulong m_nPublishedFileId;

			public uint m_nAppID;
		}

		public struct RemoteStoragePublishedFileUnsubscribed
		{
			public const int k_iCallback = 1322;

			public ulong m_nPublishedFileId;

			public uint m_nAppID;
		}

		public class SteamRemoteStorage
		{
			internal SteamRemoteStorage(IntPtr ptr)
			{
				this.handle = new HandleRef(this, ptr);
			}

			private IntPtr Handle
			{
				get
				{
					return this.handle.Handle;
				}
			}

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamRemoteStorage_FileWrite(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchFile, IntPtr pvData, int cubData);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern int ISteamRemoteStorage_FileRead(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchFile, IntPtr pvData, int cubDataToRead);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamRemoteStorage_FileForget(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchFile);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamRemoteStorage_FileDelete(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchFile);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamRemoteStorage_FileShare(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchFile);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamRemoteStorage_SetSyncPlatforms(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchFile, Steamworks.SteamRemoteStorage.ERemoteStoragePlatform eRemoteStoragePlatform);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamRemoteStorage_FileWriteStreamOpen(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchFile);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamRemoteStorage_FileWriteStreamWriteChunk(IntPtr ptr, ulong writeHandle, IntPtr pvData, int cubData);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamRemoteStorage_FileWriteStreamClose(IntPtr ptr, ulong writeHandle);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamRemoteStorage_FileWriteStreamCancel(IntPtr ptr, ulong writeHandle);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamRemoteStorage_FileExists(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchFile);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamRemoteStorage_FilePersisted(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchFile);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern int ISteamRemoteStorage_GetFileSize(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchFile);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern long ISteamRemoteStorage_GetFileTimestamp(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchFile);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern Steamworks.SteamRemoteStorage.ERemoteStoragePlatform ISteamRemoteStorage_GetSyncPlatforms(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchFile);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern int ISteamRemoteStorage_GetFileCount(IntPtr ptr);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern IntPtr ISteamRemoteStorage_GetFileNameAndSize(IntPtr ptr, int iFile, out int pnFileSizeInBytes);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamRemoteStorage_GetQuota(IntPtr ptr, out int pnTotalBytes, out int puAvailableBytes);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamRemoteStorage_IsCloudEnabledForAccount(IntPtr ptr);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamRemoteStorage_IsCloudEnabledForApp(IntPtr ptr);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamRemoteStorage_SetCloudEnabledForApp(IntPtr ptr, bool bEnabled);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamRemoteStorage_UGCDownload(IntPtr ptr, ulong hContent, uint unPriority);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamRemoteStorage_GetUGCDownloadProgress(IntPtr ptr, ulong hContent, out int pnBytesDownloaded, out int pnBytesExpected);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamRemoteStorage_GetUGCDetails(IntPtr ptr, ulong hContent, out uint pnAppID, out IntPtr ppchName, out int pnFileSizeInBytes, out IntPtr pSteamIDOwner);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern int ISteamRemoteStorage_UGCRead(IntPtr ptr, ulong hContent, IntPtr pvData, int cubDataToRead, uint cOffset, Steamworks.SteamRemoteStorage.EUGCReadAction eAction);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern int ISteamRemoteStorage_GetCachedUGCCount(IntPtr ptr);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamRemoteStorage_GetCachedUGCHandle(IntPtr ptr, int iCachedContent);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamRemoteStorage_PublishWorkshopFile(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchFile, [MarshalAs(UnmanagedType.LPStr)] string pchPreviewFile, uint nConsumerAppId, [MarshalAs(UnmanagedType.LPStr)] string pchTitle, [MarshalAs(UnmanagedType.LPStr)] string pchDescription, Steamworks.SteamRemoteStorage.ERemoteStoragePublishedFileVisibility eVisibility, IntPtr pTags, Steamworks.SteamRemoteStorage.EWorkshopFileType eWorkshopFileType);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamRemoteStorage_CreatePublishedFileUpdateRequest(IntPtr ptr, ulong unPublishedFileId);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamRemoteStorage_UpdatePublishedFileFile(IntPtr ptr, ulong updateHandle, [MarshalAs(UnmanagedType.LPStr)] string pchFile);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamRemoteStorage_UpdatePublishedFilePreviewFile(IntPtr ptr, ulong updateHandle, [MarshalAs(UnmanagedType.LPStr)] string pchPreviewFile);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamRemoteStorage_UpdatePublishedFileTitle(IntPtr ptr, ulong updateHandle, [MarshalAs(UnmanagedType.LPStr)] string pchTitle);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamRemoteStorage_UpdatePublishedFileDescription(IntPtr ptr, ulong updateHandle, [MarshalAs(UnmanagedType.LPStr)] string pchDescription);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamRemoteStorage_UpdatePublishedFileVisibility(IntPtr ptr, ulong updateHandle, Steamworks.SteamRemoteStorage.ERemoteStoragePublishedFileVisibility eVisibility);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamRemoteStorage_UpdatePublishedFileTags(IntPtr ptr, ulong updateHandle, IntPtr pTags);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamRemoteStorage_CommitPublishedFileUpdate(IntPtr ptr, ulong updateHandle);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamRemoteStorage_GetPublishedFileDetails(IntPtr ptr, ulong unPublishedFileId, uint unMaxSecondsOld);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamRemoteStorage_DeletePublishedFile(IntPtr ptr, ulong unPublishedFileId);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamRemoteStorage_EnumerateUserPublishedFiles(IntPtr ptr, uint unStartIndex);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamRemoteStorage_SubscribePublishedFile(IntPtr ptr, ulong unPublishedFileId);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamRemoteStorage_EnumerateUserSubscribedFiles(IntPtr ptr, uint unStartIndex);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamRemoteStorage_UnsubscribePublishedFile(IntPtr ptr, ulong unPublishedFileId);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamRemoteStorage_UpdatePublishedFileSetChangeDescription(IntPtr ptr, ulong updateHandle, [MarshalAs(UnmanagedType.LPStr)] string pchChangeDescription);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamRemoteStorage_GetPublishedItemVoteDetails(IntPtr ptr, ulong unPublishedFileId);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamRemoteStorage_UpdateUserPublishedItemVote(IntPtr ptr, ulong unPublishedFileId, bool bVoteUp);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamRemoteStorage_GetUserPublishedItemVoteDetails(IntPtr ptr, ulong unPublishedFileId);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamRemoteStorage_EnumerateUserSharedWorkshopFiles(IntPtr ptr, ulong steamId, uint unStartIndex, IntPtr pRequiredTags, IntPtr pExcludedTags);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamRemoteStorage_PublishVideo(IntPtr ptr, Steamworks.SteamRemoteStorage.EWorkshopVideoProvider eVideoProvider, [MarshalAs(UnmanagedType.LPStr)] string pchVideoAccount, [MarshalAs(UnmanagedType.LPStr)] string pchVideoIdentifier, [MarshalAs(UnmanagedType.LPStr)] string pchPreviewFile, uint nConsumerAppId, [MarshalAs(UnmanagedType.LPStr)] string pchTitle, [MarshalAs(UnmanagedType.LPStr)] string pchDescription, Steamworks.SteamRemoteStorage.ERemoteStoragePublishedFileVisibility eVisibility, IntPtr pTags);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamRemoteStorage_SetUserPublishedFileAction(IntPtr ptr, ulong unPublishedFileId, Steamworks.SteamRemoteStorage.EWorkshopFileAction eAction);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamRemoteStorage_EnumeratePublishedFilesByUserAction(IntPtr ptr, Steamworks.SteamRemoteStorage.EWorkshopFileAction eAction, uint unStartIndex);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamRemoteStorage_EnumeratePublishedWorkshopFiles(IntPtr ptr, Steamworks.SteamRemoteStorage.EWorkshopEnumerationType eEnumerationType, uint unStartIndex, uint unCount, uint unDays, IntPtr pTags, IntPtr pUserTags);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamRemoteStorage_UGCDownloadToLocation(IntPtr ptr, ulong hContent, [MarshalAs(UnmanagedType.LPStr)] string pchLocation, uint unPriority);

			private HandleRef handle;

			public enum ERemoteStoragePublishedFileVisibility
			{
				k_ERemoteStoragePublishedFileVisibilityPublic,
				k_ERemoteStoragePublishedFileVisibilityFriendsOnly,
				k_ERemoteStoragePublishedFileVisibilityPrivate
			}

			public enum ERemoteStoragePlatform : uint
			{
				k_ERemoteStoragePlatformNone,
				k_ERemoteStoragePlatformWindows,
				k_ERemoteStoragePlatformOSX,
				k_ERemoteStoragePlatformPS3 = 4u,
				k_ERemoteStoragePlatformLinux = 8u,
				k_ERemoteStoragePlatformReserved2 = 16u,
				k_ERemoteStoragePlatformAll = 4294967295u
			}

			public enum EUGCReadAction
			{
				k_EUGCRead_ContinueReadingUntilFinished,
				k_EUGCRead_ContinueReading,
				k_EUGCRead_Close
			}

			public enum EWorkshopEnumerationType
			{
				k_EWorkshopEnumerationTypeRankedByVote,
				k_EWorkshopEnumerationTypeRecent,
				k_EWorkshopEnumerationTypeTrending,
				k_EWorkshopEnumerationTypeFavoritesOfFriends,
				k_EWorkshopEnumerationTypeVotedByFriends,
				k_EWorkshopEnumerationTypeContentByFriends,
				k_EWorkshopEnumerationTypeRecentFromFollowedUsers
			}

			public enum EWorkshopFileAction
			{
				k_EWorkshopFileActionPlayed,
				k_EWorkshopFileActionCompleted
			}

			public enum EWorkshopFileType
			{
				k_EWorkshopFileTypeFirst,
				k_EWorkshopFileTypeCommunity = 0,
				k_EWorkshopFileTypeMicrotransaction,
				k_EWorkshopFileTypeCollection,
				k_EWorkshopFileTypeArt,
				k_EWorkshopFileTypeVideo,
				k_EWorkshopFileTypeScreenshot,
				k_EWorkshopFileTypeGame,
				k_EWorkshopFileTypeSoftware,
				k_EWorkshopFileTypeConcept,
				k_EWorkshopFileTypeWebGuide,
				k_EWorkshopFileTypeIntegratedGuide,
				k_EWorkshopFileTypeMerch,
				k_EWorkshopFileTypeControllerBinding,
				k_EWorkshopFileTypeSteamworksAccessInvite,
				k_EWorkshopFileTypeSteamVideo,
				k_EWorkshopFileTypeGameManagedItem,
				k_EWorkshopFileTypeMax
			}

			public enum EWorkshopVideoProvider
			{
				k_EWorkshopVideoProviderNone,
				k_EWorkshopVideoProviderYoutube
			}
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct CreateItem
		{
			public const int k_iCallback = 3403;

			[MarshalAs(UnmanagedType.I4)]
			public Steamworks.EResult m_eResult;

			public ulong m_nPublishedFileId;

			public bool m_bUserNeedsToAcceptWorkshopLegalAgreement;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct DownloadItemResult
		{
			public const int k_iCallback = 3406;

			public uint m_nAppID;

			public ulong m_nPublishedFileId;

			[MarshalAs(UnmanagedType.I4)]
			public Steamworks.EResult m_eResult;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct ItemInstalled
		{
			public const int k_iCallback = 3405;

			public uint m_nAppId;

			public ulong m_nPublishedFileId;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct SteamUGCQueryCompleted
		{
			public const int k_iCallback = 3401;

			public ulong m_handle;

			[MarshalAs(UnmanagedType.I4)]
			public Steamworks.EResult m_eResult;

			public uint m_unNumResultsReturned;

			public uint m_unTotalMatchingResults;

			public bool m_bCachedData;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct SubmitItemUpdate
		{
			public const int k_iCallback = 3404;

			[MarshalAs(UnmanagedType.I4)]
			public Steamworks.EResult m_eResult;

			public bool m_bUserNeedsToAcceptWorkshopLegalAgreement;
		}

		public class SteamUGC
		{
			internal SteamUGC(IntPtr ptr)
			{
				this.handle = new HandleRef(this, ptr);
			}

			private IntPtr Handle
			{
				get
				{
					return this.handle.Handle;
				}
			}

			public ulong CreateQueryUserUGCRequest(uint unAccountID, Steamworks.SteamUGC.EUserUGCList eListType, Steamworks.SteamUGC.EUGCMatchingUGCType eMatchingUGCType, Steamworks.SteamUGC.EUserUGCListSortOrder eSortOrder, uint nCreatorAppID, uint nConsumerAppID, uint unPage)
			{
				return Steamworks.SteamUGC.ISteamUGC_CreateQueryUserUGCRequest(this.Handle, unAccountID, eListType, eMatchingUGCType, eSortOrder, nCreatorAppID, nConsumerAppID, unPage);
			}

			public ulong CreateQueryAllUGCRequest(Steamworks.SteamUGC.EUGCQuery eQueryType, Steamworks.SteamUGC.EUGCMatchingUGCType eMatchingeMatchingUGCTypeFileType, uint nCreatorAppID, uint nConsumerAppID, uint unPage)
			{
				return Steamworks.SteamUGC.ISteamUGC_CreateQueryAllUGCRequest(this.Handle, eQueryType, eMatchingeMatchingUGCTypeFileType, nCreatorAppID, nConsumerAppID, unPage);
			}

			public ulong CreateQueryUGCDetailsRequest(ref ulong[] pvecPublishedFileID, uint unNumPublishedFileIDs)
			{
				if (pvecPublishedFileID != null && pvecPublishedFileID.Length > 0 && (long)pvecPublishedFileID.Length >= (long)((ulong)unNumPublishedFileIDs))
				{
					return Steamworks.SteamUGC.ISteamUGC_CreateQueryUGCDetailsRequest(this.Handle, Marshal.UnsafeAddrOfPinnedArrayElement(pvecPublishedFileID, 0), unNumPublishedFileIDs);
				}
				return 0UL;
			}

			public ulong SendQueryUGCRequest(ulong handle)
			{
				return Steamworks.SteamUGC.ISteamUGC_SendQueryUGCRequest(this.Handle, handle);
			}

			public bool GetQueryUGCResult(ulong handle, uint index, ref Steamworks.SteamUGC.SteamUGCDetails pDetails)
			{
				return Steamworks.SteamUGC.ISteamUGC_GetQueryUGCResult(this.Handle, handle, index, ref pDetails);
			}

			public bool GetQueryUGCPreviewURL(ulong handle, uint index, out string pchURL)
			{
				byte[] array = new byte[2048];
				bool flag = Steamworks.SteamUGC.ISteamUGC_GetQueryUGCPreviewURL(this.Handle, handle, index, Marshal.UnsafeAddrOfPinnedArrayElement(array, 0), (uint)array.Length);
				if (flag)
				{
					pchURL = Encoding.UTF8.GetString(array);
				}
				else
				{
					pchURL = null;
				}
				return flag;
			}

			public bool ReleaseQueryUGCRequest(ulong handle)
			{
				return Steamworks.SteamUGC.ISteamUGC_ReleaseQueryUGCRequest(this.Handle, handle);
			}

			public bool AddRequiredTag(ulong handle, string pTagName)
			{
				return Steamworks.SteamUGC.ISteamUGC_AddRequiredTag(this.Handle, handle, pTagName);
			}

			public bool AddExcludedTag(ulong handle, string pTagName)
			{
				return Steamworks.SteamUGC.ISteamUGC_AddExcludedTag(this.Handle, handle, pTagName);
			}

			public bool SetReturnKeyValueTags(ulong handle, bool bReturnKeyValueTags)
			{
				return Steamworks.SteamUGC.ISteamUGC_SetReturnKeyValueTags(this.Handle, handle, bReturnKeyValueTags);
			}

			public bool SetReturnLongDescription(ulong handle, bool bReturnLongDescription)
			{
				return Steamworks.SteamUGC.ISteamUGC_SetReturnLongDescription(this.Handle, handle, bReturnLongDescription);
			}

			public bool SetReturnMetadata(ulong handle, bool bReturnMetadata)
			{
				return Steamworks.SteamUGC.ISteamUGC_SetReturnMetadata(this.Handle, handle, bReturnMetadata);
			}

			public bool SetReturnChildren(ulong handle, bool bReturnChildren)
			{
				return Steamworks.SteamUGC.ISteamUGC_SetReturnChildren(this.Handle, handle, bReturnChildren);
			}

			public bool SetReturnAdditionalPreviews(ulong handle, bool bReturnAdditionalPreviews)
			{
				return Steamworks.SteamUGC.ISteamUGC_SetReturnAdditionalPreviews(this.Handle, handle, bReturnAdditionalPreviews);
			}

			public bool SetReturnTotalOnly(ulong handle, bool bReturnTotalOnly)
			{
				return Steamworks.SteamUGC.ISteamUGC_SetReturnTotalOnly(this.Handle, handle, bReturnTotalOnly);
			}

			public bool SetLanguage(ulong handle, string pchLanguage)
			{
				return Steamworks.SteamUGC.ISteamUGC_SetLanguage(this.Handle, handle, pchLanguage);
			}

			public bool SetAllowCachedResponse(ulong handle, uint unMaxAgeSeconds)
			{
				return Steamworks.SteamUGC.ISteamUGC_SetAllowCachedResponse(this.Handle, handle, unMaxAgeSeconds);
			}

			public bool SetCloudFileNameFilter(ulong handle, string pMatchCloudFileName)
			{
				return Steamworks.SteamUGC.ISteamUGC_SetCloudFileNameFilter(this.Handle, handle, pMatchCloudFileName);
			}

			public bool SetMatchAnyTag(ulong handle, bool bMatchAnyTag)
			{
				return Steamworks.SteamUGC.ISteamUGC_SetMatchAnyTag(this.Handle, handle, bMatchAnyTag);
			}

			public bool SetSearchText(ulong handle, string pSearchText)
			{
				return Steamworks.SteamUGC.ISteamUGC_SetSearchText(this.Handle, handle, pSearchText);
			}

			public bool SetRankedByTrendDays(ulong handle, uint unDays)
			{
				return Steamworks.SteamUGC.ISteamUGC_SetRankedByTrendDays(this.Handle, handle, unDays);
			}

			public bool AddRequiredKeyValueTag(ulong handle, string pKey, string pValue)
			{
				return Steamworks.SteamUGC.ISteamUGC_AddRequiredKeyValueTag(this.Handle, handle, pKey, pValue);
			}

			public ulong CreateItem(uint nConsumerAppId, Steamworks.SteamRemoteStorage.EWorkshopFileType eFileType)
			{
				return Steamworks.SteamUGC.ISteamUGC_CreateItem(this.Handle, nConsumerAppId, eFileType);
			}

			public ulong StartItemUpdate(uint nConsumerAppId, ulong nPublishedFileID)
			{
				return Steamworks.SteamUGC.ISteamUGC_StartItemUpdate(this.Handle, nConsumerAppId, nPublishedFileID);
			}

			public bool SetItemTitle(ulong handle, string pchTitle)
			{
				return Steamworks.SteamUGC.ISteamUGC_SetItemTitle(this.Handle, handle, pchTitle.ToASCIIString());
			}

			public bool SetItemDescription(ulong handle, string pchDescription)
			{
				return Steamworks.SteamUGC.ISteamUGC_SetItemDescription(this.Handle, handle, pchDescription.ToASCIIString());
			}

			public bool SetItemUpdateLanguage(ulong handle, string pchLanguage)
			{
				return Steamworks.SteamUGC.ISteamUGC_SetItemUpdateLanguage(this.Handle, handle, pchLanguage);
			}

			public bool SetItemMetadata(ulong handle, string pchMetaData)
			{
				return Steamworks.SteamUGC.ISteamUGC_SetItemMetadata(this.Handle, handle, pchMetaData.ToASCIIString());
			}

			public bool SetItemVisibility(ulong handle, Steamworks.SteamRemoteStorage.ERemoteStoragePublishedFileVisibility eVisibility)
			{
				return Steamworks.SteamUGC.ISteamUGC_SetItemVisibility(this.Handle, handle, eVisibility);
			}

			public bool SetItemTags(ulong updateHandle, params string[] pTags)
			{
				Steamworks.SteamUGC.SteamParamStringArray that = new Steamworks.SteamUGC.SteamParamStringArray(pTags);
				return Steamworks.SteamUGC.ISteamUGC_SetItemTags(this.Handle, updateHandle, that);
			}

			public bool SetItemTags(ulong updateHandle, IList<string> pTags)
			{
				Steamworks.SteamUGC.SteamParamStringArray that = new Steamworks.SteamUGC.SteamParamStringArray(pTags);
				return Steamworks.SteamUGC.ISteamUGC_SetItemTags(this.Handle, updateHandle, that);
			}

			public bool SetItemContent(ulong handle, string pszContentFolder)
			{
				return Steamworks.SteamUGC.ISteamUGC_SetItemContent(this.Handle, handle, pszContentFolder);
			}

			public bool SetItemPreview(ulong handle, string pszPreviewFile)
			{
				return Steamworks.SteamUGC.ISteamUGC_SetItemPreview(this.Handle, handle, pszPreviewFile);
			}

			public bool RemoveItemKeyValueTags(ulong handle, string pchKey)
			{
				return Steamworks.SteamUGC.ISteamUGC_RemoveItemKeyValueTags(this.Handle, handle, pchKey);
			}

			public bool AddItemKeyValueTag(ulong handle, string pchKey, string pchValue)
			{
				return Steamworks.SteamUGC.ISteamUGC_AddItemKeyValueTag(this.Handle, handle, pchKey, pchValue);
			}

			public ulong SubmitItemUpdate(ulong handle, string pchChangeNote)
			{
				return Steamworks.SteamUGC.ISteamUGC_SubmitItemUpdate(this.Handle, handle, pchChangeNote);
			}

			public Steamworks.SteamUGC.EItemUpdateStatus GetItemUpdateProgress(ulong handle, out ulong punBytesProcessed, out ulong punBytesTotal)
			{
				return Steamworks.SteamUGC.ISteamUGC_GetItemUpdateProgress(this.Handle, handle, out punBytesProcessed, out punBytesTotal);
			}

			public ulong SetUserItemVote(ulong nPublishedFileID, bool bVoteUp)
			{
				return Steamworks.SteamUGC.ISteamUGC_SetUserItemVote(this.Handle, nPublishedFileID, bVoteUp);
			}

			public ulong GetUserItemVote(ulong nPublishedFileID)
			{
				return Steamworks.SteamUGC.ISteamUGC_GetUserItemVote(this.Handle, nPublishedFileID);
			}

			public ulong AddItemToFavorites(uint nAppId, ulong nPublishedFileID)
			{
				return Steamworks.SteamUGC.ISteamUGC_AddItemToFavorites(this.Handle, nAppId, nPublishedFileID);
			}

			public ulong RemoveItemFromFavorites(uint nAppId, ulong nPublishedFileID)
			{
				return Steamworks.SteamUGC.ISteamUGC_RemoveItemFromFavorites(this.Handle, nAppId, nPublishedFileID);
			}

			public ulong SubscribeItem(ulong nPublishedFileID)
			{
				return Steamworks.SteamUGC.ISteamUGC_SubscribeItem(this.Handle, nPublishedFileID);
			}

			public ulong UnsubscribeItem(ulong nPublishedFileID)
			{
				return Steamworks.SteamUGC.ISteamUGC_UnsubscribeItem(this.Handle, nPublishedFileID);
			}

			public uint GetNumSubscribedItems()
			{
				return Steamworks.SteamUGC.ISteamUGC_GetNumSubscribedItems(this.Handle);
			}

			public uint GetSubscribedItems(ref ulong[] pvecPublishedFileID)
			{
				if (pvecPublishedFileID != null && pvecPublishedFileID.Length > 0)
				{
					return Steamworks.SteamUGC.ISteamUGC_GetSubscribedItems(this.Handle, Marshal.UnsafeAddrOfPinnedArrayElement(pvecPublishedFileID, 0), (uint)pvecPublishedFileID.Length);
				}
				return 0u;
			}

			public uint GetItemState(ulong nPublishedFileID)
			{
				return Steamworks.SteamUGC.ISteamUGC_GetItemState(this.Handle, nPublishedFileID);
			}

			public bool GetItemInstallInfo(ulong nPublishedFileID, out ulong punSizeOnDisk, out string pchFolder, out uint punTimeStamp)
			{
				IntPtr intPtr = Marshal.AllocHGlobal(2048);
				bool flag = false;
				try
				{
					flag = Steamworks.SteamUGC.ISteamUGC_GetItemInstallInfo(this.Handle, nPublishedFileID, out punSizeOnDisk, intPtr, 2048u, out punTimeStamp);
					if (flag)
					{
						pchFolder = Marshal.PtrToStringAnsi(intPtr);
					}
					else
					{
						pchFolder = null;
					}
				}
				finally
				{
					Marshal.FreeHGlobal(intPtr);
				}
				return flag;
			}

			public bool GetItemDownloadInfo(ulong nPublishedFileID, out ulong punBytesDownloaded, out ulong punBytesTotal)
			{
				return Steamworks.SteamUGC.ISteamUGC_GetItemDownloadInfo(this.Handle, nPublishedFileID, out punBytesDownloaded, out punBytesTotal);
			}

			public bool DownloadItem(ulong nPublishedFileID, bool bHighPriority)
			{
				return Steamworks.SteamUGC.ISteamUGC_DownloadItem(this.Handle, nPublishedFileID, bHighPriority);
			}

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamUGC_CreateQueryUserUGCRequest(IntPtr ptr, uint unAccountID, Steamworks.SteamUGC.EUserUGCList eListType, Steamworks.SteamUGC.EUGCMatchingUGCType eMatchingUGCType, Steamworks.SteamUGC.EUserUGCListSortOrder eSortOrder, uint nCreatorAppID, uint nConsumerAppID, uint unPage);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamUGC_CreateQueryAllUGCRequest(IntPtr ptr, Steamworks.SteamUGC.EUGCQuery eQueryType, Steamworks.SteamUGC.EUGCMatchingUGCType eMatchingeMatchingUGCTypeFileType, uint nCreatorAppID, uint nConsumerAppID, uint unPage);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamUGC_CreateQueryUGCDetailsRequest(IntPtr ptr, IntPtr pvecPublishedFileID, uint unNumPublishedFileIDs);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamUGC_SendQueryUGCRequest(IntPtr ptr, ulong handle);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_GetQueryUGCResult(IntPtr ptr, ulong handle, uint index, ref Steamworks.SteamUGC.SteamUGCDetails pDetails);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_GetQueryUGCPreviewURL(IntPtr ptr, ulong handle, uint index, IntPtr pchURL, uint cchURLSize);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_GetQueryUGCMetadata(IntPtr ptr, ulong handle, uint index, [MarshalAs(UnmanagedType.LPStr)] string pchMetadata, uint cchMetadatasize);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_GetQueryUGCChildren(IntPtr ptr, ulong handle, uint index, IntPtr pvecPublishedFileID, uint cMaxEntries);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_GetQueryUGCStatistic(IntPtr ptr, ulong handle, uint index, Steamworks.SteamUGC.EItemStatistic eStatType, out uint pStatValue);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern uint ISteamUGC_GetQueryUGCNumAdditionalPreviews(IntPtr ptr, ulong handle, uint index);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_GetQueryUGCAdditionalPreview(IntPtr ptr, ulong handle, uint index, uint previewIndex, [MarshalAs(UnmanagedType.LPStr)] string pchURLOrVideoID, uint cchURLSize, out bool pbIsImage);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern uint ISteamUGC_GetQueryUGCNumKeyValueTags(IntPtr ptr, ulong handle, uint index);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_GetQueryUGCKeyValueTag(IntPtr ptr, ulong handle, uint index, uint keyValueTagIndex, [MarshalAs(UnmanagedType.LPStr)] string pchKey, uint cchKeySize, [MarshalAs(UnmanagedType.LPStr)] string pchValue, uint cchValueSize);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_ReleaseQueryUGCRequest(IntPtr ptr, ulong handle);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_AddRequiredTag(IntPtr ptr, ulong handle, [MarshalAs(UnmanagedType.LPStr)] string pTagName);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_AddExcludedTag(IntPtr ptr, ulong handle, [MarshalAs(UnmanagedType.LPStr)] string pTagName);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_SetReturnKeyValueTags(IntPtr ptr, ulong handle, bool bReturnKeyValueTags);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_SetReturnLongDescription(IntPtr ptr, ulong handle, bool bReturnLongDescription);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_SetReturnMetadata(IntPtr ptr, ulong handle, bool bReturnMetadata);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_SetReturnChildren(IntPtr ptr, ulong handle, bool bReturnChildren);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_SetReturnAdditionalPreviews(IntPtr ptr, ulong handle, bool bReturnAdditionalPreviews);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_SetReturnTotalOnly(IntPtr ptr, ulong handle, bool bReturnTotalOnly);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_SetLanguage(IntPtr ptr, ulong handle, [MarshalAs(UnmanagedType.LPStr)] string pchLanguage);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_SetAllowCachedResponse(IntPtr ptr, ulong handle, uint unMaxAgeSeconds);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_SetCloudFileNameFilter(IntPtr ptr, ulong handle, [MarshalAs(UnmanagedType.LPStr)] string pMatchCloudFileName);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_SetMatchAnyTag(IntPtr ptr, ulong handle, bool bMatchAnyTag);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_SetSearchText(IntPtr ptr, ulong handle, [MarshalAs(UnmanagedType.LPStr)] string pSearchText);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_SetRankedByTrendDays(IntPtr ptr, ulong handle, uint unDays);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_AddRequiredKeyValueTag(IntPtr ptr, ulong handle, [MarshalAs(UnmanagedType.LPStr)] string pKey, [MarshalAs(UnmanagedType.LPStr)] string pValue);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamUGC_CreateItem(IntPtr ptr, uint nConsumerAppId, Steamworks.SteamRemoteStorage.EWorkshopFileType eFileType);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamUGC_StartItemUpdate(IntPtr ptr, uint nConsumerAppId, ulong nPublishedFileID);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_SetItemTitle(IntPtr ptr, ulong handle, [MarshalAs(UnmanagedType.LPStr)] string pchTitle);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_SetItemDescription(IntPtr ptr, ulong handle, [MarshalAs(UnmanagedType.LPStr)] string pchDescription);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_SetItemUpdateLanguage(IntPtr ptr, ulong handle, [MarshalAs(UnmanagedType.LPStr)] string pchLanguage);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_SetItemMetadata(IntPtr ptr, ulong handle, [MarshalAs(UnmanagedType.LPStr)] string pchMetaData);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_SetItemVisibility(IntPtr ptr, ulong handle, Steamworks.SteamRemoteStorage.ERemoteStoragePublishedFileVisibility eVisibility);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_SetItemTags(IntPtr ptr, ulong updateHandle, IntPtr pTags);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_SetItemContent(IntPtr ptr, ulong handle, [MarshalAs(UnmanagedType.LPStr)] string pszContentFolder);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_SetItemPreview(IntPtr ptr, ulong handle, [MarshalAs(UnmanagedType.LPStr)] string pszPreviewFile);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_RemoveItemKeyValueTags(IntPtr ptr, ulong handle, [MarshalAs(UnmanagedType.LPStr)] string pchKey);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_AddItemKeyValueTag(IntPtr ptr, ulong handle, [MarshalAs(UnmanagedType.LPStr)] string pchKey, [MarshalAs(UnmanagedType.LPStr)] string pchValue);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamUGC_SubmitItemUpdate(IntPtr ptr, ulong handle, [MarshalAs(UnmanagedType.LPStr)] string pchChangeNote);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern Steamworks.SteamUGC.EItemUpdateStatus ISteamUGC_GetItemUpdateProgress(IntPtr ptr, ulong handle, out ulong punBytesProcessed, out ulong punBytesTotal);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamUGC_SetUserItemVote(IntPtr ptr, ulong nPublishedFileID, bool bVoteUp);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamUGC_GetUserItemVote(IntPtr ptr, ulong nPublishedFileID);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamUGC_AddItemToFavorites(IntPtr ptr, uint nAppId, ulong nPublishedFileID);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamUGC_RemoveItemFromFavorites(IntPtr ptr, uint nAppId, ulong nPublishedFileID);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamUGC_SubscribeItem(IntPtr ptr, ulong nPublishedFileID);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamUGC_UnsubscribeItem(IntPtr ptr, ulong nPublishedFileID);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern uint ISteamUGC_GetNumSubscribedItems(IntPtr ptr);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern uint ISteamUGC_GetSubscribedItems(IntPtr ptr, IntPtr pvecPublishedFileID, uint cMaxEntries);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern uint ISteamUGC_GetItemState(IntPtr ptr, ulong nPublishedFileID);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_GetItemInstallInfo(IntPtr ptr, ulong nPublishedFileID, out ulong punSizeOnDisk, IntPtr pchFolder, uint cchFolderSize, out uint punTimeStamp);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_GetItemDownloadInfo(IntPtr ptr, ulong nPublishedFileID, out ulong punBytesDownloaded, out ulong punBytesTotal);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUGC_DownloadItem(IntPtr ptr, ulong nPublishedFileID, bool bHighPriority);

			public const int k_cchPublishedDocumentTitleMax = 129;

			public const int k_cchPublishedDocumentDescriptionMax = 8000;

			public const int k_cchPublishedDocumentChangeDescriptionMax = 8000;

			public const int k_unEnumeratePublishedFilesMaxResults = 50;

			public const int k_cchTagListMax = 1025;

			public const int k_cchFilenameMax = 260;

			public const int k_cchPublishedFileURLMax = 256;

			public const int k_cchCacheSize = 2048;

			private HandleRef handle;

			[Flags]
			public enum EItemState : uint
			{
				k_EItemStateNone = 0u,
				k_EItemStateSubscribed = 1u,
				k_EItemStateLegacyItem = 2u,
				k_EItemStateInstalled = 4u,
				k_EItemStateNeedsUpdate = 8u,
				k_EItemStateDownloading = 16u,
				k_EItemStateDownloadPending = 32u
			}

			public enum EItemStatistic
			{
				k_EItemStatistic_NumSubscriptions,
				k_EItemStatistic_NumFavorites,
				k_EItemStatistic_NumFollowers,
				k_EItemStatistic_NumUniqueSubscriptions,
				k_EItemStatistic_NumUniqueFavorites,
				k_EItemStatistic_NumUniqueFollowers,
				k_EItemStatistic_NumUniqueWebsiteViews,
				k_EItemStatistic_ReportScore
			}

			public enum EItemUpdateStatus
			{
				k_EItemUpdateStatusInvalid,
				k_EItemUpdateStatusPreparingConfig,
				k_EItemUpdateStatusPreparingContent,
				k_EItemUpdateStatusUploadingContent,
				k_EItemUpdateStatusUploadingPreviewFile,
				k_EItemUpdateStatusCommittingChanges
			}

			public enum EUserUGCList
			{
				k_EUserUGCList_Published,
				k_EUserUGCList_VotedOn,
				k_EUserUGCList_VotedUp,
				k_EUserUGCList_VotedDown,
				k_EUserUGCList_WillVoteLater,
				k_EUserUGCList_Favorited,
				k_EUserUGCList_Subscribed,
				k_EUserUGCList_UsedOrPlayed,
				k_EUserUGCList_Followed
			}

			public enum EUserUGCListSortOrder
			{
				k_EUserUGCListSortOrder_CreationOrderDesc,
				k_EUserUGCListSortOrder_CreationOrderAsc,
				k_EUserUGCListSortOrder_TitleAsc,
				k_EUserUGCListSortOrder_LastUpdatedDesc,
				k_EUserUGCListSortOrder_SubscriptionDateDesc,
				k_EUserUGCListSortOrder_VoteScoreDesc,
				k_EUserUGCListSortOrder_ForModeration
			}

			public enum EUGCMatchingUGCType
			{
				k_EUGCMatchingUGCType_Items,
				k_EUGCMatchingUGCType_Items_Mtx,
				k_EUGCMatchingUGCType_Items_ReadyToUse,
				k_EUGCMatchingUGCType_Collections,
				k_EUGCMatchingUGCType_Artwork,
				k_EUGCMatchingUGCType_Videos,
				k_EUGCMatchingUGCType_Screenshots,
				k_EUGCMatchingUGCType_AllGuides,
				k_EUGCMatchingUGCType_WebGuides,
				k_EUGCMatchingUGCType_IntegratedGuides,
				k_EUGCMatchingUGCType_UsableInGame,
				k_EUGCMatchingUGCType_ControllerBindings,
				k_EUGCMatchingUGCType_GameManagedItems
			}

			public enum EUGCQuery
			{
				k_EUGCQuery_RankedByVote,
				k_EUGCQuery_RankedByPublicationDate,
				k_EUGCQuery_AcceptedForGameRankedByAcceptanceDate,
				k_EUGCQuery_RankedByTrend,
				k_EUGCQuery_FavoritedByFriendsRankedByPublicationDate,
				k_EUGCQuery_CreatedByFriendsRankedByPublicationDate,
				k_EUGCQuery_RankedByNumTimesReported,
				k_EUGCQuery_CreatedByFollowedUsersRankedByPublicationDate,
				k_EUGCQuery_NotYetRated,
				k_EUGCQuery_RankedByTotalVotesAsc,
				k_EUGCQuery_RankedByVotesUp,
				k_EUGCQuery_RankedByTextSearch,
				k_EUGCQuery_RankedByTotalUniqueSubscriptions
			}

			[StructLayout(LayoutKind.Sequential, Pack = 8)]
			public struct SteamUGCDetails
			{
				public ulong m_nPublishedFileId;

				public Steamworks.EResult m_eResult;

				public Steamworks.SteamRemoteStorage.EWorkshopFileType m_eFileType;

				public uint m_nCreatorAppID;

				public uint m_nConsumerAppID;

				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 129)]
				public string m_rgchTitle;

				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8000)]
				public string m_rgchDescription;

				public ulong m_ulSteamIDOwner;

				public uint m_rtimeCreated;

				public uint m_rtimeUpdated;

				public uint m_rtimeAddedToUserList;

				public Steamworks.SteamRemoteStorage.ERemoteStoragePublishedFileVisibility m_eVisibility;

				[MarshalAs(UnmanagedType.I1)]
				public bool m_bBanned;

				[MarshalAs(UnmanagedType.I1)]
				public bool m_bAcceptedForUse;

				[MarshalAs(UnmanagedType.I1)]
				public bool m_bTagsTruncated;

				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1025)]
				public string m_rgchTags;

				public ulong m_hFile;

				public ulong m_hPreviewFile;

				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
				public string m_pchFileName;

				public uint m_nFileSize;

				public uint m_nPreviewFileSize;

				[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
				public string m_rgchURL;

				public uint m_unVotesUp;

				public uint m_unVotesDown;

				public float m_flScore;

				public uint m_unNumChildren;
			}

			[StructLayout(LayoutKind.Sequential, Pack = 8)]
			private struct SteamParamStringArray_t
			{
				public IntPtr m_ppStrings;

				public int m_nNumStrings;
			}

			private class SteamParamStringArray
			{
				public SteamParamStringArray(IList<string> strings)
				{
					if (strings == null)
					{
						this.steamParamStringArray = IntPtr.Zero;
						return;
					}
					this.allocHGlobalStrings = new IntPtr[strings.Count];
					for (int i = 0; i < strings.Count; i++)
					{
						byte[] array = new byte[Encoding.UTF8.GetByteCount(strings[i]) + 1];
						Encoding.UTF8.GetBytes(strings[i], 0, strings[i].Length, array, 0);
						this.allocHGlobalStrings[i] = Marshal.AllocHGlobal(array.Length);
						Marshal.Copy(array, 0, this.allocHGlobalStrings[i], array.Length);
					}
					this.ptrStrings = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(IntPtr)) * this.allocHGlobalStrings.Length);
					Steamworks.SteamUGC.SteamParamStringArray_t steamParamStringArray_t = new Steamworks.SteamUGC.SteamParamStringArray_t
					{
						m_ppStrings = this.ptrStrings,
						m_nNumStrings = this.allocHGlobalStrings.Length
					};
					Marshal.Copy(this.allocHGlobalStrings, 0, steamParamStringArray_t.m_ppStrings, this.allocHGlobalStrings.Length);
					this.steamParamStringArray = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Steamworks.SteamUGC.SteamParamStringArray_t)));
					Marshal.StructureToPtr(steamParamStringArray_t, this.steamParamStringArray, false);
				}

				protected override void Finalize()
				{
					try
					{
						foreach (IntPtr hglobal in this.allocHGlobalStrings)
						{
							Marshal.FreeHGlobal(hglobal);
						}
						if (this.ptrStrings != IntPtr.Zero)
						{
							Marshal.FreeHGlobal(this.ptrStrings);
						}
						if (this.steamParamStringArray != IntPtr.Zero)
						{
							Marshal.FreeHGlobal(this.steamParamStringArray);
						}
					}
					finally
					{
						base.Finalize();
					}
				}

				public static implicit operator IntPtr(Steamworks.SteamUGC.SteamParamStringArray that)
				{
					return that.steamParamStringArray;
				}

				private IntPtr[] allocHGlobalStrings;

				private IntPtr ptrStrings;

				private IntPtr steamParamStringArray;
			}
		}

		public struct SteamServersConnected
		{
			public const int k_iCallback = 101;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct SteamServerConnectFailure
		{
			public const int k_iCallback = 102;

			[MarshalAs(UnmanagedType.I4)]
			public Steamworks.EResult m_eResult;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct SteamServersDisconnected
		{
			public const int k_iCallback = 103;

			[MarshalAs(UnmanagedType.I4)]
			public Steamworks.EResult m_eResult;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct ValidateAuthTicketResponse
		{
			public const int k_iCallback = 143;

			public ulong m_SteamID;

			[MarshalAs(UnmanagedType.I4)]
			public Steamworks.EAuthSessionResponse m_eAuthSessionResponse;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct IPCFailure
		{
			public const int k_iCallback = 117;

			public byte m_eFailureType;

			public enum EFailureType
			{
				k_EFailureFlushedCallbackQueue,
				k_EFailurePipeFail
			}
		}

		public class SteamUser
		{
			internal SteamUser(IntPtr ptr)
			{
				this.handle = new HandleRef(this, ptr);
			}

			public Steamworks.SteamID SteamID
			{
				get
				{
					ulong num = Steamworks.SteamUser.ISteamUser_GetSteamID(this.Handle);
					if (num != 0UL)
					{
						return new Steamworks.SteamID(num);
					}
					return Steamworks.SteamID.Zero;
				}
			}

			private IntPtr Handle
			{
				get
				{
					return this.handle.Handle;
				}
			}

			public void CancelAuthTicket(uint hAuthTicket)
			{
				Steamworks.SteamUser.ISteamUser_CancelAuthTicket(this.Handle, hAuthTicket);
			}

			public uint GetAuthSessionTicket(ref byte[] pTicket, uint cbMaxTicket, out uint pcbTicket)
			{
				GCHandle gchandle = GCHandle.Alloc(pTicket, GCHandleType.Pinned);
				uint result = 0u;
				try
				{
					result = Steamworks.SteamUser.ISteamUser_GetAuthSessionTicket(this.Handle, gchandle.AddrOfPinnedObject(), cbMaxTicket, out pcbTicket);
				}
				finally
				{
					gchandle.Free();
				}
				return result;
			}

			public void TerminateGameConnection(uint unServerIP, ushort usServerPort)
			{
				Steamworks.SteamUser.ISteamUser_TerminateGameConnection(unServerIP, usServerPort);
			}

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamUser_CancelAuthTicket(IntPtr ptr, uint hAuthTicket);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamUser_GetSteamID(IntPtr ptr);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern uint ISteamUser_GetAuthSessionTicket(IntPtr ptr, [In] [Out] IntPtr pTicket, uint cbMaxTicket, out uint pcbTicket);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern void ISteamUser_TerminateGameConnection(uint unServerIP, ushort usServerPort);

			private HandleRef handle;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct UserStatsReceived
		{
			public ulong m_nGameID;

			[MarshalAs(UnmanagedType.I4)]
			public Steamworks.EResult m_eResult;

			public ulong m_steamIDUser;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct UserStatsStored
		{
			public ulong m_nGameID;

			[MarshalAs(UnmanagedType.I4)]
			public Steamworks.EResult m_eResult;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		public struct UserAchievementStored
		{
			public ulong m_nGameID;

			public byte m_bGroupAchievement;

			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string m_rgchAchievementName;

			public uint m_nCurProgress;

			public uint m_nMaxProgress;
		}

		public class SteamUserStats
		{
			internal SteamUserStats(IntPtr ptr)
			{
				this.handle = new HandleRef(this, ptr);
			}

			private IntPtr Handle
			{
				get
				{
					return this.handle.Handle;
				}
			}

			public bool RequestCurrentStats()
			{
				return Steamworks.SteamUserStats.ISteamUserStats_RequestCurrentStats(this.Handle);
			}

			public void RegisterAchievement(int achievementId, string achivementName)
			{
				Steamworks.SteamUserStats.ISteamUserStats_RegisterAchievement(achievementId, achivementName);
			}

			public bool GetStat(string statName, ref int value)
			{
				return Steamworks.SteamUserStats.ISteamUserStats_GetStat(this.Handle, statName, ref value);
			}

			public bool GetStatF(string statName, ref float value)
			{
				return Steamworks.SteamUserStats.ISteamUserStats_GetStatF(this.Handle, statName, ref value);
			}

			public bool GetGlobalStat(string statName, ref long value)
			{
				return Steamworks.SteamUserStats.ISteamUserStats_GetGlobalStat(this.Handle, statName, ref value);
			}

			public bool GetGlobalStatF(string statName, ref double value)
			{
				return Steamworks.SteamUserStats.ISteamUserStats_GetGlobalStatF(this.Handle, statName, ref value);
			}

			public bool SetStat(string statName, int value)
			{
				return Steamworks.SteamUserStats.ISteamUserStats_SetStat(this.Handle, statName, value);
			}

			public bool SetStatF(string statName, float value)
			{
				return Steamworks.SteamUserStats.ISteamUserStats_SetStatF(this.Handle, statName, value);
			}

			public bool UpdateAverateStat(string statname, float valueForThisSession, double sessionLength)
			{
				return Steamworks.SteamUserStats.ISteamUserStats_UpdateAvgRateStat(this.Handle, statname, valueForThisSession, sessionLength);
			}

			public bool GetAchievement(string achievementName, ref bool achieved)
			{
				return Steamworks.SteamUserStats.ISteamUserStats_GetAchievement(this.Handle, achievementName, ref achieved);
			}

			public bool SetAchievement(string achievementName)
			{
				return Steamworks.SteamUserStats.ISteamUserStats_SetAchievement(this.Handle, achievementName);
			}

			public bool ClearAchievement(string achievementName)
			{
				return Steamworks.SteamUserStats.ISteamUserStats_ClearAchievement(this.Handle, achievementName);
			}

			public bool StoreStats()
			{
				return this.Handle != IntPtr.Zero && Steamworks.SteamUserStats.ISteamUserStats_StoreStats(this.Handle);
			}

			public bool ResetAllStatistics(bool resetAchievements)
			{
				return Steamworks.SteamUserStats.ISteamUserStats_ResetAllStats(this.Handle, resetAchievements);
			}

			public ulong FindLeaderboard(string leaderboardName)
			{
				return Steamworks.SteamUserStats.ISteamUserStats_FindLeaderboard(this.Handle, leaderboardName);
			}

			public ulong UploadLeaderboardScore(ulong steamLeaderboardHandle, ELeaderboardUploadScoreMethod leaderboardUploadScoreMethod, int score, ref int scoreDetails, int scoreDetailsCount)
			{
				return Steamworks.SteamUserStats.ISteamUserStats_UploadLeaderboardScore(this.Handle, steamLeaderboardHandle, leaderboardUploadScoreMethod, score, ref scoreDetails, scoreDetailsCount);
			}

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUserStats_RequestCurrentStats(IntPtr ptr);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUserStats_GetStat(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchName, ref int pData);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUserStats_GetStatF(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchName, ref float pData);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUserStats_GetGlobalStat(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchName, ref long pData);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUserStats_GetGlobalStatF(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchName, ref double pData);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUserStats_SetStat(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchName, int nData);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUserStats_SetStatF(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchName, float fData);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUserStats_UpdateAvgRateStat(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchName, float flCountThisSession, double dSessionLength);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUserStats_GetAchievement(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchName, ref bool pbAchieved);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUserStats_SetAchievement(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchName);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUserStats_ClearAchievement(IntPtr ptr, [MarshalAs(UnmanagedType.LPStr)] string pchName);

			[DllImport("steam_api64", CallingConvention = CallingConvention.Cdecl)]
			public static extern void ISteamUserStats_RegisterAchievement(int appID, string storeID);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUserStats_StoreStats(IntPtr ptr);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUserStats_ResetAllStats(IntPtr ptr, bool bAchievementsToo);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamUserStats_FindLeaderboard(IntPtr ptr, string pchLeaderboardName);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern ulong ISteamUserStats_UploadLeaderboardScore(IntPtr ptr, ulong hSteamLeaderboard, ELeaderboardUploadScoreMethod eLeaderboardUploadScoreMethod, int nScore, ref int pScoreDetails, int cScoreDetailsCount);

			private HandleRef handle;
		}

		public struct SteamShutdown
		{
			public const int k_iCallback = 704;
		}

		public class SteamUtils
		{
			internal SteamUtils(IntPtr ptr)
			{
				this.handle = new HandleRef(this, ptr);
			}

			public uint AppID
			{
				get
				{
					return Steamworks.SteamUtils.ISteamUtils_GetAppID(this.Handle);
				}
			}

			public bool IsOverlayEnabled
			{
				get
				{
					return Steamworks.SteamUtils.ISteamUtils_IsOverlayEnabled(this.Handle);
				}
			}

			private IntPtr Handle
			{
				get
				{
					return this.handle.Handle;
				}
			}

			public void SetOverlayNotificationPosition(Steamworks.SteamUtils.ENotificationPosition notificationPosition)
			{
				Steamworks.SteamUtils.ISteamUtils_SetOverlayNotificationPosition(this.Handle, (int)notificationPosition);
			}

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern uint ISteamUtils_GetAppID(IntPtr ptr);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUtils_IsOverlayEnabled(IntPtr ptr);

			[DllImport("steam_api_dotnetwrapper64", CallingConvention = CallingConvention.Cdecl)]
			private static extern bool ISteamUtils_SetOverlayNotificationPosition(IntPtr ptr, int eNotificationPosition);

			private HandleRef handle;

			public enum ENotificationPosition
			{
				k_EPositionTopLeft,
				k_EPositionTopRight,
				k_EPositionBottomLeft,
				k_EPositionBottomRight
			}
		}

		public enum EResult
		{
			k_EResultOK = 1,
			k_EResultFail,
			k_EResultNoConnection,
			k_EResultInvalidPassword = 5,
			k_EResultLoggedInElsewhere,
			k_EResultInvalidProtocolVer,
			k_EResultInvalidParam,
			k_EResultFileNotFound,
			k_EResultBusy,
			k_EResultInvalidState,
			k_EResultInvalidName,
			k_EResultInvalidEmail,
			k_EResultDuplicateName,
			k_EResultAccessDenied,
			k_EResultTimeout,
			k_EResultBanned,
			k_EResultAccountNotFound,
			k_EResultInvalidSteamID,
			k_EResultServiceUnavailable,
			k_EResultNotLoggedOn,
			k_EResultPending,
			k_EResultEncryptionFailure,
			k_EResultInsufficientPrivilege,
			k_EResultLimitExceeded,
			k_EResultRevoked,
			k_EResultExpired,
			k_EResultAlreadyRedeemed,
			k_EResultDuplicateRequest,
			k_EResultAlreadyOwned,
			k_EResultIPNotFound,
			k_EResultPersistFailed,
			k_EResultLockingFailed,
			k_EResultLogonSessionReplaced,
			k_EResultConnectFailed,
			k_EResultHandshakeFailed,
			k_EResultIOFailure,
			k_EResultRemoteDisconnect,
			k_EResultShoppingCartNotFound,
			k_EResultBlocked,
			k_EResultIgnored,
			k_EResultNoMatch,
			k_EResultAccountDisabled,
			k_EResultServiceReadOnly,
			k_EResultAccountNotFeatured,
			k_EResultAdministratorOK,
			k_EResultContentVersion,
			k_EResultTryAnotherCM,
			k_EResultPasswordRequiredToKickSession,
			k_EResultAlreadyLoggedInElsewhere,
			k_EResultSuspended,
			k_EResultCancelled,
			k_EResultDataCorruption,
			k_EResultDiskFull,
			k_EResultRemoteCallFailed,
			k_EResultPasswordUnset,
			k_EResultExternalAccountUnlinked,
			k_EResultPSNTicketInvalid,
			k_EResultExternalAccountAlreadyLinked,
			k_EResultRemoteFileConflict,
			k_EResultIllegalPassword,
			k_EResultSameAsPreviousValue,
			k_EResultAccountLogonDenied,
			k_EResultCannotUseOldPassword,
			k_EResultInvalidLoginAuthCode,
			k_EResultAccountLogonDeniedNoMail,
			k_EResultHardwareNotCapableOfIPT,
			k_EResultIPTInitError,
			k_EResultParentalControlRestricted,
			k_EResultFacebookQueryError,
			k_EResultExpiredLoginAuthCode,
			k_EResultIPLoginRestrictionFailed
		}

		public enum EVoiceResult
		{
			k_EVoiceResultOK,
			k_EVoiceResultNotInitialized,
			k_EVoiceResultNotRecording,
			k_EVoiceResultNoData,
			k_EVoiceResultBufferTooSmall,
			k_EVoiceResultDataCorrupted,
			k_EVoiceResultRestricted,
			k_EVoiceResultUnsupportedCodec
		}

		public enum EDenyReason
		{
			k_EDenyInvalid,
			k_EDenyInvalidVersion,
			k_EDenyGeneric,
			k_EDenyNotLoggedOn,
			k_EDenyNoLicense,
			k_EDenyCheater,
			k_EDenyLoggedInElseWhere,
			k_EDenyUnknownText,
			k_EDenyIncompatibleAnticheat,
			k_EDenyMemoryCorruption,
			k_EDenyIncompatibleSoftware,
			k_EDenySteamConnectionLost,
			k_EDenySteamConnectionError,
			k_EDenySteamResponseTimedOut,
			k_EDenySteamValidationStalled,
			k_EDenySteamOwnerLeftGuestUser
		}

		public enum EBeginAuthSessionResult
		{
			k_EBeginAuthSessionResultOK,
			k_EBeginAuthSessionResultInvalidTicket,
			k_EBeginAuthSessionResultDuplicateRequest,
			k_EBeginAuthSessionResultInvalidVersion,
			k_EBeginAuthSessionResultGameMismatch,
			k_EBeginAuthSessionResultExpiredTicket
		}

		public enum EAuthSessionResponse
		{
			k_EAuthSessionResponseOK,
			k_EAuthSessionResponseUserNotConnectedToSteam,
			k_EAuthSessionResponseNoLicenseOrExpired,
			k_EAuthSessionResponseVACBanned,
			k_EAuthSessionResponseLoggedInElseWhere,
			k_EAuthSessionResponseVACCheckTimedOut,
			k_EAuthSessionResponseAuthTicketCanceled,
			k_EAuthSessionResponseAuthTicketInvalidAlreadyUsed,
			k_EAuthSessionResponseAuthTicketInvalid
		}

		public enum EUniverse
		{
			k_EUniverseInvalid,
			k_EUniversePublic,
			k_EUniverseBeta,
			k_EUniverseInternal,
			k_EUniverseDev,
			k_EUniverseRC,
			k_EUniverseMax
		}

		public enum EAccountType
		{
			k_EAccountTypeInvalid,
			k_EAccountTypeIndividual,
			k_EAccountTypeMultiseat,
			k_EAccountTypeGameServer,
			k_EAccountTypeAnonGameServer,
			k_EAccountTypePending,
			k_EAccountTypeContentServer,
			k_EAccountTypeClan,
			k_EAccountTypeChat,
			k_EAccountTypeConsoleUser,
			k_EAccountTypeAnonUser,
			k_EAccountTypeMax
		}

		[StructLayout(LayoutKind.Explicit, Pack = 8, Size = 8)]
		public class SteamID
		{
			public SteamID()
			{
			}

			public SteamID(ulong value)
			{
				this.__quadPart = value;
			}

			public uint AccountID
			{
				get
				{
					return this.__lowPart;
				}
				set
				{
					this.__lowPart = value;
				}
			}

			public ulong UInt64AccountID
			{
				get
				{
					return this.__quadPart;
				}
				set
				{
					this.__quadPart = value;
				}
			}

			public uint AccountInstance
			{
				get
				{
					return this.__highPart & 1048575u;
				}
				set
				{
					this.__highPart = (this.__highPart & 4293918720u) + (value & 1048575u);
				}
			}

			public Steamworks.EAccountType AccountType
			{
				get
				{
					return (Steamworks.EAccountType)(this.__highPart >> 20 & 15u);
				}
				set
				{
					this.__highPart = (uint)((int)(this.__highPart & 4279238655u) + ((uint)(value & (Steamworks.EAccountType)15) << 20));
				}
			}

			public bool IsValid
			{
				get
				{
					return this.AccountType > Steamworks.EAccountType.k_EAccountTypeInvalid && this.AccountType < Steamworks.EAccountType.k_EAccountTypeMax && this.Universe > Steamworks.EUniverse.k_EUniverseInvalid && this.Universe < Steamworks.EUniverse.k_EUniverseMax && (this.AccountType != Steamworks.EAccountType.k_EAccountTypeIndividual || (this.AccountID != 0u && this.AccountInstance <= 4u)) && (this.AccountType != Steamworks.EAccountType.k_EAccountTypeClan || (this.AccountID != 0u && this.AccountInstance == 0u)) && (this.AccountType != Steamworks.EAccountType.k_EAccountTypeGameServer || this.AccountID != 0u);
				}
			}

			public Steamworks.EUniverse Universe
			{
				get
				{
					return (Steamworks.EUniverse)(this.__highPart >> 24 & 255u);
				}
				set
				{
					this.__highPart = (uint)((int)(this.__highPart & 16777215u) + ((uint)(value & (Steamworks.EUniverse)255) << 24));
				}
			}

			public override bool Equals(object x)
			{
				return !object.ReferenceEquals(x, null) && x.GetType() == typeof(Steamworks.SteamID) && (x as Steamworks.SteamID).__quadPart == this.__quadPart;
			}

			public override int GetHashCode()
			{
				return (int)this.__quadPart;
			}

			public override string ToString()
			{
				return "0x" + this.__quadPart.ToString("x16");
			}

			public static implicit operator ulong(Steamworks.SteamID steamID)
			{
				return steamID.__quadPart;
			}

			public static bool operator ==(Steamworks.SteamID x, Steamworks.SteamID y)
			{
				return (object.ReferenceEquals(x, null) && object.ReferenceEquals(y, null)) || (!object.ReferenceEquals(x, null) && !object.ReferenceEquals(y, null) && x.__quadPart == y.__quadPart);
			}

			public static bool operator !=(Steamworks.SteamID x, Steamworks.SteamID y)
			{
				return (!object.ReferenceEquals(x, null) || !object.ReferenceEquals(y, null)) && (object.ReferenceEquals(x, null) || object.ReferenceEquals(y, null) || x.__quadPart != y.__quadPart);
			}

			public const uint k_unSteamAccountIDMask = 4294967295u;

			public const uint k_unSteamAccountInstanceMask = 1048575u;

			public const uint k_unSteamUserDesktopInstance = 1u;

			public const uint k_unSteamUserConsoleInstance = 2u;

			public const uint k_unSteamUserWebInstance = 4u;

			public static Steamworks.SteamID Zero = new Steamworks.SteamID();

			[FieldOffset(0)]
			private uint __lowPart;

			[FieldOffset(4)]
			private uint __highPart;

			[FieldOffset(0)]
			private ulong __quadPart;
		}

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void GameOverlayActivatedCallback(ref Steamworks.GameOverlayActivated message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void GameLobbyJoinRequestedCallback(ref Steamworks.GameLobbyJoinRequested message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void GameRichPresenceJoinRequestedCallback(ref Steamworks.GameRichPresenceJoinRequested message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void GSPolicyResponseCallback(ref Steamworks.GSPolicyResponse message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void P2PSessionRequestCallback(ref Steamworks.P2PSessionRequest message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void P2PSessionConnectFailCallback(ref Steamworks.P2PSessionConnectFail message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SteamServersConnectedCallback(ref Steamworks.SteamServersConnected message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SteamServerConnectFailureCallback(ref Steamworks.SteamServerConnectFailure message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SteamServersDisconnectedCallback(ref Steamworks.SteamServersDisconnected message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void ValidateAuthTicketResponseCallback(ref Steamworks.ValidateAuthTicketResponse message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void IPCFailureCallback(ref Steamworks.IPCFailure message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void AchievementStoredCallback(ref Steamworks.UserAchievementStored message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void UserStatsReceivedCallback(ref Steamworks.UserStatsReceived message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void UserStatsStoredCallback(ref Steamworks.UserStatsStored message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SteamShutdownCallback(ref Steamworks.SteamShutdown message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void LobbyChatMsgCallback(ref Steamworks.LobbyChatMsg message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void LobbyChatUpdateCallback(ref Steamworks.LobbyChatUpdate message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void LobbyCreatedCallback(ref Steamworks.LobbyCreated message, bool ioFailure);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void LobbyDataUpdateCallback(ref Steamworks.LobbyDataUpdate message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void LobbyEnterCallback(ref Steamworks.LobbyEnter message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void LobbyGameCreatedCallback(ref Steamworks.LobbyGameCreated message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void LobbyInviteCallback(ref Steamworks.LobbyInvite message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void LobbyKickedCallback(ref Steamworks.LobbyKicked message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void RequestLobbyListCallback(ref Steamworks.RequestLobbyList message, bool ioFailure);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void PersonaStateChangedCallback(ref Steamworks.PersonaStateChange message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void RemoteStoragePublishedFileSubscribedCallback(ref Steamworks.RemoteStoragePublishedFileSubscribed message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void RemoteStoragePublishedFileUnsubscribedCallback(ref Steamworks.RemoteStoragePublishedFileUnsubscribed message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void CreateItemCallback(ref Steamworks.CreateItem message, bool ioFailure);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void DownloadItemResultCallback(ref Steamworks.DownloadItemResult message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void ItemInstalledCallback(ref Steamworks.ItemInstalled message);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SteamUGCQueryCompletedCallback(ref Steamworks.SteamUGCQueryCompleted message, bool ioFailure);

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SubmitItemUpdateCallback(ref Steamworks.SubmitItemUpdate message, bool ioFailure);
	}
}
