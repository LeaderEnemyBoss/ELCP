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
				Diagnostics.Log("ELCP====================================================================================");
				Diagnostics.Log(string.Format("ELCP: Empire {2} {0}, spawn {1}", base.Context.Configuration.Empires[i].Name, base.Context.SpawnPointsDefault[i], i));
				short key = base.Context.SpawnRegions[i];
				Region region = base.Context.Regions[key];
				HexPos center = base.Context.SpawnPointsDefault[i];
				Dictionary<string, HashSet<HexPos>> dictionary = new Dictionary<string, HashSet<HexPos>>();
				DiskPicker<HexPos> diskPicker = new DiskPicker<HexPos>(new GridBasedGraph(base.Context.Grid, region.Hexes))
				{
					Center = center,
					Radius = 2
				};
				diskPicker.Execute();
				FIDS fids = default(FIDS);
				foreach (HexPos hex in diskPicker.DiskNodes)
				{
					fids += base.Context.GetTerrainFIDS(hex);
					fids += base.Context.GetAnomalyFIDS(hex);
				}
				FIDS fids2 = default(FIDS);
				if (base.Context.Settings.FactionFIDS.ContainsKey(base.Context.Configuration.Empires[i].Name))
				{
					fids2 = base.Context.Settings.FactionFIDS[base.Context.Configuration.Empires[i].Name];
				}
				else if (base.Context.Settings.FactionFIDS.ContainsKey("Default"))
				{
					fids2 = base.Context.Settings.FactionFIDS["Default"];
				}
				this.ManageSpecialFactions(i, region.Biome.IsVolcanic, ref fids, ref fids2);
				Diagnostics.Log(string.Format("ELCP: base FIDS at location {0}, minus min FIDS of {1} = {2}", fids, fids2, fids - fids2));
				int totalValue = fids2.GetTotalValue();
				fids -= fids2;
				bool flag = false;
				foreach (HexPos hexPos in diskPicker.DiskNodes)
				{
					if (!flag)
					{
						string text = base.Context.AnomalyMap[hexPos.Row, hexPos.Column];
						if (text != null && base.Context.Settings.AnomalyFIDS.ContainsKey(text))
						{
							flag = true;
						}
					}
					if (base.Context.POIValidityMap[hexPos.Row, hexPos.Column] == WorldGeneratorContext.POIValidity.Free || base.Context.POIValidityMap[hexPos.Row, hexPos.Column] == WorldGeneratorContext.POIValidity.Excluded)
					{
						string name = base.Context.GetTerrain(hexPos).Name;
						if (base.Context.Settings.AnomalyWeightsPerTerrain.ContainsKey(name))
						{
							foreach (string text2 in base.Context.Settings.AnomalyWeightsPerTerrain[name].Keys)
							{
								if (!base.Context.Settings.UniqueAnomaliesQuantities.ContainsKey(text2) && base.Context.Settings.AnomalyFIDS.ContainsKey(text2) && (!base.Context.HasRiver[hexPos.Row, hexPos.Column] || !base.Context.Settings.NoRiverHexAnomalies.Contains(text2)))
								{
									if (!dictionary.ContainsKey(text2))
									{
										dictionary.Add(text2, new HashSet<HexPos>());
									}
									dictionary[text2].Add(hexPos);
								}
							}
						}
					}
				}
				if (!flag && !fids.HasAnyNegative)
				{
					Diagnostics.Log(string.Format("ELCP: !!! no Anomaly around, spawning at least one !!!", Array.Empty<object>()));
					while (!fids.HasAnyNegative)
					{
						fids.Minus(1);
						this.ManageSpecialFactions(i, region.Biome.IsVolcanic, ref fids, ref fids2);
					}
				}
				bool flag2 = base.Context.Settings.GlobalAnomalyMultiplier > 0;
				while (fids.HasAnyNegative && flag2)
				{
					Diagnostics.Log(string.Format("ELCP: starting improvement pass at {0}", fids));
					if (fids.GetTotalValue() - fids.Negativity > totalValue && flag)
					{
						Diagnostics.Log("ELCP: --- Aborting due to too high FISD ---");
						break;
					}
					string text3 = null;
					int num = fids.Negativity;
					foreach (string text4 in dictionary.Keys)
					{
						if (dictionary[text4].Count > 0)
						{
							int negativity = (fids + base.Context.Settings.AnomalyFIDS[text4]).Negativity;
							if (num > negativity)
							{
								text3 = text4;
								num = negativity;
							}
						}
					}
					if (text3 != null)
					{
						HexPos hexPos2 = dictionary[text3].ToList<HexPos>().ElementAt(0);
						if (!base.Context.Anomalies.ContainsKey(text3))
						{
							base.Context.Anomalies.Add(text3, new List<HexPos>());
						}
						Diagnostics.Log(string.Format("ELCP: adding {0} to {1}", text3, hexPos2));
						base.Context.Anomalies[text3].Add(hexPos2);
						base.Context.AnomalyMap[hexPos2.Row, hexPos2.Column] = text3;
						base.Context.POIValidityMap[hexPos2.Row, hexPos2.Column] = WorldGeneratorContext.POIValidity.Impossible;
						fids += base.Context.Settings.AnomalyFIDS[text3];
						this.ManageSpecialFactions(i, region.Biome.IsVolcanic, ref fids, ref fids2);
						flag = true;
						Diagnostics.Log(string.Format("ELCP: fisd is now {0}, negative {1}", fids, fids.HasAnyNegative));
						using (Dictionary<string, HashSet<HexPos>>.KeyCollection.Enumerator enumerator4 = dictionary.Keys.GetEnumerator())
						{
							while (enumerator4.MoveNext())
							{
								string key2 = enumerator4.Current;
								if (dictionary[key2].Contains(hexPos2))
								{
									dictionary[key2].Remove(hexPos2);
								}
							}
							continue;
						}
					}
					flag2 = false;
					base.Trace("Unable to improve spawn");
				}
				Diagnostics.Log("ELCP====================================================================================");
			}
		}

		private void ManageSpecialFactions(int index, bool volcanic, ref FIDS fids, ref FIDS requiredfids)
		{
			if (base.Context.Configuration.Empires[index].Name == "AffinityBrokenLords")
			{
				fids.Food = 0;
				return;
			}
			if (base.Context.Configuration.Empires[index].Name == "AffinityReplicants")
			{
				fids.Science = 0;
				return;
			}
			if (volcanic && base.Context.Configuration.Empires[index].Name == "AffinityNecrophages")
			{
				fids.Food = 0;
				requiredfids.Food = 0;
			}
		}
	}
}
