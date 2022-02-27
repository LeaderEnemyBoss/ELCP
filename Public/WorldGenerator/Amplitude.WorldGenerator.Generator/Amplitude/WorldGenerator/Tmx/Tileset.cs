using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Amplitude.WorldGenerator.Tmx
{
	[XmlRoot(ElementName = "tileset")]
	public class Tileset : IXmlSerializable
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

		public XmlSchema GetSchema()
		{
			return null;
		}

		public void ReadXml(XmlReader reader)
		{
			string attribute = reader.GetAttribute("firstgid");
			if (!string.IsNullOrEmpty(attribute))
			{
				this.FirstGID = int.Parse(attribute);
			}
			string attribute2 = reader.GetAttribute("source");
			if (string.IsNullOrEmpty(attribute2))
			{
				this.ReadXml_fromTmx(reader);
				reader.ReadEndElement();
				return;
			}
			this.ReadXml_fromTsx(reader, attribute2);
			reader.Read();
		}

		public void WriteXml(XmlWriter writer)
		{
			throw new NotImplementedException();
		}

		private void ReadXml_fromTmx(XmlReader reader)
		{
			this.Name = reader.GetAttribute("name");
			reader.Read();
			while (reader.Name != "tile")
			{
				reader.Read();
			}
			List<Tile> list = new List<Tile>();
			while (reader.Name == "tile")
			{
				Tile tile = new Tile();
				tile.ReadXml(reader);
				list.Add(tile);
			}
			this.Tiles = list.ToArray();
		}

		private void ReadXml_fromTsx(XmlReader reader, string source)
		{
			string path = Tileset.TmxPath + "\\" + source;
			if (!File.Exists(path))
			{
				throw new TmxImportException();
			}
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(Tileset));
			Tileset tileset;
			using (Stream stream = File.OpenRead(path))
			{
				tileset = (xmlSerializer.Deserialize(stream) as Tileset);
			}
			if (tileset != null)
			{
				this.Name = tileset.Name;
				this.Tiles = tileset.Tiles;
			}
		}

		public static string TmxPath;
	}
}
