using System;
using Amplitude;
using UnityEngine;

public class CompetitorVictoryItem : MonoBehaviour
{
	public IComparable Comparable { get; set; }

	public void SetContent(MajorEmpire empire, VictoryCondition victoryCondition, PlayerController playerController, int victoryRank)
	{
		this.Comparable = 0f;
		bool flag = true;
		if (empire != playerController.Empire && !playerController.Empire.SimulationObject.Tags.Contains(Empire.TagEmpireEliminated))
		{
			flag = false;
			Diagnostics.Assert(playerController.Empire != null);
			EmpireInfo.Accessibility lastAccessibilityLevel = EmpireInfo.LastAccessibilityLevel;
			if (lastAccessibilityLevel != EmpireInfo.Accessibility.Default)
			{
				if (lastAccessibilityLevel == EmpireInfo.Accessibility.Partial)
				{
					DepartmentOfIntelligence agency = playerController.Empire.GetAgency<DepartmentOfIntelligence>();
					if (agency != null && agency.IsEmpireInfiltrated(empire))
					{
						flag = true;
					}
				}
			}
			else
			{
				DepartmentOfForeignAffairs agency2 = playerController.Empire.GetAgency<DepartmentOfForeignAffairs>();
				if (agency2 != null)
				{
					DiplomaticRelation diplomaticRelation = agency2.GetDiplomaticRelation(empire);
					flag |= (diplomaticRelation != null && diplomaticRelation.State != null && diplomaticRelation.State.Name != DiplomaticRelationState.Names.Unknown);
				}
				if (!flag)
				{
					DepartmentOfIntelligence agency3 = playerController.Empire.GetAgency<DepartmentOfIntelligence>();
					if (agency3 != null && agency3.IsEmpireInfiltrated(empire))
					{
						flag = true;
					}
				}
			}
		}
		if (!empire.VictoryConditionStatuses.ContainsKey(victoryCondition.Name) || !flag)
		{
			base.GetComponent<AgeTransform>().Visible = false;
			return;
		}
		base.GetComponent<AgeTransform>().Visible = true;
		this.format = AgeLocalizer.Instance.LocalizeString(victoryCondition.Progression.Format);
		for (int i = 0; i < victoryCondition.Progression.Vars.Length; i++)
		{
			if (this.format.Contains(victoryCondition.Progression.Vars[i].Name))
			{
				this.varName = "$" + victoryCondition.Progression.Vars[i].Name;
				this.format = this.format.Replace(this.varName, GuiFormater.FormatGui(empire.VictoryConditionStatuses[victoryCondition.Name].Variables[i], victoryCondition.Progression.Vars[i].Name == "Ratio", false, false, 0));
			}
			if (victoryCondition.Progression.Vars[i].Name == victoryCondition.Progression.SortVariable)
			{
				this.Comparable = empire.VictoryConditionStatuses[victoryCondition.Name].Variables[i];
			}
		}
		this.Title.Text = empire.LocalizedName;
		this.Value.Text = this.format;
		this.Value.AgeTransform.Width = this.Value.Font.ComputeTextWidth(this.Value.Text, false, false);
		this.Title.AgeTransform.PixelMarginRight = this.Value.AgeTransform.PixelMarginRight + this.Value.AgeTransform.PixelMarginLeft + this.Value.AgeTransform.Width;
		this.Title.TintColor = empire.Color;
		this.Value.TintColor = empire.Color;
		if (empire == playerController.Empire)
		{
			this.TitleModifier.StartDelay = 0.3f * (float)victoryRank;
			this.TitleModifier.StartAnimation();
			return;
		}
		this.TitleModifier.Reset();
		this.Title.AgeTransform.Alpha = 1f;
	}

	public const float BaseDelay = 0.3f;

	public AgePrimitiveLabel Title;

	public AgeModifierAlpha TitleModifier;

	public AgePrimitiveLabel Value;

	private string format = string.Empty;

	private string varName = string.Empty;
}
