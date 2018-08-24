using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.View;
using UnityEngine;

public class UnitListPanel : GuiCollapsingPanel
{
	public event EventHandler SelectionChange;

	public List<Unit> SelectUnits
	{
		get
		{
			return this.selectedUnits;
		}
	}

	public IGarrison Garrison
	{
		get
		{
			return this.garrison;
		}
		private set
		{
			if (this.garrison != null)
			{
				this.garrison.HeroChange -= this.Garrison_HeroChange;
				this.garrison.EncounterChange -= this.Garrison_EncounterChange;
				this.Garrison.StandardUnitCollectionChange -= this.Garrison_StandardUnitCollectionChange;
				for (int i = 0; i < this.Garrison.StandardUnits.Count; i++)
				{
					this.Garrison.StandardUnits[i].Refreshed -= this.Unit_Refreshed;
				}
			}
			this.garrison = value;
			if (this.garrison != null)
			{
				this.garrison.HeroChange += this.Garrison_HeroChange;
				this.garrison.EncounterChange += this.Garrison_EncounterChange;
				this.Garrison.StandardUnitCollectionChange += this.Garrison_StandardUnitCollectionChange;
				for (int j = 0; j < this.Garrison.StandardUnits.Count; j++)
				{
					this.Garrison.StandardUnits[j].Refreshed += this.Unit_Refreshed;
				}
			}
		}
	}

	public IGarrison Militia
	{
		get
		{
			return this.militia;
		}
		private set
		{
			if (this.militia != null)
			{
				this.militia.StandardUnitCollectionChange -= this.Garrison_StandardUnitCollectionChange;
				for (int i = 0; i < this.militia.StandardUnits.Count; i++)
				{
					this.militia.StandardUnits[i].Refreshed -= this.Unit_Refreshed;
				}
			}
			this.militia = value;
			if (this.militia != null)
			{
				this.militia.StandardUnitCollectionChange += this.Garrison_StandardUnitCollectionChange;
				for (int j = 0; j < this.militia.StandardUnits.Count; j++)
				{
					this.militia.StandardUnits[j].Refreshed += this.Unit_Refreshed;
				}
			}
		}
	}

	protected bool IsOtherEmpire { get; set; }

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

	public virtual void Bind(IGarrison garrison, IGarrison militia = null, GameObject parent = null)
	{
		this.Unbind();
		IPlayerControllerRepositoryService service = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
		this.IsOtherEmpire = (service.ActivePlayerController.Empire != garrison.Empire);
		this.Garrison = garrison;
		this.Militia = militia;
		this.guiUnits.Clear();
		for (int i = 0; i < this.Garrison.StandardUnits.Count; i++)
		{
			this.guiUnits.Add(new GuiUnit(this.Garrison.StandardUnits[i], null));
		}
		if (this.Militia != null)
		{
			for (int j = 0; j < this.Militia.StandardUnits.Count; j++)
			{
				this.guiUnits.Add(new GuiUnit(this.Militia.StandardUnits[j], null));
			}
		}
		this.DepartmentOfTheTreasury = this.Garrison.Empire.GetAgency<DepartmentOfTheTreasury>();
		this.DepartmentOfScience = this.Garrison.Empire.GetAgency<DepartmentOfScience>();
		Diagnostics.Assert(this.Garrison.Empire != null, "Garrison has not assigned Empire");
		this.departmentOfDefense = this.Garrison.Empire.GetAgency<DepartmentOfDefense>();
		this.departmentOfTheInterior = this.Garrison.Empire.GetAgency<DepartmentOfTheInterior>();
		this.parent = parent;
	}

	public virtual void Unbind()
	{
		if (this.Garrison != null)
		{
			if (this.departmentOfDefense != null)
			{
				this.departmentOfDefense = null;
			}
			this.DepartmentOfTheTreasury = null;
			this.DepartmentOfScience = null;
			this.guiUnits.Clear();
			this.Garrison = null;
			this.Militia = null;
		}
		this.IsOtherEmpire = true;
	}

	public override void RefreshContent()
	{
		if (this.Garrison != null)
		{
			base.RefreshContent();
			this.guiUnits.Sort();
			this.UnitsTable.Enable = !this.IsOtherEmpire;
			this.UnitsTable.Height = 2f;
			if (this.guiUnits.Count > 0)
			{
				this.UnitsTable.ReserveChildren(this.guiUnits.Count, this.UnitPrefab, "Unit");
			}
			this.UnitsTable.RefreshChildrenIList<GuiUnit>(this.guiUnits, this.refreshGuiUnitDelegate, true, false);
			this.UnitsTable.ArrangeChildren();
			if (this.UnitsScrollview != null)
			{
				this.UnitsScrollview.OnPositionRecomputed();
			}
			this.ComputeSelection();
		}
		this.RefreshButtons();
	}

	public void OnUnitToggle(GameObject obj)
	{
		if (Input.GetKey(KeyCode.Mouse0))
		{
			AgeControlToggle component = obj.GetComponent<AgeControlToggle>();
			List<UnitGuiItem> children = this.UnitsTable.GetChildren<UnitGuiItem>(true);
			for (int i = 0; i < children.Count; i++)
			{
				bool flag = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
				if (children[i].UnitToggle == component)
				{
					if (!flag)
					{
						children[i].UnitToggle.State = true;
					}
				}
				else if (!flag)
				{
					children[i].UnitToggle.State = false;
				}
			}
			this.ComputeSelection();
			if (this.parent != null)
			{
				this.parent.SendMessage("OnUnitToggle");
				return;
			}
		}
		else
		{
			this.OnELCPRightClick(obj);
		}
	}

	public void OnHealUnitsCB(GameObject obj = null)
	{
		this.HealSelectedUnits();
	}

	public void OnForceShiftCB(GameObject obj)
	{
		if (this.Garrison != null)
		{
			DepartmentOfScience agency = this.Garrison.Empire.GetAgency<DepartmentOfScience>();
			if (agency == null || agency.GetTechnologyState("TechnologyDefinitionOrbUnlock17WinterShifters") != DepartmentOfScience.ConstructibleElement.State.Researched)
			{
				return;
			}
			List<Unit> list = new List<Unit>(this.selectedUnits);
			list.RemoveAll((Unit unit) => !unit.IsShifter() || !unit.IsInCurrentSeasonForm());
			if (list.Count > 0)
			{
				OrderForceShiftUnits order = new OrderForceShiftUnits(this.Garrison.Empire.Index, list.ToArray());
				IPlayerControllerRepositoryService service = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
				Ticket ticket;
				service.ActivePlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
				EventForceShift eventToNotify = new EventForceShift(this.Garrison.Empire, list.Count, true);
				IEventService service2 = Services.GetService<IEventService>();
				if (service2 != null)
				{
					service2.Notify(eventToNotify);
				}
			}
		}
	}

	public void OnImmolateCB(GameObject obj)
	{
		if (this.Garrison != null)
		{
			List<Unit> list = new List<Unit>(this.selectedUnits);
			list.RemoveAll((Unit unit) => !unit.IsImmolableUnit() || unit.IsAlreadyImmolated());
			if (list.Count > 0)
			{
				OrderImmolateUnits order = new OrderImmolateUnits(this.Garrison.Empire.Index, list.ToArray());
				IPlayerControllerRepositoryService service = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
				Ticket ticket;
				service.ActivePlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
			}
		}
	}

	public void OnRetrofitUnitsCB(GameObject obj = null)
	{
		string message = AgeLocalizer.Instance.LocalizeString("%MilitaryConfirmRetrofitUnitsTitle");
		MessagePanel.Instance.Show(message, "%Confirmation", MessagePanelButtons.YesNo, new MessagePanel.EventHandler(this.OnRetrofitUnitsConfirmation), MessagePanelType.INFORMATIVE, new MessagePanelButton[0]);
	}

	public void OnSelectAllCB(GameObject obj = null)
	{
		this.SelectAll();
	}

	public void OnDisbandUnitsCB(GameObject obj = null)
	{
		string message = AgeLocalizer.Instance.LocalizeString("%MilitaryConfirmDisbandUnitsTitle");
		MessagePanel.Instance.Show(message, "%Confirmation", MessagePanelButtons.YesNo, new MessagePanel.EventHandler(this.OnDisbandUnitsConfirmation), MessagePanelType.INFORMATIVE, new MessagePanelButton[0]);
	}

	public void OnSellUnitsCB(GameObject obj = null)
	{
		string message = AgeLocalizer.Instance.LocalizeString("%MilitaryConfirmSellUnitsTitle");
		MessagePanel.Instance.Show(message, "%Confirmation", MessagePanelButtons.YesNo, new MessagePanel.EventHandler(this.OnSellUnitsConfirmation), MessagePanelType.INFORMATIVE, new MessagePanelButton[0]);
	}

	public void SelectAll()
	{
		List<UnitGuiItem> children = this.UnitsTable.GetChildren<UnitGuiItem>(true);
		for (int i = 0; i < children.Count; i++)
		{
			if (children[i].AgeTransform.Enable)
			{
				children[i].UnitToggle.State = true;
			}
		}
		this.ComputeSelection();
		if (this.parent != null)
		{
			this.parent.SendMessage("OnUnitToggle");
		}
	}

	public void UnselectAll()
	{
		List<UnitGuiItem> children = this.UnitsTable.GetChildren<UnitGuiItem>(true);
		for (int i = 0; i < children.Count; i++)
		{
			children[i].UnitToggle.State = false;
		}
		this.ComputeSelection();
	}

	public void HealSelectedUnits()
	{
		if (this.Garrison != null)
		{
			List<Unit> list = new List<Unit>(this.selectedUnits);
			list.RemoveAll((Unit unit) => !unit.CheckUnitAbility(UnitAbility.UnitAbilityInstantHeal, -1));
			list.RemoveAll((Unit unit) => !unit.IsWounded());
			list.RemoveAll((Unit unit) => !unit.HasEnoughActionPointLeft(1));
			if (list.Count > 0)
			{
				base.AgeTransform.Enable = false;
				OrderHealUnits order = new OrderHealUnits(this.Garrison.Empire.Index, list);
				Ticket ticket;
				this.playerControllerRepositoryService.ActivePlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
			}
		}
	}

	public void RetrofitSelectedUnits()
	{
		if (this.Garrison != null)
		{
			this.RetrofitComputeCandidates();
			base.AgeTransform.Enable = false;
			OrderRetrofitUnit order = new OrderRetrofitUnit(this.Garrison.Empire.Index, this.retrofitableUnits.ConvertAll<GameEntityGUID>((Unit unit) => unit.GUID).ToArray());
			Ticket ticket;
			this.playerControllerRepositoryService.ActivePlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
		}
	}

	public void DisbandSelectedUnits()
	{
		if (this.Garrison != null)
		{
			this.DisbandComputeCandidates();
			OrderDisbandUnits order = new OrderDisbandUnits(this.Garrison.Empire.Index, this.salableUnits.ConvertAll<GameEntityGUID>((Unit unit) => unit.GUID).ToArray());
			base.AgeTransform.Enable = false;
			Ticket ticket;
			this.playerControllerRepositoryService.ActivePlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
		}
	}

	public void SellSelectedUnits()
	{
		if (this.Garrison != null)
		{
			this.SellComputeCandidates();
			OrderSelloutTradableUnits order = new OrderSelloutTradableUnits(this.Garrison.Empire.Index, this.salableUnits.ConvertAll<GameEntityGUID>((Unit unit) => unit.GUID).ToArray());
			base.AgeTransform.Enable = false;
			Ticket ticket;
			this.playerControllerRepositoryService.ActivePlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
		}
	}

	protected virtual bool CanDisplayRetrofitCost()
	{
		return true;
	}

	protected virtual bool CheckLeaderAbility(Unit unit)
	{
		return false;
	}

	protected virtual void ComputeSelection()
	{
		this.selectedUnits.Clear();
		if (!this.IsOtherEmpire)
		{
			List<UnitGuiItem> children = this.UnitsTable.GetChildren<UnitGuiItem>(true);
			for (int i = 0; i < children.Count; i++)
			{
				if (children[i].UnitToggle.State)
				{
					Unit unit = children[i].GuiUnit.TryGetUnit();
					if (unit != null)
					{
						this.selectedUnits.Add(unit);
					}
				}
			}
		}
		this.RefreshButtons();
		this.OnSelectionChange();
		if (this.garrisonWorldCursor != null && this.garrisonWorldCursor.Garrison != null)
		{
			this.garrisonWorldCursor.ChangeSelection(this.selectedUnits.ToArray());
		}
	}

	protected override void OnApplyHighDefinition(float scale)
	{
		base.OnApplyHighDefinition(scale);
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		this.guiUnits = new List<GuiUnit>();
		this.selectedUnits = new List<Unit>();
		this.retrofitableUnits = new List<Unit>();
		this.salableUnits = new List<Unit>();
		yield break;
	}

	protected override void OnUnload()
	{
		this.selectedUnits.Clear();
		this.selectedUnits = null;
		this.retrofitableUnits.Clear();
		this.retrofitableUnits = null;
		this.salableUnits.Clear();
		this.salableUnits = null;
		this.guiUnits.Clear();
		this.guiUnits = null;
		base.OnUnload();
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		this.playerControllerRepositoryService = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
		Diagnostics.Assert(this.playerControllerRepositoryService != null);
		this.EndTurnService = Services.GetService<IEndTurnService>();
		this.refreshGuiUnitDelegate = new AgeTransform.RefreshTableItem<GuiUnit>(this.RefreshGuiUnit);
		this.seasonService = base.GameService.Game.Services.GetService<ISeasonService>();
		yield break;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		this.UnselectAll();
		this.Unbind();
		this.garrisonWorldCursor = null;
		yield return base.OnHide(instant);
		yield break;
	}

	protected void OnOrderResponse(object sender, TicketRaisedEventArgs args)
	{
		base.AgeTransform.Enable = true;
		this.RefreshContent();
		if (this.parent != null)
		{
			this.parent.SendMessage("OnUnitsChanged");
		}
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		ICursorService cursorService = Services.GetService<ICursorService>();
		this.interactionsAllowed = this.playerControllerRepositoryService.ActivePlayerController.CanSendOrders();
		this.garrisonWorldCursor = (cursorService.CurrentCursor as GarrisonWorldCursor);
		yield break;
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.Hide(true);
		this.refreshGuiUnitDelegate = null;
		this.EndTurnService = null;
		this.playerControllerRepositoryService = null;
		this.UnitsTable.DestroyAllChildren();
		base.OnUnloadGame(game);
	}

	protected virtual void RefreshButtons()
	{
		if (this.SelectAllButton != null)
		{
			if (!this.IsOtherEmpire)
			{
				this.SelectAllButton.Visible = true;
				this.SelectAllButton.Enable = (this.CanSelectAllAndExplain() && this.interactionsAllowed);
			}
			else
			{
				this.SelectAllButton.Visible = false;
			}
		}
		if (this.DisbandButton != null)
		{
			if ((!this.IsOtherEmpire && !this.DepartmentOfScience.CanTradeUnits(false)) || (this.selectedUnits.Count == 1 && this.selectedUnits[0].CheckUnitAbility(UnitAbility.ReadonlyColonize, -1)))
			{
				this.DisbandButton.Visible = true;
				this.DisbandButton.Enable = (this.CanDisbandUnitsAndExplain() && this.interactionsAllowed);
			}
			else
			{
				this.DisbandButton.Visible = false;
			}
		}
		if (this.SellButton != null)
		{
			if (!this.IsOtherEmpire && this.DepartmentOfScience.CanTradeUnits(false) && (this.selectedUnits.Count != 1 || !this.selectedUnits[0].CheckUnitAbility(UnitAbility.ReadonlyColonize, -1)))
			{
				this.SellButton.Visible = true;
				this.SellButton.Enable = (this.CanSellUnitsAndExplain() && this.interactionsAllowed);
			}
			else
			{
				this.SellButton.Visible = false;
			}
		}
		if (this.RetrofitButton != null)
		{
			if (!this.IsOtherEmpire)
			{
				this.RetrofitButton.Visible = true;
				this.RetrofitButton.Enable = (this.CanRetrofitUnitsAndExplain() && this.interactionsAllowed);
			}
			else
			{
				this.RetrofitButton.Visible = false;
			}
		}
		if (this.HealButton != null)
		{
			if (!this.IsOtherEmpire)
			{
				this.HealButton.Visible = true;
				this.HealButton.Enable = (this.CanHealUnitsAndExplain() && this.interactionsAllowed);
			}
			else
			{
				this.HealButton.Visible = false;
			}
		}
		this.RefreshForceShiftingButton();
		this.RefreshImmolateButton();
		this.RefreshImmolated();
	}

	protected virtual void RefreshGuiUnit(AgeTransform unitTransform, GuiUnit guiUnit, int index)
	{
		unitTransform.GetComponent<UnitGuiItem>().RefreshContent(guiUnit, base.gameObject, base.AgeTransform);
		unitTransform.Enable = !guiUnit.IsMilitia;
		if (guiUnit.Unit == this.Garrison.Hero)
		{
			unitTransform.Visible = false;
		}
		else
		{
			unitTransform.Visible = true;
		}
	}

	protected void Select(Unit unit, bool selectOnlyHim)
	{
		List<UnitGuiItem> children = this.UnitsTable.GetChildren<UnitGuiItem>(true);
		for (int i = 0; i < children.Count; i++)
		{
			if (children[i].GuiUnit.GUID == unit.GUID)
			{
				children[i].UnitToggle.State = true;
			}
			else if (selectOnlyHim)
			{
				children[i].UnitToggle.State = false;
			}
		}
		this.ComputeSelection();
	}

	private bool CanHealUnitsAndExplain()
	{
		this.HealButton.Visible = false;
		if (this.IsOtherEmpire)
		{
			return false;
		}
		if (this.selectedUnits.Count > 0)
		{
			List<Unit> list = new List<Unit>(this.selectedUnits);
			list.RemoveAll((Unit match) => !match.CheckUnitAbility(UnitAbility.UnitAbilityInstantHeal, -1));
			list.RemoveAll((Unit match) => !match.IsWounded());
			if (list.Count > 0)
			{
				ConstructionCost[] unitHealCost = this.DepartmentOfTheTreasury.GetUnitHealCost(list);
				AgeUtils.CleanLine(GuiFormater.FormatCost(this.garrison.Empire, unitHealCost, false, 1, null), ref this.monochromaticFormat);
				this.HealButtonPriceLabel.Text = GuiFormater.FormatCost(this.garrison.Empire, unitHealCost, false, 0, null);
				this.HealButton.Visible = true;
				if (list.Exists((Unit unit) => !unit.HasEnoughActionPointLeft(1)))
				{
					this.HealButton.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%HealTabNoActionPointDescription");
				}
				else if (this.DepartmentOfTheTreasury.CanAfford(unitHealCost))
				{
					if (!this.garrison.IsInEncounter)
					{
						this.HealButton.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%HealTabOKFormat").Replace("$Value", this.monochromaticFormat.ToString());
						return true;
					}
					this.HealButton.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%ArmyLockedInBattleDescription");
				}
				else
				{
					this.HealButton.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%HealTabCannotAffordDescription") + " : " + this.monochromaticFormat;
				}
			}
		}
		return false;
	}

	private void RefreshForceShiftingButton()
	{
		if (this.ForceShiftButton == null || this.Garrison == null || this.Garrison.Empire == null)
		{
			return;
		}
		this.ForceShiftButton.Visible = false;
		this.ForceShiftButton.Enable = false;
		if (this.IsOtherEmpire || !this.interactionsAllowed)
		{
			return;
		}
		DepartmentOfScience agency = this.Garrison.Empire.GetAgency<DepartmentOfScience>();
		if (agency == null || agency.GetTechnologyState("TechnologyDefinitionOrbUnlock17WinterShifters") != DepartmentOfScience.ConstructibleElement.State.Researched)
		{
			return;
		}
		if (this.selectedUnits.Count > 0)
		{
			List<Unit> list = new List<Unit>(this.selectedUnits);
			list.RemoveAll((Unit match) => !match.IsShifter() || !match.IsInCurrentSeasonForm());
			if (list.Count > 0)
			{
				ConstructionCost[] unitForceShiftingCost = this.DepartmentOfTheTreasury.GetUnitForceShiftingCost(list);
				AgeUtils.CleanLine(GuiFormater.FormatCost(this.garrison.Empire, unitForceShiftingCost, false, 1, null), ref this.monochromaticFormat);
				this.ForceShiftButtonPriceLabel.Text = GuiFormater.FormatCost(this.garrison.Empire, unitForceShiftingCost, false, 0, null);
				this.ForceShiftButton.Visible = true;
				if (this.DepartmentOfTheTreasury.CanAfford(unitForceShiftingCost))
				{
					if (!this.garrison.IsInEncounter)
					{
						this.ForceShiftButton.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%ForceShiftTabOKFormat").Replace("$Value", this.monochromaticFormat.ToString());
						this.ForceShiftButton.Enable = true;
					}
					else
					{
						this.ForceShiftButton.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%ArmyLockedInBattleDescription");
					}
				}
				else
				{
					this.ForceShiftButton.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%ForceShiftTabCannotAffordDescription") + " : " + this.monochromaticFormat;
				}
			}
		}
	}

	private void RefreshImmolateButton()
	{
		if (this.ImmolateShiftButton == null || this.Garrison == null || this.Garrison.Empire == null)
		{
			return;
		}
		this.ImmolateShiftButton.Visible = false;
		this.ImmolateShiftButton.Enable = false;
		if (this.IsOtherEmpire || !this.interactionsAllowed || !this.Garrison.Empire.SimulationObject.Tags.Contains("FactionTraitBrokenLordsHeatWave"))
		{
			return;
		}
		Season currentSeason = this.seasonService.GetCurrentSeason();
		if (currentSeason.SeasonDefinition.SeasonType != Season.ReadOnlyHeatWave)
		{
			return;
		}
		if (this.selectedUnits.Count > 0)
		{
			List<Unit> list = new List<Unit>(this.selectedUnits);
			list.RemoveAll((Unit match) => !match.IsImmolableUnit() || match.IsAlreadyImmolated());
			if (list.Count > 0)
			{
				this.ImmolateShiftButton.Visible = true;
				if (!this.garrison.IsInEncounter)
				{
					this.ImmolateShiftButton.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%ImmolateTabTooltip");
					this.ImmolateShiftButton.Enable = true;
				}
				else
				{
					this.ImmolateShiftButton.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%ArmyLockedInBattleDescription");
				}
			}
		}
	}

	private void RefreshImmolated()
	{
		if (this.Immolated == null || this.Garrison == null || this.Garrison.Empire == null)
		{
			return;
		}
		this.Immolated.Visible = false;
		if (this.IsOtherEmpire || !this.Garrison.Empire.SimulationObject.Tags.Contains("FactionTraitBrokenLordsHeatWave"))
		{
			return;
		}
		Season currentSeason = this.seasonService.GetCurrentSeason();
		if (currentSeason.SeasonDefinition.SeasonType != Season.ReadOnlyHeatWave)
		{
			return;
		}
		if (this.selectedUnits.Count > 0)
		{
			List<Unit> list = new List<Unit>(this.selectedUnits);
			list.RemoveAll((Unit match) => !match.IsImmolableUnit() || !match.IsAlreadyImmolated());
			if (list.Count == this.selectedUnits.Count)
			{
				this.Immolated.Visible = true;
				if (!this.garrison.IsInEncounter)
				{
					this.Immolated.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%ImmolatedTabTooltip");
				}
				else
				{
					this.Immolated.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%ArmyLockedInBattleDescription");
				}
			}
		}
	}

	private bool CanRetrofitUnitsAndExplain()
	{
		this.RetrofitButtonPriceLabel.AgeTransform.Visible = false;
		if (this.IsOtherEmpire)
		{
			return false;
		}
		if (this.Garrison.IsInEncounter)
		{
			this.RetrofitButton.AgeTooltip.Content = "%ArmyLockedInBattleDescription";
			return false;
		}
		if (this.selectedUnits.Count == 0)
		{
			this.RetrofitButton.AgeTooltip.Content = "%ArmyEmptySelectionDescription";
			return false;
		}
		this.RetrofitComputeCandidates();
		if (this.retrofitableUnits.Count == 0)
		{
			this.RetrofitButton.AgeTooltip.Content = "%RetrofitTabNoCandidateDescription";
			return false;
		}
		ConstructionCost[] array = this.departmentOfDefense.GetRetrofitCosts(this.retrofitableUnits.ToArray());
		if (array.Length == 0)
		{
			array = new ConstructionCost[]
			{
				new ConstructionCost("EmpireMoney", 0f, true, false)
			};
		}
		AgeUtils.CleanLine(GuiFormater.FormatCost(this.Garrison.Empire, array, false, 1, null), ref this.monochromaticFormat);
		this.RetrofitButtonPriceLabel.Text = GuiFormater.FormatCost(this.Garrison.Empire, array, false, 0, null);
		this.RetrofitButtonPriceLabel.AgeTransform.Visible = this.CanDisplayRetrofitCost();
		for (int i = 0; i < this.retrofitableUnits.Count; i++)
		{
			DepartmentOfDefense.CheckRetrofitPrerequisitesResult checkRetrofitPrerequisitesResult = this.departmentOfDefense.CheckRetrofitPrerequisites(this.retrofitableUnits[i], array);
			if (checkRetrofitPrerequisitesResult == DepartmentOfDefense.CheckRetrofitPrerequisitesResult.GarrisonArmyIsInEncounter)
			{
				this.RetrofitButton.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%ArmyLockedInBattleDescription");
				return false;
			}
			if (checkRetrofitPrerequisitesResult == DepartmentOfDefense.CheckRetrofitPrerequisitesResult.GarrisonCityIsUnderSiege)
			{
				this.RetrofitButton.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%RetrofitTabGarrisonCityUnderSiegeDescription");
				return false;
			}
			if (checkRetrofitPrerequisitesResult == DepartmentOfDefense.CheckRetrofitPrerequisitesResult.RegionDoesntBelongToUs || checkRetrofitPrerequisitesResult == DepartmentOfDefense.CheckRetrofitPrerequisitesResult.WorldPositionIsNotValid)
			{
				this.RetrofitButton.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%RetrofitTabNotInOwnRegionDescription");
				return false;
			}
		}
		if (!this.DepartmentOfTheTreasury.CanAfford(array))
		{
			this.RetrofitButton.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%RetrofitTabCannotAffordDescription") + " : " + this.monochromaticFormat;
			return false;
		}
		this.RetrofitButton.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%RetrofitTabOKDescription") + " : " + this.monochromaticFormat;
		return true;
	}

	private bool CanSelectAllAndExplain()
	{
		if (this.IsOtherEmpire)
		{
			return false;
		}
		if (this.selectedUnits.Count < this.guiUnits.Count)
		{
			this.SelectAllButton.AgeTooltip.Content = "%SelectAllTabOKDescription";
			return true;
		}
		this.SelectAllButton.AgeTooltip.Content = "%SelectAllTabFullSelectionDescription";
		return false;
	}

	private bool CanDisbandUnitsAndExplain()
	{
		if ((this.IsOtherEmpire || this.DepartmentOfScience.CanTradeUnits(false)) && (this.selectedUnits.Count != 1 || !this.selectedUnits[0].CheckUnitAbility(UnitAbility.ReadonlyColonize, -1)))
		{
			return false;
		}
		if (this.selectedUnits.Count == 0)
		{
			this.DisbandButton.AgeTooltip.Content = "%ArmyEmptySelectionDescription";
			return false;
		}
		this.DisbandComputeCandidates();
		if (this.salableUnits.Count == 0)
		{
			this.DisbandButton.AgeTooltip.Content = "%DisbandTabNoCandidateDescription";
			return false;
		}
		if (!this.Garrison.IsInEncounter)
		{
			this.DisbandButton.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%DisbandTabOKDescription");
			return true;
		}
		this.DisbandButton.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%ArmyLockedInBattleDescription");
		return false;
	}

	private bool CanSellUnitsAndExplain()
	{
		this.SellButtonPriceLabel.AgeTransform.Visible = false;
		if (this.IsOtherEmpire || !this.DepartmentOfScience.CanTradeUnits(false))
		{
			return false;
		}
		if (this.selectedUnits.Count == 0)
		{
			this.SellButton.AgeTooltip.Content = "%ArmyEmptySelectionDescription";
			return false;
		}
		this.SellComputeCandidates();
		if (this.salableUnits.Count == 0)
		{
			this.SellButton.AgeTooltip.Content = "%SellTabNoCandidateDescription";
			return false;
		}
		float num = this.TotalSellPriceOfSalableUnits();
		string text = GuiFormater.FormatStock(num, DepartmentOfTheTreasury.Resources.EmpireMoney, 0, true);
		string formattedLine = GuiFormater.FormatInstantCost(this.Garrison.Empire, num, DepartmentOfTheTreasury.Resources.EmpireMoney, true, 1);
		this.SellButtonPriceLabel.Text = text;
		this.SellButtonPriceLabel.AgeTransform.Visible = true;
		AgeUtils.CleanLine(formattedLine, ref this.monochromaticFormat);
		if (!this.Garrison.IsInEncounter)
		{
			this.SellButton.AgeTooltip.Content = string.Format(AgeLocalizer.Instance.LocalizeString("%SellTabOKDescription"), this.monochromaticFormat);
			return true;
		}
		this.SellButton.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%ArmyLockedInBattleDescription");
		return false;
	}

	private void EndTurnService_GameClientStateChange(object sender, GameClientStateChangeEventArgs e)
	{
		if (base.IsVisible && this.playerControllerRepositoryService != null && this.playerControllerRepositoryService.ActivePlayerController != null)
		{
			bool flag = this.playerControllerRepositoryService.ActivePlayerController.CanSendOrders();
			if (this.interactionsAllowed != flag)
			{
				this.interactionsAllowed = flag;
				this.RefreshButtons();
				if (!this.interactionsAllowed && MessagePanel.Instance.IsVisible)
				{
					MessagePanel.Instance.Hide(false);
				}
			}
		}
	}

	private void DepartmentOfTheTreasury_ResourcePropertyChange(object sender, ResourcePropertyChangeEventArgs e)
	{
		if (base.IsVisible && !this.IsOtherEmpire && e.ResourcePropertyName == SimulationProperties.BankAccount)
		{
			this.RefreshButtons();
		}
	}

	private void DepartmentOfScience_TechnologyUnlocked(object sender, ConstructibleElementEventArgs e)
	{
		if (base.IsVisible && !this.IsOtherEmpire)
		{
			this.RefreshButtons();
		}
	}

	private void Garrison_EncounterChange(object sender, EncounterRepositoryChangeEventArgs e)
	{
		if (base.IsVisible && !this.IsOtherEmpire)
		{
			this.RefreshButtons();
		}
	}

	private void Garrison_HeroChange(object sender, HeroChangeEventArgs e)
	{
		if (base.IsVisible)
		{
			this.RefreshContent();
		}
	}

	private void Garrison_EncounterChange(object sender, EventArgs e)
	{
		if (base.IsVisible)
		{
			this.RefreshContent();
		}
	}

	private void Garrison_StandardUnitCollectionChange(object sender, CollectionChangeEventArgs e)
	{
		if (e.Action == CollectionChangeAction.Add)
		{
			Unit unit = e.Element as Unit;
			if (unit != null)
			{
				this.guiUnits.Add(new GuiUnit(unit, null));
				unit.Refreshed += this.Unit_Refreshed;
			}
		}
		else if (e.Action == CollectionChangeAction.Remove)
		{
			Unit removedUnit = e.Element as Unit;
			if (removedUnit != null)
			{
				removedUnit.Refreshed -= this.Unit_Refreshed;
				this.guiUnits.RemoveAll((GuiUnit guiUnit) => guiUnit.GUID == removedUnit.GUID);
			}
		}
		if (base.IsVisible)
		{
			this.RefreshContent();
		}
	}

	private void OnSelectionChange()
	{
		if (this.SelectionChange != null)
		{
			this.SelectionChange(this, new EventArgs());
		}
	}

	private void Unit_Refreshed(object sender)
	{
		this.RefreshContent();
	}

	private void RetrofitComputeCandidates()
	{
		this.retrofitableUnits.Clear();
		int i;
		for (i = 0; i < this.selectedUnits.Count; i++)
		{
			if (!(this.selectedUnits[i].Garrison is Army) || !(this.selectedUnits[i].Garrison as Army).HasCatspaw)
			{
				UnitDesign unitDesign = this.departmentOfDefense.UnitDesignDatabase.UserDefinedUnitDesigns.FirstOrDefault((UnitDesign design) => design.Model == this.selectedUnits[i].UnitDesign.Model);
				if (unitDesign != null && this.selectedUnits[i].UnitDesign != unitDesign)
				{
					this.retrofitableUnits.Add(this.selectedUnits[i]);
				}
			}
		}
	}

	private void SellComputeCandidates()
	{
		this.salableUnits.Clear();
		for (int i = 0; i < this.selectedUnits.Count; i++)
		{
			if (!this.selectedUnits[i].CheckUnitAbility(UnitAbility.ReadonlyUnsalable, -1))
			{
				this.salableUnits.Add(this.selectedUnits[i]);
			}
		}
	}

	private void DisbandComputeCandidates()
	{
		this.salableUnits.Clear();
		for (int i = 0; i < this.selectedUnits.Count; i++)
		{
			if (!this.selectedUnits[i].CheckUnitAbility(UnitAbility.ReadonlyColonize, -1) || this.departmentOfTheInterior.Cities.Count != 0)
			{
				if (!(this.selectedUnits[i].Garrison is Army) || !(this.selectedUnits[i].Garrison as Army).HasCatspaw)
				{
					this.salableUnits.Add(this.selectedUnits[i]);
				}
			}
		}
	}

	private float TotalSellPriceOfSalableUnits()
	{
		float num = 0f;
		for (int i = 0; i < this.salableUnits.Count; i++)
		{
			num += TradableUnit.GetPriceWithSalesTaxes(this.salableUnits[i], TradableTransactionType.Sellout, this.Garrison.Empire, 1f);
		}
		return num;
	}

	private void OnRetrofitUnitsConfirmation(object sender, MessagePanelResultEventArgs e)
	{
		if (e.Result == MessagePanelResult.Yes)
		{
			this.RetrofitSelectedUnits();
		}
	}

	private void OnSellUnitsConfirmation(object sender, MessagePanelResultEventArgs e)
	{
		if (e.Result == MessagePanelResult.Yes)
		{
			this.SellSelectedUnits();
		}
	}

	private void OnDisbandUnitsConfirmation(object sender, MessagePanelResultEventArgs e)
	{
		if (e.Result == MessagePanelResult.Yes)
		{
			this.DisbandSelectedUnits();
		}
	}

	public void OnELCPRightClick(GameObject obj)
	{
		if (!this.IsOtherEmpire)
		{
			UnitListPanel.<>c__DisplayClass0_0 CS$<>8__locals1 = new UnitListPanel.<>c__DisplayClass0_0();
			AgeControlToggle component = obj.GetComponent<AgeControlToggle>();
			CS$<>8__locals1.children = this.UnitsTable.GetChildren<UnitGuiItem>(true);
			int j;
			int i;
			for (i = 0; i < CS$<>8__locals1.children.Count; i = j + 1)
			{
				if (CS$<>8__locals1.children[i].UnitToggle == component && CS$<>8__locals1.children[i].GuiUnit.UnitDesign != null && this.departmentOfDefense.AvailableUnitDesigns.Find((UnitDesign unitDesign) => unitDesign.Model == CS$<>8__locals1.children[i].GuiUnit.UnitDesign.Model) != null)
				{
					base.GuiService.GetGuiPanel<UnitDesignModalPanel>().CreateMode = false;
					base.GuiService.GetGuiPanel<UnitDesignModalPanel>().Show(new object[]
					{
						CS$<>8__locals1.children[i].GuiUnit.UnitDesign
					});
				}
				j = i;
			}
		}
	}

	public AgePrimitiveLabel Title;

	public AgeControlScrollView UnitsScrollview;

	public AgeTransform UnitsTable;

	public AgeTransform SelectAllButton;

	public AgeTransform RetrofitButton;

	public AgePrimitiveLabel RetrofitButtonPriceLabel;

	public AgeTransform DisbandButton;

	public AgeTransform SellButton;

	public AgePrimitiveLabel SellButtonPriceLabel;

	public AgeTransform HealButton;

	public AgePrimitiveLabel HealButtonPriceLabel;

	public AgeTransform ForceShiftButton;

	public AgePrimitiveLabel ForceShiftButtonPriceLabel;

	public AgeTransform ImmolateShiftButton;

	public AgeTransform Immolated;

	public Transform UnitPrefab;

	protected List<Unit> selectedUnits;

	protected List<Unit> retrofitableUnits;

	protected List<Unit> salableUnits;

	protected IGarrison garrison;

	protected IGarrison militia;

	protected GameObject parent;

	protected bool interactionsAllowed = true;

	private List<GuiUnit> guiUnits;

	private GarrisonWorldCursor garrisonWorldCursor;

	private DepartmentOfDefense departmentOfDefense;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private DepartmentOfScience departmentOfScience;

	private IPlayerControllerRepositoryService playerControllerRepositoryService;

	private ISeasonService seasonService;

	private IEndTurnService endTurnService;

	private StringBuilder monochromaticFormat = new StringBuilder();

	private AgeTransform.RefreshTableItem<GuiUnit> refreshGuiUnitDelegate;
}
