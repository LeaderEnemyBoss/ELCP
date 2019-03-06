using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Path;
using Amplitude.Unity.AI;
using Amplitude.Unity.AI.Decision;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Utilities.Maps;

public class WorldPositionEvaluation : AIHelper, IWorldPositionEvaluationAIHelper, IService, IAIEvaluationHelper<WorldPositionScore, InterpreterContext>, ISimulationAIEvaluationHelper<WorldPositionScore>
{
	private IDatabase<SimulationDescriptor> SimulationDescriptorDatabase { get; set; }

	private IWorldPositionningService WorldPositionningService { get; set; }

	public SimulationObject GetEmpireCityProxy(global::Empire empire)
	{
		return this.cityProxyByEmpires[empire.Index];
	}

	public WorldPositionScore[] GetWorldPositionColonizationScore(global::Empire empire, WorldPosition[] listOfPositionCandidate)
	{
		List<WorldPositionScore> list = new List<WorldPositionScore>();
		for (int i = 0; i < listOfPositionCandidate.Length; i++)
		{
			if (this.WorldPositionningService.IsExtensionConstructible(listOfPositionCandidate[i], false))
			{
				if (this.WorldPositionningService.IsConstructible(listOfPositionCandidate[i], 0))
				{
					list.Add(this.ComputeColonizationScoreAtPosition(empire, listOfPositionCandidate[i]));
				}
			}
		}
		return list.ToArray();
	}

	public WorldPositionScore[] GetWorldPositionCreepingNodeImprovementScore(global::Empire empire, City city, List<AILayer_CreepingNode.EvaluableCreepingNode> listOfCreepingNodeCandidate)
	{
		List<WorldPositionScore> list = new List<WorldPositionScore>();
		city.SimulationObject.AddChild(this.districtProxyByEmpires[empire.Index]);
		for (int i = 0; i < listOfCreepingNodeCandidate.Count; i++)
		{
			PointOfInterest pointOfInterest = listOfCreepingNodeCandidate[i].pointOfInterest;
			CreepingNodeImprovementDefinition nodeDefinition = listOfCreepingNodeCandidate[i].nodeDefinition;
			if (pointOfInterest != null && nodeDefinition != null)
			{
				list.Add(this.ComputeCreepingNodeImprovementScoreAtPosition(empire, city, pointOfInterest, nodeDefinition));
			}
		}
		city.SimulationObject.RemoveChild(this.districtProxyByEmpires[empire.Index]);
		this.cityProxyByEmpires[empire.Index].AddChild(this.districtProxyByEmpires[empire.Index]);
		return list.ToArray();
	}

	public WorldPositionScore[] GetWorldPositionExpansionScore(global::Empire empire, City city)
	{
		List<WorldPositionScore> list = new List<WorldPositionScore>();
		city.SimulationObject.AddChild(this.districtProxyByEmpires[empire.Index]);
		for (int i = 0; i < city.Districts.Count; i++)
		{
			if (this.WorldPositionningService.IsExtensionConstructible(city.Districts[i].WorldPosition, false))
			{
				if (city.Districts[i].Type == DistrictType.Exploitation)
				{
					list.Add(this.ComputeExpansionScoreAtPosition(empire, city, city.WorldPosition, city.Districts[i].WorldPosition));
				}
			}
		}
		this.cityProxyByEmpires[empire.Index].AddChild(this.districtProxyByEmpires[empire.Index]);
		return list.ToArray();
	}

	public WorldPositionScore GetWorldPositionExpansionScore(global::Empire empire, City city, WorldPosition position)
	{
		city.SimulationObject.AddChild(this.districtProxyByEmpires[empire.Index]);
		WorldPositionScore result = null;
		if (this.WorldPositionningService.IsExtensionConstructible(position, false))
		{
			result = this.ComputeExpansionScoreAtPosition(empire, city, city.WorldPosition, position);
		}
		this.cityProxyByEmpires[empire.Index].AddChild(this.districtProxyByEmpires[empire.Index]);
		return result;
	}

	public WorldPositionScore[] GetWorldPositionExpansionScore(global::Empire empire, City city, IEnumerable<WorldPosition> elements)
	{
		List<WorldPositionScore> list = new List<WorldPositionScore>();
		city.SimulationObject.AddChild(this.districtProxyByEmpires[empire.Index]);
		foreach (WorldPosition worldPosition in elements)
		{
			if (this.WorldPositionningService.IsExtensionConstructible(worldPosition, false))
			{
				District district = this.WorldPositionningService.GetDistrict(worldPosition);
				if (district != null && !District.IsACityTile(district))
				{
					list.Add(this.ComputeExpansionScoreAtPosition(empire, city, city.WorldPosition, worldPosition));
				}
			}
		}
		this.cityProxyByEmpires[empire.Index].AddChild(this.districtProxyByEmpires[empire.Index]);
		return list.ToArray();
	}

	public bool IsLevelUpPossibleByItSelf(City city, WorldPosition currentCityDistrict)
	{
		int num = 0;
		for (int i = 0; i < 6; i++)
		{
			WorldPosition neighbourTile = this.WorldPositionningService.GetNeighbourTile(currentCityDistrict, (WorldOrientation)i, 1);
			bool flag = false;
			for (int j = 0; j < city.Districts.Count; j++)
			{
				if (city.Districts[j].Type != DistrictType.Exploitation && city.Districts[j].WorldPosition == neighbourTile)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				num++;
			}
		}
		return num == DepartmentOfTheInterior.MinimumNumberOfExtensionNeighbourForLevelUp;
	}

	public bool IsLevelUpPossibleByNeigbourg(City city, WorldPosition currentCityDistrict)
	{
		for (int i = 0; i < city.Districts.Count; i++)
		{
			if (city.Districts[i].WorldPosition == currentCityDistrict && (city.Districts[i].Level >= 1 || city.Districts[i].Type == DistrictType.Exploitation))
			{
				return false;
			}
		}
		int num = 0;
		for (int j = 0; j < 6; j++)
		{
			WorldPosition neighbourTile = this.WorldPositionningService.GetNeighbourTile(currentCityDistrict, (WorldOrientation)j, 1);
			bool flag = false;
			for (int k = 0; k < city.Districts.Count; k++)
			{
				if (city.Districts[k].Type != DistrictType.Exploitation && city.Districts[k].WorldPosition == neighbourTile)
				{
					flag = true;
				}
			}
			if (flag)
			{
				num++;
			}
		}
		return num == DepartmentOfTheInterior.MinimumNumberOfExtensionNeighbourForLevelUp - 1;
	}

	public void ComputeCountByOrientation(WorldPosition position, ref int[] countByOrientation)
	{
		int regionIndex = (int)this.WorldPositionningService.GetRegionIndex(position);
		for (int i = 0; i < 6; i++)
		{
			countByOrientation[i] = 0;
		}
		for (int j = 0; j < countByOrientation.Length; j++)
		{
			WorldOrientation worldOrientation = (WorldOrientation)j;
			int num = j;
			int num2 = (int)worldOrientation.Rotate(3);
			WorldPosition worldPosition = position;
			WorldPosition worldPosition2 = this.WorldPositionningService.GetNeighbourTile(worldPosition, worldOrientation.Rotate(1), 1);
			if (!this.IsTileValidForExtension(worldPosition2, worldOrientation, regionIndex, 1))
			{
				worldPosition2 = WorldPosition.Invalid;
			}
			WorldPosition worldPosition3 = this.WorldPositionningService.GetNeighbourTile(worldPosition, worldOrientation.Rotate(-1), 1);
			if (!this.IsTileValidForExtension(worldPosition3, worldOrientation, regionIndex, -1))
			{
				worldPosition3 = WorldPosition.Invalid;
			}
			while (worldPosition.IsValid && (worldPosition3.IsValid || worldPosition2.IsValid))
			{
				WorldPosition neighbourTile = this.WorldPositionningService.GetNeighbourTile(worldPosition, worldOrientation, 1);
				if (!this.IsTileValidForExtension(neighbourTile, worldOrientation, regionIndex, 0))
				{
					break;
				}
				WorldPosition worldPosition4 = WorldPosition.Invalid;
				if (worldPosition2.IsValid)
				{
					countByOrientation[num]++;
					worldPosition4 = this.WorldPositionningService.GetNeighbourTile(worldPosition2, worldOrientation, 1);
					if (!this.IsTileValidForExtension(worldPosition4, worldOrientation, regionIndex, 1))
					{
						worldPosition4 = WorldPosition.Invalid;
					}
				}
				WorldPosition worldPosition5 = WorldPosition.Invalid;
				if (worldPosition3.IsValid)
				{
					countByOrientation[num2]++;
					worldPosition5 = this.WorldPositionningService.GetNeighbourTile(worldPosition3, worldOrientation, 1);
					if (!this.IsTileValidForExtension(worldPosition5, worldOrientation, regionIndex, -1))
					{
						worldPosition5 = WorldPosition.Invalid;
					}
				}
				worldPosition = neighbourTile;
				worldPosition2 = worldPosition4;
				worldPosition3 = worldPosition5;
			}
		}
	}

	private WorldPositionScore ComputeColonizationScoreAtPosition(global::Empire empire, WorldPosition element)
	{
		if (!element.IsValid)
		{
			return null;
		}
		bool flag = false;
		WorldPositionScore worldPositionScore = new WorldPositionScore(this.GetWorldPositionScore(empire.Index, element));
		int regionIndex = (int)this.WorldPositionningService.GetRegionIndex(element);
		foreach (WorldPosition worldPosition in WorldPosition.ParseTilesAtRange(element, 1, this.WorldPositionningService.World.WorldParameters))
		{
			if (worldPosition.IsValid)
			{
				if (this.WorldPositionningService.IsExploitable(worldPosition, 0))
				{
					if ((int)this.WorldPositionningService.GetRegionIndex(worldPosition) == regionIndex)
					{
						flag |= this.WorldPositionningService.IsOceanTile(worldPosition);
						worldPositionScore.Add(this.GetWorldPositionScore(empire.Index, worldPosition), 1f);
					}
				}
			}
		}
		foreach (WorldPosition worldPosition2 in WorldPosition.ParseTilesAtRange(element, 1, this.WorldPositionningService.World.WorldParameters))
		{
			if (worldPosition2.IsValid)
			{
				if (this.WorldPositionningService.IsExploitable(worldPosition2, 0))
				{
					if ((int)this.WorldPositionningService.GetRegionIndex(worldPosition2) == regionIndex)
					{
						worldPositionScore.Add(this.ComputeExpansionScoreAtPosition(empire, null, element, worldPosition2), this.secondRangePercent);
						worldPositionScore.NewDistrictNeighbourgNumber++;
						if (!this.WorldPositionningService.IsWaterTile(worldPosition2))
						{
							worldPositionScore.NewDistrictNotWaterNeighbourNumber++;
						}
					}
				}
			}
		}
		this.ComputeCountByOrientation(element, ref worldPositionScore.CountByOrientation);
		worldPositionScore.HasCostalTile = flag;
		return worldPositionScore;
	}

	private WorldPositionScore ComputeExpansionScoreAtPosition(global::Empire empire, City city, WorldPosition centerPosition, WorldPosition element)
	{
		if (!element.IsValid)
		{
			return null;
		}
		WorldPositionScore worldPositionScore = new WorldPositionScore(element, this.fimse);
		int regionIndex = (int)this.WorldPositionningService.GetRegionIndex(element);
		int num = 0;
		bool flag = false;
		for (int i = 0; i < 6; i++)
		{
			WorldPosition neighbourTile = this.WorldPositionningService.GetNeighbourTile(element, (WorldOrientation)i, 1);
			if (neighbourTile.IsValid)
			{
				if ((int)this.WorldPositionningService.GetRegionIndex(neighbourTile) != regionIndex)
				{
					worldPositionScore.SumOfLostTiles++;
				}
				else if (!this.WorldPositionningService.IsExploitable(neighbourTile, 0))
				{
					worldPositionScore.SumOfLostTiles++;
				}
				else
				{
					if (city != null)
					{
						District district = this.WorldPositionningService.GetDistrict(neighbourTile);
						if (district != null)
						{
							if (district.Type == DistrictType.Exploitation)
							{
								goto IL_12B;
							}
							float propertyValue = district.GetPropertyValue(SimulationProperties.NumberOfExtensionAround);
							if (propertyValue < (float)DepartmentOfTheInterior.MinimumNumberOfExtensionNeighbourForLevelUp)
							{
								num += (int)propertyValue;
								goto IL_12B;
							}
							goto IL_12B;
						}
					}
					flag |= this.WorldPositionningService.IsOceanTile(neighbourTile);
					worldPositionScore.Add(this.GetWorldPositionScore(empire.Index, neighbourTile), 1f);
					worldPositionScore.NewDistrictNeighbourgNumber++;
					if (!this.WorldPositionningService.IsWaterTile(neighbourTile))
					{
						worldPositionScore.NewDistrictNotWaterNeighbourNumber++;
					}
				}
			}
			IL_12B:;
		}
		worldPositionScore.SumOfNumberOfExtensionAround = num;
		worldPositionScore.HasCostalTile = flag;
		return worldPositionScore;
	}

	private WorldPositionScore ComputeCreepingNodeImprovementScoreAtPosition(global::Empire empire, City city, PointOfInterest creepingNodePOI, CreepingNodeImprovementDefinition creepingNodeImprovementDefinition)
	{
		if (creepingNodePOI == null)
		{
			return null;
		}
		if (creepingNodeImprovementDefinition == null)
		{
			return null;
		}
		WorldPosition worldPosition = creepingNodePOI.WorldPosition;
		if (!worldPosition.IsValid)
		{
			return null;
		}
		WorldPositionScore worldPositionScore = new WorldPositionScore(worldPosition, this.fimse);
		int regionIndex = (int)this.WorldPositionningService.GetRegionIndex(worldPosition);
		int fidsiextractionRange = creepingNodeImprovementDefinition.FIDSIExtractionRange;
		WorldCircle worldCircle = new WorldCircle(worldPosition, fidsiextractionRange);
		WorldPosition[] worldPositions = worldCircle.GetWorldPositions(this.WorldPositionningService.World.WorldParameters);
		int num = 0;
		bool flag = false;
		foreach (WorldPosition worldPosition2 in worldPositions)
		{
			if (worldPosition2.IsValid)
			{
				if ((int)this.WorldPositionningService.GetRegionIndex(worldPosition2) != regionIndex)
				{
					worldPositionScore.SumOfLostTiles++;
				}
				else if (!this.WorldPositionningService.IsExploitable(worldPosition2, 0))
				{
					worldPositionScore.SumOfLostTiles++;
				}
				else
				{
					if (city != null)
					{
						District district = this.WorldPositionningService.GetDistrict(worldPosition2);
						if (district != null)
						{
							if (district.Type != DistrictType.Exploitation)
							{
								float propertyValue = district.GetPropertyValue(SimulationProperties.NumberOfExtensionAround);
								if (propertyValue < (float)DepartmentOfTheInterior.MinimumNumberOfExtensionNeighbourForLevelUp)
								{
									num += (int)propertyValue;
								}
							}
							goto IL_18F;
						}
					}
					flag |= this.WorldPositionningService.IsWaterTile(worldPosition2);
					worldPositionScore.Add(this.GetWorldPositionScore(empire.Index, worldPosition2), 1f);
					worldPositionScore.NewDistrictNeighbourgNumber++;
					if (!this.WorldPositionningService.IsWaterTile(worldPosition2))
					{
						worldPositionScore.NewDistrictNotWaterNeighbourNumber++;
					}
				}
			}
			IL_18F:;
		}
		worldPositionScore.SumOfNumberOfExtensionAround = num;
		worldPositionScore.HasCostalTile = flag;
		return worldPositionScore;
	}

	private void GenerateDistrictProxy(int empireIndex, WorldPosition worldPosition, SimulationObject districtProxy)
	{
		districtProxy.RemoveAllDescriptors_ModifierForwardType_ChildrenOnly();
		if (this.WorldPositionningService != null)
		{
			DepartmentOfTheInterior.ApplyDistrictProxyDescriptors(base.Game.Empires[empireIndex], districtProxy, worldPosition, DistrictType.Exploitation, true, false);
			districtProxy.Refresh();
		}
	}

	private WorldPositionScore GetWorldPositionScore(int empireIndex, WorldPosition worldPosition)
	{
		WorldPositionScore worldPositionScore = this.scoreByWorldPositionByEmpires[empireIndex].GetValue(worldPosition);
		if (worldPositionScore == null)
		{
			this.GenerateDistrictProxy(empireIndex, worldPosition, this.districtProxyByEmpires[empireIndex]);
			worldPositionScore = new WorldPositionScore();
			worldPositionScore.Scores = new AIParameterDefinition[this.fimse.Length];
			worldPositionScore.WorldPosition = worldPosition;
			for (int i = 0; i < this.fimse.Length; i++)
			{
				worldPositionScore.Scores[i] = new AIParameterDefinition();
				worldPositionScore.Scores[i].Name = this.fimse[i];
				worldPositionScore.Scores[i].Value = this.districtProxyByEmpires[empireIndex].GetPropertyValue(this.fimse[i]);
			}
			this.scoreByWorldPositionByEmpires[empireIndex].SetValue(worldPosition, worldPositionScore);
		}
		return worldPositionScore;
	}

	private void InitializeAIParameter(IServiceContainer serviceContainer, global::Game game)
	{
		this.WorldPositionningService = game.Services.GetService<IWorldPositionningService>();
		this.SimulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
		int num = 0;
		for (int i = 0; i < game.Empires.Length; i++)
		{
			if (game.Empires[i] is MinorEmpire)
			{
				break;
			}
			num++;
		}
		this.scoreByWorldPositionByEmpires = new GridMap<WorldPositionScore>[num];
		this.cityProxyByEmpires = new SimulationObject[num];
		this.districtProxyByEmpires = new SimulationObject[num];
		SimulationDescriptor value = this.SimulationDescriptorDatabase.GetValue("ClassCity");
		for (int j = 0; j < num; j++)
		{
			this.scoreByWorldPositionByEmpires[j] = new GridMap<WorldPositionScore>("PositionScore", (int)this.WorldPositionningService.World.WorldParameters.Columns, (int)this.WorldPositionningService.World.WorldParameters.Rows, null);
			this.cityProxyByEmpires[j] = new SimulationObject("WorldPositionEvaluation.CityProxy#" + j);
			this.cityProxyByEmpires[j].ModifierForward = ModifierForwardType.ChildrenOnly;
			this.cityProxyByEmpires[j].AddDescriptor(value);
			this.districtProxyByEmpires[j] = new SimulationObject("WorldPositionEvaluation.DistrictProxy#" + j);
			this.districtProxyByEmpires[j].ModifierForward = ModifierForwardType.ChildrenOnly;
			this.cityProxyByEmpires[j].AddChild_ModifierForwardType_ChildrenOnly(this.districtProxyByEmpires[j]);
			game.Empires[j].SimulationObject.AddChild_ModifierForwardType_ChildrenOnly(this.cityProxyByEmpires[j]);
		}
	}

	private bool IsPositionExploitedByCity(City city, WorldPosition position)
	{
		for (int i = 0; i < city.Districts.Count; i++)
		{
			if (city.Districts[i].WorldPosition == position)
			{
				return true;
			}
		}
		return false;
	}

	private void ReleaseAIParameter()
	{
		if (this.scoreByWorldPositionByEmpires != null)
		{
			for (int i = 0; i < this.scoreByWorldPositionByEmpires.Length; i++)
			{
				this.scoreByWorldPositionByEmpires[i] = null;
				this.districtProxyByEmpires[i].Dispose();
				this.cityProxyByEmpires[i].Dispose();
			}
			this.scoreByWorldPositionByEmpires = null;
			this.districtProxyByEmpires = null;
			this.cityProxyByEmpires = null;
		}
		this.WorldPositionningService = null;
		this.SimulationDescriptorDatabase = null;
	}

	private bool IsExtensionInterestingAt(WorldPosition extensionPosition, WorldOrientation currentDirection, int localRow, int centerRegionIndex)
	{
		WorldPosition neighbourTile = this.WorldPositionningService.GetNeighbourTile(extensionPosition, currentDirection, 1);
		if (!this.WorldPositionningService.IsExploitable(neighbourTile, 0) || (int)this.WorldPositionningService.GetRegionIndex(neighbourTile) != centerRegionIndex)
		{
			return false;
		}
		if (localRow == 0 || localRow == 1)
		{
			neighbourTile = this.WorldPositionningService.GetNeighbourTile(extensionPosition, currentDirection.Rotate(1), 1);
			if (!this.WorldPositionningService.IsExploitable(neighbourTile, 0) || (int)this.WorldPositionningService.GetRegionIndex(neighbourTile) != centerRegionIndex)
			{
				return false;
			}
		}
		if (localRow == 0 || localRow == -1)
		{
			neighbourTile = this.WorldPositionningService.GetNeighbourTile(extensionPosition, currentDirection.Rotate(-1), 1);
			if (!this.WorldPositionningService.IsExploitable(neighbourTile, 0) || (int)this.WorldPositionningService.GetRegionIndex(neighbourTile) != centerRegionIndex)
			{
				return false;
			}
		}
		return true;
	}

	private bool IsTileValidForExtension(WorldPosition extensionPosition, WorldOrientation currentDirection, int centerRegionIndex, int localRow)
	{
		if ((int)this.WorldPositionningService.GetRegionIndex(extensionPosition) != centerRegionIndex)
		{
			return false;
		}
		District district = this.WorldPositionningService.GetDistrict(extensionPosition);
		return (district != null && District.IsACityTile(district)) || (this.WorldPositionningService.IsExtensionConstructible(extensionPosition, false) && this.IsExtensionInterestingAt(extensionPosition, currentDirection, localRow, centerRegionIndex));
	}

	public IEnumerable<IAIParameterConverter<InterpreterContext>> GetAIParameterConverters(StaticString aiParameterName)
	{
		Diagnostics.Assert(this.aiParameterConverterDatabase != null);
		AIParameterConverter aiParameterConverter;
		if (!this.aiParameterConverterDatabase.TryGetValue(aiParameterName, out aiParameterConverter))
		{
			yield break;
		}
		Diagnostics.Assert(aiParameterConverter != null);
		if (aiParameterConverter.ToAIParameters == null)
		{
			yield break;
		}
		for (int index = 0; index < aiParameterConverter.ToAIParameters.Length; index++)
		{
			yield return aiParameterConverter.ToAIParameters[index];
		}
		yield break;
	}

	public IEnumerable<IAIParameter<InterpreterContext>> GetAIParameters(WorldPositionScore element)
	{
		for (int index = 0; index < element.Scores.Length; index++)
		{
			yield return element.Scores[index];
		}
		yield break;
	}

	public IEnumerable<IAIPrerequisite<InterpreterContext>> GetAIPrerequisites(WorldPositionScore element)
	{
		yield break;
	}

	public void ComputeBarycenter(WorldPosition[] listOfPosition, out WorldPosition barycenter)
	{
		WorldPosition invalid = WorldPosition.Invalid;
		if (listOfPosition.Length > 0)
		{
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < listOfPosition.Length; i++)
			{
				num += (int)listOfPosition[i].Column;
				num2 += (int)listOfPosition[i].Row;
			}
			invalid.Row = (short)(num2 / listOfPosition.Length);
			invalid.Column = (short)(num / listOfPosition.Length);
		}
		barycenter = invalid;
	}

	public void ComputeClosestPositionInList(WorldPosition[] listOfPosition, WorldPosition origin, out WorldPosition final)
	{
		this.ComputeClosestPositionInList(listOfPosition, origin, PathfindingMovementCapacity.Ground, out final);
	}

	public void ComputeClosestPositionInList(WorldPosition[] listOfPosition, WorldPosition origin, PathfindingMovementCapacity pathfindingCapacity, out WorldPosition final)
	{
		WorldPosition worldPosition = WorldPosition.Invalid;
		if (listOfPosition.Length > 0)
		{
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
			Diagnostics.Assert(service2 != null);
			IPathfindingService service3 = service.Game.Services.GetService<IPathfindingService>();
			float num = float.MaxValue;
			for (int i = 0; i < listOfPosition.Length; i++)
			{
				if (service3.IsTilePassable(listOfPosition[i], pathfindingCapacity, (PathfindingFlags)0) && service3.IsTileStopable(listOfPosition[i], pathfindingCapacity, (PathfindingFlags)0))
				{
					float num2 = (float)service2.GetDistance(listOfPosition[i], origin);
					if (num2 < num)
					{
						worldPosition = listOfPosition[i];
						num = num2;
					}
				}
			}
		}
		final = worldPosition;
	}

	public void ComputeFarestPositionInList(WorldPosition[] listOfPosition, WorldPosition origin, out WorldPosition final)
	{
		this.ComputeFarestPositionInList(listOfPosition, origin, PathfindingMovementCapacity.Ground, out final);
	}

	public void ComputeFarestPositionInList(WorldPosition[] listOfPosition, WorldPosition origin, PathfindingMovementCapacity pathfindingCapacity, out WorldPosition final)
	{
		WorldPosition worldPosition = WorldPosition.Invalid;
		if (listOfPosition.Length > 0)
		{
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
			Diagnostics.Assert(service2 != null);
			IPathfindingService service3 = service.Game.Services.GetService<IPathfindingService>();
			float num = 0f;
			for (int i = 0; i < listOfPosition.Length; i++)
			{
				if (service3.IsTilePassable(listOfPosition[i], pathfindingCapacity, (PathfindingFlags)0) && service3.IsTileStopable(listOfPosition[i], pathfindingCapacity, (PathfindingFlags)0))
				{
					float num2 = (float)service2.GetDistance(listOfPosition[i], origin);
					if (num2 > num)
					{
						worldPosition = listOfPosition[i];
						num = num2;
					}
				}
			}
		}
		final = worldPosition;
	}

	public override IEnumerator Initialize(IServiceContainer serviceContainer, global::Game game)
	{
		yield return base.Initialize(serviceContainer, game);
		this.aiParameterConverterDatabase = Databases.GetDatabase<AIParameterConverter>(false);
		serviceContainer.AddService<IWorldPositionEvaluationAIHelper>(this);
		this.worldAtlas = game.World.Atlas;
		Diagnostics.Assert(this.worldAtlas != null);
		this.InitializeAIParameter(serviceContainer, game);
		yield break;
	}

	public bool IsPositionInRange(WorldPosition origin, WorldPosition destination, int range)
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(service2 != null);
		int distance = service2.GetDistance(origin, destination);
		return distance <= range;
	}

	public override void Release()
	{
		base.Release();
		this.ReleaseAIParameter();
		this.worldAtlas = null;
	}

	private SimulationObject[] cityProxyByEmpires;

	private SimulationObject[] districtProxyByEmpires;

	private StaticString[] fimse = new StaticString[]
	{
		SimulationProperties.DistrictFood,
		SimulationProperties.DistrictIndustry,
		SimulationProperties.DistrictScience,
		SimulationProperties.DistrictDust,
		SimulationProperties.DistrictCityPoint,
		SimulationProperties.CityApproval
	};

	private GridMap<WorldPositionScore>[] scoreByWorldPositionByEmpires;

	private float secondRangePercent = 0.05f;

	private WorldAtlas worldAtlas;

	private IDownloadableContentService downloadableContentService;

	private IDatabase<AIParameterConverter> aiParameterConverterDatabase;
}
