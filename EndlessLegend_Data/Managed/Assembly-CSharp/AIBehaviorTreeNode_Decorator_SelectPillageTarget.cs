using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class AIBehaviorTreeNode_Decorator_SelectPillageTarget : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_SelectPillageTarget()
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
		return base.Initialize(behaviourTree);
	}

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		DepartmentOfScience agency = aiBehaviorTree.AICommander.Empire.GetAgency<DepartmentOfScience>();
		if (agency != null && !agency.CanPillage())
		{
			return State.Failure;
		}
		if (!this.downloadableContentService.IsShared(DownloadableContent11.ReadOnlyName))
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
			if (pointOfInterest2 != null && pointOfInterest2.Region.City != null)
			{
				if (pointOfInterest2.PointOfInterestImprovement != null)
				{
					if (DepartmentOfDefense.CanStartPillage(army, pointOfInterest2, false))
					{
						float num2 = 0.5f;
						if (entity != null)
						{
							num2 = this.ComputePillageInterest(pointOfInterest2, entity, army) * 0.5f;
						}
						if (num2 >= 0f)
						{
							float num3 = (float)this.worldPositionningService.GetDistance(army.WorldPosition, pointOfInterest2.WorldPosition);
							float num4 = num3 / propertyValue;
							if (num4 <= this.MaximumTurnDistance)
							{
								float num5 = 0.5f - num4 / this.MaximumTurnDistance;
								num2 = AILayer.Boost(num2, num5 * 0.5f);
								float propertyValue2 = pointOfInterest2.GetPropertyValue(SimulationProperties.PillageDefense);
								float propertyValue3 = pointOfInterest2.GetPropertyValue(SimulationProperties.MaximumPillageDefense);
								num2 = AILayer.Boost(num2, (1f - propertyValue2 / propertyValue3) * 0.2f);
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
				int remainingTurnToPillage = DepartmentOfDefense.GetRemainingTurnToPillage(army, pointOfInterest);
				IAIDataRepositoryAIHelper service = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
				Diagnostics.Assert(service != null);
				float num8 = 1f;
				AIData_Army aidata_Army;
				if (service.TryGetAIData<AIData_Army>(army.GUID, out aidata_Army))
				{
					num8 = aiBehaviorTree.AICommander.GetPillageModifier(aidata_Army.CommanderMission);
				}
				if ((float)(num6 - num7 + remainingTurnToPillage) > this.OpportunityMaximumTurn * num8)
				{
					return State.Failure;
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

	private float ComputePillageInterest(PointOfInterest target, AIEntity_Empire entityEmpire, Army army)
	{
		float normalizedScore = 0.1f;
		AILayer_Diplomacy layer = entityEmpire.GetLayer<AILayer_Diplomacy>();
		if (layer != null)
		{
			float num = layer.GetWantWarScore(target.Region.City.Empire);
			float num2 = layer.GetAllyScore(target.Region.City.Empire);
			if (num2 > 0.5f || num < 0.25f)
			{
				return -1f;
			}
			num = (num - 0.25f) / 0.75f;
			num2 = (num2 - 0.5f) / 0.5f;
			float boostFactor = 0.2f * (num - num2);
			normalizedScore = AILayer.Boost(normalizedScore, boostFactor);
		}
		float propertyValue = army.Empire.GetPropertyValue(SimulationProperties.MilitaryPower);
		float num3 = target.Region.City.Empire.GetPropertyValue(SimulationProperties.MilitaryPower);
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
		normalizedScore = AILayer.Boost(normalizedScore, boostFactor2);
		string a = target.Type;
		float boostFactor3;
		if (a == "ResourceDeposit")
		{
			boostFactor3 = 0.2f;
		}
		else if (a == "WatchTower")
		{
			boostFactor3 = 0.2f;
		}
		else
		{
			boostFactor3 = 0.2f;
		}
		normalizedScore = AILayer.Boost(normalizedScore, boostFactor3);
		return AILayer.Boost(normalizedScore, this.ComputeInfluence(army, target) * 0.5f);
	}

	private float ComputeInfluence(Army army, PointOfInterest pointOfInterest)
	{
		int remainingTurnToPillage = DepartmentOfDefense.GetRemainingTurnToPillage(army, pointOfInterest);
		float num = this.ComputeInfluence(remainingTurnToPillage, pointOfInterest.WorldPosition, pointOfInterest.Region.City.Empire);
		float num2 = this.ComputeInfluence(remainingTurnToPillage, pointOfInterest.WorldPosition, army.Empire);
		if (num > num2)
		{
			return -num2 / num;
		}
		return num / num2;
	}

	private float ComputeInfluence(int numberOfPillageTurn, WorldPosition pillageTargetPosition, global::Empire empire)
	{
		float num = 0f;
		DepartmentOfDefense agency = empire.GetAgency<DepartmentOfDefense>();
		for (int i = 0; i < agency.Armies.Count; i++)
		{
			int distance = this.worldPositionningService.GetDistance(pillageTargetPosition, agency.Armies[i].WorldPosition);
			float propertyValue = agency.Armies[i].GetPropertyValue(SimulationProperties.MaximumMovement);
			if ((float)distance < propertyValue * (float)numberOfPillageTurn)
			{
				float num2 = agency.Armies[i].GetPropertyValue(SimulationProperties.MilitaryPower);
				num2 /= 10f;
				num2 /= (float)distance;
				num += num2;
			}
		}
		DepartmentOfTheInterior agency2 = empire.GetAgency<DepartmentOfTheInterior>();
		if (agency2 != null)
		{
			for (int j = 0; j < agency2.Cities.Count; j++)
			{
				int distance2 = this.worldPositionningService.GetDistance(pillageTargetPosition, agency2.Cities[j].WorldPosition);
				float num3 = 4f;
				if ((float)distance2 < num3 * (float)numberOfPillageTurn)
				{
					float num4 = agency2.Cities[j].GetPropertyValue(SimulationProperties.Workers);
					num4 *= 10f;
					num4 /= (float)distance2;
					num += num4;
					float num5 = agency2.Cities[j].GetPropertyValue(SimulationProperties.MilitaryPower);
					num5 /= 10f;
					num5 /= (float)distance2;
					num += num5;
				}
			}
		}
		return num;
	}

	private const float MinimumWarDesire = 0.25f;

	private const float MaximumAllyDesire = 0.5f;

	private IWorldPositionningService worldPositionningService;

	private IDownloadableContentService downloadableContentService;
}
