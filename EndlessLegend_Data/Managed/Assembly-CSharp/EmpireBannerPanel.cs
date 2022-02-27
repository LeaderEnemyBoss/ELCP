using System;
using System.Collections;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using UnityEngine;

public class EmpireBannerPanel : GuiPlayerControllerPanel
{
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
				this.departmentOfScience.ResearchQueueChanged -= this.DepartmentOfScience_ResearchQueueChanged;
			}
			this.departmentOfScience = value;
			if (this.departmentOfScience != null)
			{
				this.departmentOfScience.ResearchQueueChanged += this.DepartmentOfScience_ResearchQueueChanged;
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

	public override void Bind(global::Empire empire)
	{
		base.Bind(empire);
		this.DepartmentOfTheTreasury = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		this.DepartmentOfScience = base.Empire.GetAgency<DepartmentOfScience>();
		base.Empire.Refreshed += this.Simulation_Refreshed;
		if (this.EmpireTreasuryPanel.AgeTooltip != null)
		{
			SimulationPropertyTooltipData simulationPropertyTooltipData = this.EmpireTreasuryPanel.AgeTooltip.ClientData as SimulationPropertyTooltipData;
			if (simulationPropertyTooltipData != null)
			{
				simulationPropertyTooltipData.Context = empire;
				simulationPropertyTooltipData.StockName = SimulationProperties.EmpireMoney;
			}
		}
		if (this.EmpirePointsPanel.AgeTooltip != null)
		{
			SimulationPropertyTooltipData simulationPropertyTooltipData = this.EmpirePointsPanel.AgeTooltip.ClientData as SimulationPropertyTooltipData;
			if (simulationPropertyTooltipData != null)
			{
				simulationPropertyTooltipData.Context = empire;
				simulationPropertyTooltipData.StockName = SimulationProperties.EmpirePoint;
			}
		}
		if (this.EmpireLavapoolStockPanel.AgeTooltip != null)
		{
			SimulationPropertyTooltipData simulationPropertyTooltipData = this.EmpireLavapoolStockPanel.AgeTooltip.ClientData as SimulationPropertyTooltipData;
			if (simulationPropertyTooltipData != null)
			{
				simulationPropertyTooltipData.Context = empire;
				simulationPropertyTooltipData.StockName = SimulationProperties.LavapoolStock;
			}
		}
		base.NeedRefresh = true;
	}

	public void Configure(string mode)
	{
		if (mode == EmpireBannerPanel.Full)
		{
			base.AgeTransform.Height = this.maxHeight;
			this.EmpireTreasuryPanel.Visible = true;
			this.EmpireResearchPanel.Visible = true;
			this.EmpirePointsPanel.Visible = true;
			this.EmpireLavapoolStockPanel.Visible = DepartmentOfTheInterior.CanPerformLavaformation(base.Empire);
			this.ResourceEnumerator.Show(new object[0]);
		}
		else if (mode == EmpireBannerPanel.TreasuryResearch)
		{
			base.AgeTransform.Height = this.maxHeight * 0.5f;
			this.EmpireTreasuryPanel.Visible = true;
			this.EmpireResearchPanel.Visible = true;
			this.EmpirePointsPanel.Visible = true;
			this.EmpireLavapoolStockPanel.Visible = DepartmentOfTheInterior.CanPerformLavaformation(base.Empire);
			this.ResourceEnumerator.Hide(false);
		}
		else if (mode == EmpireBannerPanel.Strategic)
		{
			base.AgeTransform.Height = this.maxHeight * 0.5f;
			this.EmpireTreasuryPanel.Visible = false;
			this.EmpireResearchPanel.Visible = false;
			this.EmpirePointsPanel.Visible = false;
			this.EmpireLavapoolStockPanel.Visible = false;
			this.ResourceEnumerator.Show(new object[0]);
		}
	}

	public override void Unbind()
	{
		if (base.Empire != null)
		{
			this.DepartmentOfTheTreasury = null;
			this.DepartmentOfScience = null;
			base.Empire.Refreshed -= this.Simulation_Refreshed;
		}
		if (this.EmpireTreasuryPanel.AgeTooltip != null)
		{
			SimulationPropertyTooltipData simulationPropertyTooltipData = this.EmpireTreasuryPanel.AgeTooltip.ClientData as SimulationPropertyTooltipData;
			if (simulationPropertyTooltipData != null)
			{
				simulationPropertyTooltipData.Context = null;
			}
		}
		if (this.EmpirePointsPanel.AgeTooltip != null)
		{
			SimulationPropertyTooltipData simulationPropertyTooltipData = this.EmpirePointsPanel.AgeTooltip.ClientData as SimulationPropertyTooltipData;
			if (simulationPropertyTooltipData != null)
			{
				simulationPropertyTooltipData.Context = null;
			}
		}
		if (this.EmpireLavapoolStockPanel.AgeTooltip != null)
		{
			SimulationPropertyTooltipData simulationPropertyTooltipData = this.EmpireLavapoolStockPanel.AgeTooltip.ClientData as SimulationPropertyTooltipData;
			if (simulationPropertyTooltipData != null)
			{
				simulationPropertyTooltipData.Context = null;
			}
		}
		base.Unbind();
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		this.RefreshCurrentResearch();
		this.RefreshBankAccount();
		this.RefreshEmpirePoints();
		this.RefreshLavapoolStock();
		this.ResourceEnumerator.RefreshContent();
		this.ResourceEnumerator.AgeTransform.Enable = this.interactionsAllowed;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		while (base.Empire == null)
		{
			yield return null;
		}
		this.interactionsAllowed = base.PlayerController.CanSendOrders();
		if (parameters.Length == 1)
		{
			this.Configure(parameters[0] as StaticString);
		}
		this.ResourceEnumerator.Show(new object[0]);
		base.NeedRefresh = true;
		yield break;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		base.NeedRefresh = false;
		this.ResourceEnumerator.Hide(instant);
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		this.maxHeight = base.AgeTransform.Height;
		this.EndTurnService = Services.GetService<IEndTurnService>();
		this.EmpireResearchPanel.AgeTooltip.ClientData = null;
		yield break;
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.Hide(true);
		this.EndTurnService = null;
		if (base.PlayerController != null && base.PlayerController.GameInterface != null)
		{
			base.PlayerController.GameInterface.StateChange -= this.GameInterface_StateChange;
		}
		base.OnUnloadGame(game);
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		base.UseRefreshLoop = true;
		GuiElement guiElement;
		base.GuiService.GuiPanelHelper.TryGetGuiElement(SimulationProperties.DistrictDust, out guiElement);
		this.ResourceEnumerator.ResourceType = ResourceDefinition.Type.Strategic;
		if (this.EmpireTreasuryPanel.AgeTooltip != null)
		{
			this.EmpireTreasuryPanel.AgeTooltip.Class = "FIDS";
			this.EmpireTreasuryPanel.AgeTooltip.Content = "none";
			this.EmpireTreasuryPanel.AgeTooltip.ClientData = new SimulationPropertyTooltipData(SimulationProperties.NetEmpireMoney, string.Format("{0},{1},{2},{3},!{4}", new object[]
			{
				SimulationProperties.NetCityMoney,
				SimulationProperties.NetFortressMoney,
				SimulationProperties.EmpireMoney,
				SimulationProperties.NetEmpireMoney,
				SimulationProperties.EmpireMoneyUpkeep
			}), base.Empire);
		}
		if (this.EmpirePointsPanel.AgeTooltip != null)
		{
			this.EmpirePointsPanel.AgeTooltip.Class = "FIDS";
			this.EmpirePointsPanel.AgeTooltip.Content = "none";
			this.EmpirePointsPanel.AgeTooltip.ClientData = new SimulationPropertyTooltipData(SimulationProperties.NetEmpirePoint, string.Format("{0},{1},{2},{3},!{4}", new object[]
			{
				SimulationProperties.NetCityEmpirePoint,
				SimulationProperties.NetFortressEmpirePoint,
				SimulationProperties.EmpirePoint,
				SimulationProperties.NetEmpirePoint,
				SimulationProperties.EmpirePointUpkeep
			}), base.Empire);
		}
		if (this.EmpireLavapoolStockPanel.AgeTooltip != null)
		{
			this.EmpireLavapoolStockPanel.AgeTooltip.Class = "Lavapool";
			this.EmpireLavapoolStockPanel.AgeTooltip.Content = SimulationProperties.NetLavapool;
			this.EmpireLavapoolStockPanel.AgeTooltip.ClientData = new SimulationPropertyTooltipData(SimulationProperties.NetLavapool, string.Format("{0}", 0), base.Empire);
		}
		yield break;
	}

	protected override void PlayerControllerRepository_ActivePlayerControllerChange(object sender, ActivePlayerControllerChangeEventArgs eventArgs)
	{
		if (base.PlayerController != null)
		{
			base.PlayerController.GameInterface.StateChange -= this.GameInterface_StateChange;
		}
		base.PlayerControllerRepository_ActivePlayerControllerChange(sender, eventArgs);
		if (base.PlayerController != null)
		{
			base.PlayerController.GameInterface.StateChange += this.GameInterface_StateChange;
		}
	}

	private void DepartmentOfScience_ResearchQueueChanged(object sender, ConstructionChangeEventArgs e)
	{
		base.NeedRefresh = true;
	}

	private void DepartmentOfTheTreasury_ResourcePropertyChange(object sender, ResourcePropertyChangeEventArgs e)
	{
		if (e.Location != base.Empire.SimulationObject)
		{
			return;
		}
		base.NeedRefresh = true;
	}

	private void EndTurnService_GameClientStateChange(object sender, GameClientStateChangeEventArgs e)
	{
		if (base.IsVisible && base.PlayerController != null)
		{
			bool flag = base.PlayerController.CanSendOrders();
			if (this.interactionsAllowed != flag)
			{
				this.interactionsAllowed = flag;
				this.ResourceEnumerator.AgeTransform.Enable = this.interactionsAllowed;
				if (!this.interactionsAllowed && MessagePanel.Instance.IsVisible)
				{
					MessagePanel.Instance.Hide(false);
				}
			}
		}
	}

	private void GameInterface_StateChange(object sender, FiniteStateChangeEventArgs e)
	{
		FiniteStateChangeAction action = e.Action;
		if (action == FiniteStateChangeAction.Begun)
		{
			if (e.State.GetType() == typeof(GameClientState_Turn_Begin))
			{
				if (base.Empire != null)
				{
					base.NeedRefresh = true;
				}
			}
		}
	}

	private void OnApplyHighDefinition(float scale)
	{
		this.maxHeight = Mathf.Round(scale * this.maxHeight);
	}

	private void OnTechnologyBuyoutCB(GameObject obj)
	{
		Construction construction = this.departmentOfScience.ResearchQueue.Peek();
		if (construction == null)
		{
			return;
		}
		OrderBuyOutTechnology order = new OrderBuyOutTechnology(base.Empire.Index, construction.ConstructibleElement.Name);
		base.PlayerController.PostOrder(order);
	}

	private void Simulation_Refreshed(object sender)
	{
		base.NeedRefresh = true;
	}

	private void RefreshCurrentResearch()
	{
		DepartmentOfScience agency = base.Empire.GetAgency<DepartmentOfScience>();
		DepartmentOfTheTreasury agency2 = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		Construction construction = agency.ResearchQueue.Peek();
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		if (service != null && service.IsShared(DownloadableContent11.ReadOnlyName) && base.Empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitReplicants1))
		{
			this.ResearchBuyoutButton.AgeTransform.Visible = true;
			this.ResearchBuyoutLabel.AgeTransform.Visible = true;
			this.CurrentResearchTitle.AgeTransform.Visible = false;
			this.EmpireResearchPanel.Enable = true;
			this.EmpireResearchPanel.AgeTooltip.Class = string.Empty;
			this.EmpireResearchPanel.AgeTooltip.Content = string.Empty;
			this.EmpireResearchPanel.AgeTooltip.ClientData = null;
			ConstructibleElement technology = null;
			if (this.departmentOfScience.ResearchQueue.Peek() != null)
			{
				technology = this.departmentOfScience.ResearchQueue.Peek().ConstructibleElement;
			}
			float buyOutTechnologyCost = this.departmentOfScience.GetBuyOutTechnologyCost(technology);
			float num = -buyOutTechnologyCost;
			string newValue = GuiFormater.FormatInstantCost(base.Empire, buyOutTechnologyCost, DepartmentOfTheTreasury.Resources.EmpireMoney, true, 0);
			string content;
			if (construction != null)
			{
				this.ResearchBuyoutLabel.AgeTransform.Alpha = 1f;
				TechnologyDefinition technologyDefinition = construction.ConstructibleElement as TechnologyDefinition;
				if (technologyDefinition != null)
				{
					AgeUtils.TruncateString(AgeLocalizer.Instance.LocalizeString(DepartmentOfScience.GetTechnologyTitle(technologyDefinition)), this.ResearchBuyoutLabel, out this.format, '.');
					DepartmentOfScience.BuildTechnologyTooltip(technologyDefinition, base.Empire, this.ResearchBuyoutLabel.AgeTransform.AgeTooltip, MultipleConstructibleTooltipData.TechnologyState.Normal);
				}
				if (agency2.IsTransferOfResourcePossible(base.Empire, DepartmentOfTheTreasury.Resources.TechnologiesBuyOut, ref num))
				{
					this.ResearchBuyoutButton.AgeTransform.Enable = true;
					this.ResearchBuyoutButton.AgeTransform.Alpha = 1f;
					this.ResearchBuyoutLabel.Text = this.format;
					content = AgeLocalizer.Instance.LocalizeString("%ResearchBuyoutAvailableFormat").Replace("$Cost", newValue);
				}
				else
				{
					this.ResearchBuyoutButton.AgeTransform.Enable = false;
					this.ResearchBuyoutButton.AgeTransform.Alpha = 0.5f;
					this.ResearchBuyoutLabel.Text = this.format;
					content = AgeLocalizer.Instance.LocalizeString("%ResearchBuyoutUnavailableFormat").Replace("$Cost", newValue);
				}
			}
			else
			{
				this.ResearchBuyoutButton.AgeTransform.Enable = false;
				this.ResearchBuyoutButton.AgeTransform.Alpha = 0.5f;
				this.ResearchBuyoutLabel.Text = "%ResearchNoneTitle";
				this.ResearchBuyoutLabel.AgeTransform.Alpha = 0.5f;
				content = AgeLocalizer.Instance.LocalizeString("%ResearchBuyoutNoSelection");
				this.ResearchBuyoutLabel.AgeTransform.AgeTooltip.Class = string.Empty;
				this.ResearchBuyoutLabel.AgeTransform.AgeTooltip.Content = "%ResearchNoneDescription";
				this.ResearchBuyoutLabel.AgeTransform.AgeTooltip.ClientData = null;
			}
			AgeTooltip ageTooltip = this.ResearchBuyoutButton.AgeTransform.AgeTooltip;
			if (ageTooltip != null)
			{
				this.ResearchBuyoutButton.AgeTransform.AgeTooltip.Content = content;
			}
		}
		else
		{
			this.ResearchBuyoutButton.AgeTransform.Visible = false;
			this.ResearchBuyoutLabel.AgeTransform.Visible = false;
			this.CurrentResearchTitle.AgeTransform.Visible = true;
			if (construction != null)
			{
				this.EmpireResearchPanel.Enable = true;
				int num2 = agency2.ComputeConstructionRemainingTurn(base.Empire, construction);
				if (num2 == 2147483647)
				{
					this.format = string.Format("$Tech ({0})", GuiFormater.Infinite.ToString());
				}
				else
				{
					this.format = string.Format("$Tech ({0})", QueueGuiItem.FormatNumberOfTurns(num2));
				}
				TechnologyDefinition technologyDefinition2 = construction.ConstructibleElement as TechnologyDefinition;
				if (technologyDefinition2 != null)
				{
					AgeUtils.TruncateStringWithSubst(this.format, "$Tech", AgeLocalizer.Instance.LocalizeString(DepartmentOfScience.GetTechnologyTitle(technologyDefinition2)), this.CurrentResearchTitle, out this.format, '.');
					this.CurrentResearchTitle.Text = this.format;
					DepartmentOfScience.BuildTechnologyTooltip(technologyDefinition2, base.Empire, this.EmpireResearchPanel.AgeTooltip, MultipleConstructibleTooltipData.TechnologyState.Normal);
				}
			}
			else
			{
				this.EmpireResearchPanel.Enable = false;
				AgeUtils.TruncateString(AgeLocalizer.Instance.LocalizeString("%ResearchNoneTitle"), this.CurrentResearchTitle, out this.format, '.');
				this.CurrentResearchTitle.Text = this.format;
				this.EmpireResearchPanel.AgeTooltip.Class = string.Empty;
				this.EmpireResearchPanel.AgeTooltip.Content = "%ResearchNoneDescription";
				this.EmpireResearchPanel.AgeTooltip.ClientData = null;
			}
			this.CurrentResearchTitle.Text = this.format;
		}
	}

	private void RefreshBankAccount()
	{
		if (this.departmentOfTheTreasury != null)
		{
			float stockValue;
			if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpireMoney, out stockValue, false))
			{
				Diagnostics.LogError("Can't get resource stock value {0} on simulation object {1}.", new object[]
				{
					DepartmentOfTheTreasury.Resources.EmpireMoney,
					base.Empire.SimulationObject.Name
				});
			}
			float netValue;
			this.departmentOfTheTreasury.TryGetNetResourceValue(base.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpireMoney, out netValue, false);
			this.BankAccountValue.Text = GuiFormater.FormatStockAndNet(stockValue, netValue, SimulationProperties.EmpireMoney, true);
			if (this.BankAccountValue.TextLines.Count > 1)
			{
				this.BankAccountValue.AgeTransform.Visible = false;
				this.BankAccountValueSmall.AgeTransform.Visible = true;
				this.BankAccountValueSmall.Text = GuiFormater.FormatStockAndNet(stockValue, netValue, SimulationProperties.EmpireMoney, true);
			}
			else
			{
				this.BankAccountValue.AgeTransform.Visible = true;
				this.BankAccountValueSmall.AgeTransform.Visible = false;
			}
		}
	}

	private void RefreshEmpirePoints()
	{
		float stockValue;
		if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, out stockValue, false))
		{
			Diagnostics.LogError("Can't get resource stock value {0} on simulation object {1}.", new object[]
			{
				DepartmentOfTheTreasury.Resources.EmpirePoint,
				base.Empire.SimulationObject
			});
		}
		float netValue;
		this.departmentOfTheTreasury.TryGetNetResourceValue(base.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, out netValue, false);
		this.EmpirePointsValue.Text = GuiFormater.FormatStockAndNet(stockValue, netValue, SimulationProperties.EmpirePoint, true);
		if (this.EmpirePointsValue.TextLines.Count > 1)
		{
			this.EmpirePointsValue.AgeTransform.Visible = false;
			this.EmpirePointsValueSmall.AgeTransform.Visible = true;
			this.EmpirePointsValueSmall.Text = GuiFormater.FormatStockAndNet(stockValue, netValue, SimulationProperties.EmpirePoint, true);
		}
		else
		{
			this.EmpirePointsValue.AgeTransform.Visible = true;
			this.EmpirePointsValueSmall.AgeTransform.Visible = false;
		}
	}

	private void RefreshLavapoolStock()
	{
		float num = 0f;
		if (this.departmentOfTheTreasury.TryGetResourceStockValue(base.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.Lavapool, out num, false))
		{
			IGuiPanelHelper guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
			GuiElement guiElement;
			if (guiPanelHelper.TryGetGuiElement(DepartmentOfTheTreasury.Resources.Lavapool, out guiElement))
			{
				this.EmpireLavapoolStockValue.Text = string.Format("{0}  {1}", ((ExtendedGuiElement)guiElement).SymbolString, num);
			}
		}
	}

	public static StaticString Full = "Full";

	public static StaticString TreasuryResearch = "TreasuryResearch";

	public static StaticString Strategic = "Strategic";

	public AgeTransform EmpireTreasuryPanel;

	public AgeTransform EmpireResearchPanel;

	public AgeTransform EmpirePointsPanel;

	public AgeTransform EmpireLavapoolStockPanel;

	public AgePrimitiveLabel BankAccountValue;

	public AgePrimitiveLabel BankAccountValueSmall;

	public AgePrimitiveLabel CurrentResearchTitle;

	public AgeControlButton ResearchBuyoutButton;

	public AgePrimitiveLabel ResearchBuyoutLabel;

	public AgePrimitiveLabel EmpirePointsValue;

	public AgePrimitiveLabel EmpirePointsValueSmall;

	public AgePrimitiveLabel EmpireLavapoolStockValue;

	public ResourceEnumerator ResourceEnumerator;

	private DepartmentOfScience departmentOfScience;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private IEndTurnService endTurnService;

	private bool interactionsAllowed = true;

	private string format = string.Empty;

	private float maxHeight;
}
