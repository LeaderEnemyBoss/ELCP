using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Interop;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Session;
using UnityEngine;

public class MenuCompetitorSlot : MonoBehaviour
{
	public bool CompetitorIsHuman { get; private set; }

	public bool CompetitorIsLocalOwner { get; private set; }

	public int EmpireIndex { get; private set; }

	private bool IsReadOnly
	{
		get
		{
			return !this.canModifyOwnEmpireSettings || !this.CompetitorIsLocalOwner || this.LockEmpireToggle.State;
		}
	}

	private bool IsMySlot
	{
		get
		{
			return this.CompetitorIsHuman && this.CompetitorIsLocalOwner;
		}
	}

	private global::Session Session { get; set; }

	public void Bind(global::Session session, int empireIndex, MenuNewGameScreen parent)
	{
		this.Session = session;
		this.EmpireIndex = empireIndex;
	}

	public void Unbind()
	{
		this.EmpireIndex = -1;
		this.Session = null;
	}

	public void RefreshContent(List<GuiFaction> guiFactions, List<Color> colorsList, bool isGameAlreadyLaunchedOnce, bool canModifyOwnEmpireSettings, bool guiLocked)
	{
		if (this.Session == null || !this.Session.IsOpened)
		{
			this.AgeTransform.Enable = false;
			return;
		}
		this.AgeTransform.Enable = true;
		this.CompetitorIsHuman = (this.EmpireIndex == 0);
		this.CompetitorIsLocalOwner = false;
		this.hasGameBeenLaunchedOnce = isGameAlreadyLaunchedOnce;
		this.canModifyOwnEmpireSettings = canModifyOwnEmpireSettings;
		string x = string.Format("LockEmpire{0}", this.EmpireIndex);
		this.empireLocked = this.Session.GetLobbyData<bool>(x, false);
		this.LockEmpireToggle.AgeTransform.Enable = this.Session.IsHosting;
		this.LockEmpireToggle.State = this.empireLocked;
		string text = string.Format("Empire{0}", this.EmpireIndex);
		string lobbyData = this.Session.GetLobbyData<string>(text, null);
		if (string.IsNullOrEmpty(lobbyData))
		{
			Diagnostics.LogError("Lobby data is null for 'keyLobbyDataEmpire' (keyLobbyDataEmpire: '{0}').", new object[]
			{
				text
			});
			base.GetComponent<AgeTransform>().Enable = false;
			return;
		}
		if (lobbyData.StartsWith("AI"))
		{
			this.CompetitorIsHuman = false;
			this.CompetitorIsLocalOwner = this.Session.IsHosting;
		}
		else
		{
			this.CompetitorIsHuman = true;
		}
		for (int i = 0; i < this.PlayersTitleLabels.Length; i++)
		{
			this.PlayersTitleLabels[i].AgeTransform.Visible = false;
			this.PlayerReadyToggles[i].AgeTransform.Visible = false;
			this.PlayerKickButtons[i].AgeTransform.Visible = false;
			this.PlayerHostingIcons[i].AgeTransform.Visible = false;
		}
		if (this.CompetitorIsHuman)
		{
			this.EmpireType.Text = "%CompetitorEmpireTypeHumanTitle";
			this.EmpireType.AgeTransform.AgeTooltip.Content = "%CompetitorEmpireTypeHumanDescription";
			string[] array = lobbyData.Split(Amplitude.String.Separators, StringSplitOptions.RemoveEmptyEntries);
			for (int j = 0; j < array.Length; j++)
			{
				this.RefreshPlayerLine(array[j], j);
			}
		}
		else
		{
			this.EmpireType.Text = "%CompetitorEmpireTypeComputerTitle";
			this.EmpireType.AgeTransform.AgeTooltip.Content = "%CompetitorEmpireTypeComputerDescription";
			this.PlayersTitleLabels[0].AgeTransform.Visible = true;
			this.PlayersTitleLabels[0].Text = "%CompetitorAIPlayerTitle";
		}
		string x2 = string.Format("_EmpireReserved{0}", this.EmpireIndex);
		string lobbyData2 = this.Session.GetLobbyData<string>(x2, null);
		if (!string.IsNullOrEmpty(lobbyData2))
		{
			string[] array2 = lobbyData2.Split(Amplitude.String.Separators, StringSplitOptions.RemoveEmptyEntries);
			List<Steamworks.SteamID> list = new List<Steamworks.SteamID>();
			for (int k = 0; k < array2.Length; k++)
			{
				ulong num = Convert.ToUInt64(array2[k], 16);
				if (num != 0UL)
				{
					list.Add(new Steamworks.SteamID(num));
				}
			}
			if (list.Count > 0)
			{
				string text2 = string.Empty;
				for (int l = 0; l < list.Count; l++)
				{
					Steamworks.SteamAPI.SteamFriends.RequestUserInformation(list[l], true);
					text2 = text2 + ((text2.Length <= 0) ? string.Empty : ",") + Steamworks.SteamAPI.SteamFriends.GetFriendPersonaName(list[l]);
				}
				this.EmpireType.Text = string.Format("{0} #A0A0A0#({1})#REVERT#", AgeLocalizer.Instance.LocalizeString(this.EmpireType.Text), text2);
			}
		}
		this.EmpireNumber.Text = (this.EmpireIndex + 1).ToString();
		this.guiFactions = new List<GuiFaction>(guiFactions);
		this.RefreshSelectedFaction();
		if (!this.Session.GetLobbyData<bool>("CustomFactions", true))
		{
			for (int m = 0; m < this.guiFactions.Count; m++)
			{
				if (this.guiFactions[m].IsCustom)
				{
					this.FactionDroplist.EnableItem(m, false);
					this.FactionDroplist.SetItemTooltip(m, "%CustomFactionsNotAllowed");
				}
				else
				{
					this.FactionDroplist.EnableItem(m, true);
				}
			}
			if (this.GetSelectedFaction().IsCustom)
			{
				Faction faction = this.guiFactions[0].Faction;
				IDatabase<Faction> database = Databases.GetDatabase<Faction>(false);
				if (database != null)
				{
					faction = database.FirstOrDefault((Faction iterator) => iterator.IsStandard && !iterator.IsHidden);
				}
				this.SelectFaction(faction);
				this.AgeTransform.Enable = false;
				return;
			}
		}
		else
		{
			Faction selectedFaction = this.GetSelectedFaction();
			for (int n = 0; n < this.guiFactions.Count; n++)
			{
				if (this.guiFactions[n].IsCustom && !GuiFaction.IsValidCustomFaction(this.guiFactions[n].Faction, null))
				{
					this.FactionDroplist.EnableItem(n, false);
					this.FactionDroplist.SetItemTooltip(n, "%CustomFactionInvalidCustomFactionTitle");
					if (selectedFaction != null && selectedFaction.Name == this.guiFactions[n].Faction.Name)
					{
						this.SelectFaction(this.guiFactions[0].Faction);
					}
				}
			}
		}
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		if (service != null)
		{
			for (int num2 = 0; num2 < this.guiFactions.Count; num2++)
			{
				if (this.guiFactions[num2].IsStandard || this.guiFactions[num2].IsCustom)
				{
					bool flag = false;
					if (!service.TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory.LobbyFaction, this.guiFactions[num2].Name, out flag) || !flag)
					{
						this.FactionDroplist.EnableItem(num2, false);
						this.FactionDroplist.SetItemTooltip(num2, "%RestrictedDownloadableContentTitle");
					}
					else if (this.guiFactions[num2].Faction.Affinity != null && (!service.TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory.LobbyFactionAffinity, this.guiFactions[num2].Faction.Affinity, out flag) || !flag))
					{
						this.FactionDroplist.EnableItem(num2, false);
						this.FactionDroplist.SetItemTooltip(num2, "%RestrictedDownloadableContentTitle");
					}
					else
					{
						foreach (FactionTrait factionTrait in Faction.EnumerableTraits(this.guiFactions[num2].Faction))
						{
							if (!service.TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory.LobbyFactionTrait, factionTrait.Name, out flag) || !flag)
							{
								this.FactionDroplist.EnableItem(num2, false);
								this.FactionDroplist.SetItemTooltip(num2, "%RestrictedDownloadableContentTitle");
								break;
							}
						}
					}
				}
			}
		}
		ISessionService service2 = Services.GetService<ISessionService>();
		Faction selectedFaction2 = this.GetSelectedFaction();
		if (this.FactionConstrainedIcon != null)
		{
			if (Faction.IsOptionDefinitionConstrained(selectedFaction2, service2.Session))
			{
				this.FactionConstrainedIcon.AgeTransform.Visible = true;
			}
			else
			{
				this.FactionConstrainedIcon.AgeTransform.Visible = false;
			}
		}
		this.RefreshColorDropList(colorsList);
		bool flag2 = !this.empireLocked && !guiLocked;
		if (!this.CompetitorIsHuman && this.Session.SessionMode != SessionMode.Single)
		{
			this.JoinButton.AgeTransform.Visible = true;
			this.NoJoinBackground.Visible = false;
			this.JoinButton.AgeTransform.Enable = (flag2 && !this.empireLocked);
		}
		else
		{
			this.JoinButton.AgeTransform.Visible = false;
			this.NoJoinBackground.Visible = true;
		}
		if (this.Session.GetLobbyData<bool>(string.Format("Empire{0}Eliminated", this.EmpireIndex), false))
		{
			this.JoinButton.AgeTransform.Visible = false;
			this.NoJoinBackground.Visible = true;
			this.EmpireType.Text = string.Format("{0} #FF0000#({1})#REVERT#", AgeLocalizer.Instance.LocalizeString(this.EmpireType.Text), AgeLocalizer.Instance.LocalizeString("%CompetitorEliminatedTitle"));
		}
		this.CurrentPlayerHighlight.Visible = this.IsMySlot;
	}

	public void SelectFaction(Faction faction)
	{
		IGuiService service = Services.GetService<IGuiService>();
		if (service != null)
		{
			MenuNewGameScreen guiPanel = service.GetGuiPanel<MenuNewGameScreen>();
			if (guiPanel != null)
			{
				guiPanel.SelectFaction(this.EmpireIndex, faction);
			}
		}
	}

	private GuiFaction GetSelectedGuiFaction(List<GuiFaction> guiFactions)
	{
		Faction selectedFaction = this.GetSelectedFaction();
		if (selectedFaction.IsRandom && !this.hasGameBeenLaunchedOnce)
		{
			return this.guiFactions.First((GuiFaction guiFaction) => guiFaction.Name == "FactionRandom");
		}
		if (selectedFaction.IsCustom)
		{
			bool lobbyData = this.Session.GetLobbyData<bool>("CustomFactions", true);
			if (lobbyData && !this.guiFactions.Any((GuiFaction guiFaction) => guiFaction.Faction.Name == selectedFaction.Name))
			{
				this.guiFactions.Add(new GuiFaction(selectedFaction));
			}
		}
		return this.guiFactions.FirstOrDefault((GuiFaction guiFaction) => guiFaction.Faction.Name == selectedFaction.Name);
	}

	private Faction GetSelectedFaction()
	{
		IGuiService service = Services.GetService<IGuiService>();
		return service.GetGuiPanel<MenuNewGameScreen>().GetSelectedFaction(this.EmpireIndex);
	}

	private void RefreshPlayerLine(string playerName, int playerIndex)
	{
		Steamworks.SteamID steamID = Steamworks.SteamID.Zero;
		if (Steamworks.SteamAPI.IsSteamRunning)
		{
			try
			{
				ulong value = Convert.ToUInt64(playerName, 16);
				steamID = new Steamworks.SteamID(value);
				playerName = Steamworks.SteamAPI.SteamFriends.GetFriendPersonaName(steamID);
			}
			catch
			{
				Diagnostics.LogWarning("Unable to get player name from steam ID " + playerName);
			}
		}
		else
		{
			playerName = Environment.UserName + "(no Steam)";
		}
		this.PlayersTitleLabels[playerIndex].AgeTransform.Visible = true;
		this.PlayersTitleLabels[playerIndex].Text = playerName;
		if (steamID == this.Session.SteamIDUser && playerIndex == 0)
		{
			this.CompetitorIsLocalOwner = true;
		}
		switch (this.Session.SessionMode)
		{
		case SessionMode.Single:
			this.PlayerReadyToggles[playerIndex].AgeTransform.Visible = false;
			this.PlayerKickButtons[playerIndex].AgeTransform.Visible = false;
			break;
		case SessionMode.Private:
		case SessionMode.Protected:
		case SessionMode.Public:
		{
			bool lobbyMemberData = this.Session.GetLobbyMemberData<bool>(steamID, "Ready", false);
			this.PlayerReadyToggles[playerIndex].AgeTransform.Visible = true;
			this.PlayerReadyToggles[playerIndex].State = lobbyMemberData;
			this.PlayerKickButtons[playerIndex].AgeTransform.Visible = (this.CompetitorIsHuman && this.Session.IsHosting && !this.CompetitorIsLocalOwner);
			this.PlayerKickButtons[playerIndex].OnActivateMethod = "OnKickUserCB";
			this.PlayerKickButtons[playerIndex].OnActivateData = steamID.ToString();
			bool visible = steamID == this.Session.LobbyOwnerSteamID;
			this.PlayerHostingIcons[playerIndex].AgeTransform.Visible = visible;
			break;
		}
		default:
			throw new ArgumentOutOfRangeException();
		}
	}

	private void RefreshSelectedFaction()
	{
		GuiFaction selectedGuiFaction = this.GetSelectedGuiFaction(this.guiFactions);
		this.FactionDroplist.ItemTable = (from guiFaction in this.guiFactions
		select guiFaction.IconAndTitle).ToArray<string>();
		this.FactionDroplist.TooltipTable = (from guiFaction in this.guiFactions
		select guiFaction.Description).ToArray<string>();
		this.FactionDroplist.ReadOnly = this.IsReadOnly;
		this.FactionDroplist.OnSelectionObject = base.gameObject;
		this.FactionDroplist.OnSelectionMethod = "OnChangeFactionCB";
		if (selectedGuiFaction == null)
		{
			this.FactionDroplist.SelectedItem = 0;
			this.FactionPortrait.Image = null;
			this.FactionLogo.AgeTransform.Visible = false;
			return;
		}
		this.FactionDroplist.SelectedItem = this.guiFactions.IndexOf(selectedGuiFaction);
		this.FactionPortrait.Image = selectedGuiFaction.GetImageTexture(GuiPanel.IconSize.Leader, false);
		this.FactionLogo.AgeTransform.Visible = true;
		this.FactionLogo.Image = selectedGuiFaction.GetImageTexture(GuiPanel.IconSize.LogoSmall, false);
		this.FactionLogo.TintColor = Color.white;
		this.AdvancedFactionButton.AgeTransform.Enable = !this.IsReadOnly;
	}

	private void RefreshColorDropList(List<Color> colorsList)
	{
		string x = string.Format("Color{0}", this.EmpireIndex);
		string lobbyData = this.Session.GetLobbyData<string>(x, null);
		this.ColorDroplist.ColorTable = colorsList.ToArray();
		this.ColorDroplist.ReadOnly = this.IsReadOnly;
		this.ColorDroplist.OnSelectionObject = base.gameObject;
		this.ColorDroplist.OnSelectionMethod = "OnChangeColorCB";
		if (!string.IsNullOrEmpty(lobbyData))
		{
			try
			{
				this.ColorDroplist.SelectedItem = int.Parse(lobbyData);
			}
			catch
			{
				Diagnostics.LogWarning("Failed to parse the lobbyDataFactionColor");
			}
		}
		else
		{
			Diagnostics.LogWarning("No lobbyDataFactionColor found, falling back to the default color for empire" + this.EmpireIndex);
		}
	}

	private void OnChangeColorCB(GameObject gameObject)
	{
		if (this.CompetitorIsLocalOwner)
		{
			AgeControlDropList component = gameObject.GetComponent<AgeControlDropList>();
			if (component != null)
			{
				int selectedItem = component.SelectedItem;
				string message = string.Format("q:/Color{0}/{1}", this.EmpireIndex, selectedItem);
				this.Session.SendLobbyChatMessage(message);
				if (this.CompetitorIsLocalOwner && this.CompetitorIsHuman)
				{
					Amplitude.Unity.Framework.Application.Registry.SetValue<int>("Preferences/Lobby/Color", selectedItem);
				}
			}
		}
	}

	private void OnChangeFactionCB(GameObject gameObject)
	{
		if (this.CompetitorIsLocalOwner)
		{
			AgeControlDropList component = gameObject.GetComponent<AgeControlDropList>();
			if (component != null)
			{
				GuiFaction guiFaction = this.guiFactions[component.SelectedItem];
				this.SelectFaction(guiFaction.Faction);
			}
		}
	}

	private void OnAdvancedFactionCB(GameObject gameObject)
	{
		IGuiService service = Services.GetService<IGuiService>();
		MenuNewGameScreen guiPanel = service.GetGuiPanel<MenuNewGameScreen>();
		guiPanel.gameObject.SendMessage("OnAdvancedFactionCB", base.gameObject);
	}

	private void OnJoinEmpireCB(GameObject gameObject)
	{
		if (this.Session != null)
		{
			string message = string.Format("q:/Empire{0}", this.EmpireIndex);
			this.Session.SendLobbyChatMessage(message);
		}
	}

	private void OnLockEmpireToggleCB(GameObject gameObject)
	{
		string message = string.Format("q:/LockEmpire{0}/{1}", this.EmpireIndex, this.LockEmpireToggle.State);
		this.Session.SendLobbyChatMessage(message);
	}

	private void OnKickUserCB(GameObject gameObject)
	{
		if (this.Session.IsHosting)
		{
			Diagnostics.Log("[Lobby] You have kicked {0}.", new object[]
			{
				gameObject.GetComponent<AgeControlButton>().OnActivateData
			});
			string message = string.Format("k:/{0}/{1}", gameObject.GetComponent<AgeControlButton>().OnActivateData, "%KickReasonByHost");
			this.Session.SendLobbyChatMessage(message);
		}
	}

	public AgeTransform AgeTransform;

	public AgePrimitiveLabel EmpireNumber;

	public AgePrimitiveImage FactionPortrait;

	public AgeControlButton AdvancedFactionButton;

	public AgePrimitiveImage FactionLogo;

	public AgeControlDropList FactionDroplist;

	public AgeControlDropList ColorDroplist;

	public AgePrimitiveLabel EmpireType;

	public AgeControlToggle LockEmpireToggle;

	public AgePrimitiveLabel[] PlayersTitleLabels;

	public AgeControlToggle[] PlayerReadyToggles;

	public AgeControlButton[] PlayerKickButtons;

	public AgePrimitiveImage[] PlayerHostingIcons;

	public AgeTransform AiDifficultyTitle;

	public AgeControlDropList AiDifficultyDroplist;

	public AgeTransform NoJoinBackground;

	public AgeControlButton JoinButton;

	public AgeTransform CurrentPlayerHighlight;

	public AgePrimitiveImage FactionConstrainedIcon;

	private List<GuiFaction> guiFactions = new List<GuiFaction>();

	private bool hasGameBeenLaunchedOnce;

	private bool canModifyOwnEmpireSettings;

	private bool empireLocked;
}
