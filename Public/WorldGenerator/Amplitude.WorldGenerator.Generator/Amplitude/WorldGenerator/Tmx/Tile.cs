using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Amplitude.WorldGenerator.Tmx
{
	public class Tile : IXmlSerializable
	{
		public Tile()
		{
			this.Id = -1;
			this.Properties = null;
		}

		[XmlAttribute("id")]
		public int Id { get; set; }

		[XmlArray("properties")]
		[XmlArrayItem("property")]
		public Dictionary<string, string> Properties { get; set; }

		public XmlSchema GetSchema()
		{
			return null;
		}

		public void ReadXml(XmlReader reader)
		{
			this.Id = int.Parse(reader.GetAttribute("id"));
			this.Properties = new Dictionary<string, string>();
			reader.ReadStartElement("tile");
			reader.ReadStartElement("properties");
			while (reader.Name == "property")
			{
				this.Properties.Add(reader.GetAttribute("name"), reader.GetAttribute("value"));
				reader.Read();
			}
			reader.ReadEndElement();
			reader.ReadEndElement();
		}

		public void WriteXml(XmlWriter writer)
		{
			throw new NotImplementedException();
		}

		public class Property
		{
			[XmlAttribute("name")]
			public string Name { get; set; }

			[XmlAttribute("value")]
			public string Value { get; set; }
		}
	}
}
