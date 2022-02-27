using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude.WorldGenerator.Graphs.Hex;
using Amplitude.WorldGenerator.Tmx;
using Amplitude.WorldGenerator.World;
using Amplitude.WorldGenerator.World.Info;

namespace Amplitude.WorldGenerator.Tasks.Generator
{
	public class ImportTmxRegionLayer : WorldGeneratorTask
	{
		public override void Execute(object context)
		{
			base.Report("?ImportTmxRegionLayer");
			base.Execute(context);
			if (base.Context.Settings.TmxMap == null)
			{
				throw new ArgumentException("[WorldGenerator][Exception] TmxMap can't be null");
			}
			this.tmxRegionLayer = base.Context.Settings.TmxMap.Layers.FirstOrDefault((Layer layer) => layer.Name == "Regions");
			if (this.tmxRegionLayer == null)
			{
				throw new ArgumentException("[WorldGenerator][Exception] TmxLayer Regions can't be null");
			}
			this.DetermineRegions();
			if (this.anyErrorCatched)
			{
				throw new TmxImportException();
			}
			this.CompleteRegionsValues();
			this.DetermineRegionNeighbors();
			base.ExecuteSubTask(new NameRegions());
			this.CheckIfRegionMixOceanWithLands();
		}

		private void DetermineRegions()
		{
			this.anyErrorCatched = false;
			this.treatedHexes = new bool[base.Context.Grid.Rows, base.Context.Grid.Columns];
			for (int i = 0; i < base.Context.Grid.Columns; i++)
			{
				for (int j = 0; j < base.Context.Grid.Rows; j++)
				{
					if (!this.treatedHexes[j, i])
					{
						this.CreateNewRegion(new HexPos(i, j));
					}
				}
			}
		}

		private void CreateNewRegion(HexPos hexe)
		{
			Tile tile = base.Context.Settings.TmxMap.GetTile(this.tmxRegionLayer.Data[hexe.Row, hexe.Column]);
			if (tile == null)
			{
				base.ReportTmx(string.Concat(new object[]
				{
					"?MissingTile&$Coordinate=",
					hexe.Column,
					",",
					hexe.Row - base.Context.Grid.Rows + 1,
					"&$LayerName=Regions"
				}));
				this.anyErrorCatched = true;
				return;
			}
			short regionTileValue = (short)tile.Id;
			Region region = new Region(base.Context.Grid);
			region.Id = ImportTmxRegionLayer.id;
			ImportTmxRegionLayer.id += 1;
			string tileProperty = base.Context.Settings.TmxMap.GetTileProperty(this.tmxRegionLayer.Data[hexe.Row, hexe.Column], ImportTmxRegionLayer.regionNameProperty);
			if (!string.IsNullOrEmpty(tileProperty))
			{
				region.Name = tileProperty;
				if (this.regionsGivenNames.Contains(tileProperty))
				{
					base.ReportTmx("?RegionNameDuplication&$RegionName=" + tileProperty);
				}
				else
				{
					this.regionsGivenNames.Add(tileProperty);
				}
			}
			District district = new District();
			district.MotherRegion = region;
			district.Id = (int)region.Id;
			district.Add(hexe);
			region.Districts.Add(district);
			this.treatedHexes[hexe.Row, hexe.Column] = true;
			base.Context.RegionData[hexe.Row, hexe.Column] = region.Id;
			base.Context.DistrictData[hexe.Row, hexe.Column] = district.Id;
			this.ExpandRegion(ref region, (int)regionTileValue);
			Diagnostics.Log(string.Format("[WorldGenerator] [ImportTmxRegionLayer] Region id is {0} it is composed of {1} hexe.", region.Id, district.Count));
			base.Context.Regions.Add(region.Id, region);
			base.Context.Districts.Add(district.Id, district);
		}

		private void ExpandRegion(ref Region region, int regionTileValue)
		{
			Queue<HexPos> queue = new Queue<HexPos>();
			queue.Enqueue(region.Districts[0].ElementAt(0));
			while (queue.Count > 0)
			{
				HexPos pos = queue.Dequeue();
				foreach (HexPos item in base.Context.Grid.Adjacents(pos))
				{
					Tile tile = base.Context.Settings.TmxMap.GetTile(this.tmxRegionLayer.Data[item.Row, item.Column]);
					if (tile == null)
					{
						base.ReportTmx(string.Concat(new object[]
						{
							"?MissingTile&$Coordinate=",
							item.Column,
							",",
							item.Row - base.Context.Grid.Rows + 1,
							"&$LayerName=Regions"
						}));
						this.anyErrorCatched = true;
						return;
					}
					if ((int)((short)tile.Id) == regionTileValue && !this.treatedHexes[item.Row, item.Column])
					{
						region.Districts[0].Add(item);
						base.Context.RegionData[item.Row, item.Column] = region.Id;
						base.Context.DistrictData[item.Row, item.Column] = region.Districts[0].Id;
						this.treatedHexes[item.Row, item.Column] = true;
						if (!queue.Contains(item))
						{
							queue.Enqueue(item);
						}
					}
				}
			}
		}

		private void CompleteRegionsValues()
		{
			base.Context.RegionSkeletonValues = new int[base.Context.Grid.Rows, base.Context.Grid.Columns];
			foreach (Region region in base.Context.Regions.Values)
			{
				region.LandMassType = Region.LandMassTypes.Continent;
				region.Districts[0].Content = District.Contents.Land;
				region.ComputeRegionSkeleton(base.Context);
				Terrain terrain = base.Context.GetTerrain(region.Center);
				if (terrain != null)
				{
					foreach (Biome biome in base.Context.Settings.Biomes)
					{
						for (int i = 0; i < biome.LandTerrainWeights.Length; i++)
						{
							if (biome.LandTerrainWeights[i].Name == terrain.Name)
							{
								region.Biome = biome;
								break;
							}
						}
						if (region.Biome != null)
						{
							break;
						}
						for (int j = 0; j < biome.OceanTerrainWeights.Length; j++)
						{
							if (biome.OceanTerrainWeights[j].Name == terrain.Name)
							{
								region.Biome = biome;
								break;
							}
						}
						if (region.Biome != null)
						{
							break;
						}
						for (int k = 0; k < biome.CoastTerrainWeights.Length; k++)
						{
							if (biome.CoastTerrainWeights[k].Name == terrain.Name)
							{
								region.Biome = biome;
								break;
							}
						}
						if (region.Biome != null)
						{
							break;
						}
						for (int l = 0; l < biome.LakeTerrainWeights.Length; l++)
						{
							if (biome.LakeTerrainWeights[l].Name == terrain.Name)
							{
								region.Biome = biome;
								break;
							}
						}
						if (region.Biome != null)
						{
							break;
						}
					}
					if (region.Biome == null)
					{
						region.Biome = base.Context.Settings.Biomes[0];
					}
					if (terrain.Name.Contains("Ocean"))
					{
						region.LandMassType = Region.LandMassTypes.Ocean;
						region.Districts[0].Content = District.Contents.Ocean;
						region.Districts[0].CoastalSkeletonValue = -1;
					}
					foreach (HexPos hexPos in region.Districts[0])
					{
						if (hexPos.Row == 0 || hexPos.Row == base.Context.Grid.Rows - 1)
						{
							region.LandMassType = Region.LandMassTypes.WasteNS;
							region.Districts[0].Content = District.Contents.WasteNS;
							region.Districts[0].CoastalSkeletonValue = 99;
						}
						if ((hexPos.Column == 0 || hexPos.Column == base.Context.Grid.Columns - 1) && !base.Context.Settings.WorldWrap)
						{
							region.LandMassType = Region.LandMassTypes.WasteEW;
							region.Districts[0].Content = District.Contents.WasteEW;
							region.Districts[0].CoastalSkeletonValue = -1;
						}
					}
					if (region.LandMassType == Region.LandMassTypes.WasteNS || region.LandMassType == Region.LandMassTypes.WasteEW)
					{
						TerrainTransformation transform = base.Context.Settings.Transformations.Find((TerrainTransformation t) => t.Name == "Wastelands");
						foreach (HexPos hex in region.Districts[0])
						{
							base.Context.ApplyTransformation(transform, hex);
						}
					}
				}
			}
		}

		private void DetermineRegionNeighbors()
		{
			foreach (Region region in base.Context.Regions.Values)
			{
				List<Frontier> list = new List<Frontier>();
				foreach (District district in region.Districts)
				{
					foreach (HexPos hexPos in district)
					{
						foreach (HexPos hexPos2 in base.Context.Grid.Adjacents(hexPos))
						{
							int key = base.Context.DistrictData[hexPos2.Row, hexPos2.Column];
							District district2 = base.Context.Districts[key];
							if (district2 != null)
							{
								Region neighborRegion = district2.MotherRegion;
								if (neighborRegion != null && region.Id != neighborRegion.Id)
								{
									if (!district.Neighbours.Contains(district2))
									{
										district.Neighbours.Add(base.Context.Districts[key]);
									}
									RegionBorder regionBorder;
									if (region.Borders.ContainsKey(neighborRegion))
									{
										regionBorder = region.Borders[neighborRegion];
									}
									else
									{
										regionBorder = new RegionBorder(region, neighborRegion);
										region.Borders.Add(neighborRegion, regionBorder);
									}
									if (!regionBorder.Contains(hexPos))
									{
										regionBorder.Add(hexPos);
									}
									Frontier frontier = list.FirstOrDefault((Frontier tmpFront) => tmpFront.Neighbour == neighborRegion.Id);
									if (frontier == null)
									{
										frontier = new Frontier
										{
											Neighbour = neighborRegion.Id,
											Points = new List<Frontier.Point>()
										};
										list.Add(frontier);
									}
									bool flag = false;
									for (int i = 0; i < frontier.Points.Count; i++)
									{
										if (frontier.Points[i].Hex == hexPos)
										{
											flag = true;
										}
									}
									if (!flag)
									{
										frontier.Points.Add(new Frontier.Point
										{
											From = (int)neighborRegion.Id,
											Hex = hexPos
										});
									}
								}
							}
						}
					}
				}
				region.Frontiers = list.ToArray();
				if (region.Neighbours.Count == 1)
				{
					base.ReportTmx("?NestedRegion");
				}
			}
		}

		private void CheckIfRegionMixOceanWithLands()
		{
			foreach (Region region in base.Context.Regions.Values)
			{
				foreach (HexPos hex in region.Hexes)
				{
					Terrain terrain = base.Context.GetTerrain(hex);
					if (terrain != null && !terrain.Name.Contains("Waste") && ((region.LandMassType != Region.LandMassTypes.Ocean && terrain.Name.Contains("Ocean")) || (region.LandMassType == Region.LandMassTypes.Ocean && !terrain.Name.Contains("Ocean"))))
					{
						base.ReportTmx(string.Concat(new object[]
						{
							"?OceanMixedWithLands&$RegionName=",
							region.Name,
							"&$Coordinate=",
							hex.Row,
							"::",
							hex.Column
						}));
						break;
					}
				}
			}
		}

		private static short id = 0;

		private static string regionNameProperty = "Name";

		private Layer tmxRegionLayer;

		private bool[,] treatedHexes;

		private List<string> regionsGivenNames = new List<string>();

		private bool anyErrorCatched;
	}
}
