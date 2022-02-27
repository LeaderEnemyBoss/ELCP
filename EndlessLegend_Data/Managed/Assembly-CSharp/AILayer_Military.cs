using System;
using System.Collections;
using System.Collections.Generic;
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
		normalizedScore = AILayer.Boost(normalizedScore, (1f - num) * unitRatioBoost);
		IEntityInfoAIHelper service = AIScheduler.Services.GetService<IEntityInfoAIHelper>();
		float developmentRatioOfCamp = service.GetDevelopmentRatioOfCamp(camp);
		return AILayer.Boost(normalizedScore, (1f - developmentRatioOfCamp) * AILayer_Military.cityDevRatioBoost);
	}

	public static float GetCityDefenseLocalPriority(City city, float unitRatioBoost, int simulatedUnitsCount = -1)
	{
		if (city == null)
		{
			return 0f;
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
			float propertyValue2 = city.GetPropertyValue(SimulationProperties.CityDefensePoint);
			float num3 = propertyValue2 / propertyValue;
			num3 = 1f - num3;
			num = AILayer.Boost(num, num3 * AILayer_Military.cityDefenseUnderSiegeBoost);
		}
		else
		{
			IEntityInfoAIHelper service = AIScheduler.Services.GetService<IEntityInfoAIHelper>();
			float developmentRatioOfCity = service.GetDevelopmentRatioOfCity(city);
			num = AILayer.Boost(num, (1f - developmentRatioOfCity) * AILayer_Military.cityDevRatioBoost);
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
		int num2 = Mathf.CeilToInt((float)garrison.MaximumUnitSlot * num);
		if (num2 < slotIndex)
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
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		this.worldPositionningService = gameService.Game.Services.GetService<IWorldPositionningService>();
		this.endTurnService = Services.GetService<IEndTurnService>();
		this.personalityAIHelper = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		this.worldAtlasHelper = AIScheduler.Services.GetService<IWorldAtlasAIHelper>();
		this.aiDataRepositoryAIHelper = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
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
		this.VillageDOFPriority.Clear();
	}

	protected override int GetCommanderLimit()
	{
		return this.departmentOfTheInterior.Cities.Count;
	}

	protected override bool IsObjectiveValid(StaticString objectiveType, int regionIndex, bool checkLocalPriority = false)
	{
		Region region = this.worldPositionningService.GetRegion(regionIndex);
		return region != null && region.City != null && region.City.Empire == base.AIEntity.Empire;
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
				globalObjectiveMessage.GlobalPriority = base.GlobalPriority;
				globalObjectiveMessage.LocalPriority.Reset();
				globalObjectiveMessage.TimeOut = 1;
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
		MajorEmpire majorEmpire = base.AIEntity.Empire as MajorEmpire;
		if (majorEmpire == null || majorEmpire.ConvertedVillages.Count == 0)
		{
			return;
		}
		City mainCity = this.departmentOfTheInterior.MainCity;
		if (mainCity == null)
		{
			return;
		}
		float num = AILayer_Military.GetCityDefenseLocalPriority(mainCity, this.unitRatioBoost, AICommanderMission_Garrison.SimulatedUnitsCount);
		num *= this.villageDefenseRatioDeboost;
		num *= base.GlobalPriority;
		for (int j = 0; j < this.VillageDOFPriority.Count; j++)
		{
			AILayer_Military.VillageDefensePriority villageDefensePriority = this.VillageDOFPriority[j];
			villageDefensePriority.Reset();
		}
		float num2 = 0f;
		for (int k = 0; k < majorEmpire.ConvertedVillages.Count; k++)
		{
			Village village = majorEmpire.ConvertedVillages[k];
			AILayer_Military.VillageDefensePriority villageDefensePriority2 = this.VillageDOFPriority.Find((AILayer_Military.VillageDefensePriority match) => match.Village.GUID == village.GUID);
			if (villageDefensePriority2 == null)
			{
				villageDefensePriority2 = new AILayer_Military.VillageDefensePriority();
				villageDefensePriority2.Reset();
				villageDefensePriority2.Village = village;
				this.VillageDOFPriority.Add(villageDefensePriority2);
			}
			villageDefensePriority2.ToDelete = false;
			villageDefensePriority2.FirstUnitPriority = num;
			float num3 = (float)this.worldPositionningService.GetDistance(village.WorldPosition, mainCity.WorldPosition);
			villageDefensePriority2.DistanceToMainCity = num3;
			if (num3 > num2)
			{
				num2 = num3;
			}
		}
		for (int l = this.VillageDOFPriority.Count - 1; l >= 0; l--)
		{
			AILayer_Military.VillageDefensePriority villageDefensePriority3 = this.VillageDOFPriority[l];
			if (villageDefensePriority3.ToDelete)
			{
				this.VillageDOFPriority.Remove(villageDefensePriority3);
			}
			else
			{
				float num4 = villageDefensePriority3.DistanceToMainCity / num2;
				if (majorEmpire.ConvertedVillages.Count > 1)
				{
					villageDefensePriority3.FirstUnitPriority = AILayer.Boost(villageDefensePriority3.FirstUnitPriority, num4 * -0.1f);
				}
			}
		}
	}

	private bool DefenseShouldStress()
	{
		return (float)this.endTurnService.Turn > this.unitInGarrisonTurnLimit;
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
