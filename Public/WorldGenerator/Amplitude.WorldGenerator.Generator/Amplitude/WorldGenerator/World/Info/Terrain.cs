using System;
using System.Xml.Serialization;

namespace Amplitude.WorldGenerator.World.Info
{
	[XmlType("Terrain")]
	public class Terrain
	{
		public Terrain()
		{
			this.Id = byte.MaxValue;
		}

		[XmlAttribute("Name")]
		public string Name { get; set; }

		[XmlIgnore]
		public byte Id { get; set; }

		public bool IsWaterTile
		{
			get
			{
				return this.Name.Contains("Ocean") || this.Name.Contains("Water") || this.Name.Contains("DriftIce") || this.Name.Contains("CoralReef");
			}
		}
	}
}
