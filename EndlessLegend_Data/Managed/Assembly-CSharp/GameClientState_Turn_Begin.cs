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
				IPlayerControllerRepositoryService playerControllerRepositoryService = base.GameClient.Game.Services.GetService<IPlayerControllerRepositoryService>();
				this.EventService.Notify(new EventTutorialGameStarted(playerControllerRepositoryService.ActivePlayerController.Empire));
			}
			if (base.GameClient.Game.Turn == 0)
			{
				IDownloadableContentService downloadableContents = Services.GetService<IDownloadableContentService>();
				if (downloadableContents != null)
				{
					List<DownloadableContent> downloadableContentsToPresent = new List<DownloadableContent>();
					foreach (DownloadableContent downloadableContent in downloadableContents)
					{
						DownloadableContentType type = downloadableContent.Type;
						if (type == DownloadableContentType.Exclusive || type == DownloadableContentType.Personal)
						{
							if (downloadableContents.IsShared(downloadableContent.Name))
							{
								downloadableContentsToPresent.Add(downloadableContent);
							}
						}
					}
					if (downloadableContentsToPresent.Count > 0)
					{
						for (int c = downloadableContentsToPresent.Count - 1; c >= 0; c--)
						{
							StaticString key = string.Format("DownloadableContent/{0}/RunOnce/Notified", downloadableContentsToPresent[c].Name);
							if (!Amplitude.Unity.Framework.Application.Registry.GetValue<bool>(key, false))
							{
								Amplitude.Unity.Framework.Application.Registry.SetValue<bool>(key, true);
								this.EventService.Notify(new EventDownloadableContentPresentation(downloadableContentsToPresent[c]));
							}
						}
					}
					Empire playerEmpire = base.GameClient.GetClientEmpire();
					if (playerEmpire != null && !playerEmpire.Faction.IsCustom && playerEmpire.Faction.IsStandard)
					{
						this.EventService.Notify(new EventFactionPresentation(playerEmpire.Faction));
					}
				}
			}
		}
		ISeasonService seasonService = base.GameClient.Game.GetService<ISeasonService>();
		SeasonManager seasonManager = seasonService as SeasonManager;
		if (seasonManager != null)
		{
			seasonManager.GameClient_Turn_Begin();
		}
		IPillarService pillarService = base.GameClient.Game.GetService<IPillarService>();
		PillarManager pillarManager = pillarService as PillarManager;
		if (pillarManager != null)
		{
			pillarManager.OnBeginTurn();
		}
		ITerraformDeviceService terraformDeviceService = base.GameClient.Game.GetService<ITerraformDeviceService>();
		TerraformDeviceManager terraformDeviceManager = terraformDeviceService as TerraformDeviceManager;
		if (terraformDeviceManager != null)
		{
			terraformDeviceManager.GameClient_Turn_Begin();
		}
		IWorldEffectService worldEffectService = base.GameClient.Game.GetService<IWorldEffectService>();
		WorldEffectManager worldEffectManager = worldEffectService as WorldEffectManager;
		if (worldEffectManager != null)
		{
			worldEffectManager.OnBeginTurn();
		}
		ILeechService leechService = base.GameClient.Game.GetService<ILeechService>();
		LeechManager leechManager = leechService as LeechManager;
		if (leechManager != null)
		{
			leechManager.OnBeginTurn();
		}
		ICooldownManagementService cooldownManagementService = base.GameClient.Game.GetService<ICooldownManagementService>();
		CooldownManager cooldownManager = cooldownManagementService as CooldownManager;
		if (cooldownManager != null)
		{
			cooldownManager.OnBeginTurn();
		}
		IMapBoostService mapBoostService = base.GameClient.Game.GetService<IMapBoostService>();
		MapBoostManager mapBoostManager = mapBoostService as MapBoostManager;
		if (mapBoostManager != null)
		{
			mapBoostManager.GameClient_OnBeginTurn();
		}
		IRegionalEffectsService regionalEffectsService = base.GameClient.Game.GetService<IRegionalEffectsService>();
		RegionalEffectsManager regionalEffectsManager = regionalEffectsService as RegionalEffectsManager;
		if (regionalEffectsManager != null)
		{
			regionalEffectsManager.GameClient_Turn_Begin();
		}
		IWeatherService weatherService = base.GameClient.Game.GetService<IWeatherService>();
		WeatherManager weatherManager = weatherService as WeatherManager;
		if (weatherManager != null)
		{
			weatherManager.GameClient_Turn_Begin();
		}
		for (int index = 0; index < base.GameClient.Game.Empires.Length; index++)
		{
			yield return base.GameClient.Game.Empires[index].DoPasses("GameClientState_Turn_Begin");
			base.GameClient.Game.Empires[index].Refresh(true);
		}
		IVisibilityService visibilityService = base.GameClient.Game.GetService<IVisibilityService>();
		if (visibilityService != null)
		{
			IPlayerControllerRepositoryService playerControllerRepositoryService2 = base.GameClient.Game.GetService<IPlayerControllerRepositoryService>();
			if (playerControllerRepositoryService2 != null && playerControllerRepositoryService2.ActivePlayerController != null && playerControllerRepositoryService2.ActivePlayerController.Empire != null)
			{
				visibilityService.NotifyVisibilityHasChanged((Empire)playerControllerRepositoryService2.ActivePlayerController.Empire);
			}
		}
		IVictoryManagementService victoryManagementService = base.GameClient.Game.GetService<IVictoryManagementService>();
		if (victoryManagementService != null)
		{
			victoryManagementService.CheckForAlerts(base.GameClient.Game.Turn - 1);
		}
		IWorldPositionningService worldPositionningService = base.GameClient.Game.GetService<IWorldPositionningService>();
		if (worldPositionningService != null)
		{
			worldPositionningService.RefreshDefensiveTowerMapForEveryone();
		}
		yield break;
	}

	private Amplitude.Coroutine coroutine;
}
