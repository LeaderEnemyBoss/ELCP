using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Interop;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Session;
using Amplitude.Unity.Simulation;
using UnityEngine;

public class BattleEncounter : IDisposable
{
	protected BattleEncounter(GameEntityGUID encounterGuid)
	{
		Diagnostics.Assert(encounterGuid != GameEntityGUID.Zero, "Battle encounter GUID is null.");
		this.EncounterGUID = encounterGuid;
		this.finiteStateMachine = new FiniteStateMachine();
		this.finiteStateMachine.RegisterInitialState(new BattleEncounterState_Setup(this));
		this.finiteStateMachine.RegisterState(new BattleEncounterState_Setup_WaitForContendersAcknowledge(this));
		this.finiteStateMachine.RegisterState(new BattleEncounterState_Deployment(this));
		this.finiteStateMachine.RegisterState(new BattleEncounterState_Deployment_WaitingForContenders(this));
		this.finiteStateMachine.RegisterState(new BattleEncounterState_Countdown(this));
		this.finiteStateMachine.RegisterState(new BattleEncounterState_Battle(this));
		this.finiteStateMachine.RegisterState(new BattleEncounterState_Report(this));
		this.finiteStateMachine.RegisterState(new BattleEncounterState_Terminate(this));
		if (this.finiteStateMachine.InitialStateType != null)
		{
			this.finiteStateMachine.PostStateChange(this.finiteStateMachine.InitialStateType, new object[0]);
		}
		this.BattleContenders = new List<BattleContender>();
		this.incommingReinforcement = new List<GameEntityGUID>();
		this.reinforcementTemplateDatabase = Databases.GetDatabase<ReinforcementTemplate>(false);
		this.gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(this.gameService != null);
		Diagnostics.Assert(this.gameService.Game != null);
		this.game = (this.gameService.Game as global::Game);
		Diagnostics.Assert(this.game != null);
		this.worldPositionningService = this.gameService.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.pathFindingService = this.gameService.Game.Services.GetService<IPathfindingService>();
		Diagnostics.Assert(this.pathFindingService != null);
		this.ExternalArmies = new List<GameEntityGUID>();
		this.battleZoneAnalysis = new BattleZoneAnalysis();
		this.battleZoneAnalysis.Initialize(this);
		this.ReinforcementNextRankingID = 0;
	}

	// Note: this type is marked as 'beforefieldinit'.
	static BattleEncounter()
	{
		BattleEncounter.deploymentPriority = "DeploymentPriority";
	}

	public BattleZoneAnalysis BattleZoneAnalysis
	{
		get
		{
			return this.battleZoneAnalysis;
		}
	}

	public virtual bool Deploy(BattleContender battleContender, bool unitsAreReinforcement = false)
	{
		Deployment deployment = battleContender.Deployment;
		WorldPosition center = battleContender.WorldPosition;
		WorldOrientation forward = battleContender.WorldOrientation;
		unitsAreReinforcement |= !battleContender.IsTakingPartInBattle;
		PathfindingWorldContext worldContext;
		if (deployment == null)
		{
			deployment = new Deployment();
			battleContender.Deployment = deployment;
			BattleContender battleContender2 = this.BattleContenders.FirstOrDefault((BattleContender match) => match.Group == battleContender.Group && match.Deployment != null && match.Deployment.DeploymentArea != null);
			if (unitsAreReinforcement && battleContender2 != null)
			{
				deployment.DeploymentArea = battleContender2.Deployment.DeploymentArea;
				deployment.ReinforcementPoints = battleContender2.Deployment.ReinforcementPoints;
				deployment.BattleZone = battleContender2.Deployment.BattleZone;
				deployment.UnitDeployment = new UnitDeployment[0];
				return true;
			}
			if (battleContender2 != null)
			{
				center = battleContender2.WorldPosition;
				forward = battleContender2.WorldOrientation;
			}
			if (!battleContender.IsAttacking && battleContender.Garrison is Fortress)
			{
				BattleContender battleContender3 = this.BattleContenders.FirstOrDefault((BattleContender match) => match.IsAttacking && match.Deployment != null && match.Deployment.DeploymentArea != null);
				if (battleContender3 != null)
				{
					forward = battleContender3.WorldOrientation.Rotate(3);
					center = this.worldPositionningService.GetNeighbourTileFullCyclic(battleContender3.WorldPosition, battleContender3.WorldOrientation, 1);
				}
			}
			int value = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<int>("Gameplay/Battle/DeploymentAreaWidth", 3);
			int value2 = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<int>("Gameplay/Battle/DeploymentAreaDepth", 2);
			WorldParameters worldParameters = this.worldPositionningService.World.WorldParameters;
			deployment.DeploymentArea = new DeploymentArea(center, forward, worldParameters);
			try
			{
				deployment.DeploymentArea.Initialize(value, value2);
			}
			catch (Exception ex)
			{
				Diagnostics.LogError("{0}, the deployment will be initiated with the default parameters.", new object[]
				{
					ex.Message
				});
				deployment.DeploymentArea.Initialize(3, 2);
			}
			BattleZone_Deployment battleZone_Deployment = new BattleZone_Deployment(deployment, worldParameters);
			this.FilterDeploymentPosition(battleContender, deployment.DeploymentArea, battleZone_Deployment);
			deployment.BattleZone = battleZone_Deployment;
			worldContext = new PathfindingWorldContext(battleZone_Deployment, new Dictionary<WorldPosition, PathfindingWorldContext.TileContext>());
			List<WorldPosition> list = this.FindReinforcementPoints(battleContender, worldContext, deployment.DeploymentArea);
			deployment.ReinforcementPoints = list.ToArray();
		}
		else
		{
			if (unitsAreReinforcement)
			{
				return true;
			}
			worldContext = new PathfindingWorldContext(deployment.BattleZone, new Dictionary<WorldPosition, PathfindingWorldContext.TileContext>());
		}
		this.battleZoneAnalysis.ResetBattleZone(deployment.BattleZone);
		this.battleZoneAnalysis.ResetGroup(battleContender.Group);
		List<Unit> undeployedUnits = new List<Unit>(battleContender.Garrison.Units);
		List<UnitDeployment> list2;
		this.DoDeployUnits(battleContender.Group, undeployedUnits, worldContext, deployment.DeploymentArea, out list2);
		deployment.UnitDeployment = list2.ToArray();
		return true;
	}

	protected virtual void FilterDeploymentPosition(BattleContender battleContender, DeploymentArea deploymentArea, BattleZone battleZone)
	{
		BattleEncounter.<FilterDeploymentPosition>c__AnonStorey8A3 <FilterDeploymentPosition>c__AnonStorey8A = new BattleEncounter.<FilterDeploymentPosition>c__AnonStorey8A3();
		<FilterDeploymentPosition>c__AnonStorey8A.battleContender = battleContender;
		bool flag = false;
		Region region = this.worldPositionningService.GetRegion(<FilterDeploymentPosition>c__AnonStorey8A.battleContender.WorldPosition);
		if (region.City != null && region.City.Districts.Any((District match) => match.Type != DistrictType.Exploitation && match.WorldPosition == <FilterDeploymentPosition>c__AnonStorey8A.battleContender.WorldPosition))
		{
			flag = true;
		}
		global::Empire contenderEnemyEmpire = this.GetContenderEnemyEmpire(<FilterDeploymentPosition>c__AnonStorey8A.battleContender);
		<FilterDeploymentPosition>c__AnonStorey8A.overallPositions = new List<WorldPosition>(battleZone.GetWorldPositions());
		List<City> list = new List<City>();
		PathfindingWorldContext worldContext = new PathfindingWorldContext(battleZone, null);
		bool flag2 = <FilterDeploymentPosition>c__AnonStorey8A.battleContender.Garrison is Fortress;
		int index;
		for (index = 0; index < <FilterDeploymentPosition>c__AnonStorey8A.overallPositions.Count; index++)
		{
			if (<FilterDeploymentPosition>c__AnonStorey8A.overallPositions[index].IsValid)
			{
				bool flag3 = this.worldPositionningService.IsWaterTile(<FilterDeploymentPosition>c__AnonStorey8A.overallPositions[index]) && !this.worldPositionningService.IsFrozenWaterTile(<FilterDeploymentPosition>c__AnonStorey8A.overallPositions[index]);
				if (<FilterDeploymentPosition>c__AnonStorey8A.battleContender.Garrison is Army)
				{
					flag2 = (<FilterDeploymentPosition>c__AnonStorey8A.battleContender.Garrison as Army).IsNaval;
					if (flag2 && battleZone.CenterWorldPosition != <FilterDeploymentPosition>c__AnonStorey8A.overallPositions[index] && this.pathFindingService.FindPath(<FilterDeploymentPosition>c__AnonStorey8A.battleContender.Garrison as Army, battleZone.CenterWorldPosition, <FilterDeploymentPosition>c__AnonStorey8A.overallPositions[index], PathfindingManager.RequestMode.Default, worldContext, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreEncounterAreas | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreKaijuGarrisons, null) == null)
					{
						battleZone.RemoveWorldPosition(<FilterDeploymentPosition>c__AnonStorey8A.overallPositions[index]);
						deploymentArea.RemoveWorldPosition(<FilterDeploymentPosition>c__AnonStorey8A.overallPositions[index]);
						goto IL_398;
					}
				}
				if (flag2 && !flag3)
				{
					battleZone.RemoveWorldPosition(<FilterDeploymentPosition>c__AnonStorey8A.overallPositions[index]);
					deploymentArea.RemoveWorldPosition(<FilterDeploymentPosition>c__AnonStorey8A.overallPositions[index]);
				}
				else
				{
					if (!flag2 && flag3)
					{
						deploymentArea.RemoveWorldPosition(<FilterDeploymentPosition>c__AnonStorey8A.overallPositions[index]);
					}
					Region region2 = this.worldPositionningService.GetRegion(<FilterDeploymentPosition>c__AnonStorey8A.overallPositions[index]);
					if (region2.City != null && region2.City.Districts.Any((District match) => match.Type != DistrictType.Exploitation && match.WorldPosition == <FilterDeploymentPosition>c__AnonStorey8A.overallPositions[index]))
					{
						bool flag4 = region2.City.Empire == <FilterDeploymentPosition>c__AnonStorey8A.battleContender.Garrison.Empire;
						bool flag5 = region2.City.BesiegingEmpire == contenderEnemyEmpire;
						if (region2.City.BesiegingEmpire != null && !flag && (!flag4 || !flag5))
						{
							battleZone.RemoveWorldPosition(<FilterDeploymentPosition>c__AnonStorey8A.overallPositions[index]);
							deploymentArea.RemoveWorldPosition(<FilterDeploymentPosition>c__AnonStorey8A.overallPositions[index]);
						}
						if (region2.City.Empire != <FilterDeploymentPosition>c__AnonStorey8A.battleContender.Garrison.Empire)
						{
							deploymentArea.RemoveWorldPosition(<FilterDeploymentPosition>c__AnonStorey8A.overallPositions[index]);
						}
						else if (!list.Contains(region2.City))
						{
							list.Add(region2.City);
						}
					}
				}
			}
			IL_398:;
		}
		for (int i = 0; i < list.Count; i++)
		{
			for (int j = 0; j < list[i].Districts.Count; j++)
			{
				if (list[i].Districts[j].Type != DistrictType.Exploitation)
				{
					WorldPosition worldPosition = list[i].Districts[j].WorldPosition;
					bool flag6 = this.worldPositionningService.IsWaterTile(worldPosition) && !this.worldPositionningService.IsFrozenWaterTile(worldPosition);
					if (!flag2 || flag6)
					{
						if (!deploymentArea.Contains(list[i].Districts[j].WorldPosition))
						{
							deploymentArea.AddWorldPosition(list[i].Districts[j].WorldPosition);
						}
						if (!battleZone.Contains(list[i].Districts[j].WorldPosition))
						{
							battleZone.AddWorldPosition(list[i].Districts[j].WorldPosition);
						}
					}
				}
			}
		}
	}

	private void DoDeployUnits(byte group, List<Unit> undeployedUnits, PathfindingWorldContext worldContext, DeploymentArea deploymentArea, out List<UnitDeployment> unitDeployments)
	{
		unitDeployments = new List<UnitDeployment>();
		List<WorldPosition> list = new List<WorldPosition>(deploymentArea.GetWorldPositions());
		IDatabase<DeploymentPriorityModifierDefinition> database = Databases.GetDatabase<DeploymentPriorityModifierDefinition>(false);
		DeploymentPriorityModifierDefinition[] values = database.GetValues();
		List<SimulationDescriptor> list2 = new List<SimulationDescriptor>();
		for (int i = 0; i < undeployedUnits.Count; i++)
		{
			Unit unit4 = undeployedUnits[i];
			foreach (DeploymentPriorityModifierDefinition deploymentPriorityModifierDefinition in values)
			{
				if (deploymentPriorityModifierDefinition.CheckPrerequisites(unit4))
				{
					SimulationDescriptor[] array = null;
					SimulationDescriptorReference.GetSimulationDescriptorsFromXmlReferences(deploymentPriorityModifierDefinition.SimulationDescriptorReferences, ref array);
					foreach (SimulationDescriptor simulationDescriptor in array)
					{
						if (!unit4.SimulationObject.Tags.Contains(simulationDescriptor.Name))
						{
							unit4.AddDescriptor(simulationDescriptor, false);
							unit4.Refresh(false);
							if (!list2.Contains(simulationDescriptor))
							{
								list2.Add(simulationDescriptor);
							}
						}
					}
				}
			}
		}
		IEnumerable<Unit> source = from unit in undeployedUnits
		orderby unit.GetPropertyValue(BattleEncounter.deploymentPriority), unit.GetPropertyValue(SimulationProperties.MilitaryPower) descending
		select unit;
		undeployedUnits = source.ToList<Unit>();
		Diagnostics.Assert(group == 0 || group == 1);
		int num = (int)(group ^ 1);
		GameEntityGUID guid = this.OrderCreateEncounter.ContenderGUIDs[num];
		IGameEntityRepositoryService service = this.game.Services.GetService<IGameEntityRepositoryService>();
		IGameEntity gameEntity;
		service.TryGetValue(guid, out gameEntity);
		Garrison simulationObjectWrapper = gameEntity as Garrison;
		foreach (Unit unit2 in undeployedUnits)
		{
			if (list.Count == 0)
			{
				break;
			}
			WorldPosition worldPosition = this.battleZoneAnalysis.SelectWorldPosition(group, unit2, list, deploymentArea.Center, worldContext, simulationObjectWrapper);
			if (!worldPosition.IsValid)
			{
				break;
			}
			list.Remove(worldPosition);
			unitDeployments.Add(new UnitDeployment(unit2.GUID, worldPosition));
			worldContext.SetTileContext(worldPosition, BattleZoneAnalysis.BattleSameGroupUnitSpecification);
		}
		for (int l = 0; l < undeployedUnits.Count; l++)
		{
			Unit unit3 = undeployedUnits[l];
			for (int m = 0; m < list2.Count; m++)
			{
				SimulationDescriptor simulationDescriptor2 = list2[m];
				if (unit3.SimulationObject.Tags.Contains(simulationDescriptor2.Name))
				{
					unit3.RemoveDescriptor(simulationDescriptor2);
					unit3.Refresh(false);
				}
			}
		}
		list2.Clear();
	}

	public bool IsCityFromGroup(City city, byte group)
	{
		for (int i = 0; i < this.BattleContenders.Count; i++)
		{
			if (city.Empire == this.BattleContenders[i].Garrison.Empire)
			{
				return this.BattleContenders[i].Group == group;
			}
		}
		return false;
	}

	public BattleContender GetContenderMainEnemyContender(BattleContender contender)
	{
		for (int i = 0; i < this.BattleContenders.Count; i++)
		{
			if (this.BattleContenders[i].Group != contender.Group && this.BattleContenders[i].IsMainContender)
			{
				return this.BattleContenders[i];
			}
		}
		return null;
	}

	public BattleContender GetEnemyContenderWithAbilityFromContender(BattleContender contender, StaticString unitAbility)
	{
		BattleContender battleContender = null;
		for (int i = 0; i < this.BattleContenders.Count; i++)
		{
			if (this.BattleContenders[i].Group != contender.Group)
			{
				if (battleContender != null && this.BattleContenders[i].IsMainContender)
				{
					battleContender = this.BattleContenders[i];
				}
				foreach (Unit unit in this.BattleContenders[i].Garrison.Units)
				{
					if (unit.CheckUnitAbility(unitAbility, -1))
					{
						return this.BattleContenders[i];
					}
				}
			}
		}
		return battleContender;
	}

	~BattleEncounter()
	{
		this.Dispose(false);
	}

	public BattleController BattleController
	{
		get
		{
			return this.battleController;
		}
	}

	public List<BattleContender> BattleContenders { get; private set; }

	public List<GameEntityGUID> ExternalArmies { get; private set; }

	public GameEntityGUID EncounterGUID { get; private set; }

	public int IncommingJoinContendersCount { get; set; }

	public bool IsBattleFinished
	{
		get
		{
			return this.finiteStateMachine == null || this.finiteStateMachine.CurrentState is BattleEncounterState_Terminate;
		}
	}

	public OrderCreateEncounter OrderCreateEncounter { get; private set; }

	public int ReinforcementNextRankingID { get; set; }

	public bool Retreat { get; set; }

	internal global::PlayerController PlayerController
	{
		get
		{
			if (this.gameService.Game != null)
			{
				IPlayerControllerRepositoryService service = this.gameService.Game.Services.GetService<IPlayerControllerRepositoryService>();
				IPlayerControllerRepositoryControl playerControllerRepositoryControl = service as IPlayerControllerRepositoryControl;
				if (playerControllerRepositoryControl != null)
				{
					return playerControllerRepositoryControl.GetPlayerControllerById("server");
				}
			}
			return null;
		}
	}

	public static BattleEncounter Decode(OrderCreateEncounter order)
	{
		BattleEncounter battleEncounter;
		if (order is OrderCreateCityAssaultEncounter)
		{
			OrderCreateCityAssaultEncounter orderCreateCityAssaultEncounter = order as OrderCreateCityAssaultEncounter;
			battleEncounter = new BattleCityAssaultEncounter(order.EncounterGUID)
			{
				CityGuid = orderCreateCityAssaultEncounter.CityGUID,
				MilitiaGuid = orderCreateCityAssaultEncounter.MilitiaGUID
			};
		}
		else
		{
			battleEncounter = new BattleEncounter(order.EncounterGUID);
		}
		battleEncounter.OrderCreateEncounter = order;
		return battleEncounter;
	}

	public static StaticString GetDefaultStrategy()
	{
		IDatabase<BattleTargetingStrategy> database = Databases.GetDatabase<BattleTargetingStrategy>(false);
		Diagnostics.Assert(database != null);
		StaticString result = null;
		BattleTargetingStrategy[] values = database.GetValues();
		if (values.Length > 0)
		{
			bool flag = false;
			foreach (BattleTargetingStrategy battleTargetingStrategy in values)
			{
				if (battleTargetingStrategy.IsDefaultStrategy)
				{
					if (flag)
					{
						Diagnostics.LogError("There can only be one default strategy. Please check BattleTargetingStrategy.xml");
					}
					else
					{
						result = battleTargetingStrategy.Name;
						flag = true;
					}
				}
			}
			if (!flag)
			{
				Diagnostics.LogError("There isn't any default strategy. Please check BattleTargetingStrategy.xml");
				result = values[0].Name;
			}
		}
		return result;
	}

	public int AskNearbyArmiesToJoin()
	{
		int num = 0;
		global::PlayerController playerController = this.PlayerController;
		if (playerController == null)
		{
			return num;
		}
		List<Army> list = new List<Army>();
		List<City> list2 = new List<City>();
		List<Fortress> list3 = new List<Fortress>();
		List<Camp> list4 = new List<Camp>();
		List<Village> list5 = new List<Village>();
		List<KaijuGarrison> list6 = new List<KaijuGarrison>();
		foreach (global::Empire empire in this.game.Empires)
		{
			DepartmentOfDefense agency = empire.GetAgency<DepartmentOfDefense>();
			list.AddRange(agency.Armies);
			DepartmentOfTheInterior agency2 = empire.GetAgency<DepartmentOfTheInterior>();
			if (agency2 != null)
			{
				list2.AddRange(agency2.Cities);
				list3.AddRange(agency2.OccupiedFortresses);
				list4.AddRange(agency2.Camps);
				list5.AddRange(agency2.ConvertedVillages);
				list6.AddRange(agency2.TamedKaijuGarrisons);
			}
		}
		IBattleEncounterRepositoryService battleEncounterRepositoryService = this.gameService.Game.Services.GetService<IBattleEncounterRepositoryService>();
		list3.RemoveAll((Fortress match) => match.IsInEncounter || battleEncounterRepositoryService.Any((BattleEncounter encounter) => encounter.IsGarrisonInEncounter(match.GUID)));
		list2.RemoveAll((City match) => match.IsInEncounter || battleEncounterRepositoryService.Any((BattleEncounter encounter) => encounter.IsGarrisonInEncounter(match.GUID)));
		list.RemoveAll((Army match) => match.IsInEncounter || battleEncounterRepositoryService.Any((BattleEncounter encounter) => encounter.IsGarrisonInEncounter(match.GUID)));
		list4.RemoveAll((Camp match) => match.IsInEncounter || battleEncounterRepositoryService.Any((BattleEncounter encounter) => encounter.IsGarrisonInEncounter(match.GUID)));
		list5.RemoveAll((Village match) => match.IsInEncounter || battleEncounterRepositoryService.Any((BattleEncounter encounter) => encounter.IsGarrisonInEncounter(match.GUID)));
		list6.RemoveAll((KaijuGarrison match) => match.IsInEncounter || battleEncounterRepositoryService.Any((BattleEncounter encounter) => encounter.IsGarrisonInEncounter(match.GUID)));
		for (int j = 0; j < this.BattleContenders.Count; j++)
		{
			BattleContender battleContender = this.BattleContenders[j];
			IBattleZone battleZone = battleContender.Deployment.BattleZone;
			Army army;
			IEnumerable<Army> enumerable = from army in list
			where battleZone.Contains(army.WorldPosition) && !this.BattleContenders.Exists((BattleContender match) => match.GUID == army.GUID) && !this.incommingReinforcement.Exists((GameEntityGUID match) => match == army.GUID)
			select army;
			City city;
			IEnumerable<City> enumerable2 = from city in list2
			where battleZone.Contains(city.WorldPosition) && !this.BattleContenders.Exists((BattleContender match) => match.GUID == city.GUID) && !this.incommingReinforcement.Exists((GameEntityGUID match) => match == city.GUID)
			select city;
			Fortress fortress;
			IEnumerable<Fortress> enumerable3 = from fortress in list3
			where battleZone.Contains(fortress.WorldPosition) && !this.BattleContenders.Exists((BattleContender match) => match.GUID == fortress.GUID) && !this.incommingReinforcement.Exists((GameEntityGUID match) => match == fortress.GUID)
			select fortress;
			Camp camp;
			IEnumerable<Camp> enumerable4 = from camp in list4
			where battleZone.Contains(camp.WorldPosition) && !this.BattleContenders.Exists((BattleContender match) => match.GUID == camp.GUID) && !this.incommingReinforcement.Exists((GameEntityGUID match) => match == camp.GUID)
			select camp;
			Village village;
			IEnumerable<Village> enumerable5 = from village in list5
			where battleZone.Contains(village.WorldPosition) && !this.BattleContenders.Exists((BattleContender match) => match.GUID == village.GUID) && !this.incommingReinforcement.Exists((GameEntityGUID match) => match == village.GUID)
			select village;
			KaijuGarrison garrison;
			IEnumerable<KaijuGarrison> enumerable6 = from garrison in list6
			where battleZone.Contains(garrison.WorldPosition) && !this.BattleContenders.Exists((BattleContender match) => match.GUID == garrison.GUID) && !this.incommingReinforcement.Exists((GameEntityGUID match) => match == garrison.GUID)
			select garrison;
			bool flag = this.worldPositionningService.IsWaterTile(battleZone.CenterWorldPosition) && !this.worldPositionningService.IsFrozenWaterTile(battleZone.CenterWorldPosition);
			List<OrderJoinEncounter.ContenderInfo> list7 = new List<OrderJoinEncounter.ContenderInfo>();
			foreach (Army army2 in enumerable)
			{
				army = army2;
				bool flag2 = false;
				if (army.Empire.SimulationObject.Tags.Contains("SeasonEffectBattleUnitAttributes6"))
				{
					flag2 = true;
				}
				else if (army.IsWildLiceArmy)
				{
					if (this.BattleContenders.Any((BattleContender iterator) => iterator is BattleContender_Village && iterator.IsMainContender && !iterator.IsAttacking))
					{
						flag2 = true;
					}
				}
				else if (army.IsNaval != flag)
				{
					flag2 = true;
				}
				else if (army.Empire is LesserEmpire)
				{
					flag2 = true;
				}
				else if (army is KaijuArmy && ((army as KaijuArmy).UnitsCount == 0 || (army as KaijuArmy).Kaiju.OnGarrisonMode()))
				{
					flag2 = true;
				}
				else
				{
					BattleContender battleContender2 = this.BattleContenders.FirstOrDefault((BattleContender contender) => contender.Garrison.Empire.Index == army.Empire.Index && contender.IsMainContender);
					if (battleContender2 == null)
					{
						battleContender2 = this.BattleContenders.FirstOrDefault((BattleContender contender) => contender.Garrison.Empire.Index == army.Empire.Index);
					}
					if (battleContender2 != null)
					{
						OrderJoinEncounter.ContenderInfo item = this.BuildContenderInfo(army.GUID, battleContender2);
						list7.Add(item);
						this.incommingReinforcement.Add(army.GUID);
						num++;
					}
					else
					{
						flag2 = true;
					}
				}
				if (flag2 && !this.ExternalArmies.Contains(army.GUID))
				{
					this.ExternalArmies.Add(army.GUID);
				}
			}
			foreach (City city2 in enumerable2)
			{
				city = city2;
				if (!city.Empire.SimulationObject.Tags.Contains("SeasonEffectBattleUnitAttributes6"))
				{
					if (!flag)
					{
						bool flag3 = city.Militia != null && city.Militia.UnitsCount > 0;
						if (city.UnitsCount != 0 || flag3)
						{
							BattleContender battleContender3 = this.BattleContenders.FirstOrDefault((BattleContender contender) => contender.Garrison.Empire.Index == city.Empire.Index && contender.IsMainContender);
							if (battleContender3 != null)
							{
								if (city.BesiegingEmpireIndex >= 0)
								{
									BattleContender battleContender4 = this.BattleContenders.FirstOrDefault((BattleContender contender) => contender.Garrison.Empire.Index != city.Empire.Index);
									if (battleContender4 == null || battleContender4.Garrison.Empire.Index != city.BesiegingEmpireIndex)
									{
										continue;
									}
								}
								if (!this.BattleContenders.Exists((BattleContender match) => match.GUID == city.GUID))
								{
									OrderJoinEncounter.ContenderInfo item2 = this.BuildContenderInfo(city.GUID, battleContender3);
									list7.Add(item2);
									this.incommingReinforcement.Add(city.GUID);
									num++;
								}
							}
						}
					}
				}
			}
			foreach (Camp camp2 in enumerable4)
			{
				camp = camp2;
				if (!camp.Empire.SimulationObject.Tags.Contains("SeasonEffectBattleUnitAttributes6"))
				{
					if (!flag)
					{
						if (camp.UnitsCount != 0)
						{
							BattleContender battleContender5 = this.BattleContenders.FirstOrDefault((BattleContender contender) => contender.Garrison.Empire.Index == camp.Empire.Index && contender.IsMainContender);
							if (battleContender5 != null)
							{
								if (camp.City.BesiegingEmpireIndex >= 0)
								{
									BattleContender battleContender6 = this.BattleContenders.FirstOrDefault((BattleContender contender) => contender.Garrison.Empire.Index != camp.Empire.Index);
									if (battleContender6 == null || battleContender6.Garrison.Empire.Index != camp.City.BesiegingEmpireIndex)
									{
										continue;
									}
								}
								if (!this.BattleContenders.Exists((BattleContender match) => match.GUID == camp.GUID))
								{
									OrderJoinEncounter.ContenderInfo item3 = this.BuildContenderInfo(camp.GUID, battleContender5);
									list7.Add(item3);
									this.incommingReinforcement.Add(camp.GUID);
									num++;
								}
							}
						}
					}
				}
			}
			foreach (Fortress fortress2 in enumerable3)
			{
				fortress = fortress2;
				if (!fortress.Empire.SimulationObject.Tags.Contains("SeasonEffectBattleUnitAttributes6"))
				{
					if (flag)
					{
						if (fortress != null && fortress.UnitsCount > 0)
						{
							BattleContender battleContender7 = this.BattleContenders.FirstOrDefault((BattleContender contender) => contender.Garrison.Empire.Index == fortress.Empire.Index && contender.IsMainContender);
							if (battleContender7 != null && !this.BattleContenders.Exists((BattleContender match) => match.GUID == fortress.GUID))
							{
								OrderJoinEncounter.ContenderInfo item4 = this.BuildContenderInfo(fortress.GUID, battleContender7);
								list7.Add(item4);
								this.incommingReinforcement.Add(fortress.GUID);
								num++;
							}
						}
					}
				}
			}
			foreach (Village village2 in enumerable5)
			{
				village = village2;
				if (!village.Empire.SimulationObject.Tags.Contains("SeasonEffectBattleUnitAttributes6"))
				{
					if (!flag)
					{
						if (village.UnitsCount != 0)
						{
							BattleContender battleContender8 = this.BattleContenders.FirstOrDefault((BattleContender contender) => contender.Garrison.Empire.Index == village.Empire.Index && contender.IsMainContender);
							if (battleContender8 != null)
							{
								if (village.Region.City != null && village.Region.City.BesiegingEmpireIndex >= 0)
								{
									BattleContender battleContender9 = this.BattleContenders.FirstOrDefault((BattleContender contender) => contender.Garrison.Empire.Index != village.Empire.Index);
									if (battleContender9 == null || battleContender9.Garrison.Empire.Index != village.Region.City.BesiegingEmpireIndex)
									{
										continue;
									}
								}
								if (!this.BattleContenders.Exists((BattleContender match) => match.GUID == village.GUID))
								{
									OrderJoinEncounter.ContenderInfo item5 = this.BuildContenderInfo(village.GUID, battleContender8);
									list7.Add(item5);
									this.incommingReinforcement.Add(village.GUID);
									num++;
								}
							}
						}
					}
				}
			}
			foreach (KaijuGarrison garrison2 in enumerable6)
			{
				garrison = garrison2;
				if (!flag)
				{
					if (garrison.UnitsCount != 0)
					{
						BattleContender battleContender10 = this.BattleContenders.FirstOrDefault((BattleContender contender) => contender.Garrison.Empire.Index == garrison.Empire.Index && contender.IsMainContender);
						if (battleContender10 != null && !this.BattleContenders.Exists((BattleContender match) => match.GUID == garrison.GUID))
						{
							OrderJoinEncounter.ContenderInfo item6 = this.BuildContenderInfo(garrison.GUID, battleContender10);
							list7.Add(item6);
							this.incommingReinforcement.Add(garrison.GUID);
							num++;
						}
					}
				}
			}
			if (list7.Count > 0)
			{
				this.IncommingJoinContendersCount++;
				OrderJoinEncounter order = new OrderJoinEncounter(this.EncounterGUID, list7);
				playerController.PostOrder(order);
			}
			if (this.ExternalArmies.Count > 0)
			{
				OrderLockEncounterExternalArmies order2 = new OrderLockEncounterExternalArmies(this.EncounterGUID, this.ExternalArmies, true);
				playerController.PostOrder(order2);
			}
		}
		return num;
	}

	public bool ChangeStrategy(BattleContender battleContender, StaticString strategy)
	{
		if (this.battleController != null && this.battleController.BattleSimulation != null)
		{
			return this.battleController.BattleSimulation.ChangeStrategy(battleContender, strategy);
		}
		if (battleContender != null)
		{
			battleContender.DefaultTargetingStrategy = strategy;
			return true;
		}
		return false;
	}

	public bool ChangeUnitStrategy(GameEntityGUID unitGUID, StaticString strategy)
	{
		if (this.battleController == null || this.battleController.BattleSimulation == null)
		{
			Diagnostics.LogError("The battle controller is no ready.");
			return false;
		}
		return this.battleController.BattleSimulation.ChangeUnitStrategy(unitGUID, strategy);
	}

	public bool ChangeUnitTargeting(GameEntityGUID unitGUID, UnitTargetingIntention targetingIntention, out List<GameEntityGUID> availableOpportunityTargets)
	{
		availableOpportunityTargets = null;
		if (this.battleController == null || this.battleController.BattleSimulation == null)
		{
			Diagnostics.LogError("The battle controller is no ready.");
			return false;
		}
		return this.battleController.BattleSimulation.ChangeUnitTargeting(unitGUID, targetingIntention, out availableOpportunityTargets);
	}

	public void CreateBattleController()
	{
		IDatabase<BattleSequence> database = Databases.GetDatabase<BattleSequence>(false);
		if (database == null)
		{
			Diagnostics.LogError("Can't get battle sequence database.");
			this.PostStateChange(typeof(BattleEncounterState_Terminate), new object[0]);
			return;
		}
		ISessionService service = Services.GetService<ISessionService>();
		Diagnostics.Assert(service != null);
		string lobbyData = service.Session.GetLobbyData<string>("EncounterSequence", BattleEncounter.DefaultBattleSequenceName);
		if (!database.TryGetValue(lobbyData, out this.battleSequence))
		{
			Diagnostics.LogError("Can't found battle sequence {0}.", new object[]
			{
				BattleEncounter.DefaultBattleSequenceName
			});
			this.PostStateChange(typeof(BattleEncounterState_Terminate), new object[0]);
			return;
		}
		try
		{
			this.battleController = new BattleController(this, this.battleSequence, 7331);
		}
		catch (Exception ex)
		{
			Diagnostics.LogError("{0}\n{1}\n", new object[]
			{
				ex.Message,
				ex.StackTrace
			});
			this.PostStateChange(typeof(BattleEncounterState_Terminate), new object[0]);
			return;
		}
		if (this.BattleController == null)
		{
			Diagnostics.LogError("The battle controller has not been initialized for encounter '{0}'", new object[]
			{
				this.EncounterGUID
			});
			this.PostStateChange(typeof(BattleEncounterState_Terminate), new object[0]);
		}
	}

	public StaticString GetUnitStrategy(GameEntityGUID unitGUID)
	{
		if (this.battleController == null || this.battleController.BattleSimulation == null)
		{
			Diagnostics.LogError("The battle controller is no ready.");
			return string.Empty;
		}
		return this.battleController.BattleSimulation.GetUnitStrategy(unitGUID);
	}

	public void Dispose()
	{
		this.Dispose(true);
	}

	public bool ExecuteBattleAction(BattleContender battleContender, BattleActionUser battleActionUser, UnitBodyDefinition unitBodyDefinition)
	{
		if (battleActionUser == null)
		{
			throw new ArgumentNullException("battleActionUser");
		}
		if (this.battleController == null || this.battleController.BattleSimulation == null)
		{
			Diagnostics.LogError("The battle controller is no ready.");
			return false;
		}
		return this.battleController.BattleSimulation.ExecuteBattleActionUser(battleContender, battleActionUser, unitBodyDefinition);
	}

	public bool IncludeContenderInEncounter(GameEntityGUID contenderGuid, bool include)
	{
		BattleContender battleContender = this.BattleContenders.Find((BattleContender iterator) => iterator.GUID == contenderGuid);
		if (battleContender == null || battleContender.IsMainContender || battleContender.IsTakingPartInBattle == include)
		{
			return false;
		}
		if (include && battleContender.IsAttacking && !battleContender.IsMainContender)
		{
			ArmyAction armyAction = null;
			IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
			if (database != null && database.TryGetValue(ArmyAction_Attack.ReadOnlyName, out armyAction))
			{
				float costInActionPoints = armyAction.GetCostInActionPoints();
				if (costInActionPoints > 0f && !battleContender.HasEnoughActionPoint(costInActionPoints))
				{
					battleContender.IsTakingPartInBattle = false;
					return false;
				}
			}
		}
		battleContender.IsTakingPartInBattle = include;
		return true;
	}

	public virtual bool IsGarrisonInEncounter(GameEntityGUID guid)
	{
		return !this.IsBattleFinished && ((this.BattleContenders != null && this.BattleContenders.Exists((BattleContender match) => match.GUID == guid && match.ContenderState != ContenderState.Defeated && match.ContenderState != ContenderState.Survived)) || (this.incommingReinforcement != null && this.incommingReinforcement.Exists((GameEntityGUID match) => match == guid)) || this.OrderCreateEncounter.ContenderGUIDs.Any((GameEntityGUID match) => match == guid));
	}

	public virtual void CancelWaitingForContenders(GameEntityGUID contenderGuid)
	{
		this.incommingReinforcement.Remove(contenderGuid);
	}

	public virtual bool Join(GameEntityGUID contenderGuid, bool isCity, bool isCamp, bool isVillage, byte group, bool isReinforcement, int reinforcementRanking, bool isAllowedToTakePart, out BattleContender battleContender)
	{
		battleContender = null;
		if (this.BattleContenders.Any((BattleContender iterator) => iterator.GUID == contenderGuid))
		{
			Diagnostics.LogError("Battle contender (guid: {0}) has already joinded the battle encounter.", new object[]
			{
				contenderGuid
			});
			return false;
		}
		if (isCity)
		{
			battleContender = new BattleContender_City(contenderGuid, group, !isReinforcement);
		}
		else if (isCamp)
		{
			battleContender = new BattleContender_Camp(contenderGuid, group, !isReinforcement);
		}
		else if (isVillage)
		{
			battleContender = new BattleContender_Village(contenderGuid, group, !isReinforcement);
		}
		else
		{
			battleContender = new BattleContender(contenderGuid, group, !isReinforcement);
		}
		battleContender.ReinforcementRanking = reinforcementRanking;
		if (this.forceDefenderPosition && contenderGuid == this.OrderCreateEncounter.ContenderGUIDs[1])
		{
			battleContender.WorldOrientation = this.BattleContenders[0].WorldOrientation.Rotate(3);
			battleContender.WorldPosition = this.worldPositionningService.GetNeighbourTile(this.BattleContenders[0].WorldPosition, this.BattleContenders[0].WorldOrientation, 1);
		}
		else if (battleContender.WorldOrientation == WorldOrientation.Undefined)
		{
			Diagnostics.Assert(this.BattleContenders.Count > 0);
			battleContender.WorldOrientation = this.worldPositionningService.GetOrientation(battleContender.WorldPosition, this.BattleContenders[0].WorldPosition);
		}
		battleContender.ContenderJoinAcknowledges = new List<Steamworks.SteamID>();
		battleContender.IsTakingPartInBattle = isAllowedToTakePart;
		if (battleContender.IsTakingPartInBattle && battleContender.IsAttacking && !battleContender.IsMainContender)
		{
			ArmyAction armyAction = null;
			IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
			if (database != null && database.TryGetValue(ArmyAction_Attack.ReadOnlyName, out armyAction))
			{
				float costInActionPoints = armyAction.GetCostInActionPoints();
				if (costInActionPoints > 0f && !battleContender.HasEnoughActionPoint(costInActionPoints))
				{
					battleContender.IsTakingPartInBattle = false;
				}
			}
		}
		this.CheckTakePartInBattleAgainstDiplomacy(ref battleContender);
		if (!battleContender.IsMainContender && !this.BattleContenders.Exists((BattleContender match) => match.Group == group && match.Deployment != null && match.Deployment.DeploymentArea != null))
		{
			battleContender.IsMainContender = true;
		}
		return this.Join(ref battleContender, isReinforcement);
	}

	public bool LaunchSpell(BattleContender battleContender, SpellDefinition spellDefinition, WorldPosition targetPosition)
	{
		if (this.battleController == null || this.battleController.BattleSimulation == null)
		{
			Diagnostics.LogError("The battle controller is not ready.");
			return false;
		}
		return this.battleController.BattleSimulation.LaunchSpell(battleContender, spellDefinition, targetPosition);
	}

	public void PostStateChange(Type type, params object[] parameters)
	{
		this.finiteStateMachine.PostStateChange(type, parameters);
	}

	public void ReleaseBattleController()
	{
		if (this.battleController != null)
		{
			this.battleController.Dispose();
			this.battleController = null;
		}
	}

	public void SetContendersState(ContenderState state)
	{
		for (int i = 0; i < this.BattleContenders.Count; i++)
		{
			this.BattleContenders[i].ContenderState = state;
		}
	}

	public void SetContenderState(GameEntityGUID contenderGuid, ContenderState state)
	{
		BattleContender battleContender = this.BattleContenders.Find((BattleContender iterator) => iterator.GUID == contenderGuid);
		if (battleContender != null)
		{
			battleContender.ContenderState = state;
		}
		else
		{
			Diagnostics.LogWarning("SetContenderState: contender with guid = '{0}' not found", new object[]
			{
				contenderGuid
			});
		}
	}

	public void SetContenderOptionChoice(GameEntityGUID contenderGuid, EncounterOptionChoice contenderEncounterOptionChoice)
	{
		BattleContender battleContender = this.BattleContenders.Find((BattleContender iterator) => iterator.GUID == contenderGuid);
		if (battleContender != null)
		{
			battleContender.ContenderEncounterOptionChoice = contenderEncounterOptionChoice;
		}
		else
		{
			Diagnostics.LogWarning("SetContenderOptionChoice: contender with guid = '{0}' not found", new object[]
			{
				contenderGuid
			});
		}
	}

	public void SetContenderIsRetreating(GameEntityGUID contenderGuid, bool isRetreating)
	{
		BattleContender battleContender = this.BattleContenders.Find((BattleContender iterator) => iterator.GUID == contenderGuid);
		if (battleContender != null)
		{
			if (isRetreating && !battleContender.HasEnoughActionPoint(1f))
			{
				isRetreating = false;
			}
			battleContender.IsRetreating = isRetreating;
			this.Retreat = (this.Retreat || isRetreating);
		}
	}

	public bool SetReadyForBattle(GameEntityGUID contenderGuid)
	{
		BattleContender battleContender = this.BattleContenders.Find((BattleContender iterator) => iterator.GUID == contenderGuid);
		if (battleContender != null)
		{
			switch (battleContender.ContenderState)
			{
			case ContenderState.ReadyForDeployment:
				break;
			case ContenderState.Deployment:
				battleContender.ContenderState = ContenderState.ReadyForBattle;
				return true;
			case ContenderState.ReadyForBattle:
				return true;
			default:
				Diagnostics.LogWarning("Invalid contender state '{0}' for contender '{1}' in encounter '{2}'.", new object[]
				{
					battleContender.ContenderState,
					battleContender.GUID,
					this.EncounterGUID
				});
				return false;
			}
		}
		return false;
	}

	public bool SetReadyForDeployment(GameEntityGUID contenderGuid)
	{
		BattleContender battleContender = this.BattleContenders.Find((BattleContender iterator) => iterator.GUID == contenderGuid);
		if (battleContender == null)
		{
			return false;
		}
		ContenderState contenderState = battleContender.ContenderState;
		if (contenderState == ContenderState.Setup)
		{
			battleContender.ContenderState = ContenderState.ReadyForDeployment;
			return true;
		}
		if (contenderState != ContenderState.ReadyForDeployment)
		{
			Diagnostics.LogWarning("Invalid contender state '{0}' for contender '{1}' in encounter '{2}'.", new object[]
			{
				battleContender.ContenderState,
				battleContender.GUID,
				this.EncounterGUID
			});
			return false;
		}
		return true;
	}

	public bool SetReadyForNextPhase(GameEntityGUID contenderGuid)
	{
		BattleContender battleContender = this.BattleContenders.Find((BattleContender iterator) => iterator.GUID == contenderGuid);
		if (battleContender == null)
		{
			return false;
		}
		ContenderState contenderState = battleContender.ContenderState;
		if (contenderState == ContenderState.TargetingPhaseInProgress)
		{
			battleContender.ContenderState = ContenderState.ReadyForNextPhase;
			return true;
		}
		if (contenderState != ContenderState.ReadyForNextPhase)
		{
			Diagnostics.LogWarning("Invalid contender state '{0}' for contender '{1}' in encounter '{2}'.", new object[]
			{
				battleContender.ContenderState,
				battleContender.GUID,
				this.EncounterGUID
			});
			return false;
		}
		return true;
	}

	public bool SetReadyForNextRound(GameEntityGUID contenderGuid)
	{
		BattleContender battleContender = this.BattleContenders.Find((BattleContender iterator) => iterator.GUID == contenderGuid);
		if (battleContender == null)
		{
			return false;
		}
		ContenderState contenderState = battleContender.ContenderState;
		if (contenderState == ContenderState.RoundInProgress)
		{
			battleContender.ContenderState = ContenderState.ReadyForNextRound;
			return true;
		}
		if (contenderState != ContenderState.ReadyForNextRound)
		{
			Diagnostics.LogError("Invalid contender state '{0}' for contender '{1}' in encounter '{2}'.", new object[]
			{
				battleContender.ContenderState,
				battleContender.GUID,
				this.EncounterGUID
			});
			return false;
		}
		return true;
	}

	public bool SetDeploymentFinished(GameEntityGUID contenderGuid)
	{
		BattleContender battleContender = this.BattleContenders.Find((BattleContender iterator) => iterator.GUID == contenderGuid);
		if (battleContender == null)
		{
			return false;
		}
		ContenderState contenderState = battleContender.ContenderState;
		if (contenderState == ContenderState.ReadyForBattle)
		{
			battleContender.ContenderState = ContenderState.ReadyForBattle_DeploymentFinished;
			return true;
		}
		if (contenderState != ContenderState.ReadyForBattle_DeploymentFinished)
		{
			Diagnostics.LogError("Invalid contender state '{0}' for contender '{1}' in encounter '{2}'.", new object[]
			{
				battleContender.ContenderState,
				battleContender.GUID,
				this.EncounterGUID
			});
			return false;
		}
		return true;
	}

	public bool SetUnitBodyAsPioritary(BattleContender battleContender, UnitBodyDefinition unitBodyDefinition)
	{
		if (this.battleController == null || this.battleController.BattleSimulation == null)
		{
			Diagnostics.LogError("The battle controller is no ready.");
			return false;
		}
		return this.battleController.BattleSimulation.SetUnitBodyAsPioritary(battleContender, unitBodyDefinition);
	}

	public bool SwitchContendersReinforcementRanking(GameEntityGUID firstContenderGuid, GameEntityGUID secondContenderGuid)
	{
		if (secondContenderGuid == firstContenderGuid)
		{
			return false;
		}
		BattleContender battleContender = null;
		BattleContender battleContender2 = null;
		for (int i = 0; i < this.BattleContenders.Count; i++)
		{
			if (this.BattleContenders[i].GUID == firstContenderGuid)
			{
				battleContender = this.BattleContenders[i];
			}
			else if (this.BattleContenders[i].GUID == secondContenderGuid)
			{
				battleContender2 = this.BattleContenders[i];
			}
		}
		if (battleContender == null || battleContender2 == null)
		{
			return false;
		}
		if (battleContender.IsMainContender || battleContender2.IsMainContender)
		{
			return false;
		}
		int reinforcementRanking = battleContender.ReinforcementRanking;
		battleContender.ReinforcementRanking = battleContender2.ReinforcementRanking;
		battleContender2.ReinforcementRanking = reinforcementRanking;
		return true;
	}

	public void ChangeContenderReinforcementRanking(GameEntityGUID contenderGuid, int newRanking)
	{
		BattleContender battleContender = null;
		for (int i = 0; i < this.BattleContenders.Count; i++)
		{
			if (this.BattleContenders[i].GUID == contenderGuid)
			{
				battleContender = this.BattleContenders[i];
				break;
			}
		}
		if (battleContender.IsMainContender)
		{
			return;
		}
		if (battleContender.ReinforcementRanking == newRanking)
		{
			return;
		}
		string text = string.Empty;
		for (int j = 0; j < this.BattleContenders.Count; j++)
		{
			if (this.BattleContenders[j].Group == battleContender.Group)
			{
				text += string.Format("{0} ({1}) at {2}\n", this.BattleContenders[j].Garrison.LocalizedName, this.BattleContenders[j].GUID, this.BattleContenders[j].ReinforcementRanking);
			}
		}
		Diagnostics.Log("Before {0} to {1}:\n{2}", new object[]
		{
			battleContender.GUID,
			newRanking,
			text
		});
		int num = 1;
		for (int k = 0; k < this.BattleContenders.Count; k++)
		{
			if (this.BattleContenders[k].Group == battleContender.Group)
			{
				if (!this.BattleContenders[k].IsMainContender && this.BattleContenders[k] != battleContender)
				{
					num++;
					if (this.BattleContenders[k].ReinforcementRanking >= newRanking)
					{
						this.BattleContenders[k].ReinforcementRanking++;
					}
					else if (this.BattleContenders[k].ReinforcementRanking > battleContender.ReinforcementRanking)
					{
						this.BattleContenders[k].ReinforcementRanking--;
					}
				}
			}
		}
		battleContender.ReinforcementRanking = Mathf.Min(num, newRanking);
		text = string.Empty;
		for (int l = 0; l < this.BattleContenders.Count; l++)
		{
			if (this.BattleContenders[l].Group == battleContender.Group)
			{
				text += string.Format("{0} ({1}) at {2}\n", this.BattleContenders[l].Garrison.LocalizedName, this.BattleContenders[l].GUID, this.BattleContenders[l].ReinforcementRanking);
			}
		}
		Diagnostics.Log("After:\n{0}", new object[]
		{
			text
		});
	}

	public bool TryGetValue(GameEntityGUID battleContenderGuid, out BattleContender battleContender)
	{
		battleContender = this.BattleContenders.Find((BattleContender iterator) => iterator.GUID == battleContenderGuid);
		return battleContender != null;
	}

	public void Update()
	{
		this.finiteStateMachine.Update();
		if (this.BattleController != null)
		{
			this.BattleController.Update();
		}
	}

	public Deployment ValidateDeployment(GameEntityGUID battleContenderGuid)
	{
		BattleContender battleContender = this.BattleContenders.Find((BattleContender match) => match.GUID == battleContenderGuid);
		Diagnostics.Assert(battleContender != null);
		if (battleContender.IsMainContender)
		{
			this.Deploy(battleContender, false);
		}
		return battleContender.Deployment;
	}

	protected OrderJoinEncounter.ContenderInfo BuildContenderInfo(GameEntityGUID contenderGUID, BattleContender allyContender)
	{
		OrderJoinEncounter.ContenderInfo result;
		result.ContenderGUID = contenderGUID;
		result.Deployment = allyContender.Deployment;
		result.Group = allyContender.Group;
		if (allyContender != null && allyContender.Garrison is EncounterCityGarrison && allyContender.Garrison.UnitsCount == 0 && allyContender.IsMainContender)
		{
			allyContender.IsMainContender = false;
			result.IsReinforcement = false;
			result.ReinforcementRanking = allyContender.ReinforcementRanking;
		}
		else
		{
			result.IsReinforcement = true;
			result.ReinforcementRanking = this.ReinforcementNextRankingID++;
		}
		result.IsTakingPartInBattle = true;
		result.IsValid = true;
		result.IsCity = false;
		result.IsCamp = false;
		result.IsVillage = false;
		return result;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			foreach (BattleContender battleContender in this.BattleContenders)
			{
				battleContender.Dispose();
			}
			this.finiteStateMachine.Clear();
			this.reinforcementTemplateDatabase = null;
			this.pathFindingService = null;
			this.worldPositionningService = null;
			this.game = null;
			this.ReleaseBattleController();
			this.battleZoneAnalysis.Release();
			this.battleZoneAnalysis = null;
		}
		this.BattleContenders.Clear();
		this.ExternalArmies.Clear();
		this.battleZoneAnalysis = null;
		this.game = null;
		this.OrderCreateEncounter = null;
		this.pathFindingService = null;
		this.reinforcementTemplateDatabase = null;
		this.worldPositionningService = null;
	}

	protected virtual List<WorldPosition> FindReinforcementPoints(BattleContender battleContender, PathfindingWorldContext worldContext, DeploymentArea deploymentArea)
	{
		if (battleContender == null)
		{
			throw new ArgumentNullException("garrison");
		}
		if (worldContext == null)
		{
			throw new ArgumentNullException("worldContext");
		}
		if (deploymentArea == null)
		{
			throw new ArgumentNullException("deploymentArea");
		}
		List<WorldPosition> list = new List<WorldPosition>();
		Diagnostics.Assert(this.reinforcementTemplateDatabase != null);
		ReinforcementTemplate reinforcementTemplate;
		if (!this.reinforcementTemplateDatabase.TryGetValue(this.DefaultReinforcementTemplateName, out reinforcementTemplate))
		{
			return list;
		}
		int num = Mathf.RoundToInt(battleContender.GetNumberOfReinforcementPoints());
		Diagnostics.Assert(reinforcementTemplate != null);
		if (reinforcementTemplate.BattleTemplatePositions == null)
		{
			return list;
		}
		PathfindingContext pathfindingContext;
		if (!(battleContender.Garrison is Fortress) && (!(battleContender.Garrison is Army) || !(battleContender.Garrison as Army).IsNaval))
		{
			pathfindingContext = new PathfindingContext(GameEntityGUID.Zero, null, PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.FrozenWater);
		}
		else
		{
			pathfindingContext = new PathfindingContext(GameEntityGUID.Zero, null, PathfindingMovementCapacity.Water);
		}
		pathfindingContext.RefreshProperties(0f, 1f, false, false, 1f, 1f);
		int num2 = 0;
		while (list.Count < num)
		{
			if (num2 >= reinforcementTemplate.BattleTemplatePositions.Length)
			{
				break;
			}
			Diagnostics.Assert(reinforcementTemplate.BattleTemplatePositions[num2] != null);
			WorldPosition relativeWorldPosition = reinforcementTemplate.BattleTemplatePositions[num2].RelativeWorldPosition;
			WorldPosition worldPosition = deploymentArea.RelativePositionToWorldPosition(relativeWorldPosition);
			bool flag;
			if (worldPosition == deploymentArea.Center)
			{
				flag = true;
			}
			else if (worldPosition.IsValid)
			{
				PathfindingResult pathfindingResult = this.pathFindingService.FindPath(pathfindingContext, deploymentArea.Center, worldPosition, PathfindingManager.RequestMode.Default, worldContext, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreEncounterAreas | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreKaijuGarrisons, null);
				flag = (pathfindingResult != null);
			}
			else
			{
				Diagnostics.LogWarning("The relative position '{0}' is not valid.", new object[]
				{
					relativeWorldPosition
				});
				flag = false;
			}
			if (flag)
			{
				list.Add(worldPosition);
			}
			num2++;
		}
		return list;
	}

	protected global::Empire GetContenderEnemyEmpire(BattleContender contender)
	{
		IGameService service = Services.GetService<IGameService>();
		IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
		for (int i = 0; i < this.OrderCreateEncounter.ContenderGUIDs.Length; i++)
		{
			IGameEntity gameEntity = null;
			if (service2.TryGetValue(this.OrderCreateEncounter.ContenderGUIDs[i], out gameEntity))
			{
				if (gameEntity is IGarrison)
				{
					IGarrison garrison = gameEntity as IGarrison;
					if (garrison.Empire.Index != contender.Garrison.Empire.Index)
					{
						return garrison.Empire;
					}
				}
			}
		}
		return null;
	}

	protected bool Join(ref BattleContender battleContender, bool joinAsReinforcement = false)
	{
		if (this.Deploy(battleContender, joinAsReinforcement))
		{
			battleContender.ContenderState = ContenderState.Setup;
			this.BattleContenders.Add(battleContender);
			if (this.incommingReinforcement.Contains(battleContender.GUID))
			{
				this.incommingReinforcement.Remove(battleContender.GUID);
			}
			return true;
		}
		battleContender = null;
		return false;
	}

	private void CheckTakePartInBattleAgainstDiplomacy(ref BattleContender battleContender)
	{
		if (!battleContender.IsTakingPartInBattle)
		{
			return;
		}
		if (!battleContender.IsAttacking)
		{
			return;
		}
		if (battleContender.IsMainContender)
		{
			return;
		}
		if (battleContender.IsPrivateers || battleContender.HasCatspaw)
		{
			return;
		}
		if (this.OrderCreateEncounter.IsAttackAllowedByDiplomacy)
		{
			return;
		}
		battleContender.IsTakingPartInBattle = false;
	}

	private static StaticString deploymentPriority;

	public static readonly StaticString DefaultBattleSequenceName = "Normal";

	protected readonly StaticString DefaultReinforcementTemplateName = "Default";

	protected bool forceDefenderPosition;

	protected IWorldPositionningService worldPositionningService;

	private BattleController battleController;

	private BattleSequence battleSequence;

	private BattleZoneAnalysis battleZoneAnalysis;

	private IGameService gameService;

	private global::Game game;

	private IPathfindingService pathFindingService;

	private IDatabase<ReinforcementTemplate> reinforcementTemplateDatabase;

	private List<GameEntityGUID> incommingReinforcement;

	private FiniteStateMachine finiteStateMachine;
}
