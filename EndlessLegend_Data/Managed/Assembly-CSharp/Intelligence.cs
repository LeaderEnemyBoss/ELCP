using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Session;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class Intelligence : AIHelper, IIntelligenceAIHelper, IService
{
	public Intelligence()
	{
		this.availableUnitDesignList = new List<UnitDesign>();
		this.availableUnitList = new List<AIData_Unit>();
		base..ctor();
	}

	public void FillAvailableUnitDesignList(global::Empire empire)
	{
		this.availableUnitDesignList.Clear();
		DepartmentOfDefense agency = empire.GetAgency<DepartmentOfDefense>();
		ReadOnlyCollection<UnitDesign> userDefinedUnitDesigns = agency.UnitDesignDatabase.UserDefinedUnitDesigns;
		for (int i = 0; i < userDefinedUnitDesigns.Count; i++)
		{
			if (!userDefinedUnitDesigns[i].CheckAgainstTag(TradableUnit.ReadOnlyMercenary))
			{
				if (!userDefinedUnitDesigns[i].CheckAgainstTag(DownloadableContent9.TagColossus))
				{
					if (!userDefinedUnitDesigns[i].CheckAgainstTag(DownloadableContent9.TagSolitary))
					{
						this.availableUnitDesignList.Add(userDefinedUnitDesigns[i]);
					}
				}
			}
		}
	}

	public ArmyPattern GenerateArmyPattern(global::Empire empire, float minArmyPower, bool perUnitTest, int timeInTurn, AIArmyPatternDefinition armyPatternDefinition = null)
	{
		int num = (int)empire.GetPropertyValue(SimulationProperties.ArmyUnitSlot);
		ArmyPattern armyPattern = new ArmyPattern();
		armyPattern.Power = minArmyPower;
		armyPattern.PerUnitTest = perUnitTest;
		armyPattern.AvailabilityTime = timeInTurn;
		if (armyPatternDefinition != null)
		{
			Array.Sort<AIArmyPatternDefinition.AIUnitDescription>(armyPatternDefinition.UnitDescriptionList, (AIArmyPatternDefinition.AIUnitDescription left, AIArmyPatternDefinition.AIUnitDescription right) => left.Priority.CompareTo(right.Priority));
			int num2 = 0;
			for (int i = 0; i < armyPatternDefinition.UnitDescriptionList.Length; i++)
			{
				AIArmyPatternDefinition.AIUnitDescription aiunitDescription = armyPatternDefinition.UnitDescriptionList[i];
				int num3 = Math.Max(aiunitDescription.MinimalQuantity, Mathf.RoundToInt(aiunitDescription.Ratio * (float)num));
				if (num3 + num2 > num)
				{
					num3 = Math.Max(0, num - num2);
				}
				num2 += num3;
				for (int j = 0; j < num3; j++)
				{
					ArmyPattern.UnitPatternCategory unitPatternCategory = new ArmyPattern.UnitPatternCategory();
					unitPatternCategory.Category = aiunitDescription.AIUnitPatternCategory;
					unitPatternCategory.Tokens = aiunitDescription.AIUnitTokens;
					armyPattern.UnitPatternCategoryList.Add(unitPatternCategory);
				}
				if (num2 >= num)
				{
					break;
				}
			}
		}
		else
		{
			for (int k = 0; k < num; k++)
			{
				ArmyPattern.UnitPatternCategory unitPatternCategory2 = new ArmyPattern.UnitPatternCategory();
				unitPatternCategory2.Category = this.defaultUnitPatternCategory;
				unitPatternCategory2.Tokens = null;
				armyPattern.UnitPatternCategoryList.Add(unitPatternCategory2);
			}
		}
		return armyPattern;
	}

	public ArmyPattern GenerateMaxPowerArmyPattern(global::Empire empire, bool perUnitTest, int timeInTurn, AIArmyPatternDefinition armyPatternDefinition = null)
	{
		ArmyPattern armyPattern = this.GenerateArmyPattern(empire, float.MaxValue, perUnitTest, timeInTurn, armyPatternDefinition);
		armyPattern.MaxPower = true;
		return armyPattern;
	}

	public UnitDesign GetBestUnitDesignForNeededCategory(DepartmentOfDefense departmentOfDefense, ArmyPattern.UnitPatternCategory neededUnitPatternCategory)
	{
		UnitDesign result = null;
		float num = -1f;
		for (int i = 0; i < this.availableUnitDesignList.Count; i++)
		{
			float unitDesignAffinityToUnitPatternCategory = this.unitPatternHelper.GetUnitDesignAffinityToUnitPatternCategory(departmentOfDefense, this.availableUnitDesignList[i], neededUnitPatternCategory.Category, neededUnitPatternCategory.Tokens);
			if (unitDesignAffinityToUnitPatternCategory >= 0f && unitDesignAffinityToUnitPatternCategory > num)
			{
				num = unitDesignAffinityToUnitPatternCategory;
				result = this.availableUnitDesignList[i];
			}
		}
		return result;
	}

	public float GetAIStrengthBelief(int empireIndex, StaticString unitBodyDefinitionName)
	{
		return this.strengthBeliefPerEmpire[empireIndex].GetStrength(unitBodyDefinitionName);
	}

	public void UpdateAIStrengthBelief(int empireIndex, StaticString unitBodyDefinitionName, float boost)
	{
		this.strengthBeliefPerEmpire[empireIndex].ApplyBoost(unitBodyDefinitionName, boost);
	}

	private void InitializeBelief(global::Game game)
	{
		int num = 0;
		for (int i = 0; i < game.Empires.Length; i++)
		{
			if (game.Empires[i] is MajorEmpire)
			{
				num++;
			}
		}
		this.strengthBeliefPerEmpire = new AIBelief_UnitBodyStrength[num];
		for (int j = 0; j < this.strengthBeliefPerEmpire.Length; j++)
		{
			this.strengthBeliefPerEmpire[j] = new AIBelief_UnitBodyStrength();
		}
	}

	private void ReleaseBelief()
	{
		this.strengthBeliefPerEmpire = null;
	}

	public override void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		base.ReadXml(reader);
		if (num < 3)
		{
			reader.Skip();
		}
		if (num >= 2)
		{
			reader.ReadStartElement("StrengthBeliefPerEmpire");
			for (int i = 0; i < this.strengthBeliefPerEmpire.Length; i++)
			{
				this.strengthBeliefPerEmpire[i] = new AIBelief_UnitBodyStrength();
				reader.ReadElementSerializable<AIBelief_UnitBodyStrength>(ref this.strengthBeliefPerEmpire[i]);
			}
			reader.ReadEndElement("StrengthBeliefPerEmpire");
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		int num = writer.WriteVersionAttribute(3);
		base.WriteXml(writer);
		if (num >= 2)
		{
			writer.WriteStartElement("StrengthBeliefPerEmpire");
			for (int i = 0; i < this.strengthBeliefPerEmpire.Length; i++)
			{
				if (this.strengthBeliefPerEmpire[i] != null)
				{
					IXmlSerializable xmlSerializable = this.strengthBeliefPerEmpire[i];
					writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
				}
			}
			writer.WriteEndElement();
		}
	}

	public int NumberOfBattleRound
	{
		get
		{
			return this.numberOfBattleRound;
		}
	}

	public void ComputeMPBasedOnBattleArea(IGarrison firstGarrison, List<IGarrison> reinforcements, int availableTile, ref float militaryPower)
	{
		int num = (int)firstGarrison.GetPropertyValue(SimulationProperties.ReinforcementPointCount);
		float additionalHealthPoint = 0f;
		City city = firstGarrison as City;
		if (city == null)
		{
			IWorldPositionable worldPositionable = firstGarrison as IWorldPositionable;
			District district = this.worldPositionningService.GetDistrict(worldPositionable.WorldPosition);
			if (district != null && district.City.Empire == firstGarrison.Empire)
			{
				city = district.City;
			}
		}
		if (city != null)
		{
			additionalHealthPoint = city.GetPropertyValue(SimulationProperties.CityDefensePoint);
		}
		int num2 = 0;
		if (firstGarrison.Hero != null)
		{
			militaryPower += this.EvaluateMilitaryPowerOfAllyUnit(firstGarrison.Hero, additionalHealthPoint);
			availableTile--;
			num2++;
		}
		int num3 = 0;
		while (availableTile > 0 && num3 < firstGarrison.StandardUnits.Count)
		{
			militaryPower += this.EvaluateMilitaryPowerOfAllyUnit(firstGarrison.StandardUnits[num3], additionalHealthPoint);
			availableTile--;
			num2++;
			num3++;
		}
		city = (firstGarrison as City);
		if (city != null)
		{
			int num4 = 0;
			while (availableTile > 0 && num4 < city.Militia.StandardUnits.Count)
			{
				militaryPower += this.EvaluateMilitaryPowerOfAllyUnit(city.Militia.StandardUnits[num4], additionalHealthPoint);
				availableTile--;
				num2++;
				num4++;
			}
		}
		militaryPower *= (float)num2;
		if (availableTile <= 0)
		{
			return;
		}
		int num5 = 0;
		int num6 = 0;
		int num7 = 0;
		while (num7 < this.numberOfBattleRound && reinforcements.Count > num5)
		{
			num2 = 0;
			float num8 = 0f;
			float num9;
			if (this.reinforcementModifierByRound != null)
			{
				num9 = this.reinforcementModifierByRound.Evaluate((float)num7);
			}
			else
			{
				num9 = 1f - (float)num7 / (float)this.numberOfBattleRound;
			}
			for (int i = 0; i < num; i++)
			{
				if (availableTile <= 0)
				{
					break;
				}
				if (num6 >= reinforcements[num5].StandardUnits.Count)
				{
					num5++;
					if (num5 >= reinforcements.Count)
					{
						break;
					}
					num6 = 0;
					if (reinforcements[num5].Hero != null)
					{
						num6 = -1;
					}
				}
				if (num6 == -1)
				{
					num8 += this.EvaluateMilitaryPowerOfAllyUnit(reinforcements[num5].Hero, additionalHealthPoint);
				}
				else
				{
					num8 += this.EvaluateMilitaryPowerOfAllyUnit(reinforcements[num5].StandardUnits[num6], additionalHealthPoint);
				}
				num6++;
				num2++;
				availableTile--;
			}
			if (num2 <= 0)
			{
				break;
			}
			num8 *= num9;
			num8 *= (float)num2;
			militaryPower += num8;
			num7++;
		}
	}

	public void EstimateMPInBattleground(Garrison attacker, Garrison defender, ref float attackerMP, ref float defenderMP)
	{
		this.EstimateMPInBattleground(attacker, WorldPosition.Invalid, defender, ref attackerMP, ref defenderMP);
	}

	public void EstimateMPInBattleground(Garrison attacker, WorldPosition attackerPosition, Garrison defender, ref float attackerMP, ref float defenderMP)
	{
		IWorldPositionable worldPositionable = attacker as IWorldPositionable;
		IWorldPositionable worldPositionable2 = defender as IWorldPositionable;
		if (worldPositionable2 == null || defender.SimulationObject == null)
		{
			return;
		}
		this.attackerReinforcement.Clear();
		this.defenderReinforcement.Clear();
		this.guidsInBattle.Clear();
		if (!attackerPosition.IsValid)
		{
			attackerPosition = worldPositionable.WorldPosition;
		}
		if (this.worldPositionningService.GetDistance(attackerPosition, worldPositionable2.WorldPosition) > 1)
		{
			WorldOrientation orientation = this.worldPositionningService.GetOrientation(worldPositionable2.WorldPosition, attackerPosition);
			attackerPosition = this.worldPositionningService.GetNeighbourTile(worldPositionable2.WorldPosition, orientation, 1);
		}
		WorldRect area = new WorldRect(attackerPosition, this.worldPositionningService.GetOrientation(attackerPosition, worldPositionable2.WorldPosition), 2, 2, 0, 3, this.worldPositionningService.World.WorldParameters);
		WorldRect worldRect = new WorldRect(worldPositionable2.WorldPosition, this.worldPositionningService.GetOrientation(attackerPosition, worldPositionable2.WorldPosition), 2, 2, 3, 0, this.worldPositionningService.World.WorldParameters);
		this.guidsInBattle.Add(attacker.GUID);
		this.guidsInBattle.Add(defender.GUID);
		int availableTile = 0;
		int availableTile2 = 0;
		this.GatherReinforcement(attacker, defender, area, ref availableTile, true);
		if (defender is City)
		{
			this.GatherReinforcementInCity(attacker.Empire, defender as City, worldRect, ref availableTile2, this.defenderReinforcement);
		}
		this.GatherReinforcement(attacker, defender, worldRect, ref availableTile2, false);
		float num = 0f;
		float num2 = 0f;
		this.ComputeMPBasedOnBattleArea(attacker, this.attackerReinforcement, availableTile, ref num);
		this.ComputeMPBasedOnBattleArea(defender, this.defenderReinforcement, availableTile2, ref num2);
		attackerMP = num;
		defenderMP = num2;
	}

	private void GatherReinforcement(Garrison attacker, Garrison defender, WorldRect area, ref int availableTile, bool attackerArea)
	{
		foreach (WorldPosition worldPosition in area.GoThroughWorldPositions(this.worldPositionningService.World.WorldParameters))
		{
			if (!this.worldPositionningService.IsWaterTile(worldPosition))
			{
				if (!this.worldPositionningService.HasRidge(worldPosition))
				{
					availableTile++;
					District district = this.worldPositionningService.GetDistrict(worldPosition);
					if (district != null && !this.guidsInBattle.Contains(district.City.GUID))
					{
						this.guidsInBattle.Add(district.City.GUID);
						if (district.City.BesiegingEmpire == null)
						{
							if (district.City.Empire == attacker.Empire)
							{
								if (district.City.UnitsCount > 0)
								{
									this.attackerReinforcement.Add(district.City);
								}
								if (district.City.Militia.UnitsCount > 0)
								{
									this.attackerReinforcement.Add(district.City.Militia);
								}
								if (attackerArea)
								{
									this.GatherReinforcementInCity(attacker.Empire, district.City, area, ref availableTile, this.attackerReinforcement);
								}
								else
								{
									this.GatherReinforcementInCity(attacker.Empire, district.City, null, ref availableTile, this.attackerReinforcement);
								}
							}
							else if (district.City.Empire == defender.Empire)
							{
								if (district.City.UnitsCount > 0)
								{
									this.defenderReinforcement.Add(district.City);
								}
								if (district.City.Militia.UnitsCount > 0)
								{
									this.defenderReinforcement.Add(district.City.Militia);
								}
								if (attackerArea)
								{
									this.GatherReinforcementInCity(attacker.Empire, district.City, null, ref availableTile, this.defenderReinforcement);
								}
								else
								{
									this.GatherReinforcementInCity(attacker.Empire, district.City, area, ref availableTile, this.defenderReinforcement);
								}
							}
						}
					}
					Army armyAtPosition = this.worldPositionningService.GetArmyAtPosition(worldPosition);
					if (armyAtPosition != null)
					{
						if (this.guidsInBattle.Contains(armyAtPosition.GUID))
						{
							break;
						}
						this.guidsInBattle.Add(armyAtPosition.GUID);
						if (armyAtPosition.Empire == attacker.Empire)
						{
							float propertyValue = armyAtPosition.GetPropertyValue(SimulationProperties.MaximumNumberOfActionPoints);
							float propertyValue2 = armyAtPosition.GetPropertyValue(SimulationProperties.ActionPointsSpent);
							float num = 1f;
							if (num <= propertyValue - propertyValue2)
							{
								this.attackerReinforcement.Add(armyAtPosition);
							}
						}
						else if (armyAtPosition.Empire == defender.Empire && (!armyAtPosition.IsCamouflaged || this.visibilityService.IsWorldPositionDetectedFor(armyAtPosition.WorldPosition, attacker.Empire)))
						{
							this.defenderReinforcement.Add(armyAtPosition);
						}
					}
				}
			}
		}
	}

	private void GatherReinforcementInCity(global::Empire attacker, City city, WorldRect battleArea, ref int availableTile, List<IGarrison> reinforcements)
	{
		for (int i = 0; i < city.Districts.Count; i++)
		{
			if (!this.worldPositionningService.IsWaterTile(city.Districts[i].WorldPosition))
			{
				Army armyAtPosition = this.worldPositionningService.GetArmyAtPosition(city.Districts[i].WorldPosition);
				if (armyAtPosition != null && armyAtPosition.Empire == city.Empire && !this.guidsInBattle.Contains(armyAtPosition.GUID))
				{
					if (armyAtPosition.Empire != attacker && armyAtPosition.IsCamouflaged && !this.visibilityService.IsWorldPositionDetectedFor(armyAtPosition.WorldPosition, attacker))
					{
						goto IL_F8;
					}
					reinforcements.Add(armyAtPosition);
					this.guidsInBattle.Add(armyAtPosition.GUID);
				}
				if (battleArea != null && !battleArea.Contains(city.Districts[i].WorldPosition, this.world.WorldParameters))
				{
					availableTile++;
				}
			}
			IL_F8:;
		}
	}

	public Intelligence.BestRecruitementCombination FillArmyPattern(int empireIndex, RequestUnitListMessage requestUnitListMessage, AILayer_Military militaryLayer)
	{
		List<AIData_Unit> obj = this.availableUnitList;
		Intelligence.BestRecruitementCombination result;
		lock (obj)
		{
			this.Recruitement_ArmiesUnits(empireIndex, requestUnitListMessage, militaryLayer);
			if (this.bestRecruitementCombination.CombinationOfArmiesUnits.Count != 0 && this.bestRecruitementCombination.GetMilitaryPower() >= 0f)
			{
				if (this.Recruitement_HeroAndFinalize(empireIndex, requestUnitListMessage))
				{
					return this.bestRecruitementCombination;
				}
			}
			result = null;
		}
		return result;
	}

	public bool IsArmyBlockedInCityUnderSiege(Army army)
	{
		District district = this.worldPositionningService.GetDistrict(army.WorldPosition);
		return district != null && District.IsACityTile(district) && district.City != null && district.City.BesiegingEmpire != null;
	}

	public void Recruitement_ArmiesUnits(int empireIndex, RequestUnitListMessage requestUnitListMessage, AILayer_Military militaryLayer)
	{
		this.bestRecruitementCombination.Reset();
		if (requestUnitListMessage.ArmyPattern.UnitPatternCategoryList.Count == 0)
		{
			return;
		}
		Diagnostics.Assert(AIScheduler.Services != null);
		global::Empire empire = base.Game.Empires[empireIndex];
		DepartmentOfDefense agency = empire.GetAgency<DepartmentOfDefense>();
		Diagnostics.Assert(agency != null);
		int maximumUnitPerArmy = (int)empire.GetPropertyValue(SimulationProperties.ArmyUnitSlot);
		this.FillAvailableGameEntitiesList(empireIndex, requestUnitListMessage, militaryLayer);
		if (this.availableGameEntities.Count == 0)
		{
			return;
		}
		float bestDistanceScore = 0f;
		for (int i = 0; i < this.availableGameEntities.Count; i++)
		{
			AIData_GameEntity aidata_GameEntity = this.availableGameEntities[i];
			float num = 4f;
			if (aidata_GameEntity.GameEntity is IPropertyEffectFeatureProvider)
			{
				num = (aidata_GameEntity.GameEntity as IPropertyEffectFeatureProvider).GetSimulationObject().GetPropertyValue(SimulationProperties.MaximumMovement);
			}
			float num2 = aidata_GameEntity.TempRecruitementUnitData.DistanceRatio / num;
			if (num2 >= this.maximalNumberOfTurn)
			{
				aidata_GameEntity.TempRecruitementUnitData.DistanceRatio = 0.05f;
			}
			else
			{
				aidata_GameEntity.TempRecruitementUnitData.DistanceRatio = 1f - num2 / this.maximalNumberOfTurn;
			}
			if (aidata_GameEntity.TempRecruitementUnitData.DistanceRatio > bestDistanceScore)
			{
				bestDistanceScore = aidata_GameEntity.TempRecruitementUnitData.DistanceRatio;
			}
			aidata_GameEntity.TempRecruitementUnitData.CalculateFinalScore();
		}
		this.availableGameEntities.Sort((AIData_GameEntity left, AIData_GameEntity right) => -1 * left.TempRecruitementUnitData.FinalScore.CompareTo(right.TempRecruitementUnitData.FinalScore));
		int unitCount = 0;
		int num3 = this.availableGameEntities.RemoveAll(delegate(AIData_GameEntity match)
		{
			if (match.TempRecruitementUnitData.DistanceRatio < 0.3f * bestDistanceScore)
			{
				return true;
			}
			if (match is AIData_Unit)
			{
				unitCount++;
				return unitCount > maximumUnitPerArmy;
			}
			return false;
		});
		if (this.availableGameEntities.Count == 0)
		{
			return;
		}
		this.CombineAvailableEntities(requestUnitListMessage, maximumUnitPerArmy);
	}

	public bool Recruitement_HeroAndFinalize(int empireIndex, RequestUnitListMessage requestUnitListMessage)
	{
		global::Empire empire = base.Game.Empires[empireIndex];
		int num = (int)empire.GetPropertyValue(SimulationProperties.ArmyUnitSlot);
		requestUnitListMessage.MissingMilitaryPower = requestUnitListMessage.ArmyPattern.Power - this.bestRecruitementCombination.GetMilitaryPower();
		requestUnitListMessage.CurrentFulfillement = this.bestRecruitementCombination.GetMilitaryPower() / requestUnitListMessage.ArmyPattern.Power;
		if (this.bestRecruitementCombination.GetMilitaryPower() >= requestUnitListMessage.ArmyPattern.Power)
		{
			return true;
		}
		requestUnitListMessage.MissingMilitaryPower = requestUnitListMessage.ArmyPattern.Power - this.bestRecruitementCombination.GetMilitaryPower();
		requestUnitListMessage.CurrentFulfillement = this.bestRecruitementCombination.GetMilitaryPower() / requestUnitListMessage.ArmyPattern.Power;
		return requestUnitListMessage.CurrentFulfillement >= requestUnitListMessage.MinimumNeededArmyFulfillement || this.bestRecruitementCombination.UsedSlots == num;
	}

	private void CombineAvailableEntities(RequestUnitListMessage requestUnitListMessage, int maximumUnitPerArmy)
	{
		int num = 0;
		if (this.selectedEntities == null)
		{
			this.selectedEntities = new int[maximumUnitPerArmy];
		}
		else if (maximumUnitPerArmy > this.selectedEntities.Length)
		{
			Array.Resize<int>(ref this.selectedEntities, maximumUnitPerArmy);
		}
		this.selectedEntities[num] = 0;
		bool flag = true;
		int count = this.availableGameEntities.Count;
		int num2 = 0;
		while (flag)
		{
			num2++;
			if (num2 > 1000)
			{
				break;
			}
			bool flag2 = true;
			int num3 = 0;
			float num4 = 0f;
			for (int i = 0; i <= num; i++)
			{
				AIData_GameEntity aidata_GameEntity = this.availableGameEntities[this.selectedEntities[i]];
				flag2 &= aidata_GameEntity.TempRecruitementUnitData.IsTransferable;
				num3 += aidata_GameEntity.TempRecruitementUnitData.UnitsNumber;
				num4 += aidata_GameEntity.TempRecruitementUnitData.MilitaryPower;
			}
			bool flag3;
			if ((num3 > maximumUnitPerArmy && num > 0) || (!flag2 && num > 0))
			{
				flag3 = false;
			}
			else
			{
				flag3 = (num3 < maximumUnitPerArmy && flag2);
				if (num4 * (float)num3 > this.bestRecruitementCombination.GetMilitaryPower())
				{
					this.bestRecruitementCombination.UsedSlots = num3;
					this.bestRecruitementCombination.SumOfMilitaryPower = num4;
					this.bestRecruitementCombination.CombinationOfArmiesUnits.Clear();
					for (int j = 0; j <= num; j++)
					{
						AIData_GameEntity item = this.availableGameEntities[this.selectedEntities[j]];
						this.bestRecruitementCombination.CombinationOfArmiesUnits.Add(item);
					}
					if (!requestUnitListMessage.ArmyPattern.MaxPower && num4 * (float)num3 > requestUnitListMessage.ArmyPattern.Power)
					{
						break;
					}
				}
			}
			if (num < maximumUnitPerArmy - 1)
			{
				if (this.selectedEntities[num] < count - 1)
				{
					if (flag3)
					{
						this.selectedEntities[num + 1] = this.selectedEntities[num] + 1;
						num++;
					}
					else
					{
						this.selectedEntities[num]++;
					}
				}
				else
				{
					flag = this.TryToGoBack(ref num, count);
				}
			}
			else if (this.selectedEntities[num] < count - 1)
			{
				this.selectedEntities[num]++;
			}
			else
			{
				flag = this.TryToGoBack(ref num, count);
			}
		}
	}

	private void FillAvailableGameEntitiesList(int empireIndex, RequestUnitListMessage requestUnitListMessage, AILayer_Military militaryLayer)
	{
		global::Empire empire = base.Game.Empires[empireIndex];
		int maximumUnitPerArmy = (int)empire.GetPropertyValue(SimulationProperties.ArmyUnitSlot);
		this.maxRecruitementDistance = 1f;
		if (requestUnitListMessage.FinalPosition.IsValid)
		{
			this.maxRecruitementDistance = 0f;
		}
		this.availableGameEntities.Clear();
		DepartmentOfDefense agency = empire.GetAgency<DepartmentOfDefense>();
		for (int i = 0; i < agency.Armies.Count; i++)
		{
			Army army = agency.Armies[i];
			if (!army.IsLocked && !army.IsInEncounter)
			{
				if (!army.HasOnlySeafaringUnits(false) && !army.HasCatspaw)
				{
					if (requestUnitListMessage.ForceSourceRegion != -1)
					{
						int regionIndex = (int)this.worldPositionningService.GetRegionIndex(army.WorldPosition);
						if (requestUnitListMessage.ForceSourceRegion != regionIndex || !this.IsArmyBlockedInCityUnderSiege(army))
						{
							goto IL_32F;
						}
					}
					else if (this.IsArmyBlockedInCityUnderSiege(army))
					{
						goto IL_32F;
					}
					AIData_Army aidata_Army;
					this.aiDataRepository.TryGetAIData<AIData_Army>(army.GUID, out aidata_Army);
					if (aidata_Army.GetArmyLockState() == AIData_Army.AIDataArmyLockState.Free)
					{
						if (requestUnitListMessage.ArmyPattern.PerUnitTest)
						{
							if (AILayer_ArmyRecruitment.GetValidArmySpawningPosition(army, this.worldPositionningService, this.pathfindingService).IsValid)
							{
								for (int j = 0; j < army.StandardUnits.Count; j++)
								{
									AIData_Unit aidata_Unit;
									if (this.aiDataRepository.TryGetAIData<AIData_Unit>(army.StandardUnits[j].GUID, out aidata_Unit) && this.PrepareRecruitementData(aidata_Unit, requestUnitListMessage, maximumUnitPerArmy, agency))
									{
										this.availableGameEntities.Add(aidata_Unit);
									}
								}
							}
						}
						else if (this.PrepareRecruitementData(aidata_Army, requestUnitListMessage, maximumUnitPerArmy, agency))
						{
							this.availableGameEntities.Add(aidata_Army);
						}
					}
					else if (aidata_Army.GetArmyLockState() == AIData_Army.AIDataArmyLockState.Locked)
					{
						if (army.StandardUnits.Count >= 1)
						{
							AIData_Unit aidata_Unit;
							if (this.aiDataRepository.TryGetAIData<AIData_Unit>(army.StandardUnits[0].GUID, out aidata_Unit))
							{
								if (aidata_Unit.ReservationExtraTag != AIData_Unit.AIDataReservationExtraTag.ArmyRecruitment)
								{
									if (!aidata_Unit.IsUnitLocked() || (!this.NearlyEqual(aidata_Unit.ReservationPriority, requestUnitListMessage.Priority) && aidata_Unit.ReservationPriority <= requestUnitListMessage.Priority))
									{
										if (requestUnitListMessage.ArmyPattern.PerUnitTest)
										{
											if (AILayer_ArmyRecruitment.GetValidArmySpawningPosition(army, this.worldPositionningService, this.pathfindingService).IsValid)
											{
												for (int k = 0; k < army.StandardUnits.Count; k++)
												{
													if (this.aiDataRepository.TryGetAIData<AIData_Unit>(army.StandardUnits[k].GUID, out aidata_Unit) && this.PrepareRecruitementData(aidata_Unit, requestUnitListMessage, maximumUnitPerArmy, agency))
													{
														this.availableGameEntities.Add(aidata_Unit);
													}
												}
											}
										}
										else if (this.PrepareRecruitementData(aidata_Army, requestUnitListMessage, maximumUnitPerArmy, agency))
										{
											this.availableGameEntities.Add(aidata_Army);
										}
									}
								}
							}
						}
					}
					else if (aidata_Army.GetArmyLockState() == AIData_Army.AIDataArmyLockState.Hybrid)
					{
					}
				}
			}
			IL_32F:;
		}
		if (empire is MajorEmpire)
		{
			DepartmentOfTheInterior agency2 = empire.GetAgency<DepartmentOfTheInterior>();
			if (agency2 != null)
			{
				for (int l = 0; l < agency2.Cities.Count; l++)
				{
					City city = agency2.Cities[l];
					if (!city.IsInEncounter)
					{
						if (requestUnitListMessage.ForceSourceRegion != -1)
						{
							if (city.Region.Index != requestUnitListMessage.ForceSourceRegion)
							{
								goto IL_54D;
							}
						}
						else if (city.BesiegingEmpire != null)
						{
							goto IL_54D;
						}
						AICommanderMission_Garrison aicommanderMission_Garrison = null;
						AIData_City aidata_City;
						if (this.aiDataRepository.TryGetAIData<AIData_City>(city.GUID, out aidata_City) && aidata_City.CommanderMission != null)
						{
							aicommanderMission_Garrison = (aidata_City.CommanderMission as AICommanderMission_Garrison);
						}
						AICommanderMission_GarrisonCamp aicommanderMission_GarrisonCamp = null;
						AIData_Camp aidata_Camp;
						if (this.aiDataRepository.TryGetAIData<AIData_Camp>(city.GUID, out aidata_Camp) && aidata_Camp.CommanderMission != null)
						{
							aicommanderMission_GarrisonCamp = (aidata_Camp.CommanderMission as AICommanderMission_GarrisonCamp);
						}
						for (int m = 0; m < city.StandardUnits.Count; m++)
						{
							AIData_Unit aidata_Unit;
							if (this.aiDataRepository.TryGetAIData<AIData_Unit>(city.StandardUnits[m].GUID, out aidata_Unit))
							{
								if (aidata_Unit.ReservationExtraTag != AIData_Unit.AIDataReservationExtraTag.ArmyRecruitment)
								{
									bool flag = aidata_Unit.Unit.UnitDesign.CheckUnitAbility(UnitAbility.ReadonlyColonize, -1);
									if (!flag && aicommanderMission_Garrison != null)
									{
										float unitPriorityInCity = aicommanderMission_Garrison.GetUnitPriorityInCity(m);
										if (this.NearlyEqual(unitPriorityInCity, requestUnitListMessage.Priority) || unitPriorityInCity > requestUnitListMessage.Priority)
										{
											goto IL_534;
										}
									}
									if (!flag && aicommanderMission_GarrisonCamp != null && aicommanderMission_GarrisonCamp.Camp != null)
									{
										float unitPriorityInCamp = aicommanderMission_GarrisonCamp.GetUnitPriorityInCamp(m);
										if (this.NearlyEqual(unitPriorityInCamp, requestUnitListMessage.Priority) || unitPriorityInCamp > requestUnitListMessage.Priority)
										{
											goto IL_534;
										}
									}
									if (this.PrepareRecruitementData(aidata_Unit, requestUnitListMessage, maximumUnitPerArmy, agency))
									{
										this.availableGameEntities.Add(aidata_Unit);
									}
								}
							}
							IL_534:;
						}
					}
					IL_54D:;
				}
			}
			if (requestUnitListMessage.ForceSourceRegion == -1)
			{
				List<Village> convertedVillages = ((MajorEmpire)empire).ConvertedVillages;
				for (int n = 0; n < convertedVillages.Count; n++)
				{
					Village village = convertedVillages[n];
					if (village != null && !village.IsInEncounter)
					{
						for (int num = 0; num < village.StandardUnits.Count; num++)
						{
							AIData_Unit aidata_Unit;
							if (this.aiDataRepository.TryGetAIData<AIData_Unit>(village.StandardUnits[num].GUID, out aidata_Unit))
							{
								if (aidata_Unit.ReservationExtraTag != AIData_Unit.AIDataReservationExtraTag.ArmyRecruitment)
								{
									float villageUnitPriority = militaryLayer.GetVillageUnitPriority(village, num);
									if (!this.NearlyEqual(villageUnitPriority, requestUnitListMessage.Priority) && villageUnitPriority <= requestUnitListMessage.Priority)
									{
										if (this.PrepareRecruitementData(aidata_Unit, requestUnitListMessage, maximumUnitPerArmy, agency))
										{
											this.availableGameEntities.Add(aidata_Unit);
										}
									}
								}
							}
						}
					}
				}
			}
		}
	}

	private bool NearlyEqual(float a, float b)
	{
		return Math.Abs(a - b) <= float.Epsilon;
	}

	private bool PrepareRecruitementData(AIData_Unit unitAIData, RequestUnitListMessage requestUnitListMessage, int maximumUnitPerArmy, DepartmentOfDefense departmentOfDefense)
	{
		if (unitAIData.Unit.UnitDesign.Tags.Contains(DownloadableContent9.TagColossus) || unitAIData.Unit.UnitDesign.Tags.Contains(DownloadableContent9.TagSolitary))
		{
			return false;
		}
		unitAIData.TempRecruitementUnitData.ModelScore = -1f;
		for (int i = 0; i < requestUnitListMessage.ArmyPattern.UnitPatternCategoryList.Count; i++)
		{
			ArmyPattern.UnitPatternCategory unitPatternCategory = requestUnitListMessage.ArmyPattern.UnitPatternCategoryList[i];
			float unitDesignAffinityToUnitPatternCategory = this.unitPatternHelper.GetUnitDesignAffinityToUnitPatternCategory(departmentOfDefense, unitAIData.Unit.UnitDesign, unitPatternCategory.Category, unitPatternCategory.Tokens);
			if (unitDesignAffinityToUnitPatternCategory > unitAIData.TempRecruitementUnitData.ModelScore)
			{
				unitAIData.TempRecruitementUnitData.ModelScore = unitDesignAffinityToUnitPatternCategory;
			}
		}
		if (unitAIData.TempRecruitementUnitData.ModelScore < 0f)
		{
			unitAIData.TempRecruitementUnitData.FinalScore = -1f;
			return false;
		}
		unitAIData.TempRecruitementUnitData.PriorityDifference = 1f;
		unitAIData.TempRecruitementUnitData.PriorityDifference = (requestUnitListMessage.Priority - unitAIData.ReservationPriority) / requestUnitListMessage.Priority;
		unitAIData.TempRecruitementUnitData.MilitaryPower = this.EvaluateMilitaryPowerOfAllyUnit(unitAIData.Unit, 0f);
		unitAIData.TempRecruitementUnitData.MilitaryPowerPercentage = 1f;
		if (!requestUnitListMessage.ArmyPattern.MaxPower)
		{
			if (unitAIData.TempRecruitementUnitData.MilitaryPower <= requestUnitListMessage.ArmyPattern.Power)
			{
				unitAIData.TempRecruitementUnitData.MilitaryPowerPercentage = unitAIData.TempRecruitementUnitData.MilitaryPower / requestUnitListMessage.ArmyPattern.Power;
			}
			else
			{
				unitAIData.TempRecruitementUnitData.MilitaryPowerPercentage = 1f;
			}
		}
		unitAIData.TempRecruitementUnitData.UnitsNumber = 1;
		unitAIData.TempRecruitementUnitData.UnitsNumberPercentage = (float)unitAIData.TempRecruitementUnitData.UnitsNumber / (float)maximumUnitPerArmy;
		if (unitAIData.Unit.Garrison is Army)
		{
			unitAIData.TempRecruitementUnitData.CurrentPosition = (unitAIData.Unit.Garrison as Army).WorldPosition;
		}
		else if (unitAIData.Unit.Garrison is City)
		{
			unitAIData.TempRecruitementUnitData.CurrentPosition = (unitAIData.Unit.Garrison as City).WorldPosition;
		}
		else if (unitAIData.Unit.Garrison is Village)
		{
			unitAIData.TempRecruitementUnitData.CurrentPosition = (unitAIData.Unit.Garrison as Village).WorldPosition;
		}
		else
		{
			Diagnostics.LogError("[AILayer_ArmyRecruitment] Unknown garrison type {0}", new object[]
			{
				unitAIData.Unit.Garrison.GetType().ToString()
			});
		}
		if (requestUnitListMessage.FinalPosition.IsValid)
		{
			unitAIData.TempRecruitementUnitData.DistanceRatio = (float)this.worldPositionningService.GetDistance(requestUnitListMessage.FinalPosition, unitAIData.TempRecruitementUnitData.CurrentPosition);
			if (unitAIData.TempRecruitementUnitData.DistanceRatio > this.maxRecruitementDistance)
			{
				this.maxRecruitementDistance = unitAIData.TempRecruitementUnitData.DistanceRatio;
			}
		}
		else
		{
			unitAIData.TempRecruitementUnitData.DistanceRatio = 1f;
		}
		return true;
	}

	private bool PrepareRecruitementData(AIData_Army armyAIData, RequestUnitListMessage requestUnitListMessage, int maximumUnitPerArmy, DepartmentOfDefense departmentOfDefense)
	{
		armyAIData.TempRecruitementUnitData.Reset();
		AIData_Unit aidata_Unit;
		for (int i = 0; i < armyAIData.Army.StandardUnits.Count; i++)
		{
			if (this.aiDataRepository.TryGetAIData<AIData_Unit>(armyAIData.Army.StandardUnits[i].GUID, out aidata_Unit))
			{
				if (aidata_Unit.Unit.UnitDesign.Tags.Contains(DownloadableContent9.TagColossus) || aidata_Unit.Unit.UnitDesign.Tags.Contains(DownloadableContent9.TagSolitary))
				{
					aidata_Unit.TempRecruitementUnitData.IsTransferable = false;
					armyAIData.TempRecruitementUnitData.IsTransferable = false;
					return false;
				}
				float num = -1f;
				for (int j = 0; j < requestUnitListMessage.ArmyPattern.UnitPatternCategoryList.Count; j++)
				{
					ArmyPattern.UnitPatternCategory unitPatternCategory = requestUnitListMessage.ArmyPattern.UnitPatternCategoryList[j];
					float unitDesignAffinityToUnitPatternCategory = this.unitPatternHelper.GetUnitDesignAffinityToUnitPatternCategory(departmentOfDefense, aidata_Unit.Unit.UnitDesign, unitPatternCategory.Category, unitPatternCategory.Tokens);
					if (unitDesignAffinityToUnitPatternCategory > num)
					{
						num = unitDesignAffinityToUnitPatternCategory;
					}
				}
				armyAIData.TempRecruitementUnitData.ModelScore = Math.Max(armyAIData.TempRecruitementUnitData.ModelScore, num);
			}
		}
		if (armyAIData.TempRecruitementUnitData.ModelScore < 0f)
		{
			armyAIData.TempRecruitementUnitData.FinalScore = -1f;
			return false;
		}
		armyAIData.TempRecruitementUnitData.PriorityDifference = 1f;
		if (armyAIData.Army.StandardUnits.Count >= 1 && this.aiDataRepository.TryGetAIData<AIData_Unit>(armyAIData.Army.StandardUnits[0].GUID, out aidata_Unit))
		{
			armyAIData.TempRecruitementUnitData.PriorityDifference = (requestUnitListMessage.Priority - aidata_Unit.ReservationPriority) / requestUnitListMessage.Priority;
		}
		armyAIData.TempRecruitementUnitData.MilitaryPower = 0f;
		for (int k = 0; k < armyAIData.Army.StandardUnits.Count; k++)
		{
			if (this.aiDataRepository.TryGetAIData<AIData_Unit>(armyAIData.Army.StandardUnits[k].GUID, out aidata_Unit))
			{
				armyAIData.TempRecruitementUnitData.MilitaryPower += this.EvaluateMilitaryPowerOfAllyUnit(aidata_Unit.Unit, 0f);
			}
		}
		if (armyAIData.Army.Hero != null)
		{
			armyAIData.TempRecruitementUnitData.MilitaryPower += this.EvaluateMilitaryPowerOfAllyUnit(armyAIData.Army.Hero, 0f);
		}
		armyAIData.TempRecruitementUnitData.MilitaryPowerPercentage = 1f;
		if (!requestUnitListMessage.ArmyPattern.MaxPower)
		{
			if (armyAIData.TempRecruitementUnitData.MilitaryPower <= requestUnitListMessage.ArmyPattern.Power)
			{
				armyAIData.TempRecruitementUnitData.MilitaryPowerPercentage = armyAIData.TempRecruitementUnitData.MilitaryPower / requestUnitListMessage.ArmyPattern.Power;
			}
			else
			{
				armyAIData.TempRecruitementUnitData.MilitaryPowerPercentage = 1f;
			}
		}
		armyAIData.TempRecruitementUnitData.UnitsNumber = armyAIData.Army.StandardUnits.Count;
		armyAIData.TempRecruitementUnitData.UnitsNumberPercentage = (float)armyAIData.TempRecruitementUnitData.UnitsNumber / (float)maximumUnitPerArmy;
		armyAIData.TempRecruitementUnitData.CurrentPosition = armyAIData.Army.WorldPosition;
		if (requestUnitListMessage.FinalPosition.IsValid)
		{
			armyAIData.TempRecruitementUnitData.DistanceRatio = (float)this.worldPositionningService.GetDistance(requestUnitListMessage.FinalPosition, armyAIData.TempRecruitementUnitData.CurrentPosition);
			if (armyAIData.TempRecruitementUnitData.DistanceRatio > this.maxRecruitementDistance)
			{
				this.maxRecruitementDistance = armyAIData.TempRecruitementUnitData.DistanceRatio;
			}
		}
		else
		{
			armyAIData.TempRecruitementUnitData.DistanceRatio = 1f;
		}
		return true;
	}

	private bool TryToGoBack(ref int currentSlot, int unitSize)
	{
		for (;;)
		{
			currentSlot--;
			if (currentSlot < 0)
			{
				break;
			}
			this.selectedEntities[currentSlot]++;
			if (this.selectedEntities[currentSlot] < unitSize)
			{
				return true;
			}
		}
		return false;
	}

	public static IEnumerable<Army> GetArmiesInRegion(int regionIndex)
	{
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		IWorldPositionningService worldPositionningService = gameService.Game.Services.GetService<IWorldPositionningService>();
		Region region = worldPositionningService.GetRegion(regionIndex);
		for (int indexTile = 0; indexTile < region.WorldPositions.Length; indexTile++)
		{
			if (!worldPositionningService.IsWaterTile(region.WorldPositions[indexTile]))
			{
				Army army = worldPositionningService.GetArmyAtPosition(region.WorldPositions[indexTile]);
				if (army != null)
				{
					yield return army;
				}
			}
		}
		yield break;
	}

	public static IEnumerable<Army> GetVisibleArmiesInRegion(int regionIndex, global::Empire referenceEmpire)
	{
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		IVisibilityService visibilityService = gameService.Game.Services.GetService<IVisibilityService>();
		foreach (Army army in Intelligence.GetArmiesInRegion(regionIndex))
		{
			if (!army.IsCamouflaged || visibilityService.IsWorldPositionDetectedFor(army.WorldPosition, referenceEmpire) || army.IsPillaging)
			{
				yield return army;
			}
		}
		yield break;
	}

	public float EvaluateMajorFactionMaxMilitaryPowerOnRegion(global::Empire empire, int regionIndex)
	{
		DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
		float num = 0f;
		foreach (Army army in Intelligence.GetVisibleArmiesInRegion(regionIndex, empire))
		{
			if (army.Empire != empire && army.Empire is MajorEmpire)
			{
				float num2 = army.GetPropertyValue(SimulationProperties.MilitaryPower);
				DiplomaticRelation diplomaticRelation = agency.DiplomaticRelations[army.Empire.Index];
				if (diplomaticRelation.State != null)
				{
					if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.ColdWar)
					{
						num2 *= 0.5f;
					}
					else if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.War)
					{
						num2 *= 1f;
					}
					else
					{
						num2 = 0f;
					}
					if (num2 > num)
					{
						num = num2;
					}
				}
			}
		}
		return num;
	}

	public float EvaluateMaxMilitaryPowerOfRegion(global::Empire empire, int regionIndex)
	{
		float val = this.EvaluateMinorFactionMaxMilitaryPower(empire, regionIndex);
		float val2 = this.EvaluateMajorFactionMaxMilitaryPowerOnRegion(empire, regionIndex);
		return Math.Max(val, val2);
	}

	public float EvaluateMilitaryPowerOfAllyUnit(Unit unit, float additionalHealthPoint = 0f)
	{
		float num = unit.GetPropertyValue(SimulationProperties.MilitaryPower);
		if (additionalHealthPoint > 0f)
		{
			float propertyValue = unit.GetPropertyValue(SimulationProperties.Health);
			if (propertyValue > 0f)
			{
				num *= propertyValue + additionalHealthPoint;
				num /= propertyValue;
			}
		}
		return num;
	}

	public float EvaluateMilitaryPowerOfBesieger(global::Empire empire, int regionIndex)
	{
		float num = 0f;
		Diagnostics.Assert(regionIndex >= 0 && regionIndex < this.world.Regions.Length);
		Region region = this.world.Regions[regionIndex];
		if (region == null || region.City == null)
		{
			return num;
		}
		for (int i = 0; i < region.City.Districts.Count; i++)
		{
			District district = region.City.Districts[i];
			if (!this.worldPositionningService.IsWaterTile(district.WorldPosition))
			{
				if (district.Type == DistrictType.Exploitation)
				{
					Army armyAtPosition = this.worldPositionningService.GetArmyAtPosition(district.WorldPosition);
					if (armyAtPosition != null && armyAtPosition.Empire.Index == region.City.BesiegingEmpireIndex)
					{
						num += this.EvaluateMilitaryPowerOfGarrison(empire, armyAtPosition, 0);
					}
				}
			}
		}
		return num;
	}

	[Obsolete]
	public float EvaluateMilitaryPowerOfEmpireBestArmy(global::Empire myEmpire, global::Empire otherEmpire)
	{
		float num = 0f;
		if (otherEmpire is MinorEmpire)
		{
			DepartmentOfDefense agency = otherEmpire.GetAgency<DepartmentOfDefense>();
			for (int i = 0; i < agency.Armies.Count; i++)
			{
				float num2 = this.EvaluateMilitaryPowerOfGarrison(myEmpire, agency.Armies[i], 0);
				if (num2 > num)
				{
					num = num2;
				}
			}
			BarbarianCouncil agency2 = otherEmpire.GetAgency<BarbarianCouncil>();
			for (int j = 0; j < agency2.Villages.Count; j++)
			{
				if (!agency2.Villages[j].HasBeenPacified)
				{
					float num3 = this.EvaluateMilitaryPowerOfGarrison(myEmpire, agency2.Villages[j], 0);
					if (num3 > num)
					{
						num = num3;
					}
				}
			}
		}
		else if (otherEmpire is MajorEmpire)
		{
			this.tempUnitsMilitaryPower.Clear();
			int val = (int)otherEmpire.GetPropertyValue(SimulationProperties.ArmyUnitSlot);
			float num4 = 0f;
			DepartmentOfTheInterior agency3 = otherEmpire.GetAgency<DepartmentOfTheInterior>();
			for (int k = 0; k < agency3.Cities.Count; k++)
			{
				City city = agency3.Cities[k];
				if (city.Hero != null)
				{
					float num5 = (myEmpire.Index != otherEmpire.Index) ? this.EvaluateMilitaryPowerOfOpponentUnit(myEmpire, city.Hero, 0f) : this.EvaluateMilitaryPowerOfAllyUnit(city.Hero, 0f);
					if (num5 > num4)
					{
						num4 = num5;
					}
				}
			}
			DepartmentOfDefense agency4 = otherEmpire.GetAgency<DepartmentOfDefense>();
			for (int l = 0; l < agency4.Armies.Count; l++)
			{
				Army army = agency4.Armies[l];
				if (army.Hero != null)
				{
					float num6 = (myEmpire.Index != otherEmpire.Index) ? this.EvaluateMilitaryPowerOfOpponentUnit(myEmpire, army.Hero, 0f) : this.EvaluateMilitaryPowerOfAllyUnit(army.Hero, 0f);
					if (num6 > num4)
					{
						num4 = num6;
					}
				}
			}
			for (int m = 0; m < agency3.Cities.Count; m++)
			{
				City city2 = agency3.Cities[m];
				for (int n = 0; n < city2.StandardUnits.Count; n++)
				{
					Unit unit = city2.StandardUnits[n];
					float item = (myEmpire.Index != otherEmpire.Index) ? this.EvaluateMilitaryPowerOfOpponentUnit(myEmpire, unit, 0f) : this.EvaluateMilitaryPowerOfAllyUnit(unit, 0f);
					this.tempUnitsMilitaryPower.Add(item);
				}
			}
			for (int num7 = 0; num7 < agency4.Armies.Count; num7++)
			{
				Army army2 = agency4.Armies[num7];
				for (int num8 = 0; num8 < army2.StandardUnits.Count; num8++)
				{
					Unit unit2 = army2.StandardUnits[num8];
					float item2 = (myEmpire.Index != otherEmpire.Index) ? this.EvaluateMilitaryPowerOfOpponentUnit(myEmpire, unit2, 0f) : this.EvaluateMilitaryPowerOfAllyUnit(unit2, 0f);
					this.tempUnitsMilitaryPower.Add(item2);
				}
			}
			this.tempUnitsMilitaryPower.Sort((float a, float b) => -1 * a.CompareTo(b));
			int num9 = Math.Min(val, this.tempUnitsMilitaryPower.Count);
			for (int num10 = 0; num10 < num9; num10++)
			{
				num += this.tempUnitsMilitaryPower[num10];
			}
			if (num4 > 0f)
			{
				num += num4;
				num *= (float)(num9 + 1);
			}
			else
			{
				num *= (float)num9;
			}
		}
		return num;
	}

	public float EvaluateMilitaryPowerOfGarrison(global::Empire empire, Garrison garrison, int turnCount = 0)
	{
		return this.EvaluateMilitaryPowerOfGarrison(empire, garrison, turnCount, null);
	}

	public float EvaluateMilitaryPowerOfKaiju(global::Empire empire, Kaiju kaiju, int turnCount = 0)
	{
		float num = 0f;
		if (kaiju == null)
		{
			return num;
		}
		Garrison activeTroops = kaiju.GetActiveTroops();
		if (kaiju.OnGarrisonMode())
		{
			foreach (Unit unit in activeTroops.Units)
			{
				num += this.EvaluateMilitaryPowerOfOpponentUnit(empire, unit, 0f);
			}
			if (kaiju.IsTamed())
			{
				num += this.EvaluateMajorFactionMaxMilitaryPowerOnRegion(kaiju.Empire, kaiju.Region.Index);
			}
		}
		return num;
	}

	public float EvaluateMilitaryPowerOfOpponentEmpireBestUnit(global::Empire myEmpire, global::Empire otherEmpire)
	{
		float num = 0f;
		DepartmentOfDefense agency = otherEmpire.GetAgency<DepartmentOfDefense>();
		for (int i = 0; i < agency.Armies.Count; i++)
		{
			for (int j = 0; j < agency.Armies[i].StandardUnits.Count; j++)
			{
				float num2 = this.EvaluateMilitaryPowerOfOpponentUnit(myEmpire, agency.Armies[i].StandardUnits[j], 0f);
				if (num2 > num)
				{
					num = num2;
				}
			}
		}
		if (otherEmpire is MinorEmpire)
		{
			BarbarianCouncil agency2 = otherEmpire.GetAgency<BarbarianCouncil>();
			for (int k = 0; k < agency2.Villages.Count; k++)
			{
				for (int l = 0; l < agency2.Villages[k].StandardUnits.Count; l++)
				{
					float num3 = this.EvaluateMilitaryPowerOfOpponentUnit(myEmpire, agency2.Villages[k].StandardUnits[l], 0f);
					if (num3 > num)
					{
						num = num3;
					}
				}
			}
		}
		if (otherEmpire is MajorEmpire)
		{
			DepartmentOfTheInterior agency3 = otherEmpire.GetAgency<DepartmentOfTheInterior>();
			for (int m = 0; m < agency3.Cities.Count; m++)
			{
				for (int n = 0; n < agency3.Cities[m].StandardUnits.Count; n++)
				{
					float num4 = this.EvaluateMilitaryPowerOfOpponentUnit(myEmpire, agency3.Cities[m].StandardUnits[n], 0f);
					if (num4 > num)
					{
						num = num4;
					}
				}
			}
		}
		return num;
	}

	public float EvaluateMilitaryPowerOfOpponentUnit(global::Empire empire, Unit unit, float additionalHealthPoint = 0f)
	{
		if (empire.Index >= this.strengthBeliefPerEmpire.Length)
		{
			AILayer.LogError("[SCORING] Trying to evaluate a unit's military power from the perspective of a non major empire");
			return unit.GetPropertyValue(SimulationProperties.MilitaryPower);
		}
		float num = this.strengthBeliefPerEmpire[empire.Index].GetStrength(unit.UnitDesign.UnitBodyDefinition.Name) * 2f - 1f;
		float num2 = unit.GetPropertyValue(SimulationProperties.MilitaryPower);
		if (additionalHealthPoint > 0f)
		{
			float propertyValue = unit.GetPropertyValue(SimulationProperties.Health);
			if (propertyValue > 0f)
			{
				num2 *= propertyValue + additionalHealthPoint;
				num2 /= propertyValue;
			}
		}
		return num2 + num2 * num;
	}

	public float EvaluateMinorFactionMaxArmyPower(global::Empire empire, int regionIndex)
	{
		Region region = this.world.Regions[regionIndex];
		float num = 0f;
		if (region.MinorEmpire != null)
		{
			DepartmentOfDefense agency = region.MinorEmpire.GetAgency<DepartmentOfDefense>();
			for (int i = 0; i < agency.Armies.Count; i++)
			{
				float num2 = this.EvaluateMilitaryPowerOfGarrison(empire, agency.Armies[i], 0);
				if (num2 > num)
				{
					num = num2;
				}
			}
		}
		return num;
	}

	public float EvaluateMinorFactionMaxArmyPowerInArea(global::Empire empire, Region region)
	{
		Diagnostics.Assert(AIScheduler.Services != null);
		IWorldAtlasAIHelper worldAtlasHelper = AIScheduler.Services.GetService<IWorldAtlasAIHelper>();
		Diagnostics.Assert(worldAtlasHelper != null);
		IEntityInfoAIHelper service = AIScheduler.Services.GetService<IEntityInfoAIHelper>();
		Diagnostics.Assert(service != null);
		List<Region> list = new List<Region>();
		worldAtlasHelper.ComputeNeighbourRegions(region, ref list);
		list.Add(region);
		list.RemoveAll((Region match) => worldAtlasHelper.IsRegionPacified(empire, match));
		float num = 0f;
		for (int i = 0; i < list.Count; i++)
		{
			if (service.IsMinorFactionAllowedToAttackArmy(list[i]))
			{
				num += this.EvaluateMinorFactionMaxArmyPower(empire, list[i].Index);
			}
		}
		return num;
	}

	public float EvaluateMinorFactionMaxMilitaryPower(global::Empire empire, int regionIndex)
	{
		Diagnostics.Assert(regionIndex >= 0 && regionIndex < this.world.Regions.Length);
		Region region = this.world.Regions[regionIndex];
		float num = 0f;
		if (region.MinorEmpire != null)
		{
			BarbarianCouncil agency = region.MinorEmpire.GetAgency<BarbarianCouncil>();
			for (int i = 0; i < agency.Villages.Count; i++)
			{
				if (!agency.Villages[i].HasBeenPacified)
				{
					float num2 = this.EvaluateMilitaryPowerOfGarrison(empire, agency.Villages[i], 0);
					if (num2 > num)
					{
						num = num2;
					}
				}
			}
			Math.Max(num, this.EvaluateMinorFactionMaxArmyPower(empire, regionIndex));
		}
		return num;
	}

	public float EvaluateNeedOfShipTechnology(global::Empire empire, float ratioOfExplorationToReach)
	{
		Diagnostics.Assert(AIScheduler.Services != null);
		IWorldAtlasAIHelper service = AIScheduler.Services.GetService<IWorldAtlasAIHelper>();
		Diagnostics.Assert(service != null);
		int num = 0;
		int num2 = 0;
		if (this.world.IsMultiContinent())
		{
			DepartmentOfTheInterior agency = empire.GetAgency<DepartmentOfTheInterior>();
			if (agency.Cities.Count >= 1)
			{
				int continentID = agency.Cities[0].Region.ContinentID;
				num = service.GetRemainingRegionToExploreInContinent(empire, continentID, ratioOfExplorationToReach);
				num2 = service.GetRemainingFreeRegionInContinent(empire, continentID);
			}
			return Math.Max(1f, Math.Max((float)(2 - num), (float)(5 - num2)));
		}
		return -1f;
	}

	public override IEnumerator Initialize(IServiceContainer serviceContainer, global::Game game)
	{
		yield return base.Initialize(serviceContainer, game);
		serviceContainer.AddService<IIntelligenceAIHelper>(this);
		this.world = game.World;
		Diagnostics.Assert(this.world != null);
		this.endTurnService = Services.GetService<IEndTurnService>();
		if (this.endTurnService == null)
		{
			Diagnostics.LogError("Failed to retrieve the end turn service.");
		}
		this.defaultUnitPatternCategory = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<string>(EntityInfoAIHelper.RegistryPath + "DefaultUnitPatternCategory", this.defaultUnitPatternCategory);
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		this.worldPositionningService = gameService.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.pathfindingService = gameService.Game.Services.GetService<IPathfindingService>();
		Diagnostics.Assert(this.pathfindingService != null);
		this.visibilityService = gameService.Game.Services.GetService<IVisibilityService>();
		this.animationCurveDatabase = Databases.GetDatabase<Amplitude.Unity.Framework.AnimationCurve>(false);
		yield return base.BindService<IAIDataRepositoryAIHelper>(serviceContainer, delegate(IAIDataRepositoryAIHelper service)
		{
			this.aiDataRepository = service;
		});
		yield return base.BindService<IUnitPatternAIHelper>(serviceContainer, delegate(IUnitPatternAIHelper service)
		{
			this.unitPatternHelper = service;
		});
		this.InitializeBelief(game);
		IDatabase<BattleSequence> battleSequenceDatabase = Databases.GetDatabase<BattleSequence>(false);
		Diagnostics.Assert(battleSequenceDatabase != null);
		ISessionService sessionService = Services.GetService<ISessionService>();
		Diagnostics.Assert(sessionService != null);
		string sequenceName = sessionService.Session.GetLobbyData<string>("EncounterSequence", BattleEncounter.DefaultBattleSequenceName);
		BattleSequence battleSequence;
		if (battleSequenceDatabase.TryGetValue(sequenceName, out battleSequence))
		{
			for (int phaseIndex = 0; phaseIndex < battleSequence.BattlePhases.Length; phaseIndex++)
			{
				BattlePhase currentBattlePhase = battleSequence.BattlePhases[phaseIndex];
				if (currentBattlePhase.Simulation != null)
				{
					this.numberOfBattleRound += currentBattlePhase.NumberOfRounds;
				}
			}
		}
		string reinforcementModifierByRoundName = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<string>(string.Format("{0}/{1}", "AI/AIHelpers/Intelligence", "ReinforcementModifierByRound"), string.Empty);
		if (!string.IsNullOrEmpty(reinforcementModifierByRoundName))
		{
			this.reinforcementModifierByRound = this.animationCurveDatabase.GetValue(reinforcementModifierByRoundName);
		}
		yield break;
	}

	public bool IsContinentAcccessible(global::Empire empire, int regionIndex)
	{
		Region region = this.worldPositionningService.GetRegion(regionIndex);
		if (region.IsWasteland)
		{
			return false;
		}
		DepartmentOfScience agency = empire.GetAgency<DepartmentOfScience>();
		if (agency.HaveResearchedShipTechnology())
		{
			return true;
		}
		if (region.IsOcean)
		{
			return false;
		}
		DepartmentOfTheInterior agency2 = empire.GetAgency<DepartmentOfTheInterior>();
		if (agency2.Cities.Count >= 1)
		{
			int continentID = agency2.Cities[0].Region.ContinentID;
			return continentID == this.world.Regions[regionIndex].ContinentID;
		}
		return false;
	}

	public override IEnumerator Load(global::Game game)
	{
		yield return base.Load(game);
		yield break;
	}

	public override void Release()
	{
		this.world = null;
		this.aiDataRepository = null;
		this.ReleaseBelief();
		base.Release();
	}

	public bool TryGetListOfRegionToColonize(global::Empire empire, float explorationRatioToReach, ref List<Region> listOfRegionToColonize)
	{
		Diagnostics.Assert(AIScheduler.Services != null);
		IWorldAtlasAIHelper service = AIScheduler.Services.GetService<IWorldAtlasAIHelper>();
		Diagnostics.Assert(service != null);
		List<int> list = new List<int>();
		service.ComputeConnectedRegionNotColonized(empire, explorationRatioToReach, ref list);
		if (list.Count == 0)
		{
			return false;
		}
		list.RemoveAll((int match) => !this.IsContinentAcccessible(empire, match));
		IGameService service2 = Services.GetService<IGameService>();
		Diagnostics.Assert(service2 != null);
		IWorldPositionningService service3 = service2.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(service3 != null);
		for (int i = 0; i < list.Count; i++)
		{
			Region item = service3.World.Regions[list[i]];
			listOfRegionToColonize.Add(item);
		}
		return listOfRegionToColonize.Count != 0;
	}

	public bool TryGetListOfRegionToExplore(global::Empire empire, float explorationRatioToReach, ref List<Region> listOfRegionToExplore)
	{
		Diagnostics.Assert(AIScheduler.Services != null);
		IWorldAtlasAIHelper service = AIScheduler.Services.GetService<IWorldAtlasAIHelper>();
		Diagnostics.Assert(service != null);
		List<int> list = new List<int>();
		service.ComputeConnectedRegionNotExplored(empire, explorationRatioToReach, ref list);
		if (list.Count == 0)
		{
			return false;
		}
		list.RemoveAll((int match) => !this.IsContinentAcccessible(empire, match));
		IGameService service2 = Services.GetService<IGameService>();
		Diagnostics.Assert(service2 != null);
		IWorldPositionningService service3 = service2.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(service3 != null);
		for (int i = 0; i < list.Count; i++)
		{
			Region item = service3.World.Regions[list[i]];
			listOfRegionToExplore.Add(item);
		}
		return listOfRegionToExplore.Count != 0;
	}

	private float EvaluateMilitaryPowerOfGarrison(global::Empire empire, Garrison garrison, int turnCount, List<GameEntityGUID> usedGuids)
	{
		float num = 0f;
		int num2 = 0;
		float num3 = 0f;
		if (garrison is City)
		{
			City city = garrison as City;
			num3 = city.GetPropertyValue(SimulationProperties.CityDefensePoint);
			if (turnCount > 0)
			{
				if (city.BesiegingEmpireIndex >= 0)
				{
					float propertyValue = city.GetPropertyValue(SimulationProperties.CityDefensePointLossPerTurn);
					num3 = Math.Max(0f, num3 - propertyValue * (float)turnCount);
				}
				else
				{
					float propertyValue2 = city.GetPropertyValue(SimulationProperties.CityDefensePointRecoveryPerTurn);
					float propertyValue3 = city.GetPropertyValue(SimulationProperties.MaximumCityDefensePoint);
					num3 = Math.Min(propertyValue3, num3 + propertyValue2 * (float)turnCount);
				}
			}
			foreach (Unit unit in city.Militia.StandardUnits)
			{
				num2++;
				if (garrison.Empire == empire || !(empire is MajorEmpire))
				{
					num += this.EvaluateMilitaryPowerOfAllyUnit(unit, num3);
				}
				else
				{
					num += this.EvaluateMilitaryPowerOfOpponentUnit(empire, unit, num3);
				}
			}
			for (int i = 0; i < city.Districts.Count; i++)
			{
				if (District.IsACityTile(city.Districts[i]))
				{
					Army armyAtPosition = this.worldPositionningService.GetArmyAtPosition(city.Districts[i].WorldPosition);
					if (armyAtPosition != null && armyAtPosition.Empire == garrison.Empire)
					{
						if (usedGuids != null)
						{
							if (usedGuids.Contains(armyAtPosition.GUID))
							{
								goto IL_20B;
							}
							usedGuids.Add(armyAtPosition.GUID);
						}
						foreach (Unit unit2 in armyAtPosition.StandardUnits)
						{
							if (unit2 != null)
							{
								num2++;
								if (garrison.Empire == empire || !(empire is MajorEmpire))
								{
									num += this.EvaluateMilitaryPowerOfAllyUnit(unit2, num3);
								}
								else
								{
									num += this.EvaluateMilitaryPowerOfOpponentUnit(empire, unit2, num3);
								}
							}
						}
					}
				}
				IL_20B:;
			}
		}
		if (garrison is Camp)
		{
			Camp camp = garrison as Camp;
			foreach (Unit unit3 in camp.StandardUnits)
			{
				num2++;
				if (garrison.Empire == empire || !(empire is MajorEmpire))
				{
					num += this.EvaluateMilitaryPowerOfAllyUnit(unit3, num3);
				}
				else
				{
					num += this.EvaluateMilitaryPowerOfOpponentUnit(empire, unit3, num3);
				}
			}
			for (int j = 0; j < camp.Districts.Count; j++)
			{
				Army armyAtPosition2 = this.worldPositionningService.GetArmyAtPosition(camp.Districts[j].WorldPosition);
				if (armyAtPosition2 != null && armyAtPosition2.Empire == garrison.Empire)
				{
					if (usedGuids != null)
					{
						if (usedGuids.Contains(armyAtPosition2.GUID))
						{
							goto IL_396;
						}
						usedGuids.Add(armyAtPosition2.GUID);
					}
					foreach (Unit unit4 in armyAtPosition2.StandardUnits)
					{
						if (unit4 != null)
						{
							num2++;
							if (garrison.Empire == empire || !(empire is MajorEmpire))
							{
								num += this.EvaluateMilitaryPowerOfAllyUnit(unit4, num3);
							}
							else
							{
								num += this.EvaluateMilitaryPowerOfOpponentUnit(empire, unit4, num3);
							}
						}
					}
				}
				IL_396:;
			}
		}
		if (garrison.Empire == empire || !(empire is MajorEmpire))
		{
			if (garrison.Hero != null)
			{
				num2++;
				num += this.EvaluateMilitaryPowerOfAllyUnit(garrison.Hero, num3);
			}
			foreach (Unit unit5 in garrison.StandardUnits)
			{
				num2++;
				num += this.EvaluateMilitaryPowerOfAllyUnit(unit5, num3);
			}
		}
		else
		{
			if (garrison.Hero != null)
			{
				num2++;
				num += this.EvaluateMilitaryPowerOfOpponentUnit(empire, garrison.Hero, num3);
			}
			foreach (Unit unit6 in garrison.StandardUnits)
			{
				num2++;
				num += this.EvaluateMilitaryPowerOfOpponentUnit(empire, unit6, num3);
			}
		}
		return (float)num2 * num;
	}

	private List<UnitDesign> availableUnitDesignList;

	private List<AIData_Unit> availableUnitList;

	private StaticString defaultUnitPatternCategory;

	private IAIDataRepositoryAIHelper aiDataRepository;

	private AIBelief_UnitBodyStrength[] strengthBeliefPerEmpire;

	private List<IGarrison> attackerReinforcement = new List<IGarrison>();

	private List<IGarrison> defenderReinforcement = new List<IGarrison>();

	private Amplitude.Unity.Framework.AnimationCurve reinforcementModifierByRound;

	private List<AIData_GameEntity> availableGameEntities = new List<AIData_GameEntity>();

	private Intelligence.BestRecruitementCombination bestRecruitementCombination = new Intelligence.BestRecruitementCombination();

	private float maximalNumberOfTurn = 10f;

	private float maxRecruitementDistance;

	private int[] selectedEntities;

	private IEndTurnService endTurnService;

	private List<GameEntityGUID> guidsInBattle = new List<GameEntityGUID>();

	private IPathfindingService pathfindingService;

	private List<float> tempUnitsMilitaryPower = new List<float>();

	private IUnitPatternAIHelper unitPatternHelper;

	private World world;

	private IWorldPositionningService worldPositionningService;

	private IDatabase<Amplitude.Unity.Framework.AnimationCurve> animationCurveDatabase;

	private IVisibilityService visibilityService;

	private int numberOfBattleRound;

	public class BestRecruitementCombination
	{
		public float SumOfMilitaryPower { private get; set; }

		public void AddHeroPower(float heroMilitaryPower)
		{
			this.withHero = true;
			this.SumOfMilitaryPower += heroMilitaryPower;
		}

		public float GetMilitaryPower()
		{
			return this.SumOfMilitaryPower * (float)(this.UsedSlots + ((!this.withHero) ? 0 : 1));
		}

		public float GetMilitaryPower(float heroMilitaryPower)
		{
			Diagnostics.Assert(!this.withHero);
			return (this.SumOfMilitaryPower + heroMilitaryPower) * (float)(this.UsedSlots + 1);
		}

		public void Reset()
		{
			this.CombinationOfArmiesUnits.Clear();
			this.SumOfMilitaryPower = -1f;
			this.UsedSlots = 0;
			this.withHero = false;
		}

		public List<AIData_GameEntity> CombinationOfArmiesUnits = new List<AIData_GameEntity>();

		public int UsedSlots;

		private bool withHero;
	}
}
