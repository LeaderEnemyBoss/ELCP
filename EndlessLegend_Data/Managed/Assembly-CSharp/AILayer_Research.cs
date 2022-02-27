using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Amplitude;
using Amplitude.Collections;
using Amplitude.Unity.AI;
using Amplitude.Unity.AI.Amas;
using Amplitude.Unity.AI.Decision;
using Amplitude.Unity.AI.Evaluation;
using Amplitude.Unity.AI.Evaluation.Diagnostics;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation.Advanced;
using UnityEngine;

[PersonalityRegistryPath("AI/MajorEmpire/AIEntity_Empire/AILayer_Research", new object[]
{

})]
public class AILayer_Research : AILayer
{
	// Note: this type is marked as 'beforefieldinit'.
	static AILayer_Research()
	{
		AILayer_Research.NonFriendlyBordersPercent = "NonFriendlyBordersPercent";
		AILayer_Research.FractionOfNeutralRegionsOnOtherContinents = "FractionOfNeutralRegionsOnOtherContinents";
		AILayer_Research.FractionOfColonizedRegionsOnOtherContinents = "FractionOfColonizedRegionsOnOtherContinents";
		AILayer_Research.RollingDust = "RollingDust";
		AILayer_Research.BribeCost = "BribeCost";
		AILayer_Research.VillageMilitaryPower = "VillageMilitaryPower";
		AILayer_Research.ArmyMilitaryPower = "ArmyMilitaryPower";
		AILayer_Research.NumberOfPOIExploitedByANonFriendlyEmpire = "NumberOfPOIExploitedByANonFriendlyEmpire";
		AILayer_Research.POIAverageDefense = "POIAverageDefense";
		AILayer_Research.CityAverageSecurity = "CityAverageSecurity";
		AILayer_Research.StrategicMarketValue = "StrategicMarketValue";
		AILayer_Research.LuxuryMarketValue = "LuxuryMarketValue";
		AILayer_Research.NumberOfAvailableMinorFactionVillages = "NumberOfAvailableMinorFactionVillages";
		AILayer_Research.DiplomacyPositiveFactor = "DiplomacyPositiveFactor";
		AILayer_Research.TotalResourcesMarketValue = "TotalResourcesMarketValue";
	}

	private void UpdateResearchAIData()
	{
		if (!this.CanEvaluateResearches())
		{
			return;
		}
		global::Empire empire = base.AIEntity.Empire;
		this.researchAIData.RegisterValue(SimulationProperties.NetEmpireApproval, empire.GetPropertyValue(SimulationProperties.NetEmpireApproval));
		this.researchAIData.RegisterValue(SimulationProperties.NetCityGrowth, this.GetCityPropertySumValue(SimulationProperties.NetCityGrowth));
		this.researchAIData.RegisterValue(SimulationProperties.NetCityProduction, this.GetCityPropertySumValue(SimulationProperties.NetCityProduction));
		this.researchAIData.RegisterValue(SimulationProperties.NetEmpireResearch, empire.GetPropertyValue(SimulationProperties.NetEmpireResearch));
		this.researchAIData.RegisterValue(SimulationProperties.NetEmpireMoney, empire.GetPropertyValue(SimulationProperties.NetEmpireMoney));
		this.researchAIData.RegisterValue(SimulationProperties.NetEmpirePoint, empire.GetPropertyValue(SimulationProperties.NetEmpirePoint));
		this.researchAIData.RegisterValue(SimulationProperties.BankAccount, empire.GetPropertyValue(SimulationProperties.BankAccount));
		this.researchAIData.RegisterValue(SimulationProperties.Population, this.GetCityPropertySumValue(SimulationProperties.Workers));
		this.researchAIData.RegisterValue(SimulationProperties.FoodPopulation, this.GetCityPropertySumValue(SimulationProperties.FoodPopulation));
		this.researchAIData.RegisterValue(SimulationProperties.IndustryPopulation, this.GetCityPropertySumValue(SimulationProperties.IndustryPopulation));
		this.researchAIData.RegisterValue(SimulationProperties.SciencePopulation, this.GetCityPropertySumValue(SimulationProperties.SciencePopulation));
		this.researchAIData.RegisterValue(SimulationProperties.DustPopulation, this.GetCityPropertySumValue(SimulationProperties.DustPopulation));
		this.researchAIData.RegisterValue(SimulationProperties.CityPointPopulation, this.GetCityPropertySumValue(SimulationProperties.CityPointPopulation));
		float value = empire.GetPropertyValue(SimulationProperties.BankAccount) + 8f * empire.GetPropertyValue(SimulationProperties.NetEmpireMoney) * empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier);
		this.researchAIData.RegisterValue(AILayer_Research.RollingDust, value);
		this.researchAIData.RegisterValue(SimulationProperties.NetCityAntiSpy, this.GetCityPropertySumValue(SimulationProperties.NetCityAntiSpy));
		this.researchAIData.RegisterValue(SimulationProperties.MilitaryPower, empire.GetPropertyValue(SimulationProperties.MilitaryPower));
		this.researchAIData.RegisterValue(SimulationProperties.MaximumCityDefensePoint, this.GetCityPropertySumValue(SimulationProperties.MaximumCityDefensePoint));
		this.AnalyseResources(empire);
		this.AnalyseEmpireAgencies(empire);
		this.AnalyseWorld(empire);
		this.AnalyseDiplomacy(empire);
	}

	private void AnalyseDiplomacy(global::Empire empire)
	{
		IGameService service = Services.GetService<IGameService>();
		global::Game game = service.Game as global::Game;
		Diagnostics.Assert(base.AIEntity.AIPlayer != null);
		AIEntity entity = base.AIEntity.AIPlayer.GetEntity<AIEntity_Amas>();
		Diagnostics.Assert(entity != null);
		AILayer_DiplomacyAmas layer = entity.GetLayer<AILayer_DiplomacyAmas>();
		float num = 0f;
		for (int i = 0; i < game.Empires.Length; i++)
		{
			MajorEmpire majorEmpire = game.Empires[i] as MajorEmpire;
			if (majorEmpire != null && majorEmpire.Index != empire.Index)
			{
				AgentGroup agentGroupForEmpire = layer.GetAgentGroupForEmpire(majorEmpire);
				Diagnostics.Assert(agentGroupForEmpire != null);
				float agentCriticityMaxIntensity = agentGroupForEmpire.GetAgentCriticityMaxIntensity(AILayer_DiplomacyAmas.AgentNames.AllianceTermAgent);
				float agentCriticityMaxIntensity2 = agentGroupForEmpire.GetAgentCriticityMaxIntensity(AILayer_DiplomacyAmas.AgentNames.PeaceTermAgent);
				float agentCriticityMaxIntensity3 = agentGroupForEmpire.GetAgentCriticityMaxIntensity(AILayer_DiplomacyAmas.AgentNames.WarTermAgent);
				float b = Mathf.Max(agentCriticityMaxIntensity, agentCriticityMaxIntensity2) - agentCriticityMaxIntensity3;
				num = Mathf.Max(num, b);
			}
		}
		this.researchAIData.RegisterValue(AILayer_Research.DiplomacyPositiveFactor, num);
	}

	private void AnalyseResources(global::Empire empire)
	{
		DepartmentOfTheTreasury agency = empire.GetAgency<DepartmentOfTheTreasury>();
		float num = 0f;
		int num2 = 0;
		float num3 = 0f;
		int num4 = 0;
		float num5 = 0f;
		IDatabase<ResourceDefinition> database = Databases.GetDatabase<ResourceDefinition>(false);
		foreach (ResourceDefinition resourceDefinition in database)
		{
			if (resourceDefinition.ResourceType == ResourceDefinition.Type.Strategic || resourceDefinition.ResourceType == ResourceDefinition.Type.Luxury)
			{
				float num6;
				agency.TryGetResourceStockValue(empire.SimulationObject, resourceDefinition.Name, out num6, true);
				float priceWithSalesTaxes = TradableResource.GetPriceWithSalesTaxes(resourceDefinition.Name, TradableTransactionType.Sellout, empire, 1f);
				if (resourceDefinition.ResourceType == ResourceDefinition.Type.Strategic)
				{
					num += priceWithSalesTaxes;
					num2++;
				}
				else if (resourceDefinition.ResourceType == ResourceDefinition.Type.Luxury)
				{
					num3 += priceWithSalesTaxes;
					num4++;
				}
				num5 += num6 * priceWithSalesTaxes;
			}
		}
		this.researchAIData.RegisterValue(AILayer_Research.StrategicMarketValue, (num2 <= 0) ? 0f : (num / (float)num2));
		this.researchAIData.RegisterValue(AILayer_Research.LuxuryMarketValue, (num4 <= 0) ? 0f : (num3 / (float)num4));
		this.researchAIData.RegisterValue(AILayer_Research.TotalResourcesMarketValue, num5);
	}

	private void AnalyseEmpireAgencies(global::Empire empire)
	{
		DepartmentOfDefense agency = empire.GetAgency<DepartmentOfDefense>();
		float num = 0f;
		for (int i = 0; i < agency.Armies.Count; i++)
		{
			Army army = agency.Armies[i];
			num = Mathf.Max(num, army.GetPropertyValue(SimulationProperties.MilitaryPower));
		}
		this.researchAIData.RegisterValue(AILayer_Research.ArmyMilitaryPower, num);
		DepartmentOfTheInterior agency2 = empire.GetAgency<DepartmentOfTheInterior>();
		float num2 = 0f;
		float num3 = 0f;
		int num4 = 0;
		int count = agency2.Cities.Count;
		for (int j = 0; j < count; j++)
		{
			num3 += agency2.Cities[j].GetPropertyValue(SimulationProperties.NetCityAntiSpy);
			for (int k = 0; k < agency2.Cities[j].Region.PointOfInterests.Length; k++)
			{
				float propertyValue = agency2.Cities[j].Region.PointOfInterests[k].GetPropertyValue(SimulationProperties.MaximumPillageDefense);
				num2 += propertyValue;
				num4++;
			}
		}
		this.researchAIData.RegisterValue(AILayer_Research.POIAverageDefense, (num4 <= 0) ? float.NaN : (num2 / (float)num4));
		this.researchAIData.RegisterValue(AILayer_Research.CityAverageSecurity, (count <= 0) ? float.NaN : (num3 / (float)count));
	}

	private void AnalyseWorld(global::Empire empire)
	{
		IGameService service = Services.GetService<IGameService>();
		global::Game game = service.Game as global::Game;
		DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
		DepartmentOfTheInterior agency2 = empire.GetAgency<DepartmentOfTheInterior>();
		int num = (agency2.Cities.Count <= 0) ? -1 : agency2.Cities[0].Region.ContinentID;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		int num7 = 0;
		int num8 = 0;
		float num9 = 0f;
		float num10 = 0f;
		int num11 = 0;
		foreach (AIRegionData airegionData in this.worldAtlasHelper.GetRegionData(empire.Index))
		{
			Region region = this.worldPositionService.GetRegion(airegionData.RegionIndex);
			if (airegionData.IsColonizedByMe)
			{
				num2 += airegionData.BorderWithEnnemy;
				num3 += airegionData.BorderWithNeutral;
				num4 += airegionData.OverallBorderSize;
				for (int j = 0; j < region.PointOfInterests.Length; j++)
				{
					PointOfInterest pointOfInterest = region.PointOfInterests[j];
					if (pointOfInterest.Type == "Village")
					{
						num10 = Mathf.Max(num10, pointOfInterest.GetPropertyValue(SimulationProperties.BribeCost));
						num9 = Mathf.Max(num9, pointOfInterest.GetPropertyValue(SimulationProperties.MilitaryPower));
					}
				}
				for (int k = 0; k < region.Borders.Length; k++)
				{
					Region region2 = this.worldPositionService.GetRegion(region.Borders[k].NeighbourRegionIndex);
					for (int l = 0; l < region2.PointOfInterests.Length; l++)
					{
						PointOfInterest pointOfInterest2 = region2.PointOfInterests[l];
						if (pointOfInterest2.Type == "Village")
						{
							num10 = Mathf.Max(num10, pointOfInterest2.GetPropertyValue(SimulationProperties.BribeCost));
							num9 = Mathf.Max(num9, pointOfInterest2.GetPropertyValue(SimulationProperties.MilitaryPower));
						}
						if (pointOfInterest2.Empire != null && pointOfInterest2.Empire.Index != empire.Index && DepartmentOfDefense.IsPointOfInterestSuitableForPillage(pointOfInterest2))
						{
							DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(pointOfInterest2.Empire);
							if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.War || diplomaticRelation.State.Name == DiplomaticRelationState.Names.ColdWar)
							{
								num11++;
							}
						}
					}
				}
			}
			if (region.IsLand && !region.IsRegionColonized())
			{
				num5++;
				if (region.ContinentID != num)
				{
					num6++;
				}
			}
			if (region.IsLand && region.IsRegionColonized() && !airegionData.IsColonizedByMe)
			{
				num7++;
				if (region.ContinentID != num)
				{
					num8++;
				}
			}
		}
		int num12 = 0;
		for (int m = 0; m < game.Empires.Length; m++)
		{
			MinorEmpire minorEmpire = game.Empires[m] as MinorEmpire;
			if (minorEmpire != null)
			{
				int num13 = 0;
				BarbarianCouncil agency3 = minorEmpire.GetAgency<BarbarianCouncil>();
				for (int n = 0; n < agency3.Villages.Count; n++)
				{
					Village village = agency3.Villages[n];
					if (village.Region.IsRegionColonized() && village.Region.Owner.Index == empire.Index)
					{
						if (!agency2.AssimilatedFactions.Contains(minorEmpire.MinorFaction))
						{
							if (village.HasBeenPacified)
							{
								num13++;
							}
						}
					}
				}
				num12 = Mathf.Max(num12, num13);
			}
		}
		float num14 = (num4 <= 0) ? 0f : ((float)(num2 / num4));
		float num15 = (num4 <= 0) ? 0f : ((float)(num3 / num4));
		float value = (num5 <= 0) ? 0f : ((float)num6 / (float)num5);
		float value2 = (num7 <= 0) ? 0f : ((float)num8 / (float)num7);
		this.researchAIData.RegisterValue(AILayer_Research.NonFriendlyBordersPercent, num14 + num15);
		this.researchAIData.RegisterValue(AILayer_Research.FractionOfNeutralRegionsOnOtherContinents, value);
		this.researchAIData.RegisterValue(AILayer_Research.FractionOfColonizedRegionsOnOtherContinents, value2);
		this.researchAIData.RegisterValue(AILayer_Research.BribeCost, num10);
		this.researchAIData.RegisterValue(AILayer_Research.VillageMilitaryPower, num9);
		this.researchAIData.RegisterValue(AILayer_Research.NumberOfPOIExploitedByANonFriendlyEmpire, (float)num11);
		this.researchAIData.RegisterValue(AILayer_Research.NumberOfAvailableMinorFactionVillages, (float)num12);
	}

	private float GetCityPropertySumValue(StaticString resourcePropertyName)
	{
		DepartmentOfTheInterior agency = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		float num = 0f;
		for (int i = 0; i < agency.Cities.Count; i++)
		{
			num += agency.Cities[i].GetPropertyValue(resourcePropertyName);
		}
		return num;
	}

	private void BuyOutResearch_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		OrderBuyOutTechnology orderBuyOutTechnology = e.Order as OrderBuyOutTechnology;
		Diagnostics.Assert(orderBuyOutTechnology != null);
		List<EvaluableMessage_ResearchBuyout> list = new List<EvaluableMessage_ResearchBuyout>(base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_ResearchBuyout>(BlackboardLayerID.Empire, (EvaluableMessage_ResearchBuyout message) => message.TechnologyReference == orderBuyOutTechnology.TechnologyName));
		if (list.Count == 0)
		{
			return;
		}
		if (list.Count > 1)
		{
			AILayer.LogWarning("There should not be several PopulationBuyout EvaluableMessages for the same city");
		}
		EvaluableMessage_ResearchBuyout evaluableMessage_ResearchBuyout = list[0];
		if (e.Result == PostOrderResponse.Processed)
		{
			evaluableMessage_ResearchBuyout.SetObtained();
		}
		else
		{
			evaluableMessage_ResearchBuyout.SetFailedToObtain();
		}
	}

	private EvaluableMessage_ResearchBuyout CreateEvaluableMessageFromTechnology(TechnologyDefinition technology)
	{
		float num;
		float num2;
		this.EvaluateTechnologyOrientation(technology, out num, out num2);
		if (num2 <= 1.401298E-45f || num > 1.401298E-45f)
		{
		}
		if (num2 > num)
		{
			return new EvaluableMessage_ResearchBuyout(technology.Name, 1, AILayer_AccountManager.MilitaryAccountName);
		}
		return new EvaluableMessage_ResearchBuyout(technology.Name, 1, AILayer_AccountManager.EconomyAccountName);
	}

	private void GenerateResearchBuyoutMessage()
	{
		if (!DepartmentOfScience.CanBuyoutResearch(base.AIEntity.Empire))
		{
			return;
		}
		DepartmentOfScience.ConstructibleElement constructibleElement = this.GetMostWantedTechnologyDecision().Element as DepartmentOfScience.ConstructibleElement;
		if (constructibleElement == null)
		{
			return;
		}
		DepartmentOfScience agency = base.AIEntity.Empire.GetAgency<DepartmentOfScience>();
		List<EvaluableMessage_ResearchBuyout> list = new List<EvaluableMessage_ResearchBuyout>(base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_ResearchBuyout>(BlackboardLayerID.Empire, (EvaluableMessage_ResearchBuyout message) => message.EvaluationState != EvaluableMessage.EvaluableMessageState.Obtained && message.EvaluationState != EvaluableMessage.EvaluableMessageState.Cancel));
		EvaluableMessage_ResearchBuyout evaluableMessage_ResearchBuyout;
		if (list.Count == 0)
		{
			evaluableMessage_ResearchBuyout = this.CreateEvaluableMessageFromTechnology(constructibleElement as TechnologyDefinition);
			base.AIEntity.AIPlayer.Blackboard.AddMessage(evaluableMessage_ResearchBuyout);
		}
		else
		{
			evaluableMessage_ResearchBuyout = list[0];
			if (constructibleElement.Name != evaluableMessage_ResearchBuyout.TechnologyReference)
			{
				Diagnostics.Log("AI don't want to research technology {0} anymore. AI now wants technology {1}.", new object[]
				{
					evaluableMessage_ResearchBuyout.TechnologyReference,
					constructibleElement.Name
				});
				evaluableMessage_ResearchBuyout.Cancel();
				evaluableMessage_ResearchBuyout = this.CreateEvaluableMessageFromTechnology(constructibleElement as TechnologyDefinition);
				base.AIEntity.AIPlayer.Blackboard.AddMessage(evaluableMessage_ResearchBuyout);
			}
		}
		float cost = 0f;
		IDatabase<DepartmentOfScience.ConstructibleElement> database = Databases.GetDatabase<DepartmentOfScience.ConstructibleElement>(false);
		DepartmentOfScience.ConstructibleElement technology;
		if (database.TryGetValue(evaluableMessage_ResearchBuyout.TechnologyReference, out technology))
		{
			cost = agency.GetBuyOutTechnologyCost(technology);
		}
		int maxValue = int.MaxValue;
		float num = 5f * base.AIEntity.Empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier);
		IGameService service = Services.GetService<IGameService>();
		float num2 = (float)(service.Game as global::Game).Turn;
		float researchPropertyValue = agency.GetResearchPropertyValue("UnlockedTechnologyCount");
		float num3 = researchPropertyValue * num;
		float num4 = Mathf.Clamp((num2 - num3) / num, -1f, 1f);
		num4 = (num4 + 1f) / 2f;
		float num5 = 0.25f + num4 * 0.75f;
		float globalMotivation = 0.8f;
		if (num5 > 0.9f)
		{
			globalMotivation = 0.9f;
		}
		evaluableMessage_ResearchBuyout.Refresh(globalMotivation, num5, cost, maxValue);
	}

	private bool CanEvaluateKaijuResearches()
	{
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		return service != null && service.IsShared(DownloadableContent20.ReadOnlyName) && (!(base.AIEntity.Empire is MajorEmpire) || (base.AIEntity.Empire as MajorEmpire).TamedKaijus.Count != 0);
	}

	private void EvaluateKaijuUnlocks()
	{
		DepartmentOfScience agency = base.AIEntity.Empire.GetAgency<DepartmentOfScience>();
		DepartmentOfTheTreasury agency2 = base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>();
		if (!this.CanEvaluateKaijuResearches())
		{
			return;
		}
		List<ConstructibleElement> list = new List<ConstructibleElement>();
		for (int i = 0; i < this.kaijuUnlockDefinitions.Length; i++)
		{
			TechnologyDefinition technologyDefinition = this.kaijuUnlockDefinitions[i];
			DepartmentOfScience.ConstructibleElement.State technologyState = this.kaijuTechsService.GetTechnologyState(technologyDefinition, base.AIEntity.Empire);
			if (technologyState != DepartmentOfScience.ConstructibleElement.State.Researched && technologyState != DepartmentOfScience.ConstructibleElement.State.NotAvailable)
			{
				if (DepartmentOfTheTreasury.CheckConstructiblePrerequisites(base.AIEntity.Empire, technologyDefinition, new string[]
				{
					ConstructionFlags.Prerequisite
				}))
				{
					if (agency2.CanAfford(technologyDefinition.Costs))
					{
						list.Add(technologyDefinition);
					}
				}
			}
		}
		Diagnostics.Assert(this.decisionMaker != null);
		this.kaijuUnlockDecisions.Clear();
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			IGameService service = Services.GetService<IGameService>();
			global::Game game = service.Game as global::Game;
			EvaluationData<ConstructibleElement, InterpreterContext> evaluationData = new EvaluationData<ConstructibleElement, InterpreterContext>();
			this.decisionMaker.Evaluate(list, ref this.kaijuUnlockDecisions, evaluationData);
			evaluationData.Turn = game.Turn;
			this.KaijuUnlockDecisionMakerEvaluationDataHistoric.Add(evaluationData);
		}
		else
		{
			this.decisionMaker.Evaluate(list, ref this.kaijuUnlockDecisions, null);
		}
	}

	private void GenerateKaijuUnlockBuyoutMessage()
	{
		if (!this.CanEvaluateKaijuResearches())
		{
			return;
		}
		DepartmentOfScience.ConstructibleElement constructibleElement = this.GetMostWantedKaijuUnlockDecision().Element as DepartmentOfScience.ConstructibleElement;
		if (constructibleElement == null)
		{
			return;
		}
		List<KaijuUnlockBuyoutMessage> list = new List<KaijuUnlockBuyoutMessage>(base.AIEntity.AIPlayer.Blackboard.GetMessages<KaijuUnlockBuyoutMessage>(BlackboardLayerID.Empire, (KaijuUnlockBuyoutMessage message) => message.State == BlackboardMessage.StateValue.Message_InProgress));
		if (list.Count == 0)
		{
			this.PostNewKaijuUnlockBuyoutMessage(constructibleElement.Name);
		}
		else
		{
			if (list.Count > 1)
			{
				AILayer.LogWarning("There should not be several KaijuUnlockBuyout in progress messages in the same empire ({0})", new object[]
				{
					base.AIEntity.Empire.Index
				});
			}
			KaijuUnlockBuyoutMessage kaijuUnlockBuyoutMessage = null;
			for (int i = 0; i < list.Count; i++)
			{
				if (kaijuUnlockBuyoutMessage == null && list[i].TechnologyReference.Equals(constructibleElement.Name))
				{
					kaijuUnlockBuyoutMessage = list[i];
				}
				else
				{
					base.AIEntity.AIPlayer.Blackboard.CancelMessage(list[i]);
					kaijuUnlockBuyoutMessage.TimeOut = 0;
				}
			}
			if (kaijuUnlockBuyoutMessage != null)
			{
				kaijuUnlockBuyoutMessage.TimeOut = 1;
				kaijuUnlockBuyoutMessage.State = BlackboardMessage.StateValue.Message_InProgress;
			}
			else
			{
				this.PostNewKaijuUnlockBuyoutMessage(constructibleElement.Name);
			}
		}
	}

	private void OrderBuyOutKaijuTechnology_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		OrderBuyOutKaijuTechnology orderBuyOutKaijuTechnology = e.Order as OrderBuyOutKaijuTechnology;
		Diagnostics.Assert(orderBuyOutKaijuTechnology != null);
		List<KaijuUnlockBuyoutMessage> list = new List<KaijuUnlockBuyoutMessage>(base.AIEntity.AIPlayer.Blackboard.GetMessages<KaijuUnlockBuyoutMessage>(BlackboardLayerID.Empire, (KaijuUnlockBuyoutMessage message) => message.TechnologyReference == orderBuyOutKaijuTechnology.TechnologyName));
		if (list.Count == 0)
		{
			return;
		}
		if (list.Count > 1)
		{
			AILayer.LogWarning("There should not be several KaijuUnlockBuyout messages for the same unlock in the same empire ({0}).", new object[]
			{
				base.AIEntity.Empire.Index
			});
		}
		if (e.Result == PostOrderResponse.Processed)
		{
			for (int i = 0; i < list.Count; i++)
			{
				KaijuUnlockBuyoutMessage kaijuUnlockBuyoutMessage = list[i];
				kaijuUnlockBuyoutMessage.TimeOut = 0;
				if (i == 0)
				{
					kaijuUnlockBuyoutMessage.State = BlackboardMessage.StateValue.Message_Success;
				}
				else
				{
					kaijuUnlockBuyoutMessage.State = BlackboardMessage.StateValue.Message_Canceled;
				}
			}
		}
		else
		{
			for (int j = list.Count - 1; j >= 0; j--)
			{
				KaijuUnlockBuyoutMessage kaijuUnlockBuyoutMessage2 = list[j];
				kaijuUnlockBuyoutMessage2.State = BlackboardMessage.StateValue.Message_Failed;
				kaijuUnlockBuyoutMessage2.TimeOut = 0;
			}
		}
	}

	private KaijuUnlockBuyoutMessage PostNewKaijuUnlockBuyoutMessage(StaticString unlockName)
	{
		if (!this.CanEvaluateKaijuResearches())
		{
			return null;
		}
		if (StaticString.IsNullOrEmpty(unlockName))
		{
			return null;
		}
		KaijuUnlockBuyoutMessage kaijuUnlockBuyoutMessage = new KaijuUnlockBuyoutMessage(unlockName, 1);
		kaijuUnlockBuyoutMessage.State = BlackboardMessage.StateValue.Message_InProgress;
		base.AIEntity.AIPlayer.Blackboard.AddMessage(kaijuUnlockBuyoutMessage);
		return kaijuUnlockBuyoutMessage;
	}

	private SynchronousJobState SynchronousJob_BuyoutKaijuUnlock()
	{
		if (!this.CanEvaluateKaijuResearches())
		{
			return SynchronousJobState.Failure;
		}
		List<KaijuUnlockBuyoutMessage> list = new List<KaijuUnlockBuyoutMessage>(base.AIEntity.AIPlayer.Blackboard.GetMessages<KaijuUnlockBuyoutMessage>(BlackboardLayerID.Empire, (KaijuUnlockBuyoutMessage match) => match.State == BlackboardMessage.StateValue.Message_InProgress));
		if (list.Count != 0)
		{
			if (list.Count > 1)
			{
				AILayer.LogWarning("There should not be several KaijuUnlockBuyout in progress messages in the same empire ({0})", new object[]
				{
					base.AIEntity.Empire.Index
				});
			}
			KaijuUnlockBuyoutMessage kaijuUnlockBuyoutMessage = list[0];
			OrderBuyOutKaijuTechnology order = new OrderBuyOutKaijuTechnology(base.AIEntity.Empire.Index, kaijuUnlockBuyoutMessage.TechnologyReference);
			Ticket ticket;
			base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderBuyOutKaijuTechnology_TicketRaised));
			return SynchronousJobState.Success;
		}
		return SynchronousJobState.Success;
	}

	private void EvaluateOrbUnlocks()
	{
		DepartmentOfScience agency = base.AIEntity.Empire.GetAgency<DepartmentOfScience>();
		if (!this.CanEvaluateOrbResearches())
		{
			return;
		}
		List<ConstructibleElement> list = new List<ConstructibleElement>();
		for (int i = 0; i < this.orbUnlockDefinitions.Length; i++)
		{
			TechnologyDefinition technologyDefinition = this.orbUnlockDefinitions[i];
			if (DepartmentOfTheTreasury.CheckConstructiblePrerequisites(base.AIEntity.Empire, technologyDefinition, new string[]
			{
				ConstructionFlags.Prerequisite
			}))
			{
				DepartmentOfScience.ConstructibleElement.State technologyState = agency.GetTechnologyState(technologyDefinition);
				if (technologyState != DepartmentOfScience.ConstructibleElement.State.Researched && technologyState != DepartmentOfScience.ConstructibleElement.State.NotAvailable)
				{
					list.Add(technologyDefinition);
				}
			}
		}
		Diagnostics.Assert(this.decisionMaker != null);
		this.orbUnlockDecisions.Clear();
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			IGameService service = Services.GetService<IGameService>();
			global::Game game = service.Game as global::Game;
			EvaluationData<ConstructibleElement, InterpreterContext> evaluationData = new EvaluationData<ConstructibleElement, InterpreterContext>();
			this.decisionMaker.Evaluate(list, ref this.orbUnlockDecisions, evaluationData);
			evaluationData.Turn = game.Turn;
			this.OrbUnlockDecisionMakerEvaluationDataHistoric.Add(evaluationData);
		}
		else
		{
			this.decisionMaker.Evaluate(list, ref this.orbUnlockDecisions, null);
		}
	}

	private bool CanEvaluateOrbResearches()
	{
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		if (service == null || !service.IsShared(DownloadableContent13.ReadOnlyName))
		{
			return false;
		}
		DepartmentOfTheInterior agency = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		return agency.IsAltarBuilt();
	}

	private void BuyOutOrbUnlock_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		OrderBuyOutTechnology orderBuyOutTechnology = e.Order as OrderBuyOutTechnology;
		Diagnostics.Assert(orderBuyOutTechnology != null);
		List<EvaluableMessage_OrbUnlockBuyout> list = new List<EvaluableMessage_OrbUnlockBuyout>(base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_OrbUnlockBuyout>(BlackboardLayerID.Empire, (EvaluableMessage_OrbUnlockBuyout message) => message.TechnologyReference == orderBuyOutTechnology.TechnologyName));
		if (list.Count == 0)
		{
			return;
		}
		if (list.Count > 1)
		{
			AILayer.LogWarning("There should not be several OrbUnlockBuyout EvaluableMessages for the same city");
		}
		EvaluableMessage_OrbUnlockBuyout evaluableMessage_OrbUnlockBuyout = list[0];
		if (e.Result == PostOrderResponse.Processed)
		{
			evaluableMessage_OrbUnlockBuyout.SetObtained();
		}
		else
		{
			evaluableMessage_OrbUnlockBuyout.SetFailedToObtain();
		}
	}

	private void GenerateOrbUnlockBuyoutMessage()
	{
		if (!this.CanEvaluateOrbResearches())
		{
			return;
		}
		DecisionResult mostWantedOrbUnlockDecision = this.GetMostWantedOrbUnlockDecision();
		DepartmentOfScience.ConstructibleElement constructibleElement = mostWantedOrbUnlockDecision.Element as DepartmentOfScience.ConstructibleElement;
		if (constructibleElement == null)
		{
			return;
		}
		List<EvaluableMessage_OrbUnlockBuyout> list = new List<EvaluableMessage_OrbUnlockBuyout>(base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_OrbUnlockBuyout>(BlackboardLayerID.Empire, (EvaluableMessage_OrbUnlockBuyout message) => message.EvaluationState != EvaluableMessage.EvaluableMessageState.Obtained && message.EvaluationState != EvaluableMessage.EvaluableMessageState.Cancel));
		EvaluableMessage_OrbUnlockBuyout evaluableMessage_OrbUnlockBuyout;
		if (list.Count == 0)
		{
			evaluableMessage_OrbUnlockBuyout = new EvaluableMessage_OrbUnlockBuyout(constructibleElement.Name, 1, AILayer_AccountManager.OrbAccountName);
			base.AIEntity.AIPlayer.Blackboard.AddMessage(evaluableMessage_OrbUnlockBuyout);
		}
		else
		{
			evaluableMessage_OrbUnlockBuyout = list[0];
			if (constructibleElement.Name != evaluableMessage_OrbUnlockBuyout.TechnologyReference)
			{
				Diagnostics.Log("AI don't want to unlock orb technology {0} anymore. AI now wants orb unlock {1}.", new object[]
				{
					evaluableMessage_OrbUnlockBuyout.TechnologyReference,
					constructibleElement.Name
				});
				evaluableMessage_OrbUnlockBuyout.Cancel();
				evaluableMessage_OrbUnlockBuyout = new EvaluableMessage_OrbUnlockBuyout(constructibleElement.Name, 1, AILayer_AccountManager.OrbAccountName);
				base.AIEntity.AIPlayer.Blackboard.AddMessage(evaluableMessage_OrbUnlockBuyout);
			}
		}
		float num = 0f;
		IDatabase<DepartmentOfScience.ConstructibleElement> database = Databases.GetDatabase<DepartmentOfScience.ConstructibleElement>(false);
		DepartmentOfScience.ConstructibleElement constructibleElement2;
		if (database.TryGetValue(evaluableMessage_OrbUnlockBuyout.TechnologyReference, out constructibleElement2))
		{
			for (int i = 0; i < constructibleElement2.Costs.Length; i++)
			{
				if (constructibleElement2.Costs[i] != null)
				{
					if (constructibleElement2.Costs[i].ResourceName != DepartmentOfTheTreasury.Resources.Orb)
					{
						Diagnostics.LogWarning("[AI] Can't handle the cost in resource {0}", new object[]
						{
							constructibleElement2.Costs[i].ResourceName
						});
					}
					else
					{
						num += constructibleElement2.Costs[i].GetValue(base.AIEntity.Empire.SimulationObject);
					}
				}
			}
		}
		int maxValue = int.MaxValue;
		float num2 = mostWantedOrbUnlockDecision.Score;
		num2 = Mathf.Clamp01(num2 / this.maximumUnlockScore);
		evaluableMessage_OrbUnlockBuyout.Refresh(0.1f, num2, num, maxValue);
	}

	private SynchronousJobState SynchronousJob_BuyoutOrbUnlock()
	{
		if (!this.CanEvaluateOrbResearches())
		{
			return SynchronousJobState.Failure;
		}
		List<EvaluableMessage_OrbUnlockBuyout> list = new List<EvaluableMessage_OrbUnlockBuyout>(base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_OrbUnlockBuyout>(BlackboardLayerID.Empire, (EvaluableMessage_OrbUnlockBuyout match) => match.State == BlackboardMessage.StateValue.Message_InProgress));
		if (list.Count != 0)
		{
			if (list.Count > 1)
			{
				AILayer.LogWarning("There should not be several ResearchBuyout EvaluableMessages for the same empire ({0})", new object[]
				{
					base.AIEntity.Empire.Index
				});
			}
			EvaluableMessage_OrbUnlockBuyout evaluableMessage_OrbUnlockBuyout = list[0];
			if (evaluableMessage_OrbUnlockBuyout.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate)
			{
				OrderBuyOutTechnology order = new OrderBuyOutTechnology(base.AIEntity.Empire.Index, evaluableMessage_OrbUnlockBuyout.TechnologyReference);
				Ticket ticket;
				base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.BuyOutOrbUnlock_TicketRaised));
				return SynchronousJobState.Success;
			}
		}
		return SynchronousJobState.Success;
	}

	private void EvaluateTechnologyOrientation(TechnologyDefinition technologyDefinition, out float economicScore, out float militaryScore)
	{
		AmasEmpireDataMessage amasEmpireDataMessage = base.AIEntity.AIPlayer.Blackboard.GetMessages<AmasEmpireDataMessage>(BlackboardLayerID.Empire).FirstOrDefault<AmasEmpireDataMessage>();
		this.RegisterInterpreterContextData(amasEmpireDataMessage, this.orientationDecisionMaker.Context);
		this.orientationDecisionMaker.UnregisterAllOutput();
		this.orientationDecisionMaker.RegisterOutput("AIEmpireGrowth");
		this.orientationDecisionMaker.RegisterOutput("AIEmpireProduction");
		this.orientationDecisionMaker.RegisterOutput("AIEmpireEmpirePoint");
		this.orientationDecisionMaker.RegisterOutput("AIEmpireMoney");
		this.orientationDecisionMaker.RegisterOutput("AIEmpireResearch");
		this.orientationDecisionMaker.RegisterOutput("AIEmpireStrategicResource");
		this.orientationDecisionMaker.RegisterOutput("AIEmpireLuxuryResource");
		this.orientationDecisionMaker.RegisterOutput("AIEmpireUnlockShip");
		this.orientationDecisionMaker.RegisterOutput("AIEmpireUnlockAssimilation");
		this.orientationDecisionMaker.RegisterOutput("AIEmpireUnlockBribe");
		this.orientationDecisionMaker.RegisterOutput("AIEmpireUnlockMarketResource");
		this.orientationDecisionMaker.RegisterOutput("AIEmpireUnlockBooster");
		this.orientationDecisionMaker.RegisterOutput("AIEmpireUnlockDiplomacyPositive");
		this.orientationDecisionMaker.RegisterOutput("AIEmpireImprovedRuinSearch");
		economicScore = this.orientationDecisionMaker.EvaluateDecision(technologyDefinition).Score;
		this.orientationDecisionMaker.UnregisterAllOutput();
		this.orientationDecisionMaker.RegisterOutput("AIEmpireProduction");
		this.orientationDecisionMaker.RegisterOutput("AIEmpireStrategicResource");
		this.orientationDecisionMaker.RegisterOutput("AIEmpireMilitaryPower");
		this.orientationDecisionMaker.RegisterOutput("AIEmpirePillageDefense");
		this.orientationDecisionMaker.RegisterOutput("AIEmpireAntiSpy");
		this.orientationDecisionMaker.RegisterOutput("AIEmpireCityDefense");
		this.orientationDecisionMaker.RegisterOutput("AIEmpireVision");
		this.orientationDecisionMaker.RegisterOutput("AIEmpireUnlockUnit");
		this.orientationDecisionMaker.RegisterOutput("AIEmpireUnlockSeafaringUnit");
		this.orientationDecisionMaker.RegisterOutput("AIEmpireUnlockPillage");
		this.orientationDecisionMaker.RegisterOutput("AIEmpireUnlockPrivateers");
		this.orientationDecisionMaker.RegisterOutput("AIEmpireUnlockMarketUnit");
		this.orientationDecisionMaker.RegisterOutput("AIEmpireUnlockBoosterCultist");
		this.orientationDecisionMaker.RegisterOutput("AIEmpireBuyout");
		militaryScore = this.orientationDecisionMaker.EvaluateDecision(technologyDefinition).Score;
	}

	private void InitializeOrientationDecisionMaker()
	{
		this.technologyEvaluationHelper = AIScheduler.Services.GetService<IConstructibleElementEvaluationAIHelper>();
		this.orientationDecisionMaker = new SimulationDecisionMaker<ConstructibleElement>(this.technologyEvaluationHelper, base.AIEntity.Empire);
	}

	public StaticString GetResearchContextGroupName()
	{
		return AILayer_Research.researchContextGroupName;
	}

	[UtilityFunction("AIEmpireApproval")]
	private static float UtilityFunc_EmpireApproval(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("EmpireAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float num = Mathf.Max(1f, aidata.GetAverageValue(SimulationProperties.NetEmpireApproval));
		float num2 = Mathf.Min(100f, num + aiParameterValue);
		float utility = num2 / num - 1f;
		return AILayer_Research.Normalize(debugContext, 0f, 0.5f, utility);
	}

	[UtilityFunction("AIEmpireGrowth")]
	private static float UtilityFunc_EmpireGrowth(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("EmpireAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float num = Mathf.Max(1f, aidata.GetAverageValue(SimulationProperties.NetCityGrowth));
		float utility = aiParameterValue / num;
		return AILayer_Research.Normalize(debugContext, 0f, 1.5f, utility);
	}

	[UtilityFunction("AIEmpireProduction")]
	private static float UtilityFunc_EmpireProduction(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("EmpireAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float num = Mathf.Max(1f, aidata.GetAverageValue(SimulationProperties.NetCityProduction));
		float utility = aiParameterValue / num;
		return AILayer_Research.Normalize(debugContext, 0f, 0.7f, utility);
	}

	[UtilityFunction("AIEmpireMoney")]
	private static float UtilityFunc_EmpireMoney(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("EmpireAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float num = Mathf.Max(1f, aidata.GetAverageValue(SimulationProperties.NetEmpireMoney));
		float utility = aiParameterValue / num;
		return AILayer_Research.Normalize(debugContext, 0f, 2f, utility);
	}

	[UtilityFunction("AIEmpireResearch")]
	private static float UtilityFunc_EmpireResearch(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		AIData aidata = context.Get("EmpireAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		if (empire.SimulationObject.Tags.Contains("AffinityReplicants"))
		{
			return 0f;
		}
		float num = Mathf.Max(1f, aidata.GetAverageValue(SimulationProperties.NetEmpireResearch));
		float utility = aiParameterValue / num;
		return AILayer_Research.Normalize(debugContext, 0f, 1.7f, utility);
	}

	[UtilityFunction("AIEmpireEmpirePoint")]
	private static float UtilityFunc_EmpirePoint(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("EmpireAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float num = Mathf.Max(1f, aidata.GetAverageValue(SimulationProperties.NetEmpirePoint));
		float utility = aiParameterValue / num;
		return AILayer_Research.Normalize(debugContext, 0.5f, 4f, utility);
	}

	[UtilityFunction("AIEmpireStrategicResource")]
	private static float UtilityFunc_EmpireStrategic(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("EmpireAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		DepartmentOfScience agency = empire.GetAgency<DepartmentOfScience>();
		DepartmentOfTheInterior agency2 = empire.GetAgency<DepartmentOfTheInterior>();
		float b = 0f;
		if (agency.GetTechnologyState(TechnologyDefinition.Names.MarketplaceResources) == DepartmentOfScience.ConstructibleElement.State.Researched)
		{
			float averageValue = aidata.GetAverageValue(AILayer_Research.StrategicMarketValue);
			float averageValue2 = aidata.GetAverageValue(SimulationProperties.NetEmpireMoney);
			b = averageValue / Mathf.Max(averageValue2, 1f);
		}
		float technologyUnlockedCount = agency.GetTechnologyUnlockedCount();
		float a = (float)agency2.Cities.Count;
		float a2 = technologyUnlockedCount / (15f * Mathf.Max(a, 1f));
		float num = Mathf.Max(a2, b);
		return aiParameterValue * num;
	}

	[UtilityFunction("AIEmpireLuxuryResource")]
	private static float UtilityFunc_EmpireLuxury(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("EmpireAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		DepartmentOfScience agency = empire.GetAgency<DepartmentOfScience>();
		DepartmentOfTheInterior agency2 = empire.GetAgency<DepartmentOfTheInterior>();
		float b = 0f;
		if (agency.GetTechnologyState(TechnologyDefinition.Names.MarketplaceResources) == DepartmentOfScience.ConstructibleElement.State.Researched)
		{
			float averageValue = aidata.GetAverageValue(AILayer_Research.LuxuryMarketValue);
			float averageValue2 = aidata.GetAverageValue(SimulationProperties.NetEmpireMoney);
			b = averageValue / Mathf.Max(averageValue2, 1f);
		}
		float technologyUnlockedCount = agency.GetTechnologyUnlockedCount();
		float a = (float)agency2.Cities.Count;
		float a2 = technologyUnlockedCount / (15f * Mathf.Max(a, 1f));
		float num = Mathf.Max(a2, b);
		return aiParameterValue * num;
	}

	[UtilityFunction("AIEmpireMilitaryPower")]
	private static float UtilityFunc_EmpireMilitaryPower(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("EmpireAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float num = Mathf.Max(1f, aidata.GetAverageValue(SimulationProperties.MilitaryPower));
		return aiParameterValue / num;
	}

	[UtilityFunction("AIEmpireCityDefense")]
	private static float UtilityFunc_EmpireCityDefense(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("EmpireAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float num = Mathf.Max(1f, aidata.GetAverageValue(SimulationProperties.MaximumCityDefensePoint));
		float utility = aiParameterValue / num;
		return AILayer_Research.Normalize(debugContext, 0f, 3f, utility);
	}

	[UtilityFunction("AIEmpireVision")]
	private static float UtilityFunc_EmpireVision(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("EmpireAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float averageValue = aidata.GetAverageValue(AILayer_Research.NonFriendlyBordersPercent);
		return aiParameterValue * averageValue;
	}

	[UtilityFunction("AIEmpireAntiSpy")]
	private static float UtilityFunc_EmpireAntiSpy(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("EmpireAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float averageValue = aidata.GetAverageValue(AILayer_Research.CityAverageSecurity);
		if (float.IsNaN(averageValue))
		{
			return 0f;
		}
		float utility = aiParameterValue / Mathf.Max(1f, averageValue);
		return AILayer_Research.Normalize(debugContext, 0f, 3f, utility);
	}

	[UtilityFunction("AIEmpirePillageDefense")]
	private static float UtilityFunc_EmpirePillageDefense(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("EmpireAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float averageValue = aidata.GetAverageValue(AILayer_Research.POIAverageDefense);
		if (float.IsNaN(averageValue))
		{
			return 0f;
		}
		float utility = aiParameterValue / Mathf.Max(1f, averageValue);
		return AILayer_Research.Normalize(debugContext, 0f, 3f, utility);
	}

	[UtilityFunction("AIEmpireUnlockShip")]
	private static float UtilityFunc_UnlockShip(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("EmpireAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float averageValue = aidata.GetAverageValue(AILayer_Research.FractionOfNeutralRegionsOnOtherContinents);
		float averageValue2 = aidata.GetAverageValue(AILayer_Research.FractionOfColonizedRegionsOnOtherContinents);
		float num = Mathf.Max(averageValue, averageValue2);
		return aiParameterValue * num;
	}

	[UtilityFunction("AIEmpireUnlockUnit")]
	private static float UtilityFunc_UnlockUnit(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("EmpireAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		DepartmentOfDefense agency = empire.GetAgency<DepartmentOfDefense>();
		int num = agency.UnitDesignDatabase.AvailableUnitBodyDefinitions.Count((UnitBodyDefinition match) => !match.Tags.Contains("Seafaring") && !match.Tags.Contains("Hidden"));
		float num2 = Mathf.Max(1f, (float)(num - 2));
		return aiParameterValue / num2;
	}

	[UtilityFunction("AIEmpireUnlockSeafaringUnit")]
	private static float UtilityFunc_UnlockSeafaringUnit(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("EmpireAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		DepartmentOfDefense agency = empire.GetAgency<DepartmentOfDefense>();
		int num = agency.UnitDesignDatabase.AvailableUnitBodyDefinitions.Count((UnitBodyDefinition match) => match.Tags.Contains("Seafaring") && !match.Tags.Contains("Hidden"));
		float num2 = Mathf.Max(0.25f, (float)num);
		return aiParameterValue / num2;
	}

	[UtilityFunction("AIEmpireUnlockBribe")]
	private static float UtilityFunc_UnlockBribe(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("EmpireAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float averageValue = aidata.GetAverageValue(AILayer_Research.RollingDust);
		float averageValue2 = aidata.GetAverageValue(AILayer_Research.BribeCost);
		float num = averageValue / Mathf.Max(averageValue2, 1f);
		float num2 = aiParameterValue * num;
		float averageValue3 = aidata.GetAverageValue(AILayer_Research.VillageMilitaryPower);
		float averageValue4 = aidata.GetAverageValue(AILayer_Research.ArmyMilitaryPower);
		float num3 = averageValue3 / Mathf.Max(averageValue4, 1f);
		num2 *= num3;
		return AILayer_Research.Normalize(debugContext, 0f, 1f, num2);
	}

	[UtilityFunction("AIEmpireUnlockAssimilation")]
	private static float UtilityFunc_UnlockAssimilation(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("EmpireAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float averageValue = aidata.GetAverageValue(AILayer_Research.NumberOfAvailableMinorFactionVillages);
		float utility = aiParameterValue * averageValue;
		return AILayer_Research.Normalize(debugContext, 0f, 5f, utility);
	}

	[UtilityFunction("AIEmpireUnlockPillage")]
	private static float UtilityFunc_UnlockPillage(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("EmpireAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float averageValue = aidata.GetAverageValue(AILayer_Research.NumberOfPOIExploitedByANonFriendlyEmpire);
		float num = averageValue / 6f;
		float utility = aiParameterValue * num;
		return AILayer_Research.Normalize(debugContext, 0f, 1f, utility);
	}

	[UtilityFunction("AIEmpireUnlockPrivateers")]
	private static float UtilityFunc_UnlockMercenary(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		return aiParameterValue;
	}

	[UtilityFunction("AIEmpireUnlockMarketUnit")]
	private static float UtilityFunc_UnlockMarketUnit(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		return aiParameterValue;
	}

	[UtilityFunction("AIEmpireUnlockMarketResource")]
	private static float UtilityFunc_UnlockMarketResource(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("EmpireAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		float value = AILayer_Research.TechnologyCountFactor(empire, 12, 30, debugContext);
		float a = Mathf.Clamp01(value);
		float averageValue = aidata.GetAverageValue(SimulationProperties.NetEmpireMoney);
		float averageValue2 = aidata.GetAverageValue(SimulationProperties.BankAccount);
		float averageValue3 = aidata.GetAverageValue(AILayer_Research.TotalResourcesMarketValue);
		float a2 = averageValue2 + 8f * empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier) * averageValue;
		float num = averageValue3 / Mathf.Max(a2, 1f);
		num = AILayer_Research.Normalize(debugContext, 2f, 15f, num);
		float num2 = Mathf.Max(a, num);
		return aiParameterValue * num2;
	}

	[UtilityFunction("AIEmpireUnlockBooster")]
	private static float UtilityFunc_UnlockBooster(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		return AILayer_Research.Normalize(debugContext, 0f, 2f, aiParameterValue);
	}

	[UtilityFunction("AIEmpireUnlockBoosterCultist")]
	private static float UtilityFunc_UnlockBoosterCultist(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		return aiParameterValue;
	}

	[UtilityFunction("AIEmpireUnlockDiplomacyPositive")]
	private static float UtilityFunc_UnlockDiplomacyPositive(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("EmpireAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float averageValue = aidata.GetAverageValue(AILayer_Research.DiplomacyPositiveFactor);
		return aiParameterValue * averageValue;
	}

	[UtilityFunction("AIEmpireBuyout")]
	private static float UtilityFunc_EmpireBuyout(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("EmpireAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		float averageValue = aidata.GetAverageValue(SimulationProperties.NetEmpireMoney);
		float averageValue2 = aidata.GetAverageValue(SimulationProperties.NetCityProduction);
		float a = DepartmentOfTheTreasury.ConvertCostsTo(DepartmentOfTheTreasury.Resources.Buyout, DepartmentOfTheTreasury.Resources.Production, averageValue2, empire);
		float num = averageValue / Mathf.Max(a, 1f);
		float utility = aiParameterValue * num;
		return AILayer_Research.Normalize(debugContext, 0f, 0.7f, utility);
	}

	[UtilityFunction("AIEmpireImprovedRuinSearch")]
	private static float UtilityFunc_EmpireSearch(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		return aiParameterValue * 0.1f;
	}

	private static float Normalize(AIHeuristicAnalyser.Context debugContext, float minimumUtilityValue, float maximumUtilityValue, float utility)
	{
		Diagnostics.Assert(maximumUtilityValue > minimumUtilityValue);
		utility = Mathf.Clamp01((utility - minimumUtilityValue) / (maximumUtilityValue - minimumUtilityValue));
		return utility;
	}

	private static float TechnologyCountFactor(global::Empire empire, int minimumNumberOfTechnologyToUnlock, int maximumNumberOfTechnologyToUnlock, AIHeuristicAnalyser.Context debugContext)
	{
		DepartmentOfScience agency = empire.GetAgency<DepartmentOfScience>();
		float technologyUnlockedCount = agency.GetTechnologyUnlockedCount();
		return (technologyUnlockedCount - (float)minimumNumberOfTechnologyToUnlock) / Mathf.Max((float)(maximumNumberOfTechnologyToUnlock - minimumNumberOfTechnologyToUnlock), 1f);
	}

	public void EvaluateTechnologies(List<DepartmentOfScience.ConstructibleElement> technologyDefinitions, ref List<DecisionResult> results)
	{
		AmasEmpireDataMessage amasEmpireDataMessage = base.AIEntity.AIPlayer.Blackboard.GetMessages<AmasEmpireDataMessage>(BlackboardLayerID.Empire).FirstOrDefault<AmasEmpireDataMessage>();
		this.RegisterInterpreterContextData(amasEmpireDataMessage, this.decisionMaker.Context);
		this.decisionMaker.Evaluate(technologyDefinitions.ConvertAll<ConstructibleElement>((DepartmentOfScience.ConstructibleElement match) => match), ref results, null);
	}

	public DecisionResult EvaluateTechnology(TechnologyDefinition technologyDefinition)
	{
		AmasEmpireDataMessage amasEmpireDataMessage = base.AIEntity.AIPlayer.Blackboard.GetMessages<AmasEmpireDataMessage>(BlackboardLayerID.Empire).FirstOrDefault<AmasEmpireDataMessage>();
		this.RegisterInterpreterContextData(amasEmpireDataMessage, this.decisionMaker.Context);
		return this.decisionMaker.Evaluate(technologyDefinition, null);
	}

	public float GetMostWantedTechnologyScore()
	{
		Diagnostics.Assert(this.technologyDecisions != null);
		if (this.technologyDecisions.Count == 0)
		{
			return 0f;
		}
		return this.technologyDecisions[0].Score;
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		this.worldPositionService = gameService.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionService != null);
		this.kaijuTechsService = gameService.Game.Services.GetService<IKaijuTechsService>();
		Diagnostics.Assert(this.kaijuTechsService != null);
		this.worldAtlasHelper = AIScheduler.Services.GetService<IWorldAtlasAIHelper>();
		int idealNumberOfTurnsToResearchTechnology = 8;
		int persistanceDuration = Mathf.RoundToInt((float)idealNumberOfTurnsToResearchTechnology * base.AIEntity.Empire.SimulationObject.GetPropertyValue(SimulationProperties.GameSpeedMultiplier));
		this.researchAIData = new AIData(persistanceDuration);
		InterpreterContext interpreterContext = new InterpreterContext(base.AIEntity.Empire);
		interpreterContext.Register("Empire", base.AIEntity.Empire);
		interpreterContext.Register("EmpireAIData", this.researchAIData);
		this.personalityHelper = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		this.technologyEvaluationHelper = AIScheduler.Services.GetService<IConstructibleElementEvaluationAIHelper>();
		this.decisionMaker = new ElementEvaluator<ConstructibleElement, InterpreterContext>(this.technologyEvaluationHelper, interpreterContext);
		this.decisionMaker.ContextWeightDelegate = new ElementEvaluator<ConstructibleElement, InterpreterContext>.ContextWeightFunc(this.DecisionParameterContextModifier);
		this.InitializeOrientationDecisionMaker();
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_Research_CreateLocalNeedsPass", new AIEntity.AIAction(this.CreateLocalNeeds), this, new StaticString[]
		{
			"LayerAmasEmpire_CreateLocalNeedsPass"
		});
		base.AIEntity.RegisterPass(AIEntity.Passes.EvaluateNeeds.ToString(), "AILayer_Research_EvaluateNeedsPass", new AIEntity.AIAction(this.EvaluateNeeds), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.ExecuteNeeds.ToString(), "AILayer_Research_ExecuteNeedsPass", new AIEntity.AIAction(this.ExecuteNeeds), this, new StaticString[0]);
		this.strategyLayer = base.AIEntity.GetLayer<AILayer_Strategy>();
		Diagnostics.Assert(this.strategyLayer != null);
		this.aiLayerAttitude = base.AIEntity.GetLayer<AILayer_Attitude>();
		Diagnostics.Assert(this.aiLayerAttitude != null);
		IDatabase<DepartmentOfScience.ConstructibleElement> technologyDatabase = Databases.GetDatabase<DepartmentOfScience.ConstructibleElement>(false);
		List<TechnologyDefinition> technologies = new List<TechnologyDefinition>();
		List<TechnologyDefinition> orbUnlocks = new List<TechnologyDefinition>();
		List<KaijuTechnologyDefinition> kaijuUnlocks = new List<KaijuTechnologyDefinition>();
		foreach (DepartmentOfScience.ConstructibleElement constructibleElement in technologyDatabase)
		{
			TechnologyDefinition technologyDefinition = constructibleElement as TechnologyDefinition;
			if (technologyDefinition != null)
			{
				if (technologyDefinition.Visibility == TechnologyDefinitionVisibility.Visible)
				{
					if ((technologyDefinition.TechnologyFlags & DepartmentOfScience.ConstructibleElement.TechnologyFlag.OrbUnlock) == DepartmentOfScience.ConstructibleElement.TechnologyFlag.OrbUnlock)
					{
						orbUnlocks.Add(technologyDefinition);
					}
					else if ((technologyDefinition.TechnologyFlags & DepartmentOfScience.ConstructibleElement.TechnologyFlag.KaijuUnlock) == DepartmentOfScience.ConstructibleElement.TechnologyFlag.KaijuUnlock)
					{
						kaijuUnlocks.Add(technologyDefinition as KaijuTechnologyDefinition);
					}
					else
					{
						technologies.Add(technologyDefinition);
					}
				}
			}
		}
		this.kaijuUnlockDefinitions = kaijuUnlocks.ToArray();
		this.orbUnlockDefinitions = orbUnlocks.ToArray();
		this.technologyDefinitions = technologies.ToArray();
		InfluencedByPersonalityAttribute.LoadFieldAndPropertyValues(base.AIEntity.Empire, this);
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public override void Release()
	{
		base.Release();
		this.worldPositionService = null;
		this.worldAtlasHelper = null;
		this.personalityHelper = null;
		this.technologyEvaluationHelper = null;
		this.decisionMaker = null;
		this.strategyLayer = null;
		this.aiLayerAttitude = null;
	}

	protected DecisionResult GetMostWantedTechnologyDecision()
	{
		Diagnostics.Assert(this.technologyDecisions != null);
		if (this.technologyDecisions.Count == 0)
		{
			return default(DecisionResult);
		}
		return this.technologyDecisions[0];
	}

	protected DecisionResult GetMostWantedOrbUnlockDecision()
	{
		Diagnostics.Assert(this.orbUnlockDecisions != null);
		if (this.orbUnlockDecisions.Count == 0)
		{
			return default(DecisionResult);
		}
		return this.orbUnlockDecisions[0];
	}

	protected DecisionResult GetMostWantedKaijuUnlockDecision()
	{
		Diagnostics.Assert(this.kaijuUnlockDecisions != null);
		if (this.kaijuUnlockDecisions.Count == 0)
		{
			return default(DecisionResult);
		}
		return this.kaijuUnlockDecisions[0];
	}

	protected override void CreateLocalNeeds(StaticString context, StaticString pass)
	{
		this.UpdateResearchAIData();
		Diagnostics.Assert(this.decisionMaker != null);
		base.AIEntity.Context.InitializeElementEvaluator<ConstructibleElement, InterpreterContext>(this.GetResearchContextGroupName(), typeof(AILayer_Research), this.decisionMaker);
	}

	protected override void EvaluateNeeds(StaticString context, StaticString pass)
	{
		base.EvaluateNeeds(context, pass);
		this.PrepareDecisionMaker();
		this.EvaluateResearches();
		this.GenerateResearchBuyoutMessage();
		this.EvaluateOrbUnlocks();
		this.GenerateOrbUnlockBuyoutMessage();
		this.EvaluateKaijuUnlocks();
		this.GenerateKaijuUnlockBuyoutMessage();
	}

	protected override void ExecuteNeeds(StaticString context, StaticString pass)
	{
		base.ExecuteNeeds(context, pass);
		ISynchronousJobRepositoryAIHelper service = AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>();
		service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_QueueResearch));
		service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_BuyoutOrbUnlock));
		service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_BuyoutKaijuUnlock));
	}

	private void RegisterInterpreterContextData(AmasEmpireDataMessage amasEmpireDataMessage, InterpreterContext interpreterContext)
	{
		Diagnostics.Assert(interpreterContext != null);
		interpreterContext.Register("PeaceTechnologyWeight", (amasEmpireDataMessage == null) ? 0f : amasEmpireDataMessage.PeaceTechnologyWeight);
		interpreterContext.Register("AllianceTechnologyWeight", (amasEmpireDataMessage == null) ? 0f : amasEmpireDataMessage.AllianceTechnologyWeight);
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		float num5 = 0f;
		float num6 = 0f;
		float num7 = 0f;
		float num8 = 0f;
		float num9 = 0f;
		float num10 = 0f;
		bool flag = false;
		try
		{
			DepartmentOfDefense agency = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
			foreach (UnitBodyDefinition unitBodyDefinition in agency.UnitDesignDatabase.AvailableUnitBodyDefinitions)
			{
				if (!unitBodyDefinition.Tags.Contains("Hidden"))
				{
					if (unitBodyDefinition.Tags.Contains("Colossus"))
					{
						num6 += 1f;
					}
					else
					{
						if (unitBodyDefinition.SubCategory == "SubCategoryInfantry")
						{
							num += 1f;
						}
						if (unitBodyDefinition.SubCategory == "SubCategoryCavalry")
						{
							num2 += 1f;
						}
						if (unitBodyDefinition.SubCategory == "SubCategoryRanged")
						{
							num3 += 1f;
						}
						if (unitBodyDefinition.SubCategory == "SubCategorySupport")
						{
							num4 += 1f;
						}
						if (unitBodyDefinition.SubCategory == "SubCategoryFlying")
						{
							num5 += 1f;
						}
						if (unitBodyDefinition.SubCategory == "SubCategoryInterceptor")
						{
							num7 += 1f;
							flag = true;
						}
						if (unitBodyDefinition.SubCategory == "SubCategoryFrigate")
						{
							num8 += 1f;
							flag = true;
						}
						if (unitBodyDefinition.SubCategory == "SubCategoryJuggernaut")
						{
							num9 += 1f;
							flag = true;
						}
						if (unitBodyDefinition.SubCategory == "SubCategorySubmersible")
						{
							num10 += 1f;
							flag = true;
						}
					}
				}
			}
		}
		catch (KeyNotFoundException)
		{
		}
		interpreterContext.Register("NumberOfAvailableInfantryUnits", num);
		interpreterContext.Register("NumberOfAvailableCavalryUnits", num2);
		interpreterContext.Register("NumberOfAvailableRangedUnits", num3);
		interpreterContext.Register("NumberOfAvailableSupportUnits", num4);
		interpreterContext.Register("NumberOfAvailableFlyingUnits", num5);
		interpreterContext.Register("NumberOfAvailableGuardianUnits", num6);
		interpreterContext.Register("NumberOfAvailableInterceptorUnits", num7);
		interpreterContext.Register("NumberOfAvailableFrigateUnits", num8);
		interpreterContext.Register("NumberOfAvailableJuggernautUnits", num9);
		interpreterContext.Register("NumberOfAvailableSubmersibleUnits", num10);
		float num11 = base.AIEntity.GetLayer<AILayer_Navy>().NavyImportance.Value;
		if (!flag && num11 > 0.5f)
		{
			num11 = 1f;
		}
		interpreterContext.Register("NavyImportance", num11);
	}

	private float DecisionScoreTransferFunctionDelegate(ConstructibleElement aiEvaluableElement, float score)
	{
		Diagnostics.Assert(AIScheduler.Services != null);
		IIntelligenceAIHelper service = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		Diagnostics.Assert(service != null);
		TechnologyDefinition technologyDefinition = aiEvaluableElement as TechnologyDefinition;
		if (technologyDefinition.Name == "TechnologyDefinitionShip")
		{
			float num = service.EvaluateNeedOfShipTechnology(base.AIEntity.Empire, this.ratioOfExplorationToReach);
			score += score * num;
		}
		return score;
	}

	[SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1513:ClosingCurlyBracketMustBeFollowedByBlankLine", Justification = "For AI heuristic scope we want to have EndScope call just after the closing bracket.")]
	private float DecisionParameterContextModifier(ConstructibleElement aiEvaluableElement, InterpreterContext context, StaticString outputName, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("EmpireAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float num = 0f;
		float modifierValueUnnormalized = base.AIEntity.Context.GetModifierValueUnnormalized(this.GetResearchContextGroupName(), outputName);
		num += modifierValueUnnormalized;
		if (outputName == "AIEmpireGrowth" || outputName == "AIEmpireProduction" || outputName == "AIEmpireResearch" || outputName == "AIEmpireMoney" || outputName == "AIEmpireEmpirePoint")
		{
			float num2 = 0f;
			num2 = this.ComputePopulationBoostValue(outputName, debugContext, num2);
			num = AILayer.Boost(num, num2);
		}
		float num3 = 0f;
		num3 = this.ComputeAttitudeBoostValue(outputName, debugContext, num3);
		num = AILayer.Boost(num, num3);
		float num4 = 1f;
		string regitryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_Research/ElementEvaluatorContextMultiplier/" + outputName;
		num4 = this.personalityHelper.GetRegistryValue<float>(base.AIEntity.Empire, regitryPath, num4);
		return num * num4;
	}

	private float ComputePopulationBoostValue(StaticString outputName, AIHeuristicAnalyser.Context debugContext, float populationBoost)
	{
		Diagnostics.Assert(this.researchAIData != null);
		if (outputName == "AIEmpireGrowth")
		{
			populationBoost = this.researchAIData.GetAverageValue(SimulationProperties.FoodPopulation) / Mathf.Max(this.researchAIData.GetAverageValue(SimulationProperties.Population), 1f);
		}
		else if (outputName == "AIEmpireProduction")
		{
			populationBoost = this.researchAIData.GetAverageValue(SimulationProperties.IndustryPopulation) / Mathf.Max(this.researchAIData.GetAverageValue(SimulationProperties.Population), 1f);
		}
		else if (outputName == "AIEmpireResearch")
		{
			populationBoost = this.researchAIData.GetAverageValue(SimulationProperties.SciencePopulation) / Mathf.Max(this.researchAIData.GetAverageValue(SimulationProperties.Population), 1f);
		}
		else if (outputName == "AIEmpireMoney")
		{
			populationBoost = this.researchAIData.GetAverageValue(SimulationProperties.DustPopulation) / Mathf.Max(this.researchAIData.GetAverageValue(SimulationProperties.Population), 1f);
		}
		else if (outputName == "AIEmpireEmpirePoint")
		{
			populationBoost = this.researchAIData.GetAverageValue(SimulationProperties.CityPointPopulation) / Mathf.Max(this.researchAIData.GetAverageValue(SimulationProperties.Population), 1f);
		}
		return populationBoost;
	}

	private float ComputeAttitudeBoostValue(StaticString outputName, AIHeuristicAnalyser.Context debugContext, float attitudeBoost)
	{
		if (outputName == "AIEmpirePillageDefense")
		{
			float attitudeValue = this.GetAttitudeValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.PillageSucceed, debugContext);
			float attitudeValue2 = this.GetAttitudeValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.Pillaging, debugContext);
			attitudeBoost = Mathf.Max(attitudeValue, attitudeValue2);
		}
		else if (outputName == "AIEmpireAntiSpy")
		{
			attitudeBoost = this.GetAttitudeValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.Spy, debugContext);
		}
		else if (outputName == "AIEmpireVision")
		{
			float attitudeValue3 = this.GetAttitudeValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.PillageSucceed, debugContext);
			float attitudeValue4 = this.GetAttitudeValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.Pillaging, debugContext);
			float attitudeValue5 = this.GetAttitudeValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.Spy, debugContext);
			attitudeBoost = Mathf.Max(new float[]
			{
				attitudeValue3,
				attitudeValue4,
				attitudeValue5
			});
		}
		return attitudeBoost;
	}

	private float GetAttitudeValue(StaticString modifierName, AIHeuristicAnalyser.Context debugContext)
	{
		float num = 0f;
		IGameService service = Services.GetService<IGameService>();
		global::Game game = service.Game as global::Game;
		for (int i = 0; i < game.Empires.Length; i++)
		{
			MajorEmpire majorEmpire = game.Empires[i] as MajorEmpire;
			if (majorEmpire != null && majorEmpire.Index != base.AIEntity.Empire.Index)
			{
				AILayer_Attitude.Attitude attitude = this.aiLayerAttitude.GetAttitude(majorEmpire);
				num = Mathf.Max(num, attitude.Score.GetNormalizedScoreByName(modifierName));
			}
		}
		return num;
	}

	private bool CanEvaluateResearches()
	{
		return base.AIEntity.Empire.GetPropertyValue(SimulationProperties.NetEmpireResearch) >= 0.1f || DepartmentOfScience.CanBuyoutResearch(base.AIEntity.Empire);
	}

	private void PrepareDecisionMaker()
	{
		if (!this.CanEvaluateResearches())
		{
			return;
		}
		AmasEmpireDataMessage amasEmpireDataMessage = base.AIEntity.AIPlayer.Blackboard.GetMessages<AmasEmpireDataMessage>(BlackboardLayerID.Empire).FirstOrDefault<AmasEmpireDataMessage>();
		if (amasEmpireDataMessage != null && amasEmpireDataMessage.ResearchWeights != null)
		{
			Diagnostics.Assert(AILayer_Research.AmasResearchWeightsModifierNames != null);
			Diagnostics.Assert(AILayer_Research.AmasResearchWeightsModifierNames.Length == amasEmpireDataMessage.ResearchWeights.Length);
			for (int i = 0; i < AILayer_Research.AmasResearchWeightsModifierNames.Length; i++)
			{
				base.AIEntity.Context.RegisterBoost(this.GetResearchContextGroupName(), "AMAS", AILayer_Research.AmasResearchWeightsModifierNames[i], (amasEmpireDataMessage.ResearchWeights[i] - 0.5f) / 0.5f * this.contextBoostFromAmas, -1);
			}
		}
		else
		{
			Diagnostics.Assert(AILayer_Research.AmasResearchWeightsModifierNames != null);
			for (int j = 0; j < AILayer_Research.AmasResearchWeightsModifierNames.Length; j++)
			{
				base.AIEntity.Context.RemoveBoost(this.GetResearchContextGroupName(), "AMAS", AILayer_Research.AmasResearchWeightsModifierNames[j]);
			}
		}
		base.AIEntity.Context.InitializeElementEvaluator<ConstructibleElement, InterpreterContext>(this.GetResearchContextGroupName(), typeof(AILayer_Research), this.decisionMaker);
		this.RegisterInterpreterContextData(amasEmpireDataMessage, this.decisionMaker.Context);
	}

	private void EvaluateResearches()
	{
		DepartmentOfScience agency = base.AIEntity.Empire.GetAgency<DepartmentOfScience>();
		if (!this.CanEvaluateResearches())
		{
			return;
		}
		List<ConstructibleElement> list = new List<ConstructibleElement>();
		for (int i = 0; i < this.technologyDefinitions.Length; i++)
		{
			TechnologyDefinition technologyDefinition = this.technologyDefinitions[i];
			if (DepartmentOfTheTreasury.CheckConstructiblePrerequisites(base.AIEntity.Empire, technologyDefinition, new string[]
			{
				ConstructionFlags.Prerequisite
			}))
			{
				DepartmentOfScience.ConstructibleElement.State technologyState = agency.GetTechnologyState(technologyDefinition);
				if (technologyState != DepartmentOfScience.ConstructibleElement.State.Researched && technologyState != DepartmentOfScience.ConstructibleElement.State.NotAvailable)
				{
					list.Add(technologyDefinition);
				}
			}
		}
		Diagnostics.Assert(this.decisionMaker != null);
		this.technologyDecisions.Clear();
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			IGameService service = Services.GetService<IGameService>();
			global::Game game = service.Game as global::Game;
			EvaluationData<ConstructibleElement, InterpreterContext> evaluationData = new EvaluationData<ConstructibleElement, InterpreterContext>();
			this.decisionMaker.Evaluate(list, ref this.technologyDecisions, evaluationData);
			evaluationData.Turn = game.Turn;
			this.TechnologyDecisionMakerEvaluationDataHistoric.Add(evaluationData);
		}
		else
		{
			this.decisionMaker.Evaluate(list, ref this.technologyDecisions, null);
		}
	}

	private SynchronousJobState SynchronousJob_QueueResearch()
	{
		if (DepartmentOfScience.CanBuyoutResearch(base.AIEntity.Empire))
		{
			List<EvaluableMessage_ResearchBuyout> list = new List<EvaluableMessage_ResearchBuyout>(base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_ResearchBuyout>(BlackboardLayerID.Empire, (EvaluableMessage_ResearchBuyout match) => match.State == BlackboardMessage.StateValue.Message_InProgress));
			if (list.Count != 0)
			{
				if (list.Count > 1)
				{
					AILayer.LogWarning("There should not be several ResearchBuyout EvaluableMessages for the same empire ({0})", new object[]
					{
						base.AIEntity.Empire.Index
					});
				}
				EvaluableMessage_ResearchBuyout evaluableMessage_ResearchBuyout = list[0];
				if (evaluableMessage_ResearchBuyout.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate)
				{
					OrderBuyOutTechnology order = new OrderBuyOutTechnology(base.AIEntity.Empire.Index, evaluableMessage_ResearchBuyout.TechnologyReference);
					Ticket ticket;
					base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.BuyOutResearch_TicketRaised));
					return SynchronousJobState.Success;
				}
			}
		}
		DepartmentOfScience agency = base.AIEntity.Empire.GetAgency<DepartmentOfScience>();
		if (agency.ResearchQueue.PendingConstructions.Count > 0)
		{
			return SynchronousJobState.Failure;
		}
		DepartmentOfScience.ConstructibleElement constructibleElement = this.GetMostWantedTechnologyDecision().Element as DepartmentOfScience.ConstructibleElement;
		if (constructibleElement == null)
		{
			return SynchronousJobState.Failure;
		}
		DepartmentOfScience.ConstructibleElement.State technologyState = agency.GetTechnologyState(constructibleElement);
		if (technologyState != DepartmentOfScience.ConstructibleElement.State.Available)
		{
			return SynchronousJobState.Failure;
		}
		OrderQueueResearch order2 = new OrderQueueResearch(base.AIEntity.Empire.Index, constructibleElement);
		base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order2);
		return SynchronousJobState.Success;
	}

	public const string RegistryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_Research";

	private static readonly StaticString NonFriendlyBordersPercent;

	private static readonly StaticString FractionOfNeutralRegionsOnOtherContinents;

	private static readonly StaticString FractionOfColonizedRegionsOnOtherContinents;

	private static readonly StaticString RollingDust;

	private static readonly StaticString BribeCost;

	private static readonly StaticString VillageMilitaryPower;

	private static readonly StaticString ArmyMilitaryPower;

	private static readonly StaticString NumberOfPOIExploitedByANonFriendlyEmpire;

	private static readonly StaticString POIAverageDefense;

	private static readonly StaticString CityAverageSecurity;

	private static readonly StaticString StrategicMarketValue;

	private static readonly StaticString LuxuryMarketValue;

	private static readonly StaticString NumberOfAvailableMinorFactionVillages;

	private static readonly StaticString DiplomacyPositiveFactor;

	private static readonly StaticString TotalResourcesMarketValue;

	[InfluencedByPersonality]
	private float maximumUnlockScore = 5f;

	private SimulationDecisionMaker<ConstructibleElement> orientationDecisionMaker;

	public static StaticString[] AmasResearchWeightsModifierNames = new StaticString[]
	{
		"AIEmpireGrowth",
		"AIEmpireProduction",
		"AIEmpireResearch",
		"AIEmpireMoney",
		"AIEmpireEmpirePoint"
	};

	public FixedSizedList<EvaluationData<ConstructibleElement, InterpreterContext>> TechnologyDecisionMakerEvaluationDataHistoric = new FixedSizedList<EvaluationData<ConstructibleElement, InterpreterContext>>(global::Application.FantasyPreferences.AIDebugHistoricSize);

	public FixedSizedList<EvaluationData<ConstructibleElement, InterpreterContext>> OrbUnlockDecisionMakerEvaluationDataHistoric = new FixedSizedList<EvaluationData<ConstructibleElement, InterpreterContext>>(global::Application.FantasyPreferences.AIDebugHistoricSize);

	public FixedSizedList<EvaluationData<ConstructibleElement, InterpreterContext>> KaijuUnlockDecisionMakerEvaluationDataHistoric = new FixedSizedList<EvaluationData<ConstructibleElement, InterpreterContext>>(global::Application.FantasyPreferences.AIDebugHistoricSize);

	private static StaticString researchContextGroupName = "Research";

	private float contextBoostFromAmas = 0.5f;

	private ElementEvaluator<ConstructibleElement, InterpreterContext> decisionMaker;

	private List<DecisionResult> technologyDecisions = new List<DecisionResult>();

	private List<DecisionResult> orbUnlockDecisions = new List<DecisionResult>();

	private List<DecisionResult> kaijuUnlockDecisions = new List<DecisionResult>();

	private float ratioOfExplorationToReach = 0.7f;

	private AILayer_Strategy strategyLayer;

	private IConstructibleElementEvaluationAIHelper technologyEvaluationHelper;

	private IKaijuTechsService kaijuTechsService;

	private AIData researchAIData;

	private IWorldAtlasAIHelper worldAtlasHelper;

	private IWorldPositionningService worldPositionService;

	private TechnologyDefinition[] technologyDefinitions;

	private TechnologyDefinition[] orbUnlockDefinitions;

	private KaijuTechnologyDefinition[] kaijuUnlockDefinitions;

	private IPersonalityAIHelper personalityHelper;

	private AILayer_Attitude aiLayerAttitude;

	private static class OutputAIParameterNames
	{
		public const string EmpireApproval = "AIEmpireApproval";

		public const string EmpireGrowth = "AIEmpireGrowth";

		public const string EmpireProduction = "AIEmpireProduction";

		public const string EmpireMoney = "AIEmpireMoney";

		public const string EmpireResearch = "AIEmpireResearch";

		public const string EmpireEmpirePoint = "AIEmpireEmpirePoint";

		public const string EmpirePillageDefense = "AIEmpirePillageDefense";

		public const string EmpireCityDefense = "AIEmpireCityDefense";

		public const string EmpireMilitaryPower = "AIEmpireMilitaryPower";

		public const string EmpireAntiSpy = "AIEmpireAntiSpy";

		public const string EmpireVision = "AIEmpireVision";

		public const string EmpireStrategicResource = "AIEmpireStrategicResource";

		public const string EmpireLuxuryResource = "AIEmpireLuxuryResource";

		public const string EmpireUnlockShip = "AIEmpireUnlockShip";

		public const string EmpireUnlockUnit = "AIEmpireUnlockUnit";

		public const string EmpireUnlockSeafaringUnit = "AIEmpireUnlockSeafaringUnit";

		public const string EmpireUnlockBribe = "AIEmpireUnlockBribe";

		public const string EmpireUnlockAssimilation = "AIEmpireUnlockAssimilation";

		public const string EmpireUnlockPillage = "AIEmpireUnlockPillage";

		public const string EmpireUnlockPrivateers = "AIEmpireUnlockPrivateers";

		public const string EmpireUnlockMarketUnit = "AIEmpireUnlockMarketUnit";

		public const string EmpireUnlockMarketResource = "AIEmpireUnlockMarketResource";

		public const string EmpireUnlockBooster = "AIEmpireUnlockBooster";

		public const string EmpireUnlockBoosterCultist = "AIEmpireUnlockBoosterCultist";

		public const string EmpireUnlockDiplomacyPositive = "AIEmpireUnlockDiplomacyPositive";

		public const string EmpireBuyout = "AIEmpireBuyout";

		public const string EmpireImprovedRuinSearch = "AIEmpireImprovedRuinSearch";
	}
}
