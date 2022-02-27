using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.AI;
using Amplitude.Unity.AI.Decision;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class WorldAtlasHelper : AIHelper, IWorldAtlasAIHelper, IService, ISimulationAIEvaluationHelper<Region>, IAIEvaluationHelper<Region, InterpreterContext>
{
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

	public IEnumerable<IAIParameter<InterpreterContext>> GetAIParameters(Region regionElement)
	{
		IDatabase<AIParameterDatatableElement> aiParameterDatabase = Databases.GetDatabase<AIParameterDatatableElement>(false);
		Diagnostics.Assert(aiParameterDatabase != null);
		IWorldAtlasAIHelper worldAtlasHelper = AIScheduler.Services.GetService<IWorldAtlasAIHelper>();
		Diagnostics.Assert(worldAtlasHelper != null);
		StaticString biomeName = worldAtlasHelper.GetRegionBiome(regionElement);
		Diagnostics.Assert(!StaticString.IsNullOrEmpty(biomeName));
		IAIParameter<InterpreterContext>[] aiParameters;
		if (!this.aiParametersByBiomeName.TryGetValue(biomeName, out aiParameters))
		{
			AIParameterDatatableElement aiParameterDatatableElement;
			if (aiParameterDatabase.TryGetValue(biomeName, out aiParameterDatatableElement))
			{
				Diagnostics.Assert(aiParameterDatatableElement != null);
				if (aiParameterDatatableElement.AIParameters != null)
				{
					aiParameters = new IAIParameter<InterpreterContext>[aiParameterDatatableElement.AIParameters.Length];
					for (int index = 0; index < aiParameterDatatableElement.AIParameters.Length; index++)
					{
						AIParameterDatatableElement.AIParameter parameterDefinition = aiParameterDatatableElement.AIParameters[index];
						Diagnostics.Assert(parameterDefinition != null);
						aiParameters[index] = parameterDefinition.Instantiate();
					}
				}
				else
				{
					Diagnostics.LogWarning("Biome {0} has no AI Parameters.", new object[]
					{
						biomeName
					});
				}
			}
			else
			{
				Diagnostics.LogWarning("Biome {0} has no AI Parameters.", new object[]
				{
					biomeName
				});
			}
			this.aiParametersByBiomeName.Add(biomeName, aiParameters);
		}
		if (aiParameters != null)
		{
			for (int index2 = 0; index2 < aiParameters.Length; index2++)
			{
				yield return aiParameters[index2];
			}
		}
		if (this.regionEvaluationAiParameters == null)
		{
			AIParameterDatatableElement aiParameterDatatableElement2;
			if (aiParameterDatabase.TryGetValue("RegionEvaluation", out aiParameterDatatableElement2))
			{
				if (aiParameterDatatableElement2.AIParameters != null)
				{
					this.regionEvaluationAiParameters = new IAIParameter<InterpreterContext>[aiParameterDatatableElement2.AIParameters.Length];
					for (int index3 = 0; index3 < aiParameterDatatableElement2.AIParameters.Length; index3++)
					{
						AIParameterDatatableElement.AIParameter parameterDefinition2 = aiParameterDatatableElement2.AIParameters[index3];
						Diagnostics.Assert(parameterDefinition2 != null);
						this.regionEvaluationAiParameters[index3] = parameterDefinition2.Instantiate();
					}
				}
				else
				{
					Diagnostics.LogWarning("RegionEvaluation has no AI Parameters.");
				}
			}
			else
			{
				Diagnostics.LogWarning("RegionEvaluation has no AI Parameters.");
			}
		}
		for (int index4 = 0; index4 < this.regionEvaluationAiParameters.Length; index4++)
		{
			yield return this.regionEvaluationAiParameters[index4];
		}
		yield break;
	}

	public IEnumerable<IAIPrerequisite<InterpreterContext>> GetAIPrerequisites(Region regionElement)
	{
		if (regionElement == null)
		{
			throw new ArgumentNullException("regionElement");
		}
		yield break;
	}

	public override void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		base.ReadXml(reader);
		if (num >= 2)
		{
			reader.ReadStartElement("AIRegionDataByEmpire");
			for (int i = 0; i < this.regionDataByEmpires.Length; i++)
			{
				int attribute = reader.GetAttribute<int>("Count");
				reader.ReadStartElement("AIRegionDataList");
				for (int j = 0; j < attribute; j++)
				{
					AIRegionData airegionData = this.regionDataByEmpires[i][j];
					if (airegionData != null && airegionData != null)
					{
						reader.ReadElementSerializable<AIRegionData>(ref airegionData);
					}
					else
					{
						Diagnostics.LogError("AIRegionData in Empire {0}, Region {1} not deserialized", new object[]
						{
							i,
							j
						});
						reader.Skip();
					}
				}
				reader.ReadEndElement("AIRegionDataList");
			}
			reader.ReadEndElement("AIRegionDataByEmpire");
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		int num = writer.WriteVersionAttribute(2);
		base.WriteXml(writer);
		if (num >= 2)
		{
			writer.WriteStartElement("AIRegionDataByEmpire");
			for (int i = 0; i < this.regionDataByEmpires.Length; i++)
			{
				writer.WriteStartElement("AIRegionDataList");
				writer.WriteAttributeString<int>("Count", this.regionDataByEmpires[i].Length);
				for (int j = 0; j < this.regionDataByEmpires[i].Length; j++)
				{
					if (this.regionDataByEmpires[i][j] != null)
					{
						IXmlSerializable xmlSerializable = this.regionDataByEmpires[i][j];
						writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
					}
				}
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}
	}

	public void ComputeRegionCandidateToColonization(global::Empire empire, ref List<int> listOfRegionToPacify)
	{
		List<AIRegionData> list = this.regionDataByEmpires[empire.Index].ToList<AIRegionData>();
		list.RemoveAll((AIRegionData match) => match.ColonizationPreference == 0f);
		if (list.Count == 0)
		{
			return;
		}
		list.Sort((AIRegionData left, AIRegionData right) => -1 * left.ColonizationPreference.CompareTo(right.ColonizationPreference));
		for (int i = 0; i < list.Count; i++)
		{
			listOfRegionToPacify.Add(list[i].RegionIndex);
		}
	}

	public float GetBestColonizePreferenceInRegionData(int empireIndex)
	{
		float num = 0f;
		AIRegionData[] array = this.regionDataByEmpires[empireIndex];
		for (int i = 0; i < array.Length; i++)
		{
			float colonizationPreference = array[i].ColonizationPreference;
			if (num < colonizationPreference)
			{
				num = colonizationPreference;
			}
		}
		return num;
	}

	public float GetCommonBorderRatio(global::Empire empire, global::Empire otherEmpire)
	{
		if (empire == null)
		{
			throw new ArgumentNullException("empire");
		}
		if (otherEmpire == null)
		{
			throw new ArgumentNullException("otherEmpire");
		}
		int num = 0;
		int num2 = 0;
		foreach (AIRegionData airegionData in this.regionDataByEmpires[empire.Index])
		{
			Region region = this.worldPositionService.GetRegion(airegionData.RegionIndex);
			global::Empire owner = region.Owner;
			if (owner != null && owner.Index == empire.Index)
			{
				num += airegionData.BorderByEmpireIndex[otherEmpire.Index];
				num2 += airegionData.OverallBorderSize - airegionData.BorderByEmpireIndex[empire.Index];
			}
		}
		if (num2 == 0)
		{
			return 0f;
		}
		float num3 = (float)num / (float)num2;
		Diagnostics.Assert(num3 >= 0f && num3 <= 1f);
		return num3;
	}

	public int GetEmpireBorderSize(int empireIndex)
	{
		return this.empireBorderSize[empireIndex];
	}

	public AIRegionData GetRegionData(int empireIndex, int regionIndex)
	{
		return this.regionDataByEmpires[empireIndex][regionIndex];
	}

	public AIRegionData[] GetRegionData(int empireIndex)
	{
		return this.regionDataByEmpires[empireIndex];
	}

	public float GetRegionExplorationRatio(global::Empire empire, Region region)
	{
		AIRegionData airegionData = this.regionDataByEmpires[empire.Index][region.Index];
		return airegionData.ExplorationRatio;
	}

	public float GetRegionExplorationRatio(int empireIndex, int regionIndex)
	{
		AIRegionData airegionData = this.regionDataByEmpires[empireIndex][regionIndex];
		return airegionData.ExplorationRatio;
	}

	public AISafetyData GetSafetyData(global::Empire empire)
	{
		if (empire.Index >= this.safetyDataByEmpires.Length)
		{
			return null;
		}
		Diagnostics.Assert(this.safetyDataByEmpires != null);
		return this.safetyDataByEmpires[empire.Index];
	}

	public float GetWorldExplorationRatio(global::Empire empire)
	{
		if (empire == null)
		{
			throw new ArgumentNullException("empire");
		}
		Diagnostics.Assert(this.regionDataByEmpires != null);
		Diagnostics.Assert(this.regionDataByEmpires[empire.Index] != null);
		float num = 0f;
		for (int i = 0; i < this.regionDataByEmpires[empire.Index].Length; i++)
		{
			AIRegionData airegionData = this.regionDataByEmpires[empire.Index][i];
			num += airegionData.ExplorationRatio;
		}
		num /= (float)this.regionDataByEmpires[empire.Index].Length;
		Diagnostics.Assert(num >= 0f && num <= 1f);
		return num;
	}

	public void InitializeRegion(global::Game game)
	{
		this.colonizableRegionCount = 0;
		for (int i = 0; i < this.world.Regions.Length; i++)
		{
			Region region = this.world.Regions[i];
			if (region.IsLand)
			{
				this.colonizableRegionCount++;
			}
		}
		this.visibilityService = base.Game.GetService<IVisibilityService>();
		Diagnostics.Assert(this.visibilityService != null);
		this.pathfindingService = base.Game.Services.GetService<IPathfindingService>();
		Diagnostics.Assert(this.pathfindingService != null);
		this.worldPositionningService = base.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		int num = 0;
		for (int j = 0; j < game.Empires.Length; j++)
		{
			if (game.Empires[j] is MajorEmpire)
			{
				num++;
			}
		}
		this.empireBorderSize = new int[num];
		this.regionDataByEmpires = new AIRegionData[num][];
		for (int k = 0; k < this.regionDataByEmpires.Length; k++)
		{
			this.regionDataByEmpires[k] = new AIRegionData[game.World.Regions.Length];
			for (int l = 0; l < game.World.Regions.Length; l++)
			{
				this.regionDataByEmpires[k][l] = new AIRegionData(this, k, game.World.Regions[l], num);
			}
		}
		this.safetyDataByEmpires = new AISafetyData[num];
		for (int m = 0; m < this.safetyDataByEmpires.Length; m++)
		{
			this.safetyDataByEmpires[m] = new AISafetyData();
		}
	}

	public bool IsRegionColonized(global::Empire empire, Region region)
	{
		AIRegionData airegionData = this.regionDataByEmpires[empire.Index][region.Index];
		return airegionData.IsColonizedByMe;
	}

	public bool IsRegionColonizedByEmpire(int regionIndex, int empireIndex)
	{
		AIRegionData airegionData = this.regionDataByEmpires[empireIndex][regionIndex];
		return airegionData.IsColonizedByMe;
	}

	public bool IsRegionColonizedBySomeone(Region region)
	{
		return this.IsRegionColonizedBySomeone(region.Index);
	}

	public bool IsRegionColonizedBySomeone(int regionIndex)
	{
		for (int i = 0; i < this.regionDataByEmpires.Length; i++)
		{
			AIRegionData airegionData = this.regionDataByEmpires[i][regionIndex];
			if (airegionData.IsColonizedByMe)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsRegionExplored(global::Empire empire, Region region, float explorationRatioToReach)
	{
		return this.IsRegionExplored(empire.Index, region.Index, explorationRatioToReach);
	}

	public bool IsRegionExplored(int empireIndex, int regionIndex, float explorationRatioToReach)
	{
		AIRegionData airegionData = this.regionDataByEmpires[empireIndex][regionIndex];
		return airegionData.ExplorationRatio >= explorationRatioToReach;
	}

	public bool IsRegionExploredBySomeone(Region region, float explorationRatioToReach)
	{
		for (int i = 0; i < this.regionDataByEmpires.Length; i++)
		{
			AIRegionData airegionData = this.regionDataByEmpires[i][region.Index];
			if (airegionData.ExplorationRatio >= explorationRatioToReach)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsRegionPacified(global::Empire empire, Region region)
	{
		return this.IsRegionPacified(empire.Index, region.Index);
	}

	public bool IsRegionPacified(int empireIndex, int regionIndex)
	{
		AIRegionData airegionData = this.regionDataByEmpires[empireIndex][regionIndex];
		return airegionData != null && airegionData.VillageConvertedByOtherCount + airegionData.VillagePacifiedOrConvertedByMeCount == airegionData.VillageTotalCount;
	}

	public bool IsTileTaggedAsExplored(int empireIndex, int regionIndex, WorldPosition position)
	{
		AIRegionData airegionData = this.regionDataByEmpires[empireIndex][regionIndex];
		return airegionData.TilesTaggedAsExplored.Contains(position);
	}

	public void RefreshColonizationPreference(int empireIndex, Region region, float preference)
	{
		AIRegionData airegionData = this.regionDataByEmpires[empireIndex][region.Index];
		airegionData.ColonizationPreference = preference;
	}

	public void ResetColonizePreferenceInRegionData(int empireIndex)
	{
		AIRegionData[] array = this.regionDataByEmpires[empireIndex];
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ColonizationPreference = 0f;
		}
	}

	public void TagTileAsExplored(int empireIndex, int regionIndex, WorldPosition position)
	{
		if (!this.IsTileTaggedAsExplored(empireIndex, regionIndex, position))
		{
			AIRegionData airegionData = this.regionDataByEmpires[empireIndex][regionIndex];
			airegionData.TilesTaggedAsExplored.Add(position);
		}
	}

	public void UpdateRegionData()
	{
		for (int i = 0; i < this.regionDataByEmpires.Length; i++)
		{
			global::Empire empire = base.Game.Empires[i];
			this.empireBorderSize[i] = 0;
			for (int j = 0; j < this.regionDataByEmpires[i].Length; j++)
			{
				AIRegionData airegionData = this.regionDataByEmpires[i][j];
				Region region = base.Game.World.Regions[j];
				airegionData.HarassingScore = 0f;
				this.UpdateColonizationStatusOfRegion(empire, region, airegionData);
				this.UpdateExplorationRatioOfRegion(empire, region, airegionData);
				this.UpdatePacificationStatusOfRegion(empire, region, airegionData);
				this.UpdateBorderStatusOfRegion(empire, region, airegionData);
				this.UpdatePointOfInterestStatus(empire, region, airegionData);
				this.empireBorderSize[i] += airegionData.BorderWithMe;
			}
			AISafetyData safetyData = this.safetyDataByEmpires[i];
			if (this.UpdateSafetyData(empire, safetyData))
			{
				for (int k = 0; k < this.regionDataByEmpires[i].Length; k++)
				{
					AIRegionData airegionData2 = this.regionDataByEmpires[i][k];
					airegionData2.NeedToRecomputePathes();
				}
			}
		}
	}

	public bool UpdateRegionDataPathes(AIRegionData regionData)
	{
		global::Empire empire = base.Game.Empires[regionData.EmpireIndex];
		DepartmentOfScience agency = empire.GetAgency<DepartmentOfScience>();
		if (empire.GetAgency<DepartmentOfTheInterior>().Cities.Count <= 0)
		{
			return true;
		}
		Region region = this.world.Regions[regionData.RegionIndex];
		PathfindingContext pathfindingContext = new PathfindingContext(GameEntityGUID.Zero, null, (!agency.HaveResearchedShipTechnology()) ? (PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.FrozenWater) : (PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.Water | PathfindingMovementCapacity.FrozenWater));
		pathfindingContext.RefreshProperties(1f, float.PositiveInfinity, false, false, float.PositiveInfinity, float.PositiveInfinity);
		pathfindingContext.Greedy = true;
		PathfindingFlags flags = PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreEncounterAreas | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnoreZoneOfControl;
		regionData.NormalPath = null;
		foreach (AIRegionData airegionData in this.regionDataByEmpires[empire.Index])
		{
			if (airegionData.IsColonizedByMe)
			{
				Region region2 = this.world.Regions[airegionData.RegionIndex];
				PathfindingResult pathfindingResult = this.pathfindingService.FindPath(pathfindingContext, region2.City.WorldPosition, region.Barycenter, PathfindingManager.RequestMode.Default, null, flags, null);
				if (pathfindingResult != null && (regionData.NormalPath == null || regionData.NormalPath.CompletPathLength > pathfindingResult.CompletPathLength))
				{
					regionData.NormalPath = pathfindingResult;
				}
			}
		}
		AISafetyData aisafetyData = this.safetyDataByEmpires[regionData.EmpireIndex];
		if (regionData.NormalPath != null && aisafetyData.UnsafeRegionIndexes.Count > 0)
		{
			bool flag = false;
			foreach (WorldPosition position in regionData.NormalPath.GetCompletePath())
			{
				int regionIndex = (int)this.worldPositionningService.GetRegionIndex(position);
				if (aisafetyData.UnsafeRegionIndexes.Contains(regionIndex))
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				if (!aisafetyData.UnsafeRegionIndexes.Contains(region.Index))
				{
					regionData.SafePath = null;
					foreach (AIRegionData airegionData2 in this.regionDataByEmpires[empire.Index])
					{
						if (airegionData2.IsColonizedByMe)
						{
							Region region3 = this.world.Regions[airegionData2.RegionIndex];
							PathfindingResult pathfindingResult2 = this.pathfindingService.FindPath(pathfindingContext, region3.City.WorldPosition, region.Barycenter, PathfindingManager.RequestMode.Default, aisafetyData.SafePathfindingContext, flags, null);
							if (pathfindingResult2 != null && (regionData.SafePath == null || regionData.SafePath.CompletPathLength > pathfindingResult2.CompletPathLength))
							{
								regionData.SafePath = pathfindingResult2;
							}
						}
					}
				}
				else
				{
					regionData.SafePath = null;
				}
			}
			else
			{
				regionData.SafePath = regionData.NormalPath;
			}
		}
		return false;
	}

	private void UpdateBorderStatusOfRegion(global::Empire empire, Region region, AIRegionData regionData)
	{
		DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
		regionData.BorderWithEnnemy = 0;
		regionData.BorderWithAllied = 0;
		regionData.BorderWithMe = 0;
		regionData.BorderWithNeutral = 0;
		regionData.OverallBorderSize = 0;
		for (int i = 0; i < regionData.BorderByEmpireIndex.Length; i++)
		{
			regionData.BorderByEmpireIndex[i] = 0;
		}
		for (int j = 0; j < region.Borders.Length; j++)
		{
			int num = region.Borders[j].WorldPositions.Length;
			regionData.OverallBorderSize += num;
			Region region2 = this.worldPositionService.GetRegion(region.Borders[j].NeighbourRegionIndex);
			global::Empire owner = region2.Owner;
			if (owner != null && owner is MajorEmpire)
			{
				regionData.BorderByEmpireIndex[owner.Index] += num;
				if (owner == empire)
				{
					regionData.BorderWithMe += num;
				}
				else if (agency != null)
				{
					DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(owner);
					if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.ColdWar || diplomaticRelation.State.Name == DiplomaticRelationState.Names.War)
					{
						regionData.BorderWithEnnemy += num;
					}
					else
					{
						regionData.BorderWithAllied += num;
					}
				}
			}
			else
			{
				regionData.BorderWithNeutral += num;
			}
		}
	}

	private bool UpdateColonizationStatusOfRegion(global::Empire empire, Region region, AIRegionData regionData)
	{
		bool flag = region.City != null && region.City.Empire == empire;
		if (regionData.IsColonizedByMe && !flag)
		{
			regionData.LostByMeAtTurn = base.Game.Turn;
		}
		regionData.IsColonizedByMe = flag;
		return flag;
	}

	private void UpdateDistanceToMyEmpireOfRegion(global::Empire empire, Region region, AIRegionData regionData)
	{
		regionData.MinimalDistanceToMyCities = 2.14748365E+09f;
		if (region.City != null && region.City.Empire == empire)
		{
			regionData.MinimalDistanceToMyCities = 0f;
		}
		DepartmentOfTheInterior agency = empire.GetAgency<DepartmentOfTheInterior>();
		for (int i = 0; i < agency.Cities.Count; i++)
		{
			float num = (float)this.worldPositionningService.GetDistance(region.Barycenter, agency.Cities[i].WorldPosition);
			if (regionData.MinimalDistanceToMyCities > num)
			{
				regionData.MinimalDistanceToMyCities = num;
			}
		}
	}

	private void UpdateExplorationRatioOfRegion(global::Empire empire, Region region, AIRegionData regionData)
	{
		if (empire == null)
		{
			throw new ArgumentNullException("empire");
		}
		if (region == null)
		{
			throw new ArgumentNullException("region");
		}
		if (region.WorldPositions.Length == 0)
		{
			regionData.ExplorationRatio = 1f;
			return;
		}
		int num = 0;
		for (int i = 0; i < region.WorldPositions.Length; i++)
		{
			if (this.visibilityService.IsWorldPositionExploredFor(region.WorldPositions[i], empire))
			{
				num++;
			}
		}
		regionData.ExplorationRatio = (float)num / (float)region.WorldPositions.Length;
	}

	private void UpdatePacificationStatusOfRegion(global::Empire empire, Region region, AIRegionData regionData)
	{
		regionData.VillageTotalCount = 0;
		regionData.VillagePacifiedOrConvertedByMeCount = 0;
		regionData.VillageConvertedByOtherCount = 0;
		regionData.VillageNotPacified = 0;
		regionData.VillageDestroyed = 0;
		regionData.VillagePacifiedAndBuilt = 0;
		if (region.MinorEmpire != null)
		{
			BarbarianCouncil agency = region.MinorEmpire.GetAgency<BarbarianCouncil>();
			if (agency != null)
			{
				regionData.VillageTotalCount = agency.Villages.Count;
				for (int i = 0; i < agency.Villages.Count; i++)
				{
					Village village = agency.Villages[i];
					if (village.HasBeenConverted)
					{
						if (village.Converter.Index != empire.Index)
						{
							regionData.VillageConvertedByOtherCount++;
							regionData.VillageNotPacified++;
						}
						else
						{
							regionData.VillagePacifiedOrConvertedByMeCount++;
							regionData.VillagePacifiedAndBuilt++;
						}
					}
					else if (village.HasBeenPacified)
					{
						regionData.VillagePacifiedOrConvertedByMeCount++;
						if (village.PointOfInterest.PointOfInterestImprovement == null)
						{
							regionData.VillageDestroyed++;
						}
						else
						{
							regionData.VillagePacifiedAndBuilt++;
						}
					}
					else
					{
						regionData.VillageNotPacified++;
					}
				}
			}
		}
	}

	private void UpdatePointOfInterestStatus(global::Empire empire, Region region, AIRegionData regionData)
	{
		regionData.NewStrategicRessourcesCount = 0;
		regionData.OwnedStrategicRessourcesCount = 0;
		regionData.ProducedStrategicRessourcesCount = 0;
		regionData.NewLuxuryRessourcesCount = 0;
		regionData.OwnedLuxuryRessourcesCount = 0;
		regionData.ProducedLuxuryRessourcesCount = 0;
		regionData.ResourcePointOfInterestCount = 0;
		regionData.WatchTowerPointOfInterestCount = 0;
		regionData.BuiltExtractor = 0;
		regionData.BuiltWatchTower = 0;
		DepartmentOfTheTreasury agency = empire.GetAgency<DepartmentOfTheTreasury>();
		for (int i = 0; i < region.PointOfInterests.Length; i++)
		{
			PointOfInterest pointOfInterest = region.PointOfInterests[i];
			if (pointOfInterest.Type == "ResourceDeposit")
			{
				this.UpdateResourceStatus(pointOfInterest, empire, region, regionData, agency);
			}
			else if (pointOfInterest.Type == "WatchTower")
			{
				if (pointOfInterest.PointOfInterestImprovement != null)
				{
					regionData.BuiltWatchTower++;
				}
				regionData.WatchTowerPointOfInterestCount++;
			}
		}
	}

	private void UpdateResourceStatus(PointOfInterest pointOfInterest, global::Empire empire, Region region, AIRegionData regionData, DepartmentOfTheTreasury departmentOfTheTreasury)
	{
		regionData.ResourcePointOfInterestCount++;
		if (pointOfInterest.PointOfInterestImprovement != null)
		{
			regionData.BuiltExtractor++;
		}
		if (!DepartmentOfTheInterior.IsPointOfInterestVisible(empire, pointOfInterest))
		{
			return;
		}
		string empty = string.Empty;
		bool condition = pointOfInterest.PointOfInterestDefinition.TryGetValue("ResourceName", out empty);
		Diagnostics.Assert(condition);
		ResourceDefinition.Type resourceType = departmentOfTheTreasury.GetResourceType(empty);
		float num = 0f;
		bool condition2 = departmentOfTheTreasury.TryGetNetResourceValue(empire, empty, out num, true);
		Diagnostics.Assert(condition2);
		if (num > 0f)
		{
			if (resourceType == ResourceDefinition.Type.Luxury)
			{
				regionData.ProducedLuxuryRessourcesCount++;
			}
			else if (resourceType == ResourceDefinition.Type.Strategic)
			{
				regionData.ProducedStrategicRessourcesCount++;
			}
			return;
		}
		float num2 = 0f;
		bool condition3 = departmentOfTheTreasury.TryGetResourceStockValue(empire, empty, out num2, true);
		Diagnostics.Assert(condition3);
		if (num2 > 0f)
		{
			if (resourceType == ResourceDefinition.Type.Luxury)
			{
				regionData.OwnedLuxuryRessourcesCount++;
			}
			else if (resourceType == ResourceDefinition.Type.Strategic)
			{
				regionData.OwnedStrategicRessourcesCount++;
			}
			return;
		}
		if (resourceType == ResourceDefinition.Type.Luxury)
		{
			regionData.NewLuxuryRessourcesCount++;
		}
		else if (resourceType == ResourceDefinition.Type.Strategic)
		{
			regionData.NewStrategicRessourcesCount++;
		}
	}

	private bool UpdateSafetyData(global::Empire empire, AISafetyData safetyData)
	{
		DepartmentOfTheInterior agency = empire.GetAgency<DepartmentOfTheInterior>();
		DepartmentOfForeignAffairs agency2 = empire.GetAgency<DepartmentOfForeignAffairs>();
		DepartmentOfScience agency3 = empire.GetAgency<DepartmentOfScience>();
		if (agency.Cities.Count <= 0)
		{
			return false;
		}
		bool result = false;
		for (int i = 0; i < this.world.Regions.Length; i++)
		{
			Region region = this.world.Regions[i];
			if (region.City != null && region.City.Empire != null)
			{
				global::Empire empire2 = base.Game.Empires[region.City.Empire.Index];
				DiplomaticRelation diplomaticRelation = agency2.GetDiplomaticRelation(empire2);
				if (empire2 != empire && (diplomaticRelation.State.Name == DiplomaticRelationState.Names.Unknown || diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.ColdWarAttackArmies) || diplomaticRelation.HasActiveAbility(DiplomaticAbilityDefinition.AttackArmies)))
				{
					if (safetyData.SafeRegionIndexes.Contains(i))
					{
						safetyData.SafeRegionIndexes.Remove(i);
						result = true;
					}
					if (!safetyData.UnsafeRegionIndexes.Contains(i))
					{
						safetyData.UnsafeRegionIndexes.Add(i);
						result = true;
					}
				}
				else if (empire2 == empire)
				{
					if (safetyData.UnsafeRegionIndexes.Contains(i))
					{
						safetyData.UnsafeRegionIndexes.Remove(i);
						result = true;
					}
					if (!safetyData.SafeRegionIndexes.Contains(i))
					{
						safetyData.SafeRegionIndexes.Add(i);
						result = true;
					}
				}
			}
			else
			{
				if (safetyData.SafeRegionIndexes.Contains(i))
				{
					safetyData.SafeRegionIndexes.Remove(i);
					result = true;
				}
				if (safetyData.UnsafeRegionIndexes.Contains(i))
				{
					safetyData.UnsafeRegionIndexes.Remove(i);
					result = true;
				}
			}
		}
		if (!safetyData.CanGoOnWater && agency3.HaveResearchedShipTechnology())
		{
			safetyData.CanGoOnWater = true;
			result = true;
		}
		return result;
	}

	public float AverageRegionSize
	{
		get
		{
			return this.world.AverageRegionSize;
		}
	}

	public Region[] Regions
	{
		get
		{
			return this.world.Regions;
		}
	}

	public WorldParameters WorldParameters
	{
		get
		{
			return this.world.WorldParameters;
		}
	}

	public int ComputeBirdEyeDistanceBetweenRegionAndEmpire(global::Empire empire, Region region)
	{
		if (this.IsRegionColonizedByEmpire(region.Index, empire.Index))
		{
			return 0;
		}
		Diagnostics.Assert(empire.GetAgency<DepartmentOfTheInterior>().Cities.Count > 0, "Can't compute distance to empire if there isn't any city yet !");
		int num = int.MaxValue;
		foreach (AIRegionData airegionData in this.regionDataByEmpires[empire.Index])
		{
			if (airegionData.IsColonizedByMe)
			{
				Region region2 = this.world.Regions[airegionData.RegionIndex];
				int distance = WorldPosition.GetDistance(region.Barycenter, region2.City.WorldPosition, this.world.WorldParameters.IsCyclicWorld, this.world.WorldParameters.Columns);
				if (distance < num)
				{
					num = distance;
				}
			}
		}
		return num;
	}

	public void ComputeConnectedRegion(global::Empire empire, ref List<int> connectedRegion, Func<Region, bool> match = null)
	{
		DepartmentOfCreepingNodes agency = empire.GetAgency<DepartmentOfCreepingNodes>();
		DepartmentOfTheInterior agency2 = empire.GetAgency<DepartmentOfTheInterior>();
		DepartmentOfForeignAffairs agency3 = empire.GetAgency<DepartmentOfForeignAffairs>();
		Diagnostics.Assert(agency2 != null);
		bool flag = false;
		for (int i = 0; i < agency2.Cities.Count; i++)
		{
			Region region = agency2.Cities[i].Region;
			if (match == null || match(region))
			{
				connectedRegion.Add(region.Index);
			}
			for (int j = 0; j < region.Borders.Length; j++)
			{
				Region region2 = this.world.Regions[region.Borders[j].NeighbourRegionIndex];
				if (region2.IsOcean)
				{
					flag = true;
				}
				if ((region2.City == null || region2.City.Empire != empire) && !connectedRegion.Contains(region2.Index) && (match == null || match(region2)))
				{
					connectedRegion.Add(region2.Index);
				}
			}
		}
		if (agency != null && agency3 != null)
		{
			foreach (CreepingNode creepingNode in agency.Nodes)
			{
				if (!creepingNode.IsUnderConstruction && AILayer_Exploration.IsTravelAllowedInNode(empire, creepingNode) && agency3.CanMoveOn((int)this.worldPositionningService.GetRegionIndex(creepingNode.WorldPosition), false) && !connectedRegion.Contains(creepingNode.Region.Index) && (match == null || match(creepingNode.Region)))
				{
					connectedRegion.Add(creepingNode.Region.Index);
				}
			}
		}
		if (flag)
		{
			for (int k = 0; k < this.world.Continents.Length; k++)
			{
				Continent continent = this.world.Continents[k];
				if (!continent.IsOcean && !continent.IsWasteland)
				{
					bool flag2 = false;
					for (int l = 0; l < agency2.Cities.Count; l++)
					{
						if (agency2.Cities[l].Region.ContinentID == continent.ID)
						{
							flag2 = true;
							break;
						}
					}
					if (!flag2)
					{
						for (int m = 0; m < continent.CostalRegionList.Length; m++)
						{
							Region region3 = this.world.Regions[continent.CostalRegionList[m]];
							if (!connectedRegion.Contains(region3.Index) && (match == null || match(region3)))
							{
								connectedRegion.Add(region3.Index);
							}
						}
					}
				}
			}
		}
	}

	public void ComputeConnectedRegionNotColonized(global::Empire empire, float explorationRatioToReach, ref List<int> regionNotColonized)
	{
		this.ComputeConnectedRegion(empire, ref regionNotColonized, (Region match) => match.City == null && match.IsLand);
	}

	public void ComputeConnectedRegionNotExplored(global::Empire empire, float explorationRatioToReach, ref List<int> regionNotExplored)
	{
		this.ComputeConnectedRegion(empire, ref regionNotExplored, (Region match) => !this.IsRegionExplored(empire, match, explorationRatioToReach) && match.IsLand);
	}

	public void ComputeContinentOccupation(global::Empire empire, int continentID, out int numberOfAlliesOnContinent, out int numberOfEnnemiesOnContinent)
	{
		numberOfAlliesOnContinent = 0;
		numberOfEnnemiesOnContinent = 0;
		DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
		int[] regionList = this.world.Continents[continentID].RegionList;
		for (int i = 0; i < regionList.Length; i++)
		{
			Region region = this.world.Regions[i];
			if (region.City != null)
			{
				DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(region.City.Empire);
				if (diplomaticRelation.State != null)
				{
					if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.War || diplomaticRelation.State.Name == DiplomaticRelationState.Names.ColdWar || diplomaticRelation.State.Name == DiplomaticRelationState.Names.Truce)
					{
						numberOfEnnemiesOnContinent++;
					}
					if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.Peace || diplomaticRelation.State.Name == DiplomaticRelationState.Names.Alliance)
					{
						numberOfAlliesOnContinent++;
					}
				}
			}
		}
	}

	public void ComputeNeighbourRegions(Region region, ref List<Region> neighboursRegion)
	{
		for (int i = 0; i < region.Borders.Length; i++)
		{
			neighboursRegion.Add(this.world.Regions[region.Borders[i].NeighbourRegionIndex]);
		}
	}

	public void ComputeNeighbourRegions(int regionIndex, ref List<int> neighboursRegion)
	{
		Region region = this.world.Regions[regionIndex];
		for (int i = 0; i < region.Borders.Length; i++)
		{
			neighboursRegion.Add(region.Borders[i].NeighbourRegionIndex);
		}
	}

	public StaticString GetRegionBiome(Region region)
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(service2 != null);
		byte biomeType = service2.GetBiomeType(region.WorldPositions[0]);
		return service2.GetBiomeTypeMappingName(biomeType);
	}

	public int GetRemainingFreeRegionInContinent(global::Empire empire, int continentIndex)
	{
		if (empire == null)
		{
			throw new ArgumentNullException("empire");
		}
		int num = 0;
		foreach (AIRegionData airegionData in this.regionDataByEmpires[empire.Index])
		{
			Region region = this.world.Regions[airegionData.RegionIndex];
			if (region.ContinentID == continentIndex && this.IsRegionColonizedBySomeone(region))
			{
				num++;
			}
		}
		return this.world.Continents[continentIndex].RegionList.Length - num;
	}

	public int GetRemainingRegionToExploreInContinent(global::Empire empire, int continentIndex, float explorationRatio)
	{
		if (empire == null)
		{
			throw new ArgumentNullException("empire");
		}
		int num = 0;
		foreach (AIRegionData airegionData in this.regionDataByEmpires[empire.Index])
		{
			Region region = this.world.Regions[airegionData.RegionIndex];
			if (region.ContinentID == continentIndex && airegionData.ExplorationRatio > explorationRatio)
			{
				num++;
			}
		}
		return this.world.Continents[continentIndex].RegionList.Length - num;
	}

	public float GetWorldColonizationRatio(global::Empire empire)
	{
		if (empire == null)
		{
			throw new ArgumentNullException("empire");
		}
		int num = 0;
		foreach (AIRegionData airegionData in this.regionDataByEmpires[empire.Index])
		{
			if (airegionData.IsColonizedByMe)
			{
				num++;
			}
		}
		float num2 = (float)num / (float)this.colonizableRegionCount;
		Diagnostics.Assert(num2 >= 0f && num2 <= 1f);
		return num2;
	}

	public override IEnumerator Initialize(IServiceContainer serviceContainer, global::Game game)
	{
		yield return base.Initialize(serviceContainer, game);
		serviceContainer.AddService<IWorldAtlasAIHelper>(this);
		this.aiParameterConverterDatabase = Databases.GetDatabase<AIParameterConverter>(false);
		this.world = game.World;
		Diagnostics.Assert(this.world != null);
		this.worldPositionService = game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionService != null);
		this.InitializeRegion(game);
		yield break;
	}

	public bool IsContinentColonizedByEmpire(global::Empire empire, int continentID)
	{
		int[] regionList = this.world.Continents[continentID].RegionList;
		for (int i = 0; i < regionList.Length; i++)
		{
			Region region = this.world.Regions[i];
			if (region.City != null && region.City.Empire.Index == empire.Index)
			{
				return true;
			}
		}
		return false;
	}

	public override IEnumerator Load(global::Game game)
	{
		yield return base.Load(game);
		this.UpdateRegionData();
		yield break;
	}

	public override void Release()
	{
		base.Release();
		this.world = null;
		this.worldPositionService = null;
	}

	public override void RunAIThread()
	{
		this.UpdateRegionData();
	}

	private IDatabase<AIParameterConverter> aiParameterConverterDatabase;

	private Dictionary<StaticString, IAIParameter<InterpreterContext>[]> aiParametersByBiomeName = new Dictionary<StaticString, IAIParameter<InterpreterContext>[]>();

	private IAIParameter<InterpreterContext>[] regionEvaluationAiParameters;

	private int[] empireBorderSize;

	private IPathfindingService pathfindingService;

	private AIRegionData[][] regionDataByEmpires;

	private AISafetyData[] safetyDataByEmpires;

	private IVisibilityService visibilityService;

	private IWorldPositionningService worldPositionningService;

	private int colonizableRegionCount;

	private World world;

	private IWorldPositionningService worldPositionService;
}
