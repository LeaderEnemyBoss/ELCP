using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amplitude;
using Amplitude.Extensions;
using Amplitude.Unity.Framework;
using Amplitude.Utilities.Maps;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class Cadaster : GameAncillary, IXmlSerializable, IService, IDumpable, ICadasterService
{
	public event EventHandler<RoadEventArgs> RoadRegistered;

	public event EventHandler<RoadEventArgs> RoadUnregistered;

	public event EventHandler<RoadEventArgs> RoadModified;

	Road ICadasterService.this[ushort index]
	{
		get
		{
			return this.roads[(int)index];
		}
	}

	ushort[] ICadasterService.Connect(City city, PathfindingMovementCapacity movementCapacity, bool proxied)
	{
		if (city == null)
		{
			throw new ArgumentNullException("city");
		}
		if ((movementCapacity & PathfindingMovementCapacity.Water) == PathfindingMovementCapacity.Water)
		{
			StaticString type = new StaticString("DistrictImprovement");
			StaticString y = new StaticString("DistrictImprovementDocks");
			WorldPosition start = WorldPosition.Invalid;
			for (int i = 0; i < city.Districts.Count; i++)
			{
				District district = city.Districts[i];
				if (district.Type == DistrictType.Improvement && district.GetDescriptorNameFromType(type) == y)
				{
					start = district.WorldPosition;
					break;
				}
			}
			if (!start.IsValid)
			{
				return null;
			}
			DepartmentOfForeignAffairs agency = city.Empire.GetAgency<DepartmentOfForeignAffairs>();
			this.OceanPathfindingWorldContext.RegionIndexList.Clear();
			this.OceanPathfindingWorldContext.RegionIndexList.Add((int)this.WorldPositionningService.GetRegionIndex(city.WorldPosition));
			for (int j = 0; j < city.Region.Borders.Length; j++)
			{
				Region region = this.WorldPositionningService.GetRegion(city.Region.Borders[j].NeighbourRegionIndex);
				if (region.IsOcean && (region.Owner == null || !(region.Owner is MajorEmpire) || region.Owner.Index == city.Empire.Index || agency.GetDiplomaticRelation(region.Owner).HasActiveAbility(DiplomaticAbilityDefinition.TradeRoute)))
				{
					this.OceanPathfindingWorldContext.RegionIndexList.Add(city.Region.Borders[j].NeighbourRegionIndex);
				}
			}
			Diagnostics.Assert(city.CadastralMap != null);
			Diagnostics.Assert((city.CadastralMap.ConnectedMovementCapacity & PathfindingMovementCapacity.Water) == PathfindingMovementCapacity.Water);
			List<ushort> list = new List<ushort>();
			List<Region> list2 = new List<Region>();
			List<Region> list3 = new List<Region>();
			list3.Add(city.Region);
			int k = 0;
			int l = 2;
			while (l > 0)
			{
				l--;
				list2.AddRange(list3);
				list3.Clear();
				while (k < list2.Count)
				{
					Region region2 = list2[k];
					for (int m = 0; m < region2.Borders.Length; m++)
					{
						short regionIndex = (short)region2.Borders[m].NeighbourRegionIndex;
						Region region3 = this.WorldPositionningService.GetRegion((int)regionIndex);
						if (region3 != null && !list2.Contains(region3))
						{
							if (region3.City != null)
							{
								Diagnostics.Assert(region3.City.CadastralMap != null);
								if ((region3.City.CadastralMap.ConnectedMovementCapacity & PathfindingMovementCapacity.Water) == PathfindingMovementCapacity.Water)
								{
									for (int n = 0; n < region3.City.Districts.Count; n++)
									{
										District district2 = region3.City.Districts[n];
										if (district2.Type == DistrictType.Improvement && district2.GetDescriptorNameFromType(type) == y)
										{
											PathfindingFlags pathfindingFlags = PathfindingFlags.IgnoreAll;
											pathfindingFlags &= ~PathfindingFlags.IgnoreMovementCapacities;
											pathfindingFlags &= ~PathfindingFlags.IgnorePOI;
											PathfindingContext pathfindingContext = new PathfindingContext(GameEntityGUID.Zero, city.Empire, PathfindingMovementCapacity.Water);
											pathfindingContext.RefreshProperties(1f, 1f, false, false, 1f, 1f);
											bool flag = false;
											if (!this.OceanPathfindingWorldContext.RegionIndexList.Contains(region2.Borders[m].NeighbourRegionIndex))
											{
												flag = true;
												this.OceanPathfindingWorldContext.RegionIndexList.Add(region2.Borders[m].NeighbourRegionIndex);
											}
											PathfindingResult pathfindingResult = this.PathfindingService.FindPath(pathfindingContext, start, district2.WorldPosition, PathfindingManager.RequestMode.Default, this.OceanPathfindingWorldContext, pathfindingFlags, null);
											if (flag)
											{
												this.OceanPathfindingWorldContext.RegionIndexList.Remove(region2.Borders[m].NeighbourRegionIndex);
											}
											if (pathfindingResult == null)
											{
												pathfindingResult = this.PathfindingService.FindPath(pathfindingContext, start, district2.WorldPosition, PathfindingManager.RequestMode.Default, null, pathfindingFlags, null);
											}
											if (pathfindingResult != null)
											{
												ushort item = this.Reserve(new Road
												{
													FromRegion = (short)city.Region.Index,
													ToRegion = (short)region3.Index,
													WorldPositions = pathfindingResult.GetCompletePath().ToArray<WorldPosition>(),
													PathfindingMovementCapacity = PathfindingMovementCapacity.Water
												});
												list.Add(item);
											}
										}
									}
								}
							}
							if (region3.IsOcean)
							{
								list3.AddOnce(region3);
							}
							else
							{
								list2.Insert(k, region3);
								k++;
							}
						}
					}
					k++;
				}
			}
			this.OceanPathfindingWorldContext.RegionIndexList.Clear();
			return list.ToArray();
		}
		else
		{
			if (city.Region.Borders != null)
			{
				List<ushort> list4 = new List<ushort>();
				for (int num = 0; num < city.Region.Borders.Length; num++)
				{
					short regionIndex2 = (short)city.Region.Borders[num].NeighbourRegionIndex;
					Region region4 = this.WorldPositionningService.GetRegion((int)regionIndex2);
					if (region4 != null && region4.City != null)
					{
						if (proxied)
						{
							if ((movementCapacity & PathfindingMovementCapacity.Ground) != PathfindingMovementCapacity.Ground)
							{
								goto IL_766;
							}
							Diagnostics.Assert(region4.City.CadastralMap != null);
							if ((region4.City.CadastralMap.ConnectedMovementCapacity & PathfindingMovementCapacity.Ground) != PathfindingMovementCapacity.Ground)
							{
								goto IL_766;
							}
						}
						bool flag2 = false;
						if (region4.City.CadastralMap.Roads != null)
						{
							for (int num2 = 0; num2 < region4.City.CadastralMap.Roads.Count; num2++)
							{
								ushort num3 = region4.City.CadastralMap.Roads[num2];
								Road road = this.roads[(int)num3];
								if (road != null && (city.Region.Index == (int)road.FromRegion || city.Region.Index == (int)road.ToRegion) && (road.PathfindingMovementCapacity & movementCapacity) != PathfindingMovementCapacity.None)
								{
									if (this.RoadModified != null)
									{
										this.RoadModified(this, new RoadEventArgs(num3, road));
									}
									flag2 = true;
									list4.Add(num3);
								}
							}
						}
						if (!flag2)
						{
							PathfindingFlags pathfindingFlags2 = PathfindingFlags.IgnoreAll;
							pathfindingFlags2 &= ~PathfindingFlags.IgnoreMovementCapacities;
							pathfindingFlags2 &= ~PathfindingFlags.IgnorePOI;
							pathfindingFlags2 &= ~PathfindingFlags.IgnoreRoad;
							PathfindingContext pathfindingContext2 = new PathfindingContext(GameEntityGUID.Zero, city.Empire, movementCapacity);
							pathfindingContext2.RefreshProperties(1f, 1f, false, false, 1f, 1f);
							pathfindingContext2.RemoveMovementCapacity(PathfindingMovementCapacity.FrozenWater);
							Diagnostics.Assert(this.pathfindingWorldContext != null && this.pathfindingWorldContext.RegionIndexList != null && this.pathfindingWorldContext.RegionIndexList.Count == 2);
							this.pathfindingWorldContext.RegionIndexList[0] = (int)this.WorldPositionningService.GetRegionIndex(city.WorldPosition);
							this.pathfindingWorldContext.RegionIndexList[1] = (int)this.WorldPositionningService.GetRegionIndex(region4.City.WorldPosition);
							PathfindingResult pathfindingResult2 = this.PathfindingService.FindPath(pathfindingContext2, city.WorldPosition, region4.City.WorldPosition, PathfindingManager.RequestMode.Default, this.pathfindingWorldContext, pathfindingFlags2, null);
							if (pathfindingResult2 == null)
							{
								pathfindingFlags2 |= PathfindingFlags.IgnorePOI;
								pathfindingResult2 = this.PathfindingService.FindPath(pathfindingContext2, city.WorldPosition, region4.City.WorldPosition, PathfindingManager.RequestMode.Default, this.pathfindingWorldContext, pathfindingFlags2, null);
							}
							if (pathfindingResult2 != null)
							{
								ushort item2 = this.Reserve(new Road
								{
									FromRegion = (short)city.Region.Index,
									ToRegion = (short)region4.Index,
									WorldPositions = pathfindingResult2.GetCompletePath().ToList<WorldPosition>().ToArray(),
									PathfindingMovementCapacity = movementCapacity
								});
								list4.Add(item2);
							}
						}
					}
					IL_766:;
				}
				return list4.ToArray();
			}
			return null;
		}
	}

	void ICadasterService.Disconnect(City city, PathfindingMovementCapacity movementCapacity, bool cityAboutToBeDestroyed)
	{
		Diagnostics.Assert(city != null);
		Diagnostics.Assert(this.roads != null);
		Diagnostics.Assert(city.Region != null);
		List<ushort> list = new List<ushort>();
		List<short> list2 = new List<short>();
		for (int i = 0; i < this.roads.Count; i++)
		{
			if (this.roads[i] != null && (city.Region.Index == (int)this.roads[i].FromRegion || city.Region.Index == (int)this.roads[i].ToRegion))
			{
				list2.AddOnce(this.roads[i].FromRegion);
				list2.AddOnce(this.roads[i].ToRegion);
				if (!cityAboutToBeDestroyed && (movementCapacity & PathfindingMovementCapacity.Ground) == PathfindingMovementCapacity.Ground)
				{
					short regionIndex = this.roads[i].ToRegion;
					if ((int)this.roads[i].ToRegion == city.Region.Index)
					{
						regionIndex = this.roads[i].FromRegion;
					}
					Region region = this.WorldPositionningService.GetRegion((int)regionIndex);
					if (region != null)
					{
						Diagnostics.Assert(region.City != null);
						Diagnostics.Assert(region.City.CadastralMap != null);
						if ((region.City.CadastralMap.ConnectedMovementCapacity & PathfindingMovementCapacity.Ground) == PathfindingMovementCapacity.Ground)
						{
							if (this.RoadModified != null)
							{
								this.RoadModified(this, new RoadEventArgs((ushort)i, this.roads[i]));
								goto IL_1CC;
							}
							goto IL_1CC;
						}
					}
				}
				this.roads[i].PathfindingMovementCapacity &= ~movementCapacity;
				if (this.roads[i].PathfindingMovementCapacity == PathfindingMovementCapacity.None)
				{
					list.Add((ushort)i);
					((ICadasterService)this).Unregister((ushort)i);
					Diagnostics.Assert(this.roads[i] == null);
				}
			}
			IL_1CC:;
		}
		list2.Remove((short)city.Region.Index);
		if (list2.Count > 0)
		{
			Diagnostics.Assert(this.WorldPositionningService != null);
			for (int j = 0; j < list2.Count; j++)
			{
				Region region2 = this.WorldPositionningService.GetRegion((int)list2[j]);
				if (region2 != null && region2.City != null && region2.City.CadastralMap.Roads != null)
				{
					List<ushort> list3 = region2.City.CadastralMap.Roads.Except(list).ToList<ushort>();
					region2.City.CadastralMap.Roads = list3;
				}
			}
		}
		if (list.Count > 0)
		{
			if (city.CadastralMap.Roads != null)
			{
				List<ushort> list4 = city.CadastralMap.Roads.Except(list).ToList<ushort>();
				city.CadastralMap.Roads = list4;
			}
			city.NotifyCityCadastralChange();
		}
	}

	Road ICadasterService.GetRoadByIndex(ushort index)
	{
		if (index >= 0 && (int)index < this.roads.Count)
		{
			return this.roads[(int)index];
		}
		return null;
	}

	void ICadasterService.Register(ushort index, Road road)
	{
		if ((int)index < this.roads.Count)
		{
			if (this.roads[(int)index] == null)
			{
				this.roads[(int)index] = road;
			}
		}
		else
		{
			while (this.roads.Count <= (int)index)
			{
				this.roads.Add(null);
			}
			this.roads[(int)index] = road;
		}
		if (this.RoadRegistered != null)
		{
			this.RoadRegistered(this, new RoadEventArgs(index, road));
		}
	}

	void ICadasterService.Unregister(ushort index)
	{
		if ((int)index < this.roads.Count)
		{
			if (this.RoadUnregistered != null)
			{
				this.RoadUnregistered(this, new RoadEventArgs(index, this.roads[(int)index]));
			}
			this.roads[(int)index] = null;
		}
	}

	void ICadasterService.RefreshCadasterMap()
	{
		Diagnostics.Assert(this.cadasterMap != null && this.cadasterMap.Data != null, "cadasterMap or its Data are null");
		Array.Clear(this.cadasterMap.Data, 0, this.cadasterMap.Data.Length);
		Diagnostics.Assert(this.roads != null, "roads are null");
		List<Region> list = new List<Region>();
		if (this.roads != null)
		{
			for (int i = 0; i < this.roads.Count; i++)
			{
				Road road = this.roads[i];
				if (road != null && (road.PathfindingMovementCapacity & PathfindingMovementCapacity.Ground) == PathfindingMovementCapacity.Ground)
				{
					Diagnostics.Assert(this.WorldPositionningService != null, "assert1");
					Region region = this.WorldPositionningService.GetRegion((int)road.FromRegion);
					Diagnostics.Assert(region != null && region.City != null && region.City.CadastralMap != null, "assert2");
					Region region2 = this.WorldPositionningService.GetRegion((int)road.ToRegion);
					Diagnostics.Assert(region2 != null && region2.City != null && region2.City.CadastralMap != null, string.Format("assert4, {0} {1}", (region2 == null) ? "null" : region2.LocalizedName, (region2.City != null) ? region2.City.LocalizedName : "null"));
					bool flag = false;
					if (region.City == null)
					{
						list.AddOnce(region);
						flag = true;
					}
					if (region2.City == null)
					{
						list.AddOnce(region2);
						flag = true;
					}
					if (!flag)
					{
						bool flag2 = (region.City.CadastralMap.ConnectedMovementCapacity & PathfindingMovementCapacity.Ground) == PathfindingMovementCapacity.Ground;
						Diagnostics.Assert(region.City.Empire != null, "assert3");
						int bits = region.City.Empire.Bits;
						bool flag3 = (region2.City.CadastralMap.ConnectedMovementCapacity & PathfindingMovementCapacity.Ground) == PathfindingMovementCapacity.Ground;
						Diagnostics.Assert(region2.City.Empire != null, "assert5");
						int bits2 = region2.City.Empire.Bits;
						Diagnostics.Assert(road.WorldPositions != null, "assert6");
						for (int j = 0; j < road.WorldPositions.Length; j++)
						{
							WorldPosition worldPosition = road.WorldPositions[j];
							short regionIndex = this.WorldPositionningService.GetRegionIndex(worldPosition);
							if (regionIndex == road.FromRegion && flag2)
							{
								byte value = this.cadasterMap.GetValue(worldPosition);
								this.cadasterMap.SetValue(worldPosition, (byte)((int)value | bits));
							}
							else if (regionIndex == road.ToRegion && flag3)
							{
								byte value2 = this.cadasterMap.GetValue(worldPosition);
								this.cadasterMap.SetValue(worldPosition, (byte)((int)value2 | bits2));
							}
						}
					}
				}
			}
		}
		foreach (Region region3 in list)
		{
			this.ELCPDisconnect(region3, PathfindingMovementCapacity.All, true);
			Diagnostics.LogError("ELCP: RefreshCadasterMap detected invalid City in Region {0}, disconnecting ...", new object[]
			{
				region3.LocalizedName
			});
		}
	}

	void IDumpable.DumpAsText(StringBuilder content, string indent)
	{
	}

	byte[] IDumpable.DumpAsBytes()
	{
		return null;
	}

	public int Count
	{
		get
		{
			return this.roads.Count;
		}
	}

	private ushort Reserve(Road road)
	{
		for (int i = 0; i < this.roads.Count; i++)
		{
			if (this.roads[i] == null)
			{
				this.roads[i] = road;
				return (ushort)i;
			}
		}
		this.roads.Add(road);
		Diagnostics.Assert(this.roads.Count < 65535);
		return (ushort)(this.roads.Count - 1);
	}

	public virtual void ReadXml(XmlReader reader)
	{
		reader.ReadStartElement();
		int attribute = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("Roads");
		this.roads = new List<Road>(attribute);
		ushort num = 0;
		while ((int)num < attribute)
		{
			if (reader.IsNullElement())
			{
				reader.Skip();
				this.roads.Add(null);
			}
			else
			{
				Road road = new Road();
				road.FromRegion = reader.GetAttribute<short>("From");
				road.ToRegion = reader.GetAttribute<short>("To");
				road.PathfindingMovementCapacity = (PathfindingMovementCapacity)reader.GetAttribute<int>("MovementCapacity");
				reader.ReadStartElement("Road");
				int attribute2 = reader.GetAttribute<int>("Count");
				reader.ReadStartElement("WorldPositions");
				road.WorldPositions = new WorldPosition[attribute2];
				for (int i = 0; i < attribute2; i++)
				{
					road.WorldPositions[i] = default(WorldPosition);
					reader.ReadElementSerializable<WorldPosition>(ref road.WorldPositions[i]);
				}
				reader.ReadEndElement("WorldPositions");
				reader.ReadEndElement("Road");
				this.roads.Add(road);
			}
			num += 1;
		}
		reader.ReadEndElement("Roads");
		((ICadasterService)this).RefreshCadasterMap();
	}

	public void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString("AssemblyQualifiedName", base.GetType().AssemblyQualifiedName);
		writer.WriteStartElement("Roads");
		writer.WriteAttributeString<int>("Count", this.roads.Count);
		ushort num = 0;
		while ((int)num < this.roads.Count)
		{
			if (this.roads[(int)num] == null)
			{
				writer.WriteStartElement("nullref");
				writer.WriteEndElement();
			}
			else
			{
				writer.WriteStartElement("Road");
				writer.WriteAttributeString<short>("From", this.roads[(int)num].FromRegion);
				writer.WriteAttributeString<short>("To", this.roads[(int)num].ToRegion);
				writer.WriteAttributeString<int>("MovementCapacity", (int)this.roads[(int)num].PathfindingMovementCapacity);
				writer.WriteStartElement("WorldPositions");
				writer.WriteAttributeString<int>("Count", this.roads[(int)num].WorldPositions.Length);
				for (int i = 0; i < this.roads[(int)num].WorldPositions.Length; i++)
				{
					writer.WriteElementSerializable<WorldPosition>(ref this.roads[(int)num].WorldPositions[i]);
				}
				writer.WriteEndElement();
				writer.WriteEndElement();
			}
			num += 1;
		}
		writer.WriteEndElement();
	}

	public IPathfindingService PathfindingService { get; private set; }

	public IWorldPositionningService WorldPositionningService { get; private set; }

	public override IEnumerator BindServices(IServiceContainer serviceContainer)
	{
		yield return base.BindServices(serviceContainer);
		yield return base.BindService<IPathfindingService>(serviceContainer, delegate(IPathfindingService service)
		{
			this.PathfindingService = service;
		});
		yield return base.BindService<IWorldPositionningService>(serviceContainer, delegate(IWorldPositionningService service)
		{
			this.WorldPositionningService = service;
		});
		this.pathfindingWorldContext = new PathfindingWorldContext(null, null);
		this.pathfindingWorldContext.RegionIndexList = new List<int>(2);
		this.pathfindingWorldContext.RegionIndexList.Add(-1);
		this.pathfindingWorldContext.RegionIndexList.Add(-1);
		this.pathfindingWorldContext.RegionIndexListType = PathfindingWorldContext.RegionListType.RegionWhiteList;
		this.OceanPathfindingWorldContext = new PathfindingWorldContext(null, null);
		this.OceanPathfindingWorldContext.RegionIndexList = new List<int>();
		this.OceanPathfindingWorldContext.RegionIndexListType = PathfindingWorldContext.RegionListType.RegionWhiteList;
		serviceContainer.AddService<ICadasterService>(this);
		yield break;
	}

	public override IEnumerator OnWorldLoaded(World loadedWorld)
	{
		yield return base.OnWorldLoaded(loadedWorld);
		this.cadasterMap = (loadedWorld.Atlas.GetMap(WorldAtlas.Maps.Cadaster) as GridMap<byte>);
		if (this.cadasterMap == null)
		{
			this.cadasterMap = new GridMap<byte>(WorldAtlas.Maps.Cadaster, (int)loadedWorld.WorldParameters.Columns, (int)loadedWorld.WorldParameters.Rows, null);
			loadedWorld.Atlas.RegisterMapInstance<GridMap<byte>>(this.cadasterMap);
		}
		Diagnostics.Assert(this.cadasterMap != null);
		yield break;
	}

	protected override void Releasing()
	{
		base.Releasing();
		this.PathfindingService = null;
		this.WorldPositionningService = null;
	}

	private void ELCPDisconnect(Region Region, PathfindingMovementCapacity movementCapacity, bool cityAboutToBeDestroyed)
	{
		Diagnostics.Assert(this.roads != null);
		Diagnostics.Assert(Region != null);
		List<ushort> list = new List<ushort>();
		List<short> list2 = new List<short>();
		for (int i = 0; i < this.roads.Count; i++)
		{
			if (this.roads[i] != null && (Region.Index == (int)this.roads[i].FromRegion || Region.Index == (int)this.roads[i].ToRegion))
			{
				list2.AddOnce(this.roads[i].FromRegion);
				list2.AddOnce(this.roads[i].ToRegion);
				if (!cityAboutToBeDestroyed && (movementCapacity & PathfindingMovementCapacity.Ground) == PathfindingMovementCapacity.Ground)
				{
					short regionIndex = this.roads[i].ToRegion;
					if ((int)this.roads[i].ToRegion == Region.Index)
					{
						regionIndex = this.roads[i].FromRegion;
					}
					Region region = this.WorldPositionningService.GetRegion((int)regionIndex);
					if (region != null)
					{
						Diagnostics.Assert(region.City != null);
						Diagnostics.Assert(region.City.CadastralMap != null);
						if ((region.City.CadastralMap.ConnectedMovementCapacity & PathfindingMovementCapacity.Ground) == PathfindingMovementCapacity.Ground)
						{
							if (this.RoadModified != null)
							{
								this.RoadModified(this, new RoadEventArgs((ushort)i, this.roads[i]));
								goto IL_1AF;
							}
							goto IL_1AF;
						}
					}
				}
				this.roads[i].PathfindingMovementCapacity &= ~movementCapacity;
				if (this.roads[i].PathfindingMovementCapacity == PathfindingMovementCapacity.None)
				{
					list.Add((ushort)i);
					((ICadasterService)this).Unregister((ushort)i);
					Diagnostics.Assert(this.roads[i] == null);
				}
			}
			IL_1AF:;
		}
		list2.Remove((short)Region.Index);
		if (list2.Count > 0)
		{
			Diagnostics.Assert(this.WorldPositionningService != null);
			for (int j = 0; j < list2.Count; j++)
			{
				Region region2 = this.WorldPositionningService.GetRegion((int)list2[j]);
				if (region2 != null && region2.City != null && region2.City.CadastralMap.Roads != null)
				{
					List<ushort> list3 = region2.City.CadastralMap.Roads.Except(list).ToList<ushort>();
					region2.City.CadastralMap.Roads = list3;
				}
			}
		}
	}

	private List<Road> roads = new List<Road>();

	private GridMap<byte> cadasterMap;

	private PathfindingWorldContext pathfindingWorldContext;

	private PathfindingWorldContext OceanPathfindingWorldContext;
}
