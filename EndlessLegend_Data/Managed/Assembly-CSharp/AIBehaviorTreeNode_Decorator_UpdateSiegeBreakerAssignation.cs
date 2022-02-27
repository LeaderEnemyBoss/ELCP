using System;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Session;

public class AIBehaviorTreeNode_Decorator_UpdateSiegeBreakerAssignation : AIBehaviorTreeNode_Decorator
{
	[XmlAttribute]
	public bool WaitForSupport { get; set; }

	[XmlAttribute]
	public string CityUnderSiege { get; set; }

	[XmlAttribute]
	public string DestinationVarName { get; set; }

	protected override State Execute(AIBehaviorTree behaviorTree, params object[] parameters)
	{
		Army army;
		if (base.GetArmyUnlessLocked(behaviorTree, "$Army", out army) != AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		Empire empire = behaviorTree.AICommander.Empire;
		if (empire == null || !(empire is MajorEmpire))
		{
			behaviorTree.ErrorCode = 10;
			return State.Failure;
		}
		if (!behaviorTree.Variables.ContainsKey(this.CityUnderSiege))
		{
			behaviorTree.LogError("city not set", new object[0]);
			return State.Failure;
		}
		City city = behaviorTree.Variables[this.CityUnderSiege] as City;
		if (city == null || city.BesiegingEmpire == null)
		{
			behaviorTree.ErrorCode = 8;
			return State.Failure;
		}
		if (this.SiegeLayer == null)
		{
			GameServer gameServer = (Services.GetService<ISessionService>().Session as global::Session).GameServer as GameServer;
			AIPlayer_MajorEmpire aiplayer_MajorEmpire;
			if (gameServer.AIScheduler != null && gameServer.AIScheduler.TryGetMajorEmpireAIPlayer(empire as MajorEmpire, out aiplayer_MajorEmpire))
			{
				AIEntity entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
				if (entity != null)
				{
					this.SiegeLayer = entity.GetLayer<AILayer_SiegeBreaker>();
				}
			}
			if (this.SiegeLayer == null)
			{
				return State.Failure;
			}
		}
		if (!this.WaitForSupport)
		{
			this.SiegeLayer.UpdateSiegeBreakerArmyAssignation(army, city);
			return State.Success;
		}
		if (!behaviorTree.Variables.ContainsKey(this.DestinationVarName))
		{
			return State.Failure;
		}
		WorldPosition targetPosition = (WorldPosition)behaviorTree.Variables[this.DestinationVarName];
		if (!targetPosition.IsValid)
		{
			behaviorTree.ErrorCode = 2;
			return State.Failure;
		}
		if (this.SiegeLayer.WaitForSupport(army, city, targetPosition))
		{
			return State.Success;
		}
		return State.Failure;
	}

	private AILayer_SiegeBreaker SiegeLayer;
}
