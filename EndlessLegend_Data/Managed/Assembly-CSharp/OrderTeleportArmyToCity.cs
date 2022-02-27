using System;
using System.IO;
using Amplitude;

public class OrderTeleportArmyToCity : Order
{
	public OrderTeleportArmyToCity(int empireIndex, GameEntityGUID armyGUID, GameEntityGUID cityGUID) : base(empireIndex)
	{
		this.ArmyGUID = armyGUID;
		this.CityGUID = cityGUID;
	}

	public GameEntityGUID ArmyGUID { get; set; }

	public GameEntityGUID CityGUID { get; set; }

	public override StaticString Path
	{
		get
		{
			return OrderTeleportArmyToCity.AuthenticationPath;
		}
	}

	public WorldPosition Destination { get; set; }

	public override void Pack(BinaryWriter writer)
	{
		base.Pack(writer);
		writer.Write(this.ArmyGUID);
		writer.Write(this.CityGUID);
		writer.Write(this.Destination.Row);
		writer.Write(this.Destination.Column);
	}

	public override string ToString()
	{
		return base.ToString() + string.Format(", empire: {0}, guid: {1:X8}, destination: {2}", base.EmpireIndex, this.ArmyGUID, this.Destination);
	}

	public override void Unpack(BinaryReader reader)
	{
		base.Unpack(reader);
		this.ArmyGUID = reader.ReadUInt64();
		this.CityGUID = reader.ReadUInt64();
		short row = reader.ReadInt16();
		short column = reader.ReadInt16();
		this.Destination = new WorldPosition(row, column);
	}

	public static StaticString AuthenticationPath = "DepartmentOfTransportation/OrderTeleportArmyToCity";
}
