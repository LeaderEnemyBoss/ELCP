﻿using System;
using System.IO;
using Amplitude;
using Amplitude.Unity.Game.Orders;

public class OrderEditHeroUnitDesign : OrderEditUnitDesign
{
	public OrderEditHeroUnitDesign(int empireIndex, UnitDesign unitDesign) : base(empireIndex, unitDesign)
	{
	}

	[Amplitude.Unity.Game.Orders.Order.Flow(Amplitude.Unity.Game.Orders.Order.Control.SetByServerPreprocessor)]
	public IConstructionCost[] RetrofitCosts { get; set; }

	public override StaticString Path
	{
		get
		{
			return OrderEditHeroUnitDesign.AuthenticationPath;
		}
	}

	public override void Pack(BinaryWriter writer)
	{
		base.Pack(writer);
		if (this.RetrofitCosts == null)
		{
			writer.Write(0);
		}
		else
		{
			writer.Write(this.RetrofitCosts.Length);
			for (int i = 0; i < this.RetrofitCosts.Length; i++)
			{
				IConstructionCost constructionCost = this.RetrofitCosts[i];
				writer.Write(constructionCost.GetType().FullName);
				constructionCost.Serialize(writer);
			}
		}
	}

	public override void Unpack(BinaryReader reader)
	{
		base.Unpack(reader);
		int num = reader.ReadInt32();
		this.RetrofitCosts = new IConstructionCost[num];
		for (int i = 0; i < this.RetrofitCosts.Length; i++)
		{
			string typeName = reader.ReadString();
			Type type = Type.GetType(typeName);
			this.RetrofitCosts[i] = (Activator.CreateInstance(type) as IConstructionCost);
			Diagnostics.Assert(this.RetrofitCosts[i] != null);
			this.RetrofitCosts[i].Deserialize(reader);
		}
	}

	public new static StaticString AuthenticationPath = "DepartmentOfDefense/OrderEditHeroUnitDesign";
}