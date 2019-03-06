using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using UnityEngine;

public class GameAcademyScreen : GuiPlayerControllerScreen
{
	public GameAcademyScreen()
	{
		base.EnableCaptureBackgroundOnShow = true;
		base.EnableMenuAudioLayer = true;
		base.EnableNavigation = true;
	}

	public bool FromMilitaryScreen { get; set; }

	private DepartmentOfEducation DepartmentOfEducation
	{
		get
		{
			return this.departmentOfEducation;
		}
		set
		{
			if (this.departmentOfEducation != null)
			{
				this.departmentOfEducation.HeroCollectionChange -= this.DepartmentOfEducation_HeroCollectionChange;
			}
			this.departmentOfEducation = value;
			if (this.departmentOfEducation != null)
			{
				this.departmentOfEducation.HeroCollectionChange += this.DepartmentOfEducation_HeroCollectionChange;
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

	private ITradeManagementService Marketplace
	{
		get
		{
			return this.marketplace;
		}
		set
		{
			if (this.marketplace != null)
			{
				this.marketplace.CollectionChange -= this.Marketplace_CollectionChanged;
			}
			this.marketplace = value;
			if (this.marketplace != null)
			{
				this.marketplace.CollectionChange += this.Marketplace_CollectionChanged;
			}
		}
	}

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

	private Unit SelectedHero
	{
		get
		{
			return this.selectedHero;
		}
		set
		{
			if (this.selectedHero != null)
			{
				this.selectedHero.Refreshed -= this.Hero_Refreshed;
				if (this.selectedHero.Garrison != null)
				{
					this.selectedHero.Garrison.EncounterChange -= this.Garrison_EncounterChange;
				}
				this.selectedHeroIndex = -1;
			}
			this.selectedHero = value;
			if (this.selectedHero != null)
			{
				this.selectedHero.Refreshed += this.Hero_Refreshed;
				if (this.selectedHero.Garrison != null)
				{
					this.selectedHero.Garrison.EncounterChange += this.Garrison_EncounterChange;
				}
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
		this.DepartmentOfEducation = base.Empire.GetAgency<DepartmentOfEducation>();
		this.DepartmentOfTheTreasury = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		this.DepartmentOfScience = base.Empire.GetAgency<DepartmentOfScience>();
		this.Marketplace = base.Game.Services.GetService<ITradeManagementService>();
		this.SelectedHero = null;
		base.NeedRefresh = true;
	}

	public override bool HandleNextRequest()
	{
		this.OnNextHeroCB(null);
		return true;
	}

	public override bool HandlePreviousRequest()
	{
		this.OnPreviousHeroCB(null);
		return true;
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		if (base.Empire == null)
		{
			return;
		}
		for (int i = 0; i < this.HeroesTable.GetChildren().Count; i++)
		{
			this.HeroesTable.GetChildren()[i].GetComponent<HeroCard>().Unbind();
		}
		DepartmentOfEducation agency = base.Empire.GetAgency<DepartmentOfEducation>();
		this.heroesList.Clear();
		this.heroesList.AddRange(agency.Heroes);
		if (agency.Prisoners != null && agency.Prisoners.Count > 0)
		{
			for (int j = 0; j < agency.Prisoners.Count; j++)
			{
				IGameEntity gameEntity;
				if (!this.gameEntityRepositoryService.TryGetValue(agency.Prisoners[j].UnitGuid, out gameEntity))
				{
					Diagnostics.LogError("GuiNotificationHeroBase.LoadHero failed because the target game entity is not valid.");
				}
				else if (!(gameEntity is Unit))
				{
					Diagnostics.LogError("GuiNotificationHeroBase.LoadHero failed because the target game entity is not a unit.");
				}
				else
				{
					this.heroesList.Add(gameEntity as Unit);
				}
			}
		}
		this.heroesList.Sort(delegate(Unit hero1, Unit hero2)
		{
			bool flag = hero1.GetPropertyValue(SimulationProperties.MaximumSkillPoints) - hero1.GetPropertyValue(SimulationProperties.SkillPointsSpent) > 0f;
			bool flag2 = hero2.GetPropertyValue(SimulationProperties.MaximumSkillPoints) - hero2.GetPropertyValue(SimulationProperties.SkillPointsSpent) > 0f;
			if (flag && !flag2)
			{
				return -1;
			}
			if (!flag && flag2)
			{
				return 1;
			}
			if (hero1.Garrison == null && hero2.Garrison != null)
			{
				return -1;
			}
			if (hero1.Garrison != null && hero2.Garrison == null)
			{
				return 1;
			}
			return hero1.GUID.CompareTo(hero2.GUID);
		});
		this.HeroesTable.DestroyAllChildren();
		this.HeroesTable.ReserveChildren(this.heroesList.Count, this.HeroCardPrefab, "HeroCard");
		this.HeroesTable.RefreshChildrenIList<Unit>(this.heroesList, this.refreshHeroCardDelegate, true, false);
		List<HeroCard> children = this.HeroesTable.GetChildren<HeroCard>(true);
		if (children.Count > 0)
		{
			float num = children[0].AgeTransform.Width + this.HeroesTable.HorizontalMargin + this.HeroesTable.HorizontalSpacing;
			this.HeroesTable.Width = num * (float)children.Count;
		}
		if (this.heroesList.Count > 0)
		{
			if (this.SelectedHero == null)
			{
				this.SelectHeroCardAtIndex(Mathf.Min(this.heroesList.Count - 1, 2));
			}
			else
			{
				this.SelectHeroCardAtIndex(this.heroesList.FindIndex((Unit hero) => hero.GUID == this.SelectedHero.GUID));
			}
		}
		this.RefreshButtons();
	}

	public override void Unbind()
	{
		this.SelectedHero = null;
		for (int i = 0; i < this.HeroesTable.GetChildren().Count; i++)
		{
			this.HeroesTable.GetChildren()[i].GetComponent<HeroCard>().Unbind();
		}
		this.DepartmentOfEducation = null;
		this.DepartmentOfTheTreasury = null;
		this.DepartmentOfScience = null;
		this.Marketplace = null;
		this.SelectedHero = null;
		base.Unbind();
	}

	internal void OnNextHero(GameObject obj = null, int direction = 1)
	{
		int num = this.selectedHeroIndex + direction;
		if (num >= 0 && num < this.HeroesTable.GetChildren<HeroCard>(true).Count - 1)
		{
			this.SelectHeroCardAtIndex(num);
			this.RefreshButtons();
		}
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		base.UseRefreshLoop = true;
		this.refreshHeroCardDelegate = new AgeTransform.RefreshTableItem<Unit>(this.RefreshHeroCard);
		IGuiPanelHelper guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
		Diagnostics.Assert(guiPanelHelper != null, "Unable to access GuiPanelHelper");
		yield break;
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		this.EndTurnService = Services.GetService<IEndTurnService>();
		this.FromMilitaryScreen = false;
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		this.gameEntityRepositoryService = gameService.Game.Services.GetService<IGameEntityRepositoryService>();
		Diagnostics.Assert(this.gameEntityRepositoryService != null);
		this.AssignAsSpyButton.AgeTransform.Visible = false;
		IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
		if (downloadableContentService != null && downloadableContentService.IsShared(DownloadableContent11.ReadOnlyName))
		{
			this.AssignAsSpyButton.AgeTransform.Visible = true;
			this.AssignAsSpyButton.AgeTransform.Enable = true;
		}
		if (this.AssignAsSpyButton.AgeTransform.Visible)
		{
			this.ButtonsGroup.Height = this.ButtonsGroupExtendedHeight * AgeUtils.CurrentUpscaleFactor();
		}
		else
		{
			this.ButtonsGroup.Height = this.ButtonsGroupStandardHeight * AgeUtils.CurrentUpscaleFactor();
		}
		yield break;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		base.NeedRefresh = false;
		base.GuiService.GetGuiPanel<ControlBanner>().OnHideScreen(GameScreenType.Academy);
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		base.AgeTransform.Enable = true;
		this.interactionsAllowed = base.PlayerController.CanSendOrders();
		base.GuiService.GetGuiPanel<ControlBanner>().OnShowScreen(GameScreenType.Academy);
		base.GuiService.Show(typeof(EmpireBannerPanel), new object[]
		{
			EmpireBannerPanel.Full
		});
		if (parameters == null || parameters.Length == 0)
		{
			this.SelectedHero = null;
		}
		else
		{
			this.SelectedHero = (parameters[0] as Unit);
		}
		base.NeedRefresh = true;
		yield break;
	}

	protected override void OnUnload()
	{
		this.refreshHeroCardDelegate = null;
		base.OnUnload();
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.Hide(true);
		this.Unbind();
		this.EndTurnService = null;
		this.gameEntityRepositoryService = null;
		this.SelectedHero = null;
		this.HeroesTable.DestroyAllChildren();
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
				if (!this.interactionsAllowed && MessagePanel.Instance.IsVisible)
				{
					MessagePanel.Instance.Hide(false);
				}
			}
		}
	}

	private void DepartmentOfTheTreasury_ResourcePropertyChange(object sender, ResourcePropertyChangeEventArgs e)
	{
		if (base.IsVisible && e.ResourcePropertyName == SimulationProperties.BankAccount)
		{
			this.RefreshButtons();
		}
	}

	private void DepartmentOfEducation_HeroCollectionChange(object sender, CollectionChangeEventArgs e)
	{
		if (base.IsVisible)
		{
			this.SelectedHero = null;
			base.NeedRefresh = true;
		}
	}

	private void DepartmentOfScience_TechnologyUnlocked(object sender, ConstructibleElementEventArgs e)
	{
		if (base.IsVisible)
		{
			this.RefreshButtons();
		}
	}

	private void Garrison_EncounterChange(object sender, EventArgs e)
	{
		if (base.IsVisible)
		{
			this.RefreshButtons();
		}
	}

	private void Marketplace_CollectionChanged(object sender, TradableCollectionChangeEventArgs e)
	{
		if (base.IsVisible)
		{
			this.RefreshBuyHeroesButton();
		}
	}

	private void RefreshHeroCard(AgeTransform tableitem, Unit hero, int index)
	{
		HeroCard component = tableitem.GetComponent<HeroCard>();
		component.UseEmpireColor = true;
		global::Empire owner = base.Empire;
		DepartmentOfEducation agency = base.Empire.GetAgency<DepartmentOfEducation>();
		if (agency.Prisoners != null && agency.Prisoners.Count > 0)
		{
			for (int i = 0; i < agency.Prisoners.Count; i++)
			{
				if (agency.Prisoners[i].UnitGuid == hero.GUID)
				{
					IGameService service = Services.GetService<IGameService>();
					global::Game game = service.Game as global::Game;
					owner = game.Empires[agency.Prisoners[i].OwnerEmpireIndex];
				}
			}
		}
		component.Bind(hero, owner, base.gameObject);
		component.RefreshContent(false, false);
		AgeModifierColorSwitch component2 = component.SelectionFrame.GetComponent<AgeModifierColorSwitch>();
		component2.OnColor = Color.clear;
	}

	private void Hero_Refreshed(object sender)
	{
		this.SelectedHero = this.SelectedHero;
		if (base.IsVisible)
		{
			this.RefreshButtons();
		}
	}

	private void OnAssignAsSpyCB(GameObject obj)
	{
		base.GuiService.Show(typeof(ForeignCitiesModalPanel), new object[]
		{
			base.gameObject,
			this.SelectedHero
		});
	}

	private void OnAssignHeroToArmyCB(GameObject obj)
	{
		base.GuiService.Show(typeof(ArmySelectionModalPanel), new object[]
		{
			base.gameObject,
			"HeroAssignement"
		});
	}

	private void OnAssignHeroToCityCB(GameObject obj)
	{
		this.cityListPurpose = "HeroAssignement";
		base.GuiService.Show(typeof(CitySelectionModalPanel), new object[]
		{
			base.gameObject,
			this.cityListPurpose,
			this.SelectedHero
		});
	}

	private void OnCreateArmyInCityCB(GameObject obj)
	{
		this.cityListPurpose = "HeroArmyCreation";
		base.GuiService.Show(typeof(CitySelectionModalPanel), new object[]
		{
			base.gameObject,
			this.cityListPurpose
		});
	}

	private void OnBuyHeroCB(GameObject obj)
	{
		base.GuiService.GetGuiPanel<GameMarketplaceScreen>().Show(new object[]
		{
			"Hero"
		});
	}

	private void OnCloseCB(GameObject obj)
	{
		this.HandleCancelRequest();
	}

	private void OnForeignCitySelected(City city)
	{
		IGameService service = Services.GetService<IGameService>();
		IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
		base.AgeTransform.Enable = false;
		OrderToggleInfiltration order = new OrderToggleInfiltration(base.Empire.Index, this.SelectedHero.GUID, city.GUID, false, true);
		Ticket ticket;
		service2.ActivePlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
	}

	private void OnHealHeroCB(GameObject control)
	{
		if (this.SelectedHero != null && this.SelectedHero.IsWounded() && this.SelectedHero.CheckUnitAbility(UnitAbility.UnitAbilityInstantHeal, -1) && this.SelectedHero.HasEnoughActionPointLeft(1))
		{
			base.AgeTransform.Enable = false;
			Unit[] units = new Unit[]
			{
				this.SelectedHero
			};
			OrderHealUnits order = new OrderHealUnits(base.Empire.Index, units);
			Ticket ticket;
			base.PlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
		}
	}

	private void OnForceShiftCB(GameObject obj)
	{
		if (this.selectedHero != null && this.selectedHero.IsShifter() && this.selectedHero.IsInCurrentSeasonForm())
		{
			DepartmentOfScience agency = this.selectedHero.Garrison.Empire.GetAgency<DepartmentOfScience>();
			if (agency == null || agency.GetTechnologyState("TechnologyDefinitionOrbUnlock17WinterShifters") != DepartmentOfScience.ConstructibleElement.State.Researched)
			{
				return;
			}
			OrderForceShiftUnits order = new OrderForceShiftUnits(base.Empire.Index, new GameEntityGUID[]
			{
				this.selectedHero.GUID
			});
			IPlayerControllerRepositoryService service = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
			Ticket ticket;
			service.ActivePlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
			EventForceShift eventToNotify = new EventForceShift(base.Empire, 1, true);
			IEventService service2 = Services.GetService<IEventService>();
			if (service2 != null)
			{
				service2.Notify(eventToNotify);
			}
		}
	}

	private void OnHeroToggle(GameObject control)
	{
		AgeControlToggle component = control.GetComponent<AgeControlToggle>();
		List<HeroCard> children = this.HeroesTable.GetChildren<HeroCard>(true);
		for (int i = 0; i < children.Count; i++)
		{
			if (children[i].HeroToggle == component)
			{
				this.SelectHeroCardAtIndex(i);
			}
			else
			{
				children[i].HeroToggle.State = false;
			}
		}
		this.RefreshButtons();
	}

	private void OnDoubleClickCB(GameObject control)
	{
		this.OnInspectHeroCB(control);
	}

	private void OnInspectHeroCB(GameObject control)
	{
		if ((this.SelectedHero.Garrison != null && this.SelectedHero.Garrison.IsInEncounter) || DepartmentOfEducation.IsCaptured(this.SelectedHero))
		{
			base.GuiService.GetGuiPanel<HeroInspectionModalPanel>().Show(new object[]
			{
				this.SelectedHero,
				"readonly"
			});
		}
		else
		{
			base.GuiService.GetGuiPanel<HeroInspectionModalPanel>().Show(new object[]
			{
				this.SelectedHero
			});
		}
	}

	private void OnNextHeroCB(GameObject obj = null)
	{
		if (this.selectedHeroIndex < this.HeroesTable.GetChildren<HeroCard>(true).Count - 1)
		{
			this.SelectHeroCardAtIndex(this.selectedHeroIndex + 1);
		}
		this.RefreshButtons();
	}

	private void OnPostOrderReleasePrisoner(object sender, MessagePanelResultEventArgs e)
	{
		if (e.Result == MessagePanelResult.Yes)
		{
			base.AgeTransform.Enable = false;
			OrderReleasePrisoner order = new OrderReleasePrisoner(base.Empire.Index, this.SelectedHero.GUID);
			Ticket ticket;
			base.PlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
		}
	}

	private void OnPreviousHeroCB(GameObject obj = null)
	{
		if (this.selectedHeroIndex > 0)
		{
			this.SelectHeroCardAtIndex(this.selectedHeroIndex - 1);
		}
		this.RefreshButtons();
	}

	private void OnRestoreHeroCB(GameObject control)
	{
		base.AgeTransform.Enable = false;
		OrderRestoreHero order = new OrderRestoreHero(base.Empire.Index, this.SelectedHero.GUID);
		Ticket ticket;
		base.PlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
	}

	private void OnDismissHeroCB(GameObject control)
	{
		string message = AgeLocalizer.Instance.LocalizeString("%AcademyConfirmHeroDismiss");
		MessagePanel.Instance.Show(message, "%Confirmation", MessagePanelButtons.YesNo, new MessagePanel.EventHandler(this.OnSellHeroConfirmation), MessagePanelType.INFORMATIVE, new MessagePanelButton[0]);
	}

	private void OnReleasePrisonerCB(GameObject control)
	{
		MessagePanel.Instance.Show("%AcademyPrisonerReleaseConfirmation", "%Confirmation", MessagePanelButtons.YesNo, new MessagePanel.EventHandler(this.OnPostOrderReleasePrisoner), MessagePanelType.INFORMATIVE, new MessagePanelButton[0]);
	}

	private void OnSellHeroCB(GameObject control)
	{
		string message = AgeLocalizer.Instance.LocalizeString("%AcademyConfirmHeroSell");
		MessagePanel.Instance.Show(message, "%Confirmation", MessagePanelButtons.YesNo, new MessagePanel.EventHandler(this.OnSellHeroConfirmation), MessagePanelType.INFORMATIVE, new MessagePanelButton[0]);
	}

	private void OnSellHeroConfirmation(object sender, MessagePanelResultEventArgs e)
	{
		if (e.Result == MessagePanelResult.Yes)
		{
			base.AgeTransform.Enable = false;
			OrderSelloutTradableHero order = new OrderSelloutTradableHero(base.Empire.Index, this.SelectedHero.GUID);
			Ticket ticket;
			base.PlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
			this.SelectedHero = null;
		}
	}

	private void OnUnassignHeroCB(GameObject control)
	{
		base.AgeTransform.Enable = false;
		OrderChangeHeroAssignment order = new OrderChangeHeroAssignment(base.Empire.Index, this.SelectedHero.GUID, GameEntityGUID.Zero);
		Ticket ticket;
		base.PlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
	}

	private void RefreshButtons()
	{
		if (this.SelectedHero != null)
		{
			this.PreviousButton.AgeTransform.Enable = (this.selectedHeroIndex > 0);
			this.NextButton.AgeTransform.Enable = (this.selectedHeroIndex < this.HeroesTable.GetChildren().Count - 1);
		}
		this.ArrowDown.Visible = (this.SelectedHero != null);
		this.InspectButton.AgeTransform.Enable = (this.SelectedHero != null);
		this.RefreshHealButton();
		this.RefreshForceShiftingButton();
		this.RefreshRestoreButton();
		if (this.DepartmentOfScience.CanTradeHeroes(false))
		{
			this.DismissButton.AgeTransform.Visible = false;
			this.SellButton.AgeTransform.Visible = true;
			this.RefreshSellButton();
		}
		else
		{
			this.DismissButton.AgeTransform.Visible = true;
			this.SellButton.AgeTransform.Visible = false;
			this.RefreshDismissButton();
		}
		this.RefreshAssignementButtons();
		if (this.DepartmentOfScience.CanTradeHeroes(false))
		{
			this.HeroesMarketLockedGroup.Visible = false;
			this.BuyHeroesButtonGroup.Visible = true;
			this.RefreshBuyHeroesButton();
		}
		else
		{
			this.HeroesMarketLockedGroup.Visible = true;
			this.BuyHeroesButtonGroup.Visible = false;
			DepartmentOfForeignAffairs agency = base.Empire.GetAgency<DepartmentOfForeignAffairs>();
			GuiElement guiElement;
			if (agency != null && agency.IsBannedFromMarket())
			{
				this.HeroesMarketLockedGroup.AgeTooltip.Content = "%AcademyBannedFromMarketplaceDescription";
			}
			else if (base.GuiService.GuiPanelHelper.TryGetGuiElement("TechnologyDefinitionMarketplaceMercenaries", out guiElement))
			{
				string arg = AgeLocalizer.Instance.LocalizeString(guiElement.Title);
				this.HeroesMarketLockedGroup.AgeTooltip.Content = string.Format(AgeLocalizer.Instance.LocalizeString("%AcademyCannotTradeHeroesDescription"), arg);
			}
		}
	}

	private void RefreshBuyHeroesButton()
	{
		int num = 0;
		List<TradableCategoryDefinition> list = new List<TradableCategoryDefinition>();
		this.Marketplace.TryGetTradableCategories(out list);
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].SubCategory == "Hero")
			{
				List<ITradable> list2;
				this.Marketplace.TryGetTradables(list[i].Name, out list2);
				if (list2 != null)
				{
					int num2 = 1 << base.Empire.Index;
					for (int j = list2.Count - 1; j >= 0; j--)
					{
						if ((list2[j].EmpireExclusionBits & num2) != 0)
						{
							list2.RemoveAt(j);
						}
					}
					num += list2.Count;
				}
			}
		}
		this.BuyHeroesButtonGroup.Enable = (num > 0);
		this.BuyHeroesButtonLabel.Text = AgeLocalizer.Instance.LocalizeString("%AcademyHeroesAvailableForSaleTitle").Replace("$HeroesCount", num.ToString());
	}

	private void RefreshHealButton()
	{
		if (this.SelectedHero == null || DepartmentOfEducation.IsInjured(this.SelectedHero) || !this.SelectedHero.CheckUnitAbility(UnitAbility.UnitAbilityInstantHeal, -1) || DepartmentOfEducation.IsCaptured(this.SelectedHero))
		{
			this.HealButton.AgeTransform.Visible = false;
			return;
		}
		this.HealButton.AgeTransform.Visible = true;
		if (!this.SelectedHero.IsWounded())
		{
			this.HealButton.AgeTransform.Enable = false;
			this.HealCostLabel.AgeTransform.Visible = false;
			this.HealButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%AcademyHealHeroNotWoundedDescription");
			return;
		}
		Unit[] unitToHeal = new Unit[]
		{
			this.SelectedHero
		};
		ConstructionCost[] unitHealCost = this.DepartmentOfTheTreasury.GetUnitHealCost(unitToHeal);
		this.HealCostLabel.Text = GuiFormater.FormatCost(base.Empire, unitHealCost, false, 0, null);
		this.HealCostLabel.AgeTransform.Visible = true;
		StringBuilder stringBuilder = new StringBuilder();
		AgeUtils.CleanLine(GuiFormater.FormatCost(base.Empire, unitHealCost, false, 1, null), ref stringBuilder);
		if (!this.selectedHero.HasEnoughActionPointLeft(1))
		{
			this.HealButton.AgeTransform.Enable = false;
			this.HealButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%HealTabNoActionPointDescription") + " : " + stringBuilder;
		}
		else if (this.SelectedHero.Garrison != null && this.SelectedHero.Garrison.IsInEncounter)
		{
			this.HealButton.AgeTransform.Enable = false;
			this.HealButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%AcademyHeroLockedInBattleDescription");
		}
		else if (this.DepartmentOfTheTreasury.CanAfford(unitHealCost))
		{
			this.HealButton.AgeTransform.Enable = this.interactionsAllowed;
			this.HealButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%AcademyHealFormat").Replace("$Value", stringBuilder.ToString());
		}
		else
		{
			this.HealButton.AgeTransform.Enable = false;
			this.HealButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%AcademyHealCannotAffordDescription") + " : " + stringBuilder;
		}
	}

	private void RefreshForceShiftingButton()
	{
		if (this.SelectedHero == null || this.SelectedHero.Garrison == null || !this.SelectedHero.IsShifter() || DepartmentOfEducation.IsInjured(this.SelectedHero) || DepartmentOfEducation.IsCaptured(this.SelectedHero))
		{
			this.ForceShiftingButton.AgeTransform.Visible = false;
			return;
		}
		DepartmentOfScience agency = this.selectedHero.Garrison.Empire.GetAgency<DepartmentOfScience>();
		if (agency == null || agency.GetTechnologyState("TechnologyDefinitionOrbUnlock17WinterShifters") != DepartmentOfScience.ConstructibleElement.State.Researched)
		{
			this.ForceShiftingButton.AgeTransform.Visible = false;
			return;
		}
		this.ForceShiftingButton.AgeTransform.Visible = true;
		if (!this.selectedHero.IsInCurrentSeasonForm())
		{
			this.ForceShiftingButton.AgeTransform.Enable = false;
			return;
		}
		ConstructionCost[] unitForceShiftingCost = this.DepartmentOfTheTreasury.GetUnitForceShiftingCost(new Unit[]
		{
			this.selectedHero
		});
		this.ForceShiftCostLabel.Text = GuiFormater.FormatCost(base.Empire, unitForceShiftingCost, false, 0, null);
		this.ForceShiftCostLabel.AgeTransform.Visible = true;
		StringBuilder stringBuilder = new StringBuilder();
		AgeUtils.CleanLine(GuiFormater.FormatCost(base.Empire, unitForceShiftingCost, false, 1, null), ref stringBuilder);
		if (this.SelectedHero.Garrison != null && this.SelectedHero.Garrison.IsInEncounter)
		{
			this.ForceShiftingButton.AgeTransform.Enable = false;
			this.ForceShiftingButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%AcademyHeroLockedInBattleDescription");
		}
		else if (this.DepartmentOfTheTreasury.CanAfford(unitForceShiftingCost))
		{
			this.ForceShiftingButton.AgeTransform.Enable = this.interactionsAllowed;
			this.ForceShiftingButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%AcademyForceShiftFormat").Replace("$Value", stringBuilder.ToString());
		}
		else
		{
			this.ForceShiftingButton.AgeTransform.Enable = false;
			this.ForceShiftingButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%AcademyForceShiftCannotAffordDescription") + " : " + stringBuilder;
		}
	}

	private void RefreshRestoreButton()
	{
		if (this.SelectedHero == null)
		{
			this.RestoreButton.AgeTransform.Enable = false;
			this.RestoreCostLabel.AgeTransform.Visible = false;
			return;
		}
		if (!DepartmentOfEducation.IsInjured(this.SelectedHero) || DepartmentOfEducation.IsCaptured(this.SelectedHero))
		{
			this.RestoreButton.AgeTransform.Enable = false;
			this.RestoreCostLabel.AgeTransform.Visible = false;
			this.RestoreButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%AcademyRestoreHeroNotDisabledDescription");
			return;
		}
		ConstructionCost[] empireMoneyRestoreCost = DepartmentOfEducation.GetEmpireMoneyRestoreCost(this.SelectedHero);
		StringBuilder arg = new StringBuilder();
		AgeUtils.CleanLine(GuiFormater.FormatCost(base.Empire, empireMoneyRestoreCost, false, 1, null), ref arg);
		this.RestoreCostLabel.Text = GuiFormater.FormatCost(base.Empire, empireMoneyRestoreCost, false, 1, null);
		this.RestoreCostLabel.AgeTransform.Visible = true;
		if (this.DepartmentOfTheTreasury.CanAfford(empireMoneyRestoreCost))
		{
			this.RestoreButton.AgeTransform.Enable = this.interactionsAllowed;
			this.RestoreButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%AcademyRestoreDescription");
		}
		else
		{
			this.RestoreButton.AgeTransform.Enable = false;
			this.RestoreButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%AcademyRestoreCannotAffordDescription") + " : " + arg;
		}
	}

	private void RefreshDismissButton()
	{
		if (this.SelectedHero == null)
		{
			this.DismissButton.AgeTransform.Enable = false;
			return;
		}
		if (this.SelectedHero.CheckUnitAbility(UnitAbility.ReadonlyUnsalable, -1) || DepartmentOfEducation.IsCaptured(this.SelectedHero))
		{
			this.DismissButton.AgeTransform.Enable = false;
			this.DismissButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%AcademyUndismissableOrSalableHeroDescription");
			return;
		}
		if (!DepartmentOfEducation.CanAssignHeroTo(this.SelectedHero, null))
		{
			this.DismissButton.AgeTransform.Enable = false;
			if (DepartmentOfEducation.IsInjured(this.SelectedHero))
			{
				this.DismissButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%AcademyRestoreBeforeDismissalOrSaleDescription");
			}
			else if (DepartmentOfEducation.IsLocked(this.SelectedHero))
			{
				this.DismissButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%AcademyUnlockBeforeDismissalOrSaleDescription");
			}
			else if (this.SelectedHero.Garrison != null && this.SelectedHero.Garrison.IsInEncounter)
			{
				this.DismissButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%AcademyHeroLockedInBattleDescription");
			}
			return;
		}
		this.DismissButton.AgeTransform.Enable = this.interactionsAllowed;
		this.DismissButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%AcademyDismissDescription");
	}

	private void RefreshSellButton()
	{
		if (this.SelectedHero == null)
		{
			this.SellButton.AgeTransform.Enable = false;
			this.SellButtonPriceLabel.AgeTransform.Visible = false;
			return;
		}
		if (this.SelectedHero.CheckUnitAbility(UnitAbility.ReadonlyUnsalable, -1) || DepartmentOfEducation.IsCaptured(this.SelectedHero))
		{
			this.SellButton.AgeTransform.Enable = false;
			this.SellButtonPriceLabel.AgeTransform.Visible = false;
			this.SellButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%AcademyUndismissableOrSalableHeroDescription");
			return;
		}
		float priceWithSalesTaxes = TradableUnit.GetPriceWithSalesTaxes(this.SelectedHero, TradableTransactionType.Sellout, base.Empire, 1f);
		this.SellButtonPriceLabel.Text = GuiFormater.FormatInstantCost(base.Empire, priceWithSalesTaxes, DepartmentOfTheTreasury.Resources.EmpireMoney, true, 1);
		this.SellButtonPriceLabel.AgeTransform.Visible = true;
		if (!DepartmentOfEducation.CanAssignHeroTo(this.SelectedHero, null))
		{
			this.SellButton.AgeTransform.Enable = false;
			if (DepartmentOfEducation.IsInjured(this.SelectedHero))
			{
				this.SellButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%AcademyRestoreBeforeDismissalOrSaleDescription");
			}
			else if (DepartmentOfEducation.IsLocked(this.SelectedHero))
			{
				this.SellButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%AcademyUnlockBeforeDismissalOrSaleDescription");
			}
			else if (this.SelectedHero.Garrison != null && this.SelectedHero.Garrison.IsInEncounter)
			{
				this.SellButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%AcademyHeroLockedInBattleDescription");
			}
			return;
		}
		this.SellButton.AgeTransform.Enable = this.interactionsAllowed;
		this.SellButton.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%AcademySellDescription");
	}

	private void RefreshAssignementButtons()
	{
		if (this.SelectedHero == null)
		{
			this.ShowAssignButtons(false);
		}
		else
		{
			bool flag = DepartmentOfEducation.IsInjured(this.SelectedHero);
			global::Empire empire = null;
			for (int i = 0; i < this.HeroesTable.GetChildren().Count; i++)
			{
				HeroCard component = this.HeroesTable.GetChildren()[i].GetComponent<HeroCard>();
				if (component.Hero == this.SelectedHero)
				{
					empire = component.Owner;
					break;
				}
			}
			if (DepartmentOfEducation.IsCaptured(this.SelectedHero))
			{
				GuiHero guiHero = new GuiHero(this.SelectedHero, null);
				if (guiHero.IsCaptured(empire))
				{
					this.ShowLockedLabel((float)this.departmentOfEducation.GetNumberOfTurnBeforeRelease(this.selectedHero.GUID), true);
				}
				else
				{
					DepartmentOfEducation agency = empire.GetAgency<DepartmentOfEducation>();
					this.ShowPrisonerButton((float)agency.GetNumberOfTurnBeforeRelease(this.selectedHero.GUID), true);
				}
			}
			else if (DepartmentOfEducation.IsLocked(this.selectedHero))
			{
				this.ShowLockedLabel((float)DepartmentOfEducation.LockedRemainingTurns(this.selectedHero), false);
			}
			else
			{
				IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
				bool flag2 = true;
				if (service != null && service.IsShared(DownloadableContent11.ReadOnlyName))
				{
					DepartmentOfIntelligence agency2 = empire.GetAgency<DepartmentOfIntelligence>();
					IGarrison garrison = null;
					InfiltrationProcessus.InfiltrationState infiltrationState;
					if (agency2.TryGetGarrisonForSpy(this.selectedHero, out garrison, out infiltrationState) && infiltrationState == InfiltrationProcessus.InfiltrationState.OnGoing)
					{
						this.ShowPendingInfiltrationLabel((float)DepartmentOfIntelligence.InfiltrationRemainingTurns(this.SelectedHero));
						flag2 = false;
					}
				}
				if (flag2)
				{
					if (this.SelectedHero.Garrison == null)
					{
						this.ShowAssignButtons(!flag);
					}
					else
					{
						this.ShowUnassignButton();
					}
				}
			}
		}
	}

	private void ShowAssignButtons(bool enable)
	{
		this.UnassignGroup.Visible = false;
		this.LockedGroup.Visible = false;
		this.ReleasePrisonerButton.AgeTransform.Visible = false;
		this.AssignGroup.Visible = true;
		this.AssignGroup.Enable = (enable && this.interactionsAllowed);
		if (TutorialManager.IsActivated)
		{
			ITutorialService service = TutorialManager.GetService();
			if (service != null)
			{
				this.AssignGroup.Enable &= service.GetValue<bool>(TutorialManager.EnableHeroAssignmentKey, true);
			}
		}
		this.TmpCreateArmyInCityButton.AgeTransform.Enable = enable;
	}

	private void ShowUnassignButton()
	{
		this.AssignGroup.Visible = false;
		this.LockedGroup.Visible = false;
		this.ReleasePrisonerButton.AgeTransform.Visible = false;
		this.UnassignGroup.Visible = true;
		if (this.SelectedHero.Garrison != null && this.SelectedHero.Garrison.IsInEncounter)
		{
			this.UnassignGroup.Enable = false;
			this.UnassignGroup.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%AcademyHeroLockedInBattleDescription");
		}
		else if (DepartmentOfEducation.CheckGarrisonAgainstSiege(this.SelectedHero, this.SelectedHero.Garrison))
		{
			this.UnassignGroup.Enable = false;
			this.UnassignGroup.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%AcademyHeroLockedInSiegeDescription");
		}
		else
		{
			this.UnassignGroup.Enable = this.interactionsAllowed;
			this.UnassignGroup.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%AcademyUnassignDescription");
		}
		if (TutorialManager.IsActivated)
		{
			ITutorialService service = TutorialManager.GetService();
			if (service != null)
			{
				this.UnassignGroup.Enable &= service.GetValue<bool>(TutorialManager.EnableHeroAssignmentKey, true);
			}
		}
	}

	private void ShowLockedLabel(float turnsBeforeUnlock, bool captured = false)
	{
		if (turnsBeforeUnlock < 0f)
		{
			turnsBeforeUnlock = 0f;
		}
		this.AssignGroup.Visible = false;
		this.UnassignGroup.Visible = false;
		this.ReleasePrisonerButton.AgeTransform.Visible = false;
		this.LockedGroup.Visible = true;
		this.LockedGroup.Enable = false;
		this.LockedImage.AgeTransform.Visible = true;
		this.InfiltratingImage.AgeTransform.Visible = false;
		string key = "%AcademyLockedForNTurnsTitle";
		string key2 = "%TurnNumberFormat";
		string text = "%AcademyLockedForNTurnsDescription";
		if (captured)
		{
			key = "%AcademyCapturedForNTurnsTitle";
			text = "%AcademyCapturedForNTurnsDescription";
		}
		this.LockedLabel.Text = AgeLocalizer.Instance.LocalizeString(key);
		this.LockedTurnsLabel.Text = AgeLocalizer.Instance.LocalizeString(key2).Replace("$NumberOfTurns", "#A0A030#" + turnsBeforeUnlock + "#REVERT#");
		text = AgeLocalizer.Instance.LocalizeString(text).Replace("$NumberOfTurns", "#A0A030#" + turnsBeforeUnlock + "#REVERT#");
		if (this.LockedGroup.AgeTooltip != null)
		{
			this.LockedGroup.AgeTooltip.Content = text;
		}
		string text2 = this.LockedLabel.Text;
		StringBuilder stringBuilder = new StringBuilder();
		AgeUtils.CleanLine(text2, ref stringBuilder);
		float num = this.LockedLabel.Font.ComputeTextWidth(text2.ToString(), false, false);
		this.LockedLabel.AgeTransform.Width = num;
		this.LockedLabel.AgeTransform.PixelOffsetLeft = -(num * 0.5f);
		this.LockedImage.AgeTransform.PixelOffsetLeft = this.LockedLabel.AgeTransform.PixelOffsetLeft - this.LockedImage.AgeTransform.Width;
	}

	private void ShowPendingInfiltrationLabel(float turns)
	{
		this.AssignGroup.Visible = false;
		this.UnassignGroup.Visible = false;
		this.LockedGroup.Visible = true;
		this.LockedGroup.Enable = false;
		this.LockedImage.AgeTransform.Visible = false;
		this.InfiltratingImage.AgeTransform.Visible = true;
		this.ReleasePrisonerButton.AgeTransform.Visible = false;
		this.LockedLabel.Text = AgeLocalizer.Instance.LocalizeString("%EspionageInfiltratingSpyTitle");
		this.LockedTurnsLabel.Text = AgeLocalizer.Instance.LocalizeString("%TurnNumberFormat").Replace("$NumberOfTurns", "#A0A030#" + turns + "#REVERT#");
		string content;
		if (turns > 1f)
		{
			content = string.Format(AgeLocalizer.Instance.LocalizeString("%EspionageInfiltratingStatusPluralFormat"), "#A0A030#" + turns + "#REVERT#");
		}
		else
		{
			content = string.Format(AgeLocalizer.Instance.LocalizeString("%EspionageInfiltratingStatusSingleFormat"), "#A0A030#" + turns + "#REVERT#");
		}
		if (this.LockedGroup.AgeTooltip != null)
		{
			this.LockedGroup.AgeTooltip.Content = content;
		}
		string text = this.LockedLabel.Text;
		StringBuilder stringBuilder = new StringBuilder();
		AgeUtils.CleanLine(text, ref stringBuilder);
		float num = this.LockedLabel.Font.ComputeTextWidth(text.ToString(), false, false);
		this.LockedLabel.AgeTransform.Width = num;
		this.LockedLabel.AgeTransform.PixelOffsetLeft = -(num * 0.5f);
		this.InfiltratingImage.AgeTransform.PixelOffsetLeft = this.LockedLabel.AgeTransform.PixelOffsetLeft - this.InfiltratingImage.AgeTransform.Width;
	}

	private void ShowPrisonerButton(float turnsBeforeUnlock, bool captured = false)
	{
		if (turnsBeforeUnlock < 0f)
		{
			turnsBeforeUnlock = 0f;
		}
		this.AssignGroup.Visible = false;
		this.UnassignGroup.Visible = false;
		this.LockedGroup.Visible = false;
		this.LockedGroup.Enable = false;
		this.ReleasePrisonerButton.AgeTransform.Visible = true;
		string key = "%TurnNumberFormat";
		string text = "%AcademyPrisonerForNTurnsDescription";
		this.ReleaseTurnsLabel.Text = AgeLocalizer.Instance.LocalizeString(key).Replace("$NumberOfTurns", "#A0A030#" + turnsBeforeUnlock + "#REVERT#");
		text = AgeLocalizer.Instance.LocalizeString(text).Replace("$NumberOfTurns", "#A0A030#" + turnsBeforeUnlock + "#REVERT#");
		if (this.ReleasePrisonerButton.AgeTransform.AgeTooltip != null)
		{
			this.ReleasePrisonerButton.AgeTransform.AgeTooltip.Content = text;
		}
	}

	private void SelectHeroCardAtIndex(int heroCardIndex)
	{
		List<HeroCard> children = this.HeroesTable.GetChildren<HeroCard>(true);
		if (this.selectedHeroIndex >= 0)
		{
			HeroCard component = children[this.selectedHeroIndex].GetComponent<HeroCard>();
			component.HeroToggle.State = false;
			this.SelectedHero = null;
		}
		children[heroCardIndex].HeroToggle.State = true;
		this.SelectedHero = children[heroCardIndex].Hero;
		this.selectedHeroIndex = heroCardIndex;
		if (this.selectedHeroIndex >= 0)
		{
			float num = children[0].AgeTransform.Width + this.HeroesTable.HorizontalMargin + this.HeroesTable.HorizontalSpacing;
			this.HeroesTable.GetComponent<AgeModifierPosition>().StartX = this.HeroesTable.X;
			this.HeroesTable.GetComponent<AgeModifierPosition>().EndX = (float)(2 - this.selectedHeroIndex) * num;
			this.HeroesTable.GetComponent<AgeModifierPosition>().StartAnimation();
		}
		for (int i = 0; i < children.Count; i++)
		{
			int num2 = Mathf.Abs(i - this.selectedHeroIndex);
			AgeTransform foreground = children[i].Foreground;
			foreground.GetComponent<AgeModifierAlpha>().StartAlpha = foreground.Alpha;
			foreground.GetComponent<AgeModifierAlpha>().EndAlpha = Mathf.Min(0.4f * (float)num2, 1f);
			foreground.GetComponent<AgeModifierAlpha>().StartAnimation();
		}
		this.RefreshButtons();
	}

	private void ValidateArmyChoice(Army army)
	{
		base.AgeTransform.Enable = false;
		OrderChangeHeroAssignment order = new OrderChangeHeroAssignment(base.Empire.Index, this.SelectedHero.GUID, army.GUID);
		Ticket ticket;
		base.PlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
	}

	private void ValidateCityChoice(City city)
	{
		if (this.cityListPurpose == "HeroAssignement")
		{
			base.AgeTransform.Enable = false;
			OrderChangeHeroAssignment order = new OrderChangeHeroAssignment(base.Empire.Index, this.SelectedHero.GUID, city.GUID);
			Ticket ticket;
			base.PlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
			this.cityListPurpose = string.Empty;
		}
		else if (this.cityListPurpose == "HeroArmyCreation")
		{
			OrderTransferAcademyToNewArmy order2 = new OrderTransferAcademyToNewArmy(base.Empire.Index, city.GUID, this.SelectedHero.GUID);
			Ticket ticket2;
			base.PlayerController.PostOrder(order2, out ticket2, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
			this.cityListPurpose = string.Empty;
		}
	}

	private void OnOrderResponse(object sender, TicketRaisedEventArgs args)
	{
		base.AgeTransform.Enable = true;
		if (args.Result == PostOrderResponse.Processed)
		{
			this.RefreshButtons();
			if (args.Order != null && args.Order is OrderTransferAcademyToNewArmy)
			{
				OrderTransferAcademyToNewArmy orderTransferAcademyToNewArmy = args.Order as OrderTransferAcademyToNewArmy;
				IGameEntityRepositoryService service = base.Game.Services.GetService<IGameEntityRepositoryService>();
				IGameEntity gameEntity;
				if (service != null && service.TryGetValue(orderTransferAcademyToNewArmy.ArmyGuid, out gameEntity) && gameEntity is Army && this.HandleCancelRequest())
				{
					IViewService service2 = Services.GetService<IViewService>();
					if (service2 != null)
					{
						service2.SelectAndCenter((Army)gameEntity, true);
					}
				}
			}
			if (args.Order != null && args.Order is OrderReleasePrisoner)
			{
				this.SelectedHero = null;
				base.NeedRefresh = true;
			}
		}
		else
		{
			base.NeedRefresh = true;
		}
	}

	public AgeTransform HeroesTable;

	public Transform HeroCardPrefab;

	public AgeControlButton PreviousButton;

	public AgeControlButton NextButton;

	public AgeTransform ArrowDown;

	public AgeTransform ButtonsGroup;

	public AgeControlButton HealButton;

	public AgePrimitiveLabel HealCostLabel;

	public AgeControlButton ForceShiftingButton;

	public AgePrimitiveLabel ForceShiftCostLabel;

	public AgeControlButton RestoreButton;

	public AgePrimitiveLabel RestoreCostLabel;

	public AgeControlButton DismissButton;

	public AgeControlButton SellButton;

	public AgePrimitiveLabel SellButtonPriceLabel;

	public AgeControlButton InspectButton;

	public AgeTransform AssignGroup;

	public AgeControlButton AssignAsSpyButton;

	public AgeControlButton TmpCreateArmyInCityButton;

	public AgeTransform UnassignGroup;

	public AgeTransform LockedGroup;

	public AgePrimitiveImage LockedImage;

	public AgePrimitiveImage InfiltratingImage;

	public AgePrimitiveLabel LockedLabel;

	public AgePrimitiveLabel LockedTurnsLabel;

	public AgeControlButton ReleasePrisonerButton;

	public AgePrimitiveLabel ReleaseTurnsLabel;

	public AgeTransform HeroesMarketLockedGroup;

	public AgeTransform BuyHeroesButtonGroup;

	public AgePrimitiveLabel BuyHeroesButtonLabel;

	public float ButtonsGroupStandardHeight;

	public float ButtonsGroupExtendedHeight;

	private DepartmentOfEducation departmentOfEducation;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private DepartmentOfScience departmentOfScience;

	private ITradeManagementService marketplace;

	private IEndTurnService endTurnService;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private bool interactionsAllowed = true;

	private Unit selectedHero;

	private int selectedHeroIndex;

	private string cityListPurpose;

	private AgeTransform.RefreshTableItem<Unit> refreshHeroCardDelegate;

	private List<Unit> heroesList = new List<Unit>();
}
