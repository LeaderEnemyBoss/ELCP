using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using UnityEngine;

public class CityWorkersPanel : global::GuiPanel
{
	public City City { get; private set; }

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

	private bool IsOtherEmpire { get; set; }

	public void Bind(City city)
	{
		this.City = city;
		this.City.Refreshed += this.City_Refreshed;
		this.IsOtherEmpire = (this.playerControllerRepository.ActivePlayerController.Empire != this.City.Empire);
		this.EndTurnService = Services.GetService<IEndTurnService>();
		for (int i = 0; i < this.prodPerPopFIDSTypes.Count; i++)
		{
			SimulationPropertyTooltipData simulationPropertyTooltipData = this.ProdPerPopFIDSValues[i].AgeTransform.AgeTooltip.ClientData as SimulationPropertyTooltipData;
			if (simulationPropertyTooltipData != null)
			{
				simulationPropertyTooltipData.Context = city;
			}
			simulationPropertyTooltipData = (this.CityTileFIDSValues[i].AgeTransform.AgeTooltip.ClientData as SimulationPropertyTooltipData);
			if (simulationPropertyTooltipData != null)
			{
				simulationPropertyTooltipData.Context = city;
			}
			simulationPropertyTooltipData = (this.TotalFIDSValues[i].AgeTransform.AgeTooltip.ClientData as SimulationPropertyTooltipData);
			if (simulationPropertyTooltipData != null)
			{
				simulationPropertyTooltipData.Context = city;
			}
			simulationPropertyTooltipData = (this.ModifierFIDSValues[i].AgeTransform.AgeTooltip.ClientData as SimulationPropertyTooltipData);
			if (simulationPropertyTooltipData != null)
			{
				simulationPropertyTooltipData.Context = city;
			}
		}
		for (int j = 0; j < this.prodPerPopFIDSTypes.Count; j++)
		{
			GuiElement guiElement = null;
			base.GuiService.GuiPanelHelper.TryGetGuiElement(this.prodPerPopFIDSTypes[j], out guiElement, this.City.Empire.Faction.Name);
			if (guiElement != null)
			{
				ExtendedGuiElement extendedGuiElement = (ExtendedGuiElement)guiElement;
				if (j < this.FidsSymbols.Length && j < this.ProdPerPopFIDSValues.Length && j < this.ModifierFIDSValues.Length && j < this.CityTileFIDSValues.Length && j < this.TotalFIDSValues.Length && extendedGuiElement != null)
				{
					Texture2D image;
					if (base.GuiService.GuiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Small, out image))
					{
						this.FidsSymbols[j].Image = image;
						this.FidsSymbols[j].TintColor = extendedGuiElement.Color;
					}
					AgeTooltip component = this.FidsGroups[j].GetComponent<AgeTooltip>();
					if (component != null)
					{
						component.Class = "Simple";
						component.Content = extendedGuiElement.Description;
					}
					this.ProdPerPopFIDSValues[j].TintColor = extendedGuiElement.Color;
					this.CityTileFIDSValues[j].TintColor = extendedGuiElement.Color;
					this.ModifierFIDSValues[j].TintColor = extendedGuiElement.Color;
					this.TotalFIDSValues[j].TintColor = extendedGuiElement.Color;
				}
			}
		}
	}

	public override void RefreshContent()
	{
		if (this.City == null)
		{
			return;
		}
		base.RefreshContent();
		float num = 0f;
		WorkersDragPanel guiPanel = base.GuiService.GetGuiPanel<WorkersDragPanel>();
		for (int i = 0; i < this.workerTypes.Count; i++)
		{
			int num2 = Mathf.FloorToInt(this.City.GetPropertyValue(this.workerTypes[i]));
			if (guiPanel.DragInProgress && guiPanel.DragMoved && this.workerTypes[i] == guiPanel.StartingWorkerType)
			{
				num2 -= guiPanel.NumberOfWorkers;
			}
			if (i < this.WorkersGroups.Length)
			{
				this.WorkersGroups[i].RefreshContent(this.City, num2, this.workerTypes[i], guiPanel);
				this.WorkersGroups[i].GetComponent<AgeTransform>().Enable = (this.interactionsAllowed && !this.IsOtherEmpire);
			}
			int num3 = num2 / this.workersPerLine;
			if (num3 * this.workersPerLine != num2)
			{
				num3++;
			}
			if (num3 < 1)
			{
				num3 = 1;
			}
			if (i < this.WorkersGroups.Length && this.WorkersGroups[i] != null)
			{
				AgeTransform workersTable = this.WorkersGroups[i].WorkersTable;
				float num4 = workersTable.VerticalMargin + (float)num3 * (this.childHeight + workersTable.VerticalSpacing);
				if (num4 > num)
				{
					num = num4;
				}
			}
		}
		for (int j = 0; j < this.prodPerPopFIDSTypes.Count; j++)
		{
			float num5 = 0f;
			if (j < this.ProdPerPopFIDSValues.Length)
			{
				float propertyValue = this.City.GetPropertyValue(this.prodPerPopFIDSTypes[j]);
				this.ProdPerPopFIDSValues[j].Text = GuiFormater.FormatGui(propertyValue, false, false, false, 0);
			}
			float propertyValue2 = this.City.GetPropertyValue(this.workedFIDSTypes[j]);
			if (j < this.CityTileFIDSValues.Length)
			{
				float propertyValue3 = this.City.GetPropertyValue(this.cityTileFIDSTypes[j]);
				this.CityTileFIDSValues[j].Text = GuiFormater.FormatGui(propertyValue3, false, false, false, 0);
			}
			if (j < this.TotalFIDSValues.Length)
			{
				num5 = this.City.GetPropertyValue(this.totalFIDSTypes[j]);
				if (this.City.SimulationObject.Tags.Contains(City.MimicsCity) && this.TotalFIDSValues[j].AgeTransform.AgeTooltip.ClientData != null && this.TotalFIDSValues[j].AgeTransform.AgeTooltip.ClientData is SimulationPropertyTooltipData)
				{
					SimulationPropertyTooltipData simulationPropertyTooltipData = this.TotalFIDSValues[j].AgeTransform.AgeTooltip.ClientData as SimulationPropertyTooltipData;
					if (simulationPropertyTooltipData.Title == SimulationProperties.NetCityProduction)
					{
						num5 = 0f;
					}
					else if (simulationPropertyTooltipData.Title == SimulationProperties.NetCityGrowth)
					{
						num5 = this.City.GetPropertyValue("AlmostNetCityGrowth");
					}
				}
				this.TotalFIDSValues[j].Text = GuiFormater.FormatGui(num5, false, false, false, 0);
			}
			if (j < this.ModifierFIDSValues.Length)
			{
				float num6 = num5 - propertyValue2;
				if (Mathf.RoundToInt(num6) != 0)
				{
					this.ModifierFIDSValues[j].Text = GuiFormater.FormatGui(num6, false, false, true, 0);
				}
				else
				{
					this.ModifierFIDSValues[j].Text = string.Empty;
				}
			}
		}
		this.WorkerGroupsTable.Height = num;
		this.WorkersTitle.Height = num;
		for (int k = 0; k < this.WorkersGroups.Length; k++)
		{
			this.WorkersGroups[k].WorkersTable.Height = num;
			this.WorkersGroups[k].ActiveHighlight.Height = num;
			this.WorkersGroups[k].GetComponent<AgeTransform>().Height = num;
		}
		base.AgeTransform.Height = this.TopMargin * AgeUtils.CurrentUpscaleFactor() + num + this.WorkerGroupsTable.PixelMarginBottom;
		this.AgeModifierPosition.EndHeight = base.AgeTransform.Height;
		bool flag = DepartmentOfTheInterior.CanBuyoutPopulation(this.City);
		bool flag2 = this.City.Empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitReplicants1);
		bool flag3 = this.City.Empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitMimics2);
		int rowIndex;
		for (rowIndex = 0; rowIndex < this.FoodColumnCells.Length; rowIndex++)
		{
			bool flag4 = false;
			if (this.IsOtherEmpire)
			{
				flag4 = this.WorkersGroups.Any((WorkersGroup cell) => cell.GetComponent<AgeTransform>() == this.FoodColumnCells[rowIndex]);
			}
			this.FoodColumnCells[rowIndex].Enable = (!flag && this.interactionsAllowed && !flag4);
			this.ScienceColumnCells[rowIndex].Enable = (!flag2 && this.interactionsAllowed && !flag4);
			this.IndustryColumnCells[rowIndex].Enable = (!flag3 && this.interactionsAllowed && !flag4);
		}
	}

	public void Unbind()
	{
		if (this.City != null)
		{
			for (int i = 0; i < this.prodPerPopFIDSTypes.Count; i++)
			{
				SimulationPropertyTooltipData simulationPropertyTooltipData = this.ProdPerPopFIDSValues[i].AgeTransform.AgeTooltip.ClientData as SimulationPropertyTooltipData;
				if (simulationPropertyTooltipData != null)
				{
					simulationPropertyTooltipData.Context = null;
				}
				simulationPropertyTooltipData = (this.CityTileFIDSValues[i].AgeTransform.AgeTooltip.ClientData as SimulationPropertyTooltipData);
				if (simulationPropertyTooltipData != null)
				{
					simulationPropertyTooltipData.Context = null;
				}
				simulationPropertyTooltipData = (this.TotalFIDSValues[i].AgeTransform.AgeTooltip.ClientData as SimulationPropertyTooltipData);
				if (simulationPropertyTooltipData != null)
				{
					simulationPropertyTooltipData.Context = null;
				}
				simulationPropertyTooltipData = (this.ModifierFIDSValues[i].AgeTransform.AgeTooltip.ClientData as SimulationPropertyTooltipData);
				if (simulationPropertyTooltipData != null)
				{
					simulationPropertyTooltipData.Context = null;
				}
			}
			this.City.Refreshed -= this.City_Refreshed;
			this.City = null;
		}
		this.EndTurnService = null;
	}

	protected void OnApplyHighDefinition(float scale)
	{
		this.childWidth = Mathf.Round(scale * this.childWidth);
		this.childHeight = Mathf.Round(scale * this.childHeight);
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		IPlayerControllerRepositoryService playerControllerRepository = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
		this.interactionsAllowed = playerControllerRepository.ActivePlayerController.CanSendOrders();
		this.previousCursor = AgeManager.Instance.Cursor;
		this.WorkerGroupsTable.Enable = true;
		this.updateDrag = true;
		UnityCoroutine.StartCoroutine(this, this.UpdateDrag(), null);
		base.GuiService.GetGuiPanel<WorkersDragPanel>().Hide(true);
		if (this.City != null)
		{
			base.NeedRefresh = true;
		}
		yield break;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		base.NeedRefresh = false;
		if (instant)
		{
			base.AgeTransform.ResetAllModifiers(true, false);
		}
		this.updateDrag = false;
		base.GuiService.GetGuiPanel<WorkersDragPanel>().CancelDrag();
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		base.UseRefreshLoop = true;
		this.workerTypes = new List<StaticString>();
		this.workerTypes.Add(SimulationProperties.FoodPopulation);
		this.workerTypes.Add(SimulationProperties.IndustryPopulation);
		this.workerTypes.Add(SimulationProperties.SciencePopulation);
		this.workerTypes.Add(SimulationProperties.DustPopulation);
		this.workerTypes.Add(SimulationProperties.CityPointPopulation);
		this.prodPerPopFIDSTypes = new List<StaticString>();
		this.prodPerPopFIDSTypes.Add(SimulationProperties.BaseFoodPerPopulation);
		this.prodPerPopFIDSTypes.Add(SimulationProperties.BaseIndustryPerPopulation);
		this.prodPerPopFIDSTypes.Add(SimulationProperties.BaseSciencePerPopulation);
		this.prodPerPopFIDSTypes.Add(SimulationProperties.BaseDustPerPopulation);
		this.prodPerPopFIDSTypes.Add(SimulationProperties.BaseCityPointPerPopulation);
		this.workedFIDSTypes = new List<StaticString>();
		this.workedFIDSTypes.Add(SimulationProperties.WorkedFood);
		this.workedFIDSTypes.Add(SimulationProperties.WorkedIndustry);
		this.workedFIDSTypes.Add(SimulationProperties.WorkedScience);
		this.workedFIDSTypes.Add(SimulationProperties.WorkedDust);
		this.workedFIDSTypes.Add(SimulationProperties.WorkedCityPoint);
		this.cityTileFIDSTypes = new List<StaticString>();
		this.cityTileFIDSTypes.Add(SimulationProperties.DistrictFoodNet);
		this.cityTileFIDSTypes.Add(SimulationProperties.DistrictIndustryNet);
		this.cityTileFIDSTypes.Add(SimulationProperties.DistrictScienceNet);
		this.cityTileFIDSTypes.Add(SimulationProperties.DistrictDustNet);
		this.cityTileFIDSTypes.Add(SimulationProperties.DistrictCityPointNet);
		this.totalFIDSTypes = new List<StaticString>();
		this.totalFIDSTypes.Add(SimulationProperties.NetCityGrowth);
		this.totalFIDSTypes.Add(SimulationProperties.NetCityProduction);
		this.totalFIDSTypes.Add(SimulationProperties.NetCityResearch);
		this.totalFIDSTypes.Add(SimulationProperties.NetCityMoney);
		this.totalFIDSTypes.Add(SimulationProperties.NetCityEmpirePoint);
		for (int i = 0; i < this.prodPerPopFIDSTypes.Count; i++)
		{
			GuiElement guiElement;
			base.GuiService.GuiPanelHelper.TryGetGuiElement(this.prodPerPopFIDSTypes[i], out guiElement);
			if (guiElement != null)
			{
				ExtendedGuiElement extendedGuiElement = (ExtendedGuiElement)guiElement;
				if (i < this.FidsSymbols.Length && i < this.ProdPerPopFIDSValues.Length && i < this.ModifierFIDSValues.Length && i < this.CityTileFIDSValues.Length && i < this.TotalFIDSValues.Length && extendedGuiElement != null)
				{
					Texture2D texture;
					if (base.GuiService.GuiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Small, out texture))
					{
						this.FidsSymbols[i].Image = texture;
						this.FidsSymbols[i].TintColor = extendedGuiElement.Color;
					}
					this.ProdPerPopFIDSValues[i].TintColor = extendedGuiElement.Color;
					this.CityTileFIDSValues[i].TintColor = extendedGuiElement.Color;
					this.ModifierFIDSValues[i].TintColor = extendedGuiElement.Color;
					this.TotalFIDSValues[i].TintColor = extendedGuiElement.Color;
				}
			}
		}
		if (this.WorkersGroups.Length > 0)
		{
			AgeTransform table = this.WorkersGroups[0].WorkersTable;
			AgeTransform childPrefab = this.WorkersGroups[0].WorkerSymbolPrefab.GetComponent<AgeTransform>();
			this.childWidth = childPrefab.Width * AgeUtils.CurrentUpscaleFactor();
			this.childHeight = childPrefab.Height * AgeUtils.CurrentUpscaleFactor();
			this.workersPerLine = Mathf.RoundToInt((table.Width - table.HorizontalMargin) / (this.childWidth + table.HorizontalSpacing));
		}
		string[] modifiers = new string[this.prodPerPopFIDSTypes.Count];
		modifiers[0] = string.Format("{0},{1},{2},!{3}", new object[]
		{
			SimulationProperties.CityFood,
			SimulationProperties.CityGrowth,
			SimulationProperties.NetCityGrowth,
			SimulationProperties.CityGrowthUpkeep
		});
		modifiers[1] = string.Format("{0},{1},{2},!{3}", new object[]
		{
			SimulationProperties.CityIndustry,
			SimulationProperties.CityProduction,
			SimulationProperties.NetCityProduction,
			SimulationProperties.CityProductionUpkeep
		});
		modifiers[2] = string.Format("{0},{1},{2},!{3}", new object[]
		{
			SimulationProperties.CityScience,
			SimulationProperties.CityResearch,
			SimulationProperties.NetCityResearch,
			SimulationProperties.CityResearchUpkeep
		});
		modifiers[3] = string.Format("{0},{1},{2},!{3}", new object[]
		{
			SimulationProperties.CityDust,
			SimulationProperties.CityMoney,
			SimulationProperties.NetCityMoney,
			SimulationProperties.TotalCityMoneyUpkeep
		});
		modifiers[4] = string.Format("{0},{1},{2}", SimulationProperties.CityCityPoint, SimulationProperties.CityEmpirePoint, SimulationProperties.NetCityEmpirePoint);
		string[] modifiersTitle = new string[this.prodPerPopFIDSTypes.Count];
		modifiersTitle[0] = string.Format("%ModifierCategory{0}Title", SimulationProperties.CityFood);
		modifiersTitle[1] = string.Format("%ModifierCategory{0}Title", SimulationProperties.CityIndustry);
		modifiersTitle[2] = string.Format("%ModifierCategory{0}Title", SimulationProperties.CityScience);
		modifiersTitle[3] = string.Format("%ModifierCategory{0}Title", SimulationProperties.CityDust);
		modifiersTitle[4] = string.Format("%ModifierCategory{0}Title", SimulationProperties.CityCityPoint);
		string[] cityTileFIDSTooltip = new string[this.prodPerPopFIDSTypes.Count];
		cityTileFIDSTooltip[0] = string.Format("{0},{1}", SimulationProperties.DistrictFood, SimulationProperties.DistrictFoodNet);
		cityTileFIDSTooltip[1] = string.Format("{0},{1}", SimulationProperties.DistrictIndustry, SimulationProperties.DistrictIndustryNet);
		cityTileFIDSTooltip[2] = string.Format("{0},{1}", SimulationProperties.DistrictScience, SimulationProperties.DistrictScienceNet);
		cityTileFIDSTooltip[3] = string.Format("{0},{1}", SimulationProperties.DistrictDust, SimulationProperties.DistrictDustNet);
		cityTileFIDSTooltip[4] = string.Format("{0},{1}", SimulationProperties.DistrictCityPoint, SimulationProperties.DistrictCityPointNet);
		for (int j = 0; j < this.prodPerPopFIDSTypes.Count; j++)
		{
			if (this.ProdPerPopFIDSValues[j].AgeTransform.AgeTooltip == null)
			{
				break;
			}
			this.ProdPerPopFIDSValues[j].AgeTransform.AgeTooltip.Class = "FIDS";
			this.ProdPerPopFIDSValues[j].AgeTransform.AgeTooltip.Content = this.prodPerPopFIDSTypes[j];
			this.ProdPerPopFIDSValues[j].AgeTransform.AgeTooltip.ClientData = new SimulationPropertyTooltipData(this.prodPerPopFIDSTypes[j], this.prodPerPopFIDSTypes[j], this.City);
			this.CityTileFIDSValues[j].AgeTransform.AgeTooltip.Class = "FIDS";
			this.CityTileFIDSValues[j].AgeTransform.AgeTooltip.Content = this.cityTileFIDSTypes[j];
			this.CityTileFIDSValues[j].AgeTransform.AgeTooltip.ClientData = new SimulationPropertyTooltipData(this.cityTileFIDSTypes[j], cityTileFIDSTooltip[j], this.City);
			this.ModifierFIDSValues[j].AgeTransform.AgeTooltip.Class = "FIDS";
			this.ModifierFIDSValues[j].AgeTransform.AgeTooltip.Content = modifiersTitle[j];
			this.ModifierFIDSValues[j].AgeTransform.AgeTooltip.ClientData = new SimulationPropertyTooltipData(modifiersTitle[j], modifiers[j], this.City);
			this.TotalFIDSValues[j].AgeTransform.AgeTooltip.Class = "FIDS";
			this.TotalFIDSValues[j].AgeTransform.AgeTooltip.Content = this.totalFIDSTypes[j];
			this.TotalFIDSValues[j].AgeTransform.AgeTooltip.ClientData = new SimulationPropertyTooltipData(this.totalFIDSTypes[j], GuiSimulation.Instance.FIMSTooltipTotal[j], this.City);
		}
		yield break;
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		this.playerControllerRepository = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
		yield break;
	}

	protected override void OnUnload()
	{
		for (int i = 0; i < this.prodPerPopFIDSTypes.Count; i++)
		{
			if (this.ProdPerPopFIDSValues[i].AgeTransform.AgeTooltip == null)
			{
				break;
			}
			this.ProdPerPopFIDSValues[i].AgeTransform.AgeTooltip.ClientData = null;
			this.CityTileFIDSValues[i].AgeTransform.AgeTooltip.ClientData = null;
			this.TotalFIDSValues[i].AgeTransform.AgeTooltip.ClientData = null;
			this.ModifierFIDSValues[i].AgeTransform.AgeTooltip.ClientData = null;
		}
		this.prodPerPopFIDSTypes = null;
		this.workerTypes = null;
		this.cityTileFIDSTypes = null;
		this.totalFIDSTypes = null;
		base.OnUnload();
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.Hide(true);
		this.Unbind();
		this.playerControllerRepository = null;
		base.OnUnloadGame(game);
	}

	private void City_Refreshed(object sender)
	{
		base.NeedRefresh = true;
	}

	private void EndTurnService_GameClientStateChange(object sender, GameClientStateChangeEventArgs e)
	{
		if (base.IsVisible && base.Game != null)
		{
			IPlayerControllerRepositoryService service = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
			if (service == null)
			{
				return;
			}
			bool flag = service.ActivePlayerController.CanSendOrders();
			if (this.interactionsAllowed != flag)
			{
				this.interactionsAllowed = flag;
				WorkersDragPanel guiPanel = base.GuiService.GetGuiPanel<WorkersDragPanel>();
				if (!this.interactionsAllowed && guiPanel.DragInProgress && guiPanel.DragMoved)
				{
					guiPanel.CancelDrag();
				}
				base.NeedRefresh = true;
			}
		}
	}

	private void CheckDragTarget(WorkersDragPanel workersDragPanel)
	{
		if (AgeManager.Instance.ActiveControl != null)
		{
			WorkersGroup component = AgeManager.Instance.ActiveControl.GetComponent<WorkersGroup>();
			if (component == null)
			{
				WorkerSymbol component2 = AgeManager.Instance.ActiveControl.GetComponent<WorkerSymbol>();
				if (component2 != null)
				{
					component = component2.AgeTransform.GetParent().GetComponent<WorkersGroup>();
				}
			}
			if (component != null)
			{
				if (workersDragPanel.StartingWorkerType != component.WorkerType)
				{
					workersDragPanel.ValidateDrag(this.WorkerGroupsTable, component.WorkerType, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
					if (TutorialManager.IsActivated)
					{
						IEventService service = Services.GetService<IEventService>();
						Diagnostics.Assert(service != null);
						IGameService service2 = Services.GetService<IGameService>();
						Diagnostics.Assert(service2 != null);
						global::Game x = service2.Game as global::Game;
						Diagnostics.Assert(x != null);
						service.Notify(new EventTutorialWorkerDragged(this.City.Empire, workersDragPanel.StartingWorkerType, component.WorkerType));
					}
				}
				else
				{
					workersDragPanel.CancelDrag();
					base.NeedRefresh = true;
				}
			}
			else
			{
				workersDragPanel.CancelDrag();
				base.NeedRefresh = true;
			}
		}
		else
		{
			workersDragPanel.CancelDrag();
			base.NeedRefresh = true;
		}
	}

	private IEnumerator UpdateDrag()
	{
		while (this.updateDrag)
		{
			WorkersDragPanel workersDragPanel = base.GuiService.GetGuiPanel<WorkersDragPanel>();
			if (Input.GetMouseButtonDown(0))
			{
				if (workersDragPanel.DragInProgress)
				{
					if (workersDragPanel.DragMoved)
					{
						this.CheckDragTarget(workersDragPanel);
					}
					else
					{
						workersDragPanel.CancelDrag();
						base.NeedRefresh = true;
					}
				}
				else if (AgeManager.Instance.ActiveControl != null)
				{
					WorkerSymbol worker = AgeManager.Instance.ActiveControl.GetComponent<WorkerSymbol>();
					if (worker != null)
					{
						workersDragPanel.InitDrag(this.City, worker.WorkerType, worker.DragQuantity);
					}
				}
			}
			else if (Input.GetMouseButtonUp(0))
			{
				if (workersDragPanel.DragInProgress)
				{
					if (!workersDragPanel.DragMoved)
					{
						workersDragPanel.StartDrag();
						base.NeedRefresh = true;
					}
					else
					{
						workersDragPanel.DragInProgress = false;
						this.CheckDragTarget(workersDragPanel);
					}
				}
			}
			else if (workersDragPanel.DragInProgress && !workersDragPanel.DragMoved && this.previousCursor != AgeManager.Instance.Cursor)
			{
				workersDragPanel.StartDrag();
				base.NeedRefresh = true;
			}
			this.previousCursor = AgeManager.Instance.Cursor;
			yield return null;
		}
		yield break;
	}

	private void OnOrderResponse(object sender, TicketRaisedEventArgs args)
	{
		this.WorkerGroupsTable.Enable = true;
		base.NeedRefresh = true;
	}

	public float TopMargin = 12f;

	public float Separator = 2f;

	public AgeTransform WorkerGroupsTable;

	public AgeTransform[] FidsGroups;

	public AgePrimitiveImage[] FidsSymbols;

	public AgePrimitiveLabel[] ProdPerPopFIDSValues;

	public AgeTransform WorkersTitle;

	public WorkersGroup[] WorkersGroups;

	public AgePrimitiveLabel[] CityTileFIDSValues;

	public AgePrimitiveLabel[] ModifierFIDSValues;

	public AgePrimitiveLabel[] TotalFIDSValues;

	public AgeTransform[] FoodColumnCells;

	public AgeTransform[] ScienceColumnCells;

	public AgeTransform[] IndustryColumnCells;

	public AgeModifierPosition AgeModifierPosition;

	private List<StaticString> workerTypes;

	private List<StaticString> prodPerPopFIDSTypes;

	private List<StaticString> workedFIDSTypes;

	private List<StaticString> cityTileFIDSTypes;

	private List<StaticString> totalFIDSTypes;

	private bool updateDrag;

	private Vector2 previousCursor;

	private IPlayerControllerRepositoryService playerControllerRepository;

	private int workersPerLine = 1;

	private float childWidth;

	private float childHeight;

	private IEndTurnService endTurnService;

	private bool interactionsAllowed = true;
}
