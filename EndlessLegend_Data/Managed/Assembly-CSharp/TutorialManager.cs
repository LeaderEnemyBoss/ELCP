using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Extensions;
using Amplitude.IO;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Serialization;
using Amplitude.Unity.Simulation;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class TutorialManager : GameAncillary, Amplitude.Xml.Serialization.IXmlSerializable, IService, ITutorialService
{
	string ITutorialService.GetValue(StaticString key)
	{
		string result;
		if (this.registers.TryGetValue(key, out result))
		{
			return result;
		}
		return null;
	}

	T ITutorialService.GetValue<T>(StaticString key)
	{
		string value;
		if (this.registers.TryGetValue(key, out value))
		{
			return (T)((object)Convert.ChangeType(value, typeof(T)));
		}
		return default(T);
	}

	T ITutorialService.GetValue<T>(StaticString key, T defaultValue)
	{
		string value;
		if (this.registers.TryGetValue(key, out value))
		{
			return (T)((object)Convert.ChangeType(value, typeof(T)));
		}
		return defaultValue;
	}

	void ITutorialService.SetValue<T>(StaticString key, T value)
	{
		((ITutorialService)this).SetValue(key, value.ToString());
	}

	void ITutorialService.SetValue(StaticString key, string value)
	{
		if (!string.IsNullOrEmpty(key))
		{
			if (this.registers.ContainsKey(key))
			{
				if (!string.IsNullOrEmpty(value))
				{
					this.registers[key] = value;
				}
				else
				{
					this.registers.Remove(key);
				}
			}
			else if (!string.IsNullOrEmpty(value))
			{
				this.registers.Add(key, value);
			}
		}
	}

	bool ITutorialService.TryGetValue(StaticString key, out string value)
	{
		return this.registers.TryGetValue(key, out value);
	}

	bool ITutorialService.TryGetValue<T>(StaticString key, out T value)
	{
		string value2;
		if (this.registers.TryGetValue(key, out value2))
		{
			value = (T)((object)Convert.ChangeType(value2, typeof(T)));
			return true;
		}
		value = default(T);
		return false;
	}

	public virtual void ReadXml(Amplitude.Xml.XmlReader reader)
	{
		this.IsActive = reader.GetAttribute<bool>("IsActive");
		reader.ReadStartElement();
		this.registers.Clear();
		int attribute = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("Registers");
		for (int i = 0; i < attribute; i++)
		{
			StaticString key = reader.GetAttribute("Key");
			reader.ReadStartElement("Register");
			string value = reader.ReadString();
			this.registers.Add(key, value);
			reader.ReadEndElement("Register");
		}
		reader.ReadEndElement("Registers");
		if (this.IsActive)
		{
			TutorialInstructionPanel.WaitingInstruction.Title = reader.GetAttribute("Title");
			TutorialInstructionPanel.WaitingInstruction.Content = reader.GetAttribute("Content");
			TutorialInstructionPanel.WaitingInstruction.Action = reader.GetAttribute("Action");
			TutorialInstructionPanel.WaitingInstruction.Modal = reader.GetAttribute<bool>("Modal");
			TutorialInstructionPanel.WaitingInstruction.WheelGrabber = reader.GetAttribute<bool>("WheelGrabber");
			TutorialInstructionPanel.WaitingInstruction.DisplayNextButton = reader.GetAttribute<bool>("DisplayNextButton");
			TutorialInstructionPanel.WaitingInstruction.OverNotifications = reader.GetAttribute<bool>("OverNotifications");
			TutorialInstructionPanel.WaitingInstruction.OverModals = reader.GetAttribute<bool>("OverModals");
			TutorialInstructionPanel.WaitingInstruction.WaitForPanelTypeName = reader.GetAttribute("WaitForPanelTypeName");
			TutorialInstructionPanel.WaitingInstruction.WaitForPanelName = reader.GetAttribute("WaitForPanelName");
			TutorialInstructionPanel.WaitingInstruction.Placement = (TutorialInstructionPlacement)((int)Enum.Parse(typeof(TutorialInstructionPlacement), reader.GetAttribute("Placement")));
			TutorialInstructionPanel.WaitingInstruction.Frame = 0;
			reader.ReadStartElement("InstructionPanel");
			reader.ReadEndElement("InstructionPanel");
			TutorialHighlightPanel.WaitingHighlight.Content = reader.GetAttribute("Content");
			TutorialHighlightPanel.WaitingHighlight.TargetName = reader.GetAttribute("TargetName");
			TutorialHighlightPanel.WaitingHighlight.RectangularHighlight = reader.GetAttribute<bool>("Rectangular");
			reader.ReadStartElement("HighlightPanel");
			reader.ReadEndElement("HighlightPanel");
		}
	}

	public virtual void WriteXml(Amplitude.Xml.XmlWriter writer)
	{
		writer.WriteAttributeString("AssemblyQualifiedName", base.GetType().AssemblyQualifiedName);
		writer.WriteAttributeString<bool>("IsActive", this.IsActive);
		writer.WriteStartElement("Registers");
		writer.WriteAttributeString<int>("Count", this.registers.Count);
		foreach (KeyValuePair<StaticString, string> keyValuePair in this.registers)
		{
			writer.WriteStartElement("Register");
			writer.WriteAttributeString<StaticString>("Key", keyValuePair.Key);
			writer.WriteString(keyValuePair.Value);
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
		if (this.IsActive)
		{
			writer.WriteStartElement("InstructionPanel");
			writer.WriteAttributeString("Title", TutorialInstructionPanel.WaitingInstruction.Title);
			writer.WriteAttributeString("Content", TutorialInstructionPanel.WaitingInstruction.Content);
			writer.WriteAttributeString("Action", TutorialInstructionPanel.WaitingInstruction.Action);
			writer.WriteAttributeString<bool>("Modal", TutorialInstructionPanel.WaitingInstruction.Modal);
			writer.WriteAttributeString<bool>("WheelGrabber", TutorialInstructionPanel.WaitingInstruction.WheelGrabber);
			writer.WriteAttributeString<bool>("DisplayNextButton", TutorialInstructionPanel.WaitingInstruction.DisplayNextButton);
			writer.WriteAttributeString<bool>("OverNotifications", TutorialInstructionPanel.WaitingInstruction.OverNotifications);
			writer.WriteAttributeString<bool>("OverModals", TutorialInstructionPanel.WaitingInstruction.OverModals);
			writer.WriteAttributeString("WaitForPanelTypeName", TutorialInstructionPanel.WaitingInstruction.WaitForPanelTypeName);
			writer.WriteAttributeString("WaitForPanelName", TutorialInstructionPanel.WaitingInstruction.WaitForPanelName);
			writer.WriteAttributeString("Placement", TutorialInstructionPanel.WaitingInstruction.Placement.ToString());
			writer.WriteEndElement();
			writer.WriteStartElement("HighlightPanel");
			string value = string.Empty;
			string value2 = string.Empty;
			bool value3 = true;
			if (this.guiService != null)
			{
				TutorialHighlightPanel guiPanel = this.guiService.GetGuiPanel<TutorialHighlightPanel>();
				if (guiPanel != null && guiPanel.Target != null)
				{
					value = guiPanel.Content;
					value2 = guiPanel.Target.name;
					value3 = guiPanel.RectangularHighlight;
				}
			}
			writer.WriteAttributeString("Content", value);
			writer.WriteAttributeString("TargetName", value2);
			writer.WriteAttributeString<bool>("Rectangular", value3);
			writer.WriteEndElement();
		}
	}

	public static bool IsEnabled
	{
		get
		{
			IRuntimeService service = Services.GetService<IRuntimeService>();
			if (service != null && service.Runtime != null && service.Runtime.Configuration != null)
			{
				RuntimeModule runtimeModule = service.Runtime.RuntimeModules.FirstOrDefault((RuntimeModule module) => module.Type == RuntimeModuleType.Standalone);
				RuntimeModule runtimeModule2 = service.Runtime.RuntimeModules.FirstOrDefault((RuntimeModule module) => module.Type == RuntimeModuleType.Conversion);
				RuntimeModule runtimeModule3 = service.Runtime.RuntimeModules.FirstOrDefault((RuntimeModule module) => module.Type == RuntimeModuleType.Extension);
				RuntimeModule runtimeModule4;
				if (runtimeModule2 != null)
				{
					runtimeModule4 = runtimeModule2;
				}
				else if (runtimeModule3 != null)
				{
					runtimeModule4 = runtimeModule3;
				}
				else
				{
					Diagnostics.Assert(runtimeModule != null);
					runtimeModule4 = runtimeModule;
					if (service != null && service.VanillaModuleName == runtimeModule.Name)
					{
						runtimeModule4 = null;
					}
				}
				if (runtimeModule4 != null)
				{
					return false;
				}
			}
			return (!Amplitude.Unity.Framework.Application.Version.Label.StartsWith("ALPHA") && !Amplitude.Unity.Framework.Application.Version.Label.StartsWith("BETA")) || Amplitude.Unity.Framework.Application.Version.Accessibility <= Accessibility.Protected;
		}
	}

	public static bool IsActivated
	{
		get
		{
			if (TutorialManager.IsEnabled)
			{
				IGameService service = Services.GetService<IGameService>();
				if (service != null && service.Game != null && service.Game is global::Game)
				{
					ITutorialService service2 = service.Game.Services.GetService<ITutorialService>();
					if (service2 != null)
					{
						return service2.IsActive;
					}
				}
			}
			return false;
		}
	}

	public bool IsActive { get; set; }

	private ICommandService CommandService { get; set; }

	public static IEnumerable<GameSaveDescriptor> GetListOfGameSaveDescritors(string path)
	{
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
		List<string> fileNames = new List<string>();
		fileNames.AddRange(Directory.GetFiles(path, "*.zip"));
		fileNames.AddRange(Directory.GetFiles(path, "*.sav"));
		Archive archive = null;
		GameSaveDescriptor gameSaveDescriptor = null;
		foreach (string fileName in fileNames)
		{
			gameSaveDescriptor = null;
			try
			{
				archive = Archive.Open(fileName, ArchiveMode.Open);
				MemoryStream stream = null;
				if (archive.TryGet(global::GameManager.GameSaveDescriptorFileName, out stream))
				{
					XmlReaderSettings xmlReaderSettings = new XmlReaderSettings
					{
						IgnoreComments = true,
						IgnoreWhitespace = true,
						CloseInput = true
					};
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
			catch
			{
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
				yield return gameSaveDescriptor;
			}
		}
		yield break;
	}

	public static ITutorialService GetService()
	{
		if (TutorialManager.IsActivated)
		{
			IGameService service = Services.GetService<IGameService>();
			if (service != null && service.Game != null && service.Game is global::Game)
			{
				return service.Game.Services.GetService<ITutorialService>();
			}
		}
		return null;
	}

	public static void Launch()
	{
		string text = Amplitude.Unity.Framework.Path.GetFullPath("Public/Tutorial");
		string a;
		if (global::Application.ResolveChineseLanguage(out a))
		{
			if (a == "schinese")
			{
				text += "/SChinese";
			}
			else if (a == "tchinese")
			{
				text += "/TChinese";
			}
		}
		GameSaveDescriptor gameSaveDescriptor = (from element in TutorialManager.GetListOfGameSaveDescritors(text)
		orderby element.SourceFileName
		select element).FirstOrDefault<GameSaveDescriptor>();
		if (gameSaveDescriptor != null)
		{
			TutorialManager.Launch(gameSaveDescriptor);
		}
	}

	public static void Launch(string fileNameWithoutExtension)
	{
		string fullPath = Amplitude.Unity.Framework.Path.GetFullPath("Public/Tutorial");
		IEnumerable<GameSaveDescriptor> source = from element in TutorialManager.GetListOfGameSaveDescritors(fullPath)
		orderby element.SourceFileName
		select element;
		GameSaveDescriptor gameSaveDescriptor = source.FirstOrDefault((GameSaveDescriptor element) => System.IO.Path.GetFileNameWithoutExtension(element.SourceFileName).Equals(fileNameWithoutExtension, StringComparison.InvariantCultureIgnoreCase));
		if (gameSaveDescriptor != null)
		{
			TutorialManager.Launch(gameSaveDescriptor);
		}
	}

	public static void Launch(GameSaveDescriptor gameSaveDescriptor)
	{
		if (gameSaveDescriptor == null)
		{
			throw new ArgumentNullException("gameSaveDescriptor");
		}
		IRuntimeService service = Services.GetService<IRuntimeService>();
		if (service != null)
		{
			if (service.Runtime == null)
			{
				throw new InvalidOperationException("Cannnot launch the tutorial when there is no runtime.");
			}
			service.Runtime.FiniteStateMachine.PostStateChange(typeof(RuntimeState_Lobby), new object[]
			{
				gameSaveDescriptor
			});
		}
	}

	public override IEnumerator BindServices(IServiceContainer serviceContainer)
	{
		yield return base.BindServices(serviceContainer);
		this.CommandService = Services.GetService<ICommandService>();
		serviceContainer.AddService<ITutorialService>(this);
		yield break;
	}

	public override IEnumerator Ignite(IServiceContainer serviceContainer)
	{
		yield return base.Ignite(serviceContainer);
		this.registers.Clear();
		yield break;
	}

	public override IEnumerator LoadGame(global::Game game)
	{
		yield return base.LoadGame(game);
		if (this.CommandService != null && Amplitude.Unity.Framework.Application.Version.Accessibility <= Accessibility.Internal)
		{
			this.CommandService.RegisterCommand(new Command("/Tutorial", "Changes the activation state of the tutorial manager."), new Func<string[], string>(this.Command_Tutorial));
		}
		if (this.IsActive)
		{
			this.SetNotifications(true);
			IGameService gameService = Services.GetService<IGameService>();
			Diagnostics.Assert(gameService != null);
			Diagnostics.Assert(gameService.Game != null);
			Diagnostics.Assert(gameService.Game is global::Game);
			SimulationDescriptor tutorialEmpireDescriptor = Databases.GetDatabase<SimulationDescriptor>(false).GetValue("EmpireTutorial");
			Diagnostics.Assert(tutorialEmpireDescriptor != null);
			foreach (global::Empire empire in game.Empires)
			{
				empire.AddDescriptor(tutorialEmpireDescriptor, false);
			}
			if (!string.IsNullOrEmpty(TutorialInstructionPanel.WaitingInstruction.Content))
			{
				this.eventService = Services.GetService<IEventService>();
				Diagnostics.Assert(this.eventService != null);
				this.eventService.EventRaise += this.EventService_EventRaise;
			}
			this.guiService = Services.GetService<Amplitude.Unity.Gui.IGuiService>();
			Diagnostics.Assert(this.guiService != null);
			TutorialInstructionPanel instructionPanel = this.guiService.GetGuiPanel<TutorialInstructionPanel>();
			Diagnostics.Assert(instructionPanel != null);
			instructionPanel.OnTutorialLoad();
			TutorialHighlightPanel highlightPanel = this.guiService.GetGuiPanel<TutorialHighlightPanel>();
			Diagnostics.Assert(highlightPanel != null);
			highlightPanel.OnTutorialLoad();
		}
		yield break;
	}

	protected override void Releasing()
	{
		if (this.eventService != null)
		{
			this.eventService.EventRaise -= this.EventService_EventRaise;
		}
		if (this.guiService != null)
		{
			TutorialHighlightPanel guiPanel = this.guiService.GetGuiPanel<TutorialHighlightPanel>();
			if (guiPanel != null)
			{
				guiPanel.OnTutorialRelease();
			}
			TutorialInstructionPanel guiPanel2 = this.guiService.GetGuiPanel<TutorialInstructionPanel>();
			if (guiPanel2 != null)
			{
				guiPanel2.OnTutorialRelease();
			}
		}
		if (this.IsActive)
		{
			this.RestoreNotifications();
		}
		if (this.CommandService != null)
		{
			if (Amplitude.Unity.Framework.Application.Version.Accessibility <= Accessibility.Internal)
			{
				this.CommandService.UnregisterCommand("/Tutorial");
			}
			this.CommandService = null;
		}
		this.registers.Clear();
		base.Releasing();
		this.IsActive = false;
	}

	private void EventService_EventRaise(object sender, EventRaiseEventArgs e)
	{
		if (e.RaisedEvent is EventBeginTurn)
		{
			TutorialInstructionPanel.Show(TutorialInstructionPanel.WaitingInstruction.Title, TutorialInstructionPanel.WaitingInstruction.Content, TutorialInstructionPanel.WaitingInstruction.Action, TutorialInstructionPanel.WaitingInstruction.Modal, TutorialInstructionPanel.WaitingInstruction.WheelGrabber, TutorialInstructionPanel.WaitingInstruction.DisplayNextButton, TutorialInstructionPanel.WaitingInstruction.OverNotifications, TutorialInstructionPanel.WaitingInstruction.OverModals, TutorialInstructionPanel.WaitingInstruction.WaitForPanelTypeName, TutorialInstructionPanel.WaitingInstruction.WaitForPanelName, TutorialInstructionPanel.WaitingInstruction.Placement);
			this.eventService.EventRaise -= this.EventService_EventRaise;
			if (!string.IsNullOrEmpty(TutorialHighlightPanel.WaitingHighlight.TargetName))
			{
				AgeTransform ageTransform = null;
				Amplitude.Unity.Gui.GuiPanel guiPanel = null;
				if (this.guiService.TryGetGuiPanelByName(TutorialHighlightPanel.WaitingHighlight.TargetName, out guiPanel) && guiPanel != null)
				{
					ageTransform = guiPanel.AgeTransform;
				}
				else
				{
					Transform transform = (this.guiService as global::GuiManager).transform.Search(TutorialHighlightPanel.WaitingHighlight.TargetName);
					if (transform != null)
					{
						ageTransform = transform.GetComponent<AgeTransform>();
					}
				}
				if (ageTransform != null)
				{
					TutorialHighlightPanel.ShowHighlight(TutorialHighlightPanel.WaitingHighlight.Content, ageTransform, TutorialHighlightPanel.WaitingHighlight.RectangularHighlight);
				}
				else
				{
					Diagnostics.LogWarning("Could not restore highlight on: " + TutorialHighlightPanel.WaitingHighlight.TargetName);
				}
			}
		}
	}

	private string Command_Tutorial(string[] commandLineArgs)
	{
		if (commandLineArgs.Length > 1)
		{
			string text = commandLineArgs[1].ToLower();
			string text2 = text;
			switch (text2)
			{
			case "get":
				if (commandLineArgs.Length > 2)
				{
					StaticString staticString = commandLineArgs[2];
					string value = ((ITutorialService)this).GetValue(staticString);
					return string.Format("Register: name = '{0}', value = '{1}'.", staticString, value);
				}
				return "Error: missing name.";
			case "off":
				this.IsActive = false;
				break;
			case "on":
				this.IsActive = true;
				break;
			case "registers":
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.AppendFormat("{0} register(s) in store.\r\n", this.registers.Count);
				foreach (KeyValuePair<StaticString, string> keyValuePair in this.registers)
				{
					stringBuilder.AppendFormat("Register: name = '{0}', value = '{1}'.\r\n", keyValuePair.Key, keyValuePair.Value);
				}
				return stringBuilder.ToString();
			}
			case "set":
				if (commandLineArgs.Length <= 2)
				{
					return "Error: missing name.";
				}
				if (commandLineArgs.Length > 3)
				{
					StaticString key = commandLineArgs[2];
					string value2 = commandLineArgs[3].ToLower();
					((ITutorialService)this).SetValue(key, value2);
					return string.Empty;
				}
				return "Error: missing value.";
			}
		}
		if (this.IsActive)
		{
			return "The tutorial manager is active.";
		}
		return "The tutorial manager is not active.";
	}

	private void SetNotifications(bool on = true)
	{
		IGuiNotificationSettingsService service = Services.GetService<IGuiNotificationSettingsService>();
		Diagnostics.Assert(service != null);
		PropertyInfo[] properties = typeof(IGuiNotificationSettingsService).GetProperties(BindingFlags.Instance | BindingFlags.Public);
		this.notificationPreferences.Clear();
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (propertyInfo.PropertyType == typeof(bool))
			{
				this.notificationPreferences.Add(propertyInfo.Name, (bool)propertyInfo.GetValue(service, null));
				propertyInfo.SetValue(service, on, null);
			}
		}
	}

	private void RestoreNotifications()
	{
		IGuiNotificationSettingsService service = Services.GetService<IGuiNotificationSettingsService>();
		Diagnostics.Assert(service != null);
		PropertyInfo[] properties = typeof(IGuiNotificationSettingsService).GetProperties(BindingFlags.Instance | BindingFlags.Public);
		KeyValuePair<StaticString, bool> notificationPreference;
		foreach (KeyValuePair<StaticString, bool> notificationPreference2 in this.notificationPreferences)
		{
			notificationPreference = notificationPreference2;
			properties.First((PropertyInfo p) => p.Name == notificationPreference.Key).SetValue(service, notificationPreference.Value, null);
		}
	}

	public static readonly string EnableBuyoutKey = "EnableBuyout";

	public static readonly string EnableEmptyCursorKey = "EnableEmptyCursor";

	public static readonly string EnableEndTurnKey = "EnableEndTurn";

	public static readonly string EnableDeploymentButtonKey = "EnableDeploymentButton";

	public static readonly string EnableTargetingButtonKey = "EnableTargetingButton";

	public static readonly string EnableHeroAssignmentKey = "EnableHeroAssignment";

	public static readonly string EnableVictoryKey = "EnableVictory";

	public static readonly string TutorialQuestMandatoryTag = "TutorialQuest";

	private Dictionary<StaticString, string> registers = new Dictionary<StaticString, string>();

	private Dictionary<StaticString, bool> notificationPreferences = new Dictionary<StaticString, bool>();

	private IEventService eventService;

	private Amplitude.Unity.Gui.IGuiService guiService;
}
