using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Interop;
using Amplitude.IO;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Serialization;
using Amplitude.Unity.Session;
using Amplitude.Unity.Steam;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class GameManager : Amplitude.Unity.Game.GameManager, IService, IGameDiagnosticsService, IEndTurnControl, IEndTurnService, IGameSerializationService, ISynchronizationService
{
	public GameManager()
	{
		this.dumps = new Dictionary<string, string>();
		base..ctor();
	}

	public event EventHandler<SynchronizationStateChangedArgs> SynchronizationStateChanged;

	public event EventHandler EndTurnValidated;

	public event EventHandler EndTurnRequested;

	public event EventHandler<GameClientStateChangeEventArgs> GameClientStateChange;

	public event EventHandler<EndTurnTimeChangedEventArgs> EndTurnTimeChanged;

	public event EventHandler<GameSavingEventArgs> GameSaving;

	GameClientState IEndTurnControl.LastGameClientState
	{
		get
		{
			return this.lastGameClientState;
		}
	}

	void IEndTurnControl.NotifyOnGameClientStateChange(GameClientState gameClientState)
	{
		this.lastGameClientState = gameClientState;
		if (this.GameClientStateChange != null)
		{
			this.GameClientStateChange(this, new GameClientStateChangeEventArgs(gameClientState));
		}
	}

	bool IEndTurnControl.EndTurn()
	{
		return this.DoEndTurn(true);
	}

	public bool InjectTestDesync { get; set; }

	public string Dump(string tag)
	{
		StringBuilder stringBuilder = new StringBuilder();
		ISessionService service = Services.GetService<ISessionService>();
		Diagnostics.Assert(service != null);
		if (this.InjectTestDesync)
		{
			Random random = new Random((int)DateTime.Now.Ticks);
			stringBuilder.AppendFormat("TEST_DESYNC_TOKEN = {0:x8}\r\n", random.Next());
		}
		IDumpable dumpable = this.Game as IDumpable;
		dumpable.DumpAsText(stringBuilder, string.Empty);
		MD5 md = MD5.Create();
		byte[] source = md.ComputeHash(Encoding.UTF8.GetBytes(stringBuilder.ToString()));
		string text = source.Aggregate(string.Empty, (string current, byte b) => current + b.ToString("x2"));
		string text2 = string.Empty;
		if (service.Session.SteamIDUser != Steamworks.SteamID.Zero)
		{
			text2 = Steamworks.SteamAPI.SteamFriends.GetFriendPersonaName(service.Session.SteamIDUser);
			text2 = text2.Replace(' ', '-');
			char[] invalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();
			text2 = invalidFileNameChars.Aggregate(text2, (string current, char invalidChar) => current.Replace(invalidChar, '#'));
		}
		string path = string.Format("{0:D3}_{2}_{3}_0x{1}.dump", new object[]
		{
			this.Turn,
			text,
			tag,
			text2
		});
		string text3 = System.IO.Path.Combine(global::Application.DumpFilesDirectory, path);
		if (!Directory.Exists(global::Application.DumpFilesDirectory))
		{
			Directory.CreateDirectory(global::Application.DumpFilesDirectory);
		}
		using (StreamWriter streamWriter = new StreamWriter(text3))
		{
			streamWriter.Write(stringBuilder);
		}
		string key = string.Format("{0:D3}{1}", this.Turn, tag);
		if (!this.dumps.ContainsKey(key))
		{
			this.dumps.Add(key, text3);
		}
		else
		{
			this.dumps[key] = text3;
		}
		Diagnostics.Log("[Dump] " + text3);
		return text;
	}

	public string ComputeChecksum()
	{
		string result = string.Empty;
		ISessionService service = Services.GetService<ISessionService>();
		Diagnostics.Assert(service != null);
		using (MemoryStream memoryStream = new MemoryStream())
		{
			using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
			{
				if (this.InjectTestDesync)
				{
					Random random = new Random((int)DateTime.Now.Ticks);
					binaryWriter.Write(random.Next());
				}
				IDumpable dumpable = this.Game as IDumpable;
				binaryWriter.Write(dumpable.DumpAsBytes());
				MD5 md = MD5.Create();
				byte[] source = md.ComputeHash(memoryStream.ToArray());
				result = source.Aggregate(string.Empty, (string current, byte b) => current + b.ToString("x2"));
			}
		}
		return result;
	}

	public int ClearDumpFiles()
	{
		this.dumps.Clear();
		if (Directory.Exists(global::Application.DumpFilesDirectory))
		{
			string[] files = Directory.GetFiles(global::Application.DumpFilesDirectory);
			for (int i = 0; i < files.Length; i++)
			{
				File.Delete(files[i]);
			}
			Diagnostics.Log("[Dump] {0} files removed.", new object[]
			{
				files.Length
			});
			return files.Length;
		}
		return 0;
	}

	public string RetrieveDumpPath(string key)
	{
		string text;
		return (!this.dumps.TryGetValue(key, out text)) ? null : text;
	}

	public void NotifySynchronizationStateChanged(object sender, SynchronizationStateChangedArgs e)
	{
		if (e == null)
		{
			throw new ArgumentNullException("e");
		}
		if (this.SynchronizationStateChanged != null)
		{
			this.SynchronizationStateChanged(sender, e);
		}
	}

	public int Turn
	{
		get
		{
			if (this.Game == null)
			{
				return 0;
			}
			return (this.Game as global::Game).Turn;
		}
	}

	public double EndTurnTime { get; private set; }

	public double EndTurnDuration { get; private set; }

	public bool CanEndTurn()
	{
		bool flag = true;
		for (int i = 0; i < this.canExecuteValidators.Count; i++)
		{
			flag &= this.canExecuteValidators[i]();
		}
		return flag;
	}

	public void RegisterCanExecuteValidator(Func<bool> canExecuteValidator)
	{
		if (this.canExecuteValidators.Contains(canExecuteValidator))
		{
			return;
		}
		this.canExecuteValidators.Add(canExecuteValidator);
	}

	public void RegisterValidator(Func<bool, bool> endTurnValidator)
	{
		if (this.endTurnValidators.Contains(endTurnValidator))
		{
			return;
		}
		this.endTurnValidators.Add(endTurnValidator);
	}

	public bool TryToEndTurn()
	{
		bool force = this.canForceEndTurn && this.alreadyAskEndTurn;
		return this.DoEndTurn(force);
	}

	public void UnregisterCanExecuteValidator(Func<bool> canExecuteValidator)
	{
		this.canExecuteValidators.Remove(canExecuteValidator);
	}

	public void UnregisterValidator(Func<bool, bool> endTurnValidator)
	{
		this.endTurnValidators.Remove(endTurnValidator);
	}

	public void ChangeEndTurnTime(double endTurnTime, double endTurnTimerDuration)
	{
		this.EndTurnTime = endTurnTime;
		this.EndTurnDuration = endTurnTimerDuration;
		if (this.EndTurnTimeChanged != null)
		{
			this.EndTurnTimeChanged(this, new EndTurnTimeChangedEventArgs(endTurnTime, endTurnTimerDuration));
		}
	}

	private bool DoEndTurn(bool force)
	{
		this.OnEndTurnRequested();
		bool flag = this.CanEndTurn();
		this.alreadyAskEndTurn = true;
		for (int i = 0; i < this.endTurnValidators.Count; i++)
		{
			flag &= this.endTurnValidators[i](force);
		}
		if (flag)
		{
			this.OnEndTurnValidated();
			this.alreadyAskEndTurn = false;
		}
		return flag;
	}

	private void OnEndTurnRequested()
	{
		if (this.EndTurnRequested != null)
		{
			this.EndTurnRequested(this, new EventArgs());
		}
	}

	private void OnEndTurnValidated()
	{
		if (this.EndTurnValidated != null)
		{
			this.EndTurnValidated(this, new EventArgs());
		}
	}

	public GameSaveDescriptor GameSaveDescriptor { get; set; }

	public GameSaveDescriptor GetMostRecentGameSaveDescritor()
	{
		string gameSaveDirectory = global::Application.GameSaveDirectory;
		if (string.IsNullOrEmpty(gameSaveDirectory))
		{
			return null;
		}
		if (!Directory.Exists(gameSaveDirectory))
		{
			return null;
		}
		ISerializationService service = Services.GetService<ISerializationService>();
		if (service == null)
		{
			return null;
		}
		List<string> list = null;
		IRuntimeService service2 = Services.GetService<IRuntimeService>();
		if (service2 != null)
		{
			Diagnostics.Assert(service2.Runtime != null);
			Diagnostics.Assert(service2.Runtime.RuntimeModules != null);
			list = (from runtimeModule in service2.Runtime.RuntimeModules
			select runtimeModule.Name).ToList<string>();
		}
		DirectoryInfo directoryInfo = new DirectoryInfo(gameSaveDirectory);
		List<FileInfo> list2 = new List<FileInfo>();
		list2.AddRange(directoryInfo.GetFiles("*.zip"));
		list2.AddRange(directoryInfo.GetFiles("*.sav"));
		FileInfo[] array = (from f in list2
		orderby f.LastWriteTime descending
		select f).ToArray<FileInfo>();
		GameSaveDescriptor gameSaveDescriptor = null;
		if (array != null)
		{
			foreach (FileInfo fileInfo in array)
			{
				Archive archive = null;
				try
				{
					archive = Archive.Open(fileInfo.FullName, ArchiveMode.Open);
					MemoryStream stream = null;
					if (archive.TryGet(global::GameManager.GameSaveDescriptorFileName, out stream))
					{
						XmlReaderSettings settings = new XmlReaderSettings
						{
							CloseInput = true,
							IgnoreComments = true,
							IgnoreProcessingInstructions = true,
							IgnoreWhitespace = true
						};
						using (System.Xml.XmlReader xmlReader = System.Xml.XmlReader.Create(stream, settings))
						{
							if (xmlReader.ReadToDescendant("GameSaveDescriptor"))
							{
								XmlSerializer xmlSerializer = service.GetXmlSerializer<GameSaveDescriptor>();
								gameSaveDescriptor = (xmlSerializer.Deserialize(xmlReader) as GameSaveDescriptor);
								gameSaveDescriptor.SourceFileName = fileInfo.FullName;
								if (gameSaveDescriptor.Version.Serial != Amplitude.Unity.Framework.Application.Version.Serial)
								{
									gameSaveDescriptor = null;
								}
								if (gameSaveDescriptor != null)
								{
									if (gameSaveDescriptor.RuntimeModules == null)
									{
										if (service2 != null)
										{
											if (list.Count != 1 || !(list[0] == service2.VanillaModuleName))
											{
												gameSaveDescriptor = null;
											}
										}
										else
										{
											gameSaveDescriptor = null;
										}
									}
									else
									{
										List<string> list3 = list.Except(gameSaveDescriptor.RuntimeModules).ToList<string>();
										List<string> list4 = gameSaveDescriptor.RuntimeModules.Except(list).ToList<string>();
										if (list3.Count + list4.Count != 0)
										{
											gameSaveDescriptor = null;
										}
									}
								}
							}
						}
					}
				}
				catch (Exception ex)
				{
					Diagnostics.LogWarning("Exception caught: " + ex.ToString());
					gameSaveDescriptor = null;
				}
				finally
				{
					if (archive != null)
					{
						archive.Close();
					}
				}
				if (gameSaveDescriptor != null)
				{
					break;
				}
			}
		}
		return gameSaveDescriptor;
	}

	public IEnumerable<GameSaveDescriptor> GetListOfGameSaveDescritors(bool withAutoSave)
	{
		string path = global::Application.GameSaveDirectory;
		if (string.IsNullOrEmpty(path))
		{
			yield break;
		}
		if (!Directory.Exists(path))
		{
			yield break;
		}
		ISerializationService serializationService = Services.GetService<ISerializationService>();
		if (serializationService == null)
		{
			yield break;
		}
		XmlSerializer serializer = serializationService.GetXmlSerializer<GameSaveDescriptor>();
		DirectoryInfo directory = new DirectoryInfo(path);
		List<FileInfo> compatibleFiles = new List<FileInfo>();
		compatibleFiles.AddRange(directory.GetFiles("*.zip"));
		compatibleFiles.AddRange(directory.GetFiles("*.sav"));
		FileInfo[] files = (from f in compatibleFiles
		orderby f.LastWriteTime descending
		select f).ToArray<FileInfo>();
		Archive archive = null;
		GameSaveDescriptor gameSaveDescriptor = null;
		XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
		{
			CloseInput = true,
			IgnoreComments = true,
			IgnoreProcessingInstructions = true,
			IgnoreWhitespace = true
		};
		for (int i = 0; i < files.Length; i++)
		{
			gameSaveDescriptor = null;
			string fileName = files[i].FullName;
			try
			{
				archive = Archive.Open(fileName, ArchiveMode.Open);
				MemoryStream stream = null;
				if (archive.TryGet(global::GameManager.GameSaveDescriptorFileName, out stream))
				{
					using (System.Xml.XmlReader reader = System.Xml.XmlReader.Create(stream, xmlReaderSettings))
					{
						if (reader.ReadToDescendant("GameSaveDescriptor"))
						{
							gameSaveDescriptor = (serializer.Deserialize(reader) as GameSaveDescriptor);
							gameSaveDescriptor.SourceFileName = fileName;
							if (gameSaveDescriptor.Version.Serial != Amplitude.Unity.Framework.Application.Version.Serial)
							{
								gameSaveDescriptor = null;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Exception exception = ex;
				Diagnostics.LogWarning("Exception caught: " + exception.ToString());
				gameSaveDescriptor = null;
			}
			finally
			{
				if (archive != null)
				{
					archive.Close();
				}
			}
			if (gameSaveDescriptor != null && gameSaveDescriptor.Closed)
			{
				gameSaveDescriptor = null;
			}
			if (gameSaveDescriptor != null && (withAutoSave || !gameSaveDescriptor.Title.StartsWith(global::GameManager.AutoSaveFileName)))
			{
				yield return gameSaveDescriptor;
			}
		}
		yield break;
	}

	public int ProcessIncremental(string path, string fileNameWithoutExtension)
	{
		int num = 0;
		DirectoryInfo directoryInfo = new DirectoryInfo(path);
		if (!directoryInfo.Exists)
		{
			directoryInfo.Create();
		}
		FileInfo[] files = directoryInfo.GetFiles(fileNameWithoutExtension + "*.sav");
		if (global::GameManager.maximumNumberOfAutoSaveFiles <= files.Length)
		{
			List<FileInfo> list = new List<FileInfo>(files);
			list.Sort((FileInfo left, FileInfo right) => -1 * left.LastWriteTime.CompareTo(right.LastWriteTime));
			do
			{
				int index = list.Count - 1;
				FileInfo fileInfo = list[index];
				list.RemoveAt(index);
				try
				{
					File.Delete(fileInfo.FullName);
				}
				catch (Exception ex)
				{
					Diagnostics.LogWarning("(Harmless?) Exception caught while attempting to delete file '{0}'.\nException: {1}", new object[]
					{
						fileInfo.FullName,
						ex
					});
				}
			}
			while (global::GameManager.maximumNumberOfAutoSaveFiles <= list.Count);
		}
		foreach (FileInfo fileInfo2 in files)
		{
			string fileNameWithoutExtension2 = System.IO.Path.GetFileNameWithoutExtension(fileInfo2.FullName);
			int num2 = fileNameWithoutExtension2.LastIndexOf(' ');
			int val;
			if (num2 != -1 && int.TryParse(fileNameWithoutExtension2.Substring(num2 + 1), out val))
			{
				num = Math.Max(num, val);
			}
		}
		return num + 1;
	}

	public IEnumerator SaveGameAsync(string title, string outputFileName, GameSaveOptions gameSaveOptions)
	{
		if (string.IsNullOrEmpty(outputFileName))
		{
			throw new ArgumentException("outputFileName");
		}
		string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(outputFileName);
		string fileNameExtension = System.IO.Path.GetExtension(outputFileName);
		string path = System.IO.Path.GetDirectoryName(outputFileName);
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
		}
		if ((gameSaveOptions & GameSaveOptions.QuickSave) == GameSaveOptions.QuickSave)
		{
			title = "%QuickSaveFileName";
			fileNameWithoutExtension = "QuickSave";
		}
		if ((gameSaveOptions & GameSaveOptions.AutoSave) == GameSaveOptions.AutoSave)
		{
			title = "%AutoSaveFileName";
			fileNameWithoutExtension = "AutoSave";
			if (global::GameManager.maximumNumberOfAutoSaveFiles == 0)
			{
				yield break;
			}
			if ((gameSaveOptions & GameSaveOptions.Incremental) == GameSaveOptions.Incremental)
			{
				int number = this.ProcessIncremental(path, fileNameWithoutExtension);
				fileNameWithoutExtension = string.Format("{0} {1}", fileNameWithoutExtension, number);
			}
		}
		outputFileName = System.IO.Path.Combine(path, string.Format("{0}{1}", fileNameWithoutExtension, fileNameExtension));
		MemoryStream stream = new MemoryStream();
		try
		{
			this.SaveGame(stream);
			ISessionService sessionService = Services.GetService<ISessionService>();
			Diagnostics.Assert(sessionService != null);
			Diagnostics.Assert(sessionService.Session != null);
			if (this.GameSaveDescriptor == null)
			{
				this.GameSaveDescriptor = new GameSaveDescriptor();
				using (WorldGenerator worldGenerator = new WorldGenerator())
				{
					this.GameSaveDescriptor.SourceFileName = worldGenerator.GetOuputPath();
					if (File.Exists(this.GameSaveDescriptor.SourceFileName))
					{
						this.GameSaveDescriptor.Source = File.ReadAllBytes(this.GameSaveDescriptor.SourceFileName);
					}
				}
				Diagnostics.Assert(this.Game is global::Game);
				Diagnostics.Assert(sessionService.Session != null);
				global::Game game = this.Game as global::Game;
				int turn = Math.Max(0, game.Turn - 1);
				this.GameSaveDescriptor.GameSaveSessionDescriptor.TrackUserPresence(game, sessionService.Session, turn);
				this.GameSaveDescriptor.GUID = Guid.NewGuid();
			}
			this.GameSaveDescriptor.DateTime = DateTime.Now;
			this.GameSaveDescriptor.Title = title;
			this.GameSaveDescriptor.Version = Amplitude.Unity.Framework.Application.Version;
			this.GameSaveDescriptor.Turn = (this.Game as global::Game).Turn;
			if ((gameSaveOptions & GameSaveOptions.Closed) == GameSaveOptions.Closed)
			{
				this.GameSaveDescriptor.Closed = true;
			}
			IRuntimeService runtimeService = Services.GetService<IRuntimeService>();
			if (runtimeService != null && runtimeService.Runtime != null)
			{
				Diagnostics.Assert(runtimeService.Runtime.RuntimeModules != null);
				this.GameSaveDescriptor.RuntimeModules = (from runtimeModule in runtimeService.Runtime.RuntimeModules
				select runtimeModule.Name).ToArray<string>();
			}
			this.GameSaveDescriptor.GameSaveSessionDescriptor.SessionMode = sessionService.Session.SessionMode;
			this.GameSaveDescriptor.GameSaveSessionDescriptor.SetLobbyData(sessionService.Session);
			outputFileName = System.IO.Path.ChangeExtension(outputFileName, "sav");
			if (File.Exists(this.GameSaveDescriptor.SourceFileName))
			{
				try
				{
					if (outputFileName != this.GameSaveDescriptor.SourceFileName)
					{
						File.Copy(this.GameSaveDescriptor.SourceFileName, outputFileName, true);
					}
				}
				catch (Exception ex)
				{
					Exception exception = ex;
					if (!(exception is IOException))
					{
						throw;
					}
					if (!File.Exists(outputFileName))
					{
						throw;
					}
				}
			}
			else
			{
				if (this.GameSaveDescriptor.Source == null)
				{
					throw new FileNotFoundException("The source file is missing; it may have been deleted?", this.GameSaveDescriptor.SourceFileName);
				}
				try
				{
					Diagnostics.LogWarning("Using binary source to restore the game archive...");
					File.WriteAllBytes(outputFileName, this.GameSaveDescriptor.Source);
				}
				catch
				{
					throw;
				}
			}
			using (Archive archive = Archive.Open(outputFileName, ArchiveMode.OpenOrCreate))
			{
				archive.Add(global::GameManager.GameFileName, stream, CompressionMethod.Compressed);
				stream.Seek(0L, SeekOrigin.Begin);
				stream.SetLength(0L);
				this.SaveGameDescriptor(stream);
				if (stream.Length > 0L)
				{
					archive.Add(global::GameManager.GameSaveDescriptorFileName, stream, CompressionMethod.Compressed);
				}
				if (this.GameSaving != null)
				{
					this.GameSaving(this, new GameSavingEventArgs(archive));
				}
			}
			this.GameSaveDescriptor.SourceFileName = outputFileName;
		}
		catch
		{
			throw;
		}
		if (stream != null)
		{
			stream.Close();
			stream = null;
		}
		yield break;
	}

	public void RegisterExternalSerializableObject(string fileName, Action<Stream> saveExternalObjectCallback)
	{
		if (this.externalObjects.ContainsKey(fileName))
		{
			return;
		}
		this.externalObjects.Add(fileName, saveExternalObjectCallback);
	}

	public bool TryExtractGameSaveDescriptorFromFile(string outputFilePath, out GameSaveDescriptor gameSaveDescriptor, bool makeCurrent)
	{
		gameSaveDescriptor = null;
		if (File.Exists(outputFilePath))
		{
			Archive archive = null;
			try
			{
				archive = Archive.Open(outputFilePath, ArchiveMode.Open);
				MemoryStream stream = null;
				if (archive.TryGet(global::GameManager.GameSaveDescriptorFileName, out stream))
				{
					XmlReaderSettings settings = new XmlReaderSettings
					{
						CloseInput = true,
						IgnoreComments = true,
						IgnoreProcessingInstructions = true,
						IgnoreWhitespace = true
					};
					using (System.Xml.XmlReader xmlReader = System.Xml.XmlReader.Create(stream, settings))
					{
						if (xmlReader.ReadToDescendant("GameSaveDescriptor"))
						{
							ISerializationService service = Services.GetService<ISerializationService>();
							XmlSerializer xmlSerializer;
							if (service != null)
							{
								xmlSerializer = service.GetXmlSerializer<GameSaveDescriptor>();
							}
							else
							{
								xmlSerializer = new XmlSerializer(typeof(GameSaveDescriptor));
							}
							gameSaveDescriptor = (xmlSerializer.Deserialize(xmlReader) as GameSaveDescriptor);
							gameSaveDescriptor.SourceFileName = outputFilePath;
							if (gameSaveDescriptor.Version.Serial != Amplitude.Unity.Framework.Application.Version.Serial)
							{
								Diagnostics.LogError("Invalid game save; application version does not match.");
								gameSaveDescriptor = null;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Diagnostics.LogWarning("Exception caught: " + ex.ToString());
				gameSaveDescriptor = null;
			}
			finally
			{
				if (archive != null)
				{
					archive.Close();
				}
			}
			if (gameSaveDescriptor != null)
			{
				if (makeCurrent)
				{
					this.GameSaveDescriptor = gameSaveDescriptor;
				}
				return true;
			}
		}
		return false;
	}

	private void SaveGame(Stream stream)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		if (!stream.CanWrite)
		{
			throw new ArgumentException("stream");
		}
		using (Amplitude.Xml.XmlWriter xmlWriter = Amplitude.Xml.XmlWriter.Create(stream))
		{
			xmlWriter.Writer.WriteStartDocument();
			xmlWriter.WriteStartElement(base.GetType().ToString());
			xmlWriter.WriteAttributeString("DateTime", DateTime.Now.ToString("U"));
			Amplitude.Xml.Serialization.IXmlSerializable xmlSerializable = this.Game as Amplitude.Xml.Serialization.IXmlSerializable;
			xmlWriter.WriteElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>(ref xmlSerializable);
			xmlWriter.WriteEndElement();
		}
	}

	private void SaveGameDescriptor(Stream stream)
	{
		ISerializationService service = Services.GetService<ISerializationService>();
		if (service != null)
		{
			XmlWriterSettings settings = new XmlWriterSettings
			{
				Encoding = Encoding.UTF8,
				Indent = true,
				IndentChars = "  ",
				NewLineChars = "\r\n",
				NewLineHandling = NewLineHandling.Replace,
				OmitXmlDeclaration = true
			};
			using (System.Xml.XmlWriter xmlWriter = System.Xml.XmlWriter.Create(stream, settings))
			{
				xmlWriter.WriteStartDocument();
				XmlSerializer xmlSerializer = service.GetXmlSerializer<GameSaveDescriptor>();
				xmlSerializer.Serialize(xmlWriter, this.GameSaveDescriptor);
				xmlWriter.WriteEndDocument();
				xmlWriter.Flush();
				xmlWriter.Close();
			}
		}
	}

	public static bool IsInGame
	{
		get
		{
			IGameService service = Services.GetService<IGameService>();
			return service != null && service.Game != null;
		}
	}

	public override IEnumerator BindServices()
	{
		yield return base.BindServices();
		Services.AddService<IGameSerializationService>(this);
		Services.AddService<IEndTurnService>(this);
		Services.AddService<ISynchronizationService>(this);
		Services.AddService<IGameDiagnosticsService>(this);
		if (base.LastError == 0 && Steamworks.SteamAPI.IsSteamRunning)
		{
			base.SetLastError(0, "Waiting for service dependencies...");
			yield return base.BindService<ISteamNetworkingService>(delegate(ISteamNetworkingService service)
			{
				this.RegisterSteamNetworkingMessageClasses(service);
			});
		}
		global::GameManager.maximumNumberOfAutoSaveFiles = Amplitude.Unity.Framework.Application.Registry.GetValue<int>("Settings/AutoSave/MaximumNumberOfAutoSaveFiles", 4);
		if (global::GameManager.maximumNumberOfAutoSaveFiles < 0)
		{
			global::GameManager.maximumNumberOfAutoSaveFiles = 0;
		}
		yield break;
	}

	protected override IEnumerator OnCreateGameAsync()
	{
		yield return base.OnCreateGameAsync();
		global::Game game = this.Game as global::Game;
		if (game == null)
		{
			throw new GameException("Invalid null game.");
		}
		string pathToArchive = null;
		if (this.GameSaveDescriptor != null)
		{
			Diagnostics.Log("Loading from archive '{0}'...", new object[]
			{
				this.GameSaveDescriptor.SourceFileName
			});
			pathToArchive = this.GameSaveDescriptor.SourceFileName;
			if (File.Exists(pathToArchive))
			{
				this.GameSaveDescriptor.Source = File.ReadAllBytes(pathToArchive);
			}
			game.Turn = this.GameSaveDescriptor.Turn;
			this.GameSaveDescriptor.Reloads++;
		}
		else
		{
			using (WorldGenerator worldGenerator = new WorldGenerator())
			{
				yield return worldGenerator.WriteConfigurationFile();
				yield return worldGenerator.GenerateWorld();
				Diagnostics.Log("The world generation has ended.");
				pathToArchive = worldGenerator.GetOuputPath();
			}
		}
		Diagnostics.Log("Using path to archive: '{0}'", new object[]
		{
			pathToArchive
		});
		using (Archive archive = Archive.Open(pathToArchive, ArchiveMode.Open))
		{
			yield return game.Launch(archive);
		}
		yield break;
	}

	protected override void OnGameChange(GameChangeEventArgs e)
	{
		base.OnGameChange(e);
		GameChangeAction action = e.Action;
		if (action == GameChangeAction.Releasing || action == GameChangeAction.Released)
		{
			this.lastGameClientState = null;
		}
	}

	private void RegisterSteamNetworkingMessageClasses(ISteamNetworkingService steamNetworkingService)
	{
		if (steamNetworkingService == null)
		{
			base.SetLastError(10, "The steam networking service is down; cannot register the game client & server messages for translation.");
			return;
		}
		steamNetworkingService.RegisterMessageClass<GameClientPostOrderMessage>();
		steamNetworkingService.RegisterMessageClass<GameClientPostOrderResponseMessage>();
		steamNetworkingService.RegisterMessageClass<GameClientStateMessage>();
		steamNetworkingService.RegisterMessageClass<GameClientChatMessage>();
		steamNetworkingService.RegisterMessageClass<GameClientDownloadGameMessage>();
		steamNetworkingService.RegisterMessageClass<GameServerPostOrderMessage>();
		steamNetworkingService.RegisterMessageClass<GameServerPostOrderResponseMessage>();
		steamNetworkingService.RegisterMessageClass<GameServerPostStateChangeMessage>();
		steamNetworkingService.RegisterMessageClass<GameServerChatMessage>();
		steamNetworkingService.RegisterMessageClass<GameServerDownloadGameResponseMessage>();
		steamNetworkingService.RegisterMessageClass<GameServerEndTurnTimerMessage>();
		base.SetLastError(0, "All steam networking messages have been registered.");
	}

	public static DumpingMethod DumpingMethod;

	public static int DumpingCoolingOffPeriod;

	private readonly Dictionary<string, string> dumps;

	private List<Func<bool, bool>> endTurnValidators = new List<Func<bool, bool>>();

	private List<Func<bool>> canExecuteValidators = new List<Func<bool>>();

	private bool alreadyAskEndTurn;

	private bool canForceEndTurn = true;

	private GameClientState lastGameClientState;

	public static readonly string GameFileName = "Endless Legend/Game.xml";

	public static readonly string GameSaveDescriptorFileName = "Endless Legend/GameSaveDescriptor.xml";

	public static readonly string AutoSaveFileName = "%AutoSaveFileName";

	public static readonly string QuickSaveFileName = "%QuickSaveFileName";

	private static int maximumNumberOfAutoSaveFiles = 4;

	private Dictionary<string, Action<Stream>> externalObjects = new Dictionary<string, Action<Stream>>();

	public abstract class Preferences
	{
		static Preferences()
		{
			global::GameManager.Preferences.gameGraphicSettings = new GameGraphicSettings();
			global::GameManager.Preferences.gameplayGraphicOptions = new GameplayGraphicOptions();
		}

		public static bool EnableDebugOverlay
		{
			get
			{
				return global::GameManager.Preferences.displayDebugInformation;
			}
		}

		public static bool EnableStepByStepBattle
		{
			get
			{
				return global::GameManager.Preferences.stepByStepBattle;
			}
		}

		public static bool EnableSkipBattleTargetingPhases
		{
			get
			{
				return global::GameManager.Preferences.skipBattleTargetingPhases;
			}
		}

		public static bool UseWeightedDeployment
		{
			get
			{
				return global::GameManager.Preferences.useWeightedDeployment;
			}
		}

		public static bool DisplayEnemyTargeting
		{
			get
			{
				return global::GameManager.Preferences.displayEnemyTargeting;
			}
		}

		public static bool DisplayApprovalWithFids
		{
			get
			{
				return global::GameManager.Preferences.displayApprovalWithFids;
			}
		}

		public static bool QuestVerboseMode
		{
			get
			{
				return global::GameManager.Preferences.questVerboseMode;
			}
		}

		public static GameGraphicSettings GameGraphicSettings
		{
			get
			{
				return global::GameManager.Preferences.gameGraphicSettings;
			}
		}

		public static GameplayGraphicOptions GameplayGraphicOptions
		{
			get
			{
				return global::GameManager.Preferences.gameplayGraphicOptions;
			}
		}

		private static GameGraphicSettings gameGraphicSettings;

		private static GameplayGraphicOptions gameplayGraphicOptions;

		private static bool displayDebugInformation = true;

		private static bool stepByStepBattle;

		private static bool skipBattleTargetingPhases;

		private static bool useWeightedDeployment = true;

		private static bool displayEnemyTargeting;

		private static bool displayApprovalWithFids;

		private static bool questVerboseMode;
	}
}
