using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Achievement;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Session;
using Amplitude.Utilities.Maps;
using UnityEngine;

public class AchievementManager : SteamAchievementManager
{
	public bool SteamStatsAndAchievementsLoaded { get; private set; }

	private Amplitude.Unity.Game.Empire ActiveEmpire
	{
		get
		{
			if (this.playerControllerRepositoryService != null && this.playerControllerRepositoryService.ActivePlayerController != null)
			{
				return this.playerControllerRepositoryService.ActivePlayerController.Empire;
			}
			return null;
		}
	}

	public override IEnumerator BindServices()
	{
		yield return base.BindServices();
		base.SetLastError(0, "Waiting for service dependencies...");
		yield return base.BindService<IGameService>(delegate(IGameService service)
		{
			this.gameService = service;
		});
		yield return base.BindService<ISessionService>(delegate(ISessionService service)
		{
			this.sessionService = service;
		});
		yield return base.BindService<IEventService>(delegate(IEventService service)
		{
			this.eventService = service;
		});
		yield return base.BindService<IAnalyticsService>(delegate(IAnalyticsService service)
		{
			this.analyticsService = service;
		});
		this.SteamStatsAndAchievementsLoaded = false;
		yield return base.BindService<IRuntimeService>(delegate(IRuntimeService service)
		{
			service.RuntimeChange += this.RuntimeService_RuntimeChange;
		});
		this.commandService = Services.GetService<ICommandService>();
		if (this.commandService != null && Amplitude.Unity.Framework.Application.Version.Accessibility <= Accessibility.Internal)
		{
			this.commandService.RegisterCommand(new Command("/Achievements", "Retrieves or sets achievement informations."), new Func<string[], string>(this.Command_Achievement));
		}
		this.gameService.CreateGameComplete += this.GameService_CreateGameComplete;
		this.gameService.GameChange += this.GameService_GameChange;
		this.eventService.EventRaise += this.EventService_EventRaise;
		yield break;
	}

	public uint GetQuestAchievementsCompletionBitfield()
	{
		uint num = 0u;
		Array values = Enum.GetValues(typeof(QuestAchievement));
		foreach (object obj in values)
		{
			QuestAchievement questAchievement = (QuestAchievement)((int)obj);
			if (this.GetAchievement(questAchievement.ToString()))
			{
				num |= 1u << (int)questAchievement;
			}
		}
		return num;
	}

	public override void Load()
	{
		if (this.SteamStatsAndAchievementsLoaded)
		{
			return;
		}
		base.Load();
		if (base.SteamStatsAndAchievementsRequested && base.SteamStatsAndAchievementsReceived)
		{
			this.SteamStatsAndAchievementsLoaded = true;
			if (this.eventHandlers == null)
			{
				this.RegisterEventHandlers();
			}
		}
		this.uniqueFacilitiesCount = 0;
		IDatabase<PointOfInterestTemplate> database = Databases.GetDatabase<PointOfInterestTemplate>(false);
		if (database != null)
		{
			foreach (PointOfInterestTemplate pointOfInterestTemplate in database)
			{
				string a;
				if (pointOfInterestTemplate.Properties.TryGetValue("IsUniqueFacility", out a) && a == "true")
				{
					this.uniqueFacilitiesCount++;
				}
			}
		}
	}

	protected override void Releasing()
	{
		if (this.commandService != null)
		{
			if (Amplitude.Unity.Framework.Application.Version.Accessibility <= Accessibility.Internal)
			{
				this.commandService.UnregisterCommand("/Achievements");
			}
			this.commandService = null;
		}
		if (this.gameService != null)
		{
			this.gameService.CreateGameComplete -= this.GameService_CreateGameComplete;
			this.gameService.GameChange -= this.GameService_GameChange;
		}
		if (this.playerControllerRepositoryService != null)
		{
			this.playerControllerRepositoryService.ActivePlayerControllerChange -= this.PlayerControllerRepositoryService_ActivePlayerControllerChange;
		}
		if (this.eventService != null)
		{
			this.eventService.EventRaise -= this.EventService_EventRaise;
		}
		if (this.tradeManagementService != null)
		{
			this.tradeManagementService.TransactionComplete -= this.TradeManagementService_TransactionComplete;
		}
		this.UnhookEmpireEvents();
		base.Releasing();
	}

	private void AchievementManager_OnChange(object sender, PlayerRepositoryChangeEventArgs e)
	{
		if (e.Player != null && e.Player.Type == PlayerType.Human && this.playerControllerRepositoryService != null && this.playerControllerRepositoryService.ActivePlayerController != null && e.Player.Empire != this.playerControllerRepositoryService.ActivePlayerController.Empire)
		{
			this.SetAchievement("MULTIPLAYER_GAME");
			this.gameService.Game.Services.GetService<IPlayerRepositoryService>().OnChange -= this.AchievementManager_OnChange;
		}
	}

	private bool ArmyContainsSeaMonster(IEnumerable<Contender> contenders)
	{
		foreach (Contender contender in contenders)
		{
			foreach (Unit unit in contender.Garrison.Units)
			{
				if (unit.UnitDesign.UnitBodyDefinition.Name == "UnitBodySeaMonsterMaw")
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool CheckRovingClansHeroKillerSetsekeAchievementCondition(Unit deathUnit, Unit killerUnit)
	{
		return !this.GetAchievement("ROVING_CLANS_HERO_KILLER_SETSEKE") && deathUnit != null && killerUnit != null && (!(this.ActiveEmpire as global::Empire).Faction.IsCustom && killerUnit.Garrison.Empire.Index == this.ActiveEmpire.Index && deathUnit.IsHero() && killerUnit.UnitDesign.Tags.Contains("Scarab"));
	}

	private bool WasKaijuLiceKilled(Unit deathUnit)
	{
		return !this.GetAchievement("THE_EXTERMINATOR") && deathUnit != null && (deathUnit.Garrison.Empire.Index != this.ActiveEmpire.Index && deathUnit.UnitDesign.Tags.Contains(Kaiju.LiceUnitTag));
	}

	private string Command_Achievement(string[] commandLineArgs)
	{
		if (commandLineArgs.Length > 1)
		{
			string text = commandLineArgs[1].ToLower();
			string text2 = text;
			switch (text2)
			{
			case "set":
			{
				if (commandLineArgs.Length <= 2)
				{
					return "Error: missing name or value";
				}
				StaticString name = commandLineArgs[2].ToUpper();
				if (commandLineArgs.Length == 3)
				{
					if (this.achievements.Contains(name))
					{
						this.SetAchievement(name);
						this.Commit();
						return "Achieved " + name;
					}
					return "Unknown achievement: " + name;
				}
				else if (commandLineArgs.Length > 3)
				{
					AchievementStatistic achievementStatistic = this.statistics.FirstOrDefault((AchievementStatistic s) => s.Name == name);
					if (achievementStatistic == null)
					{
						return "Unknown statistic: " + name;
					}
					float num2;
					if (float.TryParse(commandLineArgs[3], out num2))
					{
						this.SetStatisticValue(name, num2, true);
						return string.Concat(new object[]
						{
							"Statistic ",
							name,
							" set to ",
							num2
						});
					}
					return "Third argument must be a proper float or int value";
				}
				break;
			}
			case "get":
			{
				if (commandLineArgs.Length <= 2)
				{
					return "Error: missing name";
				}
				StaticString name = commandLineArgs[2].ToUpper();
				if (this.achievements.Contains(name))
				{
					if (this.GetAchievement(name))
					{
						return name + " is achieved";
					}
					return name + " is not achieved";
				}
				else
				{
					AchievementStatistic achievementStatistic2 = this.statistics.FirstOrDefault((AchievementStatistic s) => s.Name == name);
					if (achievementStatistic2 != null)
					{
						return string.Concat(new object[]
						{
							"Statistic ",
							name,
							" = ",
							achievementStatistic2.Value
						});
					}
					return "Unknown achievement or statistic: " + name;
				}
				break;
			}
			case "reset":
				this.ResetAllStatistics();
				return "Resetted all statistics and achievements";
			case "commit":
				this.Commit();
				return "Commited all statistics and achievements";
			case "achievementlist":
			{
				StaticString x = string.Empty;
				List<StaticString> list = new List<StaticString>(this.achievements);
				list.Sort();
				foreach (StaticString staticString in list)
				{
					x = string.Concat(new object[]
					{
						x,
						"\n",
						staticString,
						" = ",
						this.GetAchievement(staticString)
					});
				}
				x = string.Concat(new object[]
				{
					x,
					"\n\n",
					list.Count,
					" achievements found"
				});
				return x;
			}
			case "statisticlist":
			{
				StaticString x2 = string.Empty;
				List<AchievementStatistic> list2 = new List<AchievementStatistic>(this.statistics);
				list2 = (from statistic in list2
				orderby statistic.Name
				select statistic).ToList<AchievementStatistic>();
				foreach (AchievementStatistic achievementStatistic3 in list2)
				{
					x2 = string.Concat(new object[]
					{
						x2,
						"\n",
						achievementStatistic3.Name,
						" = ",
						achievementStatistic3.Value
					});
				}
				x2 = string.Concat(new object[]
				{
					x2,
					"\n\n",
					list2.Count,
					" statistics found"
				});
				return x2;
			}
			}
		}
		return "Possible arguments are: set, get, reset, commit, achievementList, statisticList";
	}

	private void DepartmentOfTheInterior_CitiesCollectionChanged(object sender, CollectionChangeEventArgs e)
	{
		if ((this.gameService.Game as global::Game).Turn < 15)
		{
			this.SetStatisticValue("CITIES_COUNT_TURN_15", (float)this.departmentOfTheInterior.Cities.Count, false);
		}
		if (!this.GetAchievement("WELCOME_TO_THE_FOLD"))
		{
			int num = 0;
			for (int i = 0; i < this.departmentOfTheInterior.Cities.Count; i++)
			{
				if (this.departmentOfTheInterior.Cities[i].IsInfected)
				{
					num++;
				}
			}
			this.SetStatisticValue("OVERGROWN_CITIES_COUNT", (float)num, false);
		}
	}

	private void DepartmentOfTransportation_ArmyTeleportedToCity(object sender, ArmyTeleportedToCityEventArgs e)
	{
		if (e.Army != null && e.Army.Empire == this.ActiveEmpire && e.City != null && e.City.BesiegingEmpire != null)
		{
			if (this.unitsTeleportedThisTurn.ContainsKey(e.City))
			{
				Dictionary<City, int> dictionary2;
				Dictionary<City, int> dictionary = dictionary2 = this.unitsTeleportedThisTurn;
				City city;
				City key = city = e.City;
				int num = dictionary2[city];
				dictionary[key] = num + e.Army.UnitsCount;
			}
			else
			{
				this.unitsTeleportedThisTurn.Add(e.City, e.Army.UnitsCount);
			}
		}
	}

	private void DepartmentOfTheTreasury_ResourcePropertyChange(object sender, ResourcePropertyChangeEventArgs e)
	{
		if (e.Amount > 0f && e.ResourceDefinition.Name == DepartmentOfTheTreasury.Resources.EmpireMoney)
		{
			this.AddToStatistic("DUST_GAINED", e.Amount, false);
		}
	}

	private void EventService_EventRaise(object sender, EventRaiseEventArgs e)
	{
		AchievementManager.AchievementEventHandler achievementEventHandler;
		if (this.eventHandlers != null && e.RaisedEvent != null && this.eventHandlers.TryGetValue(e.RaisedEvent.EventName, out achievementEventHandler))
		{
			achievementEventHandler(e.RaisedEvent);
		}
	}

	private void GameService_CreateGameComplete(object sender, CreateGameCompleteEventArgs e)
	{
		if (e.Cancelled)
		{
			return;
		}
		if (e.Exception != null)
		{
			return;
		}
		this.Load();
		if (!this.SteamStatsAndAchievementsLoaded)
		{
			Diagnostics.LogWarning("[Achievement] Steam user statistics and achievements haven't been loaded yet.");
		}
		Diagnostics.Assert(this.gameService != null);
		Diagnostics.Assert(this.gameService.Game != null);
		Diagnostics.Assert(this.gameService.Game is global::Game);
		this.game = (this.gameService.Game as global::Game);
		IPillarService service = this.game.Services.GetService<IPillarService>();
		Diagnostics.Assert(service != null);
		this.pillarManager = (service as PillarManager);
		Diagnostics.Assert(this.pillarManager != null);
		this.playerControllerRepositoryService = this.game.Services.GetService<IPlayerControllerRepositoryService>();
		Diagnostics.Assert(this.playerControllerRepositoryService != null);
		this.playerControllerRepositoryService.ActivePlayerControllerChange += this.PlayerControllerRepositoryService_ActivePlayerControllerChange;
		if (!this.GetAchievement("ROVING_CLANS_IRS"))
		{
			this.tradeManagementService = this.gameService.Game.Services.GetService<ITradeManagementService>();
			Diagnostics.Assert(this.tradeManagementService != null);
			this.tradeManagementService.TransactionComplete += this.TradeManagementService_TransactionComplete;
		}
		this.seasonService = this.gameService.Game.Services.GetService<ISeasonService>();
		Diagnostics.Assert(this.seasonService != null);
		if (!this.GetAchievement("MULTIPLE_CITY_CONQUERED_WINTER"))
		{
			this.seasonService.SeasonChange += this.SeasonService_SeasonChange;
		}
		if (this.game.World != null && this.game.World.Atlas != null)
		{
			IMap map = this.game.World.Atlas.GetMap(WorldAtlas.Maps.Exploration);
			if (map != null)
			{
				this.explorationMap = (map as GridMap<short>);
			}
		}
		IDatabase<GameModifierDefinition> database = Databases.GetDatabase<GameModifierDefinition>(false);
		Diagnostics.Assert(database != null);
		this.orderedDifficulties = (from gm in database.GetValues()
		where gm.Name.ToString().StartsWith("GameDifficulty")
		select gm.Name.ToString().Replace("GameDifficulty", string.Empty)).ToList<string>();
		this.majorFactionNames = (from faction in Databases.GetDatabase<Faction>(false).GetValues()
		where !(faction is MinorFaction) && faction.IsStandard && faction.Affinity != null && faction.AffinityMapping != null && faction.Name != "FactionMezari"
		select faction into majorFaction
		select majorFaction.Name.ToString().ToUpper()).ToArray<string>();
		this.strategicResourceNames = (from resource in Databases.GetDatabase<ResourceDefinition>(false).GetValues()
		where resource.ResourceType == ResourceDefinition.Type.Strategic
		select resource into stratResource
		select stratResource.Name).ToArray<StaticString>();
		this.luxuryBoosterNames = (from booster in Databases.GetDatabase<BoosterDefinition>(false).GetValues()
		select booster.Name.ToString().ToUpper() into name
		where name.Contains("LUXURY")
		select name).ToArray<string>();
		if (this.sessionService != null && this.sessionService.Session != null && this.sessionService.Session.SessionMode != SessionMode.Single && !this.GetAchievement("MULTIPLAYER_GAME"))
		{
			this.gameService.Game.Services.GetService<IPlayerRepositoryService>().OnChange += this.AchievementManager_OnChange;
		}
		if (this.game.Turn == 0)
		{
			this.IncrementStatistic("GAMES_PLAYED", false);
			float statisticValue = this.GetStatisticValue("GAMES_PLAYED");
			if (statisticValue > 0f)
			{
				this.analyticsService.SendNewUserMetrics(statisticValue == 1f);
			}
		}
		this.alreadyWon = false;
		this.fisBoosterProduced = new bool[3];
		this.empirePlansLevels = new int[4];
		this.unitsTeleportedThisTurn = new Dictionary<City, int>();
		this.encounters = new Dictionary<Encounter, List<KeyValuePair<EncounterUnit, float>>>();
		this.startedOnIceEncounterGUIDs = new List<GameEntityGUID>();
	}

	private void RuntimeService_RuntimeChange(object sender, RuntimeChangeEventArgs e)
	{
		if (e.Action == RuntimeChangeAction.Loading)
		{
			return;
		}
		if (base.IsDisabled)
		{
			Diagnostics.Log("Steam achievements restored.");
			base.IsDisabled = false;
		}
		if (e.Action != RuntimeChangeAction.Loaded)
		{
			return;
		}
		this.Load();
		if (base.IsDisabled)
		{
			return;
		}
		if (e.Action == RuntimeChangeAction.Loaded && e.Runtime != null && e.Runtime.Configuration != null)
		{
			RuntimeModule runtimeModule = e.Runtime.RuntimeModules.FirstOrDefault((RuntimeModule module) => module.Type == RuntimeModuleType.Standalone);
			RuntimeModule runtimeModule2 = e.Runtime.RuntimeModules.FirstOrDefault((RuntimeModule module) => module.Type == RuntimeModuleType.Conversion);
			RuntimeModule runtimeModule3 = e.Runtime.RuntimeModules.FirstOrDefault((RuntimeModule module) => module.Type == RuntimeModuleType.Extension);
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
				runtimeModule4 = runtimeModule;
				IRuntimeService service = Services.GetService<IRuntimeService>();
				if (service != null && service.VanillaModuleName == runtimeModule.Name)
				{
					runtimeModule4 = null;
				}
			}
			if (runtimeModule4 != null)
			{
				base.IsDisabled = false;
			}
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools && !Amplitude.Unity.Framework.Application.Preferences.ELCPDevMode)
			{
				Diagnostics.LogWarning("The network achievement manager has been disabled because the modding tools are enabled...");
				base.IsDisabled = true;
			}
		}
	}

	private void SeasonService_SeasonChange(object sender, SeasonChangeEventArgs e)
	{
		if (e.OldSeason != null)
		{
			this.departmentOfPlanificationAndDevelopment.StatCityConqueredCurrentWinter = 0;
		}
	}

	private void TradeManagementService_TransactionComplete(object sender, TradableTransactionCompleteEventArgs e)
	{
		if ((ulong)e.Transaction.EmpireIndex == (ulong)((long)this.ActiveEmpire.Index) || this.activeFactionName != "FACTIONROVINGCLANS")
		{
			return;
		}
		this.AddToStatistic("ROVING_CLANS_MARKETPLACE_TAXES", this.departmentOfTheTreasury.GetRovingClansTollFee(e.Transaction), false);
	}

	private void GameService_GameChange(object sender, GameChangeEventArgs e)
	{
		if (e.Action == GameChangeAction.Releasing && this.gameService != null && this.gameService.Game != null && this.gameService.Game.Services != null)
		{
			this.gameService.Game.Services.GetService<IPlayerRepositoryService>().OnChange -= this.AchievementManager_OnChange;
		}
	}

	private List<EncounterUnit> GetUnitsFromContenders(IEnumerable<Contender> contenders)
	{
		List<EncounterUnit> list = new List<EncounterUnit>();
		foreach (Contender contender in contenders)
		{
			list.AddRange(contender.EncounterUnits);
		}
		return list.Distinct<EncounterUnit>().ToList<EncounterUnit>();
	}

	private bool IsDistrictWonder(District district)
	{
		return district.Type == DistrictType.Extension && district.SimulationObject.Tags.Contains(DistrictImprovementDefinition.ReadOnlyWonderClass);
	}

	private bool IsArmyComposedOfEachUnitFomorians(IEnumerable<Contender> contenders)
	{
		int num = 0;
		foreach (Contender contender in contenders)
		{
			foreach (Unit unit in contender.Garrison.Units)
			{
				string text = unit.UnitDesign.UnitBodyDefinition.Name;
				switch (text)
				{
				case "UnitBodyFomoriansBoardingVessel":
					num |= 1;
					break;
				case "UnitBodyFomoriansFireShip":
					num |= 2;
					break;
				case "UnitBodyFomoriansArtilleryShip":
					num |= 4;
					break;
				case "UnitBodyFomoriansBathysphere":
					num |= 8;
					break;
				}
			}
		}
		return num == 15;
	}

	private bool IsArmyComposedOfEachUnitShiftedForms(IEnumerable<Contender> contenders)
	{
		int num = 0;
		foreach (Contender contender in contenders)
		{
			foreach (Unit unit in contender.Garrison.Units)
			{
				float propertyValue = unit.GetPropertyValue("ShiftingForm");
				if (unit.UnitDesign.UnitBodyDefinition.Name == "UnitBodyWinterShiftersFeline")
				{
					num |= ((propertyValue != 0f) ? 2 : 1);
				}
				if (unit.UnitDesign.UnitBodyDefinition.Name == "UnitBodyWinterShiftersManta")
				{
					num |= ((propertyValue != 0f) ? 8 : 4);
				}
				if (unit.UnitDesign.UnitBodyDefinition.Name == "UnitBodyWinterShiftersDarkAngel")
				{
					num |= ((propertyValue != 0f) ? 32 : 16);
				}
			}
		}
		return num == 63;
	}

	private bool IsArmyOnlyComposedOfMinorUnits(List<KeyValuePair<EncounterUnit, float>> units)
	{
		for (int i = 0; i < units.Count; i++)
		{
			if (!units[i].Key.Unit.SimulationObject.Tags.Contains("UnitFactionTypeMinorFaction"))
			{
				return false;
			}
		}
		return true;
	}

	private bool IsArmyOnlyComposedOfLandUnits(IEnumerable<Contender> contenders)
	{
		foreach (Contender contender in contenders)
		{
			foreach (Unit unit in contender.Garrison.Units)
			{
				if (unit.UnitDesign.Tags.Contains(UnitDesign.TagSeafaring))
				{
					return false;
				}
			}
		}
		return true;
	}

	private bool IsArmyOnlyComposedOfSubmersibleUnits(IEnumerable<Contender> contenders)
	{
		foreach (Contender contender in contenders)
		{
			foreach (Unit unit in contender.Garrison.Units)
			{
				if (!unit.CheckUnitAbility(UnitAbility.ReadonlySubmersible, -1))
				{
					return false;
				}
			}
		}
		return true;
	}

	private bool IsRegionPartOfAnOceanicHub(Region region, global::Empire empire)
	{
		DepartmentOfTheInterior agency = empire.GetAgency<DepartmentOfTheInterior>();
		IWorldPositionningService service = this.gameService.Game.Services.GetService<IWorldPositionningService>();
		if (agency != null && service != null)
		{
			foreach (Region.Border border in region.Borders)
			{
				Region region2 = service.GetRegion(border.NeighbourRegionIndex);
				if (region2.IsOcean && agency.BorderingRegionsCount(region2, true, true) >= 4)
				{
					return true;
				}
			}
		}
		return false;
	}

	private void OnEventArmyDrowned(Amplitude.Unity.Event.Event eventRaised)
	{
		EventArmyDrowned eventArmyDrowned = eventRaised as EventArmyDrowned;
		if (this.ActiveEmpire.Index == eventArmyDrowned.Empire.Index)
		{
			this.SetAchievement("DROWN_ON_ICE");
		}
	}

	private void OnEventAspirate(Amplitude.Unity.Event.Event eventRaised)
	{
		EventAspirate eventAspirate = eventRaised as EventAspirate;
		if (eventAspirate.Empire.Index == this.ActiveEmpire.Index && eventAspirate.PointOfInterest != null && eventAspirate.PointOfInterest.Region != null && eventAspirate.PointOfInterest.Region.City != null && eventAspirate.PointOfInterest.Region.City.Empire.Index != this.ActiveEmpire.Index)
		{
			this.AddToStatistic("MANTA_EXTRACTION_OVERALL_COUNT", eventAspirate.AmountAspirated, false);
		}
	}

	private void OnEventBattlebornCreated(Amplitude.Unity.Event.Event eventRaised)
	{
		if (!(eventRaised is EventBattlebornCreated))
		{
			return;
		}
		EventBattlebornCreated eventBattlebornCreated = eventRaised as EventBattlebornCreated;
		if (eventBattlebornCreated.Empire.Index == this.ActiveEmpire.Index && !(this.ActiveEmpire as global::Empire).Faction.IsCustom)
		{
			this.AddToStatistic("NECROPHAGES_BATTLEBORNS_CREATED_COUNT", 1f, false);
		}
	}

	private void OnEventBoosterActivated(Amplitude.Unity.Event.Event eventRaised)
	{
		EventBoosterActivated eventBoosterActivated = eventRaised as EventBoosterActivated;
		if (eventBoosterActivated.Empire == this.ActiveEmpire)
		{
			string text = eventBoosterActivated.Booster.BoosterDefinition.Name.ToString().ToUpper();
			if (text == "BOOSTERCADAVERS" && this.activeFactionName == "FACTIONNECROPHAGES")
			{
				this.SetStatisticValue("NECROPHAGES_CADAVER_BOOSTER_USED_PER_GAME", (float)(++this.departmentOfPlanificationAndDevelopment.StatNecrophagesCadaverBoosterUsed), false);
			}
			else if (text.Contains("LUXURY"))
			{
				this.IncrementStatistic(text + "_USED", false);
				this.SetStatisticValue("LUXURY_BOOSTER_USED_COUNT", (float)this.luxuryBoosterNames.Count((string name) => this.GetStatisticValue(name + "_USED") > 0f), false);
				this.SetStatisticValue("LUXURY_BOOSTERS_ACTIVE", (float)this.departmentOfPlanificationAndDevelopment.Boosters.Values.Count((Booster booster) => booster.IsActive() && booster.BoosterDefinition.Name.ToString().ToUpper().Contains("LUXURY")), false);
			}
			else
			{
				if (text == "BOOSTERFOOD")
				{
					this.fisBoosterProduced[0] = true;
				}
				else if (text == "BOOSTERINDUSTRY")
				{
					this.fisBoosterProduced[1] = true;
				}
				else if (text == "BOOSTERSCIENCE")
				{
					this.fisBoosterProduced[2] = true;
				}
				if (this.fisBoosterProduced.All((bool boosterProduced) => boosterProduced))
				{
					this.SetAchievement("ALL_FIS_BOOSTER_USED");
				}
			}
		}
	}

	private void OnEventAutomaticBattleUnitDeath(Amplitude.Unity.Event.Event eventRaised)
	{
		if (!(eventRaised is EventAutomaticBattleUnitDeath))
		{
			return;
		}
		EventAutomaticBattleUnitDeath eventAutomaticBattleUnitDeath = eventRaised as EventAutomaticBattleUnitDeath;
		if (eventAutomaticBattleUnitDeath.KillerUnitGameEntityGUID != GameEntityGUID.Zero)
		{
			Unit deathUnit;
			this.departmentOfDefense.GameEntityRepositoryService.TryGetValue<Unit>(eventAutomaticBattleUnitDeath.DeathUnitGameEntityGUID, out deathUnit);
			Unit killerUnit;
			this.departmentOfDefense.GameEntityRepositoryService.TryGetValue<Unit>(eventAutomaticBattleUnitDeath.KillerUnitGameEntityGUID, out killerUnit);
			if (this.CheckRovingClansHeroKillerSetsekeAchievementCondition(deathUnit, killerUnit))
			{
				this.IncrementStatistic("ROVING_CLANS_SETSEKE_HERO_KILLS_COUNT", false);
			}
			if (this.WasKaijuLiceKilled(deathUnit))
			{
				this.AddToStatistic("LICE_KILLED_OVERALL_COUNT", 1f, false);
			}
		}
	}

	private void OnEventBeginTurn(Amplitude.Unity.Event.Event eventRaised)
	{
		if (this.game.Turn == 24)
		{
			int num = this.orderedDifficulties.IndexOf(this.game.GameDifficulty);
			if (num >= 3)
			{
				Dictionary<MajorEmpire, float> dictionary = new Dictionary<MajorEmpire, float>();
				foreach (global::Empire empire in this.game.Empires)
				{
					MajorEmpire majorEmpire = empire as MajorEmpire;
					GameScore gameScore = null;
					if (majorEmpire != null && majorEmpire.GameScores.TryGetValue(GameScores.Names.GlobalScore, out gameScore))
					{
						dictionary.Add(majorEmpire, gameScore.Value);
					}
				}
				if ((from empireScore in dictionary
				orderby empireScore.Value descending
				select empireScore).ElementAt(0).Key == this.ActiveEmpire as MajorEmpire)
				{
					this.SetAchievement("BEST_SCORE_EARLY_DIFFICULTY3");
					if (num >= 6)
					{
						this.SetAchievement("BEST_SCORE_EARLY_DIFFICULTY6");
					}
				}
			}
		}
		if (this.activeFactionName == "FACTIONDRAKKENS")
		{
			int num2 = 0;
			City city;
			foreach (City city2 in this.departmentOfTheInterior.Cities)
			{
				city = city2;
				if (city != null && city.Region != null && city.Region.PointOfInterests != null && city.Districts != null)
				{
					num2 += city.Districts.Count((District district) => city.Region.PointOfInterests.Any((PointOfInterest poi) => poi.Type == "QuestLocation" && district.WorldPosition == poi.WorldPosition));
				}
			}
			if (num2 > 0)
			{
				this.SetStatisticValue("DRAKKEN_EXPLOITED_RUINS_COUNT", (float)num2, false);
			}
		}
		List<StaticString> list = new List<StaticString>();
		for (int j = 0; j < this.departmentOfDefense.Armies.Count; j++)
		{
			foreach (Unit unit in this.departmentOfDefense.Armies[j].Units)
			{
				if (unit.SimulationObject.Tags.Contains(Unit.ReadOnlyColossus) && !list.Contains(unit.UnitDesign.Name))
				{
					list.Add(unit.UnitDesign.Name);
				}
			}
		}
		this.SetStatisticValue("DIFFERENT_COLOSSI", (float)list.Count, false);
		if (!this.GetAchievement("BEST_EMPIRE_PLAN"))
		{
			this.empirePlansLevels[0] = (int)this.ActiveEmpire.GetPropertyValue("EmpirePlanEconomyLevel");
			this.empirePlansLevels[1] = (int)this.ActiveEmpire.GetPropertyValue("EmpirePlanForeignAffairsLevel");
			this.empirePlansLevels[2] = (int)this.ActiveEmpire.GetPropertyValue("EmpirePlanKnowledgeLevel");
			this.empirePlansLevels[3] = (int)this.ActiveEmpire.GetPropertyValue("EmpirePlanMilitaryLevel");
			bool flag = true;
			for (int k = 0; k < this.empirePlansLevels.Length; k++)
			{
				if (this.empirePlansLevels[k] < 4)
				{
					flag = false;
				}
			}
			if (flag)
			{
				this.SetAchievement("BEST_EMPIRE_PLAN");
			}
		}
		this.unitsTeleportedThisTurn.Clear();
	}

	private void OnEventBlackSpotUsed(Amplitude.Unity.Event.Event eventRaised)
	{
		EventBlackSpotUsed eventBlackSpotUsed = eventRaised as EventBlackSpotUsed;
		if (eventBlackSpotUsed != null && eventBlackSpotUsed.BlackSpotInitiator != null && eventBlackSpotUsed.BlackSpotInitiator.Index == this.ActiveEmpire.Index && this.activeFactionName == "FACTIONSEADEMONS")
		{
			this.SetAchievement("BLACK_SPOT_USED");
		}
	}

	private void OnEventCastSeasonVote(Amplitude.Unity.Event.Event eventRaised)
	{
		EventCastSeasonVote eventCastSeasonVote = eventRaised as EventCastSeasonVote;
		if (eventCastSeasonVote.Empire.Index == this.ActiveEmpire.Index)
		{
			this.IncrementStatistic("CAST_VOTES_OVERALL_COUNT", false);
		}
	}

	private void OnEventCityCaptured(Amplitude.Unity.Event.Event eventRaised)
	{
		EventCityCaptured eventCityCaptured = eventRaised as EventCityCaptured;
		if (eventCityCaptured.Conqueror.Index == this.ActiveEmpire.Index)
		{
			if (this.seasonService.GetCurrentSeason().SeasonDefinition.SeasonType == Season.ReadOnlyWinter)
			{
				this.SetStatisticValue("CURRENT_WINTER_CITY_CONQUERED", (float)(++this.departmentOfPlanificationAndDevelopment.StatCityConqueredCurrentWinter), false);
			}
			int num = this.departmentOfTheInterior.Cities.Sum((City city) => city.Districts.Count((District district) => district.Type == DistrictType.Extension && district.SimulationObject.Tags.Contains(DistrictImprovementDefinition.ReadOnlyWonderClass)));
			int num2 = eventCityCaptured.City.Districts.Count((District district) => district.Type == DistrictType.Extension && district.SimulationObject.Tags.Contains(DistrictImprovementDefinition.ReadOnlyWonderClass));
			this.SetStatisticValue("DIFFERENT_WONDERS", (float)(num + num2), false);
			if (num2 > 0)
			{
				this.SetAchievement("CAPTURE_WONDER");
			}
			if ((eventCityCaptured.Conqueror as MajorEmpire).Faction.Name.ToString().ToUpper() == "FACTIONSEADEMONS" && this.IsRegionPartOfAnOceanicHub(eventCityCaptured.City.Region, eventCityCaptured.Empire as global::Empire))
			{
				this.SetAchievement("SEA_DEMONS_OCEANIC_HUB");
			}
		}
	}

	private void OnEventCityRazed(Amplitude.Unity.Event.Event eventRaised)
	{
		EventCityRazed eventCityRazed = eventRaised as EventCityRazed;
		if (eventCityRazed.Destroyer.Index == this.ActiveEmpire.Index && eventCityRazed.Empire.Index != this.ActiveEmpire.Index && this.seasonService.GetCurrentSeason().SeasonDefinition.SeasonType == Season.ReadOnlyWinter)
		{
			this.SetStatisticValue("CURRENT_WINTER_CITY_CONQUERED", (float)(++this.departmentOfPlanificationAndDevelopment.StatCityConqueredCurrentWinter), false);
		}
	}

	private void OnEventConstructionEnded(Amplitude.Unity.Event.Event eventRaised)
	{
		EventConstructionEnded eventConstructionEnded = eventRaised as EventConstructionEnded;
		if (eventConstructionEnded.Empire.Index == this.ActiveEmpire.Index)
		{
			this.departmentOfPlanificationAndDevelopment.StatConstructionBuilt++;
			if (this.departmentOfTheInterior != null && eventConstructionEnded.ConstructibleElement.SubCategory == DistrictImprovementDefinition.WonderSubCategory)
			{
				int num = 1 + this.departmentOfTheInterior.Cities.Sum((City city) => city.Districts.Count((District district) => this.IsDistrictWonder(district)));
				this.SetStatisticValue("DIFFERENT_WONDERS", (float)num, false);
			}
		}
	}

	private void OnEventBattleDamageDone(Amplitude.Unity.Event.Event eventRaised)
	{
		if (!(eventRaised is EventBattleDamageDone))
		{
			return;
		}
		EventBattleDamageDone eventBattleDamageDone = eventRaised as EventBattleDamageDone;
		if (!this.GetAchievement("RAGE_WIZARDS_CRITICAL_DAMAGE") && eventBattleDamageDone.Empire.Index == this.ActiveEmpire.Index && this.activeFactionName == "FACTIONRAGEWIZARDS" && !(this.ActiveEmpire as global::Empire).Faction.IsCustom)
		{
			this.SetStatisticValue("RAGE_WIZARDS_CRITICAL_DAMAGE_DONE", eventBattleDamageDone.Damage, false);
		}
	}

	private void OnEventAutomaticBattleUnitSimulationApplyBattleEffect(Amplitude.Unity.Event.Event eventRaised)
	{
		if (!(eventRaised is EventAutomaticBattleUnitSimulationApplyBattleEffect))
		{
			return;
		}
		if (!this.GetAchievement("ALL_GEOMANCY_BUFFS"))
		{
			EventAutomaticBattleUnitSimulationApplyBattleEffect eventAutomaticBattleUnitSimulationApplyBattleEffect = eventRaised as EventAutomaticBattleUnitSimulationApplyBattleEffect;
			BattleSimulationUnit unit = eventAutomaticBattleUnitSimulationApplyBattleEffect.BattleEffectContext.Target.Unit;
			if (this.ActiveEmpire.Index == unit.Unit.Garrison.Empire.Index && unit.SimulationObject.Tags.Contains("UnitActionForestGeomancyInitUp") && unit.SimulationObject.Tags.Contains("UnitActionTerrainDefUp") && unit.SimulationObject.Tags.Contains("UnitActionVolcanicGeomancyAttUp"))
			{
				this.SetAchievement("ALL_GEOMANCY_BUFFS");
			}
		}
	}

	private void OnEventDiplomaticRelationStateChange(Amplitude.Unity.Event.Event eventRaised)
	{
		EventDiplomaticRelationStateChange eventDiplomaticRelationStateChange = eventRaised as EventDiplomaticRelationStateChange;
		if (eventDiplomaticRelationStateChange.EmpireWithWhichTheStatusChange.Index == this.ActiveEmpire.Index)
		{
			if (eventDiplomaticRelationStateChange.DiplomaticRelationStateName == DiplomaticRelationState.Names.War)
			{
				this.departmentOfPlanificationAndDevelopment.StatWarDeclarations++;
			}
			else if (eventDiplomaticRelationStateChange.DiplomaticRelationStateName == DiplomaticRelationState.Names.Alliance)
			{
				this.departmentOfPlanificationAndDevelopment.StatAllianceDeclarations++;
			}
			else if (eventDiplomaticRelationStateChange.DiplomaticRelationStateName == DiplomaticRelationState.Names.Peace)
			{
				this.departmentOfPlanificationAndDevelopment.StatPeaceDeclarations++;
				if (!this.GetAchievement("DRAKKENS_INSTANT_WORLD_PEACE") && this.activeFactionName == "FACTIONDRAKKENS" && !(this.ActiveEmpire as global::Empire).Faction.IsCustom)
				{
					this.AddToStatistic("DRAKKENS_EMPIRE_PEACE_RESTORED_COUNT", 1f, false);
				}
			}
		}
	}

	private void OnEventDistrictLevelUp(Amplitude.Unity.Event.Event eventRaised)
	{
		EventDistrictLevelUp eventDistrictLevelUp = eventRaised as EventDistrictLevelUp;
		if (eventDistrictLevelUp.Empire == this.ActiveEmpire && eventDistrictLevelUp.City != null && eventDistrictLevelUp.City.Districts != null)
		{
			int num = (int)this.GetStatisticValue("MAX_CITY_LEVELED_BOROUGHS_COUNT");
			int num2 = eventDistrictLevelUp.City.Districts.Count((District district) => district.GetPropertyValue(SimulationProperties.Level) > 0f);
			if (num2 > num)
			{
				this.SetStatisticValue("MAX_CITY_LEVELED_BOROUGHS_COUNT", (float)num2, false);
			}
		}
	}

	private void OnEventDustConsumptionHPLost(Amplitude.Unity.Event.Event eventRaised)
	{
		if (!(eventRaised is EventDustConsumptionHPLost))
		{
			return;
		}
		EventDustConsumptionHPLost eventDustConsumptionHPLost = eventRaised as EventDustConsumptionHPLost;
		if (eventDustConsumptionHPLost.Empire.Index != this.ActiveEmpire.Index || (this.ActiveEmpire as global::Empire).Faction.IsCustom)
		{
			return;
		}
		this.AddToStatistic("BROKEN_LORDS_DUST_CONSUMER_OVERALL_COUNT", eventDustConsumptionHPLost.HPLost, false);
	}

	private void OnEventDustStormAmbush(Amplitude.Unity.Event.Event eventRaised)
	{
		if (!(eventRaised is EventDustStormAmbush))
		{
			return;
		}
		EventDustStormAmbush eventDustStormAmbush = eventRaised as EventDustStormAmbush;
		if (eventDustStormAmbush.Empire.Index == this.ActiveEmpire.Index)
		{
			this.SetAchievement("DUST_STORM_AMBUSH");
		}
	}

	private void OnEventEncounterStateChange(Amplitude.Unity.Event.Event eventRaised)
	{
		EventEncounterStateChange eventEncounterStateChange = eventRaised as EventEncounterStateChange;
		if (eventEncounterStateChange.Empire == this.ActiveEmpire && eventEncounterStateChange.EventArgs != null)
		{
			global::Empire empire = this.ActiveEmpire as global::Empire;
			if (eventEncounterStateChange.EventArgs.EncounterState == EncounterState.Setup)
			{
				IWorldPositionningService service = this.gameService.Game.Services.GetService<IWorldPositionningService>();
				if (service == null)
				{
					return;
				}
				for (int i = 0; i < eventEncounterStateChange.EventArgs.Encounter.Contenders.Count; i++)
				{
					if (eventEncounterStateChange.EventArgs.Encounter.Contenders[i].Empire == this.ActiveEmpire && eventEncounterStateChange.EventArgs.Encounter.Contenders[i].IsAttacking && service.IsFrozenWaterTile(eventEncounterStateChange.EventArgs.Encounter.Contenders[i].WorldPosition))
					{
						this.startedOnIceEncounterGUIDs.Add(eventEncounterStateChange.EventArgs.Encounter.GUID);
					}
				}
			}
			else if (eventEncounterStateChange.EventArgs.EncounterState == EncounterState.BattleHasEnded)
			{
				if (this.startedOnIceEncounterGUIDs != null && this.startedOnIceEncounterGUIDs.Contains(eventEncounterStateChange.EventArgs.Encounter.GUID))
				{
					if (eventEncounterStateChange.EventArgs.Encounter.GetEncounterResultForEmpire(empire) == EncounterResult.Victory)
					{
						this.SetAchievement("FIGHT_ON_ICE");
					}
					this.startedOnIceEncounterGUIDs.Remove(eventEncounterStateChange.EventArgs.Encounter.GUID);
				}
				IEnumerable<Contender> enumerable = eventEncounterStateChange.EventArgs.Encounter.GetEnemiesContenderFromEmpire(empire);
				if (enumerable != null)
				{
					enumerable = from contender in enumerable
					where contender.IsTakingPartInBattle
					select contender;
					List<EncounterUnit> list = new List<EncounterUnit>();
					foreach (Contender contender2 in enumerable)
					{
						if (contender2.EncounterUnits != null)
						{
							list.AddRange(from encounterUnit in contender2.EncounterUnits
							where encounterUnit.GetPropertyValue(SimulationProperties.Health) <= float.Epsilon
							select encounterUnit);
						}
					}
					this.AddToStatistic("UNITS_KILLED", (float)list.Count, false);
					if (list.Any((EncounterUnit deadUnit) => deadUnit.Unit.UnitDesign.Tags.Contains(UnitDesign.TagMigrationUnit)))
					{
						this.SetAchievement("KILLED_MIGRATION_UNIT");
					}
					if (list.Any((EncounterUnit deadUnit) => deadUnit.Unit.UnitDesign.Tags.Contains(DownloadableContent9.TagColossus)))
					{
						this.SetAchievement("COLOSSI_KILLER");
					}
					if (eventEncounterStateChange.EventArgs.Encounter.GetEncounterResultForEmpire(empire) == EncounterResult.Victory)
					{
						IEnumerable<Contender> enumerable2 = eventEncounterStateChange.EventArgs.Encounter.GetAlliedContendersFromEmpire(empire);
						List<KeyValuePair<EncounterUnit, float>> units;
						if (!empire.Faction.IsCustom && empire.SimulationObject.Tags.Contains("FactionTraitCultistsHeatWave") && this.encounters.TryGetValue(eventEncounterStateChange.EventArgs.Encounter, out units))
						{
							if (this.IsArmyOnlyComposedOfMinorUnits(units))
							{
								this.IncrementStatistic("CULTISTS_MINOR_ARMY_BATTLES_WON", false);
							}
							this.encounters.Remove(eventEncounterStateChange.EventArgs.Encounter);
						}
						List<KeyValuePair<EncounterUnit, float>> list2;
						if (this.activeFactionName == "FACTIONMADFAIRIES" && this.encounters.TryGetValue(eventEncounterStateChange.EventArgs.Encounter, out list2))
						{
							if (enumerable2 != null)
							{
								enumerable2 = from contender in enumerable2
								where contender.IsTakingPartInBattle
								select contender;
								List<EncounterUnit> unitsFromContenders = this.GetUnitsFromContenders(enumerable2);
								int num = 0;
								foreach (KeyValuePair<EncounterUnit, float> keyValuePair in list2)
								{
									int num2 = unitsFromContenders.IndexOf(keyValuePair.Key);
									if (num2 >= 0 && keyValuePair.Value <= unitsFromContenders[num2].GetPropertyValue(SimulationProperties.Health))
									{
										num++;
									}
								}
								if (num == list2.Count)
								{
									List<EncounterUnit> unitsFromContenders2 = this.GetUnitsFromContenders(enumerable);
									if ((float)unitsFromContenders2.Count > this.GetStatisticValue("WILD_WALKERS_UNDAMAGED_UNITS_AFTER_BATTLE"))
									{
										this.SetStatisticValue("WILD_WALKERS_UNDAMAGED_UNITS_AFTER_BATTLE", (float)unitsFromContenders2.Count, false);
									}
								}
							}
							this.encounters.Remove(eventEncounterStateChange.EventArgs.Encounter);
						}
						if (enumerable2 != null)
						{
							if (this.IsArmyComposedOfEachUnitFomorians(enumerable2))
							{
								this.SetAchievement("BATTLE_FOMORIANS_VARIETY");
							}
							if (this.ArmyContainsSeaMonster(enumerable))
							{
								this.SetAchievement("SEA_MONSTER_KILLED");
							}
						}
					}
					else if (eventEncounterStateChange.EventArgs.Encounter.GetEncounterResultForEmpire(empire) == EncounterResult.Defeat && this.ArmyContainsSeaMonster(enumerable))
					{
						this.SetAchievement("SEA_MONSTER_FAILED");
					}
				}
			}
			else if (eventEncounterStateChange.EventArgs.EncounterState == EncounterState.Deployment)
			{
				IEnumerable<Contender> enumerable3 = eventEncounterStateChange.EventArgs.Encounter.GetAlliedContendersFromEmpire(empire);
				if (enumerable3 != null)
				{
					if (this.IsArmyComposedOfEachUnitShiftedForms(enumerable3))
					{
						this.SetAchievement("BATTLE_SHIFT_FORM_VARIETY");
					}
					enumerable3 = from contender in enumerable3
					where contender.IsTakingPartInBattle
					select contender;
					if (this.activeFactionName == "FACTIONMADFAIRIES" || (!empire.Faction.IsCustom && empire.SimulationObject.Tags.Contains("FactionTraitCultistsHeatWave")))
					{
						List<KeyValuePair<EncounterUnit, float>> value = (from encounterUnit in this.GetUnitsFromContenders(enumerable3)
						select new KeyValuePair<EncounterUnit, float>(encounterUnit, encounterUnit.GetPropertyValue(SimulationProperties.Health))).ToList<KeyValuePair<EncounterUnit, float>>();
						this.encounters.Add(eventEncounterStateChange.EventArgs.Encounter, value);
					}
					if (enumerable3.Any((Contender alliedContender) => alliedContender.IsPrivateers))
					{
						IEnumerable<Contender> enemiesContenderFromEmpire = eventEncounterStateChange.EventArgs.Encounter.GetEnemiesContenderFromEmpire(empire);
						if (enemiesContenderFromEmpire != null)
						{
							IEnumerable<global::Empire> enumerable4 = (from contender in enemiesContenderFromEmpire
							where contender.IsTakingPartInBattle
							select contender.Empire).Distinct<global::Empire>();
							foreach (global::Empire empire2 in enumerable4)
							{
								DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(empire2);
								if (diplomaticRelation != null && (diplomaticRelation.State.Name == DiplomaticRelationState.Names.Peace || diplomaticRelation.State.Name == DiplomaticRelationState.Names.Alliance))
								{
									this.SetAchievement("ATTACK_ALLY_WITH_PRIVATEERS");
								}
							}
						}
					}
					IEnumerable<Contender> enemiesContenderFromEmpire2 = eventEncounterStateChange.EventArgs.Encounter.GetEnemiesContenderFromEmpire(empire);
					if (this.IsArmyOnlyComposedOfSubmersibleUnits(enumerable3) && this.IsArmyOnlyComposedOfLandUnits(enemiesContenderFromEmpire2))
					{
						this.SetAchievement("ATTACK_LAND_WITH_SUBMERSIBLE");
					}
				}
			}
		}
	}

	private void OnEventEndTurn(Amplitude.Unity.Event.Event eventRaised)
	{
		if (this.departmentOfEducation.Heroes.Count > 0)
		{
			this.SetStatisticValue("HERO_COUNT", (float)this.departmentOfEducation.Heroes.Count, false);
			int num = (from hero in this.departmentOfEducation.Heroes
			select hero.Level + 1).Max();
			if ((float)num > this.GetStatisticValue("MAX_HERO_LEVEL"))
			{
				this.SetStatisticValue("MAX_HERO_LEVEL", (float)Mathf.FloorToInt((float)num), false);
			}
		}
		if (this.departmentOfTheInterior.Cities.Count > 0)
		{
			float num2 = (from city in this.departmentOfTheInterior.Cities
			select city.GetPropertyValue(SimulationProperties.NetCityGrowth)).Max();
			if (num2 > this.GetStatisticValue("MAX_NET_CITY_FOOD"))
			{
				this.SetStatisticValue("MAX_NET_CITY_FOOD", (float)Mathf.FloorToInt(num2), false);
			}
			float num3 = (from city in this.departmentOfTheInterior.Cities
			select city.GetPropertyValue(SimulationProperties.NetCityProduction)).Max();
			if (num3 > this.GetStatisticValue("MAX_NET_CITY_INDUSTRY"))
			{
				this.SetStatisticValue("MAX_NET_CITY_INDUSTRY", (float)Mathf.FloorToInt(num3), false);
			}
			float num4 = (from city in this.departmentOfTheInterior.Cities
			select city.GetPropertyValue(SimulationProperties.NetCityMoney)).Max();
			if (num4 > this.GetStatisticValue("MAX_NET_CITY_DUST"))
			{
				this.SetStatisticValue("MAX_NET_CITY_DUST", (float)Mathf.FloorToInt(num4), false);
			}
			float num5 = (from city in this.departmentOfTheInterior.Cities
			select city.GetPropertyValue(SimulationProperties.NetCityResearch)).Max();
			if (num5 > this.GetStatisticValue("MAX_NET_CITY_SCIENCE"))
			{
				this.SetStatisticValue("MAX_NET_CITY_SCIENCE", (float)Mathf.FloorToInt(num5), false);
			}
			float num6 = (from city in this.departmentOfTheInterior.Cities
			select city.GetPropertyValue(SimulationProperties.NetCityEmpirePoint)).Max();
			if (num6 > this.GetStatisticValue("MAX_NET_CITY_INFLUENCE"))
			{
				this.SetStatisticValue("MAX_NET_CITY_INFLUENCE", (float)Mathf.FloorToInt(num6), false);
			}
			if (this.activeFactionName == "FACTIONBROKENLORDS")
			{
				float num7 = 0f;
				foreach (City city3 in this.departmentOfTheInterior.Cities)
				{
					if (city3.Districts != null && city3.Districts.Count > 0)
					{
						num7 = Mathf.Max(num7, (from district in city3.Districts
						select district.GetPropertyValue(SimulationProperties.DistrictDust)).Max());
					}
				}
				if (num7 > this.GetStatisticValue("BROKEN_LORDS_MAX_DUST_DISTRICT"))
				{
					this.SetStatisticValue("BROKEN_LORDS_MAX_DUST_DISTRICT", num7, false);
				}
			}
		}
		if (!this.strategicResourceNames.Any((StaticString resource) => !this.departmentOfTheTreasury.CanAfford(100f, resource)))
		{
			this.SetAchievement("ALL_STRATEGIC_RESOURCES");
		}
		int num8 = 0;
		List<string> list = new List<string>();
		foreach (City city2 in this.departmentOfTheInterior.Cities)
		{
			if (city2.Region != null && city2.Region.PointOfInterests != null)
			{
				foreach (PointOfInterest pointOfInterest in city2.Region.PointOfInterests)
				{
					PointOfInterestImprovementDefinition pointOfInterestImprovementDefinition = pointOfInterest.PointOfInterestImprovement as PointOfInterestImprovementDefinition;
					if (pointOfInterestImprovementDefinition != null && pointOfInterestImprovementDefinition.Name != null)
					{
						string text = pointOfInterestImprovementDefinition.Name.ToString().ToUpper();
						if (text.StartsWith("RESOURCEEXTRACTOR_STRATEGIC") && !list.Contains(text))
						{
							list.Add(text);
						}
					}
				}
			}
			num8 += city2.TradeRoutes.Count;
		}
		this.SetStatisticValue("STRATEGIC_EXTRACTOR_COUNT", (float)list.Count, false);
		this.SetStatisticValue("TRADE_ROUTES", (float)num8, false);
		this.SetStatisticValue("IMPROVEMENTS_BUILT_PER_GAME", (float)this.departmentOfPlanificationAndDevelopment.StatConstructionBuilt, false);
		if (this.activeFactionName == "FACTIONRAGEWIZARDS")
		{
			this.SetStatisticValue("ARDENT_MAGES_PILLARS_COUNT", (float)this.pillarManager.Count, false);
		}
		if (this.activeFactionName == "FACTIONDRAKKENS" && !this.GetAchievement("DRAKKENS_INSTANT_WORLD_PEACE"))
		{
			this.SetStatisticValue("DRAKKENS_EMPIRE_PEACE_RESTORED_COUNT", 0f, false);
		}
		if ((this.gameService.Game as global::Game).Turn < 49 && this.explorationMap != null)
		{
			int num9 = 0;
			for (int j = 0; j < this.explorationMap.Data.Length; j++)
			{
				if ((this.explorationMap.Data[j] & (short)this.ActiveEmpire.Bits) != 0)
				{
					num9++;
				}
			}
			this.SetStatisticValue("TILES_EXPLORED", (float)num9, false);
		}
		if (this.unitsTeleportedThisTurn.Count > 0)
		{
			this.SetStatisticValue("VAULTERS_UNIT_COUNT_TELEPORTED_TO_BESIEGED_CITY", (float)this.unitsTeleportedThisTurn.Values.Max(), false);
		}
		if (!(this.ActiveEmpire as global::Empire).Faction.IsCustom && this.activeFactionName == "FACTIONMADFAIRIES" && !this.GetAchievement("MAD_FAIRIES_HIGH_SPEED_JOURNEY"))
		{
			this.SetStatisticValue("MAD_FAIRIES_TILES_TRAVELED", 0f, false);
		}
		if (!(this.ActiveEmpire as global::Empire).Faction.IsCustom && this.ActiveEmpire.SimulationObject.Tags.Contains("FactionTraitCultistsHeatWave") && !this.GetAchievement("CULTISTS_MINIONS_LEGION"))
		{
			this.SetStatisticValue("CULTISTS_MINOR_ARMY_BATTLES_WON", 0f, false);
		}
		if (this.seasonService.GetCurrentSeason().SeasonDefinition.SeasonType != Season.ReadOnlyHeatWave)
		{
			this.SetStatisticValue("MAD_SEASON_SEARCHED_TEMPLE_COUNT", 0f, false);
		}
		this.IncrementStatistic("TURNS_PLAYED", false);
		this.Commit();
	}

	private void OnEventEmpirePeaceRestored(Amplitude.Unity.Event.Event eventRaised)
	{
		if (!(eventRaised is EventEmpirePeaceRestored))
		{
			return;
		}
		EventEmpirePeaceRestored eventEmpirePeaceRestored = eventRaised as EventEmpirePeaceRestored;
	}

	private void OnEventFactionAssimilated(Amplitude.Unity.Event.Event eventRaised)
	{
		EventFactionAssimilated eventFactionAssimilated = eventRaised as EventFactionAssimilated;
		if (eventFactionAssimilated.Assimilated && eventFactionAssimilated.Empire.Index == this.ActiveEmpire.Index)
		{
			this.SetStatisticValue("MINOR_FACTIONS_ASSIMILATED_PER_GAME", (float)this.departmentOfTheInterior.AssimilatedFactions.Count, false);
		}
	}

	private void OnEventFogBankAmbush(Amplitude.Unity.Event.Event eventRaised)
	{
		EventFogBankAmbush eventFogBankAmbush = eventRaised as EventFogBankAmbush;
		if (eventFogBankAmbush != null && eventFogBankAmbush.Empire != null && eventFogBankAmbush.Empire.Index == this.ActiveEmpire.Index)
		{
			this.SetAchievement("FOG_BANK_AMBUSH");
		}
	}

	private void OnEventForceShift(Amplitude.Unity.Event.Event eventRaised)
	{
		EventForceShift eventForceShift = eventRaised as EventForceShift;
		if (eventForceShift.Empire.Index == this.ActiveEmpire.Index)
		{
			this.AddToStatistic("FORCE_SHIFT_OVERALL_COUNT", (float)eventForceShift.ShiftingUnitCount, false);
		}
	}

	private void OnEventFortressesAllOwned(Amplitude.Unity.Event.Event eventRaised)
	{
		EventFortressesAllOwned eventFortressesAllOwned = eventRaised as EventFortressesAllOwned;
		if (eventFortressesAllOwned != null && eventFortressesAllOwned.Empire != null && eventFortressesAllOwned.Empire.Index == this.ActiveEmpire.Index)
		{
			this.SetAchievement("ALL_FORTRESSES");
		}
	}

	private void OnEventFortressOccupantSwapped(Amplitude.Unity.Event.Event eventRaised)
	{
		EventFortressOccupantSwapped eventFortressOccupantSwapped = eventRaised as EventFortressOccupantSwapped;
		if (eventFortressOccupantSwapped != null && eventFortressOccupantSwapped.NewOccupant.Index == this.ActiveEmpire.Index && eventFortressOccupantSwapped.OldOccupant.Faction.Name == "Fomorians")
		{
			this.AddToStatistic("FOMORIAN_FORTRESS_CAPTURE_COUNT", 1f, true);
		}
	}

	private void OnEventGameEndedClient(Amplitude.Unity.Event.Event eventRaised)
	{
		if (this.gameService != null && this.gameService.Game != null && this.gameService.Game is global::Game)
		{
			string lobbyData = this.sessionService.Session.GetLobbyData<string>(VictoryCondition.ReadOnlyVictory, null);
			if (!string.IsNullOrEmpty(lobbyData))
			{
				char[] separator = new char[]
				{
					'&'
				};
				string[] array = lobbyData.Split(separator, StringSplitOptions.RemoveEmptyEntries);
				if (array.Length > 0)
				{
					foreach (string text in array)
					{
						string[] array2 = text.Split(Amplitude.String.Separators, StringSplitOptions.RemoveEmptyEntries);
						int num = -1;
						if (array2.Length > 1 && int.TryParse(array2[1], out num) && this.game.Empires != null && this.game.Empires.Length > num)
						{
							this.OnVictory(this.game.Empires[num], array2[0]);
						}
					}
				}
			}
		}
	}

	private void OnEventGeomancyBuffApplied(Amplitude.Unity.Event.Event eventRaised)
	{
		if (!(eventRaised is EventGeomancyBuffApplied))
		{
			return;
		}
		EventGeomancyBuffApplied eventGeomancyBuffApplied = eventRaised as EventGeomancyBuffApplied;
	}

	private void OnEventDustDepositExplored(Amplitude.Unity.Event.Event eventRaised)
	{
		if (!(eventRaised is EventDustDepositSearched))
		{
			return;
		}
		EventDustDepositSearched eventDustDepositSearched = eventRaised as EventDustDepositSearched;
		if (this.ActiveEmpire.Index == eventDustDepositSearched.Empire.Index)
		{
			this.IncrementStatistic("MAD_SEASON_SEARCHED_TEMPLE_COUNT", false);
		}
	}

	private void OnEventHeroCaptured(Amplitude.Unity.Event.Event eventRaised)
	{
		EventHeroCaptured eventHeroCaptured = eventRaised as EventHeroCaptured;
		if (eventHeroCaptured.JailerEmpireIndex == this.ActiveEmpire.Index)
		{
			this.IncrementStatistic("HERO_CAPTURE_COUNT", false);
		}
	}

	private void OnEventWorldBattleUnitPerformDeath(Amplitude.Unity.Event.Event eventRaised)
	{
		if (!(eventRaised is EventWorldBattleUnitPerformDeath))
		{
			return;
		}
		EventWorldBattleUnitPerformDeath eventWorldBattleUnitPerformDeath = eventRaised as EventWorldBattleUnitPerformDeath;
		if (this.CheckRovingClansHeroKillerSetsekeAchievementCondition(eventWorldBattleUnitPerformDeath.DeathUnit, eventWorldBattleUnitPerformDeath.KillerUnit))
		{
			this.IncrementStatistic("ROVING_CLANS_SETSEKE_HERO_KILLS_COUNT", false);
		}
		if (this.WasKaijuLiceKilled(eventWorldBattleUnitPerformDeath.DeathUnit))
		{
			this.AddToStatistic("LICE_KILLED_OVERALL_COUNT", 1f, false);
		}
	}

	private void OnEventOnApplySimulationDescriptorChange(Amplitude.Unity.Event.Event eventRaised)
	{
		if (!(eventRaised is EventOnApplySimulationDescriptorChange))
		{
			return;
		}
		if (!this.GetAchievement("ALL_GEOMANCY_BUFFS"))
		{
			EventOnApplySimulationDescriptorChange eventOnApplySimulationDescriptorChange = eventRaised as EventOnApplySimulationDescriptorChange;
			if (eventOnApplySimulationDescriptorChange != null && eventOnApplySimulationDescriptorChange.SimulationDescriptorChangeInstruction.InstructionAction == SimulationDescriptorChangeInstruction.Action.Add && this.ActiveEmpire.Index == eventOnApplySimulationDescriptorChange.Empire.Index)
			{
				EncounterUnit encounterUnit = eventOnApplySimulationDescriptorChange.Target.EncounterUnit;
				if (encounterUnit.UnitDuplicatedSimulationObject.Tags.Contains("UnitActionTerrainDefUp") && encounterUnit.UnitDuplicatedSimulationObject.Tags.Contains("UnitActionForestGeomancyInitUp") && encounterUnit.UnitDuplicatedSimulationObject.Tags.Contains("UnitActionVolcanicGeomancyAttUp"))
				{
					this.SetAchievement("ALL_GEOMANCY_BUFFS");
				}
			}
		}
	}

	private void OnEventInfiltrationActionResult(Amplitude.Unity.Event.Event eventRaised)
	{
		EventInfiltrationActionResult infiltrationActionResultEvent = eventRaised as EventInfiltrationActionResult;
		if ((from city in this.departmentOfTheInterior.Cities
		where city.GUID == infiltrationActionResultEvent.InfiltratedCityGUID
		select city).FirstOrDefault<City>() != null || this.ActiveEmpire.Index != infiltrationActionResultEvent.Empire.Index || infiltrationActionResultEvent.InfiltratedHeroUnitGUID == GameEntityGUID.Zero)
		{
			return;
		}
		int num = 0;
		if (this.departmentOfIntelligence != null)
		{
			num = this.departmentOfIntelligence.SpiedGarrisons.Max((SpiedGarrison spy) => spy.ActionInfiltrationCount);
		}
		this.SetStatisticValue("SPY_ACTION_SERIES_COUNT", (float)num, false);
		if (infiltrationActionResultEvent.InfiltrationAction is InfiltrationActionOnCity_InjureGovernor && infiltrationActionResultEvent.AntiSpyResult == DepartmentOfIntelligence.AntiSpyResult.Nothing)
		{
			this.SetAchievement("KILL_GOVERNOR");
		}
	}

	private void OnEventNewEraGlobal(Amplitude.Unity.Event.Event eventRaised)
	{
		EventNewEraGlobal eventNewEraGlobal = eventRaised as EventNewEraGlobal;
		if (this.departmentOfScience.CurrentTechnologyEraNumber == eventNewEraGlobal.NewEraNumber && eventNewEraGlobal.NewEraNumber == 2 && eventNewEraGlobal.NewEraEmpire.Index == this.ActiveEmpire.Index && this.activeFactionName == "FACTIONREPLICANTS")
		{
			this.SetAchievement("FIRST_SECOND_ERA_REPLICANTS");
		}
	}

	private void OnEventMapBoostApplied(Amplitude.Unity.Event.Event eventRaised)
	{
		if (!(eventRaised is EventMapBoostApplied))
		{
			return;
		}
		EventMapBoostApplied eventMapBoostApplied = eventRaised as EventMapBoostApplied;
		if (eventMapBoostApplied.Empire.Index != this.ActiveEmpire.Index)
		{
			return;
		}
		Army army = this.departmentOfDefense.GetArmy(eventMapBoostApplied.ArmyGUID);
		if (army == null)
		{
			return;
		}
		if (army.MapBoostsOnArmy.Contains("NourishingMapBoost") && army.MapBoostsOnArmy.Contains("EmpoweringMapBoost") && army.MapBoostsOnArmy.Contains("SwiftMapBoost"))
		{
			this.SetAchievement("ALL_MAP_BOOSTS_BUFFS");
		}
	}

	private void OnEventOceanControlWithTrade(Amplitude.Unity.Event.Event eventRaised)
	{
		EventOceanControlWithTrade eventOceanControlWithTrade = eventRaised as EventOceanControlWithTrade;
		if (eventOceanControlWithTrade != null && eventOceanControlWithTrade.Empire != null && eventOceanControlWithTrade.Empire.Index == this.ActiveEmpire.Index)
		{
			this.SetAchievement("OCEAN_CONTROL_WITH_TRADE");
		}
	}

	private void OnEventOrbsCollected(Amplitude.Unity.Event.Event eventRaised)
	{
		EventOrbsCollected eventOrbsCollected = eventRaised as EventOrbsCollected;
		if (eventOrbsCollected.Empire.Index == this.ActiveEmpire.Index && eventOrbsCollected.OrbsQuantity > 0)
		{
			this.AddToStatistic("COLLECT_ORBS_OVERALL_COUNT", (float)eventOrbsCollected.OrbsQuantity, false);
			float propertyValue = this.ActiveEmpire.GetPropertyValue("OrbAccumulator");
			if (propertyValue >= 1000f)
			{
				this.SetAchievement("COLLECT_ORBS_PLAYTHROUGH");
			}
			if (eventOrbsCollected.OrbsQuantity >= 20)
			{
				this.SetAchievement("COLLECT_BIG_ORB");
			}
		}
	}

	private void OnEventPillageSucceed(Amplitude.Unity.Event.Event eventRaised)
	{
		EventPillageSucceed eventPillageSucceed = eventRaised as EventPillageSucceed;
		if (eventPillageSucceed.Empire.Index == this.ActiveEmpire.Index)
		{
			this.IncrementStatistic("PILLAGE_BUILDINGS_COUNT", false);
			for (int i = 0; i < eventPillageSucceed.Loots.Length; i++)
			{
				if (eventPillageSucceed.Loots[i] is DroppableResource && (eventPillageSucceed.Loots[i] as DroppableResource).ResourceName == DepartmentOfTheTreasury.Resources.EmpireMoney)
				{
					this.AddToStatistic("PILLAGE_DUST_LOOT_COUNT", (float)(eventPillageSucceed.Loots[i] as DroppableResource).Quantity, false);
				}
			}
		}
	}

	private void OnEventQuestComplete(Amplitude.Unity.Event.Event eventRaised)
	{
		EventQuestComplete eventQuestComplete = eventRaised as EventQuestComplete;
		if (eventQuestComplete.Empire.Index == this.ActiveEmpire.Index)
		{
			this.IncrementStatistic("QUESTS_COMPLETED", false);
			this.departmentOfPlanificationAndDevelopment.StatQuestCompleted++;
			if (eventQuestComplete.Quest.QuestDefinition.Tags.Contains("FinalQuest"))
			{
				this.IncrementStatistic("FINAL_QUEST_COMPLETED", false);
				this.IncrementStatistic("FINAL_QUEST_COMPLETED_" + this.activeFactionName, false);
				this.SetStatisticValue("FINAL_QUEST_COMPLETED_COUNT", (float)this.majorFactionNames.Count((string name) => this.GetStatisticValue("FINAL_QUEST_COMPLETED_" + name) > 0f), false);
			}
			if (eventQuestComplete.Quest.QuestDefinition.IsGlobal && eventQuestComplete.Quest.QuestDefinition.GlobalWinner == GlobalQuestWinner.Participants && eventQuestComplete.Quest.QuestDefinition.Steps != null && eventQuestComplete.Quest.QuestDefinition.Steps.Length != 0)
			{
				StaticString stepName = eventQuestComplete.Quest.QuestDefinition.Steps[eventQuestComplete.Quest.GetCurrentStepIndex()].Name + Quest.PersonnalProgressStepSuffix;
				if (eventQuestComplete.Quest.GetStepProgressionValueByName(stepName) > 0)
				{
					this.SetAchievement("COOP_QUEST_CONTRIBUTOR");
				}
			}
			if (eventQuestComplete.Quest.QuestDefinition.TriggersAchievementStatistic)
			{
				StaticString statisticName = "QUEST_COMPLETED_" + eventQuestComplete.Quest.QuestDefinition.Name.ToString().ToUpper();
				if (eventQuestComplete.Quest.QuestDefinition.Category == QuestDefinition.CategoryMedal && this.GetStatisticValue(statisticName) <= 0f)
				{
					this.IncrementStatistic("MEDALS_COMPLETED", false);
				}
				this.IncrementStatistic(statisticName, false);
			}
			if (!this.GetAchievement("FOMORIAN_QUEST_COMPLETER"))
			{
				int num = 0;
				if (eventQuestComplete.Quest.QuestDefinition.Tags.Contains("NavalTalk"))
				{
					foreach (Quest quest in this.departmentOfInternalAffairs.QuestJournal[QuestState.Completed])
					{
						if (quest.QuestDefinition.Tags.Contains("NavalTalk"))
						{
							num++;
						}
					}
					if (num >= 5)
					{
						this.SetAchievement("FOMORIAN_QUEST_COMPLETER");
					}
				}
			}
		}
	}

	private void OnEventSeaDemons4Continents(Amplitude.Unity.Event.Event eventRaised)
	{
		EventSeaDemons4Continents eventSeaDemons4Continents = eventRaised as EventSeaDemons4Continents;
		if (this.ActiveEmpire != null && eventSeaDemons4Continents != null && eventSeaDemons4Continents.Empire != null && eventSeaDemons4Continents.Empire.Index == this.ActiveEmpire.Index)
		{
			this.SetAchievement("SEA_DEMONS_4_CONTINENTS");
		}
	}

	private void OnEventSeaDemonsOceanicHub(Amplitude.Unity.Event.Event eventRaised)
	{
		EventSeaDemonsOceanicHub eventSeaDemonsOceanicHub = eventRaised as EventSeaDemonsOceanicHub;
		if (eventSeaDemonsOceanicHub != null && eventSeaDemonsOceanicHub.Empire != null && eventSeaDemonsOceanicHub.Empire.Index == this.ActiveEmpire.Index)
		{
			this.SetAchievement("SEA_DEMONS_OCEANIC_HUB");
		}
	}

	private void OnEventColonize(Amplitude.Unity.Event.Event eventRaised)
	{
		EventColonize eventColonize = eventRaised as EventColonize;
		if (eventColonize != null && eventColonize.Empire != null && eventColonize.Empire.Index == this.ActiveEmpire.Index && (eventColonize.Empire as MajorEmpire).Faction.Name.ToString().ToUpper() == "FACTIONSEADEMONS" && (eventColonize.Empire as MajorEmpire).Faction.Name.ToString().ToUpper() == "FACTIONSEADEMONS" && this.IsRegionPartOfAnOceanicHub(eventColonize.City.Region, eventColonize.Empire as global::Empire))
		{
			this.SetAchievement("SEA_DEMONS_OCEANIC_HUB");
		}
	}

	private void OnEventSwapCity(Amplitude.Unity.Event.Event eventRaised)
	{
		EventSwapCity eventSwapCity = eventRaised as EventSwapCity;
		if (eventSwapCity.NewOwnerEmpireIndex == this.ActiveEmpire.Index)
		{
			int num = this.departmentOfTheInterior.Cities.Sum((City city) => city.Districts.Count((District district) => this.IsDistrictWonder(district)));
			this.SetStatisticValue("DIFFERENT_WONDERS", (float)num, false);
			bool flag = eventSwapCity.City.Districts.Any((District district) => this.IsDistrictWonder(district));
			if (flag)
			{
				this.SetAchievement("CAPTURE_WONDER");
			}
			if ((this.ActiveEmpire as MajorEmpire).Faction.Name.ToString().ToUpper() == "FACTIONSEADEMONS" && this.IsRegionPartOfAnOceanicHub(eventSwapCity.City.Region, eventSwapCity.Empire as global::Empire))
			{
				this.SetAchievement("SEA_DEMONS_OCEANIC_HUB");
			}
		}
	}

	private void OnEventTechnologyEnded(Amplitude.Unity.Event.Event eventRaised)
	{
		if (this.departmentOfScience == null || this.ActiveEmpire == null)
		{
			return;
		}
		EventTechnologyEnded eventTechnologyEnded = eventRaised as EventTechnologyEnded;
		if (eventTechnologyEnded.Empire.Index == this.ActiveEmpire.Index && !(this.ActiveEmpire as global::Empire).Faction.IsCustom && (eventTechnologyEnded.ConstructibleElement as TechnologyDefinition).TechnologyFlags == DepartmentOfScience.ConstructibleElement.TechnologyFlag.OrbUnlock)
		{
			int num = 0;
			foreach (DepartmentOfScience.ConstructibleElement constructibleElement in this.departmentOfScience.TechnologyDatabase)
			{
				if (constructibleElement.TechnologyFlags == DepartmentOfScience.ConstructibleElement.TechnologyFlag.OrbUnlock && this.departmentOfScience.GetTechnologyState(constructibleElement) == DepartmentOfScience.ConstructibleElement.State.Researched)
				{
					num++;
				}
			}
			if (num >= 13)
			{
				this.SetAchievement("ALL_ORB_UNLOCKS");
			}
		}
	}

	private void OnEventTechnologyStealed(Amplitude.Unity.Event.Event eventRaised)
	{
		EventTechnologyStealed eventTechnologyStealed = eventRaised as EventTechnologyStealed;
		if (eventTechnologyStealed.Empire.Index == this.ActiveEmpire.Index && !(this.ActiveEmpire as global::Empire).Faction.IsCustom)
		{
			this.IncrementStatistic("STEAL_TECHNOLOGIES_COUNT", false);
		}
	}

	private void OnEventUniqueFacilitiesUpdated(Amplitude.Unity.Event.Event eventRaised)
	{
		EventUniqueFacilitiesUpdated eventUniqueFacilitiesUpdated = eventRaised as EventUniqueFacilitiesUpdated;
		if (eventUniqueFacilitiesUpdated != null && eventUniqueFacilitiesUpdated.Empire != null && eventUniqueFacilitiesUpdated.Empire.Index == this.ActiveEmpire.Index)
		{
			int num = (int)this.GetStatisticValue("UNIQUE_FACILITIES_OWNED");
			PointOfInterestTemplate pointOfInterestTemplate = eventUniqueFacilitiesUpdated.Facility.PointOfInterestDefinition.PointOfInterestTemplate;
			if (pointOfInterestTemplate == null)
			{
				return;
			}
			string text;
			int num2;
			if (pointOfInterestTemplate.Properties.TryGetValue("FacilityImprovement", out text) && int.TryParse(text.Substring("FacilityUnique".Length), out num2))
			{
				num2--;
				num |= 1 << num2;
				this.SetStatisticValue("UNIQUE_FACILITIES_OWNED", (float)num, true);
				if (this.uniqueFacilitiesCount < 1)
				{
					return;
				}
				int num3 = (int)Mathf.Pow(2f, (float)this.uniqueFacilitiesCount) - 1;
				if (num == num3)
				{
					this.SetAchievement("ALL_UNIQUE_FACILITIES");
				}
			}
		}
	}

	private void OnEventUnitsTeleported(Amplitude.Unity.Event.Event eventRaised)
	{
		if (!(eventRaised is EventUnitsTeleported))
		{
			return;
		}
		EventUnitsTeleported eventUnitsTeleported = eventRaised as EventUnitsTeleported;
		if (eventUnitsTeleported.Empire.Index == this.ActiveEmpire.Index && !(this.ActiveEmpire as global::Empire).Faction.IsCustom)
		{
			this.SetStatisticValue("VAULTERS_UNITS_TELEPORTED_TO_SIEGING_HERO", (float)eventUnitsTeleported.TeleportedCount, false);
		}
	}

	private void OnEventVillageConverted(Amplitude.Unity.Event.Event eventRaised)
	{
		EventVillageConverted eventVillageConverted = eventRaised as EventVillageConverted;
		if (eventVillageConverted.Empire.Index == this.ActiveEmpire.Index)
		{
			this.SetStatisticValue("CULTISTS_CONVERTED_VILLAGES", (float)((this.ActiveEmpire as MajorEmpire).ConvertedVillages.Count + 1), false);
		}
	}

	private void OnEventVillagePacified(Amplitude.Unity.Event.Event eventRaised)
	{
		EventVillagePacified eventVillagePacified = eventRaised as EventVillagePacified;
		if (eventVillagePacified.Empire.Index == this.ActiveEmpire.Index)
		{
			this.IncrementStatistic("VILLAGES_PACIFIED", false);
		}
	}

	private void OnEventWorldArmyMoveTo(Amplitude.Unity.Event.Event eventRaised)
	{
		if (eventRaised is EventWorldArmyMoveTo)
		{
			EventWorldArmyMoveTo eventWorldArmyMoveTo = eventRaised as EventWorldArmyMoveTo;
			if (eventWorldArmyMoveTo.Army.Empire.Index == this.ActiveEmpire.Index && !(this.ActiveEmpire as global::Empire).Faction.IsCustom && this.activeFactionName == "FACTIONMADFAIRIES" && !this.GetAchievement("MAD_FAIRIES_HIGH_SPEED_JOURNEY"))
			{
				this.SetStatisticValue("MAD_FAIRIES_TILES_TRAVELED", eventWorldArmyMoveTo.Army.GetPropertyValue(SimulationProperties.TilesMovedThisTurn), false);
			}
		}
	}

	private void OnEventWorldTerraformed(Amplitude.Unity.Event.Event eventRaised)
	{
		if (!(eventRaised is EventEmpireWorldTerraformed))
		{
			return;
		}
		EventEmpireWorldTerraformed eventEmpireWorldTerraformed = eventRaised as EventEmpireWorldTerraformed;
		if (eventEmpireWorldTerraformed.Empire.Index != this.ActiveEmpire.Index)
		{
			return;
		}
		this.AddToStatistic("TILES_TERRAFORMED_OVERALL_COUNT", (float)eventEmpireWorldTerraformed.TerraformedTiles.Length, false);
		IWorldPositionningService service = this.gameService.Game.Services.GetService<IWorldPositionningService>();
		WorldPosition[] terraformedTiles = eventEmpireWorldTerraformed.TerraformedTiles;
		List<City> list = new List<City>();
		for (int i = 0; i < terraformedTiles.Length; i++)
		{
			District district = service.GetDistrict(terraformedTiles[i]);
			if (district != null && district.City.Empire.Index != this.ActiveEmpire.Index && !list.Contains(district.City))
			{
				list.Add(district.City);
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			City city = list[j];
			bool flag = true;
			for (int k = 0; k < city.Districts.Count; k++)
			{
				District district2 = city.Districts[k];
				if (district2.Type == DistrictType.Exploitation)
				{
					bool flag2 = false;
					for (int l = 0; l < terraformedTiles.Length; l++)
					{
						if (district2.WorldPosition == terraformedTiles[l] || district2.WasTerraformed)
						{
							flag2 = true;
							break;
						}
					}
					if (!flag2)
					{
						flag = false;
						break;
					}
				}
			}
			if (flag)
			{
				this.SetAchievement("ENEMY_EXPLOITATION_TERRAFORMED");
				break;
			}
		}
	}

	private void OnEventCreepingNodeComplete(Amplitude.Unity.Event.Event eventRaised)
	{
		if (!(eventRaised is EventCreepingNodeUpgradeComplete))
		{
			return;
		}
		EventCreepingNodeUpgradeComplete eventCreepingNodeUpgradeComplete = eventRaised as EventCreepingNodeUpgradeComplete;
		if (eventCreepingNodeUpgradeComplete.Empire.Index != this.ActiveEmpire.Index)
		{
			return;
		}
		this.AddToStatistic("NODES_BUILT_OVERALL_COUNT", 1f, false);
		if (this.departmentOfCreepingNodes != null)
		{
			int num = 0;
			for (int i = 0; i < this.departmentOfCreepingNodes.Nodes.Count; i++)
			{
				if (this.departmentOfCreepingNodes.Nodes[i].IsUpgradeReady)
				{
					num++;
				}
			}
			this.SetStatisticValue("CURRENT_NODES_COUNT", (float)num, false);
		}
	}

	private void OnEventTameKaiju(Amplitude.Unity.Event.Event eventRaised)
	{
		if (!(eventRaised is EventKaijuTamed))
		{
			return;
		}
		EventKaijuTamed eventKaijuTamed = eventRaised as EventKaijuTamed;
		if (eventKaijuTamed.Empire.Index != this.ActiveEmpire.Index)
		{
			return;
		}
		if (this.departmentOfTheInterior != null)
		{
			int count = this.departmentOfTheInterior.TamedKaijuGarrisons.Count;
			this.SetStatisticValue("TAMED_KAIJU_COUNT", (float)count, false);
		}
	}

	private void OnEventFactionIntegrated(Amplitude.Unity.Event.Event eventRaised)
	{
		if (!(eventRaised is EventFactionIntegrated))
		{
			return;
		}
		EventFactionIntegrated eventFactionIntegrated = eventRaised as EventFactionIntegrated;
		if (eventFactionIntegrated.Empire.Index != this.ActiveEmpire.Index)
		{
			return;
		}
		if (this.departmentOfPlanificationAndDevelopment != null)
		{
			this.SetStatisticValue("INTEGRATED_FACTIONS_COUNT", (float)this.departmentOfPlanificationAndDevelopment.IntegratedFactionsCount(), false);
		}
	}

	private void OnEventCityAddRegionalEffects(Amplitude.Unity.Event.Event eventRaised)
	{
		if (!(eventRaised is EventCityAddRegionalEffects))
		{
			return;
		}
		EventCityAddRegionalEffects eventCityAddRegionalEffects = eventRaised as EventCityAddRegionalEffects;
		if (eventCityAddRegionalEffects.Empire.Index != this.ActiveEmpire.Index)
		{
			return;
		}
		IGameService service = Services.GetService<IGameService>();
		IRegionalEffectsService service2 = service.Game.Services.GetService<IRegionalEffectsService>();
		List<Kaiju> list = new List<Kaiju>();
		foreach (RegionalEffect regionalEffect in eventCityAddRegionalEffects.City.GetRegionalEffects())
		{
			IRegionalEffectsProviderGameEntity regionalEffectsProviderGameEntity = null;
			if (service2.TryGetEffectOwner(regionalEffect.GUID, out regionalEffectsProviderGameEntity) && regionalEffectsProviderGameEntity is Kaiju)
			{
				Kaiju item = regionalEffectsProviderGameEntity as Kaiju;
				if (!list.Contains(item))
				{
					list.Add(item);
				}
			}
		}
		int count = list.Count;
		this.SetStatisticValue("AFFECTING_KAIJU_COUNT", (float)count, false);
	}

	private void OnEventCityDamagedByKaijuEarthquake(Amplitude.Unity.Event.Event eventRaised)
	{
		if (!(eventRaised is EventCityDamagedByEarthquake))
		{
			return;
		}
		EventCityDamagedByEarthquake eventCityDamagedByEarthquake = eventRaised as EventCityDamagedByEarthquake;
		if (eventCityDamagedByEarthquake.Empire.Index != this.ActiveEmpire.Index)
		{
			return;
		}
		float value = eventCityDamagedByEarthquake.EarthquakeGarrisonDamage + eventCityDamagedByEarthquake.EarthquakeCityPointDamage;
		this.AddToStatistic("TREMOR_DAMAGE_OVERALL_COUNT", value, false);
	}

	private void OnEventKaijuLost(Amplitude.Unity.Event.Event eventRaised)
	{
		if (!(eventRaised is EventKaijuLost))
		{
			return;
		}
		EventKaijuLost eventKaijuLost = eventRaised as EventKaijuLost;
		if (eventKaijuLost.Empire.Index != this.ActiveEmpire.Index)
		{
			return;
		}
		MajorEmpire lastOwner = eventKaijuLost.LastOwner;
		Kaiju kaiju = eventKaijuLost.Kaiju;
		int takenFromEmpire = eventKaijuLost.TakenFromEmpire;
		EventKaijuLost.KaijuLostReason lostReason = eventKaijuLost.LostReason;
		int turn = (this.gameService.Game as global::Game).Turn;
		int tamedTurn = kaiju.TamedTurn;
		int previousStunnerIndex = kaiju.PreviousStunnerIndex;
		int previousStunnedTurn = kaiju.PreviousStunnedTurn;
		if (lostReason == EventKaijuLost.KaijuLostReason.FREE && takenFromEmpire != -1 && previousStunnerIndex != -1 && lastOwner.Index == eventKaijuLost.Empire.Index && lastOwner.Index != takenFromEmpire && previousStunnerIndex == eventKaijuLost.Empire.Index && turn == tamedTurn && turn == previousStunnedTurn)
		{
			this.SetAchievement("CATCH_AND_RELEASE");
		}
	}

	private void OnVictory(global::Empire empire, StaticString victoryConditionName)
	{
		if (this.alreadyWon && victoryConditionName.ToString().ToUpper() != "SHARED")
		{
			Diagnostics.Log("Ignoring victory achievement as one has already been raised this game");
			return;
		}
		if (empire == null)
		{
			Diagnostics.LogError("Winning empire is null");
			return;
		}
		if (string.IsNullOrEmpty(victoryConditionName))
		{
			Diagnostics.LogError("Victory condition is null");
			return;
		}
		Diagnostics.Assert(this.playerControllerRepositoryService != null);
		for (int i = 0; i < this.departmentOfForeignAffairs.DiplomaticRelations.Count; i++)
		{
			if (this.departmentOfForeignAffairs.DiplomaticRelations[i].OtherEmpireIndex != this.ActiveEmpire.Index && this.departmentOfForeignAffairs.DiplomaticRelations[i].RelationDuration >= 100f && this.departmentOfForeignAffairs.DiplomaticRelations[i].State.Name == DiplomaticRelationState.Names.Alliance)
			{
				this.SetAchievement("LONG_UNBROKEN_ALLIANCE");
			}
		}
		if (this.ActiveEmpire == empire && empire.Faction != null)
		{
			this.SetAchievement(victoryConditionName.ToString().ToUpper() + "_VICTORY");
			if (!TutorialManager.IsActivated)
			{
				Faction faction = empire.Faction;
				if (faction.IsCustom)
				{
					this.SetAchievement("FACTION_CUSTOM_VICTORY");
				}
				else
				{
					this.SetAchievement(this.activeFactionName + "_VICTORY");
					if (this.activeFactionName == "FACTIONDRAKKENS" && this.departmentOfPlanificationAndDevelopment.StatWarDeclarations <= 0)
					{
						this.SetAchievement("DRAKKENS_VICTORY_NO_WAR");
					}
					else if (this.activeFactionName == "FACTIONSEADEMONS" && !this.departmentOfDefense.LandUnitBuildOrPurchased)
					{
						this.SetAchievement("SEA_DEMONS_NO_LAND_UNITS");
					}
				}
				int num = this.orderedDifficulties.IndexOf(this.game.GameDifficulty);
				if (num == -1)
				{
					Diagnostics.LogError("Couldn't get difficulty index for: " + this.game.GameDifficulty);
				}
				else if (this.sessionService.Session.SessionMode == SessionMode.Single)
				{
					for (int j = num; j >= 0; j--)
					{
						this.SetAchievement("DIFFICULTY" + j.ToString() + "_VICTORY");
					}
				}
				string lobbyData = this.sessionService.Session.GetLobbyData<string>("Scenario", null);
				if (!string.IsNullOrEmpty(lobbyData))
				{
					this.SetAchievement("SCENARIO_VICTORY");
				}
				if (this.sessionService.Session.SessionMode != SessionMode.Single)
				{
					if (this.game.Empires.Count((global::Empire emp) => !emp.IsControlledByAI) > 1)
					{
						this.SetAchievement("MULTIPLAYER_VICTORY");
					}
				}
			}
			this.alreadyWon = true;
			this.Commit();
		}
	}

	private void PlayerControllerRepositoryService_ActivePlayerControllerChange(object sender, ActivePlayerControllerChangeEventArgs e)
	{
		if (e.ActivePlayerController != null && e.ActivePlayerController.Empire != null)
		{
			this.UnhookEmpireEvents();
			this.activeFactionName = (e.ActivePlayerController.Empire as MajorEmpire).Faction.Name.ToString().ToUpper();
			if (this.activeFactionName == "FACTIONMEZARI")
			{
				this.activeFactionName = "FACTIONVAULTERS";
			}
			this.departmentOfTheInterior = e.ActivePlayerController.Empire.GetAgency<DepartmentOfTheInterior>();
			this.departmentOfEducation = e.ActivePlayerController.Empire.GetAgency<DepartmentOfEducation>();
			this.departmentOfTheTreasury = e.ActivePlayerController.Empire.GetAgency<DepartmentOfTheTreasury>();
			this.departmentOfPlanificationAndDevelopment = e.ActivePlayerController.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
			this.departmentOfInternalAffairs = e.ActivePlayerController.Empire.GetAgency<DepartmentOfInternalAffairs>();
			this.departmentOfForeignAffairs = e.ActivePlayerController.Empire.GetAgency<DepartmentOfForeignAffairs>();
			this.departmentOfTransportation = e.ActivePlayerController.Empire.GetAgency<DepartmentOfTransportation>();
			this.departmentOfScience = e.ActivePlayerController.Empire.GetAgency<DepartmentOfScience>();
			this.departmentOfDefense = e.ActivePlayerController.Empire.GetAgency<DepartmentOfDefense>();
			IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
			if (service != null && service.IsShared(DownloadableContent11.ReadOnlyName))
			{
				this.departmentOfIntelligence = e.ActivePlayerController.Empire.GetAgency<DepartmentOfIntelligence>();
				Diagnostics.Assert(this.departmentOfIntelligence != null);
			}
			if (service != null && service.IsShared(DownloadableContent20.ReadOnlyName))
			{
				this.departmentOfCreepingNodes = e.ActivePlayerController.Empire.GetAgency<DepartmentOfCreepingNodes>();
				Diagnostics.Assert(this.departmentOfCreepingNodes != null);
			}
			Diagnostics.Assert(this.departmentOfTheInterior != null);
			Diagnostics.Assert(this.departmentOfEducation != null);
			Diagnostics.Assert(this.departmentOfTheTreasury != null);
			Diagnostics.Assert(this.departmentOfPlanificationAndDevelopment != null);
			Diagnostics.Assert(this.departmentOfInternalAffairs != null);
			Diagnostics.Assert(this.departmentOfForeignAffairs != null);
			Diagnostics.Assert(this.departmentOfTransportation != null);
			Diagnostics.Assert(this.departmentOfDefense != null);
			if (!this.GetAchievement("TWO_CITIES_EARLY") || !this.GetAchievement("WELCOME_TO_THE_FOLD"))
			{
				this.departmentOfTheInterior.CitiesCollectionChanged += this.DepartmentOfTheInterior_CitiesCollectionChanged;
			}
			if (!this.GetAchievement("MASS_DUST_GAINED"))
			{
				this.departmentOfTheTreasury.ResourcePropertyChange += this.DepartmentOfTheTreasury_ResourcePropertyChange;
			}
			if (this.activeFactionName == "FACTIONVAULTERS" && !this.GetAchievement("VAULTERS_TELEPORTER"))
			{
				this.departmentOfTransportation.ArmyTeleportedToCity += this.DepartmentOfTransportation_ArmyTeleportedToCity;
			}
		}
	}

	private void RegisterEventHandlers()
	{
		this.eventHandlers = new Dictionary<StaticString, AchievementManager.AchievementEventHandler>();
		this.eventHandlers.Add(EventBeginTurn.Name, new AchievementManager.AchievementEventHandler(this.OnEventBeginTurn));
		this.eventHandlers.Add(EventGameEnded.Name, new AchievementManager.AchievementEventHandler(this.OnEventGameEndedClient));
		this.eventHandlers.Add(EventEndTurn.Name, new AchievementManager.AchievementEventHandler(this.OnEventEndTurn));
		this.eventHandlers.Add(EventQuestComplete.Name, new AchievementManager.AchievementEventHandler(this.OnEventQuestComplete));
		this.eventHandlers.Add(EventBoosterActivated.Name, new AchievementManager.AchievementEventHandler(this.OnEventBoosterActivated));
		this.eventHandlers.Add(EventDiplomaticRelationStateChange.Name, new AchievementManager.AchievementEventHandler(this.OnEventDiplomaticRelationStateChange));
		this.eventHandlers.Add(EventAutomaticBattleUnitSimulationApplyBattleEffect.Name, new AchievementManager.AchievementEventHandler(this.OnEventAutomaticBattleUnitSimulationApplyBattleEffect));
		this.eventHandlers.Add(EventWorldArmyMoveTo.Name, new AchievementManager.AchievementEventHandler(this.OnEventWorldArmyMoveTo));
		this.eventHandlers.Add(EventAutomaticBattleUnitDeath.Name, new AchievementManager.AchievementEventHandler(this.OnEventAutomaticBattleUnitDeath));
		this.eventHandlers.Add(EventWorldBattleUnitPerformDeath.Name, new AchievementManager.AchievementEventHandler(this.OnEventWorldBattleUnitPerformDeath));
		this.eventHandlers.Add(EventOnApplySimulationDescriptorChange.Name, new AchievementManager.AchievementEventHandler(this.OnEventOnApplySimulationDescriptorChange));
		if (!this.GetAchievement("COLLECT_BIG_ORB") || !this.GetAchievement("COLLECT_ORBS_PLAYTHROUGH") || !this.GetAchievement("COLLECT_ORBS_OVERALL"))
		{
			this.eventHandlers.Add(EventOrbsCollected.Name, new AchievementManager.AchievementEventHandler(this.OnEventOrbsCollected));
		}
		if (!this.GetAchievement("MANTA_EXTRACTION_OVERALL"))
		{
			this.eventHandlers.Add(EventAspirate.Name, new AchievementManager.AchievementEventHandler(this.OnEventAspirate));
		}
		if (!this.GetAchievement("CAST_VOTES_OVERALL"))
		{
			this.eventHandlers.Add(EventCastSeasonVote.Name, new AchievementManager.AchievementEventHandler(this.OnEventCastSeasonVote));
		}
		if (!this.GetAchievement("FORCE_SHIFT_OVERALL"))
		{
			this.eventHandlers.Add(EventForceShift.Name, new AchievementManager.AchievementEventHandler(this.OnEventForceShift));
		}
		if (!this.GetAchievement("ALL_ORB_UNLOCKS"))
		{
			this.eventHandlers.Add(EventTechnologyEnded.Name, new AchievementManager.AchievementEventHandler(this.OnEventTechnologyEnded));
		}
		if (!this.GetAchievement("DROWN_ON_ICE"))
		{
			this.eventHandlers.Add(EventArmyDrowned.Name, new AchievementManager.AchievementEventHandler(this.OnEventArmyDrowned));
		}
		if (!this.GetAchievement("SPY_ACTION_SERIES") || !this.GetAchievement("KILL_GOVERNOR"))
		{
			this.eventHandlers.Add(EventInfiltrationActionResult.Name, new AchievementManager.AchievementEventHandler(this.OnEventInfiltrationActionResult));
		}
		if (!this.GetAchievement("FIRST_SECOND_ERA_REPLICANTS"))
		{
			this.eventHandlers.Add(EventNewEraGlobal.Name, new AchievementManager.AchievementEventHandler(this.OnEventNewEraGlobal));
		}
		if (!this.GetAchievement("HERO_CAPTURE"))
		{
			this.eventHandlers.Add(EventHeroCaptured.Name, new AchievementManager.AchievementEventHandler(this.OnEventHeroCaptured));
		}
		if (!this.GetAchievement("PILLAGE_BUILDINGS") || !this.GetAchievement("PILLAGE_DUST_LOOT"))
		{
			this.eventHandlers.Add(EventPillageSucceed.Name, new AchievementManager.AchievementEventHandler(this.OnEventPillageSucceed));
		}
		if (!this.GetAchievement("STEAL_TECHNOLOGIES"))
		{
			this.eventHandlers.Add(EventTechnologyStealed.Name, new AchievementManager.AchievementEventHandler(this.OnEventTechnologyStealed));
		}
		if (!this.GetAchievement("MINOR_FACTIONS_ASSIMILATION"))
		{
			this.eventHandlers.Add(EventFactionAssimilated.Name, new AchievementManager.AchievementEventHandler(this.OnEventFactionAssimilated));
		}
		if (!this.GetAchievement("VILLAGE_PACIFIER"))
		{
			this.eventHandlers.Add(EventVillagePacified.Name, new AchievementManager.AchievementEventHandler(this.OnEventVillagePacified));
		}
		if (!this.GetAchievement("WILD_WALKERS_WIN_WITHOUT_DAMAGE") || !this.GetAchievement("UNITS_KILLER") || !this.GetAchievement("COLOSSI_KILLER") || !this.GetAchievement("KILLED_MIGRATION_UNIT") || !this.GetAchievement("FIGHT_ON_ICE") || !this.GetAchievement("ATTACK_LAND_WITH_SUBMERSIBLE") || !this.GetAchievement("SEA_MONSTER_KILLED") || !this.GetAchievement("SEA_MONSTER_FAILED") || !this.GetAchievement("BATTLE_FOMORIANS_VARIETY") || !this.GetAchievement("CULTISTS_MINIONS_LEGION"))
		{
			this.eventHandlers.Add(EventEncounterStateChange.Name, new AchievementManager.AchievementEventHandler(this.OnEventEncounterStateChange));
		}
		if (!this.GetAchievement("MANY_LEVELED_BOROUGHS"))
		{
			this.eventHandlers.Add(EventDistrictLevelUp.Name, new AchievementManager.AchievementEventHandler(this.OnEventDistrictLevelUp));
		}
		if (!this.GetAchievement("BUILDER") || !this.GetAchievement("ALL_WONDERS"))
		{
			this.eventHandlers.Add(EventConstructionEnded.Name, new AchievementManager.AchievementEventHandler(this.OnEventConstructionEnded));
		}
		if (!this.GetAchievement("CULTISTS_MANY_CONVERTED_VILLAGES"))
		{
			this.eventHandlers.Add(EventVillageConverted.Name, new AchievementManager.AchievementEventHandler(this.OnEventVillageConverted));
		}
		if (!this.GetAchievement("MULTIPLE_CITY_CONQUERED_WINTER") || !this.GetAchievement("SEA_DEMONS_OCEANIC_HUB"))
		{
			this.eventHandlers.Add(EventCityCaptured.Name, new AchievementManager.AchievementEventHandler(this.OnEventCityCaptured));
		}
		if (!this.GetAchievement("ALL_WONDERS") || !this.GetAchievement("CAPTURE_WONDER") || !this.GetAchievement("SEA_DEMONS_OCEANIC_HUB"))
		{
			this.eventHandlers.Add(EventSwapCity.Name, new AchievementManager.AchievementEventHandler(this.OnEventSwapCity));
		}
		if (!this.GetAchievement("SEA_DEMONS_OCEANIC_HUB"))
		{
			this.eventHandlers.Add(EventColonize.Name, new AchievementManager.AchievementEventHandler(this.OnEventColonize));
		}
		if (!this.GetAchievement("MULTIPLE_CITY_CONQUERED_WINTER"))
		{
			this.eventHandlers.Add(EventCityRazed.Name, new AchievementManager.AchievementEventHandler(this.OnEventCityRazed));
		}
		if (!this.GetAchievement("ALL_FORTRESSES"))
		{
			this.eventHandlers.Add(EventFortressesAllOwned.Name, new AchievementManager.AchievementEventHandler(this.OnEventFortressesAllOwned));
		}
		if (!this.GetAchievement("ALL_UNIQUE_FACILITIES"))
		{
			this.eventHandlers.Add(EventUniqueFacilitiesUpdated.Name, new AchievementManager.AchievementEventHandler(this.OnEventUniqueFacilitiesUpdated));
		}
		if (!this.GetAchievement("BLACK_SPOT_USED"))
		{
			this.eventHandlers.Add(EventBlackSpotUsed.Name, new AchievementManager.AchievementEventHandler(this.OnEventBlackSpotUsed));
		}
		if (!this.GetAchievement("OCEAN_CONTROL_WITH_TRADE"))
		{
			this.eventHandlers.Add(EventOceanControlWithTrade.Name, new AchievementManager.AchievementEventHandler(this.OnEventOceanControlWithTrade));
		}
		if (!this.GetAchievement("SEA_DEMONS_OCEANIC_HUB"))
		{
			this.eventHandlers.Add(EventSeaDemonsOceanicHub.Name, new AchievementManager.AchievementEventHandler(this.OnEventSeaDemonsOceanicHub));
		}
		if (!this.GetAchievement("SEA_DEMONS_4_CONTINENTS"))
		{
			this.eventHandlers.Add(EventSeaDemons4Continents.Name, new AchievementManager.AchievementEventHandler(this.OnEventSeaDemons4Continents));
		}
		if (!this.GetAchievement("FOG_BANK_AMBUSH"))
		{
			this.eventHandlers.Add(EventFogBankAmbush.Name, new AchievementManager.AchievementEventHandler(this.OnEventFogBankAmbush));
		}
		if (!this.GetAchievement("FOMORIAN_FORTRESS_CAPTURE"))
		{
			this.eventHandlers.Add(EventFortressOccupantSwapped.Name, new AchievementManager.AchievementEventHandler(this.OnEventFortressOccupantSwapped));
		}
		if (!this.GetAchievement("TILES_TERRAFORMED_OVERALL") || !this.GetAchievement("ENEMY_EXPLOITATION_TERRAFORMED"))
		{
			this.eventHandlers.Add(EventEmpireWorldTerraformed.Name, new AchievementManager.AchievementEventHandler(this.OnEventWorldTerraformed));
		}
		if (!this.GetAchievement("ALL_GEOMANCY_BUFFS"))
		{
			this.eventHandlers.Add(EventGeomancyBuffApplied.Name, new AchievementManager.AchievementEventHandler(this.OnEventGeomancyBuffApplied));
		}
		if (!this.GetAchievement("MAD_SEASON_TEMPLES_INVESTIGATION"))
		{
			this.eventHandlers.Add(EventDustDepositSearched.Name, new AchievementManager.AchievementEventHandler(this.OnEventDustDepositExplored));
		}
		if (!this.GetAchievement("DUST_STORM_AMBUSH"))
		{
			this.eventHandlers.Add(EventDustStormAmbush.Name, new AchievementManager.AchievementEventHandler(this.OnEventDustStormAmbush));
		}
		if (!this.GetAchievement("ALL_MAP_BOOSTS_BUFFS"))
		{
			this.eventHandlers.Add(EventMapBoostApplied.Name, new AchievementManager.AchievementEventHandler(this.OnEventMapBoostApplied));
		}
		if (!this.GetAchievement("BROKEN_LORDS_DUST_CONSUMER_OVERALL"))
		{
			this.eventHandlers.Add(EventDustConsumptionHPLost.Name, new AchievementManager.AchievementEventHandler(this.OnEventDustConsumptionHPLost));
		}
		if (!this.GetAchievement("VAULTERS_TELEPORT_TO_SIEGING_HERO"))
		{
			this.eventHandlers.Add(EventUnitsTeleported.Name, new AchievementManager.AchievementEventHandler(this.OnEventUnitsTeleported));
		}
		if (!this.GetAchievement("NECROPHAGES_BATTLEBORN_CREATOR"))
		{
			this.eventHandlers.Add(EventBattlebornCreated.Name, new AchievementManager.AchievementEventHandler(this.OnEventBattlebornCreated));
		}
		if (!this.GetAchievement("RAGE_WIZARDS_CRITICAL_DAMAGE"))
		{
			this.eventHandlers.Add(EventBattleDamageDone.Name, new AchievementManager.AchievementEventHandler(this.OnEventBattleDamageDone));
		}
		if (!this.GetAchievement("SPORETASTIC") || !this.GetAchievement("CREEPING_SPREE"))
		{
			this.eventHandlers.Add(EventCreepingNodeUpgradeComplete.Name, new AchievementManager.AchievementEventHandler(this.OnEventCreepingNodeComplete));
		}
		if (!this.GetAchievement("MONSTER_WRANGLER"))
		{
			this.eventHandlers.Add(EventKaijuTamed.Name, new AchievementManager.AchievementEventHandler(this.OnEventTameKaiju));
		}
		if (!this.GetAchievement("RESISTANCE_IS_FUTILE"))
		{
			this.eventHandlers.Add(EventFactionIntegrated.Name, new AchievementManager.AchievementEventHandler(this.OnEventFactionIntegrated));
		}
		if (!this.GetAchievement("SHADOW_OF_THE_COLOSSI"))
		{
			this.eventHandlers.Add(EventCityAddRegionalEffects.Name, new AchievementManager.AchievementEventHandler(this.OnEventCityAddRegionalEffects));
		}
		if (!this.GetAchievement("SHAKE_YOUR_FUNDATIONS"))
		{
			this.eventHandlers.Add(EventCityDamagedByEarthquake.Name, new AchievementManager.AchievementEventHandler(this.OnEventCityDamagedByKaijuEarthquake));
		}
		if (!this.GetAchievement("CATCH_AND_RELEASE"))
		{
			this.eventHandlers.Add(EventKaijuLost.Name, new AchievementManager.AchievementEventHandler(this.OnEventKaijuLost));
		}
	}

	private void UnhookEmpireEvents()
	{
		if (this.departmentOfTheInterior != null)
		{
			this.departmentOfTheInterior.CitiesCollectionChanged -= this.DepartmentOfTheInterior_CitiesCollectionChanged;
		}
		if (this.departmentOfTheTreasury != null)
		{
			this.departmentOfTheTreasury.ResourcePropertyChange -= this.DepartmentOfTheTreasury_ResourcePropertyChange;
		}
		if (this.departmentOfTransportation != null)
		{
			this.departmentOfTransportation.ArmyTeleportedToCity -= this.DepartmentOfTransportation_ArmyTeleportedToCity;
		}
	}

	public void Disable(bool disable)
	{
		base.IsDisabled = disable;
	}

	private IGameService gameService;

	private ISessionService sessionService;

	private IPlayerControllerRepositoryService playerControllerRepositoryService;

	private IEventService eventService;

	private ITradeManagementService tradeManagementService;

	private ISeasonService seasonService;

	private ICommandService commandService;

	private IAnalyticsService analyticsService;

	private global::Game game;

	private List<string> orderedDifficulties;

	private bool alreadyWon;

	private bool[] fisBoosterProduced = new bool[3];

	private string[] majorFactionNames;

	private StaticString[] strategicResourceNames;

	private string[] luxuryBoosterNames;

	private int[] empirePlansLevels;

	private GridMap<short> explorationMap;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private DepartmentOfDefense departmentOfDefense;

	private DepartmentOfEducation departmentOfEducation;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private DepartmentOfPlanificationAndDevelopment departmentOfPlanificationAndDevelopment;

	private DepartmentOfInternalAffairs departmentOfInternalAffairs;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private DepartmentOfScience departmentOfScience;

	private DepartmentOfTransportation departmentOfTransportation;

	private DepartmentOfIntelligence departmentOfIntelligence;

	private DepartmentOfCreepingNodes departmentOfCreepingNodes;

	private string activeFactionName;

	private PillarManager pillarManager;

	private Dictionary<City, int> unitsTeleportedThisTurn;

	private Dictionary<Encounter, List<KeyValuePair<EncounterUnit, float>>> encounters;

	private Dictionary<StaticString, AchievementManager.AchievementEventHandler> eventHandlers;

	private List<GameEntityGUID> startedOnIceEncounterGUIDs;

	private int uniqueFacilitiesCount;

	public static StaticString BattlebornDesignName = "BoosterBattlebornHeatWave";

	private delegate void AchievementEventHandler(Amplitude.Unity.Event.Event raisedEvent);
}
