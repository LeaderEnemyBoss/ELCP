using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AILayer_Village : AILayerWithObjective
{
	public AILayer_Village() : base("Village")
	{
	}

	public static bool GetRegionAndVillageData(global::Empire empire, int regionIndex, GameEntityGUID villageGUID, out Region region, out Village village)
	{
		village = null;
		region = null;
		if (empire == null)
		{
			return false;
		}
		IGameService service = Services.GetService<IGameService>();
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		IIntelligenceAIHelper service3 = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		village = null;
		region = service2.GetRegion(regionIndex);
		if (region == null || region.MinorEmpire == null || !service3.IsContinentAcccessible(empire, regionIndex))
		{
			return false;
		}
		BarbarianCouncil agency = region.MinorEmpire.GetAgency<BarbarianCouncil>();
		if (agency == null)
		{
			return false;
		}
		for (int i = 0; i < agency.Villages.Count; i++)
		{
			village = agency.Villages[i];
			if (village.GUID == villageGUID)
			{
				return true;
			}
		}
		return false;
	}

	public static float GetVillageBribeCost(MajorEmpire empire, Village village)
	{
		float propertyValue = village.GetPropertyValue(SimulationProperties.BribeCost);
		float amount = DepartmentOfTheTreasury.ConvertCostsTo(DepartmentOfTheTreasury.Resources.EmpireMoney, "Bribe", propertyValue, empire);
		return DepartmentOfTheTreasury.ComputeCostWithReduction(empire, amount, "Bribe", CostReduction.ReductionType.Buyout);
	}

	public static float GetVillageConversionCost(MajorEmpire empire, Village village)
	{
		if (empire == null || village == null)
		{
			return 0f;
		}
		ConstructionCost[] convertionCost = ArmyAction_Convert.GetConvertionCost(empire, village);
		if (convertionCost.Length >= 1)
		{
			return convertionCost[0].GetValue(empire);
		}
		return 0f;
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		this.worldPositionningService = gameService.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.gameEntityRepositoryService = gameService.Game.Services.GetService<IGameEntityRepositoryService>();
		Diagnostics.Assert(this.gameEntityRepositoryService != null);
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		this.departmentOfDefense = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		this.departmentOfForeignAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		this.intelligenceAIHelper = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		base.AIEntity.RegisterPass(AIEntity.Passes.RefreshObjectives.ToString(), "AILayer_Village_RefreshObjectives", new AIEntity.AIAction(this.RefreshObjectives), this, new StaticString[0]);
		this.conversionArmies = new List<Army>();
		this.personalityAIHelper = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		this.turnLimitBeforeHardPacification = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_Village.RegistryPath, "TurnLimitBeforeHardPacification"), this.turnLimitBeforeHardPacification);
		this.colonizationLayer = base.AIEntity.GetLayer<AILayer_Colonization>();
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
		this.departmentOfTheInterior = null;
		this.departmentOfDefense = null;
		this.departmentOfForeignAffairs = null;
		this.intelligenceAIHelper = null;
		if (this.conversionObjectives != null)
		{
			this.conversionObjectives.Clear();
			this.conversionObjectives = null;
		}
		this.conversionArmies = null;
	}

	protected override int GetCommanderLimit()
	{
		int count = this.departmentOfTheInterior.Cities.Count;
		return count + 1;
	}

	protected override bool IsObjectiveValid(GlobalObjectiveMessage objective)
	{
		if (!(objective.ObjectiveType == base.ObjectiveType))
		{
			return false;
		}
		bool flag = this.departmentOfForeignAffairs.IsInWarWithSomeone();
		bool flag2 = base.AIEntity.Empire.SimulationObject.Tags.Contains(AILayer_Village.TagConversionTrait);
		Region region = this.worldPositionningService.GetRegion(objective.RegionIndex);
		if (region.City != null && region.City.Empire != base.AIEntity.Empire)
		{
			if (!flag2)
			{
				return false;
			}
			DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(region.City.Empire);
			if (diplomaticRelation == null || diplomaticRelation.State.Name != DiplomaticRelationState.Names.War)
			{
				return false;
			}
		}
		if (!flag2 && region.City == null && flag)
		{
			return false;
		}
		if (region.MinorEmpire == null)
		{
			return false;
		}
		IGameEntity gameEntity;
		if (!this.gameEntityRepositoryService.TryGetValue(objective.SubObjectifGUID, out gameEntity) || !(gameEntity is Village))
		{
			return false;
		}
		Village village = gameEntity as Village;
		return this.IsVillageValidForObjective(flag2, flag, village);
	}

	protected override bool IsObjectiveValid(StaticString objectiveType, int regionIndex, bool checkLocalPriority = false)
	{
		return true;
	}

	protected override void RefreshObjectives(StaticString context, StaticString pass)
	{
		base.RefreshObjectives(context, pass);
		bool flag = this.departmentOfForeignAffairs.IsInWarWithSomeone();
		bool flag2 = base.AIEntity.Empire.SimulationObject.Tags.Contains(AILayer_Village.TagConversionTrait);
		this.FillUpRegions();
		AILayer_Strategy layer = base.AIEntity.GetLayer<AILayer_Strategy>();
		float agentValue = layer.StrategicNetwork.GetAgentValue("InternalMilitary");
		float agentValue2 = layer.StrategicNetwork.GetAgentValue("Pacification");
		base.GatherObjectives(base.ObjectiveType, true, ref this.globalObjectiveMessages);
		base.ValidateMessages(ref this.globalObjectiveMessages);
		this.RefreshArmiesThatCanConvert();
		this.workingList.Clear();
		this.workingListConversionCost.Clear();
		this.workingListDistance.Clear();
		float num = 0f;
		float num2 = 0f;
		int index = 0;
		while (index < this.regions.Count)
		{
			if (this.regions[index].City == null || this.regions[index].City.Empire == base.AIEntity.Empire)
			{
				goto IL_1D3;
			}
			if (flag2)
			{
				DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(this.regions[index].City.Empire);
				if (diplomaticRelation != null && !(diplomaticRelation.State.Name != DiplomaticRelationState.Names.War))
				{
					goto IL_1D3;
				}
				this.CancelMessagesFor(this.regions[index].Index);
			}
			else
			{
				this.CancelMessagesFor(this.regions[index].Index);
			}
			IL_41C:
			index++;
			continue;
			IL_1D3:
			if (!flag2 && this.regions[index].City == null && flag)
			{
				this.CancelMessagesFor(this.regions[index].Index);
				goto IL_41C;
			}
			if (this.regions[index].MinorEmpire == null)
			{
				this.CancelMessagesFor(this.regions[index].Index);
				goto IL_41C;
			}
			BarbarianCouncil agency = this.regions[index].MinorEmpire.GetAgency<BarbarianCouncil>();
			if (agency == null)
			{
				this.CancelMessagesFor(this.regions[index].Index);
				goto IL_41C;
			}
			for (int i = 0; i < agency.Villages.Count; i++)
			{
				Village village = agency.Villages[i];
				if (!this.IsVillageValidForObjective(flag2, flag, village))
				{
					this.CancelMessagesFor(this.regions[index].Index, village.GUID);
				}
				else
				{
					GlobalObjectiveMessage globalObjectiveMessage = base.AIEntity.AIPlayer.Blackboard.FindFirst<GlobalObjectiveMessage>(BlackboardLayerID.Empire, (GlobalObjectiveMessage match) => match.RegionIndex == this.regions[index].Index && match.SubObjectifGUID == village.GUID && match.ObjectiveType == this.ObjectiveType);
					if (globalObjectiveMessage == null || globalObjectiveMessage.State == BlackboardMessage.StateValue.Message_Canceled)
					{
						globalObjectiveMessage = base.GenerateObjective(base.ObjectiveType, this.regions[index].Index, village.GUID);
					}
					globalObjectiveMessage.TimeOut = 1;
					this.workingList.Add(globalObjectiveMessage);
					float num3 = this.ComputePerceivedDistanceOfVillageToConvert(village);
					if (num3 > num)
					{
						num = num3;
					}
					this.workingListDistance.Add(num3);
					if (flag2)
					{
						float villageConversionCost = AILayer_Village.GetVillageConversionCost(base.AIEntity.Empire as MajorEmpire, village);
						if (villageConversionCost > num2)
						{
							num2 = villageConversionCost;
						}
						this.workingListConversionCost.Add(villageConversionCost);
					}
				}
			}
			goto IL_41C;
		}
		if (flag2)
		{
			ConversionNeedPrestigeMessage conversionNeedPrestigeMessage = base.AIEntity.AIPlayer.Blackboard.GetMessages<ConversionNeedPrestigeMessage>(BlackboardLayerID.Empire).FirstOrDefault<ConversionNeedPrestigeMessage>();
			if (conversionNeedPrestigeMessage == null)
			{
				conversionNeedPrestigeMessage = new ConversionNeedPrestigeMessage(BlackboardLayerID.Empire);
				base.AIEntity.AIPlayer.Blackboard.AddMessage(conversionNeedPrestigeMessage);
			}
			conversionNeedPrestigeMessage.TimeOut = 1;
			conversionNeedPrestigeMessage.NeededPrestigePoints = 0f;
			conversionNeedPrestigeMessage.Priority = 0f;
			if (num2 > 0f)
			{
				AILayer_AccountManager layer2 = base.AIEntity.GetLayer<AILayer_AccountManager>();
				Account account = layer2.TryGetAccount(AILayer_AccountManager.ConversionAccountName);
				if (account != null && num2 > account.GetAvailableAmount())
				{
					conversionNeedPrestigeMessage.NeededPrestigePoints = num2 - account.GetAvailableAmount();
				}
			}
		}
		for (int j = 0; j < this.workingList.Count; j++)
		{
			GlobalObjectiveMessage globalObjectiveMessage2 = this.workingList[j];
			Region region = this.worldPositionningService.GetRegion(globalObjectiveMessage2.RegionIndex);
			IGameEntity gameEntity;
			if (!this.gameEntityRepositoryService.TryGetValue(globalObjectiveMessage2.SubObjectifGUID, out gameEntity) || !(gameEntity is Village))
			{
				this.CancelMessagesFor(globalObjectiveMessage2.RegionIndex, globalObjectiveMessage2.SubObjectifGUID);
			}
			else
			{
				Village village2 = gameEntity as Village;
				HeuristicValue heuristicValue = new HeuristicValue(0f);
				HeuristicValue heuristicValue2 = new HeuristicValue(0f);
				HeuristicValue heuristicValue3 = new HeuristicValue(0f);
				if (num > 0f)
				{
					HeuristicValue heuristicValue4 = new HeuristicValue(0f);
					float operand = this.workingListDistance[j];
					heuristicValue4.Add(operand, "Objective distance", new object[0]);
					heuristicValue4.Divide(num, "Max distance", new object[0]);
					heuristicValue3.Add(1f, "constant", new object[0]);
					heuristicValue3.Subtract(heuristicValue4, "Objective/max distance", new object[0]);
				}
				else
				{
					heuristicValue3.Add(1f, "Max distance is 0", new object[0]);
				}
				if (flag2)
				{
					heuristicValue2.Add(heuristicValue3, "Distance ratio", new object[0]);
					heuristicValue.Add(0.8f, "(constant)", new object[0]);
					if (region != null && region.City != null && region.City.Empire.Index != base.AIEntity.Empire.Index)
					{
						heuristicValue2.Boost(-0.2f, "Avoid other empire region", new object[0]);
					}
					HeuristicValue heuristicValue5 = new HeuristicValue(0f);
					if (num2 > 0f)
					{
						HeuristicValue heuristicValue6 = new HeuristicValue(0f);
						float operand2 = this.workingListConversionCost[j];
						heuristicValue6.Add(operand2, "Objective distance", new object[0]);
						heuristicValue6.Divide(num2, "Max distance", new object[0]);
						heuristicValue5.Add(1f, "(constant)", new object[0]);
						heuristicValue5.Subtract(heuristicValue6, "Objective/max cost", new object[0]);
					}
					else
					{
						heuristicValue5.Add(1f, "Max cost is 0", new object[0]);
					}
					HeuristicValue heuristicValue7 = new HeuristicValue(0f);
					heuristicValue7.Add(heuristicValue5, "Cost ratio", new object[0]);
					heuristicValue7.Subtract(0.5f, "(constant)", new object[0]);
					heuristicValue7.Multiply(0.5f, "(constant)", new object[0]);
					heuristicValue2.Boost(heuristicValue7, "Cost boost", new object[0]);
					if (village2.HasBeenPacified)
					{
						heuristicValue2.Boost(0.05f, "Pacified", new object[0]);
						if (village2.PointOfInterest.PointOfInterestImprovement != null)
						{
							heuristicValue2.Boost(0.1f, "Built", new object[0]);
						}
					}
					if (village2.HasBeenConverted)
					{
						heuristicValue2.Boost(-0.05f, "Converted by someone else", new object[0]);
					}
					int num4 = 0;
					for (int k = 0; k < 6; k++)
					{
						WorldPosition neighbourTile = this.worldPositionningService.GetNeighbourTile(village2.WorldPosition, (WorldOrientation)k, 1);
						if (neighbourTile.IsValid && this.worldPositionningService.IsExploitable(neighbourTile, 0))
						{
							num4++;
						}
					}
					HeuristicValue heuristicValue8 = new HeuristicValue(0f);
					heuristicValue8.Add((float)num4, "exploitable tile around", new object[0]);
					heuristicValue8.Divide(6f, "Max tile", new object[0]);
					heuristicValue8.Multiply(0.5f, "(constant)", new object[0]);
					heuristicValue2.Boost(heuristicValue8, "FIDS generated by village", new object[0]);
				}
				else
				{
					if (region.City != null && region.City.Empire == base.AIEntity.Empire)
					{
						heuristicValue.Add(agentValue, "Internal pacification global score", new object[0]);
					}
					else
					{
						heuristicValue.Add(agentValue2, "External pacification global score", new object[0]);
					}
					HeuristicValue heuristicValue9 = new HeuristicValue(0f);
					heuristicValue9.Add(heuristicValue3, "Distance ratio", new object[0]);
					heuristicValue9.Subtract(1f, "(constant)", new object[0]);
					if (village2.Region.City == null)
					{
						heuristicValue2.Add(0.6f, "(constant) nobody own the region", new object[0]);
						heuristicValue9.Multiply(0.5f, "(constant)", new object[0]);
						heuristicValue2.Boost(heuristicValue9, "Distance boost", new object[0]);
						HeuristicValue heuristicValue10 = new HeuristicValue(0f);
						heuristicValue10.Add(this.colonizationLayer.GetColonizationInterest(globalObjectiveMessage2.RegionIndex), "Region colo interest", new object[0]);
						heuristicValue10.Multiply(0.1f, "(constant)", new object[0]);
						heuristicValue2.Boost(heuristicValue10, "Colonization boost", new object[0]);
					}
					else
					{
						heuristicValue2.Add(0.8f, "(constant) I own the region", new object[0]);
						heuristicValue9.Multiply(0.1f, "(constant)", new object[0]);
						heuristicValue2.Boost(heuristicValue9, "Distance boost", new object[0]);
					}
				}
				this.workingList[j].GlobalPriority = heuristicValue;
				this.workingList[j].LocalPriority = heuristicValue2;
			}
		}
	}

	private void AddRegion(Region region, bool addNeighbour)
	{
		if (region == null || !this.intelligenceAIHelper.IsContinentAcccessible(base.AIEntity.Empire, region.Index))
		{
			return;
		}
		if (region.City != null && region.City.Empire != base.AIEntity.Empire)
		{
			return;
		}
		if (!this.regions.Contains(region))
		{
			this.regions.Add(region);
		}
		if (!addNeighbour)
		{
			return;
		}
		for (int i = 0; i < region.Borders.Length; i++)
		{
			Region.Border border = region.Borders[i];
			Region region2 = this.worldPositionningService.World.Regions[border.NeighbourRegionIndex];
			if (region2.ContinentID == region.ContinentID)
			{
				if (region2 != null && !this.regions.Contains(region2))
				{
					this.regions.Add(region2);
				}
			}
		}
	}

	private void CancelMessagesFor(int regionIndex, GameEntityGUID villageGUID)
	{
		foreach (GlobalObjectiveMessage message in base.AIEntity.AIPlayer.Blackboard.GetMessages<GlobalObjectiveMessage>(BlackboardLayerID.Empire, (GlobalObjectiveMessage match) => match.RegionIndex == regionIndex && match.SubObjectifGUID == villageGUID && match.ObjectiveType == this.ObjectiveType))
		{
			base.AIEntity.AIPlayer.Blackboard.CancelMessage(message);
		}
	}

	private void CancelMessagesFor(int regionIndex)
	{
		foreach (GlobalObjectiveMessage message in base.AIEntity.AIPlayer.Blackboard.GetMessages<GlobalObjectiveMessage>(BlackboardLayerID.Empire, (GlobalObjectiveMessage match) => match.RegionIndex == regionIndex && match.ObjectiveType == this.ObjectiveType))
		{
			base.AIEntity.AIPlayer.Blackboard.CancelMessage(message);
		}
	}

	private float ComputePerceivedDistanceOfVillageToConvert(Village village)
	{
		City city = this.departmentOfTheInterior.MainCity;
		if (village.Region.City != null && village.Region.City.Empire == base.AIEntity.Empire)
		{
			city = village.Region.City;
		}
		if (city == null)
		{
			city = this.departmentOfTheInterior.Cities[0];
		}
		float num = (float)this.worldPositionningService.GetDistance(city.WorldPosition, village.WorldPosition);
		float num2 = float.MaxValue;
		for (int i = 0; i < this.departmentOfDefense.Armies.Count; i++)
		{
			float num3 = (float)this.worldPositionningService.GetDistance(this.departmentOfDefense.Armies[i].WorldPosition, village.WorldPosition);
			if (num3 < num2)
			{
				num2 = num3;
			}
		}
		if (num2 == 3.40282347E+38f)
		{
			return num;
		}
		return (num + num2) / 2f;
	}

	private void FillUpRegions()
	{
		this.regions.Clear();
		for (int i = 0; i < this.departmentOfTheInterior.Cities.Count; i++)
		{
			Region region = this.departmentOfTheInterior.Cities[i].Region;
			this.AddRegion(region, true);
		}
		MajorEmpire majorEmpire = base.AIEntity.Empire as MajorEmpire;
		if (majorEmpire != null)
		{
			for (int j = 0; j < majorEmpire.ConvertedVillages.Count; j++)
			{
				Village village = majorEmpire.ConvertedVillages[j];
				this.AddRegion(village.Region, true);
			}
		}
		if (this.departmentOfTheInterior.Cities.Count > 0)
		{
			AILayer_Colonization layer = base.AIEntity.GetLayer<AILayer_Colonization>();
			base.GatherObjectives(layer.ObjectiveType, false, ref this.globalObjectiveMessages);
			if (this.globalObjectiveMessages.Count > 0)
			{
				int regionIndex = this.globalObjectiveMessages[0].RegionIndex;
				Region region2 = this.worldPositionningService.GetRegion(regionIndex);
				this.AddRegion(region2, false);
			}
		}
	}

	private bool IsVillageValidForObjective(bool canConvert, bool isAtWar, Village village)
	{
		if (canConvert)
		{
			if (village.HasBeenConverted && village.Converter == base.AIEntity.Empire)
			{
				return false;
			}
			if (village.HasBeenPacified)
			{
				return true;
			}
		}
		else if (village.HasBeenPacified)
		{
			return false;
		}
		return this.departmentOfForeignAffairs.CanAttack(village);
	}

	private void RefreshArmiesThatCanConvert()
	{
		this.conversionArmies.Clear();
		for (int i = 0; i < this.departmentOfDefense.Armies.Count; i++)
		{
			DepartmentOfTheInterior.IsArmyAbleToConvert(this.departmentOfDefense.Armies[i], true);
		}
	}

	public static readonly StaticString TagConversionTrait = new StaticString("FactionTraitCultists14");

	public static readonly float DeboostRatio = -0.3f;

	public static string RegistryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_Village";

	private List<Army> conversionArmies;

	private List<GlobalObjectiveMessage> conversionObjectives = new List<GlobalObjectiveMessage>();

	private DepartmentOfDefense departmentOfDefense;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private IIntelligenceAIHelper intelligenceAIHelper;

	private IPersonalityAIHelper personalityAIHelper;

	private List<Region> regions = new List<Region>();

	private float turnLimitBeforeHardPacification = 10f;

	private AILayer_Colonization colonizationLayer;

	private List<GlobalObjectiveMessage> workingList = new List<GlobalObjectiveMessage>();

	private List<float> workingListConversionCost = new List<float>();

	private List<float> workingListDistance = new List<float>();

	private IWorldPositionningService worldPositionningService;
}
