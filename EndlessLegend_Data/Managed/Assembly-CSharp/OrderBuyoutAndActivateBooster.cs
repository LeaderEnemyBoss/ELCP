using System;
using System.IO;
using Amplitude;
using Amplitude.Unity.Game.Orders;

public class OrderBuyoutAndActivateBooster : global::Order
{
	public OrderBuyoutAndActivateBooster(int empireIndex, string boosterDefinitionName, ulong boosterGameEntityGUID = 0UL, bool isFree = false) : base(empireIndex)
	{
		if (string.IsNullOrEmpty(boosterDefinitionName))
		{
			throw new ArgumentNullException("boosterDefinitionName", "String argument is null or empty.");
		}
		this.BoosterDefinitionName = boosterDefinitionName;
		this.BoosterGameEntityGUID = boosterGameEntityGUID;
		this.TargetGUID = 0UL;
		this.IsFree = isFree;
		this.Duration = -1;
		this.InstigatorEmpireIndex = empireIndex;
	}

	[Amplitude.Unity.Game.Orders.Order.Flow(Amplitude.Unity.Game.Orders.Order.Control.SetByClient)]
	public string BoosterDefinitionName { get; private set; }

	[Amplitude.Unity.Game.Orders.Order.Flow(Amplitude.Unity.Game.Orders.Order.Control.SetByClient)]
	public ulong TargetGUID { get; set; }

	[Amplitude.Unity.Game.Orders.Order.Flow(Amplitude.Unity.Game.Orders.Order.Control.SetByServer)]
	public GameEntityGUID BoosterGameEntityGUID { get; set; }

	[Amplitude.Unity.Game.Orders.Order.Flow(Amplitude.Unity.Game.Orders.Order.Control.SetByServer)]
	public ConstructionResourceStock[] ConstructionResourceStocks { get; set; }

	[Amplitude.Unity.Game.Orders.Order.Flow(Amplitude.Unity.Game.Orders.Order.Control.SetByClient)]
	public bool IsFree { get; set; }

	public int Duration { get; set; }

	public int InstigatorEmpireIndex { get; set; }

	public override StaticString Path
	{
		get
		{
			return OrderBuyoutAndActivateBooster.AuthenticationPath;
		}
	}

	public override void Pack(BinaryWriter writer)
	{
		base.Pack(writer);
		writer.Write(this.BoosterDefinitionName);
		writer.Write(this.BoosterGameEntityGUID);
		writer.Write(this.TargetGUID);
		writer.Write(this.IsFree);
		writer.Write(this.Duration);
		writer.Write(this.InstigatorEmpireIndex);
		if (this.ConstructionResourceStocks == null)
		{
			writer.Write(0);
		}
		else
		{
			writer.Write(this.ConstructionResourceStocks.Length);
			for (int i = 0; i < this.ConstructionResourceStocks.Length; i++)
			{
				if (this.ConstructionResourceStocks[i] == null)
				{
					writer.Write(0);
				}
				else
				{
					writer.Write(this.ConstructionResourceStocks[i].Stock);
					writer.Write(this.ConstructionResourceStocks[i].PropertyName);
				}
			}
		}
	}

	public override void Unpack(BinaryReader reader)
	{
		base.Unpack(reader);
		this.BoosterDefinitionName = reader.ReadString();
		this.BoosterGameEntityGUID = reader.ReadUInt64();
		this.TargetGUID = reader.ReadUInt64();
		this.IsFree = reader.ReadBoolean();
		this.Duration = reader.ReadInt32();
		this.InstigatorEmpireIndex = reader.ReadInt32();
		int num = reader.ReadInt32();
		if (num > 0)
		{
			this.ConstructionResourceStocks = new ConstructionResourceStock[num];
			for (int i = 0; i < this.ConstructionResourceStocks.Length; i++)
			{
				float num2 = reader.ReadSingle();
				if (num2 > 0f)
				{
					string x = reader.ReadString();
					this.ConstructionResourceStocks[i] = new ConstructionResourceStock(x, null)
					{
						Stock = num2
					};
				}
			}
		}
		else
		{
			this.ConstructionResourceStocks = null;
		}
	}

	[Amplitude.Unity.Game.Orders.Order.Flow(Amplitude.Unity.Game.Orders.Order.Control.SetByClient)]
	public bool IgnoreCost { get; set; }

	public static StaticString AuthenticationPath = "DepartmentOfPlanificationAndDevelopment/OrderBuyoutAndActivateBooster";
}
