using System;
using Amplitude;
using Amplitude.Interop;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Session;
using Amplitude.Unity.View;

public class GameClientState_GameLaunched : GameClientState
{
	public GameClientState_GameLaunched(GameClient gameClient) : base(gameClient)
	{
	}

	public override void Begin(params object[] parameters)
	{
		base.Begin(parameters);
		IMouseCursorService service = Services.GetService<IMouseCursorService>();
		if (service != null)
		{
			service.AddKey("Loading");
		}
		Diagnostics.Log("GameClientState_GameLaunched.");
		int num = 0;
		ISessionService service2 = Services.GetService<ISessionService>();
		Diagnostics.Assert(service2 != null);
		Diagnostics.Assert(service2.Session != null);
		Diagnostics.Assert(service2.Session.IsOpened);
		string text = service2.Session.SteamIDUser.ToString();
		Diagnostics.Assert(service2.Session.SteamIDUser.AccountID == Amplitude.Unity.Framework.Application.UserUniqueID);
		PlayerController playerController = new PlayerController(base.GameClient)
		{
			PlayerID = "player#" + service2.Session.SteamIDUser.AccountID
		};
		for (;;)
		{
			string x = string.Format("Empire{0}", num);
			string lobbyData = service2.Session.GetLobbyData<string>(x, null);
			if (string.IsNullOrEmpty(lobbyData))
			{
				break;
			}
			if (lobbyData.Contains(text))
			{
				goto Block_3;
			}
			num++;
		}
		Diagnostics.LogError("Player doesn't belong here (SteamUserID: {0}).", new object[]
		{
			text
		});
		goto IL_14E;
		Block_3:
		playerController.Empire = base.GameClient.Game.Empires[num];
		IL_14E:
		int num2 = 0;
		for (;;)
		{
			string x2 = string.Format("Empire{0}", num2);
			string lobbyData2 = service2.Session.GetLobbyData<string>(x2, null);
			if (string.IsNullOrEmpty(lobbyData2))
			{
				break;
			}
			base.GameClient.Game.Empires[num2].IsControlledByAI = true;
			MajorEmpire majorEmpire = base.GameClient.Game.Empires[num2] as MajorEmpire;
			if (majorEmpire != null)
			{
				if (!lobbyData2.StartsWith("AI"))
				{
					majorEmpire.IsControlledByAI = false;
					if (Steamworks.SteamAPI.IsSteamRunning)
					{
						string[] array = lobbyData2.Split(Amplitude.String.Separators, StringSplitOptions.RemoveEmptyEntries);
						for (int i = 0; i < array.Length; i++)
						{
							ulong value = Convert.ToUInt64(array[i], 16);
							Steamworks.SteamID steamID = new Steamworks.SteamID(value);
							if (!service2.Session.GetLobbyMemberData<bool>(steamID, "Ready", false) && base.GameClient.Session.SessionMode != SessionMode.Single)
							{
								majorEmpire.BindPlayer(new Player(majorEmpire)
								{
									Type = PlayerType.AI,
									Location = ((base.GameClient.Session.GameServer == null) ? PlayerLocation.Remote : PlayerLocation.Local),
									LocalizedName = MajorEmpire.GenerateBasicAIName(majorEmpire.Index)
								});
							}
							else
							{
								string text2 = Steamworks.SteamAPI.SteamFriends.GetFriendPersonaName(steamID);
								if (text2 == null)
								{
									text2 = AgeLocalizer.Instance.LocalizeString("%DefaultPlayerName");
								}
								majorEmpire.BindPlayer(new Player(majorEmpire)
								{
									Type = PlayerType.Human,
									Location = ((!(steamID == base.GameClient.SteamIDUser)) ? PlayerLocation.Remote : PlayerLocation.Local),
									LocalizedName = text2,
									SteamID = steamID
								});
							}
						}
					}
					else
					{
						Diagnostics.LogWarning("Steam is not running, cannot get player's name, setting a default one.");
						majorEmpire.BindPlayer(new Player(majorEmpire)
						{
							Type = PlayerType.Human,
							Location = PlayerLocation.Local,
							LocalizedName = AgeLocalizer.Instance.LocalizeString("%DefaultPlayerName"),
							SteamID = Steamworks.SteamID.Zero
						});
					}
				}
				else
				{
					majorEmpire.BindPlayer(new Player(majorEmpire)
					{
						Type = PlayerType.AI,
						Location = ((base.GameClient.Session.GameServer == null) ? PlayerLocation.Remote : PlayerLocation.Local),
						LocalizedName = MajorEmpire.GenerateBasicAIName(majorEmpire.Index)
					});
				}
			}
			num2++;
		}
		IPlayerControllerRepositoryService service3 = base.GameClient.Game.GetService<IPlayerControllerRepositoryService>();
		if (service3 != null)
		{
			for (int j = 0; j < base.GameClient.Game.Empires.Length; j++)
			{
				PlayerController playerController2 = new PlayerController(base.GameClient)
				{
					Empire = base.GameClient.Game.Empires[j],
					PlayerID = base.GameClient.Game.Empires[j].PlayerID
				};
				service3.Register(playerController2);
				base.GameClient.Game.Empires[j].PlayerController = playerController2;
				base.GameClient.Game.Empires[j].PlayerControllers.Client = playerController2;
			}
		}
		if (service3 != null)
		{
			service3.Register(playerController);
			service3.SetActivePlayerController(playerController);
		}
		Ticket.ResetCounter(0UL);
		Amplitude.Unity.View.IViewService service4 = Services.GetService<Amplitude.Unity.View.IViewService>();
		if (service4 != null)
		{
			WorldView worldView = (WorldView)service4.FindByType(typeof(WorldView));
			if (worldView != null)
			{
				worldView.WorldViewTechniqueChange += this.WorldView_WorldViewTechniqueChange;
			}
			service4.PostViewChange(typeof(WorldView), new object[0]);
		}
		Amplitude.Unity.Gui.IGuiService service5 = Services.GetService<Amplitude.Unity.Gui.IGuiService>();
		Diagnostics.Assert(service5 != null);
		ConsolePanel guiPanel = service5.GetGuiPanel<ConsolePanel>();
		Diagnostics.Assert(guiPanel != null);
		guiPanel.Load();
		guiPanel.Hide(false);
	}

	public override void End(bool abort)
	{
		base.End(abort);
		base.GameClient.TakeSnapshot();
		IMouseCursorService service = Services.GetService<IMouseCursorService>();
		if (service != null)
		{
			service.RemoveKey("Loading");
		}
		Amplitude.Unity.View.IViewService service2 = Services.GetService<Amplitude.Unity.View.IViewService>();
		if (service2 != null)
		{
			WorldView worldView = (WorldView)service2.FindByType(typeof(WorldView));
			if (worldView != null)
			{
				worldView.WorldViewTechniqueChange -= this.WorldView_WorldViewTechniqueChange;
			}
		}
	}

	private void WorldView_WorldViewTechniqueChange(object sender, WorldViewTechniqueChangeEventArgs e)
	{
		WorldViewTechniqueChangeAction action = e.Action;
		if (action == WorldViewTechniqueChangeAction.Ready)
		{
			(sender as WorldView).WorldViewTechniqueChange -= this.WorldView_WorldViewTechniqueChange;
			base.GameClient.PostStateChange(typeof(GameClientState_GameLaunchedAndReady), new object[0]);
		}
	}
}
