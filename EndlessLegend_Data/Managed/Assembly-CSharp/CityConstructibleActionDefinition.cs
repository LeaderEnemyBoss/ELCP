using System;
using System.Xml.Serialization;
using Amplitude.Unity.Xml;

public class CityConstructibleActionDefinition : DepartmentOfIndustry.ConstructibleElement
{
	public CityConstructibleActionDefinition()
	{
		this.Category = CityConstructibleActionDefinition.ReadOnlyCategory;
	}

	[XmlElement("Action")]
	public XmlNamedReference Action { get; set; }

	[XmlElement("InfectedAffinityConstraint")]
	public string InfectedAffinityConstraint { get; set; }

	public static readonly string ReadOnlyCategory = "CityConstructibleAction";
}
