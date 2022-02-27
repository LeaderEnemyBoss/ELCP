using System;
using System.Collections.Generic;
using System.ComponentModel;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Simulation;
using UnityEngine;

public class CityLabel : MonoBehaviour
{
	public City City
	{
		get
		{
			return this.city;
		}
		private set
		{
			if (this.city != null)
			{
				this.city.Refreshed -= this.City_Refreshed;
				if (this.city.Region != null)
				{
					this.city.Region.UserDefinedNameChange -= this.Region_UserDefinedNameChange;
				}
			}
			this.city = value;
			if (this.city != null)
			{
				Diagnostics.Assert(this.city.Region != null);
				this.city.Refreshed += this.City_Refreshed;
				this.city.Region.UserDefinedNameChange += this.Region_UserDefinedNameChange;
			}
		}
	}

	public GuiEmpire GuiEmpire
	{
		get
		{
			return this.guiEmpire;
		}
		private set
		{
			if (this.guiEmpire != null)
			{
				this.guiEmpire.Empire.Refreshed -= this.Empire_Refreshed;
			}
			this.guiEmpire = value;
			if (this.guiEmpire != null)
			{
				this.guiEmpire.Empire.Refreshed += this.Empire_Refreshed;
			}
		}
	}

	public GuiEmpire GuiPreviousEmpire
	{
		get
		{
			return this.guiPreviousEmpire;
		}
		private set
		{
			this.guiPreviousEmpire = value;
		}
	}

	public ConstructionQueue ConstructionQueue
	{
		get
		{
			return this.constructionQueue;
		}
		private set
		{
			if (this.constructionQueue != null)
			{
				this.constructionQueue.CollectionChanged -= this.ConstructionQueue_CollectionChanged;
			}
			this.constructionQueue = value;
			if (this.constructionQueue != null)
			{
				this.constructionQueue.CollectionChanged += this.ConstructionQueue_CollectionChanged;
			}
		}
	}

	public float Height
	{
		get
		{
			return this.AgeTransform.Height;
		}
	}

	public float Width
	{
		get
		{
			return this.AgeTransform.Width;
		}
	}

	public float LeftOffset
	{
		get
		{
			return this.PinLine.AgeTransform.X;
		}
	}

	public bool IsInScreen
	{
		get
		{
			return this.WantedPosition.x + this.Width >= 0f && this.WantedPosition.x <= (float)Screen.width && this.WantedPosition.y + this.AgeTransform.Height >= 0f && this.WantedPosition.y <= (float)Screen.height;
		}
	}

	public void Bind(City city, IGuiPanelHelper helper)
	{
		this.Unbind();
		this.City = city;
		this.GuiEmpire = new GuiEmpire(this.City.Empire);
		DepartmentOfIndustry agency = this.City.Empire.GetAgency<DepartmentOfIndustry>();
		this.ConstructionQueue = agency.GetConstructionQueue(this.City);
		this.guiPanelHelper = helper;
		IGameService service = Services.GetService<IGameService>();
		this.game = (service.Game as global::Game);
		this.playerControllerRepository = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
		this.regionalEffectsService = service.Game.Services.GetService<IRegionalEffectsService>();
		this.PanelSpying.Visible = false;
		this.AgeTransform.Height = this.ModifierPosition.StartHeight;
		IDownloadableContentService service2 = Services.GetService<IDownloadableContentService>();
		if (service2 != null && service2.IsShared(DownloadableContent11.ReadOnlyName))
		{
			this.PanelSpying.Visible = true;
			this.AgeTransform.Height = this.ModifierPosition.EndHeight;
		}
		this.PanelOvergrownCity.Visible = false;
		if (service2 != null && service2.IsShared(DownloadableContent20.ReadOnlyName) && this.City.IsInfected)
		{
			this.PanelSpying.Visible = false;
			this.PanelOvergrownCity.Visible = true;
			this.AgeTransform.Height = this.ModifierPosition.EndHeight;
			this.GuiPreviousEmpire = new GuiEmpire(this.City.LastNonInfectedOwner);
			if (this.GuiPreviousEmpire != null)
			{
				this.PreviousEmpireFactionIcon.TintColor = this.GuiPreviousEmpire.Color;
				this.PreviousEmpireFactionIcon.Image = this.GuiPreviousEmpire.GuiFaction.GetImageTexture(global::GuiPanel.IconSize.LogoSmall, true);
				this.PreviousEmpireFactionTooltip.Class = "Descriptor";
				this.PreviousEmpireFactionTooltip.ClientData = this.GuiEmpire.Empire;
				Faction faction = this.GuiPreviousEmpire.Empire.Faction;
				if (faction != null)
				{
					new List<string>();
					foreach (SimulationDescriptor simulationDescriptor in faction.GetIntegrationDescriptors())
					{
						this.PreviousEmpireFactionTooltip.Content = simulationDescriptor.Name;
					}
				}
			}
		}
		this.infiltrationCostResourceName = DepartmentOfTheTreasury.Resources.InfiltrationCost;
		ResourceDefinition resourceDefinition;
		if (Databases.GetDatabase<ResourceDefinition>(false).TryGetValue(DepartmentOfTheTreasury.Resources.InfiltrationCost, out resourceDefinition))
		{
			this.infiltrationCostResourceName = resourceDefinition.GetName(this.playerControllerRepository.ActivePlayerController.Empire);
		}
	}

	public void Unbind()
	{
		this.playerControllerRepository = null;
		this.guiPanelHelper = null;
		this.ConstructionQueue = null;
		this.HeroCard.Unbind();
		this.GuiEmpire = null;
		this.GuiPreviousEmpire = null;
		this.City = null;
	}

	public void RefreshContent()
	{
		if (this.City == null)
		{
			return;
		}
		if (this.City.Empire == null)
		{
			return;
		}
		if (this.GuiEmpire.Index != this.City.Empire.Index)
		{
			if (this.guiEmpire == null)
			{
				this.Unbind();
				return;
			}
			this.GuiEmpire = new GuiEmpire(this.City.Empire);
			DepartmentOfIndustry agency = this.City.Empire.GetAgency<DepartmentOfIndustry>();
			this.ConstructionQueue = agency.GetConstructionQueue(this.City);
		}
		if (this.GuiEmpire != null)
		{
			this.FactionBackground.TintColor = this.GuiEmpire.Color;
			this.PopulationSymbol.TintColor = this.GuiEmpire.Color;
			this.FactionSymbol.TintColor = this.GuiEmpire.Color;
			this.PinLine.TintColor = this.GuiEmpire.Color;
			this.PopulationNumber.Text = GuiFormater.FormatGui(this.City.GetPropertyValue(SimulationProperties.Population), false, false, false, 1);
			this.FactionSymbol.Image = this.GuiEmpire.GuiFaction.GetImageTexture(global::GuiPanel.IconSize.LogoSmall, true);
		}
		string content = "%CityCurrentConstructionDescription";
		if (this.City.IsInfected)
		{
			this.PanelSpying.Visible = false;
			this.PanelOvergrownCity.Visible = true;
			this.AgeTransform.Height = this.ModifierPosition.EndHeight;
			if (this.GuiPreviousEmpire == null)
			{
				this.GuiPreviousEmpire = new GuiEmpire(this.City.LastNonInfectedOwner);
			}
			if (this.GuiPreviousEmpire != null)
			{
				this.PreviousEmpireFactionIcon.TintColor = this.GuiPreviousEmpire.Color;
				this.PreviousEmpireFactionIcon.Image = this.GuiPreviousEmpire.GuiFaction.GetImageTexture(global::GuiPanel.IconSize.LogoSmall, true);
				this.PreviousEmpireFactionTooltip.Class = "Descriptor";
				this.PreviousEmpireFactionTooltip.ClientData = this.GuiEmpire.Empire;
				Faction faction = this.GuiPreviousEmpire.Empire.Faction;
				if (faction != null)
				{
					new List<string>();
					foreach (SimulationDescriptor simulationDescriptor in faction.GetIntegrationDescriptors())
					{
						this.PreviousEmpireFactionTooltip.Content = simulationDescriptor.Name;
					}
				}
			}
			if (this.ConstructionQueue.PendingConstructions.Count > 0)
			{
				Construction construction = this.ConstructionQueue.Peek();
				if (construction.ConstructibleElement.SubCategory == "SubCategoryAssimilation" && construction.ConstructibleElement.Name != "CityConstructibleActionInfectedRaze")
				{
					content = "%IntegratingFactionUnderConstructionDescription";
				}
			}
		}
		this.ConstructionGroup.AgeTooltip.Content = content;
		this.RefreshCityName();
		DepartmentOfIntelligence agency2 = this.playerControllerRepository.ActivePlayerController.Empire.GetAgency<DepartmentOfIntelligence>();
		bool flag = this.playerControllerRepository.ActivePlayerController.Empire.SimulationObject.Tags.Contains(global::Empire.TagEmpireEliminated);
		if (flag && ELCPUtilities.SpectatorSpyFocus >= 0)
		{
			agency2 = this.game.Empires[ELCPUtilities.SpectatorSpyFocus].GetAgency<DepartmentOfIntelligence>();
		}
		Unit hero = null;
		InfiltrationProcessus.InfiltrationState infiltrationState = InfiltrationProcessus.InfiltrationState.None;
		bool flag2 = false;
		if (this.playerControllerRepository != null)
		{
			if (this.City.Empire == this.playerControllerRepository.ActivePlayerController.Empire)
			{
				flag2 = true;
			}
			else if (this.PanelSpying.Visible && agency2 != null && agency2.TryGetSpyOnGarrison(this.City, out hero, out infiltrationState) && infiltrationState == InfiltrationProcessus.InfiltrationState.Infiltrated)
			{
				flag2 = true;
			}
			else if (flag)
			{
				flag2 = true;
			}
		}
		if (!flag2)
		{
			if (this.City.Empire == null)
			{
				this.CompetitorTitle.Text = string.Empty;
			}
			else
			{
				DiplomaticRelation diplomaticRelation = this.playerControllerRepository.ActivePlayerController.Empire.GetAgency<DepartmentOfForeignAffairs>().GetDiplomaticRelation(this.City.Empire);
				Diagnostics.Assert(diplomaticRelation != null);
				if (diplomaticRelation.State != null && diplomaticRelation.State.Name == DiplomaticRelationState.Names.Alliance)
				{
					AgeUtils.TruncateString(AgeLocalizer.Instance.LocalizeString("%CityLabelAlliedTitle"), this.CompetitorTitle, out this.temp, '.');
					this.CompetitorTitle.Text = this.temp;
				}
				else if (diplomaticRelation.State != null && diplomaticRelation.State.Name == DiplomaticRelationState.Names.Peace)
				{
					AgeUtils.TruncateString(AgeLocalizer.Instance.LocalizeString("%CityLabelFriendlyTitle"), this.CompetitorTitle, out this.temp, '.');
					this.CompetitorTitle.Text = this.temp;
				}
				else
				{
					AgeUtils.TruncateString(AgeLocalizer.Instance.LocalizeString("%CityLabelEnemyTitle"), this.CompetitorTitle, out this.temp, '.');
					this.CompetitorTitle.Text = this.temp;
				}
			}
		}
		this.SelectionButton.AgeTransform.Enable = flag2;
		this.PlayerSpecificGroup.Visible = flag2;
		this.CompetitorSpecificGroup.Visible = !flag2;
		float propertyValue = this.City.GetPropertyValue(SimulationProperties.CityDefensePoint);
		float propertyValue2 = this.City.GetPropertyValue(SimulationProperties.MaximumCityDefensePoint);
		float propertyValue3 = this.City.GetPropertyValue(SimulationProperties.CityDefensePointRecoveryPerTurn);
		float propertyValue4 = this.City.GetPropertyValue(SimulationProperties.Ownership);
		Diagnostics.Assert(this.UnitNumber != null);
		Diagnostics.Assert(this.City.StandardUnits != null);
		int num = (this.City.Militia == null) ? 0 : this.City.Militia.StandardUnits.Count;
		this.UnitNumber.Text = GuiFormater.FormatGui(this.City.StandardUnits.Count + num);
		this.DefenseNumber.Text = GuiFormater.FormatStock(propertyValue, SimulationProperties.CityDefensePoint, 0, true);
		float propertyValue5 = this.City.GetPropertyValue(SimulationProperties.DefensivePower);
		this.DefenseGroup.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%CityDefenseDescription");
		this.DefenseIcon.Image = AgeManager.Instance.FindDynamicTexture("fortificationCityLabel", false);
		if (propertyValue5 > 0f)
		{
			this.DefenseGroup.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%CityDefenseWithDefensivePowerDescription").Replace("$DefensivePowerValue", GuiFormater.FormatGui(propertyValue5, false, true, false, 1));
			this.DefenseIcon.Image = AgeManager.Instance.FindDynamicTexture("retaliationCityLabel", false);
		}
		this.DefenseGroup.AgeTooltip.Content = this.DefenseGroup.AgeTooltip.Content.Replace("$CityDefenseValue", GuiFormater.FormatGui(propertyValue, false, true, false, 1));
		if (propertyValue4 < 1f && !this.City.IsInfected)
		{
			this.OwnershipPercentage.AgeTransform.Visible = true;
			this.OwnershipPercentage.Text = GuiFormater.FormatGui(propertyValue4, true, false, false, 1);
		}
		else
		{
			this.OwnershipPercentage.AgeTransform.Visible = false;
		}
		if (this.City.BesiegingEmpire != null || this.City.BesiegingSeafaringArmies.Count != 0 || this.City.IsUnderEarthquake)
		{
			float num2 = DepartmentOfTheInterior.GetBesiegingPower(this.City, true);
			num2 += DepartmentOfTheInterior.GetCityPointEarthquakeDamage(this.City);
			this.DefenseTendency.Text = "(" + GuiFormater.FormatGui(-num2, false, true, true, 1) + ")";
		}
		else if (propertyValue < propertyValue2)
		{
			this.DefenseTendency.Text = "(" + GuiFormater.FormatGui(propertyValue3, false, true, true, 1) + ")";
		}
		else
		{
			this.DefenseTendency.Text = string.Empty;
		}
		if (flag2)
		{
			Construction construction2 = null;
			if (this.ConstructionQueue != null)
			{
				construction2 = this.ConstructionQueue.Peek();
			}
			if (this.City.IsInfected && construction2 == null)
			{
				this.PlayerSpecificGroup.Visible = false;
			}
			if (construction2 != null)
			{
				IImageFeatureProvider imageFeatureProvider = construction2.ConstructibleElement as IImageFeatureProvider;
				GuiElement guiElement;
				if (imageFeatureProvider != null)
				{
					Texture2D image;
					if (imageFeatureProvider.TryGetTextureFromIcon(global::GuiPanel.IconSize.Small, out image))
					{
						this.ConstructionImage.Image = image;
					}
				}
				else if (this.guiPanelHelper.TryGetGuiElement(construction2.ConstructibleElement.Name, out guiElement))
				{
					Texture2D image2;
					if (this.guiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Small, out image2))
					{
						this.ConstructionImage.Image = image2;
					}
				}
				else
				{
					this.ConstructionImage.Image = null;
				}
				CityLabel.emptyList.Clear();
				int b;
				float num3;
				float num4;
				bool flag3;
				QueueGuiItem.GetConstructionTurnInfos(this.City, construction2, CityLabel.emptyList, out b, out num3, out num4, out flag3);
				int numberOfTurn = Mathf.Max(1, b);
				this.ConstructionTurns.Text = QueueGuiItem.FormatNumberOfTurns(numberOfTurn);
			}
			else
			{
				this.ConstructionImage.Image = this.NoConstructionTexture;
				this.ConstructionTurns.Text = "%EmptyConstructionTitle";
			}
		}
		if (this.PanelSpying.Visible && this.playerControllerRepository != null && agency2 != null)
		{
			float value = this.City.GetPropertyValue(SimulationProperties.CityAntiSpy) * 0.01f;
			this.AntiSpyValue.Text = GuiFormater.FormatGui(value, true, true, false, 0);
			this.HeroGroup.Visible = false;
			this.SpyingEffectsGroup.Visible = false;
			this.AssignSpyButton.AgeTransform.Visible = false;
			if (this.City.Empire != this.playerControllerRepository.ActivePlayerController.Empire)
			{
				if (infiltrationState != InfiltrationProcessus.InfiltrationState.None)
				{
					this.HeroGroup.Visible = true;
					this.HeroCard.Bind(hero, this.playerControllerRepository.ActivePlayerController.Empire as global::Empire, null);
					this.HeroCard.RefreshContent(false, false);
					if (this.HeroCard.GuiHero != null)
					{
						AgeTooltip ageTooltip = this.HeroCard.HeroPortrait.AgeTransform.AgeTooltip;
						if (ageTooltip != null)
						{
							ageTooltip.Class = "Unit";
							ageTooltip.ClientData = this.HeroCard.GuiHero;
							ageTooltip.Content = this.HeroCard.GuiHero.Title;
						}
					}
					if (infiltrationState == InfiltrationProcessus.InfiltrationState.Infiltrated)
					{
						this.InfiltrationTurnSymbol.AgeTransform.Visible = false;
						this.InfiltrationTurnValue.AgeTransform.Visible = false;
						this.HeroCard.AgeTransform.Alpha = 1f;
						this.SpyingEffectsGroup.Visible = true;
					}
					else
					{
						this.HeroCard.AgeTransform.Alpha = 0.75f;
						this.InfiltrationTurnSymbol.AgeTransform.Visible = true;
						this.InfiltrationTurnValue.AgeTransform.Visible = true;
						int value2 = DepartmentOfIntelligence.InfiltrationRemainingTurns(hero);
						this.InfiltrationTurnValue.Text = GuiFormater.FormatGui(value2);
					}
				}
				else if (!flag)
				{
					this.AssignSpyButton.AgeTransform.Visible = true;
					float value3 = 0f;
					this.failures.Clear();
					bool flag4 = agency2.CanBeInfiltrate(this.City, out value3, this.failures);
					bool flag5 = agency2.Empire.GetAgency<DepartmentOfEducation>().Heroes.Count < 1;
					if (flag5)
					{
						flag4 = false;
					}
					this.AssignSpyButton.AgeTransform.Enable = flag4;
					global::IGuiService service = Services.GetService<global::IGuiService>();
					string str = string.Empty;
					if (this.failures.Contains(DepartmentOfIntelligence.InfiltrationTargetFailureNotAffordable))
					{
						str = "#c52222#";
					}
					string arg = str + GuiFormater.FormatGui(value3, false, true, false, 0) + service.FormatSymbol(this.infiltrationCostResourceName);
					string format = AgeLocalizer.Instance.LocalizeString("%EspionageLabelAssignSpyTitle");
					this.AssignTitle.Text = string.Format(format, arg);
					AgeTooltip ageTooltip2 = this.AssignSpyButton.AgeTransform.AgeTooltip;
					if (ageTooltip2)
					{
						if (flag4)
						{
							ageTooltip2.Content = "%EspionageLabelAssignSpyDescription";
						}
						else if (flag5)
						{
							ageTooltip2.Content = "%EspionageLabelAssignSpyNoHeroesDescription";
						}
						else if (this.failures.Contains(DepartmentOfIntelligence.InfiltrationTargetFailureNotVisible))
						{
							ageTooltip2.Content = "%EspionageLabelAssignSpyNoVisibilityDescription";
						}
						else if (this.failures.Contains(DepartmentOfIntelligence.InfiltrationTargetFailureSiege))
						{
							ageTooltip2.Content = "%EspionageLabelAssignSpyUnderSiegeDescription";
						}
						else if (this.failures.Contains(DepartmentOfIntelligence.InfiltrationTargetFailureNotAffordable))
						{
							ageTooltip2.Content = "%EspionageLabelAssignSpyNotAffordableDescription";
						}
						else if (this.failures.Contains(DepartmentOfIntelligence.InfiltrationTargetFailureAlreadyInfiltrated))
						{
							ageTooltip2.Content = "%EspionageLabelAssignSpyAlreadyInfiltratedDescription";
						}
						else if (this.failures.Contains(DepartmentOfIntelligence.InfiltrationTargetFailureCityInfected))
						{
							ageTooltip2.Content = "%EspionageLabelAssignSpyCityInfectedDescription";
						}
					}
				}
			}
		}
		bool flag6 = false;
		List<Kaiju> list = new List<Kaiju>();
		foreach (RegionalEffect regionalEffect in this.city.GetRegionalEffects())
		{
			IRegionalEffectsProviderGameEntity regionalEffectsProviderGameEntity = null;
			if (this.regionalEffectsService.TryGetEffectOwner(regionalEffect.GUID, out regionalEffectsProviderGameEntity) && regionalEffectsProviderGameEntity.GetRegionalEffectsProviderContext().SimulationObject.Tags.Contains("ClassKaiju"))
			{
				flag6 = true;
				if (regionalEffectsProviderGameEntity is Kaiju)
				{
					Kaiju item = regionalEffectsProviderGameEntity as Kaiju;
					if (!list.Contains(item))
					{
						list.Add(item);
					}
				}
			}
		}
		if (flag6)
		{
			AgeTransform ageTransform = this.KaijuIcon.AgeTransform;
			ageTransform.Visible = true;
			ageTransform.Enable = true;
			this.PopulationGroup.PercentBottom = 100f - (ageTransform.Height + ageTransform.PixelMarginBottom) / ageTransform.GetParent().Height * 100f;
			KaijuInfluenceInCityTooltipData clientData = new KaijuInfluenceInCityTooltipData(this.City, list);
			this.KaijuIcon.AgeTransform.AgeTooltip.Content = "%CityKaijuAffectedDescription";
			this.KaijuIcon.AgeTransform.AgeTooltip.Class = "Kaijus";
			this.KaijuIcon.AgeTransform.AgeTooltip.ClientData = clientData;
			return;
		}
		this.KaijuIcon.AgeTransform.Visible = false;
		this.KaijuIcon.AgeTransform.Enable = false;
		this.PopulationGroup.PercentBottom = 100f;
	}

	private void OnDestroy()
	{
		this.Unbind();
	}

	private void OnAssignSpyCB()
	{
		global::IGuiService service = Services.GetService<global::IGuiService>();
		service.Show(typeof(HeroSelectionModalPanel), new object[]
		{
			base.gameObject,
			null,
			true
		});
	}

	private void OnViewEffectsCB()
	{
		global::IGuiService service = Services.GetService<global::IGuiService>();
		service.Show(typeof(ViewEffectsModalPanel), new object[]
		{
			base.gameObject,
			true
		});
	}

	private void OnSelectCityCB()
	{
		IViewService service = Services.GetService<IViewService>();
		if (service != null)
		{
			service.SelectAndCenter(this.City, true);
		}
	}

	private void OnMouseEnterCB()
	{
		IMouseCursorService service = Services.GetService<IMouseCursorService>();
		Diagnostics.Assert(service != null);
		service.AddKey(base.GetType().ToString());
	}

	private void OnMouseLeaveCB()
	{
		IMouseCursorService service = Services.GetService<IMouseCursorService>();
		Diagnostics.Assert(service != null);
		service.RemoveKey(base.GetType().ToString());
	}

	private void Region_UserDefinedNameChange(object sender, EventArgs e)
	{
		this.RefreshCityName();
	}

	private void City_Refreshed(object sender)
	{
		this.RefreshContent();
	}

	private void Empire_Refreshed(object sender)
	{
		this.RefreshContent();
	}

	private void ConstructionQueue_CollectionChanged(object sender, CollectionChangeEventArgs e)
	{
		this.RefreshContent();
	}

	private void RefreshCityName()
	{
		if (this.City.Region != null)
		{
			this.CityName.Text = this.City.Region.LocalizedName;
		}
		DepartmentOfTheInterior agency = this.City.Empire.GetAgency<DepartmentOfTheInterior>();
		if (agency != null && agency.MainCityGUID == this.City.GUID)
		{
			this.CityName.Text = AgeLocalizer.Instance.LocalizeString("%MainCitySymbol") + this.CityName.Text;
		}
	}

	private void ValidateHeroChoice(Unit hero)
	{
		OrderToggleInfiltration order = new OrderToggleInfiltration(this.playerControllerRepository.ActivePlayerController.Empire.Index, hero.GUID, this.City.GUID, false, true);
		Ticket ticket;
		this.playerControllerRepository.ActivePlayerController.PostOrder(order, out ticket, null);
	}

	public Texture2D NoConstructionTexture;

	public AgeTransform AgeTransform;

	public AgeModifierPosition ModifierPosition;

	public AgeControlButton SelectionButton;

	public AgePrimitiveImage FactionBackground;

	public AgeTransform PopulationGroup;

	public AgePrimitiveImage PopulationSymbol;

	public AgePrimitiveLabel PopulationNumber;

	public AgePrimitiveImage KaijuIcon;

	public AgePrimitiveLabel OwnershipPercentage;

	public AgePrimitiveImage FactionSymbol;

	public AgePrimitiveImage PinLine;

	public AgePrimitiveLabel CityName;

	public AgePrimitiveLabel UnitNumber;

	public AgeTransform DefenseGroup;

	public AgePrimitiveImage DefenseIcon;

	public AgePrimitiveLabel DefenseNumber;

	public AgePrimitiveLabel DefenseTendency;

	public AgeTransform PlayerSpecificGroup;

	public AgeTransform ConstructionGroup;

	public AgePrimitiveImage ConstructionImage;

	public AgePrimitiveLabel ConstructionTurns;

	public AgeTransform CompetitorSpecificGroup;

	public AgePrimitiveLabel CompetitorTitle;

	public AgeTransform PanelSpying;

	public AgeTransform PanelOvergrownCity;

	public AgePrimitiveImage PreviousEmpireFactionIcon;

	public AgeTooltip PreviousEmpireFactionTooltip;

	public AgeTransform AntiSpyGroup;

	public AgePrimitiveLabel AntiSpyValue;

	public AgeTransform HeroGroup;

	public HeroCard HeroCard;

	public AgePrimitiveImage InfiltrationTurnSymbol;

	public AgePrimitiveLabel InfiltrationTurnValue;

	public AgeTransform SpyingEffectsGroup;

	public AgeControlButton AssignSpyButton;

	public AgePrimitiveLabel AssignTitle;

	public Vector2 WantedPosition;

	private static List<ConstructionResourceStock> emptyList = new List<ConstructionResourceStock>();

	private City city;

	private GuiEmpire guiEmpire;

	private GuiEmpire guiPreviousEmpire;

	private ConstructionQueue constructionQueue;

	private IGuiPanelHelper guiPanelHelper;

	private IPlayerControllerRepositoryService playerControllerRepository;

	private IRegionalEffectsService regionalEffectsService;

	private string infiltrationCostResourceName;

	private string temp = string.Empty;

	private List<StaticString> failures = new List<StaticString>();

	private global::Game game;
}
