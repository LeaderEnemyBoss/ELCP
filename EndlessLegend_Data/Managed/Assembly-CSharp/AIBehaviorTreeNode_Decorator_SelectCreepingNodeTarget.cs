using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class AIBehaviorTreeNode_Decorator_SelectCreepingNodeTarget : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_SelectCreepingNodeTarget()
	{
		this.MaximumTurnDistance = 5f;
		this.OpportunityMaximumTurn = -1f;
	}

	[XmlAttribute]
	public float MaximumTurnDistance { get; set; }

	[XmlAttribute]
	public string Output_TargetVarName { get; set; }

	[XmlAttribute]
	public string TargetListVarName { get; set; }

	[XmlAttribute]
	public float OpportunityMaximumTurn { get; set; }

	[XmlAttribute]
	public string OpportunityMaximumTurnName { get; set; }

	[XmlAttribute]
	public string OpportunityMainTargetPosition { get; set; }

	public override bool Initialize(BehaviourTree behaviourTree)
	{
		IGameService service = Services.GetService<IGameService>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		this.downloadableContentService = Services.GetService<IDownloadableContentService>();
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		return base.Initialize(behaviourTree);
	}

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		if (!this.downloadableContentService.IsShared(DownloadableContent20.ReadOnlyName))
		{
			return State.Failure;
		}
		Army army;
		AIArmyMission.AIArmyMissionErrorCode armyUnlessLocked = base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army);
		if (armyUnlessLocked != AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		if (!aiBehaviorTree.Variables.ContainsKey(this.TargetListVarName))
		{
			return State.Failure;
		}
		if (!string.IsNullOrEmpty(this.OpportunityMaximumTurnName) && aiBehaviorTree.Variables.ContainsKey(this.OpportunityMaximumTurnName))
		{
			this.OpportunityMaximumTurn = (float)aiBehaviorTree.Variables[this.OpportunityMaximumTurnName];
		}
		List<IWorldPositionable> list = aiBehaviorTree.Variables[this.TargetListVarName] as List<IWorldPositionable>;
		if (list == null)
		{
			aiBehaviorTree.ErrorCode = 10;
			return State.Failure;
		}
		if (list.Count == 0)
		{
			aiBehaviorTree.ErrorCode = 10;
			return State.Failure;
		}
		AIEntity_Empire entity = aiBehaviorTree.AICommander.AIPlayer.GetEntity<AIEntity_Empire>();
		float num = 0.1f;
		float propertyValue = army.GetPropertyValue(SimulationProperties.MaximumMovement);
		PointOfInterest pointOfInterest = null;
		for (int i = 0; i < list.Count; i++)
		{
			PointOfInterest pointOfInterest2 = list[i] as PointOfInterest;
			if (pointOfInterest2 != null)
			{
				if (pointOfInterest2.CreepingNodeImprovement != null)
				{
					CreepingNode creepingNode = null;
					IGameEntity gameEntity = null;
					if (this.gameEntityRepositoryService.TryGetValue(pointOfInterest2.CreepingNodeGUID, out gameEntity))
					{
						creepingNode = (gameEntity as CreepingNode);
					}
					if (creepingNode != null)
					{
						if (DepartmentOfDefense.CanDismantleCreepingNode(army, creepingNode, false))
						{
							float num2 = 0.5f;
							if (entity != null)
							{
								num2 = this.ComputeDestroyInterest(creepingNode, entity, army) * 0.5f;
							}
							if (num2 >= 0f)
							{
								float num3 = (float)this.worldPositionningService.GetDistance(army.WorldPosition, pointOfInterest2.WorldPosition);
								float num4 = num3 / propertyValue;
								if (num4 <= this.MaximumTurnDistance)
								{
									float num5 = 0.5f - num4 / this.MaximumTurnDistance;
									num2 = AILayer.Boost(num2, num5 * 0.5f);
									float life = creepingNode.Life;
									float maxLife = creepingNode.MaxLife;
									num2 = AILayer.Boost(num2, (1f - life / maxLife) * 0.2f);
									if (num2 > num)
									{
										num = num2;
										pointOfInterest = pointOfInterest2;
									}
								}
							}
						}
					}
				}
			}
		}
		if (pointOfInterest != null)
		{
			if (this.OpportunityMaximumTurn > 0f)
			{
				int num6 = 0;
				int num7 = 0;
				if (aiBehaviorTree.Variables.ContainsKey(this.OpportunityMainTargetPosition))
				{
					WorldPosition mainTargetPosition = (WorldPosition)aiBehaviorTree.Variables[this.OpportunityMainTargetPosition];
					if (!AIBehaviorTreeNode_Decorator_EvaluateOpportunity.IsDetourWorthCheckingFast(this.worldPositionningService, army, pointOfInterest.WorldPosition, mainTargetPosition, out num7, out num6))
					{
						return State.Failure;
					}
				}
			}
			if (aiBehaviorTree.Variables.ContainsKey(this.Output_TargetVarName))
			{
				aiBehaviorTree.Variables[this.Output_TargetVarName] = pointOfInterest;
			}
			else
			{
				aiBehaviorTree.Variables.Add(this.Output_TargetVarName, pointOfInterest);
			}
			return State.Success;
		}
		return State.Failure;
	}

	private float ComputeDestroyInterest(CreepingNode target, AIEntity_Empire entityEmpire, Army army)
	{
		float normalizedScore = 0.1f;
		AILayer_Diplomacy layer = entityEmpire.GetLayer<AILayer_Diplomacy>();
		if (layer != null)
		{
			float num = layer.GetWantWarScore(target.Empire);
			float num2 = layer.GetAllyScore(target.Empire);
			if (num2 > 0.5f || num < 0.25f || layer.GetPeaceWish(target.Empire.Index))
			{
				return -1f;
			}
			num = (num - 0.25f) / 0.75f;
			num2 = (num2 - 0.5f) / 0.5f;
			float boostFactor = 0.2f * (num - num2);
			normalizedScore = AILayer.Boost(normalizedScore, boostFactor);
		}
		float propertyValue = army.Empire.GetPropertyValue(SimulationProperties.MilitaryPower);
		float num3 = target.Empire.GetPropertyValue(SimulationProperties.MilitaryPower);
		if (num3 == 0f)
		{
			num3 = 1f;
		}
		float num4 = propertyValue / num3;
		if (num4 < 1f)
		{
			return -1f;
		}
		float boostFactor2 = Mathf.Clamp01(num4 / 2f) * 0.2f;
		return AILayer.Boost(normalizedScore, boostFactor2);
	}

	private const float MinimumWarDesire = 0.25f;

	private const float MaximumAllyDesire = 0.5f;

	private IWorldPositionningService worldPositionningService;

	private IDownloadableContentService downloadableContentService;

	private IGameEntityRepositoryService gameEntityRepositoryService;
}
