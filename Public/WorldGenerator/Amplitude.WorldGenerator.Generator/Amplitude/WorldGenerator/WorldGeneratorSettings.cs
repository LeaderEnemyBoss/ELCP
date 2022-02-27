using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Serialization;
using Amplitude.WorldGenerator.Tasks;
using Amplitude.WorldGenerator.Tmx;
using Amplitude.WorldGenerator.World.Info;

namespace Amplitude.WorldGenerator
{
	public class WorldGeneratorSettings
	{
		public WorldGeneratorSettings()
		{
			this.Width = 200;
			this.Height = 150;
			this.WorldWrap = true;
			this.WastelandEdges = true;
			this.ExpectedRegionArea = 200;
			this.ExpectedDistrictArea = 3;
			this.MinSpawnRegionAreaFactor = 1.25f;
			this.MaxSpawnRegionAreaFactor = 1.5f;
			this.GUILimitedResourcesPerRegion = 15;
			this.Biomes = new List<Biome>();
			this.Terrains = new List<Terrain>();
			this.GlobalClimates = new List<GlobalClimate>();
			this.Climates = new List<Climate>();
			this.Transformations = new List<TerrainTransformation>();
			this.xmlAnomalyOdds = new WorldGeneratorSettings.TerrainAnomaly[0];
			this.xmlAnomalyWeights = new WorldGeneratorSettings.Anomaly[0];
			this.xmlRegionNames = new WorldGeneratorSettings.RegionName[0];
			this.xmlPOIAlgorithmParameters = new WorldGeneratorSettings.AlgorithmParameters[0];
			this.xmlFactionSpawnPreferences = new WorldGeneratorSettings.Faction[0];
			this.AnomalyOdds = new Dictionary<string, int>();
			this.AnomalyWeightsPerTerrain = new Dictionary<string, Dictionary<string, int>>();
			this.UniqueAnomaliesQuantities = new Dictionary<string, int>();
			this.RegionNames = new List<string>();
			this.POITemplates = new Dictionary<string, PointOfInterestTemplate>();
			this.NoRiverHexAnomalies = new HashSet<string>();
			this.Algorithms = new Dictionary<string, WorldGeneratorSettings.AlgorithmParameters>();
			this.FactionSpawnPreferences = new Dictionary<string, WorldGeneratorSettings.Faction>();
			this.xmlTerrainBaseFIDSList = new List<WorldGeneratorSettings.xmlFIDS>();
			this.TerrainFIDS = new Dictionary<string, FIDS>();
			this.xmlFactionMinimalFIDSList = new List<WorldGeneratorSettings.xmlFIDS>();
			this.FactionFIDS = new Dictionary<string, FIDS>();
			this.xmlAnomalyBonusFIDSList = new List<WorldGeneratorSettings.xmlFIDS>();
			this.AnomalyFIDS = new Dictionary<string, FIDS>();
			this.xmlGeoFeatures = new WorldGeneratorSettings.xmlGeoFeature[0];
			this.GeoFeatureDefinitions = new Dictionary<string, GeoFeatureDefinition>();
			this.xmlPoiWithHoleList = new List<WorldGeneratorSettings.xmlHoleDefinition>();
			this.PoiWithHoleList = new List<HoleDefinition>();
			this.xmlAnomalyWithHoleList = new List<WorldGeneratorSettings.xmlHoleDefinition>();
			this.AnomalyWithHoleList = new List<HoleDefinition>();
			this.GeometryHexaPerBigPatchX = 10;
			this.GeometryHexaPerBigPatchY = 10;
			this.GeometryGenerateOceanRegionBorder = false;
			this.GeometryGenerateRiver = true;
			this.GeometryTerrainLevelHeight = 0.3f;
			this.GeometryTranslationPerStep = 0.25f;
			this.GeometryTranslationPerStepUnderWater = 0.25f;
			this.GeometryTranslationPerTransitionNoStep = 0.25f;
			this.GeometryTranslationPerTransitionNoStepUnderWater = 0.25f;
			this.GeometryTranslationPerSmallCliff = 0.05f;
			this.GeometryTranslationPerSmallCliffUnderWater = 0.05f;
			this.GeometrySmallBreakUpCountSteps = 2;
			this.GeometrySmallBreakUpCountNoStep = 1;
			this.GeometrySmallBreakUpCountRivers = 1;
			this.GeometrySmoothGroupAngle = 30f;
			this.GeometryBreakUpProbabilityMultiplier = 0.86f;
			this.GeometryLowResNoiseFrequencyMultiplier = 0.2f;
			this.GeometryLowResNoiseIntensity = 0.3f;
			this.GeometryHighResNoiseFrequencyMultiplier = 1f;
			this.GeometryHighResNoiseIntensityLowAltitude = 0.05f;
			this.GeometryHighResNoiseIntensityHighAltitude = 0.05f;
			this.GeometryHighResNoiseLowAltitude = 0f;
			this.GeometryHighResNoiseHighAltitude = 1f;
			this.GeometryRegionLinefilterRadius = 0.4f;
			this.GeometryLevelLinefilterRadius = 0.05f;
			this.GeometryOffsetYOfRegionLineAboveCliff = 0.066f;
			this.GeometryMinRiverWidth = 0.05f;
			this.GeometryMaxRiverWidth = 0.3f;
			this.GeometryMinRiverDepth = 0.02f;
			this.GeometryMaxRiverDepth = 0.1f;
			this.GeometryLengthRiverToBeMaxWidthDepth = 10f;
			this.GeometryRiverBorderFactor = 0.3f;
		}

		[XmlIgnore]
		public XmlDocument Document { get; set; }

		[XmlElement("Scenario")]
		public Scenario Scenario { get; set; }

		[XmlIgnore]
		public Map TmxMap { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("Version")]
		public int Version { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("NumberOfMajorFactions")]
		public int NumberOfMajorFactions { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("Seed")]
		public int Seed { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("Width")]
		public int Width { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("Height")]
		public int Height { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("WorldWrap")]
		public bool WorldWrap { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("ForceOceansOnMapEdges")]
		public bool ForceOceansOnMapEdges { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("WastelandEdges")]
		public bool WastelandEdges { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("OceanContourSailDistance")]
		public int OceanContourSailDistance { get; set; }

		[XmlArray("Climates")]
		public List<Climate> Climates { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("SelectedGlobalClimate")]
		public string SelectedGlobalClimate { get; set; }

		[XmlArray("GlobalClimates")]
		public List<GlobalClimate> GlobalClimates { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("ClimateStructure")]
		public ClimateStructure ClimateStructure { get; set; }

		[XmlIgnore]
		public List<ITask> Tasks
		{
			get
			{
				return this.tasks;
			}
			set
			{
				this.tasks = value;
			}
		}

		[XmlArray("Biomes")]
		public List<Biome> Biomes { get; set; }

		[XmlIgnore]
		public List<Terrain> Terrains { get; private set; }

		[XmlArray("TerrainTransformations")]
		public List<TerrainTransformation> Transformations { get; set; }

		[XmlIgnore]
		public Dictionary<string, int> AnomalyOdds { get; private set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GlobalAnomalyMultiplier")]
		public int GlobalAnomalyMultiplier { get; set; }

		[XmlArray("AnomalyOdds")]
		public WorldGeneratorSettings.TerrainAnomaly[] xmlAnomalyOdds { get; set; }

		[XmlArray("AnomalyWeights")]
		public WorldGeneratorSettings.Anomaly[] xmlAnomalyWeights { get; set; }

		[XmlIgnore]
		public Dictionary<string, Dictionary<string, int>> AnomalyWeightsPerTerrain { get; private set; }

		[XmlIgnore]
		public Dictionary<string, int> UniqueAnomaliesQuantities { get; private set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("LandPrevalence")]
		public int LandPrevalence { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("OceanPrevalence")]
		public int OceanPrevalence { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("LandMasses")]
		public int LandMasses { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("MinLandMasses")]
		public int MinLandMasses { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("MaxLandMasses")]
		public int MaxLandMasses { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("ContinentShaping")]
		public WorldGeneratorSettings.ContinentStyles ContinentShaping { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("ContinentSpreading")]
		public WorldGeneratorSettings.ContinentStyles ContinentSpreading { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("IslandsPresencePercent")]
		public int IslandsPresencePercent { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("IslandsMinimalSize")]
		public int IslandsMinimalSize { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("ExpectedRegionArea")]
		public int ExpectedRegionArea { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("ExpectedOceanRegionArea")]
		public int ExpectedOceanRegionArea { get; set; }

		[XmlElement("MinSpawnRegionAreaFactor")]
		public float MinSpawnRegionAreaFactor { get; set; }

		[XmlElement("MaxSpawnRegionAreaFactor")]
		public float MaxSpawnRegionAreaFactor { get; set; }

		[XmlIgnore]
		public int MinSpawnRegionArea
		{
			get
			{
				if (this.MinSpawnRegionAreaFactor > 0f)
				{
					return (int)((float)this.ExpectedRegionArea / this.MinSpawnRegionAreaFactor);
				}
				return 0;
			}
		}

		[XmlIgnore]
		public int MaxSpawnRegionArea
		{
			get
			{
				if (this.MaxSpawnRegionAreaFactor > 0f)
				{
					return (int)((float)this.ExpectedRegionArea * this.MaxSpawnRegionAreaFactor);
				}
				return this.Width * this.Height;
			}
		}

		[WorldGeneratorConfigurationProperty]
		[XmlElement("ExpectedDistrictArea")]
		public int ExpectedDistrictArea { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("MaxResourcesPerRegion")]
		public int MaxResourcesPerRegion { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("MaxDifferentLuxuryTypes")]
		public int MaxDifferentLuxuryTypes { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("MaxLandElevation")]
		public sbyte MaxLandElevation { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("MaxOceanDepth")]
		public sbyte MaxOceanDepth { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("MediumMountainElevation")]
		public sbyte MediumMountainElevation { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("HighMountainElevation")]
		public sbyte HighMountainElevation { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("MaxCliffDeltaElevation")]
		public sbyte MaxCliffDeltaElevation { get; set; }

		[XmlElement("MaxPassableDeltaElevation")]
		public sbyte MaxPassableDeltaElevation { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("PassablePrevalence")]
		public int PassablePrevalence { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("CliffPrevalence")]
		public int CliffPrevalence { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("MinLakeArea")]
		public sbyte MinLakeArea { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("MaxLakeArea")]
		public sbyte MaxLakeArea { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("MaxLakeBottomElevation")]
		public sbyte MaxLakeBottomElevation { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("LakePresencePercent")]
		public int LakePresencePercent { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("MaxRiverElevation")]
		public sbyte MaxRiverElevation { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("RiverPresencePercent")]
		public int RiverPresencePercent { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("MountainRangeAreaSize")]
		public int MountainRangeAreaSize { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("MountainDownwardSlopePercent")]
		public int MountainDownwardSlopePercent { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("ElevationNoisePercent")]
		public int ElevationNoisePercent { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("CoastalCliffPercent")]
		public int CoastalCliffPercent { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("RiverMinLength")]
		public int RiverMinLength { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("RidgePresencePercent")]
		public int RidgePresencePercent { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("MinRidgeSize")]
		public int MinRidgeSize { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("MaxRidgeSize")]
		public int MaxRidgeSize { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("RidgeMinElevation")]
		public int RidgeMinElevation { get; set; }

		[XmlElement("AdjacentBiomeInclusionOdds")]
		public int AdjacentBiomeInclusionOdds { get; set; }

		[XmlElement("AdjacentBiomeInclusionDepth")]
		public int AdjacentBiomeInclusionDepth { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("MinVolvanicRegions")]
		public int MinVolvanicRegions { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("WeightMultiplier")]
		public float WeightMultiplier { get; set; }

		[XmlIgnore]
		public Dictionary<string, PointOfInterestTemplate> POITemplates { get; private set; }

		[XmlArray("RegionNames")]
		public WorldGeneratorSettings.RegionName[] xmlRegionNames { get; set; }

		[XmlIgnore]
		public List<string> RegionNames { get; protected set; }

		[XmlArray("NoRiverHexAnomalies")]
		[XmlArrayItem(Type = typeof(string), ElementName = "NoRiverHexAnomaly")]
		public HashSet<string> NoRiverHexAnomalies { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("SpawnDesirabilityFIDS")]
		public float SpawnDesirabilityFIDS { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("SpawnDesirabilityShape")]
		public float SpawnDesirabilityShape { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("SpawnDesirabilityLocation")]
		public float SpawnDesirabilityLocation { get; set; }

		[XmlArray("SpawnPreferences")]
		[XmlArrayItem(Type = typeof(WorldGeneratorSettings.Faction), ElementName = "Faction")]
		public WorldGeneratorSettings.Faction[] xmlFactionSpawnPreferences { get; set; }

		[XmlIgnore]
		public Dictionary<string, WorldGeneratorSettings.Faction> FactionSpawnPreferences { get; protected set; }

		[XmlArray("TerrainBaseFIDSList")]
		[XmlArrayItem(Type = typeof(WorldGeneratorSettings.xmlFIDS), ElementName = "TerrainFIDS")]
		public List<WorldGeneratorSettings.xmlFIDS> xmlTerrainBaseFIDSList { get; set; }

		[XmlIgnore]
		public Dictionary<string, FIDS> TerrainFIDS { get; protected set; }

		[XmlArray("FactionMinimalFIDSList")]
		[XmlArrayItem(Type = typeof(WorldGeneratorSettings.xmlFIDS), ElementName = "FactionFIDS")]
		public List<WorldGeneratorSettings.xmlFIDS> xmlFactionMinimalFIDSList { get; set; }

		[XmlIgnore]
		public Dictionary<string, FIDS> FactionFIDS { get; protected set; }

		[XmlArray("AnomalyBonusFIDSList")]
		[XmlArrayItem(Type = typeof(WorldGeneratorSettings.xmlFIDS), ElementName = "AnomalyFIDS")]
		public List<WorldGeneratorSettings.xmlFIDS> xmlAnomalyBonusFIDSList { get; set; }

		[XmlIgnore]
		public Dictionary<string, FIDS> AnomalyFIDS { get; protected set; }

		[XmlArray("POIWithHoleList")]
		[XmlArrayItem(Type = typeof(WorldGeneratorSettings.xmlHoleDefinition), ElementName = "POIHole")]
		public List<WorldGeneratorSettings.xmlHoleDefinition> xmlPoiWithHoleList { get; set; }

		[XmlIgnore]
		public List<HoleDefinition> PoiWithHoleList { get; private set; }

		[XmlArray("AnomalyWithHoleList")]
		[XmlArrayItem(Type = typeof(WorldGeneratorSettings.xmlHoleDefinition), ElementName = "AnomalyHole")]
		public List<WorldGeneratorSettings.xmlHoleDefinition> xmlAnomalyWithHoleList { get; set; }

		[XmlIgnore]
		public List<HoleDefinition> AnomalyWithHoleList { get; private set; }

		[XmlArray("Geography")]
		public WorldGeneratorSettings.xmlGeoFeature[] xmlGeoFeatures { get; set; }

		[XmlIgnore]
		public Dictionary<string, GeoFeatureDefinition> GeoFeatureDefinitions { get; private set; }

		[XmlElement("GUILimitedResourcesPerRegion")]
		public int GUILimitedResourcesPerRegion { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("StrategicResourcesAbundancePercent")]
		public int StrategicResourcesAbundancePercent { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("LuxuryResourcesAbundancePercent")]
		public int LuxuryResourcesAbundancePercent { get; set; }

		[XmlArray("POIAlgorithmParameters")]
		public WorldGeneratorSettings.AlgorithmParameters[] xmlPOIAlgorithmParameters { get; set; }

		[XmlIgnore]
		public Dictionary<string, WorldGeneratorSettings.AlgorithmParameters> Algorithms { get; private set; }

		[XmlIgnore]
		public sbyte MaxDeltaPeakElevation
		{
			get
			{
				return this.MaxLandElevation - this.HighMountainElevation + 1;
			}
		}

		[WorldGeneratorConfigurationProperty]
		[XmlElement("Geometry")]
		public string Geometry { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryHexaPerBigPatchX")]
		public int GeometryHexaPerBigPatchX { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryHexaPerBigPatchY")]
		public int GeometryHexaPerBigPatchY { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryGenerateRiver")]
		public bool GeometryGenerateRiver { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryGenerateOceanRegionBorder")]
		public bool GeometryGenerateOceanRegionBorder { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryTerrainLevelHeight")]
		public float GeometryTerrainLevelHeight { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryTranslationPerTransitionNoStep")]
		public float GeometryTranslationPerTransitionNoStep { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryTranslationPerTransitionNoStepUnderWater")]
		public float GeometryTranslationPerTransitionNoStepUnderWater { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryTranslationPerStep")]
		public float GeometryTranslationPerStep { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryTranslationPerStepUnderWater")]
		public float GeometryTranslationPerStepUnderWater { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryTranslationPerSmallCliff")]
		public float GeometryTranslationPerSmallCliff { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryTranslationPerSmallCliffUnderWater")]
		public float GeometryTranslationPerSmallCliffUnderWater { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometrySmallBreakUpCountSteps")]
		public int GeometrySmallBreakUpCountSteps { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometrySmallBreakUpCountNoStep")]
		public int GeometrySmallBreakUpCountNoStep { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometrySmallBreakUpCountRivers")]
		public int GeometrySmallBreakUpCountRivers { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometrySmoothGroupAngle")]
		public float GeometrySmoothGroupAngle { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryBreakUpProbabilityMultiplier")]
		public float GeometryBreakUpProbabilityMultiplier { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryLowResNoiseFrequencyMultiplier")]
		public float GeometryLowResNoiseFrequencyMultiplier { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryLowResNoiseIntensity")]
		public float GeometryLowResNoiseIntensity { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryHighResNoiseFrequencyMultiplier")]
		public float GeometryHighResNoiseFrequencyMultiplier { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryHighResNoiseIntensityLowAltitude")]
		public float GeometryHighResNoiseIntensityLowAltitude { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryHighResNoiseIntensityHighAltitude")]
		public float GeometryHighResNoiseIntensityHighAltitude { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryHighResNoiseLowAltitude")]
		public float GeometryHighResNoiseLowAltitude { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryHighResNoiseHighAltitude")]
		public float GeometryHighResNoiseHighAltitude { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryRegionLinefilterRadius")]
		public float GeometryRegionLinefilterRadius { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryLevelLinefilterRadius")]
		public float GeometryLevelLinefilterRadius { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryMinRiverWidth")]
		public float GeometryMinRiverWidth { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryMaxRiverWidth")]
		public float GeometryMaxRiverWidth { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryMinRiverDepth")]
		public float GeometryMinRiverDepth { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryMaxRiverDepth")]
		public float GeometryMaxRiverDepth { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryLengthRiverToBeMaxWidthDepth")]
		public float GeometryLengthRiverToBeMaxWidthDepth { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryRiverBorderFactor")]
		public float GeometryRiverBorderFactor { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryOffsetYOfRegionLineAboveCliff")]
		public float GeometryOffsetYOfRegionLineAboveCliff { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryDisableNoTransition")]
		public bool GeometryDisableNoTransition { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("GeometryDisableTripleTransition")]
		public bool GeometryDisableTripleTransition { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("ReplaceInlandSeas")]
		public bool ReplaceInlandSeas { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("Volcanize")]
		public bool Volcanize { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("XephiWorldGeneratorBalance")]
		public bool XephiWorldGeneratorBalance { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("XephiStrategic")]
		public bool XephiStrategic { get; set; }

		[WorldGeneratorConfigurationProperty]
		[XmlElement("TeamCount")]
		public int TeamCount { get; set; }

		private List<ITask> tasks = new List<ITask>();

		[XmlType("TerrainAnomaly")]
		public class TerrainAnomaly
		{
			[XmlAttribute("Name")]
			public string Name;

			[XmlAttribute("Odds")]
			public int Odds;
		}

		[XmlType("PrevalenceInTerrain")]
		public class PrevalenceInTerrain
		{
			[XmlAttribute("Name")]
			public string Name;

			[XmlAttribute("Weight")]
			public int Weight;
		}

		[XmlType("Anomaly")]
		public class Anomaly
		{
			public Anomaly()
			{
				this.Prevalences = new WorldGeneratorSettings.PrevalenceInTerrain[0];
			}

			[XmlAttribute("Name")]
			public string Name;

			[XmlAttribute("Quantity")]
			public int Quantity;

			[XmlElement("PrevalenceInTerrain")]
			public WorldGeneratorSettings.PrevalenceInTerrain[] Prevalences;

			[XmlAttribute("DLCPrerequisite")]
			public string DLCPrerequisite;
		}

		public enum ContinentStyles
		{
			Regular,
			Chaotic
		}

		[XmlType("RegionName")]
		public class RegionName
		{
			[XmlText]
			public string Text;
		}

		[XmlType("Faction")]
		public class Faction
		{
			public Faction()
			{
				this.Preferences = new Dictionary<string, int>();
				this.Elements = new XmlElement[0];
			}

			[XmlAttribute("Name")]
			public string Name { get; set; }

			[XmlAnyElement]
			public XmlElement[] Elements { get; set; }

			[XmlIgnore]
			public Dictionary<string, int> Preferences { get; protected set; }

			public void PostProcess()
			{
				if (this.Elements == null)
				{
					return;
				}
				foreach (XmlElement xmlElement in this.Elements)
				{
					int value;
					if (int.TryParse(xmlElement.InnerText, out value))
					{
						this.Preferences.Add(xmlElement.Name, value);
					}
				}
			}
		}

		[XmlType("FIDS")]
		[StructLayout(LayoutKind.Sequential, Size = 1)]
		public struct xmlFIDS
		{
			[XmlAttribute("Name")]
			public string Name { get; set; }

			[XmlAttribute("Food")]
			public int Food { get; set; }

			[XmlAttribute("Industry")]
			public int Industry { get; set; }

			[XmlAttribute("Dust")]
			public int Dust { get; set; }

			[XmlAttribute("Science")]
			public int Science { get; set; }
		}

		[XmlType("HoleDefinition")]
		public class xmlHoleDefinition
		{
			[XmlAttribute("Name")]
			public string Name { get; set; }

			[XmlAttribute("RadiusXAxis")]
			public float RadiusXAxis { get; set; }

			[XmlAttribute("RadiusZAxis")]
			public float RadiusZAxis { get; set; }

			[XmlAttribute("XOffset")]
			public float XOffset { get; set; }

			[XmlAttribute("ZOffset")]
			public float ZOffset { get; set; }
		}

		[XmlType("Feature")]
		public class xmlGeoFeature
		{
			public xmlGeoFeature()
			{
				this.Names = new WorldGeneratorSettings.xmlGeoFeature.xmlName[0];
				this.Terrains = new WorldGeneratorSettings.xmlGeoFeature.xmlTerrain[0];
				this.Elevations = new WorldGeneratorSettings.xmlGeoFeature.xmlElevations();
				this.Elevations.Min = -99;
				this.Elevations.Max = 99;
			}

			[XmlAttribute("Category")]
			public string Category { get; set; }

			[XmlAttribute("KeepPercent")]
			public int KeepPercent { get; set; }

			[XmlArray("Names")]
			public WorldGeneratorSettings.xmlGeoFeature.xmlName[] Names { get; set; }

			[XmlArray("TerrainsIncluded")]
			public WorldGeneratorSettings.xmlGeoFeature.xmlTerrain[] Terrains { get; set; }

			[XmlElement("Elevations")]
			public WorldGeneratorSettings.xmlGeoFeature.xmlElevations Elevations { get; set; }

			[XmlType("Name")]
			public class xmlName
			{
				[XmlText]
				public string Text { get; set; }
			}

			[XmlType("TerrainIncluded")]
			public class xmlTerrain
			{
				[XmlAttribute("Name")]
				public string Name;
			}

			[XmlType("Elevations")]
			public class xmlElevations
			{
				[XmlAttribute("Min")]
				public int Min;

				[XmlAttribute("Max")]
				public int Max;
			}
		}

		[XmlType("Algorithm")]
		public class AlgorithmParameters
		{
			public AlgorithmParameters()
			{
				this.Parameters = new List<WorldGeneratorSettings.AlgorithmParameters.KeyValuePair>();
			}

			public int GetValue(string parameterName)
			{
				int result = 0;
				foreach (WorldGeneratorSettings.AlgorithmParameters.KeyValuePair keyValuePair in this.Parameters)
				{
					if (keyValuePair.Name == parameterName)
					{
						result = keyValuePair.Value;
					}
				}
				return result;
			}

			[XmlAttribute("Name")]
			public string Name;

			[XmlElement("Parameter")]
			public List<WorldGeneratorSettings.AlgorithmParameters.KeyValuePair> Parameters;

			[XmlType("Parameter")]
			public class KeyValuePair
			{
				[XmlAttribute("Name")]
				public string Name;

				[XmlAttribute("Value")]
				public int Value;
			}
		}
	}
}
