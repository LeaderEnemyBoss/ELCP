using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Amplitude.WorldGenerator.Tmx
{
	public class Layer : IXmlSerializable
	{
		public Layer()
		{
			this.Name = string.Empty;
			this.Data = null;
		}

		public string Name { get; set; }

		public int[,] Data { get; set; }

		public XmlSchema GetSchema()
		{
			return null;
		}

		public void ReadXml(XmlReader reader)
		{
			this.Name = reader.GetAttribute("name");
			int num = int.Parse(reader.GetAttribute("width"));
			int num2 = int.Parse(reader.GetAttribute("height"));
			this.Data = new int[num2, num];
			reader.ReadStartElement("layer");
			reader.ReadStartElement("data");
			int num3 = num2 - 1;
			int num4 = 0;
			while (reader.Name == "tile" && num3 >= 0)
			{
				if (reader.GetAttribute("gid") != null)
				{
					this.Data[num3, num4] = int.Parse(reader.GetAttribute("gid"));
				}
				else
				{
					this.Data[num3, num4] = 0;
				}
				num4++;
				if (num4 >= num)
				{
					num3--;
					num4 = 0;
				}
				reader.Read();
			}
			reader.ReadEndElement();
			reader.ReadEndElement();
		}

		public void WriteXml(XmlWriter writer)
		{
			throw new NotImplementedException();
		}
	}
}
