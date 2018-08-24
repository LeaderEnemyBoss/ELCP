using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Collections;
using Amplitude.Unity.AI;
using Amplitude.Unity.AI.Decision;
using Amplitude.Unity.AI.Decision.Diagnostics;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class AICommander_Colonization : AICommanderWithObjective, IXmlSerializable
{
	public AICommander_Colonization(ulong globalObjectiveID, int regionIndex) : base(AICommanderMissionDefinition.AICommanderCategory.Colonization, globalObjectiveID, regionIndex)
	{
		this.RegionTarget = null;
		this.listOfPosition = null;
	}

	public AICommander_Colonization() : base(AICommanderMissionDefinition.AICommanderCategory.Colonization, 0UL, 0)
	{
	}

	public override void ReadXml(XmlReader reader)
	{
		int attribute = reader.GetAttribute<int>("RegionTargetIndex");
		this.RegionTarget = null;
		if (attribute != -1)
		{
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			global::Game game = service.Game as global::Game;
			World world = game.World;
			this.RegionTarget = world.Regions[attribute];
			Diagnostics.Assert(this.RegionTarget != null);
		}
		base.ReadXml(reader);
		int attribute2 = reader.GetAttribute<int>("Count");
		if (attribute2 > 0)
		{
			this.listOfPosition = new WorldPosition[attribute2];
			reader.ReadStartElement("ListOfPosition");
			try
			{
				for (int i = 0; i < attribute2; i++)
				{
					this.ListOfPosition[i] = reader.ReadElementSerializable<WorldPosition>();
				}
			}
			catch
			{
				throw;
			}
			reader.ReadEndElement();
		}
		else
		{
			reader.Skip();
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<int>("RegionTargetIndex", (this.RegionTarget != null) ? this.RegionTarget.Index : -1);
		base.WriteXml(writer);
		writer.WriteStartElement("ListOfPosition");
		writer.WriteAttributeString<int>("Count", (this.ListOfPosition != null) ? this.ListOfPosition.Length : 0);
		if (this.ListOfPosition != null)
		{
			for (int i = 0; i < this.ListOfPosition.Length; i++)
			{
				IXmlSerializable xmlSerializable = this.ListOfPosition[i];
				writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
			}
		}
		writer.WriteEndElement();
	}

	public WorldPosition[] ListOfPosition
	{
		get
		{
			return this.listOfPosition;
		}
	}

	public Region RegionTarget
	{
		get
		{
			return this.region;
		}
		set
		{
			this.region = value;
			if (value != null)
			{
				base.RegionIndex = value.Index;
			}
		}
	}

	public override void Initialize()
	{
		base.Initialize();
		IWorldPositionEvaluationAIHelper service = AIScheduler.Services.GetService<IWorldPositionEvaluationAIHelper>();
		Diagnostics.Assert(service != null);
		this.decisionMakerPosition = new SimulationDecisionMaker<WorldPositionScore>(service, service.GetEmpireCityProxy(base.Empire));
		this.decisionMakerPosition.ParameterContextModifierDelegate = new Func<WorldPositionScore, StaticString, float>(this.DecisionParameterContextModifierPosition);
		this.decisionMakerPosition.ScoreTransferFunctionDelegate = new Func<WorldPositionScore, float, float>(this.DecisionScoreTransferFunctionPosition);
		IGameService service2 = Services.GetService<IGameService>();
		Diagnostics.Assert(service2 != null);
		this.worldPositionningService = service2.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.costalColonizationBoost = new HeuristicValue(0f);
	}

	public override bool IsMissionFinished(bool forceStop)
	{
		return base.IsMissionFinished(forceStop);
	}

	public override void PopulateMission()
	{
		if (base.Missions.Count > 0)
		{
			return;
		}
		DepartmentOfTheInterior agency = base.Empire.GetAgency<DepartmentOfTheInterior>();
		bool immediate = agency.Cities.Count == 0;
		if (!this.SelectPositionToColonize(this.region, out this.listOfPosition, immediate))
		{
			return;
		}
		base.PopulationFirstMissionFromCategory(base.Category.ToString(), new object[]
		{
			this.RegionTarget,
			this.ListOfPosition
		});
	}

	public override void RefreshMission()
	{
		base.RefreshMission();
		this.PopulateMission();
		this.EvaluateMission();
		base.PromoteMission();
	}

	public override void Release()
	{
		this.RegionTarget = null;
		base.Release();
	}

	private bool CanReachPositionInTurn(Army army, WorldPosition destination, int maximumNumberOfTurns, out int numberOfTurns)
	{
		numberOfTurns = maximumNumberOfTurns;
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		IPathfindingService service2 = service.Game.Services.GetService<IPathfindingService>();
		Diagnostics.Assert(service2 != null);
		if (army.WorldPosition.Equals(destination))
		{
			return true;
		}
		PathfindingContext pathfindingContext = army.GenerateContext();
		pathfindingContext.Greedy = true;
		PathfindingResult pathfindingResult = service2.FindPath(pathfindingContext, army.WorldPosition, destination, PathfindingManager.RequestMode.Default, null, PathfindingFlags.IgnoreFogOfWar, null);
		if (pathfindingResult == null)
		{
			return false;
		}
		WorldPath worldPath = new WorldPath();
		worldPath.Build(pathfindingResult, army.GetPropertyValue(SimulationProperties.MovementRatio), maximumNumberOfTurns, false);
		numberOfTurns = worldPath.ControlPoints.Length;
		if (worldPath.ControlPoints.Length < maximumNumberOfTurns)
		{
			return true;
		}
		WorldPosition left = worldPath.WorldPositions[(int)worldPath.ControlPoints[maximumNumberOfTurns - 1]];
		return left == destination && left == worldPath.WorldPositions[worldPath.Length - 1];
	}

	private float DecisionParameterContextModifierPosition(WorldPositionScore aiEvaluableElement, StaticString aiParameterName)
	{
		AIEntity_Empire entity = base.AIPlayer.GetEntity<AIEntity_Empire>();
		return entity.Context.GetModifierValue(AILayer_Strategy.ColonizationParameterModifier, aiParameterName);
	}

	private float DecisionScoreTransferFunctionPosition(WorldPositionScore aiEvaluableElement, float currentScore)
	{
		int num = 0;
		for (int i = 0; i < aiEvaluableElement.CountByOrientation.Length; i++)
		{
			if (num < aiEvaluableElement.CountByOrientation[i])
			{
				num = aiEvaluableElement.CountByOrientation[i];
			}
		}
		num -= 2;
		num = Mathf.Min(8, num);
		currentScore += currentScore * 0.05f * (float)num;
		if (aiEvaluableElement.HasCostalTile)
		{
			currentScore += currentScore * this.costalColonizationBoost;
		}
		if (aiEvaluableElement.NewDistrictNeighbourgNumber < 6)
		{
			currentScore *= 0.5f;
		}
		return currentScore;
	}

	private bool SelectPositionToColonize(Region region, out WorldPosition[] sortedPosition, bool immediate = false)
	{
		this.ComputeCostalBoost();
		IWorldPositionEvaluationAIHelper service = AIScheduler.Services.GetService<IWorldPositionEvaluationAIHelper>();
		Diagnostics.Assert(service != null);
		IWorldAtlasAIHelper service2 = AIScheduler.Services.GetService<IWorldAtlasAIHelper>();
		Diagnostics.Assert(service2 != null);
		IDatabase<SimulationDescriptor> database = Databases.GetDatabase<SimulationDescriptor>(false);
		Diagnostics.Assert(database != null);
		WorldPositionScore[] array = service.GetWorldPositionColonizationScore(base.Empire, region.WorldPositions);
		if (array == null || array.Length == 0)
		{
			sortedPosition = null;
			return false;
		}
		if (immediate)
		{
			DepartmentOfDefense agency = base.Empire.GetAgency<DepartmentOfDefense>();
			Diagnostics.Assert(agency != null);
			List<Army> list = null;
			if (agency.Armies != null && agency.Armies.Count > 0)
			{
				list = (from army in agency.Armies
				where army.IsSettler
				select army).ToList<Army>();
			}
			if (list == null || list.Count == 0)
			{
				sortedPosition = null;
				return false;
			}
			Army army3 = null;
			int num = 2;
			List<WorldPositionScore> list2 = new List<WorldPositionScore>();
			for (int i = 0; i < list.Count; i++)
			{
				Army army2 = list[i];
				List<WorldPositionScore> list3 = new List<WorldPositionScore>(array.Length);
				int maximumNumberOfTurns = num;
				for (int j = 0; j < array.Length; j++)
				{
					int num2;
					if (this.CanReachPositionInTurn(army2, array[j].WorldPosition, maximumNumberOfTurns, out num2))
					{
						if (num2 < num)
						{
							army3 = army2;
							num = num2;
						}
						list3.Add(array[j]);
					}
				}
				if (army3 == army2)
				{
					list2 = list3;
					break;
				}
				if (list2.Count == 0)
				{
					army3 = army2;
					list2 = list3;
				}
			}
			array = list2.ToArray();
		}
		Diagnostics.Assert(this.decisionMakerPosition != null);
		this.decisionMakerPosition.UnregisterAllOutput();
		this.decisionMakerPosition.ClearAIParametersOverrides();
		AIEntity_Empire entity = base.AIPlayer.GetEntity<AIEntity_Empire>();
		Diagnostics.Assert(entity != null);
		entity.Context.InitializeDecisionMaker<WorldPositionScore>(AILayer_Strategy.ColonizationParameterModifier, this.decisionMakerPosition);
		IGameService service3 = Services.GetService<IGameService>();
		Diagnostics.Assert(service3 != null);
		this.decisionResults.Clear();
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			DecisionMakerEvaluationData<WorldPositionScore, InterpreterContext> decisionMakerEvaluationData;
			this.decisionMakerPosition.EvaluateDecisions(array, ref this.decisionResults, out decisionMakerEvaluationData);
			decisionMakerEvaluationData.Turn = (service3.Game as global::Game).Turn;
			this.DecisionMakerEvaluationDataHistoric.Add(decisionMakerEvaluationData);
		}
		else
		{
			this.decisionMakerPosition.EvaluateDecisions(array, ref this.decisionResults);
		}
		if (this.decisionResults == null || this.decisionResults.Count == 0)
		{
			sortedPosition = null;
			return false;
		}
		int num3 = Math.Max(this.decisionResults.Count, Math.Min(this.decisionResults.Count, 5));
		WorldPosition[] array2 = new WorldPosition[num3];
		for (int k = 0; k < num3; k++)
		{
			WorldPositionScore worldPositionScore = (WorldPositionScore)this.decisionResults[k].Element;
			array2[k] = worldPositionScore.WorldPosition;
		}
		sortedPosition = array2;
		return true;
	}

	private void ComputeCostalBoost()
	{
		DepartmentOfTheInterior agency = base.Empire.GetAgency<DepartmentOfTheInterior>();
		this.costalCityCount = 0;
		for (int i = 0; i < agency.Cities.Count; i++)
		{
			if (agency.Cities[i].Districts.Any((District match) => this.worldPositionningService.IsWaterTile(match.WorldPosition)))
			{
				this.costalCityCount++;
			}
		}
		this.costalColonizationBoost.Reset();
		if (this.costalCityCount < 3)
		{
			int num = 3;
			HeuristicValue heuristicValue = new HeuristicValue(0f);
			heuristicValue.Add((float)agency.Cities.Count, "City count", new object[0]);
			heuristicValue.Subtract((float)(this.costalCityCount * num), "Costal city * {0}", new object[]
			{
				num
			});
			heuristicValue.Divide((float)num, "Costal gap", new object[0]);
			heuristicValue.Clamp(0.1f, 1f);
			heuristicValue.Multiply(this.costalCityRatioMaxBoost, "Factor from xml", new object[0]);
			this.costalColonizationBoost.Boost(heuristicValue, "Costal city ratio boost", new object[0]);
		}
		if (base.Empire.SimulationObject.Tags.Contains(DownloadableContent16.AffinitySeaDemons))
		{
			this.costalColonizationBoost.Boost(0.1f, "Sea demons", new object[0]);
		}
	}

	public FixedSizedList<DecisionMakerEvaluationData<WorldPositionScore, InterpreterContext>> DecisionMakerEvaluationDataHistoric = new FixedSizedList<DecisionMakerEvaluationData<WorldPositionScore, InterpreterContext>>(global::Application.FantasyPreferences.AIDebugHistoricSize);

	private SimulationDecisionMaker<WorldPositionScore> decisionMakerPosition;

	private List<DecisionResult> decisionResults = new List<DecisionResult>();

	private WorldPosition[] listOfPosition;

	private Region region;

	private IWorldPositionningService worldPositionningService;

	private int costalCityCount;

	private HeuristicValue costalColonizationBoost;

	private float costalCityRatioMaxBoost = 0.6f;
}
