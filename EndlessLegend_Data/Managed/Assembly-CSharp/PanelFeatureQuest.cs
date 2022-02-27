using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using UnityEngine;

public class PanelFeatureQuest : GuiPanelFeature
{
	public override StaticString InternalName
	{
		get
		{
			return "Quest";
		}
		protected set
		{
		}
	}

	public float DefaultWidth { get; set; }

	protected override void Awake()
	{
		base.Awake();
		this.DefaultWidth = base.AgeTransform.Width;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		Quest quest = this.context as Quest;
		if (quest == null)
		{
			Diagnostics.LogError("Context must be a quest");
			yield break;
		}
		int stepIndex = 0;
		GuiElement guiElement;
		if (base.GuiService.GuiPanelHelper.TryGetGuiElement(quest.Name, out guiElement))
		{
			QuestGuiElement questGuiElement = guiElement as QuestGuiElement;
			this.ObjectiveLabel.AgeTransform.Height = 0f;
			this.ObjectiveLabel.Text = string.Empty;
			if (!this.progressionOnly)
			{
				this.ObjectiveBackground.TintColor = this.DescriptionColor;
				AgePrimitiveLabel objectiveLabel = this.ObjectiveLabel;
				objectiveLabel.Text += AgeLocalizer.Instance.LocalizeString(questGuiElement.Steps[stepIndex].Text);
			}
			if (!this.descriptionOnly)
			{
				this.ObjectiveBackground.TintColor = this.ProgressionColor;
				AgePrimitiveLabel objectiveLabel2 = this.ObjectiveLabel;
				objectiveLabel2.Text += quest.GetProgressionString(quest.QuestDefinition.Steps[stepIndex].Name);
				if (this.constructibleProgression && quest.QuestState != QuestState.Failed && quest.QuestState != QuestState.Completed)
				{
					if (this.departmentOfIndustries == null)
					{
						IGameService gameService = Services.GetService<IGameService>();
						Diagnostics.Assert(gameService != null && gameService.Game != null);
						global::Game game = gameService.Game as global::Game;
						this.departmentOfIndustries = new List<DepartmentOfIndustry>();
						for (int index = 0; index < game.Empires.Length; index++)
						{
							global::Empire empire = game.Empires[index];
							if (empire is MajorEmpire)
							{
								DepartmentOfIndustry departmentOfIndustry = empire.GetAgency<DepartmentOfIndustry>();
								if (departmentOfIndustry != null)
								{
									this.departmentOfIndustries.Add(departmentOfIndustry);
								}
							}
						}
					}
					DepartmentOfIndustry.ConstructibleElement constructibleElement = null;
					if (!this.departmentOfIndustries[0].ConstructibleElementDatabase.TryGetValue(quest.Name.ToString().Replace("Quest", string.Empty), out constructibleElement))
					{
						Diagnostics.LogError("Cannot find ConstructibleElement {0}", new object[]
						{
							quest.Name.ToString().Replace("Quest", string.Empty)
						});
					}
					else
					{
						List<KeyValuePair<float, City>> constructionsInfo = new List<KeyValuePair<float, City>>();
						KeyValuePair<float, City> ownConstruction = default(KeyValuePair<float, City>);
						for (int index2 = 0; index2 < this.departmentOfIndustries.Count; index2++)
						{
							City city;
							Construction construction = this.departmentOfIndustries[index2].GetConstruction(constructibleElement, out city);
							if (construction != null && city != null)
							{
								int worstNumberOfTurn;
								float worstStock;
								float worstCost;
								bool checkPrerequisites;
								QueueGuiItem.GetConstructionTurnInfos(city, construction, QueueGuiItem.EmptyList, out worstNumberOfTurn, out worstStock, out worstCost, out checkPrerequisites);
								KeyValuePair<float, City> constructionInfo = new KeyValuePair<float, City>(worstStock, city);
								constructionsInfo.Add(constructionInfo);
								if (city.Empire.Bits == quest.EmpireBits)
								{
									ownConstruction = constructionInfo;
								}
							}
						}
						constructionsInfo.Sort(new Comparison<KeyValuePair<float, City>>(PanelFeatureQuest.ConstructionInProgressInverseSorter));
						int ownIndex;
						if (ownConstruction.Value != null)
						{
							ownIndex = constructionsInfo.IndexOf(ownConstruction);
						}
						else
						{
							ownIndex = constructionsInfo.Count;
						}
						if (this.ObjectiveLabel.Text != string.Empty)
						{
							this.ObjectiveLabel.Text + "\n";
						}
						this.ObjectiveLabel.Text = string.Concat(new object[]
						{
							this.ObjectiveLabel.Text,
							AgeLocalizer.Instance.LocalizeString("%QuestRank"),
							ownIndex + 1,
							"/",
							this.departmentOfIndustries.Count
						});
					}
				}
			}
			if (!string.IsNullOrEmpty(this.ObjectiveLabel.Text))
			{
				this.ObjectiveLabel.AgeTransform.Width = this.DefaultWidth - this.ObjectiveLabel.AgeTransform.PixelMarginLeft - this.ObjectiveLabel.AgeTransform.PixelMarginRight;
				this.ObjectiveLabel.ComputeText();
				this.ObjectiveGroup.Height = this.ObjectiveLabel.Font.LineHeight * (float)this.ObjectiveLabel.TextLines.Count + this.ObjectiveLabel.AgeTransform.PixelMarginTop + this.ObjectiveLabel.AgeTransform.PixelMarginBottom;
				this.ObjectiveBox.Height = this.ObjectiveGroup.Height + this.ObjectiveGroup.PixelMarginTop + this.ObjectiveGroup.PixelMarginBottom;
			}
			base.AgeTransform.Height = this.ObjectiveBox.Height;
		}
		yield return base.OnShow(parameters);
		yield break;
	}

	protected override void DeserializeFeatureDescription(XmlElement featureDescription)
	{
		base.DeserializeFeatureDescription(featureDescription);
		if (featureDescription.Name == "ProgressionOnly")
		{
			this.progressionOnly = bool.Parse(featureDescription.GetAttribute("Value"));
		}
		else if (featureDescription.Name == "DescriptionOnly")
		{
			this.descriptionOnly = bool.Parse(featureDescription.GetAttribute("Value"));
		}
		else if (featureDescription.Name == "ConstructibleProgression")
		{
			this.constructibleProgression = bool.Parse(featureDescription.GetAttribute("Value"));
		}
	}

	private static int ConstructionInProgressInverseSorter(KeyValuePair<float, City> right, KeyValuePair<float, City> left)
	{
		int num = left.Key.CompareTo(right.Key);
		if (num == 0)
		{
			num = left.Value.GetPropertyValue(SimulationProperties.NetCityProduction).CompareTo(right.Value.GetPropertyValue(SimulationProperties.NetCityProduction));
		}
		return num;
	}

	private void OnApplyHighDefinition(float scale)
	{
		this.DefaultWidth = Mathf.Round(this.DefaultWidth * scale);
	}

	public AgeTransform ObjectiveBox;

	public AgeTransform ObjectiveGroup;

	public AgeTransform ObjectiveTitle;

	public AgePrimitiveLabel ObjectiveLabel;

	public AgePrimitiveImage ObjectiveBackground;

	public Color DescriptionColor;

	public Color ProgressionColor;

	private bool progressionOnly;

	private bool descriptionOnly;

	private bool constructibleProgression;

	private List<DepartmentOfIndustry> departmentOfIndustries;
}
