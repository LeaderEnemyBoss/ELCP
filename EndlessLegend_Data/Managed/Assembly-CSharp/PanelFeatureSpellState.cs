using System;
using System.Collections;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Gui;

public class PanelFeatureSpellState : GuiPanelFeature
{
	public override StaticString InternalName
	{
		get
		{
			return "SpellState";
		}
		protected set
		{
		}
	}

	protected override IEnumerator OnInitialize()
	{
		yield return base.OnInitialize();
		this.spellDefinitionDatabase = Databases.GetDatabase<SpellDefinition>(false);
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		SpellDefinitionTooltipData spellDefinition = null;
		if (this.context is SpellDefinitionTooltipData)
		{
			spellDefinition = (this.context as SpellDefinitionTooltipData);
		}
		this.Title.Text = string.Empty;
		if (spellDefinition != null)
		{
			if (!this.IsInTargeting(spellDefinition))
			{
				this.Title.Text = AgeLocalizer.Instance.LocalizeString("%SpellWaitForTargetingPhase");
			}
			else
			{
				bool isThisSpellActive = false;
				bool isAnySpellActive = this.IsSpellAlreadyCasted(spellDefinition, ref isThisSpellActive);
				if (isAnySpellActive)
				{
					if (isThisSpellActive)
					{
						this.Title.Text = AgeLocalizer.Instance.LocalizeString("%SpellActive");
					}
					else
					{
						this.Title.Text = AgeLocalizer.Instance.LocalizeString("%SpellWaitForEndOfCurrentSpell");
					}
				}
				else
				{
					this.Title.Text = AgeLocalizer.Instance.LocalizeString("%SpellClickToCast");
				}
			}
		}
		yield return base.OnShow(parameters);
		yield break;
	}

	private bool IsInTargeting(SpellDefinitionTooltipData spellDefinitionTooltipData)
	{
		return spellDefinitionTooltipData.Encounter.EncounterState == EncounterState.BattleIsInProgress && spellDefinitionTooltipData.Encounter.IsInTargetingPhase();
	}

	private bool IsSpellAlreadyCasted(SpellDefinitionTooltipData spellDefinitionTooltipData, ref bool isThisSpellActive)
	{
		Contender firstAlliedContenderFromEmpire = spellDefinitionTooltipData.Encounter.GetFirstAlliedContenderFromEmpire(spellDefinitionTooltipData.Empire);
		foreach (SpellDefinition spellDefinition in this.spellDefinitionDatabase)
		{
			if (DepartmentOfTheTreasury.CheckConstructiblePrerequisites(spellDefinitionTooltipData.Empire, spellDefinition, new string[]
			{
				ConstructionFlags.Prerequisite
			}))
			{
				for (int i = 0; i < spellDefinition.SpellBattleActions.Length; i++)
				{
					StaticString name = spellDefinition.SpellBattleActions[i].BattleActionUserDefinitionReference.Name;
					BattleAction.State state;
					if (firstAlliedContenderFromEmpire.TryGetBattleActionUserState(name, out state) && state != BattleAction.State.Available)
					{
						isThisSpellActive = (spellDefinition.Name == spellDefinitionTooltipData.SpellDefinition.Name);
						return true;
					}
				}
			}
		}
		return false;
	}

	public AgePrimitiveLabel Title;

	private IDatabase<SpellDefinition> spellDefinitionDatabase;
}
