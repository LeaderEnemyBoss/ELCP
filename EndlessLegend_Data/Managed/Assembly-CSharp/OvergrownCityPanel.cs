using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Simulation;
using UnityEngine;

public class OvergrownCityPanel : GuiCollapsingPanel
{
	public City City { get; private set; }

	public global::Empire Empire { get; private set; }

	public GuiEmpire GuiEmpire
	{
		get
		{
			return this.guiEmpire;
		}
		private set
		{
			this.guiEmpire = value;
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

	public ConstructionQueue ConstructionQueue { get; private set; }

	private bool IsOtherEmpire { get; set; }

	public void Bind(City city)
	{
		if (this.City != null)
		{
			this.Unbind();
		}
		this.City = city;
		this.Empire = this.City.Empire;
		this.Empire.Refreshed += this.Simulation_Refreshed;
		this.IsOtherEmpire = (this.playerControllerRepository.ActivePlayerController.Empire != this.Empire);
		DepartmentOfIndustry agency = this.Empire.GetAgency<DepartmentOfIndustry>();
		this.ConstructionQueue = agency.GetConstructionQueue(this.City);
		base.GuiService.GetGuiPanel<QueueDragPanel>().Bind(this.City);
		base.GuiService.GetGuiPanel<QueueDragPanel>().CancelDrag();
		if (this.ConstructionQueue != null)
		{
			this.constructions = this.ConstructionQueue.PendingConstructions;
			this.ConstructionQueue.CollectionChanged += this.ConstructionQueue_CollectionChanged;
		}
		else
		{
			this.constructions = new List<Construction>().AsReadOnly();
		}
		GuiElement guiElement = null;
		for (int i = 0; i < this.typesFIDS.Count; i++)
		{
			base.GuiService.GuiPanelHelper.TryGetGuiElement(this.typesFIDS[i], out guiElement, this.City.Empire.Faction.Name);
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
				simulationPropertyTooltipData.Context = this.City;
			}
		}
		this.GuiEmpire = new GuiEmpire(this.City.Empire);
		if (this.GuiEmpire != null)
		{
			this.OwnerFactionIconSmall.TintColor = this.GuiEmpire.Color;
			this.OwnerFactionIconSmall.Image = this.GuiEmpire.GuiFaction.GetImageTexture(global::GuiPanel.IconSize.LogoLarge, true);
		}
		this.GuiPreviousEmpire = new GuiEmpire(this.City.LastNonInfectedOwner);
		if (this.GuiPreviousEmpire != null)
		{
			this.IntegratedFactionIconLarge.Image = this.GuiPreviousEmpire.GuiFaction.GetImageTexture(global::GuiPanel.IconSize.LogoLarge, true);
			this.LastOwnerCityFactionTitle.Text = this.GuiPreviousEmpire.Empire.Faction.LocalizedName;
		}
		this.IntegratedFactionIconTooltip.Class = "Descriptor";
		this.IntegratedFactionIconTooltip.ClientData = this.City.Empire;
		this.IntegratedTraitTitle.AgeTransform.AgeTooltip.Class = "Descriptor";
		this.IntegratedTraitTitle.AgeTransform.AgeTooltip.ClientData = this.City.Empire;
		Faction faction = this.City.LastNonInfectedOwner.Faction;
		if (faction != null)
		{
			List<string> list = new List<string>();
			foreach (SimulationDescriptor simulationDescriptor in faction.GetIntegrationDescriptors())
			{
				this.IntegratedFactionIconTooltip.Content = simulationDescriptor.Name;
				this.IntegratedTraitTitle.AgeTransform.AgeTooltip.Content = simulationDescriptor.Name;
				if (base.GuiService.GuiPanelHelper.TryGetGuiElement(simulationDescriptor.Name, out guiElement))
				{
					this.IntegratedTraitTitle.Text = "\\7765\\ " + AgeLocalizer.Instance.LocalizeString(guiElement.Title);
				}
			}
		}
		if (faction.Affinity.Name == this.City.Empire.Faction.Affinity.Name)
		{
			this.IntegratedTraitTitle.Text = "%OCPanelIntegratedFactionSameAffinityTitle";
			this.IntegratedTraitTitle.AgeTransform.AgeTooltip.Class = string.Empty;
			this.IntegratedTraitTitle.AgeTransform.AgeTooltip.ClientData = null;
			this.IntegratedTraitTitle.AgeTransform.AgeTooltip.Content = "%OCPanelIntegratedFactionSameAffinityTooltip";
			this.IntegratedFactionIconTooltip.Class = string.Empty;
			this.IntegratedFactionIconTooltip.ClientData = null;
			this.IntegratedFactionIconTooltip.Content = "%OCPanelIntegratedFactionSameAffinityTooltip";
		}
		bool enable = false;
		this.ActionToogle.AgeTransform.Enable = enable;
		this.ActionToogle.AgeTransform.Alpha = ((!this.ActionToogle.AgeTransform.Enable) ? 0.5f : 1f);
		this.ModifierSector.GetComponent<AgePrimitiveSector>().MaxAngle = 0f;
		this.ModifierSector.Reset();
	}

	public void Unbind()
	{
		base.GuiService.GetGuiPanel<QueueDragPanel>().Unbind();
		this.GuiEmpire = null;
		this.GuiPreviousEmpire = null;
		this.constructions = null;
		this.previousConstructionSurplus.Clear();
		if (this.ConstructionQueue != null)
		{
			this.ConstructionQueue.CollectionChanged -= this.ConstructionQueue_CollectionChanged;
			this.ConstructionQueue = null;
		}
		if (this.Empire != null)
		{
			this.Empire.Refreshed -= this.Simulation_Refreshed;
			this.Empire = null;
		}
		if (this.City != null)
		{
			this.City = null;
		}
		for (int i = 0; i < this.valuesFIDS.Count; i++)
		{
			SimulationPropertyTooltipData simulationPropertyTooltipData = this.valuesFIDS[i].AgeTransform.AgeTooltip.ClientData as SimulationPropertyTooltipData;
			if (simulationPropertyTooltipData != null)
			{
				simulationPropertyTooltipData.Context = null;
			}
		}
		this.IsOtherEmpire = true;
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		this.previousConstructionSurplus.Clear();
		this.alreadyPassedTurns = 0;
		bool isIntegrated = this.City.IsIntegrated;
		this.TotalValuesTable.Visible = isIntegrated;
		this.FIDSIBackground.Visible = isIntegrated;
		for (int i = 0; i < this.typesFIDS.Count; i++)
		{
			if (i < this.valuesFIDS.Count)
			{
				this.valuesFIDS[i].AgeTransform.Alpha = 1f;
				this.valuesFIDS[i].AgeTransform.ResetAllModifiers(true, false);
				float propertyValue = this.City.GetPropertyValue(this.typesFIDS[i]);
				this.valuesFIDS[i].Text = GuiFormater.FormatGui(propertyValue, false, false, false, 0);
			}
		}
		bool flag = this.GetCurrentConstructionRaze() != null;
		DepartmentOfPlanificationAndDevelopment agency = this.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
		if (this.ConstructionQueue.PendingConstructions.Count > 0 && !flag && !agency.HasIntegratedFaction(this.City.LastNonInfectedOwner.Faction))
		{
			int num2;
			bool flag2;
			int num = this.ComputeRemainingTurn(this.City, this.ConstructionQueue.Peek(), this.previousConstructionSurplus, ref this.alreadyPassedTurns, out num2, out flag2);
			this.IntegrateStatusTitle.Text = string.Format(AgeLocalizer.Instance.LocalizeString("%IntegratingCityRemainTurns"), num);
			this.IntegrateStatusTitle.TintColor = Color.white;
		}
		else
		{
			this.IntegrateStatusTitle.Text = AgeLocalizer.Instance.LocalizeString("%IntegratingCityComplete");
			this.IntegrateStatusTitle.TintColor = Color.green;
		}
		Faction faction = this.City.LastNonInfectedOwner.Faction;
		if (faction.Affinity.Name == this.City.Empire.Faction.Affinity.Name)
		{
			this.IntegratedTraitTitle.Text = "%OCPanelIntegratedFactionSameAffinityTitle";
			this.IntegratedTraitTitle.AgeTransform.AgeTooltip.Class = string.Empty;
			this.IntegratedTraitTitle.AgeTransform.AgeTooltip.ClientData = null;
			this.IntegratedTraitTitle.AgeTransform.AgeTooltip.Content = "%OCPanelIntegratedFactionSameAffinityTooltip";
			this.IntegratedFactionIconTooltip.Class = string.Empty;
			this.IntegratedFactionIconTooltip.ClientData = null;
			this.IntegratedFactionIconTooltip.Content = "%OCPanelIntegratedFactionSameAffinityTooltip";
		}
		bool flag3 = this.CanRazeCity();
		bool flag4 = this.OvergrownCityRazeCooldownReached();
		this.ActionToogle.AgeTransform.Enable = flag3;
		this.ActionToogle.AgeTransform.Alpha = ((!flag3) ? 0.5f : 1f);
		this.ConstructibleActionIcon.AgeTransform.Alpha = ((!flag3) ? 0.5f : 1f);
		this.ActionToogle.State = false;
		if (this.City.BesiegingEmpire != null)
		{
			this.ActionToogle.AgeTransform.AgeTooltip.Content = OvergrownCityPanel.RazeOvergrownCityNotAvailableCityUnderSiegeString;
		}
		else if (flag)
		{
			this.ActionToogle.AgeTransform.AgeTooltip.Content = OvergrownCityPanel.RazeOvergrownCityQueuedDescriptionString;
			this.ActionToogle.State = true;
		}
		else if (flag4)
		{
			this.ActionToogle.AgeTransform.AgeTooltip.Content = OvergrownCityPanel.RazeOvergrownCityDescriptionString;
		}
		else
		{
			int num3 = this.OvergrownCityRazeCooldownRemainTurns();
			this.ActionToogle.AgeTransform.AgeTooltip.Content = string.Format(AgeLocalizer.Instance.LocalizeString(OvergrownCityPanel.RazeOvergrownCityOnCooldownDescriptionString), num3);
		}
		if (this.ActionToogle.State && !this.ModifierSector.IsStarted())
		{
			this.ModifierSector.StartAnimation();
		}
		else if (!this.ActionToogle.State && this.ModifierSector.IsStarted())
		{
			this.ModifierSector.Reset();
		}
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		this.interactionsAllowed = this.playerControllerRepository.ActivePlayerController.CanSendOrders();
		bool cityIntegrated = this.City.IsIntegrated;
		this.TotalValuesTable.Visible = cityIntegrated;
		this.FIDSIBackground.Visible = cityIntegrated;
		base.NeedRefresh = true;
		yield break;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		base.NeedRefresh = false;
		this.previousConstructionSurplus.Clear();
		base.GuiService.GetGuiPanel<QueueDragPanel>().CancelDrag();
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		this.playerControllerRepository = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
		this.valuesFIDS = new List<AgePrimitiveLabel>();
		this.typesFIDS = new List<StaticString>();
		this.typesFIDS.Add(SimulationProperties.DistrictFood);
		this.typesFIDS.Add(SimulationProperties.DistrictIndustry);
		this.typesFIDS.Add(SimulationProperties.DistrictScience);
		this.typesFIDS.Add(SimulationProperties.DistrictDust);
		this.typesFIDS.Add(SimulationProperties.DistrictCityPoint);
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
		Diagnostics.Assert(this.typesFIDS.Count == this.valuesFIDS.Count, "CREEPING NODE INFO PANEL : Invalid number of value FIDS");
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

	protected override void OnUnloadGame(IGame game)
	{
		this.Hide(true);
		this.Unbind();
		this.playerControllerRepository = null;
		base.OnUnloadGame(game);
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		base.UseRefreshLoop = true;
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

	private void ConstructionQueue_CollectionChanged(object sender, CollectionChangeEventArgs e)
	{
		this.constructions = this.ConstructionQueue.PendingConstructions;
		base.NeedRefresh = true;
	}

	private void DepartmentOfTheTreasury_ResourcePropertyChange(object sender, ResourcePropertyChangeEventArgs e)
	{
		if (base.IsVisible && e.ResourcePropertyName == SimulationProperties.BankAccount)
		{
			base.NeedRefresh = true;
		}
	}

	private void OnOrderResponse(object sender, TicketRaisedEventArgs args)
	{
		base.NeedRefresh = true;
	}

	private void Simulation_Refreshed(object sender)
	{
		base.NeedRefresh = true;
	}

	private int ComputeRemainingTurn(City city, Construction construction, List<ConstructionResourceStock> previousConstructionSurplus, ref int alreadyPassedTurns, out int worstNumberOfTurn, out bool checkPrerequisites)
	{
		worstNumberOfTurn = -1;
		checkPrerequisites = false;
		float num = -1f;
		float num2 = -1f;
		QueueGuiItem.GetConstructionTurnInfos(city, construction, previousConstructionSurplus, out worstNumberOfTurn, out num, out num2, out checkPrerequisites);
		if (worstNumberOfTurn >= 0 && worstNumberOfTurn != 2147483647)
		{
			alreadyPassedTurns += worstNumberOfTurn;
		}
		string s;
		if (worstNumberOfTurn == 2147483647)
		{
			s = QueueGuiItem.FormatNumberOfTurns(worstNumberOfTurn);
		}
		else if (worstNumberOfTurn == 0)
		{
			int numberOfTurn = Mathf.Max(1, alreadyPassedTurns);
			s = QueueGuiItem.FormatNumberOfTurns(numberOfTurn);
		}
		else
		{
			int numberOfTurn2 = Mathf.Max(1, alreadyPassedTurns);
			s = QueueGuiItem.FormatNumberOfTurns(numberOfTurn2);
		}
		base.AgeTransform.Alpha = ((!checkPrerequisites) ? 0.666f : 1f);
		if (num2 == 0f)
		{
			Diagnostics.LogWarning("The worst cost is 0. Construction = {0}.", new object[]
			{
				construction.ConstructibleElement.Name
			});
			num2 = 1f;
		}
		int result = 0;
		int.TryParse(s, out result);
		return result;
	}

	private void OnActionToogleSwitchCB(GameObject obj)
	{
		string message = string.Format(AgeLocalizer.Instance.LocalizeString(OvergrownCityPanel.ConfirmOrderRazeOvergrownCityString), new object[0]);
		Construction currentConstructionRaze = this.GetCurrentConstructionRaze();
		if (currentConstructionRaze != null)
		{
			this.DoRazeInfectedCity();
		}
		else
		{
			MessagePanel.Instance.Show(message, string.Empty, MessagePanelButtons.YesNo, new MessagePanel.EventHandler(this.ConfirmAction), MessagePanelType.IMPORTANT, new MessagePanelButton[0]);
		}
	}

	private void ConfirmAction(object sender, MessagePanelResultEventArgs e)
	{
		if (e.Result == MessagePanelResult.Yes && this.City != null)
		{
			this.DoRazeInfectedCity();
		}
		this.ActionToogle.State = (this.GetCurrentConstructionRaze() != null);
		if (this.ActionToogle.State && !this.ModifierSector.IsStarted())
		{
			this.ModifierSector.StartAnimation();
		}
		else if (!this.ActionToogle.State && this.ModifierSector.IsStarted())
		{
			this.ModifierSector.Reset();
		}
	}

	private void DoRazeInfectedCity()
	{
		Construction currentConstructionRaze = this.GetCurrentConstructionRaze();
		if (currentConstructionRaze != null)
		{
			OrderCancelConstruction order = new OrderCancelConstruction(this.City.Empire.Index, this.City.GUID, currentConstructionRaze.GUID);
			Ticket ticket;
			this.playerControllerRepository.ActivePlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
		}
		else
		{
			bool flag = !Amplitude.Unity.Runtime.Runtime.Registry.GetValue<bool>(OvergrownCityPanel.gameplaySettingOvergrownCityImmediateRazePath, true);
			if (flag)
			{
				DepartmentOfIndustry agency = this.City.Empire.GetAgency<DepartmentOfIndustry>();
				IEnumerable<DepartmentOfIndustry.ConstructibleElement> availableConstructibleElementsAsEnumerable = agency.ConstructibleElementDatabase.GetAvailableConstructibleElementsAsEnumerable(new StaticString[0]);
				DepartmentOfIndustry.ConstructibleElement constructibleElement = availableConstructibleElementsAsEnumerable.FirstOrDefault((DepartmentOfIndustry.ConstructibleElement m) => m.Name == OvergrownCityPanel.CityConstructibleActionInfectedRaze);
				OrderQueueConstruction order2 = new OrderQueueConstruction(this.City.Empire.Index, this.City.GUID, constructibleElement, string.Empty);
				Ticket ticket2;
				this.playerControllerRepository.ActivePlayerController.PostOrder(order2, out ticket2, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
			}
			else
			{
				WorldPosition firstAvailablePositionForArmyCreation = this.GetFirstAvailablePositionForArmyCreation();
				if (this.City.StandardUnits.Count > 0 && firstAvailablePositionForArmyCreation.IsValid)
				{
					OrderTransferGarrisonToNewArmy order3 = new OrderTransferGarrisonToNewArmy(this.City.Empire.Index, this.City.GUID, (from m in this.City.Units
					select m.GUID).ToArray<GameEntityGUID>(), firstAvailablePositionForArmyCreation, StaticString.Empty, false, false, false);
					Ticket ticket3;
					this.playerControllerRepository.ActivePlayerController.PostOrder(order3, out ticket3, new EventHandler<TicketRaisedEventArgs>(this.OrderTransferGarrisonToNewArmy_TicketRaised));
				}
				else
				{
					this.SendOrderDestroyCity();
				}
				this.Hide(true);
			}
		}
	}

	private void OrderTransferGarrisonToNewArmy_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		if (e.Result == PostOrderResponse.Processed)
		{
			this.SendOrderDestroyCity();
		}
	}

	private void SendOrderDestroyCity()
	{
		OrderDestroyCity order = new OrderDestroyCity(this.City.Empire.Index, this.City.GUID, true, true, -1);
		this.playerControllerRepository.ActivePlayerController.PostOrder(order);
	}

	private WorldPosition GetFirstAvailablePositionForArmyCreation()
	{
		return DepartmentOfDefense.GetFirstAvailablePositionToTransferGarrisonUnits(this.City, 3, PathfindingMovementCapacity.Ground, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreFogOfWar);
	}

	private int OvergrownCityRazeCooldownRemainTurns()
	{
		int turn = (base.GameService.Game as global::Game).Turn;
		int num = (int)this.City.SimulationObject.GetPropertyValue(SimulationProperties.CityOwnedTurn);
		int num2 = (int)this.City.SimulationObject.GetPropertyValue(SimulationProperties.OvergrownCityRazeCityCooldownInTurns);
		return num + num2 - turn;
	}

	private bool OvergrownCityRazeCooldownReached()
	{
		return this.OvergrownCityRazeCooldownRemainTurns() <= 0;
	}

	private bool CanRazeCity()
	{
		bool flag = this.OvergrownCityRazeCooldownReached();
		bool flag2 = this.City.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>().HasIntegratedFaction(this.City.LastNonInfectedOwner.Faction);
		if (this.City.LastNonInfectedOwner.Faction.Affinity.Name == this.City.Empire.Faction.Affinity.Name)
		{
			flag2 = true;
		}
		return flag2 && this.City.BesiegingEmpire == null && flag && !this.IsOtherEmpire;
	}

	private Construction GetCurrentConstructionRaze()
	{
		if (this.ConstructionQueue.PendingConstructions.Count > 0)
		{
			Construction construction = this.ConstructionQueue.Peek();
			if (construction != null && construction.ConstructibleElement is DepartmentOfIndustry.ConstructibleElement)
			{
				DepartmentOfIndustry.ConstructibleElement constructibleElement = construction.ConstructibleElement as DepartmentOfIndustry.ConstructibleElement;
				if (constructibleElement != null && constructibleElement.Name == OvergrownCityPanel.CityConstructibleActionInfectedRaze)
				{
					return construction;
				}
			}
		}
		return null;
	}

	public static readonly StaticString ConfirmOrderRazeOvergrownCityString = "%ConfirmOrderRazeOvergrownCity";

	public static readonly StaticString RazeOvergrownCityNotAvailableCityUnderSiegeString = "%RazeOvergrownCityNotAvailableCityUnderSiege";

	public static readonly StaticString RazeOvergrownCityQueuedDescriptionString = "%RazeOvergrownCityQueuedDescription";

	public static readonly StaticString RazeOvergrownCityOnCooldownDescriptionString = "%RazeOvergrownCityOnCooldownDescription";

	public static readonly StaticString RazeOvergrownCityDescriptionString = "%RazeOvergrownCityDescription";

	public static StaticString CityConstructibleActionInfectedRaze = "CityConstructibleActionInfectedRaze";

	public static StaticString gameplaySettingOvergrownCityImmediateRazePath = "Gameplay/OvergrownCity/OvergrownCityRazeImmediate";

	public AgeTransform FIDSIBackground;

	public AgeTransform TotalValuesTable;

	public AgeTooltip IntegratedFactionIconTooltip;

	public AgePrimitiveImage IntegratedFactionIconLarge;

	public AgePrimitiveImage OwnerFactionIconSmall;

	public AgePrimitiveLabel LastOwnerCityFactionTitle;

	public AgePrimitiveLabel IntegratedTraitTitle;

	public AgePrimitiveLabel IntegrateStatusTitle;

	public AgePrimitiveImage ConstructibleActionIcon;

	public AgeControlToggle ActionToogle;

	public AgeModifierSector ModifierSector;

	private List<StaticString> typesFIDS;

	private List<AgePrimitiveLabel> valuesFIDS;

	private GuiEmpire guiEmpire;

	private GuiEmpire guiPreviousEmpire;

	private List<ConstructionResourceStock> previousConstructionSurplus = new List<ConstructionResourceStock>();

	private ReadOnlyCollection<Construction> constructions;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private int alreadyPassedTurns;

	private bool interactionsAllowed = true;

	private IPlayerControllerRepositoryService playerControllerRepository;

	private IEndTurnService endTurnService;

	private Construction pendingConstructionToCancel;
}
