using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Amplitude;
using Amplitude.Collections;
using Amplitude.Extensions;
using Amplitude.Unity.AI;
using Amplitude.Unity.AI.Decision;
using Amplitude.Unity.AI.Evaluation;
using Amplitude.Unity.AI.Evaluation.Diagnostics;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Session;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

[PersonalityRegistryPath("AI/MajorEmpire/AIEntity_City/AILayer_Production/", new object[]
{

})]
public class AILayer_Production : AILayer, IAIEvaluationHelper<ConstructibleElement, InterpreterContext>, IXmlSerializable, IAIEvaluationHelper<WorldPositionScore, InterpreterContext>, ISimulationAIEvaluationHelper<ConstructibleElement>, ISimulationAIEvaluationHelper<WorldPositionScore>
{
	static AILayer_Production()
	{
		AILayer_Production.EmpireNetStrategicResources = "EmpireNetStrategicResources";
		AILayer_Production.EmpireNetLuxuryResources = "EmpireNetLuxuryResources";
		AILayer_Production.EmpireCityMoneyUpkeep = "EmpireCityMoneyUpkeep";
		AILayer_Production.FractionOfNeighbouringRegionsControlledByANonFriendlyEmpire = "FractionOfNeighbouringRegionsControlledByANonFriendlyEmpire";
		AILayer_Production.EmpireCityMoney = "EmpireCityMoney";
		AILayer_Production.POIPillageDefense = "POIPillageDefense";
		AILayer_Production.EmpireMilitaryPower = "EmpireMilitaryPower";
	}

	private void UpdateCityAIData()
	{
		DepartmentOfTheTreasury agency = base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>();
		City city = this.aiEntityCity.City;
		this.cityAIData.RegisterValue(SimulationProperties.NetCityApproval, city.GetPropertyValue(SimulationProperties.NetCityApproval));
		this.cityAIData.RegisterValue(SimulationProperties.NetCityGrowth, city.GetPropertyValue(SimulationProperties.NetCityGrowth));
		this.cityAIData.RegisterValue(SimulationProperties.NetCityProduction, city.GetPropertyValue(SimulationProperties.NetCityProduction));
		this.cityAIData.RegisterValue(SimulationProperties.NetCityResearch, city.GetPropertyValue(SimulationProperties.NetCityResearch));
		this.cityAIData.RegisterValue(SimulationProperties.NetCityMoney, city.GetPropertyValue(SimulationProperties.NetCityMoney));
		this.cityAIData.RegisterValue(SimulationProperties.NetCityEmpirePoint, city.GetPropertyValue(SimulationProperties.NetCityEmpirePoint));
		this.cityAIData.RegisterValue(SimulationProperties.MaximumCityDefensePoint, city.GetPropertyValue(SimulationProperties.MaximumCityDefensePoint));
		this.cityAIData.RegisterValue(SimulationProperties.NetCityAntiSpy, city.GetPropertyValue(SimulationProperties.NetCityAntiSpy));
		float num = 0f;
		float num2 = 0f;
		foreach (ResourceDefinition resourceDefinition in Databases.GetDatabase<ResourceDefinition>(false))
		{
			float num3;
			if (resourceDefinition.ResourceType == ResourceDefinition.Type.Strategic && agency.TryGetNetResourceValue(city.SimulationObject, resourceDefinition.Name, out num3, true))
			{
				num += num3;
			}
			float num4;
			if (resourceDefinition.ResourceType == ResourceDefinition.Type.Luxury && agency.TryGetNetResourceValue(city.SimulationObject, resourceDefinition.Name, out num4, true))
			{
				num2 += num4;
			}
		}
		this.cityAIData.RegisterValue(AILayer_Production.EmpireNetStrategicResources, num);
		this.cityAIData.RegisterValue(AILayer_Production.EmpireNetLuxuryResources, num2);
		int num5 = 0;
		int num6 = 0;
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(service2 != null);
		for (int i = 0; i < city.Region.Borders.Length; i++)
		{
			Region.Border border = city.Region.Borders[i];
			Region region = service2.GetRegion(border.NeighbourRegionIndex);
			global::Empire empire = (!region.IsRegionColonized()) ? null : region.Owner;
			if (empire != null && empire.Index != base.AIEntity.Empire.Index && !(empire is KaijuEmpire))
			{
				DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(empire);
				if (diplomaticRelation.State != null && diplomaticRelation.State.Name != DiplomaticRelationState.Names.Peace && diplomaticRelation.State.Name != DiplomaticRelationState.Names.Alliance)
				{
					num5++;
				}
			}
			num6++;
		}
		float value = (float)num5 / Mathf.Max((float)num6, 1f);
		this.cityAIData.RegisterValue(AILayer_Production.FractionOfNeighbouringRegionsControlledByANonFriendlyEmpire, value);
		float num7 = 0f;
		float num8 = 0f;
		for (int j = 0; j < this.departmentOfTheInterior.Cities.Count; j++)
		{
			num7 += city.GetPropertyValue(SimulationProperties.CityMoneyUpkeep);
			num8 += city.GetPropertyValue(SimulationProperties.CityMoney);
		}
		this.cityAIData.RegisterValue(AILayer_Production.EmpireCityMoneyUpkeep, num7);
		this.cityAIData.RegisterValue(AILayer_Production.EmpireCityMoney, num8);
		this.cityAIData.RegisterValue(AILayer_Production.EmpireMilitaryPower, this.Empire.GetPropertyValue(SimulationProperties.MilitaryPower));
		this.cityAIData.RegisterValue(SimulationProperties.MilitaryPower, city.GetPropertyValue(SimulationProperties.MilitaryPower));
		float num9 = 0f;
		foreach (PointOfInterest pointOfInterest in city.Region.PointOfInterests)
		{
			num9 += pointOfInterest.SimulationObject.GetPropertyValue(SimulationProperties.MaximumPillageDefense);
		}
		this.cityAIData.RegisterValue(AILayer_Production.POIPillageDefense, num9);
	}

	private void InitializeExtensions()
	{
		DepartmentOfIndustry.ConstructibleElement[] availableConstructibleElements = ((IConstructibleElementDatabase)this.departmentOfIndustry).GetAvailableConstructibleElements(new StaticString[]
		{
			DistrictImprovementDefinition.ReadOnlyCategory
		});
		for (int i = 0; i < availableConstructibleElements.Length; i++)
		{
			DistrictImprovementDefinition districtImprovementDefinition = availableConstructibleElements[i] as DistrictImprovementDefinition;
			if (districtImprovementDefinition != null)
			{
				this.extensionEvaluations.Add(new AILayer_Production.ExtensionEvaluation
				{
					DistrictImprovementDefinition = districtImprovementDefinition
				});
			}
		}
		GameServer gameServer = (Services.GetService<ISessionService>().Session as global::Session).GameServer as GameServer;
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (gameServer.AIScheduler != null && gameServer.AIScheduler.TryGetMajorEmpireAIPlayer(base.AIEntity.Empire as MajorEmpire, out aiplayer_MajorEmpire))
		{
			AIEntity entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
			if (entity != null)
			{
				this.VictoryLayer = entity.GetLayer<AILayer_Victory>();
				this.ColonizationLayer = entity.GetLayer<AILayer_Colonization>();
				this.NavyLayer = entity.GetLayer<AILayer_Navy>();
				this.DiplomacyLayer = entity.GetLayer<AILayer_Diplomacy>();
			}
		}
		this.departmentOfDefense = this.Empire.GetAgency<DepartmentOfDefense>();
		this.departmentOfForeignAffairs = this.Empire.GetAgency<DepartmentOfForeignAffairs>();
		this.departmentOfTheInterior = this.Empire.GetAgency<DepartmentOfTheInterior>();
		this.worldAtlasHelper = AIScheduler.Services.GetService<IWorldAtlasAIHelper>();
		this.departmentOfIndustry.OnConstructionChange += this.DepartmentOfIndustry_OnConstructionChange;
	}

	private void GenerateBuildingMessageForExtension(ref List<EvaluableMessage_BuildingProduction> buildingMessages, ref EvaluationData<ConstructibleElement, InterpreterContext> evaluationData)
	{
		this.needCostalAccess = true;
		Func<District, bool> <>9__1;
		for (int i = 0; i < this.departmentOfTheInterior.NonInfectedCities.Count; i++)
		{
			IEnumerable<District> districts = this.departmentOfTheInterior.NonInfectedCities[i].Districts;
			Func<District, bool> predicate;
			if ((predicate = <>9__1) == null)
			{
				predicate = (<>9__1 = ((District match) => this.worldPositionningService.IsWaterTile(match.WorldPosition)));
			}
			if (districts.Any(predicate))
			{
				this.needCostalAccess = false;
				break;
			}
		}
		this.extensionScores = this.worldPositionEvaluationAIHelper.GetWorldPositionExpansionScore(this.Empire, this.aiEntityCity.City);
		for (int j = 0; j < this.extensionEvaluations.Count; j++)
		{
			this.DefineBestExtensionPositionFor(this.extensionEvaluations[j], this.extensionScores, ref evaluationData);
		}
		float globalMotivation = 0.8f;
		if (this.aiEntityCity.AICityState != null)
		{
			globalMotivation = this.aiEntityCity.AICityState.ExtensionGlobalPriority;
		}
		AILayer_Production.ExtensionEvaluation currentExtensionEvaluation;
		EvaluableMessage_BuildingProduction evaluableMessage_BuildingProduction;
		if (this.FindCurrentExtensionMessage(buildingMessages, out evaluableMessage_BuildingProduction, out currentExtensionEvaluation))
		{
			if (evaluableMessage_BuildingProduction.EvaluationState != EvaluableMessage.EvaluableMessageState.Obtaining)
			{
				if (currentExtensionEvaluation.DistrictImprovementDefinition.OnePerEmpire && base.AIEntity.AIPlayer.Blackboard.FindFirst<EvaluableMessage_BuildingProduction>(BlackboardLayerID.City, (EvaluableMessage_BuildingProduction match) => match.ConstructibleElementName == currentExtensionEvaluation.DistrictImprovementDefinition.Name && match.CityGuid != this.aiEntityCity.City.GUID && match.EvaluationState == EvaluableMessage.EvaluableMessageState.Obtaining) != null)
				{
					evaluableMessage_BuildingProduction.Cancel();
				}
				else
				{
					evaluableMessage_BuildingProduction.UpdatePosition(currentExtensionEvaluation.LastWorldPosition);
				}
			}
			else
			{
				if (evaluableMessage_BuildingProduction.ChosenProductionEvaluation != null && !this.constructionQueue.Contains(currentExtensionEvaluation.DistrictImprovementDefinition))
				{
					evaluableMessage_BuildingProduction.ResetState();
				}
				evaluableMessage_BuildingProduction.UpdatePosition(currentExtensionEvaluation.LastWorldPosition);
			}
		}
		else
		{
			List<MissingResource> list = new List<MissingResource>();
			currentExtensionEvaluation = null;
			float num = 0f;
			int index2;
			int index;
			Predicate<EvaluableMessage_BuildingProduction> <>9__2;
			for (index = 0; index < this.extensionEvaluations.Count; index = index2 + 1)
			{
				if (!this.extensionEvaluations[index].DistrictImprovementDefinition.OnePerWorld)
				{
					if (this.extensionEvaluations[index].DistrictImprovementDefinition.OnePerEmpire)
					{
						Blackboard<BlackboardLayerID, BlackboardMessage> blackboard = base.AIEntity.AIPlayer.Blackboard;
						BlackboardLayerID blackboardLayerID = BlackboardLayerID.City;
						BlackboardLayerID layerID = blackboardLayerID;
						Predicate<EvaluableMessage_BuildingProduction> filter;
						if ((filter = <>9__2) == null)
						{
							filter = (<>9__2 = ((EvaluableMessage_BuildingProduction match) => match.ConstructibleElementName == this.extensionEvaluations[index].DistrictImprovementDefinition.Name && match.CityGuid != this.aiEntityCity.City.GUID && match.EvaluationState == EvaluableMessage.EvaluableMessageState.Obtaining));
						}
						if (blackboard.FindFirst<EvaluableMessage_BuildingProduction>(layerID, filter) != null)
						{
							goto IL_33F;
						}
					}
					if (this.extensionEvaluations[index].LastScore > num)
					{
						list.Clear();
						this.departmentOfTheTreasury.FillConstructibleMissingRessources(this.aiEntityCity.City, this.extensionEvaluations[index].DistrictImprovementDefinition, ref list);
						if (list != null && list.Count > 0)
						{
							AILayer_Trade.UpdateResourceNeed(1f, this.extensionEvaluations[index].LastScore, list, this.extensionEvaluations[index].DistrictImprovementDefinition.Name, this.aiEntityCity.Blackboard);
						}
						else
						{
							num = this.extensionEvaluations[index].LastScore;
							currentExtensionEvaluation = this.extensionEvaluations[index];
						}
					}
				}
				IL_33F:
				index2 = index;
			}
			if (currentExtensionEvaluation == null)
			{
				return;
			}
			evaluableMessage_BuildingProduction = new EvaluableMessage_BuildingProduction(this.aiEntityCity.City.GUID, currentExtensionEvaluation.DistrictImprovementDefinition.Name, currentExtensionEvaluation.LastWorldPosition.WorldPosition, new WorldPositionScore(currentExtensionEvaluation.LastWorldPosition), 1, AILayer_AccountManager.EconomyAccountName);
			base.AIEntity.AIPlayer.Blackboard.AddMessage(evaluableMessage_BuildingProduction);
		}
		evaluableMessage_BuildingProduction.Tick(globalMotivation, currentExtensionEvaluation.LastScore);
	}

	private bool FindCurrentExtensionMessage(List<EvaluableMessage_BuildingProduction> buildingMessages, out EvaluableMessage_BuildingProduction message, out AILayer_Production.ExtensionEvaluation evaluation)
	{
		message = null;
		evaluation = null;
		for (int i = 0; i < buildingMessages.Count; i++)
		{
			EvaluableMessage_BuildingProduction currentMessage = buildingMessages[i];
			if (currentMessage.BuildingWorldPositionScore != null)
			{
				if (currentMessage.State == BlackboardMessage.StateValue.Message_InProgress)
				{
					evaluation = this.extensionEvaluations.Find((AILayer_Production.ExtensionEvaluation match) => match.DistrictImprovementDefinition.Name == currentMessage.ConstructibleElementName);
					if (evaluation != null)
					{
						message = currentMessage;
						return true;
					}
				}
			}
		}
		return false;
	}

	private void DefineBestExtensionPositionFor(AILayer_Production.ExtensionEvaluation extensionToEvaluate, WorldPositionScore[] extensionScores, ref EvaluationData<ConstructibleElement, InterpreterContext> evaluationData)
	{
		extensionToEvaluate.LastScore = 0f;
		extensionToEvaluate.LastWorldPosition = null;
		if (!DepartmentOfTheTreasury.CheckConstructiblePrerequisites(this.aiEntityCity.City, extensionToEvaluate.DistrictImprovementDefinition, new string[]
		{
			ConstructionFlags.Discard,
			ConstructionFlags.Disable
		}) || this.aiEntityCity.City.IsInfected)
		{
			return;
		}
		List<WorldPosition> list = new List<WorldPosition>();
		for (int i = 0; i < this.aiEntityCity.City.Districts.Count; i++)
		{
			District district = this.aiEntityCity.City.Districts[i];
			if (district != null && district.SimulationObject.Tags.Contains(DistrictImprovementDefinition.ReadOnlyWonderClass) && district.Level < 1)
			{
				foreach (WorldPosition worldPosition in district.WorldPosition.GetNeighbours(this.worldPositionningService.World.WorldParameters))
				{
					District district2 = this.worldPositionningService.GetDistrict(worldPosition);
					if (district2 != null && this.IsPositionValidForExtension(extensionToEvaluate.DistrictImprovementDefinition, district2))
					{
						list.Add(district2.WorldPosition);
					}
				}
			}
		}
		bool flag = list.Count > 0;
		this.bestWorldPositionScoreSum = 0f;
		for (int j = 0; j < extensionScores.Length; j++)
		{
			District district3 = this.worldPositionningService.GetDistrict(extensionScores[j].WorldPosition);
			if (district3 != null && this.IsPositionValidForExtension(extensionToEvaluate.DistrictImprovementDefinition, district3) && (!flag || list.Contains(extensionScores[j].WorldPosition)))
			{
				float scoresSum = extensionScores[j].ScoresSum;
				if (extensionScores[j].ScoresSum > this.bestWorldPositionScoreSum)
				{
					this.bestWorldPositionScoreSum = extensionScores[j].ScoresSum;
				}
			}
		}
		for (int k = 0; k < extensionScores.Length; k++)
		{
			District district4 = this.worldPositionningService.GetDistrict(extensionScores[k].WorldPosition);
			if (district4 != null && this.IsPositionValidForExtension(extensionToEvaluate.DistrictImprovementDefinition, district4) && (!flag || list.Contains(extensionScores[k].WorldPosition)))
			{
				this.currentWorldPositionScore = extensionScores[k];
				float num = this.decisionMaker.Evaluate(extensionToEvaluate.DistrictImprovementDefinition, null).Score;
				this.currentWorldPositionScore = null;
				if (flag)
				{
					num = AILayer.Boost(num, 0.3f);
				}
				if (num > extensionToEvaluate.LastScore)
				{
					extensionToEvaluate.LastScore = num;
					extensionToEvaluate.LastWorldPosition = extensionScores[k];
				}
			}
		}
		this.currentWorldPositionScore = null;
	}

	private float ElementEvaluationScoreTransferFunction(WorldPositionScore aiEvaluableElement, InterpreterContext context, float score, AIHeuristicAnalyser.Context debugContext)
	{
		if (this.currentWorldPositionScore == null)
		{
			return score;
		}
		float boostFactor = this.ComputeExtensionAlignementFactor(aiEvaluableElement);
		score = AILayer.Boost(score, boostFactor);
		score *= this.currentWorldPositionScore.ScoresSum / this.bestWorldPositionScoreSum * 0.4f + 0.6f;
		if (this.aiEntityCity.AIDataCity.CityTileCount > 2)
		{
			float boostFactor2 = (float)(aiEvaluableElement.SumOfNumberOfExtensionAround - 3) * 0.3f;
			score = AILayer.Boost(score, boostFactor2);
		}
		else if (this.currentWorldPositionScore.NewDistrictNeighbourgNumber < 3)
		{
			float boostFactor3 = -0.2f;
			score = AILayer.Boost(score, boostFactor3);
		}
		float boostFactor4 = (float)aiEvaluableElement.SumOfLostTiles / 6f * -1f;
		score = AILayer.Boost(score, boostFactor4);
		if (aiEvaluableElement.HasCostalTile && this.needCostalAccess)
		{
			float boostFactor5 = 0.5f;
			score = AILayer.Boost(score, boostFactor5);
		}
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools && ELCPUtilities.ELCPVerboseMode)
		{
			Diagnostics.Log("ELCP {0} {1} district pos {2} with score {3}/{4}", new object[]
			{
				this.Empire,
				this.aiEntityCity.City.LocalizedName,
				this.currentWorldPositionScore.WorldPosition,
				score,
				this.currentWorldPositionScore.ScoresSum
			});
		}
		return score;
	}

	private WorldPositionScore GetExtensionBestPosition(StaticString name)
	{
		AILayer_Production.ExtensionEvaluation extensionEvaluation = this.extensionEvaluations.Find((AILayer_Production.ExtensionEvaluation match) => match.DistrictImprovementDefinition.Name == name);
		if (extensionEvaluation != null && extensionEvaluation.LastScore > 0f)
		{
			return extensionEvaluation.LastWorldPosition;
		}
		return null;
	}

	private WorldPositionScore DefineBestPositionForUnit()
	{
		for (int i = this.extensionScores.Length - 1; i >= 0; i--)
		{
			if (!this.alreadyUsedPosition.Contains(this.extensionScores[i].WorldPosition))
			{
				if (!this.worldPositionningService.IsWaterTile(this.extensionScores[i].WorldPosition))
				{
					if (!this.IsInLineWithExtensionPlanning(this.extensionScores[i].WorldPosition))
					{
						return this.extensionScores[i];
					}
					if (this.extensionScores[i].SumOfLostTiles > 0)
					{
						return this.extensionScores[i];
					}
				}
			}
		}
		return null;
	}

	private void ReserveExtensionPosition(WorldPosition worldPosition)
	{
		this.alreadyUsedPosition.Add(worldPosition);
	}

	private bool IsPositionValidForExtension(DistrictImprovementDefinition districtImprovementDefinition, District district)
	{
		DistrictType type = district.Type;
		if (type != DistrictType.Exploitation)
		{
			return false;
		}
		if (districtImprovementDefinition.WorldPlacementTags != null && !districtImprovementDefinition.WorldPlacementTags.IsNullOrEmpty)
		{
			return district.SimulationObject.Tags != null && districtImprovementDefinition.WorldPlacementTags.Check(district.SimulationObject.Tags);
		}
		return !this.worldPositionningService.IsWaterTile(district.WorldPosition);
	}

	private int GatherLostTileAround(WorldPosition worldposition, int currentRegionIndex)
	{
		int num = 0;
		for (int i = 0; i < 6; i++)
		{
			WorldPosition neighbourTile = this.worldPositionningService.GetNeighbourTile(worldposition, (WorldOrientation)i, 1);
			if (!this.worldPositionningService.IsExploitable(neighbourTile, 0) || (int)this.worldPositionningService.GetRegionIndex(neighbourTile) != currentRegionIndex)
			{
				num++;
			}
		}
		return num;
	}

	private void GenerateBuildingMessageFor(DepartmentOfIndustry.ConstructibleElement[] constructibleElements, ref List<EvaluableMessage_BuildingProduction> buildingMessages, ref EvaluationData<ConstructibleElement, InterpreterContext> evaluationData)
	{
		for (int i = 0; i < constructibleElements.Length; i++)
		{
			DepartmentOfIndustry.ConstructibleElement constructibleElement = constructibleElements[i];
			if (DepartmentOfTheTreasury.CheckConstructiblePrerequisites(this.aiEntityCity.City, constructibleElement, new string[]
			{
				ConstructionFlags.Prerequisite
			}))
			{
				if (constructibleElement is CityConstructibleActionDefinition && (constructibleElement as CityConstructibleActionDefinition).Action.Name == "PurgeTheLand")
				{
					int num = 0;
					PointOfInterest[] pointOfInterests = this.aiEntityCity.City.Region.PointOfInterests;
					for (int j = 0; j < pointOfInterests.Length; j++)
					{
						if (pointOfInterests[j].CreepingNodeGUID != GameEntityGUID.Zero)
						{
							IGameEntity gameEntity = null;
							if (this.gameEntityRepositoryService.TryGetValue(pointOfInterests[j].CreepingNodeGUID, out gameEntity))
							{
								CreepingNode creepingNode = gameEntity as CreepingNode;
								if (creepingNode != null && creepingNode.Empire.Index != this.aiEntityCity.City.Empire.Index && !this.departmentOfForeignAffairs.IsFriend(creepingNode.Empire))
								{
									num++;
								}
							}
						}
					}
					if (num < 3 || this.aiEntityCity.City.GetPropertyValue(SimulationProperties.Population) < 6f)
					{
						goto IL_3D4;
					}
				}
				if (this.aiEntityCity.City.IsInfected)
				{
					if (constructibleElement.SubCategory != DepartmentOfTheInterior.InfectionAllowedSubcategory)
					{
						goto IL_3D4;
					}
					if (constructibleElement is CityConstructibleActionDefinition)
					{
						CityConstructibleActionDefinition cityConstructibleActionDefinition = constructibleElement as CityConstructibleActionDefinition;
						if (cityConstructibleActionDefinition.Action.Name == "IntegrateFaction" && (string.IsNullOrEmpty(cityConstructibleActionDefinition.InfectedAffinityConstraint) || !cityConstructibleActionDefinition.InfectedAffinityConstraint.Equals(this.aiEntityCity.City.LastNonInfectedOwner.Faction.Affinity.Name) || this.aiEntityCity.City.LastNonInfectedOwner.Faction.GetIntegrationDescriptorsCount() <= 0 || this.departmentOfPlanificationAndDevelopment.HasIntegratedFaction(this.aiEntityCity.City.LastNonInfectedOwner.Faction)))
						{
							goto IL_3D4;
						}
					}
				}
				bool flag = false;
				if (constructibleElement.Descriptors.Any((SimulationDescriptor match) => match.Name == "OnlyOneConstructionPerEmpire" || match.Name == "OnlyOnePerEmpire"))
				{
					flag = (base.AIEntity.AIPlayer.Blackboard.FindFirst<EvaluableMessage_BuildingProduction>(BlackboardLayerID.City, (EvaluableMessage_BuildingProduction match) => match.ConstructibleElementName == constructibleElement.Name && match.CityGuid != this.aiEntityCity.City.GUID && match.EvaluationState == EvaluableMessage.EvaluableMessageState.Obtaining) != null);
				}
				float num2 = this.decisionMaker.Evaluate(constructibleElement, evaluationData).Score;
				if (num2 < 0f)
				{
					num2 = 0f;
				}
				List<MissingResource> constructibleMissingRessources = this.departmentOfTheTreasury.GetConstructibleMissingRessources(this.aiEntityCity.City, constructibleElement);
				if (constructibleMissingRessources != null && constructibleMissingRessources.Count > 0)
				{
					flag = true;
					AILayer_Trade.UpdateResourceNeed(1f, num2, constructibleMissingRessources, constructibleElement.Name, this.aiEntityCity.Blackboard);
				}
				EvaluableMessage_BuildingProduction evaluableMessage_BuildingProduction = buildingMessages.Find((EvaluableMessage_BuildingProduction match) => match.ConstructibleElementName == constructibleElement.Name && match.State == BlackboardMessage.StateValue.Message_InProgress);
				if (evaluableMessage_BuildingProduction == null)
				{
					if (flag)
					{
						goto IL_3D4;
					}
					evaluableMessage_BuildingProduction = new EvaluableMessage_BuildingProduction(this.aiEntityCity.City.GUID, constructibleElement.Name, WorldPosition.Invalid, null, 1, AILayer_AccountManager.EconomyAccountName);
					base.AIEntity.AIPlayer.Blackboard.AddMessage(evaluableMessage_BuildingProduction);
				}
				else if (evaluableMessage_BuildingProduction.EvaluationState != EvaluableMessage.EvaluableMessageState.Obtaining)
				{
					if (flag)
					{
						evaluableMessage_BuildingProduction.Cancel();
						goto IL_3D4;
					}
				}
				else if (evaluableMessage_BuildingProduction.ChosenProductionEvaluation != null && !this.constructionQueue.Contains(constructibleElement))
				{
					evaluableMessage_BuildingProduction.ResetState();
				}
				evaluableMessage_BuildingProduction.Tick(1f, num2);
			}
			IL_3D4:;
		}
	}

	private void GenerateBuildingMessages()
	{
		List<EvaluableMessage_BuildingProduction> list = new List<EvaluableMessage_BuildingProduction>();
		list.AddRange(this.aiEntityCity.Blackboard.GetMessages<EvaluableMessage_BuildingProduction>(BlackboardLayerID.City, (EvaluableMessage_BuildingProduction match) => match.CityGuid == this.aiEntityCity.City.GUID));
		EvaluationData<ConstructibleElement, InterpreterContext> evaluationData = null;
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			evaluationData = this.GetOrCreateEvaluationData(evaluationData);
		}
		if (this.constructibleElements != null)
		{
			Diagnostics.Assert(this.departmentOfTheTreasury != null);
			this.GenerateBuildingMessageFor(this.constructibleElements, ref list, ref evaluationData);
		}
		if (this.nationalBuildings != null)
		{
			this.GenerateBuildingMessageFor(this.nationalBuildings, ref list, ref evaluationData);
		}
		this.GeneratePointOfInterestBuildings(list, evaluationData);
		this.GenerateLocalCityBoosterMessage(ref list, ref evaluationData);
		this.GenerateBuildingMessageForExtension(ref list, ref evaluationData);
	}

	private void GeneratePointOfInterestBuildings(List<EvaluableMessage_BuildingProduction> buildingMessages, EvaluationData<ConstructibleElement, InterpreterContext> evaluationData)
	{
		if (this.pointOfInterestConstructibleElement == null)
		{
			return;
		}
		PointOfInterest[] pointOfInterests = this.aiEntityCity.City.Region.PointOfInterests;
		PointOfInterest chosenPointOfInterest = null;
		Diagnostics.Assert(this.departmentOfTheTreasury != null);
		for (int i = 0; i < this.pointOfInterestConstructibleElement.Length; i++)
		{
			DepartmentOfIndustry.ConstructibleElement constructibleElement = this.pointOfInterestConstructibleElement[i];
			if (DepartmentOfTheTreasury.CheckConstructiblePrerequisites(this.aiEntityCity.City, constructibleElement, new string[]
			{
				ConstructionFlags.Prerequisite
			}))
			{
				chosenPointOfInterest = null;
				foreach (PointOfInterest chosenPointOfInterest in pointOfInterests)
				{
					if (chosenPointOfInterest.WorldPosition.IsValid && !this.visibilityService.IsWorldPositionExploredFor(chosenPointOfInterest.WorldPosition, this.Empire))
					{
						chosenPointOfInterest = null;
					}
					else if (!(chosenPointOfInterest.PointOfInterestDefinition.PointOfInterestTemplate.Name == (constructibleElement as PointOfInterestImprovementDefinition).PointOfInterestTemplateName))
					{
						chosenPointOfInterest = null;
					}
					else
					{
						if (this.constructionQueue != null)
						{
							ReadOnlyCollection<Construction> pendingConstructions = this.constructionQueue.PendingConstructions;
							bool flag = pendingConstructions.Any((Construction construction) => construction.WorldPosition == chosenPointOfInterest.WorldPosition);
							if (flag)
							{
								chosenPointOfInterest = null;
								goto IL_1C4;
							}
						}
						if (chosenPointOfInterest.PointOfInterestImprovement != null)
						{
							chosenPointOfInterest = null;
						}
						else
						{
							if (chosenPointOfInterest.CreepingNodeImprovement == null)
							{
								break;
							}
							chosenPointOfInterest = null;
						}
					}
					IL_1C4:;
				}
				if (chosenPointOfInterest != null)
				{
					float num = this.decisionMaker.Evaluate(constructibleElement, evaluationData).Score;
					if (num < 0f)
					{
						num = 0f;
					}
					List<MissingResource> constructibleMissingRessources = this.departmentOfTheTreasury.GetConstructibleMissingRessources(this.aiEntityCity.City, constructibleElement);
					if (constructibleMissingRessources != null && constructibleMissingRessources.Count > 0)
					{
						AILayer_Trade.UpdateResourceNeed(1f, num, constructibleMissingRessources, constructibleElement.Name, this.aiEntityCity.Blackboard);
					}
					else
					{
						EvaluableMessage_BuildingProduction evaluableMessage_BuildingProduction = buildingMessages.Find((EvaluableMessage_BuildingProduction match) => match.ConstructibleElementName == constructibleElement.Name && match.BuildingPosition == chosenPointOfInterest.WorldPosition && match.State == BlackboardMessage.StateValue.Message_InProgress);
						if (evaluableMessage_BuildingProduction == null)
						{
							evaluableMessage_BuildingProduction = new EvaluableMessage_BuildingProduction(this.aiEntityCity.City.GUID, constructibleElement.Name, chosenPointOfInterest.WorldPosition, null, 1, AILayer_AccountManager.EconomyAccountName);
							base.AIEntity.AIPlayer.Blackboard.AddMessage(evaluableMessage_BuildingProduction);
						}
						else
						{
							EvaluableMessage.EvaluableMessageState evaluationState = evaluableMessage_BuildingProduction.EvaluationState;
							if (evaluationState == EvaluableMessage.EvaluableMessageState.Obtaining)
							{
								if (evaluableMessage_BuildingProduction.ChosenProductionEvaluation != null && !this.constructionQueue.Contains(constructibleElement))
								{
									evaluableMessage_BuildingProduction.ResetState();
								}
							}
						}
						evaluableMessage_BuildingProduction.Tick(1f, num);
					}
				}
			}
		}
	}

	private void GenerateLocalCityBoosterMessage(ref List<EvaluableMessage_BuildingProduction> buildingMessages, ref EvaluationData<ConstructibleElement, InterpreterContext> evaluationData)
	{
		EvaluableMessage_BuildingProduction buildingMessage = buildingMessages.Find((EvaluableMessage_BuildingProduction match) => match.IsLocalBooster && match.State == BlackboardMessage.StateValue.Message_InProgress);
		if (buildingMessage != null)
		{
			if (buildingMessage.EvaluationState == EvaluableMessage.EvaluableMessageState.Obtaining && buildingMessage.ChosenProductionEvaluation != null && this.constructionQueue.Contains((Construction match) => match.ConstructibleElementName == buildingMessage.ConstructibleElementName))
			{
				return;
			}
			buildingMessage.ResetState();
		}
		BoosterGeneratorDefinition boosterGeneratorDefinition = this.ChooseBoosterGenerator(ref evaluationData);
		if (boosterGeneratorDefinition != null)
		{
			if (buildingMessage == null)
			{
				buildingMessage = new EvaluableMessage_BuildingProduction(this.aiEntityCity.City.GUID, boosterGeneratorDefinition.Name, WorldPosition.Invalid, null, 1, AILayer_AccountManager.EconomyAccountName);
				buildingMessage.IsLocalBooster = true;
				base.AIEntity.AIPlayer.Blackboard.AddMessage(buildingMessage);
			}
			if (buildingMessage.EvaluationState != EvaluableMessage.EvaluableMessageState.Obtaining)
			{
				buildingMessage.UpdateConstructibleElementName(boosterGeneratorDefinition.Name);
			}
			buildingMessage.Tick(0.01f, 0.01f);
		}
		else if (buildingMessage != null)
		{
			buildingMessage.Cancel();
		}
	}

	private BoosterGeneratorDefinition ChooseBoosterGenerator(ref EvaluationData<ConstructibleElement, InterpreterContext> evaluationData)
	{
		IConstructibleElementAIHelper service = AIScheduler.Services.GetService<IConstructibleElementAIHelper>();
		List<BoosterGeneratorDefinition> list = new List<BoosterGeneratorDefinition>();
		service.FillWithValidBoosterGenerator(this.Empire, ref list);
		BoosterGeneratorDefinition result = null;
		float num = 0f;
		for (int i = 0; i < list.Count; i++)
		{
			float score = this.decisionMaker.Evaluate(list[i], evaluationData).Score;
			if (num < score)
			{
				num = score;
				result = list[i];
			}
		}
		return result;
	}

	public IEnumerable<IAIParameterConverter<InterpreterContext>> GetAIParameterConverters(StaticString aiParameterName)
	{
		Diagnostics.Assert(this.aiParameterConverterDatabase != null);
		AIParameterConverter aiParameterConverter;
		if (!this.aiParameterConverterDatabase.TryGetValue(aiParameterName, out aiParameterConverter))
		{
			yield break;
		}
		Diagnostics.Assert(aiParameterConverter != null);
		if (aiParameterConverter.ToAIParameters == null)
		{
			yield break;
		}
		for (int index = 0; index < aiParameterConverter.ToAIParameters.Length; index++)
		{
			yield return aiParameterConverter.ToAIParameters[index];
		}
		yield break;
	}

	public IEnumerable<IAIParameter<InterpreterContext>> GetAIParameters(ConstructibleElement element)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		if (this.currentWorldPositionScore != null)
		{
			IEnumerable<IAIParameter<InterpreterContext>> aiParameters = this.worldPositionEvaluationAIHelper.GetAIParameters(this.currentWorldPositionScore);
			if (aiParameters != null)
			{
				foreach (IAIParameter<InterpreterContext> aiParameter in aiParameters)
				{
					yield return aiParameter;
				}
			}
		}
		IEnumerable<IAIParameter<InterpreterContext>> parameters = this.constructibleElementAIEvaluationHelper.GetAIParameters(element);
		if (parameters == null)
		{
			yield break;
		}
		foreach (IAIParameter<InterpreterContext> aiParameter2 in parameters)
		{
			yield return aiParameter2;
		}
		yield break;
	}

	public IEnumerable<IAIParameter<InterpreterContext>> GetAIParameters(WorldPositionScore element)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		IEnumerable<IAIParameter<InterpreterContext>> aiParameters = this.worldPositionEvaluationAIHelper.GetAIParameters(this.currentWorldPositionScore);
		if (aiParameters != null)
		{
			foreach (IAIParameter<InterpreterContext> aiParameter in aiParameters)
			{
				yield return aiParameter;
			}
		}
		yield break;
	}

	public IEnumerable<IAIPrerequisite<InterpreterContext>> GetAIPrerequisites(ConstructibleElement constructibleElement)
	{
		if (constructibleElement == null)
		{
			throw new ArgumentNullException("constructibleElement");
		}
		if (constructibleElement.AIInfo == null || constructibleElement.AIInfo.AIPrerequisites == null)
		{
			yield break;
		}
		for (int index = 0; index < constructibleElement.AIInfo.AIPrerequisites.Length; index++)
		{
			yield return constructibleElement.AIInfo.AIPrerequisites[index];
		}
		yield break;
	}

	public IEnumerable<IAIPrerequisite<InterpreterContext>> GetAIPrerequisites(WorldPositionScore position)
	{
		yield break;
	}

	public override void ReadXml(XmlReader reader)
	{
		base.ReadXml(reader);
	}

	public override void WriteXml(XmlWriter writer)
	{
		base.WriteXml(writer);
	}

	[UtilityFunction("AICityApproval")]
	private static float UtilityFunc_CityApproval(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("CityAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float num = Mathf.Max(1f, aidata.GetAverageValue(SimulationProperties.NetCityApproval));
		float num2 = Mathf.Min(100f, num + aiParameterValue);
		float utility = num2 / num - 1f;
		return AILayer_Production.Normalize(debugContext, 0f, 0.5f, utility);
	}

	[UtilityFunction("AICityGrowth")]
	private static float UtilityFunc_CityGrowth(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("CityAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float num = Mathf.Max(1f, aidata.GetAverageValue(SimulationProperties.NetCityGrowth));
		float utility = aiParameterValue / num;
		return AILayer_Production.Normalize(debugContext, 0f, 1.2f, utility);
	}

	[UtilityFunction("AICityProduction")]
	private static float UtilityFunc_CityProduction(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("CityAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float num = Mathf.Max(1f, aidata.GetAverageValue(SimulationProperties.NetCityProduction));
		float utility = aiParameterValue / num;
		return AILayer_Production.Normalize(debugContext, 0f, 0.7f, utility);
	}

	[UtilityFunction("AICityResearch")]
	private static float UtilityFunc_CityResearch(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("CityAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float num = Mathf.Max(1f, aidata.GetAverageValue(SimulationProperties.NetCityResearch));
		float utility = aiParameterValue / num;
		return AILayer_Production.Normalize(debugContext, 0f, 2f, utility);
	}

	[UtilityFunction("AICityMoney")]
	private static float UtilityFunc_CityMoney(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("CityAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float num = Mathf.Max(1f, aidata.GetAverageValue(SimulationProperties.NetCityMoney));
		float utility = aiParameterValue / num;
		return AILayer_Production.Normalize(debugContext, 0f, 6f, utility);
	}

	[UtilityFunction("AICityEmpirePoint")]
	private static float UtilityFunc_CityEmpirePoint(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("CityAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float num = Mathf.Max(1f, aidata.GetAverageValue(SimulationProperties.NetCityEmpirePoint));
		float utility = aiParameterValue / num;
		return AILayer_Production.Normalize(debugContext, 0.35f, 4f, utility);
	}

	[UtilityFunction("AICityDefense")]
	private static float UtilityFunc_CityDefense(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("CityAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float num = Mathf.Max(1f, aidata.GetAverageValue(SimulationProperties.MaximumCityDefensePoint));
		float utility = aiParameterValue / num;
		return AILayer_Production.Normalize(debugContext, 0f, 1f, utility);
	}

	[UtilityFunction("AICityPillageDefense")]
	private static float UtilityFunc_CityPillageDefense(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("CityAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float num = Mathf.Max(1f, aidata.GetAverageValue(AILayer_Production.POIPillageDefense));
		float utility = aiParameterValue / num;
		return AILayer_Production.Normalize(debugContext, 0f, 1f, utility);
	}

	[UtilityFunction("AICityAntiSpy")]
	private static float UtilityFunc_CityAntiSpy(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("CityAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float num = Mathf.Max(1f, aidata.GetAverageValue(SimulationProperties.NetCityAntiSpy));
		float utility = aiParameterValue / num;
		return AILayer_Production.Normalize(debugContext, 0f, 1f, utility);
	}

	[UtilityFunction("AICityStrategicResource")]
	private static float UtilityFunc_CityStrategicResource(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("CityAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		DepartmentOfTheInterior agency = empire.GetAgency<DepartmentOfTheInterior>();
		float b = (float)agency.Cities.Count;
		float averageValue = aidata.GetAverageValue(AILayer_Production.EmpireNetStrategicResources);
		float value = averageValue / Mathf.Max(1f, b);
		float num = Mathf.Clamp(value, 1f, 5f);
		float utility = aiParameterValue / num;
		return AILayer_Production.Normalize(debugContext, 0f, 0.5f, utility);
	}

	[UtilityFunction("AICityLuxuryResource")]
	private static float UtilityFunc_CityLuxuryResource(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("CityAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		DepartmentOfTheInterior agency = empire.GetAgency<DepartmentOfTheInterior>();
		float b = (float)agency.Cities.Count;
		float averageValue = aidata.GetAverageValue(AILayer_Production.EmpireNetLuxuryResources);
		float value = averageValue / Mathf.Max(1f, b);
		float num = Mathf.Clamp(value, 1f, 5f);
		float utility = aiParameterValue / num;
		return AILayer_Production.Normalize(debugContext, 0f, 0.5f, utility);
	}

	[UtilityFunction("AICityMoneyUpkeep")]
	private static float UtilityFunc_CityMoneyUpkeep(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("CityAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float num = Mathf.Max(1f, aidata.GetAverageValue(AILayer_Production.EmpireCityMoney));
		float num2 = Mathf.Max(1f, aidata.GetAverageValue(AILayer_Production.EmpireCityMoneyUpkeep));
		float num3 = num2 / num;
		return aiParameterValue * num3;
	}

	[UtilityFunction("AIEmpireVision")]
	private static float UtilityFunc_EmpireVision(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("CityAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		float averageValue = aidata.GetAverageValue(AILayer_Production.FractionOfNeighbouringRegionsControlledByANonFriendlyEmpire);
		return aiParameterValue * averageValue;
	}

	[UtilityFunction("AIEmpireMilitaryPower")]
	private static float UtilityFunc_EmpireMilitaryPower(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("CityAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		DepartmentOfTheInterior agency = empire.GetAgency<DepartmentOfTheInterior>();
		float b = (float)agency.Cities.Count;
		float averageValue = aidata.GetAverageValue(AILayer_Production.EmpireMilitaryPower);
		float a = averageValue / Mathf.Max(1f, b);
		float averageValue2 = aidata.GetAverageValue(SimulationProperties.MilitaryPower);
		return aiParameterValue / Mathf.Max(a, averageValue2);
	}

	[UtilityFunction("AIEmpireWonderVictory")]
	private static float UtilityFunc_EmpireWonderVictory(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		AIData aidata = context.Get("CityAIData") as AIData;
		Diagnostics.Assert(aidata != null);
		return aiParameterValue;
	}

	[UtilityFunction("AIEmpireUnlockAltarOfAuriga")]
	private static float UtilityFunc_EmpireUnlockAltarOfAuriga(ConstructibleElement aiEvaluableElement, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		global::Empire empire = context.Get("Empire") as global::Empire;
		Diagnostics.Assert(empire != null);
		float propertyValue = empire.GetPropertyValue(SimulationProperties.OrbStock);
		float num = Mathf.Min(propertyValue, 50f) / 50f;
		num *= 2f * num;
		num *= num;
		return aiParameterValue * num;
	}

	private static float Normalize(AIHeuristicAnalyser.Context debugContext, float minimumUtilityValue, float maximumUtilityValue, float utility)
	{
		Diagnostics.Assert(maximumUtilityValue > minimumUtilityValue);
		utility = Mathf.Clamp01((utility - minimumUtilityValue) / (maximumUtilityValue - minimumUtilityValue));
		return utility;
	}

	protected global::Empire Empire
	{
		get
		{
			if (base.AIEntity == null)
			{
				return null;
			}
			return base.AIEntity.Empire;
		}
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		this.aiEntityCity = (aiEntity as AIEntity_City);
		this.personalityHelper = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		int idealNumberOfTurnsToTrackChanges = 8;
		int persistanceDuration = Mathf.RoundToInt((float)idealNumberOfTurnsToTrackChanges * base.AIEntity.Empire.SimulationObject.GetPropertyValue(SimulationProperties.GameSpeedMultiplier));
		this.cityAIData = new AIData(persistanceDuration);
		InterpreterContext interpreterContext = new InterpreterContext(this.aiEntityCity.City.SimulationObject);
		interpreterContext.Register("Empire", base.AIEntity.Empire);
		interpreterContext.Register("City", this.aiEntityCity.City);
		interpreterContext.Register("CityAIData", this.cityAIData);
		this.aiLayerBooster = base.AIEntity.GetLayer<AILayer_Booster>();
		AIEntity empireEntity = base.AIEntity.AIPlayer.AIEntities.Find((AIEntity match) => match is AIEntity_Empire);
		this.aiLayerAttitude = empireEntity.GetLayer<AILayer_Attitude>();
		IGameService gameService = Services.GetService<IGameService>();
		this.visibilityService = gameService.Game.Services.GetService<IVisibilityService>();
		this.gameEntityRepositoryService = gameService.Game.Services.GetService<IGameEntityRepositoryService>();
		this.worldPositionningService = gameService.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(AIScheduler.Services != null);
		this.constructibleElementAIEvaluationHelper = AIScheduler.Services.GetService<IConstructibleElementEvaluationAIHelper>();
		this.worldPositionEvaluationAIHelper = AIScheduler.Services.GetService<IWorldPositionEvaluationAIHelper>();
		this.entityAIHelper = AIScheduler.Services.GetService<IEntityInfoAIHelper>();
		this.decisionMaker = new ElementEvaluator<ConstructibleElement, InterpreterContext>(this, interpreterContext);
		this.decisionMaker.ContextWeightDelegate = new ElementEvaluator<ConstructibleElement, InterpreterContext>.ContextWeightFunc(this.ElementEvaluationContextWeightModifier);
		this.decisionMaker.FinalInterestDelegate = new ElementEvaluator<ConstructibleElement, InterpreterContext>.InterestFunc(this.ElementEvaluationScoreTransferFunction);
		this.departmentOfIndustry = this.Empire.GetAgency<DepartmentOfIndustry>();
		this.departmentOfTheTreasury = this.Empire.GetAgency<DepartmentOfTheTreasury>();
		this.departmentOfPlanificationAndDevelopment = this.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
		this.constructionQueue = this.departmentOfIndustry.GetConstructionQueue(this.aiEntityCity.City);
		this.aiParameterConverterDatabase = Databases.GetDatabase<AIParameterConverter>(false);
		this.constructibleElements = this.departmentOfIndustry.ConstructibleElementDatabase.GetAvailableConstructibleElements(new StaticString[]
		{
			CityImprovementDefinition.ReadOnlyCategory
		});
		this.nationalBuildings = this.departmentOfIndustry.ConstructibleElementDatabase.GetAvailableConstructibleElements(new StaticString[]
		{
			CityImprovementDefinition.ReadOnlyNationalCategory
		});
		this.pointOfInterestConstructibleElement = this.departmentOfIndustry.ConstructibleElementDatabase.GetAvailableConstructibleElements(new StaticString[]
		{
			PointOfInterestImprovementDefinition.ReadOnlyCategory
		});
		this.InitializeExtensions();
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "LayerProduction_CreateLocalNeedsPass", new AIEntity.AIAction(this.CreateLocalNeeds), this, new StaticString[]
		{
			"LayerAmasCity_CreateLocalNeedsPass",
			"LayerPopulation_CreateLocalNeedsPass"
		});
		base.AIEntity.RegisterPass(AIEntity.Passes.EvaluateNeeds.ToString(), "LayerProduction_EvaluateNeedsPass", new AIEntity.AIAction(this.EvaluateNeeds), this, new StaticString[]
		{
			"LayerPopulation_EvaluateNeedsPass"
		});
		base.AIEntity.RegisterPass(AIEntity.Passes.ExecuteNeeds.ToString(), "LayerProduction_ExecuteNeedsPass", new AIEntity.AIAction(this.ExecuteNeeds), this, new StaticString[]
		{
			"LayerPopulation_ExecuteNeedsPass",
			"AILayerArmyRecruitment_ExecuteNeedsPass"
		});
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI || (base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByHuman && !StaticString.IsNullOrEmpty(this.aiEntityCity.City.AdministrationSpeciality) && this.aiEntityCity.AICityState != null && this.aiEntityCity.AICityState.IsGuiCompliant);
	}

	public override void Release()
	{
		base.Release();
		this.aiEntityCity = null;
		this.constructibleElementAIEvaluationHelper = null;
		this.worldPositionEvaluationAIHelper = null;
		this.entityAIHelper = null;
		Diagnostics.Assert(this.candidateConstructibleElements != null);
		this.candidateConstructibleElements.Clear();
		this.decisionMaker = null;
		if (this.departmentOfIndustry != null)
		{
			this.departmentOfIndustry.OnConstructionChange -= this.DepartmentOfIndustry_OnConstructionChange;
			this.departmentOfIndustry = null;
		}
		this.departmentOfTheTreasury = null;
		this.departmentOfPlanificationAndDevelopment = null;
		this.departmentOfForeignAffairs = null;
		this.departmentOfTheInterior = null;
		this.gameEntityRepositoryService = null;
		this.visibilityService = null;
		this.pointOfInterestConstructibleElement = null;
		this.constructionQueue = null;
		this.constructibleElements = null;
		this.pointOfInterestConstructibleElement = null;
		this.LastResortDesigns = null;
		this.VictoryLayer = null;
		this.ColonizationLayer = null;
		this.worldAtlasHelper = null;
		this.NavyLayer = null;
		this.DiplomacyLayer = null;
	}

	protected override void CreateLocalNeeds(StaticString context, StaticString pass)
	{
		base.CreateLocalNeeds(context, pass);
		this.UpdateCityAIData();
		this.alreadyUsedPosition.Clear();
		for (int i = 0; i < this.constructionQueue.Length; i++)
		{
			Construction construction = this.constructionQueue.PeekAt(i);
			if (construction.WorldPosition.IsValid)
			{
				this.alreadyUsedPosition.Add(construction.WorldPosition);
			}
		}
		base.AIEntity.Context.InitializeElementEvaluator<ConstructibleElement, InterpreterContext>(AICityState.ProductionParameterModifier, typeof(AILayer_Production), this.decisionMaker);
		AmasCityDataMessage amasCityDataMessage = base.AIEntity.AIPlayer.Blackboard.GetMessages<AmasCityDataMessage>(BlackboardLayerID.City).FirstOrDefault((AmasCityDataMessage match) => match.CityGuid == this.aiEntityCity.City.GUID);
		Diagnostics.Assert(AILayer_Population.PopulationResource != null);
		Diagnostics.Assert(this.decisionMaker.Context != null);
		for (int j = 0; j < AILayer_Population.PopulationResource.Length; j++)
		{
			if (amasCityDataMessage != null && amasCityDataMessage.PopulationRepartitions != null && j < amasCityDataMessage.PopulationRepartitions.Length)
			{
				this.decisionMaker.Context.Register(AILayer_Population.PopulationResource[j], amasCityDataMessage.PopulationRepartitions[j]);
			}
			else
			{
				this.decisionMaker.Context.Unregister(AILayer_Population.PopulationResource[j]);
			}
		}
		if (amasCityDataMessage != null && amasCityDataMessage.ProductionWeights != null)
		{
			Diagnostics.Assert(AILayer_Production.AmasProductionWeightsModifierNames.Length == amasCityDataMessage.ProductionWeights.Length);
			Diagnostics.Assert(AILayer_Production.AmasProductionWeightsModifierNames != null);
			for (int k = 0; k < AILayer_Production.AmasProductionWeightsModifierNames.Length; k++)
			{
				base.AIEntity.Context.RegisterBoost(AICityState.ProductionParameterModifier, "AMAS", AILayer_Production.AmasProductionWeightsModifierNames[k], amasCityDataMessage.ProductionWeights[k], -1);
			}
		}
		else
		{
			for (int l = 0; l < AILayer_Production.AmasProductionWeightsModifierNames.Length; l++)
			{
				base.AIEntity.Context.RemoveBoost(AICityState.ProductionParameterModifier, "AMAS", AILayer_Production.AmasProductionWeightsModifierNames[l]);
			}
		}
		if (this.constructionQueue.Length > 0)
		{
			this.peekConstructibleCanBeDelay = false;
			if (this.constructionQueue != null)
			{
				ConstructibleElement constructibleElement = this.constructionQueue.Peek().ConstructibleElement;
				if (!DepartmentOfTheTreasury.CheckConstructiblePrerequisites(this.aiEntityCity.City, constructibleElement, new string[]
				{
					ConstructionFlags.Prerequisite
				}))
				{
					this.peekConstructibleCanBeDelay = true;
				}
				else if (constructibleElement is BoosterGeneratorDefinition)
				{
					this.peekConstructibleCanBeDelay = true;
				}
				else if ((this.aiEntityCity.City.BesiegingEmpireIndex >= 0 || (constructibleElement.SubCategory != "SubCategoryWonder" && constructibleElement.SubCategory != "SubCategoryDistrict" && constructibleElement.SubCategory != "SubCategoryDust" && constructibleElement.SubCategory != "SubCategoryVictory" && constructibleElement.SubCategory != "SubCategoryIndustry" && constructibleElement.SubCategory != "SubCategoryFIDS")) && !this.departmentOfTheTreasury.CheckConstructibleInstantCosts(this.aiEntityCity.City, constructibleElement))
				{
					this.peekConstructibleCanBeDelay = true;
				}
			}
			this.currentAvailableProduction = ((!this.peekConstructibleCanBeDelay) ? 0f : 1f);
		}
		else
		{
			this.currentAvailableProduction = 0f;
			if (!this.departmentOfTheTreasury.TryGetResourceStockValue(this.aiEntityCity.City, DepartmentOfTheTreasury.Resources.Production, out this.currentAvailableProduction, false))
			{
				this.currentAvailableProduction = 0f;
			}
			this.currentAvailableProduction += this.aiEntityCity.City.GetPropertyValue(SimulationProperties.NetCityProduction);
			this.currentAvailableProduction = Math.Max(1f, this.currentAvailableProduction);
		}
		this.GenerateBuildingMessages();
	}

	protected override void EvaluateNeeds(StaticString context, StaticString pass)
	{
		base.EvaluateNeeds(context, pass);
		this.CancelInvalidConstructions();
		this.NoMoreSettlers = false;
		if (this.VictoryLayer.CurrentVictoryDesign != AILayer_Victory.VictoryDesign.Settler && this.MaxSettlersNeeded(true) < 1)
		{
			this.NoMoreSettlers = true;
		}
		this.candidateConstructibleElements.Clear();
		bool flag = this.aiEntityCity.City.BesiegingEmpireIndex >= 0;
		float developmentRatioOfCity = this.entityAIHelper.GetDevelopmentRatioOfCity(this.aiEntityCity.City);
		float num = this.aiEntityCity.City.GetPropertyValue(SimulationProperties.NetCityProduction);
		if (num <= 0f)
		{
			num = 1f;
		}
		bool flag2 = this.departmentOfForeignAffairs.IsInWarWithSomeone();
		bool flag3 = false;
		if (flag2)
		{
			IGameService service = Services.GetService<IGameService>();
			GameServer gameServer = (Services.GetService<ISessionService>().Session as global::Session).GameServer as GameServer;
			global::Game game = service.Game as global::Game;
			for (int i = 0; i < game.Empires.Length; i++)
			{
				MajorEmpire majorEmpire = game.Empires[i] as MajorEmpire;
				if (majorEmpire == null)
				{
					break;
				}
				AIPlayer_MajorEmpire aiplayer_MajorEmpire;
				if (majorEmpire.Index != base.AIEntity.Empire.Index && this.departmentOfForeignAffairs.IsAtWarWith(majorEmpire) && gameServer.AIScheduler.TryGetMajorEmpireAIPlayer(majorEmpire, out aiplayer_MajorEmpire) && aiplayer_MajorEmpire.AIState == AIPlayer.PlayerState.EmpireControlledByHuman)
				{
					flag3 = true;
					break;
				}
			}
		}
		int num2 = 0;
		UnitDesign unitDesign = null;
		float num3 = 0f;
		float num4 = 0f;
		List<EvaluableMessage_BuildingProduction> list = new List<EvaluableMessage_BuildingProduction>();
		list.AddRange(this.aiEntityCity.Blackboard.GetMessages<EvaluableMessage_BuildingProduction>(BlackboardLayerID.City, (EvaluableMessage_BuildingProduction match) => match.CityGuid == this.aiEntityCity.City.GUID));
		float num5 = 0f;
		for (int j = 0; j < list.Count; j++)
		{
			if (num5 < list[j].Interest)
			{
				num5 = list[j].Interest;
			}
		}
		float propertyValue = this.Empire.GetPropertyValue(SimulationProperties.EmpireScaleFactor);
		float propertyValue2 = this.Empire.GetPropertyValue(SimulationProperties.LandMilitaryPower);
		float propertyValue3 = this.aiEntityCity.City.GetPropertyValue(SimulationProperties.NetCityApproval);
		foreach (EvaluableMessage_BuildingProduction evaluableMessage_BuildingProduction in list)
		{
			DepartmentOfIndustry.ConstructibleElement constructibleElement;
			if ((evaluableMessage_BuildingProduction.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending || evaluableMessage_BuildingProduction.EvaluationState == EvaluableMessage.EvaluableMessageState.Obtaining) && this.departmentOfIndustry.ConstructibleElementDatabase.TryGetValue(evaluableMessage_BuildingProduction.ConstructibleElementName, out constructibleElement))
			{
				float num6;
				float buyoutCost;
				this.GetProductionCost(evaluableMessage_BuildingProduction, constructibleElement, out num6, out buyoutCost);
				float num7 = evaluableMessage_BuildingProduction.Interest / num5 * 0.5f;
				string text = evaluableMessage_BuildingProduction.ConstructibleElementName.ToString();
				if (num7 > 0f)
				{
					num7 = AILayer.Boost(num7, this.ComputeCostBoost(num6 / num));
					num7 = AILayer.Boost(num7, this.ComputeSiegeBoostForEco());
					if (evaluableMessage_BuildingProduction.ConstructibleElementName == "DistrictAltarOfAuriga")
					{
						num7 = AILayer.Boost(num7, 0.4f);
					}
					if (!flag2 && text.Contains("Defense"))
					{
						num7 = AILayer.Boost(num7, -0.9f);
					}
					if (text == "CityImprovementIndustry0")
					{
						num7 = 1f;
						num2++;
					}
					else if (text.Contains("Industry"))
					{
						num7 = AILayer.Boost(num7, 0.4f);
						num2++;
					}
					if (propertyValue3 < 61f && text.Contains("Approval"))
					{
						num7 = AILayer.Boost(num7, 0.35f);
						num2++;
					}
					if (!flag3 && 0.2f * propertyValue2 < this.DiplomacyLayer.GetMilitaryPowerDif(false) && !flag && propertyValue <= (float)this.departmentOfDefense.Armies.Count && !text.Contains("BoosterGenerator"))
					{
						foreach (string value in new List<string>
						{
							"Food",
							"Industry",
							"Dust",
							"Science",
							"Influence",
							"ResourceExtractor",
							"Approval",
							"OrbUnlock"
						})
						{
							if (text.Contains(value))
							{
								num7 = AILayer.Boost(num7, 0.75f);
								num2++;
							}
						}
						if (text.Contains("District"))
						{
							if (this.aiEntityCity.AIDataCity.CityTileCount < 4)
							{
								num7 = AILayer.Boost(num7, 0.1f);
							}
							else
							{
								num7 = AILayer.Boost(num7, 0.5f);
							}
						}
						if (text.Contains("FIDS"))
						{
							num7 = AILayer.Boost(num7, 0.3f);
						}
					}
				}
				float economicalStress = 0f;
				this.ApplyProductionEvaluation(evaluableMessage_BuildingProduction, constructibleElement, num7, num, num6, buyoutCost, economicalStress);
				if (num7 > num4)
				{
					num4 = num7;
					num3 = num6;
				}
			}
		}
		ProductionNeedsMessage productionNeedsMessage = new ProductionNeedsMessage();
		productionNeedsMessage.State = BlackboardMessage.StateValue.Message_InProgress;
		productionNeedsMessage.TimeOut = 0;
		productionNeedsMessage.CityGuid = this.aiEntityCity.City.GUID;
		productionNeedsMessage.BestProductionCost = num3;
		productionNeedsMessage.BestProductionTurn = Mathf.CeilToInt(num3 / num);
		this.aiEntityCity.Blackboard.AddMessage(productionNeedsMessage);
		if (!this.BoostersInQueue(true))
		{
			List<EvaluableMessage_CityBooster> list2 = new List<EvaluableMessage_CityBooster>();
			list2.AddRange(this.aiEntityCity.Blackboard.GetMessages<EvaluableMessage_CityBooster>(BlackboardLayerID.Empire, (EvaluableMessage_CityBooster match) => match.CityGuid != this.aiEntityCity.City.GUID));
			foreach (EvaluableMessage_CityBooster evaluableMessage_CityBooster in list2)
			{
				DepartmentOfIndustry.ConstructibleElement constructibleElement2;
				if ((evaluableMessage_CityBooster.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending || evaluableMessage_CityBooster.EvaluationState == EvaluableMessage.EvaluableMessageState.Obtaining) && this.departmentOfIndustry.ConstructibleElementDatabase.TryGetValue(evaluableMessage_CityBooster.BoosterDefinitionGeneratorName, out constructibleElement2))
				{
					float priority = this.aiLayerBooster.GetPriority(evaluableMessage_CityBooster.BoosterDefinitionName);
					if (evaluableMessage_CityBooster.Interest > priority * 1.2f)
					{
						float num8;
						float buyoutCost2;
						this.GetProductionCost(evaluableMessage_CityBooster, constructibleElement2, out num8, out buyoutCost2);
						float num9 = evaluableMessage_CityBooster.Interest;
						num9 = AILayer.Boost(num9, this.ComputeCostBoost(num8 / num));
						num9 = AILayer.Boost(num9, this.ComputeSiegeBoostForEco());
						num9 = AILayer.Boost(num9, this.ComputeBoosterBoost(evaluableMessage_CityBooster));
						float num10 = 0.2f;
						num10 = AILayer.Boost(num10, num4 * 0.5f);
						if (num2 > 0)
						{
							num10 = AILayer.Boost(num10, 1f);
							num9 = AILayer.Boost(num9, -1f);
						}
						this.ApplyProductionEvaluation(evaluableMessage_CityBooster, constructibleElement2, num9, num, num8, buyoutCost2, num10);
					}
				}
			}
		}
		List<string> list3 = new List<string>();
		this.GetForbiddenResources(ref list3);
		List<EvaluableMessageWithUnitDesign> list4 = new List<EvaluableMessageWithUnitDesign>();
		list4.AddRange(this.aiEntityCity.Blackboard.GetMessages<EvaluableMessageWithUnitDesign>(BlackboardLayerID.Empire));
		foreach (EvaluableMessageWithUnitDesign evaluableMessageWithUnitDesign in list4)
		{
			if ((evaluableMessageWithUnitDesign.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending || evaluableMessageWithUnitDesign.EvaluationState == EvaluableMessage.EvaluableMessageState.Obtaining) && evaluableMessageWithUnitDesign.UnitDesign != null)
			{
				if (!this.departmentOfDefense.UnitDesignDatabase.TryGetValue(evaluableMessageWithUnitDesign.UnitDesign.Model, out unitDesign, false))
				{
					AILayer.LogWarning("Cannot found the unit design with model {0}. The unit request will failed.", new object[]
					{
						evaluableMessageWithUnitDesign.UnitDesign.Model
					});
					evaluableMessageWithUnitDesign.SetFailedToObtain();
				}
				else
				{
					bool flag4 = true;
					EvaluableMessage_UnitRequest evaluableMessage_UnitRequest = evaluableMessageWithUnitDesign as EvaluableMessage_UnitRequest;
					if (evaluableMessage_UnitRequest != null && evaluableMessage_UnitRequest.RequestUnitListMessageID != 0UL)
					{
						RequestUnitListMessage requestUnitListMessage = base.AIEntity.AIPlayer.Blackboard.GetMessage(evaluableMessage_UnitRequest.RequestUnitListMessageID) as RequestUnitListMessage;
						if (requestUnitListMessage != null && (flag || requestUnitListMessage.ForceSourceRegion != -1) && requestUnitListMessage.ForceSourceRegion != this.aiEntityCity.City.Region.Index)
						{
							flag4 = false;
						}
					}
					if (flag4 && !this.departmentOfIndustry.CheckConstructiblePrerequisites(this.aiEntityCity.City, unitDesign))
					{
						flag4 = false;
					}
					if (!flag4)
					{
						for (int k = 0; k < evaluableMessageWithUnitDesign.ProductionEvaluations.Count; k++)
						{
							if (evaluableMessageWithUnitDesign.ProductionEvaluations[k].CityGuid == this.aiEntityCity.City.GUID)
							{
								evaluableMessageWithUnitDesign.ProductionEvaluations.RemoveAt(k);
								k--;
							}
						}
					}
					else
					{
						float num11;
						float buyoutCost3;
						this.GetProductionCost(evaluableMessageWithUnitDesign, unitDesign, out num11, out buyoutCost3);
						float num12 = evaluableMessageWithUnitDesign.Interest;
						num12 = AILayer.Boost(num12, this.ComputeCostBoost(num11 / num));
						num12 = AILayer.Boost(num12, this.ComputeDistanceToObjectiveBoost(evaluableMessageWithUnitDesign, unitDesign));
						num12 = AILayer.Boost(num12, this.ComputeEconomicBoostForUnit(evaluableMessageWithUnitDesign, developmentRatioOfCity));
						if (unitDesign.Tags.Contains(DownloadableContent9.TagColossus))
						{
							num12 = AILayer.Boost(num12, this.colossusProductionBoost);
						}
						float num13 = 0f;
						if (!flag)
						{
							num13 = 0.5f;
							num13 = AILayer.Boost(num13, (this.minimalDevelopmentRatioForUnit - developmentRatioOfCity) / this.minimalDevelopmentRatioForUnit * this.maximalDevelopmentRatioBoost);
						}
						else if (developmentRatioOfCity < this.minimalDevelopmentRatioForUnit)
						{
							num13 = (1f - (developmentRatioOfCity - this.minimalDevelopmentRatioForUnit) / this.minimalDevelopmentRatioForUnit) * this.maximalDevelopmentRatioBoost;
						}
						if (unitDesign.CheckUnitAbility(UnitAbility.ReadonlyColonize, -1))
						{
							num13 = AILayer.Boost(num13, this.settlerEconomicalStress);
						}
						if (developmentRatioOfCity >= 0.9f)
						{
							num13 = AILayer.Boost(num13, -0.5f + (developmentRatioOfCity - 0.9f) / 0.1f * -0.4f);
						}
						if (flag3)
						{
							num13 = AILayer.Boost(num13, -0.3f);
						}
						if (this.aiEntityCity.AICityState != null)
						{
							num13 = AILayer.Boost(num13, -this.aiEntityCity.AICityState.UnitBoost);
						}
						num13 = AILayer.Boost(num13, -this.ComputeCostBoost(num11 / num));
						num13 = AILayer.Boost(num13, num4 * 0.2f);
						if (this.ArmyThresholdTurns == 0)
						{
							this.ArmyThresholdTurns = 15;
						}
						int num14 = (Services.GetService<IGameService>().Game as global::Game).Turn / this.ArmyThresholdTurns + 1;
						if (!unitDesign.CheckUnitAbility(UnitAbility.ReadonlyColonize, -1) && !flag2 && !flag && this.aiEntityCity.AIDataCity.CityTileCount > 1 && this.departmentOfDefense.Armies.Count > num14 && num2 > 0)
						{
							num13 = AILayer.Boost(num13, 0.6f);
						}
						foreach (string x in list3)
						{
							if (DepartmentOfTheTreasury.GetProductionCostWithBonus(this.aiEntityCity.City, unitDesign, x) > 0f)
							{
								num12 = 0f;
								num13 = 1f;
								break;
							}
						}
						if (unitDesign.CheckUnitAbility(UnitAbility.ReadonlyColonize, -1))
						{
							if (this.NoMoreSettlers)
							{
								num13 = 1f;
								num12 = 0f;
							}
							else if (this.VictoryLayer.CurrentVictoryDesign == AILayer_Victory.VictoryDesign.Settler && this.ColonizationLayer.CurrentSettlerCount < 10)
							{
								num13 = 0f;
								num12 = 1f;
							}
						}
						else if (unitDesign.Tags.Contains(UnitDesign.TagSeafaring) && !this.worldAtlasHelper.IsRegionPacified(base.AIEntity.Empire.Index, this.aiEntityCity.City.Region.Index))
						{
							num13 = 1f;
							num12 = 0f;
						}
						this.ApplyProductionEvaluation(evaluableMessageWithUnitDesign, unitDesign, num12, num, num11, buyoutCost3, num13);
					}
				}
			}
		}
		List<EvaluableMessage_Wonder> list5 = new List<EvaluableMessage_Wonder>();
		list5.AddRange(this.aiEntityCity.Blackboard.GetMessages<EvaluableMessage_Wonder>(BlackboardLayerID.Empire));
		EvaluationData<ConstructibleElement, InterpreterContext> evaluationData = null;
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			evaluationData = this.GetOrCreateEvaluationData(evaluationData);
		}
		foreach (EvaluableMessage_Wonder evaluableMessage_Wonder in list5)
		{
			if ((evaluableMessage_Wonder.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending || evaluableMessage_Wonder.EvaluationState == EvaluableMessage.EvaluableMessageState.Obtaining) && !StaticString.IsNullOrEmpty(evaluableMessage_Wonder.ConstructibleElementName))
			{
				DepartmentOfIndustry.ConstructibleElement constructibleElement3;
				if (!this.departmentOfIndustry.ConstructibleElementDatabase.TryGetValue(evaluableMessage_Wonder.ConstructibleElementName, out constructibleElement3))
				{
					AILayer.LogWarning("Cannot found the wonder {0}. The wonder request will failed.", new object[]
					{
						evaluableMessage_Wonder.ConstructibleElementName
					});
					evaluableMessage_Wonder.SetFailedToObtain();
				}
				else
				{
					bool flag5 = this.aiEntityCity.City.BesiegingEmpire == null;
					if (flag5 && !this.departmentOfIndustry.CheckConstructiblePrerequisites(this.aiEntityCity.City, constructibleElement3))
					{
						flag5 = false;
					}
					float num15 = 0f;
					WorldPositionScore extensionBestPosition = this.GetExtensionBestPosition(constructibleElement3.Name);
					if (flag5 && extensionBestPosition == null)
					{
						flag5 = false;
					}
					if (!flag5)
					{
						for (int l = 0; l < evaluableMessage_Wonder.ProductionEvaluations.Count; l++)
						{
							if (evaluableMessage_Wonder.ProductionEvaluations[l].CityGuid == this.aiEntityCity.City.GUID)
							{
								evaluableMessage_Wonder.ProductionEvaluations.RemoveAt(l);
								l--;
							}
						}
					}
					else
					{
						num15 = Mathf.Clamp01(num15 / 2f);
						float num16;
						float buyoutCost4;
						this.GetProductionCost(evaluableMessage_Wonder, constructibleElement3, out num16, out buyoutCost4);
						num15 = AILayer.Boost(num15, this.ComputeCostBoost(num16 / num) * 0.7f);
						num15 = AILayer.Boost(num15, 0.2f);
						num15 = AILayer.Boost(num15, evaluableMessage_Wonder.Interest - 0.5f);
						float num17 = 0.5f;
						num17 = AILayer.Boost(num17, -num15 * 0.5f);
						if (0.6f * propertyValue2 < this.DiplomacyLayer.GetMilitaryPowerDif(false) && this.aiEntityCity.AIDataCity.CityTileCount > 2)
						{
							num15 = AILayer.Boost(num15, 0.4f);
							num17 = AILayer.Boost(num17, -0.3f);
							if ((this.Empire.SimulationObject.Tags.Contains("FactionTraitCultists9") || base.AIEntity.Empire.SimulationObject.Tags.Contains("FactionTraitMimics1")) && evaluableMessage_Wonder.ConstructibleElementName.ToString().Contains("DistrictWonder2"))
							{
								num15 = AILayer.Boost(num15, 0.99f);
								num17 = 0f;
							}
						}
						else if (this.aiEntityCity.AIDataCity.CityTileCount < 2)
						{
							num17 = 1f;
							num15 = 0f;
						}
						this.ApplyProductionEvaluation(evaluableMessage_Wonder, constructibleElement3, num15, num, num16, buyoutCost4, num17);
					}
				}
			}
		}
		List<EvaluableMessage_GolemCamp> list6 = new List<EvaluableMessage_GolemCamp>();
		list6.AddRange(this.aiEntityCity.Blackboard.GetMessages<EvaluableMessage_GolemCamp>(BlackboardLayerID.Empire));
		foreach (EvaluableMessage_GolemCamp evaluableMessage_GolemCamp in list6)
		{
			if ((evaluableMessage_GolemCamp.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending || evaluableMessage_GolemCamp.EvaluationState == EvaluableMessage.EvaluableMessageState.Obtaining) && !StaticString.IsNullOrEmpty(evaluableMessage_GolemCamp.ConstructibleElementName))
			{
				DepartmentOfIndustry.ConstructibleElement constructibleElement4;
				if (!this.departmentOfIndustry.ConstructibleElementDatabase.TryGetValue(evaluableMessage_GolemCamp.ConstructibleElementName, out constructibleElement4))
				{
					AILayer.LogWarning("Cannot found the camp {0}. The camp request will fail.", new object[]
					{
						evaluableMessage_GolemCamp.ConstructibleElementName
					});
					evaluableMessage_GolemCamp.SetFailedToObtain();
				}
				else
				{
					bool flag6 = this.aiEntityCity.City.BesiegingEmpire == null;
					if (flag6 && !this.departmentOfIndustry.CheckConstructiblePrerequisites(this.aiEntityCity.City, constructibleElement4))
					{
						flag6 = false;
					}
					float num18 = 0f;
					WorldPosition availableCampPosition = this.GetAvailableCampPosition();
					if (flag6 && availableCampPosition == WorldPosition.Invalid)
					{
						flag6 = false;
					}
					if (!flag6)
					{
						for (int m = 0; m < evaluableMessage_GolemCamp.ProductionEvaluations.Count; m++)
						{
							if (evaluableMessage_GolemCamp.ProductionEvaluations[m].CityGuid == this.aiEntityCity.City.GUID)
							{
								evaluableMessage_GolemCamp.ProductionEvaluations.RemoveAt(m);
								m--;
							}
						}
					}
					else
					{
						num18 = Mathf.Clamp01(num18 / 2f);
						float num19;
						float buyoutCost5;
						this.GetProductionCost(evaluableMessage_GolemCamp, constructibleElement4, out num19, out buyoutCost5);
						num18 = AILayer.Boost(num18, this.ComputeCostBoost(num19 / num) * 0.7f);
						num18 = AILayer.Boost(num18, 0.2f);
						num18 = AILayer.Boost(num18, evaluableMessage_GolemCamp.Interest - 0.5f);
						float num20 = 0.5f;
						num20 = AILayer.Boost(num20, -num18 * 0.5f);
						this.ApplyProductionEvaluation(evaluableMessage_GolemCamp, constructibleElement4, num18, num, num19, buyoutCost5, num20);
					}
				}
			}
		}
	}

	protected override void ExecuteNeeds(StaticString context, StaticString pass)
	{
		base.ExecuteNeeds(context, pass);
		Diagnostics.Assert(this.aiEntityCity != null && this.aiEntityCity.Blackboard != null);
		ISynchronousJobRepositoryAIHelper service = AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>();
		this.DelayedTicks = 0;
		this.boosterOnCity = false;
		service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_ExecuteNeeds_Delayed));
	}

	private EvaluationData<ConstructibleElement, InterpreterContext> GetOrCreateEvaluationData(EvaluationData<ConstructibleElement, InterpreterContext> evaluationData)
	{
		if (!Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			return null;
		}
		IGameService service = Services.GetService<IGameService>();
		int turn = (service.Game as global::Game).Turn;
		if (this.DecisionMakerEvaluationDataHistoric.Count > 0 && this.DecisionMakerEvaluationDataHistoric[this.DecisionMakerEvaluationDataHistoric.Count - 1].Turn == turn)
		{
			evaluationData = this.DecisionMakerEvaluationDataHistoric[this.DecisionMakerEvaluationDataHistoric.Count - 1];
		}
		else
		{
			evaluationData = new EvaluationData<ConstructibleElement, InterpreterContext>();
			evaluationData.Turn = turn;
			this.DecisionMakerEvaluationDataHistoric.Add(evaluationData);
		}
		return evaluationData;
	}

	private void ApplyProductionEvaluation(EvaluableMessage evaluableMessage, ConstructibleElement element, float productionScore, float productionPerTurn, float productionCost, float buyoutCost, float economicalStress)
	{
		int num = Mathf.CeilToInt(productionCost / productionPerTurn);
		if (evaluableMessage.EvaluationState != EvaluableMessage.EvaluableMessageState.Obtaining)
		{
			evaluableMessage.UpdateProductionEvaluation("Production", this.aiEntityCity.City.GUID, productionScore, productionCost, num, economicalStress);
		}
		if (num > 1)
		{
			evaluableMessage.UpdateBuyEvaluation("Production", this.aiEntityCity.City.GUID, buyoutCost, num, 0f, 0UL);
		}
		else
		{
			evaluableMessage.CancelBuyEvaluation("Production", this.aiEntityCity.City.GUID);
		}
	}

	private float ComputeBoosterBoost(EvaluableMessage_CityBooster cityBoosterMessage)
	{
		float priority = this.aiLayerBooster.GetPriority(cityBoosterMessage.BoosterDefinitionName);
		float num = priority - cityBoosterMessage.Interest;
		if (num < 0.1f)
		{
			return -1f;
		}
		return num;
	}

	private float ComputeCostBoost(float elementTurnDuration)
	{
		float num;
		if (elementTurnDuration < this.minimalTurnDuration)
		{
			num = -1f + elementTurnDuration / this.minimalTurnDuration;
		}
		else
		{
			elementTurnDuration -= this.minimalTurnDuration;
			if (elementTurnDuration < 0f)
			{
				elementTurnDuration = 0f;
			}
			num = elementTurnDuration / this.maximalTurnDuration;
			num = Mathf.Sqrt(num);
		}
		if (num > 1f)
		{
			num = 1f;
		}
		return this.maximalTurnDurationBoost * num;
	}

	private float ComputeDistanceToObjectiveBoost(EvaluableMessageWithUnitDesign unitDesignRequest, UnitDesign unitDesign)
	{
		EvaluableMessage_UnitRequest evaluableMessage_UnitRequest = unitDesignRequest as EvaluableMessage_UnitRequest;
		if (evaluableMessage_UnitRequest == null || evaluableMessage_UnitRequest.RequestUnitListMessageID == 0UL)
		{
			return 0f;
		}
		RequestUnitListMessage requestUnitListMessage = base.AIEntity.AIPlayer.Blackboard.GetMessage(evaluableMessage_UnitRequest.RequestUnitListMessageID) as RequestUnitListMessage;
		if (requestUnitListMessage == null)
		{
			return 0f;
		}
		if (!requestUnitListMessage.FinalPosition.IsValid)
		{
			return 0f;
		}
		float num = (float)this.worldPositionningService.GetDistance(requestUnitListMessage.FinalPosition, this.aiEntityCity.City.WorldPosition);
		float num2 = 0f;
		if (unitDesign.Context != null)
		{
			num2 = unitDesign.Context.GetPropertyValue(SimulationProperties.MaximumMovement);
		}
		if (num2 <= 0f)
		{
			num2 = 2f;
		}
		float num3 = num / num2;
		float num4 = Mathf.Min(1f, num3 / this.maximalUnitDistance);
		return num4 * this.maximalUnitDistanceBoost;
	}

	private float ComputeEconomicBoostForUnit(EvaluableMessageWithUnitDesign unitRequest, float developmentRatio)
	{
		float num = developmentRatio - this.minimalDevelopmentRatioForUnit;
		if (float.IsNaN(num) || num < -1f || num > 1f)
		{
			AILayer.Log("Boost invalid {0} = {1} - {2}", new object[]
			{
				num,
				developmentRatio,
				this.minimalDevelopmentRatioForUnit
			});
		}
		return num;
	}

	private float ComputeSiegeBoostForEco()
	{
		bool flag = this.aiEntityCity.City.BesiegingEmpire != null;
		if (flag)
		{
			return -0.5f;
		}
		return 0f;
	}

	[SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1513:ClosingCurlyBracketMustBeFollowedByBlankLine", Justification = "For AI heuristic scope we want to have EndScope call just after the closing bracket.")]
	private float ElementEvaluationContextWeightModifier(ConstructibleElement aiEvaluableElement, InterpreterContext context, StaticString outputName, AIHeuristicAnalyser.Context debugContext)
	{
		float result = 0f;
		this.ComputeCityContextWeightModifier(ref result, outputName, debugContext);
		return result;
	}

	[SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1513:ClosingCurlyBracketMustBeFollowedByBlankLine", Justification = "For AI heuristic scope we want to have EndScope call just after the closing bracket.")]
	private void ComputeCityContextWeightModifier(ref float contextModifier, StaticString outputName, AIHeuristicAnalyser.Context debugContext)
	{
		float modifierValueUnnormalized = base.AIEntity.Context.GetModifierValueUnnormalized(AICityState.ProductionParameterModifier, outputName);
		contextModifier += modifierValueUnnormalized;
		float num = 0f;
		num = this.ComputeAttitudeBoostValue(outputName, debugContext, num);
		contextModifier = AILayer.Boost(contextModifier, num);
		float num2 = 1f;
		string regitryPath = "AI/MajorEmpire/AIEntity_City/AILayer_Production/ElementEvaluatorContextMultiplier/" + outputName;
		num2 = this.personalityHelper.GetRegistryValue<float>(base.AIEntity.Empire, regitryPath, num2);
		contextModifier *= num2;
	}

	private float ComputeAttitudeBoostValue(StaticString outputName, AIHeuristicAnalyser.Context debugContext, float attitudeBoost)
	{
		if (outputName == "AICityPillageDefense")
		{
			float attitudeValue = this.GetAttitudeValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.PillageSucceed, debugContext);
			float attitudeValue2 = this.GetAttitudeValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.Pillaging, debugContext);
			attitudeBoost = Mathf.Max(attitudeValue, attitudeValue2);
		}
		else if (outputName == "AICityLuxuryResource" || outputName == "AICityStrategicResource")
		{
			float attitudeValue3 = this.GetAttitudeValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.PillageSucceed, debugContext);
			float attitudeValue4 = this.GetAttitudeValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.Pillaging, debugContext);
			attitudeBoost = -Mathf.Max(attitudeValue3, attitudeValue4);
		}
		else if (outputName == "AICityAntiSpy")
		{
			attitudeBoost = this.GetAttitudeValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.Spy, debugContext);
		}
		else if (outputName == "AIEmpireVision")
		{
			float attitudeValue5 = this.GetAttitudeValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.PillageSucceed, debugContext);
			float attitudeValue6 = this.GetAttitudeValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.Pillaging, debugContext);
			float attitudeValue7 = this.GetAttitudeValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.Spy, debugContext);
			attitudeBoost = Mathf.Max(new float[]
			{
				attitudeValue5,
				attitudeValue6,
				attitudeValue7
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

	private float ElementEvaluationScoreTransferFunction(ConstructibleElement aiEvaluableElement, InterpreterContext context, float score, AIHeuristicAnalyser.Context debugContext)
	{
		score = Mathf.Clamp01(score);
		PopulationConstructionCost populationConstructionCost = Array.Find<IConstructionCost>(aiEvaluableElement.Costs, (IConstructionCost match) => match is PopulationConstructionCost) as PopulationConstructionCost;
		if (populationConstructionCost != null)
		{
			float propertyValue = this.aiEntityCity.City.GetPropertyValue(SimulationProperties.Population);
			if (propertyValue < populationConstructionCost.PopulationValue + (float)this.aiEntityCity.AICityState.NetPopulationForSettlerPreRequisit)
			{
				return -1f;
			}
			if (this.constructionQueue.Contains((Construction match) => match.ConstructibleElement is UnitDesign && (match.ConstructibleElement as UnitDesign).CheckUnitAbility(UnitAbility.ReadonlyColonize, -1)))
			{
				return -1f;
			}
		}
		score = this.ElementEvaluationScoreTransferFunction(this.currentWorldPositionScore, context, score, debugContext);
		score = Mathf.Clamp01(score);
		return score;
	}

	private void GetProductionCost(EvaluableMessage evaluableMessage, ConstructibleElement constructibleElement, out float productionCost, out float buyoutCost)
	{
		productionCost = DepartmentOfTheTreasury.GetProductionCostWithBonus(this.aiEntityCity.City, constructibleElement, DepartmentOfTheTreasury.Resources.Production);
		buyoutCost = DepartmentOfTheTreasury.GetBuyoutCostWithBonus(this.aiEntityCity.City, constructibleElement);
		IGameEntity gameEntity;
		if (evaluableMessage.EvaluationState == EvaluableMessage.EvaluableMessageState.Obtaining && this.gameEntityRepositoryService.TryGetValue(evaluableMessage.ElementGuid, out gameEntity) && gameEntity is Construction)
		{
			productionCost -= (gameEntity as Construction).GetSpecificConstructionStock(DepartmentOfTheTreasury.Resources.Production);
			buyoutCost = DepartmentOfTheTreasury.GetBuyoutCostWithBonus(this.aiEntityCity.City, gameEntity as Construction);
		}
	}

	private void OrderBuyout_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		OrderBuyoutConstruction order = e.Order as OrderBuyoutConstruction;
		IEnumerable<EvaluableMessage_ResourceNeed> messages = base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_ResourceNeed>(BlackboardLayerID.Empire, (EvaluableMessage_ResourceNeed match) => match.ElementGuid == order.ConstructionGameEntityGUID);
		if (messages != null)
		{
			foreach (EvaluableMessage_ResourceNeed evaluableMessage_ResourceNeed in messages)
			{
				if (e.Result == PostOrderResponse.Processed)
				{
					evaluableMessage_ResourceNeed.SetObtained();
				}
				else if (evaluableMessage_ResourceNeed.EvaluationState != EvaluableMessage.EvaluableMessageState.Obtaining)
				{
					evaluableMessage_ResourceNeed.SetFailedToObtain();
				}
			}
		}
	}

	private void OrderQueue_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		OrderQueueConstruction orderQueueConstruction = e.Order as OrderQueueConstruction;
		ulong messageID;
		if (ulong.TryParse(orderQueueConstruction.Tag, out messageID))
		{
			EvaluableMessage evaluableMessage = base.AIEntity.AIPlayer.Blackboard.GetMessage(messageID) as EvaluableMessage;
			if (evaluableMessage == null)
			{
				Diagnostics.Assert(false, "Could not retreive message matching with order");
				return;
			}
			if (e.Result == PostOrderResponse.Processed)
			{
				evaluableMessage.SetBeingObtained(orderQueueConstruction.ConstructionGameEntityGUID);
			}
			else if (evaluableMessage.EvaluationState != EvaluableMessage.EvaluableMessageState.Obtaining)
			{
				evaluableMessage.SetFailedToObtain();
			}
		}
		else
		{
			Diagnostics.Assert(false, "Could not parse order Tag");
		}
	}

	private void OrderQueueAndBuy_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		this.OrderQueue_TicketRaised(sender, e);
		if (e.Result == PostOrderResponse.Processed)
		{
			OrderQueueConstruction order = e.Order as OrderQueueConstruction;
			Construction construction = this.constructionQueue.Get((Construction match) => match.GUID == order.ConstructionGameEntityGUID);
			if (construction == null || construction.IsBuyout || construction.ConstructibleElement.Tags.Contains(ConstructibleElement.TagNoBuyout))
			{
				ulong messageID;
				if (ulong.TryParse(order.Tag, out messageID))
				{
					EvaluableMessage evaluableMessage = base.AIEntity.AIPlayer.Blackboard.GetMessage(messageID) as EvaluableMessage;
					if (evaluableMessage != null)
					{
						evaluableMessage.SetFailedToObtain();
					}
				}
				return;
			}
			OrderBuyoutConstruction order2 = new OrderBuyoutConstruction(this.Empire.Index, this.aiEntityCity.City.GUID, order.ConstructionGameEntityGUID);
			Ticket ticket;
			this.Empire.PlayerControllers.AI.PostOrder(order2, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderBuyout_TicketRaised));
		}
	}

	private float ParseEvaluableMessages(List<EvaluableMessage> evaluableMessages, bool insert = false)
	{
		UnitDesign element2 = null;
		DepartmentOfIndustry.ConstructibleElement element = null;
		Func<EvaluableMessage_BuildingProduction, bool> <>9__1;
		for (int i = 0; i < evaluableMessages.Count; i++)
		{
			bool flag = false;
			EvaluableMessage evaluableMessage = evaluableMessages[i];
			bool flag2 = evaluableMessage.ChosenBuyEvaluation != null && evaluableMessage.ChosenBuyEvaluation.CityGuid == this.aiEntityCity.City.GUID && evaluableMessage.ChosenBuyEvaluation.LayerTag == "Production";
			bool flag3 = evaluableMessage.ChosenProductionEvaluation != null && evaluableMessage.ChosenProductionEvaluation.CityGuid == this.aiEntityCity.City.GUID && evaluableMessage.ChosenProductionEvaluation.LayerTag == "Production";
			if (flag2 || flag3)
			{
				WorldPosition worldPosition = WorldPosition.Invalid;
				if (evaluableMessage is EvaluableMessageWithUnitDesign)
				{
					EvaluableMessageWithUnitDesign evaluableMessageWithUnitDesign = evaluableMessage as EvaluableMessageWithUnitDesign;
					if (evaluableMessageWithUnitDesign.UnitDesign == null)
					{
						goto IL_774;
					}
					if (!this.departmentOfDefense.UnitDesignDatabase.TryGetValue(evaluableMessageWithUnitDesign.UnitDesign.Model, out element2, false))
					{
						evaluableMessage.SetFailedToObtain();
						goto IL_774;
					}
					if (evaluableMessageWithUnitDesign.UnitDesign.CheckUnitAbility(UnitAbility.ReadonlyColonize, -1) && this.NoMoreSettlers)
					{
						evaluableMessage.SetFailedToObtain();
						goto IL_774;
					}
					element = element2;
					if (element.Tags.Contains(DownloadableContent9.TagColossus) || element.Tags.Contains(DownloadableContent9.TagSolitary))
					{
						WorldPositionScore worldPositionScore = this.DefineBestPositionForUnit();
						if (worldPositionScore == null)
						{
							evaluableMessage.SetFailedToObtain();
							goto IL_774;
						}
						worldPosition = worldPositionScore.WorldPosition;
						if (this.alreadyUsedPosition.Contains(worldPosition))
						{
							evaluableMessage.SetFailedToObtain();
							goto IL_774;
						}
						this.ReserveExtensionPosition(worldPosition);
					}
					else if (element.Tags.Contains(DownloadableContent16.TagSeafaring))
					{
						worldPosition = this.aiEntityCity.City.DryDockPosition;
					}
				}
				else
				{
					if (evaluableMessage is EvaluableMessage_BuildingProduction)
					{
						flag = (evaluableMessage as EvaluableMessage_BuildingProduction).ConstructibleElementName.ToString().Contains("BoosterGenerator");
						if (!this.departmentOfIndustry.ConstructibleElementDatabase.TryGetValue((evaluableMessage as EvaluableMessage_BuildingProduction).ConstructibleElementName, out element))
						{
							evaluableMessage.SetFailedToObtain();
							goto IL_774;
						}
						worldPosition = (evaluableMessage as EvaluableMessage_BuildingProduction).BuildingPosition;
						if (this.alreadyUsedPosition.Contains(worldPosition))
						{
							evaluableMessage.SetFailedToObtain();
							goto IL_774;
						}
						this.ReserveExtensionPosition(worldPosition);
						if (!element.Descriptors.Any((SimulationDescriptor match) => match.Name == AILayer_Production.OnlyOneConstructionPerEmpire || match.Name == AILayer_Production.OnlyOnePerEmpire))
						{
							goto IL_502;
						}
						Blackboard<BlackboardLayerID, BlackboardMessage> blackboard = base.AIEntity.AIPlayer.Blackboard;
						BlackboardLayerID blackboardLayerID = BlackboardLayerID.City;
						BlackboardLayerID layerID = blackboardLayerID;
						Func<EvaluableMessage_BuildingProduction, bool> filter;
						if ((filter = <>9__1) == null)
						{
							filter = (<>9__1 = ((EvaluableMessage_BuildingProduction match) => match.ConstructibleElementName == element.Name && match.CityGuid != this.aiEntityCity.City.GUID));
						}
						using (IEnumerator<EvaluableMessage_BuildingProduction> enumerator = blackboard.GetMessages<EvaluableMessage_BuildingProduction>(layerID, filter).GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								EvaluableMessage_BuildingProduction evaluableMessage_BuildingProduction = enumerator.Current;
								evaluableMessage_BuildingProduction.Cancel();
							}
							goto IL_502;
						}
					}
					if (evaluableMessage is EvaluableMessage_CityBooster)
					{
						flag = true;
						if (!this.departmentOfIndustry.ConstructibleElementDatabase.TryGetValue((evaluableMessage as EvaluableMessage_CityBooster).BoosterDefinitionGeneratorName, out element))
						{
							evaluableMessage.SetFailedToObtain();
							goto IL_774;
						}
					}
					else if (evaluableMessage is EvaluableMessage_Wonder)
					{
						EvaluableMessage_Wonder evaluableMessage_Wonder = evaluableMessage as EvaluableMessage_Wonder;
						if (!this.departmentOfIndustry.ConstructibleElementDatabase.TryGetValue(evaluableMessage_Wonder.ConstructibleElementName, out element))
						{
							evaluableMessage.SetFailedToObtain();
							goto IL_774;
						}
						WorldPositionScore extensionBestPosition = this.GetExtensionBestPosition(evaluableMessage_Wonder.ConstructibleElementName);
						if (extensionBestPosition == null)
						{
							evaluableMessage.SetFailedToObtain();
							goto IL_774;
						}
						worldPosition = extensionBestPosition.WorldPosition;
						if (this.alreadyUsedPosition.Contains(worldPosition))
						{
							evaluableMessage.SetFailedToObtain();
							goto IL_774;
						}
						this.ReserveExtensionPosition(worldPosition);
					}
					else if (evaluableMessage is EvaluableMessage_GolemCamp)
					{
						EvaluableMessage_GolemCamp evaluableMessage_GolemCamp = evaluableMessage as EvaluableMessage_GolemCamp;
						if (!this.departmentOfIndustry.ConstructibleElementDatabase.TryGetValue(evaluableMessage_GolemCamp.ConstructibleElementName, out element))
						{
							evaluableMessage.SetFailedToObtain();
							goto IL_774;
						}
						WorldPosition availableCampPosition = this.GetAvailableCampPosition();
						if (availableCampPosition == WorldPosition.Invalid)
						{
							evaluableMessage.SetFailedToObtain();
							goto IL_774;
						}
						worldPosition = availableCampPosition;
						if (this.alreadyUsedPosition.Contains(worldPosition))
						{
							evaluableMessage.SetFailedToObtain();
							goto IL_774;
						}
						this.ReserveExtensionPosition(worldPosition);
					}
				}
				IL_502:
				if (flag2)
				{
					if (evaluableMessage.EvaluationState != EvaluableMessage.EvaluableMessageState.Obtaining)
					{
						OrderQueueConstruction orderQueueConstruction = new OrderQueueConstruction(this.Empire.Index, this.aiEntityCity.City.GUID, element, worldPosition, evaluableMessage.ID.ToString());
						orderQueueConstruction.InsertAtFirstPlace = (insert && !flag);
						Ticket ticket;
						this.Empire.PlayerControllers.AI.PostOrder(orderQueueConstruction, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderQueueAndBuy_TicketRaised));
					}
					else if (evaluableMessage.ElementGuid.IsValid)
					{
						Construction construction = this.constructionQueue.Get((Construction match) => match.GUID == evaluableMessage.ElementGuid);
						if (construction != null && !construction.IsBuyout && !construction.ConstructibleElement.Tags.Contains(ConstructibleElement.TagNoBuyout))
						{
							OrderBuyoutConstruction order = new OrderBuyoutConstruction(this.Empire.Index, this.aiEntityCity.City.GUID, evaluableMessage.ElementGuid);
							Ticket ticket2;
							this.Empire.PlayerControllers.AI.PostOrder(order, out ticket2, new EventHandler<TicketRaisedEventArgs>(this.OrderBuyout_TicketRaised));
						}
					}
				}
				else if (flag3 && evaluableMessage.EvaluationState != EvaluableMessage.EvaluableMessageState.Obtaining && this.currentAvailableProduction > 0f && DepartmentOfTheTreasury.CheckConstructiblePrerequisites(this.aiEntityCity.City, element, new string[]
				{
					ConstructionFlags.Prerequisite
				}))
				{
					if (flag && (this.BoostersInQueue(false) || this.boosterOnCity))
					{
						evaluableMessage.SetFailedToObtain();
					}
					else
					{
						OrderQueueConstruction orderQueueConstruction2 = new OrderQueueConstruction(this.Empire.Index, this.aiEntityCity.City.GUID, element, worldPosition, evaluableMessage.ID.ToString());
						orderQueueConstruction2.InsertAtFirstPlace = (insert && !flag);
						Ticket ticket3;
						this.Empire.PlayerControllers.AI.PostOrder(orderQueueConstruction2, out ticket3, new EventHandler<TicketRaisedEventArgs>(this.OrderQueue_TicketRaised));
						this.currentAvailableProduction -= DepartmentOfTheTreasury.GetProductionCostWithBonus(this.aiEntityCity.City, element, DepartmentOfTheTreasury.Resources.Production);
					}
				}
			}
			IL_774:;
		}
		return this.currentAvailableProduction;
	}

	private SynchronousJobState SynchronousJob_ExecuteNeeds()
	{
		if (this.aiEntityCity == null || this.aiEntityCity.City == null)
		{
			return SynchronousJobState.Failure;
		}
		List<EvaluableMessage> list = new List<EvaluableMessage>();
		if (this.currentAvailableProduction > 0f)
		{
			list.Clear();
			this.aiEntityCity.Blackboard.FillMessages<EvaluableMessage>(BlackboardLayerID.City, (EvaluableMessage match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate && match.ChosenBuyEvaluation == null, ref list);
			this.aiEntityCity.Blackboard.FillMessages<EvaluableMessage>(BlackboardLayerID.Empire, (EvaluableMessage match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate && match.ChosenBuyEvaluation == null, ref list);
			this.ParseEvaluableMessages(list, this.peekConstructibleCanBeDelay);
		}
		else if (this.constructionQueue != null && this.constructionQueue.Length == 1)
		{
			Construction construction = this.constructionQueue.Peek();
			ConstructibleElement constructibleElement = construction.ConstructibleElement;
			float productionCostWithBonus = DepartmentOfTheTreasury.GetProductionCostWithBonus(this.aiEntityCity.City, constructibleElement, DepartmentOfTheTreasury.Resources.Production);
			float specificConstructionStock = construction.GetSpecificConstructionStock(DepartmentOfTheTreasury.Resources.Production);
			float num = 0f;
			if (!this.departmentOfTheTreasury.TryGetNetResourceValue(this.aiEntityCity.City, DepartmentOfTheTreasury.Resources.Production, out num, false))
			{
				num = 1f;
			}
			if ((productionCostWithBonus - specificConstructionStock) / num > 5f)
			{
				EvaluableMessage_BuildingProduction evaluableMessage_BuildingProduction = base.AIEntity.AIPlayer.Blackboard.FindFirst<EvaluableMessage_BuildingProduction>(BlackboardLayerID.City, (EvaluableMessage_BuildingProduction match) => match.CityGuid == this.aiEntityCity.City.GUID && match.ConstructibleElementName == constructibleElement.Name);
				if (evaluableMessage_BuildingProduction != null)
				{
					float num2 = 0f;
					if (evaluableMessage_BuildingProduction.ChosenProductionEvaluation != null)
					{
						num2 = evaluableMessage_BuildingProduction.ChosenProductionEvaluation.ProductionFinalScore;
					}
					else
					{
						for (int i = 0; i < evaluableMessage_BuildingProduction.ProductionEvaluations.Count; i++)
						{
							if (num2 < evaluableMessage_BuildingProduction.ProductionEvaluations[i].ProductionFinalScore)
							{
								num2 = evaluableMessage_BuildingProduction.ProductionEvaluations[i].ProductionFinalScore;
							}
						}
					}
					num2 *= 2f;
					list.Clear();
					this.aiEntityCity.Blackboard.FillMessages<EvaluableMessage>(BlackboardLayerID.City, (EvaluableMessage match) => match is EvaluableMessage_BuildingProduction && match.ChosenBuyEvaluation == null && (match as EvaluableMessage_BuildingProduction).CityGuid == this.aiEntityCity.City.GUID && match.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending, ref list);
					bool flag = false;
					for (int j = 0; j < list.Count; j++)
					{
						EvaluableMessage evaluableMessage = list[j];
						if (evaluableMessage.ProductionEvaluations.Count == 1 && evaluableMessage.ProductionEvaluations[0].ProductionFinalScore > num2)
						{
							evaluableMessage.ValidateProductionEvaluation(evaluableMessage.ProductionEvaluations[0]);
							flag = true;
						}
					}
					if (flag)
					{
						this.currentAvailableProduction = 1f;
						this.ParseEvaluableMessages(list, this.peekConstructibleCanBeDelay);
					}
				}
			}
		}
		if (this.currentAvailableProduction > 0f)
		{
			list.Clear();
			this.aiEntityCity.Blackboard.FillMessages<EvaluableMessage>(BlackboardLayerID.City, (EvaluableMessage match) => match is EvaluableMessage_BuildingProduction && match.ChosenBuyEvaluation == null && (match as EvaluableMessage_BuildingProduction).CityGuid == this.aiEntityCity.City.GUID && match.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending, ref list);
			for (int k = 0; k < list.Count; k++)
			{
				EvaluableMessage evaluableMessage2 = list[k];
				if (evaluableMessage2.ProductionEvaluations.Count == 1)
				{
					evaluableMessage2.ValidateProductionEvaluation(evaluableMessage2.ProductionEvaluations[0]);
				}
			}
			list.Sort((EvaluableMessage left, EvaluableMessage right) => -1 * left.ChosenProductionEvaluation.ProductionFinalScore.CompareTo(right.ChosenProductionEvaluation.ProductionFinalScore));
			this.ParseEvaluableMessages(list, this.peekConstructibleCanBeDelay);
		}
		list.Clear();
		this.aiEntityCity.Blackboard.FillMessages<EvaluableMessage>(BlackboardLayerID.City, (EvaluableMessage match) => match.ChosenBuyEvaluation != null && match.ChosenBuyEvaluation.CityGuid == this.aiEntityCity.City.GUID && (match.EvaluationState == EvaluableMessage.EvaluableMessageState.Obtaining || match.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate), ref list);
		this.aiEntityCity.Blackboard.FillMessages<EvaluableMessage>(BlackboardLayerID.Empire, (EvaluableMessage match) => match.ChosenBuyEvaluation != null && (match.EvaluationState == EvaluableMessage.EvaluableMessageState.Obtaining || match.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate), ref list);
		this.ParseEvaluableMessages(list, false);
		return SynchronousJobState.Success;
	}

	private float ComputeExtensionAlignementFactor(WorldPositionScore aiEvaluableElement)
	{
		if (this.IsInLineWithExtensionPlanning(aiEvaluableElement.WorldPosition))
		{
			return 0.2f;
		}
		return -0.4f;
	}

	private bool IsInLineWithExtensionPlanning(WorldPosition worldPosition)
	{
		WorldOrientation currentExtensionOrientation = this.aiEntityCity.AIDataCity.CurrentExtensionOrientation;
		WorldTransform worldTransform = new WorldTransform(this.aiEntityCity.City.WorldPosition, currentExtensionOrientation);
		WorldPosition worldPosition2 = worldTransform.WorldToLocal(worldPosition);
		bool result = false;
		if (worldPosition2.Row == 0)
		{
			result = true;
		}
		else if (worldPosition2.Row == 1)
		{
			result = true;
		}
		return result;
	}

	private WorldPosition GetAvailableCampPosition()
	{
		WorldPosition result = WorldPosition.Invalid;
		if (this.aiEntityCity.City == null)
		{
			return result;
		}
		List<WorldPosition> list = new List<WorldPosition>();
		this.FilterCampPositions(this.aiEntityCity.City.Region, out list);
		if (list.Count > 0)
		{
			IWorldPositionningService service = Services.GetService<IGameService>().Game.Services.GetService<IWorldPositionningService>();
			int num = 0;
			List<WorldPositionScore> list2 = new List<WorldPositionScore>();
			foreach (WorldPosition worldPosition in list)
			{
				if (!this.alreadyUsedPosition.Contains(worldPosition))
				{
					int distance = service.GetDistance(this.aiEntityCity.City.WorldPosition, worldPosition);
					if (distance > num)
					{
						num = distance;
					}
					list2.Add(AIScheduler.Services.GetService<IWorldPositionEvaluationAIHelper>().GetWorldPositionExpansionScore(base.AIEntity.Empire, this.aiEntityCity.City, worldPosition));
				}
			}
			if (num > 6)
			{
				num = 6;
			}
			float num2 = 0f;
			for (int i = 0; i < list2.Count; i++)
			{
				float num3 = 0f;
				foreach (AIParameterDefinition aiparameterDefinition in list2[i].Scores)
				{
					if (aiparameterDefinition.Name == "CityApproval")
					{
						num3 += aiparameterDefinition.Value * 0.2f;
					}
					else
					{
						num3 += aiparameterDefinition.Value;
					}
				}
				float num4 = (float)service.GetDistance(this.aiEntityCity.City.WorldPosition, list2[i].WorldPosition) / (float)num;
				if (num4 > 1f)
				{
					num4 = 1f;
				}
				num3 = num3 * 0.6f + num4 * 0.4f;
				if (num3 > num2)
				{
					num2 = num3;
					result = list2[i].WorldPosition;
				}
			}
		}
		return result;
	}

	private void FilterCampPositions(Region region, out List<WorldPosition> positions)
	{
		positions = new List<WorldPosition>();
		WorldPosition[] worldPositions = region.WorldPositions;
		for (int i = 0; i < worldPositions.Length; i++)
		{
			int bits = 1 << region.City.Empire.Index;
			if (this.worldPositionningService.IsConstructible(worldPositions[i], WorldPositionning.PreventsDistrictTypeExtensionConstruction, bits) && this.worldPositionningService.GetPointOfInterest(worldPositions[i]) == null)
			{
				for (int j = 0; j < region.City.Districts.Count; j++)
				{
					if (!(region.City.Districts[j].WorldPosition == worldPositions[i]) || region.City.Districts[j].Type != DistrictType.Exploitation)
					{
						positions.Add(worldPositions[i]);
					}
				}
			}
		}
	}

	private SynchronousJobState SynchronousJob_ExecuteNeeds_Delayed()
	{
		if (this.DelayedTicks == 0)
		{
			if (this.SynchronousJob_ExecuteNeeds() == SynchronousJobState.Failure)
			{
				return SynchronousJobState.Failure;
			}
			this.DelayedTicks++;
			return SynchronousJobState.Running;
		}
		else
		{
			if (this.aiEntityCity == null || this.aiEntityCity.City == null || this.aiEntityCity.City.IsInfected || base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByHuman)
			{
				return SynchronousJobState.Failure;
			}
			List<EvaluableMessage> list = new List<EvaluableMessage>();
			ConstructionQueue constructionQueue = base.AIEntity.Empire.GetAgency<DepartmentOfIndustry>().GetConstructionQueue(this.aiEntityCity.City);
			if (constructionQueue == null)
			{
				return SynchronousJobState.Failure;
			}
			float num = 0f;
			if (!this.departmentOfTheTreasury.TryGetResourceStockValue(this.aiEntityCity.City, DepartmentOfTheTreasury.Resources.Production, out num, false))
			{
				num = 0f;
			}
			num += this.aiEntityCity.City.GetPropertyValue(SimulationProperties.NetCityProduction);
			num = Math.Max(1f, num);
			float num2 = num;
			for (int i = 0; i < constructionQueue.Length; i++)
			{
				Construction construction = constructionQueue.PeekAt(i);
				if (this.aiEntityCity.City.BesiegingEmpireIndex >= 0 && !(construction.ConstructibleElement is UnitDesign))
				{
					break;
				}
				float productionCostWithBonus = DepartmentOfTheTreasury.GetProductionCostWithBonus(this.aiEntityCity.City, construction.ConstructibleElement, "Production");
				float num3 = 0f;
				int j = 0;
				while (j < construction.CurrentConstructionStock.Length)
				{
					if (construction.CurrentConstructionStock[j].PropertyName == "Production")
					{
						num3 += construction.CurrentConstructionStock[j].Stock;
						if (construction.IsBuyout)
						{
							num3 = productionCostWithBonus;
							break;
						}
						break;
					}
					else
					{
						j++;
					}
				}
				if (i == 0 && construction.ConstructibleElement.SubCategory == "SubCategoryVictory" && (productionCostWithBonus - num3) / this.aiEntityCity.City.GetPropertyValue(SimulationProperties.NetCityProduction) > 5f)
				{
					this.OrderLastResortCityBuilding(true);
					return SynchronousJobState.Success;
				}
				float num4 = productionCostWithBonus - num3;
				num -= num4;
				if (num < 0f)
				{
					break;
				}
			}
			this.currentAvailableProduction = num;
			if (this.aiEntityCity.City.BesiegingEmpireIndex >= 0)
			{
				if (this.currentAvailableProduction > 0f)
				{
					this.OrderLastResortUnit();
				}
				return SynchronousJobState.Success;
			}
			List<CityBoosterNeeds> list2 = new List<CityBoosterNeeds>();
			list2.AddRange(base.AIEntity.AIPlayer.Blackboard.GetMessages<CityBoosterNeeds>(BlackboardLayerID.Empire, (CityBoosterNeeds match) => match.CityGuid == this.aiEntityCity.City.GUID));
			bool flag = false;
			foreach (CityBoosterNeeds cityBoosterNeeds in list2)
			{
				if (cityBoosterNeeds.BoosterDefinitionName == "BoosterIndustry" && cityBoosterNeeds.AvailabilityState == CityBoosterNeeds.CityBoosterState.Success)
				{
					flag = true;
					this.boosterOnCity = true;
					break;
				}
			}
			this.constructionQueue = this.departmentOfIndustry.GetConstructionQueue(this.aiEntityCity.City);
			if (this.constructionQueue.Length == 0)
			{
				flag = true;
			}
			if (this.currentAvailableProduction <= 0f && !flag)
			{
				return SynchronousJobState.Success;
			}
			if (flag && this.currentAvailableProduction <= 0f && this.DelayedTicks == 1)
			{
				this.DelayedTicks++;
				return SynchronousJobState.Running;
			}
			if (this.currentAvailableProduction > 0f && (flag || num2 == this.currentAvailableProduction))
			{
				list.Clear();
				this.aiEntityCity.Blackboard.FillMessages<EvaluableMessage>(BlackboardLayerID.City, (EvaluableMessage match) => match is EvaluableMessage_BuildingProduction && match.ChosenBuyEvaluation == null && (match as EvaluableMessage_BuildingProduction).CityGuid == this.aiEntityCity.City.GUID && match.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending, ref list);
				this.aiEntityCity.Blackboard.FillMessages<EvaluableMessage>(BlackboardLayerID.City, (EvaluableMessage match) => match is EvaluableMessage_BuildingProduction && match.ChosenBuyEvaluation == null && (match as EvaluableMessage_BuildingProduction).CityGuid == this.aiEntityCity.City.GUID && match.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending, ref list);
				this.aiEntityCity.Blackboard.FillMessages<EvaluableMessage>(BlackboardLayerID.City, (EvaluableMessage match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate && match.ChosenBuyEvaluation == null, ref list);
				this.aiEntityCity.Blackboard.FillMessages<EvaluableMessage>(BlackboardLayerID.Empire, (EvaluableMessage match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate && match.ChosenBuyEvaluation == null, ref list);
				this.aiEntityCity.Blackboard.FillMessages<EvaluableMessage>(BlackboardLayerID.City, (EvaluableMessage match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending && match.ChosenBuyEvaluation == null, ref list);
				if (list.Count > 0)
				{
					for (int k = 0; k < list.Count; k++)
					{
						EvaluableMessage evaluableMessage = list[k];
						if (evaluableMessage.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending && evaluableMessage.ProductionEvaluations.Count == 1)
						{
							evaluableMessage.ValidateProductionEvaluation(evaluableMessage.ProductionEvaluations[0]);
						}
					}
					for (int l = list.Count - 1; l >= 0; l--)
					{
						if (list[l].EvaluationState == EvaluableMessage.EvaluableMessageState.Pending || list[l].ChosenProductionEvaluation == null || list[l].ChosenProductionEvaluation.CityGuid != this.aiEntityCity.City.GUID)
						{
							list.RemoveAt(l);
						}
					}
					list.Sort((EvaluableMessage left, EvaluableMessage right) => -1 * left.ChosenProductionEvaluation.ProductionFinalScore.CompareTo(right.ChosenProductionEvaluation.ProductionFinalScore));
					this.ParseEvaluableMessages(list, false);
				}
				if (flag)
				{
					if (this.aiEntityCity.City.BesiegingEmpire == null)
					{
						this.OrderLastResortCityBuilding(false);
					}
					float num5 = 0f;
					if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpireMoney, out num5, false))
					{
						num5 = 0f;
					}
					if (this.DelayedTicks > 1 && this.currentAvailableProduction > 0f && (base.AIEntity.Empire.GetPropertyValue(SimulationProperties.NetEmpireMoney) > 4f || num5 > 300f))
					{
						this.OrderLastResortUnit();
					}
				}
			}
			if (this.DelayedTicks > 1)
			{
				return SynchronousJobState.Success;
			}
			this.DelayedTicks++;
			return SynchronousJobState.Running;
		}
	}

	private void OrderLastResortUnit()
	{
		if (!this.PreventFurtherUnits())
		{
			List<string> list = new List<string>();
			this.GetForbiddenResources(ref list);
			List<UnitDesign> list2 = new List<UnitDesign>();
			foreach (DepartmentOfIndustry.ConstructibleElement constructibleElement in this.departmentOfIndustry.ConstructibleElementDatabase.GetAvailableConstructibleElements(new StaticString[]
			{
				UnitDesign.ReadOnlyCategory
			}))
			{
				if (DepartmentOfTheTreasury.CheckConstructiblePrerequisites(this.aiEntityCity.City, constructibleElement, new string[]
				{
					ConstructionFlags.Prerequisite
				}))
				{
					UnitDesign unitDesign = constructibleElement as UnitDesign;
					if (unitDesign != null && (!unitDesign.CheckUnitAbility(UnitAbility.ReadonlyColonize, -1) || (this.VictoryLayer.CurrentVictoryDesign == AILayer_Victory.VictoryDesign.Settler && this.ColonizationLayer.CurrentSettlerCount < 10)) && !unitDesign.CheckUnitAbility(UnitAbility.ReadonlyResettle, -1) && !unitDesign.CheckUnitAbility("UnitAbilityLowDamage", -1) && !unitDesign.Tags.Contains(DownloadableContent9.TagColossus) && unitDesign.Context != null && unitDesign.Context.GetPropertyValue(SimulationProperties.MilitaryPower) > 0f && (!unitDesign.Name.ToString().Contains("Preacher") || this.VictoryLayer.CurrentVictoryDesign == AILayer_Victory.VictoryDesign.Preacher))
					{
						bool flag = false;
						foreach (string x in list)
						{
							if (DepartmentOfTheTreasury.GetProductionCostWithBonus(this.aiEntityCity.City, unitDesign, x) > 0f)
							{
								flag = true;
								break;
							}
						}
						if (!flag)
						{
							list2.Add(unitDesign);
						}
					}
				}
			}
			if (list2.Count < 1)
			{
				return;
			}
			list2.Sort((UnitDesign left, UnitDesign right) => -1 * left.Context.GetPropertyValue(SimulationProperties.MilitaryPower).CompareTo(right.Context.GetPropertyValue(SimulationProperties.MilitaryPower)));
			this.LastResortDesigns = new List<DepartmentOfIndustry.ConstructibleElement>();
			this.LastResortDesignIndex = 0;
			foreach (string x2 in new List<string>
			{
				"UnitAbilityHighRanged",
				"UnitAbilityRanged",
				"UnitAbilityShortRanged"
			})
			{
				for (int j = 0; j < list2.Count; j++)
				{
					if (list2[j].CheckUnitAbility(x2, -1) && !list2[j].Tags.Contains(UnitDesign.TagSeafaring) && list2[j].UnitBodyDefinition.SubCategory != "SubCategorySupport")
					{
						this.LastResortDesigns.Add(list2[j]);
						list2.RemoveAt(j);
						j--;
					}
				}
			}
			for (int k = 0; k < list2.Count; k++)
			{
				if (!list2[k].Tags.Contains(UnitDesign.TagSeafaring))
				{
					this.LastResortDesigns.AddOnce(list2[k]);
					list2.RemoveAt(k);
					k--;
				}
			}
			if (this.NavyLayer.NavyImportance.Value > 0.01f)
			{
				for (int l = 0; l < list2.Count; l++)
				{
					this.LastResortDesigns.AddOnce(list2[l]);
				}
			}
			for (int m = 0; m < this.LastResortDesigns.Count; m++)
			{
				DepartmentOfIndustry.ConstructibleElement constructibleElement2 = this.LastResortDesigns[m];
				if (constructibleElement2 != null && this.currentAvailableProduction > 0f && this.departmentOfIndustry.CheckConstructiblePrerequisites(this.aiEntityCity.City, constructibleElement2))
				{
					OrderQueueConstruction order = new OrderQueueConstruction(this.Empire.Index, this.aiEntityCity.City.GUID, constructibleElement2, string.Empty);
					this.firsttry = true;
					Ticket ticket;
					this.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderLastResortUnit_TicketRaised));
					return;
				}
				this.LastResortDesignIndex++;
			}
			return;
		}
		else
		{
			this.LastResortDesigns = new List<DepartmentOfIndustry.ConstructibleElement>();
			List<string> list3 = new List<string>
			{
				"BoosterGeneratorScienceBonus1",
				"BoosterGeneratorScienceBonus2"
			};
			if (base.AIEntity.Empire.GetAgency<DepartmentOfScience>().CurrentTechnologyEraNumber < 6)
			{
				list3.Add("BoosterGeneratorFoodBonus1");
				list3.Add("BoosterGeneratorFoodBonus2");
			}
			foreach (DepartmentOfIndustry.ConstructibleElement constructibleElement3 in this.departmentOfIndustry.ConstructibleElementDatabase.GetAvailableConstructibleElements(new StaticString[]
			{
				BoosterGeneratorDefinition.ReadOnlyCategory
			}))
			{
				if (DepartmentOfTheTreasury.CheckConstructiblePrerequisites(this.aiEntityCity.City, constructibleElement3, new string[]
				{
					ConstructionFlags.Prerequisite
				}))
				{
					foreach (string value in list3)
					{
						if (constructibleElement3.ToString().Contains(value))
						{
							this.LastResortDesigns.Add(constructibleElement3);
							break;
						}
					}
				}
			}
			if (this.LastResortDesigns.Count < 1)
			{
				return;
			}
			this.LastResortDesignIndex = this.random.Next(this.LastResortDesigns.Count);
			OrderQueueConstruction order2 = new OrderQueueConstruction(this.Empire.Index, this.aiEntityCity.City.GUID, this.LastResortDesigns[this.LastResortDesignIndex], string.Empty);
			Ticket ticket2;
			this.Empire.PlayerControllers.AI.PostOrder(order2, out ticket2, new EventHandler<TicketRaisedEventArgs>(this.OrderLastResortUnit_TicketRaised));
			return;
		}
	}

	private void OrderLastResortUnit_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		OrderQueueConstruction orderQueueConstruction = e.Order as OrderQueueConstruction;
		if (e.Result != PostOrderResponse.Processed)
		{
			if (e.Result == PostOrderResponse.PreprocessHasFailed)
			{
				if (this.firsttry)
				{
					this.firsttry = false;
				}
				if (this.LastResortDesigns.Count > this.LastResortDesignIndex)
				{
					this.LastResortDesigns.RemoveAt(this.LastResortDesignIndex);
				}
				if (this.LastResortDesigns.Count < 1)
				{
					List<string> list = new List<string>
					{
						"BoosterGeneratorScienceBonus1",
						"BoosterGeneratorScienceBonus2"
					};
					if (base.AIEntity.Empire.GetAgency<DepartmentOfScience>().CurrentTechnologyEraNumber < 6)
					{
						list.Add("BoosterGeneratorFoodBonus1");
						list.Add("BoosterGeneratorFoodBonus2");
					}
					foreach (DepartmentOfIndustry.ConstructibleElement constructibleElement in this.departmentOfIndustry.ConstructibleElementDatabase.GetAvailableConstructibleElements(new StaticString[]
					{
						BoosterGeneratorDefinition.ReadOnlyCategory
					}))
					{
						if (DepartmentOfTheTreasury.CheckConstructiblePrerequisites(this.aiEntityCity.City, constructibleElement, new string[]
						{
							ConstructionFlags.Prerequisite
						}))
						{
							foreach (string value in list)
							{
								if (constructibleElement.ToString().Contains(value))
								{
									this.LastResortDesigns.Add(constructibleElement);
									break;
								}
							}
						}
					}
					if (this.LastResortDesigns.Count < 1)
					{
						this.OrderLastResortUnit_TicketRaised_PrioritizeUnits();
						return;
					}
				}
				this.LastResortDesignIndex = this.random.Next(this.LastResortDesigns.Count);
				OrderQueueConstruction order = new OrderQueueConstruction(this.Empire.Index, this.aiEntityCity.City.GUID, this.LastResortDesigns[this.LastResortDesignIndex], string.Empty);
				Ticket ticket;
				this.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderLastResortUnit_TicketRaised));
			}
			this.OrderLastResortUnit_TicketRaised_PrioritizeUnits();
			return;
		}
		float productionCostWithBonus = DepartmentOfTheTreasury.GetProductionCostWithBonus(this.aiEntityCity.City, this.LastResortDesigns[this.LastResortDesignIndex], "Production");
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.Log("ELCP Empire {0} City {1} ordering lastresort Unit {2}, currentAvailableProduction {3}/{5} - {4} ", new object[]
			{
				base.AIEntity.Empire.ToString(),
				this.aiEntityCity.City.LocalizedName,
				orderQueueConstruction.ConstructibleElementName,
				this.aiEntityCity.City.GetPropertyValue(SimulationProperties.NetCityProduction),
				this.currentAvailableProduction,
				productionCostWithBonus
			});
		}
		this.currentAvailableProduction -= productionCostWithBonus;
		if (this.currentAvailableProduction < 0f)
		{
			ConstructionQueue constructionQueue = base.AIEntity.Empire.GetAgency<DepartmentOfIndustry>().GetConstructionQueue(this.aiEntityCity.City);
			if (constructionQueue == null)
			{
				return;
			}
			float num = 0f;
			if (!this.departmentOfTheTreasury.TryGetResourceStockValue(this.aiEntityCity.City, DepartmentOfTheTreasury.Resources.Production, out num, false))
			{
				num = 0f;
			}
			num += this.aiEntityCity.City.GetPropertyValue(SimulationProperties.NetCityProduction);
			num = Math.Max(1f, num);
			for (int j = 0; j < constructionQueue.Length; j++)
			{
				Construction construction = constructionQueue.PeekAt(j);
				float num2 = 0f;
				for (int k = 0; k < construction.CurrentConstructionStock.Length; k++)
				{
					if (construction.CurrentConstructionStock[k].PropertyName == "Production")
					{
						num2 += construction.CurrentConstructionStock[k].Stock;
						if (construction.IsBuyout)
						{
							num2 = DepartmentOfTheTreasury.GetProductionCostWithBonus(this.aiEntityCity.City, construction.ConstructibleElement, "Production");
						}
					}
				}
				float num3 = DepartmentOfTheTreasury.GetProductionCostWithBonus(this.aiEntityCity.City, construction.ConstructibleElement, "Production") - num2;
				num -= num3;
				if (num <= 0f)
				{
					this.OrderLastResortUnit_TicketRaised_PrioritizeUnits();
					return;
				}
			}
			this.currentAvailableProduction = num;
		}
		if (this.firsttry)
		{
			this.firsttry = false;
		}
		else
		{
			this.LastResortDesignIndex = this.random.Next(this.LastResortDesigns.Count);
		}
		OrderQueueConstruction order2 = new OrderQueueConstruction(this.Empire.Index, this.aiEntityCity.City.GUID, this.LastResortDesigns[this.LastResortDesignIndex], string.Empty);
		Ticket ticket2;
		this.Empire.PlayerControllers.AI.PostOrder(order2, out ticket2, new EventHandler<TicketRaisedEventArgs>(this.OrderLastResortUnit_TicketRaised));
	}

	private void OrderCancelBooster_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		if (e.Result == PostOrderResponse.Processed)
		{
			this.constructionQueue = base.AIEntity.Empire.GetAgency<DepartmentOfIndustry>().GetConstructionQueue(this.aiEntityCity.City);
		}
	}

	private bool BoostersInQueue(bool CancelSuperfluous = false)
	{
		ConstructionQueue constructionQueue = base.AIEntity.Empire.GetAgency<DepartmentOfIndustry>().GetConstructionQueue(this.aiEntityCity.City);
		if (constructionQueue.Length <= 0)
		{
			return false;
		}
		bool result = false;
		int num = 0;
		for (int i = constructionQueue.Length - 1; i >= 0; i--)
		{
			Construction construction = constructionQueue.PeekAt(i);
			if (construction.ConstructibleElementName.ToString().Contains("BoosterGenerator"))
			{
				result = true;
				num++;
				if (CancelSuperfluous && num > 1 && construction.GetSpecificConstructionStock(DepartmentOfTheTreasury.Resources.Production) <= 0f && construction.GetSpecificConstructionStock(DepartmentOfTheTreasury.Resources.Orb) <= 0f)
				{
					OrderCancelConstruction order = new OrderCancelConstruction(base.AIEntity.Empire.Index, this.aiEntityCity.City.GUID, construction.GUID);
					Ticket ticket;
					this.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderCancelBooster_TicketRaised));
					break;
				}
				if (!CancelSuperfluous)
				{
					break;
				}
			}
		}
		return result;
	}

	private int MaxSettlersNeeded(bool CancelSuperfluous = false)
	{
		int num = 0;
		Continent[] continents = this.worldPositionningService.World.Continents;
		for (int i = 0; i < continents.Length; i++)
		{
			foreach (int regionIndex in continents[i].RegionList)
			{
				Region region = this.worldPositionningService.GetRegion(regionIndex);
				if (region.IsLand && !region.IsRegionColonized())
				{
					num++;
				}
			}
		}
		int num2 = 0;
		if (num > 0)
		{
			int currentSettlerCount = this.ColonizationLayer.CurrentSettlerCount;
			num2 = num - currentSettlerCount;
		}
		ConstructionQueue constructionQueue = base.AIEntity.Empire.GetAgency<DepartmentOfIndustry>().GetConstructionQueue(this.aiEntityCity.City);
		for (int k = constructionQueue.Length - 1; k >= 0; k--)
		{
			Construction construction = constructionQueue.PeekAt(k);
			if (construction.ConstructibleElementName.ToString().Contains("Settler"))
			{
				if (CancelSuperfluous && num2 <= 0)
				{
					OrderCancelConstruction order = new OrderCancelConstruction(base.AIEntity.Empire.Index, this.aiEntityCity.City.GUID, construction.GUID);
					Ticket ticket;
					this.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderCancelBooster_TicketRaised));
				}
				else
				{
					num2--;
				}
			}
		}
		return num2;
	}

	private WorldPosition GetAvailablePOIPosition(DepartmentOfIndustry.ConstructibleElement POIconstructible)
	{
		if (this.aiEntityCity.City == null)
		{
			return WorldPosition.Invalid;
		}
		PointOfInterest[] pointOfInterests = this.aiEntityCity.City.Region.PointOfInterests;
		for (int i = 0; i < pointOfInterests.Length; i++)
		{
			PointOfInterest chosenPointOfInterest = pointOfInterests[i];
			if (chosenPointOfInterest.PointOfInterestDefinition.PointOfInterestTemplate.Name == (POIconstructible as PointOfInterestImprovementDefinition).PointOfInterestTemplateName && chosenPointOfInterest.PointOfInterestImprovement == null && this.visibilityService.IsWorldPositionExploredFor(chosenPointOfInterest.WorldPosition, this.Empire))
			{
				ConstructionQueue constructionQueue = base.AIEntity.Empire.GetAgency<DepartmentOfIndustry>().GetConstructionQueue(this.aiEntityCity.City);
				if ((constructionQueue == null || !constructionQueue.PendingConstructions.Any((Construction construction) => construction.WorldPosition == chosenPointOfInterest.WorldPosition)) && !this.alreadyUsedPosition.Exists((WorldPosition x) => x == chosenPointOfInterest.WorldPosition))
				{
					return chosenPointOfInterest.WorldPosition;
				}
			}
		}
		return WorldPosition.Invalid;
	}

	private void CancelInvalidConstructions()
	{
		int i = this.constructionQueue.Length - 1;
		while (i >= 0)
		{
			Construction construction = this.constructionQueue.PeekAt(i);
			if (construction.ConstructibleElement is PointOfInterestImprovementDefinition && !DepartmentOfTheTreasury.CheckConstructiblePrerequisites(this.aiEntityCity.City, construction.ConstructibleElement, new string[]
			{
				ConstructionFlags.Discard
			}))
			{
				goto IL_107;
			}
			if ((construction.ConstructibleElement.SimulationDescriptorReferences.Any((SimulationDescriptorReference X) => X.Name == "OnlyOneConstructionPerEmpire") && !DepartmentOfTheTreasury.CheckConstructiblePrerequisites(this.aiEntityCity.City, construction.ConstructibleElement, new string[]
			{
				ConstructionFlags.Discard
			})) || (construction.ConstructibleElement is UnitDesign && !DepartmentOfTheTreasury.CheckConstructiblePrerequisites(this.aiEntityCity.City, construction.ConstructibleElement, new string[]
			{
				ConstructionFlags.Affinity
			})))
			{
				goto IL_107;
			}
			IL_FE:
			i--;
			continue;
			IL_107:
			OrderCancelConstruction order = new OrderCancelConstruction(base.AIEntity.Empire.Index, this.aiEntityCity.City.GUID, construction.GUID);
			Ticket ticket;
			this.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderCancelBooster_TicketRaised));
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				Diagnostics.Log("ELCP: {0}/{1} canceling {2}", new object[]
				{
					base.AIEntity.Empire,
					this.aiEntityCity.City.LocalizedName,
					construction.Name
				});
				goto IL_FE;
			}
			goto IL_FE;
		}
	}

	private void GetForbiddenResources(ref List<string> ForbiddenResources)
	{
		if (this.VictoryLayer != null)
		{
			if (this.VictoryLayer.Chapter4Resource1 != string.Empty && this.VictoryLayer.Chapter4Resource2 != string.Empty)
			{
				float num;
				if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, this.VictoryLayer.Chapter4Resource1, out num, false))
				{
					num = 0f;
				}
				float num2;
				if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, this.VictoryLayer.Chapter4Resource2, out num2, false))
				{
					num2 = 0f;
				}
				if (num < 2f * (float)this.VictoryLayer.Chapter4Resource1Amount)
				{
					ForbiddenResources.Add(this.VictoryLayer.Chapter4Resource1);
				}
				if (num2 < 2f * (float)this.VictoryLayer.Chapter4Resource2Amount)
				{
					ForbiddenResources.Add(this.VictoryLayer.Chapter4Resource2);
				}
			}
			if (this.VictoryLayer.TryingToBuildVictoryWonder)
			{
				float num3;
				if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, "Strategic5", out num3, false))
				{
					num3 = 0f;
				}
				float num4;
				if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, "Strategic6", out num4, false))
				{
					num4 = 0f;
				}
				if (num3 < 80f)
				{
					ForbiddenResources.Add("Strategic5");
				}
				if (num4 < 80f)
				{
					ForbiddenResources.Add("Strategic6");
				}
			}
		}
	}

	private bool PreventFurtherUnits()
	{
		return ELCPUtilities.ELCPShackleAI && this.aiEntityCity.City.BesiegingEmpireIndex < 0 && (float)this.departmentOfDefense.Armies.Count > 1.5f * this.Empire.GetPropertyValue(SimulationProperties.EmpireScaleFactor) && this.departmentOfDefense.Armies.Count > 15;
	}

	private void OrderLastResortUnit_TicketRaised_PrioritizeUnits()
	{
		if (this.aiEntityCity.City.BesiegingEmpireIndex < 0)
		{
			return;
		}
		ConstructionQueue constructionQueue = base.AIEntity.Empire.GetAgency<DepartmentOfIndustry>().GetConstructionQueue(this.aiEntityCity.City);
		if (constructionQueue == null)
		{
			return;
		}
		List<KeyValuePair<GameEntityGUID, float>> list = new List<KeyValuePair<GameEntityGUID, float>>();
		for (int i = 0; i < constructionQueue.Length; i++)
		{
			Construction construction = constructionQueue.PeekAt(i);
			if (construction.ConstructibleElement is UnitDesign)
			{
				float num = 0f;
				for (int j = 0; j < construction.CurrentConstructionStock.Length; j++)
				{
					if (construction.CurrentConstructionStock[j].PropertyName == "Production")
					{
						num += construction.CurrentConstructionStock[j].Stock;
						if (construction.IsBuyout)
						{
							num = DepartmentOfTheTreasury.GetProductionCostWithBonus(this.aiEntityCity.City, construction.ConstructibleElement, "Production");
						}
					}
				}
				float value = DepartmentOfTheTreasury.GetProductionCostWithBonus(this.aiEntityCity.City, construction.ConstructibleElement, "Production") - num;
				list.Add(new KeyValuePair<GameEntityGUID, float>(construction.GUID, value));
			}
		}
		list.Sort((KeyValuePair<GameEntityGUID, float> left, KeyValuePair<GameEntityGUID, float> right) => -1 * left.Value.CompareTo(right.Value));
		foreach (KeyValuePair<GameEntityGUID, float> keyValuePair in list)
		{
			OrderMoveConstruction order = new OrderMoveConstruction(this.Empire.Index, this.aiEntityCity.City.GUID, keyValuePair.Key, 0);
			this.Empire.PlayerControllers.AI.PostOrder(order);
		}
	}

	private void OrderLastResortCityBuilding(bool RushIndustry)
	{
		List<string> list;
		if (RushIndustry)
		{
			list = new List<string>
			{
				"DistrictImprovementFlames14",
				"Village_",
				"CityImprovementIndustry",
				"CityImprovementLavaRiver1",
				"CityImprovementApproval4",
				"DistrictImprovement1",
				"DistrictImprovementOrbUnlock",
				"DistrictImprovementDocks"
			};
		}
		else
		{
			list = new List<string>
			{
				"CityImprovementIndustry",
				"DistrictImprovementFlames14",
				"Village_",
				"CityImprovementDust",
				"CityImprovementRoads",
				"DistrictImprovementDocks",
				"CityImprovementTradeRoutes",
				"CityImprovementFood",
				"CityImprovementScience",
				"CityImprovementEmpirePoint",
				"CityImprovementApproval",
				"DistrictImprovement1",
				"DistrictImprovementOrbUnlock",
				"ResourceExtractor",
				"CityImprovement"
			};
		}
		List<DepartmentOfIndustry.ConstructibleElement> list2 = new List<DepartmentOfIndustry.ConstructibleElement>();
		foreach (DepartmentOfIndustry.ConstructibleElement constructibleElement in this.departmentOfIndustry.ConstructibleElementDatabase.GetAvailableConstructibleElements(new StaticString[]
		{
			CityImprovementDefinition.ReadOnlyCategory,
			ConstructibleDistrictDefinition.ReadOnlyCategory,
			PointOfInterestImprovementDefinition.ReadOnlyCategory,
			DistrictImprovementDefinition.ReadOnlyCategory,
			CoastalDistrictImprovementDefinition.ReadOnlyCategory
		}))
		{
			if (DepartmentOfTheTreasury.CheckConstructiblePrerequisites(this.aiEntityCity.City, constructibleElement, new string[]
			{
				ConstructionFlags.Prerequisite
			}))
			{
				foreach (string value in list)
				{
					if (constructibleElement.ToString().Contains(value))
					{
						if (!RushIndustry)
						{
							if (constructibleElement.Descriptors.Any((SimulationDescriptor match) => match.Name == AILayer_Production.OnlyOneConstructionPerEmpire || match.Name == AILayer_Production.OnlyOnePerEmpire))
							{
								continue;
							}
						}
						list2.Add(constructibleElement);
					}
				}
			}
		}
		List<DepartmentOfIndustry.ConstructibleElement> list3 = new List<DepartmentOfIndustry.ConstructibleElement>();
		foreach (string value2 in list)
		{
			using (List<DepartmentOfIndustry.ConstructibleElement>.Enumerator enumerator2 = list2.GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					DepartmentOfIndustry.ConstructibleElement constructibleElement2 = enumerator2.Current;
					if (constructibleElement2.ToString().Contains(value2) && !list3.Any((DepartmentOfIndustry.ConstructibleElement match) => match.Name == constructibleElement2.Name))
					{
						list3.Add(constructibleElement2);
					}
				}
			}
		}
		foreach (DepartmentOfIndustry.ConstructibleElement constructibleElement3 in list3)
		{
			if (constructibleElement3 != null && (RushIndustry || this.currentAvailableProduction > 0f) && !this.constructionQueue.Contains(constructibleElement3) && this.departmentOfIndustry.CheckConstructiblePrerequisites(this.aiEntityCity.City, constructibleElement3))
			{
				List<MissingResource> constructibleMissingRessources = this.departmentOfTheTreasury.GetConstructibleMissingRessources(this.aiEntityCity.City, constructibleElement3);
				if (constructibleMissingRessources == null || constructibleMissingRessources.Count <= 0)
				{
					OrderQueueConstruction orderQueueConstruction = new OrderQueueConstruction(this.Empire.Index, this.aiEntityCity.City.GUID, constructibleElement3, string.Empty);
					if (constructibleElement3.Name == "DistrictImprovementFlames14")
					{
						orderQueueConstruction = new OrderQueueConstruction(this.Empire.Index, this.aiEntityCity.City.GUID, constructibleElement3, this.GetAvailableCampPosition(), string.Empty);
						if (orderQueueConstruction.WorldPosition == WorldPosition.Invalid)
						{
							continue;
						}
						this.alreadyUsedPosition.Add(orderQueueConstruction.WorldPosition);
					}
					else if (constructibleElement3 is DistrictImprovementDefinition || constructibleElement3 is CoastalDistrictImprovementDefinition)
					{
						WorldPositionScore extensionBestPosition = this.GetExtensionBestPosition(constructibleElement3.Name);
						if (extensionBestPosition == null || !extensionBestPosition.WorldPosition.IsValid)
						{
							continue;
						}
						orderQueueConstruction = new OrderQueueConstruction(this.Empire.Index, this.aiEntityCity.City.GUID, constructibleElement3, extensionBestPosition.WorldPosition, string.Empty);
						this.alreadyUsedPosition.Add(extensionBestPosition.WorldPosition);
					}
					else if (constructibleElement3 is PointOfInterestImprovementDefinition)
					{
						orderQueueConstruction = new OrderQueueConstruction(this.Empire.Index, this.aiEntityCity.City.GUID, constructibleElement3, this.GetAvailablePOIPosition(constructibleElement3), string.Empty);
						if (orderQueueConstruction.WorldPosition == WorldPosition.Invalid)
						{
							continue;
						}
						this.alreadyUsedPosition.Add(orderQueueConstruction.WorldPosition);
					}
					orderQueueConstruction.InsertAtFirstPlace = RushIndustry;
					this.Empire.PlayerControllers.AI.PostOrder(orderQueueConstruction);
					this.currentAvailableProduction -= DepartmentOfTheTreasury.GetProductionCostWithBonus(this.aiEntityCity.City, constructibleElement3, "Production");
					if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
					{
						Diagnostics.Log("ELCP Empire {0} City {1} ordering lastresort building {2}, currentAvailableProduction {3}", new object[]
						{
							base.AIEntity.Empire.ToString(),
							this.aiEntityCity.City.LocalizedName,
							constructibleElement3.Name,
							this.currentAvailableProduction
						});
					}
				}
			}
		}
	}

	private void DepartmentOfIndustry_OnConstructionChange(object sender, ConstructionChangeEventArgs e)
	{
		if (e.Context.GUID != this.aiEntityCity.City.GUID || e.Action != ConstructionChangeEventAction.Completed)
		{
			return;
		}
		if (!(e.Construction.ConstructibleElement is CityImprovementDefinition))
		{
			return;
		}
		List<EvaluableMessage_BuildingProduction> list = new List<EvaluableMessage_BuildingProduction>();
		list.AddRange(this.aiEntityCity.Blackboard.GetMessages<EvaluableMessage_BuildingProduction>(BlackboardLayerID.City, (EvaluableMessage_BuildingProduction match) => match.CityGuid == this.aiEntityCity.City.GUID && match.ConstructibleElementName == e.Construction.ConstructibleElement.Name));
		foreach (EvaluableMessage_BuildingProduction evaluableMessage_BuildingProduction in list)
		{
			evaluableMessage_BuildingProduction.SetObtained();
		}
	}

	private static readonly StaticString EmpireNetStrategicResources;

	private static readonly StaticString EmpireNetLuxuryResources;

	private static readonly StaticString EmpireCityMoneyUpkeep;

	private static readonly StaticString FractionOfNeighbouringRegionsControlledByANonFriendlyEmpire;

	private static readonly StaticString EmpireCityMoney;

	private static readonly StaticString POIPillageDefense;

	private static readonly StaticString EmpireMilitaryPower;

	private List<AILayer_Production.ExtensionEvaluation> extensionEvaluations = new List<AILayer_Production.ExtensionEvaluation>();

	private List<WorldPosition> alreadyUsedPosition = new List<WorldPosition>();

	private WorldPositionScore[] extensionScores;

	private bool needCostalAccess;

	private WorldPositionScore currentWorldPositionScore;

	public static StaticString OnlyOneConstructionPerEmpire = "OnlyOneConstructionPerEmpire";

	public static StaticString OnlyOnePerEmpire = "OnlyOnePerEmpire";

	public static StaticString[] AmasProductionWeightsModifierNames = new StaticString[]
	{
		"AICityGrowth",
		"AICityProduction",
		"AICityResearch",
		"AICityMoney",
		"AICityEmpirePoint"
	};

	public FixedSizedList<EvaluationData<ConstructibleElement, InterpreterContext>> DecisionMakerEvaluationDataHistoric = new FixedSizedList<EvaluationData<ConstructibleElement, InterpreterContext>>(global::Application.FantasyPreferences.AIDebugHistoricSize);

	private AIEntity_City aiEntityCity;

	private AILayer_Booster aiLayerBooster;

	private IDatabase<AIParameterConverter> aiParameterConverterDatabase;

	private List<DepartmentOfIndustry.ConstructibleElement> candidateConstructibleElements = new List<DepartmentOfIndustry.ConstructibleElement>();

	private float colossusProductionBoost = 0.2f;

	private IConstructibleElementEvaluationAIHelper constructibleElementAIEvaluationHelper;

	private DepartmentOfIndustry.ConstructibleElement[] constructibleElements;

	private ConstructionQueue constructionQueue;

	private float currentAvailableProduction;

	private ElementEvaluator<ConstructibleElement, InterpreterContext> decisionMaker;

	private DepartmentOfIndustry departmentOfIndustry;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private DepartmentOfPlanificationAndDevelopment departmentOfPlanificationAndDevelopment;

	private IEntityInfoAIHelper entityAIHelper;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private DepartmentOfIndustry.ConstructibleElement[] nationalBuildings;

	private bool peekConstructibleCanBeDelay;

	private DepartmentOfIndustry.ConstructibleElement[] pointOfInterestConstructibleElement;

	private IVisibilityService visibilityService;

	private IWorldPositionEvaluationAIHelper worldPositionEvaluationAIHelper;

	private IWorldPositionningService worldPositionningService;

	private AIData cityAIData;

	private IPersonalityAIHelper personalityHelper;

	private AILayer_Attitude aiLayerAttitude;

	private System.Random random = new System.Random();

	[InfluencedByPersonality]
	private float settlerEconomicalStress = -0.2f;

	[InfluencedByPersonality]
	private float minimalTurnDuration = 5f;

	[InfluencedByPersonality]
	private float maximalTurnDuration = 30f;

	[InfluencedByPersonality]
	private float maximalTurnDurationBoost = -1f;

	[InfluencedByPersonality]
	private float maximalUnitDistance = 10f;

	[InfluencedByPersonality]
	private float maximalUnitDistanceBoost = -0.5f;

	[InfluencedByPersonality]
	private float maximalDevelopmentRatioBoost = 0.4f;

	[InfluencedByPersonality]
	private float minimalDevelopmentRatioForUnit = 0.5f;

	[InfluencedByPersonality]
	private int ArmyThresholdTurns;

	private int DelayedTicks;

	private List<DepartmentOfIndustry.ConstructibleElement> LastResortDesigns;

	private int LastResortDesignIndex;

	private bool boosterOnCity;

	private bool NoMoreSettlers;

	private bool firsttry;

	private AILayer_Victory VictoryLayer;

	private AILayer_Colonization ColonizationLayer;

	private DepartmentOfDefense departmentOfDefense;

	private IWorldAtlasAIHelper worldAtlasHelper;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private AILayer_Navy NavyLayer;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private AILayer_Diplomacy DiplomacyLayer;

	private float bestWorldPositionScoreSum;

	public class ExtensionEvaluation
	{
		public DistrictImprovementDefinition DistrictImprovementDefinition { get; set; }

		public float LastScore { get; set; }

		public WorldPositionScore LastWorldPosition { get; set; }
	}

	public static class OutputAIParameterNames
	{
		public const string CityApproval = "AICityApproval";

		public const string CityGrowth = "AICityGrowth";

		public const string CityProduction = "AICityProduction";

		public const string CityResearch = "AICityResearch";

		public const string CityMoney = "AICityMoney";

		public const string CityEmpirePoint = "AICityEmpirePoint";

		public const string CityMoneyUpkeep = "AICityMoneyUpkeep";

		public const string CityDefense = "AICityDefense";

		public const string CityPillageDefense = "AICityPillageDefense";

		public const string CityLuxuryResource = "AICityLuxuryResource";

		public const string CityStrategicResource = "AICityStrategicResource";

		public const string CityAntiSpy = "AICityAntiSpy";

		public const string EmpireMilitaryPower = "AIEmpireMilitaryPower";

		public const string EmpireVision = "AIEmpireVision";

		public const string EmpireWonderVictory = "AIEmpireWonderVictory";

		public const string EmpireUnlockAltarOfAuriga = "AIEmpireUnlockAltarOfAuriga";
	}
}
