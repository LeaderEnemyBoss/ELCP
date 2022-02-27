using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Session;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

[Diagnostics.TagAttribute("Diplomacy")]
public class DiplomacyManager : GameAncillary, IXmlSerializable, IService, IEnumerable, IEnumerable<KeyValuePair<ulong, DiplomaticContract>>, IEnumerable<DiplomaticContract>, IDiplomacyControl, IDiplomacyService, IDiplomaticContractRepositoryControl, IDiplomaticContractRepositoryService, IRepositoryService<DiplomaticContract>
{
	public event EventHandler<DiplomaticContractRepositoryChangeEventArgs> DiplomaticContractRepositoryChange;

	IEnumerator<DiplomaticContract> IEnumerable<DiplomaticContract>.GetEnumerator()
	{
		foreach (DiplomaticContract diplomaticContract in this.diplomaticContractsByGUID.Values)
		{
			yield return diplomaticContract;
		}
		yield break;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.diplomaticContractsByGUID.GetEnumerator();
	}

	void IDiplomacyControl.OnServerBeginTurn()
	{
		this.firstBeginTurnDone = true;
		if (this.majorEmpires == null)
		{
			return;
		}
		while (this.visibilityControllerRefreshedArgs.Count > 0)
		{
			KeyValuePair<object, VisibilityRefreshedEventArgs> keyValuePair = this.visibilityControllerRefreshedArgs.Dequeue();
			this.OnVisibityRefreshed(keyValuePair.Key, keyValuePair.Value);
		}
		int turn = base.Game.Turn;
		for (int i = 0; i < this.majorEmpires.Length; i++)
		{
			global::Empire empire = this.majorEmpires[i];
			DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
			Diagnostics.Assert(agency != null);
			for (int j = i + 1; j < this.majorEmpires.Length; j++)
			{
				global::Empire empire2 = this.majorEmpires[j];
				DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(empire2);
				Diagnostics.Assert(diplomaticRelation != null && diplomaticRelation.State != null);
				DiplomaticRelationState.Transition automaticTransition = diplomaticRelation.State.AutomaticTransition;
				if (automaticTransition != null && automaticTransition.GetRemainingTurns(diplomaticRelation) <= 0)
				{
					OrderChangeDiplomaticRelationState order = new OrderChangeDiplomaticRelationState(i, j, automaticTransition.DestinationState);
					this.PlayerController.PostOrder(order);
				}
			}
		}
		bool[][] array = new bool[this.majorEmpires.Length][];
		for (int k = 0; k < this.majorEmpires.Length; k++)
		{
			array[k] = new bool[this.majorEmpires.Length];
		}
		Diagnostics.Assert(this.diplomaticContracts != null);
		for (int l = 0; l < this.diplomaticContracts.Count; l++)
		{
			DiplomaticContract diplomaticContract = this.diplomaticContracts[l];
			Diagnostics.Assert(diplomaticContract != null);
			if (diplomaticContract.State == DiplomaticContractState.Proposed && turn > diplomaticContract.TurnAtTheBeginningOfTheState)
			{
				OrderChangeDiplomaticContractState order2 = new OrderChangeDiplomaticContractState(diplomaticContract, DiplomaticContractState.Refused);
				this.PlayerController.PostOrder(order2);
			}
			if (diplomaticContract.State == DiplomaticContractState.Negotiation && turn > diplomaticContract.TurnAtTheBeginningOfTheState)
			{
				if (diplomaticContract.EmpireWhichInitiated != diplomaticContract.EmpireWhichProposes || diplomaticContract.ContractRevisionNumber > 0 || array[diplomaticContract.EmpireWhichInitiated.Index][diplomaticContract.EmpireWhichReceives.Index])
				{
					DiplomaticContractState newState = DiplomaticContractState.Refused;
					OrderChangeDiplomaticContractState order3 = new OrderChangeDiplomaticContractState(diplomaticContract, newState);
					this.PlayerController.PostOrder(order3);
				}
				else
				{
					array[diplomaticContract.EmpireWhichInitiated.Index][diplomaticContract.EmpireWhichReceives.Index] = true;
				}
			}
		}
	}

	void IDiplomacyControl.SetDiplomaticRelationStateBetweenEmpires(global::Empire empireA, global::Empire empireB, StaticString diplomaticRelationStateName)
	{
		if (empireA == null || empireB == null)
		{
			throw new ArgumentNullException();
		}
		if (!this.sessionService.Session.IsHosting)
		{
			Diagnostics.LogError("Should not happen on client.");
			return;
		}
		Diagnostics.Log("[DiplomacyManager] {0} and {1} status is now {2}", new object[]
		{
			empireA,
			empireB,
			diplomaticRelationStateName
		});
		OrderChangeDiplomaticRelationState order = new OrderChangeDiplomaticRelationState(empireA.Index, empireB.Index, diplomaticRelationStateName);
		Diagnostics.Assert(this.PlayerController != null);
		this.PlayerController.PostOrder(order);
	}

	public bool TryGetActiveDiplomaticContract(global::Empire empireWhichProposes, global::Empire empireWhichReceives, out DiplomaticContract diplomaticContract)
	{
		if (empireWhichProposes == null)
		{
			throw new ArgumentNullException("empireWhichProposes");
		}
		if (empireWhichReceives == null)
		{
			throw new ArgumentNullException("empireWhichReceives");
		}
		diplomaticContract = null;
		Predicate<DiplomaticContract> match2 = (DiplomaticContract match) => match.State == DiplomaticContractState.Negotiation && match.EmpireWhichProposes.Index == empireWhichProposes.Index && match.EmpireWhichReceives.Index == empireWhichReceives.Index;
		foreach (DiplomaticContract diplomaticContract2 in this.FindAll(match2))
		{
			if (diplomaticContract == null || diplomaticContract.ContractRevisionNumber < diplomaticContract2.ContractRevisionNumber)
			{
				diplomaticContract = diplomaticContract2;
			}
		}
		return diplomaticContract != null;
	}

	public int Count
	{
		get
		{
			return this.diplomaticContractsByGUID.Count;
		}
	}

	public IGameEntity this[GameEntityGUID guid]
	{
		get
		{
			return this.diplomaticContractsByGUID[guid];
		}
	}

	public void Clear()
	{
		foreach (KeyValuePair<ulong, DiplomaticContract> keyValuePair in this.diplomaticContractsByGUID)
		{
			keyValuePair.Value.Dispose();
		}
		this.diplomaticContractsByGUID.Clear();
	}

	public bool Contains(GameEntityGUID guid)
	{
		return this.diplomaticContractsByGUID.ContainsKey(guid);
	}

	public IEnumerable<DiplomaticContract> FindAll(Predicate<DiplomaticContract> match)
	{
		Diagnostics.Assert(this.diplomaticContracts != null);
		for (int contractIndex = 0; contractIndex < this.diplomaticContracts.Count; contractIndex++)
		{
			DiplomaticContract diplomaticContract = this.diplomaticContracts[contractIndex];
			if (match == null || match(diplomaticContract))
			{
				yield return diplomaticContract;
			}
		}
		yield break;
	}

	public IEnumerator<KeyValuePair<ulong, DiplomaticContract>> GetEnumerator()
	{
		return this.diplomaticContractsByGUID.GetEnumerator();
	}

	public void InitializeDiplomaticContractRepository(global::Empire[] empires)
	{
	}

	public void Register(DiplomaticContract diplomaticContract)
	{
		if (diplomaticContract == null)
		{
			throw new ArgumentNullException("diplomaticContract");
		}
		Diagnostics.Assert(this.diplomaticContractsByGUID != null);
		this.diplomaticContractsByGUID.Add(diplomaticContract.GUID, diplomaticContract);
		Diagnostics.Assert(this.diplomaticContracts != null);
		this.diplomaticContracts.Add(diplomaticContract);
		this.OnDiplomaticContractRepositoryChange(DiplomaticContractRepositoryChangeAction.Add, diplomaticContract.GUID);
		Diagnostics.Log("Register new contract {0}.", new object[]
		{
			diplomaticContract
		});
	}

	public bool TryGetValue(GameEntityGUID guid, out DiplomaticContract diplomaticContract)
	{
		return this.diplomaticContractsByGUID.TryGetValue(guid, out diplomaticContract);
	}

	public void Unregister(DiplomaticContract diplomaticContract)
	{
		if (diplomaticContract == null)
		{
			throw new ArgumentNullException("diplomaticContract");
		}
		this.Unregister(diplomaticContract.GUID);
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
		Diagnostics.Assert(this.diplomaticContractsByGUID != null);
		DiplomaticContract item = this.diplomaticContractsByGUID[guid];
		Diagnostics.Assert(this.diplomaticContracts != null);
		this.diplomaticContracts.Remove(item);
		if (this.diplomaticContractsByGUID.Remove(guid))
		{
			this.OnDiplomaticContractRepositoryChange(DiplomaticContractRepositoryChangeAction.Remove, guid);
		}
	}

	private void OnDiplomaticContractRepositoryChange(DiplomaticContractRepositoryChangeAction action, ulong gameEntityGuid)
	{
		if (this.DiplomaticContractRepositoryChange != null)
		{
			this.DiplomaticContractRepositoryChange(this, new DiplomaticContractRepositoryChangeEventArgs(action, gameEntityGuid));
		}
	}

	public void ReadXml(XmlReader reader)
	{
		reader.ReadStartElement();
		int attribute = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("DiplomaticContractRepository");
		for (int i = 0; i < attribute; i++)
		{
			DiplomaticContract diplomaticContract = reader.ReadElementSerializable<DiplomaticContract>();
			if (diplomaticContract == null || !diplomaticContract.CheckContractDataValidity())
			{
				Diagnostics.LogWarning("Can't reload contract {0}. It will be deleted from the save.", new object[]
				{
					(diplomaticContract == null) ? GameEntityGUID.Zero : diplomaticContract.GUID
				});
			}
			else
			{
				this.Register(diplomaticContract);
			}
		}
		reader.ReadEndElement("DiplomaticContractRepository");
	}

	public void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString("AssemblyQualifiedName", base.GetType().AssemblyQualifiedName);
		writer.WriteStartElement("DiplomaticContractRepository");
		Diagnostics.Assert(this.diplomaticContracts != null);
		writer.WriteAttributeString<int>("Count", this.diplomaticContracts.Count);
		for (int i = 0; i < this.diplomaticContracts.Count; i++)
		{
			IXmlSerializable xmlSerializable = this.diplomaticContracts[i];
			writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
		}
		writer.WriteEndElement();
	}

	private global::PlayerController PlayerController
	{
		get
		{
			if (this.playerController == null && base.Game != null)
			{
				IPlayerControllerRepositoryControl playerControllerRepositoryControl = base.Game.Services.GetService<IPlayerControllerRepositoryService>() as IPlayerControllerRepositoryControl;
				if (playerControllerRepositoryControl != null)
				{
					this.playerController = playerControllerRepositoryControl.GetPlayerControllerById("server");
				}
			}
			return this.playerController;
		}
	}

	public override IEnumerator BindServices(IServiceContainer serviceContainer)
	{
		yield return base.BindServices(serviceContainer);
		this.sessionService = Services.GetService<ISessionService>();
		Diagnostics.Assert(this.sessionService != null);
		serviceContainer.AddService<IDiplomacyService>(this);
		serviceContainer.AddService<IDiplomaticContractRepositoryService>(this);
		this.technologyDefinitionDrakkenVisionDuringFirstTurn = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<string>("Gameplay/FactionTrait/Drakkens/TechnologyDefinitionDrakkenVisionDuringFirstTurn");
		yield return base.BindService<IVisibilityService>(serviceContainer, delegate(IVisibilityService service)
		{
			this.visibilityService = service;
		});
		yield break;
	}

	public override IEnumerator Ignite(IServiceContainer serviceContainer)
	{
		yield return base.Ignite(serviceContainer);
		this.diplomaticRelationStateDatabase = Databases.GetDatabase<DiplomaticRelationState>(false);
		if (this.diplomaticRelationStateDatabase == null)
		{
			Diagnostics.LogError("Failed to retrieve the database of diplomatic relation state.");
		}
		this.firstBeginTurnDone = false;
		this.visibilityControllerRefreshedArgs = new Queue<KeyValuePair<object, VisibilityRefreshedEventArgs>>();
		yield break;
	}

	public override IEnumerator LoadGame(global::Game game)
	{
		yield return base.LoadGame(game);
		Diagnostics.Assert(base.Game != null && base.Game.Empires != null);
		this.majorEmpires = Array.FindAll<global::Empire>(base.Game.Empires, (global::Empire empire) => empire is MajorEmpire);
		Diagnostics.Assert(this.sessionService.Session != null);
		if (this.sessionService.Session.IsHosting)
		{
			this.visibilityService.VisibilityRefreshed += this.VisibilityService_VisibilityRefreshed;
			IEventService eventService = Services.GetService<IEventService>();
			eventService.EventRaise += this.EventService_EventRaise;
		}
		this.InitializeDiplomaticContractRepository(this.majorEmpires);
		yield break;
	}

	public void SetDiplomaticContractState(DiplomaticContract diplomaticContract, DiplomaticContractState destinationState)
	{
		DiplomaticContractState state = diplomaticContract.State;
		if (diplomaticContract.Terms.Count > 0 && !diplomaticContract.IsTransitionPossible(destinationState))
		{
			Diagnostics.LogError("Transition between state {0} and {1} is not possible.", new object[]
			{
				state,
				destinationState
			});
		}
		int num = 0;
		if (diplomaticContract.TurnAtTheBeginningOfTheState >= 0)
		{
			num = base.Game.Turn - diplomaticContract.TurnAtTheBeginningOfTheState;
		}
		((IDiplomaticContractManagement)diplomaticContract).SetDiplomaticState(destinationState);
		if (num > 1)
		{
			return;
		}
		IEventService service = Services.GetService<IEventService>();
		service.Notify(new EventDiplomaticContractStateChange(diplomaticContract, state));
	}

	protected override void Releasing()
	{
		base.Releasing();
		Diagnostics.Assert(this.sessionService.Session != null);
		if (this.sessionService.Session.IsHosting)
		{
			this.visibilityService.VisibilityRefreshed -= this.VisibilityService_VisibilityRefreshed;
			IEventService service = Services.GetService<IEventService>();
			service.EventRaise -= this.EventService_EventRaise;
		}
		this.diplomaticContractsByGUID.Clear();
		this.majorEmpires = null;
		this.visibilityService = null;
	}

	private void EventService_EventRaise(object sender, EventRaiseEventArgs eventArgs)
	{
		EventEmpireEliminated eventEmpireEliminated = eventArgs.RaisedEvent as EventEmpireEliminated;
		if (eventEmpireEliminated == null)
		{
			return;
		}
		Amplitude.Unity.Game.Empire eliminatedEmpire = eventEmpireEliminated.EliminatedEmpire;
		DepartmentOfForeignAffairs agency = eliminatedEmpire.GetAgency<DepartmentOfForeignAffairs>();
		Diagnostics.Assert(agency != null);
		for (int i = 0; i < this.majorEmpires.Length; i++)
		{
			global::Empire empire = this.majorEmpires[i];
			if (i != eliminatedEmpire.Index)
			{
				DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(empire);
				Diagnostics.Assert(diplomaticRelation != null && diplomaticRelation.State != null);
				if (diplomaticRelation.State.Name != DiplomaticRelationState.Names.Dead)
				{
					OrderChangeDiplomaticRelationState order = new OrderChangeDiplomaticRelationState(eliminatedEmpire.Index, i, DiplomaticRelationState.Names.Dead);
					this.PlayerController.PostOrder(order);
				}
			}
		}
	}

	private WorldPosition CheckMetting(global::Empire empireA, global::Empire empireB)
	{
		Diagnostics.Assert(empireA != null && empireB != null);
		if (empireA.Index == empireB.Index)
		{
			return WorldPosition.Invalid;
		}
		DepartmentOfForeignAffairs agency = empireA.GetAgency<DepartmentOfForeignAffairs>();
		Diagnostics.Assert(agency != null);
		DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(empireB);
		if (diplomaticRelation == null || diplomaticRelation.State == null || diplomaticRelation.State.Name != DiplomaticRelationState.Names.Unknown)
		{
			return WorldPosition.Invalid;
		}
		DepartmentOfDefense agency2 = empireB.GetAgency<DepartmentOfDefense>();
		DepartmentOfTheInterior agency3 = empireB.GetAgency<DepartmentOfTheInterior>();
		DepartmentOfScience agency4 = empireA.GetAgency<DepartmentOfScience>();
		Diagnostics.Assert(agency4 != null);
		if (agency2 != null && agency3 != null && agency4.GetTechnologyState(this.technologyDefinitionDrakkenVisionDuringFirstTurn) == DepartmentOfScience.ConstructibleElement.State.Researched)
		{
			WorldPosition worldPosition = WorldPosition.Invalid;
			if (agency3.Cities.Count > 0)
			{
				worldPosition = agency3.Cities[0].WorldPosition;
			}
			else if (agency2.Armies.Count > 0)
			{
				worldPosition = agency2.Armies[0].WorldPosition;
			}
			if (worldPosition != WorldPosition.Invalid)
			{
				return worldPosition;
			}
		}
		if (agency2 != null)
		{
			ReadOnlyCollection<Army> armies = agency2.Armies;
			for (int i = 0; i < armies.Count; i++)
			{
				Army army = armies[i];
				if (!army.IsPrivateers)
				{
					if (this.visibilityService.IsWorldPositionVisibleFor(army.WorldPosition, empireA))
					{
						if (!army.IsCamouflaged || this.visibilityService.IsWorldPositionDetectedFor(army.WorldPosition, empireA))
						{
							return army.WorldPosition;
						}
					}
				}
			}
		}
		if (agency3 != null)
		{
			ReadOnlyCollection<City> cities = agency3.Cities;
			for (int j = 0; j < cities.Count; j++)
			{
				ReadOnlyCollection<District> districts = cities[j].Districts;
				for (int k = 0; k < districts.Count; k++)
				{
					if (this.visibilityService.IsWorldPositionVisibleFor(districts[k].WorldPosition, empireA))
					{
						return districts[k].WorldPosition;
					}
				}
			}
		}
		if (agency3 != null)
		{
			MajorEmpire majorEmpire = empireB as MajorEmpire;
			if (majorEmpire != null)
			{
				for (int l = 0; l < majorEmpire.ConvertedVillages.Count; l++)
				{
					if (this.visibilityService.IsWorldPositionVisibleFor(majorEmpire.ConvertedVillages[l].WorldPosition, empireA))
					{
						return majorEmpire.ConvertedVillages[l].WorldPosition;
					}
				}
			}
		}
		if (agency3 != null)
		{
			for (int m = 0; m < agency3.OccupiedFortresses.Count; m++)
			{
				if (this.visibilityService.IsWorldPositionVisibleFor(agency3.OccupiedFortresses[m].WorldPosition, empireA))
				{
					return agency3.OccupiedFortresses[m].WorldPosition;
				}
			}
		}
		if (agency3 != null)
		{
			for (int n = 0; n < agency3.Camps.Count; n++)
			{
				if (this.visibilityService.IsWorldPositionVisibleFor(agency3.Camps[n].WorldPosition, empireA))
				{
					return agency3.Camps[n].WorldPosition;
				}
			}
		}
		if (agency3 != null)
		{
			for (int num = 0; num < agency3.TamedKaijuGarrisons.Count; num++)
			{
				if (this.visibilityService.IsWorldPositionVisibleFor(agency3.TamedKaijuGarrisons[num].WorldPosition, empireA))
				{
					return agency3.TamedKaijuGarrisons[num].WorldPosition;
				}
			}
		}
		return WorldPosition.Invalid;
	}

	private void OnEmpireAsymetricDiscovery(global::Empire initiatorEmpire, global::Empire discoveredEmpire, WorldPosition discoveredEmpirePosition)
	{
		if (initiatorEmpire == null || discoveredEmpire == null)
		{
			throw new ArgumentNullException();
		}
		Diagnostics.Assert(this.pendingDiscoveryNotifications != null);
		OrderNotifyEmpireDiscovery orderNotifyEmpireDiscovery = this.pendingDiscoveryNotifications.Find((OrderNotifyEmpireDiscovery match) => (match.InitiatorEmpireIndex == initiatorEmpire.Index && match.DiscoveredEmpireIndex == discoveredEmpire.Index) || (match.InitiatorEmpireIndex == discoveredEmpire.Index && match.DiscoveredEmpireIndex == initiatorEmpire.Index));
		if (orderNotifyEmpireDiscovery == null)
		{
			orderNotifyEmpireDiscovery = new OrderNotifyEmpireDiscovery(initiatorEmpire.Index, discoveredEmpire.Index);
			this.pendingDiscoveryNotifications.Add(orderNotifyEmpireDiscovery);
		}
		if (initiatorEmpire.Index == orderNotifyEmpireDiscovery.InitiatorEmpireIndex)
		{
			orderNotifyEmpireDiscovery.DiscoveredEmpirePosition = discoveredEmpirePosition;
		}
		else if (initiatorEmpire.Index == orderNotifyEmpireDiscovery.DiscoveredEmpireIndex)
		{
			orderNotifyEmpireDiscovery.InitiatorEmpirePosition = discoveredEmpirePosition;
		}
	}

	private void OnEmpireSymetricDiscovery(global::Empire initiatorEmpire, global::Empire discoveredEmpire, WorldPosition initiatorEmpirePosition, WorldPosition discoveredEmpirePosition)
	{
		if (initiatorEmpire == null || discoveredEmpire == null)
		{
			throw new ArgumentNullException();
		}
		Diagnostics.Assert(this.pendingDiscoveryNotifications != null);
		OrderNotifyEmpireDiscovery orderNotifyEmpireDiscovery = this.pendingDiscoveryNotifications.Find((OrderNotifyEmpireDiscovery match) => (match.InitiatorEmpireIndex == initiatorEmpire.Index && match.DiscoveredEmpireIndex == discoveredEmpire.Index) || (match.InitiatorEmpireIndex == discoveredEmpire.Index && match.DiscoveredEmpireIndex == initiatorEmpire.Index));
		if (orderNotifyEmpireDiscovery == null)
		{
			orderNotifyEmpireDiscovery = new OrderNotifyEmpireDiscovery(initiatorEmpire.Index, discoveredEmpire.Index);
			this.pendingDiscoveryNotifications.Add(orderNotifyEmpireDiscovery);
		}
		orderNotifyEmpireDiscovery.InitiatorEmpirePosition = initiatorEmpirePosition;
		orderNotifyEmpireDiscovery.DiscoveredEmpirePosition = discoveredEmpirePosition;
	}

	private void LateUpdate()
	{
		Diagnostics.Assert(this.pendingDiscoveryNotifications != null);
		if (this.pendingDiscoveryNotifications.Count > 0)
		{
			for (int i = 0; i < this.pendingDiscoveryNotifications.Count; i++)
			{
				OrderNotifyEmpireDiscovery order = this.pendingDiscoveryNotifications[i];
				Diagnostics.Assert(this.PlayerController != null);
				this.PlayerController.PostOrder(order);
			}
			this.pendingDiscoveryNotifications.Clear();
		}
	}

	private void OnVisibityRefreshed(object sender, VisibilityRefreshedEventArgs args)
	{
		MajorEmpire majorEmpire = args.Empire as MajorEmpire;
		if (majorEmpire == null)
		{
			return;
		}
		DepartmentOfScience agency = majorEmpire.GetAgency<DepartmentOfScience>();
		for (int i = 0; i < this.majorEmpires.Length; i++)
		{
			global::Empire empire = this.majorEmpires[i];
			if (majorEmpire.Index != empire.Index)
			{
				WorldPosition worldPosition = this.CheckMetting(majorEmpire, empire);
				if (worldPosition != WorldPosition.Invalid)
				{
					WorldPosition worldPosition2 = this.CheckMetting(empire, majorEmpire);
					if (worldPosition2 != WorldPosition.Invalid)
					{
						this.OnEmpireSymetricDiscovery(majorEmpire, empire, worldPosition, worldPosition2);
					}
					else if (agency.GetTechnologyState(this.technologyDefinitionDrakkenVisionDuringFirstTurn) == DepartmentOfScience.ConstructibleElement.State.Researched)
					{
						this.OnEmpireAsymetricDiscovery(majorEmpire, empire, worldPosition);
					}
				}
			}
		}
	}

	private void VisibilityService_VisibilityRefreshed(object sender, VisibilityRefreshedEventArgs args)
	{
		if (this.firstBeginTurnDone)
		{
			this.OnVisibityRefreshed(sender, args);
		}
		else
		{
			this.visibilityControllerRefreshedArgs.Enqueue(new KeyValuePair<object, VisibilityRefreshedEventArgs>(sender, args));
		}
	}

	private Dictionary<ulong, DiplomaticContract> diplomaticContractsByGUID = new Dictionary<ulong, DiplomaticContract>();

	private List<DiplomaticContract> diplomaticContracts = new List<DiplomaticContract>();

	private IDatabase<DiplomaticRelationState> diplomaticRelationStateDatabase;

	private global::Empire[] majorEmpires;

	private global::PlayerController playerController;

	private ISessionService sessionService;

	private IVisibilityService visibilityService;

	private StaticString technologyDefinitionDrakkenVisionDuringFirstTurn;

	private bool firstBeginTurnDone;

	private Queue<KeyValuePair<object, VisibilityRefreshedEventArgs>> visibilityControllerRefreshedArgs;

	private List<OrderNotifyEmpireDiscovery> pendingDiscoveryNotifications = new List<OrderNotifyEmpireDiscovery>();

	public delegate void RequestActiveDiplomaticContractCallback(DiplomaticContract contract);
}
