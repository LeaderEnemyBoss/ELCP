using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using UnityEngine;

public class PanelFeatureCapacities : GuiPanelFeature
{
	public bool ResizeSelf
	{
		get
		{
			return this.resizeSelf;
		}
		set
		{
			this.resizeSelf = value;
		}
	}

	public override StaticString InternalName
	{
		get
		{
			return "Capacities";
		}
		protected set
		{
		}
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		if (this.Background != null)
		{
			this.Background.TintColor = this.NonEmbarkedBackgroundColor;
		}
		this.abilityReferences.Clear();
		if (this.context is IUnitAbilityController)
		{
			UnitAbilityReference[] abilities = (this.context as IUnitAbilityController).GetAbilities();
			if (abilities != null)
			{
				UnitAbilityReference[] array = abilities;
				for (int i = 0; i < array.Length; i++)
				{
					UnitAbilityReference ability = array[i];
					if (this.abilityReferences.Any((UnitAbilityReference match) => match.Name == ability.Name))
					{
						int index = this.abilityReferences.FindIndex((UnitAbilityReference match) => match.Name == ability.Name);
						if (ability.Level > this.abilityReferences[index].Level)
						{
							this.abilityReferences[index] = ability;
						}
					}
					else
					{
						this.abilityReferences.Add(ability);
					}
				}
			}
		}
		this.abilityReferences.RemoveAll((UnitAbilityReference match) => !this.unitAbilityDatatable.ContainsKey(match.Name) || this.unitAbilityDatatable.GetValue(match.Name).Hidden);
		if (this.abilityReferences.Count > this.smallPrefabThreshold && this.abilityReferences.Count <= this.minimalPrefabThreshold && this.ResizeSelf)
		{
			if (this.previousSize != PanelFeatureCapacities.CapacityPrefabSizes.Small)
			{
				this.CapacitiesTable.DestroyAllChildren();
				this.previousSize = PanelFeatureCapacities.CapacityPrefabSizes.Small;
			}
			this.CapacitiesTable.ReserveChildren(this.abilityReferences.Count, this.CapacitySmallPrefab, "Item");
		}
		else if (this.abilityReferences.Count > this.minimalPrefabThreshold && this.ResizeSelf)
		{
			if (this.previousSize != PanelFeatureCapacities.CapacityPrefabSizes.Minimal)
			{
				this.CapacitiesTable.DestroyAllChildren();
				this.previousSize = PanelFeatureCapacities.CapacityPrefabSizes.Minimal;
			}
			this.CapacitiesTable.ReserveChildren(this.abilityReferences.Count, this.CapacityMinimalPrefab, "Item");
		}
		else
		{
			if (this.previousSize != PanelFeatureCapacities.CapacityPrefabSizes.Normal)
			{
				this.CapacitiesTable.DestroyAllChildren();
				this.previousSize = PanelFeatureCapacities.CapacityPrefabSizes.Normal;
			}
			this.CapacitiesTable.ReserveChildren(this.abilityReferences.Count, this.CapacityPrefab, "Item");
		}
		GuiUnit guiUnit = this.context as GuiUnit;
		Unit unit = this.context as Unit;
		List<UnitAbilityReference> listToCheck = new List<UnitAbilityReference>();
		List<UnitAbilityReference> HeroAbilities = new List<UnitAbilityReference>();
		List<UnitAbilityReference> ItemAbilities = new List<UnitAbilityReference>();
		UnitDesign unitDesign = null;
		if (unit != null)
		{
			unitDesign = unit.UnitDesign;
		}
		if (guiUnit != null)
		{
			unitDesign = guiUnit.UnitDesign;
		}
		if (unitDesign != null)
		{
			if (unitDesign.UnitBodyDefinition.UnitAbilities != null && unitDesign.UnitBodyDefinition.UnitAbilities.Length != 0)
			{
				listToCheck = unitDesign.UnitBodyDefinition.UnitAbilities.ToList<UnitAbilityReference>();
			}
			UnitProfile unitProfile = unitDesign as UnitProfile;
			if (unitProfile != null && unitProfile.ProfileAbilityReferences != null && unitProfile.ProfileAbilityReferences.Length != 0)
			{
				HeroAbilities = unitProfile.ProfileAbilityReferences.ToList<UnitAbilityReference>();
			}
			if (unitDesign.UnitEquipmentSet != null)
			{
				List<StaticString> list = new List<StaticString>(unitDesign.UnitEquipmentSet.Slots.Length);
				IDatabase<ItemDefinition> database = Databases.GetDatabase<ItemDefinition>(false);
				Diagnostics.Assert(database != null);
				for (int j = 0; j < unitDesign.UnitEquipmentSet.Slots.Length; j++)
				{
					UnitEquipmentSet.Slot slot = unitDesign.UnitEquipmentSet.Slots[j];
					if (!list.Contains(slot.ItemName))
					{
						StaticString key = slot.ItemName.ToString().Split(DepartmentOfDefense.ItemSeparators)[0];
						ItemDefinition itemDefinition;
						if (database.TryGetValue(key, out itemDefinition))
						{
							Diagnostics.Assert(itemDefinition != null);
							if (itemDefinition.AbilityReferences != null)
							{
								ItemAbilities.AddRange(itemDefinition.AbilityReferences);
								list.Add(slot.ItemName);
							}
						}
					}
				}
			}
		}
		this.abilityReferences.Sort(delegate(UnitAbilityReference left, UnitAbilityReference right)
		{
			bool flag = !this.ContainsAbilityReference(HeroAbilities, left) && !this.ContainsAbilityReference(ItemAbilities, left);
			bool flag2 = this.ContainsAbilityReference(HeroAbilities, left);
			bool flag3 = this.ContainsAbilityReference(ItemAbilities, left);
			bool flag4 = !this.ContainsAbilityReference(HeroAbilities, right) && !this.ContainsAbilityReference(ItemAbilities, right);
			bool flag5 = this.ContainsAbilityReference(HeroAbilities, right);
			bool flag6 = this.ContainsAbilityReference(ItemAbilities, right);
			string x = left.Name + Mathf.Max((float)left.Level, 0f);
			string text = "";
			GuiElement guiElement;
			if (this.GuiService.GuiPanelHelper.TryGetGuiElement(x, out guiElement))
			{
				text = AgeLocalizer.Instance.LocalizeString(guiElement.Title);
			}
			x = right.Name + Mathf.Max((float)right.Level, 0f);
			string strB = "";
			if (this.GuiService.GuiPanelHelper.TryGetGuiElement(x, out guiElement))
			{
				strB = AgeLocalizer.Instance.LocalizeString(guiElement.Title);
			}
			if (flag && flag5)
			{
				return -1;
			}
			if (flag && flag6)
			{
				return -1;
			}
			if (flag2 && flag4)
			{
				return 1;
			}
			if (flag3 && flag4)
			{
				return 1;
			}
			if (flag2 && flag6)
			{
				return -1;
			}
			if (flag3 && flag2)
			{
				return 1;
			}
			if ((flag && flag4) || (flag2 && flag5) || (flag3 && flag6))
			{
				return text.CompareTo(strB);
			}
			return 0;
		});
		this.CapacitiesTable.RefreshChildrenIList<UnitAbilityReference>(this.abilityReferences, this.refreshAbilityReferenceDelegate, true, false);
		this.CapacitiesTable.ArrangeChildren();
		int num = this.CapacitiesTable.ComputeVisibleChildren();
		if (num > 0)
		{
			this.CapacitiesTable.Visible = true;
			AgeTransform ageTransform = this.CapacitiesTable.GetChildren()[num - 1];
			this.CapacitiesTable.Height = ageTransform.Y + ageTransform.Height + this.CapacitiesTable.VerticalMargin;
			this.Title.Text = AgeLocalizer.Instance.LocalizeString("%FeatureCapacitiesTitle");
			if (this.ResizeSelf)
			{
				base.AgeTransform.Height = this.CapacitiesTable.PixelMarginTop + this.CapacitiesTable.Height + this.CapacitiesTable.PixelMarginBottom;
			}
			if (guiUnit != null && (guiUnit.Unit != null || guiUnit.UnitSnapshot != null) && this.Background != null && ((guiUnit.Unit != null && guiUnit.Unit.Embarked) || (guiUnit.UnitSnapshot != null && guiUnit.UnitSnapshot.Embarked)))
			{
				this.Title.Text = "%FeatureCapacitiesEmbarkedTitle";
				this.Background.TintColor = this.EmbarkedBackgroundColor;
			}
			Color tintColor = PanelFeatureCapacities.Colorlist[Amplitude.Unity.Framework.Application.Registry.GetValue<int>(new StaticString("Settings/ELCP/UI/CapacityColor1"), 0)];
			Color tintColor2 = PanelFeatureCapacities.Colorlist[Amplitude.Unity.Framework.Application.Registry.GetValue<int>(new StaticString("Settings/ELCP/UI/CapacityColor2"), 2)];
			Color tintColor3 = PanelFeatureCapacities.Colorlist[Amplitude.Unity.Framework.Application.Registry.GetValue<int>(new StaticString("Settings/ELCP/UI/CapacityColor3"), 8)];
			for (int k = 0; k < this.abilityReferences.Count; k++)
			{
				FeatureItemCapacity component = this.CapacitiesTable.GetChildren()[k].GetComponent<FeatureItemCapacity>();
				if (component != null && !this.ContainsAbilityReference(listToCheck, this.abilityReferences[k]))
				{
					component.Icon.TintColor = tintColor;
					if (this.ContainsAbilityReference(HeroAbilities, this.abilityReferences[k]))
					{
						component.Icon.TintColor = tintColor2;
					}
					else if (this.ContainsAbilityReference(ItemAbilities, this.abilityReferences[k]))
					{
						component.Icon.TintColor = tintColor3;
					}
					else
					{
						component.Icon.TintColor = tintColor;
					}
				}
				else if (component != null && this.ContainsAbilityReference(listToCheck, this.abilityReferences[k]))
				{
					component.Icon.TintColor = tintColor;
				}
			}
		}
		else
		{
			this.CapacitiesTable.Visible = false;
			this.Title.Text = AgeLocalizer.Instance.LocalizeString("%FeatureNoCapacitiesTitle");
			if (this.ResizeSelf)
			{
				base.AgeTransform.Height = this.CapacitiesTable.PixelMarginTop;
			}
		}
		yield return base.OnShow(parameters);
		yield break;
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.CapacitiesTable.DestroyAllChildren();
		base.OnUnloadGame(game);
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		this.unitAbilityDatatable = Databases.GetDatabase<UnitAbility>(false);
		Diagnostics.Assert(this.unitAbilityDatatable != null);
		this.refreshAbilityReferenceDelegate = new AgeTransform.RefreshTableItem<UnitAbilityReference>(this.RefreshAbilityReference);
		yield break;
	}

	protected override void OnUnload()
	{
		this.refreshAbilityReferenceDelegate = null;
		base.OnUnload();
	}

	private void RefreshAbilityReference(AgeTransform tableItem, UnitAbilityReference abilityReference, int index)
	{
		tableItem.GetComponent<FeatureItemCapacity>().SetContent(abilityReference, base.GuiService.GuiPanelHelper);
	}

	private bool ContainsAbilityReference(List<UnitAbilityReference> ListToCheck, UnitAbilityReference AbilityRef)
	{
		foreach (UnitAbilityReference unitAbilityReference in ListToCheck)
		{
			if (unitAbilityReference.Name == AbilityRef.Name && unitAbilityReference.Level == AbilityRef.Level)
			{
				return true;
			}
			if (unitAbilityReference.Name == AbilityRef.Name && unitAbilityReference.Level == 0 && AbilityRef.Level == -1)
			{
				return true;
			}
		}
		return false;
	}

	public AgePrimitiveLabel Title;

	public AgePrimitiveImage Background;

	public AgeTransform CapacitiesTable;

	public Transform CapacityPrefab;

	public Transform CapacitySmallPrefab;

	public Transform CapacityMinimalPrefab;

	public Color EmbarkedBackgroundColor;

	public Color NonEmbarkedBackgroundColor;

	private readonly int smallPrefabThreshold = 10;

	private readonly int minimalPrefabThreshold = 13;

	private List<UnitAbilityReference> abilityReferences = new List<UnitAbilityReference>();

	private IDatabase<UnitAbility> unitAbilityDatatable;

	private AgeTransform.RefreshTableItem<UnitAbilityReference> refreshAbilityReferenceDelegate;

	private bool resizeSelf = true;

	private PanelFeatureCapacities.CapacityPrefabSizes previousSize;

	private static Color[] Colorlist = new Color[]
	{
		Color.white,
		new Color(0.827451f, 0.827451f, 0.827451f),
		new Color(0f, 0.545098066f, 0.545098066f),
		new Color(0.117647059f, 0.5647059f, 1f),
		new Color(0.235294119f, 0.7019608f, 0.443137258f),
		new Color(0f, 1f, 0.498039216f),
		new Color(0.6039216f, 0.8039216f, 0.196078435f),
		new Color(1f, 0.843137264f, 0f),
		new Color(1f, 0.647058845f, 0f),
		new Color(0.8039216f, 0.360784322f, 0.360784322f),
		new Color(0.8666667f, 0.627451f, 0.8666667f),
		new Color(0.5764706f, 0.4392157f, 0.858823538f),
		new Color(0.41568628f, 0.3529412f, 0.8039216f)
	};

	private enum CapacityPrefabSizes
	{
		None,
		Normal,
		Small,
		Minimal
	}
}
