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
	}
}
