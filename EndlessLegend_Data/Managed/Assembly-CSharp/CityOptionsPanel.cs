using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Input;
using Amplitude.Unity.View;
using UnityEngine;

public class CityOptionsPanel : GuiCollapsingPanel
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
				DepartmentOfIndustry agency = this.city.Empire.GetAgency<DepartmentOfIndustry>();
				ConstructionQueue constructionQueue = agency.GetConstructionQueue(this.city);
				if (constructionQueue != null)
				{
					constructionQueue.CollectionChanged -= this.ConstructionQueue_CollectionChanged;
				}
			}
			this.city = value;
			if (this.city != null)
			{
				DepartmentOfIndustry agency2 = this.city.Empire.GetAgency<DepartmentOfIndustry>();
				ConstructionQueue constructionQueue2 = agency2.GetConstructionQueue(this.city);
				if (constructionQueue2 != null)
				{
					constructionQueue2.CollectionChanged += this.ConstructionQueue_CollectionChanged;
				}
			}
		}
	}

	public bool AuthorizeExpand { get; set; }

	private bool IsOtherEmpire { get; set; }

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

	private DepartmentOfTheTreasury DepartmentOfTheTreasury
	{
		get
		{
			return this.departmentOfTheTreasury;
		}
		set
		{
			if (this.departmentOfTheTreasury != null)
			{
				this.departmentOfTheTreasury.ResourcePropertyChange -= this.DepartmentOfTheTreasury_ResourcePropertyChange;
			}
			this.departmentOfTheTreasury = value;
			if (this.departmentOfTheTreasury != null)
			{
				this.departmentOfTheTreasury.ResourcePropertyChange += this.DepartmentOfTheTreasury_ResourcePropertyChange;
			}
		}
	}

	public void Bind(City city)
	{
		this.City = city;
		this.IsOtherEmpire = (this.playerControllerRepository.ActivePlayerController.Empire != this.City.Empire);
		this.keyMappingService = Services.GetService<IKeyMappingService>();
		this.FilterTable.Visible = !this.IsOtherEmpire;
		this.DepartmentOfTheTreasury = this.City.Empire.GetAgency<DepartmentOfTheTreasury>();
		this.departmentOfPlanificationAndDevelopment = this.city.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
		this.departmentOfForeignAffairs = this.city.Empire.GetAgency<DepartmentOfForeignAffairs>();
	}

	public void Unbind()
	{
		this.IsOtherEmpire = true;
		this.filteredElement.Clear();
		this.City = null;
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		this.filter = CityOptionsPanel.OptionCategory.Default;
		for (int i = 0; i < this.FilterTable.GetChildren().Count; i++)
		{
			AgeControlToggle ageControlToggle = this.filterToggles[i];
			if (ageControlToggle.State)
			{
				this.filter |= this.filtersByToggle[ageControlToggle];
			}
		}
		DepartmentOfIndustry agency = this.City.Empire.GetAgency<DepartmentOfIndustry>();
		IEnumerable<DepartmentOfIndustry.ConstructibleElement> availableConstructibleElementsAsEnumerable = agency.ConstructibleElementDatabase.GetAvailableConstructibleElementsAsEnumerable(new StaticString[0]);
		this.filteredElement.Clear();
		foreach (DepartmentOfIndustry.ConstructibleElement constructibleElement in availableConstructibleElementsAsEnumerable)
		{
			CityOptionsPanel.OptionCategory optionCategory = CityOptionsPanel.OptionCategory.Buildings;
			if (!(constructibleElement.Category == CampConstructibleActionDefinition.ReadOnlyCategory))
			{
				if (constructibleElement.Category == CityImprovementDefinition.ReadOnlyCategory)
				{
					optionCategory = CityOptionsPanel.OptionCategory.Buildings;
				}
				else if (constructibleElement.Category == CityConstructibleActionDefinition.ReadOnlyCategory)
				{
					optionCategory = CityOptionsPanel.OptionCategory.Buildings;
				}
				else if (constructibleElement.Category == CityImprovementDefinition.ReadOnlyNationalCategory)
				{
					optionCategory = CityOptionsPanel.OptionCategory.Buildings;
				}
				else if (constructibleElement.Category == DistrictImprovementDefinition.ReadOnlyCategory)
				{
					optionCategory = CityOptionsPanel.OptionCategory.Expand;
				}
				else if (constructibleElement.Category == PointOfInterestImprovementDefinition.ReadOnlyCategory)
				{
					optionCategory = CityOptionsPanel.OptionCategory.Expand;
				}
				else if (constructibleElement.Category == UnitDesign.ReadOnlyCategory)
				{
					optionCategory = CityOptionsPanel.OptionCategory.Unit;
					if (constructibleElement is UnitDesign && (constructibleElement as UnitDesign).Hidden)
					{
						continue;
					}
				}
				else if (constructibleElement.Category == BoosterGeneratorDefinition.ReadOnlyCategory)
				{
					optionCategory = CityOptionsPanel.OptionCategory.Booster;
				}
				else if (constructibleElement.Category == ConstructibleDistrictDefinition.ReadOnlyCategory)
				{
					optionCategory = CityOptionsPanel.OptionCategory.Expand;
				}
				if ((optionCategory & this.filter) != CityOptionsPanel.OptionCategory.Default)
				{
					this.lastFailureFlags.Clear();
					agency.GetConstructibleState(this.City, constructibleElement, ref this.lastFailureFlags);
					DepartmentOfTheTreasury.CheckConstructiblePrerequisites(this.City, constructibleElement, ref this.lastFailureFlags, new string[]
					{
						ConstructionFlags.Disable,
						ConstructionFlags.Discard
					});
					if (!this.lastFailureFlags.Contains(ConstructionFlags.Discard))
					{
						if (!this.lastFailureFlags.Contains(ConstructionFlags.Disable) && !this.AuthorizeExpand && optionCategory == CityOptionsPanel.OptionCategory.Expand)
						{
							this.lastFailureFlags.Add(ConstructionFlags.Disable);
						}
						if (this.City.IsInfected)
						{
							Diagnostics.Assert(this.City.LastNonInfectedOwner != null);
							if (StaticString.IsNullOrEmpty(constructibleElement.SubCategory) || !constructibleElement.SubCategory.Equals(DepartmentOfTheInterior.InfectionAllowedSubcategory))
							{
								continue;
							}
							if (constructibleElement is CityConstructibleActionDefinition)
							{
								CityConstructibleActionDefinition cityConstructibleActionDefinition = constructibleElement as CityConstructibleActionDefinition;
								if (cityConstructibleActionDefinition.Action.Name == "IntegrateFaction" && (string.IsNullOrEmpty(cityConstructibleActionDefinition.InfectedAffinityConstraint) || !cityConstructibleActionDefinition.InfectedAffinityConstraint.Equals(this.city.LastNonInfectedOwner.Faction.Affinity.Name) || this.City.LastNonInfectedOwner.Faction.GetIntegrationDescriptorsCount() <= 0 || this.departmentOfPlanificationAndDevelopment.HasIntegratedFaction(this.City.LastNonInfectedOwner.Faction)))
								{
									continue;
								}
							}
						}
						if (constructibleElement is CityConstructibleActionDefinition && (constructibleElement as CityConstructibleActionDefinition).Action.Name == "PurgeTheLand")
						{
							bool flag = false;
							PointOfInterest[] pointOfInterests = this.City.Region.PointOfInterests;
							for (int j = 0; j < pointOfInterests.Length; j++)
							{
								if (pointOfInterests[j].CreepingNodeGUID != GameEntityGUID.Zero)
								{
									IGameEntity gameEntity = null;
									if (this.gameEntityRepositoryService.TryGetValue(pointOfInterests[j].CreepingNodeGUID, out gameEntity))
									{
										CreepingNode creepingNode = gameEntity as CreepingNode;
										if (creepingNode != null && creepingNode.Empire.Index != this.City.Empire.Index && !this.departmentOfForeignAffairs.IsFriend(creepingNode.Empire))
										{
											flag = true;
											break;
										}
									}
								}
							}
							if (!flag)
							{
								continue;
							}
						}
						if (constructibleElement is PointOfInterestImprovementDefinition)
						{
							this.CheckPointOfInterestImprovementPrerequisites((PointOfInterestImprovementDefinition)constructibleElement, ref this.lastFailureFlags);
							if (this.lastFailureFlags.Contains(ConstructionFlags.Discard))
							{
								continue;
							}
						}
						this.filteredElement.Add(new CityOptionsPanel.ProductionConstruction(constructibleElement, this.playerControllerRepository.ActivePlayerController.Empire as global::Empire, this.guiSubCategorySorting, this.lastFailureFlags.ToArray()));
					}
				}
			}
		}
		this.filteredElement.Sort();
		this.OptionsTable.Height = this.optionsTableMinHeight;
		this.OptionsTable.ReserveChildren(this.filteredElement.Count, this.ConstructibleItemPrefab, "Option");
		this.OptionsTable.RefreshChildrenIList<CityOptionsPanel.ProductionConstruction>(this.filteredElement, this.constructibleItemRefreshDelegate, true, false);
		this.OptionsTable.ArrangeChildren();
		this.OptionsScrollview.OnPositionRecomputed();
		this.OptionsTable.Enable = (!this.IsOtherEmpire && this.interactionsAllowed);
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		this.interactionsAllowed = this.playerControllerRepository.ActivePlayerController.CanSendOrders();
		this.filter = (CityOptionsPanel.OptionCategory.Expand | CityOptionsPanel.OptionCategory.Buildings | CityOptionsPanel.OptionCategory.Unit);
		for (int i = 0; i < this.filterToggles.Count; i++)
		{
			this.filterToggles[i].State = (i == 0);
		}
		base.NeedRefresh = true;
		yield break;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		base.NeedRefresh = false;
		this.Unbind();
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		this.playerControllerRepository = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
		this.EndTurnService = Services.GetService<IEndTurnService>();
		this.gameEntityRepositoryService = base.Game.Services.GetService<IGameEntityRepositoryService>();
		yield break;
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.Hide(true);
		this.Unbind();
		this.OptionsTable.DestroyAllChildren();
		this.EndTurnService = null;
		this.playerControllerRepository = null;
		this.gameEntityRepositoryService = null;
		base.OnUnloadGame(game);
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		base.UseRefreshLoop = true;
		this.optionsTableMinHeight = this.OptionsTable.Height;
		this.filter = CityOptionsPanel.OptionCategory.Default;
		this.filterToggles = new List<AgeControlToggle>();
		this.filtersByToggle = new Dictionary<AgeControlToggle, CityOptionsPanel.OptionCategory>();
		for (int i = 0; i < this.FilterTable.GetChildren().Count; i++)
		{
			this.filterToggles.Add(this.FilterTable.GetChildren()[i].GetComponent<AgeControlToggle>());
			this.filterToggles[i].OnSwitchMethod = "OnFilterSwitch";
			this.filterToggles[i].OnSwitchObject = base.gameObject;
			this.filterToggles[i].State = true;
		}
		this.filtersByToggle.Add(this.filterToggles[0], CityOptionsPanel.OptionCategory.Expand | CityOptionsPanel.OptionCategory.Buildings | CityOptionsPanel.OptionCategory.Unit | CityOptionsPanel.OptionCategory.Booster);
		this.filtersByToggle.Add(this.filterToggles[1], CityOptionsPanel.OptionCategory.Buildings);
		this.filtersByToggle.Add(this.filterToggles[2], CityOptionsPanel.OptionCategory.Expand);
		this.filtersByToggle.Add(this.filterToggles[3], CityOptionsPanel.OptionCategory.Unit);
		this.constructibleItemRefreshDelegate = new AgeTransform.RefreshTableItem<CityOptionsPanel.ProductionConstruction>(this.ConstructibleItemRefresh);
		IDatabase<GuiSorting> guiSortingDatabase = Databases.GetDatabase<GuiSorting>(false);
		Diagnostics.Assert(guiSortingDatabase != null);
		this.guiSubCategorySorting = guiSortingDatabase.GetValue("SubCategory");
		yield break;
	}

	protected override void OnUnload()
	{
		this.guiSubCategorySorting = null;
		this.filter = CityOptionsPanel.OptionCategory.Default;
		this.filtersByToggle.Clear();
		this.filtersByToggle = null;
		this.filterToggles.Clear();
		this.filterToggles = null;
		this.constructibleItemRefreshDelegate = null;
		base.OnUnload();
	}

	private bool CheckPointOfInterestImprovementPrerequisites(PointOfInterestImprovementDefinition pointOfInterestImprovementDefinition, ref List<StaticString> failureFlags)
	{
		if (this.lastFailureFlags.Contains(ConstructionFlags.Discard))
		{
			return false;
		}
		IWorldPositionningService service = base.Game.Services.GetService<IWorldPositionningService>();
		if (service == null)
		{
			this.lastFailureFlags.Add(ConstructionFlags.Discard);
			return false;
		}
		DepartmentOfIndustry agency = this.City.Empire.GetAgency<DepartmentOfIndustry>();
		if (agency == null)
		{
			this.lastFailureFlags.Add(ConstructionFlags.Discard);
			return false;
		}
		ConstructionQueue constructionQueue = agency.GetConstructionQueue(this.City);
		if (constructionQueue == null)
		{
			this.lastFailureFlags.Add(ConstructionFlags.Discard);
			return false;
		}
		int num = constructionQueue.Count(pointOfInterestImprovementDefinition);
		int num2 = 0;
		for (int i = 0; i < this.city.Region.PointOfInterests.Length; i++)
		{
			PointOfInterest pointOfInterest = this.city.Region.PointOfInterests[i];
			if (!(pointOfInterest.PointOfInterestDefinition.PointOfInterestTemplateName != pointOfInterestImprovementDefinition.PointOfInterestTemplateName))
			{
				if (pointOfInterest.PointOfInterestImprovement == null)
				{
					int explorationBits = service.GetExplorationBits(pointOfInterest.WorldPosition);
					if ((explorationBits & this.city.Empire.Bits) != 0)
					{
						num2++;
					}
				}
			}
		}
		if (num2 <= num)
		{
			this.lastFailureFlags.Add(ConstructionFlags.Discard);
			return false;
		}
		return true;
	}

	private void ConstructionQueue_CollectionChanged(object sender, CollectionChangeEventArgs e)
	{
		base.NeedRefresh = true;
	}

	private void DepartmentOfTheTreasury_ResourcePropertyChange(object sender, ResourcePropertyChangeEventArgs e)
	{
		if (base.IsVisible)
		{
			base.NeedRefresh = true;
		}
	}

	private void EndTurnService_GameClientStateChange(object sender, GameClientStateChangeEventArgs e)
	{
		if (base.IsVisible && this.playerControllerRepository != null && this.playerControllerRepository.ActivePlayerController != null)
		{
			bool flag = this.playerControllerRepository.ActivePlayerController.CanSendOrders();
			if (this.interactionsAllowed != flag)
			{
				this.interactionsAllowed = flag;
				this.OptionsTable.Enable = (!this.IsOtherEmpire && this.interactionsAllowed);
				if (!this.interactionsAllowed && MessagePanel.Instance.IsVisible)
				{
					MessagePanel.Instance.Hide(false);
				}
			}
		}
	}

	private void ConstructibleItemRefresh(AgeTransform tableitem, CityOptionsPanel.ProductionConstruction reference, int index)
	{
		ConstructibleGuiItem component = tableitem.GetComponent<ConstructibleGuiItem>();
		if (component.AgeTransform.AgeTooltip != null)
		{
			component.AgeTransform.AgeTooltip.Anchor = base.AgeTransform;
			component.AgeTransform.AgeTooltip.AnchorMode = AgeTooltipAnchorMode.TOP_CENTER;
		}
		Diagnostics.Assert(component != null);
		if (component != null)
		{
			component.RefreshContent(this.City, reference, index, base.gameObject);
		}
	}

	private void OnFilterSwitch(GameObject obj)
	{
		for (int i = 0; i < this.filterToggles.Count; i++)
		{
			this.filterToggles[i].State = (this.filterToggles[i].gameObject == obj);
		}
		base.NeedRefresh = true;
	}

	private void OnOptionSelect(GameObject obj)
	{
		AgeControlButton component = obj.GetComponent<AgeControlButton>();
		DepartmentOfIndustry.ConstructibleElement constructibleElement = component.OnActivateDataObject as DepartmentOfIndustry.ConstructibleElement;
		Diagnostics.Assert(constructibleElement != null);
		bool flag = false;
		object[] array = constructibleElement.GetType().GetCustomAttributes(typeof(WorldPlacementCursorAttribute), true);
		if (array != null && array.Length > 0)
		{
			flag = true;
		}
		else if (constructibleElement.Tags.Contains(DownloadableContent9.TagColossus))
		{
			flag = true;
			array = new object[]
			{
				new ColossusWorldPlacementCursorAttribute()
			};
		}
		ICursorService service = Services.GetService<ICursorService>();
		if (flag)
		{
			bool flag2 = true;
			if (flag2)
			{
				if (service != null)
				{
					service.Backup();
					WorldPlacementCursorAttribute worldPlacementCursorAttribute = array[0] as WorldPlacementCursorAttribute;
					Type type = worldPlacementCursorAttribute.Type;
					service.ChangeCursor(type, new object[]
					{
						this.City,
						constructibleElement
					});
				}
				return;
			}
			OrderQueueConstruction order = new OrderQueueConstruction(this.City.Empire.Index, this.City.GUID, constructibleElement, string.Empty);
			Ticket ticket;
			this.playerControllerRepository.ActivePlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
		}
		else if (this.keyMappingService.GetKeyUp(KeyAction.ControlBindingsQueueOnAll) || this.keyMappingService.GetKeyUp(KeyAction.ControlBindingsQueueOnTopOfAll))
		{
			DepartmentOfTheInterior agency = this.City.Empire.GetAgency<DepartmentOfTheInterior>();
			List<City> list = agency.Cities.ToList<City>();
			for (int i = 0; i < list.Count; i++)
			{
				OrderQueueConstruction order2 = new OrderQueueConstruction(this.City.Empire.Index, list[i].GUID, constructibleElement, string.Empty);
				Ticket ticket2;
				this.playerControllerRepository.ActivePlayerController.PostOrder(order2, out ticket2, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
			}
		}
		else
		{
			OrderQueueConstruction order3 = new OrderQueueConstruction(this.City.Empire.Index, this.City.GUID, constructibleElement, string.Empty);
			Ticket ticket3;
			this.playerControllerRepository.ActivePlayerController.PostOrder(order3, out ticket3, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
		}
	}

	private void OnOrderResponse(object sender, TicketRaisedEventArgs args)
	{
	}

	private void OnRightClick(GameObject obj)
	{
		DepartmentOfIndustry.ConstructibleElement constructibleElement = obj.GetComponent<AgeControlButton>().OnActivateDataObject as DepartmentOfIndustry.ConstructibleElement;
		Diagnostics.Assert(constructibleElement != null);
		if (constructibleElement is UnitDesign)
		{
			base.GuiService.GetGuiPanel<UnitDesignModalPanel>().CreateMode = false;
			base.GuiService.GetGuiPanel<UnitDesignModalPanel>().Show(new object[]
			{
				constructibleElement as UnitDesign
			});
		}
	}

	public AgeControlScrollView OptionsScrollview;

	public AgeTransform OptionsTable;

	public AgeTransform FilterTable;

	public Transform ConstructibleItemPrefab;

	private CityOptionsPanel.OptionCategory filter;

	private List<AgeControlToggle> filterToggles;

	private Dictionary<AgeControlToggle, CityOptionsPanel.OptionCategory> filtersByToggle;

	private float optionsTableMinHeight;

	private City city;

	private List<CityOptionsPanel.ProductionConstruction> filteredElement = new List<CityOptionsPanel.ProductionConstruction>();

	private List<StaticString> lastFailureFlags = new List<StaticString>();

	private GuiSorting guiSubCategorySorting;

	private IPlayerControllerRepositoryService playerControllerRepository;

	private IEndTurnService endTurnService;

	private bool interactionsAllowed = true;

	private AgeTransform.RefreshTableItem<CityOptionsPanel.ProductionConstruction> constructibleItemRefreshDelegate;

	private IKeyMappingService keyMappingService;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private DepartmentOfPlanificationAndDevelopment departmentOfPlanificationAndDevelopment;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	[Flags]
	public enum OptionCategory
	{
		Default = 0,
		Expand = 1,
		Buildings = 2,
		Unit = 4,
		Booster = 8
	}

	public struct ProductionConstruction : IComparable, IComparable<CityOptionsPanel.ProductionConstruction>
	{
		public ProductionConstruction(DepartmentOfIndustry.ConstructibleElement constructibleElement, global::Empire empire, GuiSorting guiSubCategorySorting, StaticString[] flags)
		{
			this.ConstructibleElement = constructibleElement;
			this.Flags = flags;
			this.CostInProduction = 0f;
			IConstructionCost[] costs = constructibleElement.Costs;
			for (int i = 0; i < costs.Length; i++)
			{
				if (costs[i].ResourceName == "Production")
				{
					this.CostInProduction += costs[i].GetValue(empire);
				}
			}
			this.subCategorySortValue = guiSubCategorySorting.GetSortValue(this.ConstructibleElement.SubCategory);
		}

		public int CompareTo(CityOptionsPanel.ProductionConstruction other)
		{
			if (this.ConstructibleElement.Category != other.ConstructibleElement.Category)
			{
				StaticString staticString = this.ConstructibleElement.Category;
				StaticString staticString2 = other.ConstructibleElement.Category;
				staticString = ((!(staticString == CityConstructibleActionDefinition.ReadOnlyCategory)) ? staticString : new StaticString("ZZZ"));
				staticString2 = ((!(staticString2 == CityConstructibleActionDefinition.ReadOnlyCategory)) ? staticString2 : new StaticString("ZZZ"));
				return staticString.CompareTo(staticString2);
			}
			if (this.CostInProduction != other.CostInProduction)
			{
				return this.CostInProduction.CompareTo(other.CostInProduction);
			}
			return this.subCategorySortValue.CompareTo(other.subCategorySortValue);
		}

		public int CompareTo(object obj)
		{
			return this.CompareTo((CityOptionsPanel.ProductionConstruction)obj);
		}

		public readonly DepartmentOfIndustry.ConstructibleElement ConstructibleElement;

		public readonly StaticString[] Flags;

		public float CostInProduction;

		private int subCategorySortValue;
	}
}
