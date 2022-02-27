using System;
using System.Collections;
using Amplitude.Unity.Framework;

public class GameMarketplaceScreen : GuiPlayerControllerScreen
{
	public GameMarketplaceScreen()
	{
		base.EnableCaptureBackgroundOnShow = true;
		base.EnableMenuAudioLayer = true;
		base.EnableNavigation = true;
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

	public override void Bind(Empire empire)
	{
		base.Bind(empire);
		this.DepartmentOfScience = base.Empire.GetAgency<DepartmentOfScience>();
		this.UpdateSalablePanelVisibility();
		base.NeedRefresh = true;
	}

	public override void Unbind()
	{
		this.DepartmentOfScience = null;
		base.Unbind();
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
	}

	protected override IEnumerator OnHide(bool instant)
	{
		base.NeedRefresh = false;
		this.BuyablePanel.Hide(true);
		this.SalablePanel.Hide(true);
		this.ExchangeInfoPanel.Hide(true);
		base.GuiService.GetGuiPanel<ControlBanner>().OnHideScreen(GameScreenType.Marketplace);
		ITradeManagementService tradeManagementService = Services.GetService<ITradeManagementService>();
		if (tradeManagementService != null)
		{
			tradeManagementService.TransactionComplete -= this.TradeManagerService_TransactionComplete;
		}
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		ITradeManagementService tradeManagementService = base.GameService.Game.Services.GetService<ITradeManagementService>();
		if (tradeManagementService != null)
		{
			tradeManagementService.TransactionComplete += this.TradeManagerService_TransactionComplete;
		}
		base.AgeTransform.Enable = true;
		base.GuiService.Show(typeof(EmpireBannerPanel), new object[]
		{
			EmpireBannerPanel.Full
		});
		base.GuiService.GetGuiPanel<ControlBanner>().OnShowScreen(GameScreenType.Marketplace);
		this.BuyablePanel.Show(parameters);
		this.ExchangeInfoPanel.Show(new object[0]);
		this.UpdateSalablePanelVisibility();
		base.NeedRefresh = true;
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		base.UseRefreshLoop = true;
		this.BuyablePanel.Load();
		this.SalablePanel.Load();
		this.ExchangeInfoPanel.Load();
		yield break;
	}

	private void DepartmentOfScience_TechnologyUnlocked(object sender, ConstructibleElementEventArgs e)
	{
		if (base.IsVisible)
		{
			this.UpdateSalablePanelVisibility();
		}
	}

	private void TradeManagerService_TransactionComplete(object sender, TradableTransactionCompleteEventArgs e)
	{
		if (e.Transaction.Type == TradableTransactionType.Ban)
		{
			this.Hide(false);
		}
	}

	private void UpdateSalablePanelVisibility()
	{
		if (this.SalablePanel.IsVisible)
		{
			return;
		}
		if (this.DepartmentOfScience != null && this.DepartmentOfScience.CanTradeResourcesAndBoosters(false) && !base.Empire.SimulationObject.Tags.Contains(Empire.TagEmpireEliminated))
		{
			this.SalablePanel.Show(new object[0]);
		}
	}

	private void OnCloseCB()
	{
		this.HandleCancelRequest();
	}

	public const int MaximumOneTimeBuyOrSellQuantity = 10;

	public BuyablePanel BuyablePanel;

	public SalablePanel SalablePanel;

	public ExchangeInfoPanel ExchangeInfoPanel;

	private DepartmentOfScience departmentOfScience;
}
