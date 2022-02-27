using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Audio;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class GameDiplomacyScreen : GuiPlayerControllerScreen
{
	public GameDiplomacyScreen()
	{
		base.EnableCaptureBackgroundOnShow = true;
		base.EnableMenuAudioLayer = true;
		base.EnableNavigation = true;
	}

	public DepartmentOfForeignAffairs DepartmentOfForeignAffairs
	{
		get
		{
			return this.departmentOfForeignAffairs;
		}
		set
		{
			this.departmentOfForeignAffairs = value;
		}
	}

	public global::Empire LookingEmpire
	{
		get
		{
			if (this.diplomaticRelationsViewport.CurrentInspectedEmpire != null)
			{
				return this.diplomaticRelationsViewport.CurrentInspectedEmpire;
			}
			return base.Empire;
		}
	}

	public global::Empire OverrolledEmpire
	{
		get
		{
			return this.overrolledEmpire;
		}
		set
		{
			if (this.overrolledEmpire == value)
			{
				return;
			}
			this.overrolledEmpire = value;
			if (this.overrolledEmpire != null)
			{
				for (int i = 0; i < this.ambassadorInteractionPanels.Length; i++)
				{
					if (this.overrolledEmpire != null && i != this.overrolledEmpire.Index && this.ambassadorInteractionPanels[i] != null && this.ambassadorInteractionPanels[i].IsVisible && this.ambassadorInteractionPanels[i].IsHovered)
					{
						return;
					}
				}
				AmbassadorInteractionPanel ambassadorInteractionPanel = this.ambassadorInteractionPanels[this.overrolledEmpire.Index];
				ambassadorInteractionPanel.PanelTargetBounds = this.diplomaticRelationsViewport.GetAmbassadorBound(this.overrolledEmpire.Index);
				if (!ambassadorInteractionPanel.IsVisible)
				{
					ambassadorInteractionPanel.Show(new object[0]);
				}
				else if (ambassadorInteractionPanel.IsHiding)
				{
					ambassadorInteractionPanel.ShowWhenFinishedHiding(new object[0]);
				}
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

	public override bool HandleCancelRequest()
	{
		if (this.LookingEmpire.Index != base.Empire.Index)
		{
			IAudioEventService service = Services.GetService<IAudioEventService>();
			service.Play2DEvent("Gui/Interface/InGame/OpenPanelButton");
			this.OnInspect(base.Empire);
			return true;
		}
		return base.HandleCancelRequest();
	}

	public override void Bind(global::Empire empire)
	{
		if (empire == null)
		{
			throw new ArgumentNullException("empire");
		}
		base.Bind(empire);
		this.DepartmentOfForeignAffairs = base.Empire.GetAgency<DepartmentOfForeignAffairs>();
		IEventService service = Services.GetService<IEventService>();
		if (service != null)
		{
			service.EventRaise += this.EventService_EventRaise;
		}
		if (this.diplomaticRelationsViewport != null)
		{
			this.diplomaticRelationsViewport.SetPlayerEmpire(empire);
		}
		this.OverrolledEmpire = null;
		this.CreateAmbassadorInteractionPanels();
		base.NeedRefresh = true;
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		if (base.Empire == null)
		{
			return;
		}
		if (this.diplomaticRelationsViewport != null)
		{
			this.diplomaticRelationsViewport.Refresh();
		}
		float num = 0f;
		float num2 = 1f;
		MajorEmpire majorEmpire = base.Empire as MajorEmpire;
		if (majorEmpire.VictoryConditionStatuses.ContainsKey("Diplomacy") && ELCPUtilities.UseELCPPeacePointRulseset)
		{
			this.DiplomaticScoreContainer.Visible = true;
			num = base.Empire.GetPropertyValue("EmpirePeacePointStock");
			float propertyValue = base.Empire.GetPropertyValue("PeacePointBucketStock");
			float value = Mathf.Min(propertyValue, base.Empire.GetPropertyValue("TreatyPeacePointPerTurn"));
			float propertyValue2 = base.Empire.GetPropertyValue("NetEmpirePeacePoint");
			this.DiplomaticScore.Text = AgeLocalizer.Instance.LocalizeString("%ELCPDiplomacyScreenDiplomaticScoreFormat").Replace("$BucketValue", GuiFormater.FormatGui(propertyValue, false, false, false, 0));
			this.DiplomaticScore.Text = this.DiplomaticScore.Text.Replace("$BucketNet", GuiFormater.FormatGui(value, false, false, false, 0));
			this.DiplomaticScore.Text = this.DiplomaticScore.Text.Replace("$PPValue", GuiFormater.FormatGui(num, false, false, false, 0));
			this.DiplomaticScore.Text = this.DiplomaticScore.Text.Replace("$PPNet", GuiFormater.FormatGui(propertyValue2, false, false, false, 0));
			this.DiplomaticScore.Alignement = AgeTextAnchor.LowerCenter;
			this.victoryService = base.Game.Services.GetService<IVictoryManagementService>();
			foreach (VictoryCondition victoryCondition in this.victoryService.VictoryConditionsFilteredThisGame)
			{
				if (victoryCondition.Name == "Diplomacy")
				{
					for (int i = 0; i < victoryCondition.Progression.Vars.Length; i++)
					{
						if (victoryCondition.Progression.Vars[i].Name == "TargetValue")
						{
							num2 = majorEmpire.VictoryConditionStatuses["Diplomacy"].Variables[i];
						}
					}
					if (this.DiplomaticScoreContainer.AgeTooltip != null)
					{
						this.DiplomaticScoreContainer.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%VictoryConditionDiplomacyDescription").Replace("$TargetValue", GuiFormater.FormatGui(num2, false, false, false, 0));
					}
				}
			}
			this.DiplomaticGauge.Visible = false;
			this.DiplomaticGauge.GetParent().Visible = false;
			this.DiplomaticGauge.PercentRight = Mathf.Clamp(100f * (num / num2), 0f, 100f);
			return;
		}
		this.DiplomaticScoreContainer.Visible = false;
	}

	public override void Unbind()
	{
		this.DestroyAmbassadorInteractionPanels();
		this.OverrolledEmpire = null;
		IEventService service = Services.GetService<IEventService>();
		if (service != null)
		{
			service.EventRaise -= this.EventService_EventRaise;
		}
		this.DepartmentOfForeignAffairs = null;
		base.Unbind();
	}

	public void OnAmbassadorInteractionPanelClosed()
	{
		this.OverrolledEmpire = null;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		base.AgeTransform.Enable = true;
		this.interactionsAllowed = base.PlayerController.CanSendOrders();
		base.GuiService.GetGuiPanel<ControlBanner>().OnShowScreen(GameScreenType.Diplomacy);
		base.GuiService.Show(typeof(EmpireBannerPanel), new object[]
		{
			EmpireBannerPanel.Full
		});
		if (this.diplomaticRelationsViewport != null)
		{
			AgeModifierAlpha ageModifierAlpha = base.GetComponent<AgeModifierAlpha>();
			float duration = (!(ageModifierAlpha != null)) ? 0f : ageModifierAlpha.Duration;
			yield return this.diplomaticRelationsViewport.OnShow(duration);
			if (base.Empire != null)
			{
				this.diplomaticRelationsViewport.SetInspectedEmpire(base.Empire);
				this.RebindAmbassadorPanels();
			}
		}
		base.NeedRefresh = true;
		base.StartCoroutine(this.UpdateOverrolledEmpire());
		yield break;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		base.NeedRefresh = false;
		base.GuiService.GetGuiPanel<ControlBanner>().OnHideScreen(GameScreenType.Diplomacy);
		this.OverrolledEmpire = null;
		for (int i = 0; i < this.ambassadorInteractionPanels.Length; i++)
		{
			if (this.ambassadorInteractionPanels[i] != null)
			{
				this.ambassadorInteractionPanels[i].Hide(true);
			}
		}
		if (this.diplomaticRelationsViewport != null)
		{
			AgeModifierAlpha ageModifierAlpha = base.GetComponent<AgeModifierAlpha>();
			float duration = (!(ageModifierAlpha != null)) ? 0f : ageModifierAlpha.Duration;
			yield return this.diplomaticRelationsViewport.OnHide(instant, duration);
		}
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		if (this.diplomaticRelationsViewport != null)
		{
			yield return this.diplomaticRelationsViewport.OnLoadGame();
		}
		this.listOfDiplomaticRelationStates = new List<DiplomaticRelationState>();
		this.listOfDiplomaticRelationStates.Add(this.DepartmentOfForeignAffairs.GetDiplomaticRelationStateFromName(DiplomaticRelationState.Names.Alliance));
		this.listOfDiplomaticRelationStates.Add(this.DepartmentOfForeignAffairs.GetDiplomaticRelationStateFromName(DiplomaticRelationState.Names.Peace));
		this.listOfDiplomaticRelationStates.Add(this.DepartmentOfForeignAffairs.GetDiplomaticRelationStateFromName(DiplomaticRelationState.Names.ColdWar));
		this.listOfDiplomaticRelationStates.Add(this.DepartmentOfForeignAffairs.GetDiplomaticRelationStateFromName(DiplomaticRelationState.Names.War));
		this.listOfDiplomaticRelationStates.Add(this.DepartmentOfForeignAffairs.GetDiplomaticRelationStateFromName(DiplomaticRelationState.Names.Unknown));
		this.RelationStatesContainer.ReserveChildren(this.listOfDiplomaticRelationStates.Count, this.RelationStatePrefab, "RelationState");
		this.RelationStatesContainer.RefreshChildrenIList<DiplomaticRelationState>(this.listOfDiplomaticRelationStates, new AgeTransform.RefreshTableItem<DiplomaticRelationState>(this.RefreshRelationState), true, false);
		this.EndTurnService = Services.GetService<IEndTurnService>();
		yield break;
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.Hide(true);
		this.EndTurnService = null;
		if (this.diplomaticRelationsViewport != null)
		{
			this.diplomaticRelationsViewport.OnUnloadGame(game);
		}
		if (this.listOfDiplomaticRelationStates != null)
		{
			this.listOfDiplomaticRelationStates.Clear();
			this.listOfDiplomaticRelationStates = null;
		}
		base.OnUnloadGame(game);
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		base.UseRefreshLoop = true;
		this.GlobalScreenButton.AgeTransform.Visible = false;
		GameObject prefab = (GameObject)Resources.Load("Prefabs/Diplomacy/DiplomaticRelationsViewport");
		if (prefab != null)
		{
			GameObject instance = UnityEngine.Object.Instantiate<GameObject>(prefab);
			if (instance != null)
			{
				instance.transform.parent = base.transform;
				this.diplomaticRelationsViewport = instance.GetComponent<DiplomaticRelationsViewport>();
				this.GlobalScreenButton.AgeTransform.Visible = true;
			}
		}
		if (this.diplomaticRelationsViewport != null)
		{
			yield return this.diplomaticRelationsViewport.OnLoad();
		}
		yield break;
	}

	protected override void OnUnload()
	{
		UnityEngine.Object.DestroyImmediate(this.diplomaticRelationsViewport.gameObject);
		this.diplomaticRelationsViewport = null;
		base.OnUnload();
	}

	protected override bool ShouldCaptureBackground()
	{
		return this.diplomaticRelationsViewport != null || base.ShouldCaptureBackground();
	}

	protected override void OnStartCaptureBackground()
	{
		if (this.diplomaticRelationsViewport != null)
		{
			this.diplomaticRelationsViewport.SetBackDropVisibility(false, null);
			if (this.Backdrop != null)
			{
				this.Backdrop.AgeTransform.Visible = false;
			}
		}
		else
		{
			base.OnStartCaptureBackground();
		}
	}

	protected override void OnEndCaptureBackground(Texture2D backgroundTexture)
	{
		if (this.diplomaticRelationsViewport != null && backgroundTexture != null)
		{
			this.diplomaticRelationsViewport.SetBackDropVisibility(true, backgroundTexture);
		}
		else
		{
			base.OnEndCaptureBackground(backgroundTexture);
		}
	}

	private void EventService_EventRaise(object sender, EventRaiseEventArgs e)
	{
		EventDiplomaticRelationStateChange eventDiplomaticRelationStateChange = e.RaisedEvent as EventDiplomaticRelationStateChange;
		EventDiplomaticContractStateChange eventDiplomaticContractStateChange = e.RaisedEvent as EventDiplomaticContractStateChange;
		if (eventDiplomaticRelationStateChange != null)
		{
			if (base.Empire == eventDiplomaticRelationStateChange.Empire || base.Empire == eventDiplomaticRelationStateChange.EmpireWithWhichTheStatusChange)
			{
				base.NeedRefresh = true;
				return;
			}
		}
		else if (eventDiplomaticContractStateChange != null && eventDiplomaticContractStateChange.DiplomaticContract != null && eventDiplomaticContractStateChange.DiplomaticContract.State == DiplomaticContractState.Signed && (eventDiplomaticContractStateChange.DiplomaticContract.EmpireWhichReceives.Index == base.Empire.Index || eventDiplomaticContractStateChange.DiplomaticContract.EmpireWhichProposes.Index == base.Empire.Index))
		{
			base.NeedRefresh = true;
		}
	}

	private void EndTurnService_GameClientStateChange(object sender, GameClientStateChangeEventArgs e)
	{
		if (base.IsVisible && base.PlayerController != null)
		{
			bool flag = base.PlayerController.CanSendOrders();
			if (this.interactionsAllowed != flag)
			{
				this.interactionsAllowed = flag;
				if (!this.interactionsAllowed && MessagePanel.Instance.IsVisible)
				{
					MessagePanel.Instance.Hide(false);
				}
			}
		}
	}

	private void CreateAmbassadorInteractionPanels()
	{
		if (this.ambassadorInteractionPanels != null)
		{
			this.DestroyAmbassadorInteractionPanels();
		}
		global::Game game = base.Game as global::Game;
		int num = (from empire in game.Empires
		where empire is MajorEmpire
		select empire).Count<global::Empire>();
		this.ambassadorInteractionPanels = new AmbassadorInteractionPanel[num];
		for (int i = 0; i < num; i++)
		{
			Transform transform = UnityEngine.Object.Instantiate<Transform>(this.AmbassadorInteractionPanelPrefab);
			if (transform != null)
			{
				transform.parent = base.transform;
				AmbassadorInteractionPanel component = transform.GetComponent<AmbassadorInteractionPanel>();
				component.Bind(this.LookingEmpire, game.Empires[i], base.Empire, this);
				component.Load();
				component.RefreshContent();
				component.Hide(true);
				this.ambassadorInteractionPanels[i] = component;
			}
		}
	}

	private void DestroyAmbassadorInteractionPanels()
	{
		if (this.ambassadorInteractionPanels == null)
		{
			return;
		}
		for (int i = 0; i < this.ambassadorInteractionPanels.Length; i++)
		{
			if (this.ambassadorInteractionPanels[i] != null)
			{
				this.ambassadorInteractionPanels[i].Hide(true);
				this.ambassadorInteractionPanels[i].Unbind();
				this.ambassadorInteractionPanels[i].Unload();
				UnityEngine.Object.Destroy(this.ambassadorInteractionPanels[i]);
			}
		}
		this.ambassadorInteractionPanels = null;
	}

	private void RebindAmbassadorPanels()
	{
		for (int i = 0; i < this.ambassadorInteractionPanels.Length; i++)
		{
			global::Empire ambassadorEmpire = this.ambassadorInteractionPanels[i].AmbassadorEmpire;
			this.ambassadorInteractionPanels[i].Unbind();
			this.ambassadorInteractionPanels[i].Bind(this.LookingEmpire, ambassadorEmpire, base.Empire, this);
			this.ambassadorInteractionPanels[i].RefreshContent();
			this.ambassadorInteractionPanels[i].Hide(true);
		}
	}

	private void RefreshRelationState(AgeTransform slot, DiplomaticRelationState diplomaticRelationState, int index)
	{
		global::Game x = base.Game as global::Game;
		Diagnostics.Assert(x != null);
		if (this.diplomaticRelationsViewport != null)
		{
			string diplomaticRelationName = diplomaticRelationState.Name;
			Bounds iconBound = this.diplomaticRelationsViewport.GetIconBound(diplomaticRelationName);
			slot.GetComponent<RelationStateItem>().RefreshContentWith3DView(diplomaticRelationState, iconBound);
		}
	}

	private void OnCloseCB(GameObject obj)
	{
		this.HandleCancelRequest();
	}

	private void OnScreenClickCB(GameObject obj)
	{
		if (this.diplomaticRelationsViewport == null)
		{
			return;
		}
		if (!this.interactionsAllowed)
		{
			return;
		}
		if (base.Empire.SimulationObject.Tags.Contains(global::Empire.TagEmpireEliminated))
		{
			return;
		}
		global::Empire empire = null;
		global::Empire empire2 = null;
		this.diplomaticRelationsViewport.GetCurrentHighlightedEmpire(ref empire, ref empire2);
		if (empire != null && empire != base.Empire)
		{
			Services.GetService<IAudioEventService>().Play2DEvent("Gui/Interface/InGame/OpenPanelButton");
			this.OnNegotiation(empire);
			return;
		}
		if (empire2 != null && empire2 != base.Empire)
		{
			Services.GetService<IAudioEventService>().Play2DEvent("Gui/Interface/InGame/OpenPanelButton");
			this.OnNegotiation(empire2);
			return;
		}
		Services.GetService<IAudioEventService>().Play2DEvent("Gui/Interface/InGame/OpenPanelButton");
		this.OnInspect(base.Empire);
	}

	private void OnScreenMiddleClickCB(GameObject obj)
	{
		if (this.diplomaticRelationsViewport == null)
		{
			return;
		}
		global::Empire empire = null;
		global::Empire empire2 = null;
		this.diplomaticRelationsViewport.GetCurrentHighlightedEmpire(ref empire, ref empire2);
		if (empire != null)
		{
			this.OnInspect(empire);
		}
	}

	private void OnInspect(global::Empire empire)
	{
		this.diplomaticRelationsViewport.SetInspectedEmpire(empire);
		this.RebindAmbassadorPanels();
	}

	private void OnNegotiation(global::Empire otherEmpire)
	{
		base.GuiService.Hide(typeof(GameDiplomacyScreen));
		base.GuiService.GetGuiPanel<GameNegotiationScreen>().Show(new object[]
		{
			otherEmpire
		});
	}

	private void OnCurrentTreaties(global::Empire otherEmpire)
	{
		Diagnostics.Log("OnCurrentTreaties");
	}

	private IEnumerator UpdateOverrolledEmpire()
	{
		while (base.IsVisible && !base.IsHiding)
		{
			global::Empire ambassadorEmpire = null;
			global::Empire inspectedEmpire = null;
			if (this.interactionsAllowed)
			{
				this.diplomaticRelationsViewport.GetCurrentHighlightedEmpire(ref ambassadorEmpire, ref inspectedEmpire);
				if (this.OverrolledEmpire != ambassadorEmpire)
				{
					this.OverrolledEmpire = ambassadorEmpire;
				}
			}
			else
			{
				this.OverrolledEmpire = null;
			}
			yield return null;
		}
		yield break;
	}

	public Transform AmbassadorInteractionPanelPrefab;

	public Transform RelationStatePrefab;

	public AgeControlButton GlobalScreenButton;

	public AgeTooltip GlobalScreenTooltip;

	public AgeTransform RelationStatesContainer;

	public AgeTransform DiplomaticScoreContainer;

	public AgePrimitiveLabel DiplomaticScore;

	public AgeTransform DiplomaticGauge;

	private AmbassadorInteractionPanel[] ambassadorInteractionPanels;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private List<DiplomaticRelationState> listOfDiplomaticRelationStates;

	private global::Empire overrolledEmpire;

	private IEndTurnService endTurnService;

	private bool interactionsAllowed = true;

	private DiplomaticRelationsViewport diplomaticRelationsViewport;

	private IVictoryManagementService victoryService;
}
