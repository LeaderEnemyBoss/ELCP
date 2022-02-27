using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
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
	public CityWorkersPanel()
	{
		this.TopMargin = 12f;
		this.Separator = 2f;
		this.workersPerLine = 1;
		this.interactionsAllowed = true;
		this.ParentIsCityListScreen = false;
	}

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
		this.departmentOfEducation = this.City.Empire.GetAgency<DepartmentOfEducation>();
		this.departmentOfEducation.VaultItemsCollectionChange += this.DepartmentOfEducation_VaultItemsCollectionChange;
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
		bool flag = DepartmentOfTheInterior.CanBuyoutPopulation(this.City);
		bool flag2 = this.City.Empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitReplicants1);
		bool flag3 = this.City.Empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitMimics2);
		int rowIndex;
		Func<WorkersGroup, bool> <>9__0;
		int rowIndex2;
		for (rowIndex = 0; rowIndex < this.FoodColumnCells.Length; rowIndex = rowIndex2 + 1)
		{
			bool flag4 = false;
			if (this.IsOtherEmpire)
			{
				IEnumerable<WorkersGroup> workersGroups = this.WorkersGroups;
				Func<WorkersGroup, bool> predicate;
				if ((predicate = <>9__0) == null)
				{
					predicate = (<>9__0 = ((WorkersGroup cell) => cell.GetComponent<AgeTransform>() == this.FoodColumnCells[rowIndex]));
				}
				flag4 = workersGroups.Any(predicate);
			}
			this.FoodColumnCells[rowIndex].Enable = (!flag && this.interactionsAllowed && !flag4);
			this.ScienceColumnCells[rowIndex].Enable = (!flag2 && this.interactionsAllowed && !flag4);
			this.IndustryColumnCells[rowIndex].Enable = (!flag3 && this.interactionsAllowed && !flag4);
			rowIndex2 = rowIndex;
		}
		if (this.BoostersTable == null)
		{
			bool highDefinition = AgeUtils.HighDefinition;
			AgeUtils.HighDefinition = false;
			this.BoostersTable = base.AgeTransform.InstanciateChild(this.BoostersEnumerator.BoostersTable.transform, "WorkerPanelBoostersTable1");
			this.BoostersTable.TableArrangement = false;
			this.BoostersTable2 = base.AgeTransform.InstanciateChild(this.BoostersEnumerator.BoostersTable.transform, "WorkerPanelBoostersTable2");
			this.BoostersTable2.TableArrangement = false;
			AgeUtils.HighDefinition = highDefinition;
		}
		this.stackedBoosters.Clear();
		this.stackedBoosters2.Clear();
		float num7 = 0f;
		bool flag5 = false;
		bool flag6 = false;
		bool flag7 = false;
		bool flag8 = false;
		float num8 = AgeUtils.HighDefinition ? 3f : 2f;
		if (!this.IsOtherEmpire)
		{
			foreach (string text in new List<string>
			{
				"BoosterFood",
				"BoosterCadavers",
				"BoosterIndustry",
				"FlamesIndustryBooster",
				"BoosterScience"
			})
			{
				BoosterDefinition boosterDefinition2;
				if (this.database.TryGetValue(text, out boosterDefinition2))
				{
					GuiStackedBooster item = new GuiStackedBooster(boosterDefinition2);
					this.stackedBoosters.Add(item);
					if (!this.ParentIsCityListScreen && (text == "BoosterCadavers" || text == "FlamesIndustryBooster"))
					{
						this.stackedBoosters2.Add(item);
					}
				}
			}
			bool flag9 = false;
			this.vaultBoosters = this.departmentOfEducation.GetVaultItems<BoosterDefinition>();
			for (int l = 0; l < this.vaultBoosters.Count; l++)
			{
				BoosterDefinition boosterDefinition = this.vaultBoosters[l].Constructible as BoosterDefinition;
				if (boosterDefinition != null)
				{
					flag9 = true;
					if (boosterDefinition.Name == "BoosterFood")
					{
						flag5 = true;
					}
					else if (boosterDefinition.Name == "BoosterIndustry")
					{
						flag6 = true;
					}
					else if (boosterDefinition.Name == "BoosterCadavers")
					{
						flag7 = true;
					}
					else if (boosterDefinition.Name == "FlamesIndustryBooster")
					{
						flag8 = true;
					}
					this.stackedBoosters.Find((GuiStackedBooster booster) => booster.BoosterDefinition.RewardType == boosterDefinition.RewardType).AddVaultBooster(this.vaultBoosters[l]);
				}
			}
			if (!flag9)
			{
				this.stackedBoosters.Clear();
				this.stackedBoosters2.Clear();
			}
			else
			{
				num7 = this.FidsGroups[0].Height;
				if (!this.ParentIsCityListScreen)
				{
					if (!flag6)
					{
						GuiStackedBooster item2 = this.stackedBoosters.Find((GuiStackedBooster booster) => booster.BoosterDefinition.Name == "BoosterIndustry");
						this.stackedBoosters.Remove(item2);
					}
					else
					{
						GuiStackedBooster item3 = this.stackedBoosters.Find((GuiStackedBooster booster) => booster.BoosterDefinition.Name == "FlamesIndustryBooster");
						this.stackedBoosters.Remove(item3);
					}
					if (!flag5)
					{
						GuiStackedBooster item4 = this.stackedBoosters.Find((GuiStackedBooster booster) => booster.BoosterDefinition.Name == "BoosterFood");
						this.stackedBoosters.Remove(item4);
					}
					else
					{
						GuiStackedBooster item5 = this.stackedBoosters.Find((GuiStackedBooster booster) => booster.BoosterDefinition.Name == "BoosterCadavers");
						this.stackedBoosters.Remove(item5);
					}
					if (!flag6 && !flag5)
					{
						this.stackedBoosters2.Clear();
					}
				}
				else
				{
					if (flag8 && !flag6)
					{
						GuiStackedBooster item6 = this.stackedBoosters.Find((GuiStackedBooster booster) => booster.BoosterDefinition.Name == "BoosterIndustry");
						this.stackedBoosters.Remove(item6);
					}
					else if (!flag8)
					{
						GuiStackedBooster item7 = this.stackedBoosters.Find((GuiStackedBooster booster) => booster.BoosterDefinition.Name == "FlamesIndustryBooster");
						this.stackedBoosters.Remove(item7);
					}
					if (!flag5 && flag7)
					{
						GuiStackedBooster item8 = this.stackedBoosters.Find((GuiStackedBooster booster) => booster.BoosterDefinition.Name == "BoosterFood");
						this.stackedBoosters.Remove(item8);
					}
					else if (!flag7)
					{
						GuiStackedBooster item9 = this.stackedBoosters.Find((GuiStackedBooster booster) => booster.BoosterDefinition.Name == "BoosterCadavers");
						this.stackedBoosters.Remove(item9);
					}
				}
			}
		}
		this.BoostersTable2.ReserveChildren(this.stackedBoosters2.Count, this.BoostersEnumerator.BoosterStockPrefab, "Item2");
		this.BoostersTable2.RefreshChildrenIList<GuiStackedBooster>(this.stackedBoosters2, this.refreshDelegate, true, true);
		this.BoostersTable.ReserveChildren(this.stackedBoosters.Count, this.BoostersEnumerator.BoosterStockPrefab, "Item");
		this.BoostersTable.RefreshChildrenIList<GuiStackedBooster>(this.stackedBoosters, this.refreshDelegate, true, true);
		this.BoostersTable.PixelMarginTop = base.AgeTransform.Height;
		this.BoostersTable2.PixelMarginTop = base.AgeTransform.Height + this.FidsGroups[0].Height + num8;
		float num9 = 0f;
		foreach (BoosterStock boosterStock in this.BoostersTable.GetChildren<BoosterStock>(true))
		{
			float num10 = num8;
			if (this.ParentIsCityListScreen && ((flag5 && flag7 && (boosterStock.GuiStackedBooster.BoosterDefinition.Name == "BoosterFood" || boosterStock.GuiStackedBooster.BoosterDefinition.Name == "BoosterCadavers")) || (flag6 && flag8 && (boosterStock.GuiStackedBooster.BoosterDefinition.Name == "BoosterIndustry" || boosterStock.GuiStackedBooster.BoosterDefinition.Name == "FlamesIndustryBooster"))))
			{
				boosterStock.AgeTransform.Width = this.FidsGroups[0].Width / 2f - 1f;
				if (boosterStock.GuiStackedBooster.BoosterDefinition.Name == "BoosterFood" || boosterStock.GuiStackedBooster.BoosterDefinition.Name == "BoosterIndustry")
				{
					num10 = 2f;
				}
			}
			else
			{
				boosterStock.AgeTransform.Width = this.FidsGroups[0].Width;
			}
			if (boosterStock.GuiStackedBooster.Quantity == 0 || (flag && (num9 == 0f || boosterStock.GuiStackedBooster.BoosterDefinition.Name == "BoosterCadavers")) || (flag2 && boosterStock.GuiStackedBooster.BoosterDefinition.Name == "BoosterScience"))
			{
				boosterStock.AgeTransform.Enable = false;
				boosterStock.AgeTransform.Visible = false;
			}
			else
			{
				num7 = boosterStock.AgeTransform.Height;
				boosterStock.AgeTransform.Enable = this.interactionsAllowed;
				boosterStock.AgeTransform.Visible = true;
				boosterStock.QuickActivation = true;
				boosterStock.Guid = this.City.GUID;
				boosterStock.QuantityLabel.AgeTransform.AttachTop = true;
				boosterStock.QuantityLabel.AgeTransform.AttachRight = true;
				boosterStock.QuantityLabel.AgeTransform.PixelMarginLeft = 0f;
				boosterStock.QuantityLabel.Alignement = AgeTextAnchor.AscendMiddleRight;
				if (!this.ParentIsCityListScreen)
				{
					boosterStock.IconImage.AgeTransform.PixelMarginLeft = num8 * 2.5f;
					boosterStock.QuantityLabel.AgeTransform.PixelMarginRight = num8 * 2f;
				}
				else
				{
					boosterStock.IconImage.AgeTransform.PixelMarginLeft = ((boosterStock.AgeTransform.Width == this.FidsGroups[0].Width) ? (boosterStock.AgeTransform.Width / 3f) : (boosterStock.AgeTransform.Width / 5f));
					boosterStock.QuantityLabel.AgeTransform.PixelMarginRight = ((boosterStock.AgeTransform.Width == this.FidsGroups[0].Width) ? (boosterStock.AgeTransform.Width / 3f) : (boosterStock.AgeTransform.Width / 5f));
				}
			}
			boosterStock.AgeTransform.X = num9;
			boosterStock.AgeTransform.AttachRight = false;
			boosterStock.AgeTransform.AttachLeft = true;
			num9 += boosterStock.AgeTransform.Width + num10;
		}
		if (this.BoostersTable2.GetChildren<BoosterStock>(true).Count > 0)
		{
			bool flag10 = false;
			num9 = 0f;
			foreach (BoosterStock boosterStock2 in this.BoostersTable2.GetChildren<BoosterStock>(true))
			{
				boosterStock2.AgeTransform.Width = this.FidsGroups[0].Width;
				if (boosterStock2.GuiStackedBooster.Quantity == 0 || (num9 == 0f && !flag5) || (num9 > 0f && !flag6) || (flag && num9 == 0f))
				{
					boosterStock2.AgeTransform.Enable = false;
					boosterStock2.AgeTransform.Visible = false;
				}
				else
				{
					flag10 = true;
					boosterStock2.AgeTransform.Enable = this.interactionsAllowed;
					boosterStock2.AgeTransform.Visible = true;
					boosterStock2.QuickActivation = true;
					boosterStock2.Guid = this.City.GUID;
					boosterStock2.IconImage.AgeTransform.PixelMarginLeft = num8 * 2.5f;
					boosterStock2.QuantityLabel.AgeTransform.AttachTop = true;
					boosterStock2.QuantityLabel.AgeTransform.AttachRight = true;
					boosterStock2.QuantityLabel.AgeTransform.PixelMarginLeft = 0f;
					boosterStock2.QuantityLabel.AgeTransform.PixelMarginRight = num8 * 2f;
					boosterStock2.QuantityLabel.Alignement = AgeTextAnchor.AscendMiddleRight;
				}
				boosterStock2.AgeTransform.X = num9;
				boosterStock2.AgeTransform.AttachRight = false;
				boosterStock2.AgeTransform.AttachLeft = true;
				num9 += this.FidsGroups[0].Width + this.BoostersTable2.HorizontalSpacing;
			}
			num7 += (flag10 ? (this.FidsGroups[0].Height + num8) : 0f);
		}
		base.AgeTransform.Height += num7;
		this.AgeModifierPosition.EndHeight = base.AgeTransform.Height;
		foreach (AgeTransform ageTransform in base.AgeTransform.GetChildren())
		{
			if (ageTransform.name.Contains("CityTile") || ageTransform.name.Contains("Total") || ageTransform.name.Contains("Modifiers"))
			{
				ageTransform.PixelMarginBottom = this.OriginalMargins[ageTransform.name] * (AgeUtils.HighDefinition ? 1.5f : 1f) + num7;
			}
		}
	}

	public void Unbind()
	{
		if (this.departmentOfEducation != null)
		{
			this.departmentOfEducation.VaultItemsCollectionChange -= this.DepartmentOfEducation_VaultItemsCollectionChange;
			this.departmentOfEducation = null;
		}
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
		IPlayerControllerRepositoryService service = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
		this.interactionsAllowed = service.ActivePlayerController.CanSendOrders();
		this.previousCursor = AgeManager.Instance.Cursor;
		this.WorkerGroupsTable.Enable = true;
		this.updateDrag = true;
		UnityCoroutine.StartCoroutine(this, this.UpdateDrag(), null);
		base.GuiService.GetGuiPanel<WorkersDragPanel>().Hide(true);
		if (this.City != null)
		{
			base.NeedRefresh = true;
		}
		if (this.BoostersEnumerator == null)
		{
			bool highDefinition = AgeUtils.HighDefinition;
			AgeUtils.HighDefinition = false;
			GameEmpireScreen guiPanel = base.GuiService.GetGuiPanel<GameEmpireScreen>();
			this.BoostersEnumerator = UnityEngine.Object.Instantiate<BoosterEnumerator>(guiPanel.BoostersEnumerator);
			AgeUtils.HighDefinition = highDefinition;
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
		yield return base.OnLoadGame();
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
					Texture2D image;
					if (base.GuiService.GuiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Small, out image))
					{
						this.FidsSymbols[i].Image = image;
						this.FidsSymbols[i].TintColor = extendedGuiElement.Color;
					}
					this.ProdPerPopFIDSValues[i].TintColor = extendedGuiElement.Color;
					this.CityTileFIDSValues[i].TintColor = extendedGuiElement.Color;
					this.ModifierFIDSValues[i].TintColor = extendedGuiElement.Color;
					this.TotalFIDSValues[i].TintColor = extendedGuiElement.Color;
				}
			}
		}
		if (this.WorkersGroups.Length != 0)
		{
			AgeTransform workersTable = this.WorkersGroups[0].WorkersTable;
			AgeTransform component = this.WorkersGroups[0].WorkerSymbolPrefab.GetComponent<AgeTransform>();
			this.childWidth = component.Width * AgeUtils.CurrentUpscaleFactor();
			this.childHeight = component.Height * AgeUtils.CurrentUpscaleFactor();
			this.workersPerLine = Mathf.RoundToInt((workersTable.Width - workersTable.HorizontalMargin) / (this.childWidth + workersTable.HorizontalSpacing));
		}
		string[] array = new string[this.prodPerPopFIDSTypes.Count];
		array[0] = string.Format("{0},{1},{2},!{3}", new object[]
		{
			SimulationProperties.CityFood,
			SimulationProperties.CityGrowth,
			SimulationProperties.NetCityGrowth,
			SimulationProperties.CityGrowthUpkeep
		});
		array[1] = string.Format("{0},{1},{2},!{3}", new object[]
		{
			SimulationProperties.CityIndustry,
			SimulationProperties.CityProduction,
			SimulationProperties.NetCityProduction,
			SimulationProperties.CityProductionUpkeep
		});
		array[2] = string.Format("{0},{1},{2},!{3}", new object[]
		{
			SimulationProperties.CityScience,
			SimulationProperties.CityResearch,
			SimulationProperties.NetCityResearch,
			SimulationProperties.CityResearchUpkeep
		});
		array[3] = string.Format("{0},{1},{2},!{3}", new object[]
		{
			SimulationProperties.CityDust,
			SimulationProperties.CityMoney,
			SimulationProperties.NetCityMoney,
			SimulationProperties.TotalCityMoneyUpkeep
		});
		array[4] = string.Format("{0},{1},{2}", SimulationProperties.CityCityPoint, SimulationProperties.CityEmpirePoint, SimulationProperties.NetCityEmpirePoint);
		string[] array2 = new string[this.prodPerPopFIDSTypes.Count];
		array2[0] = string.Format("%ModifierCategory{0}Title", SimulationProperties.CityFood);
		array2[1] = string.Format("%ModifierCategory{0}Title", SimulationProperties.CityIndustry);
		array2[2] = string.Format("%ModifierCategory{0}Title", SimulationProperties.CityScience);
		array2[3] = string.Format("%ModifierCategory{0}Title", SimulationProperties.CityDust);
		array2[4] = string.Format("%ModifierCategory{0}Title", SimulationProperties.CityCityPoint);
		string[] array3 = new string[this.prodPerPopFIDSTypes.Count];
		array3[0] = string.Format("{0},{1}", SimulationProperties.DistrictFood, SimulationProperties.DistrictFoodNet);
		array3[1] = string.Format("{0},{1}", SimulationProperties.DistrictIndustry, SimulationProperties.DistrictIndustryNet);
		array3[2] = string.Format("{0},{1}", SimulationProperties.DistrictScience, SimulationProperties.DistrictScienceNet);
		array3[3] = string.Format("{0},{1}", SimulationProperties.DistrictDust, SimulationProperties.DistrictDustNet);
		array3[4] = string.Format("{0},{1}", SimulationProperties.DistrictCityPoint, SimulationProperties.DistrictCityPointNet);
		int num = 0;
		while (num < this.prodPerPopFIDSTypes.Count && !(this.ProdPerPopFIDSValues[num].AgeTransform.AgeTooltip == null))
		{
			this.ProdPerPopFIDSValues[num].AgeTransform.AgeTooltip.Class = "FIDS";
			this.ProdPerPopFIDSValues[num].AgeTransform.AgeTooltip.Content = this.prodPerPopFIDSTypes[num];
			this.ProdPerPopFIDSValues[num].AgeTransform.AgeTooltip.ClientData = new SimulationPropertyTooltipData(this.prodPerPopFIDSTypes[num], this.prodPerPopFIDSTypes[num], this.City);
			this.CityTileFIDSValues[num].AgeTransform.AgeTooltip.Class = "FIDS";
			this.CityTileFIDSValues[num].AgeTransform.AgeTooltip.Content = this.cityTileFIDSTypes[num];
			this.CityTileFIDSValues[num].AgeTransform.AgeTooltip.ClientData = new SimulationPropertyTooltipData(this.cityTileFIDSTypes[num], array3[num], this.City);
			this.ModifierFIDSValues[num].AgeTransform.AgeTooltip.Class = "FIDS";
			this.ModifierFIDSValues[num].AgeTransform.AgeTooltip.Content = array2[num];
			this.ModifierFIDSValues[num].AgeTransform.AgeTooltip.ClientData = new SimulationPropertyTooltipData(array2[num], array[num], this.City);
			this.TotalFIDSValues[num].AgeTransform.AgeTooltip.Class = "FIDS";
			this.TotalFIDSValues[num].AgeTransform.AgeTooltip.Content = this.totalFIDSTypes[num];
			this.TotalFIDSValues[num].AgeTransform.AgeTooltip.ClientData = new SimulationPropertyTooltipData(this.totalFIDSTypes[num], GuiSimulation.Instance.FIMSTooltipTotal[num], this.City);
			num++;
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
		int num = 0;
		while (num < this.prodPerPopFIDSTypes.Count && !(this.ProdPerPopFIDSValues[num].AgeTransform.AgeTooltip == null))
		{
			this.ProdPerPopFIDSValues[num].AgeTransform.AgeTooltip.ClientData = null;
			this.CityTileFIDSValues[num].AgeTransform.AgeTooltip.ClientData = null;
			this.TotalFIDSValues[num].AgeTransform.AgeTooltip.ClientData = null;
			this.ModifierFIDSValues[num].AgeTransform.AgeTooltip.ClientData = null;
			num++;
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
			if (!(component != null))
			{
				workersDragPanel.CancelDrag();
				base.NeedRefresh = true;
				return;
			}
			if (!(workersDragPanel.StartingWorkerType != component.WorkerType))
			{
				workersDragPanel.CancelDrag();
				base.NeedRefresh = true;
				return;
			}
			workersDragPanel.ValidateDrag(this.WorkerGroupsTable, component.WorkerType, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
			if (TutorialManager.IsActivated)
			{
				IEventService service = Services.GetService<IEventService>();
				Diagnostics.Assert(service != null);
				IGameService service2 = Services.GetService<IGameService>();
				Diagnostics.Assert(service2 != null);
				Diagnostics.Assert(service2.Game as global::Game != null);
				service.Notify(new EventTutorialWorkerDragged(this.City.Empire, workersDragPanel.StartingWorkerType, component.WorkerType));
				return;
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
			WorkersDragPanel guiPanel = base.GuiService.GetGuiPanel<WorkersDragPanel>();
			if (Input.GetMouseButtonDown(0))
			{
				if (guiPanel.DragInProgress)
				{
					if (guiPanel.DragMoved)
					{
						this.CheckDragTarget(guiPanel);
					}
					else
					{
						guiPanel.CancelDrag();
						base.NeedRefresh = true;
					}
				}
				else if (AgeManager.Instance.ActiveControl != null)
				{
					WorkerSymbol component = AgeManager.Instance.ActiveControl.GetComponent<WorkerSymbol>();
					if (component != null)
					{
						guiPanel.InitDrag(this.City, component.WorkerType, component.DragQuantity);
					}
				}
			}
			else if (Input.GetMouseButtonUp(0))
			{
				if (guiPanel.DragInProgress)
				{
					if (!guiPanel.DragMoved)
					{
						guiPanel.StartDrag();
						base.NeedRefresh = true;
					}
					else
					{
						guiPanel.DragInProgress = false;
						this.CheckDragTarget(guiPanel);
					}
				}
			}
			else if (guiPanel.DragInProgress && !guiPanel.DragMoved && this.previousCursor != AgeManager.Instance.Cursor)
			{
				guiPanel.StartDrag();
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

	private void RefreshBoosterStock(AgeTransform tableitem, GuiStackedBooster guiStackedBooster, int index)
	{
		if (this.City == null || this.City.Empire == null)
		{
			return;
		}
		BoosterStock component = tableitem.GetComponent<BoosterStock>();
		if (component != null)
		{
			component.Bind(this.City.Empire);
			component.SetContent(guiStackedBooster);
			component.RefreshContent();
		}
	}

	private void DepartmentOfEducation_VaultItemsCollectionChange(object sender, CollectionChangeEventArgs e)
	{
		base.NeedRefresh = true;
	}

	public override void Initialize()
	{
		this.database = Databases.GetDatabase<BoosterDefinition>(false);
		this.vaultBoosters = new List<VaultItem>();
		this.stackedBoosters = new List<GuiStackedBooster>();
		this.stackedBoosters2 = new List<GuiStackedBooster>();
		this.refreshDelegate = new AgeTransform.RefreshTableItem<GuiStackedBooster>(this.RefreshBoosterStock);
		this.OriginalMargins = new Dictionary<string, float>();
		foreach (AgeTransform ageTransform in base.AgeTransform.GetChildren())
		{
			if (ageTransform.name.Contains("CityTile") || ageTransform.name.Contains("Total") || ageTransform.name.Contains("Modifiers"))
			{
				if (!this.OriginalMargins.ContainsKey(ageTransform.name))
				{
					this.OriginalMargins.Add(ageTransform.name, 0f);
				}
				this.OriginalMargins[ageTransform.name] = ageTransform.PixelMarginBottom / (AgeUtils.HighDefinition ? 1.5f : 1f);
			}
		}
		base.Initialize();
	}

	public new virtual void Unload()
	{
		base.Unload();
		this.vaultBoosters = null;
		this.stackedBoosters.Clear();
		this.stackedBoosters2.Clear();
		this.refreshDelegate = null;
		this.database = null;
		this.BoostersEnumerator.AgeTransform.DestroyAllChildren();
		UnityEngine.Object.Destroy(this.BoostersEnumerator.gameObject);
		this.BoostersEnumerator = null;
		this.OriginalMargins.Clear();
		this.BoostersTable.DestroyAllChildren();
		UnityEngine.Object.Destroy(this.BoostersTable.gameObject);
		this.BoostersTable = null;
		this.BoostersTable2.DestroyAllChildren();
		UnityEngine.Object.Destroy(this.BoostersTable2.gameObject);
		this.BoostersTable2 = null;
	}

	public float TopMargin;

	public float Separator;

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

	private int workersPerLine;

	private float childWidth;

	private float childHeight;

	private IEndTurnService endTurnService;

	private bool interactionsAllowed;

	private BoosterEnumerator BoostersEnumerator;

	private AgeTransform BoostersTable;

	private List<BoosterStock> BoosterStocks;

	private List<VaultItem> vaultBoosters;

	private List<GuiStackedBooster> stackedBoosters;

	private AgeTransform.RefreshTableItem<GuiStackedBooster> refreshDelegate;

	private DepartmentOfEducation departmentOfEducation;

	private Dictionary<string, float> OriginalMargins;

	private List<GuiStackedBooster> stackedBoosters2;

	private AgeTransform BoostersTable2;

	private IDatabase<BoosterDefinition> database;

	public bool ParentIsCityListScreen;
}
