﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using Amplitude;
using Amplitude.Interop;
using Amplitude.Unity.Framework;

public class Application : Amplitude.Unity.Framework.Application
{
	static Application()
	{
		Amplitude.Unity.Framework.Application.Name = "Endless Legend";
		Amplitude.Unity.Framework.Application.Version = new Amplitude.Unity.Framework.Version
		{
			Major = 2,
			Minor = 7,
			Revision = 69,
			Serial = 3,
			Label = "Community Patch",
			Accessibility = Accessibility.Public
		};
		global::Application.SteamAppID = 289130;
		switch (Amplitude.Unity.Framework.Application.Version.Accessibility)
		{
		case Accessibility.Private:
			global::Application.SteamAppID = 242900;
			Diagnostics.LogWarning("Borrowing SteamAppID {0} from 'Endless Legend DEV' (accessibility: '{1}').", new object[]
			{
				global::Application.SteamAppID,
				Amplitude.Unity.Framework.Application.Version.Accessibility
			});
			break;
		case Accessibility.Internal:
			global::Application.SteamAppID = 271140;
			Diagnostics.LogWarning("Borrowing SteamAppID {0} from 'Endless Legend QA' (accessibility: '{1}').", new object[]
			{
				global::Application.SteamAppID,
				Amplitude.Unity.Framework.Application.Version.Accessibility
			});
			break;
		case Accessibility.ProtectedInternal:
		case Accessibility.Protected:
			global::Application.SteamAppID = 271160;
			Diagnostics.LogWarning("Borrowing SteamAppID {0} from 'Endless Legend VIP' (accessibility: '{1}').", new object[]
			{
				global::Application.SteamAppID,
				Amplitude.Unity.Framework.Application.Version.Accessibility
			});
			break;
		}
		Amplitude.Unity.Framework.Application.DestroyBootstrapperOnIgnitionComplete = false;
		Databases.HashAlgorithm = MD5.Create();
		Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
	}

	~Application()
	{
	}

	public static global::CommandLineArguments CommandLineArguments
	{
		get
		{
			if (global::Application.commandLineArguments == null)
			{
				global::Application.commandLineArguments = new global::CommandLineArguments();
				string[] commandLineArgs = Environment.GetCommandLineArgs();
				CommandLineArgumentsParser.Interpret(commandLineArgs, ref global::Application.commandLineArguments);
			}
			return global::Application.commandLineArguments as global::CommandLineArguments;
		}
	}

	public static string DumpFilesDirectory
	{
		get
		{
			return System.IO.Path.Combine(Amplitude.Unity.Framework.Application.GameDirectory, "Dump Files");
		}
	}

	public new static string GameSaveDirectory
	{
		get
		{
			bool value = Amplitude.Unity.Framework.Application.Registry.GetValue<bool>(global::Application.Registers.SteamCloudRemoteStorage, false);
			if (value)
			{
				return System.IO.Path.Combine(Amplitude.Unity.Framework.Application.GameSaveDirectory, "Cloud");
			}
			return Amplitude.Unity.Framework.Application.GameSaveDirectory;
		}
	}

	public static int SteamAppID { get; private set; }

	protected override void Awake()
	{
		base.Awake();
		Diagnostics.Log(global::Application.CommandLineArguments);
		Amplitude.Unity.Framework.Application.DebugNetwork = global::Application.CommandLineArguments.DebugNetwork;
		Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools = global::Application.CommandLineArguments.EnableModdingTools;
		Amplitude.Unity.Framework.Application.Preferences.ELCPDevMode = false;
		ELCPUtilities.UseELCPMultiThreading = false;
		Amplitude.Unity.Framework.Application.Preferences.EnableMultiplayer = (Amplitude.Unity.Framework.Application.Preferences.ELCPDevMode || !Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools);
	}

	protected override IEnumerator OnApplicationIgnitionComplete()
	{
		try
		{
			List<DirectoryInfo> list = new List<DirectoryInfo>();
			int value = Amplitude.Unity.Framework.Application.Registry.GetValue<int>(global::Application.Registers.MaximumNumberOfTemporaryFilesFolders, 10);
			string[] directories = Directory.GetDirectories(Amplitude.Unity.Framework.Application.TempDirectory, "*", SearchOption.AllDirectories);
			if (directories.Length > value)
			{
				for (int i = 0; i < directories.Length; i++)
				{
					try
					{
						DirectoryInfo item = new DirectoryInfo(directories[i]);
						list.Add(item);
					}
					catch
					{
					}
				}
				if (list.Count > value)
				{
					list.Sort((DirectoryInfo l, DirectoryInfo r) => r.LastWriteTime.CompareTo(l.LastWriteTime));
					for (int j = value; j < list.Count; j++)
					{
						list[j].Delete(true);
					}
				}
			}
		}
		catch
		{
			throw;
		}
		Session.LateUpdatePreemption.EnableDiagnostics = Amplitude.Unity.Framework.Application.Registry.GetValue<bool>(global::Application.Registers.LateUpdatePreemptionEnableDiagnostics, Session.LateUpdatePreemption.EnableDiagnostics);
		Session.LateUpdatePreemption.Frequency = Amplitude.Unity.Framework.Application.Registry.GetValue<int>(global::Application.Registers.LateUpdatePreemptionFrequency, Session.LateUpdatePreemption.Frequency);
		Session.LateUpdatePreemption.TimeSpanInMilliseconds = Amplitude.Unity.Framework.Application.Registry.GetValue<double>(global::Application.Registers.LateUpdatePreemptionTimeSpanInMilliseconds, Session.LateUpdatePreemption.TimeSpanInMilliseconds);
		if (global::Application.CommandLineArguments.EnableModdingTools)
		{
			base.gameObject.AddComponent<EnableModdingToolsReminder>();
		}
		yield return base.OnApplicationIgnitionComplete();
		yield break;
	}

	protected override IEnumerator OnApplicationIgnitionStarted()
	{
		yield return base.OnApplicationIgnitionStarted();
		yield break;
	}

	protected override void OnApplicationQuit()
	{
		base.OnApplicationQuit();
	}

	public static bool ResolveChineseLanguage(out string chineseLanguage)
	{
		if (global::Application.chineseLanguage == null)
		{
			global::Application.chineseLanguage = string.Empty;
			try
			{
				Steamworks.SteamApps steamApps = Steamworks.SteamAPI.SteamApps;
				if (steamApps != null)
				{
					string currentGameLanguage = steamApps.GetCurrentGameLanguage();
					if (currentGameLanguage == "tchinese" || currentGameLanguage == "schinese")
					{
						global::Application.chineseLanguage = currentGameLanguage;
					}
				}
			}
			catch
			{
			}
		}
		chineseLanguage = global::Application.chineseLanguage;
		return !string.IsNullOrEmpty(global::Application.chineseLanguage);
	}

	private static Amplitude.Unity.Framework.CommandLineArguments commandLineArguments;

	private static string chineseLanguage;

	public abstract class FantasyPreferences
	{
		public static bool ActivateAIScheduler
		{
			get
			{
				return global::Application.FantasyPreferences.activateAIScheduler;
			}
		}

		public static int AIDebugHistoricSize
		{
			get
			{
				return global::Application.FantasyPreferences.aiDebugHistoricSize;
			}
		}

		public static bool EnableRunAIsOnThreads
		{
			get
			{
				return global::Application.FantasyPreferences.enableRunAIsOnThreads;
			}
		}

		public static bool EnableAIOnEmpire0
		{
			get
			{
				return global::Application.FantasyPreferences.enableAIOnEmpire0;
			}
		}

		public static bool EnableMinorEmpireAI
		{
			get
			{
				return global::Application.FantasyPreferences.enableMinorEmpireAI;
			}
		}

		public static bool EnableKaijuEmpireAI
		{
			get
			{
				return global::Application.FantasyPreferences.enableKaijuEmpireAI;
			}
		}

		public static bool EnableNavalEmpireAI
		{
			get
			{
				return global::Application.FantasyPreferences.enableNavalEmpireAI;
			}
		}

		public static bool ForceAIEndTurn
		{
			get
			{
				return global::Application.FantasyPreferences.forceAIEndTurn;
			}
		}

		public static bool EnableAIEntityCityReRun
		{
			get
			{
				return global::Application.FantasyPreferences.enableAIEntityCityReRun;
			}
		}

		public static bool EnableAIBattleSpectatorMode
		{
			get
			{
				return global::Application.FantasyPreferences.enableAIBattleSpectatorMode;
			}
		}

		public static bool EnableLesserEmpireAI
		{
			get
			{
				return global::Application.FantasyPreferences.enableLesserEmpireAI;
			}
		}

		public static bool ForceReinforcementToParticipate
		{
			get
			{
				return global::Application.FantasyPreferences.forceReinforcementToParticipate;
			}
		}

		private static bool activateAIScheduler = true;

		private static int aiDebugHistoricSize = 10;

		private static bool enableRunAIsOnThreads = true;

		private static bool enableAIOnEmpire0;

		private static bool enableMinorEmpireAI = true;

		private static bool enableNavalEmpireAI = true;

		private static bool enableLesserEmpireAI = true;

		private static bool enableKaijuEmpireAI = true;

		private static bool forceAIEndTurn = true;

		private static bool enableAIEntityCityReRun;

		private static bool enableAIBattleSpectatorMode;

		private static bool forceReinforcementToParticipate;
	}

	public static class Calendar
	{
		public static readonly DateTime FreeWeekend201504End = new DateTime(2015, 4, 20, 17, 0, 0);

		public static readonly DateTime FreeWeekend201504Start = new DateTime(2015, 4, 16, 17, 0, 0);
	}

	public new static class Registers
	{
		public static StaticString AlphaDisclaimerAcknowledged = new StaticString("Legal/RunOnce/AlphaDisclaimerAcknowledged");

		public static StaticString BetaDisclaimerAcknowledged = new StaticString("Legal/RunOnce/BetaDisclaimerAcknowledged");

		public static StaticString FreeWeekend201504DisclaimerAcknowledged = new StaticString("Legal/RunOnce/FreeWeekend201504DisclaimerAcknowledged");

		public static StaticString MaximumNumberOfTemporaryFilesFolders = new StaticString("Settings/MaximumNumberOfTemporaryFilesFolders");

		public static StaticString SteamCloudRemoteStorage = new StaticString("Settings/Steam/CloudRemoteStorage");

		public static StaticString NextLoadingTipNumber = new StaticString("NextLoadingTipNumber");

		public static StaticString LateUpdatePreemptionFrequency = new StaticString("Settings/LateUpdatePreemption/Frequency");

		public static StaticString LateUpdatePreemptionTimeSpanInMilliseconds = new StaticString("Settings/LateUpdatePreemption/TimeSpanInMilliseconds");

		public static StaticString LateUpdatePreemptionEnableDiagnostics = new StaticString("Settings/LateUpdatePreemption/EnableDiagnostics");

		public static StaticString LastModulePlaylistActivated = new StaticString("LastModulePlaylistActivated");

		public static StaticString LastModulePlaylistActivatedUrl = new StaticString("LastModulePlaylistActivatedUrl");

		public static StaticString AnonymousModulePlaylist = new StaticString("AnonymousModulePlaylist");
	}

	public static class Versions
	{
		public static Amplitude.Unity.Framework.Version PreAlpha = new Amplitude.Unity.Framework.Version
		{
			Major = 0,
			Minor = 1,
			Revision = 1,
			Serial = 3,
			Label = "PREALPHA",
			Accessibility = Accessibility.Internal
		};

		public static Amplitude.Unity.Framework.Version AlphaVip = new Amplitude.Unity.Framework.Version
		{
			Major = 0,
			Minor = 2,
			Revision = 6,
			Serial = 3,
			Label = "ALPHAVIP",
			Accessibility = Accessibility.Protected
		};

		public static Amplitude.Unity.Framework.Version Alpha = new Amplitude.Unity.Framework.Version
		{
			Major = 0,
			Minor = 4,
			Revision = 0,
			Serial = 3,
			Label = "ALPHA",
			Accessibility = Accessibility.Public
		};

		public static Amplitude.Unity.Framework.Version Beta = new Amplitude.Unity.Framework.Version
		{
			Major = 0,
			Minor = 5,
			Revision = 10,
			Serial = 3,
			Label = "BETA",
			Accessibility = Accessibility.Public
		};

		public static Amplitude.Unity.Framework.Version Retail = new Amplitude.Unity.Framework.Version
		{
			Major = 1,
			Minor = 0,
			Revision = 0,
			Serial = 3,
			Label = string.Empty,
			Accessibility = Accessibility.Public
		};
	}
}
