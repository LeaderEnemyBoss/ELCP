using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.Simulation;

[XmlRoot("AreaOfEffect")]
[XmlType("AreaOfEffect")]
public class BattleEffectsArea : BattleEffects
{
	[XmlAttribute]
	public bool AvoidCastingUnit { get; set; }

	[XmlElement("BattleEffects")]
	public BattleEffects[] BattleEffects { get; set; }

	[XmlAttribute]
	public BattleEffectsArea.AreaType Type { get; set; }

	[XmlAttribute]
	public string Parameter1
	{
		get
		{
			return this.preprocessedParameter.ToString();
		}
		set
		{
			this.preprocessedParameter = SimulationHelpers.Evaluate(value);
		}
	}

	[XmlAttribute("RealizationApplicationMethod")]
	public BattleEffect.BattleEffectRealizationApplicationMethod RealizationApplicationMethod { get; protected set; }

	[XmlAttribute("RealizationApplicationData")]
	public string RealizationApplicationData { get; protected set; }

	[XmlIgnore]
	public StaticString RealizationVisualEffectName { get; private set; }

	[XmlAttribute("RealizationVisualEffectName")]
	public string XmlSerializableRealizationVisualEffectName
	{
		get
		{
			return this.RealizationVisualEffectName;
		}
		set
		{
			this.RealizationVisualEffectName = value;
		}
	}

	public IPathfindingArea GetArea(WorldPosition center, WorldOrientation orientation, IEnumerable<WorldPosition> interactivePositions, WorldParameters worldParameters, IPathfindingArea battleArea, SimulationObject context)
	{
		switch (this.Type)
		{
		case BattleEffectsArea.AreaType.Circle:
		{
			int distance = this.GetDistance(context);
			return new WorldCircle(center, distance);
		}
		case BattleEffectsArea.AreaType.Cone:
		{
			int distance2 = this.GetDistance(context);
			return new WorldCone(center, distance2, orientation);
		}
		case BattleEffectsArea.AreaType.Chain:
		{
			int distance3 = this.GetDistance(context);
			return new BattleAreaChain(center, interactivePositions, distance3, worldParameters);
		}
		case BattleEffectsArea.AreaType.Line:
		{
			int distance4 = this.GetDistance(context);
			return new WorldLine(center, distance4, orientation);
		}
		case BattleEffectsArea.AreaType.BattleGround:
			return battleArea;
		default:
			throw new NotImplementedException();
		}
	}

	public int GetDistance(SimulationObject initiator)
	{
		if (this.preprocessedParameter is StaticString)
		{
			return (int)initiator.GetPropertyValue(this.preprocessedParameter as StaticString);
		}
		if (this.preprocessedParameter is float)
		{
			return (int)((float)this.preprocessedParameter);
		}
		if (this.preprocessedParameter is int)
		{
			return (int)this.preprocessedParameter;
		}
		Diagnostics.LogError("Parameter1 <{0}> is incorrect in battleEffectArea. It should be an integer!", new object[]
		{
			this.Parameter1
		});
		return 1;
	}

	private object preprocessedParameter;

	public enum AreaType
	{
		Circle,
		Cone,
		Chain,
		Line,
		BattleGround
	}
}
