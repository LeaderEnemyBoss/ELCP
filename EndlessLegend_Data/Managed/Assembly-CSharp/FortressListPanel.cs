using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Amplitude.Unity.Game;
using UnityEngine;

public class FortressListPanel : GuiPanel
{
	public global::Empire Empire { get; private set; }

	public ReadOnlyCollection<Fortress> FilteredFortresses { get; set; }

	public bool ReadOnly
	{
		get
		{
			return this.readOnly;
		}
		set
		{
			this.readOnly = value;
			if (base.IsVisible)
			{
				this.RefreshContent();
			}
		}
	}

	public GameObject RefreshClient { get; set; }

	public void Bind(global::Empire empire, GameObject refreshClient)
	{
		this.Empire = empire;
		this.departmentOfTheInterior = this.Empire.GetAgency<DepartmentOfTheInterior>();
		this.departmentOfTheInterior.OccupiedFortressesCollectionChanged += this.DepartmentOfInterior_OccupiedFortressesCollectionChanged;
		this.RefreshClient = refreshClient;
		this.Empire.Refreshed += this.Simulation_Refreshed;
		base.NeedRefresh = true;
	}

	public void EnforceRadio()
	{
		for (int i = 0; i < this.FortressesTable.GetChildren().Count; i++)
		{
			FortressLine component = this.FortressesTable.GetChildren()[i].GetComponent<FortressLine>();
			component.SelectionToggle.State = (component.Fortress == FortressLine.CurrentFortress);
		}
	}

	public void Unbind()
	{
		if (this.Empire != null)
		{
			this.departmentOfTheInterior.OccupiedFortressesCollectionChanged -= this.DepartmentOfInterior_OccupiedFortressesCollectionChanged;
			this.departmentOfTheInterior = null;
			this.FilteredFortresses = null;
			this.Empire = null;
		}
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		if (this.Empire != null)
		{
			this.FilteredFortresses = this.departmentOfTheInterior.OccupiedFortresses;
			if (this.FilteredFortresses != null)
			{
				this.FortressesTable.Height = 0f;
				this.FortressesTable.ReserveChildren(this.FilteredFortresses.Count, this.FortressLinePrefab, "FortressLine");
				this.FortressesTable.RefreshChildrenIList<Fortress>(this.FilteredFortresses, this.refreshFortressLineDelegate, true, false);
				this.FortressesTable.ArrangeChildren();
				SortedLinesTable component = this.FortressesTable.GetComponent<SortedLinesTable>();
				if (component != null)
				{
					component.SortLines();
				}
			}
		}
	}

	protected override IEnumerator OnHide(bool instant)
	{
		List<FortressLine> fortressLines = this.FortressesTable.GetChildren<FortressLine>(true);
		for (int i = 0; i < fortressLines.Count; i++)
		{
			fortressLines[i].SelectionToggle.State = false;
			fortressLines[i].Unbind();
		}
		FortressLine.CurrentFortress = null;
		this.FilteredFortresses = null;
		this.SortsContainer.UnsetContent();
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		this.refreshFortressLineDelegate = new AgeTransform.RefreshTableItem<Fortress>(this.RefreshFortressLine);
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		FortressLine.CurrentFortress = null;
		this.selectionClient = null;
		if (parameters.Length > 0)
		{
			FortressLine.CurrentFortress = (parameters[0] as Fortress);
			if (parameters.Length > 1)
			{
				this.selectionClient = (parameters[1] as GameObject);
			}
		}
		this.SortsContainer.SetContent(this.FortressLinePrefab, "FortressLine", null);
		yield break;
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.FortressesTable.DestroyAllChildren();
		this.refreshFortressLineDelegate = null;
		base.OnUnloadGame(game);
	}

	private void Simulation_Refreshed(object sender)
	{
		if (base.IsVisible)
		{
			this.RefreshContent();
		}
	}

	private void DepartmentOfInterior_OccupiedFortressesCollectionChanged(object sender, CollectionChangeEventArgs e)
	{
		if (base.IsVisible)
		{
			FortressLine.CurrentFortress = null;
			base.NeedRefresh = true;
		}
	}

	private void RefreshFortressLine(AgeTransform tableitem, Fortress fortress, int index)
	{
		tableitem.StartNewMesh = true;
		FortressLine component = tableitem.GetComponent<FortressLine>();
		component.Bind(fortress, this.Empire, this.selectionClient.gameObject, false);
		component.RefreshContent();
		component.AgeTransform.Enable = true;
		component.DisableIfGarrisonIsInEncounter();
	}

	public const string LastSortString = "ZZZZZZZ";

	public AgeTransform TitleGroup;

	public Transform FortressLinePrefab;

	public AgeTransform FortressesTable;

	public SortButtonsContainer SortsContainer;

	private GameObject selectionClient;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private bool readOnly;

	private AgeTransform.RefreshTableItem<Fortress> refreshFortressLineDelegate;

	public delegate int CompareLine(FortressLine l, FortressLine r);
}
