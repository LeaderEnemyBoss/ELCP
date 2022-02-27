using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class CreepingNode : SimulationObjectWrapper, IXmlSerializable, ILineOfSightEntity, IWorldPositionable, IFastTravelNode, IFastTravelNodeGameEntity, IGameEntity, IGameEntityWithWorldPosition, IGameEntityWithEmpire, IGameEntityWithLineOfSight, ICategoryProvider
{
	public CreepingNode(GameEntityGUID guid, global::Empire empire) : base("CreepingNode#" + guid.ToString())
	{
		this.GUID = guid;
		this.Empire = empire;
		this.LineOfSightActive = true;
		this.LineOfSightDirty = true;
		this.DismantlingArmyGUID = GameEntityGUID.Zero;
		this.Life = 0f;
		this.LastTurnWhenDismantleBegun = 0;
		this.eventService = Services.GetService<IEventService>();
		Diagnostics.Assert(this.eventService != null);
		this.gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(this.gameService != null);
		this.game = (this.gameService.Game as global::Game);
		Diagnostics.Assert(this.game != null);
		this.pathfindingService = this.gameService.Game.Services.GetService<IPathfindingService>();
		Diagnostics.Assert(this.pathfindingService != null);
		this.worldPositionningService = this.gameService.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.simulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
		Diagnostics.Assert(this.simulationDescriptorDatabase != null);
		this.departmentOfTheInterior = this.Empire.GetAgency<DepartmentOfTheInterior>();
		if (ELCPUtilities.UseELCPCreepingNodeRuleset)
		{
			SimulationDescriptor descriptor = null;
			if (this.simulationDescriptorDatabase.TryGetValue("VanillaNode", out descriptor))
			{
				base.SimulationObject.AddDescriptor(descriptor);
			}
		}
		this.StoredConstructionCost = new Dictionary<string, float>();
		this.ExploitedTiles = new List<WorldPosition>();
	}

	public CreepingNode(GameEntityGUID guid, global::Empire empire, PointOfInterest poi, CreepingNodeImprovementDefinition definition) : this(guid, empire)
	{
		this.PointOfInterest = poi;
		this.NodeDefinition = definition;
		this.UpgradeReachedTurn = -1;
		this.DismantlingArmyGUID = GameEntityGUID.Zero;
		this.Life = 0f;
		this.LastTurnWhenDismantleBegun = 0;
		this.MaxLife = this.ConstructionTurns * this.NodeDefinition.GrowthPerTurn;
	}

	public event EventHandler OnUpgradeComplete;

	public event CreepingNode.DismantleStatusChangedSignature DismantleStatusChanged;

	bool ILineOfSightEntity.IgnoreFog
	{
		get
		{
			return false;
		}
	}

	int ILineOfSightEntity.EmpireInfiltrationBits
	{
		get
		{
			DepartmentOfTheInterior agency = this.Empire.GetAgency<DepartmentOfTheInterior>();
			if (agency != null && agency.MainCity != null)
			{
				return agency.MainCity.EmpireInfiltrationBits;
			}
			return 0;
		}
	}

	public int TravelAllowedBitsMask
	{
		get
		{
			if (this.Empire != null)
			{
				return this.Empire.Bits;
			}
			return 0;
		}
	}

	public WorldPosition[] GetTravelEntrancePositions()
	{
		WorldCircle worldCircle = new WorldCircle(this.WorldPosition, 1);
		WorldPosition[] worldPositions = worldCircle.GetWorldPositions(this.worldPositionningService.World.WorldParameters);
		List<WorldPosition> list = new List<WorldPosition>(worldPositions);
		for (int i = list.Count - 1; i >= 0; i--)
		{
			WorldPosition worldPosition = list[i];
			if (!worldPosition.IsValid)
			{
				list.RemoveAt(i);
			}
			else if (!this.pathfindingService.IsTileStopable(worldPosition, PathfindingMovementCapacity.Ground, (PathfindingFlags)0))
			{
				list.RemoveAt(i);
			}
			else if (this.worldPositionningService.IsWaterTile(worldPosition))
			{
				list.RemoveAt(i);
			}
		}
		list.Sort((WorldPosition left, WorldPosition right) => this.worldPositionningService.GetDistance(this.WorldPosition, left).CompareTo(this.worldPositionningService.GetDistance(this.WorldPosition, right)));
		return list.ToArray();
	}

	public WorldPosition[] GetTravelExitPositions()
	{
		WorldCircle worldCircle = new WorldCircle(this.WorldPosition, 1);
		WorldPosition[] worldPositions = worldCircle.GetWorldPositions(this.worldPositionningService.World.WorldParameters);
		List<WorldPosition> list = new List<WorldPosition>(worldPositions);
		for (int i = list.Count - 1; i >= 0; i--)
		{
			WorldPosition worldPosition = list[i];
			if (!worldPosition.IsValid)
			{
				list.RemoveAt(i);
			}
			else if (!DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(worldPosition, PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.Water))
			{
				list.RemoveAt(i);
			}
			else if (!this.pathfindingService.IsTileStopable(worldPosition, PathfindingMovementCapacity.Ground, (PathfindingFlags)0))
			{
				list.RemoveAt(i);
			}
			else
			{
				Region region = this.worldPositionningService.GetRegion(worldPosition);
				if (region != null && region.Kaiju != null && worldPosition == region.Kaiju.WorldPosition)
				{
					list.RemoveAt(i);
				}
				else if (this.worldPositionningService.IsWaterTile(worldPosition))
				{
					list.RemoveAt(i);
				}
			}
		}
		list.Sort((WorldPosition left, WorldPosition right) => this.worldPositionningService.GetDistance(this.WorldPosition, left).CompareTo(this.worldPositionningService.GetDistance(this.WorldPosition, right)));
		return list.ToArray();
	}

	public SimulationObject GetTravelNodeContext()
	{
		return this;
	}

	public override void ReadXml(XmlReader reader)
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		Diagnostics.Assert(service.Game != null);
		global::Game game = service.Game as global::Game;
		Diagnostics.Assert(game != null);
		reader.ReadVersionAttribute();
		this.GUID = reader.GetAttribute<ulong>("GUID");
		base.ReadXml(reader);
		string x = reader.ReadElementString("NodeDefinitionName");
		IDatabase<CreepingNodeImprovementDefinition> database = Databases.GetDatabase<CreepingNodeImprovementDefinition>(true);
		CreepingNodeImprovementDefinition nodeDefinition = null;
		if (database.TryGetValue(x, out nodeDefinition))
		{
			this.NodeDefinition = nodeDefinition;
		}
		int num = reader.ReadElementString<int>("IndexOfPointOfInterest");
		int num2 = reader.ReadElementString<int>("IndexOfRegion");
		this.PointOfInterest = game.World.Regions[num2].PointOfInterests[num];
		this.LastTurnChecked = reader.ReadElementString<int>("LastTurnChecked");
		this.IsUpgradeReady = reader.ReadElementString<bool>("IsUpgradeReady");
		this.UpgradeReachedTurn = reader.ReadElementString<int>("UpgradeReachedTurn");
		this.TurnsCounter = reader.ReadElementString<float>("TurnsCounter");
		this.dismantlingArmyGUID = reader.ReadElementString<ulong>("DismantlingArmyGUID");
		this.Life = reader.ReadElementString<float>("Life");
		this.MaxLife = reader.ReadElementString<float>("MaxLife");
		this.LastTurnWhenDismantleBegun = reader.ReadElementString<int>("LastTurnWhenDismantleBegun");
		this.ReadDictionnary(reader, "StoredConstructionCost", this.StoredConstructionCost);
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteVersionAttribute(1);
		writer.WriteAttributeString<ulong>("GUID", this.GUID);
		base.WriteXml(writer);
		writer.WriteElementString<StaticString>("NodeDefinitionName", this.NodeDefinition.Name);
		Region region = this.PointOfInterest.Region;
		int value = Array.IndexOf<PointOfInterest>(region.PointOfInterests, this.PointOfInterest);
		writer.WriteElementString<int>("IndexOfPointOfInterest", value);
		writer.WriteElementString<int>("IndexOfRegion", region.Index);
		writer.WriteElementString<int>("LastTurnChecked", this.LastTurnChecked);
		writer.WriteElementString<bool>("IsUpgradeReady", this.IsUpgradeReady);
		writer.WriteElementString<int>("UpgradeReachedTurn", this.UpgradeReachedTurn);
		writer.WriteElementString<float>("TurnsCounter", this.TurnsCounter);
		writer.WriteElementString<ulong>("DismantlingArmyGUID", this.dismantlingArmyGUID);
		writer.WriteElementString<float>("Life", this.Life);
		writer.WriteElementString<float>("MaxLife", this.MaxLife);
		writer.WriteElementString<int>("LastTurnWhenDismantleBegun", this.LastTurnWhenDismantleBegun);
		this.WriteDictionnary(writer, "StoredConstructionCost", this.StoredConstructionCost);
	}

	public GameEntityGUID GUID { get; private set; }

	public VisibilityController.VisibilityAccessibility VisibilityAccessibilityLevel
	{
		get
		{
			return VisibilityController.VisibilityAccessibility.Public;
		}
	}

	public int LineOfSightDetectionRange
	{
		get
		{
			return Mathf.RoundToInt(this.GetPropertyValue(SimulationProperties.DetectionRange));
		}
	}

	public int LineOfSightVisionRange
	{
		get
		{
			return Mathf.RoundToInt(this.GetPropertyValue(SimulationProperties.VisionRange));
		}
	}

	public int LineOfSightVisionHeight
	{
		get
		{
			return Mathf.RoundToInt(this.GetPropertyValue(SimulationProperties.VisionHeight));
		}
	}

	public LineOfSightData LineOfSightData { get; set; }

	public bool LineOfSightActive { get; set; }

	public bool LineOfSightDirty { get; set; }

	public StaticString Category
	{
		get
		{
			return this.NodeDefinition.Category;
		}
	}

	public StaticString SubCategory
	{
		get
		{
			return this.NodeDefinition.SubCategory;
		}
	}

	public global::Empire Empire { get; private set; }

	public CreepingNodeImprovementDefinition NodeDefinition { get; private set; }

	public PointOfInterest PointOfInterest { get; private set; }

	public WorldPosition WorldPosition
	{
		get
		{
			if (this.PointOfInterest == null)
			{
				return WorldPosition.Invalid;
			}
			return this.PointOfInterest.WorldPosition;
		}
	}

	public Region Region
	{
		get
		{
			if (this.PointOfInterest == null)
			{
				return null;
			}
			return this.PointOfInterest.Region;
		}
	}

	public int LastTurnChecked { get; set; }

	public bool IsUpgradeReady { get; private set; }

	public int UpgradeReachedTurn { get; private set; }

	public float TurnsCounter
	{
		get
		{
			return this.GetPropertyValue(SimulationProperties.CreepingNodeTurnsCounter);
		}
		private set
		{
			base.SimulationObject.GetProperty(SimulationProperties.CreepingNodeTurnsCounter).Value = value;
			this.Refresh(false);
		}
	}

	public float ConstructionTurns
	{
		get
		{
			float propertyValue = this.Empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier);
			return (float)Math.Max(0.0, Math.Ceiling((double)((float)this.NodeDefinition.ConstructionTurns * propertyValue)));
		}
	}

	public bool IsUnderConstruction
	{
		get
		{
			return this.GetPropertyValue(SimulationProperties.CreepingNodeTimesUpgraded) == 0f;
		}
	}

	public float Life { get; set; }

	public float MaxLife { get; private set; }

	public int LastTurnWhenDismantleBegun { get; set; }

	public Army DismantlingArmy
	{
		get
		{
			Army result = null;
			if (this.DismantlingArmyGUID.IsValid)
			{
				this.gameService.Game.Services.GetService<IGameEntityRepositoryService>().TryGetValue<Army>(this.DismantlingArmyGUID, out result);
			}
			return result;
		}
	}

	public GameEntityGUID DismantlingArmyGUID
	{
		get
		{
			return this.dismantlingArmyGUID;
		}
		set
		{
			if (this.dismantlingArmyGUID != value)
			{
				this.dismantlingArmyGUID = value;
				if (this.DismantleStatusChanged != null)
				{
					this.DismantleStatusChanged(value.IsValid);
				}
			}
		}
	}

	public int TurnsToDeactivate()
	{
		if (this.DismantlingArmy == null || this.DismantlingArmy.GetPropertyValue(SimulationProperties.CreepingNodeDismantlePower) <= 0f)
		{
			return -1;
		}
		return (int)Math.Max(0.0, Math.Ceiling((double)(this.Life / this.DismantlingArmy.GetPropertyValue(SimulationProperties.CreepingNodeDismantlePower))));
	}

	public float GetBuyoutCost()
	{
		float value = this.NodeDefinition.BuyoutCost.GetValue(this.Empire.SimulationObject);
		return value * this.TurnsToFinishConstruction();
	}

	public float TurnsToFinishConstruction()
	{
		return this.ConstructionTurns - this.TurnsCounter;
	}

	public bool IsConstructionPaused()
	{
		if (this.Empire != null && this.Empire.IsControlledByAI)
		{
			return false;
		}
		float propertyValue = this.departmentOfTheInterior.MainCity.GetPropertyValue(SimulationProperties.Population);
		float propertyValue2 = this.departmentOfTheInterior.MainCity.GetPropertyValue(SimulationProperties.NetCityGrowth);
		return propertyValue == 1f && propertyValue2 < 0f;
	}

	public void OnBeginTurn()
	{
		float num = this.NodeDefinition.GrowthPerTurn;
		if (this.DismantlingArmyGUID != GameEntityGUID.Zero)
		{
			num = -this.DismantlingArmy.GetPropertyValue(SimulationProperties.CreepingNodeDismantlePower);
		}
		if (this.LastTurnChecked < this.game.Turn)
		{
			bool flag = this.IsConstructionPaused();
			if (this.UpgradeReachedTurn == -1 && flag && this.DismantlingArmyGUID == GameEntityGUID.Zero)
			{
				num = 0f;
			}
			if (this.DismantlingArmyGUID == GameEntityGUID.Zero && !flag)
			{
				this.TurnsCounter += 1f;
			}
			this.Life += num;
			this.LastTurnChecked = this.game.Turn;
		}
		if (this.Life > this.MaxLife)
		{
			this.Life = this.MaxLife;
		}
		if (this.TurnsCounter >= this.ConstructionTurns && this.UpgradeReachedTurn == -1)
		{
			this.CompleteUpgrade();
		}
	}

	public void UpgradeNode(CreepingNodeImprovementDefinition upgrade)
	{
		this.RemoveImprovementDescriptors(this.NodeDefinition);
		this.RemoveConstructionCostDescriptor();
		DepartmentOfTheInterior.ClearFIMSEOnCreepingNode(this.Empire, this);
		this.NodeDefinition = upgrade;
		this.PointOfInterest.CreepingNodeImprovement = this.NodeDefinition;
		this.MaxLife = this.ConstructionTurns * this.NodeDefinition.GrowthPerTurn;
		this.AddConstructionCostDescriptor();
		if (!this.IsUnderConstruction)
		{
			this.CompleteUpgrade();
		}
	}

	public void CompleteUpgrade()
	{
		this.StoredConstructionCost.Clear();
		this.IsUpgradeReady = true;
		this.UpgradeReachedTurn = this.game.Turn;
		this.Life = this.MaxLife;
		base.SetPropertyBaseValue(SimulationProperties.CreepingNodeTimesUpgraded, this.GetPropertyValue(SimulationProperties.CreepingNodeTimesUpgraded) + 1f);
		this.RemoveConstructionCostDescriptor();
		this.AddImprovementDescriptors(this.NodeDefinition);
		DepartmentOfTheInterior.GenerateFIMSEForCreepingNode(this.Empire, this);
		this.LineOfSightDirty = true;
		this.Refresh(false);
		if (this.OnUpgradeComplete != null)
		{
			this.OnUpgradeComplete(this, new EventArgs());
		}
		this.eventService.Notify(new EventCreepingNodeUpgradeComplete(this, false));
		if (this.NodeDefinition != null)
		{
			this.eventService.Notify(new EventConstructionEnded(this.Empire, this.GUID, this.NodeDefinition));
		}
	}

	public void ReApplyImprovementDescriptors()
	{
		this.RemoveImprovementDescriptors(this.NodeDefinition);
		this.AddImprovementDescriptors(this.NodeDefinition);
	}

	public void ReApplyFIMSEOnCreepingNode()
	{
		DepartmentOfTheInterior.ClearFIMSEOnCreepingNode(this.Empire, this);
		DepartmentOfTheInterior.GenerateFIMSEForCreepingNode(this.Empire, this);
	}

	private void AddImprovementDescriptors(CreepingNodeImprovementDefinition definition)
	{
		SimulationDescriptor[] descriptors = definition.Descriptors;
		for (int i = 0; i < descriptors.Length; i++)
		{
			base.AddDescriptor(descriptors[i], false);
		}
		if (!string.IsNullOrEmpty(definition.VillageInfectionDescriptor))
		{
			SimulationDescriptor descriptor = null;
			if (this.simulationDescriptorDatabase.TryGetValue(definition.VillageInfectionDescriptor, out descriptor))
			{
				base.AddDescriptor(descriptor, false);
			}
		}
	}

	private void RemoveImprovementDescriptors(CreepingNodeImprovementDefinition definition)
	{
		SimulationDescriptor[] descriptors = definition.Descriptors;
		for (int i = 0; i < descriptors.Length; i++)
		{
			base.RemoveDescriptor(descriptors[i]);
		}
		if (!string.IsNullOrEmpty(definition.VillageInfectionDescriptor))
		{
			SimulationDescriptor descriptor = null;
			if (this.simulationDescriptorDatabase.TryGetValue(definition.VillageInfectionDescriptor, out descriptor))
			{
				base.RemoveDescriptor(descriptor);
			}
		}
	}

	private void AddConstructionCostDescriptor()
	{
		if (!string.IsNullOrEmpty(this.NodeDefinition.ConstructionCostDescriptor))
		{
			SimulationDescriptor descriptor = null;
			if (this.simulationDescriptorDatabase.TryGetValue(this.NodeDefinition.ConstructionCostDescriptor, out descriptor))
			{
				base.AddDescriptor(descriptor, false);
			}
		}
	}

	private void RemoveConstructionCostDescriptor()
	{
		if (!string.IsNullOrEmpty(this.NodeDefinition.ConstructionCostDescriptor))
		{
			SimulationDescriptor descriptor = null;
			if (this.simulationDescriptorDatabase.TryGetValue(this.NodeDefinition.ConstructionCostDescriptor, out descriptor))
			{
				base.RemoveDescriptor(descriptor);
			}
		}
	}

	public Dictionary<string, float> StoredConstructionCost { get; set; }

	private void WriteDictionnary(XmlWriter writer, string name, Dictionary<string, float> dictionary)
	{
		writer.WriteStartElement(name);
		writer.WriteAttributeString<int>("Count", dictionary.Count);
		foreach (KeyValuePair<string, float> keyValuePair in dictionary)
		{
			writer.WriteStartElement("KeyValuePair");
			writer.WriteAttributeString<string>("Key", keyValuePair.Key);
			writer.WriteAttributeString<float>("Value", keyValuePair.Value);
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
	}

	private void ReadDictionnary(XmlReader reader, string name, Dictionary<string, float> dictionary)
	{
		if (reader.IsStartElement(name))
		{
			int attribute = reader.GetAttribute<int>("Count");
			if (attribute > 0)
			{
				reader.ReadStartElement(name);
				for (int i = 0; i < attribute; i++)
				{
					string attribute2 = reader.GetAttribute<string>("Key");
					float attribute3 = reader.GetAttribute<float>("Value");
					reader.Skip();
					dictionary[attribute2] = attribute3;
				}
				reader.ReadEndElement(name);
				return;
			}
			reader.Skip();
		}
	}

	public List<WorldPosition> ExploitedTiles { get; set; }

	private IGameService gameService;

	private global::Game game;

	private GameEntityGUID dismantlingArmyGUID;

	private IEventService eventService;

	private IPathfindingService pathfindingService;

	private IWorldPositionningService worldPositionningService;

	private IDatabase<SimulationDescriptor> simulationDescriptorDatabase;

	private DepartmentOfTheInterior departmentOfTheInterior;

	public delegate void DismantleStatusChangedSignature(bool isDismantling);
}
