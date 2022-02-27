using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Decorator_ComputeSavingPosition : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_ComputeSavingPosition()
	{
		this.TargetListVarName = null;
		this.Output_DestinationVarName = string.Empty;
	}

	[XmlAttribute]
	public string Output_DestinationVarName { get; set; }

	[XmlAttribute]
	public string TargetListVarName { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) != AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		List<Army> list = aiBehaviorTree.Variables[this.TargetListVarName] as List<Army>;
		if (list == null)
		{
			list = this.GetArmiesFromWorldPositionableList(aiBehaviorTree.Variables[this.TargetListVarName] as List<IWorldPositionable>, army, aiBehaviorTree);
			if (list == null)
			{
				return State.Failure;
			}
		}
		if (list.Count == 0)
		{
			return State.Failure;
		}
		WorldPosition worldPosition = WorldPosition.Invalid;
		if (!this.ComputeSavingPosition(army, list, out worldPosition))
		{
			aiBehaviorTree.ErrorCode = 15;
			return State.Failure;
		}
		worldPosition = WorldPosition.GetValidPosition(worldPosition, this.worldPositionningService.World.WorldParameters);
		if (worldPosition == WorldPosition.Invalid)
		{
			aiBehaviorTree.ErrorCode = 15;
			return State.Failure;
		}
		if (aiBehaviorTree.Variables.ContainsKey(this.Output_DestinationVarName))
		{
			aiBehaviorTree.Variables[this.Output_DestinationVarName] = worldPosition;
		}
		else
		{
			aiBehaviorTree.Variables.Add(this.Output_DestinationVarName, worldPosition);
		}
		return State.Success;
	}

	private bool ComputeSavingPosition(Army army, List<Army> targetList, out WorldPosition destination)
	{
		bool flag = false;
		for (int i = 0; i < army.StandardUnits.Count; i++)
		{
			if (army.StandardUnits[i].SimulationObject.Tags.Contains(WorldPositionning.FriendlyBannerDescriptor))
			{
				flag = true;
			}
		}
		if (army.Hero != null && army.Hero.IsSkillUnlocked(WorldPositionning.FriendlyBannerSkill))
		{
			flag = true;
		}
		destination = army.WorldPosition;
		for (int j = 0; j < targetList.Count; j++)
		{
			Army army2 = targetList[j];
			if (flag && (army2.Empire is MinorEmpire || army2.Empire is NavalEmpire))
			{
				targetList.RemoveAt(j);
				j--;
			}
			else
			{
				float propertyValue = army2.GetPropertyValue(SimulationProperties.Movement);
				if ((float)this.worldPositionningService.GetDistance(destination, army2.WorldPosition) > propertyValue + 2f)
				{
					targetList.RemoveAt(j);
					j--;
				}
			}
		}
		if (targetList.Count == 0)
		{
			return false;
		}
		float num = 0f;
		float num2 = 0f;
		for (int k = 0; k < targetList.Count; k++)
		{
			Army army3 = targetList[k];
			float num3 = (float)(army.WorldPosition.Column - army3.WorldPosition.Column);
			float num4 = (float)(army.WorldPosition.Row - army3.WorldPosition.Row);
			float num5 = num3 * num3 + num4 * num4;
			num += (float)(army.WorldPosition.Column - army3.WorldPosition.Column) / num5;
			num2 += (float)(army.WorldPosition.Row - army3.WorldPosition.Row) / num5;
		}
		destination.Column += (short)(num * 2f * (float)army.LineOfSightVisionRange);
		destination.Row += (short)(num2 * 2f * (float)army.LineOfSightVisionRange);
		return !(destination == army.WorldPosition);
	}

	private List<Army> GetArmiesFromWorldPositionableList(List<IWorldPositionable> sourceList, Army myArmy, AIBehaviorTree aiBehaviourTree)
	{
		if (sourceList == null || myArmy == null || aiBehaviourTree == null)
		{
			return null;
		}
		List<Army> list = new List<Army>();
		List<IWorldPositionable> list2 = aiBehaviourTree.Variables[this.TargetListVarName] as List<IWorldPositionable>;
		if (list2 != null && list2.Count > 0)
		{
			list = new List<Army>();
			for (int i = 0; i < list2.Count; i++)
			{
				if (list2[i] != null && list2[i] is Army)
				{
					Army army = list2[i] as Army;
					if (army != myArmy && army.Empire != myArmy.Empire)
					{
						list.Add(army);
					}
				}
			}
		}
		return list;
	}

	public override bool Initialize(BehaviourTree behaviourTree)
	{
		IGameService service = Services.GetService<IGameService>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		return base.Initialize(behaviourTree);
	}

	private IWorldPositionningService worldPositionningService;
}
