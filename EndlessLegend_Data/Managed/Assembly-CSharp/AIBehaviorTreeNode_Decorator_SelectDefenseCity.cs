using System;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Session;

public class AIBehaviorTreeNode_Decorator_SelectDefenseCity : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_SelectDefenseCity()
	{
		this.Inverted = false;
	}

	[XmlAttribute]
	public bool Inverted { get; set; }

	[XmlAttribute]
	public string Output_TargetVarName { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		global::Empire empire = aiBehaviorTree.AICommander.Empire;
		if (empire == null || !(empire is MajorEmpire))
		{
			aiBehaviorTree.ErrorCode = 10;
			return State.Failure;
		}
		GameServer gameServer = (Services.GetService<ISessionService>().Session as global::Session).GameServer as GameServer;
		AILayer_War ailayer_War = null;
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (gameServer.AIScheduler != null && gameServer.AIScheduler.TryGetMajorEmpireAIPlayer(empire as MajorEmpire, out aiplayer_MajorEmpire))
		{
			AIEntity entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
			if (entity != null)
			{
				ailayer_War = entity.GetLayer<AILayer_War>();
			}
		}
		if (ailayer_War == null)
		{
			return State.Failure;
		}
		if (ailayer_War.warCityDefenseJobs.Count == 0)
		{
			return State.Failure;
		}
		Army key;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out key) > AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		bool flag = false;
		City city = null;
		if (ailayer_War.DefensiveArmyAssignations.TryGetValue(key, out city) && city != null)
		{
			if (aiBehaviorTree.Variables.ContainsKey(this.Output_TargetVarName))
			{
				aiBehaviorTree.Variables[this.Output_TargetVarName] = city;
			}
			else
			{
				aiBehaviorTree.Variables.Add(this.Output_TargetVarName, city);
			}
			flag = true;
		}
		if (!flag)
		{
			if (this.Inverted)
			{
				return State.Success;
			}
			return State.Failure;
		}
		else
		{
			if (this.Inverted)
			{
				return State.Failure;
			}
			return State.Success;
		}
	}

	public override bool Initialize(BehaviourTree behaviourTree)
	{
		IGameService service = Services.GetService<IGameService>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		return base.Initialize(behaviourTree);
	}

	private IWorldPositionningService worldPositionningService;
}
