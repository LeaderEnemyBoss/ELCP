using System;
using System.Xml.Serialization;
using Amplitude;

public class GameOptionDefinition : OptionDefinition
{
	public GameOptionDefinition()
	{
		this.SaveAsGlobalTag = false;
	}

	[XmlIgnore]
	public override string RegistryPath
	{
		get
		{
			return GameOptionDefinition.RegistryPathPrefix + "/" + base.RegistryPath;
		}
	}

	[XmlAttribute]
	public bool SaveAsGlobalTag { get; set; }

	public static StaticString Advanced = new StaticString("Advanced");

	private static readonly StaticString RegistryPathPrefix = "Settings/Game";
}
