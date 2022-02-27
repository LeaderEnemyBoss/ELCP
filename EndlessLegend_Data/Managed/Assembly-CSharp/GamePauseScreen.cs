using System;
using System.Collections;
using System.Diagnostics;
using Amplitude;
using Amplitude.Interop;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Session;
using UnityEngine;

public class GamePauseScreen : GuiScreen
{
	public GamePauseScreen()
	{
		base.EnableCaptureBackgroundOnShow = true;
		base.EnableMenuAudioLayer = true;
	}

	public override bool HandleCancelRequest()
	{
		base.GuiService.GetGuiPanel<GameWorldScreen>().Show(new object[0]);
		return true;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		base.GuiService.GetGuiPanel<ControlBanner>().OnHideScreen(GameScreenType.Menu);
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		base.GuiService.GetGuiPanel<ControlBanner>().OnShowScreen(GameScreenType.Menu);
		ISessionService sessionService = Services.GetService<ISessionService>();
		Diagnostics.Assert(sessionService != null && sessionService.Session != null);
		switch (sessionService.Session.SessionMode)
		{
		case SessionMode.Single:
			this.InviteGroup.Visible = false;
			break;
		case SessionMode.Private:
		case SessionMode.Protected:
		case SessionMode.Public:
			this.InviteGroup.Visible = true;
			break;
		default:
			throw new ArgumentOutOfRangeException();
		}
		this.MenuBar.Width = 0f;
		this.MenuBar.ArrangeChildren();
		this.MenuBar.PixelOffsetLeft = -0.5f * this.MenuBar.Width;
		this.InviteButton.AgeTransform.Enable &= !TutorialManager.IsActivated;
		if (this.VersionLabel.AgeTransform.Visible)
		{
			MenuMainScreen.FormatVersionAndModulePlaylist(null, null, this.VersionLabel);
		}
		this.DlcIconEnumerator.Load();
		this.DlcIconEnumerator.AccessibilityMask = DownloadableContentAccessibility.Shared;
		this.DlcIconEnumerator.SubscriptionMask = DownloadableContentAccessibility.Shared;
		this.DlcIconEnumerator.RefreshContent();
		float biggestWidth = 0f;
		for (int i = 0; i < this.VersionLabel.TextLines.Count; i++)
		{
			float width = this.VersionLabel.Font.ComputeTextWidth(this.VersionLabel.TextLines[i], this.VersionLabel.ForceCaps, false);
			if (width > biggestWidth)
			{
				biggestWidth = width;
			}
		}
		AgeTransform parent = this.VersionLabel.AgeTransform.GetParent();
		parent.Width = biggestWidth + this.VersionLabel.AgeTransform.PixelMarginLeft + 4f;
		parent.Height = ((!AgeUtils.HighDefinition) ? ((float)this.VersionLabel.TextLines.Count * 17f) : ((float)this.VersionLabel.TextLines.Count * 17f * AgeUtils.HighDefinitionFactor));
		this.DlcIconEnumerator.AgeTransform.PixelMarginBottom = parent.PixelMarginBottom + parent.Height;
		this.RefreshContent();
		if (!TutorialManager.IsActivated)
		{
			this.GameSessionInformationPanel.Visible = true;
			GameSessionInformationSetting[] settings = this.GameSessionInformationPanel.GetComponentsInChildren<GameSessionInformationSetting>();
			foreach (GameSessionInformationSetting setting in settings)
			{
				setting.SetContent();
			}
		}
		else
		{
			this.GameSessionInformationPanel.Visible = false;
		}
		yield break;
	}

	private void OnSaveGameCB(GameObject obj)
	{
		base.GuiService.GetGuiPanel<LoadSaveModalPanel>().SaveMode = true;
		base.GuiService.GetGuiPanel<LoadSaveModalPanel>().Show(new object[]
		{
			"LoadSaveModalPanel"
		});
	}

	private void OnLoadGameCB(GameObject obj)
	{
		base.GuiService.GetGuiPanel<LoadSaveModalPanel>().SaveMode = false;
		base.GuiService.GetGuiPanel<LoadSaveModalPanel>().Show(new object[]
		{
			"LoadSaveModalPanel"
		});
	}

	private void OnOptionCB(GameObject obj)
	{
	}

	private void OnArchivesCB(GameObject obj)
	{
		Process.Start("https://endlesslegend.gamepedia.com/Endless_Legend_Wiki");
	}

	private void OnInviteCB(GameObject obj)
	{
		ISessionService service = Services.GetService<ISessionService>();
		Diagnostics.Assert(service != null && service.Session != null);
		Steamworks.SteamAPI.SteamFriends.ActivateGameOverlayInviteDialog(service.Session.SteamIDLobby);
	}

	private void OnExitDesktopCB(GameObject obj)
	{
		if (MessagePanel.Instance != null)
		{
			MessagePanel.Instance.Show("%ConfirmQuitAndExitGameTitle", string.Empty, MessagePanelButtons.YesNo, new MessagePanel.EventHandler(this.OnExitDesktopResult), MessagePanelType.IMPORTANT, new MessagePanelButton[0]);
		}
		else
		{
			this.OnExitDesktopResult(this, new MessagePanelResultEventArgs(MessagePanelResult.Yes));
		}
	}

	private void OnExitDesktopResult(object sender, MessagePanelResultEventArgs e)
	{
		if (e.Result == MessagePanelResult.Yes)
		{
			Amplitude.Unity.Framework.Application.Quit();
		}
	}

	private void OnGameSessionDetailsCB(GameObject obj)
	{
		base.GuiService.GetGuiPanel<GameSessionDetailsScreen>().Show(new object[0]);
	}

	private void OnOptionsCB(GameObject obj)
	{
		base.GuiService.Show(typeof(OptionsModalPanel), new object[0]);
	}

	private void OnQuitCB(GameObject obj)
	{
		if (MessagePanel.Instance != null)
		{
			MessagePanel.Instance.Show("%ConfirmQuitGameTitle", string.Empty, MessagePanelButtons.YesNo, new MessagePanel.EventHandler(this.OnQuitResult), MessagePanelType.IMPORTANT, new MessagePanelButton[0]);
		}
		else
		{
			this.OnQuitResult(this, new MessagePanelResultEventArgs(MessagePanelResult.Yes));
		}
	}

	private void OnQuitResult(object sender, MessagePanelResultEventArgs e)
	{
		if (e.Result == MessagePanelResult.Yes)
		{
			IRuntimeService service = Services.GetService<IRuntimeService>();
			if (service != null)
			{
				ISessionService service2 = Services.GetService<ISessionService>();
				Diagnostics.Assert(service2 != null);
				Diagnostics.Assert(service2.Session != null);
				Diagnostics.Assert(service.Runtime != null);
				Diagnostics.Assert(service.Runtime.FiniteStateMachine != null);
				if (!TutorialManager.IsActivated)
				{
					int lobbyData = service2.Session.GetLobbyData<int>("NumberOfMajorFactions", 0);
					EmpireInfo[] array = new EmpireInfo[lobbyData];
					for (int i = 0; i < lobbyData; i++)
					{
						array[i] = EmpireInfo.Read(service2.Session, i);
					}
					service.Runtime.FiniteStateMachine.PostStateChange(typeof(RuntimeState_OutGame), new object[]
					{
						"GameEnded",
						array
					});
				}
				else
				{
					service.Runtime.FiniteStateMachine.PostStateChange(typeof(RuntimeState_OutGame), new object[0]);
				}
			}
		}
	}

	private void OnResumeCB(GameObject obj)
	{
		this.HandleCancelRequest();
	}

	public AgeTransform MenuBar;

	public AgeTransform InviteGroup;

	public AgeControlButton SaveButton;

	public AgeControlButton LoadButton;

	public AgeControlButton InviteButton;

	public AgeControlButton ArchiveButton;

	public AgePrimitiveLabel VersionLabel;

	public DlcIconEnumerator DlcIconEnumerator;

	public AgeTransform GameSessionInformationPanel;
}
