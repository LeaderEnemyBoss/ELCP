using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Extensions;
using Amplitude.Path;
using Amplitude.Test;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Game.Orders;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Serialization;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Unity.Xml;
using Amplitude.Utilities.Maps;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

[Diagnostics.TagAttribute("UnitTests")]
[OrderProcessor(typeof(OrderDisbandArmy), "DisbandArmy")]
[OrderProcessor(typeof(OrderRetrofitUnit), "RetrofitUnit")]
[OrderProcessor(typeof(OrderDisbandProducedUnits), "DisbandProducedUnits")]
[OrderProcessor(typeof(OrderHealUnitsThroughArmy), "HealUnitsThroughArmy")]
[OrderProcessor(typeof(OrderSellUnits), "SellUnits")]
[OrderProcessor(typeof(OrderSpawnArmies), "SpawnArmies")]
[OrderProcessor(typeof(OrderSpawnArmy), "SpawnArmy")]
[OrderProcessor(typeof(OrderSpawnUnit), "SpawnUnit")]
[OrderProcessor(typeof(OrderHealUnits), "HealUnits")]
[OrderProcessor(typeof(OrderFortifyCityDefensesThroughArmy), "FortifyCityDefensesThroughArmy")]
[OrderProcessor(typeof(OrderToggleAspirate), "ToggleAspirate")]
[OrderProcessor(typeof(OrderDisbandUnits), "DisbandUnits")]
[OrderProcessor(typeof(OrderPillageSucceed), "PillageSucceed")]
[OrderProcessor(typeof(OrderDestroyMilitiaUnits), "DestroyMilitiaUnits")]
[OrderProcessor(typeof(OrderDestroyArmy), "DestroyArmy")]
[OrderProcessor(typeof(OrderCreateUnitDesign), "CreateUnitDesign")]
[OrderProcessor(typeof(OrderCreateUndeadUnits), "CreateUndeadUnits")]
[OrderProcessor(typeof(OrderKaijuChangeMode), "KaijuChangeMode")]
[OrderProcessor(typeof(OrderToggleAutoExplore), "ToggleAutoExplore")]
[OrderProcessor(typeof(OrderToggleCatspaw), "ToggleCatspaw")]
[OrderProcessor(typeof(OrderToggleDismantleCreepingNode), "ToggleDismantleCreepingNode")]
[OrderProcessor(typeof(OrderToggleDismantleDevice), "ToggleDismantleDevice")]
[OrderProcessor(typeof(OrderAttackCity), "AttackCity")]
[OrderProcessor(typeof(OrderRemoveUnitDesign), "RemoveUnitDesign")]
[OrderProcessor(typeof(OrderAttack), "Attack")]
[OrderProcessor(typeof(OrderDismantleCreepingNodeSucceed), "DismantleCreepingNodeSucceed")]
[OrderProcessor(typeof(OrderEnablePortableForgeOnArmy), "EnablePortableForgeOnArmy")]
[OrderProcessor(typeof(OrderEditUnitDesign), "EditUnitDesign")]
[OrderProcessor(typeof(OrderToggleEarthquake), "ToggleEarthquake")]
[OrderProcessor(typeof(OrderToggleGuard), "ToggleGuard")]
[OrderProcessor(typeof(OrderToggleNavalSiege), "ToggleNavalSiege")]
[OrderProcessor(typeof(OrderTogglePillage), "TogglePillage")]
[OrderProcessor(typeof(OrderTogglePrivateers), "TogglePrivateers")]
[OrderProcessor(typeof(OrderToggleSiege), "ToggleSiege")]
[OrderProcessor(typeof(OrderTransferAcademyToNewArmy), "TransferAcademyToNewArmy")]
[OrderProcessor(typeof(OrderExecuteArmyAction), "ExecuteArmyAction")]
[OrderProcessor(typeof(OrderTransferGarrisonToNewArmy), "TransferGarrisonToNewArmy")]
[OrderProcessor(typeof(OrderCreateMilitiaUnits), "CreateMilitiaUnits")]
[OrderProcessor(typeof(OrderTransferSolitaryUnitToNewArmy), "TransferSolitaryUnitToNewArmy")]
[OrderProcessor(typeof(OrderTransferUnits), "TransferUnits")]
[OrderProcessor(typeof(OrderUpdateArmyObjective), "UpdateArmyObjective")]
[OrderProcessor(typeof(OrderDismantleDeviceSucceed), "DismantleDeviceSucceed")]
[OrderProcessor(typeof(OrderLockTarget), "LockTarget")]
[OrderProcessor(typeof(OrderEditHeroUnitDesign), "EditHeroUnitDesign")]
[OrderProcessor(typeof(OrderUpdateMilitia), "UpdateMilitia")]
[OrderProcessor(typeof(OrderWoundUnit), "WoundUnit")]
[OrderProcessor(typeof(OrderTransferSeafaringUnitToNewArmy), "TransferSeafaringUnitToNewArmy")]
public class DepartmentOfDefense : Agency, Amplitude.Xml.Serialization.IXmlSerializable, IUnitDesignDatabase
{
	public DepartmentOfDefense(global::Empire empire) : base(empire)
	{
	}

	public event DepartmentOfDefense.AvailableUnitBodyChangeEventHandler AvailableUnitBodyChanged;

	public event DepartmentOfDefense.AvailableUnitDesignChangeEventHandler AvailableUnitDesignChanged;

	public event EventHandler<UnitDesignDatabaseChangeEventArgs> UnitDesignDatabaseChanged;

	public event EventHandler ArmyActionStateChange;

	public event EventHandler GarrisonActionStateChange;

	public event CollectionChangeEventHandler ArmiesCollectionChange;

	public event EventHandler<ArmyObjectiveUpdatedEventArgs> ObjectiveUpdateChange;

	public event EventHandler<AttackStartEventArgs> OnAttackStart;

	public event EventHandler<SiegeStateChangedEventArgs> OnSiegeStateChange;

	ReadOnlyCollection<UnitBodyDefinition> IUnitDesignDatabase.AvailableUnitBodyDefinitions
	{
		get
		{
			return this.availableUnitBodyDefinitions.AsReadOnly();
		}
	}

	ReadOnlyCollection<UnitDesign> IUnitDesignDatabase.DatabaseCompatibleUnitDesigns
	{
		get
		{
			return this.hiddenUnitDesigns.AsReadOnly();
		}
	}

	ReadOnlyCollection<UnitDesign> IUnitDesignDatabase.UserDefinedUnitDesigns
	{
		get
		{
			return this.availableUnitDesigns.AsReadOnly();
		}
	}

	bool IUnitDesignDatabase.CheckWhetherUnitDesignIsValid(UnitDesign unitDesign)
	{
		if (unitDesign == null)
		{
			return false;
		}
		if (!this.CheckWhetherUnitBodyDefinitionIsValid(unitDesign.UnitBodyDefinition))
		{
			return false;
		}
		if (!this.CheckWhetherUnitDesignMatchUnitBodyEquipmentSet(unitDesign))
		{
			return false;
		}
		if (this.departmentOfTheTreasury != null && !DepartmentOfTheTreasury.CheckConstructiblePrerequisites(base.Empire, unitDesign, new string[]
		{
			"Validity"
		}))
		{
			return false;
		}
		Diagnostics.Assert(this.outdatedUnitDesigns != null);
		return !this.outdatedUnitDesigns.Any((UnitDesign match) => match.Model == unitDesign.Model && match.ModelRevision >= unitDesign.ModelRevision);
	}

	IEnumerable<UnitBodyDefinition> IUnitDesignDatabase.GetAvailableUnitBodyDefinitionsAsEnumerable()
	{
		foreach (UnitBodyDefinition unitBodyDefinition in this.availableUnitBodyDefinitions)
		{
			yield return unitBodyDefinition;
		}
		yield break;
	}

	IEnumerable<UnitDesign> IUnitDesignDatabase.GetAvailableUnitDesignsAsEnumerable()
	{
		foreach (UnitDesign unitDesign in this.hiddenUnitDesigns)
		{
			yield return unitDesign;
		}
		foreach (UnitDesign unitDesign2 in this.availableUnitDesigns)
		{
			yield return unitDesign2;
		}
		yield break;
	}

	IEnumerable<UnitDesign> IUnitDesignDatabase.GetDatabaseCompatibleUnitDesignsAsEnumerable()
	{
		foreach (UnitDesign unitDesign in this.hiddenUnitDesigns)
		{
			yield return unitDesign;
		}
		yield break;
	}

	IEnumerable<UnitDesign> IUnitDesignDatabase.GetUserDefinedUnitDesignsAsEnumerable()
	{
		foreach (UnitDesign unitDesign in this.availableUnitDesigns)
		{
			yield return unitDesign;
		}
		yield break;
	}

	bool IUnitDesignDatabase.TryGetValue(uint model, out UnitDesign unitDesign, bool searchAlsoInOutdated)
	{
		unitDesign = null;
		foreach (UnitDesign unitDesign2 in ((IUnitDesignDatabase)this).GetAvailableUnitDesignsAsEnumerable())
		{
			if (unitDesign2.Model == model)
			{
				if (unitDesign == null || unitDesign2.ModelRevision > unitDesign.ModelRevision)
				{
					unitDesign = unitDesign2;
				}
			}
		}
		if (searchAlsoInOutdated)
		{
			foreach (UnitDesign unitDesign3 in ((IUnitDesignDatabase)this).GetOutdatedUnitDesignsAsEnumerable())
			{
				if (unitDesign3.Model == model)
				{
					if (unitDesign == null || unitDesign3.ModelRevision > unitDesign.ModelRevision)
					{
						unitDesign = unitDesign3;
					}
				}
			}
		}
		return unitDesign != null;
	}

	bool IUnitDesignDatabase.TryGetValue(uint model, uint modelRevision, out UnitDesign unitDesign, bool searchAlsoInOutdated)
	{
		unitDesign = ((IUnitDesignDatabase)this).GetAvailableUnitDesignsAsEnumerable().FirstOrDefault((UnitDesign constructibleElement) => constructibleElement.Model == model && constructibleElement.ModelRevision == modelRevision);
		if (unitDesign != null)
		{
			return true;
		}
		if (searchAlsoInOutdated)
		{
			unitDesign = ((IUnitDesignDatabase)this).GetOutdatedUnitDesignsAsEnumerable().FirstOrDefault((UnitDesign constructibleElement) => constructibleElement.Model == model && constructibleElement.ModelRevision == modelRevision);
			if (unitDesign != null)
			{
				return true;
			}
		}
		return false;
	}

	bool IUnitDesignDatabase.TryGetValue(StaticString unitDesignName, out UnitDesign unitDesign, bool searchAlsoInOutdated)
	{
		unitDesign = ((IUnitDesignDatabase)this).GetAvailableUnitDesignsAsEnumerable().FirstOrDefault((UnitDesign constructibleElement) => constructibleElement.FullName == unitDesignName);
		if (unitDesign != null)
		{
			return true;
		}
		if (searchAlsoInOutdated)
		{
			unitDesign = ((IUnitDesignDatabase)this).GetOutdatedUnitDesignsAsEnumerable().FirstOrDefault((UnitDesign constructibleElement) => constructibleElement.FullName == unitDesignName);
			if (unitDesign != null)
			{
				return true;
			}
		}
		return false;
	}

	public bool CanStartAspirate(Army army, PointOfInterest pointOfInterest)
	{
		if (army.Empire == null)
		{
			return false;
		}
		if (army.WorldPath != null && army.WorldPosition != army.WorldPath.Destination)
		{
			bool flag = true;
			for (int i = 0; i < army.WorldPath.ControlPoints.Length; i++)
			{
				if (army.WorldPosition == army.WorldPath.WorldPositions[(int)army.WorldPath.ControlPoints[i]])
				{
					flag = false;
				}
			}
			if (flag)
			{
				return false;
			}
		}
		if (army.IsInEncounter)
		{
			return false;
		}
		if (army.IsPillaging)
		{
			return false;
		}
		if (army.IsDismantlingDevice)
		{
			return false;
		}
		if (army.IsDismantlingCreepingNode)
		{
			return false;
		}
		if (army.SimulationObject == null || army.SimulationObject.Tags == null || army.SimulationObject.Tags.Contains(DepartmentOfTheInterior.ArmyStatusBesiegerDescriptorName))
		{
			return false;
		}
		bool flag2 = false;
		foreach (Unit unit in army.Units)
		{
			if (unit.CheckUnitAbility("UnitAbilityHarbinger", -1))
			{
				flag2 = true;
			}
		}
		return flag2 && this.IsPointOfInterestSuitableForAspirate(pointOfInterest);
	}

	public void StopAspirating(Army army)
	{
		if (!army.IsAspirating)
		{
			return;
		}
		IGameEntity gameEntity = null;
		PointOfInterest pointOfInterest = null;
		if (this.GameEntityRepositoryService.TryGetValue(army.AspirateTarget, out gameEntity))
		{
			pointOfInterest = (gameEntity as PointOfInterest);
			if (pointOfInterest != null && pointOfInterest.PointOfInterestDefinition != null)
			{
				string str = null;
				if (!pointOfInterest.PointOfInterestDefinition.TryGetValue("ResourceName", out str))
				{
					Diagnostics.LogError("Fail getting POI resource name.");
					return;
				}
				StaticString propertyName = "Aspirate" + str;
				army.SetPropertyBaseValue(propertyName, 0f);
				army.Refresh(false);
			}
			if (pointOfInterest != null && pointOfInterest.Region.City != null && pointOfInterest.Region.City.Empire != null)
			{
				this.visibilityService.NotifyVisibilityHasChanged(pointOfInterest.Region.City.Empire);
				IDatabase<SimulationDescriptor> database = Databases.GetDatabase<SimulationDescriptor>(false);
				if (database == null)
				{
					return;
				}
				SimulationDescriptor descriptor;
				if (database.TryGetValue("MantaApprovalBonus", out descriptor))
				{
					pointOfInterest.Region.City.RemoveDescriptor(descriptor);
					pointOfInterest.Region.City.Refresh(false);
				}
			}
		}
		ArmyAction armyAction = null;
		IDatabase<ArmyAction> database2 = Databases.GetDatabase<ArmyAction>(false);
		if (database2.TryGetValue("ArmyActionAspirate", out armyAction) && armyAction.ExperienceReward > 0f)
		{
			foreach (Unit unit in army.Units)
			{
				float num = unit.GetPropertyBaseValue(SimulationProperties.UnitExperienceGainPerTurn);
				num -= armyAction.ExperienceReward;
				unit.SetPropertyBaseValue(SimulationProperties.UnitExperienceGainPerTurn, num);
				unit.Refresh(false);
			}
			if (army.Hero != null)
			{
				float num2 = army.Hero.GetPropertyBaseValue(SimulationProperties.UnitExperienceGainPerTurn);
				num2 -= armyAction.ExperienceReward;
				army.Hero.SetPropertyBaseValue(SimulationProperties.UnitExperienceGainPerTurn, num2);
				army.Hero.Refresh(false);
			}
		}
		army.AspirateTarget = GameEntityGUID.Zero;
		if (armyAction != null && pointOfInterest != null)
		{
			army.OnArmyAction(armyAction, pointOfInterest);
		}
	}

	private bool IsPointOfInterestSuitableForAspirate(PointOfInterest pointOfInterest)
	{
		if (pointOfInterest == null)
		{
			return false;
		}
		string a;
		if (!pointOfInterest.PointOfInterestDefinition.TryGetValue("Type", out a))
		{
			return false;
		}
		if (a != "ResourceDeposit")
		{
			return false;
		}
		if (this.departmentOfScience == null)
		{
			Diagnostics.LogWarning("DepartmentOfScience can't be null.");
			return false;
		}
		string technologyName;
		return pointOfInterest.PointOfInterestDefinition.TryGetValue("VisibilityTechnology", out technologyName) && this.departmentOfScience.GetTechnologyState(technologyName) == DepartmentOfScience.ConstructibleElement.State.Researched && (pointOfInterest.Region.City == null || !pointOfInterest.SimulationObject.Tags.Contains("ExploitedPointOfInterest"));
	}

	private void UpdateAspirate_OnWorldPositionChange(object sender, ArmyWorldPositionChangeEventArgs e)
	{
		Army army = sender as Army;
		if (army == null)
		{
			return;
		}
		GridMap<PointOfInterest> gridMap = (this.GameService.Game as global::Game).World.Atlas.GetMap(WorldAtlas.Maps.PointOfInterest) as GridMap<PointOfInterest>;
		PointOfInterest value = gridMap.GetValue((int)army.WorldPosition.Row, (int)army.WorldPosition.Column);
		if (this.CanStartAspirate(army, value))
		{
			this.StartAspirate(army, value);
		}
	}

	private void StartAspirate(Army army, PointOfInterest pointOfInterest)
	{
		army.AspirateTarget = pointOfInterest.GUID;
		string str = null;
		if (!pointOfInterest.PointOfInterestDefinition.TryGetValue("ResourceName", out str))
		{
			Diagnostics.LogError("Fail getting POI resource name.");
			return;
		}
		StaticString propertyName = "Aspirate" + str;
		float propertyValue = army.SimulationObject.GetPropertyValue("AspirateValue");
		army.SetPropertyBaseValue(propertyName, propertyValue);
		army.Refresh(false);
		if (pointOfInterest.Region.City != null && pointOfInterest.Region.City.Empire != null)
		{
			this.visibilityService.NotifyVisibilityHasChanged(pointOfInterest.Region.City.Empire);
			IDatabase<SimulationDescriptor> database = Databases.GetDatabase<SimulationDescriptor>(false);
			if (database == null)
			{
				return;
			}
			SimulationDescriptor descriptor;
			if (database.TryGetValue("MantaApprovalBonus", out descriptor))
			{
				pointOfInterest.Region.City.AddDescriptor(descriptor, false);
				pointOfInterest.Region.City.Refresh(false);
			}
		}
		ArmyAction armyAction = null;
		IDatabase<ArmyAction> database2 = Databases.GetDatabase<ArmyAction>(false);
		if (database2.TryGetValue("ArmyActionAspirate", out armyAction) && armyAction.ExperienceReward > 0f)
		{
			foreach (Unit unit in army.Units)
			{
				float num = unit.GetPropertyBaseValue(SimulationProperties.UnitExperienceGainPerTurn);
				num += armyAction.ExperienceReward;
				unit.SetPropertyBaseValue(SimulationProperties.UnitExperienceGainPerTurn, num);
				unit.Refresh(false);
			}
			if (army.Hero != null)
			{
				float num2 = army.Hero.GetPropertyBaseValue(SimulationProperties.UnitExperienceGainPerTurn);
				num2 += armyAction.ExperienceReward;
				army.Hero.SetPropertyBaseValue(SimulationProperties.UnitExperienceGainPerTurn, num2);
				army.Hero.Refresh(false);
			}
		}
		if (armyAction != null && pointOfInterest != null)
		{
			army.OnArmyAction(armyAction, pointOfInterest);
		}
	}

	private bool EnableDetection { get; set; }

	internal void UpdateDetection(Army army)
	{
		Diagnostics.Assert(army != null);
		if (!this.EnableDetection)
		{
			return;
		}
		bool camouflaged = false;
		float propertyValue = army.GetPropertyValue(SimulationProperties.LevelOfStealth);
		if (propertyValue > 0f)
		{
			camouflaged = true;
		}
		else
		{
			float propertyValue2 = army.GetPropertyValue(SimulationProperties.LevelOfCamouflage);
			if (propertyValue2 > 0f)
			{
				byte value = this.forests.GetValue(army.WorldPosition);
				if (value != 0)
				{
					camouflaged = true;
				}
				sbyte terrainHeight = this.WorldPositionningService.GetTerrainHeight(army.WorldPosition);
				if ((int)terrainHeight < -1)
				{
					camouflaged = true;
				}
			}
		}
		army.SetCamouflaged(camouflaged);
	}

	private void UpdateDetection_OnRefreshed(object sender)
	{
		Diagnostics.Assert(sender is Army);
		this.UpdateDetection((Army)sender);
	}

	private void UpdateDetection_OnWorldPositionChange(object sender, ArmyWorldPositionChangeEventArgs e)
	{
		this.UpdateDetection(e.Army);
	}

	public void StartDismantelingCreepingNode(Army army, CreepingNode creepingNode)
	{
		creepingNode.DismantlingArmyGUID = army.GUID;
		creepingNode.LastTurnWhenDismantleBegun = ((global::Game)this.GameService.Game).Turn;
		army.DismantlingCreepingNodeTarget = creepingNode.GUID;
		army.WorldOrientation = this.WorldPositionningService.GetOrientation(army.WorldPosition, creepingNode.WorldPosition);
		SimulationDescriptor descriptor;
		if (this.SimulationDescriptorDatabase.TryGetValue(DepartmentOfCreepingNodes.DismantelingStatus, out descriptor))
		{
			creepingNode.AddDescriptor(descriptor, false);
		}
		creepingNode.Refresh(false);
	}

	public void StopDismantelingCreepingNode(Army army, CreepingNode creepingNode)
	{
		creepingNode.DismantlingArmyGUID = GameEntityGUID.Zero;
		army.DismantlingCreepingNodeTarget = GameEntityGUID.Zero;
		SimulationDescriptor descriptor;
		if (this.SimulationDescriptorDatabase.TryGetValue(DepartmentOfCreepingNodes.DismantelingStatus, out descriptor))
		{
			creepingNode.RemoveDescriptor(descriptor);
		}
		creepingNode.Refresh(false);
	}

	public static bool CanDismantleCreepingNode(Army army, CreepingNode node, bool checkDistance = true)
	{
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		if (!service.IsShared(DownloadableContent20.ReadOnlyName))
		{
			return false;
		}
		if (node.SimulationObject.Tags.Contains(DepartmentOfCreepingNodes.DismantelingStatus))
		{
			return false;
		}
		if (node.Empire == army.Empire)
		{
			return false;
		}
		if (army.Empire is MajorEmpire)
		{
			DiplomaticRelation diplomaticRelation = army.Empire.GetAgency<DepartmentOfForeignAffairs>().DiplomaticRelations[node.Empire.Index];
			if (diplomaticRelation == null || diplomaticRelation.State == null || diplomaticRelation.State.Name == DiplomaticRelationState.Names.Alliance || diplomaticRelation.State.Name == DiplomaticRelationState.Names.Peace || diplomaticRelation.State.Name == DiplomaticRelationState.Names.Truce)
			{
				return false;
			}
		}
		if (army.Empire.GetAgency<DepartmentOfForeignAffairs>().IsFriend(node.Empire))
		{
			return false;
		}
		if (node.DismantlingArmyGUID.IsValid && army.GUID != node.DismantlingArmyGUID)
		{
			return false;
		}
		if (checkDistance)
		{
			int distance = DepartmentOfDefense.worldPositionningServiceStatic.GetDistance(army.WorldPosition, node.WorldPosition);
			if (distance > 1)
			{
				return false;
			}
			if (distance > 0 && !DepartmentOfDefense.pathfindingServiceStatic.IsTransitionPassable(army.WorldPosition, node.WorldPosition, army.GenerateContext().MovementCapacities, OrderAttack.AttackFlags))
			{
				return false;
			}
		}
		Army armyAtPosition = DepartmentOfDefense.worldPositionningServiceStatic.GetArmyAtPosition(node.WorldPosition);
		return armyAtPosition == null || armyAtPosition.Empire.Index == army.Empire.Index;
	}

	public void StartDismantelingDevice(Army army, TerraformDevice device)
	{
		device.DismantlingArmyGUID = army.GUID;
		device.LastTurnWhenDismantleBegun = ((global::Game)this.GameService.Game).Turn;
		if (device.ChargesToActivate > 2000f)
		{
			device.ChargesWhenDismantleStarted = 0f;
			device.Charges = 0f;
		}
		else
		{
			device.ChargesWhenDismantleStarted = device.Charges;
		}
		army.DismantlingDeviceTarget = device.GUID;
		army.WorldOrientation = this.WorldPositionningService.GetOrientation(army.WorldPosition, device.WorldPosition);
	}

	public void StopDismantelingDevice(Army army, TerraformDevice device)
	{
		device.DismantlingArmyGUID = GameEntityGUID.Zero;
		army.DismantlingDeviceTarget = GameEntityGUID.Zero;
		device.ChargesWhenDismantleStarted = 0f;
	}

	public static void ApplyEquipmentSet(Unit unit)
	{
		if (unit.UnitDesign == null)
		{
			return;
		}
		if (DepartmentOfDefense.staticItemDefinitionsDatabase == null)
		{
			DepartmentOfDefense.staticItemDefinitionsDatabase = Databases.GetDatabase<ItemDefinition>(true);
		}
		if (DepartmentOfDefense.staticSimulationDescriptorDatabase == null)
		{
			DepartmentOfDefense.staticSimulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
		}
		UnitEquipmentSet unitEquipmentSet = unit.UnitDesign.UnitEquipmentSet;
		if (unitEquipmentSet.Slots == null || unitEquipmentSet.Slots.Length == 0)
		{
			unitEquipmentSet = unit.UnitDesign.UnitBodyDefinition.UnitEquipmentSet;
		}
		int num = 0;
		while (unitEquipmentSet.Slots != null && num < unitEquipmentSet.Slots.Length)
		{
			UnitEquipmentSet.Slot slot = unitEquipmentSet.Slots[num];
			if (StaticString.IsNullOrEmpty(slot.Name))
			{
				Diagnostics.LogError("Slot name is either null or empty.");
			}
			else if (!StaticString.IsNullOrEmpty(slot.ItemName))
			{
				StaticString key = slot.ItemName.ToString().Split(DepartmentOfDefense.ItemSeparators)[0];
				ItemDefinition itemDefinition = null;
				if (!DepartmentOfDefense.staticItemDefinitionsDatabase.TryGetValue(key, out itemDefinition))
				{
					Diagnostics.LogError("Cannot find item definition ('{0}') in database.", new object[]
					{
						slot.ItemName
					});
				}
				else
				{
					DepartmentOfDefense.AddItemEquipment(unit, slot, itemDefinition, unitEquipmentSet);
				}
			}
			num++;
		}
	}

	public static bool CanLootArmies(global::Empire empire, Encounter encounter)
	{
		if (empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitBrokenLordsHeatWave) && SimulationGlobal.GlobalTagsContains(Season.ReadOnlyHeatWave))
		{
			for (int i = 0; i < encounter.Contenders.Count; i++)
			{
				Contender contender = encounter.Contenders[i];
				if (contender.Empire == empire)
				{
					for (int j = 0; j < contender.EncounterUnits.Count; j++)
					{
						UnitSnapShot unitCurrentSnapshot = contender.EncounterUnits[j].UnitCurrentSnapshot;
						for (int k = 0; k < unitCurrentSnapshot.ValidUnitAbilitiesReferences.Length; k++)
						{
							if (unitCurrentSnapshot.GetPropertyValue(SimulationProperties.Health) >= 0f && unitCurrentSnapshot.ValidUnitAbilitiesReferences[k].Name.Equals(UnitAbility.ReadonlyEssenceHarvest))
							{
								return true;
							}
						}
					}
				}
			}
		}
		return false;
	}

	public static bool CanPerformEarthquake(Army army, bool infectedCitiesAreAllowed, ref List<StaticString> failureFlags)
	{
		if (army == null || !army.WorldPosition.IsValid)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileSystemError);
			return false;
		}
		if (army.IsNaval)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		if (army.SimulationObject.Tags.Contains(DepartmentOfTheInterior.ArmyStatusBesiegerDescriptorName))
		{
			failureFlags.Contains(ArmyAction.NoCanDoWhileSieging);
			return false;
		}
		if (army.IsPillaging)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhilePillaging);
			return false;
		}
		if (army.IsAspirating)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileAspirating);
			return false;
		}
		if (army.IsDismantlingDevice)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileDismantlingDevice);
			return false;
		}
		if (army.IsDismantlingCreepingNode)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileDismantlingCreepingNode);
			return false;
		}
		if (army.IsLocked)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		if (service == null || !service.IsShared(DownloadableContent20.ReadOnlyName))
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileSystemError);
			return false;
		}
		IGameService service2 = Services.GetService<IGameService>();
		if (service2 == null)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileSystemError);
			return false;
		}
		IWorldPositionningService service3 = service2.Game.Services.GetService<IWorldPositionningService>();
		if (service3 == null)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileSystemError);
			return false;
		}
		Region region = service3.GetRegion(army.WorldPosition);
		City city = region.City;
		if (city == null || city.Empire.Index == army.Empire.Index)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		IVisibilityService service4 = service2.Game.Services.GetService<IVisibilityService>();
		if (service4 == null)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileSystemError);
			return false;
		}
		if (!service4.IsWorldPositionExploredFor(city.WorldPosition, army.Empire))
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		if (city.IsInfected && !infectedCitiesAreAllowed)
		{
			failureFlags.Add(ArmyAction_ToggleEarthquake.NoCanDoWhileCityIsInfected);
			return false;
		}
		DepartmentOfForeignAffairs agency = army.Empire.GetAgency<DepartmentOfForeignAffairs>();
		if (agency == null)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileSystemError);
			return false;
		}
		DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(city.Empire);
		if (diplomaticRelation == null || diplomaticRelation.State == null || StaticString.IsNullOrEmpty(diplomaticRelation.State.Name) || diplomaticRelation.State.Name.Equals(DiplomaticRelationState.Names.Unknown) || diplomaticRelation.State.Name.Equals(DiplomaticRelationState.Names.Dead))
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileHidden);
			return false;
		}
		if (army.IsInEncounter)
		{
			failureFlags.Add(ArmyAction.NoCanDoWhileLockedInBattle);
			return false;
		}
		return true;
	}

	public static bool CanPerformEarthquake(Army army, bool infectedCitiesAreAllowed)
	{
		List<StaticString> list = new List<StaticString>();
		return DepartmentOfDefense.CanPerformEarthquake(army, infectedCitiesAreAllowed, ref list);
	}

	public static List<WorldPosition> GetAvailablePositionsForArmyCreation(City city)
	{
		List<WorldPosition> list = new List<WorldPosition>();
		for (int i = 0; i < city.Districts.Count; i++)
		{
			District district = city.Districts[i];
			if (District.IsACityTile(district))
			{
				if (DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(district.WorldPosition, PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.Water))
				{
					list.Add(district.WorldPosition);
				}
			}
		}
		if (list.Count >= 0)
		{
			IGameService service = Services.GetService<IGameService>();
			IWorldPositionningService worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
			Diagnostics.Assert(worldPositionningService != null);
			list.Sort((WorldPosition left, WorldPosition right) => worldPositionningService.GetDistance(city.WorldPosition, left).CompareTo(worldPositionningService.GetDistance(city.WorldPosition, right)));
		}
		return list;
	}

	public static List<WorldPosition> GetAvailablePositionsForArmyCreation(Village village)
	{
		return DepartmentOfDefense.GetAvailablePositionsForArmyCreation(village.WorldPosition);
	}

	public static List<WorldPosition> GetAvailablePositionsForArmyCreation(Fortress fortress)
	{
		return DepartmentOfDefense.GetAvailablePositionsForArmyCreation(fortress.WorldPosition);
	}

	public static List<WorldPosition> GetAvailablePositionsForArmyCreation(KaijuGarrison kaijuGarrison)
	{
		return DepartmentOfDefense.GetAvailablePositionsForArmyCreation(kaijuGarrison.WorldPosition);
	}

	public static bool CanCreateNewArmyInCamp(Camp camp)
	{
		IGameService service = Services.GetService<IGameService>();
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		return service2 != null && DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(camp.WorldPosition, PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.Water);
	}

	public static WorldPosition GetNeighbourFirstAvailablePositionForArmyCreation(Army army)
	{
		IGameService service = Services.GetService<IGameService>();
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		IPathfindingService service3 = service.Game.Services.GetService<IPathfindingService>();
		if (service2 != null)
		{
			for (int i = 0; i < 6; i++)
			{
				WorldPosition neighbourTile = service2.GetNeighbourTile(army.WorldPosition, (WorldOrientation)i, 1);
				if (DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(neighbourTile, PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.Water) && service3.IsTransitionPassable(army.WorldPosition, neighbourTile, army, PathfindingFlags.IgnoreFogOfWar, null) && service3.IsTileStopable(neighbourTile, army, PathfindingFlags.IgnoreFogOfWar, null))
				{
					return neighbourTile;
				}
			}
		}
		return WorldPosition.Invalid;
	}

	public static WorldPosition GetFirstAvailablePositionForArmyCreation(WorldPosition position)
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null && service.Game != null);
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(service2 != null);
		List<WorldPosition> list = new List<WorldPosition>();
		Queue<WorldPosition> queue = new Queue<WorldPosition>();
		queue.Enqueue(position);
		WorldPosition worldPosition = WorldPosition.Invalid;
		while (queue.Count > 0 && worldPosition == WorldPosition.Invalid)
		{
			worldPosition = queue.Dequeue();
			if (!DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(worldPosition, PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.Water))
			{
				for (int i = 0; i < 6; i++)
				{
					WorldPosition neighbourTile = service2.GetNeighbourTile(position, (WorldOrientation)i, 1);
					if (!list.Contains(neighbourTile))
					{
						list.Add(neighbourTile);
						queue.Enqueue(neighbourTile);
					}
				}
				worldPosition = WorldPosition.Invalid;
			}
		}
		return worldPosition;
	}

	public static List<WorldPosition> GetAvailablePositionsForArmyCreation(WorldPosition position)
	{
		List<WorldPosition> list = new List<WorldPosition>();
		IGameService service = Services.GetService<IGameService>();
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		if (service2 != null)
		{
			for (int i = 0; i < 6; i++)
			{
				WorldPosition neighbourTile = service2.GetNeighbourTile(position, (WorldOrientation)i, 1);
				if (DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(neighbourTile, PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.Water))
				{
					list.Add(neighbourTile);
				}
			}
		}
		return list;
	}

	public static List<WorldPosition> GetAvailablePositionsForUnitsTranslation(WorldPosition from, int radius, global::Empire empire, GameEntityGUID[] unitsGUIDs, bool checkIfTileIsStopable = true, PathfindingFlags stopableFlags = PathfindingFlags.IgnoreFogOfWar, bool checkIfTileIsPassable = true, PathfindingFlags passableFlags = PathfindingFlags.IgnoreFogOfWar)
	{
		List<WorldPosition> list = new List<WorldPosition>();
		IGameService service = Services.GetService<IGameService>();
		IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
		Diagnostics.Assert(service2 != null);
		IPathfindingService pathfindingService = service.Game.Services.GetService<IPathfindingService>();
		Diagnostics.Assert(pathfindingService != null);
		IWorldPositionningService worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(worldPositionningService != null);
		if (radius <= 0)
		{
			radius = 1;
		}
		WorldCircle worldCircle = new WorldCircle(from, radius);
		WorldPosition[] worldPositions = worldCircle.GetWorldPositions(worldPositionningService.World.WorldParameters);
		for (int i = 0; i < worldPositions.Length; i++)
		{
			bool flag = true;
			WorldPosition worldPosition = worldPositions[i];
			if (DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(worldPosition, PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.Water))
			{
				for (int j = 0; j < unitsGUIDs.Length; j++)
				{
					Unit pathfindingContextProvider = null;
					if (service2.TryGetValue<Unit>(unitsGUIDs[j], out pathfindingContextProvider) && ((checkIfTileIsStopable && !pathfindingService.IsTileStopable(worldPosition, pathfindingContextProvider, stopableFlags, null)) || (checkIfTileIsPassable && !pathfindingService.IsTransitionPassable(from, worldPosition, pathfindingContextProvider, passableFlags, null))))
					{
						flag = false;
					}
				}
				if (flag)
				{
					list.Add(worldPosition);
				}
			}
		}
		if (unitsGUIDs.Length > 0)
		{
			Unit firstUnit = null;
			if (service2.TryGetValue<Unit>(unitsGUIDs[0], out firstUnit))
			{
				list = (from position in list
				orderby pathfindingService.GetTransitionCost(@from, position, firstUnit, PathfindingFlags.IgnoreFogOfWar, null)
				select position).ToList<WorldPosition>();
			}
		}
		DepartmentOfForeignAffairs departmentOfForeignAffairs = empire.GetAgency<DepartmentOfForeignAffairs>();
		if (departmentOfForeignAffairs != null)
		{
			list = (from pos in list
			where departmentOfForeignAffairs.CanMoveOn(pos, false, false)
			select pos into position
			orderby worldPositionningService.GetDistance(position, @from)
			select position).ToList<WorldPosition>();
		}
		if (list.Count == 0)
		{
			list.Add(WorldPosition.Invalid);
		}
		return list;
	}

	public static List<WorldPosition> GetAvailablePositionsForSeafaringArmyCreation(City city)
	{
		List<WorldPosition> list = new List<WorldPosition>();
		if (city.DryDockPosition.IsValid)
		{
			list.Add(city.DryDockPosition);
		}
		IGameService service = Services.GetService<IGameService>();
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		if (service2 != null)
		{
			for (int i = 0; i < city.Districts.Count; i++)
			{
				if (service2.IsOceanTile(city.Districts[i].WorldPosition))
				{
					bool flag = service2.IsFrozenWaterTile(city.Districts[i].WorldPosition);
					if (!flag)
					{
						if (!(city.Districts[i].WorldPosition == city.DryDockPosition))
						{
							list.Add(city.Districts[i].WorldPosition);
						}
					}
				}
			}
		}
		list.RemoveAll((WorldPosition worldPosition) => !DepartmentOfDefense.CheckWhetherTargetPositionIsValidAsSeafaringSpawnLocation(worldPosition, city.Empire.Index, 1));
		return list;
	}

	public static void RemoveUnitDesignFromUnit(Unit unit)
	{
		Diagnostics.Assert(unit != null);
		if (unit.UnitDesign == null)
		{
			return;
		}
		if (DepartmentOfDefense.staticSimulationDescriptorDatabase == null)
		{
			DepartmentOfDefense.staticSimulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
		}
		if (unit.UnitDesign.UnitBodyDefinition.UnitAbilities != null)
		{
			unit.RemoveUnitAbilities(unit.UnitDesign.UnitBodyDefinition.UnitAbilities);
		}
		if (unit.UnitDesign.SimulationDescriptorReferences != null)
		{
			for (int i = 0; i < unit.UnitDesign.SimulationDescriptorReferences.Length; i++)
			{
				StaticString name = unit.UnitDesign.SimulationDescriptorReferences[i].Name;
				if (StaticString.IsNullOrEmpty(name))
				{
					Diagnostics.LogWarning("Simulation descriptor name is either null or empty.");
				}
				else
				{
					SimulationDescriptor descriptor = null;
					if (!DepartmentOfDefense.staticSimulationDescriptorDatabase.TryGetValue(name, out descriptor))
					{
						Diagnostics.LogWarning("Cannot find simulation descriptor ('{0}') in database.", new object[]
						{
							name
						});
					}
					else
					{
						unit.RemoveDescriptor(descriptor);
					}
				}
			}
		}
		Diagnostics.Assert(unit.UnitDesign.UnitBodyDefinition != null);
		SimulationDescriptor[] descriptors = unit.UnitDesign.UnitBodyDefinition.Descriptors;
		if (descriptors != null)
		{
			for (int j = 0; j < descriptors.Length; j++)
			{
				unit.RemoveDescriptor(descriptors[j]);
			}
		}
		DepartmentOfDefense.RemoveEquipmentSet(unit);
		unit.SimulationObject.Refresh();
		if (unit.UnitDesign is UnitProfile)
		{
			UnitProfile unitProfile = unit.UnitDesign as UnitProfile;
			unit.RemoveUnitAbilities(unitProfile.ProfileAbilityReferences);
		}
		unit.UnitDesign = null;
		unit.Refresh(false);
		unit.SetPropertyBaseValue(SimulationProperties.Health, unit.GetPropertyValue(SimulationProperties.MaximumHealth));
		unit.SetPropertyBaseValue(SimulationProperties.MovementRatio, 1f);
		unit.Refresh(false);
	}

	public static void ApplyUnitDesignToUnit(Unit unit, UnitDesign unitDesign)
	{
		Diagnostics.Assert(unit != null);
		if (unitDesign == null)
		{
			Diagnostics.LogError("Unit design is null.");
			return;
		}
		if (unit.UnitDesign != null)
		{
			Diagnostics.LogError("Unit already has a unit design.");
			return;
		}
		unit.UnitDesign = unitDesign;
		if (DepartmentOfDefense.staticSimulationDescriptorDatabase == null)
		{
			DepartmentOfDefense.staticSimulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
		}
		if (unitDesign.UnitBodyDefinition.UnitAbilities != null)
		{
			unit.AddUnitAbilities(unitDesign.UnitBodyDefinition.UnitAbilities);
		}
		if (unitDesign.SimulationDescriptorReferences != null)
		{
			for (int i = 0; i < unitDesign.SimulationDescriptorReferences.Length; i++)
			{
				StaticString name = unitDesign.SimulationDescriptorReferences[i].Name;
				if (StaticString.IsNullOrEmpty(name))
				{
					Diagnostics.LogWarning("Simulation descriptor name is either null or empty.");
				}
				else
				{
					SimulationDescriptor descriptor = null;
					if (!DepartmentOfDefense.staticSimulationDescriptorDatabase.TryGetValue(name, out descriptor))
					{
						Diagnostics.LogWarning("Cannot find simulation descriptor ('{0}') in database.", new object[]
						{
							name
						});
					}
					else
					{
						unit.AddDescriptor(descriptor, false);
					}
				}
			}
		}
		Diagnostics.Assert(unitDesign.UnitBodyDefinition != null);
		SimulationDescriptor[] descriptors = unitDesign.UnitBodyDefinition.Descriptors;
		if (descriptors != null)
		{
			for (int j = 0; j < descriptors.Length; j++)
			{
				unit.AddDescriptor(descriptors[j], false);
			}
		}
		if (unitDesign.UnlockedUnitRankReferences != null)
		{
			unit.ForceRank(Array.ConvertAll<XmlNamedReference, StaticString>(unitDesign.UnlockedUnitRankReferences, (XmlNamedReference match) => match.Name));
		}
		StaticString staticString = unitDesign.UnitRankReference;
		if (StaticString.IsNullOrEmpty(staticString))
		{
			staticString = unitDesign.UnitBodyDefinition.UnitRankReference;
		}
		unit.ForceRank(staticString, 0);
		DepartmentOfDefense.ApplyEquipmentSet(unit);
		unit.SimulationObject.Refresh();
		if (unitDesign is UnitProfile)
		{
			UnitProfile unitProfile = unitDesign as UnitProfile;
			unit.AddUnitAbilities(unitProfile.ProfileAbilityReferences);
		}
		unit.Refresh(false);
		unit.SetPropertyBaseValue(SimulationProperties.Health, unit.GetPropertyValue(SimulationProperties.MaximumHealth));
		unit.SetPropertyBaseValue(SimulationProperties.MovementRatio, 1f);
		unit.Refresh(false);
	}

	public static void ReplaceUnitDesign(Unit unit, UnitDesign unitDesign)
	{
		if (unit == null)
		{
			Diagnostics.LogError("Unit design is null.");
			return;
		}
		if (unitDesign == null)
		{
			Diagnostics.LogError("Unit design is null.");
			return;
		}
		Diagnostics.Assert(unit.UnitDesign != unitDesign);
		DepartmentOfDefense.RemoveUnitDesignFromUnit(unit);
		DepartmentOfDefense.ApplyUnitDesignToUnit(unit, unitDesign);
	}

	public static Unit CreateUnitByDesign(GameEntityGUID guid, UnitDesign unitDesign)
	{
		Unit unit = DepartmentOfDefense.CreateUnit(guid);
		DepartmentOfDefense.ApplyUnitDesignToUnit(unit, unitDesign);
		return unit;
	}

	public static void ForceUnitEquipmentAccessoriesSlots(UnitDesign unitDesign)
	{
		if (unitDesign.Context == null)
		{
			return;
		}
		float propertyValue = unitDesign.Context.GetPropertyValue(SimulationProperties.AccessoriesSlotCount);
		int num = 0;
		for (int i = 0; i < unitDesign.UnitEquipmentSet.Slots.Length; i++)
		{
			if (unitDesign.UnitEquipmentSet.Slots[i].SlotType == UnitEquipmentSet.AccessoryType)
			{
				if ((float)num >= propertyValue)
				{
					unitDesign.UnitEquipmentSet.Slots[i].ItemName = StaticString.Empty;
				}
				else
				{
					num++;
				}
			}
		}
	}

	public static UnitDesign GetClosestUnitDesign(IUnitDesignDatabase unitDesignDatabase, uint unitModel)
	{
		UnitDesign unitDesign;
		if (!unitDesignDatabase.TryGetValue(unitModel, out unitDesign, false))
		{
			Diagnostics.Assert(unitDesign != null, "Can't retrieve a unit design with model: {0}.", new object[]
			{
				unitModel
			});
		}
		return unitDesign;
	}

	public static void HealUnits(IEnumerable<Unit> units)
	{
		foreach (Unit unit in units)
		{
			float propertyValue = unit.GetPropertyValue(SimulationProperties.MaximumHealth);
			unit.SetPropertyBaseValue(SimulationProperties.Health, propertyValue);
		}
		foreach (Unit unit2 in units)
		{
			SimulationObjectWrapper simulationObjectWrapper = unit2.Garrison as SimulationObjectWrapper;
			if (simulationObjectWrapper != null)
			{
				simulationObjectWrapper.Refresh(false);
			}
		}
	}

	public static void HealUnits(IEnumerable<Unit> units, float amount)
	{
		foreach (Unit unit in units)
		{
			float propertyValue = unit.GetPropertyValue(SimulationProperties.MaximumHealth);
			float num = unit.GetPropertyValue(SimulationProperties.Health);
			num = Math.Max(1f, Math.Min(propertyValue, num + amount));
			unit.SetPropertyBaseValue(SimulationProperties.Health, num);
		}
		foreach (Unit unit2 in units)
		{
			SimulationObjectWrapper simulationObjectWrapper = unit2.Garrison as SimulationObjectWrapper;
			if (simulationObjectWrapper != null)
			{
				simulationObjectWrapper.Refresh(false);
			}
		}
	}

	public static UnitDesign ReadUnitDesignFromAttributes(XmlReader reader, IUnitDesignDatabase unitDesignDatabase)
	{
		if (reader.IsNullElement())
		{
			reader.Skip();
			return null;
		}
		string attribute = reader.GetAttribute("UnitDesignName");
		uint unitDesignModel = reader.GetAttribute<uint>("UnitDesignModel");
		uint unitDesignRevision = reader.GetAttribute<uint>("UnitDesignRevision");
		UnitDesign unitDesign = unitDesignDatabase.GetRegisteredUnitDesignsAsEnumerable().FirstOrDefault((UnitDesign iterator) => iterator.Model == unitDesignModel && iterator.ModelRevision == unitDesignRevision);
		Diagnostics.Assert(unitDesign != null, "Can't retrieve the unit design '{0}' (model: {1}, revision: {2}).", new object[]
		{
			attribute,
			unitDesignModel,
			unitDesignRevision
		});
		return unitDesign;
	}

	public static void WriteUnitDesignAttributes(XmlWriter writer, UnitDesign unitDesign)
	{
		writer.WriteAttributeString<StaticString>("UnitDesignName", unitDesign.Name);
		writer.WriteAttributeString<uint>("UnitDesignModel", unitDesign.Model);
		writer.WriteAttributeString<uint>("UnitDesignRevision", unitDesign.ModelRevision);
	}

	public static Unit ReadUnit(XmlReader reader, IUnitDesignDatabase unitDesignDatabase)
	{
		if (reader.IsNullElement())
		{
			reader.Skip();
			return null;
		}
		ulong attribute = reader.GetAttribute<ulong>("GUID");
		Unit unit = new Unit(attribute);
		if (unit != null)
		{
			unit.UnitDesign = DepartmentOfDefense.ReadUnitDesignFromAttributes(reader, unitDesignDatabase);
			if (unit.UnitDesign == null)
			{
				reader.Skip();
				return null;
			}
		}
		reader.ReadElementSerializable<Unit>(ref unit);
		if (unit.UnitDesign is UnitProfile)
		{
			StaticString descriptorNameFromType = unit.GetDescriptorNameFromType("UnitProfile");
			if (unit.UnitDesign.Name != descriptorNameFromType)
			{
				return null;
			}
		}
		unit.VerifyUnitAbilities();
		return unit;
	}

	public static void RegenUnit(Unit unit, float regenModifier, int pacifiedVillage)
	{
		float num = 0f;
		if (!DepartmentOfDefense.IsUnitOnOwnedDistrict(unit) && unit.Garrison is IGarrisonWithPosition && !unit.Garrison.Empire.SimulationObject.Tags.Contains("FactionTraitFlames3") && unit.GetPropertyValue(SimulationProperties.VolcanicRegen) == 0f && !(unit.Garrison is KaijuGarrison))
		{
			WorldPosition worldPosition = (unit.Garrison as IGarrisonWithPosition).WorldPosition;
			if (DepartmentOfDefense.worldPositionningServiceStatic.ContainsTerrainTag(worldPosition, "TerrainTagVolcanic"))
			{
				return;
			}
		}
		if (!unit.CheckUnitAbility(UnitAbility.ReadonlyCannotRegen, -1))
		{
			num = unit.GetPropertyValue(SimulationProperties.UnitRegen);
			num += regenModifier;
		}
		if (unit.CheckUnitAbility(UnitAbility.ReadonlyCanRegenWithVillage, -1))
		{
			float num2 = unit.GetPropertyValue(SimulationProperties.BrokenLordsUnitRegenPerPacifiedVillage);
			num2 *= (float)pacifiedVillage;
			num += num2;
		}
		float propertyValue = unit.GetPropertyValue(SimulationProperties.UnitRegenModifier);
		if (unit.CheckUnitAbility(UnitAbility.ReadonlyNodeRegeneration, -1))
		{
			float propertyValue2 = unit.GetPropertyValue(SimulationProperties.BrokenLordsUnitRegenPerPacifiedVillage);
			num += propertyValue2;
		}
		num *= propertyValue;
		num += unit.GetPropertyValue(SimulationProperties.UnitPoison);
		if (num == 0f)
		{
			return;
		}
		float propertyValue3 = unit.GetPropertyValue(SimulationProperties.MaximumHealth);
		float num3 = unit.GetPropertyValue(SimulationProperties.Health);
		float num4 = propertyValue3 * num;
		num3 = Mathf.Min(propertyValue3, num3 + num4);
		unit.SetPropertyBaseValue(SimulationProperties.Health, num3);
	}

	public Unit CreateDummyUnitInTemporaryGarrison()
	{
		ulong x;
		this.tempGuid = (x = this.tempGuid) + 1UL;
		Unit unit = DepartmentOfDefense.CreateUnit(x);
		unit.SimulationObject.ModifierForward = ModifierForwardType.ChildrenOnly;
		this.temporaryGarrison.AddChild_ModifierForwardType_ChildrenOnly(unit);
		return unit;
	}

	public void DisposeDummyUnit(Unit dummyUnit)
	{
		this.temporaryGarrison.RemoveChild_ModifierForwardType_ChildrenOnly(dummyUnit);
		dummyUnit.Dispose();
	}

	public bool IsEquipmentItemPrerequisitesValid(Unit unit, ItemDefinition item)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unitDesign");
		}
		return DepartmentOfTheTreasury.CheckConstructiblePrerequisites(unit, item, new string[]
		{
			"Discard"
		});
	}

	public bool IsUnitEquipmentItemPrerequisitesValid(UnitDesign unitDesign, Unit referenceUnit = null)
	{
		Unit unit = DepartmentOfDefense.CreateUnitByDesign(1UL, unitDesign);
		if (this.temporaryGarrison == null)
		{
			this.temporaryGarrison = new SimulationObject("DepartmentOfDefense.TemporaryGarrison");
			this.temporaryGarrison.Tags.AddTag("ClassArmy");
			this.temporaryGarrison.Tags.AddTag("ClassCity");
			this.temporaryGarrison.Tags.AddTag("Garrison");
			this.temporaryGarrison.ModifierForward = ModifierForwardType.ChildrenOnly;
			base.Empire.SimulationObject.AddChild(this.temporaryGarrison);
		}
		this.temporaryGarrison.AddChild(unit);
		DepartmentOfDefense.RemoveEquipmentSet(unit);
		ItemDefinition[] array = new ItemDefinition[unitDesign.UnitEquipmentSet.Slots.Length];
		for (int i = 0; i < unitDesign.UnitEquipmentSet.Slots.Length; i++)
		{
			StaticString key = unitDesign.UnitEquipmentSet.Slots[i].ItemName.ToString().Split(DepartmentOfDefense.ItemSeparators)[0];
			ItemDefinition itemDefinition;
			if (this.ItemDefinitionDatabase.TryGetValue(key, out itemDefinition))
			{
				array[i] = itemDefinition;
			}
		}
		unit.Refresh(false);
		int num = 0;
		float propertyValue = unit.GetPropertyValue(SimulationProperties.AccessoriesSlotCount);
		if (referenceUnit != null)
		{
			propertyValue = referenceUnit.GetPropertyValue(SimulationProperties.AccessoriesSlotCount);
		}
		List<StaticString> list = new List<StaticString>();
		for (int j = 0; j < array.Length; j++)
		{
			if (array[j] != null)
			{
				list.Clear();
				if (!DepartmentOfTheTreasury.CheckConstructiblePrerequisites(unit, array[j], ref list, new string[]
				{
					"Prerequisite"
				}))
				{
					bool flag = false;
					if (list.Contains(ConstructionFlags.Technology) && referenceUnit != null && referenceUnit.UnitDesign != null && referenceUnit.UnitDesign.UnitEquipmentSet != null && referenceUnit.UnitDesign.UnitEquipmentSet.Slots != null && j < referenceUnit.UnitDesign.UnitEquipmentSet.Slots.Length)
					{
						string text = referenceUnit.UnitDesign.UnitEquipmentSet.Slots[j].ItemName;
						int num2 = text.IndexOf('#');
						if (num2 != -1)
						{
							text = text.Substring(0, num2);
						}
						if (text == array[j].Name)
						{
							flag = true;
						}
					}
					if (!flag)
					{
						goto IL_2D3;
					}
				}
				if (unitDesign.UnitEquipmentSet.Slots[j].SlotType == UnitEquipmentSet.AccessoryType)
				{
					if ((float)num >= propertyValue)
					{
						goto IL_2D3;
					}
					num++;
				}
				DepartmentOfDefense.AddItemEquipment(unit, unitDesign.UnitEquipmentSet.Slots[j], array[j], unitDesign.UnitEquipmentSet);
				array[j] = null;
				j = 0;
				unit.Refresh(false);
			}
			IL_2D3:;
		}
		this.temporaryGarrison.RemoveChild(unit);
		unit.Dispose();
		for (int k = 0; k < array.Length; k++)
		{
			if (array[k] != null)
			{
				return false;
			}
		}
		return true;
	}

	public bool CanBeHealedAtWorldPosition(WorldPosition worldPosition)
	{
		Region region = this.WorldPositionningService.GetRegion(worldPosition);
		bool flag = region.BelongToEmpire(base.Empire as global::Empire);
		City city = region.City;
		bool flag2 = false;
		StaticString x = StaticString.Empty;
		if (flag)
		{
			x = SimulationProperties.InOwnedRegionUnitRegenModifier;
		}
		else if (flag2)
		{
			x = SimulationProperties.InAlliedRegionUnitRegenModifier;
		}
		else
		{
			x = SimulationProperties.InNoneAlliedRegionUnitRegenModifier;
		}
		if (StaticString.IsNullOrEmpty(x))
		{
			return false;
		}
		if (flag2 || flag)
		{
			if (region.City != null)
			{
				float propertyValue;
				if (flag)
				{
					propertyValue = region.City.GetPropertyValue(SimulationProperties.OwnedUnitRegenModifier);
				}
				else
				{
					propertyValue = region.City.GetPropertyValue(SimulationProperties.AlliedUnitRegenModifier);
				}
				if (propertyValue > 0f)
				{
					int distanceLimit = (int)region.City.GetPropertyValue(SimulationProperties.UnitRegenLimitDistance);
					if (region.City.Districts.Any((District match) => this.WorldPositionningService.GetDistance(match.WorldPosition, worldPosition) <= distanceLimit))
					{
						return true;
					}
				}
			}
			float num = 0f;
			for (int i = 0; i < region.PointOfInterests.Length; i++)
			{
				if (num > 0f)
				{
					int num2 = (int)region.PointOfInterests[i].GetPropertyValue(SimulationProperties.UnitRegenLimitDistance);
					if (this.WorldPositionningService.GetDistance(region.PointOfInterests[i].WorldPosition, worldPosition) <= num2)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private static Unit CreateUnit(GameEntityGUID guid)
	{
		Unit unit = new Unit(guid);
		if (DepartmentOfDefense.staticSimulationDescriptorDatabase == null)
		{
			DepartmentOfDefense.staticSimulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
		}
		SimulationDescriptor descriptor = null;
		Diagnostics.Assert(DepartmentOfDefense.staticSimulationDescriptorDatabase != null);
		if (DepartmentOfDefense.staticSimulationDescriptorDatabase.TryGetValue("ClassUnit", out descriptor))
		{
			unit.AddDescriptor(descriptor, false);
		}
		else
		{
			Diagnostics.LogError("Unable to retrieve the 'ClassUnit' simulation descriptor from the database.");
		}
		return unit;
	}

	private static void AddItemEquipment(Unit unit, UnitEquipmentSet.Slot slot, ItemDefinition itemDefinition, UnitEquipmentSet equipmentSet)
	{
		SimulationObject simulationObject = unit.SimulationObject.Children.FirstOrDefault((SimulationObject iterator) => iterator.Name == slot.ItemName);
		if (simulationObject != null)
		{
			simulationObject.Tags.AddTag(slot.SlotType);
			return;
		}
		simulationObject = new SimulationObject(slot.ItemName);
		simulationObject.Tags.AddTag(slot.SlotType);
		unit.SimulationObject.AddChild(simulationObject);
		if (DepartmentOfDefense.staticSimulationDescriptorDatabase == null)
		{
			DepartmentOfDefense.staticSimulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
		}
		SimulationDescriptor descriptor = null;
		Diagnostics.Assert(DepartmentOfDefense.staticSimulationDescriptorDatabase != null);
		if (DepartmentOfDefense.staticSimulationDescriptorDatabase.TryGetValue("ClassItem", out descriptor))
		{
			simulationObject.AddDescriptor(descriptor);
		}
		SimulationDescriptor descriptor2 = null;
		if (DepartmentOfDefense.staticSimulationDescriptorDatabase.TryGetValue(itemDefinition.Name, out descriptor2))
		{
			simulationObject.AddDescriptor(descriptor2);
		}
		if (itemDefinition.AbilityReferences != null)
		{
			unit.AddUnitAbilities(itemDefinition.AbilityReferences);
		}
		if (itemDefinition.SimulationDescriptorReferences != null)
		{
			for (int i = 0; i < itemDefinition.SimulationDescriptorReferences.Length; i++)
			{
				StaticString name = itemDefinition.SimulationDescriptorReferences[i].Name;
				if (StaticString.IsNullOrEmpty(name))
				{
					Diagnostics.LogWarning("Simulation descriptor name is either null or empty.");
				}
				else if (!DepartmentOfDefense.staticSimulationDescriptorDatabase.TryGetValue(name, out descriptor2))
				{
					Diagnostics.LogWarning("Cannot find simulation descriptor ('{0}') in database.", new object[]
					{
						name
					});
				}
				else
				{
					simulationObject.AddDescriptor(descriptor2);
				}
			}
		}
		if (itemDefinition.Slots != null)
		{
			for (int j = 0; j < itemDefinition.Slots.Length; j++)
			{
				int num = 0;
				int num2 = 0;
				for (int k = 0; k < equipmentSet.Slots.Length; k++)
				{
					StaticString x = equipmentSet.Slots[k].ItemName.ToString().Split(DepartmentOfDefense.ItemSeparators)[0];
					if (x == itemDefinition.Name)
					{
						if (!itemDefinition.Slots[j].SlotTypes.Contains(slot.SlotType))
						{
							num2++;
							break;
						}
						num++;
					}
				}
				if (num2 == 0 && num == itemDefinition.Slots[j].SlotTypes.Length && itemDefinition.Slots[j].SimulationDescriptorReferences != null)
				{
					for (int l = 0; l < itemDefinition.Slots[j].SimulationDescriptorReferences.Length; l++)
					{
						StaticString name2 = itemDefinition.Slots[j].SimulationDescriptorReferences[l].Name;
						if (StaticString.IsNullOrEmpty(name2))
						{
							Diagnostics.LogWarning("Simulation descriptor name is either null or empty.");
						}
						else if (!DepartmentOfDefense.staticSimulationDescriptorDatabase.TryGetValue(name2, out descriptor2))
						{
							Diagnostics.LogWarning("Cannot find simulation descriptor ('{0}') in database.", new object[]
							{
								name2
							});
						}
						else
						{
							simulationObject.AddDescriptor(descriptor2);
						}
					}
				}
			}
		}
		simulationObject.Refresh();
	}

	private void GetTerrainDamage(WorldPosition worldPosition, out float totalDamageAbsolute, out float totalDamagePercentMax, out float totalDamagePercentCurrent)
	{
		this.WorldPositionningService.GetTerrainDamage(worldPosition, out totalDamageAbsolute, out totalDamagePercentMax, out totalDamagePercentCurrent);
	}

	private void GetRiverDamage(WorldPosition worldPosition, out float totalDamageAbsolute, out float totalDamagePercentMax, out float totalDamagePercentCurrent)
	{
		this.WorldPositionningService.GetRiverDamage(worldPosition, out totalDamageAbsolute, out totalDamagePercentMax, out totalDamagePercentCurrent);
	}

	public bool IsTileHarmfulForArmy(Army army, WorldPosition worldPosition)
	{
		if (army.UnitsCount <= 0)
		{
			return false;
		}
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		this.GetTerrainDamage(worldPosition, out num, out num2, out num3);
		this.GetRiverDamage(worldPosition, out num, out num2, out num3);
		if (num <= 0f && num2 <= 0f && num3 <= 0f)
		{
			return false;
		}
		float num4 = num;
		foreach (Unit unit in army.Units)
		{
			num4 += unit.GetPropertyValue(SimulationProperties.MaximumHealth) * num2;
			num4 += unit.GetPropertyValue(SimulationProperties.Health) * num3;
			num4 *= unit.GetPropertyValue(SimulationProperties.ReceivedTerrainDamageMultiplier);
		}
		return num4 > 0f;
	}

	public bool CheckTerrainDamageForUnits(Army army)
	{
		bool flag = false;
		foreach (Unit unit in army.Units)
		{
			bool flag2 = this.CheckTerrainDamageForUnit(unit);
			if (flag2 && !flag)
			{
				flag = true;
			}
		}
		return flag;
	}

	private bool CheckTerrainDamageForUnit(Unit unit)
	{
		if (unit == null)
		{
			return false;
		}
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		WorldPosition worldPosition = WorldPosition.Invalid;
		IWorldPositionable worldPositionable = unit.Garrison as IWorldPositionable;
		if (worldPositionable != null)
		{
			worldPosition = worldPositionable.WorldPosition;
		}
		if (worldPosition == WorldPosition.Invalid)
		{
			return false;
		}
		this.GetTerrainDamage(worldPosition, out num, out num2, out num3);
		this.GetRiverDamage(worldPosition, out num, out num2, out num3);
		if (num <= 0f && num2 <= 0f && num3 <= 0f)
		{
			return false;
		}
		float num4 = num;
		num4 += unit.GetPropertyValue(SimulationProperties.MaximumHealth) * num2;
		num4 += unit.GetPropertyValue(SimulationProperties.Health) * num3;
		num4 *= unit.GetPropertyValue(SimulationProperties.ReceivedTerrainDamageMultiplier);
		if (num4 > 0f)
		{
			this.WoundUnit(unit, num4);
			unit.OnWorldTerrainDamageReceived();
			return true;
		}
		return false;
	}

	private void CheckAndSlapArmiesOverCityExploitation(City city, bool isImmuneToDefensiveImprovements)
	{
		bool flag = base.Empire is MajorEmpire;
		Army[] array = this.Armies.ToArray<Army>();
		for (int i = 0; i < array.Length; i++)
		{
			if (!isImmuneToDefensiveImprovements || array[i].IsPrivateers)
			{
				District district = this.WorldPositionningService.GetDistrict(array[i].WorldPosition);
				if (district != null && district.City == city && district.Type == DistrictType.Exploitation)
				{
					float propertyValue = city.GetPropertyValue(SimulationProperties.DefensivePower);
					float propertyValue2 = city.GetPropertyValue(SimulationProperties.CoastalDefensivePower);
					int unitsCount = array[i].UnitsCount;
					float num = 0f;
					foreach (Unit unit in array[i].Units)
					{
						float num2 = propertyValue / (float)array[i].UnitsCount * unit.GetPropertyValue(SimulationProperties.DefensivePowerDamageReceived);
						if (array[i].IsNaval)
						{
							num2 += propertyValue2 / (float)array[i].UnitsCount * unit.GetPropertyValue(SimulationProperties.DefensivePowerDamageReceived);
						}
						this.WoundUnit(unit, num2);
						num += num2;
					}
					if (flag && num > 0f)
					{
						ArmyHitInfo armyInfo = new ArmyHitInfo(array[i], unitsCount, city.WorldPosition, ArmyHitInfo.HitType.Retaliate);
						this.EventService.Notify(new EventArmyHit(base.Empire, armyInfo, false));
					}
				}
			}
		}
	}

	private void CheckAndSlapArmiesInRange(PointOfInterest pointOfInterest, bool isImmuneToDefensiveImprovements)
	{
		float propertyValue = pointOfInterest.SimulationObject.GetPropertyValue(SimulationProperties.DefensivePower);
		this.CheckAndSlapArmiesInRange(pointOfInterest.Empire, pointOfInterest.WorldPosition, propertyValue, pointOfInterest.LineOfSightVisionRange, isImmuneToDefensiveImprovements);
	}

	private void CheckAndSlapArmiesInRange(CreepingNode creepingNode, bool isImmuneToDefensiveImprovements)
	{
		float propertyValue = creepingNode.SimulationObject.GetPropertyValue(SimulationProperties.CreepingNodeRetaliationDamage);
		this.CheckAndSlapArmiesInRange(creepingNode.Empire, creepingNode.WorldPosition, propertyValue, creepingNode.LineOfSightVisionRange, isImmuneToDefensiveImprovements);
	}

	private void CheckAndSlapArmiesInRange(global::Empire empire, WorldPosition worldPosition, float retaliationDamage, int lineOfSightVisionRange, bool isImmuneToDefensiveImprovements)
	{
		IVisibilityService service = this.GameService.Game.Services.GetService<IVisibilityService>();
		Diagnostics.Assert(service != null);
		IWorldPositionningService service2 = this.GameService.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(service2 != null);
		bool flag = base.Empire is MajorEmpire;
		bool flag2 = false;
		if (flag)
		{
			DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(empire.Index);
			if (diplomaticRelation != null && diplomaticRelation.State != null && (diplomaticRelation.State.Name == DiplomaticRelationState.Names.ColdWar || diplomaticRelation.State.Name == DiplomaticRelationState.Names.Truce))
			{
				flag2 = true;
			}
		}
		global::Empire empire2 = base.Empire as global::Empire;
		Army[] array = this.Armies.ToArray<Army>();
		for (int i = 0; i < array.Length; i++)
		{
			if (!isImmuneToDefensiveImprovements || array[i].IsPrivateers)
			{
				if (flag2 && !array[i].IsPrivateers)
				{
					Region region = service2.GetRegion(array[i].WorldPosition);
					if (region != null && region.BelongToEmpire(base.Empire.Index))
					{
						goto IL_27D;
					}
				}
				if (service.IsWorldPositionVisibleFor(array[i].WorldPosition, empire))
				{
					bool flag3 = array[i].GetPropertyValue(SimulationProperties.LevelOfStealth) > 0f || array[i].GetPropertyValue(SimulationProperties.LevelOfCamouflage) > 0f;
					if (!flag3 || service.IsWorldPositionDetectedFor(array[i].WorldPosition, empire))
					{
						if (service2.GetDistance(array[i].WorldPosition, worldPosition) <= lineOfSightVisionRange)
						{
							int unitsCount = array[i].UnitsCount;
							foreach (Unit unit in array[i].Units)
							{
								float damage = retaliationDamage / (float)array[i].UnitsCount * unit.GetPropertyValue(SimulationProperties.DefensivePowerDamageReceived);
								this.WoundUnit(unit, damage);
							}
							if (flag)
							{
								service.SetWorldPositionAsExplored(worldPosition, empire2, 0);
								service.NotifyVisibilityHasChanged(empire2);
								ArmyHitInfo armyInfo = new ArmyHitInfo(array[i], unitsCount, worldPosition, ArmyHitInfo.HitType.Retaliate);
								this.EventService.Notify(new EventArmyHit(base.Empire, armyInfo, false));
							}
						}
					}
				}
			}
			IL_27D:;
		}
	}

	private void CollectOrbs_OnWorldPositionChange(object sender, ArmyWorldPositionChangeEventArgs e)
	{
		Army army = sender as Army;
		if (army == null)
		{
			return;
		}
		IOrbService service = this.GameService.Game.Services.GetService<IOrbService>();
		if (service != null)
		{
			service.CollectOrbsAtPosition(army.WorldPosition, army, army.Empire);
		}
	}

	private void CheckDismantlingDevice_OnWorldPositionChange(object sender, ArmyWorldPositionChangeEventArgs e)
	{
		Army army = sender as Army;
		if (army == null)
		{
			return;
		}
		if (army.IsDismantlingDevice)
		{
			ITerraformDeviceService service = this.GameService.Game.Services.GetService<ITerraformDeviceService>();
			TerraformDeviceManager terraformDeviceManager = service as TerraformDeviceManager;
			TerraformDevice terraformDevice = (TerraformDevice)terraformDeviceManager[army.DismantlingDeviceTarget];
			if (!terraformDeviceManager.CanExecuteDeviceDismantling(army, terraformDevice))
			{
				IPlayerControllerRepositoryControl playerControllerRepositoryControl = (IPlayerControllerRepositoryControl)this.PlayerControllerRepositoryService;
				global::PlayerController playerControllerById = playerControllerRepositoryControl.GetPlayerControllerById("server");
				if (playerControllerById != null)
				{
					OrderToggleDismantleDevice order = new OrderToggleDismantleDevice(army.Empire.Index, army.GUID, terraformDevice.GUID, false);
					playerControllerById.PostOrder(order);
				}
			}
		}
	}

	private void CheckDismantlingCreepingNode_OnWorldPositionChange(object sender, ArmyWorldPositionChangeEventArgs e)
	{
		Army army = sender as Army;
		if (army == null)
		{
			return;
		}
		if (army.IsDismantlingCreepingNode)
		{
			IGameService service = Services.GetService<IGameService>();
			IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
			CreepingNode creepingNode = null;
			service2.TryGetValue<CreepingNode>(army.DismantlingCreepingNodeTarget, out creepingNode);
			if (creepingNode != null && !this.CanExecuteCreepingNodeDismantling(army, creepingNode))
			{
				IPlayerControllerRepositoryControl playerControllerRepositoryControl = (IPlayerControllerRepositoryControl)this.PlayerControllerRepositoryService;
				global::PlayerController playerControllerById = playerControllerRepositoryControl.GetPlayerControllerById("server");
				if (playerControllerById != null)
				{
					OrderToggleDismantleCreepingNode order = new OrderToggleDismantleCreepingNode(army.Empire.Index, army.GUID, army.DismantlingCreepingNodeTarget, false);
					playerControllerById.PostOrder(order);
				}
			}
		}
	}

	private void CheckEarthquakerStatus_OnWorldPositionChange(object sender, ArmyWorldPositionChangeEventArgs e)
	{
		Army army = sender as Army;
		if (army == null)
		{
			return;
		}
		if (army.IsEarthquaker)
		{
			army.SetEarthquakerStatus(false, false, null);
		}
	}

	private void CheckArmyOnMapBoost(object sender, ArmyWorldPositionChangeEventArgs e)
	{
		Army army = sender as Army;
		if (army == null)
		{
			return;
		}
		this.CheckArmyOnMapBoost(army);
	}

	private void CheckArmyOnMapBoost(Army army)
	{
		MapBoost mapBoostAtPosition = this.mapBoostService.GetMapBoostAtPosition(army.WorldPosition);
		if (mapBoostAtPosition != null)
		{
			this.mapBoostService.ApplyBoostToArmy(army, mapBoostAtPosition);
		}
	}

	public static bool IsUnitOnOwnedDistrict(Unit unit)
	{
		if (unit.Garrison is IWorldPositionable)
		{
			WorldPosition worldPosition = (unit.Garrison as IWorldPositionable).WorldPosition;
			Region region = DepartmentOfDefense.worldPositionningServiceStatic.GetRegion(worldPosition);
			if (region != null && region.City != null && region.City.Empire.Index == unit.Garrison.Empire.Index)
			{
				if (region.City.WorldPosition == worldPosition)
				{
					return true;
				}
				for (int i = 0; i < region.City.Districts.Count; i++)
				{
					if (region.City.Districts[i].Type != DistrictType.Exploitation && region.City.Districts[i].WorldPosition == worldPosition)
					{
						return true;
					}
				}
				if (region.City.Camp != null)
				{
					for (int j = 0; j < region.City.Camp.Districts.Count; j++)
					{
						if (region.City.Camp.Districts[j].Type != DistrictType.Exploitation && region.City.Camp.Districts[j].WorldPosition == worldPosition)
						{
							return true;
						}
					}
				}
			}
			return false;
		}
		return false;
	}

	public bool CanExecuteCreepingNodeDismantling(Army army, CreepingNode creepingNode)
	{
		IGameService service = Services.GetService<IGameService>();
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		IPathfindingService service3 = service.Game.Services.GetService<IPathfindingService>();
		Diagnostics.Assert(service3 != null);
		int distance = this.WorldPositionningService.GetDistance(army.WorldPosition, creepingNode.WorldPosition);
		if (distance > 1)
		{
			return false;
		}
		PathfindingFlags flags = PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreKaijuGarrisons;
		return service3.IsTransitionPassable(army.WorldPosition, creepingNode.WorldPosition, army, flags, null);
	}

	public static WorldPosition GetFirstAvailablePositionToTransferGarrisonUnits(IGarrisonWithPosition garrison, int radius = 1, PathfindingMovementCapacity pathfindingMovementCapacity = PathfindingMovementCapacity.Ground, PathfindingFlags pathfindingFlags = PathfindingFlags.IgnoreFogOfWar)
	{
		return DepartmentOfDefense.GetFirstAvailablePositionToTransferUnits(garrison.WorldPosition, radius, pathfindingMovementCapacity, pathfindingFlags);
	}

	public static WorldPosition GetFirstAvailablePositionToTransferUnits(WorldPosition sourceWorldPosition, int radius = 1, PathfindingMovementCapacity pathfindingMovementCapacity = PathfindingMovementCapacity.Ground, PathfindingFlags pathfindingFlags = PathfindingFlags.IgnoreFogOfWar)
	{
		WorldPosition invalid = WorldPosition.Invalid;
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		IPathfindingService service2 = service.Game.Services.GetService<IPathfindingService>();
		IWorldPositionningService positionService = service.Game.Services.GetService<IWorldPositionningService>();
		WorldCircle worldCircle = new WorldCircle(sourceWorldPosition, radius);
		WorldPosition[] worldPositions = worldCircle.GetWorldPositions(positionService.World.WorldParameters);
		global::Game game = service.Game as global::Game;
		GridMap<Army> gridMap = (!(game != null)) ? null : (game.World.Atlas.GetMap(WorldAtlas.Maps.Armies) as GridMap<Army>);
		List<WorldPosition> list = new List<WorldPosition>();
		foreach (WorldPosition worldPosition in worldPositions)
		{
			if (worldPosition.IsValid)
			{
				if (gridMap != null)
				{
					Army value = gridMap.GetValue(worldPosition);
					if (value != null)
					{
						goto IL_127;
					}
				}
				if (service2.IsTileStopableAndPassable(worldPosition, pathfindingMovementCapacity, pathfindingFlags))
				{
					list.Add(worldPosition);
				}
			}
			IL_127:;
		}
		list.Sort((WorldPosition left, WorldPosition right) => positionService.GetDistance(sourceWorldPosition, left).CompareTo(positionService.GetDistance(sourceWorldPosition, right)));
		return list.First<WorldPosition>();
	}

	public override void DumpAsText(StringBuilder content, string indent = "")
	{
		base.DumpAsText(content, indent);
		content.AppendFormat("{0}nextAvailableUnitDesignModelId = {1}\r\n", indent, this.nextAvailableUnitDesignModelId);
		for (int i = 0; i < this.armies.Count; i++)
		{
			Army army = this.armies[i];
			content.AppendFormat("{0}{1} WorldPosition = ({2},{3})\r\n", new object[]
			{
				indent,
				army.Name,
				army.WorldPosition.Column,
				army.WorldPosition.Row
			});
			foreach (Unit unit in army.Units)
			{
				content.AppendFormat("{0}{1} '{2}'\r\n", indent + "  ", unit.Name, unit.UnitDesign.FullName);
			}
		}
	}

	public override byte[] DumpAsBytes()
	{
		MemoryStream memoryStream = new MemoryStream();
		using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
		{
			binaryWriter.Write(base.DumpAsBytes());
			binaryWriter.Write(this.nextAvailableUnitDesignModelId);
			for (int i = 0; i < this.armies.Count; i++)
			{
				Army army = this.armies[i];
				binaryWriter.Write(army.Name);
				binaryWriter.Write(army.WorldPosition.Column);
				binaryWriter.Write(army.WorldPosition.Row);
				foreach (Unit unit in army.Units)
				{
					binaryWriter.Write(unit.Name);
					binaryWriter.Write(unit.UnitDesign.FullName);
				}
			}
		}
		byte[] result = memoryStream.ToArray();
		memoryStream.Close();
		return result;
	}

	public IUnitDesignDatabase UnitDesignDatabase
	{
		get
		{
			return this;
		}
	}

	private List<UnitDesign> DatabaseCompatibleUnitDesigns
	{
		get
		{
			return this.hiddenUnitDesigns;
		}
	}

	private List<UnitDesign> AvailableUnitDesigns
	{
		get
		{
			return this.availableUnitDesigns;
		}
	}

	public bool CheckWhetherUnitBodyDefinitionIsValid(UnitBodyDefinition unitBodyDefinition)
	{
		return unitBodyDefinition != null && (this.departmentOfTheTreasury == null || DepartmentOfTheTreasury.CheckConstructiblePrerequisites(base.Empire, unitBodyDefinition, new string[]
		{
			"Prerequisite"
		}));
	}

	public IEnumerable<UnitDesign> GetOutdatedUnitDesignsAsEnumerable()
	{
		foreach (UnitDesign unitDesign in this.outdatedUnitDesigns)
		{
			yield return unitDesign;
		}
		yield break;
	}

	public IEnumerable<UnitDesign> GetRegisteredUnitDesignsAsEnumerable()
	{
		foreach (UnitDesign unitDesign in this.hiddenUnitDesigns)
		{
			yield return unitDesign;
		}
		foreach (UnitDesign unitDesign2 in this.availableUnitDesigns)
		{
			yield return unitDesign2;
		}
		foreach (UnitDesign unitDesign3 in this.outdatedUnitDesigns)
		{
			yield return unitDesign3;
		}
		yield break;
	}

	public override void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		base.ReadXml(reader);
		Diagnostics.Assert(this.availableUnitDesigns != null && this.outdatedUnitDesigns != null);
		if (num >= 3)
		{
			this.nextAvailableUnitDesignModelId = reader.GetAttribute<uint>("NextAvailableUnitDesignModelId", this.nextAvailableUnitDesignModelId);
		}
		reader.ReadStartElement("UnitDesigns");
		int attribute = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("Available");
		ISerializationService service = Services.GetService<ISerializationService>();
		XmlSerializer xmlSerializer = service.GetXmlSerializer<UnitDesign>(new Type[]
		{
			typeof(UnitProfile)
		});
		for (int i = 0; i < attribute; i++)
		{
			UnitDesign unitDesign = xmlSerializer.Deserialize(reader.Reader) as UnitDesign;
			if (unitDesign != null)
			{
				if (unitDesign.Tags.Contains(DownloadableContent9.TagColossus) && !unitDesign.Tags.Contains(DownloadableContent9.TagSolitary))
				{
					unitDesign.Tags.AddTag(DownloadableContent9.TagSolitary);
				}
				if (unitDesign is UnitProfile)
				{
					this.AddUnitDesign(unitDesign, unitDesign.Model, DepartmentOfDefense.UnitDesignState.Hidden);
				}
				else
				{
					this.AddUnitDesign(unitDesign, unitDesign.Model, DepartmentOfDefense.UnitDesignState.Available);
				}
			}
		}
		reader.ReadEndElement("Available");
		int attribute2 = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("Outdated");
		ISerializationService service2 = Services.GetService<ISerializationService>();
		XmlSerializer xmlSerializer2 = service2.GetXmlSerializer<UnitDesign>(new Type[]
		{
			typeof(UnitProfile)
		});
		for (int j = 0; j < attribute2; j++)
		{
			UnitDesign unitDesign2 = xmlSerializer2.Deserialize(reader.Reader) as UnitDesign;
			if (unitDesign2 != null)
			{
				if (!(unitDesign2 is UnitProfile))
				{
					this.AddUnitDesign(unitDesign2, unitDesign2.Model, DepartmentOfDefense.UnitDesignState.Outdated);
				}
			}
		}
		reader.ReadEndElement("Outdated");
		int attribute3 = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("Hidden");
		ISerializationService service3 = Services.GetService<ISerializationService>();
		XmlSerializer xmlSerializer3 = service3.GetXmlSerializer<UnitDesign>(new Type[]
		{
			typeof(UnitProfile)
		});
		for (int k = 0; k < attribute3; k++)
		{
			UnitDesign unitDesign3 = xmlSerializer3.Deserialize(reader.Reader) as UnitDesign;
			if (unitDesign3 != null)
			{
				this.AddUnitDesign(unitDesign3, unitDesign3.Model, DepartmentOfDefense.UnitDesignState.Hidden);
			}
		}
		reader.ReadEndElement("Hidden");
		reader.ReadEndElement("UnitDesigns");
		this.UnlockUnitDesign();
		int attribute4 = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("Armies");
		this.armies.Clear();
		for (int l = 0; l < attribute4; l++)
		{
			ulong attribute5 = reader.GetAttribute<ulong>("GUID");
			Army army = new Army(attribute5)
			{
				Empire = (global::Empire)base.Empire
			};
			reader.ReadElementSerializable<Army>(ref army);
			if (army != null)
			{
				if (army.UnitsCount == 0)
				{
					Diagnostics.LogWarning("Trying to load an army without units. Army GUID: " + attribute5.ToString());
				}
				army.ClientLocalizedName = this.GenerateArmyClientLocalizedName(army);
				this.AddArmy(army);
			}
		}
		reader.ReadEndElement("Armies");
		if (num >= 4)
		{
			if (reader.IsStartElement("DelayedSolitaryUnitSpawnCommands"))
			{
				int attribute6 = reader.GetAttribute<int>("Count");
				if (attribute6 > 0)
				{
					this.delayedSolitaryUnitSpawnCommands = new List<DelayedSolitaryUnitSpawnCommand>(attribute6);
					reader.ReadStartElement("DelayedSolitaryUnitSpawnCommands");
					for (int m = 0; m < attribute6; m++)
					{
						GameEntityGUID garrisonGameEntityGUID = reader.GetAttribute<ulong>("GarrisonGameEntityGUID");
						GameEntityGUID gameEntityGUID = reader.GetAttribute<ulong>("GameEntityGUID");
						short attribute7 = reader.GetAttribute<short>("Row");
						short attribute8 = reader.GetAttribute<short>("Column");
						DelayedSolitaryUnitSpawnCommand item = new DelayedSolitaryUnitSpawnCommand(garrisonGameEntityGUID, gameEntityGUID, null, new WorldPosition(attribute7, attribute8));
						this.delayedSolitaryUnitSpawnCommands.Add(item);
						reader.Skip("Command");
					}
					reader.ReadEndElement("DelayedSolitaryUnitSpawnCommands");
				}
				else
				{
					reader.Skip("DelayedSolitaryUnitSpawnCommands");
				}
			}
		}
		else if (num >= 2 && reader.IsStartElement("DelayedColossiSpawnCommands"))
		{
			int attribute9 = reader.GetAttribute<int>("Count");
			if (attribute9 > 0)
			{
				this.delayedSolitaryUnitSpawnCommands = new List<DelayedSolitaryUnitSpawnCommand>(attribute9);
				reader.ReadStartElement("DelayedColossiSpawnCommands");
				for (int n = 0; n < attribute9; n++)
				{
					GameEntityGUID garrisonGameEntityGUID2 = reader.GetAttribute<ulong>("GarrisonGameEntityGUID");
					GameEntityGUID gameEntityGUID2 = reader.GetAttribute<ulong>("GameEntityGUID");
					short attribute10 = reader.GetAttribute<short>("Row");
					short attribute11 = reader.GetAttribute<short>("Column");
					DelayedSolitaryUnitSpawnCommand item2 = new DelayedSolitaryUnitSpawnCommand(garrisonGameEntityGUID2, gameEntityGUID2, null, new WorldPosition(attribute10, attribute11));
					this.delayedSolitaryUnitSpawnCommands.Add(item2);
					reader.Skip("Command");
				}
				reader.ReadEndElement("DelayedColossiSpawnCommands");
			}
			else
			{
				reader.Skip("DelayedColossiSpawnCommands");
			}
		}
		if (num >= 5)
		{
			this.TurnWhenLastBegun = reader.ReadElementString<int>("TurnWhenLastBegun");
		}
		if (num >= 6)
		{
			this.LandUnitBuildOrPurchased = reader.ReadElementString<bool>("LandUnitBuildOrPurchased");
		}
		else
		{
			this.LandUnitBuildOrPurchased = false;
		}
		if (num >= 7)
		{
			this.TurnWhenLastUnshiftedUnits = reader.ReadElementString<int>("TurnWhenLastUnshiftedUnits");
		}
		if (num >= 8)
		{
			this.TurnWhenLastImmolatedUnits = reader.ReadElementString<int>("TurnWhenLastImmolatedUnits");
		}
		if (num >= 9)
		{
			int attribute12 = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("KaijuArmies");
			for (int num2 = 0; num2 < attribute12; num2++)
			{
				ulong attribute13 = reader.GetAttribute<ulong>("GUID");
				KaijuArmy kaijuArmy = new KaijuArmy(attribute13)
				{
					Empire = (global::Empire)base.Empire
				};
				reader.ReadElementSerializable<KaijuArmy>(ref kaijuArmy);
				if (kaijuArmy != null)
				{
					if (kaijuArmy.UnitsCount == 0)
					{
						Diagnostics.LogWarning("Trying to load an army without units. Army GUID: " + attribute13.ToString());
					}
					kaijuArmy.ClientLocalizedName = this.GenerateArmyClientLocalizedName(kaijuArmy);
					this.AddArmy(kaijuArmy);
				}
			}
			reader.ReadEndElement("KaijuArmies");
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		int num = writer.WriteVersionAttribute(9);
		base.WriteXml(writer);
		writer.WriteStartElement("UnitDesigns");
		writer.WriteAttributeString<uint>("NextAvailableUnitDesignModelId", this.nextAvailableUnitDesignModelId);
		ISerializationService service = Services.GetService<ISerializationService>();
		XmlSerializer xmlSerializer = service.GetXmlSerializer<UnitDesign>(new Type[]
		{
			typeof(UnitProfile)
		});
		writer.WriteStartElement("Available");
		Diagnostics.Assert(this.availableUnitDesigns != null);
		writer.WriteAttributeString<int>("Count", this.availableUnitDesigns.Count);
		for (int i = 0; i < this.availableUnitDesigns.Count; i++)
		{
			UnitDesign unitDesign = this.availableUnitDesigns[i];
			unitDesign.XmlSerializableUnitEquipmentSet = (UnitEquipmentSet)unitDesign.UnitEquipmentSet.Clone();
			xmlSerializer.Serialize(writer.Writer, unitDesign);
		}
		writer.WriteEndElement();
		writer.WriteStartElement("Outdated");
		Diagnostics.Assert(this.outdatedUnitDesigns != null);
		writer.WriteAttributeString<int>("Count", this.outdatedUnitDesigns.Count);
		for (int j = 0; j < this.outdatedUnitDesigns.Count; j++)
		{
			UnitDesign unitDesign2 = this.outdatedUnitDesigns[j];
			unitDesign2.XmlSerializableUnitEquipmentSet = (UnitEquipmentSet)unitDesign2.UnitEquipmentSet.Clone();
			xmlSerializer.Serialize(writer.Writer, unitDesign2);
		}
		writer.WriteEndElement();
		writer.WriteStartElement("Hidden");
		Diagnostics.Assert(this.hiddenUnitDesigns != null);
		writer.WriteAttributeString<int>("Count", this.hiddenUnitDesigns.Count);
		for (int k = 0; k < this.hiddenUnitDesigns.Count; k++)
		{
			UnitDesign unitDesign3 = this.hiddenUnitDesigns[k];
			unitDesign3.XmlSerializableUnitEquipmentSet = (UnitEquipmentSet)unitDesign3.UnitEquipmentSet.Clone();
			xmlSerializer.Serialize(writer.Writer, unitDesign3);
		}
		writer.WriteEndElement();
		writer.WriteEndElement();
		List<Army> list = new List<Army>();
		List<KaijuArmy> list2 = new List<KaijuArmy>();
		if (num >= 9)
		{
			for (int l = 0; l < this.armies.Count; l++)
			{
				Army army = this.armies[l];
				if (army is KaijuArmy)
				{
					list2.Add(army as KaijuArmy);
				}
				else
				{
					list.Add(army);
				}
			}
		}
		writer.WriteStartElement("Armies");
		writer.WriteAttributeString<int>("Count", list.Count);
		for (int m = 0; m < list.Count; m++)
		{
			if (list[m].UnitsCount == 0)
			{
				Diagnostics.LogWarning("Trying to save an army without units.");
			}
			Amplitude.Xml.Serialization.IXmlSerializable xmlSerializable = list[m];
			writer.WriteElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>(ref xmlSerializable);
		}
		writer.WriteEndElement();
		if (num >= 2 && this.delayedSolitaryUnitSpawnCommands != null)
		{
			writer.WriteStartElement("DelayedSolitaryUnitSpawnCommands");
			writer.WriteAttributeString<int>("Count", this.delayedSolitaryUnitSpawnCommands.Count);
			for (int n = 0; n < this.delayedSolitaryUnitSpawnCommands.Count; n++)
			{
				writer.WriteStartElement("Command");
				writer.WriteAttributeString<ulong>("GarrisonGameEntityGUID", this.delayedSolitaryUnitSpawnCommands[n].GarrisonGameEntityGUID);
				writer.WriteAttributeString<ulong>("GameEntityGUID", this.delayedSolitaryUnitSpawnCommands[n].GameEntityGUID);
				writer.WriteAttributeString<short>("Row", this.delayedSolitaryUnitSpawnCommands[n].WorldPosition.Row);
				writer.WriteAttributeString<short>("Column", this.delayedSolitaryUnitSpawnCommands[n].WorldPosition.Column);
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}
		if (num >= 5)
		{
			writer.WriteElementString<int>("TurnWhenLastBegun", this.TurnWhenLastBegun);
		}
		if (num >= 6)
		{
			writer.WriteElementString<bool>("LandUnitBuildOrPurchased", this.LandUnitBuildOrPurchased);
		}
		if (num >= 7)
		{
			writer.WriteElementString<int>("TurnWhenLastUnshiftedUnits", this.TurnWhenLastUnshiftedUnits);
		}
		if (num >= 8)
		{
			writer.WriteElementString<int>("TurnWhenLastImmolatedUnits", this.TurnWhenLastImmolatedUnits);
		}
		if (num >= 9)
		{
			writer.WriteStartElement("KaijuArmies");
			writer.WriteAttributeString<int>("Count", list2.Count);
			for (int num2 = 0; num2 < list2.Count; num2++)
			{
				if (list2[num2].UnitsCount == 0)
				{
					Diagnostics.LogWarning("Trying to save an kiaju army without units.");
				}
				Amplitude.Xml.Serialization.IXmlSerializable xmlSerializable2 = list2[num2];
				writer.WriteElementSerializable<Amplitude.Xml.Serialization.IXmlSerializable>(ref xmlSerializable2);
			}
			writer.WriteEndElement();
		}
	}

	private bool AttackPreprocessor(OrderAttack order)
	{
		if (!order.AttackerGUID.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the attacker guid is not valid.");
			return false;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.AttackerGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the attacker is not referenced (guid = {0:X8}).", new object[]
			{
				order.AttackerGUID
			});
			return false;
		}
		IBattleEncounterRepositoryService service = this.GameService.Game.Services.GetService<IBattleEncounterRepositoryService>();
		if (service != null)
		{
			IEnumerable<BattleEncounter> enumerable = service;
			if (enumerable != null)
			{
				bool flag = enumerable.Any((BattleEncounter encounter) => encounter.IsGarrisonInEncounter(order.AttackerGUID) || encounter.IsGarrisonInEncounter(order.DefenderGUID));
				if (flag)
				{
					Diagnostics.LogWarning("Order preprocessing failed because the attacker already in combat ");
					return false;
				}
			}
		}
		Army attacker = gameEntity as Army;
		if (attacker == null || !(attacker is IWorldPositionable))
		{
			Diagnostics.LogWarning("Order preprocessing failed because the attacker is either not an 'Army' or does not implement 'IWorldPositionable'.");
			return false;
		}
		if (order.NumberOfActionPointsToSpend < 0f)
		{
			ArmyAction armyAction = null;
			IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
			if (database != null && database.TryGetValue(ArmyAction_Attack.ReadOnlyName, out armyAction))
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
		if (!order.DefenderGUID.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the defender guid is not valid.");
			return false;
		}
		if (!this.GameEntityRepositoryService.TryGetValue(order.DefenderGUID, out gameEntity))
		{
			Diagnostics.LogWarning("Order preprocessing failed because the defender is not referenced (guid = {0:X8}).", new object[]
			{
				order.DefenderGUID
			});
			return false;
		}
		if (!(gameEntity is IWorldPositionable))
		{
			Diagnostics.LogError("Order preprocessing failed because the defender does not implement 'IWorldPositionable'.");
			return false;
		}
		Army army = gameEntity as Army;
		if (attacker != null && army != null && attacker.IsNaval != army.IsNaval)
		{
			Diagnostics.LogWarning("Order preprocessing failed because the defender is not a ship.");
			return false;
		}
		IWorldPositionable worldPositionable = gameEntity as IWorldPositionable;
		int distance = this.WorldPositionningService.GetDistance(worldPositionable.WorldPosition, attacker.WorldPosition);
		if (distance != 1 && !(gameEntity is Fortress))
		{
			Diagnostics.LogWarning("Order preprocessing failed because the distance between the attacker and the defender is not equal to 1 tile.(attacker = {0:X8}, defender = {1:X8}), distance = {2}.", new object[]
			{
				order.AttackerGUID,
				order.DefenderGUID,
				distance
			});
			return false;
		}
		if (gameEntity is Fortress)
		{
			bool flag2 = false;
			Fortress fortress = gameEntity as Fortress;
			distance = this.WorldPositionningService.GetDistance(fortress.WorldPosition, attacker.WorldPosition);
			if (distance == 1)
			{
				worldPositionable = fortress;
				flag2 = true;
			}
			for (int i = 0; i < fortress.Facilities.Count; i++)
			{
				distance = this.WorldPositionningService.GetDistance(fortress.Facilities[i].WorldPosition, attacker.WorldPosition);
				if (distance == 1)
				{
					worldPositionable = fortress.Facilities[i];
					flag2 = true;
				}
			}
			if (!flag2)
			{
				Diagnostics.LogWarning("Order preprocessing failed because the distance between the attacker and the defender is not equal to 1 tile.(attacker = {0:X8}, defender = {1:X8}).", new object[]
				{
					order.AttackerGUID,
					order.DefenderGUID
				});
				return false;
			}
		}
		bool flag3 = true;
		DepartmentOfForeignAffairs agency = base.Empire.GetAgency<DepartmentOfForeignAffairs>();
		if (agency != null)
		{
			flag3 = agency.CanAttack(gameEntity);
		}
		if (base.Empire is MajorEmpire && !attacker.IsPrivateers)
		{
			Diagnostics.Assert(agency != null);
			if (!flag3)
			{
				Diagnostics.LogWarning("Order preprocessing failed because the diplomatic relation doesn't authorize the attack.(attacker = {0:X8}, defender = {1:X8}).", new object[]
				{
					order.AttackerGUID,
					order.DefenderGUID
				});
				return false;
			}
		}
		if (!this.PathfindingService.IsTransitionPassable(attacker.WorldPosition, worldPositionable.WorldPosition, attacker, OrderAttack.AttackFlags, null))
		{
			Diagnostics.LogWarning("Order preprocessing failed because the transition between the attacker and the defender is not passable.(attacker = {0:X8}, defender = {1:X8}).", new object[]
			{
				order.AttackerGUID,
				order.DefenderGUID
			});
			return false;
		}
		DepartmentOfTransportation agency2 = base.Empire.GetAgency<DepartmentOfTransportation>();
		if (agency2 != null)
		{
			ArmyGoToInstruction armyGoToInstruction = agency2.ArmiesWithPendingGoToInstructions.Find((ArmyGoToInstruction match) => match.ArmyGUID == attacker.GUID);
			if (armyGoToInstruction != null && armyGoToInstruction.IsMoving)
			{
				armyGoToInstruction.Cancel(false);
			}
		}
		IPlayerControllerRepositoryService service2 = this.GameService.Game.Services.GetService<IPlayerControllerRepositoryService>();
		IPlayerControllerRepositoryControl playerControllerRepositoryControl = service2 as IPlayerControllerRepositoryControl;
		if (playerControllerRepositoryControl != null)
		{
			global::PlayerController playerControllerById = playerControllerRepositoryControl.GetPlayerControllerById("server");
			order.EncounterGUID = this.GameEntityRepositoryService.GenerateGUID();
			if (gameEntity is District)
			{
				DepartmentOfDefense.<AttackPreprocessor>c__AnonStorey946 <AttackPreprocessor>c__AnonStorey2 = new DepartmentOfDefense.<AttackPreprocessor>c__AnonStorey946();
				District district = gameEntity as District;
				IGarrison garrison = district.City;
				if (garrison.Empire == attacker.Empire)
				{
					Diagnostics.LogWarning("Order preprocessing failed because the defender and attacker are on the same side!");
					return false;
				}
				if (district.City.BesiegingEmpire != null && district.City.BesiegingEmpire != attacker.Empire)
				{
					Diagnostics.LogWarning("Order preprocessing failed because the city is besieged by someone else.");
					return false;
				}
				<AttackPreprocessor>c__AnonStorey2.cityDefense = garrison.Empire.GetAgency<DepartmentOfDefense>();
				int index;
				for (index = 0; index < <AttackPreprocessor>c__AnonStorey2.cityDefense.Armies.Count; index++)
				{
					if (!<AttackPreprocessor>c__AnonStorey2.cityDefense.armies[index].IsNaval)
					{
						if (district.City.Districts.Any((District match) => match.Type != DistrictType.Exploitation && match.WorldPosition == <AttackPreprocessor>c__AnonStorey2.cityDefense.armies[index].WorldPosition))
						{
							garrison = <AttackPreprocessor>c__AnonStorey2.cityDefense.armies[index];
							break;
						}
					}
				}
				if (playerControllerById != null)
				{
					bool flag4 = attacker.Empire.IsControlledByAI && garrison.Empire.IsControlledByAI;
					EncounterOptionChoice encounterMode = EncounterOptionChoice.Manual;
					if (flag4)
					{
						encounterMode = EncounterOptionChoice.Simulated;
					}
					OrderCreateCityAssaultEncounter order2 = new OrderCreateCityAssaultEncounter(order.EncounterGUID, attacker.GUID, garrison.GUID, district.City.GUID, district.City.Militia.GUID, false, flag3, encounterMode);
					playerControllerById.PostOrder(order2);
				}
			}
			else
			{
				IGarrison garrison2;
				if (gameEntity is Kaiju)
				{
					Kaiju kaiju = gameEntity as Kaiju;
					garrison2 = kaiju.GetActiveTroops();
				}
				else
				{
					garrison2 = (gameEntity as IGarrison);
				}
				if (garrison2 == null)
				{
					Diagnostics.LogError("Order preprocessing failed because the defender does not implement 'IGarrison'.");
					return false;
				}
				if (garrison2.Empire == attacker.Empire)
				{
					Diagnostics.LogWarning("Order preprocessing failed because the defender and attacker are on the same side!");
					return false;
				}
				if (playerControllerById != null)
				{
					bool flag5 = attacker.Empire.IsControlledByAI && garrison2.Empire.IsControlledByAI;
					bool instant = false;
					EncounterOptionChoice encounterMode2 = EncounterOptionChoice.Manual;
					if (flag5)
					{
						encounterMode2 = EncounterOptionChoice.Simulated;
					}
					OrderCreateEncounter order3 = new OrderCreateEncounter(order.EncounterGUID, attacker.GUID, garrison2.GUID, instant, flag3, encounterMode2);
					playerControllerById.PostOrder(order3);
				}
			}
		}
		return true;
	}

	private IEnumerator AttackProcessor(OrderAttack order)
	{
		if (!order.AttackerGUID.IsValid)
		{
			Diagnostics.LogError("Order processing failed because the attacker guid is not valid.");
			yield break;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.AttackerGUID, out gameEntity))
		{
			Diagnostics.LogError("Order processing failed because the attacker is not referenced (guid = {0:X8}).", new object[]
			{
				order.AttackerGUID
			});
			yield break;
		}
		Army attacker = gameEntity as Army;
		if (attacker == null || !(attacker is IWorldPositionable))
		{
			Diagnostics.LogError("Order processing failed because the attacker is either not an 'Army' or does not implement 'IWorldPositionable'.");
			yield break;
		}
		if (!order.DefenderGUID.IsValid)
		{
			Diagnostics.LogError("Order processing failed because the defender guid is not valid.");
			yield break;
		}
		if (!this.GameEntityRepositoryService.TryGetValue(order.DefenderGUID, out gameEntity))
		{
			Diagnostics.LogError("Order processing failed because the defender is not referenced (guid = {0:X8}).", new object[]
			{
				order.AttackerGUID
			});
			yield break;
		}
		if (!(gameEntity is IWorldPositionable))
		{
			Diagnostics.LogError("Order preprocessing failed because the defender does not implement 'IWorldPositionable'.");
			yield break;
		}
		IWorldPositionable defenderPositionable = gameEntity as IWorldPositionable;
		WorldOrientation attackerOrientation = this.WorldPositionningService.GetOrientation(attacker.WorldPosition, defenderPositionable.WorldPosition);
		WorldOrientation defenderOrientation = attackerOrientation.Rotate(3);
		attacker.WorldOrientation = attackerOrientation;
		if (defenderPositionable is Army)
		{
			(defenderPositionable as Army).WorldOrientation = defenderOrientation;
		}
		if (this.OnAttackStart != null)
		{
			this.OnAttackStart(this, new AttackStartEventArgs(attacker, gameEntity));
		}
		IEventService eventService = Services.GetService<IEventService>();
		if (eventService != null && this.weatherService != null)
		{
			WeatherDefinition weather = this.weatherService.GetWeatherDefinitionAtPosition(attacker.WorldPosition);
			if (weather != null)
			{
				if (weather.Name == weather.WeatherFog)
				{
					eventService.Notify(new EventFogBankAmbush(base.Empire));
				}
				if (weather.Name == weather.WeatherDustStorm)
				{
					eventService.Notify(new EventDustStormAmbush(base.Empire));
				}
			}
		}
		yield break;
	}

	private bool AttackCityPreprocessor(OrderAttackCity order)
	{
		DepartmentOfDefense.<AttackCityPreprocessor>c__AnonStorey948 <AttackCityPreprocessor>c__AnonStorey = new DepartmentOfDefense.<AttackCityPreprocessor>c__AnonStorey948();
		<AttackCityPreprocessor>c__AnonStorey.order = order;
		if (!<AttackCityPreprocessor>c__AnonStorey.order.AttackerGUID.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the attacker guid is not valid.");
			return false;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(<AttackCityPreprocessor>c__AnonStorey.order.AttackerGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the attacker is not referenced (guid = {0:X8}).", new object[]
			{
				<AttackCityPreprocessor>c__AnonStorey.order.AttackerGUID
			});
			return false;
		}
		IBattleEncounterRepositoryService service = this.GameService.Game.Services.GetService<IBattleEncounterRepositoryService>();
		if (service != null)
		{
			IEnumerable<BattleEncounter> enumerable = service;
			if (enumerable != null)
			{
				bool flag = enumerable.Any((BattleEncounter encounter) => encounter.IsGarrisonInEncounter(<AttackCityPreprocessor>c__AnonStorey.order.AttackerGUID) || encounter.IsGarrisonInEncounter(<AttackCityPreprocessor>c__AnonStorey.order.DefenderGUID));
				if (flag)
				{
					return false;
				}
			}
		}
		<AttackCityPreprocessor>c__AnonStorey.attacker = (gameEntity as Army);
		if (<AttackCityPreprocessor>c__AnonStorey.attacker == null || !(<AttackCityPreprocessor>c__AnonStorey.attacker is IWorldPositionable))
		{
			Diagnostics.LogError("Order preprocessing failed because the attacker is either not an 'Army' or does not implement 'IWorldPositionable'.");
			return false;
		}
		if (!<AttackCityPreprocessor>c__AnonStorey.order.DefenderGUID.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the defender guid is not valid.");
			return false;
		}
		if (!this.GameEntityRepositoryService.TryGetValue(<AttackCityPreprocessor>c__AnonStorey.order.DefenderGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the defender is not referenced (guid = {0:X8}).", new object[]
			{
				<AttackCityPreprocessor>c__AnonStorey.order.AttackerGUID
			});
			return false;
		}
		if (!(gameEntity is District) || !(gameEntity is IWorldPositionable))
		{
			Diagnostics.LogError("Order preprocessing failed because the attack target is either not a 'District' or does not implement 'IWorldPositionable'.");
			return false;
		}
		District district = gameEntity as District;
		if (district.City.BesiegingEmpireIndex == <AttackCityPreprocessor>c__AnonStorey.attacker.Empire.Index)
		{
			if (!this.PathfindingService.IsTransitionPassable(<AttackCityPreprocessor>c__AnonStorey.attacker.WorldPosition, district.WorldPosition, <AttackCityPreprocessor>c__AnonStorey.attacker, PathfindingFlags.IgnoreSieges, null))
			{
				Diagnostics.LogError("Order preprocessing failed because the transition between the attacker and the defender is not passable.");
				return false;
			}
		}
		else if (!this.PathfindingService.IsTransitionPassable(<AttackCityPreprocessor>c__AnonStorey.attacker.WorldPosition, district.WorldPosition, <AttackCityPreprocessor>c__AnonStorey.attacker, (PathfindingFlags)0, null))
		{
			Diagnostics.LogError("Order preprocessing failed because the transition between the attacker and the defender is not passable.");
			return false;
		}
		DepartmentOfTransportation agency = base.Empire.GetAgency<DepartmentOfTransportation>();
		ArmyGoToInstruction armyGoToInstruction = agency.ArmiesWithPendingGoToInstructions.Find((ArmyGoToInstruction match) => match.ArmyGUID == <AttackCityPreprocessor>c__AnonStorey.attacker.GUID);
		if (armyGoToInstruction != null && armyGoToInstruction.IsMoving)
		{
			armyGoToInstruction.Cancel(false);
		}
		bool flag2 = true;
		DepartmentOfForeignAffairs agency2 = base.Empire.GetAgency<DepartmentOfForeignAffairs>();
		if (agency2 != null)
		{
			flag2 = agency2.CanAttack(gameEntity);
		}
		if (base.Empire is MajorEmpire && !<AttackCityPreprocessor>c__AnonStorey.attacker.IsPrivateers && !flag2)
		{
			Diagnostics.LogWarning("Order preprocessing failed because the diplomatic relation doesn't authorize the attack.(attacker = {0:X8}, defender = {1:X8}).", new object[]
			{
				<AttackCityPreprocessor>c__AnonStorey.order.AttackerGUID,
				<AttackCityPreprocessor>c__AnonStorey.order.DefenderGUID
			});
			return false;
		}
		IGarrison garrison = district.City;
		<AttackCityPreprocessor>c__AnonStorey.cityDefense = garrison.Empire.GetAgency<DepartmentOfDefense>();
		int index;
		for (index = 0; index < <AttackCityPreprocessor>c__AnonStorey.cityDefense.Armies.Count; index++)
		{
			if (!<AttackCityPreprocessor>c__AnonStorey.cityDefense.armies[index].IsNaval)
			{
				if (district.City.Districts.Any((District match) => match.Type != DistrictType.Exploitation && match.WorldPosition == <AttackCityPreprocessor>c__AnonStorey.cityDefense.armies[index].WorldPosition))
				{
					garrison = <AttackCityPreprocessor>c__AnonStorey.cityDefense.armies[index];
					break;
				}
			}
		}
		<AttackCityPreprocessor>c__AnonStorey.order.EncounterGUID = this.GameEntityRepositoryService.GenerateGUID();
		IPlayerControllerRepositoryService service2 = this.GameService.Game.Services.GetService<IPlayerControllerRepositoryService>();
		IPlayerControllerRepositoryControl playerControllerRepositoryControl = service2 as IPlayerControllerRepositoryControl;
		if (playerControllerRepositoryControl != null)
		{
			global::PlayerController playerControllerById = playerControllerRepositoryControl.GetPlayerControllerById("server");
			if (playerControllerById != null)
			{
				bool flag3 = <AttackCityPreprocessor>c__AnonStorey.attacker.Empire.IsControlledByAI && garrison.Empire.IsControlledByAI;
				EncounterOptionChoice encounterMode = EncounterOptionChoice.Manual;
				if (flag3)
				{
					encounterMode = EncounterOptionChoice.Simulated;
				}
				OrderCreateCityAssaultEncounter order2 = new OrderCreateCityAssaultEncounter(<AttackCityPreprocessor>c__AnonStorey.order.EncounterGUID, <AttackCityPreprocessor>c__AnonStorey.attacker.GUID, garrison.GUID, district.City.GUID, district.City.Militia.GUID, false, flag2, encounterMode);
				playerControllerById.PostOrder(order2);
			}
		}
		return true;
	}

	private IEnumerator AttackCityProcessor(OrderAttackCity order)
	{
		Diagnostics.Log("Process order: {0}.", new object[]
		{
			order.ToString()
		});
		if (!order.AttackerGUID.IsValid)
		{
			Diagnostics.LogError("Order processing failed because the attacker guid is not valid.");
			yield break;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.AttackerGUID, out gameEntity))
		{
			Diagnostics.LogError("Order processing failed because the attacker is not referenced (guid = {0:X8}).", new object[]
			{
				order.AttackerGUID
			});
			yield break;
		}
		Army attacker = gameEntity as Army;
		if (attacker == null || !(attacker is IWorldPositionable))
		{
			Diagnostics.LogError("Order processing failed because the attacker is either not an 'Army' or does not implement 'IWorldPositionable'.");
			yield break;
		}
		if (!order.DefenderGUID.IsValid)
		{
			Diagnostics.LogError("Order processing failed because the defender guid is not valid.");
			yield break;
		}
		if (!this.GameEntityRepositoryService.TryGetValue(order.DefenderGUID, out gameEntity))
		{
			Diagnostics.LogError("Order processing failed because the defender is not referenced (guid = {0:X8}).", new object[]
			{
				order.AttackerGUID
			});
			yield break;
		}
		if (!(gameEntity is IWorldPositionable))
		{
			Diagnostics.LogError("Order preprocessing failed because the defender does not implement 'IWorldPositionable'.");
			yield break;
		}
		IWorldPositionable defenderPositionable = gameEntity as IWorldPositionable;
		WorldOrientation attackerOrientation = this.WorldPositionningService.GetOrientation(attacker.WorldPosition, defenderPositionable.WorldPosition);
		attacker.WorldOrientation = attackerOrientation;
		if (gameEntity is Army)
		{
			(gameEntity as Army).WorldOrientation = attackerOrientation.Rotate(3);
		}
		yield break;
	}

	internal UnitDesign FindBestUnitDesignAvailableForMilitiaUnits()
	{
		UnitDesign unitDesign = null;
		for (int i = 0; i < this.hiddenUnitDesigns.Count; i++)
		{
			UnitDesign unitDesign2 = this.hiddenUnitDesigns[i];
			if (unitDesign2.Tags.Contains(DepartmentOfDefense.TagMilitia))
			{
				if (unitDesign == null)
				{
					unitDesign = unitDesign2;
				}
				else if (unitDesign2.Model > unitDesign.Model)
				{
					unitDesign = unitDesign2;
				}
			}
		}
		return unitDesign;
	}

	private bool CreateMilitiaUnitsPreprocessor(OrderCreateMilitiaUnits order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityGameEntityGUID, out gameEntity) || !(gameEntity is City))
		{
			Diagnostics.LogError("Preprocessor failed because city guid is invalid. Empire={0}, CityGuid={1}", new object[]
			{
				base.Empire.Index,
				order.CityGameEntityGUID
			});
			return false;
		}
		City city = gameEntity as City;
		if (city == null)
		{
			Diagnostics.LogError("Preprocessor failed because city is null! Empire={0}, CityGuid={1}", new object[]
			{
				base.Empire.Index,
				order.CityGameEntityGUID
			});
			return false;
		}
		if (city.BesiegingEmpire != null)
		{
			Diagnostics.LogError("Preprocessor failed because City is under Besieging! Can not spawn Militia Units in a Besieging City!. Empire={0}, CityGuid={1}", new object[]
			{
				base.Empire.Index,
				order.CityGameEntityGUID
			});
			return false;
		}
		if (city.IsInfected)
		{
			Diagnostics.LogError("Preprocessor failed because City is infected! Can not spawn Militia Units in City Infected!. Empire={0}, CityGuid={1}", new object[]
			{
				base.Empire.Index,
				order.CityGameEntityGUID
			});
			return false;
		}
		return true;
	}

	private IEnumerator CreateMilitiaUnitsProcessor(OrderCreateMilitiaUnits order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityGameEntityGUID, out gameEntity) || !(gameEntity is City))
		{
			Diagnostics.LogError("Cannot find the city where to create new militia unit(s).");
			yield break;
		}
		UnitDesign unitDesign = this.FindBestUnitDesignAvailableForMilitiaUnits();
		if (unitDesign == null)
		{
			Diagnostics.LogError("Cannot find a unit design for the militia unit(s) to create.");
			yield break;
		}
		for (int index = 0; index < order.GameEntityGUIDs.Length; index++)
		{
			this.CreateMilitiaUnitIntoGarrison(order.GameEntityGUIDs[index], unitDesign, ((City)gameEntity).Militia);
		}
		yield break;
	}

	private void CreateMilitiaUnitIntoGarrison(GameEntityGUID unitMilitiaGuid, UnitDesign unitMilitiaDesign, Militia militia)
	{
		Diagnostics.Assert(unitMilitiaGuid.IsValid);
		Unit unit = DepartmentOfDefense.CreateUnitByDesign(unitMilitiaGuid, unitMilitiaDesign);
		if (unit != null)
		{
			Diagnostics.Assert(militia != null);
			militia.AddUnit(unit);
			unit.Refresh(true);
			this.GameEntityRepositoryService.Register(unit);
			float propertyValue = unit.GetPropertyValue(SimulationProperties.UnitExperienceRewardAtCreation);
			if (propertyValue > 0f)
			{
				unit.GainXp(propertyValue, true, true);
			}
		}
	}

	internal UnitDesign FindBestUnitDesignAvailableForUndeadUnits()
	{
		UnitDesign unitDesign = null;
		for (int i = 0; i < this.hiddenUnitDesigns.Count; i++)
		{
			UnitDesign unitDesign2 = this.hiddenUnitDesigns[i];
			if (unitDesign2.Tags.Contains(DepartmentOfDefense.TagUndead))
			{
				if (unitDesign == null)
				{
					unitDesign = unitDesign2;
				}
				else if (unitDesign2.Model > unitDesign.Model)
				{
					unitDesign = unitDesign2;
				}
			}
		}
		return unitDesign;
	}

	private bool CreateUndeadUnitsPreprocessor(OrderCreateUndeadUnits order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.GarrisonGUID, out gameEntity) || !(gameEntity is Garrison))
		{
			Diagnostics.LogError("Cannot find the garrison where to create new undead unit(s).");
			return false;
		}
		order.GameEntityGUIDs = new GameEntityGUID[order.UnitsToCreateCount];
		for (int i = 0; i < order.GameEntityGUIDs.Length; i++)
		{
			order.GameEntityGUIDs[i] = this.GameEntityRepositoryService.GenerateGUID();
		}
		return true;
	}

	private IEnumerator CreateUndeadUnitsProcessor(OrderCreateUndeadUnits order)
	{
		IGameEntity garrisonGameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.GarrisonGUID, out garrisonGameEntity) || !(garrisonGameEntity is Garrison))
		{
			Diagnostics.LogError("Cannot find the garrison where to create new undead unit(s).");
			yield break;
		}
		Garrison garrison = (Garrison)garrisonGameEntity;
		UnitDesign unitDesign = this.FindBestUnitDesignAvailableForUndeadUnits();
		if (unitDesign == null)
		{
			Diagnostics.LogError("Cannot find a unit design for the undead unit(s) to create.");
			yield break;
		}
		Contender contender = null;
		if (order.EncounterGUID != GameEntityGUID.Zero)
		{
			IEncounterRepositoryService encounterRepositoryService = this.GameService.Game.Services.GetService<IEncounterRepositoryService>();
			if (encounterRepositoryService != null)
			{
				Encounter encounter = null;
				if (encounterRepositoryService.TryGetValue(order.EncounterGUID, out encounter))
				{
					contender = encounter.Contenders.Find((Contender contenderMatch) => contenderMatch.GUID == order.GarrisonGUID);
				}
			}
		}
		for (int index = 0; index < order.GameEntityGUIDs.Length; index++)
		{
			Diagnostics.Assert(order.GameEntityGUIDs[index].IsValid);
			Unit unit = DepartmentOfDefense.CreateUnitByDesign(order.GameEntityGUIDs[index], unitDesign);
			if (unit != null)
			{
				Diagnostics.Assert(garrisonGameEntity is Garrison);
				this.GameEntityRepositoryService.Register(unit);
				garrison.AddUnit(unit);
				if (contender != null)
				{
					contender.NewUndeadUnitGUIDs.Add(unit.GUID);
				}
			}
		}
		yield break;
	}

	private bool CreateUnitDesignPreprocessor(OrderCreateUnitDesign order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		UnitBodyDefinition unitBodyDefinition = this.availableUnitBodyDefinitions.Find((UnitBodyDefinition body) => body.Name == order.UnitBodyDefinitionReference);
		if (unitBodyDefinition == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the unit body definition needed to create the unit design is not available.");
			return false;
		}
		order.UnitDesignModel = this.nextAvailableUnitDesignModelId + 1u;
		Diagnostics.Assert(this.availableUnitDesigns != null);
		UnitDesign unitDesign3 = this.availableUnitDesigns.Find((UnitDesign unitDesign) => unitDesign.Model == order.UnitDesignModel);
		if (unitDesign3 != null)
		{
			Diagnostics.LogError("Order preprocessing failed because the unit design to create already exist in user defined unit design list.");
			return false;
		}
		UnitDesign unitDesign2 = new UnitDesign("TemporaryUnitDesign");
		unitDesign2.UnitBodyDefinitionReference = new XmlNamedReference(order.UnitBodyDefinitionReference);
		Unit unit = this.CreateTemporaryUnit(this.temporaryGuid, unitDesign2);
		unit.SimulationObject.ModifierForward = ModifierForwardType.ChildrenOnly;
		this.temporaryGarrison.AddChild(unit);
		unit.Refresh(true);
		IDatabase<ItemDefinition> database = Databases.GetDatabase<ItemDefinition>(false);
		float propertyValue = unit.GetPropertyValue(SimulationProperties.AccessoriesSlotCount);
		int num = 0;
		for (int i = 0; i < order.UnitEquipmentSet.Slots.Length; i++)
		{
			UnitEquipmentSet.Slot slot = order.UnitEquipmentSet.Slots[i];
			StaticString key = slot.ItemName.ToString().Split(DepartmentOfDefense.ItemSeparators)[0];
			ItemDefinition constructibleElement;
			if (database.TryGetValue(key, out constructibleElement))
			{
				if (!DepartmentOfTheTreasury.CheckConstructiblePrerequisites(unit, constructibleElement, new string[]
				{
					"Prerequisite"
				}))
				{
					Diagnostics.LogError("Order preprocessing failed because the unit design to create contains a forbidden item: {0}.", new object[]
					{
						slot.ItemName
					});
					base.Empire.RemoveChild(unit);
					unit.Dispose();
					return false;
				}
				if (slot.SlotType == UnitEquipmentSet.AccessoryType)
				{
					if ((float)num >= propertyValue && !StaticString.IsNullOrEmpty(slot.ItemName))
					{
						Diagnostics.LogError("Order preprocessing failed because the unit design to create contains too much accessories: item {0} at accesory slot index {1}/{2}.", new object[]
						{
							slot.ItemName,
							num,
							propertyValue
						});
						return false;
					}
					num++;
				}
			}
		}
		this.temporaryGarrison.RemoveChild(unit);
		unit.Dispose();
		this.GenerateUnitEquipmentDefault(unitBodyDefinition.UnitEquipmentSet, order.UnitEquipmentSet);
		if (!this.IsUnitEquipmentSetValid(unitBodyDefinition, order.UnitEquipmentSet))
		{
			Diagnostics.LogError("Order preprocessing failed because the unit equipment set is not valid.");
			return false;
		}
		return true;
	}

	private IEnumerator CreateUnitDesignProcessor(OrderCreateUnitDesign order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		UnitBodyDefinition unitBodyDefinition = this.availableUnitBodyDefinitions.Find((UnitBodyDefinition body) => body.Name == order.UnitBodyDefinitionReference);
		if (unitBodyDefinition == null)
		{
			Diagnostics.LogError("Order processing failed because the unit body definition needed to create the unit design is not available.");
			yield break;
		}
		UnitDesign newUnitDesign = new UnitDesign(string.Format("UnitDesign{0}", order.UnitBodyDefinitionReference.ToString().Replace("UnitBody", string.Empty)));
		newUnitDesign.UnitBodyDefinitionReference = new XmlNamedReference(order.UnitBodyDefinitionReference);
		newUnitDesign.XmlSerializableUnitEquipmentSet = (order.UnitEquipmentSet.Clone() as UnitEquipmentSet);
		newUnitDesign.Model = order.UnitDesignModel;
		newUnitDesign.ModelRevision = 0u;
		newUnitDesign.UserDefinedName = order.UserDefinedName;
		if (unitBodyDefinition.Tags != null && unitBodyDefinition.Tags.Contains(DownloadableContent9.TagSolitary) && !newUnitDesign.Tags.Contains(DownloadableContent9.TagSolitary))
		{
			newUnitDesign.Tags.AddTag(DownloadableContent9.TagSolitary);
		}
		if (unitBodyDefinition.Tags != null && unitBodyDefinition.Tags.Contains(DownloadableContent9.TagColossus))
		{
			if (!newUnitDesign.Tags.Contains(DownloadableContent9.TagColossus))
			{
				newUnitDesign.Tags.AddTag(DownloadableContent9.TagColossus);
			}
			if (!newUnitDesign.Tags.Contains(DownloadableContent9.TagSolitary))
			{
				newUnitDesign.Tags.AddTag(DownloadableContent9.TagSolitary);
			}
		}
		if (unitBodyDefinition.Tags != null && unitBodyDefinition.Tags.Contains("Seafaring") && !newUnitDesign.Tags.Contains("Seafaring"))
		{
			newUnitDesign.Tags.AddTag("Seafaring");
		}
		if (unitBodyDefinition.Tags != null && unitBodyDefinition.Tags.Contains("BattleSideUnit") && !newUnitDesign.Tags.Contains("BattleSideUnit"))
		{
			newUnitDesign.Tags.AddTag("BattleSideUnit");
		}
		this.AddUnitDesign(newUnitDesign, order.UnitDesignModel, DepartmentOfDefense.UnitDesignState.Available);
		yield break;
	}

	private bool DestroyArmyPreprocessor(OrderDestroyArmy order)
	{
		if (!order.ArmyGuid.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the army guid is not valid.");
			return false;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity))
		{
			return false;
		}
		Army army = gameEntity as Army;
		if (army == null || !(army is IWorldPositionable))
		{
			Diagnostics.LogError("Order preprocessing failed because the attacker is either not an 'Army' or does not implement 'IWorldPositionable'.");
			return false;
		}
		if (base.Empire.GetAgency<DepartmentOfDefense>() == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the empire has no department of defense.");
			return false;
		}
		Garrison garrison = gameEntity as Garrison;
		if (garrison == null || garrison.IsInEncounter)
		{
			Diagnostics.LogError("Order preprocessing failed because the garrison is in an encounter.");
			return false;
		}
		return true;
	}

	private IEnumerator DestroyArmyProcessor(OrderDestroyArmy order)
	{
		Diagnostics.Log("Process order: {0}.", new object[]
		{
			order.ToString()
		});
		if (!order.ArmyGuid.IsValid)
		{
			Diagnostics.LogError("Order processing failed because the army guid is not valid.");
			yield break;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity))
		{
			Diagnostics.LogError("Order processing failed because the army is not referenced (guid = {0:X8}).", new object[]
			{
				order.ArmyGuid
			});
			yield break;
		}
		Army army = gameEntity as Army;
		if (army == null || !(army is IWorldPositionable))
		{
			Diagnostics.LogError("Order processing failed because the army is either not an 'Army' or does not implement 'IWorldPositionable'.");
			yield break;
		}
		while (army.StandardUnits.Count > 0)
		{
			Unit unit = army.StandardUnits[0];
			army.RemoveUnit(unit);
			this.GameEntityRepositoryService.Unregister(army);
			unit.Dispose();
		}
		this.RemoveArmy(army, true);
		yield break;
	}

	private IEnumerator DestroyMilitiaUnitsProcessor(OrderDestroyMilitiaUnits order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityGameEntityGUID, out gameEntity) || !(gameEntity is City))
		{
			Diagnostics.LogError("Cannot find the city where to destroy militia unit(s) from.");
			yield break;
		}
		for (int index = 0; index < order.GameEntityGUIDs.Length; index++)
		{
			Diagnostics.Assert(order.GameEntityGUIDs[index].IsValid);
			Unit unit = ((City)gameEntity).Militia.StandardUnits.FirstOrDefault((Unit u) => u.GUID == order.GameEntityGUIDs[index]);
			Diagnostics.Assert(unit != null);
			((City)gameEntity).Militia.RemoveUnit(unit);
			this.GameEntityRepositoryService.Unregister(unit);
			unit.Dispose();
		}
		yield break;
	}

	private bool DisbandArmyPreprocessor(OrderDisbandArmy order)
	{
		if (!order.ArmyGuid.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the army guid is not valid.");
			return false;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the army is not referenced (guid = {0:X8}).", new object[]
			{
				order.ArmyGuid
			});
			return false;
		}
		Army army = gameEntity as Army;
		if (army == null || !(army is IWorldPositionable))
		{
			Diagnostics.LogError("Order preprocessing failed because the attacker is either not an 'Army' or does not implement 'IWorldPositionable'.");
			return false;
		}
		DepartmentOfTheInterior agency = base.Empire.GetAgency<DepartmentOfTheInterior>();
		if (agency == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the empire has no department of the interior.");
			return false;
		}
		City city = null;
		for (int i = 0; i < agency.Cities.Count; i++)
		{
			if (agency.Cities[i].Districts.Any((District district) => district.WorldPosition == army.WorldPosition))
			{
				city = agency.Cities[i];
				break;
			}
		}
		if (city == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the army is not on a city tile.");
			return false;
		}
		Garrison garrison = gameEntity as Garrison;
		if (garrison == null || garrison.IsInEncounter)
		{
			Diagnostics.LogError("Order preprocessing failed because the garrison is in an encounter.");
			return false;
		}
		order.CityGuid = city.GUID;
		return true;
	}

	private IEnumerator DisbandArmyProcessor(OrderDisbandArmy order)
	{
		Diagnostics.Log("Process order: {0}.", new object[]
		{
			order.ToString()
		});
		if (!order.ArmyGuid.IsValid)
		{
			Diagnostics.LogError("Order processing failed because the army guid is not valid.");
			yield break;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity))
		{
			Diagnostics.LogError("Order processing failed because the army is not referenced (guid = {0:X8}).", new object[]
			{
				order.ArmyGuid
			});
			yield break;
		}
		Army army = gameEntity as Army;
		if (army == null || !(army is IWorldPositionable))
		{
			Diagnostics.LogError("Order processing failed because the army is either not an 'Army' or does not implement 'IWorldPositionable'.");
			yield break;
		}
		if (!order.CityGuid.IsValid)
		{
			Diagnostics.LogError("Order processing failed because the city guid is not valid.");
			yield break;
		}
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityGuid, out gameEntity))
		{
			Diagnostics.LogError("Order processing failed because the city is not referenced (guid = {0:X8}).", new object[]
			{
				order.CityGuid
			});
			yield break;
		}
		City city = gameEntity as City;
		if (city == null)
		{
			Diagnostics.LogError("Order processing failed because the city is not an 'City'.");
			yield break;
		}
		while (army.StandardUnits.Count > 0)
		{
			Unit unit = army.StandardUnits[0];
			army.RemoveUnit(unit);
			city.AddUnit(unit);
		}
		this.RemoveArmy(army, true);
		yield break;
	}

	private bool DisbandProducedUnitsPreprocessor(OrderDisbandProducedUnits order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ContextGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			return false;
		}
		if (order.ProducedUnitGuids == null || order.ProducedUnitGuids.Length == 0)
		{
			Diagnostics.LogError("Order preprocessing failed because the target produced units array is null or empty.");
			return false;
		}
		for (int i = 0; i < order.ProducedUnitGuids.Length; i++)
		{
			if (!this.GameEntityRepositoryService.TryGetValue(order.ProducedUnitGuids[i], out gameEntity))
			{
				Diagnostics.LogError("Order preprocessing failed because the produced unit entity guid at index {0} is not valid.", new object[]
				{
					i
				});
				return false;
			}
		}
		return true;
	}

	private IEnumerator DisbandProducedUnitsProcessor(OrderDisbandProducedUnits order)
	{
		IGameEntity context;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ContextGUID, out context))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			yield break;
		}
		List<Unit> producedUnits = null;
		if (this.producedUnitsPerContext.TryGetValue(context, out producedUnits))
		{
			for (int index = 0; index < order.ProducedUnitGuids.Length; index++)
			{
				this.GameEntityRepositoryService.Unregister(order.ProducedUnitGuids[index]);
				producedUnits.RemoveAll((Unit match) => match.GUID == order.ProducedUnitGuids[index]);
			}
		}
		yield break;
	}

	private bool DisbandUnitsPreprocessor(OrderDisbandUnits order)
	{
		if (order.DisbandedUnitGuids == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the unit array is null.");
			return false;
		}
		IGameEntity gameEntity = null;
		for (int i = 0; i < order.DisbandedUnitGuids.Length; i++)
		{
			if (!this.GameEntityRepositoryService.TryGetValue(order.DisbandedUnitGuids[i], out gameEntity))
			{
				Diagnostics.LogError("Order preprocessing failed because the unit entity guid at index {0} is not valid.", new object[]
				{
					i
				});
				return false;
			}
			Unit unit = gameEntity as Unit;
			if (unit.Garrison == null)
			{
				Diagnostics.LogError("Order preprocessing failed because the unit entity at index {0} is owned by nobody.", new object[]
				{
					i
				});
				return false;
			}
			if (unit.Garrison.IsInEncounter)
			{
				Diagnostics.LogError("Order preprocessing failed because the unit's garrison is in an encounter.");
				return false;
			}
			if (unit.SimulationObject.Tags.Contains("UnitTypeKaijusMilitia") || unit.SimulationObject.Tags.Contains("UnitTypeKaijusUnit") || unit.SimulationObject.Tags.Contains("UnitTypeKaijus"))
			{
				Diagnostics.LogError("Order preprocessing failed because can not disband unit from Kaiju!.");
				return false;
			}
		}
		return true;
	}

	private IEnumerator DisbandUnitsProcessor(OrderDisbandUnits order)
	{
		Diagnostics.Assert(order.DisbandedUnitGuids != null);
		IGameEntity gameEntity = null;
		Unit unit = null;
		List<Army> besiegingSeafaringArmies = DepartmentOfTheInterior.GetBesiegingSeafaringArmies(order.DisbandedUnitGuids);
		for (int index = 0; index < order.DisbandedUnitGuids.Length; index++)
		{
			if (this.GameEntityRepositoryService.TryGetValue(order.DisbandedUnitGuids[index], out gameEntity))
			{
				unit = (gameEntity as Unit);
				if (unit.Garrison != null)
				{
					IGarrison garrison = unit.Garrison;
					garrison.RemoveUnit(unit);
					if (garrison is Army && garrison.StandardUnits.Count == 0)
					{
						this.EventService.Notify(new EventArmyDisbanded(garrison.Empire, garrison.GUID));
						this.RemoveArmy(garrison as Army, true);
						if (besiegingSeafaringArmies != null)
						{
							besiegingSeafaringArmies.Remove(garrison as Army);
						}
					}
				}
				this.GameEntityRepositoryService.Unregister(unit);
				unit.Dispose();
			}
		}
		if (besiegingSeafaringArmies != null)
		{
			DepartmentOfTheInterior.CheckBesiegingSeafaringArmyStatus(besiegingSeafaringArmies, DepartmentOfTheInterior.BesiegingSeafaringArmyStatus.CityDefensePointLossPerTurn);
		}
		yield break;
	}

	private bool DismantleDeviceSucceedPreprocessor(OrderDismantleCreepingNodeSucceed order)
	{
		Army army = null;
		if (!this.GameEntityRepositoryService.TryGetValue<Army>(order.ArmyGuid, out army) || army == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the army game entity is not valid.");
			return false;
		}
		CreepingNode creepingNode = null;
		if (!this.GameEntityRepositoryService.TryGetValue<CreepingNode>(order.CreepingNodeGUID, out creepingNode) || creepingNode == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the CreepingNode game entity is not valid.");
			return false;
		}
		return true;
	}

	private IEnumerator DismantleCreepingNodeSucceedProcessor(OrderDismantleCreepingNodeSucceed order)
	{
		Army army = null;
		if (!this.GameEntityRepositoryService.TryGetValue<Army>(order.ArmyGuid, out army) || army == null)
		{
			Diagnostics.LogError("Order processing failed because the army game entity is not valid.");
			yield break;
		}
		CreepingNode creepingNode = null;
		if (!this.GameEntityRepositoryService.TryGetValue<CreepingNode>(order.CreepingNodeGUID, out creepingNode) || creepingNode == null)
		{
			Diagnostics.LogError("Order processing failed because the terraform CreepingNode game entity is not valid.");
			yield break;
		}
		DepartmentOfDefense instigatorDepartmentOfDefense = army.Empire.GetAgency<DepartmentOfDefense>();
		instigatorDepartmentOfDefense.StopDismantelingCreepingNode(army, creepingNode);
		if (!StaticString.IsNullOrEmpty(order.ArmyActionName))
		{
			ArmyAction armyAction = null;
			IDatabase<ArmyAction> armyActionDatabase = Databases.GetDatabase<ArmyAction>(false);
			if (armyActionDatabase.TryGetValue(order.ArmyActionName, out armyAction) && armyAction.ExperienceReward > 0f && army.Hero != null)
			{
				army.Hero.GainXp(armyAction.ExperienceReward, false, true);
			}
		}
		this.EventService.Notify(new EventDismantleCreepingNodeSucceed(army.Empire, army, creepingNode.WorldPosition));
		this.EventService.Notify(new EventDismantleCreepingNodeSuffered(creepingNode.Empire, army, creepingNode.WorldPosition));
		global::PlayerController serverPlayerController = army.Empire.PlayerControllers.Server;
		if (serverPlayerController != null)
		{
			OrderDestroyCreepingNode orderDestroyCreepingNode = new OrderDestroyCreepingNode(creepingNode.Empire.Index, creepingNode.GUID);
			serverPlayerController.PostOrder(orderDestroyCreepingNode);
		}
		yield break;
	}

	private bool DismantleDeviceSucceedPreprocessor(OrderDismantleDeviceSucceed order)
	{
		Army army = null;
		if (!this.GameEntityRepositoryService.TryGetValue<Army>(order.ArmyGuid, out army) || army == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the army game entity is not valid.");
			return false;
		}
		TerraformDevice terraformDevice = null;
		if (!this.GameEntityRepositoryService.TryGetValue<TerraformDevice>(order.DeviceGUID, out terraformDevice) || terraformDevice == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the terraform device game entity is not valid.");
			return false;
		}
		return true;
	}

	private IEnumerator DismantleDeviceSucceedProcessor(OrderDismantleDeviceSucceed order)
	{
		Army army = null;
		if (!this.GameEntityRepositoryService.TryGetValue<Army>(order.ArmyGuid, out army) || army == null)
		{
			Diagnostics.LogError("Order processing failed because the army game entity is not valid.");
			yield break;
		}
		TerraformDevice device = null;
		if (!this.GameEntityRepositoryService.TryGetValue<TerraformDevice>(order.DeviceGUID, out device) || device == null)
		{
			Diagnostics.LogError("Order processing failed because the terraform device game entity is not valid.");
			yield break;
		}
		DepartmentOfDefense instigatorDepartmentOfDefense = army.Empire.GetAgency<DepartmentOfDefense>();
		instigatorDepartmentOfDefense.StopDismantelingDevice(army, device);
		if (!StaticString.IsNullOrEmpty(order.ArmyActionName))
		{
			ArmyAction armyAction = null;
			IDatabase<ArmyAction> armyActionDatabase = Databases.GetDatabase<ArmyAction>(false);
			if (armyActionDatabase.TryGetValue(order.ArmyActionName, out armyAction) && armyAction.ExperienceReward > 0f && army.Hero != null)
			{
				army.Hero.GainXp(armyAction.ExperienceReward, false, true);
			}
		}
		device.LineOfSightDirty = true;
		this.EventService.Notify(new EventDismantleDeviceSucceed(army.Empire, army, device.WorldPosition));
		this.EventService.Notify(new EventDismantleDeviceSuffered(device.Empire, army, device.WorldPosition));
		ITerraformDeviceService terraformDeviceService = this.GameService.Game.Services.GetService<ITerraformDeviceService>();
		terraformDeviceService.DestroyDevice(device, army);
		yield break;
	}

	private bool EditHeroUnitDesignPreprocessor(OrderEditHeroUnitDesign order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		Diagnostics.Assert(this.hiddenUnitDesigns != null);
		UnitDesign currentDesign = this.hiddenUnitDesigns.Find((UnitDesign match) => match.Model == order.UnitDesignModel);
		if (currentDesign == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the unit design to edit does not exist in unit design list.");
			return false;
		}
		DepartmentOfEducation agency = base.Empire.GetAgency<DepartmentOfEducation>();
		if (agency == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the hero unit design to edit does not have an hero attached.");
			return false;
		}
		Unit unit = agency.Heroes.FirstOrDefault((Unit match) => match.UnitDesign.Model == currentDesign.Model);
		if (unit == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the hero unit design to edit does not have an hero attached.");
			return false;
		}
		if (DepartmentOfEducation.IsCaptured(unit))
		{
			return false;
		}
		UnitEquipmentSet defaultUnitEquipmentSet = unit.UnitDesign.DefaultUnitEquipmentSet;
		UnitEquipmentSet unitEquipmentSet = order.UnitEquipmentSet;
		this.GenerateUnitEquipmentDefault(defaultUnitEquipmentSet, unitEquipmentSet);
		if (!this.IsUnitEquipmentSetValid(currentDesign.UnitBodyDefinition, unitEquipmentSet))
		{
			Diagnostics.LogError("Order preprocessing failed because the unit equipment set is not valid.");
			return false;
		}
		UnitDesign unitDesign = unit.UnitDesign.Clone() as UnitDesign;
		unitDesign.UnitEquipmentSet = unitEquipmentSet;
		if (!this.IsUnitEquipmentSetValid(unitDesign.UnitBodyDefinition, unitDesign.UnitEquipmentSet))
		{
			Diagnostics.LogError("Order preprocessing failed because the unit equipment set is not valid.");
			return false;
		}
		if (!this.IsUnitEquipmentItemPrerequisitesValid(unitDesign, unit))
		{
			Diagnostics.LogError("Order preprocessing failed because the equipment is invalid.");
			return false;
		}
		IConstructionCost[] retrofitCosts = this.GetRetrofitCosts(unit, unitDesign);
		for (int i = 0; i < retrofitCosts.Length; i++)
		{
			float num = -retrofitCosts[i].GetValue(base.Empire);
			if (!this.departmentOfTheTreasury.IsTransferOfResourcePossible(base.Empire, retrofitCosts[i].ResourceName, ref num))
			{
				return false;
			}
		}
		DepartmentOfDefense.CheckRetrofitPrerequisitesResult checkRetrofitPrerequisitesResult = this.CheckRetrofitPrerequisites(unit, retrofitCosts);
		if (checkRetrofitPrerequisitesResult != DepartmentOfDefense.CheckRetrofitPrerequisitesResult.Ok)
		{
			return false;
		}
		order.RetrofitCosts = retrofitCosts;
		return true;
	}

	private IEnumerator EditHeroUnitDesignProcessor(OrderEditHeroUnitDesign order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		Diagnostics.Assert(this.hiddenUnitDesigns != null);
		UnitDesign currentDesign = this.hiddenUnitDesigns.Find((UnitDesign unitDesign) => unitDesign.Model == order.UnitDesignModel);
		if (currentDesign == null)
		{
			Diagnostics.LogError("Can't process order {0} because it's impossible to retrieve the unit design {1}.", new object[]
			{
				order,
				order.UnitDesignModel
			});
			yield break;
		}
		Unit hero = null;
		DepartmentOfEducation education = base.Empire.GetAgency<DepartmentOfEducation>();
		if (education != null)
		{
			hero = education.Heroes.FirstOrDefault((Unit match) => match.UnitDesign.Model == currentDesign.Model);
		}
		for (int index = 0; index < order.RetrofitCosts.Length; index++)
		{
			if (order.RetrofitCosts[index].Instant)
			{
				float resourceCost = order.RetrofitCosts[index].GetValue(base.Empire);
				if (!this.departmentOfTheTreasury.TryTransferResources(base.Empire, order.RetrofitCosts[index].ResourceName, -resourceCost))
				{
					Diagnostics.LogError("Cannot transfert the amount of resources (resource name = '{0}', cost = {0}).", new object[]
					{
						order.RetrofitCosts[index].ResourceName,
						-resourceCost
					});
				}
			}
		}
		if (hero != null)
		{
			DepartmentOfDefense.RemoveEquipmentSet(hero);
		}
		currentDesign.UnitEquipmentSet = (order.UnitEquipmentSet.Clone() as UnitEquipmentSet);
		currentDesign.ResetCaches();
		if (hero == null)
		{
			yield break;
		}
		hero.RetrofitTo(currentDesign);
		hero.UpdateExperienceReward(base.Empire);
		if (hero.Garrison is SimulationObjectWrapper)
		{
			(hero.Garrison as SimulationObjectWrapper).Refresh(false);
		}
		if (this.visibilityService != null)
		{
			this.visibilityService.NotifyVisibilityHasChanged((global::Empire)base.Empire);
			yield break;
		}
		yield break;
	}

	private bool EditUnitDesignPreprocessor(OrderEditUnitDesign order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		Diagnostics.Assert(this.availableUnitDesigns != null);
		UnitDesign unitDesign3 = this.availableUnitDesigns.Find((UnitDesign unitDesign) => unitDesign.Model == order.UnitDesignModel);
		if (unitDesign3 == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the unit design to edit does not exist in user defined unit design list.");
			return false;
		}
		order.UnitDesignModelRevision = unitDesign3.ModelRevision + 1u;
		this.GenerateUnitEquipmentDefault(unitDesign3.DefaultUnitEquipmentSet, order.UnitEquipmentSet);
		if (!this.IsUnitEquipmentSetValid(unitDesign3.UnitBodyDefinition, order.UnitEquipmentSet))
		{
			Diagnostics.LogError("Order preprocessing failed because the unit equipment set is not valid.");
			return false;
		}
		UnitDesign unitDesign2 = unitDesign3.Clone() as UnitDesign;
		unitDesign2.UnitEquipmentSet = order.UnitEquipmentSet;
		if (!this.IsUnitEquipmentItemPrerequisitesValid(unitDesign2, null))
		{
			Diagnostics.LogError("Order preprocessing failed because the equipment is invalid.");
			return false;
		}
		return true;
	}

	private IEnumerator EditUnitDesignProcessor(OrderEditUnitDesign order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		Diagnostics.Assert(this.availableUnitDesigns != null);
		UnitDesign currentDesign = this.availableUnitDesigns.Find((UnitDesign unitDesign) => unitDesign.Model == order.UnitDesignModel);
		if (currentDesign == null)
		{
			Diagnostics.LogError("Can't process order {0} because it's impossible to retrieve the unit design {1}.", new object[]
			{
				order,
				order.UnitDesignModel
			});
			yield break;
		}
		UnitDesign newDesign = currentDesign.Clone() as UnitDesign;
		newDesign.UnitEquipmentSet = (order.UnitEquipmentSet.Clone() as UnitEquipmentSet);
		newDesign.Model = order.UnitDesignModel;
		newDesign.ModelRevision = order.UnitDesignModelRevision;
		this.RemoveUnitDesign(currentDesign.Model, currentDesign.ModelRevision);
		this.AddUnitDesign(newDesign, newDesign.Model, DepartmentOfDefense.UnitDesignState.Available);
		yield break;
	}

	private bool EnablePortableForgeOnArmyPreprocessor(OrderEnablePortableForgeOnArmy order)
	{
		if (!order.ArmyGuid.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the army guid is not valid.");
			return false;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the army is not referenced (guid = {0:X8}).", new object[]
			{
				order.ArmyGuid
			});
			return false;
		}
		Army army = gameEntity as Army;
		if (army == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the army is not an 'Army'.");
			return false;
		}
		if (army.Hero == null)
		{
			Diagnostics.LogError("Army does not posses a hero unit.");
			return false;
		}
		if (order.NumberOfActionPointsToSpend < 0f)
		{
			ArmyAction armyAction = null;
			IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
			if (database != null && database.TryGetValue(ArmyAction_PortableForge.ReadOnlyName, out armyAction))
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
		return true;
	}

	private IEnumerator EnablePortableForgeOnArmyProcessor(OrderEnablePortableForgeOnArmy order)
	{
		if (!order.ArmyGuid.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the army guid is not valid.");
			yield break;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the army is not referenced (guid = {0:X8}).", new object[]
			{
				order.ArmyGuid
			});
			yield break;
		}
		Army army = gameEntity as Army;
		if (army == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the army is not an 'Army'.");
			yield break;
		}
		if (army.Hero == null)
		{
			Diagnostics.LogError("Army does not posses a hero unit.");
			yield break;
		}
		if (order.NumberOfActionPointsToSpend > 0f)
		{
			ArmyAction.SpendSomeNumberOfActionPoints(gameEntity, order.NumberOfActionPointsToSpend);
		}
		foreach (Unit unit in army.Units)
		{
			unit.SetPropertyBaseValue(SimulationProperties.MovementRatio, 0f);
			unit.Refresh(false);
		}
		army.Refresh(false);
		army.PortableForgeActive = true;
		yield break;
	}

	private bool ExecuteArmyActionPreprocessor(OrderExecuteArmyAction order)
	{
		Army army = null;
		if (!this.GameEntityRepositoryService.TryGetValue<Army>(order.ArmyGuid, out army))
		{
			Diagnostics.LogError("Order preprocessing failed because the army unit game entity is not valid.");
			return false;
		}
		IGameEntityWithWorldPosition gameEntityWithWorldPosition = null;
		if (!this.GameEntityRepositoryService.TryGetValue<IGameEntityWithWorldPosition>(order.TargetGUID, out gameEntityWithWorldPosition))
		{
			Diagnostics.LogError("Order preprocessing failed because the target unit game entity is not valid.");
			return false;
		}
		IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
		ArmyAction value = database.GetValue(order.ArmyActionName);
		if (value == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the army action is not valid!.");
			return false;
		}
		List<StaticString> list = new List<StaticString>();
		if (!value.CanExecute(army, ref list, new object[]
		{
			gameEntityWithWorldPosition
		}))
		{
			Diagnostics.LogWarning("Cannot execute the action '{0}' on target guid '{1}'", new object[]
			{
				order.ArmyActionName,
				order.TargetGUID
			});
			return false;
		}
		return true;
	}

	private IEnumerator ExecuteArmyActionProcessor(OrderExecuteArmyAction order)
	{
		Army army = null;
		if (!this.GameEntityRepositoryService.TryGetValue<Army>(order.ArmyGuid, out army))
		{
			Diagnostics.LogError("Skipping ExecuteArmyActionProcessor because army context is null.");
			yield break;
		}
		IGameEntityWithWorldPosition target = null;
		if (!this.GameEntityRepositoryService.TryGetValue<IGameEntityWithWorldPosition>(order.TargetGUID, out target))
		{
			Diagnostics.LogError("Skipping ExecuteArmyActionProcessor because target entity context is null.");
			yield break;
		}
		IDatabase<ArmyAction> armyActionDatabase = Databases.GetDatabase<ArmyAction>(false);
		ArmyAction armyAction = armyActionDatabase.GetValue(order.ArmyActionName);
		if (armyAction != null)
		{
			List<StaticString> failures = new List<StaticString>();
			if (!armyAction.CanExecute(army, ref failures, new object[]
			{
				target
			}))
			{
				Diagnostics.LogError("Skipping ExecuteArmyActionProcessor because army action can not be executed!");
				yield break;
			}
			armyAction.Execute(army, (base.Empire as global::Empire).PlayerControllers.Server, new object[]
			{
				target
			});
		}
		yield break;
	}

	private bool FortifyCityDefensesThroughArmyPreprocessor(OrderFortifyCityDefensesThroughArmy order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGUID, out gameEntity))
		{
			return false;
		}
		Army army = gameEntity as Army;
		if (army == null)
		{
			return false;
		}
		if (order.Amount <= 0f)
		{
			return false;
		}
		if (order.Amount > 1f)
		{
			order.Amount = 1f;
		}
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityGUID, out gameEntity))
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
		return true;
	}

	private IEnumerator FortifyCityDefensesThroughArmyProcessor(OrderFortifyCityDefensesThroughArmy order)
	{
		if (!order.ArmyGUID.IsValid)
		{
			Diagnostics.LogError("Game entity guid is not valid: {0}.", new object[]
			{
				order.ArmyGUID.ToString()
			});
			yield break;
		}
		Army army = this.GetArmy(order.ArmyGUID);
		if (army == null)
		{
			Diagnostics.LogError("Cannot find the army referenced by the given guid: {0}.", new object[]
			{
				order.ArmyGUID.ToString()
			});
			yield break;
		}
		ArmyAction armyAction = null;
		bool zeroMovement = true;
		if (!StaticString.IsNullOrEmpty(order.ArmyActionName))
		{
			IDatabase<ArmyAction> armyActionDatabase = Databases.GetDatabase<ArmyAction>(false);
			if (armyActionDatabase != null && armyActionDatabase.TryGetValue(order.ArmyActionName, out armyAction) && armyAction is IArmyActionWithMovementEffect)
			{
				zeroMovement = (armyAction as IArmyActionWithMovementEffect).ZeroMovement;
			}
		}
		if (zeroMovement)
		{
			foreach (Unit unit in army.Units)
			{
				unit.SetPropertyBaseValue(SimulationProperties.MovementRatio, 0f);
				unit.Refresh(false);
			}
		}
		if (order.NumberOfActionPointsToSpend > 0f)
		{
			ArmyAction.SpendSomeNumberOfActionPoints(army, order.NumberOfActionPointsToSpend);
		}
		if (order.ArmyActionCooldownDuration > 0f)
		{
			ArmyActionWithCooldown.ApplyCooldown(army, order.ArmyActionCooldownDuration);
		}
		army.Refresh(false);
		if (!order.CityGUID.IsValid)
		{
			yield break;
		}
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityGUID, out gameEntity))
		{
			yield break;
		}
		City cityToFortify = gameEntity as City;
		if (cityToFortify == null)
		{
			yield break;
		}
		if (cityToFortify.Empire == null)
		{
			yield break;
		}
		DepartmentOfTheInterior departmentOfTheInterior = cityToFortify.Empire.GetAgency<DepartmentOfTheInterior>();
		departmentOfTheInterior.FortifyCityByAmountInPercent(cityToFortify, order.Amount);
		if (armyAction != null)
		{
			army.OnArmyAction(armyAction, cityToFortify);
			yield break;
		}
		yield break;
	}

	private bool HealUnitsPreprocessor(OrderHealUnits order)
	{
		Unit[] array = new Unit[order.UnitGUIDs.Length];
		for (int i = 0; i < order.UnitGUIDs.Length; i++)
		{
			IGameEntity gameEntity;
			if (!this.GameEntityRepositoryService.TryGetValue(order.UnitGUIDs[i], out gameEntity))
			{
				Diagnostics.LogError("Order preprocessing failed because the target unit game entity is not valid (GUID = '{0}').", new object[]
				{
					order.UnitGUIDs[i]
				});
				return false;
			}
			if (!(gameEntity is Unit))
			{
				Diagnostics.LogError("Order preprocessing failed because the target game entity is not a unit (GUID = '{0}').", new object[]
				{
					order.UnitGUIDs[i]
				});
				return false;
			}
			array[i] = (gameEntity as Unit);
			if (array[i] != null && !array[i].HasEnoughActionPointLeft(1))
			{
				Diagnostics.LogWarning("Not enough action points.");
				return false;
			}
		}
		ConstructionCost[] unitHealCost = this.departmentOfTheTreasury.GetUnitHealCost(array);
		if (!this.departmentOfTheTreasury.CanAfford(unitHealCost))
		{
			Diagnostics.LogError("Order preprocessing failed because we haven't enough resources.");
			return false;
		}
		return true;
	}

	private IEnumerator HealUnitsProcessor(OrderHealUnits order)
	{
		Unit[] units = new Unit[order.UnitGUIDs.Length];
		for (int unitIndex = 0; unitIndex < order.UnitGUIDs.Length; unitIndex++)
		{
			IGameEntity gameEntity;
			if (!this.GameEntityRepositoryService.TryGetValue(order.UnitGUIDs[unitIndex], out gameEntity))
			{
				Diagnostics.LogError("Order processing failed because the target unit game entity is not valid (GUID = '{0}').", new object[]
				{
					order.UnitGUIDs[unitIndex]
				});
				yield break;
			}
			if (!(gameEntity is Unit))
			{
				Diagnostics.LogError("Order processing failed because the target game entity is not a unit (GUID = '{0}').", new object[]
				{
					order.UnitGUIDs[unitIndex]
				});
				yield break;
			}
			units[unitIndex] = (gameEntity as Unit);
			ArmyAction.SpendSomeNumberOfActionPoints(units[unitIndex], 1f);
		}
		ConstructionCost[] costs = this.departmentOfTheTreasury.GetUnitHealCost(units);
		for (int index = 0; index < costs.Length; index++)
		{
			float value = -costs[index].Value;
			if (!this.departmentOfTheTreasury.TryTransferResources(base.Empire, costs[index].ResourceName, value))
			{
				Diagnostics.LogError("Order processing failed because we haven't enough resources.");
				yield break;
			}
		}
		DepartmentOfDefense.HealUnits(units);
		yield break;
	}

	private bool HealUnitsThroughArmyPreprocessor(OrderHealUnitsThroughArmy order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGUID, out gameEntity))
		{
			return false;
		}
		Army army = gameEntity as Army;
		if (army == null)
		{
			return false;
		}
		if (order.Amount <= 0f)
		{
			return false;
		}
		if (order.Amount > 1f)
		{
			order.Amount = 1f;
		}
		Unit[] array = new Unit[order.UnitGUIDs.Length];
		for (int i = 0; i < order.UnitGUIDs.Length; i++)
		{
			if (!this.GameEntityRepositoryService.TryGetValue(order.UnitGUIDs[i], out gameEntity))
			{
				Diagnostics.LogWarning("Order preprocessing failed because the target unit game entity is not valid (GUID = '{0}').", new object[]
				{
					order.UnitGUIDs[i]
				});
				return false;
			}
			if (!(gameEntity is Unit))
			{
				Diagnostics.LogWarning("Order preprocessing failed because the target game entity is not a unit (GUID = '{0}').", new object[]
				{
					order.UnitGUIDs[i]
				});
				return false;
			}
			array[i] = (gameEntity as Unit);
		}
		if (order.Costs != null && !this.departmentOfTheTreasury.CanAfford(order.Costs))
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
		return true;
	}

	private IEnumerator HealUnitsThroughArmyProcessor(OrderHealUnitsThroughArmy order)
	{
		if (!order.ArmyGUID.IsValid)
		{
			Diagnostics.LogError("Game entity guid is not valid: {0}.", new object[]
			{
				order.ArmyGUID.ToString()
			});
			yield break;
		}
		Army army = this.GetArmy(order.ArmyGUID);
		if (army == null)
		{
			Diagnostics.LogError("Cannot find the army referenced by the given guid: {0}.", new object[]
			{
				order.ArmyGUID.ToString()
			});
			yield break;
		}
		ArmyAction armyAction = null;
		bool zeroMovement = true;
		if (!StaticString.IsNullOrEmpty(order.ArmyActionName))
		{
			IDatabase<ArmyAction> armyActionDatabase = Databases.GetDatabase<ArmyAction>(false);
			if (armyActionDatabase != null && armyActionDatabase.TryGetValue(order.ArmyActionName, out armyAction) && armyAction is IArmyActionWithMovementEffect)
			{
				zeroMovement = (armyAction as IArmyActionWithMovementEffect).ZeroMovement;
			}
		}
		if (zeroMovement)
		{
			foreach (Unit unit in army.Units)
			{
				unit.SetPropertyBaseValue(SimulationProperties.MovementRatio, 0f);
				unit.Refresh(false);
			}
		}
		if (order.NumberOfActionPointsToSpend > 0f)
		{
			ArmyAction.SpendSomeNumberOfActionPoints(army, order.NumberOfActionPointsToSpend);
		}
		if (order.ArmyActionCooldownDuration > 0f)
		{
			ArmyActionWithCooldown.ApplyCooldown(army, order.ArmyActionCooldownDuration);
		}
		army.Refresh(false);
		if (order.Costs != null)
		{
			for (int index = 0; index < order.Costs.Length; index++)
			{
				float value = -order.Costs[index].Value;
				this.departmentOfTheTreasury.TryTransferResources(base.Empire, order.Costs[index].ResourceName, value);
			}
		}
		if (order.UnitGUIDs == null)
		{
			yield break;
		}
		List<IGameEntityWithWorldPosition> worldPositionables = new List<IGameEntityWithWorldPosition>(order.UnitGUIDs.Length);
		for (int index2 = 0; index2 < order.UnitGUIDs.Length; index2++)
		{
			IGameEntity gameEntity;
			if (this.GameEntityRepositoryService.TryGetValue(order.UnitGUIDs[index2], out gameEntity) && gameEntity is Unit)
			{
				DepartmentOfDefense departmentOfDefense = (gameEntity as Unit).Garrison.Empire.GetAgency<DepartmentOfDefense>();
				departmentOfDefense.HealUnitByAmountInPercent(gameEntity as Unit, order.Amount);
				IGameEntityWithWorldPosition worldPositionable = (gameEntity as Unit).Garrison as IGameEntityWithWorldPosition;
				if (worldPositionable != null && !worldPositionables.Contains(worldPositionable))
				{
					worldPositionables.Add(worldPositionable);
				}
			}
		}
		if (armyAction == null)
		{
			yield break;
		}
		if (worldPositionables.Count > 0)
		{
			army.OnArmyAction(armyAction, worldPositionables[0]);
			yield break;
		}
		yield break;
	}

	private bool KaijuChangeModePreprocessor(OrderKaijuChangeMode order)
	{
		if (order.ConvertToGarrison && order.ConvertToArmy)
		{
			Diagnostics.LogError("Order preprocessing failed because can not convert Kaiju to both modes!");
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
		if (order.ConvertToGarrison && !kaiju.CanChangeToGarrisonMode())
		{
			Diagnostics.LogError("Order preprocessing failed because Kaiju can not change to Garrison Mode!");
			return false;
		}
		if (order.ConvertToArmy && !kaiju.CanChangeToArmyMode())
		{
			Diagnostics.LogError("Order preprocessing failed because Kaiju can not change to Army Mode!");
			return false;
		}
		if (order.CheckCosts)
		{
			float num = 0f;
			if (order.ConvertToGarrison)
			{
				ArmyAction armyAction = null;
				IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
				if (database != null && database.TryGetValue(ArmyAction_SettleKaiju.ReadOnlyName, out armyAction))
				{
					num = armyAction.GetCostInActionPoints();
					if (num > 0f && kaiju.KaijuArmy != null)
					{
						float propertyValue = kaiju.KaijuArmy.GetPropertyValue(SimulationProperties.MaximumNumberOfActionPoints);
						float propertyValue2 = kaiju.KaijuArmy.GetPropertyValue(SimulationProperties.ActionPointsSpent);
						if (order.NumberOfActionPointsToSpend > propertyValue - propertyValue2)
						{
							Diagnostics.LogWarning("Not enough action points.");
							return false;
						}
					}
				}
			}
			else if (order.ConvertToArmy)
			{
				GarrisonAction garrisonAction = null;
				IDatabase<GarrisonAction> database2 = Databases.GetDatabase<GarrisonAction>(false);
				if (database2 != null && database2.TryGetValue(GarrisonAction_RiseKaiju.ReadOnlyName, out garrisonAction))
				{
					num = garrisonAction.GetCostInActionPoints();
					if (!garrisonAction.CanAffordActionPoints(kaiju.KaijuGarrison))
					{
						Diagnostics.LogWarning("Not enough action points.");
						return false;
					}
				}
			}
			order.NumberOfActionPointsToSpend = num;
		}
		return true;
	}

	private IEnumerator KaijuChangeModeProcessor(OrderKaijuChangeMode order)
	{
		Kaiju kaiju = null;
		if (!this.GameEntityRepositoryService.TryGetValue<Kaiju>(order.KaijuGUID, out kaiju))
		{
			Diagnostics.LogError("Skipping Kaiju Change mode because Kaiju guid is null.");
			yield break;
		}
		if (order.ConvertToGarrison)
		{
			if (order.CheckCosts && order.NumberOfActionPointsToSpend > 0f)
			{
				ArmyAction.SpendSomeNumberOfActionPoints(kaiju.KaijuArmy, order.NumberOfActionPointsToSpend);
			}
			kaiju.ChangeToGarrisonMode(true);
		}
		else if (order.ConvertToArmy)
		{
			if (order.CheckCosts && order.NumberOfActionPointsToSpend > 0f)
			{
				GarrisonAction.SpendSomeNumberOfActionPoints(kaiju.KaijuGarrison, order.NumberOfActionPointsToSpend);
			}
			kaiju.ChangeToArmyMode(true);
		}
		yield break;
	}

	private bool LockTargetPreprocessor(OrderLockTarget order)
	{
		return order.GameEntityGUID.IsValid && order.TargetGameEntityGUID.IsValid;
	}

	private IEnumerator LockTargetProcessor(OrderLockTarget order)
	{
		IGameEntity gameEntity;
		if (this.GameEntityRepositoryService.TryGetValue(order.TargetGameEntityGUID, out gameEntity))
		{
			ILockableTarget lockableTarget = gameEntity as ILockableTarget;
			if (lockableTarget != null)
			{
				lockableTarget.Lock(order.GameEntityGUID, order.Locked);
			}
		}
		yield break;
	}

	private bool PillageSucceedPreprocessor(OrderPillageSucceed order)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			return false;
		}
		Army army = gameEntity as Army;
		if (!this.GameEntityRepositoryService.TryGetValue(order.PointOfInterestGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			return false;
		}
		PointOfInterest pointOfInterest = gameEntity as PointOfInterest;
		List<IDroppable> list = new List<IDroppable>();
		InterpreterContext interpreterContext = new InterpreterContext(pointOfInterest);
		interpreterContext.Register("PointOfInterest", pointOfInterest);
		interpreterContext.Register("Army", army);
		foreach (PillageRewardDefinition pillageRewardDefinition in this.PillageRewardDefinitionDatabase)
		{
			if (pillageRewardDefinition != null && pointOfInterest.SimulationObject.Tags.Contains(pillageRewardDefinition.PointOfInterestTags))
			{
				for (int i = 0; i < pillageRewardDefinition.Rewards.Length; i++)
				{
					IDroppable droppable = pillageRewardDefinition.Rewards[i].ComputeReward(army, interpreterContext);
					if (droppable != null)
					{
						list.Add(droppable);
					}
				}
			}
		}
		order.Droppables = list.ToArray();
		return true;
	}

	private IEnumerator PillageSucceedProcessor(OrderPillageSucceed order)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			yield break;
		}
		Army army = gameEntity as Army;
		if (!this.GameEntityRepositoryService.TryGetValue(order.PointOfInterestGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			yield break;
		}
		PointOfInterest pointOfInterest = gameEntity as PointOfInterest;
		float pointOfInterestPillageMaximumDefense = pointOfInterest.GetPropertyValue(SimulationProperties.MaximumPillageDefense);
		pointOfInterest.SetPropertyBaseValue(SimulationProperties.PillageDefense, pointOfInterestPillageMaximumDefense);
		DepartmentOfDefense.StopPillage(army, pointOfInterest);
		if (!StaticString.IsNullOrEmpty(order.ArmyActionName))
		{
			ArmyAction armyAction = null;
			IDatabase<ArmyAction> armyActionDatabase = Databases.GetDatabase<ArmyAction>(false);
			if (armyActionDatabase.TryGetValue(order.ArmyActionName, out armyAction) && armyAction.ExperienceReward > 0f && army.Hero != null)
			{
				army.Hero.GainXp(armyAction.ExperienceReward, false, true);
			}
		}
		SimulationDescriptor descriptor = null;
		if (this.SimulationDescriptorDatabase.TryGetValue(DepartmentOfDefense.PillageStatusDescriptor, out descriptor))
		{
			pointOfInterest.AddDescriptor(descriptor, false);
		}
		else
		{
			pointOfInterest.SimulationObject.Tags.AddTag(DepartmentOfDefense.PillageStatusDescriptor);
		}
		pointOfInterest.Refresh(false);
		pointOfInterest.SetPropertyBaseValue(SimulationProperties.PillageCooldown, 0f);
		if (order.Droppables != null)
		{
			for (int index = 0; index < order.Droppables.Length; index++)
			{
				IDroppableWithRewardAllocation droppableWithReward = order.Droppables[index] as IDroppableWithRewardAllocation;
				if (droppableWithReward != null)
				{
					droppableWithReward.AllocateRewardTo(army.Empire, new object[]
					{
						army
					});
				}
			}
		}
		pointOfInterest.LineOfSightDirty = true;
		if (pointOfInterest.Region.City != null)
		{
			City ownerCity = pointOfInterest.Region.City;
			DepartmentOfTheInterior ownerInterior = ownerCity.Empire.GetAgency<DepartmentOfTheInterior>();
			ownerInterior.VerifyOverallPopulation(ownerCity);
			ownerInterior.BindMinorFactionToCity(ownerCity, ownerCity.Region.MinorEmpire);
			ownerCity.Empire.Refresh(false);
			if (this.EventService != null && pointOfInterest.Region.City != null)
			{
				global::Empire pointOfInterestOwner = ownerCity.Empire;
				this.EventService.Notify(new EventPillageSuffered(pointOfInterestOwner, army, pointOfInterest));
			}
		}
		if (this.EventService != null)
		{
			this.EventService.Notify(new EventPillageSucceed(army.Empire, army, pointOfInterest, order.Droppables));
		}
		yield break;
	}

	private bool RemoveUnitDesignPreprocessor(OrderRemoveUnitDesign order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		Diagnostics.Assert(this.availableUnitDesigns != null);
		if (this.availableUnitDesigns.Find((UnitDesign unitDesign) => unitDesign.Model == order.UnitDesignModel) == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the unit design to edit does not exist in user defined unit design list.");
			return false;
		}
		return true;
	}

	private IEnumerator RemoveUnitDesignProcessor(OrderRemoveUnitDesign order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		Diagnostics.Assert(this.availableUnitDesigns != null);
		this.RemoveUnitDesign(order.UnitDesignModel, order.UnitDesignModelRevision);
		yield break;
	}

	private bool RetrofitUnitPreprocessor(OrderRetrofitUnit order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		Diagnostics.Assert(this.GameEntityRepositoryService != null);
		List<ConstructionCost> list = new List<ConstructionCost>();
		for (int i = 0; i < order.UnitGuids.Length; i++)
		{
			IGameEntity gameEntity;
			if (!this.GameEntityRepositoryService.TryGetValue(order.UnitGuids[i], out gameEntity))
			{
				Diagnostics.LogError("Order preprocessing fail because unit guid is not valid.");
				return false;
			}
			Unit unit = gameEntity as Unit;
			if (unit == null)
			{
				Diagnostics.LogError("Order preprocessing fail because the guid does not represent a unit.");
				return false;
			}
			Diagnostics.Assert(unit.UnitDesign != null);
			UnitDesign unitDesign = this.UnitDesignDatabase.UserDefinedUnitDesigns.FirstOrDefault((UnitDesign design) => design.Model == unit.UnitDesign.Model);
			if (unitDesign == null)
			{
				Diagnostics.LogError("Order preprocessing fail because it's impossible to retrieve the unit design model.");
				return false;
			}
			if (unitDesign.ModelRevision <= unit.UnitDesign.ModelRevision)
			{
				Diagnostics.LogError("Order preprocessing fail because the unit design model model revision is not newest.");
				return false;
			}
			ConstructionCost[] retrofitCosts = this.GetRetrofitCosts(unit, unitDesign);
			if (retrofitCosts != null && retrofitCosts.Length != 0)
			{
				DepartmentOfDefense.CheckRetrofitPrerequisitesResult checkRetrofitPrerequisitesResult = this.CheckRetrofitPrerequisites(unit, retrofitCosts);
				if (checkRetrofitPrerequisitesResult != DepartmentOfDefense.CheckRetrofitPrerequisitesResult.Ok)
				{
					return false;
				}
				int index;
				for (index = 0; index < retrofitCosts.Length; index++)
				{
					ConstructionCost constructionCost = list.Find((ConstructionCost match) => match.ResourceName == retrofitCosts[index].ResourceName);
					if (constructionCost != null)
					{
						constructionCost.Value += retrofitCosts[index].Value;
					}
					else
					{
						list.Add(retrofitCosts[index]);
					}
				}
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			float num = -list[j].Value;
			if (!this.departmentOfTheTreasury.IsTransferOfResourcePossible(base.Empire, list[j].ResourceName, ref num))
			{
				return false;
			}
		}
		order.RetrofitCosts = list.ToArray();
		return true;
	}

	private IEnumerator RetrofitUnitProcessor(OrderRetrofitUnit order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		Diagnostics.Assert(this.departmentOfTheTreasury != null);
		List<IGarrison> garrisons = new List<IGarrison>(1);
		for (int unitIndex = 0; unitIndex < order.UnitGuids.Length; unitIndex++)
		{
			IGameEntity gameEntity;
			if (!this.GameEntityRepositoryService.TryGetValue(order.UnitGuids[unitIndex], out gameEntity))
			{
				Diagnostics.LogError("Order processing fail because unit guid is not valid.");
				yield break;
			}
			Unit unit = gameEntity as Unit;
			if (unit == null)
			{
				Diagnostics.LogError("Order processing fail because the guid does not represent a unit.");
				yield break;
			}
			Diagnostics.Assert(unit.UnitDesign != null);
			UnitDesign newestUnitDesign = this.UnitDesignDatabase.UserDefinedUnitDesigns.FirstOrDefault((UnitDesign design) => design.Model == unit.UnitDesign.Model);
			if (newestUnitDesign == null)
			{
				Diagnostics.LogError("Order processing fail because it's impossible to retrieve the unit design model.");
				yield break;
			}
			if (newestUnitDesign.ModelRevision <= unit.UnitDesign.ModelRevision)
			{
				Diagnostics.LogError("Order processing fail because the unit design model model revision is not newest.");
				yield break;
			}
			DepartmentOfDefense.RemoveEquipmentSet(unit);
			unit.RetrofitTo(newestUnitDesign);
			unit.UpdateExperienceReward(base.Empire);
			unit.Refresh(true);
			if (this.EnableDetection && unit.Garrison != null && !garrisons.Contains(unit.Garrison))
			{
				garrisons.Add(unit.Garrison);
			}
		}
		if (this.EnableDetection)
		{
			for (int index = 0; index < garrisons.Count; index++)
			{
				Garrison garrison = garrisons[index] as Garrison;
				if (garrison != null)
				{
					garrison.Refresh(true);
					if (garrison is Army)
					{
						this.UpdateDetection((Army)garrison);
					}
				}
			}
		}
		if (this.visibilityService != null)
		{
			this.visibilityService.NotifyVisibilityHasChanged((global::Empire)base.Empire);
		}
		if (order.RetrofitCosts != null && order.RetrofitCosts.Length > 0)
		{
			for (int index2 = 0; index2 < order.RetrofitCosts.Length; index2++)
			{
				if (!this.departmentOfTheTreasury.TryTransferResources(base.Empire.SimulationObject, order.RetrofitCosts[index2].ResourceName, -order.RetrofitCosts[index2].GetValue(base.Empire)))
				{
					Diagnostics.LogError("Order processing failed because the unit retrofit ask for instant resource '{0}' that can't be retrieve.", new object[]
					{
						order.RetrofitCosts[index2].ResourceName
					});
					yield break;
				}
			}
		}
		if (order.EmpireIndex == base.Empire.Index)
		{
			IGuiService guiService = Services.GetService<IGuiService>();
			MetaPanelCity cityPanel = guiService.GetGuiPanel<MetaPanelCity>();
			if (cityPanel != null && cityPanel.IsVisible)
			{
				cityPanel.OnUnitRetrofited();
			}
		}
		yield break;
	}

	private bool SellUnitsPreprocessor(OrderSellUnits order)
	{
		if (order.UnitGUIDs == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the unit array is null.");
			return false;
		}
		IGameEntity gameEntity = null;
		for (int i = 0; i < order.UnitGUIDs.Length; i++)
		{
			if (!this.GameEntityRepositoryService.TryGetValue(order.UnitGUIDs[i], out gameEntity))
			{
				Diagnostics.LogError("Order preprocessing failed because the unit entity guid at index {0} is not valid.", new object[]
				{
					i
				});
				return false;
			}
			if (!(gameEntity is Unit))
			{
				Diagnostics.LogError("Order preprocessing failed because the unit entity at index {0} is not a unit.", new object[]
				{
					i
				});
				return false;
			}
			Unit unit = gameEntity as Unit;
			if (unit.Garrison.IsInEncounter)
			{
				Diagnostics.LogError("Order preprocessing failed because the unit's garrison is in an encounter.");
				return false;
			}
		}
		return true;
	}

	private IEnumerator SellUnitsProcessor(OrderSellUnits order)
	{
		Diagnostics.Assert(order.UnitGUIDs != null);
		IGameEntity gameEntity = null;
		Unit unit = null;
		for (int index = 0; index < order.UnitGUIDs.Length; index++)
		{
			if (this.GameEntityRepositoryService.TryGetValue(order.UnitGUIDs[index], out gameEntity))
			{
				unit = (gameEntity as Unit);
				if (unit.UnitDesign is UnitProfile)
				{
					DepartmentOfEducation education = base.Empire.GetAgency<DepartmentOfEducation>();
					education.InternalRemoveHero(unit);
				}
				else
				{
					if (unit.Garrison != null)
					{
						IGarrison garrison = unit.Garrison;
						garrison.RemoveUnit(unit);
						if (garrison.IsEmpty && garrison is Army)
						{
							this.RemoveArmy(garrison as Army, true);
						}
					}
					this.departmentOfTheTreasury.TryTransferResources(base.Empire, SimulationProperties.EmpireMoney, DepartmentOfDefense.UnitSellPrice);
					this.GameEntityRepositoryService.Unregister(unit);
				}
			}
		}
		yield break;
	}

	private bool SpawnArmiesPreprocessor(OrderSpawnArmies order)
	{
		DepartmentOfDefense.<SpawnArmiesPreprocessor>c__AnonStorey951 <SpawnArmiesPreprocessor>c__AnonStorey = new DepartmentOfDefense.<SpawnArmiesPreprocessor>c__AnonStorey951();
		<SpawnArmiesPreprocessor>c__AnonStorey.order = order;
		if (<SpawnArmiesPreprocessor>c__AnonStorey.order.WorldPositions == null || <SpawnArmiesPreprocessor>c__AnonStorey.order.WorldPositions.Length == 0)
		{
			Diagnostics.LogWarning("Order preprocessing failed because the array of world positions is not valid.");
			return false;
		}
		if (<SpawnArmiesPreprocessor>c__AnonStorey.order.UnitDescriptors == null || <SpawnArmiesPreprocessor>c__AnonStorey.order.UnitDescriptors.Length == 0)
		{
			Diagnostics.LogWarning("Order preprocessing failed because the array of unit descriptors is not valid.");
			return false;
		}
		int index;
		for (index = 0; index < <SpawnArmiesPreprocessor>c__AnonStorey.order.UnitDescriptors.Length; index++)
		{
			uint unitDesignModel = 0u;
			UnitDesign unitDesign;
			if (this.UnitDesignDatabase.TryGetValue(<SpawnArmiesPreprocessor>c__AnonStorey.order.UnitDescriptors[index].UnitDesignName, out unitDesign, false))
			{
				unitDesignModel = unitDesign.Model;
			}
			else
			{
				bool flag = false;
				for (int i = 0; i < index; i++)
				{
					if (<SpawnArmiesPreprocessor>c__AnonStorey.order.UnitDescriptors[i].UnitDesignName == <SpawnArmiesPreprocessor>c__AnonStorey.order.UnitDescriptors[index].UnitDesignName)
					{
						unitDesignModel = <SpawnArmiesPreprocessor>c__AnonStorey.order.UnitDescriptors[i].UnitDesignModel;
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					UnitDesign unitDesign2 = this.UnitDesignDatabase.GetAvailableUnitDesignsAsEnumerable().FirstOrDefault((UnitDesign iterator) => iterator.ModelRevision == 0u && iterator.Name == <SpawnArmiesPreprocessor>c__AnonStorey.order.UnitDescriptors[index].UnitDesignName);
					if (unitDesign2 != null)
					{
						unitDesignModel = unitDesign2.Model;
					}
					else if (!this.GenerateUnitDesignModelId(<SpawnArmiesPreprocessor>c__AnonStorey.order.UnitDescriptors[index].UnitDesignName, out unitDesignModel))
					{
						Diagnostics.LogWarning("Order preprocessing failed because GenerateUnitDesignModelId() failed.");
						return false;
					}
				}
			}
			<SpawnArmiesPreprocessor>c__AnonStorey.order.UnitDescriptors[index].UnitDesignModel = unitDesignModel;
		}
		List<WorldPosition> list = new List<WorldPosition>(<SpawnArmiesPreprocessor>c__AnonStorey.order.WorldPositions);
		for (int j = list.Count - 1; j >= 0; j--)
		{
			if (!DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(list[j], PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.Water))
			{
				list.RemoveAt(j);
			}
		}
		if (list.Count > 0)
		{
			<SpawnArmiesPreprocessor>c__AnonStorey.order.WorldPositions = list.ToArray();
			<SpawnArmiesPreprocessor>c__AnonStorey.order.ArmiesGUIDs = new GameEntityGUID[<SpawnArmiesPreprocessor>c__AnonStorey.order.WorldPositions.Length];
			for (int k = 0; k < <SpawnArmiesPreprocessor>c__AnonStorey.order.ArmiesGUIDs.Length; k++)
			{
				<SpawnArmiesPreprocessor>c__AnonStorey.order.ArmiesGUIDs[k] = this.GameEntityRepositoryService.GenerateGUID();
			}
			<SpawnArmiesPreprocessor>c__AnonStorey.order.UnitsGUIDs = new GameEntityGUID[<SpawnArmiesPreprocessor>c__AnonStorey.order.ArmiesGUIDs.Length * <SpawnArmiesPreprocessor>c__AnonStorey.order.UnitDescriptors.Length];
			for (int l = 0; l < <SpawnArmiesPreprocessor>c__AnonStorey.order.UnitsGUIDs.Length; l++)
			{
				<SpawnArmiesPreprocessor>c__AnonStorey.order.UnitsGUIDs[l] = this.GameEntityRepositoryService.GenerateGUID();
			}
			return true;
		}
		return false;
	}

	private IEnumerator SpawnArmiesProcessor(OrderSpawnArmies order)
	{
		Diagnostics.Log(order.ToString());
		Queue<GameEntityGUID> armiesGUIDs = new Queue<GameEntityGUID>(order.ArmiesGUIDs);
		Queue<GameEntityGUID> unitsGUIDs = new Queue<GameEntityGUID>(order.UnitsGUIDs);
		Queue<WorldPosition> worldPositions = new Queue<WorldPosition>(order.WorldPositions);
		if (armiesGUIDs.Count == 0 || armiesGUIDs.Count != worldPositions.Count || unitsGUIDs.Count != armiesGUIDs.Count * order.UnitDescriptors.Length)
		{
			Diagnostics.LogError("Order Spawn Armies has invalid parameters.");
			yield break;
		}
		IEventService eventService = Services.GetService<IEventService>();
		Diagnostics.Assert(eventService != null);
		SimulationDescriptor movementCapacitySailDescriptor;
		if (!this.SimulationDescriptorDatabase.TryGetValue(PathfindingContext.MovementCapacitySailDescriptor, out movementCapacitySailDescriptor))
		{
			Diagnostics.LogError("Couldn't retrieve movement capacity sail descriptor from the database.");
		}
		int maxArmiesPerFrame = order.MaxArmiesPerFrame;
		for (int armyIndex = 0; armyIndex < order.ArmiesGUIDs.Length; armyIndex++)
		{
			if (armiesGUIDs.Count <= 0)
			{
				Diagnostics.LogWarning("Armies GUIDs count is zero.");
				break;
			}
			DepartmentOfDefense.UnitDescriptor[] unitDescriptors = order.UnitDescriptors;
			for (int unitDescriptorIndex = 0; unitDescriptorIndex < unitDescriptors.Length; unitDescriptorIndex++)
			{
				unitDescriptors[unitDescriptorIndex].GameEntityGUID = unitsGUIDs.Dequeue();
			}
			Army army;
			bool armyCreationSucceded = this.CreateArmy(armiesGUIDs.Dequeue(), unitDescriptors, worldPositions.Dequeue(), out army, false, false);
			if (armyCreationSucceded)
			{
				this.GameEntityRepositoryService.Register(army);
				for (int armyTagIndex = 0; armyTagIndex < order.ArmyTags.Length; armyTagIndex++)
				{
					army.SimulationObject.Tags.AddTag(order.ArmyTags[armyTagIndex]);
				}
				bool isWaterMovementCapacity = this.PathfindingService.GetTileMovementCapacity(army.WorldPosition, (PathfindingFlags)0) == PathfindingMovementCapacity.Water;
				bool armySailCapacity = army.SimulationObject.Tags.Contains(PathfindingContext.MovementCapacitySailDescriptor);
				foreach (Unit unit in army.Units)
				{
					this.GameEntityRepositoryService.Register(unit);
					for (int unitTagIndex = 0; unitTagIndex < order.UnitsTags.Length; unitTagIndex++)
					{
						unit.SimulationObject.Tags.AddTag(order.UnitsTags[unitTagIndex]);
					}
					unit.Level = order.UnitsLevel;
					if (armySailCapacity && movementCapacitySailDescriptor != null && !unit.SimulationObject.Tags.Contains(PathfindingContext.MovementCapacitySailDescriptor))
					{
						unit.AddDescriptor(movementCapacitySailDescriptor, true);
					}
					if (!unit.SimulationObject.Tags.Contains(DownloadableContent16.SeafaringUnit))
					{
						unit.SwitchToEmbarkedUnit(isWaterMovementCapacity);
					}
					unit.Refresh(true);
					if (unit.UnitDesign.Tags.Contains(DepartmentOfTheInterior.TagConvertedVillageUnit))
					{
						eventService.Notify(new EventVillageUnitSpawned(army.Empire, unit));
					}
				}
				army.Refresh(false);
				global::PlayerController serverPlayerController = army.Empire.PlayerControllers.Server;
				if (serverPlayerController != null)
				{
					QuestArmyObjective armyObjective = new QuestArmyObjective(order.ArmiesBehaviour);
					OrderUpdateArmyObjective updateArmyObjectiveOrder = new OrderUpdateArmyObjective(order.EmpireIndex, army.GUID, armyObjective);
					serverPlayerController.PostOrder(updateArmyObjectiveOrder);
					Diagnostics.Log("Posting order: {0}.", new object[]
					{
						updateArmyObjectiveOrder.ToString()
					});
				}
				if (army.IsNavalOrPartiallySo && this.WorldPositionningService.IsWaterTile(army.WorldPosition))
				{
					army.SetSails();
					if (army.IsNaval)
					{
						Region region = this.WorldPositionningService.GetRegion(army.WorldPosition);
						if (region.City != null)
						{
							bool incommingArmy = false;
							for (int districtIndex = 0; districtIndex < region.City.Districts.Count; districtIndex++)
							{
								if (region.City.Districts[districtIndex].WorldPosition == army.WorldPosition)
								{
									incommingArmy = true;
									break;
								}
							}
							if (incommingArmy)
							{
								float cityDefensePointLossPerTurn = 0f;
								foreach (Unit unit2 in army.Units)
								{
									cityDefensePointLossPerTurn += unit2.GetPropertyValue(SimulationProperties.CityDefensePointLossPerTurn);
								}
								if (cityDefensePointLossPerTurn > 0f)
								{
									bool besiege = false;
									if (region.City.BesiegingEmpireIndex == army.Empire.Index)
									{
										besiege = true;
									}
									else if (this.departmentOfForeignAffairs.IsAtWarWith(region.City.Empire))
									{
										besiege = true;
									}
									else
									{
										for (int besiegingArmyIndex = 0; besiegingArmyIndex < region.City.BesiegingSeafaringArmies.Count; besiegingArmyIndex++)
										{
											if (region.City.BesiegingSeafaringArmies[besiegingArmyIndex].Empire.Index == army.Empire.Index)
											{
												besiege = true;
												break;
											}
										}
									}
									if (besiege)
									{
										DepartmentOfTheInterior departmentOfTheInterior = region.City.Empire.GetAgency<DepartmentOfTheInterior>();
										departmentOfTheInterior.StartNavalSiege(region.City, army);
									}
								}
							}
						}
					}
				}
			}
			bool waitAFrame = maxArmiesPerFrame > 0 && armiesGUIDs.Count > 0 && (float)(armyIndex + 1) % (float)maxArmiesPerFrame == 0f;
			if (waitAFrame)
			{
				yield return null;
			}
		}
		yield break;
	}

	private bool SpawnArmyPreprocessor(OrderSpawnArmy orderGod)
	{
		DepartmentOfDefense.<SpawnArmyPreprocessor>c__AnonStorey953 <SpawnArmyPreprocessor>c__AnonStorey = new DepartmentOfDefense.<SpawnArmyPreprocessor>c__AnonStorey953();
		<SpawnArmyPreprocessor>c__AnonStorey.orderGod = orderGod;
		if (<SpawnArmyPreprocessor>c__AnonStorey.orderGod.UnitDescriptors == null)
		{
			Diagnostics.LogWarning("Order preprocessing failed because the array of unit descriptors is null.");
			return false;
		}
		if (<SpawnArmyPreprocessor>c__AnonStorey.orderGod.UnitDescriptors.Length == 0)
		{
			Diagnostics.LogWarning("Order preprocessing failed because the array of unit descriptors is empty.");
			return false;
		}
		if (<SpawnArmyPreprocessor>c__AnonStorey.orderGod.Level < 0)
		{
			Diagnostics.LogWarning("Order preprocessing failed because the level is < 0.");
			return false;
		}
		if (<SpawnArmyPreprocessor>c__AnonStorey.orderGod.XP < 0)
		{
			Diagnostics.LogWarning("Order preprocessing failed because the XP is < 0.");
			return false;
		}
		int index;
		for (index = 0; index < <SpawnArmyPreprocessor>c__AnonStorey.orderGod.UnitDescriptors.Length; index++)
		{
			uint unitDesignModel = 0u;
			UnitDesign unitDesign;
			if (this.UnitDesignDatabase.TryGetValue(<SpawnArmyPreprocessor>c__AnonStorey.orderGod.UnitDescriptors[index].UnitDesignName, out unitDesign, false))
			{
				unitDesignModel = unitDesign.Model;
			}
			else
			{
				bool flag = false;
				for (int i = 0; i < index; i++)
				{
					if (<SpawnArmyPreprocessor>c__AnonStorey.orderGod.UnitDescriptors[i].UnitDesignName == <SpawnArmyPreprocessor>c__AnonStorey.orderGod.UnitDescriptors[index].UnitDesignName)
					{
						unitDesignModel = <SpawnArmyPreprocessor>c__AnonStorey.orderGod.UnitDescriptors[i].UnitDesignModel;
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					UnitDesign unitDesign2 = this.UnitDesignDatabase.GetAvailableUnitDesignsAsEnumerable().FirstOrDefault((UnitDesign iterator) => (!<SpawnArmyPreprocessor>c__AnonStorey.orderGod.OnlyDefaultUnitDesign || iterator.ModelRevision == 0u) && iterator.Name == <SpawnArmyPreprocessor>c__AnonStorey.orderGod.UnitDescriptors[index].UnitDesignName);
					if (unitDesign2 != null)
					{
						unitDesignModel = unitDesign2.Model;
					}
					else if (!this.GenerateUnitDesignModelId(<SpawnArmyPreprocessor>c__AnonStorey.orderGod.UnitDescriptors[index].UnitDesignName, out unitDesignModel))
					{
						Diagnostics.LogWarning("Order preprocessing failed because GenerateUnitDesignModelId failed.");
						return false;
					}
				}
			}
			<SpawnArmyPreprocessor>c__AnonStorey.orderGod.UnitDescriptors[index].UnitDesignModel = unitDesignModel;
		}
		for (int j = 0; j < <SpawnArmyPreprocessor>c__AnonStorey.orderGod.WantedWorldPositions.Length; j++)
		{
			if (DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(<SpawnArmyPreprocessor>c__AnonStorey.orderGod.WantedWorldPositions[j], PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.Water))
			{
				<SpawnArmyPreprocessor>c__AnonStorey.orderGod.ValidatedWorldPosition = <SpawnArmyPreprocessor>c__AnonStorey.orderGod.WantedWorldPositions[j];
				break;
			}
		}
		if (!<SpawnArmyPreprocessor>c__AnonStorey.orderGod.ValidatedWorldPosition.IsValid)
		{
			return false;
		}
		if (<SpawnArmyPreprocessor>c__AnonStorey.orderGod.GameEntityGUID == GameEntityGUID.Zero)
		{
			<SpawnArmyPreprocessor>c__AnonStorey.orderGod.GameEntityGUID = this.GameEntityRepositoryService.GenerateGUID();
		}
		for (int k = 0; k < <SpawnArmyPreprocessor>c__AnonStorey.orderGod.UnitDescriptors.Length; k++)
		{
			<SpawnArmyPreprocessor>c__AnonStorey.orderGod.UnitDescriptors[k].GameEntityGUID = this.GameEntityRepositoryService.GenerateGUID();
		}
		return true;
	}

	private IEnumerator SpawnArmyProcessor(OrderSpawnArmy orderGod)
	{
		Diagnostics.Log(orderGod.ToString());
		if (orderGod.ValidatedWorldPosition == WorldPosition.Invalid)
		{
			Diagnostics.LogWarning("Order preprocessing failed because the target world position is not valid.");
			yield break;
		}
		Army army;
		if (!this.CreateArmy(orderGod.GameEntityGUID, orderGod.UnitDescriptors, orderGod.ValidatedWorldPosition, out army, orderGod.OnlyDefaultUnitDesign, false))
		{
			yield break;
		}
		IEventService eventService = Services.GetService<IEventService>();
		Diagnostics.Assert(eventService != null);
		if (army != null)
		{
			army.IsFromGlobalQuest = orderGod.IsFromGlobalQuest;
			for (int armyTagIndex = 0; armyTagIndex < orderGod.ArmyTags.Length; armyTagIndex++)
			{
				StaticString armytTag = orderGod.ArmyTags[armyTagIndex];
				if (!StaticString.IsNullOrEmpty(armytTag) && !army.SimulationObject.Tags.Contains(armytTag))
				{
					army.SimulationObject.Tags.AddTag(armytTag);
				}
			}
			bool waterTile = this.PathfindingService.GetTileMovementCapacity(army.WorldPosition, (PathfindingFlags)0) == PathfindingMovementCapacity.Water;
			foreach (Unit unit2 in army.Units)
			{
				this.GameEntityRepositoryService.Register(unit2);
				unit2.Level = orderGod.Level;
				unit2.GainXp((float)orderGod.XP, true, true);
				unit2.Refresh(true);
				unit2.UpdateExperienceReward(base.Empire);
				for (int unitTagIndex = 0; unitTagIndex < orderGod.UnitsTags.Length; unitTagIndex++)
				{
					StaticString unitTag = orderGod.UnitsTags[unitTagIndex];
					if (!StaticString.IsNullOrEmpty(unitTag) && !unit2.SimulationObject.Tags.Contains(unitTag))
					{
						unit2.SimulationObject.Tags.AddTag(unitTag);
					}
				}
				SimulationDescriptor simulationDescriptor;
				if (army.SimulationObject.Tags.Contains(PathfindingContext.MovementCapacitySailDescriptor) && !unit2.SimulationObject.Tags.Contains(PathfindingContext.MovementCapacitySailDescriptor) && this.SimulationDescriptorDatabase != null && this.SimulationDescriptorDatabase.TryGetValue(PathfindingContext.MovementCapacitySailDescriptor, out simulationDescriptor))
				{
					unit2.AddDescriptor(simulationDescriptor, true);
				}
				if (!unit2.SimulationObject.Tags.Contains(DownloadableContent16.SeafaringUnit))
				{
					unit2.SwitchToEmbarkedUnit(waterTile);
				}
				if (!orderGod.CanMoveOnSpawn)
				{
					unit2.SetPropertyBaseValue(SimulationProperties.MovementRatio, 0f);
				}
				if (unit2.UnitDesign.Tags.Contains(DepartmentOfTheInterior.TagConvertedVillageUnit))
				{
					eventService.Notify(new EventVillageUnitSpawned(army.Empire, unit2));
				}
			}
			this.GameEntityRepositoryService.Register(army);
			eventService.Notify(new EventArmySpawned(army));
			if (!orderGod.HaveActionPointOnSpawn)
			{
				float maximumNumberOfActionPoints = army.GetPropertyValue(SimulationProperties.MaximumNumberOfActionPoints);
				army.SetPropertyBaseValue(SimulationProperties.ActionPointsSpent, maximumNumberOfActionPoints);
			}
			IWorldPositionningService worldPositionningService = this.GameService.Game.Services.GetService<IWorldPositionningService>();
			if (army.IsNavalOrPartiallySo && worldPositionningService != null && worldPositionningService.IsWaterTile(army.WorldPosition))
			{
				army.SetSails();
				if (army.IsNaval)
				{
					Region region = worldPositionningService.GetRegion(army.WorldPosition);
					if (region != null && region.City != null && region.City.Districts.Any((District match) => match.WorldPosition == army.WorldPosition))
					{
						float cityDefensePointLossPerTurn = army.Units.Sum((Unit unit) => unit.GetPropertyValue(SimulationProperties.CityDefensePointLossPerTurn));
						if (cityDefensePointLossPerTurn > 0f)
						{
							bool besiege = false;
							if (region.City.BesiegingSeafaringArmies.Exists((Army besiegingSeafaringArmy) => besiegingSeafaringArmy.Empire.Index == army.Empire.Index))
							{
								besiege = true;
							}
							else if (region.City.BesiegingEmpireIndex == army.Empire.Index)
							{
								besiege = true;
							}
							else if (this.departmentOfForeignAffairs != null && this.departmentOfForeignAffairs.IsAtWarWith(region.City.Empire))
							{
								besiege = true;
							}
							if (besiege)
							{
								DepartmentOfTheInterior departmentOfTheInterior = region.City.Empire.GetAgency<DepartmentOfTheInterior>();
								if (departmentOfTheInterior != null)
								{
									departmentOfTheInterior.StartNavalSiege(region.City, army);
								}
							}
						}
					}
				}
			}
			army.Refresh(true);
			yield break;
		}
		yield break;
	}

	private bool SpawnUnitPreprocessor(OrderSpawnUnit order)
	{
		if (StaticString.IsNullOrEmpty(order.UnitDesignName))
		{
			Diagnostics.LogWarning("Order preprocessing failed because UnitDesignName is empty.");
			return false;
		}
		IDatabase<UnitDesign> database = Databases.GetDatabase<UnitDesign>(false);
		if (!database.ContainsKey(order.UnitDesignName))
		{
			Diagnostics.LogWarning("Order preprocessing failed because unit design '{0}' isn't in the database.", new object[]
			{
				order.UnitDesignName
			});
			return false;
		}
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.DestinationGarrisonGUID, out gameEntity))
		{
			Diagnostics.LogWarning("Order preprocessing failed because destination GUID {0} doesn't exists.", new object[]
			{
				order.DestinationGarrisonGUID
			});
			return false;
		}
		if (!(gameEntity is Garrison))
		{
			Diagnostics.LogWarning("Order preprocessing failed because destination GUID {0} is not a garrison.", new object[]
			{
				order.DestinationGarrisonGUID
			});
			return false;
		}
		Garrison garrison = gameEntity as Garrison;
		if (garrison.UnitsCount >= garrison.MaximumUnitSlot && !order.AllowGarrisonOverflow)
		{
			Diagnostics.LogWarning("Order preprocessing failed because destination garrison {0} is full.", new object[]
			{
				order.DestinationGarrisonGUID
			});
			return false;
		}
		if (garrison.IsInEncounter)
		{
			Diagnostics.LogWarning("Order preprocessing failed because destination garrison {0} is in encounter.", new object[]
			{
				order.DestinationGarrisonGUID
			});
			return false;
		}
		if (order.Level < 0)
		{
			Diagnostics.LogWarning("Order preprocessing failed because the the level is < 0.");
			return false;
		}
		UnitDesign unitDesign;
		if (base.Empire is MinorEmpire && gameEntity is Village && ((Village)gameEntity).HasBeenConverted && database.TryGetValue(order.UnitDesignName, out unitDesign) && !unitDesign.Tags.Contains(DepartmentOfTheInterior.TagConvertedVillageUnit))
		{
			Diagnostics.LogWarning("Order preprocessing failed because the unit design '{0}' is not compatible with the village's conversion status.", new object[]
			{
				order.UnitDesignName
			});
			return false;
		}
		if (order.UnitGUID == GameEntityGUID.Zero)
		{
			order.UnitGUID = this.GameEntityRepositoryService.GenerateGUID();
		}
		return true;
	}

	private IEnumerator SpawnUnitProcessor(OrderSpawnUnit order)
	{
		Unit unit = this.CreateUnitByDesignName(order.UnitGUID, order.UnitDesignName);
		if (unit == null)
		{
			yield break;
		}
		this.GameEntityRepositoryService.Register(unit);
		unit.Level = order.Level;
		unit.Refresh(true);
		unit.UpdateExperienceReward(base.Empire);
		IGameEntity entity;
		if (this.GameEntityRepositoryService.TryGetValue(order.DestinationGarrisonGUID, out entity) && entity is Garrison)
		{
			Garrison garrison = entity as Garrison;
			garrison.AddUnit(unit);
		}
		if (!order.ExperienceRewardAtCreation)
		{
			yield break;
		}
		float xpAtCreation = unit.GetPropertyValue(SimulationProperties.UnitExperienceRewardAtCreation);
		if (xpAtCreation > 0f)
		{
			unit.GainXp(xpAtCreation, false, true);
			yield break;
		}
		yield break;
	}

	private bool ToggleAspiratePreprocessor(OrderToggleAspirate order)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			return false;
		}
		PointOfInterest pointOfInterest = null;
		if (this.GameEntityRepositoryService.TryGetValue(order.PointOfInterestGuid, out gameEntity))
		{
			pointOfInterest = (gameEntity as PointOfInterest);
		}
		if (order.State)
		{
			if (pointOfInterest == null)
			{
				return false;
			}
			if (pointOfInterest.SimulationObject.Tags.Contains("ExploitedPointOfInterest"))
			{
				return false;
			}
		}
		return true;
	}

	private IEnumerator ToggleAspirateProcessor(OrderToggleAspirate order)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			yield break;
		}
		Army army = gameEntity as Army;
		PointOfInterest pointOfInterest = null;
		if (this.GameEntityRepositoryService.TryGetValue(order.PointOfInterestGuid, out gameEntity))
		{
			pointOfInterest = (gameEntity as PointOfInterest);
		}
		if (order.State && pointOfInterest != null)
		{
			this.StartAspirate(army, pointOfInterest);
		}
		else
		{
			this.StopAspirating(army);
		}
		this.OnArmyActionStateChange(army);
		yield break;
	}

	internal void ToggleAutoExplore(Army army, bool state)
	{
		if (army == null)
		{
			throw new ArgumentNullException("army");
		}
		if (army.Empire == null || army.Empire.Index != base.Empire.Index)
		{
			return;
		}
		if (state != army.IsAutoExploring)
		{
			army.IsAutoExploring = state;
			if (army.IsGuarding)
			{
				army.IsGuarding = false;
			}
			this.OnArmyActionStateChange(army);
		}
	}

	private bool ToggleAutoExplorePreprocessor(OrderToggleAutoExplore order)
	{
		if (!order.ArmyGuid.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the army guid is not valid.");
			return false;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the army is not referenced (guid: {0:X8}).", new object[]
			{
				order.ArmyGuid
			});
			return false;
		}
		if (!(gameEntity is Army))
		{
			Diagnostics.LogError("Order preprocessing failed because the army is not an 'Army'.");
			return false;
		}
		return true;
	}

	private IEnumerator ToggleAutoExploreProcessor(OrderToggleAutoExplore order)
	{
		if (!order.ArmyGuid.IsValid)
		{
			Diagnostics.LogError("Order processing failed because the army guid is not valid.");
			yield break;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity))
		{
			Diagnostics.LogError("Order processing failed because the army is not referenced (guid = {0:X8}).", new object[]
			{
				order.ArmyGuid
			});
			yield break;
		}
		Army army = gameEntity as Army;
		if (army == null)
		{
			Diagnostics.LogError("Order processing failed because the army is not an 'Army'.");
			yield break;
		}
		this.ToggleAutoExplore(army, order.State);
		yield break;
	}

	public static ConstructionCost[] GetCatspawCost(Amplitude.Unity.Game.Empire empire, Army army)
	{
		if (DepartmentOfDefense.catsPawInterpreterContext == null)
		{
			DepartmentOfDefense.catsPawInterpreterContext = new InterpreterContext(null);
			string value = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<string>("Gameplay/Agencies/DepartmentOfDefense/CatspawCostFormula");
			DepartmentOfDefense.catsPawFormulaTokens = Interpreter.InfixTransform(value);
		}
		DepartmentOfDefense.catsPawInterpreterContext.SimulationObject = empire.SimulationObject;
		DepartmentOfDefense.catsPawInterpreterContext.Register("ArmyCatspawCost", army.SimulationObject.GetPropertyValue(DepartmentOfDefense.ReadOnlyCatspawCostModifier));
		float value2 = (float)Interpreter.Execute(DepartmentOfDefense.catsPawFormulaTokens, DepartmentOfDefense.catsPawInterpreterContext);
		return new ConstructionCost[]
		{
			new ConstructionCost(DepartmentOfTheTreasury.Resources.EmpirePoint, value2, true, false)
		};
	}

	private void ToggleCatspaw(Army army, bool state)
	{
		if (army == null)
		{
			throw new ArgumentNullException("army");
		}
		if (army.Empire == null)
		{
			return;
		}
		DepartmentOfDefense departmentOfDefense = null;
		if (state)
		{
			departmentOfDefense = this;
			army.CatspawOriginalEmpireIndex = army.Empire.Index;
		}
		else
		{
			departmentOfDefense = (this.GameService.Game as global::Game).Empires[army.CatspawOriginalEmpireIndex].GetAgency<DepartmentOfDefense>();
		}
		if (departmentOfDefense == null)
		{
			Diagnostics.LogError("Fail retrieving department of defense.");
			return;
		}
		DepartmentOfDefense agency = army.Empire.GetAgency<DepartmentOfDefense>();
		if (agency == null)
		{
			Diagnostics.LogError("In method ToggleCatspaw army previous empire department of defense can't be null.");
			return;
		}
		agency.ReleaseArmy(army);
		foreach (Unit unit in army.Units)
		{
			if (unit.IsHero())
			{
				Diagnostics.LogError("Catspawed army should not be composed of hero.");
				return;
			}
			departmentOfDefense.AddCatspawUnitDesignAndRetrofit(unit, state);
		}
		army.SetPrivaters(state);
		army.SetCatspaw(state);
		if (base.Empire is MajorEmpire)
		{
			army.ClientLocalizedName = this.GenerateArmyClientLocalizedName(army);
		}
		army.Refresh(false);
		departmentOfDefense.AddArmy(army);
		this.visibilityService.NotifyVisibilityHasChanged(base.Empire as global::Empire);
	}

	private bool ToggleCatspawPreprocessor(OrderToggleCatspaw order)
	{
		if (!order.ArmyGuid.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the army guid is not valid.");
			return false;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity))
		{
			Diagnostics.LogWarning("Order preprocessing failed because the army is not referenced (guid: {0:X8}).", new object[]
			{
				order.ArmyGuid
			});
			return false;
		}
		Army army = gameEntity as Army;
		if (army == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the army is not an 'Army'.");
			return false;
		}
		if (order.Activate && army.Hero != null)
		{
			return false;
		}
		if (order.Activate == army.Empire is MajorEmpire)
		{
			return false;
		}
		if (order.Activate && !(army.Empire is MinorEmpire) && !(army.Empire is NavalEmpire))
		{
			return false;
		}
		if (order.Activate == army.HasCatspaw)
		{
			return false;
		}
		if (order.Activate)
		{
			order.CatspawCost = DepartmentOfDefense.GetCatspawCost((global::Empire)base.Empire, army);
			for (int i = 0; i < order.CatspawCost.Length; i++)
			{
				float num = -order.CatspawCost[i].GetValue(base.Empire);
				if (!this.departmentOfTheTreasury.IsTransferOfResourcePossible(base.Empire, order.CatspawCost[i].ResourceName, ref num))
				{
					return false;
				}
			}
		}
		return true;
	}

	private IEnumerator ToggleCatspawProcessor(OrderToggleCatspaw order)
	{
		if (!order.ArmyGuid.IsValid)
		{
			Diagnostics.LogError("Order processing failed because the army guid is not valid.");
			yield break;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity))
		{
			Diagnostics.LogError("Order processing failed because the army is not referenced (guid = {0:X8}).", new object[]
			{
				order.ArmyGuid
			});
			yield break;
		}
		Army army = gameEntity as Army;
		if (army == null)
		{
			Diagnostics.LogError("Order processing failed because the army is not an 'Army'.");
			yield break;
		}
		if (order.Activate && order.CatspawCost != null)
		{
			for (int index = 0; index < order.CatspawCost.Length; index++)
			{
				if (order.CatspawCost[index].Instant)
				{
					float resourceCost = order.CatspawCost[index].GetValue(base.Empire);
					if (!this.departmentOfTheTreasury.TryTransferResources(base.Empire, order.CatspawCost[index].ResourceName, -resourceCost))
					{
						Diagnostics.LogError("Cannot transfert the amount of resources (resource name = '{0}', cost = {0}).", new object[]
						{
							order.CatspawCost[index].ResourceName,
							-resourceCost
						});
					}
				}
			}
		}
		global::Empire previourOwner = army.Empire;
		this.ToggleCatspaw(army, order.Activate);
		if (this.EventService != null)
		{
			this.EventService.Notify(new EventCatspaw(army, previourOwner));
			yield break;
		}
		yield break;
	}

	private bool ToggleDismantleCreepingNodePreprocessor(OrderToggleDismantleCreepingNode order)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity) || !(gameEntity is Army))
		{
			Diagnostics.LogError("Order preprocessing failed because the army game entity is not valid.");
			return false;
		}
		Army army = gameEntity as Army;
		if (!this.GameEntityRepositoryService.TryGetValue(order.CreepingNodeGuid, out gameEntity) || !(gameEntity is CreepingNode))
		{
			Diagnostics.LogError("Order preprocessing failed because the CreepingNode game entity is not valid.");
			return false;
		}
		CreepingNode creepingNode = gameEntity as CreepingNode;
		Army armyAtPosition = this.WorldPositionningService.GetArmyAtPosition(creepingNode.WorldPosition);
		if (creepingNode != null && creepingNode.DismantlingArmy == null && armyAtPosition != null && armyAtPosition.Empire.Index != army.Empire.Index)
		{
			Diagnostics.LogError("Order preprocessing failed because the CreepingNode is protected by an Army!");
			return false;
		}
		if (order.NewToggleState)
		{
			if (creepingNode.DismantlingArmy != null && creepingNode.DismantlingArmy != army)
			{
				Diagnostics.LogError("Order preprocessing failed because the CreepingNode is already being dismantled.");
				return false;
			}
			DepartmentOfTransportation agency = base.Empire.GetAgency<DepartmentOfTransportation>();
			if (agency != null && agency.ArmiesWithPendingGoToInstructions != null)
			{
				List<ArmyGoToInstruction> armiesWithPendingGoToInstructions = agency.ArmiesWithPendingGoToInstructions;
				for (int i = 0; i < armiesWithPendingGoToInstructions.Count; i++)
				{
					ArmyGoToInstruction armyGoToInstruction = armiesWithPendingGoToInstructions[i];
					if (armyGoToInstruction.ArmyGUID == army.GUID && !armyGoToInstruction.IsMoveCancelled)
					{
						armyGoToInstruction.Cancel(true);
						armiesWithPendingGoToInstructions.Remove(armyGoToInstruction);
						break;
					}
				}
			}
		}
		return true;
	}

	private IEnumerator ToggleDismantleCreepingNodeProcessor(OrderToggleDismantleCreepingNode order)
	{
		IGameEntity gameEntity = null;
		Army army = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity) || !(gameEntity is Army))
		{
			Diagnostics.LogError("Order proccessing failed because the army game entity is not valid.");
			yield break;
		}
		army = (gameEntity as Army);
		CreepingNode creepingNode = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.CreepingNodeGuid, out gameEntity) || !(gameEntity is CreepingNode))
		{
			Diagnostics.LogError("Order proccessing failed because the CreepingNode game entity is not valid.");
			yield break;
		}
		creepingNode = (gameEntity as CreepingNode);
		if (order.NewToggleState)
		{
			if (army.DismantlingCreepingNodeTarget.IsValid && army.DismantlingCreepingNodeTarget != creepingNode.GUID)
			{
				this.StopDismantelingCreepingNode(army, creepingNode);
			}
			this.StartDismantelingCreepingNode(army, creepingNode);
			army.SetWorldPathWithEstimatedTimeOfArrival(null, global::Game.Time);
			this.EventService.Notify(new EventDismantleCreepingNodeStarted(creepingNode.Empire, army, creepingNode.WorldPosition));
			yield break;
		}
		this.StopDismantelingCreepingNode(army, creepingNode);
		yield break;
	}

	private bool ToggleDismantleDevicePreprocessor(OrderToggleDismantleDevice order)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity) || !(gameEntity is Army))
		{
			Diagnostics.LogError("Order preprocessing failed because the army game entity is not valid.");
			return false;
		}
		Army army = gameEntity as Army;
		if (!this.GameEntityRepositoryService.TryGetValue(order.DeviceGuid, out gameEntity) || !(gameEntity is TerraformDevice))
		{
			Diagnostics.LogError("Order preprocessing failed because the device game entity is not valid.");
			return false;
		}
		TerraformDevice terraformDevice = gameEntity as TerraformDevice;
		Army armyAtPosition = this.WorldPositionningService.GetArmyAtPosition(terraformDevice.WorldPosition);
		if (terraformDevice != null && terraformDevice.DismantlingArmy == null && armyAtPosition != null && armyAtPosition.Empire.Index != army.Empire.Index)
		{
			Diagnostics.LogError("Order preprocessing failed because the device is protected by an Army!");
			return false;
		}
		if (order.NewToggleState)
		{
			if (terraformDevice.DismantlingArmy != null && terraformDevice.DismantlingArmy != army)
			{
				Diagnostics.LogError("Order preprocessing failed because the device is already being dismantled.");
				return false;
			}
			DepartmentOfTransportation agency = base.Empire.GetAgency<DepartmentOfTransportation>();
			if (agency != null && agency.ArmiesWithPendingGoToInstructions != null)
			{
				List<ArmyGoToInstruction> armiesWithPendingGoToInstructions = agency.ArmiesWithPendingGoToInstructions;
				for (int i = 0; i < armiesWithPendingGoToInstructions.Count; i++)
				{
					ArmyGoToInstruction armyGoToInstruction = armiesWithPendingGoToInstructions[i];
					if (armyGoToInstruction.ArmyGUID == army.GUID && !armyGoToInstruction.IsMoveCancelled)
					{
						armyGoToInstruction.Cancel(true);
						armiesWithPendingGoToInstructions.Remove(armyGoToInstruction);
						break;
					}
				}
			}
		}
		return true;
	}

	private IEnumerator ToggleDismantleDeviceProcessor(OrderToggleDismantleDevice order)
	{
		IGameEntity gameEntity = null;
		Army army = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity) || !(gameEntity is Army))
		{
			Diagnostics.LogError("Order proccessing failed because the army game entity is not valid.");
			yield break;
		}
		army = (gameEntity as Army);
		TerraformDevice device = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.DeviceGuid, out gameEntity) || !(gameEntity is TerraformDevice))
		{
			Diagnostics.LogError("Order proccessing failed because the device game entity is not valid.");
			yield break;
		}
		device = (gameEntity as TerraformDevice);
		if (order.NewToggleState)
		{
			if (army.DismantlingDeviceTarget.IsValid && army.DismantlingDeviceTarget != device.GUID)
			{
				this.StopDismantelingDevice(army, device);
			}
			this.StartDismantelingDevice(army, device);
			army.SetWorldPathWithEstimatedTimeOfArrival(null, global::Game.Time);
			this.EventService.Notify(new EventDismantleDeviceStarted(device.Empire, army, device.WorldPosition));
			yield break;
		}
		this.StopDismantelingDevice(army, device);
		yield break;
	}

	private bool ToggleEarthquakePreprocessor(OrderToggleEarthquake order)
	{
		if (!order.ArmyGuid.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the Army GUID is not valid.");
			return false;
		}
		Army army = null;
		if (!this.GameEntityRepositoryService.TryGetValue<Army>(order.ArmyGuid, out army))
		{
			Diagnostics.LogError("Order preprocessing failed because the Army could not be retrieved. GUID: {0})", new object[]
			{
				order.ArmyGuid
			});
			return false;
		}
		bool infectedCitiesAreAllowed = true;
		if (!StaticString.IsNullOrEmpty(order.ArmyActionName))
		{
			IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
			if (database == null)
			{
				Diagnostics.LogError("Order preprocessing failed because the Army Action Database not be retrieved.)");
				return false;
			}
			ArmyAction armyAction = null;
			if (!database.TryGetValue(order.ArmyActionName, out armyAction) || armyAction == null)
			{
				Diagnostics.LogError("Order preprocessing failed because the Army Action could not be retrieved. Name: {0})", new object[]
				{
					order.ArmyActionName
				});
				return false;
			}
			if (armyAction is ArmyAction_ToggleEarthquake)
			{
				infectedCitiesAreAllowed = ((ArmyAction_ToggleEarthquake)armyAction).AllowInfectedCities;
			}
		}
		order.NewToggleState = (DepartmentOfDefense.CanPerformEarthquake(army, infectedCitiesAreAllowed) && !army.IsEarthquaker);
		return true;
	}

	private IEnumerator ToggleEarthquakeProcessor(OrderToggleEarthquake order)
	{
		if (!order.ArmyGuid.IsValid)
		{
			Diagnostics.LogError("Order processing failed because the Army GUID is not valid.");
			yield break;
		}
		Army army = null;
		if (!this.GameEntityRepositoryService.TryGetValue<Army>(order.ArmyGuid, out army))
		{
			Diagnostics.LogError("Order processing failed because the Army could not be retrieved. GUID: {0})", new object[]
			{
				order.ArmyGuid
			});
			yield break;
		}
		City city = null;
		if (!this.GameEntityRepositoryService.TryGetValue<City>(order.CityGuid, out city))
		{
			Diagnostics.LogError("Order processing failed because the City could not be retrieved. GUID: {0})", new object[]
			{
				order.CityGuid
			});
			yield break;
		}
		army.SetEarthquakerStatus(order.NewToggleState, false, city);
		yield break;
	}

	internal void ToggleGuard(Army army, bool state)
	{
		if (army == null)
		{
			throw new ArgumentNullException("army");
		}
		if (army.Empire == null || army.Empire.Index != base.Empire.Index)
		{
			return;
		}
		if (state != army.IsGuarding)
		{
			army.IsGuarding = state;
			if (army.IsAutoExploring)
			{
				army.IsAutoExploring = false;
			}
			this.OnArmyActionStateChange(army);
		}
	}

	private bool ToggleGuardPreprocessor(OrderToggleGuard order)
	{
		if (!order.ArmyGuid.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the army guid is not valid.");
			return false;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the army is not referenced (guid: {0:X8}).", new object[]
			{
				order.ArmyGuid
			});
			return false;
		}
		if (!(gameEntity is Army))
		{
			Diagnostics.LogError("Order preprocessing failed because the army is not an 'Army'.");
			return false;
		}
		return true;
	}

	private IEnumerator ToggleGuardProcessor(OrderToggleGuard order)
	{
		if (!order.ArmyGuid.IsValid)
		{
			Diagnostics.LogError("Order processing failed because the army guid is not valid.");
			yield break;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity))
		{
			Diagnostics.LogError("Order processing failed because the army is not referenced (guid = {0:X8}).", new object[]
			{
				order.ArmyGuid
			});
			yield break;
		}
		Army army = gameEntity as Army;
		if (army == null)
		{
			Diagnostics.LogError("Order processing failed because the army is not an 'Army'.");
			yield break;
		}
		this.ToggleGuard(army, order.State);
		yield break;
	}

	private bool ToggleNavalSiegePreprocessor(OrderToggleNavalSiege order)
	{
		if (!order.ArmyGuid.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the army guid is not valid.");
			return false;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the army is not referenced (guid = {0:X8}).", new object[]
			{
				order.ArmyGuid
			});
			return false;
		}
		Army army = gameEntity as Army;
		if (army == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the army is not an 'Army'.");
			return false;
		}
		if (!order.CityGuid.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the city guid is not valid.");
			return false;
		}
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the city is not referenced (guid = {0:X8}).", new object[]
			{
				order.ArmyGuid
			});
			return false;
		}
		City city = gameEntity as City;
		if (city == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the city is not a 'City'.");
			return false;
		}
		return !order.State || !order.State || this.CanBesiegeCity(army, city, true, true);
	}

	private IEnumerator ToggleNavalSiegeProcessor(OrderToggleNavalSiege order)
	{
		if (!order.ArmyGuid.IsValid)
		{
			Diagnostics.LogError("Order processing failed because the army guid is not valid.");
			yield break;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity))
		{
			Diagnostics.LogError("Order processing failed because the army is not referenced (guid = {0:X8}).", new object[]
			{
				order.ArmyGuid
			});
			yield break;
		}
		Army army = gameEntity as Army;
		if (army == null)
		{
			Diagnostics.LogError("Order processing failed because the army is not an 'Army'.");
			yield break;
		}
		if (!order.CityGuid.IsValid)
		{
			Diagnostics.LogError("Order processing failed because the city guid is not valid.");
			yield break;
		}
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityGuid, out gameEntity))
		{
			Diagnostics.LogError("Order processing failed because the city is not referenced (guid = {0:X8}).", new object[]
			{
				order.ArmyGuid
			});
			yield break;
		}
		City city = gameEntity as City;
		if (city == null)
		{
			Diagnostics.LogError("Order processing failed because the city is not a 'City'.");
			yield break;
		}
		DepartmentOfTheInterior besiegedInterior = city.Empire.GetAgency<DepartmentOfTheInterior>();
		if (order.State)
		{
			besiegedInterior.StartNavalSiege(city, army);
			this.OnArmyActionStateChange(army);
		}
		else
		{
			besiegedInterior.StopNavalSiege(city, army);
		}
		foreach (District district in city.Districts)
		{
			if (!(district.WorldPosition == army.WorldPosition))
			{
				if (this.WorldPositionningService.IsWaterTile(district.WorldPosition))
				{
					Army otherArmy = this.WorldPositionningService.GetArmyAtPosition(district.WorldPosition);
					if (otherArmy != null && otherArmy.Empire.Index == army.Empire.Index && otherArmy.IsNaval)
					{
						if (order.State)
						{
							float cityDefensePointLossPerTurn = army.Units.Sum((Unit unit) => unit.GetPropertyValue(SimulationProperties.CityDefensePointLossPerTurn));
							if (cityDefensePointLossPerTurn > 0f)
							{
								besiegedInterior.StartNavalSiege(city, otherArmy);
							}
						}
						else
						{
							besiegedInterior.StopNavalSiege(city, otherArmy);
						}
					}
				}
			}
		}
		city.Refresh(false);
		if (this.OnSiegeStateChange != null)
		{
			this.OnSiegeStateChange(this, new SiegeStateChangedEventArgs(army, city, order.State));
		}
		this.OnArmyActionStateChange(army);
		yield break;
	}

	private bool TogglePillagePreprocessor(OrderTogglePillage order)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			return false;
		}
		Army army = gameEntity as Army;
		PointOfInterest pointOfInterest = null;
		if (this.GameEntityRepositoryService.TryGetValue(order.PointOfInterestGuid, out gameEntity))
		{
			pointOfInterest = (gameEntity as PointOfInterest);
		}
		if (order.State)
		{
			if (pointOfInterest == null)
			{
				return false;
			}
			if (!DepartmentOfDefense.CanStartPillage(army, pointOfInterest, true))
			{
				return false;
			}
		}
		DepartmentOfTransportation agency = base.Empire.GetAgency<DepartmentOfTransportation>();
		if (agency != null && agency.ArmiesWithPendingGoToInstructions != null)
		{
			ArmyGoToInstruction armyGoToInstruction = agency.ArmiesWithPendingGoToInstructions.Find((ArmyGoToInstruction match) => match.ArmyGUID == order.ArmyGuid);
			if (armyGoToInstruction != null)
			{
				if (!armyGoToInstruction.IsMoveCancelled)
				{
					armyGoToInstruction.Cancel(true);
				}
				agency.ArmiesWithPendingGoToInstructions.Remove(armyGoToInstruction);
			}
		}
		return true;
	}

	private IEnumerator TogglePillageProcessor(OrderTogglePillage order)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			yield break;
		}
		Army army = gameEntity as Army;
		PointOfInterest pointOfInterest = null;
		if (this.GameEntityRepositoryService.TryGetValue(order.PointOfInterestGuid, out gameEntity))
		{
			pointOfInterest = (gameEntity as PointOfInterest);
		}
		if (order.State)
		{
			if (pointOfInterest != null && army.PillageTarget.IsValid && army.PillageTarget != pointOfInterest.GUID)
			{
				DepartmentOfDefense.StopPillage(army);
			}
			this.StartPillage(army, pointOfInterest);
			army.SetWorldPathWithEstimatedTimeOfArrival(null, global::Game.Time);
			yield break;
		}
		if (pointOfInterest != null)
		{
			DepartmentOfDefense.StopPillage(army, pointOfInterest);
			yield break;
		}
		DepartmentOfDefense.StopPillage(army);
		yield break;
	}

	internal void TogglePrivateers(Army army, bool state)
	{
		if (army == null)
		{
			throw new ArgumentNullException("army");
		}
		if (army.Empire == null || army.Empire.Index != base.Empire.Index)
		{
			return;
		}
		army.SetPrivaters(state);
	}

	private bool TogglePrivateersPreprocessor(OrderTogglePrivateers order)
	{
		if (!order.ArmyGuid.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the army guid is not valid.");
			return false;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the army is not referenced (guid: {0:X8}).", new object[]
			{
				order.ArmyGuid
			});
			return false;
		}
		Army army = gameEntity as Army;
		if (army == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the army is not an 'Army'.");
			return false;
		}
		if (order.NumberOfActionPointsToSpend < 0f)
		{
			ArmyAction armyAction = null;
			IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
			if (database != null && database.TryGetValue(ArmyAction_Privateers.ReadOnlyName, out armyAction))
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
		if (order.State)
		{
			if (army.Hero != null)
			{
				return false;
			}
			foreach (Unit unit in army.StandardUnits)
			{
				Diagnostics.Assert(unit.UnitDesign != null);
				if (!unit.UnitDesign.Tags.Contains(TradableUnit.ReadOnlyMercenary))
				{
					return false;
				}
			}
			return true;
		}
		return true;
	}

	private IEnumerator TogglePrivateersProcessor(OrderTogglePrivateers order)
	{
		if (!order.ArmyGuid.IsValid)
		{
			Diagnostics.LogError("Order processing failed because the army guid is not valid.");
			yield break;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity))
		{
			Diagnostics.LogError("Order processing failed because the army is not referenced (guid = {0:X8}).", new object[]
			{
				order.ArmyGuid
			});
			yield break;
		}
		Army army = gameEntity as Army;
		if (army == null)
		{
			Diagnostics.LogError("Order processing failed because the army is not an 'Army'.");
			yield break;
		}
		if (order.NumberOfActionPointsToSpend > 0f)
		{
			ArmyAction.SpendSomeNumberOfActionPoints(gameEntity, order.NumberOfActionPointsToSpend);
		}
		this.TogglePrivateers(army, order.State);
		yield break;
	}

	private bool ToggleSiegePreprocessor(OrderToggleSiege order)
	{
		if (!order.ArmyGuid.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the army guid is not valid.");
			return false;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the army is not referenced (guid = {0:X8}).", new object[]
			{
				order.ArmyGuid
			});
			return false;
		}
		Army army = gameEntity as Army;
		if (army == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the army is not an 'Army'.");
			return false;
		}
		if (!order.CityGuid.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the city guid is not valid.");
			return false;
		}
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the city is not referenced (guid = {0:X8}).", new object[]
			{
				order.ArmyGuid
			});
			return false;
		}
		City city = gameEntity as City;
		if (city == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the city is not a 'City'.");
			return false;
		}
		if (order.State)
		{
			return this.CanBesiegeCity(army, city, true, true);
		}
		return city.BesiegingEmpire == base.Empire;
	}

	private IEnumerator ToggleSiegeProcessor(OrderToggleSiege order)
	{
		if (!order.ArmyGuid.IsValid)
		{
			Diagnostics.LogError("Order processing failed because the army guid is not valid.");
			yield break;
		}
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ArmyGuid, out gameEntity))
		{
			Diagnostics.LogError("Order processing failed because the army is not referenced (guid = {0:X8}).", new object[]
			{
				order.ArmyGuid
			});
			yield break;
		}
		Army army = gameEntity as Army;
		if (army == null)
		{
			Diagnostics.LogError("Order processing failed because the army is not an 'Army'.");
			yield break;
		}
		if (!order.CityGuid.IsValid)
		{
			Diagnostics.LogError("Order processing failed because the city guid is not valid.");
			yield break;
		}
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityGuid, out gameEntity))
		{
			Diagnostics.LogError("Order processing failed because the city is not referenced (guid = {0:X8}).", new object[]
			{
				order.ArmyGuid
			});
			yield break;
		}
		City city = gameEntity as City;
		if (city == null)
		{
			Diagnostics.LogError("Order processing failed because the city is not a 'City'.");
			yield break;
		}
		DepartmentOfTheInterior besiegedInterior = city.Empire.GetAgency<DepartmentOfTheInterior>();
		if (order.State)
		{
			besiegedInterior.StartSiege(city, base.Empire as global::Empire);
			this.OnArmyActionStateChange(army);
		}
		else
		{
			besiegedInterior.StopSiege(city);
		}
		city.Refresh(false);
		if (this.OnSiegeStateChange != null)
		{
			this.OnSiegeStateChange(this, new SiegeStateChangedEventArgs(army, city, order.State));
			yield break;
		}
		yield break;
	}

	private bool TransferAcademyToNewArmyPreprocessor(OrderTransferAcademyToNewArmy order)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.GarrisonGuid, out gameEntity) || !(gameEntity is City))
		{
			return false;
		}
		City city = gameEntity as City;
		if (city.Empire != base.Empire || city.IsInEncounter || city.BesiegingEmpire != null)
		{
			return false;
		}
		if (!this.GameEntityRepositoryService.TryGetValue(order.TransferedHeroGUID, out gameEntity))
		{
			return false;
		}
		Unit unit = gameEntity as Unit;
		if (unit == null)
		{
			return false;
		}
		DepartmentOfEducation agency = base.Empire.GetAgency<DepartmentOfEducation>();
		if (agency == null || !agency.Heroes.Contains(unit) || !DepartmentOfEducation.CanAssignHero(unit))
		{
			return false;
		}
		List<WorldPosition> availablePositionsForArmyCreation = DepartmentOfDefense.GetAvailablePositionsForArmyCreation(city);
		if (availablePositionsForArmyCreation == null || availablePositionsForArmyCreation.Count == 0)
		{
			return false;
		}
		order.ArmyWorldPosition = availablePositionsForArmyCreation[0];
		order.ArmyGuid = this.GameEntityRepositoryService.GenerateGUID();
		return true;
	}

	private IEnumerator TransferAcademyToNewArmyProcessor(OrderTransferAcademyToNewArmy order)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.GarrisonGuid, out gameEntity) || !(gameEntity is City))
		{
			yield break;
		}
		City city = gameEntity as City;
		if (city.Empire != base.Empire || city.IsInEncounter || city.BesiegingEmpire != null)
		{
			yield break;
		}
		if (!this.GameEntityRepositoryService.TryGetValue(order.TransferedHeroGUID, out gameEntity))
		{
			yield break;
		}
		Unit heroUnit = gameEntity as Unit;
		if (heroUnit == null)
		{
			yield break;
		}
		DepartmentOfEducation departmentOfEducation = base.Empire.GetAgency<DepartmentOfEducation>();
		if (departmentOfEducation == null || !departmentOfEducation.Heroes.Contains(heroUnit) || !DepartmentOfEducation.CanAssignHero(heroUnit))
		{
			yield break;
		}
		if (order.ArmyGuid == GameEntityGUID.Zero)
		{
			yield break;
		}
		Army army = this.CreateArmy(order.ArmyGuid, true);
		army.SetWorldPositionWithEstimatedTimeOfArrival(order.ArmyWorldPosition, global::Game.Time);
		departmentOfEducation.ChangeAssignment(heroUnit, army);
		if (army.Hero == null)
		{
			yield break;
		}
		army.Refresh(true);
		this.AddArmy(army);
		this.GameEntityRepositoryService.Register(army);
		IOrbService orbService = this.GameService.Game.Services.GetService<IOrbService>();
		if (orbService != null)
		{
			orbService.CollectOrbsAtPosition(army.WorldPosition, army, base.Empire as global::Empire);
		}
		IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
		if (downloadableContentService.IsShared(DownloadableContent19.ReadOnlyName))
		{
			this.CheckArmyOnMapBoost(army);
			int armyCountBeforeTerrainDamage = army.UnitsCount;
			if (this.CheckTerrainDamageForUnits(army))
			{
				ArmyHitInfo hitInfo = new ArmyHitInfo(army, armyCountBeforeTerrainDamage, army.WorldPosition, ArmyHitInfo.HitType.Travel);
				this.EventService.Notify(new EventArmyHit(base.Empire, hitInfo, false));
			}
		}
		if (this.EnableDetection)
		{
			this.UpdateDetection(army);
			yield break;
		}
		yield break;
	}

	internal bool TransferGarrisonToNewArmyPreprocessor(OrderTransferGarrisonToNewArmy order)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.GarrisonGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target garrison entity guid is not valid.");
			return false;
		}
		if (!(gameEntity is IGarrison))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a garrison.");
			return false;
		}
		IGarrison garrison = gameEntity as IGarrison;
		if (garrison.Empire != base.Empire)
		{
			Diagnostics.LogError("Order preprocessing failed because the target garrison is not part of the department's empire.");
			return false;
		}
		if (garrison.IsInEncounter)
		{
			Diagnostics.LogError("Order preprocessing failed because the target garrison is in an encounter.");
			return false;
		}
		if (order.TransferedUnitGUIDs == null || order.TransferedUnitGUIDs.Length == 0)
		{
			Diagnostics.LogError("Order preprocessing failed because the target transfered units array is null or empty.");
			return false;
		}
		if (!this.CanTransfertUnitsFromGarrison(order.TransferedUnitGUIDs, garrison, true))
		{
			return false;
		}
		if (!this.CanCreateNewArmyFrom(order.TransferedUnitGUIDs, garrison, false, order.GenerateArmyLeader))
		{
			return false;
		}
		if (!DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(order.ArmyWorldPosition, PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.Water))
		{
			Diagnostics.LogWarning("Order preprocessing failed because the target army world position is not valid.");
			return false;
		}
		if (garrison is City)
		{
			City city = garrison as City;
			if (!city.Districts.Any((District district) => order.ArmyWorldPosition == district.WorldPosition && (city.BesiegingEmpire == null || district.Type != DistrictType.Exploitation)) && order.CheckIfCityTile)
			{
				Diagnostics.LogError("Order preprocessing failed because the target army world position is not over a valid city tile.");
				return false;
			}
		}
		else if (garrison is Army)
		{
			for (int i = 0; i < order.TransferedUnitGUIDs.Length; i++)
			{
				IGameEntity gameEntity2;
				if (this.GameEntityRepositoryService.TryGetValue(order.TransferedUnitGUIDs[i], out gameEntity2) && gameEntity2 is Unit)
				{
					Unit pathfindingContextProvider = gameEntity2 as Unit;
					IWorldPositionable worldPositionable = garrison as IWorldPositionable;
					if (!this.PathfindingService.IsTileStopable(order.ArmyWorldPosition, pathfindingContextProvider, PathfindingFlags.IgnoreFogOfWar, null) || !this.PathfindingService.IsTransitionPassable(worldPositionable.WorldPosition, order.ArmyWorldPosition, pathfindingContextProvider, PathfindingFlags.IgnoreFogOfWar, null))
					{
						Diagnostics.LogError("Order preprocessing failed because the target army world position is not reachable. Army {0} order {1}", new object[]
						{
							worldPositionable.WorldPosition.ToString(),
							order.ArmyWorldPosition.ToString()
						});
						return false;
					}
				}
			}
		}
		IWorldPositionable worldPositionable2 = garrison as IWorldPositionable;
		bool flag = true;
		if (order.CheckMovementPoints)
		{
			for (int j = 0; j < order.TransferedUnitGUIDs.Length; j++)
			{
				IGameEntity gameEntity3;
				if (this.GameEntityRepositoryService.TryGetValue(order.TransferedUnitGUIDs[j], out gameEntity3) && gameEntity3 is Unit)
				{
					if (worldPositionable2 != null && worldPositionable2.WorldPosition != order.ArmyWorldPosition)
					{
						if (!(gameEntity3 as Unit).UnitDesign.Tags.Contains(DownloadableContent9.TagSolitary))
						{
							float transitionCost = this.PathfindingService.GetTransitionCost(worldPositionable2.WorldPosition, order.ArmyWorldPosition, (Unit)gameEntity3, PathfindingFlags.IgnoreFogOfWar, null);
							if (float.IsInfinity(transitionCost))
							{
								flag = false;
								break;
							}
							if (((Unit)gameEntity3).GetPropertyValue(SimulationProperties.Movement) <= 0f)
							{
								flag = false;
								break;
							}
						}
					}
					else if (((Unit)gameEntity3).GetPropertyValue(SimulationProperties.Movement) <= 0f)
					{
						flag = false;
						break;
					}
				}
			}
		}
		if (!flag)
		{
			Diagnostics.LogWarning("Order preprocessing failed because the target unit(s) has(have) not enough movement points.");
			return false;
		}
		order.ArmyGuid = this.GameEntityRepositoryService.GenerateGUID();
		return true;
	}

	internal IEnumerator TransferGarrisonToNewArmyProcessor(OrderTransferGarrisonToNewArmy order, WorldOrientation worldOrientation)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.GarrisonGuid, out gameEntity))
		{
			Diagnostics.LogError("Skipping garrison unit transfer process because the target garrison entity guid is not valid.");
			yield break;
		}
		if (!(gameEntity is IGarrison))
		{
			Diagnostics.LogError("Skipping garrison unit transfer process because the target game entity is not a garrison.");
			yield break;
		}
		IGarrison garrison = gameEntity as IGarrison;
		if (garrison.Empire != base.Empire)
		{
			Diagnostics.LogError("Skipping garrison unit transfer process because the target garrison is not part of the department's empire.");
			yield break;
		}
		if (order.ArmyGuid == GameEntityGUID.Zero)
		{
			Diagnostics.LogError("Skipping garrison unit transfer process because the army game entity guid is null.");
			yield break;
		}
		Army army = this.CreateArmy(order.ArmyGuid, false);
		if (garrison is Army && (garrison as Army).IsPrivateers)
		{
			army.SetPrivaters(true);
		}
		army.SetWorldPositionWithEstimatedTimeOfArrival(order.ArmyWorldPosition, global::Game.Time);
		army.WorldOrientation = worldOrientation;
		IWorldPositionable worldPositionable = garrison as IWorldPositionable;
		for (int index = 0; index < order.TransferedUnitGUIDs.Length; index++)
		{
			if (this.GameEntityRepositoryService.TryGetValue(order.TransferedUnitGUIDs[index], out gameEntity))
			{
				Unit unit = gameEntity as Unit;
				if (worldPositionable != null && worldPositionable.WorldPosition != order.ArmyWorldPosition && !unit.UnitDesign.Tags.Contains(DownloadableContent9.TagSolitary))
				{
					float cost = this.PathfindingService.GetTransitionCost(worldPositionable.WorldPosition, order.ArmyWorldPosition, unit, PathfindingFlags.IgnoreFogOfWar, null);
					float maximumMovement = this.PathfindingService.GetMaximumMovementPoints(order.ArmyWorldPosition, unit, PathfindingFlags.IgnoreFogOfWar);
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
					unit.SwitchToEmbarkedUnit(this.PathfindingService.GetTileMovementCapacity(army.WorldPosition, (PathfindingFlags)0) == PathfindingMovementCapacity.Water);
				}
				if (unit != garrison.Hero)
				{
					garrison.RemoveUnit(unit);
					army.AddUnit(unit);
				}
				else
				{
					garrison.SetHero(null);
					army.SetHero(unit);
				}
			}
		}
		army.ClientLocalizedName = this.GenerateArmyClientLocalizedName(army);
		army.Refresh(true);
		if (garrison.IsEmpty && garrison is Army)
		{
			this.RemoveArmy(garrison as Army, true);
		}
		else if (garrison is SimulationObjectWrapper)
		{
			SimulationObjectWrapper wrapper = garrison as SimulationObjectWrapper;
			wrapper.Refresh(false);
		}
		this.AddArmy(army);
		this.GameEntityRepositoryService.Register(army);
		IOrbService orbService = this.GameService.Game.Services.GetService<IOrbService>();
		if (orbService != null)
		{
			orbService.CollectOrbsAtPosition(army.WorldPosition, army, base.Empire as global::Empire);
		}
		IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
		if (downloadableContentService.IsShared(DownloadableContent19.ReadOnlyName))
		{
			this.CheckArmyOnMapBoost(army);
			int armyCountBeforeTerrainDamage = army.UnitsCount;
			if (this.CheckTerrainDamageForUnits(army))
			{
				ArmyHitInfo hitInfo = new ArmyHitInfo(army, armyCountBeforeTerrainDamage, army.WorldPosition, ArmyHitInfo.HitType.Travel);
				this.EventService.Notify(new EventArmyHit(base.Empire, hitInfo, false));
			}
		}
		if (this.EnableDetection)
		{
			this.UpdateDetection(army);
		}
		if (this.PathfindingService.GetTileMovementCapacity(army.WorldPosition, (PathfindingFlags)0) == PathfindingMovementCapacity.Water)
		{
			army.SetSails();
		}
		if (order.GoToDestination != WorldPosition.Invalid)
		{
			OrderGoTo orderGoTo = new OrderGoTo(army.Empire.Index, army.GUID, order.GoToDestination);
			army.Empire.PlayerControllers.Client.PostOrder(orderGoTo);
			yield break;
		}
		yield break;
	}

	private IEnumerator TransferGarrisonToNewArmyProcessor(OrderTransferGarrisonToNewArmy order)
	{
		yield return this.TransferGarrisonToNewArmyProcessor(order, WorldOrientation.East);
		yield break;
	}

	private bool TransferSeafaringUnitToNewArmyPreprocessor(OrderTransferSeafaringUnitToNewArmy order)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.GarrisonGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target garrison entity guid is not valid.");
			return false;
		}
		if (!(gameEntity is City))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a garrison.");
			return false;
		}
		City city = gameEntity as City;
		if (!order.ArmyWorldPosition.IsValid)
		{
			return false;
		}
		if (city.Empire != base.Empire)
		{
			Diagnostics.LogError("Order preprocessing failed because the target garrison is not part of the department's empire.");
			return false;
		}
		if (city.IsInEncounter)
		{
			Diagnostics.LogError("Order preprocessing failed because the target garrison is in an encounter.");
			return false;
		}
		if (order.TransferedUnitGUIDs == null || order.TransferedUnitGUIDs.Length == 0)
		{
			Diagnostics.LogError("Order preprocessing failed because the target transfered units array is null or empty.");
			return false;
		}
		if (!this.CanCreateNewArmyFrom(order.TransferedUnitGUIDs, city, false, order.GenerateArmyLeader))
		{
			return false;
		}
		if (!DepartmentOfDefense.CheckWhetherTargetPositionIsValidAsSeafaringSpawnLocation(order.ArmyWorldPosition, city.Empire.Index, order.TransferedUnitGUIDs.Length))
		{
			Diagnostics.LogWarning("Order preprocessing failed because the target army world position is not valid.");
			return false;
		}
		order.ArmyGuid = this.GameEntityRepositoryService.GenerateGUID();
		return true;
	}

	private IEnumerator TransferSeafaringUnitToNewArmyProcessor(OrderTransferSeafaringUnitToNewArmy order)
	{
		if (order.ArmyWorldPosition.IsValid)
		{
			IGameEntity gameEntity = null;
			if (!this.GameEntityRepositoryService.TryGetValue(order.GarrisonGuid, out gameEntity))
			{
				Diagnostics.LogError("Skipping garrison unit transfer process because the target garrison entity guid is not valid.");
				yield break;
			}
			if (!(gameEntity is IGarrison))
			{
				Diagnostics.LogError("Skipping garrison unit transfer process because the target game entity is not a garrison.");
				yield break;
			}
			IGarrison garrison = gameEntity as IGarrison;
			if (garrison.Empire != base.Empire)
			{
				Diagnostics.LogError("Skipping garrison unit transfer process because the target garrison is not part of the department's empire.");
				yield break;
			}
			if (order.ArmyGuid == GameEntityGUID.Zero)
			{
				Diagnostics.LogError("Skipping garrison unit transfer process because the army game entity guid is null.");
				yield break;
			}
			GridMap<Army> armiesMap = (this.GameService.Game as global::Game).World.Atlas.GetMap(WorldAtlas.Maps.Armies) as GridMap<Army>;
			Diagnostics.Assert(armiesMap != null);
			Army army = armiesMap.GetValue(order.ArmyWorldPosition);
			bool armyAlreadyExist = false;
			if (army != null)
			{
				if (army.Empire.Index != garrison.Empire.Index)
				{
					yield break;
				}
				float capacity = army.GetPropertyValue(SimulationProperties.MaximumUnitSlotCount);
				if (capacity - (float)(army.UnitsCount + order.TransferedUnitGUIDs.Length) < 0f)
				{
					Diagnostics.LogError("Skipping garrison unit transfer process because the destination army has not room enough.");
					yield break;
				}
				armyAlreadyExist = true;
			}
			if (army == null)
			{
				army = this.CreateArmy(order.ArmyGuid, false);
			}
			if (garrison is Army && (garrison as Army).IsPrivateers)
			{
				army.SetPrivaters(true);
			}
			army.SetWorldPositionWithEstimatedTimeOfArrival(order.ArmyWorldPosition, global::Game.Time);
			army.WorldOrientation = WorldOrientation.SouthWest;
			IWorldPositionable worldPositionable = garrison as IWorldPositionable;
			for (int index = 0; index < order.TransferedUnitGUIDs.Length; index++)
			{
				if (this.GameEntityRepositoryService.TryGetValue(order.TransferedUnitGUIDs[index], out gameEntity))
				{
					Unit unit = gameEntity as Unit;
					if (worldPositionable != null && worldPositionable.WorldPosition != order.ArmyWorldPosition && !unit.UnitDesign.Tags.Contains(DownloadableContent9.TagSolitary) && !unit.UnitDesign.Tags.Contains(DownloadableContent16.TagSeafaring))
					{
						float cost = this.PathfindingService.GetTransitionCost(worldPositionable.WorldPosition, order.ArmyWorldPosition, unit, PathfindingFlags.IgnoreFogOfWar, null);
						float maximumMovement = this.PathfindingService.GetMaximumMovementPoints(order.ArmyWorldPosition, unit, PathfindingFlags.IgnoreFogOfWar);
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
					if (unit != garrison.Hero)
					{
						garrison.RemoveUnit(unit);
						army.AddUnit(unit);
					}
					else
					{
						garrison.SetHero(null);
						army.SetHero(unit);
					}
				}
			}
			army.ClientLocalizedName = this.GenerateArmyClientLocalizedName(army);
			army.Refresh(true);
			if (garrison.IsEmpty && garrison is Army)
			{
				this.RemoveArmy(garrison as Army, true);
			}
			else if (garrison is SimulationObjectWrapper)
			{
				SimulationObjectWrapper wrapper = garrison as SimulationObjectWrapper;
				wrapper.Refresh(false);
			}
			if (!armyAlreadyExist)
			{
				this.AddArmy(army);
				this.GameEntityRepositoryService.Register(army);
			}
			IOrbService orbService = this.GameService.Game.Services.GetService<IOrbService>();
			if (orbService != null)
			{
				orbService.CollectOrbsAtPosition(army.WorldPosition, army, base.Empire as global::Empire);
			}
			IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
			if (downloadableContentService.IsShared(DownloadableContent19.ReadOnlyName))
			{
				this.CheckArmyOnMapBoost(army);
				int armyCountBeforeTerrainDamage = army.UnitsCount;
				if (this.CheckTerrainDamageForUnits(army))
				{
					ArmyHitInfo hitInfo = new ArmyHitInfo(army, armyCountBeforeTerrainDamage, army.WorldPosition, ArmyHitInfo.HitType.Travel);
					this.EventService.Notify(new EventArmyHit(base.Empire, hitInfo, false));
				}
			}
			if (this.EnableDetection)
			{
				this.UpdateDetection(army);
			}
			if (!army.IsNaval)
			{
				if (this.PathfindingService.GetTileMovementCapacity(army.WorldPosition, (PathfindingFlags)0) == PathfindingMovementCapacity.Water)
				{
					army.SetSails(true);
				}
				army.Refresh(false);
			}
			if (order.GoToDestination != WorldPosition.Invalid)
			{
				OrderGoTo orderGoTo = new OrderGoTo(army.Empire.Index, army.GUID, order.GoToDestination);
				army.Empire.PlayerControllers.Client.PostOrder(orderGoTo);
			}
		}
		else
		{
			IGameEntity gameEntity2 = null;
			Unit unit2 = null;
			for (int index2 = 0; index2 < order.TransferedUnitGUIDs.Length; index2++)
			{
				if (this.GameEntityRepositoryService.TryGetValue(order.TransferedUnitGUIDs[index2], out gameEntity2))
				{
					unit2 = (gameEntity2 as Unit);
					if (unit2.Garrison != null)
					{
						IGarrison garrison2 = unit2.Garrison;
						garrison2.RemoveUnit(unit2);
					}
					this.GameEntityRepositoryService.Unregister(unit2);
					unit2.Dispose();
				}
			}
		}
		for (int index3 = 0; index3 < order.TransferedUnitGUIDs.Length; index3++)
		{
			if (this.delayedSolitaryUnitSpawnCommands != null)
			{
				this.delayedSolitaryUnitSpawnCommands.RemoveAll((DelayedSolitaryUnitSpawnCommand match) => match.GameEntityGUID == order.TransferedUnitGUIDs[index3]);
			}
		}
		yield break;
	}

	private bool TransferSolitaryUnitToNewArmyPreprocessor(OrderTransferSolitaryUnitToNewArmy order)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.GarrisonGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target garrison entity guid is not valid.");
			return false;
		}
		if (!(gameEntity is City))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a garrison.");
			return false;
		}
		City city = gameEntity as City;
		if (!city.Districts.Any((District district) => order.ArmyWorldPosition == district.WorldPosition && (city.BesiegingEmpire == null || district.Type != DistrictType.Exploitation)))
		{
			order.ArmyWorldPosition = WorldPosition.Invalid;
			if (city.BesiegingEmpire == null)
			{
				for (int i = 0; i < city.Districts.Count; i++)
				{
					if (city.Districts[i].Type == DistrictType.Exploitation && DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(city.Districts[i].WorldPosition, PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.Water))
					{
						order.ArmyWorldPosition = city.Districts[i].WorldPosition;
						break;
					}
				}
			}
			if (!order.ArmyWorldPosition.IsValid)
			{
				for (int j = 0; j < city.Districts.Count; j++)
				{
					if (city.Districts[j].Type != DistrictType.Exploitation && DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(city.Districts[j].WorldPosition, PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.Water))
					{
						order.ArmyWorldPosition = city.Districts[j].WorldPosition;
						break;
					}
				}
			}
		}
		return !order.ArmyWorldPosition.IsValid || this.TransferGarrisonToNewArmyPreprocessor(order);
	}

	private IEnumerator TransferSolitaryUnitToNewArmyProcessor(OrderTransferSolitaryUnitToNewArmy order)
	{
		if (order.ArmyWorldPosition.IsValid)
		{
			yield return this.TransferGarrisonToNewArmyProcessor(order, WorldOrientation.SouthWest);
		}
		else
		{
			IGameEntity gameEntity = null;
			Unit unit = null;
			for (int index = 0; index < order.TransferedUnitGUIDs.Length; index++)
			{
				if (this.GameEntityRepositoryService.TryGetValue(order.TransferedUnitGUIDs[index], out gameEntity))
				{
					unit = (gameEntity as Unit);
					if (unit.Garrison != null)
					{
						IGarrison garrison = unit.Garrison;
						garrison.RemoveUnit(unit);
					}
					this.GameEntityRepositoryService.Unregister(unit);
					unit.Dispose();
				}
			}
		}
		for (int index2 = 0; index2 < order.TransferedUnitGUIDs.Length; index2++)
		{
			if (this.delayedSolitaryUnitSpawnCommands != null)
			{
				this.delayedSolitaryUnitSpawnCommands.RemoveAll((DelayedSolitaryUnitSpawnCommand match) => match.GameEntityGUID == order.TransferedUnitGUIDs[index2]);
			}
		}
		yield break;
	}

	private bool TransferUnitsPreprocessor(OrderTransferUnits order)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.SourceGuid, out gameEntity))
		{
			Diagnostics.LogWarning("Order preprocessing failed because the target source entity guid is not valid.");
			return false;
		}
		if (!(gameEntity is IGarrison))
		{
			Diagnostics.LogError("Order preprocessing failed because the target source game entity {0} is not a garrison.", new object[]
			{
				order.SourceGuid
			});
			return false;
		}
		IGarrison source = gameEntity as IGarrison;
		if (source.Empire != base.Empire)
		{
			Diagnostics.LogError("Order preprocessing failed because the target garrison {0} is not part of the department's empire.", new object[]
			{
				order.DestinationGuid
			});
			return false;
		}
		if (source.IsInEncounter)
		{
			Diagnostics.LogError("Order preprocessing failed because the target source garrison {0} is in an encounter.", new object[]
			{
				order.SourceGuid
			});
			return false;
		}
		if (order.TransferedUnitGuids == null || order.TransferedUnitGuids.Length == 0)
		{
			Diagnostics.LogError("Order preprocessing failed because the target transfered units array is null or empty.");
			return false;
		}
		if (!this.GameEntityRepositoryService.TryGetValue(order.DestinationGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target destination entity guid is not valid.");
			return false;
		}
		if (!(gameEntity is IGarrison))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a garrison.");
			return false;
		}
		IGarrison garrison = gameEntity as IGarrison;
		if (!(source is IWorldPositionable) || !(garrison is IWorldPositionable))
		{
			Diagnostics.LogError("Order preprocessing failed because the source and destination are not positionnables.");
			return false;
		}
		if (garrison.IsInEncounter)
		{
			Diagnostics.LogError("Order preprocessing failed because the target destination garrison {0} is in an encounter.", new object[]
			{
				order.DestinationGuid
			});
			return false;
		}
		if (source.Empire != garrison.Empire)
		{
			return false;
		}
		WorldPosition worldPosition = (source as IWorldPositionable).WorldPosition;
		WorldPosition worldPosition2 = (garrison as IWorldPositionable).WorldPosition;
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		bool flag = service.IsShared(DownloadableContent19.ReadOnlyName) && order.TeleportAllowed;
		bool flag2 = garrison is City;
		bool flag3 = garrison is Village;
		bool flag4 = garrison is Fortress;
		if (flag2)
		{
			City city = garrison as City;
			bool flag5 = false;
			for (int i = 0; i < city.Districts.Count; i++)
			{
				if (city.Districts[i].Type != DistrictType.Exploitation)
				{
					if (city.Districts[i].Type != DistrictType.Improvement)
					{
						if (this.WorldPositionningService.GetDistance(worldPosition, city.Districts[i].WorldPosition) <= 1)
						{
							flag5 = true;
							break;
						}
					}
				}
			}
			if (!flag5)
			{
				Diagnostics.LogError("Order preprocessing failed because the source {0} and the city destination {1} are too far away.", new object[]
				{
					worldPosition,
					worldPosition2
				});
				return false;
			}
		}
		else if (flag3)
		{
			if (this.WorldPositionningService.GetDistance(worldPosition, (garrison as Village).WorldPosition) > 1)
			{
				Diagnostics.LogError("Order preprocessing failed because the source {0} and the village destination {1} are too far away.", new object[]
				{
					worldPosition,
					worldPosition2
				});
				return false;
			}
		}
		else if (flag4)
		{
			bool flag6 = false;
			if (this.WorldPositionningService.GetDistance(worldPosition, (garrison as Fortress).WorldPosition) <= 1)
			{
				flag6 = true;
			}
			for (int j = 0; j < (garrison as Fortress).Facilities.Count; j++)
			{
				if (this.WorldPositionningService.GetDistance(worldPosition, (garrison as Fortress).Facilities[j].WorldPosition) <= 1)
				{
					flag6 = true;
				}
			}
			if (!flag6)
			{
				Diagnostics.LogError("Order preprocessing failed because the source {0} and the village destination {1} are too far away.", new object[]
				{
					worldPosition,
					worldPosition2
				});
				return false;
			}
		}
		else if (worldPosition.IsValid && worldPosition2.IsValid && !flag)
		{
			int distance = this.WorldPositionningService.GetDistance(worldPosition, worldPosition2);
			if (distance > 1)
			{
				Diagnostics.LogError("Order preprocessing failed because the source {0} and destination {1} are too far away.", new object[]
				{
					worldPosition,
					worldPosition2
				});
				return false;
			}
		}
		int num = 0;
		for (int k = 0; k < order.TransferedUnitGuids.Length; k++)
		{
			if (!this.GameEntityRepositoryService.TryGetValue(order.TransferedUnitGuids[k], out gameEntity))
			{
				Diagnostics.LogError("Order preprocessing failed because the produced unit entity guid at index {0} is not valid.", new object[]
				{
					k
				});
				return false;
			}
			Unit unit = gameEntity as Unit;
			if (unit != null)
			{
				num += (int)unit.GetPropertyValue(SimulationProperties.UnitSlotCount);
			}
			if (flag4 && unit.IsHero())
			{
				return false;
			}
			if (unit.UnitDesign != null && unit.UnitDesign.Tags.Contains(DownloadableContent9.TagSolitary))
			{
				return false;
			}
			if (unit != null && unit.SimulationObject.Tags.Contains(KaijuArmy.ClassKaijuArmy) && !DepartmentOfScience.IsTechnologyResearched(base.Empire as global::Empire, "TechnologyDefinitionMimics1"))
			{
				Diagnostics.LogError("Order preprocessing failed because can not transfer unit to Kaiju Army!.", new object[]
				{
					unit.GUID,
					k
				});
				return false;
			}
			if (worldPosition != worldPosition2 && !flag2 && !flag3 && !flag4 && !flag)
			{
				float transitionCost = this.PathfindingService.GetTransitionCost(worldPosition, worldPosition2, unit, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreFogOfWar, null);
				if (float.IsInfinity(transitionCost))
				{
					Diagnostics.LogError("Order preprocessing failed because the unit '{0}' at index '{1}' cannot reach the destination.", new object[]
					{
						unit.GUID,
						k
					});
					return false;
				}
				float propertyValue = unit.GetPropertyValue(SimulationProperties.Movement);
				if (propertyValue <= 0f)
				{
					Diagnostics.LogWarning("Order preprocessing failed because the unit '{0}' at index '{1}' has no movement left.", new object[]
					{
						unit.GUID,
						k
					});
					return false;
				}
			}
		}
		if (garrison.MaximumUnitSlot < garrison.CurrentUnitSlot + num)
		{
			Diagnostics.LogError("Order preprocessing failed because the target destination has not enough room for the transfered units.");
			return false;
		}
		if (!this.CanTransfertUnitsFromGarrison(order.TransferedUnitGuids, source, true))
		{
			return false;
		}
		if (!this.CanTransferToGarrison(order.TransferedUnitGuids.Count<GameEntityGUID>(), garrison, true))
		{
			return false;
		}
		DepartmentOfTransportation agency = base.Empire.GetAgency<DepartmentOfTransportation>();
		if (agency != null)
		{
			ArmyGoToInstruction armyGoToInstruction = agency.ArmiesWithPendingGoToInstructions.Find((ArmyGoToInstruction match) => match.ArmyGUID == source.GUID);
			if (armyGoToInstruction != null && armyGoToInstruction.IsMoving)
			{
				armyGoToInstruction.Cancel(false);
			}
		}
		return true;
	}

	private IEnumerator TransferUnitsProcessor(OrderTransferUnits order)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(order.SourceGuid, out gameEntity))
		{
			Diagnostics.LogError("Skipping garrison unit transfer process because the target garrison entity guid is not valid.");
			yield break;
		}
		if (!(gameEntity is IGarrison))
		{
			Diagnostics.LogError("Skipping garrison unit transfer process because the target game entity is not a garrison.");
			yield break;
		}
		IGarrison source = gameEntity as IGarrison;
		if (source.Empire != base.Empire)
		{
			Diagnostics.LogError("Skipping garrison unit transfer process because the target source is not part of the department's empire.");
			yield break;
		}
		if (!this.GameEntityRepositoryService.TryGetValue(order.DestinationGuid, out gameEntity))
		{
			Diagnostics.LogError("Skipping garrison unit transfer process because the target destination entity guid is not valid.");
			yield break;
		}
		if (!(gameEntity is IGarrison))
		{
			Diagnostics.LogError("Skipping garrison unit transfer process because the target game entity is not a garrison.");
			yield break;
		}
		IGarrison destination = gameEntity as IGarrison;
		WorldPosition sourcePosition = (source as IWorldPositionable).WorldPosition;
		WorldPosition destinationPosition = (destination as IWorldPositionable).WorldPosition;
		IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
		bool isCityOrVillage = destination is City || destination is Village || destination is Camp;
		for (int index = 0; index < order.TransferedUnitGuids.Length; index++)
		{
			if (this.GameEntityRepositoryService.TryGetValue(order.TransferedUnitGuids[index], out gameEntity))
			{
				Unit unit = gameEntity as Unit;
				if (unit != source.Hero)
				{
					source.RemoveUnit(unit);
					destination.AddUnit(unit);
				}
				else
				{
					source.SetHero(null);
					destination.SetHero(unit);
				}
				if (!unit.SimulationObject.Tags.Contains(DownloadableContent16.SeafaringUnit))
				{
					unit.SwitchToEmbarkedUnit(this.PathfindingService.GetTileMovementCapacity(destinationPosition, (PathfindingFlags)0) == PathfindingMovementCapacity.Water);
				}
				if (isCityOrVillage)
				{
					unit.SetPropertyBaseValue(SimulationProperties.MovementRatio, 0f);
				}
				else if (sourcePosition != destinationPosition)
				{
					float cost = this.PathfindingService.GetTransitionCost(sourcePosition, destinationPosition, unit, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreFogOfWar, null);
					float maximumMovement = this.PathfindingService.GetMaximumMovementPoints(destinationPosition, unit, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreFogOfWar);
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
				if (downloadableContentService != null && downloadableContentService.IsShared(DownloadableContent19.ReadOnlyName) && destination is Army)
				{
					Army destinationArmy = destination as Army;
					if (destinationArmy != null && this.CheckTerrainDamageForUnit(unit))
					{
						ArmyHitInfo hitInfo = new ArmyHitInfo(destinationArmy, destinationArmy.UnitsCount, destinationArmy.WorldPosition, ArmyHitInfo.HitType.Travel);
						this.EventService.Notify(new EventArmyHit(destinationArmy.Empire, hitInfo, false));
					}
				}
			}
		}
		if (source.StandardUnits.Count == 0 && source is Army && source.Hero != null && destination is Army && destination.Hero == null && !(destination is KaijuArmy) && !(destination is KaijuGarrison))
		{
			Unit hero = source.Hero;
			source.SetHero(null);
			destination.SetHero(hero);
			destination.Hero.SwitchToEmbarkedUnit(this.PathfindingService.GetTileMovementCapacity(destinationPosition, (PathfindingFlags)0) == PathfindingMovementCapacity.Water);
		}
		GameEntityGUID destroyedSourceArmyGUID = GameEntityGUID.Zero;
		if (source.IsEmpty)
		{
			destroyedSourceArmyGUID = (source as Army).GUID;
			this.RemoveArmy(source as Army, true);
		}
		else if (source is SimulationObjectWrapper)
		{
			SimulationObjectWrapper wrapper = source as SimulationObjectWrapper;
			wrapper.Refresh(false);
			if (this.EnableDetection && source is Army)
			{
				this.UpdateDetection((Army)source);
			}
			if (source is Army)
			{
				Army sourceArmy = (Army)source;
				sourceArmy.SetSails();
			}
		}
		if (destination is Army)
		{
			Army destinationArmy2 = (Army)destination;
			if (destinationArmy2.IsPrivateers)
			{
				if (destinationArmy2.Hero != null)
				{
					this.TogglePrivateers(destinationArmy2, false);
				}
				else
				{
					foreach (Unit standardUnit in destinationArmy2.StandardUnits)
					{
						Diagnostics.Assert(standardUnit.UnitDesign != null);
						if (!standardUnit.UnitDesign.Tags.Contains(TradableUnit.ReadOnlyMercenary))
						{
							this.TogglePrivateers(destinationArmy2, false);
							break;
						}
					}
				}
			}
			destinationArmy2.SetSails();
		}
		if (destination is SimulationObjectWrapper)
		{
			SimulationObjectWrapper wrapper2 = destination as SimulationObjectWrapper;
			wrapper2.Refresh(false);
			if (this.EnableDetection && destination is Army)
			{
				this.UpdateDetection((Army)destination);
			}
		}
		if (destroyedSourceArmyGUID != GameEntityGUID.Zero)
		{
			EventArmyTransferred evt = new EventArmyTransferred(destination.Empire, destroyedSourceArmyGUID, destination);
			this.EventService.Notify(evt);
		}
		yield break;
	}

	private bool UpdateArmyObjectivePreprocessor(OrderUpdateArmyObjective orderUpdate)
	{
		IGameEntity gameEntity = null;
		if (!this.GameEntityRepositoryService.TryGetValue(orderUpdate.ArmyGUID, out gameEntity))
		{
			Diagnostics.LogWarning("Order preprocessing failed because the army is not referenced (guid = {0:X8}).", new object[]
			{
				orderUpdate.ArmyGUID
			});
			return false;
		}
		if (orderUpdate.Objective.TargetCityGUID != 0UL && !this.GameEntityRepositoryService.Contains(orderUpdate.Objective.TargetCityGUID))
		{
			Diagnostics.LogWarning("Order preprocessing failed because the city target is not referenced (guid = {0:X8}).", new object[]
			{
				orderUpdate.Objective.ToString()
			});
			return false;
		}
		switch (orderUpdate.Objective.BehaviourType)
		{
		case QuestArmyObjective.QuestBehaviourType.Roaming:
		case QuestArmyObjective.QuestBehaviourType.PacifistRoaming:
		case QuestArmyObjective.QuestBehaviourType.SeaMonsterRoaming:
		case QuestArmyObjective.QuestBehaviourType.FleeRoaming:
			return true;
		case QuestArmyObjective.QuestBehaviourType.Offense:
			if (!this.GameEntityRepositoryService.Contains(orderUpdate.Objective.TargetCityGUID) && orderUpdate.Objective.TargetEmpireIndex == -1)
			{
				Diagnostics.LogWarning("Order preprocessing failed because the city target is not referenced (guid = {0:X8}).", new object[]
				{
					orderUpdate.Objective.ToString()
				});
				return false;
			}
			return true;
		}
		Diagnostics.LogWarning("Order preprocessing failed because the behaviour is undefined).");
		return false;
	}

	private IEnumerator UpdateArmyObjectiveProcessor(OrderUpdateArmyObjective orderUpdate)
	{
		ArmyObjectiveUpdatedEventArgs eventArgs = new ArmyObjectiveUpdatedEventArgs(orderUpdate.ArmyGUID, orderUpdate.Objective);
		if (this.ObjectiveUpdateChange != null)
		{
			this.ObjectiveUpdateChange(this, eventArgs);
		}
		yield break;
	}

	private bool UpdateMilitiaPreprocessor(OrderUpdateMilitia order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityGuid, out gameEntity) || !(gameEntity is City))
		{
			Diagnostics.LogError("Preprocessor failed because city guid is invalid. Empire={0}, CityGuid={1}", new object[]
			{
				base.Empire.Index,
				order.CityGuid
			});
			return false;
		}
		City city = gameEntity as City;
		int num = (int)city.Militia.GetPropertyValue(SimulationProperties.MaximumUnitSlotCount);
		int unitsCount = city.Militia.UnitsCount;
		if (unitsCount < num)
		{
			int num2 = Math.Min(num - unitsCount, 10);
			GameEntityGUID[] array = new GameEntityGUID[num2];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = this.GameEntityRepositoryService.GenerateGUID();
			}
			order.MilitiaUnitGuid = array;
			order.Create = true;
		}
		if (num < unitsCount)
		{
			Unit[] standardUnitsAsArray = city.Militia.StandardUnitsAsArray;
			int num3 = Math.Min(unitsCount - num, standardUnitsAsArray.Length);
			GameEntityGUID[] array2 = new GameEntityGUID[num3];
			for (int j = 0; j < array2.Length; j++)
			{
				array2[j] = standardUnitsAsArray[standardUnitsAsArray.Length - j - 1].GUID;
			}
			order.MilitiaUnitGuid = array2;
			order.Create = false;
		}
		return true;
	}

	private IEnumerator UpdateMilitiaProcessor(OrderUpdateMilitia order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityGuid, out gameEntity) || !(gameEntity is City))
		{
			yield break;
		}
		City city = gameEntity as City;
		if (!order.Create)
		{
			for (int index = 0; index < order.MilitiaUnitGuid.Length; index++)
			{
				Diagnostics.Assert(order.MilitiaUnitGuid[index].IsValid);
				Unit unit = ((City)gameEntity).Militia.StandardUnits.FirstOrDefault((Unit u) => u.GUID == order.MilitiaUnitGuid[index]);
				Diagnostics.Assert(unit != null);
				((City)gameEntity).Militia.RemoveUnit(unit);
				this.GameEntityRepositoryService.Unregister(unit);
				unit.Dispose();
			}
			yield break;
		}
		UnitDesign unitDesign = this.FindBestUnitDesignAvailableForMilitiaUnits();
		if (unitDesign == null)
		{
			Diagnostics.LogError("Cannot find a unit design for the militia unit(s) to create.");
			yield break;
		}
		for (int index2 = 0; index2 < order.MilitiaUnitGuid.Length; index2++)
		{
			this.CreateMilitiaUnitIntoGarrison(order.MilitiaUnitGuid[index2], unitDesign, city.Militia);
		}
		yield break;
	}

	private bool WoundUnitPreprocessor(OrderWoundUnit order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.UnitGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target unit game entity is not valid.");
			return false;
		}
		if (!(gameEntity is Unit))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a unit.");
			return false;
		}
		return true;
	}

	private IEnumerator WoundUnitProcessor(OrderWoundUnit order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.UnitGUID, out gameEntity))
		{
			Diagnostics.LogError("Order processing failed because the target unit game entity is not valid.");
			yield break;
		}
		if (!(gameEntity is Unit))
		{
			Diagnostics.LogError("Order processing failed because the target game entity is not a unit.");
			yield break;
		}
		Unit unit = gameEntity as Unit;
		this.WoundUnit(unit, order.Damage);
		yield break;
	}

	public static bool CanStartPillage(Army army, PointOfInterest pointOfInterest, bool checkDistance = true)
	{
		if (army == null || pointOfInterest == null)
		{
			return false;
		}
		if (army.IsNaval)
		{
			return false;
		}
		if (pointOfInterest.PointOfInterestImprovement == null)
		{
			return false;
		}
		if (pointOfInterest.CreepingNodeGUID != GameEntityGUID.Zero)
		{
			return false;
		}
		if (pointOfInterest.Region.City == null)
		{
			return false;
		}
		if (pointOfInterest.Type == "QuestLocation" || pointOfInterest.Type == "NavalQuestLocation")
		{
			return false;
		}
		if (pointOfInterest.Region.City.Empire == army.Empire)
		{
			return false;
		}
		if (!army.IsPrivateers)
		{
			DepartmentOfForeignAffairs agency = army.Empire.GetAgency<DepartmentOfForeignAffairs>();
			if (agency != null && !agency.IsEnnemy(pointOfInterest.Region.City.Empire) && !pointOfInterest.Region.City.Empire.SimulationObject.Tags.Contains(DiplomaticAbilityDefinition.BlackSpotVictim))
			{
				return false;
			}
		}
		if (pointOfInterest.SimulationObject.Tags.Contains(DepartmentOfDefense.PillageStatusDescriptor))
		{
			return false;
		}
		if (pointOfInterest.ArmyPillaging.IsValid && army.GUID != pointOfInterest.ArmyPillaging)
		{
			return false;
		}
		if (pointOfInterest.Type == "Village" && !pointOfInterest.SimulationObject.Tags.Contains("PacifiedVillage"))
		{
			return false;
		}
		DepartmentOfScience agency2 = army.Empire.GetAgency<DepartmentOfScience>();
		if (agency2 != null)
		{
			if (!agency2.CanPillage())
			{
				return false;
			}
		}
		else
		{
			IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
			if (!service.IsShared(DownloadableContent11.ReadOnlyName))
			{
				return false;
			}
		}
		if (checkDistance)
		{
			int distance = DepartmentOfDefense.worldPositionningServiceStatic.GetDistance(army.WorldPosition, pointOfInterest.WorldPosition);
			if (distance > 1)
			{
				return false;
			}
			if (distance > 0 && !DepartmentOfDefense.pathfindingServiceStatic.IsTransitionPassable(army.WorldPosition, pointOfInterest.WorldPosition, army.GenerateContext().MovementCapacities, OrderAttack.AttackFlags))
			{
				return false;
			}
		}
		return true;
	}

	public static bool IsPointOfInterestSuitableForPillage(PointOfInterest pointOfInterest)
	{
		return pointOfInterest != null && pointOfInterest.PointOfInterestImprovement != null && !(pointOfInterest.CreepingNodeGUID != GameEntityGUID.Zero) && pointOfInterest.Region.City != null && !(pointOfInterest.Type == "QuestLocation") && !(pointOfInterest.Type == "NavalQuestLocation") && (!(pointOfInterest.Type == "Village") || pointOfInterest.SimulationObject.Tags.Contains("PacifiedVillage"));
	}

	public static void StopPillage(Army army)
	{
		PointOfInterest pointOfInterest = null;
		IGameEntity gameEntity = null;
		if (DepartmentOfDefense.gameEntityRepositoryServiceStatic.TryGetValue(army.PillageTarget, out gameEntity))
		{
			pointOfInterest = (gameEntity as PointOfInterest);
		}
		DepartmentOfDefense.StopPillage(army, pointOfInterest);
	}

	public static void StopPillage(PointOfInterest pointOfInterest)
	{
		Army army = null;
		IGameEntity gameEntity = null;
		if (DepartmentOfDefense.gameEntityRepositoryServiceStatic.TryGetValue(pointOfInterest.ArmyPillaging, out gameEntity))
		{
			army = (gameEntity as Army);
		}
		DepartmentOfDefense.StopPillage(army, pointOfInterest);
	}

	public static void StopPillage(Army army, PointOfInterest pointOfInterest)
	{
		if (army != null)
		{
			army.SetPillageTarget(GameEntityGUID.Zero);
			army.Refresh(false);
		}
		if (pointOfInterest != null)
		{
			pointOfInterest.SetArmyPillaging(GameEntityGUID.Zero);
			pointOfInterest.Refresh(false);
		}
	}

	public static int GetRemainingTurnToPillage(Army army, PointOfInterest pointOfInterest)
	{
		float propertyValue = pointOfInterest.GetPropertyValue(SimulationProperties.PillageDefense);
		float propertyValue2 = army.GetPropertyValue(SimulationProperties.PillagePower);
		int result = -1;
		if (propertyValue2 > 0f)
		{
			result = Mathf.CeilToInt(propertyValue / propertyValue2);
		}
		return result;
	}

	public int GetRemainingTurnToPillage(Army army)
	{
		IGameEntity gameEntity = null;
		if (this.GameEntityRepositoryService.TryGetValue(army.PillageTarget, out gameEntity))
		{
			PointOfInterest pointOfInterest = gameEntity as PointOfInterest;
			if (pointOfInterest != null)
			{
				return DepartmentOfDefense.GetRemainingTurnToPillage(army, pointOfInterest);
			}
		}
		return 0;
	}

	public int GetRemainingTurnToPillage(PointOfInterest pointOfInterest)
	{
		IGameEntity gameEntity = null;
		if (this.GameEntityRepositoryService.TryGetValue(pointOfInterest.ArmyPillaging, out gameEntity))
		{
			Army army = gameEntity as Army;
			if (army != null)
			{
				return DepartmentOfDefense.GetRemainingTurnToPillage(army, pointOfInterest);
			}
		}
		return 0;
	}

	private void StartPillage(Army army, PointOfInterest pointOfInterest)
	{
		if (DepartmentOfDefense.CanStartPillage(army, pointOfInterest, true))
		{
			pointOfInterest.SetArmyPillaging(army.GUID);
			army.SetPillageTarget(pointOfInterest.GUID);
			if (this.EventService != null && pointOfInterest.Region.City != null)
			{
				global::Empire empire = pointOfInterest.Region.City.Empire;
				this.EventService.Notify(new EventPillageStarted(empire, army, pointOfInterest));
			}
		}
	}

	public DepartmentOfDefense.CheckRetrofitPrerequisitesResult CheckRetrofitPrerequisites(Unit unit, IConstructionCost[] costs)
	{
		if (costs == null || costs.Length == 0)
		{
			return DepartmentOfDefense.CheckRetrofitPrerequisitesResult.Ok;
		}
		UnitProfile unitProfile = unit.UnitDesign as UnitProfile;
		if (unitProfile != null && unitProfile.IsHero)
		{
			DepartmentOfIntelligence agency = base.Empire.GetAgency<DepartmentOfIntelligence>();
			IGarrison garrison;
			InfiltrationProcessus.InfiltrationState infiltrationState;
			if (agency != null && agency.TryGetGarrisonForSpy(unit.GUID, out garrison, out infiltrationState))
			{
				InfiltrationProcessus.InfiltrationState infiltrationState2 = infiltrationState;
				if (infiltrationState2 == InfiltrationProcessus.InfiltrationState.OnGoing || infiltrationState2 == InfiltrationProcessus.InfiltrationState.Infiltrated)
				{
					return DepartmentOfDefense.CheckRetrofitPrerequisitesResult.RegionDoesntBelongToUs;
				}
			}
			if (unit.Garrison == null)
			{
				return DepartmentOfDefense.CheckRetrofitPrerequisitesResult.Ok;
			}
		}
		IWorldPositionable worldPositionable = unit.Garrison as IWorldPositionable;
		if (worldPositionable != null)
		{
			if (!worldPositionable.WorldPosition.IsValid)
			{
				return DepartmentOfDefense.CheckRetrofitPrerequisitesResult.WorldPositionIsNotValid;
			}
			if (unit.Garrison != null)
			{
				Fortress fortress = unit.Garrison as Fortress;
				if (fortress == null || fortress.Occupant.Index != base.Empire.Index)
				{
					Region region = this.WorldPositionningService.GetRegion(worldPositionable.WorldPosition);
					if (region == null || (!region.BelongToEmpire(base.Empire as global::Empire) && !unit.CheckUnitAbility(UnitAbility.ReadonlyRapidMutation, -1)))
					{
						Army army = unit.Garrison as Army;
						if (army == null || army.Hero == null)
						{
							return DepartmentOfDefense.CheckRetrofitPrerequisitesResult.RegionDoesntBelongToUs;
						}
						if (!army.Hero.CheckUnitAbility(UnitAbility.ReadonlyPortableForge, -1) || !army.PortableForgeActive)
						{
							return DepartmentOfDefense.CheckRetrofitPrerequisitesResult.RegionDoesntBelongToUs;
						}
					}
				}
			}
		}
		if (unit.Garrison != null)
		{
			City city = unit.Garrison as City;
			if (city != null)
			{
				if (city.BesiegingEmpireIndex >= 0)
				{
					bool flag = true;
					for (int i = 0; i < costs.Length; i++)
					{
						flag &= (costs[i].ResourceName == DepartmentOfTheTreasury.Resources.EmpireMoney);
						if (!flag)
						{
							return DepartmentOfDefense.CheckRetrofitPrerequisitesResult.GarrisonCityIsUnderSiege;
						}
					}
				}
			}
			else
			{
				Army army2 = unit.Garrison as Army;
				if (army2 != null)
				{
					if (army2.IsLocked)
					{
						return DepartmentOfDefense.CheckRetrofitPrerequisitesResult.GarrisonArmyIsLocked;
					}
					if (army2.IsInEncounter)
					{
						return DepartmentOfDefense.CheckRetrofitPrerequisitesResult.GarrisonArmyIsInEncounter;
					}
				}
			}
		}
		return DepartmentOfDefense.CheckRetrofitPrerequisitesResult.Ok;
	}

	public bool CheckWhetherUnitDesignIsOutdated(UnitDesign unitDesign)
	{
		foreach (UnitDesign unitDesign2 in ((IUnitDesignDatabase)this).GetAvailableUnitDesignsAsEnumerable())
		{
			if (unitDesign2.Model == unitDesign.Model)
			{
				return unitDesign.ModelRevision < unitDesign2.ModelRevision;
			}
		}
		return false;
	}

	public bool CheckWhetherUnitDesignMatchUnitBodyEquipmentSet(UnitDesign unitDesign)
	{
		return this.CheckWhetherUnitDesignMatchUnitBodyEquipmentSet(unitDesign.UnitBodyDefinition, unitDesign.UnitEquipmentSet);
	}

	public bool CheckWhetherUnitDesignMatchUnitBodyEquipmentSet(UnitBodyDefinition unitBodyDefinition, UnitEquipmentSet unitEquipmentSet)
	{
		if (unitBodyDefinition == null)
		{
			throw new ArgumentNullException("unitBodyDefinition");
		}
		if (unitEquipmentSet == null)
		{
			throw new ArgumentNullException("unitEquipmentSet");
		}
		if (unitBodyDefinition.UnitEquipmentSet == null)
		{
			Diagnostics.LogError("The unit body has no unit equipment set.");
			return false;
		}
		if (unitEquipmentSet.Slots == null || unitBodyDefinition.UnitEquipmentSet.Slots == null)
		{
			return unitBodyDefinition.UnitEquipmentSet.Slots == unitEquipmentSet.Slots;
		}
		for (int i = 0; i < unitBodyDefinition.UnitEquipmentSet.Slots.Length; i++)
		{
			if (unitEquipmentSet.Slots[i].Name != unitBodyDefinition.UnitEquipmentSet.Slots[i].Name)
			{
				return false;
			}
		}
		return unitBodyDefinition.UnitEquipmentSet.Slots.Length == unitEquipmentSet.Slots.Length;
	}

	public void GenerateUnitEquipmentDefault(UnitEquipmentSet defaultEquipmentSet, UnitEquipmentSet newEquipmentSet)
	{
		DepartmentOfDefense.<GenerateUnitEquipmentDefault>c__AnonStorey95A <GenerateUnitEquipmentDefault>c__AnonStorey95A = new DepartmentOfDefense.<GenerateUnitEquipmentDefault>c__AnonStorey95A();
		<GenerateUnitEquipmentDefault>c__AnonStorey95A.newEquipmentSet = newEquipmentSet;
		int index;
		for (index = 0; index < <GenerateUnitEquipmentDefault>c__AnonStorey95A.newEquipmentSet.Slots.Length; index++)
		{
			StaticString itemName = <GenerateUnitEquipmentDefault>c__AnonStorey95A.newEquipmentSet.Slots[index].ItemName;
			if (StaticString.IsNullOrEmpty(itemName))
			{
				UnitEquipmentSet.Slot slot = defaultEquipmentSet.Slots.FirstOrDefault((UnitEquipmentSet.Slot match) => match.Name == <GenerateUnitEquipmentDefault>c__AnonStorey95A.newEquipmentSet.Slots[index].Name);
				if (!StaticString.IsNullOrEmpty(slot.ItemName))
				{
					itemName = slot.ItemName;
					bool flag = true;
					for (int i = 0; i < defaultEquipmentSet.Slots.Length; i++)
					{
						if (i != index)
						{
							if (defaultEquipmentSet.Slots[i].ItemName == itemName && i < <GenerateUnitEquipmentDefault>c__AnonStorey95A.newEquipmentSet.Slots.Length && !StaticString.IsNullOrEmpty(<GenerateUnitEquipmentDefault>c__AnonStorey95A.newEquipmentSet.Slots[i].ItemName))
							{
								flag = false;
								break;
							}
						}
					}
					if (flag)
					{
						for (int j = 0; j < defaultEquipmentSet.Slots.Length; j++)
						{
							if (defaultEquipmentSet.Slots[j].ItemName == itemName)
							{
								<GenerateUnitEquipmentDefault>c__AnonStorey95A.newEquipmentSet.Slots[j].ItemName = itemName;
							}
						}
					}
				}
			}
		}
	}

	public ConstructionCost[] GetRetrofitCosts(Unit[] units)
	{
		List<ConstructionCost> list = new List<ConstructionCost>();
		for (int i = 0; i < units.Length; i++)
		{
			Unit unit = units[i];
			if (unit != null)
			{
				Diagnostics.Assert(unit.UnitDesign != null);
				UnitDesign unitDesign = this.UnitDesignDatabase.UserDefinedUnitDesigns.FirstOrDefault((UnitDesign design) => design.Model == unit.UnitDesign.Model);
				if (unitDesign != null)
				{
					if (unitDesign.ModelRevision > unit.UnitDesign.ModelRevision)
					{
						this.GetRetrofitCosts(unit, unitDesign, ref list);
					}
				}
			}
		}
		return list.ToArray();
	}

	public ConstructionCost[] GetRetrofitCosts(Unit unit, UnitDesign newestUnitDesign)
	{
		List<ConstructionCost> list = new List<ConstructionCost>();
		this.GetRetrofitCosts(unit, newestUnitDesign, ref list);
		return list.ToArray();
	}

	public UnitDesign GetEmbarkedUnitDesignFor(Unit unit)
	{
		UnitDesign result;
		if (unit.IsHero())
		{
			result = this.hiddenUnitDesigns.FirstOrDefault((UnitDesign unitDesign) => "UnitDesignEmbarkedHeroVessel#1".Equals(unitDesign.Name));
		}
		else
		{
			result = this.hiddenUnitDesigns.FirstOrDefault((UnitDesign unitDesign) => "UnitDesignEmbarkedUnitVessel#1".Equals(unitDesign.Name));
		}
		return result;
	}

	public bool IsUnitEquipmentSetValid(UnitBodyDefinition unitBodyDefinition, UnitEquipmentSet unitEquipmentSet)
	{
		if (!this.CheckWhetherUnitDesignMatchUnitBodyEquipmentSet(unitBodyDefinition, unitEquipmentSet))
		{
			return false;
		}
		int index;
		for (index = 0; index < unitBodyDefinition.UnitBodyStances.Length; index++)
		{
			if (unitBodyDefinition.UnitBodyStances[index].Attachments != null && unitBodyDefinition.UnitBodyStances[index].Attachments.Length != 0)
			{
				int num = 0;
				int jndex;
				for (jndex = 0; jndex < unitBodyDefinition.UnitBodyStances[index].Attachments.Length; jndex++)
				{
					if (unitEquipmentSet.Slots.Any((UnitEquipmentSet.Slot match) => match.Name == unitBodyDefinition.UnitBodyStances[index].Attachments[jndex].Slot && !StaticString.IsNullOrEmpty(match.ItemName)))
					{
						num++;
					}
				}
				if (num > 0 && num == unitBodyDefinition.UnitBodyStances[index].Attachments.Length)
				{
					return true;
				}
			}
		}
		return false;
	}

	private void AddUnitBodyDefinition(UnitBodyDefinition unitBodyDefinition)
	{
		if (this.availableUnitBodyDefinitions.Any((UnitBodyDefinition body) => body.Name == unitBodyDefinition.Name))
		{
			return;
		}
		this.availableUnitBodyDefinitions.Add(unitBodyDefinition);
		if (this.AvailableUnitBodyChanged != null)
		{
			this.AvailableUnitBodyChanged(this, new ConstructibleElementEventArgs(unitBodyDefinition));
		}
	}

	private UnitDesign AddUnitDesign(UnitDesign unitDesign, uint modelId, DepartmentOfDefense.UnitDesignState unitDesignState)
	{
		if (unitDesign == null)
		{
			throw new ArgumentNullException("unitDesign");
		}
		if (modelId >= this.nextAvailableUnitDesignModelId)
		{
			this.nextAvailableUnitDesignModelId = modelId + 1u;
		}
		if (unitDesignState == DepartmentOfDefense.UnitDesignState.Available)
		{
			if (unitDesign.Hidden)
			{
				Diagnostics.LogWarning("Unit design '{0}' cannot be added with state 'Available' because it is 'Hidden'. Changing state to 'Hidden'.", new object[]
				{
					unitDesign.Name
				});
				unitDesignState = DepartmentOfDefense.UnitDesignState.Hidden;
			}
			else if (unitDesign.Tags.Contains(TradableUnit.ReadOnlyMercenary))
			{
				Diagnostics.LogWarning("Unit design '{0}' cannot be added with state 'Available' because it has the 'Mercenay' tag. Changing state to 'Hidden'.", new object[]
				{
					unitDesign.Name
				});
				unitDesignState = DepartmentOfDefense.UnitDesignState.Hidden;
			}
			else if (unitDesign.Tags.Contains(DepartmentOfTheInterior.TagConvertedVillageUnit))
			{
				Diagnostics.LogWarning("Unit design '{0}' cannot be added with state 'Available' because it has the 'ConvertedVillageUnit' tag. Changing state to 'Hidden'.", new object[]
				{
					unitDesign.Name
				});
				unitDesignState = DepartmentOfDefense.UnitDesignState.Hidden;
			}
			else if (unitDesign.Tags.Contains(DepartmentOfDefense.TagUndead))
			{
				Diagnostics.LogWarning("Unit design '{0}' cannot be added with state 'Available' because it has the 'Undead' tag. Changing state to 'Hidden'.", new object[]
				{
					unitDesign.Name
				});
				unitDesignState = DepartmentOfDefense.UnitDesignState.Hidden;
			}
		}
		UnitDesign unitDesign2 = null;
		switch (unitDesignState)
		{
		case DepartmentOfDefense.UnitDesignState.Available:
		{
			Diagnostics.Assert(this.availableUnitDesigns != null);
			UnitDesign unitDesign3 = this.availableUnitDesigns.Find((UnitDesign match) => match.Model == unitDesign.Model);
			if (unitDesign3 == null)
			{
				unitDesign3 = this.hiddenUnitDesigns.Find((UnitDesign match) => match.Model == unitDesign.Model);
			}
			if (unitDesign3 != null)
			{
				if (unitDesign3.ModelRevision >= unitDesign.ModelRevision)
				{
					return unitDesign3;
				}
				this.RemoveUnitDesign(unitDesign3.Model, unitDesign3.ModelRevision);
			}
			unitDesign2 = (UnitDesign)unitDesign.Clone();
			unitDesign2.Model = modelId;
			Diagnostics.Assert(this.availableUnitDesigns != null);
			this.availableUnitDesigns.Add(unitDesign2);
			Unit unit = DepartmentOfDefense.CreateUnitByDesign(1UL, unitDesign2);
			Diagnostics.Assert(unit != null);
			unit.Rename(unitDesign2.FullName + "#" + unit.GUID);
			unit.SimulationObject.ModifierForward = ModifierForwardType.ChildrenOnly;
			this.temporaryGarrison.AddChild(unit);
			unitDesign2.Context = unit;
			this.NotifyUnitDesignDatabaseChange(unitDesign2, UnitDesignDatabaseChangeEventArgs.UnitDesignDatabaseChangeAction.MoveToAvailable);
			break;
		}
		case DepartmentOfDefense.UnitDesignState.Outdated:
		{
			Diagnostics.Assert(this.outdatedUnitDesigns != null);
			UnitDesign unitDesign4 = this.outdatedUnitDesigns.Find((UnitDesign match) => match.Model == unitDesign.Model && match.ModelRevision == unitDesign.ModelRevision);
			if (unitDesign4 != null)
			{
				return unitDesign4;
			}
			unitDesign2 = (UnitDesign)unitDesign.Clone();
			unitDesign2.Model = modelId;
			this.outdatedUnitDesigns.Add(unitDesign2);
			if (unitDesign2.Context == null)
			{
				Unit unit2 = DepartmentOfDefense.CreateUnitByDesign(this.GameEntityRepositoryService.GenerateGUID(), unitDesign2);
				Diagnostics.Assert(unit2 != null);
				unit2.Rename(unitDesign2.Name + unit2.GUID);
				unit2.SimulationObject.ModifierForward = ModifierForwardType.ChildrenOnly;
				this.temporaryGarrison.AddChild(unit2);
				unitDesign2.Context = unit2;
			}
			this.NotifyUnitDesignDatabaseChange(unitDesign2, UnitDesignDatabaseChangeEventArgs.UnitDesignDatabaseChangeAction.MoveToObsolet);
			break;
		}
		case DepartmentOfDefense.UnitDesignState.Hidden:
		{
			Diagnostics.Assert(this.hiddenUnitDesigns != null);
			UnitDesign unitDesign5 = this.hiddenUnitDesigns.Find((UnitDesign match) => match.Model == unitDesign.Model && match.ModelRevision == unitDesign.ModelRevision);
			if (unitDesign5 != null)
			{
				return unitDesign5;
			}
			unitDesign2 = (UnitDesign)unitDesign.Clone();
			unitDesign2.Model = modelId;
			this.hiddenUnitDesigns.Add(unitDesign2);
			this.NotifyUnitDesignDatabaseChange(unitDesign2, UnitDesignDatabaseChangeEventArgs.UnitDesignDatabaseChangeAction.MoveToHidden);
			break;
		}
		}
		if (unitDesign2 != null && this.AvailableUnitDesignChanged != null)
		{
			this.AvailableUnitDesignChanged(this, new ConstructibleElementEventArgs(unitDesign2));
		}
		return unitDesign2;
	}

	private void ComputeNewEquipement(UnitEquipmentSet newEquipmentSet, ref List<StaticString> oldEquipedItems, ref List<StaticString> newStuff)
	{
		List<StaticString> list = new List<StaticString>();
		for (int i = 0; i < newEquipmentSet.Slots.Length; i++)
		{
			if (newEquipmentSet.Slots[i].ItemName != null)
			{
				if (!list.Contains(newEquipmentSet.Slots[i].ItemName))
				{
					list.Add(newEquipmentSet.Slots[i].ItemName);
					string x = newEquipmentSet.Slots[i].ItemName.ToString().Split(DepartmentOfDefense.ItemSeparators)[0];
					if (oldEquipedItems.Contains(x))
					{
						oldEquipedItems.Remove(x);
					}
					else
					{
						newStuff.Add(x);
					}
				}
			}
		}
	}

	private void DeleteUnitDesign(uint unitDesignModel, uint unitDesignModelRevision)
	{
		UnitDesign unitDesign = this.GetOutdatedUnitDesignsAsEnumerable().FirstOrDefault((UnitDesign match) => match.Model == unitDesignModel && match.ModelRevision == unitDesignModelRevision);
		if (unitDesign == null)
		{
			Diagnostics.LogError("Can't delete unit design {0}.{1} because it does not exist or is not outdated.");
			return;
		}
		Diagnostics.Assert(this.outdatedUnitDesigns != null);
		this.outdatedUnitDesigns.Remove(unitDesign);
		this.NotifyUnitDesignDatabaseChange(unitDesign, UnitDesignDatabaseChangeEventArgs.UnitDesignDatabaseChangeAction.Destroyed);
		Unit unit = unitDesign.Context as Unit;
		unitDesign.Context = null;
		Diagnostics.Assert(this.temporaryGarrison != null);
		this.temporaryGarrison.RemoveChild(unit);
		unit.Dispose();
	}

	private IEnumerator GameClientState_Turn_End_CleanUnitDesignDatabase(string context, string name)
	{
		Diagnostics.Assert(this.outdatedUnitDesigns != null);
		for (int index = 0; index < this.outdatedUnitDesigns.Count; index++)
		{
			UnitDesign outdatedUnitDesign = this.outdatedUnitDesigns[index];
			if (outdatedUnitDesign.RemoveOutdated)
			{
				if (!this.IsUnitDesignModelExistInArmies(outdatedUnitDesign))
				{
					if (!this.IsUnitDesignModelExistInCityConstructionQueue(outdatedUnitDesign))
					{
						if (!this.IsUnitDesignModelExistInCityGarrison(outdatedUnitDesign))
						{
							if (!this.IsUnitModelDesignExistInConvertedVillage(outdatedUnitDesign))
							{
								if (!this.IsUnitModelDeisgnExistInColossusSpawnCommand(outdatedUnitDesign))
								{
									if (!this.IsUnitModelDesignExistInOwnedFortresses(outdatedUnitDesign))
									{
										if (!this.IsUnitDesignModelExistInCampGarrison(outdatedUnitDesign))
										{
											if (!this.IsUnitDesignModelExistInKaijuGarrison(outdatedUnitDesign))
											{
												this.DeleteUnitDesign(outdatedUnitDesign.Model, outdatedUnitDesign.ModelRevision);
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
		yield break;
	}

	private void GetRetrofitCosts(Unit unit, UnitDesign newestUnitDesign, ref List<ConstructionCost> retrofitCosts)
	{
		DepartmentOfDefense.<GetRetrofitCosts>c__AnonStorey964 <GetRetrofitCosts>c__AnonStorey = new DepartmentOfDefense.<GetRetrofitCosts>c__AnonStorey964();
		<GetRetrofitCosts>c__AnonStorey.unitRetrofitCosts = new List<ConstructionCost>();
		if (newestUnitDesign.Costs != null && newestUnitDesign.Costs.Length > 0)
		{
			for (int i = 0; i < newestUnitDesign.Costs.Length; i++)
			{
				IConstructionCost constructionCost = newestUnitDesign.Costs[i];
				StaticString resourceName2 = DepartmentOfTheTreasury.Resources.EmpireMoney;
				float value = 0f;
				float num;
				if (constructionCost.ResourceName == DepartmentOfTheTreasury.Resources.Production)
				{
					num = DepartmentOfTheTreasury.GetProductionCostWithBonus(base.Empire, newestUnitDesign, constructionCost, false);
				}
				else
				{
					num = constructionCost.GetValue(base.Empire);
				}
				if (!DepartmentOfTheTreasury.TryConvertCostTo("Retrofit", constructionCost.ResourceName, num, base.Empire, out value))
				{
					resourceName2 = constructionCost.ResourceName;
					value = num;
				}
				<GetRetrofitCosts>c__AnonStorey.unitRetrofitCosts.Add(new ConstructionCost(resourceName2, value, true, false));
			}
		}
		if (unit.UnitDesign.Costs != null && unit.UnitDesign.Costs.Length > 0)
		{
			for (int j = 0; j < unit.UnitDesign.Costs.Length; j++)
			{
				IConstructionCost constructionCost2 = unit.UnitDesign.Costs[j];
				StaticString resourceName = DepartmentOfTheTreasury.Resources.EmpireMoney;
				float num2 = 0f;
				float num3;
				if (constructionCost2.ResourceName == DepartmentOfTheTreasury.Resources.Production)
				{
					num3 = DepartmentOfTheTreasury.GetProductionCostWithBonus(base.Empire, unit.UnitDesign, constructionCost2, false);
				}
				else
				{
					num3 = constructionCost2.GetValue(base.Empire);
				}
				if (!DepartmentOfTheTreasury.TryConvertCostTo("Retrofit", constructionCost2.ResourceName, num3, base.Empire, out num2))
				{
					resourceName = constructionCost2.ResourceName;
					num2 = num3;
				}
				ConstructionCost constructionCost3 = <GetRetrofitCosts>c__AnonStorey.unitRetrofitCosts.FirstOrDefault((ConstructionCost match) => match.ResourceName == resourceName);
				if (constructionCost3 != null)
				{
					constructionCost3.Value -= num2;
					if (constructionCost3.Value <= 0f)
					{
						<GetRetrofitCosts>c__AnonStorey.unitRetrofitCosts.Remove(constructionCost3);
					}
				}
			}
		}
		if (unit.Garrison != null && unit.Garrison.Hero != null && unit.Garrison.Hero.IsSkillUnlocked("HeroSkillLeaderMap24"))
		{
			ConstructionCost constructionCost4 = <GetRetrofitCosts>c__AnonStorey.unitRetrofitCosts.FirstOrDefault((ConstructionCost match) => match.ResourceName == DepartmentOfTheTreasury.Resources.EmpireMoney);
			if (constructionCost4 != null)
			{
				constructionCost4.Value *= unit.Garrison.Hero.GetPropertyValue(SimulationProperties.ArmyRetrofitCostMultiplier);
			}
		}
		int index;
		for (index = 0; index < <GetRetrofitCosts>c__AnonStorey.unitRetrofitCosts.Count; index++)
		{
			ConstructionCost constructionCost5 = retrofitCosts.FirstOrDefault((ConstructionCost match) => match.ResourceName == <GetRetrofitCosts>c__AnonStorey.unitRetrofitCosts[index].ResourceName);
			if (constructionCost5 != null)
			{
				constructionCost5.Value += <GetRetrofitCosts>c__AnonStorey.unitRetrofitCosts[index].Value;
			}
			else
			{
				retrofitCosts.Add(new ConstructionCost(<GetRetrofitCosts>c__AnonStorey.unitRetrofitCosts[index].ResourceName, <GetRetrofitCosts>c__AnonStorey.unitRetrofitCosts[index].Value, true, false));
			}
		}
	}

	private bool IsUnitDesignModelExistInArmies(UnitDesign unitDesign)
	{
		if (unitDesign == null)
		{
			throw new ArgumentNullException("unitDesign");
		}
		Diagnostics.Assert(this.armies != null);
		for (int i = 0; i < this.armies.Count; i++)
		{
			Army army = this.armies[i];
			Diagnostics.Assert(army != null && army.Units != null);
			foreach (Unit unit in army.Units)
			{
				Diagnostics.Assert(unit != null && unit.UnitDesign != null);
				if (unit.UnitDesign.Model == unitDesign.Model && unit.UnitDesign.ModelRevision == unitDesign.ModelRevision)
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool IsUnitDesignModelExistInCityConstructionQueue(UnitDesign unitDesign)
	{
		if (unitDesign == null)
		{
			throw new ArgumentNullException("unitDesign");
		}
		DepartmentOfTheInterior agency = base.Empire.GetAgency<DepartmentOfTheInterior>();
		DepartmentOfIndustry agency2 = base.Empire.GetAgency<DepartmentOfIndustry>();
		Diagnostics.Assert(agency != null && agency2 != null);
		for (int i = 0; i < agency.Cities.Count; i++)
		{
			City gameEntity = agency.Cities[i];
			ConstructionQueue constructionQueue = agency2.GetConstructionQueue(gameEntity);
			Diagnostics.Assert(constructionQueue != null);
			if (constructionQueue.Contains((Construction match) => match.ConstructibleElement is UnitDesign && (match.ConstructibleElement as UnitDesign).Model == unitDesign.Model && (match.ConstructibleElement as UnitDesign).ModelRevision == unitDesign.ModelRevision))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsUnitDesignModelExistInCityGarrison(UnitDesign unitDesign)
	{
		if (unitDesign == null)
		{
			throw new ArgumentNullException("unitDesign");
		}
		DepartmentOfTheInterior agency = base.Empire.GetAgency<DepartmentOfTheInterior>();
		Diagnostics.Assert(agency != null);
		for (int i = 0; i < agency.Cities.Count; i++)
		{
			City city = agency.Cities[i];
			foreach (Unit unit in city.Units)
			{
				Diagnostics.Assert(unit != null && unit.UnitDesign != null);
				if (unit.UnitDesign.Model == unitDesign.Model && unit.UnitDesign.ModelRevision == unitDesign.ModelRevision)
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool IsUnitModelDesignExistInConvertedVillage(UnitDesign unitDesign)
	{
		if (unitDesign == null)
		{
			throw new ArgumentNullException("unitDesign");
		}
		MajorEmpire majorEmpire = base.Empire as MajorEmpire;
		if (majorEmpire == null)
		{
			return false;
		}
		for (int i = 0; i < majorEmpire.ConvertedVillages.Count; i++)
		{
			Village village = majorEmpire.ConvertedVillages[i];
			foreach (Unit unit in village.Units)
			{
				Diagnostics.Assert(unit != null && unit.UnitDesign != null);
				if (unit.UnitDesign.Model == unitDesign.Model && unit.UnitDesign.ModelRevision == unitDesign.ModelRevision)
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool IsUnitModelDesignExistInOwnedFortresses(UnitDesign unitDesign)
	{
		if (unitDesign == null)
		{
			throw new ArgumentNullException("unitDesign");
		}
		DepartmentOfTheInterior agency = base.Empire.GetAgency<DepartmentOfTheInterior>();
		if (agency != null)
		{
			for (int i = 0; i < agency.OccupiedFortresses.Count; i++)
			{
				Fortress fortress = agency.OccupiedFortresses[i];
				foreach (Unit unit in fortress.Units)
				{
					Diagnostics.Assert(unit != null && unit.UnitDesign != null);
					if (unit.UnitDesign.Model == unitDesign.Model && unit.UnitDesign.ModelRevision == unitDesign.ModelRevision)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private bool IsUnitDesignModelExistInCampGarrison(UnitDesign unitDesign)
	{
		if (unitDesign == null)
		{
			throw new ArgumentNullException("unitDesign");
		}
		DepartmentOfTheInterior agency = base.Empire.GetAgency<DepartmentOfTheInterior>();
		Diagnostics.Assert(agency != null);
		for (int i = 0; i < agency.Cities.Count; i++)
		{
			City city = agency.Cities[i];
			if (city != null && city.Camp != null)
			{
				Camp camp = city.Camp;
				foreach (Unit unit in camp.Units)
				{
					Diagnostics.Assert(unit != null && unit.UnitDesign != null);
					if (unit.UnitDesign.Model == unitDesign.Model && unit.UnitDesign.ModelRevision == unitDesign.ModelRevision)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private bool IsUnitDesignModelExistInKaijuGarrison(UnitDesign unitDesign)
	{
		if (unitDesign == null)
		{
			throw new ArgumentNullException("unitDesign");
		}
		MajorEmpire majorEmpire = base.Empire as MajorEmpire;
		if (majorEmpire == null)
		{
			return false;
		}
		for (int i = 0; i < majorEmpire.TamedKaijus.Count; i++)
		{
			Kaiju kaiju = majorEmpire.TamedKaijus[i];
			foreach (Unit unit in kaiju.KaijuGarrison.Units)
			{
				Diagnostics.Assert(unit != null && unit.UnitDesign != null);
				if (unit.UnitDesign.Model == unitDesign.Model && unit.UnitDesign.ModelRevision == unitDesign.ModelRevision)
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool IsUnitModelDeisgnExistInColossusSpawnCommand(UnitDesign unitDesign)
	{
		if (this.delayedSolitaryUnitSpawnCommands != null)
		{
			for (int i = 0; i < this.delayedSolitaryUnitSpawnCommands.Count; i++)
			{
				if (this.delayedSolitaryUnitSpawnCommands[i].UnitDesign != null && this.delayedSolitaryUnitSpawnCommands[i].UnitDesign.Model == unitDesign.Model && this.delayedSolitaryUnitSpawnCommands[i].UnitDesign.ModelRevision == unitDesign.ModelRevision)
				{
					return true;
				}
			}
		}
		return false;
	}

	private void RemoveUnitBodyDefinition(UnitBodyDefinition unitBodyDefinition)
	{
		this.availableUnitBodyDefinitions.RemoveAll((UnitBodyDefinition body) => body.Name == unitBodyDefinition.Name);
		if (this.AvailableUnitBodyChanged != null)
		{
			this.AvailableUnitBodyChanged(this, new ConstructibleElementEventArgs(null));
		}
	}

	private void RemoveUnitDesign(uint unitDesignModel, uint unitDesignModelRevision)
	{
		UnitDesign unitDesign = ((IUnitDesignDatabase)this).GetAvailableUnitDesignsAsEnumerable().FirstOrDefault((UnitDesign design) => design.Model == unitDesignModel && design.ModelRevision == unitDesignModelRevision);
		if (unitDesign == null)
		{
			return;
		}
		Diagnostics.Assert(this.availableUnitDesigns != null);
		if (this.availableUnitDesigns.Remove(unitDesign))
		{
			this.AddUnitDesign(unitDesign, unitDesign.Model, DepartmentOfDefense.UnitDesignState.Outdated);
		}
		else if (this.outdatedUnitDesigns.Contains(unitDesign))
		{
			Diagnostics.LogWarning("[TODO] We are trying to remove an outdated unit design, we need to check wether the unit design is not used anymore before delete it.");
		}
		else
		{
			Diagnostics.LogWarning("[TODO] We are trying to remove an unknown unit design.");
		}
		if (this.AvailableUnitDesignChanged != null)
		{
			this.AvailableUnitDesignChanged(this, new ConstructibleElementEventArgs(null));
		}
	}

	private void NotifyUnitDesignDatabaseChange(UnitDesign unitDesign, UnitDesignDatabaseChangeEventArgs.UnitDesignDatabaseChangeAction action)
	{
		if (this.UnitDesignDatabaseChanged != null)
		{
			this.UnitDesignDatabaseChanged(this, new UnitDesignDatabaseChangeEventArgs(base.Empire.Index, unitDesign, action));
		}
	}

	[UnitTestMethod("Game", UnitTestMethodAttribute.Scope.Game)]
	public static void UnitBench_CreateUnitVsRecycleUnit(UnitTestResult unitTestResult)
	{
		unitTestResult.State = UnitTestResult.UnitTestState.Running;
		unitTestResult.TestMessage = "This test needs the game to be launched to run";
		IGameService service = Services.GetService<IGameService>();
		if (service == null)
		{
			unitTestResult.State = UnitTestResult.UnitTestState.Failed;
			return;
		}
		global::Game game = service.Game as global::Game;
		if (game == null)
		{
			unitTestResult.State = UnitTestResult.UnitTestState.Failed;
			return;
		}
		DepartmentOfDefense agency = game.Empires[0].GetAgency<DepartmentOfDefense>();
		if (agency == null)
		{
			unitTestResult.State = UnitTestResult.UnitTestState.Failed;
			return;
		}
		IEnumerator<UnitDesign> enumerator = agency.UnitDesignDatabase.UserDefinedUnitDesigns.GetEnumerator();
		enumerator.MoveNext();
		UnitDesign unitDesign = enumerator.Current;
		enumerator.MoveNext();
		UnitDesign unitDesign2 = enumerator.Current;
		if (unitDesign == null || unitDesign2 == null)
		{
			unitTestResult.TestMessage = "Failed to find unit designs in empire 0";
			unitTestResult.State = UnitTestResult.UnitTestState.Failed;
			return;
		}
		int num = 5;
		while (--num >= 0)
		{
			Unit unit = agency.CreateDummyUnitInTemporaryGarrison();
			DepartmentOfDefense.ApplyUnitDesignToUnit(unit, unitDesign);
			agency.DisposeDummyUnit(unit);
			Unit unit2 = agency.CreateDummyUnitInTemporaryGarrison();
			DepartmentOfDefense.ApplyUnitDesignToUnit(unit2, unitDesign2);
			agency.DisposeDummyUnit(unit2);
		}
		Unit unit3 = agency.CreateDummyUnitInTemporaryGarrison();
		int num2 = 5;
		while (--num2 >= 0)
		{
			DepartmentOfDefense.ReplaceUnitDesign(unit3, unitDesign);
			DepartmentOfDefense.ReplaceUnitDesign(unit3, unitDesign2);
		}
		agency.DisposeDummyUnit(unit3);
		unitTestResult.TestMessage = "Success";
		unitTestResult.State = UnitTestResult.UnitTestState.Success;
	}

	public ReadOnlyCollection<Army> Armies
	{
		get
		{
			if (this.readOnlyArmies == null)
			{
				this.readOnlyArmies = this.armies.AsReadOnly();
			}
			return this.readOnlyArmies;
		}
	}

	[Ancillary]
	public IGameEntityRepositoryService GameEntityRepositoryService { get; set; }

	public bool LandUnitBuildOrPurchased { get; set; }

	public DepartmentOfScience.ConstructibleElement.State TechnologyDefinitionShipState { get; private set; }

	public int TurnWhenLastBegun { get; set; }

	public int TurnWhenLastUnshiftedUnits { get; set; }

	public int TurnWhenLastImmolatedUnits { get; set; }

	[Ancillary]
	private IEncounterRepositoryService EncounterRepositoryService { get; set; }

	[Service]
	private IGameService GameService { get; set; }

	[Database]
	private IDatabase<ItemDefinition> ItemDefinitionDatabase { get; set; }

	[Ancillary]
	private IPathfindingService PathfindingService { get; set; }

	private IDatabase<PillageRewardDefinition> PillageRewardDefinitionDatabase { get; set; }

	[Ancillary]
	private IPlayerControllerRepositoryService PlayerControllerRepositoryService { get; set; }

	[Database]
	private IDatabase<SimulationDescriptor> SimulationDescriptorDatabase { get; set; }

	[Ancillary]
	private IWorldPositionningService WorldPositionningService { get; set; }

	[Service]
	private IEventService EventService { get; set; }

	public static void RemoveEquipmentSet(Unit unit)
	{
		UnitEquipmentSet unitEquipmentSet = unit.UnitDesign.UnitEquipmentSet;
		if (unitEquipmentSet.Slots == null || unitEquipmentSet.Slots.Length == 0)
		{
			unitEquipmentSet = unit.UnitDesign.UnitBodyDefinition.UnitEquipmentSet;
		}
		List<StaticString> list = new List<StaticString>();
		int num = 0;
		while (unitEquipmentSet.Slots != null && num < unitEquipmentSet.Slots.Length)
		{
			UnitEquipmentSet.Slot slot = unitEquipmentSet.Slots[num];
			if (StaticString.IsNullOrEmpty(slot.Name))
			{
				Diagnostics.LogError("Slot name is either null or empty.");
			}
			else if (!StaticString.IsNullOrEmpty(slot.ItemName))
			{
				if (!list.Contains(slot.ItemName))
				{
					list.Add(slot.ItemName);
				}
				SimulationObject simulationObject = unit.SimulationObject.Children.FirstOrDefault((SimulationObject iterator) => iterator.Name == slot.ItemName);
				if (simulationObject != null)
				{
					unit.SimulationObject.RemoveChild(simulationObject);
					simulationObject.Dispose();
				}
			}
			num++;
		}
		if (DepartmentOfDefense.staticSimulationDescriptorDatabase == null)
		{
			DepartmentOfDefense.staticSimulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
		}
		for (int i = 0; i < list.Count; i++)
		{
			StaticString key = list[i].ToString().Split(DepartmentOfDefense.ItemSeparators)[0];
			ItemDefinition value = DepartmentOfDefense.staticItemDefinitionsDatabase.GetValue(key);
			if (value != null)
			{
				unit.RemoveUnitAbilities(value.AbilityReferences);
			}
		}
	}

	public static bool CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(WorldPosition worldPosition, PathfindingMovementCapacity movementCapacity = PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.Water)
	{
		if (!worldPosition.IsValid)
		{
			return false;
		}
		IGameService service = Services.GetService<IGameService>();
		IPathfindingService service2 = service.Game.Services.GetService<IPathfindingService>();
		Diagnostics.Assert(service2 != null);
		if (!service2.IsTilePassable(worldPosition, movementCapacity, PathfindingFlags.IgnoreFogOfWar))
		{
			return false;
		}
		global::Game game = service.Game as global::Game;
		if (game == null)
		{
			Diagnostics.LogError("CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation: game is null.");
			return false;
		}
		GridMap<Army> gridMap = game.World.Atlas.GetMap(WorldAtlas.Maps.Armies) as GridMap<Army>;
		Diagnostics.Assert(gridMap != null);
		Army value = gridMap.GetValue(worldPosition);
		if (value != null)
		{
			return false;
		}
		GridMap<bool> map = game.World.Atlas.GetMap(WorldAtlas.Maps.Ridges) as GridMap<bool>;
		Diagnostics.Assert(gridMap != null);
		return !map.GetValue(worldPosition);
	}

	public static bool CheckWhetherTargetPositionIsValidAsSeafaringSpawnLocation(WorldPosition worldPosition, int empireIndex, int unitCount)
	{
		if (!worldPosition.IsValid)
		{
			return false;
		}
		IGameService service = Services.GetService<IGameService>();
		global::Game game = service.Game as global::Game;
		if (game == null)
		{
			Diagnostics.LogError("CheckWhetherTargetPositionIsValidAsSeafaringSpawnLocation: game is null.");
			return false;
		}
		GridMap<Army> gridMap = game.World.Atlas.GetMap(WorldAtlas.Maps.Armies) as GridMap<Army>;
		Diagnostics.Assert(gridMap != null);
		IWorldPositionningService service2 = game.Services.GetService<IWorldPositionningService>();
		if (service2.IsFrozenWaterTile(worldPosition))
		{
			return false;
		}
		if (!service2.ContainsTerrainTag(worldPosition, "TerrainTagSeafaringUnitCreation"))
		{
			return false;
		}
		Army value = gridMap.GetValue(worldPosition);
		if (value != null)
		{
			if (value.Empire.Index == empireIndex && !value.IsPrivateers)
			{
				float propertyValue = value.GetPropertyValue(SimulationProperties.MaximumUnitSlotCount);
				if (propertyValue - (float)(value.UnitsCount + unitCount) >= 0f)
				{
					return true;
				}
			}
			return false;
		}
		return true;
	}

	public bool CanAttack(Army myArmy, Army defenderArmy)
	{
		Diagnostics.Assert(this.PathfindingService != null);
		if (!this.PathfindingService.IsTransitionPassable(myArmy.WorldPosition, defenderArmy.WorldPosition, myArmy, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnoreZoneOfControl | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreKaijuGarrisons, null))
		{
			return false;
		}
		if (base.Empire is MajorEmpire && !myArmy.IsPrivateers)
		{
			Diagnostics.Assert(this.departmentOfForeignAffairs != null);
			if (!this.departmentOfForeignAffairs.CanAttack(defenderArmy))
			{
				return false;
			}
		}
		return true;
	}

	public bool CanBesiegeCity(Army army, City city, bool checkDiplomaticRelation, bool silent = true)
	{
		if (city.Empire == army.Empire)
		{
			if (!silent)
			{
				Diagnostics.LogError("Cannot besiege because the city is owned by the army empire.");
			}
			return false;
		}
		if (checkDiplomaticRelation && base.Empire is MajorEmpire && !army.IsPrivateers)
		{
			Diagnostics.Assert(this.departmentOfForeignAffairs != null);
			if (!this.departmentOfForeignAffairs.CanBesiegeCity(city))
			{
				Diagnostics.LogWarning("Cannot besiege because the diplomatic relation doesn't authorize the siege.");
				return false;
			}
		}
		if (!city.Districts.Any((District match) => match.WorldPosition == army.WorldPosition))
		{
			if (!silent)
			{
				Diagnostics.LogError("Cannot besiege because the army is not near the city.");
			}
			return false;
		}
		if (army.IsNaval)
		{
			return true;
		}
		if (city.BesiegingEmpire != null && city.BesiegingEmpire != base.Empire)
		{
			if (!silent)
			{
				Diagnostics.LogError("Cannot besiege because the city is already besieged by another empire.");
			}
			return false;
		}
		PathfindingContext pathfindingContext = army.GenerateContext();
		pathfindingContext.RemoveMovementCapacity(PathfindingMovementCapacity.Air);
		for (int i = 0; i < 6; i++)
		{
			WorldPosition neighbourTile = this.WorldPositionningService.GetNeighbourTile(army.WorldPosition, (WorldOrientation)i, 1);
			if (neighbourTile.IsValid)
			{
				District district = this.WorldPositionningService.GetDistrict(neighbourTile);
				if (district != null && District.IsACityTile(district) && this.PathfindingService.IsTransitionPassable(army.WorldPosition, district.WorldPosition, pathfindingContext, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreDistrict | PathfindingFlags.IgnoreKaijuGarrisons, null))
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool CanCreateNewArmyFrom(GameEntityGUID[] unitGuid, IGarrison garrison, bool silent = true, bool generateArmyLeader = false)
	{
		ReadOnlyCollection<Unit> standardUnits = garrison.StandardUnits;
		IGameEntity gameEntity = null;
		int num = 0;
		for (int i = 0; i < unitGuid.Length; i++)
		{
			if (!this.GameEntityRepositoryService.TryGetValue(unitGuid[i], out gameEntity))
			{
				Diagnostics.LogError("Order preprocessing failed because the transfered unit entity guid at index {0} is not valid.", new object[]
				{
					i
				});
				return false;
			}
			Unit unit = gameEntity as Unit;
			if (unit.Garrison != garrison)
			{
				Diagnostics.LogError("Order preprocessing failed because the transfered unit entity guid at index {0} is owned by the garrison city.", new object[]
				{
					i
				});
				return false;
			}
			if (!standardUnits.Contains(unit) && unit != garrison.Hero)
			{
				Diagnostics.LogError("Order preprocessing failed because the transfered unit entity guid at index {0} is not in the garrison.", new object[]
				{
					i
				});
				return false;
			}
			num += (int)unit.GetPropertyValue(SimulationProperties.UnitSlotCount);
		}
		float propertyValue = base.Empire.GetPropertyValue(SimulationProperties.ArmyUnitSlot);
		if (propertyValue < (float)num)
		{
			Diagnostics.LogError("Order preprocessing failed because the target army has not enough room for the transfered units.");
			return false;
		}
		return true;
	}

	public bool CanMoveAndAttack(Army myArmy, Army defenderArmy)
	{
		Diagnostics.Assert(this.PathfindingService != null);
		if (this.PathfindingService.IsTransitionPassable(myArmy.WorldPosition, defenderArmy.WorldPosition, myArmy, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnoreZoneOfControl | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreKaijuGarrisons, null))
		{
			return false;
		}
		if (base.Empire is MajorEmpire && !myArmy.IsPrivateers)
		{
			Diagnostics.Assert(this.departmentOfForeignAffairs != null);
			if (!this.departmentOfForeignAffairs.CanAttack(defenderArmy))
			{
				return false;
			}
		}
		return true;
	}

	public bool CanTransferToGarrison(int unitsToTransferCount, IGarrison destination, bool silent = true)
	{
		if ((destination is KaijuArmy || destination is KaijuGarrison) && !DepartmentOfScience.IsTechnologyResearched(base.Empire as global::Empire, "TechnologyDefinitionMimics1"))
		{
			if (!silent)
			{
				Diagnostics.LogError("CanTransferToGarrison() Army Destination is Kaiju Army or Garrison!");
			}
			return false;
		}
		if (destination.Empire.Index != base.Empire.Index)
		{
			if (!silent)
			{
				Diagnostics.LogError("CanTransferToGarrison() False: armyDestination.Empire.Index != this.Empire.Index");
			}
			return false;
		}
		if (unitsToTransferCount + destination.CurrentUnitSlot > destination.MaximumUnitSlot)
		{
			if (!silent)
			{
				Diagnostics.LogError("CanTransferToGarrison() There is not available slots to store all units!");
			}
			return false;
		}
		return true;
	}

	public bool CanTransfertUnitsFromGarrison(GameEntityGUID[] unitGuid, IGarrison source, bool silent = true)
	{
		if ((source is KaijuArmy || source is KaijuGarrison) && !DepartmentOfScience.IsTechnologyResearched(base.Empire as global::Empire, "TechnologyDefinitionMimics1"))
		{
			if (!silent)
			{
				Diagnostics.LogError("CanTransfertUnitsFromSource() Can transfer units from Kaiju Garrison or Kaiju Army!");
			}
			return false;
		}
		ReadOnlyCollection<Unit> standardUnits = source.StandardUnits;
		IGameEntity gameEntity = null;
		int num = 0;
		for (int i = 0; i < unitGuid.Length; i++)
		{
			if (!this.GameEntityRepositoryService.TryGetValue(unitGuid[i], out gameEntity))
			{
				if (!silent)
				{
					Diagnostics.LogError("Order preprocessing failed because the transfered unit entity guid at index {0} is not valid. (GUID={1})", new object[]
					{
						i,
						unitGuid[i]
					});
				}
				return false;
			}
			Unit unit = gameEntity as Unit;
			if (unit.Garrison != source)
			{
				if (!silent)
				{
					Diagnostics.LogError("Order preprocessing failed because the transfered unit entity guid at index {0} is not owned by the garrison city. (GUID={1}, garrison={2})", new object[]
					{
						i,
						unit.GUID,
						source.GUID
					});
				}
				return false;
			}
			if (!standardUnits.Contains(unit) && unit != source.Hero)
			{
				if (!silent)
				{
					Diagnostics.LogError("Order preprocessing failed because the transfered unit entity guid at index {0} is not in the garrison. (unit={1}, garrison={2})", new object[]
					{
						i,
						unit.GUID,
						source.GUID
					});
				}
				return false;
			}
			if (unit.SimulationObject.Tags.Contains("UnitTypeKaijus") || unit.SimulationObject.Tags.Contains("UnitTypeKaijusMilitia"))
			{
				return false;
			}
			num += (int)unit.GetPropertyValue(SimulationProperties.UnitSlotCount);
		}
		float num2 = (float)source.MaximumUnitSlot;
		if (num2 < (float)num)
		{
			if (!silent)
			{
				Diagnostics.LogError("Order preprocessing failed because the target army has not enough room for the transfered units.");
			}
			return false;
		}
		return true;
	}

	public void CleanGarrisonAfterEncounter(IGarrison garrison)
	{
		this.InternalCleanGarrisonAfterEncounter(garrison);
		if (garrison is Army)
		{
			Army army = garrison as Army;
			if (garrison.IsEmpty)
			{
				this.RemoveArmy(army, true);
			}
			else
			{
				army.Refresh(false);
			}
		}
	}

	public bool ComputeEmpireRegenModifierPropertyNameForRegion(Region region, out StaticString regenModifierPropertyName)
	{
		regenModifierPropertyName = StaticString.Empty;
		bool flag = false;
		bool flag2 = false;
		global::Empire owner = region.Owner;
		if (owner != null)
		{
			if (base.Empire == owner)
			{
				flag = true;
			}
			else if (base.Empire is MajorEmpire && owner is MajorEmpire)
			{
				DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.DiplomaticRelations[owner.Index];
				if (diplomaticRelation != null && diplomaticRelation.State != null && diplomaticRelation.State.Name == DiplomaticRelationState.Names.Alliance)
				{
					flag2 = true;
				}
			}
		}
		if (flag)
		{
			regenModifierPropertyName = SimulationProperties.InOwnedRegionUnitRegenModifier;
		}
		else if (flag2)
		{
			regenModifierPropertyName = SimulationProperties.InAlliedRegionUnitRegenModifier;
		}
		else
		{
			regenModifierPropertyName = SimulationProperties.InNoneAlliedRegionUnitRegenModifier;
		}
		return !StaticString.IsNullOrEmpty(regenModifierPropertyName);
	}

	public void ChangeWinterShifterForm(bool winter)
	{
		if (!(base.Empire is MajorEmpire))
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < this.Armies.Count; i++)
		{
			foreach (Unit unit in this.Armies[i].Units)
			{
				if (unit.SimulationObject.Tags.Contains(DownloadableContent13.AffinityShifters))
				{
					unit.SetPropertyBaseValue("ShiftingForm", (float)((!winter) ? 0 : 1));
					unit.Refresh(false);
					flag = true;
				}
			}
			if (flag)
			{
				this.Armies[i].ShiftingFormHasChange(false);
			}
		}
		DepartmentOfTheInterior agency = base.Empire.GetAgency<DepartmentOfTheInterior>();
		for (int j = 0; j < agency.Cities.Count; j++)
		{
			flag = false;
			Garrison garrison = agency.Cities[j];
			foreach (Unit unit2 in garrison.Units)
			{
				if (unit2.SimulationObject.Tags.Contains(DownloadableContent13.AffinityShifters))
				{
					unit2.SetPropertyBaseValue("ShiftingForm", (float)((!winter) ? 0 : 1));
					unit2.Refresh(false);
					flag = true;
				}
			}
			if (flag && garrison is Army)
			{
				(garrison as Army).ShiftingFormHasChange(false);
			}
			garrison = agency.Cities[j].Militia;
			foreach (Unit unit3 in garrison.Units)
			{
				if (unit3.SimulationObject.Tags.Contains(DownloadableContent13.AffinityShifters))
				{
					unit3.SetPropertyBaseValue("ShiftingForm", (float)((!winter) ? 0 : 1));
					unit3.Refresh(false);
				}
			}
		}
		DepartmentOfEducation agency2 = base.Empire.GetAgency<DepartmentOfEducation>();
		for (int k = 0; k < agency2.Heroes.Count; k++)
		{
			if (agency2.Heroes[k].SimulationObject.Tags.Contains(DownloadableContent13.AffinityShifters))
			{
				agency2.Heroes[k].SetPropertyBaseValue("ShiftingForm", (float)((!winter) ? 0 : 1));
				agency2.Heroes[k].Refresh(false);
			}
		}
	}

	public void CheckArmiesOnFreezingTiles()
	{
		List<Unit> list = new List<Unit>();
		List<Unit> list2 = new List<Unit>();
		List<Unit> list3 = new List<Unit>();
		DepartmentOfEducation agency = base.Empire.GetAgency<DepartmentOfEducation>();
		bool flag = !(base.Empire is MajorEmpire) || this.departmentOfScience.HaveResearchedShipTechnology();
		for (int i = this.Armies.Count - 1; i >= 0; i--)
		{
			Army army = this.armies[i];
			bool flag2 = this.WorldPositionningService.IsWaterTile(army.WorldPosition);
			bool flag3 = this.WorldPositionningService.IsFrozenWaterTile(army.WorldPosition);
			bool flag4 = flag2 && !flag3;
			if (flag4 != army.IsNaval)
			{
				list.Clear();
				list2.Clear();
				list3.Clear();
				int unitsCount = army.UnitsCount;
				foreach (Unit unit in army.StandardUnits)
				{
					if (unit.SimulationObject.Tags.Contains(DownloadableContent16.SeafaringUnit))
					{
						list.Add(unit);
					}
					else if (unit.SimulationObject.Tags.Contains(DownloadableContent16.TransportShipUnit))
					{
						list2.Add(unit);
					}
					else
					{
						list3.Add(unit);
					}
				}
				if (flag4)
				{
					if (list3.Count != 0 || army.Hero != null)
					{
						if (flag)
						{
							for (int j = 0; j < list3.Count; j++)
							{
								list3[j].SwitchToEmbarkedUnit(true);
							}
							if (army.Hero != null)
							{
								army.Hero.SwitchToEmbarkedUnit(true);
							}
							army.SetSails();
						}
						else
						{
							for (int k = list3.Count - 1; k >= 0; k--)
							{
								Unit unit2 = list3[k];
								army.RemoveUnit(unit2);
								this.GameEntityRepositoryService.Unregister(unit2);
								unit2.Dispose();
							}
							if (army.StandardUnits.Count == 0 && army.Hero != null && agency != null)
							{
								agency.InjureHero(army.Hero.GUID, true);
							}
							if (unitsCount != army.UnitsCount)
							{
								ArmyHitInfo armyInfo = new ArmyHitInfo(army, unitsCount, army.WorldPosition, ArmyHitInfo.HitType.Drown);
								this.EventService.Notify(new EventArmyDrowned(base.Empire, armyInfo, false));
							}
							if (army.UnitsCount == 0)
							{
								this.RemoveArmy(army, true);
							}
						}
					}
				}
				else
				{
					if (list.Count != 0 && !flag3)
					{
						for (int l = list3.Count - 1; l >= 0; l--)
						{
							Unit unit3 = list[l];
							army.RemoveUnit(unit3);
							this.GameEntityRepositoryService.Unregister(unit3);
							unit3.Dispose();
						}
						if (army.StandardUnits.Count == 0 && army.Hero != null && agency != null)
						{
							agency.InjureHero(army.Hero.GUID, true);
						}
						if (unitsCount != army.UnitsCount)
						{
							ArmyHitInfo armyInfo2 = new ArmyHitInfo(army, unitsCount, army.WorldPosition, ArmyHitInfo.HitType.Drown);
							this.EventService.Notify(new EventArmyDrowned(base.Empire, armyInfo2, false));
						}
						if (army.UnitsCount == 0)
						{
							this.RemoveArmy(army, true);
							goto IL_3E3;
						}
					}
					if (list2.Count != 0)
					{
						for (int m = 0; m < list2.Count; m++)
						{
							list2[m].SwitchToEmbarkedUnit(false);
						}
						if (army.Hero != null)
						{
							army.Hero.SwitchToEmbarkedUnit(false);
						}
						army.SetSails();
						if (list.Count != 0)
						{
						}
					}
				}
			}
			IL_3E3:;
		}
	}

	public void CheckArmiesOnOrb()
	{
		GridMap<byte> gridMap = (this.GameService.Game as global::Game).World.Atlas.GetMap(WorldAtlas.Maps.Orbs) as GridMap<byte>;
		if (gridMap == null)
		{
			return;
		}
		for (int i = 0; i < this.Armies.Count; i++)
		{
			if (gridMap.GetValue(this.Armies[i].WorldPosition) > 0)
			{
				IOrbService service = this.GameService.Game.Services.GetService<IOrbService>();
				if (service != null)
				{
					service.CollectOrbsAtPosition(this.Armies[i].WorldPosition, this.Armies[i], base.Empire as global::Empire);
				}
			}
		}
	}

	public void CheckArmiesOnMapBoost()
	{
		for (int i = 0; i < this.Armies.Count; i++)
		{
			this.CheckArmyOnMapBoost(this.Armies[i]);
		}
	}

	public void UpdateLifeAfterEncounter(IGarrison garrison)
	{
		if (garrison == null)
		{
			Diagnostics.LogError("UpdateLifeAfterEncounter: garrison shouldn't be null");
			return;
		}
		bool flag = false;
		foreach (Unit unit in garrison.Units)
		{
			unit.Refresh(false);
			float propertyValue = unit.GetPropertyValue(SimulationProperties.Health);
			if (propertyValue > 0f)
			{
				flag = true;
			}
		}
		for (int i = garrison.StandardUnits.Count - 1; i >= 0; i--)
		{
			Unit unit2 = garrison.StandardUnits[i];
			if (unit2 == null)
			{
				Diagnostics.LogError("UpdateLifeAfterEncounter: unit shouldn't be null");
			}
			else if (unit2 != garrison.Hero)
			{
				float propertyValue2 = unit2.GetPropertyValue(SimulationProperties.Health);
				if (propertyValue2 <= 0f)
				{
					if (flag && unit2.CheckUnitAbility(UnitAbility.ReadonlyLastStand, -1))
					{
						unit2.SetPropertyBaseValue(SimulationProperties.Health, 1f);
					}
					if (unit2.CheckUnitAbility(UnitAbility.ReadonlyIndestructible, -1))
					{
						unit2.SetPropertyBaseValue(SimulationProperties.Health, 1f);
					}
				}
				else
				{
					unit2.SetPropertyBaseValue(SimulationProperties.Health, propertyValue2);
				}
			}
		}
		if (garrison.Hero != null)
		{
			float propertyValue3 = garrison.Hero.GetPropertyValue(SimulationProperties.Health);
			if (propertyValue3 <= 0f && flag && garrison.Hero.CheckUnitAbility(UnitAbility.ReadonlyLastStand, -1))
			{
				garrison.Hero.SetPropertyBaseValue(SimulationProperties.Health, 1f);
			}
		}
	}

	public string GenerateArmyClientLocalizedName(Army army)
	{
		global::Empire empire = base.Empire as global::Empire;
		Diagnostics.Assert(empire != null);
		Diagnostics.Assert(empire.Faction != null);
		if (empire is MinorEmpire || empire is NavalEmpire)
		{
			return AgeLocalizer.Instance.LocalizeString("%RoamingArmyTitle");
		}
		if (empire is LesserEmpire)
		{
			return AgeLocalizer.Instance.LocalizeString("%RoamingArmyTitle");
		}
		List<string> list = new List<string>();
		for (int i = 0; i < this.Armies.Count; i++)
		{
			list.Add(this.Armies[i].LocalizedName);
		}
		bool flag;
		if (army.StandardUnits.Count > 0)
		{
			flag = army.StandardUnits.All((Unit unit) => unit.UnitDesign.Tags.Contains(DownloadableContent9.TagColossus));
		}
		else
		{
			flag = false;
		}
		bool flag2 = flag;
		int num = (!flag2) ? 10 : 3;
		int num2 = (!flag2) ? 10 : 3;
		StaticString x = (!flag2) ? "%ArmyNumber" : "%ArmyColossusNumber";
		StaticString x2 = (!flag2) ? ("%Faction" + empire.Faction.Affinity.Name + "ArmyTitle") : ("%ArmyColossusCaption" + army.StandardUnits[0].UnitDesign.UnitBodyDefinition.Name + "_");
		StaticString x3 = (!flag2) ? ("%Faction" + empire.Faction.Affinity.Name + "ArmyCaption") : ("%Faction" + empire.Faction.Affinity.Name + "ArmyColossusTitle");
		string text = AgeLocalizer.Instance.LocalizeString((!flag2) ? "%ArmyGeneratedNameFormat" : "%ArmyColossusGeneratedNameFormat");
		string text2 = string.Empty;
		int num3 = num * num2;
		for (int j = 0; j < num3; j++)
		{
			int num4 = (int)army.GUID;
			num4 = num4 / num3 * num3 + j;
			int num5 = num4 % num2 + 1;
			int num6 = num4 % num3 / num2 + 1;
			text2 = text.Replace("$Number", AgeLocalizer.Instance.LocalizeString(x + num6.ToString()));
			text2 = text2.Replace("$Title", AgeLocalizer.Instance.LocalizeString(x2 + num5.ToString()));
			text2 = text2.Replace("$Caption", AgeLocalizer.Instance.LocalizeString(x3));
			if (!list.Contains(text2))
			{
				return text2;
			}
		}
		return string.Empty;
	}

	public bool GenerateUnitDesignModelId(string unitDesignName, out uint unitDesignModelId)
	{
		unitDesignModelId = uint.MaxValue;
		if (string.IsNullOrEmpty(unitDesignName))
		{
			Diagnostics.LogError("GenerateUnitDesignModelId: unit design name is either null or empty.");
			return false;
		}
		UnitDesign unitDesign = null;
		try
		{
			IDatabase<UnitDesign> database = Databases.GetDatabase<UnitDesign>(false);
			Diagnostics.Assert(database != null);
			if (!database.TryGetValue(unitDesignName, out unitDesign))
			{
				IDatabase<UnitProfile> database2 = Databases.GetDatabase<UnitProfile>(false);
				UnitProfile unitProfile;
				if (!database2.TryGetValue(unitDesignName, out unitProfile))
				{
					Diagnostics.LogError("GenerateUnitDesignModelId: unit design/profile ('{0}') is not available.", new object[]
					{
						unitDesignName
					});
					return false;
				}
			}
		}
		catch (InvalidOperationException ex)
		{
			Diagnostics.LogError("{0} : GenerateUnitDesignModelId failed because unit design's name ('{1}') is not unique.", new object[]
			{
				ex.Message,
				unitDesignName
			});
			return false;
		}
		unitDesignModelId = this.nextAvailableUnitDesignModelId++;
		return true;
	}

	public bool ExistsArmy(GameEntityGUID guid)
	{
		int num = this.armies.BinarySearch((Army match) => match.GUID.CompareTo(guid));
		return num >= 0;
	}

	public Army GetArmy(GameEntityGUID guid)
	{
		int num = this.armies.BinarySearch((Army match) => match.GUID.CompareTo(guid));
		if (num < 0)
		{
			return null;
		}
		return this.armies[num];
	}

	public IGarrison GetGarrison(GameEntityGUID guid)
	{
		int num = this.armies.BinarySearch((Army match) => match.GUID.CompareTo(guid));
		if (num >= 0)
		{
			return this.armies[num];
		}
		DepartmentOfTheInterior agency = base.Empire.GetAgency<DepartmentOfTheInterior>();
		for (int i = 0; i < agency.Cities.Count; i++)
		{
			City city = agency.Cities[i];
			if (city.GUID == guid)
			{
				return city;
			}
			if (city.Camp != null && city.Camp.GUID == guid)
			{
				return city.Camp;
			}
		}
		for (int j = 0; j < agency.OccupiedFortresses.Count; j++)
		{
			if (agency.OccupiedFortresses[j].GUID == guid)
			{
				return agency.OccupiedFortresses[j];
			}
		}
		MajorEmpire majorEmpire = base.Empire as MajorEmpire;
		if (majorEmpire != null)
		{
			for (int k = 0; k < majorEmpire.ConvertedVillages.Count; k++)
			{
				if (majorEmpire.ConvertedVillages[k].GUID == guid)
				{
					return majorEmpire.ConvertedVillages[k];
				}
			}
			for (int l = 0; l < majorEmpire.TamedKaijus.Count; l++)
			{
				if (majorEmpire.TamedKaijus[l].OnGarrisonMode() && majorEmpire.TamedKaijus[l].KaijuGarrison.GUID == guid)
				{
					return majorEmpire.TamedKaijus[l].KaijuGarrison;
				}
			}
		}
		return null;
	}

	public ReadOnlyCollection<Army> GetHeroLedArmies()
	{
		List<Army> list = new List<Army>();
		if (this.armies != null)
		{
			for (int i = 0; i < this.armies.Count; i++)
			{
				if (this.armies[i].Hero != null)
				{
					list.Add(this.armies[i]);
				}
			}
		}
		return list.AsReadOnly();
	}

	public void HealUnitByAmountInPercent(Unit unit, float amount)
	{
		float propertyValue = unit.GetPropertyValue(SimulationProperties.MaximumHealth);
		float num = unit.GetPropertyValue(SimulationProperties.Health);
		num = Math.Max(1f, Math.Min(propertyValue, num + propertyValue * amount));
		unit.SetPropertyBaseValue(SimulationProperties.Health, num);
		SimulationObjectWrapper simulationObjectWrapper = unit.Garrison as SimulationObjectWrapper;
		if (simulationObjectWrapper != null)
		{
			simulationObjectWrapper.Refresh(false);
		}
	}

	public void RemoveArmy(Army army, bool disposeAndUnregister = true)
	{
		if (this.armies.Remove(army))
		{
			EventArmyDestroyed eventArmyDestroyed = new EventArmyDestroyed(army.GUID, army.Empire.Index);
			if (army.PillageTarget.IsValid)
			{
				DepartmentOfDefense.StopPillage(army);
			}
			if (army.IsAspirating)
			{
				this.StopAspirating(army);
			}
			if (army.IsDismantlingDevice)
			{
				ITerraformDeviceRepositoryService service = this.GameService.Game.Services.GetService<ITerraformDeviceRepositoryService>();
				TerraformDevice device = service[army.DismantlingDeviceTarget] as TerraformDevice;
				this.StopDismantelingDevice(army, device);
			}
			if (army.IsDismantlingCreepingNode)
			{
				CreepingNode creepingNode = null;
				if (this.GameEntityRepositoryService.TryGetValue<CreepingNode>(army.DismantlingCreepingNodeTarget, out creepingNode))
				{
					this.StopDismantelingCreepingNode(army, creepingNode);
				}
			}
			if (army.Hero != null)
			{
				DepartmentOfEducation agency = base.Empire.GetAgency<DepartmentOfEducation>();
				agency.UnassignHero(army.Hero);
			}
			this.OnArmiesCollectionChange(CollectionChangeAction.Remove, army);
			Region region = this.WorldPositionningService.GetRegion(army.WorldPosition);
			if (disposeAndUnregister)
			{
				this.GameEntityRepositoryService.Unregister(army);
			}
			base.Empire.RemoveChild(army);
			base.Empire.Refresh(false);
			if (region.City != null && region.City.Empire != base.Empire)
			{
				DepartmentOfTheInterior agency2 = region.City.Empire.GetAgency<DepartmentOfTheInterior>();
				if (agency2 != null)
				{
					agency2.StopNavalSiege(region.City, army);
				}
			}
			if (disposeAndUnregister)
			{
				army.Dispose();
			}
			if (region.City != null && region.City.Empire != base.Empire)
			{
				DepartmentOfTheInterior agency3 = region.City.Empire.GetAgency<DepartmentOfTheInterior>();
				if (agency3 != null && region.City.BesiegingEmpire == base.Empire && agency3.NeedToStopSiege(region.City))
				{
					agency3.StopSiege(region.City);
				}
			}
			IEventService service2 = Services.GetService<IEventService>();
			if (service2 != null)
			{
				global::Game game = this.GameService.Game as global::Game;
				global::Empire empire = game.Empires[eventArmyDestroyed.ArmyEmpireIndex];
				service2.Notify(new EventArmyDisbanded(empire, eventArmyDestroyed.ArmyGUID));
				service2.Notify(eventArmyDestroyed);
			}
		}
	}

	public void ReleaseArmy(Army army)
	{
		if (this.armies.Remove(army))
		{
			if (army.PillageTarget.IsValid)
			{
				DepartmentOfDefense.StopPillage(army);
			}
			if (army.IsAspirating)
			{
				this.StopAspirating(army);
			}
			if (army.IsDismantlingDevice)
			{
				ITerraformDeviceRepositoryService service = this.GameService.Game.Services.GetService<ITerraformDeviceRepositoryService>();
				TerraformDevice device = service[army.DismantlingDeviceTarget] as TerraformDevice;
				this.StopDismantelingDevice(army, device);
			}
			if (army.IsDismantlingCreepingNode)
			{
				CreepingNode creepingNode = null;
				if (this.GameEntityRepositoryService.TryGetValue<CreepingNode>(army.DismantlingCreepingNodeTarget, out creepingNode))
				{
					this.StopDismantelingCreepingNode(army, creepingNode);
				}
			}
			if (army.Hero != null)
			{
				DepartmentOfEducation agency = base.Empire.GetAgency<DepartmentOfEducation>();
				agency.UnassignHero(army.Hero);
			}
			this.OnArmiesCollectionChange(CollectionChangeAction.Remove, army);
			army.Empire = null;
			base.Empire.RemoveChild(army);
			base.Empire.Refresh(false);
			Region region = this.WorldPositionningService.GetRegion(army.WorldPosition);
			if (region.City != null && region.City.Empire != base.Empire)
			{
				DepartmentOfTheInterior agency2 = region.City.Empire.GetAgency<DepartmentOfTheInterior>();
				if (agency2 != null)
				{
					if (region.City.BesiegingEmpire == base.Empire && agency2.NeedToStopSiege(region.City))
					{
						agency2.StopSiege(region.City);
					}
					agency2.StopNavalSiege(region.City, army);
				}
				this.visibilityService.NotifyVisibilityHasChanged(base.Empire as global::Empire);
			}
			if (army.IsEarthquaker)
			{
				army.SetEarthquakerStatus(false, false, null);
			}
		}
	}

	public void WoundUnit(Unit unit, float damage)
	{
		float propertyValue = unit.GetPropertyValue(SimulationProperties.Health);
		unit.SetPropertyBaseValue(SimulationProperties.Health, propertyValue - damage);
		if (unit.Garrison != null)
		{
			this.UpdateLifeAfterEncounter(unit.Garrison);
			this.CleanGarrisonAfterEncounter(unit.Garrison);
		}
		else if (unit.UnitDesign is UnitProfile && propertyValue <= 0f)
		{
			DepartmentOfEducation agency = base.Empire.GetAgency<DepartmentOfEducation>();
			Diagnostics.Assert(agency != null);
			agency.InjureHero(unit, true);
		}
		if (unit.SimulationObject != null)
		{
			unit.Refresh(false);
		}
	}

	public void WoundUnitByAmountInPercent(Unit unit, float damage)
	{
		float propertyValue = unit.GetPropertyValue(SimulationProperties.MaximumHealth);
		float propertyValue2 = unit.GetPropertyValue(SimulationProperties.Health);
		unit.SetPropertyBaseValue(SimulationProperties.Health, propertyValue2 - propertyValue * damage / 100f);
		if (unit.Garrison != null)
		{
			this.UpdateLifeAfterEncounter(unit.Garrison);
			this.CleanGarrisonAfterEncounter(unit.Garrison);
		}
		unit.Refresh(false);
	}

	internal void CreateUnitInGarrison(StaticString[] unitDesignNames, IGarrison garrison)
	{
		for (int i = 0; i < unitDesignNames.Length; i++)
		{
			GameEntityGUID guid = this.GameEntityRepositoryService.GenerateGUID();
			Unit unit = this.CreateUnitByDesignName(guid, unitDesignNames[i]);
			if (unit != null)
			{
				unit.Refresh(true);
				unit.UpdateExperienceReward(base.Empire);
				unit.UpdateShiftingForm();
				garrison.AddUnit(unit);
			}
		}
	}

	internal bool CreateArmy(GameEntityGUID armyGUID, DepartmentOfDefense.UnitDescriptor[] unitDescriptors, WorldPosition position, out Army army, bool onlyDefaultUnitDesign = false, bool allowEmpty = false)
	{
		army = null;
		if (armyGUID == GameEntityGUID.Zero)
		{
			Diagnostics.LogError("Skipping army creation process because the game entity guid is null.");
			return false;
		}
		army = this.CreateArmy(armyGUID, false);
		army.SetWorldPositionWithEstimatedTimeOfArrival(position, global::Game.Time);
		List<Unit> list = new List<Unit>();
		Unit unit = null;
		if (unitDescriptors != null)
		{
			for (int i = 0; i < unitDescriptors.Length; i++)
			{
				if (unitDescriptors[i].GameEntityGUID == GameEntityGUID.Zero)
				{
					Diagnostics.LogError("Skipping unit descriptor #{0} because the game entity GUID is null.", new object[]
					{
						i
					});
				}
				else
				{
					StaticString unitDesignName = unitDescriptors[i].UnitDesignName;
					Unit unit2 = null;
					IDatabase<UnitProfile> database = Databases.GetDatabase<UnitProfile>(false);
					UnitProfile unitProfile;
					if (database.TryGetValue(unitDesignName, out unitProfile) && unitProfile.IsHero)
					{
						if (unit != null)
						{
							Diagnostics.LogWarning("Skipping unit descriptor #{0} (which is a hero) because the army #{1} already contains a hero.", new object[]
							{
								i,
								army.GUID
							});
							goto IL_295;
						}
						if (base.Empire is MinorEmpire)
						{
							Diagnostics.LogError("You are trying to create an army with a hero for a MinorEmpire, you shouldn't...");
							goto IL_295;
						}
						DepartmentOfEducation agency = base.Empire.GetAgency<DepartmentOfEducation>();
						Diagnostics.Assert(agency != null);
						unit = agency.CreateHero(unitDescriptors[i].GameEntityGUID, unitProfile);
						unit2 = unit;
					}
					if (unit2 == null)
					{
						UnitDesign unitDesign = this.UnitDesignDatabase.GetAvailableUnitDesignsAsEnumerable().FirstOrDefault((UnitDesign iterator) => (!onlyDefaultUnitDesign || iterator.ModelRevision == 0u) && iterator.Name == unitDesignName);
						if (unitDesign == null)
						{
							UnitDesign unitDesign2 = null;
							IDatabase<UnitDesign> database2 = Databases.GetDatabase<UnitDesign>(false);
							Diagnostics.Assert(database2 != null);
							if (!database2.TryGetValue(unitDesignName, out unitDesign2))
							{
								Diagnostics.LogError("Skipping unit #{0} (unit design: '{1}') because we can't retrieve the unit design.", new object[]
								{
									i,
									unitDesignName
								});
								goto IL_295;
							}
							unitDesign = (unitDesign2.Clone() as UnitDesign);
							unitDesign.UnitEquipmentSet = (unitDesign2.UnitEquipmentSet.Clone() as UnitEquipmentSet);
							unitDesign.Model = unitDescriptors[i].UnitDesignModel;
							unitDesign.ModelRevision = 0u;
							unitDesign.Hidden = true;
							this.AddUnitDesign(unitDesign, unitDesign.Model, DepartmentOfDefense.UnitDesignState.Hidden);
						}
						unit2 = DepartmentOfDefense.CreateUnitByDesign(unitDescriptors[i].GameEntityGUID, unitDesign);
						if (unit2 == null)
						{
							Diagnostics.LogError("Skipping unit #{0} (unit design: '{1}') because the unit creation failed.", new object[]
							{
								i,
								unitDesignName
							});
						}
						else
						{
							list.Add(unit2);
						}
					}
				}
				IL_295:;
			}
		}
		if (list.Count == 0 && unit == null && !allowEmpty)
		{
			Diagnostics.LogError("Skipping army creation process because the list of units is empty.");
			return false;
		}
		if (unit != null)
		{
			army.SetHero(unit);
		}
		army.ClientLocalizedName = this.GenerateArmyClientLocalizedName(army);
		for (int j = 0; j < list.Count; j++)
		{
			list[j].Refresh(true);
			list[j].UpdateExperienceReward(base.Empire);
			army.AddUnit(list[j]);
		}
		army.Refresh(true);
		this.AddArmy(army);
		return true;
	}

	internal Unit GenerateClone(Unit unit, GameEntityGUID temporaryGuid)
	{
		Unit unit2 = DepartmentOfDefense.CreateUnitByDesign(temporaryGuid, unit.UnitDesign);
		unit2.UnitDesign = (unit.UnitDesign.Clone() as UnitDesign);
		unit2.CopyRanksFrom(unit);
		unit2.CopySkillsFrom(unit);
		unit2.CopySerializablePropertiesFrom(unit);
		DepartmentOfDefense.ApplyEquipmentSet(unit2);
		unit2.Refresh(true);
		unit2.UpdateExperienceReward(base.Empire);
		return unit2;
	}

	internal Unit CreateTemporaryUnit(GameEntityGUID guid, UnitDesign unitDesign)
	{
		Unit unit = DepartmentOfDefense.CreateUnitByDesign(guid, unitDesign);
		if (unit != null)
		{
			unit.SimulationObject.Tags.AddTag(DepartmentOfDefense.TagTemporary);
		}
		return unit;
	}

	internal virtual void OnEmpireEliminated(global::Empire empire, bool authorized)
	{
		if (empire.Index == base.Empire.Index)
		{
			for (int i = this.armies.Count - 1; i >= 0; i--)
			{
				Army army = this.armies[i];
				if (!(army is KaijuArmy))
				{
					for (int j = army.StandardUnits.Count - 1; j >= 0; j--)
					{
						Unit unit = army.StandardUnits[j];
						army.RemoveUnit(unit);
						this.GameEntityRepositoryService.Unregister(unit);
						unit.Dispose();
					}
					if (army.Hero != null)
					{
						DepartmentOfEducation agency = base.Empire.GetAgency<DepartmentOfEducation>();
						if (agency != null)
						{
							agency.UnassignHero(army.Hero);
						}
					}
					this.RemoveArmy(army, true);
				}
			}
		}
	}

	internal UnitDesign RegisterHeroUnitDesign(UnitDesign unitDesign)
	{
		return this.AddUnitDesign(unitDesign, this.nextAvailableUnitDesignModelId++, DepartmentOfDefense.UnitDesignState.Hidden);
	}

	internal UnitDesign RegisterUnitDesign(UnitDesign unitDesign, DepartmentOfDefense.UnitDesignState state)
	{
		return this.AddUnitDesign(unitDesign, this.nextAvailableUnitDesignModelId++, state);
	}

	internal void AddCatspawUnitDesignAndRetrofit(Unit unit, bool catspaw)
	{
		UnitDesign unitDesign = this.UnitDesignDatabase.GetAvailableUnitDesignsAsEnumerable().FirstOrDefault((UnitDesign iterator) => iterator.Name == unit.UnitDesign.Name);
		if (unitDesign == null)
		{
			unitDesign = (unit.UnitDesign.Clone() as UnitDesign);
			if (catspaw)
			{
				unitDesign.Tags.AddTag(TradableUnit.ReadOnlyMercenary);
			}
			else
			{
				unitDesign.Tags.RemoveTag(TradableUnit.ReadOnlyMercenary);
			}
			this.AddUnitDesign(unitDesign, unitDesign.Model, DepartmentOfDefense.UnitDesignState.Available);
		}
		unit.UnitDesign = unitDesign;
	}

	[ToutePourrite]
	internal void UnlockUnitDesign()
	{
		this.availableUnitBodyDefinitions.Clear();
		Diagnostics.Assert(this.unitBodyDefinitionDatabase != null);
		UnitBodyDefinition[] values = this.unitBodyDefinitionDatabase.GetValues();
		Diagnostics.Assert(values != null);
		IEnumerable<UnitBodyDefinition> enumerable = from definition in values
		where this.CheckWhetherUnitBodyDefinitionIsValid(definition)
		select definition;
		foreach (UnitBodyDefinition unitBodyDefinition in enumerable)
		{
			this.AddUnitBodyDefinition(unitBodyDefinition);
		}
		Diagnostics.Assert(this.unitDesignDatabase != null);
		IEnumerable<UnitDesign> enumerable2 = from unitDesign in this.unitDesignDatabase
		where this.UnitDesignDatabase.CheckWhetherUnitDesignIsValid(unitDesign)
		select unitDesign;
		foreach (UnitDesign unitDesign2 in enumerable2)
		{
			this.AddUnitDesign(unitDesign2, unitDesign2.Model, (!unitDesign2.Hidden) ? DepartmentOfDefense.UnitDesignState.Available : DepartmentOfDefense.UnitDesignState.Hidden);
		}
	}

	protected override IEnumerator OnInitialize()
	{
		yield return base.OnInitialize();
		this.SimulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
		Diagnostics.Assert(this.SimulationDescriptorDatabase != null);
		this.unitBodyDefinitionDatabase = Databases.GetDatabase<UnitBodyDefinition>(false);
		Diagnostics.Assert(this.unitBodyDefinitionDatabase != null);
		this.unitDesignDatabase = Databases.GetDatabase<UnitDesign>(false);
		Diagnostics.Assert(this.unitDesignDatabase != null);
		this.ItemDefinitionDatabase = Databases.GetDatabase<ItemDefinition>(false);
		Diagnostics.Assert(this.ItemDefinitionDatabase != null);
		this.PillageRewardDefinitionDatabase = Databases.GetDatabase<PillageRewardDefinition>(false);
		Diagnostics.Assert(this.PillageRewardDefinitionDatabase != null);
		this.GameService = Services.GetService<IGameService>();
		this.EventService = Services.GetService<IEventService>();
		if (this.EventService == null)
		{
			Diagnostics.LogError("Failed to retrieve the event service.");
		}
		this.EventService.EventRaise += this.EventService_EventRaise;
		this.GameEntityRepositoryService = this.GameService.Game.Services.GetService<IGameEntityRepositoryService>();
		if (this.GameEntityRepositoryService == null)
		{
			Diagnostics.LogError("Failed to retrieve the game entity repository service.");
		}
		this.PlayerControllerRepositoryService = this.GameService.Game.Services.GetService<IPlayerControllerRepositoryService>();
		if (this.PlayerControllerRepositoryService == null)
		{
			Diagnostics.LogError("Failed to retrieve the player controller repository service.");
		}
		this.WorldPositionningService = this.GameService.Game.Services.GetService<IWorldPositionningService>();
		if (this.PlayerControllerRepositoryService == null)
		{
			Diagnostics.LogError("Failed to retrieve the world positionning service.");
		}
		this.EncounterRepositoryService = this.GameService.Game.Services.GetService<IEncounterRepositoryService>();
		if (this.EncounterRepositoryService == null)
		{
			Diagnostics.LogError("Failed to retrieve the encounter repository service.");
		}
		this.PathfindingService = this.GameService.Game.Services.GetService<IPathfindingService>();
		Diagnostics.Assert(this.PathfindingService != null);
		if (DepartmentOfDefense.gameEntityRepositoryServiceStatic == null)
		{
			DepartmentOfDefense.gameEntityRepositoryServiceStatic = this.GameEntityRepositoryService;
			DepartmentOfDefense.worldPositionningServiceStatic = this.WorldPositionningService;
			DepartmentOfDefense.pathfindingServiceStatic = this.PathfindingService;
		}
		base.Empire.RegisterPass("GameServerState_Turn_Begin", "TeleportArmiesWhichHasForbiddenPosition", new Agency.Action(this.GameServerState_Turn_Begin_TeleportArmiesWhichHasForbiddenPosition), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_Begin", "ResetExperienceRewardOnUnits", new Agency.Action(this.GameClientState_Turn_Begin_ResetExperienceRewardOnUnits), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_Begin", "SpawnDelayedSolitaryUnit", new Agency.Action(this.GameClientState_Turn_Begin_SpawnDelayedSolitaryUnit), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_Begin", "AspiratingArmies", new Agency.Action(this.GameClientState_Turn_Begin_AspiratingArmies), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_Begin", "Technology281MadFairies", new Agency.Action(this.GameClientState_Turn_Begin_Technology281MadFairies), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_Begin", "ApplyDefensiveEffects", new Agency.Action(this.GameClientState_Turn_Begin_ApplyDefensiveEffects), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_Begin", "UnShiftUnits", new Agency.Action(this.GameClientState_Turn_Begin_UnShiftUnits), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_Begin", "CheckArmiesOnFreezingTiles", new Agency.Action(this.GameClientState_Turn_Begin_CheckArmiesOnFreezingTiles), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_Begin", "RemoveImmolation", new Agency.Action(this.GameClientState_Turn_Begin_RemoveImmolation), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_Begin", "ResetPerTurnCounters", new Agency.Action(this.GameClientState_Turn_Begin_ResetPerTurnCounters), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_End", "ArmyUnitExperiencePerTurnGain", new Agency.Action(this.GameClientState_Turn_End_UnitExperiencePerTurnGain), new string[]
		{
			"ComputeCityDefensePoint"
		});
		base.Empire.RegisterPass("GameClientState_Turn_End", "ArmyUnitHealthPerTurnGain", new Agency.Action(this.GameClientState_Turn_End_UnitHealthPerTurnGain), new string[]
		{
			"ComputeCityDefensePoint"
		});
		base.Empire.RegisterPass("GameClientState_Turn_End", "ResetUnitsActionPoints", new Agency.Action(this.GameClientState_Turn_End_ResetUnitsActionPoints), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_End", "ResetPortableForgeArmies", new Agency.Action(this.GameClientState_Turn_End_ResetPortableForgeArmies), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_End", "HandleMigrationPostConstruction", new Agency.Action(this.GameClientState_Turn_End_HandleMigrationPostConstruction), new string[]
		{
			"ComputeBuildConstruction"
		});
		base.Empire.RegisterPass("GameClientState_Turn_End", "CleanUnitDesignDatabase", new Agency.Action(this.GameClientState_Turn_End_CleanUnitDesignDatabase), new string[]
		{
			"ComputeBuildConstruction",
			"HandleMigrationPostConstruction"
		});
		base.Empire.RegisterPass("GameClientState_Turn_End", "ResetArmyTowerEffectsThisTurn", new Agency.Action(this.GameClientState_Turn_End_ResetArmyTowerEffectsThisTurn), new string[0]);
		base.Empire.RegisterPass("GameServerState_Turn_Ended", "SpawnDelayedSolitaryUnit", new Agency.Action(this.GameServerState_Turn_Ended_SpawnDelayedSolitaryUnit), new string[0]);
		DepartmentOfIndustry departmentOfIndustry = base.Empire.GetAgency<DepartmentOfIndustry>();
		if (departmentOfIndustry != null)
		{
			departmentOfIndustry.AddConstructionChangeEventHandler<UnitDesign>(new DepartmentOfIndustry.ConstructionChangeEventHandler(this.DepartmentOfIndustry_ConstructionChangeEventHandler));
			departmentOfIndustry.AddConstructionChangeEventHandler<CityConstructibleActionDefinition>(new DepartmentOfIndustry.ConstructionChangeEventHandler(this.DepartmentOfIndustry_CityConstructibleActionConstructionChange));
		}
		DepartmentOfScience departmentOfScience = base.Empire.GetAgency<DepartmentOfScience>();
		if (departmentOfScience != null)
		{
			departmentOfScience.TechnologyUnlocked += this.DepartmentOfScience_TechnologyUnlocked;
		}
		this.departmentOfTheTreasury = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		Diagnostics.Assert(this.departmentOfTheTreasury != null);
		this.departmentOfScience = base.Empire.GetAgency<DepartmentOfScience>();
		if (this.temporaryGarrison == null)
		{
			this.temporaryGarrison = new SimulationObject("DepartmentOfDefense.TemporaryGarrison");
			this.temporaryGarrison.Tags.AddTag("ClassArmy");
			this.temporaryGarrison.Tags.AddTag("ClassCity");
			this.temporaryGarrison.Tags.AddTag("Garrison");
			this.temporaryGarrison.ModifierForward = ModifierForwardType.ChildrenOnly;
			base.Empire.SimulationObject.AddChild(this.temporaryGarrison);
		}
		UnitDesign[] unitDesigns = this.unitDesignDatabase.GetValues();
		Diagnostics.Assert(unitDesigns != null);
		List<UnitDesign> unitDesignList = new List<UnitDesign>(unitDesigns);
		unitDesignList.Sort((UnitDesign left, UnitDesign right) => left.Model.CompareTo(right.Model));
		this.nextAvailableUnitDesignModelId = ((unitDesignList.Count <= 0) ? 0u : (unitDesignList[unitDesignList.Count - 1].Model + 1u));
		if (unitDesignList.Count > 1)
		{
			for (int index = 0; index < unitDesignList.Count - 1; index++)
			{
				Diagnostics.Assert(unitDesignList[index] != null && unitDesignList[index + 1] != null);
				if (unitDesignList[index].Model >= unitDesignList[index + 1].Model)
				{
					while (index < unitDesignList.Count - 1 && unitDesignList[index].Model == unitDesignList[index + 1].Model)
					{
						Diagnostics.LogError("Unit design {0} model number ({1}) is not unique.", new object[]
						{
							unitDesignList[index].Name,
							unitDesignList[index].Model
						});
						index++;
						if (index == unitDesignList.Count - 1)
						{
							Diagnostics.LogError("Unit design {0} model number ({1}) is not unique.", new object[]
							{
								unitDesignList[index].Name,
								unitDesignList[index].Model
							});
						}
					}
				}
			}
		}
		this.UnlockUnitDesign();
		this.temporaryGuid = 1UL;
		this.EnableDetection = false;
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		yield break;
	}

	protected override IEnumerator OnLoadGame(Amplitude.Unity.Game.Game game)
	{
		yield return base.OnLoadGame(game);
		this.visibilityService = game.GetService<IVisibilityService>();
		Diagnostics.Assert(this.visibilityService != null);
		this.weatherService = game.GetService<IWeatherService>();
		Diagnostics.Assert(this.weatherService != null);
		this.mapBoostService = game.GetService<IMapBoostService>();
		Diagnostics.Assert(this.mapBoostService != null);
		IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
		for (int index = 0; index < this.armies.Count; index++)
		{
			Army army = this.armies[index];
			if (!(army is KaijuArmy))
			{
				this.GameEntityRepositoryService.Register(army);
				foreach (Unit unit in army.Units)
				{
					this.GameEntityRepositoryService.Register(unit);
				}
			}
		}
		this.UnlockUnitDesign();
		if (base.Empire.SimulationObject.Tags.Contains("FactionTraitCultists14"))
		{
			IEnumerable<UnitDesign> convertedUnitDesigns = from unitDesign in this.unitDesignDatabase.GetValues()
			where unitDesign.Tags.Contains(DepartmentOfTheInterior.TagConvertedVillageUnit)
			select unitDesign;
			foreach (UnitDesign convertedUnitDesign in convertedUnitDesigns)
			{
				this.AddUnitDesign(convertedUnitDesign, convertedUnitDesign.Model, DepartmentOfDefense.UnitDesignState.Hidden);
			}
		}
		this.TechnologyDefinitionShipState = DepartmentOfScience.ConstructibleElement.State.NotAvailable;
		DepartmentOfScience departmentOfScience = base.Empire.GetAgency<DepartmentOfScience>();
		if (departmentOfScience != null)
		{
			this.TechnologyDefinitionShipState = departmentOfScience.GetTechnologyState("TechnologyDefinitionShip");
		}
		if (base.Empire is MinorEmpire)
		{
			this.TechnologyDefinitionShipState = DepartmentOfScience.ConstructibleElement.State.Researched;
		}
		if (base.Empire is LesserEmpire)
		{
			this.TechnologyDefinitionShipState = DepartmentOfScience.ConstructibleElement.State.Researched;
		}
		this.departmentOfForeignAffairs = base.Empire.GetAgency<DepartmentOfForeignAffairs>();
		if (this.departmentOfForeignAffairs != null)
		{
			this.departmentOfForeignAffairs.DiplomaticRelationStateChange += this.DepartmentOfForeignAffairs_DiplomaticRelationStateChange;
		}
		DepartmentOfTheInterior departmentOfTheInterior = base.Empire.GetAgency<DepartmentOfTheInterior>();
		if (departmentOfTheInterior != null)
		{
			departmentOfTheInterior.AssimilatedFactionsCollectionChanged += this.DepartmentOfTheInterior_AssimilatedFactionsCollectionChanged;
		}
		Diagnostics.Assert(this.armies != null);
		Diagnostics.Assert(this.WorldPositionningService != null);
		for (int index2 = 0; index2 < this.armies.Count; index2++)
		{
			DepartmentOfTheInterior.CheckBesiegerArmyStatus(this.armies[index2]);
			DepartmentOfTheInterior.CheckDefenderArmyStatus(this.armies[index2]);
		}
		this.EnableDetection = false;
		if (downloadableContentService != null)
		{
			bool hasDLC11 = downloadableContentService.IsShared(DownloadableContent11.ReadOnlyName);
			bool hasDLC12 = downloadableContentService.IsShared(DownloadableContent19.ReadOnlyName);
			if (hasDLC11 || hasDLC12)
			{
				if (hasDLC11)
				{
					this.EnableDetection = true;
					foreach (Army army2 in this.armies)
					{
						army2.WorldPositionChange += this.UpdateDetection_OnWorldPositionChange;
						army2.Refreshed += this.UpdateDetection_OnRefreshed;
					}
				}
				World world = (game as global::Game).World;
				this.forests = (world.Atlas.GetMap(WorldAtlas.Maps.Forests) as GridMap<byte>);
				if (this.forests == null)
				{
					GridMap<byte> terrainTypes = world.Atlas.GetMap(WorldAtlas.Maps.Terrain) as GridMap<byte>;
					IDatabase<TerrainTypeMapping> terrainTypeMappings = Databases.GetDatabase<TerrainTypeMapping>(false);
					if (terrainTypes != null && terrainTypeMappings != null)
					{
						Diagnostics.Assert(terrainTypes.Width == (int)world.WorldParameters.Columns);
						Diagnostics.Assert(terrainTypes.Height == (int)world.WorldParameters.Rows);
						this.forests = new GridMap<byte>(WorldAtlas.Maps.Forests, (int)world.WorldParameters.Columns, (int)world.WorldParameters.Rows, null);
						world.Atlas.RegisterMapInstance<GridMap<byte>>(this.forests);
						StaticString layerNameSimulation = new StaticString("Simulation");
						for (int index3 = 0; index3 < this.forests.Data.Length; index3++)
						{
							byte terrainType = terrainTypes.Data[index3];
							StaticString terrainTypeMappingName = this.WorldPositionningService.GetTerrainTypeMappingName(terrainType);
							TerrainTypeMapping terrainTypeMapping;
							if (terrainTypeMappings.TryGetValue(terrainTypeMappingName, out terrainTypeMapping))
							{
								bool forest = terrainTypeMapping.Layers.Any(delegate(SimulationLayer layer)
								{
									bool result;
									if (layer.Name == layerNameSimulation)
									{
										result = layer.Samples.Any((SimulationLayer.Sample sample) => sample.Value == "TerrainTagForest");
									}
									else
									{
										result = false;
									}
									return result;
								});
								if (forest)
								{
									this.forests.Data[index3] = 1;
								}
							}
						}
					}
				}
				if (hasDLC11)
				{
					foreach (Army army3 in this.armies)
					{
						this.UpdateDetection(army3);
					}
				}
			}
		}
		yield break;
	}

	protected override void OnRelease()
	{
		base.OnRelease();
		if (this.temporaryGarrison != null)
		{
			base.Empire.SimulationObject.RemoveChild(this.temporaryGarrison);
			this.temporaryGarrison.Dispose();
			this.temporaryGarrison = null;
		}
		for (int i = 0; i < this.armies.Count; i++)
		{
			this.armies[i].Dispose();
		}
		this.armies.Clear();
		foreach (UnitDesign unitDesign in this.UnitDesignDatabase.GetAvailableUnitDesignsAsEnumerable())
		{
			if (unitDesign.Context != null)
			{
				unitDesign.Context.Dispose();
				unitDesign.Context = null;
			}
		}
		this.availableUnitDesigns.Clear();
		this.hiddenUnitDesigns.Clear();
		this.producedUnitsPerContext.Clear();
		this.EncounterRepositoryService = null;
		this.GameService = null;
		this.GameEntityRepositoryService = null;
		this.SimulationDescriptorDatabase = null;
		this.PathfindingService = null;
		this.PlayerControllerRepositoryService = null;
		this.visibilityService = null;
		this.WorldPositionningService = null;
		if (DepartmentOfDefense.gameEntityRepositoryServiceStatic != null)
		{
			DepartmentOfDefense.gameEntityRepositoryServiceStatic = null;
			DepartmentOfDefense.worldPositionningServiceStatic = null;
			DepartmentOfDefense.pathfindingServiceStatic = null;
		}
		DepartmentOfIndustry agency = base.Empire.GetAgency<DepartmentOfIndustry>();
		if (agency != null)
		{
			agency.RemoveConstructionChangeEventHandler<UnitDesign>(new DepartmentOfIndustry.ConstructionChangeEventHandler(this.DepartmentOfIndustry_ConstructionChangeEventHandler));
			agency.RemoveConstructionChangeEventHandler<CityConstructibleActionDefinition>(new DepartmentOfIndustry.ConstructionChangeEventHandler(this.DepartmentOfIndustry_CityConstructibleActionConstructionChange));
		}
		DepartmentOfScience agency2 = base.Empire.GetAgency<DepartmentOfScience>();
		if (agency2 != null)
		{
			agency2.TechnologyUnlocked -= this.DepartmentOfScience_TechnologyUnlocked;
		}
		if (this.departmentOfForeignAffairs != null)
		{
			this.departmentOfForeignAffairs.DiplomaticRelationStateChange -= this.DepartmentOfForeignAffairs_DiplomaticRelationStateChange;
			this.departmentOfForeignAffairs = null;
		}
		DepartmentOfTheInterior agency3 = base.Empire.GetAgency<DepartmentOfTheInterior>();
		if (agency3 != null)
		{
			agency3.AssimilatedFactionsCollectionChanged -= this.DepartmentOfTheInterior_AssimilatedFactionsCollectionChanged;
		}
		if (this.EventService != null)
		{
			this.EventService.EventRaise -= this.EventService_EventRaise;
			this.EventService = null;
		}
	}

	public void AddArmy(Army army)
	{
		if (army.Empire != null && army.Empire != base.Empire)
		{
			Diagnostics.LogError("The department of defense was asked to add an army (guid: {0}, empire: {1}) but it is still bound to another empire.", new object[]
			{
				army.GUID,
				army.Empire.Name
			});
			return;
		}
		army.Empire = (global::Empire)base.Empire;
		int num = this.armies.BinarySearch((Army match) => match.GUID.CompareTo(army.GUID));
		if (num >= 0)
		{
			Diagnostics.LogWarning("The department of defense was asked to add an army (guid #{0}) but it is already present in its list of armies.", new object[]
			{
				army.GUID
			});
			return;
		}
		this.armies.Insert(~num, army);
		base.Empire.AddChild(army);
		base.Empire.Refresh(false);
		DepartmentOfTheInterior.CheckBesiegerArmyStatus(army);
		DepartmentOfTheInterior.CheckDefenderArmyStatus(army);
		army.Refresh(false);
		this.OnArmiesCollectionChange(CollectionChangeAction.Add, army);
	}

	private bool ComputeRegenModifier(Army army, out float overallRegenModifier, out int pacifiedVillageCount)
	{
		overallRegenModifier = 0f;
		pacifiedVillageCount = 0;
		bool flag = false;
		bool flag2 = false;
		Region region = this.WorldPositionningService.GetRegion(army.WorldPosition);
		StaticString propertyName;
		if (!this.ComputeEmpireRegenModifierPropertyNameForRegion(region, out propertyName))
		{
			return false;
		}
		overallRegenModifier = base.Empire.GetPropertyValue(propertyName);
		overallRegenModifier += army.GetPropertyValue(propertyName);
		global::Empire owner = region.Owner;
		if (owner != null)
		{
			if (base.Empire == owner)
			{
				flag2 = true;
			}
			else if (base.Empire is MajorEmpire && owner is MajorEmpire)
			{
				DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.DiplomaticRelations[owner.Index];
				if (diplomaticRelation != null && diplomaticRelation.State.Name == DiplomaticRelationState.Names.Alliance)
				{
					flag = true;
				}
			}
		}
		if (flag || flag2)
		{
			float num = 0f;
			if (region.City != null)
			{
				pacifiedVillageCount = Mathf.FloorToInt(region.City.GetPropertyValue(SimulationProperties.NumberOfRebuildPacifiedVillage));
				float propertyValue;
				if (flag2)
				{
					propertyValue = region.City.GetPropertyValue(SimulationProperties.OwnedUnitRegenModifier);
				}
				else
				{
					propertyValue = region.City.GetPropertyValue(SimulationProperties.AlliedUnitRegenModifier);
				}
				if (propertyValue != 0f)
				{
					int distanceLimit = (int)region.City.GetPropertyValue(SimulationProperties.UnitRegenLimitDistance);
					if (region.City.Districts.Any((District match) => this.WorldPositionningService.GetDistance(match.WorldPosition, army.WorldPosition) <= distanceLimit))
					{
						num = propertyValue;
					}
				}
				if (region.City.BesiegingEmpire != null)
				{
					for (int i = 0; i < region.City.Districts.Count; i++)
					{
						if (region.City.Districts[i].WorldPosition == army.WorldPosition)
						{
							overallRegenModifier = 0f;
							return false;
						}
					}
				}
			}
			for (int j = 0; j < region.PointOfInterests.Length; j++)
			{
				float propertyValue2;
				if (flag2)
				{
					propertyValue2 = region.PointOfInterests[j].GetPropertyValue(SimulationProperties.OwnedUnitRegenModifier);
				}
				else
				{
					propertyValue2 = region.PointOfInterests[j].GetPropertyValue(SimulationProperties.AlliedUnitRegenModifier);
				}
				if (propertyValue2 != 0f)
				{
					int num2 = (int)region.PointOfInterests[j].GetPropertyValue(SimulationProperties.UnitRegenLimitDistance);
					bool flag3 = this.WorldPositionningService.ContainsTerrainTag(army.WorldPosition, "TerrainTagVolcanic");
					if (army.Empire.SimulationObject.Tags.Contains("FactionTraitFlames3") || !flag3)
					{
						if (this.WorldPositionningService.GetDistance(region.PointOfInterests[j].WorldPosition, army.WorldPosition) <= num2)
						{
							if (num < propertyValue2)
							{
								num = propertyValue2;
							}
							army.HasBeenHealedByTower = true;
						}
					}
				}
			}
			if (num != 0f)
			{
				overallRegenModifier += num;
			}
		}
		return true;
	}

	private Army CreateArmy(GameEntityGUID guid, bool generateArmyName = true)
	{
		Army army = new Army(guid)
		{
			Empire = (base.Empire as global::Empire)
		};
		SimulationDescriptor descriptor = null;
		if (this.SimulationDescriptorDatabase.TryGetValue("ClassArmy", out descriptor))
		{
			army.AddDescriptor(descriptor, false);
		}
		else
		{
			Diagnostics.LogError("Unable to retrieve the 'ClassArmy' simulation descriptor from the database.");
		}
		if (generateArmyName)
		{
			army.ClientLocalizedName = this.GenerateArmyClientLocalizedName(army);
		}
		DepartmentOfScience.ConstructibleElement.State technologyDefinitionShipState = this.TechnologyDefinitionShipState;
		if (technologyDefinitionShipState == DepartmentOfScience.ConstructibleElement.State.Researched)
		{
			SimulationDescriptor descriptor2;
			if (this.SimulationDescriptorDatabase != null && this.SimulationDescriptorDatabase.TryGetValue(PathfindingContext.MovementCapacitySailDescriptor, out descriptor2))
			{
				army.AddDescriptor(descriptor2, true);
				foreach (Unit unit in army.Units)
				{
					unit.AddDescriptor(descriptor2, true);
				}
			}
		}
		return army;
	}

	internal KaijuArmy CreateKaijuArmy(GameEntityGUID guid, WorldPosition worldPosition, bool generateArmyName = true)
	{
		KaijuArmy kaijuArmy = new KaijuArmy(guid)
		{
			Empire = (base.Empire as global::Empire)
		};
		kaijuArmy.SetWorldPositionWithEstimatedTimeOfArrival(worldPosition, global::Game.Time);
		SimulationDescriptor descriptor = null;
		if (this.SimulationDescriptorDatabase.TryGetValue("ClassArmy", out descriptor))
		{
			kaijuArmy.AddDescriptor(descriptor, false);
		}
		else
		{
			Diagnostics.LogError("Unable to retrieve the 'ClassArmy' simulation descriptor from the database.");
		}
		if (generateArmyName)
		{
			kaijuArmy.ClientLocalizedName = this.GenerateArmyClientLocalizedName(kaijuArmy);
		}
		DepartmentOfScience.ConstructibleElement.State technologyDefinitionShipState = this.TechnologyDefinitionShipState;
		if (technologyDefinitionShipState == DepartmentOfScience.ConstructibleElement.State.Researched)
		{
			SimulationDescriptor descriptor2;
			if (this.SimulationDescriptorDatabase != null && this.SimulationDescriptorDatabase.TryGetValue(PathfindingContext.MovementCapacitySailDescriptor, out descriptor2))
			{
				kaijuArmy.AddDescriptor(descriptor2, true);
				foreach (Unit unit in kaijuArmy.Units)
				{
					unit.AddDescriptor(descriptor2, true);
				}
			}
		}
		return kaijuArmy;
	}

	private Unit CreateUnitByDesignName(GameEntityGUID guid, StaticString unitDesignName)
	{
		if (string.IsNullOrEmpty(unitDesignName))
		{
			Diagnostics.LogError("Cannot create a unit because the unit design name is empty.");
			return null;
		}
		UnitDesign unitDesign = null;
		try
		{
			unitDesign = this.UnitDesignDatabase.GetAvailableUnitDesignsAsEnumerable().SingleOrDefault((UnitDesign enumerableUnitDesign) => enumerableUnitDesign.Name == unitDesignName);
			if (unitDesign == null)
			{
				Diagnostics.LogError("Cannot create a unit because the unit design '{0}' is not available.", new object[]
				{
					unitDesignName
				});
				return null;
			}
		}
		catch (InvalidOperationException ex)
		{
			Diagnostics.LogError("{0}: Cannot create a unit because the unit design's name '{1}' is not unique. exception={0}", new object[]
			{
				ex.Message,
				unitDesignName
			});
			return null;
		}
		return DepartmentOfDefense.CreateUnitByDesign(guid, unitDesign);
	}

	private Unit CreateUnitIntoGarrison(GameEntityGUID guid, UnitDesign unitDesign, IGarrison garrison)
	{
		Diagnostics.Assert(guid.IsValid);
		Diagnostics.Assert(unitDesign != null);
		if (garrison == null)
		{
			Diagnostics.LogError("Cannot proceed with unit creation because there is no garrison to add the unit to.");
			this.GameEntityRepositoryService.Unregister(guid);
			return null;
		}
		Diagnostics.Assert(garrison != null);
		Unit unit = DepartmentOfDefense.CreateUnitByDesign(guid, unitDesign);
		if (unitDesign.UnlockedUnitRankReferences != null)
		{
			unit.ForceRank(Array.ConvertAll<XmlNamedReference, StaticString>(unitDesign.UnlockedUnitRankReferences, (XmlNamedReference match) => match.Name));
		}
		unit.Refresh(true);
		unit.UpdateExperienceReward(base.Empire);
		unit.UpdateShiftingForm();
		unit.Refresh(true);
		garrison.AddUnit(unit);
		this.GameEntityRepositoryService.Swap(unit);
		float propertyValue = unit.GetPropertyValue(SimulationProperties.UnitExperienceRewardAtCreation);
		if (propertyValue > 0f)
		{
			unit.GainXp(propertyValue, true, true);
		}
		return unit;
	}

	private void DepartmentOfForeignAffairs_DiplomaticRelationStateChange(object sender, DiplomaticRelationStateChangeEventArgs e)
	{
		if (e.Empire.Index == base.Empire.Index || e.EmpireWithWhichTheStatusChange.Index == base.Empire.Index)
		{
			global::Empire empire = (e.Empire.Index != base.Empire.Index) ? e.Empire : e.EmpireWithWhichTheStatusChange;
			DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(empire);
			if (diplomaticRelation != null && !diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.SiegeCities))
			{
				DepartmentOfTheInterior agency = empire.GetAgency<DepartmentOfTheInterior>();
				foreach (City city in agency.Cities)
				{
					if (city.BesiegingEmpireIndex == base.Empire.Index)
					{
						Army[] besiegers = DepartmentOfTheInterior.GetBesiegers(city);
						if (besiegers.Any((Army army) => !army.IsPrivateers))
						{
							agency.StopSiege(city);
						}
					}
					List<Army> list = (from besiegingArmy in city.BesiegingSeafaringArmies
					where besiegingArmy.Empire.Index == base.Empire.Index
					select besiegingArmy).ToList<Army>();
					if (list.Count != 0)
					{
						agency.StopNavalSiege(city, list);
					}
				}
			}
			if (diplomaticRelation == null || diplomaticRelation.State == null || StaticString.IsNullOrEmpty(diplomaticRelation.State.Name) || !diplomaticRelation.State.Name.Equals(DiplomaticRelationState.Names.War))
			{
				for (int i = 0; i < this.armies.Count; i++)
				{
					Army army2 = this.armies[i];
					if (army2.IsEarthquaker)
					{
						City city2 = this.WorldPositionningService.GetRegion(army2.WorldPosition).City;
						if (city2 != null && city2.Empire.Index == e.EmpireWithWhichTheStatusChange.Index)
						{
							army2.SetEarthquakerStatus(false, false, null);
						}
					}
				}
			}
		}
		for (int j = 0; j < this.armies.Count; j++)
		{
			this.Armies[j].CallRefreshAppliedRegionEffects();
		}
	}

	private void DepartmentOfIndustry_ConstructionChangeEventHandler(object sender, ConstructionChangeEventArgs e)
	{
		Diagnostics.Assert(e.Construction.ConstructibleElement != null);
		ConstructionChangeEventAction action = e.Action;
		if (action == ConstructionChangeEventAction.Completed)
		{
			UnitDesign unitDesign = e.Construction.ConstructibleElement as UnitDesign;
			if (unitDesign.Tags.Contains(DownloadableContent9.TagColossus) || unitDesign.Tags.Contains(DownloadableContent9.TagSolitary) || unitDesign.Tags.Contains(DownloadableContent16.TagSeafaring))
			{
				IGarrison garrison = e.Context as Garrison;
				DelayedSolitaryUnitSpawnCommand delayedSolitaryUnitSpawnCommand = new DelayedSolitaryUnitSpawnCommand(garrison.GUID, e.Construction.GUID, unitDesign, e.Construction.WorldPosition);
				if (delayedSolitaryUnitSpawnCommand == null)
				{
					Diagnostics.LogError("DelayedSolitaryUnitSpawnCommand should not be null.");
					return;
				}
				if (this.delayedSolitaryUnitSpawnCommands == null)
				{
					this.delayedSolitaryUnitSpawnCommands = new List<DelayedSolitaryUnitSpawnCommand>();
				}
				this.delayedSolitaryUnitSpawnCommands.Add(delayedSolitaryUnitSpawnCommand);
			}
			if (!unitDesign.UnitBodyDefinition.Tags.Contains(DownloadableContent16.TagSeafaring) && !unitDesign.UnitBodyDefinition.Tags.Contains("Settler"))
			{
				this.LandUnitBuildOrPurchased = true;
			}
			this.CreateUnitIntoGarrison(e.Construction.GUID, unitDesign, e.Context as Garrison);
		}
	}

	private void DepartmentOfIndustry_CityConstructibleActionConstructionChange(object sender, ConstructionChangeEventArgs e)
	{
		Diagnostics.Assert(e.Construction.ConstructibleElement != null);
		CityConstructibleActionDefinition cityConstructibleActionDefinition = e.Construction.ConstructibleElement as CityConstructibleActionDefinition;
		if (cityConstructibleActionDefinition == null)
		{
			return;
		}
		ConstructionChangeEventAction action = e.Action;
		if (action == ConstructionChangeEventAction.Completed)
		{
			string text = cityConstructibleActionDefinition.Action;
			if (text != null)
			{
				if (DepartmentOfDefense.<>f__switch$map10 == null)
				{
					DepartmentOfDefense.<>f__switch$map10 = new Dictionary<string, int>(2)
					{
						{
							"Raze",
							0
						},
						{
							"Migrate",
							1
						}
					};
				}
				int num;
				if (DepartmentOfDefense.<>f__switch$map10.TryGetValue(text, out num))
				{
					if (num != 0)
					{
						if (num == 1)
						{
							if (e.Context is City)
							{
								this.pendingCitiesToMigrate.Add(e.Construction.GUID, e.Context as City);
							}
						}
					}
					else
					{
						if (e.Context is City)
						{
							City city = e.Context as City;
							if (city.IsInfected)
							{
								goto IL_172;
							}
						}
						UnitDesign unitDesign2 = (from unitDesign in this.UnitDesignDatabase.DatabaseCompatibleUnitDesigns
						where unitDesign.Hidden && unitDesign.CheckUnitAbility(UnitAbility.ReadonlyColonize, -1)
						select unitDesign).FirstOrDefault<UnitDesign>();
						if (unitDesign2 != null)
						{
							this.CreateUnitIntoGarrison(e.Construction.GUID, unitDesign2, e.Context as IGarrison);
						}
					}
				}
			}
			IL_172:;
		}
	}

	private void DepartmentOfScience_TechnologyUnlocked(object sender, ConstructibleElementEventArgs e)
	{
		this.UnlockUnitDesign();
		string text = e.ConstructibleElement.Name;
		if (text != null)
		{
			if (DepartmentOfDefense.<>f__switch$map11 == null)
			{
				DepartmentOfDefense.<>f__switch$map11 = new Dictionary<string, int>(1)
				{
					{
						"TechnologyDefinitionShip",
						0
					}
				};
			}
			int num;
			if (DepartmentOfDefense.<>f__switch$map11.TryGetValue(text, out num))
			{
				if (num == 0)
				{
					this.TechnologyDefinitionShipState = DepartmentOfScience.ConstructibleElement.State.Researched;
					SimulationDescriptor descriptor;
					if (this.SimulationDescriptorDatabase != null && this.SimulationDescriptorDatabase.TryGetValue(PathfindingContext.MovementCapacitySailDescriptor, out descriptor))
					{
						foreach (Army army in this.Armies)
						{
							army.AddDescriptor(descriptor, true);
							foreach (Unit unit in army.Units)
							{
								unit.AddDescriptor(descriptor, true);
							}
						}
					}
				}
			}
		}
	}

	private void DepartmentOfTheInterior_AssimilatedFactionsCollectionChanged(object sender, CollectionChangeEventArgs e)
	{
		Faction faction = e.Element as Faction;
		if (faction == null)
		{
			return;
		}
		this.UnlockUnitDesign();
		CollectionChangeAction action = e.Action;
		if (action != CollectionChangeAction.Add)
		{
			if (action == CollectionChangeAction.Remove)
			{
				for (int i = this.availableUnitDesigns.Count - 1; i >= 0; i--)
				{
					UnitDesign unitDesign = this.availableUnitDesigns[i];
					if (unitDesign.UnitBodyDefinition != null)
					{
						if (unitDesign.UnitBodyDefinition.Affinity != null)
						{
							if (!(unitDesign.UnitBodyDefinition.Affinity.Name != faction.Affinity.Name))
							{
								UnitDesign unitDesign3 = this.hiddenUnitDesigns.Find((UnitDesign match) => match.Model == unitDesign.Model);
								if (unitDesign3 != null)
								{
									if (unitDesign3.ModelRevision > unitDesign.ModelRevision)
									{
										goto IL_26D;
									}
									this.hiddenUnitDesigns.Remove(unitDesign3);
								}
								this.hiddenUnitDesigns.Add(unitDesign);
								this.availableUnitDesigns.RemoveAt(i);
							}
						}
					}
					IL_26D:;
				}
			}
		}
		else
		{
			for (int j = this.hiddenUnitDesigns.Count - 1; j >= 0; j--)
			{
				UnitDesign unitDesign = this.hiddenUnitDesigns[j];
				if (unitDesign.UnitBodyDefinition != null)
				{
					if (unitDesign.UnitBodyDefinition.Affinity != null)
					{
						if (!(unitDesign.UnitBodyDefinition.Affinity.Name != faction.Affinity.Name))
						{
							if (!unitDesign.Tags.Contains(DepartmentOfTheInterior.TagConvertedVillageUnit) && !unitDesign.Tags.Contains(TradableUnit.ReadOnlyMercenary))
							{
								UnitDesign unitDesign2 = this.availableUnitDesigns.Find((UnitDesign match) => match.Model == unitDesign.Model);
								if (unitDesign2 != null)
								{
									if (unitDesign2.ModelRevision > unitDesign.ModelRevision)
									{
										goto IL_165;
									}
									this.availableUnitDesigns.Remove(unitDesign2);
								}
								this.availableUnitDesigns.Add(unitDesign);
								this.hiddenUnitDesigns.RemoveAt(j);
							}
						}
					}
				}
				IL_165:;
			}
		}
	}

	private void EventService_EventRaise(object sender, EventRaiseEventArgs e)
	{
		StaticString eventName = e.RaisedEvent.EventName;
		if (eventName.Equals(EventKaijuTamed.Name) || eventName.Equals(EventKaijuLost.Name))
		{
			this.UnlockUnitDesign();
		}
	}

	private IEnumerator GameClientState_Turn_Begin_CheckArmiesOnFreezingTiles(string context, string name)
	{
		this.CheckArmiesOnFreezingTiles();
		yield break;
	}

	private IEnumerator GameClientState_Turn_Begin_SpawnDelayedSolitaryUnit(string context, string name)
	{
		Diagnostics.Assert(base.Empire != null);
		Diagnostics.Assert(this.armies != null);
		if (!(base.Empire is MajorEmpire))
		{
			yield break;
		}
		if (this.delayedSolitaryUnitSpawnCommands == null)
		{
			yield break;
		}
		global::Empire.PlayerControllersContainer playerControllers = (base.Empire as MajorEmpire).PlayerControllers;
		if (playerControllers.Client != null)
		{
			for (int index = 0; index < this.delayedSolitaryUnitSpawnCommands.Count; index++)
			{
				DelayedSolitaryUnitSpawnCommand command = this.delayedSolitaryUnitSpawnCommands[index];
				if (command.UnitDesign != null)
				{
					if (command.UnitDesign.Tags.Contains(DownloadableContent9.TagColossus) || command.UnitDesign.Tags.Contains(DownloadableContent9.TagSolitary))
					{
						OrderTransferSolitaryUnitToNewArmy order = new OrderTransferSolitaryUnitToNewArmy(base.Empire.Index, command.GarrisonGameEntityGUID, command.GameEntityGUID, command.WorldPosition, StaticString.Empty);
						playerControllers.Server.PostOrder(order);
					}
					else if (command.UnitDesign.Tags.Contains(DownloadableContent16.TagSeafaring))
					{
						OrderTransferSeafaringUnitToNewArmy order2 = new OrderTransferSeafaringUnitToNewArmy(base.Empire.Index, command.GarrisonGameEntityGUID, command.GameEntityGUID, command.WorldPosition, StaticString.Empty);
						playerControllers.Server.PostOrder(order2);
					}
				}
			}
			yield break;
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_Begin_AspiratingArmies(string context, string name)
	{
		Diagnostics.Assert(base.Empire != null);
		Diagnostics.Assert(this.armies != null);
		if (!(base.Empire is MajorEmpire))
		{
			yield break;
		}
		global::Empire.PlayerControllersContainer playerControllers = (base.Empire as MajorEmpire).PlayerControllers;
		if (playerControllers.Client != null)
		{
			for (int index = 0; index < this.armies.Count; index++)
			{
				if (this.armies[index].IsAspirating)
				{
					IGameEntity gameEntity = null;
					PointOfInterest pointOfInterest = null;
					if (!this.GameEntityRepositoryService.TryGetValue(this.armies[index].AspirateTarget, out gameEntity))
					{
						Diagnostics.LogWarning("Fail getting the targeted ResourceDeposit to aspirate.");
					}
					else
					{
						pointOfInterest = (gameEntity as PointOfInterest);
						if (pointOfInterest.SimulationObject.Tags.Contains("ExploitedPointOfInterest"))
						{
							this.StopAspirating(this.armies[index]);
							yield break;
						}
						string resourceType = null;
						if (!pointOfInterest.PointOfInterestDefinition.TryGetValue("ResourceName", out resourceType))
						{
							Diagnostics.LogWarning("Fail getting the targeted ResourceName to aspirate.");
						}
						else
						{
							float resourceAmount = this.armies[index].SimulationObject.GetPropertyValue("AspirateValue");
							EventAspirate eventAspirate = new EventAspirate(base.Empire, resourceType, resourceAmount, pointOfInterest, true);
							IEventService eventService = Services.GetService<IEventService>();
							if (eventService != null)
							{
								eventService.Notify(eventAspirate);
							}
						}
					}
				}
			}
			yield break;
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_Begin_ApplyDefensiveEffects(string context, string name)
	{
		Diagnostics.Assert(base.Empire != null);
		Diagnostics.Assert(this.armies != null);
		if (!(base.Empire is MajorEmpire))
		{
			yield break;
		}
		global::Game game = this.GameService.Game as global::Game;
		if (game != null && this.TurnWhenLastBegun >= game.Turn)
		{
			yield break;
		}
		this.TurnWhenLastBegun = game.Turn;
		DepartmentOfTheInterior departmentOfTheInterior = base.Empire.GetAgency<DepartmentOfTheInterior>();
		if (departmentOfTheInterior == null)
		{
			Diagnostics.LogError("DepartmentOfTheInterior can't be null.");
			yield break;
		}
		DepartmentOfDefense otherEmpireDepartmentOfDefense = null;
		for (int index = 0; index < game.Empires.Length; index++)
		{
			if (index != base.Empire.Index)
			{
				bool isImmuneToDefensiveImprovements = false;
				if (game.Empires[index] is MajorEmpire)
				{
					DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(index);
					if (diplomaticRelation == null || diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.ImmuneToDefensiveImprovements))
					{
						isImmuneToDefensiveImprovements = true;
						goto IL_442;
					}
				}
				otherEmpireDepartmentOfDefense = game.Empires[index].GetAgency<DepartmentOfDefense>();
				Diagnostics.Assert(otherEmpireDepartmentOfDefense != null);
				for (int cityIndex = 0; cityIndex < departmentOfTheInterior.Cities.Count; cityIndex++)
				{
					if (departmentOfTheInterior.Cities[cityIndex].Ownership[base.Empire.Index] >= 1f)
					{
						float defensivePower = departmentOfTheInterior.Cities[cityIndex].GetPropertyValue(SimulationProperties.DefensivePower);
						float coastalDefensivePower = departmentOfTheInterior.Cities[cityIndex].GetPropertyValue(SimulationProperties.CoastalDefensivePower);
						if (defensivePower + coastalDefensivePower > 0f)
						{
							otherEmpireDepartmentOfDefense.CheckAndSlapArmiesOverCityExploitation(departmentOfTheInterior.Cities[cityIndex], isImmuneToDefensiveImprovements);
						}
						for (int poiIndex = 0; poiIndex < departmentOfTheInterior.Cities[cityIndex].Region.PointOfInterests.Length; poiIndex++)
						{
							PointOfInterest poi = departmentOfTheInterior.Cities[cityIndex].Region.PointOfInterests[poiIndex];
							if (poi.GetPropertyValue(SimulationProperties.DefensivePower) > 0f && poi.CreepingNodeGUID == GameEntityGUID.Zero)
							{
								otherEmpireDepartmentOfDefense.CheckAndSlapArmiesInRange(poi, isImmuneToDefensiveImprovements);
							}
						}
					}
				}
				IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
				if (downloadableContentService.IsShared(DownloadableContent20.ReadOnlyName))
				{
					DepartmentOfCreepingNodes departmentOfCreepingNodes = base.Empire.GetAgency<DepartmentOfCreepingNodes>();
					if (departmentOfCreepingNodes != null)
					{
						for (int creepingNodeIndex = 0; creepingNodeIndex < departmentOfCreepingNodes.Nodes.Count; creepingNodeIndex++)
						{
							CreepingNode creepingNode = departmentOfCreepingNodes.Nodes[creepingNodeIndex];
							if (!creepingNode.IsUnderConstruction && creepingNode.GetPropertyValue(SimulationProperties.CreepingNodeRetaliationDamage) > 0f)
							{
								otherEmpireDepartmentOfDefense.CheckAndSlapArmiesInRange(creepingNode, isImmuneToDefensiveImprovements);
							}
						}
					}
				}
			}
			IL_442:;
		}
		yield break;
	}

	private IEnumerator GameServerState_Turn_Ended_SpawnDelayedSolitaryUnit(string context, string name)
	{
		Diagnostics.Assert(base.Empire != null);
		Diagnostics.Assert(this.armies != null);
		if (!(base.Empire is MajorEmpire))
		{
			yield break;
		}
		if (this.delayedSolitaryUnitSpawnCommands == null)
		{
			yield break;
		}
		global::Empire.PlayerControllersContainer playerControllers = (base.Empire as MajorEmpire).PlayerControllers;
		for (int index = 0; index < this.delayedSolitaryUnitSpawnCommands.Count; index++)
		{
			DelayedSolitaryUnitSpawnCommand command = this.delayedSolitaryUnitSpawnCommands[index];
			if (command.UnitDesign.Tags.Contains(DownloadableContent9.TagColossus) || command.UnitDesign.Tags.Contains(DownloadableContent9.TagSolitary))
			{
				OrderTransferSolitaryUnitToNewArmy order = new OrderTransferSolitaryUnitToNewArmy(base.Empire.Index, command.GarrisonGameEntityGUID, command.GameEntityGUID, command.WorldPosition, StaticString.Empty);
				playerControllers.Server.PostOrder(order);
			}
			else if (command.UnitDesign.Tags.Contains(DownloadableContent16.TagSeafaring))
			{
				OrderTransferSeafaringUnitToNewArmy order2 = new OrderTransferSeafaringUnitToNewArmy(base.Empire.Index, command.GarrisonGameEntityGUID, command.GameEntityGUID, command.WorldPosition, StaticString.Empty);
				playerControllers.Server.PostOrder(order2);
			}
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_Begin_Technology281MadFairies(string context, string name)
	{
		float bonusMovementOnEnemySpotted = base.Empire.GetPropertyValue(SimulationProperties.BonusMovementOnEnemySpotted);
		if (bonusMovementOnEnemySpotted <= 0f)
		{
			yield break;
		}
		if (this.WorldPositionningService == null)
		{
			yield break;
		}
		if (this.WorldPositionningService.World == null)
		{
			yield break;
		}
		if (this.visibilityService == null)
		{
			yield break;
		}
		if (this.GameService == null)
		{
			yield break;
		}
		global::Game game = this.GameService.Game as global::Game;
		if (game == null || game.Empires == null)
		{
			yield break;
		}
		List<Army> visibleEnemyArmies = new List<Army>();
		for (int index = 0; index < game.Empires.Length; index++)
		{
			global::Empire empire = game.Empires[index];
			if (empire.Index != base.Empire.Index)
			{
				DepartmentOfDefense departmentOfDefense = empire.GetAgency<DepartmentOfDefense>();
				if (departmentOfDefense != null)
				{
					foreach (Army army in departmentOfDefense.Armies)
					{
						if (this.visibilityService.IsWorldPositionVisibleFor(army.WorldPosition, base.Empire as global::Empire))
						{
							visibleEnemyArmies.Add(army);
						}
					}
				}
			}
		}
		if (visibleEnemyArmies.Count > 0)
		{
			using (IEnumerator<Army> enumerator2 = this.Armies.GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					Army army2 = enumerator2.Current;
					float visionRange = army2.GetPropertyValue(SimulationProperties.VisionRange);
					foreach (Army visibleEnemyArmy in visibleEnemyArmies)
					{
						float distance = (float)this.WorldPositionningService.GetDistance(army2.WorldPosition, visibleEnemyArmy.WorldPosition);
						if (distance <= visionRange)
						{
							foreach (Unit unit in army2.Units)
							{
								float maximumMovementPoints = this.PathfindingService.GetMaximumMovementPoints(army2.WorldPosition, unit, (PathfindingFlags)0);
								float bonusRatio = (maximumMovementPoints <= 0f) ? 0f : (bonusMovementOnEnemySpotted / maximumMovementPoints);
								unit.SetPropertyBaseValue(SimulationProperties.MovementRatio, unit.GetPropertyValue(SimulationProperties.MovementRatio) + bonusRatio);
							}
							army2.Refresh(false);
							break;
						}
					}
				}
				yield break;
			}
			yield break;
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_HandleMigrationPostConstruction(string context, string name)
	{
		if (this.pendingCitiesToMigrate.Count == 0)
		{
			yield break;
		}
		UnitDesign compatibleUnitDesign = (from unitDesign in this.UnitDesignDatabase.DatabaseCompatibleUnitDesigns
		where unitDesign.Hidden && unitDesign.Tags.Contains(UnitDesign.TagMigrationUnit)
		select unitDesign).FirstOrDefault<UnitDesign>();
		if (compatibleUnitDesign != null)
		{
			foreach (KeyValuePair<GameEntityGUID, City> keyValuePair in this.pendingCitiesToMigrate)
			{
				GameEntityGUID gameEntityGUID = keyValuePair.Key;
				City city = keyValuePair.Value;
				Unit settler = this.CreateUnitIntoGarrison(gameEntityGUID, compatibleUnitDesign, city);
				if (settler != null)
				{
					for (int cityImprovementIndex = 0; cityImprovementIndex < city.CityImprovements.Count; cityImprovementIndex++)
					{
						settler.AddCarriedCityImprovement(city.CityImprovements[cityImprovementIndex]);
					}
					DepartmentOfIndustry departmentOfIndustry = base.Empire.GetAgency<DepartmentOfIndustry>();
					DepartmentOfTheInterior departmentOfTheInterior = base.Empire.GetAgency<DepartmentOfTheInterior>();
					ResourceDefinition[] migrationCarriedResources = DepartmentOfTheTreasury.GetMigrationCarriedResources();
					for (int index = 0; index < migrationCarriedResources.Length; index++)
					{
						StaticString resourceName = migrationCarriedResources[index].Name;
						float stock = 0f;
						if (this.departmentOfTheTreasury.TryGetResourceStockValue(city, resourceName, out stock, false))
						{
							stock += (float)city.Districts.Count((District district) => district.ResourceOnMigration == resourceName);
							if (departmentOfTheInterior != null && departmentOfIndustry != null)
							{
								foreach (OrderCreateDistrictImprovement order in departmentOfTheInterior.PendingExtensions)
								{
									DepartmentOfIndustry.ConstructibleElement constructibleElement;
									if (departmentOfIndustry.ConstructibleElementDatabase.TryGetValue(order.ConstructibleElementName, out constructibleElement))
									{
										DistrictImprovementDefinition districtImprovementDefinition = constructibleElement as DistrictImprovementDefinition;
										if (districtImprovementDefinition != null && districtImprovementDefinition.ResourceOnMigration == resourceName && !districtImprovementDefinition.IsUnique)
										{
											stock += 1f;
										}
									}
								}
							}
							if (!this.departmentOfTheTreasury.TryTransferResources(settler, resourceName, stock))
							{
								Diagnostics.LogError("Wasn't able to transfer resource {1} to unit#{0}", new object[]
								{
									settler.GUID,
									resourceName
								});
							}
						}
					}
					if (departmentOfTheInterior != null && city.GUID == departmentOfTheInterior.MainCityGUID)
					{
						settler.SimulationObject.Tags.AddTag(City.TagMainCity);
					}
				}
			}
		}
		this.pendingCitiesToMigrate.Clear();
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_ResetUnitsActionPoints(string context, string name)
	{
		foreach (Army army in this.Armies)
		{
			foreach (Unit unit in army.Units)
			{
				unit.SetPropertyBaseValue(SimulationProperties.ActionPointsSpent, 0f);
			}
			army.Refresh(false);
		}
		DepartmentOfTheInterior departmentOfTheInterior = base.Empire.GetAgency<DepartmentOfTheInterior>();
		if (departmentOfTheInterior != null)
		{
			foreach (City city in departmentOfTheInterior.Cities)
			{
				foreach (Unit unit2 in city.Units)
				{
					unit2.SetPropertyBaseValue(SimulationProperties.ActionPointsSpent, 0f);
				}
				foreach (Unit unit3 in city.Militia.Units)
				{
					unit3.SetPropertyBaseValue(SimulationProperties.ActionPointsSpent, 0f);
				}
				if (city.Camp != null)
				{
					foreach (Unit unit4 in city.Camp.StandardUnits)
					{
						unit4.SetPropertyBaseValue(SimulationProperties.ActionPointsSpent, 0f);
					}
				}
				city.Refresh(false);
			}
			foreach (Fortress fortress in departmentOfTheInterior.OccupiedFortresses)
			{
				foreach (Unit unit5 in fortress.Units)
				{
					unit5.SetPropertyBaseValue(SimulationProperties.ActionPointsSpent, 0f);
				}
				fortress.Refresh(false);
			}
		}
		DepartmentOfEducation departmentOfEducation = base.Empire.GetAgency<DepartmentOfEducation>();
		if (departmentOfEducation != null)
		{
			foreach (Unit unit6 in departmentOfEducation.Heroes)
			{
				unit6.SetPropertyBaseValue(SimulationProperties.ActionPointsSpent, 0f);
			}
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_UnitExperiencePerTurnGain(string context, string name)
	{
		for (int index = 0; index < this.armies.Count; index++)
		{
			foreach (Unit unit in this.armies[index].Units)
			{
				float experience = unit.GetPropertyValue(SimulationProperties.UnitExperienceGainPerTurn);
				unit.GainXp(experience, false, true);
			}
		}
		if (base.Empire is MajorEmpire)
		{
			MajorEmpire majorEmpire = base.Empire as MajorEmpire;
			for (int index2 = 0; index2 < majorEmpire.ConvertedVillages.Count; index2++)
			{
				foreach (Unit unit2 in majorEmpire.ConvertedVillages[index2].Units)
				{
					float experience2 = unit2.GetPropertyValue(SimulationProperties.UnitExperienceGainPerTurn);
					unit2.GainXp(experience2, false, true);
				}
			}
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_UnitHealthPerTurnGain(string context, string name)
	{
		float regenModifier = 0f;
		int pacifiedVillageCount = 0;
		for (int index = this.armies.Count - 1; index >= 0; index--)
		{
			Army army = this.armies[index];
			if ((!(army is KaijuArmy) || (army as KaijuArmy).Kaiju.OnArmyMode()) && this.ComputeRegenModifier(army, out regenModifier, out pacifiedVillageCount))
			{
				foreach (Unit unit in army.Units)
				{
					DepartmentOfDefense.RegenUnit(unit, regenModifier, pacifiedVillageCount);
				}
				army.Refresh(true);
				this.CleanGarrisonAfterEncounter(army);
			}
		}
		yield break;
	}

	private IEnumerator GameServerState_Turn_Begin_TeleportArmiesWhichHasForbiddenPosition(string context, string name)
	{
		Diagnostics.Assert(base.Empire != null);
		Diagnostics.Assert(this.armies != null);
		if (!(base.Empire is MajorEmpire))
		{
			yield break;
		}
		Diagnostics.Assert(this.departmentOfForeignAffairs != null);
		Diagnostics.Assert(this.alreadyUsedValidPosition != null);
		this.alreadyUsedValidPosition.Clear();
		for (int index = 0; index < this.armies.Count; index++)
		{
			Army army = this.armies[index];
			Diagnostics.Assert(army != null);
			if (!this.departmentOfForeignAffairs.CanMoveOn(army.WorldPosition, army.IsPrivateers, army.IsCamouflaged))
			{
				PathfindingFlags flags = ~(PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreDistrict);
				PathfindingContext pathfindingContext = army.GenerateContext();
				pathfindingContext.IsCamouflaged = false;
				PathfindingResult pathfindingResult = this.PathfindingService.FindLocation(pathfindingContext, army.WorldPosition, new PathfindingAStar.StopPredicate(this.FindNearestStopPredicate), null, flags);
				WorldPosition nearestValidPosition = (pathfindingResult == null) ? WorldPosition.Invalid : pathfindingResult.Goal;
				if (nearestValidPosition.IsValid)
				{
					this.alreadyUsedValidPosition.Add(nearestValidPosition);
					global::Empire empire = base.Empire as global::Empire;
					OrderTeleportArmy order = new OrderTeleportArmy(base.Empire.Index, army.GUID, nearestValidPosition);
					Diagnostics.Assert(empire != null && empire.PlayerControllers.Server != null);
					empire.PlayerControllers.Server.PostOrder(order);
				}
				else
				{
					Diagnostics.LogError("Can't found valid position to teleport the army {0}.", new object[]
					{
						army.GUID
					});
				}
				yield return null;
			}
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_Begin_ResetExperienceRewardOnUnits(string context, string name)
	{
		foreach (Army army in this.Armies)
		{
			foreach (Unit unit in army.Units)
			{
				unit.UpdateExperienceReward(base.Empire);
			}
		}
		DepartmentOfTheInterior departmentOfTheInterior = base.Empire.GetAgency<DepartmentOfTheInterior>();
		if (departmentOfTheInterior != null)
		{
			foreach (City city in departmentOfTheInterior.Cities)
			{
				foreach (Unit unit2 in city.Units)
				{
					unit2.UpdateExperienceReward(base.Empire);
				}
			}
		}
		DepartmentOfEducation departmentOfEducation = base.Empire.GetAgency<DepartmentOfEducation>();
		if (departmentOfEducation != null)
		{
			foreach (Unit unit3 in departmentOfEducation.Heroes)
			{
				unit3.UpdateExperienceReward(base.Empire);
			}
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_Begin_UnShiftUnits(string context, string name)
	{
		if (this.TurnWhenLastUnshiftedUnits == (this.GameService.Game as global::Game).Turn)
		{
			yield break;
		}
		this.TurnWhenLastUnshiftedUnits = (this.GameService.Game as global::Game).Turn;
		for (int armyIndex = 0; armyIndex < this.Armies.Count; armyIndex++)
		{
			foreach (Unit unit in this.Armies[armyIndex].Units)
			{
				if (unit.CheckUnitAbility(UnitAbility.ReadonlyShifterNature, -1))
				{
					if (SimulationGlobal.GlobalTagsContains("Winter"))
					{
						unit.SetPropertyBaseValue("ShiftingForm", 1f);
						unit.Refresh(false);
					}
					else
					{
						unit.SetPropertyBaseValue("ShiftingForm", 0f);
						unit.Refresh(false);
					}
				}
			}
			this.Armies[armyIndex].ShiftingFormHasChange(true);
		}
		DepartmentOfTheInterior departmentOfTheInterior = base.Empire.GetAgency<DepartmentOfTheInterior>();
		if (departmentOfTheInterior == null || departmentOfTheInterior.Cities == null)
		{
			yield break;
		}
		for (int index = 0; index < departmentOfTheInterior.Cities.Count; index++)
		{
			Garrison garrison = departmentOfTheInterior.Cities[index];
			foreach (Unit unit2 in garrison.Units)
			{
				if (unit2.CheckUnitAbility(UnitAbility.ReadonlyShifterNature, -1))
				{
					if (SimulationGlobal.GlobalTagsContains("Winter"))
					{
						unit2.SetPropertyBaseValue("ShiftingForm", 1f);
					}
					else
					{
						unit2.SetPropertyBaseValue("ShiftingForm", 0f);
					}
				}
			}
			if (garrison is Army)
			{
				(garrison as Army).ShiftingFormHasChange(true);
			}
		}
		DepartmentOfEducation departmentOfEducation = base.Empire.GetAgency<DepartmentOfEducation>();
		if (departmentOfEducation == null || departmentOfEducation.Heroes == null)
		{
			yield break;
		}
		for (int index2 = 0; index2 < departmentOfEducation.Heroes.Count; index2++)
		{
			if (departmentOfEducation.Heroes[index2].CheckUnitAbility(UnitAbility.ReadonlyShifterNature, -1))
			{
				if (SimulationGlobal.GlobalTagsContains("Winter"))
				{
					departmentOfEducation.Heroes[index2].SetPropertyBaseValue("ShiftingForm", 1f);
					departmentOfEducation.Heroes[index2].Refresh(false);
				}
				else
				{
					departmentOfEducation.Heroes[index2].SetPropertyBaseValue("ShiftingForm", 0f);
					departmentOfEducation.Heroes[index2].Refresh(false);
				}
			}
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_ResetArmyTowerEffectsThisTurn(string context, string name)
	{
		for (int armyIndex = 0; armyIndex < this.Armies.Count; armyIndex++)
		{
			this.Armies[armyIndex].HasBeenHitByTower = false;
			this.Armies[armyIndex].HasBeenHealedByTower = false;
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_Begin_RemoveImmolation(string context, string name)
	{
		if (!base.Empire.SimulationObject.Tags.Contains("FactionTraitBrokenLordsHeatWave"))
		{
			yield break;
		}
		if (this.TurnWhenLastImmolatedUnits == (this.GameService.Game as global::Game).Turn)
		{
			yield break;
		}
		this.TurnWhenLastImmolatedUnits = (this.GameService.Game as global::Game).Turn;
		for (int armyIndex = 0; armyIndex < this.Armies.Count; armyIndex++)
		{
			bool armyWasImmolated = false;
			foreach (Unit unit in this.Armies[armyIndex].Units)
			{
				if (unit.IsImmolableUnit())
				{
					unit.SetPropertyBaseValue("ImmolationState", 0f);
					unit.Refresh(false);
					armyWasImmolated = true;
				}
			}
			if (armyWasImmolated)
			{
				this.Armies[armyIndex].NotifyUnitsImmolated(false);
			}
		}
		DepartmentOfTheInterior departmentOfTheInterior = base.Empire.GetAgency<DepartmentOfTheInterior>();
		if (departmentOfTheInterior == null || departmentOfTheInterior.Cities == null)
		{
			yield break;
		}
		for (int index = 0; index < departmentOfTheInterior.Cities.Count; index++)
		{
			Garrison garrison = departmentOfTheInterior.Cities[index];
			foreach (Unit unit2 in garrison.Units)
			{
				if (unit2.IsImmolableUnit())
				{
					unit2.SetPropertyBaseValue("ImmolationState", 0f);
					unit2.Refresh(false);
				}
			}
		}
		DepartmentOfEducation departmentOfEducation = base.Empire.GetAgency<DepartmentOfEducation>();
		if (departmentOfEducation == null || departmentOfEducation.Heroes == null)
		{
			yield break;
		}
		for (int index2 = 0; index2 < departmentOfEducation.Heroes.Count; index2++)
		{
			if (departmentOfEducation.Heroes[index2].IsImmolableUnit())
			{
				departmentOfEducation.Heroes[index2].SetPropertyBaseValue("ImmolationState", 0f);
				departmentOfEducation.Heroes[index2].Refresh(false);
			}
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_ResetPortableForgeArmies(string context, string name)
	{
		for (int armyIndex = 0; armyIndex < this.Armies.Count; armyIndex++)
		{
			this.Armies[armyIndex].PortableForgeActive = false;
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_Begin_ResetPerTurnCounters(string context, string name)
	{
		for (int armyIndex = 0; armyIndex < this.Armies.Count; armyIndex++)
		{
			this.Armies[armyIndex].SetPropertyBaseValue(SimulationProperties.TilesMovedThisTurn, 0f);
			this.Armies[armyIndex].Refresh(false);
		}
		yield break;
	}

	private bool FindNearestStopPredicate(WorldPosition start, WorldPosition goal, PathfindingContext pathfindingContext, PathfindingWorldContext pathfindingWorldContext, WorldPosition evaluatedPosition, PathfindingFlags flags)
	{
		Diagnostics.Assert(this.alreadyUsedValidPosition != null);
		if (this.alreadyUsedValidPosition.Contains(evaluatedPosition))
		{
			return false;
		}
		if (!this.departmentOfForeignAffairs.CanMoveOn(evaluatedPosition, pathfindingContext.IsPrivateers, pathfindingContext.IsCamouflaged))
		{
			return false;
		}
		Diagnostics.Assert(this.PathfindingService != null);
		return this.PathfindingService.IsTilePassable(evaluatedPosition, pathfindingContext, PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnoreZoneOfControl, null) && this.PathfindingService.IsTileStopable(evaluatedPosition, pathfindingContext, PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnoreZoneOfControl, null);
	}

	private void InternalCleanGarrisonAfterEncounter(IGarrison garrison)
	{
		for (int i = garrison.StandardUnits.Count - 1; i >= 0; i--)
		{
			Unit unit = garrison.StandardUnits[i];
			if (unit != garrison.Hero)
			{
				float propertyValue = unit.GetPropertyValue(SimulationProperties.Health);
				if (propertyValue <= 0f)
				{
					if (unit.CheckUnitAbility(UnitAbility.ReadonlyIndestructible, -1))
					{
						unit.SetPropertyBaseValue(SimulationProperties.Health, 1f);
					}
					else
					{
						garrison.RemoveUnit(unit);
						if (unit.UnitDesign.CheckUnitAbility(UnitAbility.ReadonlyColonize, -1))
						{
							this.EventService.Notify(new EventSettlerDied(garrison.Empire));
						}
						this.GameEntityRepositoryService.Unregister(unit);
						unit.Dispose();
					}
				}
			}
		}
		if (garrison.Hero != null && garrison.InjureHeroOnClean)
		{
			float propertyValue2 = garrison.Hero.GetPropertyValue(SimulationProperties.Health);
			if (propertyValue2 <= 0f)
			{
				DepartmentOfEducation agency = base.Empire.GetAgency<DepartmentOfEducation>();
				if (agency != null)
				{
					agency.InjureHero(garrison.Hero, true);
				}
			}
		}
	}

	private void OnArmyActionStateChange(Army proxy)
	{
		if (this.ArmyActionStateChange != null)
		{
			this.ArmyActionStateChange(proxy, null);
		}
	}

	private void OnGarrisonActionStateChange(Garrison proxy)
	{
		if (this.GarrisonActionStateChange != null)
		{
			this.GarrisonActionStateChange(proxy, null);
		}
	}

	private void OnArmiesCollectionChange(CollectionChangeAction action, Army element)
	{
		if (this.ArmiesCollectionChange != null)
		{
			this.ArmiesCollectionChange(this, new CollectionChangeEventArgs(action, element));
		}
		if (this.EnableDetection)
		{
			if (action != CollectionChangeAction.Add)
			{
				if (action == CollectionChangeAction.Remove)
				{
					element.Refreshed -= this.UpdateDetection_OnRefreshed;
					element.WorldPositionChange -= this.UpdateDetection_OnWorldPositionChange;
				}
			}
			else
			{
				element.WorldPositionChange += this.UpdateDetection_OnWorldPositionChange;
				element.Refreshed += this.UpdateDetection_OnRefreshed;
			}
		}
		if (element.IsSolitary && element.Units.ElementAt(0).CheckUnitAbility(UnitAbility.ReadonlyHarbinger, -1))
		{
			if (action != CollectionChangeAction.Add)
			{
				if (action == CollectionChangeAction.Remove)
				{
					element.WorldPositionChange -= this.UpdateAspirate_OnWorldPositionChange;
					this.EventService.EventRaise -= element.UpdateAspirateDescriptors_OnColonizationEventRaise;
				}
			}
			else
			{
				element.WorldPositionChange += this.UpdateAspirate_OnWorldPositionChange;
				this.EventService.EventRaise += element.UpdateAspirateDescriptors_OnColonizationEventRaise;
			}
		}
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		if (service.IsShared(DownloadableContent13.ReadOnlyName))
		{
			if (action != CollectionChangeAction.Add)
			{
				if (action == CollectionChangeAction.Remove)
				{
					element.WorldPositionChange -= this.CollectOrbs_OnWorldPositionChange;
				}
			}
			else
			{
				element.WorldPositionChange += this.CollectOrbs_OnWorldPositionChange;
			}
		}
		if (service.IsShared(DownloadableContent19.ReadOnlyName))
		{
			if (action != CollectionChangeAction.Add)
			{
				if (action == CollectionChangeAction.Remove)
				{
					element.WorldPositionChange -= this.CheckArmyOnMapBoost;
				}
			}
			else
			{
				element.WorldPositionChange += this.CheckArmyOnMapBoost;
			}
		}
		if (action != CollectionChangeAction.Add)
		{
			if (action == CollectionChangeAction.Remove)
			{
				element.WorldPositionChange -= this.CheckDismantlingDevice_OnWorldPositionChange;
				element.WorldPositionChange -= this.CheckDismantlingCreepingNode_OnWorldPositionChange;
				element.WorldPositionChange -= this.CheckEarthquakerStatus_OnWorldPositionChange;
			}
		}
		else
		{
			element.WorldPositionChange += this.CheckDismantlingDevice_OnWorldPositionChange;
			element.WorldPositionChange += this.CheckDismantlingCreepingNode_OnWorldPositionChange;
			element.WorldPositionChange += this.CheckEarthquakerStatus_OnWorldPositionChange;
		}
		if (!base.HasBeenLoaded)
		{
			return;
		}
		if (action == CollectionChangeAction.Add)
		{
			DepartmentOfIndustry agency = base.Empire.GetAgency<DepartmentOfIndustry>();
			if (agency != null && agency.NotifyFirstColossus)
			{
				bool flag = element.StandardUnits.Count == 1 && element.StandardUnits[0].UnitDesign.Tags.Contains(DownloadableContent9.TagColossus);
				if (flag)
				{
					IGameService service2 = Services.GetService<IGameService>();
					if (service2 != null && service2.Game != null)
					{
						foreach (global::Empire empire in (service2.Game as global::Game).Empires)
						{
							DepartmentOfIndustry agency2 = empire.GetAgency<DepartmentOfIndustry>();
							if (agency2 != null)
							{
								agency2.NotifyFirstColossus = false;
							}
							if (empire is MajorEmpire)
							{
								DepartmentOfScience agency3 = empire.GetAgency<DepartmentOfScience>();
								if (agency3 != null)
								{
									agency3.UnlockTechnology("TechnologyDefinitionGuardianKillerPrerequisite", true);
								}
								this.EventService.Notify(new EventFirstColossusCreated(empire, element.StandardUnits[0].UnitDesign.Name));
							}
						}
					}
				}
			}
		}
	}

	public IEnumerable<IGarrison> GetGarrisons()
	{
		for (int index = 0; index < this.Armies.Count; index++)
		{
			if (this.Armies[index].Hero != null)
			{
				this.Armies[index].Hero.SetPropertyBaseValue(SimulationProperties.InfiltrationCooldown, float.MaxValue);
			}
			yield return this.Armies[index];
		}
		if (base.Empire is MajorEmpire)
		{
			DepartmentOfTheInterior interior = base.Empire.GetAgency<DepartmentOfTheInterior>();
			if (interior != null)
			{
				for (int index2 = 0; index2 < interior.Cities.Count; index2++)
				{
					if (interior.Cities[index2].Hero != null)
					{
						interior.Cities[index2].Hero.SetPropertyBaseValue(SimulationProperties.InfiltrationCooldown, float.MaxValue);
					}
					yield return interior.Cities[index2];
				}
			}
			DepartmentOfIntelligence intelligenceDepartment = base.Empire.GetAgency<DepartmentOfIntelligence>();
			if (intelligenceDepartment != null)
			{
				for (int index3 = 0; index3 < intelligenceDepartment.SpiedGarrisons.Count; index3++)
				{
					yield return intelligenceDepartment.SpiedGarrisons[index3];
				}
			}
		}
		else if (base.Empire is MinorEmpire)
		{
			BarbarianCouncil barbarianCouncil = base.Empire.GetAgency<BarbarianCouncil>();
			for (int index4 = 0; index4 < barbarianCouncil.Villages.Count; index4++)
			{
				yield return barbarianCouncil.Villages[index4];
			}
		}
		yield break;
	}

	public Unit GetUnitByGUID(GameEntityGUID guid)
	{
		IGarrison[] array = this.GetGarrisons().ToArray<IGarrison>();
		for (int i = 0; i < array.Length; i++)
		{
			if (!array[i].IsEmpty)
			{
				if (array[i].Hero != null && array[i].Hero.GUID == guid)
				{
					return array[i].Hero;
				}
				ReadOnlyCollection<Unit> standardUnits = array[i].StandardUnits;
				for (int j = 0; j < standardUnits.Count; j++)
				{
					if (standardUnits[j].GUID == guid)
					{
						return standardUnits[j];
					}
				}
			}
		}
		DepartmentOfEducation agency = base.Empire.GetAgency<DepartmentOfEducation>();
		if (agency == null || agency.Heroes == null)
		{
			return null;
		}
		for (int k = 0; k < agency.Heroes.Count; k++)
		{
			if (agency.Heroes[k].GUID == guid)
			{
				return agency.Heroes[k];
			}
		}
		return null;
	}

	public const int ArmyTitleVariations = 10;

	public const int ArmyNumberVariations = 10;

	public const int ArmyColossusTitleVariations = 3;

	public const int ArmyColossusNumberVariations = 3;

	private const PathfindingFlags FindNearestValidPositionPathfindingFlags = PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnoreZoneOfControl;

	private GridMap<byte> forests;

	private static IDatabase<SimulationDescriptor> staticSimulationDescriptorDatabase;

	private static IDatabase<ItemDefinition> staticItemDefinitionsDatabase;

	private SimulationObject temporaryGarrison;

	private ulong tempGuid = 1UL;

	private readonly List<UnitBodyDefinition> availableUnitBodyDefinitions = new List<UnitBodyDefinition>();

	private readonly List<UnitDesign> hiddenUnitDesigns = new List<UnitDesign>();

	private readonly List<UnitDesign> availableUnitDesigns = new List<UnitDesign>();

	private readonly List<UnitDesign> outdatedUnitDesigns = new List<UnitDesign>();

	private uint nextAvailableUnitDesignModelId;

	public static readonly StaticString TagMilitia = new StaticString("Militia");

	public static readonly StaticString TagUndead = new StaticString("Undead");

	public static float UnitSellPrice = 10f;

	private static readonly string ReadOnlyCatspawCostModifier = "CatspawCostModifier";

	private static object[] catsPawFormulaTokens;

	private static InterpreterContext catsPawInterpreterContext;

	public static StaticString PillageStatusDescriptor = "PointOfInterestStatusPillaged";

	private static IGameEntityRepositoryService gameEntityRepositoryServiceStatic;

	private static IWorldPositionningService worldPositionningServiceStatic;

	private static IPathfindingService pathfindingServiceStatic;

	public static readonly StaticString TagTemporary = new StaticString("Temporary");

	public static char[] ItemSeparators = new char[]
	{
		'#',
		'?',
		'^'
	};

	private List<Army> armies = new List<Army>();

	private ReadOnlyCollection<Army> readOnlyArmies;

	private Dictionary<IGameEntity, List<Unit>> producedUnitsPerContext = new Dictionary<IGameEntity, List<Unit>>();

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private IDatabase<UnitBodyDefinition> unitBodyDefinitionDatabase;

	private IDatabase<UnitDesign> unitDesignDatabase;

	private GameEntityGUID temporaryGuid;

	private List<WorldPosition> alreadyUsedValidPosition = new List<WorldPosition>();

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private DepartmentOfScience departmentOfScience;

	private IVisibilityService visibilityService;

	private IWeatherService weatherService;

	private IMapBoostService mapBoostService;

	private List<DelayedSolitaryUnitSpawnCommand> delayedSolitaryUnitSpawnCommands;

	private Dictionary<GameEntityGUID, City> pendingCitiesToMigrate = new Dictionary<GameEntityGUID, City>();

	[Flags]
	public enum CheckRetrofitPrerequisitesResult
	{
		Ok = 0,
		GarrisonArmyIsLocked = 1,
		GarrisonArmyIsInEncounter = 2,
		GarrisonCityIsUnderSiege = 3,
		RegionDoesntBelongToUs = 4,
		WorldPositionIsNotValid = 5
	}

	internal enum UnitDesignState
	{
		Available,
		Outdated,
		Hidden
	}

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct UnitDescriptor
	{
		public GameEntityGUID GameEntityGUID { get; set; }

		public uint UnitDesignModel { get; set; }

		public StaticString UnitDesignName { get; set; }
	}

	public delegate void AvailableUnitBodyChangeEventHandler(object sender, ConstructibleElementEventArgs e);

	public delegate void AvailableUnitDesignChangeEventHandler(object sender, ConstructibleElementEventArgs e);

	[CompilerGenerated]
	private sealed class TryGetValue>c__AnonStorey938
	{
		internal bool <>m__3FA(UnitDesign constructibleElement)
		{
			return constructibleElement.FullName == this.unitDesignName;
		}

		internal bool <>m__3FB(UnitDesign constructibleElement)
		{
			return constructibleElement.FullName == this.unitDesignName;
		}

		internal StaticString unitDesignName;
	}

	[CompilerGenerated]
	private sealed class TryGetValue>c__AnonStorey939
	{
		internal bool <>m__3FC(UnitDesign constructibleElement)
		{
			return constructibleElement.Model == this.model && constructibleElement.ModelRevision == this.modelRevision;
		}

		internal bool <>m__3FD(UnitDesign constructibleElement)
		{
			return constructibleElement.Model == this.model && constructibleElement.ModelRevision == this.modelRevision;
		}

		internal uint model;

		internal uint modelRevision;
	}

	[CompilerGenerated]
	private sealed class CheckWhetherUnitDesignIsValid>c__AnonStorey93A
	{
		internal bool <>m__3FE(UnitDesign match)
		{
			return match.Model == this.unitDesign.Model && match.ModelRevision >= this.unitDesign.ModelRevision;
		}

		internal UnitDesign unitDesign;
	}
}
