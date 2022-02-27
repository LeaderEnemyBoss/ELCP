using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Amplitude.Xml.Serialization;
using UnityEngine;

namespace Amplitude.Unity.Framework
{
	public class Database<T> : IEnumerable, IDatabaseControl, IDatabase<T>, IEnumerable<T> where T : IDatatableElement
	{
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		bool IDatabaseControl.LoadFile(string path, XmlAttributeOverride[] overrides, XmlExtraType[] extraTypes)
		{
			return this.LoadFile(path, overrides, extraTypes);
		}

		void IDatabaseControl.RollbackTo(int revision)
		{
			this.RollbackTo(revision);
		}

		public XmlExtraType[] ExtraTypes { get; private set; }

		public XmlAttributeOverride[] Overrides { get; private set; }

		public void Add(T datatableElement)
		{
			if (datatableElement == null)
			{
				throw new ArgumentNullException("datatableElement");
			}
			int num = Databases.CurrentRevision + 1;
			Datatable<T> datatable = null;
			for (int i = 0; i < this.datatables.Count; i++)
			{
				if (this.datatables[i].Revision == num)
				{
					datatable = this.datatables[i];
					break;
				}
			}
			if (datatable == null)
			{
				datatable = new Datatable<T>(num);
				this.datatables.Add(datatable);
			}
			datatable.Add(datatableElement.Name, datatableElement);
		}

		public void Clear()
		{
			int num = this.datatables.Count - 1;
			if (num >= 0)
			{
				this.datatables[num].Clear();
			}
		}

		public byte[] ComputeHash()
		{
			if (this.datatables == null || this.datatables.Count == 0)
			{
				return new byte[0];
			}
			if (Databases.HashAlgorithm == null)
			{
				return new byte[0];
			}
			byte[] result;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				for (int i = 0; i < this.datatables.Count; i++)
				{
					if (this.datatables[i].HashBytes != null)
					{
						memoryStream.Write(this.datatables[i].HashBytes, 0, this.datatables[i].HashBytes.Length);
					}
				}
				memoryStream.Seek(0L, SeekOrigin.Begin);
				result = Databases.HashAlgorithm.ComputeHash(memoryStream);
			}
			return result;
		}

		public bool ContainsKey(StaticString key)
		{
			for (int i = this.datatables.Count - 1; i >= 0; i--)
			{
				if (this.datatables[i].ContainsKey(key))
				{
					return true;
				}
			}
			return false;
		}

		public IEnumerator<T> GetEnumerator()
		{
			List<StaticString> keys = new List<StaticString>();
			for (int index = this.datatables.Count - 1; index >= 0; index--)
			{
				foreach (T element in this.datatables[index].Values)
				{
					if (!keys.Contains(element.Name))
					{
						keys.Add(element.Name);
						yield return element;
					}
				}
			}
			yield break;
		}

		public T GetValue(StaticString key)
		{
			for (int i = this.datatables.Count - 1; i >= 0; i--)
			{
				if (this.datatables[i].ContainsKey(key))
				{
					return this.datatables[i][key];
				}
			}
			return default(T);
		}

		public T GetValue(StaticString key, T defaultValue)
		{
			for (int i = this.datatables.Count - 1; i >= 0; i--)
			{
				if (this.datatables[i].ContainsKey(key))
				{
					return this.datatables[i][key];
				}
			}
			return defaultValue;
		}

		public T[] GetValues()
		{
			Datatable<T> overallDatatable = this.GetOverallDatatable();
			return this.GetValues(overallDatatable);
		}

		public void Remove(T datatableElement)
		{
			if (datatableElement == null)
			{
				throw new ArgumentNullException("datatableElement");
			}
			int num = this.datatables.Count - 1;
			if (num >= 0)
			{
				if (this.datatables[num].ContainsKey(datatableElement.Name))
				{
					this.datatables[num].Remove(datatableElement.Name);
				}
			}
		}

		public void Remove(StaticString key)
		{
			if (StaticString.IsNullOrEmpty(key))
			{
				return;
			}
			int num = this.datatables.Count - 1;
			if (num >= 0)
			{
				if (this.datatables[num].ContainsKey(key))
				{
					this.datatables[num].Remove(key);
				}
			}
		}

		public IEnumerable<T> Shuffle()
		{
			T[] collection = this.GetValues();
			List<int> indices = new List<int>();
			System.Random random = new System.Random();
			for (int index = 0; index < collection.Length; index++)
			{
				int at = random.Next(index);
				indices.Insert(at, index);
			}
			for (int index2 = 0; index2 < collection.Length; index2++)
			{
				yield return collection[indices[index2]];
			}
			yield break;
		}

		public void Touch(T datatableElement)
		{
			if (datatableElement == null)
			{
				throw new ArgumentNullException("datatableElement");
			}
			int num = Databases.CurrentRevision + 1;
			Datatable<T> datatable = null;
			for (int i = 0; i < this.datatables.Count; i++)
			{
				if (this.datatables[i].Revision == num)
				{
					datatable = this.datatables[i];
					break;
				}
			}
			if (datatable == null)
			{
				datatable = new Datatable<T>(num);
				this.datatables.Add(datatable);
			}
			if (datatable.ContainsKey(datatableElement.Name))
			{
				datatable[datatableElement.Name] = datatableElement;
			}
			else
			{
				datatable.Add(datatableElement.Name, datatableElement);
			}
		}

		public bool TryGetValue(StaticString key, out T value)
		{
			if (key == null)
			{
				throw new ArgumentNullException("key");
			}
			value = default(T);
			if (!StaticString.IsNullOrEmpty(key))
			{
				for (int i = this.datatables.Count - 1; i >= 0; i--)
				{
					if (this.datatables[i].TryGetValue(key, out value))
					{
						return true;
					}
				}
			}
			return false;
		}

		internal bool LoadFile(string path, XmlAttributeOverride[] overrides = null, XmlExtraType[] extraTypes = null)
		{
			if (this.Overrides == null)
			{
				this.Overrides = overrides;
			}
			else
			{
				int num = 0;
				foreach (XmlAttributeOverride xmlAttributeOverride in this.Overrides)
				{
					if (xmlAttributeOverride.ExtraTypes != null)
					{
						num += xmlAttributeOverride.ExtraTypes.Length;
					}
				}
				int num2 = 0;
				if (overrides != null)
				{
					foreach (XmlAttributeOverride xmlAttributeOverride2 in overrides)
					{
						if (xmlAttributeOverride2.ExtraTypes != null)
						{
							num2 += xmlAttributeOverride2.ExtraTypes.Length;
						}
					}
				}
				if (num > num2)
				{
					overrides = this.Overrides;
				}
				else
				{
					this.Overrides = overrides;
				}
			}
			if (this.ExtraTypes == null)
			{
				this.ExtraTypes = extraTypes;
			}
			else
			{
				int num3 = 0;
				if (extraTypes != null)
				{
					num3 += extraTypes.Length;
				}
				if (this.ExtraTypes.Length > num3)
				{
					extraTypes = this.ExtraTypes;
				}
				else
				{
					this.ExtraTypes = extraTypes;
				}
			}
			int num4 = Databases.CurrentRevision + 1;
			Datatable<T> datatable = null;
			for (int j = 0; j < this.datatables.Count; j++)
			{
				if (this.datatables[j].Revision == num4)
				{
					datatable = this.datatables[j];
					break;
				}
			}
			if (datatable == null)
			{
				datatable = new Datatable<T>(num4);
				this.datatables.Add(datatable);
			}
			return datatable.LoadFromFile(path, overrides, extraTypes);
		}

		internal bool LoadFile(TextAsset textAsset, XmlAttributeOverride[] overrides = null, XmlExtraType[] extraTypes = null)
		{
			this.Overrides = overrides;
			this.ExtraTypes = extraTypes;
			int num = Databases.CurrentRevision + 1;
			Datatable<T> datatable = null;
			for (int i = 0; i < this.datatables.Count; i++)
			{
				if (this.datatables[i].Revision == num)
				{
					datatable = this.datatables[i];
					break;
				}
			}
			if (datatable == null)
			{
				datatable = new Datatable<T>(num);
				this.datatables.Add(datatable);
			}
			return datatable.LoadFromTextAsset(textAsset, overrides, extraTypes);
		}

		internal void RollbackTo(int revision)
		{
			for (int i = 0; i < this.datatables.Count; i++)
			{
				if (this.datatables[i].Revision > revision)
				{
					this.datatables.RemoveAt(i);
					i--;
				}
			}
		}

		private Datatable<T> GetOverallDatatable()
		{
			Datatable<T> datatable = new Datatable<T>(-1);
			for (int i = 0; i < this.datatables.Count; i++)
			{
				foreach (KeyValuePair<StaticString, T> keyValuePair in this.datatables[i])
				{
					if (datatable.ContainsKey(keyValuePair.Key))
					{
						datatable[keyValuePair.Key] = keyValuePair.Value;
					}
					else
					{
						datatable.Add(keyValuePair.Key, keyValuePair.Value);
					}
				}
			}
			return datatable;
		}

		private Datatable<T> GetOverallDatatable(Func<T, bool> func)
		{
			Datatable<T> datatable = new Datatable<T>(-1);
			for (int i = 0; i < this.datatables.Count; i++)
			{
				foreach (KeyValuePair<StaticString, T> keyValuePair in this.datatables[i])
				{
					if (func(keyValuePair.Value))
					{
						if (datatable.ContainsKey(keyValuePair.Key))
						{
							datatable[keyValuePair.Key] = keyValuePair.Value;
						}
						else
						{
							datatable.Add(keyValuePair.Key, keyValuePair.Value);
						}
					}
				}
			}
			return datatable;
		}

		private T[] GetValues(Datatable<T> datatable)
		{
			T[] array = new T[datatable.Values.Count];
			datatable.Values.CopyTo(array, 0);
			return array;
		}

		private List<Datatable<T>> datatables = new List<Datatable<T>>();
	}
}
