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
			bool flag = true;
			while (flag)
			{
				flag = false;
				for (int i = 0; i < base.Context.Grid.Columns; i++)
				{
					for (int j = 0; j < base.Context.Grid.Rows; j++)
					{
						HexPos hexPos = new HexPos(i, j);
						Terrain terrain = base.Context.GetTerrain(hexPos);
						if (terrain != null)
						{
							int num = (int)base.Context.HeightData[j, i];
							if (!terrain.IsWaterTile)
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
									flag = true;
								}
							}
							else if (num >= 0)
							{
								base.Context.HeightData[j, i] = -1;
								flag = true;
							}
							else
							{
								int num2 = num;
								bool flag2 = true;
								foreach (HexPos hex in base.Context.Grid.Adjacents(hexPos))
								{
									terrain = base.Context.GetTerrain(hex);
									if (terrain != null)
									{
										if (terrain.IsWaterTile)
										{
											if (num2 < (int)(base.Context.HeightData[hex.Row, hex.Column] - 1))
											{
												num2 = (int)(base.Context.HeightData[hex.Row, hex.Column] - 1);
											}
										}
										else
										{
											flag2 = false;
											if (num >= (int)base.Context.HeightData[hex.Row, hex.Column])
											{
												num = -1;
											}
										}
									}
								}
								if (flag2)
								{
									num = num2;
								}
								if (base.Context.HeightData[j, i] != (sbyte)num)
								{
									flag = true;
								}
								base.Context.HeightData[j, i] = (sbyte)num;
							}
						}
					}
				}
			}
		}

		private static string elevationNameProperty = "Value";

		private Layer tmxElevationLayer;
	}
}
