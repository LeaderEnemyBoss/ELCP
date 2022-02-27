using System;
using System.IO;
using Amplitude;
using Amplitude.Unity.Game.Orders;

public class OrderChangeHeroAssignment : global::Order
{
	public OrderChangeHeroAssignment(int empireIndex, GameEntityGUID heroGuid, GameEntityGUID assignmentGuid) : base(empireIndex)
	{
		this.HeroGuid = heroGuid;
		this.AssignmentGUID = assignmentGuid;
	}

	[Amplitude.Unity.Game.Orders.Order.Flow(Amplitude.Unity.Game.Orders.Order.Control.SetByClient)]
	public GameEntityGUID HeroGuid { get; set; }

	[Amplitude.Unity.Game.Orders.Order.Flow(Amplitude.Unity.Game.Orders.Order.Control.SetByClient)]
	public GameEntityGUID AssignmentGUID { get; set; }

	public override StaticString Path
	{
		get
		{
			return OrderChangeHeroAssignment.AuthenticationPath;
		}
	}

	public override void Pack(BinaryWriter writer)
	{
		base.Pack(writer);
		writer.Write(this.HeroGuid);
		writer.Write(this.AssignmentGUID);
	}

	public override void Unpack(BinaryReader reader)
	{
		base.Unpack(reader);
		this.HeroGuid = reader.ReadUInt64();
		this.AssignmentGUID = reader.ReadUInt64();
	}

	public static StaticString AuthenticationPath = "DepartmentOfEducation/OrderChangeHeroAssignment";
}
