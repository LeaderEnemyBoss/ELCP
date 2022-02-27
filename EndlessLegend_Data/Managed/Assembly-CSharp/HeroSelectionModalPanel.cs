using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class HeroSelectionModalPanel : GuiModalPanel
{
	public global::Empire Empire { get; private set; }

	public Unit CurrentHero { get; private set; }

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

	public void Bind(global::Empire empire)
	{
		this.Empire = empire;
		this.departmentOfEducation = this.Empire.GetAgency<DepartmentOfEducation>();
		this.departmentOfEducation.HeroCollectionChange += this.HeroSelectionModalPanel_HeroesCollectionChanged;
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		for (int i = 0; i < this.HeroesTable.GetChildren().Count; i++)
		{
			this.HeroesTable.GetChildren()[i].GetComponent<HeroCard>().Unbind();
		}
		List<Unit> list = new List<Unit>(this.departmentOfEducation.Heroes);
		list.Sort(delegate(Unit hero1, Unit hero2)
		{
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
		this.HeroesTable.Width = 0f;
		this.HeroesTable.ReserveChildren(list.Count, this.HeroCardPrefab, "HeroCard");
		this.HeroesTable.RefreshChildrenIList<Unit>(list, this.heroRefreshDelegate, true, false);
		this.HeroesTable.ArrangeChildren();
		this.HeroesScrollView.ResetLeft();
		this.RefreshButtons();
	}

	public void Unbind()
	{
		if (this.Empire != null)
		{
			for (int i = 0; i < this.HeroesTable.GetChildren().Count; i++)
			{
				this.HeroesTable.GetChildren()[i].GetComponent<HeroCard>().Unbind();
			}
			this.CurrentHero = null;
			this.departmentOfEducation.HeroCollectionChange -= this.HeroSelectionModalPanel_HeroesCollectionChanged;
			this.departmentOfEducation = null;
			this.Empire = null;
		}
	}

	protected override IEnumerator OnHide(bool instant)
	{
		this.Unbind();
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		this.EncounterRepositoryService = base.Game.Services.GetService<IEncounterRepositoryService>();
		this.EndTurnService = Services.GetService<IEndTurnService>();
		this.heroRefreshDelegate = new AgeTransform.RefreshTableItem<Unit>(this.HeroRefresh);
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		IPlayerControllerRepositoryService playerControllerRepositoryService = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
		this.interactionsAllowed = playerControllerRepositoryService.ActivePlayerController.CanSendOrders();
		if (parameters.Length > 0)
		{
			this.selectionValidatedClient = (parameters[0] as GameObject);
		}
		if (parameters.Length > 1)
		{
			this.destination = (parameters[1] as IGarrison);
		}
		this.isASpySelection = false;
		if (parameters.Length > 2)
		{
			this.isASpySelection = (bool)parameters[2];
		}
		IPlayerControllerRepositoryService playerControllerRepository = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
		this.CurrentHero = null;
		this.Bind(playerControllerRepository.ActivePlayerController.Empire as global::Empire);
		this.RefreshContent();
		yield break;
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.Hide(true);
		this.heroRefreshDelegate = null;
		this.HeroesTable.DestroyAllChildren();
		this.destination = null;
		this.EncounterRepositoryService = null;
		this.isASpySelection = false;
		base.OnUnloadGame(game);
	}

	private void EncounterRepositoryService_EncounterRepositoryChange(object sender, EncounterRepositoryChangeEventArgs e)
	{
		if (base.IsVisible && base.Game != null && this.Empire != null)
		{
			this.RefreshContent();
		}
	}

	private void EndTurnService_GameClientStateChange(object sender, GameClientStateChangeEventArgs e)
	{
		if (base.IsVisible && base.Game != null && this.Empire != null)
		{
			IPlayerControllerRepositoryService service = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
			bool flag = service.ActivePlayerController.CanSendOrders();
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

	private void HeroRefresh(AgeTransform tableitem, Unit hero, int index)
	{
		HeroCard component = tableitem.GetComponent<HeroCard>();
		component.Bind(hero, this.Empire, base.gameObject);
		component.RefreshContent(false, false);
		if (DepartmentOfEducation.IsInjured(hero) || DepartmentOfEducation.IsLocked(hero) || DepartmentOfIntelligence.IsHeroInfiltrating(hero, this.Empire) || DepartmentOfEducation.IsCaptured(hero))
		{
			component.GetComponent<AgeTransform>().Enable = false;
			component.Foreground.Alpha = 0.8f;
		}
		else if (hero.Garrison != null && hero.Garrison.IsInEncounter)
		{
			component.GetComponent<AgeTransform>().Enable = false;
			component.Foreground.Alpha = 0.8f;
		}
		else if (DepartmentOfEducation.CheckGarrisonAgainstSiege(hero, hero.Garrison) && !DepartmentOfEducation.CheckHeroExchangeAgainstSiege(hero.Garrison, this.destination))
		{
			component.GetComponent<AgeTransform>().Enable = false;
			component.Foreground.Alpha = 0.8f;
		}
		else
		{
			component.GetComponent<AgeTransform>().Enable = true;
			component.Foreground.Alpha = 0f;
		}
		if (this.isASpySelection && !hero.CheckUnitAbility(UnitAbility.ReadonlySpy, -1))
		{
			component.GetComponent<AgeTransform>().Enable = false;
			component.Foreground.Alpha = 0.8f;
		}
	}

	private void HeroSelectionModalPanel_HeroesCollectionChanged(object sender, CollectionChangeEventArgs e)
	{
		this.SelectButton.Enable = false;
		this.InspectButton.Enable = false;
		this.CurrentHero = null;
		this.RefreshContent();
	}

	private void OnDoubleClickCB(GameObject obj)
	{
		if (!this.interactionsAllowed)
		{
			return;
		}
		this.OnSelectCB(obj);
	}

	private void OnSelectCB(GameObject obj)
	{
		this.selectionValidatedClient.SendMessage("ValidateHeroChoice", this.CurrentHero);
		this.Hide(false);
	}

	private void OnInspectCB(GameObject obj)
	{
		IGuiService service = Services.GetService<IGuiService>();
		service.Show(typeof(HeroInspectionModalPanel), new object[]
		{
			this.CurrentHero
		});
	}

	private void OnHeroToggle(GameObject obj)
	{
		AgeControlToggle component = obj.GetComponent<AgeControlToggle>();
		List<HeroCard> children = this.HeroesTable.GetChildren<HeroCard>(true);
		for (int i = 0; i < children.Count; i++)
		{
			children[i].HeroToggle.State = (children[i].HeroToggle == component);
			if (children[i].HeroToggle == component)
			{
				this.CurrentHero = children[i].Hero;
			}
		}
		this.RefreshButtons();
	}

	private void OnCancelCB(GameObject obj)
	{
		this.Hide(false);
	}

	private void RefreshButtons()
	{
		this.SelectButton.Enable = (this.interactionsAllowed && this.CurrentHero != null);
		this.InspectButton.Enable = (this.CurrentHero != null);
	}

	public Transform HeroCardPrefab;

	public AgeControlScrollView HeroesScrollView;

	public AgeTransform HeroesTable;

	public AgeTransform SelectButton;

	public AgeTransform InspectButton;

	public AgeTransform CancelButton;

	private GameObject selectionValidatedClient;

	private DepartmentOfEducation departmentOfEducation;

	private IEncounterRepositoryService encounterRepositoryService;

	private IEndTurnService endTurnService;

	private bool interactionsAllowed = true;

	private AgeTransform.RefreshTableItem<Unit> heroRefreshDelegate;

	private bool isASpySelection;

	private IGarrison destination;
}
