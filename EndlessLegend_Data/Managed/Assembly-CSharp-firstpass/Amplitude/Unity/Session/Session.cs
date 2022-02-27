using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amplitude.Interop;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Steam;
using UnityEngine;

namespace Amplitude.Unity.Session
{
	[Diagnostics.TagAttribute("Session")]
	public class Session : IDisposable
	{
		public Session()
		{
			ISteamService service = Services.GetService<ISteamService>();
			if (service.IsSteamRunning)
			{
				this.SteamIDUser = service.SteamUser.SteamID;
			}
			else
			{
				this.SteamIDUser = new Steamworks.SteamID();
			}
		}

		public event EventHandler<LobbyDataChangeEventArgs> LobbyDataChange;

		public event EventHandler<LobbyMemberDataChangeEventArgs> LobbyMemberDataChange;

		public event EventHandler<LobbyChatMessageEventArgs> LobbyChatMessage;

		public event EventHandler<SessionChangeEventArgs> SessionChange;

		public event EventHandler<LobbyChatUpdateEventArgs> LobbyChatUpdate;

		public event EventHandler<LobbyEnterEventArgs> LobbyEnter;

		~Session()
		{
			this.Dispose(false);
		}

		public static Steamworks.SteamID IgnoreP2PSessionConnectFail { get; set; }

		public int MaximumNumberOfLobbyMembers { get; private set; }

		public Steamworks.SteamID LobbyOwnerSteamID
		{
			get
			{
				if (this.SteamIDLobby != null && this.SteamIDLobby.IsValid)
				{
					return this.SteamMatchMakingService.SteamMatchMaking.GetLobbyOwner(this.SteamIDLobby);
				}
				return Steamworks.SteamID.Zero;
			}
		}

		public bool IsHosting
		{
			get
			{
				if (this.SteamIDLobby != null && this.SteamIDLobby.IsValid)
				{
					Steamworks.SteamID lobbyOwner = this.SteamMatchMakingService.SteamMatchMaking.GetLobbyOwner(this.SteamIDLobby);
					return lobbyOwner == this.SteamIDUser;
				}
				return this.hosting;
			}
		}

		public bool IsOpened
		{
			get
			{
				return this.opened;
			}
		}

		public bool IsOpening
		{
			get
			{
				return this.opening;
			}
		}

		public bool IsReopening
		{
			get
			{
				return this.reopening;
			}
		}

		public SessionMode SessionMode { get; private set; }

		public Steamworks.SteamID SteamIDUser { get; private set; }

		public Steamworks.SteamID SteamIDLobby { get; private set; }

		public Steamworks.SteamID SteamIDServer { get; set; }

		private protected ISteamMatchMakingService SteamMatchMakingService { protected get; private set; }

		public void Close()
		{
			if (this.opened || this.opening || this.reopening)
			{
				this.opened = false;
				this.opening = false;
				this.reopening = false;
				if (this.SteamMatchMakingService != null)
				{
					this.SteamMatchMakingService.SteamLobbyChatMsg -= this.SteamMatchMakingService_SteamLobbyChatMsg;
					this.SteamMatchMakingService.SteamLobbyChatUpdate -= this.SteamMatchMakingService_SteamLobbyChatUpdate;
					this.SteamMatchMakingService.SteamLobbyDataUpdate -= this.SteamMatchMakingService_SteamLobbyDataUpdate;
					this.SteamMatchMakingService.SteamLobbyEnter -= this.SteamMatchMakingService_SteamLobbyEnter;
					this.SteamMatchMakingService.SteamLobbyGameCreated -= this.SteamMatchMakingService_SteamLobbyGameCreated;
					this.SteamMatchMakingService.SteamLobbyInvite -= this.SteamMatchMakingService_SteamLobbyInvite;
					this.SteamMatchMakingService.SteamLobbyKicked -= this.SteamMatchMakingService_SteamLobbyKicked;
					ISteamClientService service = Services.GetService<ISteamClientService>();
					Diagnostics.Assert(service != null);
					service.ClientSteamServersDisconnected -= this.ISteamClientService_ClientSteamServersDisconnected;
					service.ClientSteamServerConnectFailure -= this.ISteamClientService_ClientSteamServerConnectFailure;
					service.ClientP2PSessionConnectFail -= this.ISteamClientService_ClientP2PSessionConnectFail;
					if (this.SteamIDLobby != null)
					{
						this.SteamMatchMakingService.SteamMatchMaking.LeaveLobby(this.SteamIDLobby);
						this.SteamIDLobby = null;
					}
					this.SteamMatchMakingService = null;
				}
				if (!this.disposing)
				{
					this.OnSessionChange(new SessionChangeEventArgs(SessionChangeAction.Closed, this));
				}
				this.hosting = false;
			}
			if (Session.lobbyCreatedCallback != null)
			{
				Session.lobbyCreatedCallback = null;
				if (this.lobbyCreatedCallResult != null)
				{
					this.lobbyCreatedCallResult.Dispose();
					this.lobbyCreatedCallResult = null;
				}
			}
			Session.lobbyCreatedCallback = null;
		}

		public void Dispose()
		{
			this.Dispose(true);
		}

		public T GetLobbyData<T>(StaticString key, T defaultValue)
		{
			object value;
			if (this.lobbyData.TryGetValue(key, out value))
			{
				try
				{
					return (T)((object)Convert.ChangeType(value, typeof(T)));
				}
				catch
				{
					return defaultValue;
				}
				return defaultValue;
			}
			return defaultValue;
		}

		public object GetLobbyData(StaticString key)
		{
			object result;
			if (this.lobbyData.TryGetValue(key, out result))
			{
				return result;
			}
			if (this.lobbyData.TryGetValue(key.ToString().ToLower(), out result))
			{
				return result;
			}
			return null;
		}

		public IEnumerable<StaticString> GetLobbyDataKeys()
		{
			return this.lobbyData.Keys.AsEnumerable<StaticString>();
		}

		public T GetLobbyMemberData<T>(Steamworks.SteamID steamIDMember, StaticString key, T defaultValue)
		{
			if (this.SteamMatchMakingService != null && this.SteamIDLobby != null)
			{
				string lobbyMemberData = this.SteamMatchMakingService.SteamMatchMaking.GetLobbyMemberData(this.SteamIDLobby, steamIDMember, key);
				if (!string.IsNullOrEmpty(lobbyMemberData))
				{
					try
					{
						return (T)((object)Convert.ChangeType(lobbyMemberData, typeof(T)));
					}
					catch
					{
						return defaultValue;
					}
					return defaultValue;
				}
			}
			return defaultValue;
		}

		public string GetLobbyMemberData(Steamworks.SteamID steamIDMember, StaticString key)
		{
			if (this.SteamMatchMakingService != null && this.SteamIDLobby != null)
			{
				return this.SteamMatchMakingService.SteamMatchMaking.GetLobbyMemberData(this.SteamIDLobby, steamIDMember, key);
			}
			return null;
		}

		public T[] GetLobbyMemberDataOfEveryone<T>(StaticString key, T defaultValue)
		{
			T[] result;
			try
			{
				List<T> list = new List<T>();
				Steamworks.SteamID[] lobbyMembers = this.GetLobbyMembers();
				for (int i = 0; i < lobbyMembers.Length; i++)
				{
					T lobbyMemberData = this.GetLobbyMemberData<T>(lobbyMembers[i], key, defaultValue);
					list.Add(lobbyMemberData);
				}
				result = list.ToArray();
			}
			catch (Exception ex)
			{
				Diagnostics.LogError("GetLobbyMemberDataOfEveryone [Lobby] Exception caught: {0}", new object[]
				{
					ex.Message
				});
				result = null;
			}
			return result;
		}

		public Steamworks.SteamID[] GetLobbyMembers()
		{
			if (this.SteamIDLobby == null)
			{
				this.OnError(Session.ErrorLevel.Error, "%SessionSteamISLobbyIsNull", 0);
				this.Close();
				return null;
			}
			Diagnostics.Assert(this.SteamMatchMakingService != null);
			this.steamIDMembers.Clear();
			int numLobbyMembers = this.SteamMatchMakingService.SteamMatchMaking.GetNumLobbyMembers(this.SteamIDLobby);
			for (int i = 0; i < numLobbyMembers; i++)
			{
				this.steamIDMember = this.SteamMatchMakingService.SteamMatchMaking.GetLobbyMemberByIndex(this.SteamIDLobby, i);
				this.steamIDMembers.Add(this.steamIDMember);
			}
			return this.steamIDMembers.ToArray();
		}

		public virtual void OnError(Session.ErrorLevel errorLevel, string text, int errorCode = 0)
		{
			Diagnostics.LogError("[Session] {0}: {1} (errorCode={2:X4})", new object[]
			{
				errorLevel,
				text,
				errorCode
			});
		}

		public void Open(SessionMode sessionMode, int maximumNumberOfLobbyMembers = 16)
		{
			if (this.opened)
			{
				throw new InvalidOperationException("The session is already opened; consider closing it first or calling 'Reopen' instead.");
			}
			this.opening = true;
			this.reopening = false;
			this.hosting = true;
			this.SessionMode = sessionMode;
			this.SetLobbyData("SessionMode", sessionMode.ToString(), true);
			this.SetLobbyData("Owner", this.SteamIDUser, true);
			this.SetLobbyData("Version", Amplitude.Unity.Framework.Application.Version.ToLong(), true);
			this.OnSessionChange(new SessionChangeEventArgs(SessionChangeAction.Opening, this));
			switch (this.SessionMode)
			{
			case SessionMode.Single:
				this.opened = true;
				this.opening = false;
				this.reopening = false;
				this.OnSessionChange(new SessionChangeEventArgs(SessionChangeAction.Opened, this));
				break;
			case SessionMode.Private:
			case SessionMode.Protected:
			case SessionMode.Public:
			{
				if (maximumNumberOfLobbyMembers < 1)
				{
					throw new ArgumentOutOfRangeException("The maximum number of lobby members should be greater that zero.");
				}
				this.MaximumNumberOfLobbyMembers = maximumNumberOfLobbyMembers;
				this.SteamMatchMakingService = Services.GetService<ISteamMatchMakingService>();
				if (this.SteamMatchMakingService == null)
				{
					throw new InvalidOperationException("Cannot find the steam match making service.");
				}
				this.SteamMatchMakingService.SteamLobbyChatMsg += this.SteamMatchMakingService_SteamLobbyChatMsg;
				this.SteamMatchMakingService.SteamLobbyChatUpdate += this.SteamMatchMakingService_SteamLobbyChatUpdate;
				this.SteamMatchMakingService.SteamLobbyDataUpdate += this.SteamMatchMakingService_SteamLobbyDataUpdate;
				this.SteamMatchMakingService.SteamLobbyEnter += this.SteamMatchMakingService_SteamLobbyEnter;
				this.SteamMatchMakingService.SteamLobbyGameCreated += this.SteamMatchMakingService_SteamLobbyGameCreated;
				this.SteamMatchMakingService.SteamLobbyInvite += this.SteamMatchMakingService_SteamLobbyInvite;
				this.SteamMatchMakingService.SteamLobbyKicked += this.SteamMatchMakingService_SteamLobbyKicked;
				ISteamClientService service = Services.GetService<ISteamClientService>();
				Diagnostics.Assert(service != null);
				service.ClientSteamServersDisconnected += this.ISteamClientService_ClientSteamServersDisconnected;
				service.ClientSteamServerConnectFailure += this.ISteamClientService_ClientSteamServerConnectFailure;
				service.ClientP2PSessionConnectFail += this.ISteamClientService_ClientP2PSessionConnectFail;
				Steamworks.SteamMatchMaking.ELobbyType lobbyType = this.SessionMode.ToSteamMatchMakingLobbyType();
				ulong steamAPICall = this.SteamMatchMakingService.SteamMatchMaking.CreateLobby(lobbyType, 1);
				if (Session.lobbyCreatedCallback != null)
				{
					Session.lobbyCreatedCallback = null;
					if (this.lobbyCreatedCallResult != null)
					{
						this.lobbyCreatedCallResult.Dispose();
						this.lobbyCreatedCallResult = null;
					}
				}
				Session.lobbyCreatedCallback = new Steamworks.LobbyCreatedCallback(this.Steamworks_LobbyCreated);
				this.lobbyCreatedCallResult = new Steamworks.CallResult(Steamworks.SteamAPI_RegisterLobbyCreatedDelegate(steamAPICall, Session.lobbyCreatedCallback));
				break;
			}
			}
		}

		public void Open(Steamworks.SteamID steamIDLobby)
		{
			if (this.opened)
			{
				throw new InvalidOperationException("The session is already opened; consider closing it first or calling 'Reopen' instead.");
			}
			this.SteamMatchMakingService = Services.GetService<ISteamMatchMakingService>();
			if (this.SteamMatchMakingService == null)
			{
				throw new InvalidOperationException("Cannot find the steam match making service.");
			}
			if (!steamIDLobby.IsValid)
			{
				throw new ArgumentException("The steamIDLobby is not valid.");
			}
			this.SteamIDLobby = steamIDLobby;
			this.opened = false;
			this.opening = true;
			this.reopening = true;
			this.hosting = false;
			this.SessionMode = SessionMode.Public;
			this.SteamMatchMakingService.SteamLobbyChatMsg += this.SteamMatchMakingService_SteamLobbyChatMsg;
			this.SteamMatchMakingService.SteamLobbyChatUpdate += this.SteamMatchMakingService_SteamLobbyChatUpdate;
			this.SteamMatchMakingService.SteamLobbyDataUpdate += this.SteamMatchMakingService_SteamLobbyDataUpdate;
			this.SteamMatchMakingService.SteamLobbyEnter += this.SteamMatchMakingService_SteamLobbyEnter;
			this.SteamMatchMakingService.SteamLobbyGameCreated += this.SteamMatchMakingService_SteamLobbyGameCreated;
			this.SteamMatchMakingService.SteamLobbyInvite += this.SteamMatchMakingService_SteamLobbyInvite;
			this.SteamMatchMakingService.SteamLobbyKicked += this.SteamMatchMakingService_SteamLobbyKicked;
			ISteamClientService service = Services.GetService<ISteamClientService>();
			Diagnostics.Assert(service != null);
			service.ClientSteamServersDisconnected += this.ISteamClientService_ClientSteamServersDisconnected;
			service.ClientSteamServerConnectFailure += this.ISteamClientService_ClientSteamServerConnectFailure;
			service.ClientP2PSessionConnectFail += this.ISteamClientService_ClientP2PSessionConnectFail;
			this.OnSessionChange(new SessionChangeEventArgs(SessionChangeAction.Opening, this));
			this.SteamMatchMakingService.SteamMatchMaking.JoinLobby(steamIDLobby);
		}

		public void Reopen(SessionMode sessionMode, int maximumNumberOfLobbyMembers = 16)
		{
			if (!this.opened)
			{
				Diagnostics.LogWarning("The session is not opened, therefore it cannot be reopened. Please consider calling 'Open' instead.");
				return;
			}
			if (!this.hosting)
			{
				Diagnostics.LogWarning("Only the host is supposed to reopen its own session.");
				return;
			}
			Diagnostics.Log("Changing session mode from '{0}' to '{1}'...", new object[]
			{
				this.SessionMode,
				sessionMode
			});
			if (sessionMode != this.SessionMode)
			{
				this.opened = false;
				this.opening = true;
				this.reopening = true;
				this.OnSessionChange(new SessionChangeEventArgs(SessionChangeAction.Reopening, this));
				SessionMode sessionMode2 = this.SessionMode;
				this.SessionMode = sessionMode;
				this.SetLobbyData("SessionMode", sessionMode.ToString(), true);
				this.SetLobbyData("Owner", this.SteamIDUser, true);
				switch (this.SessionMode)
				{
				case SessionMode.Single:
					if (this.SteamMatchMakingService != null)
					{
						this.SteamMatchMakingService.SteamLobbyChatMsg -= this.SteamMatchMakingService_SteamLobbyChatMsg;
						this.SteamMatchMakingService.SteamLobbyChatUpdate -= this.SteamMatchMakingService_SteamLobbyChatUpdate;
						this.SteamMatchMakingService.SteamLobbyDataUpdate -= this.SteamMatchMakingService_SteamLobbyDataUpdate;
						this.SteamMatchMakingService.SteamLobbyEnter -= this.SteamMatchMakingService_SteamLobbyEnter;
						this.SteamMatchMakingService.SteamLobbyGameCreated -= this.SteamMatchMakingService_SteamLobbyGameCreated;
						this.SteamMatchMakingService.SteamLobbyInvite -= this.SteamMatchMakingService_SteamLobbyInvite;
						this.SteamMatchMakingService.SteamLobbyKicked -= this.SteamMatchMakingService_SteamLobbyKicked;
						ISteamClientService service = Services.GetService<ISteamClientService>();
						Diagnostics.Assert(service != null);
						service.ClientSteamServersDisconnected -= this.ISteamClientService_ClientSteamServersDisconnected;
						service.ClientSteamServerConnectFailure -= this.ISteamClientService_ClientSteamServerConnectFailure;
						service.ClientP2PSessionConnectFail -= this.ISteamClientService_ClientP2PSessionConnectFail;
						if (this.SteamIDLobby != null)
						{
							this.SteamMatchMakingService.SteamMatchMaking.LeaveLobby(this.SteamIDLobby);
							this.SteamIDLobby = null;
						}
					}
					this.opened = true;
					this.opening = false;
					this.reopening = false;
					this.OnSessionChange(new SessionChangeEventArgs(SessionChangeAction.Reopened, this));
					break;
				case SessionMode.Private:
				case SessionMode.Protected:
				case SessionMode.Public:
					if (sessionMode2 == SessionMode.Single)
					{
						this.SteamMatchMakingService = Services.GetService<ISteamMatchMakingService>();
						if (this.SteamMatchMakingService == null)
						{
							throw new InvalidOperationException("Cannot find the steam match making service.");
						}
						if (maximumNumberOfLobbyMembers < 1)
						{
							throw new ArgumentOutOfRangeException("The maximum number of lobby members should be greater that zero.");
						}
						this.MaximumNumberOfLobbyMembers = maximumNumberOfLobbyMembers;
						this.SteamMatchMakingService.SteamLobbyChatMsg += this.SteamMatchMakingService_SteamLobbyChatMsg;
						this.SteamMatchMakingService.SteamLobbyChatUpdate += this.SteamMatchMakingService_SteamLobbyChatUpdate;
						this.SteamMatchMakingService.SteamLobbyDataUpdate += this.SteamMatchMakingService_SteamLobbyDataUpdate;
						this.SteamMatchMakingService.SteamLobbyEnter += this.SteamMatchMakingService_SteamLobbyEnter;
						this.SteamMatchMakingService.SteamLobbyGameCreated += this.SteamMatchMakingService_SteamLobbyGameCreated;
						this.SteamMatchMakingService.SteamLobbyInvite += this.SteamMatchMakingService_SteamLobbyInvite;
						this.SteamMatchMakingService.SteamLobbyKicked += this.SteamMatchMakingService_SteamLobbyKicked;
						ISteamClientService service2 = Services.GetService<ISteamClientService>();
						Diagnostics.Assert(service2 != null);
						service2.ClientSteamServersDisconnected += this.ISteamClientService_ClientSteamServersDisconnected;
						service2.ClientSteamServerConnectFailure += this.ISteamClientService_ClientSteamServerConnectFailure;
						service2.ClientP2PSessionConnectFail += this.ISteamClientService_ClientP2PSessionConnectFail;
						Steamworks.SteamMatchMaking.ELobbyType lobbyType = this.SessionMode.ToSteamMatchMakingLobbyType();
						ulong steamAPICall = this.SteamMatchMakingService.SteamMatchMaking.CreateLobby(lobbyType, 1);
						if (Session.lobbyCreatedCallback != null)
						{
							Session.lobbyCreatedCallback = null;
							if (this.lobbyCreatedCallResult != null)
							{
								this.lobbyCreatedCallResult.Dispose();
								this.lobbyCreatedCallResult = null;
							}
						}
						Session.lobbyCreatedCallback = new Steamworks.LobbyCreatedCallback(this.Steamworks_LobbyCreated);
						this.lobbyCreatedCallResult = new Steamworks.CallResult(Steamworks.SteamAPI_RegisterLobbyCreatedDelegate(steamAPICall, Session.lobbyCreatedCallback));
					}
					else
					{
						this.SteamMatchMakingService.SteamMatchMaking.SetLobbyType(this.SteamIDLobby, this.SessionMode.ToSteamMatchMakingLobbyType());
						if (this.MaximumNumberOfLobbyMembers > 1)
						{
							this.SteamMatchMakingService.SteamMatchMaking.SetLobbyMemberLimit(this.SteamIDLobby, this.MaximumNumberOfLobbyMembers);
						}
						this.opened = true;
						this.opening = false;
						this.reopening = false;
						this.OnSessionChange(new SessionChangeEventArgs(SessionChangeAction.Reopened, this));
					}
					break;
				}
			}
		}

		public void SendLobbyChatMessage(string message)
		{
			if (string.IsNullOrEmpty(message))
			{
				return;
			}
			if (this.SteamIDLobby == null)
			{
				if (this.IsOpened)
				{
					SessionMode sessionMode = this.SessionMode;
					if (sessionMode == SessionMode.Single)
					{
						this.OnLobbyChatMessage(new LobbyChatMessageEventArgs(message, this.SteamIDUser));
					}
				}
				return;
			}
			if (this.SteamIDLobby.IsValid)
			{
				this.SteamMatchMakingService.SteamMatchMaking.SendLobbyChatMessage(this.SteamIDLobby, message);
			}
		}

		public void SendLocalChatMessage(string message)
		{
			this.OnLobbyChatMessage(new LobbyChatMessageEventArgs(message, null));
		}

		public virtual void SetLobbyData(StaticString key, object data, bool replicate = true)
		{
			object obj;
			if (this.lobbyData.TryGetValue(key, out obj))
			{
				if (data == null)
				{
					this.lobbyData.Remove(key);
				}
				else
				{
					if (obj != null && data.ToString() == obj.ToString())
					{
						return;
					}
					this.lobbyData[key] = data;
				}
			}
			else
			{
				this.lobbyData.Add(key, data);
			}
			if (this.SteamMatchMakingService != null && this.SteamIDLobby != null)
			{
				if (this.IsHosting && replicate)
				{
					if (data != null)
					{
						this.SteamMatchMakingService.SteamMatchMaking.SetLobbyData(this.SteamIDLobby, key, data.ToString());
						Diagnostics.Log("Lobby data has been set (key: '{0}', data: '{1}').", new object[]
						{
							key,
							data.ToString()
						});
					}
					else
					{
						bool flag = this.SteamMatchMakingService.SteamMatchMaking.DeleteLobbyData(this.SteamIDLobby, key);
						Diagnostics.Log("Lobby data has been cleared (key: '{0}', deleted: {1}).", new object[]
						{
							key,
							flag
						});
					}
				}
				else
				{
					Diagnostics.Log("Lobby data has not been propagated. (key: '{0}', data: '{1}').", new object[]
					{
						key,
						(data == null) ? "<NULL>" : data.ToString()
					});
				}
			}
			this.OnLobbyDataChange(new LobbyDataChangeEventArgs(key, data));
		}

		public void SetLobbyMemberData(StaticString key, object data)
		{
			if (this.SteamIDUser != null && this.SteamIDUser.IsValid && this.SteamMatchMakingService != null && this.SteamIDLobby != null)
			{
				if (data != null)
				{
					this.SteamMatchMakingService.SteamMatchMaking.SetLobbyMemberData(this.SteamIDLobby, key, data.ToString());
					Diagnostics.Log("Lobby member data has been set (key: '{0}', data: '{1}').", new object[]
					{
						key,
						data.ToString()
					});
				}
				else
				{
					this.SteamMatchMakingService.SteamMatchMaking.SetLobbyMemberData(this.SteamIDLobby, key, string.Empty);
					Diagnostics.Log("Lobby member data has been cleared (key: '{0}').", new object[]
					{
						key
					});
				}
			}
		}

		public virtual void Update()
		{
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				Debug.Log(string.Format("Disposing({0})...", disposing.ToString()));
				this.disposing = disposing;
				if (disposing)
				{
					this.Close();
				}
				this.SessionChange = null;
				this.disposed = true;
			}
		}

		protected virtual void OnLobbyChatMessage(LobbyChatMessageEventArgs e)
		{
			if (this.LobbyChatMessage != null)
			{
				this.LobbyChatMessage(this, e);
			}
		}

		protected virtual void OnLobbyChatUpdate(LobbyChatUpdateEventArgs e)
		{
			if (this.LobbyChatUpdate != null)
			{
				this.LobbyChatUpdate(this, e);
			}
		}

		protected virtual void OnLobbyDataChange(LobbyDataChangeEventArgs e)
		{
			if (this.LobbyDataChange != null)
			{
				this.LobbyDataChange(this, e);
			}
		}

		protected virtual void OnLobbyMemberDataChange(LobbyMemberDataChangeEventArgs e)
		{
			if (this.LobbyMemberDataChange != null)
			{
				this.LobbyMemberDataChange(this, e);
			}
		}

		protected virtual void OnLobbyOwnerChange(LobbyOwnerChangeEventArgs e)
		{
			if (this.SessionChange != null)
			{
				this.SessionChange(this, new SessionChangeEventArgs(SessionChangeAction.OwnerChanged, this));
			}
		}

		protected virtual void OnSessionChange(SessionChangeEventArgs e)
		{
			if (this.SessionChange != null)
			{
				this.SessionChange(this, e);
			}
		}

		protected virtual void SteamMatchMakingService_SteamLobbyDataUpdate(object sender, SteamLobbyDataUpdateEventArgs e)
		{
			if (this.SteamIDLobby != null && this.SteamMatchMakingService != null)
			{
				if (e.Message.m_bSuccess == 0)
				{
					return;
				}
				if (e.Message.m_ulSteamIDLobby == this.SteamIDLobby)
				{
					if (e.Message.m_ulSteamIDMember == this.SteamIDLobby)
					{
						Diagnostics.Log("SteamMatchMakingService_SteamLobbyDataUpdate (lobby: {0}).", new object[]
						{
							e.Message.m_ulSteamIDLobby
						});
						if (!this.hosting)
						{
							Steamworks.SteamID lobbyOwner = this.SteamMatchMakingService.SteamMatchMaking.GetLobbyOwner(this.SteamIDLobby);
							if (lobbyOwner == this.SteamIDUser)
							{
								Diagnostics.LogWarning("We have been promoted lobby owner by steam! we are now hosting the game...");
								this.hosting = true;
							}
							if (lobbyOwner != this.steamIDLobbyOwner)
							{
								this.steamIDLobbyOwner = lobbyOwner;
								this.OnLobbyOwnerChange(new LobbyOwnerChangeEventArgs(this.SteamIDLobby, this.steamIDLobbyOwner));
							}
						}
						Steamworks.SteamMatchMaking steamMatchMaking = this.SteamMatchMakingService.SteamMatchMaking;
						int lobbyDataCount = steamMatchMaking.GetLobbyDataCount(this.SteamIDLobby);
						for (int i = 0; i < lobbyDataCount; i++)
						{
							string x;
							string text;
							if (steamMatchMaking.GetLobbyDataByIndex(this.SteamIDLobby, i, out x, out text))
							{
								if (string.IsNullOrEmpty(text))
								{
									text = null;
								}
								this.SetLobbyData(x, text, false);
							}
						}
					}
					else
					{
						Diagnostics.Log("SteamMatchMakingService_SteamLobbyDataUpdate (lobby: {0}).", new object[]
						{
							e.Message.m_ulSteamIDLobby
						});
						Steamworks.SteamID steamID = new Steamworks.SteamID(e.Message.m_ulSteamIDMember);
						if (steamID.IsValid)
						{
							Diagnostics.Log("SteamMatchMakingService_SteamLobbyDataUpdate (member: {0}).", new object[]
							{
								e.Message.m_ulSteamIDMember
							});
							this.OnLobbyMemberDataChange(new LobbyMemberDataChangeEventArgs(steamID));
						}
					}
				}
			}
		}

		private void SteamMatchMakingService_SteamLobbyChatMsg(object sender, SteamLobbyChatMsgEventArgs e)
		{
			if (e.Message.m_ulSteamIDLobby != this.SteamIDLobby)
			{
				return;
			}
			Diagnostics.Log("SteamMatchMakingService_SteamLobbyChatMsg (message id = \"{0}\", from {1})", new object[]
			{
				e.Message.m_iChatID,
				e.Message.m_ulSteamIDUser
			});
			Steamworks.SteamID steamIDUser;
			int lobbyChatEntry = this.SteamMatchMakingService.SteamMatchMaking.GetLobbyChatEntry(this.SteamIDLobby, e.Message.m_iChatID, out steamIDUser, ref this.buffer);
			if (lobbyChatEntry != 0)
			{
				string @string = Encoding.UTF8.GetString(this.buffer, 0, lobbyChatEntry);
				this.OnLobbyChatMessage(new LobbyChatMessageEventArgs(@string, steamIDUser));
			}
		}

		private void SteamMatchMakingService_SteamLobbyChatUpdate(object sender, SteamLobbyChatUpdateEventArgs e)
		{
			this.OnLobbyChatUpdate(new LobbyChatUpdateEventArgs(new Steamworks.SteamID(e.Message.m_ulSteamIDLobby), new Steamworks.SteamID(e.Message.m_ulSteamIDUserChanged), new Steamworks.SteamID(e.Message.m_ulSteamIDMakingChange), (LobbyChatUpdateStateChange)e.Message.m_rgfChatMemberStateChange));
		}

		private void SteamMatchMakingService_SteamLobbyEnter(object sender, SteamLobbyEnterEventArgs e)
		{
			Diagnostics.Log("[Session] Entered the lobby: " + this.SteamMatchMakingService.SteamMatchMaking.GetLobbyData(this.SteamIDLobby, "name"));
			Steamworks.SteamMatchMaking.EChatRoomEnterResponse echatRoomEnterResponse = (Steamworks.SteamMatchMaking.EChatRoomEnterResponse)e.Message.m_EChatRoomEnterResponse;
			if (!Amplitude.Unity.Framework.Application.Preferences.EnableMultiplayer)
			{
				this.OnError(Session.ErrorLevel.Info, "Modding Tools enabled.", (int)e.Message.m_EChatRoomEnterResponse);
				this.Close();
				return;
			}
			string text = this.SteamMatchMakingService.SteamMatchMaking.GetLobbyData(this.SteamIDLobby, "runtimehash");
			if (string.IsNullOrEmpty(text))
			{
				text = "Invalid";
			}
			IRuntimeService service = Services.GetService<IRuntimeService>();
			if (!this.hosting && (service == null || service.Runtime == null || text != service.Runtime.HashKey))
			{
				string text2 = "Invalid";
				if (service != null && service.Runtime != null && !string.IsNullOrEmpty(service.Runtime.HashKey))
				{
					text2 = service.Runtime.HashKey;
				}
				Diagnostics.LogWarning("ELCP: Hash mismatch: Me: {0}; LobbyDescriptor: {1}", new object[]
				{
					text2,
					text
				});
				this.OnError(Session.ErrorLevel.Info, "%JoinGameCheckSumMismatchDescription", (int)e.Message.m_EChatRoomEnterResponse);
				this.Close();
				return;
			}
			if (echatRoomEnterResponse == Steamworks.SteamMatchMaking.EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
			{
				this.OnSessionChange(new SessionChangeEventArgs(SessionChangeAction.Opened, this));
				int lobbyDataCount = this.SteamMatchMakingService.SteamMatchMaking.GetLobbyDataCount(this.SteamIDLobby);
				for (int i = 0; i < lobbyDataCount; i++)
				{
					string empty = string.Empty;
					string empty2 = string.Empty;
					if (this.SteamMatchMakingService.SteamMatchMaking.GetLobbyDataByIndex(this.SteamIDLobby, i, out empty, out empty2))
					{
						this.SetLobbyData(empty, empty2, true);
					}
				}
				this.opening = false;
				this.opened = true;
				if (this.LobbyEnter != null)
				{
					this.LobbyEnter(this, new LobbyEnterEventArgs(new Steamworks.SteamID(e.Message.m_ulSteamIDLobby)));
				}
				return;
			}
			if (echatRoomEnterResponse != Steamworks.SteamMatchMaking.EChatRoomEnterResponse.k_EChatRoomEnterResponseDoesntExist)
			{
				this.OnError(Session.ErrorLevel.Info, "%SessionErrorCannotJoinLobby", (int)e.Message.m_EChatRoomEnterResponse);
				this.Close();
				return;
			}
			this.OnError(Session.ErrorLevel.Info, "%SessionErrorLobbyDoesNotExist", (int)e.Message.m_EChatRoomEnterResponse);
			this.Close();
		}

		private void SteamMatchMakingService_SteamLobbyGameCreated(object sender, SteamLobbyGameCreatedEventArgs e)
		{
			if (this.SteamIDLobby == null)
			{
				Diagnostics.Log("[Session] SteamMatchMakingService_SteamLobbyGameCreated: SteamIDLobby is null.");
				return;
			}
			if (this.SteamIDLobby == e.Message.m_ulSteamIDLobby && e.Message.m_ulSteamIDGameServer != 0UL)
			{
				this.SteamIDServer = new Steamworks.SteamID(e.Message.m_ulSteamIDGameServer);
			}
		}

		private void SteamMatchMakingService_SteamLobbyInvite(object sender, SteamLobbyInviteEventArgs e)
		{
		}

		private void SteamMatchMakingService_SteamLobbyKicked(object sender, SteamLobbyKickedEventArgs e)
		{
			throw new NotImplementedException();
		}

		private void Steamworks_LobbyCreated(ref Steamworks.LobbyCreated message, bool ioFailure)
		{
			Diagnostics.Log("Steamworks_LobbyCreated (message result = '{0}', io failure = {1}).", new object[]
			{
				message.m_eResult.ToString(),
				ioFailure.ToString()
			});
			Steamworks.EResult eResult = message.m_eResult;
			if (eResult != Steamworks.EResult.k_EResultOK)
			{
				if (eResult != Steamworks.EResult.k_EResultTimeout)
				{
					this.Close();
				}
				else
				{
					this.OnError(Session.ErrorLevel.Error, "%SessionLobbyCreationTimeout", (int)message.m_eResult);
					this.Close();
				}
			}
			else
			{
				this.SteamIDLobby = new Steamworks.SteamID(message.m_ulSteamIDLobby);
				if (this.SteamMatchMakingService != null)
				{
					if (this.MaximumNumberOfLobbyMembers > 1)
					{
						this.SteamMatchMakingService.SteamMatchMaking.SetLobbyMemberLimit(this.SteamIDLobby, this.MaximumNumberOfLobbyMembers);
					}
					else if (this.MaximumNumberOfLobbyMembers < 1)
					{
						Diagnostics.LogError("The maximum number of lobby members should greated than zero (at least one for the lobby owner).");
					}
				}
				if (this.SteamMatchMakingService != null)
				{
					foreach (KeyValuePair<StaticString, object> keyValuePair in this.lobbyData)
					{
						if (!StaticString.IsNullOrEmpty(keyValuePair.Key))
						{
							if (keyValuePair.Value != null)
							{
								this.SteamMatchMakingService.SteamMatchMaking.SetLobbyData(this.SteamIDLobby, keyValuePair.Key, keyValuePair.Value.ToString());
							}
						}
					}
				}
				SessionChangeAction action = SessionChangeAction.Opened;
				if (this.reopening)
				{
					action = SessionChangeAction.Reopened;
				}
				this.opened = true;
				this.opening = false;
				this.reopening = false;
				this.OnSessionChange(new SessionChangeEventArgs(action, this));
			}
		}

		private void ISteamClientService_ClientSteamServersDisconnected(object sender, SteamServersDisconnectedEventArgs e)
		{
			this.OnError(Session.ErrorLevel.Warning, "%SessionSteamServersDisconnected", (int)e.Message.m_eResult);
			IGameService service = Services.GetService<IGameService>();
			if (service == null || service.Game == null)
			{
				this.Close();
				return;
			}
			this.ELCPSendServerMessage(4, "%SessionSteamServersDisconnected");
		}

		private void ISteamClientService_ClientSteamServerConnectFailure(object sender, SteamServerConnectFailureEventArgs e)
		{
			this.OnError(Session.ErrorLevel.Error, "%SessionSteamServerConnectFailure", (int)e.Message.m_eResult);
			IGameService service = Services.GetService<IGameService>();
			if (service == null || service.Game == null)
			{
				this.Close();
			}
		}

		private void ISteamClientService_ClientP2PSessionConnectFail(object sender, P2PSessionConnectFailEventArgs e)
		{
			Steamworks.SteamID steamID = new Steamworks.SteamID(e.Message.m_steamIDRemote);
			Diagnostics.LogWarning("[Session] P2PSessionConnectFail with {0}.", new object[]
			{
				steamID
			});
			if (Session.IgnoreP2PSessionConnectFail != steamID)
			{
				if (!this.IsHosting && steamID == this.SteamIDServer)
				{
					this.OnError(Session.ErrorLevel.Error, "%SessionP2PSessionConnectFail", (int)e.Message.m_eP2PSessionError);
					this.Close();
				}
			}
			else
			{
				Diagnostics.LogWarning("[Session] P2PSessionConnectFail ignored for {0}.", new object[]
				{
					Session.IgnoreP2PSessionConnectFail
				});
				Session.IgnoreP2PSessionConnectFail = null;
			}
		}

		protected virtual void ELCPSendServerMessage(int type, string command)
		{
		}

		protected Dictionary<StaticString, object> lobbyData = new Dictionary<StaticString, object>();

		protected bool hosting;

		private static Steamworks.LobbyCreatedCallback lobbyCreatedCallback;

		private bool disposed;

		private bool disposing;

		private bool opened;

		private bool opening;

		private bool reopening;

		private Steamworks.CallResult lobbyCreatedCallResult;

		private Steamworks.SteamID steamIDLobbyOwner;

		private byte[] buffer = new byte[1024];

		private List<Steamworks.SteamID> steamIDMembers = new List<Steamworks.SteamID>();

		private Steamworks.SteamID steamIDMember;

		public enum ErrorLevel
		{
			Error,
			Warning,
			Info
		}
	}
}
