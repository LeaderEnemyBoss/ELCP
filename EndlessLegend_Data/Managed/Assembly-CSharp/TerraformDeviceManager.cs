using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation;
using Amplitude.Utilities.Maps;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class TerraformDeviceManager : GameAncillary, IXmlSerializable, IService, IEnumerable, ITerraformDeviceRepositoryService, IRepositoryService<TerraformDevice>, IEnumerable<TerraformDevice>, IEnumerable<KeyValuePair<ulong, TerraformDevice>>, ITerraformDeviceService
{
	public event EventHandler<TerraformDeviceRepositoryChangeEventArgs> TerraformDeviceRepositoryChange;

	IEnumerable<TerraformDevice> ITerraformDeviceRepositoryService.AsEnumerable(int empireIndex)
	{
		foreach (TerraformDevice device in this.terraformDevices.Values)
		{
			if (device.Empire.Index == empireIndex)
			{
				yield return device;
			}
		}
		yield break;
	}

	IEnumerator<TerraformDevice> IEnumerable<TerraformDevice>.GetEnumerator()
	{
		foreach (TerraformDevice terraformDevice in this.terraformDevices.Values)
		{
			yield return terraformDevice;
		}
		yield break;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.terraformDevices.GetEnumerator();
	}

	public int Count
	{
		get
		{
			return this.terraformDevices.Count;
		}
	}

	public IGameEntity this[GameEntityGUID guid]
	{
		get
		{
			return this.terraformDevices[guid];
		}
	}

	public bool Contains(GameEntityGUID guid)
	{
		return this.terraformDevices.ContainsKey(guid);
	}

	public IEnumerator<KeyValuePair<ulong, TerraformDevice>> GetEnumerator()
	{
		return this.terraformDevices.GetEnumerator();
	}

	public void Register(TerraformDevice device)
	{
		if (device == null)
		{
			throw new ArgumentNullException("device");
		}
		this.terraformDevices.Add(device.GUID, device);
		this.OnTerraformDeviceRepositoryChange(TerraformDeviceRepositoryChangeAction.Add, device.GUID);
		this.gameEntityRepositoryService.Register(device);
	}

	public bool TryGetValue(GameEntityGUID guid, out TerraformDevice gameEntity)
	{
		return this.terraformDevices.TryGetValue(guid, out gameEntity);
	}

	public void Unregister(TerraformDevice device)
	{
		if (device == null)
		{
			throw new ArgumentNullException("device");
		}
		this.Unregister(device.GUID);
	}

	public void Unregister(IGameEntity gameEntity)
	{
		if (gameEntity == null)
		{
			throw new ArgumentNullException("gameEntity");
		}
		this.Unregister(gameEntity.GUID);
	}

	public void Unregister(GameEntityGUID guid)
	{
		if (!this.terraformDevices.ContainsKey(guid))
		{
			Diagnostics.LogError("Could not find a terraform device with GUID#{0}.", new object[]
			{
				guid
			});
			return;
		}
		TerraformDevice terraformDevice = this.terraformDevices[guid];
		global::Empire empire = terraformDevice.Empire;
		empire.RemoveChild(terraformDevice);
		empire.Refresh(false);
		if (this.terraformDevices.Remove(guid))
		{
			this.OnTerraformDeviceRepositoryChange(TerraformDeviceRepositoryChangeAction.Remove, guid);
		}
		this.gameEntityRepositoryService.Unregister(guid);
	}

	private void OnTerraformDeviceRepositoryChange(TerraformDeviceRepositoryChangeAction action, ulong gameEntityGuid)
	{
		if (this.TerraformDeviceRepositoryChange != null)
		{
			this.TerraformDeviceRepositoryChange(this, new TerraformDeviceRepositoryChangeEventArgs(action, gameEntityGuid));
		}
	}

	public virtual void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		this.turnWhenLastBegun = reader.GetAttribute<int>("TurnWhenLastBegun");
		reader.ReadStartElement();
		int attribute = reader.GetAttribute<int>("TerraformStateCount");
		byte[] array = new byte[attribute];
		reader.ReadStartElement("TerraformStateMap");
		for (int i = 0; i < attribute; i++)
		{
			array[i] = reader.ReadElementString<byte>("TerraformStateValue");
		}
		reader.ReadEndElement("TerraformStateMap");
		if (attribute > 0)
		{
			(base.Game.World.Atlas.GetMap(WorldAtlas.Maps.TerraformState) as GridMap<byte>).Data = array;
		}
		int attribute2 = reader.GetAttribute<int>("DevicesCount");
		reader.ReadStartElement("TerraformDevices");
		IDatabase<TerraformDeviceDefinition> database = Databases.GetDatabase<TerraformDeviceDefinition>(false);
		for (int j = 0; j < attribute2; j++)
		{
			int attribute3 = reader.GetAttribute<int>("EmpireIndex");
			GameEntityGUID guid = reader.GetAttribute<ulong>("GUID");
			WorldPosition position;
			position.Row = reader.GetAttribute<short>("Row");
			position.Column = reader.GetAttribute<short>("Column");
			string attribute4 = reader.GetAttribute("TerraformDeviceDefinitionName");
			float attribute5 = reader.GetAttribute<float>("Charges");
			float attribute6 = reader.GetAttribute<float>("ChargesPerTurn");
			float attribute7 = reader.GetAttribute<float>("ChargesToActivate");
			int attribute8 = reader.GetAttribute<int>("Range");
			int attribute9 = reader.GetAttribute<int>("Activations");
			float attribute10 = reader.GetAttribute<float>("DismantleDefense");
			GameEntityGUID dismantlingArmyGUID = reader.GetAttribute<ulong>("DismantlingArmyGUID");
			int attribute11 = reader.GetAttribute<int>("LastTurnCharged");
			int attribute12 = reader.GetAttribute<int>("LastTurnWhenDismantleBegun");
			int attribute13 = reader.GetAttribute<int>("LastTurnDismantled");
			float attribute14 = reader.GetAttribute<float>("ChargesWhenDismantleStarted");
			bool placedByPrivateers = false;
			if (num >= 2)
			{
				placedByPrivateers = reader.GetAttribute<bool>("PlacedByPrivateers");
			}
			TerraformDeviceDefinition terraformDeviceDefinition;
			if (database.TryGetValue(attribute4, out terraformDeviceDefinition))
			{
				global::Empire empire = base.Game.Empires[attribute3];
				for (int k = 0; k < base.Game.Empires.Length; k++)
				{
					empire = base.Game.Empires[k];
					if (empire.Index == attribute3)
					{
						TerraformDevice terraformDevice = this.AddDevice(guid, terraformDeviceDefinition, position, empire, placedByPrivateers);
						terraformDevice.ChargesToActivate = attribute7;
						terraformDevice.ChargesPerTurn = attribute6;
						terraformDevice.Charges = attribute5;
						terraformDevice.Range = attribute8;
						terraformDevice.Activations = attribute9;
						terraformDevice.DismantleDefense = attribute10;
						terraformDevice.DismantlingArmyGUID = dismantlingArmyGUID;
						terraformDevice.LastTurnCharged = attribute11;
						terraformDevice.LastTurnWhenDismantleBegun = attribute12;
						terraformDevice.LastTurnDismantled = attribute13;
						terraformDevice.ChargesWhenDismantleStarted = attribute14;
						break;
					}
				}
			}
			reader.ReadStartElement("TerraformDevice");
			reader.ReadEndElement("TerraformDevice");
		}
		reader.ReadEndElement("TerraformDevices");
		if (num > 2 && reader.IsStartElement("TemporaryTerraformations"))
		{
			int attribute15 = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("TemporaryTerraformations");
			base.Game.World.TemporaryTerraformations.Clear();
			for (int l = 0; l < attribute15; l++)
			{
				World.TemporaryTerraformation temporaryTerraformation = new World.TemporaryTerraformation(WorldPosition.Invalid, StaticString.Empty, 1);
				reader.ReadElementSerializable<World.TemporaryTerraformation>(ref temporaryTerraformation);
				if (temporaryTerraformation.worldPosition.IsValid)
				{
					if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
					{
						Diagnostics.Log("ELCP: Loading TemporaryTerraformation {0}", new object[]
						{
							temporaryTerraformation.ToString()
						});
					}
					base.Game.World.TemporaryTerraformations.Add(temporaryTerraformation);
				}
			}
			global::Game.ELCPRevertTempTerraformationsToOriginal(base.Game.World, true);
			reader.ReadEndElement("TemporaryTerraformations");
		}
	}

	public virtual void WriteXml(XmlWriter writer)
	{
		writer.WriteVersionAttribute(3);
		writer.WriteAttributeString("AssemblyQualifiedName", base.GetType().AssemblyQualifiedName);
		writer.WriteAttributeString<int>("TurnWhenLastBegun", this.turnWhenLastBegun);
		writer.WriteStartElement("TerraformStateMap");
		GridMap<byte> gridMap = base.Game.World.Atlas.GetMap(WorldAtlas.Maps.TerraformState) as GridMap<byte>;
		writer.WriteAttributeString<int>("TerraformStateCount", gridMap.Data.Length);
		for (int i = 0; i < gridMap.Data.Length; i++)
		{
			writer.WriteElementString<byte>("TerraformStateValue", gridMap.Data[i]);
		}
		writer.WriteEndElement();
		writer.WriteStartElement("TerraformDevices");
		writer.WriteAttributeString<int>("DevicesCount", this.terraformDevices.Values.Count);
		foreach (TerraformDevice terraformDevice in this.terraformDevices.Values)
		{
			writer.WriteStartElement("TerraformDevice");
			writer.WriteAttributeString<int>("EmpireIndex", terraformDevice.Empire.Index);
			writer.WriteAttributeString<ulong>("GUID", terraformDevice.GUID);
			writer.WriteAttributeString<short>("Row", terraformDevice.WorldPosition.Row);
			writer.WriteAttributeString<short>("Column", terraformDevice.WorldPosition.Column);
			writer.WriteAttributeString<float>("Charges", terraformDevice.Charges);
			writer.WriteAttributeString<float>("ChargesPerTurn", terraformDevice.ChargesPerTurn);
			writer.WriteAttributeString<float>("ChargesToActivate", terraformDevice.ChargesToActivate);
			writer.WriteAttributeString<int>("Range", terraformDevice.Range);
			writer.WriteAttributeString<int>("Activations", terraformDevice.Activations);
			writer.WriteAttributeString<float>("DismantleDefense", terraformDevice.DismantleDefense);
			writer.WriteAttributeString<ulong>("DismantlingArmyGUID", terraformDevice.DismantlingArmyGUID);
			writer.WriteAttributeString<int>("LastTurnCharged", terraformDevice.LastTurnCharged);
			writer.WriteAttributeString<int>("LastTurnWhenDismantleBegun", terraformDevice.LastTurnWhenDismantleBegun);
			writer.WriteAttributeString<string>("TerraformDeviceDefinitionName", terraformDevice.TerraformDeviceDefinition.XmlSerializableName);
			writer.WriteAttributeString<int>("LastTurnDismantled", terraformDevice.LastTurnDismantled);
			writer.WriteAttributeString<float>("ChargesWhenDismantleStarted", terraformDevice.ChargesWhenDismantleStarted);
			writer.WriteAttributeString<bool>("PlacedByPrivateers", terraformDevice.PlacedByPrivateers);
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
		writer.WriteStartElement("TemporaryTerraformations");
		writer.WriteAttributeString<int>("Count", base.Game.World.TemporaryTerraformations.Count);
		for (int j = 0; j < base.Game.World.TemporaryTerraformations.Count; j++)
		{
			IXmlSerializable xmlSerializable = base.Game.World.TemporaryTerraformations[j];
			writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
		}
		writer.WriteEndElement();
	}

	public IWorldPositionningService WorldPositionningService { get; private set; }

	private global::PlayerController ServerPlayerController
	{
		get
		{
			IPlayerControllerRepositoryService service = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
			return ((IPlayerControllerRepositoryControl)service).GetPlayerControllerById("server");
		}
	}

	public override IEnumerator BindServices(IServiceContainer serviceContainer)
	{
		yield return base.BindServices(serviceContainer);
		yield return base.BindService<IVisibilityService>(serviceContainer, delegate(IVisibilityService service)
		{
			this.visibilityService = service;
		});
		yield return base.BindService<IGameEntityRepositoryService>(serviceContainer, delegate(IGameEntityRepositoryService service)
		{
			this.gameEntityRepositoryService = service;
		});
		yield return base.BindService<IWorldPositionSimulationEvaluatorService>(serviceContainer, delegate(IWorldPositionSimulationEvaluatorService service)
		{
			this.worldPositionSimulationEvaluatorService = service;
		});
		yield return base.BindService<IWorldPositionningService>(serviceContainer, delegate(IWorldPositionningService service)
		{
			this.WorldPositionningService = service;
		});
		serviceContainer.AddService<ITerraformDeviceService>(this);
		serviceContainer.AddService<ITerraformDeviceRepositoryService>(this);
		this.eventService = Services.GetService<IEventService>();
		yield break;
	}

	public override IEnumerator LoadGame(global::Game game)
	{
		yield return base.LoadGame(game);
		this.terraformDeviceDatabase = Databases.GetDatabase<TerraformDeviceDefinition>(false);
		Diagnostics.Assert(this.terraformDeviceDatabase != null);
		this.world = base.Game.World;
		Diagnostics.Assert(this.world != null);
		this.pillarService = base.Game.Services.GetService<IPillarService>();
		Diagnostics.Assert(this.pillarService != null);
		yield break;
	}

	public void GameClient_Turn_Begin()
	{
		if (base.Game.Turn <= this.turnWhenLastBegun)
		{
			return;
		}
		this.turnWhenLastBegun = base.Game.Turn;
		this.CheckDevicesDismantling();
		foreach (TerraformDevice terraformDevice in this.terraformDevices.Values)
		{
			terraformDevice.OnBeginTurn();
		}
		this.RunTerraformation();
		this.UnregisterExpiredDevices();
		this.ManageGeomancy();
		this.ManageTemporaryTerraformations();
	}

	public TerraformDevice AddDevice(GameEntityGUID guid, TerraformDeviceDefinition terraformDeviceDefinition, WorldPosition position, global::Empire empire, bool placedByPrivateers)
	{
		TerraformDevice terraformDevice = new TerraformDevice(empire.Index, guid, position, terraformDeviceDefinition, placedByPrivateers);
		IDatabase<SimulationDescriptor> database = Databases.GetDatabase<SimulationDescriptor>(false);
		Diagnostics.Assert(database != null);
		SimulationDescriptor descriptor = null;
		if (database.TryGetValue(TerraformDeviceManager.ClassTerraformDeviceDescriptor, out descriptor))
		{
			terraformDevice.AddDescriptor(descriptor, false);
		}
		float propertyValue = terraformDevice.Empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier);
		terraformDevice.Charges = 0f;
		terraformDevice.ChargesPerTurn = terraformDeviceDefinition.ChargesPerTurn;
		terraformDevice.ChargesToActivate = terraformDeviceDefinition.ChargesToActivate * propertyValue;
		terraformDevice.Range = terraformDeviceDefinition.TerraformRange;
		terraformDevice.DismantleDefense = (float)terraformDeviceDefinition.DismantleDefense * propertyValue;
		empire.AddChild(terraformDevice);
		empire.Refresh(false);
		this.Register(terraformDevice);
		this.worldPositionSimulationEvaluatorService.SetSomethingChangedOnRegion(this.WorldPositionningService.GetRegionIndex(terraformDevice.WorldPosition));
		return terraformDevice;
	}

	public bool CanExecuteDeviceDismantling(Army army, TerraformDevice device)
	{
		IPathfindingService service = base.Game.Services.GetService<IPathfindingService>();
		Diagnostics.Assert(service != null);
		int distance = this.WorldPositionningService.GetDistance(army.WorldPosition, device.WorldPosition);
		if (distance > 1)
		{
			return false;
		}
		PathfindingFlags flags = PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreKaijuGarrisons;
		return service.IsTransitionPassable(army.WorldPosition, device.WorldPosition, army, flags, null);
	}

	public void DestroyDevice(TerraformDevice device)
	{
		Army dismantlingArmy = device.DismantlingArmy;
		if (dismantlingArmy != null)
		{
			dismantlingArmy.Empire.GetAgency<DepartmentOfDefense>().StopDismantelingDevice(dismantlingArmy, device);
		}
		this.Unregister(device.GUID);
	}

	public void DestroyDevice(TerraformDevice device, Army army)
	{
		if (device != null && device.EmpireIndex != army.Empire.Index)
		{
			this.DestroyDevice(device);
		}
	}

	public void DestroyDevice(WorldPosition position, Army army)
	{
		TerraformDevice deviceAtPosition = this.GetDeviceAtPosition(position);
		if (deviceAtPosition != null)
		{
			this.DestroyDevice(deviceAtPosition, army);
		}
	}

	public ICollection<WorldPosition> GetDevicesPositions()
	{
		List<WorldPosition> list = new List<WorldPosition>();
		foreach (TerraformDevice terraformDevice in this.terraformDevices.Values)
		{
			list.Add(terraformDevice.WorldPosition);
		}
		return list;
	}

	public TerraformDevice GetDeviceAtPosition(WorldPosition position)
	{
		foreach (TerraformDevice terraformDevice in this.terraformDevices.Values)
		{
			if (terraformDevice.WorldPosition == position)
			{
				return terraformDevice;
			}
		}
		return null;
	}

	public int GetRangeForDeviceDefinition(StaticString deviceDefinitionName)
	{
		TerraformDeviceDefinition terraformDeviceDefinition;
		if (this.terraformDeviceDatabase != null && this.terraformDeviceDatabase.TryGetValue(deviceDefinitionName, out terraformDeviceDefinition))
		{
			return terraformDeviceDefinition.TerraformRange;
		}
		return 0;
	}

	public bool HasDeviceInPosition(WorldPosition position)
	{
		return this.GetDeviceAtPosition(position) != null;
	}

	public bool IsPositionValidForDevice(Amplitude.Unity.Game.Empire empire, WorldPosition position)
	{
		if (!position.IsValid)
		{
			return false;
		}
		if (this.WorldPositionningService.IsWaterTile(position))
		{
			return false;
		}
		if (this.GetDeviceAtPosition(position) != null)
		{
			return false;
		}
		if (this.IsAreaVolcanoformed(position))
		{
			return false;
		}
		if (this.WorldPositionningService != null)
		{
			if (!this.WorldPositionningService.IsConstructible(position, WorldPositionning.PreventsDistrictTypeExtensionConstruction, 0) && !this.pillarService.IsPositionOccupiedByAPillar(position))
			{
				return false;
			}
			Region region = this.WorldPositionningService.GetRegion(position);
			if (region.KaijuEmpire != null && region.Kaiju != null)
			{
				KaijuGarrison kaijuGarrison = region.Kaiju.KaijuGarrison;
				if (kaijuGarrison.WorldPosition == position)
				{
					return false;
				}
			}
			if (region != null && region.City != null && region.City.Empire.Index != empire.Index)
			{
				for (int i = 0; i < region.City.Districts.Count; i++)
				{
					if (region.City.Districts[i].Type != DistrictType.Exploitation && region.City.Districts[i].WorldPosition == position)
					{
						return false;
					}
				}
				DepartmentOfIndustry agency = region.City.Empire.GetAgency<DepartmentOfIndustry>();
				ConstructionQueue constructionQueue = agency.GetConstructionQueue(region.City);
				if (constructionQueue != null)
				{
					for (int j = 0; j < constructionQueue.PendingConstructions.Count; j++)
					{
						if (constructionQueue.PendingConstructions[j].WorldPosition == position)
						{
							if (constructionQueue.PendingConstructions[j].ConstructibleElement is DistrictImprovementDefinition)
							{
								return false;
							}
							if (constructionQueue.PendingConstructions[j].ConstructibleElement is UnitDesign && (constructionQueue.PendingConstructions[j].ConstructibleElement as UnitDesign).Tags.Contains(DownloadableContent9.TagSolitary))
							{
								return false;
							}
						}
					}
				}
			}
		}
		return true;
	}

	private bool IsAreaVolcanoformed(WorldPosition areaCenter)
	{
		World world = base.Game.World;
		WorldCircle worldCircle = new WorldCircle(areaCenter, 1);
		WorldPosition[] worldPositions = worldCircle.GetWorldPositions(this.WorldPositionningService.World.WorldParameters);
		List<WorldPosition> list = new List<WorldPosition>(worldPositions);
		for (int i = list.Count - 1; i >= 0; i--)
		{
			TerrainTypeMapping terrainTypeMapping = null;
			if (world.TryGetTerraformMapping(worldPositions[i], out terrainTypeMapping))
			{
				return false;
			}
		}
		return true;
	}

	public bool IsPositionNextToDevice(WorldPosition position)
	{
		WorldCircle worldCircle = new WorldCircle(position, 1);
		WorldPosition[] worldPositions = worldCircle.GetWorldPositions(this.WorldPositionningService.World.WorldParameters);
		List<WorldPosition> list = new List<WorldPosition>(worldPositions);
		for (int i = list.Count - 1; i >= 0; i--)
		{
			if (this.GetDeviceAtPosition(list[i]) != null)
			{
				return true;
			}
		}
		return false;
	}

	private void CheckDevicesDismantling()
	{
		List<TerraformDevice> list = new List<TerraformDevice>();
		foreach (TerraformDevice terraformDevice in this.terraformDevices.Values)
		{
			if (terraformDevice.DismantlingArmy != null && terraformDevice.LastTurnWhenDismantleBegun < base.Game.Turn)
			{
				if (terraformDevice.Charges <= 0f)
				{
					float propertyValue = terraformDevice.DismantlingArmy.GetPropertyValue(SimulationProperties.DeviceDismantlePower);
					terraformDevice.DismantleDefense = Math.Max(0f, terraformDevice.DismantleDefense - propertyValue);
					if (terraformDevice.DismantleDefense <= 0f)
					{
						list.Add(terraformDevice);
					}
				}
			}
			else
			{
				terraformDevice.DismantleDefense = (float)terraformDevice.TerraformDeviceDefinition.DismantleDefense;
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			TerraformDevice terraformDevice2 = list[i];
			if (this.ServerPlayerController != null)
			{
				OrderDismantleDeviceSucceed order = new OrderDismantleDeviceSucceed(terraformDevice2.DismantlingArmy.Empire.Index, terraformDevice2.DismantlingArmyGUID, terraformDevice2.GUID, ArmyAction_ToggleDismantleDevice.ReadOnlyName);
				this.ServerPlayerController.PostOrder(order);
			}
		}
	}

	private void RunTerraformation()
	{
		foreach (TerraformDevice terraformDevice in this.terraformDevices.Values)
		{
			if (terraformDevice == null)
			{
				Diagnostics.LogError("TerraformDevice is 'null'.");
			}
			else if (terraformDevice.IsReadyToTerraform && terraformDevice.Activations == 0 && this.ServerPlayerController != null)
			{
				OrderRunTerraformationForDevice order = new OrderRunTerraformationForDevice(terraformDevice.GUID);
				this.ServerPlayerController.PostOrder(order);
			}
		}
	}

	private void UnregisterExpiredDevices()
	{
		List<TerraformDevice> list = new List<TerraformDevice>();
		List<short> list2 = new List<short>();
		foreach (TerraformDevice terraformDevice in this.terraformDevices.Values)
		{
			if (terraformDevice == null)
			{
				Diagnostics.LogError("TerraformDevice is 'null'.");
			}
			else if (!terraformDevice.IsReadyToTerraform || terraformDevice.Activations != 0)
			{
				if (terraformDevice.TurnsToActivate() <= 0)
				{
					if (!list.Contains(terraformDevice))
					{
						list.Add(terraformDevice);
					}
					short regionIndex = this.WorldPositionningService.GetRegionIndex(terraformDevice.WorldPosition);
					if (!list2.Contains(regionIndex))
					{
						list2.Add(regionIndex);
					}
				}
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			this.Unregister(list[i].GUID);
		}
		for (int j = 0; j < list2.Count; j++)
		{
			this.worldPositionSimulationEvaluatorService.SetSomethingChangedOnRegion(list2[j]);
		}
	}

	private void ManageTemporaryTerraformations()
	{
		List<WorldPosition> list = new List<WorldPosition>();
		foreach (World.TemporaryTerraformation temporaryTerraformation in base.Game.World.TemporaryTerraformations)
		{
			temporaryTerraformation.turnsRemaing--;
			if (temporaryTerraformation.turnsRemaing == 0)
			{
				list.Add(temporaryTerraformation.worldPosition);
			}
		}
		if (list.Count > 0)
		{
			WorldPosition[] array = base.Game.World.PerformReversibleTerraformation(list.ToArray(), true, 0);
			if (array.Length != 0)
			{
				base.Game.World.UpdateTerraformStateMap(true);
				global::Empire terraformingEmpire = Services.GetService<IGameService>().Game.Services.GetService<IPlayerControllerRepositoryService>().ActivePlayerController.Empire as global::Empire;
				this.eventService.Notify(new EventEmpireWorldTerraformed(terraformingEmpire, array, true));
			}
		}
	}

	private void ManageGeomancy()
	{
		if (ELCPUtilities.GeomancyDuration < 1 || ELCPUtilities.GeomancyRadius < 0)
		{
			return;
		}
		Stopwatch stopwatch = new Stopwatch();
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			stopwatch.Start();
		}
		foreach (global::Empire empire in base.Game.Empires)
		{
			List<IGarrison> list = new List<IGarrison>();
			DepartmentOfTheInterior agency = empire.GetAgency<DepartmentOfTheInterior>();
			if (agency != null)
			{
				list.AddRange(agency.Cities.Cast<IGarrison>());
				list.AddRange(agency.Camps.Cast<IGarrison>());
			}
			DepartmentOfDefense agency2 = empire.GetAgency<DepartmentOfDefense>();
			if (agency2 != null)
			{
				list.AddRange(agency2.Armies.Cast<IGarrison>());
			}
			foreach (IGarrison garrison in list)
			{
				using (IEnumerator<Unit> enumerator2 = garrison.Units.GetEnumerator())
				{
					while (enumerator2.MoveNext())
					{
						if (enumerator2.Current.CheckUnitAbility(UnitAbility.UnitAbilityGeomancy, -1))
						{
							WorldCircle worldCircle = new WorldCircle((garrison as IWorldPositionable).WorldPosition, ELCPUtilities.GeomancyRadius);
							WorldPosition[] array = base.Game.World.PerformReversibleTerraformation(worldCircle.GetWorldPositions(base.Game.World.WorldParameters), false, ELCPUtilities.GeomancyDuration + 1);
							if (array.Length != 0)
							{
								base.Game.World.UpdateTerraformStateMap(true);
								this.eventService.Notify(new EventEmpireWorldTerraformed(empire, array, true));
							}
						}
					}
				}
			}
		}
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			stopwatch.Stop();
			Diagnostics.Log("ELCP ManageGeomancy time elapsed: {0}", new object[]
			{
				stopwatch.Elapsed
			});
		}
	}

	public static readonly StaticString ClassTerraformDeviceDescriptor = new StaticString("ClassTerraformDevice");

	private IEventService eventService;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private Dictionary<ulong, TerraformDevice> terraformDevices = new Dictionary<ulong, TerraformDevice>();

	private IDatabase<TerraformDeviceDefinition> terraformDeviceDatabase;

	private int turnWhenLastBegun;

	private IVisibilityService visibilityService;

	private IPillarService pillarService;

	private IWorldPositionSimulationEvaluatorService worldPositionSimulationEvaluatorService;

	private World world;
}
