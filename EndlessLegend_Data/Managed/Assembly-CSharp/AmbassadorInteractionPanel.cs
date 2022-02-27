using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Gui;
using UnityEngine;

public class AmbassadorInteractionPanel : global::GuiPanel
{
	public Bounds PanelTargetBounds
	{
		set
		{
			base.AgeTransform.X = value.center.x - base.AgeTransform.Width / 2f;
			float num = (float)Screen.height - value.center.y;
			base.AgeTransform.Y = num + value.extents.y - base.AgeTransform.Height;
		}
	}

	public Empire AmbassadorEmpire { get; private set; }

	public bool IsHovered
	{
		get
		{
			if (!base.IsVisible)
			{
				return false;
			}
			Rect rect;
			base.AgeTransform.ComputeGlobalPosition(out rect);
			return rect.Contains(AgeManager.Instance.Cursor);
		}
	}

	private Empire LookingEmpire { get; set; }

	private Empire PlayerEmpire { get; set; }

	public override void RefreshContent()
	{
		base.RefreshContent();
		this.RelationshipGroup.RefreshContent();
		this.RefreshDiplomaticAbilities();
		DepartmentOfForeignAffairs agency = this.AmbassadorEmpire.GetAgency<DepartmentOfForeignAffairs>();
		this.MarketplaceBannedIcon.AgeTransform.Visible = agency.IsBannedFromMarket();
		this.BlackSpotVictimIcon.AgeTransform.Visible = agency.IsBlackSpotVictim();
	}

	public void Bind(Empire lookingEmpire, Empire ambassadorEmpire, Empire playerEmpire, GameDiplomacyScreen parent)
	{
		this.LookingEmpire = lookingEmpire;
		this.AmbassadorEmpire = ambassadorEmpire;
		this.PlayerEmpire = playerEmpire;
		this.parent = parent;
		this.EmpireNameLabel.Text = this.AmbassadorEmpire.LocalizedName;
		this.NegotiateButton.AgeTransform.Enable = (this.AmbassadorEmpire != this.PlayerEmpire);
		if (this.CanSeeAllInformation())
		{
			this.RelationshipGroup.AgeTransform.Visible = true;
			this.UnknownRelationshipGroup.Visible = false;
			this.DiplomaticAbilitiesTable.Visible = true;
			this.UnknownDiplomaticAbilitiesGroup.Visible = false;
		}
		else
		{
			this.RelationshipGroup.AgeTransform.Visible = false;
			this.UnknownRelationshipGroup.Visible = true;
			this.DiplomaticAbilitiesTable.Visible = false;
			this.UnknownDiplomaticAbilitiesGroup.Visible = true;
		}
		this.RelationshipGroup.Bind(this.LookingEmpire, this.AmbassadorEmpire);
		this.EspionageGroup.Visible = false;
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		if (service != null && service.IsShared(DownloadableContent11.ReadOnlyName))
		{
			DepartmentOfIntelligence agency = this.LookingEmpire.GetAgency<DepartmentOfIntelligence>();
			if (agency != null)
			{
				if (agency.IsEmpireInfiltrated(this.AmbassadorEmpire) && this.PlayerEmpire.Index == this.LookingEmpire.Index)
				{
					this.EspionageGroup.Visible = true;
				}
				else
				{
					this.EspionageGroup.Visible = false;
				}
			}
		}
	}

	public void Unbind()
	{
		base.StopAllCoroutines();
		this.RelationshipGroup.Unbind();
		this.parent = null;
		this.AmbassadorEmpire = null;
		this.LookingEmpire = null;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		base.StartCoroutine(this.ShowWhileHover());
		if (Amplitude.Unity.Framework.Application.Version.Accessibility <= Accessibility.Private)
		{
			IPlayerControllerRepositoryService playerControllerRepository = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
			Diagnostics.Assert(playerControllerRepository != null);
			Game game = base.Game as Game;
			IDiplomaticContractRepositoryService diplomacyService = game.Services.GetService<IDiplomaticContractRepositoryService>();
			Diagnostics.Assert(diplomacyService != null);
			this.existingContracts = diplomacyService.FindAll((DiplomaticContract contract) => contract.EmpireWhichProposes == this.AmbassadorEmpire && contract.EmpireWhichReceives == playerControllerRepository.ActivePlayerController.Empire && contract.State != DiplomaticContractState.Negotiation).ToList<DiplomaticContract>();
			this.DebugShowNotifButton.Visible = (this.existingContracts.Count > 0);
		}
		else
		{
			this.DebugShowNotifButton.Visible = false;
		}
		this.RefreshContent();
		yield break;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		this.parent.OnAmbassadorInteractionPanelClosed();
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		this.setupDiplomaticAbilityDelegate = new AgeTransform.RefreshTableItem<GuiElement>(this.SetupDiplomaticAbility);
		yield break;
	}

	protected override void OnUnload()
	{
		this.setupDiplomaticAbilityDelegate = null;
		base.OnUnload();
	}

	private bool CanSeeAllInformation()
	{
		if (this.LookingEmpire.Index == this.PlayerEmpire.Index || this.AmbassadorEmpire.Index == this.PlayerEmpire.Index || this.PlayerEmpire.SimulationObject.Tags.Contains(Empire.TagEmpireEliminated))
		{
			return true;
		}
		DepartmentOfIntelligence agency = this.PlayerEmpire.GetAgency<DepartmentOfIntelligence>();
		return agency != null && agency.IsEmpireInfiltrated(this.LookingEmpire);
	}

	private void OnInspectCB(GameObject obj)
	{
		if (this.AmbassadorEmpire != null && this.parent != null)
		{
			this.parent.SendMessage("OnInspect", this.AmbassadorEmpire);
		}
		this.Hide(false);
	}

	private void OnNegotiationCB(GameObject obj)
	{
		if (this.PlayerEmpire.SimulationObject.Tags.Contains(Empire.TagEmpireEliminated))
		{
			return;
		}
		if (!base.IsVisible || base.IsHiding)
		{
			return;
		}
		if (this.AmbassadorEmpire != null && this.parent != null)
		{
			this.parent.SendMessage("OnNegotiation", this.AmbassadorEmpire);
		}
		this.Hide(false);
	}

	private void OnCurrentTreatiesCB(GameObject obj)
	{
		if (this.AmbassadorEmpire != null && this.parent != null)
		{
			this.parent.SendMessage("OnCurrentTreaties", this.AmbassadorEmpire);
		}
		this.Hide(false);
	}

	private void OnDebugShowNotifCB(GameObject obj)
	{
		IGuiNotificationService service = Services.GetService<IGuiNotificationService>();
		for (int i = 0; i < service.GuiNotifications.Count; i++)
		{
			GuiNotificationDiplomaticContractStateChange guiNotificationDiplomaticContractStateChange = service.GuiNotifications[i] as GuiNotificationDiplomaticContractStateChange;
			if (guiNotificationDiplomaticContractStateChange != null)
			{
				EventDiplomaticContractStateChange eventDiplomaticContractStateChange = guiNotificationDiplomaticContractStateChange.RaisedEvent as EventDiplomaticContractStateChange;
				if (eventDiplomaticContractStateChange != null && this.existingContracts.Contains(eventDiplomaticContractStateChange.DiplomaticContract))
				{
					return;
				}
			}
		}
		foreach (DiplomaticContract diplomaticContract in this.existingContracts)
		{
			IEventService service2 = Services.GetService<IEventService>();
			service2.Notify(new EventDiplomaticContractStateChange(diplomaticContract, diplomaticContract.State));
		}
	}

	private void RefreshDiplomaticAbilities()
	{
		List<GuiElement> filteredDiplomaticAbilityGuiElements = GameNegotiationScreen.GetFilteredDiplomaticAbilityGuiElements(this.LookingEmpire, this.AmbassadorEmpire);
		this.DiplomaticAbilitiesTable.ReserveChildren(filteredDiplomaticAbilityGuiElements.Count, this.DiplomaticAbilityItemPrefab, "Ability");
		this.DiplomaticAbilitiesTable.RefreshChildrenIList<GuiElement>(filteredDiplomaticAbilityGuiElements, this.setupDiplomaticAbilityDelegate, true, false);
		this.DiplomaticAbilitiesTable.ArrangeChildren();
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
		component.AgeTransform.Alpha = 0.7f;
	}

	private IEnumerator ShowWhileHover()
	{
		while (base.IsShowing)
		{
			yield return null;
		}
		while (!this.ShouldHide())
		{
			yield return null;
		}
		if (!this.ShouldHide())
		{
			base.StartCoroutine(this.ShowWhileHover());
		}
		else
		{
			this.Hide(false);
		}
		yield break;
	}

	private bool ShouldHide()
	{
		return this.parent.OverrolledEmpire != this.AmbassadorEmpire && !this.IsHovered;
	}

	private void OnViewEffectsCB()
	{
		global::IGuiService service = Services.GetService<global::IGuiService>();
		service.Show(typeof(ViewEffectsModalPanel), new object[]
		{
			base.gameObject,
			false
		});
	}

	public const float SecondsBeforeClosing = 0.3f;

	public Transform DiplomaticAbilityItemPrefab;

	public AgeControlButton NegotiateButton;

	public AgePrimitiveLabel EmpireNameLabel;

	public DiplomaticRelationshipDisk RelationshipGroup;

	public AgeTransform UnknownRelationshipGroup;

	public AgeTransform DiplomaticAbilitiesTable;

	public AgeTransform UnknownDiplomaticAbilitiesGroup;

	public AgeTransform DebugShowNotifButton;

	public AgePrimitiveImage MarketplaceBannedIcon;

	public AgePrimitiveImage BlackSpotVictimIcon;

	public AgeTransform EspionageGroup;

	private List<DiplomaticContract> existingContracts;

	private GameDiplomacyScreen parent;

	private AgeTransform.RefreshTableItem<GuiElement> setupDiplomaticAbilityDelegate;
}
