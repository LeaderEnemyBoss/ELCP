using System;
using System.Collections.Generic;
using System.IO;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class TradableBooster : Tradable, IXmlSerializable, ITradableWithStacking
{
	public override void Deserialize(BinaryReader reader)
	{
		base.Deserialize(reader);
		this.BoosterDefinitionName = reader.ReadString();
		int num = reader.ReadInt32();
		this.GameEntityGUIDs = new GameEntityGUID[num];
		for (int i = 0; i < num; i++)
		{
			this.GameEntityGUIDs[i] = reader.ReadUInt64();
		}
	}

	public override void Serialize(BinaryWriter writer)
	{
		base.Serialize(writer);
		writer.Write(this.BoosterDefinitionName.ToString());
		if (this.GameEntityGUIDs == null)
		{
			writer.Write(0);
		}
		else
		{
			writer.Write(this.GameEntityGUIDs.Length);
			for (int i = 0; i < this.GameEntityGUIDs.Length; i++)
			{
				writer.Write(this.GameEntityGUIDs[i]);
			}
		}
	}

	public override void ReadXml(XmlReader reader)
	{
		base.ReadXml(reader);
		this.BoosterDefinitionName = reader.ReadElementString("BoosterDefinitionName");
		int attribute = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("GameEntityGUIDs");
		if (attribute > 0)
		{
			this.GameEntityGUIDs = new GameEntityGUID[attribute];
			for (int i = 0; i < attribute; i++)
			{
				this.GameEntityGUIDs[i] = reader.ReadElementString<ulong>("GameEntityGUID");
			}
		}
		else
		{
			this.GameEntityGUIDs = null;
		}
		reader.ReadEndElement("GameEntityGUIDs");
	}

	public override void WriteXml(XmlWriter writer)
	{
		base.WriteXml(writer);
		writer.WriteElementString("BoosterDefinitionName", this.BoosterDefinitionName.ToString());
		writer.WriteStartElement("GameEntityGUIDs");
		if (this.GameEntityGUIDs != null)
		{
			writer.WriteAttributeString<int>("Count", this.GameEntityGUIDs.Length);
			for (int i = 0; i < this.GameEntityGUIDs.Length; i++)
			{
				writer.WriteElementString<GameEntityGUID>("GameEntityGUID", this.GameEntityGUIDs[i]);
			}
		}
		else
		{
			writer.WriteAttributeString<int>("Count", 0);
		}
		writer.WriteEndElement();
	}

	public StaticString BoosterDefinitionName { get; set; }

	public GameEntityGUID[] GameEntityGUIDs { get; set; }

	public static float GetPriceWithSalesTaxes(StaticString boosterDefinitionName, TradableTransactionType transactionType, Empire empire, float quantity = 1f)
	{
		IDatabase<TradableCategoryDefinition> database = Databases.GetDatabase<TradableCategoryDefinition>(false);
		Diagnostics.Assert(database != null);
		StaticString key = "Tradable" + boosterDefinitionName;
		TradableCategoryDefinition tradableCategoryDefinition;
		if (database.TryGetValue(key, out tradableCategoryDefinition))
		{
			float unitPrice = Tradable.GetUnitPrice(tradableCategoryDefinition, 0f);
			return Tradable.ApplySalesTaxes(unitPrice * quantity, transactionType, empire);
		}
		return 0f;
	}

	public override bool IsTradableValid(Empire empire)
	{
		IDatabase<BoosterDefinition> database = Databases.GetDatabase<BoosterDefinition>(false);
		BoosterDefinition boosterDefinition;
		if (!database.TryGetValue(this.BoosterDefinitionName, out boosterDefinition))
		{
			Diagnostics.LogError("Cannot find the BoosterDefinition for the booster {0}", new object[]
			{
				this.BoosterDefinitionName
			});
			return false;
		}
		return (!empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitReplicants1) || !boosterDefinition.Name.ToString().Contains("Science")) && (!empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitBrokenLords2) || !boosterDefinition.Name.ToString().Contains("Food")) && (!empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitMimics2) || !boosterDefinition.Name.ToString().Contains("Industry"));
	}

	public override bool TryAllocateTo(Empire empire, float quantity, params object[] parameters)
	{
		if (this.GameEntityGUIDs == null)
		{
			return false;
		}
		if ((float)this.GameEntityGUIDs.Length < quantity)
		{
			return false;
		}
		DepartmentOfEducation agency = empire.GetAgency<DepartmentOfEducation>();
		if (agency != null)
		{
			IDatabase<BoosterDefinition> database = Databases.GetDatabase<BoosterDefinition>(false);
			BoosterDefinition constructibleElement;
			if (database != null && database.TryGetValue(this.BoosterDefinitionName, out constructibleElement))
			{
				int num = (int)quantity;
				int num2 = this.GameEntityGUIDs.Length - 1;
				while (num > 0 && num2 >= 0)
				{
					agency.AddVaultItem(new VaultItem(this.GameEntityGUIDs[num2], constructibleElement));
					num--;
					num2--;
				}
				int num3 = this.GameEntityGUIDs.Length - (int)quantity;
				if (num3 == 0)
				{
					this.GameEntityGUIDs = null;
				}
				else
				{
					GameEntityGUID[] array = new GameEntityGUID[num3];
					Array.Copy(this.GameEntityGUIDs, array, num3);
					this.GameEntityGUIDs = array;
				}
				return true;
			}
		}
		return false;
	}

	public override bool TryGetReferenceName(Empire empire, out string referenceName)
	{
		referenceName = this.BoosterDefinitionName;
		return true;
	}

	public virtual bool TryStack(ITradableWithStacking other)
	{
		if (other == null)
		{
			return false;
		}
		TradableBooster tradableBooster = other as TradableBooster;
		if (tradableBooster == null)
		{
			return false;
		}
		if (tradableBooster.TradableCategoryDefinition.Name != this.TradableCategoryDefinition.Name)
		{
			return false;
		}
		if (tradableBooster.BoosterDefinitionName != this.BoosterDefinitionName)
		{
			return false;
		}
		Diagnostics.Assert(this.GameEntityGUIDs != null || this.Quantity == 0f);
		Diagnostics.Assert(tradableBooster.GameEntityGUIDs != null);
		base.Quantity = this.Quantity + tradableBooster.Quantity;
		if (tradableBooster.Value != base.Value)
		{
			Diagnostics.LogError("Merging tradables with different value prices; aligning on highest one.");
			base.Value = Math.Max(base.Value, tradableBooster.Value);
		}
		int num = (int)this.Quantity;
		Diagnostics.Assert(num == ((this.GameEntityGUIDs == null) ? 0 : this.GameEntityGUIDs.Length) + tradableBooster.GameEntityGUIDs.Length);
		List<GameEntityGUID> list = new List<GameEntityGUID>(num);
		if (this.GameEntityGUIDs != null)
		{
			list.AddRange(this.GameEntityGUIDs);
		}
		list.AddRange(tradableBooster.GameEntityGUIDs);
		if (num > this.TradableCategoryDefinition.MaximumStackSize)
		{
			num = this.TradableCategoryDefinition.MaximumStackSize;
			base.Quantity = (float)this.TradableCategoryDefinition.MaximumStackSize;
			list.RemoveRange(this.TradableCategoryDefinition.MaximumStackSize, list.Count - this.TradableCategoryDefinition.MaximumStackSize);
		}
		if (list.Count > num)
		{
			list.RemoveRange(num, list.Count - num);
		}
		this.GameEntityGUIDs = list.ToArray();
		return true;
	}
}
