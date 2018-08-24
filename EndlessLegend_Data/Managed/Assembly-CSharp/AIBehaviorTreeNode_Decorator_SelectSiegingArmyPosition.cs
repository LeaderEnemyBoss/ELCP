using System;
using System.Collections.Generic;
using System.Linq;
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
		State result;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) > AIArmyMission.AIArmyMissionErrorCode.None)
		{
			result = State.Failure;
		}
		else if (!aiBehaviorTree.Variables.ContainsKey(this.CityUnderSiege))
		{
			aiBehaviorTree.LogError("city not set", new object[0]);
			result = State.Failure;
		}
		else
		{
			City city = aiBehaviorTree.Variables[this.CityUnderSiege] as City;
			if (city.BesiegingEmpire == null)
			{
				aiBehaviorTree.ErrorCode = 8;
				result = State.Failure;
			}
			else
			{
				int num = int.MaxValue;
				Army army2 = null;
				Army strongestAttackerInRegion = this.GetStrongestAttackerInRegion(army);
				for (int i = 0; i < city.Districts.Count; i++)
				{
					District district = city.Districts[i];
					Army armyAtPosition = this.worldPositionningService.GetArmyAtPosition(district.WorldPosition);
					if (armyAtPosition != null && armyAtPosition.Empire == city.BesiegingEmpire)
					{
						int distance = this.worldPositionningService.GetDistance(strongestAttackerInRegion.WorldPosition, armyAtPosition.WorldPosition);
						if (distance < num)
						{
							num = distance;
							army2 = armyAtPosition;
						}
					}
				}
				if (army2 != null)
				{
					WorldPosition worldPosition = army2.WorldPosition;
					if (strongestAttackerInRegion != army)
					{
						IPathfindingService service = Services.GetService<IGameService>().Game.Services.GetService<IPathfindingService>();
						Region region = city.Region;
						List<WorldPosition> list = new List<WorldPosition>();
						foreach (WorldPosition worldPosition2 in region.WorldPositions)
						{
							if (!this.worldPositionningService.IsWaterTile(worldPosition2) && !this.worldPositionningService.HasRidge(worldPosition2) && this.worldPositionningService.GetArmyAtPosition(worldPosition2) == null && service.IsTileStopable(worldPosition2, army, (PathfindingFlags)0, null) && this.worldPositionningService.GetDistance(army2.WorldPosition, worldPosition2) < 4)
							{
								list.Add(worldPosition2);
							}
						}
						if (list.Count > 0)
						{
							from Pos in list
							orderby this.worldPositionningService.GetDistance(army2.WorldPosition, Pos) descending
							select Pos;
							worldPosition = list[0];
							if (BattleSimulation.ELCPFortification())
							{
								list.RemoveAll((WorldPosition Pos) => this.worldPositionningService.GetDistrict(Pos) == null);
								list.RemoveAll((WorldPosition Pos) => this.worldPositionningService.GetDistrict(Pos) == null);
								if (list.Count > 0)
								{
									worldPosition = list[0];
								}
							}
							int num2 = (this.worldPositionningService.GetDistrict(worldPosition) != null) ? 1 : 0;
							bool flag = this.worldPositionningService.GetDistrict(army.WorldPosition) != null;
							if ((num2 == 0 || flag) && this.worldPositionningService.GetDistance(army2.WorldPosition, army.WorldPosition) == this.worldPositionningService.GetDistance(army2.WorldPosition, worldPosition) && !this.worldPositionningService.IsWaterTile(army.WorldPosition))
							{
								worldPosition = army.WorldPosition;
							}
						}
					}
					if (aiBehaviorTree.Variables.ContainsKey(this.Output_DestinationVarName))
					{
						aiBehaviorTree.Variables[this.Output_DestinationVarName] = worldPosition;
					}
					else
					{
						aiBehaviorTree.Variables.Add(this.Output_DestinationVarName, worldPosition);
					}
					result = State.Success;
				}
				else
				{
					if (aiBehaviorTree.Variables.ContainsKey(this.Output_DestinationVarName))
					{
						aiBehaviorTree.Variables[this.Output_DestinationVarName] = WorldPosition.Invalid;
					}
					aiBehaviorTree.ErrorCode = 8;
					result = State.Failure;
				}
			}
		}
		return result;
	}

	protected override bool Initialize(AIBehaviorTree questBehaviour)
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		return base.Initialize(questBehaviour);
	}

	private Army GetStrongestAttackerInRegion(Army army)
	{
		Army army2 = army;
		Region region = this.worldPositionningService.GetRegion(army.WorldPosition);
		Army result;
		if (region == null)
		{
			result = army2;
		}
		else
		{
			foreach (Army army3 in Intelligence.GetArmiesInRegion(region.Index).ToList<Army>())
			{
				District district = this.worldPositionningService.GetDistrict(army3.WorldPosition);
				if ((district == null || !District.IsACityTile(district)) && army3.Empire == army.Empire && army3.GetPropertyValue(SimulationProperties.MilitaryPower) > army2.GetPropertyValue(SimulationProperties.MilitaryPower))
				{
					army2 = army3;
				}
			}
			result = army2;
		}
		return result;
	}

	private IWorldPositionningService worldPositionningService;
}
