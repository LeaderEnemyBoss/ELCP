using System;
using Amplitude;
using Amplitude.Interop;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Steam;
using Amplitude.Unity.View;

public class RuntimeState_OutGame : RuntimeState
{
	public override void Begin(params object[] parameters)
	{
		base.Begin(parameters);
		this.viewService = Services.GetService<Amplitude.Unity.View.IViewService>();
		if (this.viewService != null)
		{
			this.viewService.PostViewChange(typeof(OutGameView), parameters);
		}
		if (AgeLocalizer.Instance != null)
		{
			this.SetRichPresenceStatus();
		}
		else
		{
			this.guiService = Services.GetService<Amplitude.Unity.Gui.IGuiService>();
			Diagnostics.Assert(this.guiService != null);
			this.guiService.GuiSceneStateChange += this.IGuiService_GuiSceneStateChange;
		}
		if (parameters.Length == 1 && parameters[0] is ulong && new Steamworks.SteamID((ulong)parameters[0]).IsValid)
		{
			ELCPUtilities.SteamMatchMaking_TryConnectingToLobby((ulong)parameters[0]);
		}
	}

	public override void End(bool abort)
	{
		base.End(abort);
		if (this.guiService != null)
		{
			this.guiService.GuiSceneStateChange -= this.IGuiService_GuiSceneStateChange;
			this.guiService = null;
		}
	}

	protected override void OnGameLobbyJoinRequested(object sender, SteamGameLobbyJoinRequestedEventArgs e)
	{
		base.OnGameLobbyJoinRequested(sender, e);
		if (new Steamworks.SteamID(e.Message.m_steamIDLobby).IsValid)
		{
			ELCPUtilities.SteamMatchMaking_TryConnectingToLobby(e.Message.m_steamIDLobby);
		}
	}

	private void SetRichPresenceStatus()
	{
		string pchValue = AgeLocalizer.Instance.LocalizeString("%RichPresenceInMainMenu");
		Steamworks.SteamAPI.SteamFriends.SetRichPresence("status", pchValue);
	}

	private void IGuiService_GuiSceneStateChange(object sender, GuiSceneStateChangedEventArgs e)
	{
		if (e.NewState == GuiSceneStateChangedEventArgs.GuiSceneState.Loaded)
		{
			this.SetRichPresenceStatus();
		}
	}

	private Amplitude.Unity.View.IViewService viewService;

	private Amplitude.Unity.Gui.IGuiService guiService;
}
