using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.AI;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class AILayer_War : AILayerWithObjective, IXmlSerializable
{
	public AILayer_War() : base("War")
	{
		this.cityQualityScoreMultiplier = 0.5f;
		this.cityToAttack = new List<City>();
		this.currentRegionResourceCount = new Dictionary<StaticString, float>();
		this.distanceDeboost = 0.5f;
		this.empireResourceCount = new Dictionary<StaticString, float>();
		this.geostraticScoreMultiplier = 0.5f;
		this.numberOfAlliedRegionAroundMultiplier = 0.5f;
		this.numberOfColdWarRegionAroundMultiplier = 0.5f;
		this.numberOfMyRegionAroundMultiplier = 0.5f;
		this.numberOfNeutralRegionAroundMultiplier = 0.5f;
		this.numberOfPeaceRegionAroundMultiplier = 0.5f;
		this.numberOfVillageDestroyedMultiplier = 0.5f;
		this.numberOfVillagePacifiedAndBuiltMultiplier = 0.5f;
		this.numberOfVillageUnpacifiedMultiplier = 0.5f;
		this.numberOfWarRegionAroundMultiplier = 0.5f;
		this.regionToAttack = new List<AIRegionData>();
		this.resourceScoreMultiplier = 0.5f;
		this.villageScoreMultiplier = 0.5f;
		this.distanceToClosestCity = new List<float>();
		this.ownershipBoost = 0.2f;
	}

	public override void ReadXml(XmlReader reader)
	{
		base.ReadXml(reader);
		int attribute = reader.GetAttribute<int>("Count");
		if (attribute > 0)
		{
			if (attribute != this.warInfoByEmpireIndex.Length)
			{
				this.warInfoByEmpireIndex = new WarInfo[attribute];
				for (int i = 0; i < this.warInfoByEmpireIndex.Length; i++)
				{
					this.warInfoByEmpireIndex[i] = new WarInfo();
					this.warInfoByEmpireIndex[i].EnnemyEmpireIndex = i;
					this.warInfoByEmpireIndex[i].WarStatus = AILayer_War.WarStatusType.None;
				}
			}
			if (reader.IsStartElement("WarStatusByEmpire"))
			{
				reader.ReadStartElement("WarStatusByEmpire");
				try
				{
					for (int j = 0; j < attribute; j++)
					{
						this.warInfoByEmpireIndex[j].WarStatus = (AILayer_War.WarStatusType)reader.ReadElementString<int>("WarStatus");
					}
				}
				catch
				{
					throw;
				}
				reader.ReadEndElement("WarStatusByEmpire");
				return;
			}
			if (reader.IsStartElement("WarInfos"))
			{
				reader.ReadStartElement("WarInfos");
				try
				{
					for (int k = 0; k < attribute; k++)
					{
						reader.ReadElementSerializable<WarInfo>(ref this.warInfoByEmpireIndex[k]);
					}
				}
				catch
				{
					throw;
				}
				reader.ReadEndElement("WarInfos");
				return;
			}
		}
		else
		{
			reader.Skip();
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		base.WriteXml(writer);
		writer.WriteStartElement("WarInfos");
		writer.WriteAttributeString<int>("Count", (this.warInfoByEmpireIndex != null) ? this.warInfoByEmpireIndex.Length : 0);
		for (int i = 0; i < this.warInfoByEmpireIndex.Length; i++)
		{
			writer.WriteElementSerializable<WarInfo>(ref this.warInfoByEmpireIndex[i]);
		}
		writer.WriteEndElement();
	}

	private void ComputeCitiesWarScore()
	{
		this.distanceToClosestCity.Clear();
		float num = 0f;
		float num2 = float.MaxValue;
		for (int i = 0; i < this.cityToAttack.Count; i++)
		{
			City city = this.cityToAttack[i];
			float num3 = float.MaxValue;
			if (this.departmentOfCreepingNodes != null && this.departmentOfForeignAffairs.IsAtWarWith(city.Empire))
			{
				foreach (CreepingNode creepingNode in this.departmentOfCreepingNodes.Nodes)
				{
					if (!creepingNode.IsUnderConstruction && creepingNode.Region.Index == city.Region.Index && AILayer_Exploration.IsTravelAllowedInNode(base.AIEntity.Empire, creepingNode))
					{
						num3 = 8f;
					}
				}
			}
			if (num3 == 3.40282347E+38f)
			{
				for (int j = 0; j < this.departmentOfTheInterior.Cities.Count; j++)
				{
					City city2 = this.departmentOfTheInterior.Cities[j];
					float num4 = (float)this.worldPositionningService.GetDistance(city.WorldPosition, city2.WorldPosition);
					if (num3 > num4)
					{
						num3 = num4;
					}
				}
			}
			this.distanceToClosestCity.Add(num3);
			if (num < num3)
			{
				num = num3;
			}
			if (num3 < num2)
			{
				num2 = num3;
			}
		}
		if (this.departmentOfForeignAffairs.IsInWarWithSomeone())
		{
			for (int k = this.cityToAttack.Count - 1; k >= 0; k--)
			{
				City city3 = this.cityToAttack[k];
				if (base.AIEntity.Empire is MajorEmpire && this.distanceToClosestCity[k] > 2f * num2 + 5f && base.AIEntity.Empire.GetPropertyValue(SimulationProperties.LandMilitaryPower) < 0.8f * city3.Empire.GetPropertyValue(SimulationProperties.LandMilitaryPower))
				{
					this.cityToAttack.RemoveAt(k);
					this.distanceToClosestCity.RemoveAt(k);
				}
			}
		}
		num = Mathf.Max(num, 1f);
		for (int l = 0; l < this.cityToAttack.Count; l++)
		{
			City city4 = this.cityToAttack[l];
			AIRegionData regionData = this.worldAtlasAIHelper.GetRegionData(base.AIEntity.Empire.Index, city4.Region.Index);
			regionData.WarAttackScore.Reset();
			HeuristicValue operand = this.ComputeGeostrategicScore(city4.Region, this.geostraticScoreMultiplier);
			HeuristicValue operand2 = this.ComputeResourceScore(city4.Region, this.resourceScoreMultiplier);
			HeuristicValue operand3 = this.ComputeVillageScore(regionData, this.villageScoreMultiplier);
			HeuristicValue heuristicValue = new HeuristicValue(0f);
			heuristicValue.Add(1f, "(constant) TODO!!!", new object[0]);
			heuristicValue.Multiply(this.cityQualityScoreMultiplier, "Xml factor", new object[0]);
			HeuristicValue heuristicValue2 = new HeuristicValue(0f);
			heuristicValue2.Add(operand, "Geostrategic score", new object[0]);
			heuristicValue2.Add(operand2, "Resource score", new object[0]);
			heuristicValue2.Add(operand3, "Village score", new object[0]);
			heuristicValue2.Add(heuristicValue, "City quality score", new object[0]);
			HeuristicValue heuristicValue3 = new HeuristicValue(0f);
			heuristicValue3.Add(this.geostraticScoreMultiplier, "Geostrat xml multiplier", new object[0]);
			heuristicValue3.Add(this.resourceScoreMultiplier, "Resource xml multiplier", new object[0]);
			heuristicValue3.Add(this.villageScoreMultiplier, "Village xml multiplier", new object[0]);
			heuristicValue3.Add(this.cityQualityScoreMultiplier, "City quality xml multiplier", new object[0]);
			heuristicValue3.Max(1f, "Avoid dividing by 0", new object[0]);
			regionData.WarAttackScore.Add(heuristicValue2, "Raw region score", new object[0]);
			regionData.WarAttackScore.Divide(heuristicValue3, "Region score multiplier", new object[0]);
			HeuristicValue operand4 = this.ComputeCityVulnerability(city4.Region);
			regionData.WarAttackScore.Multiply(operand4, "City vulnerability", new object[0]);
			HeuristicValue heuristicValue4 = new HeuristicValue(0f);
			heuristicValue4.Add(this.distanceToClosestCity[l], "Distance to closest city", new object[0]);
			heuristicValue4.Divide(num, "Maximal distance to cities", new object[0]);
			heuristicValue4.Multiply(this.distanceDeboost, "Xml factor", new object[0]);
			heuristicValue4.Multiply(-1f, "Invert the boost!", new object[0]);
			regionData.WarAttackScore.Boost(heuristicValue4, "Distance boost", new object[0]);
			if (city4.BesiegingEmpireIndex == base.AIEntity.Empire.Index)
			{
				regionData.WarAttackScore.Boost(0.2f, "Already under siege", new object[0]);
			}
			if (!city4.Empire.IsControlledByAI)
			{
				if (this.IsWonderInConstruction(city4))
				{
					regionData.WarAttackScore.Boost(0.8f, "Victory Wonder in construction", new object[0]);
				}
				else if (this.humanTargetBoost != 1f)
				{
					regionData.WarAttackScore.Boost(this.humanTargetBoost - 1f, "Human Target", new object[0]);
				}
			}
			this.regionToAttack.Add(regionData);
		}
	}

	private HeuristicValue ComputeGeostrategicScore(Region region, float modifier)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		int num7 = 0;
		for (int i = 0; i < region.Borders.Length; i++)
		{
			Region region2 = this.worldPositionningService.GetRegion(region.Borders[i].NeighbourRegionIndex);
			if (region2 != null && !region2.IsOcean && !region2.IsWasteland)
			{
				num7++;
				if (region2.City == null)
				{
					num4++;
				}
				else if (region2.City.Empire == base.AIEntity.Empire)
				{
					num++;
				}
				else
				{
					DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(region2.City.Empire);
					if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.Alliance)
					{
						num2++;
					}
					else if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.Peace)
					{
						num3++;
					}
					else if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.ColdWar || diplomaticRelation.State.Name == DiplomaticRelationState.Names.Truce)
					{
						num5++;
					}
					else if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.War)
					{
						num6++;
					}
					else
					{
						num4++;
					}
				}
			}
		}
		HeuristicValue heuristicValue = new HeuristicValue(0f);
		HeuristicValue heuristicValue2 = new HeuristicValue(0f);
		heuristicValue2.Add((float)num, "Number of my region around", new object[0]);
		heuristicValue2.Multiply(this.numberOfMyRegionAroundMultiplier, "Xml factor", new object[0]);
		HeuristicValue heuristicValue3 = new HeuristicValue(0f);
		heuristicValue3.Add((float)num2, "Number of allied region around", new object[0]);
		heuristicValue3.Multiply(this.numberOfAlliedRegionAroundMultiplier, "Xml factor", new object[0]);
		HeuristicValue heuristicValue4 = new HeuristicValue(0f);
		heuristicValue4.Add((float)num3, "Number of peace region around", new object[0]);
		heuristicValue4.Multiply(this.numberOfPeaceRegionAroundMultiplier, "Xml factor", new object[0]);
		HeuristicValue heuristicValue5 = new HeuristicValue(0f);
		heuristicValue5.Add((float)num4, "Number of neutral region around", new object[0]);
		heuristicValue5.Multiply(this.numberOfNeutralRegionAroundMultiplier, "Xml factor", new object[0]);
		HeuristicValue heuristicValue6 = new HeuristicValue(0f);
		heuristicValue6.Add((float)num5, "Number of cold war region around", new object[0]);
		heuristicValue6.Multiply(this.numberOfColdWarRegionAroundMultiplier, "Xml factor", new object[0]);
		HeuristicValue heuristicValue7 = new HeuristicValue(0f);
		heuristicValue7.Add((float)num6, "Number of war region around", new object[0]);
		heuristicValue7.Multiply(this.numberOfWarRegionAroundMultiplier, "Xml factor", new object[0]);
		heuristicValue.Add(heuristicValue2, "My region", new object[0]);
		heuristicValue.Add(heuristicValue3, "Allied region", new object[0]);
		heuristicValue.Add(heuristicValue4, "Peace region", new object[0]);
		heuristicValue.Add(heuristicValue5, "Neutral region", new object[0]);
		heuristicValue.Add(heuristicValue6, "Cold war region", new object[0]);
		heuristicValue.Add(heuristicValue7, "War region", new object[0]);
		HeuristicValue heuristicValue8 = new HeuristicValue(0f);
		heuristicValue8.Max(this.numberOfMyRegionAroundMultiplier, "My region factor", new object[0]);
		heuristicValue8.Max(this.numberOfAlliedRegionAroundMultiplier, "Allied region factor", new object[0]);
		heuristicValue8.Max(this.numberOfPeaceRegionAroundMultiplier, "Peace region factor", new object[0]);
		heuristicValue8.Max(this.numberOfNeutralRegionAroundMultiplier, "Neutral region factor", new object[0]);
		heuristicValue8.Max(this.numberOfColdWarRegionAroundMultiplier, "ColdWar region factor", new object[0]);
		heuristicValue8.Max(this.numberOfWarRegionAroundMultiplier, "War region factor", new object[0]);
		HeuristicValue heuristicValue9 = new HeuristicValue(0f);
		heuristicValue9.Add((float)num7, "Number of Neighbours", new object[0]);
		heuristicValue9.Max(1f, "Avoid division by 0", new object[0]);
		heuristicValue9.Multiply(heuristicValue8, "Max xml factor", new object[0]);
		heuristicValue.Divide(heuristicValue9, "Normalization", new object[0]);
		heuristicValue.Multiply(modifier, "Xml factor", new object[0]);
		return heuristicValue;
	}

	private HeuristicValue ComputeCityVulnerability(Region region)
	{
		HeuristicValue heuristicValue = new HeuristicValue(0f);
		if (region.City == null)
		{
			heuristicValue.Log("No city", new object[0]);
		}
		else if (region.City.Empire == base.AIEntity.Empire)
		{
			heuristicValue.Log("My city", new object[0]);
		}
		else
		{
			HeuristicValue heuristicValue2 = new HeuristicValue(0f);
			heuristicValue2.Add(base.AIEntity.Empire.GetPropertyValue(SimulationProperties.MilitaryPower), "My empire Power", new object[0]);
			HeuristicValue heuristicValue3 = new HeuristicValue(0f);
			DepartmentOfDefense agency = region.City.Empire.GetAgency<DepartmentOfDefense>();
			for (int i = 0; i < agency.Armies.Count; i++)
			{
				if (this.worldPositionningService.GetRegion(agency.Armies[i].WorldPosition).Index == region.Index)
				{
					float propertyValue = agency.Armies[i].GetPropertyValue(SimulationProperties.MilitaryPower);
					heuristicValue3.Add(propertyValue, "Army at {0}", new object[]
					{
						agency.Armies[i].WorldPosition
					});
				}
			}
			float operand = this.intelligenceAIHelper.EvaluateMilitaryPowerOfGarrison(base.AIEntity.Empire, region.City, 0);
			heuristicValue3.Add(operand, "Evaluate city military power", new object[0]);
			if (heuristicValue3 + heuristicValue2 > 0f)
			{
				heuristicValue.Add(heuristicValue2, "My Empire MP", new object[0]);
				HeuristicValue heuristicValue4 = new HeuristicValue(0f);
				heuristicValue4.Add(heuristicValue2, "My empire power", new object[0]);
				heuristicValue4.Add(heuristicValue3, "Region enemy power", new object[0]);
				heuristicValue.Divide(heuristicValue4, "(MyEmpireMP + regionMP)", new object[0]);
			}
			else
			{
				heuristicValue.Add(1f, "No defense in the region!", new object[0]);
			}
		}
		return heuristicValue;
	}

	private void ComputeObjectivesPriority()
	{
		base.GlobalPriority.Reset();
		AILayer_Strategy layer = base.AIEntity.GetLayer<AILayer_Strategy>();
		base.GlobalPriority.Add(layer.StrategicNetwork.GetAgentValue("WarNeed"), "Strategic network 'WarNeed'", new object[0]);
		this.EmpirePreprocessor();
		this.cityToAttack.Clear();
		this.regionToAttack.Clear();
		this.GatherEnnemyCities(this.cityToAttack);
		this.ComputeCitiesWarScore();
		base.GatherObjectives(AICommanderMissionDefinition.AICommanderCategory.War.ToString(), ref this.globalObjectiveMessages);
		base.ValidateMessages(ref this.globalObjectiveMessages);
		List<global::Empire> list = new List<global::Empire>();
		int num = 0;
		int num2 = 0;
		bool flag = false;
		using (IEnumerator<City> enumerator = this.departmentOfTheInterior.Cities.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.BesiegingEmpire != null)
				{
					flag = true;
					break;
				}
			}
		}
		float num3 = 0f;
		int num4 = -1;
		int index;
		Predicate<AIRegionData> <>9__0;
		int index2;
		for (index = 0; index < this.globalObjectiveMessages.Count; index = index2 + 1)
		{
			List<AIRegionData> list2 = this.regionToAttack;
			Predicate<AIRegionData> match2;
			if ((match2 = <>9__0) == null)
			{
				match2 = (<>9__0 = ((AIRegionData match) => match.RegionIndex == this.globalObjectiveMessages[index].RegionIndex));
			}
			int num5 = list2.FindIndex(match2);
			if (num5 >= 0)
			{
				if (this.globalObjectiveMessages[index].ObjectiveState == "Attacking")
				{
					num++;
				}
				else
				{
					num2++;
				}
				AIRegionData airegionData = this.regionToAttack[num5];
				City city = this.cityToAttack[num5];
				HeuristicValue heuristicValue = new HeuristicValue(0f);
				heuristicValue.Add(airegionData.WarAttackScore, "Attack score from the region", new object[0]);
				heuristicValue.Boost(0.3f, "constant", new object[0]);
				this.globalObjectiveMessages[index].GlobalPriority = base.GlobalPriority;
				this.globalObjectiveMessages[index].LocalPriority = heuristicValue;
				this.globalObjectiveMessages[index].TimeOut = 1;
				this.regionToAttack.RemoveAt(num5);
				this.cityToAttack.RemoveAt(num5);
				if (!list.Contains(city.Empire))
				{
					list.Add(city.Empire);
				}
				if (heuristicValue > num3 && this.departmentOfForeignAffairs.IsAtWarWith(city.Empire) && !flag && base.AIEntity.Empire.GetPropertyValue(SimulationProperties.LandMilitaryPower) > city.Empire.GetPropertyValue(SimulationProperties.LandMilitaryPower) * 1.2f)
				{
					num3 = heuristicValue;
					num4 = index;
				}
			}
			index2 = index;
		}
		if (num4 >= 0)
		{
			this.globalObjectiveMessages[num4].GlobalPriority.Boost(1f, "aggro boost", new object[0]);
			this.globalObjectiveMessages[num4].LocalPriority.Boost(0.7f, "aggro boost", new object[0]);
		}
		if (num2 == 0 && this.regionToAttack.Count > 0)
		{
			this.regionToAttack.Sort((AIRegionData left, AIRegionData right) => -1 * left.WarAttackScore.CompareTo(right.WarAttackScore));
			AIRegionData regionData = null;
			for (int i = 0; i < this.regionToAttack.Count; i++)
			{
				Region region = this.worldPositionningService.GetRegion(this.regionToAttack[i].RegionIndex);
				if (list.Count < 2 || (region.City != null && list.Contains(region.City.Empire)))
				{
					if (!this.myEmpireData.HasShips)
					{
						bool flag2 = false;
						for (int j = 0; j < this.departmentOfTheInterior.Cities.Count; j++)
						{
							if (this.departmentOfTheInterior.Cities[j].Region.ContinentID == region.ContinentID)
							{
								flag2 = true;
								break;
							}
						}
						if (!flag2)
						{
							goto IL_441;
						}
					}
					regionData = this.regionToAttack[i];
					break;
				}
				IL_441:;
			}
			if (regionData == null)
			{
				return;
			}
			GlobalObjectiveMessage globalObjectiveMessage = this.globalObjectiveMessages.Find((GlobalObjectiveMessage match) => match.RegionIndex == regionData.RegionIndex);
			if (globalObjectiveMessage == null)
			{
				globalObjectiveMessage = base.GenerateObjective(regionData.RegionIndex);
			}
			City city2 = this.worldPositionningService.GetRegion(regionData.RegionIndex).City;
			HeuristicValue heuristicValue2 = new HeuristicValue(0f);
			heuristicValue2.Add(regionData.WarAttackScore, "Region war score", new object[0]);
			heuristicValue2.Boost(0.5f, "constant", new object[0]);
			globalObjectiveMessage.GlobalPriority = base.GlobalPriority;
			if (city2 != null && this.departmentOfForeignAffairs.IsAtWarWith(city2.Empire) && !flag && num4 < 0 && base.AIEntity.Empire.GetPropertyValue(SimulationProperties.LandMilitaryPower) > city2.Empire.GetPropertyValue(SimulationProperties.LandMilitaryPower))
			{
				heuristicValue2.Boost(0.8f, "aggro boost", new object[0]);
				globalObjectiveMessage.GlobalPriority.Boost(1f, "aggro boost", new object[0]);
			}
			globalObjectiveMessage.LocalPriority = heuristicValue2;
			globalObjectiveMessage.ObjectiveState = "Preparing";
			globalObjectiveMessage.TimeOut = 1;
		}
	}

	private HeuristicValue ComputeResourceScore(Region region, float modifier)
	{
		HeuristicValue heuristicValue = new HeuristicValue(0f);
		this.currentRegionResourceCount.Clear();
		this.FillRegionResourceData(region, this.currentRegionResourceCount);
		foreach (KeyValuePair<StaticString, float> keyValuePair in this.empireResourceCount)
		{
			float num = keyValuePair.Value;
			if (this.currentRegionResourceCount.ContainsKey(keyValuePair.Key))
			{
				num += this.currentRegionResourceCount[keyValuePair.Key];
			}
			if (num != 0f)
			{
				HeuristicValue heuristicValue2 = new HeuristicValue(0f);
				heuristicValue2.Add(keyValuePair.Value, "Empire Resource {0} count", new object[]
				{
					keyValuePair.Key
				});
				heuristicValue2.Divide(num, "Empire + region resource count", new object[0]);
				heuristicValue.Add(heuristicValue2, "Resource {0}", new object[]
				{
					keyValuePair.Key
				});
			}
		}
		if (this.empireResourceCount.Count > 0)
		{
			heuristicValue.Divide((float)this.empireResourceCount.Count, "Number of different resource in the empire", new object[0]);
		}
		heuristicValue.Multiply(modifier, "Xml modifier", new object[0]);
		return heuristicValue;
	}

	private HeuristicValue ComputeVillageScore(AIRegionData regionData, float modifier)
	{
		HeuristicValue heuristicValue = new HeuristicValue(0f);
		heuristicValue.Add((float)regionData.VillageDestroyed, "Village destroyed", new object[0]);
		heuristicValue.Multiply(this.numberOfVillageDestroyedMultiplier, "Xml factor", new object[0]);
		HeuristicValue heuristicValue2 = new HeuristicValue(0f);
		heuristicValue2.Add((float)regionData.VillageNotPacified, "Village not pacified", new object[0]);
		heuristicValue2.Multiply(this.numberOfVillageUnpacifiedMultiplier, "Xml factor", new object[0]);
		HeuristicValue heuristicValue3 = new HeuristicValue(0f);
		heuristicValue3.Add((float)regionData.VillagePacifiedAndBuilt, "Village built", new object[0]);
		heuristicValue3.Multiply(this.numberOfVillagePacifiedAndBuiltMultiplier, "Xml factor", new object[0]);
		HeuristicValue heuristicValue4 = new HeuristicValue(0f);
		heuristicValue4.Max(this.numberOfVillageDestroyedMultiplier, "Destroy multiplier", new object[0]);
		heuristicValue4.Max(this.numberOfVillageUnpacifiedMultiplier, "Unpacified multiplier", new object[0]);
		heuristicValue4.Max(this.numberOfVillagePacifiedAndBuiltMultiplier, "Built multiplier", new object[0]);
		HeuristicValue heuristicValue5 = new HeuristicValue(0f);
		heuristicValue5.Add(3f, "Max village possible in region (constant)", new object[0]);
		heuristicValue5.Multiply(heuristicValue4, "Max xml factor", new object[0]);
		HeuristicValue heuristicValue6 = new HeuristicValue(0f);
		heuristicValue6.Add(heuristicValue, "Village destroyed", new object[0]);
		heuristicValue6.Add(heuristicValue2, "Village not pacified", new object[0]);
		heuristicValue6.Add(heuristicValue3, "Village built", new object[0]);
		heuristicValue6.Divide(heuristicValue5, "Normalization", new object[0]);
		heuristicValue6.Multiply(modifier, "Xml factor", new object[0]);
		return heuristicValue6;
	}

	private void EmpirePreprocessor()
	{
		this.empireResourceCount.Clear();
		for (int i = 0; i < this.departmentOfTheInterior.Cities.Count; i++)
		{
			this.FillRegionResourceData(this.departmentOfTheInterior.Cities[i].Region, this.empireResourceCount);
		}
		foreach (ResourceDefinition resourceDefinition in this.resourceDatabase)
		{
			if ((resourceDefinition.ResourceType == ResourceDefinition.Type.Luxury || resourceDefinition.ResourceType == ResourceDefinition.Type.Strategic) && !this.empireResourceCount.ContainsKey(resourceDefinition.Name))
			{
				this.empireResourceCount.Add(resourceDefinition.Name, 0f);
			}
		}
	}

	private void FillRegionResourceData(Region region, Dictionary<StaticString, float> resourceCount)
	{
		for (int i = 0; i < region.PointOfInterests.Length; i++)
		{
			if (!(region.PointOfInterests[i].Type != "ResourceDeposit"))
			{
				string empty = string.Empty;
				Diagnostics.Assert(region.PointOfInterests[i].PointOfInterestDefinition.TryGetValue("ResourceName", out empty));
				if (!resourceCount.ContainsKey(empty))
				{
					resourceCount.Add(empty, 0f);
				}
				StaticString key;
				float num = resourceCount[key = empty];
				resourceCount[key] = num + 1f;
			}
		}
	}

	private void GatherEnnemyCities(List<City> ennemyCities)
	{
		global::Game game = this.gameService.Game as global::Game;
		for (int i = 0; i < this.warInfoByEmpireIndex.Length; i++)
		{
			if (i != base.AIEntity.Empire.Index)
			{
				DepartmentOfTheInterior agency = game.Empires[i].GetAgency<DepartmentOfTheInterior>();
				if (this.warInfoByEmpireIndex[i].WarStatus == AILayer_War.WarStatusType.None)
				{
					for (int j = 0; j < agency.Cities.Count; j++)
					{
						base.AIEntity.KillCommanders("AICommander_WarWithObjective", agency.Cities[j].Region.Index);
					}
				}
				else if (this.NumberOfWar > 0 && this.warInfoByEmpireIndex[i].WarStatus != AILayer_War.WarStatusType.War)
				{
					for (int k = 0; k < agency.Cities.Count; k++)
					{
						base.AIEntity.KillCommanders("AICommander_WarWithObjective", agency.Cities[k].Region.Index);
					}
				}
				else
				{
					ennemyCities.AddRange(agency.Cities);
				}
			}
		}
		for (int l = ennemyCities.Count - 1; l >= 0; l--)
		{
			if (!this.CityIsAttackable(ennemyCities[l]))
			{
				ennemyCities.RemoveAt(l);
			}
		}
	}

	private void InitializeRegistryValues()
	{
		this.numberOfMyRegionAroundMultiplier = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_War.registryPath, "NumberOfMyRegionAroundMultiplier"), this.numberOfMyRegionAroundMultiplier);
		this.numberOfAlliedRegionAroundMultiplier = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_War.registryPath, "NumberOfAlliedRegionAroundMultiplier"), this.numberOfAlliedRegionAroundMultiplier);
		this.numberOfPeaceRegionAroundMultiplier = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_War.registryPath, "NumberOfPeaceRegionAroundMultiplier"), this.numberOfPeaceRegionAroundMultiplier);
		this.numberOfNeutralRegionAroundMultiplier = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_War.registryPath, "NumberOfNeutralRegionAroundMultiplier"), this.numberOfNeutralRegionAroundMultiplier);
		this.numberOfColdWarRegionAroundMultiplier = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_War.registryPath, "NumberOfColdWarRegionAroundMultiplier"), this.numberOfColdWarRegionAroundMultiplier);
		this.numberOfWarRegionAroundMultiplier = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_War.registryPath, "NumberOfWarRegionAroundMultiplier"), this.numberOfWarRegionAroundMultiplier);
		this.numberOfVillageUnpacifiedMultiplier = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_War.registryPath, "NumberOfVillageUnpacifiedMultiplier"), this.numberOfVillageUnpacifiedMultiplier);
		this.numberOfVillageDestroyedMultiplier = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_War.registryPath, "NumberOfVillageDestroyedMultiplier"), this.numberOfVillageDestroyedMultiplier);
		this.numberOfVillagePacifiedAndBuiltMultiplier = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_War.registryPath, "NumberOfVillagePacifiedAndBuiltMultiplier"), this.numberOfVillagePacifiedAndBuiltMultiplier);
		this.geostraticScoreMultiplier = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_War.registryPath, "GeostraticScoreMultiplier"), this.geostraticScoreMultiplier);
		this.resourceScoreMultiplier = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_War.registryPath, "ResourceScoreMultiplier"), this.resourceScoreMultiplier);
		this.villageScoreMultiplier = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_War.registryPath, "VillageScoreMultiplier"), this.villageScoreMultiplier);
		this.cityQualityScoreMultiplier = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_War.registryPath, "CityQualityScoreMultiplier"), this.cityQualityScoreMultiplier);
		this.distanceDeboost = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_War.registryPath, "DistanceDeboost"), this.distanceDeboost);
		this.humanTargetBoost = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_War.registryPath, "HumanTargetBoost"), 1f);
	}

	public int NumberOfWantedWar { get; set; }

	public int NumberOfWar { get; set; }

	public AILayer_War.WarStatusType GetWarStatusWithEmpire(int empireIndex)
	{
		Diagnostics.Assert(empireIndex < this.warInfoByEmpireIndex.Length);
		return this.warInfoByEmpireIndex[empireIndex].WarStatus;
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		this.gameService = Services.GetService<IGameService>();
		this.worldPositionningService = this.gameService.Game.Services.GetService<IWorldPositionningService>();
		this.endTurnService = Services.GetService<IEndTurnService>();
		this.pathfindingService = this.gameService.Game.Services.GetService<IPathfindingService>();
		this.personalityAIHelper = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		this.intelligenceAIHelper = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		this.worldAtlasAIHelper = AIScheduler.Services.GetService<IWorldAtlasAIHelper>();
		this.resourceDatabase = Databases.GetDatabase<ResourceDefinition>(false);
		AIScheduler.Services.GetService<IAIEmpireDataAIHelper>().TryGet(base.AIEntity.Empire.Index, out this.myEmpireData);
		Diagnostics.Assert(base.AIEntity.Empire != null);
		this.departmentOfForeignAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		Diagnostics.Assert(this.departmentOfForeignAffairs != null);
		this.departmentOfForeignAffairs.DiplomaticRelationStateChange += this.WarStart;
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		Diagnostics.Assert(this.departmentOfTheInterior != null);
		this.departmentOfScience = base.AIEntity.Empire.GetAgency<DepartmentOfScience>();
		Diagnostics.Assert(this.departmentOfScience != null);
		this.departmentOfDefense = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		Diagnostics.Assert(this.departmentOfDefense != null);
		this.departmentOfCreepingNodes = base.AIEntity.Empire.GetAgency<DepartmentOfCreepingNodes>();
		this.departmentOfIndustry = base.AIEntity.Empire.GetAgency<DepartmentOfIndustry>();
		this.warInfoByEmpireIndex = new WarInfo[this.departmentOfForeignAffairs.DiplomaticRelations.Count];
		for (int i = 0; i < this.warInfoByEmpireIndex.Length; i++)
		{
			this.warInfoByEmpireIndex[i] = new WarInfo();
			this.warInfoByEmpireIndex[i].EnnemyEmpireIndex = i;
			this.warInfoByEmpireIndex[i].WarStatus = AILayer_War.WarStatusType.None;
		}
		base.AIEntity.RegisterPass(AIEntity.Passes.RefreshObjectives.ToString(), "AILayer_War_RefreshObjectives", new AIEntity.AIAction(this.RefreshObjectives), this, new StaticString[0]);
		this.ownershipBoost = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_War.registryPath, "OwnershipBoost"), this.ownershipBoost);
		this.InitializeRegistryValues();
		this.PathfindingDataBase = Databases.GetDatabase<PathfindingRule>(false);
		this.aILayer_Diplomacy = base.AIEntity.GetLayer<AILayer_Diplomacy>();
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public override IEnumerator Load()
	{
		yield return base.Load();
		yield break;
	}

	public override void Release()
	{
		base.Release();
		this.warCityDefenseJobs = null;
		this.DefensiveArmyAssignations = null;
		if (this.departmentOfForeignAffairs != null)
		{
			this.departmentOfForeignAffairs.DiplomaticRelationStateChange -= this.WarStart;
			this.departmentOfForeignAffairs = null;
		}
		this.departmentOfDefense = null;
		this.pathfindingService = null;
		this.PathfindingDataBase = null;
		this.aILayer_Diplomacy = null;
		this.departmentOfCreepingNodes = null;
		this.departmentOfIndustry = null;
	}

	public bool WantWarWithSomoeone()
	{
		for (int i = 0; i < this.warInfoByEmpireIndex.Length; i++)
		{
			if (this.warInfoByEmpireIndex[i].WarStatus == AILayer_War.WarStatusType.Preparing || this.warInfoByEmpireIndex[i].WarStatus == AILayer_War.WarStatusType.Ready)
			{
				return true;
			}
		}
		return false;
	}

	protected override int GetCommanderLimit()
	{
		return 10;
	}

	protected override bool IsObjectiveValid(StaticString objectiveType, int regionIndex, bool checkLocalPriority = false)
	{
		Region region = this.worldPositionningService.GetRegion(regionIndex);
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.Log("ELCP {0} AILayer_War IsObjectiveValid checking {1}", new object[]
			{
				base.AIEntity.Empire,
				region.LocalizedName
			});
		}
		if (region == null || region.City == null || region.City.Empire == base.AIEntity.Empire)
		{
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				Diagnostics.Log("fail 1");
			}
			return false;
		}
		if (region.City.BesiegingEmpireIndex >= 0 && region.City.BesiegingEmpireIndex != base.AIEntity.Empire.Index)
		{
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				Diagnostics.Log("fail 2");
			}
			return false;
		}
		if (!this.regionToAttack.Exists((AIRegionData x) => x.RegionIndex == regionIndex))
		{
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				Diagnostics.Log("fail 3");
			}
			return false;
		}
		if (!this.myEmpireData.HasShips)
		{
			bool flag = false;
			for (int i = 0; i < this.departmentOfTheInterior.Cities.Count; i++)
			{
				if (this.departmentOfTheInterior.Cities[i].Region.ContinentID == region.ContinentID)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
				{
					Diagnostics.Log("fail 4");
				}
				return false;
			}
		}
		if (this.NumberOfWar > 0)
		{
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				Diagnostics.Log(string.Format("Warstatus {0}", this.warInfoByEmpireIndex[region.City.Empire.Index].WarStatus));
			}
			return this.warInfoByEmpireIndex[region.City.Empire.Index].WarStatus == AILayer_War.WarStatusType.War;
		}
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.Log(string.Format("Warstatus2 {0}", this.GetWarStatusWithEmpire(region.City.Empire.Index) > AILayer_War.WarStatusType.None));
		}
		return this.GetWarStatusWithEmpire(region.City.Empire.Index) > AILayer_War.WarStatusType.None;
	}

	protected override void RefreshObjectives(StaticString context, StaticString pass)
	{
		base.RefreshObjectives(context, pass);
		this.RefreshWarStates();
		this.ComputeObjectivesPriority();
		this.DefensiveArmyAssignations.Clear();
		this.warCityDefenseJobs.Clear();
		if (this.departmentOfForeignAffairs.IsInWarWithSomeone())
		{
			this.CreateCityDefenseJobs();
			this.RefreshCityDefenseJobs();
			this.lastTickTime = global::Game.Time;
		}
	}

	private void RefreshWarStates()
	{
		global::Game game = this.gameService.Game as global::Game;
		int num = 0;
		float num2 = 0f;
		float num3 = 0f;
		int num4 = 0;
		float num5 = 0f;
		for (int i = 0; i < this.departmentOfTheInterior.Cities.Count; i++)
		{
			num3 += this.departmentOfTheInterior.Cities[i].GetPropertyValue(SimulationProperties.MilitaryPower);
			if (this.departmentOfTheInterior.Cities[i].BesiegingEmpire != null)
			{
				num++;
				num2 += DepartmentOfTheInterior.GetBesiegingPower(this.departmentOfTheInterior.Cities[i], true);
			}
			else
			{
				Army maxHostileArmy = AILayer_Pacification.GetMaxHostileArmy(base.AIEntity.Empire, this.departmentOfTheInterior.Cities[i].Region.Index);
				if (maxHostileArmy != null)
				{
					num4++;
					num5 += maxHostileArmy.GetPropertyValue(SimulationProperties.MilitaryPower);
				}
			}
		}
		this.NumberOfWar = 0;
		this.NumberOfWantedWar = 0;
		int index;
		Predicate<WantedDiplomaticRelationStateMessage> <>9__0;
		int num6;
		for (index = 0; index < this.warInfoByEmpireIndex.Length; index = num6 + 1)
		{
			if (index != base.AIEntity.Empire.Index)
			{
				DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.DiplomaticRelations[index];
				if (diplomaticRelation.State != null)
				{
					this.warInfoByEmpireIndex[index].NumberOfCurrentInterception = num4;
					this.warInfoByEmpireIndex[index].NumberOfCurrentSiegeBreaker = num;
					this.warInfoByEmpireIndex[index].EnnemyInterceptionMP = num5;
					this.warInfoByEmpireIndex[index].EnnemyBesiegingMP = num2;
					this.warInfoByEmpireIndex[index].MySiegeBreakerMP = num3;
					this.warInfoByEmpireIndex[index].EnnemyMP = game.Empires[index].GetPropertyValue(SimulationProperties.MilitaryPower);
					if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.War)
					{
						if (this.warInfoByEmpireIndex[index].WarStatus != AILayer_War.WarStatusType.War)
						{
							this.warInfoByEmpireIndex[index].WarStatus = AILayer_War.WarStatusType.War;
						}
						if (this.warInfoByEmpireIndex[index].WarBeginingTurn < 0)
						{
							this.warInfoByEmpireIndex[index].WarBeginingTurn = this.endTurnService.Turn;
						}
						num6 = this.NumberOfWar;
						this.NumberOfWar = num6 + 1;
					}
					else
					{
						if (this.warInfoByEmpireIndex[index].WarBeginingTurn >= 0)
						{
							this.warInfoByEmpireIndex[index].WarBeginingTurn = -1;
						}
						Blackboard<BlackboardLayerID, BlackboardMessage> blackboard = base.AIEntity.AIPlayer.Blackboard;
						BlackboardLayerID blackboardLayerID = BlackboardLayerID.Empire;
						BlackboardLayerID layerID = blackboardLayerID;
						Predicate<WantedDiplomaticRelationStateMessage> filter;
						if ((filter = <>9__0) == null)
						{
							filter = (<>9__0 = ((WantedDiplomaticRelationStateMessage message) => message.OpponentEmpireIndex == index));
						}
						WantedDiplomaticRelationStateMessage wantedDiplomaticRelationStateMessage = blackboard.FindFirst<WantedDiplomaticRelationStateMessage>(layerID, filter);
						if ((wantedDiplomaticRelationStateMessage != null && wantedDiplomaticRelationStateMessage.WantedDiplomaticRelationStateName == DiplomaticRelationState.Names.War && !this.aILayer_Diplomacy.AnyVictoryreactionNeeded) || (!(game.Empires[index] as MajorEmpire).IsEliminated && this.aILayer_Diplomacy.NeedsVictoryReaction[index]))
						{
							if (this.aILayer_Diplomacy.NeedsVictoryReaction[index])
							{
								this.warInfoByEmpireIndex[index].WarStatus = AILayer_War.WarStatusType.Preparing;
							}
							else if (this.warInfoByEmpireIndex[index].WarStatus == AILayer_War.WarStatusType.None || this.warInfoByEmpireIndex[index].WarStatus == AILayer_War.WarStatusType.War)
							{
								this.warInfoByEmpireIndex[index].WarStatus = AILayer_War.WarStatusType.Preparing;
							}
							else if (this.warInfoByEmpireIndex[index].WarStatus == AILayer_War.WarStatusType.Ready)
							{
								this.warInfoByEmpireIndex[index].WarStatus = AILayer_War.WarStatusType.Ready;
							}
							num6 = this.NumberOfWantedWar;
							this.NumberOfWantedWar = num6 + 1;
						}
						else
						{
							this.warInfoByEmpireIndex[index].WarStatus = AILayer_War.WarStatusType.None;
						}
					}
				}
			}
			num6 = index;
		}
	}

	public static bool IsWarTarget(AIEntity AIEntity, City city)
	{
		AILayerCommanderController layer = AIEntity.GetLayer<AILayer_ArmyManagement>();
		IWorldPositionningService service = Services.GetService<IGameService>().Game.Services.GetService<IWorldPositionningService>();
		foreach (AICommander aicommander in layer.AICommanders)
		{
			if (aicommander is AICommander_WarWithObjective)
			{
				if (aicommander.Missions.Find((AICommanderMission match) => match.AIDataArmyGUID.IsValid) != null)
				{
					Region region = service.GetRegion((aicommander as AICommander_WarWithObjective).RegionIndex);
					if (region != null && region.City != null && region.City == city && city.GetPropertyValue(SimulationProperties.CityDefensePoint) >= 70f)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private void CreateCityDefenseJobs()
	{
		this.warCityDefenseJobs.Clear();
		foreach (Army key in this.DefensiveArmyAssignations.Keys.ToList<Army>())
		{
			this.DefensiveArmyAssignations[key] = null;
		}
		foreach (City city in this.departmentOfTheInterior.Cities)
		{
			if (this.CityIsAttackable(city) && !AILayer_Military.AreaIsSave(city.WorldPosition, 15, this.departmentOfForeignAffairs, false))
			{
				AILayer_War.WarCityDefenseJob item = new AILayer_War.WarCityDefenseJob(city, 0f);
				this.warCityDefenseJobs.Add(item);
			}
		}
	}

	private void RefreshCityDefenseJobs()
	{
		for (int i = this.warCityDefenseJobs.Count - 1; i >= 0; i--)
		{
			float score = 0f;
			float enemyMP = 0f;
			bool flag = false;
			AILayer_War.WarCityDefenseJob warCityDefenseJob = this.warCityDefenseJobs[i];
			if (warCityDefenseJob.city == null || warCityDefenseJob.city.Region == null)
			{
				flag = true;
			}
			else if (warCityDefenseJob.city.Empire != base.AIEntity.Empire || AILayer_Military.AreaIsSave(warCityDefenseJob.city.WorldPosition, 12, this.departmentOfForeignAffairs, out score, out enemyMP, false))
			{
				flag = true;
			}
			if (flag)
			{
				foreach (Army army in warCityDefenseJob.AssignedArmies)
				{
					if (army != null && this.DefensiveArmyAssignations.ContainsKey(army))
					{
						this.DefensiveArmyAssignations[army] = null;
					}
				}
				this.warCityDefenseJobs.RemoveAt(i);
			}
			else
			{
				warCityDefenseJob.score = score;
				warCityDefenseJob.EnemyMP = enemyMP;
				warCityDefenseJob.RemoveDisposedArmies();
				warCityDefenseJob.Refresh();
			}
		}
	}

	private void WarStart(object sender, DiplomaticRelationStateChangeEventArgs e)
	{
		if (e.DiplomaticRelationState.Name == DiplomaticRelationState.Names.War && e.PreviousDiplomaticRelationState.Name != DiplomaticRelationState.Names.War)
		{
			this.CreateCityDefenseJobs();
			this.RefreshCityDefenseJobs();
			List<Army> list = this.DefensiveArmyAssignations.Keys.ToList<Army>();
			this.DefensiveArmyAssignations.Clear();
			foreach (Army army in list)
			{
				this.AssignDefensiveArmyToCity(army);
			}
		}
	}

	private bool CityIsAttackable(City city)
	{
		bool flag = base.AIEntity.Empire.Index == city.Empire.Index;
		if (!flag && city.BesiegingEmpireIndex >= 0 && city.BesiegingEmpireIndex != base.AIEntity.Empire.Index)
		{
			return false;
		}
		bool flag2 = this.departmentOfScience.HaveResearchedShipTechnology();
		int num = 0;
		int num2 = 3;
		foreach (District district in city.Districts)
		{
			if (!District.IsACityTile(district) && !this.worldPositionningService.IsWaterTile(district.WorldPosition))
			{
				PointOfInterest pointOfInterest = this.worldPositionningService.GetPointOfInterest(district.WorldPosition);
				PathfindingRule pathfindingRule;
				if (pointOfInterest == null)
				{
					if (flag || flag2)
					{
						return true;
					}
					if (this.PositionIsPathable(district.WorldPosition))
					{
						return true;
					}
					if (num >= num2)
					{
						return false;
					}
					num++;
				}
				else if (!this.PathfindingDataBase.TryGetValue("PointOfInterest_" + pointOfInterest.Type, out pathfindingRule))
				{
					if (flag || flag2)
					{
						return true;
					}
					if (this.PositionIsPathable(district.WorldPosition))
					{
						return true;
					}
					if (num >= num2)
					{
						return false;
					}
					num++;
				}
				else if (!pathfindingRule.IsTileUnstopable)
				{
					if (flag || flag2)
					{
						return true;
					}
					if (this.PositionIsPathable(district.WorldPosition))
					{
						return true;
					}
					if (num >= num2)
					{
						return false;
					}
					num++;
				}
			}
		}
		return false;
	}

	public void AssignDefensiveArmyToCity(Army army)
	{
		if (army == null || !army.GUID.IsValid)
		{
			return;
		}
		if (this.DefensiveArmyAssignations.ContainsKey(army))
		{
			return;
		}
		this.DefensiveArmyAssignations.Add(army, null);
		if (this.warCityDefenseJobs.Count == 0 || !this.departmentOfForeignAffairs.IsInWarWithSomeone())
		{
			return;
		}
		if (global::Game.Time - this.lastTickTime > 5.0)
		{
			this.RefreshCityDefenseJobs();
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				foreach (AILayer_War.WarCityDefenseJob warCityDefenseJob in this.warCityDefenseJobs)
				{
					Diagnostics.Log("{0}", new object[]
					{
						warCityDefenseJob.ToString()
					});
				}
			}
			this.lastTickTime = global::Game.Time;
		}
		Dictionary<int, float> dictionary = new Dictionary<int, float>();
		float num = float.MaxValue;
		for (int i = 0; i < this.warCityDefenseJobs.Count; i++)
		{
			AILayer_War.WarCityDefenseJob warCityDefenseJob2 = this.warCityDefenseJobs[i];
			if (warCityDefenseJob2.NeedmoreMP())
			{
				float num2 = Mathf.Max((float)this.worldPositionningService.GetDistance(army.WorldPosition, warCityDefenseJob2.city.WorldPosition) / (army.GetPropertyValue(SimulationProperties.MaximumMovement) * 1.5f), 1f);
				dictionary.Add(i, num2);
				if (num2 < num)
				{
					num = num2;
				}
			}
		}
		if (num == 3.40282347E+38f)
		{
			return;
		}
		bool flag = true;
		if (!army.IsSolitary)
		{
			flag = false;
		}
		else
		{
			using (IEnumerator<Unit> enumerator2 = army.Units.GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					if (!enumerator2.Current.CheckUnitAbility("UnitAbilityTeleportInRange", -1))
					{
						flag = false;
						break;
					}
				}
			}
		}
		float num3 = 0f;
		int num4 = -1;
		foreach (KeyValuePair<int, float> keyValuePair in dictionary)
		{
			float num5 = this.warCityDefenseJobs[keyValuePair.Key].score * 0.6f;
			if (flag)
			{
				num5 = AILayer.Boost(num5, 0.1f * (num / keyValuePair.Value));
			}
			else if (!army.Empire.SimulationObject.Tags.Contains("FactionTraitAffinityStrategic") || !army.Empire.SimulationObject.Tags.Contains("BoosterTeleport") || army.IsSolitary)
			{
				num5 = AILayer.Boost(num5, 0.4f * (num / keyValuePair.Value));
			}
			if (num5 > num3)
			{
				num3 = num5;
				num4 = keyValuePair.Key;
			}
		}
		if (num4 >= 0)
		{
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				Diagnostics.Log("ELCP: {0} Assigning defensive army {1} to city {2}", new object[]
				{
					base.AIEntity.Empire,
					army.LocalizedName,
					this.warCityDefenseJobs[num4].city.LocalizedName
				});
			}
			this.warCityDefenseJobs[num4].AssignedArmies.Add(army);
			this.DefensiveArmyAssignations[army] = this.warCityDefenseJobs[num4].city;
		}
	}

	public static bool IsWarTarget(AIEntity AIEntity, City city, float MindDefense = 0f)
	{
		AILayerCommanderController layer = AIEntity.GetLayer<AILayer_ArmyManagement>();
		IWorldPositionningService service = Services.GetService<IGameService>().Game.Services.GetService<IWorldPositionningService>();
		foreach (AICommander aicommander in layer.AICommanders)
		{
			if (aicommander is AICommander_WarWithObjective)
			{
				if (aicommander.Missions.Find((AICommanderMission match) => match.AIDataArmyGUID.IsValid || match.State == TickableState.NeedTick) != null)
				{
					Region region = service.GetRegion((aicommander as AICommander_WarWithObjective).RegionIndex);
					if (region != null && region.City != null && region.City == city && city.GetPropertyValue(SimulationProperties.CityDefensePoint) >= MindDefense)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private bool PositionIsPathable(WorldPosition position)
	{
		if (!position.IsValid || this.departmentOfTheInterior.Cities.Count == 0)
		{
			return false;
		}
		foreach (Army army in this.departmentOfDefense.Armies)
		{
			if (!army.IsSeafaring)
			{
				if (this.pathfindingService.FindPath(army, this.departmentOfTheInterior.Cities[0].WorldPosition, position, PathfindingManager.RequestMode.Default, null, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreKaijuGarrisons, null) == null)
				{
					return false;
				}
				return true;
			}
		}
		return false;
	}

	private bool IsWonderInConstruction(City city)
	{
		DepartmentOfIndustry agency = city.Empire.GetAgency<DepartmentOfIndustry>();
		if (agency != null)
		{
			if (agency.GetConstructionQueue(city).Get((Construction x) => x.ConstructibleElement.SubCategory == "SubCategoryVictory") != null)
			{
				return true;
			}
		}
		return false;
	}

	private float cityQualityScoreMultiplier;

	private List<City> cityToAttack;

	private Dictionary<StaticString, float> currentRegionResourceCount;

	private float distanceDeboost;

	private Dictionary<StaticString, float> empireResourceCount;

	private float geostraticScoreMultiplier;

	private IIntelligenceAIHelper intelligenceAIHelper;

	private float numberOfAlliedRegionAroundMultiplier;

	private float numberOfColdWarRegionAroundMultiplier;

	private float numberOfMyRegionAroundMultiplier;

	private float numberOfNeutralRegionAroundMultiplier;

	private float numberOfPeaceRegionAroundMultiplier;

	private float numberOfVillageDestroyedMultiplier;

	private float numberOfVillagePacifiedAndBuiltMultiplier;

	private float numberOfVillageUnpacifiedMultiplier;

	private float numberOfWarRegionAroundMultiplier;

	private List<AIRegionData> regionToAttack;

	private IDatabase<ResourceDefinition> resourceDatabase;

	private float resourceScoreMultiplier;

	private float villageScoreMultiplier;

	private IWorldAtlasAIHelper worldAtlasAIHelper;

	public static readonly StaticString TagNoWarTrait = new StaticString("FactionTraitRovingClans8");

	private static string registryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_War";

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private DepartmentOfScience departmentOfScience;

	private List<float> distanceToClosestCity;

	private IEndTurnService endTurnService;

	private IGameService gameService;

	private float ownershipBoost;

	private IPersonalityAIHelper personalityAIHelper;

	private WarInfo[] warInfoByEmpireIndex;

	private IWorldPositionningService worldPositionningService;

	private AIEmpireData myEmpireData;

	private int ObjectiveToCancel;

	private double lastTickTime;

	public Dictionary<Army, City> DefensiveArmyAssignations = new Dictionary<Army, City>();

	public List<AILayer_War.WarCityDefenseJob> warCityDefenseJobs = new List<AILayer_War.WarCityDefenseJob>();

	private IDatabase<PathfindingRule> PathfindingDataBase;

	private AILayer_Diplomacy aILayer_Diplomacy;

	private DepartmentOfDefense departmentOfDefense;

	private IPathfindingService pathfindingService;

	private DepartmentOfCreepingNodes departmentOfCreepingNodes;

	private DepartmentOfIndustry departmentOfIndustry;

	private float humanTargetBoost;

	public interface IScoreNode
	{
		float BaseValue { get; }

		float Multiplier { get; }

		string Name { get; }

		float Value { get; }
	}

	public class ScoreNode : AILayer_War.IScoreNode
	{
		public ScoreNode(string name, float value, float multiplier = 1f)
		{
			this.Name = name;
			this.BaseValue = value;
			this.Multiplier = multiplier;
		}

		public float BaseValue { get; set; }

		public float Multiplier { get; set; }

		public string Name { get; set; }

		public float Value
		{
			get
			{
				return this.BaseValue * this.Multiplier;
			}
		}
	}

	public class ScoreNodeContainer : AILayer_War.IScoreNode
	{
		public ScoreNodeContainer(string name)
		{
			this.Name = name;
			this.Multiplier = 1f;
			this.Normalizer = 1f;
			this.Boost = 0f;
			this.Children = new List<AILayer_War.IScoreNode>();
		}

		public float BaseValue
		{
			get
			{
				float num = 0f;
				for (int i = 0; i < this.Children.Count; i++)
				{
					num += this.Children[i].Value;
				}
				return num;
			}
		}

		public float Boost { get; set; }

		public List<AILayer_War.IScoreNode> Children { get; set; }

		public bool Foldout { get; set; }

		public float Multiplier { get; set; }

		public string Name { get; set; }

		public float Normalizer { get; set; }

		public float Value
		{
			get
			{
				return AILayer.Boost(this.BaseValue / this.Normalizer * this.Multiplier, this.Boost);
			}
		}

		public void UpdateChildrenNode(string nodeName, float value, float modifier)
		{
			for (int i = 0; i < this.Children.Count; i++)
			{
				if (this.Children[i].Name == nodeName)
				{
					AILayer_War.ScoreNode scoreNode = this.Children[i] as AILayer_War.ScoreNode;
					if (scoreNode != null)
					{
						scoreNode.BaseValue = value;
						scoreNode.Multiplier = modifier;
					}
					return;
				}
			}
			this.Children.Add(new AILayer_War.ScoreNode(nodeName, value, modifier));
		}
	}

	public enum WarStatusType
	{
		None,
		Preparing,
		Ready,
		War
	}

	public class WarCityDefenseJob
	{
		public WarCityDefenseJob(City city, float score = 0f)
		{
			this.AssignedArmies = new List<Army>();
			this.city = city;
			this.empireIndex = city.Empire.Index;
			this.score = score;
			this.NumberOfWantedArmies = 0;
			this.MaxArmies = 0;
			this.CityTilePositions = new List<WorldPosition>();
			this.EnemyMP = 0f;
		}

		public void Refresh()
		{
			IWorldPositionningService service = Services.GetService<IGameService>().Game.Services.GetService<IWorldPositionningService>();
			for (int i = 0; i < this.city.Districts.Count; i++)
			{
				if (District.IsACityTile(this.city.Districts[i]))
				{
					Army armyAtPosition = service.GetArmyAtPosition(this.city.Districts[i].WorldPosition);
					if (armyAtPosition == null || this.AssignedArmies.Contains(armyAtPosition))
					{
						this.CityTilePositions.Add(this.city.Districts[i].WorldPosition);
					}
				}
			}
			this.MaxArmies = this.CityTilePositions.Count - 1;
		}

		public override string ToString()
		{
			Services.GetService<IGameService>().Game.Services.GetService<IGameEntityRepositoryService>();
			if (this.city == null || !this.city.GUID.IsValid)
			{
				return string.Format("ELCP: WarCityDefenseJob Error!, City is null", new object[0]);
			}
			string text = string.Format("ELCP: {0}/{1} WarCityDefenseJob {2}/{3}/{5}/{4}, Armies:", new object[]
			{
				this.city.Empire,
				this.city.LocalizedName,
				this.MaxArmies,
				this.NeedmoreMP(),
				this.score,
				this.EnemyMP
			});
			foreach (Army army in this.AssignedArmies)
			{
				if (army == null || army.GUID == GameEntityGUID.Zero)
				{
					text = string.Format("{0} null", text);
				}
				else
				{
					text = string.Format("{0} {1}", text, army.LocalizedName);
				}
			}
			return text;
		}

		public void RemoveDisposedArmies()
		{
			for (int i = this.AssignedArmies.Count - 1; i >= 0; i--)
			{
				if (this.AssignedArmies[i] == null || this.AssignedArmies[i].GUID == GameEntityGUID.Zero)
				{
					this.AssignedArmies.RemoveAt(i);
				}
			}
		}

		public bool NeedmoreMP()
		{
			if (this.city == null || this.city.Empire == null || this.city.Empire.Index != this.empireIndex)
			{
				return false;
			}
			float num = 0f;
			num += this.city.GetPropertyValue(SimulationProperties.MilitaryPower);
			foreach (Army army in this.AssignedArmies)
			{
				if (army != null && army.GUID != GameEntityGUID.Zero)
				{
					num += army.GetPropertyValue(SimulationProperties.MilitaryPower);
				}
			}
			return num < this.EnemyMP && this.AssignedArmies.Count < this.MaxArmies;
		}

		public bool CanTrimMp()
		{
			float num = 0f;
			num += this.city.GetPropertyValue(SimulationProperties.MilitaryPower);
			for (int i = this.AssignedArmies.Count - 2; i >= 0; i--)
			{
				Army army = this.AssignedArmies[i];
				if (army != null && army.GUID != GameEntityGUID.Zero)
				{
					num += army.GetPropertyValue(SimulationProperties.MilitaryPower);
				}
			}
			return num >= this.EnemyMP;
		}

		public float score;

		public City city;

		public int MaxArmies;

		public List<WorldPosition> CityTilePositions;

		public int NumberOfWantedArmies;

		public List<Army> AssignedArmies;

		public float EnemyMP;

		private int empireIndex;
	}
}
