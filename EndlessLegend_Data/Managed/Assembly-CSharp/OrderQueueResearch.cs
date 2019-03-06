using System;
using System.IO;
using Amplitude;
using Amplitude.Unity.Game.Orders;

public class OrderQueueResearch : global::Order
{
	public OrderQueueResearch(int empireIndex, DepartmentOfScience.ConstructibleElement constructibleElement) : base(empireIndex)
	{
		if (constructibleElement == null)
		{
			throw new ArgumentNullException("constructibleElement");
		}
		this.ConstructionGameEntityGUID = GameEntityGUID.Zero;
		this.ConstructibleElementName = constructibleElement.Name;
	}

	[Amplitude.Unity.Game.Orders.Order.Flow(Amplitude.Unity.Game.Orders.Order.Control.SetByClient)]
	public StaticString ConstructibleElementName { get; private set; }

	[Amplitude.Unity.Game.Orders.Order.Flow(Amplitude.Unity.Game.Orders.Order.Control.SetByServerPreprocessor)]
	public GameEntityGUID ConstructionGameEntityGUID { get; set; }

	public override StaticString Path
	{
		get
		{
			return OrderQueueResearch.AuthenticationPath;
		}
	}

	[Amplitude.Unity.Game.Orders.Order.Flow(Amplitude.Unity.Game.Orders.Order.Control.SetByServerPreprocessor)]
	public ConstructionResourceStock[] ResourceStocks { get; set; }

	public override string ToString()
	{
		return base.ToString() + string.Format(", constructible element guid: {0}, technology name: {1}", this.ConstructionGameEntityGUID, this.ConstructibleElementName);
	}

	public override void Pack(BinaryWriter writer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		base.Pack(writer);
		Diagnostics.Assert(!StaticString.IsNullOrEmpty(this.ConstructibleElementName));
		writer.Write(this.ConstructionGameEntityGUID);
		writer.Write(this.ConstructibleElementName);
		if (this.ResourceStocks == null)
		{
			writer.Write(0);
		}
		else
		{
			writer.Write(this.ResourceStocks.Length);
			for (int i = 0; i < this.ResourceStocks.Length; i++)
			{
				Diagnostics.Assert(this.ResourceStocks[i] != null);
				Diagnostics.Assert(!StaticString.IsNullOrEmpty(this.ResourceStocks[i].PropertyName));
				writer.Write(this.ResourceStocks[i].PropertyName);
				writer.Write(this.ResourceStocks[i].Stock);
			}
		}
	}

	public override void Unpack(BinaryReader reader)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		base.Unpack(reader);
		this.ConstructionGameEntityGUID = reader.ReadUInt64();
		this.ConstructibleElementName = reader.ReadString();
		int num = reader.ReadInt32();
		this.ResourceStocks = new ConstructionResourceStock[num];
		for (int i = 0; i < this.ResourceStocks.Length; i++)
		{
			this.ResourceStocks[i] = new ConstructionResourceStock(reader.ReadString(), null)
			{
				Stock = reader.ReadSingle()
			};
		}
	}

	public static readonly StaticString AuthenticationPath = "DepartmentOfScience/OrderQueueResearch";
}
