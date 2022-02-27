using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Amplitude.Path;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Simulation;
using UnityEngine;

public class GameMilitaryScreen : GuiPlayerControllerScreen
{
	public GameMilitaryScreen()
	{
		base.EnableCaptureBackgroundOnShow = true;
		base.EnableMenuAudioLayer = true;
		base.EnableNavigation = true;
	}

	public UnitDesign SelectedDesign { get; private set; }

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
			}
			this.endTurnService = value;
			if (this.endTurnService != null)
			{
				this.endTurnService.GameClientStateChange += this.EndTurnService_GameClientStateChange;
			}
		}
	}

	private DepartmentOfDefense DepartmentOfDefense
	{
		get
		{
			return this.departmentOfDefense;
		}
		set
		{
			if (this.departmentOfDefense != null)
			{
				this.departmentOfDefense.AvailableUnitDesignChanged -= this.DepartmentOfDefense_AvailableUnitDesignChanged;
			}
			this.departmentOfDefense = value;
			if (this.departmentOfDefense != null)
			{
				this.departmentOfDefense.AvailableUnitDesignChanged += this.DepartmentOfDefense_AvailableUnitDesignChanged;
			}
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

	private DepartmentOfScience DepartmentOfScience
	{
		get
		{
			return this.departmentOfScience;
		}
		set
		{
			if (this.departmentOfScience != null)
			{
				this.departmentOfScience.TechnologyUnlocked -= this.DepartmentOfScience_TechnologyUnlocked;
			}
			this.departmentOfScience = value;
			if (this.departmentOfScience != null)
			{
				this.departmentOfScience.TechnologyUnlocked += this.DepartmentOfScience_TechnologyUnlocked;
			}
		}
	}

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
				this.encounterRepositoryService.EncounterRepositoryChange -= this.EncounterRepositoryService_EncounterRepositoryChange;
			}
			this.encounterRepositoryService = value;
			if (this.encounterRepositoryService != null)
			{
				this.encounterRepositoryService.EncounterRepositoryChange += this.EncounterRepositoryService_EncounterRepositoryChange;
			}
		}
	}

	public override void Bind(global::Empire empire)
	{
		if (empire == null)
		{
			throw new ArgumentNullException("empire");
		}
		base.Bind(empire);
		this.DepartmentOfTheTreasury = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		this.DepartmentOfDefense = base.Empire.GetAgency<DepartmentOfDefense>();
		this.DepartmentOfScience = base.Empire.GetAgency<DepartmentOfScience>();
		ArmyLine.CurrentArmy = null;
		this.armyListPanel.Bind(empire);
		if (this.temporaryGarrison == null)
		{
			this.temporaryGarrison = new SimulationObjectWrapper(base.GetType().Name + ".Garrison");
			this.temporaryGarrison.SimulationObject.Tags.AddTag("ClassArmy");
			this.temporaryGarrison.SimulationObject.Tags.AddTag("ClassCity");
			this.temporaryGarrison.SimulationObject.Tags.AddTag("Garrison");
			this.temporaryGarrison.SimulationObject.ModifierForward = ModifierForwardType.ChildrenOnly;
		}
		base.Empire.AddChild(this.temporaryGarrison);
		base.NeedRefresh = true;
	}

	public override bool HandleDownRequest()
	{
		return this.armyListPanel.HandleDownRequest();
	}

	public override bool HandleUpRequest()
	{
		return this.armyListPanel.HandleUpRequest();
	}

	public override void RefreshContent()
	{
		if (base.Empire == null)
		{
			return;
		}
		if (ArmyLine.CurrentArmy != null && ArmyLine.CurrentArmy.UnitsCount == 0)
		{
			ArmyLine.CurrentArmy = null;
		}
		this.armyListPanel.RefreshContent();
		this.armyListPanel.EnforceRadio();
		this.RefreshUnitListPanel();
		this.RefreshUnitEquipment();
		this.UnitDesignsTable.Height = 0f;
		ReadOnlyCollection<UnitDesign> userDefinedUnitDesigns = ((IUnitDesignDatabase)this.DepartmentOfDefense).UserDefinedUnitDesigns;
		this.UnitDesignsTable.ReserveChildren(userDefinedUnitDesigns.Count, this.UnitDesignItemPrefab, "Item");
		this.UnitDesignsTable.RefreshChildrenIList<UnitDesign>(userDefinedUnitDesigns, this.unitDesignRefreshDelegate, true, false);
		this.UnitDesignsTable.ArrangeChildren();
		this.UnitDesignsScrollView.OnPositionRecomputed();
		this.SelectedDesign = null;
		this.RefreshUnitDesignEquipment();
		this.RefreshButtons();
	}

	public override void Unbind()
	{
		this.armyListPanel.Unbind();
		ArmyLine.CurrentArmy = null;
		this.DepartmentOfTheTreasury = null;
		this.DepartmentOfDefense = null;
		this.DepartmentOfScience = null;
		if (base.Empire != null && this.temporaryGarrison != null)
		{
			base.Empire.RemoveChild(this.temporaryGarrison);
		}
		if (this.temporaryUnit != null)
		{
			this.temporaryUnit.Dispose();
			this.temporaryUnit = null;
		}
		base.Unbind();
	}

	public void ValidateUnitBodyChoice(UnitBodyDefinition unitBody)
	{
		base.GuiService.GetGuiPanel<UnitDesignModalPanel>().CreateMode = true;
		base.GuiService.GetGuiPanel<UnitDesignModalPanel>().Show(new object[]
		{
			unitBody
		});
	}

	protected override IEnumerator OnHide(bool instant)
	{
		base.NeedRefresh = false;
		this.armyListPanel.Hide(instant);
		this.UnitListPanel.Hide(instant);
		this.HideUnitEquipmentPanel();
		base.GuiService.GetGuiPanel<ControlBanner>().OnHideScreen(GameScreenType.Military);
		this.UnitListPanel.Unbind();
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		base.UseRefreshLoop = true;
		this.unitDesignRefreshDelegate = new AgeTransform.RefreshTableItem<UnitDesign>(this.UnitDesignRefresh);
		this.armyListPanel = Amplitude.Unity.Gui.GuiPanel.Instanciate<ArmyListPanel>(this.ArmyListFrame, this.ArmyListPanelPrefab, null);
		this.unitEquipmentPanel = Amplitude.Unity.Gui.GuiPanel.Instanciate<UnitEquipmentPanel>(this.UnitEquipementFrame, this.UnitEquipmentPanelPrefab, null);
		this.unitEquipmentPanel.ViewPortLayer = "Viewport0";
		this.UnitListPanel.Load();
		yield break;
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		IGameEntityRepositoryService gameEntityRepositoryService = base.Game.Services.GetService<IGameEntityRepositoryService>();
		this.temporaryGuid = gameEntityRepositoryService.GenerateGUID();
		this.EncounterRepositoryService = ((global::Game)base.Game).Services.GetService<IEncounterRepositoryService>();
		this.EndTurnService = Services.GetService<IEndTurnService>();
		yield break;
	}

	protected override void PlayerControllerRepository_ActivePlayerControllerChange(object sender, ActivePlayerControllerChangeEventArgs eventArgs)
	{
		base.PlayerControllerRepository_ActivePlayerControllerChange(sender, eventArgs);
		this.SelectedDesign = null;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		this.interactionsAllowed = base.PlayerController.CanSendOrders();
		base.GuiService.GetGuiPanel<ControlBanner>().OnShowScreen(GameScreenType.Military);
		base.GuiService.Show(typeof(EmpireBannerPanel), new object[]
		{
			EmpireBannerPanel.Full
		});
		this.UnitsTable.DestroyAllChildren();
		this.SelectedDesign = null;
		this.armyListPanel.Show(new object[]
		{
			null,
			base.gameObject
		});
		this.AcademyScreenButton.GetComponent<ButtonAligner>().Align();
		base.NeedRefresh = true;
		yield break;
	}

	protected override void OnUnload()
	{
		if (this.armyListPanel != null)
		{
			this.armyListPanel.Unload();
			UnityEngine.Object.Destroy(this.armyListPanel);
			this.armyListPanel = null;
		}
		if (this.unitEquipmentPanel != null)
		{
			this.unitEquipmentPanel.Unload();
			UnityEngine.Object.Destroy(this.unitEquipmentPanel);
			this.unitEquipmentPanel = null;
		}
		this.unitDesignRefreshDelegate = null;
		base.OnUnload();
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.Hide(true);
		this.Unbind();
		this.UnitsTable.DestroyAllChildren();
		this.UnitDesignsTable.DestroyAllChildren();
		this.SelectedDesign = null;
		this.EndTurnService = null;
		this.EncounterRepositoryService = null;
		ArmyLine.CurrentArmy = null;
		base.OnUnloadGame(game);
	}

	private void EndTurnService_GameClientStateChange(object sender, GameClientStateChangeEventArgs e)
	{
		if (base.IsVisible && base.PlayerController != null)
		{
			bool flag = base.PlayerController.CanSendOrders();
			if (this.interactionsAllowed != flag)
			{
				this.interactionsAllowed = flag;
				this.RefreshButtons();
				if (!flag)
				{
					if (MessagePanel.Instance.IsVisible)
					{
						MessagePanel.Instance.Hide(false);
					}
					ArmyLine selectedArmyLine = this.GetSelectedArmyLine();
					if (selectedArmyLine != null)
					{
						selectedArmyLine.CancelRename();
					}
				}
			}
		}
	}

	private void DepartmentOfDefense_AvailableUnitDesignChanged(object sender, ConstructibleElementEventArgs e)
	{
		base.NeedRefresh = true;
	}

	private void DepartmentOfTheTreasury_ResourcePropertyChange(object sender, ResourcePropertyChangeEventArgs e)
	{
		if (base.IsVisible && e.ResourcePropertyName == SimulationProperties.BankAccount)
		{
			this.RefreshButtons();
		}
	}

	private void DepartmentOfScience_TechnologyUnlocked(object sender, ConstructibleElementEventArgs e)
	{
		if (base.IsVisible)
		{
			this.RefreshButtons();
		}
	}

	private void EncounterRepositoryService_EncounterRepositoryChange(object sender, EncounterRepositoryChangeEventArgs e)
	{
		if (base.IsVisible)
		{
			this.RefreshButtons();
		}
	}

	private List<Unit> GetRetrofitableUnits(ReadOnlyCollection<Unit> candidateUnits)
	{
		GameMilitaryScreen.<GetRetrofitableUnits>c__AnonStoreyA5A <GetRetrofitableUnits>c__AnonStoreyA5A = new GameMilitaryScreen.<GetRetrofitableUnits>c__AnonStoreyA5A();
		<GetRetrofitableUnits>c__AnonStoreyA5A.candidateUnits = candidateUnits;
		List<Unit> list = new List<Unit>();
		int i;
		for (i = 0; i < <GetRetrofitableUnits>c__AnonStoreyA5A.candidateUnits.Count; i++)
		{
			UnitDesign unitDesign = this.DepartmentOfDefense.UnitDesignDatabase.UserDefinedUnitDesigns.FirstOrDefault((UnitDesign design) => design.Model == <GetRetrofitableUnits>c__AnonStoreyA5A.candidateUnits[i].UnitDesign.Model);
			if (unitDesign != null && <GetRetrofitableUnits>c__AnonStoreyA5A.candidateUnits[i].UnitDesign != unitDesign)
			{
				list.Add(<GetRetrofitableUnits>c__AnonStoreyA5A.candidateUnits[i]);
			}
		}
		return list;
	}

	private List<Unit> GetSalableUnits(ReadOnlyCollection<Unit> candidateUnits)
	{
		List<Unit> list = new List<Unit>();
		for (int i = 0; i < candidateUnits.Count; i++)
		{
			if (!candidateUnits[i].CheckUnitAbility(UnitAbility.ReadonlyUnsalable, -1))
			{
				list.Add(candidateUnits[i]);
			}
		}
		return list;
	}

	private void HideUnitEquipmentPanel()
	{
		this.unitEquipmentPanel.Unbind();
		this.unitEquipmentPanel.Hide(false);
		if (this.temporaryUnit != null)
		{
			base.PlayerController.Empire.RemoveChild(this.temporaryUnit);
			this.temporaryUnit.Dispose();
			this.temporaryUnit = null;
		}
	}

	private bool IsSelectedArmyInEncounter()
	{
		if (this.EncounterRepositoryService != null)
		{
			IEnumerable<Encounter> enumerable = this.EncounterRepositoryService;
			if (enumerable != null)
			{
				return enumerable.Any((Encounter encounter) => encounter.IsGarrisonInEncounter(ArmyLine.CurrentArmy.GUID, false));
			}
		}
		return false;
	}

	private ArmyLine GetSelectedArmyLine()
	{
		List<ArmyLine> children = this.armyListPanel.ArmiesTable.GetChildren<ArmyLine>(true);
		for (int i = 0; i < children.Count; i++)
		{
			if (children[i] != null && children[i].SelectionToggle.State)
			{
				return children[i];
			}
		}
		return null;
	}

	private void OnAcademyScreenCB(GameObject obj)
	{
		this.Hide(true);
		GameAcademyScreen guiPanel = base.GuiService.GetGuiPanel<GameAcademyScreen>();
		guiPanel.FromMilitaryScreen = true;
		guiPanel.Show(new object[0]);
	}

	private void OnCloseCB(GameObject obj)
	{
		this.HandleCancelRequest();
	}

	private void OnCreateDesignCB(GameObject control)
	{
		if (this.SelectedDesign != null)
		{
			base.GuiService.GetGuiPanel<UnitBodySelectionModalPanel>().Show(new object[]
			{
				base.gameObject,
				this.SelectedDesign.UnitBodyDefinition
			});
		}
		else
		{
			base.GuiService.GetGuiPanel<UnitBodySelectionModalPanel>().Show(new object[]
			{
				base.gameObject
			});
		}
	}

	private void OnConfirmDeleteDesign(object sender, MessagePanelResultEventArgs e)
	{
		if (e.Result == MessagePanelResult.Ok)
		{
			this.DeleteButton.AgeTransform.Enable = false;
			OrderRemoveUnitDesign order = new OrderRemoveUnitDesign(base.Empire.Index, this.SelectedDesign);
			Ticket ticket;
			base.PlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
			this.SelectedDesign = null;
		}
	}

	private void OnDeleteDesignCB(GameObject control)
	{
		string message = string.Format(AgeLocalizer.Instance.LocalizeString("%MilitaryConfirmDeleteDesignTitle"), this.SelectedDesign.LocalizedName);
		MessagePanel.Instance.Show(message, string.Empty, MessagePanelButtons.OkCancel, new MessagePanel.EventHandler(this.OnConfirmDeleteDesign), MessagePanelType.IMPORTANT, new MessagePanelButton[0]);
	}

	private void OnDoubleClickArmyLine()
	{
		Army currentArmy = ArmyLine.CurrentArmy;
		this.HandleCancelRequest();
		IViewService service = Services.GetService<IViewService>();
		if (service != null)
		{
			service.SelectAndCenter(currentArmy, true);
		}
		this.Hide(false);
	}

	private void OnDoubleClickDesignCB(GameObject control)
	{
		base.GuiService.GetGuiPanel<UnitDesignModalPanel>().CreateMode = false;
		base.GuiService.GetGuiPanel<UnitDesignModalPanel>().Show(new object[]
		{
			this.SelectedDesign
		});
	}

	private void OnEditDesignCB(GameObject control)
	{
		base.GuiService.GetGuiPanel<UnitDesignModalPanel>().CreateMode = false;
		base.GuiService.GetGuiPanel<UnitDesignModalPanel>().Show(new object[]
		{
			this.SelectedDesign
		});
	}

	private void OnOrderResponse(object sender, TicketRaisedEventArgs args)
	{
		base.AgeTransform.Enable = true;
		base.NeedRefresh = true;
	}

	private void OnHealArmyCB(GameObject control)
	{
		if (ArmyLine.CurrentArmy != null)
		{
			List<Unit> list = new List<Unit>(ArmyLine.CurrentArmy.StandardUnits);
			list.RemoveAll((Unit unit) => !unit.CheckUnitAbility(UnitAbility.UnitAbilityInstantHeal, -1));
			list.RemoveAll((Unit unit) => !unit.IsWounded());
			list.RemoveAll((Unit unit) => !unit.HasEnoughActionPointLeft(1));
			if (list.Count > 0)
			{
				OrderHealUnits order = new OrderHealUnits(base.Empire.Index, list);
				Ticket ticket;
				base.PlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
			}
		}
	}

	private void OnForceShiftCB(GameObject obj)
	{
		if (ArmyLine.CurrentArmy != null)
		{
			DepartmentOfScience agency = ArmyLine.CurrentArmy.Empire.GetAgency<DepartmentOfScience>();
			if (agency == null || agency.GetTechnologyState("TechnologyDefinitionOrbUnlock17WinterShifters") != DepartmentOfScience.ConstructibleElement.State.Researched)
			{
				return;
			}
			List<Unit> list = new List<Unit>(ArmyLine.CurrentArmy.StandardUnits);
			list.RemoveAll((Unit unit) => !unit.IsShifter() || !unit.IsInCurrentSeasonForm());
			if (list.Count > 0)
			{
				OrderForceShiftUnits order = new OrderForceShiftUnits(base.Empire.Index, list.ToArray());
				IPlayerControllerRepositoryService service = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
				Ticket ticket;
				service.ActivePlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
				EventForceShift eventToNotify = new EventForceShift(base.Empire, list.Count, true);
				IEventService service2 = Services.GetService<IEventService>();
				if (service2 != null)
				{
					service2.Notify(eventToNotify);
				}
			}
		}
	}

	private void OnRenameArmyCB(GameObject control)
	{
		List<ArmyLine> children = this.armyListPanel.ArmiesTable.GetChildren<ArmyLine>(true);
		for (int i = 0; i < children.Count; i++)
		{
			if (children[i] != null && children[i].SelectionToggle.State)
			{
				children[i].StartRename();
				return;
			}
		}
	}

	private void OnRetrofitArmyCB(GameObject control)
	{
		string message = AgeLocalizer.Instance.LocalizeString("%MilitaryConfirmRetrofitArmyTitle");
		MessagePanel.Instance.Show(message, "%Confirmation", MessagePanelButtons.YesNo, new MessagePanel.EventHandler(this.OnRetrofitArmyConfirmation), MessagePanelType.INFORMATIVE, new MessagePanelButton[0]);
	}

	private void OnRetrofitArmyConfirmation(object sender, MessagePanelResultEventArgs e)
	{
		if (e.Result == MessagePanelResult.Yes)
		{
			this.RetrofitArmy();
		}
	}

	private void OnDisbandArmyCB(GameObject control)
	{
		string message = AgeLocalizer.Instance.LocalizeString("%MilitaryConfirmDisbandArmyTitle");
		MessagePanel.Instance.Show(message, "%Confirmation", MessagePanelButtons.YesNo, new MessagePanel.EventHandler(this.OnSellArmyConfirmation), MessagePanelType.INFORMATIVE, new MessagePanelButton[0]);
	}

	private void OnSellArmyCB(GameObject control)
	{
		string message = AgeLocalizer.Instance.LocalizeString("%MilitaryConfirmSellArmyTitle");
		MessagePanel.Instance.Show(message, "%Confirmation", MessagePanelButtons.YesNo, new MessagePanel.EventHandler(this.OnSellArmyConfirmation), MessagePanelType.INFORMATIVE, new MessagePanelButton[0]);
	}

	private void OnSellArmyConfirmation(object sender, MessagePanelResultEventArgs e)
	{
		if (e.Result == MessagePanelResult.Yes)
		{
			this.SellArmy();
		}
	}

	private void OnHealUnitsCB(GameObject control)
	{
		this.UnitListPanel.OnHealUnitsCB(null);
	}

	private void OnSelectAllUnitsCB(GameObject control)
	{
		this.UnitListPanel.OnSelectAllCB(null);
		this.RefreshUnitEquipment();
	}

	private void OnRetrofitUnitsCB(GameObject control)
	{
		this.UnitListPanel.OnRetrofitUnitsCB(null);
		this.RefreshUnitEquipment();
	}

	private void OnDisbandUnitsCB(GameObject control)
	{
		this.UnitListPanel.OnDisbandUnitsCB(null);
		this.RefreshUnitEquipment();
	}

	private void OnSellUnitsCB(GameObject control)
	{
		this.UnitListPanel.OnSellUnitsCB(null);
		this.RefreshUnitEquipment();
	}

	private void OnToggleArmyLine()
	{
		this.RefreshUnitListPanel();
		this.RefreshButtons();
		if (ArmyLine.CurrentArmy != null && this.SelectedDesign != null)
		{
			this.SelectAllTogglesInTable(this.UnitDesignsTable, false);
			this.SelectedDesign = null;
			this.RefreshUnitDesignEquipment();
		}
	}

	private void OnUnitsChanged()
	{
		base.NeedRefresh = true;
	}

	private void OnUnitDesignToggle(GameObject control)
	{
		this.SelectAllTogglesInTable(this.UnitsTable, false);
		this.SelectedDesign = null;
		AgeControlToggle component = control.GetComponent<AgeControlToggle>();
		List<UnitGuiItem> children = this.UnitDesignsTable.GetChildren<UnitGuiItem>(true);
		for (int i = 0; i < children.Count; i++)
		{
			if (children[i].UnitToggle == component)
			{
				children[i].UnitToggle.State = true;
				this.SelectedDesign = children[i].UnitDesign;
			}
			else
			{
				children[i].UnitToggle.State = false;
			}
		}
		this.RefreshUnitDesignEquipment();
		if (ArmyLine.CurrentArmy != null)
		{
			this.UnitListPanel.RefreshContent();
		}
		this.RefreshButtons();
	}

	private void OnUnitToggle()
	{
		this.SelectAllTogglesInTable(this.UnitDesignsTable, false);
		this.SelectedDesign = null;
		this.RefreshUnitEquipment();
		this.RefreshButtons();
	}

	private void RefreshButtons()
	{
		this.ArmyRenameButton.AgeTransform.Enable = (ArmyLine.CurrentArmy != null && this.interactionsAllowed);
		this.UpdateArmyRetrofitButton();
		if (this.DepartmentOfScience.CanTradeUnits(false))
		{
			this.ArmyDisbandButton.AgeTransform.Visible = false;
			this.ArmySellButton.AgeTransform.Visible = true;
			this.UpdateArmySellButton();
		}
		else
		{
			this.ArmyDisbandButton.AgeTransform.Visible = true;
			this.ArmySellButton.AgeTransform.Visible = false;
			this.UpdateArmyDisbandButton();
		}
		this.UpdateArmyHealButton();
		this.UpdateArmyForceShiftButton();
		this.EditButton.AgeTransform.Enable = (this.SelectedDesign != null);
		this.DeleteButton.AgeTransform.Enable = (this.SelectedDesign != null && this.interactionsAllowed);
	}

	private void RefreshUnitListPanel()
	{
		if (ArmyLine.CurrentArmy != null)
		{
			this.UnitListPanel.Bind(ArmyLine.CurrentArmy, null, base.gameObject);
			this.UnitListPanel.RefreshContent();
			this.UnitListPanel.Show(new object[0]);
			this.UnitListPanel.SelectAll();
		}
		else if (this.UnitListPanel.IsVisible)
		{
			this.UnitListPanel.Hide(false);
		}
	}

	private void RefreshUnitEquipment()
	{
		List<UnitGuiItem> list = (from unitGuiItem in this.UnitsTable.GetChildren<UnitGuiItem>(true)
		where unitGuiItem.UnitToggle.State
		select unitGuiItem).ToList<UnitGuiItem>();
		if (list.Count != 1)
		{
			this.HideUnitEquipmentPanel();
			return;
		}
		Unit unit = list[0].GuiUnit.TryGetUnit();
		if (unit != null)
		{
			this.unitEquipmentPanel.Bind(list[0].GuiUnit.UnitDesign, unit);
			if (this.unitEquipmentPanel.IsVisible)
			{
				this.unitEquipmentPanel.RefreshContent();
			}
			else
			{
				this.unitEquipmentPanel.Show(new object[]
				{
					"readonly"
				});
			}
		}
	}

	private void RefreshUnitDesignEquipment()
	{
		if (this.temporaryUnit != null && this.SelectedDesign == this.unitEquipmentPanel.UnitDesign)
		{
			return;
		}
		if (this.SelectedDesign == null)
		{
			this.HideUnitEquipmentPanel();
			return;
		}
		if (this.temporaryUnit != null)
		{
			this.temporaryUnit.Dispose();
			this.temporaryUnit = null;
		}
		this.temporaryUnit = this.DepartmentOfDefense.CreateTemporaryUnit(this.temporaryGuid, this.SelectedDesign);
		if (this.temporaryUnit == null)
		{
			this.Hide(false);
			return;
		}
		if (this.temporaryGarrison != null)
		{
			this.temporaryGarrison.AddChild(this.temporaryUnit);
			this.temporaryUnit.Refresh(true);
		}
		this.unitEquipmentPanel.Bind(this.SelectedDesign, this.temporaryUnit);
		if (this.unitEquipmentPanel.IsVisible)
		{
			this.unitEquipmentPanel.RefreshContent();
		}
		else
		{
			this.unitEquipmentPanel.Show(new object[]
			{
				"readonly"
			});
		}
	}

	private void UnitDesignRefresh(AgeTransform tableitem, UnitDesign design, int index)
	{
		UnitGuiItem component = tableitem.GetComponent<UnitGuiItem>();
		component.RefreshContent(base.Empire, design, base.gameObject, null, true);
	}

	private void UpdateArmyRetrofitButton()
	{
		this.ArmyRetrofitButton.AgeTransform.Enable = false;
		this.ArmyRetrofitPriceLabel.AgeTransform.Visible = false;
		if (ArmyLine.CurrentArmy == null)
		{
			this.ArmyRetrofitButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%MilitaryNoArmySelectedDescription");
			return;
		}
		List<Unit> retrofitableUnits = this.GetRetrofitableUnits(ArmyLine.CurrentArmy.StandardUnits);
		if (retrofitableUnits.Count == 0)
		{
			this.ArmyRetrofitButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%RetrofitTabNoCandidateDescription");
			return;
		}
		ConstructionCost[] array = this.DepartmentOfDefense.GetRetrofitCosts(retrofitableUnits.ToArray());
		if (array.Length == 0)
		{
			array = new ConstructionCost[]
			{
				new ConstructionCost("EmpireMoney", 0f, true, false)
			};
		}
		StringBuilder arg = new StringBuilder();
		AgeUtils.CleanLine(GuiFormater.FormatCost(base.Empire, array, false, 1, null), ref arg);
		this.ArmyRetrofitPriceLabel.Text = GuiFormater.FormatCost(base.Empire, array, false, 0, null);
		this.ArmyRetrofitPriceLabel.AgeTransform.Visible = true;
		for (int i = 0; i < retrofitableUnits.Count; i++)
		{
			DepartmentOfDefense.CheckRetrofitPrerequisitesResult checkRetrofitPrerequisitesResult = this.departmentOfDefense.CheckRetrofitPrerequisites(retrofitableUnits[0], array);
			if (checkRetrofitPrerequisitesResult == DepartmentOfDefense.CheckRetrofitPrerequisitesResult.GarrisonArmyIsInEncounter)
			{
				this.ArmyRetrofitButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%ArmyLockedInBattleDescription");
				return;
			}
			if (checkRetrofitPrerequisitesResult == DepartmentOfDefense.CheckRetrofitPrerequisitesResult.GarrisonCityIsUnderSiege)
			{
				this.ArmyRetrofitButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%RetrofitTabGarrisonCityUnderSiegeDescription");
				return;
			}
			if (checkRetrofitPrerequisitesResult == DepartmentOfDefense.CheckRetrofitPrerequisitesResult.RegionDoesntBelongToUs || checkRetrofitPrerequisitesResult == DepartmentOfDefense.CheckRetrofitPrerequisitesResult.WorldPositionIsNotValid)
			{
				this.ArmyRetrofitButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%RetrofitTabNotInOwnRegionDescription");
				return;
			}
		}
		if (!this.DepartmentOfTheTreasury.CanAfford(array))
		{
			this.ArmyRetrofitButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%MilitaryArmyRetrofitCannotAffordDescription") + " : " + arg;
			return;
		}
		this.ArmyRetrofitButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%MilitaryArmyRetrofitDescription") + " : " + arg;
		this.ArmyRetrofitButton.AgeTransform.Enable = this.interactionsAllowed;
	}

	private void UpdateArmyDisbandButton()
	{
		if (ArmyLine.CurrentArmy == null)
		{
			this.ArmyDisbandButton.AgeTransform.Enable = false;
			this.ArmyDisbandButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%MilitaryNoArmySelectedDescription");
			return;
		}
		List<Unit> salableUnits = this.GetSalableUnits(ArmyLine.CurrentArmy.StandardUnits);
		if (salableUnits.Count == 0)
		{
			this.ArmyDisbandButton.AgeTransform.Enable = false;
			this.ArmyDisbandButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%DisbandTabNoCandidateDescription");
			return;
		}
		if (!this.IsSelectedArmyInEncounter())
		{
			this.ArmyDisbandButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%MilitaryArmyDisbandDescription");
			this.ArmyDisbandButton.AgeTransform.Enable = this.interactionsAllowed;
		}
		else
		{
			this.ArmyDisbandButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%ArmyLockedInBattleDescription");
			this.ArmyDisbandButton.AgeTransform.Enable = false;
		}
	}

	private void UpdateArmySellButton()
	{
		this.ArmySellPriceLabel.AgeTransform.Visible = false;
		if (ArmyLine.CurrentArmy == null)
		{
			this.ArmySellButton.AgeTransform.Enable = false;
			this.ArmySellButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%MilitaryNoArmySelectedDescription");
			return;
		}
		List<Unit> salableUnits = this.GetSalableUnits(ArmyLine.CurrentArmy.StandardUnits);
		if (salableUnits.Count == 0)
		{
			this.ArmySellButton.AgeTransform.Enable = false;
			this.ArmySellButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%SellTabNoCandidateDescription");
			return;
		}
		float stockValue = this.TotalSellPriceOfStandardUnits(salableUnits);
		this.ArmySellPriceLabel.Text = GuiFormater.FormatStock(stockValue, DepartmentOfTheTreasury.Resources.EmpireMoney, 0, true);
		this.ArmySellPriceLabel.AgeTransform.Visible = true;
		if (!this.IsSelectedArmyInEncounter())
		{
			this.ArmySellButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%MilitaryArmySellDescription");
			this.ArmySellButton.AgeTransform.Enable = this.interactionsAllowed;
		}
		else
		{
			this.ArmySellButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%ArmyLockedInBattleDescription");
			this.ArmySellButton.AgeTransform.Enable = false;
		}
	}

	private void UpdateArmyHealButton()
	{
		this.ArmyHealButton.AgeTransform.Visible = false;
		this.ArmyHealButton.AgeTransform.Enable = false;
		if (ArmyLine.CurrentArmy == null)
		{
			this.ArmyHealButton.AgeTransform.Enable = false;
			this.ArmyHealButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%MilitaryNoArmySelectedDescription");
			return;
		}
		List<Unit> list = new List<Unit>(ArmyLine.CurrentArmy.StandardUnits);
		list.RemoveAll((Unit unit) => !unit.CheckUnitAbility(UnitAbility.UnitAbilityInstantHeal, -1));
		list.RemoveAll((Unit unit) => !unit.IsWounded());
		if (list.Count > 0)
		{
			StringBuilder stringBuilder = new StringBuilder();
			ConstructionCost[] unitHealCost = this.DepartmentOfTheTreasury.GetUnitHealCost(list);
			AgeUtils.CleanLine(GuiFormater.FormatCost(base.Empire, unitHealCost, false, 1, null), ref stringBuilder);
			this.ArmyHealPriceLabel.Text = GuiFormater.FormatCost(base.Empire, unitHealCost, false, 0, null);
			this.ArmyHealButton.AgeTransform.Visible = true;
			if (list.Exists((Unit unit) => !unit.HasEnoughActionPointLeft(1)))
			{
				this.ArmyHealButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%HealTabNoActionPointDescription") + " : " + stringBuilder;
			}
			else if (!this.DepartmentOfTheTreasury.CanAfford(unitHealCost))
			{
				this.ArmyHealButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%MilitaryArmyHealCannotAffordDescription") + " : " + stringBuilder;
			}
			else if (!this.IsSelectedArmyInEncounter())
			{
				this.ArmyHealButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%MilitaryArmyHealFormat").Replace("$Value", stringBuilder.ToString());
				this.ArmyHealButton.AgeTransform.Enable = this.interactionsAllowed;
			}
			else
			{
				this.ArmyHealButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%ArmyLockedInBattleDescription");
			}
		}
	}

	private void UpdateArmyForceShiftButton()
	{
		this.ArmyForceShiftButton.AgeTransform.Visible = false;
		this.ArmyForceShiftButton.AgeTransform.Enable = false;
		if (ArmyLine.CurrentArmy == null)
		{
			this.ArmyForceShiftButton.AgeTransform.Enable = false;
			this.ArmyForceShiftButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%MilitaryNoArmySelectedDescription");
			return;
		}
		DepartmentOfScience agency = ArmyLine.CurrentArmy.Empire.GetAgency<DepartmentOfScience>();
		if (agency == null || agency.GetTechnologyState("TechnologyDefinitionOrbUnlock17WinterShifters") != DepartmentOfScience.ConstructibleElement.State.Researched)
		{
			return;
		}
		List<Unit> list = new List<Unit>(ArmyLine.CurrentArmy.StandardUnits);
		list.RemoveAll((Unit unit) => !unit.IsShifter() || !unit.IsInCurrentSeasonForm());
		if (list.Count > 0)
		{
			StringBuilder stringBuilder = new StringBuilder();
			ConstructionCost[] unitForceShiftingCost = this.DepartmentOfTheTreasury.GetUnitForceShiftingCost(list);
			AgeUtils.CleanLine(GuiFormater.FormatCost(base.Empire, unitForceShiftingCost, false, 1, null), ref stringBuilder);
			this.ArmyForceShiftPriceLabel.Text = GuiFormater.FormatCost(base.Empire, unitForceShiftingCost, false, 0, null);
			this.ArmyForceShiftButton.AgeTransform.Visible = true;
			if (!this.DepartmentOfTheTreasury.CanAfford(unitForceShiftingCost))
			{
				this.ArmyForceShiftButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%MilitaryArmyForceShiftCannotAffordDescription") + " : " + stringBuilder;
			}
			else if (!this.IsSelectedArmyInEncounter())
			{
				this.ArmyForceShiftButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%MilitaryArmyForceShiftFormat").Replace("$Value", stringBuilder.ToString());
				this.ArmyForceShiftButton.AgeTransform.Enable = this.interactionsAllowed;
			}
			else
			{
				this.ArmyForceShiftButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%ArmyLockedInBattleDescription");
			}
		}
	}

	private void RetrofitArmy()
	{
		if (ArmyLine.CurrentArmy != null)
		{
			GameMilitaryScreen.<RetrofitArmy>c__AnonStoreyA5C <RetrofitArmy>c__AnonStoreyA5C = new GameMilitaryScreen.<RetrofitArmy>c__AnonStoreyA5C();
			<RetrofitArmy>c__AnonStoreyA5C.units = ArmyLine.CurrentArmy.StandardUnits;
			List<Unit> list = new List<Unit>();
			int i;
			for (i = 0; i < <RetrofitArmy>c__AnonStoreyA5C.units.Count; i++)
			{
				UnitDesign unitDesign = this.DepartmentOfDefense.UnitDesignDatabase.UserDefinedUnitDesigns.FirstOrDefault((UnitDesign design) => design.Model == <RetrofitArmy>c__AnonStoreyA5C.units[i].UnitDesign.Model);
				if (<RetrofitArmy>c__AnonStoreyA5C.units[i].UnitDesign != unitDesign)
				{
					list.Add(<RetrofitArmy>c__AnonStoreyA5C.units[i]);
				}
			}
			base.AgeTransform.Enable = false;
			OrderRetrofitUnit order = new OrderRetrofitUnit(base.Empire.Index, list.ConvertAll<GameEntityGUID>((Unit unit) => unit.GUID).ToArray());
			Ticket ticket;
			base.PlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
		}
	}

	private void SellArmy()
	{
		List<Unit> salableUnits = this.GetSalableUnits(ArmyLine.CurrentArmy.StandardUnits);
		bool flag = salableUnits.Count == ArmyLine.CurrentArmy.StandardUnits.Count && ArmyLine.CurrentArmy.Hero == null;
		OrderSelloutTradableUnits order = new OrderSelloutTradableUnits(base.Empire.Index, salableUnits.ConvertAll<GameEntityGUID>((Unit unit) => unit.GUID).ToArray());
		base.PlayerController.PostOrder(order);
		if (flag)
		{
			this.DestroyArmy();
		}
	}

	private void DestroyArmy()
	{
		base.AgeTransform.Enable = false;
		OrderDestroyArmy order = new OrderDestroyArmy(base.Empire.Index, ArmyLine.CurrentArmy.GUID);
		ArmyLine.CurrentArmy = null;
		Ticket ticket;
		base.PlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
	}

	private void SelectAllTogglesInTable(AgeTransform table, bool toggleState)
	{
		List<UnitGuiItem> children = table.GetChildren<UnitGuiItem>(true);
		for (int i = 0; i < children.Count; i++)
		{
			if (children[i].AgeTransform.Visible && children[i].AgeTransform.Alpha > 0f)
			{
				children[i].UnitToggle.State = toggleState;
			}
		}
	}

	private float TotalSellPriceOfStandardUnits(List<Unit> salableUnits)
	{
		float num = 0f;
		for (int i = 0; i < salableUnits.Count; i++)
		{
			num += TradableUnit.GetPriceWithSalesTaxes(salableUnits[i], TradableTransactionType.Sellout, base.Empire, 1f);
		}
		return num;
	}

	public Transform ArmyListPanelPrefab;

	public Transform UnitEquipmentPanelPrefab;

	public Transform UnitItemPrefab;

	public Transform UnitDesignItemPrefab;

	public AgeTransform ArmyListFrame;

	public AgeTransform UnitEquipementFrame;

	public AgeControlButton CreateButton;

	public AgeControlButton DeleteButton;

	public AgeControlButton EditButton;

	public AgeControlButton AcademyScreenButton;

	public AgeControlButton ArmyRenameButton;

	public AgeControlButton ArmyRetrofitButton;

	public AgePrimitiveLabel ArmyRetrofitPriceLabel;

	public AgeControlButton ArmyDisbandButton;

	public AgeControlButton ArmySellButton;

	public AgePrimitiveLabel ArmySellPriceLabel;

	public AgeControlButton ArmyHealButton;

	public AgePrimitiveLabel ArmyHealPriceLabel;

	public AgeControlButton ArmyForceShiftButton;

	public AgePrimitiveLabel ArmyForceShiftPriceLabel;

	public AgeTransform UnitsTable;

	public AgeTransform UnitDesignsTable;

	public AgeControlScrollView UnitDesignsScrollView;

	public UnitListPanel UnitListPanel;

	private ArmyListPanel armyListPanel;

	private UnitEquipmentPanel unitEquipmentPanel;

	private Unit temporaryUnit;

	private GameEntityGUID temporaryGuid;

	private DepartmentOfDefense departmentOfDefense;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private DepartmentOfScience departmentOfScience;

	private IEncounterRepositoryService encounterRepositoryService;

	private IEndTurnService endTurnService;

	private bool interactionsAllowed = true;

	private AgeTransform.RefreshTableItem<UnitDesign> unitDesignRefreshDelegate;

	private SimulationObjectWrapper temporaryGarrison;
}
