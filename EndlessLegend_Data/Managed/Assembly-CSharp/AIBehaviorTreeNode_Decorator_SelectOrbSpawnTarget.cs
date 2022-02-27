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
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) != AIArmyMission.AIArmyMissionErrorCode.None)
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
		if (this.DiplomacyLayer == null && aiBehaviorTree.AICommander.Empire is MajorEmpire)
		{
			this.DiplomacyLayer = entity.GetLayer<AILayer_Diplomacy>();
		}
		Region region = this.worldPositionningService.GetRegion(army.WorldPosition);
		bool flag = false;
		AIData_Army aidata_Army;
		if (this.aiDataRepositoryHelper.TryGetAIData<AIData_Army>(army.GUID, out aidata_Army))
		{
			flag = aidata_Army.IsManta;
		}
		float num = flag ? -1000f : 0.1f;
		float propertyValue = army.GetPropertyValue(SimulationProperties.MaximumMovement);
		OrbSpawnInfo orbSpawnInfo = null;
		for (int i = 0; i < this.orbAIHelper.OrbSpawns.Count; i++)
		{
			OrbSpawnInfo orbSpawnInfo2 = this.orbAIHelper.OrbSpawns[i];
			if (orbSpawnInfo2 != null && orbSpawnInfo2.CurrentOrbCount != 0f)
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
							float num3 = (float)this.worldPositionningService.GetDistance(army.WorldPosition, orbSpawnInfo2.WorldPosition) / propertyValue;
							if (num3 <= this.MaximumTurnDistance)
							{
								if (flag)
								{
									num2 = -num3 + num2 * 1E-09f;
								}
								else
								{
									float orbDistanceExponent = this.orbAIHelper.GetOrbDistanceExponent(entity.Empire);
									float num4 = 1f + Mathf.Pow(num3, orbDistanceExponent);
									num2 /= num4;
								}
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
		if (orbSpawnInfo == null)
		{
			return State.Failure;
		}
		if (num < this.orbAIHelper.EmpireOrbNeedThreshold[army.Empire.Index] && !flag)
		{
			return State.Failure;
		}
		if (this.OpportunityMaximumTurn >= 0f)
		{
			int num5 = 0;
			int num6 = 0;
			if (aiBehaviorTree.Variables.ContainsKey(this.OpportunityMainTargetPosition))
			{
				WorldPosition mainTargetPosition = (WorldPosition)aiBehaviorTree.Variables[this.OpportunityMainTargetPosition];
				if (!AIBehaviorTreeNode_Decorator_EvaluateOpportunity.IsDetourWorthCheckingFast(this.worldPositionningService, army, orbSpawnInfo.WorldPosition, mainTargetPosition, out num6, out num5))
				{
					return State.Failure;
				}
			}
			if ((float)(num5 - num6) > this.OpportunityMaximumTurn)
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
		if (this.DiplomacyLayer != null)
		{
			Region region = this.worldPositionningService.GetRegion(orbSpawn.WorldPosition);
			if (region.Owner != null && region.Owner is MajorEmpire && this.DiplomacyLayer.GetPeaceWish(region.Owner.Index))
			{
				return -1f;
			}
		}
		return orbSpawn.EmpireNeedModifier[entityEmpire.Empire.Index];
	}

	private const float MinimumWarDesire = 0.25f;

	private const float MaximumAllyDesire = 0.5f;

	private IWorldPositionningService worldPositionningService;

	private IDownloadableContentService downloadableContentService;

	private IOrbAIHelper orbAIHelper;

	private IAIDataRepositoryAIHelper aiDataRepositoryHelper;

	private AILayer_Diplomacy DiplomacyLayer;
}
