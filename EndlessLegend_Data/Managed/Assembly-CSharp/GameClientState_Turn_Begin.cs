using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Interop;
using Amplitude.Unity.Audio;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.View;
using UnityEngine;

public class GameClientState_Turn_Begin : GameClientState
{
	public GameClientState_Turn_Begin(GameClient gameClient) : base(gameClient)
	{
		base.CanSendOrder = true;
		base.CanProcessOrder = true;
	}

	[Ancillary]
	private IEventService EventService { get; set; }

	public override void Begin(params object[] parameters)
	{
		base.Begin(parameters);
		Diagnostics.Log("GameClientState_Turn_Begin.");
		this.EventService = Services.GetService<IEventService>();
		IGuiService service = Services.GetService<IGuiService>();
		Diagnostics.Assert(service != null);
		if (service.GetGuiPanel<LoadingScreen>().IsVisible)
		{
			service.GetGuiPanel<LoadingScreen>().Hide(false);
			service.GetGuiPanel<GameWorldScreen>().Show(new object[0]);
			service.GetGuiPanel<GameOverlayPanel>().Show(new object[0]);
			service.GetGuiPanel<InGameConsolePanel>().Load();
		}
		Amplitude.Unity.View.IViewService service2 = Services.GetService<Amplitude.Unity.View.IViewService>();
		if (service2 != null && (service2.CurrentView == null || service2.CurrentView.GetType() != typeof(WorldView)))
		{
			service2.PostViewChange(typeof(WorldView), new object[0]);
		}
		this.coroutine = Amplitude.Coroutine.StartCoroutine(this.RunAsync(), null);
		string text = AgeLocalizer.Instance.LocalizeString("%RichPresenceInGame" + base.GameClient.Session.SessionMode);
		text = text.Replace("$Name", base.GameClient.Session.GetLobbyData<string>("name", null));
		text = text.Replace("$Turn", (base.GameClient.Session.GetLobbyData<int>("_Turn", 0) + 1).ToString());
		Steamworks.SteamAPI.SteamFriends.SetRichPresence("status", text);
		IAudioEventService service3 = Services.GetService<IAudioEventService>();
		if (service3 != null)
		{
			service3.Play2DEvent("Gui/Interface/EndTurnValid");
		}
	}

	public override void End(bool abort)
	{
		base.End(abort);
		this.EventService = null;
	}

	public override void Run()
	{
		base.Run();
		this.coroutine.Run();
		if (this.coroutine.IsFinished)
		{
			if (base.GameClient.Game.Turn == 0 && !base.GameClient.Session.GetLobbyData<bool>("TimedTurn", false))
			{
				bool flag = false;
				string[] commandLineArgs = Environment.GetCommandLineArgs();
				foreach (string text in commandLineArgs)
				{
					flag |= text.Equals("-novideo");
				}
				if (!flag)
				{
					string graphicsDeviceVersion = SystemInfo.graphicsDeviceVersion;
					if (!graphicsDeviceVersion.StartsWith("Direct3D") || graphicsDeviceVersion.StartsWith("Direct3D 9"))
					{
						base.GameClient.PostStateChange(typeof(GameClientState_IntroductionVideo), new object[0]);
						return;
					}
					Diagnostics.Log("Unsupported graphics device version '{0}' , skipping introduction video...", new object[]
					{
						graphicsDeviceVersion
					});
				}
			}
			base.GameClient.PostStateChange(typeof(GameClientState_Turn_Main), new object[0]);
		}
	}

	private IEnumerator RunAsync()
	{
		if (this.EventService != null)
		{
			this.EventService.Notify(new EventBeginTurn(base.GameClient.Game.Turn));
			if (base.GameClient.Game.Turn == 0 && TutorialManager.IsActivated)
			{
				IPlayerControllerRepositoryService service = base.GameClient.Game.Services.GetService<IPlayerControllerRepositoryService>();
				this.EventService.Notify(new EventTutorialGameStarted(service.ActivePlayerController.Empire));
			}
			if (base.GameClient.Game.Turn == 0)
			{
				IDownloadableContentService service2 = Services.GetService<IDownloadableContentService>();
				if (service2 != null)
				{
					List<DownloadableContent> list = new List<DownloadableContent>();
					foreach (DownloadableContent downloadableContent in service2)
					{
						DownloadableContentType type = downloadableContent.Type;
						if ((type == DownloadableContentType.Exclusive || type == DownloadableContentType.Personal) && service2.IsShared(downloadableContent.Name))
						{
							list.Add(downloadableContent);
						}
					}
					if (list.Count > 0)
					{
						for (int i = list.Count - 1; i >= 0; i--)
						{
							StaticString key = string.Format("DownloadableContent/{0}/RunOnce/Notified", list[i].Name);
							if (!Amplitude.Unity.Framework.Application.Registry.GetValue<bool>(key, false))
							{
								Amplitude.Unity.Framework.Application.Registry.SetValue<bool>(key, true);
								this.EventService.Notify(new EventDownloadableContentPresentation(list[i]));
							}
						}
					}
					Empire clientEmpire = base.GameClient.GetClientEmpire();
					if (clientEmpire != null && !clientEmpire.Faction.IsCustom && clientEmpire.Faction.IsStandard)
					{
						this.EventService.Notify(new EventFactionPresentation(clientEmpire.Faction));
					}
				}
			}
		}
		SeasonManager seasonManager = base.GameClient.Game.GetService<ISeasonService>() as SeasonManager;
		if (seasonManager != null)
		{
			seasonManager.GameClient_Turn_Begin();
		}
		PillarManager pillarManager = base.GameClient.Game.GetService<IPillarService>() as PillarManager;
		if (pillarManager != null)
		{
			pillarManager.OnBeginTurn();
		}
		TerraformDeviceManager terraformDeviceManager = base.GameClient.Game.GetService<ITerraformDeviceService>() as TerraformDeviceManager;
		if (terraformDeviceManager != null)
		{
			terraformDeviceManager.GameClient_Turn_Begin();
		}
		WorldEffectManager worldEffectManager = base.GameClient.Game.GetService<IWorldEffectService>() as WorldEffectManager;
		if (worldEffectManager != null)
		{
			worldEffectManager.OnBeginTurn();
		}
		LeechManager leechManager = base.GameClient.Game.GetService<ILeechService>() as LeechManager;
		if (leechManager != null)
		{
			leechManager.OnBeginTurn();
		}
		CooldownManager cooldownManager = base.GameClient.Game.GetService<ICooldownManagementService>() as CooldownManager;
		if (cooldownManager != null)
		{
			cooldownManager.OnBeginTurn();
		}
		MapBoostManager mapBoostManager = base.GameClient.Game.GetService<IMapBoostService>() as MapBoostManager;
		if (mapBoostManager != null)
		{
			mapBoostManager.GameClient_OnBeginTurn();
		}
		RegionalEffectsManager regionalEffectsManager = base.GameClient.Game.GetService<IRegionalEffectsService>() as RegionalEffectsManager;
		if (regionalEffectsManager != null)
		{
			regionalEffectsManager.GameClient_Turn_Begin();
		}
		WeatherManager weatherManager = base.GameClient.Game.GetService<IWeatherService>() as WeatherManager;
		if (weatherManager != null)
		{
			weatherManager.GameClient_Turn_Begin();
		}
		int num;
		for (int index = 0; index < base.GameClient.Game.Empires.Length; index = num + 1)
		{
			yield return base.GameClient.Game.Empires[index].DoPasses("GameClientState_Turn_Begin");
			base.GameClient.Game.Empires[index].Refresh(true);
			num = index;
		}
		IVisibilityService service3 = base.GameClient.Game.GetService<IVisibilityService>();
		if (service3 != null)
		{
			IPlayerControllerRepositoryService service4 = base.GameClient.Game.GetService<IPlayerControllerRepositoryService>();
			if (service4 != null && service4.ActivePlayerController != null && service4.ActivePlayerController.Empire != null)
			{
				service3.NotifyVisibilityHasChanged((Empire)service4.ActivePlayerController.Empire);
			}
		}
		IVictoryManagementService service5 = base.GameClient.Game.GetService<IVictoryManagementService>();
		if (service5 != null)
		{
			service5.CheckForAlerts(base.GameClient.Game.Turn - 1);
		}
		IWorldPositionningService service6 = base.GameClient.Game.GetService<IWorldPositionningService>();
		if (service6 != null)
		{
			service6.RefreshDefensiveTowerMapForEveryone();
		}
		yield break;
	}

	private Amplitude.Coroutine coroutine;
}
