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
			base.Report("?ImportTmxSpawnLayer");
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
			this.RemoveELCPSpectatorEmpires();
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
						else if (num2 >= 0 && (num2 <= 14 || num2 == 42))
						{
							short regionId = base.Context.RegionData[j, i];
							HexPos hexPos = new HexPos(i, j);
							Terrain terrain = base.Context.GetTerrain(hexPos);
							if (terrain.IsWaterTile || terrain.Name.Contains("Waste"))
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
									string tileProperty = base.Context.Settings.TmxMap.GetTileProperty(num, ImportTmxSpawnLayer.affinityNameProperty);
									if (string.IsNullOrEmpty(tileProperty))
									{
										this.spawns.Add(new ImportTmxSpawnLayer.Spawn(num2, regionId, hexPos));
									}
									else
									{
										if (!this.affinitySpawns.ContainsKey(tileProperty))
										{
											this.affinitySpawns.Add(tileProperty, new List<ImportTmxSpawnLayer.Spawn>());
										}
										this.affinitySpawns[tileProperty].Add(new ImportTmxSpawnLayer.Spawn(-1, regionId, hexPos));
									}
								}
							}
						}
					}
				}
			}
			this.OverrideNumericSpawns();
			this.ReserveUnusedAffinitySpawns();
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
					this.ValidateSpawnPoint(ref solution, empireIndex, i);
				}
			}
			if (this.spawnsSolutions.Count == 0)
			{
				this.FindExtraSpawn(empireIndex, -1);
				this.FindSpawnsSet(ref solution, empireIndex);
			}
			return this.spawnsSolutions.Count > 0;
		}

		private void PickSolution()
		{
			if (this.spawnsSolutions.Count == 0)
			{
				throw new TmxImportException();
			}
			foreach (ImportTmxSpawnLayer.Spawn spawn in this.spawnsSolutions[0])
			{
				base.Report(spawn.EmpireIndex + " - " + spawn.RegionId);
			}
			int index = this.randomGenerator.Next(this.spawnsSolutions.Count);
			base.Context.SpawnRegions = new short[this.spawnsSolutions[index].Length];
			base.Context.SpawnPointsDefault = new HexPos[this.spawnsSolutions[index].Length];
			base.Context.SpawnEmpirePreferences = new List<HexPos>[this.spawnsSolutions[index].Length];
			for (int j = 0; j < this.spawnsSolutions[index].Length; j++)
			{
				ImportTmxSpawnLayer.Spawn spawn2 = this.spawnsSolutions[index][j];
				base.Context.SpawnRegions[j] = spawn2.RegionId;
				base.Context.SpawnPointsDefault[j] = spawn2.Hexe;
				base.Context.SpawnEmpirePreferences[j] = new List<HexPos>
				{
					spawn2.Hexe
				};
				if (base.Context.Configuration.Empires[(int)spawn2.EmpireIndex].Name == "AffinityFlames")
				{
					this.ImportTmxSpawnLayer_TryVolcanizeRegion(base.Context.Regions[spawn2.RegionId]);
				}
			}
		}

		private void ImportTmxSpawnLayer_TryVolcanizeRegion(Region region)
		{
			Random random = new Random((int)region.Id);
			List<Biome> list = new List<Biome>();
			foreach (Biome biome in base.Context.Settings.Biomes)
			{
				if (biome.IsVolcanic)
				{
					list.Add(biome);
				}
			}
			if (list.Count == 0)
			{
				return;
			}
			region.Biome = list[random.Next(list.Count)];
			TerrainTransformation transform = base.Context.Settings.Transformations.Find((TerrainTransformation t) => t.Name == "ELCPAshlandsTrans");
			foreach (HexPos hex in region.Hexes)
			{
				base.Context.ApplyTransformation(transform, hex);
			}
			List<River> list2 = base.Context.Rivers.FindAll((River R) => region.Hexes.Contains(R.StartingHex) && R.Tributaries.Count == 0 && R.FlowsInto == null);
			if (list2 != null)
			{
				foreach (River river in list2)
				{
					river.Type = River.RiverType.LavaRiver;
				}
			}
		}

		private void RemoveELCPSpectatorEmpires()
		{
			List<EmpireDefinition> list = base.Context.Configuration.Empires.ToList<EmpireDefinition>();
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].Name == "AffinityELCPSpectator")
				{
					list.RemoveAt(i);
					i--;
				}
			}
			base.Context.Configuration.Empires = list.ToArray();
		}

		private void ValidateSpawnPoint(ref ImportTmxSpawnLayer.Spawn[] solution, int empireIndex, int spawnIndex)
		{
			bool flag = false;
			for (int i = 0; i < solution.Length; i++)
			{
				if (this.spawns[spawnIndex].RegionId == solution[i].RegionId && (int)solution[i].EmpireIndex != empireIndex)
				{
					flag = true;
				}
			}
			if (flag)
			{
				if (this.FindExtraSpawn(empireIndex, spawnIndex))
				{
					this.ValidateSpawnPoint(ref solution, empireIndex, spawnIndex);
				}
				return;
			}
			solution[empireIndex] = this.spawns[spawnIndex];
			if (empireIndex + 1 == base.Context.EmpiresCount)
			{
				this.spawnsSolutions.Add(solution.Clone() as ImportTmxSpawnLayer.Spawn[]);
				return;
			}
			this.FindSpawnsSet(ref solution, empireIndex + 1);
		}

		private bool FindExtraSpawn(int empireIndex, int spawnIndex = -1)
		{
			if (this.spawnsSolutions.Count == 0 && this.extraSpawns.Count > 0)
			{
				List<int> list = new List<int>();
				for (int i = 0; i < this.extraSpawns.Count; i++)
				{
					if ((int)this.extraSpawns[i].EmpireIndex == empireIndex)
					{
						list.Add(i);
						break;
					}
				}
				int index;
				if (list.Count != 0)
				{
					index = list[this.randomGenerator.Next(list.Count)];
				}
				else
				{
					index = this.randomGenerator.Next(this.extraSpawns.Count);
				}
				if (spawnIndex == -1)
				{
					this.spawns.Add(new ImportTmxSpawnLayer.Spawn(Convert.ToInt16(empireIndex), this.extraSpawns[index].RegionId, this.extraSpawns[index].Hexe));
				}
				else
				{
					this.spawns[spawnIndex] = new ImportTmxSpawnLayer.Spawn(Convert.ToInt16(empireIndex), this.extraSpawns[index].RegionId, this.extraSpawns[index].Hexe);
				}
				this.extraSpawns.RemoveAt(index);
				return true;
			}
			return false;
		}

		private void OverrideNumericSpawns()
		{
			List<EmpireDefinition> list = new List<EmpireDefinition>();
			list.AddRange(base.Context.Configuration.Empires);
			for (int i = 0; i < list.Count; i++)
			{
				if (this.affinitySpawns.ContainsKey(list[i].Name) && this.affinitySpawns[list[i].Name].Count > 0)
				{
					for (int j = 0; j < this.spawns.Count; j++)
					{
						if ((int)this.spawns[j].EmpireIndex == i)
						{
							this.extraSpawns.Add(this.spawns[j]);
							this.spawns.RemoveAt(j);
							j--;
						}
					}
					int index = this.randomGenerator.Next(this.affinitySpawns[list[i].Name].Count);
					this.spawns.Add(new ImportTmxSpawnLayer.Spawn(Convert.ToInt16(i), this.affinitySpawns[list[i].Name][index].RegionId, this.affinitySpawns[list[i].Name][0].Hexe));
					this.affinitySpawns[list[i].Name].RemoveAt(index);
				}
			}
		}

		private void ReserveUnusedAffinitySpawns()
		{
			foreach (KeyValuePair<string, List<ImportTmxSpawnLayer.Spawn>> keyValuePair in this.affinitySpawns)
			{
				List<ImportTmxSpawnLayer.Spawn> value = keyValuePair.Value;
				while (value.Count > 0)
				{
					this.extraSpawns.Add(value[0]);
					value.RemoveAt(0);
				}
			}
		}

		private const int RandomEmpireValue = 42;

		private static string spawnNameProperty = "Value";

		private Layer tmxSpawnLayer;

		private List<ImportTmxSpawnLayer.Spawn> spawns = new List<ImportTmxSpawnLayer.Spawn>();

		private List<ImportTmxSpawnLayer.Spawn[]> spawnsSolutions = new List<ImportTmxSpawnLayer.Spawn[]>();

		private Dictionary<string, List<ImportTmxSpawnLayer.Spawn>> affinitySpawns = new Dictionary<string, List<ImportTmxSpawnLayer.Spawn>>();

		private static string affinityNameProperty = "Affinity";

		private List<ImportTmxSpawnLayer.Spawn> extraSpawns = new List<ImportTmxSpawnLayer.Spawn>();

		private Random randomGenerator = new Random();

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
