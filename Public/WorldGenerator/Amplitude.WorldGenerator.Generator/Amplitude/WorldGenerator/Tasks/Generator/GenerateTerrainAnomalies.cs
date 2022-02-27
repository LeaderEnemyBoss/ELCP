using System;
using System.Collections.Generic;
using Amplitude.WorldGenerator.Graphs;
using Amplitude.WorldGenerator.Graphs.Hex;
using Amplitude.WorldGenerator.World;
using Amplitude.WorldGenerator.World.Info;

namespace Amplitude.WorldGenerator.Tasks.Generator
{
	public class GenerateTerrainAnomalies : WorldGeneratorTask
	{
		public override void Execute(object context)
		{
			base.Execute(context);
			if (base.Context.Settings.GlobalAnomalyMultiplier <= 0)
			{
				return;
			}
			for (int i = 0; i < base.Context.Grid.Rows; i++)
			{
				for (int j = 0; j < base.Context.Grid.Columns; j++)
				{
					int index = (int)base.Context.TerrainData[i, j];
					Terrain terrain = base.Context.Settings.Terrains[index];
					HexPos item = new HexPos(j, i);
					int num = 0;
					if (base.Context.POIValidityMap[item.Row, item.Column] == WorldGeneratorContext.POIValidity.Free || base.Context.POIValidityMap[item.Row, item.Column] == WorldGeneratorContext.POIValidity.Excluded)
					{
						item.Neighbours.ForEach(delegate(HexPos hex)
						{
							int num2 = hex.Row;
							if (num2 < 0)
							{
								num2 = base.Context.AnomalyMap.GetLength(0) - 1;
							}
							else if (num2 >= base.Context.AnomalyMap.GetLength(0))
							{
								num2 -= num2;
							}
							int num3 = hex.Column;
							if (num3 < 0)
							{
								num3 = base.Context.AnomalyMap.GetLength(1) - 1;
							}
							else if (num3 >= base.Context.AnomalyMap.GetLength(1))
							{
								num3 -= num3;
							}
							if (base.Context.AnomalyMap[num2, num3] != null)
							{
								this.XephiAnyNeighborAnomalies = true;
							}
						});
						if (base.Context.Settings.AnomalyOdds.ContainsKey(terrain.Name))
						{
							num = base.Context.Settings.AnomalyOdds[terrain.Name];
						}
						num *= 10000;
						num /= base.Context.Settings.GlobalAnomalyMultiplier;
						bool flag = base.Context.Randomizer.Next(num) <= 100;
						if ((num > 0 && base.Context.Settings.AnomalyWeightsPerTerrain.ContainsKey(terrain.Name) && flag) || (this.XephiConfirmNextHexAnomaly > 0 && base.Context.Settings.AnomalyWeightsPerTerrain.ContainsKey(terrain.Name)))
						{
							if (flag)
							{
								this.XephiConfirmNextHexAnomaly++;
							}
							if (this.XephiAnyNeighborAnomalies && base.Context.Settings.XephiWorldGeneratorBalance)
							{
								this.XephiAnyNeighborAnomalies = false;
							}
							else
							{
								WeightedRandomSelector<string> weightedRandomSelector = new WeightedRandomSelector<string>();
								weightedRandomSelector.Randomizer = base.Context.Randomizer;
								weightedRandomSelector.UseDictionary(base.Context.Settings.AnomalyWeightsPerTerrain[terrain.Name]);
								string randomSelected = weightedRandomSelector.RandomSelected;
								if (!base.Context.Settings.UniqueAnomaliesQuantities.ContainsKey(randomSelected) && (!base.Context.HasRiver[item.Row, item.Column] || !base.Context.Settings.NoRiverHexAnomalies.Contains(randomSelected)))
								{
									if (!base.Context.Anomalies.ContainsKey(randomSelected))
									{
										base.Context.Anomalies.Add(randomSelected, new List<HexPos>());
									}
									base.Context.Anomalies[randomSelected].Add(item);
									base.Silent = false;
									base.Trace(string.Format("{0} at {1}", randomSelected, item.ToString()));
									base.Silent = true;
									base.Context.AnomalyMap[item.Row, item.Column] = randomSelected;
									base.Context.POIValidityMap[item.Row, item.Column] = WorldGeneratorContext.POIValidity.Impossible;
									this.XephiConfirmNextHexAnomaly--;
									this.XephiAnomalyCountTracker++;
								}
							}
						}
					}
				}
			}
			string[,] array = new string[base.Context.Grid.Rows, base.Context.Grid.Columns];
			foreach (PointOfInterestDefinition pointOfInterestDefinition in base.Context.POIDefinitions)
			{
				array[pointOfInterestDefinition.Position.Row, pointOfInterestDefinition.Position.Column] = pointOfInterestDefinition.TemplateName;
				if (base.Context.AnomalyMap[pointOfInterestDefinition.Position.Row, pointOfInterestDefinition.Position.Column] != null)
				{
					base.Trace(string.Format("Dual setup POI/Anomaly at {0} : {1} & {2}", pointOfInterestDefinition.Position.ToString(), pointOfInterestDefinition.TemplateName, base.Context.AnomalyMap[pointOfInterestDefinition.Position.Row, pointOfInterestDefinition.Position.Column]));
				}
			}
		}

		private bool XephiAnyNeighborAnomalies;

		private int XephiAnomalyCountTracker;

		private int XephiConfirmNextHexAnomaly;
	}
}
