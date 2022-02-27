using System;
using System.IO;
using Amplitude;
using Amplitude.Unity.Game.Orders;

public class OrderIntegrateFaction : global::Order
{
	public OrderIntegrateFaction(int empireIndex, int integratingEmpireIndex) : base(empireIndex)
	{
		this.IntegratingEmpireIndex = integratingEmpireIndex;
		this.IntegrationDescriptors = new string[0];
	}

	[Amplitude.Unity.Game.Orders.Order.Flow(Amplitude.Unity.Game.Orders.Order.Control.SetByServerPreprocessor)]
	public string[] IntegrationDescriptors { get; set; }

	[Amplitude.Unity.Game.Orders.Order.Flow(Amplitude.Unity.Game.Orders.Order.Control.SetByClient)]
	public int IntegratingEmpireIndex { get; set; }

	public override StaticString Path
	{
		get
		{
			return OrderIntegrateFaction.AuthenticationPath;
		}
	}

	public override void Pack(BinaryWriter writer)
	{
		base.Pack(writer);
		writer.Write((short)this.IntegrationDescriptors.Length);
		for (int i = 0; i < this.IntegrationDescriptors.Length; i++)
		{
			writer.Write(this.IntegrationDescriptors[i]);
		}
		writer.Write((short)this.IntegratingEmpireIndex);
	}

	public override void Unpack(BinaryReader reader)
	{
		base.Unpack(reader);
		this.IntegrationDescriptors = new string[(int)reader.ReadInt16()];
		for (int i = 0; i < this.IntegrationDescriptors.Length; i++)
		{
			this.IntegrationDescriptors[i] = reader.ReadString();
		}
		this.IntegratingEmpireIndex = (int)reader.ReadInt16();
	}

	public static StaticString AuthenticationPath = "DepartmentOfPlanificationAndDevelopment/OrderIntegrateFaction";
}
