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
		if (empire != playerController.Empire)
		{
			flag = false;
			Diagnostics.Assert(playerController.Empire != null);
			switch (EmpireInfo.LastAccessibilityLevel)
			{
			case EmpireInfo.Accessibility.Default:
			{
				DepartmentOfForeignAffairs agency = playerController.Empire.GetAgency<DepartmentOfForeignAffairs>();
				if (agency != null)
				{
					DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(empire);
					flag |= (diplomaticRelation != null && diplomaticRelation.State != null && diplomaticRelation.State.Name != DiplomaticRelationState.Names.Unknown);
				}
				if (!flag)
				{
					DepartmentOfIntelligence agency2 = playerController.Empire.GetAgency<DepartmentOfIntelligence>();
					if (agency2 != null && agency2.IsEmpireInfiltrated(empire))
					{
						flag = true;
					}
				}
				break;
			}
			case EmpireInfo.Accessibility.Partial:
			{
				DepartmentOfIntelligence agency3 = playerController.Empire.GetAgency<DepartmentOfIntelligence>();
				if (agency3 != null && agency3.IsEmpireInfiltrated(empire))
				{
					flag = true;
				}
				break;
			}
			}
		}
		if (empire.VictoryConditionStatuses.ContainsKey(victoryCondition.Name) && flag)
		{
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
			}
			else
			{
				this.TitleModifier.Reset();
				this.Title.AgeTransform.Alpha = 1f;
			}
		}
		else
		{
			base.GetComponent<AgeTransform>().Visible = false;
		}
	}

	public const float BaseDelay = 0.3f;

	public AgePrimitiveLabel Title;

	public AgeModifierAlpha TitleModifier;

	public AgePrimitiveLabel Value;

	private string format = string.Empty;

	private string varName = string.Empty;
}
