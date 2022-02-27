using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Runtime;
using UnityEngine;

public class MenuOnlineSessionLine : SortedLine
{
	public MenuJoinGameScreen Parent { get; set; }

	public MenuJoinGameScreen.LobbyDescription LobbyDescription { get; set; }

	public string Name
	{
		get
		{
			if (this.LobbyDescription.HasFlag(MenuJoinGameScreen.LobbyDescription.LobbyFlag.VersionMismatch))
			{
				return string.Format("#FF0000#{0} {1}#REVERT#", this.LobbyDescription.Name, this.LobbyDescription.Version.ToString("(V{0}.{1}.{2} S{3})"));
			}
			if (this.LobbyDescription.HasFlag(MenuJoinGameScreen.LobbyDescription.LobbyFlag.FriendsOnly | MenuJoinGameScreen.LobbyDescription.LobbyFlag.FriendlyLobby))
			{
				return string.Format("#00C000#{0}#REVERT#", this.LobbyDescription.Name);
			}
			if (this.LobbyDescription.HasFlag(MenuJoinGameScreen.LobbyDescription.LobbyFlag.FriendlyLobby))
			{
				return string.Format("#00C0FF#{0}#REVERT#", this.LobbyDescription.Name);
			}
			return this.LobbyDescription.Name;
		}
	}

	public MenuOnlineSessionLine.SortedPlayersCount PlayersCount
	{
		get
		{
			return new MenuOnlineSessionLine.SortedPlayersCount(this.LobbyDescription.OccupiedSlots, this.LobbyDescription.FreeSlots);
		}
	}

	public MenuOnlineSessionLine.SortedWorldType WorldType
	{
		get
		{
			return new MenuOnlineSessionLine.SortedWorldType(this.LobbyDescription.WorldSize, this.LobbyDescription.WorldShape, this.LobbyDescription.WorldTemperature, this.LobbyDescription.WorldWrap);
		}
	}

	public MenuOnlineSessionLine.GameStatus GameState
	{
		get
		{
			if (this.LobbyDescription.GameInProgress)
			{
				return MenuOnlineSessionLine.GameStatus.InProgress;
			}
			if (this.LobbyDescription.Launching)
			{
				return MenuOnlineSessionLine.GameStatus.Launching;
			}
			if (this.LobbyDescription.IsMultiplayerSave)
			{
				return MenuOnlineSessionLine.GameStatus.InMultiplayerSaveLobby;
			}
			return MenuOnlineSessionLine.GameStatus.InNewGameLobby;
		}
	}

	public string Difficulty
	{
		get
		{
			return "%GameOptionGameDifficulty" + this.LobbyDescription.GameDifficulty + "Title";
		}
	}

	public string Speed
	{
		get
		{
			return "%GameOptionGameSpeed" + this.LobbyDescription.GameSpeed + "Title";
		}
	}

	public string Timers
	{
		get
		{
			return string.Format("{0}, {1}", AgeLocalizer.Instance.LocalizeString("%GameOptionTimedEncounter" + this.LobbyDescription.TimedTurns + "Title"), AgeLocalizer.Instance.LocalizeString("%GameOptionTimedEncounter" + this.LobbyDescription.TimedEncounters + "Title"));
		}
	}

	public string WithCustomFactions
	{
		get
		{
			return AgeLocalizer.Instance.LocalizeString("%GameOptionCustomFactions" + this.LobbyDescription.WithCustomFactions + "Title");
		}
	}

	public int Turn
	{
		get
		{
			if (this.GameState == MenuOnlineSessionLine.GameStatus.InProgress || this.GameState == MenuOnlineSessionLine.GameStatus.InMultiplayerSaveLobby || this.GameState == MenuOnlineSessionLine.GameStatus.Launching)
			{
				return this.LobbyDescription.Turn + 1;
			}
			return 0;
		}
	}

	public GameSaveDescriptor GameSaveDescriptor { get; private set; }

	public RuntimeModuleConfigurationState GameSaveRuntimeConfigurationState { get; private set; }

	public void RefreshContent()
	{
		if (this.LobbyDescription == null)
		{
			Diagnostics.LogError("Trying to refresh a GameSessionLine that doesn't have a LobbyDescription");
			return;
		}
		this.NameLabel.Text = this.Name;
		this.PlayersLabel.Text = this.PlayersCount.ToString();
		this.WorldTypeLabel.Text = this.WorldType.ToString();
		this.DifficultyLabel.Text = this.Difficulty;
		this.SpeedLabel.Text = this.Speed;
		this.CustomFactionsLabel.Text = this.WithCustomFactions;
		this.TimersLabel.Text = this.Timers;
		this.GameStateLabel.Text = AgeLocalizer.Instance.LocalizeString("%JoinGameStatus" + this.GameState.ToString() + "Title");
		this.TurnLabel.Text = this.Turn.ToString();
		bool flag = false;
		IRuntimeService service = Services.GetService<IRuntimeService>();
		if (service != null && service.Runtime != null && !string.IsNullOrEmpty(service.Runtime.HashKey) && !string.IsNullOrEmpty(this.LobbyDescription.Hash) && service.Runtime.HashKey == this.LobbyDescription.Hash)
		{
			flag = true;
		}
		string workshopContentTooltip = this.GetWorkshopContentTooltip(ref flag);
		if (this.LobbyDescription.Version != Amplitude.Unity.Framework.Application.Version)
		{
			this.AgeTransform.Enable = (Amplitude.Unity.Framework.Application.Version.Accessibility <= Accessibility.Internal);
			this.NameLabel.TintColor = this.ErrorColor;
			this.AgeTransform.StopSearchingForTooltips = true;
			this.AgeTransform.AgeTooltip.Content = string.Format(AgeLocalizer.Instance.LocalizeString("%JoinGameVersionMismatchDescription"), Amplitude.Unity.Framework.Application.Version.ToString(), this.LobbyDescription.Version.ToString());
		}
		else if (this.LobbyDescription.HasFlag(MenuJoinGameScreen.LobbyDescription.LobbyFlag.RuntimeConfigurationMismatch))
		{
			this.AgeTransform.Enable = true;
			this.AgeTransform.StopSearchingForTooltips = true;
			RuntimeModuleConfigurationState gameSaveRuntimeConfigurationState = this.GameSaveRuntimeConfigurationState;
			if (gameSaveRuntimeConfigurationState != RuntimeModuleConfigurationState.Yellow)
			{
				if (gameSaveRuntimeConfigurationState == RuntimeModuleConfigurationState.Red)
				{
					this.NameLabel.TintColor = this.ErrorColor;
				}
			}
			else
			{
				this.NameLabel.TintColor = this.WarningColor;
			}
			this.AgeTransform.AgeTooltip.Content = workshopContentTooltip;
		}
		else if (!flag)
		{
			this.AgeTransform.Enable = (Amplitude.Unity.Framework.Application.Version.Accessibility <= Accessibility.Internal);
			this.NameLabel.TintColor = this.WarningColor;
			this.AgeTransform.StopSearchingForTooltips = true;
			this.AgeTransform.AgeTooltip.Content = "%JoinGameCheckSumMismatchDescription";
		}
		else
		{
			this.AgeTransform.Enable = true;
			this.NameLabel.TintColor = Color.white;
			this.AgeTransform.StopSearchingForTooltips = false;
			this.AgeTransform.AgeTooltip.Content = string.Empty;
			this.RefreshDownloadableContentsTooltip();
			this.RefreshVictoryConditionTooltip();
			this.RefreshDLC13Tooltip();
		}
	}

	private string GetWorkshopContentTooltip(ref bool runtimeCheck)
	{
		IRuntimeModuleSubscriptionService service = Services.GetService<IRuntimeModuleSubscriptionService>();
		string[] source = null;
		string[] source2 = null;
		string str = string.Empty;
		if (service != null)
		{
			this.GameSaveRuntimeConfigurationState = service.GetRuntimeModuleListState(this.LobbyDescription.RuntimeConfiguration, out source, out source2);
		}
		RuntimeModuleConfigurationState gameSaveRuntimeConfigurationState = this.GameSaveRuntimeConfigurationState;
		if (gameSaveRuntimeConfigurationState != RuntimeModuleConfigurationState.Yellow)
		{
			if (gameSaveRuntimeConfigurationState == RuntimeModuleConfigurationState.Red)
			{
				runtimeCheck = true;
				str = AgeLocalizer.Instance.LocalizeString("%MPSessionInvalidRuntimeModulesDescription") + "\n \n - ";
			}
		}
		else
		{
			runtimeCheck = true;
			str = AgeLocalizer.Instance.LocalizeString("%MPSessionCanBeFixedRuntimeModulesDescription") + "\n \n - ";
		}
		IDatabase<RuntimeModule> database = Databases.GetDatabase<RuntimeModule>(true);
		List<string> list = new List<string>();
		int i = 0;
		int num = this.LobbyDescription.RuntimeConfiguration.Length;
		while (i < num)
		{
			if (source.Contains(this.LobbyDescription.RuntimeConfiguration[i]) || source2.Contains(this.LobbyDescription.RuntimeConfiguration[i]))
			{
				if (this.LobbyDescription.RuntimeConfiguration[i].Contains(global::RuntimeManager.Folders.Workshop.Affix))
				{
					string newValue = this.LobbyDescription.RuntimeConfiguration[i].Replace(global::RuntimeManager.Folders.Workshop.Affix, string.Empty);
					list.Add(AgeLocalizer.Instance.LocalizeString("%MissingModuleTitle").Replace("$ModuleFolder", "Workshop").Replace("$ModuleName", newValue));
				}
				else if (this.LobbyDescription.RuntimeConfiguration[i].Contains(global::RuntimeManager.Folders.UGC.Affix))
				{
					string newValue2 = this.LobbyDescription.RuntimeConfiguration[i].Replace(global::RuntimeManager.Folders.UGC.Affix, string.Empty);
					list.Add(AgeLocalizer.Instance.LocalizeString("%MissingModuleTitle").Replace("$ModuleFolder", "UGC").Replace("$ModuleName", newValue2));
				}
				else
				{
					list.Add(AgeLocalizer.Instance.LocalizeString("%MissingModuleTitle").Replace("$ModuleFolder", "Community").Replace("$ModuleName", this.LobbyDescription.RuntimeConfiguration[i]));
				}
			}
			else
			{
				RuntimeModule runtimeModule = database.FirstOrDefault((RuntimeModule module) => module.Name == this.LobbyDescription.RuntimeConfiguration[i]);
				if (runtimeModule != null)
				{
					list.Add(AgeLocalizer.Instance.LocalizeString(runtimeModule.Title));
				}
			}
			i++;
		}
		return str + string.Join("\n - ", list.ToArray());
	}

	private void RefreshDownloadableContentsTooltip()
	{
		if (this.LobbyDescription.DownloadableContentSharedByServer != 0u)
		{
			string text = string.Empty;
			uint num = this.LobbyDescription.DownloadableContentSharedByServer;
			IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
			if (service != null)
			{
				foreach (DownloadableContent downloadableContent in service)
				{
					uint num2 = 1u << (int)downloadableContent.Number;
					if ((num & num2) != 0u)
					{
						if (downloadableContent.Type == DownloadableContentType.Exclusive)
						{
							IDatabase<GuiElement> database = Databases.GetDatabase<GuiElement>(true);
							ExtendedGuiElement extendedGuiElement = database.GetValue(downloadableContent.Name) as ExtendedGuiElement;
							if (extendedGuiElement != null && !string.IsNullOrEmpty(extendedGuiElement.SymbolString))
							{
								string text2 = text;
								text = string.Concat(new string[]
								{
									text2,
									extendedGuiElement.SymbolString,
									" ",
									AgeLocalizer.Instance.LocalizeString(extendedGuiElement.Title),
									"\n"
								});
							}
						}
						num &= ~num2;
						if (num == 0u)
						{
							break;
						}
					}
				}
				if (!string.IsNullOrEmpty(text))
				{
					string str = AgeLocalizer.Instance.LocalizeStringDefaults("%DownloadableContentSharedByServerTooltip", "Shared Content: $SBS").Replace("$SBS", text);
					AgeTooltip ageTooltip = this.AgeTransform.AgeTooltip;
					ageTooltip.Content += str;
					this.AgeTransform.StopSearchingForTooltips = true;
				}
			}
		}
	}

	private void RefreshVictoryConditionTooltip()
	{
		if (this.LobbyDescription.VictoryConditions != null)
		{
			if (!string.IsNullOrEmpty(this.AgeTransform.AgeTooltip.Content))
			{
				AgeTooltip ageTooltip = this.AgeTransform.AgeTooltip;
				ageTooltip.Content += "\n";
			}
			if (this.LobbyDescription.VictoryConditions.Length > 0)
			{
				IDatabase<GuiElement> database = Databases.GetDatabase<GuiElement>(true);
				List<string> list = new List<string>(this.LobbyDescription.VictoryConditions.Length);
				for (int i = 0; i < this.LobbyDescription.VictoryConditions.Length; i++)
				{
					string x = "VictoryCondition" + this.LobbyDescription.VictoryConditions[i].Name;
					GuiElement value = database.GetValue(x);
					if (value != null)
					{
						string item = AgeLocalizer.Instance.LocalizeString(value.Title);
						list.Add(item);
					}
				}
				string str = AgeLocalizer.Instance.LocalizeStringDefaults("%VictoryConditionsTooltip", "Victory conditions: $Conditions").Replace("$Conditions", string.Join(", ", list.ToArray()));
				AgeTooltip ageTooltip2 = this.AgeTransform.AgeTooltip;
				ageTooltip2.Content += str;
			}
			else
			{
				string str2 = AgeLocalizer.Instance.LocalizeStringDefaults("%VictoryConditionsNoneTooltip", "Victory conditions: none");
				AgeTooltip ageTooltip3 = this.AgeTransform.AgeTooltip;
				ageTooltip3.Content += str2;
			}
			this.AgeTransform.StopSearchingForTooltips = true;
		}
	}

	private void RefreshDLC13Tooltip()
	{
		if (this.LobbyDescription.DownloadableContentSharedByServer != 0u)
		{
			uint downloadableContentSharedByServer = this.LobbyDescription.DownloadableContentSharedByServer;
			IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
			if (service != null)
			{
				foreach (DownloadableContent downloadableContent in service)
				{
					uint num = 1u << (int)downloadableContent.Number;
					if ((downloadableContentSharedByServer & num) != 0u && downloadableContent.Type == DownloadableContentType.Exclusive && downloadableContent.Name == DownloadableContent13.ReadOnlyName)
					{
						string key = (!this.LobbyDescription.AdvancedSeasons) ? "%GameOptionSeasonDifficultyVanillaTitle" : ("%GameOptionSeasonDifficulty" + this.LobbyDescription.SeasonDifficulty + "Title");
						AgeTooltip ageTooltip = this.AgeTransform.AgeTooltip;
						ageTooltip.Content = ageTooltip.Content + "\n" + AgeLocalizer.Instance.LocalizeString("%AdvancedWinterSettingTooltip").Replace("$WinterSetting", AgeLocalizer.Instance.LocalizeString(key));
						break;
					}
				}
			}
		}
		string key2 = "%GameOptionEmpireInfoAccessibility" + this.LobbyDescription.EmpireInfoAccessibility + "Title";
		AgeTooltip ageTooltip2 = this.AgeTransform.AgeTooltip;
		ageTooltip2.Content = ageTooltip2.Content + "\n" + AgeLocalizer.Instance.LocalizeString("%EmpireScoresSettingTooltip").Replace("$ScoresSetting", AgeLocalizer.Instance.LocalizeString(key2));
		this.AgeTransform.StopSearchingForTooltips = true;
	}

	private void OnToggleLineCB(GameObject gameObject)
	{
		this.Parent.OnToggleLine(this);
	}

	private void OnDoubleClickLineCB(GameObject gameObject)
	{
		this.Parent.OnDoubleClickLine(this);
	}

	public AgeControlToggle SelectionToggle;

	public AgePrimitiveLabel NameLabel;

	public AgePrimitiveLabel PlayersLabel;

	public AgePrimitiveLabel WorldTypeLabel;

	public AgePrimitiveLabel GameStateLabel;

	public AgePrimitiveLabel DifficultyLabel;

	public AgePrimitiveLabel SpeedLabel;

	public AgePrimitiveLabel CustomFactionsLabel;

	public AgePrimitiveLabel TimersLabel;

	public AgePrimitiveLabel TurnLabel;

	public Color OkColor = Color.green;

	public Color WarningColor = Color.yellow;

	public Color ErrorColor = Color.red;

	public enum GameStatus
	{
		InNewGameLobby,
		InMultiplayerSaveLobby,
		Launching,
		InProgress
	}

	public struct SortedPlayersCount : IComparable, IComparable<MenuOnlineSessionLine.SortedPlayersCount>
	{
		public SortedPlayersCount(int occupiedSlots, int freeSlots)
		{
			this.occupiedSlots = occupiedSlots;
			this.freeSlots = freeSlots;
			this.totalSlots = this.occupiedSlots + this.freeSlots;
		}

		public int CompareTo(MenuOnlineSessionLine.SortedPlayersCount other)
		{
			if (this.totalSlots != other.totalSlots)
			{
				return this.totalSlots.CompareTo(other.totalSlots);
			}
			return this.freeSlots.CompareTo(other.freeSlots);
		}

		public int CompareTo(object obj)
		{
			return this.CompareTo((MenuOnlineSessionLine.SortedPlayersCount)obj);
		}

		public override string ToString()
		{
			return this.occupiedSlots.ToString() + "/" + this.totalSlots.ToString();
		}

		private int occupiedSlots;

		private int freeSlots;

		private int totalSlots;
	}

	public struct SortedWorldType : IComparable, IComparable<MenuOnlineSessionLine.SortedWorldType>
	{
		public SortedWorldType(string worldSize, string worldShape, string worldTemperature, string worldWrap)
		{
			this.worldSize = new MenuOnlineSessionLine.SortedWorldGeneratorOptionBasedValue("WorldSize", worldSize);
			this.worldShape = new MenuOnlineSessionLine.SortedWorldGeneratorOptionBasedValue("WorldShape", worldShape);
			this.worldTemperature = new MenuOnlineSessionLine.SortedWorldGeneratorOptionBasedValue("Temperature", worldTemperature);
			this.worldWrap = new MenuOnlineSessionLine.SortedWorldGeneratorOptionBasedValue("WorldWrap", worldWrap);
		}

		public int CompareTo(MenuOnlineSessionLine.SortedWorldType other)
		{
			if (this.worldSize != other.worldSize)
			{
				return this.worldSize.CompareTo(other.worldSize);
			}
			if (this.worldShape != other.worldShape)
			{
				return this.worldShape.CompareTo(other.worldShape);
			}
			if (this.worldTemperature != other.worldTemperature)
			{
				return this.worldTemperature.CompareTo(other.worldTemperature);
			}
			return this.worldWrap.CompareTo(other.worldWrap != null);
		}

		public int CompareTo(object obj)
		{
			return this.CompareTo((MenuOnlineSessionLine.SortedWorldType)obj);
		}

		public override string ToString()
		{
			return string.Format("{0}, {1}, {2}, {3}", new object[]
			{
				AgeLocalizer.Instance.LocalizeString("%WorldGeneratorOptionWorldSize" + this.worldSize + "Title"),
				AgeLocalizer.Instance.LocalizeString("%WorldGeneratorOptionWorldShape" + this.worldShape + "Title"),
				AgeLocalizer.Instance.LocalizeString("%WorldGeneratorOptionTemperature" + this.worldTemperature + "Title"),
				AgeLocalizer.Instance.LocalizeString("%WorldGeneratorOptionWorldWrap" + this.worldWrap + "Title")
			});
		}

		private MenuOnlineSessionLine.SortedWorldGeneratorOptionBasedValue worldSize;

		private MenuOnlineSessionLine.SortedWorldGeneratorOptionBasedValue worldShape;

		private MenuOnlineSessionLine.SortedWorldGeneratorOptionBasedValue worldTemperature;

		private MenuOnlineSessionLine.SortedWorldGeneratorOptionBasedValue worldWrap;
	}

	public struct SortedWorldGeneratorOptionBasedValue : IComparable, IComparable<MenuOnlineSessionLine.SortedWorldGeneratorOptionBasedValue>
	{
		public SortedWorldGeneratorOptionBasedValue(StaticString optionDefinitionName, string value)
		{
			this.value = value;
			this.optionDefinition = null;
			this.itemPositionInOptionDefinition = -1;
			IDatabase<WorldGeneratorOptionDefinition> database = Databases.GetDatabase<WorldGeneratorOptionDefinition>(true);
			if (database.TryGetValue(optionDefinitionName, out this.optionDefinition) && this.optionDefinition.ItemDefinitions != null)
			{
				for (int i = 0; i < this.optionDefinition.ItemDefinitions.Length; i++)
				{
					if (this.optionDefinition.ItemDefinitions[i].Name == this.value)
					{
						this.itemPositionInOptionDefinition = i;
						break;
					}
				}
			}
		}

		public override bool Equals(object obj)
		{
			bool result;
			try
			{
				result = this.value.Equals(((MenuOnlineSessionLine.SortedWorldGeneratorOptionBasedValue)obj).value);
			}
			catch
			{
				result = false;
			}
			return result;
		}

		public override int GetHashCode()
		{
			return this.value.GetHashCode();
		}

		public int CompareTo(MenuOnlineSessionLine.SortedWorldGeneratorOptionBasedValue other)
		{
			if (this.optionDefinition == null || other.optionDefinition == null)
			{
				return this.value.CompareTo(other.value);
			}
			return this.itemPositionInOptionDefinition.CompareTo(other.itemPositionInOptionDefinition);
		}

		public int CompareTo(object obj)
		{
			return this.CompareTo((MenuOnlineSessionLine.SortedWorldGeneratorOptionBasedValue)obj);
		}

		public override string ToString()
		{
			return this.value.ToString();
		}

		public static bool operator ==(MenuOnlineSessionLine.SortedWorldGeneratorOptionBasedValue left, MenuOnlineSessionLine.SortedWorldGeneratorOptionBasedValue right)
		{
			return left.value == right.value;
		}

		public static bool operator !=(MenuOnlineSessionLine.SortedWorldGeneratorOptionBasedValue left, MenuOnlineSessionLine.SortedWorldGeneratorOptionBasedValue right)
		{
			return left.value != right.value;
		}

		private WorldGeneratorOptionDefinition optionDefinition;

		private string value;

		private int itemPositionInOptionDefinition;
	}
}
