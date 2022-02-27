using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude.WorldGenerator.Graphs;
using Amplitude.WorldGenerator.Graphs.Hex;
using Amplitude.WorldGenerator.Tmx;
using Amplitude.WorldGenerator.World;
using Amplitude.WorldGenerator.World.Info;

namespace Amplitude.WorldGenerator.Tasks.Generator
{
	public class ImportTmxPointOfInterestLayer : WorldGeneratorTask
	{
		public override void Execute(object context)
		{
			base.Report("?ImportTmxPointOfInterestLayer");
			base.Execute(context);
			if (base.Context.Settings.TmxMap == null)
			{
				throw new ArgumentException("[WorldGenerator][Exception] TmxMap can't be null");
			}
			this.tmxPointOfInterestLayer = base.Context.Settings.TmxMap.Layers.FirstOrDefault((Layer layer) => layer.Name == "PointOfInterests");
			if (this.tmxPointOfInterestLayer == null)
			{
				throw new ArgumentException("[WorldGenerator][Exception] TmxLayer PointOfInterests can't be null");
			}
			this.random = new Random(DateTime.Now.GetHashCode());
			this.GetPointOfInterests();
			this.AssignRandomAnomalies_Unique();
			this.AssignRandomAnomalies_Terrain();
			this.CreateRidgesFromHexes();
		}

		private void GetPointOfInterests()
		{
			this.customRandomizationGroups = new Dictionary<int, List<int>>();
			this.randomAnomalyPositions = new List<HexPos>();
			TerrainTransformation transform = base.Context.Settings.Transformations.Find((TerrainTransformation t) => t.Name == "Volcano");
			bool flag = base.Context.Configuration.IsDLCAvailable("SummerFlamesPack");
			bool flag2 = base.Context.Configuration.IsDLCAvailable("NavalPack");
			for (int i = 0; i < base.Context.Grid.Columns; i++)
			{
				for (int j = 0; j < base.Context.Grid.Rows; j++)
				{
					if (this.tmxPointOfInterestLayer.Data[j, i] != 0)
					{
						HexPos hexPos = new HexPos(i, j);
						bool flag3 = false;
						string tileProperty = base.Context.Settings.TmxMap.GetTileProperty(this.tmxPointOfInterestLayer.Data[hexPos.Row, hexPos.Column], "Values");
						string poiName;
						if (!string.IsNullOrEmpty(tileProperty))
						{
							poiName = this.GetPointOfInterest_CustomRandomization(hexPos, tileProperty);
						}
						else
						{
							poiName = base.Context.Settings.TmxMap.GetTileProperty(this.tmxPointOfInterestLayer.Data[j, i], ImportTmxPointOfInterestLayer.poiTypeNameProperty);
						}
						if (string.IsNullOrEmpty(poiName))
						{
							base.ReportTmx(string.Concat(new object[]
							{
								"?MissingPropertyTile&$Coordinate=",
								i,
								",",
								j - base.Context.Grid.Rows + 1,
								"&$MissingProperty=",
								ImportTmxPointOfInterestLayer.poiTypeNameProperty
							}));
						}
						else
						{
							if (poiName.Contains("Facility"))
							{
								if (poiName == ImportTmxPointOfInterestLayer.randomFacilityName)
								{
									string[] array = (from template in base.Context.Settings.POITemplates.Values
									select template.Name into name
									where name.Contains("Facility")
									select name).ToArray<string>();
									poiName = array[this.random.Next(array.Length)];
								}
								bool flag4 = false;
								for (int k = 0; k < 6; k++)
								{
									HexPos hexPos2 = hexPos.Neighbour((HexPos.Direction)k);
									if (hexPos2.Row >= 0 && hexPos2.Row <= base.Context.Settings.Height && hexPos2.Column >= 0 && hexPos2.Column <= base.Context.Settings.Width && this.tmxPointOfInterestLayer.Data[hexPos2.Row, hexPos2.Column] != 0)
									{
										string tileProperty2 = base.Context.Settings.TmxMap.GetTileProperty(this.tmxPointOfInterestLayer.Data[hexPos2.Row, hexPos2.Column], ImportTmxPointOfInterestLayer.poiTypeNameProperty);
										if (!string.IsNullOrEmpty(tileProperty2) && tileProperty2.Contains("Citadel"))
										{
											if (flag4)
											{
												base.ReportTmx(string.Concat(new object[]
												{
													"?CitadelIncorrectPosition&$Coordinate=",
													hexPos2.Column,
													",",
													hexPos2.Row - base.Context.Grid.Rows + 1
												}));
												throw new TmxImportException();
											}
											flag4 = true;
										}
									}
								}
								if (!flag4)
								{
									goto IL_9F7;
								}
								flag3 = true;
							}
							if (poiName.Contains("Citadel"))
							{
								if (poiName == ImportTmxPointOfInterestLayer.randomCitadelName)
								{
									string[] array2 = (from template in base.Context.Settings.POITemplates.Values
									select template.Name into name
									where name.Contains("Citadel")
									select name).ToArray<string>();
									poiName = array2[this.random.Next(array2.Length)];
								}
								flag3 = true;
							}
							flag3 |= poiName.Contains("NavalQuestLocation_SunkenRuin");
							Terrain terrain = base.Context.GetTerrain(hexPos);
							if ((!flag3 && terrain.IsWaterTile) || terrain.Name.Contains("Waste"))
							{
								base.ReportTmx(string.Concat(new object[]
								{
									"?POIIncompatibleTerrain&$Coordinate=",
									hexPos.Column,
									",",
									hexPos.Row - base.Context.Grid.Rows + 1
								}));
							}
							else if (flag3 && !terrain.IsWaterTile)
							{
								base.ReportTmx(string.Concat(new object[]
								{
									"?POIIncompatibleTerrain&$Coordinate=",
									hexPos.Column,
									",",
									hexPos.Row - base.Context.Grid.Rows + 1
								}));
							}
							else if (poiName == "Ridge")
							{
								this.ridgesHexes.Add(hexPos);
							}
							else if (poiName == "Volcano")
							{
								this.ridgesHexes.Add(hexPos);
								if (flag)
								{
									base.Context.ApplyTransformation(transform, hexPos);
								}
							}
							else
							{
								if (poiName.Contains("Anomaly"))
								{
									if (poiName == ImportTmxPointOfInterestLayer.randomAnomalyName)
									{
										this.randomAnomalyPositions.Add(hexPos);
									}
									else if (base.Context.Anomalies.Keys.Contains(poiName))
									{
										if (!base.Context.Anomalies[poiName].Contains(hexPos))
										{
											base.Context.Anomalies[poiName].Add(hexPos);
										}
									}
									else
									{
										base.Context.Anomalies.Add(poiName, new List<HexPos>());
										base.Context.Anomalies[poiName].Add(hexPos);
									}
								}
								if (poiName.Contains("Village"))
								{
									if (poiName == ImportTmxPointOfInterestLayer.randomMinorFactionVillage)
									{
										string[] array3 = (from template in base.Context.Settings.POITemplates.Values
										select template.Name into name
										where name.Contains("Village_")
										select name).ToArray<string>();
										poiName = array3[this.random.Next(array3.Length)];
									}
									short key = base.Context.RegionData[j, i];
									Region region = base.Context.Regions[key];
									if (region == null || poiName.Split(new char[]
									{
										'_'
									}).Count<string>() < 2)
									{
										goto IL_9F7;
									}
									Region region2 = region;
									int villages = region2.Villages;
									region2.Villages = villages + 1;
									if (string.IsNullOrEmpty(region.MinorFactionName))
									{
										region.MinorFactionName = poiName.Split(new char[]
										{
											'_'
										})[1];
									}
									else
									{
										poiName = "Village_" + region.MinorFactionName;
									}
								}
								if (poiName == ImportTmxPointOfInterestLayer.randomStrategicResourceDepositName)
								{
									string[] array4 = (from template in base.Context.Settings.POITemplates.Values
									select template.Name into name
									where name.Contains("ResourceDeposit_Strategic")
									select name).ToArray<string>();
									poiName = array4[this.random.Next(array4.Length)];
								}
								if (poiName == ImportTmxPointOfInterestLayer.randomLuxuryResourceDepositName)
								{
									string[] array5 = (from template in base.Context.Settings.POITemplates.Values
									select template.Name into name
									where name.Contains("ResourceDeposit_Luxury")
									select name).ToArray<string>();
									poiName = array5[this.random.Next(array5.Length)];
								}
								PointOfInterestTemplate pointOfInterestTemplate = base.Context.Settings.POITemplates.Values.FirstOrDefault((PointOfInterestTemplate template) => template.Name == poiName);
								if (!flag2 && (poiName.Contains("Facility") || poiName.Contains("Citadel") || poiName.Contains("NavalQuestLocation_SunkenRuin")))
								{
									pointOfInterestTemplate = null;
								}
								if (pointOfInterestTemplate != null)
								{
									if (poiName.Contains("ResourceDeposit"))
									{
										short key2 = base.Context.RegionData[j, i];
										Region region3 = base.Context.Regions[key2];
										if (region3 != null)
										{
											region3.Resources.Add(new PointOfInterestDefinition
											{
												Position = new HexPos(i, j),
												TemplateName = pointOfInterestTemplate.Name
											});
										}
									}
									base.Context.POIDefinitions.Add(new PointOfInterestDefinition
									{
										Position = new HexPos(i, j),
										TemplateName = pointOfInterestTemplate.Name
									});
								}
							}
						}
					}
					IL_9F7:;
				}
			}
			this.customRandomizationGroups.Clear();
		}

		private void CreateRidgesFromHexes()
		{
			while (this.ridgesHexes.Count > 0)
			{
				Ridge ridge = new Ridge();
				ridge.Hexes.Add(this.ridgesHexes[0]);
				this.ridgesHexes.RemoveAt(0);
				Queue<HexPos> queue = new Queue<HexPos>();
				queue.Enqueue(ridge.Hexes[0]);
				while (queue.Count > 0)
				{
					HexPos pos = queue.Dequeue();
					foreach (HexPos item in base.Context.Grid.Adjacents(pos))
					{
						int num = this.tmxPointOfInterestLayer.Data[item.Row, item.Column];
						if (num != 0 && base.Context.Settings.TmxMap.GetTileProperty(num, ImportTmxPointOfInterestLayer.poiTypeNameProperty) == "Ridge" && !ridge.Hexes.Contains(item))
						{
							ridge.Hexes.Add(item);
							this.ridgesHexes.Remove(item);
							queue.Enqueue(item);
						}
					}
				}
				base.Context.Ridges.Add(ridge);
			}
		}

		private void CheckMinimumStrategicDepositPerRegion()
		{
			foreach (Region region in base.Context.Regions.Values)
			{
				if (region.LandMassType != Region.LandMassTypes.Ocean)
				{
					if (region.Resources.Count((PointOfInterestDefinition resource) => resource.TemplateName.Contains("ResourceDeposit_Strategic")) < 6)
					{
						base.ReportTmx("?MinimumStrategicDeposit&$RegionName=" + region.Name);
					}
				}
			}
		}

		private string GetPointOfInterest_CustomRandomization(HexPos hexPos, string values)
		{
			List<string> list = values.Split(new char[]
			{
				',',
				';'
			}, StringSplitOptions.RemoveEmptyEntries).ToList<string>();
			int num;
			if (!int.TryParse(base.Context.Settings.TmxMap.GetTileProperty(this.tmxPointOfInterestLayer.Data[hexPos.Row, hexPos.Column], "Group"), out num) || num < 0)
			{
				return list[base.Context.Randomizer.Next(list.Count)].Trim();
			}
			if (!this.customRandomizationGroups.ContainsKey(num))
			{
				List<int> list2 = Enumerable.Range(0, list.Count).ToList<int>();
				list2 = (from x in list2
				orderby base.Context.Randomizer.Next()
				select x).ToList<int>();
				this.customRandomizationGroups.Add(num, list2);
			}
			int index;
			int.TryParse(base.Context.Settings.TmxMap.GetTileProperty(this.tmxPointOfInterestLayer.Data[hexPos.Row, hexPos.Column], "Offset"), out index);
			int index2 = this.customRandomizationGroups[num][index] % list.Count;
			return list[index2].Trim();
		}

		private void AssignRandomAnomalies_Unique()
		{
			Dictionary<HexPos, int> weights = new Dictionary<HexPos, int>();
			this.randomAnomalyPositions.ForEach(delegate(HexPos h)
			{
				weights.Add(h, 0);
			});
			WeightedRandomSelector<HexPos> weightedRandomSelector = new WeightedRandomSelector<HexPos>
			{
				Randomizer = base.Context.Randomizer
			};
			foreach (string text in base.Context.Settings.UniqueAnomaliesQuantities.Keys)
			{
				int num = 0;
				if (base.Context.Anomalies.ContainsKey(text))
				{
					num = base.Context.Anomalies[text].Count;
				}
				if (base.Context.Settings.UniqueAnomaliesQuantities[text] > num)
				{
					foreach (HexPos hexPos in this.randomAnomalyPositions)
					{
						string name = base.Context.GetTerrain(hexPos).Name;
						weights[hexPos] = 0;
						if (base.Context.Settings.AnomalyWeightsPerTerrain.ContainsKey(name) && base.Context.Settings.AnomalyWeightsPerTerrain[name].ContainsKey(text))
						{
							weights[hexPos] = base.Context.Settings.AnomalyWeightsPerTerrain[name][text];
						}
					}
					weightedRandomSelector.UseDictionary(weights);
					int num2 = base.Context.Settings.UniqueAnomaliesQuantities[text] - num;
					if (weightedRandomSelector.IsValid && num2 > 0)
					{
						for (int i = 0; i < num2; i++)
						{
							if (weightedRandomSelector.IsValid)
							{
								HexPos randomSelected = weightedRandomSelector.RandomSelected;
								weights[randomSelected] = 0;
								weightedRandomSelector.UseDictionary(weights);
								if (!base.Context.Anomalies.ContainsKey(text))
								{
									base.Context.Anomalies.Add(text, new List<HexPos>());
								}
								base.Context.Anomalies[text].Add(randomSelected);
								base.Context.AnomalyMap[randomSelected.Row, randomSelected.Column] = text;
								this.randomAnomalyPositions.Remove(randomSelected);
							}
						}
					}
				}
			}
		}

		private void AssignRandomAnomalies_Terrain()
		{
			foreach (HexPos hexPos in this.randomAnomalyPositions)
			{
				Terrain terrain = base.Context.GetTerrain(hexPos);
				WeightedRandomSelector<string> weightedRandomSelector = new WeightedRandomSelector<string>();
				weightedRandomSelector.Randomizer = base.Context.Randomizer;
				Dictionary<string, int> dictionary = new Dictionary<string, int>(base.Context.Settings.AnomalyWeightsPerTerrain[terrain.Name]);
				foreach (string key in base.Context.Settings.UniqueAnomaliesQuantities.Keys)
				{
					dictionary.Remove(key);
				}
				if (dictionary.Count != 0)
				{
					weightedRandomSelector.UseDictionary(dictionary);
					string randomSelected = weightedRandomSelector.RandomSelected;
					if (!base.Context.Settings.UniqueAnomaliesQuantities.ContainsKey(randomSelected))
					{
						if (!base.Context.Anomalies.ContainsKey(randomSelected))
						{
							base.Context.Anomalies.Add(randomSelected, new List<HexPos>());
						}
						base.Context.Anomalies[randomSelected].Add(hexPos);
						base.Context.AnomalyMap[hexPos.Row, hexPos.Column] = randomSelected;
					}
				}
			}
			this.randomAnomalyPositions.Clear();
		}

		private static string poiTypeNameProperty = "Value";

		private static string randomAnomalyName = "AnomalyRandom";

		private static string randomStrategicResourceDepositName = "ResourceDeposit_StrategicRandom";

		private static string randomLuxuryResourceDepositName = "ResourceDeposit_LuxuryRandom";

		private static string randomMinorFactionVillage = "Village_Random";

		private static string randomFacilityName = "Facility_Random";

		private static string randomCitadelName = "Citadel_Random";

		private Layer tmxPointOfInterestLayer;

		private List<HexPos> ridgesHexes = new List<HexPos>();

		private Random random;

		private Dictionary<int, List<int>> customRandomizationGroups;

		private List<HexPos> randomAnomalyPositions;
	}
}
