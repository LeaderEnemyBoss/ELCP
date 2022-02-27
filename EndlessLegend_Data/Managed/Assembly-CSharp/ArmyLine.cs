using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using UnityEngine;

public class ArmyLine : SortedLine
{
	public ArmyLine()
	{
		this.temp = string.Empty;
		this.profanityError = string.Empty;
		this.invalidColor = new Color(0.7529412f, 0.2509804f, 0.2509804f);
	}

	public Army Army { get; private set; }

	public IComparable Comparable { get; set; }

	public string ArmyName
	{
		get
		{
			if (this.Army != null)
			{
				return this.ArmyNameLabel.Text;
			}
			return "ZZZZZZZ";
		}
	}

	public string ArmyHero
	{
		get
		{
			if (this.Army != null && this.Army.Hero != null)
			{
				UnitProfile unitProfile = this.Army.Hero.UnitDesign as UnitProfile;
				global::IGuiService service = Services.GetService<global::IGuiService>();
				GuiElement guiElement;
				if (service.GuiPanelHelper.TryGetGuiElement(unitProfile.ProfileGuiElementName, out guiElement))
				{
					return AgeLocalizer.Instance.LocalizeString(guiElement.Title);
				}
			}
			return "ZZZZZZZ";
		}
	}

	public int ArmyCapacity
	{
		get
		{
			if (this.Army != null)
			{
				return this.Army.CurrentUnitSlot;
			}
			return 0;
		}
	}

	public float ArmyHP
	{
		get
		{
			if (this.Army != null)
			{
				return this.Army.GetPropertyValue(SimulationProperties.Health);
			}
			return 0f;
		}
	}

	public float ArmyMovePoints
	{
		get
		{
			if (this.Army != null)
			{
				return this.Army.GetPropertyValue(SimulationProperties.Movement);
			}
			return 0f;
		}
	}

	public string ArmyPosition
	{
		get
		{
			if (this.Army != null)
			{
				return this.ArmyLocation.Text;
			}
			return "ZZZZZZZ";
		}
	}

	public float ArmyMilitaryUpkeep
	{
		get
		{
			if (this.Army != null)
			{
				return this.Army.GetPropertyValue(SimulationProperties.MilitaryUpkeep);
			}
			return 0f;
		}
	}

	public override AgeTransform AgeTransform
	{
		get
		{
			AgeTransform component = base.GetComponent<AgeTransform>();
			if (component == null)
			{
				Diagnostics.Assert(component != null, "The ArmyLine does not contain a AgeTransform component");
			}
			return component;
		}
	}

	public bool ReadOnly { get; set; }

	public void Bind(Army army, ArmyListPanel parent, global::Empire empire)
	{
		if (this.Army != null)
		{
			this.Unbind();
		}
		this.Army = army;
		if (this.Army != null)
		{
			this.Army.Refreshed += this.Army_Refreshed;
			this.Army.StandardUnitCollectionChange += this.Army_StandardUnitCollectionChange;
			bool flag;
			if (this.Army.StandardUnits.Count > 0)
			{
				flag = this.Army.StandardUnits.All((Unit unit) => unit.UnitDesign.Tags.Contains(DownloadableContent9.TagColossus));
			}
			else
			{
				flag = false;
			}
			this.isColossusArmy = flag;
			bool flag2;
			if (this.Army.StandardUnits.Count > 0)
			{
				flag2 = this.Army.StandardUnits.Any((Unit unit) => unit.UnitDesign.Tags.Contains(DownloadableContent20.TagKaijuMonster));
			}
			else
			{
				flag2 = false;
			}
			this.isKaijuArmy = flag2;
		}
		if (this.HeroCard != null)
		{
			Garrison.IsAcceptingHeroAssignmentReasonsEnum isAcceptingHeroAssignmentReasonsEnum = Garrison.IsAcceptingHeroAssignmentReasonsEnum.None;
			if (this.Army.Hero != null)
			{
				this.guiHero = new GuiHero(this.Army.Hero, null);
				this.HeroCard.HeroInspectionButton.AgeTransform.Enable = true;
			}
			else
			{
				this.guiHero = null;
				this.HeroCard.HeroInspectionButton.AgeTransform.Enable = (!this.Army.IsInEncounter && this.Army.IsAcceptingHeroAssignments(out isAcceptingHeroAssignmentReasonsEnum));
			}
			this.HeroCard.Bind(this.guiHero, this.Army.Empire, base.gameObject);
			AgeTooltip ageTooltip = this.HeroCard.HeroInspectionButton.AgeTransform.AgeTooltip;
			if (ageTooltip != null)
			{
				if (this.guiHero != null)
				{
					ageTooltip.Class = "Unit";
					ageTooltip.ClientData = this.guiHero;
					ageTooltip.Content = this.guiHero.Title;
				}
				else
				{
					ageTooltip.Class = string.Empty;
					if (!this.Army.IsInEncounter)
					{
						if (isAcceptingHeroAssignmentReasonsEnum == Garrison.IsAcceptingHeroAssignmentReasonsEnum.None)
						{
							ageTooltip.Content = "%HeroAssignDescription";
						}
						else
						{
							ageTooltip.Content = "%ArmyLineNoHeroAssignmentBecause" + isAcceptingHeroAssignmentReasonsEnum.ToString();
						}
					}
					else
					{
						ageTooltip.Content = "%ArmyLockedInBattleDescription";
					}
				}
			}
		}
		if (this.ArmyUpkeep != null)
		{
			AgeTransform ageTransform = this.ArmyUpkeep.AgeTransform.GetParent();
			if (ageTransform != null && ageTransform.AgeTooltip != null)
			{
				ageTransform.AgeTooltip.Class = "MilitaryUpkeep";
				ageTransform.AgeTooltip.Content = "MilitaryUpkeep";
				ageTransform.AgeTooltip.ClientData = new SimulationPropertyTooltipData(SimulationProperties.MilitaryUpkeep, SimulationProperties.MilitaryUpkeep, this.Army);
			}
		}
		this.parent = parent;
		this.departmentOfDefense = empire.GetAgency<DepartmentOfDefense>();
		this.ArmyNameTextField.ValidChars = AgeLocalizer.Instance.LocalizeString("%ArmyValidChars");
		Diagnostics.Assert(this.ArmyNameTextField.ValidChars != "%ArmyValidChars", "No localization found for %ArmyValidChars");
	}

	public void Unbind()
	{
		this.departmentOfDefense = null;
		this.isColossusArmy = false;
		this.isKaijuArmy = false;
		if (this.HeroCard != null)
		{
			this.HeroCard.Unbind();
			this.guiHero = null;
			AgeTooltip ageTooltip = this.HeroCard.HeroInspectionButton.AgeTransform.AgeTooltip;
			if (ageTooltip != null && this.guiHero != null)
			{
				ageTooltip.ClientData = null;
			}
		}
		if (this.Army != null)
		{
			this.Army.StandardUnitCollectionChange -= this.Army_StandardUnitCollectionChange;
			this.Army.Refreshed -= this.Army_Refreshed;
			this.Army = null;
		}
		if (this.ArmyUpkeep != null)
		{
			AgeTransform ageTransform = this.ArmyUpkeep.AgeTransform.GetParent();
			if (ageTransform != null && ageTransform.AgeTooltip != null)
			{
				ageTransform.AgeTooltip.ClientData = null;
			}
		}
		this.parent = null;
	}

	public void DisableIfArmyisBeingBesieged()
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		Diagnostics.Assert(service.Game != null);
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		if (service2 != null)
		{
			District district = service2.GetDistrict(this.Army.WorldPosition);
			if (district != null && district.City != null && district.City.BesiegingEmpire != null)
			{
				this.AgeTransform.Enable = false;
				this.AgeTransform.StopSearchingForTooltips = true;
				this.AgeTransform.AgeTooltip.Content = string.Empty;
			}
		}
	}

	public void DisableIfHasCatspaw()
	{
		if (this.Army.HasCatspaw)
		{
			this.AgeTransform.Enable = false;
			this.AgeTransform.StopSearchingForTooltips = true;
			this.AgeTransform.AgeTooltip.Content = string.Empty;
		}
	}

	public void DisableIfGarrisonIsInEncounter()
	{
		if (this.Army.IsInEncounter)
		{
			this.AgeTransform.Enable = false;
			this.AgeTransform.StopSearchingForTooltips = true;
			this.AgeTransform.AgeTooltip.Content = "%ArmyLockedInBattleDescription";
		}
	}

	public void DisableIfNoSlotLeft()
	{
		if (this.Army.CurrentUnitSlot >= this.Army.MaximumUnitSlot)
		{
			this.AgeTransform.Enable = false;
			this.AgeTransform.StopSearchingForTooltips = true;
			this.AgeTransform.AgeTooltip.Content = "%ArmyFullDescription";
			this.ArmySize.TintColor = Color.red;
		}
	}

	public void DisableIfDoesNotAcceptHeroAssignments()
	{
		Garrison.IsAcceptingHeroAssignmentReasonsEnum isAcceptingHeroAssignmentReasonsEnum;
		if (this.Army != null && !this.Army.IsAcceptingHeroAssignments(out isAcceptingHeroAssignmentReasonsEnum))
		{
			this.AgeTransform.Enable = false;
			this.AgeTransform.StopSearchingForTooltips = true;
			this.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%ArmyLineNoHeroAssignmentBecause" + isAcceptingHeroAssignmentReasonsEnum.ToString());
		}
	}

	public void DisableIfLockedHeroAssigned()
	{
		int num = 0;
		if (this.Army.Hero != null)
		{
			num = DepartmentOfEducation.LockedRemainingTurns(this.Army.Hero);
		}
		if (num > 0)
		{
			this.AgeTransform.Enable = false;
			this.AgeTransform.StopSearchingForTooltips = true;
			this.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%HeroAssignmentLockedTurnsDescription").Replace("$NumberOfTurns", num.ToString());
			this.HeroCard.HeroPortrait.TintColor = new Color(1f, 0.5f, 0.5f, 1f);
		}
	}

	public void RefreshContent()
	{
		this.AgeTransform.Enable = true;
		this.AgeTransform.StopSearchingForTooltips = false;
		this.AgeTransform.AgeTooltip.Content = string.Empty;
		this.ArmySize.TintColor = Color.white;
		this.HeroCard.HeroPortrait.TintColor = Color.white;
		if (this.ArmyNameLabel != null)
		{
			AgeUtils.TruncateString(this.Army.LocalizedName, this.ArmyNameLabel, out this.temp, '.');
			this.ArmyNameLabel.Text = this.temp;
			if (this.Army.IsInEncounter)
			{
				this.ArmyNameLabel.TintColor = this.ColorIfInBattle;
				this.ArmyNameLabel.AgeTransform.AgeTooltip.Content = "%ArmyLockedInBattleDescription";
			}
			else
			{
				this.ArmyNameLabel.TintColor = Color.white;
				this.ArmyNameLabel.AgeTransform.AgeTooltip.Content = string.Empty;
			}
		}
		if (this.HeroCard != null)
		{
			this.HeroCard.RefreshContent(this.isColossusArmy, this.isKaijuArmy);
		}
		if (this.ArmySize != null)
		{
			this.ArmySize.Text = this.Army.CurrentUnitSlot.ToString() + "/" + this.Army.MaximumUnitSlot.ToString();
			AgeTooltip ageTooltip = this.ArmySize.AgeTransform.AgeTooltip;
			if (ageTooltip != null)
			{
				if (this.Army.CurrentUnitSlot > this.Army.MaximumUnitSlot)
				{
					ageTooltip.Content = "%ArmyCapacityExceededDescription";
					this.ArmySize.TintColor = Color.red;
				}
				else
				{
					ageTooltip.Content = string.Empty;
				}
			}
		}
		if (this.ArmyHPLabel != null)
		{
			float propertyValue = this.Army.GetPropertyValue(SimulationProperties.Health);
			float propertyValue2 = this.Army.GetPropertyValue(SimulationProperties.MaximumHealth);
			float num = 0f;
			if (propertyValue2 > 0f)
			{
				num = 100f * (propertyValue / propertyValue2);
			}
			this.ArmyHPGauge.AgeTransform.PercentRight = num;
			if (num >= 75f)
			{
				this.ArmyHPGauge.TintColor = this.FullLifeColor;
			}
			else if (num >= 25f)
			{
				this.ArmyHPGauge.TintColor = this.HalfLifeColor;
			}
			else
			{
				this.ArmyHPGauge.TintColor = this.CriticalLifeColor;
			}
			this.ArmyHPLabel.Text = GuiFormater.FormatGui(Mathf.Floor(propertyValue), false, false, false, 1) + "/" + GuiFormater.FormatGui(Mathf.Floor(propertyValue2), false, false, false, 1);
		}
		if (this.ArmyMovement != null)
		{
			string str = GuiFormater.FormatGui(this.Army.GetPropertyValue(SimulationProperties.Movement), false, false, false, 1);
			string str2 = GuiFormater.FormatGui(this.Army.GetPropertyValue(SimulationProperties.MaximumMovement), false, false, false, 1);
			this.ArmyMovement.Text = str + "/" + str2;
		}
		if (this.ArmyLocation != null)
		{
			IGameService service = Services.GetService<IGameService>();
			this.ArmyLocation.Text = service.Game.Services.GetService<IWorldPositionningService>().GetRegion(this.Army.WorldPosition).LocalizedName;
		}
		if (this.ArmyUpkeep != null)
		{
			float propertyValue3 = this.Army.GetPropertyValue(SimulationProperties.MilitaryUpkeep);
			this.ArmyUpkeep.Text = GuiFormater.FormatQuantity(-propertyValue3, SimulationProperties.MilitaryUpkeep, 0);
		}
		if (this.HeroCard != null)
		{
			this.HeroCard.AgeTransform.Enable = !this.ReadOnly;
		}
		bool flag;
		if (this.Army.StandardUnits.Count > 0)
		{
			flag = this.Army.StandardUnits.All((Unit unit) => unit.UnitDesign.Tags.Contains(DownloadableContent9.TagColossus));
		}
		else
		{
			flag = false;
		}
		bool flag2 = flag;
		bool flag3;
		if (this.Army.StandardUnits.Count > 0)
		{
			flag3 = this.Army.StandardUnits.Any((Unit unit) => unit.UnitDesign.Tags.Contains(DownloadableContent20.TagKaijuMonster));
		}
		else
		{
			flag3 = false;
		}
		bool flag4 = flag3;
		this.ArmySize.AgeTransform.Visible = (!flag2 && !flag4);
		this.ColossusArmyLabel.AgeTransform.Visible = flag2;
		if (flag2)
		{
			this.ColossusArmyLabel.Text = "\\7805\\";
			this.ColossusArmyLabel.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%ArmyWithColossusDescription");
		}
		this.KaijuArmyLabel.AgeTransform.Visible = flag4;
		if (flag4)
		{
			string text = this.ArmySize.Text;
			this.KaijuArmyLabel.Text = "\\7100\\ " + text;
			this.KaijuArmyLabel.AgeTransform.AgeTooltip.Content = AgeLocalizer.Instance.LocalizeString("%ArmyWithKaijuMonsterDescription");
		}
		AgeTransform ageTransform = this.ArmyStatusLabel.AgeTransform.GetParent();
		if (this.Army.IsCamouflaged)
		{
			this.ArmyStatusLabel.AgeTransform.Visible = true;
			this.ArmyStatusLabel.Text = "\\7815\\";
			ageTransform.AgeTooltip.Content = "%ArmyCamouflagedDescription";
		}
		else if (this.Army.IsNaval)
		{
			this.ArmyStatusLabel.AgeTransform.Visible = true;
			this.ArmyStatusLabel.Text = "\\7825\\";
			ageTransform.AgeTooltip.Content = "%ArmyIsSeafaringDescription";
		}
		else
		{
			this.ArmyStatusLabel.AgeTransform.Visible = false;
			ageTransform.AgeTooltip.Content = string.Empty;
		}
		this.SelectionToggle.State = false;
	}

	public void StartRename()
	{
		if (this.Army != null)
		{
			this.ArmyNameLabel.Text = string.Empty;
			this.ArmyNameTextField.AgeTransform.Enable = true;
			AgeManager.Instance.FocusedControl = this.ArmyNameTextField;
			this.ArmyNameValidationFrame.AgeTransform.Visible = true;
		}
	}

	public void CancelRename()
	{
		if (this.ArmyNameTextField == AgeManager.Instance.ActiveControl)
		{
			this.ArmyNameLabel.Text = this.Army.LocalizedName;
			AgeManager.Instance.ActiveControl = null;
		}
	}

	private void Army_Refreshed(object sender)
	{
		if (this.Army != null)
		{
			this.RefreshContent();
		}
	}

	private void Army_StandardUnitCollectionChange(object sender, CollectionChangeEventArgs e)
	{
		if (this.Army != null)
		{
			this.RefreshContent();
		}
	}

	private void ChangeArmyNameIfValid()
	{
		if (this.Army == null)
		{
			return;
		}
		if (this.ValidateArmyName())
		{
			Order order = new OrderChangeEntityUserDefinedName(this.Army.Empire.Index, this.Army.GUID, this.ArmyNameLabel.Text.Trim());
			IPlayerControllerRepositoryService service = this.parent.Game.Services.GetService<IPlayerControllerRepositoryService>();
			Diagnostics.Assert(service != null);
			service.ActivePlayerController.PostOrder(order);
		}
		else
		{
			this.ArmyNameLabel.Text = this.Army.LocalizedName;
		}
	}

	private bool IsArmyNameAlreadyUsed()
	{
		string b = this.ArmyNameLabel.Text.Trim().ToLower();
		ReadOnlyCollection<Army> armies = this.departmentOfDefense.Armies;
		for (int i = 0; i < armies.Count; i++)
		{
			if (armies[i] != this.Army)
			{
				if (armies[i].Name.ToString().ToLower() == b)
				{
					return true;
				}
				if (armies[i].LocalizedName.ToLower() == b)
				{
					return true;
				}
			}
		}
		return false;
	}

	private void OnAssignHeroCB(GameObject obj)
	{
		Garrison.IsAcceptingHeroAssignmentReasonsEnum isAcceptingHeroAssignmentReasonsEnum;
		if (this.Army != null && this.Army.IsAcceptingHeroAssignments(out isAcceptingHeroAssignmentReasonsEnum))
		{
			global::IGuiService service = Services.GetService<global::IGuiService>();
			if (service != null)
			{
				service.Show(typeof(HeroSelectionModalPanel), new object[]
				{
					base.gameObject
				});
			}
		}
	}

	private void OnDoubleClickLineCB(GameObject obj)
	{
		ArmyLine.CurrentArmy = this.Army;
		this.SelectionToggle.State = true;
		if (this.parent != null)
		{
			this.parent.OnDoubleClickLine();
		}
	}

	private void OnSwitchLine(GameObject obj)
	{
		if (this.Army != ArmyLine.CurrentArmy)
		{
			ArmyLine.CurrentArmy = this.Army;
			this.SelectionToggle.State = true;
		}
		else
		{
			ArmyLine.CurrentArmy = null;
			this.SelectionToggle.State = false;
		}
		if (this.parent != null)
		{
			this.parent.OnToggleLine();
		}
	}

	private void OnChangeNameCB(GameObject obj)
	{
		this.ValidateArmyName();
	}

	private void OnNameFocusLostCB(GameObject obj)
	{
		this.ChangeArmyNameIfValid();
		this.ArmyNameLabel.TintColor = Color.white;
		this.ArmyNameTextField.AgeTransform.Enable = false;
		this.ArmyNameValidationFrame.TintColor = Color.white;
		this.ArmyNameValidationFrame.AgeTransform.Visible = false;
	}

	private void OnValidateNameCB(GameObject obj)
	{
		AgeManager.Instance.FocusedControl = null;
	}

	private void OnOrderResponse(object sender, TicketRaisedEventArgs args)
	{
		this.AgeTransform.Enable = true;
		this.parent.RefreshContent();
	}

	private bool ValidateArmyName()
	{
		bool flag = true;
		if (this.ArmyNameLabel.Text.Trim().Length == 0)
		{
			flag = false;
			this.ArmyNameTextField.AgeTransform.AgeTooltip.Content = "%ArmyNameCannotBeEmptyDescription";
		}
		else if (this.IsArmyNameAlreadyUsed())
		{
			flag = false;
			this.ArmyNameTextField.AgeTransform.AgeTooltip.Content = "%ArmyNameAlreadyExistsDescription";
		}
		if (flag)
		{
			this.ArmyNameLabel.TintColor = Color.white;
			this.ArmyNameValidationFrame.TintColor = Color.white;
			this.ArmyNameTextField.AgeTransform.AgeTooltip.Content = null;
		}
		else
		{
			this.ArmyNameLabel.TintColor = Color.red;
			this.ArmyNameValidationFrame.TintColor = Color.red;
		}
		return flag;
	}

	private void ValidateHeroChoice(Unit hero)
	{
		IGameService service = Services.GetService<IGameService>();
		IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
		this.AgeTransform.Enable = false;
		this.selectedHero = hero;
		if (this.Army.IsPrivateers)
		{
			string message = string.Format(AgeLocalizer.Instance.LocalizeString("%ConfirmHeroAssignmentToPrivateers"), new object[0]);
			MessagePanel.Instance.Show(message, string.Empty, MessagePanelButtons.YesNo, new MessagePanel.EventHandler(this.ConfirmAssignment), MessagePanelType.IMPORTANT, new MessagePanelButton[0]);
		}
		else
		{
			OrderChangeHeroAssignment order = new OrderChangeHeroAssignment(this.Army.Empire.Index, hero.GUID, this.Army.GUID);
			Ticket ticket;
			service2.ActivePlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
			this.selectedHero = null;
		}
	}

	private void ConfirmAssignment(object sender, MessagePanelResultEventArgs e)
	{
		if (e.Result == MessagePanelResult.Yes && this.selectedHero != null)
		{
			IGameService service = Services.GetService<IGameService>();
			IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
			OrderChangeHeroAssignment order = new OrderChangeHeroAssignment(this.Army.Empire.Index, this.selectedHero.GUID, this.Army.GUID);
			Ticket ticket;
			service2.ActivePlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
		}
		this.selectedHero = null;
	}

	private void StartProfanityFiltering()
	{
	}

	public static Army CurrentArmy;

	public AgePrimitiveLabel ArmyNameLabel;

	public AgeControlTextField ArmyNameTextField;

	public AgePrimitiveImage ArmyNameValidationFrame;

	public AgePrimitiveLabel ArmyMovement;

	public AgePrimitiveLabel ArmySize;

	public AgePrimitiveLabel ColossusArmyLabel;

	public AgePrimitiveLabel KaijuArmyLabel;

	public AgePrimitiveLabel ArmyLocation;

	public AgePrimitiveLabel ArmyUpkeep;

	public AgePrimitiveImage ArmyHPGauge;

	public AgePrimitiveLabel ArmyHPLabel;

	public AgePrimitiveLabel ArmyStatusLabel;

	public HeroCard HeroCard;

	public AgeControlButton DoubleClickButton;

	public AgeControlToggle SelectionToggle;

	public Color FullLifeColor;

	public Color HalfLifeColor;

	public Color CriticalLifeColor;

	public Color ColorIfInBattle;

	private GuiHero guiHero;

	private ArmyListPanel parent;

	private bool isColossusArmy;

	private bool isKaijuArmy;

	private DepartmentOfDefense departmentOfDefense;

	private string temp;

	private Unit selectedHero;

	private string profanityError;

	private UnityEngine.Coroutine profanityFilterCoroutine;

	private Color invalidColor;
}
