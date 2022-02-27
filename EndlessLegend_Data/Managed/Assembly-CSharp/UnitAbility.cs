using System;
using System.Xml.Serialization;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Xml;

public class UnitAbility : ConstructibleElement
{
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

	public static readonly string ReadonlyArmyUnique = "UnitAbilityArmyUnique";

	public static readonly string ReadonlyColonize = "UnitAbilityColonization";

	public static readonly string ReadonlyPreacher = "UnitAbilityPreacher";

	public static readonly string ReadonlyResettle = "UnitAbilityResettle";

	public static readonly string ReadonlyLastStand = "UnitAbilityLastStand";

	public static readonly string ReadonlyUnsalable = "UnitAbilityUnsalable";

	public static readonly string ReadonlyCannotRegen = "UnitAbilityCannotRegen";

	public static readonly string ReadonlyCanRegenWithVillage = "UnitAbilityCanRegenWithVillage";

	public static readonly string ReadonlySpy = "UnitAbilitySpy";

	public static readonly string ReadonlyParasite = "UnitAbilityParasite";

	public static readonly string ReadonlyElusive = "UnitAbilityElusive";

	public static readonly string ReadonlyDualWield = "UnitAbilityDualWield";

	public static readonly string ReadonlyShifterNature = "UnitAbilityShifterNature";

	public static readonly string ReadonlyHarbinger = "UnitAbilityHarbinger";

	public static readonly string ReadonlyTransportShip = "UnitAbilityTransportShip";

	public static readonly string ReadonlyRapidMutation = "UnitAbilityRapidMutation";

	public static readonly string ReadonlySubmersible = "UnitAbilitySubmersible";

	public static readonly string ReadonlyEssenceHarvest = "UnitAbilityEssenceHarvest";

	public static readonly string ReadonlyPortableForge = "UnitAbilityPortableForge";

	public static readonly string UnitAbilityAllowAssignationUnderSiege = "UnitAbilityAllowAssignationUnderSiege";

	public static readonly string UnitAbilityInstantHeal = "UnitAbilityInstantHeal";

	public static readonly string UnitAbilityImmolation = "UnitAbilityImmolation";

	public static readonly string UnitAbilityInnerFire = "UnitAbilityInnerFire";

	public static readonly string ReadonlyIndestructible = "UnitAbilityIndestructible";

	public static readonly string ReadonlyNodeRegeneration = "UnitAbilityNodeRegeneration";

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
