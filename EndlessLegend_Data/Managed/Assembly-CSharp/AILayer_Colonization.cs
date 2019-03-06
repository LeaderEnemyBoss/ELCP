using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

[PersonalityRegistryPath("AI/MajorEmpire/AIEntity_Empire/AILayer_Colonization/", new object[]
{

})]
public class AILayer_Colonization : AILayerWithObjective, IXmlSerializable
{
	public AILayer_Colonization() : base("Colonization")
	{
	}

	private void CreateContinentInformation()
	{
		this.continentData = new AILayer_Colonization.ContinentData[this.worldPositionningService.World.Continents.Length];
		int num = 0;
		for (int i = 0; i < this.continentData.Length; i++)
		{
			AILayer_Colonization.ContinentData continentData = new AILayer_Colonization.ContinentData();
			continentData.Initialize(base.AIEntity.Empire, this.worldPositionningService.World.Continents[i], this.worldPositionningService);
			if (continentData.Type == AILayer_Colonization.ContinentData.ContinentType.Continent)
			{
				num++;
			}
			this.continentData[i] = continentData;
		}
		if (num == 1)
		{
			for (int j = 0; j < this.continentData.Length; j++)
			{
				if (this.continentData[j].Type == AILayer_Colonization.ContinentData.ContinentType.Continent)
				{
					this.continentData[j].Type = AILayer_Colonization.ContinentData.ContinentType.Unique;
					break;
				}
			}
		}
	}

	private void UpdateContinentData()
	{
		for (int i = 0; i < this.continentData.Length; i++)
		{
			this.continentData[i].UpdateRegionOwnership();
		}
	}

	public override void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		if (num >= 2)
		{
			this.settlerCount = reader.GetAttribute<int>("SettlerCount");
		}
		base.ReadXml(reader);
	}

	public override void WriteXml(XmlWriter writer)
	{
		int num = writer.WriteVersionAttribute(2);
		if (num >= 2)
		{
			writer.WriteAttributeString<int>("SettlerCount", this.settlerCount);
		}
		base.WriteXml(writer);
	}

	private Region GetNextRegionToColonize()
	{
		Region region = this.ChooseNeighbourgRegion();
		if (region == null)
		{
			return this.ChooseOverseaRegion();
		}
		return region;
	}

	private Region ChooseNeutralIsland()
	{
		this.potentialRegions.Clear();
		for (int i = 0; i < this.continentData.Length; i++)
		{
			if (!this.continentData[i].EmpireWithRegion.Contains(base.AIEntity.Empire))
			{
				if (this.continentData[i].LandRegionCount != 0)
				{
					if (this.continentData[i].OverallColonizationPercent <= 0.2f)
					{
						if (this.continentData[i].CostalRegionRatio >= 0.8f)
						{
							this.continentData[i].FillPotentialRegion(this.potentialRegions);
						}
					}
				}
			}
		}
		return this.GetClosestRegion(this.potentialRegions);
	}

	private Region ChooseOverseaRegion()
	{
		if (this.departmentOfDefense.TechnologyDefinitionShipState != DepartmentOfScience.ConstructibleElement.State.Researched)
		{
			return null;
		}
		Region region = this.ChooseNeutralIsland();
		if (region != null)
		{
			return region;
		}
		this.potentialRegions.Clear();
		for (int i = 0; i < this.continentData.Length; i++)
		{
			if (!this.continentData[i].EmpireWithRegion.Contains(base.AIEntity.Empire))
			{
				if (this.continentData[i].LandRegionCount != 0)
				{
					if (this.continentData[i].OverallColonizationPercent <= 0.34f)
					{
						this.continentData[i].FillPotentialRegion(this.potentialRegions);
					}
				}
			}
		}
		return this.GetClosestRegion(this.potentialRegions);
	}

	private Region ChooseNeighbourgRegion()
	{
		this.potentialRegions.Clear();
		for (int i = 0; i < this.departmentOfTheInterior.Cities.Count; i++)
		{
			this.FillNeighbourgs(this.departmentOfTheInterior.Cities[i].Region, this.potentialRegions);
		}
		return this.GetClosestRegion(this.potentialRegions);
	}

	private Region ChooseLastHopeRegion()
	{
		Army army = null;
		DepartmentOfDefense agency = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		for (int i = 0; i < agency.Armies.Count; i++)
		{
			if (agency.Armies[i].IsSettler)
			{
				army = agency.Armies[i];
				break;
			}
		}
		if (army == null)
		{
			return null;
		}
		Region region = this.worldPositionningService.GetRegion(agency.Armies[0].WorldPosition);
		if (region.City == null)
		{
			return region;
		}
		List<Region> list = new List<Region>();
		this.FillNeighbourgs(region, list);
		if (list.Count == 0)
		{
			for (int j = 0; j < this.worldPositionningService.World.Regions.Length; j++)
			{
				Region region2 = this.worldPositionningService.World.Regions[j];
				if (region2.City == null && region2.IsLand)
				{
					list.Add(region2);
				}
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list[0];
	}

	private Region GetClosestRegion(List<Region> regions)
	{
		AILayer_Colonization.<GetClosestRegion>c__AnonStorey7F1 <GetClosestRegion>c__AnonStorey7F = new AILayer_Colonization.<GetClosestRegion>c__AnonStorey7F1();
		<GetClosestRegion>c__AnonStorey7F.regions = regions;
		int num = int.MaxValue;
		Region result = null;
		int index;
		for (index = 0; index < <GetClosestRegion>c__AnonStorey7F.regions.Count; index++)
		{
			if (<GetClosestRegion>c__AnonStorey7F.regions[index].City == null)
			{
				if (<GetClosestRegion>c__AnonStorey7F.regions[index].IsLand)
				{
					if (!this.globalObjectiveMessages.Exists((GlobalObjectiveMessage match) => match.RegionIndex == <GetClosestRegion>c__AnonStorey7F.regions[index].Index))
					{
						int num2 = this.worldAtlasHelper.ComputeBirdEyeDistanceBetweenRegionAndEmpire(base.AIEntity.Empire, <GetClosestRegion>c__AnonStorey7F.regions[index]);
						if (num2 < num)
						{
							num = num2;
							result = <GetClosestRegion>c__AnonStorey7F.regions[index];
						}
					}
				}
			}
		}
		return result;
	}

	private void FillNeighbourgs(Region source, List<Region> neighbourgs)
	{
		for (int i = 0; i < source.Borders.Length; i++)
		{
			Region region = this.worldPositionningService.GetRegion(source.Borders[i].NeighbourRegionIndex);
			if (region.City == null && region.IsLand && source.ContinentID == region.ContinentID)
			{
				if (!neighbourgs.Contains(region))
				{
					neighbourgs.Add(region);
				}
			}
		}
	}

	public int CurrentSettlerCount { get; set; }

	public int WantedNewCity { get; private set; }

	public bool WantToColonizeOversea { get; set; }

	public static bool IsAbleToColonize(global::Empire empire)
	{
		DepartmentOfDefense agency = empire.GetAgency<DepartmentOfDefense>();
		ReadOnlyCollection<UnitBodyDefinition> availableUnitBodyDefinitions = ((IUnitDesignDatabase)agency).AvailableUnitBodyDefinitions;
		for (int i = 0; i < availableUnitBodyDefinitions.Count; i++)
		{
			if (!availableUnitBodyDefinitions[i].Tags.Contains("Hidden") && availableUnitBodyDefinitions[i].CheckUnitAbility(UnitAbility.ReadonlyColonize, -1) && DepartmentOfTheTreasury.CheckConstructiblePrerequisites(empire, availableUnitBodyDefinitions[i], new string[]
			{
				"Prerequisites"
			}))
			{
				return true;
			}
		}
		return false;
	}

	public HeuristicValue GetColonizationInterest(int regionIndex)
	{
		if (this.globalObjectiveMessages.Exists((GlobalObjectiveMessage match) => match.RegionIndex == regionIndex))
		{
			return base.GlobalPriority;
		}
		for (int i = 0; i < this.departmentOfTheInterior.Cities.Count; i++)
		{
			if (this.IsNeighbourgTo(this.departmentOfTheInterior.Cities[i].Region, regionIndex))
			{
				HeuristicValue heuristicValue = new HeuristicValue(0f);
				heuristicValue.Add(base.GlobalPriority, "Global priority", new object[0]);
				heuristicValue.Multiply(0.2f, "constant", new object[0]);
				return heuristicValue;
			}
		}
		return new HeuristicValue(0f);
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		base.AIEntity.RegisterPass(AIEntity.Passes.RefreshObjectives.ToString(), "AILayer_Colonization_RefreshObjectives", new AIEntity.AIAction(this.RefreshObjectives), this, new StaticString[0]);
		IWorldPositionEvaluationAIHelper worldPositionHelper = AIScheduler.Services.GetService<IWorldPositionEvaluationAIHelper>();
		Diagnostics.Assert(worldPositionHelper != null);
		this.worldAtlasHelper = AIScheduler.Services.GetService<IWorldAtlasAIHelper>();
		Diagnostics.Assert(this.worldAtlasHelper != null);
		this.worldPositionningService = gameService.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		this.departmentOfDefense = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		this.aiLayerStrategy = base.AIEntity.GetLayer<AILayer_Strategy>();
		this.CreateContinentInformation();
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public override void Release()
	{
		this.worldPositionningService = null;
		this.departmentOfTheInterior = null;
		this.worldAtlasHelper = null;
		this.departmentOfDefense = null;
		this.aiLayerStrategy = null;
		for (int i = 0; i < this.continentData.Length; i++)
		{
			this.continentData[i].Release();
		}
		this.continentData = new AILayer_Colonization.ContinentData[0];
	}

	protected override bool IsObjectiveValid(StaticString objectiveType, int regionIndex, bool checkLocalPriority = false)
	{
		Region region = this.worldPositionningService.GetRegion(regionIndex);
		return !region.IsRegionColonized();
	}

	protected override void RefreshObjectives(StaticString context, StaticString pass)
	{
		base.RefreshObjectives(context, pass);
		this.ComputeGlobalPriority();
		this.UpdateContinentData();
		int num = this.GetNumberOfAcceptedColonization();
		this.WantedNewCity = num;
		this.CurrentSettlerCount = this.GetSettlerCount();
		base.GatherObjectives(AICommanderMissionDefinition.AICommanderCategory.Colonization.ToString(), ref this.globalObjectiveMessages);
		base.ValidateMessages(ref this.globalObjectiveMessages);
		for (int i = 0; i < this.globalObjectiveMessages.Count; i++)
		{
			GlobalObjectiveMessage globalObjectiveMessage = this.globalObjectiveMessages[i];
			if (this.IsObjectiveValid(AICommanderMissionDefinition.AICommanderCategory.Colonization.ToString(), globalObjectiveMessage.RegionIndex, false))
			{
				if (num > 0 || this.CurrentSettlerCount > i)
				{
					num--;
					this.RefreshMessagePriority(globalObjectiveMessage);
				}
				else
				{
					base.AIEntity.AIPlayer.Blackboard.CancelMessage(this.globalObjectiveMessages[i]);
				}
			}
		}
		if (this.departmentOfTheInterior.Cities.Count == 0 && this.globalObjectiveMessages.Count == 0)
		{
			Region region2 = this.ChooseLastHopeRegion();
			if (region2 == null)
			{
				return;
			}
			this.CreateObjectiveFor(region2);
			num--;
		}
		for (int j = 0; j < num; j++)
		{
			Region nextRegionToColonize = this.GetNextRegionToColonize();
			if (nextRegionToColonize != null)
			{
				this.CreateObjectiveFor(nextRegionToColonize);
			}
		}
		this.WantToColonizeOversea = false;
		for (int k = 0; k < this.globalObjectiveMessages.Count; k++)
		{
			GlobalObjectiveMessage globalObjectiveMessage2 = this.globalObjectiveMessages[k];
			Region region = this.worldPositionningService.GetRegion(globalObjectiveMessage2.RegionIndex);
			AILayer_Colonization.ContinentData continentData = Array.Find<AILayer_Colonization.ContinentData>(this.continentData, (AILayer_Colonization.ContinentData match) => match.ContinentId == region.ContinentID);
			if (!continentData.EmpireWithRegion.Contains(base.AIEntity.Empire))
			{
				this.WantToColonizeOversea = true;
				break;
			}
		}
	}

	protected override int GetCommanderLimit()
	{
		return Mathf.Clamp(this.CurrentSettlerCount, 1, this.WantedNewCity);
	}

	private int GetNumberOfAcceptedColonization()
	{
		int num = 0;
		if (this.departmentOfTheInterior.Cities.Count == 0)
		{
			return 1;
		}
		if (!AILayer_Colonization.IsAbleToColonize(base.AIEntity.Empire))
		{
			return 0;
		}
		int num2 = 0;
		for (int i = 0; i < this.departmentOfTheInterior.Cities.Count; i++)
		{
			if (!this.worldAtlasHelper.IsRegionPacified(base.AIEntity.Empire.Index, this.departmentOfTheInterior.Cities[i].Region.Index))
			{
				num2++;
			}
		}
		float num3 = Mathf.Clamp(this.empireApprovalNeededToEngageColonization, 0f, 100f);
		float num4 = base.AIEntity.Empire.GetPropertyValue(SimulationProperties.NetEmpireApproval);
		float propertyBaseValue = this.departmentOfTheInterior.Cities[0].GetPropertyBaseValue(SimulationProperties.CityExpansionDisapproval);
		float propertyBaseValue2 = this.departmentOfTheInterior.Cities[0].GetPropertyBaseValue(SimulationProperties.CityApproval);
		int num5 = Mathf.RoundToInt(this.acceptedNumberOfUnPacifiedRegion);
		while (num4 >= num3 && num2 + num <= num5)
		{
			num++;
			float num6 = 0f;
			float num7 = 0f;
			for (int j = 0; j < this.departmentOfTheInterior.Cities.Count; j++)
			{
				float num8 = this.departmentOfTheInterior.Cities[j].GetPropertyValue(SimulationProperties.NetCityApproval) - (float)num * propertyBaseValue;
				float num9 = this.departmentOfTheInterior.Cities[j].GetPropertyValue(SimulationProperties.Population);
				num7 += num8 * num9;
				num6 += num9;
			}
			for (int k = 0; k < num; k++)
			{
				float num8 = propertyBaseValue2 - (float)(this.departmentOfTheInterior.Cities.Count - 1 + num) * propertyBaseValue;
				float num9 = 1f;
				num7 += num8 * num9;
				num6 += num9;
			}
			num4 = num7 / num6;
		}
		return Mathf.Max(0, num - 1);
	}

	private int GetSettlerCount()
	{
		int num = 0;
		for (int i = 0; i < this.departmentOfDefense.Armies.Count; i++)
		{
			num += this.GetSettlerCount(this.departmentOfDefense.Armies[i]);
		}
		for (int j = 0; j < this.departmentOfTheInterior.Cities.Count; j++)
		{
			num += this.GetSettlerCount(this.departmentOfTheInterior.Cities[j]);
		}
		return num;
	}

	private int GetSettlerCount(IGarrison garrison)
	{
		int num = 0;
		for (int i = 0; i < garrison.StandardUnits.Count; i++)
		{
			bool flag = garrison.StandardUnits[i].UnitDesign.CheckUnitAbility(UnitAbility.ReadonlyColonize, -1);
			if (flag)
			{
				num++;
			}
		}
		return num;
	}

	private bool IsNeighbourgTo(Region source, int neighbourgRegionIndex)
	{
		Region region = this.worldPositionningService.GetRegion(neighbourgRegionIndex);
		for (int i = 0; i < source.Borders.Length; i++)
		{
			if (source.Borders[i].NeighbourRegionIndex == neighbourgRegionIndex && (this.departmentOfDefense.TechnologyDefinitionShipState == DepartmentOfScience.ConstructibleElement.State.Researched || source.ContinentID == region.ContinentID))
			{
				return true;
			}
		}
		return false;
	}

	private void ComputeGlobalPriority()
	{
		base.GlobalPriority.Reset();
		if (this.departmentOfTheInterior.Cities.Count == 0)
		{
			base.GlobalPriority.Add(1f, "No more city!", new object[0]);
		}
		else
		{
			AILayer_Strategy layer = base.AIEntity.GetLayer<AILayer_Strategy>();
			base.GlobalPriority.Add(layer.StrategicNetwork.GetAgentValue("Expansion"), "Expansion strategic network score", new object[0]);
			if (this.aiLayerStrategy.IsAtWar())
			{
				base.GlobalPriority.Boost(-0.5f, "At war", new object[0]);
			}
			else if (this.aiLayerStrategy.WantWarWithSomeone())
			{
				base.GlobalPriority.Boost(-0.2f, "Want war but not at war", new object[0]);
			}
			else if (this.departmentOfTheInterior.Cities.Count == 1)
			{
				base.GlobalPriority.Boost(0.2f, "I am sure you want more than one city...", new object[0]);
			}
		}
	}

	private void CreateObjectiveFor(Region region)
	{
		GlobalObjectiveMessage globalObjectiveMessage = base.GenerateObjective(region.Index);
		this.RefreshMessagePriority(globalObjectiveMessage);
		this.globalObjectiveMessages.Add(globalObjectiveMessage);
	}

	private void RefreshMessagePriority(GlobalObjectiveMessage objectiveMessage)
	{
		objectiveMessage.GlobalPriority = base.GlobalPriority;
		if (this.departmentOfTheInterior.Cities.Count == 0)
		{
			objectiveMessage.LocalPriority = new HeuristicValue(1f);
		}
		else
		{
			objectiveMessage.LocalPriority = new HeuristicValue(0.8f);
		}
		objectiveMessage.TimeOut = 1;
	}

	private AILayer_Colonization.ContinentData[] continentData;

	private List<Region> potentialRegions = new List<Region>();

	private AILayer_Strategy aiLayerStrategy;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private DepartmentOfDefense departmentOfDefense;

	private int settlerCount;

	private IWorldAtlasAIHelper worldAtlasHelper;

	private IWorldPositionningService worldPositionningService;

	[InfluencedByPersonality]
	private float empireApprovalNeededToEngageColonization = 40f;

	[InfluencedByPersonality]
	private float acceptedNumberOfUnPacifiedRegion = 2f;

	private class ContinentData
	{
		public global::Empire DataOwner { get; set; }

		public int ContinentId
		{
			get
			{
				return this.Continent.ID;
			}
		}

		public Continent Continent { get; set; }

		public List<Region> Regions { get; set; }

		public List<global::Empire> EmpireWithRegion { get; set; }

		public List<float> ColonizationPercentByEmpire { get; set; }

		public float OverallColonizationPercent { get; set; }

		public int LandRegionCount { get; set; }

		public float CostalRegionRatio { get; set; }

		public AILayer_Colonization.ContinentData.ContinentType Type { get; set; }

		public void Initialize(global::Empire dataOwner, Continent continent, IWorldPositionningService worldPositionningService)
		{
			this.DataOwner = dataOwner;
			this.Regions = new List<Region>();
			this.EmpireWithRegion = new List<global::Empire>();
			this.ColonizationPercentByEmpire = new List<float>();
			this.Continent = continent;
			for (int i = 0; i < continent.RegionList.Length; i++)
			{
				Region region = worldPositionningService.GetRegion(continent.RegionList[i]);
				this.Regions.Add(region);
				if (region.IsLand)
				{
					this.LandRegionCount++;
				}
			}
			this.CostalRegionRatio = (float)continent.CostalRegionList.Length / (float)this.LandRegionCount;
			this.Type = AILayer_Colonization.ContinentData.ContinentType.Continent;
			if (this.LandRegionCount == 0)
			{
				this.Type = AILayer_Colonization.ContinentData.ContinentType.Water;
			}
			else if (this.CostalRegionRatio > 0.8f)
			{
				this.Type = AILayer_Colonization.ContinentData.ContinentType.Island;
			}
		}

		public void UpdateRegionOwnership()
		{
			this.EmpireWithRegion.Clear();
			this.ColonizationPercentByEmpire.Clear();
			this.OverallColonizationPercent = 0f;
			for (int i = 0; i < this.Regions.Count; i++)
			{
				if (this.Regions[i].City != null)
				{
					int num = this.EmpireWithRegion.IndexOf(this.Regions[i].City.Empire);
					if (num < 0)
					{
						num = this.EmpireWithRegion.Count;
						this.EmpireWithRegion.Add(this.Regions[i].City.Empire);
						this.ColonizationPercentByEmpire.Add(0f);
					}
					this.OverallColonizationPercent += 1f;
					List<float> colonizationPercentByEmpire;
					List<float> list = colonizationPercentByEmpire = this.ColonizationPercentByEmpire;
					int index2;
					int index = index2 = num;
					float num2 = colonizationPercentByEmpire[index2];
					list[index] = num2 + 1f;
				}
			}
			this.OverallColonizationPercent /= (float)this.LandRegionCount;
			for (int j = 0; j < this.ColonizationPercentByEmpire.Count; j++)
			{
				List<float> colonizationPercentByEmpire2;
				List<float> list2 = colonizationPercentByEmpire2 = this.ColonizationPercentByEmpire;
				int index2;
				int index3 = index2 = j;
				float num2 = colonizationPercentByEmpire2[index2];
				list2[index3] = num2 / (float)this.LandRegionCount;
			}
		}

		public void FillPotentialRegion(List<Region> potentialRegions)
		{
			for (int i = 0; i < this.Regions.Count; i++)
			{
				if (this.Regions[i].City == null)
				{
					if (this.Regions[i].IsLand)
					{
						potentialRegions.Add(this.Regions[i]);
					}
				}
			}
		}

		public void Release()
		{
			this.DataOwner = null;
			this.Regions.Clear();
			this.EmpireWithRegion.Clear();
			this.ColonizationPercentByEmpire.Clear();
		}

		private IWorldPositionningService worldPositionningService;

		public enum ContinentType
		{
			Island,
			Unique,
			Continent,
			Water
		}
	}
}
