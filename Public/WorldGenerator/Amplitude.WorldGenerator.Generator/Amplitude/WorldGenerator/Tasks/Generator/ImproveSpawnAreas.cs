using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude.WorldGenerator.Graphs;
using Amplitude.WorldGenerator.Graphs.Hex;
using Amplitude.WorldGenerator.World;
using Amplitude.WorldGenerator.World.Info;

namespace Amplitude.WorldGenerator.Tasks.Generator
{
	public class ImproveSpawnAreas : WorldGeneratorTask
	{
		public override void Execute(object context)
		{
			base.Execute(context);
			for (int i = 0; i < base.Context.EmpiresCount; i++)
			{
				short key = base.Context.SpawnRegions[i];
				Region region = base.Context.Regions[key];
				HexPos center = base.Context.SpawnPointsDefault[i];
				WorldGeneratorSettings.Faction faction = base.Context.GetFaction(base.Context.Configuration.Empires[i]);
				Dictionary<string, HashSet<HexPos>> dictionary = new Dictionary<string, HashSet<HexPos>>();
				DiskPicker<HexPos> diskPicker = new DiskPicker<HexPos>(new GridBasedGraph(base.Context.Grid, region.Hexes))
				{
					Center = center,
					Radius = 2
				};
				diskPicker.Execute();
				FIDS a = default(FIDS);
				foreach (HexPos hex in diskPicker.DiskNodes)
				{
					a += base.Context.GetTerrainFIDS(hex);
					a += base.Context.GetAnomalyFIDS(hex);
				}
				FIDS b = default(FIDS);
				if (faction != null)
				{
					if (base.Context.Settings.FactionFIDS.ContainsKey(faction.Name))
					{
						b = base.Context.Settings.FactionFIDS[faction.Name];
					}
				}
				else if (base.Context.Settings.FactionFIDS.ContainsKey("Default"))
				{
					b = base.Context.Settings.FactionFIDS["Default"];
				}
				a -= b;
				foreach (HexPos hexPos in diskPicker.DiskNodes)
				{
					if (base.Context.POIValidityMap[hexPos.Row, hexPos.Column] == WorldGeneratorContext.POIValidity.Free)
					{
						string name = base.Context.GetTerrain(hexPos).Name;
						if (base.Context.Settings.AnomalyWeightsPerTerrain.ContainsKey(name))
						{
							foreach (string text in base.Context.Settings.AnomalyWeightsPerTerrain[name].Keys)
							{
								if (!base.Context.Settings.UniqueAnomaliesQuantities.ContainsKey(text) && base.Context.Settings.AnomalyFIDS.ContainsKey(text) && (!base.Context.HasRiver[hexPos.Row, hexPos.Column] || !base.Context.Settings.NoRiverHexAnomalies.Contains(text)))
								{
									if (!dictionary.ContainsKey(text))
									{
										dictionary.Add(text, new HashSet<HexPos>());
									}
									dictionary[text].Add(hexPos);
								}
							}
						}
					}
				}
				int negativity = a.Negativity;
				bool flag = base.Context.Settings.GlobalAnomalyMultiplier > 0;
				while (a.HasAnyNegative && flag)
				{
					string text2 = null;
					int num = a.Negativity;
					foreach (string text3 in dictionary.Keys)
					{
						if (dictionary[text3].Count > 0)
						{
							int negativity2 = (a + base.Context.Settings.AnomalyFIDS[text3]).Negativity;
							if (num > negativity2)
							{
								text2 = text3;
								num = negativity2;
							}
						}
					}
					if (text2 != null)
					{
						HexPos item = dictionary[text2].ToList<HexPos>().ElementAt(base.Context.Randomizer.Next(dictionary[text2].Count));
						if (!base.Context.Anomalies.ContainsKey(text2))
						{
							base.Context.Anomalies.Add(text2, new List<HexPos>());
						}
						base.Context.Anomalies[text2].Add(item);
						base.Context.AnomalyMap[item.Row, item.Column] = text2;
						base.Context.POIValidityMap[item.Row, item.Column] = WorldGeneratorContext.POIValidity.Impossible;
						a += base.Context.Settings.AnomalyFIDS[text2];
						using (Dictionary<string, HashSet<HexPos>>.KeyCollection.Enumerator enumerator3 = dictionary.Keys.GetEnumerator())
						{
							while (enumerator3.MoveNext())
							{
								string key2 = enumerator3.Current;
								if (dictionary[key2].Contains(item))
								{
									dictionary[key2].Remove(item);
								}
							}
							continue;
						}
					}
					flag = false;
					base.Trace("Unable to improve spawn");
				}
			}
		}
	}
}
