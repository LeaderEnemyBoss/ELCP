using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Amplitude;
using Amplitude.Interop;
using Amplitude.Unity.Audio;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Input;
using Amplitude.Unity.Localization;
using Amplitude.Unity.Messaging;
using Amplitude.Unity.Session;
using Amplitude.Unity.View;
using Amplitude.Unity.Xml;
using Amplitude.Utilities.Maps;
using UnityEngine;

public class CommandManager : Amplitude.Unity.Framework.CommandManager
{
	private string InputLine { get; set; }

	public override IEnumerator BindServices()
	{
		yield return base.BindServices();
		ILocalizationService localizationService = null;
		yield return base.BindService<ILocalizationService>(delegate(ILocalizationService service)
		{
			localizationService = service;
		});
		Diagnostics.Assert(localizationService != null);
		base.RegisterCommand(new Command("/?", "Displays a list of all available commands."), new Func<string[], string>(this.Command_Help));
		base.RegisterCommand(new Command("/SetArmySpeed", "Sets the army speed to a selected multiplier (default: 1)."), new Func<string[], string>(this.Command_SetArmySpeed));
		base.RegisterCommand(new Command("/Quit", "Quits the application and returns to desktop."), new Func<string[], string>(this.Command_Quit));
		base.RegisterCommand(new Command("/WhoAmI", "Displays information about the current user."), new Func<string[], string>(this.Command_WhoAmI));
		base.RegisterCommand(new Command("/Ping", "Displays your latency with the server."), new Func<string[], string>(this.Command_Ping));
		base.RegisterAliasForCommand("/?", "/Help");
		base.RegisterCommand(new Command("/Whisper", localizationService.Localize("%ChatWhisperCommandHelp")), new Func<string[], string>(this.Command_ChatWhisper));
		base.RegisterCommand(new Command("/Empire", localizationService.Localize("%ChatEmpireCommandHelp")), new Func<string[], string>(this.Command_ChatToEmpire));
		base.RegisterAliasForCommand("/Whisper", "/w");
		base.RegisterAliasForCommand("/Empire", "/e");
		if (Amplitude.Unity.Framework.Application.Version.Accessibility <= Accessibility.ProtectedInternal || Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			base.RegisterCommand(new Command("/UpdateNodes", "Tries to bring all Fungal Blooms up to date."), new Func<string[], string>(this.Command_UpdateNodes));
			base.RegisterCommand(new Command("/CheckDLCStatus", "Lists all DLC-Content and if it is active."), new Func<string[], string>(this.Command_CheckDLCStatus));
			base.RegisterCommand(new Command("/ShowMeTheStockpiles", "Orders the transfer of a specific amount of Stockpiles."), new Func<string[], string>(this.Command_ShowMeTheStockpiles));
			base.RegisterCommand(new Command("/Teleport", "Teleports the selected army to the issued coordinates."), new Func<string[], string>(this.Command_Teleport));
			base.RegisterCommand(new Command("/Terraform", "Terraforms the issued coordinates."), new Func<string[], string>(this.Command_Terraform));
			base.RegisterCommand(new Command("/AutoTurn", "Plays some number of turns automatically."), new Func<string[], string>(this.Command_AutoTurn));
			base.RegisterCommand(new Command("/Skynet", "AI will take over for Empire 0 without automatically ending the turn."), new Func<string[], string>(this.Command_Skynet));
			base.RegisterCommand(new Command("/Bind", "Syntax : /Bind [KeyAction] [KeyCode] [bool]. Display bindings with no arguments, Call to bind function with arguments"), new Func<string[], string>(this.Command_Bind));
			base.RegisterCommand(new Command("/BringThePain", "Force the health of selected army units at half the maximum value. (Network non supported)."), new Func<string[], string>(this.Command_BringThePain));
			base.RegisterCommand(new Command("/ForceCurrentQuestsCompletion", "Will complete all the quests in progress (/ForceCurrentQuestsCompletion Failed to fail them all)."), new Func<string[], string>(this.Command_ForceCurrentQuestsCompletion));
			base.RegisterCommand(new Command("/CompleteQuest", "Will complete the currently active quest. (/CompleteQuest Failed to fail the active quest)"), new Func<string[], string>(this.Command_CompleteQuest));
			base.RegisterCommand(new Command("/ForceQuestTriggering", "Force a quest to be triggered."), new Func<string[], string>(this.Command_ForceQuestTriggering));
			base.RegisterCommand(new Command("/ForceUnlockTechnology", "Will unlock a technology without checking any prerequisite."), new Func<string[], string>(this.Command_ForceUnlockTechnology));
			base.RegisterCommand(new Command("/GetRegistryValue", "Get a registry value."), new Func<string[], string>(this.Command_GetRegistryValue));
			base.RegisterCommand(new Command("/IAmACheater", "Unlocks cheat items; 'NoMore' to get the real game back."), new Func<string[], string>(this.Command_IAmACheater));
			base.RegisterCommand(new Command("/INeedAHero", "Orders one hero creation; The Unit profile name could be provided, otherwise, the hero profile will be random."), new Func<string[], string>(this.Command_INeedAHero));
			base.RegisterCommand(new Command("/IPutASpellOnYou", "Capture the select army hero and push it to the second empire jail."), new Func<string[], string>(this.Command_IPutASpellOnYou));
			base.RegisterCommand(new Command("/LetTheSunshineIn", "Change season to summer."), new Func<string[], string>(this.Command_LetTheSunshineIn));
			base.RegisterCommand(new Command("/LightMeUp", "Disables the fog of war; 'NoMore' to get the fog of war back."), new Func<string[], string>(this.Command_LightMeUp));
			base.RegisterCommand(new Command("/LookAt", "Center camera on a specific world position."), new Func<string[], string>(this.Command_LookAt));
			base.RegisterCommand(new Command("/KnowledgeIsPower", "Unlock every technology. +\"all\" to include affinity & quest technologies."), new Func<string[], string>(this.Command_KnowledgeIsPower));
			base.RegisterCommand(new Command("/PowerMeUp", "Give to all of your selected world army's units enough xp to level up."), new Func<string[], string>(this.Command_PowerMeUp));
			base.RegisterCommand(new Command("/ShowMeTheMoney", "Orders the transfer of a specific amount of Dust."), new Func<string[], string>(this.Command_ShowMeTheMoney));
			base.RegisterCommand(new Command("/ShowMeTheResources", "Orders the transfer of a specific amount of Dust, Influence, Science & Growth."), new Func<string[], string>(this.Command_ShowMeTheResources));
			base.RegisterCommand(new Command("/ShowMeTheWay", "Orders the exploration of the whole world."), new Func<string[], string>(this.Command_ShowMeTheWay));
			base.RegisterCommand(new Command("/Slap", "Deal damage (random or given as parameter) to all the units (including hero) in your selected army or city."), new Func<string[], string>(this.Command_Slap));
			base.RegisterCommand(new Command("/TimeToDie", "Destroy the selected army."), new Func<string[], string>(this.Command_TimeToDie));
			base.RegisterCommand(new Command("/TransferResources", "Orders the transfer of a specific amount of resources."), new Func<string[], string>(this.Command_TransferResources));
			base.RegisterCommand(new Command("/WinterIsComing", "Change season to winter."), new Func<string[], string>(this.Command_WinterIsComming));
			base.RegisterCommand(new Command("/WhatIsYoursIsMine", "Toggle Catspaw action on minor/naval armies."), new Func<string[], string>(this.Command_WhatIsYoursIsMine));
			base.RegisterCommand(new Command("/SetWindPreferences", "Set the direction and strength of the wind."), new Func<string[], string>(this.Command_SetWindPreferences));
			base.RegisterCommand(new Command("/SeasonEffect", "Handle season effects."), new Func<string[], string>(this.Command_SeasonEffect));
			base.RegisterCommand(new Command("/Orb", "Give some data concerning orbs."), new Func<string[], string>(this.Command_Orb));
			base.RegisterCommand(new Command("/WeatherControl", "Activate the weather control using a given preset."), new Func<string[], string>(this.Command_WeatherControl));
			base.RegisterCommand(new Command("/BringTheHeat", "Change season to Heat Wave."), new Func<string[], string>(this.Command_BringTheHeat));
			base.RegisterCommand(new Command("/Visions", "Generate season mirages."), new Func<string[], string>(this.Command_Visions));
			base.RegisterCommand(new Command("/TameKaiju", "Tames Kaiju on Army region."), new Func<string[], string>(this.Command_TameKaiju));
			base.RegisterCommand(new Command("/UntameKaiju", "Utames Kaiju on Army region."), new Func<string[], string>(this.Command_UntameKaiju));
			base.RegisterCommand(new Command("/RelocateKaiju", "Triggers relocation of all kaiju in the world."), new Func<string[], string>(this.Command_RelocateKaijus));
			base.RegisterCommand(new Command("/KaijuIChooseYou", "Convert selected Kaiju Garrison to Kaiju Army."), new Func<string[], string>(this.Command_KaijuIChooseYou));
			base.RegisterCommand(new Command("/KaijuComeBack", "Come Back Kaiju Army to Kaiju Garrison."), new Func<string[], string>(this.Command_KaijuComeBack));
			base.RegisterCommand(new Command("/SpawnKaiju", "Summoning the beast!!"), new Func<string[], string>(this.Command_SpawnKaiju));
			base.RegisterCommand(new Command("/AIDebugMode", "Active the AI debug mode."), new Func<string[], string>(this.Command_AIDebugMode));
			base.RegisterCommand(new Command("/SimulationDebugMode", "Active the Simulation debug mode."), new Func<string[], string>(this.Command_SimulationDebugMode));
			base.RegisterCommand(new Command("/side", "Syntax: /side [quest number]. Force a side quest to be triggered in the next interaction / parley."), new Func<string[], string>(this.Command_ForceSideQuestVillageTriggering));
			base.RegisterAliasForCommand("/ShowMeTheMoney", "/TransferMoney");
			base.RegisterAliasForCommand("/TransferResources", "/Transfer");
			base.RegisterCommand(new Command("/ListPlayers", "List the players."), new Func<string[], string>(this.Command_ListPlayers));
			base.RegisterAliasForCommand("/ListPlayers", "/lp");
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				base.RegisterCommand(new Command("/UnitBodyInspector", "Activate the remapping tool."), new Func<string[], string>(this.CommandManager_UnitBodyInspector));
				base.RegisterAliasForCommand("/UnitBodyInspector", "/ShowMeYourBody");
			}
		}
		if (Amplitude.Unity.Framework.Application.Version.Accessibility <= Accessibility.ProtectedInternal)
		{
			base.RegisterCommand(new Command("/CutTheRope", "Disconnect the game client."), new Func<string[], string>(this.Command_DisconnectClient));
			base.RegisterCommand(new Command("/Time", "Show the game time."), new Func<string[], string>(this.Command_GameTime));
			base.RegisterCommand(new Command("/WorldViewStatistics", "Display the world view statistics."), new Func<string[], string>(this.Command_WorldViewStatistics));
			base.RegisterCommand(new Command("/GetLobbyData", "Gets a lobby data value."), new Func<string[], string>(this.Command_GetLobbyData));
			base.RegisterAliasForCommand("/CutTheRope", "/Disc");
		}
		if (Amplitude.Unity.Framework.Application.Version.Accessibility <= Accessibility.Internal)
		{
			base.RegisterCommand(new Command("/Dump", "Dumps the game content."), new Func<string[], string>(this.Command_Dump));
			base.RegisterCommand(new Command("/ToggleVisibility", "Toggle visibility of render element."), new Func<string[], string>(this.Command_ToggleVisibility));
			base.RegisterCommand(new Command("/LoadDynamicBitmaps", "Loads all dynamic available bitmaps."), new Func<string[], string>(this.Command_LoadDynamicBitmaps));
			base.RegisterCommand(new Command("/EpicMusicTime", "Orders the lauch of a faction music."), new Func<string[], string>(this.Command_EpicMusicTime));
			base.RegisterCommand(new Command("/EndlessDay", "Toggles the endless day status."), new Func<string[], string>(this.Command_ToggleEndlessDay));
			base.RegisterCommand(new Command("/AutoTurn", "Plays some number of turns automatically."), new Func<string[], string>(this.Command_AutoTurn));
			base.RegisterCommand(new Command("/GCCollect", "Forces a full garbage collection to free some memory."), new Func<string[], string>(this.CommandManager_GCCollect));
			base.RegisterCommand(new Command("/UnloadUnusedAssets", "Unloads all unused assets in Unity."), new Func<string[], string>(this.CommandManager_UnloadUnusedAssets));
		}
		yield break;
	}

	protected override void Awake()
	{
		base.Awake();
		this.MatchCase = false;
	}

	protected override void OnOutput(CommandOutputEventArgs e)
	{
		base.OnOutput(e);
		Diagnostics.Log(e.Output);
	}

	private string Command_AIDebugMode(string[] commandLineArgs)
	{
		bool flag;
		if (commandLineArgs.Length == 1)
		{
			flag = true;
		}
		else
		{
			if (commandLineArgs.Length > 2)
			{
				return string.Format("Excepted format: {0} [On|Off]", commandLineArgs[0]);
			}
			string text = commandLineArgs[1];
			if (text == "help")
			{
				return string.Format("Excepted format: {0} [On|Off]", commandLineArgs[0]);
			}
			if (text.ToLower() == "on")
			{
				flag = true;
			}
			else if (text.ToLower() == "off")
			{
				flag = false;
			}
			else
			{
				if (!(text.ToLower() == "nomore"))
				{
					return string.Format("Excepted format: {0} [On|Off]", commandLineArgs[0]);
				}
				flag = false;
			}
		}
		if (flag)
		{
			if (!(this.debugModeObject == null))
			{
				return "The AI debug mode is already started.";
			}
			GameObject gameObject = (GameObject)Resources.Load("Prefabs/Debug/AIDebugMode");
			if (gameObject == null)
			{
				return "[Error] Failed to retrieve the AI debug mode prefab.";
			}
			this.debugModeObject = UnityEngine.Object.Instantiate<GameObject>(gameObject);
			return "Succeed to start the AI debug mode.";
		}
		else
		{
			if (this.debugModeObject != null)
			{
				AIDebugMode component = this.debugModeObject.GetComponent<AIDebugMode>();
				if (component != null)
				{
					component.Release();
				}
				UnityEngine.Object.Destroy(this.debugModeObject);
				return "Succeed to stop the AI debug mode.";
			}
			return "The AI debug mode is already stoped.";
		}
	}

	private string Command_AutoTurn(string[] commandLineArgs)
	{
		ISessionService service = Services.GetService<ISessionService>();
		if (service == null || service.Session == null)
		{
			return "This command requires a game session to be running.";
		}
		if (service.Session.SessionMode != SessionMode.Single)
		{
			if (!(service.Session is global::Session))
			{
				return "Invalid session.";
			}
			return "This command requires the session to run in single player mode.";
		}
		else
		{
			int num = 10;
			if (commandLineArgs.Length > 1)
			{
				int.TryParse(commandLineArgs[1], out num);
			}
			double num2 = 10.0;
			if (commandLineArgs.Length > 2)
			{
				double.TryParse(commandLineArgs[2], out num2);
			}
			if (num <= 0)
			{
				return "Invalid number of turns.";
			}
			this.needToStop = false;
			base.StartCoroutine(this.Command_AutoTurnAsync(num, num2));
			return string.Concat(new object[]
			{
				"Running for ",
				num,
				" turn(s) with a delay of ",
				num2,
				" seconds."
			});
		}
	}

	private IEnumerator Command_AutoTurnAsync(int numberOfTurns = 10, double delay = 10.0)
	{
		ISessionService service = Services.GetService<ISessionService>();
		if (service == null || service.Session == null)
		{
			Diagnostics.LogError("Cannot find the running session.");
			yield break;
		}
		global::Session session = service.Session as global::Session;
		if (session == null)
		{
			Diagnostics.LogError("Invalid session.");
			yield break;
		}
		IEndTurnControl endTurnControl = Services.GetService<IEndTurnService>() as IEndTurnControl;
		if (endTurnControl == null)
		{
			Diagnostics.LogWarning("Cannot find the 'IEndTurnService' service.");
			yield break;
		}
		GameServer gameServer = session.GameServer as GameServer;
		if (gameServer == null)
		{
			Diagnostics.LogError("Invalid game server.");
			yield break;
		}
		MajorEmpire empire = session.GameServer.Game.Empires[0] as MajorEmpire;
		Diagnostics.Assert(empire != null);
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (!gameServer.AIScheduler.TryGetMajorEmpireAIPlayer(empire, out aiplayer_MajorEmpire))
		{
			Diagnostics.LogError("Can't get the AI player state.");
			yield break;
		}
		AIPlayer.PlayerState backupPlayerState = aiplayer_MajorEmpire.AIState;
		gameServer.AIScheduler.ChangeMajorEmpireAIState(empire, AIPlayer.PlayerState.EmpireControlledByAI);
		StaticString keyLobbyDataEmpire = string.Format("Empire{0}", empire.Index);
		string lobbyDataEmpire0 = session.GetLobbyData<string>(keyLobbyDataEmpire, null);
		session.SetLobbyData(keyLobbyDataEmpire, "AI" + lobbyDataEmpire0, true);
		aiplayer_MajorEmpire.Start();
		int counter = 0;
		DateTime timestamp = DateTime.Now;
		Diagnostics.AssertionFailed += this.Diagnostics_AssertionFailed;
		Diagnostics.MessageLogged += this.Diagnostics_MessageLogged;
		while (counter <= numberOfTurns)
		{
			yield return null;
			if (endTurnControl.LastGameClientState is GameClientState_Turn_Main)
			{
				if (timestamp == DateTime.MaxValue)
				{
					timestamp = DateTime.Now;
					continue;
				}
				if (delay > 0.0 && (DateTime.Now - timestamp).TotalSeconds < delay)
				{
					continue;
				}
			}
			if (endTurnControl.EndTurn())
			{
				int num = counter;
				counter = num + 1;
				timestamp = DateTime.MaxValue;
			}
		}
		Diagnostics.AssertionFailed -= this.Diagnostics_AssertionFailed;
		Diagnostics.MessageLogged -= this.Diagnostics_MessageLogged;
		session.SetLobbyData(keyLobbyDataEmpire, lobbyDataEmpire0, true);
		gameServer.AIScheduler.ChangeMajorEmpireAIState(empire, backupPlayerState);
		yield break;
	}

	private void Diagnostics_AssertionFailed(Diagnostics.LogMessage message)
	{
		this.needToStop = true;
	}

	private void Diagnostics_MessageLogged(Diagnostics.LogMessage message)
	{
		Diagnostics.LogType logType = (Diagnostics.LogType)((long)message.Flags & (long)((ulong)-16777216));
		if (logType != Diagnostics.LogType.Message && logType != Diagnostics.LogType.Warning)
		{
			this.needToStop = true;
		}
	}

	private string Command_Bind(string[] commandLineArgs)
	{
		IKeyMappingService service = Services.GetService<IKeyMappingService>();
		if (service != null)
		{
			if (commandLineArgs.Length < 3)
			{
				return service.CurrentMapping.ToString();
			}
			if (commandLineArgs.Length != 4)
			{
				if (commandLineArgs.Length != 5)
				{
					goto IL_FC;
				}
			}
			try
			{
				KeyAction index = (KeyAction)((int)Enum.Parse(typeof(KeyAction), "ControlBindings" + commandLineArgs[1], true));
				KeyCode keyCode = (KeyCode)((int)Enum.Parse(typeof(KeyCode), commandLineArgs[2], true));
				KeyCode modifier;
				bool isPrimaryKey;
				if (commandLineArgs.Length == 5)
				{
					modifier = (KeyCode)((int)Enum.Parse(typeof(KeyCode), commandLineArgs[3], true));
					isPrimaryKey = bool.Parse(commandLineArgs[4]);
				}
				else
				{
					modifier = KeyCode.None;
					isPrimaryKey = bool.Parse(commandLineArgs[3]);
				}
				if (keyCode != KeyCode.None)
				{
					KeyBindingResult keyBindingResult = service.Bind(index, new KeyConfig(keyCode, modifier), isPrimaryKey);
					return "Bind finish with return code " + keyBindingResult.ToString();
				}
			}
			catch
			{
				return "One of the arguments is incorrect or missing.";
			}
		}
		IL_FC:
		return string.Empty;
	}

	private string Command_BringTheHeat(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service == null || service.Game == null)
		{
			return string.Empty;
		}
		ISeasonService service2 = service.Game.Services.GetService<ISeasonService>();
		if (service2 == null)
		{
			return string.Empty;
		}
		ISessionService service3 = Services.GetService<ISessionService>();
		if (service3 == null || service3.Session == null || !service3.Session.IsOpened || !service3.Session.IsHosting)
		{
			return "You need to host the session in order to bring the heat.";
		}
		Season currentSeason = service2.GetCurrentSeason();
		if (currentSeason.SeasonDefinition.SeasonType == Season.ReadOnlyHeatWave)
		{
			return "Heat Wave is already there.";
		}
		service2.ForceCurrentSeason(Season.ReadOnlyHeatWave);
		return "Feel the heat.";
	}

	private string Command_BringThePain(string[] commandLineArgs)
	{
		ICursorService service = Services.GetService<ICursorService>();
		if (service != null && service.CurrentCursor is ArmyWorldCursor)
		{
			ArmyWorldCursor armyWorldCursor = service.CurrentCursor as ArmyWorldCursor;
			if (armyWorldCursor.Army != null && armyWorldCursor.Army.Units != null)
			{
				foreach (Unit unit in armyWorldCursor.Army.Units)
				{
					float propertyValue = unit.GetPropertyValue(SimulationProperties.MaximumHealth);
					float value = propertyValue / 2f;
					unit.SetPropertyBaseValue(SimulationProperties.Health, value);
				}
				armyWorldCursor.Army.Refresh(false);
				return "Half dead or half alive, that's up to you.";
			}
		}
		return "You need to select a world army for this command to work.";
	}

	private string Command_ChatToEmpire(string[] commandLineArgs)
	{
		if (commandLineArgs.Length < 3)
		{
			return string.Format("#FF0000#{0}#REVERT#", AgeLocalizer.Instance.LocalizeString("%ChatEmpireCommandHelp"));
		}
		ISessionService service = Services.GetService<ISessionService>();
		Diagnostics.Assert(service != null, "ISessionService is null.");
		IGameService service2 = Services.GetService<IGameService>();
		if (service2 != null && service2.Game != null)
		{
			IPlayerRepositoryService service3 = service2.Game.Services.GetService<IPlayerRepositoryService>();
			Diagnostics.Assert(service3 != null);
			Player player = null;
			int num = this.CheckPlayer(commandLineArgs, service3, out player);
			if (player == null)
			{
				return string.Format("#FF0000#{0}#REVERT#", AgeLocalizer.Instance.LocalizeString("%ChatPlayerNotFound"));
			}
			if (player.Type != PlayerType.Human)
			{
				return string.Format("#FF0000#{0}#REVERT#", AgeLocalizer.Instance.LocalizeString("%ChatCannotTalkToEmpire").Replace("$PlayerName", player.LocalizedName));
			}
			Steamworks.SteamID steamID = player.SteamID;
			string command = string.Join(" ", commandLineArgs, num + 1, commandLineArgs.Length - num - 1);
			Message message = new GameClientChatMessage(ChatMessageType.Empire, command)
			{
				Recipent = steamID
			};
			(service.Session as global::Session).GameClient.SendMessageToServer(ref message);
		}
		return string.Empty;
	}

	private string Command_ChatWhisper(string[] commandLineArgs)
	{
		if (commandLineArgs.Length < 3)
		{
			return string.Format("#FF0000#{0}#REVERT#", AgeLocalizer.Instance.LocalizeString("%ChatWhisperCommandHelp"));
		}
		ISessionService service = Services.GetService<ISessionService>();
		Diagnostics.Assert(service != null, "ISessionService is null.");
		IGameService service2 = Services.GetService<IGameService>();
		if (service2 != null && service2.Game != null)
		{
			IPlayerRepositoryService service3 = service2.Game.Services.GetService<IPlayerRepositoryService>();
			Diagnostics.Assert(service3 != null);
			Player player = null;
			int num = this.CheckPlayer(commandLineArgs, service3, out player);
			if (player == null)
			{
				return string.Format("#FF0000#{0}#REVERT#", AgeLocalizer.Instance.LocalizeString("%ChatPlayerNotFound"));
			}
			if (player.Type != PlayerType.Human)
			{
				return string.Format("#FF0000#{0}#REVERT#", AgeLocalizer.Instance.LocalizeString("%ChatCannotTalkToPlayer").Replace("$PlayerName", player.LocalizedName));
			}
			Steamworks.SteamID steamID = player.SteamID;
			string command = string.Join(" ", commandLineArgs, num + 1, commandLineArgs.Length - num - 1);
			Message message = new GameClientChatMessage(ChatMessageType.Player, command)
			{
				Recipent = steamID
			};
			(service.Session as global::Session).GameClient.SendMessageToServer(ref message);
		}
		return string.Empty;
	}

	private int CheckPlayer(string[] commandLineArgs, IPlayerRepositoryService playerRepositoryService, out Player player)
	{
		int num = 1;
		string playerName = commandLineArgs[1];
		do
		{
			playerName = string.Join(" ", commandLineArgs, 1, num);
			playerName = playerName.Replace('_', ' ');
			player = playerRepositoryService.FirstOrDefault((Player p) => string.Compare(p.LocalizedName, playerName, StringComparison.OrdinalIgnoreCase) == 0);
		}
		while (player == null && num++ < commandLineArgs.Length - 2);
		return num;
	}

	private string Command_DisconnectClient(string[] commandLineArgs)
	{
		string text = string.Format("Disconnecting with flag {0} (0x{1:x2}).", GameDisconnectionReason._Debug, 255);
		Diagnostics.LogWarning("[Command][Net] " + text);
		ISessionService service = Services.GetService<ISessionService>();
		Diagnostics.Assert(service != null);
		global::Session session = service.Session as global::Session;
		Diagnostics.Assert(session != null);
		session.GameClient.Disconnect(GameDisconnectionReason._Debug, 0);
		return text;
	}

	private string Command_Dump(string[] commandLineArgs)
	{
		if (commandLineArgs.Length != 2)
		{
			return "Use: /Dump txt|md5|test|clear|reg";
		}
		IGameDiagnosticsService service = Services.GetService<IGameDiagnosticsService>();
		Diagnostics.Assert(service != null);
		try
		{
			if (commandLineArgs[1] == "txt")
			{
				return "0x" + service.Dump("Command");
			}
			if (commandLineArgs[1] == "md5")
			{
				return "0x" + service.ComputeChecksum();
			}
			if (commandLineArgs[1] == "test")
			{
				service.InjectTestDesync = !service.InjectTestDesync;
				return "Test mode is " + ((!service.InjectTestDesync) ? "#FF0000#OFF#REVERT#" : "#00FF00#ON#REVERT#");
			}
			if (commandLineArgs[1] == "clear")
			{
				return service.ClearDumpFiles() + " files removed.";
			}
			if (commandLineArgs[1] == "reg")
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendFormat("Registry://Diagnostics/DumpingMethod -> {0}\n", Amplitude.Unity.Framework.Application.Registry.GetValue<string>("Diagnostics/DumpingMethod", "Binary (default)"));
				stringBuilder.AppendFormat("Registry://Diagnostics/OpenDesyncReport -> {0}\n", Amplitude.Unity.Framework.Application.Registry.GetValue<string>("Diagnostics/OpenDesyncReport", "False (default)"));
				stringBuilder.AppendFormat("Registry://Diagnostics/MergeToolPath -> {0}\n", Amplitude.Unity.Framework.Application.Registry.GetValue<string>("Diagnostics/MergeToolPath", "(default)").Replace('\\', '/'));
				stringBuilder.AppendFormat("Registry://Diagnostics/MergeArgsFormat -> {0}\n", Amplitude.Unity.Framework.Application.Registry.GetValue<string>("Diagnostics/MergeArgsFormat", "\"{0}\" \"{1}\" (default)").Replace('\\', '/'));
				return stringBuilder.ToString();
			}
		}
		catch (Exception ex)
		{
			Diagnostics.LogError("Exception raised while dumping the game: {0}\n{1}", new object[]
			{
				ex.Message,
				ex.StackTrace
			});
		}
		return "Unknown argument. Use: /Dump txt|md5|test|clear|reg";
	}

	private string Command_EpicMusicTime(string[] commandLineArgs)
	{
		string text = string.Empty;
		if (commandLineArgs.Length > 1)
		{
			IDatabase<FactionTrait> database = Databases.GetDatabase<FactionTrait>(false);
			Diagnostics.Assert(database != null);
			FactionTrait factionTrait;
			if (database.TryGetValue(string.Format("AffinityMapping{0}", commandLineArgs[1]), out factionTrait))
			{
				text = commandLineArgs[1];
			}
		}
		else
		{
			IGameService service = Services.GetService<IGameService>();
			if (service == null || service.Game == null)
			{
				return string.Empty;
			}
			IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
			if (service2 == null || service2.ActivePlayerController == null)
			{
				return string.Empty;
			}
			global::Empire empire = service2.ActivePlayerController.Empire as global::Empire;
			Diagnostics.Assert(empire != null && empire.Faction != null);
			XmlNamedReference affinityMapping = empire.Faction.AffinityMapping;
			text = affinityMapping.Name.ToString().Replace("AffinityMapping", string.Empty);
		}
		if (string.IsNullOrEmpty(text))
		{
			return string.Format("Can't found the affinity mapping.\nExcepted format: {0} [AffinityMapping]", commandLineArgs[0]);
		}
		Amplitude.Unity.Audio.IAudioLayeredMusicService service3 = Services.GetService<Amplitude.Unity.Audio.IAudioLayeredMusicService>();
		string x = string.Format("FactionMusic{0}", text);
		service3.PlayLayeredMusic(x, x, 5);
		return string.Format("It's an epic moment for the faction {0}.", text);
	}

	private string Command_ForceCurrentQuestsCompletion(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service != null && service.Game != null)
		{
			IQuestManagementService service2 = service.Game.Services.GetService<IQuestManagementService>();
			IPlayerControllerRepositoryService service3 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
			if (service3 != null && service2 != null && service3.ActivePlayerController != null)
			{
				QuestState questState = QuestState.Completed;
				if (commandLineArgs.Length == 2 && commandLineArgs[1] == QuestState.Failed.ToString())
				{
					questState = QuestState.Failed;
				}
				DepartmentOfInternalAffairs agency = service3.ActivePlayerController.Empire.GetAgency<DepartmentOfInternalAffairs>();
				Diagnostics.Assert(agency.QuestJournal != null, "Department has not quest journal.");
				if (agency != null && agency.QuestJournal != null)
				{
					foreach (Quest quest in agency.QuestJournal.Read(QuestState.InProgress))
					{
						if (!quest.QuestDefinition.Tags.Contains("Hidden") && quest.QuestDefinition.GlobalWinner != GlobalQuestWinner.Participants)
						{
							service2.ForceQuestCompletion(quest.GUID, questState);
						}
					}
				}
			}
		}
		return string.Empty;
	}

	private string Command_ForceQuestTriggering(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service == null || service.Game == null || !(service.Game is global::Game))
		{
			return "Failed to retrieve game service";
		}
		IPlayerControllerRepositoryService service2 = (service.Game as global::Game).Services.GetService<IPlayerControllerRepositoryService>();
		IQuestManagementService service3 = service.Game.Services.GetService<IQuestManagementService>();
		Diagnostics.Assert(service2 != null);
		Diagnostics.Assert(service3 != null);
		if (commandLineArgs.Length <= 1 || string.IsNullOrEmpty(commandLineArgs[1]))
		{
			return "You must specify a quest definition name";
		}
		QuestDefinition value = Databases.GetDatabase<QuestDefinition>(false).GetValue(commandLineArgs[1]);
		if (value == null)
		{
			return "Unknown quest: " + commandLineArgs[1];
		}
		return service3.ForceTrigger(value, service2.ActivePlayerController.Empire as global::Empire, commandLineArgs.Length > 2 && commandLineArgs[2].ToLower() == "stopiferror");
	}

	private string Command_ForceUnlockTechnology(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service != null && service.Game != null && commandLineArgs.Length == 2)
		{
			IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
			if (service2 != null && service2.ActivePlayerController != null)
			{
				OrderForceUnlockTechnology order = new OrderForceUnlockTechnology(service2.ActivePlayerController.Empire.Index, commandLineArgs[1]);
				service2.ActivePlayerController.PostOrder(order);
				return commandLineArgs[1] + " unlocked";
			}
		}
		return "The technology name is missing.";
	}

	public static string Format(long number)
	{
		long num = 0L;
		int i;
		for (i = 0; i < global::CommandManager.FormatSeparators.Length; i++)
		{
			if (number < 1024L)
			{
				break;
			}
			num = number % 1024L;
			number /= 1024L;
		}
		i = Math.Min(i, global::CommandManager.FormatSeparators.Length - 1);
		return string.Format("{0}.{1:D3} {2}", number, num, global::CommandManager.FormatSeparators[i]);
	}

	private string CommandManager_GCCollect(string[] commandLineArgs)
	{
		long totalMemory = GC.GetTotalMemory(false);
		GC.Collect();
		long totalMemory2 = GC.GetTotalMemory(true);
		return string.Format("GC collection complete: {0} freed, {1} still allocated.", global::CommandManager.Format(totalMemory - totalMemory2), global::CommandManager.Format(totalMemory2));
	}

	private string Command_GameTime(string[] commandLineArgs)
	{
		return string.Format("GameTime = {0}", global::Game.Time.ToString("#.00"));
	}

	private string Command_GetLobbyData(string[] commandLineArgs)
	{
		if (commandLineArgs.Length != 2)
		{
			return "Use: /GetLobbyData key";
		}
		ISessionService service = Services.GetService<ISessionService>();
		Diagnostics.Assert(service != null);
		return string.Format("'{0}' = [Session]'{1}' [SteamAPI]'{2}'", commandLineArgs[1], service.Session.GetLobbyData<string>(commandLineArgs[1], string.Empty), Steamworks.SteamAPI.SteamMatchMaking.GetLobbyData(service.Session.SteamIDLobby, commandLineArgs[1]));
	}

	private string Command_GetRegistryValue(string[] commandLineArgs)
	{
		if (commandLineArgs.Length != 2)
		{
			return "Use: /GetRegistryValue path";
		}
		return string.Format("{0} = '{1}'", commandLineArgs[1], Amplitude.Unity.Framework.Application.Registry.GetValue(commandLineArgs[1]) ?? "NULL");
	}

	private string Command_Help(string[] commandLineArgs)
	{
		string[] value = (from selector in this.Commands
		orderby selector.Name
		select string.Format("Command: {0}, {1}", selector.Name, selector.Help)).ToArray<string>();
		return string.Join("\n", value);
	}

	private string Command_IAmACheater(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service == null || service.Game == null)
		{
			return string.Empty;
		}
		IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
		if (service2 == null || service2.ActivePlayerController == null)
		{
			return string.Empty;
		}
		global::Empire empire = service2.ActivePlayerController.Empire as global::Empire;
		if (empire == null)
		{
			return string.Empty;
		}
		bool flag = true;
		if (commandLineArgs.Length > 1)
		{
			if (!(commandLineArgs[1].ToLower() == "nomore"))
			{
				return string.Format("Incorrect argument.\nExcepted format: {0} [NoMore]", commandLineArgs[0]);
			}
			flag = false;
		}
		Order order = new OrderSwitchCheatMode(empire.Index, flag);
		Diagnostics.Assert(empire.PlayerControllers != null && empire.PlayerControllers.Client != null);
		empire.PlayerControllers.Client.PostOrder(order);
		if (flag)
		{
			return string.Format("You're now a cheater. Shame on you !", new object[0]);
		}
		return string.Format("You're not a cheater anymore. Welcome on the right side.", new object[0]);
	}

	private string Command_INeedAHero(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
		OrderGenerateHero orderGenerateHero = new OrderGenerateHero(service2.ActivePlayerController.Empire.Index);
		if (commandLineArgs.Length > 1)
		{
			orderGenerateHero.UnitProfileName = commandLineArgs[1];
		}
		else
		{
			IDatabase<UnitProfile> database = Databases.GetDatabase<UnitProfile>(false);
			UnitProfile[] values = database.GetValues();
			if (values.Length == 0)
			{
				return "ERROR: No profile found in the database.";
			}
			int num = UnityEngine.Random.Range(0, values.Length - 1);
			orderGenerateHero.UnitProfileName = values[num].Name;
		}
		service2.ActivePlayerController.PostOrder(orderGenerateHero);
		return string.Empty;
	}

	private string Command_IPutASpellOnYou(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service != null && service.Game != null)
		{
			IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
			if (service2 != null && service2.ActivePlayerController != null)
			{
				int jailerEmpireIndex = 1;
				if (service2.ActivePlayerController.Empire.Index == 1)
				{
					jailerEmpireIndex = 0;
				}
				ICursorService service3 = Services.GetService<ICursorService>();
				if (service3 != null && service3.CurrentCursor is ArmyWorldCursor)
				{
					ArmyWorldCursor armyWorldCursor = service3.CurrentCursor as ArmyWorldCursor;
					if (armyWorldCursor.Army != null && armyWorldCursor.Army.Hero != null)
					{
						OrderCaptureHero order = new OrderCaptureHero(service2.ActivePlayerController.Empire.Index, armyWorldCursor.Army.Hero.GUID, jailerEmpireIndex);
						service2.ActivePlayerController.PostOrder(order);
						return "Your hero is now in the ennemy jail...";
					}
				}
				else if (service3 != null && service3.CurrentCursor is DistrictWorldCursor)
				{
					DistrictWorldCursor districtWorldCursor = service3.CurrentCursor as DistrictWorldCursor;
					if (districtWorldCursor.City != null && districtWorldCursor.City.Hero != null)
					{
						OrderCaptureHero order2 = new OrderCaptureHero(service2.ActivePlayerController.Empire.Index, districtWorldCursor.City.Hero.GUID, jailerEmpireIndex);
						service2.ActivePlayerController.PostOrder(order2);
						return "You hero is now in the ennemy jail...";
					}
				}
			}
		}
		return "You need to select an army or a city with an hero for this command to work.";
	}

	public string Command_KaijuComeBack(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
		ICursorService service3 = Services.GetService<ICursorService>();
		if (service3 != null && service3.CurrentCursor is ArmyWorldCursor)
		{
			ArmyWorldCursor armyWorldCursor = service3.CurrentCursor as ArmyWorldCursor;
			Army army = armyWorldCursor.Army;
			if (army != null && army is KaijuArmy)
			{
				KaijuArmy kaijuArmy = army as KaijuArmy;
				Kaiju kaiju = kaijuArmy.Kaiju;
				OrderKaijuChangeMode order = new OrderKaijuChangeMode(kaiju, true, false, false);
				service2.ActivePlayerController.PostOrder(order);
				return "The beast now rest again!";
			}
		}
		return "You need to select a world Kaiju Army for this command to work.";
	}

	public string Command_KaijuIChooseYou(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
		ICursorService service3 = Services.GetService<ICursorService>();
		if (service3 != null && service3.CurrentCursor is KaijuWorldCursor)
		{
			KaijuWorldCursor kaijuWorldCursor = service3.CurrentCursor as KaijuWorldCursor;
			Kaiju kaiju = kaijuWorldCursor.Kaiju;
			if (kaiju != null)
			{
				if (service != null)
				{
					IWorldPositionningService service4 = service.Game.Services.GetService<IWorldPositionningService>();
					if (service4 != null)
					{
						OrderKaijuChangeMode order = new OrderKaijuChangeMode(kaiju, false, true, false);
						service2.ActivePlayerController.PostOrder(order);
						return "The beast now joins the battle!";
					}
				}
				return "Game service could not be found";
			}
		}
		return "You need to select a Kaiju Garrison for this command to work.";
	}

	private string Command_KnowledgeIsPower(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service != null && service.Game != null)
		{
			IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
			IEnumerable<DepartmentOfScience.ConstructibleElement> source = Databases.GetDatabase<DepartmentOfScience.ConstructibleElement>(false).GetValues();
			if (commandLineArgs.Length <= 1 || commandLineArgs[1] != "all")
			{
				source = from technology in source
				where !technology.HasTechnologyFlag(DepartmentOfScience.ConstructibleElement.TechnologyFlag.Affinity) && !technology.HasTechnologyFlag(DepartmentOfScience.ConstructibleElement.TechnologyFlag.Quest)
				select technology;
			}
			OrderForceUnlockTechnology order = new OrderForceUnlockTechnology(service2.ActivePlayerController.Empire.Index, Array.ConvertAll<DepartmentOfScience.ConstructibleElement, StaticString>(source.ToArray<DepartmentOfScience.ConstructibleElement>(), (DepartmentOfScience.ConstructibleElement match) => match.Name));
			service2.ActivePlayerController.PostOrder(order);
			return "And now you know it all...";
		}
		return string.Empty;
	}

	private string Command_LetTheSunshineIn(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service == null || service.Game == null)
		{
			return string.Empty;
		}
		ISeasonService service2 = service.Game.Services.GetService<ISeasonService>();
		if (service2 == null)
		{
			return string.Empty;
		}
		ISessionService service3 = Services.GetService<ISessionService>();
		if (service3 == null || service3.Session == null || !service3.Session.IsOpened || !service3.Session.IsHosting)
		{
			return "You need to host the session in order to let the sun shine.";
		}
		Season currentSeason = service2.GetCurrentSeason();
		if (currentSeason.SeasonDefinition.SeasonType == Season.ReadOnlySummer)
		{
			return "Summer is already there.";
		}
		service2.ForceCurrentSeason(Season.ReadOnlySummer);
		return "Feel the sun shine.";
	}

	private string Command_LightMeUp(string[] commandLineArgs)
	{
		if (commandLineArgs.Length > 1 && commandLineArgs[1].ToLower() == "nomore")
		{
			global::GameManager.Preferences.GameplayGraphicOptions.EnableFogOfWar = true;
			return "...and in the darkness bind them";
		}
		global::GameManager.Preferences.GameplayGraphicOptions.EnableFogOfWar = false;
		return "Let there be light!";
	}

	private string Command_ListPlayers(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service == null || service.Game == null)
		{
			return "IGameService is null.";
		}
		IPlayerRepositoryService service2 = service.Game.Services.GetService<IPlayerRepositoryService>();
		if (service2 != null)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendFormat("{0} Players found.\n", service2.Count);
			foreach (Player player in service2)
			{
				stringBuilder.AppendLine(player.ToString());
			}
			return stringBuilder.ToString();
		}
		return "IPlayerRepositoryService is null.";
	}

	private string Command_LoadDynamicBitmaps(string[] commandLineArgs)
	{
		string path = System.IO.Path.Combine(UnityEngine.Application.dataPath, "Resources/GUI/DynamicBitmaps");
		DirectoryInfo directoryInfo = new DirectoryInfo(path);
		if (directoryInfo.Exists)
		{
			int num = 0;
			char[] trimChars = new char[]
			{
				'\\',
				'/',
				'.'
			};
			FileInfo[] files = directoryInfo.GetFiles("*.*", SearchOption.AllDirectories);
			foreach (FileInfo fileInfo in files)
			{
				string text = System.IO.Path.ChangeExtension(fileInfo.FullName, string.Empty).Substring(UnityEngine.Application.dataPath.Length + "/Resources".Length).Trim(trimChars);
				Texture2D x = AgeManager.Instance.FindDynamicTexture(text.Replace('\\', '/'), false);
				if (x != null)
				{
					num++;
				}
			}
			return string.Format("Found {0} texture(s).", num);
		}
		return "Loading failed.";
	}

	private string Command_LookAt(string[] commandLineArgs)
	{
		if (commandLineArgs.Length < 3)
		{
			return "Please specify the X and Y position";
		}
		int num;
		if (!int.TryParse(commandLineArgs[1], out num))
		{
			return "Argument 1 is not an int: " + commandLineArgs[1];
		}
		int num2;
		if (!int.TryParse(commandLineArgs[2], out num2))
		{
			return "Argument 2 is not an int: " + commandLineArgs[2];
		}
		Amplitude.Unity.View.IViewService service = Services.GetService<Amplitude.Unity.View.IViewService>();
		if (service.CurrentView != null && service.CurrentView.CameraController is IWorldViewCameraController)
		{
			IWorldViewCameraController worldViewCameraController = service.CurrentView.CameraController as IWorldViewCameraController;
			worldViewCameraController.FocusCameraAt(new WorldPosition(num, num2), true, true);
		}
		return string.Concat(new object[]
		{
			"Looking at (",
			num,
			",",
			num2,
			")"
		});
	}

	private string Command_Orb(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service == null || service.Game == null)
		{
			return string.Empty;
		}
		if (commandLineArgs.Length > 1)
		{
			string text = commandLineArgs[1].ToLower();
			string text2 = text;
			if (text2 != null)
			{
				if (global::CommandManager.<>f__switch$map6 == null)
				{
					global::CommandManager.<>f__switch$map6 = new Dictionary<string, int>(1)
					{
						{
							"count",
							0
						}
					};
				}
				int num;
				if (global::CommandManager.<>f__switch$map6.TryGetValue(text2, out num))
				{
					if (num == 0)
					{
						GridMap<byte> gridMap = (service.Game as global::Game).World.Atlas.GetMap(WorldAtlas.Maps.Orbs) as GridMap<byte>;
						if (gridMap == null)
						{
							return "Fail getting OrbMap.";
						}
						int num2 = 0;
						for (int i = 0; i < gridMap.Data.Length; i++)
						{
							num2 += (int)gridMap.Data[i];
						}
						return "There is " + num2 + " in the world for now.";
					}
				}
			}
		}
		return "Possible arguments are: count, more might come.";
	}

	private string Command_Ping(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null && service.Game != null);
		IPingService service2 = service.Game.Services.GetService<IPingService>();
		Diagnostics.Assert(service2 != null);
		return string.Format("{0:0.} ms.", service2.LastPingResponse.AverageLatency * 1000.0);
	}

	private string Command_PowerMeUp(string[] commandLineArgs)
	{
		ICursorService service = Services.GetService<ICursorService>();
		if (service != null && service.CurrentCursor is ArmyWorldCursor)
		{
			ArmyWorldCursor armyWorldCursor = service.CurrentCursor as ArmyWorldCursor;
			if (armyWorldCursor.Army != null && armyWorldCursor.Army.Units != null)
			{
				int num = 1;
				if (commandLineArgs.Length > 1)
				{
					int.TryParse(commandLineArgs[1], out num);
				}
				for (int i = 0; i < num; i++)
				{
					foreach (Unit unit in armyWorldCursor.Army.Units)
					{
						float propertyValue = unit.GetPropertyValue(SimulationProperties.UnitExperience);
						float propertyValue2 = unit.GetPropertyValue(SimulationProperties.UnitNextLevelExperience);
						float xp = propertyValue2 - propertyValue;
						unit.GainXp(xp, false, true);
					}
				}
				return "Your army's units have leveled up!";
			}
		}
		return "You need to select a world army for this command to work.";
	}

	private string Command_Quit(string[] commandLineArgs)
	{
		Amplitude.Unity.Framework.Application.Quit();
		return string.Empty;
	}

	public string Command_RelocateKaijus(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service == null || service.Game == null)
		{
			return string.Empty;
		}
		global::Game game = service.Game as global::Game;
		if (game == null)
		{
			return string.Empty;
		}
		for (int i = 0; i < game.Empires.Length; i++)
		{
			if (game.Empires[i] is KaijuEmpire)
			{
				KaijuEmpire kaijuEmpire = game.Empires[i] as KaijuEmpire;
				kaijuEmpire.GetAgency<KaijuCouncil>().TryRelocateKaijuOrResetETA();
			}
		}
		return "The giants have moved";
	}

	private string Command_RequestGameSave(string[] commandLineArgs)
	{
		ISessionService service = Services.GetService<ISessionService>();
		Diagnostics.Assert(service != null);
		Message message = new GameClientDownloadGameMessage(SaveType.LastAutoSave, string.Empty);
		(service.Session as global::Session).GameClient.SendMessageToServer(ref message);
		return "Request a remote game save to the server.";
	}

	private string Command_SeasonEffect(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service == null || service.Game == null)
		{
			return string.Empty;
		}
		ISeasonService service2 = service.Game.Services.GetService<ISeasonService>();
		if (service2 == null)
		{
			return string.Empty;
		}
		if (commandLineArgs.Length > 1)
		{
			string text = commandLineArgs[1].ToLower();
			string text2 = text;
			switch (text2)
			{
			case "pick":
			{
				if (commandLineArgs.Length <= 2)
				{
					return "Error: missing name";
				}
				StaticString staticString = commandLineArgs[2];
				if (service2.ForcePickSeasonEffect(staticString))
				{
					return staticString + " has been added to selected SeasonEffects";
				}
				return "Unknown SeasonEffect: " + staticString;
			}
			case "vote":
				if (commandLineArgs.Length > 2)
				{
					StaticString x = commandLineArgs[2];
					service2.WinterImmunityBid(0, 1);
					return x + "Empire 0 has bid one orb for immunity";
				}
				return "Error: missing name";
			case "remove":
			{
				if (commandLineArgs.Length <= 2)
				{
					return "Error: missing name";
				}
				StaticString staticString2 = commandLineArgs[2];
				if (service2.ForceRemoveSeasonEffect(staticString2))
				{
					return staticString2 + " has been retired from selected SeasonEffects";
				}
				return "Unknown achievement or statistic: " + staticString2;
			}
			case "show":
			{
				StaticString x2 = string.Empty;
				List<SeasonEffect> list = new List<SeasonEffect>();
				list = service2.GetAvailableSeasonEffects();
				x2 += "\nCandidate season effects : ";
				foreach (SeasonEffect seasonEffect in list)
				{
					x2 = string.Concat(new object[]
					{
						x2,
						"\n",
						seasonEffect.SeasonEffectDefinition.Name,
						", #3399FF#  DrawCount = ",
						seasonEffect.DrawCount,
						"#REVERT#"
					});
				}
				list = service2.GetCandidateEffectsForSeasonType(Season.ReadOnlyWinter);
				x2 += "\n\nPreselected season effects : ";
				foreach (SeasonEffect seasonEffect2 in list)
				{
					x2 = string.Concat(new object[]
					{
						x2,
						"\n",
						seasonEffect2.SeasonEffectDefinition.Name,
						", #3399FF# DrawCount = ",
						seasonEffect2.DrawCount,
						", VoteCount = ",
						service2.GetSeasonEffectScore(seasonEffect2),
						"#REVERT#"
					});
				}
				list = service2.GetCurrentSeason().AdditionalSeasonEffects;
				x2 += "\n\nCurrent season effects applied : ";
				foreach (SeasonEffect seasonEffect3 in list)
				{
					x2 = x2 + "\n" + seasonEffect3.SeasonEffectDefinition.Name;
				}
				return x2;
			}
			case "iamelsa":
			case "iamjackfrost":
			{
				service2.ForcePickSeasonEffect("SeasonEffectFrozenWater1");
				ISessionService service3 = Services.GetService<ISessionService>();
				if (service3 == null || service3.Session == null || !service3.Session.IsOpened || !service3.Session.IsHosting)
				{
					return "You need to host the session in order to let the winter come.";
				}
				Season currentSeason = service2.GetCurrentSeason();
				if (currentSeason.SeasonDefinition.SeasonType == Season.ReadOnlyWinter)
				{
					return "Winter is already there.";
				}
				service2.ForceCurrentSeason(Season.ReadOnlyWinter);
				return "Let it snow. Let it snow. Let it snow !";
			}
			}
		}
		return "Possible arguments are: Pick, Remove, Show";
	}

	private string Command_SetWindPreferences(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service == null || service.Game == null)
		{
			return string.Empty;
		}
		if (service.Game.Services.GetService<IWeatherService>() == null)
		{
			return string.Empty;
		}
		int windDirection;
		int windStrength;
		if (commandLineArgs.Length == 3 && int.TryParse(commandLineArgs[1], out windDirection) && int.TryParse(commandLineArgs[2], out windStrength))
		{
			IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
			if (service2 != null)
			{
				OrderSetWindPreferences order = new OrderSetWindPreferences(windDirection, windStrength);
				service2.ActivePlayerController.PostOrder(order);
				return "Done";
			}
		}
		return "Correct syntaxe is /SetWindPreferences [WindDirectionValue (0-5)] [WindStrengthValue (1-3)]";
	}

	private string Command_ShowMeTheMoney(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service != null && service.Game != null)
		{
			IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
			if (service2 != null && service2.ActivePlayerController != null)
			{
				int num = 500;
				if (commandLineArgs.Length > 1)
				{
					int.TryParse(commandLineArgs[1], out num);
				}
				OrderTransferResources order = new OrderTransferResources(service2.ActivePlayerController.Empire.Index, DepartmentOfTheTreasury.Resources.EmpireMoney, (float)num, 0UL);
				service2.ActivePlayerController.PostOrder(order);
				Diagnostics.Log("Ordering transfer of {0} unit(s) of Dust.", new object[]
				{
					num
				});
			}
		}
		return string.Empty;
	}

	private string Command_ShowMeTheResources(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service != null && service.Game != null)
		{
			IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
			if (service2 != null && service2.ActivePlayerController != null)
			{
				int num = 9000;
				if (commandLineArgs.Length > 1)
				{
					int.TryParse(commandLineArgs[1], out num);
				}
				OrderTransferResources order = new OrderTransferResources(service2.ActivePlayerController.Empire.Index, DepartmentOfTheTreasury.Resources.CityGrowth, (float)num, 0UL);
				service2.ActivePlayerController.PostOrder(order);
				order = new OrderTransferResources(service2.ActivePlayerController.Empire.Index, DepartmentOfTheTreasury.Resources.EmpireMoney, (float)(num * 10), 0UL);
				service2.ActivePlayerController.PostOrder(order);
				order = new OrderTransferResources(service2.ActivePlayerController.Empire.Index, DepartmentOfTheTreasury.Resources.EmpirePoint, (float)num, 0UL);
				service2.ActivePlayerController.PostOrder(order);
				order = new OrderTransferResources(service2.ActivePlayerController.Empire.Index, DepartmentOfTheTreasury.Resources.EmpireResearch, (float)num, 0UL);
				service2.ActivePlayerController.PostOrder(order);
				order = new OrderTransferResources(service2.ActivePlayerController.Empire.Index, DepartmentOfTheTreasury.Resources.Orb, (float)num * 0.1f, 0UL);
				service2.ActivePlayerController.PostOrder(order);
				IDatabase<ResourceDefinition> database = Databases.GetDatabase<ResourceDefinition>(false);
				IEnumerable<ResourceDefinition> enumerable = from definition in database.GetValues()
				where definition.ResourceType == ResourceDefinition.Type.Strategic || definition.ResourceType == ResourceDefinition.Type.Luxury
				select definition;
				foreach (ResourceDefinition resourceDefinition in enumerable)
				{
					order = new OrderTransferResources(service2.ActivePlayerController.Empire.Index, resourceDefinition.Name, (float)num, 0UL);
					service2.ActivePlayerController.PostOrder(order);
				}
			}
		}
		return string.Empty;
	}

	private string Command_ShowMeTheWay(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service != null && service.Game != null)
		{
			global::Game game = service.Game as global::Game;
			if (game != null)
			{
				IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
				if (service2 != null && service2.ActivePlayerController != null)
				{
					GridMap<short> gridMap = game.World.Atlas.GetMap(WorldAtlas.Maps.Exploration) as GridMap<short>;
					if (gridMap != null)
					{
						for (int i = 0; i < gridMap.Data.Length; i++)
						{
							short[] data = gridMap.Data;
							int num = i;
							data[num] |= (short)service2.ActivePlayerController.Empire.Bits;
						}
					}
					Amplitude.Unity.View.IViewService service3 = Services.GetService<Amplitude.Unity.View.IViewService>();
					if (service3 != null)
					{
						WorldView x = service3.FindByType(typeof(WorldView)) as WorldView;
						if (x != null)
						{
						}
					}
					IVisibilityService service4 = game.GetService<IVisibilityService>();
					if (service4 != null)
					{
						service4.NotifyVisibilityHasChanged(service2.ActivePlayerController.Empire as global::Empire);
					}
					return string.Format("Ordering exploration of the whole world.", new object[0]);
				}
			}
		}
		return string.Empty;
	}

	private string Command_SimulationDebugMode(string[] commandLineArgs)
	{
		bool flag;
		if (commandLineArgs.Length == 1)
		{
			flag = true;
		}
		else
		{
			if (commandLineArgs.Length > 2)
			{
				return string.Format("Excepted format: {0} [On|Off]", commandLineArgs[0]);
			}
			string text = commandLineArgs[1];
			if (text == "help")
			{
				return string.Format("Excepted format: {0} [On|Off]", commandLineArgs[0]);
			}
			if (text.ToLower() == "on")
			{
				flag = true;
			}
			else if (text.ToLower() == "off")
			{
				flag = false;
			}
			else
			{
				if (!(text.ToLower() == "nomore"))
				{
					return string.Format("Excepted format: {0} [On|Off]", commandLineArgs[0]);
				}
				flag = false;
			}
		}
		if (flag)
		{
			if (!(this.simulationDebugModeObject == null))
			{
				return "The Simulation debug mode is already started.";
			}
			GameObject gameObject = (GameObject)Resources.Load("Prefabs/Debug/SimulationDebugMode");
			if (gameObject == null)
			{
				return "[Error] Failed to retrieve the Simulation debug mode prefab.";
			}
			this.simulationDebugModeObject = UnityEngine.Object.Instantiate<GameObject>(gameObject);
			return "Succeed to start the Simulation debug mode.";
		}
		else
		{
			if (this.simulationDebugModeObject != null)
			{
				SimulationDebugMode component = this.simulationDebugModeObject.GetComponent<SimulationDebugMode>();
				if (component != null)
				{
					component.Release();
				}
				UnityEngine.Object.Destroy(this.simulationDebugModeObject);
				return "Succeed to stop the Simulation debug mode.";
			}
			return "The Simulation debug mode is already stoped.";
		}
	}

	private string Command_Slap(string[] commandLineArgs)
	{
		ICursorService service = Services.GetService<ICursorService>();
		if (service != null && service.CurrentCursor is ArmyWorldCursor)
		{
			ArmyWorldCursor armyWorldCursor = service.CurrentCursor as ArmyWorldCursor;
			if (armyWorldCursor.Army != null && armyWorldCursor.Army.Units != null)
			{
				float damage = 0f;
				if (commandLineArgs.Length > 1)
				{
					float.TryParse(commandLineArgs[1], NumberStyles.Any, CultureInfo.InvariantCulture, out damage);
				}
				this.WoundUnits(armyWorldCursor.Army.Units, damage);
				return "Your army's units are now bleeding...";
			}
		}
		else if (service != null && service.CurrentCursor is DistrictWorldCursor)
		{
			DistrictWorldCursor districtWorldCursor = service.CurrentCursor as DistrictWorldCursor;
			if (districtWorldCursor.City != null && districtWorldCursor.City.Units != null)
			{
				float damage2 = 0f;
				if (commandLineArgs.Length > 1)
				{
					float.TryParse(commandLineArgs[1], NumberStyles.Any, CultureInfo.InvariantCulture, out damage2);
				}
				this.WoundUnits(districtWorldCursor.City.Units, damage2);
				return "Your city's units have been poisoned...";
			}
		}
		else if (service != null && service.CurrentCursor is GarrisonWorldCursor)
		{
			GarrisonWorldCursor garrisonWorldCursor = service.CurrentCursor as GarrisonWorldCursor;
			if (garrisonWorldCursor.Garrison != null && garrisonWorldCursor.Garrison.Units != null)
			{
				float damage3 = 0f;
				if (commandLineArgs.Length > 1)
				{
					float.TryParse(commandLineArgs[1], NumberStyles.Any, CultureInfo.InvariantCulture, out damage3);
				}
				this.WoundUnits(garrisonWorldCursor.Garrison.Units, damage3);
				return "Your Garrison's units are now bleeding...";
			}
		}
		else if (service != null && service.CurrentCursor is KaijuWorldCursor)
		{
			KaijuWorldCursor kaijuWorldCursor = service.CurrentCursor as KaijuWorldCursor;
			if (kaijuWorldCursor.Kaiju != null && kaijuWorldCursor.Kaiju.GetActiveTroops() != null)
			{
				float damage4 = 0f;
				if (commandLineArgs.Length > 1)
				{
					float.TryParse(commandLineArgs[1], NumberStyles.Any, CultureInfo.InvariantCulture, out damage4);
				}
				this.WoundUnits(kaijuWorldCursor.Kaiju.GetActiveTroops().Units, damage4);
				return "Your Kaiju's units are now bleeding...";
			}
		}
		return "You need to select an army or a city for this command to work.";
	}

	private void WoundUnits(IEnumerable<Unit> units, float damage)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service == null || service.Game == null)
		{
			return;
		}
		IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
		if (service2 == null || service2.ActivePlayerController == null)
		{
			return;
		}
		bool flag = damage == 0f;
		foreach (Unit unit in units)
		{
			float propertyValue = unit.GetPropertyValue(SimulationProperties.MaximumHealth);
			if (flag)
			{
				damage = UnityEngine.Random.Range(1f, propertyValue / 2f);
			}
			OrderWoundUnit order = new OrderWoundUnit(service2.ActivePlayerController.Empire.Index, unit.GUID, damage);
			service2.ActivePlayerController.PostOrder(order);
		}
	}

	public string Command_SpawnKaiju(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service == null || service.Game == null)
		{
			return string.Empty;
		}
		global::Game game = service.Game as global::Game;
		if (game == null)
		{
			return string.Empty;
		}
		IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
		KaijuEmpire[] array = Array.ConvertAll<global::Empire, KaijuEmpire>(Array.FindAll<global::Empire>(game.Empires, (global::Empire match) => match is KaijuEmpire), (global::Empire empire) => empire as KaijuEmpire);
		int num = -1;
		float num2 = float.MaxValue;
		for (int i = 0; i < array.Length; i++)
		{
			KaijuCouncil agency = array[i].GetAgency<KaijuCouncil>();
			if (!agency.KaijuEmpire.HasSpawnedAnyKaiju)
			{
				float industryNeededToSpawn = agency.GetIndustryNeededToSpawn();
				if (industryNeededToSpawn < num2)
				{
					num2 = industryNeededToSpawn;
					num = i;
				}
			}
		}
		if (num != -1)
		{
			OrderSpawnKaiju order = new OrderSpawnKaiju(array[num].Index);
			service2.ActivePlayerController.PostOrder(order);
			return "A Kaiju has spawned";
		}
		return "No more Kaijus to spawn";
	}

	private string Command_TameKaiju(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
		Diagnostics.Assert(service2 != null);
		ICursorService service3 = Services.GetService<ICursorService>();
		Diagnostics.Assert(service3 != null);
		if (service3 != null && service3.CurrentCursor is KaijuWorldCursor)
		{
			KaijuWorldCursor kaijuWorldCursor = service3.CurrentCursor as KaijuWorldCursor;
			Kaiju kaiju = kaijuWorldCursor.Kaiju;
			if (kaiju != null && !kaiju.IsTamed())
			{
				global::Empire empire = service2.ActivePlayerController.Empire as global::Empire;
				OrderTameKaiju order = new OrderTameKaiju(empire.Index, kaiju, null);
				service2.ActivePlayerController.PostOrder(order);
				return "The beast now belongs to you.";
			}
		}
		else if (service3 != null && service3.CurrentCursor is ArmyWorldCursor)
		{
			ArmyWorldCursor armyWorldCursor = service3.CurrentCursor as ArmyWorldCursor;
			KaijuArmy kaijuArmy = armyWorldCursor.Army as KaijuArmy;
			if (kaijuArmy != null && kaijuArmy.Kaiju != null)
			{
				global::Empire empire2 = service2.ActivePlayerController.Empire as global::Empire;
				OrderTameKaiju order2 = new OrderTameKaiju(empire2.Index, kaijuArmy.Kaiju, null);
				service2.ActivePlayerController.PostOrder(order2);
				return "The beast now belongs to you.";
			}
		}
		return "You need to select a Kaiju for this command to work.";
	}

	private string Command_TimeToDie(string[] commandLineArgs)
	{
		ICursorService service = Services.GetService<ICursorService>();
		if (service != null && service.CurrentCursor is ArmyWorldCursor)
		{
			ArmyWorldCursor armyWorldCursor = service.CurrentCursor as ArmyWorldCursor;
			if (armyWorldCursor.Army != null && armyWorldCursor.Army.Units != null)
			{
				DepartmentOfDefense agency = armyWorldCursor.Army.Empire.GetAgency<DepartmentOfDefense>();
				if (agency != null)
				{
					agency.RemoveArmy(armyWorldCursor.Army, true);
					return "Veni, vidi, not vici AT ALL";
				}
				return "Hm, no Department of Defense associated with this army... That shouldn't happen.";
			}
		}
		return "You need to select a world army for this command to work.";
	}

	private string Command_ToggleEndlessDay(string[] commandLineArgs)
	{
		ISessionService service = Services.GetService<ISessionService>();
		string text2;
		if (service != null && service.Session != null && service.Session.IsOpened && service.Session.IsHosting && commandLineArgs != null && commandLineArgs.Length > 1)
		{
			string text = commandLineArgs[1].ToLower();
			text2 = text;
			if (text2 != null)
			{
				if (global::CommandManager.<>f__switch$map8 == null)
				{
					global::CommandManager.<>f__switch$map8 = new Dictionary<string, int>(3)
					{
						{
							"on",
							0
						},
						{
							"off",
							0
						},
						{
							"default",
							1
						}
					};
				}
				int num;
				if (global::CommandManager.<>f__switch$map8.TryGetValue(text2, out num))
				{
					if (num != 0)
					{
						if (num == 1)
						{
							DownloadableContent8.EndlessDay.ProtectedOverride = string.Empty;
						}
					}
					else
					{
						DownloadableContent8.EndlessDay.ProtectedOverride = text;
					}
				}
			}
		}
		text2 = DownloadableContent8.EndlessDay.ProtectedOverride;
		if (text2 != null)
		{
			if (global::CommandManager.<>f__switch$map9 == null)
			{
				global::CommandManager.<>f__switch$map9 = new Dictionary<string, int>(2)
				{
					{
						"on",
						0
					},
					{
						"off",
						0
					}
				};
			}
			int num;
			if (global::CommandManager.<>f__switch$map9.TryGetValue(text2, out num))
			{
				if (num == 0)
				{
					return string.Format("Endless day is '{0}' (override).", (!DownloadableContent8.EndlessDay.IsActive) ? "off" : "on");
				}
			}
		}
		return string.Format("Endless day is '{0}'.", (!DownloadableContent8.EndlessDay.IsActive) ? "off" : "on");
	}

	private string Command_ToggleVisibility(string[] commandLineArgs)
	{
		string[] names = Enum.GetNames(typeof(PrimitiveLayerMask));
		if (commandLineArgs.Length < 2)
		{
			string text = "All";
			for (int i = 0; i < names.Length; i++)
			{
				text = text + ";" + names[i];
			}
			return string.Format("ToggleVisiblity LayerName [on/off]\n Layer name in {0}", text);
		}
		uint num = 0u;
		bool flag = false;
		int num2 = 0;
		if (commandLineArgs[1] == "All")
		{
			flag = true;
		}
		if (commandLineArgs.Length > 2 && commandLineArgs[2] == "off")
		{
			num2 = 1;
		}
		if (commandLineArgs.Length > 2 && commandLineArgs[2] == "on")
		{
			num2 = 2;
		}
		for (int j = 0; j < names.Length; j++)
		{
			if (flag || commandLineArgs[1] == names[j])
			{
				num |= 1u << j;
			}
		}
		if (num == 0u)
		{
			return "no layer mask match.";
		}
		bool flag2 = (global::GameManager.Preferences.GameGraphicSettings.PrimitiveLayerMask & (ulong)num) != 0UL;
		if (num2 == 1 || (num2 == 0 && flag2))
		{
			global::GameManager.Preferences.GameGraphicSettings.PrimitiveLayerMask = (global::GameManager.Preferences.GameGraphicSettings.PrimitiveLayerMask & (ulong)(~(ulong)num));
			return "Render element toggled off";
		}
		global::GameManager.Preferences.GameGraphicSettings.PrimitiveLayerMask = (global::GameManager.Preferences.GameGraphicSettings.PrimitiveLayerMask | (ulong)num);
		return "Render element toggled on";
	}

	private string Command_TransferResources(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service != null && service.Game != null)
		{
			IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
			if (service2 != null && service2.ActivePlayerController != null)
			{
				Diagnostics.Assert(commandLineArgs.Length > 0);
				if (commandLineArgs.Length == 1)
				{
					return string.Format("Excepted format: {0} ResourceName [Amount]", commandLineArgs[0]);
				}
				string text = commandLineArgs[1];
				if (text == "help")
				{
					return string.Format("Excepted format: {0} ResourceName [Amount]", commandLineArgs[0]);
				}
				IDatabase<ResourceDefinition> database = Databases.GetDatabase<ResourceDefinition>(false);
				ResourceDefinition resourceDefinition;
				if (!database.TryGetValue(text, out resourceDefinition))
				{
					return string.Format("Error! Invalid resource name. The resource {1} does not exist in the resource database.\nExcepted format: {0} ResourceName [Amount]", commandLineArgs[0], commandLineArgs[1]);
				}
				float num = 500f;
				if (commandLineArgs.Length > 2 && !float.TryParse(commandLineArgs[2], NumberStyles.Any, CultureInfo.InvariantCulture, out num))
				{
					return string.Format("Error! Invalid amount. Can't parse the amount {1}, it must be a float value.\nExcepted format: {0} ResourceName [Amount]", commandLineArgs[0], commandLineArgs[2]);
				}
				OrderTransferResources order = new OrderTransferResources(service2.ActivePlayerController.Empire.Index, resourceDefinition.Name, num, 0UL);
				service2.ActivePlayerController.PostOrder(order);
				Diagnostics.Log("Ordering transfer of {0} unit(s) of {1}.", new object[]
				{
					num,
					resourceDefinition.Name
				});
			}
		}
		return string.Empty;
	}

	private string CommandManager_UnitBodyInspector(string[] commandLineArgs)
	{
		GameObject gameObject = GameObject.Find("UnitBodyInspectorWindow");
		if (gameObject == null)
		{
			gameObject = new GameObject("UnitBodyInspectorWindow");
			gameObject.AddComponent<UnitBodyInspectorWindow>();
		}
		return string.Empty;
	}

	private string CommandManager_UnloadUnusedAssets(string[] commandLineArgs)
	{
		base.StartCoroutine(this.UnloadUnusedAssetsAsync());
		return string.Empty;
	}

	private IEnumerator UnloadUnusedAssetsAsync()
	{
		yield return Resources.UnloadUnusedAssets();
		yield break;
	}

	private string Command_UntameKaiju(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
		ICursorService service3 = Services.GetService<ICursorService>();
		if (service3 != null && service3.CurrentCursor is KaijuWorldCursor)
		{
			KaijuWorldCursor kaijuWorldCursor = service3.CurrentCursor as KaijuWorldCursor;
			Kaiju kaiju = kaijuWorldCursor.Kaiju;
			if (kaiju != null)
			{
				if (kaiju.IsTamed() && kaiju.Empire is MajorEmpire)
				{
					if (service != null)
					{
						IWorldPositionningService service4 = service.Game.Services.GetService<IWorldPositionningService>();
						if (service4 != null && kaiju.KaijuGarrison != null && kaiju.KaijuGarrison.Owner != null)
						{
							MajorEmpire owner = kaiju.KaijuGarrison.Owner;
							OrderUntameKaiju order = new OrderUntameKaiju(kaiju, true);
							service2.ActivePlayerController.PostOrder(order);
							return "The beast now returns to wild!";
						}
					}
					return "Game service could not be found";
				}
				return "Kaiju Garrison must be Tamed!.";
			}
		}
		else if (service3 != null && service3.CurrentCursor is ArmyWorldCursor)
		{
			ArmyWorldCursor armyWorldCursor = service3.CurrentCursor as ArmyWorldCursor;
			KaijuArmy kaijuArmy = armyWorldCursor.Army as KaijuArmy;
			if (kaijuArmy != null && kaijuArmy.Kaiju != null)
			{
				if (kaijuArmy.Kaiju.IsTamed() && kaijuArmy != null && kaijuArmy.Empire is MajorEmpire)
				{
					if (service != null)
					{
						IWorldPositionningService service5 = service.Game.Services.GetService<IWorldPositionningService>();
						if (service5 != null && kaijuArmy != null && kaijuArmy.Owner != null)
						{
							MajorEmpire owner2 = kaijuArmy.Owner;
							Kaiju kaiju2 = kaijuArmy.Kaiju;
							OrderUntameKaiju order2 = new OrderUntameKaiju(kaiju2, true);
							service2.ActivePlayerController.PostOrder(order2);
							return "The beast now returns to wild!";
						}
					}
					return "Game service could not be found";
				}
				return "Kaiju Army must be Tamed!.";
			}
		}
		return "You need to select a Kaiju for this command to work.";
	}

	private string Command_Visions(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service == null || service.Game == null)
		{
			return string.Empty;
		}
		ISessionService service2 = Services.GetService<ISessionService>();
		if (service2 == null || service2.Session == null || !service2.Session.IsOpened || !service2.Session.IsHosting)
		{
			return "You need to host the session in order to generate season mirages.";
		}
		IMiragesService service3 = service.Game.Services.GetService<IMiragesService>();
		if (service3 == null)
		{
			return string.Empty;
		}
		service3.RunMirageGenerationForCurrentSeason(World.Seed + ((global::Game)service.Game).Turn, true);
		return "Use your illusion.";
	}

	private string Command_WeatherControl(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service == null || service.Game == null)
		{
			return string.Empty;
		}
		if (service.Game.Services.GetService<IWeatherService>() == null)
		{
			return string.Empty;
		}
		if (commandLineArgs.Length == 2)
		{
			string text = commandLineArgs[1];
			IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
			if (service2 != null)
			{
				OrderActivateWeatherControl order = new OrderActivateWeatherControl(service2.ActivePlayerController.Empire.Index, text);
				service2.ActivePlayerController.PostOrder(order);
				return "Next turn should regenerate weather layer following preset ='" + text + "' settings.";
			}
		}
		return "Correct syntaxe is /WeatherControl [PresetName]";
	}

	private string Command_WhatIsYoursIsMine(string[] commandLineArgs)
	{
		ICursorService service = Services.GetService<ICursorService>();
		IGameService service2 = Services.GetService<IGameService>();
		if (service != null && service.CurrentCursor is ArmyWorldCursor && service2 != null && service2.Game != null)
		{
			IPlayerControllerRepositoryService service3 = service2.Game.Services.GetService<IPlayerControllerRepositoryService>();
			ArmyWorldCursor armyWorldCursor = service.CurrentCursor as ArmyWorldCursor;
			if (armyWorldCursor.Army != null && armyWorldCursor.Army.Units != null)
			{
				if (armyWorldCursor.Army.Empire.Index == service3.ActivePlayerController.Empire.Index && armyWorldCursor.Army.HasCatspaw)
				{
					OrderToggleCatspaw order = new OrderToggleCatspaw(service3.ActivePlayerController.Empire.Index, armyWorldCursor.Army.GUID, false);
					service3.ActivePlayerController.PostOrder(order);
					return "the army isn't yours anymore";
				}
				if ((armyWorldCursor.Army.Empire is MinorEmpire || armyWorldCursor.Army.Empire is NavalEmpire) && !armyWorldCursor.Army.HasCatspaw && armyWorldCursor.Army.Empire.Index != service3.ActivePlayerController.Empire.Index)
				{
					OrderToggleCatspaw order2 = new OrderToggleCatspaw(service3.ActivePlayerController.Empire.Index, armyWorldCursor.Army.GUID, true);
					service3.ActivePlayerController.PostOrder(order2);
					return "The army is now yours";
				}
			}
		}
		return "You need to select a world army for this command to work.";
	}

	private string Command_WhoAmI(string[] commandLineArgs)
	{
		string text = string.Format("Current user is logged in as '{0}'.", Environment.UserName);
		Steamworks.SteamUser steamUser = Steamworks.SteamAPI.SteamUser;
		if (steamUser != null)
		{
			Steamworks.SteamID steamID = steamUser.SteamID;
			string friendPersonaName = Steamworks.SteamAPI.SteamFriends.GetFriendPersonaName(steamID);
			text += string.Format(".. connected to steam (id: {0}, account name: '{1}').", steamID, friendPersonaName);
			ISessionService service = Services.GetService<ISessionService>();
			Diagnostics.Assert(service != null);
		}
		return text;
	}

	private string Command_WinterIsComming(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service == null || service.Game == null)
		{
			return string.Empty;
		}
		ISeasonService service2 = service.Game.Services.GetService<ISeasonService>();
		if (service2 == null)
		{
			return string.Empty;
		}
		ISessionService service3 = Services.GetService<ISessionService>();
		if (service3 == null || service3.Session == null || !service3.Session.IsOpened || !service3.Session.IsHosting)
		{
			int num = (service3 == null) ? 1 : ((service3.Session == null) ? 2 : ((!service3.Session.IsOpened) ? 3 : 4));
			return string.Format("You need to host the session in order to let the winter come. Error code: {0}", num);
		}
		if (service2.GetCurrentSeason().SeasonDefinition.SeasonType == Season.ReadOnlyWinter)
		{
			return "Winter is already there.";
		}
		service2.ForceCurrentSeason(Season.ReadOnlyWinter);
		return "Brace yourself, winter is coming.";
	}

	private string Command_WorldViewStatistics(string[] commandLineArgs)
	{
		Amplitude.Unity.View.IViewService service = Services.GetService<Amplitude.Unity.View.IViewService>();
		if (service == null)
		{
			return "Error: Can't retrieve the view service.";
		}
		WorldView worldView = service.CurrentView as WorldView;
		if (worldView == null)
		{
			return "Error: The current view is not the world view.";
		}
		WorldViewTechnique currentWorldViewTechnique = worldView.CurrentWorldViewTechnique;
		if (currentWorldViewTechnique == null)
		{
			return "Error: The current wordl view technique is null.";
		}
		IWorldViewStatisticsService service2 = currentWorldViewTechnique.GetService<IWorldViewStatisticsService>();
		if (service2 == null)
		{
			return "Error: Can't retrieve the world view statistics service.";
		}
		Diagnostics.Assert(commandLineArgs.Length > 0);
		float num = 0f;
		if (commandLineArgs.Length > 2)
		{
			return string.Format("Excepted format: {0} [ScreenOccupationThreshold]", commandLineArgs[0]);
		}
		if (commandLineArgs.Length == 2)
		{
			string text = commandLineArgs[1];
			if (text == "help")
			{
				return string.Format("Excepted format: {0} [ScreenOccupationThreshold]", commandLineArgs[0]);
			}
			if (!float.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out num))
			{
				return string.Format("Error: Incorrect float value for ScreenOccupationThreshold.\nExcepted format: {0} [ScreenOccupationThreshold]", commandLineArgs[0]);
			}
		}
		string text2 = "********** World View Statistics **********\n";
		if (service2.Statistics == null)
		{
			text2 += "No statistics.";
		}
		else
		{
			List<DefaultWorldViewStatistics.RenderStatistics> list = new List<DefaultWorldViewStatistics.RenderStatistics>(service2.Statistics);
			list.Sort((DefaultWorldViewStatistics.RenderStatistics left, DefaultWorldViewStatistics.RenderStatistics right) => -1 * left.OccupationPercent.CompareTo(right.OccupationPercent));
			foreach (DefaultWorldViewStatistics.RenderStatistics renderStatistics in list)
			{
				if (renderStatistics != null)
				{
					float num2 = renderStatistics.OccupationPercent * 100f;
					if (num2 > num)
					{
						text2 += string.Format("- {0} occupation = {1}% pan = {2}.\n", renderStatistics.StatisticsName, num2.ToString("0.00"), renderStatistics.Panoramic.ToString("0.00"));
					}
				}
			}
		}
		return text2;
	}

	private string Command_SetArmySpeed(string[] commandLineArgs)
	{
		double num = 1.0;
		if (commandLineArgs.Length != 2 || !double.TryParse(commandLineArgs[1], out num) || num <= 0.0)
		{
			return "Error! Example: /SetArmySpeed 2";
		}
		ISessionService service = Services.GetService<ISessionService>();
		if (service == null || service.Session == null || service.Session.SessionMode != SessionMode.Single)
		{
			return "Error! To be used in singleplayer only!";
		}
		string arg = string.Format("Army Speed was {0}\n", ELCPUtilities.ELCPArmySpeedScaleFactor);
		service.Session.SetLobbyData("ArmySpeedScaleFactor", commandLineArgs[1], true);
		ELCPUtilities.ELCPArmySpeedScaleFactor = num;
		return string.Format("{0}Changing to {1}", arg, ELCPUtilities.ELCPArmySpeedScaleFactor);
	}

	private string Command_Teleport(string[] commandLineArgs)
	{
		if (commandLineArgs.Length < 3)
		{
			return "Error! Usage: Teleport X Y";
		}
		ICursorService service = Services.GetService<ICursorService>();
		if (service != null && service.CurrentCursor is ArmyWorldCursor)
		{
			ArmyWorldCursor armyWorldCursor = service.CurrentCursor as ArmyWorldCursor;
			if (armyWorldCursor.Army != null && armyWorldCursor.Army.Units != null && !armyWorldCursor.Army.IsInEncounter && !armyWorldCursor.Army.IsLocked)
			{
				int num = -1;
				int num2 = -1;
				int.TryParse(commandLineArgs[1], out num);
				int.TryParse(commandLineArgs[2], out num2);
				if (num >= 0 && num2 >= 0)
				{
					WorldPosition item = new WorldPosition(num, num2);
					if (!item.IsValid)
					{
						return "Invalid Position";
					}
					IGameService service2 = Services.GetService<IGameService>();
					IPathfindingService service3 = service2.Game.Services.GetService<IPathfindingService>();
					PathfindingContext pathfindingContextProvider = armyWorldCursor.Army.GenerateContext();
					global::Game game = service2.Game as global::Game;
					List<WorldPosition> list = new List<WorldPosition>();
					list.Add(item);
					list.AddRange(item.GetNeighbours(game.World.WorldParameters));
					foreach (WorldPosition worldPosition in list)
					{
						if (worldPosition.IsValid && service3.IsTilePassable(worldPosition, pathfindingContextProvider, PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnoreZoneOfControl, null) && service3.IsTileStopable(worldPosition, pathfindingContextProvider, PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnoreZoneOfControl, null))
						{
							OrderTeleportArmy order = new OrderTeleportArmy(armyWorldCursor.Army.Empire.Index, armyWorldCursor.Army.GUID, worldPosition);
							armyWorldCursor.Army.Empire.PlayerControllers.Server.PostOrder(order);
							return "Teleporting to " + worldPosition;
						}
					}
				}
				return "Invalid Position";
			}
		}
		return "Error!";
	}

	private string Command_ShowMeTheStockpiles(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service != null && service.Game != null)
		{
			IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
			if (service2 != null && service2.ActivePlayerController != null)
			{
				int num = 10;
				if (commandLineArgs.Length > 1)
				{
					int.TryParse(commandLineArgs[1], out num);
				}
				for (int i = 0; i < num; i++)
				{
					OrderBuyoutAndActivateBooster order = new OrderBuyoutAndActivateBooster(service2.ActivePlayerController.Empire.Index, "BoosterIndustry", 0UL, false);
					service2.ActivePlayerController.PostOrder(order);
					OrderBuyoutAndActivateBooster order2 = new OrderBuyoutAndActivateBooster(service2.ActivePlayerController.Empire.Index, "BoosterFood", 0UL, false);
					service2.ActivePlayerController.PostOrder(order2);
					OrderBuyoutAndActivateBooster order3 = new OrderBuyoutAndActivateBooster(service2.ActivePlayerController.Empire.Index, "BoosterScience", 0UL, false);
					service2.ActivePlayerController.PostOrder(order3);
					OrderBuyoutAndActivateBooster orderBuyoutAndActivateBooster = new OrderBuyoutAndActivateBooster(service2.ActivePlayerController.Empire.Index, "FlamesIndustryBooster", 0UL, false);
					orderBuyoutAndActivateBooster.IgnoreCost = true;
					service2.ActivePlayerController.PostOrder(orderBuyoutAndActivateBooster);
					OrderBuyoutAndActivateBooster orderBuyoutAndActivateBooster2 = new OrderBuyoutAndActivateBooster(service2.ActivePlayerController.Empire.Index, "BoosterCadavers", 0UL, false);
					orderBuyoutAndActivateBooster2.IgnoreCost = true;
					service2.ActivePlayerController.PostOrder(orderBuyoutAndActivateBooster2);
				}
			}
		}
		return string.Empty;
	}

	private string Command_CheckDLCStatus(string[] commandLineArgs)
	{
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		if (service != null)
		{
			string str = "";
			DownloadableContentAccessibility accessibility = service.GetAccessibility(DownloadableContent1.ReadOnlyName);
			string str2 = str + string.Format("{0}: {1}\n", DownloadableContent1.ReadOnlyName, accessibility);
			accessibility = service.GetAccessibility(DownloadableContent2.ReadOnlyName);
			string str3 = str2 + string.Format("{0}: {1}\n", DownloadableContent2.ReadOnlyName, accessibility);
			accessibility = service.GetAccessibility(DownloadableContent3.ReadOnlyName);
			string str4 = str3 + string.Format("{0}: {1}\n", DownloadableContent3.ReadOnlyName, accessibility);
			accessibility = service.GetAccessibility(DownloadableContent4.ReadOnlyName);
			string str5 = str4 + string.Format("{0}: {1}\n", DownloadableContent4.ReadOnlyName, accessibility);
			accessibility = service.GetAccessibility(DownloadableContent5.ReadOnlyName);
			string str6 = str5 + string.Format("{0}: {1}\n", DownloadableContent5.ReadOnlyName, accessibility);
			accessibility = service.GetAccessibility(DownloadableContent6.ReadOnlyName);
			string str7 = str6 + string.Format("{0}: {1}\n", DownloadableContent6.ReadOnlyName, accessibility);
			accessibility = service.GetAccessibility(DownloadableContent7.ReadOnlyName);
			string str8 = str7 + string.Format("{0}: {1}\n", DownloadableContent7.ReadOnlyName, accessibility);
			accessibility = service.GetAccessibility(DownloadableContent8.ReadOnlyName);
			string str9 = str8 + string.Format("{0}: {1}\n", DownloadableContent8.ReadOnlyName, accessibility);
			accessibility = service.GetAccessibility(DownloadableContent9.ReadOnlyName);
			string str10 = str9 + string.Format("{0}: {1}\n", DownloadableContent9.ReadOnlyName, accessibility);
			accessibility = service.GetAccessibility(DownloadableContent10.ReadOnlyName);
			string str11 = str10 + string.Format("{0}: {1}\n", DownloadableContent10.ReadOnlyName, accessibility);
			accessibility = service.GetAccessibility(DownloadableContent11.ReadOnlyName);
			string str12 = str11 + string.Format("{0}: {1}\n", DownloadableContent11.ReadOnlyName, accessibility);
			accessibility = service.GetAccessibility(DownloadableContent12.ReadOnlyName);
			string str13 = str12 + string.Format("{0}: {1}\n", DownloadableContent12.ReadOnlyName, accessibility);
			accessibility = service.GetAccessibility(DownloadableContent13.ReadOnlyName);
			string str14 = str13 + string.Format("{0}: {1}\n", DownloadableContent13.ReadOnlyName, accessibility);
			accessibility = service.GetAccessibility(DownloadableContent14.ReadOnlyName);
			string str15 = str14 + string.Format("{0}: {1}\n", DownloadableContent14.ReadOnlyName, accessibility);
			accessibility = service.GetAccessibility(DownloadableContent15.ReadOnlyName);
			string str16 = str15 + string.Format("{0}: {1}\n", DownloadableContent15.ReadOnlyName, accessibility);
			accessibility = service.GetAccessibility(DownloadableContent16.ReadOnlyName);
			string str17 = str16 + string.Format("{0}: {1}\n", DownloadableContent16.ReadOnlyName, accessibility);
			accessibility = service.GetAccessibility(DownloadableContent17.ReadOnlyName);
			string str18 = str17 + string.Format("{0}: {1}\n", DownloadableContent17.ReadOnlyName, accessibility);
			accessibility = service.GetAccessibility(DownloadableContent18.ReadOnlyName);
			string str19 = str18 + string.Format("{0}: {1}\n", DownloadableContent18.ReadOnlyName, accessibility);
			accessibility = service.GetAccessibility(DownloadableContent19.ReadOnlyName);
			string str20 = str19 + string.Format("{0}: {1}\n", DownloadableContent19.ReadOnlyName, accessibility);
			accessibility = service.GetAccessibility(DownloadableContent20.ReadOnlyName);
			return str20 + string.Format("{0}: {1}", DownloadableContent20.ReadOnlyName, accessibility);
		}
		return "Error: DLC-Service not found";
	}

	private string Command_UpdateNodes(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		string text = string.Empty;
		if (service != null && service.Game != null)
		{
			IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
			if (service2 != null && service2.ActivePlayerController != null)
			{
				DepartmentOfCreepingNodes agency = service2.ActivePlayerController.Empire.GetAgency<DepartmentOfCreepingNodes>();
				if (agency == null)
				{
					return "Error: DepartmentOfCreepingNodes not found. Symbiosis DLC missing?";
				}
				DepartmentOfTheInterior agency2 = service2.ActivePlayerController.Empire.GetAgency<DepartmentOfTheInterior>();
				if (agency2 == null || agency2.MainCity == null)
				{
					return "Error: No Capital found";
				}
				List<StaticString> lastFailureFlags = new List<StaticString>();
				for (int i = 0; i < agency.Nodes.Count; i++)
				{
					PointOfInterest pointOfInterest = agency.Nodes[i].PointOfInterest;
					CreepingNodeImprovementDefinition bestCreepingNodeDefinition = agency.GetBestCreepingNodeDefinition(agency2.MainCity, pointOfInterest, pointOfInterest.CreepingNodeImprovement as CreepingNodeImprovementDefinition, lastFailureFlags);
					if (bestCreepingNodeDefinition != null && bestCreepingNodeDefinition != pointOfInterest.CreepingNodeImprovement as CreepingNodeImprovementDefinition)
					{
						text += string.Format("Upgrading Node {0} from {1} to {2}\n", pointOfInterest.WorldPosition, pointOfInterest.CreepingNodeImprovement.Name, bestCreepingNodeDefinition.Name);
						agency.Nodes[i].UpgradeNode(bestCreepingNodeDefinition);
					}
				}
			}
		}
		return text;
	}

	private string Command_Terraform(string[] commandLineArgs)
	{
		if (commandLineArgs.Length < 3)
		{
			return "Error! Usage: Terraform X Y (Range) (R for Reverse) (Duration)";
		}
		int num = -1;
		int num2 = -1;
		int.TryParse(commandLineArgs[1], out num);
		int.TryParse(commandLineArgs[2], out num2);
		if (num < 0 || num2 < 0)
		{
			return "Invalid Position";
		}
		WorldPosition worldPosition = new WorldPosition(num, num2);
		if (!worldPosition.IsValid)
		{
			return "Invalid Position";
		}
		int num3 = 1;
		if (commandLineArgs.Length > 3 && !int.TryParse(commandLineArgs[3], out num3))
		{
			num3 = 1;
		}
		bool flag = false;
		if (commandLineArgs.Length > 4 && commandLineArgs[4].ToLower().Contains("r"))
		{
			flag = true;
		}
		int num4 = 0;
		if (commandLineArgs.Length > 5 && !int.TryParse(commandLineArgs[5], out num4))
		{
			num4 = 0;
		}
		string text = string.Format("Trying to Terraform at {0} with radius {1}, Reverse? {2}, Duration {3}...", new object[]
		{
			worldPosition,
			num3,
			flag,
			num4
		});
		WorldCircle worldCircle = new WorldCircle(worldPosition, num3);
		IGameService service = Services.GetService<IGameService>();
		IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
		IWorldPositionningService service3 = service.Game.Services.GetService<IWorldPositionningService>();
		global::Game game = service.Game as global::Game;
		WorldPosition[] array = game.World.PerformReversibleTerraformation(worldCircle.GetWorldPositions(service3.World.WorldParameters), flag, num4);
		if (array.Length != 0)
		{
			game.World.UpdateTerraformStateMap(true);
			global::Empire empire = service2.ActivePlayerController.Empire as global::Empire;
			float propertyValue = empire.GetPropertyValue(SimulationProperties.TilesTerraformed);
			empire.SetPropertyBaseValue(SimulationProperties.TilesTerraformed, propertyValue + (float)array.Length);
			empire.Refresh(false);
			Services.GetService<IEventService>().Notify(new EventEmpireWorldTerraformed(empire, array));
			text += string.Format("\nTerraformed {0} tiles!", array.Length);
		}
		else
		{
			text += "\nError!";
		}
		return text;
	}

	private string Command_Skynet(string[] commandLineArgs)
	{
		ISessionService service = Services.GetService<ISessionService>();
		if (service == null || service.Session == null)
		{
			return "This command requires a game session to be running.";
		}
		if (service.Session.SessionMode != SessionMode.Single)
		{
			if (!(service.Session is global::Session))
			{
				return "Invalid session.";
			}
			return "This command requires the session to run in single player mode.";
		}
		else
		{
			bool flag = true;
			if (commandLineArgs.Length > 1 && commandLineArgs[1] == "off")
			{
				flag = false;
			}
			global::Session session = service.Session as global::Session;
			MajorEmpire majorEmpire = session.GameServer.Game.Empires[0] as MajorEmpire;
			GameServer gameServer = session.GameServer as GameServer;
			AIPlayer_MajorEmpire aiplayer_MajorEmpire;
			if (!gameServer.AIScheduler.TryGetMajorEmpireAIPlayer(majorEmpire, out aiplayer_MajorEmpire))
			{
				return "Can't get the AI player state.";
			}
			StaticString key = string.Format("Empire{0}", majorEmpire.Index);
			string lobbyData = session.GetLobbyData<string>(key, null);
			if (!flag)
			{
				if (aiplayer_MajorEmpire.AIState == AIPlayer.PlayerState.EmpireControlledByHuman)
				{
					return "Empire 0 already controlled by a human";
				}
				session.SetLobbyData(key, lobbyData.Replace("AI", ""), true);
				gameServer.AIScheduler.ChangeMajorEmpireAIState(majorEmpire, AIPlayer.PlayerState.EmpireControlledByHuman);
				return "Deactivating AI.";
			}
			else
			{
				if (aiplayer_MajorEmpire.AIState != AIPlayer.PlayerState.EmpireControlledByHuman)
				{
					return "Empire 0 not controlled by a human";
				}
				session.SetLobbyData(key, "AI" + lobbyData, true);
				gameServer.AIScheduler.ChangeMajorEmpireAIState(majorEmpire, AIPlayer.PlayerState.EmpireControlledByAI);
				aiplayer_MajorEmpire.Start();
				return "Activating AI, type '/Skynet off' to deactivate.";
			}
		}
	}

	private string Command_ForceSideQuestVillageTriggering(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service == null || service.Game == null || !(service.Game is global::Game))
		{
			return "Failed to retrieve game service";
		}
		object service2 = (service.Game as global::Game).Services.GetService<IPlayerControllerRepositoryService>();
		IQuestManagementService service3 = service.Game.Services.GetService<IQuestManagementService>();
		Diagnostics.Assert(service2 != null);
		Diagnostics.Assert(service3 != null);
		if (commandLineArgs.Length <= 1 || string.IsNullOrEmpty(commandLineArgs[1]))
		{
			return "You must specify a side quest village number";
		}
		int num;
		if (!int.TryParse(commandLineArgs[1], out num))
		{
			return "You must specify a valid side quest village number (1..49)";
		}
		if (num > 0 && num < 50)
		{
			string sideQuestVillageName = "DLC21_" + string.Format("{0:00}", num);
			return service3.ForceSideQuestVillageTrigger(sideQuestVillageName);
		}
		return "Invalid side quest village number (1..49)";
	}

	private string Command_CompleteQuest(string[] commandLineArgs)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service != null && service.Game != null)
		{
			IQuestManagementService service2 = service.Game.Services.GetService<IQuestManagementService>();
			IPlayerControllerRepositoryService service3 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
			if (service3 != null && service2 != null && service3.ActivePlayerController != null)
			{
				QuestState questState = QuestState.Completed;
				if (commandLineArgs.Length == 2 && commandLineArgs[1] == QuestState.Failed.ToString())
				{
					questState = QuestState.Failed;
				}
				DepartmentOfInternalAffairs agency = service3.ActivePlayerController.Empire.GetAgency<DepartmentOfInternalAffairs>();
				if (agency != null && agency.QuestJournal != null)
				{
					if (agency.QuestJournal.ActiveQuest == null)
					{
						return "Error: No active Quest found!";
					}
					if (agency.QuestJournal.ActiveQuest.QuestDefinition.GlobalWinner == GlobalQuestWinner.Participants)
					{
						return string.Format("Error: Cannot force complete quest \"{0}\"", agency.QuestJournal.ActiveQuest.Name);
					}
					service2.ForceQuestCompletion(agency.QuestJournal.ActiveQuest.GUID, questState);
					return string.Format("Trying to complete \"{0}\"", agency.QuestJournal.ActiveQuest.Name);
				}
			}
		}
		return "Error!";
	}

	private const GameDisconnectionReason Flag = GameDisconnectionReason._Debug;

	private GameObject debugModeObject;

	private bool needToStop;

	private static readonly string[] FormatSeparators = new string[]
	{
		"B",
		"KB",
		"MB",
		"GB",
		"TB"
	};

	private GameObject simulationDebugModeObject;

	private string skynetbackuplobbydata;
}
