using System;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Decorator_CanDefeatTarget : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_CanDefeatTarget()
	{
		this.TargetVarName = null;
		this.Inverted = false;
		this.StrongestAttacker = false;
		this.EvaluateArmyGroup = false;
	}

	[XmlAttribute]
	public bool Inverted { get; set; }

	[XmlAttribute]
	public string TargetVarName { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army;
		State result;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) > AIArmyMission.AIArmyMissionErrorCode.None)
		{
			result = State.Failure;
		}
		else if (this.StrongestAttacker)
		{
			result = this.StrongestAttackerExecute(army);
		}
		else if (!aiBehaviorTree.Variables.ContainsKey(this.TargetVarName))
		{
			result = State.Failure;
		}
		else
		{
			Garrison garrison;
			if (aiBehaviorTree.Variables[this.TargetVarName] is Kaiju)
			{
				garrison = (aiBehaviorTree.Variables[this.TargetVarName] as Kaiju).GetActiveTroops();
				if (garrison == null)
				{
					aiBehaviorTree.ErrorCode = 10;
					return State.Failure;
				}
			}
			else
			{
				garrison = (aiBehaviorTree.Variables[this.TargetVarName] as Garrison);
				if (garrison == null)
				{
					aiBehaviorTree.ErrorCode = 10;
					return State.Failure;
				}
			}
			if (garrison == army)
			{
				result = State.Failure;
			}
			else if (garrison is Fortress && garrison.UnitsCount > 0)
			{
				if (this.Inverted)
				{
					return State.Success;
				}
				return State.Failure;
			}
			else
			{
				float num = (garrison is City) ? 1.5f : ((garrison is KaijuGarrison || garrison is KaijuArmy) ? 1f : 1.1f);
				IGameService service = Services.GetService<IGameService>();
				Diagnostics.Assert(service != null);
				Diagnostics.Assert(service.Game.Services.GetService<IWorldPositionningService>() != null);
				IIntelligenceAIHelper service2 = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
				float num2 = 0f;
				float num3 = 0f;
				service2.EstimateMPInBattleground(army, garrison, ref num3, ref num2);
				if (!this.EvaluateArmyGroup)
				{
					bool flag = true;
					if (num2 * num > num3)
					{
						flag = false;
					}
					if (this.Inverted)
					{
						if (!flag)
						{
							result = State.Success;
						}
						else
						{
							aiBehaviorTree.ErrorCode = 14;
							result = State.Failure;
						}
					}
					else if (flag)
					{
						result = State.Success;
					}
					else
					{
						aiBehaviorTree.ErrorCode = 13;
						result = State.Failure;
					}
				}
				else
				{
					float num4 = army.GetPropertyValue(SimulationProperties.MilitaryPower);
					foreach (AICommanderMission aicommanderMission in aiBehaviorTree.AICommander.Missions)
					{
						IGameEntity gameEntity = null;
						if (aicommanderMission.AIDataArmyGUID.IsValid && aicommanderMission.AIDataArmyGUID != army.GUID && this.gameEntityRepositoryService.TryGetValue(aicommanderMission.AIDataArmyGUID, out gameEntity) && gameEntity is Army)
						{
							Army army2 = gameEntity as Army;
							if (army2.GUID.IsValid)
							{
								num4 += army2.GetPropertyValue(SimulationProperties.MilitaryPower);
							}
						}
					}
					bool flag2 = true;
					if (num2 * num > num4)
					{
						flag2 = false;
					}
					if (this.Inverted)
					{
						if (!flag2)
						{
							result = State.Success;
						}
						else
						{
							aiBehaviorTree.ErrorCode = 14;
							result = State.Failure;
						}
					}
					else if (flag2)
					{
						result = State.Success;
					}
					else
					{
						aiBehaviorTree.ErrorCode = 13;
						result = State.Failure;
					}
				}
			}
		}
		return result;
	}

	private State StrongestAttackerExecute(Army army)
	{
		AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		Region region = service.Game.Services.GetService<IWorldPositionningService>().GetRegion(army.WorldPosition);
		if (region != null)
		{
			foreach (Army army2 in Intelligence.GetArmiesInRegion(region.Index).ToList<Army>())
			{
				District district = service.Game.Services.GetService<IWorldPositionningService>().GetDistrict(army2.WorldPosition);
				if ((district == null || !District.IsACityTile(district)) && army2.Empire == army.Empire && !army2.IsSeafaring && !army2.IsSolitary && !army2.IsPrivateers && army2.GetPropertyValue(SimulationProperties.MilitaryPower) > army.GetPropertyValue(SimulationProperties.MilitaryPower) * 1.25f)
				{
					if (!this.Inverted)
					{
						return State.Failure;
					}
					if (this.Inverted)
					{
						return State.Success;
					}
				}
			}
			if (!this.Inverted)
			{
				return State.Success;
			}
			return State.Failure;
		}
		return State.Failure;
	}

	[XmlAttribute]
	public bool StrongestAttacker { get; set; }

	public override bool Initialize(BehaviourTree behaviourTree)
	{
		IGameService service = Services.GetService<IGameService>();
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		return base.Initialize(behaviourTree);
	}

	[XmlAttribute]
	public bool EvaluateArmyGroup { get; set; }

	private IGameEntityRepositoryService gameEntityRepositoryService;
}
