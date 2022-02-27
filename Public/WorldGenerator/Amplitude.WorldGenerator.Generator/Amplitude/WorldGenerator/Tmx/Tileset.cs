using System;
using System.Xml.Serialization;

namespace Amplitude.WorldGenerator.Tmx
{
	public class Tileset
	{
		public Tileset()
		{
			this.FirstGID = -1;
			this.Name = string.Empty;
			this.Tiles = null;
		}

		[XmlAttribute("firstgid")]
		public int FirstGID { get; set; }

		[XmlAttribute("name")]
		public string Name { get; set; }

		[XmlElement("tile")]
		public Tile[] Tiles { get; set; }
	}
}
