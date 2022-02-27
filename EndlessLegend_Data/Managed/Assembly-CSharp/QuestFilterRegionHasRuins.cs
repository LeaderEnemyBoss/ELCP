using System;
using System.Xml.Serialization;
using Amplitude;

public class QuestFilterRegionHasRuins
{
	[XmlAttribute]
	public int Count { get; set; }

	[XmlAttribute]
	public bool OrMore { get; set; }

	public bool Check(Region regionToCheck)
	{
		Diagnostics.Assert(regionToCheck != null);
		bool flag = regionToCheck.IsLand;
		if (flag)
		{
			int num = 0;
			foreach (PointOfInterest pointOfInterest in regionToCheck.PointOfInterests)
			{
				if (pointOfInterest.Type == "QuestLocation" && pointOfInterest.SimulationObject.Tags.Contains("QuestLocationTypeRuin"))
				{
					num++;
				}
			}
			if (!this.OrMore)
			{
				flag &= (num == this.Count);
			}
			else
			{
				flag &= (num >= this.Count);
			}
		}
		return flag;
	}
}
