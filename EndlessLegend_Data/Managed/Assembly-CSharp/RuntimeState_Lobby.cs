using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Amplitude;
using Amplitude.Interop;
using Amplitude.Unity.Achievement;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Session;
using Amplitude.Unity.Steam;
using Amplitude.Unity.View;

public class RuntimeState_Lobby : RuntimeState
{
	public RuntimeState_Lobby()
	{
		this.PlayersToCheck = new List<uint>();
	}

	public object[] Parameters { get; private set; }

	private AchievementManager AchievementManager { get; set; }

	private Faction DefaultFaction { get; set; }

	private Faction DefaultRandomFaction { get; set; }

	private GameSaveDescriptor GameSaveDescriptor { get; set; }

	private bool HasGameBeenLaunchedOnce
	{
		get
		{
			return this.Session != null && (this.GameSaveDescriptor != null || this.Session.GetLobbyData<bool>("_Launching", false) || this.Session.GetLobbyData<bool>("_GameInProgress", false) || this.Session.GetLobbyData<int>("_Turn", 0) > 0);
		}
	}

	[Service]
	private IDownloadableContentService DownloadableContentService { get; set; }

	private Steamworks.SteamID SteamIDLobbyToJoin { get; set; }

	private global::Session Session
	{
		get
		{
			return this.session;
		}
		set
		{
			if (this.session != value)
			{
				if (this.session != null)
				{
					this.Session.LobbyChatMessage -= this.Session_LobbyChatMessage;
					this.Session.LobbyChatUpdate -= this.Session_LobbyChatUpdate;
					this.Session.LobbyDataChange -= this.Session_LobbyDataChange;
					this.Session.LobbyEnter -= this.Session_LobbyEnter;
					this.Session.LobbyMemberDataChange -= this.Session_LobbyMemberDataChange;
					this.Session.LobbyOwnerChange -= this.Session_LobbyOwnerChange;
				}
				this.session = value;
				if (this.session != null)
				{
					this.Session.LobbyChatMessage += this.Session_LobbyChatMessage;
					this.Session.LobbyChatUpdate += this.Session_LobbyChatUpdate;
					this.Session.LobbyDataChange += this.Session_LobbyDataChange;
					this.Session.LobbyEnter += this.Session_LobbyEnter;
					this.Session.LobbyMemberDataChange += this.Session_LobbyMemberDataChange;
					this.Session.LobbyOwnerChange += this.Session_LobbyOwnerChange;
				}
			}
		}
	}

	[Service]
	private ISessionService SessionService { get; set; }

	public override void Begin(params object[] parameters)
	{
		base.Begin(parameters);
		this.DownloadableContentService = Services.GetService<IDownloadableContentService>();
		IAchievementService service = Services.GetService<IAchievementService>();
		if (service != null)
		{
			this.AchievementManager = (service as AchievementManager);
		}
		this.Parameters = parameters;
		this.GameSaveDescriptor = null;
		this.SteamIDLobbyToJoin = null;
		if (parameters != null)
		{
			for (int i = 0; i < parameters.Length; i++)
			{
				if (parameters[i] is GameSaveDescriptor)
				{
					this.GameSaveDescriptor = (parameters[i] as GameSaveDescriptor);
				}
				else if (parameters[i] is Steamworks.SteamID)
				{
					this.SteamIDLobbyToJoin = (parameters[i] as Steamworks.SteamID);
				}
			}
		}
		this.DefaultFaction = null;
		this.DefaultRandomFaction = null;
		IDatabase<Faction> database = Databases.GetDatabase<Faction>(false);
		if (database != null)
		{
			this.DefaultFaction = database.FirstOrDefault((Faction iterator) => iterator.IsStandard && !iterator.IsHidden);
			this.DefaultRandomFaction = database.GetValue("FactionRandom");
			if (this.DefaultRandomFaction == null)
			{
				this.DefaultRandomFaction = this.DefaultFaction;
			}
		}
		if (this.DefaultFaction == null)
		{
			throw new RuntimeException("Unable to resolve a default faction.");
		}
		if (this.DefaultRandomFaction == null)
		{
			throw new RuntimeException("Unable to resolve a default random faction.");
		}
		IGameSerializationService service2 = Services.GetService<IGameSerializationService>();
		if (service2 != null)
		{
			service2.GameSaveDescriptor = this.GameSaveDescriptor;
		}
		WorldGeneratorScenarioDefinition.Select(false);
		this.SessionService = Services.GetService<ISessionService>();
		if (this.SessionService != null)
		{
			this.SessionService.SessionChange += this.SessionService_SessionChange;
			this.SessionService.ReleaseSession();
			this.SessionService.CreateSession();
		}
	}

	public override void End(bool abort)
	{
		base.End(abort);
		if (this.Session != null)
		{
			this.Session = null;
		}
		if (this.DownloadableContentService != null)
		{
			this.DownloadableContentService = null;
		}
		if (this.SessionService != null)
		{
			this.SessionService.SessionChange -= this.SessionService_SessionChange;
			this.SessionService.ReleaseSession();
			this.SessionService = null;
		}
		this.PlayersToCheck.Clear();
	}

	protected override void OnGameLobbyJoinRequested(object sender, SteamGameLobbyJoinRequestedEventArgs e)
	{
		base.OnGameLobbyJoinRequested(sender, e);
		MessagePanel.Instance.Show("%ConfirmLobbyJoinRequest", string.Empty, MessagePanelButtons.OkCancel, delegate(object o, MessagePanelResultEventArgs args)
		{
			if (args.Result == MessagePanelResult.Cancel)
			{
				return;
			}
			if (this.Session.GameClient != null)
			{
				this.Session.GameClient.Disconnect(GameDisconnectionReason.ClientLeft, 64);
			}
			this.Session.Close();
			IRuntimeService service = Services.GetService<IRuntimeService>();
			if (service != null)
			{
				service.Runtime.FiniteStateMachine.PostStateChange(typeof(RuntimeState_OutGame), new object[]
				{
					e.Message.m_steamIDLobby
				});
			}
		}, MessagePanelType.INFORMATIVE, new MessagePanelButton[0]);
	}

	private void CheckOptionsConstraints()
	{
		ISessionService service = Services.GetService<ISessionService>();
		if (service != null && service.Session != null)
		{
			IDatabase<OptionDefinition> database = Databases.GetDatabase<OptionDefinition>(false);
			if (database != null)
			{
				List<OptionDefinition> list = (from optionDefinition in database.GetValues()
				where optionDefinition is GameOptionDefinition
				select optionDefinition).ToList<OptionDefinition>();
				for (int i = 0; i < list.Count; i++)
				{
					string text = list[i].Name;
					string lobbyData = service.Session.GetLobbyData<string>(text, null);
					if (string.IsNullOrEmpty(lobbyData))
					{
						string text2 = list[i].DefaultName;
						service.Session.SetLobbyData(text, text2, true);
						Diagnostics.LogWarning("Missing value for option (name: '{0}') has been reset to default (value: '{1}').", new object[]
						{
							text,
							text2
						});
					}
					else
					{
						IDownloadableContentService service2 = Services.GetService<IDownloadableContentService>();
						if (list[i].ItemDefinitions != null)
						{
							bool flag = false;
							for (int j = 0; j < list[i].ItemDefinitions.Length; j++)
							{
								if (list[i].ItemDefinitions[j].Name == lobbyData)
								{
									flag = true;
									OptionDefinition.ItemDefinition itemDefinition = list[i].ItemDefinitions[j];
									if (itemDefinition.OptionDefinitionConstraints != null)
									{
										foreach (OptionDefinitionConstraint optionDefinitionConstraint in from iterator in itemDefinition.OptionDefinitionConstraints
										where iterator.Type == OptionDefinitionConstraintType.Control
										select iterator)
										{
											string text3 = optionDefinitionConstraint.OptionName;
											if (string.IsNullOrEmpty(text3))
											{
												Diagnostics.LogWarning("Invalid null or empty option name for constraint (item: '{0}', index: '{1}') in option (name: '{2}').", new object[]
												{
													itemDefinition.Name,
													Array.IndexOf<OptionDefinitionConstraint>(itemDefinition.OptionDefinitionConstraints, optionDefinitionConstraint),
													list[i].Name
												});
											}
											else
											{
												string lobbyData2 = service.Session.GetLobbyData<string>(text3, null);
												if (!optionDefinitionConstraint.Keys.Select((OptionDefinitionConstraint.Key key) => key.Name).Contains(lobbyData2))
												{
													string text4 = optionDefinitionConstraint.Keys[0].Name;
													service.Session.SetLobbyData(text3, text4, true);
													Diagnostics.LogWarning("Option (name: '{0}') has been control-constrained (new value: '{1}').", new object[]
													{
														text3,
														text4
													});
													for (int k = 0; k < list.Count; k++)
													{
														if (list[k].Name == text3)
														{
															if (k < i)
															{
																Diagnostics.Log("Rollback has been triggered.");
															}
															break;
														}
													}
												}
											}
										}
										foreach (OptionDefinitionConstraint optionDefinitionConstraint2 in from iterator in itemDefinition.OptionDefinitionConstraints
										where iterator.Type == OptionDefinitionConstraintType.Conditional
										select iterator)
										{
											string text5 = optionDefinitionConstraint2.OptionName;
											if (!string.IsNullOrEmpty(text5))
											{
												string lobbyData3 = service.Session.GetLobbyData<string>(text5, null);
												if (!optionDefinitionConstraint2.Keys.Select((OptionDefinitionConstraint.Key key) => key.Name).Contains(lobbyData3))
												{
													bool flag2 = false;
													foreach (OptionDefinition.ItemDefinition itemDefinition2 in list[i].ItemDefinitions)
													{
														if (!(itemDefinition2.Name == lobbyData))
														{
															bool flag3 = false;
															if (itemDefinition2.OptionDefinitionConstraints == null)
															{
																flag3 = true;
															}
															else
															{
																foreach (OptionDefinitionConstraint optionDefinitionConstraint3 in itemDefinition2.OptionDefinitionConstraints.Where((OptionDefinitionConstraint iterator) => iterator.Type == OptionDefinitionConstraintType.Conditional))
																{
																	text5 = optionDefinitionConstraint3.OptionName;
																	if (!string.IsNullOrEmpty(text5))
																	{
																		lobbyData3 = service.Session.GetLobbyData<string>(text5, null);
																		if (optionDefinitionConstraint2.Keys.Select((OptionDefinitionConstraint.Key key) => key.Name).Contains(lobbyData3))
																		{
																			flag3 = true;
																			break;
																		}
																	}
																}
															}
															if (flag3)
															{
																service.Session.SetLobbyData(text, itemDefinition2.Name, true);
																Diagnostics.LogWarning("Option (name: '{0}') has been condition-constrained (new value: '{1}').", new object[]
																{
																	text,
																	itemDefinition2.Name
																});
																flag2 = true;
																break;
															}
														}
													}
													if (flag2)
													{
														break;
													}
												}
											}
										}
										if (service2 != null)
										{
											foreach (OptionDefinitionConstraint optionDefinitionConstraint4 in from element in itemDefinition.OptionDefinitionConstraints
											where element.Type == OptionDefinitionConstraintType.DownloadableContentConditional
											select element)
											{
												if (string.IsNullOrEmpty(optionDefinitionConstraint4.OptionName))
												{
													Diagnostics.LogWarning("Invalid null or empty option name for constraint (item: '{0}', index: '{1}') in option (name: '{2}').", new object[]
													{
														itemDefinition.Name,
														Array.IndexOf<OptionDefinitionConstraint>(itemDefinition.OptionDefinitionConstraints, optionDefinitionConstraint4),
														list[i].Name
													});
												}
												else
												{
													DownloadableContentAccessibility accessibility = service2.GetAccessibility(optionDefinitionConstraint4.OptionName);
													if ((accessibility & DownloadableContentAccessibility.Available) != DownloadableContentAccessibility.Available)
													{
														string text6 = list[i].DefaultName;
														service.Session.SetLobbyData(text, text6, true);
														Diagnostics.LogWarning("Option (name: '{0}') has been dlc-constrained (to default value: '{1}').", new object[]
														{
															text,
															text6
														});
														break;
													}
												}
											}
										}
									}
								}
							}
							if (!flag)
							{
								string text7 = list[i].DefaultName;
								service.Session.SetLobbyData(text, text7, true);
								Diagnostics.LogWarning("Invalid value for option (name: '{0}', value: '{1}') has been reset to default (value: '{2}').", new object[]
								{
									text,
									lobbyData,
									text7
								});
							}
						}
					}
				}
			}
		}
	}

	private void OnDownlodableContentSharingChanged()
	{
		if (this.DownloadableContentService != null)
		{
			uint lobbyData = this.Session.GetLobbyData<uint>("sbs", 0u);
			foreach (DownloadableContent downloadableContent in this.DownloadableContentService)
			{
				uint num = 1u << (int)downloadableContent.Number;
				if ((lobbyData & num) != 0u)
				{
					downloadableContent.Accessibility |= DownloadableContentAccessibility.Shared;
				}
				else
				{
					downloadableContent.Accessibility &= ~DownloadableContentAccessibility.Shared;
				}
			}
		}
	}

	private void RefreshDownlodableContentSharing(DownloadableContentSharing sharing, SessionChangeAction action)
	{
		if (sharing != DownloadableContentSharing.SharedByServer)
		{
			if (sharing == DownloadableContentSharing.SharedByClient)
			{
				if (this.DownloadableContentService != null)
				{
					List<DownloadableContent> list = (from downloadableContent in this.DownloadableContentService
					where downloadableContent.Sharing == DownloadableContentSharing.SharedByClient
					where (downloadableContent.Accessibility & DownloadableContentAccessibility.Available) == DownloadableContentAccessibility.Available
					select downloadableContent).ToList<DownloadableContent>();
					uint num = 0u;
					foreach (DownloadableContent downloadableContent6 in list)
					{
						num |= 1u << (int)downloadableContent6.Number;
					}
					this.Session.SetLobbyMemberData("sbc", num);
					Diagnostics.Log("[DownloadableContent] DownloadableContentSharing.SharedByClient, bitfield = " + num.ToString());
				}
			}
		}
		else if (this.Session != null && this.Session.IsHosting)
		{
			if (this.DownloadableContentService != null)
			{
				foreach (DownloadableContent downloadableContent2 in this.DownloadableContentService)
				{
					downloadableContent2.Accessibility &= ~DownloadableContentAccessibility.Shared;
					downloadableContent2.Accessibility &= ~DownloadableContentAccessibility.Granted;
				}
				uint num2 = 0u;
				object lobbyData = this.Session.GetLobbyData("sbs");
				if (object.Equals(lobbyData, null))
				{
					Diagnostics.LogWarning("[DownloadableContent] DownloadableContentSharing.SharedByServer is null.");
				}
				if (this.GameSaveDescriptor != null)
				{
					uint lobbyData2 = this.GameSaveDescriptor.GameSaveSessionDescriptor.GetLobbyData<uint>("sbs", 0u);
					num2 = lobbyData2;
					Diagnostics.Log("[DownloadableContent] DownloadableContentSharing.SharedByServer, bitfield = {0} (GameSaveDescriptor).", new object[]
					{
						num2.ToString()
					});
				}
				else
				{
					if (action != SessionChangeAction.Created)
					{
						num2 = this.Session.GetLobbyData<uint>("sbs", 0u);
						List<DownloadableContent> list = (from downloadableContent in this.DownloadableContentService
						where downloadableContent.Sharing == DownloadableContentSharing.SharedByClient
						select downloadableContent).ToList<DownloadableContent>();
						foreach (DownloadableContent downloadableContent3 in list)
						{
							num2 &= ~(1u << (int)downloadableContent3.Number);
						}
					}
					else
					{
						List<DownloadableContent> list = (from downloadableContent in this.DownloadableContentService
						where downloadableContent.Sharing == DownloadableContentSharing.SharedByServer
						where (downloadableContent.Accessibility & DownloadableContentAccessibility.Available) == DownloadableContentAccessibility.Available
						select downloadableContent).ToList<DownloadableContent>();
						foreach (DownloadableContent downloadableContent4 in list)
						{
							num2 |= 1u << (int)downloadableContent4.Number;
						}
					}
					Diagnostics.Log("[DownloadableContent] DownloadableContentSharing.SharedByServer, bitfield = {0} (on action '{1}').", new object[]
					{
						num2.ToString(),
						action.ToString()
					});
				}
				switch (this.Session.SessionMode)
				{
				case SessionMode.Single:
				{
					List<DownloadableContent> list = (from downloadableContent in this.DownloadableContentService
					where downloadableContent.Sharing == DownloadableContentSharing.SharedByClient
					where (downloadableContent.Accessibility & DownloadableContentAccessibility.Available) == DownloadableContentAccessibility.Available
					select downloadableContent).ToList<DownloadableContent>();
					uint num3 = 0u;
					foreach (DownloadableContent downloadableContent5 in list)
					{
						num3 |= 1u << (int)downloadableContent5.Number;
					}
					num2 |= num3;
					break;
				}
				case SessionMode.Private:
				case SessionMode.Protected:
				case SessionMode.Public:
					if (this.Session.SteamIDLobby != null && this.Session.SteamIDLobby.IsValid)
					{
						Steamworks.SteamID[] lobbyMembers = this.Session.GetLobbyMembers();
						for (int i = 0; i < lobbyMembers.Length; i++)
						{
							uint lobbyMemberData = this.Session.GetLobbyMemberData<uint>(lobbyMembers[i], "sbc", 0u);
							num2 |= lobbyMemberData;
						}
					}
					break;
				}
				Diagnostics.Log("[DownloadableContent] DownloadableContentSharing.SharedByServer, bitfield = {0} (clients).", new object[]
				{
					num2.ToString()
				});
				this.Session.SetLobbyData("sbs", num2, true);
				switch (action)
				{
				case SessionChangeAction.Opened:
				case SessionChangeAction.Reopened:
					this.OnDownlodableContentSharingChanged();
					goto IL_4FC;
				}
				this.OnDownlodableContentSharingChanged();
			}
			IL_4FC:;
		}
	}

	private void Session_LobbyChatMessage(object sender, LobbyChatMessageEventArgs e)
	{
		if (this.Session == null)
		{
			return;
		}
		if (!this.Session.IsHosting)
		{
			return;
		}
		if (e.Message.StartsWith("q:/"))
		{
			bool flag = false;
			try
			{
				string text = e.Message.Substring(3);
				string[] array = text.Split(Amplitude.String.ExtendedSeparators);
				if (array[0].StartsWith("Color"))
				{
					if (!this.HasGameBeenLaunchedOnce)
					{
						flag = this.TryChangeEmpireColor(array[0], array[1]);
					}
				}
				else if (array[0].StartsWith("Empire"))
				{
					flag = this.TryChangeEmpire(e.SteamIDUser.ToString(), array[0]);
				}
				else if (array[0].StartsWith("Faction"))
				{
					string factionNameOrDescriptor = text.Substring(array[0].Length + 1);
					flag = this.TryChangeFaction(array[0], array[1], factionNameOrDescriptor);
				}
				else if (array[0].StartsWith("Handicap"))
				{
					flag = this.TryChangeHandicap(array[0], array[1]);
				}
				else if (array[0].StartsWith("LockEmpire"))
				{
					string x = array[0];
					bool flag2;
					if (array.Length == 2 && bool.TryParse(array[1], out flag2))
					{
						string data = flag2.ToString();
						this.Session.SetLobbyData(x, data, true);
						this.UpdateSlotCount();
						flag = true;
					}
				}
			}
			catch
			{
				Diagnostics.LogWarning("Failed to treat query message {0}", new object[]
				{
					e.Message
				});
			}
			if (!flag)
			{
				string message = string.Format("r:/{0}/{1}", e.SteamIDUser.ToString(), e.Message);
				this.Session.SendLobbyChatMessage(message);
			}
		}
	}

	private void Session_LobbyChatUpdate(object sender, LobbyChatUpdateEventArgs e)
	{
		string friendPersonaName = Steamworks.SteamAPI.SteamFriends.GetFriendPersonaName(e.SteamIDUserChanged);
		switch (e.ChatMemberStateChange)
		{
		case LobbyChatUpdateStateChange.Entered:
			this.OnPlayerEntered(e.SteamIDUserChanged);
			if (this.Session != null && this.Session.IsHosting)
			{
				this.RefreshDownlodableContentSharing(DownloadableContentSharing.SharedByServer, SessionChangeAction.LobbyChatUpdate);
			}
			break;
		case LobbyChatUpdateStateChange.Left:
			if (this.Session != null && this.Session.IsHosting)
			{
				this.RefreshDownlodableContentSharing(DownloadableContentSharing.SharedByServer, SessionChangeAction.LobbyChatUpdate);
			}
			this.OnPlayerLeft(e.SteamIDUserChanged);
			break;
		case LobbyChatUpdateStateChange.Disconnected:
			Diagnostics.LogWarning("[Lobby] player '{0}' has disconnected from the lobby", new object[]
			{
				friendPersonaName
			});
			if (this.Session != null && this.Session.IsHosting)
			{
				this.RefreshDownlodableContentSharing(DownloadableContentSharing.SharedByServer, SessionChangeAction.LobbyChatUpdate);
			}
			break;
		case LobbyChatUpdateStateChange.Kicked:
			Diagnostics.LogWarning("[Lobby] player '{0}' has been kicked from the lobby", new object[]
			{
				friendPersonaName
			});
			if (this.Session != null && this.Session.IsHosting)
			{
				this.RefreshDownlodableContentSharing(DownloadableContentSharing.SharedByServer, SessionChangeAction.LobbyChatUpdate);
			}
			break;
		}
	}

	private void Session_LobbyDataChange(object sender, LobbyDataChangeEventArgs e)
	{
		if (this.Session == null)
		{
			return;
		}
		if (!this.Session.IsHosting)
		{
			return;
		}
		string text = e.Key;
		if (text != null)
		{
			if (RuntimeState_Lobby.<>f__switch$map1E == null)
			{
				RuntimeState_Lobby.<>f__switch$map1E = new Dictionary<string, int>(2)
				{
					{
						"NumberOfMajorFactions",
						0
					},
					{
						"sbs",
						1
					}
				};
			}
			int num;
			if (RuntimeState_Lobby.<>f__switch$map1E.TryGetValue(text, out num))
			{
				if (num != 0)
				{
					if (num == 1)
					{
						this.OnDownlodableContentSharingChanged();
					}
				}
				else
				{
					this.OnNumberOfMajorFactionsChanged();
				}
			}
		}
	}

	private void Session_LobbyEnter(object sender, LobbyEnterEventArgs e)
	{
		Diagnostics.Log("[RuntimeState_Lobby] Lobby Entered.");
		this.Session.SetLobbyMemberData("Version", Amplitude.Unity.Framework.Application.Version.ToLong());
		this.Session.SetLobbyMemberData("Ready", false);
		IRuntimeService service = Services.GetService<IRuntimeService>();
		Diagnostics.Assert(service != null);
		Diagnostics.Assert(service.Runtime != null);
		if (this.Session.IsHosting && service != null && service.Runtime != null && !string.IsNullOrEmpty(service.Runtime.HashKey) && this.Session.GetLobbyData<string>("runtimehash", "Invalid") != service.Runtime.HashKey)
		{
			this.Session.SetLobbyData("runtimehash", service.Runtime.HashKey, true);
		}
		if (this.AchievementManager != null)
		{
			uint questAchievementsCompletionBitfield = this.AchievementManager.GetQuestAchievementsCompletionBitfield();
			this.Session.SetLobbyMemberData("QuestAchievementsCompletion", questAchievementsCompletionBitfield);
		}
		if (!this.Session.IsHosting)
		{
			this.OnDownlodableContentSharingChanged();
		}
	}

	private void SessionService_SessionChange(object sender, SessionChangeEventArgs e)
	{
		switch (e.Action)
		{
		case SessionChangeAction.Created:
			this.Session = (e.Session as global::Session);
			this.OnSessionCreated();
			break;
		case SessionChangeAction.Releasing:
		case SessionChangeAction.Released:
			this.Session = null;
			break;
		case SessionChangeAction.Opened:
			this.OnSessionOpened();
			break;
		case SessionChangeAction.Reopened:
			this.OnSessionReopened();
			break;
		}
	}

	private void Session_LobbyMemberDataChange(object sender, LobbyMemberDataChangeEventArgs e)
	{
		if (this.Session != null && this.Session.IsHosting)
		{
			this.RefreshDownlodableContentSharing(DownloadableContentSharing.SharedByServer, SessionChangeAction.LobbyMemberDataChange);
			if (!this.PlayersToCheck.Contains(e.SteamIDMember.AccountID))
			{
				this.PlayersToCheck.Add(e.SteamIDMember.AccountID);
				return;
			}
			long lobbyMemberData = this.Session.GetLobbyMemberData<long>(e.SteamIDMember, "Version", 0L);
			if (lobbyMemberData != Amplitude.Unity.Framework.Application.Version.ToLong())
			{
				if (Amplitude.Unity.Framework.Application.Preferences.ELCPDevMode)
				{
					Amplitude.Unity.Framework.Version version = new Amplitude.Unity.Framework.Version(lobbyMemberData);
					string str = string.Format(AgeLocalizer.Instance.LocalizeString("%JoinGameVersionMismatchDescription"), version.ToString(), Amplitude.Unity.Framework.Application.Version.ToString());
					Diagnostics.LogError("ELCP Player Kicked: " + str);
				}
				string message = string.Format("k:/{0}/{1}", e.SteamIDMember, "%KickReasonVersionMismatch");
				this.Session.SendLobbyChatMessage(message);
			}
		}
	}

	private void Session_LobbyOwnerChange(object sender, LobbyOwnerChangeEventArgs e)
	{
		if (this.Session.SteamIDUser != e.SteamIDLobbyOwner)
		{
			return;
		}
		Diagnostics.Log("[Lobby] Owner changed. Begining relocation process.");
		List<Steamworks.SteamID> list = new List<Steamworks.SteamID>(this.Session.GetLobbyMembers());
		int num = 0;
		for (;;)
		{
			string x = string.Format("Empire{0}", num);
			string lobbyData = this.Session.GetLobbyData<string>(x, null);
			if (string.IsNullOrEmpty(lobbyData))
			{
				break;
			}
			if (!lobbyData.StartsWith("AI"))
			{
				string[] array = lobbyData.Split(Amplitude.String.Separators, StringSplitOptions.RemoveEmptyEntries);
				for (int i = 0; i < array.Length; i++)
				{
					ulong steamIDUserUInt64 = Convert.ToUInt64(array[i], 16);
					list.RemoveAll((Steamworks.SteamID id) => id == steamIDUserUInt64);
				}
			}
			num++;
		}
		if (list.Count == 0)
		{
			return;
		}
		for (int j = 0; j < list.Count; j++)
		{
			Diagnostics.Log("[Lobby] Relocating user {0}.", new object[]
			{
				list[j]
			});
			if (this.AttributeSlotToUser(list[j]))
			{
				Diagnostics.Log("[Lobby] User successfully relocated.");
			}
			else
			{
				Diagnostics.LogWarning("[Lobby] User unsuccessfully relocated.");
			}
		}
		if (this.Session != null && this.Session.IsHosting)
		{
			this.RefreshDownlodableContentSharing(DownloadableContentSharing.SharedByServer, SessionChangeAction.OwnerChanged);
		}
	}

	private void OnSessionCreated()
	{
		SessionState_Opened.ThisIsAHackButIAmReady = false;
		if (this.SteamIDLobbyToJoin != null)
		{
			Diagnostics.Log("The session has been created; opening the session in 'Public' mode");
			this.Session.Open(this.SteamIDLobbyToJoin);
		}
		else
		{
			Diagnostics.Log("The session has been created; opening the session in 'Single' mode (default)...");
			if (this.GameSaveDescriptor != null)
			{
				switch (this.GameSaveDescriptor.GameSaveSessionDescriptor.SessionMode)
				{
				case SessionMode.Private:
				case SessionMode.Protected:
				case SessionMode.Public:
				{
					Amplitude.Unity.View.IViewService service = Services.GetService<Amplitude.Unity.View.IViewService>();
					if (service != null)
					{
						service.PostViewChange(typeof(OutGameView), new object[0]);
					}
					break;
				}
				}
				this.Session.Open(this.GameSaveDescriptor.GameSaveSessionDescriptor.SessionMode, 16);
			}
			else
			{
				this.Session.Open(SessionMode.Single, 16);
			}
		}
		if (this.Session.IsHosting)
		{
			if (Steamworks.SteamAPI.IsSteamRunning)
			{
				this.Session.SetLobbyData("name", Steamworks.SteamAPI.SteamFriends.GetFriendPersonaName(this.session.SteamIDUser) + "'s game", true);
			}
			else
			{
				this.Session.SetLobbyData("name", Environment.UserName + "'s game", true);
			}
			IRuntimeService service2 = Services.GetService<IRuntimeService>();
			Diagnostics.Assert(service2 != null);
			Diagnostics.Assert(service2.Runtime != null);
			if (service2 != null && service2.Runtime != null && !string.IsNullOrEmpty(service2.Runtime.HashKey))
			{
				this.Session.SetLobbyData("runtimehash", service2.Runtime.HashKey, true);
			}
			if (service2 != null && service2.Runtime != null)
			{
				Diagnostics.Assert(service2.Runtime.RuntimeModules != null);
				string[] array;
				if (service2.Runtime.RuntimeModules.Count == 1)
				{
					RuntimeModule runtimeModule2 = service2.Runtime.RuntimeModules[0];
					Diagnostics.Assert(runtimeModule2.Type == RuntimeModuleType.Standalone);
					if (service2 != null && service2.VanillaModuleName != runtimeModule2.Name)
					{
						array = new string[]
						{
							runtimeModule2.Name
						};
					}
					else
					{
						array = new string[]
						{
							runtimeModule2.Name
						};
					}
				}
				else
				{
					array = (from runtimeModule in service2.Runtime.RuntimeModules
					select runtimeModule.Name).ToArray<string>();
				}
				if (array != null)
				{
					this.Session.SetLobbyData("runtimeconfiguration", string.Join(";", array), true);
				}
			}
			this.DownloadableContentService.RemoveAccessibility(DownloadableContent8.ReadOnlyName, DownloadableContentAccessibility.Activated);
			IAchievementService service3 = Services.GetService<IAchievementService>();
			if (service3 != null)
			{
				bool achievement = service3.GetAchievement(DownloadableContent8.ReadOnlyRelatedAchievementName);
				if (achievement)
				{
					this.DownloadableContentService.AddAccessibility(DownloadableContent8.ReadOnlyName, DownloadableContentAccessibility.Activated);
				}
			}
		}
		if (this.Session.IsHosting)
		{
			this.RefreshDownlodableContentSharing(DownloadableContentSharing.SharedByServer, SessionChangeAction.Created);
		}
	}

	private void OnSessionOpened()
	{
		Diagnostics.Log("The session has been opened.");
		this.RefreshDownlodableContentSharing(DownloadableContentSharing.SharedByClient, SessionChangeAction.Opened);
		if (this.Session.IsHosting)
		{
			this.RefreshDownlodableContentSharing(DownloadableContentSharing.SharedByServer, SessionChangeAction.Opened);
			this.Session.SetLobbyData("_Turn", (this.GameSaveDescriptor == null) ? 0 : this.GameSaveDescriptor.Turn, true);
			if (this.GameSaveDescriptor != null)
			{
				foreach (string text in this.GameSaveDescriptor.GameSaveSessionDescriptor.GetLobbyDataKeys())
				{
					if (text != "name" && text.ToLower() != "owner")
					{
						string lobbyData = this.GameSaveDescriptor.GameSaveSessionDescriptor.GetLobbyData<string>(text, null);
						this.Session.SetLobbyData(text, lobbyData, true);
					}
				}
				this.Session.SetLobbyData("Owner", this.Session.SteamIDUser.UInt64AccountID, true);
				this.Session.SetLobbyData("owner", this.Session.SteamIDUser.UInt64AccountID, true);
				this.Session.SetLobbyData("Version", Amplitude.Unity.Framework.Application.Version.ToLong(), true);
				this.Session.SetLobbyData("version", Amplitude.Unity.Framework.Application.Version.ToLong(), true);
				this.Session.SetLobbyData("_IsSavedGame", true, true);
				if (this.GameSaveDescriptor.GameSaveSessionDescriptor.SessionMode == SessionMode.Single)
				{
					this.Session.LocalPlayerReady = true;
					string text2 = "Empire0";
					string text3 = this.Session.GetLobbyData<string>(text2, null);
					if (text3 != this.Session.SteamIDUser.ToString())
					{
						string text4 = string.Empty;
						int num = 0;
						string lobbyData2;
						for (;;)
						{
							text4 = string.Format("Empire{0}", num);
							lobbyData2 = this.Session.GetLobbyData<string>(text4, null);
							if (string.IsNullOrEmpty(lobbyData2))
							{
								goto IL_25B;
							}
							if (!lobbyData2.StartsWith("AI"))
							{
								break;
							}
							num++;
						}
						text2 = text4;
						text3 = lobbyData2;
						IL_25B:
						Diagnostics.LogWarning("Replacing steam id user {0} from '{1}' by current user's {2} in order to load the single player game.", new object[]
						{
							text3,
							text2,
							this.Session.SteamIDUser.ToString()
						});
						this.Session.SetLobbyData(text2, this.Session.SteamIDUser.ToString(), true);
					}
					return;
				}
				bool flag = false;
				int lobbyData3 = this.Session.GetLobbyData<int>("NumberOfMajorFactions", 0);
				for (int i = 0; i < lobbyData3; i++)
				{
					string x = string.Format("Empire{0}", i);
					string x2 = string.Format("_EmpireReserved{0}", i);
					string text5 = this.Session.GetLobbyData<string>(x, null);
					string data = "0";
					if (text5 == this.Session.SteamIDUser.ToString())
					{
						data = text5;
						flag = true;
					}
					else if (!text5.StartsWith("AI"))
					{
						data = text5;
						text5 = "AI0";
					}
					this.Session.SetLobbyData(x, text5, true);
					this.Session.SetLobbyData(x2, data, true);
				}
				if (!flag)
				{
					this.AttributeSlotToUser(this.Session.SteamIDUser);
				}
			}
			else
			{
				IDatabase<OptionDefinition> database = Databases.GetDatabase<OptionDefinition>(false);
				if (database != null)
				{
					foreach (OptionDefinition optionDefinition in database)
					{
						if (!(optionDefinition is WorldGeneratorOptionDefinition))
						{
							Diagnostics.Assert(optionDefinition != null);
							string selection = Amplitude.Unity.Framework.Application.Registry.GetValue<string>(optionDefinition.RegistryPath, optionDefinition.DefaultName.ToString());
							if (optionDefinition.ItemDefinitions == null || !Array.Exists<OptionDefinition.ItemDefinition>(optionDefinition.ItemDefinitions, (OptionDefinition.ItemDefinition match) => match.Name == selection))
							{
								Diagnostics.Log("Can't retrieve the option value {0}, fallback to default value {1} in registry.", new object[]
								{
									selection,
									optionDefinition.DefaultName
								});
								selection = optionDefinition.DefaultName;
								Amplitude.Unity.Framework.Application.Registry.SetValue(optionDefinition.RegistryPath, selection);
							}
							this.Session.SetLobbyData(optionDefinition.Name, selection, true);
						}
					}
				}
				IDatabase<WorldGeneratorOptionDefinition> database2 = Databases.GetDatabase<WorldGeneratorOptionDefinition>(false);
				if (database2 != null)
				{
					foreach (WorldGeneratorOptionDefinition worldGeneratorOptionDefinition in database2)
					{
						Diagnostics.Assert(worldGeneratorOptionDefinition != null);
						string selection = Amplitude.Unity.Framework.Application.Registry.GetValue<string>(worldGeneratorOptionDefinition.RegistryPath, worldGeneratorOptionDefinition.DefaultName.ToString());
						if (worldGeneratorOptionDefinition.ItemDefinitions == null || !Array.Exists<OptionDefinition.ItemDefinition>(worldGeneratorOptionDefinition.ItemDefinitions, (OptionDefinition.ItemDefinition match) => match.Name == selection))
						{
							Diagnostics.Log("Can't retrieve the option value {0}, fallback to default value {1} in registry.", new object[]
							{
								selection,
								worldGeneratorOptionDefinition.DefaultName
							});
							selection = worldGeneratorOptionDefinition.DefaultName;
							Amplitude.Unity.Framework.Application.Registry.SetValue(worldGeneratorOptionDefinition.RegistryPath, selection);
						}
						this.Session.SetLobbyData(worldGeneratorOptionDefinition.Name, selection, true);
					}
				}
				this.CheckOptionsConstraints();
				WorldGenerator.CheckWorldGeneratorOptionsConstraints();
				if (this.GameSaveDescriptor == null)
				{
					int lobbyData4 = this.Session.GetLobbyData<int>("NumberOfMajorFactions", 0);
					Diagnostics.Assert(lobbyData4 > 0);
					Diagnostics.Assert(lobbyData4 <= 32);
					for (int j = 0; j < lobbyData4; j++)
					{
						string x3 = string.Format("Empire{0}", j);
						string x4 = string.Format("Faction{0}", j);
						string x5 = string.Format("Color{0}", j);
						if (j == 0)
						{
							this.Session.SetLobbyData(x3, this.Session.SteamIDUser.ToString(), true);
							string text6 = Amplitude.Unity.Framework.Application.Registry.GetValue<string>("Preferences/Lobby/Faction", "FactionVaulters");
							if (this.DownloadableContentService != null)
							{
								DownloadableContentRestrictionCategory category = (this.Session.SessionMode != SessionMode.Single) ? DownloadableContentRestrictionCategory.LobbyFaction : DownloadableContentRestrictionCategory.Faction;
								bool flag2;
								string text7;
								if (!this.DownloadableContentService.TryCheckAgainstRestrictions(category, text6, out flag2, out text7) || !flag2)
								{
									if (!string.IsNullOrEmpty(text7))
									{
										text6 = text7;
									}
									else
									{
										text6 = this.DefaultFaction.Name;
									}
								}
							}
							IDatabase<Faction> database3 = Databases.GetDatabase<Faction>(true);
							Faction faction = null;
							if (!database3.TryGetValue(text6, out faction))
							{
								faction = this.DefaultFaction;
							}
							if (this.DownloadableContentService != null)
							{
								foreach (FactionTrait factionTrait in Faction.EnumerableTraits(faction))
								{
									bool flag3;
									if (!this.DownloadableContentService.TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory.FactionTrait, factionTrait.Name, out flag3) || !flag3)
									{
										faction = this.DefaultFaction;
										break;
									}
								}
							}
							bool flag4;
							string text8;
							if (this.DownloadableContentService != null && faction != null && faction.Affinity != null && this.DownloadableContentService.TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory.FactionAffinity, faction.Affinity, out flag4, out text8) && !flag4)
							{
								if (!string.IsNullOrEmpty(text8))
								{
									if (!database3.TryGetValue(text8, out faction))
									{
										faction = this.DefaultFaction;
									}
								}
								else
								{
									faction = this.DefaultFaction;
								}
							}
							string data2 = Faction.Encode(faction);
							this.Session.SetLobbyData(x4, data2, true);
						}
						else
						{
							this.Session.SetLobbyData(x3, "AI0", true);
							string data3 = Faction.Encode(this.DefaultRandomFaction);
							this.Session.SetLobbyData(x4, data3, true);
						}
						this.Session.SetLobbyData(x5, this.GetFirstAvailableColor(j), true);
						if (j == 0)
						{
							string value = Amplitude.Unity.Framework.Application.Registry.GetValue<string>("Preferences/Lobby/Color", "0");
							this.TryChangeEmpireColor("Color0", value);
						}
					}
				}
			}
		}
		this.UpdateSlotCount();
		IGuiService service = Services.GetService<IGuiService>();
		if (service != null)
		{
			MenuNewGameScreen guiPanel = service.GetGuiPanel<MenuNewGameScreen>();
			guiPanel.ShowWhenFinishedHiding(new object[0]);
			MenuMainScreen guiPanel2 = service.GetGuiPanel<MenuMainScreen>();
			guiPanel.SetBreadcrumb(guiPanel2.MenuBreadcrumb.Text);
		}
	}

	private void OnSessionReopened()
	{
		Diagnostics.Log("The session has been reopened.");
		switch (this.Session.SessionMode)
		{
		case SessionMode.Single:
		{
			this.Session.SetLobbyData("Empire0", this.Session.SteamIDUser.ToString(), true);
			int num = 1;
			for (;;)
			{
				string lobbyData = this.session.GetLobbyData<string>("Empire" + num, null);
				if (string.IsNullOrEmpty(lobbyData))
				{
					break;
				}
				if (!lobbyData.StartsWith("AI"))
				{
					this.Session.SetLobbyData("Empire" + num, "AI0", true);
				}
				num++;
			}
			break;
		}
		}
		this.RefreshDownlodableContentSharing(DownloadableContentSharing.SharedByClient, SessionChangeAction.Reopened);
		if (this.Session.IsHosting)
		{
			this.RefreshDownlodableContentSharing(DownloadableContentSharing.SharedByServer, SessionChangeAction.Reopened);
		}
	}

	private void OnNumberOfMajorFactionsChanged()
	{
		List<ulong> list = new List<ulong>();
		int lobbyData = this.Session.GetLobbyData<int>("NumberOfMajorFactions", 0);
		Diagnostics.Assert(lobbyData > 0);
		Diagnostics.Assert(lobbyData <= 32);
		int num = lobbyData;
		for (;;)
		{
			string x = string.Format("Empire{0}", num);
			object lobbyData2 = this.Session.GetLobbyData(x);
			if (lobbyData2 == null)
			{
				break;
			}
			string lobbyData3 = this.Session.GetLobbyData<string>(x, null);
			if (!string.IsNullOrEmpty(lobbyData3) && !lobbyData3.StartsWith("AI"))
			{
				string[] array = lobbyData3.Split(Amplitude.String.Separators, StringSplitOptions.RemoveEmptyEntries);
				foreach (string value in array)
				{
					ulong item = Convert.ToUInt64(value, 16);
					list.Add(item);
				}
			}
			string x2 = string.Format("Faction{0}", num);
			string x3 = string.Format("Color{0}", num);
			this.Session.SetLobbyData(x, null, true);
			this.Session.SetLobbyData(x2, null, true);
			this.Session.SetLobbyData(x3, null, true);
			num++;
		}
		for (int j = 1; j < lobbyData; j++)
		{
			string x4 = string.Format("Empire{0}", j);
			object lobbyData4 = this.Session.GetLobbyData(x4);
			if (lobbyData4 == null)
			{
				string x5 = string.Format("Faction{0}", j);
				string x6 = string.Format("Color{0}", j);
				this.Session.SetLobbyData(x4, "AI0", true);
				Faction defaultRandomFaction = this.DefaultRandomFaction;
				string data = Faction.Encode(defaultRandomFaction);
				this.Session.SetLobbyData(x5, data, true);
				this.Session.SetLobbyData(x6, this.GetFirstAvailableColor(j), true);
			}
		}
		foreach (ulong value2 in list)
		{
			Steamworks.SteamID steamID = new Steamworks.SteamID(value2);
			if (this.AttributeSlotToUser(steamID))
			{
				Diagnostics.Log("[Lobby] User {0} relocated.", new object[]
				{
					steamID
				});
			}
			else
			{
				Diagnostics.LogWarning("[Lobby] Couldn't relocate user {0}. Kicking her.", new object[]
				{
					steamID
				});
				string message = string.Format("k:/{0}/{1}", steamID, "%KickReasonCouldNotRelocate");
				this.Session.SendLobbyChatMessage(message);
			}
		}
		this.UpdateSlotCount();
	}

	private void OnPlayerEntered(Steamworks.SteamID steamID)
	{
		string friendPersonaName = Steamworks.SteamAPI.SteamFriends.GetFriendPersonaName(steamID);
		Diagnostics.Log("[Lobby] player '{0}' has joined the lobby", new object[]
		{
			friendPersonaName
		});
		if (!this.IsUserAllowedToJoin(steamID))
		{
			string message = string.Format("k:/{0}/{1}{2}", steamID, "%KickReasonNotAllowed", this.Session.SessionMode);
			this.Session.SendLobbyChatMessage(message);
		}
		if (this.Session.IsHosting)
		{
			int num = 0;
			for (;;)
			{
				string lobbyData = this.Session.GetLobbyData<string>(string.Format("Empire{0}", num), null);
				if (string.IsNullOrEmpty(lobbyData))
				{
					break;
				}
				if (string.CompareOrdinal(lobbyData, steamID.ToString()) == 0)
				{
					goto IL_FF;
				}
				num++;
			}
			if (this.AttributeSlotToUser(steamID))
			{
				this.UpdateSlotCount();
				Diagnostics.Log("[Lobby] Player '{0}' has been affected to a slot.", new object[]
				{
					friendPersonaName
				});
				return;
			}
			Diagnostics.LogWarning("[Lobby] Cannot find an free slot for player '{0}'.", new object[]
			{
				friendPersonaName
			});
			string message2 = string.Format("k:/{0}/{1}", steamID, "%KickReasonLobbyFull");
			this.Session.SendLobbyChatMessage(message2);
			return;
			IL_FF:
			Diagnostics.LogWarning("[Lobby] Ignoring update; player '{0}' has already entered the lobby.", new object[]
			{
				friendPersonaName
			});
			return;
		}
	}

	private void OnPlayerLeft(Steamworks.SteamID steamID)
	{
		if (this.PlayersToCheck.Contains(steamID.AccountID))
		{
			this.PlayersToCheck.Remove(steamID.AccountID);
		}
		string friendPersonaName = Steamworks.SteamAPI.SteamFriends.GetFriendPersonaName(steamID);
		IGameService service = Services.GetService<IGameService>();
		if (service != null && service.Game != null)
		{
			IPlayerRepositoryService service2 = service.Game.Services.GetService<IPlayerRepositoryService>();
			if (service2 != null)
			{
				using (IEnumerator<Player> enumerator = service2.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (enumerator.Current.SteamID == steamID)
						{
							Diagnostics.LogError("[Lobby] ELCP: Received invalid leave message for Player '{0}'", new object[]
							{
								friendPersonaName
							});
							return;
						}
					}
				}
			}
		}
		Diagnostics.Log("[Lobby] Player '{0}' has left the lobby.", new object[]
		{
			friendPersonaName
		});
		if (this.Session.IsHosting)
		{
			bool flag = false;
			int num = 0;
			string text;
			string text2;
			for (;;)
			{
				text = string.Format("Empire{0}", num);
				text2 = this.Session.GetLobbyData<string>(text, null);
				if (string.IsNullOrEmpty(text2))
				{
					goto IL_2AB;
				}
				if (text2.Contains(steamID.ToString()))
				{
					break;
				}
				num++;
			}
			List<string> list = new List<string>(text2.Split(Amplitude.String.Separators, StringSplitOptions.RemoveEmptyEntries));
			list.Remove(steamID.ToString());
			if (list.Count == 0)
			{
				text2 = "AI0";
				string x = string.Format("Faction{0}", num);
				string text3 = this.Session.GetLobbyData<string>(x, null);
				Faction faction = Faction.Decode(text3);
				if (this.DownloadableContentService != null)
				{
					Faction faction2 = null;
					string empty = string.Empty;
					bool flag2;
					if (!this.DownloadableContentService.TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory.Faction, faction.Name, out flag2, out empty) || !flag2)
					{
						if (!string.IsNullOrEmpty(empty))
						{
							if (!Databases.GetDatabase<Faction>(true).TryGetValue(empty, out faction2))
							{
								faction2 = this.DefaultFaction;
							}
						}
						else
						{
							faction2 = this.DefaultFaction;
						}
					}
					else if (!this.DownloadableContentService.TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory.FactionAffinity, faction.Affinity, out flag2) || !flag2)
					{
						faction2 = this.DefaultFaction;
					}
					else
					{
						foreach (FactionTrait factionTrait in Faction.EnumerableTraits(faction))
						{
							if (!this.DownloadableContentService.TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory.FactionTrait, factionTrait.Name, out flag2) || !flag2)
							{
								faction2 = this.DefaultFaction;
								break;
							}
						}
					}
					if (faction2 != null)
					{
						text3 = Faction.Encode(faction2);
						this.Session.SetLobbyData(x, text3, true);
					}
				}
			}
			else
			{
				text2 = string.Join(";", list.ToArray());
			}
			flag = true;
			IL_2AB:
			if (flag)
			{
				this.Session.SetLobbyData(text, text2, true);
				Diagnostics.Log("[Lobby] Player '{0}' has been removed from slot '{1}'.", new object[]
				{
					friendPersonaName,
					text
				});
				this.UpdateSlotCount();
				return;
			}
			Diagnostics.LogWarning("[Lobby] Player '{0}' wasn't affected to any empire.", new object[]
			{
				friendPersonaName
			});
		}
	}

	private bool TryChangeEmpireColor(string colorKey, string askedColorValue)
	{
		int num = 0;
		for (;;)
		{
			string lobbyData = this.Session.GetLobbyData<string>("Color" + num, null);
			if (string.IsNullOrEmpty(lobbyData))
			{
				break;
			}
			if (lobbyData == askedColorValue)
			{
				goto Block_2;
			}
			num++;
		}
		goto IL_BC;
		Block_2:
		string lobbyData2 = this.Session.GetLobbyData<string>("Empire" + num, null);
		if (!lobbyData2.StartsWith("AI"))
		{
			return false;
		}
		string lobbyData3 = this.Session.GetLobbyData<string>(colorKey, null);
		this.Session.SetLobbyData("Color" + num, lobbyData3, true);
		IL_BC:
		this.Session.SetLobbyData(colorKey, askedColorValue, true);
		return true;
	}

	private bool TryChangeEmpire(string steamIDstring, string askedEmpireKey)
	{
		string text = this.Session.GetLobbyData<string>(askedEmpireKey, null);
		string x = string.Format("Lock{0}", askedEmpireKey);
		bool lobbyData = this.Session.GetLobbyData<bool>(x, false);
		string x2 = string.Format("{0}Eliminated", askedEmpireKey);
		bool flag = this.Session.GetLobbyData<bool>(x2, false);
		if (this.Session.GetLobbyData<bool>("SpectatorMode", false))
		{
			flag = false;
		}
		bool flag2 = false;
		if (text.StartsWith("AI"))
		{
			flag2 = true;
			text = steamIDstring;
		}
		else
		{
			string[] array = text.Split(Amplitude.String.Separators, StringSplitOptions.RemoveEmptyEntries);
			if (array.Length < 1)
			{
				if (string.CompareOrdinal(array[0], steamIDstring) == 0)
				{
					Diagnostics.LogWarning("[Lobby] User already affected to this slot.");
					flag2 = false;
				}
				else
				{
					flag2 = true;
					text = text + ";" + steamIDstring;
				}
			}
		}
		if (flag2 && !lobbyData && !flag)
		{
			int num = 0;
			string x3;
			string text2;
			for (;;)
			{
				x3 = string.Format("Empire{0}", num);
				text2 = this.Session.GetLobbyData<string>(x3, null);
				if (text2.Contains(steamIDstring))
				{
					break;
				}
				if (string.IsNullOrEmpty(text))
				{
					Diagnostics.LogError("Failed to locate steam user's empire ({0}).", new object[]
					{
						steamIDstring
					});
				}
				num++;
			}
			string[] array2 = text2.Split(Amplitude.String.Separators, StringSplitOptions.RemoveEmptyEntries);
			if (array2.Length == 1)
			{
				text2 = "AI0";
			}
			else
			{
				List<string> list = new List<string>(array2);
				list.Remove(steamIDstring);
				text2 = string.Join(";", list.ToArray());
			}
			this.Session.SetLobbyData(askedEmpireKey, text, true);
			this.Session.SetLobbyData(x3, text2, true);
			return true;
		}
		return false;
	}

	private bool TryChangeFaction(string factionKey, string factionName, string factionNameOrDescriptor)
	{
		IDatabase<Faction> database = Databases.GetDatabase<Faction>(true);
		Faction faction = null;
		if (database.TryGetValue(factionName, out faction))
		{
			Faction faction2 = faction;
			string data = Faction.Encode(faction2);
			this.Session.SetLobbyData(factionKey, data, true);
			return true;
		}
		try
		{
			faction = Faction.Decode(factionNameOrDescriptor);
			int num = 0;
			if (faction.IsCustom && Faction.IsValidCustomFaction(faction, out num) && num >= 0)
			{
				string data2 = Faction.Encode(faction);
				this.Session.SetLobbyData(factionKey, data2, true);
				return true;
			}
		}
		catch
		{
		}
		return false;
	}

	private string GetFirstAvailableColor(int slotIndex)
	{
		List<string> list = new List<string>();
		int num = 0;
		for (;;)
		{
			if (num != slotIndex)
			{
				string lobbyData = this.Session.GetLobbyData<string>("Color" + num, null);
				if (string.IsNullOrEmpty(lobbyData))
				{
					break;
				}
				list.Add(lobbyData);
			}
			num++;
		}
		int num2 = num - 1;
		for (int i = 0; i < num2 + 1; i++)
		{
			string text = i.ToString();
			if (!list.Contains(text))
			{
				return text;
			}
		}
		return (num2 + 1).ToString();
	}

	private bool AttributeSlotToUser(Steamworks.SteamID steamIDUserChanged)
	{
		bool lobbyData = this.Session.GetLobbyData<bool>("SpectatorMode", false);
		bool flag = false;
		string x = string.Empty;
		string text = string.Empty;
		int num = 0;
		string[] array;
		for (;;)
		{
			x = string.Format("Empire{0}", num);
			text = this.Session.GetLobbyData<string>(x, null);
			if (string.IsNullOrEmpty(text))
			{
				goto IL_12A;
			}
			string x2 = string.Format("_EmpireReserved{0}", num);
			string lobbyData2 = this.Session.GetLobbyData<string>(x2, null);
			if (!string.IsNullOrEmpty(lobbyData2) && !(lobbyData2 == "0") && lobbyData2.Contains(steamIDUserChanged.ToString()))
			{
				string x3 = string.Format("Empire{0}Eliminated", num);
				if (this.Session.GetLobbyData<bool>(x3, false) && !lobbyData)
				{
					goto IL_12A;
				}
				if (text.StartsWith("AI"))
				{
					break;
				}
				array = text.Split(Amplitude.String.Separators, StringSplitOptions.RemoveEmptyEntries);
				if (array.Length < 1)
				{
					goto IL_109;
				}
			}
			num++;
		}
		text = steamIDUserChanged.ToString();
		flag = true;
		goto IL_12A;
		IL_109:
		text = string.Join(";", array);
		text += string.Format(";{0}", steamIDUserChanged);
		flag = true;
		IL_12A:
		if (!flag)
		{
			int num2 = 0;
			for (;;)
			{
				x = string.Format("Empire{0}", num2);
				text = this.Session.GetLobbyData<string>(x, null);
				if (string.IsNullOrEmpty(text))
				{
					goto IL_218;
				}
				string x4 = string.Format("LockEmpire{0}", num2);
				if (!this.Session.GetLobbyData<bool>(x4, false))
				{
					string x5 = string.Format("Empire{0}Eliminated", num2);
					if (!this.Session.GetLobbyData<bool>(x5, false) || lobbyData)
					{
						string x6 = string.Format("_EmpireReserved{0}", num2);
						string lobbyData3 = this.Session.GetLobbyData<string>(x6, null);
						if ((string.IsNullOrEmpty(lobbyData3) || lobbyData3 == "0") && text.StartsWith("AI"))
						{
							break;
						}
					}
				}
				num2++;
			}
			text = steamIDUserChanged.ToString();
			flag = true;
		}
		IL_218:
		if (!flag)
		{
			int num3 = 0;
			for (;;)
			{
				x = string.Format("Empire{0}", num3);
				text = this.Session.GetLobbyData<string>(x, null);
				if (string.IsNullOrEmpty(text))
				{
					goto IL_2C4;
				}
				string x7 = string.Format("LockEmpire{0}", num3);
				if (!this.Session.GetLobbyData<bool>(x7, false))
				{
					string x8 = string.Format("Empire{0}Eliminated", num3);
					if ((!this.Session.GetLobbyData<bool>(x8, false) || lobbyData) && text.StartsWith("AI"))
					{
						break;
					}
				}
				num3++;
			}
			text = steamIDUserChanged.ToString();
			flag = true;
		}
		IL_2C4:
		if (!flag)
		{
			int num4 = 0;
			string[] array2;
			for (;;)
			{
				x = string.Format("Empire{0}", num4);
				text = this.Session.GetLobbyData<string>(x, null);
				if (string.IsNullOrEmpty(text))
				{
					goto IL_393;
				}
				string x9 = string.Format("LockEmpire{0}", num4);
				if (!this.Session.GetLobbyData<bool>(x9, false))
				{
					string x10 = string.Format("Empire{0}Eliminated", num4);
					if (!this.Session.GetLobbyData<bool>(x10, false) || lobbyData)
					{
						array2 = text.Split(Amplitude.String.Separators, StringSplitOptions.RemoveEmptyEntries);
						if (array2.Length < 1)
						{
							break;
						}
					}
				}
				num4++;
			}
			text = string.Join(";", array2);
			text += string.Format(";{0}", steamIDUserChanged);
			flag = true;
		}
		IL_393:
		if (flag)
		{
			this.Session.SetLobbyData(x, text, true);
		}
		return flag;
	}

	private void UpdateSlotCount()
	{
		RuntimeState_Lobby.SlotCount slotCount = default(RuntimeState_Lobby.SlotCount);
		int num = 0;
		for (;;)
		{
			string x = string.Format("Empire{0}", num);
			string lobbyData = this.Session.GetLobbyData<string>(x, null);
			if (string.IsNullOrEmpty(lobbyData))
			{
				break;
			}
			slotCount.Total++;
			string x2 = string.Format("LockEmpire{0}", num);
			bool lobbyData2 = this.Session.GetLobbyData<bool>(x2, false);
			string x3 = string.Format("Empire{0}Eliminated", num);
			bool lobbyData3 = this.Session.GetLobbyData<bool>(x3, false);
			if (lobbyData.StartsWith("AI"))
			{
				if (!lobbyData2 && !lobbyData3)
				{
					slotCount.Free++;
				}
			}
			else
			{
				string[] array = lobbyData.Split(Amplitude.String.Separators, StringSplitOptions.RemoveEmptyEntries);
				slotCount.Occupied += array.Length;
				if (!lobbyData2 && !lobbyData3)
				{
					slotCount.Free += 1 - array.Length;
				}
			}
			num++;
		}
		this.Session.SetLobbyData("OccupiedSlots", slotCount.Occupied, true);
		this.Session.SetLobbyData("FreeSlots", slotCount.Free, true);
		Diagnostics.Log(slotCount.ToString());
	}

	private bool IsUserAllowedToJoin(Steamworks.SteamID steamIDUser)
	{
		Diagnostics.Assert(steamIDUser != null && steamIDUser.IsValid);
		switch (this.Session.SessionMode)
		{
		case SessionMode.Single:
		case SessionMode.Public:
			return true;
		case SessionMode.Private:
			Diagnostics.LogWarning("[Lobby] IsUserAllowedToJoin is not implemented yet. Return true by default for private mode.");
			return true;
		case SessionMode.Protected:
			switch (Steamworks.SteamAPI.SteamFriends.GetFriendRelationship(steamIDUser))
			{
			case Steamworks.EFriendRelationship.k_EFriendRelationshipNone:
			case Steamworks.EFriendRelationship.k_EFriendRelationshipBlocked:
			case Steamworks.EFriendRelationship.k_EFriendRelationshipRequestRecipient:
			case Steamworks.EFriendRelationship.k_EFriendRelationshipRequestInitiator:
			case Steamworks.EFriendRelationship.k_EFriendRelationshipIgnored:
			case Steamworks.EFriendRelationship.k_EFriendRelationshipIgnoredFriend:
			case Steamworks.EFriendRelationship.k_EFriendRelationshipSuggested:
				return false;
			case Steamworks.EFriendRelationship.k_EFriendRelationshipFriend:
				return true;
			default:
				throw new ArgumentOutOfRangeException();
			}
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	private bool TryChangeHandicap(string HandicapKey, string HandicapValue)
	{
		this.Session.SetLobbyData(HandicapKey, HandicapValue, true);
		return true;
	}

	private global::Session session;

	private List<uint> PlayersToCheck;

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct SlotCount
	{
		public int Total { get; set; }

		public int Occupied { get; set; }

		public int Free { get; set; }

		public override string ToString()
		{
			return string.Format("[Lobby] SlotCount: total={0}, occupied={1}, free={2}.", this.Total, this.Occupied, this.Free);
		}

		public const int MaxUserPerSlot = 1;
	}
}
