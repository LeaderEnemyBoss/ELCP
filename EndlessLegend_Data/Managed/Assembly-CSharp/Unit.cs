using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Unity.Xml;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class Unit : SimulationObjectWrapper, IXmlSerializable, IPathfindingContextProvider, IUnitAbilityController, IUnitRankController, IGameEntity, IGuiEntity, IAttributeFeatureProvider, IBattleActionProvider, ICategoryProvider, IDescriptorEffectProvider, IEquipmentProvider, IPropertyEffectFeatureProvider, ITitleFeatureProvider
{
	public Unit(GameEntityGUID guid)
	{
		this.unitRanks = new List<UnitRank>();
		base..ctor("Unit#" + guid);
		Diagnostics.Assert(guid != GameEntityGUID.Zero, "Game entity GUID is null.");
		this.GUID = guid;
		this.unitRankDatabase = Databases.GetDatabase<UnitRank>(false);
		this.unitAbilityDatabase = Databases.GetDatabase<UnitAbility>(false);
		this.unitSkillDatabase = Databases.GetDatabase<UnitSkill>(false);
		this.simulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
		this.eventService = Services.GetService<IEventService>();
		this.carriedCityImprovements = new List<StaticString>();
		this.AppliedBoosts = new List<StaticString>();
		this.HasMapBoost = false;
		this.EnablesRetrofit = false;
		this.PathfindingContextMode = PathfindingContextMode.Default;
	}

	public event EventHandler<UnitSkillChangeEventArgs> UnitSkillChange;

	public event EventHandler UnitRetrofited;

	public event EventHandler WorldTerrainDamageReceived;

	GuiElement IGuiEntity.Gui
	{
		get
		{
			if (this.guiElement == null && this.UnitDesign != null)
			{
				IDatabase<GuiElement> database = Databases.GetDatabase<GuiElement>(false);
				if (database != null)
				{
					if (this.UnitDesign is UnitProfile)
					{
						UnitProfile unitProfile = this.UnitDesign as UnitProfile;
						this.guiElement = database.GetValue(unitProfile.ProfileGuiElementName);
					}
					else
					{
						this.guiElement = database.GetValue(this.UnitDesign.UnitBodyDefinitionReference);
					}
				}
			}
			return this.guiElement;
		}
	}

	string ITitleFeatureProvider.Title
	{
		get
		{
			if (this.UnitDesign == null)
			{
				return string.Empty;
			}
			return this.UnitDesign.LocalizedName;
		}
	}

	XmlNamedReference IBattleActionProvider.BattleActionName
	{
		get
		{
			if (this.UnitDesign != null)
			{
				return ((IBattleActionProvider)this.UnitDesign).BattleActionName;
			}
			return null;
		}
	}

	UnitEquipmentSet IEquipmentProvider.UnitEquipmentSet
	{
		get
		{
			if (this.UnitDesign != null)
			{
				return ((IEquipmentProvider)this.UnitDesign).UnitEquipmentSet;
			}
			return new UnitEquipmentSet();
		}
	}

	StaticString ICategoryProvider.Category
	{
		get
		{
			if (this.UnitDesign is UnitProfile)
			{
				return UnitProfile.ReadOnlyCategory;
			}
			return Unit.ReadOnlyCategory;
		}
	}

	StaticString ICategoryProvider.SubCategory
	{
		get
		{
			if (this.UnitDesign != null)
			{
				return this.UnitDesign.SubCategory;
			}
			return StaticString.Empty;
		}
	}

	StaticString IDescriptorEffectProvider.DefaultClass
	{
		get
		{
			return this.UnitDesign.DefaultClass;
		}
	}

	float IAttributeFeatureProvider.GetAttributeValue(StaticString attributeName)
	{
		return this.GetPropertyValue(attributeName);
	}

	float IAttributeFeatureProvider.GetAttributeDelta(StaticString attributeName)
	{
		return 0f;
	}

	IEnumerable<SimulationDescriptor> IDescriptorEffectProvider.GetDescriptors()
	{
		for (int index = 0; index < base.SimulationObject.DescriptorHolders.Count; index++)
		{
			yield return base.SimulationObject.DescriptorHolders[index].Descriptor;
		}
		yield break;
	}

	SimulationObject IPropertyEffectFeatureProvider.GetSimulationObject()
	{
		return base.SimulationObject;
	}

	public UnitRank CurrentUnitRank
	{
		get
		{
			return this.currentUnitRank;
		}
	}

	public int Level
	{
		get
		{
			if (this.currentUnitRank == null || StaticString.IsNullOrEmpty(this.currentUnitRank.LevelPropertyName))
			{
				return 0;
			}
			return Mathf.RoundToInt(this.GetPropertyValue(this.currentUnitRank.LevelPropertyName));
		}
		set
		{
			this.ForceLevel(value);
		}
	}

	public void GainXp(float xp, bool silent = false, bool considerModifier = true)
	{
		if (this.currentUnitRank == null)
		{
			return;
		}
		float num = this.GetPropertyValue(SimulationProperties.UnitExperience);
		float propertyValue = this.GetPropertyValue(SimulationProperties.UnitAccumulatedExperience);
		float propertyValue2 = this.GetPropertyValue(SimulationProperties.UnitNextLevelExperience);
		float level = this.GetPropertyValue(this.currentUnitRank.LevelPropertyName);
		float propertyValue3 = this.GetPropertyValue(SimulationProperties.UnitExperienceGainModifier);
		if (this.currentUnitRank.MaximumLevel > 0 && level >= (float)this.currentUnitRank.MaximumLevel && !StaticString.IsNullOrEmpty(this.currentUnitRank.NextRank))
		{
			return;
		}
		float num2 = (!considerModifier) ? xp : (xp * propertyValue3);
		num += num2;
		base.SetPropertyBaseValue(SimulationProperties.UnitAccumulatedExperience, propertyValue + num2);
		bool flag = false;
		UnitProfile unitProfile = this.UnitDesign as UnitProfile;
		if (unitProfile != null && unitProfile.IsHero)
		{
			flag = true;
		}
		float level2 = level;
		while (num >= propertyValue2)
		{
			level += 1f;
			num -= propertyValue2;
			if (this.currentUnitRank.UnitLevelBonuses != null && this.currentUnitRank.UnitLevelBonuses.Length > 0)
			{
				UnitLevelBonus unitLevelBonus = Array.Find<UnitLevelBonus>(this.currentUnitRank.UnitLevelBonuses, (UnitLevelBonus match) => (float)match.Level == level);
				if (unitLevelBonus.UnitAbilityReferences != null && unitLevelBonus.UnitAbilityReferences.Length > 0)
				{
					for (int i = 0; i < unitLevelBonus.UnitAbilityReferences.Length; i++)
					{
						this.AddUnitAbility(unitLevelBonus.UnitAbilityReferences[i]);
					}
				}
			}
			if (this.currentUnitRank.MaximumLevel > 0 && level >= (float)this.currentUnitRank.MaximumLevel && !StaticString.IsNullOrEmpty(this.currentUnitRank.NextRank))
			{
				this.ForceRank(this.currentUnitRank.NextRank, 0);
			}
			else
			{
				if (this.currentUnitRank.MaximumLevel > 0 && level >= (float)this.currentUnitRank.MaximumLevel)
				{
					base.SetPropertyBaseValue(this.currentUnitRank.LevelPropertyName, (float)this.currentUnitRank.MaximumLevel);
					base.SetPropertyBaseValue(SimulationProperties.UnitAccumulatedExperience, propertyValue2);
					level = (float)this.currentUnitRank.MaximumLevel;
					num = propertyValue2;
					break;
				}
				base.SetPropertyBaseValue(this.currentUnitRank.LevelPropertyName, level);
			}
			base.SetPropertyBaseValue(SimulationProperties.UnitExperience, num);
			this.Refresh(false);
			propertyValue2 = this.GetPropertyValue(SimulationProperties.UnitNextLevelExperience);
		}
		base.SetPropertyBaseValue(SimulationProperties.UnitExperience, num);
		this.Refresh(false);
		if (level > level2 && this.Garrison != null && this.Garrison.Empire != null)
		{
			if (flag)
			{
				this.eventService.Notify(new EventHeroLevelUp(this.Garrison.Empire, this, silent));
			}
			else
			{
				this.eventService.Notify(new EventUnitLevelUp(this.Garrison.Empire, this, silent));
			}
		}
	}

	public void ForceLevel(int level)
	{
		if (this.currentUnitRank == null)
		{
			return;
		}
		if (level >= this.currentUnitRank.MaximumLevel && this.currentUnitRank.MaximumLevel >= 0)
		{
			level = this.currentUnitRank.MaximumLevel;
		}
		if (this.Level >= level)
		{
			return;
		}
		if (this.currentUnitRank.UnitLevelBonuses != null)
		{
			for (int i = 0; i < this.currentUnitRank.UnitLevelBonuses.Length; i++)
			{
				if (level < this.currentUnitRank.UnitLevelBonuses[i].Level)
				{
					break;
				}
				this.AddUnitAbilities(this.currentUnitRank.UnitLevelBonuses[i].UnitAbilityReferences);
			}
		}
		base.SetPropertyBaseValue(this.currentUnitRank.LevelPropertyName, (float)level);
		this.Refresh(false);
	}

	public void ForceRank(StaticString rankName, int startingLevel = 0)
	{
		UnitRank unitRank = null;
		if (this.unitRankDatabase.TryGetValue(rankName, out unitRank))
		{
			this.unitRanks.Add(unitRank);
			if (startingLevel > unitRank.MaximumLevel)
			{
				startingLevel = unitRank.MaximumLevel;
			}
			if (startingLevel < 0)
			{
				startingLevel = 0;
			}
			this.currentUnitRank = unitRank;
			SimulationDescriptor descriptor = null;
			if (this.simulationDescriptorDatabase.TryGetValue(this.currentUnitRank.LevelUpDescriptor, out descriptor))
			{
				base.AddDescriptor(descriptor, false);
			}
			if (this.simulationDescriptorDatabase.TryGetValue(this.currentUnitRank.Name, out descriptor))
			{
				base.SwapDescriptor(descriptor);
			}
			this.AddUnitAbilities(unitRank.UnitAbilities);
			if (unitRank.UnitLevelBonuses != null)
			{
				for (int i = 0; i < unitRank.UnitLevelBonuses.Length; i++)
				{
					if (startingLevel < unitRank.UnitLevelBonuses[i].Level)
					{
						break;
					}
					this.AddUnitAbilities(unitRank.UnitLevelBonuses[i].UnitAbilityReferences);
				}
			}
			base.SetPropertyBaseValue(this.currentUnitRank.LevelPropertyName, (float)startingLevel);
		}
		else
		{
			Diagnostics.LogWarning("Cannot find the rank '{0}' in the database.", new object[]
			{
				rankName
			});
		}
		this.Refresh(false);
	}

	public void ForceRank(StaticString[] rankNames)
	{
		for (int i = 0; i < rankNames.Length; i++)
		{
			this.ForceRank(rankNames[i], int.MaxValue);
		}
	}

	public UnitAbilityReference[] GetAbilities()
	{
		return (from referenceCount in this.currentAbilities
		where !referenceCount.Disabled
		select new UnitAbilityReference(referenceCount.UnitAbility.Name, referenceCount.CurrentLevel)).ToArray<UnitAbilityReference>();
	}

	public IEnumerable<KeyValuePair<StaticString, int>> EnumerateUnitAbilities()
	{
		for (int index = 0; index < this.currentAbilities.Count; index++)
		{
			if (!this.currentAbilities[index].Disabled)
			{
				yield return new KeyValuePair<StaticString, int>(this.currentAbilities[index].UnitAbility.Name, this.currentAbilities[index].CurrentLevel);
			}
		}
		yield break;
	}

	public bool CheckUnitAbility(StaticString unitcurrentAbility, int level = -1)
	{
		Unit.UnitAbilityReferenceCount unitAbilityReferenceCount = this.currentAbilities.Find((Unit.UnitAbilityReferenceCount match) => match.UnitAbility.Name == unitcurrentAbility);
		if (unitAbilityReferenceCount != null)
		{
			if (level == -1)
			{
				return unitAbilityReferenceCount.ReferenceCount > 0;
			}
			if (level < unitAbilityReferenceCount.ReferenceCountByLevel.Length)
			{
				return unitAbilityReferenceCount.ReferenceCountByLevel[level] > 0;
			}
		}
		return false;
	}

	public void AddUnitAbilities(UnitAbilityReference[] abilityReferences)
	{
		if (abilityReferences == null)
		{
			return;
		}
		for (int i = 0; i < abilityReferences.Length; i++)
		{
			this.AddUnitAbility(abilityReferences[i]);
		}
	}

	public void AddUnitAbility(UnitAbilityReference abilityReference)
	{
		this.AddUnitAbility(abilityReference.Name, abilityReference.Level, abilityReference.Parameters);
	}

	public void AddUnitAbility(UnitAbilityReference abilityReference, out Unit.UnitAbilityReferenceCount unitAbilityReferenceCount)
	{
		unitAbilityReferenceCount = this.AddUnitAbility(abilityReference.Name, abilityReference.Level, abilityReference.Parameters);
	}

	public StaticString[] GetUnitAbilityParameters(StaticString unitAbilityName)
	{
		Unit.UnitAbilityReferenceCount unitAbilityReferenceCount = this.currentAbilities.Find((Unit.UnitAbilityReferenceCount match) => match.UnitAbility.Name == unitAbilityName);
		if (unitAbilityReferenceCount != null)
		{
			return unitAbilityReferenceCount.Parameters;
		}
		return null;
	}

	public void RemoveUnitAbilities(UnitAbilityReference[] abilityReferences)
	{
		if (abilityReferences == null)
		{
			return;
		}
		for (int i = 0; i < abilityReferences.Length; i++)
		{
			this.RemoveUnitAbility(abilityReferences[i]);
		}
	}

	public void RemoveUnitAbility(UnitAbilityReference abilityReference)
	{
		this.RemoveUnitAbility(abilityReference.Name, abilityReference.Level);
	}

	public void VerifyUnitAbilities()
	{
		Diagnostics.Assert(this.UnitDesign != null);
		Diagnostics.Assert(this.UnitDesign.UnitEquipmentSet != null);
		for (int i = this.currentAbilities.Count - 1; i >= 0; i--)
		{
			Unit.UnitAbilityReferenceCount unitAbilityReferenceCount = this.currentAbilities[i];
			int num = 0;
			if (unitAbilityReferenceCount.UnitAbility.AbilityLevels != null)
			{
				num = unitAbilityReferenceCount.UnitAbility.AbilityLevels.Length;
			}
			if (unitAbilityReferenceCount.ReferenceCountByLevel.Length != num)
			{
				while (unitAbilityReferenceCount.ReferenceCount > 0)
				{
					this.RemoveUnitAbility(unitAbilityReferenceCount.UnitAbility.Name, unitAbilityReferenceCount.CurrentLevel);
				}
			}
		}
		List<ItemDefinition> items = new List<ItemDefinition>();
		if (this.UnitDesign.UnitEquipmentSet.Slots != null && this.UnitDesign.UnitEquipmentSet.Slots.Length != 0)
		{
			IDatabase<ItemDefinition> database = Databases.GetDatabase<ItemDefinition>(false);
			for (int j = 0; j < this.UnitDesign.UnitEquipmentSet.Slots.Length; j++)
			{
				ItemDefinition itemDefinition;
				if (database.TryGetValue(this.UnitDesign.UnitEquipmentSet.Slots[j].ItemName.ToString().Split(DepartmentOfDefense.ItemSeparators)[0], out itemDefinition))
				{
					if (itemDefinition.AbilityReferences != null)
					{
						if (!items.Contains(itemDefinition))
						{
							items.Add(itemDefinition);
						}
					}
				}
			}
		}
		if (this.UnitDesign.UnitBodyDefinition.UnitAbilities != null)
		{
			int abilityIndex;
			for (abilityIndex = 0; abilityIndex < this.UnitDesign.UnitBodyDefinition.UnitAbilities.Length; abilityIndex++)
			{
				Unit.UnitAbilityReferenceCount unitAbilityReferenceCount2 = this.currentAbilities.Find((Unit.UnitAbilityReferenceCount match) => match.UnitAbility.Name == this.UnitDesign.UnitBodyDefinition.UnitAbilities[abilityIndex]);
				if (unitAbilityReferenceCount2 == null)
				{
					this.AddUnitAbility(this.UnitDesign.UnitBodyDefinition.UnitAbilities[abilityIndex]);
				}
				else
				{
					unitAbilityReferenceCount2.Parameters = this.UnitDesign.UnitBodyDefinition.UnitAbilities[abilityIndex].Parameters;
				}
			}
		}
		UnitProfile profile = this.UnitDesign as UnitProfile;
		if (profile != null && profile.ProfileAbilityReferences != null)
		{
			int abilityIndex;
			for (abilityIndex = 0; abilityIndex < profile.ProfileAbilityReferences.Length; abilityIndex++)
			{
				Unit.UnitAbilityReferenceCount unitAbilityReferenceCount2 = this.currentAbilities.Find((Unit.UnitAbilityReferenceCount match) => match.UnitAbility.Name == profile.ProfileAbilityReferences[abilityIndex]);
				if (unitAbilityReferenceCount2 == null)
				{
					this.AddUnitAbility(profile.ProfileAbilityReferences[abilityIndex]);
				}
				else
				{
					unitAbilityReferenceCount2.Parameters = profile.ProfileAbilityReferences[abilityIndex].Parameters;
				}
			}
		}
		if (this.CurrentUnitRank != null && this.CurrentUnitRank.UnitAbilities != null)
		{
			int abilityIndex;
			for (abilityIndex = 0; abilityIndex < this.CurrentUnitRank.UnitAbilities.Length; abilityIndex++)
			{
				Unit.UnitAbilityReferenceCount unitAbilityReferenceCount2 = this.currentAbilities.Find((Unit.UnitAbilityReferenceCount match) => match.UnitAbility.Name == this.CurrentUnitRank.UnitAbilities[abilityIndex]);
				if (unitAbilityReferenceCount2 == null)
				{
					this.AddUnitAbility(this.CurrentUnitRank.UnitAbilities[abilityIndex]);
				}
				else
				{
					unitAbilityReferenceCount2.Parameters = this.CurrentUnitRank.UnitAbilities[abilityIndex].Parameters;
				}
			}
			if (this.CurrentUnitRank.UnitLevelBonuses != null)
			{
			}
		}
		int itemIndex;
		for (itemIndex = 0; itemIndex < items.Count; itemIndex++)
		{
			int abilityIndex;
			for (abilityIndex = 0; abilityIndex < items[itemIndex].AbilityReferences.Length; abilityIndex++)
			{
				Unit.UnitAbilityReferenceCount unitAbilityReferenceCount2 = this.currentAbilities.Find((Unit.UnitAbilityReferenceCount match) => match.UnitAbility.Name == items[itemIndex].AbilityReferences[abilityIndex]);
				if (unitAbilityReferenceCount2 == null)
				{
					this.AddUnitAbility(items[itemIndex].AbilityReferences[abilityIndex]);
				}
				else
				{
					unitAbilityReferenceCount2.Parameters = items[itemIndex].AbilityReferences[abilityIndex].Parameters;
				}
			}
		}
		foreach (KeyValuePair<StaticString, int> keyValuePair in this.unlockedSkillLevelBySkillName)
		{
			UnitSkill unitSkill = this.unitSkillDatabase.GetValue(keyValuePair.Key);
			if (unitSkill != null && unitSkill.UnitSkillLevels != null)
			{
				int index = 0;
				while (index <= keyValuePair.Value && index < unitSkill.UnitSkillLevels.Length)
				{
					if (unitSkill.UnitSkillLevels[index].UnitAbilities != null)
					{
						int abilityIndex;
						for (abilityIndex = 0; abilityIndex < unitSkill.UnitSkillLevels[index].UnitAbilities.Length; abilityIndex++)
						{
							Unit.UnitAbilityReferenceCount unitAbilityReferenceCount2 = this.currentAbilities.Find((Unit.UnitAbilityReferenceCount match) => match.UnitAbility.Name == unitSkill.UnitSkillLevels[index].UnitAbilities[abilityIndex]);
							if (unitAbilityReferenceCount2 == null)
							{
								this.AddUnitAbility(unitSkill.UnitSkillLevels[index].UnitAbilities[abilityIndex]);
							}
							else
							{
								unitAbilityReferenceCount2.Parameters = unitSkill.UnitSkillLevels[index].UnitAbilities[abilityIndex].Parameters;
							}
						}
					}
					index++;
				}
			}
		}
		if (this.Embarked)
		{
			if (this.currentAbilities.Find((Unit.UnitAbilityReferenceCount match) => match.UnitAbility.Name == UnitAbility.ReadonlyTransportShip) == null)
			{
				this.AddUnitAbility(UnitAbility.ReadonlyTransportShip, 1, null);
			}
		}
		for (int k = this.currentAbilities.Count - 1; k >= 0; k--)
		{
			Unit.UnitAbilityReferenceCount unitAbilityReferenceCount3 = this.currentAbilities[k];
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			if (unitAbilityReferenceCount3.UnitAbility.AbilityLevels != null)
			{
				num4 = unitAbilityReferenceCount3.UnitAbility.AbilityLevels.Length;
			}
			int[] array = new int[num4];
			this.VerifyReferenceCount(unitAbilityReferenceCount3.UnitAbility.Name, this.UnitDesign.UnitBodyDefinition.UnitAbilities, ref num2, ref num3, ref array);
			if (this.Embarked)
			{
				IDatabase<UnitBodyDefinition> database2 = Databases.GetDatabase<UnitBodyDefinition>(false);
				UnitBodyDefinition unitBodyDefinition;
				if (database2 != null && database2.TryGetValue("UnitBodyTransportShip", out unitBodyDefinition) && unitBodyDefinition.UnitAbilities != null)
				{
					this.VerifyReferenceCount(unitAbilityReferenceCount3.UnitAbility.Name, unitBodyDefinition.UnitAbilities, ref num2, ref num3, ref array);
				}
			}
			if (profile != null)
			{
				this.VerifyReferenceCount(unitAbilityReferenceCount3.UnitAbility.Name, profile.ProfileAbilityReferences, ref num2, ref num3, ref array);
			}
			if (this.CurrentUnitRank != null)
			{
				this.VerifyReferenceCount(unitAbilityReferenceCount3.UnitAbility.Name, this.CurrentUnitRank.UnitAbilities, ref num2, ref num3, ref array);
			}
			for (int l = 0; l < items.Count; l++)
			{
				this.VerifyReferenceCount(unitAbilityReferenceCount3.UnitAbility.Name, items[l].AbilityReferences, ref num2, ref num3, ref array);
			}
			foreach (KeyValuePair<StaticString, int> keyValuePair2 in this.unlockedSkillLevelBySkillName)
			{
				UnitSkill value = this.unitSkillDatabase.GetValue(keyValuePair2.Key);
				if (value != null && value.UnitSkillLevels != null)
				{
					int num5 = 0;
					while (num5 <= keyValuePair2.Value && num5 < value.UnitSkillLevels.Length)
					{
						if (value.UnitSkillLevels[num5].UnitAbilities != null)
						{
							this.VerifyReferenceCount(unitAbilityReferenceCount3.UnitAbility.Name, value.UnitSkillLevels[num5].UnitAbilities, ref num2, ref num3, ref array);
						}
						num5++;
					}
				}
			}
			if (unitAbilityReferenceCount3.UnitAbility.Name == UnitAbility.ReadonlyTransportShip && this.Embarked)
			{
				num2++;
			}
			if (num2 == 0)
			{
				while (unitAbilityReferenceCount3.ReferenceCount > 0)
				{
					this.RemoveUnitAbility(unitAbilityReferenceCount3.UnitAbility.Name, unitAbilityReferenceCount3.CurrentLevel);
				}
			}
			else
			{
				for (int m = 0; m < array.Length; m++)
				{
					while (m < unitAbilityReferenceCount3.ReferenceCountByLevel.Length && unitAbilityReferenceCount3.ReferenceCountByLevel[m] < array[m])
					{
						this.AddUnitAbility(unitAbilityReferenceCount3.UnitAbility.Name, m, unitAbilityReferenceCount3.Parameters);
					}
				}
				for (int n = 0; n < array.Length; n++)
				{
					while (n < unitAbilityReferenceCount3.ReferenceCountByLevel.Length && unitAbilityReferenceCount3.ReferenceCountByLevel[n] > array[n])
					{
						this.RemoveUnitAbility(unitAbilityReferenceCount3.UnitAbility.Name, n);
					}
				}
			}
			if (!unitAbilityReferenceCount3.Disabled)
			{
				if (unitAbilityReferenceCount3.UnitAbility.Descriptors != null)
				{
					for (int num6 = 0; num6 < unitAbilityReferenceCount3.UnitAbility.Descriptors.Length; num6++)
					{
						SimulationDescriptor simulationDescriptor = unitAbilityReferenceCount3.UnitAbility.Descriptors[num6];
						if (simulationDescriptor != null && !base.SimulationObject.Tags.Contains(simulationDescriptor.Name))
						{
							base.AddDescriptor(simulationDescriptor, false);
						}
					}
				}
				if (unitAbilityReferenceCount3.CurrentLevel >= 0 && unitAbilityReferenceCount3.UnitAbility.AbilityLevels[unitAbilityReferenceCount3.CurrentLevel].SimulationDescriptorReferences != null)
				{
					UnitAbility.UnitAbilityLevelDefinition unitAbilityLevelDefinition = unitAbilityReferenceCount3.UnitAbility.AbilityLevels[unitAbilityReferenceCount3.CurrentLevel];
					SimulationDescriptor descriptor = null;
					for (int num7 = 0; num7 < unitAbilityLevelDefinition.SimulationDescriptorReferences.Length; num7++)
					{
						StaticString staticString = unitAbilityLevelDefinition.SimulationDescriptorReferences[num7];
						if (!base.SimulationObject.Tags.Contains(staticString))
						{
							if (this.simulationDescriptorDatabase.TryGetValue(staticString, out descriptor))
							{
								base.AddDescriptor(descriptor, false);
							}
						}
					}
				}
			}
		}
	}

	internal void FindUnitAbilityFromBattleActionName(StaticString battleActionName, out StaticString unitAbilityName, out int unitAbilityLevel)
	{
		for (int i = 0; i < this.currentAbilities.Count; i++)
		{
			UnitAbility unitAbility = this.currentAbilities[i].UnitAbility;
			int currentLevel = this.currentAbilities[i].CurrentLevel;
			if (unitAbility.BattleActionUnitReferences != null && Array.Exists<XmlNamedReference>(unitAbility.BattleActionUnitReferences, (XmlNamedReference match) => match.Name == battleActionName))
			{
				unitAbilityName = unitAbility.Name;
				unitAbilityLevel = currentLevel;
				return;
			}
			if (currentLevel >= 0 && unitAbility.AbilityLevels != null && unitAbility.AbilityLevels[currentLevel].BattleActionUnitReferences != null && Array.Exists<XmlNamedReference>(unitAbility.AbilityLevels[currentLevel].BattleActionUnitReferences, (XmlNamedReference match) => match.Name == battleActionName))
			{
				unitAbilityName = unitAbility.Name;
				unitAbilityLevel = currentLevel;
				return;
			}
		}
		unitAbilityName = StaticString.Empty;
		unitAbilityLevel = -1;
	}

	private Unit.UnitAbilityReferenceCount AddUnitAbility(StaticString unitAbilityName, int level, StaticString[] parameters)
	{
		UnitAbility unitAbility = null;
		SimulationDescriptor descriptor = null;
		if (this.unitAbilityDatabase.TryGetValue(unitAbilityName, out unitAbility))
		{
			if (Services.GetService<IDownloadableContentService>() != null)
			{
				bool flag = true;
				foreach (Prerequisite prerequisite in unitAbility.Prerequisites)
				{
					if (prerequisite is DownloadableContentPrerequisite)
					{
						Prerequisite prerequisite2 = prerequisite as DownloadableContentPrerequisite;
						InterpreterContext context = null;
						flag = prerequisite2.Check(context);
						break;
					}
				}
				if (!flag)
				{
					return null;
				}
			}
			Unit.UnitAbilityReferenceCount unitAbilityReferenceCount = this.currentAbilities.Find((Unit.UnitAbilityReferenceCount match) => match.UnitAbility == unitAbility);
			if (unitAbilityReferenceCount == null)
			{
				unitAbilityReferenceCount = new Unit.UnitAbilityReferenceCount(unitAbility, parameters);
				unitAbilityReferenceCount.Disabled = (this.Embarked && !unitAbility.Persistent);
				this.currentAbilities.Add(unitAbilityReferenceCount);
			}
			if (unitAbilityReferenceCount.ReferenceCountByLevel.Length != 0)
			{
				if (level >= unitAbilityReferenceCount.ReferenceCountByLevel.Length)
				{
					Diagnostics.LogError("Invalid level {1} for unit ability '{0}'", new object[]
					{
						unitAbilityName,
						level
					});
				}
				if (unitAbilityReferenceCount.ReferenceCountByLevel[level] == 0 && level > unitAbilityReferenceCount.CurrentLevel)
				{
					if (unitAbilityReferenceCount.CurrentLevel >= 0 && unitAbilityReferenceCount.UnitAbility.AbilityLevels[unitAbilityReferenceCount.CurrentLevel].SimulationDescriptorReferences != null)
					{
						for (int j = 0; j < unitAbilityReferenceCount.UnitAbility.AbilityLevels[unitAbilityReferenceCount.CurrentLevel].SimulationDescriptorReferences.Length; j++)
						{
							base.RemoveDescriptorByName(unitAbilityReferenceCount.UnitAbility.AbilityLevels[unitAbilityReferenceCount.CurrentLevel].SimulationDescriptorReferences[j]);
						}
					}
					if (unitAbilityReferenceCount.UnitAbility.AbilityLevels[level].SimulationDescriptorReferences != null && !unitAbilityReferenceCount.Disabled)
					{
						for (int k = 0; k < unitAbilityReferenceCount.UnitAbility.AbilityLevels[level].SimulationDescriptorReferences.Length; k++)
						{
							if (this.simulationDescriptorDatabase.TryGetValue(unitAbilityReferenceCount.UnitAbility.AbilityLevels[level].SimulationDescriptorReferences[k], out descriptor))
							{
								base.AddDescriptor(descriptor, false);
							}
						}
					}
					unitAbilityReferenceCount.CurrentLevel = level;
				}
				unitAbilityReferenceCount.ReferenceCountByLevel[level]++;
			}
			if (unitAbilityReferenceCount.ReferenceCount == 0 && unitAbilityReferenceCount.UnitAbility.Descriptors != null && !unitAbilityReferenceCount.Disabled)
			{
				for (int l = 0; l < unitAbilityReferenceCount.UnitAbility.Descriptors.Length; l++)
				{
					if (unitAbilityReferenceCount.UnitAbility.Descriptors[l] != null)
					{
						base.AddDescriptor(unitAbilityReferenceCount.UnitAbility.Descriptors[l], false);
					}
				}
			}
			Unit.UnitAbilityReferenceCount unitAbilityReferenceCount2 = unitAbilityReferenceCount;
			int i = unitAbilityReferenceCount2.ReferenceCount;
			unitAbilityReferenceCount2.ReferenceCount = i + 1;
			return unitAbilityReferenceCount;
		}
		Diagnostics.LogWarning("Cannot find the ability '{0}' in the database.", new object[]
		{
			unitAbilityName
		});
		return null;
	}

	private void DisableUnitAbilities(Unit.UnitAbilityReferenceCount referenceCount)
	{
		if (referenceCount == null)
		{
			return;
		}
		for (int i = 0; i < referenceCount.UnitAbility.Descriptors.Length; i++)
		{
			if (referenceCount.UnitAbility.Descriptors[i] != null)
			{
				base.RemoveDescriptorByName(referenceCount.UnitAbility.Descriptors[i].Name);
			}
		}
		if (referenceCount.ReferenceCountByLevel.Length > 0 && referenceCount.UnitAbility.AbilityLevels[referenceCount.CurrentLevel].SimulationDescriptorReferences != null)
		{
			for (int j = 0; j < referenceCount.UnitAbility.AbilityLevels[referenceCount.CurrentLevel].SimulationDescriptorReferences.Length; j++)
			{
				base.RemoveDescriptorByName(referenceCount.UnitAbility.AbilityLevels[referenceCount.CurrentLevel].SimulationDescriptorReferences[j]);
			}
		}
		referenceCount.Disabled = true;
	}

	private void ReenableUnitAbility(Unit.UnitAbilityReferenceCount referenceCount, int level)
	{
		SimulationDescriptor descriptor = null;
		if (referenceCount.ReferenceCountByLevel.Length > 0 && referenceCount.UnitAbility.AbilityLevels[level].SimulationDescriptorReferences != null)
		{
			for (int i = 0; i < referenceCount.UnitAbility.AbilityLevels[level].SimulationDescriptorReferences.Length; i++)
			{
				if (this.simulationDescriptorDatabase.TryGetValue(referenceCount.UnitAbility.AbilityLevels[level].SimulationDescriptorReferences[i], out descriptor))
				{
					base.AddDescriptor(descriptor, false);
				}
			}
		}
		for (int j = 0; j < referenceCount.UnitAbility.Descriptors.Length; j++)
		{
			if (referenceCount.UnitAbility.Descriptors[j] != null)
			{
				base.AddDescriptor(referenceCount.UnitAbility.Descriptors[j], false);
			}
		}
		referenceCount.Disabled = false;
	}

	private void RemoveUnitAbility(StaticString currentAbility, int level)
	{
		UnitAbility unitAbility = null;
		SimulationDescriptor descriptor = null;
		if (this.unitAbilityDatabase.TryGetValue(currentAbility, out unitAbility))
		{
			Unit.UnitAbilityReferenceCount unitAbilityReferenceCount = this.currentAbilities.Find((Unit.UnitAbilityReferenceCount match) => match.UnitAbility == unitAbility);
			if (unitAbilityReferenceCount == null)
			{
				return;
			}
			unitAbilityReferenceCount.ReferenceCount--;
			if (unitAbilityReferenceCount.ReferenceCount == 0)
			{
				for (int i = 0; i < unitAbilityReferenceCount.UnitAbility.Descriptors.Length; i++)
				{
					if (unitAbilityReferenceCount.UnitAbility.Descriptors[i] != null)
					{
						base.RemoveDescriptorByName(unitAbilityReferenceCount.UnitAbility.Descriptors[i].Name);
					}
				}
				this.currentAbilities.Remove(unitAbilityReferenceCount);
			}
			if (unitAbilityReferenceCount.ReferenceCountByLevel.Length > 0)
			{
				unitAbilityReferenceCount.ReferenceCountByLevel[level]--;
				if (unitAbilityReferenceCount.ReferenceCountByLevel[level] == 0 && level == unitAbilityReferenceCount.CurrentLevel)
				{
					if (unitAbilityReferenceCount.UnitAbility.AbilityLevels[unitAbilityReferenceCount.CurrentLevel].SimulationDescriptorReferences != null)
					{
						for (int j = 0; j < unitAbilityReferenceCount.UnitAbility.AbilityLevels[unitAbilityReferenceCount.CurrentLevel].SimulationDescriptorReferences.Length; j++)
						{
							base.RemoveDescriptorByName(unitAbilityReferenceCount.UnitAbility.AbilityLevels[unitAbilityReferenceCount.CurrentLevel].SimulationDescriptorReferences[j]);
						}
					}
					if (unitAbilityReferenceCount.ReferenceCount == 0)
					{
						return;
					}
					unitAbilityReferenceCount.CurrentLevel = -1;
					for (int k = 0; k < unitAbilityReferenceCount.ReferenceCountByLevel.Length; k++)
					{
						if (unitAbilityReferenceCount.ReferenceCountByLevel[k] > 0 && k > unitAbilityReferenceCount.CurrentLevel)
						{
							unitAbilityReferenceCount.CurrentLevel = k;
						}
					}
					if (unitAbilityReferenceCount.CurrentLevel >= 0 && unitAbilityReferenceCount.UnitAbility.AbilityLevels[unitAbilityReferenceCount.CurrentLevel].SimulationDescriptorReferences != null)
					{
						for (int l = 0; l < unitAbilityReferenceCount.UnitAbility.AbilityLevels[unitAbilityReferenceCount.CurrentLevel].SimulationDescriptorReferences.Length; l++)
						{
							if (!base.SimulationObject.Tags.Contains(unitAbilityReferenceCount.UnitAbility.AbilityLevels[unitAbilityReferenceCount.CurrentLevel].SimulationDescriptorReferences[l]))
							{
								if (this.simulationDescriptorDatabase.TryGetValue(unitAbilityReferenceCount.UnitAbility.AbilityLevels[unitAbilityReferenceCount.CurrentLevel].SimulationDescriptorReferences[l], out descriptor))
								{
									base.AddDescriptor(descriptor, false);
								}
							}
						}
					}
				}
			}
		}
		else
		{
			Diagnostics.LogWarning("Cannot find the ability '{0}' in the database.", new object[]
			{
				currentAbility
			});
		}
	}

	private void VerifyReferenceCount(StaticString abilityName, UnitAbilityReference[] abilityReferences, ref int verifiedCount, ref int maxLevel, ref int[] referenceCountByLevel)
	{
		if (abilityReferences == null)
		{
			return;
		}
		for (int i = 0; i < abilityReferences.Length; i++)
		{
			if (abilityName == abilityReferences[i].Name)
			{
				verifiedCount++;
				if (maxLevel < abilityReferences[i].Level)
				{
					maxLevel = abilityReferences[i].Level;
				}
				if (referenceCountByLevel.Length > 0)
				{
					referenceCountByLevel[abilityReferences[i].Level]++;
				}
			}
		}
	}

	public override void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		this.GUID = reader.GetAttribute<ulong>("GUID");
		this.UnitUnassignedTurnCount = reader.GetAttribute<int>("UnitUnassignedTurnCount");
		this.HasMapBoost = false;
		if (num >= 5)
		{
			this.HasMapBoost = reader.GetAttribute<bool>("HasMapBoost");
			this.EnablesRetrofit = reader.GetAttribute<bool>("EnablesRetrofit");
		}
		base.ReadXml(reader);
		string attribute = reader.GetAttribute("Name");
		UnitRank unitRank = null;
		if (this.unitRankDatabase.TryGetValue(attribute, out unitRank))
		{
			this.currentUnitRank = unitRank;
		}
		reader.Skip("Rank");
		bool flag = false;
		for (int i = 0; i < base.SimulationObject.DescriptorHolders.Count; i++)
		{
			if (base.SimulationObject.DescriptorHolders[i].Descriptor.Name == this.currentUnitRank.LevelUpDescriptor)
			{
				if (flag)
				{
					base.SimulationObject.RemoveDescriptor(base.SimulationObject.DescriptorHolders[i].Descriptor);
					i--;
				}
				else
				{
					flag = true;
				}
			}
		}
		if (reader.IsStartElement("Abilities"))
		{
			int attribute2 = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("Abilities");
			for (int j = 0; j < attribute2; j++)
			{
				string attribute3 = reader.GetAttribute("Name");
				UnitAbility unitAbility = null;
				this.unitAbilityDatabase.TryGetValue(attribute3, out unitAbility);
				if (unitAbility != null)
				{
					Unit.UnitAbilityReferenceCount unitAbilityReferenceCount = new Unit.UnitAbilityReferenceCount(unitAbility, null);
					unitAbilityReferenceCount.CurrentLevel = reader.GetAttribute<int>("Level");
					unitAbilityReferenceCount.ReferenceCount = reader.GetAttribute<int>("ReferenceCount");
					unitAbilityReferenceCount.Disabled = reader.GetAttribute<bool>("Disabled");
					if (unitAbilityReferenceCount.Disabled && unitAbility.Persistent)
					{
						unitAbilityReferenceCount.Disabled = false;
					}
					if (num < 4 && unitAbilityReferenceCount.Disabled)
					{
						this.DisableUnitAbilities(unitAbilityReferenceCount);
					}
					reader.ReadStartElement("Ability");
					if (reader.IsStartElement("Parameters"))
					{
						int attribute4 = reader.GetAttribute<int>("Count");
						unitAbilityReferenceCount.Parameters = new StaticString[attribute4];
						reader.ReadStartElement();
						for (int k = 0; k < attribute4; k++)
						{
							unitAbilityReferenceCount.Parameters[k] = reader.ReadElementString("Parameter");
						}
						reader.ReadEndElement();
					}
					int attribute5 = reader.GetAttribute<int>("Count");
					unitAbilityReferenceCount.ReferenceCountByLevel = new int[attribute5];
					reader.ReadStartElement("Levels");
					for (int l = 0; l < attribute5; l++)
					{
						int attribute6 = reader.GetAttribute<int>("ReferenceCount");
						unitAbilityReferenceCount.ReferenceCountByLevel[l] = attribute6;
						reader.Skip("Level");
					}
					reader.ReadEndElement("Levels");
					reader.ReadEndElement("Ability");
					this.currentAbilities.Add(unitAbilityReferenceCount);
				}
				else
				{
					Diagnostics.LogWarning("Failed to retreive the ability (name: '{0}') from the database.", new object[]
					{
						attribute3
					});
					reader.Skip("Ability");
				}
			}
		}
		reader.ReadEndElement("Abilities");
		int attribute7 = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("Items");
		for (int m = 0; m < attribute7; m++)
		{
			string attribute8 = reader.GetAttribute("Name");
			SimulationObject simulationObject = new SimulationObject(attribute8);
			reader.ReadElementSerializable<SimulationObject>(ref simulationObject);
			if (simulationObject != null)
			{
				base.SimulationObject.AddChild(simulationObject);
			}
		}
		reader.ReadEndElement("Items");
		int attribute9 = reader.GetAttribute<int>("Count");
		if (attribute9 == 0)
		{
			reader.Skip();
		}
		else
		{
			reader.ReadStartElement("Skills");
			for (int n = 0; n < attribute9; n++)
			{
				StaticString key = reader.GetAttribute<string>("Name");
				int attribute10 = reader.GetAttribute<int>("Level");
				this.UnlockedSkillLevelBySkillName.Add(key, attribute10);
				reader.Skip();
			}
			reader.ReadEndElement("Skills");
			foreach (KeyValuePair<StaticString, int> keyValuePair in this.unlockedSkillLevelBySkillName)
			{
				UnitSkill value = this.unitSkillDatabase.GetValue(keyValuePair.Key);
				if (value != null)
				{
					int num2 = 0;
					while (num2 <= keyValuePair.Value && num2 < value.LevelCount)
					{
						if (value.UnitSkillLevels[num2] != null && value.UnitSkillLevels[num2].ParentSimulationDescriptors != null)
						{
							this.deportedDescriptors.AddRange(value.UnitSkillLevels[num2].ParentSimulationDescriptors);
						}
						num2++;
					}
				}
			}
		}
		int attribute11 = reader.GetAttribute<int>("Count");
		if (attribute11 == 0)
		{
			reader.Skip();
		}
		else
		{
			reader.ReadStartElement("CarriedCityImprovements");
			for (int num3 = 0; num3 < attribute11; num3++)
			{
				this.CarriedCityImprovements.Add(reader.GetAttribute<string>("Name"));
				reader.Skip();
			}
			reader.ReadEndElement("CarriedCityImprovements");
		}
		if (num >= 5)
		{
			if (reader.IsStartElement("MapBoosts"))
			{
				int attribute12 = reader.GetAttribute<int>("Count");
				reader.ReadStartElement("MapBoosts");
				for (int num4 = 0; num4 < attribute12; num4++)
				{
					this.AppliedBoosts.Add(reader.ReadElementString("BoostName"));
				}
			}
			reader.ReadEndElement("MapBoosts");
		}
		if (num < 3)
		{
			float propertyValue = this.GetPropertyValue("SpentMovement");
			float propertyValue2 = this.GetPropertyValue("MovementBonus");
			float propertyValue3 = this.GetPropertyValue(SimulationProperties.MaximumMovement);
			float num5 = propertyValue3 - propertyValue + propertyValue2;
			base.SetPropertyBaseValue(SimulationProperties.MovementRatio, (propertyValue3 <= 0f) ? 0f : (num5 / propertyValue3));
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteVersionAttribute(5);
		writer.WriteAttributeString<GameEntityGUID>("GUID", this.GUID);
		writer.WriteAttributeString<int>("UnitUnassignedTurnCount", this.UnitUnassignedTurnCount);
		DepartmentOfDefense.WriteUnitDesignAttributes(writer, this.UnitDesign);
		if (this.UnitDesign.Barcode != 0UL)
		{
			writer.WriteAttributeString<ulong>("UnitDesignBarcode", this.UnitDesign.Barcode);
		}
		writer.WriteAttributeString<bool>("HasMapBoost", this.HasMapBoost);
		writer.WriteAttributeString<bool>("EnablesRetrofit", this.EnablesRetrofit);
		base.WriteXml(writer);
		writer.WriteStartElement("Rank");
		string value = string.Empty;
		if (this.currentUnitRank != null)
		{
			value = this.currentUnitRank.Name;
		}
		writer.WriteAttributeString("Name", value);
		writer.WriteEndElement();
		writer.WriteStartElement("Abilities");
		writer.WriteAttributeString<int>("Count", this.currentAbilities.Count);
		for (int i = 0; i < this.currentAbilities.Count; i++)
		{
			Unit.UnitAbilityReferenceCount unitAbilityReferenceCount = this.currentAbilities[i];
			writer.WriteStartElement("Ability");
			writer.WriteAttributeString<StaticString>("Name", unitAbilityReferenceCount.UnitAbility.Name);
			writer.WriteAttributeString<int>("Level", unitAbilityReferenceCount.CurrentLevel);
			writer.WriteAttributeString<int>("ReferenceCount", unitAbilityReferenceCount.ReferenceCount);
			writer.WriteAttributeString<bool>("Disabled", unitAbilityReferenceCount.Disabled);
			if (unitAbilityReferenceCount.Parameters != null)
			{
				writer.WriteStartElement("Parameters");
				writer.WriteAttributeString<int>("Count", unitAbilityReferenceCount.Parameters.Length);
				for (int j = 0; j < unitAbilityReferenceCount.Parameters.Length; j++)
				{
					writer.WriteElementString<StaticString>("Parameter", unitAbilityReferenceCount.Parameters[j]);
				}
				writer.WriteEndElement();
			}
			writer.WriteStartElement("Levels");
			writer.WriteAttributeString<int>("Count", unitAbilityReferenceCount.ReferenceCountByLevel.Length);
			for (int k = 0; k < unitAbilityReferenceCount.ReferenceCountByLevel.Length; k++)
			{
				writer.WriteStartElement("Level");
				writer.WriteAttributeString<int>("Number", k);
				writer.WriteAttributeString<int>("ReferenceCount", unitAbilityReferenceCount.ReferenceCountByLevel[k]);
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
		writer.WriteStartElement("Items");
		SimulationObject[] array = (from iterator in base.SimulationObject.Children
		where iterator.GetDescriptorFromType("ItemClass") != null
		select iterator).ToArray<SimulationObject>();
		writer.WriteAttributeString<int>("Count", array.Length);
		foreach (SimulationObject xmlSerializable in array)
		{
			writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
		}
		writer.WriteEndElement();
		writer.WriteStartElement("Skills");
		writer.WriteAttributeString<int>("Count", this.unlockedSkillLevelBySkillName.Count);
		foreach (KeyValuePair<StaticString, int> keyValuePair in this.unlockedSkillLevelBySkillName)
		{
			writer.WriteStartElement("Skill");
			writer.WriteAttributeString<string>("Name", keyValuePair.Key);
			writer.WriteAttributeString<int>("Level", keyValuePair.Value);
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
		writer.WriteStartElement("CarriedCityImprovements");
		writer.WriteAttributeString<int>("Count", this.carriedCityImprovements.Count);
		for (int m = 0; m < this.CarriedCityImprovements.Count; m++)
		{
			writer.WriteStartElement("CarriedCityImprovement");
			writer.WriteAttributeString<string>("Name", this.CarriedCityImprovements[m]);
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
		writer.WriteStartElement("MapBoosts");
		writer.WriteAttributeString<int>("Count", this.AppliedBoosts.Count);
		for (int n = 0; n < this.AppliedBoosts.Count; n++)
		{
			writer.WriteElementString<StaticString>("BoostName", this.AppliedBoosts[n]);
		}
		writer.WriteEndElement();
	}

	public Dictionary<StaticString, int> UnlockedSkillLevelBySkillName
	{
		get
		{
			return this.unlockedSkillLevelBySkillName;
		}
	}

	public void UnlockSkill(StaticString skillName, int level = 0)
	{
		int num = -1;
		if (this.IsSkillUnlocked(skillName))
		{
			if (this.unlockedSkillLevelBySkillName[skillName] >= level)
			{
				return;
			}
			num = this.unlockedSkillLevelBySkillName[skillName];
		}
		UnitSkill value = this.unitSkillDatabase.GetValue(skillName);
		if (level < 0 || level >= value.UnitSkillLevels.Length)
		{
			return;
		}
		float num2 = this.GetPropertyValue(SimulationProperties.SkillPointsSpent);
		for (int i = num + 1; i <= level; i++)
		{
			UnitSkillLevel unitSkillLevel = value.UnitSkillLevels[i];
			if (unitSkillLevel.UnitAbilities != null)
			{
				this.AddUnitAbilities(unitSkillLevel.UnitAbilities);
			}
			if (unitSkillLevel.SimulationDescriptorReferences != null)
			{
				for (int j = 0; j < unitSkillLevel.SimulationDescriptorReferences.Length; j++)
				{
					SimulationDescriptor descriptor;
					if (this.simulationDescriptorDatabase.TryGetValue(unitSkillLevel.SimulationDescriptorReferences[j], out descriptor))
					{
						base.AddDescriptor(descriptor, false);
					}
				}
			}
			if (unitSkillLevel.ParentSimulationDescriptors != null)
			{
				this.ApplyDeportedDescriptor(unitSkillLevel.ParentSimulationDescriptors);
				this.deportedDescriptors.AddRange(unitSkillLevel.ParentSimulationDescriptors);
			}
			num2 += (float)unitSkillLevel.UnitSkillPointCost;
		}
		base.SetPropertyBaseValue(SimulationProperties.SkillPointsSpent, num2);
		if (this.IsSkillUnlocked(skillName))
		{
			this.unlockedSkillLevelBySkillName[skillName] = level;
		}
		else
		{
			this.unlockedSkillLevelBySkillName.Add(skillName, level);
			if (value.SimulationDescriptorReferences != null)
			{
				for (int k = 0; k < value.SimulationDescriptorReferences.Length; k++)
				{
					SimulationDescriptor descriptor;
					if (this.simulationDescriptorDatabase.TryGetValue(value.SimulationDescriptorReferences[k], out descriptor))
					{
						base.AddDescriptor(descriptor, false);
					}
				}
			}
		}
		if (this.Garrison != null && this.Garrison is SimulationObjectWrapper)
		{
			(this.Garrison as SimulationObjectWrapper).Refresh(false);
			if (this.Garrison is City)
			{
				(this.Garrison as City).Districts[0].LineOfSightDirty = true;
			}
		}
		this.Refresh(false);
		this.OnUnitSkillChange(skillName, level);
	}

	public void RevertSkillAtLevel(StaticString skillName, int level = -1)
	{
		if (this.IsSkillUnlocked(skillName))
		{
			int num = this.unlockedSkillLevelBySkillName[skillName];
			if (num <= level)
			{
				return;
			}
			UnitSkill value = this.unitSkillDatabase.GetValue(skillName);
			float num2 = this.GetPropertyValue(SimulationProperties.SkillPointsSpent);
			for (int i = num; i > level; i--)
			{
				UnitSkillLevel unitSkillLevel = value.UnitSkillLevels[i];
				if (unitSkillLevel.UnitAbilities != null)
				{
					this.RemoveUnitAbilities(unitSkillLevel.UnitAbilities);
				}
				if (unitSkillLevel.SimulationDescriptorReferences != null)
				{
					for (int j = 0; j < unitSkillLevel.SimulationDescriptorReferences.Length; j++)
					{
						base.RemoveDescriptorByName(unitSkillLevel.SimulationDescriptorReferences[j]);
					}
				}
				if (unitSkillLevel.ParentSimulationDescriptors != null)
				{
					this.RemoveDeportedDescriptor(unitSkillLevel.ParentSimulationDescriptors);
					for (int k = 0; k < unitSkillLevel.ParentSimulationDescriptors.Length; k++)
					{
						this.deportedDescriptors.Remove(unitSkillLevel.ParentSimulationDescriptors[k]);
					}
				}
				num2 -= (float)unitSkillLevel.UnitSkillPointCost;
			}
			float propertyValue = this.GetPropertyValue(SimulationProperties.MaximumSkillPoints);
			if (num2 > propertyValue)
			{
				num2 = propertyValue;
			}
			base.SetPropertyBaseValue(SimulationProperties.SkillPointsSpent, num2);
			if (level < 0)
			{
				this.unlockedSkillLevelBySkillName.Remove(skillName);
				if (value.SimulationDescriptorReferences != null)
				{
					for (int l = 0; l < value.SimulationDescriptorReferences.Length; l++)
					{
						base.RemoveDescriptorByName(value.SimulationDescriptorReferences[l]);
					}
				}
			}
			else
			{
				this.unlockedSkillLevelBySkillName[skillName] = level;
			}
			this.OnUnitSkillChange(skillName, level);
			this.Refresh(false);
		}
	}

	public bool IsHero()
	{
		UnitProfile unitProfile = this.UnitDesign as UnitProfile;
		return unitProfile != null && unitProfile.IsHero;
	}

	public bool IsSkillUnlocked(StaticString skillName)
	{
		return this.unlockedSkillLevelBySkillName.ContainsKey(skillName);
	}

	public int GetSkillLevel(StaticString skillName)
	{
		if (!this.IsSkillUnlocked(skillName))
		{
			return -1;
		}
		return this.unlockedSkillLevelBySkillName[skillName];
	}

	private void ApplyCurrentDeportedDescriptors()
	{
		this.ApplyDeportedDescriptor(this.deportedDescriptors.ToArray());
	}

	private void OnUnitSkillChange(StaticString unitSkillName, int level)
	{
		if (this.UnitSkillChange != null)
		{
			this.UnitSkillChange(this, new UnitSkillChangeEventArgs(unitSkillName, level));
		}
	}

	private void RemoveCurrentDeportedDescriptors()
	{
		this.RemoveDeportedDescriptor(this.deportedDescriptors.ToArray());
	}

	public List<StaticString> CarriedCityImprovements
	{
		get
		{
			return this.carriedCityImprovements;
		}
	}

	public IGarrison Garrison { get; private set; }

	public GameEntityGUID GUID { get; private set; }

	public bool HasMapBoost { get; set; }

	public bool EnablesRetrofit { get; set; }

	public List<StaticString> AppliedBoosts { get; set; }

	public bool Embarked
	{
		get
		{
			return base.SimulationObject != null && base.SimulationObject.Tags.Contains(DownloadableContent16.TransportShipUnit);
		}
	}

	public PathfindingContextMode PathfindingContextMode { get; set; }

	public int UnitUnassignedTurnCount { get; set; }

	public UnitDesign UnitDesign { get; internal set; }

	public bool IsSeafaring
	{
		get
		{
			return base.SimulationObject.Tags.Contains(DownloadableContent16.SeafaringUnit);
		}
	}

	public void AddCarriedCityImprovement(CityImprovement cityImprovement)
	{
		if (cityImprovement == null)
		{
			return;
		}
		this.carriedCityImprovements.Add(cityImprovement.CityImprovementDefinition.Name);
	}

	public void ChangeGarrison(IGarrison newGarrison)
	{
		if (this.Garrison != null)
		{
			this.RemoveCurrentDeportedDescriptors();
			this.Garrison = null;
		}
		this.Garrison = newGarrison;
		if (this.Garrison != null)
		{
			this.ApplyCurrentDeportedDescriptors();
		}
	}

	public void CopySkillsFrom(Unit unit)
	{
		foreach (KeyValuePair<StaticString, int> keyValuePair in unit.unlockedSkillLevelBySkillName)
		{
			this.UnlockSkill(keyValuePair.Key, keyValuePair.Value);
		}
	}

	public void CopyRanksFrom(Unit unit)
	{
		Unit.<CopyRanksFrom>c__AnonStorey91A <CopyRanksFrom>c__AnonStorey91A = new Unit.<CopyRanksFrom>c__AnonStorey91A();
		<CopyRanksFrom>c__AnonStorey91A.unit = unit;
		int index;
		for (index = 0; index < <CopyRanksFrom>c__AnonStorey91A.unit.unitRanks.Count; index++)
		{
			if (!this.unitRanks.Exists((UnitRank match) => match.Name == <CopyRanksFrom>c__AnonStorey91A.unit.unitRanks[index].Name))
			{
				this.ForceRank(<CopyRanksFrom>c__AnonStorey91A.unit.unitRanks[index].Name, (int)<CopyRanksFrom>c__AnonStorey91A.unit.GetPropertyValue(<CopyRanksFrom>c__AnonStorey91A.unit.unitRanks[index].LevelPropertyName));
			}
		}
	}

	public void CopySerializablePropertiesFrom(Unit unit)
	{
		for (int i = 0; i < base.SimulationObject.Properties.Count; i++)
		{
			if (base.SimulationObject.Properties.Data[i].IsSerializable)
			{
				SimulationProperty property = unit.SimulationObject.GetProperty(base.SimulationObject.Properties.Data[i].Name);
				if (property != null)
				{
					base.SimulationObject.CloneProperty(property);
				}
			}
		}
	}

	public PathfindingContext GenerateContext()
	{
		if (this.pathfindingContext == null || this.pathfindingContext.Empire != this.Garrison.Empire)
		{
			Diagnostics.Assert(this.Garrison != null && this.Garrison.Empire != null);
			this.pathfindingContext = new PathfindingContext(this.Garrison.GUID, this.Garrison.Empire, this);
		}
		else
		{
			this.pathfindingContext.RefreshMovementCapacity(this);
		}
		Army army = this.Garrison as Army;
		if (army != null && army.SimulationObject.Tags.Contains(PathfindingContext.MovementCapacitySailDescriptor))
		{
			this.pathfindingContext.AddMovementCapacity(PathfindingMovementCapacity.Water);
		}
		if (army != null && army.Hero != null && army.Hero.SimulationObject.Tags.Contains(PathfindingContext.HeroSkillPathfinderDescriptor))
		{
			this.pathfindingContext.AddMovementCapacity(PathfindingMovementCapacity.Crusher);
		}
		if (army != null && army.Hero != null && army.Hero.SimulationObject.Tags.Contains(PathfindingContext.HeroSkillLordOfTheSeaDescriptor))
		{
			this.pathfindingContext.AddMovementCapacity(PathfindingMovementCapacity.IgnoreFlotsam);
		}
		if (this.Garrison is Fortress && (this.pathfindingContext.MovementCapacities & PathfindingMovementCapacity.Water) == PathfindingMovementCapacity.None && this.Embarked)
		{
			this.pathfindingContext.AddMovementCapacity(PathfindingMovementCapacity.Water);
		}
		PathfindingContextMode pathfindingContextMode = this.PathfindingContextMode;
		if (pathfindingContextMode != PathfindingContextMode.Encounter)
		{
			if (pathfindingContextMode == PathfindingContextMode.NavalEncounter)
			{
				this.pathfindingContext.RemoveMovementCapacity(PathfindingMovementCapacity.Ground);
			}
		}
		else
		{
			this.pathfindingContext.RemoveMovementCapacity(PathfindingMovementCapacity.Water);
		}
		if (SimulationGlobal.GlobalTagsContains(DownloadableContent13.FrozenTile) && !this.UnitDesign.Tags.Contains(DownloadableContent16.TagSeafaring))
		{
			this.pathfindingContext.AddMovementCapacity(PathfindingMovementCapacity.FrozenWater);
		}
		bool isPrivateers = army != null && army.IsPrivateers;
		bool isCamouflaged = army != null && army.IsCamouflaged;
		this.pathfindingContext.RefreshProperties(this.GetPropertyValue(SimulationProperties.MovementRatio), this.GetPropertyValue(SimulationProperties.MaximumMovement), isPrivateers, isCamouflaged, this.GetPropertyValue(SimulationProperties.MaximumMovementOnLand), this.GetPropertyValue(SimulationProperties.MaximumMovementOnWater));
		this.pathfindingContext.Goal = WorldPosition.Invalid;
		if (SimulationGlobal.GlobalTagsContains(Season.ReadOnlyWinter))
		{
			if (this.Garrison != null && this.Garrison.Empire != null && this.Garrison.Empire.SimulationObject.Tags.Contains(SeasonEffect.SeasonEffectMovement2) && (this.pathfindingContext.MovementCapacities & PathfindingMovementCapacity.Water) != PathfindingMovementCapacity.None)
			{
				this.pathfindingContext.AddMovementCapacity(PathfindingMovementCapacity.FasterWater);
			}
			if (base.SimulationObject.Tags.Contains(DownloadableContent13.MovementCapacityIceWalker))
			{
				this.pathfindingContext.AddMovementCapacity(PathfindingMovementCapacity.FrozenRiverSurfer);
			}
		}
		return this.pathfindingContext;
	}

	public bool IsWounded()
	{
		return this.GetPropertyValue(SimulationProperties.Health) < this.GetPropertyValue(SimulationProperties.MaximumHealth);
	}

	public bool HasEnoughActionPointLeft(int numberOfPointLeft)
	{
		float propertyValue = this.GetPropertyValue(SimulationProperties.MaximumNumberOfActionPoints);
		float propertyValue2 = this.GetPropertyValue(SimulationProperties.ActionPointsSpent);
		return (float)numberOfPointLeft <= propertyValue - propertyValue2;
	}

	public bool CanShift()
	{
		return this.IsShifter() && this.IsInCurrentSeasonForm();
	}

	public void OnWorldTerrainDamageReceived()
	{
		if (this.WorldTerrainDamageReceived != null)
		{
			this.WorldTerrainDamageReceived(this, null);
		}
	}

	public void SwitchToEmbarkedUnit(bool active)
	{
		if (active)
		{
			if (!base.SimulationObject.Tags.Contains(DownloadableContent16.TransportShipUnit))
			{
				IDatabase<SimulationDescriptor> database = Databases.GetDatabase<SimulationDescriptor>(false);
				SimulationDescriptor value = database.GetValue(DownloadableContent16.TransportShipUnit);
				if (value != null)
				{
					base.AddDescriptor(value, false);
				}
				List<Unit.UnitAbilityReferenceCount> list = (from unitAbilityReferenceCount in this.currentAbilities
				where !unitAbilityReferenceCount.UnitAbility.Persistent
				select unitAbilityReferenceCount).ToList<Unit.UnitAbilityReferenceCount>();
				IDatabase<UnitBodyDefinition> database2 = Databases.GetDatabase<UnitBodyDefinition>(false);
				UnitBodyDefinition unitBodyDefinition;
				if (database2 != null && database2.TryGetValue("UnitBodyTransportShip", out unitBodyDefinition))
				{
					if (unitBodyDefinition.UnitAbilities != null)
					{
						for (int i = 0; i < unitBodyDefinition.UnitAbilities.Length; i++)
						{
							StaticString abilityReferenceName = unitBodyDefinition.UnitAbilities[i].Name;
							Unit.UnitAbilityReferenceCount unitAbilityReferenceCount4 = this.currentAbilities.FirstOrDefault((Unit.UnitAbilityReferenceCount unitAbility) => unitAbility.UnitAbility.Name == abilityReferenceName);
							if (unitAbilityReferenceCount4 == null)
							{
								unitAbilityReferenceCount4 = this.AddUnitAbility(abilityReferenceName, 1, null);
								if (unitAbilityReferenceCount4 != null)
								{
									unitAbilityReferenceCount4.Disabled = false;
								}
							}
							else if (!unitAbilityReferenceCount4.UnitAbility.Persistent)
							{
								Diagnostics.LogWarning("Ability '{0}' present in 'UnitBodyTransportShip' will be disabled, make ability 'Persistent'.", new object[]
								{
									unitAbilityReferenceCount4.UnitAbility.Name
								});
							}
						}
					}
				}
				else
				{
					Unit.UnitAbilityReferenceCount unitAbilityReferenceCount2 = this.currentAbilities.FirstOrDefault((Unit.UnitAbilityReferenceCount unitAbility) => unitAbility.UnitAbility.Name == UnitAbility.ReadonlyTransportShip);
					if (unitAbilityReferenceCount2 == null)
					{
						unitAbilityReferenceCount2 = this.AddUnitAbility(UnitAbility.ReadonlyTransportShip, 1, null);
						if (unitAbilityReferenceCount2 != null)
						{
							unitAbilityReferenceCount2.Disabled = false;
						}
					}
					else if (!unitAbilityReferenceCount2.UnitAbility.Persistent)
					{
						Diagnostics.LogWarning("Ability '{0}' present in 'UnitBodyTransportShip' will be disabled, make ability 'Persistent'.", new object[]
						{
							unitAbilityReferenceCount2.UnitAbility.Name
						});
					}
				}
				foreach (Unit.UnitAbilityReferenceCount referenceCount in list)
				{
					this.DisableUnitAbilities(referenceCount);
				}
			}
		}
		else if (base.SimulationObject.Tags.Contains(DownloadableContent16.TransportShipUnit))
		{
			base.RemoveDescriptorByName(DownloadableContent16.TransportShipUnit);
			IDatabase<UnitBodyDefinition> database3 = Databases.GetDatabase<UnitBodyDefinition>(false);
			UnitBodyDefinition unitBodyDefinition2;
			if (database3 != null && database3.TryGetValue("UnitBodyTransportShip", out unitBodyDefinition2))
			{
				if (unitBodyDefinition2.UnitAbilities != null)
				{
					for (int j = 0; j < unitBodyDefinition2.UnitAbilities.Length; j++)
					{
						StaticString name = unitBodyDefinition2.UnitAbilities[j].Name;
						this.RemoveUnitAbility(name, 1);
					}
				}
			}
			else
			{
				this.RemoveUnitAbility(UnitAbility.ReadonlyTransportShip, 1);
			}
			List<Unit.UnitAbilityReferenceCount> list2 = (from unitAbilityReferenceCount in this.currentAbilities
			where unitAbilityReferenceCount.Disabled
			select unitAbilityReferenceCount).ToList<Unit.UnitAbilityReferenceCount>();
			foreach (Unit.UnitAbilityReferenceCount unitAbilityReferenceCount3 in list2)
			{
				this.ReenableUnitAbility(unitAbilityReferenceCount3, unitAbilityReferenceCount3.CurrentLevel);
			}
		}
		this.Refresh(false);
	}

	internal void RetrofitTo(UnitDesign newUnitDesign)
	{
		this.UnitDesign = newUnitDesign;
		DepartmentOfDefense.ApplyEquipmentSet(this);
		this.Refresh(true);
		this.NotifyUnitRetrofited();
	}

	internal void UpdateExperienceReward(Amplitude.Unity.Game.Empire owner)
	{
		float value = DepartmentOfTheTreasury.ConvertCostsTo(SimulationProperties.UnitValue, this.UnitDesign, owner);
		base.SetPropertyBaseValue(SimulationProperties.UnitValue, value);
	}

	internal void UpdateShiftingForm()
	{
		if (base.SimulationObject == null || base.SimulationObject.Tags == null)
		{
			return;
		}
		if (base.SimulationObject.Tags.Contains(DownloadableContent13.AffinityShifters))
		{
			base.SetPropertyBaseValue("ShiftingForm", (float)((!SimulationGlobal.GlobalTagsContains("Winter")) ? 0 : 1));
		}
	}

	internal bool IsShifter()
	{
		return this.CheckUnitAbility(UnitAbility.ReadonlyShifterNature, -1);
	}

	internal bool IsInCurrentSeasonForm()
	{
		int num = (int)base.SimulationObject.GetPropertyValue("ShiftingForm");
		if (this.IsShifter())
		{
			if (num == 0 && SimulationGlobal.GlobalTagsContains("Winter"))
			{
				return false;
			}
			if (num == 1 && (SimulationGlobal.GlobalTagsContains("Summer") || SimulationGlobal.GlobalTagsContains("HeatWave")))
			{
				return false;
			}
		}
		return true;
	}

	internal bool IsImmolableUnit()
	{
		return this.CheckUnitAbility(UnitAbility.UnitAbilityImmolation, -1);
	}

	internal bool IsAlreadyImmolated()
	{
		int num = (int)base.SimulationObject.GetPropertyValue("ImmolationState");
		if (this.IsImmolableUnit())
		{
			if (num == 0)
			{
				return false;
			}
			if (num == 1)
			{
				return true;
			}
		}
		return false;
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		this.Garrison = null;
		this.UnitDesign = null;
		this.currentUnitRank = null;
		this.currentAbilities.Clear();
		this.unitRanks.Clear();
		this.carriedCityImprovements.Clear();
	}

	private void NotifyUnitRetrofited()
	{
		if (this.UnitRetrofited != null)
		{
			this.UnitRetrofited(this, new EventArgs());
		}
	}

	public void AddMapBoost(string boostName, SimulationDescriptor simulationDescriptor)
	{
		if (!this.AppliedBoosts.Contains(boostName))
		{
			this.AppliedBoosts.Add(boostName);
		}
		if (base.SimulationObject != null)
		{
			base.AddDescriptor(simulationDescriptor, false);
		}
		if (this.Garrison is Army)
		{
			Army army = this.Garrison as Army;
			army.CheckMapBoostOnUnits();
		}
	}

	public void RemoveMapBoost(string boostName, SimulationDescriptor simulationDescriptor)
	{
		if (this.AppliedBoosts.Contains(boostName))
		{
			this.AppliedBoosts.Remove(boostName);
		}
		if (base.SimulationObject != null)
		{
			base.RemoveDescriptor(simulationDescriptor);
		}
		if (this.Garrison is Army)
		{
			Army army = this.Garrison as Army;
			army.CheckMapBoostOnUnits();
		}
	}

	public bool IsSettler
	{
		get
		{
			return this.CheckUnitAbility(UnitAbility.ReadonlyColonize, -1) || this.CheckUnitAbility(UnitAbility.ReadonlyResettle, -1);
		}
	}

	private List<UnitRank> unitRanks;

	private UnitRank currentUnitRank;

	private List<Unit.UnitAbilityReferenceCount> currentAbilities = new List<Unit.UnitAbilityReferenceCount>();

	private IDatabase<UnitSkill> unitSkillDatabase;

	private Dictionary<StaticString, int> unlockedSkillLevelBySkillName = new Dictionary<StaticString, int>();

	private List<ParentSimulationDescriptorReference> deportedDescriptors = new List<ParentSimulationDescriptorReference>();

	public static readonly string ReadOnlyCategory = "Unit";

	public static readonly string ResourceNameActionPoint = "ActionPoint";

	public static readonly string ReadOnlyColossus = "UnitTypeColossus";

	private IDatabase<UnitRank> unitRankDatabase;

	private IDatabase<SimulationDescriptor> simulationDescriptorDatabase;

	private IDatabase<UnitAbility> unitAbilityDatabase;

	private IEventService eventService;

	private List<StaticString> carriedCityImprovements;

	private GuiElement guiElement;

	private PathfindingContext pathfindingContext;

	public class UnitAbilityReferenceCount
	{
		public UnitAbilityReferenceCount(UnitAbility unitAbility, StaticString[] parameters)
		{
			this.UnitAbility = unitAbility;
			int num = 0;
			if (unitAbility.AbilityLevels != null)
			{
				num = unitAbility.AbilityLevels.Length;
			}
			this.ReferenceCountByLevel = new int[num];
			this.CurrentLevel = -1;
			this.Parameters = parameters;
		}

		public int ReferenceCount { get; set; }

		public int CurrentLevel { get; set; }

		public int[] ReferenceCountByLevel { get; set; }

		public UnitAbility UnitAbility { get; private set; }

		public StaticString[] Parameters { get; set; }

		public bool Disabled { get; set; }
	}
}
