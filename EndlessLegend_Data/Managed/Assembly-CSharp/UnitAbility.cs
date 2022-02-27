using System;
using System.Xml.Serialization;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Xml;

public class UnitAbility : ConstructibleElement
{
	static UnitAbility()
	{
		UnitAbility.ReadonlyArmyUnique = "UnitAbilityArmyUnique";
		UnitAbility.ReadonlyColonize = "UnitAbilityColonization";
		UnitAbility.ReadonlyPreacher = "UnitAbilityPreacher";
		UnitAbility.ReadonlyResettle = "UnitAbilityResettle";
		UnitAbility.ReadonlyLastStand = "UnitAbilityLastStand";
		UnitAbility.ReadonlyUnsalable = "UnitAbilityUnsalable";
		UnitAbility.ReadonlyCannotRegen = "UnitAbilityCannotRegen";
		UnitAbility.ReadonlyCanRegenWithVillage = "UnitAbilityCanRegenWithVillage";
		UnitAbility.ReadonlySpy = "UnitAbilitySpy";
		UnitAbility.ReadonlyParasite = "UnitAbilityParasite";
		UnitAbility.ReadonlyElusive = "UnitAbilityElusive";
		UnitAbility.ReadonlyDualWield = "UnitAbilityDualWield";
		UnitAbility.ReadonlyShifterNature = "UnitAbilityShifterNature";
		UnitAbility.ReadonlyHarbinger = "UnitAbilityHarbinger";
		UnitAbility.ReadonlyTransportShip = "UnitAbilityTransportShip";
		UnitAbility.ReadonlyRapidMutation = "UnitAbilityRapidMutation";
		UnitAbility.ReadonlySubmersible = "UnitAbilitySubmersible";
		UnitAbility.ReadonlyEssenceHarvest = "UnitAbilityEssenceHarvest";
		UnitAbility.ReadonlyPortableForge = "UnitAbilityPortableForge";
		UnitAbility.UnitAbilityAllowAssignationUnderSiege = "UnitAbilityAllowAssignationUnderSiege";
		UnitAbility.UnitAbilityInstantHeal = "UnitAbilityInstantHeal";
		UnitAbility.UnitAbilityImmolation = "UnitAbilityImmolation";
		UnitAbility.UnitAbilityInnerFire = "UnitAbilityInnerFire";
		UnitAbility.ReadonlyIndestructible = "UnitAbilityIndestructible";
		UnitAbility.ReadonlyNodeRegeneration = "UnitAbilityNodeRegeneration";
		UnitAbility.UnitAbilityGeomancy = "UnitAbilityGeomancy";
	}

	[XmlElement(Type = typeof(UnitAbility.UnitAbilityLevelDefinition), ElementName = "UnitAbilityLevelDefinition")]
	public UnitAbility.UnitAbilityLevelDefinition[] AbilityLevels { get; set; }

	[XmlElement("BattleActionUnitReference")]
	public XmlNamedReference[] BattleActionUnitReferences { get; set; }

	[XmlElement("BattleTargetingUnitBehaviorWeightsReferences")]
	public XmlNamedReference[] BattleTargetingUnitBehaviorWeightsReferences { get; set; }

	[XmlAttribute]
	public bool Hidden { get; set; }

	[XmlAttribute]
	public bool Persistent { get; set; }

	public static readonly string ReadonlyArmyUnique;

	public static readonly string ReadonlyColonize;

	public static readonly string ReadonlyPreacher;

	public static readonly string ReadonlyResettle;

	public static readonly string ReadonlyLastStand;

	public static readonly string ReadonlyUnsalable;

	public static readonly string ReadonlyCannotRegen;

	public static readonly string ReadonlyCanRegenWithVillage;

	public static readonly string ReadonlySpy;

	public static readonly string ReadonlyParasite;

	public static readonly string ReadonlyElusive;

	public static readonly string ReadonlyDualWield;

	public static readonly string ReadonlyShifterNature;

	public static readonly string ReadonlyHarbinger;

	public static readonly string ReadonlyTransportShip;

	public static readonly string ReadonlyRapidMutation;

	public static readonly string ReadonlySubmersible;

	public static readonly string ReadonlyEssenceHarvest;

	public static readonly string ReadonlyPortableForge;

	public static readonly string UnitAbilityAllowAssignationUnderSiege;

	public static readonly string UnitAbilityInstantHeal;

	public static readonly string UnitAbilityImmolation;

	public static readonly string UnitAbilityInnerFire;

	public static readonly string ReadonlyIndestructible;

	public static readonly string ReadonlyNodeRegeneration;

	public static readonly string UnitAbilityGeomancy = "UnitAbilityGeomancy";

	public class UnitAbilityLevelDefinition
	{
		[XmlElement("BattleActionUnitReference")]
		public XmlNamedReference[] BattleActionUnitReferences { get; set; }

		[XmlElement("BattleTargetingUnitBehaviorWeightsReferences")]
		public XmlNamedReference[] BattleTargetingUnitBehaviorWeightsReferences { get; set; }

		[XmlAttribute]
		public int Level { get; set; }

		[XmlElement(Type = typeof(SimulationDescriptorReference), ElementName = "SimulationDescriptorReference")]
		public SimulationDescriptorReference[] SimulationDescriptorReferences { get; set; }
	}
}
