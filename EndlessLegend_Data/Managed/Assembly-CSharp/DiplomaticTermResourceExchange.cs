using System;
using System.IO;
using Amplitude;
using Amplitude.Xml;

public class DiplomaticTermResourceExchange : DiplomaticTerm, IDiplomaticTermManagement
{
	public DiplomaticTermResourceExchange(DiplomaticTermResourceExchangeDefinition definition, Empire empireWhichProposes, Empire empireWhichProvides, Empire empireWhichReceives, StaticString resourceName, float amount = 0f) : base(definition, empireWhichProposes, empireWhichProvides, empireWhichReceives)
	{
		if (StaticString.IsNullOrEmpty(resourceName))
		{
			throw new ArgumentNullException("resourceName");
		}
		this.ResourceName = resourceName;
		this.Amount = amount;
	}

	void IDiplomaticTermManagement.ApplyEffects()
	{
		Diagnostics.Assert(base.Definition is DiplomaticTermResourceExchangeDefinition);
		Diagnostics.Assert(base.EmpireWhichProvides != null && base.EmpireWhichReceives != null);
		Diagnostics.Assert(base.EmpireWhichProvides.Index != base.EmpireWhichReceives.Index);
		DepartmentOfTheTreasury agency = base.EmpireWhichProvides.GetAgency<DepartmentOfTheTreasury>();
		DepartmentOfTheTreasury agency2 = base.EmpireWhichReceives.GetAgency<DepartmentOfTheTreasury>();
		Diagnostics.Assert(agency != null && agency2 != null);
		if (this.BufferedAmount > 0f && this.BufferedAmount >= this.Amount)
		{
			this.BufferedAmount = 0f;
		}
		else if (!agency.TryTransferResources(base.EmpireWhichProvides, this.ResourceName, -this.Amount))
		{
			Diagnostics.LogError("DiplomaticTermResourceExchange.ApplyEffect failed, can't debit the empire which provides the term (resource: {0} amount: {1})", new object[]
			{
				this.ResourceName,
				-this.Amount
			});
			return;
		}
		if (!agency2.TryTransferResources(base.EmpireWhichReceives, this.ResourceName, this.Amount))
		{
			Diagnostics.LogError("DiplomaticTermResourceExchange.ApplyEffect failed, can't credt the empire which receive the term (resource: {0} amount: {1})", new object[]
			{
				this.ResourceName,
				this.Amount
			});
			return;
		}
	}

	public float Amount { get; set; }

	public StaticString ResourceName { get; private set; }

	public override bool CanApply(IDiplomaticContract diplomaticContract, params string[] flags)
	{
		if (!base.CanApply(diplomaticContract, new string[0]))
		{
			return false;
		}
		Diagnostics.Assert(base.EmpireWhichProvides != null && base.EmpireWhichReceives != null);
		Diagnostics.Assert(base.EmpireWhichProvides.Index != base.EmpireWhichReceives.Index);
		DepartmentOfTheTreasury agency = base.EmpireWhichProvides.GetAgency<DepartmentOfTheTreasury>();
		DepartmentOfTheTreasury agency2 = base.EmpireWhichReceives.GetAgency<DepartmentOfTheTreasury>();
		Diagnostics.Assert(agency != null && agency2 != null);
		float num = -this.Amount;
		if ((this.BufferedAmount != this.Amount || this.BufferedAmount == 0f) && !agency.IsTransferOfResourcePossible(base.EmpireWhichProvides, this.ResourceName, ref num))
		{
			return false;
		}
		num = this.Amount;
		return agency2.IsTransferOfResourcePossible(base.EmpireWhichReceives, this.ResourceName, ref num);
	}

	public override bool Equals(DiplomaticTerm other)
	{
		DiplomaticTermResourceExchange diplomaticTermResourceExchange = other as DiplomaticTermResourceExchange;
		return diplomaticTermResourceExchange != null && diplomaticTermResourceExchange.ResourceName == this.ResourceName;
	}

	public override void Pack(BinaryWriter writer)
	{
		base.Pack(writer);
		Diagnostics.Assert(this.ResourceName != null);
		writer.Write(this.ResourceName.ToString());
		writer.Write(this.Amount);
	}

	public override void ReadXml(XmlReader reader)
	{
		base.ReadXml(reader);
		this.ResourceName = reader.ReadElementString<string>("ResourceName");
		this.Amount = reader.ReadElementString<float>("Amount");
		try
		{
			this.BufferedAmount = reader.ReadElementString<float>("BufferedAmount");
		}
		catch
		{
			Diagnostics.LogWarning("ELCP: Error reading BufferedAmount, setting to 0");
			this.BufferedAmount = 0f;
		}
	}

	public override string ToString()
	{
		return string.Format("{0} ResourceName: {1} Amount: {2}", base.ToString(), this.ResourceName, this.Amount);
	}

	public override void Unpack(BinaryReader reader)
	{
		base.Unpack(reader);
		this.ResourceName = reader.ReadString();
		this.Amount = reader.ReadSingle();
	}

	public override void WriteXml(XmlWriter writer)
	{
		base.WriteXml(writer);
		writer.WriteElementString<string>("ResourceName", this.ResourceName);
		writer.WriteElementString<float>("Amount", this.Amount);
		writer.WriteElementString<float>("BufferedAmount", this.BufferedAmount);
	}

	public float BufferedAmount { get; set; }
}
