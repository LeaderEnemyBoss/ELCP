using System;
using System.Xml.Serialization;
using Amplitude;

public class GameOptionDefinition : OptionDefinition
{
	[XmlIgnore]
	public override string RegistryPath
	{
		get
		{
			return GameOptionDefinition.RegistryPathPrefix + "/" + base.RegistryPath;
		}
	}

	public static StaticString Advanced = new StaticString("Advanced");

	private static readonly StaticString RegistryPathPrefix = "Settings/Game";
}
