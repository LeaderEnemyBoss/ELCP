using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amplitude;
using Amplitude.Interop;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Session;
using UnityEngine;

public class LoadingScreen : global::GuiScreen
{
	public ISessionService SessionService { get; private set; }

	protected virtual void Update()
	{
		if (base.IsVisible && Diagnostics.Progress.IsProgressing)
		{
			if (this.currentProgress != Diagnostics.Progress.Current)
			{
				this.currentProgress += this.speed * Time.deltaTime;
				this.currentProgress = Mathf.Min(Diagnostics.Progress.Current, this.currentProgress);
			}
			this.format.Length = 0;
			if (Diagnostics.Progress.Message != null)
			{
				this.format.Append(Diagnostics.Progress.Message);
				if (this.format.Length > 0 && this.format[this.format.Length - 1] == '\r')
				{
					this.format.Remove(this.format.Length - 1, 1);
				}
			}
			this.newItem = this.format.ToString();
			if (this.newItem != this.previousItem)
			{
				this.progressStrings.Insert(0, this.newItem);
				this.previousItem = this.newItem;
				while (this.progressStrings.Count > 5)
				{
					this.progressStrings.RemoveAt(this.progressStrings.Count - 1);
				}
				this.ProgressItemTable.ReserveChildren(this.progressStrings.Count, this.ProgressItemPrefab, "Item");
				this.ProgressItemTable.RefreshChildrenIList<string>(this.progressStrings, new AgeTransform.RefreshTableItem<string>(this.RefreshProgressItem), true, false);
				this.ProgressItemTable.ArrangeChildren();
			}
			this.ProgressValue.PercentRight = 100f * this.currentProgress;
		}
	}

	protected override IEnumerator OnHide(bool instant)
	{
		Diagnostics.Progress.ProgressChange -= this.Progress_ProgressChange;
		Diagnostics.MultiplayerProgress.ProgressChange -= this.MultiplayerProgress_ProgressChange;
		LoadingScreen.LoadingInProgress = false;
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		this.validGuiElements = new List<GuiElement>();
		int index = 0;
		GuiElement guiElement;
		while (base.GuiService.GuiPanelHelper.TryGetGuiElement("LoadingScreen" + index.ToString(), out guiElement))
		{
			this.validGuiElements.Add(guiElement);
			index++;
		}
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		this.SessionService = Services.GetService<ISessionService>();
		LoadingScreen.LoadingInProgress = true;
		this.LoadGuiImage(parameters);
		this.progressStrings.Clear();
		this.previousItem = string.Empty;
		Diagnostics.Progress.ProgressChange += this.Progress_ProgressChange;
		this.UpdateProgressBar();
		int nextLoadingTipNumber = Amplitude.Unity.Framework.Application.Registry.GetValue<int>(global::Application.Registers.NextLoadingTipNumber, 1);
		bool dontDisplayAnyLoadingTip = false;
		if (parameters.Length > 0)
		{
			for (int index = 0; index < parameters.Length; index++)
			{
				if (parameters[index] is LoadingScreen.DontDisplayAnyLoadingTip)
				{
					dontDisplayAnyLoadingTip = true;
					break;
				}
			}
		}
		GuiElement guiElement = null;
		if (!dontDisplayAnyLoadingTip && !base.GuiService.GuiPanelHelper.TryGetGuiElement("LoadingTip" + nextLoadingTipNumber, out guiElement) && nextLoadingTipNumber != 1)
		{
			nextLoadingTipNumber = 1;
			base.GuiService.GuiPanelHelper.TryGetGuiElement("LoadingTip1", out guiElement);
		}
		if (guiElement != null)
		{
			this.TipLabel.AgeTransform.Visible = true;
			this.TipLabel.Text = guiElement.Title;
			AgeTransform parent = this.TipLabel.AgeTransform.GetParent();
			if (parent)
			{
				parent.Visible = true;
			}
			nextLoadingTipNumber++;
			Amplitude.Unity.Framework.Application.Registry.SetValue<int>(global::Application.Registers.NextLoadingTipNumber, nextLoadingTipNumber);
		}
		else
		{
			this.TipLabel.AgeTransform.Visible = false;
			AgeTransform parent2 = this.TipLabel.AgeTransform.GetParent();
			if (parent2)
			{
				parent2.Visible = false;
			}
		}
		if (this.SessionService != null && this.SessionService.Session != null && this.SessionService.Session.SessionMode != SessionMode.Single)
		{
			if (parameters.Any((object o) => o is bool && (bool)o))
			{
				this.PlayerLoadStatusGroup.Visible = true;
				Diagnostics.MultiplayerProgress.ProgressChange += this.MultiplayerProgress_ProgressChange;
				this.UpdatePlayerLoadStatus();
				goto IL_348;
			}
		}
		this.PlayerLoadStatusGroup.Visible = false;
		IL_348:
		this.DlcAdPanel.Bind(true);
		yield break;
	}

	private void LoadGuiImage(params object[] parameters)
	{
		if (this.Background.Image != null)
		{
			AgeManager.Instance.ReleaseDynamicTexture(this.Background.Image.name);
		}
		string text = string.Empty;
		GameSaveDescriptor gameSaveDescriptor = null;
		if (parameters.Length > 0)
		{
			for (int i = 0; i < parameters.Length; i++)
			{
				if (parameters[i] is string)
				{
					text = (string)parameters[i];
					this.Background.Image = (Resources.Load(text) as Texture2D);
					if (this.Background.Image != null)
					{
						break;
					}
				}
				if (parameters[i] is GameSaveDescriptor)
				{
					gameSaveDescriptor = (parameters[i] as GameSaveDescriptor);
				}
			}
		}
		if (string.IsNullOrEmpty(text))
		{
			if (this.SessionService != null && this.SessionService.Session != null && this.SessionService.Session.IsOpened)
			{
				int num = 0;
				for (;;)
				{
					string x = string.Format("Empire{0}", num);
					string lobbyData = this.SessionService.Session.GetLobbyData<string>(x, null);
					if (string.IsNullOrEmpty(lobbyData))
					{
						break;
					}
					if (lobbyData.Contains(this.SessionService.Session.SteamIDUser.ToString()))
					{
						goto Block_11;
					}
					num++;
				}
				goto IL_201;
				Block_11:
				string x2 = string.Format("Faction{0}", num);
				string lobbyData2 = this.SessionService.Session.GetLobbyData<string>(x2, null);
				if (!string.IsNullOrEmpty(lobbyData2))
				{
					string[] array = lobbyData2.Split(Amplitude.String.Separators, StringSplitOptions.None);
					string x3 = array[3];
					GuiElement guiElement = null;
					IDatabase<GuiElement> database = Databases.GetDatabase<GuiElement>(false);
					if (database != null && database.TryGetValue(x3, out guiElement))
					{
						text = guiElement.Icons["MoodScore"];
						this.Background.Image = (Resources.Load(text) as Texture2D);
					}
				}
			}
			IL_201:
			if (string.IsNullOrEmpty(text))
			{
				if (gameSaveDescriptor == null)
				{
					IGameSerializationService service = Services.GetService<IGameSerializationService>();
					if (service != null && service.GameSaveDescriptor != null)
					{
						gameSaveDescriptor = service.GameSaveDescriptor;
					}
				}
				if (gameSaveDescriptor != null)
				{
					int num2 = 0;
					for (;;)
					{
						string key = string.Format("Empire{0}", num2);
						string lobbyData3 = gameSaveDescriptor.GameSaveSessionDescriptor.GetLobbyData<string>(key, null);
						if (string.IsNullOrEmpty(lobbyData3))
						{
							break;
						}
						Steamworks.SteamID steamID = Steamworks.SteamAPI.SteamUser.SteamID;
						bool flag = lobbyData3.Contains(steamID.ToString());
						bool flag2 = !lobbyData3.StartsWith("AI");
						if (flag || (flag2 && string.IsNullOrEmpty(text)))
						{
							string key2 = string.Format("Faction{0}", num2);
							string lobbyData4 = gameSaveDescriptor.GameSaveSessionDescriptor.GetLobbyData<string>(key2, null);
							if (!string.IsNullOrEmpty(lobbyData4))
							{
								string[] array2 = lobbyData4.Split(Amplitude.String.Separators, StringSplitOptions.None);
								string x4 = array2[3];
								GuiElement guiElement2 = null;
								IDatabase<GuiElement> database2 = Databases.GetDatabase<GuiElement>(false);
								if (database2 != null && database2.TryGetValue(x4, out guiElement2))
								{
									text = guiElement2.Icons["MoodScore"];
									this.Background.Image = (Resources.Load(text) as Texture2D);
								}
							}
							if (flag)
							{
								break;
							}
						}
						num2++;
					}
				}
				if (string.IsNullOrEmpty(text))
				{
					if (this.validGuiElements != null && this.validGuiElements.Count != 0)
					{
						GuiElement guiElement3 = this.validGuiElements[UnityEngine.Random.Range(0, this.validGuiElements.Count)];
						Texture2D image;
						if (base.GuiService.GuiPanelHelper.TryGetTextureFromIcon(guiElement3, global::GuiPanel.IconSize.MoodScore, out image))
						{
							this.Background.Image = image;
						}
					}
					else
					{
						this.Background.Image = (Resources.Load("GUI/DynamicBitmaps/Backdrop/Pov_Auriga") as Texture2D);
					}
				}
			}
		}
	}

	private void MultiplayerProgress_ProgressChange(Steamworks.SteamID steamIDUser, int empireIndex, string status)
	{
		Diagnostics.Log("[MP_Progress] Empire#{0} {1} {2}", new object[]
		{
			empireIndex,
			steamIDUser,
			status
		});
		this.UpdatePlayerLoadStatus();
	}

	private void Progress_ProgressChange(object sender, EventArgs e)
	{
		this.UpdateProgressBar();
	}

	private void RefreshProgressItem(AgeTransform item, string text, int index)
	{
		AgePrimitiveLabel component = item.GetComponent<AgePrimitiveLabel>();
		component.Text = text;
		item.Alpha = 1f - 0.2f * (float)index;
	}

	private void UpdateProgressBar()
	{
		if (this.lastCaption != Diagnostics.Progress.Caption)
		{
			this.currentProgress = 0f;
		}
		else if (this.lastProgress > Diagnostics.Progress.Current)
		{
			this.currentProgress = 0f;
		}
		else if (this.lastProgress == 1f)
		{
			this.currentProgress = 0f;
		}
		this.lastProgress = Diagnostics.Progress.Current;
		this.lastCaption = Diagnostics.Progress.Caption;
	}

	private void UpdatePlayerLoadStatus()
	{
		Diagnostics.MultiplayerProgress.UserProgress[] userProgresses = Diagnostics.MultiplayerProgress.UserProgresses;
		this.PlayerLoadStatusTable.ReserveChildren(userProgresses.Length, this.PlayerLoadStatusPrefab, "Item");
		this.PlayerLoadStatusTable.RefreshChildrenIList<Diagnostics.MultiplayerProgress.UserProgress>(userProgresses, new AgeTransform.RefreshTableItem<Diagnostics.MultiplayerProgress.UserProgress>(this.UpdatePlayerLoadStatusItem), true, false);
	}

	private void UpdatePlayerLoadStatusItem(AgeTransform tableItem, Diagnostics.MultiplayerProgress.UserProgress userProgress, int index)
	{
		PlayerLoadStatus component = tableItem.GetComponent<PlayerLoadStatus>();
		component.PlayerStatus.Text = string.Empty;
		if (!string.IsNullOrEmpty(userProgress.Status))
		{
			component.PlayerStatus.Text = string.Format("%LoadingUserStatus_{0}", userProgress.Status);
		}
		component.PlayerTitle.Text = "Default";
		if (Steamworks.SteamAPI.IsSteamRunning)
		{
			component.PlayerTitle.Text = Steamworks.SteamAPI.SteamFriends.GetFriendPersonaName(userProgress.SteamIDUser);
		}
	}

	public static bool LoadingInProgress;

	public AgePrimitiveImage Background;

	public AgeTransform ProgressValue;

	public AgeTransform ProgressItemTable;

	public Transform ProgressItemPrefab;

	public AgePrimitiveLabel TipLabel;

	public AgeTransform PlayerLoadStatusGroup;

	public AgeTransform PlayerLoadStatusTable;

	public Transform PlayerLoadStatusPrefab;

	public DlcAdPanel DlcAdPanel;

	private List<GuiElement> validGuiElements;

	private float currentProgress;

	private float lastProgress;

	private List<string> progressStrings = new List<string>();

	private float speed = 2f;

	private string lastCaption = string.Empty;

	private StringBuilder format = new StringBuilder();

	private string newItem = string.Empty;

	private string previousItem = string.Empty;

	public static class Bitmaps
	{
		public const string DefaultAuriga = "GUI/DynamicBitmaps/Backdrop/Pov_Auriga";

		public const string OutGameVanilla = "GUI/DynamicBitmaps/Backdrop/MainMenuBackground0";

		public const string Test1 = "GUI/DynamicBitmaps/Backdrop/MainMenuBackground1";

		public const string Test2 = "GUI/DynamicBitmaps/Backdrop/MainMenuBackground2";
	}

	public class DontDisplayAnyLoadingTip
	{
	}
}
