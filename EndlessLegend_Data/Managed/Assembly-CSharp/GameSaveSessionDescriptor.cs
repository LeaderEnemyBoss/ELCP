using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Interop;
using Amplitude.Unity.Session;

public class GameSaveSessionDescriptor : IXmlSerializable
{
	XmlSchema IXmlSerializable.GetSchema()
	{
		return null;
	}

	void IXmlSerializable.ReadXml(XmlReader reader)
	{
		this.lobbyData.Clear();
		int num = 1;
		string attribute = reader.GetAttribute("Version");
		if (!string.IsNullOrEmpty(attribute))
		{
			int.TryParse(attribute, out num);
		}
		reader.ReadStartElement();
		string value = reader.ReadElementString("SessionMode");
		this.SessionMode = (SessionMode)((int)Enum.Parse(typeof(SessionMode), value));
		reader.ReadStartElement("LobbyData");
		while (reader.IsStartElement("LobbyData"))
		{
			string attribute2 = reader.GetAttribute("Key");
			reader.ReadStartElement("LobbyData");
			string value2 = reader.ReadString();
			this.lobbyData.Add(attribute2, value2);
			reader.ReadEndElement();
		}
		reader.ReadEndElement();
		if (num >= 2)
		{
			int num2 = 0;
			string attribute3 = reader.GetAttribute("Count");
			int.TryParse(attribute3, out num2);
			this.presence = null;
			if (num2 > 0)
			{
				this.presence = new List<GameSaveSessionDescriptor.UserPresenceInfo>[num2];
				for (int i = 0; i < num2; i++)
				{
					this.presence[i] = new List<GameSaveSessionDescriptor.UserPresenceInfo>();
				}
			}
			reader.ReadStartElement("UserPresence");
			for (int j = 0; j < this.presence.Length; j++)
			{
				attribute3 = reader.GetAttribute("Count");
				reader.ReadStartElement("UserPresenceInfo");
				int num3 = 0;
				int.TryParse(attribute3, out num3);
				for (int k = 0; k < num3; k++)
				{
					string attribute4 = reader.GetAttribute("Turn");
					string attribute5 = reader.GetAttribute("SteamUserID");
					GameSaveSessionDescriptor.UserPresenceInfo userPresenceInfo = default(GameSaveSessionDescriptor.UserPresenceInfo);
					GameSaveSessionDescriptor.UserPresenceInfo userPresenceInfo2 = userPresenceInfo;
					userPresenceInfo2.SteamUserID = attribute5;
					userPresenceInfo = userPresenceInfo2;
					int.TryParse(attribute4, out userPresenceInfo.Turn);
					reader.ReadStartElement("Entry");
					if (reader.NodeType == XmlNodeType.CDATA)
					{
						userPresenceInfo.UserName = reader.Value;
						reader.Skip();
					}
					if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "Entry")
					{
						reader.ReadEndElement();
					}
					this.presence[j].Add(userPresenceInfo);
				}
				if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "UserPresenceInfo")
				{
					reader.ReadEndElement();
				}
			}
			if (reader.NodeType == XmlNodeType.EndElement && reader.Name == "UserPresence")
			{
				reader.ReadEndElement();
			}
		}
		reader.ReadEndElement();
		if (!this.lobbyData.ContainsKey("HeatWaveIntensity"))
		{
			this.lobbyData.Add("HeatWaveIntensity", "Moderate");
		}
		if (!this.lobbyData.ContainsKey("HeatWaveDuration"))
		{
			this.lobbyData.Add("HeatWaveDuration", "Moderate");
		}
		if (!this.lobbyData.ContainsKey("MadSeasonType"))
		{
			this.lobbyData.Add("MadSeasonType", "HighChanceShortDuration");
		}
		if (!this.lobbyData.ContainsKey("PlayWithMadSeason"))
		{
			this.lobbyData.Add("PlayWithMadSeason", "true");
		}
		if (!this.lobbyData.ContainsKey("PlayWithKaiju"))
		{
			this.lobbyData.Add("PlayWithKaiju", "true");
		}
	}

	void IXmlSerializable.WriteXml(XmlWriter writer)
	{
		int num = 2;
		writer.WriteAttributeString("Version", num.ToString());
		writer.WriteElementString("SessionMode", this.SessionMode.ToString());
		writer.WriteStartElement("LobbyData");
		writer.WriteAttributeString("Count", this.lobbyData.Count.ToString());
		foreach (KeyValuePair<string, string> keyValuePair in this.lobbyData)
		{
			if (!keyValuePair.Key.StartsWith("_"))
			{
				writer.WriteStartElement("LobbyData");
				writer.WriteAttributeString("Key", keyValuePair.Key);
				writer.WriteString(keyValuePair.Value);
				writer.WriteEndElement();
			}
		}
		writer.WriteEndElement();
		if (num >= 2)
		{
			writer.WriteStartElement("UserPresence");
			if (this.presence != null)
			{
				writer.WriteAttributeString("Count", this.presence.Length.ToString());
				for (int i = 0; i < this.presence.Length; i++)
				{
					writer.WriteStartElement("UserPresenceInfo");
					writer.WriteAttributeString("EmpireIndex", i.ToString());
					writer.WriteAttributeString("Count", this.presence[i].Count.ToString());
					for (int j = 0; j < this.presence[i].Count; j++)
					{
						writer.WriteStartElement("Entry");
						writer.WriteAttributeString("Turn", this.presence[i][j].Turn.ToString());
						writer.WriteAttributeString("SteamUserID", this.presence[i][j].SteamUserID);
						if (!string.IsNullOrEmpty(this.presence[i][j].UserName))
						{
							writer.WriteCData(this.presence[i][j].UserName);
						}
						writer.WriteEndElement();
					}
					writer.WriteEndElement();
				}
			}
			else
			{
				writer.WriteAttributeString("Count", "0");
			}
			writer.WriteEndElement();
		}
	}

	public SessionMode SessionMode { get; set; }

	public T GetLobbyData<T>(string key, T defaultValue)
	{
		string value;
		if (this.lobbyData.TryGetValue(key, out value))
		{
			try
			{
				return (T)((object)Convert.ChangeType(value, typeof(T)));
			}
			catch
			{
				return defaultValue;
			}
			return defaultValue;
		}
		return defaultValue;
	}

	public string GetLobbyData(string key)
	{
		string result;
		if (this.lobbyData.TryGetValue(key, out result))
		{
			return result;
		}
		return null;
	}

	public IEnumerable<string> GetLobbyDataKeys()
	{
		return this.lobbyData.Keys.AsEnumerable<string>();
	}

	public void SetLobbyData(Amplitude.Unity.Session.Session session)
	{
		if (session == null)
		{
			throw new ArgumentNullException("session");
		}
		this.lobbyData.Clear();
		foreach (StaticString x in session.GetLobbyDataKeys())
		{
			string text = x;
			string value = session.GetLobbyData<string>(text, null);
			if (string.IsNullOrEmpty(value))
			{
				object obj = session.GetLobbyData(text);
				if (obj != null)
				{
					value = obj.ToString();
				}
			}
			this.lobbyData.Add(text, value);
		}
	}

	public void SetLobbyData(string key, string data)
	{
		string text;
		if (this.lobbyData.TryGetValue(key, out text))
		{
			if (text.Equals(data))
			{
				return;
			}
			if (data == null)
			{
				this.lobbyData.Remove(key);
			}
			else
			{
				this.lobbyData[key] = data;
			}
		}
		else
		{
			this.lobbyData.Add(key, data);
		}
	}

	public void TrackUserPresence(Game game, Amplitude.Unity.Session.Session session, int turn)
	{
		Diagnostics.Assert(game != null);
		Diagnostics.Assert(session != null);
		if (this.presence == null)
		{
			int num = game.Empires.Count((Empire empire) => empire is MajorEmpire);
			this.presence = new List<GameSaveSessionDescriptor.UserPresenceInfo>[num];
			for (int i = 0; i < num; i++)
			{
				this.presence[i] = new List<GameSaveSessionDescriptor.UserPresenceInfo>();
			}
		}
		for (int j = 0; j < this.presence.Length; j++)
		{
			string x = string.Format("Empire{0}", j);
			string text = session.GetLobbyData<string>(x, null);
			if (!string.IsNullOrEmpty(text))
			{
				int index = this.presence[j].Count - 1;
				if (this.presence[j].Count == 0 || this.presence[j][index].SteamUserID != text)
				{
					GameSaveSessionDescriptor.UserPresenceInfo userPresenceInfo = default(GameSaveSessionDescriptor.UserPresenceInfo);
					GameSaveSessionDescriptor.UserPresenceInfo userPresenceInfo2 = userPresenceInfo;
					userPresenceInfo2.SteamUserID = text;
					userPresenceInfo2.Turn = turn;
					userPresenceInfo = userPresenceInfo2;
					if (Steamworks.SteamAPI.IsSteamRunning && !text.StartsWith("AI"))
					{
						try
						{
							ulong value = Convert.ToUInt64(text, 16);
							userPresenceInfo.UserName = Steamworks.SteamAPI.SteamFriends.GetFriendPersonaName(new Steamworks.SteamID(value));
						}
						catch
						{
						}
					}
					this.presence[j].Add(userPresenceInfo);
				}
			}
		}
	}

	[XmlIgnore]
	private Dictionary<string, string> lobbyData = new Dictionary<string, string>();

	[XmlIgnore]
	private List<GameSaveSessionDescriptor.UserPresenceInfo>[] presence;

	private struct KeyValuePair
	{
		[XmlAttribute]
		public string Key;

		[XmlAttribute]
		public string Value;
	}

	private struct UserPresenceInfo
	{
		public int Turn;

		public string SteamUserID;

		public string UserName;
	}
}
