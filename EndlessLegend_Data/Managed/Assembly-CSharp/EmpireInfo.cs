using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Interop;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Localization;
using Amplitude.Unity.Session;
using UnityEngine;

public class EmpireInfo : IComparable, IComparable<EmpireInfo>
{
	private EmpireInfo()
	{
	}

	public static EmpireInfo.Accessibility LastAccessibilityLevel { get; private set; }

	public List<int> AlliedIndexList { get; private set; }

	public bool EmpireEliminated { get; set; }

	public int EmpireExplorationBits { get; set; }

	public int EmpireIndex { get; private set; }

	public int EmpireInfiltrationBits { get; set; }

	public string EmpireName { get; private set; }

	public Faction Faction { get; private set; }

	public Color FactionColor { get; private set; }

	public bool IsActiveOrLocalPlayer { get; set; }

	public string LocalizedName { get; private set; }

	public string Players { get; set; }

	public VictoryCondition[] VictoryConditions { get; private set; }

	public static EmpireInfo Read(Amplitude.Unity.Session.Session session, int empireIndex)
	{
		if (session == null)
		{
			throw new ArgumentNullException("session");
		}
		if (empireIndex < 0)
		{
			throw new IndexOutOfRangeException("Empire index must be a positive integer.");
		}
		if (empireIndex == 0)
		{
			try
			{
				string lobbyData = session.GetLobbyData<string>(EmpireInfo.EmpireInfoAccessibility, null);
				EmpireInfo.LastAccessibilityLevel = (EmpireInfo.Accessibility)((int)Enum.Parse(typeof(EmpireInfo.Accessibility), lobbyData));
			}
			catch
			{
				EmpireInfo.LastAccessibilityLevel = EmpireInfo.Accessibility.Default;
			}
		}
		string x = string.Format("Empire{0}", empireIndex);
		string lobbyData2 = session.GetLobbyData<string>(x, null);
		if (string.IsNullOrEmpty(lobbyData2))
		{
			return null;
		}
		string x2 = string.Format("Faction{0}", empireIndex);
		string x3 = string.Format("Color{0}", empireIndex);
		string lobbyData3 = session.GetLobbyData<string>(x2, null);
		string lobbyData4 = session.GetLobbyData<string>(x3, null);
		EmpireInfo empireInfo = new EmpireInfo();
		empireInfo.EmpireIndex = empireIndex;
		empireInfo.EmpireName = "Empire#" + empireIndex;
		empireInfo.Faction = Faction.Decode(lobbyData3);
		empireInfo.FactionColor = Color.white;
		empireInfo.Players = lobbyData2;
		string value = Amplitude.Unity.Framework.Application.Registry.GetValue<string>("Settings/UI/EmpireColorPalette", "Standard");
		IDatabase<Palette> database = Databases.GetDatabase<Palette>(false);
		Palette palette;
		if (database != null && database.TryGetValue(value, out palette))
		{
			if (palette.Colors == null || palette.Colors.Length == 0)
			{
				Diagnostics.LogError("Invalid color palette (name: '{0}').", new object[]
				{
					value
				});
			}
			else
			{
				try
				{
					int num = int.Parse(lobbyData4);
					empireInfo.FactionColor = palette.Colors[num];
				}
				catch
				{
					Diagnostics.LogError("Failed to retrieve faction color from palette (palette name: '{0}', color index: '{1}').", new object[]
					{
						value,
						lobbyData4
					});
				}
			}
		}
		string lobbyData5 = session.GetLobbyData<string>(VictoryCondition.ReadOnlyVictory, null);
		if (!string.IsNullOrEmpty(lobbyData5))
		{
			IDatabase<VictoryCondition> database2 = Databases.GetDatabase<VictoryCondition>(false);
			if (database2 != null)
			{
				char[] separator = new char[]
				{
					'&'
				};
				string[] array = lobbyData5.Split(separator, StringSplitOptions.RemoveEmptyEntries);
				if (array.Length != 0)
				{
					List<VictoryCondition> list = new List<VictoryCondition>();
					string[] array2 = array;
					for (int i = 0; i < array2.Length; i++)
					{
						string[] array3 = array2[i].Split(Amplitude.String.Separators, StringSplitOptions.RemoveEmptyEntries);
						int j = 1;
						while (j < array3.Length)
						{
							int num2;
							if (int.TryParse(array3[j], out num2) && num2 == empireIndex)
							{
								VictoryCondition item;
								if (database2.TryGetValue(array3[0], out item))
								{
									list.Add(item);
									break;
								}
								break;
							}
							else
							{
								j++;
							}
						}
					}
					empireInfo.VictoryConditions = list.ToArray();
				}
			}
			else
			{
				Diagnostics.LogError("Unable to retrieve the database of victory conditions.");
			}
		}
		ILocalizationService service = Services.GetService<ILocalizationService>();
		empireInfo.LocalizedName = string.Empty;
		string[] array4 = lobbyData2.Split(Amplitude.String.Separators, StringSplitOptions.RemoveEmptyEntries);
		for (int k = 0; k < array4.Length; k++)
		{
			if (service != null)
			{
				if (array4[k].StartsWith("AI"))
				{
					if (empireInfo.Faction.Name == "FactionELCPSpectator")
					{
						empireInfo.LocalizedName = AgeLocalizer.Instance.LocalizeString("%NotificationEncounterParticipationModeSpectatorTitle");
					}
					else
					{
						empireInfo.LocalizedName = MajorEmpire.GenerateAIName(empireInfo.Faction.Affinity.Name, empireInfo.EmpireIndex);
					}
				}
				else
				{
					Steamworks.SteamID steamID = new Steamworks.SteamID(Convert.ToUInt64(array4[k], 16));
					string newValue = AgeLocalizer.Instance.LocalizeString("%DefaultPlayerName");
					if (Steamworks.SteamAPI.IsSteamRunning)
					{
						newValue = Steamworks.SteamAPI.SteamFriends.GetFriendPersonaName(steamID);
					}
					string name = (empireInfo.LocalizedName.Length != 0) ? "%EmpireNameFormatAdditionnalHuman" : "%EmpireNameFormatHuman";
					EmpireInfo empireInfo2 = empireInfo;
					empireInfo2.LocalizedName += service.Localize(name).ToString().Replace("$PlayerName", newValue);
				}
			}
		}
		empireInfo.IsActiveOrLocalPlayer = false;
		empireInfo.EmpireEliminated = false;
		empireInfo.EmpireExplorationBits = 0;
		empireInfo.EmpireInfiltrationBits = 0;
		IGameService service2 = Services.GetService<IGameService>();
		if (service2 != null && service2.Game != null)
		{
			IPlayerControllerRepositoryService service3 = service2.Game.Services.GetService<IPlayerControllerRepositoryService>();
			if (service3 != null && service3.ActivePlayerController != null && service3.ActivePlayerController.Empire != null)
			{
				empireInfo.IsActiveOrLocalPlayer = (service3.ActivePlayerController.Empire.Index == empireIndex);
			}
			else
			{
				Steamworks.SteamUser steamUser = Steamworks.SteamAPI.SteamUser;
				if (steamUser != null)
				{
					empireInfo.IsActiveOrLocalPlayer = empireInfo.Players.Contains(steamUser.SteamID.ToString());
				}
			}
			global::Game game = service2.Game as global::Game;
			if (game != null && game.Empires != null)
			{
				MajorEmpire majorEmpire = game.Empires[empireIndex] as MajorEmpire;
				if (majorEmpire.IsEliminated)
				{
					empireInfo.EmpireEliminated = true;
				}
				empireInfo.AlliedIndexList = new List<int>();
				for (int l = 0; l < game.Empires.Length; l++)
				{
					MajorEmpire majorEmpire2 = game.Empires[l] as MajorEmpire;
					if (majorEmpire2 == null)
					{
						break;
					}
					if (majorEmpire2.Index == empireIndex)
					{
						empireInfo.EmpireExplorationBits |= 1 << empireIndex;
						empireInfo.EmpireInfiltrationBits |= 1 << empireIndex;
					}
					else
					{
						DepartmentOfForeignAffairs agency = majorEmpire2.GetAgency<DepartmentOfForeignAffairs>();
						if (agency != null)
						{
							DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(majorEmpire);
							if (diplomaticRelation != null && diplomaticRelation.State != null && diplomaticRelation.State.Name != DiplomaticRelationState.Names.Unknown)
							{
								empireInfo.EmpireExplorationBits |= 1 << majorEmpire2.Index;
								if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.Alliance)
								{
									empireInfo.AlliedIndexList.Add(majorEmpire2.Index);
								}
							}
						}
						DepartmentOfIntelligence agency2 = majorEmpire2.GetAgency<DepartmentOfIntelligence>();
						if (agency2 != null && agency2.IsEmpireInfiltrated(majorEmpire))
						{
							empireInfo.EmpireInfiltrationBits |= 1 << majorEmpire2.Index;
						}
					}
				}
			}
		}
		return empireInfo;
	}

	public int CompareTo(EmpireInfo other)
	{
		if (this.VictoryConditions.Length == other.VictoryConditions.Length)
		{
			return this.EmpireIndex.CompareTo(other.EmpireIndex);
		}
		return other.VictoryConditions.Length.CompareTo(this.VictoryConditions.Length);
	}

	public int CompareTo(object obj)
	{
		return this.CompareTo((EmpireInfo)obj);
	}

	public static readonly StaticString EmpireInfoAccessibility = new StaticString("EmpireInfoAccessibility");

	public enum Accessibility
	{
		Default,
		None,
		Partial
	}
}
