using System;
using System.Collections;
using System.IO;
using System.Threading;
using Amplitude;
using Amplitude.Interop;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Input;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Video;
using Amplitude.Unity.View;
using UnityEngine;

public class GuiManager : Amplitude.Unity.Gui.GuiManager, Amplitude.Unity.Gui.IGuiService, IService, global::IGuiService, IGuiSettingsService
{
	public IGuiSimulationParser GuiSimulationParser
	{
		get
		{
			return this.simulationEffectParser;
		}
	}

	public bool HighDefinitionUI
	{
		get
		{
			return AgeUtils.HighDefinition;
		}
		set
		{
			if (AgeUtils.HighDefinition != value)
			{
				if (value && (Screen.width < global::GuiManager.MinimumResolutionWidthForHighDefinitionUI || Screen.height < global::GuiManager.MinimumResolutionHeightForHighDefinitionUI))
				{
					value = false;
				}
				if (base.AgeManager != null)
				{
					base.AgeManager.SwitchHighDefinition(value);
				}
				else
				{
					Diagnostics.LogError("Can't switch high definition (switch: '{0}') because the age manager is null.", new object[]
					{
						value
					});
					AgeUtils.HighDefinition = value;
				}
				Amplitude.Unity.Framework.Application.Registry.SetValue<bool>(global::GuiManager.Registers.HighDefinitionUI, value);
			}
		}
	}

	public string EmpireColorPalette
	{
		get
		{
			return Amplitude.Unity.Framework.Application.Registry.GetValue<string>(global::GuiManager.Registers.EmpireColorPalette, "Default");
		}
		set
		{
			if (Amplitude.Unity.Framework.Application.Registry.GetValue<string>(global::GuiManager.Registers.EmpireColorPalette, "Default") != value)
			{
				Amplitude.Unity.Framework.Application.Registry.SetValue(global::GuiManager.Registers.EmpireColorPalette, value);
			}
		}
	}

	private StaticString CursorName { get; set; }

	private IRuntimeService RuntimeService { get; set; }

	public override IEnumerator BindServices()
	{
		yield return base.BindServices();
		this.randomizeLoadingScreen = Amplitude.Unity.Framework.Application.Registry.GetValue<bool>(new StaticString("Settings/ELCP/UI/RandomizeLoadingScreen"), false);
		this.capacityColor1 = Amplitude.Unity.Framework.Application.Registry.GetValue<int>(new StaticString("Settings/ELCP/UI/CapacityColor1"), 0);
		this.capacityColor2 = Amplitude.Unity.Framework.Application.Registry.GetValue<int>(new StaticString("Settings/ELCP/UI/CapacityColor2"), 2);
		this.capacityColor3 = Amplitude.Unity.Framework.Application.Registry.GetValue<int>(new StaticString("Settings/ELCP/UI/CapacityColor3"), 8);
		this.zoomRatioDetailsBecomeAbstract = Amplitude.Unity.Framework.Application.Registry.GetValue<float>(global::GuiManager.Registers.ZoomRatioDetailsBecomeAbstract, 0.5f);
		this.simulationEffectParser = new SimulationEffectParser();
		this.simulationEffectParser.GuiService = this;
		Services.AddService<global::IGuiService>(this);
		Services.AddService<IGuiSettingsService>(this);
		this.keyMapperService = Services.GetService<IKeyMappingService>();
		Diagnostics.Assert(this.keyMapperService != null);
		AgeUtils.HighDefinition = Amplitude.Unity.Framework.Application.Registry.GetValue<bool>(global::GuiManager.Registers.HighDefinitionUI, true);
		if (Amplitude.Unity.Framework.Application.Version.Accessibility <= Accessibility.Private)
		{
			UnityEngine.Application.logMessageReceived += this.ExceptionHandler;
		}
		base.SetLastError(0, "Waiting for service dependencies...");
		global::IVideoSettingsService videoSettingsService = null;
		yield return base.BindService<global::IVideoSettingsService>(delegate(global::IVideoSettingsService service)
		{
			videoSettingsService = service;
		});
		if (videoSettingsService != null)
		{
			videoSettingsService.ResolutionChange += this.VideoSettingsService_ResolutionChange;
			this.VideoSettingsService_ResolutionChange(this, new VideoResolutionChangeEventArgs(videoSettingsService.Resolution));
		}
		yield return base.BindService<IRuntimeService>(delegate(IRuntimeService service)
		{
			this.RuntimeService = service;
		});
		yield break;
	}

	public bool CheckHighDefinitionUI()
	{
		return Screen.width >= global::GuiManager.MinimumResolutionWidthForHighDefinitionUI && Screen.height >= global::GuiManager.MinimumResolutionHeightForHighDefinitionUI;
	}

	public bool ChangeMouseCursor(StaticString cursorName)
	{
		if (base.AgeManager == null)
		{
			return false;
		}
		if (this.CursorName != cursorName)
		{
			IDatabase<GuiCursor> database = Databases.GetDatabase<GuiCursor>(false);
			if (database == null)
			{
				return false;
			}
			GuiCursor guiCursor;
			if (database.TryGetValue(cursorName, out guiCursor))
			{
				Texture2D texture2D = base.AgeManager.FindDynamicTexture(guiCursor.Image.Path, false);
				if (texture2D != null)
				{
					UnityEngine.Cursor.SetCursor(texture2D, guiCursor.HotSpot, CursorMode.Auto);
				}
			}
			else
			{
				Diagnostics.LogWarning("Unable to find GuiCursor of name {0}", new object[]
				{
					cursorName
				});
			}
			this.CursorName = cursorName;
		}
		return true;
	}

	public override bool HandleCancelRequest()
	{
		bool flag = false;
		if (!flag && LoadingScreen.LoadingInProgress)
		{
			flag = true;
		}
		TutorialInstructionPanel guiPanel = base.GetGuiPanel<TutorialInstructionPanel>();
		if (!flag && guiPanel != null && guiPanel.IsVisible && guiPanel.ModalFrame.Visible)
		{
			flag = true;
		}
		if (!flag && base.AgeManager.FocusedControl != null && base.AgeManager.FocusedControl.IsKeyExclusive)
		{
			base.AgeManager.FocusedControl = null;
			flag = true;
		}
		if (!flag && this.modalManager.CurrentModalPanel != null && this.modalManager.CurrentModalPanel.HandleCancelRequest())
		{
			flag = true;
		}
		if (!flag && NotificationPanelBase.OpenedPanel != null)
		{
			NotificationPanelBase.OpenedPanel.Hide(false);
			flag = true;
		}
		if (!flag && Amplitude.Unity.Gui.GuiScreen.CurrentScreen != null)
		{
			flag = Amplitude.Unity.Gui.GuiScreen.CurrentScreen.HandleCancelRequest();
		}
		return flag;
	}

	protected override void InitModal()
	{
		this.modalManager = new global::GuiModalManager();
		this.modalManager.Init(this.blurComponents);
		Amplitude.Unity.Gui.GuiModalPanel.GuiModalManager = this.modalManager;
	}

	protected override IEnumerator LoadGuiScene()
	{
		try
		{
			if (base.AgeManager != null)
			{
				base.AgeManager.OnLoadDynamicTexture -= this.OnLoadDynamicTexture;
			}
			if (base.Panels != null)
			{
				Diagnostics.Log("Unloading all panels...");
				Amplitude.Unity.Gui.GuiPanel[] panels = base.Panels;
				for (int i = 0; i < panels.Length; i++)
				{
					panels[i].Unload();
				}
				base.AgeManager.ReleaseDynamicTextures();
			}
		}
		catch (Exception ex)
		{
			Diagnostics.LogError(ex.ToString());
			throw;
		}
		if (base.AgeManager != null && this.RuntimeService != null)
		{
			Diagnostics.Assert(this.RuntimeService.Runtime != null);
			Diagnostics.Assert(this.RuntimeService.Runtime.RuntimeModules != null);
			for (int j = 0; j < this.RuntimeService.Runtime.RuntimeModules.Count; j++)
			{
				if (this.RuntimeService.Runtime.RuntimeModules[j].ResourcesFolder != null)
				{
					base.AgeManager.OnLoadDynamicTexture += this.OnLoadDynamicTexture;
					break;
				}
			}
		}
		this.LoadPalette();
		yield return base.LoadGuiScene();
		Steamworks.SteamAPI.SteamUtils.SetOverlayNotificationPosition(Steamworks.SteamUtils.ENotificationPosition.k_EPositionTopRight);
		yield break;
	}

	protected override void OnGuiSceneStateChange(GuiSceneStateChangedEventArgs.GuiSceneState state)
	{
		Diagnostics.Log("(pre) OnGuiSceneStateChange: {0}.", new object[]
		{
			state.ToString()
		});
		base.OnGuiSceneStateChange(state);
		Diagnostics.Log("OnGuiSceneStateChange: {0}.", new object[]
		{
			state.ToString()
		});
		if (state == GuiSceneStateChangedEventArgs.GuiSceneState.Reloaded)
		{
			Diagnostics.Log("Reinitializing the AGE manager...");
			base.AgeManager.Init();
			Diagnostics.Log("MenuMainScreen.");
			base.GetGuiPanel<MenuMainScreen>().Show(new object[0]);
		}
	}

	protected override void Update()
	{
		base.Update();
		if (Input.GetMouseButtonDown(1))
		{
			this.HandleCancelRequest();
		}
		if (this.keyMapperService.GetKeyDown(KeyAction.ControlBindingsCancel))
		{
			this.HandleCancelRequest();
		}
		if (this.keyMapperService.GetKeyDown(KeyAction.ControlBindingsGUIDisplay))
		{
			this.displayInterfaceToggle = !this.displayInterfaceToggle;
			AgeManager.Instance.ShowGui = this.displayInterfaceToggle;
		}
		if (base.AgeManager != null && base.AgeManager.FocusedControl != null && base.AgeManager.FocusedControl.IsKeyExclusive)
		{
			return;
		}
		if (this.keyMapperService.GetKeyDown(KeyAction.ControlBindingsConsole))
		{
			if (global::GameManager.IsInGame)
			{
				this.ShowHideInGameConsolePanel();
			}
			else
			{
				this.FocusOutGameConsolePanel();
			}
		}
		if (Input.GetKeyDown(KeyCode.RightArrow))
		{
			this.HandleNextRequest();
		}
		if (Input.GetKeyDown(KeyCode.LeftArrow))
		{
			this.HandlePreviousRequest();
		}
		if (this.keyMapperService.GetKeyDown(KeyAction.ControlBindingsNavigateUp))
		{
			this.HandleUpRequest();
		}
		if (this.keyMapperService.GetKeyDown(KeyAction.ControlBindingsNavigateDown))
		{
			this.HandleDownRequest();
		}
		if (Amplitude.Unity.Framework.Application.Version.Accessibility <= Accessibility.Internal || Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			int empireIndex = -1;
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			{
				if (Input.GetKeyUp(KeyCode.Alpha1) || Input.GetKeyUp(KeyCode.Keypad1))
				{
					empireIndex = 10;
				}
				else if (Input.GetKeyUp(KeyCode.Alpha2) || Input.GetKeyUp(KeyCode.Keypad2))
				{
					empireIndex = 11;
				}
				else if (Input.GetKeyUp(KeyCode.Alpha3) || Input.GetKeyUp(KeyCode.Keypad3))
				{
					empireIndex = 12;
				}
				else if (Input.GetKeyUp(KeyCode.Alpha4) || Input.GetKeyUp(KeyCode.Keypad4))
				{
					empireIndex = 13;
				}
				else if (Input.GetKeyUp(KeyCode.Alpha5) || Input.GetKeyUp(KeyCode.Keypad5))
				{
					empireIndex = 14;
				}
			}
			else if (Input.GetKeyUp(KeyCode.Alpha1) || Input.GetKeyUp(KeyCode.Keypad1))
			{
				empireIndex = 0;
			}
			else if (Input.GetKeyUp(KeyCode.Alpha2) || Input.GetKeyUp(KeyCode.Keypad2))
			{
				empireIndex = 1;
			}
			else if (Input.GetKeyUp(KeyCode.Alpha3) || Input.GetKeyUp(KeyCode.Keypad3))
			{
				empireIndex = 2;
			}
			else if (Input.GetKeyUp(KeyCode.Alpha4) || Input.GetKeyUp(KeyCode.Keypad4))
			{
				empireIndex = 3;
			}
			else if (Input.GetKeyUp(KeyCode.Alpha5) || Input.GetKeyUp(KeyCode.Keypad5))
			{
				empireIndex = 4;
			}
			else if (Input.GetKeyUp(KeyCode.Alpha6) || Input.GetKeyUp(KeyCode.Keypad6))
			{
				empireIndex = 5;
			}
			else if (Input.GetKeyUp(KeyCode.Alpha7) || Input.GetKeyUp(KeyCode.Keypad7))
			{
				empireIndex = 6;
			}
			else if (Input.GetKeyUp(KeyCode.Alpha8) || Input.GetKeyUp(KeyCode.Keypad8))
			{
				empireIndex = 7;
			}
			else if (Input.GetKeyUp(KeyCode.Alpha9) || Input.GetKeyUp(KeyCode.Keypad9))
			{
				empireIndex = 8;
			}
			else if (Input.GetKeyUp(KeyCode.Alpha0) || Input.GetKeyUp(KeyCode.Keypad0))
			{
				empireIndex = 9;
			}
			if (empireIndex != -1)
			{
				IGameService service = Services.GetService<IGameService>();
				Diagnostics.Assert(service != null);
				global::Game game = service.Game as global::Game;
				if (game != null && game.Empires != null)
				{
					global::Empire empire = Array.Find<global::Empire>(game.Empires, (global::Empire match) => match.Index == empireIndex);
					if (empire != null && empire.PlayerControllers.Client != null && empire is MajorEmpire)
					{
						IPlayerControllerRepositoryService service2 = game.Services.GetService<IPlayerControllerRepositoryService>();
						Diagnostics.Assert(service2 != null);
						service2.SetActivePlayerController(empire.PlayerControllers.Client);
					}
				}
			}
		}
	}

	private void ExceptionHandler(string logMessage, string stackTrace, LogType logType)
	{
		ErrorModalPanel guiPanel = base.GetGuiPanel<ErrorModalPanel>();
		if (guiPanel != null && logType == LogType.Exception)
		{
			guiPanel.Show(logMessage, stackTrace, logType);
		}
	}

	private bool FocusOutGameConsolePanel()
	{
		MenuNewGameScreen guiPanel = base.GetGuiPanel<MenuNewGameScreen>();
		return (guiPanel != null || guiPanel.IsVisible) && guiPanel.FocusOutGameConsolePanel();
	}

	private void LoadPalette()
	{
		string text = Amplitude.Unity.Framework.Application.Registry.GetValue<string>("Settings/UI/EmpireColorPalette", "Standard");
		IDatabase<Palette> database = Databases.GetDatabase<Palette>(false);
		if (database != null)
		{
			Palette palette;
			if (!database.TryGetValue(text, out palette))
			{
				if (text != "Standard")
				{
					text = "Standard";
					database.TryGetValue(text, out palette);
				}
				if (palette == null)
				{
					Palette[] values = database.GetValues();
					if (values != null && values.Length != 0)
					{
						text = values[0].Name;
					}
				}
			}
			if (palette != null)
			{
				Amplitude.Unity.Framework.Application.Registry.SetValue("Settings/UI/EmpireColorPalette", text);
			}
		}
	}

	private bool OnLoadDynamicTexture(string path, out Texture2D texture)
	{
		Diagnostics.Assert(this.RuntimeService != null);
		Diagnostics.Assert(this.RuntimeService.Runtime != null);
		Diagnostics.Assert(this.RuntimeService.Runtime.RuntimeModules != null);
		for (int i = 0; i < this.RuntimeService.Runtime.RuntimeModules.Count; i++)
		{
			RuntimeModule runtimeModule = this.RuntimeService.Runtime.RuntimeModules[i];
			if (runtimeModule.ResourcesFolder != null)
			{
				string text = System.IO.Path.ChangeExtension(System.IO.Path.Combine(runtimeModule.ResourcesFolder.FullName, path), "png");
				if (File.Exists(text))
				{
					WWW www = new WWW(new Uri(text).AbsoluteUri);
					while (!www.isDone)
					{
						Thread.Sleep(0);
					}
					if (www.texture != null)
					{
						texture = www.texture;
						return true;
					}
				}
			}
		}
		texture = null;
		return false;
	}

	private bool ShowHideInGameConsolePanel()
	{
		InGameConsolePanel guiPanel = base.GetGuiPanel<InGameConsolePanel>();
		if (guiPanel != null)
		{
			if (!guiPanel.IsVisible)
			{
				guiPanel.IsDiscreet = false;
				guiPanel.Show(new object[0]);
				return true;
			}
			if (guiPanel.IsDiscreet)
			{
				guiPanel.IsDiscreet = false;
			}
			else if (guiPanel.InputLabel.Text == string.Empty)
			{
				guiPanel.Hide(true);
				guiPanel.IsDiscreet = true;
				return true;
			}
		}
		return false;
	}

	private void VideoSettingsService_ResolutionChange(object sender, VideoResolutionChangeEventArgs e)
	{
		if (this.HighDefinitionUI && (Screen.width < global::GuiManager.MinimumResolutionWidthForHighDefinitionUI || Screen.height < global::GuiManager.MinimumResolutionHeightForHighDefinitionUI))
		{
			this.HighDefinitionUI = false;
		}
	}

	public int CapacityColor2
	{
		get
		{
			return this.capacityColor2;
		}
		set
		{
			if (this.capacityColor2 != value)
			{
				this.capacityColor2 = value;
				Amplitude.Unity.Framework.Application.Registry.SetValue(new StaticString("Settings/ELCP/UI/CapacityColor2"), value.ToString());
			}
		}
	}

	public int CapacityColor1
	{
		get
		{
			return this.capacityColor1;
		}
		set
		{
			if (this.capacityColor1 != value)
			{
				this.capacityColor1 = value;
				Amplitude.Unity.Framework.Application.Registry.SetValue(new StaticString("Settings/ELCP/UI/CapacityColor1"), value.ToString());
			}
		}
	}

	public int CapacityColor3
	{
		get
		{
			return this.capacityColor3;
		}
		set
		{
			if (this.capacityColor3 != value)
			{
				this.capacityColor3 = value;
				Amplitude.Unity.Framework.Application.Registry.SetValue(new StaticString("Settings/ELCP/UI/CapacityColor3"), value.ToString());
			}
		}
	}

	public float ZoomRatioDetailsBecomeAbstract
	{
		get
		{
			return this.zoomRatioDetailsBecomeAbstract;
		}
		set
		{
			if (this.zoomRatioDetailsBecomeAbstract != value)
			{
				this.zoomRatioDetailsBecomeAbstract = value;
				Amplitude.Unity.Framework.Application.Registry.SetValue(new StaticString(global::GuiManager.Registers.ZoomRatioDetailsBecomeAbstract), value.ToString());
				Amplitude.Unity.View.IViewService service = Services.GetService<Amplitude.Unity.View.IViewService>();
				if (service != null && service.CurrentView != null)
				{
					WorldView worldView = service.CurrentView as WorldView;
					if (worldView != null)
					{
						ILayerService service2 = worldView.CurrentWorldViewTechnique.GetService<ILayerService>();
						if (service2 != null && service2 as DefaultLayerController != null)
						{
							(service2 as DefaultLayerController).UpdateZoomRatioDetailsBecomeAbstract();
						}
					}
				}
			}
		}
	}

	public bool RandomizeLoadingScreen
	{
		get
		{
			return this.randomizeLoadingScreen;
		}
		set
		{
			if (this.randomizeLoadingScreen != value)
			{
				this.randomizeLoadingScreen = value;
				Amplitude.Unity.Framework.Application.Registry.SetValue(new StaticString("Settings/ELCP/UI/RandomizeLoadingScreen"), value.ToString());
			}
		}
	}

	public static readonly int MinimumResolutionWidthForHighDefinitionUI = 1900;

	public static readonly int MinimumResolutionHeightForHighDefinitionUI = 1000;

	public AgeRenderer BackgroundRenderer;

	private bool displayInterfaceToggle = true;

	private SimulationEffectParser simulationEffectParser;

	private IKeyMappingService keyMapperService;

	private int capacityColor1;

	private int capacityColor2 = 2;

	private int capacityColor3 = 8;

	private float zoomRatioDetailsBecomeAbstract = 0.5f;

	private bool randomizeLoadingScreen;

	public static class Registers
	{
		public static StaticString HighDefinitionUI = new StaticString("Settings/UI/HighDefinitionUI");

		public static StaticString EmpireColorPalette = new StaticString("Settings/UI/EmpireColorPalette");

		public static StaticString ZoomRatioDetailsBecomeAbstract = new StaticString("Settings/ELCP/UI/ZoomRatioDetailsBecomeAbstract");
	}
}
