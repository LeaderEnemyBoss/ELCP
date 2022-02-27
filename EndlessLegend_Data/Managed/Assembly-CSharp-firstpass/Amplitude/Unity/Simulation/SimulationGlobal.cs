using System;
using System.Collections.Generic;
using Amplitude.Xml;

namespace Amplitude.Unity.Simulation
{
	public class SimulationGlobal
	{
		public static void AddGlobalTag(StaticString tag, bool needRefresh = false)
		{
			object syncObject = SimulationObject.SyncObject;
			lock (syncObject)
			{
				for (int i = 0; i < SimulationGlobal.roots.Count; i++)
				{
					SimulationGlobal.roots[i].Unsafe_RemoveGlobalTag(tag);
				}
				SimulationGlobal.globalTags.AddTag(tag);
				for (int j = 0; j < SimulationGlobal.roots.Count; j++)
				{
					SimulationGlobal.roots[j].Unsafe_AddGlobalTag(tag);
					if (needRefresh)
					{
						SimulationGlobal.roots[j].Unsafe_Refresh();
					}
				}
			}
		}

		public static void AddGlobalTags(StaticString[] tags)
		{
			for (int i = 0; i < tags.Length; i++)
			{
				SimulationGlobal.AddGlobalTag(tags[i], i == tags.Length - 1);
			}
		}

		public static void AddGlobalTags(string[] tags)
		{
			for (int i = 0; i < tags.Length; i++)
			{
				SimulationGlobal.AddGlobalTag(tags[i], i == tags.Length - 1);
			}
		}

		public static void ClearGlobalTags(bool needRefresh = false)
		{
			object syncObject = SimulationObject.SyncObject;
			lock (syncObject)
			{
				SimulationGlobal.globalTags.Clear();
				if (needRefresh)
				{
					for (int i = 0; i < SimulationGlobal.roots.Count; i++)
					{
						SimulationGlobal.roots[i].Unsafe_Refresh();
					}
				}
			}
		}

		public static bool GlobalTagsContains(StaticString tag)
		{
			object syncObject = SimulationObject.SyncObject;
			bool result;
			lock (syncObject)
			{
				result = SimulationGlobal.globalTags.Contains(tag);
			}
			return result;
		}

		public static void RemoveGlobalTag(StaticString tag, bool needRefresh = false)
		{
			object syncObject = SimulationObject.SyncObject;
			lock (syncObject)
			{
				if (SimulationGlobal.globalTags.Contains(tag))
				{
					for (int i = 0; i < SimulationGlobal.roots.Count; i++)
					{
						SimulationGlobal.roots[i].Unsafe_RemoveGlobalTag(tag);
					}
					SimulationGlobal.globalTags.RemoveTag(tag);
					for (int j = 0; j < SimulationGlobal.roots.Count; j++)
					{
						SimulationGlobal.roots[j].Unsafe_AddGlobalTag(tag);
						if (needRefresh)
						{
							SimulationGlobal.roots[j].Unsafe_Refresh();
						}
					}
				}
			}
		}

		public static void AddRoot(SimulationObject root)
		{
			object syncObject = SimulationObject.SyncObject;
			lock (syncObject)
			{
				SimulationGlobal.Unsafe_AddRoot(root);
			}
		}

		public static void RemoveGlobalTags(StaticString[] tags)
		{
			for (int i = 0; i < tags.Length; i++)
			{
				SimulationGlobal.RemoveGlobalTag(tags[i], i == tags.Length - 1);
			}
		}

		public static void RemoveGlobalTags(string[] tags)
		{
			for (int i = 0; i < tags.Length; i++)
			{
				SimulationGlobal.RemoveGlobalTag(tags[i], i == tags.Length - 1);
			}
		}

		public static void RemoveRoot(SimulationObject root)
		{
			object syncObject = SimulationObject.SyncObject;
			lock (syncObject)
			{
				SimulationGlobal.Unsafe_RemoveRoot(root);
			}
		}

		public static void ReadXml(XmlReader reader)
		{
			if (reader.IsStartElement("GlobalTags"))
			{
				string text = reader.ReadElementString("GlobalTags");
				if (!string.IsNullOrEmpty(text))
				{
					SimulationGlobal.globalTags.ParseTags(text);
				}
			}
		}

		public static void WriteXml(XmlWriter writer)
		{
			writer.WriteElementString("GlobalTags", SimulationGlobal.globalTags.ToString());
		}

		internal static void Unsafe_AddRoot(SimulationObject root)
		{
			SimulationGlobal.roots.Add(root);
		}

		internal static void Unsafe_RemoveRoot(SimulationObject root)
		{
			SimulationGlobal.roots.Remove(root);
		}

		private static Tags globalTags = new Tags();

		private static List<SimulationObject> roots = new List<SimulationObject>();
	}
}
