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
				if (pointOfInterest.Type == "QuestLocation")
				{
					bool flag2 = pointOfInterest.SimulationObject.Tags.Contains("QuestLocationTypeRuin");
					if (flag2)
					{
						num++;
					}
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
			if (flag)
			{
				Diagnostics.Log(string.Concat(new object[]
				{
					"[EGO] ",
					regionToCheck.LocalizedName,
					" has ",
					num,
					" ruins."
				}));
			}
		}
		return flag;
	}
}
