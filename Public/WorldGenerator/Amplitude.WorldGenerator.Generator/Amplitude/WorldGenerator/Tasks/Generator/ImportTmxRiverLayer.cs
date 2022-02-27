using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude.WorldGenerator.Graphs.Hex;
using Amplitude.WorldGenerator.Tmx;
using Amplitude.WorldGenerator.World;
using Amplitude.WorldGenerator.World.Info;

namespace Amplitude.WorldGenerator.Tasks.Generator
{
	public class ImportTmxRiverLayer : WorldGeneratorTask
	{
		public override void Execute(object context)
		{
			base.Report("?ImportTmxRiverLayer");
			base.Execute(context);
			if (base.Context.Settings.TmxMap == null)
			{
				throw new ArgumentException("[WorldGenerator][Exception] TmxMap can't be null");
			}
			this.tmxRiverLayer = base.Context.Settings.TmxMap.Layers.FirstOrDefault((Layer layer) => layer.Name == "Rivers");
			if (this.tmxRiverLayer == null)
			{
				throw new ArgumentException("[WorldGenerator][Exception] TmxLayer Rivers can't be null");
			}
			base.Context.RiverMap = new int[base.Context.Grid.Rows, base.Context.Grid.Columns];
			this.CreateRivers();
		}

		private void CreateRivers()
		{
			for (int i = 0; i < base.Context.Grid.Columns; i++)
			{
				for (int j = 0; j < base.Context.Grid.Rows; j++)
				{
					HexPos hexPos = new HexPos(i, j);
					if (!this.treatedHexes.Contains(hexPos))
					{
						int num = this.tmxRiverLayer.Data[j, i];
						if (num != 0)
						{
							Tile tile = base.Context.Settings.TmxMap.GetTile(num);
							if (!tile.Properties.Keys.Contains(ImportTmxRiverLayer.nextTileProperty))
							{
								base.ReportTmx(string.Concat(new object[]
								{
									"?MissingPropertyTile&$Coordinate=",
									i,
									",",
									j - base.Context.Grid.Rows + 1,
									"&$MissingProperty=",
									ImportTmxRiverLayer.nextTileProperty
								}));
							}
							else if (!tile.Properties.Keys.Contains(ImportTmxRiverLayer.previousTileProperty))
							{
								base.ReportTmx(string.Concat(new object[]
								{
									"?MissingPropertyTile&$Coordinate=",
									i,
									",",
									j - base.Context.Grid.Rows + 1,
									"&$MissingProperty=",
									ImportTmxRiverLayer.previousTileProperty
								}));
							}
							else
							{
								HexPos.Direction d = (HexPos.Direction)Enum.Parse(typeof(HexPos.Direction), tile.Properties[ImportTmxRiverLayer.previousTileProperty]);
								HexPos lhs = new HexPos(hexPos);
								lhs.DoStep(d);
								int num2 = this.tmxRiverLayer.Data[lhs.Row, lhs.Column];
								if (num2 != 0)
								{
									Tile tile2 = base.Context.Settings.TmxMap.GetTile(num2);
									if (!tile2.Properties.Keys.Contains(ImportTmxRiverLayer.nextTileProperty))
									{
										base.ReportTmx(string.Concat(new object[]
										{
											"?MissingPropertyTile&$Coordinate=",
											i,
											",",
											j - base.Context.Grid.Rows + 1,
											"&$MissingProperty=",
											ImportTmxRiverLayer.nextTileProperty
										}));
										goto IL_2DB;
									}
									if (!tile2.Properties.Keys.Contains(ImportTmxRiverLayer.previousTileProperty))
									{
										base.ReportTmx(string.Concat(new object[]
										{
											"?MissingPropertyTile&$Coordinate=",
											i,
											",",
											j - base.Context.Grid.Rows + 1,
											"&$MissingProperty=",
											ImportTmxRiverLayer.previousTileProperty
										}));
										goto IL_2DB;
									}
									HexPos.Direction d2 = (HexPos.Direction)Enum.Parse(typeof(HexPos.Direction), tile2.Properties[ImportTmxRiverLayer.nextTileProperty]);
									lhs.DoStep(d2);
									if (lhs == hexPos)
									{
										goto IL_2DB;
									}
								}
								this.ExpandRiver(hexPos);
							}
						}
					}
					IL_2DB:;
				}
			}
		}

		private void ExpandRiver(HexPos startHexe)
		{
			River river = new River();
			river.Id = ImportTmxRiverLayer.id;
			ImportTmxRiverLayer.id++;
			river.Hexes.Add(startHexe);
			base.Context.SetRiverMap(startHexe, river.Id);
			this.treatedHexes.Add(startHexe);
			HexPos hexPos = new HexPos(startHexe);
			bool flag = false;
			do
			{
				Tile tile = base.Context.Settings.TmxMap.GetTile(this.tmxRiverLayer.Data[hexPos.Row, hexPos.Column]);
				if (!tile.Properties.Keys.Contains(ImportTmxRiverLayer.nextTileProperty))
				{
					return;
				}
				int elevation = (int)base.Context.GetElevation(hexPos);
				HexPos.Direction d = (HexPos.Direction)Enum.Parse(typeof(HexPos.Direction), tile.Properties[ImportTmxRiverLayer.nextTileProperty]);
				hexPos.DoStep(d);
				int elevation2 = (int)base.Context.GetElevation(hexPos);
				if (elevation < elevation2)
				{
					goto IL_217;
				}
				Terrain terrain = base.Context.GetTerrain(hexPos);
				if (terrain.Name.Contains("Waste"))
				{
					goto IL_26A;
				}
				if (this.tmxRiverLayer.Data[hexPos.Row, hexPos.Column] == 0)
				{
					if (!terrain.IsWaterTile)
					{
						goto IL_2BD;
					}
					HexPos hexPos2 = new HexPos(hexPos);
					river.Hexes.Add(hexPos2);
					base.Context.SetRiverMap(hexPos2, river.Id);
					this.treatedHexes.Add(hexPos2);
					string a;
					if (base.Context.Configuration.IsDLCAvailable("SummerFlamesPack") && tile.Properties.TryGetValue(ImportTmxRiverLayer.riverTypeProperty, out a) && a == "Lava")
					{
						river.Type = River.RiverType.LavaRiver;
					}
					flag = true;
				}
				else
				{
					if (terrain.IsWaterTile)
					{
						goto IL_2C9;
					}
					HexPos hexPos3 = new HexPos(hexPos);
					river.Hexes.Add(hexPos3);
					base.Context.SetRiverMap(hexPos3, river.Id);
					this.treatedHexes.Add(hexPos3);
				}
			}
			while (!flag);
			base.Context.Rivers.Add(river);
			return;
			IL_217:
			base.ReportTmx(string.Concat(new object[]
			{
				"?AscendingRiverImpossible&$Coordinate=",
				hexPos.Column,
				",",
				hexPos.Row - base.Context.Grid.Rows + 1
			}));
			return;
			IL_26A:
			base.ReportTmx(string.Concat(new object[]
			{
				"?RiverIncompatibleTerrain&$Coordinate=",
				hexPos.Column,
				",",
				hexPos.Row - base.Context.Grid.Rows + 1
			}));
			return;
			IL_2BD:
			base.ReportTmx("?InvalidRiverEnding");
			return;
			IL_2C9:
			base.ReportTmx(string.Concat(new object[]
			{
				"?RiverIncompatibleTerrain&$Coordinate=",
				hexPos.Column,
				",",
				hexPos.Row - base.Context.Grid.Rows + 1
			}));
		}

		private static int id = 1;

		private static string nextTileProperty = "NextTile";

		private static string previousTileProperty = "PreviousTile";

		private Layer tmxRiverLayer;

		private List<HexPos> treatedHexes = new List<HexPos>();

		private static string riverTypeProperty = "RiverType";
	}
}
