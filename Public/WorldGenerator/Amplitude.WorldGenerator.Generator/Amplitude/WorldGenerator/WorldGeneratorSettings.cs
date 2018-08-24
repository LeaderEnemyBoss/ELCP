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

		[XmlElement("Version")]
		[WorldGeneratorConfigurationProperty]
		public int Version { get; set; }

		[XmlElement("NumberOfMajorFactions")]
		[WorldGeneratorConfigurationProperty]
		public int NumberOfMajorFactions { get; set; }

		[XmlElement("Seed")]
		[WorldGeneratorConfigurationProperty]
		public int Seed { get; set; }

		[XmlElement("Width")]
		[WorldGeneratorConfigurationProperty]
		public int Width { get; set; }

		[XmlElement("Height")]
		[WorldGeneratorConfigurationProperty]
		public int Height { get; set; }

		[XmlElement("WorldWrap")]
		[WorldGeneratorConfigurationProperty]
		public bool WorldWrap { get; set; }

		[XmlElement("ForceOceansOnMapEdges")]
		[WorldGeneratorConfigurationProperty]
		public bool ForceOceansOnMapEdges { get; set; }

		[XmlElement("WastelandEdges")]
		[WorldGeneratorConfigurationProperty]
		public bool WastelandEdges { get; set; }

		[XmlElement("OceanContourSailDistance")]
		[WorldGeneratorConfigurationProperty]
		public int OceanContourSailDistance { get; set; }

		[XmlArray("Climates")]
		public List<Climate> Climates { get; set; }

		[XmlElement("SelectedGlobalClimate")]
		[WorldGeneratorConfigurationProperty]
		public string SelectedGlobalClimate { get; set; }

		[XmlArray("GlobalClimates")]
		public List<GlobalClimate> GlobalClimates { get; set; }

		[XmlElement("ClimateStructure")]
		[WorldGeneratorConfigurationProperty]
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

		[XmlElement("GlobalAnomalyMultiplier")]
		[WorldGeneratorConfigurationProperty]
		public int GlobalAnomalyMultiplier { get; set; }

		[XmlArray("AnomalyOdds")]
		public WorldGeneratorSettings.TerrainAnomaly[] xmlAnomalyOdds { get; set; }

		[XmlArray("AnomalyWeights")]
		public WorldGeneratorSettings.Anomaly[] xmlAnomalyWeights { get; set; }

		[XmlIgnore]
		public Dictionary<string, Dictionary<string, int>> AnomalyWeightsPerTerrain { get; private set; }

		[XmlIgnore]
		public Dictionary<string, int> UniqueAnomaliesQuantities { get; private set; }

		[XmlElement("LandPrevalence")]
		[WorldGeneratorConfigurationProperty]
		public int LandPrevalence { get; set; }

		[XmlElement("OceanPrevalence")]
		[WorldGeneratorConfigurationProperty]
		public int OceanPrevalence { get; set; }

		[XmlElement("LandMasses")]
		[WorldGeneratorConfigurationProperty]
		public int LandMasses { get; set; }

		[XmlElement("MinLandMasses")]
		[WorldGeneratorConfigurationProperty]
		public int MinLandMasses { get; set; }

		[XmlElement("MaxLandMasses")]
		[WorldGeneratorConfigurationProperty]
		public int MaxLandMasses { get; set; }

		[XmlElement("ContinentShaping")]
		[WorldGeneratorConfigurationProperty]
		public WorldGeneratorSettings.ContinentStyles ContinentShaping { get; set; }

		[XmlElement("ContinentSpreading")]
		[WorldGeneratorConfigurationProperty]
		public WorldGeneratorSettings.ContinentStyles ContinentSpreading { get; set; }

		[XmlElement("IslandsPresencePercent")]
		[WorldGeneratorConfigurationProperty]
		public int IslandsPresencePercent { get; set; }

		[XmlElement("IslandsMinimalSize")]
		[WorldGeneratorConfigurationProperty]
		public int IslandsMinimalSize { get; set; }

		[XmlElement("ExpectedRegionArea")]
		[WorldGeneratorConfigurationProperty]
		public int ExpectedRegionArea { get; set; }

		[XmlElement("ExpectedOceanRegionArea")]
		[WorldGeneratorConfigurationProperty]
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

		[XmlElement("ExpectedDistrictArea")]
		[WorldGeneratorConfigurationProperty]
		public int ExpectedDistrictArea { get; set; }

		[XmlElement("MaxResourcesPerRegion")]
		[WorldGeneratorConfigurationProperty]
		public int MaxResourcesPerRegion { get; set; }

		[XmlElement("MaxDifferentLuxuryTypes")]
		[WorldGeneratorConfigurationProperty]
		public int MaxDifferentLuxuryTypes { get; set; }

		[XmlElement("MaxLandElevation")]
		[WorldGeneratorConfigurationProperty]
		public sbyte MaxLandElevation { get; set; }

		[XmlElement("MaxOceanDepth")]
		[WorldGeneratorConfigurationProperty]
		public sbyte MaxOceanDepth { get; set; }

		[XmlElement("MediumMountainElevation")]
		[WorldGeneratorConfigurationProperty]
		public sbyte MediumMountainElevation { get; set; }

		[XmlElement("HighMountainElevation")]
		[WorldGeneratorConfigurationProperty]
		public sbyte HighMountainElevation { get; set; }

		[XmlElement("MaxCliffDeltaElevation")]
		[WorldGeneratorConfigurationProperty]
		public sbyte MaxCliffDeltaElevation { get; set; }

		[XmlElement("MaxPassableDeltaElevation")]
		public sbyte MaxPassableDeltaElevation { get; set; }

		[XmlElement("PassablePrevalence")]
		[WorldGeneratorConfigurationProperty]
		public int PassablePrevalence { get; set; }

		[XmlElement("CliffPrevalence")]
		[WorldGeneratorConfigurationProperty]
		public int CliffPrevalence { get; set; }

		[XmlElement("MinLakeArea")]
		[WorldGeneratorConfigurationProperty]
		public sbyte MinLakeArea { get; set; }

		[XmlElement("MaxLakeArea")]
		[WorldGeneratorConfigurationProperty]
		public sbyte MaxLakeArea { get; set; }

		[XmlElement("MaxLakeBottomElevation")]
		[WorldGeneratorConfigurationProperty]
		public sbyte MaxLakeBottomElevation { get; set; }

		[XmlElement("LakePresencePercent")]
		[WorldGeneratorConfigurationProperty]
		public int LakePresencePercent { get; set; }

		[XmlElement("MaxRiverElevation")]
		[WorldGeneratorConfigurationProperty]
		public sbyte MaxRiverElevation { get; set; }

		[XmlElement("RiverPresencePercent")]
		[WorldGeneratorConfigurationProperty]
		public int RiverPresencePercent { get; set; }

		[XmlElement("MountainRangeAreaSize")]
		[WorldGeneratorConfigurationProperty]
		public int MountainRangeAreaSize { get; set; }

		[XmlElement("MountainDownwardSlopePercent")]
		[WorldGeneratorConfigurationProperty]
		public int MountainDownwardSlopePercent { get; set; }

		[XmlElement("ElevationNoisePercent")]
		[WorldGeneratorConfigurationProperty]
		public int ElevationNoisePercent { get; set; }

		[XmlElement("CoastalCliffPercent")]
		[WorldGeneratorConfigurationProperty]
		public int CoastalCliffPercent { get; set; }

		[XmlElement("RiverMinLength")]
		[WorldGeneratorConfigurationProperty]
		public int RiverMinLength { get; set; }

		[XmlElement("RidgePresencePercent")]
		[WorldGeneratorConfigurationProperty]
		public int RidgePresencePercent { get; set; }

		[XmlElement("MinRidgeSize")]
		[WorldGeneratorConfigurationProperty]
		public int MinRidgeSize { get; set; }

		[XmlElement("MaxRidgeSize")]
		[WorldGeneratorConfigurationProperty]
		public int MaxRidgeSize { get; set; }

		[XmlElement("RidgeMinElevation")]
		[WorldGeneratorConfigurationProperty]
		public int RidgeMinElevation { get; set; }

		[XmlElement("AdjacentBiomeInclusionOdds")]
		public int AdjacentBiomeInclusionOdds { get; set; }

		[XmlElement("AdjacentBiomeInclusionDepth")]
		public int AdjacentBiomeInclusionDepth { get; set; }

		[XmlElement("MinVolvanicRegions")]
		[WorldGeneratorConfigurationProperty]
		public int MinVolvanicRegions { get; set; }

		[XmlElement("WeightMultiplier")]
		[WorldGeneratorConfigurationProperty]
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

		[XmlElement("SpawnDesirabilityFIDS")]
		[WorldGeneratorConfigurationProperty]
		public float SpawnDesirabilityFIDS { get; set; }

		[XmlElement("SpawnDesirabilityShape")]
		[WorldGeneratorConfigurationProperty]
		public float SpawnDesirabilityShape { get; set; }

		[XmlElement("SpawnDesirabilityLocation")]
		[WorldGeneratorConfigurationProperty]
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

		[XmlElement("StrategicResourcesAbundancePercent")]
		[WorldGeneratorConfigurationProperty]
		public int StrategicResourcesAbundancePercent { get; set; }

		[XmlElement("LuxuryResourcesAbundancePercent")]
		[WorldGeneratorConfigurationProperty]
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

		[XmlElement("Geometry")]
		[WorldGeneratorConfigurationProperty]
		public string Geometry { get; set; }

		[XmlElement("GeometryHexaPerBigPatchX")]
		[WorldGeneratorConfigurationProperty]
		public int GeometryHexaPerBigPatchX { get; set; }

		[XmlElement("GeometryHexaPerBigPatchY")]
		[WorldGeneratorConfigurationProperty]
		public int GeometryHexaPerBigPatchY { get; set; }

		[XmlElement("GeometryGenerateRiver")]
		[WorldGeneratorConfigurationProperty]
		public bool GeometryGenerateRiver { get; set; }

		[XmlElement("GeometryGenerateOceanRegionBorder")]
		[WorldGeneratorConfigurationProperty]
		public bool GeometryGenerateOceanRegionBorder { get; set; }

		[XmlElement("GeometryTerrainLevelHeight")]
		[WorldGeneratorConfigurationProperty]
		public float GeometryTerrainLevelHeight { get; set; }

		[XmlElement("GeometryTranslationPerTransitionNoStep")]
		[WorldGeneratorConfigurationProperty]
		public float GeometryTranslationPerTransitionNoStep { get; set; }

		[XmlElement("GeometryTranslationPerTransitionNoStepUnderWater")]
		[WorldGeneratorConfigurationProperty]
		public float GeometryTranslationPerTransitionNoStepUnderWater { get; set; }

		[XmlElement("GeometryTranslationPerStep")]
		[WorldGeneratorConfigurationProperty]
		public float GeometryTranslationPerStep { get; set; }

		[XmlElement("GeometryTranslationPerStepUnderWater")]
		[WorldGeneratorConfigurationProperty]
		public float GeometryTranslationPerStepUnderWater { get; set; }

		[XmlElement("GeometryTranslationPerSmallCliff")]
		[WorldGeneratorConfigurationProperty]
		public float GeometryTranslationPerSmallCliff { get; set; }

		[XmlElement("GeometryTranslationPerSmallCliffUnderWater")]
		[WorldGeneratorConfigurationProperty]
		public float GeometryTranslationPerSmallCliffUnderWater { get; set; }

		[XmlElement("GeometrySmallBreakUpCountSteps")]
		[WorldGeneratorConfigurationProperty]
		public int GeometrySmallBreakUpCountSteps { get; set; }

		[XmlElement("GeometrySmallBreakUpCountNoStep")]
		[WorldGeneratorConfigurationProperty]
		public int GeometrySmallBreakUpCountNoStep { get; set; }

		[XmlElement("GeometrySmallBreakUpCountRivers")]
		[WorldGeneratorConfigurationProperty]
		public int GeometrySmallBreakUpCountRivers { get; set; }

		[XmlElement("GeometrySmoothGroupAngle")]
		[WorldGeneratorConfigurationProperty]
		public float GeometrySmoothGroupAngle { get; set; }

		[XmlElement("GeometryBreakUpProbabilityMultiplier")]
		[WorldGeneratorConfigurationProperty]
		public float GeometryBreakUpProbabilityMultiplier { get; set; }

		[XmlElement("GeometryLowResNoiseFrequencyMultiplier")]
		[WorldGeneratorConfigurationProperty]
		public float GeometryLowResNoiseFrequencyMultiplier { get; set; }

		[XmlElement("GeometryLowResNoiseIntensity")]
		[WorldGeneratorConfigurationProperty]
		public float GeometryLowResNoiseIntensity { get; set; }

		[XmlElement("GeometryHighResNoiseFrequencyMultiplier")]
		[WorldGeneratorConfigurationProperty]
		public float GeometryHighResNoiseFrequencyMultiplier { get; set; }

		[XmlElement("GeometryHighResNoiseIntensityLowAltitude")]
		[WorldGeneratorConfigurationProperty]
		public float GeometryHighResNoiseIntensityLowAltitude { get; set; }

		[XmlElement("GeometryHighResNoiseIntensityHighAltitude")]
		[WorldGeneratorConfigurationProperty]
		public float GeometryHighResNoiseIntensityHighAltitude { get; set; }

		[XmlElement("GeometryHighResNoiseLowAltitude")]
		[WorldGeneratorConfigurationProperty]
		public float GeometryHighResNoiseLowAltitude { get; set; }

		[XmlElement("GeometryHighResNoiseHighAltitude")]
		[WorldGeneratorConfigurationProperty]
		public float GeometryHighResNoiseHighAltitude { get; set; }

		[XmlElement("GeometryRegionLinefilterRadius")]
		[WorldGeneratorConfigurationProperty]
		public float GeometryRegionLinefilterRadius { get; set; }

		[XmlElement("GeometryLevelLinefilterRadius")]
		[WorldGeneratorConfigurationProperty]
		public float GeometryLevelLinefilterRadius { get; set; }

		[XmlElement("GeometryMinRiverWidth")]
		[WorldGeneratorConfigurationProperty]
		public float GeometryMinRiverWidth { get; set; }

		[XmlElement("GeometryMaxRiverWidth")]
		[WorldGeneratorConfigurationProperty]
		public float GeometryMaxRiverWidth { get; set; }

		[XmlElement("GeometryMinRiverDepth")]
		[WorldGeneratorConfigurationProperty]
		public float GeometryMinRiverDepth { get; set; }

		[XmlElement("GeometryMaxRiverDepth")]
		[WorldGeneratorConfigurationProperty]
		public float GeometryMaxRiverDepth { get; set; }

		[XmlElement("GeometryLengthRiverToBeMaxWidthDepth")]
		[WorldGeneratorConfigurationProperty]
		public float GeometryLengthRiverToBeMaxWidthDepth { get; set; }

		[XmlElement("GeometryRiverBorderFactor")]
		[WorldGeneratorConfigurationProperty]
		public float GeometryRiverBorderFactor { get; set; }

		[XmlElement("GeometryOffsetYOfRegionLineAboveCliff")]
		[WorldGeneratorConfigurationProperty]
		public float GeometryOffsetYOfRegionLineAboveCliff { get; set; }

		[XmlElement("GeometryDisableNoTransition")]
		[WorldGeneratorConfigurationProperty]
		public bool GeometryDisableNoTransition { get; set; }

		[XmlElement("GeometryDisableTripleTransition")]
		[WorldGeneratorConfigurationProperty]
		public bool GeometryDisableTripleTransition { get; set; }

		[XmlElement("ReplaceInlandSeas")]
		[WorldGeneratorConfigurationProperty]
		public bool ReplaceInlandSeas { get; set; }

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
