using System;
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
			this.OrderedByTactical = new List<List<Region>>();
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
				Region region2 = region;
				if (base.Context.Settings.XephiWorldGeneratorBalance)
				{
					int num = 0;
					int num2 = 0;
					int num3 = 0;
					int num4 = 0;
					int num5 = 0;
					int num6 = 0;
					foreach (PointOfInterestDefinition pointOfInterestDefinition in region.Resources)
					{
						if (pointOfInterestDefinition.TemplateName == "ResourceDeposit_Strategic1" || pointOfInterestDefinition.TemplateName == "ResourceDeposit_Strategic2")
						{
							num++;
						}
						else if (pointOfInterestDefinition.TemplateName == "ResourceDeposit_Luxury1" || pointOfInterestDefinition.TemplateName == "ResourceDeposit_Luxury2" || pointOfInterestDefinition.TemplateName == "ResourceDeposit_Luxury3" || pointOfInterestDefinition.TemplateName == "ResourceDeposit_Luxury4" || pointOfInterestDefinition.TemplateName == "ResourceDeposit_Luxury5")
						{
							num4++;
						}
						else if (pointOfInterestDefinition.TemplateName == "ResourceDeposit_Strategic3" || pointOfInterestDefinition.TemplateName == "ResourceDeposit_Strategic4")
						{
							num2++;
						}
						else if (pointOfInterestDefinition.TemplateName == "ResourceDeposit_Luxury6" || pointOfInterestDefinition.TemplateName == "ResourceDeposit_Luxury7" || pointOfInterestDefinition.TemplateName == "ResourceDeposit_Luxury8" || pointOfInterestDefinition.TemplateName == "ResourceDeposit_Luxury9" || pointOfInterestDefinition.TemplateName == "ResourceDeposit_Luxury10")
						{
							num5++;
						}
						else if (pointOfInterestDefinition.TemplateName == "ResourceDeposit_Strategic5" || pointOfInterestDefinition.TemplateName == "ResourceDeposit_Strategic6")
						{
							num3++;
						}
						else
						{
							num6++;
						}
					}
					Dictionary<Region, int> dictionary = regionIntrinsicValues;
					Region region3 = region2;
					Region region4 = region3;
					Dictionary<Region, int> dictionary2 = dictionary;
					Region region5 = region4;
					Dictionary<Region, int> dictionary3 = dictionary2;
					Region key = region5;
					dictionary3[key] += 4 * region.DeepHexes.Count + 20 * num + 10 * num2 + 5 * num3 + 12 * num4 + 7 * num5 + 2 * num6;
				}
				else
				{
					Dictionary<Region, int> dictionary4 = regionIntrinsicValues;
					Region region6 = region2;
					Region region7 = region6;
					Dictionary<Region, int> dictionary5 = dictionary4;
					Region region8 = region7;
					Dictionary<Region, int> dictionary3 = dictionary5;
					Region key = region8;
					dictionary3[key] += region.HexCount() + 3 * region.DeepHexes.Count + 10 * region.Resources.Count;
				}
				if (region.Neighbours.Any((Region n) => n.LandMassType == Region.LandMassTypes.Ocean))
				{
					regionIntrinsicValues = this.RegionIntrinsicValues;
					region2 = region;
					Dictionary<Region, int> dictionary6 = regionIntrinsicValues;
					Region region9 = region2;
					Region region10 = region9;
					Dictionary<Region, int> dictionary7 = dictionary6;
					Region region11 = region10;
					Dictionary<Region, int> dictionary3 = dictionary7;
					Region key = region11;
					dictionary3[key] *= 11;
					regionIntrinsicValues = this.RegionIntrinsicValues;
					region2 = region;
					Dictionary<Region, int> dictionary8 = regionIntrinsicValues;
					Region region12 = region2;
					Region region13 = region12;
					dictionary7 = dictionary8;
					Region region14 = region13;
					dictionary3 = dictionary7;
					key = region14;
					dictionary3[key] /= 10;
				}
			}
			this.OrderedByIntrinsic = new List<Region>(from r in this.RegionIntrinsicValues.Keys
			orderby this.RegionIntrinsicValues[r]
			select r);
			using (List<WorldGeneratorSettings.Faction>.Enumerator enumerator3 = list.GetEnumerator())
			{
				while (enumerator3.MoveNext())
				{
					WorldGeneratorSettings.Faction faction = enumerator3.Current;
					if (!this.Evaluations.ContainsKey(faction.Name))
					{
						this.Evaluations.Add(faction.Name, new int[base.Context.Grid.Rows, base.Context.Grid.Columns]);
						this.RegionSpawnPointEvaluations.Add(faction.Name, new Dictionary<Region, int>());
						this.RegionSpawnPoints.Add(faction.Name, new Dictionary<Region, HexPos>());
						this.OrderedBySpawnValue.Add(faction.Name, new List<Region>());
					}
					foreach (Region region15 in list2)
					{
						GridBasedGraph gridBasedGraph = new GridBasedGraph(base.Context.Grid, from d in region15.Districts
						where d.Content == District.Contents.Land || d.Content == District.Contents.Coastal || d.Content == District.Contents.Lake
						from h in d
						select h);
						if (gridBasedGraph.Hexes.All((HexPos h) => base.Context.POIValidityMap[h.Row, h.Column] != WorldGeneratorContext.POIValidity.Free))
						{
							base.Trace(string.Format("Unusable region {0} for faction {1}", region15.Id, faction.Name));
						}
						foreach (HexPos hexPos in gridBasedGraph.Hexes)
						{
							if (base.Context.POIValidityMap[hexPos.Row, hexPos.Column] == WorldGeneratorContext.POIValidity.Free)
							{
								DiskPicker<HexPos> diskPicker = new DiskPicker<HexPos>(gridBasedGraph);
								diskPicker.Center = hexPos;
								diskPicker.Radius = 2;
								diskPicker.Execute();
								int num7 = 0;
								using (HashSet<HexPos>.Enumerator enumerator5 = diskPicker.DiskNodes.GetEnumerator())
								{
									while (enumerator5.MoveNext())
									{
										HexPos ringHex = enumerator5.Current;
										if (base.Context.Rivers.Any((River r) => r.Type == River.RiverType.NormalRiver && r.Hexes.Contains(ringHex)))
										{
											int num8 = 0;
											if (faction.Preferences.ContainsKey("River"))
											{
												num8 = faction.Preferences["River"];
											}
											num7 += num8;
										}
										if (faction.Preferences.ContainsKey(base.Context.GetTerrain(ringHex).Name))
										{
											num7 += faction.Preferences[base.Context.GetTerrain(ringHex).Name];
										}
										if (base.Context.AnomalyMap != null && base.Context.AnomalyMap[ringHex.Row, ringHex.Column] != null && faction.Preferences.ContainsKey(base.Context.AnomalyMap[ringHex.Row, ringHex.Column]))
										{
											num7 += faction.Preferences[base.Context.AnomalyMap[ringHex.Row, ringHex.Column]];
										}
									}
								}
								this.Evaluations[faction.Name][hexPos.Row, hexPos.Column] = num7;
								if (!this.RegionSpawnPointEvaluations[faction.Name].ContainsKey(region15))
								{
									this.RegionSpawnPointEvaluations[faction.Name].Add(region15, num7);
									this.RegionSpawnPoints[faction.Name].Add(region15, hexPos);
								}
								else if (this.RegionSpawnPointEvaluations[faction.Name][region15] < num7)
								{
									this.RegionSpawnPointEvaluations[faction.Name][region15] = num7;
									this.RegionSpawnPoints[faction.Name][region15] = hexPos;
								}
							}
						}
					}
					this.OrderedBySpawnValue[faction.Name].AddRange(from r in this.RegionSpawnPointEvaluations[faction.Name].Keys
					orderby this.RegionSpawnPointEvaluations[faction.Name][r]
					select r);
				}
			}
			if (base.Context.Settings.TeamCount > 0 && base.Context.EmpiresCount % base.Context.Settings.TeamCount == 0)
			{
				this.SetupEmpiresTeamSpawnLocations(list2, mustAcceptIslands, hashSet2);
			}
			else
			{
				List<int> list3 = new List<int>();
				Queue<int> queue = new Queue<int>();
				Queue<int> queue2 = new Queue<int>();
				for (int i = 0; i < base.Context.EmpiresCount; i++)
				{
					list3.Add(i);
				}
				while (list3.Count > 0)
				{
					int num9 = base.Context.Randomizer.Next(list3.Count);
					WorldGeneratorSettings.Faction faction2 = base.Context.GetFaction(base.Context.Configuration.Empires[num9]);
					if (hashSet2.Contains(faction2.Name))
					{
						queue2.Enqueue(list3[num9]);
					}
					else
					{
						queue.Enqueue(list3[num9]);
					}
					list3.RemoveAt(num9);
				}
				HashSet<Region> alreadySpawned = new HashSet<Region>();
				base.Context.SpawnEmpirePreferences = new List<HexPos>[base.Context.EmpiresCount];
				while (queue2.Count > 0)
				{
					int empireIndex = queue2.Dequeue();
					if (this.PickSpawnRegion(mustAcceptIslands, empireIndex, alreadySpawned))
					{
						this.UpdateRegionValues(list2, alreadySpawned);
					}
				}
				while (queue.Count > 0)
				{
					int empireIndex2 = queue.Dequeue();
					if (this.PickSpawnRegion(mustAcceptIslands, empireIndex2, alreadySpawned))
					{
						this.UpdateRegionValues(list2, alreadySpawned);
					}
				}
			}
			base.Silent = false;
			for (int j = 0; j < base.Context.EmpiresCount; j++)
			{
				base.Trace(string.Format("{0} {3} : r{1} at {2} - continent {4}", new object[]
				{
					j,
					base.Context.SpawnRegions[j],
					base.Context.SpawnPointsDefault[j].ToString(),
					base.Context.Configuration.Empires[j].Name,
					base.Context.Regions[base.Context.SpawnRegions[j]].LandMassIndex
				}));
			}
			base.Silent = true;
		}

		private void TryVolcanizeRegion(Region region)
		{
			if (this.randomizer == null)
			{
				this.randomizer = new Random(base.Context.Settings.Seed);
			}
			List<Biome> list = new List<Biome>();
			foreach (Biome biome in base.Context.Settings.Biomes)
			{
				if (!(biome.DLCPrerequisite != string.Empty) || base.Context.Configuration.IsDLCAvailable(biome.DLCPrerequisite))
				{
					list.Add(biome);
				}
			}
			if (region.Climate.HasVolcanicBiome(list))
			{
				region.Biome = region.Climate.GetVolcanicBiome(base.Context.Randomizer, list);
				TerrainTransformation transform = base.Context.Settings.Transformations.Find((TerrainTransformation t) => t.Name == "RidgePresence");
				TerrainTransformation transform2 = base.Context.Settings.Transformations.Find((TerrainTransformation t) => t.Name == "HighMountain");
				TerrainTransformation transform3 = base.Context.Settings.Transformations.Find((TerrainTransformation t) => t.Name == "MediumMountain");
				using (List<District>.Enumerator enumerator2 = region.Districts.GetEnumerator())
				{
					while (enumerator2.MoveNext())
					{
						District district4 = enumerator2.Current;
						if (district4.Content == District.Contents.Coastal)
						{
							district4.Terrain = base.Context.CoastSelectors[(int)region.Biome.Id].RandomSelected;
						}
						else if (district4.Content == District.Contents.Lake)
						{
							district4.Terrain = base.Context.LakeSelectors[(int)region.Biome.Id].RandomSelected;
						}
						else if (district4.Content == District.Contents.Land)
						{
							district4.Terrain = base.Context.LandSelectors[(int)region.Biome.Id].RandomSelected;
						}
						else if (district4.Content == District.Contents.Ocean)
						{
							district4.Terrain = base.Context.OceanSelectors[(int)region.Biome.Id].RandomSelected;
						}
						else if (district4.Content == District.Contents.Ridge)
						{
							district4.Terrain = base.Context.LandSelectors[(int)region.Biome.Id].RandomSelected;
							base.Context.ApplyTransformation(transform, district4);
						}
						if (district4.Elevation >= base.Context.Settings.HighMountainElevation)
						{
							base.Context.ApplyTransformation(transform2, district4);
						}
						else if (district4.Elevation >= base.Context.Settings.MediumMountainElevation)
						{
							base.Context.ApplyTransformation(transform3, district4);
						}
					}
					using (List<District>.Enumerator enumerator3 = region.Districts.GetEnumerator())
					{
						while (enumerator3.MoveNext())
						{
							District district2 = enumerator3.Current;
							foreach (HexPos hexPos in district2)
							{
								base.Context.TerrainData[hexPos.Row, hexPos.Column] = district2.Terrain.Id;
							}
						}
						goto IL_471;
					}
				}
			}
			TerrainTransformation transform4 = base.Context.Settings.Transformations.Find((TerrainTransformation t) => t.Name == "ELCPAshlandsTrans");
			foreach (District district3 in region.Districts)
			{
				base.Context.ApplyTransformation(transform4, district3);
				foreach (HexPos hex2 in district3)
				{
					base.Context.ApplyTransformation(transform4, hex2);
				}
			}
			IL_471:
			base.Trace(string.Format("Adding bonus Resources to {0}", region.Name));
			List<PointOfInterestDefinition> createdPOIs = new List<PointOfInterestDefinition>();
			List<HexPos> list2 = new List<HexPos>(from district in region.Districts
			where district.Content == District.Contents.Land
			from hex in district
			where this.Context.POIValidityMap[hex.Row, hex.Column] == WorldGeneratorContext.POIValidity.Free
			orderby DistributeStrategicResources.Influence.GetMinDistance(hex) descending
			select hex);
			if (list2.Count <= 0)
			{
				return;
			}
			List<PointOfInterestTemplate> list3 = new List<PointOfInterestTemplate>(from poiTemplate in base.Context.Settings.POITemplates.Values
			where poiTemplate.GetPropertyValue("Type") == "ResourceDeposit"
			where poiTemplate.GetPropertyValue("ResourceType") == "Strategic"
			where poiTemplate.HasProperty("StrategicTier")
			where poiTemplate.HasProperty("NormalQuantity")
			where poiTemplate.HasProperty("ResourceName")
			where poiTemplate.GetPropertyValue("StrategicTier") == "Tier1"
			select poiTemplate);
			PointOfInterestTemplate poiTemplate2 = list3[this.randomizer.Next(list3.Count)];
			this.PickResourceSite(poiTemplate2, createdPOIs, list2);
			List<River> list4 = base.Context.Rivers.FindAll((River R) => region.Hexes.Contains(R.StartingHex) && R.Tributaries.Count == 0 && R.FlowsInto == null);
			if (list4 != null)
			{
				base.Trace(string.Format("Transforming {0} Rivers to Lava Flows... ", list4.Count));
				foreach (River river in list4)
				{
					river.Type = River.RiverType.LavaRiver;
				}
			}
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
				DistributeStrategicResources.Influence.AddSpot(hexPos);
				if (pointOfInterestDefinition != null)
				{
					createdPOIs.Add(pointOfInterestDefinition);
					Dictionary<int, int> dictionary = DistributeStrategicResources.CreatedSites[propertyValue];
					int landMassIndex = region.LandMassIndex;
					int num2 = dictionary[landMassIndex];
					dictionary[landMassIndex] = num2 + 1;
					region.Resources.Add(pointOfInterestDefinition);
					flag = true;
				}
			}
			while (!flag && num > 0 && availableSpots.Count > 0);
		}

		private PointOfInterestDefinition MakeNewPOI(HexPos position, PointOfInterestTemplate template, int exclusionRadius = 1)
		{
			if (template == null)
			{
				return null;
			}
			if (!base.Context.Grid.Contains(position))
			{
				return null;
			}
			PointOfInterestDefinition pointOfInterestDefinition = new PointOfInterestDefinition
			{
				Position = position,
				TemplateName = template.Name
			};
			base.Context.POIDefinitions.Add(pointOfInterestDefinition);
			DiskPicker<HexPos> diskPicker = new DiskPicker<HexPos>(base.Context.Grid);
			diskPicker.Center = position;
			diskPicker.Radius = exclusionRadius;
			diskPicker.Execute();
			foreach (HexPos hex in diskPicker.DiskNodes)
			{
				if (base.Context.GetDistrict(hex).Content == District.Contents.Land && base.Context.POIValidityMap[hex.Row, hex.Column] == WorldGeneratorContext.POIValidity.Free)
				{
					base.Context.POIValidityMap[hex.Row, hex.Column] = WorldGeneratorContext.POIValidity.Excluded;
				}
			}
			base.Context.POIValidityMap[position.Row, position.Column] = WorldGeneratorContext.POIValidity.Impossible;
			base.Trace(string.Format("Made POI : {0} in {1}", pointOfInterestDefinition.TemplateName, base.Context.RegionData[pointOfInterestDefinition.Position.Row, pointOfInterestDefinition.Position.Column]));
			return pointOfInterestDefinition;
		}

		private void UpdateRegionValues(List<Region> landRegions, HashSet<Region> alreadySpawned)
		{
			ProximityComputer<Region> proximityComputer = new ProximityComputer<Region>(new AdHocGraph<Region>(landRegions))
			{
				StartingNodes = new List<Region>(alreadySpawned)
			};
			proximityComputer.Execute();
			Func<Region, bool> <>9__2;
			Func<Region, bool> <>9__3;
			foreach (Region region in landRegions)
			{
				if (!this.RegionTacticalValues.ContainsKey(region))
				{
					this.RegionTacticalValues.Add(region, 0);
				}
				this.RegionTacticalValues[region] = 0;
				if (proximityComputer.StartingNodes.Count > 0)
				{
					int num = proximityComputer.ProximityGraph[proximityComputer.Graph.DataIndex(region)];
					if (num > 0)
					{
						Dictionary<Region, int> regionTacticalValues = this.RegionTacticalValues;
						Region region2 = region;
						Dictionary<Region, int> dictionary = regionTacticalValues;
						Region key = region2;
						dictionary[key] += 5 * num;
					}
					else
					{
						Dictionary<Region, int> regionTacticalValues2 = this.RegionTacticalValues;
						Region region3 = region;
						Dictionary<Region, int> dictionary = regionTacticalValues2;
						Region key = region3;
						dictionary[key] += 1000;
					}
				}
				foreach (Region region4 in new List<Region>(from r in region.Neighbours
				where r.LandMassType == Region.LandMassTypes.Continent
				select r))
				{
					if (alreadySpawned.Contains(region4))
					{
						Dictionary<Region, int> regionTacticalValues3 = this.RegionTacticalValues;
						Region region5 = region;
						Dictionary<Region, int> dictionary = regionTacticalValues3;
						Region key = region5;
						dictionary[key] -= 30;
					}
					else
					{
						IEnumerable<Region> neighbours = region4.Neighbours;
						Func<Region, bool> predicate;
						if ((predicate = <>9__2) == null)
						{
							predicate = (<>9__2 = ((Region nn) => !alreadySpawned.Contains(nn)));
						}
						if (neighbours.All(predicate))
						{
							Dictionary<Region, int> regionTacticalValues4 = this.RegionTacticalValues;
							Region region6 = region;
							Dictionary<Region, int> dictionary = regionTacticalValues4;
							Region key = region6;
							dictionary[key] += 10;
						}
						else
						{
							int num2 = this.RegionTacticalValues[region];
							int num3 = 5;
							Dictionary<Region, int> regionTacticalValues5 = this.RegionTacticalValues;
							Region region7 = region;
							int num4 = num2;
							int num5 = num3;
							IEnumerable<Region> neighbours2 = region4.Neighbours;
							Dictionary<Region, int> dictionary2 = regionTacticalValues5;
							Region key2 = region7;
							int num6 = num4;
							int num7 = num5;
							IEnumerable<Region> source = neighbours2;
							Func<Region, bool> predicate2;
							if ((predicate2 = <>9__3) == null)
							{
								predicate2 = (<>9__3 = ((Region nn) => alreadySpawned.Contains(nn)));
							}
							dictionary2[key2] = num6 + num7 / source.Count(predicate2);
						}
					}
				}
			}
			if (this.OrderedByTactical.Count == 0)
			{
				this.OrderedByTactical.Add(new List<Region>());
			}
			this.OrderedByTactical[0] = new List<Region>(from r in this.RegionTacticalValues.Keys
			orderby this.RegionTacticalValues[r]
			select r);
		}

		private float OverallRegionEvaluation(WorldGeneratorSettings.Faction faction, Region region, int empireIndex)
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
				if (base.Context.Settings.TeamCount > 0 && base.Context.EmpiresCount % base.Context.Settings.TeamCount == 0)
				{
					int num3 = base.Context.EmpiresCount / base.Context.Settings.TeamCount;
					int index = empireIndex / num3;
					num += base.Context.Settings.SpawnDesirabilityLocation * (float)this.OrderedByTactical[index].IndexOf(region) / (float)this.OrderedByTactical[index].Count;
				}
				else
				{
					num += base.Context.Settings.SpawnDesirabilityLocation * (float)this.OrderedByTactical[0].IndexOf(region) / (float)this.OrderedByTactical.Count;
				}
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

		private bool PickSpawnRegion(bool mustAcceptIslands, int empireIndex, HashSet<Region> alreadySpawned)
		{
			WorldGeneratorSettings.Faction faction = base.Context.GetFaction(base.Context.Configuration.Empires[empireIndex]);
			HashSet<int> allowedContinents = this.AllowedContinents[empireIndex];
			base.Silent = false;
			base.Trace(string.Format("Empire : {0} - Index {2} - Faction : {1}", base.Context.Configuration.Empires[empireIndex].Name, faction.Name, empireIndex));
			base.Silent = true;
			base.Context.SpawnEmpirePreferences[empireIndex] = new List<HexPos>(from r in this.RegionSpawnPointEvaluations[faction.Name].Keys
			where !r.IsIsland | mustAcceptIslands
			where allowedContinents.Contains(r.LandMassIndex)
			orderby this.OverallRegionEvaluation(faction, r, empireIndex) descending
			select this.RegionSpawnPoints[faction.Name][r]);
			List<Region> list = new List<Region>(from r in this.RegionSpawnPointEvaluations[faction.Name].Keys
			where !alreadySpawned.Contains(r)
			where !r.IsIsland | mustAcceptIslands
			where allowedContinents.Contains(r.LandMassIndex)
			orderby this.OverallRegionEvaluation(faction, r, empireIndex) descending
			select r);
			if (list.Count < 1)
			{
				base.Trace("Unable to spawn one empire on its allowed continents, trying on any continent");
				list = new List<Region>(from r in this.RegionSpawnPointEvaluations[faction.Name].Keys
				where !alreadySpawned.Contains(r)
				where !r.IsIsland | mustAcceptIslands
				orderby this.OverallRegionEvaluation(faction, r, empireIndex) descending
				select r);
			}
			if (list.Count > 0)
			{
				Region region3 = list.First<Region>();
				if (base.Context.Configuration.Empires[empireIndex].Name == "AffinityFlames")
				{
					if (!region3.Biome.IsVolcanic && base.Context.Settings.Volcanize)
					{
						this.TryVolcanizeRegion(region3);
					}
					base.Report(string.Format("{0} is volcanic? {1}", region3.Name, region3.Biome.IsVolcanic));
					if (!region3.Biome.IsVolcanic)
					{
						if (list.Any((Region region) => region.Biome.IsVolcanic))
						{
							region3 = list.First((Region region) => region.Biome.IsVolcanic);
						}
					}
				}
				if (base.Context.Configuration.Empires[empireIndex].Name == "AffinityBrokenLords")
				{
					if (list.Any((Region region) => region.Biome.IsVolcanic || region.Biome.Name.Contains("Desert")))
					{
						region3 = list.First((Region region) => region.Biome.IsVolcanic || region.Biome.Name.Contains("Desert"));
					}
				}
				if (base.Context.Configuration.Empires[empireIndex].Name != "AffinityFlames" && base.Context.Configuration.Empires[empireIndex].Name != "AffinityBrokenLords" && base.Context.Configuration.Empires[empireIndex].Name != "AffinityNecrophages")
				{
					if (list.Any((Region region) => !region.Biome.IsVolcanic))
					{
						region3 = list.First((Region region) => !region.Biome.IsVolcanic);
					}
				}
				if (base.Context.Configuration.Empires[empireIndex].Name == "AffinityCultists" || base.Context.Configuration.Empires[empireIndex].Name == "AffinitySeaDemons" || base.Context.Configuration.Empires[empireIndex].Name == "AffinityMimics")
				{
					foreach (Region region2 in list)
					{
						if (!region2.Biome.IsVolcanic)
						{
							foreach (District district in region2.Districts)
							{
								if (district.Content == District.Contents.Ocean || district.Content == District.Contents.Coastal)
								{
									region3 = region2;
									goto IL_514;
								}
							}
						}
					}
				}
				IL_514:
				alreadySpawned.Add(region3);
				base.Context.SpawnRegions[empireIndex] = region3.Id;
				base.Context.SpawnPointsDefault[empireIndex] = this.RegionSpawnPoints[faction.Name][region3];
				HexPos hexPos = base.Context.SpawnPointsDefault[empireIndex];
				base.Context.POIValidityMap[hexPos.Row, hexPos.Column] = WorldGeneratorContext.POIValidity.Impossible;
				return true;
			}
			return false;
		}

		private void SetupEmpiresTeamSpawnLocations(List<Region> landRegions, bool mustAcceptIslands, HashSet<string> hashSet)
		{
			Queue<int> queue = new Queue<int>();
			List<List<int>> list = new List<List<int>>();
			int num = base.Context.EmpiresCount / base.Context.Settings.TeamCount;
			int num2 = -1;
			for (int i = 0; i < base.Context.EmpiresCount; i++)
			{
				if (i % num == 0)
				{
					num2++;
					list.Add(new List<int>());
				}
				list[num2].Add(i);
			}
			while (list.Count > 0)
			{
				for (int j = 0; j < list.Count; j++)
				{
					if (list[j].Count > 0)
					{
						if (!queue.Contains(list[j][0]))
						{
							queue.Enqueue(list[j][0]);
						}
						list[j].RemoveAt(0);
					}
					else
					{
						list.RemoveAt(j);
						j--;
					}
				}
			}
			HashSet<Region> hashSet2 = new HashSet<Region>();
			base.Context.SpawnEmpirePreferences = new List<HexPos>[base.Context.EmpiresCount];
			while (queue.Count > 0)
			{
				int empireIndex = queue.Dequeue();
				if (this.PickSpawnRegion(mustAcceptIslands, empireIndex, hashSet2))
				{
					List<HashSet<Region>> list2 = new List<HashSet<Region>>();
					for (int k = 0; k < base.Context.Settings.TeamCount; k++)
					{
						list2.Add(new HashSet<Region>());
					}
					foreach (Region region in hashSet2)
					{
						for (int l = 0; l < base.Context.EmpiresCount; l++)
						{
							if (base.Context.SpawnRegions[l] == region.Id)
							{
								int num3 = l / num;
								for (int m = 0; m < list2.Count; m++)
								{
									if (m != num3)
									{
										list2[m].Add(region);
									}
								}
							}
						}
					}
					List<ProximityComputer<Region>> list3 = new List<ProximityComputer<Region>>();
					for (int n = 0; n < list2.Count; n++)
					{
						list3.Add(new ProximityComputer<Region>(new AdHocGraph<Region>(landRegions))
						{
							StartingNodes = new List<Region>(list2[n])
						});
					}
					while (this.OrderedByTactical.Count < list3.Count)
					{
						this.OrderedByTactical.Add(new List<Region>());
					}
					for (int num4 = 0; num4 < base.Context.Settings.TeamCount; num4++)
					{
						List<Region> value = this.OrderedByTactical[num4];
						int num5 = base.Context.EmpiresCount / base.Context.Settings.TeamCount * num4;
						List<Region> list4 = new List<Region>();
						foreach (Region region2 in hashSet2)
						{
							if (region2.Id == base.Context.SpawnRegions[num5])
							{
								list4.Add(region2);
							}
						}
						ProximityComputer<Region> proximityComputerLeader = new ProximityComputer<Region>(new AdHocGraph<Region>(landRegions))
						{
							StartingNodes = list4
						};
						this.UpdateRegionTeamValues(landRegions, hashSet2, list3[num4], proximityComputerLeader, out value);
						this.OrderedByTactical[num4] = value;
					}
				}
			}
		}

		private void UpdateRegionTeamValues(List<Region> landRegions, HashSet<Region> alreadySpawned, ProximityComputer<Region> proximityComputer, ProximityComputer<Region> proximityComputerLeader, out List<Region> regionTacticalOrderList)
		{
			proximityComputer.Execute();
			bool flag = proximityComputerLeader.Execute();
			Func<Region, bool> <>9__2;
			Func<Region, bool> <>9__3;
			foreach (Region region in landRegions)
			{
				if (!this.RegionTacticalValues.ContainsKey(region))
				{
					this.RegionTacticalValues.Add(region, 0);
				}
				this.RegionTacticalValues[region] = 0;
				if (proximityComputer.StartingNodes.Count > 0)
				{
					int num = proximityComputer.ProximityGraph[proximityComputer.Graph.DataIndex(region)];
					if (num > 0)
					{
						Region region2 = region;
						if (flag)
						{
							int num2 = proximityComputerLeader.ProximityGraph[proximityComputerLeader.Graph.DataIndex(region)];
							if (num2 > 0)
							{
								int num3 = 5 * num + (30 - 5 * num2);
								Dictionary<Region, int> regionTacticalValues = this.RegionTacticalValues;
								Region key = region2;
								regionTacticalValues[key] += num3;
							}
							else
							{
								Dictionary<Region, int> regionTacticalValues = this.RegionTacticalValues;
								Region key = region2;
								regionTacticalValues[key] += 5 * num;
							}
						}
						else
						{
							Dictionary<Region, int> regionTacticalValues = this.RegionTacticalValues;
							Region key = region2;
							regionTacticalValues[key] += 5 * num;
						}
					}
					else
					{
						Region region3 = region;
						Dictionary<Region, int> regionTacticalValues = this.RegionTacticalValues;
						Region key = region3;
						regionTacticalValues[key] += 1000;
						base.Report(" key boosted by 1000");
					}
				}
				foreach (Region region4 in new List<Region>(from r in region.Neighbours
				where r.LandMassType == Region.LandMassTypes.Continent
				select r))
				{
					if (alreadySpawned.Contains(region4))
					{
						Region region5 = region;
						Dictionary<Region, int> regionTacticalValues = this.RegionTacticalValues;
						Region key = region5;
						regionTacticalValues[key] -= 30;
					}
					else
					{
						IEnumerable<Region> neighbours = region4.Neighbours;
						Func<Region, bool> predicate;
						if ((predicate = <>9__2) == null)
						{
							predicate = (<>9__2 = ((Region nn) => !alreadySpawned.Contains(nn)));
						}
						if (neighbours.All(predicate))
						{
							Region region6 = region;
							Dictionary<Region, int> regionTacticalValues = this.RegionTacticalValues;
							Region key = region6;
							regionTacticalValues[key] += 10;
						}
						else
						{
							int num4 = this.RegionTacticalValues[region];
							int num5 = 5;
							Region region7 = region;
							int num6 = num4;
							int num7 = num5;
							IEnumerable<Region> neighbours2 = region4.Neighbours;
							Dictionary<Region, int> regionTacticalValues2 = this.RegionTacticalValues;
							Region key2 = region7;
							int num8 = num6;
							int num9 = num7;
							IEnumerable<Region> source = neighbours2;
							Func<Region, bool> predicate2;
							if ((predicate2 = <>9__3) == null)
							{
								predicate2 = (<>9__3 = ((Region nn) => alreadySpawned.Contains(nn)));
							}
							regionTacticalValues2[key2] = num8 + num9 / source.Count(predicate2);
						}
					}
				}
			}
			regionTacticalOrderList = new List<Region>(from r in this.RegionTacticalValues.Keys
			orderby this.RegionTacticalValues[r]
			select r);
		}

		private Dictionary<string, int[,]> Evaluations;

		private Dictionary<string, Dictionary<Region, int>> RegionSpawnPointEvaluations;

		private Dictionary<string, List<Region>> OrderedBySpawnValue;

		private Dictionary<Region, int> RegionIntrinsicValues;

		private List<Region> OrderedByIntrinsic;

		private Dictionary<Region, int> RegionTacticalValues;

		private Dictionary<string, Dictionary<Region, HexPos>> RegionSpawnPoints;

		private Dictionary<int, HashSet<int>> AllowedContinents;

		private Random randomizer;

		private List<List<Region>> OrderedByTactical;
	}
}
