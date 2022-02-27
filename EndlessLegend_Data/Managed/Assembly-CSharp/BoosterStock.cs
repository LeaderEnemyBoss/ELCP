using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Audio;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class BoosterStock : MonoBehaviour
{
	public void Bind(global::Empire empire)
	{
		if (this.empire != null)
		{
			this.Unbind();
		}
		this.empire = empire;
	}

	public void Unbind()
	{
		this.empire = null;
	}

	public void RefreshContent()
	{
		if (this.empire == null)
		{
			Diagnostics.LogWarning("Trying to refresh a BoosterStock while the current empire is null");
			return;
		}
		this.IconImage.Image = this.guiStackedBooster.IconTexture;
		this.IconImage.TintColor = this.guiStackedBooster.IconColor;
		this.QuantityLabel.Text = GuiFormater.FormatGui(Mathf.Floor((float)this.guiStackedBooster.Quantity), false, false, false, 1);
		this.AgeTransform.AgeTooltip.Class = this.guiStackedBooster.BoosterDefinition.TooltipClass;
		this.AgeTransform.AgeTooltip.Content = this.guiStackedBooster.BoosterDefinition.Name;
		this.AgeTransform.AgeTooltip.ClientData = this.guiStackedBooster;
		this.RefreshBoosterAvailability();
	}

	public void SetContent(GuiStackedBooster guiStackedBooster)
	{
		this.guiStackedBooster = guiStackedBooster;
	}

	public void UnsetContent()
	{
		this.guiStackedBooster = null;
	}

	protected void OnActivateBooster(GameObject obj)
	{
		if (this.guiStackedBooster.BoosterDefinition == null || this.guiStackedBooster.Quantity == 0)
		{
			return;
		}
		if (this.QuickActivation && this.Guid.IsValid)
		{
			this.PostOrderBuyoutAndActivateBooster((this.guiStackedBooster.BoosterDefinition.Target != BoosterDefinition.TargetType.City) ? GameEntityGUID.Zero : this.Guid);
			return;
		}
		if (this.guiStackedBooster.BoosterDefinition.Target != BoosterDefinition.TargetType.City)
		{
			MessagePanel.Instance.Show("%ConfirmOrderBuyoutAndActivateStockpile", "%Confirmation", MessagePanelButtons.YesNo, new MessagePanel.EventHandler(this.OnPostOrderBuyoutAndActivateBooster), MessagePanelType.INFORMATIVE, new MessagePanelButton[0]);
			return;
		}
		this.ShowCitySelectionPanel();
	}

	private void OnPostOrderBuyoutAndActivateBooster(object sender, MessagePanelResultEventArgs e)
	{
		if (e.Result == MessagePanelResult.Yes && this.guiStackedBooster.BoosterDefinition.Target != BoosterDefinition.TargetType.City)
		{
			this.PostOrderBuyoutAndActivateBooster(GameEntityGUID.Zero);
		}
	}

	private void ShowCitySelectionPanel()
	{
		List<City> list = new List<City>();
		DepartmentOfTheInterior agency = this.empire.GetAgency<DepartmentOfTheInterior>();
		for (int i = 0; i < agency.Cities.Count; i++)
		{
			City city = agency.Cities[i];
			if (Booster.IsTargetValid(this.guiStackedBooster.BoosterDefinition, city))
			{
				list.Add(city);
			}
		}
		IGuiService service = Services.GetService<IGuiService>();
		Diagnostics.Assert(service != null);
		service.Show(typeof(CitySelectionModalPanel), new object[]
		{
			base.gameObject,
			"BoosterTarget",
			list.AsReadOnly()
		});
	}

	private void RefreshBoosterAvailability()
	{
		if (this.guiStackedBooster.BoosterDefinition != null && Booster.CanActivate(this.guiStackedBooster.BoosterDefinition, this.empire))
		{
			this.Background.AgeTransform.Visible = true;
			this.ActivateButton.AgeTransform.Visible = true;
			return;
		}
		this.Background.AgeTransform.Visible = false;
		this.ActivateButton.AgeTransform.Visible = false;
	}

	private void ValidateCityChoice(City city)
	{
		this.PostOrderBuyoutAndActivateBooster(city.GUID);
	}

	private void PostOrderBuyoutAndActivateBooster(GameEntityGUID targetGUID)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service != null && service.Game != null)
		{
			IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
			if (service2 != null && service2.ActivePlayerController != null)
			{
				OrderBuyoutAndActivateBooster orderBuyoutAndActivateBooster = new OrderBuyoutAndActivateBooster(service2.ActivePlayerController.Empire.Index, this.guiStackedBooster.BoosterDefinition.Name, this.guiStackedBooster.GetFirstAvailableVaultBooster().GUID, false);
				orderBuyoutAndActivateBooster.TargetGUID = targetGUID;
				service2.ActivePlayerController.PostOrder(orderBuyoutAndActivateBooster);
				Services.GetService<IAudioEventService>().Play2DEvent("Gui/Interface/BoosterStockPile");
			}
		}
	}

	public GuiStackedBooster GuiStackedBooster
	{
		get
		{
			return this.guiStackedBooster;
		}
	}

	public AgeTransform AgeTransform;

	public AgePrimitiveImage IconImage;

	public AgePrimitiveLabel QuantityLabel;

	public AgeControlButton ActivateButton;

	public AgeTransform AvailableHighlight;

	public AgePrimitiveImage Background;

	private GuiStackedBooster guiStackedBooster;

	private global::Empire empire;

	public bool QuickActivation;

	public GameEntityGUID Guid;
}
