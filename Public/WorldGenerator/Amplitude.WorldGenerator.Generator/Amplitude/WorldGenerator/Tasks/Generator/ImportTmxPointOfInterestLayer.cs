using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude.WorldGenerator.Graphs.Hex;
using Amplitude.WorldGenerator.Tmx;
using Amplitude.WorldGenerator.World;
using Amplitude.WorldGenerator.World.Info;

namespace Amplitude.WorldGenerator.Tasks.Generator
{
	public class ImportTmxPointOfInterestLayer : WorldGeneratorTask
	{
		public override void Execute(object context)
		{
			base.Report("?ImportTmxPointOfInterestLayer");
			base.Execute(context);
			if (base.Context.Settings.TmxMap == null)
			{
				throw new ArgumentException("[WorldGenerator][Exception] TmxMap can't be null");
			}
			this.tmxPointOfInterestLayer = base.Context.Settings.TmxMap.Layers.FirstOrDefault((Layer layer) => layer.Name == "PointOfInterests");
			if (this.tmxPointOfInterestLayer == null)
			{
				throw new ArgumentException("[WorldGenerator][Exception] TmxLayer PointOfInterests can't be null");
			}
			this.random = new Random(DateTime.Now.GetHashCode());
			this.GetPointOfInterests();
			this.CreateRidgesFromHexes();
			this.CheckMinimumStrategicDepositPerRegion();
		}

		private void GetPointOfInterests()
		{
			for (int i = 0; i < base.Context.Grid.Columns; i++)
			{
				for (int j = 0; j < base.Context.Grid.Rows; j++)
				{
					if (this.tmxPointOfInterestLayer.Data[j, i] != 0)
					{
						HexPos hexPos = new HexPos(i, j);
						bool flag = false;
						string poiName = base.Context.Settings.TmxMap.GetTileProperty(this.tmxPointOfInterestLayer.Data[j, i], ImportTmxPointOfInterestLayer.poiTypeNameProperty);
						if (string.IsNullOrEmpty(poiName))
						{
							base.ReportTmx(string.Concat(new object[]
							{
								"?MissingPropertyTile&$Coordinate=",
								i,
								",",
								j - base.Context.Grid.Rows + 1,
								"&$MissingProperty=",
								ImportTmxPointOfInterestLayer.poiTypeNameProperty
							}));
						}
						else
						{
							if (poiName.Contains("Facility"))
							{
								if (poiName == ImportTmxPointOfInterestLayer.randomFacilityName)
								{
									string[] array = (from template in base.Context.Settings.POITemplates.Values
									select template.Name into name
									where name.Contains("Facility")
									select name).ToArray<string>();
									poiName = array[this.random.Next(array.Length)];
								}
								bool flag2 = false;
								for (int k = 0; k < 6; k++)
								{
									HexPos hexPos2 = hexPos.Neighbour((HexPos.Direction)k);
									if (hexPos2.Row >= 0 && hexPos2.Row <= base.Context.Settings.Height && hexPos2.Column >= 0 && hexPos2.Column <= base.Context.Settings.Width && this.tmxPointOfInterestLayer.Data[hexPos2.Row, hexPos2.Column] != 0)
									{
										string tileProperty = base.Context.Settings.TmxMap.GetTileProperty(this.tmxPointOfInterestLayer.Data[hexPos2.Row, hexPos2.Column], ImportTmxPointOfInterestLayer.poiTypeNameProperty);
										if (!string.IsNullOrEmpty(tileProperty) && tileProperty.Contains("Citadel"))
										{
											if (flag2)
											{
												base.ReportTmx(string.Concat(new object[]
												{
													"?CitadelIncorrectPosition&$Coordinate=",
													hexPos2.Column,
													",",
													hexPos2.Row - base.Context.Grid.Rows + 1
												}));
												throw new TmxImportException();
											}
											flag2 = true;
										}
									}
								}
								if (!flag2)
								{
									goto IL_962;
								}
								flag = true;
							}
							if (poiName.Contains("Citadel"))
							{
								if (poiName == ImportTmxPointOfInterestLayer.randomCitadelName)
								{
									string[] array2 = (from template in base.Context.Settings.POITemplates.Values
									select template.Name into name
									where name.Contains("Citadel")
									select name).ToArray<string>();
									poiName = array2[this.random.Next(array2.Length)];
								}
								flag = true;
							}
							flag |= poiName.Contains("NavalQuestLocation_SunkenRuin");
							Terrain terrain = base.Context.GetTerrain(hexPos);
							if ((!flag && (terrain.Name.Contains("Ocean") || terrain.Name.Contains("Water"))) || terrain.Name.Contains("Waste"))
							{
								base.ReportTmx(string.Concat(new object[]
								{
									"?POIIncompatibleTerrain&$Coordinate=",
									hexPos.Column,
									",",
									hexPos.Row - base.Context.Grid.Rows + 1
								}));
							}
							else if (flag && !terrain.Name.Contains("Ocean") && !terrain.Name.Contains("Water"))
							{
								base.ReportTmx(string.Concat(new object[]
								{
									"?POIIncompatibleTerrain&$Coordinate=",
									hexPos.Column,
									",",
									hexPos.Row - base.Context.Grid.Rows + 1
								}));
							}
							else if (poiName == "Ridge")
							{
								if (base.Context.RiverMap[j, i] < 1)
								{
									this.ridgesHexes.Add(hexPos);
								}
								else
								{
									base.ReportTmx(string.Concat(new object[]
									{
										"?RidgeOnRiver&$Coordinate=",
										hexPos.Column,
										",",
										hexPos.Row - base.Context.Grid.Rows + 1
									}));
								}
							}
							else
							{
								if (poiName.Contains("Anomaly"))
								{
									if (poiName == ImportTmxPointOfInterestLayer.randomAnomalyName)
									{
										poiName = base.Context.Settings.AnomalyFIDS.Keys.ElementAt(this.random.Next(base.Context.Settings.AnomalyFIDS.Keys.Count));
									}
									if (base.Context.Anomalies.Keys.Contains(poiName))
									{
										if (!base.Context.Anomalies[poiName].Contains(hexPos))
										{
											base.Context.Anomalies[poiName].Add(hexPos);
										}
									}
									else
									{
										base.Context.Anomalies.Add(poiName, new List<HexPos>());
										base.Context.Anomalies[poiName].Add(hexPos);
									}
								}
								if (poiName.Contains("Village"))
								{
									if (poiName == ImportTmxPointOfInterestLayer.randomMinorFactionVillage)
									{
										string[] array3 = (from template in base.Context.Settings.POITemplates.Values
										select template.Name into name
										where name.Contains("Village_")
										select name).ToArray<string>();
										poiName = array3[this.random.Next(array3.Length)];
									}
									short key = base.Context.RegionData[j, i];
									Region region = base.Context.Regions[key];
									if (region == null || poiName.Split(new char[]
									{
										'_'
									}).Count<string>() < 2)
									{
										goto IL_962;
									}
									Region region2 = region;
									int villages = region2.Villages;
									region2.Villages = villages + 1;
									if (string.IsNullOrEmpty(region.MinorFactionName))
									{
										region.MinorFactionName = poiName.Split(new char[]
										{
											'_'
										})[1];
									}
									else
									{
										poiName = "Village_" + region.MinorFactionName;
									}
								}
								if (poiName == ImportTmxPointOfInterestLayer.randomStrategicResourceDepositName)
								{
									string[] array4 = (from template in base.Context.Settings.POITemplates.Values
									select template.Name into name
									where name.Contains("ResourceDeposit_Strategic")
									select name).ToArray<string>();
									poiName = array4[this.random.Next(array4.Length)];
								}
								if (poiName == ImportTmxPointOfInterestLayer.randomLuxuryResourceDepositName)
								{
									string[] array5 = (from template in base.Context.Settings.POITemplates.Values
									select template.Name into name
									where name.Contains("ResourceDeposit_Luxury")
									select name).ToArray<string>();
									poiName = array5[this.random.Next(array5.Length)];
								}
								PointOfInterestTemplate pointOfInterestTemplate = base.Context.Settings.POITemplates.Values.FirstOrDefault((PointOfInterestTemplate template) => template.Name == poiName);
								if (pointOfInterestTemplate != null)
								{
									if (poiName.Contains("ResourceDeposit"))
									{
										short key2 = base.Context.RegionData[j, i];
										Region region3 = base.Context.Regions[key2];
										if (region3 != null)
										{
											region3.Resources.Add(new PointOfInterestDefinition
											{
												Position = new HexPos(i, j),
												TemplateName = pointOfInterestTemplate.Name
											});
										}
									}
									base.Context.POIDefinitions.Add(new PointOfInterestDefinition
									{
										Position = new HexPos(i, j),
										TemplateName = pointOfInterestTemplate.Name
									});
								}
							}
						}
					}
					IL_962:;
				}
			}
		}

		private void CreateRidgesFromHexes()
		{
			while (this.ridgesHexes.Count > 0)
			{
				Ridge ridge = new Ridge();
				ridge.Hexes.Add(this.ridgesHexes[0]);
				this.ridgesHexes.RemoveAt(0);
				Queue<HexPos> queue = new Queue<HexPos>();
				queue.Enqueue(ridge.Hexes[0]);
				while (queue.Count > 0)
				{
					HexPos pos = queue.Dequeue();
					foreach (HexPos item in base.Context.Grid.Adjacents(pos))
					{
						int num = this.tmxPointOfInterestLayer.Data[item.Row, item.Column];
						if (num != 0 && base.Context.Settings.TmxMap.GetTileProperty(num, ImportTmxPointOfInterestLayer.poiTypeNameProperty) == "Ridge" && !ridge.Hexes.Contains(item))
						{
							ridge.Hexes.Add(item);
							this.ridgesHexes.Remove(item);
							queue.Enqueue(item);
						}
					}
				}
				base.Context.Ridges.Add(ridge);
			}
		}

		private void CheckMinimumStrategicDepositPerRegion()
		{
			foreach (Region region in base.Context.Regions.Values)
			{
				if (region.LandMassType != Region.LandMassTypes.Ocean)
				{
					if (region.Resources.Count((PointOfInterestDefinition resource) => resource.TemplateName.Contains("ResourceDeposit_Strategic")) < 6)
					{
						base.ReportTmx("?MinimumStrategicDeposit&$RegionName=" + region.Name);
					}
				}
			}
		}

		private static string poiTypeNameProperty = "Value";

		private static string randomAnomalyName = "AnomalyRandom";

		private static string randomStrategicResourceDepositName = "ResourceDeposit_StrategicRandom";

		private static string randomLuxuryResourceDepositName = "ResourceDeposit_LuxuryRandom";

		private static string randomMinorFactionVillage = "Village_Random";

		private static string randomFacilityName = "Facility_Random";

		private static string randomCitadelName = "Citadel_Random";

		private Layer tmxPointOfInterestLayer;

		private List<HexPos> ridgesHexes = new List<HexPos>();

		private Random random;
	}
}
