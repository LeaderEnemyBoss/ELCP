using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class AIBehaviorTreeNode_Decorator_WaitForAllies : AIBehaviorTreeNode_Decorator
{
	[XmlAttribute]
	public string TargetVarName { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		global::Empire empire = aiBehaviorTree.AICommander.Empire;
		if (empire == null || !(empire is MajorEmpire))
		{
			aiBehaviorTree.ErrorCode = 10;
			return State.Failure;
		}
		Army army;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) > AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		if (string.IsNullOrEmpty(this.TargetVarName))
		{
			return State.Failure;
		}
		if (army.GetPropertyValue(SimulationProperties.Movement) < 0.001f)
		{
			aiBehaviorTree.ErrorCode = 24;
			return State.Failure;
		}
		IGameEntity gameEntity = aiBehaviorTree.Variables[this.TargetVarName] as IGameEntity;
		if (!(gameEntity is IWorldPositionable))
		{
			aiBehaviorTree.LogError("${0} is not a IWorldPositionable", new object[]
			{
				this.TargetVarName
			});
			return State.Failure;
		}
		IWorldPositionable worldPositionable = gameEntity as IWorldPositionable;
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		service.Game.Services.GetService<IGameEntityRepositoryService>();
		if (!worldPositionable.WorldPosition.IsValid)
		{
			aiBehaviorTree.LogError("${0} doesn't exists", new object[]
			{
				this.TargetVarName
			});
			aiBehaviorTree.ErrorCode = 10;
			return State.Failure;
		}
		if (aiBehaviorTree.AICommander.TargetPower < 0f)
		{
			aiBehaviorTree.AICommander.TargetPower = this.GetTargetPower(worldPositionable, 6, army.Empire, service);
			Diagnostics.Log("ELCP {0} AIBehaviorTreeNode_Decorator_WaitForAllies TargetPower for {1} is {2}", new object[]
			{
				army.Empire,
				worldPositionable.WorldPosition,
				aiBehaviorTree.AICommander.TargetPower
			});
		}
		float propertyValue = army.GetPropertyValue(SimulationProperties.MilitaryPower);
		if (propertyValue > aiBehaviorTree.AICommander.TargetPower * 1.5f)
		{
			if (aiBehaviorTree.Variables.ContainsKey(this.Output_LeaderVarName))
			{
				aiBehaviorTree.Variables.Remove(this.Output_LeaderVarName);
			}
			return State.Failure;
		}
		List<AIBehaviorTreeNode_Decorator_WaitForAllies.ArmyDistance> list = new List<AIBehaviorTreeNode_Decorator_WaitForAllies.ArmyDistance>();
		foreach (AICommanderMission aicommanderMission in aiBehaviorTree.AICommander.Missions)
		{
			IGameEntity gameEntity2 = null;
			if (aicommanderMission.AIDataArmyGUID.IsValid && this.gameEntityRepositoryService.TryGetValue(aicommanderMission.AIDataArmyGUID, out gameEntity2) && gameEntity2 is Army)
			{
				Army army2 = gameEntity2 as Army;
				if (army2.GUID.IsValid)
				{
					int distance = this.worldPositionningService.GetDistance(worldPositionable.WorldPosition, army2.WorldPosition);
					list.Add(new AIBehaviorTreeNode_Decorator_WaitForAllies.ArmyDistance(army2, distance));
				}
			}
		}
		list.Sort(delegate(AIBehaviorTreeNode_Decorator_WaitForAllies.ArmyDistance leftarmy, AIBehaviorTreeNode_Decorator_WaitForAllies.ArmyDistance rightarmy)
		{
			if (leftarmy.distanceToObjective != rightarmy.distanceToObjective)
			{
				return leftarmy.distanceToObjective.CompareTo(rightarmy.distanceToObjective);
			}
			return leftarmy.army.GUID.CompareTo(rightarmy.army.GUID);
		});
		int i = 0;
		Army army3 = null;
		bool flag = false;
		while (i < list.Count)
		{
			if (list[i].army == army)
			{
				float num = 0f;
				if (i > 0)
				{
					num = list[0].army.GetPropertyValue(SimulationProperties.MilitaryPower);
				}
				float propertyValue2 = army.GetPropertyValue(SimulationProperties.Movement);
				float propertyValue3 = army.GetPropertyValue(SimulationProperties.MaximumMovement);
				int distance2 = this.worldPositionningService.GetDistance(army.WorldPosition, list[0].army.WorldPosition);
				flag = (i == 0 || this.worldPositionningService.IsWaterTile(list[0].army.WorldPosition) || (float)distance2 > Mathf.Max(propertyValue2 + propertyValue3, 6f) || list[i].distanceToObjective <= distance2 || distance2 < 4);
				if (i == list.Count - 1 || (float)list[i].distanceToObjective <= Mathf.Max(propertyValue2, 6f) || this.worldPositionningService.IsWaterTile(army.WorldPosition))
				{
					if (i == list.Count - 1 && (float)list[i].distanceToObjective > Mathf.Max(propertyValue2, 6f) && !this.worldPositionningService.IsWaterTile(army.WorldPosition))
					{
						army3 = list[0].army;
						break;
					}
					break;
				}
				else if ((float)(list[i + 1].distanceToObjective - list[i].distanceToObjective) > list[i + 1].army.GetPropertyValue(SimulationProperties.MaximumMovement) + list[i + 1].army.GetPropertyValue(SimulationProperties.Movement))
				{
					if (i > 0)
					{
						army3 = list[0].army;
						break;
					}
					break;
				}
				else
				{
					if (propertyValue + num > 1.5f * aiBehaviorTree.AICommander.TargetPower || list[0].army.IsNaval)
					{
						army3 = list[0].army;
						break;
					}
					if (i == 0)
					{
						if (this.worldPositionningService.GetDistance(army.WorldPosition, list[i + 1].army.WorldPosition) > 2 && list[i + 1].distanceToObjective - list[i].distanceToObjective > 0)
						{
							return State.Success;
						}
						break;
					}
					else
					{
						if ((float)(list[i + 1].distanceToObjective - list[i].distanceToObjective) > list[i + 1].army.GetPropertyValue(SimulationProperties.Movement))
						{
							return State.Success;
						}
						break;
					}
				}
			}
			else
			{
				i++;
			}
		}
		if (army3 != null && !flag)
		{
			if (aiBehaviorTree.Variables.ContainsKey(this.Output_LeaderVarName))
			{
				aiBehaviorTree.Variables[this.Output_LeaderVarName] = army3;
			}
			else
			{
				aiBehaviorTree.Variables.Add(this.Output_LeaderVarName, army3);
			}
		}
		else if (aiBehaviorTree.Variables.ContainsKey(this.Output_LeaderVarName))
		{
			aiBehaviorTree.Variables.Remove(this.Output_LeaderVarName);
		}
		return State.Failure;
	}

	public override bool Initialize(BehaviourTree behaviourTree)
	{
		IGameService service = Services.GetService<IGameService>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		return base.Initialize(behaviourTree);
	}

	[XmlAttribute]
	public string Output_LeaderVarName { get; set; }

	private float GetTargetPower(IWorldPositionable worldPositionable, int size, global::Empire myEmpire, IGameService service)
	{
		DepartmentOfForeignAffairs agency = myEmpire.GetAgency<DepartmentOfForeignAffairs>();
		float num = 0f;
		if (worldPositionable is KaijuGarrison)
		{
			num += (worldPositionable as KaijuGarrison).GetPropertyValue(SimulationProperties.MilitaryPower);
		}
		else if (worldPositionable is Kaiju && (worldPositionable as Kaiju).OnGarrisonMode())
		{
			num += (worldPositionable as Kaiju).GetActiveTroops().GetPropertyValue(SimulationProperties.MilitaryPower);
		}
		if (size > 0)
		{
			foreach (global::Empire empire in (service.Game as global::Game).Empires)
			{
				if (empire is KaijuEmpire)
				{
					KaijuEmpire kaijuEmpire = empire as KaijuEmpire;
					if (kaijuEmpire != null && kaijuEmpire.Region != null)
					{
						KaijuCouncil agency2 = kaijuEmpire.GetAgency<KaijuCouncil>();
						if (agency2 != null && agency2.Kaiju != null && agency2.Kaiju.IsTamed() && agency2.Kaiju.OnGarrisonMode() && this.worldPositionningService.GetDistance(worldPositionable.WorldPosition, agency2.Kaiju.WorldPosition) <= size && agency.IsEnnemy(agency2.Kaiju.MajorEmpire))
						{
							num += agency2.Kaiju.KaijuGarrison.GetPropertyValue(SimulationProperties.MilitaryPower);
						}
					}
				}
			}
			List<City> list = new List<City>();
			foreach (WorldPosition worldPosition in new WorldCircle(worldPositionable.WorldPosition, size).GetWorldPositions(this.worldPositionningService.World.WorldParameters))
			{
				Army armyAtPosition = this.worldPositionningService.GetArmyAtPosition(worldPosition);
				if (armyAtPosition != null && !armyAtPosition.IsFomorian && agency.IsEnnemy(armyAtPosition.Empire))
				{
					num += armyAtPosition.GetPropertyValue(SimulationProperties.MilitaryPower);
				}
				District district = this.worldPositionningService.GetDistrict(worldPosition);
				if (district != null && district.Empire != null && district.City != null && agency.IsEnnemy(district.Empire))
				{
					if (district.Type == DistrictType.Camp)
					{
						num += district.City.Camp.GetPropertyValue(SimulationProperties.MilitaryPower);
					}
					else if (!list.Contains(district.City))
					{
						num += district.City.GetPropertyValue(SimulationProperties.MilitaryPower);
						list.Add(district.City);
					}
				}
			}
		}
		return num;
	}

	public override void Release()
	{
		base.Release();
		this.worldPositionningService = null;
		this.gameEntityRepositoryService = null;
	}

	private IWorldPositionningService worldPositionningService;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private class ArmyDistance
	{
		public ArmyDistance(Army army, int distance)
		{
			this.army = army;
			this.distanceToObjective = distance;
		}

		public Army army;

		public int distanceToObjective;
	}
}
