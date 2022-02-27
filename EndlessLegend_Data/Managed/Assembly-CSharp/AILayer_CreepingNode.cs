using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;
using UnityEngine;

[PersonalityRegistryPath("AI/MajorEmpire/AIEntity_City/AILayer_CreepingNode/", new object[]
{

})]
public class AILayer_CreepingNode : AILayer
{
	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		this.gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(this.gameService != null);
		this.aiEntityCity = (base.AIEntity as AIEntity_City);
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		this.departmentOfScience = base.AIEntity.Empire.GetAgency<DepartmentOfScience>();
		this.departmentOfTheTreasury = base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>();
		this.departmentOfCreepingNodes = base.AIEntity.Empire.GetAgency<DepartmentOfCreepingNodes>();
		this.creepingNodeDefinitionDatabase = Databases.GetDatabase<CreepingNodeImprovementDefinition>(true);
		this.simulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
		this.visibilityService = this.gameService.Game.Services.GetService<IVisibilityService>();
		this.worldPositionningService = this.gameService.Game.Services.GetService<IWorldPositionningService>();
		this.gameEntityRepositoryService = this.gameService.Game.Services.GetService<IGameEntityRepositoryService>();
		this.seasonService = this.gameService.Game.Services.GetService<ISeasonService>();
		Diagnostics.Assert(this.seasonService != null);
		this.seasonService.SeasonChange += this.OnSeasonChange;
		this.validatedMessages = new List<CreepingNodeConstructionMessage>();
		this.validatedBuyoutMessages = new List<CreepingNodeBuyoutMessage>();
		this.availableNodes = new List<AILayer_CreepingNode.EvaluableCreepingNode>();
		this.random = new System.Random(World.Seed);
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_CreepingNode_CreateLocalNeedsPass", new AIEntity.AIAction(this.CreateLocalNeeds), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.ExecuteNeeds.ToString(), "AILayer_CreepingNode_ExecuteNeedsPass", new AIEntity.AIAction(this.ExecuteNeeds), this, new StaticString[0]);
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI && DepartmentOfTheInterior.CanPlaceCreepingNodes(base.AIEntity.Empire) && !this.aiEntityCity.City.IsInfected;
	}

	public override void Release()
	{
		base.Release();
		this.departmentOfTheInterior = null;
		this.departmentOfScience = null;
		this.departmentOfTheTreasury = null;
		this.departmentOfCreepingNodes = null;
		this.worldPositionningService = null;
		this.creepingNodeDefinitionDatabase = null;
		if (this.seasonService != null)
		{
			this.seasonService.SeasonChange -= this.OnSeasonChange;
		}
	}

	protected override void CreateLocalNeeds(StaticString context, StaticString pass)
	{
		base.CreateLocalNeeds(context, pass);
		this.availableNodes.Clear();
		CreepingNode[] array = this.departmentOfCreepingNodes.GetNodesUnderConstruction();
		float nodesPerPopulation = this.GetNodesPerPopulation();
		float num = nodesPerPopulation - (float)array.Length;
		if (num > 0f)
		{
			this.availableNodes = this.GetAvailableNodes();
			int num2 = 0;
			while ((float)num2 < num)
			{
				this.FilterUnaffordableNodes();
				if (this.availableNodes.Count == 0)
				{
					break;
				}
				this.ResetNodesScore();
				this.ScoreAvailableNodes();
				AILayer_CreepingNode.EvaluableCreepingNode evaluableCreepingNode = this.availableNodes[0];
				if (evaluableCreepingNode.score < this.MinScoreToQueueNode)
				{
					break;
				}
				CreepingNodeConstructionMessage message = new CreepingNodeConstructionMessage(base.AIEntity.Empire.Index, this.aiEntityCity.City.GUID, evaluableCreepingNode.pointOfInterest, evaluableCreepingNode.nodeDefinition.Name);
				base.AIEntity.AIPlayer.Blackboard.AddMessage(message);
				this.availableNodes.Remove(evaluableCreepingNode);
				num2++;
			}
		}
		if (this.IsMadSeason)
		{
			array = (from node in array
			orderby node.GetBuyoutCost() descending
			select node).ToArray<CreepingNode>();
			for (int i = 0; i < array.Length; i++)
			{
				float num3 = -array[i].GetBuyoutCost();
				if (this.departmentOfTheTreasury.IsTransferOfResourcePossible(base.AIEntity.Empire, DepartmentOfTheTreasury.Resources.EmpireMoney, ref num3))
				{
					CreepingNodeBuyoutMessage message2 = new CreepingNodeBuyoutMessage(base.AIEntity.Empire.Index, array[i].GUID);
					base.AIEntity.AIPlayer.Blackboard.AddMessage(message2);
					break;
				}
			}
		}
	}

	protected override void ExecuteNeeds(StaticString context, StaticString pass)
	{
		base.ExecuteNeeds(context, pass);
		List<CreepingNodeConstructionMessage> list = new List<CreepingNodeConstructionMessage>(base.AIEntity.AIPlayer.Blackboard.GetMessages<CreepingNodeConstructionMessage>(BlackboardLayerID.Empire, (CreepingNodeConstructionMessage match) => match.State != BlackboardMessage.StateValue.Message_Canceled));
		for (int i = list.Count - 1; i >= 0; i--)
		{
			CreepingNodeConstructionMessage message = list[i];
			if (!this.IsMessageValid(message))
			{
				base.AIEntity.AIPlayer.Blackboard.CancelMessage(message);
				list.RemoveAt(i);
			}
		}
		if (list.Count > 0)
		{
			this.validatedMessages = list;
			ISynchronousJobRepositoryAIHelper service = AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>();
			service.RegisterSynchronousJob(new SynchronousJob(this.SyncrhronousJob_QueueNode));
		}
		List<CreepingNodeBuyoutMessage> list2 = new List<CreepingNodeBuyoutMessage>(base.AIEntity.AIPlayer.Blackboard.GetMessages<CreepingNodeBuyoutMessage>(BlackboardLayerID.Empire, (CreepingNodeBuyoutMessage match) => match.State != BlackboardMessage.StateValue.Message_Canceled));
		for (int j = list2.Count - 1; j >= 0; j--)
		{
			CreepingNodeBuyoutMessage message2 = list2[j];
			if (!this.IsMessageValid(message2))
			{
				base.AIEntity.AIPlayer.Blackboard.CancelMessage(message2);
				list2.RemoveAt(j);
			}
		}
		if (list2.Count > 0)
		{
			this.validatedBuyoutMessages = list2;
			ISynchronousJobRepositoryAIHelper service2 = AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>();
			service2.RegisterSynchronousJob(new SynchronousJob(this.SyncrhronousJob_BuyoutNode));
		}
	}

	private bool IsMessageValid(CreepingNodeConstructionMessage message)
	{
		IGameEntity gameEntity = null;
		if (!this.gameEntityRepositoryService.TryGetValue(message.PointOfInterestGUID, out gameEntity))
		{
			return false;
		}
		PointOfInterest pointOfInterest = gameEntity as PointOfInterest;
		if (pointOfInterest == null || pointOfInterest.CreepingNodeImprovement != null)
		{
			return false;
		}
		for (int i = 0; i < this.validatedMessages.Count; i++)
		{
			CreepingNodeConstructionMessage creepingNodeConstructionMessage = this.validatedMessages[i];
			GameEntityGUID pointOfInterestGUID = message.PointOfInterestGUID;
			GameEntityGUID pointOfInterestGUID2 = creepingNodeConstructionMessage.PointOfInterestGUID;
			if (pointOfInterestGUID == pointOfInterestGUID2)
			{
				return false;
			}
		}
		return true;
	}

	private bool IsMessageValid(CreepingNodeBuyoutMessage message)
	{
		IGameEntity gameEntity = null;
		if (!this.gameEntityRepositoryService.TryGetValue(message.NodeGUID, out gameEntity))
		{
			return false;
		}
		CreepingNode creepingNode = gameEntity as CreepingNode;
		return creepingNode != null && creepingNode.IsUnderConstruction;
	}

	private SynchronousJobState SyncrhronousJob_QueueNode()
	{
		if (this.validatedMessages.Count > 0)
		{
			for (int i = this.validatedMessages.Count - 1; i >= 0; i--)
			{
				CreepingNodeConstructionMessage creepingNodeConstructionMessage = this.validatedMessages[i];
				this.validatedMessages.RemoveAt(i);
				CreepingNodeImprovementDefinition value = this.creepingNodeDefinitionDatabase.GetValue(creepingNodeConstructionMessage.NodeDefinitionName);
				OrderQueueCreepingNode order = new OrderQueueCreepingNode(creepingNodeConstructionMessage.EmpireIndex, creepingNodeConstructionMessage.CityGUID, creepingNodeConstructionMessage.PointOfInterestGUID, value, creepingNodeConstructionMessage.PointOfInterestPosition, false, true);
				base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order);
				base.AIEntity.AIPlayer.Blackboard.CancelMessage(creepingNodeConstructionMessage);
			}
			return SynchronousJobState.Success;
		}
		return SynchronousJobState.Failure;
	}

	private SynchronousJobState SyncrhronousJob_BuyoutNode()
	{
		if (this.validatedBuyoutMessages.Count > 0)
		{
			for (int i = this.validatedBuyoutMessages.Count - 1; i >= 0; i--)
			{
				CreepingNodeBuyoutMessage creepingNodeBuyoutMessage = this.validatedBuyoutMessages[i];
				this.validatedBuyoutMessages.RemoveAt(i);
				OrderBuyoutCreepingNode order = new OrderBuyoutCreepingNode(creepingNodeBuyoutMessage.EmpireIndex, creepingNodeBuyoutMessage.NodeGUID);
				base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order);
				base.AIEntity.AIPlayer.Blackboard.CancelMessage(creepingNodeBuyoutMessage);
			}
			return SynchronousJobState.Success;
		}
		return SynchronousJobState.Failure;
	}

	protected void FilterUnaffordableNodes()
	{
		List<AILayer_CreepingNode.EvaluableCreepingNode> list = new List<AILayer_CreepingNode.EvaluableCreepingNode>();
		for (int i = 0; i < this.availableNodes.Count; i++)
		{
			if (!this.CanAffordFoodCost(this.availableNodes[i]) || !this.CanAffordUpkeepCost(this.availableNodes[i]))
			{
				list.Add(this.availableNodes[i]);
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			this.availableNodes.Remove(list[j]);
		}
	}

	protected void ScoreAvailableNodes()
	{
		this.ComputeScoresByFIDSI();
		this.ComputeScoresByDistance();
		this.ScoreVillageNodes();
		this.ComputeScoresByResource();
		this.ScoreWatchtowerNodes();
		this.ScoreRuinsNodes();
		this.availableNodes = (from node in this.availableNodes
		orderby node.score descending
		select node).ToList<AILayer_CreepingNode.EvaluableCreepingNode>();
	}

	protected void ResetNodesScore()
	{
		for (int i = 0; i < this.availableNodes.Count; i++)
		{
			this.availableNodes[i].score = 0f;
		}
	}

	protected bool CanAffordUpkeepCost(AILayer_CreepingNode.EvaluableCreepingNode node)
	{
		List<CreepingNodeConstructionMessage> list = new List<CreepingNodeConstructionMessage>(base.AIEntity.AIPlayer.Blackboard.GetMessages<CreepingNodeConstructionMessage>(BlackboardLayerID.Empire, (CreepingNodeConstructionMessage match) => match.State != BlackboardMessage.StateValue.Message_Canceled));
		SimulationObject simulationObject = new SimulationObject("PendingNodesUpkeep");
		SimulationDescriptor upkeepDescriptor;
		for (int i = 0; i < list.Count; i++)
		{
			CreepingNodeImprovementDefinition value = this.creepingNodeDefinitionDatabase.GetValue(list[i].NodeDefinitionName);
			if (value != null)
			{
				upkeepDescriptor = value.GetUpkeepDescriptor();
				if (upkeepDescriptor != null)
				{
					simulationObject.AddDescriptor(upkeepDescriptor);
				}
			}
		}
		upkeepDescriptor = node.nodeDefinition.GetUpkeepDescriptor();
		if (upkeepDescriptor != null)
		{
			simulationObject.AddDescriptor(upkeepDescriptor);
		}
		this.aiEntityCity.City.SimulationObject.AddChild(simulationObject);
		this.aiEntityCity.City.SimulationObject.Refresh();
		float propertyValue = this.aiEntityCity.City.GetPropertyValue("CreepingNodesUpkeep");
		float upkeepLimit = this.GetUpkeepLimit();
		this.aiEntityCity.City.SimulationObject.RemoveChild(simulationObject);
		this.aiEntityCity.City.SimulationObject.Refresh();
		return propertyValue <= upkeepLimit;
	}

	protected bool CanAffordFoodCost(AILayer_CreepingNode.EvaluableCreepingNode node)
	{
		List<CreepingNodeConstructionMessage> list = new List<CreepingNodeConstructionMessage>(base.AIEntity.AIPlayer.Blackboard.GetMessages<CreepingNodeConstructionMessage>(BlackboardLayerID.Empire, (CreepingNodeConstructionMessage match) => match.State != BlackboardMessage.StateValue.Message_Canceled));
		SimulationObject simulationObject = new SimulationObject("PendingNodesUpkeep");
		SimulationDescriptor descriptor = null;
		SimulationDescriptor descriptor2 = null;
		if (this.simulationDescriptorDatabase.TryGetValue("ClassCreepingNode", out descriptor2))
		{
			simulationObject.AddDescriptor(descriptor2);
			for (int i = 0; i < list.Count; i++)
			{
				CreepingNodeImprovementDefinition value = this.creepingNodeDefinitionDatabase.GetValue(list[i].NodeDefinitionName);
				if (value != null && this.simulationDescriptorDatabase.TryGetValue(node.nodeDefinition.ConstructionCostDescriptor, out descriptor))
				{
					simulationObject.AddDescriptor(descriptor);
				}
			}
			if (this.simulationDescriptorDatabase.TryGetValue(node.nodeDefinition.ConstructionCostDescriptor, out descriptor))
			{
				simulationObject.AddDescriptor(descriptor);
			}
			this.aiEntityCity.City.SimulationObject.AddChild(simulationObject);
			this.aiEntityCity.City.SimulationObject.Refresh();
			float propertyValue = this.aiEntityCity.City.GetPropertyValue("NetCityGrowth");
			this.aiEntityCity.City.SimulationObject.RemoveChild(simulationObject);
			this.aiEntityCity.City.SimulationObject.Refresh();
			return propertyValue > 0f;
		}
		Diagnostics.LogError("Could not find the class creeping node descriptor");
		return false;
	}

	protected float GetNodesPerPopulation()
	{
		if (AILayer_CreepingNode.NodesPerPopulationFormula == null)
		{
			string value = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<string>("AI/MajorEmpire/AIEntity_City/AILayer_CreepingNode/NodesPerPopulationFormula");
			AILayer_CreepingNode.NodesPerPopulationFormula = Interpreter.InfixTransform(value);
		}
		if (AILayer_CreepingNode.FoodInterpreterContext == null)
		{
			AILayer_CreepingNode.FoodInterpreterContext = new InterpreterContext(null);
		}
		AILayer_CreepingNode.FoodInterpreterContext.SimulationObject = base.AIEntity.Empire.SimulationObject;
		return (float)Interpreter.Execute(AILayer_CreepingNode.NodesPerPopulationFormula, AILayer_CreepingNode.FoodInterpreterContext);
	}

	protected float GetUpkeepLimit()
	{
		if (AILayer_CreepingNode.MaxUpkeepFormula == null)
		{
			string value = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<string>("AI/MajorEmpire/AIEntity_City/AILayer_CreepingNode/MaxUpkeepFormula");
			AILayer_CreepingNode.MaxUpkeepFormula = Interpreter.InfixTransform(value);
		}
		if (AILayer_CreepingNode.UpkeepInterpreterContext == null)
		{
			AILayer_CreepingNode.UpkeepInterpreterContext = new InterpreterContext(null);
		}
		AILayer_CreepingNode.UpkeepInterpreterContext.SimulationObject = base.AIEntity.Empire.SimulationObject;
		return (float)Interpreter.Execute(AILayer_CreepingNode.MaxUpkeepFormula, AILayer_CreepingNode.UpkeepInterpreterContext);
	}

	protected List<AILayer_CreepingNode.EvaluableCreepingNode> GetAvailableNodes()
	{
		List<AILayer_CreepingNode.EvaluableCreepingNode> list = new List<AILayer_CreepingNode.EvaluableCreepingNode>();
		List<StaticString> list2 = new List<StaticString>();
		CreepingNodeImprovementDefinition[] values = this.creepingNodeDefinitionDatabase.GetValues();
		for (int i = 0; i < this.worldPositionningService.World.Regions.Length; i++)
		{
			Region region = this.worldPositionningService.World.Regions[i];
			bool flag = region.IsRegionColonized();
			bool flag2 = region.Kaiju != null;
			bool flag3 = region.BelongToEmpire(base.AIEntity.Empire);
			bool flag4 = false;
			if (flag2)
			{
				flag4 = (region.Kaiju.IsWild() || region.Kaiju.OwnerEmpireIndex == base.AIEntity.Empire.Index);
			}
			if (!flag || flag3 || (flag2 && flag4))
			{
				foreach (PointOfInterest pointOfInterest in region.PointOfInterests)
				{
					bool flag5 = this.IsPoiUnlocked(pointOfInterest);
					if (pointOfInterest.CreepingNodeImprovement == null)
					{
						if (!(pointOfInterest.Type != "Village") || pointOfInterest.PointOfInterestImprovement == null)
						{
							if ((this.worldPositionningService.GetExplorationBits(pointOfInterest.WorldPosition) & base.AIEntity.Empire.Bits) > 0 && flag5)
							{
								foreach (CreepingNodeImprovementDefinition creepingNodeImprovementDefinition in values)
								{
									if (creepingNodeImprovementDefinition.PointOfInterestTemplateName == pointOfInterest.PointOfInterestDefinition.PointOfInterestTemplateName)
									{
										if (!(pointOfInterest.Type == "Village") || (pointOfInterest.SimulationObject.Tags.Contains(Village.PacifiedVillage) && !pointOfInterest.SimulationObject.Tags.Contains(Village.ConvertedVillage)))
										{
											list2.Clear();
											DepartmentOfTheTreasury.CheckConstructiblePrerequisites(this.aiEntityCity.City, creepingNodeImprovementDefinition, ref list2, new string[]
											{
												ConstructionFlags.Prerequisite
											});
											if (!list2.Contains(ConstructionFlags.Discard) && this.departmentOfTheTreasury.CheckConstructibleInstantCosts(this.aiEntityCity.City, creepingNodeImprovementDefinition))
											{
												CreepingNodeImprovementDefinition bestCreepingNodeDefinition = this.departmentOfCreepingNodes.GetBestCreepingNodeDefinition(this.aiEntityCity.City, pointOfInterest, creepingNodeImprovementDefinition, list2);
												AILayer_CreepingNode.EvaluableCreepingNode item = new AILayer_CreepingNode.EvaluableCreepingNode(pointOfInterest, bestCreepingNodeDefinition);
												if (!list.Contains(item))
												{
													list.Add(item);
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
		return list;
	}

	private bool IsPoiUnlocked(PointOfInterest pointOfInterest)
	{
		string empty = string.Empty;
		return !pointOfInterest.PointOfInterestDefinition.TryGetValue("VisibilityTechnology", out empty) || this.departmentOfScience.GetTechnologyState(empty) == DepartmentOfScience.ConstructibleElement.State.Researched;
	}

	private void ComputeScoresByFIDSI()
	{
		IWorldPositionEvaluationAIHelper service = AIScheduler.Services.GetService<IWorldPositionEvaluationAIHelper>();
		Diagnostics.Assert(service != null);
		IPersonalityAIHelper service2 = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		Diagnostics.Assert(service2 != null);
		WorldPositionScore[] worldPositionCreepingNodeImprovementScore = service.GetWorldPositionCreepingNodeImprovementScore(base.AIEntity.Empire, this.aiEntityCity.City, this.availableNodes);
		string regitryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_Research/ElementEvaluatorContextMultiplier/AIEmpireGrowth";
		float registryValue = service2.GetRegistryValue<float>(base.AIEntity.Empire, regitryPath, 1f);
		regitryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_Research/ElementEvaluatorContextMultiplier/AIEmpireProduction";
		float registryValue2 = service2.GetRegistryValue<float>(base.AIEntity.Empire, regitryPath, 1f);
		regitryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_Research/ElementEvaluatorContextMultiplier/AIEmpireResearch";
		float registryValue3 = service2.GetRegistryValue<float>(base.AIEntity.Empire, regitryPath, 1f);
		regitryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_Research/ElementEvaluatorContextMultiplier/AIEmpireMoney";
		float registryValue4 = service2.GetRegistryValue<float>(base.AIEntity.Empire, regitryPath, 1f);
		regitryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_Research/ElementEvaluatorContextMultiplier/AIEmpireEmpirePoint";
		float registryValue5 = service2.GetRegistryValue<float>(base.AIEntity.Empire, regitryPath, 1f);
		List<float> list = new List<float>();
		foreach (WorldPositionScore worldPositionScore in worldPositionCreepingNodeImprovementScore)
		{
			float num = worldPositionScore.Scores[0].Value * registryValue;
			float num2 = worldPositionScore.Scores[1].Value * registryValue2;
			float num3 = worldPositionScore.Scores[2].Value * registryValue3;
			float num4 = worldPositionScore.Scores[3].Value * registryValue4;
			float num5 = worldPositionScore.Scores[4].Value * registryValue5;
			float item = num + num2 + num3 + num4 + num5;
			list.Add(item);
		}
		float num6 = list.Min();
		float num7 = list.Max();
		for (int j = 0; j < list.Count; j++)
		{
			float num8 = (num6 == num7) ? this.NormalizationFailSafeValue : Mathf.InverseLerp(num6, num7, list[j]);
			float score = this.availableNodes[j].score;
			float boostFactor = num8 * this.FIDSIWeight;
			this.availableNodes[j].score = AILayer.Boost(score, boostFactor);
		}
	}

	private void ComputeScoresByResource()
	{
		Dictionary<StaticString, int> dictionary = new Dictionary<StaticString, int>();
		IEnumerable<EvaluableMessage_ResourceNeed> messages = base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_ResourceNeed>(BlackboardLayerID.Empire, (EvaluableMessage_ResourceNeed match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending || match.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate);
		if (messages.Any<EvaluableMessage_ResourceNeed>())
		{
			foreach (EvaluableMessage_ResourceNeed evaluableMessage_ResourceNeed in messages)
			{
				if (evaluableMessage_ResourceNeed.MissingResources != null)
				{
					foreach (MissingResource missingResource in evaluableMessage_ResourceNeed.MissingResources)
					{
						if (dictionary.ContainsKey(missingResource.ResourceName))
						{
							Dictionary<StaticString, int> dictionary3;
							Dictionary<StaticString, int> dictionary2 = dictionary3 = dictionary;
							StaticString resourceName;
							StaticString key = resourceName = missingResource.ResourceName;
							int num = dictionary3[resourceName];
							dictionary2[key] = num + 1;
						}
						else
						{
							dictionary.Add(missingResource.ResourceName, 1);
						}
					}
				}
			}
		}
		List<float> list = new List<float>();
		int i = 0;
		while (i < this.availableNodes.Count)
		{
			AILayer_CreepingNode.EvaluableCreepingNode evaluableCreepingNode = this.availableNodes[i];
			PointOfInterest pointOfInterest = evaluableCreepingNode.pointOfInterest;
			CreepingNodeImprovementDefinition nodeDefinition = evaluableCreepingNode.nodeDefinition;
			float item = 0f;
			if (!(pointOfInterest.Type == "ResourceDeposit"))
			{
				goto IL_19D;
			}
			PointOfInterestDefinition pointOfInterestDefinition = pointOfInterest.PointOfInterestDefinition;
			string x;
			if (pointOfInterestDefinition.TryGetValue("ResourceName", out x))
			{
				if (dictionary.ContainsKey(x))
				{
					item = (float)dictionary[x];
					goto IL_19D;
				}
				goto IL_19D;
			}
			IL_1A6:
			i++;
			continue;
			IL_19D:
			list.Add(item);
			goto IL_1A6;
		}
		float num2 = list.Min();
		float num3 = list.Max();
		for (int j = 0; j < list.Count; j++)
		{
			if (!(this.availableNodes[j].pointOfInterest.Type != "ResourceDeposit"))
			{
				float num4 = (num2 == num3) ? this.NormalizationFailSafeValue : Mathf.InverseLerp(num2, num3, list[j]);
				float score = this.availableNodes[j].score;
				float boostFactor = num4 * this.ResourceWeight;
				this.availableNodes[j].score = AILayer.Boost(score, boostFactor);
			}
		}
	}

	private void ComputeScoresByDistance()
	{
		City[] knownCities = this.GetKnownCities(true);
		Dictionary<AILayer_CreepingNode.EvaluableCreepingNode, float> dictionary = new Dictionary<AILayer_CreepingNode.EvaluableCreepingNode, float>();
		double num = Math.Pow((double)this.worldPositionningService.World.WorldParameters.Columns, 2.0);
		double num2 = Math.Pow((double)this.worldPositionningService.World.WorldParameters.Rows, 2.0);
		float num3 = (float)Math.Sqrt(num + num2);
		float num4 = 1f;
		float num5 = 0f;
		for (int i = 0; i < this.availableNodes.Count; i++)
		{
			AILayer_CreepingNode.EvaluableCreepingNode evaluableCreepingNode = this.availableNodes[i];
			float num6 = num3;
			float num7 = num3;
			foreach (City city in knownCities)
			{
				float num8 = (float)this.worldPositionningService.GetDistance(evaluableCreepingNode.pointOfInterest.WorldPosition, city.WorldPosition);
				if (city.Empire.Index == base.AIEntity.Empire.Index)
				{
					if (num8 < num6)
					{
						num6 = num8;
					}
				}
				else if (num8 < num7)
				{
					num7 = num8;
				}
			}
			if (num6 <= 0f || num7 <= 0f)
			{
				dictionary.Add(evaluableCreepingNode, 0f);
			}
			else
			{
				float num9 = num6 + num7;
				float num10 = num7 / num9;
				if (num10 < num4)
				{
					num4 = num10;
				}
				if (num10 > num5)
				{
					num5 = num10;
				}
				float value = num10 * num7 * evaluableCreepingNode.nodeDefinition.AIPreferences.ScoringByDistanceMultiplier;
				dictionary.Add(evaluableCreepingNode, value);
			}
		}
		float num11 = float.MaxValue;
		float num12 = float.MinValue;
		foreach (float num13 in dictionary.Values)
		{
			float num14 = num13;
			if (num14 < num11)
			{
				num11 = num14;
			}
			if (num14 > num12)
			{
				num12 = num14;
			}
		}
		float num15 = num5 - num4;
		for (int k = 0; k < this.availableNodes.Count; k++)
		{
			AILayer_CreepingNode.EvaluableCreepingNode evaluableCreepingNode2 = this.availableNodes[k];
			float value2 = dictionary[evaluableCreepingNode2];
			float num16 = (num11 == num12) ? this.NormalizationFailSafeValue : Mathf.InverseLerp(num11, num12, value2);
			evaluableCreepingNode2.score = AILayer.Boost(evaluableCreepingNode2.score, num16 * num15 * this.DistanceWeight);
		}
	}

	private void ScoreRuinsNodes()
	{
		StaticString ruinPOIType = new StaticString("QuestLocation");
		for (int i = 0; i < this.availableNodes.Count; i++)
		{
			AILayer_CreepingNode.EvaluableCreepingNode evaluableCreepingNode = this.availableNodes[i];
			if (evaluableCreepingNode.pointOfInterest.Type.Equals(ruinPOIType))
			{
				Region targetRegion = this.worldPositionningService.GetRegion(evaluableCreepingNode.pointOfInterest.WorldPosition);
				PointOfInterest[] pointOfInterests = targetRegion.PointOfInterests;
				bool flag = false;
				for (int j = 0; j < pointOfInterests.Length; j++)
				{
					if (pointOfInterests[j].CreepingNodeImprovement != null && pointOfInterests[j].Empire.Index == base.AIEntity.Empire.Index && pointOfInterests[j].Type.Equals(ruinPOIType))
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					evaluableCreepingNode.score = AILayer.Boost(evaluableCreepingNode.score, -this.RuinsWeight);
				}
				else
				{
					List<CreepingNodeConstructionMessage> list = new List<CreepingNodeConstructionMessage>(base.AIEntity.AIPlayer.Blackboard.GetMessages<CreepingNodeConstructionMessage>(BlackboardLayerID.Empire, (CreepingNodeConstructionMessage match) => match.State != BlackboardMessage.StateValue.Message_Canceled && match.PointOFInterestType.Equals(ruinPOIType) && this.worldPositionningService.GetRegion(match.PointOfInterestPosition).Index == targetRegion.Index));
					if (list.Count > 0)
					{
						evaluableCreepingNode.score = AILayer.Boost(evaluableCreepingNode.score, -this.RuinsWeight);
					}
					else
					{
						evaluableCreepingNode.score = AILayer.Boost(evaluableCreepingNode.score, this.RuinsWeight);
					}
				}
			}
		}
	}

	private void ScoreVillageNodes()
	{
		int num = Mathf.RoundToInt(base.AIEntity.Empire.GetPropertyValue(SimulationProperties.MinorFactionSlotCount));
		ReadOnlyCollection<Faction> assimilatedFactions = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>().AssimilatedFactions;
		List<float> list = new List<float>();
		for (int i = 0; i < this.availableNodes.Count; i++)
		{
			if (this.availableNodes[i].pointOfInterest.Type != "Village")
			{
				list.Add(0f);
			}
			else
			{
				Region region = this.worldPositionningService.GetRegion(this.availableNodes[i].pointOfInterest.WorldPosition);
				MinorFaction minorFaction = region.MinorEmpire.Faction as MinorFaction;
				float num2 = (float)num;
				for (int j = 0; j < assimilatedFactions.Count; j++)
				{
					if (assimilatedFactions[j].Name == minorFaction.Name)
					{
						float num3 = (float)this.departmentOfTheInterior.GetNumberOfOwnedMinorFactionVillages(minorFaction, false);
						num2 += this.MaxNumOfFactionVillages - num3;
					}
				}
				list.Add(num2);
			}
		}
		float num4 = list.Min();
		float num5 = list.Max();
		for (int k = 0; k < list.Count; k++)
		{
			if (!(this.availableNodes[k].pointOfInterest.Type != "Village"))
			{
				float num6 = (num4 == num5) ? this.NormalizationFailSafeValue : Mathf.InverseLerp(num4, num5, list[k]);
				float score = this.availableNodes[k].score;
				float boostFactor = num6 * this.VilageWeight;
				this.availableNodes[k].score = AILayer.Boost(score, boostFactor);
			}
		}
	}

	private void ScoreWatchtowerNodes()
	{
		City[] knownCities = this.GetKnownCities(false);
		List<float> list = new List<float>();
		for (int i = 0; i < this.availableNodes.Count; i++)
		{
			AILayer_CreepingNode.EvaluableCreepingNode evaluableCreepingNode = this.availableNodes[i];
			if (evaluableCreepingNode.pointOfInterest.Type != "WatchTower")
			{
				list.Add(0f);
			}
			else
			{
				float num = float.MaxValue;
				int num2 = 0;
				for (int j = 0; j < knownCities.Length; j++)
				{
					City city = knownCities[j];
					float num3 = (float)this.worldPositionningService.GetDistance(evaluableCreepingNode.pointOfInterest.WorldPosition, city.WorldPosition);
					if (num3 < num)
					{
						num = num3;
						num2 = j;
					}
				}
				float num4 = 0f;
				if (knownCities.Length > 0)
				{
					num4 = 1f / num;
					DepartmentOfForeignAffairs agency = knownCities[num2].Empire.GetAgency<DepartmentOfForeignAffairs>();
					DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(base.AIEntity.Empire);
					if (diplomaticRelation != null)
					{
						string text = diplomaticRelation.State.Name;
						switch (text)
						{
						case "DiplomaticRelationStateWar":
							num4 *= this.TowerMultiplierForWar;
							goto IL_1F5;
						case "DiplomaticRelationStateTruce":
							num4 *= this.TowerMultiplierForTruce;
							goto IL_1F5;
						case "DiplomaticRelationStateColdWar":
							num4 *= this.TowerMultiplierForColdWar;
							goto IL_1F5;
						case "DiplomaticRelationStatePeace":
							num4 *= this.TowerMultiplierForPeace;
							goto IL_1F5;
						case "DiplomaticRelationStateAlliance":
							num4 *= this.TowerMultiplierForAlliance;
							goto IL_1F5;
						}
						num4 *= 0f;
					}
				}
				IL_1F5:
				list.Add(num4);
			}
		}
		float num6 = list.Min();
		float num7 = list.Max();
		for (int k = 0; k < list.Count; k++)
		{
			if (!(this.availableNodes[k].pointOfInterest.Type != "WatchTower"))
			{
				float num8 = (num6 == num7) ? this.NormalizationFailSafeValue : Mathf.InverseLerp(num6, num7, list[k]);
				float score = this.availableNodes[k].score;
				float boostFactor = num8 * this.TowersWeight;
				this.availableNodes[k].score = AILayer.Boost(score, boostFactor);
			}
		}
	}

	private City[] GetKnownCities(bool includeOwnCities = true)
	{
		List<City> list = new List<City>();
		global::Game game = this.gameService.Game as global::Game;
		if (game != null)
		{
			for (int i = 0; i < game.Empires.Length; i++)
			{
				if (game.Empires[i] is MajorEmpire)
				{
					if (includeOwnCities || game.Empires[i].Index != base.AIEntity.Empire.Index)
					{
						DepartmentOfTheInterior agency = game.Empires[i].GetAgency<DepartmentOfTheInterior>();
						if (agency != null && agency.Cities != null)
						{
							for (int j = 0; j < agency.Cities.Count; j++)
							{
								if (this.visibilityService.IsWorldPositionExploredFor(agency.Cities[j].WorldPosition, base.AIEntity.Empire))
								{
									list.Add(agency.Cities[j]);
								}
							}
						}
					}
				}
			}
		}
		return list.ToArray();
	}

	private void OnSeasonChange(object sender, SeasonChangeEventArgs e)
	{
		this.IsMadSeason = (e.NewSeason.SeasonDefinition.SeasonType == Season.ReadOnlyHeatWave);
	}

	private static object[] NodesPerPopulationFormula;

	private static InterpreterContext FoodInterpreterContext;

	private static object[] MaxUpkeepFormula;

	private static InterpreterContext UpkeepInterpreterContext;

	private AIEntity_City aiEntityCity;

	private List<CreepingNodeConstructionMessage> validatedMessages;

	private List<CreepingNodeBuyoutMessage> validatedBuyoutMessages;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private DepartmentOfScience departmentOfScience;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private DepartmentOfCreepingNodes departmentOfCreepingNodes;

	private IDatabase<CreepingNodeImprovementDefinition> creepingNodeDefinitionDatabase;

	private IDatabase<SimulationDescriptor> simulationDescriptorDatabase;

	private IGameService gameService;

	private IVisibilityService visibilityService;

	private IWorldPositionningService worldPositionningService;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private ISeasonService seasonService;

	private List<AILayer_CreepingNode.EvaluableCreepingNode> availableNodes;

	private System.Random random;

	private bool IsMadSeason;

	[InfluencedByPersonality]
	private float MinScoreToQueueNode = 0.5f;

	[InfluencedByPersonality]
	private float NormalizationFailSafeValue = 0.5f;

	[InfluencedByPersonality]
	private float FIDSIWeight = 0.5f;

	[InfluencedByPersonality]
	private float DistanceWeight = 0.3f;

	[InfluencedByPersonality]
	private float VilageWeight = 0.2f;

	[InfluencedByPersonality]
	private float ResourceWeight = 0.2f;

	[InfluencedByPersonality]
	private float RuinsWeight = 0.2f;

	[InfluencedByPersonality]
	private float TowersWeight = 0.2f;

	[InfluencedByPersonality]
	private float MaxNumOfFactionVillages = 6f;

	[InfluencedByPersonality]
	private float TowerMultiplierForWar = 2f;

	[InfluencedByPersonality]
	private float TowerMultiplierForTruce = 0.5f;

	[InfluencedByPersonality]
	private float TowerMultiplierForColdWar = 1f;

	[InfluencedByPersonality]
	private float TowerMultiplierForPeace = 0.2f;

	[InfluencedByPersonality]
	private float TowerMultiplierForAlliance = 0.2f;

	public class EvaluableCreepingNode
	{
		public EvaluableCreepingNode(PointOfInterest poi, CreepingNodeImprovementDefinition definition)
		{
			this.pointOfInterest = poi;
			this.nodeDefinition = definition;
			this.score = 0f;
		}

		public PointOfInterest pointOfInterest;

		public CreepingNodeImprovementDefinition nodeDefinition;

		public float score;
	}
}
