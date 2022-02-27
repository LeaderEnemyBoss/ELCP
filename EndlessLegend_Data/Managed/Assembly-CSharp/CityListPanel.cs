using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using UnityEngine;

public class CityListPanel : global::GuiPanel
{
	public global::Empire Empire { get; private set; }

	public bool DisableCitiesInEncounter { get; set; }

	public bool DisableCitiesWithNoArmySpawnLocation { get; set; }

	public bool DisableCitiesWithNoSeafaringArmySpawnLocation { get; set; }

	public bool DisableCitiesWithoutSlotLeft { get; set; }

	public bool DisableCitiesWithHeroAssignmentImpossible { get; set; }

	public ReadOnlyCollection<City> FilteredCities { get; set; }

	public bool ReadOnly
	{
		get
		{
			return this.readOnly;
		}
		set
		{
			this.readOnly = value;
			base.NeedRefresh = true;
		}
	}

	public bool InteractionAllowed
	{
		get
		{
			return this.interactionAllowed;
		}
		set
		{
			if (this.interactionAllowed != value)
			{
				this.interactionAllowed = value;
				this.OnInteractionAllowedChanged();
			}
		}
	}

	public GameObject RefreshClient { get; set; }

	public void Bind(global::Empire empire, GameObject refreshClient)
	{
		this.Empire = empire;
		this.departmentOfTheInterior = this.Empire.GetAgency<DepartmentOfTheInterior>();
		this.departmentOfTheInterior.CitiesCollectionChanged += this.CityListPanel_CitiesCollectionChanged;
		this.RefreshClient = refreshClient;
		this.Empire.Refreshed += this.Simulation_Refreshed;
		base.NeedRefresh = true;
	}

	public void EnforceRadio()
	{
		for (int i = 0; i < this.CitiesTable.GetChildren().Count; i++)
		{
			CityLine component = this.CitiesTable.GetChildren()[i].GetComponent<CityLine>();
			component.SelectionToggle.State = (component.City == CityLine.CurrentCity);
		}
	}

	public override bool HandleDownRequest()
	{
		List<CityLine> children = this.CitiesTable.GetChildren<CityLine>(true);
		if (children.Count > 0)
		{
			CityLine cityLine;
			if (CityLine.CurrentCity == null)
			{
				cityLine = children[0];
			}
			else
			{
				int num = children.IndexOf(this.FindCityLineOfCity(CityLine.CurrentCity)) + 1;
				if (num > children.Count - 1)
				{
					num = children.Count - 1;
				}
				cityLine = children[num];
			}
			if (cityLine.City != CityLine.CurrentCity)
			{
				cityLine.SendMessage("OnSwitchLine", cityLine.SelectionToggle.gameObject);
				if (this.CitiesScrollView != null)
				{
					this.CitiesScrollView.EnsureChildIsVerticallyVisible(cityLine.AgeTransform);
				}
			}
		}
		return true;
	}

	public override bool HandleUpRequest()
	{
		List<CityLine> children = this.CitiesTable.GetChildren<CityLine>(true);
		if (children.Count > 0)
		{
			CityLine cityLine;
			if (CityLine.CurrentCity == null)
			{
				cityLine = children[0];
			}
			else
			{
				int num = children.IndexOf(this.FindCityLineOfCity(CityLine.CurrentCity)) - 1;
				if (num < 0)
				{
					num = 0;
				}
				cityLine = children[num];
			}
			if (cityLine.City != CityLine.CurrentCity)
			{
				cityLine.SendMessage("OnSwitchLine", cityLine.SelectionToggle.gameObject);
				if (this.CitiesScrollView != null)
				{
					this.CitiesScrollView.EnsureChildIsVerticallyVisible(cityLine.AgeTransform);
				}
			}
		}
		return true;
	}

	public void Unbind()
	{
		this.FilteredCities = null;
		if (this.Empire != null)
		{
			this.departmentOfTheInterior.CitiesCollectionChanged -= this.CityListPanel_CitiesCollectionChanged;
			this.departmentOfTheInterior = null;
			this.Empire.Refreshed -= this.Simulation_Refreshed;
			this.Empire = null;
		}
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		if (this.Empire != null)
		{
			if (this.FilteredCities == null)
			{
				this.FilteredCities = this.departmentOfTheInterior.NonInfectedCities;
			}
			this.CitiesTable.Height = 0f;
			this.CitiesTable.ReserveChildren(this.FilteredCities.Count, this.CityLinePrefab, "CityLine");
			this.CitiesTable.RefreshChildrenIList<City>(this.FilteredCities, this.refreshCityLineDelegate, true, false);
			this.CitiesTable.ArrangeChildren();
			if (this.CitiesScrollView != null)
			{
				this.CitiesScrollView.OnPositionRecomputed();
			}
			this.EnforceRadio();
			this.OnInteractionAllowedChanged();
			SortedLinesTable component = this.CitiesTable.GetComponent<SortedLinesTable>();
			if (component != null)
			{
				component.SortLines();
			}
		}
	}

	protected override IEnumerator OnHide(bool instant)
	{
		base.NeedRefresh = false;
		List<CityLine> cityLines = this.CitiesTable.GetChildren<CityLine>(true);
		for (int i = 0; i < cityLines.Count; i++)
		{
			cityLines[i].SelectionToggle.State = false;
			cityLines[i].Unbind();
		}
		CityLine.CurrentCity = null;
		this.FilteredCities = null;
		this.SortsContainer.UnsetContent();
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		base.UseRefreshLoop = true;
		this.InteractionAllowed = true;
		CityLine.ResourceOccurencePrefab = this.ResourceOccurencePrefab;
		CityLine.FidsTypes = new List<string>();
		CityLine.FidsTypes.Add(SimulationProperties.NetCityGrowth);
		CityLine.FidsTypes.Add(SimulationProperties.NetCityProduction);
		CityLine.FidsTypes.Add(SimulationProperties.NetCityResearch);
		CityLine.FidsTypes.Add(SimulationProperties.NetCityMoney);
		CityLine.FidsTypes.Add(SimulationProperties.NetCityEmpirePoint);
		CityLine.FidsPopulations = new List<string>();
		CityLine.FidsPopulations.Add(SimulationProperties.FoodPopulation);
		CityLine.FidsPopulations.Add(SimulationProperties.IndustryPopulation);
		CityLine.FidsPopulations.Add(SimulationProperties.SciencePopulation);
		CityLine.FidsPopulations.Add(SimulationProperties.DustPopulation);
		CityLine.FidsPopulations.Add(SimulationProperties.CityPointPopulation);
		CityLine.FidsColors = new List<Color>();
		for (int i = 0; i < CityLine.FidsTypes.Count; i++)
		{
			GuiElement guiElement;
			base.GuiService.GuiPanelHelper.TryGetGuiElement(CityLine.FidsTypes[i], out guiElement);
			if (guiElement != null)
			{
				ExtendedGuiElement extendedGuiElement = (ExtendedGuiElement)guiElement;
				if (extendedGuiElement != null)
				{
					CityLine.FidsColors.Add(extendedGuiElement.Color);
				}
			}
		}
		yield break;
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		this.refreshCityLineDelegate = new AgeTransform.RefreshTableItem<City>(this.RefreshCityLine);
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		CityLine.CurrentCity = null;
		this.selectionClient = null;
		if (parameters.Length > 0)
		{
			CityLine.CurrentCity = (parameters[0] as City);
			if (parameters.Length > 1)
			{
				this.selectionClient = (parameters[1] as GameObject);
			}
		}
		this.SortsContainer.SetContent(this.CityLinePrefab, "CityLine", null);
		yield break;
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.CitiesTable.DestroyAllChildren();
		this.refreshCityLineDelegate = null;
		base.OnUnloadGame(game);
	}

	private CityLine FindCityLineOfCity(City city)
	{
		List<CityLine> children = this.CitiesTable.GetChildren<CityLine>(true);
		for (int i = 0; i < children.Count; i++)
		{
			if (children[i].City == city)
			{
				return children[i];
			}
		}
		return null;
	}

	private void OnInteractionAllowedChanged()
	{
		List<CityLine> children = this.CitiesTable.GetChildren<CityLine>(true);
		for (int i = 0; i < children.Count; i++)
		{
			children[i].PopulationBuyoutButton.AgeTransform.Enable &= this.InteractionAllowed;
			children[i].PopulationSacrificeButton.AgeTransform.Enable &= this.InteractionAllowed;
			children[i].BuyoutButton.AgeTransform.Enable &= this.InteractionAllowed;
			children[i].AIDropList.GetComponent<AgeTransform>().Enable &= this.InteractionAllowed;
		}
	}

	private void Simulation_Refreshed(object sender)
	{
		base.NeedRefresh = true;
	}

	private void RefreshCityLine(AgeTransform tableitem, City city, int index)
	{
		tableitem.StartNewMesh = true;
		CityLine component = tableitem.GetComponent<CityLine>();
		component.Bind(city, this.selectionClient.gameObject);
		component.ReadOnly = this.ReadOnly;
		component.RefreshContent();
		component.AgeTransform.Enable = true;
		if (this.DisableCitiesWithoutSlotLeft)
		{
			component.DisableIfNoSlotLeft();
		}
		if (this.DisableCitiesWithHeroAssignmentImpossible)
		{
			component.DisableIfHeroAssignmentImpossible();
		}
		if (this.DisableCitiesInEncounter)
		{
			component.DisableIfGarrisonIsInEncounter();
		}
		if (this.DisableCitiesWithNoArmySpawnLocation)
		{
			component.DisableIfNoArmySpawnLocation();
		}
		if (this.DisableCitiesWithNoSeafaringArmySpawnLocation)
		{
			component.DisableIfNoSeafaringArmySpawnLocation();
		}
	}

	private void CityListPanel_CitiesCollectionChanged(object sender, CollectionChangeEventArgs e)
	{
		if (base.IsVisible)
		{
			CityLine.CurrentCity = null;
			base.NeedRefresh = true;
			this.RefreshClient.SendMessage("OnCitiesCollectionChanged");
		}
	}

	public const string LastSortString = "ZZZZZZZ";

	public AgeTransform Background;

	public AgeTransform TitleGroup;

	public Transform CityLinePrefab;

	public Transform ResourceOccurencePrefab;

	public AgeTransform CitiesTable;

	public AgeControlScrollView CitiesScrollView;

	public SortButtonsContainer SortsContainer;

	private GameObject selectionClient;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private bool readOnly;

	private bool interactionAllowed;

	private AgeTransform.RefreshTableItem<City> refreshCityLineDelegate;

	public delegate int CompareLine(CityLine l, CityLine r);
}
