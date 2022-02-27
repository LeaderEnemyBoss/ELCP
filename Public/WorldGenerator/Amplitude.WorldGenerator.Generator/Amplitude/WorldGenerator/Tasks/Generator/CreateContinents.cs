using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude.WorldGenerator.Graphs;
using Amplitude.WorldGenerator.Graphs.Hex;
using Amplitude.WorldGenerator.World;
using Amplitude.WorldGenerator.World.Info;

namespace Amplitude.WorldGenerator.Tasks.Generator
{
	public class CreateContinents : WorldGeneratorTask
	{
		public override void Execute(object context)
		{
			base.Report("?CreateContinents");
			CreateContinents.Blob.CurrentId = 0;
			CreateContinents.Continent.CurrentId = 0;
			base.Execute(context);
			this.RemoveELCPSpectatorEmpires();
			this.CreateBlobs();
			this.NeutralizeOceanEdgeBlobs();
			this.UnifyBlobGraph();
			this.InitializeContinents();
			this.ApplyScenarioRules();
			if (!base.Context.IsOnlyLand)
			{
				this.GrowSmoothLandMasses(base.Context.Settings.ContinentShaping, base.Context.Settings.ContinentSpreading);
				this.CullDegenerateLandMasses();
				this.MakeIslands(base.Context.Settings.IslandsMinimalSize, base.Context.Settings.IslandsPresencePercent);
			}
			this.ConfirmContinents();
			this.PrepareCoastalDistricts();
			this.CutCoastline();
			CreateContinents.FinalizeCoastalSkeleton(base.Context);
		}

		protected void ApplyScenarioRules()
		{
			if (base.Context.Settings.Scenario == null)
			{
				return;
			}
			List<Rule> list = new List<Rule>(from r in base.Context.Settings.Scenario.Rules
			where r.GetRuleType == Rule.RuleType.ContinentSize
			where r.Continent >= 1 && r.Continent <= this.Continents.Count
			select r);
			if (list.Count < 1)
			{
				return;
			}
			Dictionary<int, Rule> dictionary = new Dictionary<int, Rule>();
			foreach (Rule rule in list)
			{
				if (!dictionary.ContainsKey(rule.Continent))
				{
					dictionary.Add(rule.Continent, rule);
				}
				else if (rule.PriorityRank < dictionary[rule.Continent].PriorityRank)
				{
					dictionary[rule.Continent] = rule;
				}
			}
			foreach (int num in dictionary.Keys)
			{
				this.Continents[num - 1].RelativeSize = dictionary[num].RelativeSize;
				base.Trace(string.Format("Applying rule {0} on continent {1} : size {2}", dictionary[num].GetRuleType.ToString(), dictionary[num].Continent, dictionary[num].RelativeSize));
			}
		}

		protected void CreateBlobs()
		{
			this.BlobMap = new Dictionary<District, CreateContinents.Blob>();
			List<District> list = new List<District>(from d in base.Context.Districts.Values
			where d.Content != District.Contents.WasteNS
			where d.Content != District.Contents.WasteEW
			select d);
			AdHocGraph<District> graph = new AdHocGraph<District>(list);
			RandomScatter<District> randomScatter = new RandomScatter<District>(graph);
			randomScatter.Randomizer = base.Context.Randomizer;
			randomScatter.Shots = Math.Min(list.Count, 500);
			randomScatter.Execute();
			SeedGrower<District, CreateContinents.Blob> seedGrower = new SeedGrower<District, CreateContinents.Blob>(graph)
			{
				Randomizer = base.Context.Randomizer,
				Blank = null
			};
			foreach (District key in randomScatter.Impacts)
			{
				seedGrower.Seeds.Add(key, new CreateContinents.Blob(this.BlobMap));
			}
			seedGrower.Execute();
			for (int i = 0; i < seedGrower.Graph.Nodes; i++)
			{
				District district = seedGrower.Graph.Node(i);
				CreateContinents.Blob blob = seedGrower.SeededGraph[i];
				this.BlobMap.Add(district, blob);
				blob.Districts.Add(district);
			}
			this.CoreBlobs = new List<CreateContinents.Blob>(this.BlobMap.Values.Distinct<CreateContinents.Blob>());
		}

		protected void NeutralizeOceanEdgeBlobs()
		{
			if (base.Context.Settings.ForceOceansOnMapEdges)
			{
				new List<CreateContinents.Blob>(this.BlobMap.Values.Where(delegate(CreateContinents.Blob b)
				{
					if (!b.Districts.Any((District d) => d.IsGridEdge(base.Context.Grid)))
					{
						return b.Districts.Any((District d) => d.Neighbours.Any((District n) => n.Content > District.Contents.Ocean));
					}
					return true;
				})).ForEach(delegate(CreateContinents.Blob b)
				{
					this.CoreBlobs.Remove(b);
				});
			}
			this.BlobGraph = new AdHocGraph<CreateContinents.Blob>(this.CoreBlobs);
		}

		protected void UnifyBlobGraph()
		{
			ConnectivityChecker<CreateContinents.Blob> blobConnexer = new ConnectivityChecker<CreateContinents.Blob>(this.BlobGraph);
			blobConnexer.Execute();
			if (blobConnexer.ConnexNodeSets.Count > 1)
			{
				this.CoreBlobs.RemoveAll((CreateContinents.Blob b) => !blobConnexer.ConnexNodeSets.First<HashSet<CreateContinents.Blob>>().Contains(b));
				this.BlobGraph = new AdHocGraph<CreateContinents.Blob>(this.CoreBlobs);
			}
		}

		protected int MinRegionCount
		{
			get
			{
				return Math.Max(base.Context.CurrentRequestedLandMasses, base.Context.EmpiresCount);
			}
		}

		protected void InitializeContinents()
		{
			this.Continents = new List<CreateContinents.Continent>();
			this.CoreHexes = this.CoreBlobs.Sum((CreateContinents.Blob b) => b.Districts.Sum((District d) => d.Count));
			int num = Math.Max(1, base.Context.Settings.LandPrevalence);
			int num2 = Math.Max(0, base.Context.Settings.OceanPrevalence);
			this.ExpectedLandHexes = this.CoreHexes * num / (num + num2);
			base.Trace(string.Format("Expected land hexes : {0}", this.ExpectedLandHexes));
			this.ExpectedLandHexes = Math.Max(this.ExpectedLandHexes, this.MinRegionCount * base.Context.Settings.ExpectedRegionArea);
			base.Trace(string.Format("Expected land hexes : {0}", this.ExpectedLandHexes));
			this.ExpectedLandHexes = Math.Min(this.ExpectedLandHexes, this.CoreHexes);
			base.Trace(string.Format("Expected land hexes : {0}", this.ExpectedLandHexes));
			base.Context.Settings.ExpectedRegionArea = Math.Min(base.Context.Settings.ExpectedRegionArea, this.ExpectedLandHexes / this.MinRegionCount);
			if (num2 <= 0)
			{
				base.Context.IsOnlyLand = true;
				this.Continents.Add(new CreateContinents.Continent(this.BlobGraph));
				foreach (CreateContinents.Blob blob in this.CoreBlobs)
				{
					blob.Content = District.Contents.Land;
					blob.Districts.ForEach(delegate(District d)
					{
						d.Content = District.Contents.Land;
					});
					this.Continents.First<CreateContinents.Continent>().Blobs.Add(blob);
				}
				return;
			}
			int num3 = Math.Max(1, base.Context.CurrentRequestedLandMasses);
			num3 = Math.Min(num3, this.CoreBlobs.Count);
			for (int i = 1; i <= num3; i++)
			{
				CreateContinents.Continent item = new CreateContinents.Continent(this.BlobGraph);
				this.Continents.Add(item);
			}
			int num4 = 0;
			for (int j = Math.Max(base.Context.EmpiresCount, base.Context.CurrentRequestedLandMasses); j > 0; j--)
			{
				this.Continents[num4].MinArea += base.Context.Settings.ExpectedRegionArea;
				num4 = (num4 + 1) % this.Continents.Count;
			}
			this.Continents.ForEach(delegate(CreateContinents.Continent c)
			{
				c.MinArea = 9 * c.MinArea / 10;
			});
		}

		protected void GrowSmoothLandMasses(WorldGeneratorSettings.ContinentStyles shaping, WorldGeneratorSettings.ContinentStyles spreading)
		{
			this.AvailableBlobs = new HashSet<CreateContinents.Blob>(this.CoreBlobs);
			this.Influencer = new IncrementalInfluencer<CreateContinents.Blob>(this.BlobGraph);
			List<CreateContinents.Blob> list = new List<CreateContinents.Blob>(this.AvailableBlobs);
			Dictionary<CreateContinents.Continent, Dictionary<CreateContinents.Blob, int>> valuations = new Dictionary<CreateContinents.Continent, Dictionary<CreateContinents.Blob, int>>();
			foreach (CreateContinents.Continent continent in this.Continents)
			{
				CreateContinents.Blob blob3 = null;
				if (spreading == WorldGeneratorSettings.ContinentStyles.Regular)
				{
					List<CreateContinents.Blob> list2 = this.Influencer.FarthestNodes();
					blob3 = list2.ElementAt(base.Context.Randomizer.Next(list2.Count));
					this.Influencer.Add(blob3);
				}
				else if (list.Count > 0)
				{
					blob3 = list.ElementAt(base.Context.Randomizer.Next(list.Count));
				}
				if (!base.Context.Settings.WorldWrap && this.Continents.Count == 1)
				{
					HexPos mapCenter = new HexPos(base.Context.Grid.Columns / 2, base.Context.Grid.Rows / 2);
					Func<District, bool> <>9__1;
					blob3 = this.CoreBlobs.Find(delegate(CreateContinents.Blob b)
					{
						IEnumerable<District> districts = b.Districts;
						Func<District, bool> predicate;
						if ((predicate = <>9__1) == null)
						{
							predicate = (<>9__1 = ((District d) => d.Contains(mapCenter)));
						}
						return districts.Any(predicate);
					});
				}
				if (blob3 != null)
				{
					this.AvailableBlobs.Remove(blob3);
					continent.Blobs.Add(blob3);
					list.Remove(blob3);
					foreach (CreateContinents.Blob item in this.BlobGraph.Adjacents(blob3))
					{
						list.Remove(item);
					}
					valuations.Add(continent, new Dictionary<CreateContinents.Blob, int>());
					continent.ProxBlober.StartingNodes.Add(blob3);
					continent.ProxBlober.Execute();
					continent.ComputeInfluence();
				}
			}
			foreach (CreateContinents.Continent continent2 in this.Continents)
			{
				Dictionary<CreateContinents.Blob, int> dictionary = valuations[continent2];
				List<CreateContinents.Continent> list3 = new List<CreateContinents.Continent>(this.Continents);
				list3.Remove(continent2);
				using (HashSet<CreateContinents.Blob>.Enumerator enumerator3 = this.AvailableBlobs.GetEnumerator())
				{
					while (enumerator3.MoveNext())
					{
						CreateContinents.Blob blob = enumerator3.Current;
						if (list3.Count > 0)
						{
							dictionary.Add(blob, list3.Min((CreateContinents.Continent k) => k.ProxBlober.Proximity(blob)));
						}
						else
						{
							dictionary.Add(blob, 0);
						}
						Dictionary<CreateContinents.Blob, int> dictionary2 = dictionary;
						CreateContinents.Blob blob2 = blob;
						dictionary2[blob2] -= continent2.ProxBlober.Proximity(blob);
						if (shaping == WorldGeneratorSettings.ContinentStyles.Chaotic)
						{
							dictionary2 = dictionary;
							blob2 = blob;
							dictionary2[blob2] += base.Context.Randomizer.Next(8) - base.Context.Randomizer.Next(8);
						}
					}
				}
			}
			bool flag = true;
			for (;;)
			{
				if (this.Continents.Sum((CreateContinents.Continent k) => k.HexCount) >= this.ExpectedLandHexes || !flag)
				{
					break;
				}
				flag = false;
				List<CreateContinents.Continent> source = new List<CreateContinents.Continent>(from k in this.Continents
				orderby k.WeightedHexCount
				select k);
				CreateContinents.Continent smallest = source.First<CreateContinents.Continent>();
				List<CreateContinents.Continent> others = new List<CreateContinents.Continent>(this.Continents);
				others.Remove(smallest);
				List<CreateContinents.Blob> list4 = new List<CreateContinents.Blob>(from b in this.AvailableBlobs
				where b.Neighbours.Any((CreateContinents.Blob n) => smallest.Blobs.Contains(n))
				where !others.Any((CreateContinents.Continent k) => k.Influence.Contains(b))
				orderby valuations[smallest][b] descending
				select b);
				if (list4.Count > 0)
				{
					flag = true;
					CreateContinents.Blob item2 = list4.First<CreateContinents.Blob>();
					smallest.Blobs.Add(item2);
					this.AvailableBlobs.Remove(item2);
					smallest.ComputeInfluence();
				}
			}
		}

		protected void CullDegenerateLandMasses()
		{
			List<CreateContinents.Continent> vitals = new List<CreateContinents.Continent>(from c in this.Continents
			where c.HexCount >= base.Context.Settings.ExpectedRegionArea
			where c.HexCount >= c.MinArea
			orderby c.HexCount descending
			select c);
			int num = Math.Max(base.Context.EmpiresCount, base.Context.CurrentRequestedLandMasses);
			if (vitals.Count > num)
			{
				vitals = new List<CreateContinents.Continent>(vitals.GetRange(0, num));
				new List<CreateContinents.Continent>(from c in this.Continents
				where !vitals.Contains(c)
				where c.HexCount < c.MinArea || c.HexCount < 2 * base.Context.Settings.ExpectedRegionArea / 3
				select c).ForEach(delegate(CreateContinents.Continent c)
				{
					this.Continents.Remove(c);
				});
				return;
			}
		}

		protected void MakeIslands(int islandSize, int keepPercent)
		{
			CreateContinents.<>c__DisplayClass14_0 CS$<>8__locals1 = new CreateContinents.<>c__DisplayClass14_0();
			CS$<>8__locals1.<>4__this = this;
			CreateContinents.<>c__DisplayClass14_0 CS$<>8__locals2 = CS$<>8__locals1;
			ProximityComputer<CreateContinents.Blob> proximityComputer = new ProximityComputer<CreateContinents.Blob>(this.BlobGraph);
			proximityComputer.StartingNodes = new List<CreateContinents.Blob>(from c in this.Continents
			from b in c.Blobs
			select b);
			CS$<>8__locals2.proxer = proximityComputer;
			CS$<>8__locals1.proxer.Execute();
			CS$<>8__locals1.potentialIslands = new List<CreateContinents.Blob>(from b in this.CoreBlobs
			where CS$<>8__locals1.proxer.Proximity(b) > 1
			select b);
			List<CreateContinents.Blob> list = new List<CreateContinents.Blob>();
			while (CS$<>8__locals1.potentialIslands.Count > 0 && this.Continents.Count < 254)
			{
				List<CreateContinents.Blob> island = new List<CreateContinents.Blob>();
				CreateContinents.Blob blob = CS$<>8__locals1.potentialIslands.ElementAt(base.Context.Randomizer.Next(CS$<>8__locals1.potentialIslands.Count));
				CS$<>8__locals1.potentialIslands.Remove(blob);
				island.Add(blob);
				list.Clear();
				List<CreateContinents.Blob> list2 = list;
				IEnumerable<CreateContinents.Blob> source = this.BlobGraph.Adjacents(blob);
				Func<CreateContinents.Blob, bool> predicate;
				if ((predicate = CS$<>8__locals1.<>9__3) == null)
				{
					predicate = (CS$<>8__locals1.<>9__3 = ((CreateContinents.Blob n) => CS$<>8__locals1.potentialIslands.Contains(n)));
				}
				list2.AddRange(source.Where(predicate));
				while (list.Count > 0)
				{
					if (island.Sum((CreateContinents.Blob b) => b.Districts.Sum((District d) => d.Count)) >= islandSize)
					{
						break;
					}
					CreateContinents.Blob item = list.ElementAt(base.Context.Randomizer.Next(list.Count));
					island.Add(item);
					CS$<>8__locals1.potentialIslands.Remove(item);
					list.Clear();
					List<CreateContinents.Blob> list3 = list;
					var source2 = from b in island
					from n in this.BlobGraph.Adjacents(b)
					select new
					{
						b,
						n
					};
					var predicate2;
					if ((predicate2 = CS$<>8__locals1.<>9__6) == null)
					{
						predicate2 = (CS$<>8__locals1.<>9__6 = (<>h__TransparentIdentifier0 => CS$<>8__locals1.potentialIslands.Contains(<>h__TransparentIdentifier0.n)));
					}
					list3.AddRange(from <>h__TransparentIdentifier0 in source2.Where(predicate2)
					select <>h__TransparentIdentifier0.n);
				}
				if (island.Count > 0)
				{
					if (island.Sum((CreateContinents.Blob b) => b.Districts.Sum((District d) => d.Count)) < 3 * islandSize)
					{
						if (island.Sum((CreateContinents.Blob b) => b.Districts.Sum((District d) => d.Count)) > islandSize && base.Context.Randomizer.Next(100) < keepPercent)
						{
							CreateContinents.Continent continent = new CreateContinents.Continent(this.BlobGraph);
							this.Continents.Add(continent);
							island.ForEach(delegate(CreateContinents.Blob b)
							{
								b.Content = District.Contents.Land;
							});
							island.ForEach(delegate(CreateContinents.Blob b)
							{
								b.Districts.ForEach(delegate(District d)
								{
									d.Content = District.Contents.Land;
								});
							});
							continent.Blobs.UnionWith(island);
							Func<CreateContinents.Blob, bool> <>9__18;
							CS$<>8__locals1.potentialIslands.RemoveAll(delegate(CreateContinents.Blob b)
							{
								IEnumerable<CreateContinents.Blob> source3 = CS$<>8__locals1.<>4__this.BlobGraph.Adjacents(b);
								Func<CreateContinents.Blob, bool> predicate3;
								if ((predicate3 = <>9__18) == null)
								{
									predicate3 = (<>9__18 = ((CreateContinents.Blob n) => island.Contains(n)));
								}
								return source3.Any(predicate3);
							});
						}
					}
				}
			}
		}

		private void CullDegenerateOceans()
		{
			ConnectivityChecker<District> connectivityChecker = new ConnectivityChecker<District>(new AdHocGraph<District>(new List<District>(from d in base.Context.Districts.Values
			where d.Content == District.Contents.Ocean
			select d)));
			connectivityChecker.Execute();
			foreach (HashSet<District> hashSet in connectivityChecker.ConnexNodeSets)
			{
				if (hashSet.Sum((District d) => d.Count) < 30)
				{
					HashSet<byte> source = new HashSet<byte>(from d in hashSet
					from n in d.Neighbours
					where n.ContinentId > 0
					select n.ContinentId);
					foreach (District district in hashSet)
					{
						district.Content = District.Contents.Land;
						district.ContinentId = source.First<byte>();
						foreach (HexPos hexPos in district)
						{
						}
					}
				}
			}
		}

		protected void ConfirmContinents()
		{
			foreach (CreateContinents.Continent continent in this.Continents)
			{
				foreach (CreateContinents.Blob blob in continent.Blobs)
				{
					foreach (District district in blob.Districts)
					{
						district.Content = District.Contents.Land;
						district.ContinentId = (byte)continent.Id;
					}
				}
				string format = "Continent {0} : {1} hexes - {2} blobs - {3} districts";
				object[] array = new object[4];
				array[0] = continent.Id;
				array[1] = continent.HexCount;
				array[2] = continent.Blobs.Count;
				array[3] = continent.Blobs.Sum((CreateContinents.Blob b) => b.Districts.Count);
				base.Trace(string.Format(format, array));
			}
			this.CullDegenerateOceans();
			ConnectivityChecker<District> connectivityChecker = new ConnectivityChecker<District>(new AdHocGraph<District>(from d in base.Context.Districts.Values
			where d.Content == District.Contents.Land
			select d));
			connectivityChecker.Execute();
			base.Context.Settings.LandMasses = connectivityChecker.ConnexNodeSets.Count;
		}

		protected void PrepareCoastalDistricts()
		{
			ProximityComputer<District> proximityComputer = new ProximityComputer<District>(base.Context.Districts);
			proximityComputer.StartingNodes = new List<District>(from d in base.Context.Districts.Values
			where d.Content == District.Contents.Ocean
			select d);
			ProximityComputer<District> proximityComputer2 = proximityComputer;
			if (proximityComputer2.StartingNodes.Count <= 0)
			{
				base.Context.IsOnlyLand = true;
				base.Context.Districts.Values.ToList<District>().ForEach(delegate(District d)
				{
					d.CoastalSkeletonValue = 999;
				});
				return;
			}
			base.Context.IsOnlyLand = false;
			proximityComputer2.Execute();
			for (int i = 0; i < base.Context.Districts.Nodes; i++)
			{
				base.Context.Districts[i].CoastalSkeletonValue = proximityComputer2.ProximityGraph[i];
			}
			ProximityComputer<District> proximityComputer3 = new ProximityComputer<District>(base.Context.Districts);
			proximityComputer3.StartingNodes = new List<District>(from d in base.Context.Districts.Values
			where d.Content == District.Contents.Land
			select d);
			ProximityComputer<District> proximityComputer4 = proximityComputer3;
			proximityComputer4.Execute();
			for (int j = 0; j < base.Context.Districts.Nodes; j++)
			{
				District district = base.Context.Districts[j];
				if (district.CoastalSkeletonValue == 0)
				{
					district.CoastalSkeletonValue = 1 - proximityComputer4.ProximityGraph[j];
				}
			}
		}

		protected void CutCoastline()
		{
			List<District> list = new List<District>(from d in base.Context.Districts.Values
			where d.CoastalSkeletonValue == 0
			select d);
			List<District> list2 = new List<District>(from d in base.Context.Districts.Values
			where d.CoastalSkeletonValue != 0
			select d);
			List<District> list3 = new List<District>();
			foreach (District district in list)
			{
				foreach (HexPos hexPos in district)
				{
					District district2 = new District();
					district2.Add(hexPos);
					if (base.Context.Grid.Adjacents(hexPos).Any((HexPos h) => base.Context.Districts[base.Context.DistrictData[h.Row, h.Column]].Content == District.Contents.Land))
					{
						district2.Content = District.Contents.Coastal;
					}
					else
					{
						district2.Content = District.Contents.Ocean;
					}
					list3.Add(district2);
				}
			}
			base.Context.Districts.Clear();
			int num = 0;
			foreach (District district3 in list2)
			{
				district3.Id = num++;
				base.Context.Districts.Add(district3.Id, district3);
			}
			foreach (District district4 in list3)
			{
				district4.Id = num++;
				base.Context.Districts.Add(district4.Id, district4);
			}
			base.ExecuteSubTask(new ComputeDistrictNeighbourhood());
		}

		public static void FinalizeCoastalSkeleton(WorldGeneratorContext context)
		{
			CreateContinents.<>c__DisplayClass19_0 CS$<>8__locals1 = new CreateContinents.<>c__DisplayClass19_0();
			CS$<>8__locals1.context = context;
			CreateContinents.<>c__DisplayClass19_0 CS$<>8__locals2 = CS$<>8__locals1;
			ProximityComputer<District> proximityComputer = new ProximityComputer<District>(CS$<>8__locals1.context.Districts);
			proximityComputer.StartingNodes = new List<District>(from d in CS$<>8__locals1.context.Districts.Values
			where d.Content == District.Contents.Coastal || d.Content == District.Contents.WasteEW || d.Content == District.Contents.WasteNS
			select d);
			CS$<>8__locals2.proxer = proximityComputer;
			List<District> list = null;
			if (CS$<>8__locals1.context.IsSymmetrical)
			{
				list = new List<District>(from d in CS$<>8__locals1.context.Districts.Values
				where d.Any((HexPos h) => h.Column == 0 || h.Column == CS$<>8__locals1.context.Grid.Columns - 1)
				select d);
				list.ForEach(delegate(District d)
				{
					CS$<>8__locals1.proxer.StartingNodes.Add(d);
				});
			}
			CS$<>8__locals1.proxer.Execute();
			for (int i = 0; i < CS$<>8__locals1.context.Districts.Nodes; i++)
			{
				District district = CS$<>8__locals1.context.Districts[i];
				if (district.Content == District.Contents.Ocean || district.Content == District.Contents.WasteEW)
				{
					district.CoastalSkeletonValue = -CS$<>8__locals1.proxer.ProximityGraph[i];
				}
				else if (district.Content == District.Contents.Land || district.Content == District.Contents.Coastal)
				{
					district.CoastalSkeletonValue = CS$<>8__locals1.proxer.ProximityGraph[i];
				}
				else if (district.Content == District.Contents.WasteNS)
				{
					district.CoastalSkeletonValue = 99;
				}
				int num = -1;
				if (district.CoastalSkeletonValue > 0)
				{
					num = 1;
				}
				else if (district.CoastalSkeletonValue == 0)
				{
					num = 2;
				}
				else if (district.CoastalSkeletonValue < 0)
				{
					num = 0;
				}
				if (num >= 0 && num <= 3)
				{
					foreach (HexPos hexPos in district)
					{
					}
				}
			}
			if (CS$<>8__locals1.context.IsSymmetrical)
			{
				list.ForEach(delegate(District d)
				{
					int coastalSkeletonValue = d.CoastalSkeletonValue;
					d.CoastalSkeletonValue = coastalSkeletonValue - 1;
				});
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

		protected int CoreHexes;

		protected int ExpectedLandHexes;

		protected HashSet<CreateContinents.Blob> AvailableBlobs;

		private IncrementalInfluencer<CreateContinents.Blob> Influencer;

		protected Dictionary<District, CreateContinents.Blob> BlobMap;

		protected List<CreateContinents.Blob> CoreBlobs;

		protected AdHocGraph<CreateContinents.Blob> BlobGraph;

		protected List<CreateContinents.Continent> Continents;

		protected class Blob : IHasNeighbours<CreateContinents.Blob>, IEquatable<CreateContinents.Blob>
		{
			public Blob(Dictionary<District, CreateContinents.Blob> blobMap)
			{
				this.BlobMap = blobMap;
				this.Districts = new List<District>();
				this.Content = District.Contents.Ocean;
				this.Id = CreateContinents.Blob.CurrentId++;
			}

			public List<CreateContinents.Blob> Neighbours
			{
				get
				{
					return new List<CreateContinents.Blob>((from d in this.Districts
					from n in d.Neighbours
					where !this.Districts.Contains(n)
					where this.BlobMap.ContainsKey(n)
					select this.BlobMap[n]).Distinct<CreateContinents.Blob>());
				}
			}

			public bool Equals(CreateContinents.Blob other)
			{
				return this.Id == other.Id;
			}

			public static int CurrentId;

			public readonly Dictionary<District, CreateContinents.Blob> BlobMap;

			public List<District> Districts;

			public District.Contents Content;

			public int Id;
		}

		protected class Continent
		{
			public Continent(AdHocGraph<CreateContinents.Blob> graph)
			{
				this.Graph = graph;
				this.Blobs = new HashSet<CreateContinents.Blob>();
				this.Influence = new HashSet<CreateContinents.Blob>();
				this.Growth = new HashSet<CreateContinents.Blob>();
				this.ProxBlober = new ProximityComputer<CreateContinents.Blob>(this.Graph);
				this.RelativeSize = 100;
				this.Id = ++CreateContinents.Continent.CurrentId;
			}

			public int HexCount
			{
				get
				{
					return this.Blobs.Sum((CreateContinents.Blob b) => b.Districts.Sum((District d) => d.Count));
				}
			}

			public int WeightedHexCount
			{
				get
				{
					return this.HexCount * 100 / this.RelativeSize;
				}
			}

			public void ComputeInfluence()
			{
				this.Influence.Clear();
				this.Influence.UnionWith(this.Blobs);
				foreach (CreateContinents.Blob node in this.Blobs)
				{
					this.Influence.UnionWith(this.Graph.Adjacents(node));
				}
			}

			public static int CurrentId;

			public int Id;

			public HashSet<CreateContinents.Blob> Blobs;

			public HashSet<CreateContinents.Blob> Influence;

			public HashSet<CreateContinents.Blob> Growth;

			public bool IsClosed;

			public int MinArea;

			public ProximityComputer<CreateContinents.Blob> ProxBlober;

			public int RelativeSize;

			protected readonly AdHocGraph<CreateContinents.Blob> Graph;
		}
	}
}
