using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class GameResearchScreen : GuiPlayerControllerScreen
{
	public GameResearchScreen()
	{
		base.EnableCaptureBackgroundOnShow = true;
		base.EnableMenuAudioLayer = true;
		base.EnableNavigation = true;
	}

	public float CurrentZoomFactor
	{
		get
		{
			return this.currentZoomFactor;
		}
		set
		{
			if (this.currentZoomFactor != value)
			{
				this.ResearchViewModifier.StartScale = this.currentZoomFactor;
				this.currentZoomFactor = value;
				this.ResearchViewModifier.EndScale = this.currentZoomFactor;
				this.ResearchViewModifier.StartAnimation();
				base.StartCoroutine(this.TransformHalfway(this.currentZoomFactor == 0.5f));
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

	public void RefreshBuyout(AgeControlButton buyoutButton)
	{
		DepartmentOfTheTreasury agency = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		ConstructibleElement technology = null;
		if (this.departmentOfScience.ResearchQueue.Peek() != null)
		{
			technology = this.departmentOfScience.ResearchQueue.Peek().ConstructibleElement;
		}
		float buyOutTechnologyCost = this.departmentOfScience.GetBuyOutTechnologyCost(technology);
		float num = -buyOutTechnologyCost;
		string text = GuiFormater.FormatInstantCost(base.Empire, buyOutTechnologyCost, DepartmentOfTheTreasury.Resources.EmpireMoney, true, 0);
		string content;
		if (buyOutTechnologyCost != 3.40282347E+38f)
		{
			if (agency.IsTransferOfResourcePossible(base.Empire, DepartmentOfTheTreasury.Resources.TechnologiesBuyOut, ref num) && this.interactionsAllowed)
			{
				buyoutButton.AgeTransform.Enable = true;
				buyoutButton.AgeTransform.Alpha = 1f;
				this.ResearchBuyoutCostLabel.Text = text;
				content = AgeLocalizer.Instance.LocalizeString("%ResearchBuyoutAvailableFormat").Replace("$Cost", text);
			}
			else
			{
				buyoutButton.AgeTransform.Enable = false;
				buyoutButton.AgeTransform.Alpha = 0.5f;
				this.ResearchBuyoutCostLabel.Text = text;
				content = AgeLocalizer.Instance.LocalizeString("%ResearchBuyoutUnavailableFormat").Replace("$Cost", text);
			}
		}
		else
		{
			buyoutButton.AgeTransform.Enable = false;
			buyoutButton.AgeTransform.Alpha = 0.5f;
			this.ResearchBuyoutCostLabel.Text = "%ResearchVoidSymbol";
			content = AgeLocalizer.Instance.LocalizeString("%ResearchBuyoutNoSelection");
		}
		if (buyoutButton.AgeTransform.AgeTooltip != null)
		{
			buyoutButton.AgeTransform.AgeTooltip.Content = content;
		}
	}

	public override void Bind(global::Empire empire)
	{
		base.Bind(empire);
		this.departmentOfScience = base.Empire.GetAgency<DepartmentOfScience>();
		this.departmentOfIndustry = base.Empire.GetAgency<DepartmentOfIndustry>();
		this.departmentOfTheInterior = base.Empire.GetAgency<DepartmentOfTheInterior>();
		Diagnostics.Assert(this.departmentOfScience != null);
		Diagnostics.Assert(this.departmentOfIndustry != null);
		Diagnostics.Assert(this.departmentOfTheInterior != null);
		this.InitializeGuiElements();
		this.departmentOfScience.TechnologyUnlocked += this.DepartmentOfScience_TechnologyUnlocked;
		this.departmentOfScience.ResearchQueueChanged += this.DepartmentOfScience_ResearchQueueChanged;
		if (base.PlayerController != null)
		{
			base.PlayerController.GameInterface.StateChange += this.GameInterface_StateChange;
		}
		base.NeedRefresh = true;
	}

	public override void ShowOrRefresh(params object[] parameters)
	{
		base.ShowOrRefresh(parameters);
		bool flag = false;
		if (parameters.Length == 1)
		{
			this.inputName = (parameters[0] as string);
			IDatabase<DepartmentOfScience.ConstructibleElement> database = Databases.GetDatabase<DepartmentOfScience.ConstructibleElement>(false);
			if (database.ContainsKey(this.inputName))
			{
				flag = true;
				this.FocusOnTechnology(database.GetValue(this.inputName) as TechnologyDefinition);
			}
		}
		if (!flag)
		{
			this.FocusOnCurrentEra();
		}
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		base.Empire.Refresh(false);
		DepartmentOfScience agency = base.Empire.GetAgency<DepartmentOfScience>();
		DepartmentOfTheTreasury agency2 = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		this.ComputeCurrentEraNumber();
		this.CurrentEraNumber.Text = AgeUtils.ToRoman(this.currentEraNumber);
		this.ResearchErasTable.RefreshChildrenIList<ResearchEraFrame.TechnologyEra>(this.eras, this.refreshEraDelegate, true, false);
		Construction construction = agency.ResearchQueue.Peek();
		bool flag = construction != null;
		float quantity;
		if (flag)
		{
			quantity = DepartmentOfTheTreasury.GetProductionCostWithBonus(base.Empire.SimulationObject, construction.ConstructibleElement, DepartmentOfTheTreasury.Resources.EmpireResearch);
		}
		else
		{
			quantity = agency.GetResearchPropertyValue(SimulationProperties.TechnologyCost);
		}
		this.TechnologyCostValue.Text = GuiFormater.FormatQuantity(quantity, SimulationProperties.EmpireResearch, 1);
		float quantity2;
		agency2.TryGetNetResourceValue(base.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpireResearch, out quantity2, false);
		this.EmpireOutputValue.Text = GuiFormater.FormatQuantity(quantity2, SimulationProperties.EmpireResearch, 0);
		if (flag)
		{
			float num = agency2.ComputeConstructionProgress(base.Empire, construction);
			this.ResearchProgress.Text = Mathf.RoundToInt(num * 100f).ToString() + "%";
			int num2 = agency2.ComputeConstructionRemainingTurn(base.Empire, construction);
			num2 = Mathf.Max(num2, 1);
			if (num2 == 2147483647)
			{
				this.ResearchTurns.Text = GuiFormater.Infinite.ToString();
			}
			else
			{
				this.ResearchTurns.Text = num2.ToString();
			}
			TechnologyDefinition technologyDefinition = construction.ConstructibleElement as TechnologyDefinition;
			if (technologyDefinition != null)
			{
				this.TechnologyName.Text = DepartmentOfScience.GetTechnologyTitle(technologyDefinition);
				this.TechnologyImage.Image = DepartmentOfScience.GetTechnologyImage(technologyDefinition, GuiPanel.IconSize.Small);
			}
			DepartmentOfScience.BuildTechnologyTooltip(technologyDefinition, base.Empire, this.TechnologyName.AgeTransform.AgeTooltip, MultipleConstructibleTooltipData.TechnologyState.Normal);
		}
		else
		{
			this.TechnologyName.Text = "%ResearchNoneTitle";
			this.TechnologyImage.Image = null;
			this.ResearchProgress.Text = "%ResearchVoidSymbol";
			this.ResearchTurns.Text = "%ResearchVoidSymbol";
			if (this.TechnologyName.AgeTransform.AgeTooltip != null)
			{
				this.TechnologyName.AgeTransform.AgeTooltip.Class = string.Empty;
				this.TechnologyName.AgeTransform.AgeTooltip.Content = string.Empty;
				this.TechnologyName.AgeTransform.AgeTooltip.ClientData = null;
			}
			if (this.TechnologyImage.AgeTransform.AgeTooltip != null)
			{
				this.TechnologyImage.AgeTransform.AgeTooltip.Class = string.Empty;
				this.TechnologyImage.AgeTransform.AgeTooltip.Content = string.Empty;
				this.TechnologyImage.AgeTransform.AgeTooltip.ClientData = null;
			}
		}
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		if (service != null && service.IsShared(DownloadableContent11.ReadOnlyName) && base.Empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitReplicants1))
		{
			this.TechnologyStatsGroup.Visible = false;
			this.ResearchBuyoutButton.AgeTransform.Visible = true;
			this.ResearchCompletionGroup.Visible = false;
			this.RefreshBuyout(this.ResearchBuyoutButton);
			return;
		}
		this.TechnologyStatsGroup.Visible = true;
		this.ResearchBuyoutButton.AgeTransform.Visible = false;
		this.ResearchCompletionGroup.Visible = true;
	}

	public override void Unbind()
	{
		this.ResearchErasTable.DestroyAllChildren();
		if (base.PlayerController != null)
		{
			base.PlayerController.GameInterface.StateChange -= this.GameInterface_StateChange;
		}
		if (this.departmentOfScience != null)
		{
			this.departmentOfScience.TechnologyUnlocked -= this.DepartmentOfScience_TechnologyUnlocked;
			this.departmentOfScience.ResearchQueueChanged -= this.DepartmentOfScience_ResearchQueueChanged;
			this.departmentOfScience = null;
		}
		if (this.TechnologyName.AgeTransform.AgeTooltip != null)
		{
			this.TechnologyName.AgeTransform.AgeTooltip.Class = string.Empty;
			this.TechnologyName.AgeTransform.AgeTooltip.Content = string.Empty;
			this.TechnologyName.AgeTransform.AgeTooltip.ClientData = null;
		}
		if (this.TechnologyImage.AgeTransform.AgeTooltip != null)
		{
			this.TechnologyImage.AgeTransform.AgeTooltip.Class = string.Empty;
			this.TechnologyImage.AgeTransform.AgeTooltip.Content = string.Empty;
			this.TechnologyImage.AgeTransform.AgeTooltip.ClientData = null;
		}
		base.Unbind();
	}

	protected override IEnumerator OnHide(bool instant)
	{
		base.NeedRefresh = false;
		this.updateDrag = false;
		base.GuiService.GetGuiPanel<ControlBanner>().OnHideScreen(GameScreenType.Research);
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		this.EndTurnService = Services.GetService<IEndTurnService>();
		this.viewHalfHeight = this.ResearchView.Height / 0.5f * 0.5f;
		this.SaveLayoutButton.AgeTransform.Visible = false;
		this.refreshEraDelegate = new AgeTransform.RefreshTableItem<ResearchEraFrame.TechnologyEra>(this.RefreshEra);
		this.matchList = new List<TechnologyFrame>();
		this.firstShow = true;
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		this.interactionsAllowed = (base.PlayerController.CanSendOrders() && base.Empire.Index == base.PlayerController.Empire.Index);
		this.ResearchErasTable.Enable = this.interactionsAllowed;
		this.LayoutGroup.Visible = false;
		this.FocusCircle.Visible = false;
		this.MaximizeToggle.State = true;
		this.OnMaximizeCB(this.MaximizeToggle.gameObject);
		for (int i = 0; i < this.ResearchErasTable.GetChildren().Count; i++)
		{
			this.ResearchErasTable.GetChildren()[i].GetComponent<ResearchEraFrame>().SetSimpleMode(false);
		}
		base.GuiService.GetGuiPanel<ControlBanner>().OnShowScreen(GameScreenType.Research);
		base.GuiService.Show(typeof(EmpireBannerPanel), new object[]
		{
			EmpireBannerPanel.Full
		});
		this.UpdateViewPivot();
		this.SearchTexfield.FocusLoss();
		this.updateDrag = true;
		base.StartCoroutine(this.UpdateDrag());
		this.RefreshContent();
		this.OnSearchClearCB(null);
		if (this.firstShow)
		{
			this.firstShow = false;
			this.ComputeCurrentEraNumber();
			this.FocusOnEra(this.currentEraNumber);
		}
		yield break;
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.Hide(true);
		this.matchList.Clear();
		this.matchList = null;
		this.refreshEraDelegate = null;
		this.Unbind();
		this.ResearchErasTable.DestroyAllChildren();
		this.eras.Clear();
		this.EndTurnService = null;
		base.OnUnloadGame(game);
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		base.UseRefreshLoop = true;
		yield break;
	}

	private void ComputeCurrentEraNumber()
	{
		this.currentEraNumber = -1;
		for (int i = 0; i < this.eras.Count; i++)
		{
			ResearchEraFrame.TechnologyEra technologyEra = this.eras[i];
			DepartmentOfScience.ConstructibleElement.State technologyState = this.departmentOfScience.GetTechnologyState(technologyEra.Definition);
			if (technologyState == DepartmentOfScience.ConstructibleElement.State.Researched && technologyEra.EraNumber > this.currentEraNumber)
			{
				this.currentEraNumber = technologyEra.EraNumber;
			}
		}
	}

	private void DepartmentOfScience_ResearchQueueChanged(object sender, ConstructionChangeEventArgs e)
	{
		base.NeedRefresh = true;
	}

	private void DepartmentOfScience_TechnologyUnlocked(object sender, ConstructibleElementEventArgs e)
	{
		base.NeedRefresh = true;
	}

	private void EndTurnService_GameClientStateChange(object sender, GameClientStateChangeEventArgs e)
	{
		if (base.IsVisible && base.PlayerController != null)
		{
			bool flag = base.PlayerController.CanSendOrders() && base.Empire.Index == base.PlayerController.Empire.Index;
			this.ResearchErasTable.Enable = this.interactionsAllowed;
			if (this.interactionsAllowed != flag)
			{
				this.interactionsAllowed = flag;
				this.ResearchErasTable.Enable = this.interactionsAllowed;
				if (!flag && MessagePanel.Instance.IsVisible)
				{
					MessagePanel.Instance.Hide(false);
				}
			}
		}
	}

	private void DisplayMatchResult(AgeTransform tableitem, TechnologyFrame techno, int index)
	{
		Rect position;
		techno.AgeTransform.ComputeGlobalPosition(out position);
		position.x -= this.ResearchView.X;
		position.y -= this.ResearchView.Y;
		tableitem.Position = position;
		tableitem.StartAllModifiers(true, false);
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

	private void GameInterface_StateChange(object sender, FiniteStateChangeEventArgs e)
	{
		if (base.IsVisible)
		{
			FiniteStateChangeAction action = e.Action;
			if (action == FiniteStateChangeAction.Begun)
			{
				if (e.State.GetType() == typeof(GameClientState_Turn_Begin))
				{
					base.NeedRefresh = true;
				}
			}
		}
	}

	private void FocusOnCurrentEra()
	{
		this.FocusOnEra(this.currentEraNumber);
	}

	private void FocusOnEra(int eraNumber)
	{
		ResearchEraFrame component = this.ResearchErasTable.GetChildren()[eraNumber - 1].GetComponent<ResearchEraFrame>();
		this.ResearchView.X = -component.AgeTransform.X + 0.5f * ((float)Screen.width / this.CurrentZoomFactor) - component.AgeTransform.Width * 0.5f;
		if (this.ResearchView.X > 0f)
		{
			this.ResearchView.X = 0f;
		}
		else if (this.ResearchView.X < (float)Screen.width - this.ResearchView.Width)
		{
			this.ResearchView.X = (float)Screen.width - this.ResearchView.Width;
		}
		this.UpdateViewPivot();
		this.lastOffset = Vector2.zero;
	}

	private void FocusOnTechnology(TechnologyDefinition technology)
	{
		List<ResearchEraFrame> children = this.ResearchErasTable.GetChildren<ResearchEraFrame>(true);
		for (int i = 0; i < children.Count; i++)
		{
			if (children[i].Technologies.Contains(technology))
			{
				this.FocusOnEra(children[i].Era.EraNumber);
				this.OnSelectTechnology(children[i].GetTechnologyAgeTransform(technology));
				break;
			}
		}
	}

	private void InitializeGuiElements()
	{
		if (this.researchCategories == null)
		{
			this.researchCategories = new List<string>();
			this.researchCategories.Add("ResearchCategoryMilitary");
			this.researchCategories.Add("ResearchCategoryScience");
			this.researchCategories.Add("ResearchCategoryEmpire");
			this.researchCategories.Add("ResearchCategoryEconomy");
		}
		this.eras.Clear();
		IDatabase<DepartmentOfScience.ConstructibleElement> database = Databases.GetDatabase<DepartmentOfScience.ConstructibleElement>(false);
		DepartmentOfScience.ConstructibleElement[] values = database.GetValues();
		for (int i = 0; i < values.Length; i++)
		{
			TechnologyEraDefinition technologyEraDefinition = values[i] as TechnologyEraDefinition;
			if (technologyEraDefinition != null)
			{
				this.eras.Add(new ResearchEraFrame.TechnologyEra(technologyEraDefinition));
			}
		}
		this.ResearchErasTable.ReserveChildren(this.eras.Count, this.ResearchEraPrefab, "Era");
		this.ResearchErasTable.RefreshChildrenIList<ResearchEraFrame.TechnologyEra>(this.eras, new AgeTransform.RefreshTableItem<ResearchEraFrame.TechnologyEra>(this.SetupEra), true, false);
		this.ResearchErasTable.ArrangeChildren();
		this.ResearchErasTable.HorizontalMargin = (float)Screen.width * 0.5f - this.ResearchErasTable.ChildWidth * 0.5f;
		this.ResearchErasTable.HorizontalSpacing = 0.5f * this.ResearchErasTable.ChildWidth;
		this.ResearchErasTable.ArrangeChildren();
		this.ResearchView.Width = this.ResearchErasTable.Width + this.ResearchErasTable.HorizontalMargin - this.ResearchErasTable.HorizontalSpacing;
	}

	private void OnCloseCB(GameObject obj)
	{
		this.HandleCancelRequest();
	}

	private void OnEditLayoutCB(GameObject obj)
	{
		this.SaveLayoutButton.AgeTransform.Visible = this.LayoutToggle.State;
		TechnologyFrame[] componentsInChildren = base.GetComponentsInChildren<TechnologyFrame>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].ActivateMarkup(this.LayoutToggle.State);
		}
	}

	private void OnGainFocusSearchCB(GameObject obj)
	{
		this.SearchLabel.Text = string.Empty;
		this.SearchTexfield.OnPositionRecomputed();
	}

	private void OnLoseFocusSearchCB(GameObject obj)
	{
		this.SearchTexfield.OnPositionRecomputed();
	}

	private void OnMaximizeCB(GameObject obj)
	{
		if (this.CurrentZoomFactor != 1f)
		{
			this.CurrentZoomFactor = 1f;
			this.MinimizeToggle.State = false;
			this.MaximizeToggle.State = true;
		}
		this.lastOffset = Vector2.zero;
	}

	private void OnMinimizeCB(GameObject obj)
	{
		if (this.CurrentZoomFactor != 0.5f)
		{
			this.CurrentZoomFactor = 0.5f;
			this.MaximizeToggle.State = false;
			this.MinimizeToggle.State = true;
		}
		this.lastOffset = Vector2.zero;
	}

	private void OnSaveLayoutCB(GameObject obj)
	{
	}

	private void OnSearchClearCB(GameObject obj)
	{
		this.SearchResultContainer.Visible = false;
		this.SearchLabel.Text = "%ResearchDefaultSearchText";
		this.SearchResultLabel.Text = string.Empty;
		this.SearchResultLabel.AgeTransform.Visible = false;
		this.SearchNextButton.AgeTransform.Visible = false;
	}

	private void OnSearchNextCB(GameObject obj)
	{
		this.currentMatch = (this.currentMatch + 1) % this.matchList.Count;
		this.FocusOnTechnology(this.matchList[this.currentMatch].TechnologyDefinition);
	}

	private void OnSelectTechnology(AgeTransform selectTechnology)
	{
		Rect rect;
		selectTechnology.ComputeGlobalPosition(out rect);
		this.FocusCircle.X = rect.x + 0.5f * rect.width - 0.5f * this.FocusCircle.Width - this.ResearchView.X;
		this.FocusCircle.Y = rect.y + 0.5f * rect.height - 0.5f * this.FocusCircle.Height - this.ResearchView.Y;
		this.FocusCircle.Visible = true;
		this.FocusCircle.StartAllModifiers(true, false);
		base.StartCoroutine(this.FadeCircle());
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

	private void OnValidateSearchCB(GameObject obj)
	{
		this.matchList.Clear();
		if (!string.IsNullOrEmpty(this.SearchLabel.Text))
		{
			List<ResearchEraFrame> children = this.ResearchErasTable.GetChildren<ResearchEraFrame>(true);
			for (int i = 0; i < children.Count; i++)
			{
				List<TechnologyFrame> children2 = children[i].TechnologiesContainer.GetChildren<TechnologyFrame>(true);
				for (int j = 0; j < children2.Count; j++)
				{
					for (int k = 0; k < children2[j].TagsList.Count; k++)
					{
						if (children2[j].TagsList[k].Contains(this.SearchLabel.Text.ToUpper()))
						{
							this.matchList.Add(children2[j]);
							k = children2[j].TagsList.Count;
						}
					}
				}
			}
		}
		this.SearchResultLabel.AgeTransform.Visible = true;
		if (this.matchList.Count == 0)
		{
			this.SearchResultLabel.Text = "%ResearchNoOccurrenceTitle";
			this.SearchNextButton.AgeTransform.Visible = false;
			this.SearchResultContainer.Visible = false;
		}
		else
		{
			if (this.matchList.Count == 1)
			{
				this.SearchResultLabel.Text = "%ResearchSingleOccurrenceTitle";
			}
			else
			{
				this.SearchResultLabel.Text = string.Format(AgeLocalizer.Instance.LocalizeString("%ResearchMultipleOccurrenceFormat"), this.matchList.Count.ToString());
			}
			this.SearchResultContainer.ReserveChildren(this.matchList.Count, this.SearchResultPrefab, "Item");
			this.SearchResultContainer.RefreshChildrenIList<TechnologyFrame>(this.matchList, new AgeTransform.RefreshTableItem<TechnologyFrame>(this.DisplayMatchResult), true, true);
			for (int l = this.matchList.Count; l < this.SearchResultContainer.GetChildren().Count; l++)
			{
				this.SearchResultContainer.GetChildren()[l].Visible = false;
			}
			this.SearchNextButton.AgeTransform.Visible = true;
			this.SearchResultContainer.Visible = true;
		}
		AgeManager.Instance.FocusedControl = null;
	}

	private void RefreshEra(AgeTransform tableitem, ResearchEraFrame.TechnologyEra era, int index)
	{
		ResearchEraFrame component = tableitem.GetComponent<ResearchEraFrame>();
		component.RefreshEra(this.currentEraNumber >= era.EraNumber, this.departmentOfIndustry, this.departmentOfTheInterior, this.departmentOfScience);
		component.SetSimpleMode(this.currentZoomFactor == 0.5f);
	}

	private void SetupEra(AgeTransform tableitem, ResearchEraFrame.TechnologyEra era, int index)
	{
		ResearchEraFrame component = tableitem.GetComponent<ResearchEraFrame>();
		component.SetupEra(base.Empire, era, base.gameObject, this.researchCategories);
	}

	private IEnumerator TransformHalfway(bool zoomedOut)
	{
		bool proceed = true;
		while (proceed)
		{
			if (this.ResearchView.GetComponent<AgeModifierScale>().IsHalfComplete())
			{
				for (int i = 0; i < this.ResearchErasTable.GetChildren().Count; i++)
				{
					this.ResearchErasTable.GetChildren()[i].GetComponent<ResearchEraFrame>().SetSimpleMode(zoomedOut);
				}
			}
			if (!this.ResearchView.ModifiersRunning)
			{
				proceed = false;
			}
			yield return null;
		}
		yield break;
	}

	private IEnumerator UpdateDrag()
	{
		this.dragInProgress = false;
		this.displacementSpeed = 0f;
		this.lastCursor = AgeManager.Instance.Cursor;
		while (this.updateDrag)
		{
			if (!base.GuiService.GetGuiPanel<TutorialHighlightPanel>().IsVisible)
			{
				Vector2 newCursorPosition = AgeManager.Instance.Cursor;
				if (!this.dragInProgress)
				{
					if (newCursorPosition.x <= 16f)
					{
						this.displacementSpeed = this.MaxAutoScrollSpeed;
					}
					else if (newCursorPosition.x >= (float)Screen.width - 16f - 1f)
					{
						this.displacementSpeed = -this.MaxAutoScrollSpeed;
					}
				}
				if (!this.dragInProgress && Input.GetKey(KeyCode.LeftArrow))
				{
					this.displacementSpeed = this.MaxAutoScrollSpeed;
				}
				else if (!this.dragInProgress && Input.GetKey(KeyCode.RightArrow))
				{
					this.displacementSpeed = -this.MaxAutoScrollSpeed;
				}
				if (Input.GetMouseButtonDown(0) && !this.dragAlmostStarted)
				{
					this.dragAlmostStarted = true;
					this.mousePositionWhenClicked = newCursorPosition;
				}
				if (this.dragAlmostStarted && Input.GetMouseButtonUp(0))
				{
					this.dragAlmostStarted = false;
					this.mousePositionWhenClicked = Vector2.zero;
				}
				if (this.dragAlmostStarted && Mathf.Abs(newCursorPosition.x - this.mousePositionWhenClicked.x) > 15f && !this.dragInProgress && !this.LayoutToggle.State)
				{
					this.dragInProgress = true;
					this.displacementSpeed = 0f;
					this.lastOffset.x = 0f;
				}
				if (this.dragInProgress)
				{
					if (newCursorPosition != this.lastCursor)
					{
						this.ResearchView.X += newCursorPosition.x - this.lastCursor.x;
						this.lastOffset = newCursorPosition - this.lastCursor;
					}
					if (Input.GetMouseButtonUp(0))
					{
						this.dragInProgress = false;
						this.displacementSpeed = this.lastOffset.x / Time.deltaTime;
						this.displacementSpeed = ((this.displacementSpeed <= 2f * this.MaxRemainingSpeed) ? this.displacementSpeed : (2f * this.MaxRemainingSpeed));
						this.displacementSpeed = ((this.displacementSpeed >= -this.MaxRemainingSpeed) ? this.displacementSpeed : (-this.MaxRemainingSpeed));
					}
				}
				if (!this.dragInProgress && Mathf.Abs(this.displacementSpeed) > 0.01f)
				{
					AgeManager.Instance.OverrolledTransform = null;
					this.ResearchView.X = this.ResearchView.X + this.displacementSpeed * Time.deltaTime;
					if (this.displacementSpeed > 0f)
					{
						this.displacementSpeed -= this.SpeedDampening * Time.deltaTime;
						if (this.displacementSpeed < 0.01f)
						{
							this.displacementSpeed = 0f;
						}
					}
					else if (this.displacementSpeed < 0f)
					{
						this.displacementSpeed += this.SpeedDampening * Time.deltaTime;
						if (this.displacementSpeed > -0.01f)
						{
							this.displacementSpeed = 0f;
						}
					}
				}
				if (this.ResearchView.X > 0f)
				{
					this.ResearchView.X = 0f;
				}
				else if (this.ResearchView.X < (float)Screen.width - this.ResearchView.Width)
				{
					this.ResearchView.X = (float)Screen.width - this.ResearchView.Width;
				}
				this.UpdateViewPivot();
				this.lastCursor = newCursorPosition;
			}
			yield return null;
		}
		yield break;
	}

	private void UpdateViewPivot()
	{
		Vector2 pivotOffset = this.ResearchView.PivotOffset;
		pivotOffset.x = (float)Screen.width * 0.5f - this.ResearchView.X;
		this.ResearchView.PivotOffset = pivotOffset;
	}

	public const float ZoomedMaxFactor = 1f;

	public const float ZoomedMinFactor = 0.5f;

	public const float AreaRadiusFactor = 1.2f;

	public const float AreaDisplaceFactor = 0.8f;

	public const float BorderScrollThreshold = 16f;

	public const float StartDragThreshold = 15f;

	public const float EasingAfterReleased = 0.01f;

	public float MaxAutoScrollSpeed = 400f;

	public float MaxRemainingSpeed = 600f;

	public float SpeedDampening = 400f;

	public AgeTransform ResearchView;

	public AgeModifierScale ResearchViewModifier;

	public Transform ResearchEraPrefab;

	public AgeTransform ResearchErasTable;

	public AgeTransform FocusCircle;

	public AgeTransform SearchResultContainer;

	public Transform SearchResultPrefab;

	public AgePrimitiveLabel CurrentEraNumber;

	public AgeControlToggle MaximizeToggle;

	public AgeControlToggle MinimizeToggle;

	public AgePrimitiveLabel EmpireOutputValue;

	public AgeTransform TechnologyStatsGroup;

	public AgePrimitiveLabel TechnologyCostValue;

	public AgePrimitiveImage TechnologyImage;

	public AgePrimitiveLabel TechnologyName;

	public AgeTransform ResearchCompletionGroup;

	public AgePrimitiveLabel ResearchProgress;

	public AgePrimitiveLabel ResearchTurns;

	public AgeControlButton ResearchBuyoutButton;

	public AgePrimitiveLabel ResearchBuyoutCostLabel;

	public AgePrimitiveLabel SearchLabel;

	public AgeControlTextField SearchTexfield;

	public AgePrimitiveLabel SearchResultLabel;

	public AgeControlButton SearchNextButton;

	public AgeTransform LayoutGroup;

	public AgeControlToggle LayoutToggle;

	public AgeControlButton SaveLayoutButton;

	private float currentZoomFactor = 1f;

	private int currentEraNumber;

	private DepartmentOfScience departmentOfScience;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private DepartmentOfIndustry departmentOfIndustry;

	private List<ResearchEraFrame.TechnologyEra> eras = new List<ResearchEraFrame.TechnologyEra>();

	private Vector2 lastOffset;

	private AgeTransform.RefreshTableItem<ResearchEraFrame.TechnologyEra> refreshEraDelegate;

	private string inputName = string.Empty;

	private bool updateDrag;

	private float displacementSpeed;

	private bool dragAlmostStarted;

	private Vector2 mousePositionWhenClicked = Vector2.zero;

	private bool dragInProgress;

	private Vector2 lastCursor = Vector2.zero;

	private List<string> researchCategories;

	private List<TechnologyFrame> matchList;

	private int currentMatch = -1;

	private bool firstShow;

	private IEndTurnService endTurnService;

	private bool interactionsAllowed = true;

	private float viewHalfHeight;
}
