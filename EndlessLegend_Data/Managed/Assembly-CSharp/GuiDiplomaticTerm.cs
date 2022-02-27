using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using UnityEngine;

public class GuiDiplomaticTerm : IComparable, IComparable<GuiDiplomaticTerm>
{
	public GuiDiplomaticTerm(DiplomaticTerm diplomaticTerm)
	{
		IGameService service = Services.GetService<IGameService>();
		global::Game game = service.Game as global::Game;
		Diagnostics.Assert(game != null);
		this.gameEntityService = game.Services.GetService<IGameEntityRepositoryService>();
		Diagnostics.Assert(this.gameEntityService != null);
		this.Term = diplomaticTerm;
		this.Type = this.GetTermType();
	}

	public DiplomaticTerm Term { get; private set; }

	public GuiDiplomaticTerm.TermType Type { get; private set; }

	public string Name
	{
		get
		{
			return this.Term.Definition.Name;
		}
	}

	public string Title
	{
		get
		{
			if ((this.Type == GuiDiplomaticTerm.TermType.RelationStateNegotiationTreaty || this.Type == GuiDiplomaticTerm.TermType.RelationStateDeclarationTreaty || this.Type == GuiDiplomaticTerm.TermType.StandardNegotiationTreaty || this.Type == GuiDiplomaticTerm.TermType.StandardDeclarationTreaty) && this.TermGuiElement != null)
			{
				return AgeLocalizer.Instance.LocalizeString(this.TermGuiElement.Title);
			}
			if ((this.Type == GuiDiplomaticTerm.TermType.Dust || this.Type == GuiDiplomaticTerm.TermType.Strategic || this.Type == GuiDiplomaticTerm.TermType.Luxury || this.Type == GuiDiplomaticTerm.TermType.Orb) && this.ResourceGuiElement != null)
			{
				return AgeLocalizer.Instance.LocalizeString(this.ResourceGuiElement.Title);
			}
			if (this.Type == GuiDiplomaticTerm.TermType.Booster && this.BoosterGuiElement != null)
			{
				return AgeLocalizer.Instance.LocalizeString(this.BoosterGuiElement.Title);
			}
			if (this.Type == GuiDiplomaticTerm.TermType.City)
			{
				DiplomaticTermCityExchange diplomaticTermCityExchange = this.Term as DiplomaticTermCityExchange;
				if (diplomaticTermCityExchange != null)
				{
					IGameEntity gameEntity;
					this.gameEntityService.TryGetValue(diplomaticTermCityExchange.CityGUID, out gameEntity);
					City city = gameEntity as City;
					if (city != null)
					{
						string text = city.Region.LocalizedName;
						DepartmentOfTheInterior agency = city.Empire.GetAgency<DepartmentOfTheInterior>();
						if (agency != null && agency.MainCityGUID == city.GUID)
						{
							text = AgeLocalizer.Instance.LocalizeString("%MainCitySymbol") + text;
						}
						if (diplomaticTermCityExchange.Definition.Name == DiplomaticTermCityExchange.MimicsCityDeal)
						{
							text = AgeLocalizer.Instance.LocalizeString("%FactionIntegrationTitle") + ": " + text;
						}
						return text;
					}
				}
			}
			if (this.Type == GuiDiplomaticTerm.TermType.Fortress)
			{
				DiplomaticTermFortressExchange diplomaticTermFortressExchange = this.Term as DiplomaticTermFortressExchange;
				if (diplomaticTermFortressExchange != null)
				{
					IGameEntity gameEntity2;
					this.gameEntityService.TryGetValue(diplomaticTermFortressExchange.FortressGUID, out gameEntity2);
					Fortress fortress = gameEntity2 as Fortress;
					if (fortress != null)
					{
						string text2 = string.Empty;
						GuiElement guiElement;
						if (this.GuiPanelHelper != null && this.GuiPanelHelper.TryGetGuiElement(fortress.PointOfInterest.PointOfInterestDefinition.PointOfInterestTemplateName, out guiElement))
						{
							if (fortress.Orientation == Fortress.CardinalOrientation.Center)
							{
								text2 = AgeLocalizer.Instance.LocalizeString(guiElement.Title);
							}
							else
							{
								text2 = AgeLocalizer.Instance.LocalizeString("%FortressGeolocalizedNameFormat").Replace("$FortressName", AgeLocalizer.Instance.LocalizeString(guiElement.Title));
								text2 = text2.Replace("$FortressPosition", AgeLocalizer.Instance.LocalizeString("%" + fortress.Orientation.ToString() + "GeolocalizationTitle"));
							}
						}
						else
						{
							text2 = fortress.PointOfInterest.PointOfInterestDefinition.PointOfInterestTemplateName;
						}
						if (fortress.Region != null)
						{
							return AgeLocalizer.Instance.LocalizeString(fortress.Region.Name) + " - " + text2;
						}
						return text2;
					}
				}
			}
			if (this.Type == GuiDiplomaticTerm.TermType.Prisoner)
			{
				DiplomaticTermPrisonerExchange diplomaticTermPrisonerExchange = this.Term as DiplomaticTermPrisonerExchange;
				IGameEntity gameEntity3;
				if (diplomaticTermPrisonerExchange != null && this.gameEntityService.TryGetValue(diplomaticTermPrisonerExchange.HeroGuid, out gameEntity3))
				{
					Unit unit = gameEntity3 as Unit;
					if (unit != null)
					{
						return unit.UnitDesign.LocalizedName;
					}
				}
			}
			if (this.Type == GuiDiplomaticTerm.TermType.Technology)
			{
				DiplomaticTermTechnologyExchange diplomaticTermTechnologyExchange = this.Term as DiplomaticTermTechnologyExchange;
				return AgeLocalizer.Instance.LocalizeString(DepartmentOfScience.GetTechnologyTitle(diplomaticTermTechnologyExchange.TechnologyDefinition));
			}
			return this.Term.Definition.Name;
		}
	}

	public string Description
	{
		get
		{
			if (this.TermGuiElement != null)
			{
				return this.TermGuiElement.Description;
			}
			return "#FF0000#Missing GuiElement #REVERT#(" + this.Name + ")";
		}
	}

	public bool IsQuantified
	{
		get
		{
			return this.Type == GuiDiplomaticTerm.TermType.Booster || this.Type == GuiDiplomaticTerm.TermType.Dust || this.Type == GuiDiplomaticTerm.TermType.Luxury || this.Type == GuiDiplomaticTerm.TermType.Strategic || this.Type == GuiDiplomaticTerm.TermType.Orb;
		}
	}

	public float Amount
	{
		get
		{
			if (this.Term is DiplomaticTermResourceExchange)
			{
				DiplomaticTermResourceExchange diplomaticTermResourceExchange = this.Term as DiplomaticTermResourceExchange;
				return Mathf.Floor(diplomaticTermResourceExchange.Amount);
			}
			if (this.Term is DiplomaticTermBoosterExchange)
			{
				DiplomaticTermBoosterExchange diplomaticTermBoosterExchange = this.Term as DiplomaticTermBoosterExchange;
				return (float)diplomaticTermBoosterExchange.BoosterGUID.Length;
			}
			return 0f;
		}
	}

	public float StockValue
	{
		get
		{
			if (this.Type == GuiDiplomaticTerm.TermType.Dust || this.Type == GuiDiplomaticTerm.TermType.Strategic || this.Type == GuiDiplomaticTerm.TermType.Luxury || this.Type == GuiDiplomaticTerm.TermType.Orb)
			{
				DiplomaticTermResourceExchange diplomaticTermResourceExchange = this.Term as DiplomaticTermResourceExchange;
				DepartmentOfTheTreasury agency = this.Term.EmpireWhichProvides.GetAgency<DepartmentOfTheTreasury>();
				float f;
				if (!agency.TryGetResourceStockValue(this.Term.EmpireWhichProvides.SimulationObject, diplomaticTermResourceExchange.ResourceName, out f, false))
				{
					Diagnostics.LogWarning("Could not get the available quantity of the resource {0}.", new object[]
					{
						diplomaticTermResourceExchange.ResourceName
					});
					return 0f;
				}
				return Mathf.Floor(f);
			}
			else
			{
				if (this.Type == GuiDiplomaticTerm.TermType.Booster)
				{
					DiplomaticTermBoosterExchange termBoosterExchange = this.Term as DiplomaticTermBoosterExchange;
					DepartmentOfEducation agency2 = this.Term.EmpireWhichProvides.GetAgency<DepartmentOfEducation>();
					return (float)agency2.Count((VaultItem match) => match.Constructible.Name == termBoosterExchange.BoosterDefinitionName);
				}
				Diagnostics.LogWarning("Called StockValue on a term that doesn't have any quantity.");
				return 0f;
			}
		}
	}

	public float NetValue
	{
		get
		{
			DiplomaticTermResourceExchange diplomaticTermResourceExchange = this.Term as DiplomaticTermResourceExchange;
			if (diplomaticTermResourceExchange == null)
			{
				Diagnostics.LogWarning("Called NetQuantity on a term that doesn't have any net quantity.");
				return 0f;
			}
			DepartmentOfTheTreasury agency = this.Term.EmpireWhichProvides.GetAgency<DepartmentOfTheTreasury>();
			float f;
			if (!agency.TryGetNetResourceValue(this.Term.EmpireWhichProvides.SimulationObject, diplomaticTermResourceExchange.ResourceName, out f, false))
			{
				Diagnostics.LogError("Can't get resource net value {0} on simulation object {1}.", new object[]
				{
					diplomaticTermResourceExchange.ResourceName,
					this.Term.EmpireWhichProvides.SimulationObject
				});
			}
			return Mathf.Round(f);
		}
	}

	public float EmpirePointCost
	{
		get
		{
			return DepartmentOfForeignAffairs.GetEmpirePointCost(this.Term, this.Term.EmpireWhichProposes);
		}
	}

	public Texture2D IconTexture
	{
		get
		{
			Texture2D result;
			if ((this.Type == GuiDiplomaticTerm.TermType.RelationStateNegotiationTreaty || this.Type == GuiDiplomaticTerm.TermType.RelationStateDeclarationTreaty || this.Type == GuiDiplomaticTerm.TermType.StandardNegotiationTreaty || this.Type == GuiDiplomaticTerm.TermType.StandardDeclarationTreaty || this.Type == GuiDiplomaticTerm.TermType.City || this.Type == GuiDiplomaticTerm.TermType.Fortress || this.Type == GuiDiplomaticTerm.TermType.Prisoner || this.Type == GuiDiplomaticTerm.TermType.Technology) && this.TermGuiElement != null && this.GuiPanelHelper.TryGetTextureFromIcon(this.TermGuiElement, global::GuiPanel.IconSize.Small, out result))
			{
				return result;
			}
			if ((this.Type == GuiDiplomaticTerm.TermType.Dust || this.Type == GuiDiplomaticTerm.TermType.Strategic || this.Type == GuiDiplomaticTerm.TermType.Luxury || this.Type == GuiDiplomaticTerm.TermType.Orb) && this.ResourceGuiElement != null && this.GuiPanelHelper.TryGetTextureFromIcon(this.ResourceGuiElement, global::GuiPanel.IconSize.Small, out result))
			{
				return result;
			}
			if (this.Type == GuiDiplomaticTerm.TermType.Booster && this.BoosterGuiElement != null && this.GuiPanelHelper.TryGetTextureFromIcon(this.BoosterGuiElement, global::GuiPanel.IconSize.LogoSmall, out result))
			{
				return result;
			}
			return null;
		}
	}

	public Color IconColor
	{
		get
		{
			if (this.Type == GuiDiplomaticTerm.TermType.Dust || this.Type == GuiDiplomaticTerm.TermType.Strategic || this.Type == GuiDiplomaticTerm.TermType.Luxury || this.Type == GuiDiplomaticTerm.TermType.Orb)
			{
				ExtendedGuiElement extendedGuiElement = this.ResourceGuiElement as ExtendedGuiElement;
				if (extendedGuiElement != null)
				{
					return extendedGuiElement.Color;
				}
			}
			if (this.Type == GuiDiplomaticTerm.TermType.Booster)
			{
				ExtendedGuiElement extendedGuiElement2 = this.BoosterGuiElement as ExtendedGuiElement;
				if (extendedGuiElement2 != null)
				{
					return extendedGuiElement2.Color;
				}
			}
			return Color.white;
		}
	}

	private IGuiPanelHelper GuiPanelHelper
	{
		get
		{
			if (this.guiPanelHelper == null)
			{
				this.guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
				Diagnostics.Assert(this.guiPanelHelper != null, "Unable to access GuiPanelHelper");
			}
			return this.guiPanelHelper;
		}
	}

	private DiplomaticTermGuiElement TermGuiElement
	{
		get
		{
			if (this.termGuiElement == null)
			{
				GuiElement guiElement;
				if (!this.GuiPanelHelper.TryGetGuiElement(this.Term.Definition.Name, out guiElement))
				{
					Diagnostics.LogWarning("Cannot find a GuiElement for the diplomatic term '{0}'.", new object[]
					{
						this.Term.Definition.Name
					});
					return null;
				}
				this.termGuiElement = (guiElement as DiplomaticTermGuiElement);
				Diagnostics.Assert(this.termGuiElement != null);
			}
			return this.termGuiElement;
		}
	}

	private GuiElement ResourceGuiElement
	{
		get
		{
			if (this.resourceGuiElement == null)
			{
				DiplomaticTermResourceExchange diplomaticTermResourceExchange = this.Term as DiplomaticTermResourceExchange;
				if (diplomaticTermResourceExchange != null && !this.GuiPanelHelper.TryGetGuiElement(diplomaticTermResourceExchange.ResourceName, out this.resourceGuiElement))
				{
					Diagnostics.LogWarning("Cannot find a GuiElement for the resource {0}", new object[]
					{
						diplomaticTermResourceExchange.ResourceName
					});
				}
			}
			return this.resourceGuiElement;
		}
	}

	private GuiElement BoosterGuiElement
	{
		get
		{
			if (this.boosterGuiElement == null)
			{
				DiplomaticTermBoosterExchange diplomaticTermBoosterExchange = this.Term as DiplomaticTermBoosterExchange;
				if (diplomaticTermBoosterExchange != null && !this.GuiPanelHelper.TryGetGuiElement(diplomaticTermBoosterExchange.BoosterDefinitionName, out this.boosterGuiElement))
				{
					Diagnostics.LogWarning("Cannot find a GuiElement for the booster {0}", new object[]
					{
						diplomaticTermBoosterExchange.BoosterDefinitionName
					});
				}
			}
			return this.boosterGuiElement;
		}
	}

	private GuiElement TechnologyGuiElement
	{
		get
		{
			if (this.technologyGuiElement == null)
			{
				DiplomaticTermTechnologyExchange diplomaticTermTechnologyExchange = this.Term as DiplomaticTermTechnologyExchange;
				if (diplomaticTermTechnologyExchange != null && !this.GuiPanelHelper.TryGetGuiElement(diplomaticTermTechnologyExchange.TechnologyDefinition.Name, out this.technologyGuiElement))
				{
					Diagnostics.LogWarning("Cannot find a GuiElement for the technology {0}", new object[]
					{
						diplomaticTermTechnologyExchange.TechnologyDefinition.Name
					});
				}
			}
			return this.technologyGuiElement;
		}
	}

	public int CompareTo(GuiDiplomaticTerm other)
	{
		if (this.Type != other.Type)
		{
			return this.Type.CompareTo(other.Type);
		}
		if ((this.Type == GuiDiplomaticTerm.TermType.Strategic || this.Type == GuiDiplomaticTerm.TermType.Luxury) && this.ResourceGuiElement != null)
		{
			return this.ResourceGuiElement.Title.CompareTo(other.ResourceGuiElement.Title);
		}
		if (this.Type == GuiDiplomaticTerm.TermType.City)
		{
			DiplomaticTermCityExchange diplomaticTermCityExchange = this.Term as DiplomaticTermCityExchange;
			DiplomaticTermCityExchange diplomaticTermCityExchange2 = other.Term as DiplomaticTermCityExchange;
			if (diplomaticTermCityExchange != null && diplomaticTermCityExchange2 != null)
			{
				return diplomaticTermCityExchange.CityGUID.CompareTo(diplomaticTermCityExchange2.CityGUID);
			}
		}
		if (this.Type == GuiDiplomaticTerm.TermType.Fortress)
		{
			DiplomaticTermFortressExchange diplomaticTermFortressExchange = this.Term as DiplomaticTermFortressExchange;
			DiplomaticTermFortressExchange diplomaticTermFortressExchange2 = other.Term as DiplomaticTermFortressExchange;
			if (diplomaticTermFortressExchange != null && diplomaticTermFortressExchange2 != null)
			{
				return diplomaticTermFortressExchange.FortressGUID.CompareTo(diplomaticTermFortressExchange2.FortressGUID);
			}
		}
		if (this.Type == GuiDiplomaticTerm.TermType.Technology)
		{
			DiplomaticTermTechnologyExchange diplomaticTermTechnologyExchange = this.Term as DiplomaticTermTechnologyExchange;
			DiplomaticTermTechnologyExchange diplomaticTermTechnologyExchange2 = other.Term as DiplomaticTermTechnologyExchange;
			int technologyEraNumber = DepartmentOfScience.GetTechnologyEraNumber(diplomaticTermTechnologyExchange.TechnologyDefinition);
			int technologyEraNumber2 = DepartmentOfScience.GetTechnologyEraNumber(diplomaticTermTechnologyExchange2.TechnologyDefinition);
			if (technologyEraNumber != technologyEraNumber2)
			{
				return technologyEraNumber.CompareTo(technologyEraNumber2);
			}
		}
		if (this.Type == GuiDiplomaticTerm.TermType.Prisoner)
		{
			DiplomaticTermPrisonerExchange diplomaticTermPrisonerExchange = this.Term as DiplomaticTermPrisonerExchange;
			DiplomaticTermPrisonerExchange diplomaticTermPrisonerExchange2 = other.Term as DiplomaticTermPrisonerExchange;
			if (diplomaticTermPrisonerExchange != null && diplomaticTermPrisonerExchange2 != null)
			{
				return diplomaticTermPrisonerExchange.HeroGuid.CompareTo(diplomaticTermPrisonerExchange2.HeroGuid);
			}
		}
		return this.Term.Definition.Name.CompareTo(other.Term.Definition.Name);
	}

	public int CompareTo(object obj)
	{
		return this.CompareTo((GuiDiplomaticTerm)obj);
	}

	public void DisplayCost(AgePrimitiveLabel costLabel)
	{
		string text = string.Empty;
		text = GuiFormater.FormatInstantCost(this.Term.EmpireWhichProposes, this.EmpirePointCost, DepartmentOfTheTreasury.Resources.EmpirePoint, false, 0);
		costLabel.Text = text;
	}

	public void GenerateTooltip(AgeTooltip ageTooltip)
	{
		if (this.Type == GuiDiplomaticTerm.TermType.Dust)
		{
			ageTooltip.Class = "Simple";
			ageTooltip.Content = string.Empty;
			ageTooltip.ClientData = null;
			return;
		}
		if (this.Type == GuiDiplomaticTerm.TermType.City && this.Term.Definition.Name == DiplomaticTermCityExchange.MimicsCityDeal)
		{
			string text = this.Term.EmpireWhichProvides.Faction.Affinity.Name.ToString();
			text = text.Replace("Affinity", "");
			if (text == "Mezari")
			{
				text = "Vaulters";
			}
			ageTooltip.Class = "Simple";
			ageTooltip.Content = "#FFB43F#" + AgeLocalizer.Instance.LocalizeString("%" + text + "IntegrationDescriptor1Title") + "#REVERT#\n\n" + AgeLocalizer.Instance.LocalizeString("%" + text + "IntegrationDescriptor1EffectOverride");
			ageTooltip.ClientData = null;
			return;
		}
		if (this.Type == GuiDiplomaticTerm.TermType.Strategic || this.Type == GuiDiplomaticTerm.TermType.Luxury)
		{
			DiplomaticTermResourceExchange diplomaticTermResourceExchange = this.Term as DiplomaticTermResourceExchange;
			ResourceDefinition resourceDefinition;
			if (diplomaticTermResourceExchange != null && Databases.GetDatabase<ResourceDefinition>(false).TryGetValue(diplomaticTermResourceExchange.ResourceName, out resourceDefinition))
			{
				IPlayerControllerRepositoryService service = Services.GetService<IGameService>().Game.Services.GetService<IPlayerControllerRepositoryService>();
				Diagnostics.Assert(service != null);
				ResourceTooltipData resourceTooltipData = new ResourceTooltipData(resourceDefinition, service.ActivePlayerController.Empire as global::Empire);
				if (this.Type == GuiDiplomaticTerm.TermType.Luxury)
				{
					IDatabase<BoosterDefinition> database = Databases.GetDatabase<BoosterDefinition>(false);
					if (database != null)
					{
						resourceTooltipData.Constructible = database.GetValue("Booster" + resourceDefinition.Name);
					}
				}
				ageTooltip.Class = resourceTooltipData.TooltipClass;
				ageTooltip.Content = resourceDefinition.Name;
				ageTooltip.ClientData = resourceTooltipData;
				return;
			}
		}
		else if (this.Type == GuiDiplomaticTerm.TermType.Booster)
		{
			DiplomaticTermBoosterExchange diplomaticTermBoosterExchange = this.Term as DiplomaticTermBoosterExchange;
			BoosterDefinition boosterDefinition;
			if (diplomaticTermBoosterExchange != null && Databases.GetDatabase<BoosterDefinition>(false).TryGetValue(diplomaticTermBoosterExchange.BoosterDefinitionName, out boosterDefinition))
			{
				GuiStackedBooster guiStackedBooster = new GuiStackedBooster(boosterDefinition);
				ageTooltip.Class = guiStackedBooster.BoosterDefinition.TooltipClass;
				ageTooltip.Content = guiStackedBooster.BoosterDefinition.Name;
				ageTooltip.ClientData = guiStackedBooster;
				return;
			}
		}
		else
		{
			if (this.Type == GuiDiplomaticTerm.TermType.Technology)
			{
				DepartmentOfScience.BuildTechnologyTooltip((this.Term as DiplomaticTermTechnologyExchange).TechnologyDefinition, this.Term.EmpireWhichProvides, ageTooltip, MultipleConstructibleTooltipData.TechnologyState.Normal);
				return;
			}
			if (this.Type == GuiDiplomaticTerm.TermType.Orb)
			{
				ageTooltip.Class = "OrbResource";
				ageTooltip.Content = "Orb";
				ageTooltip.ClientData = null;
				return;
			}
			ageTooltip.Class = "Simple";
			ageTooltip.Content = this.Description;
			ageTooltip.ClientData = this;
		}
	}

	public override string ToString()
	{
		string str = AgeLocalizer.Instance.LocalizeString(this.Title);
		if (this.IsQuantified)
		{
			str = str + " (" + this.Amount.ToString() + ")";
		}
		return this.Description;
	}

	public List<DiplomaticRelationScore.ModifiersData> GetPriceModifiers()
	{
		List<DiplomaticRelationScore.ModifiersData> list = new List<DiplomaticRelationScore.ModifiersData>();
		global::Empire payingEmpire = this.GetPayingEmpire();
		Diagnostics.Assert(payingEmpire != null);
		global::Empire empire = (payingEmpire != this.Term.EmpireWhichProvides) ? this.Term.EmpireWhichProvides : this.Term.EmpireWhichReceives;
		Diagnostics.Assert(empire != null);
		DepartmentOfForeignAffairs agency = payingEmpire.GetAgency<DepartmentOfForeignAffairs>();
		Diagnostics.Assert(agency != null);
		DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(empire);
		foreach (DiplomaticRelationScore.ModifiersData modifiersData in diplomaticRelation.DiplomaticRelationScore.MofifiersDatas)
		{
			if (this.TermGuiElement != null && this.TermGuiElement.PriceModifierCategories.Contains(modifiersData.Category) && Mathf.Abs(modifiersData.TotalValue) > 0.1f)
			{
				list.Add(modifiersData);
			}
		}
		return list;
	}

	public global::Empire GetPayingEmpire()
	{
		IDiplomaticCost[] diplomaticCosts = this.Term.DiplomaticCosts;
		if (diplomaticCosts != null && diplomaticCosts.Length > 0)
		{
			DiplomaticCustomCost diplomaticCustomCost = diplomaticCosts[0] as DiplomaticCustomCost;
			if (diplomaticCustomCost != null)
			{
				if (diplomaticCustomCost.Responsible == DiplomaticCostResponsible.EmpireWhichProposes)
				{
					return this.Term.EmpireWhichProposes;
				}
				if (diplomaticCustomCost.Responsible == DiplomaticCostResponsible.EmpireWhichProvides)
				{
					return this.Term.EmpireWhichProvides;
				}
				if (diplomaticCustomCost.Responsible == DiplomaticCostResponsible.EmpireWhichReceives)
				{
					return this.Term.EmpireWhichReceives;
				}
			}
		}
		return this.Term.EmpireWhichProposes;
	}

	private GuiDiplomaticTerm.TermType GetTermType()
	{
		if (!(this.Term.Definition.Category == "Treaty"))
		{
			if (this.Term.Definition.Category == "Resource")
			{
				DiplomaticTermResourceExchange diplomaticTermResourceExchange = this.Term as DiplomaticTermResourceExchange;
				if (diplomaticTermResourceExchange != null)
				{
					if (diplomaticTermResourceExchange.ResourceName == "EmpireMoney")
					{
						return GuiDiplomaticTerm.TermType.Dust;
					}
					IDatabase<ResourceDefinition> database = Databases.GetDatabase<ResourceDefinition>(false);
					ResourceDefinition resourceDefinition;
					if (database.TryGetValue(diplomaticTermResourceExchange.ResourceName, out resourceDefinition))
					{
						if (resourceDefinition.ResourceType == ResourceDefinition.Type.Strategic)
						{
							return GuiDiplomaticTerm.TermType.Strategic;
						}
						if (resourceDefinition.ResourceType == ResourceDefinition.Type.Luxury)
						{
							return GuiDiplomaticTerm.TermType.Luxury;
						}
						if (resourceDefinition.ResourceType == ResourceDefinition.Type.Common)
						{
							return GuiDiplomaticTerm.TermType.Orb;
						}
					}
				}
			}
			else
			{
				if (this.Term.Definition.Category == "Booster" && this.Term is DiplomaticTermBoosterExchange)
				{
					return GuiDiplomaticTerm.TermType.Booster;
				}
				if (this.Term.Definition.Category == "City" && this.Term is DiplomaticTermCityExchange)
				{
					return GuiDiplomaticTerm.TermType.City;
				}
				if (this.Term.Definition.Category == "Fortress" && this.Term is DiplomaticTermFortressExchange)
				{
					return GuiDiplomaticTerm.TermType.Fortress;
				}
				if (this.Term.Definition.Category == "Technology" && this.Term is DiplomaticTermTechnologyExchange)
				{
					return GuiDiplomaticTerm.TermType.Technology;
				}
			}
			return GuiDiplomaticTerm.TermType.Unknown;
		}
		if (this.Term is DiplomaticTermDiplomaticRelationState)
		{
			if (this.Term.Definition.PropositionMethod == DiplomaticTerm.PropositionMethod.Negotiation)
			{
				return GuiDiplomaticTerm.TermType.RelationStateNegotiationTreaty;
			}
			return GuiDiplomaticTerm.TermType.RelationStateDeclarationTreaty;
		}
		else
		{
			if (this.Term is DiplomaticTermPrisonerExchange)
			{
				return GuiDiplomaticTerm.TermType.Prisoner;
			}
			if (this.Term.Definition.PropositionMethod == DiplomaticTerm.PropositionMethod.Negotiation)
			{
				return GuiDiplomaticTerm.TermType.StandardNegotiationTreaty;
			}
			return GuiDiplomaticTerm.TermType.StandardDeclarationTreaty;
		}
	}

	private DiplomaticTermGuiElement termGuiElement;

	private GuiElement resourceGuiElement;

	private GuiElement boosterGuiElement;

	private GuiElement technologyGuiElement;

	private IGuiPanelHelper guiPanelHelper;

	private IGameEntityRepositoryService gameEntityService;

	public enum TermType
	{
		RelationStateNegotiationTreaty,
		RelationStateDeclarationTreaty,
		StandardNegotiationTreaty,
		StandardDeclarationTreaty,
		Dust,
		Strategic,
		Luxury,
		Orb,
		Booster,
		Technology,
		City,
		Fortress,
		Prisoner,
		Unknown
	}
}
