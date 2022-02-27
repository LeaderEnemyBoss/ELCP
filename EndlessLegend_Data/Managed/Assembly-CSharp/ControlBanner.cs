using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Input;
using UnityEngine;

public class ControlBanner : GuiPlayerControllerPanel
{
	private DepartmentOfScience DepartmentOfScience { get; set; }

	public override void Bind(global::Empire empire)
	{
		base.Bind(empire);
		this.DepartmentOfScience = base.Empire.GetAgency<DepartmentOfScience>();
		this.keyMapperService = Services.GetService<IKeyMappingService>();
		Diagnostics.Assert(this.keyMapperService != null);
		if (base.IsVisible)
		{
			this.UpdateButtonsAvailability();
		}
	}

	public override void Unbind()
	{
		this.DepartmentOfScience = null;
		base.Unbind();
	}

	public override void Show(params object[] parameters)
	{
		base.Show(parameters);
		this.UpdateButtonsAvailability();
	}

	public void OnAcademyCB(GameObject control)
	{
		if (base.GuiService.GetGuiPanel<GameAcademyScreen>().IsVisible)
		{
			base.GuiService.GetGuiPanel<GameWorldScreen>().Show(new object[0]);
		}
		else
		{
			base.GuiService.Hide(typeof(CurrentQuestPanel));
			base.GuiService.GetGuiPanel<GameAcademyScreen>().Show(new object[0]);
		}
	}

	public void OnCityListCB(GameObject control)
	{
		if (base.GuiService.GetGuiPanel<GameCityListScreen>().IsVisible)
		{
			base.GuiService.GetGuiPanel<GameWorldScreen>().Show(new object[0]);
		}
		else
		{
			base.GuiService.Hide(typeof(CurrentQuestPanel));
			if (base.GuiService.GetGuiPanel<GameWorldScreen>().CurrentMetaPanel is MetaPanelCity)
			{
				base.GuiService.GetGuiPanel<GameCityListScreen>().Show(new object[]
				{
					(base.GuiService.GetGuiPanel<GameWorldScreen>().CurrentMetaPanel as MetaPanelCity).City
				});
			}
			else
			{
				base.GuiService.GetGuiPanel<GameCityListScreen>().Show(new object[0]);
			}
		}
	}

	public void OnDiplomacyCB(GameObject control)
	{
		if (base.GuiService.GetGuiPanel<GameDiplomacyScreen>().IsVisible)
		{
			base.GuiService.GetGuiPanel<GameWorldScreen>().Show(new object[0]);
		}
		else
		{
			base.GuiService.Hide(typeof(CurrentQuestPanel));
			base.GuiService.GetGuiPanel<GameDiplomacyScreen>().Show(new object[0]);
		}
	}

	public void OnEmpireCB(GameObject control)
	{
		if (base.GuiService.GetGuiPanel<GameEmpireScreen>().IsVisible)
		{
			base.GuiService.GetGuiPanel<GameWorldScreen>().Show(new object[0]);
		}
		else
		{
			base.GuiService.Hide(typeof(CurrentQuestPanel));
			base.GuiService.GetGuiPanel<GameEmpireScreen>().Show(new object[0]);
		}
	}

	public void OnMarketplaceCB(GameObject control)
	{
		if (base.GuiService.GetGuiPanel<GameMarketplaceScreen>().IsVisible)
		{
			base.GuiService.GetGuiPanel<GameWorldScreen>().Show(new object[0]);
		}
		else
		{
			base.GuiService.Hide(typeof(CurrentQuestPanel));
			base.GuiService.GetGuiPanel<GameMarketplaceScreen>().Show(new object[0]);
		}
	}

	public void OnEspionageplaceCB(GameObject control)
	{
		if (base.GuiService.GetGuiPanel<GameEspionageScreen>().IsVisible)
		{
			base.GuiService.GetGuiPanel<GameWorldScreen>().Show(new object[0]);
		}
		else
		{
			base.GuiService.Hide(typeof(CurrentQuestPanel));
			base.GuiService.GetGuiPanel<GameEspionageScreen>().Show(new object[0]);
		}
	}

	public void OnMilitaryCB(GameObject control)
	{
		if (base.GuiService.GetGuiPanel<GameMilitaryScreen>().IsVisible)
		{
			base.GuiService.GetGuiPanel<GameWorldScreen>().Show(new object[0]);
		}
		else
		{
			base.GuiService.Hide(typeof(CurrentQuestPanel));
			base.GuiService.GetGuiPanel<GameMilitaryScreen>().Show(new object[0]);
		}
	}

	public void OnQuestCB(GameObject control)
	{
		if (base.GuiService.GetGuiPanel<GameQuestScreen>().IsVisible)
		{
			base.GuiService.GetGuiPanel<GameWorldScreen>().Show(new object[0]);
		}
		else
		{
			base.GuiService.GetGuiPanel<GameQuestScreen>().Show(new object[0]);
		}
	}

	public void OnResearchCB(GameObject control)
	{
		if (base.GuiService.GetGuiPanel<GameResearchScreen>().IsVisible)
		{
			base.GuiService.GetGuiPanel<GameWorldScreen>().Show(new object[0]);
		}
		else
		{
			base.GuiService.Hide(typeof(CurrentQuestPanel));
			base.GuiService.GetGuiPanel<GameResearchScreen>().Show(new object[0]);
		}
	}

	public void OnHideScreen(GameScreenType type)
	{
		if (this.toggleMap.ContainsKey(type) && this.toggleMap[type] != null)
		{
			this.toggleMap[type].State = false;
		}
	}

	public void OnShowScreen(GameScreenType type)
	{
		if (this.toggleMap.ContainsKey(type) && this.toggleMap[type] != null)
		{
			this.toggleMap[type].State = true;
		}
	}

	protected override IEnumerator OnHide(bool instant)
	{
		this.doCheckShortcut = false;
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
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		this.doCheckShortcut = true;
		base.StartCoroutine(this.CheckShortcut());
		yield break;
	}

	protected override void OnUnloadGame(IGame game)
	{
		IEventService service = Services.GetService<IEventService>();
		if (service != null)
		{
			service.EventRaise -= this.EventService_EventRaise;
		}
		base.OnUnloadGame(game);
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		this.toggleMap = new Dictionary<GameScreenType, AgeControlToggle>();
		this.toggleMap.Add(GameScreenType.Empire, this.EmpireToggle);
		this.toggleMap.Add(GameScreenType.CityList, this.CityListToggle);
		this.toggleMap.Add(GameScreenType.Research, this.ResearchToggle);
		this.toggleMap.Add(GameScreenType.Academy, this.AcademyToggle);
		this.toggleMap.Add(GameScreenType.Quest, this.QuestToggle);
		this.toggleMap.Add(GameScreenType.Military, this.MilitaryToggle);
		this.toggleMap.Add(GameScreenType.Diplomacy, this.DiplomacyToggle);
		this.toggleMap.Add(GameScreenType.Marketplace, this.MarketplaceToggle);
		this.toggleMap.Add(GameScreenType.Espionage, this.EspionageplaceToggle);
		this.navigationEnabledScreens = new GuiPlayerControllerScreen[]
		{
			base.GuiService.GetGuiPanel<GameEmpireScreen>(),
			base.GuiService.GetGuiPanel<GameCityListScreen>(),
			base.GuiService.GetGuiPanel<GameResearchScreen>(),
			base.GuiService.GetGuiPanel<GameQuestScreen>(),
			base.GuiService.GetGuiPanel<GameAcademyScreen>(),
			base.GuiService.GetGuiPanel<GameMilitaryScreen>(),
			base.GuiService.GetGuiPanel<GameDiplomacyScreen>(),
			base.GuiService.GetGuiPanel<GameMarketplaceScreen>(),
			base.GuiService.GetGuiPanel<GameEspionageScreen>(),
			base.GuiService.GetGuiPanel<GameStatusScreen>(),
			base.GuiService.GetGuiPanel<GameAltarOfAurigaScreen>()
		};
		yield break;
	}

	protected override void OnUnload()
	{
		this.toggleMap.Clear();
		this.toggleMap = null;
		base.OnUnload();
	}

	private void EventService_EventRaise(object sender, EventRaiseEventArgs e)
	{
		if (!base.IsVisible)
		{
			return;
		}
		EventTechnologyEnded eventTechnologyEnded = e.RaisedEvent as EventTechnologyEnded;
		if (eventTechnologyEnded != null)
		{
			this.UpdateButtonsAvailability();
		}
	}

	private void UpdateButtonsAvailability()
	{
		if (this.DepartmentOfScience.HaveResearchedAtLeastOneTradeTechnology())
		{
			this.MarketplaceToggle.AgeTransform.Enable = true;
			this.MarketplaceToggle.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%MarketplaceScreenShortcutDescription");
		}
		else
		{
			this.MarketplaceToggle.AgeTransform.Enable = false;
			string text = string.Empty;
			IGuiPanelHelper guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
			Diagnostics.Assert(guiPanelHelper != null, "Unable to access GuiPanelHelper");
			for (int i = 0; i < this.DepartmentOfScience.TechnologiesToUnlockMarketplace.Length; i++)
			{
				GuiElement guiElement;
				if (guiPanelHelper.TryGetGuiElement(this.DepartmentOfScience.TechnologiesToUnlockMarketplace[i], out guiElement))
				{
					text = text + "\n - " + AgeLocalizer.Instance.LocalizeString(guiElement.Title);
				}
			}
			this.MarketplaceToggle.AgeTransform.AgeTooltip.Content = string.Format(AgeLocalizer.Instance.LocalizeString("%MarketplaceScreenShortcutDisabledDescription"), text);
		}
		bool isActivated = TutorialManager.IsActivated;
		this.EmpireToggle.AgeTransform.Enable = (!isActivated || this.DepartmentOfScience.GetTechnologyState("TechnologyDefinitionTutorialUnlockEmpire") == DepartmentOfScience.ConstructibleElement.State.Researched);
		this.ResearchToggle.AgeTransform.Enable = (!isActivated || this.DepartmentOfScience.GetTechnologyState("TechnologyDefinitionTutorialUnlockResearch") == DepartmentOfScience.ConstructibleElement.State.Researched);
		this.QuestToggle.AgeTransform.Enable = (!isActivated || this.DepartmentOfScience.GetTechnologyState("TechnologyDefinitionTutorialUnlockQuest") == DepartmentOfScience.ConstructibleElement.State.Researched);
		this.CityListToggle.AgeTransform.Enable = !isActivated;
		this.AcademyToggle.AgeTransform.Enable = (!isActivated || this.DepartmentOfScience.GetTechnologyState("TechnologyDefinitionTutorialUnlockAcademy") == DepartmentOfScience.ConstructibleElement.State.Researched);
		this.MilitaryToggle.AgeTransform.Enable = (!isActivated || this.DepartmentOfScience.GetTechnologyState("TechnologyDefinitionTutorialUnlockMilitary") == DepartmentOfScience.ConstructibleElement.State.Researched);
		this.DiplomacyToggle.AgeTransform.Enable = !isActivated;
		this.EspionageplaceToggle.AgeTransform.Visible = false;
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		if (service != null && service.IsShared(DownloadableContent11.ReadOnlyName))
		{
			this.EspionageplaceToggle.AgeTransform.Visible = true;
			this.EspionageplaceToggle.AgeTransform.Enable = !isActivated;
		}
		if (this.EspionageplaceToggle.AgeTransform.Visible)
		{
			this.ControlTable.HorizontalSpacing = this.WideSpacing * AgeUtils.CurrentUpscaleFactor();
		}
		else
		{
			this.ControlTable.HorizontalSpacing = this.StandardSpacing * AgeUtils.CurrentUpscaleFactor();
		}
	}

	private IEnumerator CheckShortcut()
	{
		while (this.doCheckShortcut)
		{
			if (Amplitude.Unity.Gui.GuiModalPanel.GuiModalManager.CurrentModalPanel == null)
			{
				if (GuiPlayerControllerScreen.LastSelection != null && GuiPlayerControllerScreen.LastSelection.IsVisible && GuiPlayerControllerScreen.LastSelection is GameAcademyScreen)
				{
					if (this.keyMapperService.GetKeyDown(KeyAction.ControlBindingsHeroRight))
					{
						((GameAcademyScreen)GuiPlayerControllerScreen.LastSelection).OnNextHero(null, 1);
					}
					else if (this.keyMapperService.GetKeyDown(KeyAction.ControlBindingsHeroRight))
					{
						((GameAcademyScreen)GuiPlayerControllerScreen.LastSelection).OnNextHero(null, -1);
					}
				}
				if (this.keyMapperService.GetKeyDown(KeyAction.ControlBindingsControlBanner))
				{
					yield return base.StartCoroutine(this.OnNextItem(null, 0, true));
				}
				else if (GuiPlayerControllerScreen.LastSelection != null && GuiPlayerControllerScreen.LastSelection.IsVisible)
				{
					if (this.keyMapperService.GetKeyDown(KeyAction.ControlBindingsNextItem))
					{
						yield return base.StartCoroutine(this.OnNextItem(null, 1, false));
					}
					else if (this.keyMapperService.GetKeyDown(KeyAction.ControlBindingsPreviousItem))
					{
						yield return base.StartCoroutine(this.OnNextItem(null, -1, false));
					}
				}
				if (this.keyMapperService.GetKeyDown(KeyAction.ControlBindingsEmpire) && this.EmpireToggle.AgeTransform.Enable)
				{
					this.OnEmpireCB(this.EmpireToggle.gameObject);
				}
				else if (this.keyMapperService.GetKeyDown(KeyAction.ControlBindingsCityList) && this.CityListToggle.AgeTransform.Enable)
				{
					this.OnCityListCB(this.CityListToggle.gameObject);
				}
				else if (this.keyMapperService.GetKeyDown(KeyAction.ControlBindingsResearch) && this.ResearchToggle.AgeTransform.Enable)
				{
					this.OnResearchCB(this.ResearchToggle.gameObject);
				}
				else if (this.keyMapperService.GetKeyDown(KeyAction.ControlBindingsQuest) && this.QuestToggle.AgeTransform.Enable)
				{
					this.OnQuestCB(this.QuestToggle.gameObject);
				}
				else if (this.keyMapperService.GetKeyDown(KeyAction.ControlBindingsAcademy) && this.AcademyToggle.AgeTransform.Enable)
				{
					this.OnAcademyCB(this.AcademyToggle.gameObject);
				}
				else if (this.keyMapperService.GetKeyDown(KeyAction.ControlBindingsMilitary) && this.MilitaryToggle.AgeTransform.Enable)
				{
					this.OnMilitaryCB(this.MilitaryToggle.gameObject);
				}
				else if (this.keyMapperService.GetKeyDown(KeyAction.ControlBindingsDiplomacy) && this.DiplomacyToggle.AgeTransform.Enable)
				{
					this.OnDiplomacyCB(this.DiplomacyToggle.gameObject);
				}
				else if (this.keyMapperService.GetKeyDown(KeyAction.ControlBindingsMarket) && this.MarketplaceToggle.AgeTransform.Enable)
				{
					this.OnMarketplaceCB(this.MarketplaceToggle.gameObject);
				}
				else if (this.keyMapperService.GetKeyDown(KeyAction.ControlBindingsEspionage) && this.EspionageplaceToggle.AgeTransform.Enable)
				{
					IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
					if (downloadableContentService != null && downloadableContentService.IsShared(DownloadableContent11.ReadOnlyName))
					{
						this.OnEspionageplaceCB(this.EspionageplaceToggle.gameObject);
					}
				}
				else if (this.keyMapperService.GetKeyDown(KeyAction.ControlBindingsGameStatus) && this.EmpireToggle.AgeTransform.Enable)
				{
					if (base.GuiService.GetGuiPanel<GameStatusScreen>().IsVisible)
					{
						base.GuiService.GetGuiPanel<GameWorldScreen>().Show(new object[0]);
					}
					else
					{
						if (!base.GuiService.GetGuiPanel<GameEmpireScreen>().IsVisible)
						{
							base.GuiService.Hide(typeof(CurrentQuestPanel));
							base.GuiService.GetGuiPanel<GameEmpireScreen>().Show(new object[0]);
						}
						yield return null;
						base.GuiService.GetGuiPanel<GameStatusScreen>().Show(new object[0]);
					}
				}
			}
			yield return null;
		}
		yield break;
	}

	private IEnumerator OnNextItem(GameObject obj, int direction = 1, bool ignoreMetapanels = false)
	{
		if (!ignoreMetapanels)
		{
			MetaPanelCity metaPanelCity = base.GuiService.GetGuiPanel<MetaPanelCity>();
			if (metaPanelCity.IsVisible)
			{
				yield break;
			}
			MetaPanelArmy metaPanelArmy = base.GuiService.GetGuiPanel<MetaPanelArmy>();
			if (metaPanelArmy.IsVisible)
			{
				yield break;
			}
		}
		IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
		int lastIndex = (this.navigationEnabledScreens.Length - direction) % this.navigationEnabledScreens.Length;
		if (GuiPlayerControllerScreen.LastSelection != null)
		{
			int indexOf = Array.IndexOf<GuiPlayerControllerScreen>(this.navigationEnabledScreens, GuiPlayerControllerScreen.LastSelection);
			if (indexOf != -1)
			{
				if (GuiPlayerControllerScreen.LastSelection.IsVisible)
				{
					lastIndex = indexOf;
				}
				else
				{
					lastIndex = (indexOf + this.navigationEnabledScreens.Length - direction) % this.navigationEnabledScreens.Length;
				}
			}
		}
		GuiPlayerControllerScreen nextNavigableScreen = null;
		while (nextNavigableScreen == null)
		{
			lastIndex = (lastIndex + this.navigationEnabledScreens.Length + direction) % this.navigationEnabledScreens.Length;
			GuiPlayerControllerScreen nextPotentialNavigableScreen = this.navigationEnabledScreens[lastIndex];
			if (!(nextPotentialNavigableScreen is GameMarketplaceScreen) || this.MarketplaceToggle.AgeTransform.Enable)
			{
				if (nextPotentialNavigableScreen is GameEspionageScreen)
				{
					if (downloadableContentService == null || !downloadableContentService.IsShared(DownloadableContent11.ReadOnlyName))
					{
						continue;
					}
				}
				if (nextPotentialNavigableScreen is GameAltarOfAurigaScreen)
				{
					if (downloadableContentService == null || !downloadableContentService.IsShared(DownloadableContent13.ReadOnlyName))
					{
						continue;
					}
				}
				nextNavigableScreen = nextPotentialNavigableScreen;
			}
		}
		if (nextNavigableScreen != null)
		{
			ControlBanner.LastPendingSelection = nextNavigableScreen;
			if (nextNavigableScreen is GameEmpireScreen && this.EmpireToggle.AgeTransform.Enable)
			{
				this.OnEmpireCB(this.EmpireToggle.gameObject);
			}
			else if (nextNavigableScreen is GameCityListScreen && this.CityListToggle.AgeTransform.Enable)
			{
				this.OnCityListCB(this.CityListToggle.gameObject);
			}
			else if (nextNavigableScreen is GameResearchScreen && this.ResearchToggle.AgeTransform.Enable)
			{
				this.OnResearchCB(this.ResearchToggle.gameObject);
			}
			else if (nextNavigableScreen is GameQuestScreen && this.QuestToggle.AgeTransform.Enable)
			{
				this.OnQuestCB(this.QuestToggle.gameObject);
			}
			else if (nextNavigableScreen is GameAcademyScreen && this.AcademyToggle.AgeTransform.Enable)
			{
				this.OnAcademyCB(this.AcademyToggle.gameObject);
			}
			else if (nextNavigableScreen is GameMilitaryScreen && this.MilitaryToggle.AgeTransform.Enable)
			{
				this.OnMilitaryCB(this.MilitaryToggle.gameObject);
			}
			else if (nextNavigableScreen is GameDiplomacyScreen && this.DiplomacyToggle.AgeTransform.Enable)
			{
				this.OnDiplomacyCB(this.DiplomacyToggle.gameObject);
			}
			else if (nextNavigableScreen is GameMarketplaceScreen && this.MarketplaceToggle.AgeTransform.Enable)
			{
				this.OnMarketplaceCB(this.MarketplaceToggle.gameObject);
			}
			else if (nextNavigableScreen is GameEspionageScreen && this.EspionageplaceToggle.AgeTransform.Enable)
			{
				this.OnEspionageplaceCB(this.EspionageplaceToggle.gameObject);
			}
			else if (nextNavigableScreen is GameStatusScreen && this.EmpireToggle.AgeTransform.Enable)
			{
				if (!base.GuiService.GetGuiPanel<GameEmpireScreen>().IsVisible)
				{
					base.GuiService.Hide(typeof(CurrentQuestPanel));
					base.GuiService.GetGuiPanel<GameEmpireScreen>().Show(new object[0]);
				}
				yield return null;
				base.GuiService.GetGuiPanel<GameStatusScreen>().Show(new object[0]);
			}
			else if (nextNavigableScreen is GameAltarOfAurigaScreen)
			{
				base.GuiService.GetGuiPanel<GameAltarOfAurigaScreen>().Show(new object[0]);
			}
			else
			{
				ControlBanner.LastPendingSelection = null;
			}
		}
		yield break;
	}

	public AgeTransform ControlTable;

	public float StandardSpacing;

	public float WideSpacing;

	public AgeControlToggle EmpireToggle;

	public AgeControlToggle CityListToggle;

	public AgeControlToggle ResearchToggle;

	public AgeControlToggle AcademyToggle;

	public AgeControlToggle QuestToggle;

	public AgeControlToggle MilitaryToggle;

	public AgeControlToggle DiplomacyToggle;

	public AgeControlToggle MarketplaceToggle;

	public AgeControlToggle EspionageplaceToggle;

	internal static GuiPlayerControllerScreen LastPendingSelection;

	private Dictionary<GameScreenType, AgeControlToggle> toggleMap;

	private bool doCheckShortcut;

	private IKeyMappingService keyMapperService;

	private GuiPlayerControllerScreen[] navigationEnabledScreens;
}
