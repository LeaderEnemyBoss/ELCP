using System;
using System.Linq;
using Amplitude.WorldGenerator.Graphs.Hex;
using Amplitude.WorldGenerator.Tmx;
using Amplitude.WorldGenerator.World;
using Amplitude.WorldGenerator.World.Info;

namespace Amplitude.WorldGenerator.Tasks.Generator
{
	public class ImportTmxElevationLayer : WorldGeneratorTask
	{
		public override void Execute(object context)
		{
			base.Report("?ImportTmxElevationLayer");
			base.Execute(context);
			if (base.Context.Settings.TmxMap == null)
			{
				throw new ArgumentException("[WorldGenerator][Exception] TmxMap can't be null");
			}
			this.tmxElevationLayer = base.Context.Settings.TmxMap.Layers.FirstOrDefault((Layer layer) => layer.Name == "Elevations");
			if (this.tmxElevationLayer == null)
			{
				throw new ArgumentException("[WorldGenerator][Exception] TmxLayer Regions can't be null");
			}
			this.GetElevation();
			this.SmoothElevationImperfection();
		}

		private void GetElevation()
		{
			for (int i = 0; i < base.Context.Grid.Columns; i++)
			{
				for (int j = 0; j < base.Context.Grid.Rows; j++)
				{
					int num = this.tmxElevationLayer.Data[j, i];
					if (num != 0)
					{
						string tileProperty = base.Context.Settings.TmxMap.GetTileProperty(num, ImportTmxElevationLayer.elevationNameProperty);
						if (string.IsNullOrEmpty(tileProperty))
						{
							base.ReportTmx(string.Concat(new object[]
							{
								"?MissingPropertyTile&$Coordinate=",
								i,
								",",
								j - base.Context.Grid.Rows + 1,
								"&$MissingProperty=",
								ImportTmxElevationLayer.elevationNameProperty
							}));
						}
						else
						{
							int num2;
							if (!int.TryParse(tileProperty, out num2))
							{
								throw new ArgumentException(string.Concat(new object[]
								{
									"[WorldGenerator][Exception] Tile value : ",
									tileProperty,
									" given in position : ",
									i,
									",",
									j,
									" is incorrect"
								}));
							}
							base.Context.HeightData[j, i] = (sbyte)num2;
						}
					}
				}
			}
		}

		private void SmoothElevationImperfection()
		{
			for (int i = 0; i < base.Context.Grid.Columns; i++)
			{
				for (int j = 0; j < base.Context.Grid.Rows; j++)
				{
					HexPos hexPos = new HexPos(i, j);
					Terrain terrain = base.Context.GetTerrain(hexPos);
					if (terrain != null)
					{
						int num = (int)base.Context.HeightData[j, i];
						if (!terrain.Name.Contains("Ocean") && !terrain.Name.Contains("Water"))
						{
							if (num < 0)
							{
								base.Context.HeightData[j, i] = 0;
								base.ReportTmx(string.Concat(new object[]
								{
									"?NegativeLandElevation&$Coordinate=",
									i,
									",",
									j - base.Context.Grid.Rows + 1
								}));
							}
						}
						else
						{
							int num2 = num;
							bool flag = true;
							foreach (HexPos hex in base.Context.Grid.Adjacents(hexPos))
							{
								terrain = base.Context.GetTerrain(hex);
								if (terrain != null)
								{
									if (terrain.Name.Contains("Ocean") || terrain.Name.Contains("Water"))
									{
										if (num2 > (int)base.Context.HeightData[hex.Row, hex.Column])
										{
											num2 = (int)base.Context.HeightData[hex.Row, hex.Column];
										}
									}
									else
									{
										flag = false;
										if (num >= (int)base.Context.HeightData[hex.Row, hex.Column])
										{
											num = (int)(base.Context.HeightData[hex.Row, hex.Column] - 1);
										}
									}
								}
							}
							if (flag)
							{
								num = num2;
							}
							base.Context.HeightData[j, i] = (sbyte)num;
						}
					}
				}
			}
		}

		private static string elevationNameProperty = "Value";

		private Layer tmxElevationLayer;
	}
}
