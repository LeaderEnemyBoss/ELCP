using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using UnityEngine;

public class FortressLine : SortedLine
{
	public global::Empire Empire { get; private set; }

	public global::Empire PlayerEmpire { get; set; }

	public Fortress Fortress { get; private set; }

	public IComparable Comparable { get; set; }

	public string FortressName
	{
		get
		{
			if (this.Fortress != null)
			{
				return this.FortressTitle.Text;
			}
			return "ZZZZZZZ";
		}
	}

	public string RegionName
	{
		get
		{
			if (this.Fortress != null)
			{
				return this.Fortress.Region.LocalizedName;
			}
			return "ZZZZZZZ";
		}
	}

	public int FortressFacilities
	{
		get
		{
			if (this.Fortress != null)
			{
				return this.Fortress.Facilities.Count;
			}
			return 0;
		}
	}

	public global::PlayerController PlayerController { get; private set; }

	public IWorldPositionningService WorldPositionningService { get; set; }

	private bool IsKnownByEmpirePlayer
	{
		get
		{
			if (this.Empire.Index == this.PlayerEmpire.Index)
			{
				return true;
			}
			DepartmentOfForeignAffairs agency = this.PlayerEmpire.GetAgency<DepartmentOfForeignAffairs>();
			if (agency != null)
			{
				DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(this.Empire);
				return diplomaticRelation != null && diplomaticRelation.State != null && diplomaticRelation.State.Name != DiplomaticRelationState.Names.Unknown;
			}
			return false;
		}
	}

	public void Bind(Fortress fortress, global::Empire empire, GameObject client, bool oceanicRegionList)
	{
		if (this.Fortress != null)
		{
			this.Unbind();
		}
		if (this.GarrisonShortcutButton != null)
		{
			if (oceanicRegionList)
			{
				this.GarrisonShortcutButton.OnActivateObject = base.gameObject;
				this.GarrisonShortcutButton.OnActivateMethod = "OnGarrisonShortcutCB";
				this.GarrisonShortcutButton.OnDoubleClickObject = base.gameObject;
				this.GarrisonShortcutButton.OnDoubleClickMethod = "OnFortressShortcutCB";
			}
			else
			{
				this.GarrisonShortcutButton.OnActivateObject = base.gameObject;
				this.GarrisonShortcutButton.OnActivateMethod = "OnFortressShortcutCB";
				this.GarrisonShortcutButton.OnDoubleClickObject = null;
				this.GarrisonShortcutButton.OnDoubleClickMethod = null;
			}
		}
		this.fromOceanicRegionList = oceanicRegionList;
		this.selectionClient = client;
		this.Empire = empire;
		this.Fortress = fortress;
		this.Fortress.Refreshed += this.Fortress_Refreshed;
		IGameService service = Services.GetService<IGameService>();
		this.PlayerController = service.Game.Services.GetService<IPlayerControllerRepositoryService>().ActivePlayerController;
		this.WorldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		this.updateFacilityDelegate = new AgeTransform.RefreshTableItem<PointOfInterest>(this.UpdateFacility);
	}

	public void Unbind()
	{
		this.PlayerController = null;
		this.WorldPositionningService = null;
		this.Empire = null;
		this.selectionClient = null;
		if (this.GarrisonShortcutButton != null)
		{
			this.GarrisonShortcutButton.OnActivateObject = null;
			this.GarrisonShortcutButton.OnActivateMethod = null;
			this.GarrisonShortcutButton.OnDoubleClickObject = null;
			this.GarrisonShortcutButton.OnDoubleClickMethod = null;
		}
		if (this.Fortress != null)
		{
			if (this.Fortress.Empire != null)
			{
				this.Fortress.Empire.Refreshed -= this.Simulation_Refreshed;
			}
			this.Fortress.Refreshed -= this.Fortress_Refreshed;
			this.Fortress = null;
		}
		this.updateFacilityDelegate = null;
		GuiSimulation.Instance.Clear();
	}

	public void RefreshContent()
	{
		if (this.Fortress != null)
		{
			global::IGuiService service = Services.GetService<global::IGuiService>();
			IGuiPanelHelper guiPanelHelper = service.GuiPanelHelper;
			Diagnostics.Assert(guiPanelHelper != null, "Unable to access GuiPanelHelper");
			string text = string.Empty;
			GuiElement guiElement;
			if (guiPanelHelper.TryGetGuiElement(this.Fortress.PointOfInterest.PointOfInterestDefinition.PointOfInterestTemplateName, out guiElement))
			{
				if (this.Fortress.Orientation == Fortress.CardinalOrientation.Center)
				{
					text = AgeLocalizer.Instance.LocalizeString(guiElement.Title);
				}
				else
				{
					text = AgeLocalizer.Instance.LocalizeString("%FortressGeolocalizedNameFormat").Replace("$FortressName", AgeLocalizer.Instance.LocalizeString(guiElement.Title));
					text = text.Replace("$FortressPosition", AgeLocalizer.Instance.LocalizeString("%" + this.Fortress.Orientation.ToString() + "GeolocalizationTitle"));
				}
				this.FortressTitle.Text = text;
				if (this.FortressTitle.TextLines.Count > 1)
				{
					this.FortressTitle.Text = AgeLocalizer.Instance.LocalizeString(guiElement.Title);
				}
				if (this.FortressTitle.AgeTransform.AgeTooltip != null)
				{
					this.FortressTitle.AgeTransform.AgeTooltip.Class = Fortress.Facility;
					this.FortressTitle.AgeTransform.AgeTooltip.Content = guiElement.Name;
					this.FortressTitle.AgeTransform.AgeTooltip.ClientData = this.Fortress.PointOfInterest.PointOfInterestImprovement;
				}
			}
			else
			{
				this.FortressTitle.Text = this.Fortress.PointOfInterest.PointOfInterestDefinition.PointOfInterestTemplateName;
			}
			this.PlayerEmpire = (this.PlayerController.Empire as global::Empire);
			if (this.EmpireLabel != null)
			{
				this.Set_FortressOwnerIcon(this.Fortress.Occupant, this.PlayerEmpire);
			}
			if (this.RegionLabel != null)
			{
				this.RegionLabel.Text = this.Fortress.Region.LocalizedName;
			}
			List<PointOfInterest> facilities = this.Fortress.Facilities;
			this.FacilitiesTable.ReserveChildren(facilities.Count, this.FacilityOccurencePrefab, Fortress.Facility);
			this.FacilitiesTable.RefreshChildrenIList<PointOfInterest>(facilities, this.updateFacilityDelegate, false, false);
		}
		if (this.Fortress.Occupant == this.PlayerEmpire)
		{
			if (this.GarrisonShortcutButton != null)
			{
				this.GarrisonShortcutButton.AgeTransform.Enable = true;
				if (this.GarrisonShortcutButton.AgeTransform.AgeTooltip != null)
				{
					if (this.fromOceanicRegionList)
					{
						this.GarrisonShortcutButton.AgeTransform.AgeTooltip.Content = "%FortressGarrisonShortcutDescription";
					}
					else
					{
						this.GarrisonShortcutButton.AgeTransform.AgeTooltip.Content = "%FortressLocationShortcutDescription";
					}
				}
			}
			this.SelectionToggle.AgeTransform.Enable = true;
			this.AgeTransform.Alpha = 1f;
		}
		else
		{
			if (this.GarrisonShortcutButton != null)
			{
				this.GarrisonShortcutButton.AgeTransform.Enable = false;
				if (this.GarrisonShortcutButton.AgeTransform.AgeTooltip != null)
				{
					this.GarrisonShortcutButton.AgeTransform.AgeTooltip.Content = "%FortressNotOwnedShortcutDescription";
				}
			}
			this.SelectionToggle.AgeTransform.Enable = false;
			this.AgeTransform.Alpha = 0.5f;
		}
	}

	protected virtual void OnDestroy()
	{
		this.Unbind();
	}

	private void Fortress_Refreshed(object sender)
	{
		this.RefreshContent();
	}

	private void OnFortressShortcutCB(GameObject obj)
	{
		FortressLine.CurrentFortress = this.Fortress;
		this.SelectionToggle.State = true;
		if (this.selectionClient != null)
		{
			this.selectionClient.SendMessage("OnFortressLocationShortcut");
		}
	}

	private void OnGarrisonShortcutCB(GameObject obj)
	{
		FortressLine.CurrentFortress = this.Fortress;
		this.SelectionToggle.State = true;
		if (this.selectionClient != null)
		{
			this.selectionClient.SendMessage("OnFortressGarrisonShortcut");
		}
	}

	private void OnSwitchLine(GameObject obj)
	{
		FortressLine.CurrentFortress = this.Fortress;
		this.SelectionToggle.State = true;
		if (this.selectionClient != null)
		{
			this.selectionClient.SendMessage("OnSelectFortress");
		}
	}

	private void Set_FortressOwnerIcon(global::Empire empire, global::Empire playerEmpire)
	{
		this.Empire = empire;
		this.PlayerEmpire = playerEmpire;
		if (this.Empire != null)
		{
			if (!this.IsKnownByEmpirePlayer)
			{
				this.EmpireLabel.Text = GuiEmpire.Colorize(this.Empire, "???");
			}
			else
			{
				this.EmpireLabel.Text = GuiEmpire.Colorize(this.Empire, GuiEmpire.GetFactionSymbolString(this.Empire, this.PlayerEmpire));
			}
		}
		else
		{
			this.EmpireLabel.Text = string.Empty;
			AgePrimitiveLabel empireLabel = this.EmpireLabel;
			empireLabel.Text += 'Ẑ';
		}
	}

	private void Simulation_Refreshed(object sender)
	{
		this.RefreshContent();
	}

	private void UpdateFacility(AgeTransform tableitem, PointOfInterest facilityPoint, int index)
	{
		FacilityOccurence component = tableitem.GetComponent<FacilityOccurence>();
		if (component != null)
		{
			component.RefreshContent(facilityPoint, facilityPoint.PointOfInterestImprovement != null, this.PlayerEmpire, true);
			component.AgeTransform.AgeTooltip.Anchor = this.FacilitiesTable;
		}
	}

	public void DisableIfGarrisonIsInEncounter()
	{
		if (this.Fortress != null && this.Fortress.IsInEncounter)
		{
			this.AgeTransform.Enable = false;
			this.FortressTitle.AgeTransform.AgeTooltip.Content = "%FortressLockedInBattleDescription";
		}
	}

	public static Fortress CurrentFortress;

	public AgePrimitiveLabel FortressTitle;

	public AgeTransform FacilitiesTable;

	public Transform FacilityOccurencePrefab;

	public AgePrimitiveLabel EmpireLabel;

	public AgePrimitiveLabel RegionLabel;

	public AgePrimitiveImage Backdrop;

	public AgeControlToggle SelectionToggle;

	public AgeControlButton GarrisonShortcutButton;

	private AgeTransform.RefreshTableItem<PointOfInterest> updateFacilityDelegate;

	private GameObject selectionClient;

	private bool fromOceanicRegionList;
}
