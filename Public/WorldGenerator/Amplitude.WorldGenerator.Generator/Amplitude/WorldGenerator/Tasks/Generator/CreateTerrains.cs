using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude.WorldGenerator.Graphs;
using Amplitude.WorldGenerator.Graphs.Hex;
using Amplitude.WorldGenerator.World;
using Amplitude.WorldGenerator.World.Info;

namespace Amplitude.WorldGenerator.Tasks.Generator
{
	public class CreateTerrains : WorldGeneratorTask
	{
		public override void Execute(object context)
		{
			base.Report("?CreateTerrains");
			base.Execute(context);
			List<Biome> list = new List<Biome>();
			for (int i = 0; i < base.Context.Settings.Biomes.Count; i++)
			{
				if (!(base.Context.Settings.Biomes[i].DLCPrerequisite != string.Empty) || base.Context.Configuration.IsDLCAvailable(base.Context.Settings.Biomes[i].DLCPrerequisite))
				{
					list.Add(base.Context.Settings.Biomes[i]);
				}
			}
			int count = list.Count;
			base.Context.OceanSelectors = new WeightedRandomSelector<Terrain>[count];
			base.Context.LandSelectors = new WeightedRandomSelector<Terrain>[count];
			base.Context.LakeSelectors = new WeightedRandomSelector<Terrain>[count];
			base.Context.CoastSelectors = new WeightedRandomSelector<Terrain>[count];
			foreach (Biome biome in list)
			{
				base.Context.OceanSelectors[(int)biome.Id] = new WeightedRandomSelector<Terrain>();
				base.Context.OceanSelectors[(int)biome.Id].ItemsList = new List<Terrain>();
				base.Context.OceanSelectors[(int)biome.Id].WeightsList = new List<int>();
				base.Context.OceanSelectors[(int)biome.Id].Randomizer = base.Context.Randomizer;
				Biome.TerrainWeight[] array = biome.OceanTerrainWeights;
				for (int j = 0; j < array.Length; j++)
				{
					Biome.TerrainWeight tw = array[j];
					Terrain item = base.Context.Settings.Terrains.Find((Terrain t) => t.Name == tw.Name);
					base.Context.OceanSelectors[(int)biome.Id].ItemsList.Add(item);
					base.Context.OceanSelectors[(int)biome.Id].WeightsList.Add(tw.Weight);
				}
				base.Context.LandSelectors[(int)biome.Id] = new WeightedRandomSelector<Terrain>();
				base.Context.LandSelectors[(int)biome.Id].ItemsList = new List<Terrain>();
				base.Context.LandSelectors[(int)biome.Id].WeightsList = new List<int>();
				base.Context.LandSelectors[(int)biome.Id].Randomizer = base.Context.Randomizer;
				array = biome.LandTerrainWeights;
				for (int j = 0; j < array.Length; j++)
				{
					Biome.TerrainWeight tw = array[j];
					Terrain item2 = base.Context.Settings.Terrains.Find((Terrain t) => t.Name == tw.Name);
					base.Context.LandSelectors[(int)biome.Id].ItemsList.Add(item2);
					base.Context.LandSelectors[(int)biome.Id].WeightsList.Add(tw.Weight);
				}
				base.Context.LakeSelectors[(int)biome.Id] = new WeightedRandomSelector<Terrain>();
				base.Context.LakeSelectors[(int)biome.Id].ItemsList = new List<Terrain>();
				base.Context.LakeSelectors[(int)biome.Id].WeightsList = new List<int>();
				base.Context.LakeSelectors[(int)biome.Id].Randomizer = base.Context.Randomizer;
				array = biome.LakeTerrainWeights;
				for (int j = 0; j < array.Length; j++)
				{
					Biome.TerrainWeight tw = array[j];
					Terrain item3 = base.Context.Settings.Terrains.Find((Terrain t) => t.Name == tw.Name);
					base.Context.LakeSelectors[(int)biome.Id].ItemsList.Add(item3);
					base.Context.LakeSelectors[(int)biome.Id].WeightsList.Add(tw.Weight);
				}
				base.Context.CoastSelectors[(int)biome.Id] = new WeightedRandomSelector<Terrain>();
				base.Context.CoastSelectors[(int)biome.Id].ItemsList = new List<Terrain>();
				base.Context.CoastSelectors[(int)biome.Id].WeightsList = new List<int>();
				base.Context.CoastSelectors[(int)biome.Id].Randomizer = base.Context.Randomizer;
				array = biome.CoastTerrainWeights;
				for (int j = 0; j < array.Length; j++)
				{
					Biome.TerrainWeight tw = array[j];
					Terrain item4 = base.Context.Settings.Terrains.Find((Terrain t) => t.Name == tw.Name);
					base.Context.CoastSelectors[(int)biome.Id].ItemsList.Add(item4);
					base.Context.CoastSelectors[(int)biome.Id].WeightsList.Add(tw.Weight);
				}
			}
			TerrainTransformation terrainTransformation = base.Context.Settings.Transformations.Find((TerrainTransformation t) => t.Name == "HighMountain");
			TerrainTransformation terrainTransformation2 = base.Context.Settings.Transformations.Find((TerrainTransformation t) => t.Name == "MediumMountain");
			TerrainTransformation transform = base.Context.Settings.Transformations.Find((TerrainTransformation t) => t.Name == "RidgePresence");
			base.Context.Settings.Transformations.Find((TerrainTransformation t) => t.Name == "RiverFlood");
			base.Context.Settings.Transformations.Find((TerrainTransformation t) => t.Name == "LavaRiver");
			TerrainTransformation transform2 = base.Context.Settings.Transformations.Find((TerrainTransformation t) => t.Name == "Wastelands");
			TerrainTransformation terrainTransformation3 = base.Context.Settings.Transformations.Find((TerrainTransformation t) => t.Name == "Volcano");
			if (terrainTransformation == null)
			{
				base.Trace("No 'HighMountain' transformation found - none applied");
			}
			if (terrainTransformation2 == null)
			{
				base.Trace("No 'MediumMountain' transformation found - none applied");
			}
			if (terrainTransformation3 == null)
			{
				base.Trace("No 'Volcano' transformation found - none applied");
			}
			foreach (District district in base.Context.Districts.Values)
			{
				Region motherRegion = district.MotherRegion;
				if (district.Content == District.Contents.Coastal)
				{
					district.Terrain = base.Context.CoastSelectors[(int)motherRegion.Biome.Id].RandomSelected;
				}
				else if (district.Content == District.Contents.Lake)
				{
					district.Terrain = base.Context.LakeSelectors[(int)motherRegion.Biome.Id].RandomSelected;
				}
				else if (district.Content == District.Contents.Land)
				{
					district.Terrain = base.Context.LandSelectors[(int)motherRegion.Biome.Id].RandomSelected;
				}
				else if (district.Content == District.Contents.Ocean)
				{
					district.Terrain = base.Context.OceanSelectors[(int)motherRegion.Biome.Id].RandomSelected;
				}
				else if (district.Content == District.Contents.Ridge)
				{
					district.Terrain = base.Context.LandSelectors[(int)motherRegion.Biome.Id].RandomSelected;
					base.Context.ApplyTransformation(transform, district);
				}
				else if (district.Content == District.Contents.WasteNS)
				{
					district.Terrain = base.Context.LandSelectors[(int)motherRegion.Biome.Id].RandomSelected;
					base.Context.ApplyTransformation(transform2, district);
				}
				else if (district.Content == District.Contents.WasteEW)
				{
					if (district.Neighbours.Any((District n) => n.Content == District.Contents.Ocean))
					{
						district.Terrain = base.Context.OceanSelectors[(int)motherRegion.Biome.Id].RandomSelected;
						district.Elevation = -2;
					}
					else
					{
						district.Terrain = base.Context.LandSelectors[(int)motherRegion.Biome.Id].RandomSelected;
						district.Elevation = 0;
					}
					base.Context.ApplyTransformation(transform2, district);
					foreach (HexPos hexPos in district)
					{
						base.Context.HeightData[hexPos.Row, hexPos.Column] = district.Elevation;
					}
				}
				if (district.Elevation >= base.Context.Settings.HighMountainElevation)
				{
					base.Context.ApplyTransformation(terrainTransformation, district);
				}
				else if (district.Elevation >= base.Context.Settings.MediumMountainElevation)
				{
					base.Context.ApplyTransformation(terrainTransformation2, district);
				}
			}
			int[] array2 = new int[base.Context.Settings.Terrains.Count];
			foreach (District district2 in base.Context.Districts.Values)
			{
				array2[(int)district2.Terrain.Id] += district2.Count;
				using (HashSet<HexPos>.Enumerator enumerator3 = district2.GetEnumerator())
				{
					while (enumerator3.MoveNext())
					{
						HexPos hex = enumerator3.Current;
						base.Context.TerrainData[hex.Row, hex.Column] = district2.Terrain.Id;
						if (base.Context.HasRiver[hex.Row, hex.Column])
						{
							River river = base.Context.Rivers.Find((River r) => r.Hexes.Contains(hex));
							if (river.Type == River.RiverType.LavaRiver && hex == river.StartingHex)
							{
								base.Context.ApplyTransformation(terrainTransformation3, district2);
								base.Context.ApplyTransformation(transform, district2);
								base.Context.ApplyTransformation(terrainTransformation3, hex);
							}
						}
					}
				}
			}
		}
	}
}
