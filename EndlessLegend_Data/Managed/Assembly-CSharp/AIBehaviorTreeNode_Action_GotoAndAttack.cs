using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Action_GotoAndAttack : AIBehaviorTreeNode_Action
{
	public AIBehaviorTreeNode_Action_GotoAndAttack()
	{
		this.TargetVarName = string.Empty;
	}

	[XmlAttribute]
	public string PathVarName { get; set; }

	[XmlAttribute]
	public string TargetVarName { get; set; }

	public override void Release()
	{
		base.Release();
		this.ticket = null;
	}

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		if (this.ticket != null)
		{
			if (!this.ticket.Raised)
			{
				return State.Running;
			}
			if (this.ticket.PostOrderResponse == PostOrderResponse.PreprocessHasFailed)
			{
				this.ticket = null;
				aiBehaviorTree.ErrorCode = 1;
				return State.Failure;
			}
			this.ticket = null;
			return State.Success;
		}
		else
		{
			Army army;
			if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) != AIArmyMission.AIArmyMissionErrorCode.None)
			{
				return State.Failure;
			}
			float propertyValue = army.GetPropertyValue(SimulationProperties.MaximumNumberOfActionPoints);
			float propertyValue2 = army.GetPropertyValue(SimulationProperties.ActionPointsSpent);
			if (propertyValue <= propertyValue2)
			{
				aiBehaviorTree.ErrorCode = 33;
				return State.Failure;
			}
			if (!aiBehaviorTree.Variables.ContainsKey(this.TargetVarName))
			{
				aiBehaviorTree.LogError("${0} not set", new object[]
				{
					this.TargetVarName
				});
				return State.Failure;
			}
			IGameEntity gameEntity = aiBehaviorTree.Variables[this.TargetVarName] as IGameEntity;
			if (!(gameEntity is IWorldPositionable))
			{
				return State.Failure;
			}
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			if (!service.Game.Services.GetService<IGameEntityRepositoryService>().Contains(gameEntity.GUID))
			{
				return State.Success;
			}
			IEncounterRepositoryService service2 = service.Game.Services.GetService<IEncounterRepositoryService>();
			if (service2 != null)
			{
				IEnumerable<Encounter> enumerable = service2;
				if (enumerable != null && enumerable.Any((Encounter encounter) => encounter.IsGarrisonInEncounter(army.GUID, false)))
				{
					return State.Running;
				}
			}
			IGarrison garrison;
			if (gameEntity is Kaiju)
			{
				Kaiju kaiju = gameEntity as Kaiju;
				garrison = kaiju.GetActiveTroops();
				if (kaiju.IsStunned())
				{
					return State.Failure;
				}
			}
			else
			{
				if (gameEntity is KaijuArmy)
				{
					KaijuArmy kaijuArmy = gameEntity as KaijuArmy;
					if (kaijuArmy != null && kaijuArmy.Kaiju.IsStunned())
					{
						return State.Failure;
					}
				}
				else if (gameEntity is KaijuGarrison)
				{
					KaijuGarrison kaijuGarrison = gameEntity as KaijuGarrison;
					if (kaijuGarrison != null && kaijuGarrison.Kaiju.IsStunned())
					{
						return State.Failure;
					}
				}
				garrison = (gameEntity as IGarrison);
			}
			if (garrison == null)
			{
				return State.Failure;
			}
			if ((army.Empire is MinorEmpire || army.Empire is NavalEmpire) && garrison.Hero != null && garrison.Hero.IsSkillUnlocked("HeroSkillLeaderMap07"))
			{
				return State.Failure;
			}
			if (garrison.Empire.Index == aiBehaviorTree.AICommander.Empire.Index)
			{
				return State.Failure;
			}
			GameEntityGUID guid = gameEntity.GUID;
			if (!aiBehaviorTree.Variables.ContainsKey(this.PathVarName))
			{
				aiBehaviorTree.LogError("{0} not set", new object[]
				{
					this.PathVarName
				});
				return State.Failure;
			}
			WorldPath worldPath = aiBehaviorTree.Variables[this.PathVarName] as WorldPath;
			if (worldPath == null || worldPath.Length < 2)
			{
				aiBehaviorTree.LogError("Path is null.", new object[0]);
				aiBehaviorTree.ErrorCode = 3;
				return State.Failure;
			}
			if (!worldPath.IsValid)
			{
				aiBehaviorTree.ErrorCode = 3;
				return State.Failure;
			}
			OrderGoToAndAttack orderGoToAndAttack = new OrderGoToAndAttack(army.Empire.Index, army.GUID, guid, worldPath);
			orderGoToAndAttack.Flags = PathfindingFlags.IgnoreFogOfWar;
			aiBehaviorTree.AICommander.Empire.PlayerControllers.AI.PostOrder(orderGoToAndAttack, out this.ticket, null);
			return State.Running;
		}
	}

	private Ticket ticket;
}
