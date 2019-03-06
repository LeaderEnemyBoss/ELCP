using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Audio;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Input;
using Amplitude.Unity.Session;
using Amplitude.Unity.View;
using UnityEngine;

public class EndTurnPanel : GuiPlayerControllerPanel
{
	public bool UserBattleLock
	{
		get
		{
			return this.userBatteLock;
		}
		set
		{
			if (this.userBatteLock != value)
			{
				this.userBatteLock = value;
				base.NeedRefresh = true;
			}
		}
	}

	[Ancillary]
	private IEncounterRepositoryService EncounterRepositoryService
	{
		get
		{
			return this.encounterRepositoryService;
		}
		set
		{
			if (this.encounterRepositoryService != null)
			{
				this.encounterRepositoryService.OneEncounterContenderCollectionChange -= this.EncounterRepositoryService_OneEncounterContenderCollectionChange;
				this.encounterRepositoryService.OneEncounterStateChange -= this.EncounterRepositoryService_OneEncounterStateChange;
			}
			this.encounterRepositoryService = value;
			if (this.encounterRepositoryService != null)
			{
				this.encounterRepositoryService.OneEncounterContenderCollectionChange += this.EncounterRepositoryService_OneEncounterContenderCollectionChange;
				this.encounterRepositoryService.OneEncounterStateChange += this.EncounterRepositoryService_OneEncounterStateChange;
			}
		}
	}

	[Service]
	private IEndTurnService EndTurnService
	{
		get
		{
			return this.endTurnService;
		}
		set
		{
			if (this.endTurnService != null)
			{
				this.endTurnService.GameClientStateChange -= this.EndTurnService_GameClientStateChange;
				this.endTurnService.EndTurnTimeChanged -= this.EndTurnService_EndTurnTimeChanged;
			}
			this.endTurnService = value;
			if (this.endTurnService != null)
			{
				this.endTurnService.GameClientStateChange += this.EndTurnService_GameClientStateChange;
				this.endTurnService.EndTurnTimeChanged += this.EndTurnService_EndTurnTimeChanged;
			}
		}
	}

	[Service]
	private ISeasonService SeasonService
	{
		get
		{
			return this.seasonService;
		}
		set
		{
			if (this.seasonService != null)
			{
				this.seasonService.SeasonChange -= this.SeasonService_SeasonChange;
			}
			this.seasonService = value;
			if (this.seasonService != null)
			{
				this.seasonService.SeasonChange += this.SeasonService_SeasonChange;
			}
		}
	}

	[Service]
	private IPlayerRepositoryService PlayerRepositoryService
	{
		get
		{
			return this.playerRepositoryService;
		}
		set
		{
			if (this.playerRepositoryService != null)
			{
				this.playerRepositoryService.OnChange -= this.PlayerRepositoryService_OnChange;
			}
			this.playerRepositoryService = value;
			if (this.playerRepositoryService != null)
			{
				this.playerRepositoryService.OnChange += this.PlayerRepositoryService_OnChange;
			}
		}
	}

	private IDownloadableContentService DownloadableContentService
	{
		get
		{
			return this.downloadableContentService;
		}
		set
		{
			this.downloadableContentService = value;
		}
	}

	private DepartmentOfTheTreasury DepartmentOfTheTreasury
	{
		get
		{
			return this.departmentOfTheTreasury;
		}
		set
		{
			if (this.departmentOfTheTreasury != null)
			{
				this.departmentOfTheTreasury.ResourcePropertyChange -= this.DepartmentOfTheTreasury_ResourcePropertyChange;
			}
			this.departmentOfTheTreasury = value;
			if (this.departmentOfTheTreasury != null)
			{
				this.departmentOfTheTreasury.ResourcePropertyChange += this.DepartmentOfTheTreasury_ResourcePropertyChange;
			}
		}
	}

	private global::Session Session { get; set; }

	public override void Bind(global::Empire empire)
	{
		base.Bind(empire);
		this.keyMapperService = Services.GetService<IKeyMappingService>();
		Diagnostics.Assert(this.keyMapperService != null);
		if (base.Empire != null)
		{
			this.DepartmentOfTheTreasury = base.Empire.GetAgency<DepartmentOfTheTreasury>();
			base.Empire.GetAgency<DepartmentOfDefense>().ArmyActionStateChange += this.DepartmentOfDefense_ArmyActionStateChange;
			base.Empire.GetAgency<DepartmentOfDefense>().ArmiesCollectionChange += this.DepartmentOfDefense_ArmiesCollectionChange;
			base.Empire.GetAgency<DepartmentOfTransportation>().ArmyPositionChange += this.DepartmentOfTransportation_ArmyPositionChange;
			base.Empire.GetAgency<DepartmentOfForeignAffairs>().DiplomaticRelationStateChange += this.DepartmentOfForeignAffairs_DiplomaticRelationStateChange;
		}
		base.NeedRefresh = true;
	}

	public void CheckShortcut()
	{
		if (Amplitude.Unity.Gui.GuiModalPanel.GuiModalManager.CurrentModalPanel == null && (AgeManager.Instance.FocusedControl == null || (AgeManager.Instance.FocusedControl != null && !AgeManager.Instance.FocusedControl.IsKeyExclusive)) && this.keyMapperService.GetKeyDown(KeyAction.ControlBindingsEndOfTurn))
		{
			if (this.ApplyPlannedMovementsButton.AgeTransform.Enable)
			{
				this.OnApplyPlannedMovementsCB(this.ApplyPlannedMovementsButton.gameObject);
			}
			else if (this.EndTurnButton.AgeTransform.Enable)
			{
				this.OnEndTurnCB(this.EndTurnButton.gameObject);
			}
		}
	}

	public void OnHideAltarOfAurigaScreen()
	{
	}

	public void OnShowAltarOfAurigaScreen()
	{
	}

	public override void Unbind()
	{
		base.Unbind();
		if (base.Empire != null)
		{
			base.Empire.GetAgency<DepartmentOfDefense>().ArmyActionStateChange -= this.DepartmentOfDefense_ArmyActionStateChange;
			base.Empire.GetAgency<DepartmentOfDefense>().ArmiesCollectionChange -= this.DepartmentOfDefense_ArmiesCollectionChange;
			base.Empire.GetAgency<DepartmentOfTransportation>().ArmyPositionChange -= this.DepartmentOfTransportation_ArmyPositionChange;
			base.Empire.GetAgency<DepartmentOfForeignAffairs>().DiplomaticRelationStateChange -= this.DepartmentOfForeignAffairs_DiplomaticRelationStateChange;
		}
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		if (this.EndTurnService == null)
		{
			return;
		}
		this.TurnNumber.Text = AgeLocalizer.Instance.LocalizeString("%TurnFormat");
		this.TurnNumber.Text = this.TurnNumber.Text.Replace("$Number", (this.EndTurnService.Turn + 1).ToString());
		bool flag = false;
		if (Amplitude.Unity.Framework.Application.Version == global::Application.Versions.AlphaVip && this.EndTurnService.Turn >= 199)
		{
			this.EndTurnButton.AgeTransform.Enable = false;
			this.ComputingFeedback.Alpha = 0f;
			this.statusText = "%AlphaVipTurnLimitReached";
			this.AgeModifierAlpha.AnimateToAlpha(0.7f);
		}
		else if (this.UserBattleLock)
		{
			this.EndTurnButton.AgeTransform.Enable = false;
			this.ComputingFeedback.Alpha = 0f;
			this.statusText = "%BattleModeTitle";
			this.AgeModifierAlpha.AnimateToAlpha(0.7f);
		}
		else
		{
			this.EndTurnButton.AgeTransform.Enable = this.EndTurnService.CanEndTurn();
			this.ComputingFeedback.Alpha = ((!this.EndTurnButton.AgeTransform.Enable) ? 1f : 0f);
			if (this.EndTurnButton.AgeTransform.Enable)
			{
				this.AgeModifierAlpha.AnimateToAlpha(1f);
				if (this.Session.SessionMode != SessionMode.Single && this.IsTheLastEmpireToPlay(base.Empire as MajorEmpire))
				{
					flag = true;
					this.statusText = "%WaitingForYouTitle";
				}
				else
				{
					this.statusText = "%EndTurnTitle";
				}
			}
			else
			{
				this.AgeModifierAlpha.AnimateToAlpha(0.7f);
				bool flag2 = false;
				if (this.Session.SessionMode != SessionMode.Single)
				{
					for (int i = 0; i < this.mainEmpires.Count; i++)
					{
						MajorEmpire majorEmpire = this.mainEmpires[i] as MajorEmpire;
						for (int j = 0; j < majorEmpire.Players.Count; j++)
						{
							Player player = majorEmpire.Players[j];
							if (player.Type != PlayerType.AI && player.State != PlayerState.Ready)
							{
								flag2 = true;
								j = majorEmpire.Players.Count;
								i = this.mainEmpires.Count;
							}
						}
					}
				}
				int num = 0;
				if (this.EncounterRepositoryService != null)
				{
					num = this.EncounterRepositoryService.Count((Encounter match) => match.EncounterState != EncounterState.BattleHasEnded);
				}
				if (num > 1)
				{
					this.statusText = AgeLocalizer.Instance.LocalizeString("%RemainingBattleFormatPlural").Replace("$Number", num.ToString());
				}
				else if (num > 0)
				{
					this.statusText = AgeLocalizer.Instance.LocalizeString("%RemainingBattleFormatSingle").Replace("$Number", num.ToString());
				}
				else if (flag2)
				{
					this.statusText = "%WaitingForOthersTitle";
				}
				else
				{
					this.statusText = "%ComputingTurnTitle";
				}
			}
		}
		this.StatusLabelLarge.Text = this.statusText;
		if (this.StatusLabelLarge.TextLines.Count > 2)
		{
			this.StatusLabelSmall.Text = this.statusText;
			this.StatusLabelSmall.AgeTransform.Alpha = 1f;
			this.StatusLabelLarge.AgeTransform.Alpha = 0f;
			if (flag && !this.StatusLabelSmall.AgeTransform.ModifiersRunning)
			{
				this.StatusLabelSmall.AgeTransform.StartAllModifiers(true, false);
			}
			else
			{
				this.StatusLabelSmall.AgeTransform.ResetAllModifiers(true, false);
			}
		}
		else
		{
			this.StatusLabelSmall.AgeTransform.Alpha = 0f;
			this.StatusLabelLarge.AgeTransform.Alpha = 1f;
			if (flag && !this.StatusLabelLarge.AgeTransform.ModifiersRunning)
			{
				this.StatusLabelLarge.AgeTransform.StartAllModifiers(true, false);
			}
			else
			{
				this.StatusLabelLarge.AgeTransform.ResetAllModifiers(true, false);
			}
		}
		if (TutorialManager.IsActivated)
		{
			IGameService service = Services.GetService<IGameService>();
			if (service != null && service.Game != null && service.Game is global::Game)
			{
				ITutorialService service2 = service.Game.Services.GetService<ITutorialService>();
				if (service2 != null)
				{
					this.EndTurnButton.AgeTransform.Enable &= service2.GetValue<bool>(TutorialManager.EnableEndTurnKey, true);
				}
			}
		}
		this.RefreshSeason();
		this.RefreshOrbs();
		this.RefreshEmpirePlayersStatuses();
		this.RefreshApplyPlannedMovementsButton();
		this.RefreshNextIdleArmyButton();
	}

	public void ShowGrid(bool visible)
	{
		this.GridToggle.State = visible;
		global::GameManager.Preferences.GameplayGraphicOptions.DisplayHexaGrid = this.GridToggle.State;
	}

	public void ShowFids(bool visible)
	{
		this.FidsToggle.State = visible;
		global::GameManager.Preferences.GameplayGraphicOptions.DisplayFidsEverywhere = this.FidsToggle.State;
		if (TutorialManager.IsActivated && global::GameManager.Preferences.GameplayGraphicOptions.DisplayFidsEverywhere)
		{
			IEventService service = Services.GetService<IEventService>();
			Diagnostics.Assert(service != null);
			IGameService service2 = Services.GetService<IGameService>();
			Diagnostics.Assert(service2 != null);
			global::Game x = service2.Game as global::Game;
			Diagnostics.Assert(x != null);
			service.Notify(new EventTutorialFIDSDisplayed());
		}
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		this.FocusCircle.Visible = false;
		this.ResetEndTurnTimer();
		if (!this.doUpdateOrbCollectFeedback && this.updateOrbCollectFeedbackCoroutine == null)
		{
			this.doUpdateOrbCollectFeedback = true;
			this.orbAmountsCollectedQueue = new List<float>();
			this.OrbCollectFeedback.ResetAllModifiers(true, false);
			this.OrbCollectFeedback.Alpha = 0f;
			this.updateOrbCollectFeedbackCoroutine = base.StartCoroutine(this.UpdateOrbCollectFeedback());
		}
		base.NeedRefresh = true;
		yield break;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		base.NeedRefresh = false;
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		this.UserBattleLock = false;
		this.ShowGrid(false);
		this.ShowFids(false);
		this.EndTurnService = Services.GetService<IEndTurnService>();
		this.EncounterRepositoryService = base.Game.Services.GetService<IEncounterRepositoryService>();
		this.SeasonService = base.Game.Services.GetService<ISeasonService>();
		this.PlayerRepositoryService = base.Game.Services.GetService<IPlayerRepositoryService>();
		this.DownloadableContentService = Services.GetService<IDownloadableContentService>();
		this.worldPositionningService = base.Game.Services.GetService<IWorldPositionningService>();
		ISessionService sessionService = Services.GetService<ISessionService>();
		this.Session = (sessionService.Session as global::Session);
		global::Game game = base.GameService.Game as global::Game;
		this.mainEmpires.Clear();
		for (int i = 0; i < game.Empires.Length; i++)
		{
			if (game.Empires[i] is MajorEmpire)
			{
				this.mainEmpires.Add(game.Empires[i]);
			}
		}
		this.currentAngle = 180f + (float)(this.mainEmpires.Count - 1) * 0.5f * 24f;
		this.PlayersSectorContainer.ReserveChildren(this.mainEmpires.Count, this.PlayerSectorPrefab, "EmpireSector");
		this.PlayersSectorContainer.RefreshChildrenIList<global::Empire>(this.mainEmpires, this.setupEmpireSectorDelegate, true, false);
		this.currentAngle = 180f + (float)(this.mainEmpires.Count - 1) * 0.5f * 24f;
		this.PlayersStatusContainer.ReserveChildren(this.mainEmpires.Count, this.EmpirePlayersStatusItemPrefab, "EmpireStatus");
		this.PlayersStatusContainer.RefreshChildrenIList<global::Empire>(this.mainEmpires, this.setupEmpireStatusDelegate, true, false);
		this.OrbCounterGroup.Visible = false;
		this.OrbCounterButton.AgeTransform.Visible = false;
		if (this.DownloadableContentService != null && this.DownloadableContentService.IsShared(DownloadableContent13.ReadOnlyName))
		{
			this.OrbCounterGroup.Visible = true;
			this.OrbCounterButton.AgeTransform.Visible = true;
		}
		yield break;
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.previousArmy = null;
		this.doUpdateTimer = false;
		this.updateEndTurnTimerCoroutine = null;
		this.doUpdateOrbCollectFeedback = false;
		this.updateOrbCollectFeedbackCoroutine = null;
		this.PlayersSectorContainer.DestroyAllChildren();
		List<EmpirePlayersStatusItem> children = this.PlayersSectorContainer.GetChildren<EmpirePlayersStatusItem>(true);
		for (int i = 0; i < children.Count; i++)
		{
			children[i].UnsetContent();
		}
		this.PlayersStatusContainer.DestroyAllChildren();
		this.mainEmpires.Clear();
		this.Session = null;
		this.SeasonService = null;
		this.EndTurnService = null;
		this.EncounterRepositoryService = null;
		this.PlayerRepositoryService = null;
		base.OnUnloadGame(game);
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		base.UseRefreshLoop = true;
		this.setupEmpireStatusDelegate = new AgeTransform.RefreshTableItem<global::Empire>(this.SetupEmpireStatus);
		this.setupEmpireSectorDelegate = new AgeTransform.RefreshTableItem<global::Empire>(this.SetupEmpireSector);
		this.mainEmpires = new List<global::Empire>();
		yield break;
	}

	protected override void OnUnload()
	{
		this.mainEmpires.Clear();
		this.mainEmpires = null;
		this.setupEmpireStatusDelegate = null;
		this.setupEmpireSectorDelegate = null;
		this.orbAmountsCollectedQueue = null;
		base.OnUnload();
	}

	private void DepartmentOfDefense_ArmyActionStateChange(object sender, EventArgs e)
	{
		this.RefreshNextIdleArmyButton();
	}

	private void DepartmentOfDefense_ArmiesCollectionChange(object sender, CollectionChangeEventArgs e)
	{
		base.NeedRefresh = true;
	}

	private void DepartmentOfTransportation_ArmyPositionChange(object sender, ArmyMoveEndedEventArgs e)
	{
		this.RefreshApplyPlannedMovementsButton();
		this.RefreshNextIdleArmyButton();
	}

	private void DepartmentOfForeignAffairs_DiplomaticRelationStateChange(object sender, DiplomaticRelationStateChangeEventArgs e)
	{
		base.NeedRefresh = true;
	}

	private void DepartmentOfTheTreasury_ResourcePropertyChange(object sender, ResourcePropertyChangeEventArgs e)
	{
		if (e.Location != base.Empire.SimulationObject)
		{
			return;
		}
		if (e.ResourcePropertyName == SimulationProperties.OrbStock && e.Amount != 0f)
		{
			base.NeedRefresh = true;
			this.orbAmountsCollectedQueue.Add(e.Amount);
		}
	}

	private void EncounterRepositoryService_OneEncounterContenderCollectionChange(object sender, ContenderCollectionChangeEventArgs e)
	{
		if (e.Action == ContenderCollectionChangeAction.Add || e.Action == ContenderCollectionChangeAction.Remove || e.Action == ContenderCollectionChangeAction.Clear)
		{
			foreach (Player player in this.PlayerRepositoryService)
			{
				Player player2 = player;
				PlayerHelper.ComputePlayerState(ref player2);
			}
			base.NeedRefresh = true;
		}
	}

	private void EncounterRepositoryService_OneEncounterStateChange(object sender, EncounterStateChangeEventArgs e)
	{
		if (e.EncounterState == EncounterState.BattleHasEnded)
		{
			foreach (Player player in this.PlayerRepositoryService)
			{
				Player player2 = player;
				PlayerHelper.ComputePlayerState(ref player2);
			}
			base.NeedRefresh = true;
		}
	}

	private void EndTurnService_GameClientStateChange(object sender, GameClientStateChangeEventArgs e)
	{
		string name = e.GameClientStateType.Name;
		if (name != null)
		{
			if (EndTurnPanel.<>f__switch$map1D == null)
			{
				EndTurnPanel.<>f__switch$map1D = new Dictionary<string, int>(2)
				{
					{
						"GameClientState_Turn_Main",
						0
					},
					{
						"GameClientState_Turn_End",
						1
					}
				};
			}
			int num;
			if (EndTurnPanel.<>f__switch$map1D.TryGetValue(name, out num))
			{
				if (num != 0)
				{
					if (num == 1)
					{
						this.EndTurnTimerContainer.Visible = false;
					}
				}
				else
				{
					this.FocusCircle.Visible = true;
					this.FocusCircle.StartAllModifiers(true, false);
					base.StartCoroutine(this.FadeCircle());
				}
			}
		}
		base.NeedRefresh = true;
	}

	private void EndTurnService_EndTurnTimeChanged(object sender, EndTurnTimeChangedEventArgs e)
	{
		this.ResetEndTurnTimer();
	}

	private void PlayerRepositoryService_OnChange(object sender, PlayerRepositoryChangeEventArgs e)
	{
		switch (e.Action)
		{
		case PlayerRepositoryChangeAction.Change:
		{
			Player player = e.Player;
			PlayerState state = player.State;
			PlayerHelper.ComputePlayerState(ref player);
			base.NeedRefresh = true;
			if (player.Empire != base.Empire && state != PlayerState.Ready && player.State == PlayerState.Ready)
			{
				ISessionService service = Services.GetService<ISessionService>();
				float lobbyData = service.Session.GetLobbyData<float>("EndTurnTimerLastPlayer", 0f);
				if (lobbyData > 0f && this.IsTheLastEmpireToPlay(base.Empire as MajorEmpire))
				{
					IAudioEventService service2 = Services.GetService<IAudioEventService>();
					service2.Play2DEvent("Gui/Interface/InGame/LastPlayerEndTurn");
				}
				else
				{
					List<EmpirePlayersStatusItem> children = this.PlayersStatusContainer.GetChildren<EmpirePlayersStatusItem>(true);
					for (int i = 0; i < children.Count; i++)
					{
						if (children[i].MajorEmpire.Players.Contains(player))
						{
							children[i].HighlightReady();
						}
					}
				}
			}
			break;
		}
		}
	}

	private void SeasonService_SeasonChange(object sender, SeasonChangeEventArgs e)
	{
		base.NeedRefresh = true;
	}

	private void OnApplyPlannedMovementsCB(GameObject obj)
	{
		IEnumerable<Army> armiesWithPlannedMovements = this.GetArmiesWithPlannedMovements();
		foreach (Army army in armiesWithPlannedMovements)
		{
			OrderContinueGoToInstruction order = new OrderContinueGoToInstruction(army.Empire.Index, army.GUID);
			base.PlayerController.PostOrder(order);
		}
	}

	private void OnEndTurnCB(GameObject obj)
	{
		this.EndTurnService.TryToEndTurn();
	}

	private void OnPreviousArmy()
	{
		ReadOnlyCollection<Army> armies = base.Empire.GetAgency<DepartmentOfDefense>().Armies;
		this.OnCycleArmy(armies, -1);
	}

	private void OnNextArmy()
	{
		ReadOnlyCollection<Army> armies = base.Empire.GetAgency<DepartmentOfDefense>().Armies;
		this.OnCycleArmy(armies, 1);
	}

	private void OnCycleArmy(IList<Army> armies, int direction = 1)
	{
		if (armies != null && armies.Count > 0)
		{
			Army army = null;
			Army army2 = null;
			ICursorService service = Services.GetService<ICursorService>();
			if (service != null && service.CurrentCursor is ArmyWorldCursor)
			{
				ArmyWorldCursor armyWorldCursor = service.CurrentCursor as ArmyWorldCursor;
				if (armyWorldCursor.Army != null && armyWorldCursor.Army.Units != null)
				{
					army2 = armyWorldCursor.Army;
				}
			}
			if (army2 != null)
			{
				army = army2;
			}
			else if (this.previousArmy != null && armies.Contains(this.previousArmy))
			{
				army = this.previousArmy;
			}
			Army army3;
			if (army == null)
			{
				army3 = armies.ElementAt(0);
			}
			else
			{
				army3 = armies.ElementAt((armies.IndexOf(army) + armies.Count + direction) % armies.Count);
			}
			this.previousArmy = army;
			if (army3 != null)
			{
				global::IViewService service2 = Services.GetService<global::IViewService>();
				if (service2 != null)
				{
					service2.SelectAndCenter(army3, true);
				}
			}
		}
	}

	private void OnPreviousIdleArmyCB(GameObject obj)
	{
		List<Army> armies = this.GetIdleArmies().ToList<Army>();
		this.OnCycleArmy(armies, -1);
	}

	private void OnNextIdleArmyCB(GameObject obj)
	{
		List<Army> armies = this.GetIdleArmies().ToList<Army>();
		this.OnCycleArmy(armies, 1);
	}

	private void OnFIDSSwitchCB(GameObject obj)
	{
		AgeControlToggle component = obj.GetComponent<AgeControlToggle>();
		this.ShowFids(component.State);
	}

	private void OnGameMenuCB(GameObject control)
	{
		base.GuiService.Hide(typeof(CurrentQuestPanel));
		base.GuiService.GetGuiPanel<GamePauseScreen>().Show(new object[0]);
	}

	private void OnGridSwitchCB(GameObject obj)
	{
		AgeControlToggle component = obj.GetComponent<AgeControlToggle>();
		this.ShowGrid(component.State);
	}

	private void OnToggleAltarOfAurigaCB(GameObject obj)
	{
		if (base.GuiService.GetGuiPanel<GameAltarOfAurigaScreen>().IsVisible)
		{
			base.GuiService.GetGuiPanel<GameWorldScreen>().Show(new object[0]);
		}
		else
		{
			base.GuiService.Hide(typeof(CurrentQuestPanel));
			base.GuiService.GetGuiPanel<GameAltarOfAurigaScreen>().Show(new object[0]);
		}
	}

	private bool IsTheLastEmpireToPlay(MajorEmpire majorEmpire)
	{
		foreach (Player player in this.PlayerRepositoryService)
		{
			if (!majorEmpire.Players.Contains(player) && player.Type != PlayerType.AI && player.State != PlayerState.Ready)
			{
				return false;
			}
		}
		for (int i = 0; i < majorEmpire.Players.Count; i++)
		{
			if (majorEmpire.Players[i].State != PlayerState.Ready)
			{
				return true;
			}
		}
		return false;
	}

	private void RefreshApplyPlannedMovementsButton()
	{
		int num = this.GetArmiesWithPlannedMovements().Count<Army>();
		this.ApplyPlannedMovementsButton.AgeTransform.Enable = (num > 0);
		if (num > 0)
		{
			this.ApplyPlannedMovementsButton.AgeTransform.AgeTooltip.Content = "%EndTurnApplyPlannedMovementsDescription";
		}
		else
		{
			this.ApplyPlannedMovementsButton.AgeTransform.AgeTooltip.Content = "%EndTurnNoPlannedMovementsDescription";
		}
	}

	private void RefreshOrbs()
	{
		if (this.DownloadableContentService != null && this.DownloadableContentService.IsShared(DownloadableContent13.ReadOnlyName) && this.DepartmentOfTheTreasury != null)
		{
			float stockValue;
			if (!this.DepartmentOfTheTreasury.TryGetResourceStockValue(base.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.Orb, out stockValue, false))
			{
				Diagnostics.LogError("Can't get resource stock value {0} on simulation object {1}.", new object[]
				{
					DepartmentOfTheTreasury.Resources.Orb,
					base.Empire.SimulationObject.Name
				});
			}
			this.OrbCounterLabel.Text = GuiFormater.FormatStock(stockValue, DepartmentOfTheTreasury.Resources.Orb, 0, true);
		}
	}

	private void RefreshNextIdleArmyButton()
	{
		int num = this.GetIdleArmies().Count<Army>();
		if (num > 0)
		{
			this.NextIdleArmyButton.AgeTransform.Enable = true;
			this.NextIdleArmyButton.AgeTransform.AgeTooltip.Content = "%EndTurnNextIdleArmyDescription";
			this.IdleArmiesCountTransform.Visible = true;
			this.IdleArmiesCountLabel.AgeTransform.Visible = true;
			this.IdleArmiesCountLabel.Text = num.ToString();
		}
		else
		{
			this.NextIdleArmyButton.AgeTransform.Enable = false;
			this.NextIdleArmyButton.AgeTransform.AgeTooltip.Content = "%EndTurnNoIdleArmyDescription";
			this.IdleArmiesCountTransform.Visible = false;
			this.IdleArmiesCountLabel.AgeTransform.Visible = false;
		}
	}

	private void RefreshSeason()
	{
		if (this.SeasonService == null)
		{
			return;
		}
		float propertyValue = base.Empire.GetPropertyValue("MaximumSeasonPredictabilityError");
		this.MadSeasonContainer.Visible = false;
		Season season = this.SeasonService.GetCurrentSeason();
		if (season != null)
		{
			Season nextSeason = this.SeasonService.GetNextSeason(null);
			GuiElement guiElement;
			if (nextSeason.SeasonDefinition.SeasonType == Season.ReadOnlyHeatWave && (base.Empire.SimulationObject.Tags.Contains("FactionTraitFlames7") || base.Empire.SimulationObject.Tags.Contains("FlamesIntegrationDescriptor1")))
			{
				this.MadSeasonContainer.Visible = true;
				string text = (this.SeasonService.GetExactSeasonStartTurn(nextSeason) - this.EndTurnService.Turn).ToString();
				StaticString name = season.SeasonDefinition.Name + nextSeason.SeasonDefinition.SeasonType;
				if (base.GuiService.GuiPanelHelper.TryGetGuiElement(name, out guiElement))
				{
					this.MadSeasonImage.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString(guiElement.Description).Replace("$TurnsToMadSeason", text);
					Texture2D image;
					if (base.GuiService.GuiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Small, out image))
					{
						this.MadSeasonImage.Image = image;
					}
					this.MadSeasonTurns.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString(guiElement.Description).Replace("$TurnsToMadSeason", text);
					this.MadSeasonTurns.Text = text;
				}
			}
			if (season.SeasonDefinition.SeasonType == Season.ReadOnlyHeatWave)
			{
				if (base.Empire.SimulationObject.Tags.Contains("FactionTraitFlames7") || base.Empire.SimulationObject.Tags.Contains("FlamesIntegrationDescriptor1"))
				{
					this.MadSeasonContainer.Visible = true;
					if (base.GuiService.GuiPanelHelper.TryGetGuiElement(season.SeasonDefinition.Name, out guiElement))
					{
						this.MadSeasonImage.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString(guiElement.Description).Replace("$TurnsToExitMadSeason", (season.EndTurn - this.EndTurnService.Turn).ToString());
						Texture2D image2;
						if (base.GuiService.GuiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Small, out image2))
						{
							this.MadSeasonImage.Image = image2;
						}
						this.MadSeasonTurns.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString(guiElement.Description).Replace("$TurnsToExitMadSeason", (season.EndTurn - this.EndTurnService.Turn).ToString());
						this.MadSeasonTurns.Text = (season.EndTurn - this.EndTurnService.Turn).ToString();
					}
				}
				season = this.SeasonService.GetNextSeason(null);
			}
			this.CurrentSeasonImage.AgeTransform.Visible = true;
			if (base.GuiService.GuiPanelHelper.TryGetGuiElement(season.SeasonDefinition.Name, out guiElement))
			{
				this.CurrentSeasonImage.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString(guiElement.Description).Replace("$PastWintersValue", base.Empire.GetPropertyValue("NumberOfPastWinters").ToString());
				Texture2D image3;
				if (base.GuiService.GuiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Small, out image3))
				{
					this.CurrentSeasonImage.Image = image3;
				}
			}
		}
		else
		{
			this.CurrentSeasonImage.AgeTransform.Visible = false;
		}
		season = this.SeasonService.GetNextPredictedSeason(base.Empire);
		if (season != null)
		{
			this.CurrentSeasonImage.AgeTransform.Visible = true;
			this.NextSeasonTurns.AgeTransform.Visible = true;
			GuiElement guiElement;
			if (base.GuiService.GuiPanelHelper.TryGetGuiElement(season.SeasonDefinition.Name, out guiElement))
			{
				if (propertyValue > 0f)
				{
					this.NextSeasonImage.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString(guiElement.Description + "Future").Replace("$PastWintersValue", base.Empire.GetPropertyValue("NumberOfPastWinters").ToString());
				}
				else
				{
					this.NextSeasonImage.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString(guiElement.Description + "FutureNoSeasonPredictabilityError").Replace("$PastWintersValue", base.Empire.GetPropertyValue("NumberOfPastWinters").ToString());
				}
				this.NextSeasonTurns.AgeTransform.AgeTooltip.Content = this.NextSeasonImage.AgeTransform.AgeTooltip.Content;
				Texture2D image4;
				if (base.GuiService.GuiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Small, out image4))
				{
					this.NextSeasonImage.Image = image4;
				}
			}
			int estimatedSeasonMinimumStartTurn = this.SeasonService.GetEstimatedSeasonMinimumStartTurn(this.SeasonService.GetNextPredictedSeason(base.PlayerController.Empire as global::Empire), base.PlayerController.Empire as global::Empire);
			int estimatedSeasonMaximumStartTurn = this.SeasonService.GetEstimatedSeasonMaximumStartTurn(this.SeasonService.GetNextPredictedSeason(base.PlayerController.Empire as global::Empire), base.PlayerController.Empire as global::Empire);
			if (estimatedSeasonMinimumStartTurn < 0 || estimatedSeasonMaximumStartTurn <= 0)
			{
				this.NextSeasonTurns.Text = "?";
			}
			else if (propertyValue > 0f)
			{
				this.NextSeasonTurns.Text = ((estimatedSeasonMinimumStartTurn - this.EndTurnService.Turn > 0) ? (estimatedSeasonMinimumStartTurn - this.EndTurnService.Turn) : 1).ToString() + "-" + (estimatedSeasonMaximumStartTurn - this.EndTurnService.Turn).ToString();
			}
			else
			{
				this.NextSeasonTurns.Text = (estimatedSeasonMaximumStartTurn - this.EndTurnService.Turn).ToString();
			}
		}
		else
		{
			this.CurrentSeasonImage.AgeTransform.Visible = false;
			this.NextSeasonTurns.AgeTransform.Visible = false;
		}
		if (this.DownloadableContentService != null && this.DownloadableContentService.IsShared(DownloadableContent13.ReadOnlyName))
		{
			this.OrbCounterSeasonImage.Image = this.CurrentSeasonImage.Image;
			this.OrbCounterSeasonImage.AgeTransform.Visible = this.CurrentSeasonImage.AgeTransform.Visible;
			this.OrbCounterButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%AltarOfAurigaButtonDescription") + "\n \n" + this.CurrentSeasonImage.AgeTransform.AgeTooltip.Content;
		}
	}

	private void RefreshEmpirePlayersStatuses()
	{
		List<EmpirePlayersStatusItem> children = this.PlayersStatusContainer.GetChildren<EmpirePlayersStatusItem>(true);
		for (int i = 0; i < children.Count; i++)
		{
			children[i].RefreshContent();
		}
	}

	private void RefreshGridGraphicStatus()
	{
		global::GameManager.Preferences.GameplayGraphicOptions.DisplayHexaGrid = this.GridToggle.State;
	}

	private void RefreshFIDSGraphicStatus()
	{
		global::GameManager.Preferences.GameplayGraphicOptions.DisplayFidsEverywhere = this.FidsToggle.State;
	}

	private void ResetEndTurnTimer()
	{
		this.endTurnTime = this.EndTurnService.EndTurnTime;
		this.endTurnTimeDuration = this.EndTurnService.EndTurnDuration;
		this.EndTurnTimerContainer.Visible = (this.endTurnTime - global::Session.Time > 0.0);
		if (!this.doUpdateTimer && this.endTurnTimeDuration > 0.0 && this.updateEndTurnTimerCoroutine == null)
		{
			this.doUpdateTimer = true;
			this.updateEndTurnTimerCoroutine = base.StartCoroutine(this.UpdateTimer());
		}
	}

	private void SetupEmpireSector(AgeTransform empireSector, global::Empire empire, int index)
	{
		empireSector.AgePrimitive.TintColor = empire.Color;
		empireSector.Alpha = 0.8f;
		AgePrimitiveSector agePrimitiveSector = empireSector.AgePrimitive as AgePrimitiveSector;
		agePrimitiveSector.MinAngle = this.currentAngle - 10f;
		agePrimitiveSector.MaxAngle = this.currentAngle + 10f;
		this.currentAngle -= 24f;
	}

	private void SetupEmpireStatus(AgeTransform empireSlot, global::Empire empire, int index)
	{
		float num = this.PlayersStatusContainer.Width * 0.5f;
		float num2 = this.PlayersStatusContainer.Width * 0.5f;
		float num3 = this.PlayersStatusContainer.Width * 0.5f - empireSlot.Width * 0.5f - 6f * AgeUtils.CurrentUpscaleFactor();
		num += num3 * Mathf.Cos(0.0174532924f * (90f - this.currentAngle));
		num2 -= num3 * Mathf.Sin(0.0174532924f * (90f - this.currentAngle));
		num -= empireSlot.Width * 0.5f;
		num2 -= empireSlot.Height * 0.5f;
		empireSlot.X = num;
		empireSlot.Y = num2;
		EmpirePlayersStatusItem component = empireSlot.GetComponent<EmpirePlayersStatusItem>();
		component.SetContent(empire as MajorEmpire);
		this.currentAngle -= 24f;
	}

	private IEnumerator UpdateOrbCollectFeedback()
	{
		while (this.doUpdateOrbCollectFeedback)
		{
			if (this.orbAmountsCollectedQueue.Count > 0)
			{
				string orbAmountString = string.Empty;
				if (this.orbAmountsCollectedQueue[0] > 0f)
				{
					orbAmountString += "+";
				}
				orbAmountString += Mathf.Floor(this.orbAmountsCollectedQueue[0]).ToString();
				this.OrbCollectFeedback.GetComponent<AgePrimitiveLabel>().Text = AgeLocalizer.Instance.LocalizeString("%OrbCollectFeedbackTitle").Replace("$OrbAmount", orbAmountString);
				this.OrbCollectFeedback.StartAllModifiers(true, false);
				while (this.OrbCollectFeedback.ModifiersRunning)
				{
					yield return null;
				}
				if (this.orbAmountsCollectedQueue != null && this.orbAmountsCollectedQueue.Count > 0)
				{
					this.orbAmountsCollectedQueue.RemoveAt(0);
				}
				this.OrbCollectFeedback.StartAllModifiers(false, false);
				while (this.OrbCollectFeedback.ModifiersRunning)
				{
					yield return null;
				}
			}
			yield return null;
		}
		this.updateOrbCollectFeedbackCoroutine = null;
		this.orbAmountsCollectedQueue = null;
		yield break;
	}

	private IEnumerator UpdateTimer()
	{
		int previousSecondsLeft = 0;
		while (this.doUpdateTimer)
		{
			if (this.endTurnTime > 0.0 && this.EndTurnTimerProgress != null)
			{
				double delta = this.endTurnTime - global::Session.Time;
				delta = ((delta >= 0.0) ? delta : 0.0);
				Color color = (delta <= 20.0) ? ((delta <= 10.0) ? new Color(1f, 0f, 0f) : new Color(1f, 0.66f, 0f)) : new Color(1f, 1f, 1f);
				this.EndTurnTimerProgress.TintColor = color;
				if (this.endTurnTimeDuration > 0.0)
				{
					this.EndTurnTimerProgress.MaxAngle = 360f * (float)(delta / this.endTurnTimeDuration);
				}
				else
				{
					this.EndTurnTimerProgress.MaxAngle = 360f;
				}
				if (base.PlayerController.GameInterface.CurrentState is GameClientState_Turn_Main)
				{
					int secondsLeft = Mathf.RoundToInt((float)delta);
					if (secondsLeft != previousSecondsLeft && secondsLeft > 0 && secondsLeft < 10)
					{
						previousSecondsLeft = secondsLeft;
						IAudioEventService audioEventService = Services.GetService<IAudioEventService>();
						audioEventService.Play2DEvent("Gui/Interface/Lobby/TimerTick");
					}
				}
			}
			yield return null;
		}
		this.updateEndTurnTimerCoroutine = null;
		yield break;
	}

	private IEnumerable<Army> GetIdleArmies()
	{
		ReadOnlyCollection<Army> armies = base.Empire.GetAgency<DepartmentOfDefense>().Armies;
		Diagnostics.Assert(armies != null);
		IEnumerable<Encounter> encounters = this.EncounterRepositoryService;
		for (int index = 0; index < armies.Count; index++)
		{
			Army army = armies[index];
			if (army != null)
			{
				if (army.IsAbleToMove)
				{
					if (!army.IsGuarding && !army.IsAutoExploring)
					{
						if (!army.SimulationObject.Tags.Contains(DepartmentOfTheInterior.ArmyStatusBesiegerDescriptorName))
						{
							if (!army.PillageTarget.IsValid)
							{
								if (!army.IsDismantlingDevice)
								{
									if (!army.IsDismantlingCreepingNode)
									{
										if (!army.IsAspirating)
										{
											if (!army.IsBombarding)
											{
												if (army.GetPropertyBaseValue(SimulationProperties.Movement) > 0.001f)
												{
													if (!this.worldPositionningService.IsFrozenWaterTile(army.WorldPosition) || !army.IsNavalOrPartiallySo)
													{
														if (!encounters.Any((Encounter encounter) => encounter != null && encounter.EncounterState != EncounterState.BattleHasEnded && encounter.Contenders != null && encounter.Contenders.Any((Contender contender) => contender != null && contender.GUID == army.GUID)))
														{
															yield return army;
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
		yield break;
	}

	private IEnumerable<Army> GetArmiesWithPlannedMovements()
	{
		ReadOnlyCollection<Army> armies = base.Empire.GetAgency<DepartmentOfDefense>().Armies;
		Diagnostics.Assert(armies != null);
		for (int index = 0; index < armies.Count; index++)
		{
			Army army = armies[index];
			if (army != null)
			{
				if (army.IsAbleToMove && !army.IsMoving)
				{
					if (army.GetPropertyBaseValue(SimulationProperties.Movement) > 0.001f)
					{
						if (army.WorldPath != null && !(army.WorldPosition == army.WorldPath.Destination))
						{
							if (!army.IsLocked && !army.IsInEncounter)
							{
								yield return army;
							}
						}
					}
				}
			}
		}
		yield break;
	}

	private IEnumerator FadeCircle()
	{
		while (this.FocusCircle.ModifiersRunning)
		{
			yield return null;
		}
		this.FocusCircle.Visible = false;
		yield break;
	}

	public const float DisableAlpha = 0.7f;

	public const float EmpireStartAngle = 180f;

	public const float EmpireAngleSpread = 24f;

	public const float EmpireSectorWidth = 20f;

	public AgeTransform EndTurnTimerContainer;

	public AgePrimitiveArc EndTurnTimerProgress;

	public AgeTransform PlayersSectorContainer;

	public Transform PlayerSectorPrefab;

	public AgeTransform PlayersStatusContainer;

	public Transform EmpirePlayersStatusItemPrefab;

	public AgeControlButton EndTurnButton;

	public AgePrimitiveLabel StatusLabelLarge;

	public AgePrimitiveLabel StatusLabelSmall;

	public AgePrimitiveLabel TurnNumber;

	public AgeTransform ComputingFeedback;

	public AgeControlToggleRadial GridToggle;

	public AgeControlToggleRadial FidsToggle;

	public AgeModifierAlpha AgeModifierAlpha;

	public AgeTransform FocusCircle;

	public AgePrimitiveImage CurrentSeasonImage;

	public AgePrimitiveImage NextSeasonImage;

	public AgePrimitiveLabel NextSeasonTurns;

	public AgeControlButtonRadial ApplyPlannedMovementsButton;

	public AgeControlButtonRadial NextIdleArmyButton;

	public AgeTransform IdleArmiesCountTransform;

	public AgePrimitiveLabel IdleArmiesCountLabel;

	public AgeTransform OrbCounterGroup;

	public AgeControlButton OrbCounterButton;

	public AgePrimitiveLabel OrbCounterLabel;

	public AgePrimitiveImage OrbCounterSeasonImage;

	public AgeTransform OrbCollectFeedback;

	public AgeTransform MadSeasonContainer;

	public AgePrimitiveImage MadSeasonImage;

	public AgePrimitiveLabel MadSeasonTurns;

	private string statusText = string.Empty;

	private bool userBatteLock;

	private float currentAngle;

	private List<global::Empire> mainEmpires;

	private IEncounterRepositoryService encounterRepositoryService;

	private IEndTurnService endTurnService;

	private ISeasonService seasonService;

	private UnityEngine.Coroutine updateEndTurnTimerCoroutine;

	private IPlayerRepositoryService playerRepositoryService;

	private IDownloadableContentService downloadableContentService;

	private IWorldPositionningService worldPositionningService;

	private AgeTransform.RefreshTableItem<global::Empire> setupEmpireStatusDelegate;

	private AgeTransform.RefreshTableItem<global::Empire> setupEmpireSectorDelegate;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private double endTurnTime;

	private double endTurnTimeDuration;

	private bool doUpdateTimer;

	private Army previousArmy;

	private UnityEngine.Coroutine updateOrbCollectFeedbackCoroutine;

	private bool doUpdateOrbCollectFeedback;

	private List<float> orbAmountsCollectedQueue;

	private IKeyMappingService keyMapperService;
}
