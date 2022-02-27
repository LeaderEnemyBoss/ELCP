using System;
using System.Xml.Serialization;
using Amplitude;

public class QuestFilterRegionIsNamed
{
	[XmlAttribute("Inverted")]
	public bool Inverted { get; set; }

	[XmlAttribute("Name")]
	public string Name { get; set; }

	public bool Check(Region regionToCheck)
	{
		Diagnostics.Assert(regionToCheck != null);
		bool flag = this.Name.ToLower().Contains(regionToCheck.LocalizedName.ToLower());
		return (!this.Inverted) ? flag : (!flag);
	}
}
