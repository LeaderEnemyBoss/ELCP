using System;
using System.Text;
using Amplitude;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class QuestOccurence : IXmlSerializable
{
	public QuestOccurence()
	{
		this.LastCompletedOnTurn = -1;
		this.LastStartedOnTurn = -1;
		this.NumberOfOccurencesThisGame = 0;
		this.NumberOfOccurrencesForThisEmpireSoFar = new int[ELCPUtilities.NumberOfMajorEmpires];
	}

	public virtual void ReadXml(XmlReader reader)
	{
		this.LastCompletedOnTurn = reader.GetAttribute<int>("LastCompletedOnTurn");
		this.LastStartedOnTurn = reader.GetAttribute<int>("LastStartedOnTurn");
		this.NumberOfOccurencesThisGame = reader.GetAttribute<int>("NumberOfOccurencesThisGame");
		string attribute = reader.GetAttribute("NumberOfOccurrencesPerEmpireSoFar");
		if (string.IsNullOrEmpty(attribute))
		{
			this.NumberOfOccurrencesForThisEmpireSoFar = new int[ELCPUtilities.NumberOfMajorEmpires];
		}
		else
		{
			string[] array = attribute.Split(Amplitude.String.Separators);
			for (int i = 0; i < this.NumberOfOccurrencesForThisEmpireSoFar.Length; i++)
			{
				try
				{
					this.NumberOfOccurrencesForThisEmpireSoFar[i] = int.Parse(array[i]);
				}
				catch
				{
					this.NumberOfOccurrencesForThisEmpireSoFar[i] = 0;
				}
			}
		}
		reader.ReadStartElement();
	}

	public virtual void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<int>("LastCompletedOnTurn", this.LastCompletedOnTurn);
		writer.WriteAttributeString<int>("LastStartedOnTurn", this.LastStartedOnTurn);
		writer.WriteAttributeString<int>("NumberOfOccurencesThisGame", this.NumberOfOccurencesThisGame);
		StringBuilder stringBuilder = new StringBuilder();
		if (this.NumberOfOccurrencesForThisEmpireSoFar != null)
		{
			for (int i = 0; i < this.NumberOfOccurrencesForThisEmpireSoFar.Length; i++)
			{
				if (i > 0)
				{
					stringBuilder.Append(";");
				}
				stringBuilder.Append(this.NumberOfOccurrencesForThisEmpireSoFar[i].ToString());
			}
		}
		writer.WriteAttributeString<StringBuilder>("NumberOfOccurrencesPerEmpireSoFar", stringBuilder);
	}

	public int LastCompletedOnTurn { get; set; }

	public int LastStartedOnTurn { get; set; }

	public int NumberOfOccurencesThisGame { get; set; }

	public int[] NumberOfOccurrencesForThisEmpireSoFar { get; set; }
}
