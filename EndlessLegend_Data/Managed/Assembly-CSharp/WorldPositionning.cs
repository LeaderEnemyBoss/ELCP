using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Runtime;
using Amplitude.Utilities.Maps;
using UnityEngine;

public class WorldPositionning : GameAncillary, IService, IWorldPositionningService
{
	public void OnClientEndTurn()
	{
		for (int i = 0; i < this.World.Regions.Length; i++)
		{
			for (int j = 0; j < this.world.Regions[i].PointOfInterests.Length; j++)
			{
				this.UpdatePillageOnEndTurn(this.world.Regions[i].PointOfInterests[j]);
			}
		}
	}

	public void OnServerTurnEnded()
	{
		for (int i = 0; i < this.World.Regions.Length; i++)
		{
			for (int j = 0; j < this.world.Regions[i].PointOfInterests.Length; j++)
			{
				this.UpdatePillageServerSide(this.world.Regions[i].PointOfInterests[j]);
			}
		}
	}

	private void UpdatePillageOnEndTurn(PointOfInterest pointOfInterest)
	{
		IGameEntity gameEntity = null;
		if (pointOfInterest.ArmyPillaging.IsValid)
		{
			if (this.GameEntityRepositoryService.TryGetValue(pointOfInterest.ArmyPillaging, out gameEntity))
			{
				Army army = gameEntity as Army;
				if (army != null)
				{
					this.UpdatePillageProgress(army, pointOfInterest);
					return;
				}
			}
			DepartmentOfDefense.StopPillage(null, pointOfInterest);
		}
		this.UpdatePillageRecovery(pointOfInterest);
	}

	private void UpdatePillageProgress(Army army, PointOfInterest pointOfInterest)
	{
		if (!DepartmentOfDefense.CanStartPillage(army, pointOfInterest, true))
		{
			DepartmentOfDefense.StopPillage(army, pointOfInterest);
			return;
		}
		float propertyValue = army.GetPropertyValue(SimulationProperties.PillagePower);
		float num = pointOfInterest.GetPropertyValue(SimulationProperties.PillageDefense);
		num -= propertyValue;
		pointOfInterest.SetPropertyBaseValue(SimulationProperties.PillageDefense, Mathf.Max(0f, num));
	}

	private void UpdatePillageRecovery(PointOfInterest pointOfInterest)
	{
		if (pointOfInterest.ArmyPillaging.IsValid)
		{
			return;
		}
		if (pointOfInterest.SimulationObject.Tags.Contains(DepartmentOfDefense.PillageStatusDescriptor))
		{
			float num = pointOfInterest.GetPropertyValue(SimulationProperties.PillageCooldown);
			float propertyValue = pointOfInterest.GetPropertyValue(SimulationProperties.MaximumPillageCooldown);
			num += 1f;
			pointOfInterest.SetPropertyBaseValue(SimulationProperties.PillageCooldown, num);
			if (num >= propertyValue || pointOfInterest.Empire == null)
			{
				float propertyValue2 = pointOfInterest.GetPropertyValue(SimulationProperties.MaximumPillageDefense);
				pointOfInterest.SetPropertyBaseValue(SimulationProperties.PillageDefense, propertyValue2);
				if (!pointOfInterest.RemoveDescriptorByName(DepartmentOfDefense.PillageStatusDescriptor))
				{
					pointOfInterest.SimulationObject.Tags.RemoveTag(DepartmentOfDefense.PillageStatusDescriptor);
				}
				if (pointOfInterest.Region.City != null)
				{
					DepartmentOfTheInterior agency = pointOfInterest.Region.City.Empire.GetAgency<DepartmentOfTheInterior>();
					agency.VerifyOverallPopulation(pointOfInterest.Region.City);
					agency.BindMinorFactionToCity(pointOfInterest.Region.City, pointOfInterest.Region.MinorEmpire);
					pointOfInterest.Region.City.Empire.Refresh(false);
				}
				pointOfInterest.LineOfSightDirty = true;
			}
		}
		else
		{
			float num2 = pointOfInterest.GetPropertyValue(SimulationProperties.PillageDefense);
			float propertyValue3 = pointOfInterest.GetPropertyValue(SimulationProperties.PillageDefenseRecovery);
			float propertyValue4 = pointOfInterest.GetPropertyValue(SimulationProperties.MaximumPillageDefense);
			num2 += propertyValue3;
			pointOfInterest.SetPropertyBaseValue(SimulationProperties.PillageDefense, Mathf.Min(propertyValue4, num2));
		}
	}

	private void UpdatePillageServerSide(PointOfInterest pointOfInterest)
	{
		float propertyValue = pointOfInterest.GetPropertyValue(SimulationProperties.PillageDefense);
		if (propertyValue <= 0f && !pointOfInterest.SimulationObject.Tags.Contains(DepartmentOfDefense.PillageStatusDescriptor))
		{
			IGameEntity gameEntity = null;
			if (this.GameEntityRepositoryService.TryGetValue(pointOfInterest.ArmyPillaging, out gameEntity))
			{
				Army army = gameEntity as Army;
				if (army != null)
				{
					OrderPillageSucceed orderPillageSucceed = new OrderPillageSucceed(army.Empire.Index, army.GUID, pointOfInterest.GUID);
					orderPillageSucceed.ArmyActionName = "ArmyActionPillage";
					IPlayerControllerRepositoryControl playerControllerRepositoryControl = this.PlayerControllerRepositoryService as IPlayerControllerRepositoryControl;
					if (playerControllerRepositoryControl != null)
					{
						global::PlayerController playerControllerById = playerControllerRepositoryControl.GetPlayerControllerById("server");
						playerControllerById.PostOrder(orderPillageSucceed);
					}
				}
			}
		}
	}

	public World World
	{
		get
		{
			return this.world;
		}
	}

	internal static bool NonConvertedVillagesArentExploitable { get; set; }

	[Ancillary]
	protected IVisibilityService VisibilityService { get; set; }

	[Ancillary]
	private IPillarService PillarService { get; set; }

	[Ancillary]
	private ITerraformDeviceService TerraformDeviceService { get; set; }

	[Ancillary]
	private IGameEntityRepositoryService GameEntityRepositoryService { get; set; }

	[Ancillary]
	private IPlayerControllerRepositoryService PlayerControllerRepositoryService { get; set; }

	public override IEnumerator BindServices(IServiceContainer serviceContainer)
	{
		yield return base.BindService<IGameEntityRepositoryService>(serviceContainer, delegate(IGameEntityRepositoryService service)
		{
			this.GameEntityRepositoryService = service;
		});
		yield return base.BindService<IPlayerControllerRepositoryService>(serviceContainer, delegate(IPlayerControllerRepositoryService service)
		{
			this.PlayerControllerRepositoryService = service;
		});
		yield return base.BindService<IVisibilityService>(serviceContainer, delegate(IVisibilityService service)
		{
			this.VisibilityService = service;
		});
		serviceContainer.AddService<IWorldPositionningService>(this);
		yield return base.BindService<IPillarService>(serviceContainer, delegate(IPillarService service)
		{
			this.PillarService = service;
		});
		yield return base.BindService<ITerraformDeviceService>(serviceContainer, delegate(ITerraformDeviceService service)
		{
			this.TerraformDeviceService = service;
		});
		yield break;
	}

	public byte GetAnomalyType(WorldPosition position)
	{
		GridMap<byte> gridMap = this.world.Atlas.GetMap(WorldAtlas.Maps.Anomalies) as GridMap<byte>;
		if (gridMap == null)
		{
			return 0;
		}
		return gridMap.GetValue((int)position.Row, (int)position.Column);
	}

	public Army GetArmyAtPosition(WorldPosition worldPosition)
	{
		GridMap<Army> gridMap = this.world.Atlas.GetMap(WorldAtlas.Maps.Armies) as GridMap<Army>;
		if (gridMap != null)
		{
			return gridMap.GetValue(worldPosition);
		}
		return null;
	}

	public StaticString GetAnomalyTypeMappingName(byte anomalyType)
	{
		StaticString empty = StaticString.Empty;
		if (this.worldAtlasAnomalies.Data.TryGetValue((int)anomalyType, ref empty))
		{
			return empty;
		}
		return StaticString.Empty;
	}

	public IEnumerable<Army> GetArmiesInEncounterZone(WorldPosition attackerPosition, WorldOrientation attackDirection)
	{
		int deploymentAreaWidth = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<int>("Gameplay/Battle/DeploymentAreaWidth", 3);
		int deploymentAreaDepth = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<int>("Gameplay/Battle/DeploymentAreaDepth", 2);
		DeploymentArea area = new DeploymentArea(attackerPosition, attackDirection, this.world.WorldParameters);
		area.Initialize(deploymentAreaWidth, deploymentAreaDepth);
		WorldArea attackerDeploymentArea = new WorldArea(area.GetWorldPositions(this.world.WorldParameters));
		WorldOrientation battleZone2Orientation = attackDirection.Rotate(3);
		WorldPosition center2 = WorldPosition.GetNeighbourTile(attackerPosition, attackDirection, 1);
		area = new DeploymentArea(center2, battleZone2Orientation, this.world.WorldParameters);
		area.Initialize(deploymentAreaWidth, deploymentAreaDepth);
		WorldArea defenderDeploymentArea = new WorldArea(area.GetWorldPositions(this.world.WorldParameters));
		WorldArea battleArea = new WorldArea(attackerDeploymentArea.Grow(this.world.WorldParameters));
		battleArea = battleArea.Union(defenderDeploymentArea.Grow(this.world.WorldParameters));
		foreach (WorldPosition worldPosition in battleArea)
		{
			Army army = this.worldAtlasArmies.GetValue(worldPosition);
			if (army != null)
			{
				yield return army;
			}
		}
		yield break;
	}

	public byte GetBiomeType(WorldPosition position)
	{
		Diagnostics.Assert(this.world != null && this.world.Atlas != null);
		GridMap<byte> gridMap = this.world.Atlas.GetMap(WorldAtlas.Maps.Biomes) as GridMap<byte>;
		if (gridMap == null)
		{
			return 0;
		}
		return gridMap.GetValue(position);
	}

	public StaticString GetBiomeTypeMappingName(byte biomeType)
	{
		Diagnostics.Assert(this.world != null && this.world.Atlas != null);
		Map<BiomeTypeName> map = this.world.Atlas.GetMap(WorldAtlas.Tables.Biomes) as Map<BiomeTypeName>;
		if (map == null)
		{
			return StaticString.Empty;
		}
		Diagnostics.Assert(map.Data != null);
		BiomeTypeName biomeTypeName = map.Data.First((BiomeTypeName typeName) => typeName.TypeValue == (int)biomeType);
		if (biomeTypeName != null)
		{
			return biomeTypeName.Value;
		}
		return StaticString.Empty;
	}

	public Vector2 GetDirection(WorldPosition worldPosition, WorldPosition lookAtWorldPosition)
	{
		return WorldPosition.GetDirection(worldPosition, lookAtWorldPosition, this.world.WorldParameters.IsCyclicWorld, this.world.WorldParameters.Columns);
	}

	public int GetDistance(WorldPosition left, WorldPosition right)
	{
		return WorldPosition.GetDistance(left, right, this.world.WorldParameters.IsCyclicWorld, this.world.WorldParameters.Columns);
	}

	public District GetDistrict(WorldPosition worldPosition)
	{
		return this.worldAtlasDistricts.GetValue(worldPosition);
	}

	public int GetExplorationBits(WorldPosition position)
	{
		return (int)this.worldAtlasExploration.GetValue((int)position.Row, (int)position.Column);
	}

	public WorldPosition GetNeighbourTile(WorldPosition positionInWorldReference, WorldOrientation direction, int distance = 1)
	{
		WorldPosition neighbourTile = WorldPosition.GetNeighbourTile(positionInWorldReference, direction, distance);
		return WorldPosition.GetValidPosition(neighbourTile, this.world.WorldParameters);
	}

	public WorldPosition GetNeighbourTileFullCyclic(WorldPosition positionInWorldReference, WorldOrientation direction, int distance = 1)
	{
		WorldPosition neighbourTile = WorldPosition.GetNeighbourTile(positionInWorldReference, direction, distance);
		if (neighbourTile.Row >= this.World.WorldParameters.Rows)
		{
			neighbourTile.Row -= this.World.WorldParameters.Rows;
		}
		if (neighbourTile.Row < 0)
		{
			neighbourTile.Row += this.World.WorldParameters.Rows;
		}
		if (neighbourTile.Column >= this.World.WorldParameters.Columns)
		{
			neighbourTile.Column -= this.World.WorldParameters.Columns;
		}
		if (neighbourTile.Column < 0)
		{
			neighbourTile.Column += this.World.WorldParameters.Columns;
		}
		return neighbourTile;
	}

	public WorldOrientation GetOrientation(WorldPosition worldPosition, WorldPosition lookAtWorldPosition)
	{
		return WorldPosition.GetOrientation(worldPosition, lookAtWorldPosition, this.world.WorldParameters.IsCyclicWorld, this.world.WorldParameters.Columns);
	}

	public Region GetRegion(WorldPosition position)
	{
		int regionIndex = (int)this.GetRegionIndex(position);
		if (regionIndex < 0 || regionIndex >= this.world.Regions.Length)
		{
			return null;
		}
		return this.world.Regions[regionIndex];
	}

	public Region GetRegion(int regionIndex)
	{
		if (regionIndex < 0 || regionIndex >= this.world.Regions.Length)
		{
			return null;
		}
		return this.world.Regions[regionIndex];
	}

	public short GetRegionIndex(WorldPosition position)
	{
		Diagnostics.Assert(this.world != null);
		position = WorldPosition.GetValidPosition(position, this.world.WorldParameters);
		if (!position.IsValid)
		{
			return -1;
		}
		return this.worldAtlasRegions.GetValue(position);
	}

	public Region[] GetNeighbourRegions(Region sourceRegion, bool includeSelf = false, bool keepWastelands = false)
	{
		List<Region> list = new List<Region>();
		for (int i = 0; i < sourceRegion.Borders.Length; i++)
		{
			Region region = this.GetRegion(sourceRegion.Borders[i].NeighbourRegionIndex);
			if (region.IsWasteland && keepWastelands)
			{
				list.Add(region);
			}
			else if (!region.IsWasteland)
			{
				list.Add(region);
			}
		}
		if (includeSelf)
		{
			if (sourceRegion.IsWasteland && keepWastelands)
			{
				list.Add(sourceRegion);
			}
			else if (!sourceRegion.IsWasteland)
			{
				list.Add(sourceRegion);
			}
		}
		return list.ToArray();
	}

	public Region[] GetNeighbourRegionsWithCity(Region sourceRegion, bool includeSelf = false)
	{
		List<Region> list = new List<Region>();
		foreach (Region region in this.GetNeighbourRegions(sourceRegion, includeSelf, false))
		{
			if (region.City != null)
			{
				list.Add(region);
			}
		}
		return list.ToArray();
	}

	public Region[] GetNeighbourRegionsWithCityOfEmpire(Region sourceRegion, global::Empire empire, bool includeSelf = false)
	{
		List<Region> list = new List<Region>();
		foreach (Region region in this.GetNeighbourRegionsWithCity(sourceRegion, includeSelf))
		{
			if (region.City.Empire == empire)
			{
				list.Add(region);
			}
		}
		return list.ToArray();
	}

	public short GetRiverId(WorldPosition position)
	{
		Diagnostics.Assert(this.world != null && this.world.Atlas != null);
		GridMap<short> gridMap = this.world.Atlas.GetMap(WorldAtlas.Maps.River) as GridMap<short>;
		if (gridMap == null)
		{
			return -1;
		}
		return gridMap.GetValue(position);
	}

	public StaticString GetRiverTypeMappingName(short riverId)
	{
		Map<WorldRiver> map = this.world.Atlas.GetMap(WorldAtlas.Tables.Rivers) as Map<WorldRiver>;
		if (map == null)
		{
			return StaticString.Empty;
		}
		WorldRiver worldRiver = map.Data.FirstOrDefault((WorldRiver typeName) => typeName.Id == riverId);
		if (worldRiver != null)
		{
			return worldRiver.RiverTypeName;
		}
		return StaticString.Empty;
	}

	public WorldRiver GetRiver(short riverId)
	{
		Map<WorldRiver> map = this.world.Atlas.GetMap(WorldAtlas.Tables.Rivers) as Map<WorldRiver>;
		if (map == null)
		{
			return null;
		}
		WorldRiver worldRiver = map.Data.FirstOrDefault((WorldRiver typeName) => typeName.Id == riverId);
		if (worldRiver != null)
		{
			return worldRiver;
		}
		return null;
	}

	public byte GetTerrainType(WorldPosition position)
	{
		return this.worldAtlasTerrain.GetValue(position);
	}

	public sbyte GetTerrainHeight(WorldPosition position)
	{
		return this.worldAtlasTerrainHeight.GetValue(position);
	}

	public StaticString GetTerrainTypeMappingName(byte terrainType)
	{
		StaticString empty = StaticString.Empty;
		if (this.worldAtlasTerrainTypeNames.Data.TryGetValue((int)terrainType, ref empty))
		{
			return empty;
		}
		return StaticString.Empty;
	}

	public bool HasRidge(WorldPosition position)
	{
		GridMap<bool> gridMap = this.World.Atlas.GetMap(WorldAtlas.Maps.Ridges) as GridMap<bool>;
		return gridMap != null && gridMap.GetValue(position);
	}

	public PointOfInterest GetPointOfInterest(WorldPosition worldPosition)
	{
		GridMap<PointOfInterest> gridMap = this.World.Atlas.GetMap(WorldAtlas.Maps.PointOfInterest) as GridMap<PointOfInterest>;
		if (gridMap != null)
		{
			return gridMap.GetValue(worldPosition);
		}
		return null;
	}

	public bool IsExtensionConstructible(WorldPosition worldPosition, bool instant = false)
	{
		GridMap<bool> gridMap = this.World.Atlas.GetMap(WorldAtlas.Maps.Ridges) as GridMap<bool>;
		if (gridMap != null && gridMap.GetValue(worldPosition))
		{
			return false;
		}
		PointOfInterest pointOfInterest = this.GetPointOfInterest(worldPosition);
		if (pointOfInterest != null)
		{
			Diagnostics.Assert(pointOfInterest != null);
			Diagnostics.Assert(pointOfInterest.PointOfInterestDefinition != null);
			Diagnostics.Assert(pointOfInterest.PointOfInterestDefinition.PointOfInterestTemplate != null);
			string text;
			if (pointOfInterest.PointOfInterestDefinition.PointOfInterestTemplate.Properties != null && pointOfInterest.PointOfInterestDefinition.PointOfInterestTemplate.Properties.TryGetValue(WorldPositionning.PreventsDistrictTypeExtensionConstruction, out text))
			{
				return false;
			}
		}
		return !instant || !this.PillarService.IsPositionOccupiedByAPillar(worldPosition);
	}

	public bool IsConstructible(WorldPosition position, int bits = 0)
	{
		byte terrainType = this.GetTerrainType(position);
		StaticString terrainTypeMappingName = this.GetTerrainTypeMappingName(terrainType);
		if (!StaticString.IsNullOrEmpty(terrainTypeMappingName))
		{
			TerrainTypeMapping terrainTypeMapping = null;
			if (this.terrainTypeMappingDatabase != null && this.terrainTypeMappingDatabase.TryGetValue(terrainTypeMappingName, out terrainTypeMapping) && terrainTypeMapping.Layers != null && terrainTypeMapping.Layers.Length > 0)
			{
				for (int i = 0; i < terrainTypeMapping.Layers.Length; i++)
				{
					if (terrainTypeMapping.Layers[i].Type == WorldPositionning.LayerTypeConstruction && terrainTypeMapping.Layers[i].Name == WorldPositionning.LayerNameNotConstructible)
					{
						return false;
					}
				}
			}
		}
		GridMap<bool> gridMap = this.World.Atlas.GetMap(WorldAtlas.Maps.Ridges) as GridMap<bool>;
		return (gridMap == null || !gridMap.GetValue(position)) && !this.PillarService.IsPositionOccupiedByAPillar(position);
	}

	public bool IsConstructible(WorldPosition position, StaticString pointOfInterestTemplatePropertyName, int bits = 0)
	{
		bool flag = this.IsConstructible(position, bits);
		if (flag)
		{
			Region region = this.GetRegion(position);
			if (region != null && region.PointOfInterests != null)
			{
				for (int i = 0; i < region.PointOfInterests.Length; i++)
				{
					PointOfInterest pointOfInterest = region.PointOfInterests[i];
					if (!(pointOfInterest.WorldPosition != position))
					{
						Diagnostics.Assert(pointOfInterest != null);
						Diagnostics.Assert(pointOfInterest.PointOfInterestDefinition != null);
						Diagnostics.Assert(pointOfInterest.PointOfInterestDefinition.PointOfInterestTemplate != null);
						string text;
						if (pointOfInterest.PointOfInterestDefinition.PointOfInterestTemplate.Properties != null && pointOfInterest.PointOfInterestDefinition.PointOfInterestTemplate.Properties.TryGetValue(pointOfInterestTemplatePropertyName, out text))
						{
							flag = false;
						}
						break;
					}
				}
			}
		}
		if (this.PillarService.IsPositionOccupiedByAPillar(position))
		{
			flag = false;
		}
		return flag;
	}

	public bool IsWaterTile(WorldPosition position)
	{
		if (this.world == null || this.world.Atlas == null)
		{
			return false;
		}
		GridMap<byte> gridMap = this.world.Atlas.GetMap(WorldAtlas.Maps.WaterState) as GridMap<byte>;
		return gridMap.GetValue((int)position.Row, (int)position.Column) != 100;
	}

	public bool IsOceanTile(WorldPosition position)
	{
		if (this.world == null || this.world.Atlas == null)
		{
			return false;
		}
		GridMap<byte> gridMap = this.world.Atlas.GetMap(WorldAtlas.Maps.WaterType) as GridMap<byte>;
		World.WaterType value = (World.WaterType)gridMap.GetValue((int)position.Row, (int)position.Column);
		return value == World.WaterType.CoastalWaters || value == World.WaterType.Ocean || value == World.WaterType.Water;
	}

	public bool IsFrozenWaterTile(WorldPosition position)
	{
		if (this.world == null || this.world.Atlas == null)
		{
			return false;
		}
		GridMap<byte> gridMap = this.world.Atlas.GetMap(WorldAtlas.Maps.WaterState) as GridMap<byte>;
		return gridMap.GetValue((int)position.Row, (int)position.Column) == 75;
	}

	public bool IsForestTile(WorldPosition position)
	{
		if (this.world == null || this.world.Atlas == null)
		{
			return false;
		}
		GridMap<byte> gridMap = this.world.Atlas.GetMap(WorldAtlas.Maps.Forests) as GridMap<byte>;
		return gridMap.GetValue((int)position.Row, (int)position.Column) == 1;
	}

	public bool ContainsTerrainTag(WorldPosition position, string tag)
	{
		GridMap<byte> gridMap = this.world.Atlas.GetMap(WorldAtlas.Maps.Terrain) as GridMap<byte>;
		IDatabase<TerrainTypeMapping> database = Databases.GetDatabase<TerrainTypeMapping>(false);
		if (gridMap != null && database != null)
		{
			StaticString layerNameSimulation = new StaticString("Simulation");
			byte value = gridMap.GetValue(position);
			StaticString terrainTypeMappingName = this.GetTerrainTypeMappingName(value);
			TerrainTypeMapping terrainTypeMapping;
			if (database.TryGetValue(terrainTypeMappingName, out terrainTypeMapping))
			{
				return terrainTypeMapping.Layers.Any((SimulationLayer layer) => layer.Name == layerNameSimulation && layer.Samples.Any((SimulationLayer.Sample sample) => sample.Value == tag));
			}
		}
		return false;
	}

	public bool HasRetaliationFor(WorldPosition position, global::Empire empire = null)
	{
		GridMap<byte> gridMap = this.world.Atlas.GetMap(WorldAtlas.Maps.DefensiveTower) as GridMap<byte>;
		Diagnostics.Assert(gridMap != null);
		byte b = gridMap.GetValue(position);
		if (empire != null)
		{
			b &= (byte)(~(byte)empire.Bits);
		}
		if (b != 0)
		{
			if (empire == null)
			{
				return true;
			}
			DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
			if (agency == null)
			{
				return true;
			}
			for (int i = 0; i < base.Game.Empires.Length; i++)
			{
				global::Empire empire2 = base.Game.Empires[i];
				if (((int)b & empire2.Bits) != 0)
				{
					DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(empire2);
					if (diplomaticRelation == null || !diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.ImmuneToDefensiveImprovements))
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	public bool IsExploitable(WorldPosition position, int bits = 0)
	{
		byte terrainType = this.GetTerrainType(position);
		StaticString terrainTypeMappingName = this.GetTerrainTypeMappingName(terrainType);
		if (!StaticString.IsNullOrEmpty(terrainTypeMappingName))
		{
			TerrainTypeMapping terrainTypeMapping = null;
			if (this.terrainTypeMappingDatabase != null && this.terrainTypeMappingDatabase.TryGetValue(terrainTypeMappingName, out terrainTypeMapping) && terrainTypeMapping.Layers != null && terrainTypeMapping.Layers.Length > 0)
			{
				for (int i = 0; i < terrainTypeMapping.Layers.Length; i++)
				{
					if (terrainTypeMapping.Layers[i].Type == WorldPositionning.LayerTypeConstruction && terrainTypeMapping.Layers[i].Name == WorldPositionning.LayerNameNotExploitable)
					{
						return false;
					}
				}
			}
		}
		GridMap<bool> gridMap = this.World.Atlas.GetMap(WorldAtlas.Maps.Ridges) as GridMap<bool>;
		if (gridMap != null && gridMap.GetValue(position))
		{
			return false;
		}
		Region region = this.GetRegion(position);
		if (region != null && region.PointOfInterests != null)
		{
			int j = 0;
			while (j < region.PointOfInterests.Length)
			{
				PointOfInterest pointOfInterest = region.PointOfInterests[j];
				if (pointOfInterest.WorldPosition != position)
				{
					j++;
				}
				else
				{
					Diagnostics.Assert(pointOfInterest != null);
					Diagnostics.Assert(pointOfInterest.PointOfInterestDefinition != null);
					Diagnostics.Assert(pointOfInterest.PointOfInterestDefinition.PointOfInterestTemplate != null);
					if (WorldPositionning.NonConvertedVillagesArentExploitable && pointOfInterest.Type == "Village")
					{
						if (!pointOfInterest.SimulationObject.Tags.Contains(BarbarianCouncil.VillageStatusConverted) && pointOfInterest.CreepingNodeGUID == GameEntityGUID.Zero)
						{
							return false;
						}
						IGameEntity gameEntity;
						if (pointOfInterest.CreepingNodeGUID != GameEntityGUID.Zero && this.GameEntityRepositoryService.TryGetValue(pointOfInterest.CreepingNodeGUID, out gameEntity))
						{
							CreepingNode creepingNode = gameEntity as CreepingNode;
							if (creepingNode.IsUnderConstruction)
							{
								return false;
							}
						}
					}
					string text;
					if (pointOfInterest.PointOfInterestDefinition.PointOfInterestTemplate.Properties != null && pointOfInterest.PointOfInterestDefinition.PointOfInterestTemplate.Properties.TryGetValue(WorldPositionning.PreventsDistrictTypeExploitationConstruction, out text))
					{
						return false;
					}
					break;
				}
			}
		}
		return true;
	}

	public void RefreshDefensiveTowerMap(MajorEmpire empire)
	{
		byte empireMask = (byte)empire.Bits;
		this.RefreshDefensiveTowerMap(empireMask);
	}

	public void RefreshDefensiveTowerMapForEveryone()
	{
		this.RefreshDefensiveTowerMap(byte.MaxValue);
	}

	public bool TryGetListOfTargetInRange(Army inputArmy, int targetEmpireIndex, bool addPointsOfInterest, int regionRestriction, ref List<IWorldPositionable> targetList)
	{
		if (targetList == null)
		{
			targetList = new List<IWorldPositionable>();
		}
		if (inputArmy == null || inputArmy.Empire == null)
		{
			return false;
		}
		DepartmentOfForeignAffairs departmentOfForeignAffairs = null;
		bool flag = inputArmy.Empire is MinorEmpire;
		bool flag2 = inputArmy.Empire is NavalEmpire;
		if (!flag && !flag2)
		{
			departmentOfForeignAffairs = inputArmy.Empire.GetAgency<DepartmentOfForeignAffairs>();
		}
		if (addPointsOfInterest)
		{
			for (int i = 0; i < this.World.Regions.Length; i++)
			{
				for (int j = 0; j < this.World.Regions[i].PointOfInterests.Length; j++)
				{
					PointOfInterest pointOfInterest = this.World.Regions[i].PointOfInterests[j];
					if (this.GetDistance(pointOfInterest.WorldPosition, inputArmy.WorldPosition) <= inputArmy.LineOfSightVisionRange)
					{
						if (regionRestriction == -1 || regionRestriction == pointOfInterest.Region.Index)
						{
							targetList.Add(pointOfInterest);
						}
					}
				}
			}
		}
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		global::Game game = service.Game as global::Game;
		for (int k = 0; k < game.Empires.Length; k++)
		{
			global::Empire empire = game.Empires[k];
			if ((!flag && !flag2) || empire is MajorEmpire)
			{
				if (targetEmpireIndex == -1 || targetEmpireIndex == k)
				{
					DepartmentOfDefense agency = empire.GetAgency<DepartmentOfDefense>();
					if (agency != null)
					{
						int l = 0;
						while (l < agency.Armies.Count)
						{
							Army army = agency.Armies[l];
							if (flag || flag2)
							{
								if (!army.IsPrivateers)
								{
									bool flag3 = false;
									for (int m = 0; m < army.StandardUnits.Count; m++)
									{
										if (army.StandardUnits[m].SimulationObject.Tags.Contains(WorldPositionning.FriendlyBannerDescriptor))
										{
											flag3 = true;
										}
									}
									if ((army.Hero == null || !army.Hero.IsSkillUnlocked(WorldPositionning.FriendlyBannerSkill)) && !flag3)
									{
										int distance = this.GetDistance(army.WorldPosition, inputArmy.WorldPosition);
										float propertyValue = inputArmy.GetPropertyValue(SimulationProperties.DetectionRange);
										if (!army.IsCamouflaged || (float)distance <= propertyValue)
										{
											goto IL_2B1;
										}
									}
								}
							}
							else if (army.Empire == inputArmy.Empire || !army.IsCamouflaged || this.VisibilityService.IsWorldPositionDetectedFor(army.WorldPosition, inputArmy.Empire))
							{
								goto IL_2B1;
							}
							IL_2FA:
							l++;
							continue;
							IL_2B1:
							if (this.GetDistance(army.WorldPosition, inputArmy.WorldPosition) > inputArmy.LineOfSightVisionRange)
							{
								goto IL_2FA;
							}
							if (regionRestriction != -1 && regionRestriction != (int)this.GetRegionIndex(army.WorldPosition))
							{
								goto IL_2FA;
							}
							targetList.Add(army);
							goto IL_2FA;
						}
					}
					if (empire is MajorEmpire)
					{
						DepartmentOfTheInterior agency2 = empire.GetAgency<DepartmentOfTheInterior>();
						if (agency2 != null)
						{
							for (int n = 0; n < agency2.Cities.Count; n++)
							{
								City city = agency2.Cities[n];
								if (city.Empire == inputArmy.Empire || flag || flag2 || departmentOfForeignAffairs == null || departmentOfForeignAffairs.CanAttack(city))
								{
									if (this.GetDistance(city.WorldPosition, inputArmy.WorldPosition) <= inputArmy.LineOfSightVisionRange)
									{
										if (regionRestriction == -1 || regionRestriction == city.Region.Index)
										{
											targetList.Add(city);
										}
									}
								}
							}
						}
						MajorEmpire majorEmpire = empire as MajorEmpire;
						for (int num = 0; num < majorEmpire.ConvertedVillages.Count; num++)
						{
							Village village = majorEmpire.ConvertedVillages[num];
							if (flag || flag2 || departmentOfForeignAffairs == null || departmentOfForeignAffairs.CanAttack(village))
							{
								if (this.GetDistance(village.WorldPosition, inputArmy.WorldPosition) <= inputArmy.LineOfSightVisionRange)
								{
									if (regionRestriction == -1 || regionRestriction == village.Region.Index)
									{
										targetList.Add(village);
									}
								}
							}
						}
					}
					else if (empire is MinorEmpire)
					{
						BarbarianCouncil agency3 = empire.GetAgency<BarbarianCouncil>();
						if (agency3 != null)
						{
							for (int num2 = 0; num2 < agency3.Villages.Count; num2++)
							{
								Village village2 = agency3.Villages[num2];
								if (flag || flag2 || departmentOfForeignAffairs == null || departmentOfForeignAffairs.CanAttack(village2))
								{
									if (this.GetDistance(village2.WorldPosition, inputArmy.WorldPosition) <= inputArmy.LineOfSightVisionRange)
									{
										if (regionRestriction == -1 || regionRestriction == village2.Region.Index)
										{
											targetList.Add(village2);
										}
									}
								}
							}
						}
					}
					else if (empire is NavalEmpire)
					{
						PirateCouncil agency4 = empire.GetAgency<PirateCouncil>();
						if (agency4 != null)
						{
							for (int num3 = 0; num3 < agency4.Fortresses.Count; num3++)
							{
								Fortress fortress = agency4.Fortresses[num3];
								if (flag || flag2 || departmentOfForeignAffairs == null || departmentOfForeignAffairs.CanAttack(fortress))
								{
									if (this.GetDistance(fortress.WorldPosition, inputArmy.WorldPosition) <= inputArmy.LineOfSightVisionRange)
									{
										if (regionRestriction == -1 || regionRestriction == fortress.Region.Index)
										{
											targetList.Add(fortress);
										}
									}
								}
							}
						}
					}
				}
			}
		}
		return targetList.Count != 0;
	}

	public override IEnumerator OnWorldLoaded(World world)
	{
		yield return base.OnWorldLoaded(world);
		Diagnostics.Assert(world != null);
		this.world = world;
		this.worldAtlasAnomalies = (this.world.Atlas.GetMap(WorldAtlas.Tables.Anomalies) as Map<AnomalyTypeDefinition>);
		this.worldAtlasArmies = (this.world.Atlas.GetMap(WorldAtlas.Maps.Armies) as GridMap<Army>);
		this.worldAtlasDistricts = (this.world.Atlas.GetMap(WorldAtlas.Maps.Districts) as GridMap<District>);
		this.worldAtlasExploration = (this.world.Atlas.GetMap(WorldAtlas.Maps.Exploration) as GridMap<short>);
		this.worldAtlasRegions = (this.world.Atlas.GetMap(WorldAtlas.Maps.Regions) as GridMap<short>);
		this.worldAtlasTerrain = (this.world.Atlas.GetMap(WorldAtlas.Maps.Terrain) as GridMap<byte>);
		this.worldAtlasTerrainHeight = (this.world.Atlas.GetMap(WorldAtlas.Maps.Height) as GridMap<sbyte>);
		this.worldAtlasTerrainTypeNames = (this.world.Atlas.GetMap(WorldAtlas.Tables.Terrains) as Map<TerrainTypeName>);
		this.terrainTypeMappingDatabase = Databases.GetDatabase<TerrainTypeMapping>(false);
		yield break;
	}

	protected override void Releasing()
	{
		base.Releasing();
		this.worldAtlasAnomalies = null;
		this.worldAtlasArmies = null;
		this.worldAtlasDistricts = null;
		this.worldAtlasExploration = null;
		this.worldAtlasRegions = null;
		this.worldAtlasTerrain = null;
		this.worldAtlasTerrainHeight = null;
		this.worldAtlasTerrainTypeNames = null;
		this.terrainTypeMappingDatabase = Databases.GetDatabase<TerrainTypeMapping>(false);
		this.world = null;
	}

	private void RefreshDefensiveTowerMap(byte empireMask)
	{
		GridMap<byte> gridMap = this.world.Atlas.GetMap(WorldAtlas.Maps.DefensiveTower) as GridMap<byte>;
		Diagnostics.Assert(gridMap != null);
		byte b = ~empireMask;
		for (int i = 0; i < gridMap.Data.Length; i++)
		{
			gridMap.Data[i] = (gridMap.Data[i] & b);
		}
		for (int j = 0; j < this.world.Regions.Length; j++)
		{
			Region region = this.world.Regions[j];
			for (int k = 0; k < region.PointOfInterests.Length; k++)
			{
				PointOfInterest pointOfInterest = region.PointOfInterests[k];
				if (pointOfInterest.Empire != null && (pointOfInterest.Empire.Bits & (int)empireMask) != 0)
				{
					if (pointOfInterest.GetPropertyValue(SimulationProperties.DefensivePower) > 0f)
					{
						int lineOfSightVisionRange = pointOfInterest.LineOfSightVisionRange;
						if (lineOfSightVisionRange >= 0)
						{
							foreach (WorldPosition worldPosition in WorldPosition.ParseTilesInRange(pointOfInterest.WorldPosition, lineOfSightVisionRange, this.world.WorldParameters))
							{
								byte value = gridMap.GetValue(worldPosition);
								byte value2 = (byte)((int)value | pointOfInterest.Empire.Bits);
								gridMap.SetValue(worldPosition, value2);
							}
						}
					}
				}
			}
			if (region.City != null && region.City.GetPropertyValue(SimulationProperties.DefensivePower) > 0f)
			{
				for (int l = 0; l < region.City.Districts.Count; l++)
				{
					District district = region.City.Districts[l];
					if (district.Type == DistrictType.Exploitation || district.Type == DistrictType.Improvement)
					{
						byte value3 = gridMap.GetValue(district.WorldPosition);
						byte value4 = (byte)((int)value3 | district.Empire.Bits);
						gridMap.SetValue(district.WorldPosition, value4);
					}
				}
			}
		}
	}

	public void GetTerrainDamage(WorldPosition worldPosition, out float totalDamageAbsolute, out float totalDamagePercentMax, out float totalDamagePercentCurrent)
	{
		totalDamageAbsolute = 0f;
		totalDamagePercentMax = 0f;
		totalDamagePercentCurrent = 0f;
		GridMap<District> map = this.World.Atlas.GetMap(WorldAtlas.Maps.Districts) as GridMap<District>;
		District value = map.GetValue(worldPosition);
		if (value != null && value.Type != DistrictType.Exploitation)
		{
			return;
		}
		byte terrainType = this.GetTerrainType(worldPosition);
		StaticString terrainTypeMappingName = this.GetTerrainTypeMappingName(terrainType);
		if (!StaticString.IsNullOrEmpty(terrainTypeMappingName))
		{
			IDatabase<TerrainTypeMapping> database = Databases.GetDatabase<TerrainTypeMapping>(false);
			TerrainTypeMapping terrainTypeMapping = null;
			if (database.TryGetValue(terrainTypeMappingName, out terrainTypeMapping) && terrainTypeMapping != null)
			{
				for (int i = 0; i < terrainTypeMapping.Layers.Length; i++)
				{
					if (terrainTypeMapping.Layers[i].Type.Equals("Damage") && terrainTypeMapping.Layers[i].Name.Equals("OnEnter"))
					{
						for (int j = 0; j < terrainTypeMapping.Layers[i].Samples.Length; j++)
						{
							int weight = terrainTypeMapping.Layers[i].Samples[j].Weight;
							float num = float.Parse(terrainTypeMapping.Layers[i].Samples[j].Value);
							if (weight == 0)
							{
								totalDamageAbsolute += num;
							}
							else if (weight == 1)
							{
								totalDamagePercentMax += num;
							}
							else if (weight == 2)
							{
								totalDamagePercentCurrent += num;
							}
						}
					}
				}
			}
		}
	}

	public void GetRiverDamage(WorldPosition worldPosition, out float totalDamageAbsolute, out float totalDamagePercentMax, out float totalDamagePercentCurrent)
	{
		totalDamageAbsolute = 0f;
		totalDamagePercentMax = 0f;
		totalDamagePercentCurrent = 0f;
		GridMap<District> map = this.World.Atlas.GetMap(WorldAtlas.Maps.Districts) as GridMap<District>;
		District value = map.GetValue(worldPosition);
		if (value != null && value.Type != DistrictType.Exploitation)
		{
			return;
		}
		short riverId = this.GetRiverId(worldPosition);
		StaticString riverTypeMappingName = this.GetRiverTypeMappingName(riverId);
		if (!StaticString.IsNullOrEmpty(riverTypeMappingName))
		{
			IDatabase<RiverTypeMapping> database = Databases.GetDatabase<RiverTypeMapping>(false);
			RiverTypeMapping riverTypeMapping = null;
			if (database.TryGetValue(riverTypeMappingName, out riverTypeMapping) && riverTypeMapping != null)
			{
				for (int i = 0; i < riverTypeMapping.Layers.Length; i++)
				{
					if (riverTypeMapping.Layers[i].Type.Equals("Damage") && riverTypeMapping.Layers[i].Name.Equals("OnEnter"))
					{
						for (int j = 0; j < riverTypeMapping.Layers[i].Samples.Length; j++)
						{
							int weight = riverTypeMapping.Layers[i].Samples[j].Weight;
							float num = float.Parse(riverTypeMapping.Layers[i].Samples[j].Value);
							if (weight == 0)
							{
								totalDamageAbsolute += num;
							}
							else if (weight == 1)
							{
								totalDamagePercentMax += num;
							}
							else if (weight == 2)
							{
								totalDamagePercentCurrent += num;
							}
						}
					}
				}
			}
		}
	}

	public static StaticString LayerNameNotConstructible = new StaticString("NotConstructible");

	public static StaticString LayerNameNotExploitable = new StaticString("NotExploitable");

	public static StaticString LayerTypeConstruction = new StaticString("Construction");

	public static StaticString LayerTypeSimulation = new StaticString("Simulation");

	public static StaticString PreventsDistrictTypeCenterConstruction = new StaticString("PreventsDistrictTypeCenterConstruction");

	public static StaticString PreventsDistrictTypeExploitationConstruction = new StaticString("PreventsDistrictTypeExploitationConstruction");

	public static StaticString PreventsDistrictTypeExtensionConstruction = new StaticString("PreventsDistrictTypeExtensionConstruction");

	public static StaticString PreventsDistrictTypeExtensionWonderConstruction = new StaticString("PreventsDistrictTypeExtensionWonderConstruction");

	public static StaticString FriendlyBannerSkill = new StaticString("HeroSkillLeaderMap07");

	public static StaticString FriendlyBannerDescriptor = new StaticString("UnitAbilityFriendlyBannerDescriptor");

	private GridMap<Army> worldAtlasArmies;

	private Map<AnomalyTypeDefinition> worldAtlasAnomalies;

	private GridMap<District> worldAtlasDistricts;

	private GridMap<short> worldAtlasExploration;

	private GridMap<short> worldAtlasRegions;

	private GridMap<byte> worldAtlasTerrain;

	private GridMap<sbyte> worldAtlasTerrainHeight;

	private Map<TerrainTypeName> worldAtlasTerrainTypeNames;

	private IDatabase<TerrainTypeMapping> terrainTypeMappingDatabase;

	private World world;
}
