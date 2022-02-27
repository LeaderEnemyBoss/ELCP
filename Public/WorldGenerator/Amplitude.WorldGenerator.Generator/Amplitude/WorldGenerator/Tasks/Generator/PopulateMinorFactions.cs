using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude.WorldGenerator.World;
using Amplitude.WorldGenerator.World.Info;

namespace Amplitude.WorldGenerator.Tasks.Generator
{
	public class PopulateMinorFactions : WorldGeneratorTask
	{
		public override void Execute(object context)
		{
			base.Execute(context);
			string key = "DistributeVillages";
			WorldGeneratorSettings.AlgorithmParameters algorithmParameters = null;
			if (base.Context.Settings.Algorithms.ContainsKey(key))
			{
				algorithmParameters = base.Context.Settings.Algorithms[key];
			}
			this.FactionCounter = new Dictionary<string, int>();
			this.MinorFactionNames = new List<string>(from poi in base.Context.Settings.POITemplates.Values
			where poi.GetPropertyValue("Type") == "Village"
			where poi.HasProperty("AffinityMapping")
			select poi.GetPropertyValue("AffinityMapping")).Distinct<string>().ToList<string>();
			if (this.MinorFactionNames.Count < 1)
			{
				return;
			}
			int num = 1;
			int num2 = 3;
			int num3 = 1;
			int num4 = 1;
			if (algorithmParameters != null)
			{
				int value = algorithmParameters.GetValue("MinSitesPerEmptyRegion");
				int value2 = algorithmParameters.GetValue("MaxSitesPerEmptyRegion");
				num = Math.Min(value, value2);
				num2 = Math.Max(value, value2);
				int value3 = algorithmParameters.GetValue("MinSitesPerSpawnRegion");
				value2 = algorithmParameters.GetValue("MaxSitesPerSpawnRegion");
				num3 = Math.Min(value3, value2);
				num4 = Math.Max(value3, value2);
			}
			List<Region> list;
			if (!base.Context.Settings.XephiWorldGeneratorBalance)
			{
				list = new List<Region>(from r in base.Context.Regions.Values
				where r.LandMassType == Region.LandMassTypes.Continent
				where !base.Context.SpawnRegions.Contains(r.Id)
				orderby r.HexCount()
				select r);
			}
			else
			{
				list = new List<Region>(from r in base.Context.Regions.Values
				where r.LandMassType == Region.LandMassTypes.Continent
				where !base.Context.SpawnRegions.Contains(r.Id)
				orderby r.Resources.Count descending
				select r);
			}
			List<Region> list2;
			if (!base.Context.Settings.XephiWorldGeneratorBalance)
			{
				list2 = new List<Region>(from i in base.Context.SpawnRegions
				let r = base.Context.Regions[i]
				where r.LandMassType == Region.LandMassTypes.Continent
				orderby r.HexCount()
				select r);
			}
			else
			{
				list2 = new List<Region>(from i in base.Context.SpawnRegions
				let r = base.Context.Regions[i]
				where r.LandMassType == Region.LandMassTypes.Continent
				orderby r.Resources.Count descending
				select r);
			}
			this.ForbiddenContinents = new Dictionary<string, HashSet<int>>();
			foreach (string key2 in this.MinorFactionNames)
			{
				if (!this.FactionCounter.ContainsKey(key2))
				{
					this.FactionCounter.Add(key2, 0);
				}
				if (!this.ForbiddenContinents.ContainsKey(key2))
				{
					this.ForbiddenContinents.Add(key2, new HashSet<int>());
				}
			}
			this.ApplyScenarioRules();
			foreach (Region region in list)
			{
				float num5 = (float)list.IndexOf(region) / (float)list.Count;
				region.Villages = (int)(num5 * (float)(num2 - num + 1)) + num;
				if (region.Villages > 0)
				{
					this.SpawnVillageInRegion(region);
				}
			}
			foreach (Region region2 in list2)
			{
				float num6 = (float)list2.IndexOf(region2) / (float)list2.Count;
				region2.Villages = (int)(num6 * (float)(num4 - num3 + 1)) + num3;
				if (region2.Villages > 0)
				{
					this.SpawnVillageInRegion(region2);
				}
			}
			foreach (Region region3 in from r in base.Context.Regions.Values
			where r.LandMassType == Region.LandMassTypes.Continent
			select r)
			{
				base.Trace(string.Format("Region {0} in continent {1} : {2} villages of {3}", new object[]
				{
					region3.Id,
					region3.LandMassIndex,
					region3.Villages,
					region3.MinorFactionName
				}));
			}
		}

		protected void SpawnVillageInRegion(Region region)
		{
			List<string> list = new List<string>(from n in this.MinorFactionNames
			where !this.ForbiddenContinents[n].Contains(region.LandMassIndex)
			orderby this.FactionCounter[n], base.Context.Randomizer.NextDouble() descending
			select n);
			if (list.Count < 1)
			{
				return;
			}
			region.MinorFactionName = list.First<string>();
			Dictionary<string, int> factionCounter = this.FactionCounter;
			string minorFactionName = region.MinorFactionName;
			int num = factionCounter[minorFactionName];
			factionCounter[minorFactionName] = num + 1;
		}

		protected void ApplyScenarioRules()
		{
			if (base.Context.Settings.Scenario == null)
			{
				return;
			}
			HashSet<int> hashSet = new HashSet<int>();
			for (int i = 1; i <= base.Context.Settings.LandMasses; i++)
			{
				hashSet.Add(i);
			}
			foreach (Rule rule in new List<Rule>(from r in base.Context.Settings.Scenario.Rules
			where r.GetRuleType == Rule.RuleType.ForceMinorFaction || r.GetRuleType == Rule.RuleType.ForbidMinorFaction
			where r.Continent >= 1 && r.Continent <= base.Context.Settings.LandMasses
			where this.MinorFactionNames.Contains(r.MinorFaction)
			orderby r.PriorityRank descending
			select r))
			{
				base.Trace(string.Format("Applying rule {0} to {1} in {2}", rule.GetRuleType.ToString(), rule.MinorFaction, rule.Continent));
				if (rule.GetRuleType == Rule.RuleType.ForbidMinorFaction)
				{
					this.ForbiddenContinents[rule.MinorFaction].Add(rule.Continent);
					this.FactionCounter[rule.MinorFaction] = 0;
				}
				else if (rule.GetRuleType == Rule.RuleType.ForceMinorFaction)
				{
					this.ForbiddenContinents[rule.MinorFaction].UnionWith(hashSet);
					this.ForbiddenContinents[rule.MinorFaction].Remove(rule.Continent);
					this.FactionCounter[rule.MinorFaction] = -1;
				}
			}
		}

		protected Dictionary<string, int> FactionCounter;

		protected List<string> MinorFactionNames;

		protected Dictionary<string, HashSet<int>> ForbiddenContinents;
	}
}
