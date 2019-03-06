using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Decorator_GetSiegingArmiesInRange : AIBehaviorTreeNode_Decorator
{
	[XmlAttribute]
	public string CityUnderSiege { get; set; }

	[XmlAttribute]
	public string Output_TargetListVarName { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army inputArmy;
		AIArmyMission.AIArmyMissionErrorCode armyUnlessLocked = base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out inputArmy);
		if (armyUnlessLocked != AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		if (!aiBehaviorTree.Variables.ContainsKey(this.CityUnderSiege))
		{
			aiBehaviorTree.LogError("city not set", new object[0]);
			return State.Failure;
		}
		City city = aiBehaviorTree.Variables[this.CityUnderSiege] as City;
		if (city.BesiegingEmpire == null)
		{
			aiBehaviorTree.ErrorCode = 8;
			return State.Failure;
		}
		this.targetList.Clear();
		this.targetArmiesList.Clear();
		this.worldPositionningService.TryGetListOfTargetInRange(inputArmy, -1, false, -1, ref this.targetArmiesList);
		for (int i = 0; i < city.Districts.Count; i++)
		{
			District district = city.Districts[i];
			Army armyAtPosition = this.worldPositionningService.GetArmyAtPosition(district.WorldPosition);
			if (armyAtPosition != null && armyAtPosition.Empire == city.BesiegingEmpire && this.targetArmiesList.Contains(armyAtPosition))
			{
				this.targetList.Add(armyAtPosition);
			}
		}
		if (this.targetList.Count != 0)
		{
			if (aiBehaviorTree.Variables.ContainsKey(this.Output_TargetListVarName))
			{
				aiBehaviorTree.Variables[this.Output_TargetListVarName] = this.targetList;
			}
			else
			{
				aiBehaviorTree.Variables.Add(this.Output_TargetListVarName, this.targetList);
			}
			return State.Success;
		}
		aiBehaviorTree.ErrorCode = 8;
		return State.Failure;
	}

	protected override bool Initialize(AIBehaviorTree questBehaviour)
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		return base.Initialize(questBehaviour);
	}

	private List<IWorldPositionable> targetArmiesList = new List<IWorldPositionable>();

	private List<IWorldPositionable> targetList = new List<IWorldPositionable>();

	private IWorldPositionningService worldPositionningService;
}
