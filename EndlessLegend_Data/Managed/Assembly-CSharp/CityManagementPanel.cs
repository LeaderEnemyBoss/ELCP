using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation;
using UnityEngine;

public class CityManagementPanel : GuiModalPanel
{
	public City City { get; private set; }

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

	private bool IsOtherEmpire { get; set; }

	public void Bind(City city)
	{
		this.City = city;
		if (this.UpkeepValue != null)
		{
			AgeTransform parent = this.UpkeepValue.AgeTransform.GetParent();
			if (parent != null && parent.AgeTooltip != null)
			{
				parent.AgeTooltip.Class = "CityMoneyUpkeep";
				parent.AgeTooltip.Content = "CityMoneyUpkeep";
				parent.AgeTooltip.ClientData = new SimulationPropertyTooltipData(SimulationProperties.CityMoneyUpkeep, SimulationProperties.CityMoneyUpkeep, this.City);
			}
		}
	}

	public void Unbind()
	{
		this.City = null;
		if (this.UpkeepValue != null)
		{
			AgeTransform parent = this.UpkeepValue.AgeTransform.GetParent();
			if (parent != null && parent.AgeTooltip != null)
			{
				parent.AgeTooltip.ClientData = null;
			}
		}
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		if (this.City == null)
		{
			Diagnostics.LogError("Trying to refresh CityManagementPanel while not bound to a city");
			return;
		}
		this.IsOtherEmpire = (this.playerControllerRepository.ActivePlayerController.Empire != this.City.Empire);
		int count = this.City.CityImprovements.Count;
		int num = Mathf.CeilToInt(this.City.GetPropertyValue(SimulationProperties.CityMoneyUpkeep));
		this.NumberValue.Text = count.ToString();
		this.UpkeepValue.Text = GuiFormater.FormatQuantity((float)num, SimulationProperties.CityMoneyUpkeep, 1);
		ReadOnlyCollection<CityImprovement> cityImprovements = this.City.CityImprovements;
		this.CityImprovementsTable.Height = 0f;
		this.CityImprovementsTable.ReserveChildren(count, this.BuildingPrefab, "Item");
		this.CityImprovementsTable.RefreshChildrenIList<CityImprovement>(cityImprovements, this.refreshCityImprovementDelegate, true, true);
		this.CityImprovementsTable.ArrangeChildren();
		this.CityImprovementsScrollView.OnPositionRecomputed();
		PointOfInterest[] pointOfInterests = this.City.Region.PointOfInterests;
		this.regionBuildings.Clear();
		for (int i = 0; i < pointOfInterests.Length; i++)
		{
			if (pointOfInterests[i].PointOfInterestImprovement != null)
			{
				string a;
				if (pointOfInterests[i].PointOfInterestDefinition.TryGetValue("Type", out a) && a == "Village")
				{
					if (!pointOfInterests[i].SimulationObject.Tags.Contains("PacifiedVillage"))
					{
						goto IL_18D;
					}
				}
				this.regionBuildings.Add(pointOfInterests[i]);
			}
			IL_18D:;
		}
		this.RegionBuildingsTable.Height = 0f;
		this.RegionBuildingsTable.ReserveChildren(this.regionBuildings.Count, this.BuildingPrefab, "Item");
		this.RegionBuildingsTable.RefreshChildrenIList<PointOfInterest>(this.regionBuildings, this.refreshRegionBuildingDelegate, true, true);
		this.RegionBuildingsTable.ArrangeChildren();
		this.RegionBuildingsScrollView.OnPositionRecomputed();
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		this.EndTurnService = Services.GetService<IEndTurnService>();
		this.regionBuildings = new List<PointOfInterest>();
		this.playerControllerRepository = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
		Diagnostics.Assert(this.playerControllerRepository != null);
		this.refreshCityImprovementDelegate = new AgeTransform.RefreshTableItem<CityImprovement>(this.RefreshCityImprovement);
		this.refreshRegionBuildingDelegate = new AgeTransform.RefreshTableItem<PointOfInterest>(this.RefreshRegionBuilding);
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		this.interactionsAllowed = this.playerControllerRepository.ActivePlayerController.CanSendOrders();
		List<ConstructedBuildingGuiItem> cityImprovementGuiItems = this.CityImprovementsTable.GetChildren<ConstructedBuildingGuiItem>(true);
		for (int i = 0; i < cityImprovementGuiItems.Count; i++)
		{
			cityImprovementGuiItems[i].Toggle.State = false;
		}
		List<ConstructedBuildingGuiItem> regionBuildingGuiItems = this.RegionBuildingsTable.GetChildren<ConstructedBuildingGuiItem>(true);
		for (int j = 0; j < regionBuildingGuiItems.Count; j++)
		{
			regionBuildingGuiItems[j].Toggle.State = false;
		}
		this.DestroyButton.Enable = false;
		if (this.DestroyButton.AgeTooltip != null)
		{
			this.DestroyButton.AgeTooltip.Content = "%DestroyButtonNoSelectionDescription";
		}
		yield break;
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.Hide(true);
		this.refreshCityImprovementDelegate = null;
		this.refreshRegionBuildingDelegate = null;
		this.Unbind();
		this.playerControllerRepository = null;
		this.CityImprovementsTable.DestroyAllChildren();
		this.RegionBuildingsTable.DestroyAllChildren();
		this.regionBuildings.Clear();
		this.regionBuildings = null;
		this.EndTurnService = null;
		base.OnUnloadGame(game);
	}

	private void EndTurnService_GameClientStateChange(object sender, GameClientStateChangeEventArgs e)
	{
		if (base.IsVisible && this.playerControllerRepository != null && this.playerControllerRepository.ActivePlayerController != null)
		{
			bool flag = this.playerControllerRepository.ActivePlayerController.CanSendOrders();
			if (this.interactionsAllowed != flag)
			{
				this.interactionsAllowed = flag;
				this.RefreshButtons();
				if (!this.interactionsAllowed && MessagePanel.Instance.IsVisible)
				{
					MessagePanel.Instance.Hide(false);
				}
			}
		}
	}

	private void RefreshCityImprovement(AgeTransform tableitem, CityImprovement reference, int index)
	{
		ConstructedBuildingGuiItem component = tableitem.GetComponent<ConstructedBuildingGuiItem>();
		Diagnostics.Assert(component != null);
		if (component != null)
		{
			component.RefreshCityImprovement(this.City, reference, base.gameObject);
			component.Toggle.State = false;
			bool flag = !reference.SimulationObject.DescriptorHolders.Exists((SimulationDescriptorHolder match) => match.Descriptor.Name == "CityImprovementUndestroyable");
			tableitem.Enable = (flag && this.interactionsAllowed && !this.IsOtherEmpire);
		}
	}

	private void RefreshRegionBuilding(AgeTransform tableitem, PointOfInterest reference, int index)
	{
		ConstructedBuildingGuiItem component = tableitem.GetComponent<ConstructedBuildingGuiItem>();
		Diagnostics.Assert(component != null);
		if (component != null)
		{
			component.RefreshRegionBuilding(this.City, reference, base.GuiService.GuiPanelHelper, base.gameObject);
			component.Toggle.State = false;
			tableitem.Enable = (this.interactionsAllowed && !this.IsOtherEmpire && !reference.ArmyPillaging.IsValid && !reference.SimulationObject.Tags.Contains(DepartmentOfDefense.PillageStatusDescriptor));
		}
	}

	private void RefreshButtons()
	{
		if (this.City != null && this.City.BesiegingEmpireIndex != -1)
		{
			this.DestroyButton.Enable = false;
			if (this.DestroyButton.AgeTooltip != null)
			{
				this.DestroyButton.AgeTooltip.Content = "%DestroyButtonBesiegedCityDescription";
			}
			return;
		}
		if (this.City != null && this.City.IsInEncounter)
		{
			this.DestroyButton.Enable = false;
			if (this.DestroyButton.AgeTooltip != null)
			{
				this.DestroyButton.AgeTooltip.Content = "%DestroyButtonCityIsInEncounterDescription";
			}
			return;
		}
		if (this.City != null && this.City.IsInfected)
		{
			this.DestroyButton.Enable = false;
			if (this.DestroyButton.AgeTooltip != null)
			{
				this.DestroyButton.AgeTooltip.Content = "%DestroyButtonCityIsInfectedDescription";
			}
			return;
		}
		if (this.City != null && this.City.GetPropertyValue(SimulationProperties.Ownership) != 1f)
		{
			this.DestroyButton.Enable = false;
			if (this.DestroyButton.AgeTooltip != null)
			{
				this.DestroyButton.AgeTooltip.Content = "%DestroyButtonCityOwnership";
			}
			return;
		}
		bool flag = true;
		List<ConstructedBuildingGuiItem> children = this.CityImprovementsTable.GetChildren<ConstructedBuildingGuiItem>(true);
		for (int i = 0; i < children.Count; i++)
		{
			if (children[i].Toggle.State)
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			List<ConstructedBuildingGuiItem> children2 = this.RegionBuildingsTable.GetChildren<ConstructedBuildingGuiItem>(true);
			for (int j = 0; j < children2.Count; j++)
			{
				if (children2[j].Toggle.State)
				{
					flag = false;
					break;
				}
			}
		}
		this.DestroyButton.Enable = (!flag && this.interactionsAllowed && !this.IsOtherEmpire);
		if (this.DestroyButton.AgeTooltip != null && !flag)
		{
			this.DestroyButton.AgeTooltip.Content = "%DestroyButtonDescription";
			return;
		}
		this.DestroyButton.AgeTooltip.Content = "%DestroyButtonNoSelectionDescription";
	}

	private void OnDestroyCB(GameObject obj)
	{
		int num = 0;
		List<ConstructedBuildingGuiItem> children = this.CityImprovementsTable.GetChildren<ConstructedBuildingGuiItem>(true);
		for (int i = 0; i < children.Count; i++)
		{
			if (children[i].Toggle.State)
			{
				num++;
			}
		}
		List<ConstructedBuildingGuiItem> children2 = this.RegionBuildingsTable.GetChildren<ConstructedBuildingGuiItem>(true);
		for (int j = 0; j < children2.Count; j++)
		{
			if (children2[j].Toggle.State)
			{
				num++;
			}
		}
		if (num > 1)
		{
			MessagePanel.Instance.Show("%CityConfirmDestroyBuildingPlural", "%CityConfirmDestroyBuildingTitle", MessagePanelButtons.OkCancel, new MessagePanel.EventHandler(this.OnDestroyCityImprovementResult), MessagePanelType.IMPORTANT, new MessagePanelButton[0]);
		}
		else if (num == 1)
		{
			MessagePanel.Instance.Show("%CityConfirmDestroyBuildingSingle", "%CityConfirmDestroyBuildingTitle", MessagePanelButtons.OkCancel, new MessagePanel.EventHandler(this.OnDestroyCityImprovementResult), MessagePanelType.IMPORTANT, new MessagePanelButton[0]);
		}
	}

	private void OnCancelCB(GameObject obj)
	{
		this.Hide(false);
	}

	private void OnDestroyCityImprovementResult(object sender, MessagePanelResultEventArgs e)
	{
		MessagePanelResult result = e.Result;
		if (result == MessagePanelResult.Ok)
		{
			List<ConstructedBuildingGuiItem> children = this.CityImprovementsTable.GetChildren<ConstructedBuildingGuiItem>(true);
			for (int i = 0; i < children.Count; i++)
			{
				if (children[i].Toggle.State && this.playerControllerRepository != null)
				{
					base.AgeTransform.Enable = false;
					OrderDestroyCityImprovement order = new OrderDestroyCityImprovement(this.City.Empire.Index, children[i].CityImprovement);
					Ticket ticket;
					this.playerControllerRepository.ActivePlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
				}
			}
			List<ConstructedBuildingGuiItem> children2 = this.RegionBuildingsTable.GetChildren<ConstructedBuildingGuiItem>(true);
			for (int j = 0; j < children2.Count; j++)
			{
				if (children2[j].Toggle.State && this.playerControllerRepository != null)
				{
					base.AgeTransform.Enable = false;
					OrderDestroyPointOfInterestImprovement order2 = new OrderDestroyPointOfInterestImprovement(this.City.Empire.Index, children2[j].PointOfInterest);
					Ticket ticket2;
					this.playerControllerRepository.ActivePlayerController.PostOrder(order2, out ticket2, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
				}
			}
		}
		this.RefreshContent();
	}

	private void OnSwitchBuildingCB(GameObject obj)
	{
		this.RefreshButtons();
	}

	private void OnOrderResponse(object sender, TicketRaisedEventArgs args)
	{
		base.AgeTransform.Enable = true;
		this.RefreshContent();
		this.RefreshButtons();
	}

	public AgeTransform CityImprovementsTable;

	public AgeControlScrollView CityImprovementsScrollView;

	public AgeTransform RegionBuildingsTable;

	public AgeControlScrollView RegionBuildingsScrollView;

	public AgePrimitiveLabel NumberValue;

	public AgePrimitiveLabel UpkeepValue;

	public AgeTransform DestroyButton;

	public Transform BuildingPrefab;

	private List<PointOfInterest> regionBuildings;

	private IPlayerControllerRepositoryService playerControllerRepository;

	private AgeTransform.RefreshTableItem<CityImprovement> refreshCityImprovementDelegate;

	private AgeTransform.RefreshTableItem<PointOfInterest> refreshRegionBuildingDelegate;

	private IEndTurnService endTurnService;

	private bool interactionsAllowed = true;
}
