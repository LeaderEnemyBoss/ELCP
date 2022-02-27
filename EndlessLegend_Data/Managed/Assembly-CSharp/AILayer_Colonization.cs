using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Session;
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
					return;
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
		if (reader.ReadVersionAttribute() >= 2)
		{
			this.settlerCount = reader.GetAttribute<int>("SettlerCount");
		}
		base.ReadXml(reader);
	}

	public override void WriteXml(XmlWriter writer)
	{
		if (writer.WriteVersionAttribute(2) >= 2)
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
			if (!this.continentData[i].EmpireWithRegion.Contains(base.AIEntity.Empire) && this.continentData[i].LandRegionCount != 0 && this.continentData[i].OverallColonizationPercent <= 0.2f && this.continentData[i].CostalRegionRatio >= 0.8f)
			{
				this.continentData[i].FillPotentialRegion(this.potentialRegions);
			}
		}
		return this.GetClosestRegion(this.potentialRegions);
	}

	private Region ChooseOverseaRegion()
	{
		Region result;
		if (this.departmentOfDefense.TechnologyDefinitionShipState != DepartmentOfScience.ConstructibleElement.State.Researched)
		{
			result = null;
		}
		else
		{
			Region region = this.ChooseNeutralIsland();
			if (region != null && this.CanPathToRegion(region))
			{
				result = region;
			}
			else
			{
				this.potentialRegions.Clear();
				for (int i = 0; i < this.continentData.Length; i++)
				{
					if (this.continentData[i].LandRegionCount != 0 && this.continentData[i].OverallColonizationPercent <= 0.8f)
					{
						this.continentData[i].FillPotentialRegion(this.potentialRegions);
					}
				}
				result = this.GetClosestRegion(this.potentialRegions, true);
			}
		}
		return result;
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
		for (int i = 0; i < this.departmentOfDefense.Armies.Count; i++)
		{
			if (this.departmentOfDefense.Armies[i].IsSettler)
			{
				army = this.departmentOfDefense.Armies[i];
				break;
			}
		}
		if (army == null)
		{
			return null;
		}
		Region region = this.worldPositionningService.GetRegion(army.WorldPosition);
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
		list.Sort((Region left, Region right) => this.worldPositionningService.GetDistance(left.Barycenter, army.WorldPosition).CompareTo(this.worldPositionningService.GetDistance(right.Barycenter, army.WorldPosition)));
		return list[0];
	}

	private Region GetClosestRegion(List<Region> regions)
	{
		base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		int num = int.MaxValue;
		Region result = null;
		int index;
		int index2;
		for (index = 0; index < regions.Count; index = index2 + 1)
		{
			if (regions[index].City == null && regions[index].IsLand && !this.globalObjectiveMessages.Exists((GlobalObjectiveMessage match) => match.RegionIndex == regions[index].Index) && !this.RegionTooDangerous(regions[index].Index))
			{
				int num2 = this.worldAtlasHelper.ComputeBirdEyeDistanceBetweenRegionAndEmpire(base.AIEntity.Empire, regions[index]);
				if (num2 < num)
				{
					num = num2;
					result = regions[index];
				}
			}
			index2 = index;
		}
		return result;
	}

	private void FillNeighbourgs(Region source, List<Region> neighbourgs)
	{
		for (int i = 0; i < source.Borders.Length; i++)
		{
			Region region = this.worldPositionningService.GetRegion(source.Borders[i].NeighbourRegionIndex);
			if (!region.IsRegionColonized() && region.IsLand && source.ContinentID == region.ContinentID && !neighbourgs.Contains(region))
			{
				neighbourgs.Add(region);
			}
		}
	}

	public int CurrentSettlerCount { get; set; }

	public int WantedNewCity { get; private set; }

	public bool WantToColonizeOversea { get; set; }

	public static bool IsAbleToColonize(global::Empire empire)
	{
		ReadOnlyCollection<UnitBodyDefinition> availableUnitBodyDefinitions = ((IUnitDesignDatabase)empire.GetAgency<DepartmentOfDefense>()).AvailableUnitBodyDefinitions;
		for (int i = 0; i < availableUnitBodyDefinitions.Count; i++)
		{
			if (!availableUnitBodyDefinitions[i].Tags.Contains("Hidden") && (availableUnitBodyDefinitions[i].CheckUnitAbility(UnitAbility.ReadonlyColonize, -1) || availableUnitBodyDefinitions[i].CheckUnitAbility(UnitAbility.ReadonlyResettle, -1)) && DepartmentOfTheTreasury.CheckConstructiblePrerequisites(empire, availableUnitBodyDefinitions[i], new string[]
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
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		base.AIEntity.RegisterPass(AIEntity.Passes.RefreshObjectives.ToString(), "AILayer_Colonization_RefreshObjectives", new AIEntity.AIAction(this.RefreshObjectives), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.ExecuteNeeds.ToString(), "AILayer_Colonization_ExecuteNeeds", new AIEntity.AIAction(this.ExecuteNeeds), this, new StaticString[0]);
		Diagnostics.Assert(AIScheduler.Services.GetService<IWorldPositionEvaluationAIHelper>() != null);
		this.worldAtlasHelper = AIScheduler.Services.GetService<IWorldAtlasAIHelper>();
		Diagnostics.Assert(this.worldAtlasHelper != null);
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		this.departmentOfDefense = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		this.departmentOfForeignAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		this.departmentOfIndustry = base.AIEntity.Empire.GetAgency<DepartmentOfIndustry>();
		this.aiLayerStrategy = base.AIEntity.GetLayer<AILayer_Strategy>();
		this.pathfindingService = service.Game.Services.GetService<IPathfindingService>();
		this.CreateContinentInformation();
		this.ForceShipyardResearch = false;
		if (Services.GetService<ISessionService>().Session.GetLobbyData<string>("Scenario", null) == "DLC21")
		{
			this.ForceShipyardResearch = true;
		}
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
		this.departmentOfForeignAffairs = null;
		this.departmentOfTheInterior = null;
		this.departmentOfIndustry = null;
		this.aiLayerStrategy = null;
		for (int i = 0; i < this.continentData.Length; i++)
		{
			this.continentData[i].Release();
		}
		this.continentData = new AILayer_Colonization.ContinentData[0];
		this.pathfindingService = null;
		this.PathingOrigin = null;
	}

	protected override bool IsObjectiveValid(StaticString objectiveType, int regionIndex, bool checkLocalPriority = false)
	{
		if (this.PathingOrigin == null)
		{
			return false;
		}
		if (this.RegionTooDangerous(regionIndex))
		{
			return false;
		}
		Region region = this.worldPositionningService.GetRegion(regionIndex);
		return region.IsLand && !region.IsRegionColonized() && this.CanPathToRegion(region);
	}

	protected override void RefreshObjectives(StaticString context, StaticString pass)
	{
		this.PathingOrigin = this.GetPathingOrigin();
		DepartmentOfForeignAffairs agency = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		base.RefreshObjectives(context, pass);
		this.CurrentSettlerCount = this.GetSettlerCount();
		this.ComputeGlobalPriority();
		this.ComputeBestDefensiveArmypower();
		this.UpdateContinentData();
		int num = this.GetNumberOfAcceptedColonization();
		this.WantedNewCity = num;
		base.GatherObjectives(AICommanderMissionDefinition.AICommanderCategory.Colonization.ToString(), ref this.globalObjectiveMessages);
		base.ValidateMessages(ref this.globalObjectiveMessages);
		for (int i = 0; i < this.globalObjectiveMessages.Count; i++)
		{
			GlobalObjectiveMessage globalObjectiveMessage = this.globalObjectiveMessages[i];
			if (this.IsObjectiveValid(AICommanderMissionDefinition.AICommanderCategory.Colonization.ToString(), globalObjectiveMessage.RegionIndex, false))
			{
				if (agency.IsInWarWithSomeone())
				{
					using (IEnumerator<Army> enumerator = Intelligence.GetArmiesInRegion(globalObjectiveMessage.RegionIndex).GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							Army army = enumerator.Current;
							if (army.Empire != base.AIEntity.Empire && army.Empire is MajorEmpire && agency.IsAtWarWith(army.Empire))
							{
								base.AIEntity.AIPlayer.Blackboard.CancelMessage(this.globalObjectiveMessages[i]);
							}
						}
						goto IL_18E;
					}
				}
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
			IL_18E:;
		}
		if (this.departmentOfTheInterior.Cities.Count == 0 && this.globalObjectiveMessages.Count == 0)
		{
			Region region3 = this.ChooseLastHopeRegion();
			if (region3 == null)
			{
				return;
			}
			this.CreateObjectiveFor(region3);
			num--;
		}
		Region region2 = null;
		for (int j = 0; j < num; j++)
		{
			region2 = this.GetNextRegionToColonize();
			if (region2 != null)
			{
				this.CreateObjectiveFor(region2);
			}
		}
		if (this.ForceShipyardResearch)
		{
			this.TryForceShipyardResearch();
		}
		else if (region2 == null && num > 0)
		{
			if (this.worldPositionningService.World.Continents.Length > 1)
			{
				this.TryForceShipyardResearch();
			}
			else if (this.continentData.Any((AILayer_Colonization.ContinentData c) => c.LandRegionCount > 0 && c.OverallColonizationPercent <= 0.8f))
			{
				this.TryForceShipyardResearch();
			}
		}
		this.WantToColonizeOversea = false;
		for (int k = 0; k < this.globalObjectiveMessages.Count; k++)
		{
			GlobalObjectiveMessage globalObjectiveMessage2 = this.globalObjectiveMessages[k];
			Region region = this.worldPositionningService.GetRegion(globalObjectiveMessage2.RegionIndex);
			if (!Array.Find<AILayer_Colonization.ContinentData>(this.continentData, (AILayer_Colonization.ContinentData match) => match.ContinentId == region.ContinentID).EmpireWithRegion.Contains(base.AIEntity.Empire))
			{
				this.WantToColonizeOversea = true;
				return;
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
		if (this.CurrentSettlerCount == 0 && !AILayer_Colonization.IsAbleToColonize(base.AIEntity.Empire))
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
				float propertyValue = this.departmentOfTheInterior.Cities[j].GetPropertyValue(SimulationProperties.Population);
				num7 += num8 * propertyValue;
				num6 += propertyValue;
			}
			for (int k = 0; k < num; k++)
			{
				float num9 = propertyBaseValue2 - (float)(this.departmentOfTheInterior.Cities.Count - 1 + num) * propertyBaseValue;
				float num10 = 1f;
				num7 += num9 * num10;
				num6 += num10;
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
			if (this.departmentOfTheInterior.Cities[j].Camp != null)
			{
				num += this.GetSettlerCount(this.departmentOfTheInterior.Cities[j].Camp);
			}
			ConstructionQueue constructionQueue = this.departmentOfIndustry.GetConstructionQueue(this.departmentOfTheInterior.Cities[j]);
			if (constructionQueue != null)
			{
				for (int k = constructionQueue.Length - 1; k >= 0; k--)
				{
					if (constructionQueue.PeekAt(k).ConstructibleElementName.ToString().Contains("Settler"))
					{
						num++;
					}
				}
			}
		}
		return num;
	}

	private int GetSettlerCount(IGarrison garrison)
	{
		int num = 0;
		for (int i = 0; i < garrison.StandardUnits.Count; i++)
		{
			if (garrison.StandardUnits[i].IsSettler)
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
			if (this.aiLayerStrategy.IsAtWar() && this.CurrentSettlerCount < 2)
			{
				base.GlobalPriority.Boost(-0.5f, "At war", new object[0]);
			}
			else if (this.aiLayerStrategy.WantWarWithSomeone() && this.CurrentSettlerCount < 2)
			{
				base.GlobalPriority.Boost(-0.2f, "Want war but not at war", new object[0]);
			}
			else if (this.departmentOfTheInterior.Cities.Count == 1)
			{
				base.GlobalPriority.Boost(0.2f, "I am sure you want more than one city...", new object[0]);
			}
		}
		global::Game game = Services.GetService<IGameService>().Game as global::Game;
		if (!this.aiLayerStrategy.IsAtWar() && this.departmentOfTheInterior.Cities.Count < 5 && this.departmentOfTheInterior.Cities.Count * 20 < game.Turn && base.AIEntity.Empire.GetPropertyValue(SimulationProperties.NetEmpireApproval) > 70f)
		{
			float operand = Mathf.Min(1f, 0.04f * (float)(game.Turn - this.departmentOfTheInterior.Cities.Count * 20));
			base.GlobalPriority.Boost(operand, "low city count boost", new object[0]);
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
			global::Game game = Services.GetService<IGameService>().Game as global::Game;
			if (!this.aiLayerStrategy.IsAtWar() && this.departmentOfTheInterior.Cities.Count < 5 && this.departmentOfTheInterior.Cities.Count * 20 < game.Turn && base.AIEntity.Empire.GetPropertyValue(SimulationProperties.NetEmpireApproval) > 70f)
			{
				float operand = Mathf.Min(1f, 0.04f * (float)(game.Turn - this.departmentOfTheInterior.Cities.Count * 20));
				objectiveMessage.LocalPriority.Boost(operand, "low city count boost", new object[0]);
			}
		}
		objectiveMessage.TimeOut = 1;
	}

	private Garrison GetPathingOrigin()
	{
		for (int i = 0; i < this.departmentOfDefense.Armies.Count; i++)
		{
			if (this.departmentOfDefense.Armies[i].IsSettler)
			{
				return this.departmentOfDefense.Armies[i];
			}
		}
		int num = 0;
		if (num >= this.departmentOfTheInterior.Cities.Count)
		{
			return null;
		}
		return this.departmentOfTheInterior.Cities[num];
	}

	private bool CanPathToRegion(Region region)
	{
		foreach (Region region2 in this.worldPositionningService.GetNeighbourRegions(region, false, false))
		{
			if (region2.Owner != null && region2.Owner.Index == base.AIEntity.Empire.Index)
			{
				return true;
			}
		}
		PathfindingContext pathfindingContext;
		if (this.PathingOrigin is Army)
		{
			pathfindingContext = (this.PathingOrigin as Army).GenerateContext();
		}
		else
		{
			pathfindingContext = new PathfindingContext(GameEntityGUID.Zero, base.AIEntity.Empire, PathfindingMovementCapacity.Ground);
			pathfindingContext.AddMovementCapacity(PathfindingMovementCapacity.FrozenWater);
			pathfindingContext.RefreshProperties(1f, 100f, false, false, 100f, 100f);
			DepartmentOfScience agency = base.AIEntity.Empire.GetAgency<DepartmentOfScience>();
			DepartmentOfScience.ConstructibleElement technology;
			if (agency.TechnologyDatabase.TryGetValue("TechnologyDefinitionShip", out technology) && agency.GetTechnologyState(technology) == DepartmentOfScience.ConstructibleElement.State.Researched)
			{
				pathfindingContext.AddMovementCapacity(PathfindingMovementCapacity.Water);
			}
		}
		WorldPosition[] worldPositions = region.WorldPositions;
		int j = 0;
		while (j < worldPositions.Length)
		{
			WorldPosition worldPosition = worldPositions[j];
			if (!this.worldPositionningService.IsWaterTile(worldPosition) && !this.worldPositionningService.HasRidge(worldPosition) && this.pathfindingService.IsTileStopable(worldPosition, pathfindingContext, (PathfindingFlags)0, null))
			{
				if (this.pathfindingService.FindPath(pathfindingContext, (this.PathingOrigin as IWorldPositionable).WorldPosition, worldPosition, PathfindingManager.RequestMode.Default, null, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreEncounterAreas | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreTerraformDevices | PathfindingFlags.IgnoreKaijuGarrisons, null) == null)
				{
					if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
					{
						Diagnostics.Log("ELCP {0}/{1} AILayer_Colonization:CanPathToRegion FAILED! from {3} to {2}/{5} with {4}", new object[]
						{
							base.AIEntity.Empire,
							this.PathingOrigin.LocalizedName,
							worldPosition,
							(this.PathingOrigin as IWorldPositionable).WorldPosition,
							pathfindingContext.MovementCapacities,
							region.LocalizedName
						});
					}
					return false;
				}
				return true;
			}
			else
			{
				j++;
			}
		}
		return false;
	}

	private Region GetClosestRegion(List<Region> regions, bool checkPathing = false)
	{
		List<KeyValuePair<Region, int>> list = new List<KeyValuePair<Region, int>>();
		int index;
		int index2;
		for (index = 0; index < regions.Count; index = index2 + 1)
		{
			if (!regions[index].IsRegionColonized() && regions[index].IsLand && !this.globalObjectiveMessages.Exists((GlobalObjectiveMessage match) => match.RegionIndex == regions[index].Index) && !this.RegionTooDangerous(regions[index].Index))
			{
				int value = this.worldAtlasHelper.ComputeBirdEyeDistanceBetweenRegionAndEmpire(base.AIEntity.Empire, regions[index]);
				list.Add(new KeyValuePair<Region, int>(regions[index], value));
			}
			index2 = index;
		}
		list.Sort((KeyValuePair<Region, int> left, KeyValuePair<Region, int> right) => left.Value.CompareTo(right.Value));
		foreach (KeyValuePair<Region, int> keyValuePair in list)
		{
			if (!checkPathing)
			{
				return keyValuePair.Key;
			}
			if (this.CanPathToRegion(keyValuePair.Key))
			{
				return keyValuePair.Key;
			}
		}
		return null;
	}

	private void ComputeBestDefensiveArmypower()
	{
		float a = 0f;
		foreach (Army army in this.departmentOfDefense.Armies)
		{
			if (!(army is KaijuArmy) && !army.IsNaval && !army.IsSolitary)
			{
				a = Mathf.Max(a, army.GetPropertyValue(SimulationProperties.MilitaryPower));
			}
		}
		this.bestDefensiveArmypower = a;
	}

	private bool RegionTooDangerous(int regionIndex)
	{
		foreach (Army army in Intelligence.GetArmiesInRegion(regionIndex))
		{
			if (army.Empire != base.AIEntity.Empire && !army.IsNaval && army.Empire is MajorEmpire)
			{
				if (this.departmentOfForeignAffairs.IsAtWarWith(army.Empire))
				{
					return true;
				}
				if (this.departmentOfForeignAffairs.CanAttack(army) && army.GetPropertyValue(SimulationProperties.MilitaryPower) > this.bestDefensiveArmypower * 2f)
				{
					return true;
				}
			}
		}
		foreach (Region region in this.worldPositionningService.GetNeighbourRegions(this.worldPositionningService.GetRegion(regionIndex), false, false))
		{
			if (region.IsLand && region.Owner is MajorEmpire && this.departmentOfForeignAffairs.IsAtWarWith(region.Owner))
			{
				return true;
			}
		}
		return false;
	}

	private void TryForceShipyardResearch()
	{
		DepartmentOfScience.ConstructibleElement constructibleElement;
		if (base.AIEntity.Empire.GetAgency<DepartmentOfScience>().TechnologyDatabase.TryGetValue("TechnologyDefinitionShip", out constructibleElement) && base.AIEntity.Empire.GetAgency<DepartmentOfScience>().GetTechnologyState(constructibleElement) == DepartmentOfScience.ConstructibleElement.State.Available)
		{
			bool flag = false;
			using (IEnumerator<City> enumerator = this.departmentOfTheInterior.Cities.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					for (int i = 0; i < enumerator.Current.Region.Borders.Length; i++)
					{
						if (this.worldPositionningService.GetRegion(enumerator.Current.Region.Borders[i].NeighbourRegionIndex).IsOcean)
						{
							flag = true;
							break;
						}
					}
				}
			}
			if (flag)
			{
				if (!DepartmentOfScience.CanBuyoutResearch(base.AIEntity.Empire))
				{
					OrderQueueResearch order = new OrderQueueResearch(base.AIEntity.Empire.Index, constructibleElement, true);
					base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order);
					return;
				}
				float num = -base.AIEntity.Empire.GetAgency<DepartmentOfScience>().GetBuyOutTechnologyCost(constructibleElement) * 1.05f;
				if (base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>().IsTransferOfResourcePossible(base.AIEntity.Empire, DepartmentOfTheTreasury.Resources.TechnologiesBuyOut, ref num))
				{
					OrderBuyOutTechnology order2 = new OrderBuyOutTechnology(base.AIEntity.Empire.Index, "TechnologyDefinitionShip");
					Ticket ticket;
					base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order2, out ticket, null);
				}
			}
		}
	}

	protected override void ExecuteNeeds(StaticString context, StaticString pass)
	{
		base.ExecuteNeeds(context, pass);
		if (!this.IsActive())
		{
			return;
		}
		if (this.CurrentSettlerCount <= 1)
		{
			return;
		}
		if (this.globalObjectiveMessages.Count > 0)
		{
			return;
		}
		this.cityWithRemovedSettlerIndex = -1;
		AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>().RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_TryConvertSuperfluousSettlersToPops));
	}

	private SynchronousJobState SynchronousJob_TryConvertSuperfluousSettlersToPops()
	{
		for (int i = 0; i < this.departmentOfTheInterior.NonInfectedCities.Count; i++)
		{
			for (int j = 0; j < this.departmentOfTheInterior.Cities[i].StandardUnits.Count; j++)
			{
				if (this.departmentOfTheInterior.Cities[i].StandardUnits[j].IsSettler)
				{
					this.cityWithRemovedSettlerIndex = i;
					OrderDisbandUnits order = new OrderDisbandUnits(base.AIEntity.Empire.Index, new GameEntityGUID[]
					{
						this.departmentOfTheInterior.Cities[i].StandardUnits[j].GUID
					});
					Ticket ticket;
					base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.RemoveSettlerResponse));
					return SynchronousJobState.Success;
				}
			}
			if (this.departmentOfTheInterior.Cities[i].Camp != null)
			{
				for (int k = 0; k < this.departmentOfTheInterior.Cities[i].Camp.StandardUnits.Count; k++)
				{
					if (this.departmentOfTheInterior.Cities[i].Camp.StandardUnits[k].IsSettler)
					{
						this.cityWithRemovedSettlerIndex = i;
						OrderDisbandUnits order2 = new OrderDisbandUnits(base.AIEntity.Empire.Index, new GameEntityGUID[]
						{
							this.departmentOfTheInterior.Cities[i].Camp.StandardUnits[k].GUID
						});
						Ticket ticket2;
						base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order2, out ticket2, new EventHandler<TicketRaisedEventArgs>(this.RemoveSettlerResponse));
						return SynchronousJobState.Success;
					}
				}
			}
		}
		return SynchronousJobState.Success;
	}

	private void RemoveSettlerResponse(object sender, TicketRaisedEventArgs args)
	{
		if (args.Result == PostOrderResponse.Processed)
		{
			City city = this.departmentOfTheInterior.Cities[this.cityWithRemovedSettlerIndex];
			float propertyValue = city.GetPropertyValue(SimulationProperties.Population);
			float propertyValue2 = city.GetPropertyValue(SimulationProperties.CityGrowthStock);
			float num = DepartmentOfTheInterior.ComputeGrowthLimit(city.Empire.SimulationObject, propertyValue);
			float num2 = DepartmentOfTheInterior.ComputeGrowthLimit(city.Empire.SimulationObject, propertyValue + 1f);
			float num3 = propertyValue2 / num;
			num3 = num2 * num3;
			num3 -= propertyValue2;
			if (num3 > 0f)
			{
				Diagnostics.Log("ELCP {0} Disbanded Settler in {1}, tranfering {2} food ({3} -> {4})", new object[]
				{
					city.Empire,
					city.LocalizedName,
					num3,
					propertyValue2,
					propertyValue2 + num3
				});
				OrderTransferResources order = new OrderTransferResources(base.AIEntity.Empire.Index, DepartmentOfTheTreasury.Resources.CityGrowth, num3, city.GUID);
				base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order);
			}
		}
		this.cityWithRemovedSettlerIndex = -1;
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

	private Garrison PathingOrigin;

	private IPathfindingService pathfindingService;

	private float bestDefensiveArmypower;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private bool ForceShipyardResearch;

	private DepartmentOfIndustry departmentOfIndustry;

	private int cityWithRemovedSettlerIndex;

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
					int landRegionCount = this.LandRegionCount;
					this.LandRegionCount = landRegionCount + 1;
				}
			}
			this.CostalRegionRatio = (float)continent.CostalRegionList.Length / (float)this.LandRegionCount;
			this.Type = AILayer_Colonization.ContinentData.ContinentType.Continent;
			if (this.LandRegionCount == 0)
			{
				this.Type = AILayer_Colonization.ContinentData.ContinentType.Water;
				return;
			}
			if (this.CostalRegionRatio > 0.8f)
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
					int index;
					float num2 = (colonizationPercentByEmpire = this.ColonizationPercentByEmpire)[index = num];
					colonizationPercentByEmpire[index] = num2 + 1f;
				}
			}
			this.OverallColonizationPercent /= (float)this.LandRegionCount;
			for (int j = 0; j < this.ColonizationPercentByEmpire.Count; j++)
			{
				List<float> colonizationPercentByEmpire2;
				int index2;
				float num3 = (colonizationPercentByEmpire2 = this.ColonizationPercentByEmpire)[index2 = j];
				colonizationPercentByEmpire2[index2] = num3 / (float)this.LandRegionCount;
			}
		}

		public void FillPotentialRegion(List<Region> potentialRegions)
		{
			for (int i = 0; i < this.Regions.Count; i++)
			{
				if (!this.Regions[i].IsRegionColonized() && this.Regions[i].IsLand)
				{
					potentialRegions.Add(this.Regions[i]);
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
