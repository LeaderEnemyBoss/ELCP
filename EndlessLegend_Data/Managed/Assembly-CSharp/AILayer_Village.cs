using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.AI;
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
		IWorldPositionningService service = Services.GetService<IGameService>().Game.Services.GetService<IWorldPositionningService>();
		IIntelligenceAIHelper service2 = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		village = null;
		region = service.GetRegion(regionIndex);
		if (region == null || region.MinorEmpire == null || !service2.IsContinentAcccessible(empire, regionIndex))
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
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
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
		int num = this.departmentOfTheInterior.Cities.Count + 1;
		float num2;
		base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>().TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, out num2, false);
		if (base.AIEntity.Empire.SimulationObject.Tags.Contains("FactionTraitCultists7") && !this.departmentOfForeignAffairs.IsInWarWithSomeone() && num2 > 500f && num < this.departmentOfDefense.Armies.Count - 2)
		{
			num = this.departmentOfDefense.Armies.Count - 2;
		}
		return num;
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
				goto IL_1BC;
			}
			if (flag2)
			{
				DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(this.regions[index].City.Empire);
				if (diplomaticRelation != null && !(diplomaticRelation.State.Name != DiplomaticRelationState.Names.War))
				{
					goto IL_1BC;
				}
				this.CancelMessagesFor(this.regions[index].Index);
			}
			else
			{
				this.CancelMessagesFor(this.regions[index].Index);
			}
			IL_1A5:
			int index2 = index;
			index = index2 + 1;
			continue;
			IL_1BC:
			if (!flag2 && this.regions[index].City == null && flag)
			{
				this.CancelMessagesFor(this.regions[index].Index);
				goto IL_1A5;
			}
			if (this.regions[index].MinorEmpire == null)
			{
				this.CancelMessagesFor(this.regions[index].Index);
				goto IL_1A5;
			}
			BarbarianCouncil agency = this.regions[index].MinorEmpire.GetAgency<BarbarianCouncil>();
			if (agency == null)
			{
				this.CancelMessagesFor(this.regions[index].Index);
				goto IL_1A5;
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
					float num3;
					if (flag2)
					{
						num3 = this.ComputePerceivedPathDistanceOfVillageToConvert(village);
						if (num3 == 0f)
						{
							this.CancelMessagesFor(this.regions[index].Index, village.GUID);
							goto IL_425;
						}
					}
					else
					{
						num3 = this.ComputePerceivedDistanceOfVillageToConvert(village);
					}
					GlobalObjectiveMessage globalObjectiveMessage = base.AIEntity.AIPlayer.Blackboard.FindFirst<GlobalObjectiveMessage>(BlackboardLayerID.Empire, (GlobalObjectiveMessage match) => match.RegionIndex == this.regions[index].Index && match.SubObjectifGUID == village.GUID && match.ObjectiveType == this.ObjectiveType);
					if (globalObjectiveMessage == null || globalObjectiveMessage.State == BlackboardMessage.StateValue.Message_Canceled)
					{
						globalObjectiveMessage = base.GenerateObjective(base.ObjectiveType, this.regions[index].Index, village.GUID);
					}
					globalObjectiveMessage.TimeOut = 1;
					this.workingList.Add(globalObjectiveMessage);
					if (num3 > num)
					{
						num = num3;
					}
					this.workingListDistance.Add(num3);
					if (flag2)
					{
						float villageConversionCost = AILayer_Village.GetVillageConversionCost(base.AIEntity.Empire as MajorEmpire, village);
						if (num2 == 0f)
						{
							num2 = villageConversionCost;
						}
						else if (villageConversionCost < num2)
						{
							num2 = villageConversionCost;
						}
						this.workingListConversionCost.Add(villageConversionCost);
					}
				}
				IL_425:;
			}
			goto IL_1A5;
		}
		DepartmentOfScience.ConstructibleElement constructibleElement;
		if (this.workingList.Count < 2 && flag2 && base.AIEntity.Empire.SimulationObject.Tags.Contains("FactionTraitCultists7") && base.AIEntity.Empire.GetAgency<DepartmentOfScience>().TechnologyDatabase.TryGetValue("TechnologyDefinitionShip", out constructibleElement) && base.AIEntity.Empire.GetAgency<DepartmentOfScience>().GetTechnologyState(constructibleElement) == DepartmentOfScience.ConstructibleElement.State.Available)
		{
			bool flag3 = false;
			if (this.departmentOfForeignAffairs.IsInWarWithSomeone() && this.departmentOfTheInterior.Cities.Count > 0)
			{
				for (int j = 0; j < this.departmentOfTheInterior.Cities[0].Region.Borders.Length; j++)
				{
					if (this.worldPositionningService.GetRegion(this.departmentOfTheInterior.Cities[0].Region.Borders[j].NeighbourRegionIndex).IsOcean)
					{
						flag3 = true;
						break;
					}
				}
			}
			if (this.ChooseOverseaRegion() != null || flag3)
			{
				if (!DepartmentOfScience.CanBuyoutResearch(base.AIEntity.Empire))
				{
					OrderQueueResearch orderQueueResearch = new OrderQueueResearch(base.AIEntity.Empire.Index, constructibleElement);
					orderQueueResearch.InsertAtFirstPlace = true;
					base.AIEntity.Empire.PlayerControllers.AI.PostOrder(orderQueueResearch);
				}
				else
				{
					float num4 = -base.AIEntity.Empire.GetAgency<DepartmentOfScience>().GetBuyOutTechnologyCost(constructibleElement) * 1.2f;
					if (base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>().IsTransferOfResourcePossible(base.AIEntity.Empire, DepartmentOfTheTreasury.Resources.TechnologiesBuyOut, ref num4))
					{
						OrderBuyOutTechnology order = new OrderBuyOutTechnology(base.AIEntity.Empire.Index, "TechnologyDefinitionShip");
						Ticket ticket;
						base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, null);
					}
				}
			}
		}
		int num5 = -1;
		for (int k = 0; k < this.workingList.Count; k++)
		{
			bool flag4 = false;
			GlobalObjectiveMessage globalObjectiveMessage2 = this.workingList[k];
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
					float operand = this.workingListDistance[k];
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
						float operand2 = this.workingListConversionCost[k];
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
						heuristicValue2.Boost(0.5f, "Pacified", new object[0]);
						if (village2.PointOfInterest.PointOfInterestImprovement != null)
						{
							heuristicValue2.Boost(0.1f, "Built", new object[0]);
						}
						if (village2.Region.City != null && this.departmentOfForeignAffairs.IsAtWarWith(village2.Region.City.Empire))
						{
							heuristicValue2.Boost(0.1f, "Steal from enemy", new object[0]);
						}
					}
					if (village2.HasBeenConverted)
					{
						heuristicValue2.Boost(-0.05f, "Converted by someone else", new object[0]);
					}
					int num6 = 0;
					int num7 = 0;
					for (int l = 0; l < 6; l++)
					{
						WorldPosition neighbourTile = this.worldPositionningService.GetNeighbourTile(village2.WorldPosition, (WorldOrientation)l, 1);
						if (neighbourTile.IsValid && this.worldPositionningService.IsExploitable(neighbourTile, 0) && this.worldPositionningService.GetRegion(neighbourTile) == village2.Region)
						{
							num6++;
							byte anomalyType = this.worldPositionningService.GetAnomalyType(neighbourTile);
							if (!StaticString.IsNullOrEmpty(this.worldPositionningService.GetAnomalyTypeMappingName(anomalyType)))
							{
								num7++;
							}
						}
					}
					HeuristicValue heuristicValue8 = new HeuristicValue(0f);
					heuristicValue8.Add((float)num6, "exploitable tile around", new object[0]);
					heuristicValue8.Divide(6f, "Max tile", new object[0]);
					heuristicValue8.Multiply(0.5f, "(constant)", new object[0]);
					heuristicValue2.Boost(heuristicValue8, "FIDS generated by village", new object[0]);
					HeuristicValue heuristicValue9 = new HeuristicValue(0f);
					heuristicValue9.Add((float)num7, "anomalies around", new object[0]);
					heuristicValue9.Divide(6f, "Max tile", new object[0]);
					heuristicValue9.Multiply(0.5f, "(constant)", new object[0]);
					heuristicValue2.Boost(heuristicValue9, "FIDS generated by village", new object[0]);
					if (!this.departmentOfForeignAffairs.IsInWarWithSomeone() && !village2.HasBeenConverted)
					{
						float num8;
						base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>().TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, out num8, false);
						if (this.workingListConversionCost[k] * 1.5f <= num8)
						{
							heuristicValue.Boost(1f, "Peacetime Cultist Boost", new object[0]);
							heuristicValue2.Boost(0.9f, "Peacetime Cultist Boost", new object[0]);
							if (village2.Region.Owner == base.AIEntity.Empire)
							{
								heuristicValue2.Boost(0.2f, "Peacetime Cultist Boost", new object[0]);
							}
							if (this.workingListDistance[k] < 14f && village2.HasBeenPacified && (village2.Region.City == null || village2.Region.City.Empire == base.AIEntity.Empire) && village2.PointOfInterest.PointOfInterestImprovement != null)
							{
								flag4 = true;
							}
						}
					}
				}
				else
				{
					if (region.City != null && region.City.Empire == base.AIEntity.Empire)
					{
						heuristicValue.Add(agentValue, "Internal pacification global score", new object[0]);
						if (!this.departmentOfForeignAffairs.IsInWarWithSomeone())
						{
							heuristicValue.Boost(1f, "Internal pacification global score", new object[0]);
						}
					}
					else
					{
						heuristicValue.Add(agentValue2, "External pacification global score", new object[0]);
					}
					HeuristicValue heuristicValue10 = new HeuristicValue(0f);
					heuristicValue10.Add(heuristicValue3, "Distance ratio", new object[0]);
					heuristicValue10.Subtract(1f, "(constant)", new object[0]);
					if (village2.Region.City == null)
					{
						heuristicValue2.Add(0.6f, "(constant) nobody own the region", new object[0]);
						heuristicValue10.Multiply(0.5f, "(constant)", new object[0]);
						heuristicValue2.Boost(heuristicValue10, "Distance boost", new object[0]);
						HeuristicValue heuristicValue11 = new HeuristicValue(0f);
						heuristicValue11.Add(this.colonizationLayer.GetColonizationInterest(globalObjectiveMessage2.RegionIndex), "Region colo interest", new object[0]);
						heuristicValue11.Multiply(0.1f, "(constant)", new object[0]);
						heuristicValue2.Boost(heuristicValue11, "Colonization boost", new object[0]);
					}
					else if (region.City.Empire == base.AIEntity.Empire)
					{
						heuristicValue2.Boost(1f, "I own the region", new object[0]);
						heuristicValue10.Multiply(0.1f, "(constant)", new object[0]);
						heuristicValue2.Boost(heuristicValue10, "Distance boost", new object[0]);
					}
					else
					{
						heuristicValue2.Add(0.8f, "(constant) I don't own the region", new object[0]);
						heuristicValue10.Multiply(0.1f, "(constant)", new object[0]);
						heuristicValue2.Boost(heuristicValue10, "Distance boost", new object[0]);
					}
				}
				this.workingList[k].GlobalPriority = heuristicValue;
				this.workingList[k].LocalPriority = heuristicValue2;
				if (flag2)
				{
					if (k == 0)
					{
						num2 = this.workingListConversionCost[k];
					}
					else if (this.workingList[k].LocalPriority.Value > this.workingList[k - 1].LocalPriority.Value)
					{
						num2 = this.workingListConversionCost[k];
					}
					if (flag4)
					{
						if (num5 == -1)
						{
							num5 = k;
						}
						else if (this.workingListDistance[k] < this.workingListDistance[num5])
						{
							num5 = k;
						}
					}
				}
			}
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
				Account account = base.AIEntity.GetLayer<AILayer_AccountManager>().TryGetAccount(AILayer_AccountManager.ConversionAccountName);
				if (account != null && num2 > account.GetAvailableAmount())
				{
					conversionNeedPrestigeMessage.NeededPrestigePoints = num2 - account.GetAvailableAmount();
				}
			}
			if (num5 >= 0)
			{
				this.workingList[num5].LocalPriority.Boost(1f, "premiumboost", new object[0]);
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
			if (region2.ContinentID == region.ContinentID && region2 != null && !this.regions.Contains(region2))
			{
				this.regions.Add(region2);
			}
		}
	}

	private void CancelMessagesFor(int regionIndex, GameEntityGUID villageGUID)
	{
		Blackboard<BlackboardLayerID, BlackboardMessage> blackboard = base.AIEntity.AIPlayer.Blackboard;
		BlackboardLayerID blackboardLayerID = BlackboardLayerID.Empire;
		BlackboardLayerID layerID = blackboardLayerID;
		Func<GlobalObjectiveMessage, bool> <>9__0;
		Func<GlobalObjectiveMessage, bool> filter;
		if ((filter = <>9__0) == null)
		{
			filter = (<>9__0 = ((GlobalObjectiveMessage match) => match.RegionIndex == regionIndex && match.SubObjectifGUID == villageGUID && match.ObjectiveType == this.ObjectiveType));
		}
		foreach (GlobalObjectiveMessage message in blackboard.GetMessages<GlobalObjectiveMessage>(layerID, filter))
		{
			base.AIEntity.AIPlayer.Blackboard.CancelMessage(message);
		}
	}

	private void CancelMessagesFor(int regionIndex)
	{
		Blackboard<BlackboardLayerID, BlackboardMessage> blackboard = base.AIEntity.AIPlayer.Blackboard;
		BlackboardLayerID blackboardLayerID = BlackboardLayerID.Empire;
		BlackboardLayerID layerID = blackboardLayerID;
		Func<GlobalObjectiveMessage, bool> <>9__0;
		Func<GlobalObjectiveMessage, bool> filter;
		if ((filter = <>9__0) == null)
		{
			filter = (<>9__0 = ((GlobalObjectiveMessage match) => match.RegionIndex == regionIndex && match.ObjectiveType == this.ObjectiveType));
		}
		foreach (GlobalObjectiveMessage message in blackboard.GetMessages<GlobalObjectiveMessage>(layerID, filter))
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
		return (float)this.worldPositionningService.GetDistance(city.WorldPosition, village.WorldPosition);
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
			if (base.AIEntity.Empire.SimulationObject.Tags.Contains("FactionTraitCultists7") && base.AIEntity.Empire.SimulationObject.Tags.Contains(AILayer_Village.TagConversionTrait) && this.departmentOfDefense.TechnologyDefinitionShipState == DepartmentOfScience.ConstructibleElement.State.Researched)
			{
				Region region3 = this.ChooseOverseaRegion();
				if (region3 != null)
				{
					this.AddRegion(region3, false);
				}
			}
		}
	}

	private bool IsVillageValidForObjective(bool canConvert, bool isAtWar, Village village)
	{
		if (canConvert)
		{
			if (village.HasBeenConverted && village.Converter == base.AIEntity.Empire as MajorEmpire)
			{
				return false;
			}
			if (village.HasBeenConverted && village.Converter != base.AIEntity.Empire as MajorEmpire)
			{
				MajorEmpire converter = village.Converter;
				DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(converter);
				return diplomaticRelation != null && diplomaticRelation.State.Name == DiplomaticRelationState.Names.War;
			}
			if (village.HasBeenPacified && !village.HasBeenInfected)
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
			if (DepartmentOfTheInterior.IsArmyAbleToConvert(this.departmentOfDefense.Armies[i], true))
			{
				this.conversionArmies.Add(this.departmentOfDefense.Armies[i]);
			}
		}
	}

	private float ComputePerceivedPathDistanceOfVillageToConvert(Village village)
	{
		if (this.conversionArmies.Count == 0)
		{
			return 0f;
		}
		IPathfindingService service = Services.GetService<IGameService>().Game.Services.GetService<IPathfindingService>();
		City city = this.departmentOfTheInterior.MainCity;
		if (village.Region.City != null && village.Region.City.Empire == base.AIEntity.Empire)
		{
			city = village.Region.City;
			return (float)this.worldPositionningService.GetDistance(city.WorldPosition, village.WorldPosition);
		}
		if (city == null)
		{
			city = this.departmentOfTheInterior.Cities[0];
		}
		PathfindingContext pathfindingContext = this.conversionArmies[0].GenerateContext();
		pathfindingContext.Greedy = true;
		PathfindingResult pathfindingResult = service.FindPath(pathfindingContext, city.WorldPosition, village.WorldPosition, PathfindingManager.RequestMode.Default, null, PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnorePOI, null);
		if (pathfindingResult != null)
		{
			return (float)pathfindingResult.CompletPathLength;
		}
		return 0f;
	}

	private Region ChooseOverseaRegion()
	{
		List<Region> list = new List<Region>();
		foreach (Continent continent in this.worldPositionningService.World.Continents)
		{
			List<Region> list2 = new List<Region>();
			foreach (int regionIndex in continent.RegionList)
			{
				Region region = this.worldPositionningService.GetRegion(regionIndex);
				if (region.IsLand)
				{
					list2.Add(region);
				}
			}
			if (list2.Count > 0 && !this.AlreadyConvertedVillagesOnContinent(list2))
			{
				for (int k = list2.Count - 1; k >= 0; k--)
				{
					if (!this.HasConvertableVillages(list2[k]))
					{
						list2.RemoveAt(k);
					}
				}
				if (list2.Count > 0)
				{
					list.AddRange(list2);
				}
			}
		}
		if (list.Count > 0)
		{
			return this.GetClosestRegion(list);
		}
		return null;
	}

	private Region GetClosestRegion(List<Region> Regions)
	{
		City city = this.departmentOfTheInterior.MainCity;
		if (city == null)
		{
			city = this.departmentOfTheInterior.Cities[0];
		}
		WorldPosition worldPosition;
		if (city == null)
		{
			if (this.departmentOfDefense.Armies.Count <= 0)
			{
				return null;
			}
			worldPosition = this.departmentOfDefense.Armies[0].WorldPosition;
		}
		else
		{
			worldPosition = city.WorldPosition;
		}
		int num = int.MaxValue;
		Region result = null;
		foreach (Region region in Regions)
		{
			int distance = this.worldPositionningService.GetDistance(region.Barycenter, worldPosition);
			if (distance <= num)
			{
				num = distance;
				result = region;
			}
		}
		return result;
	}

	private bool AlreadyConvertedVillagesOnContinent(List<Region> Regions)
	{
		foreach (Region region in Regions)
		{
			if (region.City != null && region.City.Empire == base.AIEntity.Empire)
			{
				return true;
			}
		}
		MajorEmpire majorEmpire = base.AIEntity.Empire as MajorEmpire;
		if (majorEmpire != null)
		{
			foreach (Village village in majorEmpire.ConvertedVillages)
			{
				if (Regions.Contains(village.Region))
				{
					return true;
				}
			}
			return false;
		}
		return true;
	}

	private bool HasConvertableVillages(Region region)
	{
		if (region.City != null)
		{
			DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(region.City.Empire);
			if (diplomaticRelation != null && diplomaticRelation.State.Name == DiplomaticRelationState.Names.War)
			{
				return true;
			}
		}
		else
		{
			BarbarianCouncil agency = region.MinorEmpire.GetAgency<BarbarianCouncil>();
			if (agency != null)
			{
				foreach (Village village in agency.Villages)
				{
					if (!village.HasBeenConverted)
					{
						return true;
					}
					if (village.HasBeenConverted && village.Converter != base.AIEntity.Empire as MajorEmpire)
					{
						DiplomaticRelation diplomaticRelation2 = this.departmentOfForeignAffairs.GetDiplomaticRelation(village.Converter);
						if (diplomaticRelation2 != null && diplomaticRelation2.State.Name == DiplomaticRelationState.Names.War)
						{
							return true;
						}
					}
				}
				return false;
			}
		}
		return false;
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
