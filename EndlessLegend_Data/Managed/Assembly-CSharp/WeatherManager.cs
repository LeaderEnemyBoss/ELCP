using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Utilities.Maps;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class WeatherManager : GameAncillary, Amplitude.Xml.Serialization.IXmlSerializable, IService, IWeatherService
{
	public event EventHandler<WindChangeEventArgs> WindChange;

	private XmlSerializer XmlSerializer { get; set; }

	public virtual void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		reader.ReadStartElement();
		if (reader.IsStartElement("TurnWhenLastBegun"))
		{
			this.TurnWhenLastBegun = reader.ReadElementString<int>("TurnWhenLastBegun");
		}
		if (num >= 2)
		{
			this.weatherDifficulty = reader.ReadElementString<string>("WeatherDifficulty");
		}
		else
		{
			this.weatherDifficulty = "Normal";
		}
		int direction = reader.ReadElementString<int>("WindDirection");
		int strength = reader.ReadElementString<int>("WindStrength");
		this.Wind = new Wind(direction, strength);
		int direction2 = reader.ReadElementString<int>("ControlledWindDirection");
		int strength2 = reader.ReadElementString<int>("ControlledWindStrength");
		this.ControlledWind = new Wind(direction2, strength2);
		int attribute = reader.GetAttribute<int>("Count");
		byte[] array = new byte[attribute];
		if (attribute > 0)
		{
			byte[,] array2 = new byte[(int)base.Game.World.WorldParameters.Rows, (int)base.Game.World.WorldParameters.Columns];
			this.weatherMap = new GridMap<byte>(WorldAtlas.Maps.Weather, (int)base.Game.World.WorldParameters.Columns, (int)base.Game.World.WorldParameters.Rows, array2);
			base.Game.World.Atlas.RegisterMapInstance<GridMap<byte>>(this.weatherMap);
		}
		reader.ReadStartElement("WeatherMap");
		for (int i = 0; i < attribute; i++)
		{
			array[i] = reader.ReadElementString<byte>("WeatherValue");
		}
		reader.ReadEndElement("WeatherMap");
		if (attribute > 0)
		{
			this.weatherMap.Data = array;
			this.LoadingTurn = this.game.Turn;
		}
		if (num >= 3)
		{
			this.WeatherControlCooldown = reader.ReadElementString<int>("WeatherControlCooldown");
			this.WeatherControlStartTurn = reader.ReadElementString<int>("WeatherControlStartTurn");
			this.WeatherControlTurnToLast = reader.ReadElementString<int>("WeatherControlTurnToLast");
			this.PresetName = reader.ReadElementString<string>("PresetName");
		}
		byte[,] array3 = new byte[(int)base.Game.World.WorldParameters.Rows, (int)base.Game.World.WorldParameters.Columns];
		this.staticWeatherMap = new GridMap<byte>(WorldAtlas.Maps.StaticWeather, (int)base.Game.World.WorldParameters.Columns, (int)base.Game.World.WorldParameters.Rows, array3);
		base.Game.World.Atlas.RegisterMapInstance<GridMap<byte>>(this.staticWeatherMap);
		if (num >= 4)
		{
			int attribute2 = reader.GetAttribute<int>("StaticCount");
			byte[] array4 = new byte[attribute2];
			reader.ReadStartElement("StaticWeatherMap");
			for (int j = 0; j < attribute; j++)
			{
				array4[j] = reader.ReadElementString<byte>("StaticWeatherValue");
			}
			reader.ReadEndElement("StaticWeatherMap");
			if (attribute2 > 0)
			{
				this.staticWeatherMap.Data = array4;
			}
		}
	}

	public virtual void WriteXml(XmlWriter writer)
	{
		writer.WriteVersionAttribute(4);
		writer.WriteAttributeString("AssemblyQualifiedName", base.GetType().AssemblyQualifiedName);
		writer.WriteElementString<int>("TurnWhenLastBegun", this.TurnWhenLastBegun);
		writer.WriteElementString("WeatherDifficulty", this.weatherDifficulty);
		writer.WriteElementString<int>("WindDirection", this.Wind.Direction);
		writer.WriteElementString<int>("WindStrength", this.Wind.Strength);
		writer.WriteElementString<int>("ControlledWindDirection", this.ControlledWind.Direction);
		writer.WriteElementString<int>("ControlledWindStrength", this.ControlledWind.Strength);
		writer.WriteStartElement("WeatherMap");
		writer.WriteAttributeString<int>("Count", this.weatherMap.Data.Length);
		for (int i = 0; i < this.weatherMap.Data.Length; i++)
		{
			writer.WriteElementString<byte>("WeatherValue", this.weatherMap.Data[i]);
		}
		writer.WriteEndElement();
		writer.WriteElementString<int>("WeatherControlCooldown", this.WeatherControlCooldown);
		writer.WriteElementString<int>("WeatherControlStartTurn", this.WeatherControlStartTurn);
		writer.WriteElementString<int>("WeatherControlTurnToLast", this.WeatherControlTurnToLast);
		writer.WriteElementString("PresetName", this.PresetName);
		writer.WriteStartElement("StaticWeatherMap");
		writer.WriteAttributeString<int>("StaticCount", this.staticWeatherMap.Data.Length);
		for (int j = 0; j < this.staticWeatherMap.Data.Length; j++)
		{
			writer.WriteElementString<byte>("StaticWeatherValue", this.staticWeatherMap.Data[j]);
		}
		writer.WriteEndElement();
	}

	public Wind ControlledWind { get; set; }

	public float LightningDamageInPercent
	{
		get
		{
			return this.lightningDamageInPercent;
		}
	}

	public int LoadingTurn
	{
		get
		{
			return this.loadingTurn;
		}
		set
		{
			this.loadingTurn = value;
		}
	}

	public string PresetName { get; set; }

	public int TurnWhenLastBegun { get; set; }

	public int WeatherControlCooldown { get; set; }

	public int WeatherControlStartTurn { get; set; }

	public int WeatherControlTurnToLast { get; set; }

	public Wind Wind { get; set; }

	public int ModificationDate
	{
		get
		{
			return this.modificationDate;
		}
	}

	public int WeatherDefinitionCount
	{
		get
		{
			return this.weatherDefinitions.Length;
		}
	}

	private global::PlayerController ServerPlayerController
	{
		get
		{
			IPlayerControllerRepositoryService service = base.Game.Services.GetService<IPlayerControllerRepositoryService>();
			IPlayerControllerRepositoryControl playerControllerRepositoryControl = service as IPlayerControllerRepositoryControl;
			if (playerControllerRepositoryControl == null)
			{
				Diagnostics.LogError("Fail getting PlayerController !");
			}
			return playerControllerRepositoryControl.GetPlayerControllerById("server");
		}
	}

	public override IEnumerator BindServices(IServiceContainer serviceContainer)
	{
		yield return base.BindServices(serviceContainer);
		IGameService gameService = Services.GetService<IGameService>();
		if (gameService != null)
		{
			this.game = (gameService.Game as global::Game);
		}
		this.eventService = Services.GetService<IEventService>();
		if (this.eventService == null)
		{
			Diagnostics.LogError("Wasn't able to find the event service.");
		}
		yield return base.BindService<IPathfindingService>(serviceContainer, delegate(IPathfindingService service)
		{
			this.pathfindingService = service;
		});
		yield return base.BindService<ISeasonService>(serviceContainer, delegate(ISeasonService service)
		{
			this.seasonService = service;
		});
		IDatabase<WeatherDefinition> weatherDefinitionDatabase = Databases.GetDatabase<WeatherDefinition>(false);
		Diagnostics.Assert(weatherDefinitionDatabase != null);
		this.weatherDefinitions = weatherDefinitionDatabase.GetValues();
		Array.Sort<WeatherDefinition>(this.weatherDefinitions, (WeatherDefinition left, WeatherDefinition right) => left.Name.CompareHandleTo(right.Name));
		List<WeatherDefinition> availableWeathers = new List<WeatherDefinition>(this.weatherDefinitions);
		IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
		if (downloadableContentService == null)
		{
			Diagnostics.LogError("Wasn't able to find the Downloadable Content Service.");
		}
		else if (downloadableContentService == null)
		{
			Diagnostics.LogError("Wasn't able to find the Downloadable Content Service.");
		}
		else
		{
			for (int weatherIndex = availableWeathers.Count - 1; weatherIndex >= 0; weatherIndex--)
			{
				if (string.IsNullOrEmpty(availableWeathers[weatherIndex].RequiredDLC) || !downloadableContentService.IsShared(availableWeathers[weatherIndex].RequiredDLC))
				{
					availableWeathers.RemoveAt(weatherIndex);
				}
			}
		}
		this.availableWeathersForGeneneration = availableWeathers.ToArray();
		this.droplistDatabase = Databases.GetDatabase<Droplist>(false);
		Diagnostics.Assert(this.droplistDatabase != null);
		this.simulationDescriptorsDatabase = Databases.GetDatabase<SimulationDescriptor>(true);
		Diagnostics.Assert(this.simulationDescriptorsDatabase != null);
		this.weatherDifficulty = Amplitude.Unity.Framework.Application.Registry.GetValue<string>(WeatherManager.WeatherDifficulty, "Random");
		if (this.weatherDifficulty != "Easy" && this.weatherDifficulty != "Normal" && this.weatherDifficulty != "Hard")
		{
			this.weatherDifficulty = "Random";
		}
		if (!float.TryParse(Amplitude.Unity.Runtime.Runtime.Registry.GetValue("Gameplay/Ancillaries/Weather/LightningDamageInPercent"), out this.lightningDamageInPercent))
		{
			Diagnostics.LogError("Fail getting lightning damage percent value.");
		}
		this.WeatherControlCooldown = -1;
		this.WeatherControlStartTurn = -1;
		this.WeatherControlTurnToLast = -1;
		serviceContainer.AddService<IWeatherService>(this);
		yield return base.BindService<IWorldPositionningService>(serviceContainer, delegate(IWorldPositionningService service)
		{
			this.worldPositionService = service;
		});
		if (this.worldPositionService == null)
		{
			Diagnostics.LogError("Wasn't able to find the world positionning service.");
		}
		yield break;
	}

	public override IEnumerator Ignite(IServiceContainer serviceContainer)
	{
		yield return base.Ignite(serviceContainer);
		yield break;
	}

	public override IEnumerator LoadGame(global::Game game)
	{
		yield return base.LoadGame(game);
		if (this.weatherDifficulty == "Random")
		{
			System.Random randomGenerator = new System.Random(World.Seed);
			switch (randomGenerator.Next(0, 3))
			{
			case 0:
				this.weatherDifficulty = "Easy";
				break;
			case 1:
				this.weatherDifficulty = "Normal";
				break;
			case 2:
				this.weatherDifficulty = "Hard";
				break;
			}
		}
		if (this.weatherMap == null)
		{
			byte[,] emptyData = new byte[(int)base.Game.World.WorldParameters.Rows, (int)base.Game.World.WorldParameters.Columns];
			this.weatherMap = new GridMap<byte>(WorldAtlas.Maps.Weather, (int)base.Game.World.WorldParameters.Columns, (int)base.Game.World.WorldParameters.Rows, emptyData);
			base.Game.World.Atlas.RegisterMapInstance<GridMap<byte>>(this.weatherMap);
		}
		if (this.staticWeatherMap == null)
		{
			byte[,] emptyData2 = new byte[(int)base.Game.World.WorldParameters.Rows, (int)base.Game.World.WorldParameters.Columns];
			this.staticWeatherMap = new GridMap<byte>(WorldAtlas.Maps.StaticWeather, (int)base.Game.World.WorldParameters.Columns, (int)base.Game.World.WorldParameters.Rows, emptyData2);
			base.Game.World.Atlas.RegisterMapInstance<GridMap<byte>>(this.staticWeatherMap);
		}
		if (this.tempData == null)
		{
			this.tempData = new GridMap<float>(string.Empty, (int)base.Game.World.WorldParameters.Columns, (int)base.Game.World.WorldParameters.Rows, new float[(int)base.Game.World.WorldParameters.Rows, (int)base.Game.World.WorldParameters.Columns]);
		}
		if (this.staticTempData == null)
		{
			this.staticTempData = new GridMap<float>(string.Empty, (int)base.Game.World.WorldParameters.Columns, (int)base.Game.World.WorldParameters.Rows, new float[(int)base.Game.World.WorldParameters.Rows, (int)base.Game.World.WorldParameters.Columns]);
		}
		if (this.regionIndexMap == null)
		{
			this.regionIndexMap = (base.Game.World.Atlas.GetMap(WorldAtlas.Maps.Regions) as GridMap<short>);
			Diagnostics.Assert(this.regionIndexMap != null);
		}
		this.mapBoostManager = base.Game.GetService<IMapBoostService>();
		Diagnostics.Assert(this.mapBoostManager != null);
		this.anomalyMap = (base.Game.World.Atlas.GetMap(WorldAtlas.Maps.Anomalies) as GridMap<byte>);
		this.pointOfInterestMap = (base.Game.World.Atlas.GetMap(WorldAtlas.Maps.PointOfInterest) as GridMap<PointOfInterest>);
		yield break;
	}

	public void GameServer_Turn_Begin()
	{
		if (this.TurnWhenLastBegun >= this.game.Turn)
		{
			return;
		}
		Wind newWindPreferences = this.GetNewWindPreferences();
		OrderSetWindPreferences order = new OrderSetWindPreferences(newWindPreferences.Direction, newWindPreferences.Strength);
		this.ServerPlayerController.PostOrder(order);
		Season currentSeason = this.seasonService.GetCurrentSeason();
		if (currentSeason != null && currentSeason.StartTurn != this.game.Turn)
		{
			if (this.WeatherControlStartTurn == this.game.Turn)
			{
				if (string.IsNullOrEmpty(this.PresetName))
				{
					Diagnostics.LogError("Preset name can't be null when activating weather control.");
					return;
				}
				OrderGenerateNewWeather order2 = new OrderGenerateNewWeather(DateTime.Now.Millisecond, this.PresetName);
				this.ServerPlayerController.PostOrder(order2);
			}
			if (this.WeatherControlTurnToLast == 0)
			{
				OrderGenerateNewWeather order3 = new OrderGenerateNewWeather(DateTime.Now.Millisecond, string.Empty);
				this.ServerPlayerController.PostOrder(order3);
			}
		}
	}

	public void GameClient_Turn_Begin()
	{
		if (this.TurnWhenLastBegun >= this.game.Turn)
		{
			if (this.LoadingTurn == this.game.Turn)
			{
				this.modificationDate++;
			}
			return;
		}
		this.TurnWhenLastBegun = this.game.Turn;
		this.ApplyWindToWeatherMap();
		this.Wind = new Wind(this.ControlledWind);
		this.ControlledWind = Wind.Invalid;
		this.OnWindChange();
		if (this.WeatherControlTurnToLast > -1)
		{
			this.WeatherControlTurnToLast--;
		}
		if (this.WeatherControlCooldown > 0)
		{
			this.WeatherControlCooldown--;
		}
	}

	public void GameClient_Turn_End()
	{
		this.SlapUnitsUnderLightningWeatherEffect();
		if (this.ControlledWind.IsValid() && !this.Wind.Equals(this.ControlledWind))
		{
			this.Wind = new Wind(this.ControlledWind);
		}
	}

	public bool GetTerrainCompatibilityAtPosition(WeatherDefinition weatherDefinition, WorldPosition worldPosition)
	{
		if (weatherDefinition.TerrainTypeMappings != null && weatherDefinition.TerrainTypeMappings.Length > 0)
		{
			byte terrainType = this.worldPositionService.GetTerrainType(worldPosition);
			StaticString terrainTypeMappingName = this.worldPositionService.GetTerrainTypeMappingName(terrainType);
			bool flag = false;
			for (int i = 0; i < weatherDefinition.TerrainTypeMappings.Length; i++)
			{
				if (terrainTypeMappingName.Equals(weatherDefinition.TerrainTypeMappings[i]))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		return this.pathfindingService.IsTileStopable(worldPosition, PathfindingMovementCapacity.All, (PathfindingFlags)0);
	}

	public void GenerateWeatherMap(int seed, string presetName)
	{
		this.UpdateWeatherOnArmies(false);
		for (int i = 0; i < this.weatherMap.Data.Length; i++)
		{
			this.weatherMap.Data[i] = 0;
			this.tempData.Data[i] = 0f;
			this.staticWeatherMap.Data[i] = 0;
			this.staticTempData.Data[i] = 0f;
		}
		for (int j = 0; j < this.availableWeathersForGeneneration.Length; j++)
		{
			this.RunGenerationForWeather(this.availableWeathersForGeneneration[j], seed + j, presetName);
		}
		if (string.IsNullOrEmpty(presetName))
		{
			this.WeatherControlTurnToLast = -1;
		}
		this.UpdateWeatherOnArmies(true);
		this.modificationDate++;
	}

	public bool GetMayAffectedByWeather(WorldPosition worldPosition)
	{
		if (this.worldPositionService.GetRegion(worldPosition) == null)
		{
			return false;
		}
		if (this.mapBoostManager.GetMapBoostAtPosition(worldPosition) != null)
		{
			return false;
		}
		if (this.anomalyMap.GetValue(worldPosition) != 0)
		{
			return false;
		}
		PointOfInterest value = this.pointOfInterestMap.GetValue(worldPosition);
		return value == null;
	}

	public byte GetWeatherValueAtPosition(WorldPosition worldPosition)
	{
		if (this.weatherMap == null || !worldPosition.IsValid)
		{
			return 0;
		}
		if (!this.GetMayAffectedByWeather(worldPosition))
		{
			return 0;
		}
		byte value = this.staticWeatherMap.GetValue((int)worldPosition.Row, (int)worldPosition.Column);
		if (value != 0)
		{
			WeatherDefinition weatherDefinition = this.GetWeatherDefinition(value);
			if (this.GetTerrainCompatibilityAtPosition(weatherDefinition, worldPosition))
			{
				return value;
			}
		}
		byte b = this.weatherMap.GetValue((int)worldPosition.Row, (int)worldPosition.Column);
		if (b != 0)
		{
			WeatherDefinition weatherDefinition2 = this.GetWeatherDefinition(b);
			if (weatherDefinition2 != null && !this.GetTerrainCompatibilityAtPosition(weatherDefinition2, worldPosition))
			{
				b = 0;
			}
		}
		return b;
	}

	public void FillPresetNames(ref List<StaticString> names)
	{
		if (this.weatherDefinitions.Length == 0)
		{
			return;
		}
		for (int i = 0; i < this.weatherDefinitions[0].GameSettingsPreferences.Length; i++)
		{
			if (this.weatherDefinitions[0].GameSettingsPreferences[i].GameDifficulty == this.weatherDifficulty && this.weatherDefinitions[0].GameSettingsPreferences[i].SeasonType != "Winter" && this.weatherDefinitions[0].GameSettingsPreferences[i].SeasonType != "Summer" && this.weatherDefinitions[0].GameSettingsPreferences[i].SeasonType != "HeatWave")
			{
				names.Add(this.weatherDefinitions[0].GameSettingsPreferences[i].SeasonType);
			}
		}
	}

	public WeatherDefinition GetWeatherDefinition(int index)
	{
		Diagnostics.Assert(index >= 0);
		Diagnostics.Assert(index < this.weatherDefinitions.Length);
		if (index >= 0 && index < this.weatherDefinitions.Length)
		{
			return this.weatherDefinitions[index];
		}
		return null;
	}

	public WeatherDefinition GetWeatherDefinition(byte weatherValue)
	{
		if (weatherValue == 0)
		{
			return null;
		}
		for (int i = 0; i < this.weatherDefinitions.Length; i++)
		{
			if ((byte)this.weatherDefinitions[i].Value == weatherValue)
			{
				return this.weatherDefinitions[i];
			}
		}
		return null;
	}

	public WeatherDefinition GetWeatherDefinitionAtPosition(WorldPosition worldPosition)
	{
		return this.GetWeatherDefinition(this.GetWeatherValueAtPosition(worldPosition));
	}

	public void OverridePathfindingCost(WorldPosition worldPosition, PathfindingMovementCapacity movementCapacity, ref float cost)
	{
		int weatherValueAtPosition = (int)this.GetWeatherValueAtPosition(worldPosition);
		if (weatherValueAtPosition == 0)
		{
			return;
		}
		WeatherDefinition weatherDefinition = null;
		for (int i = 0; i < this.weatherDefinitions.Length; i++)
		{
			if (this.weatherDefinitions[i].Value == weatherValueAtPosition)
			{
				weatherDefinition = this.weatherDefinitions[i];
				break;
			}
		}
		if (weatherDefinition == null)
		{
			Diagnostics.LogError("In OverrideCostByWeatherEffectsPathfindingRule, Fail getting weather definition.");
			return;
		}
		PathfindingRule rule = weatherDefinition.GetRule();
		if (rule == null)
		{
			return;
		}
		if ((movementCapacity & PathfindingMovementCapacity.Water) == PathfindingMovementCapacity.None)
		{
			cost = float.PositiveInfinity;
		}
		else
		{
			int currentTileHeigh = (int)this.worldPositionService.GetTerrainHeight(worldPosition);
			cost = rule.GetCost(movementCapacity, currentTileHeigh);
		}
	}

	public void UpdateWeatherEffectsOnArmyWorldPositionChange(Army army, WorldPosition oldPosition, WorldPosition newPosition)
	{
		this.UpdateWeatherEffectAtPosition(oldPosition, army, false);
		this.UpdateWeatherEffectAtPosition(newPosition, army, true);
	}

	public void OnWindChange()
	{
		if (this.WindChange != null)
		{
			this.WindChange(this, new WindChangeEventArgs(this.Wind));
		}
	}

	public void UpdateWeatherControlValues(int empireIndex, string presetName)
	{
		this.PresetName = presetName;
		if (string.IsNullOrEmpty(this.PresetName))
		{
			this.WeatherControlStartTurn = -1;
			this.WeatherControlTurnToLast = -1;
			this.WeatherControlCooldown = 0;
		}
		else if (this.WeatherControlStartTurn <= base.Game.Turn)
		{
			this.WeatherControlStartTurn = base.Game.Turn + 1;
			this.WeatherControlTurnToLast = this.GetWeatherControlTurnDurationFor(this.game.Empires[empireIndex]);
			this.WeatherControlCooldown = this.GetWeatherControlCoolDownDurationFor(this.game.Empires[empireIndex]);
		}
	}

	public int GetWeatherControlTurnDurationFor(global::Empire empire)
	{
		if (this.weatherControlTurnToLastInterpreterContext == null)
		{
			this.weatherControlTurnToLastInterpreterContext = new InterpreterContext(null);
			string value = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<string>("Gameplay/Ancillaries/Weather/WeatherControlTurnToLastFormula");
			this.weatherControlTurnToLastFormulaTokens = Interpreter.InfixTransform(value);
		}
		this.weatherControlTurnToLastInterpreterContext.SimulationObject = empire.SimulationObject;
		return (int)((float)Interpreter.Execute(this.weatherControlTurnToLastFormulaTokens, this.weatherControlTurnToLastInterpreterContext));
	}

	public int GetWeatherControlCoolDownDurationFor(global::Empire empire)
	{
		if (this.weatherControlCooldownInterpreterContext == null)
		{
			this.weatherControlCooldownInterpreterContext = new InterpreterContext(null);
			string value = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<string>("Gameplay/Ancillaries/Weather/WeatherControlCooldownFormula");
			this.weatherControlCooldownFormulaTokens = Interpreter.InfixTransform(value);
		}
		this.weatherControlCooldownInterpreterContext.SimulationObject = empire.SimulationObject;
		return (int)((float)Interpreter.Execute(this.weatherControlCooldownFormulaTokens, this.weatherControlCooldownInterpreterContext));
	}

	private Wind GetNewWindPreferences()
	{
		string seasonType = this.seasonService.GetCurrentSeason().SeasonDefinition.SeasonType;
		Droplist droplist;
		if (!this.droplistDatabase.TryGetValue("DroplistWindDirectionChange" + seasonType + this.weatherDifficulty, out droplist))
		{
			Diagnostics.LogError("Fail getting wind direction droplist.");
			return this.Wind;
		}
		Droplist droplist2;
		if (!this.droplistDatabase.TryGetValue("DroplistWindStrengthChange" + seasonType + this.weatherDifficulty, out droplist2))
		{
			Diagnostics.LogError("Fail getting wind strength droplist.");
			return this.Wind;
		}
		Droplist droplist3;
		DroppableInteger droppableInteger = droplist.Pick(null, out droplist3, new object[0]) as DroppableInteger;
		DroppableInteger droppableInteger2 = droplist2.Pick(null, out droplist3, new object[0]) as DroppableInteger;
		WorldOrientation worldOrientation = (WorldOrientation)this.Wind.Direction;
		worldOrientation = worldOrientation.Rotate(droppableInteger.Value);
		return new Wind((int)worldOrientation, droppableInteger2.Value);
	}

	private float GetWeatherThresholdMultiplier(WeatherDefinition.Preferences weatherPreferences, string overrideKeyMatchCase)
	{
		if (weatherPreferences == null || string.IsNullOrEmpty(overrideKeyMatchCase))
		{
			return 1f;
		}
		WeatherDefinition.Preferences.PreferencesOverride[] overrides = weatherPreferences.Overrides;
		if (overrides != null && overrides.Length > 0)
		{
			for (int i = 0; i < overrides.Length; i++)
			{
				string overrideKey = overrides[i].OverrideKey;
				if (!string.IsNullOrEmpty(overrideKey) && overrideKey.Equals(overrideKeyMatchCase))
				{
					return overrides[i].ThresholdMultiplier;
				}
			}
		}
		return 1f;
	}

	private void UpdateWeatherEffectAtPosition(WorldPosition worldPosition, Army army, bool apply)
	{
		WeatherDefinition weatherDefinitionAtPosition = this.GetWeatherDefinitionAtPosition(worldPosition);
		if (weatherDefinitionAtPosition == null)
		{
			return;
		}
		if (weatherDefinitionAtPosition.SimulationDescriptorReferences == null)
		{
			return;
		}
		if (apply)
		{
			for (int i = 0; i < weatherDefinitionAtPosition.SimulationDescriptorReferences.Length; i++)
			{
				SimulationDescriptor simulationDescriptor;
				if (this.simulationDescriptorsDatabase.TryGetValue(weatherDefinitionAtPosition.SimulationDescriptorReferences[i].Name, out simulationDescriptor) && !army.SimulationObject.Tags.Contains(simulationDescriptor.Name))
				{
					army.AddDescriptor(simulationDescriptor, false);
				}
			}
		}
		else
		{
			for (int j = 0; j < weatherDefinitionAtPosition.SimulationDescriptorReferences.Length; j++)
			{
				army.RemoveDescriptorByName(weatherDefinitionAtPosition.SimulationDescriptorReferences[j].Name);
			}
		}
		army.Refresh(false);
	}

	private void ApplyWindToWeatherMap()
	{
		this.UpdateWeatherOnArmies(false);
		WorldOrientation worldOrientation = (WorldOrientation)this.Wind.Direction;
		worldOrientation = worldOrientation.Rotate(3);
		GridMap<byte> gridMap = new GridMap<byte>(string.Empty, this.weatherMap.Width, this.weatherMap.Height, null);
		WorldPosition invalid = WorldPosition.Invalid;
		WorldPosition worldPosition = WorldPosition.Invalid;
		short num = 0;
		while ((int)num < gridMap.Height)
		{
			invalid.Row = num;
			short num2 = 0;
			while ((int)num2 < gridMap.Width)
			{
				invalid.Column = num2;
				worldPosition = this.worldPositionService.GetNeighbourTileFullCyclic(invalid, worldOrientation, this.Wind.Strength);
				byte value = this.weatherMap.GetValue(worldPosition);
				gridMap.SetValue(invalid, value);
				num2 += 1;
			}
			num += 1;
		}
		this.weatherMap.Data = gridMap.Data;
		this.UpdateWeatherOnArmies(true);
		this.modificationDate++;
	}

	private void UpdateWeatherOnArmies(bool apply)
	{
		for (int i = 0; i < this.game.Empires.Length; i++)
		{
			DepartmentOfDefense agency = this.game.Empires[i].GetAgency<DepartmentOfDefense>();
			for (int j = 0; j < agency.Armies.Count; j++)
			{
				this.UpdateWeatherEffectAtPosition(agency.Armies[j].WorldPosition, agency.Armies[j], apply);
			}
		}
	}

	private void RunGenerationForWeather(WeatherDefinition weatherDefinition, int seed, string presetName)
	{
		string currentSeasonType;
		if (!string.IsNullOrEmpty(presetName))
		{
			currentSeasonType = presetName;
		}
		else
		{
			currentSeasonType = this.seasonService.GetCurrentSeason().SeasonDefinition.SeasonType;
		}
		if (string.IsNullOrEmpty(currentSeasonType))
		{
			Diagnostics.LogError("currentSeasonType can't be null or empty");
			return;
		}
		WeatherDefinition.Preferences preferences = weatherDefinition.GameSettingsPreferences.First((WeatherDefinition.Preferences pref) => pref.SeasonType == currentSeasonType && pref.GameDifficulty == this.weatherDifficulty);
		if (preferences == null)
		{
			Diagnostics.LogWarning("No preferences were found corresponding to season type and game difficulty");
			return;
		}
		float num = preferences.Threshold;
		WeatherDefinition.Preferences.PreferencesOverride[] overrides = preferences.Overrides;
		if (overrides != null && overrides.Length > 0)
		{
			string seasonIntensityName = this.seasonService.GetSeasonIntensityName();
			if (!string.IsNullOrEmpty(seasonIntensityName))
			{
				num *= this.GetWeatherThresholdMultiplier(preferences, seasonIntensityName);
			}
		}
		if (num > 1f || preferences.Priority <= 0f)
		{
			return;
		}
		NoiseHelper.BufferGenerationData bufferGenerationData = preferences.BufferGenerationData;
		NoiseHelper.ApplyRandomOffsetToBufferGenerationData(seed, ref bufferGenerationData);
		Vector2 mapSize = new Vector2((float)this.weatherMap.Width, (float)this.weatherMap.Height);
		for (int i = 0; i < this.weatherMap.Height; i++)
		{
			for (int j = 0; j < this.weatherMap.Width; j++)
			{
				Vector2 uv = new Vector2((float)j / mapSize.x, (float)i / mapSize.y);
				float num2 = NoiseHelper.GetNoiseCyclicMap(uv, bufferGenerationData, mapSize);
				if (num2 >= num)
				{
					num2 *= preferences.Priority;
					if (weatherDefinition.AffectedByWind)
					{
						if (num2 > this.tempData.GetValue(i, j))
						{
							this.tempData.SetValue(i, j, num2);
							this.weatherMap.SetValue(i, j, (byte)weatherDefinition.Value);
						}
					}
					else if (num2 > this.staticTempData.GetValue(i, j))
					{
						this.staticTempData.SetValue(i, j, num2);
						this.staticWeatherMap.SetValue(i, j, (byte)weatherDefinition.Value);
					}
				}
			}
		}
	}

	private void SlapUnitsUnderLightningWeatherEffect()
	{
		for (int i = 0; i < this.game.Empires.Length; i++)
		{
			DepartmentOfDefense agency = this.game.Empires[i].GetAgency<DepartmentOfDefense>();
			if (agency != null)
			{
				for (int j = agency.Armies.Count - 1; j >= 0; j--)
				{
					Army army = agency.Armies[j];
					if (army.SimulationObject.Tags.Contains(WeatherManager.LightningTarget))
					{
						int unitsCount = army.UnitsCount;
						bool flag = false;
						for (int k = army.StandardUnits.Count - 1; k >= 0; k--)
						{
							flag |= this.DamageUnit(agency, army.StandardUnits[k]);
						}
						if (army.Hero != null)
						{
							flag |= this.DamageUnit(agency, army.Hero);
						}
						if (flag && this.game.Empires[i] is MajorEmpire)
						{
							ArmyHitInfo armyInfo = new ArmyHitInfo(army, unitsCount, army.WorldPosition, ArmyHitInfo.HitType.Weather);
							this.eventService.Notify(new EventArmyHit(this.game.Empires[i], armyInfo, false));
						}
					}
				}
			}
		}
	}

	private bool DamageUnit(DepartmentOfDefense departmentOfDefense, Unit unit)
	{
		if (unit == null || unit.CheckUnitAbility(UnitAbility.ReadonlySubmersible, -1))
		{
			return false;
		}
		float num = this.lightningDamageInPercent - unit.GetPropertyValue(SimulationProperties.LightningDamageReduction);
		num *= unit.GetPropertyValue(SimulationProperties.MaximumHealth);
		if (num > 0f)
		{
			departmentOfDefense.WoundUnit(unit, num);
			return true;
		}
		return false;
	}

	public static string WeatherDifficulty = "Settings/Game/WeatherDifficulty";

	public static StaticString LightningImmunity = new StaticString("LightningImmunity");

	public static StaticString LightningTarget = new StaticString("LightningTarget");

	private global::Game game;

	private IEventService eventService;

	private IPathfindingService pathfindingService;

	private ISeasonService seasonService;

	private IWorldPositionningService worldPositionService;

	private IMapBoostService mapBoostManager;

	private WeatherDefinition[] weatherDefinitions;

	private WeatherDefinition[] availableWeathersForGeneneration;

	private IDatabase<Droplist> droplistDatabase;

	private IDatabase<SimulationDescriptor> simulationDescriptorsDatabase;

	private GridMap<short> regionIndexMap;

	private GridMap<byte> weatherMap;

	private GridMap<float> tempData;

	private GridMap<byte> staticWeatherMap;

	private GridMap<float> staticTempData;

	private GridMap<PointOfInterest> pointOfInterestMap;

	private GridMap<byte> anomalyMap;

	private string weatherDifficulty;

	private int loadingTurn = -1;

	private int modificationDate;

	private float lightningDamageInPercent;

	private object[] weatherControlCooldownFormulaTokens;

	private object[] weatherControlTurnToLastFormulaTokens;

	private InterpreterContext weatherControlCooldownInterpreterContext;

	private InterpreterContext weatherControlTurnToLastInterpreterContext;
}
