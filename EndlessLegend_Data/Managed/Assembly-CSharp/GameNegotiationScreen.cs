using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using UnityEngine;

public class GameNegotiationScreen : GuiPlayerControllerScreen
{
	public GameNegotiationScreen()
	{
		base.EnableCaptureBackgroundOnShow = true;
		base.EnableMenuAudioLayer = true;
	}

	public DiplomaticContract DiplomaticContract { get; private set; }

	public global::Empire SelectedEmpire
	{
		get
		{
			return this.selectedEmpire;
		}
		private set
		{
			if (this.selectedEmpire != null)
			{
				this.DiplomaticRelationshipDisk.Unbind();
				this.ContractPanel.Unbind();
				this.TheirTermOptionsPanel.Unbind();
				this.MyTermOptionsPanel.Unbind();
				this.DiplomaticContract = null;
				this.diplomaticTermsAndEvalutations.Clear();
				this.PlayerDiplomaticRelationWithOther = null;
			}
			this.selectedEmpire = value;
			if (this.selectedEmpire != null)
			{
				this.PlayerDiplomaticRelationWithOther = this.DepartmentOfForeignAffairs.GetDiplomaticRelation(this.selectedEmpire);
				this.DiplomaticRelationshipDisk.Bind(base.Empire, this.selectedEmpire);
				this.GetOrCreateContract();
				this.DealApprovalGroup.Visible = this.SelectedEmpire.IsControlledByAI;
				if (this.diplomaticNegotiationViewport != null && this.selectedEmpire != null)
				{
					this.diplomaticNegotiationViewport.SetApparence(this.selectedEmpire.Faction.AffinityMapping, this.selectedEmpire.Color);
					this.diplomaticNegotiationViewport.TriggerAlternativeIdle(0.1f);
				}
			}
		}
	}

	private DiplomaticRelation PlayerDiplomaticRelationWithOther { get; set; }

	private DepartmentOfForeignAffairs DepartmentOfForeignAffairs { get; set; }

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

	public static List<GuiElement> GetFilteredDiplomaticAbilityGuiElements(global::Empire thisEmpire, global::Empire otherEmpire)
	{
		IGuiPanelHelper guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
		Diagnostics.Assert(guiPanelHelper != null, "Unable to access GuiPanelHelper");
		DepartmentOfForeignAffairs agency = thisEmpire.GetAgency<DepartmentOfForeignAffairs>();
		DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(otherEmpire);
		List<GuiElement> list = new List<GuiElement>();
		bool flag = false;
		bool flag2 = false;
		ReadOnlyCollection<DiplomaticAbility> diplomaticAbilities = diplomaticRelation.DiplomaticAbilities;
		GuiElement item;
		for (int i = 0; i < diplomaticAbilities.Count; i++)
		{
			StaticString name = diplomaticAbilities[i].Name;
			if (name == DiplomaticAbilityDefinition.CloseBorders && !thisEmpire.SimulationObject.Tags.Contains("SeasonEffectDiplomacy1"))
			{
				flag = true;
			}
			else if (diplomaticAbilities[i].IsActive)
			{
				IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
				if (!(name == DiplomaticAbilityDefinition.ImmuneToDefensiveImprovements) || service.IsShared(DownloadableContent13.ReadOnlyName))
				{
					if (guiPanelHelper.TryGetGuiElement("DiplomaticAbility" + name, out item))
					{
						list.Add(item);
					}
				}
			}
		}
		DepartmentOfForeignAffairs agency2 = otherEmpire.GetAgency<DepartmentOfForeignAffairs>();
		DiplomaticRelation diplomaticRelation2 = agency2.GetDiplomaticRelation(thisEmpire);
		if (diplomaticRelation2.HasActiveAbility(DiplomaticAbilityDefinition.CloseBorders) && !otherEmpire.SimulationObject.Tags.Contains("SeasonEffectDiplomacy1"))
		{
			flag2 = true;
		}
		if (flag && flag2)
		{
			if (guiPanelHelper.TryGetGuiElement("DiplomaticAbilityMutualCloseBorders", out item))
			{
				list.Add(item);
			}
		}
		else if (flag)
		{
			if (guiPanelHelper.TryGetGuiElement("DiplomaticAbilityPersonaNonGrata", out item))
			{
				list.Add(item);
			}
		}
		else if (flag2 && guiPanelHelper.TryGetGuiElement("DiplomaticAbilityLockedOut", out item))
		{
			list.Add(item);
		}
		if (diplomaticRelation2.HasActiveAbility(DiplomaticAbilityDefinition.MarketBan) && guiPanelHelper.TryGetGuiElement("DiplomaticAbilityMarketBanner", out item))
		{
			list.Add(item);
		}
		return list;
	}

	public override void Bind(global::Empire empire)
	{
		base.Bind(empire);
		this.DepartmentOfForeignAffairs = base.Empire.GetAgency<DepartmentOfForeignAffairs>();
		this.DepartmentOfTheTreasury = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		this.YouLabel.Text = AgeLocalizer.Instance.LocalizeString("%NegotiationYouTitle") + " - " + base.Empire.Faction.LocalizedName;
		this.RefreshOtherEmpiresDropList();
	}

	public override void Unbind()
	{
		this.SelectedEmpire = null;
		this.otherEmpires = null;
		this.TheirEmpireNameDropList.ItemTable = null;
		this.TheirEmpireNameDropList.TooltipTable = null;
		this.YouLabel.Text = string.Empty;
		this.YouLabel.TintColor = Color.white;
		this.DepartmentOfTheTreasury = null;
		this.DepartmentOfForeignAffairs = null;
		base.Unbind();
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		if (base.Empire == null)
		{
			return;
		}
		if (this.SelectedEmpire == null)
		{
			return;
		}
		this.DiplomaticRelationshipDisk.RefreshContent();
		this.TheirEmpirePointsValueLabel.Text = this.FormatEmpirePoints(this.SelectedEmpire, false);
		DepartmentOfForeignAffairs agency = this.SelectedEmpire.GetAgency<DepartmentOfForeignAffairs>();
		this.MarketplaceBannedIcon.AgeTransform.Visible = agency.IsBannedFromMarket();
		this.BlackSpotVictimIcon.AgeTransform.Visible = agency.IsBlackSpotVictim();
		this.RefreshDiplomaticAbilities();
		this.MyTermOptionsPanel.RefreshContent();
		this.TheirTermOptionsPanel.RefreshContent();
		this.ContractPanel.RefreshContent();
		bool flag = this.PlayerDiplomaticRelationWithOther.State.Name != DiplomaticRelationState.Names.Dead;
		this.MyTermOptionsPanel.AgeTransform.Enable = (flag && this.interactionsAllowed);
		this.TheirTermOptionsPanel.AgeTransform.Enable = (flag && this.interactionsAllowed);
		this.ContractPanel.AgeTransform.Enable = (flag && this.interactionsAllowed);
		this.RefreshButtons();
		if (flag && this.DiplomaticContract != null)
		{
			Diagnostics.Assert(this.DiplomaticContract.EmpireWhichReceives.Index == this.SelectedEmpire.Index, "The current selected empire  ({0}) does not match the current receiver empire ({1}).", new object[]
			{
				this.DiplomaticContract.EmpireWhichReceives,
				this.SelectedEmpire
			});
			bool flag2 = this.DiplomaticContract.Terms.Count > 0 && this.DiplomaticContract.GetPropositionMethod() == DiplomaticTerm.PropositionMethod.Negotiation;
			this.DealApprovalGroup.Visible = true;
			this.DealApprovalGroup.Enable = flag2;
			if (this.SelectedEmpire.IsControlledByAI)
			{
				this.DealApprovalGauge.AgeTransform.Visible = flag2;
			}
			else
			{
				this.DealApprovalGauge.AgeTransform.Visible = false;
			}
			this.RefreshApprovalSlider();
			this.OfferTitleLabel.Text = "%NegotiationMakeAnOfferTitle";
			if (this.DiplomaticContract.Terms.Count > 0 && this.DiplomaticContract.GetPropositionMethod() == DiplomaticTerm.PropositionMethod.Declaration)
			{
				this.OfferTitleLabel.Text = "%NegotiationDeclareTitle";
			}
		}
		else
		{
			this.DealApprovalGroup.Visible = false;
		}
	}

	public override bool HandleCancelRequest()
	{
		base.GuiService.Hide(typeof(GameNegotiationScreen));
		base.GuiService.GetGuiPanel<GameDiplomacyScreen>().Show(new object[0]);
		return true;
	}

	public override bool HandleNextRequest()
	{
		if (this.interactionsAllowed)
		{
			this.SelectEmpireDropListIndex(this.TheirEmpireNameDropList.SelectedItem + 1);
		}
		return true;
	}

	public override bool HandlePreviousRequest()
	{
		if (this.interactionsAllowed)
		{
			this.SelectEmpireDropListIndex(this.TheirEmpireNameDropList.SelectedItem - 1);
		}
		return true;
	}

	public void ReShow(global::Empire empireToSelect)
	{
		this.SelectEmpire(empireToSelect);
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		base.GuiService.Show(typeof(EmpireBannerPanel), new object[]
		{
			EmpireBannerPanel.Full
		});
		this.interactionsAllowed = base.PlayerController.CanSendOrders();
		this.RefreshOtherEmpiresDropList();
		if (parameters.Length > 0)
		{
			global::Empire empireToSelect = parameters[0] as global::Empire;
			this.SelectEmpire(empireToSelect);
		}
		if (this.SelectedEmpire == null)
		{
			Diagnostics.LogError("Trying to show the GameNegotiationScreen without other empire, which is not possible");
			this.HandleCancelRequest();
		}
		if (this.diplomaticNegotiationViewport != null)
		{
			AgeModifierAlpha ageModifierAlpha = base.GetComponent<AgeModifierAlpha>();
			float duration = (!(ageModifierAlpha != null)) ? 0f : ageModifierAlpha.Duration;
			this.diplomaticNegotiationViewport.OnShow(duration);
		}
		this.MyTermOptionsPanel.Show(new object[]
		{
			base.gameObject
		});
		this.TheirTermOptionsPanel.Show(new object[]
		{
			base.gameObject
		});
		this.ContractPanel.Show(new object[]
		{
			base.gameObject
		});
		yield break;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		base.NeedRefresh = false;
		this.MyTermOptionsPanel.Hide(instant);
		this.TheirTermOptionsPanel.Hide(instant);
		this.ContractPanel.Hide(instant);
		this.SelectedEmpire = null;
		if (this.diplomaticNegotiationViewport != null)
		{
			AgeModifierAlpha ageModifierAlpha = base.GetComponent<AgeModifierAlpha>();
			float duration = (!(ageModifierAlpha != null)) ? 0f : ageModifierAlpha.Duration;
			this.diplomaticNegotiationViewport.OnHide((!instant) ? duration : 0f);
		}
		yield return base.OnHide(instant);
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
		this.EndTurnService = Services.GetService<IEndTurnService>();
		yield break;
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.Hide(true);
		IEventService service = Services.GetService<IEventService>();
		if (service != null)
		{
			service.EventRaise -= this.EventService_EventRaise;
		}
		this.EndTurnService = null;
		base.OnUnloadGame(game);
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		base.UseRefreshLoop = true;
		GameObject prefab = (GameObject)Resources.Load(DiplomaticNegotiationViewport.DefaultPrefabName);
		if (prefab != null)
		{
			GameObject instance = UnityEngine.Object.Instantiate<GameObject>(prefab);
			if (instance != null)
			{
				instance.transform.parent = base.transform;
				this.diplomaticNegotiationViewport = instance.GetComponent<DiplomaticNegotiationViewport>();
				yield return this.diplomaticNegotiationViewport.OnLoad(this.ViewportLayer);
				if (this.SelectedEmpire != null)
				{
					this.diplomaticNegotiationViewport.SetApparence(this.SelectedEmpire.Faction.AffinityMapping, this.SelectedEmpire.Color);
				}
			}
		}
		this.setupDiplomaticAbilityDelegate = new AgeTransform.RefreshTableItem<GuiElement>(this.SetupDiplomaticAbility);
		this.MyTermOptionsPanel.Load();
		this.TheirTermOptionsPanel.Load();
		this.ContractPanel.Load();
		this.ContractPanel.AdjustHeightToContent = false;
		Color positiveColor = Color.white;
		Color negativeColor = Color.white;
		GuiElement guiElement;
		if (base.GuiService.GuiPanelHelper.TryGetGuiElement("DiplomaticContractEvaluationPositive", out guiElement))
		{
			ExtendedGuiElement extendedGuiElement = guiElement as ExtendedGuiElement;
			if (extendedGuiElement != null)
			{
				positiveColor = extendedGuiElement.Color;
			}
		}
		if (base.GuiService.GuiPanelHelper.TryGetGuiElement("DiplomaticContractEvaluationNegative", out guiElement))
		{
			ExtendedGuiElement extendedGuiElement2 = guiElement as ExtendedGuiElement;
			if (extendedGuiElement2 != null)
			{
				negativeColor = extendedGuiElement2.Color;
			}
		}
		this.DealApprovalGauge.Init(-4f, 4f, positiveColor, negativeColor);
		yield break;
	}

	protected override void OnUnload()
	{
		this.MyTermOptionsPanel.Unload();
		this.TheirTermOptionsPanel.Unload();
		this.ContractPanel.Load();
		this.setupDiplomaticAbilityDelegate = null;
		if (this.diplomaticNegotiationViewport != null)
		{
			this.diplomaticNegotiationViewport.OnUnload();
		}
		UnityEngine.Object.DestroyImmediate(this.diplomaticNegotiationViewport.gameObject);
		this.diplomaticNegotiationViewport = null;
		base.OnUnload();
	}

	private float ComputeContractCost(global::Empire referenceEmpire)
	{
		return DepartmentOfForeignAffairs.GetEmpirePointCost(this.DiplomaticContract, referenceEmpire);
	}

	private void DepartmentOfTheTreasury_ResourcePropertyChange(object sender, ResourcePropertyChangeEventArgs e)
	{
		if (base.IsVisible)
		{
			base.NeedRefresh = true;
		}
	}

	private void EventService_EventRaise(object sender, EventRaiseEventArgs e)
	{
		if (!base.IsVisible)
		{
			return;
		}
		EventDiplomaticContractStateChange eventDiplomaticContractStateChange = e.RaisedEvent as EventDiplomaticContractStateChange;
		if (eventDiplomaticContractStateChange != null && (eventDiplomaticContractStateChange.DiplomaticContract.EmpireWhichProposes.Index == base.Empire.Index || eventDiplomaticContractStateChange.DiplomaticContract.EmpireWhichReceives.Index == base.Empire.Index))
		{
			base.NeedRefresh = true;
			return;
		}
		EventEmpireEliminated eventEmpireEliminated = e.RaisedEvent as EventEmpireEliminated;
		if (eventEmpireEliminated != null && this.SelectedEmpire != null && this.SelectedEmpire.Index == eventEmpireEliminated.EliminatedEmpire.Index)
		{
			this.HandleCancelRequest();
		}
	}

	private void EndTurnService_GameClientStateChange(object sender, GameClientStateChangeEventArgs e)
	{
		if (base.IsVisible && base.PlayerController != null && this.SelectedEmpire != null)
		{
			bool flag = base.PlayerController.CanSendOrders();
			if (this.interactionsAllowed != flag)
			{
				this.interactionsAllowed = flag;
				this.RefreshButtons();
				bool flag2 = this.PlayerDiplomaticRelationWithOther.State.Name != DiplomaticRelationState.Names.Dead;
				this.MyTermOptionsPanel.AgeTransform.Enable = (flag2 && this.interactionsAllowed);
				this.TheirTermOptionsPanel.AgeTransform.Enable = (flag2 && this.interactionsAllowed);
				this.ContractPanel.AgeTransform.Enable = (flag2 && this.interactionsAllowed);
				if (!this.interactionsAllowed && MessagePanel.Instance.IsVisible)
				{
					MessagePanel.Instance.Hide(false);
				}
			}
		}
	}

	private void DiplomaticContractCreationComplete(object sender, TicketRaisedEventArgs ticketRaisedEventArgs)
	{
		IDiplomacyService service = base.Game.Services.GetService<IDiplomacyService>();
		DiplomaticContract diplomaticContract;
		if (!service.TryGetActiveDiplomaticContract(base.Empire, this.selectedEmpire, out diplomaticContract))
		{
			Diagnostics.LogError("Failed to create a valid active contract.");
			this.HandleCancelRequest();
			return;
		}
		this.OnDiplomaticContractChanged(diplomaticContract);
	}

	private string FormatEmpirePoints(global::Empire empire, bool withNet = false)
	{
		DepartmentOfTheTreasury agency = empire.GetAgency<DepartmentOfTheTreasury>();
		float stockValue;
		if (!agency.TryGetResourceStockValue(empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, out stockValue, false))
		{
			Diagnostics.LogError("Can't get resource stock value {0} on simulation object {1}.", new object[]
			{
				DepartmentOfTheTreasury.Resources.EmpirePoint,
				empire.SimulationObject
			});
		}
		if (!withNet)
		{
			return GuiFormater.FormatStock(stockValue, SimulationProperties.EmpirePoint, 1, false);
		}
		float netValue;
		if (!agency.TryGetNetResourceValue(empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, out netValue, false))
		{
			Diagnostics.LogError("Can't get resource net value {0} on simulation object {1}.", new object[]
			{
				DepartmentOfTheTreasury.Resources.EmpirePoint,
				empire.SimulationObject
			});
		}
		return GuiFormater.FormatStockAndNet(stockValue, netValue, SimulationProperties.EmpirePoint, false);
	}

	private bool CanContractBeValidated()
	{
		if (this.DiplomaticContract.Terms.Count == 0)
		{
			return false;
		}
		float cost = this.ComputeContractCost(this.DiplomaticContract.EmpireWhichProposes);
		if (!this.DepartmentOfTheTreasury.CanAfford(cost, SimulationProperties.EmpirePoint))
		{
			return false;
		}
		DiplomaticTerm.PropositionMethod propositionMethod = this.DiplomaticContract.GetPropositionMethod();
		return (propositionMethod == DiplomaticTerm.PropositionMethod.Negotiation && this.DiplomaticContract.IsTransitionPossible(DiplomaticContractState.Proposed)) || (propositionMethod == DiplomaticTerm.PropositionMethod.Declaration && this.DiplomaticContract.IsTransitionPossible(DiplomaticContractState.Signed));
	}

	private void OnCloseCB()
	{
		this.HandleCancelRequest();
	}

	private void OnEmpireInformationCB()
	{
		base.GuiService.GetGuiPanel<EmpireInformationModalPanel>().Show(new object[]
		{
			this.SelectedEmpire
		});
	}

	private void OnOfferCB(GameObject obj)
	{
		if (this.DiplomaticContract == null)
		{
			Diagnostics.LogWarning("Diplomatic contract is null");
			return;
		}
		Diagnostics.Assert(this.DiplomaticContract.Terms != null);
		if (this.DiplomaticContract.Terms.Count == 0)
		{
			return;
		}
		OrderChangeDiplomaticContractState order = new OrderChangeDiplomaticContractState(this.DiplomaticContract, DiplomaticContractState.Proposed);
		base.PlayerController.PostOrder(order);
		this.DiplomaticContract = null;
		this.HandleCancelRequest();
	}

	private void OnResetCB(GameObject obj)
	{
		if (this.DiplomaticContract == null)
		{
			Diagnostics.LogWarning("Diplomatic contract is null");
			return;
		}
		Diagnostics.Assert(this.DiplomaticContract.Terms != null);
		if (this.DiplomaticContract.Terms.Count == 0)
		{
			return;
		}
		DiplomaticTermChange[] array = new DiplomaticTermChange[this.DiplomaticContract.Terms.Count];
		for (int i = 0; i < this.DiplomaticContract.Terms.Count; i++)
		{
			DiplomaticTerm diplomaticTerm = this.DiplomaticContract.Terms[i];
			Diagnostics.Assert(diplomaticTerm != null);
			array[i] = DiplomaticTermChange.Remove(diplomaticTerm.Index);
		}
		OrderChangeDiplomaticContractTermsCollection order = new OrderChangeDiplomaticContractTermsCollection(this.DiplomaticContract, array);
		Ticket ticket;
		base.PlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderChangeDiplomaticContractTermsCollectionResponse));
	}

	private void OnChooseOtherEmpireCB(GameObject obj)
	{
		this.SelectEmpireDropListIndex(this.TheirEmpireNameDropList.SelectedItem);
	}

	private void OnClickTermOptionLine(TermOptionLine termOptionLine)
	{
		if (this.DiplomaticContract == null)
		{
			Diagnostics.LogWarning("Diplomatic contract is null");
			return;
		}
		DiplomaticTermChange[] diplomaticTermChanges = new DiplomaticTermChange[]
		{
			DiplomaticTermChange.Add(termOptionLine.GuiDiplomaticTerm.Term)
		};
		OrderChangeDiplomaticContractTermsCollection order = new OrderChangeDiplomaticContractTermsCollection(this.DiplomaticContract, diplomaticTermChanges);
		Ticket ticket;
		base.PlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderChangeDiplomaticContractTermsCollectionResponse));
		this.OnLeaveTermOptionLine(termOptionLine);
	}

	private void OnClickContractTermLine(ContractTermLine contractTermLine)
	{
		if (this.DiplomaticContract == null)
		{
			Diagnostics.LogWarning("Diplomatic contract is null");
			return;
		}
		DiplomaticTermChange[] diplomaticTermChanges = new DiplomaticTermChange[]
		{
			DiplomaticTermChange.Remove(contractTermLine.ContractTerm.Index)
		};
		OrderChangeDiplomaticContractTermsCollection order = new OrderChangeDiplomaticContractTermsCollection(this.DiplomaticContract, diplomaticTermChanges);
		Ticket ticket;
		base.PlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderChangeDiplomaticContractTermsCollectionResponse));
	}

	private void OnChangeContractTermLineQuantity(ContractTermLine contractTermLine)
	{
		if (this.DiplomaticContract == null)
		{
			Diagnostics.LogWarning("Diplomatic contract is null");
			return;
		}
		if (contractTermLine.ContractTerm is DiplomaticTermResourceExchange)
		{
			DiplomaticTermResourceExchange diplomaticTermResourceExchange = contractTermLine.ContractTerm as DiplomaticTermResourceExchange;
			DiplomaticTermResourceExchange term = new DiplomaticTermResourceExchange((DiplomaticTermResourceExchangeDefinition)diplomaticTermResourceExchange.Definition, diplomaticTermResourceExchange.EmpireWhichProposes, diplomaticTermResourceExchange.EmpireWhichProvides, diplomaticTermResourceExchange.EmpireWhichReceives, diplomaticTermResourceExchange.ResourceName, contractTermLine.CurrentQuantity);
			DiplomaticTermChange[] diplomaticTermChanges = new DiplomaticTermChange[]
			{
				DiplomaticTermChange.Refresh(term, contractTermLine.ContractTerm.Index)
			};
			OrderChangeDiplomaticContractTermsCollection order = new OrderChangeDiplomaticContractTermsCollection(this.DiplomaticContract, diplomaticTermChanges);
			Ticket ticket;
			base.PlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderChangeDiplomaticContractTermsCollectionResponse));
		}
		else
		{
			if (!(contractTermLine.ContractTerm is DiplomaticTermBoosterExchange))
			{
				Diagnostics.LogError("Trying to change quantity on a term that doesn't have any quantity.");
				return;
			}
			DiplomaticTermBoosterExchange termBoosterExchange = contractTermLine.ContractTerm as DiplomaticTermBoosterExchange;
			DepartmentOfEducation agency = termBoosterExchange.EmpireWhichProvides.GetAgency<DepartmentOfEducation>();
			List<GameEntityGUID> list = (from match in agency
			where match.Constructible.Name == termBoosterExchange.BoosterDefinitionName
			select match into selectedBooster
			select selectedBooster.GUID).ToList<GameEntityGUID>();
			int num = (int)contractTermLine.CurrentQuantity;
			if (num < list.Count)
			{
				list.RemoveRange(num, list.Count - num);
				list.TrimExcess();
			}
			GameEntityGUID[] boosterGUIDs = list.ToArray();
			DiplomaticTermBoosterExchange term2 = new DiplomaticTermBoosterExchange((DiplomaticTermBoosterExchangeDefinition)termBoosterExchange.Definition, termBoosterExchange.EmpireWhichProposes, termBoosterExchange.EmpireWhichProvides, termBoosterExchange.EmpireWhichReceives, boosterGUIDs, termBoosterExchange.BoosterDefinitionName);
			DiplomaticTermChange[] diplomaticTermChanges2 = new DiplomaticTermChange[]
			{
				DiplomaticTermChange.Refresh(term2, contractTermLine.ContractTerm.Index)
			};
			OrderChangeDiplomaticContractTermsCollection order2 = new OrderChangeDiplomaticContractTermsCollection(this.DiplomaticContract, diplomaticTermChanges2);
			Ticket ticket2;
			base.PlayerController.PostOrder(order2, out ticket2, new EventHandler<TicketRaisedEventArgs>(this.OnOrderChangeDiplomaticContractTermsCollectionResponse));
		}
	}

	private void OnChangeContractTermLineThirdEmpire(ContractTermLine contractTermLine)
	{
		if (this.DiplomaticContract == null)
		{
			Diagnostics.LogWarning("Diplomatic contract is null");
			return;
		}
		if (contractTermLine.SelectedEmpire == null)
		{
			return;
		}
		DiplomaticTermProposal diplomaticTermProposal = contractTermLine.ContractTerm as DiplomaticTermProposal;
		if (diplomaticTermProposal == null)
		{
			return;
		}
		DiplomaticTermProposal diplomaticTermProposal2 = diplomaticTermProposal.Clone();
		diplomaticTermProposal2.ChangeEmpire(this.DiplomaticContract, contractTermLine.SelectedEmpire);
		DiplomaticTermChange[] diplomaticTermChanges = new DiplomaticTermChange[]
		{
			DiplomaticTermChange.Refresh(diplomaticTermProposal2, contractTermLine.ContractTerm.Index)
		};
		OrderChangeDiplomaticContractTermsCollection order = new OrderChangeDiplomaticContractTermsCollection(this.DiplomaticContract, diplomaticTermChanges);
		Ticket ticket;
		base.PlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderChangeDiplomaticContractTermsCollectionResponse));
	}

	private void OnOrderChangeDiplomaticContractTermsCollectionResponse(object sender, TicketRaisedEventArgs ticketRaisedEventArgs)
	{
		if (!(ticketRaisedEventArgs.Order is OrderChangeDiplomaticContractTermsCollection))
		{
			Diagnostics.LogError("Invalid ticket raised event args.");
			return;
		}
		base.NeedRefresh = true;
		if (ticketRaisedEventArgs.Result == PostOrderResponse.Processed && this.SelectedEmpire.IsControlledByAI)
		{
			this.PostOrderGetAIDiplomaticContractEvaluation();
		}
	}

	private void PostOrderGetAIDiplomaticContractEvaluation()
	{
		OrderGetAIDiplomaticContractEvaluation order = new OrderGetAIDiplomaticContractEvaluation(this.DiplomaticContract);
		Ticket ticket;
		base.PlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderGetAIDiplomaticContractEvaluationResponse));
		this.PostOrderGetAIDiplomaticTermsEvaluation();
	}

	private void OnOrderGetAIDiplomaticContractEvaluationResponse(object sender, TicketRaisedEventArgs ticketRaisedEventArgs)
	{
		OrderGetAIDiplomaticContractEvaluation orderGetAIDiplomaticContractEvaluation = ticketRaisedEventArgs.Order as OrderGetAIDiplomaticContractEvaluation;
		if (orderGetAIDiplomaticContractEvaluation == null)
		{
			Diagnostics.LogError("Invalid ticket raised event args.");
			return;
		}
		base.NeedRefresh = true;
		if (ticketRaisedEventArgs.Result == PostOrderResponse.Processed && orderGetAIDiplomaticContractEvaluation != null)
		{
			this.contractApproval = orderGetAIDiplomaticContractEvaluation.AIEvaluationScore;
		}
		this.RefreshApprovalSlider();
	}

	private void PostOrderGetAIDiplomaticTermsEvaluation()
	{
		MajorEmpire majorEmpire = this.selectedEmpire as MajorEmpire;
		MajorEmpire majorEmpire2 = base.Empire as MajorEmpire;
		if (majorEmpire == null || majorEmpire2 == null)
		{
			return;
		}
		List<DiplomaticTerm> list = new List<DiplomaticTerm>();
		DepartmentOfForeignAffairs.GetAllAvailableDiplomaticTerm(this.DiplomaticContract, base.Empire, this.SelectedEmpire, ref list);
		DepartmentOfForeignAffairs.GetAllAvailableDiplomaticTerm(this.DiplomaticContract, this.SelectedEmpire, base.Empire, ref list);
		OrderGetAIDiplomaticTermEvaluation order = new OrderGetAIDiplomaticTermEvaluation(majorEmpire, majorEmpire2, list.ToArray());
		Ticket ticket;
		base.PlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnPostOrderGetAIDiplomaticTermsEvaluationResponse));
	}

	private void OnPostOrderGetAIDiplomaticTermsEvaluationResponse(object sender, TicketRaisedEventArgs ticketRaisedEventArgs)
	{
		OrderGetAIDiplomaticTermEvaluation orderGetAIDiplomaticTermEvaluation = ticketRaisedEventArgs.Order as OrderGetAIDiplomaticTermEvaluation;
		if (orderGetAIDiplomaticTermEvaluation == null)
		{
			Diagnostics.LogError("Invalid ticket raised event args.");
			return;
		}
		if (ticketRaisedEventArgs.Result == PostOrderResponse.Processed)
		{
			Diagnostics.Assert(orderGetAIDiplomaticTermEvaluation.DiplomaticTerms.Length == orderGetAIDiplomaticTermEvaluation.DiplomaticTermEvaluations.Length);
			this.diplomaticTermsAndEvalutations.Clear();
			for (int i = 0; i < orderGetAIDiplomaticTermEvaluation.DiplomaticTerms.Length; i++)
			{
				this.diplomaticTermsAndEvalutations.Add(orderGetAIDiplomaticTermEvaluation.DiplomaticTerms[i], orderGetAIDiplomaticTermEvaluation.DiplomaticTermEvaluations[i]);
			}
		}
	}

	private void OnEnterTermOptionLine(TermOptionLine termOptionLine)
	{
		if (!this.SelectedEmpire.IsControlledByAI)
		{
			return;
		}
		if (this.DealApprovalGauge.AgeTransform.Visible)
		{
			DiplomaticTerm term = termOptionLine.GuiDiplomaticTerm.Term;
			if (term != null && this.diplomaticTermsAndEvalutations.ContainsKey(term))
			{
				this.hoveredTermApproval = this.diplomaticTermsAndEvalutations[term];
			}
		}
		this.RefreshApprovalSlider();
	}

	private void OnLeaveTermOptionLine(TermOptionLine termOptionLine)
	{
		if (!this.SelectedEmpire.IsControlledByAI)
		{
			return;
		}
		if (this.DealApprovalGauge.AgeTransform.Visible)
		{
			DiplomaticTerm term = termOptionLine.GuiDiplomaticTerm.Term;
			if (term != null && this.diplomaticTermsAndEvalutations.ContainsKey(term))
			{
				this.hoveredTermApproval = 0f;
			}
		}
		this.RefreshApprovalSlider();
	}

	private void OnDiplomaticContractChanged(DiplomaticContract diplomaticContract)
	{
		this.DiplomaticContract = diplomaticContract;
		this.MyTermOptionsPanel.Bind(this.DiplomaticContract, base.Empire, this.SelectedEmpire);
		this.TheirTermOptionsPanel.Bind(this.DiplomaticContract, this.SelectedEmpire, base.Empire);
		this.ContractPanel.Bind(this.DiplomaticContract, base.Empire, this.SelectedEmpire);
		base.NeedRefresh = true;
		if (this.selectedEmpire != null && this.selectedEmpire.IsControlledByAI)
		{
			this.PostOrderGetAIDiplomaticTermsEvaluation();
		}
	}

	private void GetOrCreateContract()
	{
		IDiplomacyService service = base.Game.Services.GetService<IDiplomacyService>();
		DiplomaticContract diplomaticContract;
		if (!service.TryGetActiveDiplomaticContract(base.Empire, this.selectedEmpire, out diplomaticContract))
		{
			OrderCreateDiplomaticContract order = new OrderCreateDiplomaticContract(base.Empire, this.selectedEmpire);
			Diagnostics.Assert(base.PlayerController != null);
			Ticket ticket;
			base.PlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.DiplomaticContractCreationComplete));
		}
		else
		{
			this.OnDiplomaticContractChanged(diplomaticContract);
			if (this.SelectedEmpire.IsControlledByAI)
			{
				this.PostOrderGetAIDiplomaticContractEvaluation();
			}
		}
	}

	private void UpdateBackgroundImage()
	{
		if (this.BackgroundImage == null)
		{
			return;
		}
		if (this.SelectedEmpire == null)
		{
			this.BackgroundImage.Image = null;
			return;
		}
		GuiEmpire guiEmpire = new GuiEmpire(this.SelectedEmpire);
		this.BackgroundImage.Image = guiEmpire.GetImageTexture(global::GuiPanel.IconSize.NegotiationLarge, base.Empire);
	}

	private void RefreshButtons()
	{
		this.TheirEmpireNameDropList.AgeTransform.Enable = this.interactionsAllowed;
		if (this.SelectedEmpire == null || this.DepartmentOfForeignAffairs == null || this.PlayerDiplomaticRelationWithOther == null || this.PlayerDiplomaticRelationWithOther.State.Name == DiplomaticRelationState.Names.Dead)
		{
			this.OfferButton.AgeTransform.Visible = false;
			this.OfferCostLabel.Text = string.Empty;
			this.CounterOfferButton.AgeTransform.Visible = false;
			this.CounterOfferMyCostLabel.Text = string.Empty;
			this.ResetButton.AgeTransform.Visible = false;
			return;
		}
		float cost = this.ComputeContractCost(this.DiplomaticContract.EmpireWhichProposes);
		string text = GuiFormater.FormatInstantCost(this.DiplomaticContract.EmpireWhichProposes, cost, SimulationProperties.EmpirePoint, false, 0);
		float cost2 = this.ComputeContractCost(this.DiplomaticContract.EmpireWhichReceives);
		string text2 = GuiFormater.FormatInstantCost(this.DiplomaticContract.EmpireWhichReceives, cost2, SimulationProperties.EmpirePoint, true, 0);
		bool flag = base.Empire != this.DiplomaticContract.EmpireWhichInitiated;
		if (flag)
		{
			this.OfferButton.AgeTransform.Visible = false;
			this.OfferCostLabel.Text = string.Empty;
			this.CounterOfferButton.AgeTransform.Visible = true;
			this.CounterOfferMyCostLabel.Text = text;
			this.CounterOfferTheirCostLabel.Text = text2;
			this.ResetButton.AgeTransform.Visible = true;
			this.ResetButton.AgeTransform.Enable = false;
			this.ResetButton.AgeTransform.AgeTooltip.Content = "%NegotiationCannotResetCounterOfferDescription";
			this.CounterOfferButton.AgeTransform.Enable = (this.CanContractBeValidated() && this.interactionsAllowed);
		}
		else
		{
			this.OfferButton.AgeTransform.Visible = true;
			this.OfferCostLabel.Text = text;
			this.CounterOfferButton.AgeTransform.Visible = false;
			this.CounterOfferMyCostLabel.Text = string.Empty;
			this.ResetButton.AgeTransform.Visible = true;
			this.ResetButton.AgeTransform.Enable = this.interactionsAllowed;
			this.ResetButton.AgeTransform.AgeTooltip.Content = "%NegotiationResetDescription";
			this.OfferButton.AgeTransform.Enable = (this.CanContractBeValidated() && this.interactionsAllowed);
		}
	}

	private void RefreshDiplomaticAbilities()
	{
		List<GuiElement> filteredDiplomaticAbilityGuiElements = GameNegotiationScreen.GetFilteredDiplomaticAbilityGuiElements(base.Empire, this.SelectedEmpire);
		this.DiplomaticAbilitiesTable.ReserveChildren(filteredDiplomaticAbilityGuiElements.Count, this.DiplomaticAbilityItemPrefab, "Ability");
		this.DiplomaticAbilitiesTable.RefreshChildrenIList<GuiElement>(filteredDiplomaticAbilityGuiElements, this.setupDiplomaticAbilityDelegate, true, false);
	}

	private void RefreshOtherEmpiresDropList()
	{
		global::Game game = base.Game as global::Game;
		Diagnostics.Assert(game != null);
		List<DiplomaticRelation> list = (from relation in this.DepartmentOfForeignAffairs.DiplomaticRelations
		where relation.OwnerEmpireIndex == this.Empire.Index
		select relation).ToList<DiplomaticRelation>();
		list.RemoveAll((DiplomaticRelation diplomaticRelation) => diplomaticRelation.State == null || diplomaticRelation.State.Name == DiplomaticRelationState.Names.Unknown || diplomaticRelation.State.Name == DiplomaticRelationState.Names.Dead);
		this.otherEmpires = (from relation in list
		select game.Empires[relation.OtherEmpireIndex]).ToArray<global::Empire>();
		List<string> list2 = new List<string>();
		List<string> list3 = new List<string>();
		for (int i = 0; i < this.otherEmpires.Length; i++)
		{
			string text;
			AgeUtils.ColorToHexaKey(this.otherEmpires[i].Color, out text);
			string text2 = string.Concat(new string[]
			{
				text,
				this.otherEmpires[i].LocalizedName,
				" - ",
				this.otherEmpires[i].Faction.LocalizedName,
				"#REVERT#"
			});
			list2.Add(text2);
			list3.Add((!this.otherEmpires[i].IsControlledByAI) ? (text2 + "\n" + AgeLocalizer.Instance.LocalizeString("%CompetitorEmpireTypeHumanDescription")) : (text2 + "\n" + AgeLocalizer.Instance.LocalizeString("%CompetitorEmpireTypeComputerDescription")));
		}
		this.TheirEmpireNameDropList.ItemTable = list2.ToArray();
		this.TheirEmpireNameDropList.TooltipTable = list3.ToArray();
	}

	private void SelectEmpire(global::Empire empireToSelect)
	{
		if (empireToSelect != null)
		{
			for (int i = 0; i < this.otherEmpires.Length; i++)
			{
				if (this.otherEmpires[i].Index == empireToSelect.Index)
				{
					this.SelectEmpireDropListIndex(i);
					return;
				}
			}
		}
	}

	private void SelectEmpireDropListIndex(int empireIndex)
	{
		if (empireIndex < 0)
		{
			this.TheirEmpireNameDropList.SelectedItem = this.otherEmpires.Length - 1;
		}
		else if (empireIndex >= this.otherEmpires.Length)
		{
			this.TheirEmpireNameDropList.SelectedItem = 0;
		}
		else
		{
			this.TheirEmpireNameDropList.SelectedItem = empireIndex;
		}
		this.SelectedEmpire = this.otherEmpires[this.TheirEmpireNameDropList.SelectedItem];
		this.UpdateBackgroundImage();
	}

	private void RefreshApprovalSlider()
	{
		this.DealApprovalGauge.RefreshContent(this.contractApproval, this.hoveredTermApproval);
	}

	private void SetupDiplomaticAbility(AgeTransform tableItem, GuiElement diplomaticAbilityGuiElement, int index)
	{
		DiplomaticAbilityItem component = tableItem.GetComponent<DiplomaticAbilityItem>();
		if (component == null)
		{
			Diagnostics.LogError("In the NegotiationScreen, trying to refresh a table item that is not a DiplomaticAbilityItem");
			return;
		}
		component.RefreshContent(diplomaticAbilityGuiElement);
	}

	public float GetEvaluationForTerm(DiplomaticTerm term)
	{
		float result;
		this.diplomaticTermsAndEvalutations.TryGetValue(term, out result);
		return result;
	}

	public const float MaximumApprovalScore = 4f;

	public Transform DiplomaticAbilityItemPrefab;

	public AgePrimitiveImage BackgroundImage;

	public AgePrimitiveLabel YouLabel;

	public AgeControlDropList TheirEmpireNameDropList;

	public AgePrimitiveLabel TheirEmpirePointsValueLabel;

	public DiplomaticRelationshipDisk DiplomaticRelationshipDisk;

	public AgePrimitiveImage MarketplaceBannedIcon;

	public AgePrimitiveImage BlackSpotVictimIcon;

	public AgeTransform DiplomaticAbilitiesTable;

	public TermOptionsPanel MyTermOptionsPanel;

	public TermOptionsPanel TheirTermOptionsPanel;

	public ContractPanel ContractPanel;

	public AgeTransform DealApprovalGroup;

	public GaugeWithDiff DealApprovalGauge;

	public AgeControlButton OfferButton;

	public AgePrimitiveLabel OfferTitleLabel;

	public AgePrimitiveLabel OfferCostLabel;

	public AgeControlButton CounterOfferButton;

	public AgePrimitiveLabel CounterOfferMyCostLabel;

	public AgePrimitiveLabel CounterOfferTheirCostLabel;

	public AgeControlButton ResetButton;

	public string ViewportLayer = "DiplomacyNegotiation";

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private global::Empire[] otherEmpires;

	private global::Empire selectedEmpire;

	private Dictionary<DiplomaticTerm, float> diplomaticTermsAndEvalutations = new Dictionary<DiplomaticTerm, float>();

	private float contractApproval;

	private float hoveredTermApproval;

	private IEndTurnService endTurnService;

	private bool interactionsAllowed = true;

	private AgeTransform.RefreshTableItem<GuiElement> setupDiplomaticAbilityDelegate;

	private DiplomaticNegotiationViewport diplomaticNegotiationViewport;
}
