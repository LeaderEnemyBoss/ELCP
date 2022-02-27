using System;
using Amplitude;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class RequestUnitListMessage : BlackboardMessage
{
	public RequestUnitListMessage() : base(BlackboardLayerID.Empire)
	{
		this.Reset();
	}

	public RequestUnitListMessage(int empireTarget, ArmyPattern pattern, float priority, AICommanderMissionDefinition.AICommanderCategory commanderCategory) : base(BlackboardLayerID.Empire)
	{
		this.Reset();
		this.CommanderCategory = commanderCategory;
		this.EmpireTarget = empireTarget;
		this.ArmyPattern = pattern;
		this.Priority = priority;
	}

	public ArmyPattern ArmyPattern { get; set; }

	public AICommanderMissionDefinition.AICommanderCategory CommanderCategory { get; set; }

	public float CurrentFulfillement { get; set; }

	public int EmpireTarget { get; set; }

	public RequestUnitListMessage.RequestUnitListState ExecutionState { get; set; }

	public WorldPosition FinalPosition { get; set; }

	public int ForceSourceRegion { get; set; }

	public float MinimumNeededArmyFulfillement { get; set; }

	public float MissingMilitaryPower { get; set; }

	public float Priority { get; protected set; }

	public override void Canceled()
	{
		base.Canceled();
	}

	public override void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		this.EmpireTarget = reader.GetAttribute<int>("EmpireTarget");
		this.Priority = reader.GetAttribute<float>("MilitaryStress");
		this.ExecutionState = (RequestUnitListMessage.RequestUnitListState)reader.GetAttribute<int>("ExecutionState");
		this.ForceSourceRegion = -1;
		this.MinimumNeededArmyFulfillement = 1f;
		this.CurrentFulfillement = 1f;
		this.MissingMilitaryPower = 0f;
		if (num >= 2)
		{
			this.ForceSourceRegion = reader.GetAttribute<int>("ForceSourceRegion");
			this.MinimumNeededArmyFulfillement = reader.GetAttribute<float>("MinimumNeededArmyFulfillement");
			this.CurrentFulfillement = reader.GetAttribute<float>("CurrentFulfillement");
			this.MissingMilitaryPower = reader.GetAttribute<float>("MissingMilitaryPower");
			if (num >= 5)
			{
				this.CommanderCategory = reader.GetAttribute<AICommanderMissionDefinition.AICommanderCategory>("CommanderCategoryEnum");
			}
		}
		base.ReadXml(reader);
		if (reader.IsStartElement("ArmyPattern"))
		{
			ArmyPattern armyPattern = new ArmyPattern();
			reader.ReadElementSerializable<ArmyPattern>(ref armyPattern);
			this.ArmyPattern = armyPattern;
		}
		if (reader.IsStartElement("FinalPosition"))
		{
			this.FinalPosition = reader.ReadElementSerializable<WorldPosition>("FinalPosition");
		}
	}

	public override bool Release()
	{
		if (this.ArmyPattern != null)
		{
			this.ArmyPattern.Release();
			this.ArmyPattern = null;
		}
		return base.Release();
	}

	public void SetPriority(float priority)
	{
		this.Priority = priority;
		if (float.IsNaN(priority) || float.IsInfinity(priority))
		{
			Diagnostics.LogWarning("[SCORING] Setting invalid priority of {1} for RequestUnitListMessage {0}", new object[]
			{
				this.CommanderCategory,
				this.Priority
			});
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		int num = writer.WriteVersionAttribute(5);
		writer.WriteAttributeString<int>("EmpireTarget", this.EmpireTarget);
		writer.WriteAttributeString<float>("MilitaryStress", this.Priority);
		writer.WriteAttributeString<int>("ExecutionState", (int)this.ExecutionState);
		if (num >= 2)
		{
			writer.WriteAttributeString<int>("ForceSourceRegion", this.ForceSourceRegion);
			writer.WriteAttributeString<float>("MinimumNeededArmyFulfillement", this.MinimumNeededArmyFulfillement);
			writer.WriteAttributeString<float>("CurrentFulfillement", this.CurrentFulfillement);
			writer.WriteAttributeString<float>("MissingMilitaryPower", this.MissingMilitaryPower);
			if (num >= 5)
			{
				writer.WriteAttributeString<AICommanderMissionDefinition.AICommanderCategory>("CommanderCategoryEnum", this.CommanderCategory);
			}
		}
		base.WriteXml(writer);
		if (this.ArmyPattern != null)
		{
			IXmlSerializable armyPattern = this.ArmyPattern;
			writer.WriteElementSerializable<IXmlSerializable>(ref armyPattern);
		}
		IXmlSerializable xmlSerializable = this.FinalPosition;
		writer.WriteElementSerializable<IXmlSerializable>("FinalPosition", ref xmlSerializable);
	}

	private void Reset()
	{
		base.TimeOut = int.MinValue;
		base.State = BlackboardMessage.StateValue.Message_None;
		this.CommanderCategory = AICommanderMissionDefinition.AICommanderCategory.Undefined;
		this.ArmyPattern = null;
		this.EmpireTarget = -1;
		this.ExecutionState = RequestUnitListMessage.RequestUnitListState.Pending;
		this.Priority = -1f;
		this.FinalPosition = WorldPosition.Invalid;
		this.ForceSourceRegion = -1;
		this.MinimumNeededArmyFulfillement = 1f;
		this.MissingMilitaryPower = 0f;
	}

	public enum RequestUnitListState
	{
		Pending,
		UnitsInProduction,
		Regrouping,
		RegroupingPending,
		ArmyAvailable
	}
}
