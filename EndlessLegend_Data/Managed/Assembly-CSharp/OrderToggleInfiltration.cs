using System;
using System.IO;
using Amplitude;
using Amplitude.Unity.Game.Orders;

public class OrderToggleInfiltration : global::Order
{
	public OrderToggleInfiltration(int empireIndex, GameEntityGUID heroGuid, GameEntityGUID assignmentGuid, bool isAGroundInfiltration, bool isStarting) : base(empireIndex)
	{
		this.HeroGuid = heroGuid;
		this.AssignmentGUID = assignmentGuid;
		this.IsAGroundInfiltration = isAGroundInfiltration;
		this.IsStarting = isStarting;
	}

	[Amplitude.Unity.Game.Orders.Order.Flow(Amplitude.Unity.Game.Orders.Order.Control.SetByClient)]
	public GameEntityGUID HeroGuid { get; set; }

	[Amplitude.Unity.Game.Orders.Order.Flow(Amplitude.Unity.Game.Orders.Order.Control.SetByClient)]
	public GameEntityGUID AssignmentGUID { get; set; }

	public float InfiltrationCost { get; set; }

	public bool IsAGroundInfiltration { get; set; }

	public bool IsStarting { get; set; }

	public override StaticString Path
	{
		get
		{
			return OrderToggleInfiltration.AuthenticationPath;
		}
	}

	public override void Pack(BinaryWriter writer)
	{
		base.Pack(writer);
		writer.Write(this.HeroGuid);
		writer.Write(this.AssignmentGUID);
		writer.Write(this.IsAGroundInfiltration);
		writer.Write(this.InfiltrationCost);
		writer.Write(this.IsStarting);
	}

	public override void Unpack(BinaryReader reader)
	{
		base.Unpack(reader);
		this.HeroGuid = reader.ReadUInt64();
		this.AssignmentGUID = reader.ReadUInt64();
		this.IsAGroundInfiltration = reader.ReadBoolean();
		this.InfiltrationCost = reader.ReadSingle();
		this.IsStarting = reader.ReadBoolean();
	}

	public static StaticString AuthenticationPath = "DepartmentOfIntelligence/OrderToggleInfiltration";
}
