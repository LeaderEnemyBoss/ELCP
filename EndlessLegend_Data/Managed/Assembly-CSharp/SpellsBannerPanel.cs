using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.View;
using UnityEngine;

public class SpellsBannerPanel : GuiPlayerControllerPanel
{
	private DepartmentOfScience DepartmentOfScience
	{
		get
		{
			return this.departmentOfScience;
		}
		set
		{
			if (this.departmentOfScience != null)
			{
				this.departmentOfScience.TechnologyUnlocked -= this.DepartmentOfScience_TechnologyUnlocked;
			}
			this.departmentOfScience = value;
			if (this.departmentOfScience != null)
			{
				this.departmentOfScience.TechnologyUnlocked += this.DepartmentOfScience_TechnologyUnlocked;
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

	private Encounter Encounter
	{
		get
		{
			return this.encounter;
		}
		set
		{
			if (this.encounter != null)
			{
				this.encounter.RoundUpdate -= this.Encounter_RoundUpdate;
				this.encounter.BattleActionStateChange -= this.Encounter_BattleActionStateChange;
			}
			this.encounter = value;
			if (this.encounter != null)
			{
				this.encounter.RoundUpdate += this.Encounter_RoundUpdate;
				this.encounter.BattleActionStateChange += this.Encounter_BattleActionStateChange;
			}
		}
	}

	public override void Bind(Empire empire)
	{
		base.Bind(empire);
		this.DepartmentOfScience = base.Empire.GetAgency<DepartmentOfScience>();
		this.DepartmentOfTheTreasury = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		if (base.IsVisible)
		{
			this.RefreshContent();
		}
		this.thereAlreadyIsASpellCasted = false;
	}

	public override void Unbind()
	{
		this.DepartmentOfScience = null;
		this.DepartmentOfTheTreasury = null;
		base.Unbind();
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		this.BuildAvailableSpellsDefinitions();
		this.CheckIfThereAlreadyIsASpellCasted();
		this.isInTargetingPhase = (this.Encounter.EncounterState == EncounterState.BattleIsInProgress && this.Encounter.IsInTargetingPhase());
		this.UnbindSpellButtons();
		this.SpellButtonsTable.ReserveChildren(this.availableSpellDefinitions.Count, this.SpellButtonPrefab, "Item");
		this.SpellButtonsTable.RefreshChildrenIList<SpellDefinition>(this.availableSpellDefinitions, this.setupSpellButtonDelegate, true, false);
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		if (parameters.Length > 0)
		{
			this.Encounter = (parameters[0] as Encounter);
		}
		if (this.Encounter == null)
		{
			this.Hide(true);
			yield break;
		}
		this.RefreshContent();
		yield break;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		this.Encounter = null;
		yield return base.OnHide(instant);
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		this.availableSpellDefinitions = new List<SpellDefinition>();
		this.setupSpellButtonDelegate = new AgeTransform.RefreshTableItem<SpellDefinition>(this.SetupSpellButton);
		yield break;
	}

	protected override void OnUnload()
	{
		this.availableSpellDefinitions.Clear();
		this.availableSpellDefinitions = null;
		base.OnUnload();
	}

	private void DepartmentOfScience_TechnologyUnlocked(object sender, ConstructibleElementEventArgs e)
	{
		if (base.IsVisible)
		{
			this.RefreshContent();
		}
	}

	private void DepartmentOfTheTreasury_ResourcePropertyChange(object sender, ResourcePropertyChangeEventArgs e)
	{
		if (base.IsVisible && e.ResourcePropertyName == SimulationProperties.BankAccount)
		{
			this.RefreshContent();
		}
	}

	private void Encounter_RoundUpdate(object sender, RoundUpdateEventArgs e)
	{
		this.RefreshContent();
	}

	private void Encounter_BattleActionStateChange(object sender, BattleActionStateChangeEventArgs e)
	{
		if (e.Contender.Empire == base.Empire && this.isInTargetingPhase)
		{
			this.RefreshContent();
		}
	}

	private void BuildAvailableSpellsDefinitions()
	{
		this.availableSpellDefinitions.Clear();
		IDatabase<SpellDefinition> database = Databases.GetDatabase<SpellDefinition>(false);
		if (database != null)
		{
			this.availableSpellDefinitions.AddRange(from spellDefinition in database.GetValues()
			where DepartmentOfTheTreasury.CheckConstructiblePrerequisites(base.Empire, spellDefinition, new string[]
			{
				ConstructionFlags.Prerequisite
			})
			select spellDefinition);
		}
	}

	private void SetupSpellButton(AgeTransform tableItem, SpellDefinition spellDefinition, int index)
	{
		SpellButton component = tableItem.GetComponent<SpellButton>();
		component.SetContent(spellDefinition, base.Empire, base.gameObject, this.isInTargetingPhase, this.thereAlreadyIsASpellCasted, this.encounter);
	}

	private void OnCastSpell(SpellDefinition spellDefinition)
	{
		ICursorService service = Services.GetService<ICursorService>();
		if (service != null)
		{
			EncounterWorldCursor encounterWorldCursor = service.CurrentCursor as EncounterWorldCursor;
			if (encounterWorldCursor != null)
			{
				service.ChangeCursor(typeof(EncounterSpellCursor), new object[]
				{
					encounterWorldCursor.CursorTarget,
					encounterWorldCursor.WorldEncounter,
					spellDefinition
				});
			}
		}
		Diagnostics.Log("OnCastSpell: {0}", new object[]
		{
			spellDefinition.Name
		});
	}

	private void UnbindSpellButtons()
	{
		List<SpellButton> children = this.SpellButtonsTable.GetChildren<SpellButton>(true);
		for (int i = 0; i < children.Count; i++)
		{
			children[i].UnsetContent();
		}
	}

	private void CheckIfThereAlreadyIsASpellCasted()
	{
		this.thereAlreadyIsASpellCasted = false;
		Contender firstAlliedContenderFromEmpire = this.Encounter.GetFirstAlliedContenderFromEmpire(base.Empire);
		int num = 0;
		while (num < this.availableSpellDefinitions.Count && !this.thereAlreadyIsASpellCasted)
		{
			SpellDefinition spellDefinition = this.availableSpellDefinitions[num];
			int num2 = 0;
			while (num2 < spellDefinition.SpellBattleActions.Length && !this.thereAlreadyIsASpellCasted)
			{
				StaticString name = spellDefinition.SpellBattleActions[num2].BattleActionUserDefinitionReference.Name;
				BattleAction.State state;
				if (firstAlliedContenderFromEmpire.TryGetBattleActionUserState(name, out state))
				{
					this.thereAlreadyIsASpellCasted = (state != BattleAction.State.Available);
				}
				num2++;
			}
			num++;
		}
	}

	public Transform SpellButtonPrefab;

	public AgeTransform SpellButtonsTable;

	private List<SpellDefinition> availableSpellDefinitions;

	private DepartmentOfScience departmentOfScience;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private Encounter encounter;

	private AgeTransform.RefreshTableItem<SpellDefinition> setupSpellButtonDelegate;

	private bool isInTargetingPhase;

	private bool thereAlreadyIsASpellCasted;
}
