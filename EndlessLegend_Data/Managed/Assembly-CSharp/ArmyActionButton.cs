using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using Amplitude.Unity.View;
using UnityEngine;

public class ArmyActionButton : MonoBehaviour
{
	public Army Army { get; private set; }

	public ArmyAction ArmyAction { get; private set; }

	public IPlayerControllerRepositoryService PlayerControllerRepositoryService { get; private set; }

	public GameObject Client { get; private set; }

	[Service]
	private ISeasonService SeasonService { get; set; }

	[Service]
	private ICursorService CursorService { get; set; }

	public void Bind(IPlayerControllerRepositoryService playerControllerRepositoryService, Army army, ArmyAction armyAction, AgeTransform anchor, GameObject client, int index, UnitListPanel unitListPanel)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service == null || service.Game == null || this.PlayerControllerRepositoryService != null || this.Army != null || this.ArmyAction != null || this.SeasonService != null)
		{
			this.Unbind();
		}
		this.unitListPanel = unitListPanel;
		this.SeasonService = service.Game.Services.GetService<ISeasonService>();
		if (this.SeasonService != null)
		{
			this.SeasonService.SeasonChange += this.SeasonService_SeasonChange;
		}
		this.CursorService = Services.GetService<ICursorService>();
		this.PlayerControllerRepositoryService = playerControllerRepositoryService;
		this.Army = army;
		this.ArmyAction = armyAction;
		this.Client = client;
		this.ActionToggle.AgeTransform.Visible = (armyAction is IArmyActionWithToggle);
		this.ActionButton.AgeTransform.Visible = !(armyAction is IArmyActionWithToggle);
		this.ModifierSector.Reset();
		this.ModifierSector.GetComponent<AgePrimitiveSector>().MaxAngle = 0f;
		this.ActionImage.Image = null;
		this.ModifierSector.Reset();
		IGuiPanelHelper guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
		Diagnostics.Assert(guiPanelHelper != null, "Unable to access GuiPanelHelper");
		GuiElement guiElement;
		Texture2D image;
		if (guiPanelHelper.TryGetGuiElement(armyAction.Name, out guiElement) && guiPanelHelper.TryGetTextureFromIcon(guiElement, global::GuiPanel.IconSize.Small, out image))
		{
			this.ActionImage.Image = image;
		}
		this.AgeTransform.AgeTooltip.Anchor = anchor;
		if (this.ArmyAction is IArmyActionWithUnitSelection)
		{
			this.unitListPanel.SelectionChange += this.UnitListPanel_SelectionChange;
		}
		this.RefreshCanExecute();
	}

	public void RefreshCanExecute()
	{
		if (this.ArmyAction != null && !this.Army.IsEmpty)
		{
			this.failure.Clear();
			bool flag;
			if (this.ArmyAction is IArmyActionWithTargetSelection && this.ArmyAction is IArmyActionWithUnitSelection)
			{
				this.armyActionTargets.Clear();
				(this.ArmyAction as IArmyActionWithTargetSelection).FillTargets(this.Army, this.armyActionTargets, ref this.failure);
				flag = this.ArmyAction.CanExecute(this.Army, ref this.failure, new object[]
				{
					this.unitListPanel.SelectUnits,
					this.armyActionTargets
				});
			}
			else if (this.ArmyAction is IArmyActionWithTargetSelection)
			{
				this.armyActionTargets.Clear();
				(this.ArmyAction as IArmyActionWithTargetSelection).FillTargets(this.Army, this.armyActionTargets, ref this.failure);
				flag = this.ArmyAction.CanExecute(this.Army, ref this.failure, new object[]
				{
					this.armyActionTargets
				});
			}
			else if (this.ArmyAction is IArmyActionWithUnitSelection)
			{
				flag = this.ArmyAction.CanExecute(this.Army, ref this.failure, new object[]
				{
					this.unitListPanel.SelectUnits
				});
			}
			else
			{
				flag = this.ArmyAction.CanExecute(this.Army, ref this.failure, new object[0]);
			}
			flag &= (!this.failure.Contains(ArmyAction.NoCanDoWhileMoving) && !this.failure.Contains(ArmyAction.NoCanDoWhileTutorial));
			if (!this.failure.Contains(ArmyAction.NoCanDoWhileHidden))
			{
				this.AgeTransform.AgeTooltip.Content = string.Empty;
				IGuiPanelHelper guiPanelHelper = Services.GetService<Amplitude.Unity.Gui.IGuiService>().GuiPanelHelper;
				GuiElement guiElement;
				if (guiPanelHelper.TryGetGuiElement(this.ArmyAction.Name, out guiElement))
				{
					if (this.failure.Count == 0)
					{
						this.AgeTransform.AgeTooltip.Content = "#50FF50#" + AgeLocalizer.Instance.LocalizeString(guiElement.Title).ToUpper() + "#REVERT#";
					}
					else
					{
						this.AgeTransform.AgeTooltip.Content = "#FF5050#" + AgeLocalizer.Instance.LocalizeString(guiElement.Title).ToUpper() + "#REVERT#";
					}
				}
				if (this.ArmyAction is IArmyActionWithToggle)
				{
					bool flag2 = this.failure.Contains(ArmyAction.NoCanDoWhileToggledOn);
					if (flag2)
					{
						this.failure.RemoveAll((StaticString failureFlag) => failureFlag.Equals(ArmyAction.NoCanDoWhileToggledOn));
					}
					IArmyActionWithToggle armyActionWithToggle = this.ArmyAction as IArmyActionWithToggle;
					this.ActionToggle.AgeTransform.Enable = flag;
					this.ActionImage.AgeTransform.Alpha = ((this.ActionToggle.AgeTransform.Enable && !flag2) ? 1f : 0.5f);
					this.ActionToggle.State = ((flag || flag2) && armyActionWithToggle.IsToggled(this.Army));
					if (this.ActionToggle.State && !this.ModifierSector.IsStarted())
					{
						this.ModifierSector.StartAnimation();
					}
					else if (!this.ActionToggle.State && this.ModifierSector.IsStarted())
					{
						this.ModifierSector.Reset();
					}
					if (this.failure.Count == 0)
					{
						if (this.ActionToggle.State)
						{
							StaticString toggledOnDescriptionOverride = armyActionWithToggle.ToggledOnDescriptionOverride;
							if (!StaticString.IsNullOrEmpty(toggledOnDescriptionOverride))
							{
								AgeTooltip ageTooltip = this.AgeTransform.AgeTooltip;
								ageTooltip.Content = ageTooltip.Content + " : " + AgeLocalizer.Instance.LocalizeString("%" + toggledOnDescriptionOverride);
							}
							else
							{
								AgeTooltip ageTooltip2 = this.AgeTransform.AgeTooltip;
								ageTooltip2.Content = ageTooltip2.Content + " : " + AgeLocalizer.Instance.LocalizeString(this.ArmyAction.FormatDescription("%" + this.ArmyAction.Name + "OffDescription"));
							}
						}
						else
						{
							StaticString toggledOffDescriptionOverride = armyActionWithToggle.ToggledOffDescriptionOverride;
							if (!StaticString.IsNullOrEmpty(toggledOffDescriptionOverride))
							{
								AgeTooltip ageTooltip3 = this.AgeTransform.AgeTooltip;
								ageTooltip3.Content = ageTooltip3.Content + " : " + AgeLocalizer.Instance.LocalizeString("%" + toggledOffDescriptionOverride);
							}
							else
							{
								AgeTooltip ageTooltip4 = this.AgeTransform.AgeTooltip;
								ageTooltip4.Content = ageTooltip4.Content + " : " + AgeLocalizer.Instance.LocalizeString(this.ArmyAction.FormatDescription("%" + this.ArmyAction.Name + "OnDescription"));
							}
						}
					}
				}
				else
				{
					this.ActionButton.AgeTransform.Enable = flag;
					this.ActionImage.AgeTransform.Alpha = ((!this.ActionButton.AgeTransform.Enable) ? 0.5f : 1f);
					if (this.failure.Count == 0)
					{
						AgeTooltip ageTooltip5 = this.AgeTransform.AgeTooltip;
						ageTooltip5.Content = ageTooltip5.Content + " : " + AgeLocalizer.Instance.LocalizeString(this.ArmyAction.FormatDescription("%" + this.ArmyAction.Name + "Description"));
						if (this.ArmyAction is ArmyActionWithCooldown)
						{
							float num = (this.ArmyAction as ArmyActionWithCooldown).ComputeCooldownDuration(this.Army);
							this.AgeTransform.AgeTooltip.Content = this.AgeTransform.AgeTooltip.Content.Replace("$CooldownDuration", num.ToString());
						}
					}
				}
				if (this.failure.Count > 0)
				{
					AgeTooltip ageTooltip6 = this.AgeTransform.AgeTooltip;
					ageTooltip6.Content = ageTooltip6.Content + " : " + AgeLocalizer.Instance.LocalizeString(this.ArmyAction.FormatDescription("%" + this.failure[0] + "Description"));
					if (this.ArmyAction is ArmyActionWithCooldown)
					{
						float num2 = (this.ArmyAction as ArmyActionWithCooldown).ComputeRemainingCooldownDuration(this.Army);
						this.AgeTransform.AgeTooltip.Content = this.AgeTransform.AgeTooltip.Content.Replace("$RemainingCooldownDuration", num2.ToString());
					}
				}
			}
			float costInActionPoints = this.ArmyAction.GetCostInActionPoints();
			if (costInActionPoints > 0f)
			{
				AgeTooltip ageTooltip7 = this.AgeTransform.AgeTooltip;
				ageTooltip7.Content = ageTooltip7.Content + "\n \n" + AgeLocalizer.Instance.LocalizeString("%ArmyActionPointRequirementFormat").Replace("$Value", costInActionPoints.ToString());
			}
			for (int i = 0; i < this.ArmyAction.Costs.Length; i++)
			{
				IConstructionCost constructionCost = this.ArmyAction.Costs[i];
				string text = constructionCost.ResourceName;
				if (!string.IsNullOrEmpty(text) && !text.Equals(DepartmentOfTheTreasury.Resources.ActionPoint))
				{
					float costForResource = this.ArmyAction.GetCostForResource(text, this.Army.Empire);
					if (costForResource != 0f)
					{
						DepartmentOfTheTreasury agency = this.Army.Empire.GetAgency<DepartmentOfTheTreasury>();
						float f = 0f;
						if (agency.TryGetResourceStockValue(this.Army.Empire.SimulationObject, text, out f, false))
						{
							IGuiPanelHelper guiPanelHelper2 = Services.GetService<global::IGuiService>().GuiPanelHelper;
							GuiElement guiElement2;
							if (guiPanelHelper2.TryGetGuiElement(text, out guiElement2) && guiElement2 is ExtendedGuiElement)
							{
								ExtendedGuiElement extendedGuiElement = guiElement2 as ExtendedGuiElement;
								if (extendedGuiElement == null)
								{
									Diagnostics.LogError("Resource extended gui is 'null'.");
								}
								string text2 = AgeLocalizer.Instance.LocalizeString(extendedGuiElement.Title);
								string symbolString = extendedGuiElement.SymbolString;
								string arg = string.Format(AgeLocalizer.Instance.LocalizeString("%ArmyActionResourceRequirementFormat"), new object[]
								{
									costForResource,
									text2,
									symbolString,
									Mathf.Ceil(f)
								});
								this.AgeTransform.AgeTooltip.Content = string.Format("{0}\n\n\n{1}", this.AgeTransform.AgeTooltip.Content, arg);
							}
							else
							{
								Diagnostics.LogError("Could not retrieve the resource gui element.");
							}
						}
					}
				}
			}
		}
	}

	public void Unbind()
	{
		if (this.Client != null)
		{
			this.Client = null;
		}
		if (this.Army != null)
		{
			this.Army = null;
		}
		if (this.ArmyAction != null)
		{
			this.ArmyAction = null;
		}
		if (this.PlayerControllerRepositoryService != null)
		{
			this.PlayerControllerRepositoryService = null;
		}
		if (this.SeasonService != null)
		{
			this.SeasonService.SeasonChange -= this.SeasonService_SeasonChange;
			this.SeasonService = null;
		}
		if (this.unitListPanel != null)
		{
			this.unitListPanel.SelectionChange -= this.UnitListPanel_SelectionChange;
			this.unitListPanel = null;
		}
	}

	private void ArmyAction_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		this.RefreshCanExecute();
	}

	private void Click()
	{
		if (this.ArmyAction == null)
		{
			return;
		}
		bool flag = false;
		object[] customAttributes = this.ArmyAction.GetType().GetCustomAttributes(typeof(WorldPlacementCursorAttribute), true);
		if (customAttributes != null && customAttributes.Length > 0)
		{
			flag = true;
			if (this.ArmyAction is IArmyActionWithToggle && (this.ArmyAction as IArmyActionWithToggle).IsToggled(this.Army))
			{
				flag = false;
			}
		}
		if (flag)
		{
			if (this.CursorService != null)
			{
				this.CursorService.Backup();
				Diagnostics.Assert(customAttributes.Length == 1);
				WorldPlacementCursorAttribute worldPlacementCursorAttribute = customAttributes[0] as WorldPlacementCursorAttribute;
				Type type = worldPlacementCursorAttribute.Type;
				if (this.ArmyAction is IArmyActionWithUnitSelection)
				{
					this.CursorService.ChangeCursor(type, new object[]
					{
						this.Army,
						this.ArmyAction,
						this.unitListPanel.SelectUnits.ToArray()
					});
				}
				else
				{
					this.CursorService.ChangeCursor(type, new object[]
					{
						this.Army,
						this.ArmyAction
					});
				}
			}
		}
		else
		{
			this.failure.Clear();
			if (this.ArmyAction != null)
			{
				if (this.ArmyAction is IArmyActionWithUnitSelection)
				{
					if (this.ArmyAction.CanExecute(this.Army, ref this.failure, this.unitListPanel.SelectUnits.ToArray()))
					{
						Ticket ticket;
						this.ArmyAction.Execute(this.Army, this.PlayerControllerRepositoryService.ActivePlayerController, out ticket, new EventHandler<TicketRaisedEventArgs>(this.ArmyAction_TicketRaised), this.unitListPanel.SelectUnits.ToArray());
					}
				}
				else if (this.ArmyAction is IArmyActionWithTargetSelection)
				{
					this.armyActionTargets.Clear();
					(this.ArmyAction as IArmyActionWithTargetSelection).FillTargets(this.Army, this.armyActionTargets, ref this.failure);
					if (this.ArmyAction.CanExecute(this.Army, ref this.failure, new object[]
					{
						this.armyActionTargets
					}))
					{
						Ticket ticket2;
						this.ArmyAction.Execute(this.Army, this.PlayerControllerRepositoryService.ActivePlayerController, out ticket2, new EventHandler<TicketRaisedEventArgs>(this.ArmyAction_TicketRaised), this.armyActionTargets.ToArray());
					}
				}
				else if (this.ArmyAction.CanExecute(this.Army, ref this.failure, new object[0]))
				{
					Ticket ticket3;
					this.ArmyAction.Execute(this.Army, this.PlayerControllerRepositoryService.ActivePlayerController, out ticket3, new EventHandler<TicketRaisedEventArgs>(this.ArmyAction_TicketRaised), new object[0]);
				}
			}
		}
		this.RefreshCanExecute();
	}

	private void OnArmyActionButtonCB(object context)
	{
		this.Click();
	}

	private void OnArmyActionSwitchCB(object context)
	{
		this.Click();
	}

	private void OnDestroy()
	{
		this.RefreshRate = 0f;
		this.Unbind();
	}

	private void SeasonService_SeasonChange(object sender, SeasonChangeEventArgs e)
	{
		this.RefreshCanExecute();
	}

	private void UnitListPanel_SelectionChange(object sender, EventArgs e)
	{
		this.RefreshCanExecute();
	}

	private void Update()
	{
		if (this.RefreshRate > 0f && Time.time >= this.lastRefreshedTime + 1f / this.RefreshRate)
		{
			this.RefreshCanExecute();
			this.lastRefreshedTime = Time.time;
		}
	}

	public AgeTransform AgeTransform;

	public AgePrimitiveImage ActionImage;

	public AgeControlButton ActionButton;

	public AgeControlToggle ActionToggle;

	public AgeModifierSector ModifierSector;

	public float RefreshRate = 15f;

	private float lastRefreshedTime;

	private List<IGameEntity> armyActionTargets = new List<IGameEntity>();

	private List<StaticString> failure = new List<StaticString>();

	private UnitListPanel unitListPanel;
}
