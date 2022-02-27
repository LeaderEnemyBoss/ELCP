using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Decorator_GetConvertToPrivateersTargetPosition : AIBehaviorTreeNode_Decorator
{
	[XmlAttribute]
	public string Output_DestinationVarName { get; set; }

	public override bool Initialize(BehaviourTree behaviourTree)
	{
		IGameService service = Services.GetService<IGameService>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		return base.Initialize(behaviourTree);
	}

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) != AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		if (army.Empire == null)
		{
			return State.Failure;
		}
		DepartmentOfTheInterior agency = army.Empire.GetAgency<DepartmentOfTheInterior>();
		if (agency == null || agency.Cities.Count == 0)
		{
			return State.Failure;
		}
		List<City> list = agency.Cities.ToList<City>();
		if (list == null || list.Count == 0)
		{
			return State.Failure;
		}
		WorldPosition worldPosition = WorldPosition.Invalid;
		if (this.worldPositionningService.GetRegion(army.WorldPosition).Owner == army.Empire && !this.worldPositionningService.IsWaterTile(army.WorldPosition))
		{
			District district = this.worldPositionningService.GetDistrict(army.WorldPosition);
			if (district == null || !District.IsACityTile(district))
			{
				worldPosition = army.WorldPosition;
			}
		}
		if (worldPosition == WorldPosition.Invalid)
		{
			list.Sort(delegate(City left, City right)
			{
				int distance2 = this.worldPositionningService.GetDistance(army.WorldPosition, left.WorldPosition);
				int distance3 = this.worldPositionningService.GetDistance(army.WorldPosition, right.WorldPosition);
				if (distance2 < distance3)
				{
					return -1;
				}
				if (distance2 > distance3)
				{
					return 1;
				}
				return 0;
			});
			bool flag = false;
			IPathfindingService service = Services.GetService<IGameService>().Game.Services.GetService<IPathfindingService>();
			foreach (City city in list)
			{
				int num = int.MaxValue;
				foreach (WorldPosition worldPosition2 in city.Region.WorldPositions)
				{
					if (!this.worldPositionningService.IsWaterTile(worldPosition2) && service.IsTileStopableAndPassable(worldPosition2, army, PathfindingFlags.IgnoreFogOfWar, null))
					{
						District district2 = this.worldPositionningService.GetDistrict(worldPosition2);
						if (district2 == null || !District.IsACityTile(district2))
						{
							int distance = this.worldPositionningService.GetDistance(army.WorldPosition, worldPosition2);
							if (distance < num)
							{
								num = distance;
								worldPosition = worldPosition2;
								flag = true;
							}
						}
					}
				}
				if (flag)
				{
					break;
				}
			}
			if (!flag || worldPosition == WorldPosition.Invalid)
			{
				return State.Failure;
			}
		}
		this.AddOutputPositionToBehaviorTree(aiBehaviorTree, worldPosition);
		return State.Success;
	}

	private void AddOutputPositionToBehaviorTree(AIBehaviorTree aiBehaviorTree, WorldPosition position)
	{
		if (aiBehaviorTree.Variables.ContainsKey(this.Output_DestinationVarName))
		{
			aiBehaviorTree.Variables[this.Output_DestinationVarName] = position;
			return;
		}
		aiBehaviorTree.Variables.Add(this.Output_DestinationVarName, position);
	}

	private IWorldPositionningService worldPositionningService;
}
