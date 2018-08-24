using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Decorator_SelectSiegingArmyPosition : AIBehaviorTreeNode_Decorator
{
	[XmlAttribute]
	public string CityUnderSiege { get; set; }

	[XmlAttribute]
	public string Output_DestinationVarName { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army;
		AIArmyMission.AIArmyMissionErrorCode armyUnlessLocked = base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army);
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
		int num = int.MaxValue;
		Army army2 = null;
		for (int i = 0; i < city.Districts.Count; i++)
		{
			District district = city.Districts[i];
			Army armyAtPosition = this.worldPositionningService.GetArmyAtPosition(district.WorldPosition);
			if (armyAtPosition != null && armyAtPosition.Empire == city.BesiegingEmpire)
			{
				int distance = this.worldPositionningService.GetDistance(army.WorldPosition, armyAtPosition.WorldPosition);
				if (distance < num)
				{
					num = distance;
					army2 = armyAtPosition;
				}
			}
		}
		if (army2 != null)
		{
			if (aiBehaviorTree.Variables.ContainsKey(this.Output_DestinationVarName))
			{
				aiBehaviorTree.Variables[this.Output_DestinationVarName] = army2.WorldPosition;
			}
			else
			{
				aiBehaviorTree.Variables.Add(this.Output_DestinationVarName, army2.WorldPosition);
			}
			return State.Success;
		}
		if (aiBehaviorTree.Variables.ContainsKey(this.Output_DestinationVarName))
		{
			aiBehaviorTree.Variables[this.Output_DestinationVarName] = WorldPosition.Invalid;
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

	private IWorldPositionningService worldPositionningService;
}
