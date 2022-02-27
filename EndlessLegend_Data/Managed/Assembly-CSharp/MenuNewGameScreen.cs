using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Interop;
using Amplitude.Unity.Audio;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Session;
using Amplitude.Unity.Steam;
using UnityEngine;

public class MenuNewGameScreen : GuiMenuScreen
{
	public MenuNewGameScreen()
	{
		this.profanityError = string.Empty;
		this.invalidColor = new Color(0.7529412f, 0.2509804f, 0.2509804f);
		base..ctor();
	}

	private void StartProfanityFiltering()
	{
	}

	public override string MenuName
	{
		get
		{
			if (this.IsGameInProgress)
			{
				return "%BreadcrumbGameInProgressTitle";
			}
			if (this.IsMultiplayerSave)
			{
				return "%BreadcrumbLoadMultiplayerGameTitle";
			}
			return "%BreadcrumbNewGameTitle";
		}
		set
		{
		}
	}

	private GameSaveDescriptor GameSaveDescriptor { get; set; }

	private bool IsGameInProgress
	{
		get
		{
			if (this.Session == null)
			{
				this.Session = (Services.GetService<ISessionService>().Session as global::Session);
			}
			return this.Session.GetLobbyData<bool>("_GameInProgress", false) || this.Session.GetLobbyData<bool>("_Launching", false);
		}
	}

	private void OnSessionNameChangeCB(GameObject gameObject)
	{
		this.StartProfanityFiltering();
	}

	private bool IsMultiplayerSave
	{
		get
		{
			if (this.Session == null)
			{
				this.Session = (Services.GetService<ISessionService>().Session as global::Session);
			}
			return this.Session.GetLobbyData<bool>("_IsSavedGame", false);
		}
	}

	private bool HasGameBeenLaunchedOnce
	{
		get
		{
			return this.IsMultiplayerSave || this.Session.GetLobbyData<bool>("_Launching", false) || this.Session.GetLobbyData<bool>("_GameInProgress", false) || this.Session.GetLobbyData<int>("_Turn", 0) > 0;
		}
	}

	private bool CanModifyOwnEmpireSettings
	{
		get
		{
			return !this.HasGameBeenLaunchedOnce && !this.guiLocked;
		}
	}

	private bool CanModifyGameSettings
	{
		get
		{
			return this.Session.IsHosting && !this.HasGameBeenLaunchedOnce && !this.guiLocked;
		}
	}

	private global::Session Session
	{
		get
		{
			return this.session;
		}
		set
		{
			if (value != this.session)
			{
				if (this.session != null)
				{
					this.session.LobbyChatMessage -= this.Session_LobbyChatMessage;
					this.session.LobbyDataChange -= this.Session_LobbyDataChange;
					this.session.LobbyMemberDataChange -= this.Session_LobbyMemberDataChange;
				}
				this.session = value;
				if (this.Session != null)
				{
					this.session.LobbyChatMessage += this.Session_LobbyChatMessage;
					this.session.LobbyDataChange += this.Session_LobbyDataChange;
					this.session.LobbyMemberDataChange += this.Session_LobbyMemberDataChange;
				}
				this.RefreshContent();
			}
		}
	}

	[Service]
	private ISessionService SessionService
	{
		get
		{
			return this.sessionService;
		}
		set
		{
			if (this.sessionService != null)
			{
				this.sessionService.SessionChange -= this.SessionService_SessionChange;
			}
			this.sessionService = value;
			if (this.sessionService != null)
			{
				this.sessionService.SessionChange += this.SessionService_SessionChange;
			}
		}
	}

	private ISteamClientService SteamClientService
	{
		get
		{
			return this.steamClientService;
		}
		set
		{
			if (this.steamClientService != null)
			{
				this.steamClientService.ClientPersonaStateChanged -= this.ISteamClientService_ClientPersonaStateChanged;
			}
			this.steamClientService = value;
			if (this.steamClientService != null)
			{
				this.steamClientService.ClientPersonaStateChanged += this.ISteamClientService_ClientPersonaStateChanged;
			}
		}
	}

	[Service]
	private ISteamMatchMakingService SteamMatchMakingService { get; set; }

	private MenuCompetitorSlot WhichCompetitorSlotIsMine
	{
		get
		{
			if (this.CompetitorsTable == null)
			{
				return null;
			}
			List<AgeTransform> children = this.CompetitorsTable.GetChildren<AgeTransform>(true);
			if (children == null || children.Count == 0)
			{
				return null;
			}
			for (int i = 0; i < children.Count; i++)
			{
				MenuCompetitorSlot component = children[i].GetComponent<MenuCompetitorSlot>();
				if (component != null && component.CompetitorIsHuman && component.CompetitorIsLocalOwner)
				{
					return component;
				}
			}
			return null;
		}
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		base.AgeTransform.Enable = (this.Session != null);
		this.GenerationScenarioSW.ResetUp();
		this.RefreshGameOptions();
		this.RefreshWorldGeneratorOptions();
		this.RefreshEmpireInformation();
		this.RefreshScenarioInformation();
		this.RefreshChatWindowsVisibility();
		this.RefreshSessionPanel();
		this.RefreshCompetitorSlots();
		this.RefreshClientDownloadableContents();
		this.RefreshButtons();
	}

	public override bool HandleCancelRequest()
	{
		if (this.guiLocked)
		{
			return false;
		}
		if (base.IsShowing)
		{
			base.StartCoroutine(this.HandleCancelRequestWhenShowingFinished());
			return true;
		}
		base.SetupDepthDown();
		if (this.Session != null && this.Session.SessionMode != SessionMode.Single && !MessagePanel.Instance.IsShowing && !MessagePanel.Instance.IsVisible)
		{
			MessagePanel.Instance.Show("%ConfirmQuitMultiplayerSession", "%Confirmation", MessagePanelButtons.YesNo, new MessagePanel.EventHandler(this.OnConfirmMultiplayerquit), MessagePanelType.IMPORTANT, new MessagePanelButton[0]);
		}
		else
		{
			this.DoExit();
		}
		return true;
	}

	public void SelectFaction(int empireIndex, Faction faction)
	{
		Diagnostics.Assert(faction != null);
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		IDatabase<Faction> database = Databases.GetDatabase<Faction>(false);
		if (service != null && database != null)
		{
			bool flag = false;
			if (!service.TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory.LobbyFaction, faction.Name, out flag) || !flag)
			{
				faction = database.FirstOrDefault((Faction iterator) => iterator.IsStandard && !iterator.IsHidden);
			}
		}
		ISessionService service2 = Services.GetService<ISessionService>();
		if (service2.Session != null && empireIndex >= 0)
		{
			if (faction.IsCustom)
			{
				string message = string.Format("q:/Faction{0}/{1}", empireIndex, Faction.Encode(faction));
				service2.Session.SendLobbyChatMessage(message);
			}
			else
			{
				string message2 = string.Format("q:/Faction{0}/{1}", empireIndex, faction.Name);
				service2.Session.SendLobbyChatMessage(message2);
			}
			if (empireIndex == this.GetPlayerEmpireIndex())
			{
				Amplitude.Unity.Framework.Application.Registry.SetValue<StaticString>("Preferences/Lobby/Faction", faction.Name);
			}
		}
	}

	public bool FocusOutGameConsolePanel()
	{
		return this.OutGameConsolePanel.IsVisible && this.OutGameConsolePanel.Focus();
	}

	public Faction GetSelectedFaction(int empireIndex)
	{
		Faction faction = null;
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		IDatabase<Faction> database = Databases.GetDatabase<Faction>(false);
		ISessionService service2 = Services.GetService<ISessionService>();
		if (service2.Session != null)
		{
			string lobbyData = service2.Session.GetLobbyData<string>("Faction" + empireIndex, null);
			if (!string.IsNullOrEmpty(lobbyData))
			{
				faction = Faction.Decode(lobbyData);
				if (service != null && database != null)
				{
					bool flag = false;
					if (!service.TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory.LobbyFaction, faction.Name, out flag) || !flag)
					{
						faction = database.FirstOrDefault((Faction iterator) => iterator.IsStandard && !iterator.IsHidden);
					}
				}
			}
		}
		return faction;
	}

	public void OnCustomFactionChanged(Faction changedFaction)
	{
		ISessionService service = Services.GetService<ISessionService>();
		if (service.Session == null)
		{
			return;
		}
		if (this.IsMultiplayerSave || service.Session.GetLobbyData<bool>("_Launching", false) || service.Session.GetLobbyData<bool>("_GameInProgress", false) || service.Session.GetLobbyData<int>("_Turn", 0) > 0 || this.guiLocked)
		{
			return;
		}
		if (service.Session.IsHosting)
		{
			int numberOfSlots = this.GetNumberOfSlots();
			for (int i = 0; i < numberOfSlots; i++)
			{
				Faction selectedFaction = this.GetSelectedFaction(i);
				if (selectedFaction.Name == changedFaction.Name)
				{
					this.SelectFaction(i, changedFaction);
				}
			}
		}
		else
		{
			int playerEmpireIndex = this.GetPlayerEmpireIndex();
			if (playerEmpireIndex >= 0)
			{
				Faction selectedFaction2 = this.GetSelectedFaction(playerEmpireIndex);
				if (selectedFaction2.Name == changedFaction.Name)
				{
					this.SelectFaction(playerEmpireIndex, changedFaction);
				}
			}
		}
	}

	public void OnCustomFactionDeleted(string deletedFactionName)
	{
		ISessionService service = Services.GetService<ISessionService>();
		if (service.Session == null)
		{
			return;
		}
		if (this.IsMultiplayerSave || service.Session.GetLobbyData<bool>("_Launching", false) || service.Session.GetLobbyData<bool>("_GameInProgress", false) || service.Session.GetLobbyData<int>("_Turn", 0) > 0 || this.guiLocked)
		{
			return;
		}
		if (service.Session.IsHosting)
		{
			int numberOfSlots = this.GetNumberOfSlots();
			for (int i = 0; i < numberOfSlots; i++)
			{
				Faction selectedFaction = this.GetSelectedFaction(i);
				if (selectedFaction.Name == deletedFactionName)
				{
					this.SelectFaction(i, this.guiFactions[0].Faction);
				}
			}
		}
		else
		{
			int playerEmpireIndex = this.GetPlayerEmpireIndex();
			if (playerEmpireIndex >= 0)
			{
				Faction selectedFaction2 = this.GetSelectedFaction(playerEmpireIndex);
				if (selectedFaction2.Name == deletedFactionName)
				{
					this.SelectFaction(playerEmpireIndex, this.guiFactions[0].Faction);
				}
			}
		}
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		base.AgeTransform.Enable = false;
		this.BuildFactionsLists();
		this.BuildColorsList();
		IGameSerializationService gameSerializationService = Services.GetService<IGameSerializationService>();
		if (gameSerializationService != null)
		{
			this.GameSaveDescriptor = gameSerializationService.GameSaveDescriptor;
		}
		else
		{
			this.GameSaveDescriptor = null;
		}
		this.SessionService = Services.GetService<ISessionService>();
		if (this.SessionService != null)
		{
			this.Session = (this.SessionService.Session as global::Session);
			SessionState.OnLockLobbyGUI += this.SessionState_OnLockLobbyGUI;
			if (this.Session != null && this.Session.IsHosting)
			{
				IDatabase<WorldGeneratorOptionDefinition> optionDefinitions = Databases.GetDatabase<WorldGeneratorOptionDefinition>(false);
				WorldGeneratorOptionDefinition optionDefinitionSeedNumber;
				if (optionDefinitions != null && optionDefinitions.TryGetValue("SeedNumber", out optionDefinitionSeedNumber))
				{
					Diagnostics.Assert(optionDefinitionSeedNumber.ItemDefinitions.Length == 1);
					Diagnostics.Assert(optionDefinitionSeedNumber.ItemDefinitions[0].KeyValuePairs.Length == 1);
					int seed = Amplitude.Unity.Framework.Application.Registry.GetValue<int>("Preferences/Lobby/Seed", 0);
					optionDefinitionSeedNumber.ItemDefinitions[0].KeyValuePairs[0].Value = seed.ToString();
				}
			}
		}
		this.guiLocked = false;
		this.triedToSelectPreferredColor = false;
		this.StartReadyToggle.State = false;
		this.OnSelectGenerationScenarioCB(null);
		base.NeedRefresh = true;
		MenuMainScreen.FormatVersionAndModulePlaylist(null, null, this.VersionLabel);
		yield break;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		base.NeedRefresh = false;
		this.guiLocked = false;
		this.triedToSelectPreferredColor = false;
		this.StartReadyToggle.State = false;
		this.UnbindCompetitorSlots();
		SessionState.OnLockLobbyGUI -= this.SessionState_OnLockLobbyGUI;
		this.SessionService = null;
		this.GameSaveDescriptor = null;
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		base.UseRefreshLoop = true;
		this.setupStandardOptionDelegate = new AgeTransform.RefreshTableItem<OptionDefinition>(this.SetupStandardOption);
		this.setupCompetitorSlotDelegate = new AgeTransform.RefreshTableItem<int>(this.SetupCompetitorSlot);
		this.SteamClientService = Services.GetService<ISteamClientService>();
		Diagnostics.Assert(this.SteamClientService != null);
		this.CompetitorsTable.StartNewMesh = true;
		this.BuildStandardOptions();
		this.BuildSessionModesDropList();
		this.BuildScenariosDropList();
		base.GuiService.GetGuiPanel<MenuAdvancedSetupScreen>().Load();
		base.GuiService.GetGuiPanel<MenuFactionScreen>().Load();
		this.DlcFrame.Visible = false;
		this.DlcFrame.Visible = true;
		this.DlcIconEnumerator.Load();
		this.DlcIconEnumerator.AccessibilityMask = DownloadableContentAccessibility.Shared;
		this.DlcIconEnumerator.SubscriptionMask = DownloadableContentAccessibility.Shared;
		this.ClientDlcIconEnumerator.Load();
		yield break;
	}

	protected override void OnUnload()
	{
		this.SteamClientService = null;
		this.setupStandardOptionDelegate = null;
		this.setupCompetitorSlotDelegate = null;
		base.OnUnload();
	}

	private void BuildStandardOptions()
	{
		Texture2D image = null;
		GuiElement guiElement;
		if (base.GuiService.GuiPanelHelper.TryGetGuiElement("NewGameMenuGameSettingsSummary", out guiElement) && base.GuiService.GuiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Large, out image))
		{
			this.GameSettingsSummaryImage.Image = image;
		}
		if (base.GuiService.GuiPanelHelper.TryGetGuiElement("NewGameMenuWorldSettingsSummary", out guiElement) && base.GuiService.GuiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Large, out image))
		{
			this.WorldSettingsSummaryImage.Image = image;
		}
		IDatabase<OptionDefinition> database = Databases.GetDatabase<OptionDefinition>(false);
		if (database != null)
		{
			List<OptionDefinition> list = (from optionDefinition in database
			where optionDefinition.Category == "Game" && !optionDefinition.IsAdvanced && !optionDefinition.IsHidden
			select optionDefinition).ToList<OptionDefinition>();
			this.GameStandardSettingsTable.ReserveChildren(list.Count, this.MenuStandardSettingPrefab, "Item");
			this.GameStandardSettingsTable.RefreshChildrenIList<OptionDefinition>(list, this.setupStandardOptionDelegate, true, false);
		}
		IDatabase<WorldGeneratorOptionDefinition> database2 = Databases.GetDatabase<WorldGeneratorOptionDefinition>(false);
		if (database2 != null)
		{
			List<OptionDefinition> list2 = (from optionDefinition in database2
			where optionDefinition.Category == "World" && !optionDefinition.IsAdvanced && !optionDefinition.IsHidden
			select optionDefinition).Cast<OptionDefinition>().ToList<OptionDefinition>();
			this.WorldStandardSettingsTable.ReserveChildren(list2.Count, this.MenuStandardSettingPrefab, "Item");
			this.WorldStandardSettingsTable.RefreshChildrenIList<OptionDefinition>(list2, this.setupStandardOptionDelegate, true, false);
		}
	}

	private void BuildFactionsLists()
	{
		this.guiFactions.Clear();
		IDatabase<Faction> database = Databases.GetDatabase<Faction>(false);
		Diagnostics.Assert(database != null);
		this.guiFactions = (from faction in database.GetValues()
		where faction.IsStandard || faction.IsCustom
		select new GuiFaction(faction)).ToList<GuiFaction>();
		this.guiFactions.Sort(new GuiFaction.Comparer());
	}

	private void BuildColorsList()
	{
		this.colorsList.Clear();
		IDatabase<Palette> database = Databases.GetDatabase<Palette>(true);
		Diagnostics.Assert(database != null);
		string value = Amplitude.Unity.Framework.Application.Registry.GetValue<string>("Settings/UI/EmpireColorPalette", "Standard");
		Palette palette;
		if (!database.TryGetValue(value, out palette))
		{
			Diagnostics.LogWarning("Could not get the empire colors' palette! Palette name = {0}.", new object[]
			{
				value
			});
			return;
		}
		if (palette.Colors != null && palette.Colors.Length > 0)
		{
			for (int i = 0; i < palette.Colors.Length; i++)
			{
				if (palette.Colors[i].Tags == null || !palette.Colors[i].Tags.Contains("Hidden"))
				{
					this.colorsList.Add(palette.Colors[i].ToColor());
				}
			}
		}
	}

	private void BuildScenariosDropList()
	{
		this.GenerationScenarioDropList.AgeTransform.Enable = false;
		List<string> list = new List<string>();
		list.Add("%WorldGeneratorScenarioNone");
		this.scenarios = new List<WorldGeneratorScenarioDefinition>(10);
		this.scenarios.Add(null);
		this.GenerationScenarioDropList.ItemTable = list.ToArray();
		this.GenerationScenarioDropList.TooltipTable = null;
		this.GenerationScenarioDropList.SelectedItem = 0;
		IRuntimeService service = Services.GetService<IRuntimeService>();
		if (service == null || service.Runtime == null || service.Runtime.Configuration == null)
		{
			return;
		}
		if (service.Runtime.Configuration.Length != 1 || service.Runtime.Configuration[0].ModuleName != service.VanillaModuleName)
		{
		}
		List<string> list2 = new List<string>();
		list2.Add(string.Empty);
		IDatabase<GuiElement> database = Databases.GetDatabase<GuiElement>(true);
		foreach (WorldGeneratorScenarioDefinition worldGeneratorScenarioDefinition in Databases.GetDatabase<WorldGeneratorScenarioDefinition>(true).GetValues())
		{
			string x = "%WorldGeneratorScenario" + worldGeneratorScenarioDefinition.Name;
			GuiElement guiElement;
			if (database.TryGetValue(x, out guiElement))
			{
				this.scenarios.Add(worldGeneratorScenarioDefinition);
				list.Add(guiElement.Title);
				list2.Add(guiElement.Description);
			}
			else
			{
				this.scenarios.Add(worldGeneratorScenarioDefinition);
				list.Add("%WorldGeneratorScenarioNoGuiElementTitle");
				list2.Add(string.Empty);
			}
		}
		this.GenerationScenarioDropList.ItemTable = list.ToArray();
		this.GenerationScenarioDropList.TooltipTable = list2.ToArray();
		this.GenerationScenarioDropList.SelectedItem = 0;
	}

	private void BuildSessionModesDropList()
	{
		this.sessionModeNames = (from name in Enum.GetNames(typeof(SessionMode))
		where name != "Private"
		select name).ToList<string>();
		if (this.SessionTypeDropList != null)
		{
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			for (int i = 0; i < this.sessionModeNames.Count; i++)
			{
				list.Add("%SessionMode" + this.sessionModeNames[i] + "Title");
				list2.Add("%SessionMode" + this.sessionModeNames[i] + "Description");
			}
			this.SessionTypeDropList.ItemTable = list.ToArray();
			this.SessionTypeDropList.TooltipTable = list2.ToArray();
			this.SessionTypeDropList.SelectedItem = 0;
		}
	}

	private int GetPlayerEmpireIndex()
	{
		ISessionService service = Services.GetService<ISessionService>();
		if (service.Session != null)
		{
			int num = 0;
			for (;;)
			{
				string x = string.Format("Empire{0}", num);
				string lobbyData = service.Session.GetLobbyData<string>(x, null);
				if (string.IsNullOrEmpty(lobbyData))
				{
					break;
				}
				if (!lobbyData.StartsWith("AI"))
				{
					string[] array = lobbyData.Split(Amplitude.String.Separators, StringSplitOptions.RemoveEmptyEntries);
					for (int i = 0; i < array.Length; i++)
					{
						try
						{
							ulong value = Convert.ToUInt64(array[i], 16);
							Steamworks.SteamID x2 = new Steamworks.SteamID(value);
							if (x2 == service.Session.SteamIDUser)
							{
								return num;
							}
						}
						catch (Exception ex)
						{
							Diagnostics.LogError(ex.Message + " lobbyDataEmpire = " + lobbyData);
						}
					}
				}
				num++;
			}
			return -1;
		}
		return -1;
	}

	private int GetNumberOfSlots()
	{
		int lobbyData = this.Session.GetLobbyData<int>("NumberOfMajorFactions", 0);
		Diagnostics.Assert(lobbyData >= 0);
		Diagnostics.Assert(lobbyData <= 32);
		return Math.Max(0, Math.Min(32, lobbyData));
	}

	private void DoExit()
	{
		IRuntimeService service = Services.GetService<IRuntimeService>();
		if (service != null)
		{
			service.Runtime.FiniteStateMachine.PostStateChange(typeof(RuntimeState_OutGame), new object[0]);
		}
	}

	private IEnumerator HandleCancelRequestWhenShowingFinished()
	{
		while (base.IsShowing)
		{
			yield return null;
		}
		this.HandleCancelRequest();
		yield break;
	}

	private void OnAdvancedWorldSettingsCB(GameObject gameObject)
	{
		base.SetupDepthUp();
		base.GuiService.GetGuiPanel<MenuAdvancedSetupScreen>().SetupDepthDown();
		base.GuiService.GetGuiPanel<MenuAdvancedSetupScreen>().Show(new object[]
		{
			"World",
			(!this.CanModifyGameSettings) ? "readonly" : string.Empty
		});
		base.GuiService.GetGuiPanel<MenuAdvancedSetupScreen>().SetBreadcrumb(this.MenuBreadcrumb.Text);
	}

	private void OnAdvancedGameSettingsCB(GameObject gameObject)
	{
		base.SetupDepthUp();
		base.GuiService.GetGuiPanel<MenuAdvancedSetupScreen>().SetupDepthDown();
		base.GuiService.GetGuiPanel<MenuAdvancedSetupScreen>().Show(new object[]
		{
			"Game",
			(!this.CanModifyGameSettings) ? "readonly" : string.Empty
		});
		base.GuiService.GetGuiPanel<MenuAdvancedSetupScreen>().SetBreadcrumb(this.MenuBreadcrumb.Text);
	}

	private void OnAdvancedFactionCB(GameObject gameObject)
	{
		int num;
		if (gameObject)
		{
			MenuCompetitorSlot component = gameObject.GetComponent<MenuCompetitorSlot>();
			if (component != null)
			{
				num = component.EmpireIndex;
			}
			else
			{
				num = this.GetPlayerEmpireIndex();
			}
		}
		else
		{
			num = this.GetPlayerEmpireIndex();
		}
		if (num < 0)
		{
			return;
		}
		Faction selectedFaction = this.GetSelectedFaction(num);
		if (selectedFaction == null)
		{
			return;
		}
		base.SetupDepthUp();
		base.GuiService.GetGuiPanel<MenuFactionScreen>().SetupDepthDown();
		base.GuiService.GetGuiPanel<MenuFactionScreen>().Show(new object[]
		{
			num,
			selectedFaction
		});
		base.GuiService.GetGuiPanel<MenuFactionScreen>().SetBreadcrumb(this.MenuBreadcrumb.Text);
	}

	private void OnAdvancedScenarioCB(GameObject gameObject)
	{
		base.SetupDepthUp();
	}

	private void OnCancelCB(GameObject gameObject)
	{
		this.HandleCancelRequest();
	}

	private void OnConfirmMultiplayerquit(object sender, MessagePanelResultEventArgs e)
	{
		if (e.Result == MessagePanelResult.Yes)
		{
			this.DoExit();
		}
	}

	private void OnSelectGenerationScenarioCB(GameObject gameObject)
	{
		if (this.Session == null || !this.Session.IsHosting)
		{
			return;
		}
		try
		{
			int selectedItem = this.GenerationScenarioDropList.SelectedItem;
			WorldGeneratorScenarioDefinition worldGeneratorScenarioDefinition = this.scenarios[selectedItem];
			bool flag;
			if (worldGeneratorScenarioDefinition == null)
			{
				flag = WorldGeneratorScenarioDefinition.Select(false);
			}
			else
			{
				flag = WorldGeneratorScenarioDefinition.Select(worldGeneratorScenarioDefinition);
			}
			if (flag)
			{
				IDatabase<WorldGeneratorOptionDefinition> database = Databases.GetDatabase<WorldGeneratorOptionDefinition>(false);
				if (database != null)
				{
					WorldGenerator.CheckWorldGeneratorOptionsConstraints();
				}
				if (WorldGeneratorScenarioDefinition.Current != null)
				{
					this.Session.SetLobbyData("Scenario", WorldGeneratorScenarioDefinition.Current.Name, true);
				}
				else
				{
					this.Session.SetLobbyData("Scenario", string.Empty, true);
				}
				this.BuildStandardOptions();
				base.NeedRefresh = true;
			}
		}
		catch
		{
		}
	}

	private void OnValidateChatMessage(string chatMessage)
	{
		if (this.Session == null)
		{
			return;
		}
		if (string.IsNullOrEmpty(chatMessage))
		{
			return;
		}
		if (this.Session.SessionMode == SessionMode.Private || this.Session.SessionMode == SessionMode.Protected || this.Session.SessionMode == SessionMode.Public)
		{
			this.Session.SendLobbyChatMessage("c:/" + chatMessage);
		}
	}

	private void OnSessionChoiceCB(GameObject gameObject)
	{
		if (this.SessionTypeDropList != null && this.Session != null)
		{
			try
			{
				SessionMode sessionMode = (SessionMode)((int)Enum.Parse(typeof(SessionMode), this.sessionModeNames[this.SessionTypeDropList.SelectedItem]));
				this.Session.Reopen(sessionMode, 16);
			}
			catch
			{
			}
			this.RefreshButtons();
		}
	}

	private void OnStartCB(GameObject gameObject)
	{
		this.Session.LocalPlayerReady = true;
		base.SetupDepthUp();
		this.Hide(false);
	}

	private void OnStartReadyToggleCB(GameObject gameObject)
	{
		this.Session.LocalPlayerReady = this.StartReadyToggle.State;
		if (this.Session.LocalPlayerReady)
		{
			for (int i = 0; i < this.CompetitorsTable.GetChildren().Count; i++)
			{
				if (this.CompetitorsTable.GetChildren()[i].GetComponent<MenuCompetitorSlot>().EmpireIndex == this.GetPlayerEmpireIndex())
				{
					this.CompetitorsTable.GetChildren()[i].GetComponent<MenuCompetitorSlot>().PlayerReadyToggles[0].State = this.Session.LocalPlayerReady;
				}
			}
		}
	}

	private void OnSessionNameChangedCB(GameObject gameObject)
	{
		this.StartProfanityFiltering();
	}

	private void RefreshButtons()
	{
		if (this.Session == null)
		{
			this.StartButton.AgeTransform.Visible = true;
			this.StartReadyToggle.AgeTransform.Visible = false;
			this.CancelButton.AgeTransform.Enable = false;
			this.StartButton.AgeTransform.Enable = false;
			this.StartReadyToggle.AgeTransform.Enable = false;
			return;
		}
		bool flag = this.Session.SessionMode != SessionMode.Single;
		this.StartButton.AgeTransform.Visible = !flag;
		this.StartReadyToggle.AgeTransform.Visible = flag;
		if (flag)
		{
			this.StartReadyLabel.Text = AgeLocalizer.Instance.LocalizeString("%ReadyGameTitle");
		}
		this.CancelButton.AgeTransform.Enable = !this.guiLocked;
		this.StartButton.AgeTransform.Enable = !this.guiLocked;
		this.StartReadyToggle.AgeTransform.Enable = !this.guiLocked;
		if (this.profanityError != string.Empty)
		{
			this.StartButton.AgeTransform.Enable = false;
			this.StartReadyToggle.AgeTransform.Enable = false;
			this.StartButton.AgeTransform.AgeTooltip.Content = "%Failure" + this.profanityError + "Description";
			this.StartReadyToggle.AgeTransform.AgeTooltip.Content = "%Failure" + this.profanityError + "Description";
		}
	}

	private void RefreshChatWindowsVisibility()
	{
		if (this.Session == null)
		{
			this.OutGameConsolePanel.Hide(true);
			return;
		}
		if (this.OutGameConsolePanel.IsVisible && this.Session.SessionMode == SessionMode.Single)
		{
			this.OutGameConsolePanel.Hide(true);
		}
		bool flag = this.Session.SessionMode == SessionMode.Private || this.Session.SessionMode == SessionMode.Protected || this.Session.SessionMode == SessionMode.Public;
		if (!this.OutGameConsolePanel.IsVisible && flag)
		{
			this.OutGameConsolePanel.Show(new object[]
			{
				base.gameObject
			});
		}
	}

	private void RefreshClientDownloadableContents()
	{
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
		this.ClientDlcIconEnumerator.AgeTransform.PixelMarginLeft = this.VersionLabel.AgeTransform.PixelMarginLeft + num + 4f;
		this.ClientDlcIconEnumerator.RefreshContent();
		parent.Width += (float)this.ClientDlcIconEnumerator.AgeTransform.GetChildren().Count * 16f;
	}

	private void RefreshEmpireInformation()
	{
		if (this.Session == null)
		{
			return;
		}
		int playerEmpireIndex = this.GetPlayerEmpireIndex();
		if (playerEmpireIndex < 0)
		{
			return;
		}
		Faction faction = this.GetSelectedFaction(playerEmpireIndex);
		if (faction != null)
		{
			IDatabase<Faction> database = Databases.GetDatabase<Faction>(false);
			if (database != null && database.ContainsKey(faction.Name))
			{
				faction = database.GetValue(faction.Name);
			}
			this.FactionGuiDescription.Bind(faction, this.HasGameBeenLaunchedOnce);
			this.FactionGuiDescription.RefreshContent();
		}
		this.MoodAdvancedFactionButton.AgeTransform.Enable = this.CanModifyOwnEmpireSettings;
		this.SelectFactionButton.AgeTransform.Enable = this.CanModifyOwnEmpireSettings;
	}

	private void RefreshGameOptions()
	{
		if (this.Session == null)
		{
			return;
		}
		List<MenuSetting> children = this.GameStandardSettingsTable.GetChildren<MenuSetting>(true);
		for (int i = 0; i < children.Count; i++)
		{
			children[i].RefreshContent(this.Session, !this.CanModifyGameSettings);
		}
		this.AdvancedGameSettingsButton.AgeTransform.Enable = !this.guiLocked;
	}

	private void RefreshWorldGeneratorOptions()
	{
		if (this.Session == null)
		{
			return;
		}
		List<MenuSetting> children = this.WorldStandardSettingsTable.GetChildren<MenuSetting>(true);
		for (int i = 0; i < children.Count; i++)
		{
			children[i].RefreshContent(this.Session, !this.CanModifyGameSettings);
		}
		this.AdvancedWorldSettingsButton.AgeTransform.Enable = !this.guiLocked;
	}

	private void RefreshSessionPanel()
	{
		if (this.Session == null)
		{
			this.InviteGroup.Visible = false;
			return;
		}
		if (!Amplitude.Unity.Framework.Application.Preferences.EnableMultiplayer)
		{
			this.SessionTypeDropList.SelectedItem = this.sessionModeNames.IndexOf(SessionMode.Single.ToString());
		}
		else
		{
			string lobbyData = this.Session.GetLobbyData<string>("SessionMode", null);
			if (!string.IsNullOrEmpty(lobbyData))
			{
				this.SessionTypeDropList.SelectedItem = this.sessionModeNames.IndexOf(lobbyData);
			}
		}
		this.SessionTypeDropList.ReadOnly = (!this.Session.IsHosting || this.guiLocked || !Amplitude.Unity.Framework.Application.Preferences.EnableMultiplayer);
		this.SessionNameGroup.Visible = (this.Session.SessionMode != SessionMode.Single);
		if (this.DlcFrame.Visible)
		{
			this.DlcIconEnumerator.RefreshContent();
		}
		if (this.SessionNameTextField != AgeManager.Instance.FocusedControl)
		{
			string lobbyData2 = this.Session.GetLobbyData<string>("name", null);
			if (!string.IsNullOrEmpty(lobbyData2))
			{
				this.SessionNameTextField.ReplaceInputText(lobbyData2);
			}
		}
		this.SessionNameTextField.ReadOnly = (!this.Session.IsHosting || this.guiLocked);
		this.InviteGroup.Visible = (this.Session.SessionMode != SessionMode.Single);
		this.InviteButton.AgeTransform.Enable = this.Session.IsHosting;
	}

	private void RefreshScenarioInformation()
	{
		if (this.Session == null)
		{
			return;
		}
		WorldGeneratorScenarioDefinition worldGeneratorScenarioDefinition = null;
		this.GenerationScenarioDropList.SelectedItem = 0;
		string lobbyData = this.Session.GetLobbyData<string>("Scenario", null);
		if (!string.IsNullOrEmpty(lobbyData) && this.scenarios != null)
		{
			for (int i = 0; i < this.scenarios.Count; i++)
			{
				if (this.scenarios[i] != null && this.scenarios[i].Name == lobbyData)
				{
					worldGeneratorScenarioDefinition = this.scenarios[i];
					this.GenerationScenarioDropList.SelectedItem = i;
					break;
				}
			}
		}
		WorldGeneratorScenarioDefinition worldGeneratorScenarioDefinition2 = WorldGeneratorScenarioDefinition.Current;
		bool flag;
		if (worldGeneratorScenarioDefinition != null)
		{
			flag = WorldGeneratorScenarioDefinition.Select(worldGeneratorScenarioDefinition);
		}
		else
		{
			flag = WorldGeneratorScenarioDefinition.Select(false);
		}
		if (flag && worldGeneratorScenarioDefinition2 != WorldGeneratorScenarioDefinition.Current)
		{
			base.NeedRefresh = true;
		}
		this.GenerationScenarioDropList.AgeTransform.Enable = this.CanModifyGameSettings;
		if (WorldGeneratorScenarioDefinition.Current == null)
		{
			this.GenerationScenarioDescription.Text = AgeLocalizer.Instance.LocalizeString("%WorldGeneratorScenarioNoneDescription");
			this.GenerationScenarioImage.Image = null;
			GuiElement guiElement;
			Texture2D image;
			if (base.GuiService.GuiPanelHelper.TryGetGuiElement("NewGameMenuWorldGeneratorScenarioSummary", out guiElement) && base.GuiService.GuiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Large, out image))
			{
				this.GenerationScenarioImage.Image = image;
			}
		}
		else
		{
			string x = "%WorldGeneratorScenario" + worldGeneratorScenarioDefinition.Name;
			IDatabase<GuiElement> database = Databases.GetDatabase<GuiElement>(true);
			GuiElement guiElement2;
			if (database != null && database.TryGetValue(x, out guiElement2))
			{
				this.GenerationScenarioDescription.Text = AgeLocalizer.Instance.LocalizeString(guiElement2.Name + "Content");
				Texture2D image2 = null;
				if (base.GuiService.GuiPanelHelper.TryGetTextureFromIcon(guiElement2, global::GuiPanel.IconSize.Large, out image2))
				{
					this.GenerationScenarioImage.Image = image2;
				}
			}
			else
			{
				this.GenerationScenarioDescription.Text = "%WorldGeneratorScenarioNoGuiElementTitle";
				this.GenerationScenarioImage.Image = null;
			}
		}
	}

	private void RefreshCompetitorSlots()
	{
		if (this.Session == null)
		{
			this.CompetitorsTable.DestroyAllChildren();
			return;
		}
		this.UnbindCompetitorSlots();
		int numberOfSlots = this.GetNumberOfSlots();
		this.CompetitorsTable.ReserveChildren(numberOfSlots, this.MenuCompetitorSlotPrefab, "Item");
		List<int> list = new List<int>(numberOfSlots);
		for (int i = 0; i < numberOfSlots; i++)
		{
			list.Add(i);
		}
		this.CompetitorsTable.RefreshChildrenIList<int>(list, this.setupCompetitorSlotDelegate, true, false);
	}

	private void UnbindCompetitorSlots()
	{
		for (int i = 0; i < this.CompetitorsTable.GetChildren().Count; i++)
		{
			this.CompetitorsTable.GetChildren()[i].GetComponent<MenuCompetitorSlot>().Unbind();
		}
	}

	private void Session_LobbyChatMessage(object sender, LobbyChatMessageEventArgs e)
	{
		if (this.Session == null || string.IsNullOrEmpty(e.Message))
		{
			return;
		}
		if (e.Message.StartsWith("c:/"))
		{
			string friendPersonaName = Steamworks.SteamAPI.SteamFriends.GetFriendPersonaName(e.SteamIDUser);
			string format = AgeLocalizer.Instance.LocalizeString("%LobbyChatPlayerMessageFormat");
			string content = string.Format(format, friendPersonaName, e.Message.Substring(3));
			this.OutGameConsolePanel.AddLine(content, this.OutGameConsolePanel.PlayerChatColor);
			IAudioEventService service = Services.GetService<IAudioEventService>();
			service.Play2DEvent("Gui/Interface/Lobby/MessageIncoming");
		}
		else if (e.Message.StartsWith("s:/"))
		{
			this.OutGameConsolePanel.AddLine(e.Message.Substring(3), this.OutGameConsolePanel.AnnouncementColor);
		}
		else if (e.Message.StartsWith("r:/"))
		{
			try
			{
				string[] array = e.Message.Substring(3).Split(Amplitude.String.ExtendedSeparators);
				if (this.Session.SteamIDUser.ToString() == array[0])
				{
					Diagnostics.LogWarning("Got a response (message: '{0}') from the lobby owner concerning an invalid query; refreshing on latest lobby data...", new object[]
					{
						e.Message
					});
					base.NeedRefresh = true;
				}
			}
			catch
			{
			}
		}
		else if (e.Message.StartsWith("k:/"))
		{
			string[] array2 = e.Message.Substring(3).Split(Amplitude.String.ExtendedSeparators);
			if (this.Session.SteamIDUser.ToString() == array2[0])
			{
				string text = string.Format(AgeLocalizer.Instance.LocalizeString("%KickedFromLobby"), AgeLocalizer.Instance.LocalizeString(array2[1]));
				Diagnostics.LogWarning("[Lobby]" + text);
				MessagePanel.Instance.Show(text, "Kicked", MessagePanelButtons.Ok, null, MessagePanelType.WARNING, new MessagePanelButton[0]);
				this.HandleCancelRequest();
			}
		}
	}

	private void XGPKickEventHandler(object sender, MessagePanelResultEventArgs e)
	{
		Amplitude.Unity.Framework.Application.Quit();
	}

	private void Session_LobbyDataChange(object sender, LobbyDataChangeEventArgs e)
	{
		base.NeedRefresh = true;
		if (!this.HasGameBeenLaunchedOnce && !this.triedToSelectPreferredColor)
		{
			int playerEmpireIndex = this.GetPlayerEmpireIndex();
			if (playerEmpireIndex >= 0)
			{
				this.triedToSelectPreferredColor = true;
				string lobbyData = this.Session.GetLobbyData<string>("Color" + playerEmpireIndex, null);
				string value = Amplitude.Unity.Framework.Application.Registry.GetValue<string>("Preferences/Lobby/Color", "0");
				if (lobbyData != value)
				{
					string message = string.Format("q:/Color{0}/{1}", playerEmpireIndex, value);
					this.Session.SendLobbyChatMessage(message);
				}
			}
		}
		if (e.Key == "_Launching" || e.Key == "_GameInProgress" || e.Key == "_IsSavedGame")
		{
			this.SetBreadcrumb(base.GuiService.GetGuiPanel<MenuMainScreen>().MenuBreadcrumb.Text);
		}
	}

	private void Session_LobbyMemberDataChange(object sender, LobbyMemberDataChangeEventArgs e)
	{
		base.NeedRefresh = true;
	}

	private void SessionService_SessionChange(object sender, SessionChangeEventArgs e)
	{
		switch (e.Action)
		{
		case SessionChangeAction.Releasing:
		case SessionChangeAction.Released:
			this.Session = null;
			break;
		case SessionChangeAction.Opened:
			this.Session = (e.Session as global::Session);
			this.RefreshChatWindowsVisibility();
			break;
		case SessionChangeAction.Closed:
			this.Session = null;
			if (base.IsShowing)
			{
				base.StartCoroutine(this.HandleCancelRequestWhenShowingFinished());
			}
			else
			{
				base.SetupDepthDown();
				IRuntimeService service = Services.GetService<IRuntimeService>();
				if (service != null)
				{
					service.Runtime.FiniteStateMachine.PostStateChange(typeof(RuntimeState_OutGame), new object[0]);
				}
			}
			break;
		case SessionChangeAction.Reopened:
			this.RefreshChatWindowsVisibility();
			break;
		}
	}

	private void SessionState_OnLockLobbyGUI(bool locked)
	{
		this.guiLocked = locked;
		base.NeedRefresh = true;
	}

	private void ISteamClientService_ClientPersonaStateChanged(object sender, PersonaStateChangedEventArgs e)
	{
		if ((e.Message.m_nChangeFlags & 1) > 0)
		{
			this.RefreshCompetitorSlots();
		}
	}

	private void OnChangeStandardOption(object[] optionAndItem)
	{
		Diagnostics.Assert(optionAndItem.Length == 2);
		OptionDefinition optionDefinition = optionAndItem[0] as OptionDefinition;
		Diagnostics.Assert(optionDefinition != null);
		OptionDefinition.ItemDefinition itemDefinition = optionAndItem[1] as OptionDefinition.ItemDefinition;
		Diagnostics.Assert(itemDefinition != null);
		if (this.Session != null)
		{
			this.Session.SetLobbyData(optionDefinition.Name, itemDefinition.Name.ToString(), true);
			Amplitude.Unity.Framework.Application.Registry.SetValue(optionDefinition.RegistryPath, itemDefinition.Name.ToString());
			List<MenuSetting> children = this.GameStandardSettingsTable.GetChildren<MenuSetting>(true);
			for (int i = 0; i < children.Count; i++)
			{
				if (children[i].OptionDefinition.EnableOn == optionDefinition.Name)
				{
					children[i].OnDependencyChanged(optionDefinition, itemDefinition);
				}
			}
			if (itemDefinition.OptionDefinitionConstraints != null)
			{
				foreach (OptionDefinitionConstraint optionDefinitionConstraint in from element in itemDefinition.OptionDefinitionConstraints
				where element.Type == OptionDefinitionConstraintType.Control
				select element)
				{
					if (optionDefinitionConstraint.Keys != null && optionDefinitionConstraint.Keys.Length != 0)
					{
						string lobbyData = this.Session.GetLobbyData<string>(optionDefinitionConstraint.OptionName, null);
						if (!string.IsNullOrEmpty(lobbyData))
						{
							if (optionDefinitionConstraint.Keys.Select((OptionDefinitionConstraint.Key key) => key.Name).Contains(lobbyData))
							{
								continue;
							}
						}
						this.Session.SetLobbyData(optionDefinitionConstraint.OptionName, optionDefinitionConstraint.Keys[0].Name.ToString(), true);
					}
				}
			}
		}
	}

	private void OnSessionNameFocusLostCB(GameObject obj)
	{
		if (this.Session != null && this.Session.IsHosting)
		{
			string text = this.SessionNameLabel.Text.Trim();
			if (!string.IsNullOrEmpty(text))
			{
				this.Session.SetLobbyData("name", text, true);
			}
			else
			{
				this.SessionNameTextField.ReplaceInputText(this.Session.GetLobbyData<string>("name", null));
			}
		}
	}

	private void OnValidateSessionNameCB(GameObject obj)
	{
		AgeManager.Instance.FocusedControl = null;
	}

	private void OnInviteCB(GameObject obj)
	{
		Steamworks.SteamAPI.SteamFriends.ActivateGameOverlayInviteDialog(this.Session.SteamIDLobby);
	}

	private void SetupStandardOption(AgeTransform tableItem, OptionDefinition optionDefinition, int index)
	{
		if (tableItem == null)
		{
			throw new InvalidOperationException();
		}
		MenuSetting component = tableItem.GetComponent<MenuSetting>();
		if (component != null)
		{
			component.InitializeContent(optionDefinition, base.gameObject);
		}
	}

	private void SetupCompetitorSlot(AgeTransform tableItem, int title, int index)
	{
		MenuCompetitorSlot component = tableItem.GetComponent<MenuCompetitorSlot>();
		if (component != null)
		{
			component.Bind(this.Session, index, this);
			component.RefreshContent(this.guiFactions, this.colorsList, this.HasGameBeenLaunchedOnce, this.CanModifyOwnEmpireSettings, this.guiLocked);
		}
	}

	private string profanityError;

	private UnityEngine.Coroutine profanityFilterCoroutine;

	private Color invalidColor;

	public static Faction SelectedFaction;

	public Transform MenuStandardSettingPrefab;

	public Transform MenuCompetitorSlotPrefab;

	public AgeTransform GameStandardSettingsTable;

	public AgePrimitiveImage GameSettingsSummaryImage;

	public AgeControlButton AdvancedGameSettingsButton;

	public AgeTransform WorldStandardSettingsTable;

	public AgePrimitiveImage WorldSettingsSummaryImage;

	public AgeControlButton AdvancedWorldSettingsButton;

	public FactionGuiDescription FactionGuiDescription;

	public AgeControlButton MoodAdvancedFactionButton;

	public AgeControlButton SelectFactionButton;

	public AgeTransform GenerationScenarioGroup;

	public AgeControlScrollView GenerationScenarioSW;

	public AgeControlDropList GenerationScenarioDropList;

	public AgePrimitiveImage GenerationScenarioImage;

	public AgePrimitiveLabel GenerationScenarioDescription;

	public AgeControlDropList SessionTypeDropList;

	public AgeTransform SessionNameGroup;

	public AgeControlTextField SessionNameTextField;

	public AgePrimitiveLabel SessionNameLabel;

	public AgeTransform InviteGroup;

	public AgeControlButton InviteButton;

	public AgeTransform DlcFrame;

	public DlcIconEnumerator DlcIconEnumerator;

	public OutGameConsolePanel OutGameConsolePanel;

	public AgeTransform CompetitorsTable;

	public AgeControlButton CancelButton;

	public AgeControlButton StartButton;

	public AgeControlToggle StartReadyToggle;

	public AgePrimitiveLabel StartReadyLabel;

	public AgePrimitiveLabel VersionLabel;

	public DlcIconEnumerator ClientDlcIconEnumerator;

	private List<GuiFaction> guiFactions = new List<GuiFaction>();

	private List<Color> colorsList = new List<Color>();

	private global::Session session;

	private bool guiLocked;

	private bool triedToSelectPreferredColor;

	private ISessionService sessionService;

	private ISteamClientService steamClientService;

	private List<string> sessionModeNames;

	private AgeTransform.RefreshTableItem<OptionDefinition> setupStandardOptionDelegate;

	private AgeTransform.RefreshTableItem<int> setupCompetitorSlotDelegate;

	private List<WorldGeneratorScenarioDefinition> scenarios;
}
