using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Action_Attack : AIBehaviorTreeNode_Action
{
	public AIBehaviorTreeNode_Action_Attack()
	{
		this.TargetVarName = string.Empty;
	}

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
			if (!this.orderExecuted)
			{
				return State.Running;
			}
			if (this.ticket.PostOrderResponse == PostOrderResponse.PreprocessHasFailed)
			{
				this.orderExecuted = false;
				this.ticket = null;
				aiBehaviorTree.ErrorCode = 1;
				return State.Failure;
			}
			this.orderExecuted = false;
			this.ticket = null;
			return State.Success;
		}
		else
		{
			Army army;
			AIArmyMission.AIArmyMissionErrorCode armyUnlessLocked = base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army);
			if (armyUnlessLocked != AIArmyMission.AIArmyMissionErrorCode.None)
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
			IGameEntity target = aiBehaviorTree.Variables[this.TargetVarName] as IGameEntity;
			if (!(target is IWorldPositionable))
			{
				return State.Failure;
			}
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
			if (!service2.Contains(target.GUID))
			{
				return State.Success;
			}
			IWorldPositionningService service3 = service.Game.Services.GetService<IWorldPositionningService>();
			IEncounterRepositoryService service4 = service.Game.Services.GetService<IEncounterRepositoryService>();
			IEnumerable<Encounter> enumerable = service4;
			if (enumerable != null)
			{
				bool flag = enumerable.Any((Encounter encounter) => encounter.IsGarrisonInEncounter(army.GUID, false) || encounter.IsGarrisonInEncounter(target.GUID, false));
				if (flag)
				{
					return State.Running;
				}
			}
			IGarrison garrison;
			if (target is Kaiju)
			{
				Kaiju kaiju = target as Kaiju;
				garrison = kaiju.GetActiveTroops();
				if (kaiju.IsStunned())
				{
					return State.Failure;
				}
			}
			else
			{
				if (target is KaijuArmy)
				{
					KaijuArmy kaijuArmy = target as KaijuArmy;
					if (kaijuArmy != null && kaijuArmy.Kaiju.IsStunned())
					{
						return State.Failure;
					}
				}
				else if (target is KaijuGarrison)
				{
					KaijuGarrison kaijuGarrison = target as KaijuGarrison;
					if (kaijuGarrison != null && kaijuGarrison.Kaiju.IsStunned())
					{
						return State.Failure;
					}
				}
				garrison = (target as IGarrison);
				if (garrison == null)
				{
					return State.Failure;
				}
			}
			if ((army.Empire is MinorEmpire || army.Empire is NavalEmpire) && garrison.Hero != null && garrison.Hero.IsSkillUnlocked("HeroSkillLeaderMap07"))
			{
				return State.Failure;
			}
			if (garrison.Empire.Index == aiBehaviorTree.AICommander.Empire.Index)
			{
				return State.Failure;
			}
			this.orderExecuted = false;
			GameEntityGUID guid = target.GUID;
			if (target is City)
			{
				Diagnostics.Assert(AIScheduler.Services != null);
				IEntityInfoAIHelper service5 = AIScheduler.Services.GetService<IEntityInfoAIHelper>();
				Diagnostics.Assert(service5 != null);
				City city = target as City;
				if (city.BesiegingEmpire != null && city.BesiegingEmpire != aiBehaviorTree.AICommander.Empire)
				{
					return State.Failure;
				}
				District districtToAttackFrom = service5.GetDistrictToAttackFrom(army, city);
				if (districtToAttackFrom == null)
				{
					aiBehaviorTree.ErrorCode = 12;
					return State.Failure;
				}
				guid = districtToAttackFrom.GUID;
			}
			else if (target is Camp)
			{
				Camp camp = target as Camp;
				if (camp == null)
				{
					return State.Failure;
				}
				IWorldPositionable worldPositionable = target as IWorldPositionable;
				if (worldPositionable == null)
				{
					return State.Failure;
				}
				if (service3.GetDistance(army.WorldPosition, worldPositionable.WorldPosition) != 1)
				{
					aiBehaviorTree.ErrorCode = 12;
					return State.Failure;
				}
				guid = camp.GUID;
			}
			else
			{
				Village village = target as Village;
				if (village != null && (village.HasBeenPacified || (village.HasBeenConverted && village.Converter == aiBehaviorTree.AICommander.Empire) || village.HasBeenInfected))
				{
					return State.Failure;
				}
				Diagnostics.Assert(AIScheduler.Services != null);
				IWorldPositionable worldPositionable2 = target as IWorldPositionable;
				if (worldPositionable2 == null)
				{
					return State.Failure;
				}
				District district = service3.GetDistrict(worldPositionable2.WorldPosition);
				if (district != null && District.IsACityTile(district) && district.City.Empire == garrison.Empire)
				{
					worldPositionable2 = district;
					guid = district.GUID;
				}
				if (service3.GetDistance(army.WorldPosition, worldPositionable2.WorldPosition) != 1)
				{
					aiBehaviorTree.ErrorCode = 12;
					return State.Failure;
				}
			}
			OrderAttack order = new OrderAttack(army.Empire.Index, army.GUID, guid);
			aiBehaviorTree.AICommander.Empire.PlayerControllers.AI.PostOrder(order, out this.ticket, new EventHandler<TicketRaisedEventArgs>(this.Order_TicketRaised));
			return State.Running;
		}
	}

	private void Order_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		this.orderExecuted = true;
	}

	private bool orderExecuted;

	private Ticket ticket;
}
