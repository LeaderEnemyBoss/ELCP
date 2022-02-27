using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Amplitude;
using Amplitude.Interop;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Session;
using Amplitude.Unity.Steam;
using UnityEngine;

public class MenuMainScreen : GuiMenuScreen
{
	public override string MenuName
	{
		get
		{
			return "%BreadcrumbMainTitle";
		}
		set
		{
		}
	}

	public static void FormatVersionAndMod(AgePrimitiveLabel currentModInfoLabel, AgePrimitiveLabel versionLabel)
	{
		if (currentModInfoLabel != null)
		{
			currentModInfoLabel.Text = "%ModDefaultTitle";
		}
		if (versionLabel != null)
		{
			versionLabel.Text = Amplitude.Unity.Framework.Application.Name.ToString() + " " + Amplitude.Unity.Framework.Application.Version.ToString();
			string a;
			if (global::Application.ResolveChineseLanguage(out a))
			{
				if (a == "schinese")
				{
					versionLabel.Text = AgeLocalizer.Instance.LocalizeString("%SChineseApplicationName") + " " + Amplitude.Unity.Framework.Application.Version.ToString();
				}
				else if (a == "tchinese")
				{
					versionLabel.Text = AgeLocalizer.Instance.LocalizeString("%TChineseApplicationName") + " " + Amplitude.Unity.Framework.Application.Version.ToString();
				}
			}
		}
		IRuntimeService service = Services.GetService<IRuntimeService>();
		if (service != null && service.Runtime != null)
		{
			RuntimeModule runtimeModule = service.Runtime.RuntimeModules.FirstOrDefault((RuntimeModule module) => module.Type == RuntimeModuleType.Standalone);
			RuntimeModule runtimeModule2 = service.Runtime.RuntimeModules.FirstOrDefault((RuntimeModule module) => module.Type == RuntimeModuleType.Conversion);
			RuntimeModule runtimeModule3;
			if (runtimeModule2 != null)
			{
				runtimeModule3 = runtimeModule2;
			}
			else
			{
				Diagnostics.Assert(runtimeModule != null);
				runtimeModule3 = runtimeModule;
				if (service != null && service.VanillaModuleName == runtimeModule.Name)
				{
					runtimeModule3 = null;
				}
			}
			if (runtimeModule3 != null && !string.IsNullOrEmpty(runtimeModule3.Title))
			{
				if (currentModInfoLabel != null)
				{
					currentModInfoLabel.Text = string.Format(AgeLocalizer.Instance.LocalizeString("%ModActiveFormat"), runtimeModule3.Title);
				}
				if (versionLabel != null)
				{
					string text = AgeLocalizer.Instance.LocalizeStringDefaults("%GameMenuModificationFormat", "$Modification $Version");
					string text2 = text.Replace("$Modification", runtimeModule3.Title).Replace("$Version", runtimeModule3.Version.ToString("V{0}.{1}.{2}"));
					if (!string.IsNullOrEmpty(text2))
					{
						versionLabel.Text = versionLabel.Text + "\n" + text2;
					}
				}
			}
		}
	}

	public static void FormatVersionAndModulePlaylist(AgeControlButton buttonLastPlaylist, AgePrimitiveLabel currentModulePlaylistLabel, AgePrimitiveLabel versionLabel)
	{
		if (currentModulePlaylistLabel != null)
		{
			currentModulePlaylistLabel.Text = "%ModDefaultTitle";
			currentModulePlaylistLabel.GetComponentInParent<AgeTooltip>().Content = string.Empty;
		}
		if (buttonLastPlaylist != null)
		{
			buttonLastPlaylist.AgeTransform.Enable = false;
		}
		if (versionLabel != null)
		{
			versionLabel.Text = Amplitude.Unity.Framework.Application.Name.ToString() + " " + Amplitude.Unity.Framework.Application.Version.ToString();
			string a;
			if (global::Application.ResolveChineseLanguage(out a))
			{
				if (a == "schinese")
				{
					versionLabel.Text = AgeLocalizer.Instance.LocalizeString("%SChineseApplicationName") + " " + Amplitude.Unity.Framework.Application.Version.ToString();
				}
				else if (a == "tchinese")
				{
					versionLabel.Text = AgeLocalizer.Instance.LocalizeString("%TChineseApplicationName") + " " + Amplitude.Unity.Framework.Application.Version.ToString();
				}
			}
			versionLabel.AgeTransform.AgeTooltip.Content = string.Empty;
		}
		Amplitude.Unity.Gui.IGuiService service = Services.GetService<Amplitude.Unity.Gui.IGuiService>();
		IRuntimeService service2 = Services.GetService<IRuntimeService>();
		IRuntimeModulePlaylistService service3 = Services.GetService<IRuntimeModulePlaylistService>();
		ModulePlaylist modulePlaylist = service3.CurrentModulePlaylist;
		if (modulePlaylist != null && modulePlaylist.Name == "VanillaModulePlaylist")
		{
			modulePlaylist = null;
		}
		if (modulePlaylist != null && modulePlaylist.Configuration.Length == 1 && modulePlaylist.Configuration[0].ModuleName == service2.VanillaModuleName)
		{
			modulePlaylist = null;
		}
		if (modulePlaylist == null)
		{
			string empty = string.Empty;
			string empty2 = string.Empty;
			if (Amplitude.Unity.Framework.Application.Registry.TryGetValue(global::Application.Registers.LastModulePlaylistActivated, out empty) && Amplitude.Unity.Framework.Application.Registry.TryGetValue(global::Application.Registers.LastModulePlaylistActivatedUrl, out empty2) && empty != "VanillaModulePlaylist")
			{
				if (buttonLastPlaylist != null)
				{
					buttonLastPlaylist.AgeTransform.Enable = true;
				}
				if (currentModulePlaylistLabel != null)
				{
					GuiElement guiElement;
					if (service.GuiPanelHelper.TryGetGuiElement(empty, out guiElement))
					{
						currentModulePlaylistLabel.Text = AgeLocalizer.Instance.LocalizeString("%OutgameLastPlaylistTitle").Replace("$PlaylistName", AgeLocalizer.Instance.LocalizeString(guiElement.Title));
					}
					else
					{
						currentModulePlaylistLabel.Text = AgeLocalizer.Instance.LocalizeString("%OutgameLastPlaylistTitle").Replace("$PlaylistName", empty);
					}
					ModulePlaylist modulePlaylist2 = new ModulePlaylist(empty, string.Empty, null, ModulePlaylist.ParseConfigurationUrl(empty2));
					modulePlaylist2.RepairPlaylistIfInvalid();
					string newValue = string.Join("\n - ", MenuMainScreen.GetPlaylistModuleTitles(modulePlaylist2));
					currentModulePlaylistLabel.GetComponentInParent<AgeTooltip>().Content = AgeLocalizer.Instance.LocalizeString("%OutgameLastPlaylistDescription").Replace("$ModuleNames", newValue);
				}
			}
		}
		else if (service2 != null && service2.Runtime != null)
		{
			modulePlaylist.RepairPlaylistIfInvalid();
			string newValue2 = modulePlaylist.Name;
			if (modulePlaylist.IsAnonymous)
			{
				newValue2 = AgeLocalizer.Instance.LocalizeString("%AnonymousActiveModulePlaylist");
			}
			string content = string.Join("\n", MenuMainScreen.GetPlaylistModuleTitles(modulePlaylist));
			if (currentModulePlaylistLabel != null)
			{
				currentModulePlaylistLabel.Text = AgeLocalizer.Instance.LocalizeString("%ModPlaylistActiveFormat").Replace("$Playlist", newValue2);
				currentModulePlaylistLabel.GetComponentInParent<AgeTooltip>().Content = content;
			}
			if (versionLabel != null)
			{
				string text = AgeLocalizer.Instance.LocalizeString("%GameMenuPlaylistFormat").Replace("$Playlist", newValue2);
				if (!string.IsNullOrEmpty(text))
				{
					versionLabel.Text = versionLabel.Text + "\n" + text;
					versionLabel.AgeTransform.AgeTooltip.Content = content;
				}
			}
		}
	}

	public static string[] GetPlaylistModuleTitles(ModulePlaylist playlist)
	{
		MenuMainScreen.<GetPlaylistModuleTitles>c__AnonStoreyA66 <GetPlaylistModuleTitles>c__AnonStoreyA = new MenuMainScreen.<GetPlaylistModuleTitles>c__AnonStoreyA66();
		<GetPlaylistModuleTitles>c__AnonStoreyA.playlist = playlist;
		IDatabase<RuntimeModule> database = Databases.GetDatabase<RuntimeModule>(true);
		List<string> list = new List<string>();
		string[] array = <GetPlaylistModuleTitles>c__AnonStoreyA.playlist.MissingModules.ToArray();
		int i = 0;
		int num = <GetPlaylistModuleTitles>c__AnonStoreyA.playlist.Configuration.Length;
		while (i < num)
		{
			list.Add(AgeLocalizer.Instance.LocalizeString(database.FirstOrDefault((RuntimeModule module) => module.Name == <GetPlaylistModuleTitles>c__AnonStoreyA.playlist.Configuration[i].ModuleName).Title));
			i++;
		}
		int k = 0;
		int num2 = array.Length;
		while (k < num2)
		{
			if (array[k].Contains(global::RuntimeManager.Folders.Workshop.Affix))
			{
				string newValue = array[k].Replace(global::RuntimeManager.Folders.Workshop.Affix, string.Empty);
				array[k] = AgeLocalizer.Instance.LocalizeString("%MissingModuleTitle").Replace("$ModuleFolder", "Workshop").Replace("$ModuleName", newValue);
			}
			else if (array[k].Contains(global::RuntimeManager.Folders.UGC.Affix))
			{
				string newValue2 = array[k].Replace(global::RuntimeManager.Folders.UGC.Affix, string.Empty);
				array[k] = AgeLocalizer.Instance.LocalizeString("%MissingModuleTitle").Replace("$ModuleFolder", "UGC").Replace("$ModuleName", newValue2);
			}
			else
			{
				array[k] = AgeLocalizer.Instance.LocalizeString("%MissingModuleTitle").Replace("$ModuleFolder", "Community").Replace("$ModuleName", array[k]);
			}
			k++;
		}
		for (int j = 0; j < array.Length; j++)
		{
			list.Add(array[j]);
		}
		return list.ToArray();
	}

	public void AlignMenuGroup()
	{
		int num = 0;
		for (int i = 0; i < this.MenuGroup.GetChildren().Count; i++)
		{
			AgeTransform ageTransform = this.MenuGroup.GetChildren()[i];
			if (ageTransform.Visible)
			{
				num++;
				ageTransform.Visible = true;
			}
		}
		this.MenuGroup.Width = this.MenuGroup.HorizontalMargin + (float)num * this.MenuGroup.ChildWidth + (float)(num - 1) * this.MenuGroup.HorizontalSpacing;
		this.MenuGroup.PixelOffsetLeft = -0.5f * this.MenuGroup.Width;
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		bool flag = false;
		IGameSerializationService service = Services.GetService<IGameSerializationService>();
		if (service != null)
		{
			GameSaveDescriptor mostRecentGameSaveDescritor = service.GetMostRecentGameSaveDescritor();
			if (mostRecentGameSaveDescritor != null)
			{
				if (mostRecentGameSaveDescritor.GameSaveSessionDescriptor.SessionMode == SessionMode.Single || Steamworks.SteamAPI.IsSteamRunning)
				{
					flag = true;
					string newValue = AgeLocalizer.Instance.LocalizeString(mostRecentGameSaveDescritor.TitleWithTurn);
					this.TitleLast.Text = AgeLocalizer.Instance.LocalizeString("%OutgameLastSaveFormat").Replace("$Name", newValue).Replace("$Date", mostRecentGameSaveDescritor.DateTime.ToShortDateString());
					this.ButtonLast.AgeTransform.Enable = true;
					this.ButtonLast.OnActivateDataObject = mostRecentGameSaveDescritor;
					this.ButtonLast.OnActivateData = mostRecentGameSaveDescritor.SourceFileName;
				}
				else
				{
					this.ButtonLast.AgeTransform.Enable = false;
				}
			}
		}
		if (!flag)
		{
			this.TitleLast.Text = "%OutgameNoLastSaveTitle";
			this.ButtonLast.AgeTransform.Enable = false;
		}
		this.UpdateFriendsOnline(5);
		MenuMainScreen.FormatVersionAndModulePlaylist(this.ButtonLastPlaylist, this.CurrentModInfoLabel, this.VersionLabel);
		float num = 0f;
		for (int i = 0; i < this.VersionLabel.TextLines.Count; i++)
		{
			float num2 = this.VersionLabel.Font.ComputeTextWidth(this.VersionLabel.TextLines[i], this.VersionLabel.ForceCaps, false);
			if (num2 > num)
			{
				num = num2;
			}
		}
		AgeTransform parent = this.VersionLabel.AgeTransform.GetParent();
		parent.Width = num + this.VersionLabel.AgeTransform.PixelMarginLeft;
		parent.Height = ((!AgeUtils.HighDefinition) ? ((float)this.VersionLabel.TextLines.Count * 17f) : ((float)this.VersionLabel.TextLines.Count * 17f * AgeUtils.HighDefinitionFactor));
		if (this.DlcIconEnumerator.AgeTransform.Visible)
		{
			this.DlcIconEnumerator.AgeTransform.PixelMarginLeft = num + this.VersionLabel.AgeTransform.PixelMarginLeft + 4f;
			this.DlcIconEnumerator.RefreshContent();
			parent.Width += (float)this.DlcIconEnumerator.AgeTransform.GetChildren().Count * 16f;
		}
	}

	public override void SetBreadcrumb(string previousRank)
	{
		base.SetBreadcrumb(previousRank);
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		bool highDefResolutionAvailable = false;
		this.HighDefGroup.Visible = highDefResolutionAvailable;
		if (this.HighDefGroup.Visible)
		{
			this.HighDefToggle.State = AgeUtils.HighDefinition;
		}
		string gameLogo = "GameLogo";
		string chineseLanguage;
		if (global::Application.ResolveChineseLanguage(out chineseLanguage))
		{
			if (chineseLanguage == "schinese")
			{
				gameLogo = "SChineseGameLogo";
			}
			else if (chineseLanguage == "tchinese")
			{
				gameLogo = "TChineseGameLogo";
			}
		}
		GuiElement guiElementImage;
		base.GuiService.GuiPanelHelper.TryGetGuiElement(gameLogo, out guiElementImage);
		if (guiElementImage != null)
		{
			Texture2D texture = null;
			if (base.GuiService.GuiPanelHelper.TryGetTextureFromIcon(guiElementImage, global::GuiPanel.IconSize.Large, out texture))
			{
				this.GameLogo.Image = texture;
			}
		}
		bool dlcAvailable = true;
		this.AlignMenuGroup();
		this.SetBreadcrumb(string.Empty);
		base.SetupDepthUp();
		base.GuiService.GetGuiPanel<MenuNewGameScreen>().Load();
		if (Amplitude.Unity.Framework.Application.Preferences.EnableMultiplayer && global::Application.CommandLineArguments.ConnectLobby != 0UL)
		{
			Steamworks.SteamID steamIDLobby = new Steamworks.SteamID(global::Application.CommandLineArguments.ConnectLobby);
			if (steamIDLobby.IsValid)
			{
				IRuntimeService runtimeService = Services.GetService<IRuntimeService>();
				runtimeService.Runtime.FiniteStateMachine.PostStateChange(typeof(RuntimeState_Lobby), new object[]
				{
					steamIDLobby
				});
				global::Application.CommandLineArguments.ConnectLobby = 0UL;
			}
		}
		this.DlcIconEnumerator.AgeTransform.Visible = dlcAvailable;
		if (dlcAvailable)
		{
			this.DlcIconEnumerator.Load();
		}
		yield return null;
		yield break;
	}

	protected override void OnUnload()
	{
		base.OnUnload();
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		this.RefreshContent();
		this.JoinButton.AgeTransform.Enable = Amplitude.Unity.Framework.Application.Preferences.EnableMultiplayer;
		List<string> disclaimerTitles = new List<string>();
		List<string> disclaimerContents = new List<string>();
		List<StaticString> disclaimerRegisters = new List<StaticString>();
		if (SystemInfo.graphicsShaderLevel < 30)
		{
			disclaimerTitles.Add("%DisclaimerInsufficientHardwareTitle");
			disclaimerContents.Add("%DisclaimerInsufficientHardwareDescription");
			disclaimerRegisters.Add(null);
		}
		if (Amplitude.Unity.Framework.Application.Version.Label.StartsWith("ALPHA") && Amplitude.Unity.Framework.Application.Version.Accessibility > Accessibility.Internal && !Amplitude.Unity.Framework.Application.Registry.GetValue<bool>(global::Application.Registers.AlphaDisclaimerAcknowledged))
		{
			disclaimerTitles.Add("%DisclaimerAlphaTitle");
			disclaimerContents.Add("%DisclaimerAlphaDescription");
			disclaimerRegisters.Add(global::Application.Registers.AlphaDisclaimerAcknowledged);
		}
		if (Amplitude.Unity.Framework.Application.Version.Label.StartsWith("BETA") && Amplitude.Unity.Framework.Application.Version.Accessibility > Accessibility.Internal && !Amplitude.Unity.Framework.Application.Registry.GetValue<bool>(global::Application.Registers.BetaDisclaimerAcknowledged))
		{
			disclaimerTitles.Add("%DisclaimerBetaTitle");
			disclaimerContents.Add("%DisclaimerBetaDescription");
			disclaimerRegisters.Add(global::Application.Registers.BetaDisclaimerAcknowledged);
		}
		if (disclaimerTitles.Count > 0)
		{
			base.GuiService.GetGuiPanel<DisclaimerModalPanel>().Show(disclaimerTitles.ToArray(), disclaimerContents.ToArray(), disclaimerRegisters.ToArray());
		}
		else
		{
			DateTime universalTime = DateTime.UtcNow;
			if (global::Application.Calendar.FreeWeekend201504Start <= universalTime && universalTime < global::Application.Calendar.FreeWeekend201504End && Amplitude.Unity.Framework.Application.Version.Accessibility > Accessibility.Internal && !Amplitude.Unity.Framework.Application.Registry.GetValue<bool>(global::Application.Registers.FreeWeekend201504DisclaimerAcknowledged))
			{
				Amplitude.Unity.Framework.Application.Registry.SetValue<bool>(global::Application.Registers.FreeWeekend201504DisclaimerAcknowledged, true);
				base.GuiService.GetGuiPanel<DisclaimerModalPanel>().ShowInformative("%DisclaimerFreeWeekEndTitle", "%DisclaimerFreeWeekEndDescription");
			}
		}
		this.IntroductionButton.AgeTransform.Enable = TutorialManager.IsEnabled;
		if (!this.IntroductionButton.AgeTransform.Enable)
		{
			this.IntroductionLabel.Text = AgeLocalizer.Instance.LocalizeString("%TutorialDisabledTitle");
		}
		else
		{
			this.IntroductionLabel.Text = AgeLocalizer.Instance.LocalizeString("%OutgameIntroductionDescription");
		}
		this.CreditsButton.AgeTransform.Enable = true;
		this.ProcessParameters();
		yield break;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		if (MessagePanel.Instance != null && MessagePanel.Instance.IsVisible)
		{
			MessagePanel.Instance.Hide(true);
		}
		yield return base.OnHide(instant);
		yield break;
	}

	private RuntimeModuleConfigurationState CheckRuntimeModules(string[] runtimeModuleNames)
	{
		IRuntimeModuleSubscriptionService service = Services.GetService<IRuntimeModuleSubscriptionService>();
		if (service != null)
		{
			string[] array;
			string[] array2;
			return service.GetRuntimeModuleListState(runtimeModuleNames, out array, out array2);
		}
		return RuntimeModuleConfigurationState.Red;
	}

	private void OnActivateLastPlaylistCB(GameObject obj = null)
	{
		string empty = string.Empty;
		string empty2 = string.Empty;
		if (Amplitude.Unity.Framework.Application.Registry.TryGetValue(global::Application.Registers.LastModulePlaylistActivated, out empty) && Amplitude.Unity.Framework.Application.Registry.TryGetValue(global::Application.Registers.LastModulePlaylistActivatedUrl, out empty2))
		{
			IRuntimeService service = Services.GetService<IRuntimeService>();
			IRuntimeModulePlaylistService service2 = Services.GetService<IRuntimeModulePlaylistService>();
			RuntimeModuleConfiguration[] configuration = ModulePlaylist.ParseConfigurationUrl(empty2);
			ModulePlaylist modulePlaylist = new ModulePlaylist(empty, string.Empty, null, configuration);
			modulePlaylist.RepairPlaylistIfInvalid();
			service2.CurrentModulePlaylist = modulePlaylist;
			service.ReloadRuntime(modulePlaylist.Configuration);
			Diagnostics.Progress.Clear();
			Diagnostics.Progress.SetProgress(0.9f, "%LoadingRuntimeModules");
			object obj2 = new LoadingScreen.DontDisplayAnyLoadingTip();
			base.GuiService.Show(typeof(LoadingScreen), new object[]
			{
				"GUI/DynamicBitmaps/Backdrop/Pov_Auriga",
				obj2
			});
		}
	}

	private void OnConfirmEnterMods(object sender, MessagePanelResultEventArgs e)
	{
		if (e.Result == MessagePanelResult.Ok)
		{
			base.GuiService.GetGuiPanel<MenuModuleSetupScreen>().Show(new object[0]);
			this.onModsCBConfirmed = true;
			this.Hide(false);
		}
	}

	private void OnCreditsCB(GameObject gameObject)
	{
		base.GuiService.Show(typeof(MenuCreditScreen), new object[0]);
		this.Hide(false);
	}

	private void OnDlcCB(GameObject gameObject)
	{
		base.GuiService.Show(typeof(DlcActivationModalPanel), new object[0]);
	}

	private void OnExitCB(GameObject gameObject)
	{
		if (MessagePanel.Instance != null)
		{
			MessagePanel.Instance.Show("%ConfirmQuitGameTitle", string.Empty, MessagePanelButtons.YesNo, new MessagePanel.EventHandler(this.OnExitResult), MessagePanelType.IMPORTANT, new MessagePanelButton[0]);
		}
		else
		{
			this.OnExitResult(this, new MessagePanelResultEventArgs(MessagePanelResult.Yes));
		}
	}

	private void OnExitResult(object sender, MessagePanelResultEventArgs e)
	{
		if (e.Result == MessagePanelResult.Yes)
		{
			Amplitude.Unity.Framework.Application.Quit();
		}
	}

	private void OnIntroductionCB(GameObject gameObject)
	{
		MessagePanel.Instance.Show("%ConfirmIntroduction", "%Confirmation", MessagePanelButtons.YesNo, new MessagePanel.EventHandler(this.OnIntroductionConfirmationCB), MessagePanelType.INFORMATIVE, new MessagePanelButton[0]);
	}

	private void OnIntroductionConfirmationCB(object sender, MessagePanelResultEventArgs e)
	{
		if (e.Result == MessagePanelResult.Yes)
		{
			ISessionService service = Services.GetService<ISessionService>();
			if (service != null && service.Session != null)
			{
				return;
			}
			TutorialManager.Launch();
		}
	}

	private void OnJoinGameCB(GameObject gameObject)
	{
		if (Amplitude.Unity.Framework.Application.Preferences.EnableMultiplayer)
		{
			base.GuiService.GetGuiPanel<MenuJoinGameScreen>().Show(new object[0]);
			this.Hide(false);
		}
	}

	private void OnLoadLastCB(GameObject gameObject)
	{
		AgeControlButton component = gameObject.GetComponent<AgeControlButton>();
		if (component != null && component.OnActivateDataObject != null)
		{
			GameSaveDescriptor gameSaveDescriptor = component.OnActivateDataObject as GameSaveDescriptor;
			if (File.Exists(gameSaveDescriptor.SourceFileName))
			{
				if (!Amplitude.Unity.Framework.Application.Preferences.EnableMultiplayer && gameSaveDescriptor.GameSaveSessionDescriptor.SessionMode != SessionMode.Single)
				{
					return;
				}
				IRuntimeService service = Services.GetService<IRuntimeService>();
				if (service != null)
				{
					RuntimeModuleConfigurationState runtimeModuleConfigurationState = this.CheckRuntimeModules(gameSaveDescriptor.RuntimeModules);
					RuntimeModuleConfigurationState runtimeModuleConfigurationState2 = runtimeModuleConfigurationState;
					switch (runtimeModuleConfigurationState2 + 1)
					{
					case RuntimeModuleConfigurationState.Yellow:
						service.Runtime.FiniteStateMachine.PostStateChange(typeof(RuntimeState_Lobby), new object[]
						{
							gameSaveDescriptor
						});
						base.GuiService.GetGuiPanel<MenuNewGameScreen>().SetupDepthDown();
						this.Hide(false);
						break;
					case RuntimeModuleConfigurationState.Red:
					case (RuntimeModuleConfigurationState)3:
						this.Hide(false);
						base.GuiService.Show(typeof(ActivateRuntimeModulesModalPanel), new object[]
						{
							gameSaveDescriptor.RuntimeModules,
							gameSaveDescriptor
						});
						break;
					}
				}
			}
			else
			{
				MessagePanel.Instance.Show("%LoadLastFileNoLongerExistsTitle", string.Empty, MessagePanelButtons.Ok, null, MessagePanelType.IMPORTANT, new MessagePanelButton[0]);
				this.RefreshContent();
			}
		}
	}

	private void OnModsCB(GameObject gameObject)
	{
		if (!this.onModsCBConfirmed)
		{
			MessagePanel.Instance.Show("%ConfirmEnterMods", "%Confirmation", MessagePanelButtons.OkCancel, new MessagePanel.EventHandler(this.OnConfirmEnterMods), MessagePanelType.IMPORTANT, new MessagePanelButton[0]);
		}
		else
		{
			this.OnConfirmEnterMods(this, new MessagePanelResultEventArgs(MessagePanelResult.Ok));
		}
	}

	private void OnNewGameCB(GameObject gameObject)
	{
		IRuntimeService service = Services.GetService<IRuntimeService>();
		if (service != null)
		{
			service.Runtime.FiniteStateMachine.PostStateChange(typeof(RuntimeState_Lobby), new object[0]);
			base.GuiService.GetGuiPanel<MenuNewGameScreen>().SetupDepthDown();
			this.Hide(false);
		}
	}

	private void OnLoadGameCB(GameObject gameObject)
	{
		Diagnostics.Log("OnLoadGameCB");
		this.LoadSaveModalPanel.SaveMode = false;
		this.LoadSaveModalPanel.Show(new object[0]);
	}

	private void OnOptionsCB(GameObject gameObject)
	{
		base.GuiService.Show(typeof(OptionsModalPanel), new object[0]);
	}

	private void OnG2GCB()
	{
		Process.Start("https://www.games2gether.com/endless-legend/forums");
	}

	private void OnSwitchHighDef(GameObject obj)
	{
		AgeManager.Instance.SwitchHighDefinition(!AgeUtils.HighDefinition);
	}

	private void ProcessParameters()
	{
	}

	private bool UpdateFriendsOnline(int maxLines = 5)
	{
		this.JoinGameInfoLabel.Text = "%OutgameJoinGameDescription";
		return false;
	}

	private void ISteamClientService_ClientPersonaStateChanged(object sender, PersonaStateChangedEventArgs e)
	{
		this.UpdateFriendsOnline(5);
	}

	public LoadSaveModalPanel LoadSaveModalPanel;

	public AgeTransform HighDefGroup;

	public AgeControlToggle HighDefToggle;

	public AgeTransform NewGameTransform;

	public AgePrimitiveImage GameLogo;

	public AgeControlButton ButtonLast;

	public AgePrimitiveLabel IntroductionLabel;

	public AgePrimitiveLabel TitleLast;

	public AgePrimitiveLabel JoinGameInfoLabel;

	public AgePrimitiveLabel CurrentModInfoLabel;

	public AgeControlButton ButtonLastPlaylist;

	public AgePrimitiveLabel DlcInfoLabel;

	public AgePrimitiveLabel VersionLabel;

	public DlcIconEnumerator DlcIconEnumerator;

	public AgePrimitiveLabel CurrentOptionsLabel;

	public AgeTransform MenuGroup;

	public AgeControlButton JoinButton;

	public AgeControlButton IntroductionButton;

	public AgeControlButton CreditsButton;

	public AgeControlButton ModsButton;

	public AgeTransform DlcGroup;

	public AgeControlButton DlcButton;

	private bool onModsCBConfirmed;
}
