using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Interop;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Serialization;
using Amplitude.Unity.Session;
using Amplitude.Unity.Xml;
using UnityEngine;

public class MenuCustomFactionScreen : GuiMenuScreen
{
	public MenuCustomFactionScreen()
	{
		this.profanityError = string.Empty;
		this.invalidColor = new Color(0.7529412f, 0.2509804f, 0.2509804f);
		base..ctor();
	}

	private void StartProfanityFiltering()
	{
	}

	public GuiFactionTrait SelectedAffinity
	{
		get
		{
			return this.selectedAffinity;
		}
		set
		{
			if (this.selectedAffinity != value)
			{
				this.selectedAffinity = value;
				if (this.selectedAffinity != null)
				{
					this.BuildListOfPreselectedTraits();
					this.CheckListOfSelectedTraits();
				}
			}
		}
	}

	public GuiFactionTrait SelectedAffinityMapping
	{
		get
		{
			return this.selectedAffinityMapping;
		}
		set
		{
			if (this.selectedAffinityMapping != value)
			{
				this.selectedAffinityMapping = value;
				if (this.selectedAffinityMapping != null)
				{
					if (this.diplomaticNegotiationViewport != null)
					{
						this.MoodImage.AgeTransform.Visible = false;
						string text = this.SelectedAffinityMapping.FactionTrait.Name;
						bool flag = this.diplomaticNegotiationViewport.AffinityMapping == null || string.Compare(this.diplomaticNegotiationViewport.AffinityMapping.ToString(), text) != 0;
						this.diplomaticNegotiationViewport.SetApparence(new XmlNamedReference(text));
						if (flag)
						{
							this.diplomaticNegotiationViewport.TriggerAlternativeIdle(0.1f);
						}
					}
					else
					{
						this.MoodImage.AgeTransform.Visible = true;
						this.MoodImage.Image = this.SelectedAffinityMapping.GetImageTexture(GuiPanel.IconSize.MoodScore);
					}
				}
			}
		}
	}

	public override string MenuName
	{
		get
		{
			return "%BreadcrumbCustomFactionTitle";
		}
		set
		{
		}
	}

	public bool CreateMode { get; set; }

	public override bool HandleCancelRequest()
	{
		if (this.modified)
		{
			MessagePanel.Instance.Show("%ConfirmCustomFactionWithoutSaving", string.Empty, MessagePanelButtons.YesNo, new MessagePanel.EventHandler(this.DoCancelRequest), MessagePanelType.IMPORTANT, new MessagePanelButton[0]);
		}
		else
		{
			if (base.IsShowing)
			{
				base.StartCoroutine(this.HandleCancelRequestWhenShowingFinished());
				return true;
			}
			base.GuiService.GetGuiPanel<MenuFactionScreen>().ShowWhenFinishedHiding(new object[]
			{
				this.empireIndex,
				this.givenFaction
			});
		}
		return true;
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		this.AffinityDropList.SelectedItem = this.affinityGuiTraits.IndexOf(this.SelectedAffinity);
		this.OverrideSelectedAffinityTooltip();
		this.SelectedAffinityMapping = this.SelectedAffinity.DefaultGuiAffinityMapping;
		Diagnostics.Assert(this.SelectedAffinityMapping != null);
		this.PortraitImage.Image = this.SelectedAffinityMapping.GetImageTexture(GuiPanel.IconSize.Leader);
		this.BuildListOfAvailableTraits();
		this.AvailableTraitsTable.Height = 0f;
		this.AvailableTraitsTable.ReserveChildren(this.availableGuiTraits.Count, this.CustomTraitPrefab, "AvailableTrait");
		this.AvailableTraitsTable.RefreshChildrenIList<GuiFactionTrait>(this.availableGuiTraits, this.setupAvailableGuiTraitDelegate, true, false);
		this.AvailableTraitsTable.ArrangeChildren();
		this.AvailableTraitsScrollView.OnPositionRecomputed();
		this.PreselectedTraitsTable.Height = 0f;
		this.PreselectedTraitsTable.ReserveChildren(this.preselectedGuiTraits.Count, this.CustomTraitPrefab, "PreselectedTrait");
		this.PreselectedTraitsTable.RefreshChildrenIList<GuiFactionTrait>(this.preselectedGuiTraits, this.setupSelectedGuiTraitDelegate, true, false);
		this.PreselectedTraitsTable.ArrangeChildren();
		this.SelectedTraitsTable.Height = 0f;
		this.SelectedTraitsTable.ReserveChildren(this.selectedGuiTraits.Count, this.CustomTraitPrefab, "SelectedTrait");
		this.SelectedTraitsTable.RefreshChildrenIList<GuiFactionTrait>(this.selectedGuiTraits, this.setupSelectedGuiTraitDelegate, true, false);
		this.SelectedTraitsTable.ArrangeChildren();
		this.SelectedTraitsTable.Y = this.PreselectedTraitsTable.Y + this.PreselectedTraitsTable.Height + this.SelectedTraitsScrollView.VirtualArea.VerticalSpacing;
		this.SelectedTraitsScrollView.VirtualArea.Height = this.SelectedTraitsTable.Y + this.SelectedTraitsTable.Height;
		this.SelectedTraitsScrollView.OnPositionRecomputed();
		this.totalPoints = this.ComputePointsSpent();
		this.PointsCounterLabel.Text = GuiFormater.FormatGui(this.totalPoints) + "/" + GuiFormater.FormatGui(this.SelectedAffinity.MaxPoints);
		if (this.totalPoints <= this.SelectedAffinity.MaxPoints)
		{
			this.PointsCounterBackground.TintColor = this.PointsCounterBackgroundColors[0];
		}
		else
		{
			this.PointsCounterBackground.TintColor = this.PointsCounterBackgroundColors[1];
		}
		this.RefreshButtons();
		this.AvailableTraitsSortsContainer.RefreshSortContent();
		this.SelectedTraitsSortsContainer.RefreshSortContent();
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		if (this.affinityGuiTraits.Count == 0)
		{
			this.HandleCancelRequest();
			yield break;
		}
		if (this.affinityMappingGuiTraits.Count == 0)
		{
			this.HandleCancelRequest();
			yield break;
		}
		this.availableGuiTraits.Clear();
		this.preselectedGuiTraits.Clear();
		this.selectedGuiTraits.Clear();
		if (this.diplomaticNegotiationViewport != null)
		{
			this.diplomaticNegotiationViewport.OnShow(0.5f);
		}
		this.empireIndex = -1;
		if (parameters.Length > 0)
		{
			this.empireIndex = (int)parameters[0];
		}
		this.givenFaction = null;
		if (parameters.Length > 1)
		{
			this.givenFaction = (parameters[1] as Faction);
		}
		IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
		if (downloadableContentService != null)
		{
			for (int index = 0; index < this.AffinityDropList.ItemTable.Length; index++)
			{
				this.AffinityDropList.EnableItem(index, true);
				if (index < this.affinityGuiTraits.Count)
				{
					GuiFactionTrait guiFactionTrait = this.affinityGuiTraits[index];
					if (this.givenFaction.Affinity == null || !(this.givenFaction.Affinity.Name == guiFactionTrait.Name))
					{
						bool result;
						if (downloadableContentService.TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory.FactionAffinity, guiFactionTrait.Name, out result) && !result)
						{
							this.AffinityDropList.EnableItem(index, false);
						}
					}
				}
			}
		}
		this.CreateFactionSqueleton();
		this.NameTextField.AgePrimitiveLabel.Text = this.Faction.LocalizedName;
		this.DescriptionTextField.AgePrimitiveLabel.Text = this.Faction.LocalizedDescription;
		this.DescriptionTextField.AgeTransform.AgeTooltip.Content = this.Faction.LocalizedDescription;
		this.AuthorTextField.AgePrimitiveLabel.Text = this.Faction.Author;
		if (this.CreateMode)
		{
			AgeManager.Instance.FocusedControl = this.NameTextField;
		}
		this.CreateCustomTraitFamilyFilters();
		this.AvailableTraitsSortsContainer.SetContent(this.CustomTraitPrefab, "CustomTrait", null);
		this.SelectedTraitsSortsContainer.SetContent(this.CustomTraitPrefab, "CustomTrait", null);
		this.RefreshContent();
		yield break;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		yield return base.OnHide(instant);
		this.AvailableTraitsSortsContainer.UnsetContent();
		this.SelectedTraitsSortsContainer.UnsetContent();
		this.empireIndex = -1;
		this.givenFaction = null;
		this.availableGuiTraits.Clear();
		this.preselectedGuiTraits.Clear();
		this.selectedGuiTraits.Clear();
		this.SelectedAffinity = null;
		this.SelectedAffinityMapping = null;
		this.Faction = null;
		if (this.diplomaticNegotiationViewport != null)
		{
			this.diplomaticNegotiationViewport.OnHide((!instant) ? 0.5f : 0f);
		}
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		this.setupAvailableGuiTraitDelegate = new AgeTransform.RefreshTableItem<GuiFactionTrait>(this.SetupAvailableGuiTrait);
		this.setupSelectedGuiTraitDelegate = new AgeTransform.RefreshTableItem<GuiFactionTrait>(this.SetupSelectedGuiTrait);
		this.setupCustomTraitFamilyDelegate = new AgeTransform.RefreshTableItem<string>(this.SetupCustomTraitFamily);
		this.NameTextField.ValidChars = AgeLocalizer.Instance.LocalizeString("%FactionValidChars");
		this.DescriptionTextField.ValidChars = AgeLocalizer.Instance.LocalizeString("%FactionValidChars");
		this.affinityGuiTraits.Clear();
		this.affinityMappingGuiTraits.Clear();
		this.standardGuiTraits.Clear();
		IDatabase<FactionTrait> factionTraitDatabase = Databases.GetDatabase<FactionTrait>(true);
		FactionTrait[] factionTraits = factionTraitDatabase.GetValues();
		for (int i = 0; i < factionTraits.Length; i++)
		{
			GuiFactionTrait guiTrait2 = new GuiFactionTrait(factionTraits[i]);
			if (!guiTrait2.FactionTrait.IsHidden)
			{
				if (guiTrait2.IsAffinity)
				{
					if (guiTrait2.FactionTrait.SubCategory == "MajorFactionAffinity")
					{
						this.affinityGuiTraits.Add(guiTrait2);
					}
				}
				else if (guiTrait2.IsAffinityMapping)
				{
					if (guiTrait2.FactionTrait.SubCategory == "MajorFactionAffinityMapping")
					{
						this.affinityMappingGuiTraits.Add(guiTrait2);
					}
				}
				else if (!guiTrait2.IsHidden)
				{
					this.standardGuiTraits.Add(guiTrait2);
				}
			}
		}
		this.InitalizeRootInStandardTraits();
		this.BuildListOfAvailableFamilies();
		this.AffinityDropList.ItemTable = (from guiTrait in this.affinityGuiTraits
		select guiTrait.IconAndTitle).ToArray<string>();
		this.OverrideAffinitiesDropListTooltips();
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
		this.affinityGuiTraits.Clear();
		this.affinityMappingGuiTraits.Clear();
		this.standardGuiTraits.Clear();
		this.setupAvailableGuiTraitDelegate = null;
		this.setupSelectedGuiTraitDelegate = null;
		this.setupCustomTraitFamilyDelegate = null;
		if (this.diplomaticNegotiationViewport != null)
		{
			this.diplomaticNegotiationViewport.OnUnload();
			UnityEngine.Object.DestroyImmediate(this.diplomaticNegotiationViewport.gameObject);
			this.diplomaticNegotiationViewport = null;
		}
		base.OnUnload();
	}

	private void OnCreateCB(GameObject obj = null)
	{
		if (this.totalPoints < this.SelectedAffinity.MaxPoints)
		{
			string message = string.Format(AgeLocalizer.Instance.LocalizeString("%ConfirmCustomFactionWithoutAllPointsUsed"), this.SelectedAffinity.MaxPoints, this.totalPoints);
			MessagePanel.Instance.Show(message, "%Confirmation", MessagePanelButtons.YesNo, new MessagePanel.EventHandler(this.OnConfirmCreateCustomFaction), MessagePanelType.INFORMATIVE, new MessagePanelButton[0]);
		}
		else
		{
			this.CreateCustomFaction();
		}
	}

	private void DoCancelRequest(object sender, MessagePanelResultEventArgs e)
	{
		if (e.Result == MessagePanelResult.Yes)
		{
			this.modified = false;
			base.GuiService.GetGuiPanel<MenuFactionScreen>().ShowWhenFinishedHiding(new object[]
			{
				this.empireIndex,
				this.givenFaction
			});
		}
	}

	private void OnCancelCB(GameObject obj = null)
	{
		this.HandleCancelRequest();
	}

	private void OnAffinityChoiceCB(GameObject gameObject)
	{
		if (this.AffinityDropList.SelectedItem >= 0 && this.AffinityDropList.SelectedItem < this.affinityGuiTraits.Count)
		{
			this.modified = true;
			this.SelectedAffinity = this.affinityGuiTraits[this.AffinityDropList.SelectedItem];
			this.RefreshContent();
		}
	}

	private void OnNameFocusGainedCB(GameObject gameObject)
	{
		if (this.NameTextField.AgePrimitiveLabel.Text == AgeLocalizer.Instance.LocalizeString("%CustomFactionEnterName"))
		{
			this.NameTextField.ReplaceInputText(string.Empty);
		}
	}

	private void OnNameChangedCB(GameObject gameObject)
	{
		this.modified = true;
		this.ValidateName();
		this.StartProfanityFiltering();
	}

	private void OnNameFocusLostCB(GameObject gameObject)
	{
		if (this.Faction == null)
		{
			return;
		}
		if (!this.ValidateName())
		{
			this.NameTextField.AgePrimitiveLabel.Text = AgeLocalizer.Instance.LocalizeString("%CustomFactionEnterName");
		}
		this.RefreshButtons();
	}

	private void OnNameValidatedCB(GameObject gameObject)
	{
		AgeManager.Instance.FocusedControl = null;
	}

	private bool IsFactionNameAlreadyUsed(string loweredFactionName)
	{
		if (loweredFactionName == AgeLocalizer.Instance.LocalizeString("%CustomFactionEnterName").ToLower())
		{
			return true;
		}
		List<Faction> list = Databases.GetDatabase<Faction>(false).GetValues().ToList<Faction>();
		if (!this.CreateMode)
		{
			list.RemoveAll((Faction faction) => faction == this.givenFaction);
		}
		return list.Any((Faction faction) => faction.LocalizedName.ToLower() == loweredFactionName);
	}

	private bool ValidateName()
	{
		string text = this.NameTextField.AgePrimitiveLabel.Text.Trim().ToLower();
		bool flag = true;
		if (text.Length == 0)
		{
			flag = false;
			this.NameTextField.AgeTransform.AgeTooltip.Content = "%CustomFactionNameCannotBeEmptyDescription";
			this.ValidateButton.AgeTransform.AgeTooltip.Content = "%CustomFactionNameCannotBeEmptyDescription";
		}
		else if (this.IsFactionNameAlreadyUsed(text))
		{
			flag = false;
			this.NameTextField.AgeTransform.AgeTooltip.Content = "%CustomFactionNameAlreadyExistsDescription";
			this.ValidateButton.AgeTransform.AgeTooltip.Content = "%CustomFactionNameAlreadyExistsDescription";
		}
		if (flag)
		{
			this.NameSelectionFrame.TintColor = Color.white;
			this.NameTextField.AgeTransform.AgeTooltip.Content = null;
			this.ValidateButton.AgeTransform.AgeTooltip.Content = "%ValidateDescription";
		}
		else
		{
			this.NameSelectionFrame.TintColor = Color.red;
			this.ValidateButton.AgeTransform.Enable = false;
		}
		return flag;
	}

	private void OnDescriptionFocusGainedCB(GameObject gameObject)
	{
		if (this.DescriptionTextField.AgePrimitiveLabel.Text == AgeLocalizer.Instance.LocalizeString("%CustomFactionEnterDescription"))
		{
			this.DescriptionTextField.ReplaceInputText(string.Empty);
		}
		this.DescriptionTextField.AgeTransform.AgeTooltip.Content = string.Empty;
	}

	private void OnDescriptionFocusLostCB(GameObject gameObject)
	{
		if (this.Faction == null)
		{
			return;
		}
		if (this.DescriptionTextField.AgePrimitiveLabel.Text == string.Empty)
		{
			this.DescriptionTextField.ReplaceInputText(AgeLocalizer.Instance.LocalizeString("%CustomFactionEnterDescription"));
		}
		this.DescriptionTextField.AgeTransform.AgeTooltip.Content = this.DescriptionTextField.AgePrimitiveLabel.Text;
		this.StartProfanityFiltering();
	}

	private void OnDescriptionValidatedCB(GameObject gameObject)
	{
		this.modified = true;
		AgeManager.Instance.FocusedControl = null;
	}

	private void OnAuthorTextChangedCB(GameObject gameObject)
	{
		this.StartProfanityFiltering();
	}

	private void OnDescriptionChangedCB(GameObject gameObject)
	{
		this.StartProfanityFiltering();
	}

	private void OnToggleFilter(CustomTraitFamilyFilterToggle familyFilterToggle)
	{
		this.selectedCustomTraitFamily = familyFilterToggle.Family;
		List<CustomTraitFamilyFilterToggle> children = this.CustomTraitFamiliesTable.GetChildren<CustomTraitFamilyFilterToggle>(true);
		for (int i = 0; i < children.Count; i++)
		{
			children[i].Toggle.State = (children[i].Family == this.selectedCustomTraitFamily);
		}
		this.RefreshContent();
	}

	private void OnClickAvailableLine(MenuCustomTraitLine customTraitLine)
	{
		this.modified = true;
		GuiFactionTrait guiFactionTrait = this.selectedGuiTraits.FirstOrDefault((GuiFactionTrait match) => !string.IsNullOrEmpty(match.Root) && match.Root == customTraitLine.GuiFactionTrait.Root);
		if (guiFactionTrait != null)
		{
			if (customTraitLine.GuiFactionTrait.Level > guiFactionTrait.Level)
			{
				this.selectedGuiTraits.Remove(guiFactionTrait);
				this.selectedGuiTraits.Add(customTraitLine.GuiFactionTrait);
			}
		}
		else
		{
			this.selectedGuiTraits.Add(customTraitLine.GuiFactionTrait);
		}
		this.RefreshContent();
	}

	private void OnClickSelectedLine(MenuCustomTraitLine customTraitLine)
	{
		this.modified = true;
		this.selectedGuiTraits.Remove(customTraitLine.GuiFactionTrait);
		this.CheckListOfSelectedTraits();
		this.RefreshContent();
	}

	private int ComputePointsSpent()
	{
		int num = 0;
		for (int i = 0; i < this.preselectedGuiTraits.Count; i++)
		{
			num += this.preselectedGuiTraits[i].Cost;
		}
		for (int j = 0; j < this.selectedGuiTraits.Count; j++)
		{
			num += this.selectedGuiTraits[j].Cost;
		}
		return num;
	}

	private void InitalizeRootInStandardTraits()
	{
		Dictionary<string, List<GuiFactionTrait>> dictionary = new Dictionary<string, List<GuiFactionTrait>>();
		for (int i = 0; i < this.standardGuiTraits.Count; i++)
		{
			GuiFactionTrait guiFactionTrait = this.standardGuiTraits[i];
			if (!string.IsNullOrEmpty(guiFactionTrait.Root))
			{
				if (!dictionary.ContainsKey(guiFactionTrait.Root))
				{
					dictionary.Add(guiFactionTrait.Root, new List<GuiFactionTrait>());
				}
				dictionary[guiFactionTrait.Root].Add(guiFactionTrait);
			}
		}
		foreach (string key in dictionary.Keys)
		{
			for (int j = 0; j < dictionary[key].Count; j++)
			{
				dictionary[key][j].RootTraits = dictionary[key];
			}
		}
	}

	private void BuildListOfAvailableFamilies()
	{
		this.customTraitFamilies.Clear();
		this.customTraitFamilies.Add("All");
		this.customTraitFamilies.AddRange((from guiTrait in this.standardGuiTraits
		where !string.IsNullOrEmpty(guiTrait.Family)
		select guiTrait.Family).Distinct<string>());
		this.customTraitFamilies.Sort();
	}

	private void BuildListOfPreselectedTraits()
	{
		this.preselectedGuiTraits.Clear();
		this.preselectedGuiTraits = (from guiTrait in this.SelectedAffinity.SubGuiTraits
		where !guiTrait.IsHidden
		select guiTrait).ToList<GuiFactionTrait>();
		this.preselectedGuiTraits.Sort();
	}

	private void BuildListOfAvailableTraits()
	{
		FactionTrait[] factionTraits = (from guiTrait in this.selectedGuiTraits
		select guiTrait.FactionTrait).ToArray<FactionTrait>();
		this.availableGuiTraits.Clear();
		for (int i = 0; i < this.standardGuiTraits.Count; i++)
		{
			GuiFactionTrait guiTrait = this.standardGuiTraits[i];
			if (!this.selectedGuiTraits.Contains(guiTrait) && !guiTrait.IsHidden && guiTrait.ShowInCustom && (guiTrait.Family == this.selectedCustomTraitFamily || this.selectedCustomTraitFamily == "All"))
			{
				string empty = string.Empty;
				if (guiTrait.FactionTrait.CheckPrerequisites(this.SelectedAffinity.FactionTrait as FactionAffinity, factionTraits, ref empty))
				{
					if (!string.IsNullOrEmpty(guiTrait.Root))
					{
						GuiFactionTrait guiFactionTrait = this.selectedGuiTraits.FirstOrDefault((GuiFactionTrait match) => match.Root == guiTrait.Root);
						if (guiFactionTrait != null)
						{
							if (guiTrait.Level == guiFactionTrait.Level + 1)
							{
								this.availableGuiTraits.Add(guiTrait);
							}
						}
						else if (guiTrait.Level == 1)
						{
							this.availableGuiTraits.Add(guiTrait);
						}
					}
					else
					{
						this.availableGuiTraits.Add(guiTrait);
					}
				}
				else
				{
					string[] array = empty.Split(new char[]
					{
						','
					});
					for (int j = 0; j < array.Length; j++)
					{
						Diagnostics.Log("Trait {0} discarded because of {1}", new object[]
						{
							guiTrait.Title,
							array[j]
						});
					}
				}
			}
		}
	}

	private void CheckListOfSelectedTraits()
	{
		List<GuiFactionTrait> list = this.selectedGuiTraits;
		this.selectedGuiTraits = new List<GuiFactionTrait>();
		for (int i = 0; i < list.Count; i++)
		{
			string empty = string.Empty;
			if (list[i].FactionTrait.CheckPrerequisites(this.SelectedAffinity.FactionTrait as FactionAffinity, list.ConvertAll<FactionTrait>((GuiFactionTrait guiTrait) => guiTrait.FactionTrait).ToArray(), ref empty))
			{
				this.selectedGuiTraits.Add(list[i]);
			}
		}
	}

	private void CreateCustomTraitFamilyFilters()
	{
		this.CustomTraitFamiliesTable.Height = 0f;
		this.CustomTraitFamiliesTable.ReserveChildren(this.customTraitFamilies.Count, this.CustomTraitFamilyFilterTogglePrefab, "Family");
		this.CustomTraitFamiliesTable.RefreshChildrenIList<string>(this.customTraitFamilies, this.setupCustomTraitFamilyDelegate, true, false);
		this.CustomTraitFamiliesTable.ArrangeChildren();
		List<CustomTraitFamilyFilterToggle> children = this.CustomTraitFamiliesTable.GetChildren<CustomTraitFamilyFilterToggle>(true);
		if (children.Count > 0)
		{
			this.OnToggleFilter(children[0]);
		}
	}

	private void CreateFactionSqueleton()
	{
		this.Faction = new Faction();
		if (this.CreateMode)
		{
			this.Faction.Name = Guid.NewGuid().ToString();
			this.Faction.LocalizedName = AgeLocalizer.Instance.LocalizeString("%CustomFactionEnterName");
			this.Faction.LocalizedDescription = AgeLocalizer.Instance.LocalizeString("%CustomFactionEnterDescription");
			ISessionService service = Services.GetService<ISessionService>();
			this.Faction.Author = Steamworks.SteamAPI.SteamFriends.GetFriendPersonaName(service.Session.SteamIDUser);
		}
		else
		{
			Diagnostics.Assert(this.givenFaction != null);
			this.Faction.Name = this.givenFaction.Name;
			this.Faction.LocalizedName = this.givenFaction.LocalizedName;
			this.Faction.LocalizedDescription = this.givenFaction.LocalizedDescription;
			this.Faction.Author = this.givenFaction.Author;
			this.Faction.FileName = this.givenFaction.FileName;
		}
		if (this.givenFaction != null)
		{
			if (!this.givenFaction.IsRandom)
			{
				this.SelectedAffinity = this.affinityGuiTraits.Find((GuiFactionTrait guiTrait) => guiTrait.Name == this.givenFaction.Affinity.Name);
				this.SelectedAffinityMapping = this.affinityMappingGuiTraits.Find((GuiFactionTrait guiTrait) => guiTrait.Name == this.givenFaction.AffinityMapping.Name);
				int i;
				for (i = 0; i < this.givenFaction.Traits.Length; i++)
				{
					GuiFactionTrait guiFactionTrait = this.standardGuiTraits.Find((GuiFactionTrait guiTrait) => guiTrait.Name == this.givenFaction.Traits[i].Name);
					if (guiFactionTrait != null && !this.preselectedGuiTraits.Contains(guiFactionTrait))
					{
						this.selectedGuiTraits.Add(guiFactionTrait);
					}
				}
			}
			else
			{
				this.SelectedAffinity = this.affinityGuiTraits[0];
				this.SelectedAffinityMapping = this.affinityMappingGuiTraits[0];
			}
		}
	}

	private void OnConfirmCreateCustomFaction(object sender, MessagePanelResultEventArgs e)
	{
		if (e.Result == MessagePanelResult.Yes)
		{
			this.CreateCustomFaction();
		}
	}

	private void CreateCustomFaction()
	{
		this.Faction.Affinity = new XmlNamedReference(this.selectedAffinity.Name);
		this.Faction.AffinityMapping = new XmlNamedReference(this.SelectedAffinityMapping.Name);
		this.Faction.TraitReferences = (from guiTrait in this.selectedGuiTraits
		select new XmlNamedReference(guiTrait.Name)).ToArray<XmlNamedReference>();
		this.Faction.LocalizedName = this.NameTextField.AgePrimitiveLabel.Text.Trim();
		this.Faction.LocalizedDescription = this.DescriptionTextField.AgePrimitiveLabel.Text.Trim();
		this.Faction.Author = this.AuthorTextField.AgePrimitiveLabel.Text.Trim();
		this.Faction.IsCustom = true;
		this.Faction.IsStandard = false;
		List<GuiError> list = new List<GuiError>();
		if (!GuiFaction.IsValidCustomFaction(this.Faction, list))
		{
			string message = string.Join("\r\n", (from guiError in list
			select guiError.ToString()).ToArray<string>());
			MessagePanel.Instance.Show(message, "%CustomFactionInvalidCustomFactionTitle", MessagePanelButtons.Ok, null, MessagePanelType.WARNING, new MessagePanelButton[0]);
			this.HandleCancelRequest();
			return;
		}
		if (string.IsNullOrEmpty(this.Faction.FileName))
		{
			string path = System.IO.Path.Combine(Amplitude.Unity.Framework.Application.GameDirectory, "Custom Factions");
			DirectoryInfo directoryInfo = new DirectoryInfo(path);
			if (!directoryInfo.Exists)
			{
				directoryInfo.Create();
			}
			string text = this.Faction.LocalizedName;
			char[] invalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();
			foreach (char oldChar in invalidFileNameChars)
			{
				text = text.Replace(oldChar, '@');
			}
			this.Faction.FileName = System.IO.Path.Combine(directoryInfo.FullName, text);
			this.Faction.FileName = System.IO.Path.ChangeExtension(this.Faction.FileName, ".xml");
		}
		if (!string.IsNullOrEmpty(this.Faction.FileName))
		{
			ISerializationService service = Services.GetService<ISerializationService>();
			if (service != null)
			{
				XmlSerializer xmlSerializer = service.GetXmlSerializer<Faction>();
				if (xmlSerializer != null)
				{
					using (Stream stream = File.Open(this.Faction.FileName, FileMode.Create))
					{
						xmlSerializer.Serialize(stream, this.Faction);
					}
				}
			}
		}
		IDatabase<Faction> database = Databases.GetDatabase<Faction>(false);
		database.Touch(this.Faction);
		base.GuiService.GetGuiPanel<MenuNewGameScreen>().OnCustomFactionChanged(this.Faction);
		base.GuiService.GetGuiPanel<MenuFactionScreen>().Show(new object[]
		{
			this.empireIndex,
			this.Faction
		});
		this.Faction = null;
	}

	private void OverrideAffinitiesDropListTooltips()
	{
		AgeControlPopup componentInChildren = this.AffinityDropList.GetComponentInChildren<AgeControlPopup>();
		if (componentInChildren != null)
		{
			List<AgeTransform> children = componentInChildren.AgeTransform.GetChildren();
			for (int i = 0; i < children.Count; i++)
			{
				if (i < this.affinityGuiTraits.Count)
				{
					GuiFactionTrait guiFactionTrait = this.affinityGuiTraits[i];
					for (int j = 0; j < guiFactionTrait.SubGuiTraits.Count; j++)
					{
						GuiFactionTrait guiFactionTrait2 = guiFactionTrait.SubGuiTraits[j];
						if (guiFactionTrait2.Type == GuiFactionTrait.TraitType.Affinity)
						{
							AgeTooltip component = children[i].GetComponent<AgeTooltip>();
							if (component != null)
							{
								StaticString relatedAffinity = StaticString.Empty;
								if (this.SelectedAffinity != null)
								{
									relatedAffinity = this.SelectedAffinity.Name;
								}
								guiFactionTrait2.GenerateTooltip(component, relatedAffinity);
								break;
							}
						}
					}
				}
			}
		}
	}

	private void OverrideSelectedAffinityTooltip()
	{
		AgeControlPopup componentInChildren = this.AffinityDropList.GetComponentInChildren<AgeControlPopup>();
		if (componentInChildren != null)
		{
			List<AgeTransform> children = componentInChildren.AgeTransform.GetChildren();
			AgeTransform ageTransform = children[this.AffinityDropList.SelectedItem];
			this.AffinityDropList.AgeTransform.AgeTooltip.Class = ageTransform.AgeTooltip.Class;
			this.AffinityDropList.AgeTransform.AgeTooltip.ClientData = ageTransform.AgeTooltip.ClientData;
			this.AffinityDropList.AgeTransform.AgeTooltip.Content = ageTransform.AgeTooltip.Content;
		}
	}

	private void RefreshButtons()
	{
		if (this.ValidateName())
		{
			if (this.totalPoints > this.SelectedAffinity.MaxPoints)
			{
				this.ValidateButton.AgeTransform.Enable = false;
				this.ValidateButton.AgeTransform.AgeTooltip.Content = "%CustomFactionTooManyPointsDescription";
			}
			else
			{
				this.ValidateButton.AgeTransform.Enable = true;
				this.ValidateButton.AgeTransform.AgeTooltip.Content = "%ValidateDescription";
			}
		}
		if (this.profanityError != string.Empty)
		{
			this.ValidateButton.AgeTransform.Enable = false;
			this.ValidateButton.AgeTransform.AgeTooltip.Content = "%Failure" + this.profanityError + "Description";
		}
	}

	private void SetupCustomTraitFamily(AgeTransform tableItem, string customTraitFamily, int index)
	{
		CustomTraitFamilyFilterToggle component = tableItem.GetComponent<CustomTraitFamilyFilterToggle>();
		if (component == null)
		{
			Diagnostics.LogError("In the MenuCustomFactionScreen, trying to refresh a table item that is not a CustomTraitFamilyFilterToggle");
			return;
		}
		component.SetContent(customTraitFamily, "CustomTraitFamily", base.gameObject);
	}

	private void SetupAvailableGuiTrait(AgeTransform tableItem, GuiFactionTrait guiFactionTrait, int index)
	{
		MenuCustomTraitLine component = tableItem.GetComponent<MenuCustomTraitLine>();
		if (component == null)
		{
			Diagnostics.LogError("In the MenuCustomFactionScreen, trying to refresh a table item that is not a MenuCustomTraitLine");
			return;
		}
		if (index % 20 == 0)
		{
			tableItem.StartNewMesh = true;
		}
		component.RefreshContent(guiFactionTrait, this.SelectedAffinity.Name, base.gameObject, "OnClickAvailableLine");
	}

	private void SetupSelectedGuiTrait(AgeTransform tableItem, GuiFactionTrait guiFactionTrait, int index)
	{
		MenuCustomTraitLine component = tableItem.GetComponent<MenuCustomTraitLine>();
		if (component == null)
		{
			Diagnostics.LogError("In the MenuJoinGameScreen, trying to refresh a table item that is not a GameSessionLine");
			return;
		}
		if (index % 20 == 0)
		{
			tableItem.StartNewMesh = true;
		}
		component.RefreshContent(guiFactionTrait, this.SelectedAffinity.Name, base.gameObject, "OnClickSelectedLine");
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

	private const string CustomTraitFamilyAll = "All";

	private string profanityError;

	private UnityEngine.Coroutine profanityFilterCoroutine;

	private Color invalidColor;

	public Transform CustomTraitFamilyFilterTogglePrefab;

	public Transform CustomTraitPrefab;

	public AgePrimitiveImage MoodImage;

	public AgePrimitiveImage PortraitImage;

	public AgeControlTextField NameTextField;

	public AgePrimitiveImage NameSelectionFrame;

	public AgeControlTextField AuthorTextField;

	public AgeControlDropList AffinityDropList;

	public AgeControlDropList AffinityMappingDropList;

	public AgeControlTextArea DescriptionTextField;

	public AgeTransform CustomTraitFamiliesTable;

	public SortButtonsContainer AvailableTraitsSortsContainer;

	public SortButtonsContainer SelectedTraitsSortsContainer;

	public AgeTransform AvailableTraitsTable;

	public AgeControlScrollView AvailableTraitsScrollView;

	public AgeTransform PreselectedTraitsTable;

	public AgeTransform SelectedTraitsTable;

	public AgeControlScrollView SelectedTraitsScrollView;

	public AgePrimitiveLabel PointsCounterLabel;

	public AgePrimitiveImage PointsCounterBackground;

	public AgeControlButton ValidateButton;

	public Color[] PointsCounterBackgroundColors;

	public Faction Faction;

	public string ViewportLayer = "DiplomacyNotification";

	private GuiFactionTrait selectedAffinity;

	private GuiFactionTrait selectedAffinityMapping;

	private string selectedCustomTraitFamily;

	private int totalPoints;

	private int empireIndex;

	private Faction givenFaction;

	private bool modified;

	private List<GuiFactionTrait> standardGuiTraits = new List<GuiFactionTrait>();

	private List<GuiFactionTrait> affinityGuiTraits = new List<GuiFactionTrait>();

	private List<GuiFactionTrait> affinityMappingGuiTraits = new List<GuiFactionTrait>();

	private List<GuiFactionTrait> availableGuiTraits = new List<GuiFactionTrait>();

	private List<GuiFactionTrait> preselectedGuiTraits = new List<GuiFactionTrait>();

	private List<GuiFactionTrait> selectedGuiTraits = new List<GuiFactionTrait>();

	private List<string> customTraitFamilies = new List<string>();

	private AgeTransform.RefreshTableItem<GuiFactionTrait> setupAvailableGuiTraitDelegate;

	private AgeTransform.RefreshTableItem<GuiFactionTrait> setupSelectedGuiTraitDelegate;

	private AgeTransform.RefreshTableItem<string> setupCustomTraitFamilyDelegate;

	private DiplomaticNegotiationViewport diplomaticNegotiationViewport;
}
