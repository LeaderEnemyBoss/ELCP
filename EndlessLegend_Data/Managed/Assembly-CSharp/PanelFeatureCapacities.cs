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
			IUnitAbilityController unitAbilityController = this.context as IUnitAbilityController;
			UnitAbilityReference[] abilities = unitAbilityController.GetAbilities();
			if (abilities != null)
			{
				foreach (UnitAbilityReference ability in abilities)
				{
					bool alreadyExist = this.abilityReferences.Any((UnitAbilityReference match) => match.Name == ability.Name);
					if (alreadyExist)
					{
						int alreadyExistingAbilityIndex = this.abilityReferences.FindIndex((UnitAbilityReference match) => match.Name == ability.Name);
						if (ability.Level > this.abilityReferences[alreadyExistingAbilityIndex].Level)
						{
							this.abilityReferences[alreadyExistingAbilityIndex] = ability;
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
		this.CapacitiesTable.RefreshChildrenIList<UnitAbilityReference>(this.abilityReferences, this.refreshAbilityReferenceDelegate, true, false);
		this.CapacitiesTable.ArrangeChildren();
		int visibleLines = this.CapacitiesTable.ComputeVisibleChildren();
		if (visibleLines > 0)
		{
			this.CapacitiesTable.Visible = true;
			AgeTransform lastChild = this.CapacitiesTable.GetChildren()[visibleLines - 1];
			this.CapacitiesTable.Height = lastChild.Y + lastChild.Height + this.CapacitiesTable.VerticalMargin;
			this.Title.Text = AgeLocalizer.Instance.LocalizeString("%FeatureCapacitiesTitle");
			if (this.ResizeSelf)
			{
				base.AgeTransform.Height = this.CapacitiesTable.PixelMarginTop + this.CapacitiesTable.Height + this.CapacitiesTable.PixelMarginBottom;
			}
			GuiUnit guiUnit = this.context as GuiUnit;
			if (guiUnit != null && (guiUnit.Unit != null || guiUnit.UnitSnapshot != null) && this.Background != null && ((guiUnit.Unit != null && guiUnit.Unit.Embarked) || (guiUnit.UnitSnapshot != null && guiUnit.UnitSnapshot.Embarked)))
			{
				this.Title.Text = "%FeatureCapacitiesEmbarkedTitle";
				this.Background.TintColor = this.EmbarkedBackgroundColor;
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
		FeatureItemCapacity component = tableItem.GetComponent<FeatureItemCapacity>();
		component.SetContent(abilityReference, base.GuiService.GuiPanelHelper);
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

	private enum CapacityPrefabSizes
	{
		None,
		Normal,
		Small,
		Minimal
	}
}
