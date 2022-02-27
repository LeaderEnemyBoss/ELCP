using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude.WorldGenerator.Graphs;
using Amplitude.WorldGenerator.Graphs.Hex;
using Amplitude.WorldGenerator.World;
using Amplitude.WorldGenerator.World.Info;

namespace Amplitude.WorldGenerator.Tasks.Generator
{
	public class CreateAnomalies : WorldGeneratorTask
	{
		public override void Execute(object context)
		{
			base.Report("?CreateAnomalies");
			base.Execute(context);
			this.GenerateUniqueAnomalies();
			base.ExecuteSubTask(new GenerateTerrainAnomalies());
			base.ExecuteSubTask(new ImproveSpawnAreas());
		}

		protected void GenerateUniqueAnomalies()
		{
			List<HexPos> list = new List<HexPos>(from d in base.Context.Districts.Values
			where d.Content == District.Contents.Land
			where !base.Context.SpawnRegions.Contains(d.MotherRegion.Id)
			from h in d
			where !base.Context.HasRiver[h.Row, h.Column]
			where base.Context.POIValidityMap[h.Row, h.Column] == WorldGeneratorContext.POIValidity.Free
			select h);
			if (list.Count < 1)
			{
				list = new List<HexPos>(from d in base.Context.Districts.Values
				where d.Content == District.Contents.Land
				from h in d
				where !base.Context.HasRiver[h.Row, h.Column]
				where base.Context.POIValidityMap[h.Row, h.Column] == WorldGeneratorContext.POIValidity.Free
				select h);
			}
			Dictionary<HexPos, int> weights = new Dictionary<HexPos, int>();
			list.ForEach(delegate(HexPos h)
			{
				weights.Add(h, 0);
			});
			WeightedRandomSelector<HexPos> weightedRandomSelector = new WeightedRandomSelector<HexPos>
			{
				Randomizer = base.Context.Randomizer
			};
			foreach (string text in base.Context.Settings.UniqueAnomaliesQuantities.Keys)
			{
				foreach (HexPos hexPos in list)
				{
					string name = base.Context.GetTerrain(hexPos).Name;
					weights[hexPos] = 0;
					if (base.Context.Settings.AnomalyWeightsPerTerrain.ContainsKey(name) && base.Context.POIValidityMap[hexPos.Row, hexPos.Column] == WorldGeneratorContext.POIValidity.Free && base.Context.Settings.AnomalyWeightsPerTerrain[name].ContainsKey(text))
					{
						weights[hexPos] = base.Context.Settings.AnomalyWeightsPerTerrain[name][text];
					}
				}
				weightedRandomSelector.UseDictionary(weights);
				int num = base.Context.Settings.UniqueAnomaliesQuantities[text];
				if (weightedRandomSelector.IsValid && num > 0)
				{
					for (int i = 0; i < num; i++)
					{
						if (weightedRandomSelector.IsValid)
						{
							HexPos randomSelected = weightedRandomSelector.RandomSelected;
							base.Context.POIValidityMap[randomSelected.Row, randomSelected.Column] = WorldGeneratorContext.POIValidity.Impossible;
							weights[randomSelected] = 0;
							weightedRandomSelector.UseDictionary(weights);
							if (!base.Context.Anomalies.ContainsKey(text))
							{
								base.Context.Anomalies.Add(text, new List<HexPos>());
							}
							base.Context.Anomalies[text].Add(randomSelected);
							base.Context.AnomalyMap[randomSelected.Row, randomSelected.Column] = text;
						}
					}
				}
			}
		}
	}
}
