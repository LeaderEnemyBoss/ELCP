using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Simulation;
using Amplitude.Utilities.Maps;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class Kaiju : SimulationObjectWrapper, IXmlSerializable, IRegionalEffectsProvider<IRegionalEffectsProviderGameEntity>, IGameEntity, IGameEntityWithEmpire, IGameEntityWithWorldPosition, IRegionalEffectsProviderGameEntity, IWorldPositionable
{
	public Kaiju(KaijuEmpire kaijuEmpire, GameEntityGUID guid) : base("Kaiju#" + guid.ToString())
	{
		this.ownerEmpireIndex = -1;
		this.majorEmpire = null;
		this.NextTurnToSpawnUnit = -1;
		this.NextTurnToRecoverFromStun = -1;
		this.previousOwnerIndex = -1;
		this.previousStunnerIndex = -1;
		this.tamedTurn = -1;
		this.previousStunnedTurn = -1;
		this.kaijuEmpire = null;
		this.kaijuEmpireIndex = -1;
		this.ownerEmpireIndex = -1;
		this.region = null;
		this.kaijuArmyGUID = GameEntityGUID.Zero;
		this.KaijuEmpire = kaijuEmpire;
		this.Empire = kaijuEmpire;
		this.GUID = guid;
		this.kaijusTechService = this.GameService.Game.Services.GetService<IKaijuTechsService>();
		this.kaijusTechService.KaijuTechnologyUnlocked += this.KaijusTechService_KaijuTechnologyUnlocked;
		this.kaijuModesShareActionPoints = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<bool>("Gameplay/Kaiju/KaijuModesShareActionPoints", false);
		this.needResetUnitsActionPointsSpent = !this.kaijuModesShareActionPoints;
		this.worldOrientation = WorldOrientation.Undefined;
	}

	public Kaiju() : base("Kaiju")
	{
		this.ownerEmpireIndex = -1;
		this.majorEmpire = null;
		this.NextTurnToSpawnUnit = -1;
		this.NextTurnToRecoverFromStun = -1;
		this.empire = null;
		this.kaijuEmpire = null;
		this.region = null;
		this.kaijuArmyGUID = GameEntityGUID.Zero;
		this.previousOwnerIndex = -1;
		this.previousStunnerIndex = -1;
		this.tamedTurn = -1;
		this.previousStunnedTurn = -1;
		this.GUID = GameEntityGUID.Zero;
		this.kaijusTechService = this.GameService.Game.Services.GetService<IKaijuTechsService>();
		this.kaijusTechService.KaijuTechnologyUnlocked += this.KaijusTechService_KaijuTechnologyUnlocked;
		this.kaijuModesShareActionPoints = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<bool>("Gameplay/Kaiju/KaijuModesShareActionPoints", false);
		this.needResetUnitsActionPointsSpent = !this.kaijuModesShareActionPoints;
	}

	public event Action<IRegionalEffectsProviderGameEntity> RefreshProvidedRegionEffects;

	public event Kaiju.ConvertedSignature OnPrepareToConvertToArmyDelegate;

	public event Kaiju.ConvertedSignature OnPrepareToConvertToGarrisonDelegate;

	public event Kaiju.ConvertedSignature OnConvertedToArmyDelegate;

	public event Kaiju.ConvertedSignature OnConvertedToGarrisonDelegate;

	public event Kaiju.RegionChangedSignature OnRegionChanged;

	public event Kaiju.OwnerChangedSignature OnOwnerChanged;

	public event EventHandler<KaijuWorldPositionChangeEvent> WorldPositionChange;

	public SimulationObjectWrapper GetRegionalEffectsProviderContext()
	{
		return this;
	}

	public void CallRefreshProvidedRegionEffects()
	{
		if (this.RefreshProvidedRegionEffects != null)
		{
			this.RefreshProvidedRegionEffects(this);
		}
	}

	public override void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		this.GUID = reader.GetAttribute<ulong>("GUID");
		base.ReadXml(reader);
		int num2 = reader.ReadElementString<int>("OwnerIndex");
		this.ownerEmpireIndex = num2;
		this.kaijuEmpireIndex = reader.ReadElementString<int>("KaijuEmpireIndex");
		ulong attribute = reader.GetAttribute<ulong>("GUID");
		KaijuGarrison kaijuGarrison = new KaijuGarrison(attribute)
		{
			Empire = this.Empire,
			Kaiju = this
		};
		reader.ReadElementSerializable<KaijuGarrison>(ref kaijuGarrison);
		if (kaijuGarrison != null)
		{
			this.SetGarrison(kaijuGarrison);
		}
		this.kaijuArmyGUID = reader.ReadElementString<ulong>("KaijuArmyGUID");
		this.NextTurnToSpawnUnit = reader.ReadElementString<int>("nextTurnToSpawnUnits");
		this.NextTurnToRecoverFromStun = reader.ReadElementString<int>("nextTurnToRecoveryFromStun");
		if (num > 1)
		{
			this.needResetUnitsActionPointsSpent = reader.ReadElementString<bool>("needResetUnitsActionPointsSpent");
		}
		if (num > 2)
		{
			this.PreviousOwnerIndex = reader.ReadElementString<int>("PreviousOwnerIndex");
			this.TamedTurn = reader.ReadElementString<int>("TamedTurn");
			this.PreviousStunnerIndex = reader.ReadElementString<int>("PreviousStunnerIndex");
			this.PreviousStunnedTurn = reader.ReadElementString<int>("PreviousStunnedTurn");
		}
		if (num > 3)
		{
			int num3 = reader.ReadElementString<int>("WorldOrientation");
			this.WorldOrientation = (WorldOrientation)num3;
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteVersionAttribute(4);
		writer.WriteAttributeString<ulong>("GUID", this.GUID);
		base.WriteXml(writer);
		int value = (this.majorEmpire == null) ? -1 : this.majorEmpire.Index;
		writer.WriteElementString<int>("OwnerIndex", value);
		writer.WriteElementString<int>("KaijuEmpireIndex", this.kaijuEmpireIndex);
		IXmlSerializable kaijuGarrison = this.KaijuGarrison;
		writer.WriteElementSerializable<IXmlSerializable>(ref kaijuGarrison);
		writer.WriteElementString<ulong>("KaijuArmyGUID", this.KaijuArmy.GUID);
		writer.WriteElementString<int>("nextTurnToSpawnUnits", this.NextTurnToSpawnUnit);
		writer.WriteElementString<int>("nextTurnToRecoveryFromStun", this.NextTurnToRecoverFromStun);
		writer.WriteElementString<bool>("needResetUnitsActionPointsSpent", this.needResetUnitsActionPointsSpent);
		writer.WriteElementString<int>("PreviousOwnerIndex", this.PreviousOwnerIndex);
		writer.WriteElementString<int>("TamedTurn", this.TamedTurn);
		writer.WriteElementString<int>("PreviousStunnerIndex", this.PreviousStunnerIndex);
		writer.WriteElementString<int>("PreviousStunnedTurn", this.PreviousStunnedTurn);
		writer.WriteElementString<int>("WorldOrientation", (int)this.WorldOrientation);
	}

	public GameEntityGUID GUID { get; private set; }

	public int OwnerEmpireIndex
	{
		get
		{
			return this.ownerEmpireIndex;
		}
	}

	public KaijuGarrison KaijuGarrison { get; private set; }

	public KaijuArmy KaijuArmy { get; private set; }

	public global::Empire Empire
	{
		get
		{
			if (this.majorEmpire != null)
			{
				return this.majorEmpire;
			}
			if (this.kaijuEmpire != null)
			{
				return this.kaijuEmpire;
			}
			return this.empire;
		}
		set
		{
			this.empire = value;
		}
	}

	public MajorEmpire MajorEmpire
	{
		get
		{
			return this.majorEmpire;
		}
		set
		{
			global::Empire lastOwner = this.majorEmpire;
			this.majorEmpire = value;
			this.ownerEmpireIndex = ((this.majorEmpire == null) ? -1 : this.majorEmpire.Index);
			if (this.OnOwnerChanged != null)
			{
				this.OnOwnerChanged(lastOwner, this.majorEmpire);
			}
		}
	}

	public KaijuEmpire KaijuEmpire
	{
		get
		{
			return this.kaijuEmpire;
		}
		set
		{
			this.kaijuEmpire = value;
			this.kaijuEmpireIndex = this.kaijuEmpire.Index;
		}
	}

	public int KaijuEmpireIndex
	{
		get
		{
			return this.kaijuEmpireIndex;
		}
	}

	public Region Region { get; set; }

	public WorldPosition WorldPosition
	{
		get
		{
			if (this.OnGarrisonMode())
			{
				return this.KaijuGarrison.WorldPosition;
			}
			if (this.OnArmyMode())
			{
				return this.KaijuArmy.WorldPosition;
			}
			return WorldPosition.Invalid;
		}
	}

	public WorldOrientation WorldOrientation
	{
		get
		{
			if (this.worldOrientation == WorldOrientation.Undefined)
			{
				this.worldOrientation = WorldOrientation.SouthEast;
			}
			return this.worldOrientation;
		}
		private set
		{
			this.worldOrientation = value;
		}
	}

	public int NextTurnToSpawnUnit { get; private set; }

	public int NextTurnToRecoverFromStun { get; private set; }

	public bool NeedResetUnitsActionPointsSpent
	{
		get
		{
			return this.needResetUnitsActionPointsSpent;
		}
		private set
		{
			this.needResetUnitsActionPointsSpent = value;
		}
	}

	public int PreviousOwnerIndex
	{
		get
		{
			if (this.previousOwnerIndex == -1)
			{
				this.previousOwnerIndex = this.ownerEmpireIndex;
			}
			return this.previousOwnerIndex;
		}
		private set
		{
			this.previousOwnerIndex = value;
		}
	}

	public int TamedTurn
	{
		get
		{
			if (this.tamedTurn == -1)
			{
				this.tamedTurn = (this.GameService.Game as global::Game).Turn;
			}
			return this.tamedTurn;
		}
		private set
		{
			this.tamedTurn = value;
		}
	}

	public int PreviousStunnerIndex
	{
		get
		{
			if (this.previousStunnerIndex == -1)
			{
				this.previousStunnerIndex = this.ownerEmpireIndex;
			}
			return this.previousStunnerIndex;
		}
		private set
		{
			this.previousStunnerIndex = value;
		}
	}

	public int PreviousStunnedTurn
	{
		get
		{
			if (this.previousStunnedTurn == -1)
			{
				this.previousStunnedTurn = (this.GameService.Game as global::Game).Turn;
			}
			return this.previousStunnedTurn;
		}
		private set
		{
			this.previousStunnedTurn = value;
		}
	}

	private IGameService GameService
	{
		get
		{
			if (this.gameService == null)
			{
				this.GameService = Services.GetService<IGameService>();
			}
			return this.gameService;
		}
		set
		{
			this.gameService = value;
		}
	}

	private IGameEntityRepositoryService GameEntityRepositoryService
	{
		get
		{
			if (this.gameEntityRepositoryService == null)
			{
				this.GameEntityRepositoryService = this.GameService.Game.Services.GetService<IGameEntityRepositoryService>();
			}
			return this.gameEntityRepositoryService;
		}
		set
		{
			this.gameEntityRepositoryService = value;
		}
	}

	private IDatabase<SimulationDescriptor> SimulationDescriptorDatabase
	{
		get
		{
			if (this.simulationDescriptorDatabase == null)
			{
				this.SimulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
			}
			return this.simulationDescriptorDatabase;
		}
		set
		{
			this.simulationDescriptorDatabase = value;
		}
	}

	private IEventService EventService
	{
		get
		{
			if (this.eventService == null)
			{
				this.EventService = Services.GetService<IEventService>();
			}
			return this.eventService;
		}
		set
		{
			this.eventService = value;
		}
	}

	public IEnumerator OnLoadGame(Amplitude.Unity.Game.Game game)
	{
		IGameEntityRepositoryService gameEntityRepository = game.Services.GetService<IGameEntityRepositoryService>();
		if (this.kaijuArmyGUID != GameEntityGUID.Zero)
		{
			DepartmentOfDefense departmentOfDefense = this.Empire.GetAgency<DepartmentOfDefense>();
			Army army = departmentOfDefense.GetArmy(this.kaijuArmyGUID);
			if (army != null && army is KaijuArmy)
			{
				KaijuArmy kaijuArmy = army as KaijuArmy;
				if (kaijuArmy != null)
				{
					this.SetArmy(kaijuArmy);
				}
			}
		}
		if (this.KaijuGarrison != null)
		{
			if (this.OnGarrisonMode())
			{
				IWorldPositionningService worldPositionningService = game.Services.GetService<IWorldPositionningService>();
				Region region = worldPositionningService.GetRegion(this.KaijuGarrison.WorldPosition);
				this.Region = region;
				this.Region.KaijuGarrisonGUID = this.KaijuGarrison.GUID;
				this.Region.KaijuEmpire = this.KaijuGarrison.KaijuEmpire;
			}
		}
		else
		{
			Diagnostics.LogError("KaijuGarrison was not loaded properly.");
		}
		if (this.OnGarrisonMode())
		{
			gameEntityRepository.Register(this.KaijuGarrison);
			gameEntityRepository.Unregister(this.KaijuArmy);
			this.HideKaijuArmyFromMap();
			this.Empire.RemoveChild(this.KaijuArmy);
		}
		else if (this.OnArmyMode())
		{
			gameEntityRepository.Register(this.KaijuArmy);
			gameEntityRepository.Unregister(this.KaijuGarrison);
			this.ShowKaijuArmyInMap();
		}
		this.RegisterTroops();
		this.ComputeNextTurnToRecoverFromStun();
		this.ComputeNextTurnToSpawnUnits(false);
		this.KaijuArmy.ForceWorldOrientation(this.WorldOrientation);
		this.KaijuGarrison.ForceWorldOrientation(this.WorldOrientation);
		yield break;
	}

	public void GameClientState_Turn_Begin()
	{
		this.Client_TurnBegin_UpdateUnitDesigns();
		this.Client_TurnBegin_SpawnLiceUnit();
	}

	public void GameClientState_Turn_Ended()
	{
		this.Client_TurnEnded_RegenUnits();
		this.Client_TurnEnded_UpdateUnits();
	}

	public void GameServerState_Turn_Begin()
	{
	}

	public void GameServerState_Turn_End()
	{
	}

	public void SetGarrison(KaijuGarrison kaijuGarrison)
	{
		kaijuGarrison.Empire = this.Empire;
		kaijuGarrison.Kaiju = this;
		this.KaijuGarrison = kaijuGarrison;
		this.KaijuGarrison.StandardUnitCollectionChange += this.KaijuGarrison_StandardUnitCollectionChange;
		if (!this.OnArmyMode())
		{
			IWorldPositionningService service = this.GameService.Game.Services.GetService<IWorldPositionningService>();
			Region region = service.GetRegion(this.KaijuGarrison.WorldPosition);
			this.Region = region;
			this.Region.KaijuEmpire = this.KaijuEmpire;
			this.Region.KaijuGarrisonGUID = this.KaijuGarrison.GUID;
			this.Empire.AddChild(kaijuGarrison);
		}
	}

	public void SetArmy(KaijuArmy kaijuArmy)
	{
		this.KaijuArmy = kaijuArmy;
		this.KaijuArmy.Kaiju = this;
		this.kaijuArmyGUID = kaijuArmy.GUID;
		DepartmentOfDefense agency = this.Empire.GetAgency<DepartmentOfDefense>();
		agency.AddArmy(this.KaijuArmy);
		this.KaijuArmy.StandardUnitCollectionChange += this.KaijuArmy_StandardUnitCollectionChange;
	}

	public void SetOwner(MajorEmpire ownerEmpire)
	{
		this.MajorEmpire = ownerEmpire;
		this.KaijuArmy.Empire = ownerEmpire;
		this.KaijuGarrison.Empire = ownerEmpire;
		this.tamedTurn = (this.GameService.Game as global::Game).Turn;
	}

	public void RemoveOwner(int instigatorEmpireIndex = -1)
	{
		MajorEmpire majorEmpire = this.MajorEmpire;
		if (majorEmpire != null)
		{
			int takenFromEmpire = this.PreviousOwnerIndex;
			this.previousOwnerIndex = majorEmpire.Index;
			majorEmpire.RemoveTamedKaiju(this);
			this.ReleaseKaijuArmyActions();
			this.MajorEmpire = null;
			this.KaijuArmy.Empire = this.KaijuEmpire;
			this.KaijuGarrison.Empire = this.KaijuEmpire;
			EventKaijuLost.KaijuLostReason kaijuLostReason = EventKaijuLost.KaijuLostReason.FREE;
			if (instigatorEmpireIndex != -1)
			{
				kaijuLostReason = EventKaijuLost.KaijuLostReason.STUN;
				this.previousStunnerIndex = instigatorEmpireIndex;
				this.previousStunnedTurn = (this.GameService.Game as global::Game).Turn;
			}
			this.EventService.Notify(new EventKaijuLost(majorEmpire, this, instigatorEmpireIndex, kaijuLostReason, takenFromEmpire));
		}
	}

	public void ChangeToWildState()
	{
		if (this.IsStunned())
		{
			SimulationDescriptor value = this.SimulationDescriptorDatabase.GetValue(Kaiju.KaijuStatusStunned);
			base.RemoveDescriptor(value);
		}
		else if (this.IsTamed())
		{
			SimulationDescriptor value2 = this.SimulationDescriptorDatabase.GetValue(Kaiju.KaijuStatusTamed);
			base.RemoveDescriptor(value2);
		}
		SimulationDescriptor value3 = this.SimulationDescriptorDatabase.GetValue(Kaiju.KaijuStatusWild);
		base.AddDescriptor(value3, false);
		this.Refresh(false);
		this.KaijuGarrison.Refresh(false);
		this.KaijuArmy.Refresh(false);
		this.ComputeNextTurnToSpawnUnits(false);
		this.KaijuGarrison.CallRefreshAppliedRegionEffects();
		EventKaijuReturnToWild eventToNotify = new EventKaijuReturnToWild(this.Empire, this);
		this.EventService.Notify(eventToNotify);
	}

	public void ChangeToStunState(global::Empire stunner)
	{
		if (this.IsWild())
		{
			SimulationDescriptor value = this.SimulationDescriptorDatabase.GetValue(Kaiju.KaijuStatusWild);
			base.RemoveDescriptor(value);
		}
		else if (this.IsTamed())
		{
			SimulationDescriptor value2 = this.SimulationDescriptorDatabase.GetValue(Kaiju.KaijuStatusTamed);
			base.RemoveDescriptor(value2);
		}
		SimulationDescriptor value3 = this.SimulationDescriptorDatabase.GetValue(Kaiju.KaijuStatusStunned);
		base.AddDescriptor(value3, false);
		this.Refresh(false);
		this.ComputeNextTurnToRecoverFromStun();
		this.ComputeNextTurnToSpawnUnits(false);
		this.KaijuGarrison.CallRefreshAppliedRegionEffects();
		this.KaijuArmy.OnStunned();
		EventKaijuStunned eventToNotify = new EventKaijuStunned(stunner, this);
		this.EventService.Notify(eventToNotify);
	}

	public void ChangeToTamedState(MajorEmpire majorEmpire)
	{
		if (this.IsWild())
		{
			SimulationDescriptor value = this.SimulationDescriptorDatabase.GetValue(Kaiju.KaijuStatusWild);
			base.RemoveDescriptor(value);
		}
		else if (this.IsStunned())
		{
			SimulationDescriptor value2 = this.SimulationDescriptorDatabase.GetValue(Kaiju.KaijuStatusStunned);
			base.RemoveDescriptor(value2);
		}
		SimulationDescriptor value3 = this.SimulationDescriptorDatabase.GetValue(Kaiju.KaijuStatusTamed);
		base.AddDescriptor(value3, false);
		this.Refresh(false);
		this.ComputeNextTurnToSpawnUnits(false);
		this.KaijuGarrison.CallRefreshAppliedRegionEffects();
		this.KaijuArmy.OnTamed();
		EventKaijuTamed eventToNotify = new EventKaijuTamed(this.majorEmpire, this);
		this.EventService.Notify(eventToNotify);
		foreach (global::Empire empire in (this.gameService.Game as global::Game).Empires)
		{
			if (empire is MajorEmpire && empire != this.majorEmpire)
			{
				this.eventService.Notify(new GlobalEventKaijuTamed(empire, this));
			}
		}
	}

	public bool CanChangeToArmyMode()
	{
		return !this.OnArmyMode() && !this.IsStunned();
	}

	public bool CanChangeToGarrisonMode()
	{
		if (this.OnGarrisonMode())
		{
			return false;
		}
		if (this.IsStunned())
		{
			return false;
		}
		IWorldPositionningService service = this.GameService.Game.Services.GetService<IWorldPositionningService>();
		Region region = service.GetRegion(this.KaijuArmy.WorldPosition);
		return region == null || region.Owner == null;
	}

	public void ChangeToGarrisonMode(bool calledByChangeModeOrderProcessor = false)
	{
		if (calledByChangeModeOrderProcessor && this.OnPrepareToConvertToGarrisonDelegate != null)
		{
			this.OnPrepareToConvertToGarrisonDelegate();
		}
		IWorldPositionningService service = this.GameService.Game.Services.GetService<IWorldPositionningService>();
		Region region = service.GetRegion(this.KaijuArmy.WorldPosition);
		SimulationDescriptor value = this.SimulationDescriptorDatabase.GetValue(Kaiju.KaijuArmyModeDescriptor);
		if (value != null)
		{
			base.RemoveDescriptor(value);
		}
		SimulationDescriptor value2 = this.SimulationDescriptorDatabase.GetValue(Kaiju.KaijuGarrisonModeDescriptor);
		if (value2 != null)
		{
			base.AddDescriptor(value2, false);
		}
		this.Refresh(false);
		this.KaijuArmy.Refresh(false);
		this.KaijuGarrison.Refresh(false);
		this.KaijuGarrison.Empire = this.Empire;
		this.KaijuGarrison.ClearAllUnits();
		this.TransferUnitsToGarrison();
		this.GameEntityRepositoryService.Unregister(this.KaijuArmy);
		this.Empire.RemoveChild(this.KaijuArmy);
		this.KaijuGarrison.MoveTo(this.KaijuArmy.WorldPosition);
		this.GameEntityRepositoryService.Register(this.KaijuGarrison);
		this.Empire.AddChild(this.KaijuGarrison);
		this.Empire.Refresh(false);
		this.OwnRegion(region);
		this.KaijuArmy.Refresh(false);
		this.KaijuGarrison.Refresh(false);
		this.HideKaijuArmyFromMap();
		this.KaijuGarrison.OnConvertedToGarrison();
		this.KaijuArmy.OnConvertedToGarrison();
		if (this.OnConvertedToGarrisonDelegate != null)
		{
			this.OnConvertedToGarrisonDelegate();
		}
		this.ComputeNextTurnToSpawnUnits(false);
		this.CallRefreshProvidedRegionEffects();
		this.ReleaseKaijuArmyActions();
		this.KaijuGarrison.ForceWorldOrientation(this.WorldOrientation);
	}

	public void ChangeToArmyMode(bool calledByChangeModeOrderProcessor = false)
	{
		if (calledByChangeModeOrderProcessor && this.OnPrepareToConvertToArmyDelegate != null)
		{
			this.OnPrepareToConvertToArmyDelegate();
		}
		WorldPosition worldPosition = this.KaijuGarrison.WorldPosition;
		this.LeaveCurrentRegion();
		SimulationDescriptor value = this.SimulationDescriptorDatabase.GetValue(Kaiju.KaijuGarrisonModeDescriptor);
		if (value != null)
		{
			base.RemoveDescriptor(value);
		}
		SimulationDescriptor value2 = this.SimulationDescriptorDatabase.GetValue(Kaiju.KaijuArmyModeDescriptor);
		if (value2 != null)
		{
			base.AddDescriptor(value2, false);
		}
		this.Refresh(false);
		this.KaijuArmy.ClearAllUnits();
		this.TransferUnitsToArmy();
		this.KaijuArmy.Refresh(false);
		this.KaijuGarrison.Refresh(false);
		this.GameEntityRepositoryService.Unregister(this.KaijuGarrison);
		this.Empire.RemoveChild(this.KaijuGarrison);
		this.Empire.Refresh(false);
		this.KaijuArmy.SetWorldPosition(WorldPosition.Invalid);
		this.KaijuArmy.MoveTo(worldPosition);
		this.GameEntityRepositoryService.Register(this.KaijuArmy);
		this.Empire.AddChild(this.KaijuArmy);
		this.KaijuArmy.Empire = this.Empire;
		this.KaijuArmy.Refresh(false);
		this.KaijuGarrison.Refresh(false);
		this.KaijuArmy.Refresh(false);
		this.ShowKaijuArmyInMap();
		this.KaijuGarrison.OnConvertedToArmy();
		this.KaijuArmy.OnConvertedToArmy();
		this.ComputeNextTurnToSpawnUnits(false);
		if (this.OnConvertedToArmyDelegate != null)
		{
			this.OnConvertedToArmyDelegate();
		}
		this.CallRefreshProvidedRegionEffects();
		this.KaijuArmy.ForceWorldOrientation(this.WorldOrientation);
	}

	public Garrison GetActiveTroops()
	{
		if (this.OnArmyMode())
		{
			return this.KaijuArmy;
		}
		if (this.OnGarrisonMode())
		{
			return this.KaijuGarrison;
		}
		return null;
	}

	public bool IsWild()
	{
		return base.SimulationObject.Tags.Contains(Kaiju.KaijuStatusWild);
	}

	public bool IsTamed()
	{
		return base.SimulationObject.Tags.Contains(Kaiju.KaijuStatusTamed);
	}

	public bool IsStunned()
	{
		return base.SimulationObject.Tags.Contains(Kaiju.KaijuStatusStunned);
	}

	public bool OnArmyMode()
	{
		return base.SimulationObject.Tags.Contains(Kaiju.KaijuArmyModeDescriptor);
	}

	public bool OnGarrisonMode()
	{
		return base.SimulationObject.Tags.Contains(Kaiju.KaijuGarrisonModeDescriptor);
	}

	public void OwnRegion(Region region)
	{
		this.Region = region;
		this.Region.OnKaijuMovingIn(this);
		this.kaijuEmpire.Region = region;
		if (this.OnRegionChanged != null)
		{
			this.OnRegionChanged(this.Region);
		}
		this.CallRefreshProvidedRegionEffects();
		DepartmentOfTheInterior.GenerateResourcesLeechingForTamedKaijus(this);
	}

	public void RegisterTroops()
	{
		IGameService service = Services.GetService<IGameService>();
		IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
		if (this.KaijuArmy != null)
		{
			foreach (Unit unit in this.KaijuArmy.Units)
			{
				if (!service2.Contains(unit.GUID))
				{
					service2.Register(unit);
				}
			}
		}
		if (this.KaijuGarrison != null)
		{
			foreach (Unit unit2 in this.KaijuGarrison.Units)
			{
				if (!service2.Contains(unit2.GUID))
				{
					service2.Register(unit2);
				}
			}
		}
	}

	public void LeaveCurrentRegion()
	{
		if (this.Region != null)
		{
			this.Region.OnKaijuMovingOut(this);
			this.Region = null;
			if (this.OnRegionChanged != null)
			{
				this.OnRegionChanged(this.Region);
			}
		}
		this.kaijuEmpire.Region = null;
		this.CallRefreshProvidedRegionEffects();
		DepartmentOfTheInterior.ClearResourcesLeechingForKaijus(this);
	}

	public void MoveToRegion(WorldPosition position)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service == null)
		{
			Diagnostics.LogError("Cannot retreive the gameService.");
			return;
		}
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		if (service2 == null)
		{
			Diagnostics.LogError("Cannot retreive the worldPositionningService.");
			return;
		}
		Region region = service2.GetRegion(position);
		if (region == null)
		{
			Diagnostics.LogError("Cannot retreive the region.");
			return;
		}
		this.MoveTo(position);
		if (this.OnArmyMode())
		{
			this.ChangeToGarrisonMode(false);
		}
		else if (this.OnGarrisonMode())
		{
			this.LeaveCurrentRegion();
			this.OwnRegion(region);
		}
	}

	public bool CanSpawnMilitiaUnit()
	{
		if (this.OnArmyMode())
		{
			return this.KaijuArmy.UnitsCount < this.KaijuArmy.MaximumUnitSlot;
		}
		return this.OnGarrisonMode() && this.KaijuGarrison.UnitsCount < this.KaijuGarrison.MaximumUnitSlot;
	}

	public override void Refresh(bool silent = false)
	{
		base.Refresh(silent);
		if (this.KaijuGarrison != null && this.KaijuGarrison.SimulationObject != null)
		{
			this.KaijuGarrison.Refresh(silent);
		}
		if (this.KaijuArmy != null && this.KaijuArmy.SimulationObject != null)
		{
			this.KaijuArmy.Refresh(silent);
		}
	}

	public void ClearAllUnits()
	{
		if (this.KaijuGarrison != null)
		{
			this.KaijuGarrison.ClearAllUnits();
		}
		if (this.KaijuArmy != null)
		{
			this.KaijuArmy.ClearAllUnits();
		}
	}

	public void ClearMilitias()
	{
		if (this.KaijuGarrison != null)
		{
			this.KaijuGarrison.ClearMilitias();
		}
		if (this.KaijuArmy != null)
		{
			this.KaijuArmy.ClearMilitias();
		}
	}

	public virtual void Release()
	{
		if (this.KaijuGarrison != null)
		{
			this.KaijuGarrison.StandardUnitCollectionChange -= this.KaijuGarrison_StandardUnitCollectionChange;
			this.KaijuGarrison.Release();
		}
		if (this.KaijuArmy != null)
		{
			this.KaijuArmy.StandardUnitCollectionChange -= this.KaijuArmy_StandardUnitCollectionChange;
			this.KaijuArmy.Release();
		}
		this.KaijuGarrison = null;
		this.KaijuArmy = null;
		if (this.kaijusTechService != null)
		{
			this.kaijusTechService.KaijuTechnologyUnlocked -= this.KaijusTechService_KaijuTechnologyUnlocked;
		}
		this.armiesMap = null;
	}

	public void OnArmyWorldUnitCreated(WorldUnit worldUnit)
	{
		WorldPawn worldMonsterPawn = this.GetWorldMonsterPawn(worldUnit);
		if (worldMonsterPawn != null)
		{
			worldMonsterPawn.WorldPawnFinalWorldOrientationChanged += this.WorldPawn_OnFinalWorldOrientationChanged;
		}
	}

	public void OnArmyWorldUnitBeginAutoKillDelayPeriod(WorldUnit worldUnit)
	{
		WorldPawn worldMonsterPawn = this.GetWorldMonsterPawn(worldUnit);
		if (worldMonsterPawn != null)
		{
			worldMonsterPawn.WorldPawnFinalWorldOrientationChanged -= this.WorldPawn_OnFinalWorldOrientationChanged;
		}
	}

	private void KaijusTechService_KaijuTechnologyUnlocked(object sender, ConstructibleElementEventArgs e)
	{
		this.CallRefreshProvidedRegionEffects();
		this.Refresh(false);
		this.ComputeNextTurnToSpawnUnits(false);
		this.ComputeNextTurnToRecoverFromStun();
	}

	public void ComputeNextTurnToSpawnUnits(bool resetPreviousComputedTurn = false)
	{
		if (resetPreviousComputedTurn || this.NextTurnToSpawnUnit <= -1 || this.NextTurnToSpawnUnit < Mathf.FloorToInt((float)(this.GameService.Game as global::Game).Turn))
		{
			this.NextTurnToSpawnUnit = -1;
			float num = (float)Mathf.CeilToInt(this.GetPropertyValue(SimulationProperties.KaijuUnitProductionTimer));
			if (num > 0f)
			{
				this.NextTurnToSpawnUnit = Mathf.FloorToInt((float)(this.GameService.Game as global::Game).Turn + num * this.Empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier));
			}
			base.SimulationObject.SetPropertyBaseValue(SimulationProperties.KaijuNextTurnToSpawnUnit, (float)this.NextTurnToSpawnUnit);
			this.Refresh(false);
		}
	}

	public void ComputeNextTurnToRecoverFromStun()
	{
		this.NextTurnToRecoverFromStun = -1;
		float num = (float)Mathf.CeilToInt(this.GetPropertyValue(SimulationProperties.KaijuStunTimer));
		if (num > 0f)
		{
			this.NextTurnToRecoverFromStun = Mathf.FloorToInt((float)(this.GameService.Game as global::Game).Turn + num * this.Empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier));
		}
		base.SimulationObject.SetPropertyBaseValue(SimulationProperties.KaijuNextTurnToRecoverFromStun, (float)this.NextTurnToRecoverFromStun);
		this.Refresh(false);
	}

	public int GetRemainingTurnBeforeUnitSpawn()
	{
		int num = this.NextTurnToSpawnUnit - (this.GameService.Game as global::Game).Turn;
		if (num == 0)
		{
			return this.GetUnitProductionTimer();
		}
		return num;
	}

	public int GetRemainingTurnBeforeStunFinish()
	{
		if (!this.IsStunned())
		{
			return -1;
		}
		int num = this.NextTurnToRecoverFromStun - (this.GameService.Game as global::Game).Turn;
		if (num == 0)
		{
			return this.GetStunTimer();
		}
		return num;
	}

	private void KaijuGarrison_StandardUnitCollectionChange(object sender, CollectionChangeEventArgs e)
	{
		if (this.KaijuGarrison != null)
		{
			this.ComputeNextTurnToSpawnUnits(false);
		}
	}

	private void KaijuArmy_StandardUnitCollectionChange(object sender, CollectionChangeEventArgs e)
	{
		if (this.KaijuArmy != null)
		{
			this.ComputeNextTurnToSpawnUnits(false);
		}
	}

	public void AddUnit(Unit unit)
	{
		if (this.OnGarrisonMode())
		{
			this.KaijuGarrison.AddUnit(unit);
		}
		else if (this.OnArmyMode())
		{
			this.KaijuArmy.AddUnit(unit);
		}
		this.gameEntityRepositoryService.Register(unit);
	}

	public void RefreshSharedSight()
	{
		((ISharedSightEntity)this.GetActiveTroops()).SharedSightDirty = true;
	}

	private int GetUnitProductionTimer()
	{
		return Mathf.FloorToInt(base.SimulationObject.GetPropertyValue(SimulationProperties.KaijuUnitProductionTimer) * this.Empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier));
	}

	private int GetStunTimer()
	{
		return Mathf.FloorToInt(base.SimulationObject.GetPropertyValue(SimulationProperties.KaijuStunTimer) * this.Empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier));
	}

	private void Client_TurnBegin_SpawnLiceUnit()
	{
		int turn = (this.GameService.Game as global::Game).Turn;
		int maxEraNumber = DepartmentOfScience.GetMaxEraNumber();
		if (this.CanSpawnMilitiaUnit() && this.NextTurnToSpawnUnit > 0 && this.NextTurnToSpawnUnit == turn)
		{
			GameEntityGUID guid = this.KaijuGarrison.GUID;
			StaticString name = this.KaijuEmpire.FindLiceDesign(true).Name;
			if (this.OnArmyMode() && this.KaijuArmy != null)
			{
				guid = this.KaijuArmy.GUID;
			}
			global::PlayerController server = this.Empire.PlayerControllers.Server;
			if (server != null)
			{
				OrderSpawnUnit order = new OrderSpawnUnit(this.Empire.Index, maxEraNumber, false, name, guid, false);
				server.PostOrder(order);
			}
			this.ComputeNextTurnToSpawnUnits(true);
		}
	}

	private void Client_TurnBegin_UpdateUnitDesigns()
	{
		Garrison activeTroops = this.GetActiveTroops();
		UnitDesign unitDesign = this.KaijuEmpire.FindMonsterDesign(true);
		UnitDesign newUnitDesign = this.KaijuEmpire.FindLiceDesign(true);
		foreach (Unit unit in activeTroops.Units)
		{
			if (unit.UnitDesign.Tags.Contains(Kaiju.MonsterUnitTag) && unit.UnitDesign.Model != unitDesign.Model)
			{
				unit.RetrofitTo(unitDesign);
			}
			else if (unit.UnitDesign.Tags.Contains(Kaiju.LiceUnitTag) && unit.UnitDesign.Model != unitDesign.Model)
			{
				unit.RetrofitTo(newUnitDesign);
			}
		}
	}

	private void Client_TurnEnded_RegenUnits()
	{
		if (this.OnArmyMode())
		{
			return;
		}
		Garrison activeTroops = this.GetActiveTroops();
		float regenModifier = this.Empire.GetPropertyValue(SimulationProperties.InOwnedRegionUnitRegenModifier) + activeTroops.GetPropertyValue(SimulationProperties.InGarrisonRegenModifier);
		foreach (Unit unit in activeTroops.Units)
		{
			DepartmentOfDefense.RegenUnit(unit, regenModifier, 0);
			unit.Refresh(false);
		}
		DepartmentOfDefense agency = this.Empire.GetAgency<DepartmentOfDefense>();
		agency.CleanGarrisonAfterEncounter(activeTroops);
		this.Refresh(false);
	}

	private void Client_TurnEnded_UpdateUnits()
	{
		this.ResetActiveGarrisonUnitProperties();
		this.NeedResetUnitsActionPointsSpent = !this.kaijuModesShareActionPoints;
	}

	private void ResetActiveGarrisonUnitProperties()
	{
		Garrison activeTroops = this.GetActiveTroops();
		foreach (Unit unit in activeTroops.Units)
		{
			unit.UpdateExperienceReward(this.Empire);
			unit.SetPropertyBaseValue(SimulationProperties.ActionPointsSpent, 0f);
			unit.SetPropertyBaseValue(SimulationProperties.MovementRatio, 1f);
		}
		this.Refresh(false);
	}

	private void TransferUnitsToGarrison()
	{
		if (this.KaijuArmy.UnitsCount <= 0)
		{
			return;
		}
		List<Unit> list = new List<Unit>(this.KaijuArmy.Units);
		foreach (Unit unit in list)
		{
			this.KaijuArmy.RemoveUnit(unit);
			this.KaijuGarrison.AddUnit(unit);
		}
		if (this.NeedResetUnitsActionPointsSpent)
		{
			this.NeedResetUnitsActionPointsSpent = false;
			this.ResetActiveGarrisonUnitProperties();
		}
	}

	private void TransferUnitsToArmy()
	{
		if (this.KaijuGarrison.UnitsCount <= 0)
		{
			return;
		}
		List<Unit> list = new List<Unit>(this.KaijuGarrison.Units);
		foreach (Unit unit in list)
		{
			this.KaijuGarrison.RemoveUnit(unit);
			this.KaijuArmy.AddUnit(unit);
		}
		if (this.NeedResetUnitsActionPointsSpent)
		{
			this.NeedResetUnitsActionPointsSpent = false;
			this.ResetActiveGarrisonUnitProperties();
		}
	}

	private void ShowKaijuArmyInMap()
	{
		if (this.armiesMap == null)
		{
			IGameService service = Services.GetService<IGameService>();
			global::Game game = service.Game as global::Game;
			this.armiesMap = (game.World.Atlas.GetMap(WorldAtlas.Maps.Armies) as GridMap<Army>);
			Diagnostics.Assert(this.armiesMap != null);
		}
		this.armiesMap.SetValue(this.KaijuArmy.WorldPosition, this.KaijuArmy);
	}

	private void HideKaijuArmyFromMap()
	{
		if (this.armiesMap == null)
		{
			IGameService service = Services.GetService<IGameService>();
			global::Game game = service.Game as global::Game;
			this.armiesMap = (game.World.Atlas.GetMap(WorldAtlas.Maps.Armies) as GridMap<Army>);
			Diagnostics.Assert(this.armiesMap != null);
		}
		this.armiesMap.SetValue(this.KaijuArmy.WorldPosition, null);
	}

	private void MoveTo(WorldPosition worldPosition)
	{
		if (this.OnGarrisonMode())
		{
			this.KaijuGarrison.MoveTo(worldPosition);
			this.HideKaijuArmyFromMap();
		}
		else if (this.OnArmyMode())
		{
			this.KaijuArmy.MoveTo(worldPosition);
		}
		this.KaijuArmy.SetWorldPosition(worldPosition);
	}

	private void ReleaseKaijuArmyActions()
	{
		DepartmentOfDefense agency = this.Empire.GetAgency<DepartmentOfDefense>();
		SimulationDescriptor value = this.SimulationDescriptorDatabase.GetValue(DepartmentOfTheInterior.ArmyStatusBesiegerDescriptorName);
		this.KaijuArmy.RemoveDescriptor(value);
		if (this.KaijuArmy.IsEarthquaker)
		{
			this.KaijuArmy.SetEarthquakerStatus(false, false, null);
		}
		if (this.KaijuArmy.PillageTarget.IsValid)
		{
			DepartmentOfDefense.StopPillage(this.KaijuArmy);
		}
		if (this.KaijuArmy.IsAspirating)
		{
			agency.StopAspirating(this.KaijuArmy);
		}
		if (this.KaijuArmy.IsDismantlingDevice)
		{
			ITerraformDeviceRepositoryService service = this.gameService.Game.Services.GetService<ITerraformDeviceRepositoryService>();
			TerraformDevice device = service[this.KaijuArmy.DismantlingDeviceTarget] as TerraformDevice;
			agency.StopDismantelingDevice(this.KaijuArmy, device);
		}
		if (this.KaijuArmy.IsDismantlingCreepingNode)
		{
			CreepingNode creepingNode = null;
			if (this.gameEntityRepositoryService.TryGetValue<CreepingNode>(this.KaijuArmy.DismantlingCreepingNodeTarget, out creepingNode))
			{
				agency.StopDismantelingCreepingNode(this.KaijuArmy, creepingNode);
			}
		}
		IWorldPositionningService service2 = this.gameService.Game.Services.GetService<IWorldPositionningService>();
		Region region = service2.GetRegion(this.KaijuArmy.WorldPosition);
		if (region.City != null && region.City.Empire != this.Empire)
		{
			DepartmentOfTheInterior agency2 = region.City.Empire.GetAgency<DepartmentOfTheInterior>();
			if (agency2 != null)
			{
				if (region.City.BesiegingEmpire == this.Empire && agency2.NeedToStopSiege(region.City))
				{
					agency2.StopSiege(region.City);
				}
				agency2.StopNavalSiege(region.City, this.KaijuArmy);
			}
			IVisibilityService service3 = this.gameService.Game.Services.GetService<IVisibilityService>();
			service3.NotifyVisibilityHasChanged(this.Empire);
		}
	}

	public string GetMonsterUnitDesignLocalizedName()
	{
		string result = string.Empty;
		foreach (Unit unit in this.GetActiveTroops().Units)
		{
			if (unit.UnitDesign.Tags.Contains(Kaiju.MonsterUnitTag))
			{
				result = unit.UnitDesign.LocalizedName;
				break;
			}
		}
		return result;
	}

	private WorldPawn GetWorldMonsterPawn(WorldUnit worldUnit)
	{
		WorldPawn result = null;
		for (int i = 0; i < worldUnit.WorldPawns.Length; i++)
		{
			WorldPawn worldPawn = worldUnit.WorldPawns[i];
			if (worldPawn != null && worldPawn.Unit.UnitDesign.Tags.Contains(Kaiju.MonsterUnitTag))
			{
				result = worldPawn;
				break;
			}
		}
		return result;
	}

	private void WorldPawn_OnFinalWorldOrientationChanged(object sender, WorldPawnFinalWorldOrientationChanged worldPawnFinalWorldOrientationChanged)
	{
		this.WorldOrientation = worldPawnFinalWorldOrientationChanged.FinalWorldOrientation;
	}

	public static readonly StaticString ClassKaijuGarrison = "ClassKaijuGarrison";

	public static readonly StaticString ClassKaijuArmy = "ClassKaijuArmy";

	public static readonly StaticString KaijuGarrisonModeDescriptor = "KaijuGarrisonMode";

	public static readonly StaticString KaijuArmyModeDescriptor = "KaijuArmyMode";

	public static readonly StaticString KaijuStatusWild = "KaijuStatusWild";

	public static readonly StaticString KaijuStatusTamed = "KaijuStatusTamed";

	public static readonly StaticString KaijuStatusStunned = "KaijuStatusStunned";

	public static readonly StaticString MonsterUnitTag = "KaijuMonster";

	public static readonly StaticString LiceUnitTag = "KaijuLice";

	private global::Empire empire;

	private MajorEmpire majorEmpire;

	private int ownerEmpireIndex;

	private KaijuEmpire kaijuEmpire;

	private int kaijuEmpireIndex;

	private GameEntityGUID kaijuArmyGUID;

	private Region region;

	private int previousOwnerIndex;

	private int tamedTurn;

	private int previousStunnerIndex;

	private int previousStunnedTurn;

	private IGameService gameService;

	private IEventService eventService;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private IKaijuTechsService kaijusTechService;

	private IDatabase<SimulationDescriptor> simulationDescriptorDatabase;

	private GridMap<Army> armiesMap;

	private bool needResetUnitsActionPointsSpent;

	private bool kaijuModesShareActionPoints;

	private WorldOrientation worldOrientation;

	public delegate void ConvertedSignature();

	public delegate void RegionChangedSignature(Region region);

	public delegate void OwnerChangedSignature(global::Empire lastOwner, global::Empire newOwner);
}
