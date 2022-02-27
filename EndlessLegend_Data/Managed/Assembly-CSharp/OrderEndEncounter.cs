using System;
using System.IO;
using Amplitude;
using Amplitude.Unity.Game.Orders;

public class OrderEndEncounter : Amplitude.Unity.Game.Orders.Order
{
	public OrderEndEncounter(GameEntityGUID guid, bool canceled)
	{
		this.EncounterGUID = guid;
		this.HasBeenCanceled = canceled;
	}

	public bool HasBeenCanceled { get; private set; }

	[Amplitude.Unity.Game.Orders.Order.Flow(Amplitude.Unity.Game.Orders.Order.Control.SetByServer)]
	public GameEntityGUID EncounterGUID { get; set; }

	[Amplitude.Unity.Game.Orders.Order.Flow(Amplitude.Unity.Game.Orders.Order.Control.SetByServer)]
	public bool DoNotSubtractActionPoints { get; set; }

	public override StaticString Path
	{
		get
		{
			return OrderEndEncounter.AuthenticationPath;
		}
	}

	public override void Pack(BinaryWriter writer)
	{
		base.Pack(writer);
		writer.Write(this.EncounterGUID);
		writer.Write(this.DoNotSubtractActionPoints);
		if (this.GUIDsOnBattlefield == null)
		{
			writer.Write(0);
			return;
		}
		writer.Write(this.GUIDsOnBattlefield.Length);
		for (int i = 0; i < this.GUIDsOnBattlefield.Length; i++)
		{
			ulong num = this.GUIDsOnBattlefield[i];
			writer.Write(this.GUIDsOnBattlefield[i]);
		}
	}

	public override void Unpack(BinaryReader reader)
	{
		base.Unpack(reader);
		this.EncounterGUID = reader.ReadUInt64();
		this.DoNotSubtractActionPoints = reader.ReadBoolean();
		int num = reader.ReadInt32();
		if (num > 0)
		{
			this.GUIDsOnBattlefield = new ulong[num];
			for (int i = 0; i < this.GUIDsOnBattlefield.Length; i++)
			{
				this.GUIDsOnBattlefield[i] = reader.ReadUInt64();
			}
			return;
		}
		this.GUIDsOnBattlefield = null;
	}

	public override string ToString()
	{
		return base.ToString() + string.Format(" EncounterID={0:X8}", this.EncounterGUID);
	}

	[Amplitude.Unity.Game.Orders.Order.Flow(Amplitude.Unity.Game.Orders.Order.Control.SetByServer)]
	public ulong[] GUIDsOnBattlefield { get; set; }

	public static StaticString AuthenticationPath = "GameServer/OrderEndEncounter";
}
