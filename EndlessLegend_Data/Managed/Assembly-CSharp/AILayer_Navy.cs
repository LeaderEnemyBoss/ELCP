using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
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
			if (navyCommander != null)
			{
				if (navyCommander.CommanderState != NavyCommander.NavyCommanderState.Inactive)
				{
					if (num2 < num)
					{
						num = num2;
						result = base.NavyCommanders[i];
					}
				}
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
				goto IL_74;
			}
			DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.DiplomaticRelations[i];
			if (diplomaticRelation.State == null || (!(diplomaticRelation.State.Name == DiplomaticRelationState.Names.Peace) && !(diplomaticRelation.State.Name == DiplomaticRelationState.Names.Alliance)))
			{
				goto IL_74;
			}
			IL_DC:
			i++;
			continue;
			IL_74:
			if (this.game.Empires[i] is NavalEmpire && !this.IsNavalEmpireHostile)
			{
				goto IL_DC;
			}
			DepartmentOfDefense agency = this.game.Empires[i].GetAgency<DepartmentOfDefense>();
			for (int j = 0; j < agency.Armies.Count; j++)
			{
				this.GenerateArmyBasedTask(agency.Armies[j]);
			}
			goto IL_DC;
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
		if (base.AIEntity.Empire.Index == army.Empire.Index)
		{
			if (army.HasSeafaringUnits() && !army.HasOnlySeafaringUnits(false))
			{
				this.GenerateMixedArmyTask(army);
			}
			else
			{
				this.GenerateReinforcementTasks(army);
			}
		}
		else
		{
			this.GenerateInterceptionTaskForEmpire(army);
		}
	}

	private void GenerateInterceptionTaskForEmpire(Army army)
	{
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
			NavyRegionData navyRegionData3 = base.NavyRegions[i] as NavyRegionData;
			if (navyRegionData3 != null)
			{
				navyRegionData3.RegionScore.Reset();
				if (navyRegionData3.RegionFortress.Count > 0)
				{
					navyRegionData3.RegionScore.Boost(0.3f, "(constant) There is fortress", new object[0]);
				}
				for (int j = 0; j < navyRegionData3.NavyRoads.Count; j++)
				{
					this.UpdateRoadData(navyRegionData3, navyRegionData3.NavyRoads[j]);
				}
				navyRegionData3.ForteressOwned = 0;
				for (int k = 0; k < navyRegionData3.RegionFortress.Count; k++)
				{
					if (navyRegionData3.RegionFortress[k].Occupant == base.AIEntity.Empire)
					{
						navyRegionData3.ForteressOwned++;
						navyRegionData3.RegionScore.Boost(0.15f, "(constant) We have a fortress", new object[0]);
					}
				}
				navyRegionData3.NumberOfMyCityOnTheBorder = 0;
				navyRegionData3.NumberOfEnemyCityOnTheBorder = 0;
				navyRegionData3.NumberOfMyLandRegionAround = 0;
				for (int l = 0; l < navyRegionData3.NeighbouringLandRegions.Count; l++)
				{
					if (navyRegionData3.NeighbouringLandRegions[l].City != null)
					{
						Empire empire = navyRegionData3.NeighbouringLandRegions[l].City.Empire;
						if (empire != null && empire == base.AIEntity.Empire)
						{
							navyRegionData3.NumberOfMyLandRegionAround++;
						}
						AIData_City aidata_City = null;
						if (this.aiDataRepositoryHelper.TryGetAIData<AIData_City>(navyRegionData3.NeighbouringLandRegions[l].City.GUID, out aidata_City))
						{
							if (aidata_City.NeighbourgRegions.Contains(navyRegionData3.WaterRegionIndex))
							{
								if (aidata_City.City.Empire == base.AIEntity.Empire)
								{
									navyRegionData3.NumberOfMyCityOnTheBorder++;
									navyRegionData3.RegionScore.Boost(0.3f, "(constant) I have a city in this ocean. {0}", new object[]
									{
										aidata_City.City.LocalizedName
									});
								}
								else if (this.departmentOfForeignAffairs.IsEnnemy(aidata_City.City.Empire))
								{
									navyRegionData3.NumberOfEnemyCityOnTheBorder++;
								}
							}
						}
					}
				}
				if (navyRegionData3.NumberOfMyLandRegionAround > 0)
				{
					navyRegionData3.RegionScore.Boost(0.05f, "Near owned land region.", new object[0]);
				}
				navyRegionData3.MayTakeRegionOver = true;
				Empire owner = navyRegionData3.WaterRegion.Owner;
				if (owner != null)
				{
					navyRegionData3.MayTakeRegionOver = this.MightAttackOwner(navyRegionData3.WaterRegion, owner);
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
				int neighbourgIndex;
				for (neighbourgIndex = 0; neighbourgIndex < navyRegionData.NeighbouringWaterRegions.Count; neighbourgIndex++)
				{
					NavyRegionData navyRegionData2 = base.NavyRegions.Find((BaseNavyRegionData match) => match.WaterRegionIndex == navyRegionData.NeighbouringWaterRegions[neighbourgIndex].Index) as NavyRegionData;
					if (navyRegionData2 != null)
					{
						if (navyRegionData2.NumberOfMyCityOnTheBorder > 0)
						{
							flag = true;
						}
						Empire owner2 = navyRegionData2.WaterRegion.Owner;
						if (owner2 != null)
						{
							if (owner2 == base.AIEntity.Empire)
							{
								flag2 = true;
							}
							else if (!this.departmentOfForeignAffairs.IsFriend(owner2))
							{
								navyRegionData.NumberOfWaterEnemy++;
							}
						}
					}
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
			}
			else if (regionOwnerState == NavyRoad.RegionOwnerState.Allied)
			{
				navyRoad.RoadType = NavyRoad.WaterRoadTypes.BothAllied;
				regionData.RegionScore.Boost(0.1f, "Both allied", new object[0]);
			}
			else if (regionOwnerState == NavyRoad.RegionOwnerState.Enemy)
			{
				navyRoad.RoadType = NavyRoad.WaterRoadTypes.BothEnemy;
			}
		}
		else if (regionOwnerState == NavyRoad.RegionOwnerState.Enemy || regionOwnerState2 == NavyRoad.RegionOwnerState.Enemy)
		{
			navyRoad.RoadType = NavyRoad.WaterRoadTypes.OneEnemy;
		}
		else if (regionOwnerState == NavyRoad.RegionOwnerState.Allied || regionOwnerState2 == NavyRoad.RegionOwnerState.Allied)
		{
			navyRoad.RoadType = NavyRoad.WaterRoadTypes.OneAllied;
		}
		else
		{
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
			if (navyCommander != null)
			{
				if (navyCommander.CommanderState != NavyCommander.NavyCommanderState.Inactive)
				{
					num += navyCommander.WantedNumberOfArmies();
				}
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
		IDatabase<Amplitude.Unity.Framework.AnimationCurve> animationCurveDatabase = Databases.GetDatabase<Amplitude.Unity.Framework.AnimationCurve>(false);
		if (!string.IsNullOrEmpty(this.gameProgressCurveName))
		{
			this.gameProgressCurve = animationCurveDatabase.GetValue(this.gameProgressCurveName);
		}
		this.NavyStateMachine = new NavyStateMachine(this);
		this.NavyStateMachine.Initialize();
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		this.departmentOfTheInterior.OccupiedFortressesCollectionChanged += this.DepartmentOfTheInterior_OccupiedFortressesCollectionChanged;
		for (int index = 0; index < this.game.Empires.Length; index++)
		{
			if (index != base.AIEntity.Empire.Index)
			{
				DepartmentOfTransportation transport = this.game.Empires[index].GetAgency<DepartmentOfTransportation>();
				transport.ArmyPositionChange += this.DepartmentOfTransport_ArmyPositionChange;
				this.enemyTransportation.Add(transport);
			}
		}
		yield break;
	}

	public override IEnumerator Load()
	{
		yield return base.Load();
		int index = 0;
		while (index < this.game.Empires.Length)
		{
			NavalEmpire navalEmpire = this.game.Empires[index] as NavalEmpire;
			if (navalEmpire != null)
			{
				this.pirateCouncil = navalEmpire.GetAgency<PirateCouncil>();
				ISessionService sessionService = Services.GetService<ISessionService>();
				global::Session session = sessionService.Session as global::Session;
				GameServer gameServer = session.GameServer as GameServer;
				AIPlayer_NavalEmpire navalPlayer;
				if (!gameServer.AIScheduler.TryGetNavalEmpireAIPlayer(out navalPlayer))
				{
					break;
				}
				AIEntity_NavalEmpire navalEmpireEntity = navalPlayer.GetEntity<AIEntity_NavalEmpire>();
				this.raiderLayer = navalEmpireEntity.GetLayer<AILayer_Raiders>();
				break;
			}
			else
			{
				index++;
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
	}

	public bool MightAttackOwner(Region targetRegion, Empire targetOwner)
	{
		if (targetOwner.Index == base.AIEntity.Empire.Index)
		{
			return false;
		}
		bool flag = targetOwner.SimulationObject.Tags.Contains(DiplomaticAbilityDefinition.BlackSpotVictim);
		if (flag)
		{
			AILayer_Diplomacy layer = base.AIEntity.GetLayer<AILayer_Diplomacy>();
			if (layer != null)
			{
				float wantWarScore = layer.GetWantWarScore(targetOwner);
				float allyScore = layer.GetAllyScore(targetOwner);
				if (allyScore > 0.5f || wantWarScore < 0.25f)
				{
					return false;
				}
			}
		}
		else
		{
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
		return (U)((object)null);
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
			if (navyCommander != null)
			{
				float num = navyCommander.WantedNumberOfArmies();
				if (num > (float)navyCommander.NumberOfMediumSizedArmies)
				{
					this.TryStealFromNeighbourgs(navyCommander);
				}
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
				if (navyCommander != null)
				{
					if (navyCommander.CommanderState != NavyCommander.NavyCommanderState.Inactive)
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
			DepartmentOfDefense agency2 = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
			if (agency2.UnitDesignDatabase.AvailableUnitBodyDefinitions.Any((UnitBodyDefinition match) => match.Tags.Contains("Seafaring")))
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
			int num = this.pirateCouncil.Fortresses.Count((Fortress match) => match.IsOccupied);
			if (num > 0)
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
				if (agency != null)
				{
					if (agency.Cities.Any((City match) => this.myCityContinentId.Contains(match.Region.ContinentID)))
					{
						flag = true;
						break;
					}
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
			if (navyCommander != null)
			{
				if (navyCommander.CommanderState != NavyCommander.NavyCommanderState.Inactive)
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
				if (numberOfMediumSizedArmies <= numberOfMediumSizedArmies2)
				{
					if (navyCommander.CommanderArmyNeed() <= num || (float)numberOfMediumSizedArmies2 >= navyCommander.WantedNumberOfArmies())
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
	}

	private void UpdateUnitRatio()
	{
		float unitRatioForFull = 1f;
		float num = 0.75f;
		float num2 = 0.25f;
		float num3 = 0f;
		for (int i = 0; i < this.game.Empires.Length; i++)
		{
			if (i != base.AIEntity.Empire.Index)
			{
				if (this.departmentOfForeignAffairs.IsEnnemy(this.game.Empires[i]))
				{
					float bestNavyArmyPower = this.GetBestNavyArmyPower(this.game.Empires[i]);
					if (bestNavyArmyPower > num3)
					{
						num3 = bestNavyArmyPower;
					}
				}
			}
		}
		float bestPirateFortress = this.GetBestPirateFortress();
		if (bestPirateFortress > num3)
		{
			num3 = bestPirateFortress;
		}
		AILayer_UnitRecruitment layer = base.AIEntity.GetLayer<AILayer_UnitRecruitment>();
		float averageMilitaryPower = layer.NavalRecruiter.AverageMilitaryPower;
		float propertyValue = base.AIEntity.Empire.GetPropertyValue(SimulationProperties.ArmyUnitSlot);
		float num4 = propertyValue * averageMilitaryPower;
		if (num4 > num3)
		{
			float num5 = num3 / num4;
			num5 = Mathf.Clamp(num5, 0.5f, 1f);
			num *= num5;
			num2 *= num5;
		}
		this.CurrentRatioLimit = new ArmyRatioLimit(num, num2, unitRatioForFull);
	}

	private float GetBestNavyArmyPower(Empire empire)
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
		if (e.Action == CollectionChangeAction.Add)
		{
			NavyCommander navyCommander = base.NavyCommanders.Find((BaseNavyCommander match) => match.RegionData.WaterRegionIndex == fortress.Region.Index) as NavyCommander;
			if (navyCommander == null)
			{
				return;
			}
			NavyFortress navyFortress = navyCommander.NavyFortresses.Find((NavyFortress match) => match.Fortress.GUID == fortress.GUID);
			if (navyFortress == null)
			{
				navyFortress = new NavyFortress(this, fortress);
				navyFortress.Initialize();
				navyFortress.IsActive = this.IsActive();
			}
			navyFortress.AssignCommander(navyCommander);
			this.RefreshFortress(navyFortress);
			navyCommander.GenerateFillFortress(fortress);
		}
		else if (e.Action == CollectionChangeAction.Remove)
		{
			for (int i = 0; i < base.NavyCommanders.Count; i++)
			{
				NavyCommander navyCommander2 = base.NavyCommanders[i] as NavyCommander;
				if (navyCommander2 != null)
				{
					NavyFortress navyFortress2 = navyCommander2.NavyFortresses.Find((NavyFortress match) => match.Fortress.GUID == fortress.GUID);
					if (navyFortress2 != null)
					{
						navyFortress2.Release();
					}
				}
			}
		}
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
		bool flag = this.worldPositionningService.IsWaterTile(e.To);
		if (!flag || region2.IsOcean)
		{
			return;
		}
		if (region.IsOcean)
		{
			return;
		}
		NavyTask_Interception navyTask_Interception = this.FindTask<NavyTask_Interception>((NavyTask_Interception match) => match.TargetGuid == army.GUID);
		if (navyTask_Interception != null)
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
}
