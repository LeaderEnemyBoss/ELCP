using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using UnityEngine;

public class GameCityListScreen : GuiPlayerControllerScreen
{
	public GameCityListScreen()
	{
		base.EnableCaptureBackgroundOnShow = true;
		base.EnableMenuAudioLayer = true;
		base.EnableNavigation = true;
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

	private Garrison CurrentSelection { get; set; }

	public override void Bind(global::Empire empire)
	{
		if (empire == null)
		{
			throw new ArgumentNullException("empire");
		}
		base.Bind(empire);
		this.CityListPanel.Bind(empire, base.gameObject);
		this.VillageListPanel.Bind(empire, base.gameObject);
		this.OceanicRegionListPanel.Bind(empire, base.gameObject);
		this.InfectedCitiesListPanel.Bind(empire, base.gameObject);
		this.KaijuListPanel.Bind(empire, base.gameObject);
		base.NeedRefresh = true;
	}

	public override bool HandleCancelRequest()
	{
		bool flag = false;
		if (Input.GetMouseButtonDown(1))
		{
			flag = base.GuiService.GetGuiPanel<NotificationListPanel>().HandleRightClick();
		}
		if (!flag && this.cityOptionsPanel.IsVisible)
		{
			this.cityOptionsPanel.Hide(false);
			this.cityQueuePanel.Hide(false);
			flag = true;
		}
		if (!flag && this.cityGarrisonPanel.IsVisible)
		{
			this.cityGarrisonPanel.Hide(false);
			flag = true;
		}
		if (!flag && this.villageGarrisonPanel.IsVisible)
		{
			this.villageGarrisonPanel.Hide(false);
			flag = true;
		}
		if (!flag && this.fortressGarrisonPanel.IsVisible)
		{
			this.fortressGarrisonPanel.Hide(false);
			flag = true;
		}
		if (!flag && this.kaijuGarrisonPanel.IsVisible)
		{
			this.kaijuGarrisonPanel.Hide(false);
			flag = true;
		}
		if (!flag && this.CityWorkersPanel.IsVisible)
		{
			this.CityWorkersPanel.Hide(false);
			flag = true;
		}
		if (!flag)
		{
			base.GuiService.GetGuiPanel<GameWorldScreen>().Show(new object[0]);
			flag = true;
		}
		return flag;
	}

	public override bool HandleDownRequest()
	{
		return this.CityListPanel.HandleDownRequest();
	}

	public override bool HandleUpRequest()
	{
		return this.CityListPanel.HandleUpRequest();
	}

	public override void RefreshContent()
	{
		if (base.Empire == null)
		{
			return;
		}
		base.RefreshContent();
		this.RefreshTableVisibility();
		this.CityListPanel.RefreshContent();
		if (this.VillageListPanel.IsVisible)
		{
			this.VillageListPanel.RefreshContent();
		}
		if (this.OceanicRegionListPanel.IsVisible)
		{
			this.OceanicRegionListPanel.RefreshContent();
		}
		if (this.InfectedCitiesListPanel.IsVisible)
		{
			this.InfectedCitiesListPanel.RefreshContent();
		}
		if (this.KaijuListPanel.IsVisible)
		{
			this.KaijuListPanel.RefreshContent();
		}
		this.RefreshTableLayout();
		this.ListsScrollView.OnPositionRecomputed();
		if (this.cityOptionsPanel.IsVisible)
		{
			this.cityOptionsPanel.RefreshContent();
		}
		if (this.cityQueuePanel.IsVisible)
		{
			this.cityQueuePanel.RefreshContent();
		}
		if (this.cityGarrisonPanel.IsVisible)
		{
			this.cityGarrisonPanel.RefreshContent();
		}
		if (this.CityWorkersPanel.IsVisible)
		{
			this.CityWorkersPanel.RefreshContent();
		}
		if (this.villageGarrisonPanel.IsVisible)
		{
			this.villageGarrisonPanel.RefreshContent();
		}
		if (this.fortressGarrisonPanel.IsVisible)
		{
			this.fortressGarrisonPanel.RefreshContent();
		}
		if (this.kaijuGarrisonPanel.IsVisible)
		{
			this.kaijuGarrisonPanel.RefreshContent();
		}
	}

	public override void Unbind()
	{
		this.CityListPanel.Unbind();
		this.VillageListPanel.Unbind();
		this.OceanicRegionListPanel.Unbind();
		this.InfectedCitiesListPanel.Unbind();
		this.KaijuListPanel.Unbind();
		this.HideSubPanels(true);
		this.UnbindSubPanels();
		base.Unbind();
	}

	protected override IEnumerator OnHide(bool instant)
	{
		base.NeedRefresh = false;
		this.CityListPanel.Hide(false);
		this.VillageListPanel.Hide(false);
		this.OceanicRegionListPanel.Hide(false);
		this.InfectedCitiesListPanel.Hide(false);
		this.KaijuListPanel.Hide(false);
		this.cityOptionsPanel.Hide(false);
		this.cityQueuePanel.Hide(false);
		this.cityGarrisonPanel.Hide(false);
		this.villageGarrisonPanel.Hide(false);
		this.fortressGarrisonPanel.Hide(false);
		this.kaijuGarrisonPanel.Hide(false);
		this.CityWorkersPanel.Hide(false);
		base.GuiService.GetGuiPanel<ControlBanner>().OnHideScreen(GameScreenType.CityList);
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		base.UseRefreshLoop = true;
		this.cityOptionsPanel = Amplitude.Unity.Gui.GuiPanel.Instanciate<CityOptionsPanel>(this.CityOptionsFrame, this.CityOptionsPanelPrefab, null);
		this.cityOptionsPanel.PositionModifier = this.CityOptionsFrame.GetComponent<AgeModifierPosition>();
		this.cityOptionsPanel.Load();
		this.cityOptionsPanel.AuthorizeExpand = false;
		this.cityQueuePanel = Amplitude.Unity.Gui.GuiPanel.Instanciate<CityQueuePanel>(this.CityQueueFrame, this.CityQueuePanelPrefab, null);
		this.cityQueuePanel.PositionModifier = this.CityQueueFrame.GetComponent<AgeModifierPosition>();
		this.cityQueuePanel.Load();
		this.cityGarrisonPanel = Amplitude.Unity.Gui.GuiPanel.Instanciate<CityGarrisonPanel>(this.CityGarrisonFrame, this.CityGarrisonPanelPrefab, null);
		this.cityGarrisonPanel.PositionModifier = this.CityGarrisonFrame.GetComponent<AgeModifierPosition>();
		this.cityGarrisonPanel.Load();
		this.villageGarrisonPanel = Amplitude.Unity.Gui.GuiPanel.Instanciate<VillageGarrisonPanel>(this.VillageGarrisonFrame, this.VillageGarrisonPanelPrefab, null);
		this.villageGarrisonPanel.PositionModifier = this.VillageGarrisonFrame.GetComponent<AgeModifierPosition>();
		this.villageGarrisonPanel.Load();
		this.fortressGarrisonPanel = Amplitude.Unity.Gui.GuiPanel.Instanciate<FortressGarrisonPanel>(this.FortressGarrisonFrame, this.FortressGarrisonPanelPrefab, null);
		this.fortressGarrisonPanel.PositionModifier = this.FortressGarrisonFrame.GetComponent<AgeModifierPosition>();
		this.fortressGarrisonPanel.Load();
		this.kaijuGarrisonPanel = Amplitude.Unity.Gui.GuiPanel.Instanciate<KaijuGarrisonPanel>(this.KaijuGarrisonFrame, this.KaijuGarrisonPanelPrefab, null);
		this.kaijuGarrisonPanel.PositionModifier = this.KaijuGarrisonFrame.GetComponent<AgeModifierPosition>();
		this.kaijuGarrisonPanel.Load();
		this.CityWorkersPanel.Load();
		yield break;
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		this.EndTurnService = Services.GetService<IEndTurnService>();
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		this.CityListPanel.InteractionAllowed = base.PlayerController.CanSendOrders();
		base.GuiService.GetGuiPanel<ControlBanner>().OnShowScreen(GameScreenType.CityList);
		this.CityListPanel.Show(new object[]
		{
			null,
			base.gameObject
		});
		base.GuiService.Show(typeof(EmpireBannerPanel), new object[]
		{
			EmpireBannerPanel.Full
		});
		base.NeedRefresh = true;
		yield break;
	}

	protected override void OnUnload()
	{
		if (this.kaijuGarrisonPanel != null)
		{
			this.kaijuGarrisonPanel.Unload();
			UnityEngine.Object.Destroy(this.kaijuGarrisonPanel);
			this.kaijuGarrisonPanel = null;
		}
		if (this.fortressGarrisonPanel != null)
		{
			this.fortressGarrisonPanel.Unload();
			UnityEngine.Object.Destroy(this.fortressGarrisonPanel);
			this.fortressGarrisonPanel = null;
		}
		if (this.villageGarrisonPanel != null)
		{
			this.villageGarrisonPanel.Unload();
			UnityEngine.Object.Destroy(this.villageGarrisonPanel);
			this.villageGarrisonPanel = null;
		}
		if (this.cityGarrisonPanel != null)
		{
			this.cityGarrisonPanel.Unload();
			UnityEngine.Object.Destroy(this.cityGarrisonPanel);
			this.cityGarrisonPanel = null;
		}
		if (this.cityQueuePanel != null)
		{
			this.cityQueuePanel.Unload();
			UnityEngine.Object.Destroy(this.cityQueuePanel);
			this.cityQueuePanel = null;
		}
		if (this.cityOptionsPanel != null)
		{
			this.cityOptionsPanel.Unload();
			UnityEngine.Object.Destroy(this.cityOptionsPanel);
			this.cityOptionsPanel = null;
		}
		this.CityWorkersPanel.Unload();
		base.OnUnload();
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.Hide(true);
		this.Unbind();
		this.EndTurnService = null;
		base.OnUnloadGame(game);
	}

	private void EndTurnService_GameClientStateChange(object sender, GameClientStateChangeEventArgs e)
	{
		if (base.IsVisible && base.PlayerController != null)
		{
			bool flag = base.PlayerController.CanSendOrders();
			if (this.CityListPanel.InteractionAllowed != flag)
			{
				this.CityListPanel.InteractionAllowed = flag;
				if (!this.CityListPanel.InteractionAllowed && MessagePanel.Instance.IsVisible)
				{
					MessagePanel.Instance.Hide(false);
				}
			}
		}
	}

	private void HideSubPanels(bool instant = false)
	{
		this.cityOptionsPanel.Hide(instant);
		this.cityQueuePanel.Hide(instant);
		this.cityGarrisonPanel.Hide(instant);
		this.CityWorkersPanel.Hide(instant);
		this.villageGarrisonPanel.Hide(instant);
		this.fortressGarrisonPanel.Hide(instant);
		this.kaijuGarrisonPanel.Hide(instant);
	}

	private void RefreshTableVisibility()
	{
		List<Village> convertedVillages = (base.Empire as MajorEmpire).ConvertedVillages;
		ReadOnlyCollection<Fortress> occupiedFortresses = (base.Empire as MajorEmpire).GetAgency<DepartmentOfTheInterior>().OccupiedFortresses;
		ReadOnlyCollection<City> infectedCities = (base.Empire as MajorEmpire).GetAgency<DepartmentOfTheInterior>().InfectedCities;
		if (convertedVillages.Count > 0)
		{
			if (!this.VillageListPanel.IsVisible)
			{
				this.VillageListPanel.Show(new object[]
				{
					null,
					base.gameObject
				});
			}
		}
		else if (this.VillageListPanel.IsVisible)
		{
			this.VillageListPanel.Hide(true);
		}
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		if (service.IsShared(DownloadableContent16.ReadOnlyName))
		{
			if (occupiedFortresses.Count > 0)
			{
				if (!this.OceanicRegionListPanel.IsVisible)
				{
					this.OceanicRegionListPanel.Show(new object[]
					{
						null,
						base.gameObject
					});
				}
			}
			else if (this.OceanicRegionListPanel.IsVisible)
			{
				this.OceanicRegionListPanel.Hide(true);
			}
		}
		else if (this.OceanicRegionListPanel.IsVisible)
		{
			this.OceanicRegionListPanel.Hide(true);
		}
		if (service.IsShared(DownloadableContent20.ReadOnlyName))
		{
			if (infectedCities.Count > 0)
			{
				if (!this.InfectedCitiesListPanel.IsVisible)
				{
					this.InfectedCitiesListPanel.Show(new object[]
					{
						null,
						base.gameObject
					});
				}
			}
			else if (this.InfectedCitiesListPanel.IsVisible)
			{
				this.InfectedCitiesListPanel.Hide(true);
			}
			if ((base.Empire as MajorEmpire).RootedKaijus.Count > 0)
			{
				if (!this.KaijuListPanel.IsVisible)
				{
					this.KaijuListPanel.Show(new object[]
					{
						null,
						base.gameObject
					});
				}
			}
			else if (this.KaijuListPanel.IsVisible)
			{
				this.KaijuListPanel.Hide(true);
			}
		}
		else
		{
			if (this.InfectedCitiesListPanel.IsVisible)
			{
				this.InfectedCitiesListPanel.Hide(true);
			}
			if (this.KaijuListPanel.IsVisible)
			{
				this.KaijuListPanel.Hide(true);
			}
		}
	}

	private void RefreshTableLayout()
	{
		this.CityListPanel.AgeTransform.Height = this.CityListPanel.CitiesTable.Y + this.CityListPanel.CitiesTable.Height + this.CityListPanel.CitiesTable.PixelMarginBottom;
		this.InfectedCitiesListPanel.AgeTransform.Y = this.CityListPanel.AgeTransform.Height + this.CityListPanel.AgeTransform.PixelMarginBottom;
		if (this.InfectedCitiesListPanel.IsVisible)
		{
			this.InfectedCitiesListPanel.AgeTransform.Height = this.InfectedCitiesListPanel.InfectedCitiesTable.Y + this.InfectedCitiesListPanel.InfectedCitiesTable.Height + this.InfectedCitiesListPanel.InfectedCitiesTable.PixelMarginBottom;
		}
		else
		{
			this.InfectedCitiesListPanel.AgeTransform.Height = 0f;
		}
		this.VillageListPanel.AgeTransform.Y = this.InfectedCitiesListPanel.AgeTransform.Y + this.InfectedCitiesListPanel.AgeTransform.Height;
		if (this.InfectedCitiesListPanel.IsVisible)
		{
			this.VillageListPanel.AgeTransform.Y += this.InfectedCitiesListPanel.AgeTransform.PixelMarginBottom;
		}
		if (this.VillageListPanel.IsVisible)
		{
			this.VillageListPanel.AgeTransform.Height = this.VillageListPanel.VillagesTable.Y + this.VillageListPanel.VillagesTable.Height + this.VillageListPanel.VillagesTable.PixelMarginBottom;
		}
		else
		{
			this.VillageListPanel.AgeTransform.Height = 0f;
		}
		this.OceanicRegionListPanel.AgeTransform.Y = this.VillageListPanel.AgeTransform.Y + this.VillageListPanel.AgeTransform.Height;
		if (this.VillageListPanel.IsVisible)
		{
			this.OceanicRegionListPanel.AgeTransform.Y += this.VillageListPanel.AgeTransform.PixelMarginBottom;
		}
		if (this.OceanicRegionListPanel.IsVisible)
		{
			this.OceanicRegionListPanel.AgeTransform.Height = this.OceanicRegionListPanel.OceanicRegionsTable.Y + this.OceanicRegionListPanel.OceanicRegionsTable.Height + this.OceanicRegionListPanel.OceanicRegionsTable.PixelMarginBottom;
		}
		else
		{
			this.OceanicRegionListPanel.AgeTransform.Height = 0f;
		}
		this.KaijuListPanel.AgeTransform.Y = this.OceanicRegionListPanel.AgeTransform.Y + this.OceanicRegionListPanel.AgeTransform.Height;
		if (this.OceanicRegionListPanel.IsVisible)
		{
			this.KaijuListPanel.AgeTransform.Y += this.OceanicRegionListPanel.AgeTransform.PixelMarginBottom;
		}
		if (this.KaijuListPanel.IsVisible)
		{
			this.KaijuListPanel.AgeTransform.Height = this.KaijuListPanel.KaijusTable.Y + this.KaijuListPanel.KaijusTable.Height + this.KaijuListPanel.KaijusTable.PixelMarginBottom;
		}
		else
		{
			this.KaijuListPanel.AgeTransform.Height = 0f;
		}
		this.ListsScrollView.VirtualArea.Height = this.KaijuListPanel.AgeTransform.Y + this.KaijuListPanel.AgeTransform.Height;
	}

	private void OnAIGovernorUpdated()
	{
		base.NeedRefresh = true;
	}

	private void OnCloseCB(GameObject obj)
	{
		this.cityOptionsPanel.Hide(true);
		this.cityQueuePanel.Hide(true);
		this.villageGarrisonPanel.Hide(true);
		this.fortressGarrisonPanel.Hide(true);
		this.cityGarrisonPanel.Hide(true);
		this.kaijuGarrisonPanel.Hide(true);
		this.CityWorkersPanel.Hide(true);
		this.VillageListPanel.Hide(true);
		this.OceanicRegionListPanel.Hide(true);
		this.InfectedCitiesListPanel.Hide(true);
		this.KaijuListPanel.Hide(true);
		base.GuiService.GetGuiPanel<GameWorldScreen>().Show(new object[0]);
	}

	private void OnConstructionShortcut()
	{
		this.CurrentSelection = CityLine.CurrentCity;
		this.EnforceRadios();
		this.UnbindSubPanels();
		this.cityOptionsPanel.Bind(CityLine.CurrentCity);
		this.cityQueuePanel.Bind(CityLine.CurrentCity);
		if (!this.cityOptionsPanel.IsVisible)
		{
			this.cityOptionsPanel.Show(new object[0]);
		}
		else
		{
			this.cityOptionsPanel.RefreshContent();
		}
		if (!this.cityQueuePanel.IsVisible)
		{
			this.cityQueuePanel.Show(new object[0]);
		}
		else
		{
			this.cityQueuePanel.RefreshContent();
		}
		if (this.cityGarrisonPanel.IsVisible)
		{
			this.cityGarrisonPanel.Hide(true);
		}
		if (this.CityWorkersPanel.IsVisible)
		{
			this.CityWorkersPanel.Hide(true);
		}
		if (this.villageGarrisonPanel.IsVisible)
		{
			this.villageGarrisonPanel.Hide(true);
		}
		if (this.fortressGarrisonPanel.IsVisible)
		{
			this.fortressGarrisonPanel.Hide(true);
		}
		if (this.kaijuGarrisonPanel.IsVisible)
		{
			this.kaijuGarrisonPanel.Hide(true);
		}
	}

	private void OnCityGarrisonShortcut()
	{
		this.CurrentSelection = CityLine.CurrentCity;
		this.EnforceRadios();
		this.UnbindSubPanels();
		this.SetupGarrisonPanel(CityLine.CurrentCity);
	}

	private void OnInfectedCityGarrisonShortcut()
	{
		this.CurrentSelection = InfectedCityLine.CurrentCity;
		this.EnforceRadios();
		this.UnbindSubPanels();
		this.SetupGarrisonPanel(InfectedCityLine.CurrentCity);
	}

	private void SetupGarrisonPanel(City cityToBind)
	{
		this.cityGarrisonPanel.Bind(cityToBind);
		if (!this.cityGarrisonPanel.IsVisible)
		{
			this.cityGarrisonPanel.Show(new object[0]);
		}
		this.cityGarrisonPanel.RefreshContent();
		if (this.cityOptionsPanel.IsVisible)
		{
			this.cityOptionsPanel.Hide(true);
		}
		if (this.cityQueuePanel.IsVisible)
		{
			this.cityQueuePanel.Hide(true);
		}
		if (this.CityWorkersPanel.IsVisible)
		{
			this.CityWorkersPanel.Hide(true);
		}
		if (this.villageGarrisonPanel.IsVisible)
		{
			this.villageGarrisonPanel.Hide(true);
		}
		if (this.fortressGarrisonPanel.IsVisible)
		{
			this.fortressGarrisonPanel.Hide(true);
		}
		if (this.kaijuGarrisonPanel.IsVisible)
		{
			this.kaijuGarrisonPanel.Hide(true);
		}
	}

	private void OnVillageGarrisonShortcut()
	{
		this.CurrentSelection = VillageLine.CurrentVillage;
		this.EnforceRadios();
		this.UnbindSubPanels();
		this.villageGarrisonPanel.Bind(VillageLine.CurrentVillage);
		if (!this.villageGarrisonPanel.IsVisible)
		{
			this.villageGarrisonPanel.Show(new object[0]);
		}
		this.villageGarrisonPanel.RefreshContent();
		if (this.cityOptionsPanel.IsVisible)
		{
			this.cityOptionsPanel.Hide(true);
		}
		if (this.cityGarrisonPanel.IsVisible)
		{
			this.cityGarrisonPanel.Hide(true);
		}
		if (this.fortressGarrisonPanel.IsVisible)
		{
			this.fortressGarrisonPanel.Hide(true);
		}
		if (this.kaijuGarrisonPanel.IsVisible)
		{
			this.kaijuGarrisonPanel.Hide(true);
		}
		if (this.cityQueuePanel.IsVisible)
		{
			this.cityQueuePanel.Hide(true);
		}
		if (this.CityWorkersPanel.IsVisible)
		{
			this.CityWorkersPanel.Hide(true);
		}
	}

	private void OnFortressGarrisonShortcut()
	{
		this.CurrentSelection = FortressLine.CurrentFortress;
		this.EnforceRadios();
		this.UnbindSubPanels();
		this.fortressGarrisonPanel.Bind(FortressLine.CurrentFortress);
		if (!this.fortressGarrisonPanel.IsVisible)
		{
			this.fortressGarrisonPanel.Show(new object[0]);
		}
		this.fortressGarrisonPanel.RefreshContent();
		if (this.cityOptionsPanel.IsVisible)
		{
			this.cityOptionsPanel.Hide(true);
		}
		if (this.cityGarrisonPanel.IsVisible)
		{
			this.cityGarrisonPanel.Hide(true);
		}
		if (this.villageGarrisonPanel.IsVisible)
		{
			this.villageGarrisonPanel.Hide(true);
		}
		if (this.kaijuGarrisonPanel.IsVisible)
		{
			this.kaijuGarrisonPanel.Hide(true);
		}
		if (this.cityQueuePanel.IsVisible)
		{
			this.cityQueuePanel.Hide(true);
		}
		if (this.CityWorkersPanel.IsVisible)
		{
			this.CityWorkersPanel.Hide(true);
		}
	}

	private void OnKaijuGarrisonShortcut()
	{
		this.CurrentSelection = KaijuLine.CurrentKaiju;
		this.EnforceRadios();
		this.UnbindSubPanels();
		this.kaijuGarrisonPanel.Bind(KaijuLine.CurrentKaiju);
		if (!this.kaijuGarrisonPanel.IsVisible)
		{
			this.kaijuGarrisonPanel.Show(new object[0]);
		}
		this.kaijuGarrisonPanel.RefreshContent();
		if (this.cityOptionsPanel.IsVisible)
		{
			this.cityOptionsPanel.Hide(true);
		}
		if (this.cityGarrisonPanel.IsVisible)
		{
			this.cityGarrisonPanel.Hide(true);
		}
		if (this.villageGarrisonPanel.IsVisible)
		{
			this.villageGarrisonPanel.Hide(true);
		}
		if (this.fortressGarrisonPanel.IsVisible)
		{
			this.fortressGarrisonPanel.Hide(true);
		}
		if (this.cityQueuePanel.IsVisible)
		{
			this.cityQueuePanel.Hide(true);
		}
		if (this.CityWorkersPanel.IsVisible)
		{
			this.CityWorkersPanel.Hide(true);
		}
	}

	private void OnPopulationShortcut()
	{
		this.CurrentSelection = CityLine.CurrentCity;
		this.EnforceRadios();
		this.UnbindSubPanels();
		this.CityWorkersPanel.Bind(CityLine.CurrentCity);
		if (!this.CityWorkersPanel.IsVisible)
		{
			this.CityWorkersPanel.Show(new object[0]);
		}
		this.CityWorkersPanel.RefreshContent();
		if (this.cityOptionsPanel.IsVisible)
		{
			this.cityOptionsPanel.Hide(true);
		}
		if (this.cityQueuePanel.IsVisible)
		{
			this.cityQueuePanel.Hide(true);
		}
		if (this.cityGarrisonPanel.IsVisible)
		{
			this.cityGarrisonPanel.Hide(true);
		}
		if (this.villageGarrisonPanel.IsVisible)
		{
			this.villageGarrisonPanel.Hide(true);
		}
		if (this.fortressGarrisonPanel.IsVisible)
		{
			this.fortressGarrisonPanel.Hide(true);
		}
		if (this.kaijuGarrisonPanel.IsVisible)
		{
			this.kaijuGarrisonPanel.Hide(true);
		}
	}

	private void OnOrderResponse()
	{
		base.NeedRefresh = true;
	}

	private void OnCitiesCollectionChanged()
	{
		this.HideSubPanels(false);
		this.UnbindSubPanels();
	}

	private void OnVillagesCollectionChanged()
	{
		this.HideSubPanels(false);
		this.UnbindSubPanels();
	}

	private void OnDoubleClickCityLine()
	{
		City currentCity = CityLine.CurrentCity;
		this.OnCloseCB(null);
		IViewService service = Services.GetService<IViewService>();
		if (service != null)
		{
			service.SelectAndCenter(currentCity, false);
		}
		this.Hide(false);
	}

	private void OnFortressLocationShortcut()
	{
		Fortress currentFortress = FortressLine.CurrentFortress;
		this.OnCloseCB(null);
		IViewService service = Services.GetService<IViewService>();
		if (service != null)
		{
			service.SelectAndCenter(currentFortress, false);
		}
		this.Hide(false);
	}

	private void OnDoubleClickVillageLine()
	{
		Village currentVillage = VillageLine.CurrentVillage;
		this.OnCloseCB(null);
		IViewService service = Services.GetService<IViewService>();
		if (service != null)
		{
			service.SelectAndCenter(currentVillage, false);
		}
		this.Hide(false);
	}

	private void OnDoubleClickInfectedCityLine()
	{
		City currentCity = InfectedCityLine.CurrentCity;
		this.OnCloseCB(null);
		IViewService service = Services.GetService<IViewService>();
		if (service != null)
		{
			service.SelectAndCenter(currentCity, false);
		}
		this.Hide(false);
	}

	private void OnDoubleClickKaijuLine()
	{
		KaijuGarrison currentKaiju = KaijuLine.CurrentKaiju;
		this.OnCloseCB(null);
		IViewService service = Services.GetService<IViewService>();
		if (service != null)
		{
			service.SelectAndCenter(currentKaiju, false);
		}
		this.Hide(false);
	}

	private void OnKaijuUnlocksClick()
	{
		if (base.GuiService.GetGuiPanel<KaijuResearchModalPanel>().IsVisible)
		{
			base.GuiService.GetGuiPanel<GameWorldScreen>().Show(new object[0]);
		}
		else
		{
			base.GuiService.Hide(typeof(CurrentQuestPanel));
			base.GuiService.GetGuiPanel<KaijuResearchModalPanel>().Show(new object[0]);
		}
	}

	private void EnforceRadios()
	{
		CityLine.CurrentCity = null;
		VillageLine.CurrentVillage = null;
		FortressLine.CurrentFortress = null;
		InfectedCityLine.CurrentCity = null;
		KaijuLine.CurrentKaiju = null;
		if (this.CurrentSelection is City)
		{
			if (!(this.CurrentSelection as City).IsInfected)
			{
				CityLine.CurrentCity = (this.CurrentSelection as City);
			}
			else
			{
				InfectedCityLine.CurrentCity = (this.CurrentSelection as City);
			}
		}
		else if (this.CurrentSelection is Village)
		{
			VillageLine.CurrentVillage = (this.CurrentSelection as Village);
		}
		else if (this.CurrentSelection is Fortress)
		{
			FortressLine.CurrentFortress = (this.CurrentSelection as Fortress);
		}
		else if (this.CurrentSelection is KaijuGarrison)
		{
			KaijuLine.CurrentKaiju = (this.CurrentSelection as KaijuGarrison);
		}
		this.CityListPanel.EnforceRadio();
		this.VillageListPanel.EnforceRadio();
		this.OceanicRegionListPanel.EnforceRadio();
		this.InfectedCitiesListPanel.EnforceRadio();
		this.KaijuListPanel.EnforceRadio();
	}

	private void OnSelectCity()
	{
		this.CurrentSelection = CityLine.CurrentCity;
		this.EnforceRadios();
		this.HideSubPanels(false);
		this.UnbindSubPanels();
	}

	private void OnSelectVillage()
	{
		this.CurrentSelection = VillageLine.CurrentVillage;
		this.EnforceRadios();
		this.HideSubPanels(false);
		this.UnbindSubPanels();
	}

	private void OnSelectFortress()
	{
		this.CurrentSelection = FortressLine.CurrentFortress;
		this.EnforceRadios();
		this.HideSubPanels(false);
		this.UnbindSubPanels();
	}

	private void OnSelectInfectedCity()
	{
		this.CurrentSelection = InfectedCityLine.CurrentCity;
		this.EnforceRadios();
		this.HideSubPanels(false);
		this.UnbindSubPanels();
	}

	private void OnSelectKaiju()
	{
		this.CurrentSelection = KaijuLine.CurrentKaiju;
		this.EnforceRadios();
		this.HideSubPanels(false);
		this.UnbindSubPanels();
	}

	private void UnbindSubPanels()
	{
		this.cityOptionsPanel.Unbind();
		this.cityQueuePanel.Unbind();
		this.cityGarrisonPanel.Unbind();
		this.CityWorkersPanel.Unbind();
		this.villageGarrisonPanel.Unbind();
		this.fortressGarrisonPanel.Unbind();
		this.kaijuGarrisonPanel.Unbind();
	}

	public Transform CityOptionsPanelPrefab;

	public Transform CityQueuePanelPrefab;

	public Transform CityGarrisonPanelPrefab;

	public Transform VillageGarrisonPanelPrefab;

	public Transform FortressGarrisonPanelPrefab;

	public Transform KaijuGarrisonPanelPrefab;

	public AgeControlScrollView ListsScrollView;

	public CityListPanel CityListPanel;

	public VillageListPanel VillageListPanel;

	public OceanicRegionListPanel OceanicRegionListPanel;

	public InfectedCitiesListPanel InfectedCitiesListPanel;

	public KaijuListPanel KaijuListPanel;

	public AgeTransform CityOptionsFrame;

	public AgeTransform CityQueueFrame;

	public AgeTransform CityGarrisonFrame;

	public AgeTransform VillageGarrisonFrame;

	public AgeTransform FortressGarrisonFrame;

	public AgeTransform KaijuGarrisonFrame;

	public CityWorkersPanel CityWorkersPanel;

	private CityOptionsPanel cityOptionsPanel;

	private CityQueuePanel cityQueuePanel;

	private CityGarrisonPanel cityGarrisonPanel;

	private VillageGarrisonPanel villageGarrisonPanel;

	private FortressGarrisonPanel fortressGarrisonPanel;

	private KaijuGarrisonPanel kaijuGarrisonPanel;

	private IEndTurnService endTurnService;
}
