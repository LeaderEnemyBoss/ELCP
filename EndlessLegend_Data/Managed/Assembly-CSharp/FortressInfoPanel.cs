using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using UnityEngine;

public class FortressInfoPanel : global::GuiPanel
{
	public global::Empire PlayerEmpire { get; set; }

	public Fortress Fortress
	{
		get
		{
			return this.fortress;
		}
		private set
		{
			if (this.fortress != null)
			{
				this.fortress.Refreshed -= this.Fortress_Refreshed;
			}
			this.fortress = value;
			if (this.fortress != null)
			{
				this.fortress.Refreshed += this.Fortress_Refreshed;
			}
		}
	}

	public global::PlayerController PlayerController { get; private set; }

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

	public void Bind(Fortress fortress)
	{
		GuiElement guiElement = null;
		this.Fortress = fortress;
		for (int i = 0; i < this.typesFIDS.Count; i++)
		{
			base.GuiService.GuiPanelHelper.TryGetGuiElement(this.typesFIDS[i], out guiElement, this.Fortress.Occupant.Faction.Name);
			if (guiElement != null)
			{
				ExtendedGuiElement extendedGuiElement = (ExtendedGuiElement)guiElement;
				if (i < this.TotalValuesTable.GetChildren().Count)
				{
					AgeTransform ageTransform = this.TotalValuesTable.GetChildren()[i];
					for (int j = 0; j < ageTransform.GetChildren().Count; j++)
					{
						AgeTooltip component = ageTransform.GetComponent<AgeTooltip>();
						if (component != null)
						{
							component.Class = "Simple";
							component.Content = extendedGuiElement.Description;
						}
						AgeTransform ageTransform2 = ageTransform.GetChildren()[j];
						if (ageTransform2.name == "1Symbol")
						{
							Texture2D image;
							if (base.GuiService.GuiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Small, out image))
							{
								ageTransform2.GetComponent<AgePrimitiveImage>().Image = image;
								ageTransform2.GetComponent<AgePrimitiveImage>().TintColor = extendedGuiElement.Color;
							}
							break;
						}
					}
				}
			}
			SimulationPropertyTooltipData simulationPropertyTooltipData = this.valuesFIDS[i].AgeTransform.AgeTooltip.ClientData as SimulationPropertyTooltipData;
			if (simulationPropertyTooltipData != null)
			{
				simulationPropertyTooltipData.Context = this.Fortress;
			}
		}
		IGameService service = Services.GetService<IGameService>();
		this.PlayerController = service.Game.Services.GetService<IPlayerControllerRepositoryService>().ActivePlayerController;
		this.updateFacilityDelegate = new AgeTransform.RefreshTableItem<PointOfInterest>(this.UpdateFacility);
	}

	public void Unbind()
	{
		this.PlayerController = null;
		if (this.Fortress != null)
		{
			for (int i = 0; i < this.valuesFIDS.Count; i++)
			{
				SimulationPropertyTooltipData simulationPropertyTooltipData = this.valuesFIDS[i].AgeTransform.AgeTooltip.ClientData as SimulationPropertyTooltipData;
				if (simulationPropertyTooltipData != null)
				{
					simulationPropertyTooltipData.Context = null;
				}
			}
			this.Fortress = null;
		}
		this.updateFacilityDelegate = null;
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		if (this.Fortress == null)
		{
			return;
		}
		if (this.Fortress.Occupant != null)
		{
			GuiEmpire guiEmpire = new GuiEmpire(this.Fortress.Occupant);
			this.OwningEmpireImage.Image = guiEmpire.GetImageTexture(global::GuiPanel.IconSize.LogoSmall, this.playerControllerRepositoryService.ActivePlayerController.Empire as global::Empire);
		}
		string text = string.Empty;
		this.NextStockpileLabel.AgeTransform.Visible = false;
		this.WeatherControlGroup.Visible = false;
		GuiElement guiElement;
		if (base.GuiService.GuiPanelHelper.TryGetGuiElement(this.Fortress.PointOfInterest.PointOfInterestDefinition.PointOfInterestTemplateName, out guiElement))
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
			this.CitadelTitle.Text = text;
			if (this.CitadelTitle.AgeTransform.AgeTooltip != null)
			{
				this.CitadelTitle.AgeTransform.AgeTooltip.Class = Fortress.Facility;
				this.CitadelTitle.AgeTransform.AgeTooltip.Content = guiElement.Name;
				this.CitadelTitle.AgeTransform.AgeTooltip.ClientData = this.Fortress.PointOfInterest.PointOfInterestImprovement;
			}
			Texture2D image;
			if (base.GuiService.GuiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Small, out image))
			{
				this.CitadelImage.Image = image;
				this.CitadelImage.AgeTransform.AgeTooltip.Copy(this.CitadelTitle.AgeTransform.AgeTooltip);
			}
		}
		else
		{
			this.CitadelTitle.Text = this.Fortress.PointOfInterest.PointOfInterestDefinition.PointOfInterestTemplateName;
		}
		List<PointOfInterest> facilities = this.Fortress.Facilities;
		for (int i = 0; i < facilities.Count; i++)
		{
			if (facilities[i].SimulationObject.Tags.Contains(Fortress.UniqueFacilityNames.StockPile1) || facilities[i].SimulationObject.Tags.Contains(Fortress.UniqueFacilityNames.StockPile2) || facilities[i].SimulationObject.Tags.Contains(Fortress.UniqueFacilityNames.StockPile3))
			{
				this.NextStockpileLabel.AgeTransform.Visible = true;
				int num;
				if (this.Fortress.Region.NavalEmpire.GetAgency<PirateCouncil>().Stockpiles.TryGetValue(facilities[i].GUID, out num))
				{
					if (num > 1)
					{
						this.NextStockpileLabel.Text = AgeLocalizer.Instance.LocalizeString("%FortressInfoNextStockpileGenerationFormatPlural").Replace("$Timer", num.ToString());
					}
					else
					{
						this.NextStockpileLabel.Text = AgeLocalizer.Instance.LocalizeString("%FortressInfoNextStockpileGenerationFormatSingle").Replace("$Timer", num.ToString());
					}
				}
				else
				{
					this.NextStockpileLabel.Text = AgeLocalizer.Instance.LocalizeString("%FortressInfoNextStockpileGenerationFormatPlural").Replace("$Timer", "???");
				}
			}
			if (facilities[i].SimulationObject.Tags.Contains(Fortress.UniqueFacilityNames.WeatherControl) && this.weatherService != null)
			{
				this.WeatherControlGroup.Visible = true;
				List<string> list = new List<string>();
				List<string> list2 = new List<string>();
				list.Add("%FortressInfoWeatherControlNoPresetTitle");
				list2.Add("%FortressInfoWeatherControlNoPresetDescription");
				bool flag = this.weatherService.WeatherControlCooldown <= 0 || this.weatherService.WeatherControlStartTurn == (base.Game as global::Game).Turn + 1;
				int weatherControlTurnDurationFor = this.weatherService.GetWeatherControlTurnDurationFor(this.fortress.Occupant);
				string newValue = string.Empty;
				for (int j = 0; j < this.presetNames.Count; j++)
				{
					GuiElement guiElement2;
					if (base.GuiService.GuiPanelHelper.TryGetGuiElement("Weather" + this.presetNames[j], out guiElement2))
					{
						list.Add(guiElement2.Title);
						if (!flag)
						{
							newValue = (weatherControlTurnDurationFor - ((base.Game as global::Game).Turn + 1 - this.weatherService.WeatherControlStartTurn)).ToString();
						}
						else
						{
							newValue = weatherControlTurnDurationFor.ToString();
						}
						list2.Add(AgeLocalizer.Instance.LocalizeString(guiElement2.Description).Replace("$Timer", newValue));
					}
				}
				this.WeatherControlDroplist.ItemTable = list.ToArray();
				this.WeatherControlDroplist.TooltipTable = list2.ToArray();
				if (this.presetNames.Contains(this.weatherService.PresetName))
				{
					this.WeatherControlDroplist.SelectedItem = this.presetNames.IndexOf(this.weatherService.PresetName) + 1;
				}
				else
				{
					this.WeatherControlDroplist.SelectedItem = 0;
				}
				if (flag)
				{
					this.WeatherControlLabel.Text = "%FortressInfoWeatherControlUseTitle";
					this.WeatherControlLabel.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%FortressInfoWeatherControlUseDescription").Replace("$Timer", weatherControlTurnDurationFor.ToString());
					this.WeatherControlDroplist.AgeTransform.Enable = true;
				}
				else if (this.weatherService.WeatherControlCooldown > 0)
				{
					this.WeatherControlDroplist.AgeTransform.Enable = false;
					this.WeatherControlLabel.Text = AgeLocalizer.Instance.LocalizeString("%FortressInfoWeatherControlEndOfCooldownFormatSingle").Replace("$Timer", this.weatherService.WeatherControlCooldown.ToString());
					this.WeatherControlLabel.AgeTransform.AgeTooltip.Content = "%FortressInfoWeatherControlEndOfCooldownDescription";
					if (this.weatherService.WeatherControlCooldown > 1)
					{
						this.WeatherControlLabel.Text = AgeLocalizer.Instance.LocalizeString("%FortressInfoWeatherControlEndOfCooldownFormatPlural").Replace("$Timer", this.weatherService.WeatherControlCooldown.ToString());
					}
				}
			}
		}
		for (int k = 0; k < this.typesFIDS.Count; k++)
		{
			if (k < this.valuesFIDS.Count)
			{
				this.valuesFIDS[k].AgeTransform.Alpha = 1f;
				this.valuesFIDS[k].AgeTransform.ResetAllModifiers(true, false);
				float propertyValue = this.Fortress.GetPropertyValue(this.typesFIDS[k]);
				this.valuesFIDS[k].Text = GuiFormater.FormatGui(propertyValue, false, false, false, 0);
			}
		}
		this.PlayerEmpire = (this.PlayerController.Empire as global::Empire);
		this.FacilitiesTable.ReserveChildren(facilities.Count, this.FacilityOccurencePrefab, Fortress.Facility);
		this.FacilitiesTable.RefreshChildrenIList<PointOfInterest>(facilities, this.updateFacilityDelegate, false, false);
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		this.valuesFIDS = new List<AgePrimitiveLabel>();
		this.typesFIDS = new List<StaticString>();
		this.typesFIDS.Add(SimulationProperties.FortressTileDust);
		this.typesFIDS.Add(SimulationProperties.FortressTileEmpirePoint);
		this.typesFIDS.Add(SimulationProperties.FortressTileScience);
		for (int i = 0; i < this.typesFIDS.Count; i++)
		{
			GuiElement guiElement;
			base.GuiService.GuiPanelHelper.TryGetGuiElement(this.typesFIDS[i], out guiElement);
			if (guiElement != null)
			{
				ExtendedGuiElement extendedGuiElement = (ExtendedGuiElement)guiElement;
				if (i < this.TotalValuesTable.GetChildren().Count)
				{
					AgeTransform element = this.TotalValuesTable.GetChildren()[i];
					for (int j = 0; j < element.GetChildren().Count; j++)
					{
						AgeTransform child = element.GetChildren()[j];
						if (child.name == "1Symbol")
						{
							Texture2D texture;
							if (base.GuiService.GuiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Small, out texture))
							{
								child.GetComponent<AgePrimitiveImage>().Image = texture;
								child.GetComponent<AgePrimitiveImage>().TintColor = extendedGuiElement.Color;
							}
						}
						else if (child.name == "2Value")
						{
							this.valuesFIDS.Add(child.GetComponent<AgePrimitiveLabel>());
							child.GetComponent<AgePrimitiveLabel>().TintColor = extendedGuiElement.Color;
						}
					}
				}
			}
		}
		Diagnostics.Assert(this.typesFIDS.Count == this.valuesFIDS.Count, "Fortress Info Panel: Invalid number of value FIDS");
		for (int k = 0; k < this.valuesFIDS.Count; k++)
		{
			if (this.valuesFIDS[k].AgeTransform.AgeTooltip == null)
			{
				break;
			}
			this.valuesFIDS[k].AgeTransform.AgeTooltip.Class = "FIDS";
			this.valuesFIDS[k].AgeTransform.AgeTooltip.Content = this.typesFIDS[k];
			this.valuesFIDS[k].AgeTransform.AgeTooltip.ClientData = new SimulationPropertyTooltipData(this.typesFIDS[k], this.typesFIDS[k], null);
		}
		yield break;
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		this.playerControllerRepositoryService = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
		Diagnostics.Assert(this.playerControllerRepositoryService != null);
		this.EndTurnService = Services.GetService<IEndTurnService>();
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null && service.Game != null);
		this.weatherService = service.Game.Services.GetService<IWeatherService>();
		this.presetNames = new List<StaticString>();
		this.weatherService.FillPresetNames(ref this.presetNames);
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		this.TotalValuesTable.Visible = true;
		yield break;
	}

	protected override void OnUnload()
	{
		for (int i = 0; i < this.valuesFIDS.Count; i++)
		{
			if (this.valuesFIDS[i].AgeTransform.AgeTooltip == null)
			{
				break;
			}
			this.valuesFIDS[i].AgeTransform.AgeTooltip.ClientData = null;
		}
		this.valuesFIDS = null;
		this.typesFIDS = null;
		base.OnUnload();
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.Unbind();
		this.EndTurnService = null;
		this.playerControllerRepositoryService = null;
		this.weatherService = null;
		this.presetNames = null;
		base.OnUnloadGame(game);
	}

	private void EndTurnService_GameClientStateChange(object sender, GameClientStateChangeEventArgs e)
	{
		if (base.IsVisible && this.playerControllerRepositoryService != null && this.playerControllerRepositoryService.ActivePlayerController != null && this.Fortress != null)
		{
			this.RefreshContent();
		}
	}

	private void Fortress_Refreshed(object sender)
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

	private void OnChangeWeatherPresetCB(GameObject gameObject)
	{
		IPlayerControllerRepositoryService service = base.GameService.Game.Services.GetService<IPlayerControllerRepositoryService>();
		if (service != null)
		{
			string presetName = string.Empty;
			if (this.WeatherControlDroplist.SelectedItem > 0)
			{
				presetName = this.presetNames[this.WeatherControlDroplist.SelectedItem - 1];
			}
			OrderActivateWeatherControl order = new OrderActivateWeatherControl(service.ActivePlayerController.Empire.Index, presetName);
			service.ActivePlayerController.PostOrder(order);
		}
	}

	public AgePrimitiveImage OwningEmpireImage;

	public AgePrimitiveLabel CitadelTitle;

	public AgePrimitiveImage CitadelImage;

	public AgePrimitiveLabel NextStockpileLabel;

	public AgeTransform WeatherControlGroup;

	public AgePrimitiveLabel WeatherControlLabel;

	public AgeControlDropList WeatherControlDroplist;

	public AgeTransform FacilitiesTable;

	public AgeTransform TotalValuesTable;

	public Transform FacilityOccurencePrefab;

	private List<StaticString> typesFIDS;

	private List<AgePrimitiveLabel> valuesFIDS;

	private AgeTransform.RefreshTableItem<PointOfInterest> updateFacilityDelegate;

	private IPlayerControllerRepositoryService playerControllerRepositoryService;

	private IEndTurnService endTurnService;

	private IWeatherService weatherService;

	private Fortress fortress;

	private List<StaticString> presetNames;
}
