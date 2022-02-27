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
		WorldCircle worldCircle = new WorldCircle(base.WorldPosition, base.WorldEffectDefinition.Range);
		List<short> list = new List<short>();
		WorldPosition[] worldPositions = worldCircle.GetWorldPositions(base.WorldEffectManager.WorldPositionningService.World.WorldParameters);
		for (int i = 0; i < worldPositions.Length; i++)
		{
			short regionIndex = base.WorldEffectManager.WorldPositionningService.GetRegionIndex(worldPositions[i]);
			if (!list.Contains(regionIndex))
			{
				Region region = base.WorldEffectManager.WorldPositionningService.GetRegion(worldPositions[i]);
				if (region.City != null)
				{
					for (int j = 0; j < region.City.Districts.Count; j++)
					{
						District district = region.City.Districts[j];
						if (this.descriptorsAppliedOnDistrict.ContainsKey(district.WorldPosition))
						{
							List<string> list2 = this.descriptorsAppliedOnDistrict[district.WorldPosition];
							for (int k = 0; k < list2.Count; k++)
							{
								StaticString descriptorNames = list2[k];
								district.RemoveDescriptorByName(descriptorNames);
							}
						}
						district.Refresh(false);
					}
				}
				list.Add(regionIndex);
			}
		}
		worldPositions = new WorldCircle(base.WorldPosition, base.WorldEffectDefinition.Range + 1).GetWorldPositions(base.WorldEffectManager.WorldPositionningService.World.WorldParameters);
		for (int l = 0; l < worldPositions.Length; l++)
		{
			PointOfInterest pointOfInterest = base.WorldEffectManager.WorldPositionningService.GetPointOfInterest(worldPositions[l]);
			if (pointOfInterest != null)
			{
				if (pointOfInterest.CreepingNodeGUID != GameEntityGUID.Zero)
				{
					IGameEntity gameEntity = null;
					if (base.WorldEffectManager.GameEntityRepositoryService.TryGetValue(pointOfInterest.CreepingNodeGUID, out gameEntity))
					{
						CreepingNode creepingNode = gameEntity as CreepingNode;
						if (!creepingNode.IsUnderConstruction)
						{
							creepingNode.ReApplyFIMSEOnCreepingNode();
						}
					}
				}
				else if (pointOfInterest.SimulationObject.Tags.Contains(Village.ConvertedVillage))
				{
					DepartmentOfTheInterior.ReApplyFIMSEOnConvertedVillage(pointOfInterest.Empire, pointOfInterest);
				}
			}
		}
	}

	private List<District> GetAffectedDistrict()
	{
		List<District> list = new List<District>();
		WorldCircle worldCircle = new WorldCircle(base.WorldPosition, base.WorldEffectDefinition.Range);
		List<short> list2 = new List<short>();
		WorldPosition[] worldPositions = worldCircle.GetWorldPositions(base.WorldEffectManager.WorldPositionningService.World.WorldParameters);
		for (int i = 0; i < worldPositions.Length; i++)
		{
			short regionIndex = base.WorldEffectManager.WorldPositionningService.GetRegionIndex(worldPositions[i]);
			if (!list2.Contains(regionIndex))
			{
				Region region = base.WorldEffectManager.WorldPositionningService.GetRegion(worldPositions[i]);
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
				list2.Add(regionIndex);
			}
		}
		worldPositions = new WorldCircle(base.WorldPosition, base.WorldEffectDefinition.Range + 1).GetWorldPositions(base.WorldEffectManager.WorldPositionningService.World.WorldParameters);
		for (int j = 0; j < worldPositions.Length; j++)
		{
			PointOfInterest pointOfInterest = base.WorldEffectManager.WorldPositionningService.GetPointOfInterest(worldPositions[j]);
			if (pointOfInterest != null)
			{
				if (pointOfInterest.CreepingNodeGUID != GameEntityGUID.Zero)
				{
					IGameEntity gameEntity = null;
					if (base.WorldEffectManager.GameEntityRepositoryService.TryGetValue(pointOfInterest.CreepingNodeGUID, out gameEntity))
					{
						CreepingNode creepingNode = gameEntity as CreepingNode;
						if (!creepingNode.IsUnderConstruction)
						{
							creepingNode.ReApplyFIMSEOnCreepingNode();
						}
					}
				}
				else if (pointOfInterest.SimulationObject.Tags.Contains(Village.ConvertedVillage))
				{
					DepartmentOfTheInterior.ReApplyFIMSEOnConvertedVillage(pointOfInterest.Empire, pointOfInterest);
				}
			}
		}
		return list;
	}

	protected Dictionary<WorldPosition, List<string>> descriptorsAppliedOnDistrict;
}
