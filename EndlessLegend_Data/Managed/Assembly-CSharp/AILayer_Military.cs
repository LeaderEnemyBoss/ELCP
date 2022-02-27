using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class AILayer_Military : AILayerWithObjective, IXmlSerializable
{
	public AILayer_Military() : base("Defense")
	{
	}

	public override void ReadXml(XmlReader reader)
	{
		base.ReadXml(reader);
	}

	public override void WriteXml(XmlWriter writer)
	{
		base.WriteXml(writer);
	}

	public static float GetCampDefenseLocalPriority(Camp camp, float unitRatioBoost, int simulatedUnitsCount = -1)
	{
		if (camp == null)
		{
			return 0f;
		}
		DepartmentOfForeignAffairs agency = camp.Empire.GetAgency<DepartmentOfForeignAffairs>();
		if (agency.IsInWarWithSomeone() && AILayer_Military.AreaIsSave(camp.WorldPosition, 12, agency, false))
		{
			return 0f;
		}
		if (camp.City.BesiegingEmpire != null)
		{
			return 0f;
		}
		float normalizedScore = 0f;
		float num;
		if (simulatedUnitsCount >= 0)
		{
			num = (float)simulatedUnitsCount / (float)camp.MaximumUnitSlot;
		}
		else
		{
			num = (float)camp.StandardUnits.Count / (float)camp.MaximumUnitSlot;
		}
		float normalizedScore2 = AILayer.Boost(normalizedScore, (1f - num) * unitRatioBoost);
		float developmentRatioOfCamp = AIScheduler.Services.GetService<IEntityInfoAIHelper>().GetDevelopmentRatioOfCamp(camp);
		return AILayer.Boost(normalizedScore2, (1f - developmentRatioOfCamp) * AILayer_Military.cityDevRatioBoost);
	}

	public static float GetCityDefenseLocalPriority(City city, float unitRatioBoost, int simulatedUnitsCount = -1)
	{
		if (city == null)
		{
			return 0f;
		}
		bool flag = false;
		DepartmentOfForeignAffairs agency = city.Empire.GetAgency<DepartmentOfForeignAffairs>();
		if (!agency.IsInWarWithSomeone() && !AIScheduler.Services.GetService<IWorldAtlasAIHelper>().IsRegionPacified(city.Empire, city.Region))
		{
			return 0f;
		}
		if (agency.IsInWarWithSomeone() && city.BesiegingEmpire == null)
		{
			flag = !AILayer_Military.AreaIsSave(city.WorldPosition, 10, agency, false);
		}
		float num = 0f;
		float num2;
		if (simulatedUnitsCount >= 0)
		{
			num2 = (float)simulatedUnitsCount / (float)city.MaximumUnitSlot;
		}
		else
		{
			num2 = (float)city.StandardUnits.Count / (float)city.MaximumUnitSlot;
		}
		num = AILayer.Boost(num, (1f - num2) * unitRatioBoost);
		if (city.BesiegingEmpire != null)
		{
			float propertyValue = city.GetPropertyValue(SimulationProperties.MaximumCityDefensePoint);
			float num3 = city.GetPropertyValue(SimulationProperties.CityDefensePoint) / propertyValue;
			num3 = 1f - num3;
			num = AILayer.Boost(num, num3 * AILayer_Military.cityDefenseUnderSiegeBoost);
		}
		else
		{
			float developmentRatioOfCity = AIScheduler.Services.GetService<IEntityInfoAIHelper>().GetDevelopmentRatioOfCity(city);
			num = AILayer.Boost(num, (1f - developmentRatioOfCity) * AILayer_Military.cityDevRatioBoost);
			if (flag)
			{
				num = AILayer.Boost(num, 0.5f);
			}
		}
		return num;
	}

	public float GetUnitPriority(Garrison garrison, int slotIndex, float priority, float minimalGarrisonPercent = 0.5f)
	{
		if ((float)this.endTurnService.Turn < this.unitInGarrisonTurnLimit)
		{
			return 0f;
		}
		float num = 1f;
		if ((float)this.endTurnService.Turn < this.unitInGarrisonTurnLimitForMaxPercent)
		{
			num = ((float)this.endTurnService.Turn - this.unitInGarrisonTurnLimit) / (this.unitInGarrisonTurnLimitForMaxPercent - this.unitInGarrisonTurnLimit);
			num = this.unitInGarrisonPercent + num * (1f - this.unitInGarrisonPercent);
		}
		if (Mathf.CeilToInt((float)garrison.MaximumUnitSlot * num) < slotIndex)
		{
			return 0f;
		}
		if ((float)slotIndex < (float)garrison.MaximumUnitSlot * minimalGarrisonPercent)
		{
			return priority;
		}
		return Mathf.Pow(this.unitInGarrisonPriorityMultiplierPerSlot, (float)slotIndex) * priority;
	}

	public float GetVillageUnitPriority(Village village, int slotIndex)
	{
		AILayer_Military.VillageDefensePriority villageDefensePriority = this.VillageDOFPriority.Find((AILayer_Military.VillageDefensePriority match) => match.Village.GUID == village.GUID);
		if (villageDefensePriority != null)
		{
			return this.GetUnitPriority(village, slotIndex, villageDefensePriority.FirstUnitPriority, 0.3f);
		}
		return 0f;
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		this.endTurnService = Services.GetService<IEndTurnService>();
		this.personalityAIHelper = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		this.worldAtlasHelper = AIScheduler.Services.GetService<IWorldAtlasAIHelper>();
		this.aiDataRepositoryAIHelper = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		this.departmentOfScience = base.AIEntity.Empire.GetAgency<DepartmentOfScience>();
		this.departmentOfForeignAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		this.departmentOfDefense = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		base.AIEntity.RegisterPass(AIEntity.Passes.RefreshObjectives.ToString(), "AILayer_Military_RefreshObjectives", new AIEntity.AIAction(this.RefreshObjectives), this, new StaticString[0]);
		this.unitInGarrisonPercent = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_Military.RegistryPath, "UnitInGarrisonPercent"), this.unitInGarrisonPercent);
		this.unitInGarrisonPriorityMultiplierPerSlot = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_Military.RegistryPath, "UnitInGarrisonPriorityMultiplierPerSlot"), this.unitInGarrisonPriorityMultiplierPerSlot);
		this.unitInGarrisonTurnLimit = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_Military.RegistryPath, "UnitInGarrisonTurnLimit"), this.unitInGarrisonTurnLimit);
		this.unitInGarrisonTurnLimitForMaxPercent = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_Military.RegistryPath, "UnitInGarrisonTurnLimit"), this.unitInGarrisonTurnLimit);
		this.unitRatioBoost = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_Military.RegistryPath, "UnitRatioBoost"), this.unitRatioBoost);
		this.villageDefenseRatioDeboost = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_Military.RegistryPath, "VillageDefenseRatioDeboost"), this.villageDefenseRatioDeboost);
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public override void Release()
	{
		base.Release();
		this.worldPositionningService = null;
		this.endTurnService = null;
		this.personalityAIHelper = null;
		this.worldAtlasHelper = null;
		this.aiDataRepositoryAIHelper = null;
		this.departmentOfTheInterior = null;
		this.departmentOfScience = null;
		this.departmentOfForeignAffairs = null;
		this.departmentOfDefense = null;
		this.VillageDOFPriority.Clear();
	}

	protected override int GetCommanderLimit()
	{
		return this.departmentOfTheInterior.Cities.Count;
	}

	protected override bool IsObjectiveValid(StaticString objectiveType, int regionIndex, bool checkLocalPriority = false)
	{
		Region region = this.worldPositionningService.GetRegion(regionIndex);
		return (region.City == null || region.City.Empire != base.AIEntity.Empire || this.worldAtlasHelper.IsRegionPacified(base.AIEntity.Empire, region) || this.departmentOfScience.CanParley() || this.departmentOfForeignAffairs.IsInWarWithSomeone()) && (region != null && region.City != null) && region.City.Empire == base.AIEntity.Empire;
	}

	protected override void RefreshObjectives(StaticString context, StaticString pass)
	{
		base.RefreshObjectives(context, pass);
		base.GatherObjectives(AICommanderMissionDefinition.AICommanderCategory.Defense.ToString(), ref this.globalObjectiveMessages);
		base.ValidateMessages(ref this.globalObjectiveMessages);
		AILayer_War layer = base.AIEntity.GetLayer<AILayer_War>();
		base.GlobalPriority.Reset();
		AILayer_Strategy layer2 = base.AIEntity.GetLayer<AILayer_Strategy>();
		base.GlobalPriority.Add(layer2.StrategicNetwork.GetAgentValue("InternalMilitary"), "Strategic Network 'InternalMilitary'", new object[0]);
		AILayer_ArmyManagement layer3 = base.AIEntity.GetLayer<AILayer_ArmyManagement>();
		float worldColonizationRatio = this.worldAtlasHelper.GetWorldColonizationRatio(base.AIEntity.Empire);
		bool flag = layer.WantWarWithSomoeone() || layer.NumberOfWar > 0;
		City mainCity = this.departmentOfTheInterior.MainCity;
		bool flag2 = false;
		if (this.departmentOfTheInterior.NonInfectedCities.Count < 4)
		{
			List<IGarrison> list = new List<IGarrison>();
			list.AddRange(this.departmentOfDefense.Armies.ToList<Army>().FindAll((Army match) => !match.IsSeafaring && !match.IsSettler && match.UnitsCount > 3).Cast<IGarrison>());
			if (list.Count > 1)
			{
				flag2 = true;
			}
		}
		for (int i = 0; i < this.departmentOfTheInterior.Cities.Count; i++)
		{
			City city = this.departmentOfTheInterior.Cities[i];
			if (this.IsObjectiveValid(AICommanderMissionDefinition.AICommanderCategory.Defense.ToString(), city.Region.Index, true))
			{
				GlobalObjectiveMessage globalObjectiveMessage = this.globalObjectiveMessages.Find((GlobalObjectiveMessage match) => match.RegionIndex == city.Region.Index);
				if (globalObjectiveMessage == null)
				{
					globalObjectiveMessage = base.GenerateObjective(city.Region.Index);
					globalObjectiveMessage.LocalPriority = new HeuristicValue(0f);
					this.globalObjectiveMessages.Add(globalObjectiveMessage);
				}
				globalObjectiveMessage.TimeOut = 1;
				globalObjectiveMessage.LocalPriority.Reset();
				if (flag2 && city == mainCity)
				{
					bool flag3 = !AILayer_Military.AreaIsSave(city.WorldPosition, 15, this.departmentOfForeignAffairs, false, false);
					if (!flag3)
					{
						foreach (Region region in this.worldPositionningService.GetNeighbourRegions(city.Region, false, false))
						{
							if (region.IsLand && region.Owner is MajorEmpire)
							{
								DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(region.Owner);
								if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.War || diplomaticRelation.State.Name == DiplomaticRelationState.Names.ColdWar || diplomaticRelation.State.Name == DiplomaticRelationState.Names.Truce)
								{
									flag3 = true;
									break;
								}
							}
						}
					}
					if (flag3)
					{
						globalObjectiveMessage.LocalPriority = new HeuristicValue(1f);
						globalObjectiveMessage.GlobalPriority = new HeuristicValue(1f);
					}
				}
				else
				{
					globalObjectiveMessage.GlobalPriority = base.GlobalPriority;
					AICommanderWithObjective aicommanderWithObjective = layer3.FindCommander(globalObjectiveMessage);
					if (aicommanderWithObjective != null && aicommanderWithObjective is AICommander_Defense)
					{
						AICommander_Defense aicommander_Defense = aicommanderWithObjective as AICommander_Defense;
						globalObjectiveMessage.LocalPriority.Add(AILayer_Military.GetCityDefenseLocalPriority(city, this.unitRatioBoost, aicommander_Defense.ComputeCurrentUnitInDefense()), "CityDefenseLocalPriority", new object[0]);
					}
					else
					{
						globalObjectiveMessage.LocalPriority.Add(AILayer_Military.GetCityDefenseLocalPriority(city, this.unitRatioBoost, -1), "CityDefenseLocalPriority", new object[0]);
					}
					HeuristicValue heuristicValue = new HeuristicValue(0f);
					heuristicValue.Add(worldColonizationRatio, "colonization ratio", new object[0]);
					heuristicValue.Multiply(0.2f, "(constant)", new object[0]);
					globalObjectiveMessage.LocalPriority.Boost(heuristicValue, "Colonization boost", new object[0]);
					if (flag)
					{
						globalObjectiveMessage.LocalPriority.Boost(0.3f, "Want war", new object[0]);
					}
					AIData_City aidata_City;
					if (this.aiDataRepositoryAIHelper.TryGetAIData<AIData_City>(city.GUID, out aidata_City) && aidata_City.IsAtWarBorder)
					{
						globalObjectiveMessage.LocalPriority.Boost(0.3f, "War border", new object[0]);
					}
					if ((float)this.endTurnService.Turn < this.unitInGarrisonTurnLimit)
					{
						globalObjectiveMessage.LocalPriority.Boost(-0.3f, "turn under 'unitInGarrisonTurnLimit' ({0})", new object[]
						{
							this.unitInGarrisonTurnLimit
						});
					}
				}
			}
		}
		MajorEmpire majorEmpire = base.AIEntity.Empire as MajorEmpire;
		if (majorEmpire == null || majorEmpire.ConvertedVillages.Count == 0)
		{
			return;
		}
		if (mainCity == null)
		{
			return;
		}
		float num = AILayer_Military.GetCityDefenseLocalPriority(mainCity, this.unitRatioBoost, AICommanderMission_Garrison.SimulatedUnitsCount);
		num *= this.villageDefenseRatioDeboost;
		num *= base.GlobalPriority;
		for (int k = 0; k < this.VillageDOFPriority.Count; k++)
		{
			this.VillageDOFPriority[k].Reset();
		}
		float num2 = 0f;
		for (int l = 0; l < majorEmpire.ConvertedVillages.Count; l++)
		{
			Village village = majorEmpire.ConvertedVillages[l];
			AILayer_Military.VillageDefensePriority villageDefensePriority = this.VillageDOFPriority.Find((AILayer_Military.VillageDefensePriority match) => match.Village.GUID == village.GUID);
			if (villageDefensePriority == null)
			{
				villageDefensePriority = new AILayer_Military.VillageDefensePriority();
				villageDefensePriority.Reset();
				villageDefensePriority.Village = village;
				this.VillageDOFPriority.Add(villageDefensePriority);
			}
			villageDefensePriority.ToDelete = false;
			villageDefensePriority.FirstUnitPriority = num;
			float num3 = (float)this.worldPositionningService.GetDistance(village.WorldPosition, mainCity.WorldPosition);
			villageDefensePriority.DistanceToMainCity = num3;
			if (num3 > num2)
			{
				num2 = num3;
			}
		}
		for (int m = this.VillageDOFPriority.Count - 1; m >= 0; m--)
		{
			AILayer_Military.VillageDefensePriority villageDefensePriority2 = this.VillageDOFPriority[m];
			if (villageDefensePriority2.ToDelete)
			{
				this.VillageDOFPriority.Remove(villageDefensePriority2);
			}
			else
			{
				float num4 = villageDefensePriority2.DistanceToMainCity / num2;
				if (majorEmpire.ConvertedVillages.Count > 1)
				{
					villageDefensePriority2.FirstUnitPriority = AILayer.Boost(villageDefensePriority2.FirstUnitPriority, num4 * -0.1f);
				}
			}
		}
	}

	private bool DefenseShouldStress()
	{
		return (float)this.endTurnService.Turn > this.unitInGarrisonTurnLimit;
	}

	public static bool AreaIsSave(WorldPosition pos, int size, DepartmentOfForeignAffairs departmentOfForeignAffairs, bool NavalOnly = false)
	{
		if (size < 1)
		{
			return true;
		}
		List<global::Empire> list = new List<global::Empire>(Array.FindAll<global::Empire>((Services.GetService<IGameService>().Game as global::Game).Empires, (global::Empire match) => match is MajorEmpire && departmentOfForeignAffairs.IsAtWarWith(match)));
		if (list.Count < 1)
		{
			return true;
		}
		IWorldPositionningService service = Services.GetService<IGameService>().Game.Services.GetService<IWorldPositionningService>();
		foreach (global::Empire empire in list)
		{
			List<IGarrison> list2 = new List<IGarrison>();
			DepartmentOfDefense agency = empire.GetAgency<DepartmentOfDefense>();
			DepartmentOfTheInterior agency2 = empire.GetAgency<DepartmentOfTheInterior>();
			if (!NavalOnly)
			{
				list2.AddRange(agency.Armies.ToList<Army>().FindAll((Army match) => !match.IsSeafaring && !match.IsSettler).Cast<IGarrison>());
				list2.AddRange(agency2.Cities.Cast<IGarrison>());
				list2.AddRange(agency2.Camps.Cast<IGarrison>());
				list2.AddRange(agency2.ConvertedVillages.Cast<IGarrison>());
			}
			if (NavalOnly)
			{
				list2.AddRange(agency.Armies.ToList<Army>().FindAll((Army match) => match.IsNaval && !match.IsSettler).Cast<IGarrison>());
				list2.AddRange(agency2.OccupiedFortresses.Cast<IGarrison>());
			}
			foreach (IGarrison garrison in list2)
			{
				if (garrison.UnitsCount > 0 && garrison is IWorldPositionable && service.GetDistance((garrison as IWorldPositionable).WorldPosition, pos) <= size)
				{
					return false;
				}
			}
		}
		return true;
	}

	public static bool AreaIsSave(WorldPosition pos, int size, DepartmentOfForeignAffairs departmentOfForeignAffairs, out float rangescore, bool NavalOnly = false)
	{
		rangescore = float.MaxValue;
		if (size < 1)
		{
			return true;
		}
		List<global::Empire> list = new List<global::Empire>(Array.FindAll<global::Empire>((Services.GetService<IGameService>().Game as global::Game).Empires, (global::Empire match) => match is MajorEmpire && departmentOfForeignAffairs.IsAtWarWith(match)));
		if (list.Count < 1)
		{
			return true;
		}
		bool result = true;
		IWorldPositionningService service = Services.GetService<IGameService>().Game.Services.GetService<IWorldPositionningService>();
		foreach (global::Empire empire in list)
		{
			List<IGarrison> list2 = new List<IGarrison>();
			DepartmentOfDefense agency = empire.GetAgency<DepartmentOfDefense>();
			DepartmentOfTheInterior agency2 = empire.GetAgency<DepartmentOfTheInterior>();
			if (!NavalOnly)
			{
				list2.AddRange(agency.Armies.ToList<Army>().FindAll((Army match) => !match.IsSeafaring && !match.IsSettler).Cast<IGarrison>());
				list2.AddRange(agency2.Cities.Cast<IGarrison>());
				list2.AddRange(agency2.Camps.Cast<IGarrison>());
				list2.AddRange(agency2.ConvertedVillages.Cast<IGarrison>());
			}
			if (NavalOnly)
			{
				list2.AddRange(agency.Armies.ToList<Army>().FindAll((Army match) => match.IsNaval && !match.IsSettler).Cast<IGarrison>());
				list2.AddRange(agency2.OccupiedFortresses.Cast<IGarrison>());
			}
			foreach (IGarrison garrison in list2)
			{
				if (garrison.UnitsCount > 0 && garrison is IWorldPositionable)
				{
					float num = (float)service.GetDistance((garrison as IWorldPositionable).WorldPosition, pos);
					if (num <= (float)size)
					{
						result = false;
						if (num / (float)size < rangescore)
						{
							rangescore = 1f - num / (float)size;
						}
					}
				}
			}
		}
		return result;
	}

	public static bool AreaIsSave(WorldPosition pos, int size, DepartmentOfForeignAffairs departmentOfForeignAffairs, out float rangescore, out float incomingMP, bool NavalOnly = false)
	{
		incomingMP = 0f;
		rangescore = 0f;
		if (size < 1)
		{
			return true;
		}
		List<global::Empire> list = new List<global::Empire>(Array.FindAll<global::Empire>((Services.GetService<IGameService>().Game as global::Game).Empires, (global::Empire match) => match is MajorEmpire && departmentOfForeignAffairs.IsAtWarWith(match)));
		if (list.Count < 1)
		{
			return true;
		}
		bool result = true;
		IWorldPositionningService service = Services.GetService<IGameService>().Game.Services.GetService<IWorldPositionningService>();
		foreach (global::Empire empire in list)
		{
			List<IGarrison> list2 = new List<IGarrison>();
			DepartmentOfDefense agency = empire.GetAgency<DepartmentOfDefense>();
			DepartmentOfTheInterior agency2 = empire.GetAgency<DepartmentOfTheInterior>();
			if (!NavalOnly)
			{
				list2.AddRange(agency.Armies.ToList<Army>().FindAll((Army match) => !match.IsSeafaring && !match.IsSettler).Cast<IGarrison>());
				list2.AddRange(agency2.Cities.Cast<IGarrison>());
				list2.AddRange(agency2.Camps.Cast<IGarrison>());
				list2.AddRange(agency2.ConvertedVillages.Cast<IGarrison>());
			}
			if (NavalOnly)
			{
				list2.AddRange(agency.Armies.ToList<Army>().FindAll((Army match) => match.IsNaval && !match.IsSettler).Cast<IGarrison>());
				list2.AddRange(agency2.OccupiedFortresses.Cast<IGarrison>());
			}
			foreach (IGarrison garrison in list2)
			{
				if (garrison.UnitsCount > 0 && garrison is IWorldPositionable)
				{
					float num = (float)service.GetDistance((garrison as IWorldPositionable).WorldPosition, pos);
					if (num <= (float)size)
					{
						incomingMP += garrison.GetPropertyValue(SimulationProperties.MilitaryPower);
						result = false;
						float num2 = 1f - num / (float)size;
						if (num2 > rangescore)
						{
							rangescore = num2;
						}
					}
				}
			}
		}
		return result;
	}

	public static bool HasSaveAttackableTargetsNearby(Garrison Attacker, int size, DepartmentOfForeignAffairs departmentOfForeignAffairs, out List<IGarrison> Targets, bool NavalOnly = false)
	{
		IIntelligenceAIHelper service = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		IVisibilityService service2 = Services.GetService<IGameService>().Game.Services.GetService<IVisibilityService>();
		Targets = new List<IGarrison>();
		if (size < 1 || Attacker == null || !(Attacker is IWorldPositionable))
		{
			return false;
		}
		List<global::Empire> list = new List<global::Empire>(Array.FindAll<global::Empire>((Services.GetService<IGameService>().Game as global::Game).Empires, (global::Empire match) => match is MajorEmpire && departmentOfForeignAffairs.IsAtWarWith(match)));
		if (list.Count < 1)
		{
			return false;
		}
		bool result = true;
		IWorldPositionningService service3 = Services.GetService<IGameService>().Game.Services.GetService<IWorldPositionningService>();
		foreach (global::Empire empire in list)
		{
			List<Garrison> list2 = new List<Garrison>();
			DepartmentOfDefense agency = empire.GetAgency<DepartmentOfDefense>();
			DepartmentOfTheInterior agency2 = empire.GetAgency<DepartmentOfTheInterior>();
			if (!NavalOnly)
			{
				list2.AddRange(agency.Armies.ToList<Army>().FindAll((Army match) => !match.IsSeafaring && !match.IsSettler).Cast<Garrison>());
				list2.AddRange(agency2.Cities.Cast<Garrison>());
				list2.AddRange(agency2.Camps.Cast<Garrison>());
				list2.AddRange(agency2.ConvertedVillages.Cast<Garrison>());
			}
			if (NavalOnly)
			{
				list2.AddRange(agency.Armies.ToList<Army>().FindAll((Army match) => match.IsNaval && !match.IsSettler).Cast<Garrison>());
				list2.AddRange(agency2.OccupiedFortresses.Cast<Garrison>());
			}
			foreach (Garrison garrison in list2)
			{
				if (garrison.UnitsCount > 0 && garrison is IWorldPositionable && (float)service3.GetDistance((garrison as IWorldPositionable).WorldPosition, (Attacker as IWorldPositionable).WorldPosition) <= (float)size && departmentOfForeignAffairs.CanAttack(garrison) && (!garrison.SimulationObject.Tags.Contains(Army.TagCamouflaged) || service2.IsWorldPositionDetectedFor((garrison as IWorldPositionable).WorldPosition, Attacker.Empire)) && service2.IsWorldPositionVisibleFor((garrison as IWorldPositionable).WorldPosition, Attacker.Empire))
				{
					float num = 0f;
					float num2 = 0f;
					if (!NavalOnly)
					{
						service.EstimateMPInBattleground(Attacker, garrison, ref num, ref num2);
					}
					else
					{
						num += Attacker.GetPropertyValue(SimulationProperties.MilitaryPower);
						num2 += garrison.GetPropertyValue(SimulationProperties.MilitaryPower);
						if (Attacker is Army && (Attacker as Army).IsSeafaring && garrison is Army && !(garrison as Army).IsSeafaring)
						{
							num2 *= 0.2f;
						}
					}
					if (num > num2 * 1.5f)
					{
						Targets.Add(garrison);
					}
					else
					{
						result = false;
					}
				}
			}
		}
		if (Targets.Count == 0)
		{
			result = false;
		}
		return result;
	}

	public static bool AreaIsSave(WorldPosition pos, int size, DepartmentOfForeignAffairs departmentOfForeignAffairs, bool NavalOnly = false, bool ignoreColdwar = false)
	{
		if (size < 1)
		{
			return true;
		}
		List<global::Empire> list = new List<global::Empire>();
		if (ignoreColdwar)
		{
			list.AddRange(Array.FindAll<global::Empire>((Services.GetService<IGameService>().Game as global::Game).Empires, (global::Empire match) => match is MajorEmpire && departmentOfForeignAffairs.IsAtWarWith(match)));
		}
		else
		{
			list.AddRange(Array.FindAll<global::Empire>((Services.GetService<IGameService>().Game as global::Game).Empires, (global::Empire match) => match is MajorEmpire && !departmentOfForeignAffairs.IsFriend(match)));
		}
		if (list.Count == 0)
		{
			return true;
		}
		IWorldPositionningService service = Services.GetService<IGameService>().Game.Services.GetService<IWorldPositionningService>();
		foreach (global::Empire empire in list)
		{
			List<IGarrison> list2 = new List<IGarrison>();
			DepartmentOfDefense agency = empire.GetAgency<DepartmentOfDefense>();
			DepartmentOfTheInterior agency2 = empire.GetAgency<DepartmentOfTheInterior>();
			if (!NavalOnly)
			{
				list2.AddRange(agency.Armies.ToList<Army>().FindAll((Army match) => !match.IsSeafaring && !match.IsSettler).Cast<IGarrison>());
				list2.AddRange(agency2.Cities.Cast<IGarrison>());
				list2.AddRange(agency2.Camps.Cast<IGarrison>());
				list2.AddRange(agency2.ConvertedVillages.Cast<IGarrison>());
			}
			if (NavalOnly)
			{
				list2.AddRange(agency.Armies.ToList<Army>().FindAll((Army match) => match.IsNaval && !match.IsSettler).Cast<IGarrison>());
				list2.AddRange(agency2.OccupiedFortresses.Cast<IGarrison>());
			}
			foreach (IGarrison garrison in list2)
			{
				if (garrison.UnitsCount > 0 && garrison is IWorldPositionable && service.GetDistance((garrison as IWorldPositionable).WorldPosition, pos) <= size)
				{
					return false;
				}
			}
		}
		return true;
	}

	public static string RegistryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_Military";

	public List<AILayer_Military.VillageDefensePriority> VillageDOFPriority = new List<AILayer_Military.VillageDefensePriority>();

	private static float cityDefenseUnderSiegeBoost = 0.8f;

	private static float cityDevRatioBoost = -0.3f;

	private IAIDataRepositoryAIHelper aiDataRepositoryAIHelper;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private IEndTurnService endTurnService;

	private IPersonalityAIHelper personalityAIHelper;

	private float unitInGarrisonPercent = 0.25f;

	private float unitInGarrisonPriorityMultiplierPerSlot = 0.5f;

	private float unitInGarrisonTurnLimit = 20f;

	private float unitInGarrisonTurnLimitForMaxPercent = 60f;

	private float unitRatioBoost = 0.8f;

	private float villageDefenseRatioDeboost = 0.5f;

	private IWorldAtlasAIHelper worldAtlasHelper;

	private IWorldPositionningService worldPositionningService;

	private DepartmentOfScience departmentOfScience;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private DepartmentOfDefense departmentOfDefense;

	public class VillageDefensePriority
	{
		public void Reset()
		{
			this.FirstUnitPriority = 0f;
			this.DistanceToMainCity = 0f;
			this.ToDelete = true;
		}

		public float DistanceToMainCity;

		public float FirstUnitPriority;

		public bool ToDelete;

		public Village Village;
	}
}
