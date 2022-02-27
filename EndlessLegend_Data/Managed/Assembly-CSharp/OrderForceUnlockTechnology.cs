using System;
using System.IO;
using Amplitude;

public class OrderForceUnlockTechnology : Order
{
	public OrderForceUnlockTechnology(int empireIndex, string technologyDefinitionName) : base(empireIndex)
	{
		if (string.IsNullOrEmpty(technologyDefinitionName))
		{
			throw new ArgumentNullException("technologyDefinitionName");
		}
		this.TechnologyDefinitionNames = new StaticString[]
		{
			technologyDefinitionName
		};
	}

	public OrderForceUnlockTechnology(int empireIndex, StaticString[] technologyDefinitionNames) : base(empireIndex)
	{
		this.TechnologyDefinitionNames = technologyDefinitionNames;
	}

	public StaticString[] TechnologyDefinitionNames { get; private set; }

	public override StaticString Path
	{
		get
		{
			return OrderForceUnlockTechnology.AuthenticationPath;
		}
	}

	public override void Pack(BinaryWriter writer)
	{
		if (writer == null)
		{
			throw new ArgumentNullException("writer");
		}
		base.Pack(writer);
		writer.Write(this.TechnologyDefinitionNames.Length);
		for (int i = 0; i < this.TechnologyDefinitionNames.Length; i++)
		{
			writer.Write(this.TechnologyDefinitionNames[i]);
		}
	}

	public override void Unpack(BinaryReader reader)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		base.Unpack(reader);
		int num = reader.ReadInt32();
		this.TechnologyDefinitionNames = new StaticString[num];
		for (int i = 0; i < num; i++)
		{
			this.TechnologyDefinitionNames[i] = reader.ReadString();
		}
	}

	public static readonly StaticString AuthenticationPath = "DepartmentOfScience/OrderForceUnlockTechnology";
}
