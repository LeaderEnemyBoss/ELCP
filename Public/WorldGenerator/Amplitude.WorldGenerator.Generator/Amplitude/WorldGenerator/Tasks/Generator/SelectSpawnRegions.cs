﻿using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude.WorldGenerator.Graphs;
using Amplitude.WorldGenerator.Graphs.Hex;
using Amplitude.WorldGenerator.World;
using Amplitude.WorldGenerator.World.Info;

namespace Amplitude.WorldGenerator.Tasks.Generator
{
	public class SelectSpawnRegions : WorldGeneratorTask
	{
		private float OverallRegionEvaluation(WorldGeneratorSettings.Faction faction, Region region)
		{
			float num = 0f;
			float num2 = 0f;
			if (this.RegionIntrinsicValues.ContainsKey(region))
			{
				num += base.Context.Settings.SpawnDesirabilityShape * (float)this.OrderedByIntrinsic.IndexOf(region) / (float)this.OrderedByIntrinsic.Count;
				num2 += base.Context.Settings.SpawnDesirabilityShape;
			}
			if (this.RegionSpawnPointEvaluations.ContainsKey(faction.Name) && this.RegionSpawnPointEvaluations[faction.Name].ContainsKey(region))
			{
				num += base.Context.Settings.SpawnDesirabilityFIDS * (float)this.OrderedBySpawnValue[faction.Name].IndexOf(region) / (float)this.OrderedBySpawnValue[faction.Name].Count;
				num2 += base.Context.Settings.SpawnDesirabilityFIDS;
			}
			if (this.RegionTacticalValues.ContainsKey(region))
			{
				num += base.Context.Settings.SpawnDesirabilityLocation * (float)this.OrderedByTactical.IndexOf(region) / (float)this.OrderedByTactical.Count;
				num2 += base.Context.Settings.SpawnDesirabilityLocation;
			}
			if (num2 != 0f)
			{
				num /= num2;
			}
			else
			{
				num = (float)base.Context.Randomizer.NextDouble();
			}
			return num;
		}

		private void ApplyScenarioRules()
		{
			HashSet<int> hashSet = new HashSet<int>();
			for (int i = 1; i <= base.Context.Settings.LandMasses; i++)
			{
				hashSet.Add(i);
			}
			for (int j = 0; j < base.Context.EmpiresCount; j++)
			{
				this.AllowedContinents.Add(j, new HashSet<int>(hashSet));
			}
			if (base.Context.Settings.Scenario == null)
			{
				return;
			}
			using (List<Rule>.Enumerator enumerator = new List<Rule>(from r in base.Context.Settings.Scenario.Rules
			where r.GetRuleType == Rule.RuleType.ForceSpawn || r.GetRuleType == Rule.RuleType.ForbidSpawn
			where r.Continent >= 1 && r.Continent <= base.Context.Settings.LandMasses
			where r.Empire >= 0 && r.Empire < base.Context.EmpiresCount
			orderby r.PriorityRank descending
			select r).GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Rule rule = enumerator.Current;
					base.Trace(string.Format("Applying Spawn rule {0} to empire {1} in continent {2}", rule.GetRuleType.ToString(), rule.Empire, rule.Continent));
					if (rule.GetRuleType == Rule.RuleType.ForbidSpawn)
					{
						this.AllowedContinents[rule.Empire].Remove(rule.Continent);
					}
					else if (rule.GetRuleType == Rule.RuleType.ForceSpawn)
					{
						this.AllowedContinents[rule.Empire].RemoveWhere((int c) => c != rule.Continent);
					}
				}
			}
			for (int k = 0; k < base.Context.EmpiresCount; k++)
			{
				if (this.AllowedContinents[k].Count < 1)
				{
					this.AllowedContinents[k] = new HashSet<int>(hashSet);
				}
			}
		}

		public override void Execute(object context)
		{
			base.Execute(context);
			if (base.Context.EmpiresCount <= 0)
			{
				base.Trace("Empty Empire list - No Spawn Point generated");
				return;
			}
			base.Context.SpawnRegions = new short[base.Context.EmpiresCount];
			base.Context.SpawnPointsDefault = new HexPos[base.Context.EmpiresCount];
			this.Evaluations = new Dictionary<string, int[,]>();
			this.RegionSpawnPointEvaluations = new Dictionary<string, Dictionary<Region, int>>();
			this.OrderedBySpawnValue = new Dictionary<string, List<Region>>();
			this.RegionIntrinsicValues = new Dictionary<Region, int>();
			this.RegionTacticalValues = new Dictionary<Region, int>();
			this.RegionSpawnPoints = new Dictionary<string, Dictionary<Region, HexPos>>();
			this.AllowedContinents = new Dictionary<int, HashSet<int>>();
			this.ApplyScenarioRules();
			HashSet<string> hashSet = new HashSet<string>(from EmpireDefinition empire in base.Context.Configuration.Empires
			where base.Context.Settings.FactionSpawnPreferences.ContainsKey(empire.Name)
			select empire.Name);
			HashSet<string> hashSet2 = new HashSet<string>(hashSet);
			hashSet.Add("Default");
			List<WorldGeneratorSettings.Faction> list = new List<WorldGeneratorSettings.Faction>(from factionName in hashSet
			select base.Context.Settings.FactionSpawnPreferences[factionName]);
			List<Region> list2 = new List<Region>(from r in base.Context.Regions.Values
			where r.LandMassType == Region.LandMassTypes.Continent
			select r);
			bool mustAcceptIslands = list2.Count((Region r) => !r.IsIsland) < base.Context.EmpiresCount;
			foreach (Region region in list2)
			{
				this.RegionIntrinsicValues.Add(region, 0);
				Dictionary<Region, int> regionIntrinsicValues = this.RegionIntrinsicValues;
				Region key = region;
				regionIntrinsicValues[key] += region.HexCount() + 3 * region.DeepHexes.Count + 10 * region.Resources.Count;
				if (region.Neighbours.Any((Region n) => n.LandMassType == Region.LandMassTypes.Ocean))
				{
					regionIntrinsicValues = this.RegionIntrinsicValues;
					key = region;
					regionIntrinsicValues[key] *= 11;
					regionIntrinsicValues = this.RegionIntrinsicValues;
					key = region;
					regionIntrinsicValues[key] /= 10;
				}
			}
			this.OrderedByIntrinsic = new List<Region>(from r in this.RegionIntrinsicValues.Keys
			orderby this.RegionIntrinsicValues[r]
			select r);
			using (List<WorldGeneratorSettings.Faction>.Enumerator enumerator2 = list.GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					WorldGeneratorSettings.Faction faction = enumerator2.Current;
					if (!this.Evaluations.ContainsKey(faction.Name))
					{
						this.Evaluations.Add(faction.Name, new int[base.Context.Grid.Rows, base.Context.Grid.Columns]);
						this.RegionSpawnPointEvaluations.Add(faction.Name, new Dictionary<Region, int>());
						this.RegionSpawnPoints.Add(faction.Name, new Dictionary<Region, HexPos>());
						this.OrderedBySpawnValue.Add(faction.Name, new List<Region>());
					}
					foreach (Region region2 in list2)
					{
						GridBasedGraph gridBasedGraph = new GridBasedGraph(base.Context.Grid, from d in region2.Districts
						where d.Content == District.Contents.Land || d.Content == District.Contents.Coastal || d.Content == District.Contents.Lake
						from h in d
						select h);
						if (gridBasedGraph.Hexes.All((HexPos h) => base.Context.POIValidityMap[h.Row, h.Column] != WorldGeneratorContext.POIValidity.Free))
						{
							base.Trace(string.Format("Unusable region {0} for faction {1}", region2.Id, faction.Name));
						}
						foreach (HexPos hexPos in gridBasedGraph.Hexes)
						{
							if (base.Context.POIValidityMap[hexPos.Row, hexPos.Column] == WorldGeneratorContext.POIValidity.Free)
							{
								DiskPicker<HexPos> diskPicker = new DiskPicker<HexPos>(gridBasedGraph);
								diskPicker.Center = hexPos;
								diskPicker.Radius = 2;
								diskPicker.Execute();
								int num = 0;
								using (HashSet<HexPos>.Enumerator enumerator4 = diskPicker.DiskNodes.GetEnumerator())
								{
									while (enumerator4.MoveNext())
									{
										HexPos ringHex = enumerator4.Current;
										if (base.Context.Rivers.Any((River r) => r.Hexes.Contains(ringHex)))
										{
											int num2 = 0;
											if (faction.Preferences.ContainsKey("River"))
											{
												num2 = faction.Preferences["River"];
											}
											num += num2;
										}
										if (faction.Preferences.ContainsKey(base.Context.GetTerrain(ringHex).Name))
										{
											num += faction.Preferences[base.Context.GetTerrain(ringHex).Name];
										}
										if (base.Context.AnomalyMap != null && base.Context.AnomalyMap[ringHex.Row, ringHex.Column] != null && faction.Preferences.ContainsKey(base.Context.AnomalyMap[ringHex.Row, ringHex.Column]))
										{
											num += faction.Preferences[base.Context.AnomalyMap[ringHex.Row, ringHex.Column]];
										}
									}
								}
								this.Evaluations[faction.Name][hexPos.Row, hexPos.Column] = num;
								if (!this.RegionSpawnPointEvaluations[faction.Name].ContainsKey(region2))
								{
									this.RegionSpawnPointEvaluations[faction.Name].Add(region2, num);
									this.RegionSpawnPoints[faction.Name].Add(region2, hexPos);
								}
								else if (this.RegionSpawnPointEvaluations[faction.Name][region2] < num)
								{
									this.RegionSpawnPointEvaluations[faction.Name][region2] = num;
									this.RegionSpawnPoints[faction.Name][region2] = hexPos;
								}
							}
						}
					}
					this.OrderedBySpawnValue[faction.Name].AddRange(from r in this.RegionSpawnPointEvaluations[faction.Name].Keys
					orderby this.RegionSpawnPointEvaluations[faction.Name][r]
					select r);
				}
			}
			List<int> list3 = new List<int>();
			Queue<int> queue = new Queue<int>();
			Queue<int> queue2 = new Queue<int>();
			for (int i = 0; i < base.Context.EmpiresCount; i++)
			{
				list3.Add(i);
			}
			while (list3.Count > 0)
			{
				int num3 = base.Context.Randomizer.Next(list3.Count);
				WorldGeneratorSettings.Faction faction2 = base.Context.GetFaction(base.Context.Configuration.Empires[num3]);
				if (hashSet2.Contains(faction2.Name))
				{
					queue2.Enqueue(list3[num3]);
				}
				else
				{
					queue.Enqueue(list3[num3]);
				}
				list3.RemoveAt(num3);
			}
			HashSet<Region> alreadySpawned = new HashSet<Region>();
			base.Context.SpawnEmpirePreferences = new List<HexPos>[base.Context.EmpiresCount];
			while (queue2.Count > 0)
			{
				int i = queue2.Dequeue();
				this.PickSpawnRegion(list2, mustAcceptIslands, i, alreadySpawned);
			}
			while (queue.Count > 0)
			{
				int i = queue.Dequeue();
				this.PickSpawnRegion(list2, mustAcceptIslands, i, alreadySpawned);
			}
			base.Silent = false;
			for (int i = 0; i < base.Context.EmpiresCount; i++)
			{
				base.Trace(string.Format("{0} {3} : r{1} at {2} - continent {4}", new object[]
				{
					i,
					base.Context.SpawnRegions[i],
					base.Context.SpawnPointsDefault[i].ToString(),
					base.Context.Configuration.Empires[i].Name,
					base.Context.Regions[base.Context.SpawnRegions[i]].LandMassIndex
				}));
			}
			base.Silent = true;
		}

		private void PickSpawnRegion(List<Region> landRegions, bool mustAcceptIslands, int empireIndex, HashSet<Region> alreadySpawned)
		{
			WorldGeneratorSettings.Faction faction = base.Context.GetFaction(base.Context.Configuration.Empires[empireIndex]);
			HashSet<int> allowedContinents = this.AllowedContinents[empireIndex];
			base.Silent = false;
			base.Trace(string.Format("Empire : {0} - Index {2} - Faction : {1}", base.Context.Configuration.Empires[empireIndex].Name, faction.Name, empireIndex));
			base.Silent = true;
			base.Context.SpawnEmpirePreferences[empireIndex] = new List<HexPos>(from r in this.RegionSpawnPointEvaluations[faction.Name].Keys
			where !r.IsIsland | mustAcceptIslands
			where allowedContinents.Contains(r.LandMassIndex)
			orderby this.OverallRegionEvaluation(faction, r) descending
			select this.RegionSpawnPoints[faction.Name][r]);
			List<Region> list = new List<Region>(from r in this.RegionSpawnPointEvaluations[faction.Name].Keys
			where !alreadySpawned.Contains(r)
			where !r.IsIsland | mustAcceptIslands
			where allowedContinents.Contains(r.LandMassIndex)
			orderby this.OverallRegionEvaluation(faction, r) descending
			select r);
			if (list.Count < 1)
			{
				base.Trace("Unable to spawn one empire on its allowed continents, trying on any continent");
				list = new List<Region>(from r in this.RegionSpawnPointEvaluations[faction.Name].Keys
				where !alreadySpawned.Contains(r)
				where !r.IsIsland | mustAcceptIslands
				orderby this.OverallRegionEvaluation(faction, r) descending
				select r);
			}
			if (list.Count > 0)
			{
				Region region5 = list.First<Region>();
				if (base.Context.Configuration.Empires[empireIndex].Name == "AffinityFlames")
				{
					if (list.Any((Region region) => region.Biome.IsVolcanic))
					{
						region5 = list.First((Region region) => region.Biome.IsVolcanic);
					}
				}
				if (base.Context.Configuration.Empires[empireIndex].Name != "AffinityFlames" && base.Context.Configuration.Empires[empireIndex].Name != "AffinityBrokenLords")
				{
					if (list.Any((Region region) => !region.Biome.IsVolcanic))
					{
						region5 = list.First((Region region) => !region.Biome.IsVolcanic);
					}
				}
				alreadySpawned.Add(region5);
				base.Context.SpawnRegions[empireIndex] = region5.Id;
				base.Context.SpawnPointsDefault[empireIndex] = this.RegionSpawnPoints[faction.Name][region5];
				HexPos hexPos = base.Context.SpawnPointsDefault[empireIndex];
				base.Context.POIValidityMap[hexPos.Row, hexPos.Column] = WorldGeneratorContext.POIValidity.Impossible;
				ProximityComputer<Region> proximityComputer = new ProximityComputer<Region>(new AdHocGraph<Region>(landRegions))
				{
					StartingNodes = new List<Region>(alreadySpawned)
				};
				proximityComputer.Execute();
				Func<Region, bool> <>9__16;
				Func<Region, bool> <>9__17;
				foreach (Region region2 in landRegions)
				{
					if (!this.RegionTacticalValues.ContainsKey(region2))
					{
						this.RegionTacticalValues.Add(region2, 0);
					}
					this.RegionTacticalValues[region2] = 0;
					if (proximityComputer.StartingNodes.Count > 0)
					{
						int num = proximityComputer.ProximityGraph[proximityComputer.Graph.DataIndex(region2)];
						if (num > 0)
						{
							Dictionary<Region, int> regionTacticalValues = this.RegionTacticalValues;
							Region region3 = region2;
							regionTacticalValues[region3] += 5 * num;
						}
						else
						{
							Dictionary<Region, int> regionTacticalValues = this.RegionTacticalValues;
							Region region3 = region2;
							regionTacticalValues[region3] += 1000;
						}
					}
					foreach (Region region4 in new List<Region>(from r in region2.Neighbours
					where r.LandMassType == Region.LandMassTypes.Continent
					select r))
					{
						if (alreadySpawned.Contains(region4))
						{
							Dictionary<Region, int> regionTacticalValues = this.RegionTacticalValues;
							Region region3 = region2;
							regionTacticalValues[region3] -= 30;
						}
						else
						{
							IEnumerable<Region> neighbours = region4.Neighbours;
							Func<Region, bool> predicate;
							if ((predicate = <>9__16) == null)
							{
								predicate = (<>9__16 = ((Region nn) => !alreadySpawned.Contains(nn)));
							}
							if (neighbours.All(predicate))
							{
								Dictionary<Region, int> regionTacticalValues = this.RegionTacticalValues;
								Region region3 = region2;
								regionTacticalValues[region3] += 10;
							}
							else
							{
								Dictionary<Region, int> regionTacticalValues = this.RegionTacticalValues;
								Region region3 = region2;
								Dictionary<Region, int> dictionary = regionTacticalValues;
								Region key = region3;
								int num2 = regionTacticalValues[region3];
								int num3 = 5;
								IEnumerable<Region> neighbours2 = region4.Neighbours;
								Func<Region, bool> predicate2;
								if ((predicate2 = <>9__17) == null)
								{
									predicate2 = (<>9__17 = ((Region nn) => alreadySpawned.Contains(nn)));
								}
								dictionary[key] = num2 + num3 / neighbours2.Count(predicate2);
							}
						}
					}
				}
				this.OrderedByTactical = new List<Region>(from r in this.RegionTacticalValues.Keys
				orderby this.RegionTacticalValues[r]
				select r);
			}
		}

		private Dictionary<string, int[,]> Evaluations;

		private Dictionary<string, Dictionary<Region, int>> RegionSpawnPointEvaluations;

		private Dictionary<string, List<Region>> OrderedBySpawnValue;

		private Dictionary<Region, int> RegionIntrinsicValues;

		private List<Region> OrderedByIntrinsic;

		private Dictionary<Region, int> RegionTacticalValues;

		private List<Region> OrderedByTactical;

		private Dictionary<string, Dictionary<Region, HexPos>> RegionSpawnPoints;

		private Dictionary<int, HashSet<int>> AllowedContinents;
	}
}
