using System;
using Amplitude;

public static class SimulationProperties
{
	public static StaticString GetBoosterActivationsPropertyName(BoosterDefinition boosterDefinition)
	{
		return boosterDefinition + "Activations";
	}

	public static readonly StaticString DistrictDust = "DistrictDust";

	public static readonly StaticString DistrictFood = "DistrictFood";

	public static readonly StaticString DistrictCityPoint = "DistrictCityPoint";

	public static readonly StaticString DistrictIndustry = "DistrictIndustry";

	public static readonly StaticString DistrictScience = "DistrictScience";

	public static readonly StaticString DistrictDustNet = "DistrictDustNet";

	public static readonly StaticString DistrictFoodNet = "DistrictFoodNet";

	public static readonly StaticString DistrictCityPointNet = "DistrictCityPointNet";

	public static readonly StaticString DistrictIndustryNet = "DistrictIndustryNet";

	public static readonly StaticString DistrictScienceNet = "DistrictScienceNet";

	public static readonly StaticString DustPopulation = "DustPopulation";

	public static readonly StaticString CityPointPopulation = "CityPointPopulation";

	public static readonly StaticString FoodPopulation = "FoodPopulation";

	public static readonly StaticString IndustryPopulation = "IndustryPopulation";

	public static readonly StaticString SciencePopulation = "SciencePopulation";

	public static readonly StaticString BaseFoodPerPopulation = "BaseFoodPerPopulation";

	public static readonly StaticString BaseIndustryPerPopulation = "BaseIndustryPerPopulation";

	public static readonly StaticString BaseDustPerPopulation = "BaseDustPerPopulation";

	public static readonly StaticString BaseSciencePerPopulation = "BaseSciencePerPopulation";

	public static readonly StaticString BaseCityPointPerPopulation = "BaseCityPointPerPopulation";

	public static readonly StaticString WorkedDust = "WorkedDust";

	public static readonly StaticString WorkedCityPoint = "WorkedCityPoint";

	public static readonly StaticString WorkedFood = "WorkedFood";

	public static readonly StaticString WorkedIndustry = "WorkedIndustry";

	public static readonly StaticString WorkedScience = "WorkedScience";

	public static readonly StaticString CityDust = "CityDust";

	public static readonly StaticString CityCityPoint = "CityCityPoint";

	public static readonly StaticString CityFood = "CityFood";

	public static readonly StaticString CityIndustry = "CityIndustry";

	public static readonly StaticString CityScience = "CityScience";

	public static readonly StaticString RawFoodToGrowthConversionFactor = "CityFoodToCityGrowthConversionFactor";

	public static readonly StaticString RawFoodToProductionConversionFactor = "CityFoodToCityProductionConversionFactor";

	public static readonly StaticString RawIndustryToGrowthConversionFactor = "CityIndustryToCityGrowthConversionFactor";

	public static readonly StaticString RawIndustryToProductionConversionFactor = "CityIndustryToCityProductionConversionFactor";

	public static readonly StaticString CityAntiSpy = "CityAntiSpy";

	public static readonly StaticString CityExpansionDisapproval = "CityExpansionDisapproval";

	public static readonly StaticString CityApproval = "CityApproval";

	public static readonly StaticString CityGrowth = "CityGrowth";

	public static readonly StaticString CityMoney = "CityMoney";

	public static readonly StaticString CityProduction = "CityProduction";

	public static readonly StaticString CityResearch = "CityResearch";

	public static readonly StaticString CityEmpirePoint = "CityEmpirePoint";

	public static readonly StaticString PopulationEfficiencyLimit = "PopulationEfficiencyLimit";

	public static readonly StaticString PopulationBuyoutCooldown = "PopulationBuyoutCooldown";

	public static readonly StaticString MaximumPopulationBuyoutCooldown = "MaximumPopulationBuyoutCooldown";

	public static readonly StaticString PopulationSacrificeCooldown = "PopulationSacrificeCooldown";

	public static readonly StaticString MaximumPopulationSacrificeCooldown = "MaximumPopulationSacrificeCooldown";

	public static readonly StaticString CityAntiSpyUpkeep = "CityAntiSpyUpkeep";

	public static readonly StaticString CityGrowthUpkeep = "CityGrowthUpkeep";

	public static readonly StaticString CityProductionUpkeep = "CityProductionUpkeep";

	public static readonly StaticString CityApprovalUpkeep = "CityApprovalUpkeep";

	public static readonly StaticString CityResearchUpkeep = "CityResearchUpkeep";

	public static readonly StaticString CityMoneyUpkeep = "CityMoneyUpkeep";

	public static readonly StaticString TotalCityMoneyUpkeep = "TotalCityMoneyUpkeep";

	public static readonly StaticString NetCityApproval = "NetCityApproval";

	public static readonly StaticString NetCityAntiSpy = "NetCityAntiSpy";

	public static readonly StaticString NetCityGrowth = "NetCityGrowth";

	public static readonly StaticString NetCityMoney = "NetCityMoney";

	public static readonly StaticString NetCityProduction = "NetCityProduction";

	public static readonly StaticString NetCityResearch = "NetCityResearch";

	public static readonly StaticString NetCityEmpirePoint = "NetCityEmpirePoint";

	public static readonly StaticString NetFortressMoney = "NetFortressMoney";

	public static readonly StaticString NetFortressEmpirePoint = "NetFortressEmpirePoint";

	public static readonly StaticString FortressTileDust = "FortressTileDust";

	public static readonly StaticString FortressTileEmpirePoint = "FortressTileEmpirePoint";

	public static readonly StaticString FortressTileScience = "FortressTileScience";

	public static readonly StaticString CityGrowthStock = "CityGrowthStock";

	public static readonly StaticString CityProductionStock = "CityProductionStock";

	public static readonly StaticString NetCityProductionToCityProductionStockConversionFactor = "NetCityProductionToCityProductionStockConversionFactor";

	public static readonly StaticString NetCityProductionToCityResearchConversionFactor = "NetCityProductionToCityResearchConversionFactor";

	public static readonly StaticString NetCityProductionToCityMoneyConversionFactor = "NetCityProductionToCityMoneyConversionFactor";

	public static readonly StaticString MaximumCityGrowth = "MaximumCityGrowth";

	public static readonly StaticString MinimumCityGrowth = "MinimumCityGrowth";

	public static readonly StaticString MaximumCityProduction = "MaximumCityProduction";

	public static readonly StaticString EmpireApproval = "EmpireApproval";

	public static readonly StaticString EmpireMoney = "EmpireMoney";

	public static readonly StaticString EmpireResearch = "EmpireResearch";

	public static readonly StaticString EmpirePoint = "EmpirePoint";

	public static readonly StaticString EmpireApprovalUpkeep = "EmpireApprovalUpkeep";

	public static readonly StaticString EmpireMoneyUpkeep = "EmpireMoneyUpkeep";

	public static readonly StaticString EmpireResearchUpkeep = "EmpireResearchUpkeep";

	public static readonly StaticString EmpirePointUpkeep = "EmpirePointUpkeep";

	public static readonly StaticString NetEmpireApproval = "NetEmpireApproval";

	public static readonly StaticString NetEmpireMoney = "NetEmpireMoney";

	public static readonly StaticString NetEmpireResearch = "NetEmpireResearch";

	public static readonly StaticString NetEmpirePoint = "NetEmpirePoint";

	public static readonly StaticString BankAccount = "BankAccount";

	public static readonly StaticString EmpireResearchStock = "EmpireResearchStock";

	public static readonly StaticString EmpirePointStock = "EmpirePointStock";

	public static readonly StaticString CadaverCountNeededToObtainBooster = "CadaverCountNeededToObtainBooster";

	public static readonly StaticString CadaverStock = "CadaverStock";

	public static readonly StaticString OrbStock = "OrbStock";

	public static readonly StaticString EraTechnologyCountPrerequisite = "EraTechnologyCountPrerequisite";

	public static readonly StaticString TechnologyCost = "TechnologyCost";

	public static readonly StaticString AlliedUnitRegenModifier = "AlliedUnitRegenModifier";

	public static readonly StaticString BrokenLordsUnitRegenPerPacifiedVillage = "BrokenLordsUnitRegenPerPacifiedVillage";

	public static readonly StaticString InAlliedRegionUnitRegenModifier = "InAlliedRegionUnitRegenModifier";

	public static readonly StaticString InGarrisonRegenModifier = "InGarrisonRegenModifier";

	public static readonly StaticString InNoneAlliedRegionUnitRegenModifier = "InNoneAlliedRegionUnitRegenModifier";

	public static readonly StaticString InOwnedRegionUnitRegenModifier = "InOwnedRegionUnitRegenModifier";

	public static readonly StaticString NoneAlliedUnitRegenModifier = "NoneAlliedUnitRegenModifier";

	public static readonly StaticString OwnedUnitRegenModifier = "OwnedUnitRegenModifier";

	public static readonly StaticString UnitRegen = "UnitRegen";

	public static readonly StaticString UnitRegenModifier = "UnitRegenModifier";

	public static readonly StaticString UnitPoison = "UnitPoison";

	public static readonly StaticString UnitRegenLimitDistance = "UnitRegenLimitDistance";

	public static readonly StaticString UnassignedHeroRegenModifier = "UnassignedHeroRegenModifier";

	public static readonly StaticString MaximumDistrictLevel = "MaximumDistrictLevel";

	public static readonly StaticString AccessoriesSlotCount = "AccessoriesSlotCount";

	public static readonly StaticString ActionPointsSpent = "ActionPointsSpent";

	public static readonly StaticString ArmorAbsorption = "AttributeDefense";

	public static readonly StaticString Armor = "AttributeArmor";

	public static readonly StaticString ArmyUnitSlot = "ArmyUnitSlot";

	public static readonly StaticString AssignmentCooldown = "AssignmentCooldown";

	public static readonly StaticString Altitude = "Altitude";

	public static readonly StaticString AttackPerRound = "AttackPerRound";

	public static readonly StaticString AttackPerRoundDone = "AttackPerRoundDone";

	public static readonly StaticString AttackPerRoundTaken = "AttackPerRoundTaken";

	public static readonly StaticString BattleInitiative = "AttributeInitiative";

	public static readonly StaticString BattleMaximumMovement = "BattleMaximumMovement";

	public static readonly StaticString BattleMorale = "BattleMorale";

	public static readonly StaticString BattleMoraleBonus = "BattleMoraleBonus";

	public static readonly StaticString BattleMoraleFromGround = "BattleMoraleFromGround";

	public static readonly StaticString BattleMoraleCumulated = "BattleMoraleCumulated";

	public static readonly StaticString BattleMoraleBonusPerAlly = "BattleMoraleBonusPerAlly";

	public static readonly StaticString BattleMoraleBonusPerEnemy = "BattleMoraleBonusPerEnemy";

	public static readonly StaticString BattleMoraleBonusPerAttack = "BattleMoraleBonusPerAttack";

	public static readonly StaticString BattleMoraleBonusPerDefense = "BattleMoraleBonusPerDefense";

	public static readonly StaticString BattleMoraleBonusPerEnemyAltitudeDifference = "BattleMoraleBonusPerEnemyAltitudeDifference";

	public static readonly StaticString BattleMovement = "BattleMovement";

	public static readonly StaticString BattleRange = "BattleRange";

	public static readonly StaticString BattleUnitBonus = "BattleUnitBonus";

	public static readonly StaticString BlindAttack = "BlindAttack";

	public static readonly StaticString CanPlayBattleRound = "CanPlayBattleRound";

	public static readonly StaticString CanUpdateFormation = "CanUpdateFormation";

	public static readonly StaticString CanBeHealed = "CanBeHealed";

	public static readonly StaticString CanTakePhysicalDamage = "CanTakePhysicalDamage";

	public static readonly StaticString CanAttackThroughImpassableTransition = "CanAttackThroughImpassableTransition";

	public static readonly StaticString CanCounter = "CanCounter";

	public static readonly StaticString Charge = "Charge";

	public static readonly StaticString CityDefensePoint = "CityDefensePoint";

	public static readonly StaticString CityDefensePointLossPerTurn = "CityDefensePointLossPerTurn";

	public static readonly StaticString CityDefensePointLossToHealthLossRatio = "CityDefensePointLossToHealthLossRatio";

	public static readonly StaticString CityDefensePointRecoveryPerTurn = "CityDefensePointRecoveryPerTurn";

	public static readonly StaticString CityUnitSlot = "CityUnitSlot";

	public static readonly StaticString ConvertedVillageUnitProductionTimer = "ConvertedVillageUnitProductionTimer";

	public static readonly StaticString CooldownReduction = "CooldownReduction";

	public static readonly StaticString CurrentEra = "CurrentEra";

	public static readonly StaticString CurrentInjuredValue = "CurrentInjuredValue";

	public static readonly StaticString CurrentTurn = "CurrentTurn";

	public static readonly StaticString Damage = "AttributeDamage";

	public static readonly StaticString DamageMultiplier = "DamageMultiplier";

	public static readonly StaticString DefensiveMilitaryPower = "DefensiveMilitaryPower";

	public static readonly StaticString DefensivePower = "DefensivePower";

	public static readonly StaticString DefensivePowerDamageReceived = "DefensivePowerDamageReceived";

	public static readonly StaticString CoastalDefensivePower = "CoastalDefensivePower";

	public static readonly StaticString DurationOfHeroesExclusivity = "DurationOfHeroesExclusivity";

	public static readonly StaticString Defense = "AttributeDefense";

	public static readonly StaticString EmpirePlanLevelUnlocked = "EmpirePlanLevelUnlocked";

	public static readonly StaticString EmpireScaleFactor = "EmpireScaleFactor";

	public static readonly StaticString ExperienceReward = "ExperienceReward";

	public static readonly StaticString FortificationPower = "FortificationPower";

	public static readonly StaticString HealingPower = "HealingPower";

	public static readonly StaticString HealingCostMultiplier = "HealingCostMultiplier";

	public static readonly StaticString Health = "Health";

	public static readonly StaticString NormalizedHealth = "NormalizedHealth";

	public static readonly StaticString HeroUpkeep = "HeroUpkeep";

	public static readonly StaticString ImprovementCount = "ImprovementCount";

	public static readonly StaticString InjuredRecoveryPerTurn = "InjuredRecoveryPerTurn";

	public static readonly StaticString InjuredValueToEmpireMoneyConversion = "InjuredValueToEmpireMoneyConversion";

	public static readonly StaticString IntermediateTradeRoutesCount = "IntermediateTradeRoutesCount";

	public static readonly StaticString IntermediateTradeRoutesDistance = "IntermediateTradeRoutesDistance";

	public static readonly StaticString IntermediateTradeRoutesGain = "IntermediateTradeRoutesGain";

	public static readonly StaticString InfiltrateLevel = "InfiltrateLevel";

	public static readonly StaticString InfiltratedTargetPopulation = "InfiltratedTargetPopulation";

	public static readonly StaticString InfiltrationCooldown = "InfiltrationCooldown";

	public static readonly StaticString LightningDamageReduction = "LightningDamageReduction";

	public static readonly StaticString NetInfiltrationPoint = "NetInfiltrationPoint";

	public static readonly StaticString NumberOfActiveTradeRoutes = "NumberOfActiveTradeRoutes";

	public static readonly StaticString NumberOfCities = "NumberOfCities";

	public static readonly StaticString IsInteractionBlocked = "IsInteractionBlocked";

	public static readonly StaticString JailPower = "JailPower";

	public static readonly StaticString JailHeroGainPerTurn = "JailHeroGainPerTurn";

	public static readonly StaticString JailHeroPower = "JailHeroPower";

	public static readonly StaticString JailHeroUpkeep = "JailHeroUpkeep";

	public static readonly StaticString LastOverrallTradeRoutesCityDustIncome = "LastOverrallTradeRoutesCityDustIncome";

	public static readonly StaticString LastOverrallTradeRoutesCityScienceIncome = "LastOverrallTradeRoutesCityScienceIncome";

	public static readonly StaticString Level = "Level";

	public static readonly StaticString LevelDisplayed = "LevelDisplayed";

	public static readonly StaticString AttackingHitInfo = "AttackingHitInfo";

	public static readonly StaticString HitInfo = "HitInfo";

	public static readonly StaticString LevelOfCamouflage = "LevelOfCamouflage";

	public static readonly StaticString LevelOfStealth = "LevelOfStealth";

	public static readonly StaticString MaximumArmor = "MaximumAttributeArmor";

	public static readonly StaticString MaximumAssignmentCooldown = "MaximumAssignmentCooldown";

	public static readonly StaticString MaximumCityDefensePoint = "MaximumCityDefensePoint";

	public static readonly StaticString MaximumDefense = "MaximumAttributeArmor";

	public static readonly StaticString MaximumImprovementCount = "MaximumImprovementCount";

	public static readonly StaticString MaximumInfiltrationCooldown = "MaximumInfiltrationCooldown";

	public static readonly StaticString MaximumInfiltrationCooldownFromGround = "MaximumInfiltrationCooldownFromGround";

	public static readonly StaticString MaximumInfiltrationCooldownFromEmpire = "MaximumInfiltrationCooldownFromEmpire";

	public static readonly StaticString MaximumInfiltrateLevel = "MaximumInfiltrateLevel";

	public static readonly StaticString MaximumInjuredValue = "MaximumInjuredValue";

	public static readonly StaticString MaximumNumberOfColossi = "MaximumNumberOfColossi";

	public static readonly StaticString MaximumUnitSlotCount = "MaximumUnitSlotCount";

	public static readonly StaticString MaximumHealth = "MaximumHealth";

	public static readonly StaticString MaximumMovement = "MaximumMovement";

	public static readonly StaticString MaximumMovementOnWater = "MaximumMovementOnWater";

	public static readonly StaticString MaximumMovementOnLand = "MaximumMovementOnLand";

	public static readonly StaticString MaximumNumberOfActionPoints = "MaximumNumberOfActionPoints";

	public static readonly StaticString MaximumNumberOfExclusiveHeroes = "MaximumNumberOfExclusiveHeroes";

	public static readonly StaticString MaximumNumberOfTradeRoutes = "MaximumNumberOfTradeRoutes";

	public static readonly StaticString MaximumPillageDefense = "MaximumPillageDefense";

	public static readonly StaticString MaximumPillageCooldown = "MaximumPillageCooldown";

	public static readonly StaticString MaximumSeasonPredictabilityError = "MaximumSeasonPredictabilityError";

	public static readonly StaticString MaximumSkillPoints = "MaximumSkillPoints";

	public static readonly StaticString MilitaryPower = "MilitaryPower";

	public static readonly StaticString LandMilitaryPower = "LandMilitaryPower";

	public static readonly StaticString NavalMilitaryPower = "NavalMilitaryPower";

	public static readonly StaticString MilitaryUpkeep = "MilitaryUpkeep";

	public static readonly StaticString MindControlCounter = "MindControlCounter";

	public static readonly StaticString MinorFactionSlotCount = "MinorFactionSlotCount";

	public static readonly StaticString MovementRatio = "MovementRatio";

	public static readonly StaticString Movement = "Movement";

	public static readonly StaticString NumberOfColossi = "NumberOfColossi";

	public static readonly StaticString NumberOfConvertedVillages = "NumberOfConvertedVillages";

	public static readonly StaticString NumberOfPawns = "NumberOfPawns";

	public static readonly StaticString NumberOfRebuildPacifiedVillage = "NumberOfRebuildPacifiedVillage";

	public static readonly StaticString NumberOfTurnSinceCommercialAggreementBegining = "NumberOfTurnSinceCommercialAggreementBegining";

	public static readonly StaticString NumberOfTurnSinceResearchAgreementBegining = "NumberOfTurnSinceResearchAgreementBegining";

	public static readonly StaticString NumberOfExtensionAround = "NumberOfExtensionAround";

	public static readonly StaticString Attack = "AttributeAttack";

	public static readonly StaticString OffensiveMilitaryPower = "OffensiveMilitaryPower";

	public static readonly StaticString OverrallTradeRoutesCityDustIncome = "OverrallTradeRoutesCityDustIncome";

	public static readonly StaticString OverrallTradeRoutesCityScienceIncome = "OverrallTradeRoutesCityScienceIncome";

	public static readonly StaticString Ownership = "Ownership";

	public static readonly StaticString OwnershipRecoveryRate = "OwnershipRecoveryRate";

	public static readonly StaticString PillageDefense = "PillageDefense";

	public static readonly StaticString PillageDefenseRecovery = "PillageDefenseRecovery";

	public static readonly StaticString PillagePower = "PillagePower";

	public static readonly StaticString PillageCooldown = "PillageCooldown";

	public static readonly StaticString Population = "Population";

	public static readonly StaticString PopulationBonus = "PopulationBonus";

	public static readonly StaticString RoundUpProgress = "RoundUpProgress";

	public static readonly StaticString RoundUpTurnToActivate = "RoundUpTurnToActivate";

	public static readonly StaticString SkillPointsSpent = "SkillPointsSpent";

	public static readonly StaticString TileBonus = "TileBonus";

	public static readonly StaticString UnitAccumulatedExperience = "AccumulatedExperience";

	public static readonly StaticString UnitExperience = "Experience";

	public static readonly StaticString UnitExperienceGainModifier = "ExperienceGainModifier";

	public static readonly StaticString UnitExperienceGainPerTurn = "ExperienceGainPerTurn";

	public static readonly StaticString UnitNextLevelExperience = "NextLevelExperience";

	public static readonly StaticString UnitExperienceRewardAtCreation = "ExperienceRewardAtCreation";

	public static readonly StaticString UnitSlotCount = "UnitSlotCount";

	public static readonly StaticString UnitSpawnCountOnConvert = "UnitSpawnCountOnConvert";

	public static readonly StaticString UnitValue = "UnitValue";

	public static readonly StaticString ReinforcementPointCount = "ReinforcementPointCount";

	public static readonly StaticString RemainingTime = "RemainingTime";

	public static readonly StaticString Seniority = "Seniority";

	public static readonly StaticString ShiftingForm = "ShiftingForm";

	public static readonly StaticString SpentBattleMovement = "SpentBattleMovement";

	public static readonly StaticString TeleportationRange = "TeleportationRange";

	public static readonly StaticString TradeRouteGain = "TradeRouteGain";

	public static readonly StaticString TradersBuyoutMultiplier = "TradersBuyoutMultiplier";

	public static readonly StaticString TradersSelloutMultiplier = "TradersSelloutMultiplier";

	public static readonly StaticString TradeRouteCityDustRevenue = "TradeRouteCityDustRevenue";

	public static readonly StaticString TradeRouteCityScienceRevenue = "TradeRouteCityScienceRevenue";

	public static readonly StaticString Workers = "Workers";

	public static readonly StaticString DetectionRange = "DetectionRange";

	public static readonly StaticString VisionHeight = "VisionHeight";

	public static readonly StaticString VisionRange = "VisionRange";

	public static readonly StaticString InteractOddsTriggeringAQuest = "Interact_OddsTriggeringAQuest";

	public static readonly StaticString InteractOddsLooting = "Interact_OddsLooting";

	public static readonly StaticString InteractOddsLootingNothing = "Interact_OddsLootingNothing";

	public static readonly StaticString BribeCost = "BribeCost";

	public static readonly StaticString ConvertNeutralCost = "ConvertNeutralCost";

	public static readonly StaticString ConvertHostileCost = "ConvertHostileCost";

	public static readonly StaticString ConvertDestructedCost = "ConvertDestructedCost";

	public static readonly StaticString DissentCost = "DissentCost";

	public static readonly StaticString DissentCostReduction = "DissentCostReduction";

	public static readonly StaticString DissentCostPenalty = "DissentCostPenalty";

	public static readonly StaticString DissentDestroyedCost = "DissentDestroyedCost";

	public static readonly StaticString DissentPacifiedCost = "DissentPacifiedCost";

	public static readonly StaticString BonusMovementOnEnemySpotted = "BonusMovementOnEnemySpotted";

	public static readonly StaticString TraitBonusExperienceToDustConversionFactor = "TraitBonusExperienceToDustConversionFactor";

	public static readonly StaticString DiplomaticAbilityPrestigeRewardOnKill = "DiplomaticAbilityPrestigeRewardOnKill";

	public static readonly StaticString DiplomaticAbilityBountyRewardOnKill = "DiplomaticAbilityBountyRewardOnKill";

	public static readonly StaticString PopulationAmountContainedBySettler = "PopulationAmountContainedBySettler";

	public static readonly StaticString BoosterDurationMultiplier = "BoosterDurationMultiplier";

	public static readonly StaticString GameSpeedMultiplier = "GameSpeedMultiplier";

	public static readonly StaticString AffinityRelationScoreSum = "AffinityRelationScoreSum";

	public static readonly StaticString EmpirePointToPeacePointFactor = "EmpirePointToPeacePointFactor";

	public static readonly StaticString PeacePointGainMultiplier = "PeacePointGainMultiplier";

	public static readonly StaticString PrestigeTrendBonusMaximumValue = "PrestigeTrendBonusMaximumValue";

	public static readonly StaticString PrestigeTrendBonusTrend = "PrestigeTrendBonusTrend";

	public static readonly StaticString PrestigeTrendBonus = "PrestigeTrendBonus";

	public static readonly StaticString EmpirePointSpentInSignificantDiplomacy = "EmpirePointSpentInSignificantDiplomacy";

	public static readonly StaticString UnknownCount = "UnknownCount";

	public static readonly StaticString TruceCount = "TruceCount";

	public static readonly StaticString WarCount = "WarCount";

	public static readonly StaticString ColdWarCount = "ColdWarCount";

	public static readonly StaticString PeaceCount = "PeaceCount";

	public static readonly StaticString AllianceCount = "AllianceCount";

	public static readonly StaticString MarketplaceUnitCostMultiplier = "MarketplaceUnitCostMultiplier";

	public static readonly StaticString MarketplaceLuxuryCostMultiplier = "MarketplaceLuxuryCostMultiplier";

	public static readonly StaticString MarketplaceStrategicCostMultiplier = "MarketplaceStrategicCostMultiplier";

	public static readonly StaticString MarketplaceBoosterFoodCostMultiplier = "MarketplaceBoosterFoodCostMultiplier";

	public static readonly StaticString MarketplaceBoosterIndustryCostMultiplier = "MarketplaceBoosterIndustryCostMultiplier";

	public static readonly StaticString SeaMonsterImmune = "SeaMonsterImmune";

	public static readonly StaticString DeviceCharges = "DeviceCharges";

	public static readonly StaticString DeviceChargesPerTurn = "DeviceChargesPerTurn";

	public static readonly StaticString DeviceChargesToActivate = "DeviceChargesToActivate";

	public static readonly StaticString DeviceRange = "DeviceRange";

	public static readonly StaticString DeviceDismantleDefense = "DeviceDismantleDefense";

	public static readonly StaticString DeviceDismantlePower = "DeviceDismantlePower";

	public static readonly StaticString Lavapool = "Lavapool";

	public static readonly StaticString LavapoolStock = "LavapoolStock";

	public static readonly StaticString NetLavapool = "NetLavapool";

	public static readonly StaticString ArmyRetrofitCostMultiplier = "ArmyRetrofitCostMultiplier";

	public static readonly StaticString SiegeDamageNeededToObtainBooster = "SiegeDamageNeededToObtainBooster";

	public static readonly StaticString SiegeDamageStock = "SiegeDamageStock";

	public static readonly StaticString CampDistrictsDust = "CampDistrictsDust";

	public static readonly StaticString CampDistrictsFood = "CampDistrictsFood";

	public static readonly StaticString CampDistrictsCityPoint = "CampDistrictsCityPoint";

	public static readonly StaticString CampDistrictsIndustry = "CampDistrictsIndustry";

	public static readonly StaticString CampDistrictsScience = "CampDistrictsScience";

	public static readonly StaticString SelloutPrice = "SelloutPrice";

	public static readonly StaticString TemplesSearchedThisSeason = "TemplesSearchedThisSeason";

	public static readonly StaticString TilesTerraformed = "TilesTerraformed";

	public static readonly StaticString TerraformState = "TerraformState";

	public static readonly StaticString TilesMovedThisTurn = "TilesMovedThisTurn";

	public static readonly StaticString ReceivedTerrainDamageMultiplier = "ReceivedTerrainDamageMultiplier";

	public static readonly StaticString VolcanicRegen = "VolcanicRegen";

	public static readonly StaticString SeasonIntensityMultiplier = "SeasonIntensityMultiplier";

	public static readonly StaticString IntegratedMajorFactionCount = "IntegratedMajorFactionCount";

	public static readonly StaticString SpawnedKaijusCounter = "SpawnedKaijusCounter";

	public static readonly StaticString SpawnedKaijusGlobalCounter = "SpawnedKaijusGlobalCounter";

	public static readonly StaticString KaijuUnitProductionTimer = "KaijuUnitProductionTimer";

	public static readonly StaticString KaijuStunTimer = "KaijuStunnedTimer";

	public static readonly StaticString KaijuNextTurnToSpawnUnit = "NextTurnToSpawnUnit";

	public static readonly StaticString KaijuNextTurnToRecoverFromStun = "NextTurnToRecoverFromStun";

	public static readonly StaticString SharedSightActive = "SharedSightActive";

	public static readonly StaticString SharedSightExploration = "SharedSightExploration";

	public static readonly StaticString SharedSightHeight = "SharedSightHeight";

	public static readonly StaticString SharedSightRange = "SharedSightRange";

	public static readonly StaticString CreepingNodeTurnsCounter = "TurnsCounter";

	public static readonly StaticString CreepingNodeTimesUpgraded = "TimesUpgraded";

	public static readonly StaticString CreepingNodesFoodUpkeep = "CreepingNodesFoodUpkeep";

	public static readonly StaticString CreepingNodeDismantleDefense = "CreepingNodeDismantleDefense";

	public static readonly StaticString CreepingNodeDismantlePower = "CreepingNodeDismantlePower";

	public static readonly StaticString CreepingNodeRetaliationDamage = "RetaliationDamage";

	public static readonly StaticString NumberOfCreepingNodes = "NumberOfCreepingNodes";

	public static readonly StaticString NumberOfFinishedCreepingNodes = "NumberOfFinishedCreepingNodes";

	public static readonly StaticString NodeCostIncrement = "NodeCostIncrement";

	public static readonly StaticString CityPointEarthquakeDamage = "CityPointEarthquakeDamage";

	public static readonly StaticString CityGarrisonEarthquakeDamage = "CityGarrisonEarthquakeDamage";

	public static readonly StaticString CityOwnedTurn = "CityOwnedTurn";

	public static readonly StaticString OvergrownCityRazeCityCooldownInTurns = "OvergrownCityRazeCityCooldownInTurns";

	public static readonly StaticString AllayiBoosterDurationMultiplier = "AllayiBoosterDurationMultiplier";

	public static readonly StaticString MarketplaceMercCostMultiplier = "MarketplaceMercCostMultiplier";

	public static readonly StaticString MarketplaceStockpileCostMultiplier = "MarketplaceStockpileCostMultiplier";

	public static readonly StaticString NodeCostIncrementModifier = "NodeCostIncrementModifier";

	public static readonly StaticString NodeOvergrownVillageCostModifier = "NodeOvergrownVillageCostModifier";

	public static readonly StaticString ConvertVillageMultiplier = "ConvertVillageMultiplier";

	public static readonly StaticString NumberOfOwnedOceanicRegions = "NumberOfOwnedOceanicRegions";

	public static readonly StaticString ELCPCadaversPerVillage = "ELCPCadaversPerVillage";

	public static readonly StaticString ELCPCadavresPerSacrifice = "ELCPCadavresPerSacrifice";
}
