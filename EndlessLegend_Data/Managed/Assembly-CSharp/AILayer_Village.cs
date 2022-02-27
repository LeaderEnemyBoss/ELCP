using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Amplitude;
using Amplitude.Extensions;
using Amplitude.Unity.AI;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AILayer_Village : AILayerWithObjective, IXmlSerializable
{
	public AILayer_Village() : base("Village")
	{
		this.conversionObjectives = new List<GlobalObjectiveMessage>();
		this.regions = new List<Region>();
		this.turnLimitBeforeHardPacification = 10f;
		this.workingList = new List<GlobalObjectiveMessage>();
		this.workingListConversionCost = new List<float>();
		this.workingListDistance = new List<float>();
		this.suspendedPacificationOrders = new List<StaticString>();
		this.questvillagesToPrioritize = new List<AILayer_Village.QuestBTVillage>();
	}

	static AILayer_Village()
	{
		AILayer_Village.DeboostRatio = -0.3f;
		AILayer_Village.RegistryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_Village";
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
		this.departmentOfCreepingNodes = base.AIEntity.Empire.GetAgency<DepartmentOfCreepingNodes>();
		this.departmentOfScience = base.AIEntity.Empire.GetAgency<DepartmentOfScience>();
		this.departmentOfInternalAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfInternalAffairs>();
		this.intelligenceAIHelper = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		base.AIEntity.RegisterPass(AIEntity.Passes.RefreshObjectives.ToString(), "AILayer_Village_RefreshObjectives", new AIEntity.AIAction(this.RefreshObjectives), this, new StaticString[0]);
		this.conversionArmies = new List<Army>();
		this.personalityAIHelper = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		this.turnLimitBeforeHardPacification = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_Village.RegistryPath, "TurnLimitBeforeHardPacification"), this.turnLimitBeforeHardPacification);
		this.colonizationLayer = base.AIEntity.GetLayer<AILayer_Colonization>();
		this.questManagementService = service.Game.Services.GetService<IQuestManagementService>();
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
		this.ActiveQuests = null;
		this.departmentOfTheInterior = null;
		this.departmentOfDefense = null;
		this.departmentOfForeignAffairs = null;
		this.departmentOfCreepingNodes = null;
		this.departmentOfScience = null;
		this.departmentOfInternalAffairs = null;
		this.intelligenceAIHelper = null;
		if (this.conversionObjectives != null)
		{
			this.conversionObjectives.Clear();
			this.conversionObjectives = null;
		}
		this.conversionArmies = null;
		this.questManagementService = null;
		this.suspendedPacificationOrders.Clear();
		this.questvillagesToPrioritize.Clear();
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
		for (int i = 0; i < this.suspendedPacificationOrders.Count; i++)
		{
			if (!this.questManagementService.IsQuestRunningForEmpire(this.suspendedPacificationOrders[i], base.AIEntity.Empire))
			{
				this.suspendedPacificationOrders.RemoveAt(i);
				i--;
			}
		}
		for (int j = 0; j < this.questvillagesToPrioritize.Count; j++)
		{
			if (!this.questManagementService.IsQuestRunningForEmpire(this.questvillagesToPrioritize[j].questName, base.AIEntity.Empire))
			{
				this.questvillagesToPrioritize.RemoveAt(j);
				j--;
			}
		}
		if (this.SuspendPacification && !this.departmentOfScience.CanParley())
		{
			this.CancelAllMessages();
			return;
		}
		this.ActiveQuests = this.departmentOfInternalAffairs.QuestJournal.Read(QuestState.InProgress);
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
				goto IL_298;
			}
			if (flag2)
			{
				DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(this.regions[index].City.Empire);
				if (diplomaticRelation != null && !(diplomaticRelation.State.Name != DiplomaticRelationState.Names.War))
				{
					goto IL_298;
				}
				this.CancelMessagesFor(this.regions[index].Index);
			}
			else
			{
				this.CancelMessagesFor(this.regions[index].Index);
			}
			IL_281:
			int index2 = index;
			index = index2 + 1;
			continue;
			IL_298:
			if (!flag2 && this.regions[index].City == null && flag)
			{
				this.CancelMessagesFor(this.regions[index].Index);
				goto IL_281;
			}
			if (this.regions[index].MinorEmpire == null)
			{
				this.CancelMessagesFor(this.regions[index].Index);
				goto IL_281;
			}
			BarbarianCouncil agency = this.regions[index].MinorEmpire.GetAgency<BarbarianCouncil>();
			if (agency == null)
			{
				this.CancelMessagesFor(this.regions[index].Index);
				goto IL_281;
			}
			for (int k = 0; k < agency.Villages.Count; k++)
			{
				Village village = agency.Villages[k];
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
							goto IL_501;
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
				IL_501:;
			}
			goto IL_281;
		}
		this.RefreshObjectives_EvaluateSailNeed(flag2, flag);
		int num4 = -1;
		for (int l = 0; l < this.workingList.Count; l++)
		{
			bool flag3 = false;
			GlobalObjectiveMessage globalObjectiveMessage2 = this.workingList[l];
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
					float operand = this.workingListDistance[l];
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
						float operand2 = this.workingListConversionCost[l];
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
					int num5 = 0;
					int num6 = 0;
					for (int m = 0; m < 6; m++)
					{
						WorldPosition neighbourTile = this.worldPositionningService.GetNeighbourTile(village2.WorldPosition, (WorldOrientation)m, 1);
						if (neighbourTile.IsValid && this.worldPositionningService.IsExploitable(neighbourTile, 0) && this.worldPositionningService.GetRegion(neighbourTile) == village2.Region)
						{
							num5++;
							byte anomalyType = this.worldPositionningService.GetAnomalyType(neighbourTile);
							if (!StaticString.IsNullOrEmpty(this.worldPositionningService.GetAnomalyTypeMappingName(anomalyType)))
							{
								num6++;
							}
						}
					}
					HeuristicValue heuristicValue8 = new HeuristicValue(0f);
					heuristicValue8.Add((float)num5, "exploitable tile around", new object[0]);
					heuristicValue8.Divide(6f, "Max tile", new object[0]);
					heuristicValue8.Multiply(0.5f, "(constant)", new object[0]);
					heuristicValue2.Boost(heuristicValue8, "FIDS generated by village", new object[0]);
					HeuristicValue heuristicValue9 = new HeuristicValue(0f);
					heuristicValue9.Add((float)num6, "anomalies around", new object[0]);
					heuristicValue9.Divide(6f, "Max tile", new object[0]);
					heuristicValue9.Multiply(0.5f, "(constant)", new object[0]);
					heuristicValue2.Boost(heuristicValue9, "FIDS generated by village", new object[0]);
					if (!flag && !village2.HasBeenConverted)
					{
						float num7;
						base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>().TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, out num7, false);
						if (this.workingListConversionCost[l] * 1.5f <= num7)
						{
							heuristicValue.Boost(1f, "Peacetime Cultist Boost", new object[0]);
							heuristicValue2.Boost(0.9f, "Peacetime Cultist Boost", new object[0]);
							if (village2.Region.Owner == base.AIEntity.Empire)
							{
								heuristicValue2.Boost(0.2f, "Peacetime Cultist Boost", new object[0]);
							}
							if (this.workingListDistance[l] < 14f && village2.HasBeenPacified && (village2.Region.City == null || village2.Region.City.Empire == base.AIEntity.Empire) && village2.PointOfInterest.PointOfInterestImprovement != null)
							{
								flag3 = true;
							}
						}
					}
				}
				else
				{
					if (region.City != null && region.City.Empire == base.AIEntity.Empire)
					{
						heuristicValue.Add(agentValue, "Internal pacification global score", new object[0]);
						if (!flag)
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
						if (this.departmentOfCreepingNodes != null && this.departmentOfCreepingNodes.Nodes.Any((CreepingNode CN) => CN.Region.Index == village2.Region.Index) && !flag)
						{
							heuristicValue11.Boost(1f, "bloom in region", new object[0]);
						}
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
				if (!this.questvillagesToPrioritize.Exists((AILayer_Village.QuestBTVillage V) => V.objectiveGUID == (gameEntity as Village).GUID))
				{
					this.workingList[l].GlobalPriority = heuristicValue;
					this.workingList[l].LocalPriority = heuristicValue2;
				}
				else
				{
					this.workingList[l].GlobalPriority = new HeuristicValue(1f);
					this.workingList[l].LocalPriority = new HeuristicValue(1f);
				}
				if (flag2)
				{
					if (l == 0)
					{
						num2 = this.workingListConversionCost[l];
					}
					else if (this.workingList[l].LocalPriority.Value > this.workingList[l - 1].LocalPriority.Value)
					{
						num2 = this.workingListConversionCost[l];
					}
					if (flag3)
					{
						if (num4 == -1)
						{
							num4 = l;
						}
						else if (this.workingListDistance[l] < this.workingListDistance[num4])
						{
							num4 = l;
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
			if (num4 >= 0)
			{
				this.workingList[num4].LocalPriority.Boost(1f, "premiumboost", new object[0]);
			}
		}
	}

	private void AddRegion(Region region, bool addNeighbour)
	{
		if (region == null || !this.intelligenceAIHelper.IsContinentAcccessible(base.AIEntity.Empire, region.Index) || region.MinorEmpire == null)
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
			if (region.IsLand && region2.ContinentID == region.ContinentID && region2 != null && !this.regions.Contains(region2))
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
			if (this.departmentOfCreepingNodes != null && !this.departmentOfForeignAffairs.IsInWarWithSomeone())
			{
				foreach (CreepingNode creepingNode in this.departmentOfCreepingNodes.Nodes)
				{
					if (!creepingNode.IsUnderConstruction && AILayer_Exploration.IsTravelAllowedInNode(base.AIEntity.Empire, creepingNode) && this.departmentOfForeignAffairs.CanMoveOn((int)this.worldPositionningService.GetRegionIndex(creepingNode.WorldPosition), false))
					{
						this.AddRegion(creepingNode.Region, true);
					}
				}
			}
		}
	}

	private bool IsVillageValidForObjective(bool canConvert, bool isAtWar, Village village)
	{
		if (this.SuspendPacification && village.PointOfInterest.Interaction.IsLocked(base.AIEntity.Empire.Index, "ArmyActionParley"))
		{
			return false;
		}
		if (this.SuspendPacification && (village.PointOfInterest.Interaction.Bits & base.AIEntity.Empire.Bits) != 0)
		{
			return false;
		}
		for (int i = 0; i < this.ActiveQuests.Count; i++)
		{
			if (village.PointOfInterest.GUID == this.ActiveQuests[i].QuestGiverGUID)
			{
				return false;
			}
		}
		if (village.HasBeenInfected)
		{
			return false;
		}
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

	private void CancelAllMessages()
	{
		foreach (GlobalObjectiveMessage message in base.AIEntity.AIPlayer.Blackboard.GetMessages<GlobalObjectiveMessage>(BlackboardLayerID.Empire, (GlobalObjectiveMessage match) => match.ObjectiveType == base.ObjectiveType))
		{
			base.AIEntity.AIPlayer.Blackboard.CancelMessage(message);
		}
	}

	public void SuspendPacifications(StaticString questname)
	{
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.Log("ELCP {0}: SuspendPacifications ordered by {1}", new object[]
			{
				base.AIEntity.Empire,
				questname
			});
		}
		this.suspendedPacificationOrders.AddOnce(questname);
	}

	public void ResumePacifications(StaticString questname)
	{
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.Log("ELCP {0}: ResumePacifications ordered by {1}", new object[]
			{
				base.AIEntity.Empire,
				questname
			});
		}
		this.suspendedPacificationOrders.Remove(questname);
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteVersionAttribute(2);
		base.WriteXml(writer);
		writer.WriteStartElement("suspendedPacificationOrders");
		writer.WriteAttributeString<int>("Count", this.suspendedPacificationOrders.Count);
		for (int i = 0; i < this.suspendedPacificationOrders.Count; i++)
		{
			writer.WriteElementString<StaticString>("PacificationOrder", this.suspendedPacificationOrders[i]);
		}
		writer.WriteEndElement();
		writer.WriteStartElement("QuestBTVillages");
		writer.WriteAttributeString<int>("Count", this.questvillagesToPrioritize.Count);
		for (int j = 0; j < this.questvillagesToPrioritize.Count; j++)
		{
			IXmlSerializable xmlSerializable = this.questvillagesToPrioritize[j];
			writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
		}
		writer.WriteEndElement();
	}

	public override void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		base.ReadXml(reader);
		if (num > 0 && reader.IsStartElement("suspendedPacificationOrders"))
		{
			int attribute = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("suspendedPacificationOrders");
			this.suspendedPacificationOrders.Clear();
			for (int i = 0; i < attribute; i++)
			{
				StaticString staticString = reader.ReadElementString("PacificationOrder");
				this.suspendedPacificationOrders.Add(staticString);
				if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
				{
					Diagnostics.Log("ELCP {0}: Loading suspendedPacificationOrder {1}", new object[]
					{
						base.AIEntity.Empire,
						staticString
					});
				}
			}
			reader.ReadEndElement("suspendedPacificationOrders");
		}
		if (num > 1)
		{
			int attribute2 = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("QuestBTVillages");
			this.questvillagesToPrioritize.Clear();
			for (int j = 0; j < attribute2; j++)
			{
				AILayer_Village.QuestBTVillage questBTVillage = new AILayer_Village.QuestBTVillage("", GameEntityGUID.Zero);
				reader.ReadElementSerializable<AILayer_Village.QuestBTVillage>(ref questBTVillage);
				if (questBTVillage.IsValid())
				{
					if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
					{
						Diagnostics.Log("ELCP {0}: Loading QuestBTVillage {1}", new object[]
						{
							base.AIEntity.Empire,
							questBTVillage.ToString()
						});
					}
					this.questvillagesToPrioritize.Add(questBTVillage);
				}
			}
			reader.ReadEndElement("QuestBTVillages");
		}
	}

	private void RefreshObjectives_EvaluateSailNeed(bool cultist, bool inwar)
	{
		DepartmentOfScience.ConstructibleElement constructibleElement;
		if (this.workingList.Count < 2 && cultist && base.AIEntity.Empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitCultists7) && this.departmentOfScience.TechnologyDatabase.TryGetValue("TechnologyDefinitionShip", out constructibleElement) && this.departmentOfScience.GetTechnologyState(constructibleElement) == DepartmentOfScience.ConstructibleElement.State.Available)
		{
			bool flag = false;
			if (inwar && this.departmentOfTheInterior.Cities.Count > 0)
			{
				for (int i = 0; i < this.departmentOfTheInterior.Cities[0].Region.Borders.Length; i++)
				{
					if (this.worldPositionningService.GetRegion(this.departmentOfTheInterior.Cities[0].Region.Borders[i].NeighbourRegionIndex).IsOcean)
					{
						flag = true;
						break;
					}
				}
			}
			if (this.ChooseOverseaRegion() != null || flag)
			{
				if (!DepartmentOfScience.CanBuyoutResearch(base.AIEntity.Empire))
				{
					OrderQueueResearch orderQueueResearch = new OrderQueueResearch(base.AIEntity.Empire.Index, constructibleElement);
					orderQueueResearch.InsertAtFirstPlace = true;
					base.AIEntity.Empire.PlayerControllers.AI.PostOrder(orderQueueResearch);
					return;
				}
				float num = -this.departmentOfScience.GetBuyOutTechnologyCost(constructibleElement) * 1.2f;
				if (base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>().IsTransferOfResourcePossible(base.AIEntity.Empire, DepartmentOfTheTreasury.Resources.TechnologiesBuyOut, ref num))
				{
					OrderBuyOutTechnology order = new OrderBuyOutTechnology(base.AIEntity.Empire.Index, "TechnologyDefinitionShip");
					Ticket ticket;
					base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, null);
				}
			}
		}
	}

	public bool SuspendPacification
	{
		get
		{
			return this.suspendedPacificationOrders.Count > 0;
		}
	}

	public void AddQuestVillageToPrioritize(StaticString questname, GameEntityGUID targetvillage)
	{
		this.questvillagesToPrioritize.Add(new AILayer_Village.QuestBTVillage(questname, targetvillage));
	}

	public void RemoveQuestVillageToPrioritize(StaticString questname, GameEntityGUID targetvillage)
	{
		this.questvillagesToPrioritize.RemoveAll((AILayer_Village.QuestBTVillage qbtv) => qbtv.questName == questname && qbtv.objectiveGUID == targetvillage);
	}

	public int ConversionArmiesCount
	{
		get
		{
			return this.conversionArmies.Count;
		}
	}

	public static readonly StaticString TagConversionTrait = new StaticString("FactionTraitCultists14");

	public static readonly float DeboostRatio;

	public static string RegistryPath;

	private List<Army> conversionArmies;

	private List<GlobalObjectiveMessage> conversionObjectives;

	private DepartmentOfDefense departmentOfDefense;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private IIntelligenceAIHelper intelligenceAIHelper;

	private IPersonalityAIHelper personalityAIHelper;

	private List<Region> regions;

	private float turnLimitBeforeHardPacification;

	private AILayer_Colonization colonizationLayer;

	private List<GlobalObjectiveMessage> workingList;

	private List<float> workingListConversionCost;

	private List<float> workingListDistance;

	private IWorldPositionningService worldPositionningService;

	private DepartmentOfCreepingNodes departmentOfCreepingNodes;

	public static StaticString RageWizardAffinity = new StaticString("AffinityRageWizards");

	private IQuestManagementService questManagementService;

	private List<StaticString> suspendedPacificationOrders;

	public static StaticString ArmyActionParley;

	private DepartmentOfScience departmentOfScience;

	private ReadOnlyCollection<Quest> ActiveQuests;

	private DepartmentOfInternalAffairs departmentOfInternalAffairs;

	private List<AILayer_Village.QuestBTVillage> questvillagesToPrioritize;

	public class QuestBTVillage : IXmlSerializable
	{
		public void WriteXml(XmlWriter writer)
		{
			writer.WriteVersionAttribute(2);
			writer.WriteElementString<StaticString>("questName", this.questName);
			writer.WriteElementString<ulong>("objectiveGUID", this.objectiveGUID);
		}

		public void ReadXml(XmlReader reader)
		{
			if (reader.ReadVersionAttribute() > 1)
			{
				reader.ReadStartElement("QuestBTVillage");
				this.questName = reader.ReadElementString("questName");
				this.objectiveGUID = reader.ReadElementString<ulong>("objectiveGUID");
				reader.ReadEndElement("QuestBTVillage");
			}
		}

		public override string ToString()
		{
			string arg = (this.questName != null) ? this.questName.ToString() : "null";
			return string.Format("{0}:{1}", arg, this.objectiveGUID);
		}

		public bool IsValid()
		{
			return this.questName != "" && this.objectiveGUID != GameEntityGUID.Zero;
		}

		public QuestBTVillage(StaticString questname, GameEntityGUID targetGUID)
		{
			this.questName = questname;
			this.objectiveGUID = targetGUID;
		}

		public StaticString questName;

		public GameEntityGUID objectiveGUID;
	}
}
