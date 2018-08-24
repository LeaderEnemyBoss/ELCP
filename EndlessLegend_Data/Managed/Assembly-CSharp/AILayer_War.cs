using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class AILayer_War : AILayerWithObjective, IXmlSerializable
{
	public AILayer_War() : base("War")
	{
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
			}
			else if (reader.IsStartElement("WarInfos"))
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
		for (int i = 0; i < this.cityToAttack.Count; i++)
		{
			City city = this.cityToAttack[i];
			float num2 = float.MaxValue;
			for (int j = 0; j < this.departmentOfTheInterior.Cities.Count; j++)
			{
				City city2 = this.departmentOfTheInterior.Cities[j];
				float num3 = (float)this.worldPositionningService.GetDistance(city.WorldPosition, city2.WorldPosition);
				if (num2 > num3)
				{
					num2 = num3;
				}
			}
			this.distanceToClosestCity.Add(num2);
			if (num < num2)
			{
				num = num2;
			}
		}
		num = Mathf.Max(num, 1f);
		for (int k = 0; k < this.cityToAttack.Count; k++)
		{
			City city3 = this.cityToAttack[k];
			AIRegionData regionData = this.worldAtlasAIHelper.GetRegionData(base.AIEntity.Empire.Index, city3.Region.Index);
			regionData.WarAttackScore.Reset();
			HeuristicValue operand = this.ComputeGeostrategicScore(city3.Region, this.geostraticScoreMultiplier);
			HeuristicValue operand2 = this.ComputeResourceScore(city3.Region, this.resourceScoreMultiplier);
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
			HeuristicValue operand4 = this.ComputeCityVulnerability(city3.Region);
			regionData.WarAttackScore.Multiply(operand4, "City vulnerability", new object[0]);
			HeuristicValue heuristicValue4 = new HeuristicValue(0f);
			heuristicValue4.Add(this.distanceToClosestCity[k], "Distance to closest city", new object[0]);
			heuristicValue4.Divide(num, "Maximal distance to cities", new object[0]);
			heuristicValue4.Multiply(this.distanceDeboost, "Xml factor", new object[0]);
			heuristicValue4.Multiply(-1f, "Invert the boost!", new object[0]);
			regionData.WarAttackScore.Boost(heuristicValue4, "Distance boost", new object[0]);
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
				Region region2 = this.worldPositionningService.GetRegion(agency.Armies[i].WorldPosition);
				if (region2.Index == region.Index)
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
				if (airegionData.WarAttackScore.Value > num3 && this.departmentOfForeignAffairs.IsAtWarWith(city.Empire) && !flag)
				{
					num3 = airegionData.WarAttackScore.Value;
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
							goto IL_41D;
						}
					}
					regionData = this.regionToAttack[i];
					break;
				}
				IL_41D:;
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
			if (city2 != null && this.departmentOfForeignAffairs.IsAtWarWith(city2.Empire) && !flag && num4 < 0)
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
				bool condition = region.PointOfInterests[i].PointOfInterestDefinition.TryGetValue("ResourceName", out empty);
				Diagnostics.Assert(condition);
				if (!resourceCount.ContainsKey(empty))
				{
					resourceCount.Add(empty, 0f);
				}
				StaticString key2;
				StaticString key = key2 = empty;
				float num = resourceCount[key2];
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
		this.personalityAIHelper = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		this.intelligenceAIHelper = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		this.worldAtlasAIHelper = AIScheduler.Services.GetService<IWorldAtlasAIHelper>();
		this.resourceDatabase = Databases.GetDatabase<ResourceDefinition>(false);
		IAIEmpireDataAIHelper empireDataHelper = AIScheduler.Services.GetService<IAIEmpireDataAIHelper>();
		empireDataHelper.TryGet(base.AIEntity.Empire.Index, out this.myEmpireData);
		Diagnostics.Assert(base.AIEntity.Empire != null);
		this.departmentOfForeignAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		Diagnostics.Assert(this.departmentOfForeignAffairs != null);
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		Diagnostics.Assert(this.departmentOfTheInterior != null);
		this.departmentOfScience = base.AIEntity.Empire.GetAgency<DepartmentOfScience>();
		Diagnostics.Assert(this.departmentOfScience != null);
		this.warInfoByEmpireIndex = new WarInfo[this.departmentOfForeignAffairs.DiplomaticRelations.Count];
		for (int index = 0; index < this.warInfoByEmpireIndex.Length; index++)
		{
			this.warInfoByEmpireIndex[index] = new WarInfo();
			this.warInfoByEmpireIndex[index].EnnemyEmpireIndex = index;
			this.warInfoByEmpireIndex[index].WarStatus = AILayer_War.WarStatusType.None;
		}
		base.AIEntity.RegisterPass(AIEntity.Passes.RefreshObjectives.ToString(), "AILayer_War_RefreshObjectives", new AIEntity.AIAction(this.RefreshObjectives), this, new StaticString[0]);
		this.ownershipBoost = this.personalityAIHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_War.registryPath, "OwnershipBoost"), this.ownershipBoost);
		this.InitializeRegistryValues();
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
		if (region == null || region.City == null || region.City.Empire == base.AIEntity.Empire)
		{
			return false;
		}
		if (region.City.BesiegingEmpire != null && region.City.BesiegingEmpire != base.AIEntity.Empire)
		{
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
				return false;
			}
		}
		if (this.NumberOfWar > 0)
		{
			return this.warInfoByEmpireIndex[region.City.Empire.Index].WarStatus == AILayer_War.WarStatusType.War;
		}
		return this.GetWarStatusWithEmpire(region.City.Empire.Index) != AILayer_War.WarStatusType.None;
	}

	protected override void RefreshObjectives(StaticString context, StaticString pass)
	{
		base.RefreshObjectives(context, pass);
		this.RefreshWarStates();
		this.ComputeObjectivesPriority();
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
		for (index = 0; index < this.warInfoByEmpireIndex.Length; index++)
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
						this.NumberOfWar++;
					}
					else
					{
						if (this.warInfoByEmpireIndex[index].WarBeginingTurn >= 0)
						{
							this.warInfoByEmpireIndex[index].WarBeginingTurn = -1;
						}
						WantedDiplomaticRelationStateMessage wantedDiplomaticRelationStateMessage = base.AIEntity.AIPlayer.Blackboard.FindFirst<WantedDiplomaticRelationStateMessage>(BlackboardLayerID.Empire, (WantedDiplomaticRelationStateMessage message) => message.OpponentEmpireIndex == index);
						if (wantedDiplomaticRelationStateMessage != null && wantedDiplomaticRelationStateMessage.WantedDiplomaticRelationStateName == DiplomaticRelationState.Names.War)
						{
							if (this.warInfoByEmpireIndex[index].WarStatus == AILayer_War.WarStatusType.None || this.warInfoByEmpireIndex[index].WarStatus == AILayer_War.WarStatusType.War)
							{
								this.warInfoByEmpireIndex[index].WarStatus = AILayer_War.WarStatusType.Preparing;
							}
							else if (this.warInfoByEmpireIndex[index].WarStatus == AILayer_War.WarStatusType.Ready)
							{
								this.warInfoByEmpireIndex[index].WarStatus = AILayer_War.WarStatusType.Ready;
							}
							this.NumberOfWantedWar++;
						}
						else
						{
							this.warInfoByEmpireIndex[index].WarStatus = AILayer_War.WarStatusType.None;
						}
					}
				}
			}
		}
	}

	private float cityQualityScoreMultiplier = 0.5f;

	private List<City> cityToAttack = new List<City>();

	private Dictionary<StaticString, float> currentRegionResourceCount = new Dictionary<StaticString, float>();

	private float distanceDeboost = 0.5f;

	private Dictionary<StaticString, float> empireResourceCount = new Dictionary<StaticString, float>();

	private float geostraticScoreMultiplier = 0.5f;

	private IIntelligenceAIHelper intelligenceAIHelper;

	private float numberOfAlliedRegionAroundMultiplier = 0.5f;

	private float numberOfColdWarRegionAroundMultiplier = 0.5f;

	private float numberOfMyRegionAroundMultiplier = 0.5f;

	private float numberOfNeutralRegionAroundMultiplier = 0.5f;

	private float numberOfPeaceRegionAroundMultiplier = 0.5f;

	private float numberOfVillageDestroyedMultiplier = 0.5f;

	private float numberOfVillagePacifiedAndBuiltMultiplier = 0.5f;

	private float numberOfVillageUnpacifiedMultiplier = 0.5f;

	private float numberOfWarRegionAroundMultiplier = 0.5f;

	private List<AIRegionData> regionToAttack = new List<AIRegionData>();

	private IDatabase<ResourceDefinition> resourceDatabase;

	private float resourceScoreMultiplier = 0.5f;

	private float villageScoreMultiplier = 0.5f;

	private IWorldAtlasAIHelper worldAtlasAIHelper;

	public static readonly StaticString TagNoWarTrait = new StaticString("FactionTraitRovingClans8");

	private static string registryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_War";

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private DepartmentOfScience departmentOfScience;

	private List<float> distanceToClosestCity = new List<float>();

	private IEndTurnService endTurnService;

	private IGameService gameService;

	private float ownershipBoost = 0.2f;

	private IPersonalityAIHelper personalityAIHelper;

	private WarInfo[] warInfoByEmpireIndex;

	private IWorldPositionningService worldPositionningService;

	private AIEmpireData myEmpireData;

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
}
