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
		SpellDefinitionTooltipData spellDefinitionTooltipData = null;
		if (this.context is SpellDefinitionTooltipData)
		{
			spellDefinitionTooltipData = (this.context as SpellDefinitionTooltipData);
		}
		this.Title.Text = string.Empty;
		if (spellDefinitionTooltipData != null)
		{
			if (!this.IsInTargeting(spellDefinitionTooltipData))
			{
				this.Title.Text = AgeLocalizer.Instance.LocalizeString("%SpellWaitForTargetingPhase");
			}
			else if (ELCPUtilities.SpellUsage_HasSpellBeenUsed(spellDefinitionTooltipData.Encounter.GUID, spellDefinitionTooltipData.Empire.Index, spellDefinitionTooltipData.SpellDefinition.Name))
			{
				this.Title.Text = AgeLocalizer.Instance.LocalizeString("%SpellActive");
			}
			else
			{
				bool flag = false;
				if (this.IsSpellAlreadyCasted(spellDefinitionTooltipData, ref flag))
				{
					if (flag)
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
		Contender firstAlliedContenderFromEmpireWithUnits = spellDefinitionTooltipData.Encounter.GetFirstAlliedContenderFromEmpireWithUnits(spellDefinitionTooltipData.Empire);
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
					if (firstAlliedContenderFromEmpireWithUnits.TryGetBattleActionUserState(name, out state) && state != BattleAction.State.Available)
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
