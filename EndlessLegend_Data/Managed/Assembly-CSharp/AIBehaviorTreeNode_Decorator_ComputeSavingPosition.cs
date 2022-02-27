using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude;
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
		AIArmyMission.AIArmyMissionErrorCode armyUnlessLocked = base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army);
		if (armyUnlessLocked != AIArmyMission.AIArmyMissionErrorCode.None)
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
		this.ComputeSavingPosition(army, list, out worldPosition);
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(service2 != null);
		worldPosition = WorldPosition.GetValidPosition(worldPosition, service2.World.WorldParameters);
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
		destination = army.WorldPosition;
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < targetList.Count; i++)
		{
			Army army2 = targetList[i];
			float num3 = (float)(army.WorldPosition.Column - army2.WorldPosition.Column);
			float num4 = (float)(army.WorldPosition.Row - army2.WorldPosition.Row);
			float num5 = num3 * num3 + num4 * num4;
			num += (float)(army.WorldPosition.Column - army2.WorldPosition.Column) / num5;
			num2 += (float)(army.WorldPosition.Row - army2.WorldPosition.Row) / num5;
		}
		destination.Column += (short)(num * 2f * (float)army.LineOfSightVisionRange);
		destination.Row += (short)(num2 * 2f * (float)army.LineOfSightVisionRange);
		return true;
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
}
