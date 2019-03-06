using System;
using System.IO;
using Amplitude;
using Amplitude.Unity.Game.Orders;

public class OrderUpdateCadastralMapClient : global::Order
{
	public OrderUpdateCadastralMapClient(int empireIndex) : base(empireIndex)
	{
		this.CityGameEntityGUID = GameEntityGUID.Zero;
		this.PathfindingMovementCapacity = PathfindingMovementCapacity.None;
		this.Operation = CadastralMapOperation.Proxy;
	}

	public OrderUpdateCadastralMapClient(int empireIndex, City city, PathfindingMovementCapacity pathfindingMovementCapacity, CadastralMapOperation operation) : base(empireIndex)
	{
		if (city == null)
		{
			throw new ArgumentNullException("city");
		}
		this.CityGameEntityGUID = city.GUID;
		this.PathfindingMovementCapacity = pathfindingMovementCapacity;
		this.Operation = operation;
	}

	[Amplitude.Unity.Game.Orders.Order.Flow(Amplitude.Unity.Game.Orders.Order.Control.SetByServer)]
	public GameEntityGUID CityGameEntityGUID { get; set; }

	[Amplitude.Unity.Game.Orders.Order.Flow(Amplitude.Unity.Game.Orders.Order.Control.SetByServer)]
	public CadastralMapOperation Operation { get; set; }

	[Amplitude.Unity.Game.Orders.Order.Flow(Amplitude.Unity.Game.Orders.Order.Control.SetByServer)]
	public ushort[] Indices { get; set; }

	public override StaticString Path
	{
		get
		{
			return OrderUpdateCadastralMapClient.AuthenticationPath;
		}
	}

	[Amplitude.Unity.Game.Orders.Order.Flow(Amplitude.Unity.Game.Orders.Order.Control.SetByServer)]
	public PathfindingMovementCapacity PathfindingMovementCapacity { get; set; }

	[Amplitude.Unity.Game.Orders.Order.Flow(Amplitude.Unity.Game.Orders.Order.Control.SetByServer)]
	public Road[] Roads { get; set; }

	[Amplitude.Unity.Game.Orders.Order.Flow(Amplitude.Unity.Game.Orders.Order.Control.SetByServer)]
	public WorldPosition WorldPosition { get; set; }

	public override void Pack(BinaryWriter writer)
	{
		base.Pack(writer);
		writer.Write(this.CityGameEntityGUID);
		writer.Write((int)this.PathfindingMovementCapacity);
		writer.Write((byte)this.Operation);
		writer.Write(this.WorldPosition.Row);
		writer.Write(this.WorldPosition.Column);
		if (this.Indices != null)
		{
			Diagnostics.Assert(this.Indices.Length < 256);
			writer.Write((byte)this.Indices.Length);
			for (int i = 0; i < this.Indices.Length; i++)
			{
				writer.Write(this.Indices[i]);
			}
		}
		else
		{
			writer.Write(0);
		}
		if (this.Roads != null)
		{
			Diagnostics.Assert(this.Roads.Length < 256);
			Diagnostics.Assert(this.Roads.Length == this.Indices.Length);
			writer.Write((byte)this.Roads.Length);
			for (int j = 0; j < this.Roads.Length; j++)
			{
				writer.Write(this.Roads[j].FromRegion);
				writer.Write(this.Roads[j].ToRegion);
				writer.Write((ushort)this.Roads[j].WorldPositions.Length);
				for (int k = 0; k < this.Roads[j].WorldPositions.Length; k++)
				{
					writer.Write(this.Roads[j].WorldPositions[k].Row);
					writer.Write(this.Roads[j].WorldPositions[k].Column);
				}
			}
			return;
		}
		writer.Write(0);
	}

	public override string ToString()
	{
		return base.ToString() + string.Format(", empire: {0}, city guid: {1:X8}, movement capacity: {3}", base.EmpireIndex, this.CityGameEntityGUID, this.PathfindingMovementCapacity);
	}

	public override void Unpack(BinaryReader reader)
	{
		base.Unpack(reader);
		this.CityGameEntityGUID = reader.ReadUInt64();
		this.PathfindingMovementCapacity = (PathfindingMovementCapacity)reader.ReadInt32();
		this.Operation = (CadastralMapOperation)reader.ReadByte();
		short row = reader.ReadInt16();
		short column = reader.ReadInt16();
		this.WorldPosition = new WorldPosition(row, column);
		byte b = reader.ReadByte();
		if (b > 0)
		{
			this.Indices = new ushort[(int)b];
			for (int i = 0; i < (int)b; i++)
			{
				this.Indices[i] = reader.ReadUInt16();
			}
		}
		byte b2 = reader.ReadByte();
		if (b2 > 0)
		{
			this.Roads = new Road[(int)b2];
			for (int j = 0; j < (int)b2; j++)
			{
				Road road = new Road();
				road.FromRegion = reader.ReadInt16();
				road.ToRegion = reader.ReadInt16();
				road.PathfindingMovementCapacity = this.PathfindingMovementCapacity;
				ushort num = reader.ReadUInt16();
				road.WorldPositions = new WorldPosition[(int)num];
				for (int k = 0; k < (int)num; k++)
				{
					row = reader.ReadInt16();
					column = reader.ReadInt16();
					road.WorldPositions[k] = new WorldPosition(row, column);
				}
				this.Roads[j] = road;
			}
		}
	}

	public static StaticString AuthenticationPath = "GameServer/OrderUpdateCadastralMapClient";
}
