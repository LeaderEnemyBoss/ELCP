using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using Amplitude;
using Amplitude.Extensions;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Game.Orders;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Unity.View;
using Amplitude.Utilities.Maps;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

[OrderProcessor(typeof(OrderSwapFortressOccupant), "SwapFortressOccupant")]
[OrderProcessor(typeof(OrderSwapCityOwner), "SwapCityOwner")]
[OrderProcessor(typeof(OrderSpawnConvertedVillageUnit), "SpawnConvertedVillageUnit")]
[OrderProcessor(typeof(OrderTameUnstunnedKaiju), "TameUnstunnedKaiju")]
[OrderProcessor(typeof(OrderTameKaiju), "TameKaiju")]
[OrderProcessor(typeof(OrderChangeRegionUserDefinedName), "ChangeRegionUserDefinedName")]
[OrderProcessor(typeof(OrderSacrificePopulation), "SacrificePopulation")]
[OrderProcessor(typeof(OrderResettle), "Resettle")]
[OrderProcessor(typeof(OrderMovePopulation), "MovePopulation")]
[OrderProcessor(typeof(OrderDissentVillage), "DissentVillage")]
[OrderProcessor(typeof(OrderUntameKaiju), "UntameKaiju")]
[OrderProcessor(typeof(OrderUpdateCadastralMap), "UpdateCadastralMap")]
[OrderProcessor(typeof(OrderUpgradePointOfInterest), "UpgradePointOfInterest")]
[OrderProcessor(typeof(OrderDestroyPointOfInterestImprovement), "DestroyPointOfInterestImprovement")]
[OrderProcessor(typeof(OrderDestroyCityImprovement), "DestroyCityImprovement")]
[OrderProcessor(typeof(OrderDestroyCity), "DestroyCity")]
[OrderProcessor(typeof(OrderDestroyCamp), "DestroyCamp")]
[OrderProcessor(typeof(OrderCreateDistrictImprovement), "CreateDistrictImprovement")]
[OrderProcessor(typeof(OrderCreateCity), "CreateCity")]
[OrderProcessor(typeof(OrderCreateCamp), "CreateCamp")]
[OrderProcessor(typeof(OrderConvertVillage), "ConvertVillage")]
[OrderProcessor(typeof(OrderColonize), "Colonize")]
[OrderProcessor(typeof(OrderChangeEntityUserDefinedName), "ChangeEntityUserDefinedName")]
[OrderProcessor(typeof(OrderChangeDryDockWorldPosition), "ChangeDryDockWorldPosition")]
[OrderProcessor(typeof(OrderBuyOutPopulation), "BuyOutPopulation")]
[OrderProcessor(typeof(OrderBribeVillage), "BribeVillage")]
[OrderProcessor(typeof(OrderAssignPopulation), "AssignPopulation")]
[OrderProcessor(typeof(OrderAssimilateFaction), "AssimilateFaction")]
[OrderProcessor(typeof(OrderToggleRoundUp), "ToggleRoundUp")]
public class DepartmentOfTheInterior : Agency, IXmlSerializable
{
	public DepartmentOfTheInterior(global::Empire empire) : base(empire)
	{
		this.mainCity = null;
		this.mainCityGUID = GameEntityGUID.Zero;
		this.ShowTerraformFIDSI = false;
	}

	// Note: this type is marked as 'beforefieldinit'.
	static DepartmentOfTheInterior()
	{
		DepartmentOfTheInterior.ArmyStatusEarthquakerDescriptorName = "ArmyStatusEarthquaker";
	}

	public event CollectionChangeEventHandler AssimilatedFactionsCollectionChanged;

	public event CollectionChangeEventHandler OccupiedFortressesCollectionChanged;

	public event CollectionChangeEventHandler CitiesCollectionChanged;

	public event EventHandler<PopulationRepartitionEventArgs> PopulationRepartitionChanged;

	public static float GetCityPointEarthquakeDamage(City city)
	{
		float num = 0f;
		Army[] cityEarthquakeInstigators = DepartmentOfTheInterior.GetCityEarthquakeInstigators(city);
		for (int i = 0; i < cityEarthquakeInstigators.Length; i++)
		{
			cityEarthquakeInstigators[i].Refresh(true);
			num += cityEarthquakeInstigators[i].GetPropertyValue(SimulationProperties.CityPointEarthquakeDamage);
		}
		return num;
	}

	public static float GetCityGarrisonEarthquakeDamage(City city)
	{
		float num = 0f;
		Army[] cityEarthquakeInstigators = DepartmentOfTheInterior.GetCityEarthquakeInstigators(city);
		for (int i = 0; i < cityEarthquakeInstigators.Length; i++)
		{
			cityEarthquakeInstigators[i].Refresh(true);
			num += cityEarthquakeInstigators[i].GetPropertyValue(SimulationProperties.CityGarrisonEarthquakeDamage);
		}
		return num;
	}

	public static Army[] GetCityEarthquakeInstigators(City city)
	{
		if (city == null)
		{
			throw new ArgumentNullException("city");
		}
		List<Army> list = new List<Army>();
		for (int i = 0; i < city.Region.WorldPositions.Length; i++)
		{
			Army armyAtPosition = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetArmyAtPosition(city.Region.WorldPositions[i]);
			if (armyAtPosition != null && armyAtPosition.IsEarthquaker && armyAtPosition.Empire.Index != city.Empire.Index)
			{
				list.Add(armyAtPosition);
			}
		}
		return list.ToArray();
	}

	public void GameClient_TurnEnd_UpdateEarthquakeDamage(City city)
	{
		if (city == null)
		{
			throw new ArgumentNullException("city");
		}
		if (!city.IsUnderEarthquake)
		{
			return;
		}
		IEventService service = Services.GetService<IEventService>();
		Army[] cityEarthquakeInstigators = DepartmentOfTheInterior.GetCityEarthquakeInstigators(city);
		DepartmentOfDefense agency = base.Empire.GetAgency<DepartmentOfDefense>();
		foreach (Army army in cityEarthquakeInstigators)
		{
			army.Refresh(true);
			float propertyValue = army.GetPropertyValue(SimulationProperties.CityPointEarthquakeDamage);
			float num = army.GetPropertyValue(SimulationProperties.CityGarrisonEarthquakeDamage);
			if (propertyValue > 0f)
			{
				float propertyValue2 = city.GetPropertyValue(SimulationProperties.CityDefensePoint);
				float num2 = propertyValue2 - propertyValue;
				city.SetPropertyBaseValue(SimulationProperties.CityDefensePoint, Math.Max(0f, num2));
				if (num2 < 0f)
				{
					num -= num2;
				}
			}
			float num3 = 0f;
			if (num > 0f)
			{
				if (city.Hero != null)
				{
					agency.WoundUnit(city.Hero, num);
					num3 += num;
				}
				ReadOnlyCollection<Unit> standardUnits = city.StandardUnits;
				for (int j = standardUnits.Count - 1; j >= 0; j--)
				{
					Unit unit = standardUnits[j];
					agency.WoundUnit(unit, num);
					num3 += num;
				}
				ReadOnlyCollection<Unit> standardUnits2 = city.Militia.StandardUnits;
				for (int k = standardUnits2.Count - 1; k >= 0; k--)
				{
					Unit unit2 = standardUnits2[k];
					agency.WoundUnit(unit2, num);
					num3 += num;
				}
			}
			if (service != null)
			{
				EventCityDamagedByEarthquake eventToNotify = new EventCityDamagedByEarthquake(army.Empire, army, city, propertyValue, num3);
				service.Notify(eventToNotify);
			}
		}
		if (service != null)
		{
			EventCityEarthquakeUpdate eventToNotify2 = new EventCityEarthquakeUpdate(city.Empire, city);
			service.Notify(eventToNotify2);
		}
	}

	private static IDatabase<SimulationDescriptor> SimulationDescriptorDatabaseStatic { get; set; }

	private static IDatabase<TerrainTypeMapping> TerrainTypeMappingDatabaseStatic { get; set; }

	private static IDatabase<AnomalyTypeMapping> AnomalyTypeMappingDatabaseStatic { get; set; }

	private static IDatabase<BiomeTypeMapping> BiomeTypeMappingDatabaseStatic { get; set; }

	private static IDatabase<RiverTypeMapping> RiverTypeMappingDatabaseStatic { get; set; }

	private static IWorldPositionningService WorldPositionningServiceStatic { get; set; }

	private static IWorldEffectService WorldEffectServiceStatic { get; set; }

	private static IGameEntityRepositoryService GameEntityRepositoryServiceStatic { get; set; }

	private static IPathfindingService PathfindingServiceStatic { get; set; }

	private static global::Game Game { get; set; }

	public static void ApplyAnomalyDescriptor(SimulationObject districtProxy, StaticString anomalyTypeName)
	{
		if (StaticString.IsNullOrEmpty(anomalyTypeName))
		{
			return;
		}
		AnomalyTypeMapping anomalyTypeMapping;
		if (DepartmentOfTheInterior.AnomalyTypeMappingDatabaseStatic.TryGetValue(anomalyTypeName, out anomalyTypeMapping))
		{
			if (anomalyTypeMapping.Layers == null)
			{
				Diagnostics.LogWarning("The anomaly type mapping '{0}' has no layer.", new object[]
				{
					anomalyTypeName
				});
				return;
			}
			for (int i = 0; i < anomalyTypeMapping.Layers.Length; i++)
			{
				if (!(anomalyTypeMapping.Layers[i].Name != DepartmentOfTheInterior.SimulationLayerName))
				{
					if (!(anomalyTypeMapping.Layers[i].Type != DepartmentOfTheInterior.SimulationLayerType))
					{
						DepartmentOfTheInterior.ApplySimulationLayerDescriptors(districtProxy, anomalyTypeMapping.Layers[i]);
					}
				}
			}
		}
	}

	public static void ApplyBiomeTypeDescriptor(SimulationObject districtProxy, StaticString biomeTypeName)
	{
		if (StaticString.IsNullOrEmpty(biomeTypeName))
		{
			throw new ArgumentException("biomeTypeName");
		}
		BiomeTypeMapping biomeTypeMapping;
		if (DepartmentOfTheInterior.BiomeTypeMappingDatabaseStatic.TryGetValue(biomeTypeName, out biomeTypeMapping))
		{
			if (biomeTypeMapping.Layers == null)
			{
				Diagnostics.LogWarning("The terrain type mapping '{0}' has no layer.", new object[]
				{
					biomeTypeName
				});
				return;
			}
			for (int i = 0; i < biomeTypeMapping.Layers.Length; i++)
			{
				if (!(biomeTypeMapping.Layers[i].Name != DepartmentOfTheInterior.SimulationLayerName))
				{
					if (!(biomeTypeMapping.Layers[i].Type != DepartmentOfTheInterior.SimulationLayerType))
					{
						DepartmentOfTheInterior.ApplySimulationLayerDescriptors(districtProxy, biomeTypeMapping.Layers[i]);
					}
				}
			}
		}
	}

	public static void ApplyDistrictDescriptors(Amplitude.Unity.Game.Empire empire, District district, DistrictType districtType, StaticString terrainTypeName, StaticString biomeTypeName, StaticString anomalyTypeName, StaticString riverTypeName)
	{
		SimulationDescriptor descriptor = null;
		if (DepartmentOfTheInterior.SimulationDescriptorDatabaseStatic.TryGetValue("ClassDistrict", out descriptor))
		{
			district.AddDescriptor(descriptor, false);
		}
		else
		{
			Diagnostics.LogError("Unable to retrieve the 'ClassDistrict' simulation descriptor from the database.");
		}
		DepartmentOfTheInterior.ApplyTerrainTypeDescriptor(district, terrainTypeName);
		DepartmentOfTheInterior.ApplyBiomeTypeDescriptor(district, biomeTypeName);
		DepartmentOfTheInterior.ApplyAnomalyDescriptor(district, anomalyTypeName);
		DepartmentOfTheInterior.ApplyDistrictType(district, districtType, null);
		DepartmentOfTheInterior.ApplyRiverTypeDescriptor(district, riverTypeName);
		DepartmentOfTheInterior.ApplyPointOfInterestDescriptors(empire, district, district.WorldPosition, district.Type);
		DepartmentOfTheInterior.ApplyWorldEffectTypeDescriptor(district, district.WorldPosition, false);
	}

	public static void ApplyDistrictDescriptors(Amplitude.Unity.Game.Empire empire, District district, DistrictType districtType = DistrictType.Exploitation)
	{
		byte terrainType = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetTerrainType(district.WorldPosition);
		StaticString terrainTypeMappingName = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetTerrainTypeMappingName(terrainType);
		byte biomeType = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetBiomeType(district.WorldPosition);
		StaticString biomeTypeMappingName = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetBiomeTypeMappingName(biomeType);
		byte anomalyType = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetAnomalyType(district.WorldPosition);
		StaticString anomalyTypeMappingName = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetAnomalyTypeMappingName(anomalyType);
		short riverId = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetRiverId(district.WorldPosition);
		StaticString riverTypeMappingName = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetRiverTypeMappingName(riverId);
		DepartmentOfTheInterior.ApplyDistrictDescriptors(district, districtType, terrainTypeMappingName, biomeTypeMappingName, anomalyTypeMappingName, riverTypeMappingName, true);
		DepartmentOfTheInterior.ApplyPointOfInterestDescriptors(empire, district, district.WorldPosition, district.Type);
		DepartmentOfTheInterior.ApplyWorldEffectTypeDescriptor(district, district.WorldPosition, false);
		district.Type = districtType;
	}

	public static void ApplyDistrictDescriptors(SimulationObject district, DistrictType districtType, StaticString terrainTypeName, StaticString biomeTypeName, StaticString anomalyTypeName, StaticString riverTypeName, bool needDistrictDescriptor = true)
	{
		if (needDistrictDescriptor)
		{
			SimulationDescriptor descriptor = null;
			if (DepartmentOfTheInterior.SimulationDescriptorDatabaseStatic.TryGetValue("ClassDistrict", out descriptor))
			{
				district.AddDescriptor(descriptor);
			}
			else
			{
				Diagnostics.LogError("Unable to retrieve the 'ClassDistrict' simulation descriptor from the database.");
			}
			DepartmentOfTheInterior.ApplyDistrictTypeDescriptor(district, districtType);
		}
		DepartmentOfTheInterior.ApplyTerrainTypeDescriptor(district, terrainTypeName);
		DepartmentOfTheInterior.ApplyBiomeTypeDescriptor(district, biomeTypeName);
		DepartmentOfTheInterior.ApplyAnomalyDescriptor(district, anomalyTypeName);
		DepartmentOfTheInterior.ApplyRiverTypeDescriptor(district, riverTypeName);
	}

	public static void ApplyDistrictProxyDescriptors(Amplitude.Unity.Game.Empire empire, SimulationObject districtProxy, WorldPosition worldPosition, DistrictType districtType = DistrictType.Exploitation, bool needDistrictDescriptor = true, bool showTerraformDescriptors = false)
	{
		byte terrainType = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetTerrainType(worldPosition);
		StaticString terrainTypeName = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetTerrainTypeMappingName(terrainType);
		if (showTerraformDescriptors)
		{
			TerrainTypeMapping terrainTypeMapping;
			DepartmentOfTheInterior.Game.World.TryGetTerraformMapping(worldPosition, out terrainTypeMapping);
			if (terrainTypeMapping != null)
			{
				terrainTypeName = terrainTypeMapping.Name;
			}
		}
		byte biomeType = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetBiomeType(worldPosition);
		StaticString biomeTypeMappingName = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetBiomeTypeMappingName(biomeType);
		byte anomalyType = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetAnomalyType(worldPosition);
		StaticString anomalyTypeMappingName = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetAnomalyTypeMappingName(anomalyType);
		short riverId = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetRiverId(worldPosition);
		StaticString riverTypeMappingName = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetRiverTypeMappingName(riverId);
		DepartmentOfTheInterior.ApplyDistrictDescriptors(districtProxy, districtType, terrainTypeName, biomeTypeMappingName, anomalyTypeMappingName, riverTypeMappingName, needDistrictDescriptor);
		DepartmentOfTheInterior.ApplyPointOfInterestDescriptors(empire, districtProxy, worldPosition, districtType);
		DepartmentOfTheInterior.ApplyWorldEffectTypeDescriptor(districtProxy, worldPosition, true);
	}

	public static void ApplyRiverTypeDescriptor(SimulationObject districtProxy, StaticString riverTypeName)
	{
		if (StaticString.IsNullOrEmpty(riverTypeName))
		{
			return;
		}
		RiverTypeMapping riverTypeMapping;
		if (DepartmentOfTheInterior.RiverTypeMappingDatabaseStatic.TryGetValue(riverTypeName, out riverTypeMapping))
		{
			if (riverTypeMapping.Layers == null)
			{
				Diagnostics.LogWarning("The terrain type mapping '{0}' has no layer.", new object[]
				{
					riverTypeName
				});
				return;
			}
			for (int i = 0; i < riverTypeMapping.Layers.Length; i++)
			{
				if (!(riverTypeMapping.Layers[i].Name != DepartmentOfTheInterior.SimulationLayerName))
				{
					if (!(riverTypeMapping.Layers[i].Type != DepartmentOfTheInterior.SimulationLayerType))
					{
						DepartmentOfTheInterior.ApplySimulationLayerDescriptors(districtProxy, riverTypeMapping.Layers[i]);
					}
				}
			}
		}
	}

	public static void ApplyTerrainTypeDescriptor(SimulationObject districtProxy, StaticString terrainTypeName)
	{
		if (StaticString.IsNullOrEmpty(terrainTypeName))
		{
			throw new ArgumentException("terrainTypeName");
		}
		TerrainTypeMapping terrainTypeMapping;
		if (DepartmentOfTheInterior.TerrainTypeMappingDatabaseStatic.TryGetValue(terrainTypeName, out terrainTypeMapping))
		{
			if (terrainTypeMapping.Layers == null)
			{
				Diagnostics.LogWarning("The terrain type mapping '{0}' has no layer.", new object[]
				{
					terrainTypeName
				});
				return;
			}
			for (int i = 0; i < terrainTypeMapping.Layers.Length; i++)
			{
				if (!(terrainTypeMapping.Layers[i].Name != DepartmentOfTheInterior.SimulationLayerName))
				{
					if (!(terrainTypeMapping.Layers[i].Type != DepartmentOfTheInterior.SimulationLayerType))
					{
						DepartmentOfTheInterior.ApplySimulationLayerDescriptors(districtProxy, terrainTypeMapping.Layers[i]);
					}
				}
			}
		}
	}

	public static bool CanBuyoutPopulation(City city)
	{
		return city != null && city.Empire.SimulationObject.Tags.Contains("FactionTraitBuyOutPopulation");
	}

	public static bool CanSacrificePopulation(City city, ref float cost)
	{
		if (city == null || city.Empire == null)
		{
			return false;
		}
		if (city.IsInEncounter)
		{
			return false;
		}
		float propertyValue = city.Empire.GetPropertyValue(SimulationProperties.PopulationSacrificeCooldown);
		if (propertyValue > 0f)
		{
			return false;
		}
		DepartmentOfScience agency = city.Empire.GetAgency<DepartmentOfScience>();
		if (agency == null)
		{
			Diagnostics.Assert("CanSacrificePopulation: departement of science is null for empire {0}", new object[]
			{
				city.Empire.Index
			});
			return false;
		}
		if (!agency.CanSacrificePopulation())
		{
			return false;
		}
		float propertyValue2 = city.GetPropertyValue(SimulationProperties.Population);
		if (propertyValue2 <= 1f)
		{
			return false;
		}
		DepartmentOfTheInterior agency2 = city.Empire.GetAgency<DepartmentOfTheInterior>();
		float num;
		float num2;
		agency2.GetGrowthLimits(propertyValue2 - 1f, out num, out num2);
		float propertyValue3 = city.GetPropertyValue(SimulationProperties.CityGrowthStock);
		cost = num - propertyValue3;
		DepartmentOfTheTreasury agency3 = city.Empire.GetAgency<DepartmentOfTheTreasury>();
		return agency3.IsTransferOfResourcePossible(city, DepartmentOfTheTreasury.Resources.CityGrowth, ref cost);
	}

	public static bool CanSacrificePopulation(City city)
	{
		float num = 0f;
		return DepartmentOfTheInterior.CanSacrificePopulation(city, ref num);
	}

	public static bool CanInvokePillarsAndSpells(global::Empire empire)
	{
		return empire.SimulationObject.Tags.Contains("FactionTraitPillarActivated");
	}

	public static bool CanInvokeTerraformationDevices(global::Empire empire)
	{
		return empire.SimulationObject.Tags.Contains("FactionTraitFlames1");
	}

	public static bool CanPlaceGolemCamps(global::Empire empire)
	{
		return empire.SimulationObject.Tags.Contains("FactionTraitFlames10");
	}

	public static bool CanPlaceCreepingNodes(global::Empire empire)
	{
		return empire.SimulationObject.Tags.Contains("FactionTraitMimics1");
	}

	public static bool CanUseAffinityStrategicResource(global::Empire empire)
	{
		if (empire == null)
		{
			Diagnostics.LogError("Empire is null.");
			return false;
		}
		return empire.SimulationObject.Tags.Contains("FactionTraitAffinityStrategic");
	}

	public static bool CanUseResourceAsAffinityResource(StaticString resourceName)
	{
		IDatabase<SimulationDescriptor> database = Databases.GetDatabase<SimulationDescriptor>(false);
		return database.ContainsKey("Affinity" + resourceName);
	}

	public static bool CanRecycleCadavers(global::Empire empire)
	{
		if (empire == null)
		{
			Diagnostics.LogError("Empire is null.");
			return false;
		}
		return empire.SimulationObject.Tags.Contains("FactionTraitNecrophagesRecycling");
	}

	public static bool CanGetBoostsFromSiegeDamage(global::Empire empire)
	{
		if (empire == null)
		{
			Diagnostics.LogError("Empire is null.");
			return false;
		}
		DepartmentOfScience agency = empire.GetAgency<DepartmentOfScience>();
		DepartmentOfScience.ConstructibleElement.State technologyState = agency.GetTechnologyState("TechnologyDefinitionFlames9");
		return technologyState == DepartmentOfScience.ConstructibleElement.State.Researched;
	}

	public static bool CanPerformLavaformation(global::Empire empire)
	{
		if (empire == null)
		{
			Diagnostics.LogError("Empire is null.");
			return false;
		}
		return empire.SimulationObject.Tags.Contains("FactionTraitFlames1");
	}

	public static bool CanImmolateUnits(global::Empire empire)
	{
		if (empire == null)
		{
			Diagnostics.LogError("Empire is null.");
			return false;
		}
		return empire.SimulationObject.Tags.Contains("FactionTraitBrokenLordsHeatWave");
	}

	public static bool CanExtractDustFromExperience(global::Empire empire)
	{
		if (empire == null)
		{
			Diagnostics.LogError("Empire is null.");
			return false;
		}
		if (!(empire is MajorEmpire))
		{
			return false;
		}
		DepartmentOfScience agency = empire.GetAgency<DepartmentOfScience>();
		if (agency == null)
		{
			Diagnostics.LogError("departmentOfScience is null.");
			return false;
		}
		return agency.GetTechnologyState("TechnologyDefinitionBrokenLords6") == DepartmentOfScience.ConstructibleElement.State.Researched;
	}

	public static bool CanSeeAllExchangeTransactions(global::Empire empire)
	{
		if (empire == null)
		{
			Diagnostics.LogError("Empire is null.");
			return false;
		}
		return empire.SimulationObject.Tags.Contains("FactionTraitRovingClans9");
	}

	public static bool CanCollectTollFeeOnTransactions(global::Empire empire)
	{
		if (empire == null)
		{
			Diagnostics.LogError("Empire is null.");
			return false;
		}
		return empire.SimulationObject.Tags.Contains("FactionTraitRovingClans10");
	}

	public static bool CanNeverDeclareWar(global::Empire empire)
	{
		if (empire == null)
		{
			Diagnostics.LogError("Empire is null.");
			return false;
		}
		return empire.SimulationObject.Tags.Contains("FactionTraitRovingClans8");
	}

	public static bool CanLootVillages(global::Empire empire)
	{
		return empire.SimulationObject.Tags.Contains("FactionTraitReplicants2");
	}

	public static void ClearFIMSEOnConvertedVillage(global::Empire converter, PointOfInterest convertedPointOfInterest)
	{
		for (int i = convertedPointOfInterest.SimulationObject.Children.Count - 1; i >= 0; i--)
		{
			SimulationObject simulationObject = convertedPointOfInterest.SimulationObject.Children[i];
			convertedPointOfInterest.SimulationObject.RemoveChild(simulationObject);
			simulationObject.Dispose();
		}
		DepartmentOfTheInterior.ClearResourceOnConvertedVillage(converter, convertedPointOfInterest);
	}

	public static void ClearResourcesLeechingForKaijus(Kaiju kaiju)
	{
		kaiju.RemoveDescriptorByType("LeechByKaijus");
		kaiju.Refresh(false);
		IEventService service = Services.GetService<IEventService>();
		service.Notify(new EventKaijuLeechUpdated(kaiju, EventKaijuLeechUpdated.UpdateOperation.Clear));
	}

	public static void DestroyPointOfInterest(PointOfInterest pointOfInterest)
	{
		if (pointOfInterest.PointOfInterestImprovement != null)
		{
			pointOfInterest.RemovePointOfInterestImprovement();
			if (pointOfInterest.ArmyPillaging.IsValid)
			{
				DepartmentOfDefense.StopPillage(pointOfInterest);
			}
			Region region = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetRegion(pointOfInterest.WorldPosition);
			if (region != null && region.City != null)
			{
				DepartmentOfTheInterior agency = region.City.Empire.GetAgency<DepartmentOfTheInterior>();
				agency.VerifyOverallPopulation(region.City);
				region.City.Refresh(false);
				pointOfInterest.LineOfSightDirty = true;
				for (int i = 0; i < region.City.Districts.Count; i++)
				{
					if (region.City.Districts[i].WorldPosition == pointOfInterest.WorldPosition)
					{
						District district = region.City.Districts[i];
						string text;
						if (pointOfInterest.PointOfInterestDefinition.TryGetValue("VisibilityTechnology", out text))
						{
							DepartmentOfScience agency2 = region.City.Empire.GetAgency<DepartmentOfScience>();
							if (agency2.GetTechnologyState(text) != DepartmentOfScience.ConstructibleElement.State.Researched && pointOfInterest.PointOfInterestDefinition.TryGetValue("DistrictDescriptor", out text))
							{
								district.RemoveDescriptorByName(text);
							}
						}
						break;
					}
				}
			}
		}
	}

	public static void GenerateFIMSEForConvertedVillage(Amplitude.Unity.Game.Empire converter, PointOfInterest convertedPointOfInterest)
	{
		SimulationObject simulationObject = new SimulationObject("VillageDistrict");
		DepartmentOfTheInterior.ApplyDistrictProxyDescriptors(converter, simulationObject, convertedPointOfInterest.WorldPosition, DistrictType.Exploitation, true, false);
		convertedPointOfInterest.SimulationObject.AddChild(simulationObject);
		int index = convertedPointOfInterest.Region.Index;
		for (int i = 0; i < 6; i++)
		{
			WorldPosition neighbourTile = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetNeighbourTile(convertedPointOfInterest.WorldPosition, (WorldOrientation)i, 1);
			if ((int)DepartmentOfTheInterior.WorldPositionningServiceStatic.GetRegionIndex(neighbourTile) == index)
			{
				SimulationObject simulationObject2 = new SimulationObject("Village_" + ((WorldOrientation)i).ToString());
				DepartmentOfTheInterior.ApplyDistrictProxyDescriptors(converter, simulationObject2, neighbourTile, DistrictType.Exploitation, true, false);
				convertedPointOfInterest.SimulationObject.AddChild(simulationObject2);
			}
		}
		DepartmentOfTheInterior.GenerateResourceForConvertedVillages(converter, convertedPointOfInterest);
	}

	public static void GenerateFIMSEForCreepingNode(Amplitude.Unity.Game.Empire empire, CreepingNode node)
	{
		int fidsiextractionRange = node.NodeDefinition.FIDSIExtractionRange;
		int index = node.PointOfInterest.Region.Index;
		WorldCircle worldCircle = new WorldCircle(node.WorldPosition, fidsiextractionRange);
		WorldPosition[] worldPositions = worldCircle.GetWorldPositions(DepartmentOfTheInterior.WorldPositionningServiceStatic.World.WorldParameters);
		for (int i = 0; i < worldPositions.Length; i++)
		{
			if ((int)DepartmentOfTheInterior.WorldPositionningServiceStatic.GetRegionIndex(worldPositions[i]) == index)
			{
				if (DepartmentOfTheInterior.WorldPositionningServiceStatic.GetDistrict(worldPositions[i]) == null && !DepartmentOfTheInterior.WorldPositionningServiceStatic.HasRidge(worldPositions[i]))
				{
					SimulationObject simulationObject = new SimulationObject("CreepingNode_" + i.ToString());
					DepartmentOfTheInterior.ApplyDistrictProxyDescriptors(empire, simulationObject, worldPositions[i], DistrictType.Exploitation, true, false);
					SimulationDescriptor descriptor = null;
					if (DepartmentOfTheInterior.SimulationDescriptorDatabaseStatic.TryGetValue("CreepingNodeDistrict", out descriptor))
					{
						simulationObject.AddDescriptor(descriptor);
					}
					else
					{
						Diagnostics.LogError("Unable to retrieve the 'CreepingNodeDistrict' simulation descriptor from the database.");
					}
					node.SimulationObject.AddChild(simulationObject);
				}
			}
		}
	}

	public static void ClearFIMSEOnCreepingNode(global::Empire empire, CreepingNode node)
	{
		for (int i = node.SimulationObject.Children.Count - 1; i >= 0; i--)
		{
			SimulationObject simulationObject = node.SimulationObject.Children[i];
			node.SimulationObject.RemoveChild(simulationObject);
			simulationObject.Dispose();
		}
	}

	public static void GenerateResourcesLeechingForTamedKaijus(Kaiju kaiju)
	{
		DepartmentOfTheInterior.ClearResourcesLeechingForKaijus(kaiju);
		if (!kaiju.IsTamed() || kaiju.OnArmyMode())
		{
			return;
		}
		global::Empire majorEmpire = kaiju.MajorEmpire;
		Region region = kaiju.Region;
		PointOfInterest[] pointOfInterests = region.PointOfInterests;
		List<PointOfInterest> list = new List<PointOfInterest>();
		DepartmentOfScience agency = majorEmpire.GetAgency<DepartmentOfScience>();
		foreach (PointOfInterest pointOfInterest in pointOfInterests)
		{
			if (!(pointOfInterest.Type != "ResourceDeposit"))
			{
				string empty = string.Empty;
				if (!pointOfInterest.PointOfInterestDefinition.TryGetValue("VisibilityTechnology", out empty) || agency.GetTechnologyState(empty) == DepartmentOfScience.ConstructibleElement.State.Researched)
				{
					list.Add(pointOfInterest);
				}
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		Dictionary<StaticString, int> dictionary = new Dictionary<StaticString, int>();
		for (int j = 0; j < list.Count; j++)
		{
			PointOfInterest pointOfInterest2 = list[j];
			string empty2 = string.Empty;
			if (pointOfInterest2.PointOfInterestDefinition.TryGetValue("LeechByKaijusDescriptor", out empty2))
			{
				if (!dictionary.ContainsKey(empty2))
				{
					dictionary.Add(empty2, 1);
				}
				else
				{
					Dictionary<StaticString, int> dictionary3;
					Dictionary<StaticString, int> dictionary2 = dictionary3 = dictionary;
					StaticString key2;
					StaticString key = key2 = empty2;
					int num = dictionary3[key2];
					dictionary2[key] = num + 1;
				}
			}
		}
		foreach (StaticString x in dictionary.Keys)
		{
			string text = x;
			SimulationDescriptor descriptor = null;
			if (!DepartmentOfTheInterior.SimulationDescriptorDatabaseStatic.TryGetValue(text, out descriptor))
			{
				Diagnostics.LogError("Could not retrieve the simulation descriptor '{0}'.", new object[]
				{
					text
				});
			}
			else
			{
				kaiju.AddDescriptor(descriptor, false);
				kaiju.SetPropertyBaseValue(text, (float)dictionary[text]);
			}
		}
		kaiju.Refresh(false);
		IEventService service = Services.GetService<IEventService>();
		service.Notify(new EventKaijuLeechUpdated(kaiju, EventKaijuLeechUpdated.UpdateOperation.Clear));
	}

	public static bool IsArmyAbleToConvert(Army army, bool isOrderCheck = true)
	{
		if (!army.Empire.SimulationObject.Tags.Contains("FactionTraitCultists14"))
		{
			if (!isOrderCheck)
			{
				Diagnostics.LogError("Order preprocessing failed because the empire has not the correct faction trait.");
			}
			return false;
		}
		DepartmentOfScience agency = army.Empire.GetAgency<DepartmentOfScience>();
		if (agency.GetTechnologyState("TechnologyDefinitionCultists5") != DepartmentOfScience.ConstructibleElement.State.Researched)
		{
			if (!isOrderCheck)
			{
				Diagnostics.LogError("Order preprocessing failed because the conversion technology is not researched.");
			}
			return false;
		}
		if (army.IsInEncounter)
		{
			if (!isOrderCheck)
			{
				Diagnostics.LogError("Order preprocessing failed because the army is only made of minor units (guid: {0:X8}).", new object[]
				{
					army.GUID
				});
			}
			return false;
		}
		if (!army.Units.Any((Unit unit) => !unit.SimulationObject.Tags.Contains("UnitFactionTypeMinorFaction") && !unit.SimulationObject.Tags.Contains(TradableUnit.ReadOnlyMercenary)))
		{
			if (!isOrderCheck)
			{
				Diagnostics.LogError("Order preprocessing failed because the army is only made of minor units (guid: {0:X8}).", new object[]
				{
					army.GUID
				});
			}
			return false;
		}
		return true;
	}

	public static bool IsArmyAbleToTerraform(Army army)
	{
		return DepartmentOfTheInterior.CanInvokeTerraformationDevices(army.Empire) && !army.IsInEncounter && !army.IsPillaging && !army.IsDismantlingCreepingNode;
	}

	public static bool IsPointOfInterestVisible(Amplitude.Unity.Game.Empire empire, PointOfInterest pointOfInterest)
	{
		DepartmentOfScience agency = empire.GetAgency<DepartmentOfScience>();
		string technologyName;
		return !pointOfInterest.PointOfInterestDefinition.TryGetValue("VisibilityTechnology", out technologyName) || pointOfInterest.PointOfInterestImprovement != null || agency.GetTechnologyState(technologyName) == DepartmentOfScience.ConstructibleElement.State.Researched;
	}

	public static void RemoveAnomalyDescriptor(SimulationObject districtProxy, StaticString anomalyTypeName)
	{
		if (StaticString.IsNullOrEmpty(anomalyTypeName))
		{
			return;
		}
		AnomalyTypeMapping anomalyTypeMapping;
		if (DepartmentOfTheInterior.AnomalyTypeMappingDatabaseStatic.TryGetValue(anomalyTypeName, out anomalyTypeMapping))
		{
			if (anomalyTypeMapping.Layers == null)
			{
				Diagnostics.LogWarning("The anomaly type mapping '{0}' has no layer.", new object[]
				{
					anomalyTypeName
				});
				return;
			}
			for (int i = 0; i < anomalyTypeMapping.Layers.Length; i++)
			{
				if (!(anomalyTypeMapping.Layers[i].Name != DepartmentOfTheInterior.SimulationLayerName))
				{
					if (!(anomalyTypeMapping.Layers[i].Type != DepartmentOfTheInterior.SimulationLayerType))
					{
						DepartmentOfTheInterior.RemoveSimulationLayerDescriptors(districtProxy, anomalyTypeMapping.Layers[i]);
					}
				}
			}
		}
	}

	public static void RemoveAnyAnomalyDescriptor(SimulationObject districtProxy)
	{
		foreach (AnomalyTypeMapping anomalyTypeMapping in DepartmentOfTheInterior.AnomalyTypeMappingDatabaseStatic.GetValues())
		{
			if (anomalyTypeMapping.Layers != null && anomalyTypeMapping.Layers.Length != 0)
			{
				for (int j = 0; j < anomalyTypeMapping.Layers.Length; j++)
				{
					if (!(anomalyTypeMapping.Layers[j].Name != DepartmentOfTheInterior.SimulationLayerName))
					{
						if (!(anomalyTypeMapping.Layers[j].Type != DepartmentOfTheInterior.SimulationLayerType))
						{
							DepartmentOfTheInterior.RemoveSimulationLayerDescriptors(districtProxy, anomalyTypeMapping.Layers[j]);
						}
					}
				}
			}
		}
	}

	public static void RemoveAnyBiomeTypeDescriptor(SimulationObject districtProxy)
	{
		foreach (BiomeTypeMapping biomeTypeMapping in DepartmentOfTheInterior.BiomeTypeMappingDatabaseStatic.GetValues())
		{
			if (biomeTypeMapping.Layers != null && biomeTypeMapping.Layers.Length != 0)
			{
				for (int j = 0; j < biomeTypeMapping.Layers.Length; j++)
				{
					if (!(biomeTypeMapping.Layers[j].Name != DepartmentOfTheInterior.SimulationLayerName))
					{
						if (!(biomeTypeMapping.Layers[j].Type != DepartmentOfTheInterior.SimulationLayerType))
						{
							DepartmentOfTheInterior.RemoveSimulationLayerDescriptors(districtProxy, biomeTypeMapping.Layers[j]);
						}
					}
				}
			}
		}
	}

	public static void RemoveAnyRiverTypeDescriptor(SimulationObject districtProxy)
	{
		foreach (RiverTypeMapping riverTypeMapping in DepartmentOfTheInterior.RiverTypeMappingDatabaseStatic.GetValues())
		{
			if (riverTypeMapping.Layers != null && riverTypeMapping.Layers.Length != 0)
			{
				for (int j = 0; j < riverTypeMapping.Layers.Length; j++)
				{
					if (!(riverTypeMapping.Layers[j].Name != DepartmentOfTheInterior.SimulationLayerName))
					{
						if (!(riverTypeMapping.Layers[j].Type != DepartmentOfTheInterior.SimulationLayerType))
						{
							DepartmentOfTheInterior.RemoveSimulationLayerDescriptors(districtProxy, riverTypeMapping.Layers[j]);
						}
					}
				}
			}
		}
	}

	public static void RemoveAnyTerrainTypeDescriptor(SimulationObject districtProxy)
	{
		foreach (TerrainTypeMapping terrainTypeMapping in DepartmentOfTheInterior.TerrainTypeMappingDatabaseStatic.GetValues())
		{
			if (terrainTypeMapping.Layers != null && terrainTypeMapping.Layers.Length != 0)
			{
				for (int j = 0; j < terrainTypeMapping.Layers.Length; j++)
				{
					if (!(terrainTypeMapping.Layers[j].Name != DepartmentOfTheInterior.SimulationLayerName))
					{
						if (!(terrainTypeMapping.Layers[j].Type != DepartmentOfTheInterior.SimulationLayerType))
						{
							DepartmentOfTheInterior.RemoveSimulationLayerDescriptors(districtProxy, terrainTypeMapping.Layers[j]);
						}
					}
				}
			}
		}
	}

	public static void RemoveBiomeTypeDescriptor(SimulationObject districtProxy, StaticString biomeTypeName)
	{
		if (StaticString.IsNullOrEmpty(biomeTypeName))
		{
			return;
		}
		BiomeTypeMapping biomeTypeMapping;
		if (DepartmentOfTheInterior.BiomeTypeMappingDatabaseStatic.TryGetValue(biomeTypeName, out biomeTypeMapping))
		{
			if (biomeTypeMapping.Layers == null)
			{
				Diagnostics.LogWarning("The terrain type mapping '{0}' has no layer.", new object[]
				{
					biomeTypeName
				});
				return;
			}
			for (int i = 0; i < biomeTypeMapping.Layers.Length; i++)
			{
				if (!(biomeTypeMapping.Layers[i].Name != DepartmentOfTheInterior.SimulationLayerName))
				{
					if (!(biomeTypeMapping.Layers[i].Type != DepartmentOfTheInterior.SimulationLayerType))
					{
						DepartmentOfTheInterior.RemoveSimulationLayerDescriptors(districtProxy, biomeTypeMapping.Layers[i]);
					}
				}
			}
		}
	}

	public static void RemoveRiverTypeDescriptor(SimulationObject districtProxy, StaticString riverTypeName)
	{
		if (StaticString.IsNullOrEmpty(riverTypeName))
		{
			return;
		}
		RiverTypeMapping riverTypeMapping;
		if (DepartmentOfTheInterior.RiverTypeMappingDatabaseStatic.TryGetValue(riverTypeName, out riverTypeMapping))
		{
			if (riverTypeMapping.Layers == null)
			{
				Diagnostics.LogWarning("The terrain type mapping '{0}' has no layer.", new object[]
				{
					riverTypeName
				});
				return;
			}
			for (int i = 0; i < riverTypeMapping.Layers.Length; i++)
			{
				if (!(riverTypeMapping.Layers[i].Name != DepartmentOfTheInterior.SimulationLayerName))
				{
					if (!(riverTypeMapping.Layers[i].Type != DepartmentOfTheInterior.SimulationLayerType))
					{
						DepartmentOfTheInterior.RemoveSimulationLayerDescriptors(districtProxy, riverTypeMapping.Layers[i]);
					}
				}
			}
		}
	}

	public static void RemoveTerrainTypeDescriptor(SimulationObject districtProxy, StaticString terrainTypeName)
	{
		if (StaticString.IsNullOrEmpty(terrainTypeName))
		{
			return;
		}
		TerrainTypeMapping terrainTypeMapping;
		if (DepartmentOfTheInterior.TerrainTypeMappingDatabaseStatic.TryGetValue(terrainTypeName, out terrainTypeMapping))
		{
			if (terrainTypeMapping.Layers == null)
			{
				Diagnostics.LogWarning("The terrain type mapping '{0}' has no layer.", new object[]
				{
					terrainTypeName
				});
				return;
			}
			for (int i = 0; i < terrainTypeMapping.Layers.Length; i++)
			{
				if (!(terrainTypeMapping.Layers[i].Name != DepartmentOfTheInterior.SimulationLayerName))
				{
					if (!(terrainTypeMapping.Layers[i].Type != DepartmentOfTheInterior.SimulationLayerType))
					{
						DepartmentOfTheInterior.RemoveSimulationLayerDescriptors(districtProxy, terrainTypeMapping.Layers[i]);
					}
				}
			}
		}
	}

	public static bool TryGetWorldPositionForNewArmyFromCamp(Camp camp, IPathfindingService pathFindingService, PathfindingContext pathfindingContext, out WorldPosition worldPosition)
	{
		worldPosition = WorldPosition.Invalid;
		bool flag = true;
		bool flag2 = false;
		int num = 0;
		while (flag && !flag2)
		{
			flag2 = true;
			for (int i = 0; i < camp.Districts.Count; i++)
			{
				if (camp.Districts[i].Type != DistrictType.Improvement && camp.Districts[i].Type != DistrictType.Exploitation)
				{
					if (DepartmentOfTheInterior.WorldPositionningServiceStatic.GetDistance(camp.WorldPosition, camp.Districts[i].WorldPosition) == num)
					{
						flag2 = false;
						Army armyAtPosition = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetArmyAtPosition(camp.Districts[i].WorldPosition);
						if (armyAtPosition == null)
						{
							if (DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(camp.Districts[i].WorldPosition, PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.Water) && pathFindingService.IsTilePassable(camp.Districts[i].WorldPosition, pathfindingContext, (PathfindingFlags)0, null) && pathFindingService.IsTileStopable(camp.Districts[i].WorldPosition, pathfindingContext, (PathfindingFlags)0, null))
							{
								worldPosition = camp.Districts[i].WorldPosition;
								return true;
							}
						}
					}
				}
			}
			num++;
		}
		return false;
	}

	public static bool TryGetWorldPositionForNewArmyFromCity(City city, IPathfindingService pathFindingService, PathfindingContext pathfindingContext, out WorldPosition worldPosition)
	{
		worldPosition = WorldPosition.Invalid;
		bool flag = city.BesiegingEmpire != null;
		bool flag2 = true;
		bool flag3 = false;
		int num = 0;
		while (flag2 && !flag3)
		{
			flag3 = true;
			for (int i = 0; i < city.Districts.Count; i++)
			{
				if (city.Districts[i].Type != DistrictType.Improvement && (!flag || city.Districts[i].Type != DistrictType.Exploitation))
				{
					if (DepartmentOfTheInterior.WorldPositionningServiceStatic.GetDistance(city.WorldPosition, city.Districts[i].WorldPosition) == num)
					{
						flag3 = false;
						Army armyAtPosition = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetArmyAtPosition(city.Districts[i].WorldPosition);
						if (armyAtPosition == null)
						{
							if (DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(city.Districts[i].WorldPosition, PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.Water) && pathFindingService.IsTilePassable(city.Districts[i].WorldPosition, pathfindingContext, (PathfindingFlags)0, null) && pathFindingService.IsTileStopable(city.Districts[i].WorldPosition, pathfindingContext, (PathfindingFlags)0, null))
							{
								worldPosition = city.Districts[i].WorldPosition;
								return true;
							}
						}
					}
				}
			}
			num++;
		}
		return false;
	}

	public void ToggleCityRoundUp(City city)
	{
		if (Databases.GetDatabase<SimulationDescriptor>(false) == null)
		{
			Diagnostics.LogError("Fail getting simulation descriptor database.");
			return;
		}
		if (!city.SimulationObject.Tags.Contains(DepartmentOfTheInterior.CityStatusRoundUpDescriptorName))
		{
			this.StartRoundUp(city);
		}
		else
		{
			this.StopRoundUp(city);
		}
	}

	public void StartRoundUp(City city)
	{
		if (city == null)
		{
			Diagnostics.LogError("City can't be null");
			return;
		}
		if (!city.SimulationObject.Tags.Contains(DepartmentOfTheInterior.CityStatusRoundUpDescriptorName))
		{
			IDatabase<SimulationDescriptor> database = Databases.GetDatabase<SimulationDescriptor>(false);
			SimulationDescriptor value = database.GetValue(DepartmentOfTheInterior.CityStatusRoundUpDescriptorName);
			if (value != null)
			{
				city.AddDescriptor(value, false);
			}
		}
		city.SetPropertyBaseValue(SimulationProperties.RoundUpProgress, 0f);
	}

	public void StopRoundUp(City city)
	{
		if (city == null)
		{
			Diagnostics.LogError("City can't be null");
			return;
		}
		if (city.SimulationObject.Tags.Contains(DepartmentOfTheInterior.CityStatusRoundUpDescriptorName))
		{
			city.RemoveDescriptorByName(DepartmentOfTheInterior.CityStatusRoundUpDescriptorName);
		}
		city.SetPropertyBaseValue(SimulationProperties.RoundUpProgress, -1f);
	}

	private static void ApplyDistrictType(District district, DistrictType districtType, StaticString districtDescriptorType = null)
	{
		district.Type = districtType;
		district.SetLevel(0, true);
		SimulationDescriptor descriptor;
		if (!district.SimulationObject.Tags.Contains("DistrictLevel0") && DepartmentOfTheInterior.SimulationDescriptorDatabaseStatic.TryGetValue("DistrictLevel0", out descriptor))
		{
			district.AddDescriptor(descriptor, false);
		}
		string text = string.Format("DistrictType{0}", districtType.ToString());
		if (DepartmentOfTheInterior.SimulationDescriptorDatabaseStatic.TryGetValue(text, out descriptor))
		{
			district.SwapDescriptor(descriptor);
		}
		else
		{
			Diagnostics.LogError("Unable to retrieve the simulation descriptor (name: '{0}', type: {1}) from the database.", new object[]
			{
				text,
				districtType
			});
		}
	}

	private static void ApplyDistrictTypeDescriptor(SimulationObject districtProxy, DistrictType districtType)
	{
		SimulationDescriptor descriptor;
		if (DepartmentOfTheInterior.SimulationDescriptorDatabaseStatic.TryGetValue("DistrictLevel0", out descriptor))
		{
			districtProxy.AddDescriptor(descriptor);
		}
		string text = string.Format("DistrictType{0}", districtType.ToString());
		if (DepartmentOfTheInterior.SimulationDescriptorDatabaseStatic.TryGetValue(text, out descriptor))
		{
			districtProxy.AddDescriptor(descriptor);
		}
		else
		{
			Diagnostics.LogError("Unable to retrieve the '{0}' simulation descriptor from the database.", new object[]
			{
				text
			});
		}
	}

	private static void ApplyTerrainTypeDescriptor(SimulationObject districtProxy, byte terrainType)
	{
		StaticString terrainTypeMappingName = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetTerrainTypeMappingName(terrainType);
		if (StaticString.IsNullOrEmpty(terrainTypeMappingName))
		{
			Diagnostics.LogWarning("Unable to retrieve the '{0}' terrain type from the terrain type mapping table.", new object[]
			{
				terrainType
			});
			return;
		}
		DepartmentOfTheInterior.ApplyTerrainTypeDescriptor(districtProxy, terrainTypeMappingName);
	}

	private static void ApplyWorldEffectTypeDescriptor(SimulationObject district, WorldPosition worldPosition, bool districtIsProxy)
	{
		DepartmentOfTheInterior.WorldEffectServiceStatic.AddFidsModifierDescriptors(district, worldPosition, districtIsProxy);
	}

	private static void ApplyPointOfInterestDescriptors(Amplitude.Unity.Game.Empire empire, SimulationObject districtProxy, WorldPosition worldPosition, DistrictType districtType)
	{
		Region region = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetRegion(worldPosition);
		for (int i = 0; i < region.PointOfInterests.Length; i++)
		{
			if (region.PointOfInterests[i].WorldPosition == worldPosition)
			{
				DepartmentOfTheInterior.ApplyPointOfInterestDescriptors(empire, districtProxy, region.PointOfInterests[i], districtType);
			}
		}
	}

	private static void ApplyPointOfInterestDescriptors(Amplitude.Unity.Game.Empire empire, SimulationObject districtProxy, PointOfInterest pointOfInterest, DistrictType districtType)
	{
		DepartmentOfScience agency = empire.GetAgency<DepartmentOfScience>();
		string text;
		if (pointOfInterest.PointOfInterestDefinition.TryGetValue("VisibilityTechnology", out text) && pointOfInterest.PointOfInterestImprovement == null && agency.GetTechnologyState(text) != DepartmentOfScience.ConstructibleElement.State.Researched)
		{
			return;
		}
		SimulationDescriptor descriptor;
		if (pointOfInterest.PointOfInterestDefinition.TryGetValue("DistrictBonus", out text) && DepartmentOfTheInterior.SimulationDescriptorDatabaseStatic.TryGetValue(text, out descriptor))
		{
			districtProxy.AddDescriptor(descriptor);
		}
		if (DepartmentOfTheInterior.SimulationDescriptorDatabaseStatic.TryGetValue("TerrainTagPointOfInterest", out descriptor))
		{
			districtProxy.AddDescriptor(descriptor);
		}
		if (pointOfInterest.Type == "QuestLocation" && DepartmentOfTheInterior.SimulationDescriptorDatabaseStatic.TryGetValue("TerrainTagQuestLocation", out descriptor))
		{
			districtProxy.AddDescriptor(descriptor);
		}
		DepartmentOfTheInterior.ApplyDistrictDescriptorOnPointOfInterest(pointOfInterest, districtType);
	}

	private static void ApplyDistrictDescriptorOnPointOfInterest(WorldPosition worldPosition, DistrictType districtType)
	{
		Region region = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetRegion(worldPosition);
		for (int i = 0; i < region.PointOfInterests.Length; i++)
		{
			if (region.PointOfInterests[i].WorldPosition == worldPosition)
			{
				DepartmentOfTheInterior.ApplyDistrictDescriptorOnPointOfInterest(region.PointOfInterests[i], districtType);
			}
		}
	}

	private static void ApplyDistrictDescriptorOnPointOfInterest(PointOfInterest pointOfInterest, DistrictType districtType)
	{
		SimulationDescriptor descriptor;
		if (District.IsACityTile(districtType) && DepartmentOfTheInterior.SimulationDescriptorDatabaseStatic.TryGetValue(DepartmentOfTheInterior.PointOfInterestOnDistrict, out descriptor))
		{
			pointOfInterest.SwapDescriptor(descriptor);
		}
	}

	private static void ApplySimulationLayerDescriptors(SimulationObject districtProxy, SimulationLayer simulationLayer)
	{
		for (int i = 0; i < simulationLayer.Samples.Length; i++)
		{
			SimulationDescriptor descriptor;
			if (DepartmentOfTheInterior.SimulationDescriptorDatabaseStatic.TryGetValue(simulationLayer.Samples[i].Value, out descriptor))
			{
				districtProxy.AddDescriptor(descriptor);
			}
			else
			{
				Diagnostics.LogWarning("Unable to retrieve the '{0}' descriptor from the database.", new object[]
				{
					simulationLayer.Samples[i].Value
				});
			}
		}
	}

	private static void ClearResourceOnConvertedVillage(global::Empire converter, PointOfInterest convertedPointOfInterest)
	{
		convertedPointOfInterest.RemoveDescriptorByType("LeachByConversion");
	}

	private static void GenerateResourceForConvertedVillages(Amplitude.Unity.Game.Empire converter, PointOfInterest convertedPointOfInterest)
	{
		DepartmentOfIndustry agency = converter.GetAgency<DepartmentOfIndustry>();
		ConstructibleElement[] availableConstructibleElements = agency.ConstructibleElementDatabase.GetAvailableConstructibleElements(new StaticString[]
		{
			PointOfInterestImprovementDefinition.ReadOnlyCategory
		});
		List<StaticString> list = new List<StaticString>();
		List<int> list2 = new List<int>();
		Region region = convertedPointOfInterest.Region;
		for (int i = 0; i < region.PointOfInterests.Length; i++)
		{
			PointOfInterest pointOfInterest = region.PointOfInterests[i];
			if (!(pointOfInterest.Type != "ResourceDeposit"))
			{
				for (int j = 0; j < availableConstructibleElements.Length; j++)
				{
					PointOfInterestImprovementDefinition pointOfInterestImprovementDefinition = availableConstructibleElements[j] as PointOfInterestImprovementDefinition;
					if (pointOfInterestImprovementDefinition != null && pointOfInterestImprovementDefinition.PointOfInterestTemplateName == pointOfInterest.PointOfInterestDefinition.PointOfInterestTemplateName && DepartmentOfTheTreasury.CheckConstructiblePrerequisites(converter, pointOfInterestImprovementDefinition, new string[]
					{
						ConstructionFlags.Technology
					}))
					{
						string x;
						if (pointOfInterest.PointOfInterestDefinition.TryGetValue("LeachByConversionDescriptor", out x))
						{
							int num = list.IndexOf(x);
							if (num < 0)
							{
								num = list.Count;
								list.Add(x);
								list2.Add(0);
							}
							List<int> list4;
							List<int> list3 = list4 = list2;
							int num2;
							int index = num2 = num;
							num2 = list4[num2];
							list3[index] = num2 + 1;
						}
						break;
					}
				}
			}
		}
		Diagnostics.Assert(list2.Count == list.Count);
		int k = 0;
		while (k < list.Count)
		{
			if (convertedPointOfInterest.SimulationObject.Tags.Contains(list[k]))
			{
				goto IL_1C1;
			}
			SimulationDescriptor descriptor;
			if (DepartmentOfTheInterior.SimulationDescriptorDatabaseStatic.TryGetValue(list[k], out descriptor))
			{
				convertedPointOfInterest.AddDescriptor(descriptor, false);
				goto IL_1C1;
			}
			IL_1D8:
			k++;
			continue;
			IL_1C1:
			convertedPointOfInterest.SetPropertyBaseValue(list[k], (float)list2[k]);
			goto IL_1D8;
		}
	}

	private static void VerifyDistrictRiver(City city)
	{
		for (int i = 0; i < city.Districts.Count; i++)
		{
			short riverId = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetRiverId(city.Districts[i].WorldPosition);
			StaticString riverTypeMappingName = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetRiverTypeMappingName(riverId);
			bool flag = city.Districts[i].SimulationObject.Tags.Contains("TerrainTagRiver");
			if ((flag && StaticString.IsNullOrEmpty(riverTypeMappingName)) || (!flag && !StaticString.IsNullOrEmpty(riverTypeMappingName)))
			{
				DistrictType type = city.Districts[i].Type;
				city.Districts[i].SimulationObject.RemoveAllDescriptors();
				DepartmentOfTheInterior.ApplyDistrictDescriptors(city.Empire, city.Districts[i], type);
			}
		}
	}

	private static void VerifyDistrictDescriptorOnPointOfInterest(City city)
	{
		for (int i = 0; i < city.Districts.Count; i++)
		{
			DepartmentOfTheInterior.ApplyDistrictDescriptorOnPointOfInterest(city.Districts[i].WorldPosition, city.Districts[i].Type);
		}
	}

	private static void VerifyDistrictLevel(City city)
	{
		int num = (int)city.GetPropertyValue(SimulationProperties.MaximumDistrictLevel);
		for (int i = 0; i < city.Districts.Count; i++)
		{
			int num2 = (int)city.Districts[i].GetPropertyValue(SimulationProperties.Level);
			if (num2 > num)
			{
				for (int j = num2; j > num; j--)
				{
					string x = "DistrictLevel" + j;
					if (city.Districts[i].SimulationObject.Tags.Contains(x))
					{
						city.Districts[i].RemoveDescriptorByName(x);
					}
				}
				city.Districts[i].SetLevel(num, true);
			}
		}
	}

	private static void VerifyDistrictAgainstImprovement(City city)
	{
		for (int i = 0; i < city.Districts.Count; i++)
		{
			SimulationDescriptor descriptor;
			if (city.Districts[i].Type == DistrictType.Extension && !city.Districts[i].SimulationObject.Tags.Contains("DistrictImprovementWonder") && !city.Districts[i].SimulationObject.Tags.Contains("DistrictImprovementExtension") && DepartmentOfTheInterior.SimulationDescriptorDatabaseStatic.TryGetValue("DistrictImprovementExtension", out descriptor))
			{
				city.Districts[i].AddDescriptor(descriptor, false);
			}
		}
	}

	private static void RemoveSimulationLayerDescriptors(SimulationObject districtProxy, SimulationLayer simulationLayer)
	{
		for (int i = 0; i < simulationLayer.Samples.Length; i++)
		{
			SimulationDescriptor descriptor;
			if (DepartmentOfTheInterior.SimulationDescriptorDatabaseStatic.TryGetValue(simulationLayer.Samples[i].Value, out descriptor))
			{
				districtProxy.RemoveDescriptor(descriptor);
			}
			else
			{
				Diagnostics.LogWarning("Unable to retrieve the '{0}' descriptor from the database.", new object[]
				{
					simulationLayer.Samples[i].Value
				});
			}
		}
	}

	private static void ReplaceWonder(City city)
	{
		for (int i = 0; i < city.Districts.Count; i++)
		{
			if (city.Districts[i].Type == DistrictType.Extension && !city.Districts[i].SimulationObject.Tags.Contains("DistrictTypeExtension"))
			{
				if (city.Districts[i].SimulationObject.Tags.Contains("DistrictTypeWonder1"))
				{
					DepartmentOfTheInterior.ReplaceWonderBy(city.Districts[i], "DistrictTypeWonder1", "DistrictWonder1");
				}
				else if (city.Districts[i].SimulationObject.Tags.Contains("DistrictTypeWonder2"))
				{
					DepartmentOfTheInterior.ReplaceWonderBy(city.Districts[i], "DistrictTypeWonder2", "DistrictWonder2");
				}
				else if (city.Districts[i].SimulationObject.Tags.Contains("DistrictTypeWonder3"))
				{
					DepartmentOfTheInterior.ReplaceWonderBy(city.Districts[i], "DistrictTypeWonder3", "DistrictWonder3");
				}
				else if (city.Districts[i].SimulationObject.Tags.Contains("DistrictTypeWonder4"))
				{
					DepartmentOfTheInterior.ReplaceWonderBy(city.Districts[i], "DistrictTypeWonder4", "DistrictWonder4");
				}
				else if (city.Districts[i].SimulationObject.Tags.Contains("DistrictTypeWonder5"))
				{
					DepartmentOfTheInterior.ReplaceWonderBy(city.Districts[i], "DistrictTypeWonder5", "DistrictWonder5");
				}
			}
		}
	}

	private static void ReplaceWonderBy(District district, StaticString oldWonderDescriptor, StaticString newWonder)
	{
		district.SimulationObject.RemoveDescriptorByName(oldWonderDescriptor);
		district.SimulationObject.Tags.RemoveTag(oldWonderDescriptor);
		DepartmentOfTheInterior.ApplyDistrictType(district, district.Type, null);
		DepartmentOfIndustry agency = district.City.Empire.GetAgency<DepartmentOfIndustry>();
		DepartmentOfIndustry.ConstructibleElement constructibleElement;
		if (!agency.ConstructibleElementDatabase.TryGetValue(newWonder, out constructibleElement))
		{
			return;
		}
		for (int i = 0; i < constructibleElement.Descriptors.Length; i++)
		{
			district.AddDescriptor(constructibleElement.Descriptors[i], false);
		}
	}

	public override void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		base.ReadXml(reader);
		if (num >= 2)
		{
			this.uniques.SimulationObject.RemoveAllDescriptors();
			reader.ReadElementSerializable<SimulationObjectWrapper>(ref this.uniques);
		}
		int attribute = reader.GetAttribute<int>("Count");
		this.mainCityGUID = reader.GetAttribute<ulong>("MainCityGuid");
		reader.ReadStartElement("Cities");
		this.cities.Clear();
		for (int i = 0; i < attribute; i++)
		{
			ulong attribute2 = reader.GetAttribute<ulong>("GUID");
			City city = new City(attribute2)
			{
				Empire = (base.Empire as global::Empire)
			};
			reader.ReadElementSerializable<City>(ref city);
			if (city != null)
			{
				this.AddCity(city, false, false);
				if (this.mainCityGUID != GameEntityGUID.Zero && this.mainCityGUID == city.GUID)
				{
					this.MainCity = city;
				}
				if (num < 3)
				{
					DepartmentOfTheInterior.VerifyDistrictRiver(city);
					DepartmentOfTheInterior.VerifyDistrictDescriptorOnPointOfInterest(city);
					DepartmentOfTheInterior.VerifyDistrictLevel(city);
				}
				DepartmentOfTheInterior.ReplaceWonder(city);
				if (num < 4)
				{
					DepartmentOfTheInterior.VerifyDistrictAgainstImprovement(city);
				}
			}
		}
		reader.ReadEndElement("Cities");
		if (num < 2)
		{
			foreach (City city2 in this.Cities)
			{
				foreach (CityImprovement cityImprovement in city2.CityImprovements)
				{
					if (cityImprovement.CityImprovementDefinition.HasUniqueTags && this.uniques != null && this.uniques.SimulationObject != null)
					{
						this.uniques.SimulationObject.Tags.AddTag(cityImprovement.CityImprovementDefinition.UniqueTags);
					}
				}
			}
		}
		int attribute3 = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("AssimilatedFactions");
		this.assimilatedFactions.Clear();
		for (int j = 0; j < attribute3; j++)
		{
			string x = reader.ReadElementString("AssimilatedFaction");
			Faction value = this.FactionDatabase.GetValue(x);
			if (value != null)
			{
				this.assimilatedFactions.Add(value);
			}
		}
		reader.ReadEndElement("AssimilatedFactions");
		if (reader.IsStartElement("EncounteredMinorFactions"))
		{
			int attribute4 = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("EncounteredMinorFactions");
			this.encounteredMinorFaction.Clear();
			for (int k = 0; k < attribute4; k++)
			{
				string text = reader.ReadElementString("MinorFaction");
				if (!string.IsNullOrEmpty(text))
				{
					this.encounteredMinorFaction.Add(text);
				}
			}
			reader.ReadEndElement("EncounteredMinorFactions");
		}
		if (reader.IsStartElement("FirstAssimilableMinorFactions"))
		{
			int attribute5 = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("FirstAssimilableMinorFactions");
			this.encounteredMinorFaction.Clear();
			for (int l = 0; l < attribute5; l++)
			{
				string text2 = reader.ReadElementString("MinorFaction");
				if (!string.IsNullOrEmpty(text2))
				{
					this.alreadyNotifyForAssimilation.Add(text2);
				}
			}
			reader.ReadEndElement("FirstAssimilableMinorFactions");
		}
		if (num >= 5)
		{
			this.TurnWhenMilitiaWasLastUpdated = reader.ReadElementString<int>("TurnWhenMilitiaWasLastUpdated");
		}
		if (num >= 6)
		{
			int attribute6 = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("Fortresses");
			this.occupiedFortresses.Clear();
			for (int m = 0; m < attribute6; m++)
			{
				ulong attribute7 = reader.GetAttribute<ulong>("GUID");
				Fortress fortress = new Fortress(attribute7)
				{
					Occupant = (base.Empire as MajorEmpire)
				};
				reader.ReadElementSerializable<Fortress>(ref fortress);
				if (fortress != null)
				{
					if (fortress.PointOfInterest != null)
					{
						fortress.PointOfInterest.Empire = (base.Empire as global::Empire);
					}
					for (int n = 0; n < fortress.Facilities.Count; n++)
					{
						fortress.Facilities[n].Empire = (base.Empire as global::Empire);
					}
					this.occupiedFortresses.Add(fortress);
				}
			}
			reader.ReadEndElement("Fortresses");
			reader.Skip("StockpilesCooldowns");
			reader.Skip("UniqueFacilitiesAlreadyOwned");
		}
		if (num >= 7)
		{
			this.serializableBesiegingSeafaringArmies = reader.ReadElementString("BesiegingSeafaringArmies");
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		int num = writer.WriteVersionAttribute(7);
		base.WriteXml(writer);
		if (num >= 2)
		{
			IXmlSerializable xmlSerializable = this.uniques;
			writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
		}
		writer.WriteStartElement("Cities");
		writer.WriteAttributeString<int>("Count", this.cities.Count);
		writer.WriteAttributeString<ulong>("MainCityGuid", this.MainCityGUID);
		for (int i = 0; i < this.cities.Count; i++)
		{
			IXmlSerializable xmlSerializable2 = this.cities[i];
			writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable2);
		}
		writer.WriteEndElement();
		writer.WriteStartElement("AssimilatedFactions");
		writer.WriteAttributeString<int>("Count", this.assimilatedFactions.Count);
		for (int j = 0; j < this.assimilatedFactions.Count; j++)
		{
			writer.WriteElementString<StaticString>("AssimilatedFaction", this.assimilatedFactions[j].Name);
		}
		writer.WriteEndElement();
		writer.WriteStartElement("EncounteredMinorFactions");
		writer.WriteAttributeString<int>("Count", this.encounteredMinorFaction.Count);
		for (int k = 0; k < this.encounteredMinorFaction.Count; k++)
		{
			writer.WriteElementString("MinorFaction", this.encounteredMinorFaction[k].ToString());
		}
		writer.WriteEndElement();
		writer.WriteStartElement("FirstAssimilableMinorFactions");
		writer.WriteAttributeString<int>("Count", this.alreadyNotifyForAssimilation.Count);
		for (int l = 0; l < this.alreadyNotifyForAssimilation.Count; l++)
		{
			writer.WriteElementString("MinorFaction", this.alreadyNotifyForAssimilation[l].ToString());
		}
		writer.WriteEndElement();
		if (num >= 5)
		{
			writer.WriteElementString<int>("TurnWhenMilitiaWasLastUpdated", this.TurnWhenMilitiaWasLastUpdated);
		}
		writer.WriteStartElement("Fortresses");
		writer.WriteAttributeString<int>("Count", this.occupiedFortresses.Count);
		for (int m = 0; m < this.occupiedFortresses.Count; m++)
		{
			IXmlSerializable xmlSerializable3 = this.occupiedFortresses[m];
			writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable3);
		}
		writer.WriteEndElement();
		if (num >= 7)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int n = 0; n < this.cities.Count; n++)
			{
				City city = this.cities[n];
				if (city.BesiegingSeafaringArmies.Count != 0)
				{
					stringBuilder.Append(city.GUID);
					stringBuilder.Append(',');
					stringBuilder.Append(city.BesiegingSeafaringArmies.Count);
					stringBuilder.Append(',');
					for (int num2 = 0; num2 < city.BesiegingSeafaringArmies.Count; num2++)
					{
						stringBuilder.Append(city.BesiegingSeafaringArmies[num2].Empire.Index);
						stringBuilder.Append(',');
						stringBuilder.Append(city.BesiegingSeafaringArmies[num2].GUID);
						stringBuilder.Append(',');
					}
				}
			}
			writer.WriteElementString("BesiegingSeafaringArmies", stringBuilder.ToString());
		}
	}

	public ReadOnlyCollection<Faction> AssimilatedFactions
	{
		get
		{
			if (this.readOnlyAssimilatedFactions == null)
			{
				this.readOnlyAssimilatedFactions = this.assimilatedFactions.AsReadOnly();
			}
			return this.readOnlyAssimilatedFactions;
		}
	}

	public static float GetAssimilationCost(Amplitude.Unity.Game.Empire empire, int delta = 0)
	{
		if (DepartmentOfTheInterior.assimilationInterpreterContext == null)
		{
			DepartmentOfTheInterior.assimilationInterpreterContext = new InterpreterContext(null);
			string value = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<string>("Gameplay/Agencies/DepartmentOfTheInterior/AssimilationFormula");
			DepartmentOfTheInterior.assimilationFormulaTokens = Interpreter.InfixTransform(value);
		}
		DepartmentOfTheInterior.assimilationInterpreterContext.SimulationObject = empire.SimulationObject;
		DepartmentOfTheInterior.assimilationInterpreterContext.Register("Delta", delta);
		return (float)Interpreter.Execute(DepartmentOfTheInterior.assimilationFormulaTokens, DepartmentOfTheInterior.assimilationInterpreterContext);
	}

	public bool CanAffordAssimilation()
	{
		float num = -DepartmentOfTheInterior.GetAssimilationCost(base.Empire, 0);
		return this.departmentOfTheTreasury.IsTransferOfResourcePossible(base.Empire, "EmpirePoint", ref num);
	}

	[Obsolete("Method doesn't do what its name suggest, use GetAssimilableMinorFactions() instead.", true)]
	public void GetAssimilableMinorEmpires(ref List<MinorEmpire> assimilableMinorEmpires)
	{
		int index;
		for (index = 0; index < this.cities.Count; index++)
		{
			MinorEmpire minorEmpire = this.cities[index].Region.MinorEmpire;
			if (minorEmpire != null)
			{
				BarbarianCouncil agency = minorEmpire.GetAgency<BarbarianCouncil>();
				if (agency.HasAtLeastOneVillagePacified && !assimilableMinorEmpires.Exists((MinorEmpire match) => match.Faction.Name == this.cities[index].Region.MinorEmpire.Faction.Name))
				{
					assimilableMinorEmpires.Add(this.cities[index].Region.MinorEmpire);
				}
			}
		}
	}

	public int GetNumberOfOwnedMinorFactionVillages(MinorFaction minorFaction, bool rebuilt = false)
	{
		return this.GetNumberOfInfectedVillages(minorFaction) + this.GetNumberOfConvertedVillages(minorFaction) + this.GetNumberOfPacifiedVillages(minorFaction, rebuilt);
	}

	public int GetNumberOfInfectedVillages(MinorFaction minorFaction)
	{
		int num = 0;
		MajorEmpire majorEmpire = base.Empire as MajorEmpire;
		if (majorEmpire != null)
		{
			for (int i = 0; i < majorEmpire.InfectedVillages.Count; i++)
			{
				if (majorEmpire.InfectedVillages[i].MinorEmpire.MinorFaction.Name == minorFaction.Name)
				{
					num++;
				}
			}
		}
		return num;
	}

	public int GetNumberOfConvertedVillages(MinorFaction minorFaction)
	{
		int num = 0;
		MajorEmpire majorEmpire = base.Empire as MajorEmpire;
		if (majorEmpire != null)
		{
			for (int i = 0; i < majorEmpire.ConvertedVillages.Count; i++)
			{
				if (majorEmpire.ConvertedVillages[i].MinorEmpire.MinorFaction.Name == minorFaction.Name)
				{
					num++;
				}
			}
		}
		return num;
	}

	public int GetNumberOfPacifiedVillages(MinorFaction minorFaction, bool rebuilt = false)
	{
		int num = 0;
		for (int i = 0; i < this.cities.Count; i++)
		{
			MinorEmpire minorEmpire = this.cities[i].Region.MinorEmpire;
			if (minorEmpire != null && minorEmpire.MinorFaction.Name == minorFaction.Name)
			{
				BarbarianCouncil agency = minorEmpire.GetAgency<BarbarianCouncil>();
				if (agency != null)
				{
					for (int j = 0; j < agency.Villages.Count; j++)
					{
						if ((!rebuilt || agency.Villages[j].PointOfInterest.PointOfInterestImprovement != null) && agency.Villages[j].HasBeenPacified && !agency.Villages[j].HasBeenInfected)
						{
							num++;
						}
					}
				}
			}
		}
		return num;
	}

	public void GetAssimilableMinorFactions(ref List<MinorFaction> assimilableMinorFactions)
	{
		int index;
		for (index = 0; index < this.cities.Count; index++)
		{
			MinorEmpire minorEmpire2 = this.cities[index].Region.MinorEmpire;
			if (minorEmpire2 != null && minorEmpire2.MinorFaction != null)
			{
				BarbarianCouncil agency = minorEmpire2.GetAgency<BarbarianCouncil>();
				if (agency.HasAtLeastOneNonInfectedVillagePacified)
				{
					int num = 0;
					foreach (Village village in agency.Villages)
					{
						int explorationBits = this.WorldPositionningService.GetExplorationBits(village.WorldPosition);
						if ((explorationBits & base.Empire.Bits) == base.Empire.Bits)
						{
							num++;
							break;
						}
					}
					if (num != 0 && !assimilableMinorFactions.Exists((MinorFaction match) => match.Name == this.cities[index].Region.MinorEmpire.MinorFaction.Name))
					{
						assimilableMinorFactions.Add(this.cities[index].Region.MinorEmpire.MinorFaction);
					}
				}
			}
		}
		MajorEmpire majorEmpire = base.Empire as MajorEmpire;
		if (majorEmpire != null)
		{
			if (majorEmpire.ConvertedVillages != null)
			{
				for (int i = 0; i < majorEmpire.ConvertedVillages.Count; i++)
				{
					Village village2 = majorEmpire.ConvertedVillages[i];
					MinorEmpire minorEmpire = village2.MinorEmpire;
					if (minorEmpire != null && minorEmpire.MinorFaction != null && !assimilableMinorFactions.Exists((MinorFaction match) => match.Name == minorEmpire.MinorFaction.Name))
					{
						assimilableMinorFactions.Add(minorEmpire.MinorFaction);
					}
				}
			}
			if (majorEmpire.InfectedVillages != null)
			{
				for (int j = 0; j < majorEmpire.InfectedVillages.Count; j++)
				{
					Village village3 = majorEmpire.InfectedVillages[j];
					MinorEmpire minorEmpire = village3.MinorEmpire;
					if (minorEmpire != null && minorEmpire.MinorFaction != null && !assimilableMinorFactions.Exists((MinorFaction match) => match.Name == minorEmpire.MinorFaction.Name))
					{
						assimilableMinorFactions.Add(minorEmpire.MinorFaction);
					}
				}
			}
		}
	}

	public bool IsAssimilated(Faction faction)
	{
		return this.assimilatedFactions.Exists((Faction match) => match.Name == faction.Name);
	}

	public void ForceGrowthToCurrentPopulation(City city, float previousPopulation)
	{
		city.Refresh(true);
		float propertyValue = city.GetPropertyValue(SimulationProperties.Population);
		if (propertyValue == previousPopulation)
		{
			return;
		}
		DepartmentOfTheTreasury agency = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		if (agency != null)
		{
			float num;
			if (!agency.TryGetResourceStockValue(city, DepartmentOfTheTreasury.Resources.CityGrowth, out num, false))
			{
				Diagnostics.LogError("Can't get resource stock value {0} on simulation object {1}.", new object[]
				{
					DepartmentOfTheTreasury.Resources.CityGrowth,
					city
				});
			}
			float num2 = DepartmentOfTheInterior.ComputeGrowthLimit(base.Empire.SimulationObject, propertyValue) + 1f;
			if (previousPopulation > 0f)
			{
				float num3 = DepartmentOfTheInterior.ComputeGrowthLimit(base.Empire.SimulationObject, previousPopulation);
				float num4 = DepartmentOfTheInterior.ComputeGrowthLimit(base.Empire.SimulationObject, previousPopulation + 1f);
				float num5 = (num - num3) / (num4 - num3);
				num2 += num5 * DepartmentOfTheInterior.ComputeGrowthLimit(base.Empire.SimulationObject, propertyValue + 1f);
			}
			float amount = num2 - num;
			agency.TryTransferResources(city, DepartmentOfTheTreasury.Resources.CityGrowth, amount);
		}
		this.VerifyOverallPopulation(city);
	}

	public void BindMinorFactionToCity(City city, MinorEmpire minorEmpire)
	{
		MajorEmpire majorEmpire = city.Empire as MajorEmpire;
		if (majorEmpire == null)
		{
			return;
		}
		if (minorEmpire == null)
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		BarbarianCouncil agency = minorEmpire.GetAgency<BarbarianCouncil>();
		for (int i = 0; i < agency.Villages.Count; i++)
		{
			Village village = agency.Villages[i];
			if (village.HasBeenPacified)
			{
				if (village.Region.City != null && village.Region.City.Empire.Index == city.Empire.Index)
				{
					num++;
					if (city.Region != null && village.Region.Index == city.Region.Index)
					{
						if (village.PointOfInterest.SimulationObject.Parent != city.SimulationObject)
						{
							city.AddChild(village.PointOfInterest);
							village.PointOfInterest.Empire = majorEmpire;
						}
					}
					else if (village.PointOfInterest.SimulationObject.Parent != village.Region.City.SimulationObject)
					{
						village.Region.City.AddChild(village.PointOfInterest);
						village.PointOfInterest.Empire = majorEmpire;
					}
				}
				else if (village.Region.City == null)
				{
					city.RemoveChild(village.PointOfInterest);
					village.PointOfInterest.Empire = null;
				}
			}
			else if (village.HasBeenConverted)
			{
				if (village.Converter.Index == city.Empire.Index)
				{
					num2++;
					City city2 = null;
					DepartmentOfTheInterior agency2 = majorEmpire.GetAgency<DepartmentOfTheInterior>();
					if (agency2 != null && agency2.MainCity != null)
					{
						city2 = agency2.MainCity;
					}
					if (city2 != null)
					{
						if (village.PointOfInterest.SimulationObject.Parent != city2.SimulationObject)
						{
							city2.AddChild(village.PointOfInterest);
							village.PointOfInterest.Empire = city2.Empire;
						}
					}
					else if (village.PointOfInterest.SimulationObject.Parent != null)
					{
						village.PointOfInterest.SimulationObject.Parent.RemoveChild(village.PointOfInterest);
					}
				}
				else
				{
					city.RemoveChild(village.PointOfInterest);
				}
			}
			else
			{
				city.RemoveChild(village.PointOfInterest);
				village.PointOfInterest.Empire = null;
			}
		}
		if (num + num2 > 0)
		{
			if (num > 0)
			{
				SimulationDescriptor value = this.SimulationDescriptorDatabase.GetValue(minorEmpire.MinorFaction.CityPacificationDescriptor);
				if (value != null)
				{
					city.SwapDescriptor(value);
				}
			}
			else
			{
				SimulationDescriptor value2 = this.SimulationDescriptorDatabase.GetValue(minorEmpire.MinorFaction.CityPacificationDescriptor);
				if (value2 != null)
				{
					city.RemoveDescriptor(value2);
				}
			}
			this.NotifyMinorFactionRdyForAssimilation(minorEmpire);
		}
		else
		{
			this.UnbindMinorEmpireToCity(city, minorEmpire);
		}
	}

	public void NotifyMinorFactionRdyForAssimilation(MinorEmpire minorEmpire)
	{
		if (!this.alreadyNotifyForAssimilation.Contains(minorEmpire.MinorFaction.Name))
		{
			this.alreadyNotifyForAssimilation.Add(minorEmpire.MinorFaction.Name);
			this.EventService.Notify(new EventMinorFactionAssimilable(base.Empire, minorEmpire.MinorFaction));
		}
	}

	public void UnbindInfectedVillage(Village village)
	{
		MajorEmpire majorEmpire = base.Empire as MajorEmpire;
		if (majorEmpire != null && majorEmpire.InfectedVillages != null && this.IsAssimilated(village.MinorEmpire.MinorFaction))
		{
			int num = 0;
			for (int i = 0; i < majorEmpire.InfectedVillages.Count; i++)
			{
				if (!(majorEmpire.InfectedVillages[i].GUID == village.GUID))
				{
					if (majorEmpire.InfectedVillages[i].MinorEmpire.Faction.Name == village.MinorEmpire.MinorFaction.Name)
					{
						num++;
						break;
					}
				}
			}
			if (num == 0)
			{
				int num2 = 0;
				foreach (City city in this.Cities)
				{
					if (city.Region != null && city.Region.MinorEmpire != null && !(city.Region.MinorEmpire.Faction.Name != village.MinorEmpire.MinorFaction.Name))
					{
						BarbarianCouncil agency = city.Region.MinorEmpire.GetAgency<BarbarianCouncil>();
						if (agency.HasAtLeastOneVillagePacified)
						{
							num2++;
							break;
						}
					}
				}
				if (num2 == 0)
				{
					this.DeassimilateFaction(village.MinorEmpire.MinorFaction);
				}
			}
		}
	}

	public void UnbindConvertedVillage(Village village)
	{
		MajorEmpire majorEmpire = base.Empire as MajorEmpire;
		if (majorEmpire != null && majorEmpire.ConvertedVillages != null && this.IsAssimilated(village.MinorEmpire.MinorFaction))
		{
			int num = 0;
			for (int i = 0; i < majorEmpire.ConvertedVillages.Count; i++)
			{
				if (!(majorEmpire.ConvertedVillages[i].GUID == village.GUID))
				{
					if (majorEmpire.ConvertedVillages[i].MinorEmpire.Faction.Name == village.MinorEmpire.MinorFaction.Name)
					{
						num++;
						break;
					}
				}
			}
			if (num == 0)
			{
				int num2 = 0;
				foreach (City city in this.Cities)
				{
					if (city.Region != null && city.Region.MinorEmpire != null && !(city.Region.MinorEmpire.Faction.Name != village.MinorEmpire.MinorFaction.Name))
					{
						BarbarianCouncil agency = city.Region.MinorEmpire.GetAgency<BarbarianCouncil>();
						if (agency.HasAtLeastOneVillagePacified)
						{
							num2++;
							break;
						}
					}
				}
				if (num2 == 0)
				{
					this.DeassimilateFaction(village.MinorEmpire.MinorFaction);
				}
			}
		}
		if (this.MainCity != null)
		{
			if (this.MainCity.SimulationObject.Children.Contains(village.PointOfInterest))
			{
				this.MainCity.RemoveChild(village.PointOfInterest);
			}
			if (village.PointOfInterest.Empire == majorEmpire)
			{
				village.PointOfInterest.Empire = null;
			}
			this.VerifyOverallPopulation(this.MainCity);
		}
	}

	public void UnbindMinorEmpireToCity(City city, MinorEmpire minorEmpire)
	{
		if (minorEmpire == null)
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		BarbarianCouncil agency = minorEmpire.GetAgency<BarbarianCouncil>();
		for (int i = 0; i < agency.Villages.Count; i++)
		{
			Village village = agency.Villages[i];
			if (village != null)
			{
				if (village.HasBeenPacified)
				{
					num++;
					city.RemoveChild(village.PointOfInterest);
					if (!village.HasBeenInfected)
					{
						village.PointOfInterest.Empire = null;
					}
				}
				else if (village.HasBeenConverted && village.Converter.Index != base.Empire.Index)
				{
					Region region = this.WorldPositionningService.GetRegion(city.WorldPosition);
					if (region != null && village.PointOfInterest.Region.Index == region.Index)
					{
						num2++;
						city.RemoveChild(village.PointOfInterest);
					}
				}
			}
		}
		SimulationDescriptor value = this.SimulationDescriptorDatabase.GetValue(minorEmpire.MinorFaction.CityPacificationDescriptor);
		if (value != null)
		{
			city.RemoveDescriptor(value);
		}
		if (this.IsAssimilated(minorEmpire.MinorFaction))
		{
			int num3 = 0;
			MajorEmpire majorEmpire = base.Empire as MajorEmpire;
			if (majorEmpire != null && majorEmpire.ConvertedVillages != null)
			{
				for (int j = 0; j < majorEmpire.ConvertedVillages.Count; j++)
				{
					if (majorEmpire.ConvertedVillages[j].MinorEmpire.Faction.Name == minorEmpire.MinorFaction.Name)
					{
						num3++;
						break;
					}
				}
			}
			int num4 = 0;
			foreach (City city2 in this.cities)
			{
				Diagnostics.Assert(city2.Region != null);
				Diagnostics.Assert(city2.Region.MinorEmpire != null);
				if (city2 != city)
				{
					if (city2.Region.MinorEmpire.Faction.Name == minorEmpire.MinorFaction.Name)
					{
						BarbarianCouncil agency2 = city2.Region.MinorEmpire.GetAgency<BarbarianCouncil>();
						if (agency2.HasAtLeastOneVillagePacified)
						{
							num4++;
							break;
						}
					}
				}
			}
			if (num4 + num3 <= 0)
			{
				this.DeassimilateFaction(minorEmpire.MinorFaction);
				EventAssimilationLost eventToNotify = new EventAssimilationLost(base.Empire, minorEmpire.MinorFaction);
				this.EventService.Notify(eventToNotify);
			}
		}
	}

	public void RebuildVillage(Village village)
	{
		Diagnostics.Assert(village.PointOfInterest != null);
		if (village.PointOfInterest != null && village.PointOfInterest.PointOfInterestImprovement == null)
		{
			ConstructibleElement[] availableConstructibleElements = this.departmentOfIndustry.ConstructibleElementDatabase.GetAvailableConstructibleElements(new StaticString[0]);
			for (int i = 0; i < availableConstructibleElements.Length; i++)
			{
				PointOfInterestImprovementDefinition pointOfInterestImprovementDefinition = availableConstructibleElements[i] as PointOfInterestImprovementDefinition;
				if (pointOfInterestImprovementDefinition != null)
				{
					if (!(pointOfInterestImprovementDefinition.PointOfInterestTemplateName != village.PointOfInterest.PointOfInterestDefinition.PointOfInterestTemplateName))
					{
						if (this.MainCity != null)
						{
							this.BuildPointOfInterestImprovement(this.MainCity, village.PointOfInterest, pointOfInterestImprovementDefinition);
						}
						else
						{
							this.BuildPointOfInterestImprovement(village.PointOfInterest, pointOfInterestImprovementDefinition);
						}
						Region region = village.PointOfInterest.Region;
						if (region.Owner != null && region.City != null)
						{
							DepartmentOfIndustry agency = region.Owner.GetAgency<DepartmentOfIndustry>();
							if (agency != null)
							{
								ConstructionQueue constructionQueue = agency.GetConstructionQueue(region.City);
								if (constructionQueue != null && pointOfInterestImprovementDefinition != null)
								{
									for (int j = 0; j < constructionQueue.Length; j++)
									{
										Construction construction = constructionQueue.PeekAt(j);
										if (construction != null && construction.ConstructibleElementName == pointOfInterestImprovementDefinition.Name && construction.WorldPosition == village.WorldPosition)
										{
											if (construction.IsInProgress)
											{
												agency.RemoveConstructionQueueDescriptors(region.City, construction);
											}
											this.GameEntityRepositoryService.Unregister(construction);
											constructionQueue.Remove(construction);
											break;
										}
									}
								}
							}
						}
						break;
					}
				}
			}
		}
	}

	private void AssimilateMinorFaction(StaticString minorFactionName)
	{
		if (!this.FactionDatabase.ContainsKey(minorFactionName))
		{
			Diagnostics.Log("Cannot find the minor faction in the database (name: '{0}').", new object[]
			{
				minorFactionName
			});
			return;
		}
		MinorFaction minorFaction = this.FactionDatabase.GetValue(minorFactionName) as MinorFaction;
		this.AssimilateMinorFaction(minorFaction);
	}

	private void AssimilateMinorFaction(MinorFaction minorFaction)
	{
		if (minorFaction == null)
		{
			Diagnostics.LogError("Cannot assimilate the minor faction because it is null (trying to assimilate a non-minor faction?).");
			return;
		}
		if (this.assimilatedFactions.Exists((Faction match) => match.Name == minorFaction.Name))
		{
			Diagnostics.Log("Cannot assimilate the same faction twice (name: '{0}').", new object[]
			{
				minorFaction.Name
			});
			return;
		}
		this.assimilatedFactions.Add(minorFaction);
		this.AssimilateMinorFactionEmpires(minorFaction);
		this.AssimilateMinorFactionTraits(minorFaction, true);
		this.OnAssimilatedFactionsCollectionChanged(minorFaction, CollectionChangeAction.Add);
		Diagnostics.Log("Minor faction (name: '{0}') has been assimilated by empire {1}.", new object[]
		{
			minorFaction.Name,
			base.Empire.Index
		});
	}

	private void AssimilateMinorFactionEmpires(Faction faction)
	{
		if (this.assimilatedFactionSimulationObject.Tags.Contains(faction.Affinity))
		{
			return;
		}
		if (this.assimilatedFactionSimulationObject.Parent != base.Empire.SimulationObject)
		{
			base.Empire.SimulationObject.AddChild(this.assimilatedFactionSimulationObject);
		}
		SimulationDescriptor descriptor;
		if (this.SimulationDescriptorDatabase.TryGetValue(faction.Affinity, out descriptor))
		{
			this.assimilatedFactionSimulationObject.AddDescriptor(descriptor);
		}
	}

	private void AssimilateMinorFactionTraits(MinorFaction minorFaction, bool applyFactionTraitsDescriptors = false)
	{
		if (minorFaction == null)
		{
			Diagnostics.LogError("Cannot assimilate faction because it is null (trying to assimilate a non-minor faction?).");
			return;
		}
		if (minorFaction.AssimilationTraits == null)
		{
			Diagnostics.LogWarning("Faction (name: '{0}') has no faction traits to assimilate.", new object[]
			{
				minorFaction.Name
			});
			return;
		}
		foreach (FactionTrait factionTrait in FactionTrait.EnumerableTraits(minorFaction.AssimilationTraits).ToArray<FactionTrait>())
		{
			if (factionTrait != null)
			{
				if (!this.factionTraitReferenceCount.ContainsKey(factionTrait))
				{
					this.factionTraitReferenceCount.Add(factionTrait, 0);
					if (applyFactionTraitsDescriptors && factionTrait.SimulationDescriptorReferences != null)
					{
						for (int j = 0; j < factionTrait.SimulationDescriptorReferences.Length; j++)
						{
							SimulationDescriptor descriptor;
							if (this.SimulationDescriptorDatabase.TryGetValue(factionTrait.SimulationDescriptorReferences[j], out descriptor))
							{
								base.Empire.AddDescriptor(descriptor, false);
							}
							else
							{
								Diagnostics.LogWarning("Fail to find the descriptor for descriptor reference {1} on the trait {0}.", new object[]
								{
									factionTrait.Name,
									factionTrait.SimulationDescriptorReferences[j]
								});
							}
						}
					}
				}
				else
				{
					Dictionary<FactionTrait, int> dictionary2;
					Dictionary<FactionTrait, int> dictionary = dictionary2 = this.factionTraitReferenceCount;
					FactionTrait key2;
					FactionTrait key = key2 = factionTrait;
					int num = dictionary2[key2];
					dictionary[key] = num + 1;
				}
			}
		}
	}

	private void OnAssimilatedFactionsCollectionChanged(Faction faction, CollectionChangeAction action)
	{
		if (this.AssimilatedFactionsCollectionChanged != null)
		{
			this.AssimilatedFactionsCollectionChanged(this, new CollectionChangeEventArgs(action, faction));
		}
	}

	private void VisibilityService_VisibilityRefreshed(object sender, VisibilityRefreshedEventArgs args)
	{
		if (args.Empire == base.Empire)
		{
			global::Game game = this.GameService.Game as global::Game;
			global::Empire empire = base.Empire as global::Empire;
			for (int i = 0; i < game.Empires.Length; i++)
			{
				MinorEmpire minorEmpire = game.Empires[i] as MinorEmpire;
				if (minorEmpire != null && (minorEmpire.AlreadyEncounteredEmpires & base.Empire.Bits) == 0)
				{
					for (int j = 0; j < this.encounteredMinorFaction.Count; j++)
					{
						if (this.encounteredMinorFaction[j] == minorEmpire.MinorFaction.Name)
						{
							minorEmpire.AlreadyEncounteredEmpires |= base.Empire.Bits;
							break;
						}
					}
					if ((minorEmpire.AlreadyEncounteredEmpires & base.Empire.Bits) == 0)
					{
						BarbarianCouncil agency = game.Empires[i].GetAgency<BarbarianCouncil>();
						if (agency != null)
						{
							for (int k = 0; k < agency.Villages.Count; k++)
							{
								if (this.VisibilityService.IsWorldPositionVisibleFor(agency.Villages[k].WorldPosition, empire))
								{
									minorEmpire.AlreadyEncounteredEmpires |= base.Empire.Bits;
									this.encounteredMinorFaction.Add(minorEmpire.MinorFaction.Name);
									this.EventService.Notify(new EventMinorFactionDiscovery(base.Empire, minorEmpire.MinorFaction, agency.Villages[k].WorldPosition));
									break;
								}
							}
						}
					}
				}
				NavalEmpire navalEmpire = game.Empires[i] as NavalEmpire;
				if (navalEmpire != null && (navalEmpire.AlreadyEncounteredEmpires & base.Empire.Bits) == 0)
				{
					bool flag = false;
					PirateCouncil agency2 = game.Empires[i].GetAgency<PirateCouncil>();
					if (agency2 != null)
					{
						for (int l = 0; l < agency2.Fortresses.Count; l++)
						{
							if (agency2.Fortresses[l].Occupant == null)
							{
								if (this.VisibilityService.IsWorldPositionVisibleFor(agency2.Fortresses[l].WorldPosition, empire))
								{
									flag = true;
									navalEmpire.AlreadyEncounteredEmpires |= base.Empire.Bits;
									this.EventService.Notify(new EventFomoriansEncountered(base.Empire, navalEmpire.Faction, agency2.Fortresses[l].WorldPosition));
									break;
								}
							}
						}
					}
					DepartmentOfDefense agency3 = game.Empires[i].GetAgency<DepartmentOfDefense>();
					if (agency3 != null && !flag)
					{
						for (int m = 0; m < agency3.Armies.Count; m++)
						{
							if (this.VisibilityService.IsWorldPositionVisibleFor(agency3.Armies[m].WorldPosition, empire))
							{
								navalEmpire.AlreadyEncounteredEmpires |= base.Empire.Bits;
								this.EventService.Notify(new EventFomoriansEncountered(base.Empire, navalEmpire.Faction, agency3.Armies[m].WorldPosition));
								break;
							}
						}
					}
				}
			}
		}
	}

	public ReadOnlyCollection<Fortress> OccupiedFortresses
	{
		get
		{
			if (this.readOnlyOccupiedFortresses == null)
			{
				this.readOnlyOccupiedFortresses = this.occupiedFortresses.AsReadOnly();
			}
			return this.readOnlyOccupiedFortresses;
		}
	}

	public ReadOnlyCollection<Region> OccupiedRegions
	{
		get
		{
			if (this.readOnlyOccupiedRegions == null)
			{
				this.readOnlyOccupiedRegions = this.occupiedOceanRegions.AsReadOnly();
			}
			return this.readOnlyOccupiedRegions;
		}
	}

	public IEnumerable<Region> GetOwnedNavalRegion()
	{
		for (int index = 0; index < this.occupiedOceanRegions.Count; index++)
		{
			if (this.occupiedOceanRegions[index].BelongToEmpire(base.Empire as global::Empire))
			{
				yield return this.occupiedOceanRegions[index];
			}
		}
		yield break;
	}

	public void AddFortress(Fortress fortress)
	{
		if (fortress.Empire != null && fortress.Empire != base.Empire)
		{
			Diagnostics.LogError("The department of the interior was asked to add a fortress (guid: {0}, empire: {1}) but it is still bound to another empire.", new object[]
			{
				fortress.GUID,
				fortress.Empire.Name
			});
			return;
		}
		int num = this.occupiedFortresses.BinarySearch((Fortress match) => match.GUID.CompareTo(fortress.GUID));
		if (num >= 0)
		{
			Diagnostics.LogWarning("The department of the interior was asked to add a fortress (guid: {0}) but it is already present in its list of fortresses.", new object[]
			{
				fortress.GUID
			});
			return;
		}
		this.occupiedFortresses.Insert(~num, fortress);
		this.AttachFortress(fortress);
	}

	public void UnoccupyFortresses()
	{
		for (int i = this.OccupiedFortresses.Count - 1; i >= 0; i--)
		{
			Fortress fortress = this.OccupiedFortresses[i];
			if (fortress != null && fortress.NavalEmpire != null)
			{
				this.SwapFortressOccupant(fortress, fortress.NavalEmpire, new object[0]);
			}
		}
	}

	public void SwapFortressOccupant(Fortress fortress, global::Empire newOccupyingEmpire, params object[] parameters)
	{
		this.SwapFortressOccupant(fortress, newOccupyingEmpire, false, parameters);
	}

	public void SwapFortressOccupant(Fortress fortress, global::Empire newOccupyingEmpire, bool hasBeenExchanged, params object[] parameters)
	{
		global::Empire empire = fortress.Empire;
		DepartmentOfTheInterior agency = empire.GetAgency<DepartmentOfTheInterior>();
		PirateCouncil pirateCouncil = null;
		if (agency != null)
		{
			agency.RemoveFortress(fortress, true);
		}
		DepartmentOfIndustry agency2 = empire.GetAgency<DepartmentOfIndustry>();
		if (agency2 != null)
		{
			agency2.RemoveQueueFrom<Fortress>(fortress);
		}
		fortress.ClearFortressUnits();
		fortress.Occupant = (newOccupyingEmpire as MajorEmpire);
		fortress.PointOfInterest.Empire = (newOccupyingEmpire as MajorEmpire);
		if (fortress.Facilities != null)
		{
			for (int i = 0; i < fortress.Facilities.Count; i++)
			{
				if (fortress.Facilities[i] != null)
				{
					fortress.Facilities[i].Empire = (newOccupyingEmpire as MajorEmpire);
				}
			}
		}
		IEventService service;
		if (newOccupyingEmpire != null)
		{
			DepartmentOfIndustry agency3 = newOccupyingEmpire.GetAgency<DepartmentOfIndustry>();
			if (agency3 != null)
			{
				agency3.AddQueueTo<Fortress>(fortress);
			}
			DepartmentOfTheInterior agency4 = newOccupyingEmpire.GetAgency<DepartmentOfTheInterior>();
			if (agency4 != null)
			{
				agency4.AddFortress(fortress);
				pirateCouncil = fortress.NavalEmpire.GetAgency<PirateCouncil>();
				if (pirateCouncil != null)
				{
					if (this.occupiedFortresses.Count == pirateCouncil.Fortresses.Count)
					{
						EventFortressesAllOwned eventToNotify = new EventFortressesAllOwned(base.Empire);
						service = Services.GetService<IEventService>();
						if (service != null)
						{
							service.Notify(eventToNotify);
						}
					}
					pirateCouncil.UpdateUniqueFacilitiesOwned(fortress);
				}
			}
			else if (newOccupyingEmpire is NavalEmpire)
			{
				newOccupyingEmpire.AddChild(fortress);
			}
		}
		if (fortress.IsOccupied)
		{
			if (!fortress.PointOfInterest.SimulationObject.Tags.Contains(DepartmentOfTheInterior.OccupiedCitadel))
			{
				IDatabase<SimulationDescriptor> database = Databases.GetDatabase<SimulationDescriptor>(false);
				SimulationDescriptor value = database.GetValue(DepartmentOfTheInterior.OccupiedCitadel);
				if (value != null)
				{
					fortress.PointOfInterest.AddDescriptor(value, false);
				}
			}
		}
		else if (fortress.PointOfInterest.SimulationObject.Tags.Contains(DepartmentOfTheInterior.OccupiedCitadel))
		{
			IDatabase<SimulationDescriptor> database2 = Databases.GetDatabase<SimulationDescriptor>(false);
			SimulationDescriptor value2 = database2.GetValue(DepartmentOfTheInterior.OccupiedCitadel);
			if (value2 != null)
			{
				fortress.PointOfInterest.RemoveDescriptor(value2);
			}
		}
		if (fortress.Region != null && fortress.Region.NavalEmpire != null)
		{
			pirateCouncil = fortress.Region.NavalEmpire.GetAgency<PirateCouncil>();
			List<Fortress> list = null;
			if (pirateCouncil != null)
			{
				list = pirateCouncil.GetRegionFortresses(fortress.Region);
				for (int j = 0; j < list.Count; j++)
				{
					if (!(fortress.GUID == list[j].GUID))
					{
						this.VisibilityService.SetWorldPositionAsExplored(list[j].WorldPosition, base.Empire as global::Empire, 0);
					}
				}
			}
			if (fortress.Region.BelongToEmpire(base.Empire as MajorEmpire))
			{
				if (list != null)
				{
					IDatabase<SimulationDescriptor> database3 = Databases.GetDatabase<SimulationDescriptor>(false);
					SimulationDescriptor value3 = database3.GetValue(DepartmentOfTheInterior.FortressBonusesOnRegionControlled);
					if (value3 != null)
					{
						for (int k = 0; k < list.Count; k++)
						{
							list[k].AddDescriptor(value3, false);
							list[k].Refresh(false);
						}
					}
				}
				if (base.Empire.SimulationObject.GetPropertyValue("OceanRegionControlledCount") < 1f)
				{
					base.Empire.SimulationObject.SetPropertyBaseValue("OceanRegionControlledCount", 1f);
					this.EventService.Notify(new EventFirstOceanicRegionControlled(base.Empire, fortress.Region, fortress.WorldPosition));
				}
				if (hasBeenExchanged)
				{
					this.EventService.Notify(new EventOceanControlWithTrade(base.Empire));
				}
				string a = (base.Empire as MajorEmpire).Faction.Name.ToString().ToUpper();
				if (a == "FACTIONSEADEMONS" && this.BorderingRegionsCount(fortress.Region, true, true) >= 4)
				{
					this.EventService.Notify(new EventSeaDemonsOceanicHub(base.Empire));
				}
			}
		}
		this.VisibilityService.NotifyVisibilityHasChanged(base.Empire as global::Empire);
		this.VisibilityService.NotifyVisibilityHasChanged(newOccupyingEmpire);
		service = Services.GetService<IEventService>();
		if (service != null)
		{
			EventFortressOccupantSwapped eventToNotify2 = new EventFortressOccupantSwapped(fortress.Empire, fortress, newOccupyingEmpire, empire, parameters);
			service.Notify(eventToNotify2);
		}
		fortress.Refresh(false);
		fortress.NotifyOccupantChange();
		this.VisibilityService.NotifyVisibilityHasChanged(base.Empire as global::Empire);
		this.VisibilityService.NotifyVisibilityHasChanged(newOccupyingEmpire);
		if (pirateCouncil != null)
		{
			pirateCouncil.UpdateFacilitiesMalus();
		}
	}

	public bool OwnTheUniqueFacility(StaticString facilityName)
	{
		for (int i = 0; i < this.occupiedFortresses.Count; i++)
		{
			Fortress fortress = this.occupiedFortresses[i];
			for (int j = 0; j < fortress.Facilities.Count; j++)
			{
				if (fortress.Facilities[j].SimulationObject.Tags.Contains(facilityName))
				{
					return true;
				}
			}
		}
		return false;
	}

	private void AttachFortress(Fortress fortress)
	{
		base.Empire.AddChild(fortress);
		base.Empire.Refresh(false);
		this.OnOccupiedFortressCollectionChanged(fortress, CollectionChangeAction.Add);
		for (int i = 0; i < fortress.Facilities.Count; i++)
		{
			fortress.Facilities[i].Empire = (base.Empire as global::Empire);
		}
		if (!this.occupiedOceanRegions.Contains(fortress.Region))
		{
			this.occupiedOceanRegions.Add(fortress.Region);
		}
	}

	public int BorderingRegionsCount(Region centralRegion, bool onlyLandRegions = true, bool onlyOwnedRegions = true)
	{
		int num = 0;
		List<Region> list = new List<Region>();
		Diagnostics.Assert(this.WorldPositionningService != null);
		for (int i = 0; i < centralRegion.Borders.Length; i++)
		{
			int neighbourRegionIndex = centralRegion.Borders[i].NeighbourRegionIndex;
			Region region = this.WorldPositionningService.GetRegion(neighbourRegionIndex);
			if (region != null)
			{
				if (!onlyLandRegions || region.IsLand)
				{
					if (!onlyOwnedRegions || region.Owner == base.Empire)
					{
						if (!list.Contains(region))
						{
							num++;
							list.Add(region);
						}
					}
				}
			}
		}
		return num;
	}

	private void RemoveFortress(Fortress fortress, bool swapping)
	{
		if (fortress.Empire != null && fortress.Empire != base.Empire)
		{
			Diagnostics.LogError("The department of the interior was asked to remove a fortress (guid: {0}, empire: {1}) but it is still bound to another empire.", new object[]
			{
				fortress.GUID,
				fortress.Empire.Name
			});
			return;
		}
		if (fortress.Region != null && fortress.Region.NavalEmpire != null)
		{
			PirateCouncil agency = fortress.Region.NavalEmpire.GetAgency<PirateCouncil>();
			if (agency != null)
			{
				List<Fortress> regionFortresses = agency.GetRegionFortresses(fortress.Region);
				if (fortress.Region.BelongToEmpire(base.Empire as MajorEmpire) && regionFortresses != null)
				{
					IDatabase<SimulationDescriptor> database = Databases.GetDatabase<SimulationDescriptor>(false);
					SimulationDescriptor value = database.GetValue(DepartmentOfTheInterior.FortressBonusesOnRegionControlled);
					if (value != null)
					{
						for (int i = 0; i < regionFortresses.Count; i++)
						{
							regionFortresses[i].RemoveDescriptor(value);
						}
					}
				}
			}
		}
		int num = this.OccupiedFortresses.BinarySearch((Fortress match) => match.GUID.CompareTo(fortress.GUID));
		if (num < 0)
		{
			Diagnostics.LogWarning("The department of the interior was asked to remove a fortress (guid: {0}) but it is not present in its list of fortresses.", new object[]
			{
				fortress.GUID
			});
			return;
		}
		this.occupiedFortresses.RemoveAt(num);
		base.Empire.RemoveChild(fortress);
		base.Empire.Refresh(true);
		this.OnOccupiedFortressCollectionChanged(fortress, CollectionChangeAction.Remove);
		if (!this.occupiedFortresses.Exists((Fortress match) => match.Region == fortress.Region))
		{
			this.occupiedOceanRegions.Remove(fortress.Region);
		}
	}

	private void OnOccupiedFortressCollectionChanged(Fortress fortress, CollectionChangeAction action)
	{
		if (this.OccupiedFortressesCollectionChanged != null)
		{
			this.OccupiedFortressesCollectionChanged(this, new CollectionChangeEventArgs(action, fortress));
		}
	}

	private bool AssignPopulationPreprocessor(OrderAssignPopulation order)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			return false;
		}
		if (!(gameEntity is City))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a City.");
			return false;
		}
		City city = gameEntity as City;
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < order.PopulationValues.Length; i++)
		{
			if (float.IsNaN(order.PopulationValues[i]) || float.IsInfinity(order.PopulationValues[i]))
			{
				Diagnostics.LogError("Order preprocessing failed because the {0} population value is NaN.", new object[]
				{
					i,
					order.PopulationValues[i]
				});
				return false;
			}
			if (order.PopulationValues[i] < 0f)
			{
				return false;
			}
			num2 += order.PopulationValues[i];
			num += city.GetPropertyValue(order.PopulationNames[i]);
		}
		return Math.Abs(num - num2) <= float.Epsilon;
	}

	private IEnumerator AssignPopulationProcessor(OrderAssignPopulation order)
	{
		if (order.CityGuid == GameEntityGUID.Zero)
		{
			Diagnostics.LogError("Skipping city extension process because the game entity guid is null.");
			yield break;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			yield break;
		}
		if (!(gameEntity is City))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a City.");
			yield break;
		}
		City city = gameEntity as City;
		for (int index = 0; index < order.PopulationValues.Length; index++)
		{
			Diagnostics.Assert(order.PopulationValues[index] >= 0f);
			city.SetPropertyBaseValue(order.PopulationNames[index], order.PopulationValues[index]);
		}
		city.Refresh(false);
		if (this.PopulationRepartitionChanged != null)
		{
			this.PopulationRepartitionChanged(this, new PopulationRepartitionEventArgs(city));
		}
		yield break;
	}

	private bool AssimilateFactionPreprocessor(OrderAssimilateFaction order)
	{
		Faction faction = null;
		int num = this.assimilatedFactions.Count;
		int num2 = 0;
		for (int i = 0; i < order.Instructions.Length; i++)
		{
			if (StaticString.IsNullOrEmpty(order.Instructions[i].FactionName))
			{
				Diagnostics.LogError("Order preprocessing failed because the minor faction name is null or empty.");
				return false;
			}
			if (!this.FactionDatabase.TryGetValue(order.Instructions[i].FactionName, out faction))
			{
				Diagnostics.LogError("Order preprocessing failed because the database does not contain the defined minor faction (name: '{0}').", new object[]
				{
					order.Instructions[i].FactionName
				});
				return false;
			}
			if (!(faction is MinorFaction))
			{
				Diagnostics.LogError("Order preprocessing failed because the faction is not a minor one.");
				return false;
			}
			bool flag = this.assimilatedFactions.Exists((Faction match) => match.Name == faction.Name);
			if (order.Instructions[i].AssimilationState)
			{
				if (flag)
				{
					Diagnostics.Log("Order preprocessing failed because the minor faction (name: '{0}') has already been assimilated.", new object[]
					{
						faction.Name
					});
					return false;
				}
				bool flag2 = false;
				for (int j = 0; j < this.cities.Count; j++)
				{
					if (this.cities[j].Region.MinorEmpire != null && this.cities[j].Region.MinorEmpire.Faction.Name == faction.Name)
					{
						BarbarianCouncil agency = this.cities[j].Region.MinorEmpire.GetAgency<BarbarianCouncil>();
						flag2 = agency.HasAtLeastOneVillagePacified;
						if (flag2)
						{
							break;
						}
					}
				}
				if (base.Empire is MajorEmpire)
				{
					List<Village> convertedVillages = ((MajorEmpire)base.Empire).ConvertedVillages;
					if (convertedVillages != null)
					{
						for (int k = 0; k < convertedVillages.Count; k++)
						{
							if (convertedVillages[k].MinorEmpire != null && convertedVillages[k].MinorEmpire.Faction.Name == faction.Name)
							{
								flag2 = true;
								break;
							}
						}
					}
					List<Village> infectedVillages = ((MajorEmpire)base.Empire).InfectedVillages;
					if (infectedVillages != null)
					{
						for (int l = 0; l < infectedVillages.Count; l++)
						{
							if (infectedVillages[l].MinorEmpire != null && infectedVillages[l].MinorEmpire.Faction.Name == faction.Name)
							{
								flag2 = true;
								break;
							}
						}
					}
				}
				if (!flag2)
				{
					Diagnostics.LogWarning("Order preprocessing failed because the minor faction (name: '{0}') cannot be assimilated atm.", new object[]
					{
						faction.Name
					});
					return false;
				}
				float num3 = -DepartmentOfTheInterior.GetAssimilationCost(base.Empire, -num2);
				if (!this.departmentOfTheTreasury.IsTransferOfResourcePossible(base.Empire, SimulationProperties.EmpirePoint, ref num3))
				{
					Diagnostics.LogWarning("Order preprocessing failed because the empire has not enough '{0}' (amount required: {1}).", new object[]
					{
						"empire points",
						num3
					});
					return false;
				}
				order.AssimilationCost += num3;
				num++;
			}
			else
			{
				if (!flag)
				{
					Diagnostics.Log("Order preprocessing failed because the minor faction (name: '{0}') hasn't been assililated yet.", new object[]
					{
						faction.Name
					});
					return false;
				}
				num--;
				num2++;
			}
		}
		float assimilationCost = order.AssimilationCost;
		if (!this.departmentOfTheTreasury.IsTransferOfResourcePossible(base.Empire, SimulationProperties.EmpirePoint, ref assimilationCost))
		{
			Diagnostics.LogWarning("Order preprocessing failed because the empire has not enough '{0}' (amount required: {1}).", new object[]
			{
				"empire points",
				assimilationCost
			});
			return false;
		}
		float propertyValue = base.Empire.GetPropertyValue(SimulationProperties.MinorFactionSlotCount);
		if ((float)num > propertyValue)
		{
			Diagnostics.Log("Order preprocessing failed because the empire has not enough room for assimilation.");
			return false;
		}
		return true;
	}

	private IEnumerator AssimilateFactionProcessor(OrderAssimilateFaction order)
	{
		Faction faction = null;
		for (int index = 0; index < order.Instructions.Length; index++)
		{
			if (!this.FactionDatabase.TryGetValue(order.Instructions[index].FactionName, out faction))
			{
				Diagnostics.LogError("Order processing failed because the database does not contain the defined faction.");
				yield break;
			}
			if (order.Instructions[index].AssimilationState)
			{
				this.AssimilateMinorFaction(faction as MinorFaction);
			}
			else
			{
				this.DeassimilateFaction(faction);
			}
			if (this.EventService != null)
			{
				EventFactionAssimilated eventFactionAssimilated = new EventFactionAssimilated(base.Empire, faction, order.Instructions[index].AssimilationState);
				this.EventService.Notify(eventFactionAssimilated);
			}
		}
		this.departmentOfTheTreasury.TryTransferResources(base.Empire, "EmpirePoint", order.AssimilationCost);
		yield break;
	}

	private bool BribeVillagePreprocessor(OrderBribeVillage order)
	{
		if (!order.InstigatorGUID.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the instigator guid is not valid.");
			return false;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.InstigatorGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the entity is not referenced (guid: {0:X8}).", new object[]
			{
				order.InstigatorGUID
			});
			return false;
		}
		Army army = gameEntity as Army;
		if (army == null || army.IsInEncounter)
		{
			Diagnostics.LogWarning("Order preprocessing failed because army is in encounter.");
			return false;
		}
		if (order.NumberOfActionPointsToSpend < 0f)
		{
			ArmyAction armyAction = null;
			IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
			if (database != null && database.TryGetValue(ArmyAction_Bribe.ReadOnlyName, out armyAction))
			{
				order.NumberOfActionPointsToSpend = armyAction.GetCostInActionPoints();
			}
		}
		if (order.NumberOfActionPointsToSpend > 0f)
		{
			SimulationObjectWrapper simulationObjectWrapper = gameEntity as SimulationObjectWrapper;
			if (simulationObjectWrapper != null)
			{
				float propertyValue = simulationObjectWrapper.GetPropertyValue(SimulationProperties.MaximumNumberOfActionPoints);
				float propertyValue2 = simulationObjectWrapper.GetPropertyValue(SimulationProperties.ActionPointsSpent);
				if (order.NumberOfActionPointsToSpend > propertyValue - propertyValue2)
				{
					Diagnostics.LogWarning("Not enough action points.");
					return false;
				}
			}
		}
		Region region = this.WorldPositionningService.GetRegion(order.VillageWorldPosition);
		if (region == null || region.MinorEmpire == null)
		{
			Diagnostics.LogWarning("Cannot bribe a invalid region or a region with no minor faction.");
			return false;
		}
		BarbarianCouncil agency = region.MinorEmpire.GetAgency<BarbarianCouncil>();
		if (agency == null)
		{
			Diagnostics.LogWarning("Invalid null barabrian council on the region.");
			return false;
		}
		Village villageAt = agency.GetVillageAt(order.VillageWorldPosition);
		if (villageAt == null || villageAt.HasBeenPacified)
		{
			Diagnostics.LogWarning("Invalid null village or village already pacified.");
			return false;
		}
		if (villageAt.IsInEncounter)
		{
			Diagnostics.LogWarning("Order preprocessing failed because village is in encounter.");
			return false;
		}
		float propertyValue3 = villageAt.GetPropertyValue(SimulationProperties.BribeCost);
		float num = DepartmentOfTheTreasury.ConvertCostsTo(DepartmentOfTheTreasury.Resources.EmpireMoney, "Bribe", propertyValue3, base.Empire);
		num = DepartmentOfTheTreasury.ComputeCostWithReduction(base.Empire, num, "Bribe", CostReduction.ReductionType.Buyout);
		num *= 1f - army.GetPropertyValue("BribeCostReduction");
		num *= -1f;
		if (!this.departmentOfTheTreasury.IsTransferOfResourcePossible(base.Empire, DepartmentOfTheTreasury.Resources.EmpireMoney, ref num))
		{
			return false;
		}
		order.BribeCost = num;
		return true;
	}

	private IEnumerator BribeVillageProcessor(OrderBribeVillage order)
	{
		Region region = this.WorldPositionningService.GetRegion(order.VillageWorldPosition);
		if (region == null || region.MinorEmpire == null)
		{
			yield break;
		}
		BarbarianCouncil barbarianCouncil = region.MinorEmpire.GetAgency<BarbarianCouncil>();
		if (barbarianCouncil == null)
		{
			yield break;
		}
		Village village = barbarianCouncil.GetVillageAt(order.VillageWorldPosition);
		if (village == null || village.HasBeenPacified)
		{
			yield break;
		}
		DepartmentOfDefense departmentOfDefense = base.Empire.GetAgency<DepartmentOfDefense>();
		Army army = departmentOfDefense.GetArmy(order.InstigatorGUID);
		if (army == null)
		{
			Diagnostics.LogError("Skipping bribe process because the army does not exists.");
			yield break;
		}
		IGameEntity gameEntity;
		if (order.NumberOfActionPointsToSpend > 0f && this.GameEntityRepositoryService.TryGetValue(order.InstigatorGUID, out gameEntity))
		{
			ArmyAction.SpendSomeNumberOfActionPoints(gameEntity, order.NumberOfActionPointsToSpend);
		}
		barbarianCouncil.PacifyVillage(village, new global::Empire[]
		{
			base.Empire as global::Empire
		});
		if (region.City != null)
		{
			DepartmentOfTheInterior departmentOfTheInterior = region.City.Empire.GetAgency<DepartmentOfTheInterior>();
			float previousPopulation = region.City.GetPropertyValue(SimulationProperties.Population);
			departmentOfTheInterior.BindMinorFactionToCity(region.City, region.MinorEmpire);
			departmentOfTheInterior.ForceGrowthToCurrentPopulation(region.City, previousPopulation);
			departmentOfTheInterior.VerifyOverallPopulation(region.City);
		}
		this.departmentOfTheTreasury.TryTransferResources(base.Empire, DepartmentOfTheTreasury.Resources.EmpireMoney, order.BribeCost);
		IDatabase<ArmyAction> armyActionDatabase = Databases.GetDatabase<ArmyAction>(false);
		ArmyAction armyAction = armyActionDatabase.GetValue(order.ArmyActionName);
		if (armyAction != null && army.Hero != null)
		{
			army.Hero.GainXp(armyAction.ExperienceReward, false, true);
		}
		yield break;
	}

	private bool BuyOutPopulationPreprocessor(OrderBuyOutPopulation order)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			return false;
		}
		if (!(gameEntity is City))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a City.");
			return false;
		}
		City city = gameEntity as City;
		float num = -DepartmentOfTheTreasury.GetPopulationBuyOutCost(city);
		if (!this.departmentOfTheTreasury.IsTransferOfResourcePossible(city.Empire, DepartmentOfTheTreasury.Resources.PopulationBuyout, ref num))
		{
			Diagnostics.LogWarning("Order preprocessing failed because we don't have enough resources.");
			return false;
		}
		float propertyValue = city.GetPropertyValue(SimulationProperties.Population);
		DepartmentOfTheInterior agency = city.Empire.GetAgency<DepartmentOfTheInterior>();
		float num2;
		float num3;
		agency.GetGrowthLimits(propertyValue, out num2, out num3);
		float propertyValue2 = city.GetPropertyValue(SimulationProperties.CityGrowthStock);
		float num4 = num3 - propertyValue2;
		if (!this.departmentOfTheTreasury.IsTransferOfResourcePossible(city, DepartmentOfTheTreasury.Resources.CityGrowth, ref num4))
		{
			Diagnostics.LogWarning("Order preprocessing failed because growth transfert failed.");
			return false;
		}
		return true;
	}

	private IEnumerator BuyOutPopulationProcessor(OrderBuyOutPopulation order)
	{
		if (order.CityGuid == GameEntityGUID.Zero)
		{
			Diagnostics.LogError("Skipping city extension process because the game entity guid is null.");
			yield break;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			yield break;
		}
		if (!(gameEntity is City))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a City.");
			yield break;
		}
		City city = gameEntity as City;
		float constructionCostForBuyout = -DepartmentOfTheTreasury.GetPopulationBuyOutCost(city);
		if (!this.departmentOfTheTreasury.TryTransferResources(city.Empire, DepartmentOfTheTreasury.Resources.PopulationBuyout, constructionCostForBuyout))
		{
			Diagnostics.LogError("Order preprocessing failed because we don't have enough resources.");
			yield break;
		}
		float population = city.GetPropertyValue(SimulationProperties.Population);
		DepartmentOfTheInterior departmentOfTheInterior = city.Empire.GetAgency<DepartmentOfTheInterior>();
		float growthMin;
		float growthMax;
		departmentOfTheInterior.GetGrowthLimits(population, out growthMin, out growthMax);
		float growthStock = city.GetPropertyValue(SimulationProperties.CityGrowthStock);
		float neededGrowth = growthMax - growthStock;
		if (!this.departmentOfTheTreasury.TryTransferResources(city, DepartmentOfTheTreasury.Resources.CityGrowth, neededGrowth))
		{
			Diagnostics.LogError("Order preprocessing failed because growth transfert failed.");
			yield break;
		}
		departmentOfTheInterior.ComputeCityPopulation(city, false);
		city.Empire.SetPropertyBaseValue(SimulationProperties.PopulationBuyoutCooldown, city.Empire.GetPropertyValue(SimulationProperties.MaximumPopulationBuyoutCooldown));
		if (this.PopulationRepartitionChanged != null)
		{
			this.PopulationRepartitionChanged(this, new PopulationRepartitionEventArgs(city));
		}
		yield break;
	}

	private bool ChangeDryDockWorldPositionPreprocessor(OrderChangeDryDockWorldPosition order)
	{
		if (order.GameEntityGUID == GameEntityGUID.Zero)
		{
			Diagnostics.LogError("Order preprocessing failed because the city guid is equal to GameEntityGUID.Zero.");
			return false;
		}
		if (this.Cities.First((City element) => element.GUID == order.GameEntityGUID) == null)
		{
			Diagnostics.LogError("Order preprocessing failed retrieving city.");
			return false;
		}
		if (order.WorldPosition == WorldPosition.Invalid)
		{
			Diagnostics.LogError("Order preprocessing failed invalid WorldPosition.");
			return false;
		}
		return true;
	}

	private IEnumerator ChangeDryDockWorldPositionProcessor(OrderChangeDryDockWorldPosition order)
	{
		City city = this.Cities.First((City element) => element.GUID == order.GameEntityGUID);
		if (city == null)
		{
			Diagnostics.LogError("Order preprocessing failed retrieving city.");
			yield break;
		}
		if (order.WorldPosition == WorldPosition.Invalid)
		{
			Diagnostics.LogError("Order preprocessing failed invalid WorldPosition.");
			yield break;
		}
		city.DryDockPosition = order.WorldPosition;
		city.Refresh(false);
		yield break;
	}

	private bool ChangeEntityUserDefinedNamePreprocessor(OrderChangeEntityUserDefinedName order)
	{
		if (string.IsNullOrEmpty(order.UserDefinedName))
		{
			Diagnostics.LogError("Order preprocessing failed because the user defined name is either null or empty.");
			return false;
		}
		if (!order.GameEntityGUID.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the entity guid is not valid.");
			return false;
		}
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.GameEntityGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the entity does not exists.");
			return false;
		}
		return true;
	}

	private IEnumerator ChangeEntityUserDefinedNameProcessor(OrderChangeEntityUserDefinedName order)
	{
		IGameEntity gameEntity;
		if (this.GameEntityRepositoryService.TryGetValue(order.GameEntityGUID, out gameEntity))
		{
			PropertyInfo propertyInfo = gameEntity.GetType().GetProperty("UserDefinedName");
			if (propertyInfo != null)
			{
				propertyInfo.SetValue(gameEntity, order.UserDefinedName, null);
			}
		}
		yield break;
	}

	private bool ChangeRegionUserDefinedNamePreprocessor(OrderChangeRegionUserDefinedName order)
	{
		if (string.IsNullOrEmpty(order.UserDefinedName))
		{
			Diagnostics.LogError("Order preprocessing failed because the user defined name is either null or empty.");
			return false;
		}
		try
		{
			World world = (this.GameService.Game as global::Game).World;
			if (order.RegionIndex >= 0 && order.RegionIndex < world.Regions.Length)
			{
				bool flag = world.Regions[order.RegionIndex].BelongToEmpire(base.Empire as global::Empire);
				if (flag)
				{
					return true;
				}
				Diagnostics.LogError("Order preprocessing failed because the region does not 'belong' to the empire.");
			}
			else
			{
				Diagnostics.LogError("Order preprocessing failed because the region index is out of bounds.");
			}
		}
		catch
		{
		}
		return false;
	}

	private IEnumerator ChangeRegionUserDefinedNameProcessor(OrderChangeRegionUserDefinedName order)
	{
		if (!string.IsNullOrEmpty(order.UserDefinedName))
		{
			try
			{
				Region region = (this.GameService.Game as global::Game).World.Regions[order.RegionIndex];
				region.UserDefinedName = order.UserDefinedName;
			}
			catch
			{
			}
		}
		yield break;
	}

	protected bool ColonizeProcessor_CreateCity(OrderColonize order, out City city)
	{
		city = null;
		if (order.ArmyGuid == GameEntityGUID.Zero)
		{
			Diagnostics.LogError("Skipping colonization process because the army game entity guid is null.");
			return false;
		}
		if (order.CityGUID == GameEntityGUID.Zero)
		{
			Diagnostics.LogError("Skipping colonization process because the city game entity guid is null.");
			return false;
		}
		DepartmentOfDefense agency = base.Empire.GetAgency<DepartmentOfDefense>();
		Diagnostics.Assert(agency != null);
		Army army = agency.GetArmy(order.ArmyGuid);
		if (army == null)
		{
			Diagnostics.LogError("Skipping colonization process because the army does not exists.");
			return false;
		}
		city = this.CreateCity(order.CityGUID, army.WorldPosition, order.DistrictEntityGUID, order.MilitiaEntityGUID, order.TerrainTypeName, order.BiomeTypeName, order.AnomalyTypeName, order.RiverTypeName);
		for (int i = 0; i < order.DistrictDescriptors.Length; i++)
		{
			if (!(order.DistrictDescriptors[i].GameEntityGUID == GameEntityGUID.Zero))
			{
				District district = this.CreateDistrict(order.DistrictDescriptors[i].GameEntityGUID, order.DistrictDescriptors[i].WorldPosition, order.DistrictDescriptors[i].DistrictType, order.DistrictDescriptors[i].TerrainTypeName, order.DistrictDescriptors[i].BiomeTypeName, order.DistrictDescriptors[i].AnomalyTypeName, order.DistrictDescriptors[i].RiverTypeName);
				city.AddDistrict(district);
			}
		}
		IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
		ArmyAction value = database.GetValue(order.ArmyActionName);
		DepartmentOfIndustry agency2 = base.Empire.GetAgency<DepartmentOfIndustry>();
		if (agency2 != null)
		{
			agency2.AddQueueTo<City>(city);
		}
		this.AddCity(city, true, true);
		this.GameEntityRepositoryService.Register(city);
		for (int j = 0; j < city.Districts.Count; j++)
		{
			this.GameEntityRepositoryService.Register(city.Districts[j]);
		}
		if (city.Militia != null)
		{
			this.GameEntityRepositoryService.Register(city.Militia);
		}
		this.BindMinorFactionToCity(city, city.Region.MinorEmpire);
		float propertyValue = base.Empire.GetPropertyValue(SimulationProperties.PopulationAmountContainedBySettler);
		city.SetPropertyBaseValue(SimulationProperties.Population, propertyValue);
		city.SetPropertyBaseValue(SimulationProperties.CityOwnedTurn, (float)(this.GameService.Game as global::Game).Turn);
		if (order.FreeCityImprovements != null)
		{
			for (int k = 0; k < order.FreeCityImprovements.Count; k++)
			{
				DepartmentOfIndustry.ConstructibleElement constructibleElement;
				if (agency2.ConstructibleElementDatabase.TryGetValue(order.FreeCityImprovements[k].FreeCityImprovementName, out constructibleElement))
				{
					CityImprovement cityImprovement = this.CreateCityImprovement(constructibleElement, order.FreeCityImprovements[k].GameEntityGUID);
					if (cityImprovement != null)
					{
						this.AddCityImprovement(city, cityImprovement);
						if (constructibleElement.Name == "CityImprovementRoads")
						{
							city.CadastralMap.ConnectedMovementCapacity |= PathfindingMovementCapacity.Ground;
						}
					}
				}
			}
		}
		city.Refresh(true);
		this.ForceGrowthToCurrentPopulation(city, 0f);
		this.UpdatePointOfInterestImprovement(city);
		DepartmentOfHealth agency3 = base.Empire.GetAgency<DepartmentOfHealth>();
		if (agency3 != null)
		{
			agency3.RefreshApprovalStatus();
		}
		IGameEntity gameEntity;
		if (this.GameEntityRepositoryService.TryGetValue(order.SettlerGUID, out gameEntity))
		{
			Unit unit = gameEntity as Unit;
			if (unit != null)
			{
				army.RemoveUnit(unit);
				this.GameEntityRepositoryService.Unregister(unit);
				unit.Dispose();
			}
			else
			{
				Diagnostics.LogError("Wasn't able to cast the settler into a unit.");
			}
		}
		else
		{
			Diagnostics.LogError("Wasn't able to find the settler (gameEntity#{0}) in GameEntityRepositoryService.", new object[]
			{
				order.SettlerGUID
			});
		}
		if (army.Hero != null)
		{
			army.Hero.GainXp(value.ExperienceReward, false, true);
		}
		army.Refresh(true);
		ICursorTargetService service = Services.GetService<ICursorTargetService>();
		if (service != null)
		{
			List<Amplitude.Unity.View.CursorTarget> selectedCursorTargets = service.SelectedCursorTargets;
			for (int l = 0; l < selectedCursorTargets.Count; l++)
			{
				ArmyWorldCursorTarget armyWorldCursorTarget = selectedCursorTargets[l] as ArmyWorldCursorTarget;
				if (armyWorldCursorTarget != null && armyWorldCursorTarget.WorldArmy.Army.GUID == army.GUID)
				{
					ICursorService service2 = Services.GetService<ICursorService>();
					if (service2 != null)
					{
						service2.ChangeCursor(typeof(DistrictWorldCursor), new object[]
						{
							city
						});
					}
					break;
				}
			}
		}
		if (army.IsEmpty)
		{
			agency.RemoveArmy(army, true);
		}
		SimulationDescriptor descriptor;
		if (base.Empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitMimics2) && !city.SimulationObject.Tags.Contains(City.MimicsCity) && this.SimulationDescriptorDatabase.TryGetValue(City.MimicsCity, out descriptor))
		{
			city.AddDescriptor(descriptor, false);
		}
		this.AddDistrictDescriptorExploitableResource(city);
		return true;
	}

	protected bool ColonizePreprocessor_ValidateOrder(OrderColonize order, StaticString abilityForColonization, out Army army)
	{
		army = null;
		if (!order.ArmyGuid.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the army guid is not valid.");
			return false;
		}
		DepartmentOfDefense agency = base.Empire.GetAgency<DepartmentOfDefense>();
		Diagnostics.Assert(agency != null);
		army = agency.GetArmy(order.ArmyGuid);
		if (army == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the army does not exists.");
			return false;
		}
		if (order.ArmyActionName == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the army action name is null.");
			return false;
		}
		if (army.IsLocked)
		{
			Diagnostics.LogError("Order preprocessing failed because the army is locked.");
			return false;
		}
		if (army.IsInEncounter)
		{
			Diagnostics.LogWarning("Order preprocessing failed because the army is in an encounter.");
			return false;
		}
		IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
		ArmyAction value = database.GetValue(order.ArmyActionName);
		if (value == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the army action '{0}' is invalid.", new object[]
			{
				order.ArmyActionName
			});
			return false;
		}
		if (!this.CanColonizeRegion(army.WorldPosition, value, false))
		{
			return false;
		}
		order.SettlerGUID = GameEntityGUID.Zero;
		foreach (Unit unit in army.Units)
		{
			if (unit.CheckUnitAbility(abilityForColonization, -1))
			{
				order.SettlerGUID = unit.GUID;
				break;
			}
		}
		if (!order.SettlerGUID.IsValid)
		{
			Diagnostics.LogWarning("Order preprocessing failed because the army does not contain any settler (anymore?).");
			return false;
		}
		if (this.WorldPositionningService != null && !this.WorldPositionningService.IsConstructible(army.WorldPosition, WorldPositionning.PreventsDistrictTypeCenterConstruction, 0))
		{
			return false;
		}
		int regionIndex = (int)this.WorldPositionningService.GetRegionIndex(army.WorldPosition);
		order.CityGUID = this.GameEntityRepositoryService.GenerateGUID();
		order.DistrictEntityGUID = this.GameEntityRepositoryService.GenerateGUID();
		order.MilitiaEntityGUID = this.GameEntityRepositoryService.GenerateGUID();
		byte terrainType = this.WorldPositionningService.GetTerrainType(army.WorldPosition);
		order.TerrainTypeName = this.WorldPositionningService.GetTerrainTypeMappingName(terrainType);
		byte biomeType = this.WorldPositionningService.GetBiomeType(army.WorldPosition);
		order.BiomeTypeName = this.WorldPositionningService.GetBiomeTypeMappingName(biomeType);
		byte anomalyType = this.WorldPositionningService.GetAnomalyType(army.WorldPosition);
		order.AnomalyTypeName = this.WorldPositionningService.GetAnomalyTypeMappingName(anomalyType);
		short riverId = this.WorldPositionningService.GetRiverId(army.WorldPosition);
		order.RiverTypeName = this.WorldPositionningService.GetRiverTypeMappingName(riverId);
		int num = 6;
		order.DistrictDescriptors = new OrderCreateCity.DistrictDescriptor[num];
		for (int i = 0; i < num; i++)
		{
			WorldPosition neighbourTile = this.WorldPositionningService.GetNeighbourTile(army.WorldPosition, (WorldOrientation)i, 1);
			if (!neighbourTile.IsValid)
			{
				order.DistrictDescriptors[i].GameEntityGUID = GameEntityGUID.Zero;
			}
			else
			{
				int regionIndex2 = (int)this.WorldPositionningService.GetRegionIndex(neighbourTile);
				if (regionIndex != regionIndex2)
				{
					order.DistrictDescriptors[i].GameEntityGUID = GameEntityGUID.Zero;
				}
				else if (!this.WorldPositionningService.IsExploitable(neighbourTile, 0))
				{
					order.DistrictDescriptors[i].GameEntityGUID = GameEntityGUID.Zero;
				}
				else
				{
					order.DistrictDescriptors[i].GameEntityGUID = this.GameEntityRepositoryService.GenerateGUID();
					order.DistrictDescriptors[i].WorldPosition = neighbourTile;
					order.DistrictDescriptors[i].DistrictType = DistrictType.Exploitation;
					terrainType = this.WorldPositionningService.GetTerrainType(neighbourTile);
					order.DistrictDescriptors[i].TerrainTypeName = this.WorldPositionningService.GetTerrainTypeMappingName(terrainType);
					anomalyType = this.WorldPositionningService.GetAnomalyType(neighbourTile);
					order.DistrictDescriptors[i].AnomalyTypeName = this.WorldPositionningService.GetAnomalyTypeMappingName(anomalyType);
					biomeType = this.WorldPositionningService.GetBiomeType(neighbourTile);
					order.DistrictDescriptors[i].BiomeTypeName = this.WorldPositionningService.GetBiomeTypeMappingName(biomeType);
					riverId = this.WorldPositionningService.GetRiverId(neighbourTile);
					order.DistrictDescriptors[i].RiverTypeName = this.WorldPositionningService.GetRiverTypeMappingName(riverId);
				}
			}
		}
		order.FreeCityImprovements = new List<OrderCreateCity.FreeCityImprovement>();
		DepartmentOfIndustry agency2 = base.Empire.GetAgency<DepartmentOfIndustry>();
		IEnumerable<DepartmentOfIndustry.ConstructibleElement> availableConstructibleElementsAsEnumerable = agency2.ConstructibleElementDatabase.GetAvailableConstructibleElementsAsEnumerable(new StaticString[0]);
		List<StaticString> list = new List<StaticString>();
		foreach (DepartmentOfIndustry.ConstructibleElement constructibleElement in availableConstructibleElementsAsEnumerable)
		{
			if (constructibleElement.Category == CityImprovementDefinition.ReadOnlyCategory && (constructibleElement.Costs == null || constructibleElement.Costs.Length == 0))
			{
				list.Clear();
				DepartmentOfTheTreasury.CheckConstructiblePrerequisites(base.Empire, constructibleElement, ref list, new string[]
				{
					ConstructionFlags.Prerequisite
				});
				if (!list.Contains(ConstructionFlags.Discard))
				{
					if (constructibleElement.Tags.Contains(City.TagMainCity))
					{
						if (this.MainCity != null)
						{
							continue;
						}
						if (this.MainCityGUID != GameEntityGUID.Zero)
						{
							continue;
						}
					}
					OrderCreateCity.FreeCityImprovement item = default(OrderCreateCity.FreeCityImprovement);
					item.GameEntityGUID = this.GameEntityRepositoryService.GenerateGUID();
					item.FreeCityImprovementName = constructibleElement.Name;
					order.FreeCityImprovements.Add(item);
				}
			}
		}
		return true;
	}

	private bool ColonizePreprocessor(OrderColonize order)
	{
		Army army;
		if (!this.ColonizePreprocessor_ValidateOrder(order, UnitAbility.ReadonlyColonize, out army))
		{
			return false;
		}
		OrderUpdateCadastralMap orderUpdateCadastralMap = new OrderUpdateCadastralMap(order.EmpireIndex);
		orderUpdateCadastralMap.CityGameEntityGUID = order.CityGUID;
		orderUpdateCadastralMap.Operation = CadastralMapOperation.Proxy;
		orderUpdateCadastralMap.PathfindingMovementCapacity = PathfindingMovementCapacity.Ground;
		orderUpdateCadastralMap.WorldPosition = army.WorldPosition;
		((global::Empire)base.Empire).PlayerControllers.Server.PostOrder(orderUpdateCadastralMap);
		OrderUpdateMilitia order2 = new OrderUpdateMilitia(order.EmpireIndex, order.CityGUID);
		((global::Empire)base.Empire).PlayerControllers.Server.PostOrder(order2);
		return true;
	}

	private IEnumerator ColonizeProcessor(OrderColonize order)
	{
		City city;
		if (this.ColonizeProcessor_CreateCity(order, out city))
		{
			this.EventService.Notify(new EventColonize(base.Empire, city));
		}
		yield break;
	}

	private bool ConvertVillagePreprocessor(OrderConvertVillage order)
	{
		if (!this.MainCityGUID.IsValid)
		{
			Diagnostics.LogWarning("Order preprocessing failed because the main city is null.");
			return false;
		}
		if (!order.InstigatorGUID.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the instigator guid is not valid.");
			return false;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.InstigatorGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the entity is not referenced (guid: {0:X8}).", new object[]
			{
				order.InstigatorGUID
			});
			return false;
		}
		Army army = gameEntity as Army;
		if (army == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the entity is not an army (guid: {0:X8}).", new object[]
			{
				order.InstigatorGUID
			});
			return false;
		}
		ArmyAction armyAction = null;
		IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
		if (database == null || !database.TryGetValue(ArmyAction_Convert.ReadOnlyName, out armyAction))
		{
			return false;
		}
		if (!army.Units.Any((Unit unit) => !unit.SimulationObject.Tags.Contains("UnitFactionTypeMinorFaction") && !unit.SimulationObject.Tags.Contains(TradableUnit.ReadOnlyMercenary)))
		{
			Diagnostics.LogError("Order preprocessing failed because the army is only made of minor units (guid: {0:X8}).", new object[]
			{
				order.InstigatorGUID
			});
			return false;
		}
		if (order.NumberOfActionPointsToSpend < 0f)
		{
			order.NumberOfActionPointsToSpend = armyAction.GetCostInActionPoints();
		}
		if (order.NumberOfActionPointsToSpend > 0f)
		{
			SimulationObjectWrapper simulationObjectWrapper = gameEntity as SimulationObjectWrapper;
			if (simulationObjectWrapper != null)
			{
				float propertyValue = simulationObjectWrapper.GetPropertyValue(SimulationProperties.MaximumNumberOfActionPoints);
				float propertyValue2 = simulationObjectWrapper.GetPropertyValue(SimulationProperties.ActionPointsSpent);
				if (order.NumberOfActionPointsToSpend > propertyValue - propertyValue2)
				{
					Diagnostics.LogWarning("Not enough action points.");
					return false;
				}
			}
		}
		Region region = this.WorldPositionningService.GetRegion(order.VillageWorldPosition);
		if (region == null || region.MinorEmpire == null)
		{
			Diagnostics.LogWarning("Cannot bribe a invalid region or a region with no minor faction.");
			return false;
		}
		BarbarianCouncil agency = region.MinorEmpire.GetAgency<BarbarianCouncil>();
		if (agency == null)
		{
			Diagnostics.LogWarning("Invalid null barabrian council on the region.");
			return false;
		}
		Village villageAt = agency.GetVillageAt(order.VillageWorldPosition);
		if (villageAt == null)
		{
			Diagnostics.LogWarning("Invalid null village.");
			return false;
		}
		if (!villageAt.HasBeenPacified)
		{
			Diagnostics.LogWarning("Village hasn't been pacified yet.");
			return false;
		}
		if (villageAt.HasBeenConverted)
		{
			Diagnostics.LogWarning("Village has been converted already.");
			return false;
		}
		if (villageAt.PointOfInterest.SimulationObject.Tags.Contains(DepartmentOfDefense.PillageStatusDescriptor))
		{
			return false;
		}
		order.ConvertionCost = ((ArmyAction_Convert)armyAction).GetConvertionCost(army, villageAt);
		for (int i = 0; i < order.ConvertionCost.Length; i++)
		{
			float num = -order.ConvertionCost[i].GetValue(base.Empire);
			if (!this.departmentOfTheTreasury.IsTransferOfResourcePossible(base.Empire, order.ConvertionCost[i].ResourceName, ref num))
			{
				return false;
			}
		}
		int num2 = (int)army.Empire.GetPropertyValue(SimulationProperties.UnitSpawnCountOnConvert);
		if (num2 > 0)
		{
			OrderSpawnConvertedVillageUnit order2 = new OrderSpawnConvertedVillageUnit(army.Empire.Index, num2, villageAt.GUID);
			army.Empire.PlayerControllers.Server.PostOrder(order2);
		}
		return true;
	}

	private IEnumerator ConvertVillageProcessor(OrderConvertVillage order)
	{
		if (!order.InstigatorGUID.IsValid)
		{
			yield break;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.InstigatorGUID, out gameEntity))
		{
			yield break;
		}
		Army army = gameEntity as Army;
		Diagnostics.Assert(army != null);
		Region region = this.WorldPositionningService.GetRegion(order.VillageWorldPosition);
		if (region == null || region.MinorEmpire == null)
		{
			yield break;
		}
		BarbarianCouncil barbarianCouncil = region.MinorEmpire.GetAgency<BarbarianCouncil>();
		if (barbarianCouncil == null)
		{
			yield break;
		}
		Village village = barbarianCouncil.GetVillageAt(order.VillageWorldPosition);
		if (village == null)
		{
			yield break;
		}
		if (!village.HasBeenPacified)
		{
			yield break;
		}
		if (village.HasBeenConverted)
		{
			yield break;
		}
		Diagnostics.Assert(village.PointOfInterest != null);
		if (village.PointOfInterest != null && village.PointOfInterest.PointOfInterestImprovement == null)
		{
			this.RebuildVillage(village);
		}
		if (order.NumberOfActionPointsToSpend > 0f)
		{
			ArmyAction.SpendSomeNumberOfActionPoints(gameEntity, order.NumberOfActionPointsToSpend);
		}
		if (order.ConvertionCost != null)
		{
			for (int index = 0; index < order.ConvertionCost.Length; index++)
			{
				if (order.ConvertionCost[index].Instant)
				{
					float resourceCost = order.ConvertionCost[index].GetValue(base.Empire);
					if (!this.departmentOfTheTreasury.TryTransferResources(base.Empire, order.ConvertionCost[index].ResourceName, -resourceCost))
					{
						Diagnostics.LogError("Cannot transfert the amount of resources (resource name = '{0}', cost = {0}).", new object[]
						{
							order.ConvertionCost[index].ResourceName,
							-resourceCost
						});
					}
				}
			}
		}
		global::Empire lastConverter = village.Converter;
		barbarianCouncil.ConvertVillage(village, (MajorEmpire)base.Empire);
		if (village.Region.City != null && village.Region.City.Empire.Index != base.Empire.Index)
		{
			Diagnostics.Assert(village.Region.City.Empire is MajorEmpire);
			DepartmentOfTheInterior departmentOfTheInterior = village.Region.City.Empire.GetAgency<DepartmentOfTheInterior>();
			Diagnostics.Assert(departmentOfTheInterior != null);
			departmentOfTheInterior.BindMinorFactionToCity(village.Region.City, village.Region.MinorEmpire);
			this.VerifyOverallPopulation(village.Region.City);
			if (this.EventService != null)
			{
				EventVillageConverted eventVillageConverted = new EventVillageConverted(village.Region.City.Empire, village);
				this.EventService.Notify(eventVillageConverted);
			}
		}
		if (lastConverter != null && lastConverter != base.Empire)
		{
			DepartmentOfTheInterior departmentOfTheInterior2 = lastConverter.GetAgency<DepartmentOfTheInterior>();
			Diagnostics.Assert(departmentOfTheInterior2 != null);
			if (departmentOfTheInterior2.MainCity != null)
			{
				departmentOfTheInterior2.BindMinorFactionToCity(departmentOfTheInterior2.MainCity, village.Region.MinorEmpire);
				this.VerifyOverallPopulation(village.Region.City);
			}
		}
		if (this.MainCity != null)
		{
			this.BindMinorFactionToCity(this.MainCity, village.MinorEmpire);
			this.VerifyOverallPopulation(this.MainCity);
			if (village.MinorEmpire.Region.Index != this.MainCity.Region.Index && village.MinorEmpire.Region.City != null)
			{
				this.VerifyOverallPopulation(village.MinorEmpire.Region.City);
			}
		}
		if (army.Hero != null)
		{
			ArmyAction armyAction = null;
			IDatabase<ArmyAction> armyActionDatabase = Databases.GetDatabase<ArmyAction>(false);
			if (armyActionDatabase != null && armyActionDatabase.TryGetValue(ArmyAction_Convert.ReadOnlyName, out armyAction))
			{
				army.Hero.GainXp(armyAction.ExperienceReward, false, true);
			}
		}
		army.Refresh(false);
		this.VisibilityService.NotifyVisibilityHasChanged((global::Empire)base.Empire);
		if (village.Region.City != null && village.Region.City.Empire.Index != base.Empire.Index)
		{
			this.VisibilityService.NotifyVisibilityHasChanged(village.Region.City.Empire);
		}
		if (lastConverter != null)
		{
			this.VisibilityService.NotifyVisibilityHasChanged(lastConverter);
		}
		village.PointOfInterest.LineOfSightDirty = true;
		if (village.PointOfInterest.ArmyPillaging.IsValid)
		{
			DepartmentOfDefense.StopPillage(village.PointOfInterest);
		}
		yield break;
	}

	private bool CreateCampPreprocessor(OrderCreateCamp order)
	{
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		if (service != null && !service.IsShared(DownloadableContent19.ReadOnlyName))
		{
			Diagnostics.LogWarning("Skipping camp creation process because couldn't find content of DLC 19.");
			return false;
		}
		Region region = this.WorldPositionningService.GetRegion(order.WorldPosition);
		if (region.Owner == null || region.Owner.Index != order.EmpireIndex)
		{
			Diagnostics.LogError("Skipping camp creation process because the region is not owned.");
			return false;
		}
		if (region.City == null)
		{
			Diagnostics.LogError("Skipping camp creation process because the region does not have a city.");
			return false;
		}
		if (region.City.Camp != null)
		{
			Diagnostics.LogError("Skipping camp creation process because the city already have a camp.");
			return false;
		}
		int bits = 1 << order.EmpireIndex;
		if (!this.WorldPositionningService.IsConstructible(order.WorldPosition, WorldPositionning.PreventsDistrictTypeExtensionConstruction, bits))
		{
			Diagnostics.LogError("Skipping camp creation process because world position is not constructible.");
			return false;
		}
		PointOfInterest pointOfInterest = this.WorldPositionningService.GetPointOfInterest(order.WorldPosition);
		if (pointOfInterest != null && pointOfInterest.Type != "ResourceDeposit" && pointOfInterest.Type != "WatchTower")
		{
			Diagnostics.LogError("Skipping camp creation process because world position is contains a POI and it not ResourceDeposit or WatchTower.");
			return false;
		}
		for (int i = 0; i < region.City.Districts.Count; i++)
		{
			if (region.City.Districts[i].WorldPosition == order.WorldPosition && region.City.Districts[i].Type != DistrictType.Exploitation)
			{
				Diagnostics.LogError("Skipping camp creation process because world position is not constructible.");
				return false;
			}
		}
		ConstructionQueue constructionQueue = this.departmentOfIndustry.GetConstructionQueue(region.City);
		for (int j = 0; j < constructionQueue.PendingConstructions.Count; j++)
		{
			if (constructionQueue.PendingConstructions[j].WorldPosition == order.WorldPosition)
			{
				Diagnostics.LogError("Skipping camp creation process because world position is not constructible.");
				return false;
			}
		}
		WorldCircle worldCircle = new WorldCircle(order.WorldPosition, 1);
		WorldPosition[] worldPositions = worldCircle.GetWorldPositions(DepartmentOfTheInterior.Game.World.WorldParameters);
		List<OrderCreateCamp.DistrictData> list = new List<OrderCreateCamp.DistrictData>();
		foreach (WorldPosition worldPosition in worldPositions)
		{
			if (worldPosition.IsValid)
			{
				if ((int)this.WorldPositionningService.GetRegionIndex(worldPosition) == region.Index)
				{
					if (this.WorldPositionningService.IsExploitable(worldPosition, 0))
					{
						bool flag = true;
						for (int l = 0; l < region.City.Districts.Count; l++)
						{
							if (worldPosition != order.WorldPosition && worldPosition == region.City.Districts[l].WorldPosition)
							{
								flag = false;
								break;
							}
						}
						for (int m = 0; m < constructionQueue.PendingConstructions.Count; m++)
						{
							if (constructionQueue.PendingConstructions[m].WorldPosition == worldPosition)
							{
								flag = false;
								break;
							}
						}
						if (flag)
						{
							list.Add(new OrderCreateCamp.DistrictData
							{
								GameEntityGUID = this.GameEntityRepositoryService.GenerateGUID(),
								WorldPosition = worldPosition,
								DistrictType = ((!(worldPosition == order.WorldPosition)) ? DistrictType.Exploitation : DistrictType.Camp)
							});
						}
					}
				}
			}
		}
		order.DistrictsData = list.ToArray();
		order.CampGUID = this.GameEntityRepositoryService.GenerateGUID();
		order.CityGUID = region.City.GUID;
		if (order.UpdateCadastralMap)
		{
			OrderUpdateCadastralMap orderUpdateCadastralMap = new OrderUpdateCadastralMap(order.EmpireIndex);
			orderUpdateCadastralMap.CityGameEntityGUID = region.City.GUID;
			orderUpdateCadastralMap.Operation = CadastralMapOperation.Proxy;
			orderUpdateCadastralMap.PathfindingMovementCapacity = PathfindingMovementCapacity.Ground;
			orderUpdateCadastralMap.WorldPosition = order.WorldPosition;
			((global::Empire)base.Empire).PlayerControllers.Server.PostOrder(orderUpdateCadastralMap);
		}
		return true;
	}

	private IEnumerator CreateCampProcessor(OrderCreateCamp order)
	{
		City city = this.GetCity(order.CityGUID);
		for (int index = 0; index < city.Districts.Count; index++)
		{
			District cityDistrict = city.Districts[index];
			if (cityDistrict.WorldPosition == order.WorldPosition && cityDistrict.Type == DistrictType.Exploitation)
			{
				this.GameEntityRepositoryService.Unregister(cityDistrict);
				city.RemoveDistrict(cityDistrict);
				cityDistrict.Dispose();
				break;
			}
		}
		Camp camp = this.CreateCamp(order.CampGUID, city.GUID, city.Empire, order.WorldPosition);
		for (int index2 = 0; index2 < order.DistrictsData.Length; index2++)
		{
			GameEntityGUID districtGUID = order.DistrictsData[index2].GameEntityGUID;
			WorldPosition districtPosition = order.DistrictsData[index2].WorldPosition;
			DistrictType districtType = order.DistrictsData[index2].DistrictType;
			byte terrainType = this.WorldPositionningService.GetTerrainType(districtPosition);
			StaticString terrainTypeName = this.WorldPositionningService.GetTerrainTypeMappingName(terrainType);
			byte biomeType = this.WorldPositionningService.GetBiomeType(districtPosition);
			StaticString biomeTypeName = this.WorldPositionningService.GetBiomeTypeMappingName(biomeType);
			byte anomalyType = this.WorldPositionningService.GetAnomalyType(districtPosition);
			StaticString anomalyTypeName = this.WorldPositionningService.GetAnomalyTypeMappingName(anomalyType);
			short riverId = this.WorldPositionningService.GetRiverId(districtPosition);
			StaticString riverTypeName = this.WorldPositionningService.GetRiverTypeMappingName(riverId);
			District district = this.CreateDistrict(districtGUID, districtPosition, districtType, terrainTypeName, biomeTypeName, anomalyTypeName, riverTypeName);
			district.City = city;
			city.AddChild(district);
			camp.AddDistrict(district);
			district.Refresh(false);
		}
		camp.Refresh(false);
		city.Camp = camp;
		this.UpdatePointOfInterestImprovement(city.Camp);
		this.GameEntityRepositoryService.Register(camp);
		for (int campDistrictIndex = 0; campDistrictIndex < camp.Districts.Count; campDistrictIndex++)
		{
			this.GameEntityRepositoryService.Register(camp.Districts[campDistrictIndex]);
		}
		this.VerifyOverallPopulation(city);
		yield break;
	}

	private bool CreateCityPreprocessor(OrderCreateCity order)
	{
		Region region = this.WorldPositionningService.GetRegion(order.WorldPosition);
		if (region.IsRegionColonized())
		{
			Diagnostics.LogError("Skipping city creation process because there is already a city in this region.");
			return false;
		}
		int bits = 1 << order.EmpireIndex;
		if (!this.WorldPositionningService.IsConstructible(order.WorldPosition, WorldPositionning.PreventsDistrictTypeCenterConstruction, bits))
		{
			Diagnostics.LogError("Skipping city creation process because world position is not constructible.");
			return false;
		}
		order.GameEntityGUID = this.GameEntityRepositoryService.GenerateGUID();
		order.DistrictEntityGUID = this.GameEntityRepositoryService.GenerateGUID();
		order.MilitiaEntityGUID = this.GameEntityRepositoryService.GenerateGUID();
		byte terrainType = this.WorldPositionningService.GetTerrainType(order.WorldPosition);
		order.TerrainTypeName = this.WorldPositionningService.GetTerrainTypeMappingName(terrainType);
		byte biomeType = this.WorldPositionningService.GetBiomeType(order.WorldPosition);
		order.BiomeTypeName = this.WorldPositionningService.GetBiomeTypeMappingName(biomeType);
		byte anomalyType = this.WorldPositionningService.GetAnomalyType(order.WorldPosition);
		order.AnomalyTypeName = this.WorldPositionningService.GetAnomalyTypeMappingName(anomalyType);
		short riverId = this.WorldPositionningService.GetRiverId(order.WorldPosition);
		order.RiverTypeName = this.WorldPositionningService.GetRiverTypeMappingName(riverId);
		int num = 6;
		order.DistrictDescriptors = new OrderCreateCity.DistrictDescriptor[num];
		for (int i = 0; i < num; i++)
		{
			WorldPosition neighbourTile = this.WorldPositionningService.GetNeighbourTile(order.WorldPosition, (WorldOrientation)i, 1);
			if (!neighbourTile.IsValid)
			{
				order.DistrictDescriptors[i].GameEntityGUID = GameEntityGUID.Zero;
			}
			else
			{
				int regionIndex = (int)this.WorldPositionningService.GetRegionIndex(neighbourTile);
				if (region.Index != regionIndex)
				{
					order.DistrictDescriptors[i].GameEntityGUID = GameEntityGUID.Zero;
				}
				else if (!this.WorldPositionningService.IsExploitable(neighbourTile, 0))
				{
					order.DistrictDescriptors[i].GameEntityGUID = GameEntityGUID.Zero;
				}
				else
				{
					order.DistrictDescriptors[i].GameEntityGUID = this.GameEntityRepositoryService.GenerateGUID();
					order.DistrictDescriptors[i].WorldPosition = neighbourTile;
					order.DistrictDescriptors[i].DistrictType = DistrictType.Exploitation;
					terrainType = this.WorldPositionningService.GetTerrainType(neighbourTile);
					order.DistrictDescriptors[i].TerrainTypeName = this.WorldPositionningService.GetTerrainTypeMappingName(terrainType);
					anomalyType = this.WorldPositionningService.GetAnomalyType(neighbourTile);
					order.DistrictDescriptors[i].AnomalyTypeName = this.WorldPositionningService.GetAnomalyTypeMappingName(anomalyType);
					biomeType = this.WorldPositionningService.GetBiomeType(neighbourTile);
					order.DistrictDescriptors[i].BiomeTypeName = this.WorldPositionningService.GetBiomeTypeMappingName(biomeType);
					riverId = this.WorldPositionningService.GetRiverId(neighbourTile);
					order.DistrictDescriptors[i].RiverTypeName = this.WorldPositionningService.GetRiverTypeMappingName(riverId);
				}
			}
		}
		List<OrderCreateCity.FreeCityImprovement> list = new List<OrderCreateCity.FreeCityImprovement>();
		DepartmentOfIndustry agency = base.Empire.GetAgency<DepartmentOfIndustry>();
		IEnumerable<DepartmentOfIndustry.ConstructibleElement> availableConstructibleElementsAsEnumerable = agency.ConstructibleElementDatabase.GetAvailableConstructibleElementsAsEnumerable(new StaticString[0]);
		List<StaticString> list2 = new List<StaticString>();
		foreach (DepartmentOfIndustry.ConstructibleElement constructibleElement in availableConstructibleElementsAsEnumerable)
		{
			if (constructibleElement.Category == CityImprovementDefinition.ReadOnlyCategory && constructibleElement.Tags.Contains(ConstructibleElement.TagFree))
			{
				list2.Clear();
				DepartmentOfTheTreasury.CheckConstructiblePrerequisites(base.Empire, constructibleElement, ref list2, new string[]
				{
					ConstructionFlags.Prerequisite
				});
				if (!list2.Contains(ConstructionFlags.Discard))
				{
					list.Add(new OrderCreateCity.FreeCityImprovement
					{
						GameEntityGUID = this.GameEntityRepositoryService.GenerateGUID(),
						FreeCityImprovementName = constructibleElement.Name
					});
				}
			}
		}
		order.FreeCityImprovements = list.ToArray();
		OrderUpdateCadastralMap orderUpdateCadastralMap = new OrderUpdateCadastralMap(order.EmpireIndex);
		orderUpdateCadastralMap.CityGameEntityGUID = order.GameEntityGUID;
		orderUpdateCadastralMap.Operation = CadastralMapOperation.Proxy;
		orderUpdateCadastralMap.PathfindingMovementCapacity = PathfindingMovementCapacity.Ground;
		orderUpdateCadastralMap.WorldPosition = order.WorldPosition;
		((global::Empire)base.Empire).PlayerControllers.Server.PostOrder(orderUpdateCadastralMap);
		return true;
	}

	private IEnumerator CreateCityProcessor(OrderCreateCity order)
	{
		if (order.GameEntityGUID == GameEntityGUID.Zero)
		{
			Diagnostics.LogError("Skipping city creation process because the game entity guid is null.");
			yield break;
		}
		City city = this.CreateCity(order.GameEntityGUID, order.WorldPosition, order.DistrictEntityGUID, order.MilitiaEntityGUID, order.TerrainTypeName, order.BiomeTypeName, order.AnomalyTypeName, order.RiverTypeName);
		for (int index = 0; index < order.DistrictDescriptors.Length; index++)
		{
			if (!(order.DistrictDescriptors[index].GameEntityGUID == GameEntityGUID.Zero))
			{
				District tile = this.CreateDistrict(order.DistrictDescriptors[index].GameEntityGUID, order.DistrictDescriptors[index].WorldPosition, order.DistrictDescriptors[index].DistrictType, order.DistrictDescriptors[index].TerrainTypeName, order.DistrictDescriptors[index].BiomeTypeName, order.DistrictDescriptors[index].AnomalyTypeName, order.DistrictDescriptors[index].RiverTypeName);
				city.AddDistrict(tile);
			}
		}
		DepartmentOfIndustry departmentOfIndustry = base.Empire.GetAgency<DepartmentOfIndustry>();
		if (departmentOfIndustry != null)
		{
			departmentOfIndustry.AddQueueTo<City>(city);
		}
		this.AddCity(city, true, true);
		this.GameEntityRepositoryService.Register(city);
		for (int tileIndex = 0; tileIndex < city.Districts.Count; tileIndex++)
		{
			this.GameEntityRepositoryService.Register(city.Districts[tileIndex]);
		}
		if (city.Militia != null)
		{
			this.GameEntityRepositoryService.Register(city.Militia);
		}
		this.BindMinorFactionToCity(city, city.Region.MinorEmpire);
		if (order.FreeCityImprovements != null)
		{
			for (int index2 = 0; index2 < order.FreeCityImprovements.Length; index2++)
			{
				DepartmentOfIndustry.ConstructibleElement constructibleElement;
				if (departmentOfIndustry.ConstructibleElementDatabase.TryGetValue(order.FreeCityImprovements[index2].FreeCityImprovementName, out constructibleElement))
				{
					CityImprovement cityImprovement = this.CreateCityImprovement(constructibleElement, order.FreeCityImprovements[index2].GameEntityGUID);
					if (cityImprovement != null)
					{
						this.AddCityImprovement(city, cityImprovement);
					}
				}
			}
		}
		float wantedPopulation = base.Empire.GetPropertyValue(SimulationProperties.PopulationAmountContainedBySettler);
		city.SetPropertyBaseValue(SimulationProperties.Population, wantedPopulation);
		city.SetPropertyBaseValue(SimulationProperties.CityOwnedTurn, (float)(this.GameService.Game as global::Game).Turn);
		SimulationDescriptor descriptor;
		if (base.Empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitMimics2) && !city.SimulationObject.Tags.Contains(City.MimicsCity) && this.SimulationDescriptorDatabase.TryGetValue(City.MimicsCity, out descriptor))
		{
			city.AddDescriptor(descriptor, false);
		}
		city.Refresh(true);
		this.ForceGrowthToCurrentPopulation(city, 0f);
		this.UpdatePointOfInterestImprovement(city);
		DepartmentOfHealth departmentOfHealth = base.Empire.GetAgency<DepartmentOfHealth>();
		if (departmentOfHealth != null)
		{
			departmentOfHealth.RefreshApprovalStatus();
		}
		this.AddDistrictDescriptorExploitableResource(city);
		yield break;
	}

	private bool CreateDistrictImprovementPreprocessor(OrderCreateDistrictImprovement order)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityGameEntityGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			return false;
		}
		City city = gameEntity as City;
		if (city == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity does not convert to a city.");
			return false;
		}
		District district = city.Districts.FirstOrDefault((District iterator) => iterator.WorldPosition == order.WorldPosition);
		if (district == null && (city.Camp == null || city.Camp.WorldPosition != order.WorldPosition))
		{
			Diagnostics.LogError("Order preprocessing failed because the target district cannot be located.");
			return false;
		}
		if ((district == null || district.Type != DistrictType.Exploitation) && (city.Camp == null || city.Camp.WorldPosition != order.WorldPosition))
		{
			Diagnostics.LogWarning("Order preprocessing failed because the target district is not an exploitation.");
			return false;
		}
		List<OrderCreateDistrictImprovement.CampGarrisonTransferData> list = new List<OrderCreateDistrictImprovement.CampGarrisonTransferData>();
		Camp camp = city.Camp;
		if (camp != null && camp.WorldPosition == order.WorldPosition && city.Camp.StandardUnits.Count > 0)
		{
			GameEntityGUID[] array = new GameEntityGUID[camp.StandardUnits.Count];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = camp.StandardUnits[i].GUID;
			}
			int num = (int)base.Empire.GetPropertyValue(SimulationProperties.ArmyUnitSlot);
			if (num <= 0)
			{
				Diagnostics.LogWarning("The maximum number Of units per army doesn't allow for transfer.");
				return false;
			}
			WorldArea worldArea = new WorldArea(new WorldPosition[]
			{
				camp.WorldPosition
			});
			int num2 = 0;
			int num3 = 1;
			int j = 0;
			while (j < array.Length)
			{
				int num4 = array.Length - j;
				if (num4 > num)
				{
					num4 = num;
				}
				WorldPosition worldPosition = WorldPosition.Invalid;
				WorldPosition worldPosition2 = WorldPosition.Invalid;
				bool flag = false;
				IPathfindingService service = DepartmentOfTheInterior.Game.Services.GetService<IPathfindingService>();
				GridMap<Army> gridMap = (!(DepartmentOfTheInterior.Game != null)) ? null : (DepartmentOfTheInterior.Game.World.Atlas.GetMap(WorldAtlas.Maps.Armies) as GridMap<Army>);
				for (;;)
				{
					if (num2 >= worldArea.WorldPositions.Count)
					{
						if (num3-- > 0)
						{
							worldArea = worldArea.Grow(this.WorldPositionningService.World.WorldParameters);
						}
						if (num2 >= worldArea.WorldPositions.Count)
						{
							goto Block_16;
						}
					}
					worldPosition = worldArea.WorldPositions[num2++];
					flag = worldPosition.IsValid;
					if (!flag || gridMap == null)
					{
						goto IL_2F0;
					}
					Army value = gridMap.GetValue(worldPosition);
					if (value == null)
					{
						goto IL_2F0;
					}
					flag = false;
					IL_3A2:
					if (flag)
					{
						break;
					}
					continue;
					IL_2F0:
					if (camp.City.Region != this.WorldPositionningService.GetRegion(worldPosition))
					{
						flag = false;
						goto IL_3A2;
					}
					if (!flag || service == null)
					{
						goto IL_3A2;
					}
					bool flag2 = service.IsTileStopableAndPassable(worldPosition, PathfindingMovementCapacity.Ground, PathfindingFlags.IgnoreFogOfWar);
					bool flag3 = service.IsTransitionPassable(camp.WorldPosition, worldPosition, PathfindingMovementCapacity.Ground, (PathfindingFlags)0);
					if (flag2 && (!(camp.WorldPosition != worldPosition) || flag3))
					{
						goto IL_3A2;
					}
					flag = false;
					if (!worldPosition2.IsValid && service.IsTilePassable(worldPosition, PathfindingMovementCapacity.Ground, PathfindingFlags.IgnoreFogOfWar) && service.IsTransitionPassable(camp.WorldPosition, worldPosition, PathfindingMovementCapacity.Ground, (PathfindingFlags)0))
					{
						worldPosition2 = worldPosition;
						goto IL_3A2;
					}
					goto IL_3A2;
				}
				IL_3A9:
				if (!flag)
				{
					worldPosition = WorldPosition.Invalid;
				}
				if (!worldPosition.IsValid && worldPosition2.IsValid)
				{
					worldPosition = worldPosition2;
				}
				if (worldPosition.IsValid)
				{
					OrderCreateDistrictImprovement.CampGarrisonTransferData item = default(OrderCreateDistrictImprovement.CampGarrisonTransferData);
					item.ArmyGUID = this.GameEntityRepositoryService.GenerateGUID();
					item.UnitsGUIDs = new GameEntityGUID[num4];
					Array.Copy(array, j, item.UnitsGUIDs, 0, num4);
					item.WorldPosition = worldPosition;
					item.WorldPositionToArmy = WorldPosition.Invalid;
					list.Add(item);
					j += num4;
					continue;
				}
				break;
				Block_16:
				worldPosition = WorldPosition.Invalid;
				goto IL_3A9;
			}
		}
		order.CampTransferData = list.ToArray();
		if (camp != null && camp.WorldPosition == order.WorldPosition)
		{
			order.ExploitationFromCampGUID = this.GameEntityRepositoryService.GenerateGUID();
		}
		DepartmentOfIndustry.ConstructibleElement constructibleElement;
		if (this.departmentOfIndustry == null || !this.departmentOfIndustry.ConstructibleElementDatabase.TryGetValue(order.ConstructibleElementName, out constructibleElement))
		{
			return false;
		}
		DistrictImprovementDefinition districtImprovementDefinition = constructibleElement as DistrictImprovementDefinition;
		if (districtImprovementDefinition == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the constructible element does not correspond to a district improvement.");
			return false;
		}
		order.DistrictType = districtImprovementDefinition.DistrictType;
		order.ResourceOnMigration = districtImprovementDefinition.ResourceOnMigration;
		switch (districtImprovementDefinition.DistrictType)
		{
		case DistrictType.Extension:
		{
			List<OrderCreateDistrictImprovement.DistrictTypeExploitationDescriptor> list2 = new List<OrderCreateDistrictImprovement.DistrictTypeExploitationDescriptor>();
			int regionIndex = (int)this.WorldPositionningService.GetRegionIndex(city.WorldPosition);
			for (int k = 0; k < 6; k++)
			{
				WorldPosition neighbourWorldPosition = this.WorldPositionningService.GetNeighbourTile(order.WorldPosition, (WorldOrientation)k, 1);
				if (neighbourWorldPosition.IsValid)
				{
					if (!city.Districts.Any((District iterator) => iterator.WorldPosition == neighbourWorldPosition))
					{
						int regionIndex2 = (int)this.WorldPositionningService.GetRegionIndex(neighbourWorldPosition);
						if (regionIndex2 == regionIndex)
						{
							if (this.WorldPositionningService.IsExploitable(neighbourWorldPosition, 0))
							{
								if (city.Camp == null || !(city.Camp.WorldPosition == neighbourWorldPosition))
								{
									OrderCreateDistrictImprovement.DistrictTypeExploitationDescriptor item2 = new OrderCreateDistrictImprovement.DistrictTypeExploitationDescriptor
									{
										GameEntityGUID = this.GameEntityRepositoryService.GenerateGUID(),
										WorldPosition = neighbourWorldPosition
									};
									byte terrainType = this.WorldPositionningService.GetTerrainType(neighbourWorldPosition);
									StaticString terrainTypeMappingName = this.WorldPositionningService.GetTerrainTypeMappingName(terrainType);
									item2.TerrainTypeName = terrainTypeMappingName;
									byte anomalyType = this.WorldPositionningService.GetAnomalyType(neighbourWorldPosition);
									StaticString anomalyTypeMappingName = this.WorldPositionningService.GetAnomalyTypeMappingName(anomalyType);
									item2.AnomalyTypeName = anomalyTypeMappingName;
									byte biomeType = this.WorldPositionningService.GetBiomeType(neighbourWorldPosition);
									StaticString biomeTypeMappingName = this.WorldPositionningService.GetBiomeTypeMappingName(biomeType);
									item2.BiomeTypeName = biomeTypeMappingName;
									short riverId = this.WorldPositionningService.GetRiverId(neighbourWorldPosition);
									StaticString riverTypeMappingName = this.WorldPositionningService.GetRiverTypeMappingName(riverId);
									item2.RiverTypeName = riverTypeMappingName;
									list2.Add(item2);
								}
							}
						}
					}
				}
			}
			order.DistrictTypeExploitationDescriptors = list2.ToArray();
			return true;
		}
		case DistrictType.Improvement:
			if (order.ConstructibleElementName == "DistrictImprovementDocks")
			{
				OrderUpdateCadastralMap orderUpdateCadastralMap = new OrderUpdateCadastralMap(base.Empire.Index, city, PathfindingMovementCapacity.Water, CadastralMapOperation.Connect);
				orderUpdateCadastralMap.WorldPosition = order.WorldPosition;
				((global::Empire)base.Empire).PlayerControllers.Server.PostOrder(orderUpdateCadastralMap);
			}
			return true;
		}
		Diagnostics.LogError("Order preprocessing failed because the district type is not valid (district type: '{0}').", new object[]
		{
			districtImprovementDefinition.DistrictType.ToString()
		});
		return false;
	}

	private IEnumerator CreateDistrictImprovementProcessor(OrderCreateDistrictImprovement order)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityGameEntityGUID, out gameEntity))
		{
			Diagnostics.LogError("Order processing failed because the target game entity is not valid.");
			yield break;
		}
		City city = gameEntity as City;
		if (city == null)
		{
			Diagnostics.LogError("Order processing failed because the target game entity does not convert to a city.");
			yield break;
		}
		if (order.ExploitationFromCampGUID != GameEntityGUID.Zero)
		{
			this.CreateDistrictImprovementProcessor_RazeCamp(order);
			byte terrainType = this.WorldPositionningService.GetTerrainType(order.WorldPosition);
			StaticString terrainTypeName = this.WorldPositionningService.GetTerrainTypeMappingName(terrainType);
			byte biomeType = this.WorldPositionningService.GetBiomeType(order.WorldPosition);
			StaticString biomeTypeName = this.WorldPositionningService.GetBiomeTypeMappingName(biomeType);
			byte anomalyType = this.WorldPositionningService.GetAnomalyType(order.WorldPosition);
			StaticString anomalyTypeName = this.WorldPositionningService.GetAnomalyTypeMappingName(anomalyType);
			short riverId = this.WorldPositionningService.GetRiverId(order.WorldPosition);
			StaticString riverTypeName = this.WorldPositionningService.GetRiverTypeMappingName(riverId);
			District newDistrictFromCamp = this.CreateDistrict(order.ExploitationFromCampGUID, order.WorldPosition, DistrictType.Exploitation, terrainTypeName, biomeTypeName, anomalyTypeName, riverTypeName);
			city.AddDistrict(newDistrictFromCamp);
			this.GameEntityRepositoryService.Register(newDistrictFromCamp);
		}
		District district = city.Districts.FirstOrDefault((District iterator) => iterator.WorldPosition == order.WorldPosition);
		if (district == null)
		{
			Diagnostics.LogError("Order processing failed because the target district cannot be located.");
			yield break;
		}
		for (int index = 0; index < this.pendingExtensions.Count; index++)
		{
			if (this.pendingExtensions[index].CityGameEntityGUID == order.CityGameEntityGUID && this.pendingExtensions[index].WorldPosition == order.WorldPosition)
			{
				this.pendingExtensions.RemoveAt(index);
				break;
			}
		}
		district.ResourceOnMigration = order.ResourceOnMigration;
		DepartmentOfIndustry.ConstructibleElement constructibleElement;
		if (!this.departmentOfIndustry.ConstructibleElementDatabase.TryGetValue(order.ConstructibleElementName, out constructibleElement))
		{
			yield break;
		}
		for (int index2 = 0; index2 < constructibleElement.Descriptors.Length; index2++)
		{
			district.AddDescriptor(constructibleElement.Descriptors[index2], false);
		}
		switch (order.DistrictType)
		{
		case DistrictType.Extension:
		{
			this.ExtendCityAt(city, district);
			IOrbService orbService = this.GameService.Game.Services.GetService<IOrbService>();
			if (orbService != null)
			{
				orbService.RemoveSpawnTileAtWorldPosition(district.WorldPosition);
			}
			if (order.DistrictTypeExploitationDescriptors != null && order.DistrictTypeExploitationDescriptors.Length != 0)
			{
				for (int index3 = 0; index3 < order.DistrictTypeExploitationDescriptors.Length; index3++)
				{
					OrderCreateDistrictImprovement.DistrictTypeExploitationDescriptor districtTypeExploitationDescriptor = order.DistrictTypeExploitationDescriptors[index3];
					if (!(districtTypeExploitationDescriptor.GameEntityGUID == GameEntityGUID.Zero))
					{
						if (city.Camp != null)
						{
							District campDistrict = city.Camp.GetDistrict(districtTypeExploitationDescriptor.WorldPosition);
							if (campDistrict != null && campDistrict.Type != DistrictType.Camp)
							{
								this.GameEntityRepositoryService.Unregister(campDistrict);
								city.RemoveChild(campDistrict);
								city.Camp.RemoveDistrict(campDistrict, true);
							}
						}
						district = this.CreateDistrict(districtTypeExploitationDescriptor.GameEntityGUID, districtTypeExploitationDescriptor.WorldPosition, DistrictType.Exploitation, districtTypeExploitationDescriptor.TerrainTypeName, districtTypeExploitationDescriptor.BiomeTypeName, districtTypeExploitationDescriptor.AnomalyTypeName, districtTypeExploitationDescriptor.RiverTypeName);
						if (district != null)
						{
							city.AddDistrict(district);
							this.GameEntityRepositoryService.Register(district);
						}
					}
				}
			}
			this.AddDistrictDescriptorExploitableResource(city);
			this.BuildFreePointOfInterestImprovement(city, order.WorldPosition);
			IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
			if (downloadableContentService.IsShared(DownloadableContent20.ReadOnlyName))
			{
				DepartmentOfCreepingNodes departmentOfCreepingNodes = base.Empire.GetAgency<DepartmentOfCreepingNodes>();
				if (departmentOfCreepingNodes != null)
				{
					departmentOfCreepingNodes.BuildFreeCreepingNodeImprovement(city, order.WorldPosition);
					departmentOfCreepingNodes.RefreshCityNodesFIMSE(city);
				}
			}
			break;
		}
		case DistrictType.Improvement:
		{
			DepartmentOfTheInterior.ApplyDistrictType(district, DistrictType.Improvement, null);
			IWorldPositionSimulationEvaluatorService worldPositionSimulationEvaluatorService = this.GameService.Game.Services.GetService<IWorldPositionSimulationEvaluatorService>();
			worldPositionSimulationEvaluatorService.SetSomethingChangedOnRegion((short)district.City.Region.Index);
			if (order.ConstructibleElementName == "DistrictImprovementDocks")
			{
				city.CadastralMap.ConnectedMovementCapacity |= PathfindingMovementCapacity.Water;
			}
			this.UpdateDistrictLevel(city, district);
			break;
		}
		}
		if (constructibleElement is DistrictImprovementDefinition)
		{
			DistrictImprovementDefinition districtImprovementDefinition = constructibleElement as DistrictImprovementDefinition;
			if (districtImprovementDefinition.PointOfInterestSimulationDescriptorReferences != null && districtImprovementDefinition.PointOfInterestSimulationDescriptorReferences.Length > 0)
			{
				GridMap<PointOfInterest> pointOfInterestMap = (this.GameService.Game as global::Game).World.Atlas.GetMap(WorldAtlas.Maps.PointOfInterest) as GridMap<PointOfInterest>;
				if (pointOfInterestMap != null)
				{
					PointOfInterest poi = pointOfInterestMap.GetValue(order.WorldPosition);
					if (poi != null)
					{
						for (int index4 = 0; index4 < districtImprovementDefinition.PointOfInterestSimulationDescriptorReferences.Length; index4++)
						{
							SimulationDescriptor descriptor;
							if (this.SimulationDescriptorDatabase.TryGetValue(districtImprovementDefinition.PointOfInterestSimulationDescriptorReferences[index4].Name, out descriptor))
							{
								poi.AddDescriptor(descriptor, false);
							}
						}
						poi.Refresh(false);
					}
				}
			}
		}
		city.Refresh(false);
		yield break;
	}

	private void CreateDistrictImprovementProcessor_RazeCamp(OrderCreateDistrictImprovement order)
	{
		City city = this.GetCity(order.CityGameEntityGUID);
		if (city == null)
		{
			return;
		}
		Camp camp = city.Camp;
		if (camp == null || camp.WorldPosition != order.WorldPosition)
		{
			return;
		}
		IPathfindingService service = DepartmentOfTheInterior.Game.Services.GetService<IPathfindingService>();
		DepartmentOfDefense agency = base.Empire.GetAgency<DepartmentOfDefense>();
		for (int i = 0; i < order.CampTransferData.Length; i++)
		{
			OrderCreateDistrictImprovement.CampGarrisonTransferData campGarrisonTransferData = order.CampTransferData[i];
			Army army = null;
			agency.CreateArmy(campGarrisonTransferData.ArmyGUID, null, campGarrisonTransferData.WorldPosition, out army, false, true);
			Unit unit = null;
			for (int j = 0; j < campGarrisonTransferData.UnitsGUIDs.Length; j++)
			{
				if (!this.GameEntityRepositoryService.TryGetValue<Unit>(campGarrisonTransferData.UnitsGUIDs[j], out unit))
				{
					Diagnostics.LogError("Could not found unit with GUID {0}.", new object[]
					{
						campGarrisonTransferData.UnitsGUIDs[j]
					});
				}
				else
				{
					if (camp.WorldPosition != campGarrisonTransferData.WorldPosition && !unit.UnitDesign.Tags.Contains(DownloadableContent9.TagSolitary))
					{
						float transitionCost = service.GetTransitionCost(camp.WorldPosition, campGarrisonTransferData.WorldPosition, unit, PathfindingFlags.IgnoreFogOfWar, null);
						float maximumMovementPoints = service.GetMaximumMovementPoints(campGarrisonTransferData.WorldPosition, unit, PathfindingFlags.IgnoreFogOfWar);
						float num = (maximumMovementPoints <= 0f) ? float.PositiveInfinity : (transitionCost / maximumMovementPoints);
						if (float.IsPositiveInfinity(num))
						{
							unit.SetPropertyBaseValue(SimulationProperties.MovementRatio, 0f);
						}
						else
						{
							float propertyValue = unit.GetPropertyValue(SimulationProperties.MovementRatio);
							unit.SetPropertyBaseValue(SimulationProperties.MovementRatio, propertyValue - num);
						}
					}
					SimulationDescriptor descriptor;
					if (army.SimulationObject.Tags.Contains(PathfindingContext.MovementCapacitySailDescriptor) && !unit.SimulationObject.Tags.Contains(PathfindingContext.MovementCapacitySailDescriptor) && this.SimulationDescriptorDatabase != null && this.SimulationDescriptorDatabase.TryGetValue(PathfindingContext.MovementCapacitySailDescriptor, out descriptor))
					{
						unit.AddDescriptor(descriptor, true);
					}
					if (!unit.SimulationObject.Tags.Contains(DownloadableContent16.SeafaringUnit))
					{
						unit.SwitchToEmbarkedUnit(service.GetTileMovementCapacity(army.WorldPosition, (PathfindingFlags)0) == PathfindingMovementCapacity.Water);
					}
					if (unit != camp.Hero)
					{
						camp.RemoveUnit(unit);
						army.AddUnit(unit);
					}
					else
					{
						camp.SetHero(null);
						army.SetHero(unit);
					}
				}
			}
			army.Refresh(false);
			this.GameEntityRepositoryService.Register(army);
			IDownloadableContentService service2 = Services.GetService<IDownloadableContentService>();
			agency.UpdateDetection(army);
			IOrbService service3 = this.GameService.Game.Services.GetService<IOrbService>();
			service3.CollectOrbsAtPosition(army.WorldPosition, army, base.Empire as global::Empire);
			if (service.GetTileMovementCapacity(army.WorldPosition, (PathfindingFlags)0) == PathfindingMovementCapacity.Water)
			{
				army.SetSails();
			}
			if (service2.IsShared(DownloadableContent19.ReadOnlyName))
			{
				agency.CheckArmiesOnMapBoost();
			}
		}
		city.Camp = null;
		for (int k = 0; k < camp.Districts.Count; k++)
		{
			this.GameEntityRepositoryService.Unregister(camp.Districts[k].GUID);
		}
		this.GameEntityRepositoryService.Unregister(camp);
		camp.Dispose();
	}

	private bool DestroyCampPreprocessor(OrderDestroyCamp order)
	{
		Camp camp = null;
		if (!order.CampGUID.IsValid || !this.GameEntityRepositoryService.TryGetValue<Camp>(order.CampGUID, out camp) || camp == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the target camp game entity GUID is not valid.");
			return false;
		}
		List<OrderDestroyCamp.GarrisonTransferData> list = new List<OrderDestroyCamp.GarrisonTransferData>();
		int count = camp.StandardUnits.Count;
		if (count > 0)
		{
			GameEntityGUID[] array = new GameEntityGUID[camp.StandardUnits.Count];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = camp.StandardUnits[i].GUID;
			}
			int num = (int)base.Empire.GetPropertyValue(SimulationProperties.ArmyUnitSlot);
			if (num <= 0)
			{
				Diagnostics.LogWarning("The maximum number Of units per army doesn't allow for transfer.");
				return false;
			}
			WorldArea worldArea = new WorldArea(new WorldPosition[]
			{
				camp.WorldPosition
			});
			int num2 = 0;
			int num3 = 1;
			int j = 0;
			while (j < array.Length)
			{
				int num4 = array.Length - j;
				if (num4 > num)
				{
					num4 = num;
				}
				WorldPosition worldPosition = WorldPosition.Invalid;
				WorldPosition worldPosition2 = WorldPosition.Invalid;
				bool flag = false;
				IPathfindingService service = DepartmentOfTheInterior.Game.Services.GetService<IPathfindingService>();
				GridMap<Army> gridMap = (!(DepartmentOfTheInterior.Game != null)) ? null : (DepartmentOfTheInterior.Game.World.Atlas.GetMap(WorldAtlas.Maps.Armies) as GridMap<Army>);
				for (;;)
				{
					if (num2 >= worldArea.WorldPositions.Count)
					{
						if (num3-- > 0)
						{
							worldArea = worldArea.Grow(this.WorldPositionningService.World.WorldParameters);
						}
						if (num2 >= worldArea.WorldPositions.Count)
						{
							goto Block_10;
						}
					}
					worldPosition = worldArea.WorldPositions[num2++];
					flag = worldPosition.IsValid;
					if (!flag || gridMap == null)
					{
						goto IL_205;
					}
					Army value = gridMap.GetValue(worldPosition);
					if (value == null)
					{
						goto IL_205;
					}
					flag = false;
					IL_2B3:
					if (flag)
					{
						break;
					}
					continue;
					IL_205:
					if (camp.City.Region != this.WorldPositionningService.GetRegion(worldPosition))
					{
						flag = false;
						goto IL_2B3;
					}
					if (!flag || service == null)
					{
						goto IL_2B3;
					}
					bool flag2 = service.IsTileStopableAndPassable(worldPosition, PathfindingMovementCapacity.Ground, PathfindingFlags.IgnoreFogOfWar);
					bool flag3 = service.IsTransitionPassable(camp.WorldPosition, worldPosition, PathfindingMovementCapacity.Ground, (PathfindingFlags)0);
					if (flag2 && (!(camp.WorldPosition != worldPosition) || flag3))
					{
						goto IL_2B3;
					}
					flag = false;
					if (!worldPosition2.IsValid && service.IsTilePassable(worldPosition, PathfindingMovementCapacity.Ground, PathfindingFlags.IgnoreFogOfWar) && service.IsTransitionPassable(camp.WorldPosition, worldPosition, PathfindingMovementCapacity.Ground, (PathfindingFlags)0))
					{
						worldPosition2 = worldPosition;
						goto IL_2B3;
					}
					goto IL_2B3;
				}
				IL_2BA:
				if (!flag)
				{
					worldPosition = WorldPosition.Invalid;
				}
				if (!worldPosition.IsValid && worldPosition2.IsValid)
				{
					worldPosition = worldPosition2;
				}
				if (worldPosition.IsValid)
				{
					OrderDestroyCamp.GarrisonTransferData item = default(OrderDestroyCamp.GarrisonTransferData);
					item.ArmyGUID = this.GameEntityRepositoryService.GenerateGUID();
					item.UnitsGUIDs = new GameEntityGUID[num4];
					Array.Copy(array, j, item.UnitsGUIDs, 0, num4);
					item.WorldPosition = worldPosition;
					list.Add(item);
					j += num4;
					continue;
				}
				break;
				Block_10:
				worldPosition = WorldPosition.Invalid;
				goto IL_2BA;
			}
		}
		order.TransferData = list.ToArray();
		for (int k = 0; k < 6; k++)
		{
			WorldPosition neighbourTile = this.WorldPositionningService.GetNeighbourTile(camp.WorldPosition, (WorldOrientation)k, 1);
			for (int l = 0; l < camp.City.Districts.Count; l++)
			{
				if (camp.City.Districts[l].WorldPosition == neighbourTile && camp.City.Districts[l].Type != DistrictType.Exploitation)
				{
					order.ExploitationFromCampGUID = this.GameEntityRepositoryService.GenerateGUID();
					k += 6;
					break;
				}
			}
		}
		return true;
	}

	private IEnumerator DestroyCampProcessor(OrderDestroyCamp order)
	{
		Camp camp = null;
		if (!this.GameEntityRepositoryService.TryGetValue<Camp>(order.CampGUID, out camp))
		{
			yield break;
		}
		WorldPosition campPosition = camp.WorldPosition;
		IPathfindingService pathfindingService = DepartmentOfTheInterior.Game.Services.GetService<IPathfindingService>();
		object obj = (!(DepartmentOfTheInterior.Game != null)) ? null : (DepartmentOfTheInterior.Game.World.Atlas.GetMap(WorldAtlas.Maps.Armies) as GridMap<Army>);
		DepartmentOfDefense departmentOfDefense = base.Empire.GetAgency<DepartmentOfDefense>();
		for (int armyIndex = 0; armyIndex < order.TransferData.Length; armyIndex++)
		{
			OrderDestroyCamp.GarrisonTransferData transferData = order.TransferData[armyIndex];
			Army army = null;
			departmentOfDefense.CreateArmy(transferData.ArmyGUID, null, transferData.WorldPosition, out army, false, true);
			Unit unit = null;
			for (int unitIndex = 0; unitIndex < transferData.UnitsGUIDs.Length; unitIndex++)
			{
				if (!this.GameEntityRepositoryService.TryGetValue<Unit>(transferData.UnitsGUIDs[unitIndex], out unit))
				{
					Diagnostics.LogError("Could not found unit with GUID {0}.", new object[]
					{
						transferData.UnitsGUIDs[unitIndex]
					});
				}
				else
				{
					if (campPosition != transferData.WorldPosition && !unit.UnitDesign.Tags.Contains(DownloadableContent9.TagSolitary))
					{
						float cost = pathfindingService.GetTransitionCost(campPosition, transferData.WorldPosition, unit, PathfindingFlags.IgnoreFogOfWar, null);
						float maximumMovement = pathfindingService.GetMaximumMovementPoints(transferData.WorldPosition, unit, PathfindingFlags.IgnoreFogOfWar);
						float costRatio = (maximumMovement <= 0f) ? float.PositiveInfinity : (cost / maximumMovement);
						if (float.IsPositiveInfinity(costRatio))
						{
							unit.SetPropertyBaseValue(SimulationProperties.MovementRatio, 0f);
						}
						else
						{
							float movementRatio = unit.GetPropertyValue(SimulationProperties.MovementRatio);
							unit.SetPropertyBaseValue(SimulationProperties.MovementRatio, movementRatio - costRatio);
						}
					}
					SimulationDescriptor simulationDescriptor;
					if (army.SimulationObject.Tags.Contains(PathfindingContext.MovementCapacitySailDescriptor) && !unit.SimulationObject.Tags.Contains(PathfindingContext.MovementCapacitySailDescriptor) && this.SimulationDescriptorDatabase != null && this.SimulationDescriptorDatabase.TryGetValue(PathfindingContext.MovementCapacitySailDescriptor, out simulationDescriptor))
					{
						unit.AddDescriptor(simulationDescriptor, true);
					}
					if (!unit.SimulationObject.Tags.Contains(DownloadableContent16.SeafaringUnit))
					{
						unit.SwitchToEmbarkedUnit(pathfindingService.GetTileMovementCapacity(army.WorldPosition, (PathfindingFlags)0) == PathfindingMovementCapacity.Water);
					}
					if (unit != camp.Hero)
					{
						camp.RemoveUnit(unit);
						army.AddUnit(unit);
					}
					else
					{
						camp.SetHero(null);
						army.SetHero(unit);
					}
				}
			}
			army.Refresh(false);
			this.GameEntityRepositoryService.Register(army);
			IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
			departmentOfDefense.UpdateDetection(army);
			IOrbService orbService = this.GameService.Game.Services.GetService<IOrbService>();
			orbService.CollectOrbsAtPosition(army.WorldPosition, army, base.Empire as global::Empire);
			if (pathfindingService.GetTileMovementCapacity(army.WorldPosition, (PathfindingFlags)0) == PathfindingMovementCapacity.Water)
			{
				army.SetSails();
			}
			if (downloadableContentService.IsShared(DownloadableContent19.ReadOnlyName))
			{
				departmentOfDefense.CheckArmiesOnMapBoost();
			}
		}
		if (order.Sellout)
		{
			global::PlayerController serverPlayerController = (base.Empire as global::Empire).PlayerControllers.Server;
			if (serverPlayerController != null)
			{
				OrderTransferResources transferDustOrder = new OrderTransferResources(base.Empire.Index, DepartmentOfTheTreasury.Resources.EmpireMoney, camp.GetPropertyValue(SimulationProperties.SelloutPrice), 0UL);
				serverPlayerController.PostOrder(transferDustOrder);
			}
		}
		City city = camp.City;
		city.Camp = null;
		for (int districtIndex = 0; districtIndex < camp.Districts.Count; districtIndex++)
		{
			this.GameEntityRepositoryService.Unregister(camp.Districts[districtIndex].GUID);
		}
		this.GameEntityRepositoryService.Unregister(camp);
		camp.Dispose();
		if (order.ExploitationFromCampGUID != GameEntityGUID.Zero)
		{
			byte terrainType = this.WorldPositionningService.GetTerrainType(campPosition);
			StaticString terrainTypeName = this.WorldPositionningService.GetTerrainTypeMappingName(terrainType);
			byte biomeType = this.WorldPositionningService.GetBiomeType(campPosition);
			StaticString biomeTypeName = this.WorldPositionningService.GetBiomeTypeMappingName(biomeType);
			byte anomalyType = this.WorldPositionningService.GetAnomalyType(campPosition);
			StaticString anomalyTypeName = this.WorldPositionningService.GetAnomalyTypeMappingName(anomalyType);
			short riverId = this.WorldPositionningService.GetRiverId(campPosition);
			StaticString riverTypeName = this.WorldPositionningService.GetRiverTypeMappingName(riverId);
			District newDistrictFromCamp = this.CreateDistrict(order.ExploitationFromCampGUID, campPosition, DistrictType.Exploitation, terrainTypeName, biomeTypeName, anomalyTypeName, riverTypeName);
			city.AddDistrict(newDistrictFromCamp);
			this.GameEntityRepositoryService.Register(newDistrictFromCamp);
		}
		this.VisibilityService.NotifyVisibilityHasChanged(base.Empire as global::Empire);
		yield break;
	}

	private bool DestroyCityPreprocessor(OrderDestroyCity order)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target city game entity GUID is not valid.");
			return false;
		}
		if (!(gameEntity is City))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a city.");
			return false;
		}
		City city = gameEntity as City;
		if (city.Empire != base.Empire)
		{
			Diagnostics.LogError("Order preprocessing failed because the target city is not owned by the empire.");
			return false;
		}
		if (city.Region == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the target city isn't attached to any region.");
			return false;
		}
		List<OrderDestroyCity.CampGarrisonTransferData> list = new List<OrderDestroyCity.CampGarrisonTransferData>();
		Camp camp = city.Camp;
		if (camp != null && city.Camp.StandardUnits.Count > 0)
		{
			GameEntityGUID[] array = new GameEntityGUID[camp.StandardUnits.Count];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = camp.StandardUnits[i].GUID;
			}
			int num = (int)base.Empire.GetPropertyValue(SimulationProperties.ArmyUnitSlot);
			if (num <= 0)
			{
				Diagnostics.LogWarning("The maximum number Of units per army doesn't allow for transfer.");
				return false;
			}
			WorldArea worldArea = new WorldArea(new WorldPosition[]
			{
				camp.WorldPosition
			});
			int num2 = 0;
			int num3 = 1;
			int j = 0;
			while (j < array.Length)
			{
				int num4 = array.Length - j;
				if (num4 > num)
				{
					num4 = num;
				}
				WorldPosition worldPosition = WorldPosition.Invalid;
				WorldPosition worldPosition2 = WorldPosition.Invalid;
				bool flag = false;
				IPathfindingService service = DepartmentOfTheInterior.Game.Services.GetService<IPathfindingService>();
				GridMap<Army> gridMap = (!(DepartmentOfTheInterior.Game != null)) ? null : (DepartmentOfTheInterior.Game.World.Atlas.GetMap(WorldAtlas.Maps.Armies) as GridMap<Army>);
				for (;;)
				{
					if (num2 >= worldArea.WorldPositions.Count)
					{
						if (num3-- > 0)
						{
							worldArea = worldArea.Grow(this.WorldPositionningService.World.WorldParameters);
						}
						if (num2 >= worldArea.WorldPositions.Count)
						{
							goto Block_13;
						}
					}
					worldPosition = worldArea.WorldPositions[num2++];
					flag = worldPosition.IsValid;
					if (!flag || gridMap == null)
					{
						goto IL_251;
					}
					Army value = gridMap.GetValue(worldPosition);
					if (value == null)
					{
						goto IL_251;
					}
					flag = false;
					IL_2FF:
					if (flag)
					{
						break;
					}
					continue;
					IL_251:
					if (camp.City.Region != this.WorldPositionningService.GetRegion(worldPosition))
					{
						flag = false;
						goto IL_2FF;
					}
					if (!flag || service == null)
					{
						goto IL_2FF;
					}
					bool flag2 = service.IsTileStopableAndPassable(worldPosition, PathfindingMovementCapacity.Ground, PathfindingFlags.IgnoreFogOfWar);
					bool flag3 = service.IsTransitionPassable(camp.WorldPosition, worldPosition, PathfindingMovementCapacity.Ground, (PathfindingFlags)0);
					if (flag2 && (!(camp.WorldPosition != worldPosition) || flag3))
					{
						goto IL_2FF;
					}
					flag = false;
					if (!worldPosition2.IsValid && service.IsTilePassable(worldPosition, PathfindingMovementCapacity.Ground, PathfindingFlags.IgnoreFogOfWar) && service.IsTransitionPassable(camp.WorldPosition, worldPosition, PathfindingMovementCapacity.Ground, (PathfindingFlags)0))
					{
						worldPosition2 = worldPosition;
						goto IL_2FF;
					}
					goto IL_2FF;
				}
				IL_306:
				if (!flag)
				{
					worldPosition = WorldPosition.Invalid;
				}
				if (!worldPosition.IsValid && worldPosition2.IsValid)
				{
					worldPosition = worldPosition2;
				}
				if (worldPosition.IsValid)
				{
					OrderDestroyCity.CampGarrisonTransferData item = default(OrderDestroyCity.CampGarrisonTransferData);
					item.ArmyGUID = this.GameEntityRepositoryService.GenerateGUID();
					item.UnitsGUIDs = new GameEntityGUID[num4];
					Array.Copy(array, j, item.UnitsGUIDs, 0, num4);
					item.WorldPosition = worldPosition;
					list.Add(item);
					j += num4;
					continue;
				}
				break;
				Block_13:
				worldPosition = WorldPosition.Invalid;
				goto IL_306;
			}
		}
		order.CampTransferData = list.ToArray();
		return true;
	}

	private IEnumerator DestroyCityProcessor(OrderDestroyCity order)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target city game entity GUID is not valid.");
			yield break;
		}
		if (!(gameEntity is City))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a city.");
			yield break;
		}
		City city = gameEntity as City;
		if (city.Camp != null)
		{
			this.DestroyCityProcessor_RazeCamp(order);
		}
		if (city.BesiegingEmpireIndex != -1)
		{
			this.StopSiege(city);
		}
		this.StopNavalSiege(city);
		Army[] earthquakeInstigators = DepartmentOfTheInterior.GetCityEarthquakeInstigators(city);
		for (int armyIndex = 0; armyIndex < earthquakeInstigators.Length; armyIndex++)
		{
			earthquakeInstigators[armyIndex].SetEarthquakerStatus(false, false, null);
		}
		if (this.MainCity != null && this.MainCity.GUID == city.GUID)
		{
			MajorEmpire majorEmpire = base.Empire as MajorEmpire;
			if (majorEmpire != null)
			{
				majorEmpire.UnconvertAndPacifyAllConvertedVillages();
			}
		}
		MinorEmpire minorEmpire = city.Region.MinorEmpire;
		for (int index = 0; index < city.Region.PointOfInterests.Length; index++)
		{
			city.Region.PointOfInterests[index].RemoveDescriptorByName(DepartmentOfTheInterior.PointOfInterestOnDistrict);
		}
		if (order.ShouldDestroyRegionBuildingWithSelf)
		{
			foreach (PointOfInterest currentPointsOfInterest in city.Region.PointOfInterests)
			{
				string pointOfInterestDefinitionType;
				if (currentPointsOfInterest.PointOfInterestImprovement != null && currentPointsOfInterest.PointOfInterestDefinition.TryGetValue("Type", out pointOfInterestDefinitionType) && pointOfInterestDefinitionType != "Village")
				{
					DepartmentOfTheInterior.DestroyPointOfInterest(currentPointsOfInterest);
				}
			}
		}
		this.RemoveCity(city, false);
		this.UnbindMinorEmpireToCity(city, minorEmpire);
		DepartmentOfIndustry industry = base.Empire.GetAgency<DepartmentOfIndustry>();
		if (industry != null)
		{
			industry.RemoveQueueFrom<City>(city);
		}
		while (city.StandardUnits.Count > 0)
		{
			Unit unit = city.StandardUnits[0];
			city.RemoveUnit(unit);
			this.GameEntityRepositoryService.Unregister(unit);
			unit.Dispose();
		}
		if (city.Militia != null && city.Militia.StandardUnits.Count > 0)
		{
			while (city.Militia.StandardUnits.Count > 0)
			{
				Unit unit2 = city.Militia.StandardUnits[0];
				city.Militia.RemoveUnit(unit2);
				this.GameEntityRepositoryService.Unregister(unit2);
				unit2.Dispose();
			}
		}
		if (city.Hero != null)
		{
			DepartmentOfEducation education = base.Empire.GetAgency<DepartmentOfEducation>();
			education.UnassignHero(city.Hero);
		}
		DepartmentOfPlanificationAndDevelopment planificationAndDevelopment = base.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
		if (planificationAndDevelopment != null)
		{
			planificationAndDevelopment.RemoveBoostersFromTarget(city.GUID, -1);
		}
		if (this.departmentOfIntelligence != null && order.ShouldInjureSpy)
		{
			foreach (SpiedGarrison spiedGarrison in DepartmentOfIntelligence.GetCityInfiltratedSpies(order.CityGuid))
			{
				DepartmentOfEducation spyDepartmentOfEducation = spiedGarrison.Empire.GetAgency<DepartmentOfEducation>();
				if (spiedGarrison.Empire.Index == order.ShouldSpareSpyFromEmpireIndex)
				{
					spyDepartmentOfEducation.UnassignHero(spiedGarrison.Hero);
				}
				else if (spyDepartmentOfEducation != null)
				{
					spyDepartmentOfEducation.InjureHero(spiedGarrison.Hero, true);
				}
			}
		}
		for (int index2 = 0; index2 < city.CityImprovements.Count; index2++)
		{
			this.GameEntityRepositoryService.Unregister(city.CityImprovements[index2]);
		}
		for (int index3 = 0; index3 < city.Districts.Count; index3++)
		{
			this.GameEntityRepositoryService.Unregister(city.Districts[index3]);
		}
		if (city.Militia != null)
		{
			this.GameEntityRepositoryService.Unregister(city.Militia);
		}
		this.GameEntityRepositoryService.Unregister(city);
		if (this.EventService != null)
		{
			EventCityDestroyed eventCityDestroyed = new EventCityDestroyed(city.Empire, city.Region);
			this.EventService.Notify(eventCityDestroyed);
		}
		city.Dispose();
		this.VisibilityService.NotifyVisibilityHasChanged(base.Empire as global::Empire);
		yield break;
	}

	private void DestroyCityProcessor_RazeCamp(OrderDestroyCity order)
	{
		City city = this.GetCity(order.CityGuid);
		if (city == null)
		{
			return;
		}
		Camp camp = city.Camp;
		if (city.Camp == null)
		{
			return;
		}
		IPathfindingService service = DepartmentOfTheInterior.Game.Services.GetService<IPathfindingService>();
		DepartmentOfDefense agency = base.Empire.GetAgency<DepartmentOfDefense>();
		for (int i = 0; i < order.CampTransferData.Length; i++)
		{
			OrderDestroyCity.CampGarrisonTransferData campGarrisonTransferData = order.CampTransferData[i];
			Army army = null;
			agency.CreateArmy(campGarrisonTransferData.ArmyGUID, null, campGarrisonTransferData.WorldPosition, out army, false, true);
			Unit unit = null;
			for (int j = 0; j < campGarrisonTransferData.UnitsGUIDs.Length; j++)
			{
				if (!this.GameEntityRepositoryService.TryGetValue<Unit>(campGarrisonTransferData.UnitsGUIDs[j], out unit))
				{
					Diagnostics.LogError("Could not found unit with GUID {0}.", new object[]
					{
						campGarrisonTransferData.UnitsGUIDs[j]
					});
				}
				else
				{
					if (camp.WorldPosition != campGarrisonTransferData.WorldPosition && !unit.UnitDesign.Tags.Contains(DownloadableContent9.TagSolitary))
					{
						float transitionCost = service.GetTransitionCost(camp.WorldPosition, campGarrisonTransferData.WorldPosition, unit, PathfindingFlags.IgnoreFogOfWar, null);
						float maximumMovementPoints = service.GetMaximumMovementPoints(campGarrisonTransferData.WorldPosition, unit, PathfindingFlags.IgnoreFogOfWar);
						float num = (maximumMovementPoints <= 0f) ? float.PositiveInfinity : (transitionCost / maximumMovementPoints);
						if (float.IsPositiveInfinity(num))
						{
							unit.SetPropertyBaseValue(SimulationProperties.MovementRatio, 0f);
						}
						else
						{
							float propertyValue = unit.GetPropertyValue(SimulationProperties.MovementRatio);
							unit.SetPropertyBaseValue(SimulationProperties.MovementRatio, propertyValue - num);
						}
					}
					SimulationDescriptor descriptor;
					if (army.SimulationObject.Tags.Contains(PathfindingContext.MovementCapacitySailDescriptor) && !unit.SimulationObject.Tags.Contains(PathfindingContext.MovementCapacitySailDescriptor) && this.SimulationDescriptorDatabase != null && this.SimulationDescriptorDatabase.TryGetValue(PathfindingContext.MovementCapacitySailDescriptor, out descriptor))
					{
						unit.AddDescriptor(descriptor, true);
					}
					if (!unit.SimulationObject.Tags.Contains(DownloadableContent16.SeafaringUnit))
					{
						unit.SwitchToEmbarkedUnit(service.GetTileMovementCapacity(army.WorldPosition, (PathfindingFlags)0) == PathfindingMovementCapacity.Water);
					}
					if (unit != camp.Hero)
					{
						camp.RemoveUnit(unit);
						army.AddUnit(unit);
					}
					else
					{
						camp.SetHero(null);
						army.SetHero(unit);
					}
				}
			}
			army.Refresh(false);
			this.GameEntityRepositoryService.Register(army);
			IDownloadableContentService service2 = Services.GetService<IDownloadableContentService>();
			agency.UpdateDetection(army);
			IOrbService service3 = this.GameService.Game.Services.GetService<IOrbService>();
			service3.CollectOrbsAtPosition(army.WorldPosition, army, base.Empire as global::Empire);
			if (service.GetTileMovementCapacity(army.WorldPosition, (PathfindingFlags)0) == PathfindingMovementCapacity.Water)
			{
				army.SetSails();
			}
			if (service2.IsShared(DownloadableContent19.ReadOnlyName))
			{
				agency.CheckArmiesOnMapBoost();
			}
		}
		city.Camp = null;
		for (int k = 0; k < camp.Districts.Count; k++)
		{
			this.GameEntityRepositoryService.Unregister(camp.Districts[k].GUID);
		}
		this.GameEntityRepositoryService.Unregister(camp);
		camp.Dispose();
	}

	private bool DestroyCityImprovementPreprocessor(OrderDestroyCityImprovement order)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityImprovementGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target city improvement game entity GUID is not valid.");
			return false;
		}
		if (!(gameEntity is CityImprovement))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a city improvement.");
			return false;
		}
		CityImprovement cityImprovement = gameEntity as CityImprovement;
		if (cityImprovement.City == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the target city improvement is not owned by a city.");
			return false;
		}
		if (!cityImprovement.City.CityImprovements.Any((CityImprovement match) => match.GUID == cityImprovement.GUID))
		{
			Diagnostics.LogError("Order preprocessing failed because the target city does not contains the city improvement.");
			return false;
		}
		return cityImprovement.City.BesiegingEmpireIndex == -1 && !cityImprovement.City.IsInEncounter && !cityImprovement.City.IsInfected;
	}

	private IEnumerator DestroyCityImprovementProcessor(OrderDestroyCityImprovement order)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityImprovementGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target city improvement game entity GUID is not valid.");
			yield break;
		}
		if (!(gameEntity is CityImprovement))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a city improvement.");
			yield break;
		}
		CityImprovement cityImprovement = gameEntity as CityImprovement;
		City city = cityImprovement.City;
		if (city != null)
		{
			if (cityImprovement.CityImprovementDefinition.Name == "CityImprovementRoads")
			{
				city.CadastralMap.ConnectedMovementCapacity &= ~PathfindingMovementCapacity.Ground;
				if (city.CadastralMap.Roads != null)
				{
					IGameService gameService = Services.GetService<IGameService>();
					if (gameService != null && gameService.Game != null)
					{
						ICadasterService cadasterService = gameService.Game.Services.GetService<ICadasterService>();
						if (cadasterService != null)
						{
							cadasterService.Disconnect(city, PathfindingMovementCapacity.Ground, false);
							cadasterService.RefreshCadasterMap();
						}
					}
				}
			}
			city.RemoveCityImprovement(cityImprovement);
			city.Refresh(false);
			DepartmentOfHealth departmentOfHealth = base.Empire.GetAgency<DepartmentOfHealth>();
			if (departmentOfHealth != null)
			{
				departmentOfHealth.RefreshApprovalStatus();
			}
		}
		this.GameEntityRepositoryService.Unregister(cityImprovement);
		yield break;
	}

	private bool DestroyPointOfInterestImprovementPreprocessor(OrderDestroyPointOfInterestImprovement order)
	{
		if (order.PointsOfInterestGUIDs == null)
		{
			return false;
		}
		IGameEntity gameEntity = null;
		bool flag = false;
		if (order.ArmyGUID.IsValid)
		{
			if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGUID, out gameEntity))
			{
				return false;
			}
			Army army = gameEntity as Army;
			if (army == null)
			{
				return false;
			}
			if (order.NumberOfActionPointsToSpend < 0f && !StaticString.IsNullOrEmpty(order.ArmyActionName))
			{
				ArmyAction armyAction = null;
				IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
				if (database != null && database.TryGetValue(order.ArmyActionName, out armyAction))
				{
					order.NumberOfActionPointsToSpend = armyAction.GetCostInActionPoints();
				}
			}
			if (order.NumberOfActionPointsToSpend > 0f)
			{
				SimulationObjectWrapper simulationObjectWrapper = army;
				if (simulationObjectWrapper != null)
				{
					float propertyValue = simulationObjectWrapper.GetPropertyValue(SimulationProperties.MaximumNumberOfActionPoints);
					float propertyValue2 = simulationObjectWrapper.GetPropertyValue(SimulationProperties.ActionPointsSpent);
					if (order.NumberOfActionPointsToSpend > propertyValue - propertyValue2)
					{
						Diagnostics.LogWarning("Not enough action points.");
						return false;
					}
				}
			}
			flag = true;
		}
		int num = 0;
		for (int i = 0; i < order.PointsOfInterestGUIDs.Length; i++)
		{
			if (!this.GameEntityRepositoryService.TryGetValue(order.PointsOfInterestGUIDs[i], out gameEntity))
			{
				Diagnostics.LogWarning("Target game entity is not valid.");
				order.PointsOfInterestGUIDs[i] = GameEntityGUID.Zero;
			}
			else if (!(gameEntity is PointOfInterest))
			{
				Diagnostics.LogWarning("Target game entity is not a point of interest.");
				order.PointsOfInterestGUIDs[i] = GameEntityGUID.Zero;
			}
			else
			{
				PointOfInterest pointOfInterest = gameEntity as PointOfInterest;
				if (pointOfInterest.PointOfInterestImprovement == null)
				{
					Diagnostics.LogWarning("Target point of interest has no improvement to destroy.");
					order.PointsOfInterestGUIDs[i] = GameEntityGUID.Zero;
				}
				else if (pointOfInterest.SimulationObject.Tags.Contains(DepartmentOfDefense.PillageStatusDescriptor))
				{
					order.PointsOfInterestGUIDs[i] = GameEntityGUID.Zero;
				}
				else
				{
					Region region = this.WorldPositionningService.GetRegion(pointOfInterest.WorldPosition);
					if (region == null)
					{
						Diagnostics.LogWarning("Target point of interest is not in a valid region.");
						order.PointsOfInterestGUIDs[i] = GameEntityGUID.Zero;
					}
					else if (region.City.BesiegingEmpireIndex != -1 && !flag)
					{
						order.PointsOfInterestGUIDs[i] = GameEntityGUID.Zero;
					}
					else
					{
						num++;
					}
				}
			}
		}
		return num != 0;
	}

	private IEnumerator DestroyPointOfInterestImprovementProcessor(OrderDestroyPointOfInterestImprovement order)
	{
		if (order.PointsOfInterestGUIDs == null)
		{
			yield break;
		}
		IGameEntity gameEntity = null;
		Army army = null;
		if (order.ArmyGUID.IsValid && this.GameEntityRepositoryService.TryGetValue(order.ArmyGUID, out gameEntity))
		{
			army = (gameEntity as Army);
		}
		IGameEntityWithWorldPosition singleTarget = null;
		for (int index = 0; index < order.PointsOfInterestGUIDs.Length; index++)
		{
			if (order.PointsOfInterestGUIDs[index].IsValid)
			{
				if (this.GameEntityRepositoryService.TryGetValue(order.PointsOfInterestGUIDs[index], out gameEntity))
				{
					if (gameEntity is PointOfInterest)
					{
						PointOfInterest pointOfInterest = gameEntity as PointOfInterest;
						global::Empire notificationEmpire = pointOfInterest.Empire;
						string notificationPointOfInterestImprovementName = pointOfInterest.PointOfInterestImprovement.Name;
						WorldPosition notificationPosition = pointOfInterest.WorldPosition;
						DepartmentOfTheInterior.DestroyPointOfInterest(pointOfInterest);
						if (army != null && this.EventService != null && notificationEmpire != null && !string.IsNullOrEmpty(notificationPointOfInterestImprovementName) && notificationPosition.IsValid)
						{
							EventRegionalBuildingDestroyed eventRegionalBuildingDestroyed = new EventRegionalBuildingDestroyed(notificationEmpire, army.Empire, notificationPointOfInterestImprovementName, notificationPosition);
							this.EventService.Notify(eventRegionalBuildingDestroyed);
						}
						if (singleTarget == null)
						{
							singleTarget = pointOfInterest;
						}
					}
				}
			}
		}
		if (army == null)
		{
			yield break;
		}
		if (order.NumberOfActionPointsToSpend > 0f)
		{
			ArmyAction.SpendSomeNumberOfActionPoints(army, order.NumberOfActionPointsToSpend);
		}
		if (order.ArmyActionCooldownDuration > 0f)
		{
			ArmyActionWithCooldown.ApplyCooldown(army, order.ArmyActionCooldownDuration);
		}
		if (StaticString.IsNullOrEmpty(order.ArmyActionName))
		{
			yield break;
		}
		ArmyAction armyAction = null;
		IDatabase<ArmyAction> armyActionDatabase = Databases.GetDatabase<ArmyAction>(false);
		if (armyActionDatabase == null)
		{
			yield break;
		}
		if (armyActionDatabase.TryGetValue(order.ArmyActionName, out armyAction))
		{
			if (!(armyAction is IArmyActionWithTargetSelection))
			{
				singleTarget = army;
			}
			army.OnArmyAction(armyAction, singleTarget);
			yield break;
		}
		yield break;
	}

	public static ConstructionCost[] GetDissentionCost(global::Empire empire, Village village)
	{
		float num = 0f;
		num += empire.GetPropertyValue(SimulationProperties.DissentCost);
		if (village.PointOfInterest.PointOfInterestImprovement == null)
		{
			num += village.GetPropertyValue(SimulationProperties.DissentDestroyedCost);
		}
		else
		{
			num += village.GetPropertyValue(SimulationProperties.DissentPacifiedCost);
		}
		global::Empire owner = village.Region.Owner;
		if (owner == null)
		{
			float num2 = 1f;
			num *= num2;
		}
		else if (owner.Index == empire.Index)
		{
			float num3 = 1f + empire.GetPropertyValue(SimulationProperties.DissentCostReduction);
			num *= num3;
		}
		else
		{
			float num4 = 1f + empire.GetPropertyValue(SimulationProperties.DissentCostPenalty);
			num *= num4;
		}
		return new ConstructionCost[]
		{
			new ConstructionCost(DepartmentOfTheTreasury.Resources.EmpirePoint, num, true, false)
		};
	}

	private bool DissentVillagePreprocessor(OrderDissentVillage order)
	{
		if (!base.Empire.SimulationObject.Tags.Contains(DownloadableContent16.FactionTraitDissent))
		{
			return false;
		}
		Region region = this.WorldPositionningService.GetRegion(order.VillageWorldPosition);
		if (region == null || region.MinorEmpire == null)
		{
			return false;
		}
		BarbarianCouncil agency = region.MinorEmpire.GetAgency<BarbarianCouncil>();
		if (agency == null)
		{
			return false;
		}
		Village villageAt = agency.GetVillageAt(order.VillageWorldPosition);
		if (villageAt == null)
		{
			return false;
		}
		if (!villageAt.HasBeenPacified)
		{
			return false;
		}
		if (villageAt.PointOfInterest.SimulationObject.Tags.Contains(DepartmentOfDefense.PillageStatusDescriptor))
		{
			return false;
		}
		order.DissentionCost = DepartmentOfTheInterior.GetDissentionCost((global::Empire)base.Empire, villageAt);
		for (int i = 0; i < order.DissentionCost.Length; i++)
		{
			float num = -order.DissentionCost[i].GetValue(base.Empire);
			if (!this.departmentOfTheTreasury.IsTransferOfResourcePossible(base.Empire, order.DissentionCost[i].ResourceName, ref num))
			{
				return false;
			}
		}
		order.NumberOfUnitsToSpawnInGarrison = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<int>("Gameplay/MinorEmpire/GarrisonSizeOnDissent", 2);
		if (order.NumberOfUnitsToSpawnInGarrison != 0)
		{
			order.GameEntityGUIDs = new GameEntityGUID[order.NumberOfUnitsToSpawnInGarrison];
			for (int j = 0; j < order.NumberOfUnitsToSpawnInGarrison; j++)
			{
				order.GameEntityGUIDs[j] = this.GameEntityRepositoryService.GenerateGUID();
			}
			order.UnitDesignName = agency.GetUpToDateDesignName();
			order.UnitLevel = agency.GetCurrentUnitLevel();
			if (string.IsNullOrEmpty(order.UnitDesignName))
			{
				return false;
			}
		}
		return true;
	}

	private IEnumerator DissentVillageProcessor(OrderDissentVillage order)
	{
		Region region = this.WorldPositionningService.GetRegion(order.VillageWorldPosition);
		if (region == null || region.MinorEmpire == null)
		{
			yield break;
		}
		BarbarianCouncil barbarianCouncil = region.MinorEmpire.GetAgency<BarbarianCouncil>();
		if (barbarianCouncil == null)
		{
			yield break;
		}
		Village village = barbarianCouncil.GetVillageAt(order.VillageWorldPosition);
		if (village == null)
		{
			yield break;
		}
		Diagnostics.Assert(village.PointOfInterest != null);
		if (village.PointOfInterest.PointOfInterestImprovement == null)
		{
			PointOfInterestImprovementDefinition pointOfInterestImprovementDefinition = null;
			ConstructibleElement[] constructibles = this.departmentOfIndustry.ConstructibleElementDatabase.GetAvailableConstructibleElements(new StaticString[0]);
			for (int index = 0; index < constructibles.Length; index++)
			{
				pointOfInterestImprovementDefinition = (constructibles[index] as PointOfInterestImprovementDefinition);
				if (pointOfInterestImprovementDefinition != null)
				{
					if (!(pointOfInterestImprovementDefinition.PointOfInterestTemplateName != village.PointOfInterest.PointOfInterestDefinition.PointOfInterestTemplateName))
					{
						this.BuildPointOfInterestImprovement(village.PointOfInterest, pointOfInterestImprovementDefinition);
						if (region.Owner != null)
						{
							DepartmentOfIndustry departmentOfIndustry = region.Owner.GetAgency<DepartmentOfIndustry>();
							if (departmentOfIndustry != null)
							{
								ConstructionQueue constructionQueue = departmentOfIndustry.GetConstructionQueue(region.City);
								if (constructionQueue != null && pointOfInterestImprovementDefinition != null)
								{
									for (int jndex = 0; jndex < constructionQueue.Length; jndex++)
									{
										Construction construction = constructionQueue.PeekAt(jndex);
										if (construction != null && construction.ConstructibleElementName == pointOfInterestImprovementDefinition.Name && construction.WorldPosition == village.WorldPosition)
										{
											if (construction.IsInProgress)
											{
												departmentOfIndustry.RemoveConstructionQueueDescriptors(region.City, construction);
											}
											this.GameEntityRepositoryService.Unregister(construction);
											constructionQueue.Remove(construction);
											break;
										}
									}
								}
							}
						}
						break;
					}
				}
			}
		}
		if (order.DissentionCost != null)
		{
			for (int index2 = 0; index2 < order.DissentionCost.Length; index2++)
			{
				if (order.DissentionCost[index2].Instant)
				{
					float resourceCost = order.DissentionCost[index2].GetValue(base.Empire);
					if (!this.departmentOfTheTreasury.TryTransferResources(base.Empire, order.DissentionCost[index2].ResourceName, -resourceCost))
					{
						Diagnostics.LogError("Cannot transfert the amount of resources (resource name = '{0}', cost = {0}).", new object[]
						{
							order.DissentionCost[index2].ResourceName,
							-resourceCost
						});
					}
				}
			}
		}
		barbarianCouncil.DissentPacifiedVillage(village, (global::Empire)base.Empire);
		if (order.NumberOfUnitsToSpawnInGarrison != 0)
		{
			DepartmentOfDefense departmentOfDefense = barbarianCouncil.MinorEmpire.GetAgency<DepartmentOfDefense>();
			UnitDesign unitDesign = departmentOfDefense.UnitDesignDatabase.GetAvailableUnitDesignsAsEnumerable().FirstOrDefault((UnitDesign iterator) => iterator.Name == order.UnitDesignName);
			if (unitDesign != null)
			{
				for (int index3 = 0; index3 < order.NumberOfUnitsToSpawnInGarrison; index3++)
				{
					Unit unit = DepartmentOfDefense.CreateUnitByDesign(order.GameEntityGUIDs[index3], unitDesign);
					if (unit != null)
					{
						unit.Level = order.UnitLevel;
						unit.Refresh(true);
						unit.UpdateExperienceReward(barbarianCouncil.MinorEmpire);
						unit.UpdateShiftingForm();
						village.AddUnit(unit);
					}
				}
			}
			village.Refresh(false);
		}
		if (village.Region.City != null)
		{
			Diagnostics.Assert(village.Region.City.Empire != null);
			Diagnostics.Assert(village.Region.City.Empire is MajorEmpire);
			DepartmentOfTheInterior departmentOfTheInterior = village.Region.City.Empire.GetAgency<DepartmentOfTheInterior>();
			Diagnostics.Assert(departmentOfTheInterior != null);
			departmentOfTheInterior.BindMinorFactionToCity(village.Region.City, village.Region.MinorEmpire);
			departmentOfTheInterior.VerifyOverallPopulation(village.Region.City);
			if (this.EventService != null)
			{
				EventVillageDissent eventVillageDissent = new EventVillageDissent(village.Region.City.Empire, village, base.Empire);
				this.EventService.Notify(eventVillageDissent);
			}
		}
		if (village.Region.City != null && village.Region.City.Empire.Index != base.Empire.Index)
		{
			this.VisibilityService.NotifyVisibilityHasChanged(village.Region.City.Empire);
		}
		if (village.PointOfInterest.ArmyPillaging.IsValid)
		{
			DepartmentOfDefense.StopPillage(village.PointOfInterest);
			yield break;
		}
		yield break;
	}

	private bool MovePopulationPreprocessor(OrderMovePopulation order)
	{
		City city = this.cities.Find((City match) => match.GUID == order.CityGUID);
		if (city == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the target city is not in the department.");
			return false;
		}
		if (order.FromPropertyName == order.ToPropertyName)
		{
			Diagnostics.LogError("Order preprocessing failed because the from property and the to property are the same.");
			return false;
		}
		if (order.MovedAmount <= 0)
		{
			Diagnostics.LogError("Order preprocessing failed because the target moving amount '{1}' is smaller or equal to 0.", new object[]
			{
				order.FromPropertyName,
				order.MovedAmount
			});
			return false;
		}
		int num = Mathf.RoundToInt(city.GetPropertyValue(order.FromPropertyName));
		if (num < order.MovedAmount)
		{
			Diagnostics.LogError("Order preprocessing failed because the target from property '{0}' is under the asked Amount={1}.", new object[]
			{
				order.FromPropertyName,
				order.MovedAmount
			});
			return false;
		}
		int num2 = Mathf.RoundToInt(city.GetPropertyValue(order.ToPropertyName));
		order.FinalFromValue = num - order.MovedAmount;
		order.FinalToValue = num2 + order.MovedAmount;
		return true;
	}

	private IEnumerator MovePopulationProcessor(OrderMovePopulation order)
	{
		City city = this.cities.Find((City match) => match.GUID == order.CityGUID);
		Diagnostics.Assert(city != null);
		city.SetPropertyBaseValue(order.FromPropertyName, (float)order.FinalFromValue);
		city.SetPropertyBaseValue(order.ToPropertyName, (float)order.FinalToValue);
		city.Refresh(false);
		if (this.PopulationRepartitionChanged != null)
		{
			this.PopulationRepartitionChanged(this, new PopulationRepartitionEventArgs(city));
		}
		yield break;
	}

	private bool ResettlePreprocessor(OrderResettle order)
	{
		Army army;
		if (!this.ColonizePreprocessor_ValidateOrder(order, UnitAbility.ReadonlyResettle, out army))
		{
			return false;
		}
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.SettlerGUID, out gameEntity))
		{
			Diagnostics.LogError("Wasn't able to find the settler (gameEntity#{0}) in GameEntityRepositoryService.", new object[]
			{
				order.SettlerGUID
			});
			return false;
		}
		Unit unit = gameEntity as Unit;
		if (unit == null)
		{
			Diagnostics.LogError("Wasn't able to cast the settler into a unit.");
			return false;
		}
		if (unit.SimulationObject.Tags.Contains(City.TagMainCity))
		{
			Diagnostics.Assert(this.MainCity == null);
			Diagnostics.Assert(this.MainCityGUID.IsValid);
			order.CityGUID = this.MainCityGUID;
		}
		float freeBoroughStock;
		if (!this.departmentOfTheTreasury.TryGetResourceStockValue(unit, DepartmentOfTheTreasury.Resources.FreeBorough, out freeBoroughStock, false))
		{
			Diagnostics.LogError("Wasn't able to retrieve the FreeBorough stock value.");
			return false;
		}
		order.FreeBoroughStock = freeBoroughStock;
		ResourceDefinition[] migrationCarriedResources = DepartmentOfTheTreasury.GetMigrationCarriedResources();
		order.FreeResources = new float[migrationCarriedResources.Length];
		for (int i = 0; i < migrationCarriedResources.Length; i++)
		{
			float num = 0f;
			this.departmentOfTheTreasury.TryGetResourceStockValue(unit, migrationCarriedResources[i].Name, out num, false);
			order.FreeResources[i] = num;
		}
		IEnumerable<StaticString> source = from match in order.FreeCityImprovements
		select match.FreeCityImprovementName;
		for (int j = 0; j < unit.CarriedCityImprovements.Count; j++)
		{
			StaticString staticString = unit.CarriedCityImprovements[j];
			if (!source.Contains(staticString))
			{
				OrderCreateCity.FreeCityImprovement item = default(OrderCreateCity.FreeCityImprovement);
				item.GameEntityGUID = this.GameEntityRepositoryService.GenerateGUID();
				item.FreeCityImprovementName = staticString;
				order.FreeCityImprovements.Add(item);
			}
		}
		OrderUpdateCadastralMap orderUpdateCadastralMap = new OrderUpdateCadastralMap(order.EmpireIndex);
		orderUpdateCadastralMap.CityGameEntityGUID = order.CityGUID;
		orderUpdateCadastralMap.Operation = CadastralMapOperation.Proxy;
		if (order.FreeCityImprovements != null)
		{
			for (int k = 0; k < order.FreeCityImprovements.Count; k++)
			{
				if (order.FreeCityImprovements[k].FreeCityImprovementName == "CityImprovementRoads")
				{
					orderUpdateCadastralMap.Operation = CadastralMapOperation.Connect;
					break;
				}
			}
		}
		orderUpdateCadastralMap.PathfindingMovementCapacity = PathfindingMovementCapacity.Ground;
		orderUpdateCadastralMap.WorldPosition = army.WorldPosition;
		((global::Empire)base.Empire).PlayerControllers.Server.PostOrder(orderUpdateCadastralMap);
		OrderUpdateMilitia order2 = new OrderUpdateMilitia(order.EmpireIndex, order.CityGUID);
		((global::Empire)base.Empire).PlayerControllers.Server.PostOrder(order2);
		return true;
	}

	private IEnumerator ResettleProcessor(OrderResettle order)
	{
		if (order.ArmyGuid == GameEntityGUID.Zero)
		{
			Diagnostics.LogError("Skipping colonization process because the army game entity guid is null.");
			yield break;
		}
		if (order.CityGUID == GameEntityGUID.Zero)
		{
			Diagnostics.LogError("Skipping colonization process because the city game entity guid is null.");
			yield break;
		}
		DepartmentOfDefense departmentOfDefense = base.Empire.GetAgency<DepartmentOfDefense>();
		Diagnostics.Assert(departmentOfDefense != null);
		if (departmentOfDefense.GetArmy(order.ArmyGuid) == null)
		{
			Diagnostics.LogError("Skipping colonization process because the army does not exists.");
			yield break;
		}
		IGameEntity settler;
		if (!this.GameEntityRepositoryService.TryGetValue(order.SettlerGUID, out settler))
		{
			Diagnostics.LogError("Wasn't able to find the settler (gameEntity#{0}) in GameEntityRepositoryService.", new object[]
			{
				order.SettlerGUID
			});
			yield break;
		}
		if (!(settler is Unit))
		{
			Diagnostics.LogError("Wasn't able to cast the settler into a unit.");
			yield break;
		}
		City city;
		if (this.ColonizeProcessor_CreateCity(order, out city))
		{
			if (order.FreeBoroughStock > 0f && order.FreeBoroughStock < 1f)
			{
				Diagnostics.LogError("FreeBoroughStock should have an integer (current value = {0}), we won't give any free borough to city#{1} '{2}' of empire#{3}.", new object[]
				{
					order.FreeBoroughStock,
					city.GUID,
					city.LocalizedName,
					base.Empire.Index
				});
				order.FreeBoroughStock = 0f;
			}
			if (!this.departmentOfTheTreasury.TryTransferResources(city, DepartmentOfTheTreasury.Resources.QueuedFreeBorough, order.FreeBoroughStock))
			{
				Diagnostics.LogError("Wasn't able to give queued free borough resource to the new city.");
				yield break;
			}
			ResourceDefinition[] migrationCarriedResources = DepartmentOfTheTreasury.GetMigrationCarriedResources();
			for (int index = 0; index < migrationCarriedResources.Length; index++)
			{
				if (!this.departmentOfTheTreasury.TryTransferResources(city, migrationCarriedResources[index].Name, order.FreeResources[index]))
				{
					Diagnostics.LogError("Wasn't able to give {0} to the new city.", new object[]
					{
						migrationCarriedResources[index].Name
					});
					yield break;
				}
			}
			this.ComputeCityPopulation(city, false);
			this.EventService.Notify(new EventColonize(base.Empire, city));
		}
		yield break;
	}

	private bool SacrificePopulationPreprocessor(OrderSacrificePopulation order)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			return false;
		}
		if (!(gameEntity is City))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a City.");
			return false;
		}
		IBattleEncounterRepositoryService service = this.GameService.Game.Services.GetService<IBattleEncounterRepositoryService>();
		if (service != null)
		{
			IEnumerable<BattleEncounter> enumerable = service;
			if (enumerable != null)
			{
				bool flag = enumerable.Any((BattleEncounter encounter) => encounter.IsGarrisonInEncounter(gameEntity.GUID));
				if (flag)
				{
					Diagnostics.LogWarning("Order preprocessing failed because the attacker already in combat ");
					return false;
				}
			}
		}
		City city = gameEntity as City;
		float propertyValue = city.GetPropertyValue(SimulationProperties.Population);
		float growthNeeded = 0f;
		if (!DepartmentOfTheInterior.CanSacrificePopulation(city, ref growthNeeded))
		{
			return false;
		}
		order.GrowthNeeded = growthNeeded;
		global::Empire empire = base.Empire as global::Empire;
		if (empire == null)
		{
			return false;
		}
		OrderBuyoutAndActivateBooster orderBuyoutAndActivateBooster = new OrderBuyoutAndActivateBooster(order.EmpireIndex, "BoosterSacrificePopulation", 0UL, false);
		if (Booster.GetTargetType("BoosterSacrificePopulation") == BoosterDefinition.TargetType.City)
		{
			orderBuyoutAndActivateBooster.TargetGUID = city.GUID;
		}
		orderBuyoutAndActivateBooster.Duration = Mathf.RoundToInt(propertyValue);
		empire.PlayerControllers.Server.PostOrder(orderBuyoutAndActivateBooster);
		return true;
	}

	private IEnumerator SacrificePopulationProcessor(OrderSacrificePopulation order)
	{
		if (order.CityGuid == GameEntityGUID.Zero)
		{
			Diagnostics.LogError("Skipping city extension process because the game entity guid is null.");
			yield break;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			yield break;
		}
		if (!(gameEntity is City))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a City.");
			yield break;
		}
		City city = gameEntity as City;
		if (!this.departmentOfTheTreasury.TryTransferResources(city, DepartmentOfTheTreasury.Resources.CityGrowth, order.GrowthNeeded))
		{
			Diagnostics.LogError("Order preprocessing failed because the city has not enough growth.");
			yield break;
		}
		DepartmentOfTheInterior departmentOfTheInterior = base.Empire.GetAgency<DepartmentOfTheInterior>();
		departmentOfTheInterior.ComputeCityPopulation(city, false);
		city.Empire.SetPropertyBaseValue(SimulationProperties.PopulationSacrificeCooldown, city.Empire.GetPropertyValue(SimulationProperties.MaximumPopulationSacrificeCooldown));
		if (this.PopulationRepartitionChanged != null)
		{
			this.PopulationRepartitionChanged(this, new PopulationRepartitionEventArgs(city));
		}
		yield break;
	}

	internal UnitDesign FindReleventUnitDesign(Village village)
	{
		DepartmentOfDefense agency = village.Converter.GetAgency<DepartmentOfDefense>();
		Diagnostics.Assert(agency != null);
		Diagnostics.Assert(this.departmentOfScience != null);
		StaticString eraNumber = "Era" + this.departmentOfScience.CurrentTechnologyEraNumber.ToString();
		UnitDesign unitDesign = ((IUnitDesignDatabase)agency).GetDatabaseCompatibleUnitDesignsAsEnumerable().FirstOrDefault((UnitDesign ud) => ud.Tags.Contains(DepartmentOfTheInterior.TagConvertedVillageUnit) && ud.Tags.Contains(eraNumber) && ud.Tags.Contains(village.MinorEmpire.MinorFaction.Name));
		if (unitDesign == null)
		{
			Diagnostics.LogWarning("Cannot find converted village unit to spawn in {0} with Minor Faction {1} for {2}", new object[]
			{
				eraNumber,
				village.MinorEmpire.MinorFaction.Name,
				village.Converter
			});
			return ((IUnitDesignDatabase)agency).GetDatabaseCompatibleUnitDesignsAsEnumerable().FirstOrDefault((UnitDesign ud) => ud.Name == DepartmentOfTheInterior.DefaultConvertedUnitDescriptorName);
		}
		return unitDesign;
	}

	private bool SpawnConvertedVillageUnitPreprocessor(OrderSpawnConvertedVillageUnit order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.VillageGUID, out gameEntity) || !(gameEntity is Village))
		{
			Diagnostics.LogError("Cannot find the village where to spawn new converted unit.");
			return false;
		}
		Village village = gameEntity as Village;
		if (village == null)
		{
			return false;
		}
		UnitDesign unitDesign = this.FindReleventUnitDesign(village);
		if (unitDesign == null)
		{
			return false;
		}
		order.GameEntityGUIDs = new GameEntityGUID[order.UnitsToCreateCount];
		for (int i = 0; i < order.UnitsToCreateCount; i++)
		{
			order.GameEntityGUIDs[i] = this.GameEntityRepositoryService.GenerateGUID();
		}
		village.ConvertedUnitSpawnTurn = (this.GameService.Game as global::Game).Turn + village.GetConvertedUnitProductionTimer();
		order.OutsideUnitsToCreateCount = Mathf.Max(village.UnitsCount + order.UnitsToCreateCount - village.MaximumUnitSlot, 0);
		int xp = (int)village.Converter.GetPropertyValue(SimulationProperties.UnitExperienceRewardAtCreation);
		if (order.OutsideUnitsToCreateCount != 0)
		{
			for (int j = 0; j < order.OutsideUnitsToCreateCount; j++)
			{
				List<WorldPosition> list = DepartmentOfDefense.GetAvailablePositionsForArmyCreation(village);
				list = (from pos in list
				where this.departmentOfForeignAffairs.CanMoveOn(pos, false, false)
				select pos).ToList<WorldPosition>();
				if (list.Count <= 0)
				{
					Diagnostics.Log("Cannot find a spot to spawn converted unit for " + village.Name);
					return false;
				}
				Diagnostics.Assert(order.GameEntityGUIDs[j].IsValid);
				OrderSpawnArmy order2 = new OrderSpawnArmy(village.Converter.Index, list[0], this.departmentOfScience.CurrentTechnologyEraNumber - 1, xp, new StaticString[]
				{
					unitDesign.Name
				});
				((global::Empire)base.Empire).PlayerControllers.Server.PostOrder(order2);
			}
		}
		return true;
	}

	private IEnumerator SpawnConvertedVillageUnitProcessor(OrderSpawnConvertedVillageUnit order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.VillageGUID, out gameEntity) || !(gameEntity is Village))
		{
			Diagnostics.LogError("Cannot find the village where to spawn new converted unit.");
			yield break;
		}
		if (order.GameEntityGUIDs.Length - order.OutsideUnitsToCreateCount > 0)
		{
			Village village = gameEntity as Village;
			village.ConvertedUnitSpawnTurn = (this.GameService.Game as global::Game).Turn + village.GetConvertedUnitProductionTimer();
			UnitDesign unitDesign = this.FindReleventUnitDesign(village);
			int bonusXP = (int)village.Converter.GetPropertyValue(SimulationProperties.UnitExperienceRewardAtCreation);
			for (int index = 0; index < order.GameEntityGUIDs.Length - order.OutsideUnitsToCreateCount; index++)
			{
				Diagnostics.Assert(order.GameEntityGUIDs[index + order.OutsideUnitsToCreateCount].IsValid);
				Unit unit = DepartmentOfDefense.CreateUnitByDesign(order.GameEntityGUIDs[index + order.OutsideUnitsToCreateCount], unitDesign);
				if (unit == null)
				{
					Diagnostics.LogError("Converted village unit creation failed");
					yield break;
				}
				this.GameEntityRepositoryService.Register(unit);
				village.AddUnit(unit);
				unit.Level = this.departmentOfScience.CurrentTechnologyEraNumber - 1;
				unit.GainXp((float)bonusXP, true, true);
				if (!unit.SimulationObject.Tags.Contains(DepartmentOfTheInterior.TagConvertedVillageUnit))
				{
					unit.SimulationObject.Tags.AddTag(DepartmentOfTheInterior.TagConvertedVillageUnit);
				}
				village.AddChild(unit);
				village.Refresh(false);
				unit.Refresh(false);
				unit.UpdateExperienceReward(base.Empire);
				this.EventService.Notify(new EventVillageUnitSpawned(village.Converter, unit));
			}
		}
		yield break;
	}

	private bool SwapCityOwnerPreprocessor(OrderSwapCityOwner order)
	{
		global::Game game = this.GameService.Game as global::Game;
		if (order.NewOwnerIndex < 0 || order.NewOwnerIndex >= game.Empires.Length)
		{
			Diagnostics.LogError("Order preprocessing failed because the new owner empire index is out of bounds.");
			return false;
		}
		City city = null;
		if (!this.GameEntityRepositoryService.TryGetValue<City>(order.CityGUID, out city))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a City.");
			return false;
		}
		if (city.Camp != null)
		{
			order.CampTransferData = this.SwapCityOwnerProcessor_CreateCampTransferData(order, city.Camp);
		}
		return true;
	}

	private IEnumerator SwapCityOwnerProcessor(OrderSwapCityOwner order)
	{
		if (order.CityGUID == GameEntityGUID.Zero)
		{
			Diagnostics.LogError("Skipping city extension process because the game entity guid is null.");
			yield break;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			yield break;
		}
		if (!(gameEntity is City))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a City.");
			yield break;
		}
		City city = gameEntity as City;
		int oldOwnerEmpireIndex = city.Empire.Index;
		global::Game game = this.GameService.Game as global::Game;
		this.SwapCityOwner(city, game.Empires[order.NewOwnerIndex]);
		Diagnostics.Assert(this.SimulationDescriptorDatabase != null);
		CityImprovementDefinition newCityImprovementDefinition = null;
		DepartmentOfIndustry departmentOfIndustry = base.Empire.GetAgency<DepartmentOfIndustry>();
		Diagnostics.Assert(departmentOfIndustry != null);
		IEnumerable<DepartmentOfIndustry.ConstructibleElement> constructibleElements = departmentOfIndustry.ConstructibleElementDatabase.GetAvailableConstructibleElementsAsEnumerable(new StaticString[0]);
		foreach (DepartmentOfIndustry.ConstructibleElement constructibleElement in constructibleElements)
		{
			if (constructibleElement.Category == CityImprovementDefinition.ReadOnlyCategory && constructibleElement.SubCategory == CityImprovementDefinition.SubCategoryCityHall && constructibleElement.Tags.Contains(ConstructibleElement.TagFree))
			{
				bool checkConstructiblePrerequisites = DepartmentOfTheTreasury.CheckConstructiblePrerequisites(city.Empire, constructibleElement, new string[0]);
				if (checkConstructiblePrerequisites)
				{
					newCityImprovementDefinition = (constructibleElement as CityImprovementDefinition);
					break;
				}
			}
		}
		if (newCityImprovementDefinition != null)
		{
			StaticString typeOfCityImprovementCityHall = new StaticString("CityImprovementCityHall");
			int index = 0;
			while (index < city.CityImprovements.Count)
			{
				SimulationDescriptor cityImprovementCityHallDescriptor = city.CityImprovements[index].SimulationObject.GetDescriptorFromType(typeOfCityImprovementCityHall);
				if (cityImprovementCityHallDescriptor != null)
				{
					if (newCityImprovementDefinition.Name == city.CityImprovements[index].CityImprovementDefinition.Name)
					{
						break;
					}
					city.CityImprovements[index].SimulationObject.RemoveAllDescriptors();
					city.CityImprovements[index].CityImprovementDefinition = newCityImprovementDefinition;
					SimulationDescriptor[] newConstructibleElementDescriptors = null;
					newConstructibleElementDescriptors = SimulationDescriptorReference.GetSimulationDescriptorsFromXmlReferences(newCityImprovementDefinition.SimulationDescriptorReferences, ref newConstructibleElementDescriptors);
					foreach (SimulationDescriptor descriptor in newConstructibleElementDescriptors)
					{
						city.CityImprovements[index].SimulationObject.AddDescriptor(descriptor);
					}
					break;
				}
				else
				{
					index++;
				}
			}
		}
		this.VerifyOverallPopulation(city);
		if (city.Camp != null)
		{
			this.SwapCityOwnerProcessor_SwapCamp(order, city.Camp);
		}
		this.EventService.Notify(new EventSwapCity(base.Empire, city, oldOwnerEmpireIndex, order.NewOwnerIndex, true));
		this.EventService.Notify(new EventSwapCity(game.Empires[order.NewOwnerIndex], city, oldOwnerEmpireIndex, order.NewOwnerIndex, true));
		yield break;
	}

	private OrderCreateDistrictImprovement.CampGarrisonTransferData[] SwapCityOwnerProcessor_CreateCampTransferData(OrderSwapCityOwner order, Camp camp)
	{
		if (camp == null)
		{
			Diagnostics.LogError("CreateCampTransferData failed because camp is not valid.");
			return null;
		}
		List<OrderCreateDistrictImprovement.CampGarrisonTransferData> list = new List<OrderCreateDistrictImprovement.CampGarrisonTransferData>();
		if (camp.StandardUnits.Count > 0)
		{
			GameEntityGUID[] array = new GameEntityGUID[camp.StandardUnits.Count];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = camp.StandardUnits[i].GUID;
			}
			int num = (int)base.Empire.GetPropertyValue(SimulationProperties.ArmyUnitSlot);
			if (num <= 0)
			{
				Diagnostics.LogWarning("The maximum number Of units per army doesn't allow for transfer.");
				return null;
			}
			int num2;
			for (int j = 0; j < array.Length; j += num2)
			{
				num2 = array.Length - j;
				if (num2 > num)
				{
					num2 = num;
				}
				int offset = 0;
				int maximumNumberOfGrowths = (int)(DepartmentOfTheInterior.Game.World.WorldParameters.Columns / 2);
				List<WorldPosition> list2 = new List<WorldPosition>();
				for (int k = 0; k < camp.City.Districts.Count; k++)
				{
					District district = camp.City.Districts[k];
					if (district.Type != DistrictType.Exploitation)
					{
						list2.Add(district.WorldPosition);
					}
				}
				Unit pathfindingContextProvider = camp.StandardUnits[j];
				Army armyAtPosition = this.WorldPositionningService.GetArmyAtPosition(camp.WorldPosition);
				WorldPosition worldPosition = this.SwapCityOwnerProcessor_TryToGetValidPosition(camp.WorldPosition, offset, maximumNumberOfGrowths, pathfindingContextProvider, list2);
				if (!worldPosition.IsValid)
				{
					break;
				}
				list2.Add(worldPosition);
				OrderCreateDistrictImprovement.CampGarrisonTransferData item = default(OrderCreateDistrictImprovement.CampGarrisonTransferData);
				item.ArmyGUID = this.GameEntityRepositoryService.GenerateGUID();
				item.UnitsGUIDs = new GameEntityGUID[num2];
				Array.Copy(array, j, item.UnitsGUIDs, 0, num2);
				item.WorldPosition = worldPosition;
				if (armyAtPosition != null)
				{
					item.WorldPositionToArmy = this.SwapCityOwnerProcessor_TryToGetValidPosition(camp.WorldPosition, offset, maximumNumberOfGrowths, armyAtPosition, list2);
				}
				else
				{
					item.WorldPositionToArmy = WorldPosition.Invalid;
				}
				list.Add(item);
			}
		}
		return list.ToArray();
	}

	private WorldPosition SwapCityOwnerProcessor_TryToGetValidPosition(WorldPosition worldPosition, int offset, int maximumNumberOfGrowths, IPathfindingContextProvider pathfindingContextProvider, List<WorldPosition> excludedWorldPositions)
	{
		WorldPosition worldPosition2 = WorldPosition.Invalid;
		WorldArea worldArea = new WorldArea(new WorldPosition[]
		{
			worldPosition
		});
		IPathfindingService service = DepartmentOfTheInterior.Game.Services.GetService<IPathfindingService>();
		GridMap<Army> gridMap = (!(DepartmentOfTheInterior.Game != null)) ? null : (DepartmentOfTheInterior.Game.World.Atlas.GetMap(WorldAtlas.Maps.Armies) as GridMap<Army>);
		for (;;)
		{
			if (offset >= worldArea.WorldPositions.Count)
			{
				if (maximumNumberOfGrowths-- > 0)
				{
					worldArea = worldArea.Grow(this.WorldPositionningService.World.WorldParameters);
				}
				if (offset >= worldArea.WorldPositions.Count)
				{
					break;
				}
			}
			worldPosition2 = worldArea.WorldPositions[offset++];
			bool flag = worldPosition2.IsValid;
			if (!flag || gridMap == null)
			{
				goto IL_FB;
			}
			Army value = gridMap.GetValue(worldPosition2);
			if (value == null)
			{
				goto IL_FB;
			}
			flag = false;
			IL_147:
			if (flag)
			{
				return worldPosition2;
			}
			continue;
			IL_FB:
			if (flag && service != null && pathfindingContextProvider != null)
			{
				bool flag2 = service.IsTileStopableAndPassable(worldPosition2, pathfindingContextProvider, PathfindingFlags.IgnoreFogOfWar, null);
				if (!flag2 || worldPosition == worldPosition2)
				{
					flag = false;
				}
			}
			if (excludedWorldPositions != null && excludedWorldPositions.Contains(worldPosition2))
			{
				flag = false;
				goto IL_147;
			}
			goto IL_147;
		}
		worldPosition2 = WorldPosition.Invalid;
		return worldPosition2;
	}

	private void SwapCityOwnerProcessor_SwapCamp(OrderSwapCityOwner order, Camp camp)
	{
		if (camp == null)
		{
			Diagnostics.LogError("SwapCityOwnerProcessor_SwapCamp fail because camp is not valid.");
			return;
		}
		IPathfindingService service = DepartmentOfTheInterior.Game.Services.GetService<IPathfindingService>();
		DepartmentOfDefense agency = base.Empire.GetAgency<DepartmentOfDefense>();
		for (int i = 0; i < order.CampTransferData.Length; i++)
		{
			OrderCreateDistrictImprovement.CampGarrisonTransferData campGarrisonTransferData = order.CampTransferData[i];
			Army army = null;
			agency.CreateArmy(campGarrisonTransferData.ArmyGUID, null, campGarrisonTransferData.WorldPosition, out army, false, true);
			Unit unit = null;
			for (int j = 0; j < campGarrisonTransferData.UnitsGUIDs.Length; j++)
			{
				if (!this.GameEntityRepositoryService.TryGetValue<Unit>(campGarrisonTransferData.UnitsGUIDs[j], out unit))
				{
					Diagnostics.LogError("Could not found unit with GUID {0}.", new object[]
					{
						campGarrisonTransferData.UnitsGUIDs[j]
					});
				}
				else
				{
					if (camp.WorldPosition != campGarrisonTransferData.WorldPosition && !unit.UnitDesign.Tags.Contains(DownloadableContent9.TagSolitary))
					{
						float transitionCost = service.GetTransitionCost(camp.WorldPosition, campGarrisonTransferData.WorldPosition, unit, PathfindingFlags.IgnoreFogOfWar, null);
						float maximumMovementPoints = service.GetMaximumMovementPoints(campGarrisonTransferData.WorldPosition, unit, PathfindingFlags.IgnoreFogOfWar);
						float num = (maximumMovementPoints <= 0f) ? float.PositiveInfinity : (transitionCost / maximumMovementPoints);
						if (float.IsPositiveInfinity(num))
						{
							unit.SetPropertyBaseValue(SimulationProperties.MovementRatio, 0f);
						}
						else
						{
							float propertyValue = unit.GetPropertyValue(SimulationProperties.MovementRatio);
							unit.SetPropertyBaseValue(SimulationProperties.MovementRatio, propertyValue - num);
						}
					}
					SimulationDescriptor descriptor;
					if (army.SimulationObject.Tags.Contains(PathfindingContext.MovementCapacitySailDescriptor) && !unit.SimulationObject.Tags.Contains(PathfindingContext.MovementCapacitySailDescriptor) && this.SimulationDescriptorDatabase != null && this.SimulationDescriptorDatabase.TryGetValue(PathfindingContext.MovementCapacitySailDescriptor, out descriptor))
					{
						unit.AddDescriptor(descriptor, true);
					}
					if (!unit.SimulationObject.Tags.Contains(DownloadableContent16.SeafaringUnit))
					{
						unit.SwitchToEmbarkedUnit(service.GetTileMovementCapacity(army.WorldPosition, (PathfindingFlags)0) == PathfindingMovementCapacity.Water);
					}
					if (unit != camp.Hero)
					{
						camp.RemoveUnit(unit);
						army.AddUnit(unit);
					}
					else
					{
						camp.SetHero(null);
						army.SetHero(unit);
					}
				}
			}
			army.Refresh(false);
			this.GameEntityRepositoryService.Register(army);
			IDownloadableContentService service2 = Services.GetService<IDownloadableContentService>();
			agency.UpdateDetection(army);
			IOrbService service3 = this.GameService.Game.Services.GetService<IOrbService>();
			service3.CollectOrbsAtPosition(army.WorldPosition, army, base.Empire as global::Empire);
			if (service.GetTileMovementCapacity(army.WorldPosition, (PathfindingFlags)0) == PathfindingMovementCapacity.Water)
			{
				army.SetSails();
			}
			if (service2.IsShared(DownloadableContent19.ReadOnlyName))
			{
				agency.CheckArmiesOnMapBoost();
			}
			if (campGarrisonTransferData.WorldPositionToArmy.IsValid)
			{
				Army armyAtPosition = this.WorldPositionningService.GetArmyAtPosition(camp.WorldPosition);
				if (armyAtPosition != null)
				{
					armyAtPosition.SetWorldPositionAndTeleport(campGarrisonTransferData.WorldPositionToArmy, true);
				}
			}
		}
		camp.Empire = (this.GameService.Game as global::Game).Empires[order.NewOwnerIndex];
	}

	private bool SwapFortressOccupantPreprocessor(OrderSwapFortressOccupant order)
	{
		global::Game game = this.GameService.Game as global::Game;
		if (order.NewOwnerIndex < 0 || order.NewOwnerIndex >= game.Empires.Length)
		{
			Diagnostics.LogError("Order preprocessing failed because the new owner empire index is out of bounds.");
			return false;
		}
		return true;
	}

	private IEnumerator SwapFortressOccupantProcessor(OrderSwapFortressOccupant order)
	{
		if (order.FortressGUID == GameEntityGUID.Zero)
		{
			Diagnostics.LogError("Skipping fortress occupation process because the game entity guid is null.");
			yield break;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.FortressGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			yield break;
		}
		Fortress fortress = gameEntity as Fortress;
		if (fortress == null)
		{
			PointOfInterest pointOfInterest = gameEntity as PointOfInterest;
			if (pointOfInterest != null && pointOfInterest.Region != null && pointOfInterest.Region.NavalEmpire != null)
			{
				PirateCouncil council = pointOfInterest.Region.NavalEmpire.GetAgency<PirateCouncil>();
				fortress = council.GetFortressAt(pointOfInterest.WorldPosition);
			}
		}
		if (fortress == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the target fortress is not valid.");
			yield break;
		}
		global::Game game = this.GameService.Game as global::Game;
		this.SwapFortressOccupant(fortress, game.Empires[order.NewOwnerIndex], new object[0]);
		yield break;
	}

	private bool TameKaijuPreprocessor(OrderTameKaiju order)
	{
		if (!this.MainCityGUID.IsValid)
		{
			Diagnostics.LogWarning("Order preprocessing failed because the main city is null.");
			return false;
		}
		if (!order.KaijuGUID.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the instigator guid is not valid.");
			return false;
		}
		Kaiju kaiju = null;
		if (!this.GameEntityRepositoryService.TryGetValue<Kaiju>(order.KaijuGUID, out kaiju))
		{
			Diagnostics.LogError("Order preprocessing failed because the entity is not referenced (guid: {0:X8}).", new object[]
			{
				order.KaijuGUID
			});
			return false;
		}
		Army army = null;
		if (order.ArmyInstigatorGUID.IsValid)
		{
			this.GameEntityRepositoryService.TryGetValue<Army>(order.ArmyInstigatorGUID, out army);
		}
		ArmyAction armyAction = null;
		IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
		if (database == null || !database.TryGetValue(ArmyAction_TameKaiju.ReadOnlyName, out armyAction))
		{
			return false;
		}
		KaijuCouncil agency = kaiju.KaijuEmpire.GetAgency<KaijuCouncil>();
		if (agency == null)
		{
			Diagnostics.LogWarning("Invalid null Kaiju council on the region.");
			return false;
		}
		if (agency.Kaiju != kaiju)
		{
			Diagnostics.LogWarning("Invalid null Kaiju in Kaiju Council.");
			return false;
		}
		if (army != null)
		{
			order.TameCost = armyAction.ComputeConstructionCost(army.Empire).ToArray();
			for (int i = 0; i < order.TameCost.Length; i++)
			{
				float num = -order.TameCost[i].GetValue(base.Empire.SimulationObject);
				if (!this.departmentOfTheTreasury.IsTransferOfResourcePossible(base.Empire, order.TameCost[i].ResourceName, ref num))
				{
					return false;
				}
			}
			order.NumberOfActionPointsToSpend = (double)armyAction.GetCostInActionPoints();
			if (order.NumberOfActionPointsToSpend > 0.0)
			{
				SimulationObjectWrapper simulationObjectWrapper = army;
				if (simulationObjectWrapper != null)
				{
					float propertyValue = simulationObjectWrapper.GetPropertyValue(SimulationProperties.MaximumNumberOfActionPoints);
					float propertyValue2 = simulationObjectWrapper.GetPropertyValue(SimulationProperties.ActionPointsSpent);
					if (order.NumberOfActionPointsToSpend > (double)(propertyValue - propertyValue2))
					{
						Diagnostics.LogWarning("Not enough action points.");
						return false;
					}
				}
			}
		}
		return true;
	}

	private IEnumerator TameKaijuProcessor(OrderTameKaiju order)
	{
		Kaiju kaiju = null;
		if (!this.GameEntityRepositoryService.TryGetValue<Kaiju>(order.KaijuGUID, out kaiju))
		{
			yield break;
		}
		if (kaiju.MajorEmpire != null)
		{
			yield break;
		}
		if (kaiju.KaijuGarrison == null)
		{
			yield break;
		}
		KaijuCouncil kaijuCouncil = kaiju.KaijuEmpire.GetAgency<KaijuCouncil>();
		if (kaijuCouncil == null)
		{
			yield break;
		}
		if (!(base.Empire is MajorEmpire))
		{
			yield break;
		}
		if (kaijuCouncil.Kaiju == null)
		{
			yield break;
		}
		Army instigator = null;
		if (order.ArmyInstigatorGUID.IsValid)
		{
			this.GameEntityRepositoryService.TryGetValue<Army>(order.ArmyInstigatorGUID, out instigator);
		}
		if (instigator != null)
		{
			if (order.NumberOfActionPointsToSpend > 0.0)
			{
				ArmyAction.SpendSomeNumberOfActionPoints(instigator, (float)order.NumberOfActionPointsToSpend);
			}
			for (int index = 0; index < order.TameCost.Length; index++)
			{
				if (order.TameCost[index].Instant)
				{
					float resourceCost = order.TameCost[index].GetValue(instigator.Empire);
					if (!this.departmentOfTheTreasury.TryTransferResources(instigator.Empire, order.TameCost[index].ResourceName, -resourceCost))
					{
						Diagnostics.LogError("Cannot transfert the amount of resources (resource name = '{0}', cost = {0}).", new object[]
						{
							order.TameCost[index].ResourceName,
							-resourceCost
						});
					}
				}
			}
		}
		global::Empire lastConverter = kaiju.MajorEmpire;
		kaijuCouncil.MajorEmpireTameKaiju((MajorEmpire)base.Empire, false);
		if (instigator != null)
		{
			instigator.Refresh(false);
		}
		kaiju.RefreshSharedSight();
		this.VisibilityService.NotifyVisibilityHasChanged((global::Empire)base.Empire);
		this.VisibilityService.NotifyVisibilityHasChanged((MajorEmpire)base.Empire);
		if (lastConverter != null)
		{
			this.VisibilityService.NotifyVisibilityHasChanged(lastConverter);
		}
		DepartmentOfTheInterior.GenerateResourcesLeechingForTamedKaijus(kaiju);
		base.Empire.Refresh(false);
		IGuiService guiService = Services.GetService<IGuiService>();
		ArmyActionModalPanel armyActionModalPanel = guiService.GetGuiPanel<ArmyActionModalPanel>();
		if (armyActionModalPanel != null && armyActionModalPanel.IsVisible)
		{
			armyActionModalPanel.RefreshContent();
		}
		yield break;
	}

	private bool TameUnstunnedKaijuPreprocessor(OrderTameUnstunnedKaiju order)
	{
		if (!this.MainCityGUID.IsValid)
		{
			Diagnostics.LogWarning("Order preprocessing failed because the main city is null.");
			return false;
		}
		if (!order.KaijuGUID.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the instigator guid is not valid.");
			return false;
		}
		Kaiju kaiju = null;
		if (!this.GameEntityRepositoryService.TryGetValue<Kaiju>(order.KaijuGUID, out kaiju))
		{
			Diagnostics.LogError("Order preprocessing failed because the entity is not referenced (guid: {0:X8}).", new object[]
			{
				order.KaijuGUID
			});
			return false;
		}
		Army army = null;
		if (order.ArmyInstigatorGUID.IsValid)
		{
			this.GameEntityRepositoryService.TryGetValue<Army>(order.ArmyInstigatorGUID, out army);
		}
		ArmyAction armyAction = null;
		IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
		if (database == null || !database.TryGetValue(ArmyAction_TameUnstunnedKaiju.ReadOnlyName, out armyAction))
		{
			return false;
		}
		KaijuCouncil agency = kaiju.KaijuEmpire.GetAgency<KaijuCouncil>();
		if (agency == null)
		{
			Diagnostics.LogWarning("Invalid null Kaiju council on the region.");
			return false;
		}
		if (agency.Kaiju != kaiju)
		{
			Diagnostics.LogWarning("Invalid null Kaiju in Kaiju Council.");
			return false;
		}
		if (army != null)
		{
			order.TameCost = armyAction.ComputeConstructionCost(army.Empire).ToArray();
			for (int i = 0; i < order.TameCost.Length; i++)
			{
				float num = -order.TameCost[i].GetValue(base.Empire.SimulationObject);
				if (!this.departmentOfTheTreasury.IsTransferOfResourcePossible(base.Empire, order.TameCost[i].ResourceName, ref num))
				{
					return false;
				}
			}
			order.NumberOfActionPointsToSpend = (double)armyAction.GetCostInActionPoints();
			if (order.NumberOfActionPointsToSpend > 0.0)
			{
				SimulationObjectWrapper simulationObjectWrapper = army;
				if (simulationObjectWrapper != null)
				{
					float propertyValue = simulationObjectWrapper.GetPropertyValue(SimulationProperties.MaximumNumberOfActionPoints);
					float propertyValue2 = simulationObjectWrapper.GetPropertyValue(SimulationProperties.ActionPointsSpent);
					if (order.NumberOfActionPointsToSpend > (double)(propertyValue - propertyValue2))
					{
						Diagnostics.LogWarning("Not enough action points.");
						return false;
					}
				}
			}
		}
		return true;
	}

	private IEnumerator TameUnstunnedKaijuProcessor(OrderTameUnstunnedKaiju order)
	{
		Kaiju kaiju = null;
		if (!this.GameEntityRepositoryService.TryGetValue<Kaiju>(order.KaijuGUID, out kaiju))
		{
			yield break;
		}
		if (kaiju.MajorEmpire != null)
		{
			yield break;
		}
		if (kaiju.KaijuGarrison == null)
		{
			yield break;
		}
		KaijuCouncil kaijuCouncil = kaiju.KaijuEmpire.GetAgency<KaijuCouncil>();
		if (kaijuCouncil == null)
		{
			yield break;
		}
		if (!(base.Empire is MajorEmpire))
		{
			yield break;
		}
		if (kaijuCouncil.Kaiju == null)
		{
			yield break;
		}
		Army instigator = null;
		if (order.ArmyInstigatorGUID.IsValid)
		{
			this.GameEntityRepositoryService.TryGetValue<Army>(order.ArmyInstigatorGUID, out instigator);
		}
		if (instigator != null)
		{
			if (order.NumberOfActionPointsToSpend > 0.0)
			{
				ArmyAction.SpendSomeNumberOfActionPoints(instigator, (float)order.NumberOfActionPointsToSpend);
			}
			for (int index = 0; index < order.TameCost.Length; index++)
			{
				if (order.TameCost[index].Instant)
				{
					float resourceCost = order.TameCost[index].GetValue(instigator.Empire);
					if (!this.departmentOfTheTreasury.TryTransferResources(instigator.Empire, order.TameCost[index].ResourceName, -resourceCost))
					{
						Diagnostics.LogError("Cannot transfert the amount of resources (resource name = '{0}', cost = {0}).", new object[]
						{
							order.TameCost[index].ResourceName,
							-resourceCost
						});
					}
				}
			}
		}
		global::Empire lastConverter = kaiju.MajorEmpire;
		kaijuCouncil.MajorEmpireTameKaiju((MajorEmpire)base.Empire, false);
		if (instigator != null)
		{
			instigator.Refresh(false);
		}
		kaiju.RefreshSharedSight();
		this.VisibilityService.NotifyVisibilityHasChanged((global::Empire)base.Empire);
		this.VisibilityService.NotifyVisibilityHasChanged((MajorEmpire)base.Empire);
		if (lastConverter != null)
		{
			this.VisibilityService.NotifyVisibilityHasChanged(lastConverter);
		}
		DepartmentOfTheInterior.GenerateResourcesLeechingForTamedKaijus(kaiju);
		base.Empire.Refresh(false);
		IGuiService guiService = Services.GetService<IGuiService>();
		ArmyActionModalPanel armyActionModalPanel = guiService.GetGuiPanel<ArmyActionModalPanel>();
		if (armyActionModalPanel != null && armyActionModalPanel.IsVisible)
		{
			armyActionModalPanel.RefreshContent();
		}
		yield break;
	}

	private bool ToggleRoundUpPreprocessor(OrderToggleRoundUp order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (!order.CityGUID.IsValid)
		{
			Diagnostics.LogError("InfiltratedCityGUID can't be invalid.");
			return false;
		}
		IGameEntity gameEntity;
		return this.GameEntityRepositoryService.TryGetValue(order.CityGUID, out gameEntity) && gameEntity is City;
	}

	private IEnumerator ToggleRoundUpProcessor(OrderToggleRoundUp order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (!order.CityGUID.IsValid)
		{
			Diagnostics.LogError("CityGUID can't be invalid.");
			yield break;
		}
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityGUID, out gameEntity))
		{
			yield break;
		}
		if (gameEntity is City)
		{
			if (order.ForceOn)
			{
				this.StartRoundUp(gameEntity as City);
			}
			else if (order.ForceOff)
			{
				this.StopRoundUp(gameEntity as City);
			}
			else
			{
				this.ToggleCityRoundUp(gameEntity as City);
			}
			(gameEntity as City).Refresh(false);
			yield break;
		}
		yield break;
	}

	private bool UntameKaijuPreprocessor(OrderUntameKaiju order)
	{
		if (!this.MainCityGUID.IsValid)
		{
			Diagnostics.LogWarning("Order preprocessing failed because the Main City GUID is not valid.");
			return false;
		}
		if (!order.KaijuGUID.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the Kaiju GUID is not valid.");
			return false;
		}
		Kaiju kaiju = null;
		if (!this.GameEntityRepositoryService.TryGetValue<Kaiju>(order.KaijuGUID, out kaiju))
		{
			Diagnostics.LogError("Order preprocessing failed because the Kaiju game entity could not be retrieved. GUID: {0}).", new object[]
			{
				order.KaijuGUID
			});
			return false;
		}
		if (!kaiju.IsTamed())
		{
			Diagnostics.LogWarning("Order preprocessing failed because Kaiju is not tamed.");
			return false;
		}
		if (kaiju.MajorEmpire == null)
		{
			Diagnostics.LogWarning("Order preprocessing failed because Kaiju's MajorEmpire is not valid.");
			return false;
		}
		if (kaiju.KaijuEmpire == null)
		{
			Diagnostics.LogWarning("Order preprocessing failed because Kaiju's KaijuEmpire is not valid.");
			return false;
		}
		if (kaiju.KaijuEmpire.GetAgency<KaijuCouncil>() == null)
		{
			Diagnostics.LogWarning("Order preprocessing failed because Kaiju Council is not valid.");
			return false;
		}
		if (order.StunningEmpireIndex != -1 && order.AutoTameAfterDefeat)
		{
			global::Empire empireByIndex = (this.GameService.Game as global::Game).GetEmpireByIndex<global::Empire>(order.StunningEmpireIndex);
			if (!(empireByIndex is MajorEmpire))
			{
				order.StunnerEmpireCanTameKaiju = false;
			}
			else
			{
				MajorEmpire majorEmpire = empireByIndex as MajorEmpire;
				if (majorEmpire != null)
				{
					DepartmentOfTheInterior agency = majorEmpire.GetAgency<DepartmentOfTheInterior>();
					if (agency != null && agency.Cities.Count <= 0)
					{
						order.StunnerEmpireCanTameKaiju = false;
					}
				}
			}
		}
		if (order.Relocate || !order.StunnerEmpireCanTameKaiju)
		{
			Region validKaijuRegion = KaijuCouncil.GetValidKaijuRegion();
			if (validKaijuRegion == null)
			{
				if (kaiju.OnArmyMode())
				{
					order.Relocate = false;
				}
				else if (kaiju.Region != null)
				{
					order.Relocate = false;
				}
				else
				{
					Region region = this.WorldPositionningService.GetRegion(kaiju.WorldPosition);
					if (region != null && region.Owner == null)
					{
						order.SpawnWorldPosition = kaiju.WorldPosition;
					}
				}
			}
			else
			{
				WorldPosition validKaijuPosition = KaijuCouncil.GetValidKaijuPosition(validKaijuRegion, false);
				if (validKaijuPosition == WorldPosition.Zero)
				{
					validKaijuPosition = KaijuCouncil.GetValidKaijuPosition(validKaijuRegion, true);
				}
				order.SpawnWorldPosition = validKaijuPosition;
			}
		}
		return true;
	}

	private IEnumerator UntameKaijuProcessor(OrderUntameKaiju order)
	{
		Kaiju kaiju = null;
		if (!this.GameEntityRepositoryService.TryGetValue<Kaiju>(order.KaijuGUID, out kaiju))
		{
			Diagnostics.LogError("Order processing failed because the Kaiju game entity could not be retrieved. GUID: {0}).", new object[]
			{
				order.KaijuGUID
			});
			yield break;
		}
		if (kaiju.MajorEmpire.Index != base.Empire.Index)
		{
			Diagnostics.LogWarning("Something went wrong, OrderUntameKaiju is being processed by a foreign empire's department.");
		}
		global::Empire lastOwnerEmpire = kaiju.Empire;
		KaijuArmy kaijuArmy = kaiju.KaijuArmy;
		bool keepOnArmyMode = !order.Relocate && kaiju.OnArmyMode();
		if (keepOnArmyMode)
		{
			MajorEmpire ownerEmpire = kaiju.MajorEmpire;
			ownerEmpire.GetAgency<DepartmentOfDefense>().RemoveArmy(kaijuArmy, false);
			ownerEmpire.Refresh(false);
			kaijuArmy.Empire = null;
		}
		KaijuCouncil kaijuCouncil = kaiju.KaijuEmpire.GetAgency<KaijuCouncil>();
		kaijuCouncil.MajorEmpireUntameKaiju(kaiju, order.StunningEmpireIndex, order.ClearMilitias);
		if (order.StunningEmpireIndex != -1 && !order.AutoTameAfterDefeat && !order.Relocate)
		{
			global::Empire stunningEmpire = (this.GameService.Game as global::Game).GetEmpireByIndex<global::Empire>(order.StunningEmpireIndex);
			kaiju.ChangeToStunState(stunningEmpire);
		}
		if (!keepOnArmyMode && !kaiju.OnGarrisonMode())
		{
			kaiju.ChangeToGarrisonMode(false);
		}
		kaiju.RefreshSharedSight();
		this.VisibilityService.NotifyVisibilityHasChanged(lastOwnerEmpire);
		DepartmentOfTheInterior.ClearResourcesLeechingForKaijus(kaiju);
		base.Empire.Refresh(false);
		bool relocated = false;
		if (order.Relocate)
		{
			kaiju.MoveToRegion(order.SpawnWorldPosition);
			relocated = true;
		}
		bool autoTame = order.AutoTameAfterDefeat;
		if (autoTame && order.StunningEmpireIndex != -1)
		{
			if (order.StunnerEmpireCanTameKaiju)
			{
				global::Empire stunningEmpire2 = (this.GameService.Game as global::Game).GetEmpireByIndex<global::Empire>(order.StunningEmpireIndex);
				if (stunningEmpire2 is MajorEmpire)
				{
					MajorEmpire stunningMajorEmpire = stunningEmpire2 as MajorEmpire;
					if (stunningMajorEmpire != null)
					{
						kaijuCouncil.MajorEmpireTameKaiju(stunningMajorEmpire, false);
					}
				}
			}
			else
			{
				kaiju.ChangeToWildState();
				kaiju.MoveToRegion(order.SpawnWorldPosition);
				relocated = true;
			}
		}
		if (relocated)
		{
			this.EventService.Notify(new EventKaijuRelocated(kaiju));
		}
		kaijuCouncil.ResetRelocationETA();
		IGuiService guiService = Services.GetService<IGuiService>();
		ArmyActionModalPanel armyActionModalPanel = guiService.GetGuiPanel<ArmyActionModalPanel>();
		if (!(armyActionModalPanel != null))
		{
			yield break;
		}
		if (armyActionModalPanel.IsVisible)
		{
			armyActionModalPanel.RefreshContent();
			yield break;
		}
		yield break;
	}

	private bool UpdateCadastralMapPreprocessor(OrderUpdateCadastralMap order)
	{
		if (order.PathfindingMovementCapacity == PathfindingMovementCapacity.None)
		{
			return false;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityGameEntityGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			return false;
		}
		City city = gameEntity as City;
		if (city == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity does not convert to a city.");
			return false;
		}
		Diagnostics.Assert(this.GameService != null);
		Diagnostics.Assert(this.GameService.Game != null);
		ICadasterService service = this.GameService.Game.Services.GetService<ICadasterService>();
		if (service != null)
		{
			switch (order.Operation)
			{
			case CadastralMapOperation.Connect:
			case CadastralMapOperation.Proxy:
			{
				bool proxied = order.Operation == CadastralMapOperation.Proxy;
				ushort[] array = service.Connect(city, order.PathfindingMovementCapacity, proxied);
				if (array != null && array.Length > 0)
				{
					order.Indices = array;
					order.Roads = new Road[array.Length];
					ushort num = 0;
					while ((int)num < array.Length)
					{
						order.Roads[(int)num] = service[array[(int)num]];
						num += 1;
					}
					return true;
				}
				break;
			}
			case CadastralMapOperation.Disconnect:
				service.Disconnect(city, order.PathfindingMovementCapacity, true);
				return true;
			}
		}
		return false;
	}

	private IEnumerator UpdateCadastralMapProcessor(OrderUpdateCadastralMap order)
	{
		Diagnostics.Assert(this.GameService != null);
		Diagnostics.Assert(this.GameService.Game != null);
		ICadasterService cadasterService = this.GameService.Game.Services.GetService<ICadasterService>();
		Diagnostics.Assert(cadasterService != null);
		switch (order.Operation)
		{
		case CadastralMapOperation.Connect:
		case CadastralMapOperation.Proxy:
		{
			Diagnostics.Assert(order.Indices.Length == order.Roads.Length);
			for (int index = 0; index < order.Indices.Length; index++)
			{
				cadasterService.Register(order.Indices[index], order.Roads[index]);
			}
			short regionIndex = -1;
			for (int index2 = 0; index2 < this.cities.Count; index2++)
			{
				if (this.cities[index2].GUID == order.CityGameEntityGUID)
				{
					regionIndex = (short)this.cities[index2].Region.Index;
					if (this.cities[index2].CadastralMap.Roads == null)
					{
						this.cities[index2].CadastralMap.Roads = new List<ushort>();
					}
					this.cities[index2].CadastralMap.Roads.AddRange(order.Indices);
					this.cities[index2].NotifyCityCadastralChange();
					break;
				}
			}
			List<short> neighbourRegions = new List<short>();
			for (int index3 = 0; index3 < order.Roads.Length; index3++)
			{
				neighbourRegions.AddOnce(order.Roads[index3].FromRegion);
				neighbourRegions.AddOnce(order.Roads[index3].ToRegion);
			}
			neighbourRegions.Remove(regionIndex);
			if (neighbourRegions.Count > 0)
			{
				for (int index4 = 0; index4 < neighbourRegions.Count; index4++)
				{
					Region region = this.WorldPositionningService.GetRegion((int)neighbourRegions[index4]);
					if (region != null)
					{
						if (region.City != null && region.City.CadastralMap.Roads == null)
						{
							region.City.CadastralMap.Roads = new List<ushort>();
						}
						for (int jndex = 0; jndex < order.Roads.Length; jndex++)
						{
							if (neighbourRegions[index4] == order.Roads[jndex].ToRegion || neighbourRegions[index4] == order.Roads[jndex].FromRegion)
							{
								region.City.CadastralMap.Roads.AddOnce(order.Indices[jndex]);
								region.City.NotifyCityCadastralChange();
							}
						}
					}
				}
			}
			break;
		}
		case CadastralMapOperation.Disconnect:
			Diagnostics.Assert(order.Indices != null && order.Indices.Length > 0);
			for (int index5 = 0; index5 < order.Indices.Length; index5++)
			{
				cadasterService.Unregister(order.Indices[index5]);
			}
			break;
		}
		cadasterService.RefreshCadasterMap();
		yield break;
	}

	private bool UpgradePointOfInterestPreprocessor(OrderUpgradePointOfInterest order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.PointOfInterestGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			return false;
		}
		PointOfInterest pointOfInterest = gameEntity as PointOfInterest;
		if (pointOfInterest == null)
		{
			Diagnostics.LogError("Order GUID does not belong to a point of interest.");
			return false;
		}
		if (pointOfInterest.Region.City == null)
		{
			Diagnostics.LogError("Can not upgrade a point of interest that does not belong to a city");
			return false;
		}
		return true;
	}

	private IEnumerator UpgradePointOfInterestProcessor(OrderUpgradePointOfInterest order)
	{
		IGameService gameService = Services.GetService<IGameService>();
		if (gameService == null)
		{
			Diagnostics.LogError("Order preprocessing failed because we cannot retrieve the game service.");
			yield break;
		}
		global::Game game = gameService.Game as global::Game;
		if (game == null)
		{
			Diagnostics.LogError("gameService.Game isn't an instance of Game.");
			yield break;
		}
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.PointOfInterestGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			yield break;
		}
		if (!(gameEntity is SimulationObjectWrapper))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a simulation object wrapper.");
			yield break;
		}
		PointOfInterest pointOfInterest = gameEntity as PointOfInterest;
		if (pointOfInterest == null)
		{
			Diagnostics.LogError("Order GUID does not belong to a point of interest.");
			yield break;
		}
		if (pointOfInterest.Region.City == null)
		{
			Diagnostics.LogError("Can not upgrade a point of interest that does not belong to a city");
			yield break;
		}
		this.UpdatePointOfInterestImprovement(pointOfInterest.Region.City, pointOfInterest);
		yield break;
	}

	public static void CheckBesiegerArmyStatus(Army army)
	{
		SimulationDescriptor value = DepartmentOfTheInterior.SimulationDescriptorDatabaseStatic.GetValue(DepartmentOfTheInterior.ArmyStatusBesiegerDescriptorName);
		District district = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetDistrict(army.WorldPosition);
		bool flag = false;
		if (district != null && district.City.BesiegingEmpireIndex == army.Empire.Index && district.Type == DistrictType.Exploitation && !DepartmentOfTheInterior.WorldPositionningServiceStatic.IsWaterTile(district.WorldPosition))
		{
			flag = true;
		}
		if (flag)
		{
			army.SwapDescriptor(value);
		}
		else
		{
			army.RemoveDescriptor(value);
		}
	}

	public static void CheckBesiegingSeafaringArmyStatus(IEnumerable<Army> besiegingSeafaringArmies, DepartmentOfTheInterior.BesiegingSeafaringArmyStatus statuses)
	{
		if (besiegingSeafaringArmies == null)
		{
			return;
		}
		IGameService service = Services.GetService<IGameService>();
		if (service == null || service.Game == null)
		{
			return;
		}
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		if (service2 == null)
		{
			return;
		}
		foreach (Army army in besiegingSeafaringArmies)
		{
			Region region = service2.GetRegion(army.WorldPosition);
			if (region != null && region.City != null && region.City.BesiegingSeafaringArmies.Contains(army))
			{
				float num = army.Units.Sum((Unit unit) => unit.GetPropertyValue(SimulationProperties.CityDefensePointLossPerTurn));
				if (num <= 0f)
				{
					DepartmentOfTheInterior agency = region.City.Empire.GetAgency<DepartmentOfTheInterior>();
					agency.StopNavalSiege(region.City, army);
				}
			}
		}
	}

	public static void CheckDefenderArmyStatus(Army army)
	{
		SimulationDescriptor value = DepartmentOfTheInterior.SimulationDescriptorDatabaseStatic.GetValue(DepartmentOfTheInterior.ArmyStatusDefenderDescriptorName);
		District district = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetDistrict(army.WorldPosition);
		bool flag = false;
		if (district != null && district.City.Empire == army.Empire && district.City.BesiegingEmpireIndex >= 0 && district.Type != DistrictType.Exploitation && !DepartmentOfTheInterior.WorldPositionningServiceStatic.IsWaterTile(district.WorldPosition))
		{
			flag = true;
		}
		if (flag)
		{
			army.SwapDescriptor(value);
		}
		else
		{
			army.RemoveDescriptor(value);
		}
	}

	public static Army[] GetBesiegers(City city)
	{
		List<Army> list = new List<Army>();
		for (int i = 0; i < city.Districts.Count; i++)
		{
			if (city.Districts[i].Type == DistrictType.Exploitation && !DepartmentOfTheInterior.WorldPositionningServiceStatic.IsWaterTile(city.Districts[i].WorldPosition))
			{
				Army armyAtPosition = DepartmentOfTheInterior.WorldPositionningServiceStatic.GetArmyAtPosition(city.Districts[i].WorldPosition);
				if (armyAtPosition != null && armyAtPosition.Empire == city.BesiegingEmpire && !armyAtPosition.IsNaval)
				{
					list.Add(armyAtPosition);
				}
			}
		}
		return list.ToArray();
	}

	public static List<Army> GetBesiegingSeafaringArmies(GameEntityGUID[] units)
	{
		if (units == null || units.Length == 0)
		{
			return null;
		}
		IGameService service = Services.GetService<IGameService>();
		if (service == null || service.Game == null)
		{
			return null;
		}
		IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
		if (service2 == null)
		{
			return null;
		}
		List<Army> list = new List<Army>();
		for (int i = 0; i < units.Length; i++)
		{
			IGameEntity gameEntity;
			if (service2.TryGetValue(units[i], out gameEntity))
			{
				Unit unit = gameEntity as Unit;
				if (unit != null && unit.Garrison != null)
				{
					Army army = unit.Garrison as Army;
					if (army != null && army.IsSeafaring && !list.Contains(army))
					{
						list.Add(army);
					}
				}
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		IWorldPositionningService service3 = service.Game.Services.GetService<IWorldPositionningService>();
		if (service3 == null)
		{
			return null;
		}
		List<Army> list2 = new List<Army>();
		foreach (Army army2 in list)
		{
			Region region = service3.GetRegion(army2.WorldPosition);
			if (region != null && region.City != null && region.City.BesiegingSeafaringArmies.Contains(army2))
			{
				list2.Add(army2);
			}
		}
		return list2;
	}

	public static float GetBesiegingPower(City city, bool includingSeafaringArmies = true)
	{
		float num = 0f;
		Army[] besiegers = DepartmentOfTheInterior.GetBesiegers(city);
		foreach (Army army in besiegers)
		{
			army.Refresh(true);
			num += army.GetPropertyValue(SimulationProperties.CityDefensePointLossPerTurn);
		}
		if (includingSeafaringArmies && city.BesiegingSeafaringArmies.Count != 0)
		{
			float num2 = city.BesiegingSeafaringArmies.Sum((Army seafaringArmy) => seafaringArmy.GetPropertyValue(SimulationProperties.CityDefensePointLossPerTurn));
			num += num2;
		}
		return num;
	}

	public bool NeedToStopSiege(City city)
	{
		if (city.BesiegingEmpire == null)
		{
			return false;
		}
		bool result = true;
		for (int i = 0; i < city.Districts.Count; i++)
		{
			if (city.Districts[i].Type == DistrictType.Exploitation && !this.WorldPositionningService.IsWaterTile(city.Districts[i].WorldPosition))
			{
				Army armyAtPosition = this.WorldPositionningService.GetArmyAtPosition(city.Districts[i].WorldPosition);
				if (armyAtPosition != null && armyAtPosition.Empire == city.BesiegingEmpire && !armyAtPosition.IsNaval && armyAtPosition.SimulationObject.Tags.Contains(DepartmentOfTheInterior.ArmyStatusBesiegerDescriptorName))
				{
					result = false;
					break;
				}
			}
		}
		return result;
	}

	public void StartSiege(City city, global::Empire besiegingEmpire)
	{
		if (city.BesiegingEmpire == besiegingEmpire)
		{
			return;
		}
		if (city.BesiegingEmpire != null)
		{
			this.UpdateBesiegerArmiesStatus(city, city.BesiegingEmpire, true);
		}
		this.UpdateBesiegerArmiesStatus(city, besiegingEmpire, false);
		this.UpdateDefenderArmiesStatus(city, city.Empire, false);
		if (!city.SimulationObject.Tags.Contains(DepartmentOfTheInterior.CityStatusSiegeDescriptorName))
		{
			SimulationDescriptor value = this.SimulationDescriptorDatabase.GetValue(DepartmentOfTheInterior.CityStatusSiegeDescriptorName);
			if (value != null)
			{
				city.AddDescriptor(value, false);
			}
		}
		city.BesiegingEmpireIndex = besiegingEmpire.Index;
		this.CitySiegeUpdate(city);
		this.VerifyOverallPopulation(city);
	}

	public void StartNavalSiege(City city, Army besiegingArmy)
	{
		if (!city.BesiegingSeafaringArmies.Contains(besiegingArmy))
		{
			city.BesiegingSeafaringArmies.Add(besiegingArmy);
			if (!besiegingArmy.SimulationObject.Tags.Contains(DepartmentOfTheInterior.ArmyStatusSeafaringBesiegerDescriptorName))
			{
				SimulationDescriptor value = this.SimulationDescriptorDatabase.GetValue(DepartmentOfTheInterior.ArmyStatusSeafaringBesiegerDescriptorName);
				if (value != null)
				{
					besiegingArmy.AddDescriptor(value, false);
				}
			}
		}
		if (!city.SimulationObject.Tags.Contains(DepartmentOfTheInterior.CityStatusNavalSiegeDescriptorName))
		{
			SimulationDescriptor value2 = this.SimulationDescriptorDatabase.GetValue(DepartmentOfTheInterior.CityStatusNavalSiegeDescriptorName);
			if (value2 != null)
			{
				city.AddDescriptor(value2, false);
			}
		}
		this.CitySiegeUpdate(city);
	}

	public void StopSiege(City city)
	{
		this.UpdateBesiegerArmiesStatus(city, city.BesiegingEmpire, true);
		this.UpdateDefenderArmiesStatus(city, city.Empire, true);
		city.RemoveDescriptorByName(DepartmentOfTheInterior.CityStatusSiegeDescriptorName);
		city.BesiegingEmpireIndex = -1;
		this.CitySiegeUpdate(city);
		this.VerifyOverallPopulation(city);
	}

	public void StopNavalSiege(City city, IEnumerable<Army> armies)
	{
		foreach (Army army in armies)
		{
			city.BesiegingSeafaringArmies.Remove(army);
			army.RemoveDescriptorByName(DepartmentOfTheInterior.ArmyStatusSeafaringBesiegerDescriptorName);
		}
		if (city.BesiegingSeafaringArmies.Count == 0)
		{
			this.StopNavalSiege(city);
		}
		else
		{
			this.CitySiegeUpdate(city);
		}
	}

	public void StopNavalSiege(City city, Army besiegingArmy)
	{
		if (city.BesiegingSeafaringArmies.Contains(besiegingArmy))
		{
			city.BesiegingSeafaringArmies.Remove(besiegingArmy);
			besiegingArmy.RemoveDescriptorByName(DepartmentOfTheInterior.ArmyStatusSeafaringBesiegerDescriptorName);
		}
		if (city.BesiegingSeafaringArmies.Count == 0)
		{
			this.StopNavalSiege(city);
		}
		else
		{
			this.CitySiegeUpdate(city);
		}
	}

	public void StopNavalSiege(City city)
	{
		if (city.BesiegingSeafaringArmies.Count != 0)
		{
			foreach (Army army in city.BesiegingSeafaringArmies)
			{
				army.RemoveDescriptorByName(DepartmentOfTheInterior.ArmyStatusSeafaringBesiegerDescriptorName);
			}
			city.BesiegingSeafaringArmies.Clear();
		}
		if (city.SimulationObject.Tags.Contains(DepartmentOfTheInterior.CityStatusNavalSiegeDescriptorName))
		{
			city.RemoveDescriptorByName(DepartmentOfTheInterior.CityStatusNavalSiegeDescriptorName);
			this.CitySiegeUpdate(city);
		}
	}

	public void UpdateSiegeAtEndTurn(City city)
	{
		bool flag = false;
		flag |= (city.BesiegingEmpire != null);
		flag |= (city.BesiegingSeafaringArmies.Count != 0);
		if (flag)
		{
			float num = DepartmentOfTheInterior.GetBesiegingPower(city, true);
			float num2 = city.GetPropertyValue(SimulationProperties.CityDefensePoint);
			float num3 = city.GetPropertyValue(SimulationProperties.MaximumCityDefensePoint);
			if (num3 <= 0f)
			{
				num3 = num2;
			}
			float num4 = num - DepartmentOfTheInterior.GetBesiegingPower(city, false);
			if (num2 > 0f)
			{
				float num5 = num2 - num;
				if (city.BesiegingEmpire != null)
				{
					if (num5 > 0f)
					{
						float damage = Mathf.Round(num * 100f / num3);
						this.UpdateSiegeDamageForBesieger(city.BesiegingEmpire, damage);
					}
					else
					{
						float damage = Mathf.Round(num2 * 100f / num3);
						this.UpdateSiegeDamageForBesieger(city.BesiegingEmpire, damage);
					}
				}
				num2 = Mathf.Max(0f, num5);
				city.SetPropertyBaseValue(SimulationProperties.CityDefensePoint, num2);
				if (num5 < 0f)
				{
					num = -num5;
				}
				else
				{
					num = 0f;
				}
			}
			if (num > 0f)
			{
				num -= num4;
			}
			if (num > 0f)
			{
				float num6 = num * city.GetPropertyValue(SimulationProperties.CityDefensePointLossToHealthLossRatio);
				foreach (Unit unit in city.Units)
				{
					float num7 = unit.GetPropertyValue(SimulationProperties.Health);
					num7 -= num6;
					unit.SetPropertyBaseValue(SimulationProperties.Health, num7);
				}
				DepartmentOfDefense agency = base.Empire.GetAgency<DepartmentOfDefense>();
				agency.UpdateLifeAfterEncounter(city);
				agency.CleanGarrisonAfterEncounter(city);
				if (city.Militia != null)
				{
					foreach (Unit unit2 in city.Militia.StandardUnits)
					{
						float num8 = unit2.GetPropertyValue(SimulationProperties.Health);
						num8 -= num6;
						unit2.SetPropertyBaseValue(SimulationProperties.Health, num8);
					}
					agency.CleanGarrisonAfterEncounter(city.Militia);
				}
				for (int i = 0; i < city.Districts.Count; i++)
				{
					if (city.Districts[i].Type != DistrictType.Exploitation && !this.WorldPositionningService.IsWaterTile(city.Districts[i].WorldPosition))
					{
						Army armyAtPosition = this.WorldPositionningService.GetArmyAtPosition(city.Districts[i].WorldPosition);
						if (armyAtPosition != null && armyAtPosition.Empire == city.Empire)
						{
							foreach (Unit unit3 in armyAtPosition.Units)
							{
								float num9 = unit3.GetPropertyValue(SimulationProperties.Health);
								num9 -= num6;
								unit3.SetPropertyBaseValue(SimulationProperties.Health, num9);
							}
							agency.UpdateLifeAfterEncounter(armyAtPosition);
							agency.CleanGarrisonAfterEncounter(armyAtPosition);
						}
					}
				}
			}
		}
		else if (!city.IsUnderEarthquake)
		{
			float propertyValue = city.GetPropertyValue(SimulationProperties.MaximumCityDefensePoint);
			float num10 = city.GetPropertyValue(SimulationProperties.CityDefensePoint);
			if (propertyValue > num10)
			{
				num10 += city.GetPropertyValue(SimulationProperties.CityDefensePointRecoveryPerTurn);
				num10 = Mathf.Min(propertyValue, num10);
				city.SetPropertyBaseValue(SimulationProperties.CityDefensePoint, num10);
			}
		}
	}

	private void UpdateSiegeDamageForBesieger(global::Empire empire, float damage)
	{
		DepartmentOfScience agency = empire.GetAgency<DepartmentOfScience>();
		if (agency != null && agency.GetTechnologyState("TechnologyDefinitionFlames9") == DepartmentOfScience.ConstructibleElement.State.Researched)
		{
			DepartmentOfTheTreasury agency2 = empire.GetAgency<DepartmentOfTheTreasury>();
			agency2.TryTransferResources(empire, DepartmentOfTheTreasury.Resources.SiegeDamage, damage);
		}
	}

	public void SwapCityOwner(City city, global::Empire empireWhichReceives)
	{
		if (city.BesiegingEmpire != null)
		{
			this.StopSiege(city);
		}
		Army[] cityEarthquakeInstigators = DepartmentOfTheInterior.GetCityEarthquakeInstigators(city);
		for (int i = 0; i < cityEarthquakeInstigators.Length; i++)
		{
			cityEarthquakeInstigators[i].SetEarthquakerStatus(false, false, null);
		}
		if (city.BesiegingSeafaringArmies.Count != 0)
		{
			List<global::Empire> list = (from besiegingSeafaringArmies in city.BesiegingSeafaringArmies
			select besiegingSeafaringArmies.Empire).Distinct<global::Empire>().ToList<global::Empire>();
			global::Empire besiegingEmpire;
			foreach (global::Empire besiegingEmpire2 in list)
			{
				besiegingEmpire = besiegingEmpire2;
				DepartmentOfForeignAffairs agency = besiegingEmpire.GetAgency<DepartmentOfForeignAffairs>();
				if (agency == null || !agency.CanBesiegeCity(city) || besiegingEmpire.Index == empireWhichReceives.Index)
				{
					List<Army> list2 = (from besiegingSeafaringArmy in city.BesiegingSeafaringArmies
					where besiegingSeafaringArmy.Empire == besiegingEmpire
					select besiegingSeafaringArmy).ToList<Army>();
					foreach (Army besiegingArmy in list2)
					{
						this.StopNavalSiege(city, besiegingArmy);
					}
				}
			}
		}
		if (this.MainCity != null && this.MainCity.GUID == city.GUID)
		{
			MajorEmpire majorEmpire = base.Empire as MajorEmpire;
			if (majorEmpire != null)
			{
				majorEmpire.UnconvertAndPacifyAllConvertedVillages();
			}
		}
		MinorEmpire minorEmpire = city.Region.MinorEmpire;
		this.StopRoundUp(city);
		this.RemoveCity(city, true);
		this.UnbindMinorEmpireToCity(city, minorEmpire);
		DepartmentOfIndustry agency2 = base.Empire.GetAgency<DepartmentOfIndustry>();
		if (agency2 != null)
		{
			agency2.RemoveQueueFrom<City>(city);
		}
		DepartmentOfPlanificationAndDevelopment agency3 = base.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
		if (agency3 != null)
		{
			agency3.RemoveBoostersFromTarget(city.GUID, base.Empire.Index);
			agency3.RemoveBoostersFromTarget(city.GUID, empireWhichReceives.Index);
		}
		if (city.StandardUnits.Count > 0)
		{
			Diagnostics.LogWarning("We are swapping a city which is not empty; this should not happen. All remaining units will be scraped.");
			while (city.StandardUnits.Count > 0)
			{
				Unit unit = city.StandardUnits[0];
				city.RemoveUnit(unit);
				this.GameEntityRepositoryService.Unregister(unit);
				unit.Dispose();
			}
		}
		if (city.Militia != null && city.Militia.StandardUnits.Count > 0)
		{
			Diagnostics.LogWarning("We are swapping a city which militia's garrison is not empty; this should not happen. All remaining units will be destroyed.");
			while (city.Militia.StandardUnits.Count > 0)
			{
				Unit unit2 = city.Militia.StandardUnits[0];
				city.Militia.RemoveUnit(unit2);
				this.GameEntityRepositoryService.Unregister(unit2);
				unit2.Dispose();
			}
		}
		if (city.Hero != null)
		{
			DepartmentOfEducation agency4 = base.Empire.GetAgency<DepartmentOfEducation>();
			agency4.UnassignHero(city.Hero);
		}
		SimulationDescriptor descriptor;
		if (empireWhichReceives.SimulationObject.Tags.Contains(FactionTrait.FactionTraitMimics2) && !city.SimulationObject.Tags.Contains(City.MimicsCity) && this.SimulationDescriptorDatabase.TryGetValue(City.MimicsCity, out descriptor))
		{
			city.AddDescriptor(descriptor, false);
		}
		city.SetPropertyBaseValue(SimulationProperties.Ownership, city.Ownership[empireWhichReceives.Index]);
		city.AdministrationSpeciality = StaticString.Empty;
		DepartmentOfIndustry agency5 = empireWhichReceives.GetAgency<DepartmentOfIndustry>();
		if (agency5 != null)
		{
			agency5.AddQueueTo<City>(city);
		}
		DepartmentOfTheInterior agency6 = empireWhichReceives.GetAgency<DepartmentOfTheInterior>();
		if (agency6 != null)
		{
			agency6.AddCity(city, true, true);
			agency6.BindMinorFactionToCity(city, city.Region.MinorEmpire);
			agency6.UpdatePointOfInterestImprovement(city);
		}
		city.SetPropertyBaseValue(SimulationProperties.CityOwnedTurn, (float)(this.GameService.Game as global::Game).Turn);
		this.VisibilityService.NotifyVisibilityHasChanged(base.Empire as global::Empire);
		this.VisibilityService.NotifyVisibilityHasChanged(empireWhichReceives);
		city.NotifyCityOwnerChange();
		for (int j = 0; j < city.Region.PointOfInterests.Length; j++)
		{
			if (city.Region.PointOfInterests[j].ArmyPillaging.IsValid)
			{
				IGameEntity gameEntity;
				if (this.GameEntityRepositoryService.TryGetValue(city.Region.PointOfInterests[j].ArmyPillaging, out gameEntity))
				{
					Army army = gameEntity as Army;
					if (army != null && DepartmentOfDefense.CanStartPillage(army, city.Region.PointOfInterests[j], true))
					{
						goto IL_4AD;
					}
				}
				DepartmentOfDefense.StopPillage(city.Region.PointOfInterests[j]);
			}
			IL_4AD:;
		}
		this.AddDistrictDescriptorExploitableResource(city);
		if (empireWhichReceives.SimulationObject.Tags.Contains(FactionTrait.FactionTraitMimics3) && city.IsInfected)
		{
			MajorEmpire majorEmpire2 = empireWhichReceives as MajorEmpire;
			if (majorEmpire2 != null)
			{
				DepartmentOfCreepingNodes agency7 = majorEmpire2.GetAgency<DepartmentOfCreepingNodes>();
				agency7.ReplacePOIImprovementsWhitCreepingNodeImprovements(city);
			}
		}
		if (agency5 != null)
		{
			agency5.QueueIntegrationIFN(city);
		}
		city.CallRefreshAppliedRegionEffects();
	}

	private void CitySiegeUpdate(City city)
	{
		for (int i = 0; i < city.Districts.Count; i++)
		{
			if (city.Districts[i].Type == DistrictType.Exploitation)
			{
				city.Districts[i].LineOfSightDirty = true;
			}
		}
		for (int j = 0; j < city.Region.PointOfInterests.Length; j++)
		{
			if (city.Region.PointOfInterests[j].PointOfInterestImprovement != null)
			{
				city.Region.PointOfInterests[j].LineOfSightDirty = true;
			}
		}
		IEventService service = Services.GetService<IEventService>();
		if (service != null)
		{
			EventCitySiegeUpdate eventToNotify = new EventCitySiegeUpdate(city.Empire, city);
			service.Notify(eventToNotify);
		}
	}

	private void UpdateBesiegerArmiesStatus(City city, Amplitude.Unity.Game.Empire empire, bool remove)
	{
		SimulationDescriptor value = this.SimulationDescriptorDatabase.GetValue(DepartmentOfTheInterior.ArmyStatusBesiegerDescriptorName);
		for (int i = 0; i < city.Districts.Count; i++)
		{
			if (city.Districts[i].Type == DistrictType.Exploitation && !this.WorldPositionningService.IsWaterTile(city.Districts[i].WorldPosition))
			{
				Army armyAtPosition = this.WorldPositionningService.GetArmyAtPosition(city.Districts[i].WorldPosition);
				if (armyAtPosition != null && armyAtPosition.Empire == empire && !armyAtPosition.IsNaval)
				{
					if (remove)
					{
						armyAtPosition.RemoveDescriptor(value);
					}
					else
					{
						armyAtPosition.SwapDescriptor(value);
					}
					armyAtPosition.Refresh(false);
				}
			}
		}
	}

	private void UpdateDefenderArmiesStatus(City city, Amplitude.Unity.Game.Empire empire, bool remove)
	{
		SimulationDescriptor value = this.SimulationDescriptorDatabase.GetValue(DepartmentOfTheInterior.ArmyStatusDefenderDescriptorName);
		for (int i = 0; i < city.Districts.Count; i++)
		{
			if (city.Districts[i].Type != DistrictType.Exploitation && !this.WorldPositionningService.IsWaterTile(city.Districts[i].WorldPosition))
			{
				Army armyAtPosition = this.WorldPositionningService.GetArmyAtPosition(city.Districts[i].WorldPosition);
				if (armyAtPosition != null && armyAtPosition.Empire == empire && !armyAtPosition.IsNaval)
				{
					if (remove)
					{
						armyAtPosition.RemoveDescriptor(value);
					}
					else
					{
						armyAtPosition.SwapDescriptor(value);
					}
					armyAtPosition.Refresh(false);
				}
			}
		}
	}

	private void VerifyOverallPopulation(City city, float oldPopulation)
	{
		this.VerifyOverallPopulation(city);
		float propertyValue = city.GetPropertyValue(SimulationProperties.Population);
		this.VerifyOverallPopulation(city, oldPopulation, propertyValue);
	}

	private void VerifyOverallPopulation(City city, float oldPopulation, float newPopulation)
	{
		if (oldPopulation != newPopulation)
		{
			float num;
			if (!this.departmentOfTheTreasury.TryGetResourceStockValue(city.SimulationObject, DepartmentOfTheTreasury.Resources.CityGrowth, out num, false))
			{
				Diagnostics.LogError("Can't get resource stock value {0} on simulation object {1}.", new object[]
				{
					DepartmentOfTheTreasury.Resources.CityGrowth,
					city.SimulationObject.Name
				});
			}
			float num2;
			float num3;
			this.GetGrowthLimits(oldPopulation, out num2, out num3);
			float num4 = (num - num2) / (num3 - num2);
			float num5;
			float num6;
			this.GetGrowthLimits(newPopulation, out num5, out num6);
			float num7 = num5 + num4 * (num6 - num5);
			float amount = num7 - num;
			this.departmentOfTheTreasury.TryTransferResources(city.SimulationObject, DepartmentOfTheTreasury.Resources.CityGrowth, amount);
		}
		city.Refresh(false);
	}

	public ReadOnlyCollection<City> Cities
	{
		get
		{
			if (this.readOnlyCities == null)
			{
				this.readOnlyCities = this.cities.AsReadOnly();
			}
			return this.readOnlyCities;
		}
	}

	public ReadOnlyCollection<City> NonInfectedCities
	{
		get
		{
			List<City> list = new List<City>();
			for (int i = 0; i < this.cities.Count; i++)
			{
				if (!this.cities[i].IsInfected)
				{
					list.Add(this.cities[i]);
				}
			}
			return list.AsReadOnly();
		}
	}

	public ReadOnlyCollection<City> InfectedCities
	{
		get
		{
			List<City> list = new List<City>();
			for (int i = 0; i < this.cities.Count; i++)
			{
				if (this.cities[i].IsInfected)
				{
					list.Add(this.cities[i]);
				}
			}
			return list.AsReadOnly();
		}
	}

	public ReadOnlyCollection<Camp> Camps
	{
		get
		{
			List<Camp> list = new List<Camp>();
			for (int i = 0; i < this.cities.Count; i++)
			{
				if (this.cities[i].Camp != null)
				{
					list.Add(this.cities[i].Camp);
				}
			}
			return list.AsReadOnly();
		}
	}

	public ReadOnlyCollection<Village> ConvertedVillages
	{
		get
		{
			List<Village> list = new List<Village>();
			if (base.Empire is MajorEmpire)
			{
				MajorEmpire majorEmpire = base.Empire as MajorEmpire;
				list = majorEmpire.ConvertedVillages;
			}
			return list.AsReadOnly();
		}
	}

	public ReadOnlyCollection<KaijuGarrison> TamedKaijuGarrisons
	{
		get
		{
			List<KaijuGarrison> list = new List<KaijuGarrison>();
			if (base.Empire is MajorEmpire)
			{
				MajorEmpire majorEmpire = base.Empire as MajorEmpire;
				list.AddRange(from m in majorEmpire.TamedKaijus
				select m.KaijuGarrison);
			}
			return list.AsReadOnly();
		}
	}

	public bool DoesRazingDetroyRegionBuilding { get; private set; }

	public bool ShowTerraformFIDSI { get; set; }

	public City MainCity
	{
		get
		{
			return this.mainCity;
		}
		private set
		{
			if (this.mainCity != null && this.mainCity.SimulationObject != null && this.mainCity.SimulationObject.Tags != null && this.SimulationDescriptorDatabase != null)
			{
				SimulationDescriptor descriptor;
				if (this.SimulationDescriptorDatabase.TryGetValue(City.TagMainCity, out descriptor))
				{
					this.mainCity.RemoveDescriptor(descriptor);
				}
				else
				{
					this.mainCity.SimulationObject.Tags.RemoveTag(City.TagMainCity);
				}
			}
			this.mainCity = value;
			if (this.mainCity != null && this.mainCity.SimulationObject != null && this.mainCity.SimulationObject.Tags != null && this.SimulationDescriptorDatabase != null)
			{
				Diagnostics.Assert(this.mainCityGUID == GameEntityGUID.Zero || this.mainCityGUID == this.mainCity.GUID);
				this.mainCityGUID = this.mainCity.GUID;
				SimulationDescriptor descriptor2;
				if (this.SimulationDescriptorDatabase.TryGetValue(City.TagMainCity, out descriptor2))
				{
					if (!this.mainCity.SimulationObject.Tags.Contains(City.TagMainCity))
					{
						this.mainCity.AddDescriptor(descriptor2, false);
					}
				}
				else
				{
					this.mainCity.SimulationObject.Tags.AddTag(City.TagMainCity);
				}
			}
		}
	}

	public GameEntityGUID MainCityGUID
	{
		get
		{
			return this.mainCityGUID;
		}
	}

	public ReadOnlyCollection<OrderCreateDistrictImprovement> PendingExtensions
	{
		get
		{
			return this.pendingExtensions.AsReadOnly();
		}
	}

	public ReadOnlyCollection<OrderCreateConstructibleDistrict> PendingDistrictConstructions
	{
		get
		{
			return this.pendingDistrictConstructions.AsReadOnly();
		}
	}

	public int TurnWhenMilitiaWasLastUpdated { get; set; }

	private IDatabase<FactionTrait> FactionTraitDatabase { get; set; }

	private IDatabase<Faction> FactionDatabase { get; set; }

	[Service]
	private IEventService EventService { get; set; }

	[Ancillary]
	private IGameEntityRepositoryService GameEntityRepositoryService { get; set; }

	[Service]
	private IGameService GameService { get; set; }

	private IDatabase<SimulationDescriptor> SimulationDescriptorDatabase { get; set; }

	private IDatabase<TerrainTypeMapping> TerrainTypeMappingDatabase { get; set; }

	private IDatabase<AnomalyTypeMapping> AnomalyTypeMappingDatabase { get; set; }

	private IDatabase<BiomeTypeMapping> BiomeTypeMappingDatabase { get; set; }

	private IDatabase<RiverTypeMapping> RiverTypeMappingDatabase { get; set; }

	private IVisibilityService VisibilityService { get; set; }

	[Ancillary]
	private IWorldPositionningService WorldPositionningService { get; set; }

	[Ancillary]
	private IWorldEffectService WorldEffectService { get; set; }

	public static float ComputeGrowthLimit(SimulationObject contextSimulationObject, float population)
	{
		if (DepartmentOfTheInterior.growthInterpreterContext == null)
		{
			DepartmentOfTheInterior.growthInterpreterContext = new InterpreterContext(null);
			string value = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<string>("Gameplay/Agencies/DepartmentOfTheInterior/GrowthFormula");
			DepartmentOfTheInterior.growthFormulaTokens = Interpreter.InfixTransform(value);
		}
		InterpreterContext obj = DepartmentOfTheInterior.growthInterpreterContext;
		float result;
		lock (obj)
		{
			DepartmentOfTheInterior.growthInterpreterContext.SimulationObject = DepartmentOfTheInterior.empireSimulationPath.GetFirstValidatedObject(contextSimulationObject);
			DepartmentOfTheInterior.growthInterpreterContext.Register("Population", population);
			result = (float)Interpreter.Execute(DepartmentOfTheInterior.growthFormulaTokens, DepartmentOfTheInterior.growthInterpreterContext);
		}
		return result;
	}

	public bool CanColonizeRegion(WorldPosition worldPosition, ArmyAction armyAction, bool silent = true)
	{
		Region region = this.WorldPositionningService.GetRegion(worldPosition);
		if (region.IsRegionColonized())
		{
			if (!silent)
			{
				Diagnostics.LogError("Cannot colonize because region is already colonized.");
			}
			return false;
		}
		if (!this.CanConstructAtWorldPosition(worldPosition, base.Empire.Index))
		{
			if (!silent)
			{
				Diagnostics.LogError("Cannot colonize because because world position is not constructible.");
			}
			return false;
		}
		return true;
	}

	public bool CanConstructAtWorldPosition(WorldPosition worldPosition, int empireIndex)
	{
		int bits = 1 << empireIndex;
		return this.WorldPositionningService.IsConstructible(worldPosition, bits);
	}

	public void ComputeCityPopulation(City city, bool notifyEventPopulationModified = false)
	{
		city.Refresh(false);
		float num = city.GetPropertyValue(SimulationProperties.Population);
		float num2 = city.GetPropertyBaseValue(SimulationProperties.Population);
		float num3 = num;
		float num4;
		if (!this.departmentOfTheTreasury.TryGetResourceStockValue(city, DepartmentOfTheTreasury.Resources.CityGrowth, out num4, false))
		{
			Diagnostics.LogError("Can't get resource stock value {0} on simulation object {1}.", new object[]
			{
				DepartmentOfTheTreasury.Resources.CityGrowth,
				city
			});
		}
		float num5;
		float num6;
		this.GetGrowthLimits(num, out num5, out num6);
		for (int i = 0; i < this.specializedPopulations.Count; i++)
		{
			this.specializedPopulations[i].PerPopulationValue = city.GetPropertyValue(this.specializedPopulations[i].PerPopulationNetPropertyName);
			if (this.specializedPopulations[i].PerPopulationValue <= 0f)
			{
				this.specializedPopulations[i].Value = -1f;
			}
			else
			{
				this.specializedPopulations[i].Value = city.GetPropertyValue(this.specializedPopulations[i].PropertyName);
			}
		}
		int num7 = 0;
		if (num4 < num5)
		{
			num7 = -1;
		}
		else if (num4 >= num6)
		{
			num7 = 1;
		}
		float num8 = city.GetPropertyValue(SimulationProperties.PopulationEfficiencyLimit);
		while (num7 != 0)
		{
			num += (float)num7;
			if (num <= 0f)
			{
				num = 1f;
				break;
			}
			num2 += (float)num7;
			num8 += (float)num7;
			this.MovePopulation(num7, num8);
			this.GetGrowthLimits(num, out num5, out num6);
			num7 = 0;
			if (num4 < num5)
			{
				num7 = -1;
			}
			else if (num4 > num6)
			{
				num7 = 1;
			}
		}
		city.SetPropertyBaseValue(SimulationProperties.Population, num2);
		for (int j = 0; j < this.specializedPopulations.Count; j++)
		{
			if (this.specializedPopulations[j].Value < 0f)
			{
				city.SetPropertyBaseValue(this.specializedPopulations[j].PropertyName, 0f);
			}
			else
			{
				city.SetPropertyBaseValue(this.specializedPopulations[j].PropertyName, this.specializedPopulations[j].Value);
			}
		}
		city.Refresh(false);
		if (Math.Abs(num3 - num) > 1.401298E-45f)
		{
			if (this.PopulationRepartitionChanged != null)
			{
				this.PopulationRepartitionChanged(this, new PopulationRepartitionEventArgs(city));
			}
			if (notifyEventPopulationModified)
			{
				this.EventService.Notify(new EventPopulationModified(base.Empire, city, num3, num));
			}
		}
	}

	public void ComputeOwnership(City city)
	{
		float num = city.Ownership[city.Empire.Index];
		if (num < 1f)
		{
			List<int> list = new List<int>();
			global::Game game = this.GameService.Game as global::Game;
			for (int i = 0; i < game.Empires.Length; i++)
			{
				if (game.Empires[i] is MinorEmpire || game.Empires[i] is NavalEmpire)
				{
					break;
				}
				if (i != city.Empire.Index && city.Ownership[i] > 1.401298E-45f)
				{
					list.Add(i);
				}
			}
			if (list.Count == 0)
			{
				city.GiveFullOwnershipToEmpire(city.Empire.Index);
				return;
			}
			float propertyValue = city.GetPropertyValue(SimulationProperties.OwnershipRecoveryRate);
			city.Ownership[city.Empire.Index] = num + propertyValue;
			if (city.Ownership[city.Empire.Index] >= 1f || Mathf.Approximately(city.Ownership[city.Empire.Index], 1f))
			{
				city.GiveFullOwnershipToEmpire(city.Empire.Index);
				return;
			}
			float num2 = city.Ownership[city.Empire.Index] - num;
			int num3 = 0;
			for (;;)
			{
				bool flag = false;
				float num4 = num2 / (float)list.Count;
				for (int j = list.Count - 1; j >= 0; j--)
				{
					int num5 = list[j];
					if (city.Ownership[num5] <= num4)
					{
						num2 -= city.Ownership[num5];
						city.Ownership[num5] = 0f;
						list.RemoveAt(j);
						flag = true;
					}
				}
				if (num3 > 8)
				{
					break;
				}
				num3++;
				if (!flag || list.Count <= 0)
				{
					goto IL_1F3;
				}
			}
			Diagnostics.LogWarning("Wasn't able to divide ownership loss, you may have an old save or have badly modified it.");
			city.GiveFullOwnershipToEmpire(city.Empire.Index);
			return;
			IL_1F3:
			float num6 = city.Ownership[city.Empire.Index];
			if (list.Count > 0)
			{
				float num4 = num2 / (float)list.Count;
				for (int k = 0; k < list.Count; k++)
				{
					int num7 = list[k];
					num2 -= num4;
					city.Ownership[num7] -= num4;
					num6 += city.Ownership[num7];
				}
			}
			float num8 = 1f - num6;
			if (Math.Abs(num8) > 1.401298E-45f)
			{
				city.Ownership[city.Empire.Index] += num8;
			}
			city.SetPropertyBaseValue(SimulationProperties.Ownership, city.Ownership[city.Empire.Index]);
			city.Refresh(false);
		}
	}

	public CityImprovement CreateCityImprovement(ConstructibleElement constructibleElement, GameEntityGUID cityImprovementGUID)
	{
		CityImprovementDefinition cityImprovementDefinition = constructibleElement as CityImprovementDefinition;
		Diagnostics.Assert(cityImprovementDefinition != null, "Can't retrieve the cityImprovementDefinition.");
		CityImprovement cityImprovement = new CityImprovement(cityImprovementGUID)
		{
			CityImprovementDefinition = cityImprovementDefinition
		};
		this.GameEntityRepositoryService.Swap(cityImprovement);
		IDatabase<SimulationDescriptor> database = Databases.GetDatabase<SimulationDescriptor>(false);
		if (database != null)
		{
			SimulationDescriptor descriptor = null;
			if (database.TryGetValue("ClassImprovement", out descriptor))
			{
				cityImprovement.AddDescriptor(descriptor, false);
			}
		}
		for (int i = 0; i < constructibleElement.Descriptors.Length; i++)
		{
			SimulationDescriptor simulationDescriptor = constructibleElement.Descriptors[i];
			if (simulationDescriptor != null)
			{
				cityImprovement.AddDescriptor(simulationDescriptor, false);
			}
		}
		return cityImprovement;
	}

	public void CreateMainCityAtWorldPosition(WorldPosition worldPosition)
	{
		Region region = this.WorldPositionningService.GetRegion(worldPosition);
		GameEntityGUID guid = this.GameEntityRepositoryService.GenerateGUID();
		GameEntityGUID districtGUID = this.GameEntityRepositoryService.GenerateGUID();
		byte terrainType = this.WorldPositionningService.GetTerrainType(worldPosition);
		StaticString terrainTypeMappingName = this.WorldPositionningService.GetTerrainTypeMappingName(terrainType);
		byte biomeType = this.WorldPositionningService.GetBiomeType(worldPosition);
		StaticString biomeTypeMappingName = this.WorldPositionningService.GetBiomeTypeMappingName(biomeType);
		byte anomalyType = this.WorldPositionningService.GetAnomalyType(worldPosition);
		StaticString anomalyTypeMappingName = this.WorldPositionningService.GetAnomalyTypeMappingName(anomalyType);
		short riverId = this.WorldPositionningService.GetRiverId(worldPosition);
		StaticString riverTypeMappingName = this.WorldPositionningService.GetRiverTypeMappingName(riverId);
		City city = this.CreateCity(guid, worldPosition, districtGUID, GameEntityGUID.Zero, terrainTypeMappingName, biomeTypeMappingName, anomalyTypeMappingName, riverTypeMappingName);
		int num = 6;
		for (int i = 0; i < num; i++)
		{
			WorldPosition neighbourTile = this.WorldPositionningService.GetNeighbourTile(worldPosition, (WorldOrientation)i, 1);
			if (neighbourTile.IsValid)
			{
				int regionIndex = (int)this.WorldPositionningService.GetRegionIndex(neighbourTile);
				if (region.Index == regionIndex)
				{
					if (this.WorldPositionningService.IsExploitable(neighbourTile, 0))
					{
						GameEntityGUID guid2 = this.GameEntityRepositoryService.GenerateGUID();
						terrainType = this.WorldPositionningService.GetTerrainType(neighbourTile);
						terrainTypeMappingName = this.WorldPositionningService.GetTerrainTypeMappingName(terrainType);
						biomeType = this.WorldPositionningService.GetBiomeType(neighbourTile);
						biomeTypeMappingName = this.WorldPositionningService.GetBiomeTypeMappingName(biomeType);
						anomalyType = this.WorldPositionningService.GetAnomalyType(neighbourTile);
						anomalyTypeMappingName = this.WorldPositionningService.GetAnomalyTypeMappingName(anomalyType);
						riverId = this.WorldPositionningService.GetRiverId(neighbourTile);
						riverTypeMappingName = this.WorldPositionningService.GetRiverTypeMappingName(riverId);
						District district = this.CreateDistrict(guid2, neighbourTile, DistrictType.Exploitation, terrainTypeMappingName, biomeTypeMappingName, anomalyTypeMappingName, riverTypeMappingName);
						city.AddDistrict(district);
					}
				}
			}
		}
		city.SetPropertyBaseValue(SimulationProperties.Population, 3f);
		city.SetPropertyBaseValue(SimulationProperties.CityOwnedTurn, (float)(this.GameService.Game as global::Game).Turn);
		DepartmentOfIndustry agency = base.Empire.GetAgency<DepartmentOfIndustry>();
		if (agency != null)
		{
			agency.AddQueueTo<City>(city);
		}
		this.AddCity(city, true, true);
		float growthLimit = this.GetGrowthLimit(city.GetPropertyValue(SimulationProperties.Population));
		float num2;
		if (!this.departmentOfTheTreasury.TryGetResourceStockValue(city.SimulationObject, DepartmentOfTheTreasury.Resources.CityGrowth, out num2, false))
		{
			Diagnostics.LogError("Can't get resource stock value {0} on simulation object {1}.", new object[]
			{
				DepartmentOfTheTreasury.Resources.CityGrowth,
				city.SimulationObject
			});
		}
		float amount = growthLimit - num2;
		this.departmentOfTheTreasury.TryTransferResources(city.SimulationObject, DepartmentOfTheTreasury.Resources.CityGrowth, amount);
		SimulationDescriptor descriptor;
		if (base.Empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitMimics2) && !city.SimulationObject.Tags.Contains(City.MimicsCity) && this.SimulationDescriptorDatabase.TryGetValue(City.MimicsCity, out descriptor))
		{
			city.AddDescriptor(descriptor, false);
		}
		this.AddDistrictDescriptorExploitableResource(city);
		this.MainCity = city;
	}

	public void FortifyCityByAmountInPercent(City cityToFortify, float amount)
	{
		float num = 1f;
		if (DepartmentOfTheInterior.FortificationRecoverByOwnershipCurve != null)
		{
			num = DepartmentOfTheInterior.FortificationRecoverByOwnershipCurve.Evaluate(cityToFortify.Ownership[base.Empire.Index]);
		}
		float propertyValue = cityToFortify.GetPropertyValue(SimulationProperties.MaximumCityDefensePoint);
		float num2 = cityToFortify.GetPropertyValue(SimulationProperties.CityDefensePoint);
		float num3 = propertyValue * num;
		num2 = Math.Max(1f, Math.Min(num3, num2 + num3 * amount));
		cityToFortify.SetPropertyBaseValue(SimulationProperties.CityDefensePoint, num2);
		if (cityToFortify != null)
		{
			cityToFortify.Refresh(false);
		}
	}

	public PointOfInterestImprovementDefinition GetBestImprovementDefinition(SimulationObject context, PointOfInterest pointOfInterest, PointOfInterestImprovementDefinition bestPointOfInterestDefinition, List<StaticString> lastFailureFlags)
	{
		if (bestPointOfInterestDefinition == null)
		{
			return null;
		}
		int num = 0;
		PointOfInterestImprovementDefinition pointOfInterestImprovementDefinition = bestPointOfInterestDefinition;
		while (!StaticString.IsNullOrEmpty(pointOfInterestImprovementDefinition.NextUpgradeName))
		{
			DepartmentOfIndustry.ConstructibleElement constructibleElement;
			if (!this.departmentOfIndustry.ConstructibleElementDatabase.TryGetValue(pointOfInterestImprovementDefinition.NextUpgradeName, out constructibleElement))
			{
				Diagnostics.LogWarning("The point of interest improvement '{0}' has an invalid constructible '{1}' as next upgrade.", new object[]
				{
					bestPointOfInterestDefinition.Name,
					bestPointOfInterestDefinition.NextUpgradeName
				});
				break;
			}
			pointOfInterestImprovementDefinition = (constructibleElement as PointOfInterestImprovementDefinition);
			if (pointOfInterestImprovementDefinition == null)
			{
				Diagnostics.LogWarning("The point of interest improvement '{0}' has an invalid constructible '{1}' as next upgrade.", new object[]
				{
					bestPointOfInterestDefinition.Name,
					bestPointOfInterestDefinition.NextUpgradeName
				});
				break;
			}
			if (pointOfInterestImprovementDefinition.Name == bestPointOfInterestDefinition.Name)
			{
				Diagnostics.LogWarning("The point of interest improvement '{0}' has himself as next upgrade.", new object[]
				{
					bestPointOfInterestDefinition.Name
				});
				break;
			}
			if (pointOfInterestImprovementDefinition.PointOfInterestTemplateName != pointOfInterest.PointOfInterestDefinition.PointOfInterestTemplateName)
			{
				Diagnostics.LogWarning("The point of interest improvement '{0}' has a constructible '{1}' which does not apply to the point template '{2}' as next upgrade.", new object[]
				{
					bestPointOfInterestDefinition.Name,
					bestPointOfInterestDefinition.NextUpgradeName,
					pointOfInterest.PointOfInterestDefinition.PointOfInterestTemplateName
				});
				break;
			}
			lastFailureFlags.Clear();
			DepartmentOfTheTreasury.CheckConstructiblePrerequisites(context, pointOfInterestImprovementDefinition, ref lastFailureFlags, new string[]
			{
				ConstructionFlags.Prerequisite
			});
			if (!lastFailureFlags.Contains(ConstructionFlags.Discard))
			{
				bestPointOfInterestDefinition = pointOfInterestImprovementDefinition;
				num++;
				if (num > 20)
				{
					bestPointOfInterestDefinition = null;
					Diagnostics.LogWarning("The point of interest improvement '{0}' has a loop in his upgrade hierarchy.", new object[]
					{
						bestPointOfInterestDefinition.Name
					});
					break;
				}
			}
		}
		return bestPointOfInterestDefinition;
	}

	public City GetClosestCityFromWorldPosition(WorldPosition worldPosition, bool allowBesiegedCity = true)
	{
		int num = int.MaxValue;
		City result = null;
		for (int i = 0; i < this.cities.Count; i++)
		{
			if (allowBesiegedCity || this.cities[i].BesiegingEmpire == null)
			{
				int distance = this.WorldPositionningService.GetDistance(this.cities[i].WorldPosition, worldPosition);
				if (distance < num)
				{
					result = this.cities[i];
					num = distance;
				}
			}
		}
		return result;
	}

	public City GetCity(GameEntityGUID cityGUID)
	{
		for (int i = 0; i < this.cities.Count; i++)
		{
			City city = this.cities[i];
			if (city.GUID == cityGUID)
			{
				return city;
			}
		}
		return null;
	}

	public void GetGrowthLimits(float population, out float minimumGrowth, out float maximumGrowth)
	{
		minimumGrowth = this.GetGrowthLimit(population);
		maximumGrowth = this.GetGrowthLimit(population + 1f);
	}

	public void GetListOfRegionColonized(out List<int> listOfRegion)
	{
		listOfRegion = new List<int>();
		for (int i = 0; i < this.cities.Count; i++)
		{
			listOfRegion.Add(this.cities[i].Region.Index);
		}
	}

	public bool IsAltarBuilt()
	{
		for (int i = 0; i < this.Cities.Count; i++)
		{
			City city = this.Cities[i];
			int j = 0;
			int count = city.Districts.Count;
			while (j < count)
			{
				if (city.Districts[j].SimulationObject.Tags.Contains(GameAltarOfAurigaScreen.DistrictImprovementAltarOfAurigaTagName))
				{
					return true;
				}
				j++;
			}
		}
		return false;
	}

	internal void VerifyOverallPopulation(City city)
	{
		city.Refresh(true);
		float num = city.GetPropertyValue(SimulationProperties.Population);
		num += city.GetPropertyValue(SimulationProperties.PopulationBonus);
		float num2 = 0f;
		for (int i = 0; i < this.specializedPopulations.Count; i++)
		{
			this.specializedPopulations[i].PerPopulationValue = city.GetPropertyValue(this.specializedPopulations[i].PerPopulationNetPropertyName);
			if (this.specializedPopulations[i].PerPopulationValue <= 0f)
			{
				this.specializedPopulations[i].Value = -1f;
			}
			else
			{
				this.specializedPopulations[i].Value = city.GetPropertyValue(this.specializedPopulations[i].PropertyName);
				num2 += this.specializedPopulations[i].Value;
			}
		}
		int num3 = Mathf.RoundToInt(num - num2);
		float propertyValue = city.GetPropertyValue(SimulationProperties.PopulationEfficiencyLimit);
		while (num3 != 0)
		{
			int num4;
			if (num3 > 0)
			{
				num4 = 1;
			}
			else
			{
				num4 = -1;
			}
			num3 -= num4;
			this.MovePopulation(num4, propertyValue);
		}
		for (int j = 0; j < this.specializedPopulations.Count; j++)
		{
			if (this.specializedPopulations[j].Value < 0f)
			{
				city.SetPropertyBaseValue(this.specializedPopulations[j].PropertyName, 0f);
			}
			else
			{
				city.SetPropertyBaseValue(this.specializedPopulations[j].PropertyName, this.specializedPopulations[j].Value);
			}
		}
	}

	internal virtual void OnEmpireEliminated(global::Empire empire, bool authorized)
	{
		if (empire.Index == base.Empire.Index)
		{
			this.UnoccupyFortresses();
			this.DestroyInfectedCities();
		}
	}

	protected override IEnumerator OnInitialize()
	{
		yield return base.OnInitialize();
		this.SimulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
		Diagnostics.Assert(this.SimulationDescriptorDatabase != null);
		if (DepartmentOfTheInterior.SimulationDescriptorDatabaseStatic == null)
		{
			DepartmentOfTheInterior.SimulationDescriptorDatabaseStatic = this.SimulationDescriptorDatabase;
		}
		this.AnomalyTypeMappingDatabase = Databases.GetDatabase<AnomalyTypeMapping>(false);
		Diagnostics.Assert(this.AnomalyTypeMappingDatabase != null);
		if (DepartmentOfTheInterior.AnomalyTypeMappingDatabaseStatic == null)
		{
			DepartmentOfTheInterior.AnomalyTypeMappingDatabaseStatic = this.AnomalyTypeMappingDatabase;
		}
		this.BiomeTypeMappingDatabase = Databases.GetDatabase<BiomeTypeMapping>(false);
		Diagnostics.Assert(this.BiomeTypeMappingDatabase != null);
		if (DepartmentOfTheInterior.BiomeTypeMappingDatabaseStatic == null)
		{
			DepartmentOfTheInterior.BiomeTypeMappingDatabaseStatic = this.BiomeTypeMappingDatabase;
		}
		this.RiverTypeMappingDatabase = Databases.GetDatabase<RiverTypeMapping>(false);
		Diagnostics.Assert(this.RiverTypeMappingDatabase != null);
		if (DepartmentOfTheInterior.RiverTypeMappingDatabaseStatic == null)
		{
			DepartmentOfTheInterior.RiverTypeMappingDatabaseStatic = this.RiverTypeMappingDatabase;
		}
		this.TerrainTypeMappingDatabase = Databases.GetDatabase<TerrainTypeMapping>(false);
		Diagnostics.Assert(this.TerrainTypeMappingDatabase != null);
		if (DepartmentOfTheInterior.TerrainTypeMappingDatabaseStatic == null)
		{
			DepartmentOfTheInterior.TerrainTypeMappingDatabaseStatic = this.TerrainTypeMappingDatabase;
		}
		this.FactionTraitDatabase = Databases.GetDatabase<FactionTrait>(false);
		Diagnostics.Assert(this.FactionTraitDatabase != null);
		this.FactionDatabase = Databases.GetDatabase<Faction>(false);
		Diagnostics.Assert(this.FactionDatabase != null);
		this.GameService = Services.GetService<IGameService>();
		if (DepartmentOfTheInterior.Game == null)
		{
			DepartmentOfTheInterior.Game = (this.GameService.Game as global::Game);
		}
		this.GameEntityRepositoryService = this.GameService.Game.Services.GetService<IGameEntityRepositoryService>();
		if (this.GameEntityRepositoryService == null)
		{
			Diagnostics.LogError("Failed to retrieve the game entity repository service.");
		}
		if (DepartmentOfTheInterior.GameEntityRepositoryServiceStatic == null)
		{
			DepartmentOfTheInterior.GameEntityRepositoryServiceStatic = this.GameEntityRepositoryService;
		}
		this.WorldPositionningService = this.GameService.Game.Services.GetService<IWorldPositionningService>();
		if (this.WorldPositionningService == null)
		{
			Diagnostics.LogError("Failed to retrieve the world positionning service.");
		}
		if (DepartmentOfTheInterior.WorldPositionningServiceStatic == null)
		{
			DepartmentOfTheInterior.WorldPositionningServiceStatic = this.WorldPositionningService;
		}
		this.playerControllerRepositoryService = this.GameService.Game.Services.GetService<IPlayerControllerRepositoryService>();
		if (this.playerControllerRepositoryService == null)
		{
			Diagnostics.LogError("Failed to retrieve the player controller repository service.");
		}
		this.WorldEffectService = this.GameService.Game.Services.GetService<IWorldEffectService>();
		if (this.WorldEffectService == null)
		{
			Diagnostics.LogError("Failed to retrieve the WorldEffect service.");
		}
		if (DepartmentOfTheInterior.WorldEffectServiceStatic == null)
		{
			DepartmentOfTheInterior.WorldEffectServiceStatic = this.WorldEffectService;
		}
		this.EventService = Services.GetService<IEventService>();
		if (this.EventService == null)
		{
			Diagnostics.LogError("Failed to retrieve the event service.");
		}
		this.EventService.EventRaise += this.EventService_EventRaise;
		if (DepartmentOfTheInterior.PathfindingServiceStatic == null)
		{
			IPathfindingService pathFindingService = this.GameService.Game.Services.GetService<IPathfindingService>();
			DepartmentOfTheInterior.PathfindingServiceStatic = pathFindingService;
		}
		this.departmentOfTheTreasury = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		Diagnostics.Assert(this.departmentOfTheTreasury != null, "Department of the interior can't get the department of treasury.");
		this.departmentOfForeignAffairs = base.Empire.GetAgency<DepartmentOfForeignAffairs>();
		Diagnostics.Assert(this.departmentOfForeignAffairs != null, "Department of the interior can't get the department of foreign affairs.");
		this.departmentOfForeignAffairs.DiplomaticRelationStateChange += this.DepartmentOfForeignAffairs_DiplomaticRelationStateChange;
		IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
		if (downloadableContentService != null && downloadableContentService.IsShared(DownloadableContent11.ReadOnlyName))
		{
			this.departmentOfIntelligence = base.Empire.GetAgency<DepartmentOfIntelligence>();
			Diagnostics.Assert(this.departmentOfIntelligence != null, "Department of the interior can't get the department of intelligence.");
		}
		this.departmentOfScience = base.Empire.GetAgency<DepartmentOfScience>();
		Diagnostics.Assert(this.departmentOfScience != null, "Department of the interior can't get the department of science.");
		this.departmentOfScience.TechnologyUnlocked += this.DepartmentOfScience_TechnologyUnlocked;
		this.departmentOfIndustry = base.Empire.GetAgency<DepartmentOfIndustry>();
		Diagnostics.Assert(this.departmentOfScience != null, "Department of the interior can't get the department of industry.");
		this.seasonService = this.GameService.Game.Services.GetService<ISeasonService>();
		Diagnostics.Assert(this.seasonService != null);
		base.Empire.RegisterPass("GameClientState_Turn_Begin", "UpdateMilitia", new Agency.Action(this.GameClientState_Turn_Begin_UpdateMilitia), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_Begin", "UpdateOwnership", new Agency.Action(this.GameClientState_Turn_Begin_UpdateOwnership), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_Begin", "RefreshDefensiveTowerPower", new Agency.Action(this.GameClientState_Turn_Begin_RefreshDefensiveTowerPower), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_End", "ComputeCityDefensePoint", new Agency.Action(this.GameClient_Turn_End_ComputeCityDefensePoint), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_End", "ComputeCityPopulation", new Agency.Action(this.GameClient_Turn_End_ComputeCityPopulation), new string[]
		{
			"CollectResources",
			"ComputeProduction"
		});
		base.Empire.RegisterPass("GameClientState_Turn_End", "CityUnitExperiencePerTurnGain", new Agency.Action(this.GameClientState_Turn_End_UnitExperiencePerTurnGain), new string[]
		{
			"ComputeCityDefensePoint"
		});
		base.Empire.RegisterPass("GameClientState_Turn_End", "CityUnitHealthPerTurnGain", new Agency.Action(this.GameClientState_Turn_End_UnitHealthPerTurnGain), new string[]
		{
			"ComputeCityDefensePoint"
		});
		base.Empire.RegisterPass("GameClientState_Turn_End", "PopulationBuyoutCooldown", new Agency.Action(this.GameClientState_Turn_End_PopulationBuyoutCooldown), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_End", "ComputeRoundUpProgress", new Agency.Action(this.GameClient_Turn_End_ComputeRoundUpProgress), new string[0]);
		base.Empire.RegisterPass("GameServerState_Turn_Begin", "DestroyRazedCities", new Agency.Action(this.GameServerState_Turn_Begin_DestroyRazedCities), new string[0]);
		base.Empire.RegisterPass("GameServerState_Turn_Begin", "UpdateMilitia", new Agency.Action(this.GameServerState_Turn_Begin_UpdateMilitia), new string[]
		{
			"DestroyRazedCities"
		});
		base.Empire.RegisterPass("GameServerState_Turn_Ended", "ExecutePendingDistrictConstruction", new Agency.Action(this.GameServerState_Turn_Ended_ExecutePendingDistrictConstruction), new string[0]);
		base.Empire.RegisterPass("GameServerState_Turn_Ended", "ExecutePendingExtension", new Agency.Action(this.GameServerState_Turn_Ended_ExecutePendingExtension), new string[0]);
		base.Empire.RegisterPass("GameServerState_Turn_Ended", "CheckRoundUpExecution", new Agency.Action(this.GameServer_Turn_Ended_CheckRoundUpExecution), new string[0]);
		this.seasonService.SeasonChange += this.OnSeasonChange;
		this.departmentOfIndustry.AddConstructionChangeEventHandler<BoosterGeneratorDefinition>(new DepartmentOfIndustry.ConstructionChangeEventHandler(this.DepartmentOfIndustry_BoosterGeneratorConstructionChange));
		this.departmentOfIndustry.AddConstructionChangeEventHandler<CityConstructibleActionDefinition>(new DepartmentOfIndustry.ConstructionChangeEventHandler(this.DepartmentOfIndustry_CityConstructibleActionConstructionChange));
		this.departmentOfIndustry.AddConstructionChangeEventHandler<ConstructibleDistrictDefinition>(new DepartmentOfIndustry.ConstructionChangeEventHandler(this.DepartmentOfIndustry_ConstructibleDistrictConstructionChange));
		this.departmentOfIndustry.AddConstructionChangeEventHandler<CityImprovementDefinition>(new DepartmentOfIndustry.ConstructionChangeEventHandler(this.DepartmentOfIndustry_CityImprovementConstructionChange));
		this.departmentOfIndustry.AddConstructionChangeEventHandler<CoastalDistrictImprovementDefinition>(new DepartmentOfIndustry.ConstructionChangeEventHandler(this.DepartmentOfIndustry_DistrictImprovementConstructionChange));
		this.departmentOfIndustry.AddConstructionChangeEventHandler<DistrictImprovementDefinition>(new DepartmentOfIndustry.ConstructionChangeEventHandler(this.DepartmentOfIndustry_DistrictImprovementConstructionChange));
		this.departmentOfIndustry.AddConstructionChangeEventHandler<FreeDistrictImprovementDefinition>(new DepartmentOfIndustry.ConstructionChangeEventHandler(this.DepartmentOfIndustry_DistrictImprovementConstructionChange));
		this.departmentOfIndustry.AddConstructionChangeEventHandler<PointOfInterestImprovementDefinition>(new DepartmentOfIndustry.ConstructionChangeEventHandler(this.DepartmentOfIndustry_PointOfInterestImprovementConstructionChange));
		global::Empire empire = base.Empire as global::Empire;
		this.factionTraitReferenceCount = new Dictionary<FactionTrait, int>();
		this.assimilatedFactionSimulationObject = new SimulationObject("AssimilatedFactions");
		SimulationDescriptor simulationDescriptor;
		if (this.SimulationDescriptorDatabase.TryGetValue("EmpireTypeMinor", out simulationDescriptor))
		{
			this.assimilatedFactionSimulationObject.AddDescriptor(simulationDescriptor);
		}
		IEnumerable<FactionTrait> enumerableFactionTraits = Faction.EnumerableTraits(empire.Faction);
		foreach (FactionTrait factionTrait in enumerableFactionTraits)
		{
			this.factionTraitReferenceCount.Add(factionTrait, 1);
		}
		this.endTurnService = Services.GetService<IEndTurnService>();
		Diagnostics.Assert(this.endTurnService != null);
		this.endTurnService.RegisterValidator(new Func<bool, bool>(this.EndTurnValidator));
		Diagnostics.Assert(this.specializedPopulations != null);
		this.specializedPopulations.Add(new DepartmentOfTheInterior.SpecializedPopulation(SimulationProperties.FoodPopulation, SimulationProperties.BaseFoodPerPopulation, 0, 0f));
		this.specializedPopulations.Add(new DepartmentOfTheInterior.SpecializedPopulation(SimulationProperties.IndustryPopulation, SimulationProperties.BaseIndustryPerPopulation, 1, 0f));
		this.specializedPopulations.Add(new DepartmentOfTheInterior.SpecializedPopulation(SimulationProperties.SciencePopulation, SimulationProperties.BaseSciencePerPopulation, 2, 0f));
		this.specializedPopulations.Add(new DepartmentOfTheInterior.SpecializedPopulation(SimulationProperties.DustPopulation, SimulationProperties.BaseDustPerPopulation, 3, 0f));
		this.specializedPopulations.Add(new DepartmentOfTheInterior.SpecializedPopulation(SimulationProperties.CityPointPopulation, SimulationProperties.BaseCityPointPerPopulation, 4, 0f));
		this.VisibilityService = this.GameService.Game.Services.GetService<IVisibilityService>();
		this.VisibilityService.VisibilityRefreshed += this.VisibilityService_VisibilityRefreshed;
		this.DoesRazingDetroyRegionBuilding = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<bool>("Gameplay/Agencies/DepartmentOfTheInterior/DoesRazingDetroyRegionBuilding");
		this.uniques = new SimulationObjectWrapper("Uniques");
		this.uniques.SimulationObject.Tags.AddTag(DepartmentOfTheInterior.ReadOnlyUniques);
		base.Empire.AddChild(this.uniques);
		IDatabase<Amplitude.Unity.Framework.AnimationCurve> animationCurves = Databases.GetDatabase<Amplitude.Unity.Framework.AnimationCurve>(false);
		if (DepartmentOfTheInterior.MilitiaRecoverByOwnershipCurve == null)
		{
			animationCurves.TryGetValue("AnimationCurveMilitiaRecoverByOwnership", out DepartmentOfTheInterior.MilitiaRecoverByOwnershipCurve);
		}
		if (DepartmentOfTheInterior.FortificationRecoverByOwnershipCurve == null)
		{
			animationCurves.TryGetValue("AnimationCurveFortificationRecoverByOwnership", out DepartmentOfTheInterior.FortificationRecoverByOwnershipCurve);
		}
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		if (this.eventHandlers == null)
		{
			this.RegisterEventHandlers();
		}
		yield break;
	}

	protected override IEnumerator OnLoadGame(Amplitude.Unity.Game.Game game)
	{
		yield return base.OnLoadGame(game);
		for (int index = 0; index < this.cities.Count; index++)
		{
			City city = this.cities[index];
			if (city.LastNonInfectedOwnerIndex != -1)
			{
				city.LastNonInfectedOwner = DepartmentOfTheInterior.Game.Empires[city.LastNonInfectedOwnerIndex];
			}
			this.GameEntityRepositoryService.Register(city);
			for (int tileIndex = 0; tileIndex < city.Districts.Count; tileIndex++)
			{
				District district = city.Districts[tileIndex];
				this.GameEntityRepositoryService.Register(district);
			}
			ReadOnlyCollection<CityImprovement> cityImprovements = city.CityImprovements;
			for (int cityImprovementIndex = 0; cityImprovementIndex < cityImprovements.Count; cityImprovementIndex++)
			{
				CityImprovement cityImprovement = cityImprovements[cityImprovementIndex];
				this.GameEntityRepositoryService.Register(cityImprovement);
			}
			foreach (Unit unit in city.Units)
			{
				this.GameEntityRepositoryService.Register(unit);
			}
			if (city.Militia != null)
			{
				this.GameEntityRepositoryService.Register(city.Militia);
				foreach (Unit unit2 in city.Militia.Units)
				{
					this.GameEntityRepositoryService.Register(unit2);
				}
			}
			if (city.Camp != null)
			{
				this.GameEntityRepositoryService.Register(city.Camp);
				foreach (Unit unit3 in city.Camp.Units)
				{
					this.GameEntityRepositoryService.Register(unit3);
				}
				for (int campDistrictIndex = 0; campDistrictIndex < city.Camp.Districts.Count; campDistrictIndex++)
				{
					this.GameEntityRepositoryService.Register(city.Camp.Districts[campDistrictIndex]);
					city.Camp.Districts[campDistrictIndex].Refresh(false);
				}
			}
			for (int pointOfInterestIndex = 0; pointOfInterestIndex < city.Region.PointOfInterests.Length; pointOfInterestIndex++)
			{
				string propertyValue;
				if (city.Region.PointOfInterests[pointOfInterestIndex].PointOfInterestDefinition.TryGetValue("VisibilityTechnology", out propertyValue))
				{
					if (city.Region.PointOfInterests[pointOfInterestIndex].PointOfInterestImprovement != null || this.departmentOfScience.GetTechnologyState(propertyValue) == DepartmentOfScience.ConstructibleElement.State.Researched)
					{
						if (!city.SimulationObject.Children.Contains(city.Region.PointOfInterests[pointOfInterestIndex]))
						{
							city.AddChild(city.Region.PointOfInterests[pointOfInterestIndex]);
							city.Region.PointOfInterests[pointOfInterestIndex].Empire = (base.Empire as global::Empire);
						}
					}
				}
			}
			this.BindMinorFactionToCity(city, city.Region.MinorEmpire);
			if (this.MainCity != null && this.MainCity.GUID == city.GUID && base.Empire is MajorEmpire)
			{
				List<Village> convertedVillages = ((MajorEmpire)base.Empire).ConvertedVillages;
				if (convertedVillages != null)
				{
					for (int villageIndex = 0; villageIndex < convertedVillages.Count; villageIndex++)
					{
						Diagnostics.Assert(convertedVillages[villageIndex].HasBeenConverted);
						Diagnostics.Assert(convertedVillages[villageIndex].Converter == base.Empire);
						Diagnostics.Assert(convertedVillages[villageIndex].PointOfInterest != null);
						if (!city.SimulationObject.Children.Contains(convertedVillages[villageIndex].PointOfInterest))
						{
							city.AddChild(convertedVillages[villageIndex].PointOfInterest);
							convertedVillages[villageIndex].PointOfInterest.Empire = (global::Empire)base.Empire;
						}
					}
				}
			}
			SimulationDescriptor descriptor;
			if (base.Empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitMimics2) && !city.SimulationObject.Tags.Contains(City.MimicsCity) && this.SimulationDescriptorDatabase.TryGetValue(City.MimicsCity, out descriptor))
			{
				city.AddDescriptor(descriptor, false);
			}
			this.VerifyOverallPopulation(city);
			this.AddDistrictDescriptorExploitableResource(city);
			if (city.IsUnderEarthquake)
			{
				foreach (Army earthQuakeInstigator in DepartmentOfTheInterior.GetCityEarthquakeInstigators(city))
				{
					District districtCenter = city.GetDistrictCenter();
					if (districtCenter != null)
					{
						districtCenter.EmpireEarthquakeBits |= 1 << earthQuakeInstigator.Empire.Index;
					}
				}
			}
		}
		for (int index2 = 0; index2 < this.occupiedFortresses.Count; index2++)
		{
			this.AttachFortress(this.occupiedFortresses[index2]);
		}
		for (int index3 = 0; index3 < this.occupiedFortresses.Count; index3++)
		{
			this.AttachFortress(this.occupiedFortresses[index3]);
		}
		if (base.Empire.SimulationObject.Tags.Contains("FactionTraitCultists14"))
		{
			bool factionTraitCultists14ConversionPatched = false;
			for (int index4 = 0; index4 < this.Cities.Count; index4++)
			{
				if (this.Cities[index4].Region != null && this.Cities[index4].Region.MinorEmpire != null)
				{
					BarbarianCouncil barbarianCouncil = this.Cities[index4].Region.MinorEmpire.GetAgency<BarbarianCouncil>();
					if (barbarianCouncil != null)
					{
						foreach (Village village in barbarianCouncil.Villages)
						{
							Diagnostics.Assert(village.PointOfInterest != null);
							if (village.HasBeenPacified)
							{
								if (village.PointOfInterest.Empire != base.Empire)
								{
									village.PointOfInterest.Empire = (global::Empire)base.Empire;
									factionTraitCultists14ConversionPatched = true;
									Diagnostics.LogWarning("Fixed: village (name: '{0}', pacified) has been rebound to the empire (index: {1}, city: '{2}')", new object[]
									{
										village.Name,
										base.Empire.Index,
										this.Cities[index4].Name
									});
								}
								if (village.PointOfInterest.SimulationObject != null && village.PointOfInterest.SimulationObject.Parent != this.Cities[index4].SimulationObject)
								{
									this.Cities[index4].SimulationObject.AddChild(village.PointOfInterest.SimulationObject);
									factionTraitCultists14ConversionPatched = true;
									Diagnostics.LogWarning("Fixed: village's (name: '{0}', pacified) point of interest has been reattached to the city (empire index: {1}, city: '{2}')", new object[]
									{
										village.Name,
										base.Empire.Index,
										this.Cities[index4].Name
									});
								}
							}
							if (village.HasBeenConvertedByIndex == base.Empire.Index)
							{
								if (village.PointOfInterest.Empire != base.Empire)
								{
									village.PointOfInterest.Empire = (global::Empire)base.Empire;
									factionTraitCultists14ConversionPatched = true;
									Diagnostics.LogWarning("Fixed: village (name: '{0}', converted) has been rebound to the empire (index: {1})", new object[]
									{
										village.Name,
										base.Empire.Index
									});
								}
								if (this.MainCity != null && village.PointOfInterest != null && village.PointOfInterest.SimulationObject != null && village.PointOfInterest.SimulationObject.Parent != this.MainCity.SimulationObject)
								{
									this.MainCity.SimulationObject.AddChild(village.PointOfInterest.SimulationObject);
									factionTraitCultists14ConversionPatched = true;
									Diagnostics.LogWarning("Fixed: village's (name: '{0}', converted) point of interest has been reattached to the main city (empire index: {1}, city: '{2}')", new object[]
									{
										village.Name,
										base.Empire.Index,
										this.MainCity.Name
									});
								}
							}
						}
					}
				}
			}
			if (factionTraitCultists14ConversionPatched)
			{
				for (int index5 = 0; index5 < this.Cities.Count; index5++)
				{
					this.VerifyOverallPopulation(this.Cities[index5]);
				}
			}
		}
		for (int index6 = 0; index6 < this.assimilatedFactions.Count; index6++)
		{
			MinorFaction minorFaction = this.assimilatedFactions[index6] as MinorFaction;
			this.AssimilateMinorFactionEmpires(minorFaction);
			this.AssimilateMinorFactionTraits(minorFaction, false);
		}
		if (this.assimilatedFactions.Count > 0)
		{
			DepartmentOfDefense departmentOfDefense = base.Empire.GetAgency<DepartmentOfDefense>();
			if (departmentOfDefense != null)
			{
				departmentOfDefense.UnlockUnitDesign();
			}
		}
		if (this.OccupiedFortresses != null)
		{
			for (int index7 = 0; index7 < this.OccupiedFortresses.Count; index7++)
			{
				Fortress fortress = this.OccupiedFortresses[index7];
				fortress.Empire = null;
				Diagnostics.Assert(fortress.PointOfInterest != null);
				Diagnostics.Assert(fortress.PointOfInterest.Region != null);
				NavalEmpire navalEmpire = fortress.PointOfInterest.Region.NavalEmpire;
				Diagnostics.Assert(navalEmpire != null);
				PirateCouncil pirateCouncil = navalEmpire.GetAgency<PirateCouncil>();
				Diagnostics.Assert(pirateCouncil != null);
				pirateCouncil.RegisterFortress(fortress);
			}
		}
		if (!string.IsNullOrEmpty(this.serializableBesiegingSeafaringArmies))
		{
			try
			{
				string[] tokens = this.serializableBesiegingSeafaringArmies.Split(Amplitude.String.Separators, StringSplitOptions.RemoveEmptyEntries);
				for (int index8 = 0; index8 < tokens.Length; index8++)
				{
					string[] array = tokens;
					int num;
					index8 = (num = index8) + 1;
					GameEntityGUID gameEntityGuid = ulong.Parse(array[num]);
					City city2 = this.cities.First((City iterator) => iterator.GUID == gameEntityGuid);
					string[] array2 = tokens;
					index8 = (num = index8) + 1;
					int numberOfBesiegingSeafaringArmies = int.Parse(array2[num]);
					for (int jndex = 0; jndex < numberOfBesiegingSeafaringArmies; jndex++)
					{
						string[] array3 = tokens;
						index8 = (num = index8) + 1;
						int empireIndex = int.Parse(array3[num]);
						string[] array4 = tokens;
						index8 = (num = index8) + 1;
						gameEntityGuid = ulong.Parse(array4[num]);
						Army army = (game as global::Game).Empires[empireIndex].GetAgency<DepartmentOfDefense>().Armies.First((Army iterator) => iterator.GUID == gameEntityGuid);
						city2.BesiegingSeafaringArmies.Add(army);
					}
				}
			}
			catch
			{
			}
			this.serializableBesiegingSeafaringArmies = null;
		}
		yield break;
	}

	protected override void OnRelease()
	{
		base.OnRelease();
		for (int i = 0; i < this.cities.Count; i++)
		{
			this.cities[i].Dispose();
		}
		this.cities.Clear();
		if (this.MainCity != null)
		{
			this.MainCity.Dispose();
			this.MainCity = null;
		}
		if (this.departmentOfIndustry != null)
		{
			this.departmentOfIndustry.RemoveConstructionChangeEventHandler<BoosterGeneratorDefinition>(new DepartmentOfIndustry.ConstructionChangeEventHandler(this.DepartmentOfIndustry_BoosterGeneratorConstructionChange));
			this.departmentOfIndustry.RemoveConstructionChangeEventHandler<CityConstructibleActionDefinition>(new DepartmentOfIndustry.ConstructionChangeEventHandler(this.DepartmentOfIndustry_CityConstructibleActionConstructionChange));
			this.departmentOfIndustry.RemoveConstructionChangeEventHandler<ConstructibleDistrictDefinition>(new DepartmentOfIndustry.ConstructionChangeEventHandler(this.DepartmentOfIndustry_ConstructibleDistrictConstructionChange));
			this.departmentOfIndustry.RemoveConstructionChangeEventHandler<CityImprovementDefinition>(new DepartmentOfIndustry.ConstructionChangeEventHandler(this.DepartmentOfIndustry_CityImprovementConstructionChange));
			this.departmentOfIndustry.RemoveConstructionChangeEventHandler<CoastalDistrictImprovementDefinition>(new DepartmentOfIndustry.ConstructionChangeEventHandler(this.DepartmentOfIndustry_DistrictImprovementConstructionChange));
			this.departmentOfIndustry.RemoveConstructionChangeEventHandler<DistrictImprovementDefinition>(new DepartmentOfIndustry.ConstructionChangeEventHandler(this.DepartmentOfIndustry_DistrictImprovementConstructionChange));
			this.departmentOfIndustry.RemoveConstructionChangeEventHandler<FreeDistrictImprovementDefinition>(new DepartmentOfIndustry.ConstructionChangeEventHandler(this.DepartmentOfIndustry_DistrictImprovementConstructionChange));
			this.departmentOfIndustry.RemoveConstructionChangeEventHandler<PointOfInterestImprovementDefinition>(new DepartmentOfIndustry.ConstructionChangeEventHandler(this.DepartmentOfIndustry_PointOfInterestImprovementConstructionChange));
			this.departmentOfIndustry = null;
		}
		if (this.departmentOfTheTreasury != null)
		{
			this.departmentOfTheTreasury = null;
		}
		if (this.departmentOfForeignAffairs != null)
		{
			this.departmentOfForeignAffairs.DiplomaticRelationStateChange -= this.DepartmentOfForeignAffairs_DiplomaticRelationStateChange;
			this.departmentOfForeignAffairs = null;
		}
		if (this.uniques != null)
		{
			if (base.Empire != null)
			{
				base.Empire.RemoveChild(this.uniques);
			}
			this.uniques.Dispose();
			this.uniques = null;
		}
		if (this.assimilatedFactionSimulationObject != null)
		{
			base.Empire.SimulationObject.RemoveChild(this.assimilatedFactionSimulationObject);
			this.assimilatedFactionSimulationObject.Dispose();
			this.assimilatedFactionSimulationObject = null;
		}
		this.assimilatedFactions.Clear();
		if (this.VisibilityService != null)
		{
			this.VisibilityService.VisibilityRefreshed -= this.VisibilityService_VisibilityRefreshed;
			this.VisibilityService = null;
		}
		this.playerControllerRepositoryService = null;
		if (this.endTurnService != null)
		{
			this.endTurnService.UnregisterValidator(new Func<bool, bool>(this.EndTurnValidator));
			this.endTurnService = null;
		}
		if (this.EventService != null)
		{
			this.EventService.EventRaise -= this.EventService_EventRaise;
			this.EventService = null;
		}
		if (this.eventHandlers != null)
		{
			this.eventHandlers.Clear();
			this.eventHandlers = null;
		}
		this.SimulationDescriptorDatabase = null;
		this.AnomalyTypeMappingDatabase = null;
		this.BiomeTypeMappingDatabase = null;
		this.TerrainTypeMappingDatabase = null;
		this.FactionTraitDatabase = null;
		this.FactionDatabase = null;
		this.GameService = null;
		this.GameEntityRepositoryService = null;
		this.WorldPositionningService = null;
		this.departmentOfTheTreasury = null;
		this.departmentOfForeignAffairs = null;
		if (DepartmentOfTheInterior.growthInterpreterContext != null)
		{
			DepartmentOfTheInterior.growthInterpreterContext.Clear();
			DepartmentOfTheInterior.growthInterpreterContext.SimulationObject = null;
			DepartmentOfTheInterior.growthInterpreterContext = null;
		}
		if (DepartmentOfTheInterior.assimilationInterpreterContext != null)
		{
			DepartmentOfTheInterior.assimilationInterpreterContext.Clear();
			DepartmentOfTheInterior.assimilationInterpreterContext.SimulationObject = null;
			DepartmentOfTheInterior.assimilationInterpreterContext = null;
		}
		DepartmentOfTheInterior.SimulationDescriptorDatabaseStatic = null;
		DepartmentOfTheInterior.AnomalyTypeMappingDatabaseStatic = null;
		DepartmentOfTheInterior.WorldPositionningServiceStatic = null;
		DepartmentOfTheInterior.TerrainTypeMappingDatabaseStatic = null;
		DepartmentOfTheInterior.RiverTypeMappingDatabaseStatic = null;
		DepartmentOfTheInterior.BiomeTypeMappingDatabaseStatic = null;
		DepartmentOfTheInterior.WorldEffectServiceStatic = null;
		DepartmentOfTheInterior.GameEntityRepositoryServiceStatic = null;
		DepartmentOfTheInterior.PathfindingServiceStatic = null;
	}

	private void AddCity(City city, bool verifyPopulation = true, bool updateInfectionStatus = true)
	{
		if (city.Empire != null && city.Empire != base.Empire)
		{
			Diagnostics.LogError("The department of the interior was asked to add a city (guid: {0}, empire: {1}) but it is still bound to another empire.", new object[]
			{
				city.GUID,
				city.Empire.Name
			});
			return;
		}
		city.Empire = (global::Empire)base.Empire;
		if (city.Militia != null)
		{
			city.Militia.Empire = (global::Empire)base.Empire;
		}
		int num = this.cities.BinarySearch((City match) => match.GUID.CompareTo(city.GUID));
		if (num >= 0)
		{
			Diagnostics.LogWarning("The department of the interior was asked to add a city (guid: {0}) but it is already present in its list of cities.", new object[]
			{
				city.GUID
			});
			return;
		}
		this.cities.Insert(~num, city);
		base.Empire.SetPropertyBaseValue(SimulationProperties.NumberOfCities, base.Empire.GetPropertyValue(SimulationProperties.NumberOfCities) + 1f);
		Region region = this.WorldPositionningService.GetRegion(city.WorldPosition);
		region.City = city;
		city.Region = region;
		city.Refresh(true);
		for (int i = 0; i < region.PointOfInterests.Length; i++)
		{
			PointOfInterest pointOfInterest = region.PointOfInterests[i];
			string text;
			if (!pointOfInterest.PointOfInterestDefinition.TryGetValue("Type", out text) || !(text == "Village"))
			{
				if (!pointOfInterest.PointOfInterestDefinition.TryGetValue("VisibilityTechnology", out text) || pointOfInterest.PointOfInterestImprovement != null || this.departmentOfScience.GetTechnologyState(text) == DepartmentOfScience.ConstructibleElement.State.Researched)
				{
					city.AddChild(pointOfInterest);
					pointOfInterest.Refresh(true);
					pointOfInterest.Empire = (base.Empire as global::Empire);
				}
			}
		}
		base.Empire.AddChild(city);
		if (updateInfectionStatus)
		{
			city.UpdateInfectionStatus();
		}
		if (!city.IsInfected)
		{
			if (this.MainCity == null && (this.MainCityGUID == GameEntityGUID.Zero || this.MainCityGUID == city.GUID))
			{
				this.MainCity = city;
			}
		}
		else if (updateInfectionStatus)
		{
			global::Game game = this.GameService.Game as global::Game;
			for (int j = 0; j < game.Empires.Length; j++)
			{
				global::Empire empire = game.Empires[j];
				if (empire is MajorEmpire)
				{
					MajorEmpire majorEmpire = empire as MajorEmpire;
					DepartmentOfIntelligence agency = majorEmpire.GetAgency<DepartmentOfIntelligence>();
					if (majorEmpire.Index != city.Empire.Index)
					{
						agency.CheckStopInfiltrationAgainstGarrisonChange(city.GUID);
					}
				}
			}
		}
		base.Empire.Refresh(true);
		if (verifyPopulation)
		{
			this.VerifyOverallPopulation(city);
		}
		if ((base.Empire as MajorEmpire).Faction.Name == "FactionSeaDemons" && this.ContinentsSettledCount() >= 4)
		{
			this.EventService.Notify(new EventSeaDemons4Continents(base.Empire));
		}
		base.Empire.Refresh(false);
		this.RefreshDefensiveTowerPower(city.Region);
		this.OnCitiesCollectionChanged(city, CollectionChangeAction.Add);
	}

	private void AddCityImprovement(City city, CityImprovement cityImprovement)
	{
		Diagnostics.Assert(city != null);
		Diagnostics.Assert(cityImprovement != null);
		city.AddCityImprovement(cityImprovement, true);
		Diagnostics.Assert(cityImprovement.CityImprovementDefinition != null);
		if (cityImprovement.CityImprovementDefinition.HasUniqueTags && this.uniques != null && this.uniques.SimulationObject != null)
		{
			this.uniques.SimulationObject.Tags.AddTag(cityImprovement.CityImprovementDefinition.UniqueTags);
		}
	}

	private void BuildFreePointOfInterestImprovement(City city, WorldPosition districtPosition)
	{
		Region region = city.Region;
		if (region != null && region.PointOfInterests != null)
		{
			for (int i = 0; i < region.PointOfInterests.Length; i++)
			{
				PointOfInterest pointOfInterest = region.PointOfInterests[i];
				if (!(pointOfInterest.WorldPosition != districtPosition))
				{
					ConstructibleElement constructibleElement;
					this.BuildFreePointOfInterestImprovement(city, pointOfInterest, out constructibleElement);
					if (constructibleElement != null)
					{
						this.EventService.Notify(new EventConstructionEnded(base.Empire, city.GUID, constructibleElement));
					}
					break;
				}
			}
		}
	}

	private void BuildFreePointOfInterestImprovement(City city, PointOfInterest pointOfInterest, out ConstructibleElement constructibleElementBuilt)
	{
		constructibleElementBuilt = null;
		if (pointOfInterest.PointOfInterestImprovement != null)
		{
			return;
		}
		ConstructibleElement[] availableConstructibleElements = this.departmentOfIndustry.ConstructibleElementDatabase.GetAvailableConstructibleElements(new StaticString[0]);
		PointOfInterestImprovementDefinition pointOfInterestImprovementDefinition = null;
		List<StaticString> list = new List<StaticString>();
		for (int i = 0; i < availableConstructibleElements.Length; i++)
		{
			PointOfInterestImprovementDefinition pointOfInterestImprovementDefinition2 = availableConstructibleElements[i] as PointOfInterestImprovementDefinition;
			if (pointOfInterestImprovementDefinition2 != null && pointOfInterestImprovementDefinition2.PointOfInterestTemplateName == pointOfInterest.PointOfInterestDefinition.PointOfInterestTemplateName)
			{
				list.Clear();
				DepartmentOfTheTreasury.CheckConstructiblePrerequisites(city, pointOfInterestImprovementDefinition2, ref list, new string[]
				{
					ConstructionFlags.Prerequisite
				});
				if (!list.Contains(ConstructionFlags.Discard))
				{
					pointOfInterestImprovementDefinition = this.GetBestImprovementDefinition(city, pointOfInterest, pointOfInterestImprovementDefinition2, list);
					break;
				}
			}
		}
		if (pointOfInterestImprovementDefinition != null)
		{
			this.BuildPointOfInterestImprovement(city, pointOfInterest, pointOfInterestImprovementDefinition);
			constructibleElementBuilt = pointOfInterestImprovementDefinition;
		}
	}

	private void BuildPointOfInterestImprovement(PointOfInterest pointOfInterest, ConstructibleElement constructibleElement)
	{
		if (pointOfInterest == null)
		{
			return;
		}
		pointOfInterest.SwapPointOfInterestImprovement(constructibleElement, (global::Empire)base.Empire);
	}

	private void BuildPointOfInterestImprovement(City city, PointOfInterest pointOfInterest, ConstructibleElement constructibleElement)
	{
		if (pointOfInterest == null)
		{
			return;
		}
		float propertyValue = city.GetPropertyValue(SimulationProperties.Population);
		pointOfInterest.SwapPointOfInterestImprovement(constructibleElement, city.Empire);
		global::Empire empire = base.Empire as global::Empire;
		SimulationDescriptor value = this.SimulationDescriptorDatabase.GetValue(empire.Faction.AffinityMapping);
		Diagnostics.Assert(value != null);
		SimulationDescriptor descriptorFromType = pointOfInterest.SimulationObject.GetDescriptorFromType(value.Type);
		Diagnostics.Log("pointOfInterest.SwapDescriptor from {0} to {1}", new object[]
		{
			(descriptorFromType == null) ? "Null" : descriptorFromType.Name.ToString(),
			value.Name
		});
		pointOfInterest.SwapDescriptor(value);
		if (city.SimulationObject.Children.Contains(pointOfInterest))
		{
			city.RemoveChild(pointOfInterest);
			city.AddChild(pointOfInterest);
			Diagnostics.Log("[Desync] '{0}' has been reattached to '{1}' (Dirty).", new object[]
			{
				pointOfInterest.Name,
				city.Name
			});
		}
		city.Refresh(true);
		this.ForceGrowthToCurrentPopulation(city, propertyValue);
		city.Refresh(false);
		string text;
		if (pointOfInterest.PointOfInterestDefinition.TryGetValue("VisibilityTechnology", out text) && this.departmentOfScience.GetTechnologyState(text) != DepartmentOfScience.ConstructibleElement.State.Researched && !city.SimulationObject.Children.Contains(pointOfInterest))
		{
			city.AddChild(pointOfInterest);
			pointOfInterest.Empire = (base.Empire as global::Empire);
			for (int i = 0; i < city.Districts.Count; i++)
			{
				if (city.Districts[i].WorldPosition == pointOfInterest.WorldPosition)
				{
					SimulationDescriptor descriptor;
					if (pointOfInterest.PointOfInterestDefinition.TryGetValue("DistrictBonus", out text) && DepartmentOfTheInterior.SimulationDescriptorDatabaseStatic.TryGetValue(text, out descriptor))
					{
						city.Districts[i].AddDescriptor(descriptor, false);
					}
					DepartmentOfTheInterior.ApplyDistrictDescriptorOnPointOfInterest(pointOfInterest, city.Districts[i].Type);
					break;
				}
			}
		}
		pointOfInterest.Refresh(false);
	}

	private int ContinentsSettledCount()
	{
		List<int> list = new List<int>();
		for (int i = 0; i < this.cities.Count<City>(); i++)
		{
			Region region = this.cities[i].Region;
			if (region.Owner == base.Empire)
			{
				if (!list.Contains(region.ContinentID))
				{
					list.Add(region.ContinentID);
				}
			}
		}
		return list.Count;
	}

	private Camp CreateCamp(GameEntityGUID campGUID, GameEntityGUID cityGUID, global::Empire empire, WorldPosition worldPosition)
	{
		Camp camp = new Camp(campGUID, cityGUID, empire, worldPosition);
		SimulationDescriptor descriptor = null;
		if (this.SimulationDescriptorDatabase.TryGetValue(Camp.ClassCampDescriptor, out descriptor))
		{
			camp.AddDescriptor(descriptor, false);
		}
		else
		{
			Diagnostics.LogError("Unable to retrieve the '{0}' simulation descriptor from the database.", new object[]
			{
				Camp.ClassCampDescriptor
			});
		}
		camp.Refresh(false);
		return camp;
	}

	private City CreateCity(GameEntityGUID guid, WorldPosition worldPosition, GameEntityGUID districtGUID, GameEntityGUID militiaGUID, StaticString terrainTypeName, StaticString biomeTypeName, StaticString anomalyTypeName, StaticString riverTypeName)
	{
		City city = new City(guid)
		{
			WorldPosition = worldPosition,
			Empire = (base.Empire as global::Empire)
		};
		city.Ownership[base.Empire.Index] = 1f;
		city.SetPropertyBaseValue(SimulationProperties.Ownership, 1f);
		SimulationDescriptor descriptor = null;
		if (this.SimulationDescriptorDatabase.TryGetValue("ClassCity", out descriptor))
		{
			city.AddDescriptor(descriptor, false);
		}
		else
		{
			Diagnostics.LogError("Unable to retrieve the 'ClassCity' simulation descriptor from the database.");
		}
		District district = this.CreateDistrict(districtGUID, worldPosition, DistrictType.Center, terrainTypeName, biomeTypeName, anomalyTypeName, riverTypeName);
		city.AddDistrict(district);
		DepartmentOfTheInterior.ApplyDistrictDescriptorOnPointOfInterest(district.WorldPosition, district.Type);
		if (militiaGUID.IsValid)
		{
			city.Militia = new Militia(militiaGUID);
			city.Militia.WorldPosition = city.WorldPosition;
			city.Militia.Empire = (global::Empire)base.Empire;
			city.AddChild(city.Militia);
		}
		city.Refresh(true);
		return city;
	}

	private District CreateDistrict(GameEntityGUID guid, WorldPosition worldPosition, DistrictType districtType, StaticString terrainTypeName, StaticString biomeTypeName, StaticString anomalyTypeName, StaticString riverTypeName)
	{
		District district = new District(guid)
		{
			WorldPosition = worldPosition
		};
		IOrbService service = this.GameService.Game.Services.GetService<IOrbService>();
		if (service != null && districtType != DistrictType.Exploitation)
		{
			service.RemoveSpawnTileAtWorldPosition(worldPosition);
		}
		DepartmentOfTheInterior.ApplyDistrictDescriptors(district, districtType, terrainTypeName, biomeTypeName, anomalyTypeName, riverTypeName, true);
		DepartmentOfTheInterior.ApplyPointOfInterestDescriptors(base.Empire, district, district.WorldPosition, district.Type);
		DepartmentOfTheInterior.ApplyWorldEffectTypeDescriptor(district, district.WorldPosition, false);
		district.Type = districtType;
		return district;
	}

	private void DeassimilateFaction(Faction faction)
	{
		if (this.assimilatedFactions.Exists((Faction match) => match.Name == faction.Name))
		{
			this.assimilatedFactions.RemoveAll((Faction match) => match.Name == faction.Name);
			if (this.assimilatedFactionSimulationObject.Tags.Contains(faction.Affinity))
			{
				this.assimilatedFactionSimulationObject.RemoveDescriptorByName(faction.Affinity);
			}
			if (this.assimilatedFactions.Count == 0 && this.assimilatedFactionSimulationObject.Parent == base.Empire.SimulationObject)
			{
				base.Empire.SimulationObject.RemoveChild(this.assimilatedFactionSimulationObject);
			}
			MinorFaction minorFaction = faction as MinorFaction;
			FactionTrait[] array = FactionTrait.EnumerableTraits(minorFaction.AssimilationTraits).ToArray<FactionTrait>();
			if (array != null)
			{
				foreach (FactionTrait factionTrait in array)
				{
					if (factionTrait != null && this.factionTraitReferenceCount.ContainsKey(factionTrait))
					{
						Dictionary<FactionTrait, int> dictionary2;
						Dictionary<FactionTrait, int> dictionary = dictionary2 = this.factionTraitReferenceCount;
						FactionTrait key2;
						FactionTrait key = key2 = factionTrait;
						int num = dictionary2[key2];
						dictionary[key] = num - 1;
						if (this.factionTraitReferenceCount[factionTrait] <= 0)
						{
							if (factionTrait.SimulationDescriptorReferences != null)
							{
								for (int j = 0; j < factionTrait.SimulationDescriptorReferences.Length; j++)
								{
									SimulationDescriptor descriptor;
									if (this.SimulationDescriptorDatabase.TryGetValue(factionTrait.SimulationDescriptorReferences[j], out descriptor))
									{
										base.Empire.RemoveDescriptor(descriptor);
									}
									else
									{
										Diagnostics.LogWarning("Failed to find the descriptor for (reference name: '{1}') on trait (name: '{0}').", new object[]
										{
											factionTrait.Name,
											factionTrait.SimulationDescriptorReferences[j]
										});
									}
								}
							}
							this.factionTraitReferenceCount.Remove(factionTrait);
						}
					}
				}
			}
			this.OnAssimilatedFactionsCollectionChanged(faction, CollectionChangeAction.Remove);
		}
	}

	private void DepartmentOfForeignAffairs_DiplomaticRelationStateChange(object sender, DiplomaticRelationStateChangeEventArgs e)
	{
		for (int i = 0; i < this.TamedKaijuGarrisons.Count; i++)
		{
			this.TamedKaijuGarrisons[i].Kaiju.CallRefreshProvidedRegionEffects();
		}
		for (int j = 0; j < this.cities.Count; j++)
		{
			this.cities[j].CallRefreshAppliedRegionEffects();
		}
		for (int k = 0; k < this.TamedKaijuGarrisons.Count; k++)
		{
			this.TamedKaijuGarrisons[k].CallRefreshAppliedRegionEffects();
		}
	}

	private void DepartmentOfIndustry_CityImprovementConstructionChange(object sender, ConstructionChangeEventArgs e)
	{
		Diagnostics.Assert(e.Context is City);
		City city = e.Context as City;
		Diagnostics.Assert(this.cities.Contains(city));
		Diagnostics.Assert(e.Construction.ConstructibleElement != null);
		Diagnostics.Assert(e.Construction.ConstructibleElement.Category == CityImprovementDefinition.ReadOnlyCategory || e.Construction.ConstructibleElement.Category == CityImprovementDefinition.ReadOnlyNationalCategory);
		ConstructionChangeEventAction action = e.Action;
		if (action == ConstructionChangeEventAction.Completed)
		{
			Construction construction = e.Construction;
			CityImprovement cityImprovement = this.CreateCityImprovement(construction.ConstructibleElement, construction.GUID);
			this.AddCityImprovement(city, cityImprovement);
			city.Refresh(false);
			if (e.Construction.ConstructibleElement.Name == "CityImprovementRoads")
			{
				city.CadastralMap.ConnectedMovementCapacity |= PathfindingMovementCapacity.Ground;
				global::PlayerController server = (base.Empire as global::Empire).PlayerControllers.Server;
				if (server != null)
				{
					server.PostOrder(new OrderUpdateCadastralMap(base.Empire.Index, city, PathfindingMovementCapacity.Ground, CadastralMapOperation.Connect)
					{
						WorldPosition = e.Construction.WorldPosition
					});
				}
			}
		}
	}

	private void DepartmentOfIndustry_BoosterGeneratorConstructionChange(object sender, ConstructionChangeEventArgs e)
	{
		Diagnostics.Assert(e.Context is City);
		City item = e.Context as City;
		Diagnostics.Assert(this.cities.Contains(item));
		Diagnostics.Assert(e.Construction.ConstructibleElement != null);
		Diagnostics.Assert(e.Construction.ConstructibleElement.Category == BoosterGeneratorDefinition.ReadOnlyCategory);
		ConstructionChangeEventAction action = e.Action;
		if (action == ConstructionChangeEventAction.Completed)
		{
			global::PlayerController server = (base.Empire as global::Empire).PlayerControllers.Server;
			if (server != null)
			{
				BoosterGeneratorDefinition boosterGeneratorDefinition = e.Construction.ConstructibleElement as BoosterGeneratorDefinition;
				OrderBuyoutAndActivateBooster order = new OrderBuyoutAndActivateBooster(base.Empire.Index, boosterGeneratorDefinition.BoosterDefinitionName, 0UL, false);
				server.PostOrder(order);
			}
		}
	}

	private void DepartmentOfIndustry_CityConstructibleActionConstructionChange(object sender, ConstructionChangeEventArgs e)
	{
		Diagnostics.Assert(e.Context is City);
		City city = e.Context as City;
		Diagnostics.Assert(this.cities.Contains(city));
		Diagnostics.Assert(e.Construction.ConstructibleElement != null);
		CityConstructibleActionDefinition cityConstructibleActionDefinition = e.Construction.ConstructibleElement as CityConstructibleActionDefinition;
		if (cityConstructibleActionDefinition != null)
		{
			ConstructionChangeEventAction action = e.Action;
			if (action == ConstructionChangeEventAction.Completed)
			{
				string text = cityConstructibleActionDefinition.Action;
				switch (text)
				{
				case "Raze":
				{
					SimulationDescriptor descriptor;
					if (this.SimulationDescriptorDatabase.TryGetValue(City.TagCityStatusRazed, out descriptor))
					{
						city.AddDescriptor(descriptor, true);
						city.TurnWhenToProceedWithRazing = (this.GameService.Game as global::Game).Turn + 1;
						city.ShouldRazeRegionBuildingWithSelf = this.DoesRazingDetroyRegionBuilding;
						city.ShouldInjureSpyOnRaze = true;
					}
					goto IL_2F3;
				}
				case "Migrate":
				{
					SimulationDescriptor descriptor2;
					if (this.SimulationDescriptorDatabase.TryGetValue(City.TagCityStatusRazed, out descriptor2))
					{
						city.AddDescriptor(descriptor2, true);
						city.TurnWhenToProceedWithRazing = (this.GameService.Game as global::Game).Turn + 1;
						city.ShouldRazeRegionBuildingWithSelf = false;
						city.ShouldInjureSpyOnRaze = false;
					}
					goto IL_2F3;
				}
				case "PurgeTheLand":
				{
					PointOfInterest[] pointOfInterests = city.Region.PointOfInterests;
					for (int i = 0; i < pointOfInterests.Length; i++)
					{
						if (pointOfInterests[i].CreepingNodeGUID != GameEntityGUID.Zero)
						{
							IGameEntity gameEntity = null;
							if (this.GameEntityRepositoryService.TryGetValue(pointOfInterests[i].CreepingNodeGUID, out gameEntity))
							{
								CreepingNode creepingNode = gameEntity as CreepingNode;
								global::PlayerController server = (base.Empire as global::Empire).PlayerControllers.Server;
								if (server != null && creepingNode != null && creepingNode.Empire.Index != city.Empire.Index)
								{
									OrderDestroyCreepingNode order = new OrderDestroyCreepingNode(creepingNode.Empire.Index, pointOfInterests[i].CreepingNodeGUID);
									server.PostOrder(order);
								}
							}
						}
					}
					goto IL_2F3;
				}
				case "IntegrateFaction":
					if (city.LastNonInfectedOwner != null)
					{
						global::PlayerController server2 = (base.Empire as global::Empire).PlayerControllers.Server;
						if (server2 != null)
						{
							OrderIntegrateFaction order2 = new OrderIntegrateFaction(base.Empire.Index, city.LastNonInfectedOwner.Index);
							server2.PostOrder(order2);
						}
					}
					goto IL_2F3;
				}
				Diagnostics.LogError("Unhandled city constructible action (name: '{0}', action: '{1}').", new object[]
				{
					cityConstructibleActionDefinition.Name,
					cityConstructibleActionDefinition.Action
				});
				IL_2F3:;
			}
		}
	}

	private void DepartmentOfIndustry_ConstructibleDistrictConstructionChange(object sender, ConstructionChangeEventArgs e)
	{
		Diagnostics.Assert(e.Context is City);
		ConstructibleDistrictDefinition constructibleDistrictDefinition = e.Construction.ConstructibleElement as ConstructibleDistrictDefinition;
		if (constructibleDistrictDefinition == null)
		{
			return;
		}
		if ((base.Empire as global::Empire).PlayerControllers.Server == null)
		{
			return;
		}
		ConstructionChangeEventAction action = e.Action;
		if (action == ConstructionChangeEventAction.Completed)
		{
			string constructionName = constructibleDistrictDefinition.ConstructionName;
			if (constructionName != null)
			{
				if (DepartmentOfTheInterior.<>f__switch$map14 == null)
				{
					DepartmentOfTheInterior.<>f__switch$map14 = new Dictionary<string, int>(1)
					{
						{
							"Camp",
							0
						}
					};
				}
				int num;
				if (DepartmentOfTheInterior.<>f__switch$map14.TryGetValue(constructionName, out num))
				{
					if (num == 0)
					{
						OrderCreateCamp item = new OrderCreateCamp(base.Empire.Index, e.Construction.WorldPosition, constructibleDistrictDefinition.UpdateCadastralMap);
						this.pendingDistrictConstructions.Add(item);
						this.GameEntityRepositoryService.Unregister(e.Construction);
					}
				}
			}
		}
	}

	private void DepartmentOfIndustry_DistrictImprovementConstructionChange(object sender, ConstructionChangeEventArgs e)
	{
		Diagnostics.Assert(e.Context is City);
		City city = e.Context as City;
		Diagnostics.Assert(this.cities.Contains(city));
		Diagnostics.Assert(e.Construction.ConstructibleElement != null);
		Diagnostics.Assert(e.Construction.ConstructibleElement.Category == DistrictImprovementDefinition.ReadOnlyCategory);
		ConstructionChangeEventAction action = e.Action;
		if (action == ConstructionChangeEventAction.Completed)
		{
			OrderCreateDistrictImprovement item = new OrderCreateDistrictImprovement(base.Empire.Index, city, e.Construction.ConstructibleElement, e.Construction.WorldPosition);
			this.pendingExtensions.Add(item);
			this.GameEntityRepositoryService.Unregister(e.Construction);
		}
	}

	private void DepartmentOfIndustry_PointOfInterestImprovementConstructionChange(object sender, ConstructionChangeEventArgs e)
	{
		Diagnostics.Assert(e.Context is City);
		City city = e.Context as City;
		Diagnostics.Assert(this.cities.Contains(city));
		Diagnostics.Assert(e.Construction.ConstructibleElement != null);
		Diagnostics.Assert(e.Construction.ConstructibleElement.Category == PointOfInterestImprovementDefinition.ReadOnlyCategory);
		ConstructionChangeEventAction action = e.Action;
		if (action == ConstructionChangeEventAction.Completed)
		{
			PointOfInterest pointOfInterest = city.Region.PointOfInterests.FirstOrDefault((PointOfInterest match) => match.WorldPosition == e.Construction.WorldPosition);
			if (pointOfInterest != null)
			{
				List<StaticString> lastFailureFlags = new List<StaticString>();
				PointOfInterestImprovementDefinition bestImprovementDefinition = this.GetBestImprovementDefinition(city, pointOfInterest, e.Construction.ConstructibleElement as PointOfInterestImprovementDefinition, lastFailureFlags);
				if (bestImprovementDefinition != null)
				{
					this.BuildPointOfInterestImprovement(city, pointOfInterest, bestImprovementDefinition);
				}
			}
			this.GameEntityRepositoryService.Unregister(e.Construction);
		}
	}

	private void DepartmentOfScience_TechnologyUnlocked(object sender, ConstructibleElementEventArgs e)
	{
		List<StaticString> list = new List<StaticString>();
		if (e.ConstructibleElement is DepartmentOfScience.ConstructibleElement)
		{
			List<ConstructibleElement> unlocksByTechnology = (e.ConstructibleElement as DepartmentOfScience.ConstructibleElement).GetUnlocksByTechnology();
			if (unlocksByTechnology != null)
			{
				for (int i = 0; i < unlocksByTechnology.Count; i++)
				{
					PointOfInterestImprovementDefinition pointOfInterestImprovementDefinition = unlocksByTechnology[i] as PointOfInterestImprovementDefinition;
					if (pointOfInterestImprovementDefinition != null)
					{
						list.Add(pointOfInterestImprovementDefinition.PointOfInterestTemplateName);
					}
				}
			}
		}
		if (list.Count > 0)
		{
			MajorEmpire majorEmpire = base.Empire as MajorEmpire;
			if (majorEmpire != null)
			{
				if (majorEmpire.ConvertedVillages != null)
				{
					for (int j = 0; j < majorEmpire.ConvertedVillages.Count; j++)
					{
						DepartmentOfTheInterior.GenerateResourceForConvertedVillages(base.Empire, majorEmpire.ConvertedVillages[j].PointOfInterest);
					}
				}
				if (majorEmpire.TamedKaijus != null)
				{
					for (int k = 0; k < majorEmpire.TamedKaijus.Count; k++)
					{
						DepartmentOfTheInterior.GenerateResourcesLeechingForTamedKaijus(majorEmpire.TamedKaijus[k]);
					}
				}
			}
		}
		List<StaticString> lastFailureFlags = new List<StaticString>();
		for (int l = 0; l < this.cities.Count; l++)
		{
			City city = this.cities[l];
			for (int m = 0; m < city.Region.PointOfInterests.Length; m++)
			{
				PointOfInterest pointOfInterest = city.Region.PointOfInterests[m];
				string x;
				if (pointOfInterest.PointOfInterestDefinition.TryGetValue("VisibilityTechnology", out x) && x == e.ConstructibleElement.Name && !city.SimulationObject.Children.Contains(pointOfInterest))
				{
					city.AddChild(pointOfInterest);
					pointOfInterest.Empire = (base.Empire as global::Empire);
					for (int n = 0; n < city.Districts.Count; n++)
					{
						if (city.Districts[n].WorldPosition == pointOfInterest.WorldPosition)
						{
							DepartmentOfTheInterior.ApplyPointOfInterestDescriptors(base.Empire, city.Districts[n], pointOfInterest, city.Districts[n].Type);
						}
					}
				}
				if (list.Contains(pointOfInterest.PointOfInterestDefinition.PointOfInterestTemplateName))
				{
					if (pointOfInterest.IsResourceDeposit())
					{
						for (int num = 0; num < city.Districts.Count; num++)
						{
							District district = city.Districts[num];
							SimulationDescriptor descriptor;
							if (district.WorldPosition == pointOfInterest.WorldPosition && !district.SimulationObject.Tags.Contains("DistrictExploitableResource") && this.SimulationDescriptorDatabase.TryGetValue("DistrictExploitableResource", out descriptor))
							{
								district.AddDescriptor(descriptor, false);
								break;
							}
						}
					}
					if (pointOfInterest.PointOfInterestImprovement == null && pointOfInterest.CreepingNodeImprovement == null)
					{
						for (int num2 = 0; num2 < city.Districts.Count; num2++)
						{
							if (pointOfInterest.WorldPosition == city.Districts[num2].WorldPosition && city.Districts[num2].Type != DistrictType.Exploitation)
							{
								ConstructibleElement constructibleElement;
								this.BuildFreePointOfInterestImprovement(city, pointOfInterest, out constructibleElement);
							}
						}
						if (city.Camp != null)
						{
							for (int num3 = 0; num3 < city.Camp.Districts.Count; num3++)
							{
								if (pointOfInterest.WorldPosition == city.Camp.Districts[num3].WorldPosition && city.Camp.Districts[num3].Type != DistrictType.Exploitation)
								{
									ConstructibleElement constructibleElement2;
									this.BuildFreePointOfInterestImprovement(city, pointOfInterest, out constructibleElement2);
								}
							}
						}
					}
					else if (pointOfInterest.CreepingNodeImprovement == null)
					{
						PointOfInterestImprovementDefinition bestImprovementDefinition = this.GetBestImprovementDefinition(city, pointOfInterest, pointOfInterest.PointOfInterestImprovement as PointOfInterestImprovementDefinition, lastFailureFlags);
						if (bestImprovementDefinition != null)
						{
							this.BuildPointOfInterestImprovement(city, pointOfInterest, bestImprovementDefinition);
						}
					}
				}
			}
		}
	}

	private bool EndTurnValidator(bool force)
	{
		bool result = true;
		if (this.playerControllerRepositoryService.ActivePlayerController.Empire == base.Empire && !force)
		{
			for (int i = 0; i < this.cities.Count; i++)
			{
				float propertyValue = this.cities[i].GetPropertyValue(SimulationProperties.NetCityGrowth);
				if (propertyValue < 0f)
				{
					float propertyValue2 = this.cities[i].GetPropertyValue(SimulationProperties.Population);
					float growthLimit = this.GetGrowthLimit(propertyValue2);
					float propertyValue3 = this.cities[i].GetPropertyValue(SimulationProperties.CityGrowthStock);
					float propertyValue4 = this.cities[i].GetPropertyValue(SimulationProperties.Population);
					if (propertyValue3 + propertyValue < growthLimit && propertyValue4 > 1f)
					{
						this.EventService.Notify(new EventCityStarvation(base.Empire, this.cities[i]));
						result = false;
					}
				}
				ConstructionQueue constructionQueue = this.departmentOfIndustry.GetConstructionQueue(this.cities[i]);
				if (constructionQueue.Length == 0 && !this.cities[i].IsInfected)
				{
					this.EventService.Notify(new EventCityIdle(base.Empire, this.cities[i]));
					result = false;
				}
			}
		}
		return result;
	}

	private void ExtendCityAt(City city, District newExtension)
	{
		DepartmentOfTheInterior.ApplyDistrictType(newExtension, DistrictType.Extension, null);
		DepartmentOfTheInterior.ApplyDistrictDescriptorOnPointOfInterest(newExtension.WorldPosition, newExtension.Type);
		city.ExtensionCount++;
		this.UpdateDistrictLevel(city, newExtension);
	}

	private IEnumerator GameClient_Turn_End_ComputeCityPopulation(string context, string name)
	{
		for (int index = 0; index < this.cities.Count; index++)
		{
			this.ComputeCityPopulation(this.cities[index], true);
			this.VerifyOverallPopulation(this.cities[index]);
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_Begin_UpdateMilitia(string context, string name)
	{
		if (base.Empire is MajorEmpire)
		{
			DepartmentOfDefense departmentOfDefense = base.Empire.GetAgency<DepartmentOfDefense>();
			Diagnostics.Assert(departmentOfDefense != null);
			for (int index = 0; index < this.cities.Count; index++)
			{
				if (this.cities[index].BesiegingEmpire == null && !this.cities[index].IsUnderEarthquake)
				{
					if (this.cities[index].Militia != null)
					{
						UnitDesign bestUnitDesignAvailableForMilitiaUnits = departmentOfDefense.FindBestUnitDesignAvailableForMilitiaUnits();
						List<Unit> replacementUnits = new List<Unit>();
						List<float> replacementUnitsXp = new List<float>();
						List<Unit> unitsToReplace = new List<Unit>();
						foreach (Unit unit in this.cities[index].Militia.StandardUnits)
						{
							if (unit.UnitDesign.Model < bestUnitDesignAvailableForMilitiaUnits.Model)
							{
								Unit replacementUnit = DepartmentOfDefense.CreateUnitByDesign(unit.GUID, bestUnitDesignAvailableForMilitiaUnits);
								replacementUnitsXp.Add(unit.GetPropertyValue(SimulationProperties.UnitAccumulatedExperience));
								replacementUnits.Add(replacementUnit);
								unitsToReplace.Add(unit);
							}
							else
							{
								float maximumHealth = unit.GetPropertyValue(SimulationProperties.MaximumHealth);
								float health = unit.GetPropertyBaseValue(SimulationProperties.Health);
								if (health < maximumHealth)
								{
									unit.SetPropertyBaseValue(SimulationProperties.Health, maximumHealth);
								}
							}
						}
						if (replacementUnits != null && replacementUnits.Count > 0 && unitsToReplace.Count > 0)
						{
							for (int militiaUnitIndex = unitsToReplace.Count - 1; militiaUnitIndex >= 0; militiaUnitIndex--)
							{
								Unit unitToReplace = unitsToReplace[militiaUnitIndex];
								this.cities[index].Militia.RemoveUnit(unitToReplace);
								this.GameEntityRepositoryService.Unregister(unitToReplace.GUID);
								unitToReplace.Dispose();
							}
							unitsToReplace.Clear();
							for (int uIndex = 0; uIndex < replacementUnits.Count; uIndex++)
							{
								this.cities[index].Militia.AddUnit(replacementUnits[uIndex]);
								this.GameEntityRepositoryService.Register(replacementUnits[uIndex]);
								replacementUnits[uIndex].GainXp(replacementUnitsXp[uIndex], false, true);
							}
						}
					}
				}
			}
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_Begin_UpdateOwnership(string context, string name)
	{
		for (int index = 0; index < this.cities.Count; index++)
		{
			this.ComputeOwnership(this.cities[index]);
		}
		yield break;
	}

	private IEnumerator GameClient_Turn_End_ComputeCityDefensePoint(string context, string name)
	{
		for (int index = 0; index < this.cities.Count; index++)
		{
			this.UpdateSiegeAtEndTurn(this.cities[index]);
			this.GameClient_TurnEnd_UpdateEarthquakeDamage(this.cities[index]);
		}
		yield break;
	}

	private IEnumerator GameClient_Turn_End_ComputeRoundUpProgress(string context, string name)
	{
		for (int index = 0; index < this.cities.Count; index++)
		{
			int roundUpProgress = (int)this.cities[index].GetPropertyBaseValue(SimulationProperties.RoundUpProgress);
			if (roundUpProgress >= 0)
			{
				this.cities[index].SetPropertyBaseValue(SimulationProperties.RoundUpProgress, (float)(roundUpProgress + 1));
			}
		}
		yield break;
	}

	private IEnumerator GameServer_Turn_Ended_CheckRoundUpExecution(string context, string name)
	{
		if (this.departmentOfIntelligence == null)
		{
			yield break;
		}
		for (int index = 0; index < this.cities.Count; index++)
		{
			int roundUpProgress = (int)this.cities[index].GetPropertyValue(SimulationProperties.RoundUpProgress);
			int roundUpTurnToActivate = (int)this.cities[index].GetPropertyValue(SimulationProperties.RoundUpTurnToActivate);
			if (roundUpProgress > 0 && roundUpProgress >= roundUpTurnToActivate)
			{
				this.departmentOfIntelligence.ExecuteRoundUp(this.cities[index]);
			}
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_PopulationBuyoutCooldown(string context, string name)
	{
		float previousCooldown = base.Empire.GetPropertyValue(SimulationProperties.PopulationBuyoutCooldown);
		if (previousCooldown > 0f)
		{
			previousCooldown -= 1f;
			base.Empire.SetPropertyBaseValue(SimulationProperties.PopulationBuyoutCooldown, previousCooldown);
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_UnitExperiencePerTurnGain(string context, string name)
	{
		for (int index = 0; index < this.cities.Count; index++)
		{
			foreach (Unit unit in this.cities[index].Units)
			{
				float experience = unit.GetPropertyValue(SimulationProperties.UnitExperienceGainPerTurn);
				unit.GainXp(experience, false, true);
			}
			foreach (Unit unit2 in this.cities[index].Militia.Units)
			{
				float experience2 = unit2.GetPropertyValue(SimulationProperties.UnitExperienceGainPerTurn);
				unit2.GainXp(experience2, false, true);
			}
		}
		for (int index2 = 0; index2 < this.OccupiedFortresses.Count; index2++)
		{
			if (this.OccupiedFortresses[index2] != null)
			{
				foreach (Unit unit3 in this.OccupiedFortresses[index2].Units)
				{
					if (unit3 != null)
					{
						float experience3 = unit3.GetPropertyValue(SimulationProperties.UnitExperienceGainPerTurn);
						unit3.GainXp(experience3, false, true);
					}
				}
			}
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_UnitHealthPerTurnGain(string context, string name)
	{
		DepartmentOfDefense defense = base.Empire.GetAgency<DepartmentOfDefense>();
		for (int index = 0; index < this.cities.Count; index++)
		{
			float regenModifier = this.cities[index].GetPropertyValue(SimulationProperties.InGarrisonRegenModifier);
			int pacifiedVillageCount = Mathf.FloorToInt(this.cities[index].GetPropertyValue(SimulationProperties.NumberOfRebuildPacifiedVillage));
			if (this.cities[index].Camp != null)
			{
				foreach (Unit unit in this.cities[index].Camp.Units)
				{
					DepartmentOfDefense.RegenUnit(unit, regenModifier, pacifiedVillageCount);
				}
				this.cities[index].Camp.Refresh(false);
			}
			if (this.cities[index].BesiegingEmpire == null && !this.cities[index].IsUnderEarthquake)
			{
				foreach (Unit unit2 in this.cities[index].Units)
				{
					DepartmentOfDefense.RegenUnit(unit2, regenModifier, pacifiedVillageCount);
				}
				this.cities[index].Refresh(false);
				defense.CleanGarrisonAfterEncounter(this.cities[index]);
			}
		}
		yield break;
	}

	private IEnumerator GameServerState_Turn_Begin_DestroyRazedCities(string context, string name)
	{
		City[] cities = this.Cities.ToArray<City>();
		for (int index = 0; index < cities.Length; index++)
		{
			if (cities[index].SimulationObject.Tags.Contains(City.TagCityStatusRazed))
			{
				if (cities[index].TurnWhenToProceedWithRazing <= (this.GameService.Game as global::Game).Turn)
				{
					if (cities[index].StandardUnits.Count > 0)
					{
						GameEntityGUID[] guids = (from unit in cities[index].StandardUnits
						select unit.GUID).ToArray<GameEntityGUID>();
						int maximumNumberOfUnitsPerArmy = (int)base.Empire.GetPropertyValue(SimulationProperties.ArmyUnitSlot);
						if (maximumNumberOfUnitsPerArmy <= 0)
						{
							Diagnostics.LogWarning("The maximum number Of units per army doesn't allow for transfer.");
							goto IL_64B;
						}
						WorldArea worldArea = new WorldArea(new WorldPosition[]
						{
							cities[index].WorldPosition
						});
						int offset = 0;
						int maximumNumberOfGrowths = 1;
						int startIndex = 0;
						while (startIndex < guids.Length)
						{
							int numberOfUnitsToTransfer = guids.Length - startIndex;
							if (numberOfUnitsToTransfer > maximumNumberOfUnitsPerArmy)
							{
								numberOfUnitsToTransfer = maximumNumberOfUnitsPerArmy;
							}
							WorldPosition worldPosition = WorldPosition.Invalid;
							WorldPosition worldPositionInSecond = WorldPosition.Invalid;
							bool targetPositionIsValidForUseAsArmySpawnLocation = false;
							IGameService gameService = Services.GetService<IGameService>();
							Diagnostics.Assert(gameService != null);
							IPathfindingService pathfindingService = gameService.Game.Services.GetService<IPathfindingService>();
							IWorldPositionningService positionService = gameService.Game.Services.GetService<IWorldPositionningService>();
							global::Game game = gameService.Game as global::Game;
							GridMap<Army> armiesMap = (!(game != null)) ? null : (game.World.Atlas.GetMap(WorldAtlas.Maps.Armies) as GridMap<Army>);
							for (;;)
							{
								int num;
								if (offset >= worldArea.WorldPositions.Count)
								{
									maximumNumberOfGrowths = (num = maximumNumberOfGrowths) - 1;
									if (num > 0)
									{
										worldArea = worldArea.Grow(this.WorldPositionningService.World.WorldParameters);
									}
									if (offset >= worldArea.WorldPositions.Count)
									{
										goto Block_10;
									}
								}
								List<WorldPosition> worldPositions = worldArea.WorldPositions;
								offset = (num = offset) + 1;
								worldPosition = worldPositions[num];
								targetPositionIsValidForUseAsArmySpawnLocation = worldPosition.IsValid;
								if (!targetPositionIsValidForUseAsArmySpawnLocation || armiesMap == null)
								{
									goto IL_372;
								}
								Army otherArmy = armiesMap.GetValue(worldPosition);
								if (otherArmy == null)
								{
									goto IL_372;
								}
								targetPositionIsValidForUseAsArmySpawnLocation = false;
								IL_49F:
								if (targetPositionIsValidForUseAsArmySpawnLocation)
								{
									break;
								}
								continue;
								IL_372:
								if (cities[index].Region != positionService.GetRegion(worldPosition))
								{
									targetPositionIsValidForUseAsArmySpawnLocation = false;
									goto IL_49F;
								}
								if (!targetPositionIsValidForUseAsArmySpawnLocation || pathfindingService == null)
								{
									goto IL_49F;
								}
								bool isTileStopableAndPassable = pathfindingService.IsTileStopableAndPassable(worldPosition, PathfindingMovementCapacity.Ground, PathfindingFlags.IgnoreFogOfWar);
								bool isTransitionPassable = pathfindingService.IsTransitionPassable(cities[index].WorldPosition, worldPosition, PathfindingMovementCapacity.Ground, (PathfindingFlags)0);
								if (isTileStopableAndPassable && (!(cities[index].WorldPosition != worldPosition) || isTransitionPassable))
								{
									goto IL_49F;
								}
								targetPositionIsValidForUseAsArmySpawnLocation = false;
								if (!worldPositionInSecond.IsValid && pathfindingService.IsTilePassable(worldPosition, PathfindingMovementCapacity.Ground, PathfindingFlags.IgnoreFogOfWar) && pathfindingService.IsTransitionPassable(cities[index].WorldPosition, worldPosition, PathfindingMovementCapacity.Ground, (PathfindingFlags)0))
								{
									worldPositionInSecond = worldPosition;
									goto IL_49F;
								}
								goto IL_49F;
							}
							IL_4AA:
							if (!targetPositionIsValidForUseAsArmySpawnLocation)
							{
								worldPosition = WorldPosition.Invalid;
							}
							if (!worldPosition.IsValid && worldPositionInSecond.IsValid)
							{
								worldPosition = worldPositionInSecond;
							}
							if (worldPosition.IsValid)
							{
								GameEntityGUID[] selection = new GameEntityGUID[numberOfUnitsToTransfer];
								Array.Copy(guids, startIndex, selection, 0, numberOfUnitsToTransfer);
								OrderTransferGarrisonToNewArmy orderTransferGarrisonToNewArmy = new OrderTransferGarrisonToNewArmy(base.Empire.Index, cities[index].GUID, selection, worldPosition, StaticString.Empty, false, true, true);
								((global::Empire)base.Empire).PlayerControllers.Server.PostOrder(orderTransferGarrisonToNewArmy);
								startIndex += numberOfUnitsToTransfer;
								continue;
							}
							break;
							Block_10:
							worldPosition = WorldPosition.Invalid;
							goto IL_4AA;
						}
					}
					OrderDestroyCity orderDestroyCity = new OrderDestroyCity(base.Empire.Index, cities[index].GUID, cities[index].ShouldRazeRegionBuildingWithSelf, cities[index].ShouldInjureSpyOnRaze, -1);
					((global::Empire)base.Empire).PlayerControllers.Server.PostOrder(orderDestroyCity);
					yield return null;
				}
			}
			IL_64B:;
		}
		yield break;
	}

	private IEnumerator GameServerState_Turn_Begin_UpdateMilitia(string context, string name)
	{
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		Diagnostics.Assert(gameService.Game != null);
		global::Game game = gameService.Game as global::Game;
		if (base.Empire is MajorEmpire && game != null && this.TurnWhenMilitiaWasLastUpdated < game.Turn)
		{
			for (int index = 0; index < this.cities.Count; index++)
			{
				if (this.cities[index].BesiegingEmpire == null && !this.cities[index].IsUnderEarthquake)
				{
					if (!this.cities[index].IsInfected)
					{
						if (this.cities[index].Militia != null && !this.cities[index].SimulationObject.Tags.Contains(City.TagCityStatusRazed))
						{
							float recoverPercentageByOwnership = 1f;
							if (DepartmentOfTheInterior.MilitiaRecoverByOwnershipCurve != null)
							{
								recoverPercentageByOwnership = DepartmentOfTheInterior.MilitiaRecoverByOwnershipCurve.Evaluate(this.cities[index].Ownership[base.Empire.Index]);
							}
							int numberOfUnitsInMilitia = this.cities[index].Militia.UnitsCount;
							int maximumNumberOfUnitsInMilitia = (int)this.cities[index].Militia.GetPropertyValue(SimulationProperties.MaximumUnitSlotCount);
							int maximumMilitiaUnitsByOwnership = (int)((float)maximumNumberOfUnitsInMilitia * recoverPercentageByOwnership);
							if (numberOfUnitsInMilitia < maximumMilitiaUnitsByOwnership)
							{
								int numberOfUnitsToCreate = Math.Min(maximumMilitiaUnitsByOwnership - numberOfUnitsInMilitia, 10);
								GameEntityGUID[] guids = new GameEntityGUID[numberOfUnitsToCreate];
								for (int jndex = 0; jndex < guids.Length; jndex++)
								{
									guids[jndex] = this.GameEntityRepositoryService.GenerateGUID();
								}
								OrderCreateMilitiaUnits orderCreateMilitiaUnits = new OrderCreateMilitiaUnits(base.Empire.Index)
								{
									CityGameEntityGUID = this.cities[index].GUID,
									GameEntityGUIDs = guids
								};
								((MajorEmpire)base.Empire).PlayerControllers.Server.PostOrder(orderCreateMilitiaUnits);
							}
							if (maximumMilitiaUnitsByOwnership < numberOfUnitsInMilitia)
							{
								Unit[] militiaUnits = this.cities[index].Militia.StandardUnitsAsArray;
								int numberOfUnitsToDestroy = Math.Min(numberOfUnitsInMilitia - maximumMilitiaUnitsByOwnership, militiaUnits.Length);
								GameEntityGUID[] guids2 = new GameEntityGUID[numberOfUnitsToDestroy];
								for (int jndex2 = 0; jndex2 < guids2.Length; jndex2++)
								{
									guids2[jndex2] = militiaUnits[militiaUnits.Length - jndex2 - 1].GUID;
								}
								OrderDestroyMilitiaUnits orderDestroyMilitiaUnits = new OrderDestroyMilitiaUnits(base.Empire.Index)
								{
									CityGameEntityGUID = this.cities[index].GUID,
									GameEntityGUIDs = guids2
								};
								((MajorEmpire)base.Empire).PlayerControllers.Server.PostOrder(orderDestroyMilitiaUnits);
							}
						}
					}
				}
			}
			this.TurnWhenMilitiaWasLastUpdated = game.Turn;
		}
		yield break;
	}

	private IEnumerator GameServerState_Turn_Ended_ExecutePendingDistrictConstruction(string context, string name)
	{
		global::Empire empire = base.Empire as global::Empire;
		for (int index = 0; index < this.pendingDistrictConstructions.Count; index++)
		{
			empire.PlayerControllers.Server.PostOrder(this.pendingDistrictConstructions[index]);
		}
		this.pendingDistrictConstructions.Clear();
		yield break;
	}

	private IEnumerator GameServerState_Turn_Ended_ExecutePendingExtension(string context, string name)
	{
		global::Empire empire = base.Empire as global::Empire;
		for (int index = 0; index < this.pendingExtensions.Count; index++)
		{
			empire.PlayerControllers.Server.PostOrder(this.pendingExtensions[index]);
		}
		this.pendingExtensions.Clear();
		yield break;
	}

	private IEnumerator GameClientState_Turn_Begin_RefreshDefensiveTowerPower(string context, string name)
	{
		for (int index = 0; index < this.cities.Count; index++)
		{
			if (this.cities[index] != null)
			{
				this.cities[index].Refresh(true);
				this.RefreshDefensiveTowerPower(this.cities[index].Region);
			}
		}
		yield break;
	}

	private float GetGrowthLimit(float population)
	{
		return DepartmentOfTheInterior.ComputeGrowthLimit(base.Empire.SimulationObject, population);
	}

	private void MovePopulation(int modifier, float efficencyLimit)
	{
		this.specializedPopulations.Sort(delegate(DepartmentOfTheInterior.SpecializedPopulation left, DepartmentOfTheInterior.SpecializedPopulation right)
		{
			if (left.Value == right.Value)
			{
				return left.Priority.CompareTo(right.Priority);
			}
			return -1 * left.Value.CompareTo(right.Value);
		});
		for (int i = 0; i < this.specializedPopulations.Count; i++)
		{
			if (this.specializedPopulations[i].Value >= 0f)
			{
				if (this.specializedPopulations[i].Value + (float)modifier >= 0f && (modifier < 0 || this.specializedPopulations[i].Value + (float)modifier <= efficencyLimit))
				{
					this.specializedPopulations[i].Value += (float)modifier;
					return;
				}
			}
		}
		this.specializedPopulations.Sort(delegate(DepartmentOfTheInterior.SpecializedPopulation left, DepartmentOfTheInterior.SpecializedPopulation right)
		{
			if (left.Value == right.Value)
			{
				return left.Priority.CompareTo(right.Priority);
			}
			return -1 * left.Value.CompareTo(right.Value);
		});
		for (int j = 0; j < this.specializedPopulations.Count; j++)
		{
			if (this.specializedPopulations[j].Value >= 0f)
			{
				if (this.specializedPopulations[j].Value + (float)modifier >= 0f && (modifier < 0 || this.specializedPopulations[j].Value + (float)modifier <= efficencyLimit))
				{
					this.specializedPopulations[j].Value += (float)modifier;
					return;
				}
			}
		}
	}

	private void OnCitiesCollectionChanged(City city, CollectionChangeAction action)
	{
		if (this.CitiesCollectionChanged != null)
		{
			this.CitiesCollectionChanged(this, new CollectionChangeEventArgs(action, city));
		}
		if (base.Empire.SimulationObject.Tags.Contains("FactionTraitMadFairiesTheSharing") && this.WorldPositionningService != null && this.WorldPositionningService.World != null && this.WorldPositionningService.World.Regions != null)
		{
			List<Region> list = new List<Region>();
			for (int i = 0; i < this.WorldPositionningService.World.Regions.Length; i++)
			{
				Region region = this.WorldPositionningService.World.Regions[i];
				region.EmpireBitsForNeighbourhood &= ~base.Empire.Bits;
				if (!SimulationGlobal.GlobalTagsContains("HeatWave"))
				{
					if (region.City != null && region.City.Empire == base.Empire)
					{
						if (region.City != city || action != CollectionChangeAction.Remove)
						{
							region.EmpireBitsForNeighbourhood |= base.Empire.Bits;
							list.Add(region);
						}
					}
				}
				else if (SimulationGlobal.GlobalTagsContains("HeatWave") && base.Empire.SimulationObject.Tags.Contains("FactionTraitMadFairiesHeatWave"))
				{
					region.EmpireBitsForNeighbourhood |= base.Empire.Bits;
					list.Add(region);
				}
			}
			int count = list.Count;
			for (int j = 0; j < count; j++)
			{
				Region region2 = list[j];
				if (region2.Borders != null)
				{
					for (int k = 0; k < region2.Borders.Length; k++)
					{
						int neighbourRegionIndex = region2.Borders[k].NeighbourRegionIndex;
						if (!list.Exists((Region match) => match.Index == neighbourRegionIndex))
						{
							Region region3 = this.WorldPositionningService.World.Regions[neighbourRegionIndex];
							region3.EmpireBitsForNeighbourhood |= base.Empire.Bits;
							list.Add(this.WorldPositionningService.World.Regions[neighbourRegionIndex]);
						}
					}
				}
			}
		}
	}

	private void OnSeasonChange(object sender, SeasonChangeEventArgs e)
	{
		if (base.Empire.SimulationObject.Tags.Contains("FactionTraitMadFairiesTheSharing") && this.WorldPositionningService != null && this.WorldPositionningService.World != null && this.WorldPositionningService.World.Regions != null)
		{
			List<Region> list = new List<Region>();
			for (int i = 0; i < this.WorldPositionningService.World.Regions.Length; i++)
			{
				Region region = this.WorldPositionningService.World.Regions[i];
				region.EmpireBitsForNeighbourhood &= ~base.Empire.Bits;
				if (!SimulationGlobal.GlobalTagsContains("HeatWave"))
				{
					if (region.City != null && region.City.Empire == base.Empire)
					{
						region.EmpireBitsForNeighbourhood |= base.Empire.Bits;
						list.Add(region);
					}
				}
				else if (SimulationGlobal.GlobalTagsContains("HeatWave") && base.Empire.SimulationObject.Tags.Contains("FactionTraitMadFairiesHeatWave"))
				{
					region.EmpireBitsForNeighbourhood |= base.Empire.Bits;
					list.Add(region);
				}
			}
			int count = list.Count;
			for (int j = 0; j < count; j++)
			{
				Region region2 = list[j];
				if (region2.Borders != null)
				{
					for (int k = 0; k < region2.Borders.Length; k++)
					{
						int neighbourRegionIndex = region2.Borders[k].NeighbourRegionIndex;
						if (!list.Exists((Region match) => match.Index == neighbourRegionIndex))
						{
							Region region3 = this.WorldPositionningService.World.Regions[neighbourRegionIndex];
							region3.EmpireBitsForNeighbourhood |= base.Empire.Bits;
							list.Add(this.WorldPositionningService.World.Regions[neighbourRegionIndex]);
						}
					}
				}
			}
		}
		base.Empire.SetPropertyBaseValue(SimulationProperties.TemplesSearchedThisSeason, 0f);
		base.Empire.Refresh(false);
	}

	private void RefreshDefensiveTowerPower(Region region)
	{
		if (region == null || region.PointOfInterests == null)
		{
			return;
		}
		float value;
		if (region.City == null)
		{
			value = 0f;
		}
		else
		{
			value = region.City.GetPropertyBaseValue(SimulationProperties.CityDefensePoint);
		}
		for (int i = 0; i < region.PointOfInterests.Length; i++)
		{
			if (region.PointOfInterests[i] != null)
			{
				region.PointOfInterests[i].SetPropertyBaseValue(SimulationProperties.CityDefensePoint, value);
				region.PointOfInterests[i].Refresh(true);
			}
		}
	}

	private void RemoveCity(City city, bool swapping)
	{
		if (city.Empire != null && city.Empire != base.Empire)
		{
			Diagnostics.LogError("The department of the interior was asked to remove a city (guid: {0}, empire: {1}) but it is still bound to another empire.", new object[]
			{
				city.GUID,
				city.Empire.Name
			});
			return;
		}
		int num = this.cities.BinarySearch((City match) => match.GUID.CompareTo(city.GUID));
		if (num < 0)
		{
			Diagnostics.LogWarning("The department of the interior was asked to remove a city (guid: {0}) but it is not present in its list of cities.", new object[]
			{
				city.GUID
			});
			return;
		}
		if (city.Hero != null)
		{
			city.SetHero(null);
		}
		this.cities.RemoveAt(num);
		base.Empire.SetPropertyBaseValue(SimulationProperties.NumberOfCities, base.Empire.GetPropertyValue(SimulationProperties.NumberOfCities) - 1f);
		if (this.MainCity == city)
		{
			this.MainCity = null;
		}
		base.Empire.RemoveChild(city);
		base.Empire.Refresh(true);
		this.OnCitiesCollectionChanged(city, CollectionChangeAction.Remove);
		Region region = this.WorldPositionningService.GetRegion(city.WorldPosition);
		for (int i = 0; i < region.PointOfInterests.Length; i++)
		{
			string a;
			if (!region.PointOfInterests[i].PointOfInterestDefinition.TryGetValue("Type", out a) || !(a == "Village"))
			{
				city.RemoveChild(region.PointOfInterests[i]);
				if (region.PointOfInterests[i].Empire == base.Empire && region.PointOfInterests[i].CreepingNodeGUID == GameEntityGUID.Zero)
				{
					region.PointOfInterests[i].Empire = null;
				}
			}
		}
		if (!swapping && city.CadastralMap.Roads != null)
		{
			Diagnostics.Assert(this.GameService != null);
			Diagnostics.Assert(this.GameService.Game != null);
			ICadasterService service = this.GameService.Game.Services.GetService<ICadasterService>();
			if (service != null)
			{
				service.Disconnect(city, PathfindingMovementCapacity.All, true);
				service.RefreshCadasterMap();
			}
			city.CadastralMap.Roads.Clear();
			city.CadastralMap.Roads = null;
		}
		this.RemoveDistrictDescriptorExploitableResource(city);
		SimulationDescriptor descriptor;
		if (city.SimulationObject.Tags.Contains(City.MimicsCity) && this.SimulationDescriptorDatabase.TryGetValue(City.MimicsCity, out descriptor))
		{
			city.RemoveDescriptor(descriptor);
		}
		SimulationDescriptor descriptor2;
		if (city.IsIntegrated && this.SimulationDescriptorDatabase.TryGetValue(City.TagCityStatusIntegrated, out descriptor2))
		{
			city.RemoveDescriptor(descriptor2);
		}
		region.City = null;
		this.RefreshDefensiveTowerPower(region);
		city.Region = null;
		city.Empire = null;
	}

	private void UpdateDistrictLevel(City city, District lastChangedDistrict)
	{
		Queue<District> queue = new Queue<District>(8);
		queue.Enqueue(lastChangedDistrict);
		lastChangedDistrict.SetLevel(-1, true);
		int b = (int)city.GetPropertyValue(SimulationProperties.MaximumDistrictLevel);
		int num = 6;
		List<District> list = new List<District>(7);
		List<int> list2 = new List<int>();
		while (queue.Count > 0)
		{
			District district3 = queue.Dequeue();
			if (district3 != null && district3.Type != DistrictType.Exploitation)
			{
				int num2 = 0;
				list2.Clear();
				for (int i = 0; i < num; i++)
				{
					WorldPosition position = this.WorldPositionningService.GetNeighbourTile(district3.WorldPosition, (WorldOrientation)i, 1);
					District district2 = city.Districts.FirstOrDefault((District district) => district.WorldPosition == position);
					if (district2 != null && district2.Type != DistrictType.Exploitation)
					{
						list.Add(district2);
						while (list2.Count < district2.Level + 1)
						{
							list2.Add(0);
						}
						for (int j = 0; j < district2.Level + 1; j++)
						{
							List<int> list4;
							List<int> list3 = list4 = list2;
							int num3;
							int index = num3 = j;
							num3 = list4[num3];
							list3[index] = num3 + 1;
						}
						num2++;
					}
				}
				district3.SetPropertyBaseValue(SimulationProperties.NumberOfExtensionAround, (float)num2);
				int k;
				for (k = 0; k < list2.Count; k++)
				{
					if (list2[k] < DepartmentOfTheInterior.MinimumNumberOfExtensionNeighbourForLevelUp)
					{
						break;
					}
				}
				k = Mathf.Min(k, b);
				if (district3.Level != k)
				{
					StaticString staticString = "DistrictLevel" + k;
					SimulationDescriptor descriptor;
					if (!district3.SimulationObject.Tags.Contains(staticString) && this.SimulationDescriptorDatabase.TryGetValue(staticString, out descriptor))
					{
						district3.AddDescriptor(descriptor, true);
					}
					district3.SetLevel(k, false);
					for (int l = 0; l < list.Count; l++)
					{
						if (!queue.Contains(list[l]))
						{
							queue.Enqueue(list[l]);
						}
					}
				}
				list.Clear();
			}
		}
	}

	private void UpdatePointOfInterestImprovement(City city)
	{
		for (int i = 0; i < city.Region.PointOfInterests.Length; i++)
		{
			PointOfInterest pointOfInterest = city.Region.PointOfInterests[i];
			this.UpdatePointOfInterestImprovement(city, pointOfInterest);
		}
	}

	private void UpdatePointOfInterestImprovement(City city, PointOfInterest pointOfInterest)
	{
		List<StaticString> lastFailureFlags = new List<StaticString>();
		global::Empire empire = base.Empire as global::Empire;
		SimulationDescriptor value = this.SimulationDescriptorDatabase.GetValue(empire.Faction.AffinityMapping);
		Diagnostics.Assert(value != null);
		if (pointOfInterest.PointOfInterestImprovement != null)
		{
			pointOfInterest.SwapDescriptor(value);
		}
		if (!DepartmentOfTheInterior.IsPointOfInterestVisible(base.Empire, pointOfInterest))
		{
			return;
		}
		if (pointOfInterest.PointOfInterestImprovement != null)
		{
			PointOfInterestImprovementDefinition bestImprovementDefinition = this.GetBestImprovementDefinition(city, pointOfInterest, pointOfInterest.PointOfInterestImprovement as PointOfInterestImprovementDefinition, lastFailureFlags);
			if (bestImprovementDefinition != null && bestImprovementDefinition.Name != pointOfInterest.PointOfInterestImprovement.Name)
			{
				this.BuildPointOfInterestImprovement(city, pointOfInterest, bestImprovementDefinition);
			}
		}
		else
		{
			for (int i = 0; i < city.Districts.Count; i++)
			{
				if (city.Districts[i].WorldPosition == pointOfInterest.WorldPosition && city.Districts[i].Type != DistrictType.Exploitation)
				{
					this.BuildFreePointOfInterestImprovement(city, pointOfInterest.WorldPosition);
				}
			}
		}
	}

	private void UpdatePointOfInterestImprovement(Camp camp)
	{
		City city = camp.City;
		List<StaticString> lastFailureFlags = new List<StaticString>();
		global::Empire empire = base.Empire as global::Empire;
		SimulationDescriptor value = this.SimulationDescriptorDatabase.GetValue(empire.Faction.AffinityMapping);
		Diagnostics.Assert(value != null);
		for (int i = 0; i < city.Region.PointOfInterests.Length; i++)
		{
			PointOfInterest pointOfInterest = city.Region.PointOfInterests[i];
			if (pointOfInterest.PointOfInterestImprovement != null)
			{
				pointOfInterest.SwapDescriptor(value);
			}
			if (DepartmentOfTheInterior.IsPointOfInterestVisible(base.Empire, pointOfInterest))
			{
				if (pointOfInterest.PointOfInterestImprovement != null)
				{
					PointOfInterestImprovementDefinition bestImprovementDefinition = this.GetBestImprovementDefinition(camp, pointOfInterest, pointOfInterest.PointOfInterestImprovement as PointOfInterestImprovementDefinition, lastFailureFlags);
					if (bestImprovementDefinition != null && bestImprovementDefinition.Name != pointOfInterest.PointOfInterestImprovement.Name)
					{
						this.BuildPointOfInterestImprovement(city, pointOfInterest, bestImprovementDefinition);
					}
				}
				else if (camp.WorldPosition == pointOfInterest.WorldPosition)
				{
					this.BuildFreePointOfInterestImprovement(city, pointOfInterest.WorldPosition);
				}
			}
		}
	}

	private void AddDistrictDescriptorExploitableResource(City city)
	{
		List<StaticString> list = new List<StaticString>();
		IDatabase<DepartmentOfScience.ConstructibleElement> database = Databases.GetDatabase<DepartmentOfScience.ConstructibleElement>(false);
		DepartmentOfScience.ConstructibleElement[] values = database.GetValues();
		for (int i = 0; i < values.Length; i++)
		{
			TechnologyDefinition technologyDefinition = values[i] as TechnologyDefinition;
			if (technologyDefinition != null && technologyDefinition.Visibility == TechnologyDefinitionVisibility.Visible && this.departmentOfScience.GetTechnologyState(technologyDefinition) == DepartmentOfScience.ConstructibleElement.State.Researched)
			{
				List<ConstructibleElement> unlocksByTechnology = values[i].GetUnlocksByTechnology();
				if (unlocksByTechnology != null && unlocksByTechnology.Count > 0)
				{
					for (int j = 0; j < unlocksByTechnology.Count; j++)
					{
						PointOfInterestImprovementDefinition pointOfInterestImprovementDefinition = unlocksByTechnology[j] as PointOfInterestImprovementDefinition;
						if (pointOfInterestImprovementDefinition != null)
						{
							list.Add(pointOfInterestImprovementDefinition.PointOfInterestTemplateName);
						}
					}
				}
			}
		}
		for (int k = 0; k < city.Region.PointOfInterests.Length; k++)
		{
			PointOfInterest pointOfInterest = city.Region.PointOfInterests[k];
			if (pointOfInterest.IsResourceDeposit() && list.Contains(pointOfInterest.PointOfInterestDefinition.PointOfInterestTemplateName))
			{
				for (int l = 0; l < city.Districts.Count; l++)
				{
					District district = city.Districts[l];
					SimulationDescriptor descriptor;
					if (district.WorldPosition == pointOfInterest.WorldPosition && !district.SimulationObject.Tags.Contains("DistrictExploitableResource") && this.SimulationDescriptorDatabase.TryGetValue("DistrictExploitableResource", out descriptor))
					{
						district.AddDescriptor(descriptor, false);
						break;
					}
				}
			}
		}
	}

	private void RemoveDistrictDescriptorExploitableResource(City city)
	{
		for (int i = 0; i < city.Districts.Count; i++)
		{
			District district = city.Districts[i];
			SimulationDescriptor descriptor;
			if (district.SimulationObject.Tags.Contains("DistrictExploitableResource") && this.SimulationDescriptorDatabase.TryGetValue("DistrictExploitableResource", out descriptor))
			{
				district.RemoveDescriptor(descriptor);
			}
		}
	}

	private void EventService_EventRaise(object sender, EventRaiseEventArgs e)
	{
		DepartmentOfTheInterior.EventHandler eventHandler;
		if (this.eventHandlers != null && e.RaisedEvent != null && this.eventHandlers.TryGetValue(e.RaisedEvent.EventName, out eventHandler))
		{
			eventHandler(e.RaisedEvent);
		}
	}

	private void RegisterEventHandlers()
	{
		this.eventHandlers = new Dictionary<StaticString, DepartmentOfTheInterior.EventHandler>();
		this.eventHandlers.Add(EventFactionIntegrated.Name, new DepartmentOfTheInterior.EventHandler(this.OnEventFactionIntegrated));
	}

	private void OnEventFactionIntegrated(Amplitude.Unity.Event.Event eventRaised)
	{
		EventFactionIntegrated eventFactionIntegrated = eventRaised as EventFactionIntegrated;
		if (eventFactionIntegrated != null && eventFactionIntegrated.Empire == base.Empire)
		{
			Faction faction = (eventFactionIntegrated.IntegratedEmpire as global::Empire).Faction;
			StaticString name = faction.Affinity.Name;
			SimulationDescriptor descriptor;
			if (this.SimulationDescriptorDatabase.TryGetValue(City.TagCityStatusIntegrated, out descriptor))
			{
				for (int i = 0; i < this.Cities.Count; i++)
				{
					if (this.Cities[i].IsInfected)
					{
						global::Empire empire = (this.GameService.Game as global::Game).Empires[this.Cities[i].LastNonInfectedOwnerIndex];
						string text = (empire == null) ? string.Empty : empire.Faction.Affinity.Name.ToString();
						bool flag = name == "AffinityMezari" || name == "AffinityVaulters";
						bool flag2 = text == "AffinityMezari" || text == "AffinityVaulters";
						if (name == text || (flag && flag2))
						{
							this.Cities[i].AddDescriptor(descriptor, false);
						}
					}
				}
			}
		}
	}

	private void DestroyInfectedCities()
	{
		City[] array = this.Cities.ToArray<City>();
		if (array != null && array.Length > 0)
		{
			int num = array.Length;
			for (int i = num - 1; i >= 0; i--)
			{
				if (array[i].IsInfected)
				{
					global::PlayerController server = (base.Empire as global::Empire).PlayerControllers.Server;
					if (server != null)
					{
						OrderDestroyCity order = new OrderDestroyCity(base.Empire.Index, array[i].GUID, array[i].ShouldRazeRegionBuildingWithSelf, array[i].ShouldInjureSpyOnRaze, -1);
						server.PostOrder(order);
					}
				}
			}
		}
	}

	private const int MaximumNumberOfAssimilatedEmpires = 3;

	private const int MaximumNumberOfNeighbourTiles = 6;

	public static StaticString ArmyStatusEarthquakerDescriptorName;

	private string serializableBesiegingSeafaringArmies;

	private List<Faction> assimilatedFactions = new List<Faction>();

	private ReadOnlyCollection<Faction> readOnlyAssimilatedFactions;

	private List<StaticString> encounteredMinorFaction = new List<StaticString>();

	private List<StaticString> alreadyNotifyForAssimilation = new List<StaticString>();

	public static readonly StaticString OccupiedCitadel = "OccupiedCitadel";

	public static readonly StaticString FortressBonusesOnRegionControlled = "FortressBonusesOnRegionControlled";

	private List<Fortress> occupiedFortresses = new List<Fortress>();

	private List<Region> occupiedOceanRegions = new List<Region>();

	private ReadOnlyCollection<Fortress> readOnlyOccupiedFortresses;

	private ReadOnlyCollection<Region> readOnlyOccupiedRegions;

	public static readonly StaticString TagConvertedVillageUnit = new StaticString("ConvertedVillageUnit");

	private static readonly StaticString DefaultConvertedUnitDescriptorName = new StaticString("DefaultConvertedVillageUnit");

	public static StaticString ArmyStatusBesiegerDescriptorName = "ArmyStatusBesieger";

	public static StaticString ArmyStatusDefenderDescriptorName = "ArmyStatusCityDefender";

	public static StaticString CityStatusSiegeDescriptorName = "CityStatusSiege";

	public static StaticString ArmyStatusSeafaringBesiegerDescriptorName = "ArmyStatusSeafaringBesieger";

	public static StaticString CityStatusNavalSiegeDescriptorName = "CityStatusNavalSiege";

	public static int MinimumNumberOfExtensionNeighbourForLevelUp = 4;

	public static Amplitude.Unity.Framework.AnimationCurve MilitiaRecoverByOwnershipCurve;

	public static Amplitude.Unity.Framework.AnimationCurve FortificationRecoverByOwnershipCurve;

	public static readonly StaticString InfectionAllowedSubcategory = "SubCategoryAssimilation";

	internal static readonly StaticString SimulationLayerName = "Simulation";

	internal static readonly StaticString SimulationLayerType = "Simulation";

	internal static readonly StaticString PointOfInterestOnDistrict = "PointOfInterestOnDistrict";

	internal static readonly StaticString ReadOnlyUniques = "Uniques";

	internal static readonly StaticString CityStatusRoundUpDescriptorName = "CityStatusRoundUp";

	private static object[] growthFormulaTokens;

	private static object[] assimilationFormulaTokens;

	private static InterpreterContext growthInterpreterContext;

	private static InterpreterContext assimilationInterpreterContext;

	private static SimulationPath empireSimulationPath = new SimulationPath("../ClassEmpire");

	private List<City> cities = new List<City>();

	private List<OrderCreateDistrictImprovement> pendingExtensions = new List<OrderCreateDistrictImprovement>();

	private List<OrderCreateConstructibleDistrict> pendingDistrictConstructions = new List<OrderCreateConstructibleDistrict>();

	private Dictionary<FactionTrait, int> factionTraitReferenceCount = new Dictionary<FactionTrait, int>();

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private DepartmentOfIndustry departmentOfIndustry;

	private DepartmentOfIntelligence departmentOfIntelligence;

	private DepartmentOfScience departmentOfScience;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private List<DepartmentOfTheInterior.SpecializedPopulation> specializedPopulations = new List<DepartmentOfTheInterior.SpecializedPopulation>();

	private IEndTurnService endTurnService;

	private IPlayerControllerRepositoryService playerControllerRepositoryService;

	private ISeasonService seasonService;

	private Dictionary<StaticString, DepartmentOfTheInterior.EventHandler> eventHandlers;

	private City mainCity;

	private GameEntityGUID mainCityGUID;

	private ReadOnlyCollection<City> readOnlyCities;

	private SimulationObject assimilatedFactionSimulationObject;

	private SimulationObjectWrapper uniques;

	[Flags]
	public enum BesiegingSeafaringArmyStatus
	{
		CityDefensePointLossPerTurn = 0
	}

	private class SpecializedPopulation
	{
		public SpecializedPopulation(StaticString propertyName, StaticString perPopulationNetPropertyName, int priority, float value = 0f)
		{
			this.PropertyName = propertyName;
			this.PerPopulationNetPropertyName = perPopulationNetPropertyName;
			this.Value = value;
			this.Priority = priority;
		}

		public StaticString PropertyName { get; set; }

		public StaticString PerPopulationNetPropertyName { get; set; }

		public float Value { get; set; }

		public float PerPopulationValue { get; set; }

		public int Priority { get; set; }
	}

	private delegate void EventHandler(Amplitude.Unity.Event.Event raisedEvent);
}
