using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude.WorldGenerator.Graphs.Hex;
using Amplitude.WorldGenerator.Tmx;
using Amplitude.WorldGenerator.World;
using Amplitude.WorldGenerator.World.Info;

namespace Amplitude.WorldGenerator.Tasks.Generator
{
	public class ImportTmxSpawnLayer : WorldGeneratorTask
	{
		public override void Execute(object context)
		{
			base.Report("?ImportTmxTerrainLayer");
			base.Execute(context);
			if (base.Context.Settings.TmxMap == null)
			{
				throw new ArgumentException("[WorldGenerator][Exception] TmxMap can't be null");
			}
			this.tmxSpawnLayer = base.Context.Settings.TmxMap.Layers.FirstOrDefault((Layer layer) => layer.Name == "Spawns");
			if (this.tmxSpawnLayer == null)
			{
				throw new ArgumentException("[WorldGenerator][Exception] TmxLayer Spawn can't be null");
			}
			this.GetSpawns();
			this.PickSolution();
		}

		private void GetSpawns()
		{
			for (int i = 0; i < base.Context.Grid.Columns; i++)
			{
				for (int j = 0; j < base.Context.Grid.Rows; j++)
				{
					int num = this.tmxSpawnLayer.Data[j, i];
					if (num != 0)
					{
						short num2;
						if (!short.TryParse(base.Context.Settings.TmxMap.GetTileProperty(num, ImportTmxSpawnLayer.spawnNameProperty), out num2))
						{
							base.ReportTmx(string.Concat(new object[]
							{
								"?MissingPropertyTile&$Coordinate=",
								i,
								",",
								j - base.Context.Grid.Rows + 1,
								"&$MissingProperty=",
								ImportTmxSpawnLayer.spawnNameProperty
							}));
						}
						else if (num2 >= 0 && (num2 <= 7 || num2 == 42))
						{
							short regionId = base.Context.RegionData[j, i];
							HexPos hexPos = new HexPos(i, j);
							Terrain terrain = base.Context.GetTerrain(hexPos);
							if (terrain.Name.Contains("Ocean") || terrain.Name.Contains("Water") || terrain.Name.Contains("Waste"))
							{
								base.ReportTmx(string.Concat(new object[]
								{
									"?SpawnIncompatibleTerrain&$Coordinate=",
									i,
									",",
									j - base.Context.Grid.Rows + 1
								}));
							}
							else
							{
								bool flag = false;
								foreach (Ridge ridge in base.Context.Ridges)
								{
									foreach (HexPos rhs in ridge.Hexes)
									{
										if (hexPos == rhs)
										{
											flag = true;
										}
									}
								}
								if (flag)
								{
									base.ReportTmx(string.Concat(new object[]
									{
										"?SpawnIncompatibleTerrain&$Coordinate=",
										i,
										",",
										j - base.Context.Grid.Rows + 1
									}));
								}
								else
								{
									this.spawns.Add(new ImportTmxSpawnLayer.Spawn(num2, regionId, hexPos));
								}
							}
						}
					}
				}
			}
			ImportTmxSpawnLayer.Spawn[] array = new ImportTmxSpawnLayer.Spawn[base.Context.EmpiresCount];
			if (!this.FindSpawnsSet(ref array, 0))
			{
				base.ReportTmx("?NoSpawnCombination");
			}
		}

		private bool FindSpawnsSet(ref ImportTmxSpawnLayer.Spawn[] solution, int empireIndex)
		{
			for (int i = 0; i < this.spawns.Count; i++)
			{
				if ((int)this.spawns[i].EmpireIndex == empireIndex || this.spawns[i].EmpireIndex == 42)
				{
					bool flag = false;
					for (int j = 0; j < solution.Length; j++)
					{
						if (this.spawns[i].RegionId == solution[j].RegionId)
						{
							flag = true;
						}
					}
					if (!flag)
					{
						solution[empireIndex] = this.spawns[i];
						if (empireIndex + 1 == base.Context.EmpiresCount)
						{
							this.spawnsSolutions.Add(solution.Clone() as ImportTmxSpawnLayer.Spawn[]);
						}
						else
						{
							this.FindSpawnsSet(ref solution, empireIndex + 1);
						}
					}
				}
			}
			return this.spawnsSolutions.Count > 0;
		}

		private void PickSolution()
		{
			if (this.spawnsSolutions.Count == 0)
			{
				throw new TmxImportException();
			}
			int index = new Random(DateTime.Now.GetHashCode()).Next(this.spawnsSolutions.Count);
			base.Context.SpawnRegions = new short[this.spawnsSolutions[index].Length];
			base.Context.SpawnPointsDefault = new HexPos[this.spawnsSolutions[index].Length];
			base.Context.SpawnEmpirePreferences = new List<HexPos>[this.spawnsSolutions[index].Length];
			for (int i = 0; i < this.spawnsSolutions[index].Length; i++)
			{
				ImportTmxSpawnLayer.Spawn spawn = this.spawnsSolutions[index][i];
				base.Context.SpawnRegions[i] = spawn.RegionId;
				base.Context.SpawnPointsDefault[i] = spawn.Hexe;
				base.Context.SpawnEmpirePreferences[i] = new List<HexPos>
				{
					spawn.Hexe
				};
			}
		}

		private const int RandomEmpireValue = 42;

		private static string spawnNameProperty = "Value";

		private Layer tmxSpawnLayer;

		private List<ImportTmxSpawnLayer.Spawn> spawns = new List<ImportTmxSpawnLayer.Spawn>();

		private List<ImportTmxSpawnLayer.Spawn[]> spawnsSolutions = new List<ImportTmxSpawnLayer.Spawn[]>();

		public struct Spawn
		{
			public Spawn(short index, short regionId, HexPos hexe)
			{
				this.EmpireIndex = index;
				this.RegionId = regionId;
				this.Hexe = hexe;
			}

			public Spawn(ImportTmxSpawnLayer.Spawn spawn)
			{
				this.EmpireIndex = spawn.EmpireIndex;
				this.RegionId = spawn.RegionId;
				this.Hexe = spawn.Hexe;
			}

			public short EmpireIndex;

			public short RegionId;

			public HexPos Hexe;
		}
	}
}
