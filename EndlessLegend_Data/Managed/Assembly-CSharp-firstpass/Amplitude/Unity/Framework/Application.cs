using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Amplitude.Interop;
using Amplitude.Unity.Runtime;
using UnityEngine;

namespace Amplitude.Unity.Framework
{
	public class Application : Behaviour
	{
		static Application()
		{
			Application.Name = "Application";
			Application.Version = new Version
			{
				Major = 0,
				Minor = 0,
				Revision = 0,
				Serial = 0,
				Label = "FRAMEWORK",
				Accessibility = Accessibility.Internal
			};
			Application.UserName = "Default";
			Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
			Application.RegistryFilePath = string.Empty;
			Application.Registry = new Registry();
			Application.DestroyBootstrapperOnIgnitionComplete = true;
		}

		public static event Action OnApplicationUpdate;

		public static Bootstrapper Bootstrapper { get; private set; }

		public static bool DebugNetwork { get; protected set; }

		public static bool DestroyBootstrapperOnIgnitionComplete { get; set; }

		public static string GameDirectory
		{
			get
			{
				string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				if (!string.IsNullOrEmpty(folderPath))
				{
					return Path.Combine(folderPath, Application.Name);
				}
				Diagnostics.LogWarning("System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) returned an empty string.");
				StringBuilder stringBuilder = new StringBuilder(260);
				int num = Shell32.SHGetFolderPath(IntPtr.Zero, 32773, IntPtr.Zero, 0u, stringBuilder);
				if (num == 0)
				{
					return Path.Combine(stringBuilder.ToString(), Application.Name);
				}
				Diagnostics.LogWarning("Amplitude.Interop.Shell32.SHGetFolderPath(...) returned an error (result: {0:X}).", new object[]
				{
					num
				});
				string environmentVariable = Environment.GetEnvironmentVariable("USERPROFILE");
				string text = Path.Combine(environmentVariable, Application.Name);
				if (!Directory.Exists(text))
				{
					try
					{
						Directory.CreateDirectory(text);
					}
					catch (Exception ex)
					{
						Diagnostics.LogWarning("Exception caught while trying to create game directory \"{0}\": {1}", new object[]
						{
							text,
							ex.ToString()
						});
						text = Application.Name;
					}
				}
				return text;
			}
		}

		public static string GameSaveDirectory
		{
			get
			{
				return Path.Combine(Application.GameDirectory, "Save Files");
			}
		}

		public static bool HasFocus { get; private set; }

		public static string Name { get; protected set; }

		public static Registry Registry { get; private set; }

		public static string RegistryFilePath { get; internal set; }

		public static string TempDirectory
		{
			get
			{
				string text = Path.Combine(Application.GameDirectory, "Temporary Files");
				DirectoryInfo directoryInfo = new DirectoryInfo(text);
				if (!directoryInfo.Exists)
				{
					directoryInfo.Create();
				}
				string path = Path.Combine(Path.FullPath, "Public/Documents/Temporary Files");
				if (Directory.Exists(path))
				{
					string[] files = Directory.GetFiles(path);
					for (int i = 0; i < files.Length; i++)
					{
						string text2 = Path.Combine(text, Path.GetFileName(files[i]));
						if (!File.Exists(text2))
						{
							try
							{
								File.Copy(files[i], text2, true);
							}
							catch (IOException ex)
							{
								Diagnostics.LogError("Copy temporary files : Caught exception, " + ex.ToString() + ", " + ex.Message);
							}
						}
					}
				}
				return text;
			}
		}

		public static string UserDirectory
		{
			get
			{
				return Path.Combine(Path.Combine(Application.GameDirectory, "Users"), Application.UserUniqueID.ToString());
			}
		}

		public static string UserGameSaveDirectory
		{
			get
			{
				return Path.Combine(Application.UserDirectory, "Save Files");
			}
		}

		public static uint UserUniqueID { get; set; }

		public static string UserName { get; set; }

		public static string PathSecuredUserName
		{
			get
			{
				char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
				string text = Application.UserName;
				foreach (char oldChar in invalidFileNameChars)
				{
					text = text.Replace(oldChar, 'x');
				}
				return text;
			}
		}

		public static Version Version { get; protected set; }

		public FiniteStateMachine FiniteStateMachine { get; private set; }

		public int LastRevision { get; private set; }

		private protected Manager[] Managers { protected get; private set; }

		private static Application Instance { get; set; } = null;

		private static string OutputDebugFileName
		{
			get
			{
				return Path.Combine(Application.TempDirectory, "Diagnostics.log");
			}
		}

		private int DiagnosticsLogCounter { get; set; }

		public static void LoadRegistry()
		{
			Application.Registry.Clear();
			string text = Path.Combine(Path.FullPath, "Registry.xml");
			string text2 = Path.Combine(Application.GameDirectory, "Registry.xml");
			string[] files = new string[]
			{
				text,
				text2
			};
			Application.LoadRegistryFiles(files, false);
			Application.RegistryFilePath = Path.Combine(Application.UserDirectory, "Registry.xml");
			files = new string[]
			{
				Application.RegistryFilePath
			};
			Application.LoadRegistryFiles(files, true);
		}

		public static bool LoadRegistryFile(string registryFilePath, bool persistant = false)
		{
			if (File.Exists(registryFilePath))
			{
				try
				{
					Diagnostics.Log("Loading registry file '{0}'...", new object[]
					{
						registryFilePath
					});
					Application.Registry.Import(registryFilePath, persistant, null);
					return true;
				}
				catch
				{
				}
				return false;
			}
			Diagnostics.Log("Registry file '{0}' does not exist.", new object[]
			{
				registryFilePath
			});
			return false;
		}

		public static void LoadRegistryFiles(string[] files, bool persistant = false)
		{
			if (files == null)
			{
				throw new ArgumentNullException("files");
			}
			for (int i = 0; i < files.Length; i++)
			{
				if (!string.IsNullOrEmpty(files[i]))
				{
					Application.LoadRegistryFile(files[i], persistant);
				}
			}
		}

		public static void Quit()
		{
			if (Application.Instance != null && Application.Instance.shutdown == null)
			{
				Application.Instance.shutdown = UnityCoroutine.StartCoroutine(Application.Instance, Application.Instance.Shutdown(), null);
			}
		}

		protected override void Awake()
		{
			base.Awake();
			Diagnostics.Assert(Application.Instance == null);
			Application.Instance = this;
			this.FiniteStateMachine = new FiniteStateMachine();
			Application.HasFocus = true;
		}

		protected IEnumerator Ignite()
		{
			yield return this.SetupDiagnosticsLogFile();
			Diagnostics.Log("Starting the application, version is {0}...", new object[]
			{
				Application.Version.ToString()
			});
			Diagnostics.Log("Running 64-bit mode...");
			Diagnostics.Log("Game directory is \"{0}\".", new object[]
			{
				Application.GameDirectory
			});
			yield return this.SteamRestartAppIfNecessary();
			yield return this.SteamInitialize();
			yield return this.SteamGetCurrentGameLanguage();
			yield return this.SteamGetSteamUserName();
			Application.LoadRegistry();
			yield return this.OnApplicationIgnitionStarted();
			this.Managers = base.gameObject.GetComponentsInChildren<Manager>();
			foreach (Manager manager in this.Managers)
			{
				UnityCoroutine.StartCoroutine(manager, manager.Ignite(), new EventHandler<CoroutineExceptionEventArgs>(this.Manager_Ignite_CoroutineExceptionCallback));
			}
			foreach (Manager manager2 in this.Managers)
			{
				while (!manager2.HasBeenIgnited)
				{
					yield return null;
				}
				if (manager2.LastError != 0)
				{
				}
			}
			Application.Bootstrapper = (UnityEngine.Object.FindObjectOfType(typeof(Bootstrapper)) as Bootstrapper);
			this.LastRevision = Databases.CurrentRevision;
			Databases.Commit();
			yield return this.OnApplicationIgnitionComplete();
			yield break;
		}

		protected virtual void OnApplicationFocus(bool focus)
		{
			Application.HasFocus = focus;
		}

		protected virtual IEnumerator OnApplicationIgnitionComplete()
		{
			if (Application.DestroyBootstrapperOnIgnitionComplete && Application.Bootstrapper != null)
			{
				UnityEngine.Object.DestroyImmediate(Application.Bootstrapper.gameObject);
				Application.Bootstrapper = null;
			}
			yield return this.LoadRuntime();
			yield break;
		}

		protected virtual IEnumerator OnApplicationIgnitionStarted()
		{
			Application.maximumNumberOfDiagnosticsFiles = Application.Registry.GetValue<int>(Application.Registers.MaximumNumberOfDiagnosticsFiles, 10);
			if (Application.maximumNumberOfDiagnosticsFiles < 1)
			{
				Application.maximumNumberOfDiagnosticsFiles = 1;
			}
			string outputDebugFileName = Application.OutputDebugFileName;
			string outputDebugFileExtension = Path.GetExtension(Application.OutputDebugFileName);
			string outputDebugFileNameWithoutExtension = Path.GetFileNameWithoutExtension(outputDebugFileName);
			string outputDirectory = Path.GetDirectoryName(outputDebugFileName);
			Diagnostics.LogFormat diagnosticsLogFormat = this.DiagnosticsLogFormat;
			if (diagnosticsLogFormat == Diagnostics.LogFormat.Html)
			{
				outputDebugFileExtension = ".html";
			}
			if (Directory.Exists(outputDirectory))
			{
				string searchPattern = outputDebugFileNameWithoutExtension + "*.*";
				searchPattern = Path.ChangeExtension(searchPattern, outputDebugFileExtension);
				string[] files = Directory.GetFiles(outputDirectory, searchPattern);
				if (files.Length > Application.maximumNumberOfDiagnosticsFiles)
				{
					files = (from fileName in files
					orderby fileName
					select fileName).ToArray<string>();
					for (int fileIndex = 0; fileIndex < files.Length - Application.maximumNumberOfDiagnosticsFiles; fileIndex++)
					{
						try
						{
							File.Delete(files[fileIndex]);
						}
						catch
						{
						}
					}
				}
			}
			yield break;
		}

		protected virtual void OnApplicationQuit()
		{
			Diagnostics.Log("Flush diagnostics before closing streams.");
			Diagnostics.Flush();
			if (this.outputDepugFileStreamWriter != null)
			{
				this.outputDepugFileStreamWriter.Close();
				this.outputDepugFileStreamWriter.Dispose();
				this.outputDepugFileStreamWriter = null;
				Diagnostics.AssertionFailed -= this.Diagnostics_AssertionFailed;
				Diagnostics.MessageLogged -= this.Diagnostics_MessageLogged;
			}
			if (Application.Registry.Modified)
			{
				string directoryName = Path.GetDirectoryName(Application.RegistryFilePath);
				if (!Directory.Exists(directoryName))
				{
					Directory.CreateDirectory(directoryName);
				}
				Application.Registry.Export(Application.RegistryFilePath);
			}
		}

		protected virtual void OnDestroy()
		{
			Application.Instance = null;
		}

		protected override IEnumerator Start()
		{
			yield return base.StartCoroutine(base.Start());
			UnityCoroutine.StartCoroutine(this, this.Ignite(), new EventHandler<CoroutineExceptionEventArgs>(this.Ignite_CoroutineExceptionCallback));
			yield break;
		}

		protected virtual void Update()
		{
			if (Application.OnApplicationUpdate != null)
			{
				Application.OnApplicationUpdate();
			}
		}

		protected virtual void FormatDiagnosticsFileName(ref string outputDebugFileName)
		{
			outputDebugFileName = string.Format("{0} ({1})", outputDebugFileName, DateTime.Now.ToString("yyyy\\'MM\\'dd @HHmm\\'ss\\'\\'"));
		}

		private void Diagnostics_AssertionFailed(Diagnostics.LogMessage message)
		{
			if (this.outputDepugFileStreamWriter == null)
			{
				return;
			}
			Diagnostics.LogFormat diagnosticsLogFormat = this.DiagnosticsLogFormat;
			if (diagnosticsLogFormat != Diagnostics.LogFormat.Html)
			{
				if (diagnosticsLogFormat == Diagnostics.LogFormat.Text)
				{
					this.outputDepugFileStreamWriter.Write("[A] ");
					this.outputDepugFileStreamWriter.Write(DateTime.Now.ToString("HH:mm:ss:fff"));
					this.outputDepugFileStreamWriter.Write("  ");
					this.outputDepugFileStreamWriter.WriteLine(message.Message);
					this.Diagnostics_OutputDebugStackTrace(message);
					this.outputDepugFileStreamWriter.WriteLine();
				}
			}
			else
			{
				Diagnostics.LogType logType = (Diagnostics.LogType)((long)message.Flags & (long)((ulong)-16777216));
				this.outputDepugFileStreamWriter.WriteLine("<p class=\"{0}\"><span class=\"time\">{1}</span><a onclick=\"hide('trace0')\">STACK</a>{2}</p>", logType.ToString(), DateTime.Now.ToString("HH:mm:ss:fff"), message);
				this.outputDepugFileStreamWriter.WriteLine("<pre id=\"trace0\">");
				this.Diagnostics_OutputDebugStackTrace(message);
				this.outputDepugFileStreamWriter.WriteLine("</pre>");
			}
			this.outputDepugFileStreamWriter.Flush();
		}

		private void Diagnostics_MessageLogged(Diagnostics.LogMessage message)
		{
			if (this.outputDepugFileStreamWriter == null)
			{
				return;
			}
			Diagnostics.LogFormat diagnosticsLogFormat = this.DiagnosticsLogFormat;
			if (diagnosticsLogFormat != Diagnostics.LogFormat.Html)
			{
				if (diagnosticsLogFormat == Diagnostics.LogFormat.Text)
				{
					Diagnostics.LogType logType = (Diagnostics.LogType)((long)message.Flags & (long)((ulong)-16777216));
					Diagnostics.LogType logType2 = logType;
					if (logType2 != Diagnostics.LogType.Message)
					{
						if (logType2 == Diagnostics.LogType.Warning)
						{
							this.outputDepugFileStreamWriter.Write("[W] ");
							goto IL_140;
						}
						if (logType2 == Diagnostics.LogType.Error)
						{
							this.outputDepugFileStreamWriter.Write("[E] ");
							goto IL_140;
						}
					}
					this.outputDepugFileStreamWriter.Write("    ");
					IL_140:
					this.outputDepugFileStreamWriter.Write(DateTime.Now.ToString("HH:mm:ss:fff"));
					this.outputDepugFileStreamWriter.Write("  ");
					this.outputDepugFileStreamWriter.WriteLine(message.Message);
					this.Diagnostics_OutputDebugStackTrace(message);
					this.outputDepugFileStreamWriter.WriteLine();
				}
			}
			else
			{
				Diagnostics.LogType logType3 = (Diagnostics.LogType)((long)message.Flags & (long)((ulong)-16777216));
				this.outputDepugFileStreamWriter.WriteLine("<p class=\"{1}\"><span class=\"time\">{2}</span><a onclick=\"hide('trace{0}')\">STACK</a>{3}</p>", new object[]
				{
					this.DiagnosticsLogCounter,
					logType3.ToString(),
					DateTime.Now.ToString("HH:mm:ss:fff"),
					message.Message
				});
				this.outputDepugFileStreamWriter.WriteLine("<pre id=\"trace{0}\">", this.DiagnosticsLogCounter);
				this.Diagnostics_OutputDebugStackTrace(message);
				this.DiagnosticsLogCounter++;
				this.outputDepugFileStreamWriter.WriteLine("</pre>");
			}
			this.outputDepugFileStreamWriter.Flush();
		}

		private void Diagnostics_OutputDebugStackTrace(Diagnostics.LogMessage message)
		{
			StackFrame[] frames = message.StackTrace.GetFrames();
			int i;
			for (i = 0; i < frames.Length; i++)
			{
				if (frames[i] == message.StackFrame)
				{
					Diagnostics.LogFormat diagnosticsLogFormat = this.DiagnosticsLogFormat;
					if (diagnosticsLogFormat == Diagnostics.LogFormat.Text)
					{
						this.outputDepugFileStreamWriter.WriteLine();
					}
					break;
				}
			}
			StringBuilder stringBuilder = new StringBuilder();
			while (i < frames.Length)
			{
				stringBuilder.Length = 0;
				StackFrame stackFrame = frames[i];
				stringBuilder.AppendFormat("{0}:{1}(", stackFrame.GetMethod().ReflectedType.ToString(), stackFrame.GetMethod().Name);
				ParameterInfo[] parameters = stackFrame.GetMethod().GetParameters();
				if (parameters.Length > 0)
				{
					for (int j = 0; j < parameters.Length; j++)
					{
						if (j > 0)
						{
							stringBuilder.Append(", ");
						}
						stringBuilder.Append(parameters[j].GetType().ToString());
					}
				}
				stringBuilder.Append(")");
				if (!string.IsNullOrEmpty(stackFrame.GetFileName()))
				{
					stringBuilder.AppendFormat(" at {0}({1})", stackFrame.GetFileName(), stackFrame.GetFileLineNumber());
				}
				Diagnostics.LogFormat diagnosticsLogFormat = this.DiagnosticsLogFormat;
				if (diagnosticsLogFormat != Diagnostics.LogFormat.Html)
				{
					if (diagnosticsLogFormat == Diagnostics.LogFormat.Text)
					{
						this.outputDepugFileStreamWriter.WriteLine("               -  {0}", stringBuilder.ToString());
					}
				}
				else
				{
					this.outputDepugFileStreamWriter.WriteLine(stringBuilder.ToString());
				}
				i++;
			}
		}

		private void Ignite_CoroutineExceptionCallback(object sender, CoroutineExceptionEventArgs e)
		{
			Application.Quit();
			Diagnostics.LogError("An exception has been raised while initializing the application:\n{0}", new object[]
			{
				e.Exception.ToString()
			});
		}

		private IEnumerator LoadRuntime()
		{
			IRuntimeService runtimeService = Services.GetService<IRuntimeService>();
			if (runtimeService == null)
			{
				Application.Quit();
				yield break;
			}
			runtimeService.RuntimeException += this.RuntimeService_RuntimeException;
			runtimeService.RuntimeChange += this.RuntimeService_RuntimeChange;
			List<RuntimeModuleConfiguration> configuration = new List<RuntimeModuleConfiguration>();
			string[] commandLineArgs = Environment.GetCommandLineArgs();
			if (commandLineArgs != null && commandLineArgs.Length > 1)
			{
				for (int index = 1; index < commandLineArgs.Length; index++)
				{
					if (commandLineArgs[index].Equals("+mod", StringComparison.InvariantCultureIgnoreCase))
					{
						if (index >= commandLineArgs.Length - 1)
						{
							Diagnostics.LogError("Missing argument while parsing the command line, (+mod command requires additional module argument)...");
							Application.Quit();
							yield break;
						}
						RuntimeModuleConfiguration runtimeModuleConfiguration = new RuntimeModuleConfiguration(commandLineArgs[index]);
						configuration.Add(runtimeModuleConfiguration);
					}
				}
			}
			runtimeService.LoadRuntime(configuration.ToArray());
			yield break;
		}

		private void Manager_Ignite_CoroutineExceptionCallback(object sender, CoroutineExceptionEventArgs e)
		{
			Application.Quit();
			Diagnostics.LogError("An exception has been raised while initializing the application managers.\n{0}", new object[]
			{
				e.Exception.ToString()
			});
		}

		[Obsolete]
		private IEnumerator RestartSteamApplicationIfNecessary()
		{
			yield break;
		}

		private void RuntimeService_RuntimeChange(object sender, RuntimeChangeEventArgs e)
		{
			Diagnostics.Log("The runtime status has changed to '{0}'.", new object[]
			{
				e.Action.ToString()
			});
		}

		private void RuntimeService_RuntimeException(object sender, RuntimeExceptionEventArgs e)
		{
			Diagnostics.LogError("The runtime has raised an exception: \"{0}\"\n{1}", new object[]
			{
				e.Exception.Message,
				e.Exception.StackTrace
			});
			Application.Quit();
		}

		private IEnumerator SetupDiagnosticsLogFile()
		{
			string outputDebugPath = Application.OutputDebugFileName;
			string outputDebugFileExtension = Path.GetExtension(outputDebugPath);
			string outputDebugFileName = Path.GetFileNameWithoutExtension(outputDebugPath);
			string outputDebugDirectory = Path.GetDirectoryName(outputDebugPath);
			this.FormatDiagnosticsFileName(ref outputDebugFileName);
			outputDebugPath = Path.Combine(outputDebugDirectory, outputDebugFileName);
			outputDebugPath = Path.ChangeExtension(outputDebugPath, outputDebugFileExtension);
			try
			{
				Diagnostics.LogFormat diagnosticsLogFormat = this.DiagnosticsLogFormat;
				if (diagnosticsLogFormat != Diagnostics.LogFormat.Html)
				{
					if (diagnosticsLogFormat == Diagnostics.LogFormat.Text)
					{
						this.outputDepugFileStreamWriter = File.CreateText(outputDebugPath);
					}
				}
				else
				{
					outputDebugPath = Path.ChangeExtension(outputDebugPath, ".html");
					this.outputDepugFileStreamWriter = File.CreateText(outputDebugPath);
					this.outputDepugFileStreamWriter.WriteLine("<script language=\"javascript\" src=\"Diagnostics.js\">");
					this.outputDepugFileStreamWriter.WriteLine("</script>");
					this.outputDepugFileStreamWriter.WriteLine("<link rel=\"stylesheet\" type=\"text/css\" href=\"Diagnostics.css\" />");
					this.outputDepugFileStreamWriter.WriteLine("<div class=\"Header\">");
					this.outputDepugFileStreamWriter.WriteLine("  <input type=\"button\" value=\"Message\" class=\"Message Button\" onclick=\"hide_class('Message')\" />");
					this.outputDepugFileStreamWriter.WriteLine("  <input type=\"button\" value=\"Warning\" class=\"Warning Button\" onclick=\"hide_class('Warning')\" />");
					this.outputDepugFileStreamWriter.WriteLine("  <input type=\"button\" value=\"Error\" class=\"Error Button\" onclick=\"hide_class('Error')\" />");
					this.outputDepugFileStreamWriter.WriteLine("  <input type=\"button\" value=\"Assert\" class=\"Assert Button\" onclick=\"hide_class('Assert')\" />");
					this.outputDepugFileStreamWriter.WriteLine("<br />");
					this.outputDepugFileStreamWriter.WriteLine("</div>");
					this.outputDepugFileStreamWriter.WriteLine("<h1>{0}<br />{1} {2}</h1>", DateTime.Now.ToString("f"), Application.Name.ToUpper(), Application.Version.ToString());
					this.outputDepugFileStreamWriter.WriteLine("<p>Click on buttons to toggle visability. Click on STACK buttons to toggle visibility of stack traces.</p>");
				}
				Diagnostics.AssertionFailed += this.Diagnostics_AssertionFailed;
				Diagnostics.MessageLogged += this.Diagnostics_MessageLogged;
			}
			catch
			{
				throw;
			}
			yield break;
		}

		private IEnumerator Shutdown()
		{
			Diagnostics.Log("Shutting down...");
			yield return null;
			Diagnostics.Log("Shudown complete.");
			Application.Quit();
			yield break;
		}

		private IEnumerator SteamGetCurrentGameLanguage()
		{
			Steamworks.SteamApps steamApps = Steamworks.SteamAPI.SteamApps;
			if (steamApps != null)
			{
				string availableGameLanguages = steamApps.GetAvailableGameLanguages();
				string currentGameLanguage = steamApps.GetCurrentGameLanguage();
				Diagnostics.Log(string.Concat(new string[]
				{
					"[Steam] Available game languages are '",
					availableGameLanguages,
					"', current game language is '",
					currentGameLanguage,
					"'."
				}));
			}
			yield break;
		}

		private IEnumerator SteamGetSteamUserName()
		{
			Steamworks.SteamUser steamUser = Steamworks.SteamAPI.SteamUser;
			if (steamUser != null)
			{
				Steamworks.SteamID steamIDUser = Steamworks.SteamAPI.SteamUser.SteamID;
				Diagnostics.Log("[Steam] Steam user id is '" + steamIDUser + "'.");
				string userName = Steamworks.SteamAPI.SteamFriends.GetFriendPersonaName(steamIDUser);
				Diagnostics.Log("[Steam] Steam user name is '" + userName + "'.");
				Application.UserUniqueID = steamIDUser.AccountID;
				Application.UserName = userName;
			}
			yield break;
		}

		private IEnumerator SteamInitialize()
		{
			Steamworks.SteamAPI.Init();
			yield break;
		}

		private IEnumerator SteamRestartAppIfNecessary()
		{
			yield break;
		}

		public Diagnostics.LogFormat DiagnosticsLogFormat;

		private static int maximumNumberOfDiagnosticsFiles = 10;

		private StreamWriter outputDepugFileStreamWriter;

		private UnityCoroutine shutdown;

		public abstract class Preferences
		{
			public static string CustomCommandLineArguments
			{
				get
				{
					return Application.Preferences.customCommandLineArguments;
				}
			}

			public static bool EnableOfflineModeWhenSteamClientIsDown
			{
				get
				{
					return Application.Preferences.enableOfflineModeWhenSteamClientIsDown;
				}
			}

			public static bool EnableModdingTools
			{
				get
				{
					return Application.Preferences.enableModdingTools;
				}
				set
				{
					if (Application.Preferences.enableModdingTools != value)
					{
						Application.Preferences.enableModdingTools = value;
					}
				}
			}

			public static bool EnableMultiplayer
			{
				get
				{
					return Application.Preferences.enableMultiplayer;
				}
				set
				{
					if (Application.Preferences.enableMultiplayer != value)
					{
						Application.Preferences.enableMultiplayer = value;
					}
				}
			}

			private static string customCommandLineArguments = string.Empty;

			private static bool enableOfflineModeWhenSteamClientIsDown = true;

			private static bool enableModdingTools = false;

			private static bool enableMultiplayer = true;
		}

		public static class Registers
		{
			public static StaticString MaximumNumberOfDiagnosticsFiles = new StaticString("Settings/Diagnostics/MaximumNumberOfDiagnosticsFiles");
		}
	}
}
