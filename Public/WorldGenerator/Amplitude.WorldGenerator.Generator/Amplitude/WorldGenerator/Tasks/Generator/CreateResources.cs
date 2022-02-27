using System;
using System.Collections.Generic;
using Amplitude.WorldGenerator.Graphs.Hex;
using Amplitude.WorldGenerator.World;
using Amplitude.WorldGenerator.World.Info;

namespace Amplitude.WorldGenerator.Tasks.Generator
{
	public class CreateResources : WorldGeneratorTask
	{
		public override void Execute(object context)
		{
			base.Report("?CreateResources");
			base.Execute(context);
			base.ExecuteSubTask(new PrepareOceanicPOIDistribution());
			bool flag = base.Context.Configuration.IsDLCAvailable("NavalPack");
			if (flag)
			{
				base.ExecuteSubTask(new DistributeOceanicCitadels());
				base.ExecuteSubTask(new DistributeUniqueFacilities());
				base.ExecuteSubTask(new DistributeStrategicFacilities());
				if (base.Context.Settings.ReplaceInlandSeas)
				{
					this.ModifyInlandOceans();
				}
			}
			base.ExecuteSubTask(new DistributeStrategicResources());
			base.ExecuteSubTask(new DistributeLuxuryResources());
			if (flag)
			{
				base.ExecuteSubTask(new DistributeSunkenRuins());
			}
			foreach (OceanicFortress oceanicFortress in base.Context.OceanicFortresses)
			{
				base.Trace(string.Format("Oceanic Fortress in region {0}", oceanicFortress.OceanRegion.Id));
				foreach (PointOfInterestDefinition pointOfInterestDefinition in oceanicFortress.Facilities)
				{
					base.Trace(string.Format(" - Facility : {0}", pointOfInterestDefinition.TemplateName));
				}
			}
		}

		private void ModifyInlandOceans()
		{
			if (base.Context.OceanicFortresses.Count < 1)
			{
				return;
			}
			this.OceanDistricts = new List<District>();
			this.CheckedDistricts = new List<District>();
			List<Region> list = new List<Region>();
			using (List<OceanicFortress>.Enumerator enumerator = base.Context.OceanicFortresses.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					OceanicFortress Fortress = enumerator.Current;
					if (!list.Exists((Region x) => x == Fortress.OceanRegion))
					{
						list.Add(Fortress.OceanRegion);
						this.OceanDistricts.AddRange(Fortress.OceanRegion.Districts);
					}
				}
			}
			foreach (District district in base.Context.Districts.Values)
			{
				Region motherRegion = district.MotherRegion;
				if ((district.Content == District.Contents.Ocean || district.Content == District.Contents.Coastal) && !this.OceanDistricts.Contains(district))
				{
					this.CheckedDistricts.Clear();
					if (!this.CheckIfConnectedToOcean(district))
					{
						using (List<District>.Enumerator enumerator3 = this.CheckedDistricts.GetEnumerator())
						{
							while (enumerator3.MoveNext())
							{
								District district2 = enumerator3.Current;
								district2.Terrain = base.Context.LakeSelectors[(int)motherRegion.Biome.Id].RandomSelected;
								district2.Content = District.Contents.Lake;
								using (HashSet<HexPos>.Enumerator enumerator4 = district2.GetEnumerator())
								{
									if (enumerator4.MoveNext())
									{
										HexPos hexPos = enumerator4.Current;
									}
								}
							}
							continue;
						}
					}
					this.OceanDistricts.AddRange(this.CheckedDistricts);
				}
			}
			int[] array = new int[base.Context.Settings.Terrains.Count];
			foreach (District district3 in base.Context.Districts.Values)
			{
				if (district3.Content == District.Contents.Lake)
				{
					array[(int)district3.Terrain.Id] += district3.Count;
					foreach (HexPos hexPos2 in district3)
					{
						base.Context.TerrainData[hexPos2.Row, hexPos2.Column] = district3.Terrain.Id;
					}
				}
			}
		}

		private bool CheckIfConnectedToOcean(District District)
		{
			this.CheckedDistricts.Add(District);
			bool flag = false;
			foreach (District district in District.Neighbours)
			{
				if ((district.Content == District.Contents.Ocean || district.Content == District.Contents.Coastal) && !this.CheckedDistricts.Contains(district))
				{
					if (this.OceanDistricts.Contains(district))
					{
						return true;
					}
					if (!flag)
					{
						flag = this.CheckIfConnectedToOcean(district);
					}
					else
					{
						this.CheckIfConnectedToOcean(district);
					}
				}
			}
			return flag;
		}

		private List<District> OceanDistricts;

		private List<District> CheckedDistricts;
	}
}
