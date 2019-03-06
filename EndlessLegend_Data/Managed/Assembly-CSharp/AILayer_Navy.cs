using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Session;
using Amplitude.Xml;
using UnityEngine;

[PersonalityRegistryPath("AI/MajorEmpire/AIEntity_Empire/AILayer_Navy", new object[]
{

})]
public class AILayer_Navy : AILayer_BaseNavy
{
	public AILayer_Navy() : base("AILayer_Navy")
	{
		this.IsNavalEmpireHostile = false;
	}

	public BaseNavyCommander FindCommanderForTaskAt(WorldPosition targetPosition)
	{
		Region region = this.worldPositionningService.GetRegion(targetPosition);
		if (region.IsOcean)
		{
			return base.NavyCommanders.Find((BaseNavyCommander match) => match.RegionData.WaterRegionIndex == region.Index);
		}
		float num = 2.14748365E+09f;
		BaseNavyCommander result = null;
		for (int i = 0; i < base.NavyCommanders.Count; i++)
		{
			float num2 = (float)this.worldPositionningService.GetDistance(targetPosition, base.NavyCommanders[i].RegionData.WaterRegion.Barycenter);
			NavyCommander navyCommander = base.NavyCommanders[i] as NavyCommander;
			if (navyCommander != null && navyCommander.CommanderState != NavyCommander.NavyCommanderState.Inactive && num2 < num)
			{
				num = num2;
				result = base.NavyCommanders[i];
			}
		}
		return result;
	}

	protected override void GenerateArmyBasedTasks()
	{
		int i = 0;
		while (i < this.game.Empires.Length)
		{
			if (!(this.game.Empires[i] is MajorEmpire))
			{
				goto IL_69;
			}
			DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.DiplomaticRelations[i];
			if (diplomaticRelation.State == null || (!(diplomaticRelation.State.Name == DiplomaticRelationState.Names.Peace) && !(diplomaticRelation.State.Name == DiplomaticRelationState.Names.Alliance)))
			{
				goto IL_69;
			}
			IL_63:
			i++;
			continue;
			IL_69:
			if (!(this.game.Empires[i] is NavalEmpire) || this.IsNavalEmpireHostile)
			{
				DepartmentOfDefense agency = this.game.Empires[i].GetAgency<DepartmentOfDefense>();
				for (int j = 0; j < agency.Armies.Count; j++)
				{
					this.GenerateArmyBasedTask(agency.Armies[j]);
				}
				goto IL_63;
			}
			goto IL_63;
		}
		IGameService service = Services.GetService<IGameService>();
		IQuestManagementService service2 = service.Game.Services.GetService<IQuestManagementService>();
		DepartmentOfDefense agency2 = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		if (service2.IsQuestRunningForEmpire("GlobalQuestCompet#0006", base.AIEntity.Empire))
		{
			QuestBehaviour questBehaviour = service.Game.Services.GetService<IQuestRepositoryService>().GetQuestBehaviour("GlobalQuestCompet#0006", base.AIEntity.Empire.Index);
			QuestBehaviourTreeNode_Decorator_KillArmy questBehaviourTreeNode_Decorator_KillArmy;
			if (questBehaviour != null && ELCPUtilities.TryGetFirstNodeOfType<QuestBehaviourTreeNode_Decorator_KillArmy>(questBehaviour.Root as BehaviourTreeNodeController, out questBehaviourTreeNode_Decorator_KillArmy))
			{
				IGameEntityRepositoryService service3 = service.Game.Services.GetService<IGameEntityRepositoryService>();
				foreach (ulong x in questBehaviourTreeNode_Decorator_KillArmy.EnemyArmyGUIDs)
				{
					IGameEntity gameEntity = null;
					if (service3.TryGetValue(x, out gameEntity) && gameEntity is Army)
					{
						Army army = gameEntity as Army;
						bool flag = false;
						if (this.visibilityService.IsWorldPositionDetectedFor(army.WorldPosition, base.AIEntity.Empire) || !army.IsCamouflaged)
						{
							foreach (Army army2 in agency2.Armies)
							{
								if (army2.HasSeafaringUnits() && army2.GetPropertyValue(SimulationProperties.MilitaryPower) > army.GetPropertyValue(SimulationProperties.MilitaryPower))
								{
									flag = true;
									break;
								}
							}
							if (flag)
							{
								this.GenerateInterceptionTaskForEmpire(army);
							}
						}
					}
				}
			}
		}
	}

	protected override BaseNavyCommander InstanciateNavyCommander(BaseNavyRegionData regionData)
	{
		return new NavyCommander(this);
	}

	protected override BaseNavyArmy InstanciateNavyArmy(Army army)
	{
		return new NavyArmy(this);
	}

	private void GenerateArmyBasedTask(Army army)
	{
		if (base.AIEntity.Empire.Index != army.Empire.Index)
		{
			this.GenerateInterceptionTaskForEmpire(army);
			return;
		}
		if (army.HasSeafaringUnits() && !army.HasOnlySeafaringUnits(false))
		{
			this.GenerateMixedArmyTask(army);
			return;
		}
		this.GenerateReinforcementTasks(army);
	}

	private void GenerateInterceptionTaskForEmpire(Army army)
	{
		if (base.AIEntity.Empire is MajorEmpire && army.Empire is MajorEmpire && this.diplomacyLayer.GetPeaceWish(army.Empire.Index))
		{
			return;
		}
		if (army.HasSeafaringUnits() && this.NavyStateMachine.CurrentState.Name == NavyState_EarlyWait.ReadonlyName)
		{
			return;
		}
		if (this.worldPositionningService.IsFrozenWaterTile(army.WorldPosition))
		{
			return;
		}
		if (army.IsCamouflaged && !this.visibilityService.IsWorldPositionDetectedFor(army.WorldPosition, base.AIEntity.Empire) && !army.IsPillaging)
		{
			return;
		}
		Region region = this.worldPositionningService.GetRegion(army.WorldPosition);
		if (!this.MightAttackOwner(region, army.Empire))
		{
			return;
		}
		NavyCommander navyCommander = this.FindCommanderForTaskAt(army.WorldPosition) as NavyCommander;
		if (navyCommander == null || navyCommander.CommanderState == NavyCommander.NavyCommanderState.Inactive)
		{
			return;
		}
		NavyTask_Interception navyTask_Interception = this.FindTask<NavyTask_Interception>((NavyTask_Interception match) => match.TargetGuid == army.GUID);
		if (navyTask_Interception != null)
		{
			return;
		}
		navyTask_Interception = new NavyTask_Interception(this);
		navyTask_Interception.Owner = base.AIEntity.Empire;
		navyTask_Interception.TargetGuid = army.GUID;
		this.NavyTasks.Add(navyTask_Interception);
	}

	private void GenerateReinforcementTasks(Army army)
	{
		BaseNavyArmy navyArmy = base.GetNavyArmy(army);
		if (navyArmy == null)
		{
			return;
		}
		if (navyArmy.Commander == null)
		{
			return;
		}
		if (navyArmy.Garrison.StandardUnits.Count == 0)
		{
			return;
		}
		District district = this.worldPositionningService.GetDistrict(army.WorldPosition);
		if (district != null && district.Type != DistrictType.Exploitation && district.City.BesiegingEmpire != null)
		{
			return;
		}
		if (this.worldPositionningService.IsFrozenWaterTile(army.WorldPosition))
		{
			return;
		}
		if (army.CurrentUnitSlot < army.MaximumUnitSlot)
		{
			NavyTask_Reinforcement navyTask_Reinforcement = this.FindTask<NavyTask_Reinforcement>((NavyTask_Reinforcement match) => match.TargetGuid == army.GUID);
			if (navyTask_Reinforcement != null)
			{
				return;
			}
			navyTask_Reinforcement = new NavyTask_Reinforcement(this, navyArmy);
			this.NavyTasks.Add(navyTask_Reinforcement);
		}
	}

	private void GenerateMixedArmyTask(Army army)
	{
		BaseNavyArmy navyArmy = base.GetNavyArmy(army);
		if (navyArmy == null)
		{
			return;
		}
		if (navyArmy.Commander == null)
		{
			return;
		}
		District district = this.worldPositionningService.GetDistrict(army.WorldPosition);
		if (district != null && district.Type != DistrictType.Exploitation && district.City.BesiegingEmpire != null)
		{
			return;
		}
		NavyTask_MixedArmy navyTask_MixedArmy = this.FindTask<NavyTask_MixedArmy>((NavyTask_MixedArmy match) => match.TargetGuid == army.GUID);
		if (navyTask_MixedArmy != null)
		{
			return;
		}
		navyTask_MixedArmy = new NavyTask_MixedArmy(this, navyArmy);
		navyArmy.Assign(navyTask_MixedArmy, new HeuristicValue(1f));
		this.NavyTasks.Add(navyTask_MixedArmy);
	}

	public override void ReadXml(XmlReader reader)
	{
		base.ReadXml(reader);
		if (reader.IsStartElement("NavyStateMachine"))
		{
			NavyStateMachine navyStateMachine = this.NavyStateMachine;
			reader.ReadElementSerializable<NavyStateMachine>("NavyStateMachine", ref navyStateMachine);
			this.NavyStateMachine = navyStateMachine;
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		base.WriteXml(writer);
		NavyStateMachine navyStateMachine = this.NavyStateMachine;
		writer.WriteElementSerializable<NavyStateMachine>("NavyStateMachine", ref navyStateMachine);
	}

	protected override void CreateNeededRegionData()
	{
		base.CreateNeededRegionData();
		for (int i = 0; i < base.NavyRegions.Count; i++)
		{
			NavyRegionData navyRegionData = base.NavyRegions[i] as NavyRegionData;
			if (navyRegionData != null)
			{
				this.CreateRoads(navyRegionData);
			}
		}
	}

	protected override BaseNavyRegionData InstanciateRegionData(Region waterRegion)
	{
		return new NavyRegionData(waterRegion);
	}

	protected override void UpdateRegionData()
	{
		for (int i = 0; i < base.NavyRegions.Count; i++)
		{
			NavyRegionData navyRegionData8 = base.NavyRegions[i] as NavyRegionData;
			if (navyRegionData8 != null)
			{
				navyRegionData8.EnemyNavalPower = 0f;
				navyRegionData8.MyNavalPower = 0f;
				foreach (Army army in Intelligence.GetArmiesInRegion(navyRegionData8.WaterRegionIndex))
				{
					if (army.Empire == base.AIEntity.Empire)
					{
						navyRegionData8.MyNavalPower += army.GetPropertyValue(SimulationProperties.MilitaryPower);
					}
					else if (this.departmentOfForeignAffairs.IsEnnemy(army.Empire))
					{
						navyRegionData8.EnemyNavalPower += army.GetPropertyValue(SimulationProperties.MilitaryPower);
						break;
					}
				}
				navyRegionData8.RegionScore.Reset();
				if (navyRegionData8.RegionFortress.Count > 0)
				{
					navyRegionData8.RegionScore.Boost(0.3f, "(constant) There is fortress", new object[0]);
				}
				for (int j = 0; j < navyRegionData8.NavyRoads.Count; j++)
				{
					this.UpdateRoadData(navyRegionData8, navyRegionData8.NavyRoads[j]);
				}
				navyRegionData8.ForteressOwned = 0;
				for (int k = 0; k < navyRegionData8.RegionFortress.Count; k++)
				{
					if (navyRegionData8.RegionFortress[k].Occupant == base.AIEntity.Empire)
					{
						NavyRegionData navyRegionData2 = navyRegionData8;
						int forteressOwned = navyRegionData2.ForteressOwned;
						navyRegionData2.ForteressOwned = forteressOwned + 1;
						navyRegionData8.RegionScore.Boost(0.15f, "(constant) We have a fortress", new object[0]);
					}
				}
				navyRegionData8.NumberOfMyCityOnTheBorder = 0;
				navyRegionData8.NumberOfEnemyCityOnTheBorder = 0;
				navyRegionData8.NumberOfMyLandRegionAround = 0;
				for (int l = 0; l < navyRegionData8.NeighbouringLandRegions.Count; l++)
				{
					if (navyRegionData8.NeighbouringLandRegions[l].City != null)
					{
						global::Empire empire = navyRegionData8.NeighbouringLandRegions[l].City.Empire;
						if (empire != null && empire == base.AIEntity.Empire)
						{
							NavyRegionData navyRegionData3 = navyRegionData8;
							int numberOfMyLandRegionAround = navyRegionData3.NumberOfMyLandRegionAround;
							navyRegionData3.NumberOfMyLandRegionAround = numberOfMyLandRegionAround + 1;
						}
						AIData_City aidata_City = null;
						if (this.aiDataRepositoryHelper.TryGetAIData<AIData_City>(navyRegionData8.NeighbouringLandRegions[l].City.GUID, out aidata_City) && aidata_City.NeighbourgRegions.Contains(navyRegionData8.WaterRegionIndex))
						{
							if (aidata_City.City.Empire == base.AIEntity.Empire)
							{
								NavyRegionData navyRegionData4 = navyRegionData8;
								int numberOfMyCityOnTheBorder = navyRegionData4.NumberOfMyCityOnTheBorder;
								navyRegionData4.NumberOfMyCityOnTheBorder = numberOfMyCityOnTheBorder + 1;
								navyRegionData8.RegionScore.Boost(0.3f, "(constant) I have a city in this ocean. {0}", new object[]
								{
									aidata_City.City.LocalizedName
								});
							}
							else if (this.departmentOfForeignAffairs.IsEnnemy(aidata_City.City.Empire))
							{
								NavyRegionData navyRegionData5 = navyRegionData8;
								int numberOfEnemyCityOnTheBorder = navyRegionData5.NumberOfEnemyCityOnTheBorder;
								navyRegionData5.NumberOfEnemyCityOnTheBorder = numberOfEnemyCityOnTheBorder + 1;
							}
						}
					}
				}
				if (navyRegionData8.NumberOfMyLandRegionAround > 0)
				{
					navyRegionData8.RegionScore.Boost(0.05f, "Near owned land region.", new object[0]);
				}
				navyRegionData8.MayTakeRegionOver = true;
				global::Empire owner = navyRegionData8.WaterRegion.Owner;
				if (owner != null)
				{
					navyRegionData8.MayTakeRegionOver = this.MightAttackOwner(navyRegionData8.WaterRegion, owner);
				}
			}
		}
		for (int m = 0; m < base.NavyRegions.Count; m++)
		{
			NavyRegionData navyRegionData = base.NavyRegions[m] as NavyRegionData;
			if (navyRegionData != null)
			{
				navyRegionData.NumberOfWaterEnemy = 0;
				bool flag = false;
				bool flag2 = false;
				int num;
				int neighbourgIndex;
				Predicate<BaseNavyRegionData> <>9__0;
				for (neighbourgIndex = 0; neighbourgIndex < navyRegionData.NeighbouringWaterRegions.Count; neighbourgIndex = num + 1)
				{
					List<BaseNavyRegionData> navyRegions = base.NavyRegions;
					Predicate<BaseNavyRegionData> match2;
					if ((match2 = <>9__0) == null)
					{
						match2 = (<>9__0 = ((BaseNavyRegionData match) => match.WaterRegionIndex == navyRegionData.NeighbouringWaterRegions[neighbourgIndex].Index));
					}
					NavyRegionData navyRegionData6 = navyRegions.Find(match2) as NavyRegionData;
					if (navyRegionData6 != null)
					{
						if (navyRegionData6.NumberOfMyCityOnTheBorder > 0)
						{
							flag = true;
						}
						global::Empire owner2 = navyRegionData6.WaterRegion.Owner;
						if (owner2 != null)
						{
							if (owner2 == base.AIEntity.Empire)
							{
								flag2 = true;
							}
							else if (!this.departmentOfForeignAffairs.IsFriend(owner2))
							{
								NavyRegionData navyRegionData7 = navyRegionData;
								num = navyRegionData7.NumberOfWaterEnemy;
								navyRegionData7.NumberOfWaterEnemy = num + 1;
							}
						}
					}
					num = neighbourgIndex;
				}
				if (flag2)
				{
					navyRegionData.RegionScore.Boost(0.15f, "Near owned water region.", new object[0]);
				}
				if (flag)
				{
					navyRegionData.RegionScore.Boost(0.2f, "Near water region with city.", new object[0]);
				}
			}
		}
	}

	private void CreateRoads(NavyRegionData regionData)
	{
		regionData.NavyRoads.Clear();
		for (int i = 0; i < regionData.NeighbouringLandRegions.Count; i++)
		{
			for (int j = i + 1; j < regionData.NeighbouringLandRegions.Count; j++)
			{
				NavyRoad navyRoad = new NavyRoad();
				navyRoad.RegionIndex1 = regionData.NeighbouringLandRegions[i].Index;
				navyRoad.RegionIndex2 = regionData.NeighbouringLandRegions[j].Index;
				regionData.NavyRoads.Add(navyRoad);
			}
		}
	}

	private void UpdateRoadData(NavyRegionData regionData, NavyRoad navyRoad)
	{
		Region region = this.worldPositionningService.World.Regions[navyRoad.RegionIndex1];
		Region region2 = this.worldPositionningService.World.Regions[navyRoad.RegionIndex2];
		navyRoad.WaterRegionType = this.GetRegionOwnerState(regionData.WaterRegion);
		NavyRoad.RegionOwnerState regionOwnerState = this.GetRegionOwnerState(region);
		NavyRoad.RegionOwnerState regionOwnerState2 = this.GetRegionOwnerState(region2);
		if (regionOwnerState == regionOwnerState2)
		{
			if (regionOwnerState == NavyRoad.RegionOwnerState.Neutral)
			{
				navyRoad.RoadType = NavyRoad.WaterRoadTypes.BothNeutral;
				return;
			}
			if (regionOwnerState == NavyRoad.RegionOwnerState.Allied)
			{
				navyRoad.RoadType = NavyRoad.WaterRoadTypes.BothAllied;
				regionData.RegionScore.Boost(0.1f, "Both allied", new object[0]);
				return;
			}
			if (regionOwnerState == NavyRoad.RegionOwnerState.Enemy)
			{
				navyRoad.RoadType = NavyRoad.WaterRoadTypes.BothEnemy;
				return;
			}
		}
		else
		{
			if (regionOwnerState == NavyRoad.RegionOwnerState.Enemy || regionOwnerState2 == NavyRoad.RegionOwnerState.Enemy)
			{
				navyRoad.RoadType = NavyRoad.WaterRoadTypes.OneEnemy;
				return;
			}
			if (regionOwnerState == NavyRoad.RegionOwnerState.Allied || regionOwnerState2 == NavyRoad.RegionOwnerState.Allied)
			{
				navyRoad.RoadType = NavyRoad.WaterRoadTypes.OneAllied;
				return;
			}
			Diagnostics.LogError("invalid owner state. FirstRegion[{0}] = {1}, OtherRegion[{2}] = {3}.", new object[]
			{
				region.Index,
				regionOwnerState,
				region2.Index,
				regionOwnerState2
			});
		}
	}

	private void UpdateRegionDataThreat(NavyRegionData regionData)
	{
	}

	private NavyRoad.RegionOwnerState GetRegionOwnerState(Region region)
	{
		if (region.Owner == null)
		{
			return NavyRoad.RegionOwnerState.Neutral;
		}
		if (region.Owner.Index == base.AIEntity.Empire.Index)
		{
			return NavyRoad.RegionOwnerState.Allied;
		}
		return NavyRoad.RegionOwnerState.Enemy;
	}

	public ArmyRatioLimit CurrentRatioLimit { get; set; }

	public bool IsNavalEmpireHostile { get; set; }

	public HeuristicValue NavyImportance { get; set; }

	public NavyStateMachine NavyStateMachine { get; set; }

	public List<BaseNavyTask> NavyTasks
	{
		get
		{
			return this.navyTasks;
		}
	}

	public float WantedArmies()
	{
		float num = 0f;
		for (int i = 0; i < base.NavyCommanders.Count; i++)
		{
			NavyCommander navyCommander = base.NavyCommanders[i] as NavyCommander;
			if (navyCommander != null && navyCommander.CommanderState != NavyCommander.NavyCommanderState.Inactive)
			{
				num += navyCommander.WantedNumberOfArmies();
			}
		}
		if (num > 0f)
		{
			return num;
		}
		return 0f;
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		this.NavyImportance = new HeuristicValue(0f);
		this.departmentOfForeignAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		InfluencedByPersonalityAttribute.LoadFieldAndPropertyValues(base.AIEntity.Empire, this);
		IDatabase<Amplitude.Unity.Framework.AnimationCurve> database = Databases.GetDatabase<Amplitude.Unity.Framework.AnimationCurve>(false);
		if (!string.IsNullOrEmpty(this.gameProgressCurveName))
		{
			this.gameProgressCurve = database.GetValue(this.gameProgressCurveName);
		}
		this.NavyStateMachine = new NavyStateMachine(this);
		this.NavyStateMachine.Initialize();
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		this.departmentOfTheInterior.OccupiedFortressesCollectionChanged += this.DepartmentOfTheInterior_OccupiedFortressesCollectionChanged;
		for (int i = 0; i < this.game.Empires.Length; i++)
		{
			if (i != base.AIEntity.Empire.Index)
			{
				DepartmentOfTransportation agency = this.game.Empires[i].GetAgency<DepartmentOfTransportation>();
				agency.ArmyPositionChange += this.DepartmentOfTransport_ArmyPositionChange;
				this.enemyTransportation.Add(agency);
			}
		}
		this.diplomacyLayer = base.AIEntity.GetLayer<AILayer_Diplomacy>();
		yield break;
	}

	public override IEnumerator Load()
	{
		yield return base.Load();
		int i = 0;
		while (i < this.game.Empires.Length)
		{
			NavalEmpire navalEmpire = this.game.Empires[i] as NavalEmpire;
			if (navalEmpire != null)
			{
				this.pirateCouncil = navalEmpire.GetAgency<PirateCouncil>();
				AIPlayer_NavalEmpire aiplayer_NavalEmpire;
				if (((Services.GetService<ISessionService>().Session as global::Session).GameServer as GameServer).AIScheduler.TryGetNavalEmpireAIPlayer(out aiplayer_NavalEmpire))
				{
					AIEntity_NavalEmpire entity = aiplayer_NavalEmpire.GetEntity<AIEntity_NavalEmpire>();
					this.raiderLayer = entity.GetLayer<AILayer_Raiders>();
					break;
				}
				break;
			}
			else
			{
				i++;
			}
		}
		yield break;
	}

	public override void Release()
	{
		base.Release();
		this.departmentOfForeignAffairs = null;
		if (this.NavyStateMachine != null)
		{
			this.NavyStateMachine.Release();
			this.NavyStateMachine = null;
		}
		if (this.departmentOfTheInterior != null)
		{
			this.departmentOfTheInterior.OccupiedFortressesCollectionChanged -= this.DepartmentOfTheInterior_OccupiedFortressesCollectionChanged;
			this.departmentOfTheInterior = null;
		}
		for (int i = 0; i < this.enemyTransportation.Count; i++)
		{
			this.enemyTransportation[i].ArmyPositionChange -= this.DepartmentOfTransport_ArmyPositionChange;
		}
		this.enemyTransportation.Clear();
		this.diplomacyLayer = null;
	}

	public bool MightAttackOwner(Region targetRegion, global::Empire targetOwner)
	{
		if (targetOwner.Index == base.AIEntity.Empire.Index)
		{
			return false;
		}
		DepartmentOfForeignAffairs agency = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		if (agency != null)
		{
			DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(targetOwner);
			if (diplomaticRelation != null)
			{
				if (targetRegion.Owner == targetOwner)
				{
					if (diplomaticRelation.State.Name != DiplomaticRelationState.Names.War)
					{
						return false;
					}
				}
				else if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.Alliance || diplomaticRelation.State.Name == DiplomaticRelationState.Names.Peace || diplomaticRelation.State.Name == DiplomaticRelationState.Names.Truce)
				{
					return false;
				}
			}
		}
		return true;
	}

	public U FindTask<U>(Func<U, bool> match) where U : BaseNavyTask
	{
		for (int i = 0; i < this.navyTasks.Count; i++)
		{
			U u = this.navyTasks[i] as U;
			if (u != null && (match == null || match(u)))
			{
				return u;
			}
		}
		return default(U);
	}

	protected override void RefreshObjectives(StaticString context, StaticString pass)
	{
		this.UpdateUnitRatio();
		base.RefreshObjectives(context, pass);
		for (int i = 0; i < base.NavyCommanders.Count; i++)
		{
			NavyCommander navyCommander = base.NavyCommanders[i] as NavyCommander;
			if (navyCommander != null)
			{
				for (int j = navyCommander.NavyFortresses.Count - 1; j >= 0; j--)
				{
					if (navyCommander.NavyFortresses[j].Fortress.Occupant != base.AIEntity.Empire)
					{
						navyCommander.NavyFortresses[j].Release();
					}
				}
			}
		}
		if (this.pirateCouncil != null)
		{
			for (int k = 0; k < this.pirateCouncil.Fortresses.Count; k++)
			{
				Fortress fortress = this.pirateCouncil.Fortresses[k];
				if (fortress.Occupant == base.AIEntity.Empire)
				{
					NavyCommander navyCommander2 = base.NavyCommanders.Find((BaseNavyCommander match) => match.RegionData.WaterRegionIndex == fortress.Region.Index) as NavyCommander;
					if (navyCommander2 != null && navyCommander2.NavyFortresses.Find((NavyFortress match) => match.Fortress.GUID == fortress.GUID) == null)
					{
						NavyFortress navyFortress = new NavyFortress(this, fortress);
						navyFortress.Initialize();
						navyFortress.IsActive = this.IsActive();
						navyFortress.AssignCommander(navyCommander2);
						navyFortress.UnitRatioLimit = this.CurrentRatioLimit;
					}
				}
			}
		}
		for (int l = 0; l < base.NavyCommanders.Count; l++)
		{
			NavyCommander navyCommander3 = base.NavyCommanders[l] as NavyCommander;
			if (navyCommander3 != null)
			{
				for (int m = navyCommander3.NavyFortresses.Count - 1; m >= 0; m--)
				{
					this.RefreshFortress(navyCommander3.NavyFortresses[m]);
				}
			}
		}
		this.NavyStateMachine.Update();
		this.ComputeNavyImportance();
	}

	protected override void RefreshCommanders(StaticString context, StaticString pass)
	{
		if (!this.IsNavalEmpireHostile && this.raiderLayer != null && this.raiderLayer.NavyRegions.Count > 0)
		{
			RaidersRegionData raidersRegionData = this.raiderLayer.NavyRegions[0] as RaidersRegionData;
			if (raidersRegionData != null && raidersRegionData.CurrentRegionState != null)
			{
				this.IsNavalEmpireHostile = (raidersRegionData.CurrentRegionState.Name != RaiderState_Curious.ReadonlyName);
			}
		}
		for (int i = this.navyTasks.Count - 1; i >= 0; i--)
		{
			if (!this.navyTasks[i].CheckValidity())
			{
				if (this.navyTasks[i].AssignedArmy != null)
				{
					this.NavyTasks[i].AssignedArmy.Unassign();
				}
				this.navyTasks.RemoveAt(i);
			}
		}
		for (int j = 0; j < base.NavyCommanders.Count; j++)
		{
			NavyCommander navyCommander = base.NavyCommanders[j] as NavyCommander;
			if (navyCommander != null && navyCommander.WantedNumberOfArmies() > (float)navyCommander.NumberOfMediumSizedArmies)
			{
				this.TryStealFromNeighbourgs(navyCommander);
			}
		}
		base.RefreshCommanders(context, pass);
		for (int k = 0; k < base.NavyCommanders.Count; k++)
		{
			NavyCommander navyCommander2 = base.NavyCommanders[k] as NavyCommander;
			if (navyCommander2 != null)
			{
				for (int l = navyCommander2.NavyFortresses.Count - 1; l >= 0; l--)
				{
					navyCommander2.NavyFortresses[l].ValidateMainTask();
				}
			}
		}
	}

	protected override void RefreshArmy(BaseNavyArmy army)
	{
		NavyCommander navyCommander = army.Commander as NavyCommander;
		if (navyCommander != null && this.IsActive() && (navyCommander.CommanderState == NavyCommander.NavyCommanderState.Inactive || (float)navyCommander.NumberOfMediumSizedArmies > navyCommander.WantedNumberOfArmies()))
		{
			army.AssignCommander(this.FindArmyCommander(army));
		}
		army.UnitRatioLimit = this.CurrentRatioLimit;
		base.RefreshArmy(army);
	}

	protected virtual void RefreshFortress(NavyFortress fortress)
	{
		fortress.UnitRatioLimit = this.CurrentRatioLimit;
		fortress.UpdateState();
		fortress.UpdateRole();
	}

	protected override BaseNavyCommander FindArmyCommander(BaseNavyArmy army)
	{
		if (this.IsActive())
		{
			float num = 2.14748365E+09f;
			BaseNavyCommander baseNavyCommander = null;
			for (int i = 0; i < base.NavyCommanders.Count; i++)
			{
				float num2 = (float)this.worldPositionningService.GetDistance(army.Garrison.WorldPosition, base.NavyCommanders[i].RegionData.WaterRegion.Barycenter);
				NavyCommander navyCommander = base.NavyCommanders[i] as NavyCommander;
				if (navyCommander != null && navyCommander.CommanderState != NavyCommander.NavyCommanderState.Inactive)
				{
					float num3 = navyCommander.WantedNumberOfArmies();
					float num4 = (float)navyCommander.NumberOfMediumSizedArmies;
					if (num3 < num4)
					{
						num2 *= 10f;
					}
					num2 *= (1f - navyCommander.CommanderArmyNeed()) * 2f;
					if (army.Role == BaseNavyArmy.ArmyRole.Renfort && navyCommander.CommanderState != NavyCommander.NavyCommanderState.BuildUp && navyCommander.CommanderState != NavyCommander.NavyCommanderState.Defense)
					{
						num2 *= 2f;
					}
					if (num2 < num)
					{
						num = num2;
						baseNavyCommander = base.NavyCommanders[i];
					}
				}
			}
			if (baseNavyCommander != null)
			{
				return baseNavyCommander;
			}
		}
		return base.FindArmyCommander(army);
	}

	protected override void AIPlayer_AIPlayerStateChange(object sender, EventArgs e)
	{
		base.AIPlayer_AIPlayerStateChange(sender, e);
		for (int i = 0; i < base.NavyCommanders.Count; i++)
		{
			NavyCommander navyCommander = base.NavyCommanders[i] as NavyCommander;
			if (navyCommander != null)
			{
				for (int j = navyCommander.NavyFortresses.Count - 1; j >= 0; j--)
				{
					navyCommander.NavyFortresses[j].IsActive = this.IsActive();
				}
			}
		}
		if (!this.IsActive())
		{
			this.navyTasks.RemoveAll((BaseNavyTask match) => !(match is NavyTask_AutoAction));
		}
	}

	private void ComputeNavyImportance()
	{
		this.NavyImportance.Reset();
		this.NavyImportance.Add(0.3f, "Constant", new object[0]);
		bool flag = false;
		DepartmentOfTheInterior agency = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		for (int i = 0; i < agency.Cities.Count; i++)
		{
			if (!this.myCityContinentId.Contains(agency.Cities[i].Region.ContinentID))
			{
				this.myCityContinentId.Add(agency.Cities[i].Region.ContinentID);
			}
			flag |= (agency.Cities[i].BesiegingEmpire != null);
		}
		bool flag2 = this.myCityContinentId.Count > 1;
		AILayer_Colonization layer = base.AIEntity.GetLayer<AILayer_Colonization>();
		if (layer != null && layer.WantToColonizeOversea)
		{
			flag2 = true;
		}
		if (flag2)
		{
			if (base.AIEntity.Empire.GetAgency<DepartmentOfDefense>().UnitDesignDatabase.AvailableUnitBodyDefinitions.Any((UnitBodyDefinition match) => match.Tags.Contains("Seafaring")))
			{
				this.NavyImportance.Boost(0.1f, "Cities spread on more than one continent.", new object[0]);
			}
			else
			{
				this.NavyImportance.Boost(0.3f, "Cities spread on more than one continent.", new object[0]);
			}
		}
		if (flag)
		{
			this.NavyImportance.Boost(-0.3f, "Some cities are besieged.", new object[0]);
		}
		this.NavyImportance.Boost(this.ComputeBasedOnOtherFactor(), "Based on other", new object[0]);
		this.NavyImportance.Boost(this.ComputeGameProgressFactor(), "Based on game progress", new object[0]);
		this.NavyImportance.Boost(this.ComputeSeaPercentageFactor(), "Based on sea percentage", new object[0]);
		float operand = this.GetHighestArmyNeedPriority() - 0.5f;
		this.NavyImportance.Boost(operand, "Highest commander priority with missing army.", new object[0]);
	}

	private HeuristicValue ComputeBasedOnOtherFactor()
	{
		HeuristicValue heuristicValue = new HeuristicValue(0f);
		if (this.pirateCouncil != null && this.departmentOfTheInterior.OccupiedFortresses.Count == 0)
		{
			if (this.pirateCouncil.Fortresses.Count((Fortress match) => match.IsOccupied) > 0)
			{
				heuristicValue.Add(0.2f, "(constant) Someone has a fortress and I don't.", new object[0]);
			}
		}
		bool flag = false;
		for (int i = 0; i < this.game.Empires.Length; i++)
		{
			if (i != base.AIEntity.Empire.Index)
			{
				DepartmentOfTheInterior agency = this.game.Empires[i].GetAgency<DepartmentOfTheInterior>();
				if (agency != null && agency.Cities.Any((City match) => this.myCityContinentId.Contains(match.Region.ContinentID)))
				{
					flag = true;
					break;
				}
			}
		}
		if (flag)
		{
			heuristicValue.Subtract(0.2f, "(constant) Someone on my continent, focus on military", new object[0]);
		}
		else
		{
			heuristicValue.Add(0.2f, "(constant) Nobody on my continent, focus on naval!", new object[0]);
		}
		return heuristicValue;
	}

	private HeuristicValue ComputeGameProgressFactor()
	{
		HeuristicValue heuristicValue = new HeuristicValue(0f);
		HeuristicValue heuristicValue2 = new HeuristicValue(0f);
		heuristicValue2.Add((float)this.game.Turn, "Current turn", new object[0]);
		heuristicValue2.Subtract((float)this.gameProgressMinTurn, "Min turn to kick in", new object[0]);
		heuristicValue2.Divide((float)(this.gameProgressMaxTurn - this.gameProgressMinTurn), "Max turn {0} - Min turn {1}", new object[]
		{
			this.gameProgressMaxTurn,
			this.gameProgressMinTurn
		});
		heuristicValue2.Clamp01();
		heuristicValue2.Curve(this.gameProgressCurve);
		heuristicValue.Add(heuristicValue2, "Game progress curve value", new object[0]);
		heuristicValue.Multiply(this.basedOnGameProgressFactor, "Xml factor", new object[0]);
		return heuristicValue;
	}

	private HeuristicValue ComputeSeaPercentageFactor()
	{
		HeuristicValue heuristicValue = new HeuristicValue(0f);
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < this.game.World.Regions.Length; i++)
		{
			if (!this.game.World.Regions[i].IsWasteland)
			{
				int num3 = this.game.World.Regions[i].WorldPositions.Length;
				num += (float)num3;
				if (this.game.World.Regions[i].IsOcean)
				{
					num2 += (float)num3;
				}
			}
		}
		float operand = num2 / num;
		heuristicValue.Add(operand, "World sea percentage", new object[0]);
		heuristicValue.Subtract(0.4f, "Percent limit", new object[0]);
		heuristicValue.Multiply(this.basedOnSeaPercentageFactor, "Xml factor", new object[0]);
		return heuristicValue;
	}

	private float GetHighestArmyNeedPriority()
	{
		float num = 0f;
		for (int i = 0; i < base.NavyCommanders.Count; i++)
		{
			NavyCommander navyCommander = base.NavyCommanders[i] as NavyCommander;
			if (navyCommander != null && navyCommander.CommanderState != NavyCommander.NavyCommanderState.Inactive)
			{
				float num2 = navyCommander.WantedNumberOfArmies();
				float num3 = (float)navyCommander.NumberOfMediumSizedArmies;
				if (num2 >= num3)
				{
					float num4 = navyCommander.CommanderArmyNeed();
					if (num < num4)
					{
						num = num4;
					}
				}
			}
		}
		return num;
	}

	private void TryStealFromNeighbourgs(NavyCommander commander)
	{
		float num = commander.CommanderArmyNeed();
		int numberOfMediumSizedArmies = commander.NumberOfMediumSizedArmies;
		for (int i = 0; i < commander.NeighbouringCommanders.Count; i++)
		{
			NavyCommander navyCommander = commander.NeighbouringCommanders[i] as NavyCommander;
			if (navyCommander != null)
			{
				int numberOfMediumSizedArmies2 = navyCommander.NumberOfMediumSizedArmies;
				if (numberOfMediumSizedArmies <= numberOfMediumSizedArmies2 && (navyCommander.CommanderArmyNeed() <= num || (float)numberOfMediumSizedArmies2 >= navyCommander.WantedNumberOfArmies()))
				{
					for (int j = 0; j < navyCommander.NavyArmies.Count; j++)
					{
						if (navyCommander.NavyArmies[j].Role == BaseNavyArmy.ArmyRole.TaskForce)
						{
							navyCommander.NavyArmies[j].AssignCommander(commander);
							return;
						}
					}
				}
			}
		}
	}

	private void UpdateUnitRatio()
	{
		float unitRatioForFull = 1f;
		float num = 0.75f;
		float num2 = 0.25f;
		float num3 = 0f;
		for (int i = 0; i < this.game.Empires.Length; i++)
		{
			if (i != base.AIEntity.Empire.Index && this.departmentOfForeignAffairs.IsEnnemy(this.game.Empires[i]))
			{
				float bestNavyArmyPower = this.GetBestNavyArmyPower(this.game.Empires[i]);
				if (bestNavyArmyPower > num3)
				{
					num3 = bestNavyArmyPower;
				}
			}
		}
		float bestPirateFortress = this.GetBestPirateFortress();
		if (bestPirateFortress > num3)
		{
			num3 = bestPirateFortress;
		}
		float averageMilitaryPower = base.AIEntity.GetLayer<AILayer_UnitRecruitment>().NavalRecruiter.AverageMilitaryPower;
		float num4 = base.AIEntity.Empire.GetPropertyValue(SimulationProperties.ArmyUnitSlot) * averageMilitaryPower;
		if (num4 > num3)
		{
			float num5 = num3 / num4;
			num5 = Mathf.Clamp(num5, 0.5f, 1f);
			num *= num5;
			num2 *= num5;
		}
		this.CurrentRatioLimit = new ArmyRatioLimit(num, num2, unitRatioForFull);
	}

	private float GetBestNavyArmyPower(global::Empire empire)
	{
		float num = 0f;
		DepartmentOfDefense agency = empire.GetAgency<DepartmentOfDefense>();
		for (int i = 0; i < agency.Armies.Count; i++)
		{
			if (agency.Armies[i].IsNaval)
			{
				float propertyValue = agency.Armies[i].GetPropertyValue(SimulationProperties.MilitaryPower);
				if (propertyValue > num)
				{
					num = propertyValue;
				}
			}
		}
		DepartmentOfTheInterior agency2 = empire.GetAgency<DepartmentOfTheInterior>();
		if (agency2 != null)
		{
			for (int j = 0; j < agency2.OccupiedFortresses.Count; j++)
			{
				float propertyValue2 = agency2.OccupiedFortresses[j].GetPropertyValue(SimulationProperties.MilitaryPower);
				if (propertyValue2 > num)
				{
					num = propertyValue2;
				}
			}
		}
		return num;
	}

	private float GetBestPirateFortress()
	{
		if (this.pirateCouncil == null)
		{
			return 0f;
		}
		float num = 0f;
		for (int i = 0; i < this.pirateCouncil.Fortresses.Count; i++)
		{
			if (!this.pirateCouncil.Fortresses[i].IsOccupied)
			{
				float propertyValue = this.pirateCouncil.Fortresses[i].GetPropertyValue(SimulationProperties.MilitaryPower);
				if (propertyValue > num)
				{
					num = propertyValue;
				}
			}
		}
		return num;
	}

	private void DepartmentOfTheInterior_OccupiedFortressesCollectionChanged(object sender, CollectionChangeEventArgs e)
	{
		Fortress fortress = e.Element as Fortress;
		if (e.Action != CollectionChangeAction.Add)
		{
			if (e.Action == CollectionChangeAction.Remove)
			{
				Predicate<NavyFortress> <>9__2;
				for (int i = 0; i < base.NavyCommanders.Count; i++)
				{
					NavyCommander navyCommander = base.NavyCommanders[i] as NavyCommander;
					if (navyCommander != null)
					{
						List<NavyFortress> navyFortresses = navyCommander.NavyFortresses;
						Predicate<NavyFortress> match2;
						if ((match2 = <>9__2) == null)
						{
							match2 = (<>9__2 = ((NavyFortress match) => match.Fortress.GUID == fortress.GUID));
						}
						NavyFortress navyFortress = navyFortresses.Find(match2);
						if (navyFortress != null)
						{
							navyFortress.Release();
						}
					}
				}
			}
			return;
		}
		NavyCommander navyCommander2 = base.NavyCommanders.Find((BaseNavyCommander match) => match.RegionData.WaterRegionIndex == fortress.Region.Index) as NavyCommander;
		if (navyCommander2 == null)
		{
			return;
		}
		NavyFortress navyFortress2 = navyCommander2.NavyFortresses.Find((NavyFortress match) => match.Fortress.GUID == fortress.GUID);
		if (navyFortress2 == null)
		{
			navyFortress2 = new NavyFortress(this, fortress);
			navyFortress2.Initialize();
			navyFortress2.IsActive = this.IsActive();
		}
		navyFortress2.AssignCommander(navyCommander2);
		this.RefreshFortress(navyFortress2);
		navyCommander2.GenerateFillFortress(fortress);
	}

	private void DepartmentOfTransport_ArmyPositionChange(object sender, ArmyMoveEndedEventArgs e)
	{
		if (!this.IsActive())
		{
			return;
		}
		Army army = e.Army;
		if (army == null)
		{
			return;
		}
		Region region = this.worldPositionningService.GetRegion(e.From);
		Region region2 = this.worldPositionningService.GetRegion(e.To);
		if (region.Index == region2.Index)
		{
			return;
		}
		if (!this.worldPositionningService.IsWaterTile(e.To) || region2.IsOcean)
		{
			return;
		}
		if (region.IsOcean)
		{
			return;
		}
		if (this.FindTask<NavyTask_Interception>((NavyTask_Interception match) => match.TargetGuid == army.GUID) != null)
		{
			return;
		}
		this.GenerateInterceptionTaskForEmpire(army);
	}

	public const string RegistryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_Navy";

	private const float MinimumWarDesire = 0.25f;

	private const float MaximumAllyDesire = 0.5f;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private PirateCouncil pirateCouncil;

	private AILayer_Raiders raiderLayer;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private List<int> myCityContinentId = new List<int>();

	[InfluencedByPersonality]
	private float basedOnGameProgressFactor = 0.5f;

	[InfluencedByPersonality]
	private float basedOnSeaPercentageFactor = 0.5f;

	[InfluencedByPersonality]
	private int gameProgressMinTurn = 10;

	[InfluencedByPersonality]
	private int gameProgressMaxTurn = 200;

	[InfluencedByPersonality]
	private string gameProgressCurveName = string.Empty;

	private List<DepartmentOfTransportation> enemyTransportation = new List<DepartmentOfTransportation>();

	private Amplitude.Unity.Framework.AnimationCurve gameProgressCurve;

	private List<BaseNavyTask> navyTasks = new List<BaseNavyTask>();

	public AILayer_Diplomacy diplomacyLayer;
}
