using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Session;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Xml;
using UnityEngine;

public class MenuFactionScreen : GuiMenuScreen
{
	public int EmpireIndex { get; private set; }

	public override string MenuName
	{
		get
		{
			return "%BreadcrumbFactionTitle";
		}
		set
		{
		}
	}

	public override bool HandleCancelRequest()
	{
		if (base.IsShowing)
		{
			base.StartCoroutine(this.HandleCancelRequestWhenShowingFinished());
			return true;
		}
		base.GuiService.GetGuiPanel<MenuNewGameScreen>().ShowWhenFinishedHiding(new object[0]);
		return true;
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		this.FactionsTable.Height = 0f;
		this.FactionsTable.ReserveChildren(this.guiFactions.Count, this.FactionCardPrefab, "Item");
		this.FactionsTable.RefreshChildrenIList<GuiFaction>(this.guiFactions, this.setupGuiFactionDelegate, true, false);
		this.FactionsTable.ArrangeChildren();
		this.FactionsTableScrollView.OnPositionRecomputed();
		this.ValidateButton.AgeTransform.Enable = false;
		this.ValidateButton.AgeTransform.AgeTooltip.Content = null;
		this.DestroyFactionButton.AgeTransform.Enable = false;
		this.ModifyFactionButton.AgeTransform.Enable = false;
		this.CreateFactionButton.AgeTransform.Enable = true;
		List<FactionCard> children = this.FactionsTable.GetChildren<FactionCard>(true);
		for (int i = 0; i < children.Count; i++)
		{
			children[i].SelectionToggle.State = (this.SelectedGuiFaction != null && children[i].GuiFaction.Faction.Name == this.SelectedGuiFaction.Faction.Name);
		}
		if (this.SelectedGuiFaction == null)
		{
			this.FactionTitle.AgeTransform.Visible = false;
			this.FactionTitleUnderline.AgeTransform.Visible = false;
			this.FactionAuthor.AgeTransform.Visible = false;
			this.FactionMoodImage.AgeTransform.Visible = false;
			this.FactionDescriptionScrollView.AgeTransform.Visible = false;
			this.FactionTraitsScrollView.AgeTransform.Visible = false;
			this.UnitBodyGroup.Visible = false;
			return;
		}
		this.ValidateButton.AgeTransform.Enable = true;
		if (this.SelectedGuiFaction.IsCustom)
		{
			bool enable = GuiFaction.IsValidCustomFaction(this.SelectedGuiFaction.Faction, null);
			this.ValidateButton.AgeTransform.Enable = enable;
		}
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		if (service != null)
		{
			bool flag;
			if (service.TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory.LobbyFaction, this.SelectedGuiFaction.Faction.Name, out flag) && !flag)
			{
				this.ValidateButton.AgeTransform.Enable = false;
				this.ValidateButton.AgeTransform.AgeTooltip.Content = "%RestrictedDownloadableContentTitle";
			}
			if (this.SelectedGuiFaction.Faction.Affinity != null && service.TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory.LobbyFactionAffinity, this.SelectedGuiFaction.Faction.Affinity.Name, out flag) && !flag)
			{
				this.ValidateButton.AgeTransform.Enable = false;
				this.ValidateButton.AgeTransform.AgeTooltip.Content = "%RestrictedDownloadableContentTitle";
			}
			if (this.ValidateButton.AgeTransform.Enable)
			{
				foreach (FactionTrait factionTrait in Faction.EnumerableTraits(this.SelectedGuiFaction.Faction))
				{
					if (!service.TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory.LobbyFactionTrait, factionTrait.Name, out flag) || !flag)
					{
						this.ValidateButton.AgeTransform.Enable = false;
						this.ValidateButton.AgeTransform.AgeTooltip.Content = "%RestrictedDownloadableContentTitle";
						break;
					}
				}
			}
		}
		this.DestroyFactionButton.AgeTransform.Enable = this.SelectedGuiFaction.IsCustom;
		this.ModifyFactionButton.AgeTransform.Enable = this.SelectedGuiFaction.IsCustom;
		this.FactionDescriptionScrollView.AgeTransform.Visible = true;
		this.FactionTitle.AgeTransform.Visible = true;
		this.FactionTitleUnderline.AgeTransform.Visible = true;
		this.FactionAuthor.AgeTransform.Visible = true;
		this.FactionTitle.Text = this.SelectedGuiFaction.Title;
		this.FactionAuthor.Text = string.Empty;
		if (this.SelectedGuiFaction.Faction.Author != "AMPLITUDE Studios")
		{
			this.FactionAuthor.Text = this.SelectedGuiFaction.Faction.Author;
		}
		if (this.diplomaticNegotiationViewport != null)
		{
			this.FactionMoodImage.AgeTransform.Visible = false;
			XmlNamedReference xmlNamedReference = (!this.SelectedGuiFaction.IsRandom) ? this.SelectedGuiFaction.Faction.AffinityMapping : new XmlNamedReference(GuiFaction.FactionRandomMappingName);
			bool flag2 = this.diplomaticNegotiationViewport.AffinityMapping == null || this.diplomaticNegotiationViewport.AffinityMapping != xmlNamedReference;
			this.diplomaticNegotiationViewport.SetApparence(xmlNamedReference);
			if (flag2)
			{
				this.diplomaticNegotiationViewport.TriggerAlternativeIdle(0.1f);
			}
		}
		else
		{
			this.FactionMoodImage.AgeTransform.Visible = true;
			this.FactionMoodImage.Image = this.SelectedGuiFaction.GetImageTexture(GuiPanel.IconSize.NegotiationLarge, false);
			this.FactionDescription.AgeTransform.Height = 0f;
		}
		if (this.SelectedGuiFaction.IsRandom || this.SelectedGuiFaction.Name == "FactionELCPSpectator")
		{
			this.FactionDescription.Text = this.SelectedGuiFaction.Description;
		}
		else
		{
			this.FactionDescription.Text = AgeLocalizer.Instance.LocalizeString("%" + this.SelectedGuiFaction.Faction.Affinity.Name + "VictoryType") + "\n \n" + AgeLocalizer.Instance.LocalizeString(this.SelectedGuiFaction.Description);
		}
		this.FactionDescriptionScrollView.ResetUp();
		if (!this.SelectedGuiFaction.Faction.IsRandom)
		{
			this.FactionTraitsScrollView.AgeTransform.Visible = true;
			this.UnitBodyGroup.Visible = true;
			this.FactionTraitsContainers.Visible = true;
			List<GuiFactionTrait> list = (from trait in Faction.EnumerableTraits(this.SelectedGuiFaction.Faction)
			where !trait.IsHidden && !trait.IsAffinityRelated
			select new GuiFactionTrait(trait)).ToList<GuiFactionTrait>();
			list.Sort();
			this.FactionTraitsTable.Height = 0f;
			this.FactionTraitsTable.ReserveChildren(list.Count, this.FactionTraitPrefab, "Item");
			this.FactionTraitsTable.RefreshChildrenIList<GuiFactionTrait>(list, this.setupGuiFactionTraitDelegate, true, false);
			this.FactionTraitsTable.ArrangeChildren();
			this.FactionTraitsScrollView.ResetUp();
			this.factionUnitBodies.Clear();
			for (int j = 0; j < this.unitBodyDefinitions.Count; j++)
			{
				UnitBodyDefinition unitBodyDefinition = this.unitBodyDefinitions[j];
				if (!unitBodyDefinition.Tags.Contains("Hidden"))
				{
					SimulationDescriptorReference[] simulationDescriptorReferences = unitBodyDefinition.SimulationDescriptorReferences;
					for (int k = 0; k < simulationDescriptorReferences.Length; k++)
					{
						if (simulationDescriptorReferences[k].Name == this.SelectedGuiFaction.Faction.Affinity.Name)
						{
							this.factionUnitBodies.Add(unitBodyDefinition);
						}
					}
				}
			}
			if (this.currentBody >= this.factionUnitBodies.Count)
			{
				this.currentBody = 0;
			}
			if (this.currentBody >= 0 && this.currentBody < this.factionUnitBodies.Count)
			{
				this.UnitBodyCard.RefreshContent(this.factionUnitBodies[this.currentBody]);
			}
			this.RefreshCurrentBody();
			return;
		}
		this.FactionTraitsScrollView.AgeTransform.Visible = false;
		this.UnitBodyGroup.Visible = false;
		this.FactionTraitsContainers.Visible = false;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		IDatabase<Faction> factionsDatabase = Databases.GetDatabase<Faction>(false);
		this.guiFactions = (from faction in factionsDatabase.GetValues()
		where faction.IsStandard || faction.IsCustom
		select new GuiFaction(faction)).ToList<GuiFaction>();
		this.guiFactions.Sort(new GuiFaction.Comparer());
		this.EmpireIndex = -1;
		if (parameters.Length > 0)
		{
			this.EmpireIndex = (int)parameters[0];
		}
		this.SelectedGuiFaction = null;
		if (parameters.Length > 1)
		{
			Faction factionToSelect = parameters[1] as Faction;
			if (factionToSelect == null)
			{
				if (this.guiFactions != null && this.guiFactions.Count > 0)
				{
					this.SelectedGuiFaction = this.guiFactions[0];
				}
			}
			else if (factionToSelect.IsRandom)
			{
				this.SelectedGuiFaction = this.guiFactions.Find((GuiFaction guiFaction) => guiFaction.Faction.Name == "FactionRandom");
			}
			else
			{
				this.SelectedGuiFaction = this.guiFactions.Find((GuiFaction guiFaction) => guiFaction.Faction.Name == factionToSelect.Name);
			}
		}
		if (this.diplomaticNegotiationViewport != null)
		{
			this.diplomaticNegotiationViewport.OnShow(0.5f);
		}
		this.OnToggleGuiFaction(this.SelectedGuiFaction);
		yield break;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		if (this.diplomaticNegotiationViewport != null)
		{
			this.diplomaticNegotiationViewport.OnHide((!instant) ? 0.5f : 0f);
		}
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		this.setupGuiFactionDelegate = new AgeTransform.RefreshTableItem<GuiFaction>(this.SetupGuiFaction);
		this.setupGuiFactionTraitDelegate = new AgeTransform.RefreshTableItem<GuiFactionTrait>(this.SetupGuiFactionTrait);
		this.setupCurrentBodyBarDelegate = new AgeTransform.RefreshTableItem<UnitBodyDefinition>(this.SetupCurrentBodyBar);
		this.factionUnitBodies = new List<UnitBodyDefinition>();
		this.unitBodyDefinitions = Databases.GetDatabase<UnitBodyDefinition>(true).GetValues().ToList<UnitBodyDefinition>();
		GameObject prefab = (GameObject)Resources.Load(DiplomaticNegotiationViewport.DefaultPrefabName);
		if (prefab != null)
		{
			GameObject instance = UnityEngine.Object.Instantiate<GameObject>(prefab);
			if (instance != null)
			{
				instance.transform.parent = base.transform;
				this.diplomaticNegotiationViewport = instance.GetComponent<DiplomaticNegotiationViewport>();
				yield return this.diplomaticNegotiationViewport.OnLoad(this.ViewportLayer);
			}
		}
		yield break;
	}

	protected override void OnUnload()
	{
		this.factionUnitBodies = null;
		this.unitBodyDefinitions = null;
		this.guiFactions = null;
		this.setupCurrentBodyBarDelegate = null;
		this.setupGuiFactionDelegate = null;
		this.setupGuiFactionTraitDelegate = null;
		if (this.diplomaticNegotiationViewport != null)
		{
			this.diplomaticNegotiationViewport.OnUnload();
			UnityEngine.Object.DestroyImmediate(this.diplomaticNegotiationViewport.gameObject);
			this.diplomaticNegotiationViewport = null;
		}
		base.OnUnload();
	}

	private void SetupCurrentBodyBar(AgeTransform tableItem, UnitBodyDefinition unused, int index)
	{
		tableItem.AgePrimitive.TintColor = ((index != this.currentBody) ? this.CurrentBodyColorBase : this.CurrentBodyColorSelected);
		tableItem.Width = this.CurrentBodyGroup.Width / (float)this.factionUnitBodies.Count - this.CurrentBodyGroup.HorizontalSpacing;
	}

	private void SetupGuiFaction(AgeTransform tableItem, GuiFaction guiFaction, int index)
	{
		FactionCard component = tableItem.GetComponent<FactionCard>();
		component.SetContent(guiFaction, base.gameObject);
		ISessionService service = Services.GetService<ISessionService>();
		Diagnostics.Assert(service != null);
		bool lobbyData = service.Session.GetLobbyData<bool>("CustomFactions", true);
		if (guiFaction.IsCustom && !lobbyData)
		{
			component.AgeTransform.Enable = false;
			component.AgeTransform.AgeTooltip.Content = "%CustomFactionsNotAllowed";
		}
		else
		{
			component.AgeTransform.Enable = true;
			component.AgeTransform.AgeTooltip.Content = string.Empty;
		}
		IDownloadableContentService service2 = Services.GetService<IDownloadableContentService>();
		bool flag;
		if (service2 != null && service2.TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory.LobbyFaction, component.GuiFaction.Faction.Name, out flag) && !flag)
		{
			component.AgeTransform.Enable = false;
			component.AgeTransform.AgeTooltip.Content = "%RestrictedDownloadableContentTitle";
		}
		if ((service.Session.SessionMode == SessionMode.Single || !service.Session.GetLobbyData<bool>("SpectatorMode", false) || service.Session.GetLobbyData<int>("NumberOfMajorFactions", 0) < 3) && guiFaction.Name == "FactionELCPSpectator")
		{
			component.AgeTransform.Enable = false;
			component.AgeTransform.AgeTooltip.Content = "%GameOptionSpectatorModeDisabled";
		}
	}

	private void SetupGuiFactionTrait(AgeTransform tableItem, GuiFactionTrait guiFactionTrait, int index)
	{
		AgePrimitiveLabel component = tableItem.GetComponent<AgePrimitiveLabel>();
		if (component != null)
		{
			component.Text = guiFactionTrait.IconAndTitle;
		}
		if (tableItem.AgeTooltip != null)
		{
			guiFactionTrait.GenerateTooltip(tableItem.AgeTooltip, this.SelectedGuiFaction.Faction.Affinity);
		}
	}

	private void OnToggleGuiFaction(GuiFaction guiFaction)
	{
		this.SelectedGuiFaction = guiFaction;
		this.currentBody = 0;
		this.RefreshContent();
	}

	private void OnDoubleClickGuiFaction(GuiFaction guiFaction)
	{
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		if (service != null)
		{
			bool flag;
			if (service.TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory.LobbyFaction, guiFaction.Faction.Name, out flag) && !flag)
			{
				return;
			}
			if (guiFaction.Faction.Affinity != null && service.TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory.LobbyFactionAffinity, guiFaction.Faction.Affinity.Name, out flag) && !flag)
			{
				return;
			}
			foreach (FactionTrait factionTrait in Faction.EnumerableTraits(guiFaction.Faction))
			{
				if (!service.TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory.LobbyFactionTrait, factionTrait.Name, out flag) || !flag)
				{
					return;
				}
			}
		}
		this.OnToggleGuiFaction(guiFaction);
		this.OnSelectCB(null);
	}

	private void OnCreateCustomFactionCB(GameObject obj = null)
	{
		base.SetupDepthUp();
		base.GuiService.GetGuiPanel<MenuCustomFactionScreen>().SetupDepthDown();
		base.GuiService.GetGuiPanel<MenuCustomFactionScreen>().CreateMode = true;
		base.GuiService.GetGuiPanel<MenuCustomFactionScreen>().Show(new object[]
		{
			this.EmpireIndex,
			this.SelectedGuiFaction.Faction
		});
		base.GuiService.GetGuiPanel<MenuCustomFactionScreen>().SetBreadcrumb(this.MenuBreadcrumb.Text);
	}

	private void OnEditCustomFactionCB(GameObject obj = null)
	{
		base.SetupDepthUp();
		base.GuiService.GetGuiPanel<MenuCustomFactionScreen>().SetupDepthDown();
		base.GuiService.GetGuiPanel<MenuCustomFactionScreen>().CreateMode = false;
		base.GuiService.GetGuiPanel<MenuCustomFactionScreen>().Show(new object[]
		{
			this.EmpireIndex,
			this.SelectedGuiFaction.Faction
		});
		base.GuiService.GetGuiPanel<MenuCustomFactionScreen>().SetBreadcrumb(this.MenuBreadcrumb.Text);
	}

	private void OnDeleteCustomFactionCB(GameObject obj = null)
	{
		string message = string.Format(AgeLocalizer.Instance.LocalizeString("%FactionConfirmDeleteTitle"), this.SelectedGuiFaction.Title);
		MessagePanel.Instance.Show(message, string.Empty, MessagePanelButtons.OkCancel, new MessagePanel.EventHandler(this.OnConfirmDeleteCustomFaction), MessagePanelType.IMPORTANT, new MessagePanelButton[0]);
	}

	private void OnConfirmDeleteCustomFaction(object sender, MessagePanelResultEventArgs e)
	{
		if (e.Result == MessagePanelResult.Ok)
		{
			this.DestroyFactionButton.AgeTransform.Enable = false;
			if (this.SelectedGuiFaction != null && this.SelectedGuiFaction.Faction != null)
			{
				if (!string.IsNullOrEmpty(this.SelectedGuiFaction.Faction.FileName))
				{
					try
					{
						File.Delete(this.SelectedGuiFaction.Faction.FileName);
					}
					catch
					{
					}
				}
				IDatabase<Faction> database = Databases.GetDatabase<Faction>(false);
				if (database != null)
				{
					database.Remove(this.SelectedGuiFaction.Faction.Name);
				}
				IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
				base.GuiService.GetGuiPanel<MenuNewGameScreen>().OnCustomFactionDeleted(this.SelectedGuiFaction.Faction.Name);
				this.guiFactions.Remove(this.SelectedGuiFaction);
				if (this.guiFactions.Count > 0)
				{
					this.SelectedGuiFaction = this.guiFactions[0];
					if (service != null)
					{
						bool flag = false;
						if (!service.TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory.LobbyFaction, this.guiFactions[0].Name, out flag) || !flag)
						{
							Faction faction = database.FirstOrDefault((Faction iterator) => iterator.IsStandard && !iterator.IsHidden);
							for (int i = 0; i < this.guiFactions.Count; i++)
							{
								if (this.guiFactions[i].Faction.Name == faction.Name)
								{
									this.SelectedGuiFaction = this.guiFactions[i];
									break;
								}
							}
						}
					}
				}
				else
				{
					this.SelectedGuiFaction = null;
				}
			}
		}
		this.RefreshContent();
	}

	private void OnCancelCB(GameObject obj = null)
	{
		this.HandleCancelRequest();
	}

	private void OnPreviousBodyCB(GameObject obj = null)
	{
		if (this.currentBody > 0)
		{
			this.currentBody--;
		}
		this.RefreshCurrentBody();
	}

	private void OnNextBodyCB(GameObject obj = null)
	{
		if (this.currentBody < this.factionUnitBodies.Count - 1)
		{
			this.currentBody++;
		}
		this.RefreshCurrentBody();
	}

	private void OnSelectCB(GameObject obj = null)
	{
		this.Hide(false);
		base.GuiService.GetGuiPanel<MenuNewGameScreen>().Show(new object[0]);
		base.GuiService.GetGuiPanel<MenuNewGameScreen>().SelectFaction(this.EmpireIndex, this.SelectedGuiFaction.Faction);
	}

	private IEnumerator HandleCancelRequestWhenShowingFinished()
	{
		while (base.IsShowing)
		{
			yield return null;
		}
		this.HandleCancelRequest();
		yield break;
	}

	private void RefreshCurrentBody()
	{
		this.BodyDescriptionScrollView.ResetUp();
		this.UnitBodyCard.UnitBodyDescription.AgeTransform.Height = 0f;
		this.PreviousBodyButton.AgeTransform.Enable = (this.currentBody > 0);
		this.NextBodyButton.AgeTransform.Enable = (this.currentBody < this.factionUnitBodies.Count - 1);
		if (this.currentBody >= 0 && this.currentBody < this.factionUnitBodies.Count)
		{
			this.UnitBodyCard.RefreshContent(this.factionUnitBodies[this.currentBody]);
			this.CurrentBodyGroup.Visible = true;
			this.CurrentBodyGroup.ReserveChildren(this.factionUnitBodies.Count, this.CurrentBodyBarPrefab, "Item");
			this.CurrentBodyGroup.RefreshChildrenIList<UnitBodyDefinition>(this.factionUnitBodies, this.setupCurrentBodyBarDelegate, true, false);
		}
		else
		{
			this.CurrentBodyGroup.Visible = false;
			this.UnitBodyCard.RefreshContent(null);
		}
	}

	public Transform FactionCardPrefab;

	public Transform FactionTraitPrefab;

	public Transform CurrentBodyBarPrefab;

	public AgeTransform FactionsTable;

	public AgeControlScrollView FactionsTableScrollView;

	public AgePrimitiveImage FactionMoodImage;

	public AgePrimitiveLabel FactionTitle;

	public AgePrimitiveLineRectilinear FactionTitleUnderline;

	public AgePrimitiveLabel FactionAuthor;

	public AgePrimitiveLabel FactionDescription;

	public AgeControlScrollView FactionDescriptionScrollView;

	public AgeTransform FactionTraitsContainers;

	public AgeTransform FactionTraitsTable;

	public AgeControlScrollView FactionTraitsScrollView;

	public AgeTransform UnitBodyGroup;

	public AgeTransform CurrentBodyGroup;

	public UnitBodyCard UnitBodyCard;

	public AgeControlScrollView BodyDescriptionScrollView;

	public AgeControlButton PreviousBodyButton;

	public AgeControlButton NextBodyButton;

	public AgeControlButton DestroyFactionButton;

	public AgeControlButton ModifyFactionButton;

	public AgeControlButton CreateFactionButton;

	public AgeControlButton ValidateButton;

	public Color CurrentBodyColorBase;

	public Color CurrentBodyColorSelected;

	public GuiFaction SelectedGuiFaction;

	public string ViewportLayer = "DiplomacyNegotiation";

	private int currentBody;

	private List<GuiFaction> guiFactions;

	private List<UnitBodyDefinition> unitBodyDefinitions;

	private List<UnitBodyDefinition> factionUnitBodies;

	private AgeTransform.RefreshTableItem<GuiFaction> setupGuiFactionDelegate;

	private AgeTransform.RefreshTableItem<UnitBodyDefinition> setupCurrentBodyBarDelegate;

	private AgeTransform.RefreshTableItem<GuiFactionTrait> setupGuiFactionTraitDelegate;

	private DiplomaticNegotiationViewport diplomaticNegotiationViewport;
}
