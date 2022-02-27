using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Simulation;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class WorldEffect_FIDSModifier : WorldEffect, IXmlSerializable
{
	public WorldEffect_FIDSModifier()
	{
		this.descriptorsAppliedOnDistrict = new Dictionary<WorldPosition, List<string>>();
	}

	public override void ReadXml(XmlReader reader)
	{
		base.ReadXml(reader);
		if (reader.IsStartElement("DescriptorsAppliedOnDistrict"))
		{
			int attribute = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("DescriptorsAppliedOnDistrict");
			if (attribute > 0)
			{
				for (int i = 0; i < attribute; i++)
				{
					WorldPosition key;
					key.Row = reader.GetAttribute<short>("Row");
					key.Column = reader.GetAttribute<short>("Column");
					reader.ReadStartElement("District");
					this.descriptorsAppliedOnDistrict.Add(key, new List<string>());
					int attribute2 = reader.GetAttribute<int>("Count");
					reader.ReadStartElement("DescriptorsNames");
					if (attribute2 > 0)
					{
						for (int j = 0; j < attribute2; j++)
						{
							string attribute3 = reader.GetAttribute("Value");
							reader.ReadStartElement("DescriptorName");
							this.descriptorsAppliedOnDistrict[key].Add(attribute3);
						}
						reader.ReadEndElement("DescriptorsNames");
					}
					reader.ReadEndElement("District");
				}
				reader.ReadEndElement("DescriptorsAppliedOnDistrict");
			}
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		base.WriteXml(writer);
		writer.WriteStartElement("DescriptorsAppliedOnDistrict");
		writer.WriteAttributeString<int>("Count", this.descriptorsAppliedOnDistrict.Keys.Count);
		foreach (KeyValuePair<WorldPosition, List<string>> keyValuePair in this.descriptorsAppliedOnDistrict)
		{
			writer.WriteStartElement("District");
			writer.WriteAttributeString<short>("Row", keyValuePair.Key.Row);
			writer.WriteAttributeString<short>("Column", keyValuePair.Key.Column);
			writer.WriteStartElement("DescriptorsNames");
			writer.WriteAttributeString<int>("Count", keyValuePair.Value.Count);
			for (int i = 0; i < keyValuePair.Value.Count; i++)
			{
				writer.WriteStartElement("DescriptorName");
				writer.WriteAttributeString("Value", keyValuePair.Value[i]);
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
	}

	public List<SimulationDescriptor> GetFidsModifierDescriptorsHavingAnEffectOnPosition(WorldPosition position)
	{
		List<SimulationDescriptor> list = new List<SimulationDescriptor>();
		int distance = base.WorldEffectManager.WorldPositionningService.GetDistance(base.WorldPosition, position);
		WorldEffectDefinition_FIDSModifier worldEffectDefinition_FIDSModifier = base.WorldEffectDefinition as WorldEffectDefinition_FIDSModifier;
		if (worldEffectDefinition_FIDSModifier.CheckPrerequisites(base.Empire) && worldEffectDefinition_FIDSModifier.Range >= distance)
		{
			list.AddRange(worldEffectDefinition_FIDSModifier.SimulationDescriptors);
		}
		return list;
	}

	public void AddFidsModifierDescriptors(SimulationObject district, WorldPosition worldPosition, bool districtIsProxy)
	{
		List<SimulationDescriptor> fidsModifierDescriptorsHavingAnEffectOnPosition = this.GetFidsModifierDescriptorsHavingAnEffectOnPosition(worldPosition);
		for (int i = 0; i < fidsModifierDescriptorsHavingAnEffectOnPosition.Count; i++)
		{
			SimulationDescriptor simulationDescriptor = fidsModifierDescriptorsHavingAnEffectOnPosition[i];
			district.AddDescriptor(simulationDescriptor);
			if (!districtIsProxy)
			{
				if (!this.descriptorsAppliedOnDistrict.ContainsKey(worldPosition))
				{
					this.descriptorsAppliedOnDistrict.Add(worldPosition, new List<string>());
				}
				this.descriptorsAppliedOnDistrict[worldPosition].Add(simulationDescriptor.Name);
			}
		}
		district.Refresh();
	}

	public override void Activate()
	{
		base.Activate();
		List<District> affectedDistrict = this.GetAffectedDistrict();
		for (int i = 0; i < affectedDistrict.Count; i++)
		{
			District district = affectedDistrict[i];
			this.AddFidsModifierDescriptors(district.SimulationObject, district.WorldPosition, false);
			district.Refresh(false);
		}
	}

	public override void Deactivate()
	{
		base.Deactivate();
		Region region = base.WorldEffectManager.WorldPositionningService.GetRegion(base.WorldPosition);
		if (region.City != null)
		{
			for (int i = 0; i < region.City.Districts.Count; i++)
			{
				District district = region.City.Districts[i];
				if (this.descriptorsAppliedOnDistrict.ContainsKey(district.WorldPosition))
				{
					List<string> list = this.descriptorsAppliedOnDistrict[district.WorldPosition];
					for (int j = 0; j < list.Count; j++)
					{
						StaticString descriptorNames = list[j];
						district.RemoveDescriptorByName(descriptorNames);
					}
				}
				district.Refresh(false);
			}
		}
	}

	private List<District> GetAffectedDistrict()
	{
		List<District> list = new List<District>();
		Region region = base.WorldEffectManager.WorldPositionningService.GetRegion(base.WorldPosition);
		if (region.City != null)
		{
			foreach (District district in region.City.Districts)
			{
				if (base.HasAnEffectOnPosition(district.WorldPosition))
				{
					list.Add(district);
				}
			}
		}
		return list;
	}

	protected Dictionary<WorldPosition, List<string>> descriptorsAppliedOnDistrict;
}
