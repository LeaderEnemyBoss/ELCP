using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Decorator_SelectMantaTarget : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_SelectMantaTarget()
	{
		this.MaximumTurnDistance = 5f;
	}

	[XmlAttribute]
	public float MaximumTurnDistance { get; set; }

	[XmlAttribute]
	public string MantaZoneVarName { get; set; }

	[XmlAttribute]
	public string Output_TargetVarName { get; set; }

	public override bool Initialize(BehaviourTree behaviourTree)
	{
		IGameService service = Services.GetService<IGameService>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		this.downloadableContentService = Services.GetService<IDownloadableContentService>();
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
		MantaZone mantaZone = null;
		if (!string.IsNullOrEmpty(this.MantaZoneVarName) && aiBehaviorTree.Variables.ContainsKey(this.MantaZoneVarName))
		{
			mantaZone = (aiBehaviorTree.Variables[this.MantaZoneVarName] as MantaZone);
		}
		if (mantaZone == null)
		{
			return State.Failure;
		}
		float num = 0f;
		IWorldPositionable worldPositionable = null;
		this.GoThrought<OrbSpawnInfo>(mantaZone.Orbs, army, new Func<OrbSpawnInfo, Army, float>(this.ComputeOrbCollectingScore), ref num, ref worldPositionable);
		this.GoThrought<PointOfInterest>(mantaZone.Ruins, army, new Func<PointOfInterest, Army, float>(this.ComputeRuinScore), ref num, ref worldPositionable);
		this.GoThrought<PointOfInterest>(mantaZone.Resources, army, new Func<PointOfInterest, Army, float>(this.ComputeResourceScore), ref num, ref worldPositionable);
		if (worldPositionable != null)
		{
			if (aiBehaviorTree.Variables.ContainsKey(this.Output_TargetVarName))
			{
				aiBehaviorTree.Variables[this.Output_TargetVarName] = worldPositionable;
			}
			else
			{
				aiBehaviorTree.Variables.Add(this.Output_TargetVarName, worldPositionable);
			}
			return State.Success;
		}
		return State.Failure;
	}

	private float ComputeOrbCollectingScore(OrbSpawnInfo orbSpawn, Army army)
	{
		if (orbSpawn.CurrentOrbCount == 0f)
		{
			return -1f;
		}
		return orbSpawn.EmpireNeedModifier[army.Empire.Index];
	}

	private float ComputeResourceScore(PointOfInterest pointOfInterest, Army army)
	{
		return 0.05f;
	}

	private float ComputeRuinScore(PointOfInterest pointOfInterest, Army army)
	{
		if ((pointOfInterest.Interaction.Bits & army.Empire.Bits) == army.Empire.Bits)
		{
			return -1f;
		}
		return 0.8f;
	}

	private void ComputeScoreWithDistance(IWorldPositionable target, Army army, ref float score)
	{
		float propertyValue = army.GetPropertyValue(SimulationProperties.MaximumMovement);
		float propertyValue2 = army.GetPropertyValue(SimulationProperties.Movement);
		float num = (float)this.worldPositionningService.GetDistance(army.WorldPosition, target.WorldPosition);
		float num2 = num / propertyValue;
		if (num > propertyValue2)
		{
			num2 += 1f;
		}
		if (num2 > this.MaximumTurnDistance)
		{
			score = -1f;
			return;
		}
		float num3 = 1f;
		float num4 = 1f + num3 * num2;
		score /= num4;
	}

	private void GoThrought<T>(List<T> targets, Army army, Func<T, Army, float> scoreFunc, ref float bestScore, ref IWorldPositionable bestTarget) where T : IWorldPositionable
	{
		for (int i = 0; i < targets.Count; i++)
		{
			T t = targets[i];
			float num = scoreFunc(t, army);
			if (num >= bestScore)
			{
				this.ComputeScoreWithDistance(t, army, ref num);
				if (num > bestScore)
				{
					bestScore = num;
					bestTarget = t;
				}
			}
		}
	}

	private const float MinimumWarDesire = 0.25f;

	private const float MaximumAllyDesire = 0.5f;

	private IWorldPositionningService worldPositionningService;

	private IDownloadableContentService downloadableContentService;
}
