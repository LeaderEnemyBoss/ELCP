using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Game.Orders;
using Amplitude.Unity.Session;
using Amplitude.Unity.Simulation;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

[OrderProcessor(typeof(OrderQueueCreepingNode), "QueueCreepingNode")]
[OrderProcessor(typeof(OrderDestroyCreepingNode), "DestroyCreepingNode")]
[OrderProcessor(typeof(OrderBuyoutCreepingNode), "BuyoutCreepingNode")]
public class DepartmentOfCreepingNodes : Agency, IXmlSerializable
{
	public DepartmentOfCreepingNodes(global::Empire empire) : base(empire)
	{
	}

	public event CollectionChangeEventHandler CollectionChanged;

	public event System.EventHandler OnNodeBuyout;

	public override void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		base.ReadXml(reader);
		int attribute = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("Nodes");
		this.nodes.Clear();
		for (int i = 0; i < attribute; i++)
		{
			ulong attribute2 = reader.GetAttribute<ulong>("GUID");
			CreepingNode creepingNode = new CreepingNode(attribute2, base.Empire as global::Empire);
			reader.ReadElementSerializable<CreepingNode>(ref creepingNode);
			if (creepingNode != null)
			{
				this.nodes.Add(creepingNode);
			}
		}
		reader.ReadEndElement("Nodes");
	}

	public override void WriteXml(XmlWriter writer)
	{
		int num = writer.WriteVersionAttribute(1);
		base.WriteXml(writer);
		writer.WriteStartElement("Nodes");
		writer.WriteAttributeString<int>("Count", this.nodes.Count);
		for (int i = 0; i < this.nodes.Count; i++)
		{
			IXmlSerializable xmlSerializable = this.nodes[i];
			writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
		}
		writer.WriteEndElement();
	}

	private bool BuyoutCreepingNodePreprocessor(OrderBuyoutCreepingNode order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.NodeEntityGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			return false;
		}
		DepartmentOfTheTreasury agency = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		CreepingNode creepingNode = gameEntity as CreepingNode;
		if (creepingNode == null)
		{
			Diagnostics.LogError("Order GUID does not belong to a creeping node.");
			return false;
		}
		float num = -creepingNode.GetBuyoutCost();
		if (!agency.IsTransferOfResourcePossible(base.Empire, creepingNode.NodeDefinition.BuyoutCost.ResourceName, ref num))
		{
			Diagnostics.LogWarning("Order preprocessing failed because we don't have enough resources.");
			return false;
		}
		return true;
	}

	private IEnumerator BuyoutCreepingNodeProcessor(OrderBuyoutCreepingNode order)
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
		DepartmentOfTheTreasury departmentOfTheTreasury = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.NodeEntityGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			yield break;
		}
		if (!(gameEntity is SimulationObjectWrapper))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a simulation object wrapper.");
			yield break;
		}
		CreepingNode node = gameEntity as CreepingNode;
		if (node == null)
		{
			Diagnostics.LogError("Order GUID does not belong to a creeping node.");
			yield break;
		}
		float buyoutCost = -node.GetBuyoutCost();
		if (!departmentOfTheTreasury.IsTransferOfResourcePossible(base.Empire, node.NodeDefinition.BuyoutCost.ResourceName, ref buyoutCost))
		{
			Diagnostics.LogWarning("Order preprocessing failed because we don't have enough resources.");
			yield break;
		}
		if (!departmentOfTheTreasury.TryTransferResources(base.Empire, node.NodeDefinition.BuyoutCost.ResourceName, buyoutCost))
		{
			Diagnostics.LogError("Order preprocessing failed because buyout costs transfer failed.");
			yield break;
		}
		node.CompleteUpgrade();
		if (node.PointOfInterest.Type == "Village")
		{
			this.SetupInfectedVillage(node);
		}
		if (this.OnNodeBuyout != null)
		{
			this.OnNodeBuyout(this, new EventArgs());
		}
		yield break;
	}

	private bool DestroyCreepingNodePreprocessor(OrderDestroyCreepingNode order)
	{
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.NodeEntityGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			return false;
		}
		if (!(gameEntity is CreepingNode))
		{
			Diagnostics.LogError("Order GUID does not belong to a creeping node.");
			return false;
		}
		return true;
	}

	private IEnumerator DestroyCreepingNodeProcessor(OrderDestroyCreepingNode order)
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
		if (!this.GameEntityRepositoryService.TryGetValue(order.NodeEntityGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			yield break;
		}
		if (!(gameEntity is SimulationObjectWrapper))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a simulation object wrapper.");
			yield break;
		}
		CreepingNode node = gameEntity as CreepingNode;
		if (node == null)
		{
			Diagnostics.LogError("Order GUID does not belong to a creeping node.");
			yield break;
		}
		if (this.Nodes.Contains(node))
		{
			this.RemoveCreepingNode(node);
			this.GameEntityRepositoryService.Unregister(node);
			yield break;
		}
		Diagnostics.LogError("Department does not contain the provided node.");
		yield break;
	}

	private bool QueueCreepingNodePreprocessor(OrderQueueCreepingNode order)
	{
		DepartmentOfTheInterior agency = base.Empire.GetAgency<DepartmentOfTheInterior>();
		if (agency == null || agency.MainCity == null)
		{
			Diagnostics.LogWarning("Order preprocessing failed Empire does not have a main city.");
			return false;
		}
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.ContextGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			return false;
		}
		if (!(gameEntity is SimulationObjectWrapper))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a simulation object wrapper.");
			return false;
		}
		IGameEntity gameEntity2;
		if (!this.GameEntityRepositoryService.TryGetValue(order.PointOfInterestGUID, out gameEntity2))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			return false;
		}
		PointOfInterest pointOfInterest = gameEntity2 as PointOfInterest;
		if (pointOfInterest == null)
		{
			return false;
		}
		if (pointOfInterest.CreepingNodeImprovement != null || (pointOfInterest.PointOfInterestImprovement != null && pointOfInterest.Type != "Village"))
		{
			return false;
		}
		Diagnostics.Assert(this.creepingNodeDefinitionDatabase != null);
		CreepingNodeImprovementDefinition creepingNodeImprovementDefinition;
		if (!this.creepingNodeDefinitionDatabase.TryGetValue(order.ConstructibleElementName, out creepingNodeImprovementDefinition))
		{
			Diagnostics.LogError("Order preprocessing failed because the constructible element {0} is not in the constructible element database.", new object[]
			{
				order.ConstructibleElementName
			});
			return false;
		}
		object[] customAttributes = creepingNodeImprovementDefinition.GetType().GetCustomAttributes(typeof(WorldPlacementCursorAttribute), true);
		if (customAttributes != null && customAttributes.Length != 0 && !order.WorldPosition.IsValid)
		{
			return false;
		}
		SimulationObjectWrapper simulationObjectWrapper = gameEntity as SimulationObjectWrapper;
		Diagnostics.Assert(this.DepartmentOfTheTreasury != null);
		if (order.CheckPrerequisites && !DepartmentOfTheTreasury.CheckConstructiblePrerequisites(simulationObjectWrapper, creepingNodeImprovementDefinition, new string[0]))
		{
			Diagnostics.LogWarning("Order preprocessing failed because the constructible element {0} is not allowed to be queued.", new object[]
			{
				order.ConstructibleElementName
			});
			return false;
		}
		order.NodeEntityGUID = this.GameEntityRepositoryService.GenerateGUID();
		bool result = true;
		if (creepingNodeImprovementDefinition.Costs == null || creepingNodeImprovementDefinition.Costs.Length == 0)
		{
			order.ResourceStocks = new ConstructionResourceStock[0];
		}
		else
		{
			ConstructionResourceStock[] resourceStocks = new ConstructionResourceStock[creepingNodeImprovementDefinition.Costs.Length];
			result = this.DepartmentOfTheTreasury.TryGetInstantConstructionResourceCost(simulationObjectWrapper, creepingNodeImprovementDefinition, true, out resourceStocks);
			order.ResourceStocks = resourceStocks;
		}
		return result;
	}

	private IEnumerator QueueCreepingNodeProcessor(OrderQueueCreepingNode order)
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
		if (!this.GameEntityRepositoryService.TryGetValue(order.ContextGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			yield break;
		}
		if (!(gameEntity is SimulationObjectWrapper))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not a simulation object wrapper.");
			yield break;
		}
		IGameEntity poiEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.PointOfInterestGUID, out poiEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target game entity is not valid.");
			yield break;
		}
		PointOfInterest pointOfInterest = poiEntity as PointOfInterest;
		if (pointOfInterest == null)
		{
			Diagnostics.LogError("Provided poi entity is not a valid poi");
			yield break;
		}
		IGameEntity context = gameEntity;
		SimulationObjectWrapper simulationObjectWrapper = context as SimulationObjectWrapper;
		if (order.NodeEntityGUID == GameEntityGUID.Zero)
		{
			Diagnostics.LogError("Skipping queue construction process because the game entity guid is null.");
			yield break;
		}
		CreepingNodeImprovementDefinition constructibleElement;
		if (!this.creepingNodeDefinitionDatabase.TryGetValue(order.ConstructibleElementName, out constructibleElement))
		{
			Diagnostics.LogError("Skipping queue construction process because the constructible element {0} is not in the constructible element database.", new object[]
			{
				order.ConstructibleElementName
			});
			yield break;
		}
		global::Empire empire;
		try
		{
			empire = game.Empires[order.EmpireIndex];
		}
		catch
		{
			Diagnostics.LogError("Order processor failed because empire index is invalid.");
			yield break;
		}
		CreepingNode node = new CreepingNode(order.NodeEntityGUID, empire, pointOfInterest, constructibleElement);
		if (this.simulationDescriptorDatabase != null)
		{
			SimulationDescriptor descriptor = null;
			if (this.simulationDescriptorDatabase.TryGetValue("ClassCreepingNode", out descriptor))
			{
				node.AddDescriptor(descriptor, false);
			}
			if (!string.IsNullOrEmpty(node.NodeDefinition.ConstructionCostDescriptor) && this.simulationDescriptorDatabase.TryGetValue(node.NodeDefinition.ConstructionCostDescriptor, out descriptor))
			{
				node.AddDescriptor(descriptor, false);
			}
		}
		if (constructibleElement.Costs != null && constructibleElement.Costs.Length > 0)
		{
			for (int index = 0; index < constructibleElement.Costs.Length; index++)
			{
				if (order.ResourceStocks[index].Stock > 0f && !this.DepartmentOfTheTreasury.TryTransferResources(simulationObjectWrapper.SimulationObject, constructibleElement.Costs[index].ResourceName, -order.ResourceStocks[index].Stock))
				{
					Diagnostics.LogError("Order processing failed because the constructible element '{0}' ask for instant resource '{1}' that can't be retrieve.", new object[]
					{
						constructibleElement.Name,
						constructibleElement.Costs[index].ResourceName
					});
					yield break;
				}
			}
		}
		this.AddCreepingNode(node);
		this.GameEntityRepositoryService.Register(node);
		if (order.IsIstantConstruction)
		{
			node.CompleteUpgrade();
			if (node.PointOfInterest.Type == "Village")
			{
				this.SetupInfectedVillage(node);
			}
		}
		yield break;
	}

	public List<CreepingNode> Nodes
	{
		get
		{
			return this.nodes;
		}
	}

	public DepartmentOfTheTreasury DepartmentOfTheTreasury { get; private set; }

	private DepartmentOfTheInterior DepartmentOfTheInterior { get; set; }

	private DepartmentOfScience DepartmentOfScience { get; set; }

	private IEventService EventService { get; set; }

	private IGameEntityRepositoryService GameEntityRepositoryService { get; set; }

	private IPlayerControllerRepositoryService PlayerControllerRepositoryService { get; set; }

	protected override IEnumerator OnInitialize()
	{
		yield return base.OnInitialize();
		this.nodes = new List<CreepingNode>();
		this.gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(this.gameService != null);
		this.EventService = Services.GetService<IEventService>();
		Diagnostics.Assert(this.EventService != null);
		this.EventService.EventRaise += this.EventService_EventRaise;
		ISessionService sessionService = Services.GetService<ISessionService>();
		Diagnostics.Assert(sessionService != null);
		this.GameEntityRepositoryService = this.gameService.Game.Services.GetService<IGameEntityRepositoryService>();
		if (this.GameEntityRepositoryService == null)
		{
			Diagnostics.LogError("Failed to retrieve the game entity repository service.");
		}
		this.PlayerControllerRepositoryService = this.gameService.Game.Services.GetService<IPlayerControllerRepositoryService>();
		if (this.PlayerControllerRepositoryService == null)
		{
			Diagnostics.LogError("Failed to retrieve the player controller repository service.");
		}
		this.creepingNodeDefinitionDatabase = Databases.GetDatabase<CreepingNodeImprovementDefinition>(true);
		this.simulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
		this.DepartmentOfTheTreasury = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		Diagnostics.Assert(this.DepartmentOfTheTreasury != null);
		this.DepartmentOfTheInterior = base.Empire.GetAgency<DepartmentOfTheInterior>();
		Diagnostics.Assert(this.DepartmentOfTheInterior != null);
		this.DepartmentOfScience = base.Empire.GetAgency<DepartmentOfScience>();
		Diagnostics.Assert(this.DepartmentOfScience != null);
		this.DepartmentOfScience.TechnologyUnlocked += this.DepartmentOfScience_TechnologyUnlocked;
		base.Empire.RegisterPass("GameClientState_Turn_Begin", "TurnBegin", new Agency.Action(this.GameClientState_Turn_Begin), new string[0]);
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
		City mainCity = this.DepartmentOfTheInterior.MainCity;
		if (mainCity != null)
		{
			for (int index = 0; index < this.nodes.Count; index++)
			{
				this.nodes[index].PointOfInterest.Empire = (base.Empire as global::Empire);
				if (!this.nodes[index].IsUnderConstruction)
				{
					DepartmentOfTheInterior.GenerateFIMSEForCreepingNode(base.Empire, this.Nodes[index]);
					this.nodes[index].ReApplyImprovementDescriptors();
					this.nodes[index].Refresh(false);
				}
				mainCity.AddChild(this.nodes[index]);
				this.GameEntityRepositoryService.Register(this.nodes[index]);
			}
			mainCity.Refresh(false);
		}
		else if (this.nodes.Count > 0)
		{
			Diagnostics.LogError("Nodes were loaded but there is no MainCity to assign them.");
		}
		base.Empire.Refresh(false);
		yield break;
	}

	protected override void OnRelease()
	{
		base.OnRelease();
		this.GameEntityRepositoryService = null;
		this.PlayerControllerRepositoryService = null;
		this.creepingNodeDefinitionDatabase = null;
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
		if (this.DepartmentOfScience != null)
		{
			this.DepartmentOfScience.TechnologyUnlocked -= this.DepartmentOfScience_TechnologyUnlocked;
			this.DepartmentOfScience = null;
		}
		this.DepartmentOfTheInterior = null;
		this.DepartmentOfTheTreasury = null;
	}

	private void AddCreepingNode(CreepingNode node)
	{
		this.nodes.Add(node);
		if (this.DepartmentOfTheInterior.MainCity != null && node.SimulationObject.Parent != this.DepartmentOfTheInterior.MainCity.SimulationObject)
		{
			this.DepartmentOfTheInterior.MainCity.SimulationObject.AddChild(node);
		}
		node.PointOfInterest.Empire = (base.Empire as global::Empire);
		node.PointOfInterest.CreepingNodeImprovement = node.NodeDefinition;
		node.PointOfInterest.CreepingNodeGUID = node.GUID;
		SimulationDescriptor descriptor;
		if (this.simulationDescriptorDatabase.TryGetValue(DepartmentOfCreepingNodes.InfectedPointOfInterest, out descriptor))
		{
			node.PointOfInterest.AddDescriptor(descriptor, false);
		}
		base.Empire.Refresh(false);
		this.OnCollectionChanged(CollectionChangeAction.Add, node);
	}

	private void RemoveCreepingNode(CreepingNode node)
	{
		if (node.SimulationObject.Parent != null)
		{
			node.SimulationObject.Parent.RemoveChild(node);
		}
		node.PointOfInterest.Empire = ((node.PointOfInterest.Region.City == null) ? null : node.PointOfInterest.Region.City.Empire);
		node.PointOfInterest.CreepingNodeImprovement = null;
		node.PointOfInterest.CreepingNodeGUID = GameEntityGUID.Zero;
		SimulationDescriptor descriptor;
		if (this.simulationDescriptorDatabase.TryGetValue(DepartmentOfCreepingNodes.InfectedPointOfInterest, out descriptor))
		{
			node.PointOfInterest.RemoveDescriptor(descriptor);
		}
		if (node.PointOfInterest.Type == "Village")
		{
			this.RemoveInfectedVillage(node);
		}
		Army dismantlingArmy = node.DismantlingArmy;
		if (dismantlingArmy != null)
		{
			dismantlingArmy.Empire.GetAgency<DepartmentOfDefense>().StopDismantelingCreepingNode(dismantlingArmy, node);
		}
		base.Empire.Refresh(false);
		this.nodes.Remove(node);
		node.Dispose();
		this.OnCollectionChanged(CollectionChangeAction.Remove, node);
	}

	private void OnCollectionChanged(CollectionChangeAction action, object element)
	{
		if (this.CollectionChanged != null)
		{
			this.CollectionChanged(this, new CollectionChangeEventArgs(action, element));
		}
	}

	private IEnumerator GameClientState_Turn_Begin(string context, string name)
	{
		for (int index = 0; index < this.Nodes.Count; index++)
		{
			this.Nodes[index].OnBeginTurn();
		}
		this.CheckCompletedNodes();
		this.CheckNodesDismantling();
		yield break;
	}

	private void CheckCompletedNodes()
	{
		global::Game game = this.gameService.Game as global::Game;
		for (int i = 0; i < this.Nodes.Count; i++)
		{
			if (this.Nodes[i].IsUpgradeReady && this.Nodes[i].UpgradeReachedTurn == game.Turn)
			{
				this.Nodes[i].PointOfInterest.CreepingNodeImprovement = this.Nodes[i].NodeDefinition;
				if (this.Nodes[i].PointOfInterest.Type == "Village")
				{
					this.SetupInfectedVillage(this.Nodes[i]);
				}
			}
		}
	}

	private void CheckNodesDismantling()
	{
		global::Game game = this.gameService.Game as global::Game;
		List<CreepingNode> list = new List<CreepingNode>();
		for (int i = 0; i < this.Nodes.Count; i++)
		{
			CreepingNode creepingNode = this.Nodes[i];
			if (creepingNode.DismantlingArmyGUID != GameEntityGUID.Zero && creepingNode.LastTurnWhenDismantleBegun < game.Turn && creepingNode.Life <= 0f)
			{
				list.Add(creepingNode);
			}
		}
		global::PlayerController playerControllerById = ((IPlayerControllerRepositoryControl)this.PlayerControllerRepositoryService).GetPlayerControllerById("server");
		for (int j = 0; j < list.Count; j++)
		{
			CreepingNode creepingNode2 = list[j];
			if (playerControllerById != null)
			{
				OrderDismantleCreepingNodeSucceed order = new OrderDismantleCreepingNodeSucceed(creepingNode2.DismantlingArmy.Empire.Index, creepingNode2.DismantlingArmyGUID, creepingNode2.GUID, ArmyAction_ToggleDismantleCreepingNode.ReadOnlyName);
				playerControllerById.PostOrder(order);
			}
		}
	}

	private void SetupInfectedVillage(CreepingNode node)
	{
		Region region = node.Region;
		if (region != null)
		{
			BarbarianCouncil agency = region.MinorEmpire.GetAgency<BarbarianCouncil>();
			Village villageAt = agency.GetVillageAt(node.WorldPosition);
			if (villageAt != null)
			{
				SimulationDescriptor simulationDescriptor;
				if (this.simulationDescriptorDatabase.TryGetValue(DepartmentOfCreepingNodes.VillageInfectionComplete, out simulationDescriptor) && !node.PointOfInterest.SimulationObject.Tags.Contains(simulationDescriptor.Name))
				{
					node.PointOfInterest.AddDescriptor(simulationDescriptor, false);
				}
				if (villageAt.PointOfInterest != null && villageAt.PointOfInterest.PointOfInterestImprovement == null)
				{
					this.DepartmentOfTheInterior.RebuildVillage(villageAt);
				}
				(base.Empire as MajorEmpire).AddInfectedVillage(villageAt);
				this.DepartmentOfTheInterior.NotifyMinorFactionRdyForAssimilation(villageAt.MinorEmpire);
			}
		}
	}

	private void RemoveInfectedVillage(CreepingNode node)
	{
		Village infectedVillageAt = (base.Empire as MajorEmpire).GetInfectedVillageAt(node.WorldPosition);
		if (infectedVillageAt != null)
		{
			SimulationDescriptor descriptor;
			if (this.simulationDescriptorDatabase.TryGetValue(DepartmentOfCreepingNodes.VillageInfectionComplete, out descriptor))
			{
				node.PointOfInterest.RemoveDescriptor(descriptor);
			}
			(base.Empire as MajorEmpire).RemoveInfectedVillage(infectedVillageAt);
			this.DepartmentOfTheInterior.UnbindInfectedVillage(infectedVillageAt);
		}
	}

	private IEnumerator GameClientState_Turn_End_ChargeCosts(string context, string name)
	{
		for (int index = 0; index < this.Nodes.Count; index++)
		{
			if (!this.Nodes[index].IsUpgradeReady)
			{
				ConstructibleElement constructible = this.Nodes[index].NodeDefinition;
				if (constructible.Costs != null && constructible.Costs.Length != 0)
				{
					for (int i = 0; i < constructible.Costs.Length; i++)
					{
						if (!constructible.Costs[i].Instant && !constructible.Costs[i].InstantOnCompletion)
						{
							StaticString resourceName = constructible.Costs[i].ResourceName;
							float constructibleCost = constructible.Costs[i].GetValue(base.Empire);
							if (!this.DepartmentOfTheTreasury.TryTransferResources(base.Empire.SimulationObject, resourceName, -constructibleCost))
							{
								Diagnostics.LogWarning("Transfer of resource '{0}' is not possible.", new object[]
								{
									constructible.Costs[i].ResourceName
								});
							}
						}
					}
				}
			}
		}
		yield break;
	}

	private void EventService_EventRaise(object sender, EventRaiseEventArgs e)
	{
		DepartmentOfCreepingNodes.EventHandler eventHandler;
		if (this.eventHandlers != null && e.RaisedEvent != null && this.eventHandlers.TryGetValue(e.RaisedEvent.EventName, out eventHandler))
		{
			eventHandler(e.RaisedEvent);
		}
	}

	private void RegisterEventHandlers()
	{
		this.eventHandlers = new Dictionary<StaticString, DepartmentOfCreepingNodes.EventHandler>();
	}

	internal virtual void OnEmpireEliminated(global::Empire empire, bool authorized)
	{
		if (empire.Index == base.Empire.Index)
		{
			GameEntityGUID[] array = new GameEntityGUID[this.Nodes.Count];
			for (int i = 0; i < this.Nodes.Count; i++)
			{
				array[i] = this.Nodes[i].GUID;
			}
			for (int j = 0; j < array.Length; j++)
			{
				global::PlayerController server = (base.Empire as global::Empire).PlayerControllers.Server;
				if (server != null)
				{
					Ticket ticket = null;
					PointOfInterest pointOfInterest = this.Nodes[j].PointOfInterest;
					OrderDestroyCreepingNode order = new OrderDestroyCreepingNode(base.Empire.Index, array[j]);
					server.PostOrder(order, out ticket, delegate(object sender, TicketRaisedEventArgs e)
					{
						if (pointOfInterest.Region.City != null)
						{
							global::Empire empire2 = pointOfInterest.Region.City.Empire;
							OrderUpgradePointOfInterest order2 = new OrderUpgradePointOfInterest(empire2.Index, pointOfInterest.GUID);
							global::PlayerController server2 = empire2.PlayerControllers.Server;
							if (server2 != null)
							{
								server2.PostOrder(order2);
							}
						}
					});
				}
			}
		}
	}

	private void DepartmentOfScience_TechnologyUnlocked(object sender, ConstructibleElementEventArgs e)
	{
		if (!base.Empire.SimulationObject.Tags.Contains("FactionTraitMimics1"))
		{
			return;
		}
		List<StaticString> list = new List<StaticString>();
		if (e.ConstructibleElement is DepartmentOfScience.ConstructibleElement)
		{
			List<ConstructibleElement> unlocksByTechnology = (e.ConstructibleElement as DepartmentOfScience.ConstructibleElement).GetUnlocksByTechnology();
			if (unlocksByTechnology != null)
			{
				for (int i = 0; i < unlocksByTechnology.Count; i++)
				{
					CreepingNodeImprovementDefinition creepingNodeImprovementDefinition = unlocksByTechnology[i] as CreepingNodeImprovementDefinition;
					if (creepingNodeImprovementDefinition != null)
					{
						list.Add(creepingNodeImprovementDefinition.PointOfInterestTemplateName);
					}
				}
			}
		}
		if (list.Count > 0)
		{
			MajorEmpire majorEmpire = base.Empire as MajorEmpire;
			if (majorEmpire != null && majorEmpire.TamedKaijus != null)
			{
				for (int j = 0; j < majorEmpire.TamedKaijus.Count; j++)
				{
					DepartmentOfTheInterior.GenerateResourcesLeechingForTamedKaijus(majorEmpire.TamedKaijus[j]);
				}
			}
		}
		City mainCity = this.DepartmentOfTheInterior.MainCity;
		if (mainCity == null)
		{
			return;
		}
		List<StaticString> lastFailureFlags = new List<StaticString>();
		for (int k = 0; k < this.Nodes.Count; k++)
		{
			PointOfInterest pointOfInterest = this.Nodes[k].PointOfInterest;
			if (list.Contains(pointOfInterest.PointOfInterestDefinition.PointOfInterestTemplateName))
			{
				CreepingNodeImprovementDefinition bestCreepingNodeDefinition = this.GetBestCreepingNodeDefinition(mainCity, pointOfInterest, pointOfInterest.CreepingNodeImprovement as CreepingNodeImprovementDefinition, lastFailureFlags);
				if (bestCreepingNodeDefinition != null)
				{
					this.Nodes[k].UpgradeNode(bestCreepingNodeDefinition);
				}
			}
		}
		foreach (PointOfInterest pointOfInterest in mainCity.Region.PointOfInterests)
		{
			PointOfInterest pointOfInterest;
			if (list.Contains(pointOfInterest.PointOfInterestDefinition.PointOfInterestTemplateName))
			{
				if (pointOfInterest.IsResourceDeposit())
				{
					for (int m = 0; m < mainCity.Districts.Count; m++)
					{
						District district = mainCity.Districts[m];
						SimulationDescriptor descriptor;
						if (district.WorldPosition == pointOfInterest.WorldPosition && !district.SimulationObject.Tags.Contains("DistrictExploitableResource") && this.simulationDescriptorDatabase.TryGetValue("DistrictExploitableResource", out descriptor))
						{
							district.AddDescriptor(descriptor, false);
							break;
						}
					}
				}
				if (pointOfInterest.CreepingNodeImprovement == null && pointOfInterest.PointOfInterestImprovement == null)
				{
					for (int n = 0; n < mainCity.Districts.Count; n++)
					{
						if (pointOfInterest.WorldPosition == mainCity.Districts[n].WorldPosition && mainCity.Districts[n].Type != DistrictType.Exploitation)
						{
							this.BuildFreeCreepingNodeImprovement(mainCity, pointOfInterest);
						}
					}
					if (mainCity.Camp != null)
					{
						for (int num = 0; num < mainCity.Camp.Districts.Count; num++)
						{
							if (pointOfInterest.WorldPosition == mainCity.Camp.Districts[num].WorldPosition && mainCity.Camp.Districts[num].Type != DistrictType.Exploitation)
							{
								this.BuildFreeCreepingNodeImprovement(mainCity, pointOfInterest);
							}
						}
					}
				}
			}
		}
	}

	public void BuildFreeCreepingNodeImprovement(City city, WorldPosition districtPosition)
	{
		Region region = city.Region;
		if (region != null && region.PointOfInterests != null)
		{
			for (int i = 0; i < region.PointOfInterests.Length; i++)
			{
				PointOfInterest pointOfInterest = region.PointOfInterests[i];
				if (!(pointOfInterest.WorldPosition != districtPosition))
				{
					this.BuildFreeCreepingNodeImprovement(city, pointOfInterest);
					break;
				}
			}
		}
	}

	private void BuildFreeCreepingNodeImprovement(City city, PointOfInterest pointOfInterest)
	{
		DepartmentOfTheInterior agency = city.Empire.GetAgency<DepartmentOfTheInterior>();
		if (agency == null || agency.MainCity == null)
		{
			return;
		}
		if (pointOfInterest.CreepingNodeImprovement != null || pointOfInterest.PointOfInterestImprovement != null)
		{
			return;
		}
		CreepingNodeImprovementDefinition[] values = this.creepingNodeDefinitionDatabase.GetValues();
		CreepingNodeImprovementDefinition creepingNodeImprovementDefinition = null;
		List<StaticString> list = new List<StaticString>();
		foreach (CreepingNodeImprovementDefinition creepingNodeImprovementDefinition2 in values)
		{
			if (creepingNodeImprovementDefinition2 != null && creepingNodeImprovementDefinition2.PointOfInterestTemplateName == pointOfInterest.PointOfInterestDefinition.PointOfInterestTemplateName)
			{
				list.Clear();
				DepartmentOfTheTreasury.CheckConstructiblePrerequisites(city, creepingNodeImprovementDefinition2, ref list, new string[]
				{
					ConstructionFlags.Prerequisite
				});
				if (!list.Contains(ConstructionFlags.Discard))
				{
					creepingNodeImprovementDefinition = this.GetBestCreepingNodeDefinition(city, pointOfInterest, creepingNodeImprovementDefinition2, list);
					break;
				}
			}
		}
		if (creepingNodeImprovementDefinition != null)
		{
			global::PlayerController playerControllerById = ((IPlayerControllerRepositoryControl)this.PlayerControllerRepositoryService).GetPlayerControllerById("server");
			if (playerControllerById != null)
			{
				OrderQueueCreepingNode order = new OrderQueueCreepingNode(base.Empire.Index, city.GUID, pointOfInterest.GUID, creepingNodeImprovementDefinition, pointOfInterest.WorldPosition, true, true);
				playerControllerById.PostOrder(order);
			}
		}
	}

	public CreepingNodeImprovementDefinition GetBestCreepingNodeDefinition(SimulationObject context, PointOfInterest pointOfInterest, CreepingNodeImprovementDefinition bestCreepingNodeDefinition, List<StaticString> lastFailureFlags)
	{
		if (bestCreepingNodeDefinition == null)
		{
			return null;
		}
		int num = 0;
		CreepingNodeImprovementDefinition creepingNodeImprovementDefinition = bestCreepingNodeDefinition;
		while (!StaticString.IsNullOrEmpty(creepingNodeImprovementDefinition.NextUpgradeName))
		{
			CreepingNodeImprovementDefinition creepingNodeImprovementDefinition2;
			if (!this.creepingNodeDefinitionDatabase.TryGetValue(creepingNodeImprovementDefinition.NextUpgradeName, out creepingNodeImprovementDefinition2))
			{
				Diagnostics.LogWarning("The point of interest improvement '{0}' has an invalid constructible '{1}' as next upgrade.", new object[]
				{
					bestCreepingNodeDefinition.Name,
					bestCreepingNodeDefinition.NextUpgradeName
				});
				break;
			}
			creepingNodeImprovementDefinition = creepingNodeImprovementDefinition2;
			if (creepingNodeImprovementDefinition == null)
			{
				Diagnostics.LogWarning("The point of interest improvement '{0}' has an invalid constructible '{1}' as next upgrade.", new object[]
				{
					bestCreepingNodeDefinition.Name,
					bestCreepingNodeDefinition.NextUpgradeName
				});
				break;
			}
			if (creepingNodeImprovementDefinition.Name == bestCreepingNodeDefinition.Name)
			{
				Diagnostics.LogWarning("The point of interest improvement '{0}' has himself as next upgrade.", new object[]
				{
					bestCreepingNodeDefinition.Name
				});
				break;
			}
			if (creepingNodeImprovementDefinition.PointOfInterestTemplateName != pointOfInterest.PointOfInterestDefinition.PointOfInterestTemplateName)
			{
				Diagnostics.LogWarning("The point of interest improvement '{0}' has a constructible '{1}' which does not apply to the point template '{2}' as next upgrade.", new object[]
				{
					bestCreepingNodeDefinition.Name,
					bestCreepingNodeDefinition.NextUpgradeName,
					pointOfInterest.PointOfInterestDefinition.PointOfInterestTemplateName
				});
				break;
			}
			lastFailureFlags.Clear();
			DepartmentOfTheTreasury.CheckConstructiblePrerequisites(context, creepingNodeImprovementDefinition, ref lastFailureFlags, new string[]
			{
				ConstructionFlags.Prerequisite
			});
			if (!lastFailureFlags.Contains(ConstructionFlags.Discard))
			{
				bestCreepingNodeDefinition = creepingNodeImprovementDefinition;
				num++;
				if (num > 20)
				{
					bestCreepingNodeDefinition = null;
					Diagnostics.LogWarning("The point of interest improvement '{0}' has a loop in his upgrade hierarchy.", new object[]
					{
						bestCreepingNodeDefinition.Name
					});
					break;
				}
			}
		}
		return bestCreepingNodeDefinition;
	}

	public void RefreshCityNodesFIMSE(City city)
	{
		PointOfInterest[] pointOfInterests = city.Region.PointOfInterests;
		for (int i = 0; i < pointOfInterests.Length; i++)
		{
			if (pointOfInterests[i].CreepingNodeGUID != GameEntityGUID.Zero)
			{
				CreepingNode nodeByGUID = this.GetNodeByGUID(pointOfInterests[i].CreepingNodeGUID);
				if (nodeByGUID != null)
				{
					nodeByGUID.ReApplyFIMSEOnCreepingNode();
				}
			}
		}
	}

	public CreepingNode GetNodeByGUID(GameEntityGUID guid)
	{
		for (int i = 0; i < this.Nodes.Count; i++)
		{
			if (this.Nodes[i].GUID == guid)
			{
				return this.Nodes[i];
			}
		}
		return null;
	}

	public CreepingNode[] GetNodesUnderConstruction()
	{
		List<CreepingNode> list = new List<CreepingNode>();
		for (int i = 0; i < this.Nodes.Count; i++)
		{
			if (this.Nodes[i].IsUnderConstruction)
			{
				list.Add(this.Nodes[i]);
			}
		}
		return list.ToArray();
	}

	public void ReplacePOIImprovementsWhitCreepingNodeImprovements(City city)
	{
		Region region = city.Region;
		List<PointOfInterest> list = new List<PointOfInterest>();
		for (int i = 0; i < region.PointOfInterests.Length; i++)
		{
			PointOfInterest pointOfInterest = region.PointOfInterests[i];
			if (pointOfInterest.PointOfInterestImprovement != null)
			{
				if (pointOfInterest.Type != "Village")
				{
					list.Add(pointOfInterest);
				}
				else if (pointOfInterest.SimulationObject.Tags.Contains(Village.PacifiedVillage) && pointOfInterest.SimulationObject.Tags.Contains(Village.RebuiltVillage))
				{
					list.Add(pointOfInterest);
				}
			}
		}
		global::PlayerController playerControllerById = ((IPlayerControllerRepositoryControl)this.PlayerControllerRepositoryService).GetPlayerControllerById("server");
		if (playerControllerById != null)
		{
			OrderDestroyPointOfInterestImprovement order = new OrderDestroyPointOfInterestImprovement(city.Empire.Index, (from m in list
			where m.Type != "Village"
			select m.GUID).ToArray<GameEntityGUID>());
			playerControllerById.PostOrder(order);
			for (int j = 0; j < list.Count; j++)
			{
				PointOfInterest pointOfInterest2 = list[j];
				CreepingNodeImprovementDefinition[] values = this.creepingNodeDefinitionDatabase.GetValues();
				CreepingNodeImprovementDefinition creepingNodeImprovementDefinition = null;
				List<StaticString> list2 = new List<StaticString>();
				foreach (CreepingNodeImprovementDefinition creepingNodeImprovementDefinition2 in values)
				{
					if (creepingNodeImprovementDefinition2 != null && creepingNodeImprovementDefinition2.PointOfInterestTemplateName == pointOfInterest2.PointOfInterestDefinition.PointOfInterestTemplateName && !list2.Contains(ConstructionFlags.Discard))
					{
						creepingNodeImprovementDefinition = this.GetBestCreepingNodeDefinition(city, pointOfInterest2, creepingNodeImprovementDefinition2, list2);
						break;
					}
				}
				if (creepingNodeImprovementDefinition != null)
				{
					OrderQueueCreepingNode order2 = new OrderQueueCreepingNode(base.Empire.Index, city.GUID, pointOfInterest2.GUID, creepingNodeImprovementDefinition, pointOfInterest2.WorldPosition, true, false);
					playerControllerById.PostOrder(order2);
				}
			}
		}
	}

	public static readonly StaticString InfectedPointOfInterest = "InfectedPointOfInterest";

	public static readonly StaticString VillageInfectionComplete = "VillageInfectionComplete";

	public static readonly StaticString DismantelingStatus = "DismantelingStatus";

	private IDatabase<CreepingNodeImprovementDefinition> creepingNodeDefinitionDatabase;

	private List<CreepingNode> nodes;

	private IDatabase<SimulationDescriptor> simulationDescriptorDatabase;

	private IGameService gameService;

	private Dictionary<StaticString, DepartmentOfCreepingNodes.EventHandler> eventHandlers;

	private delegate void EventHandler(Event raisedEvent);
}
