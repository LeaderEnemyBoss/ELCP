using System;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class AIBehaviorTreeNode_Decorator_SelectOrbSpawnTarget : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_SelectOrbSpawnTarget()
	{
		this.MaximumTurnDistance = 5f;
		this.OpportunityMaximumTurn = -1f;
	}

	[XmlAttribute]
	public float MaximumTurnDistance { get; set; }

	[XmlAttribute]
	public string Output_TargetVarName { get; set; }

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
		this.orbAIHelper = AIScheduler.Services.GetService<IOrbAIHelper>();
		this.aiDataRepositoryHelper = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		return base.Initialize(behaviourTree);
	}

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		if (!this.downloadableContentService.IsShared(DownloadableContent13.ReadOnlyName))
		{
			return State.Failure;
		}
		Army army;
		AIArmyMission.AIArmyMissionErrorCode armyUnlessLocked = base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army);
		if (armyUnlessLocked != AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		if (!(army.Empire is MajorEmpire))
		{
			return State.Failure;
		}
		if (!string.IsNullOrEmpty(this.OpportunityMaximumTurnName) && aiBehaviorTree.Variables.ContainsKey(this.OpportunityMaximumTurnName))
		{
			this.OpportunityMaximumTurn = (float)aiBehaviorTree.Variables[this.OpportunityMaximumTurnName];
		}
		AIEntity_Empire entity = aiBehaviorTree.AICommander.AIPlayer.GetEntity<AIEntity_Empire>();
		Region region = this.worldPositionningService.GetRegion(army.WorldPosition);
		bool flag = false;
		AIData_Army aidata_Army;
		if (this.aiDataRepositoryHelper.TryGetAIData<AIData_Army>(army.GUID, out aidata_Army))
		{
			flag = aidata_Army.IsManta;
		}
		float num = 0.1f;
		float propertyValue = army.GetPropertyValue(SimulationProperties.MaximumMovement);
		OrbSpawnInfo orbSpawnInfo = null;
		for (int i = 0; i < this.orbAIHelper.OrbSpawns.Count; i++)
		{
			OrbSpawnInfo orbSpawnInfo2 = this.orbAIHelper.OrbSpawns[i];
			if (orbSpawnInfo2 != null)
			{
				if (orbSpawnInfo2.CurrentOrbCount != 0f)
				{
					bool flag2 = this.worldPositionningService.IsWaterTile(orbSpawnInfo2.WorldPosition);
					if (flag || flag2 != !army.IsSeafaring)
					{
						float num2 = 0.5f;
						if (entity != null)
						{
							num2 = this.ComputeOrbCollectingScore(orbSpawnInfo2, entity, army);
						}
						if (num2 > 0f)
						{
							Region region2 = this.worldPositionningService.GetRegion(orbSpawnInfo2.WorldPosition);
							if (flag || region2.ContinentID == region.ContinentID)
							{
								float num3 = (float)this.worldPositionningService.GetDistance(army.WorldPosition, orbSpawnInfo2.WorldPosition);
								float num4 = num3 / propertyValue;
								if (num4 <= this.MaximumTurnDistance)
								{
									float orbDistanceExponent = this.orbAIHelper.GetOrbDistanceExponent(entity.Empire);
									float num5 = 1f + Mathf.Pow(num4, orbDistanceExponent);
									num2 /= num5;
									if (num2 > num)
									{
										num = num2;
										orbSpawnInfo = orbSpawnInfo2;
									}
								}
							}
						}
					}
				}
			}
		}
		if (orbSpawnInfo == null)
		{
			return State.Failure;
		}
		if (num < this.orbAIHelper.EmpireOrbNeedThreshold[army.Empire.Index])
		{
			return State.Failure;
		}
		if (this.OpportunityMaximumTurn >= 0f)
		{
			int num6 = 0;
			int num7 = 0;
			if (aiBehaviorTree.Variables.ContainsKey(this.OpportunityMainTargetPosition))
			{
				WorldPosition mainTargetPosition = (WorldPosition)aiBehaviorTree.Variables[this.OpportunityMainTargetPosition];
				if (!AIBehaviorTreeNode_Decorator_EvaluateOpportunity.IsDetourWorthCheckingFast(this.worldPositionningService, army, orbSpawnInfo.WorldPosition, mainTargetPosition, out num7, out num6))
				{
					return State.Failure;
				}
			}
			if ((float)(num6 - num7) > this.OpportunityMaximumTurn)
			{
				return State.Failure;
			}
		}
		if (aiBehaviorTree.Variables.ContainsKey(this.Output_TargetVarName))
		{
			aiBehaviorTree.Variables[this.Output_TargetVarName] = orbSpawnInfo;
		}
		else
		{
			aiBehaviorTree.Variables.Add(this.Output_TargetVarName, orbSpawnInfo);
		}
		return State.Success;
	}

	private float ComputeOrbCollectingScore(OrbSpawnInfo orbSpawn, AIEntity_Empire entityEmpire, Army army)
	{
		return orbSpawn.EmpireNeedModifier[entityEmpire.Empire.Index];
	}

	private const float MinimumWarDesire = 0.25f;

	private const float MaximumAllyDesire = 0.5f;

	private IWorldPositionningService worldPositionningService;

	private IDownloadableContentService downloadableContentService;

	private IOrbAIHelper orbAIHelper;

	private IAIDataRepositoryAIHelper aiDataRepositoryHelper;
}
