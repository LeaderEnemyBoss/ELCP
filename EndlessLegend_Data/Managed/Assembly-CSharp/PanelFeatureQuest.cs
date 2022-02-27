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
		int num = 0;
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
				objectiveLabel.Text += AgeLocalizer.Instance.LocalizeString(questGuiElement.Steps[num].Text);
			}
			if (!this.descriptionOnly)
			{
				this.ObjectiveBackground.TintColor = this.ProgressionColor;
				AgePrimitiveLabel objectiveLabel2 = this.ObjectiveLabel;
				objectiveLabel2.Text += quest.GetProgressionString(quest.QuestDefinition.Steps[num].Name);
				if (this.constructibleProgression && quest.QuestState != QuestState.Failed && quest.QuestState != QuestState.Completed)
				{
					if (this.departmentOfIndustries == null)
					{
						IGameService service = Services.GetService<IGameService>();
						Diagnostics.Assert(service != null && service.Game != null);
						global::Game game = service.Game as global::Game;
						this.departmentOfIndustries = new List<DepartmentOfIndustry>();
						for (int i = 0; i < game.Empires.Length; i++)
						{
							global::Empire empire = game.Empires[i];
							if (empire is MajorEmpire)
							{
								DepartmentOfIndustry agency = empire.GetAgency<DepartmentOfIndustry>();
								if (agency != null)
								{
									this.departmentOfIndustries.Add(agency);
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
						List<KeyValuePair<float, City>> list = new List<KeyValuePair<float, City>>();
						KeyValuePair<float, City> item = default(KeyValuePair<float, City>);
						for (int j = 0; j < this.departmentOfIndustries.Count; j++)
						{
							City city;
							Construction construction = this.departmentOfIndustries[j].GetConstruction(constructibleElement, out city);
							if (construction != null && city != null)
							{
								int num2;
								float key;
								float num3;
								bool flag;
								QueueGuiItem.GetConstructionTurnInfos(city, construction, QueueGuiItem.EmptyList, out num2, out key, out num3, out flag);
								KeyValuePair<float, City> keyValuePair = new KeyValuePair<float, City>(key, city);
								list.Add(keyValuePair);
								if (city.Empire.Bits == quest.EmpireBits)
								{
									item = keyValuePair;
								}
							}
						}
						list.Sort(new Comparison<KeyValuePair<float, City>>(PanelFeatureQuest.ConstructionInProgressInverseSorter));
						int num4;
						if (item.Value != null)
						{
							num4 = list.IndexOf(item);
						}
						else
						{
							num4 = list.Count;
						}
						if (this.ObjectiveLabel.Text != string.Empty)
						{
							AgePrimitiveLabel objectiveLabel3 = this.ObjectiveLabel;
							objectiveLabel3.Text += "\n";
						}
						this.ObjectiveLabel.Text = string.Concat(new object[]
						{
							this.ObjectiveLabel.Text,
							AgeLocalizer.Instance.LocalizeString("%QuestRank"),
							num4 + 1,
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

	protected override void OnUnloadGame(IGame game)
	{
		this.departmentOfIndustries = null;
		base.OnUnloadGame(game);
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
