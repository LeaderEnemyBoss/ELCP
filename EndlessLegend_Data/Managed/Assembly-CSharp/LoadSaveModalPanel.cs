using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amplitude;
using Amplitude.Unity;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Messaging;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Session;
using UnityEngine;

public class LoadSaveModalPanel : global::GuiModalPanel
{
	private void StartProfanityFiltering()
	{
	}

	public bool SaveMode { get; set; }

	public int ListingNumber { get; private set; }

	private GameSaveDescriptor GameSaveDescriptor { get; set; }

	private RuntimeModuleConfigurationState GameSaveRuntimeConfigurationState { get; set; }

	public override void RefreshContent()
	{
		base.RefreshContent();
		if (this.listing != null)
		{
			this.ListingNumber++;
			this.listing = null;
		}
		if (this.listing == null)
		{
			this.listing = UnityCoroutine.StartCoroutine(this, this.ListGamesAsync(this.ListingNumber), new EventHandler<CoroutineExceptionEventArgs>(this.ListGamesAsync_CoroutineExceptionHandler));
			if (this.listing.IsFinished)
			{
				this.listing = null;
			}
		}
		SortedLinesTable component = this.LoadSaveContainer.GetComponent<SortedLinesTable>();
		if (component != null)
		{
			component.SortLines();
		}
		this.RefreshButtons();
	}

	protected override IEnumerator OnHide(bool instant)
	{
		if (this.listing != null)
		{
			this.listing = null;
		}
		this.SortsContainer.UnsetContent();
		AgeManager.Instance.FocusedControl = null;
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		if (this.SaveMode)
		{
			this.PanelTitle.Text = "%SaveGamePanelTitle";
			int turn = (base.Game as Game).Turn;
			this.TextInputLabel.Text = string.Format("New Save Title - Turn {0}", turn + 1);
			IPlayerControllerRepositoryService playerControllerRepositoryService = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
			if (playerControllerRepositoryService != null)
			{
				string factionName = (!TutorialManager.IsActivated) ? (playerControllerRepositoryService.ActivePlayerController.Empire as Empire).Faction.LocalizedName : AgeLocalizer.Instance.LocalizeString("%TutorialSaveName");
				this.TextInputLabel.Text = string.Format(AgeLocalizer.Instance.LocalizeString("%SaveFileFormat"), factionName, turn + 1);
			}
			this.LoadButton.Visible = false;
			this.SaveButton.Visible = true;
			this.TextInput.AgeTransform.Enable = true;
			AgeManager.Instance.FocusedControl = this.TextInput;
		}
		else
		{
			this.PanelTitle.Text = "%LoadGamePanelTitle";
			this.TextInputLabel.Text = string.Empty;
			this.LoadButton.Visible = true;
			this.LoadButton.Enable = false;
			this.SaveButton.Visible = false;
			this.TextInput.AgeTransform.Enable = false;
		}
		this.DeleteButton.Enable = false;
		this.GameSaveDescriptor = null;
		this.SortsContainer.SetContent(this.SaveLineSlotPrefab, "LoadSaveGame", null);
		this.RefreshContent();
		yield break;
	}

	protected override void OnUnload()
	{
		base.OnUnload();
	}

	private IEnumerator ListGamesAsync(int listingNumber)
	{
		this.LoadSaveContainer.DestroyAllChildren();
		IGameSerializationService gameSerializationService = Services.GetService<IGameSerializationService>();
		if (gameSerializationService != null)
		{
			List<GameSaveDescriptor> gameSaveDescriptors = new List<GameSaveDescriptor>();
			this.LoadSaveContainer.Height = 0f;
			IEnumerable<GameSaveDescriptor> enumerable = gameSerializationService.GetListOfGameSaveDescritors(!this.SaveMode);
			foreach (GameSaveDescriptor gameSaveDescriptor in enumerable)
			{
				gameSaveDescriptors.Add(gameSaveDescriptor);
				this.LoadSaveContainer.ReserveChildren(gameSaveDescriptors.Count, this.SaveLineSlotPrefab, "Item");
				this.LoadSaveContainer.ArrangeChildren();
				this.LoadSaveScrollView.OnPositionRecomputed();
				int index = gameSaveDescriptors.Count - 1;
				AgeTransform child = this.LoadSaveContainer.GetChildren()[index];
				this.SetupSaveLineSlot(child, index, gameSaveDescriptor);
				if (!Amplitude.Unity.Framework.Application.Preferences.EnableMultiplayer && gameSaveDescriptor.GameSaveSessionDescriptor.SessionMode != SessionMode.Single)
				{
					child.Enable = false;
				}
				else
				{
					SaveLineSlot slot = child.GetComponent<SaveLineSlot>();
					Diagnostics.Assert(slot != null);
					RuntimeModuleConfigurationState gameSaveRuntimeConfigurationState = slot.GameSaveRuntimeConfigurationState;
					if (gameSaveRuntimeConfigurationState == RuntimeModuleConfigurationState.Undefined)
					{
						child.Enable = false;
					}
				}
				yield return null;
				if (this.ListingNumber > listingNumber)
				{
					yield break;
				}
			}
			if (this.SortsContainer.SortButtons.Last<SortButton>() != null)
			{
				this.SortsContainer.SortButtons.Last<SortButton>().OnSortCB(null);
				this.SortsContainer.SortButtons.Last<SortButton>().OnSortCB(null);
			}
		}
		this.listing = null;
		yield break;
	}

	private void ListGamesAsync_CoroutineExceptionHandler(object sender, CoroutineExceptionEventArgs args)
	{
		Diagnostics.LogError("Exception caught: {0}\n{1}", new object[]
		{
			args.Exception.ToString(),
			args.Exception.StackTrace
		});
		this.listing = null;
	}

	private void OnCloseCB(GameObject obj)
	{
		this.Hide(false);
	}

	private void OnCloudToggleCB(GameObject obj)
	{
		Amplitude.Unity.Framework.Application.Registry.SetValue<bool>(global::Application.Registers.SteamCloudRemoteStorage, this.CloudToggle.State);
		AgeTooltip ageTooltip = this.CloudToggle.AgeTransform.AgeTooltip;
		if (ageTooltip != null)
		{
			if (this.CloudToggle.State)
			{
				ageTooltip.Content = "%LoadSaveCloudEnabledDescription";
			}
			else
			{
				ageTooltip.Content = "%LoadSaveCloudDescription";
			}
		}
		this.RefreshContent();
	}

	private void OnConfirmDownloadModulesBeforeActivation(object sender, MessagePanelResultEventArgs e)
	{
		if (e.Result == MessagePanelResult.Ok)
		{
			this.Hide(false);
			base.GuiService.Show(typeof(ActivateRuntimeModulesModalPanel), new object[]
			{
				this.GameSaveDescriptor.RuntimeModules,
				this.GameSaveDescriptor,
				this
			});
		}
	}

	private void OnConfirmFileDelete(object sender, MessagePanelResultEventArgs e)
	{
		MessagePanelResult result = e.Result;
		if (result == MessagePanelResult.Ok || result == MessagePanelResult.Yes)
		{
			File.Delete(this.GameSaveDescriptor.SourceFileName);
			this.GameSaveDescriptor = null;
			this.RefreshContent();
		}
	}

	private void OnConfirmFileSaveOverwrite(object sender, MessagePanelResultEventArgs e)
	{
		MessagePanelResult result = e.Result;
		if (result == MessagePanelResult.Ok || result == MessagePanelResult.Yes)
		{
			string text = this.TextInputLabel.Text;
			string outputFileName = System.IO.Path.Combine(global::Application.GameSaveDirectory, string.Format("{0}.sav", text));
			this.saving = UnityCoroutine.StartCoroutine(this, this.SaveGameAsync(text, outputFileName), new EventHandler<CoroutineExceptionEventArgs>(this.SaveGameAsync_CoroutineExceptionHandler));
			if (this.saving.IsFinished)
			{
				this.saving = null;
			}
		}
	}

	private void OnDeleteCB(GameObject obj)
	{
		if (this.GameSaveDescriptor != null)
		{
			string message = string.Format(AgeLocalizer.Instance.LocalizeString("%ConfirmFileDeleteFormat"), System.IO.Path.GetFileNameWithoutExtension(this.GameSaveDescriptor.SourceFileName));
			MessagePanel.Instance.Show(message, string.Empty, MessagePanelButtons.YesNo, new MessagePanel.EventHandler(this.OnConfirmFileDelete), MessagePanelType.WARNING, new MessagePanelButton[0]);
		}
	}

	private void OnDoubleClickLineCB(GameObject obj)
	{
		if (this.SaveMode)
		{
			this.OnSaveCB(obj);
		}
		else
		{
			this.OnLoadCB(obj);
		}
	}

	private void OnLoadCB(GameObject obj)
	{
		if (this.GameSaveDescriptor != null)
		{
			Diagnostics.Log("Loading file '{0}'", new object[]
			{
				this.GameSaveDescriptor.SourceFileName
			});
			IRuntimeService service = Services.GetService<IRuntimeService>();
			if (service != null)
			{
				RuntimeModuleConfigurationState gameSaveRuntimeConfigurationState = this.GameSaveRuntimeConfigurationState;
				switch (gameSaveRuntimeConfigurationState + 1)
				{
				case RuntimeModuleConfigurationState.Yellow:
				{
					LoadingScreen guiPanel = base.GuiService.GetGuiPanel<LoadingScreen>();
					if (guiPanel != null)
					{
						guiPanel.Show(new object[]
						{
							this.GameSaveDescriptor
						});
					}
					service.Runtime.FiniteStateMachine.PostStateChange(typeof(RuntimeState_Lobby), new object[]
					{
						this.GameSaveDescriptor
					});
					this.Hide(false);
					break;
				}
				case RuntimeModuleConfigurationState.Red:
				{
					IRuntimeModuleSubscriptionService service2 = Services.GetService<IRuntimeModuleSubscriptionService>();
					string[] array = new string[0];
					string[] array2 = new string[0];
					if (service2 != null)
					{
						service2.GetRuntimeModuleListState(this.GameSaveDescriptor.RuntimeModules, out array, out array2);
					}
					if (array.Length > 0)
					{
						string text = AgeLocalizer.Instance.LocalizeString("%ConfirmDownloadModsBeforeActivation");
						MessagePanel.Instance.Show(text.Replace("$NumberOfMods", array.Length.ToString()), "%Confirmation", MessagePanelButtons.OkCancel, new MessagePanel.EventHandler(this.OnConfirmDownloadModulesBeforeActivation), MessagePanelType.IMPORTANT, new MessagePanelButton[0]);
					}
					else
					{
						this.Hide(false);
						base.GuiService.Show(typeof(ActivateRuntimeModulesModalPanel), new object[]
						{
							this.GameSaveDescriptor.RuntimeModules,
							this.GameSaveDescriptor,
							this
						});
					}
					break;
				}
				case (RuntimeModuleConfigurationState)3:
					this.Hide(false);
					base.GuiService.Show(typeof(ActivateRuntimeModulesModalPanel), new object[]
					{
						this.GameSaveDescriptor.RuntimeModules,
						this.GameSaveDescriptor,
						this
					});
					break;
				}
			}
		}
	}

	private void OnTextChangeCB(GameObject obj)
	{
		this.StartProfanityFiltering();
	}

	private void OnSaveCB(GameObject obj)
	{
		if (string.IsNullOrEmpty(this.TextInputLabel.Text))
		{
			return;
		}
		if (this.saving == null)
		{
			string text = this.TextInputLabel.Text;
			string text2 = System.IO.Path.Combine(global::Application.GameSaveDirectory, string.Format("{0}.sav", text));
			if (File.Exists(text2))
			{
				string message = string.Format(AgeLocalizer.Instance.LocalizeString("%ConfirmFileSaveOverwriteFormat"), System.IO.Path.GetFileNameWithoutExtension(text2));
				MessagePanel.Instance.Show(message, string.Empty, MessagePanelButtons.YesNo, new MessagePanel.EventHandler(this.OnConfirmFileSaveOverwrite), MessagePanelType.WARNING, new MessagePanelButton[0]);
			}
			else
			{
				this.saving = UnityCoroutine.StartCoroutine(this, this.SaveGameAsync(text, text2), new EventHandler<CoroutineExceptionEventArgs>(this.SaveGameAsync_CoroutineExceptionHandler));
				if (this.saving.IsFinished)
				{
					this.saving = null;
				}
			}
		}
	}

	private void OnValidateCB(GameObject obj)
	{
		this.OnSaveCB(obj);
	}

	private void OnSelectSaveLineCB(GameObject obj)
	{
		int num = int.Parse(obj.GetComponent<AgeControlToggle>().OnSwitchData);
		List<SaveLineSlot> children = this.LoadSaveContainer.GetChildren<SaveLineSlot>(true);
		for (int i = 0; i < children.Count; i++)
		{
			if (children[i].Index == num)
			{
				children[i].HighlightToggle.State = true;
				this.GameSaveDescriptor = children[i].GameSaveDescriptor;
				this.GameSaveRuntimeConfigurationState = children[i].GameSaveRuntimeConfigurationState;
				this.TextInputLabel.Text = children[i].Title.Text;
			}
			else
			{
				children[i].HighlightToggle.State = false;
			}
		}
		this.RefreshButtons();
	}

	private void RefreshButtons()
	{
		this.LoadButton.Enable = (this.GameSaveDescriptor != null);
		this.DeleteButton.Enable = (this.GameSaveDescriptor != null);
		this.CloudToggle.State = Amplitude.Unity.Framework.Application.Registry.GetValue<bool>(global::Application.Registers.SteamCloudRemoteStorage);
		if (this.profanityError != string.Empty)
		{
			this.SaveButton.Enable = false;
			this.SaveButton.AgeTooltip.Content = "%Failure" + this.profanityError + "Description";
		}
	}

	private IEnumerator SaveGameAsync(string title, string outputFileName)
	{
		IGameSerializationService gameSerializationService = Services.GetService<IGameSerializationService>();
		if (gameSerializationService != null)
		{
			ISessionService sessionService = Services.GetService<ISessionService>();
			Diagnostics.Assert(sessionService != null);
			global::Session session = sessionService.Session as global::Session;
			switch (session.SessionMode)
			{
			case SessionMode.Single:
				yield return gameSerializationService.SaveGameAsync(title, outputFileName, GameSaveOptions.None);
				break;
			case SessionMode.Private:
			case SessionMode.Protected:
			case SessionMode.Public:
				if (session.GameServer != null)
				{
					yield return gameSerializationService.SaveGameAsync(title, outputFileName, GameSaveOptions.None);
				}
				else
				{
					Message downloadGameMessage = new GameClientDownloadGameMessage(SaveType.OnUserRequest, title);
					session.GameClient.SendMessageToServer(ref downloadGameMessage);
				}
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
			this.Hide(true);
			global::IGuiService guiService = Services.GetService<global::IGuiService>();
			Amplitude.Unity.Gui.GuiPanel[] panels;
			if (guiService != null && guiService.TryGetGuiPanelByType(typeof(GameWorldScreen), out panels))
			{
				panels[0].Show(new object[0]);
			}
		}
		this.saving = null;
		yield break;
	}

	private void SetupSaveLineSlot(AgeTransform tableitem, int index, GameSaveDescriptor gameSaveDescriptor)
	{
		tableitem.DirtyPosition = true;
		tableitem.Enable = true;
		if (index % 20 == 0)
		{
			tableitem.StartNewMesh = true;
		}
		SaveLineSlot component = tableitem.GetComponent<SaveLineSlot>();
		component.RefreshContent(index, gameSaveDescriptor, base.gameObject);
	}

	private void SaveGameAsync_CoroutineExceptionHandler(object sender, CoroutineExceptionEventArgs args)
	{
		Diagnostics.LogError("Exception caught. {0}\n{1}\n----------", new object[]
		{
			args.Exception.ToString(),
			args.Exception.StackTrace
		});
		this.saving = null;
		if (MessagePanel.Instance != null)
		{
			string message = args.Exception.Message;
			MessagePanel.Instance.Show(message, "Exception", MessagePanelButtons.Ok, null, MessagePanelType.IMPORTANT, new MessagePanelButton[0]);
		}
	}

	private string profanityError = string.Empty;

	private UnityEngine.Coroutine profanityFilterCoroutine;

	private Color invalidColor = new Color(0.7529412f, 0.2509804f, 0.2509804f);

	public AgePrimitiveLabel PanelTitle;

	public AgeTransform LoadButton;

	public AgeTransform SaveButton;

	public AgeTransform DeleteButton;

	public AgeTransform LoadSaveContainer;

	public AgeControlScrollView LoadSaveScrollView;

	public Transform SaveLineSlotPrefab;

	public AgeControlTextField TextInput;

	public AgePrimitiveLabel TextInputLabel;

	public AgeControlToggle CloudToggle;

	public SortButtonsContainer SortsContainer;

	private UnityCoroutine saving;

	private UnityCoroutine listing;
}
