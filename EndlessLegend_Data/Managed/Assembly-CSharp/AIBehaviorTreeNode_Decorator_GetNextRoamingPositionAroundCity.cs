﻿using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Decorator_GetNextRoamingPositionAroundCity : AIBehaviorTreeNode_Decorator
{
	[XmlAttribute]
	public string Output_DestinationVarName { get; set; }

	[XmlAttribute]
	public string TargetRegionVarName { get; set; }

	public override bool Initialize(BehaviourTree behaviourTree)
	{
		IGameService service = Services.GetService<IGameService>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		this.pathfindingService = service.Game.Services.GetService<IPathfindingService>();
		return base.Initialize(behaviourTree);
	}

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) != AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		Region region = null;
		if (aiBehaviorTree.Variables.ContainsKey(this.TargetCity) || !string.IsNullOrEmpty(this.TargetCity))
		{
			City city = aiBehaviorTree.Variables[this.TargetCity] as City;
			if (city != null)
			{
				region = city.Region;
			}
		}
		if (region == null)
		{
			if (string.IsNullOrEmpty(this.TargetRegionVarName) || !aiBehaviorTree.Variables.ContainsKey(this.TargetRegionVarName))
			{
				aiBehaviorTree.LogError("Target region variable '{0}' not initialized.", new object[]
				{
					this.TargetRegionVarName
				});
				return State.Failure;
			}
			int num = (int)aiBehaviorTree.Variables[this.TargetRegionVarName];
			region = this.worldPositionningService.World.Regions[num];
			if (region == null)
			{
				aiBehaviorTree.LogError("Target region index '{0}' is not valid.", new object[]
				{
					num
				});
				return State.Failure;
			}
		}
		if (region.City == null || region.City.Empire != army.Empire)
		{
			if (aiBehaviorTree.AICommander.Empire is MajorEmpire)
			{
				List<Village> list = (aiBehaviorTree.AICommander.Empire as MajorEmpire).ConvertedVillages.FindAll((Village match) => match.Region == region);
				for (int i = list.Count - 1; i >= 0; i--)
				{
					if (list[i].IsInEncounter)
					{
						list.RemoveAt(i);
					}
				}
				if (list.Count != 0)
				{
					int num2 = this.random.Next(0, list.Count);
					Village village = list[num2];
					num2 = this.random.Next(0, 6);
					WorldOrientation worldOrientation = (WorldOrientation)num2;
					for (int j = 0; j < 6; j++)
					{
						WorldPosition neighbourTile = this.worldPositionningService.GetNeighbourTile(village.WorldPosition, worldOrientation, 1);
						if (this.pathfindingService.IsTileStopableAndPassable(neighbourTile, army, PathfindingFlags.IgnoreFogOfWar, null))
						{
							this.AddOutputPositionToBehaviorTree(aiBehaviorTree, neighbourTile);
							return State.Success;
						}
						worldOrientation = worldOrientation.Rotate(1);
					}
				}
			}
			this.AddOutputPositionToBehaviorTree(aiBehaviorTree, this.GetFurthestPositionInRegion(army, region));
			return State.Success;
		}
		City city2 = region.City;
		if (!city2.IsInEncounter)
		{
			int num3 = this.random.Next(0, city2.Districts.Count);
			for (int k = 0; k < city2.Districts.Count; k++)
			{
				District district = city2.Districts[(k + num3) % city2.Districts.Count];
				if (this.pathfindingService.IsTileStopableAndPassable(district.WorldPosition, army, PathfindingFlags.IgnoreFogOfWar, null) && (city2.BesiegingEmpire == null || !District.IsACityTile(district)))
				{
					this.AddOutputPositionToBehaviorTree(aiBehaviorTree, district.WorldPosition);
					return State.Success;
				}
			}
		}
		this.AddOutputPositionToBehaviorTree(aiBehaviorTree, this.GetFurthestPositionInRegion(army, region));
		return State.Success;
	}

	private void AddOutputPositionToBehaviorTree(AIBehaviorTree aiBehaviorTree, WorldPosition position)
	{
		if (aiBehaviorTree.Variables.ContainsKey(this.Output_DestinationVarName))
		{
			aiBehaviorTree.Variables[this.Output_DestinationVarName] = position;
		}
		else
		{
			aiBehaviorTree.Variables.Add(this.Output_DestinationVarName, position);
		}
	}

	private WorldPosition GetFurthestPositionInRegion(Army army, Region region)
	{
		int num = -1;
		int minValue = int.MinValue;
		for (int i = 0; i < region.WorldPositions.Length; i++)
		{
			if (this.pathfindingService.IsTileStopableAndPassable(region.WorldPositions[i], army, PathfindingFlags.IgnoreFogOfWar, null))
			{
				int distance = this.worldPositionningService.GetDistance(army.WorldPosition, region.WorldPositions[i]);
				if (distance > minValue)
				{
					num = i;
				}
			}
		}
		if (num != -1)
		{
			return region.WorldPositions[num];
		}
		return WorldPosition.Invalid;
	}

	[XmlAttribute]
	public string TargetCity { get; set; }

	private IPathfindingService pathfindingService;

	private Random random = new Random();

	private IWorldPositionningService worldPositionningService;
}
