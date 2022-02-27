using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class ExchangeInfoPanel : GuiPlayerControllerPanel
{
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
				this.marketplace.TransactionComplete -= this.Marketplace_TransactionComplete;
			}
			this.marketplace = value;
			if (this.marketplace != null)
			{
				this.marketplace.TransactionComplete += this.Marketplace_TransactionComplete;
			}
		}
	}

	private uint CurrentTurn
	{
		get
		{
			return (uint)((global::Game)base.GameService.Game).Turn;
		}
	}

	public override void Bind(global::Empire empire)
	{
		base.Bind(empire);
		this.Marketplace = base.Game.Services.GetService<ITradeManagementService>();
		this.BuildListOfExchangeTurns();
		if (base.IsVisible)
		{
			this.RefreshContent();
		}
	}

	public override void Unbind()
	{
		this.EmpireFiltersContainer.GetChildren<EmpireFilterToggle>(true).ForEach(delegate(EmpireFilterToggle empireFilter)
		{
			empireFilter.UnsetContent();
		});
		this.UnbindExchangeTurnItems();
		this.empireIndexesToShow.Clear();
		this.empireToFilter.Clear();
		this.Marketplace = null;
		this.guiExchangeTurns.Clear();
		base.Unbind();
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		this.BuildListOfExchangeTurns();
		this.ExchangeTurnItemsContainer.ReserveChildren(this.guiExchangeTurns.Count, this.ExchangeTurnItemPrefab, "Turn");
		this.ExchangeTurnItemsContainer.RefreshChildrenIList<GuiExchangeTurn>(this.guiExchangeTurns, this.setupGuiExchangeTurnDelegate, true, false);
		List<ExchangeTurnItem> children = this.ExchangeTurnItemsContainer.GetChildren<ExchangeTurnItem>(true);
		float num = 0f;
		for (int i = children.Count - 1; i >= 0; i--)
		{
			children[i].AgeTransform.Y = num;
			num += children[i].AgeTransform.Height + this.ExchangeTurnItemsContainer.VerticalSpacing;
		}
		this.ExchangeTurnItemsContainer.Height = 0f;
		if (children.Count > 0)
		{
			this.ExchangeTurnItemsContainer.Height = children[0].AgeTransform.Y + children[0].AgeTransform.Height;
		}
		this.ExchangeTurnItemsScrollView.OnPositionRecomputed();
	}

	protected override IEnumerator OnHide(bool instant)
	{
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		this.RefreshEmpireFilters();
		this.RefreshContent();
		yield break;
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		IEventService eventService = Services.GetService<IEventService>();
		if (eventService != null)
		{
			eventService.EventRaise += this.EventService_EventRaise;
		}
		yield break;
	}

	protected override void OnUnloadGame(IGame game)
	{
		IEventService service = Services.GetService<IEventService>();
		if (service != null)
		{
			service.EventRaise -= this.EventService_EventRaise;
		}
		base.OnUnloadGame(game);
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		this.setupGuiExchangeTurnDelegate = new AgeTransform.RefreshTableItem<GuiExchangeTurn>(this.SetupGuiExchangeTurn);
		yield break;
	}

	protected override void OnUnload()
	{
		this.setupGuiExchangeTurnDelegate = null;
		base.OnUnload();
	}

	private void EventService_EventRaise(object sender, EventRaiseEventArgs e)
	{
		if (!base.AgeTransform.Visible)
		{
			return;
		}
		EventTechnologyEnded eventTechnologyEnded = e.RaisedEvent as EventTechnologyEnded;
		if (eventTechnologyEnded != null)
		{
			TechnologyDefinition technologyDefinition = eventTechnologyEnded.ConstructibleElement as TechnologyDefinition;
			if (technologyDefinition.Name == "TechnologyDefinitionMarketplaceHeroes" || technologyDefinition.Name == "TechnologyDefinitionMarketplaceMercenaries" || technologyDefinition.Name == "TechnologyDefinitionMarketplaceResources")
			{
				this.RefreshEmpireFilters();
				this.RefreshContent();
			}
		}
		EventHeroInfiltrated eventHeroInfiltrated = e.RaisedEvent as EventHeroInfiltrated;
		if (eventHeroInfiltrated != null)
		{
			this.RefreshEmpireFilters();
			this.RefreshContent();
		}
		EventHeroExfiltrated eventHeroExfiltrated = e.RaisedEvent as EventHeroExfiltrated;
		if (eventHeroExfiltrated != null)
		{
			this.RefreshEmpireFilters();
			this.RefreshContent();
		}
	}

	private void Marketplace_TransactionComplete(object sender, TradableTransactionCompleteEventArgs e)
	{
		if (this.AddTransaction(e.Transaction))
		{
			this.RefreshContent();
		}
	}

	private void OnToggleEmpireFilter(EmpireFilterToggle empireFilterToggle)
	{
		if (empireFilterToggle == null)
		{
			return;
		}
		if (empireFilterToggle.Toggle.State)
		{
			this.empireIndexesToShow.Add(empireFilterToggle.Empire.Index);
		}
		else
		{
			this.empireIndexesToShow.Remove(empireFilterToggle.Empire.Index);
		}
		this.RefreshContent();
	}

	private void BuildListOfExchangeTurns()
	{
		if (this.guiExchangeTurns.Count > 0)
		{
			this.UnbindExchangeTurnItems();
		}
		this.latestLoggedTurn = -1;
		ReadOnlyCollection<TradableTransaction> pastTransactions = this.marketplace.GetPastTransactions();
		for (int i = 0; i < pastTransactions.Count; i++)
		{
			this.AddTransaction(pastTransactions[i]);
		}
	}

	private bool AddTransaction(TradableTransaction transaction)
	{
		if (this.empireIndexesToShow.Contains((int)transaction.EmpireIndex))
		{
			if ((ulong)transaction.Turn > (ulong)((long)this.latestLoggedTurn))
			{
				this.latestLoggedTurn = (int)transaction.Turn;
				this.guiExchangeTurns.Add(new GuiExchangeTurn(this.latestLoggedTurn, base.Empire));
			}
			this.guiExchangeTurns[this.guiExchangeTurns.Count - 1].Add(transaction);
			return true;
		}
		return false;
	}

	private void UnbindExchangeTurnItems()
	{
		List<ExchangeTurnItem> children = this.ExchangeTurnItemsContainer.GetChildren<ExchangeTurnItem>(true);
		for (int i = 0; i < children.Count; i++)
		{
			children[i].UnsetContent();
		}
		this.guiExchangeTurns.Clear();
	}

	private void SetupGuiExchangeTurn(AgeTransform tableItem, GuiExchangeTurn guiExchangeTurn, int index)
	{
		ExchangeTurnItem component = tableItem.GetComponent<ExchangeTurnItem>();
		component.SetContent(guiExchangeTurn, base.Empire);
		component.RefreshContent();
		tableItem.StartNewMesh = true;
	}

	private void RefreshEmpireFilters()
	{
		List<global::Empire> list = new List<global::Empire>();
		List<EmpireFilterToggle> children = this.EmpireFiltersContainer.GetChildren<EmpireFilterToggle>(true);
		for (int i = 0; i < children.Count; i++)
		{
			if (children[i].Toggle.State)
			{
				list.Add(children[i].Empire);
			}
			children[i].UnsetContent();
		}
		this.CreateEmpireFilters(list);
	}

	private void ShowHideEmpireFilters()
	{
		bool flag = this.empireToFilter.Count > 1;
		float num = this.EmpireFiltersContainer.Height + this.EmpireFiltersContainer.PixelMarginBottom;
		if (flag && !this.EmpireFiltersContainer.Visible)
		{
			this.EmpireFiltersContainer.Visible = true;
			this.ExchangeTurnItemsScrollView.AgeTransform.PixelMarginTop += num;
		}
		else if (!flag && this.EmpireFiltersContainer.Visible)
		{
			this.EmpireFiltersContainer.Visible = false;
			this.ExchangeTurnItemsScrollView.AgeTransform.PixelMarginTop -= num;
		}
	}

	private void CreateEmpireFilters(List<global::Empire> empiresToSelect = null)
	{
		if (empiresToSelect != null && empiresToSelect.Count == 1 && empiresToSelect[0] == base.Empire)
		{
			empiresToSelect.Clear();
		}
		this.empireToFilter.Clear();
		DepartmentOfIntelligence agency = base.Empire.GetAgency<DepartmentOfIntelligence>();
		bool flag = DepartmentOfTheInterior.CanSeeAllExchangeTransactions(base.Empire);
		global::Game game = base.Game as global::Game;
		for (int i = 0; i < game.Empires.Length; i++)
		{
			MajorEmpire majorEmpire = game.Empires[i] as MajorEmpire;
			if (majorEmpire != null)
			{
				if (base.Empire.Index == majorEmpire.Index)
				{
					this.empireToFilter.Add(majorEmpire);
				}
				else if (!majorEmpire.IsEliminated)
				{
					DepartmentOfScience agency2 = majorEmpire.GetAgency<DepartmentOfScience>();
					if (agency2.CanTradeHeroes(true) || agency2.CanTradeUnits(true) || agency2.CanTradeResourcesAndBoosters(true))
					{
						if (flag)
						{
							this.empireToFilter.Add(majorEmpire);
						}
						else if (agency != null && agency.IsEmpireInfiltrated(majorEmpire))
						{
							this.empireToFilter.Add(majorEmpire);
						}
					}
				}
			}
		}
		this.empireFilterWidth = Mathf.Floor((this.EmpireFiltersContainer.Width - this.EmpireFiltersContainer.HorizontalSpacing * (float)(this.empireToFilter.Count - 1)) / (float)this.empireToFilter.Count);
		this.EmpireFiltersContainer.ReserveChildren(this.empireToFilter.Count, this.EmpireFilterPrefab, "EmpireFilterToggle");
		this.EmpireFiltersContainer.RefreshChildrenIList<global::Empire>(this.empireToFilter, new AgeTransform.RefreshTableItem<global::Empire>(this.SetupEmpireFilterToggle), true, false);
		this.EmpireFiltersContainer.ArrangeChildren();
		if (empiresToSelect != null && empiresToSelect.Count > 0)
		{
			this.SetEmpiresToShow(empiresToSelect);
		}
		else
		{
			this.SetEmpiresToShow(this.empireToFilter);
		}
		AgeTooltip ageTooltip = this.EmpireFilterPrefab.GetComponent<AgeTransform>().AgeTooltip;
		if (ageTooltip != null)
		{
			ageTooltip.Content = "%MarketplaceEmpireFilterDescription";
		}
	}

	private void SetEmpiresToShow(List<global::Empire> empiresToSelect)
	{
		ExchangeInfoPanel.<SetEmpiresToShow>c__AnonStoreyA48 <SetEmpiresToShow>c__AnonStoreyA = new ExchangeInfoPanel.<SetEmpiresToShow>c__AnonStoreyA48();
		<SetEmpiresToShow>c__AnonStoreyA.empiresToSelect = empiresToSelect;
		this.empireIndexesToShow.Clear();
		int index;
		for (index = 0; index < <SetEmpiresToShow>c__AnonStoreyA.empiresToSelect.Count; index++)
		{
			if (this.empireToFilter.Exists((global::Empire match) => match == <SetEmpiresToShow>c__AnonStoreyA.empiresToSelect[index]))
			{
				this.empireIndexesToShow.Add(<SetEmpiresToShow>c__AnonStoreyA.empiresToSelect[index].Index);
			}
		}
		List<EmpireFilterToggle> children = this.EmpireFiltersContainer.GetChildren<EmpireFilterToggle>(true);
		for (int i = 0; i < children.Count; i++)
		{
			children[i].Toggle.State = this.empireIndexesToShow.Contains(children[i].Empire.Index);
		}
		this.ShowHideEmpireFilters();
	}

	private void SetupEmpireFilterToggle(AgeTransform tableItem, global::Empire empire, int index)
	{
		tableItem.Width = this.empireFilterWidth;
		EmpireFilterToggle component = tableItem.GetComponent<EmpireFilterToggle>();
		component.SetContent(empire, base.Empire, base.gameObject, false);
	}

	public Transform EmpireFilterPrefab;

	public Transform ExchangeTurnItemPrefab;

	public AgeTransform EmpireFiltersContainer;

	public AgeTransform ExchangeTurnItemsContainer;

	public AgeControlScrollView ExchangeTurnItemsScrollView;

	private List<GuiExchangeTurn> guiExchangeTurns = new List<GuiExchangeTurn>();

	private int latestLoggedTurn = -1;

	private List<global::Empire> empireToFilter = new List<global::Empire>();

	private List<int> empireIndexesToShow = new List<int>();

	private ITradeManagementService marketplace;

	private float empireFilterWidth;

	private AgeTransform.RefreshTableItem<GuiExchangeTurn> setupGuiExchangeTurnDelegate;
}
