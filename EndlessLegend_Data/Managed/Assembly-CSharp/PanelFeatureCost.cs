using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Xml;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Simulation;
using UnityEngine;

public class PanelFeatureCost : GuiPanelFeature
{
	public override StaticString InternalName
	{
		get
		{
			return "Cost";
		}
		protected set
		{
		}
	}

	public static void ComputeCostAndTurn(Amplitude.Unity.Gui.IGuiService guiService, ReadOnlyCollection<Construction> constructibles, DepartmentOfTheTreasury departmentOfTheTreasury, SimulationObjectWrapper context, out string costString, out int turn)
	{
		PanelFeatureCost.costByResource.Clear();
		for (int i = 0; i < constructibles.Count; i++)
		{
			ConstructibleElement constructibleElement = constructibles[i].ConstructibleElement;
			if (constructibleElement.Costs != null)
			{
				if (constructibleElement is TechnologyDefinition && context.SimulationObject.Tags.Contains(FactionTrait.FactionTraitReplicants1) && (constructibleElement as TechnologyDefinition).TechnologyFlags != DepartmentOfScience.ConstructibleElement.TechnologyFlag.OrbUnlock && (constructibleElement as TechnologyDefinition).TechnologyFlags != DepartmentOfScience.ConstructibleElement.TechnologyFlag.KaijuUnlock)
				{
					costString = string.Empty;
					turn = -1;
					global::Empire empire = context as global::Empire;
					if (empire == null)
					{
						Diagnostics.LogError("Empire is null.");
						return;
					}
					DepartmentOfScience agency = empire.GetAgency<DepartmentOfScience>();
					if (agency == null)
					{
						Diagnostics.LogError("Department of science is null");
						return;
					}
					float buyOutTechnologyCost = agency.GetBuyOutTechnologyCost(constructibleElement);
					if (buyOutTechnologyCost != 3.40282347E+38f)
					{
						costString = GuiFormater.FormatInstantCost(empire, buyOutTechnologyCost, DepartmentOfTheTreasury.Resources.EmpireMoney, true, 0);
					}
					else
					{
						costString = "-";
					}
					return;
				}
				else
				{
					for (int j = 0; j < constructibleElement.Costs.Length; j++)
					{
						if (constructibleElement.Costs[j] is PopulationConstructionCost)
						{
							PopulationConstructionCost populationConstructionCost = constructibleElement.Costs[j] as PopulationConstructionCost;
							PanelFeatureCost.AppendCost(SimulationProperties.Population, populationConstructionCost.PopulationValue, true);
						}
						else
						{
							float productionCostWithBonus = DepartmentOfTheTreasury.GetProductionCostWithBonus(context, constructibleElement, constructibleElement.Costs[j], true);
							PanelFeatureCost.AppendCost(constructibleElement.Costs[j].ResourceName, productionCostWithBonus, constructibleElement.Costs[j].Instant || constructibleElement.Costs[j].InstantOnCompletion);
						}
					}
				}
			}
		}
		PanelFeatureCost.GetCostAndTurn(guiService, departmentOfTheTreasury, context, out costString, out turn);
	}

	public static void ComputeCostAndTurn(Amplitude.Unity.Gui.IGuiService guiService, ConstructibleElement constructible, DepartmentOfTheTreasury departmentOfTheTreasury, SimulationObjectWrapper context, out string costString, out int turn)
	{
		PanelFeatureCost.costByResource.Clear();
		if (constructible.Costs != null)
		{
			if (constructible is TechnologyDefinition && context.SimulationObject.Tags.Contains(FactionTrait.FactionTraitReplicants1) && (constructible as TechnologyDefinition).TechnologyFlags != DepartmentOfScience.ConstructibleElement.TechnologyFlag.OrbUnlock && (constructible as TechnologyDefinition).TechnologyFlags != DepartmentOfScience.ConstructibleElement.TechnologyFlag.KaijuUnlock)
			{
				costString = string.Empty;
				turn = -1;
				global::Empire empire = context as global::Empire;
				if (empire == null)
				{
					Diagnostics.LogError("Empire is null.");
					return;
				}
				DepartmentOfScience agency = empire.GetAgency<DepartmentOfScience>();
				if (agency == null)
				{
					Diagnostics.LogError("Department of science is null");
					return;
				}
				float buyOutTechnologyCost = agency.GetBuyOutTechnologyCost(constructible);
				if (buyOutTechnologyCost != 3.40282347E+38f)
				{
					costString = GuiFormater.FormatInstantCost(empire, buyOutTechnologyCost, DepartmentOfTheTreasury.Resources.EmpireMoney, true, 0);
				}
				else
				{
					costString = "-";
				}
				return;
			}
			else
			{
				for (int i = 0; i < constructible.Costs.Length; i++)
				{
					if (constructible.Costs[i] is PopulationConstructionCost)
					{
						PopulationConstructionCost populationConstructionCost = constructible.Costs[i] as PopulationConstructionCost;
						PanelFeatureCost.AppendCost(SimulationProperties.Population, populationConstructionCost.PopulationValue, true);
					}
					else
					{
						float productionCostWithBonus = DepartmentOfTheTreasury.GetProductionCostWithBonus(context, constructible, constructible.Costs[i], true);
						PanelFeatureCost.AppendCost(constructible.Costs[i].ResourceName, productionCostWithBonus, constructible.Costs[i].Instant || constructible.Costs[i].InstantOnCompletion);
					}
				}
			}
		}
		PanelFeatureCost.GetCostAndTurn(guiService, departmentOfTheTreasury, context, out costString, out turn);
	}

	public static void GetCostAndTurn(Amplitude.Unity.Gui.IGuiService guiService, DepartmentOfTheTreasury departmentOfTheTreasury, SimulationObjectWrapper context, out string costString, out int turn)
	{
		turn = 0;
		StringBuilder stringBuilder = new StringBuilder();
		if (PanelFeatureCost.costByResource.Count > 0)
		{
			bool flag = false;
			IDatabase<ResourceDefinition> database = Databases.GetDatabase<ResourceDefinition>(false);
			foreach (KeyValuePair<StaticString, PanelFeatureCost.CostResume> keyValuePair in PanelFeatureCost.costByResource)
			{
				if (keyValuePair.Key == SimulationProperties.Population)
				{
					stringBuilder.Append(GuiFormater.FormatGui(keyValuePair.Value.Cost, false, true, false, 1));
					stringBuilder.Append(guiService.FormatSymbol(keyValuePair.Key));
				}
				else if (keyValuePair.Key == DepartmentOfTheTreasury.Resources.FreeBorough)
				{
					stringBuilder.Append(GuiFormater.FormatGui(keyValuePair.Value.Cost, false, true, false, 1));
					stringBuilder.Append(guiService.FormatSymbol(keyValuePair.Key));
					float num;
					if (!departmentOfTheTreasury.TryGetResourceStockValue(context.SimulationObject, DepartmentOfTheTreasury.Resources.QueuedFreeBorough, out num, true))
					{
						Diagnostics.Log("Can't get resource stock value {0} on simulation object {1}.", new object[]
						{
							DepartmentOfTheTreasury.Resources.QueuedFreeBorough,
							context.SimulationObject.Name
						});
					}
					else
					{
						stringBuilder.Append(string.Format(AgeLocalizer.Instance.LocalizeString("%CityFreeBoroughsLeft"), num));
					}
				}
				else
				{
					float cost = keyValuePair.Value.Cost;
					StaticString key = keyValuePair.Key;
					ResourceDefinition resourceDefinition;
					if (!database.TryGetValue(key, out resourceDefinition))
					{
						Diagnostics.LogError("Invalid resource name. The resource {0} does not exist in the resource database.", new object[]
						{
							key
						});
					}
					else
					{
						string value = guiService.FormatSymbol(resourceDefinition.GetName(departmentOfTheTreasury.Empire));
						if (!string.IsNullOrEmpty(value))
						{
							global::Empire empire = null;
							if (context is City)
							{
								empire = (context as City).Empire;
							}
							else if (context is global::Empire)
							{
								empire = (context as global::Empire);
							}
							if (empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitMimics2) && key == DepartmentOfTheTreasury.Resources.Production)
							{
								ResourceDefinition resourceDefinition2;
								if (!database.TryGetValue(SimulationProperties.CityGrowth, out resourceDefinition2))
								{
									Diagnostics.LogError("Invalid resource name. The resource {0} does not exist in the resource database.", new object[]
									{
										key
									});
									continue;
								}
								value = guiService.FormatSymbol(resourceDefinition2.GetName(departmentOfTheTreasury.Empire));
								if (string.IsNullOrEmpty(value))
								{
									continue;
								}
							}
							float num2;
							if (!departmentOfTheTreasury.TryGetResourceStockValue(context.SimulationObject, key, out num2, true))
							{
								num2 = 0f;
							}
							if (keyValuePair.Value.Instant && num2 < cost)
							{
								AgeUtils.ColorToHexaKey(Color.red, ref stringBuilder, false);
								flag = true;
							}
							stringBuilder.Append(GuiFormater.FormatGui(cost, false, true, false, 1));
							stringBuilder.Append(value);
							if (flag)
							{
								stringBuilder.Append("#REVERT#");
								flag = false;
							}
							if (!keyValuePair.Value.Instant)
							{
								float num3;
								if (!departmentOfTheTreasury.TryGetNetResourceValue(context.SimulationObject, key, out num3, true))
								{
									num3 = 0f;
								}
								if (cost > num2)
								{
									if (num3 <= 0f)
									{
										turn = int.MaxValue;
									}
									else
									{
										int num4 = Mathf.CeilToInt((cost - num2) / num3);
										if (num4 > turn)
										{
											turn = num4;
										}
									}
								}
							}
							stringBuilder.Append(" ");
						}
					}
				}
			}
		}
		costString = stringBuilder.ToString();
		if (string.IsNullOrEmpty(costString))
		{
			costString = "-";
		}
	}

	protected override void DeserializeFeatureDescription(XmlElement featureDescription)
	{
		base.DeserializeFeatureDescription(featureDescription);
		if (featureDescription.Name == "DisplayTurnCost")
		{
			string attribute = featureDescription.GetAttribute("Value");
			if (!string.IsNullOrEmpty(attribute))
			{
				this.DisplayTurnCost = bool.Parse(attribute);
			}
		}
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		this.CostValue.Text = string.Empty;
		this.TurnValue.Text = string.Empty + GuiFormater.Infinite;
		ICostFeatureProvider provider = this.context as ICostFeatureProvider;
		if (provider != null && provider.Constructible != null && provider.Empire != null)
		{
			DepartmentOfTheTreasury departmentOfTheTreasury = provider.Empire.GetAgency<DepartmentOfTheTreasury>();
			SimulationObjectWrapper context = (provider.Context == null) ? provider.Empire : provider.Context;
			string costString;
			int turn;
			PanelFeatureCost.ComputeCostAndTurn(base.GuiService, provider.Constructible, departmentOfTheTreasury, context, out costString, out turn);
			this.CostValue.AgeTransform.PixelMarginRight = 0f;
			if (this.DisplayTurnCost)
			{
				this.CostValue.AgeTransform.PixelMarginRight = base.AgeTransform.Width - this.TurnValue.AgeTransform.X - 20f * AgeUtils.CurrentUpscaleFactor();
			}
			this.CostValue.AgeTransform.ComputeExtents(Rect.MinMaxRect(0f, 0f, 0f, 0f));
			this.CostValue.Text = costString;
			this.CostTitle.Text = "%FeatureCostTitle";
			if (provider.Constructible is KaijuTechnologyDefinition)
			{
				KaijuTechnologyDefinition kaijuTechnologyDefinition = provider.Constructible as KaijuTechnologyDefinition;
				IGameService gameService = Services.GetService<IGameService>();
				IKaijuTechsService kaijuTechsService = gameService.Game.Services.GetService<IKaijuTechsService>();
				if (kaijuTechsService.GetTechnologyState(kaijuTechnologyDefinition, provider.Empire) == DepartmentOfScience.ConstructibleElement.State.Researched)
				{
					this.CostTitle.Text = "%FeatureAlreadyUnlockedTitle";
					costString = " ";
				}
			}
			this.CostValue.Text = costString;
			if (this.CostValue.AgeTransform.PixelMarginTop == this.CostTitle.AgeTransform.PixelMarginTop)
			{
				float pixelMargin = 2f * this.CostTitle.AgeTransform.PixelMarginLeft + this.CostTitle.Font.ComputeTextWidth(AgeLocalizer.Instance.LocalizeString(this.CostTitle.Text), this.CostTitle.ForceCaps, false);
				this.CostValue.AgeTransform.PixelMarginLeft = pixelMargin;
			}
			if (provider.Context == null)
			{
				this.TurnValue.Text = string.Empty;
				this.TurnIcon.AgeTransform.Visible = false;
			}
			else
			{
				this.TurnIcon.AgeTransform.Visible = true;
				if (turn == 2147483647)
				{
					this.TurnValue.Text = string.Empty + GuiFormater.Infinite;
				}
				else
				{
					turn = Mathf.Max(1, turn);
					this.TurnValue.Text = turn.ToString();
				}
			}
		}
		yield return base.OnShow(parameters);
		this.CostValue.AgeTransform.Height = this.CostValue.Font.LineHeight * (float)this.CostValue.TextLines.Count;
		base.AgeTransform.Height = this.CostValue.Font.LineHeight * (float)this.CostValue.TextLines.Count + this.CostValue.AgeTransform.PixelMarginTop + this.CostValue.AgeTransform.PixelMarginBottom;
		base.AgeTransform.Visible = !string.IsNullOrEmpty(this.CostValue.Text);
		if (!this.DisplayTurnCost)
		{
			this.TurnIcon.AgeTransform.Visible = false;
			this.TurnValue.AgeTransform.Visible = false;
		}
		yield break;
	}

	private static void AppendCost(StaticString resourceName, float value, bool instant)
	{
		if (!PanelFeatureCost.costByResource.ContainsKey(resourceName))
		{
			PanelFeatureCost.costByResource.Add(resourceName, default(PanelFeatureCost.CostResume));
		}
		value += PanelFeatureCost.costByResource[resourceName].Cost;
		instant |= PanelFeatureCost.costByResource[resourceName].Instant;
		PanelFeatureCost.costByResource[resourceName] = new PanelFeatureCost.CostResume(value, instant);
	}

	public const string Separator = " ";

	public AgePrimitiveLabel CostTitle;

	public AgePrimitiveLabel CostValue;

	public AgePrimitiveLabel TurnValue;

	public AgePrimitiveImage TurnIcon;

	public bool DisplayTurnCost = true;

	private static Dictionary<StaticString, PanelFeatureCost.CostResume> costByResource = new Dictionary<StaticString, PanelFeatureCost.CostResume>();

	private struct CostResume
	{
		public CostResume(float cost, bool instant)
		{
			this.Cost = cost;
			this.Instant = instant;
		}

		public float Cost;

		public bool Instant;
	}
}
