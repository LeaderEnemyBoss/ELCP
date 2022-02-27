using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude.WorldGenerator.Graphs.Hex;
using Amplitude.WorldGenerator.World;
using Amplitude.WorldGenerator.World.Info;

namespace Amplitude.WorldGenerator.Tasks.Generator
{
	public class DistributeStrategicResources : DistributePOITask
	{
		public override void Execute(object context)
		{
			base.Silent = false;
			base.Execute(context);
			base.UsedTemplates = new List<PointOfInterestTemplate>(from poiTemplate in base.Context.Settings.POITemplates.Values
			where poiTemplate.GetPropertyValue("Type") == "ResourceDeposit"
			where poiTemplate.GetPropertyValue("ResourceType") == "Strategic"
			where poiTemplate.HasProperty("StrategicTier")
			where poiTemplate.HasProperty("NormalQuantity")
			where poiTemplate.HasProperty("ResourceName")
			select poiTemplate);
			new List<PointOfInterestTemplate>(from poiT in base.UsedTemplates
			where poiT.GetPropertyValue("StrategicTier") == "Tier1"
			select poiT);
			if (base.UsedTemplates.Count < 1)
			{
				base.Trace("No valid POI template found");
				base.Trace("Algorithm bypassed");
				return;
			}
			this.Influence = new InfluenceMap(base.Context.Grid);
			this.Sensitivity_MapSize = base.Parameters.GetValue("MapSizeSensitivity");
			this.Sensitivity_EmpireCount = base.Parameters.GetValue("EmpireCountSensitivity");
			this.AbundanceFactor = base.Context.Settings.StrategicResourcesAbundancePercent;
			if (this.AbundanceFactor <= 0)
			{
				return;
			}
			this.MapSizeFactor = (from d in base.Context.Districts.Values
			where d.Content == District.Contents.Land
			select d).Sum((District d) => d.Count) / 20;
			int num = (this.MapSizeFactor - 100) * this.Sensitivity_MapSize / 100;
			this.MapSizeFactor = 100 + num;
			this.CompetitionFactor = base.Context.EmpiresCount * 25;
			num = (this.CompetitionFactor - 100) * this.Sensitivity_EmpireCount / 100;
			this.CompetitionFactor = 100 + num;
			this.ProcessRules();
			foreach (PointOfInterestTemplate pointOfInterestTemplate in base.UsedTemplates)
			{
				string propertyValue = pointOfInterestTemplate.GetPropertyValue("ResourceName");
				List<PointOfInterestDefinition> list = new List<PointOfInterestDefinition>();
				int num2 = this.ComputeGlobalNumberOfSites(pointOfInterestTemplate) + this.ExtraneousSiteCount[pointOfInterestTemplate.GetPropertyValue("ResourceName")];
				int num3 = this.MinSites[propertyValue].Values.Sum();
				if (num2 < num3)
				{
					num2 = num3;
				}
				base.Trace(string.Format("{0} : {1} sites", pointOfInterestTemplate.GetPropertyValue("ResourceName"), num2));
				int i = 0;
				List<HexPos> list2 = new List<HexPos>();
				while (i < num2)
				{
					list2.Clear();
					HashSet<int> closedContinents = new HashSet<int>();
					for (int j = 1; j <= base.Context.Settings.LandMasses; j++)
					{
						if (this.CreatedSites[propertyValue][j] >= this.MaxSites[propertyValue][j])
						{
							closedContinents.Add(j);
						}
					}
					list2 = new List<HexPos>(from region in base.Context.Regions.Values
					where !closedContinents.Contains(region.LandMassIndex) && region.Resources.Count < this.Context.Settings.MaxResourcesPerRegion + region.Biome.BonusResourceLimit
					from district in region.Districts
					where district.Content == District.Contents.Land
					from hex in district
					where base.Context.POIValidityMap[hex.Row, hex.Column] == WorldGeneratorContext.POIValidity.Free
					orderby this.Influence.GetMinDistance(hex) descending
					select hex);
					if (this.MinSites[propertyValue].Values.Any((int q) => q > 0))
					{
						int continent = 0;
						int num4 = 0;
						foreach (int num5 in this.MinSites[propertyValue].Keys)
						{
							if (num4 < this.MinSites[propertyValue][num5] - this.CreatedSites[propertyValue][num5])
							{
								continent = num5;
								num4 = this.MinSites[propertyValue][num5] - this.CreatedSites[propertyValue][num5];
							}
						}
						if (num4 > 0)
						{
							List<HexPos> list3 = new List<HexPos>(from region in base.Context.Regions.Values
							where region.LandMassIndex == continent && region.Resources.Count < this.Context.Settings.MaxResourcesPerRegion + region.Biome.BonusResourceLimit
							from district in region.Districts
							where district.Content == District.Contents.Land
							from hex in district
							where base.Context.POIValidityMap[hex.Row, hex.Column] == WorldGeneratorContext.POIValidity.Free
							orderby this.Influence.GetMinDistance(hex) descending
							select hex);
							if (list3.Count > 0)
							{
								list2 = list3;
							}
						}
					}
					if (list2.Count != 0)
					{
						this.PickResourceSite(pointOfInterestTemplate, list, list2);
					}
					i++;
				}
				foreach (PointOfInterestDefinition pointOfInterestDefinition in list)
				{
					base.Trace(string.Format("POI {0} in {1}", pointOfInterestDefinition.TemplateName, base.Context.GetDistrict(pointOfInterestDefinition.Position).MotherRegion.LandMassIndex));
				}
			}
			if (base.Context.Configuration.IsDLCAvailable("SummerFlamesPack"))
			{
				List<PointOfInterestDefinition> list4 = new List<PointOfInterestDefinition>();
				Region[] array = (from region in base.Context.Regions.Values
				where region.Biome.BonusResourceLimit != 0 && region.Resources.Count <= base.Context.Settings.MaxResourcesPerRegion + region.Biome.BonusResourceLimit
				select region).ToArray<Region>();
				Random random = new Random(base.Context.Settings.Seed);
				int k = 0;
				while (k < array.Length)
				{
					base.Trace(string.Format("Adding bonus POI to {0}", array[k].Name));
					List<HexPos> list5 = new List<HexPos>(from district in array[k].Districts
					where district.Content == District.Contents.Land
					from hex in district
					where base.Context.POIValidityMap[hex.Row, hex.Column] == WorldGeneratorContext.POIValidity.Free
					orderby this.Influence.GetMinDistance(hex) descending
					select hex);
					if (list5.Count <= 0 || random.NextDouble() >= (double)array[k].Biome.BonusResourceChance)
					{
						goto IL_909;
					}
					PointOfInterestTemplate poiTemplate3 = base.UsedTemplates[random.Next(base.UsedTemplates.Count)];
					this.PickResourceSite(poiTemplate3, list4, list5);
					if (array[k].Resources.Count < base.Context.Settings.MaxResourcesPerRegion + array[k].Biome.BonusResourceLimit && list5.Count != 0)
					{
						goto IL_909;
					}
					IL_957:
					k++;
					continue;
					IL_909:
					if (list5.Count > 0 && random.NextDouble() < (double)array[k].Biome.BonusResourceChance)
					{
						PointOfInterestTemplate poiTemplate2 = base.UsedTemplates[random.Next(base.UsedTemplates.Count)];
						this.PickResourceSite(poiTemplate2, list4, list5);
						goto IL_957;
					}
					goto IL_957;
				}
				foreach (PointOfInterestDefinition pointOfInterestDefinition2 in list4)
				{
					base.Trace(string.Format("Bonus POI {0} in {1}", pointOfInterestDefinition2.TemplateName, base.Context.GetDistrict(pointOfInterestDefinition2.Position).MotherRegion.Name));
				}
			}
			base.Silent = true;
		}

		private void PickResourceSite(PointOfInterestTemplate poiTemplate, List<PointOfInterestDefinition> createdPOIs, List<HexPos> availableSpots)
		{
			bool flag = false;
			int num = 20;
			string propertyValue = poiTemplate.GetPropertyValue("ResourceName");
			do
			{
				num--;
				HexPos hexPos = availableSpots.First<HexPos>();
				availableSpots.RemoveAt(0);
				Region region = base.Context.Regions[base.Context.RegionData[hexPos.Row, hexPos.Column]];
				PointOfInterestDefinition pointOfInterestDefinition = this.MakeNewPOI(hexPos, poiTemplate, 1);
				this.Influence.AddSpot(hexPos);
				if (pointOfInterestDefinition != null)
				{
					createdPOIs.Add(pointOfInterestDefinition);
					Dictionary<int, int> dictionary = this.CreatedSites[propertyValue];
					int landMassIndex = region.LandMassIndex;
					int num2 = dictionary[landMassIndex];
					dictionary[landMassIndex] = num2 + 1;
					region.Resources.Add(pointOfInterestDefinition);
					flag = true;
				}
			}
			while (!flag && num > 0 && availableSpots.Count > 0);
		}

		public static int EstimatedGlobalNumberOfSites(WorldGeneratorContext context, WorldGeneratorSettings.AlgorithmParameters parameters, PointOfInterestTemplate poiTemplate)
		{
			int value = parameters.GetValue("MapSizeSensitivity");
			int value2 = parameters.GetValue("EmpireCountSensitivity");
			int strategicResourcesAbundancePercent = context.Settings.StrategicResourcesAbundancePercent;
			if (strategicResourcesAbundancePercent <= 0)
			{
				return 0;
			}
			int num = (from d in context.Districts.Values
			where d.Content == District.Contents.Land
			select d).Sum((District d) => d.Count) / 20;
			int num2 = (num - 100) * value / 100;
			num = 100 + num2;
			int num3 = context.EmpiresCount * 25;
			num2 = (num3 - 100) * value2 / 100;
			num3 = 100 + num2;
			int num4;
			if (!int.TryParse(poiTemplate.GetPropertyValue("NormalQuantity"), out num4))
			{
				num4 = 1;
			}
			else if (num4 == 0)
			{
				return 0;
			}
			num4 *= strategicResourcesAbundancePercent * num * num3;
			num4 /= 1000000;
			if (context.OceanicResourceCounts != null && context.OceanicResourceCounts.ContainsKey(poiTemplate.GetPropertyValue("ResourceName")))
			{
				int num5 = context.OceanicResourceCounts[poiTemplate.GetPropertyValue("ResourceName")];
				num4 -= num5;
			}
			return num4;
		}

		private int ComputeGlobalNumberOfSites(PointOfInterestTemplate poiTemplate)
		{
			int num;
			if (!int.TryParse(poiTemplate.GetPropertyValue("NormalQuantity"), out num))
			{
				num = 1;
			}
			else if (num == 0)
			{
				return 0;
			}
			num *= this.AbundanceFactor * this.MapSizeFactor * this.CompetitionFactor;
			num /= 1000000;
			base.Trace(string.Format("{1} : normal number of sites : {0}", num, poiTemplate.GetPropertyValue("ResourceName")));
			if (base.Context.OceanicResourceCounts.ContainsKey(poiTemplate.GetPropertyValue("ResourceName")))
			{
				int num2 = base.Context.OceanicResourceCounts[poiTemplate.GetPropertyValue("ResourceName")];
				if (!base.Context.Settings.XephiStrategic)
				{
					base.Trace(string.Format("removing {0} sites already moved to oceans", num2));
					num -= num2;
				}
			}
			if (num < 1)
			{
				num = 1;
			}
			return num;
		}

		private void ProcessRules()
		{
			this.ApplicableRules = new List<Rule>();
			this.ExtraneousSiteCount = new Dictionary<string, int>();
			this.CreatedSites = new Dictionary<string, Dictionary<int, int>>();
			this.MinSites = new Dictionary<string, Dictionary<int, int>>();
			this.MaxSites = new Dictionary<string, Dictionary<int, int>>();
			Dictionary<string, int> dictionary = new Dictionary<string, int>();
			foreach (PointOfInterestTemplate pointOfInterestTemplate in base.UsedTemplates)
			{
				string propertyValue = pointOfInterestTemplate.GetPropertyValue("ResourceName");
				int num = this.ComputeGlobalNumberOfSites(pointOfInterestTemplate);
				dictionary.Add(propertyValue, num / base.Context.Settings.LandMasses);
				this.ExtraneousSiteCount.Add(propertyValue, 0);
				this.CreatedSites.Add(propertyValue, new Dictionary<int, int>());
				this.MinSites.Add(propertyValue, new Dictionary<int, int>());
				this.MaxSites.Add(propertyValue, new Dictionary<int, int>());
				for (int i = 1; i <= base.Context.Settings.LandMasses; i++)
				{
					this.CreatedSites[propertyValue].Add(i, 0);
					this.MinSites[propertyValue].Add(i, 0);
					this.MaxSites[propertyValue].Add(i, num);
				}
			}
			if (base.Context.Settings.Scenario == null)
			{
				return;
			}
			Dictionary<int, Dictionary<string, Rule>> ruleLogicProcessor = new Dictionary<int, Dictionary<string, Rule>>();
			foreach (Rule rule3 in new List<Rule>(from rule in base.Context.Settings.Scenario.Rules
			where rule.GetRuleType == Rule.RuleType.ForbidStrategic || rule.GetRuleType == Rule.RuleType.ForceStrategic || rule.GetRuleType == Rule.RuleType.StrategicAbundance
			where rule.Continent >= 1 && rule.Continent <= base.Context.Settings.LandMasses
			where base.UsedTemplates.Any((PointOfInterestTemplate pt) => pt.GetPropertyValue("ResourceName") == rule.Resource)
			orderby rule.PriorityRank
			select rule))
			{
				if (!ruleLogicProcessor.ContainsKey(rule3.Continent))
				{
					ruleLogicProcessor.Add(rule3.Continent, new Dictionary<string, Rule>());
				}
				if (!ruleLogicProcessor[rule3.Continent].ContainsKey(rule3.Resource))
				{
					ruleLogicProcessor[rule3.Continent].Add(rule3.Resource, rule3);
				}
				else if (rule3.PriorityRank < ruleLogicProcessor[rule3.Continent][rule3.Resource].PriorityRank)
				{
					ruleLogicProcessor[rule3.Continent][rule3.Resource] = rule3;
				}
			}
			this.ApplicableRules.AddRange(from c in ruleLogicProcessor.Keys
			from r in ruleLogicProcessor[c].Keys
			select ruleLogicProcessor[c][r]);
			foreach (Rule rule2 in this.ApplicableRules)
			{
				base.Trace(string.Format("applying rule {0} for {1} in {2}", rule2.GetRuleType.ToString(), rule2.Resource, rule2.Continent));
				if (rule2.GetRuleType == Rule.RuleType.ForbidStrategic)
				{
					this.MaxSites[rule2.Resource][rule2.Continent] = 0;
				}
				else if (rule2.GetRuleType == Rule.RuleType.ForceStrategic)
				{
					this.MinSites[rule2.Resource][rule2.Continent] = 1;
				}
				else if (rule2.GetRuleType == Rule.RuleType.StrategicAbundance)
				{
					if (rule2.Abundance > 0)
					{
						if (rule2.Abundance < 100)
						{
							Dictionary<int, int> dictionary2 = this.MaxSites[rule2.Resource];
							int continent = rule2.Continent;
							dictionary2[continent] *= rule2.Abundance;
							dictionary2 = this.MaxSites[rule2.Resource];
							continent = rule2.Continent;
							dictionary2[continent] /= 100;
						}
						else if (rule2.Abundance > 100)
						{
							int num2 = dictionary[rule2.Resource] * (rule2.Abundance - 100) / 100;
							this.MinSites[rule2.Resource][rule2.Continent] = num2;
							Dictionary<string, int> extraneousSiteCount = this.ExtraneousSiteCount;
							string resource = rule2.Resource;
							extraneousSiteCount[resource] += num2;
						}
					}
					else
					{
						this.MaxSites[rule2.Resource][rule2.Continent] = 0;
					}
				}
			}
			foreach (PointOfInterestTemplate pointOfInterestTemplate2 in base.UsedTemplates)
			{
				string propertyValue2 = pointOfInterestTemplate2.GetPropertyValue("ResourceName");
				base.Trace(string.Format("Extraneous {0} : {1}", propertyValue2, this.ExtraneousSiteCount[propertyValue2]));
				for (int j = 1; j <= base.Context.Settings.LandMasses; j++)
				{
					base.Trace(string.Format("{0} in {1} : Min {2} - Max {3}", new object[]
					{
						propertyValue2,
						j,
						this.MinSites[propertyValue2][j],
						this.MaxSites[propertyValue2][j]
					}));
				}
			}
		}

		private int AbundanceFactor;

		private int MapSizeFactor;

		private int CompetitionFactor;

		private int Sensitivity_MapSize;

		private int Sensitivity_EmpireCount;

		public static InfluenceMap Influence;

		private List<Rule> ApplicableRules;

		private Dictionary<string, Dictionary<int, int>> MinSites;

		private Dictionary<string, Dictionary<int, int>> MaxSites;

		public static Dictionary<string, Dictionary<int, int>> CreatedSites;

		private Dictionary<string, int> ExtraneousSiteCount;
	}
}
