using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Amplitude;
using Amplitude.Extensions;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Session;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class AILayer_Attitude : AILayer, IXmlSerializable
{
	public AILayer_Attitude()
	{
		this.empireLastAggressorIndex = new int[]
		{
			-1,
			-1,
			-1,
			-1,
			-1,
			-1,
			-1,
			-1
		};
		this.LastWarHelpInquiry = new Dictionary<int, int>
		{
			{
				0,
				-10
			},
			{
				1,
				-10
			},
			{
				2,
				-10
			},
			{
				3,
				-10
			},
			{
				4,
				-10
			},
			{
				5,
				-10
			},
			{
				6,
				-10
			},
			{
				7,
				-10
			}
		};
		this.LastWarHelpTarget = new Dictionary<int, int>
		{
			{
				0,
				-1
			},
			{
				1,
				-1
			},
			{
				2,
				-1
			},
			{
				3,
				-1
			},
			{
				4,
				-1
			},
			{
				5,
				-1
			},
			{
				6,
				-1
			},
			{
				7,
				-1
			}
		};
		this.refreshedUnitGuidCache = new List<GameEntityGUID>();
		this.refreshedRegionIndex = new List<int>();
		this.scoresByNameBuffer = new Dictionary<StaticString, float>();
		this.empireCache = new List<global::Empire>(8);
		this.sameRegionFortresses = new List<Fortress>();
	}

	private bool ShouldITakeEncounterIntoAccount(Encounter encounter)
	{
		List<Contender> contenders = encounter.Contenders;
		if (encounter.Contenders.Count <= 0)
		{
			AILayer.LogError("An encounter without contender has ended.");
			return false;
		}
		for (int i = 0; i < encounter.Empires.Count; i++)
		{
			if (!(encounter.Empires[i] is MajorEmpire))
			{
				return false;
			}
		}
		bool flag = true;
		bool flag2 = true;
		for (int j = 0; j < contenders.Count; j++)
		{
			if (contenders[j].IsTakingPartInBattle)
			{
				if (contenders[j].Empire == this.Empire)
				{
					flag &= contenders[j].IsPrivateers;
				}
				else
				{
					flag2 &= contenders[j].IsPrivateers;
				}
			}
		}
		return !flag && (this.maySeeThroughCatspaw || !flag2);
	}

	private void ArmyAggressionInNeutralRegionDuringColdWar(Encounter encounter)
	{
		Contender contender = encounter.Contenders[0];
		global::Empire empire = contender.Empire;
		if (empire.Index == this.Empire.Index)
		{
			return;
		}
		AILayer_Attitude.Attitude attitude = this.attitudeScores[empire.Index];
		attitude.Score.RemoveModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == AILayer_Attitude.AttitudeScoreDefinitionReferences.Peaceful);
		DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(empire);
		Diagnostics.Assert(diplomaticRelation != null && diplomaticRelation.State != null);
		if (diplomaticRelation.State.Name != DiplomaticRelationState.Names.ColdWar)
		{
			return;
		}
		attitude.AddScoreModifier(this.attitudeScoreArmyAggressionInNeutralRegionDuringColdWar, 1f);
	}

	private void CitySiegeUpdate(City city)
	{
		Diagnostics.Assert(city != null);
		if (city.BesiegingEmpire == null)
		{
			for (int i = 0; i < this.attitudeScores.Length; i++)
			{
				if (i != this.Empire.Index)
				{
					AILayer_Attitude.Attitude attitude = this.attitudeScores[i];
					Diagnostics.Assert(attitude != null);
					AILayer_Attitude.Attitude.CityModifiersInfo cityModifiersInfo;
					if (attitude.CityModifiersInfoByCityGuid.TryGetValue(city.GUID, out cityModifiersInfo) && cityModifiersInfo.CityBesiegedModifierId >= 0)
					{
						attitude.Score.RemoveModifier(cityModifiersInfo.CityBesiegedModifierId);
						cityModifiersInfo.CityBesiegedModifierId = -1;
					}
				}
			}
		}
		if (city.Empire.Index == this.Empire.Index && city.BesiegingEmpire is MajorEmpire)
		{
			AILayer_Attitude.Attitude attitude2 = this.attitudeScores[city.BesiegingEmpireIndex];
			Diagnostics.Assert(attitude2 != null && attitude2.Score != null);
			AILayer_Attitude.Attitude.CityModifiersInfo cityModifiersInfo2;
			if (attitude2.CityModifiersInfoByCityGuid.TryGetValue(city.GUID, out cityModifiersInfo2))
			{
				if (cityModifiersInfo2.CityBesiegedModifierId >= 0)
				{
					attitude2.Score.RemoveModifier(cityModifiersInfo2.CityBesiegedModifierId);
					cityModifiersInfo2.CityBesiegedModifierId = -1;
				}
			}
			else
			{
				attitude2.CityModifiersInfoByCityGuid.Add(city.GUID, new AILayer_Attitude.Attitude.CityModifiersInfo());
			}
			int num = attitude2.AddScoreModifier(this.myEmpireCityBesiegedDuringWarDefinition, 1f);
			if (num >= 0)
			{
				attitude2.CityModifiersInfoByCityGuid[city.GUID].CityBesiegedModifierId = num;
			}
			attitude2.Score.RemoveModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == AILayer_Attitude.AttitudeScoreDefinitionReferences.Peaceful);
		}
		if (city.Empire.Index != this.Empire.Index && city.BesiegingEmpireIndex == this.Empire.Index)
		{
			AILayer_Attitude.Attitude attitude3 = this.attitudeScores[city.Empire.Index];
			AILayer_Attitude.Attitude.CityModifiersInfo cityModifiersInfo3;
			if (attitude3.CityModifiersInfoByCityGuid.TryGetValue(city.GUID, out cityModifiersInfo3))
			{
				if (cityModifiersInfo3.CityBesiegedModifierId >= 0)
				{
					AILayer.LogError("Attitude already contains a besieged modifier.");
					attitude3.Score.RemoveModifier(cityModifiersInfo3.CityBesiegedModifierId);
					cityModifiersInfo3.CityBesiegedModifierId = -1;
				}
			}
			else
			{
				attitude3.CityModifiersInfoByCityGuid.Add(city.GUID, new AILayer_Attitude.Attitude.CityModifiersInfo());
			}
			int num2 = attitude3.AddScoreModifier(this.otherEmpireCityBesiegedDuringWarDefinition, 1f);
			if (num2 >= 0)
			{
				attitude3.CityModifiersInfoByCityGuid[city.GUID].CityBesiegedModifierId = num2;
			}
		}
	}

	private void CityBombardmentUpdate(City city)
	{
		Diagnostics.Assert(city != null);
		if (city.Empire.Index == this.Empire.Index)
		{
			int index;
			for (index = 0; index < this.majorEmpires.Length; index++)
			{
				if (index != this.Empire.Index)
				{
					bool add = city.BesiegingSeafaringArmies.Exists((Army match) => match.Empire.Index == index);
					this.UpdateCityNavalModifier(index, city.GUID, this.myEmpireCitiesBombardedDuringWarDefinition, add);
				}
			}
		}
		else
		{
			bool add2 = city.BesiegingSeafaringArmies.Exists((Army match) => match.Empire.Index == this.Empire.Index);
			this.UpdateCityNavalModifier(city.Empire.Index, city.GUID, this.otherEmpireCitiesBombardedDuringWarDefinition, add2);
		}
	}

	private void UpdateCityNavalModifier(int otherEmpireIndex, GameEntityGUID cityGuid, DiplomaticRelationScoreModifierDefinition definition, bool add)
	{
		AILayer_Attitude.Attitude attitude = this.attitudeScores[otherEmpireIndex];
		AILayer_Attitude.Attitude.CityModifiersInfo cityModifiersInfo;
		if (!attitude.CityModifiersInfoByCityGuid.TryGetValue(cityGuid, out cityModifiersInfo) && add)
		{
			cityModifiersInfo = new AILayer_Attitude.Attitude.CityModifiersInfo();
			attitude.CityModifiersInfoByCityGuid.Add(cityGuid, cityModifiersInfo);
		}
		if (add)
		{
			if (cityModifiersInfo.CityNavalBesiegedModifierId >= 0)
			{
				DiplomaticRelationScoreModifier modifier = attitude.Score.GetModifier(cityModifiersInfo.CityNavalBesiegedModifierId);
				if (modifier.Definition != definition)
				{
					attitude.Score.RemoveModifier(cityModifiersInfo.CityNavalBesiegedModifierId);
					cityModifiersInfo.CityNavalBesiegedModifierId = -1;
				}
			}
			if (cityModifiersInfo.CityNavalBesiegedModifierId < 0)
			{
				int num = attitude.AddScoreModifier(definition, 1f);
				if (num >= 0)
				{
					cityModifiersInfo.CityNavalBesiegedModifierId = num;
				}
			}
		}
		else if (cityModifiersInfo != null && cityModifiersInfo.CityNavalBesiegedModifierId >= 0)
		{
			attitude.Score.RemoveModifier(cityModifiersInfo.CityNavalBesiegedModifierId);
			cityModifiersInfo.CityNavalBesiegedModifierId = -1;
		}
	}

	private void CityRazed(Region region, int victimEmpireIndex, int razingEmpireIndex)
	{
		if (victimEmpireIndex == this.Empire.Index && razingEmpireIndex < this.majorEmpires.Length)
		{
			AILayer_Attitude.Attitude attitude = this.attitudeScores[razingEmpireIndex];
			Diagnostics.Assert(attitude != null && attitude.Score != null);
			attitude.AddScoreModifier(this.myEmpireCitiesTakenDuringWarDefinition, 1f);
			attitude.Score.RemoveModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == AILayer_Attitude.AttitudeScoreDefinitionReferences.Peaceful);
		}
	}

	private void CityTaken(City city, int oldOwnerEmpireIndex, int newOwnerEmpireIndex)
	{
		Diagnostics.Assert(city != null);
		if (oldOwnerEmpireIndex == this.Empire.Index && newOwnerEmpireIndex < this.majorEmpires.Length)
		{
			AILayer_Attitude.Attitude attitude = this.attitudeScores[newOwnerEmpireIndex];
			Diagnostics.Assert(attitude != null && attitude.Score != null);
			bool flag = true;
			AILayer_Attitude.Attitude.CityModifiersInfo cityModifiersInfo;
			if (attitude.CityModifiersInfoByCityGuid.TryGetValue(city.GUID, out cityModifiersInfo))
			{
				if (cityModifiersInfo.CityTakenModifierId >= 0)
				{
					attitude.Score.RemoveModifier(cityModifiersInfo.CityTakenModifierId);
					cityModifiersInfo.CityTakenModifierId = -1;
					flag = false;
				}
			}
			else
			{
				attitude.CityModifiersInfoByCityGuid.Add(city.GUID, new AILayer_Attitude.Attitude.CityModifiersInfo());
			}
			if (flag)
			{
				int num = attitude.AddScoreModifier(this.myEmpireCitiesTakenDuringWarDefinition, 1f);
				if (num >= 0)
				{
					attitude.CityModifiersInfoByCityGuid[city.GUID].CityTakenModifierId = num;
				}
			}
			attitude.Score.RemoveModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == AILayer_Attitude.AttitudeScoreDefinitionReferences.Peaceful);
		}
		if (oldOwnerEmpireIndex != this.Empire.Index && newOwnerEmpireIndex == this.Empire.Index)
		{
			AILayer_Attitude.Attitude attitude2 = this.attitudeScores[oldOwnerEmpireIndex];
			bool flag2 = true;
			AILayer_Attitude.Attitude.CityModifiersInfo cityModifiersInfo2;
			if (attitude2.CityModifiersInfoByCityGuid.TryGetValue(city.GUID, out cityModifiersInfo2))
			{
				if (cityModifiersInfo2.CityTakenModifierId >= 0)
				{
					attitude2.Score.RemoveModifier(cityModifiersInfo2.CityTakenModifierId);
					cityModifiersInfo2.CityTakenModifierId = -1;
					flag2 = false;
				}
			}
			else
			{
				attitude2.CityModifiersInfoByCityGuid.Add(city.GUID, new AILayer_Attitude.Attitude.CityModifiersInfo());
			}
			if (flag2)
			{
				int num2 = attitude2.AddScoreModifier(this.otherEmpireCitiesTakenDuringWarDefinition, 1f);
				if (num2 >= 0)
				{
					attitude2.CityModifiersInfoByCityGuid[city.GUID].CityTakenModifierId = num2;
				}
			}
		}
	}

	private void InitializeAggression()
	{
		Diagnostics.Assert(this.diplomaticRelationScoreModifierDatabase != null);
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.ArmyAggressionInNeutralRegionDuringColdWar, out this.attitudeScoreArmyAggressionInNeutralRegionDuringColdWar))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.ArmyAggressionInNeutralRegionDuringColdWar
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireUnitsKilledDuringWar, out this.myEmpireUnitsKilledDuringWarDefinition))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireUnitsKilledDuringWar
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireCityBesiegedDuringWar, out this.myEmpireCityBesiegedDuringWarDefinition))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireCityBesiegedDuringWar
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireCitiesTakenDuringWar, out this.myEmpireCitiesTakenDuringWarDefinition))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireCitiesTakenDuringWar
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireUnitsKilledDuringWar, out this.otherEmpireUnitsKilledDuringWarDefinition))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireUnitsKilledDuringWar
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireCityBesiegedDuringWar, out this.otherEmpireCityBesiegedDuringWarDefinition))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireCityBesiegedDuringWar
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireCitiesTakenDuringWar, out this.otherEmpireCitiesTakenDuringWarDefinition))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireCitiesTakenDuringWar
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.Peaceful, out this.peacefulModifierDefinition))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.Peaceful
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.RegionalBuildingDestroyed, out this.regionalBuildingDestroyedDefinition))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.RegionalBuildingDestroyed
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireCitiesBombardedDuringWar, out this.myEmpireCitiesBombardedDuringWarDefinition))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireCitiesBombardedDuringWar
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireCitiesBombardedDuringWar, out this.otherEmpireCitiesBombardedDuringWarDefinition))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireCitiesBombardedDuringWar
			});
		}
	}

	private void OnAggressionEventRaise(Amplitude.Unity.Event.Event raisedEvent)
	{
		EventCitySiegeUpdate eventCitySiegeUpdate = raisedEvent as EventCitySiegeUpdate;
		if (eventCitySiegeUpdate != null)
		{
			this.CitySiegeUpdate(eventCitySiegeUpdate.City);
			this.CityBombardmentUpdate(eventCitySiegeUpdate.City);
		}
		EventEncounterStateChange eventEncounterStateChange = raisedEvent as EventEncounterStateChange;
		if (eventEncounterStateChange != null && eventEncounterStateChange.Empire.Index == this.Empire.Index && eventEncounterStateChange.EventArgs.EncounterState == EncounterState.BattleHasEnded)
		{
			Encounter encounter = eventEncounterStateChange.EventArgs.Encounter;
			if (this.ShouldITakeEncounterIntoAccount(encounter))
			{
				this.ArmyAggressionInNeutralRegionDuringColdWar(encounter);
				this.UnitsKilledDuringWar(encounter);
			}
		}
		EventSwapCity eventSwapCity = raisedEvent as EventSwapCity;
		if (eventSwapCity != null && eventSwapCity.Empire.Index == this.Empire.Index && eventSwapCity.IsAnActOfAggression)
		{
			this.CityTaken(eventSwapCity.City, eventSwapCity.OldOwnerEmpireIndex, eventSwapCity.NewOwnerEmpireIndex);
		}
		EventCityRazed eventCityRazed = raisedEvent as EventCityRazed;
		if (eventCityRazed != null && !eventCityRazed.DestroyingArmyIsPrivateers)
		{
			this.CityRazed(eventCityRazed.Region, eventCityRazed.Empire.Index, eventCityRazed.Destroyer.Index);
		}
		EventRegionalBuildingDestroyed eventRegionalBuildingDestroyed = raisedEvent as EventRegionalBuildingDestroyed;
		if (eventRegionalBuildingDestroyed != null && eventRegionalBuildingDestroyed.Empire.Index == this.Empire.Index)
		{
			this.RegionalBuildingDestroyed(eventRegionalBuildingDestroyed.WorldPosition, eventRegionalBuildingDestroyed.Destroyer);
		}
	}

	private void RegionalBuildingDestroyed(WorldPosition worldPositionPointOfInterest, global::Empire destroyer)
	{
		AILayer_Attitude.Attitude attitude = this.attitudeScores[destroyer.Index];
		attitude.Score.RemoveModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == AILayer_Attitude.AttitudeScoreDefinitionReferences.Peaceful);
		attitude.AddScoreModifier(this.regionalBuildingDestroyedDefinition, 1f);
	}

	private void UnitsKilledDuringWar(Encounter encounter)
	{
		this.empireCache.Clear();
		List<Contender> contenders = encounter.Contenders;
		bool[] array = new bool[]
		{
			true,
			true
		};
		for (int i = 0; i < contenders.Count; i++)
		{
			if (contenders[i].IsTakingPartInBattle)
			{
				array[(int)contenders[i].Group] &= contenders[i].IsPrivateers;
			}
		}
		int num = this.Empire.Index;
		for (int j = 0; j < contenders.Count; j++)
		{
			Contender contender = contenders[j];
			if (contender.Empire.Index != this.Empire.Index)
			{
				if (!array[(int)contender.Group] || !contender.IsPrivateers || this.maySeeThroughCatspaw)
				{
					num = Mathf.Max(num, contender.Empire.Index);
					this.empireCache.AddOnce(contender.Empire);
				}
			}
		}
		int[] array2 = new int[num + 1];
		for (int k = 0; k < contenders.Count; k++)
		{
			Contender contender2 = contenders[k];
			if (!array[(int)contender2.Group] || !contender2.IsPrivateers || this.maySeeThroughCatspaw)
			{
				if (contender2.ContenderSnapShots.Count > 0)
				{
					ContenderSnapShot contenderSnapShot = contender2.ContenderSnapShots[contender2.ContenderSnapShots.Count - 1];
					for (int l = 0; l < contenderSnapShot.UnitSnapShots.Count; l++)
					{
						UnitSnapShot unitSnapShot = contenderSnapShot.UnitSnapShots[l];
						if (unitSnapShot.GetPropertyValue(SimulationProperties.Health) <= 0f)
						{
							array2[contender2.Empire.Index]++;
						}
					}
				}
			}
		}
		for (int m = 0; m < this.empireCache.Count; m++)
		{
			global::Empire empire = this.empireCache[m];
			DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(empire);
			if (diplomaticRelation != null && diplomaticRelation.State != null && !(diplomaticRelation.State.Name != DiplomaticRelationState.Names.War))
			{
				AILayer_Attitude.Attitude attitude = this.attitudeScores[empire.Index];
				for (int n = 0; n < array2[this.Empire.Index]; n++)
				{
					attitude.AddScoreModifier(this.myEmpireUnitsKilledDuringWarDefinition, 1f);
				}
				for (int num2 = 0; num2 < array2[empire.Index]; num2++)
				{
					attitude.AddScoreModifier(this.otherEmpireUnitsKilledDuringWarDefinition, 1f);
				}
			}
		}
	}

	private void UpdateAgressionModifiers(StaticString context, StaticString pass)
	{
		Diagnostics.Assert(this.majorEmpires != null);
		for (int i = 0; i < this.majorEmpires.Length; i++)
		{
			MajorEmpire majorEmpire = this.majorEmpires[i];
			Diagnostics.Assert(majorEmpire != null);
			if (majorEmpire.Index != this.Empire.Index)
			{
				DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(majorEmpire);
				Diagnostics.Assert(diplomaticRelation != null && diplomaticRelation.State != null);
				StaticString name = diplomaticRelation.State.Name;
				if (!(name == DiplomaticRelationState.Names.Unknown) && !(name == DiplomaticRelationState.Names.Dead))
				{
					Diagnostics.Assert(this.attitudeScores != null);
					AILayer_Attitude.Attitude attitude = this.attitudeScores[majorEmpire.Index];
					Diagnostics.Assert(attitude != null && attitude.Score != null);
					if (attitude.Score.CountModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == AILayer_Attitude.AttitudeScoreDefinitionReferences.Peaceful) == 0)
					{
						attitude.AddScoreModifier(this.peacefulModifierDefinition, 1f);
					}
				}
				if (this.LastWarHelpInquiry[majorEmpire.Index] == this.game.Turn - 1 && this.LastWarHelpTarget[majorEmpire.Index] >= 0)
				{
					MajorEmpire majorEmpire2 = Array.Find<MajorEmpire>(this.majorEmpires, (MajorEmpire x) => x.Index == this.LastWarHelpTarget[majorEmpire.Index]);
					if (majorEmpire2 != null && !majorEmpire2.IsEliminated && this.departmentOfForeignAffairs.IsAtWarWith(majorEmpire2) && !this.majorEmpires[i].GetAgency<DepartmentOfForeignAffairs>().IsAtWarWith(majorEmpire2))
					{
						AILayer_Attitude.Attitude attitude2 = this.attitudeScores[majorEmpire.Index];
						Diagnostics.Assert(attitude2 != null);
						attitude2.AddScoreModifier(this.negativeContractDefinition, 1.5f);
						this.LastWarHelpInquiry[majorEmpire.Index] = -10;
					}
				}
			}
		}
	}

	private static bool IsWarWithMyAlly(DiplomaticRelationScoreModifier relationScoreModifier)
	{
		return relationScoreModifier.Definition.Name == AILayer_Attitude.AttitudeScoreDefinitionReferences.AtWarWithMyAlly;
	}

	private static bool IsAlliedWithMyEnemy(DiplomaticRelationScoreModifier relationScoreModifier)
	{
		return relationScoreModifier.Definition.Name == AILayer_Attitude.AttitudeScoreDefinitionReferences.AlliedWithMyEnemy;
	}

	private void InitializeAlliedWarEffort()
	{
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.AtWarWithMyAlly, out this.attitudeScoreWarWithMyAlly))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.AtWarWithMyAlly
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.AlliedWithMyEnemy, out this.attitudeScoreAlliedWithMyEnemy))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.AlliedWithMyEnemy
			});
		}
	}

	private void OnAlliedWarEffortEventRaise(Amplitude.Unity.Event.Event raisedEvent)
	{
		if (!(raisedEvent is EventDiplomaticRelationStateChange))
		{
			return;
		}
		Diagnostics.Assert(this.majorEmpires != null);
		Diagnostics.Assert(this.attitudeScores != null);
		Diagnostics.Assert(this.departmentOfForeignAffairs != null);
		for (int i = 0; i < this.majorEmpires.Length; i++)
		{
			MajorEmpire majorEmpire = this.majorEmpires[i];
			if (majorEmpire.Index != this.Empire.Index)
			{
				DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(majorEmpire);
				if (!(diplomaticRelation.State.Name == DiplomaticRelationState.Names.Unknown) && !(diplomaticRelation.State.Name == DiplomaticRelationState.Names.Dead))
				{
					AILayer_Attitude.Attitude attitude = this.GetAttitude(majorEmpire);
					Diagnostics.Assert(attitude != null && attitude.Score != null);
					this.CheckIfEnemyOfMyAllies(majorEmpire, attitude, diplomaticRelation);
					this.CheckIfAllyOfMyEnemies(majorEmpire, attitude, diplomaticRelation);
				}
			}
		}
	}

	private void CheckIfEnemyOfMyAllies(MajorEmpire bullyEmpire, AILayer_Attitude.Attitude attitudeTowardsBully, DiplomaticRelation diplomaticRelationToBully)
	{
		if (diplomaticRelationToBully.State.Name == DiplomaticRelationState.Names.Alliance)
		{
			attitudeTowardsBully.Score.RemoveModifiers(new Predicate<DiplomaticRelationScoreModifier>(AILayer_Attitude.IsWarWithMyAlly));
			return;
		}
		DepartmentOfForeignAffairs agency = bullyEmpire.GetAgency<DepartmentOfForeignAffairs>();
		Diagnostics.Assert(agency != null);
		int num = 0;
		for (int i = 0; i < this.majorEmpires.Length; i++)
		{
			MajorEmpire majorEmpire = this.majorEmpires[i];
			if (majorEmpire.Index != this.Empire.Index && majorEmpire.Index != bullyEmpire.Index)
			{
				DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(majorEmpire);
				Diagnostics.Assert(diplomaticRelation != null && diplomaticRelation.State != null);
				if (!(diplomaticRelation.State.Name != DiplomaticRelationState.Names.Alliance))
				{
					AILayer_Attitude.Attitude attitude = this.GetAttitude(majorEmpire);
					Diagnostics.Assert(attitude != null && attitude.Score != null);
					if (!attitude.Score.GetModifiers(new Predicate<DiplomaticRelationScoreModifier>(AILayer_Attitude.IsForcedStatus)).Any<DiplomaticRelationScoreModifier>())
					{
						DiplomaticRelation diplomaticRelation2 = agency.GetDiplomaticRelation(majorEmpire);
						Diagnostics.Assert(diplomaticRelation2 != null && diplomaticRelation2.State != null);
						if (diplomaticRelation2.State.Name == DiplomaticRelationState.Names.War)
						{
							num++;
						}
					}
				}
			}
		}
		if (num > 0)
		{
			if (attitudeTowardsBully.Score.CountModifiers(new Func<DiplomaticRelationScoreModifier, bool>(AILayer_Attitude.IsWarWithMyAlly)) <= 0)
			{
				attitudeTowardsBully.AddScoreModifier(this.attitudeScoreWarWithMyAlly, (float)num);
			}
			else
			{
				IEnumerable<DiplomaticRelationScoreModifier> modifiers = attitudeTowardsBully.Score.GetModifiers(new Predicate<DiplomaticRelationScoreModifier>(AILayer_Attitude.IsWarWithMyAlly));
				Diagnostics.Assert(modifiers.Count<DiplomaticRelationScoreModifier>() == 1);
				modifiers.First<DiplomaticRelationScoreModifier>().Multiplier = (float)num;
			}
		}
		else
		{
			attitudeTowardsBully.Score.RemoveModifiers(new Predicate<DiplomaticRelationScoreModifier>(AILayer_Attitude.IsWarWithMyAlly));
		}
	}

	private void CheckIfAllyOfMyEnemies(MajorEmpire traitorEmpire, AILayer_Attitude.Attitude attitudeTowardsTraitor, DiplomaticRelation diplomaticRelationToTraitor)
	{
		DepartmentOfForeignAffairs agency = traitorEmpire.GetAgency<DepartmentOfForeignAffairs>();
		Diagnostics.Assert(agency != null);
		int num = 0;
		for (int i = 0; i < this.majorEmpires.Length; i++)
		{
			MajorEmpire majorEmpire = this.majorEmpires[i];
			if (majorEmpire.Index != this.Empire.Index && majorEmpire.Index != traitorEmpire.Index)
			{
				DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(majorEmpire);
				Diagnostics.Assert(diplomaticRelation != null && diplomaticRelation.State != null);
				if (!(diplomaticRelation.State.Name != DiplomaticRelationState.Names.War))
				{
					DiplomaticRelation diplomaticRelation2 = agency.GetDiplomaticRelation(majorEmpire);
					Diagnostics.Assert(diplomaticRelation2 != null && diplomaticRelation2.State != null);
					if (diplomaticRelation2.State.Name == DiplomaticRelationState.Names.Alliance)
					{
						num++;
					}
				}
			}
		}
		if (num > 0)
		{
			if (attitudeTowardsTraitor.Score.CountModifiers(new Func<DiplomaticRelationScoreModifier, bool>(AILayer_Attitude.IsAlliedWithMyEnemy)) <= 0)
			{
				attitudeTowardsTraitor.AddScoreModifier(this.attitudeScoreAlliedWithMyEnemy, (float)num);
			}
			else
			{
				IEnumerable<DiplomaticRelationScoreModifier> modifiers = attitudeTowardsTraitor.Score.GetModifiers(new Predicate<DiplomaticRelationScoreModifier>(AILayer_Attitude.IsAlliedWithMyEnemy));
				Diagnostics.Assert(modifiers.Count<DiplomaticRelationScoreModifier>() == 1);
				modifiers.First<DiplomaticRelationScoreModifier>().Multiplier = (float)num;
			}
		}
		else
		{
			attitudeTowardsTraitor.Score.RemoveModifiers(new Predicate<DiplomaticRelationScoreModifier>(AILayer_Attitude.IsAlliedWithMyEnemy));
		}
	}

	private void InitializeColonization()
	{
		Diagnostics.Assert(this.diplomaticRelationScoreModifierDatabase != null);
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.AggressiveColonization, out this.attitudeScoreAggressiveColonization))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.AggressiveColonization
			});
		}
	}

	private bool IsNeighbourRegion(Region region)
	{
		DepartmentOfTheInterior agency = this.Empire.GetAgency<DepartmentOfTheInterior>();
		for (int i = 0; i < agency.Cities.Count; i++)
		{
			City city = agency.Cities[i];
			for (int j = 0; j < city.Region.Borders.Length; j++)
			{
				Region.Border border = city.Region.Borders[j];
				if (border.NeighbourRegionIndex == region.Index)
				{
					return true;
				}
			}
		}
		return false;
	}

	private void OnColonizationEventRaise(Amplitude.Unity.Event.Event raisedEvent)
	{
		EventColonize eventColonize = raisedEvent as EventColonize;
		if (eventColonize == null)
		{
			return;
		}
		MajorEmpire majorEmpire = eventColonize.Empire as MajorEmpire;
		Diagnostics.Assert(majorEmpire != null);
		Region region = eventColonize.City.Region;
		if (!this.IsNeighbourRegion(region))
		{
			return;
		}
		bool flag = false;
		DepartmentOfTheInterior agency = majorEmpire.GetAgency<DepartmentOfTheInterior>();
		for (int i = 0; i < agency.Cities.Count; i++)
		{
			City city = agency.Cities[i];
			for (int j = 0; j < city.Region.Borders.Length; j++)
			{
				Region.Border border = city.Region.Borders[j];
				Region region2 = this.worldPositioning.GetRegion(border.NeighbourRegionIndex);
				if (region2 != null && region2.IsLand)
				{
					if (!region2.IsRegionColonized() && !this.IsNeighbourRegion(region2))
					{
						flag = true;
						break;
					}
				}
			}
			if (flag)
			{
				break;
			}
		}
		if (!flag)
		{
			return;
		}
		AILayer_Attitude.Attitude attitude = this.GetAttitude(majorEmpire);
		Diagnostics.Assert(attitude != null);
		attitude.AddScoreModifier(this.attitudeScoreAggressiveColonization, 1f);
	}

	private void InitializeCommonDiplomaticStatus()
	{
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.CommonEnemyModifier, out this.attitudeScoreCommonEnemyModifier))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.CommonEnemyModifier
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.CommonFriendModifier, out this.attitudeScoreCommonFriendModifier))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.CommonFriendModifier
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.CommonAllyModifier, out this.attitudeScoreCommonAllyModifier))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.CommonAllyModifier
			});
		}
	}

	private void UpdateCommonDiplomaticStatusModifiers(StaticString context, StaticString pass)
	{
		Diagnostics.Assert(this.majorEmpires != null && this.attitudeScores != null);
		Diagnostics.Assert(this.departmentOfForeignAffairs != null);
		for (int i = 0; i < this.majorEmpires.Length; i++)
		{
			MajorEmpire majorEmpire = this.majorEmpires[i];
			if (majorEmpire.Index != this.Empire.Index)
			{
				DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(majorEmpire);
				if (!(diplomaticRelation.State.Name == DiplomaticRelationState.Names.Unknown) && !(diplomaticRelation.State.Name == DiplomaticRelationState.Names.Dead))
				{
					AILayer_Attitude.Attitude attitude = this.GetAttitude(majorEmpire);
					Diagnostics.Assert(attitude != null && attitude.Score != null);
					DepartmentOfForeignAffairs agency = majorEmpire.GetAgency<DepartmentOfForeignAffairs>();
					Diagnostics.Assert(agency != null);
					for (int j = 0; j < this.majorEmpires.Length; j++)
					{
						MajorEmpire majorEmpire2 = this.majorEmpires[j];
						if (majorEmpire2.Index != this.Empire.Index && majorEmpire2.Index != majorEmpire.Index)
						{
							DiplomaticRelation diplomaticRelation2 = this.departmentOfForeignAffairs.GetDiplomaticRelation(majorEmpire2);
							Diagnostics.Assert(diplomaticRelation2 != null && diplomaticRelation2.State != null);
							DiplomaticRelation diplomaticRelation3 = agency.GetDiplomaticRelation(majorEmpire2);
							Diagnostics.Assert(diplomaticRelation3 != null && diplomaticRelation3.State != null);
							if (diplomaticRelation2.State.Name == DiplomaticRelationState.Names.War && diplomaticRelation3.State.Name == DiplomaticRelationState.Names.War)
							{
								if (attitude.CommonEnemiesModifierIds[majorEmpire2.Index] >= 0 && attitude.Score.GetModifier(attitude.CommonEnemiesModifierIds[majorEmpire2.Index]) == null)
								{
									attitude.CommonEnemiesModifierIds[majorEmpire2.Index] = -1;
								}
								if (attitude.CommonEnemiesModifierIds[majorEmpire2.Index] < 0)
								{
									int num = attitude.AddScoreModifier(this.attitudeScoreCommonEnemyModifier, 1f);
									attitude.CommonEnemiesModifierIds[majorEmpire2.Index] = num;
								}
							}
							else if (attitude.CommonEnemiesModifierIds[majorEmpire2.Index] >= 0)
							{
								attitude.Score.RemoveModifier(attitude.CommonEnemiesModifierIds[majorEmpire2.Index]);
							}
							bool flag = attitude.Score.GetModifiers(new Predicate<DiplomaticRelationScoreModifier>(AILayer_Attitude.IsForcedStatus)).Any<DiplomaticRelationScoreModifier>();
							bool flag2 = (diplomaticRelation2.State.Name == DiplomaticRelationState.Names.Peace && diplomaticRelation3.State.Name == DiplomaticRelationState.Names.Peace) || (diplomaticRelation2.State.Name == DiplomaticRelationState.Names.Peace && diplomaticRelation3.State.Name == DiplomaticRelationState.Names.Alliance) || (diplomaticRelation2.State.Name == DiplomaticRelationState.Names.Alliance && diplomaticRelation3.State.Name == DiplomaticRelationState.Names.Peace);
							if (flag2 && !flag)
							{
								if (attitude.CommonFriendsModifierIds[majorEmpire2.Index] >= 0 && attitude.Score.GetModifier(attitude.CommonFriendsModifierIds[majorEmpire2.Index]) == null)
								{
									attitude.CommonFriendsModifierIds[majorEmpire2.Index] = -1;
								}
								if (attitude.CommonFriendsModifierIds[majorEmpire2.Index] < 0)
								{
									int num2 = attitude.AddScoreModifier(this.attitudeScoreCommonFriendModifier, 1f);
									attitude.CommonFriendsModifierIds[majorEmpire2.Index] = num2;
								}
							}
							else if (attitude.CommonFriendsModifierIds[majorEmpire2.Index] >= 0)
							{
								attitude.Score.RemoveModifier(attitude.CommonFriendsModifierIds[majorEmpire2.Index]);
							}
							bool flag3 = diplomaticRelation2.State.Name == DiplomaticRelationState.Names.Alliance && diplomaticRelation3.State.Name == DiplomaticRelationState.Names.Alliance;
							if (flag3 && !flag)
							{
								if (attitude.CommonAlliesModifierIds[majorEmpire2.Index] >= 0 && attitude.Score.GetModifier(attitude.CommonAlliesModifierIds[majorEmpire2.Index]) == null)
								{
									attitude.CommonAlliesModifierIds[majorEmpire2.Index] = -1;
								}
								if (attitude.CommonAlliesModifierIds[majorEmpire2.Index] < 0)
								{
									int num3 = attitude.AddScoreModifier(this.attitudeScoreCommonAllyModifier, 1f);
									attitude.CommonAlliesModifierIds[majorEmpire2.Index] = num3;
								}
							}
							else if (attitude.CommonAlliesModifierIds[majorEmpire2.Index] >= 0)
							{
								attitude.Score.RemoveModifier(attitude.CommonAlliesModifierIds[majorEmpire2.Index]);
							}
						}
					}
				}
			}
		}
	}

	private void InitializeCommonFrontier()
	{
		Diagnostics.Assert(this.diplomaticRelationScoreModifierDatabase != null);
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.CommonFrontier, out this.attitudeScoreCommonFrontier))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.CommonFrontier
			});
		}
	}

	private void UpdateCommonFrontierModifiers(StaticString context, StaticString pass)
	{
		IWorldPositionningService service = this.game.GetService<IWorldPositionningService>();
		Diagnostics.Assert(service != null);
		for (int i = 0; i < this.majorEmpires.Length; i++)
		{
			MajorEmpire majorEmpire = this.majorEmpires[i];
			if (majorEmpire.Index != this.Empire.Index)
			{
				AILayer_Attitude.Attitude attitude = this.GetAttitude(majorEmpire);
				for (int j = attitude.Frontiers.Count - 1; j >= 0; j--)
				{
					AILayer_Attitude.Attitude.EmpireFrontier empireFrontier = attitude.Frontiers[j];
					Diagnostics.Assert(empireFrontier != null);
					Region region = service.GetRegion((int)empireFrontier.EmpireRegionIndex);
					Diagnostics.Assert(region != null);
					Region region2 = service.GetRegion((int)empireFrontier.NeighbourRegionIndex);
					Diagnostics.Assert(region2 != null);
					if (region.Owner == null || region.Owner != this.Empire || region2.Owner == null || region2.Owner != majorEmpire)
					{
						if (empireFrontier.AttitudeModifierIndex >= 0)
						{
							attitude.Score.RemoveModifier(empireFrontier.AttitudeModifierIndex);
						}
						attitude.Frontiers.RemoveAt(j);
					}
				}
			}
		}
		DepartmentOfTheInterior agency = this.Empire.GetAgency<DepartmentOfTheInterior>();
		Diagnostics.Assert(agency != null && agency.Cities != null);
		for (int k = 0; k < agency.Cities.Count; k++)
		{
			Diagnostics.Assert(agency.Cities[k] != null);
			Region region3 = agency.Cities[k].Region;
			Diagnostics.Assert(region3 != null && region3.Borders != null);
			for (int l = 0; l < region3.Borders.Length; l++)
			{
				Region.Border border = region3.Borders[l];
				Region region4 = service.GetRegion(border.NeighbourRegionIndex);
				Diagnostics.Assert(region4 != null);
				this.CheckFrontierAndRegister(region3, region4);
			}
		}
		for (int m = 0; m < agency.OccupiedRegions.Count; m++)
		{
			Region region5 = agency.OccupiedRegions[m];
			Diagnostics.Assert(region5 != null && region5.Borders != null);
			for (int n = 0; n < region5.Borders.Length; n++)
			{
				Region.Border border2 = region5.Borders[n];
				Region region6 = service.GetRegion(border2.NeighbourRegionIndex);
				Diagnostics.Assert(region6 != null);
				this.CheckFrontierAndRegister(region5, region6);
			}
		}
	}

	private void CheckFrontierAndRegister(Region myEmpireRegion, Region neighbourRegion)
	{
		global::Empire owner = neighbourRegion.Owner;
		if (owner == null)
		{
			return;
		}
		if (owner.Index == this.Empire.Index)
		{
			return;
		}
		if (owner is KaijuEmpire)
		{
			return;
		}
		short empireRegionIndex = (short)myEmpireRegion.Index;
		short neighbourRegionIndex = (short)neighbourRegion.Index;
		AILayer_Attitude.Attitude attitude = this.GetAttitude(owner);
		AILayer_Attitude.Attitude.EmpireFrontier empireFrontier = attitude.Frontiers.Find((AILayer_Attitude.Attitude.EmpireFrontier match) => match.EmpireRegionIndex == empireRegionIndex && match.NeighbourRegionIndex == neighbourRegionIndex);
		if (empireFrontier != null)
		{
			return;
		}
		AILayer_Attitude.Attitude.EmpireFrontier empireFrontier2 = new AILayer_Attitude.Attitude.EmpireFrontier(empireRegionIndex, neighbourRegionIndex);
		empireFrontier2.AttitudeModifierIndex = attitude.AddScoreModifier(this.attitudeScoreCommonFrontier, 1f);
		attitude.Frontiers.Add(empireFrontier2);
	}

	private void InitializeCreepingNodes()
	{
		Diagnostics.Assert(this.diplomaticRelationScoreModifierDatabase != null);
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.DismantleCreepingNodeSuffered, out this.attitudeScoreDismantleCreepingNodeSuffered))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.DismantleCreepingNodeSuffered
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.CreepingNodeUpgradeComplete, out this.attitudeScoreCreepingNodeUpgradeComplete))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.CreepingNodeUpgradeComplete
			});
		}
	}

	private void OnCreepingNodeEventRaise(Amplitude.Unity.Event.Event raisedEvent)
	{
		if (raisedEvent == null)
		{
			throw new ArgumentNullException("raisedEvent");
		}
		if (raisedEvent is EventDismantleCreepingNodeSuffered)
		{
			this.OnDismantleCreepingNodeSuffered((EventDismantleCreepingNodeSuffered)raisedEvent);
		}
		if (raisedEvent is EventCreepingNodeUpgradeComplete)
		{
			this.OnCreepingNodeUpgradeComplete((EventCreepingNodeUpgradeComplete)raisedEvent);
		}
	}

	private void OnDismantleCreepingNodeSuffered(EventDismantleCreepingNodeSuffered raisedEvent)
	{
		MajorEmpire majorEmpire = raisedEvent.Instigator.Empire as MajorEmpire;
		if (majorEmpire == null || majorEmpire.Index == this.Empire.Index)
		{
			return;
		}
		DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(majorEmpire);
		if (diplomaticRelation == null || diplomaticRelation.State == null || diplomaticRelation.State.Name.Equals(DiplomaticRelationState.Names.War) || diplomaticRelation.State.Name.Equals(DiplomaticRelationState.Names.Dead))
		{
			return;
		}
		AILayer_Attitude.Attitude attitude = this.GetAttitude(majorEmpire);
		if (attitude == null)
		{
			return;
		}
		if (raisedEvent.Empire.Index == this.Empire.Index)
		{
			attitude.AddScoreModifier(this.attitudeScoreDismantleCreepingNodeSuffered, 1f);
		}
	}

	private void OnCreepingNodeUpgradeComplete(EventCreepingNodeUpgradeComplete raisedEvent)
	{
		CreepingNode creepingNode = raisedEvent.CreepingNode;
		if (creepingNode == null || !creepingNode.WorldPosition.IsValid)
		{
			return;
		}
		global::Empire empire = creepingNode.Empire;
		if (empire == null || empire.Index == this.Empire.Index)
		{
			return;
		}
		DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(creepingNode.Empire);
		if (diplomaticRelation == null || diplomaticRelation.State == null || diplomaticRelation.State.Name.Equals(DiplomaticRelationState.Names.War) || diplomaticRelation.State.Name.Equals(DiplomaticRelationState.Names.Dead) || this.diplomacyLayer.GetPeaceWish(empire.Index))
		{
			return;
		}
		AILayer_Attitude.Attitude attitude = this.GetAttitude(creepingNode.Empire);
		if (attitude == null)
		{
			return;
		}
		if (!this.visibilityService.IsWorldPositionVisibleFor(creepingNode.WorldPosition, this.Empire))
		{
			return;
		}
		if (this.worldPositioning.GetRegion(creepingNode.WorldPosition).BelongToEmpire(empire))
		{
			return;
		}
		DepartmentOfTheInterior agency = this.Empire.GetAgency<DepartmentOfTheInterior>();
		if (agency.Cities.Count == 0)
		{
			return;
		}
		int num = int.MaxValue;
		for (int i = 0; i < agency.Cities.Count; i++)
		{
			num = Math.Min(num, this.worldPositioning.GetDistance(agency.Cities[i].WorldPosition, creepingNode.WorldPosition));
		}
		float multiplier = 1f - Mathf.InverseLerp(0f, this.worldPositioning.World.Hypotenuse, (float)num);
		attitude.AddScoreModifier(this.attitudeScoreCreepingNodeUpgradeComplete, multiplier);
	}

	public void RegisterContractBenefitForMyEmpire(DiplomaticContract contract, float benefitForMyEmpire)
	{
		if (contract == null)
		{
			throw new ArgumentNullException("contract");
		}
		if (benefitForMyEmpire <= 0f)
		{
			return;
		}
		global::Empire empireWhichProposes = contract.EmpireWhichProposes;
		Diagnostics.Assert(empireWhichProposes != null);
		Diagnostics.Assert(this.attitudeScores != null);
		AILayer_Attitude.Attitude attitude = this.attitudeScores[empireWhichProposes.Index];
		Diagnostics.Assert(attitude != null);
		float num = Mathf.Sqrt(benefitForMyEmpire / 4f);
		if (num < 0.2f)
		{
			return;
		}
		attitude.AddScoreModifier((!this.IsGiftContract(contract)) ? this.positiveContractDefinition : this.giftDefinition, num);
	}

	private static bool IsTermForcedStatus(DiplomaticTerm term)
	{
		return term != null && (term.Definition.Name == DiplomaticTermDefinition.Names.WarToTruceDeclaration || term.Definition.Name == DiplomaticTermDefinition.Names.ColdWarToPeaceDeclaration || term.Definition.Name == DiplomaticTermDefinition.Names.PeaceToAllianceDeclaration);
	}

	private static bool IsTermFreeStatus(DiplomaticTerm term)
	{
		return term != null && (term.Definition.Name == DiplomaticTermDefinition.Names.WarToTruce || term.Definition.Name == DiplomaticTermDefinition.Names.TruceToWar || term.Definition.Name == DiplomaticTermDefinition.Names.ColdWarToWar || term.Definition.Name == DiplomaticTermDefinition.Names.ColdWarToPeace || term.Definition.Name == DiplomaticTermDefinition.Names.ColdWarToAlliance || term.Definition.Name == DiplomaticTermDefinition.Names.PeaceToWar || term.Definition.Name == DiplomaticTermDefinition.Names.PeaceToColdWar || term.Definition.Name == DiplomaticTermDefinition.Names.PeaceToAlliance || term.Definition.Name == DiplomaticTermDefinition.Names.AllianceToWar || term.Definition.Name == DiplomaticTermDefinition.Names.AllianceToColdWar || term.Definition.Name == DiplomaticTermDefinition.Names.AllianceToPeace);
	}

	private static bool IsTermBlackSpot(DiplomaticTerm term)
	{
		return term != null && term.Definition.Name == DiplomaticTermDefinition.Names.BlackSpot;
	}

	private static bool IsForcedStatus(DiplomaticRelationScoreModifier relationScoreModifier)
	{
		return relationScoreModifier.Definition.Name == AILayer_Attitude.AttitudeScoreDefinitionReferences.ForcedStatus;
	}

	private void InitializeDiplomaticContract()
	{
		Diagnostics.Assert(this.diplomaticRelationScoreModifierDatabase != null);
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.PositiveContract, out this.positiveContractDefinition))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.PositiveContract
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.NegativeContract, out this.negativeContractDefinition))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.NegativeContract
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.Gift, out this.giftDefinition))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.Gift
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.BlackSpot, out this.blackSpotDefinition))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.BlackSpot
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.ForcedStatus, out this.forcedStatusDefinition))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.ForcedStatus
			});
		}
	}

	private bool IsAgressiveContract(DiplomaticContract diplomaticContract)
	{
		if (diplomaticContract == null)
		{
			throw new ArgumentNullException("diplomaticContract");
		}
		if (diplomaticContract.GetPropositionMethod() == DiplomaticTerm.PropositionMethod.Negotiation)
		{
			return false;
		}
		if (diplomaticContract.Terms.Any((DiplomaticTerm term) => term is DiplomaticTermDiplomaticRelationState))
		{
			return false;
		}
		return !diplomaticContract.Terms.Any((DiplomaticTerm term) => term.Definition.Name == DiplomaticTermDefinition.Names.Gratify || term.Definition.Name == DiplomaticTermDefinition.Names.Warning);
	}

	private bool IsGiftContract(DiplomaticContract diplomaticContract)
	{
		return this.IsCommercialContract(diplomaticContract) && diplomaticContract.Terms.All((DiplomaticTerm term) => term.EmpireWhichReceives.Index == this.Empire.Index);
	}

	private bool IsCommercialContract(DiplomaticContract diplomaticContract)
	{
		if (diplomaticContract == null)
		{
			throw new ArgumentNullException("diplomaticContract");
		}
		if (diplomaticContract.GetPropositionMethod() == DiplomaticTerm.PropositionMethod.Declaration)
		{
			return false;
		}
		if (diplomaticContract.Terms == null || diplomaticContract.Terms.Count == 0)
		{
			return false;
		}
		if (diplomaticContract.Terms.Any((DiplomaticTerm term) => term is DiplomaticTermDiplomaticRelationState))
		{
			return false;
		}
		if (diplomaticContract.Terms.Any((DiplomaticTerm term) => term is DiplomaticTermDiplomaticRelationState))
		{
			return false;
		}
		return diplomaticContract.Terms.All((DiplomaticTerm term) => term is DiplomaticTermResourceExchange || term is DiplomaticTermTechnologyExchange || term is DiplomaticTermBoosterExchange || term is DiplomaticTermCityExchange || term is DiplomaticTermFortressExchange || term.Definition.Alignment == DiplomaticTermAlignment.Good);
	}

	private void OnContractEventRaise(Amplitude.Unity.Event.Event raisedEvent)
	{
		EventDiplomaticContractStateChange eventDiplomaticContractStateChange = raisedEvent as EventDiplomaticContractStateChange;
		if (eventDiplomaticContractStateChange != null)
		{
			DiplomaticContract diplomaticContract = eventDiplomaticContractStateChange.DiplomaticContract;
			Diagnostics.Assert(diplomaticContract != null);
			Diagnostics.Assert(diplomaticContract.EmpireWhichProposes != null);
			Diagnostics.Assert(diplomaticContract.EmpireWhichReceives != null);
			if (diplomaticContract.State == DiplomaticContractState.Signed && diplomaticContract.Terms != null && diplomaticContract.EmpireWhichReceives.Index == this.Empire.Index)
			{
				global::Empire empire = (diplomaticContract.EmpireWhichProposes.Index != this.Empire.Index) ? diplomaticContract.EmpireWhichProposes : diplomaticContract.EmpireWhichReceives;
				AILayer_Attitude.Attitude attitude = this.attitudeScores[empire.Index];
				Diagnostics.Assert(attitude != null);
				if (diplomaticContract.Terms.Any((DiplomaticTerm term) => term != null && term.Definition.Name == DiplomaticTermDefinition.Names.Warning))
				{
					attitude.Score.RemoveModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == DiplomaticRelationScoreModifier.Names.Peaceful);
				}
				if (diplomaticContract.Terms.Any(new Func<DiplomaticTerm, bool>(AILayer_Attitude.IsTermForcedStatus)))
				{
					attitude.Score.RemoveModifiers((DiplomaticRelationScoreModifier modifier) => AILayer_Attitude.IsLongTermPeace(modifier) || AILayer_Attitude.IsLongTermAlliance(modifier) || modifier.Definition.Name == DiplomaticRelationScoreModifier.Names.Peaceful);
					attitude.AddScoreModifier(this.forcedStatusDefinition, 1f);
				}
				else if (diplomaticContract.Terms.Any(new Func<DiplomaticTerm, bool>(AILayer_Attitude.IsTermBlackSpot)))
				{
					attitude.Score.RemoveModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == DiplomaticRelationScoreModifier.Names.Peaceful);
					attitude.AddScoreModifier(this.blackSpotDefinition, 1f);
				}
				else if (this.IsAgressiveContract(diplomaticContract))
				{
					attitude.Score.RemoveModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == DiplomaticRelationScoreModifier.Names.Peaceful);
					attitude.AddScoreModifier(this.negativeContractDefinition, 1f);
				}
				if (diplomaticContract.Terms.Any(new Func<DiplomaticTerm, bool>(AILayer_Attitude.IsTermFreeStatus)))
				{
					attitude.Score.RemoveModifiers(new Predicate<DiplomaticRelationScoreModifier>(AILayer_Attitude.IsForcedStatus));
				}
			}
			if (diplomaticContract.State == DiplomaticContractState.Proposed && diplomaticContract.Terms != null && diplomaticContract.EmpireWhichProposes.Index == this.Empire.Index)
			{
				global::Empire empire2 = diplomaticContract.EmpireWhichReceives;
				if (this.departmentOfForeignAffairs.IsFriend(empire2))
				{
					DiplomaticTermProposal diplomaticTermProposal = diplomaticContract.Terms.FirstOrDefault((DiplomaticTerm term) => term.EmpireWhichProvides == empire2 && term is DiplomaticTermProposal) as DiplomaticTermProposal;
					if (diplomaticTermProposal != null && this.departmentOfForeignAffairs.IsAtWarWith(diplomaticTermProposal.ChosenEmpire) && this.Empire.GetPropertyValue(SimulationProperties.MilitaryPower) < diplomaticTermProposal.ChosenEmpire.GetPropertyValue(SimulationProperties.MilitaryPower) * 1.25f)
					{
						this.LastWarHelpInquiry[empire2.Index] = this.game.Turn;
						this.LastWarHelpTarget[empire2.Index] = diplomaticTermProposal.ChosenEmpireIndex;
					}
				}
			}
		}
	}

	private void InitializeFortress()
	{
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireFortressesTakenDuringWar, out this.myEmpireFortressesTakenDuringWarDefinition))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireFortressesTakenDuringWar
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireFortressesTakenDuringWar, out this.otherEmpireFortressesTakenDuringWarDefinition))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireFortressesTakenDuringWar
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireLostNavalRegionControlDuringWar, out this.myEmpireLostNavalRegionControlDuringWarDefinition))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireLostNavalRegionControlDuringWar
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireLostNavalRegionControlDuringWar, out this.otherEmpireLostNavalRegionControlDuringWarDefinition))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireLostNavalRegionControlDuringWar
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireHasUniqueFacilityDefinition, out this.otherEmpireHasUniqueFacilityDefinition))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireHasUniqueFacilityDefinition
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.FortressesOwnedInTheSameRegion, out this.fortressesOwnedInTheSameRegionDefinition))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.FortressesOwnedInTheSameRegion
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireFortressesTakenDuringColdWar, out this.myEmpireFortressesTakenDuringColdWarDefinition))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireFortressesTakenDuringColdWar
			});
		}
	}

	private void OnFortressEventRaised(Amplitude.Unity.Event.Event raisedEvent)
	{
		EventFortressOccupantSwapped eventFortressOccupantSwapped = raisedEvent as EventFortressOccupantSwapped;
		if (eventFortressOccupantSwapped != null && (eventFortressOccupantSwapped.NewOccupant.Index == this.Empire.Index || eventFortressOccupantSwapped.OldOccupant.Index == this.Empire.Index))
		{
			this.FortressSwap(eventFortressOccupantSwapped.Fortress, eventFortressOccupantSwapped.OldOccupant.Index, eventFortressOccupantSwapped.NewOccupant.Index);
			this.CheckFortressLostAgainstRegionControl(eventFortressOccupantSwapped.Fortress, eventFortressOccupantSwapped.OldOccupant.Index, eventFortressOccupantSwapped.NewOccupant.Index);
			this.CheckFortressAgainstMyOwnedFortress(eventFortressOccupantSwapped.Fortress, eventFortressOccupantSwapped.OldOccupant, eventFortressOccupantSwapped.NewOccupant);
			this.CheckFortressAgainstUniqueFacility(eventFortressOccupantSwapped.Fortress, eventFortressOccupantSwapped.OldOccupant.Index, eventFortressOccupantSwapped.NewOccupant.Index);
		}
		EventFacilityRevealed eventFacilityRevealed = raisedEvent as EventFacilityRevealed;
		if (eventFacilityRevealed != null && eventFacilityRevealed.Empire.Index == this.Empire.Index)
		{
			int aggressorEmpireIndex = -1;
			if (eventFacilityRevealed.Fortress.Occupant != null)
			{
				aggressorEmpireIndex = eventFacilityRevealed.Fortress.Occupant.Index;
			}
			this.CheckFortressAgainstUniqueFacility(eventFacilityRevealed.Fortress, -1, aggressorEmpireIndex);
		}
	}

	private void FortressSwap(Fortress fortress, int oldOwnerEmpireIndex, int newOwnerEmpireIndex)
	{
		Diagnostics.Assert(fortress != null);
		Diagnostics.Assert(this.majorEmpires != null);
		AILayer_Attitude.Attitude attitude = null;
		DiplomaticRelationScoreModifierDefinition diplomaticRelationScoreModifierDefinition = null;
		if (oldOwnerEmpireIndex == this.Empire.Index && newOwnerEmpireIndex < this.majorEmpires.Length)
		{
			DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(this.majorEmpires[newOwnerEmpireIndex]);
			Diagnostics.Assert(diplomaticRelation != null);
			if (diplomaticRelation.State != null && diplomaticRelation.State.Name == DiplomaticRelationState.Names.War)
			{
				diplomaticRelationScoreModifierDefinition = this.myEmpireFortressesTakenDuringWarDefinition;
			}
			else
			{
				diplomaticRelationScoreModifierDefinition = this.myEmpireFortressesTakenDuringColdWarDefinition;
			}
			attitude = this.attitudeScores[newOwnerEmpireIndex];
			Diagnostics.Assert(attitude != null && attitude.Score != null);
			attitude.Score.RemoveModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == AILayer_Attitude.AttitudeScoreDefinitionReferences.Peaceful);
		}
		if (oldOwnerEmpireIndex < this.majorEmpires.Length && newOwnerEmpireIndex == this.Empire.Index)
		{
			attitude = this.attitudeScores[oldOwnerEmpireIndex];
			diplomaticRelationScoreModifierDefinition = this.otherEmpireFortressesTakenDuringWarDefinition;
		}
		if (attitude != null)
		{
			int num;
			if (attitude.FortressModifiersInfoByFortressGuid.TryGetValue(fortress.GUID, out num))
			{
				if (num >= 0)
				{
					DiplomaticRelationScoreModifier modifier = attitude.Score.GetModifier(num);
					if (modifier == null || modifier.Definition == null || diplomaticRelationScoreModifierDefinition == null || modifier.Definition.Name != diplomaticRelationScoreModifierDefinition.Name)
					{
						attitude.Score.RemoveModifier(num);
						attitude.FortressModifiersInfoByFortressGuid.Remove(fortress.GUID);
					}
					return;
				}
			}
			else
			{
				attitude.FortressModifiersInfoByFortressGuid.Add(fortress.GUID, -1);
			}
			num = attitude.AddScoreModifier(diplomaticRelationScoreModifierDefinition, 1f);
			if (num >= 0)
			{
				attitude.FortressModifiersInfoByFortressGuid[fortress.GUID] = num;
			}
		}
	}

	private void CheckFortressLostAgainstRegionControl(Fortress fortress, int oldOwnerEmpireIndex, int aggressorEmpireIndex)
	{
		if (fortress.Region == null)
		{
			return;
		}
		bool flag = false;
		Region region = fortress.Region;
		if (fortress.Region.NavalEmpire != null)
		{
			PirateCouncil agency = fortress.Region.NavalEmpire.GetAgency<PirateCouncil>();
			if (agency != null)
			{
				this.sameRegionFortresses.Clear();
				flag = true;
				agency.FillRegionFortresses(fortress.Region, this.sameRegionFortresses);
				for (int i = 0; i < this.sameRegionFortresses.Count; i++)
				{
					if (!(fortress.GUID == this.sameRegionFortresses[i].GUID))
					{
						if (fortress.Occupant == null || fortress.Occupant.Index == oldOwnerEmpireIndex)
						{
							flag = false;
							break;
						}
					}
				}
			}
		}
		if (!flag)
		{
			return;
		}
		AILayer_Attitude.Attitude attitude = null;
		DiplomaticRelationScoreModifierDefinition diplomaticRelationScoreModifierDefinition = null;
		if (oldOwnerEmpireIndex == this.Empire.Index && aggressorEmpireIndex < this.majorEmpires.Length)
		{
			attitude = this.attitudeScores[aggressorEmpireIndex];
			Diagnostics.Assert(attitude != null && attitude.Score != null);
			diplomaticRelationScoreModifierDefinition = this.myEmpireLostNavalRegionControlDuringWarDefinition;
		}
		if (oldOwnerEmpireIndex < this.majorEmpires.Length && aggressorEmpireIndex == this.Empire.Index)
		{
			attitude = this.attitudeScores[oldOwnerEmpireIndex];
			Diagnostics.Assert(attitude != null && attitude.Score != null);
			diplomaticRelationScoreModifierDefinition = this.otherEmpireLostNavalRegionControlDuringWarDefinition;
		}
		if (attitude != null)
		{
			AILayer_Attitude.Attitude.NavalRegionModifiersInfo navalRegionModifiersInfo;
			if (!attitude.OceanRegionModifiersInfoByRegionIndex.TryGetValue(region.Index, out navalRegionModifiersInfo))
			{
				navalRegionModifiersInfo = new AILayer_Attitude.Attitude.NavalRegionModifiersInfo();
				attitude.OceanRegionModifiersInfoByRegionIndex.Add(region.Index, navalRegionModifiersInfo);
			}
			int num = navalRegionModifiersInfo.RegionLostModifierId;
			if (num >= 0)
			{
				DiplomaticRelationScoreModifier modifier = attitude.Score.GetModifier(num);
				attitude.Score.RemoveModifier(num);
				navalRegionModifiersInfo.RegionLostModifierId = -1;
				if (modifier.Definition.Name != diplomaticRelationScoreModifierDefinition.Name)
				{
					return;
				}
			}
			num = attitude.AddScoreModifier(diplomaticRelationScoreModifierDefinition, 1f);
			if (num >= 0)
			{
				navalRegionModifiersInfo.RegionLostModifierId = num;
			}
		}
	}

	private void CheckFortressAgainstUniqueFacility(Fortress fortress, int oldOwnerEmpireIndex, int aggressorEmpireIndex)
	{
		if (oldOwnerEmpireIndex < this.majorEmpires.Length && oldOwnerEmpireIndex >= 0 && oldOwnerEmpireIndex != this.Empire.Index)
		{
			AILayer_Attitude.Attitude attitude = this.attitudeScores[oldOwnerEmpireIndex];
			for (int i = 0; i < fortress.Facilities.Count; i++)
			{
				if (attitude.FacilityModifiersByFacilityGuid.ContainsKey(fortress.Facilities[i].GUID))
				{
					int num = attitude.FacilityModifiersByFacilityGuid[fortress.Facilities[i].GUID];
					if (num >= 0)
					{
						attitude.Score.RemoveModifier(num);
					}
					attitude.FacilityModifiersByFacilityGuid.Remove(fortress.Facilities[i].GUID);
				}
			}
		}
		if (aggressorEmpireIndex < this.majorEmpires.Length && aggressorEmpireIndex >= 0 && aggressorEmpireIndex != this.Empire.Index)
		{
			AILayer_Attitude.Attitude attitude2 = this.attitudeScores[aggressorEmpireIndex];
			string format = "AI/MajorEmpire/AIEntity_Empire/AILayer_Attitude/Facilities/{0}";
			IPersonalityAIHelper service = AIScheduler.Services.GetService<IPersonalityAIHelper>();
			for (int j = 0; j < fortress.Facilities.Count; j++)
			{
				if (fortress.Facilities[j].PointOfInterestImprovement != null)
				{
					if (Fortress.IsFacilityUnique(fortress.Facilities[j]))
					{
						int num2 = 0;
						if (attitude2.FacilityModifiersByFacilityGuid.TryGetValue(fortress.Facilities[j].GUID, out num2))
						{
							if (num2 >= 0)
							{
								goto IL_1FB;
							}
						}
						else
						{
							attitude2.FacilityModifiersByFacilityGuid.Add(fortress.Facilities[j].GUID, -1);
						}
						float num3 = 1f;
						num3 = service.GetRegistryValue<float>(this.Empire, string.Format(format, fortress.Facilities[j].PointOfInterestImprovement.Name), num3);
						num2 = attitude2.AddScoreModifier(this.otherEmpireHasUniqueFacilityDefinition, num3);
						if (num2 >= 0)
						{
							attitude2.FacilityModifiersByFacilityGuid[fortress.Facilities[j].GUID] = num2;
						}
					}
				}
				IL_1FB:;
			}
		}
	}

	private void CheckFortressAgainstMyOwnedFortress(Fortress fortress, global::Empire oldOwner, global::Empire newOwner)
	{
		Region region = fortress.Region;
		DepartmentOfTheInterior agency = this.Empire.GetAgency<DepartmentOfTheInterior>();
		bool flag = agency.OccupiedRegions.Contains(region);
		if (flag)
		{
			if (newOwner.Index == this.Empire.Index)
			{
				PirateCouncil agency2 = fortress.Region.NavalEmpire.GetAgency<PirateCouncil>();
				if (agency2 != null)
				{
					this.sameRegionFortresses.Clear();
					agency2.FillRegionFortresses(fortress.Region, this.sameRegionFortresses);
					for (int i = 0; i < this.sameRegionFortresses.Count; i++)
					{
						if (!(fortress.GUID == this.sameRegionFortresses[i].GUID))
						{
							if (fortress.Occupant != null && fortress.Occupant.Index != this.Empire.Index)
							{
								AILayer_Attitude.Attitude attitudeScore = this.attitudeScores[fortress.Occupant.Index];
								this.ApplyMyOwnedFortressDefinition(region.Index, attitudeScore, false);
							}
						}
					}
				}
			}
			else if (newOwner is MajorEmpire)
			{
				AILayer_Attitude.Attitude attitudeScore2 = this.attitudeScores[newOwner.Index];
				this.ApplyMyOwnedFortressDefinition(region.Index, attitudeScore2, false);
			}
			if (oldOwner is MajorEmpire && oldOwner.Index != this.Empire.Index)
			{
				AILayer_Attitude.Attitude attitudeScore3 = this.attitudeScores[oldOwner.Index];
				DepartmentOfTheInterior agency3 = oldOwner.GetAgency<DepartmentOfTheInterior>();
				bool flag2 = agency3.OccupiedRegions.Contains(region);
				this.ApplyMyOwnedFortressDefinition(region.Index, attitudeScore3, !flag2);
			}
		}
		else if (oldOwner.Index == this.Empire.Index)
		{
			for (int j = 0; j < this.attitudeScores.Length; j++)
			{
				if (j != this.Empire.Index)
				{
					AILayer_Attitude.Attitude attitudeScore4 = this.attitudeScores[j];
					this.ApplyMyOwnedFortressDefinition(region.Index, attitudeScore4, true);
				}
			}
		}
	}

	private void ApplyMyOwnedFortressDefinition(int regionIndex, AILayer_Attitude.Attitude attitudeScore, bool remove)
	{
		if (attitudeScore != null)
		{
			AILayer_Attitude.Attitude.NavalRegionModifiersInfo navalRegionModifiersInfo;
			if (!attitudeScore.OceanRegionModifiersInfoByRegionIndex.TryGetValue(regionIndex, out navalRegionModifiersInfo))
			{
				navalRegionModifiersInfo = new AILayer_Attitude.Attitude.NavalRegionModifiersInfo();
				attitudeScore.OceanRegionModifiersInfoByRegionIndex.Add(regionIndex, navalRegionModifiersInfo);
			}
			int num = navalRegionModifiersInfo.BothOwnFortressModifierId;
			if (remove)
			{
				if (num >= 0)
				{
					attitudeScore.Score.RemoveModifier(num);
					navalRegionModifiersInfo.BothOwnFortressModifierId = -1;
				}
			}
			else if (num < 0)
			{
				num = attitudeScore.AddScoreModifier(this.fortressesOwnedInTheSameRegionDefinition, 1f);
				if (num >= 0)
				{
					navalRegionModifiersInfo.BothOwnFortressModifierId = num;
				}
			}
		}
	}

	public override void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		base.ReadXml(reader);
		if (num >= 2)
		{
			int attribute = reader.GetAttribute<int>("Count");
			Diagnostics.Assert(this.attitudeScores.Length == attribute);
			reader.ReadStartElement("Attitudes");
			for (int i = 0; i < attribute; i++)
			{
				AILayer_Attitude.Attitude attitude = this.attitudeScores[i] ?? new AILayer_Attitude.Attitude(this.Empire, null, attribute);
				reader.ReadElementSerializable<AILayer_Attitude.Attitude>(ref attitude);
				Diagnostics.Assert(attitude == null || attitude.OtherEmpire != null);
				this.attitudeScores[i] = attitude;
			}
			reader.ReadEndElement("Attitudes");
		}
		if (num >= 3)
		{
			reader.ReadStartElement("EmpireLastAggressorIndex");
			for (int j = 0; j < this.empireLastAggressorIndex.Length; j++)
			{
				Diagnostics.Assert(int.TryParse(reader.ReadElementString("Empire_" + j.ToString()), out this.empireLastAggressorIndex[j]));
			}
			reader.ReadEndElement("EmpireLastAggressorIndex");
			this.ReadDictionnary(reader, "LastWarHelpInquiry", this.LastWarHelpInquiry);
			this.ReadDictionnary(reader, "LastWarHelpTarget", this.LastWarHelpTarget);
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		int num = writer.WriteVersionAttribute(3);
		base.WriteXml(writer);
		if (num >= 2)
		{
			writer.WriteStartElement("Attitudes");
			writer.WriteAttributeString<int>("Count", this.attitudeScores.Length);
			for (int i = 0; i < this.attitudeScores.Length; i++)
			{
				IXmlSerializable xmlSerializable = this.attitudeScores[i];
				writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
			}
			writer.WriteEndElement();
		}
		if (num >= 3)
		{
			writer.WriteStartElement("EmpireLastAggressorIndex");
			for (int j = 0; j < this.empireLastAggressorIndex.Length; j++)
			{
				writer.WriteElementString("Empire_" + j.ToString(), this.empireLastAggressorIndex[j].ToString());
			}
			writer.WriteEndElement();
			this.WriteDictionnary(writer, "LastWarHelpInquiry", this.LastWarHelpInquiry);
			this.WriteDictionnary(writer, "LastWarHelpTarget", this.LastWarHelpTarget);
		}
	}

	private void InitializeKaijus()
	{
		Diagnostics.Assert(this.diplomaticRelationScoreModifierDatabase != null);
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.KaijuAttacked, out this.AttitudeScoreKaijuAttacked))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.KaijuAttacked
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.KaijuPlacedNearMyCityNegative, out this.AttitudeScoreKaijuPlacedNearMyCityNegative))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.KaijuPlacedNearMyCityNegative
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.KaijuPlacedNearMyCityPositive, out this.AttitudeScoreKaijuPlacedNearMyCityPositive))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.KaijuPlacedNearMyCityPositive
			});
		}
	}

	private void OnKaijuEventRaise(Amplitude.Unity.Event.Event raisedEvent)
	{
		if (raisedEvent == null)
		{
			throw new ArgumentNullException("raisedEvent");
		}
		if (raisedEvent is EventKaijuLost)
		{
			this.OnKaijuLost((EventKaijuLost)raisedEvent);
		}
		if (raisedEvent is EventKaijuRelocated)
		{
			this.OnKaijuRelocated((EventKaijuRelocated)raisedEvent);
		}
	}

	private void OnKaijuLost(EventKaijuLost raisedEvent)
	{
		Kaiju kaiju = raisedEvent.Kaiju;
		MajorEmpire lastOwner = raisedEvent.LastOwner;
		if (lastOwner == null || lastOwner.Index != this.Empire.Index)
		{
			return;
		}
		MajorEmpire empireByIndex = this.game.GetEmpireByIndex<MajorEmpire>(raisedEvent.InstigatorEmpireIndex);
		if (empireByIndex == null)
		{
			return;
		}
		DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(empireByIndex);
		if (diplomaticRelation == null || diplomaticRelation.State == null || diplomaticRelation.State.Name.Equals(DiplomaticRelationState.Names.War) || diplomaticRelation.State.Name.Equals(DiplomaticRelationState.Names.Dead))
		{
			return;
		}
		AILayer_Attitude.Attitude attitude = this.GetAttitude(empireByIndex);
		if (attitude == null)
		{
			return;
		}
		attitude.AddScoreModifier(this.AttitudeScoreKaijuAttacked, 1f);
	}

	private void OnKaijuRelocated(EventKaijuRelocated raisedEvent)
	{
		Kaiju kaiju = raisedEvent.Kaiju;
		MajorEmpire majorEmpire = kaiju.MajorEmpire;
		if (majorEmpire == null || majorEmpire.Index == this.Empire.Index)
		{
			return;
		}
		if (!kaiju.OnGarrisonMode())
		{
			return;
		}
		DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(majorEmpire);
		if (diplomaticRelation == null || diplomaticRelation.State == null || diplomaticRelation.State.Name.Equals(DiplomaticRelationState.Names.War) || diplomaticRelation.State.Name.Equals(DiplomaticRelationState.Names.Dead))
		{
			return;
		}
		AILayer_Attitude.Attitude attitude = this.GetAttitude(majorEmpire);
		if (attitude == null)
		{
			return;
		}
		int num = 0;
		Region[] neighbourRegions = this.worldPositioning.GetNeighbourRegions(kaiju.Region, false, false);
		for (int i = 0; i < neighbourRegions.Length; i++)
		{
			City city = neighbourRegions[i].City;
			if (city != null && city.Empire.Index == this.Empire.Index)
			{
				num++;
			}
		}
		if (num == 0)
		{
			return;
		}
		if (diplomaticRelation.State.Name.Equals(DiplomaticRelationState.Names.Alliance))
		{
			attitude.AddScoreModifier(this.AttitudeScoreKaijuPlacedNearMyCityPositive, (float)num);
		}
		else
		{
			attitude.AddScoreModifier(this.AttitudeScoreKaijuPlacedNearMyCityNegative, (float)num);
		}
	}

	private void InitializeLivingSpaceTense()
	{
		Diagnostics.Assert(this.diplomaticRelationScoreModifierDatabase != null);
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.LivingSpaceTense, out this.attitudeScoreLivingSpaceTense))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.LivingSpaceTense
			});
		}
	}

	private void UpdateLivingSpaceTenseModifiers(StaticString context, StaticString pass)
	{
		DepartmentOfTheInterior agency = this.Empire.GetAgency<DepartmentOfTheInterior>();
		if (agency.Cities.Count == 0)
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < agency.Cities.Count; i++)
		{
			City city = agency.Cities[i];
			for (int j = 0; j < city.Region.Borders.Length; j++)
			{
				Region.Border border = city.Region.Borders[j];
				Region region = this.worldPositioning.GetRegion(border.NeighbourRegionIndex);
				if (region != null && region.IsLand && region.City == null)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				break;
			}
		}
		IWorldAtlasAIHelper service = AIScheduler.Services.GetService<IWorldAtlasAIHelper>();
		Diagnostics.Assert(service != null);
		Diagnostics.Assert(this.majorEmpires != null);
		for (int k = 0; k < this.majorEmpires.Length; k++)
		{
			MajorEmpire majorEmpire = this.majorEmpires[k];
			if (this.Empire.Index != majorEmpire.Index)
			{
				DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(majorEmpire);
				Diagnostics.Assert(diplomaticRelation != null);
				AILayer_Attitude.Attitude attitude = this.GetAttitude(majorEmpire);
				Diagnostics.Assert(attitude != null);
				float commonBorderRatio = service.GetCommonBorderRatio(this.Empire, majorEmpire);
				if (flag || commonBorderRatio <= 1.401298E-45f)
				{
					attitude.Score.RemoveModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == AILayer_Attitude.AttitudeScoreDefinitionReferences.LivingSpaceTense);
				}
				else
				{
					DiplomaticRelationScoreModifier diplomaticRelationScoreModifier = null;
					IEnumerable<DiplomaticRelationScoreModifier> modifiers = attitude.Score.GetModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == AILayer_Attitude.AttitudeScoreDefinitionReferences.LivingSpaceTense);
					using (IEnumerator<DiplomaticRelationScoreModifier> enumerator = modifiers.GetEnumerator())
					{
						if (enumerator.MoveNext())
						{
							DiplomaticRelationScoreModifier diplomaticRelationScoreModifier2 = enumerator.Current;
							diplomaticRelationScoreModifier = diplomaticRelationScoreModifier2;
						}
					}
					if (diplomaticRelationScoreModifier == null)
					{
						attitude.AddScoreModifier(this.attitudeScoreLivingSpaceTense, commonBorderRatio);
					}
					else
					{
						diplomaticRelationScoreModifier.Multiplier = commonBorderRatio;
					}
				}
			}
		}
	}

	private static bool IsLongTermAlliance(DiplomaticRelationScoreModifier relationScoreModifier)
	{
		return relationScoreModifier.Definition.Name == AILayer_Attitude.AttitudeScoreDefinitionReferences.LongTermAlliance;
	}

	private static bool IsLongTermPeace(DiplomaticRelationScoreModifier relationScoreModifier)
	{
		return relationScoreModifier.Definition.Name == AILayer_Attitude.AttitudeScoreDefinitionReferences.LongTermPeace;
	}

	private void InitializeLongTermRelationship()
	{
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.LongTermAlliance, out this.attitudeScoreLongTermAllianceModifier))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.LongTermAlliance
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.LongTermPeace, out this.attitudeScoreLongTermPeaceModifier))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.LongTermPeace
			});
		}
	}

	private void OnLongTermRelationshipEventRaise(Amplitude.Unity.Event.Event raisedEvent)
	{
		EventDiplomaticRelationStateChange eventDiplomaticRelationStateChange = raisedEvent as EventDiplomaticRelationStateChange;
		if (eventDiplomaticRelationStateChange == null)
		{
			return;
		}
		if (eventDiplomaticRelationStateChange.Empire.Index == this.Empire.Index)
		{
			global::Empire empireWithWhichTheStatusChange = eventDiplomaticRelationStateChange.EmpireWithWhichTheStatusChange;
			AILayer_Attitude.Attitude attitude = this.GetAttitude(empireWithWhichTheStatusChange);
			Diagnostics.Assert(attitude != null);
			if (eventDiplomaticRelationStateChange.PreviousDiplomaticRelationStateName == DiplomaticRelationState.Names.Alliance)
			{
				attitude.Score.RemoveModifiers(new Predicate<DiplomaticRelationScoreModifier>(AILayer_Attitude.IsLongTermAlliance));
			}
			else if (eventDiplomaticRelationStateChange.PreviousDiplomaticRelationStateName == DiplomaticRelationState.Names.Peace && eventDiplomaticRelationStateChange.DiplomaticRelationStateName != DiplomaticRelationState.Names.Alliance)
			{
				attitude.Score.RemoveModifiers(new Predicate<DiplomaticRelationScoreModifier>(AILayer_Attitude.IsLongTermPeace));
			}
			bool flag = attitude.Score.GetModifiers(new Predicate<DiplomaticRelationScoreModifier>(AILayer_Attitude.IsForcedStatus)).Any<DiplomaticRelationScoreModifier>();
			if (!flag && eventDiplomaticRelationStateChange.DiplomaticRelationStateName == DiplomaticRelationState.Names.Alliance)
			{
				if (attitude.Score.CountModifiers(new Func<DiplomaticRelationScoreModifier, bool>(AILayer_Attitude.IsLongTermAlliance)) <= 0)
				{
					attitude.AddScoreModifier(this.attitudeScoreLongTermAllianceModifier, 1f);
				}
				if (attitude.Score.CountModifiers(new Func<DiplomaticRelationScoreModifier, bool>(AILayer_Attitude.IsLongTermPeace)) <= 0)
				{
					attitude.AddScoreModifier(this.attitudeScoreLongTermPeaceModifier, 1f);
				}
			}
			else if (!flag && eventDiplomaticRelationStateChange.DiplomaticRelationStateName == DiplomaticRelationState.Names.Peace)
			{
				if (attitude.Score.CountModifiers(new Func<DiplomaticRelationScoreModifier, bool>(AILayer_Attitude.IsLongTermPeace)) <= 0)
				{
					attitude.AddScoreModifier(this.attitudeScoreLongTermPeaceModifier, 1f);
				}
			}
			else
			{
				attitude.Score.RemoveModifiers((DiplomaticRelationScoreModifier modifier) => AILayer_Attitude.IsLongTermPeace(modifier) || AILayer_Attitude.IsLongTermAlliance(modifier));
			}
		}
	}

	private void InitializeOrbs()
	{
		Diagnostics.Assert(this.diplomaticRelationScoreModifierDatabase != null);
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.MantaAspiratingInMyTerritory, out this.attitudeScoreMantaAspiratingInMyTerritory))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.MantaAspiratingInMyTerritory
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.OrbsStolen, out this.attitudeScoreOrbsStollen))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.OrbsStolen
			});
		}
	}

	private void UpdateOrbsModifiers(StaticString context, StaticString pass)
	{
		DepartmentOfForeignAffairs agency = this.Empire.GetAgency<DepartmentOfForeignAffairs>();
		DepartmentOfTheInterior agency2 = this.Empire.GetAgency<DepartmentOfTheInterior>();
		Diagnostics.Assert(agency2 != null && agency2.Cities != null);
		for (int i = 0; i < agency2.Cities.Count; i++)
		{
			City city = agency2.Cities[i];
			Diagnostics.Assert(city != null);
			Diagnostics.Assert(city.Empire != null && city.Empire.Index == this.Empire.Index);
			Region region = city.Region;
			Diagnostics.Assert(region != null);
			foreach (Army army in Intelligence.GetVisibleArmiesInRegion(region.Index, this.Empire))
			{
				if (army.Empire.Index != this.Empire.Index)
				{
					MajorEmpire majorEmpire = army.Empire as MajorEmpire;
					if (majorEmpire != null)
					{
						if (!army.IsPrivateers || this.maySeeThroughCatspaw)
						{
							bool flag = false;
							foreach (Unit unit in army.Units)
							{
								flag |= unit.CheckUnitAbility(UnitAbility.ReadonlyHarbinger, -1);
							}
							if (flag)
							{
								DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(majorEmpire);
								if (diplomaticRelation.State == null || (!(diplomaticRelation.State.Name == DiplomaticRelationState.Names.War) && !(diplomaticRelation.State.Name == DiplomaticRelationState.Names.Dead)))
								{
									AILayer_Attitude.Attitude attitude = this.GetAttitude(majorEmpire);
									Diagnostics.Assert(attitude != null);
									if (army.IsAspirating)
									{
										attitude.AddScoreModifier(this.attitudeScoreMantaAspiratingInMyTerritory, 1f);
									}
								}
							}
						}
					}
				}
			}
		}
	}

	private void OnOrbEventRaise(Amplitude.Unity.Event.Event raisedEvent)
	{
		EventOrbsCollected eventOrbsCollected = raisedEvent as EventOrbsCollected;
		if (eventOrbsCollected != null)
		{
			if (eventOrbsCollected.Empire == null || eventOrbsCollected.Empire.Index == this.Empire.Index)
			{
				return;
			}
			float num = 1f;
			if (!this.visibilityService.IsWorldPositionVisibleFor(eventOrbsCollected.WorldPosition, this.Empire))
			{
				if (!this.departmentOfForeignAffairs.CanSeeOrbWithOrbHunterTrait)
				{
					return;
				}
				num = 0.2f;
			}
			MajorEmpire majorEmpire = eventOrbsCollected.Empire as MajorEmpire;
			if (majorEmpire == null)
			{
				return;
			}
			IGameEntity gameEntity;
			this.gameEntityRepositoryService.TryGetValue(eventOrbsCollected.CollectorEntityGUID, out gameEntity);
			if (gameEntity != null)
			{
				Army army = gameEntity as Army;
				if (army != null)
				{
					if (army.IsPrivateers)
					{
						return;
					}
					if (army.IsCamouflaged && !this.visibilityService.IsWorldPositionDetectedFor(army.WorldPosition, this.Empire) && !this.visibilityService.IsWorldPositionDetectedFor(eventOrbsCollected.WorldPosition, this.Empire))
					{
						return;
					}
				}
			}
			Region region = this.worldPositioning.GetRegion(eventOrbsCollected.WorldPosition);
			if (region == null)
			{
				return;
			}
			global::Empire owner = region.Owner;
			if (owner == this.Empire)
			{
				AILayer_Attitude.Attitude attitude = this.GetAttitude(majorEmpire);
				Diagnostics.Assert(attitude != null);
				attitude.AddScoreModifier(this.attitudeScoreOrbsStollen, (float)eventOrbsCollected.OrbsQuantity * num);
				return;
			}
			if (owner == null)
			{
				if (this.diplomacyLayer.GetPeaceWish(majorEmpire.Index))
				{
					return;
				}
				foreach (Army army2 in this.Empire.GetAgency<DepartmentOfDefense>().Armies)
				{
					float propertyValue = army2.GetPropertyValue(SimulationProperties.MaximumMovement);
					if ((float)this.worldPositioning.GetDistance(eventOrbsCollected.WorldPosition, army2.WorldPosition) <= propertyValue)
					{
						AILayer_Attitude.Attitude attitude2 = this.GetAttitude(majorEmpire);
						Diagnostics.Assert(attitude2 != null);
						attitude2.AddScoreModifier(this.attitudeScoreOrbsStollen, (float)eventOrbsCollected.OrbsQuantity * num / 2f);
						break;
					}
				}
			}
		}
	}

	private void InitializePillage()
	{
		Diagnostics.Assert(this.diplomaticRelationScoreModifierDatabase != null);
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.Pillaging, out this.attitudeScorePillaging))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.Pillaging
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.PillageSucceed, out this.attitudeScorePillageSucceed))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.PillageSucceed
			});
		}
	}

	private void OnPillageSucceedEventRaise(Amplitude.Unity.Event.Event raisedEvent)
	{
		EventPillageSuffered eventPillageSuffered = raisedEvent as EventPillageSuffered;
		if (eventPillageSuffered != null && eventPillageSuffered.Empire.Index == this.Empire.Index && eventPillageSuffered.Instigator.Empire != this.Empire && eventPillageSuffered.Instigator.Empire is MajorEmpire && this.CanPillagingArmyBeSeen(eventPillageSuffered.Instigator))
		{
			AILayer_Attitude.Attitude attitude = this.GetAttitude(eventPillageSuffered.Instigator.Empire);
			attitude.AddScoreModifier(this.attitudeScorePillageSucceed, 1f);
		}
	}

	private bool CanPillagingArmyBeSeen(Army army)
	{
		return this.visibilityService.IsWorldPositionVisibleFor(army.WorldPosition, this.Empire) && (!army.IsCamouflaged || this.visibilityService.IsWorldPositionDetectedFor(army.WorldPosition, this.Empire)) && !army.IsPrivateers;
	}

	private void UpdatePillageModifiers(StaticString context, StaticString pass)
	{
		DepartmentOfTheInterior agency = this.Empire.GetAgency<DepartmentOfTheInterior>();
		for (int i = 0; i < agency.Cities.Count; i++)
		{
			for (int j = 0; j < agency.Cities[i].Region.PointOfInterests.Length; j++)
			{
				PointOfInterest pointOfInterest = agency.Cities[i].Region.PointOfInterests[j];
				if (pointOfInterest.ArmyPillaging.IsValid && pointOfInterest.PointOfInterestImprovement != null)
				{
					IGameEntity gameEntity;
					if (this.gameEntityRepositoryService.TryGetValue(pointOfInterest.ArmyPillaging, out gameEntity) && gameEntity is Army)
					{
						Army army = gameEntity as Army;
						global::Empire empire = army.Empire;
						if (empire is MajorEmpire)
						{
							if (this.CanPillagingArmyBeSeen(army))
							{
								AILayer_Attitude.Attitude attitude = this.GetAttitude(empire);
								if (!attitude.PillageModifierByPointOfInterestGuid.ContainsKey(pointOfInterest.GUID))
								{
									attitude.PillageModifierByPointOfInterestGuid.Add(pointOfInterest.GUID, -1);
								}
								else
								{
									int num = attitude.PillageModifierByPointOfInterestGuid[pointOfInterest.GUID];
									if (num >= 0)
									{
										attitude.Score.RemoveModifier(num);
									}
								}
								int value = attitude.AddScoreModifier(this.attitudeScorePillaging, 1f);
								attitude.PillageModifierByPointOfInterestGuid[pointOfInterest.GUID] = value;
							}
						}
					}
				}
			}
		}
	}

	private float GetGlobalScore(MajorEmpire empire)
	{
		float result = 0f;
		GameScore gameScore;
		if (empire.GameScores.TryGetValue(GameScores.Names.GlobalScore, out gameScore))
		{
			Diagnostics.Assert(gameScore != null);
			result = gameScore.Value;
		}
		return result;
	}

	private float GetLandMilitaryPower(MajorEmpire empire)
	{
		IAIEmpireDataAIHelper service = AIScheduler.Services.GetService<IAIEmpireDataAIHelper>();
		AIEmpireData aiempireData;
		if (service.TryGet(empire.Index, out aiempireData))
		{
			return aiempireData.LandMilitaryPower;
		}
		return empire.GetPropertyValue(SimulationProperties.LandMilitaryPower);
	}

	private float GetRegionCount(MajorEmpire empire)
	{
		DepartmentOfTheInterior agency = empire.GetAgency<DepartmentOfTheInterior>();
		return (float)agency.Cities.Count;
	}

	private float GetNavalRegionCount(MajorEmpire empire)
	{
		DepartmentOfTheInterior agency = empire.GetAgency<DepartmentOfTheInterior>();
		int num = agency.GetOwnedNavalRegion().Count<Region>();
		return (float)num;
	}

	private float GetNavalMilitaryPower(MajorEmpire empire)
	{
		IAIEmpireDataAIHelper service = AIScheduler.Services.GetService<IAIEmpireDataAIHelper>();
		AIEmpireData aiempireData;
		if (service.TryGet(empire.Index, out aiempireData))
		{
			return aiempireData.NavalMilitaryPower;
		}
		return empire.GetPropertyValue(SimulationProperties.NavalMilitaryPower);
	}

	private void InitializeScore()
	{
		Diagnostics.Assert(this.diplomaticRelationScoreModifierDatabase != null);
		this.comparativeModifierRule = new AILayer_Attitude.ComparativeModifierRule[5];
		this.comparativeModifierRule[0] = this.CreatRule(AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireLeadScore, AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireLeadScore, new Func<MajorEmpire, float>(this.GetGlobalScore), 0.1f, 1f, 2f, null);
		this.landMilitaryPowerRule = (this.comparativeModifierRule[1] = this.CreatRule(AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireLeadMilitaryPower, AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireLeadMilitaryPower, new Func<MajorEmpire, float>(this.GetLandMilitaryPower), 0.5f, 1f, 4f, null));
		this.comparativeModifierRule[2] = this.CreatRule(AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireLeadRegionCount, AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireLeadRegionCount, new Func<MajorEmpire, float>(this.GetRegionCount), 0f, 1f, 4f, new PathPrerequisite("../ClassEmpire,!FactionTraitCultists7,!FactionTraitMimics1", false, new string[0]));
		this.navalMilitaryPowerRule = (this.comparativeModifierRule[3] = this.CreatRule(AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireLeadNavalMilitaryPower, AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireLeadNavalMilitaryPower, new Func<MajorEmpire, float>(this.GetNavalMilitaryPower), 0.5f, 1f, 4f, null));
		this.comparativeModifierRule[4] = this.CreatRule(AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireLeadNavalRegionCount, AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireLeadNavalRegionCount, new Func<MajorEmpire, float>(this.GetNavalRegionCount), 0f, 1f, 4f, null);
	}

	private void UpdateScoreModifiers(StaticString context, StaticString pass)
	{
		Diagnostics.Assert(base.AIEntity != null);
		AILayer_Navy layer = base.AIEntity.GetLayer<AILayer_Navy>();
		Diagnostics.Assert(layer != null);
		float value = layer.NavyImportance.Value;
		float num = 1f - value;
		this.landMilitaryPowerRule.MaximumMultiplierValue = 4f * num;
		this.navalMilitaryPowerRule.MaximumMultiplierValue = 4f * value;
		this.landMilitaryPowerRule.MinimumMultiplierValue = num;
		this.navalMilitaryPowerRule.MinimumMultiplierValue = value;
		for (int i = 0; i < this.comparativeModifierRule.Length; i++)
		{
			AILayer_Attitude.ComparativeModifierRule comparativeRule = this.comparativeModifierRule[i];
			Diagnostics.Assert(comparativeRule != null && comparativeRule.GetScoreAccessor != null && comparativeRule.AttitudeScoreMyEmpireLead != null && comparativeRule.AttitudeScoreOtherEmpireLead != null);
			float num2 = comparativeRule.GetScoreAccessor(this.Empire as MajorEmpire);
			if (comparativeRule.Prerequisite == null || comparativeRule.Prerequisite.Check(this.Empire))
			{
				Diagnostics.Assert(this.majorEmpires != null);
				for (int j = 0; j < this.majorEmpires.Length; j++)
				{
					MajorEmpire majorEmpire = this.majorEmpires[j];
					if (this.Empire.Index != majorEmpire.Index)
					{
						if (comparativeRule.Prerequisite == null || comparativeRule.Prerequisite.Check(majorEmpire))
						{
							if (this.IsRelationAvailableWithMyEmpire(majorEmpire))
							{
								DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(majorEmpire);
								Diagnostics.Assert(diplomaticRelation != null);
								AILayer_Attitude.Attitude attitude = this.GetAttitude(majorEmpire);
								Diagnostics.Assert(attitude != null);
								DiplomaticRelationScore score = attitude.Score;
								float num3 = comparativeRule.GetScoreAccessor(majorEmpire);
								if (num2 > (1f + comparativeRule.NeutralIntervalPercent) * num3)
								{
									DiplomaticRelationScoreModifier diplomaticRelationScoreModifier = score.GetModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == comparativeRule.AttitudeScoreMyEmpireLead.Name).FirstOrDefault<DiplomaticRelationScoreModifier>();
									if (diplomaticRelationScoreModifier == null)
									{
										int modifierId = attitude.AddScoreModifier(comparativeRule.AttitudeScoreMyEmpireLead, 1f);
										diplomaticRelationScoreModifier = score.GetModifier(modifierId);
									}
									diplomaticRelationScoreModifier.Multiplier = Mathf.Clamp(num2 / Mathf.Max(num3, 1f), comparativeRule.MinimumMultiplierValue, comparativeRule.MaximumMultiplierValue);
									score.RemoveModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == comparativeRule.AttitudeScoreOtherEmpireLead.Name);
								}
								else if (num3 > (1f + comparativeRule.NeutralIntervalPercent) * num2)
								{
									DiplomaticRelationScoreModifier diplomaticRelationScoreModifier2 = score.GetModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == comparativeRule.AttitudeScoreOtherEmpireLead.Name).FirstOrDefault<DiplomaticRelationScoreModifier>();
									if (diplomaticRelationScoreModifier2 == null)
									{
										int modifierId2 = attitude.AddScoreModifier(comparativeRule.AttitudeScoreOtherEmpireLead, 1f);
										diplomaticRelationScoreModifier2 = score.GetModifier(modifierId2);
									}
									diplomaticRelationScoreModifier2.Multiplier = Mathf.Clamp(num3 / Mathf.Max(num2, 1f), comparativeRule.MinimumMultiplierValue, comparativeRule.MaximumMultiplierValue);
									score.RemoveModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == comparativeRule.AttitudeScoreMyEmpireLead.Name);
								}
								else
								{
									score.RemoveModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == comparativeRule.AttitudeScoreMyEmpireLead.Name);
									score.RemoveModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == comparativeRule.AttitudeScoreOtherEmpireLead.Name);
								}
							}
						}
					}
				}
			}
		}
	}

	private AILayer_Attitude.ComparativeModifierRule CreatRule(StaticString myEmpire, StaticString otherEmpire, Func<MajorEmpire, float> func, float neutralIntervalPercent, float minimumMultiplierValue, float maximumMultiplierValue, PathPrerequisite prerequisite)
	{
		DiplomaticRelationScoreModifierDefinition attitudeScoreMyEmpireLead;
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(myEmpire, out attitudeScoreMyEmpireLead))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				myEmpire
			});
		}
		DiplomaticRelationScoreModifierDefinition attitudeScoreOtherEmpireLead;
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(otherEmpire, out attitudeScoreOtherEmpireLead))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				otherEmpire
			});
		}
		return new AILayer_Attitude.ComparativeModifierRule
		{
			AttitudeScoreMyEmpireLead = attitudeScoreMyEmpireLead,
			AttitudeScoreOtherEmpireLead = attitudeScoreOtherEmpireLead,
			GetScoreAccessor = func,
			NeutralIntervalPercent = neutralIntervalPercent,
			Prerequisite = prerequisite,
			MinimumMultiplierValue = minimumMultiplierValue,
			MaximumMultiplierValue = maximumMultiplierValue
		};
	}

	private void InitializeSpy()
	{
		Diagnostics.Assert(this.diplomaticRelationScoreModifierDatabase != null);
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.Spy, out this.attitudeScoreSpy))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.Spy
			});
		}
	}

	private void OnSpyEventRaise(Amplitude.Unity.Event.Event raisedEvent)
	{
		EventInfiltrationActionResult infiltrationActionResult = raisedEvent as EventInfiltrationActionResult;
		if (infiltrationActionResult != null && infiltrationActionResult.Empire.Index == base.AIEntity.Empire.Index && infiltrationActionResult.Suffered)
		{
			if (infiltrationActionResult.AntiSpyResult == DepartmentOfIntelligence.AntiSpyResult.Nothing)
			{
				if (this.majorEmpires.Count((MajorEmpire match) => !match.IsEliminated) > 2)
				{
					goto IL_11B;
				}
			}
			for (int i = 0; i < this.majorEmpires.Length; i++)
			{
				MajorEmpire majorEmpire = this.majorEmpires[i];
				if (this.Empire.Index != majorEmpire.Index)
				{
					DepartmentOfEducation agency = majorEmpire.GetAgency<DepartmentOfEducation>();
					if (agency.Heroes.Any((Unit match) => match.GUID == infiltrationActionResult.InfiltratedHeroUnitGUID))
					{
						AILayer_Attitude.Attitude attitude = this.GetAttitude(majorEmpire);
						attitude.AddScoreModifier(this.attitudeScoreSpy, 1f);
						break;
					}
				}
			}
		}
		IL_11B:
		EventRoundUpEndInitiator eventRoundUpEndInitiator = raisedEvent as EventRoundUpEndInitiator;
		if (eventRoundUpEndInitiator != null && eventRoundUpEndInitiator.Empire.Index == base.AIEntity.Empire.Index)
		{
			for (int j = 0; j < eventRoundUpEndInitiator.AntiSpyResults.Length; j++)
			{
				DepartmentOfIntelligence.AntiSpyResult antiSpyResult = eventRoundUpEndInitiator.AntiSpyResults[j];
				GameEntityGUID heroGameEntityGUID = eventRoundUpEndInitiator.HeroesGUIDs[j];
				if (antiSpyResult != DepartmentOfIntelligence.AntiSpyResult.Nothing)
				{
					global::Empire empire = null;
					for (int k = 0; k < this.majorEmpires.Length; k++)
					{
						if (k != this.Empire.Index)
						{
							DepartmentOfEducation agency2 = this.majorEmpires[k].GetAgency<DepartmentOfEducation>();
							if (agency2.Heroes.Any((Unit match) => match.GUID == heroGameEntityGUID))
							{
								empire = this.majorEmpires[k];
								break;
							}
						}
					}
					if (empire != null)
					{
						AILayer_Attitude.Attitude attitude2 = this.GetAttitude(empire);
						if (attitude2 != null)
						{
							attitude2.AddScoreModifier(this.attitudeScoreSpy, 1f);
						}
					}
				}
			}
		}
	}

	private void UpdateSpyModifiers(StaticString context, StaticString pass)
	{
	}

	private void InitializeTerraform()
	{
		Diagnostics.Assert(this.diplomaticRelationScoreModifierDatabase != null);
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.DismantleDeviceSuffered, out this.attitudeScoreDismantleDeviceSuffered))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.DismantleDeviceSuffered
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.TerraformedNearMyArmiesPositive, out this.attitudeScoreTerraformedNearMyArmiesPositive))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.TerraformedNearMyArmiesPositive
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.TerraformedNearMyArmiesNegative, out this.attitudeScoreTerraformedNearMyArmiesNegative))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.TerraformedNearMyArmiesNegative
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.TerraformedInMyTerritoryPositive, out this.attitudeScoreTerraformedInMyTerritoryPositive))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.TerraformedInMyTerritoryPositive
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.TerraformedInMyTerritoryNegative, out this.attitudeScoreTerraformedInMyTerritoryNegative))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.TerraformedInMyTerritoryNegative
			});
		}
	}

	private void OnTerraformEventRaise(Amplitude.Unity.Event.Event raisedEvent)
	{
		if (raisedEvent == null)
		{
			throw new ArgumentNullException("raisedEvent");
		}
		if (raisedEvent is EventDismantleDeviceSuffered)
		{
			this.OnDismantleDeviceSuffered((EventDismantleDeviceSuffered)raisedEvent);
		}
		if (raisedEvent is EventEmpireTerraformDevicePlaced)
		{
			this.OnEmpireTerraformDevicePlaced((EventEmpireTerraformDevicePlaced)raisedEvent);
		}
	}

	private void OnEmpireTerraformDevicePlaced(EventEmpireTerraformDevicePlaced eventEmpireTerraformDevicePlaced)
	{
		if (eventEmpireTerraformDevicePlaced == null)
		{
			throw new ArgumentNullException("eventEmpireTerraformDevicePlaced");
		}
		WorldPosition terraformDeviceWorldPosition = eventEmpireTerraformDevicePlaced.TerraformDeviceWorldPosition;
		if (terraformDeviceWorldPosition == WorldPosition.Zero)
		{
			throw new ArgumentException("eventEmpireTerraformDevicePlaced.TerraformDeviceWorldPosition");
		}
		MajorEmpire majorEmpire = eventEmpireTerraformDevicePlaced.TerraformingEmpire as MajorEmpire;
		if (majorEmpire == null)
		{
			throw new ArgumentException("raisedEvent.TerraformingEmpire");
		}
		if (majorEmpire.Index == this.Empire.Index)
		{
			return;
		}
		DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(majorEmpire);
		if (diplomaticRelation.State == null || diplomaticRelation.State.Name.Equals(DiplomaticRelationState.Names.War) || diplomaticRelation.State.Name.Equals(DiplomaticRelationState.Names.Dead))
		{
			return;
		}
		AILayer_Attitude.Attitude attitude = this.GetAttitude(majorEmpire);
		if (attitude == null)
		{
			return;
		}
		DepartmentOfDefense agency = this.Empire.GetAgency<DepartmentOfDefense>();
		ReadOnlyCollection<Army> armies = agency.Armies;
		int num = 0;
		int num2 = 0;
		Region region = this.worldPositioning.GetRegion(terraformDeviceWorldPosition);
		if (region.Owner != null && region.Owner.Index == this.Empire.Index)
		{
			num++;
		}
		else if (region.Owner == null)
		{
			for (int i = 0; i < armies.Count; i++)
			{
				Army army = armies[i];
				float propertyValue = army.GetPropertyValue(SimulationProperties.MaximumMovement);
				float num3 = (float)this.worldPositioning.GetDistance(terraformDeviceWorldPosition, army.WorldPosition);
				if (num3 <= propertyValue)
				{
					num2++;
					break;
				}
			}
		}
		if (num > 0)
		{
			attitude.AddScoreModifier(this.attitudeScoreTerraformedInMyTerritoryPositive, (float)num);
			attitude.AddScoreModifier(this.attitudeScoreTerraformedInMyTerritoryNegative, (float)num);
		}
		if (num2 > 0)
		{
			attitude.AddScoreModifier(this.attitudeScoreTerraformedNearMyArmiesPositive, (float)num2);
			attitude.AddScoreModifier(this.attitudeScoreTerraformedNearMyArmiesNegative, (float)num2);
		}
	}

	private void OnDismantleDeviceSuffered(EventDismantleDeviceSuffered raisedEvent)
	{
		if (raisedEvent == null)
		{
			throw new ArgumentNullException("raisedEvent");
		}
		MajorEmpire majorEmpire = raisedEvent.Instigator.Empire as MajorEmpire;
		if (majorEmpire == null)
		{
			throw new ArgumentException("raisedEvent.TerraformingEmpire");
		}
		if (majorEmpire.Index == this.Empire.Index)
		{
			return;
		}
		DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(majorEmpire);
		if (diplomaticRelation.State == null || diplomaticRelation.State.Name.Equals(DiplomaticRelationState.Names.War) || diplomaticRelation.State.Name.Equals(DiplomaticRelationState.Names.Dead))
		{
			return;
		}
		AILayer_Attitude.Attitude attitude = this.GetAttitude(majorEmpire);
		if (attitude == null)
		{
			return;
		}
		if (raisedEvent.Empire.Index == this.Empire.Index)
		{
			attitude.AddScoreModifier(this.attitudeScoreDismantleDeviceSuffered, 1f);
		}
	}

	private void InitializeThirdPartyAggression()
	{
		Diagnostics.Assert(this.diplomaticRelationScoreModifierDatabase != null);
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.EliminatedAcquaintance, out this.attitudeScoreEliminatedAcquaintance))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.EliminatedAcquaintance
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.EliminatedAlly, out this.attitudeScoreEliminatedAlly))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.EliminatedAlly
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.EliminatedEnemy, out this.attitudeScoreEliminatedEnemy))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.EliminatedEnemy
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.EliminatedFriend, out this.attitudeScoreEliminatedFriend))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.EliminatedFriend
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.CityTakenFromAlly, out this.attitudeScoreCityTakenFromAlly))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.CityTakenFromAlly
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.CityTakenFromEnemy, out this.attitudeScoreCityTakenFromEnemy))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.CityTakenFromEnemy
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.CityTakenFromFriend, out this.attitudeScoreCityTakenFromFriend))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.CityTakenFromFriend
			});
		}
	}

	private void RememberLastAggressor(global::Empire victim, global::Empire aggressor)
	{
		if (aggressor != null && aggressor is MajorEmpire && aggressor.Index != victim.Index)
		{
			this.empireLastAggressorIndex[victim.Index] = aggressor.Index;
		}
		else
		{
			this.empireLastAggressorIndex[victim.Index] = -1;
		}
	}

	private void OnCityTaken(global::Empire victim, global::Empire aggressor)
	{
		this.RememberLastAggressor(victim, aggressor);
		if (aggressor == null || !(aggressor is MajorEmpire))
		{
			return;
		}
		if (victim.Index == aggressor.Index)
		{
			return;
		}
		if (victim.Index == this.Empire.Index)
		{
			return;
		}
		if (this.IsRelationAvailableWithMyEmpire(victim) && this.IsRelationAvailableWithMyEmpire(aggressor))
		{
			DepartmentOfForeignAffairs agency = this.Empire.GetAgency<DepartmentOfForeignAffairs>();
			AILayer_Attitude.Attitude attitude = this.GetAttitude(aggressor);
			Diagnostics.Assert(attitude != null);
			AILayer_Attitude.Attitude attitude2 = this.GetAttitude(victim);
			Diagnostics.Assert(attitude2 != null && attitude2.Score != null);
			bool flag = attitude2.Score.GetModifiers(new Predicate<DiplomaticRelationScoreModifier>(AILayer_Attitude.IsForcedStatus)).Any<DiplomaticRelationScoreModifier>();
			DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(victim.Index);
			Diagnostics.Assert(diplomaticRelation != null);
			StaticString name = diplomaticRelation.State.Name;
			if (name == DiplomaticRelationState.Names.Alliance)
			{
				if (!flag)
				{
					attitude.AddScoreModifier(this.attitudeScoreCityTakenFromAlly, 1f);
				}
			}
			else if (name == DiplomaticRelationState.Names.Peace)
			{
				if (!flag)
				{
					attitude.AddScoreModifier(this.attitudeScoreCityTakenFromFriend, 1f);
				}
			}
			else if (name == DiplomaticRelationState.Names.War)
			{
				attitude.AddScoreModifier(this.attitudeScoreCityTakenFromEnemy, 1f);
			}
		}
	}

	private void OnEmpireEliminated(global::Empire victim, global::Empire aggressor)
	{
		if (aggressor.Index == victim.Index)
		{
			return;
		}
		DepartmentOfForeignAffairs agency = this.Empire.GetAgency<DepartmentOfForeignAffairs>();
		AILayer_Attitude.Attitude attitude = this.GetAttitude(aggressor);
		Diagnostics.Assert(attitude != null);
		AILayer_Attitude.Attitude attitude2 = this.GetAttitude(victim);
		Diagnostics.Assert(attitude2 != null && attitude2.Score != null);
		bool flag = attitude2.Score.GetModifiers(new Predicate<DiplomaticRelationScoreModifier>(AILayer_Attitude.IsForcedStatus)).Any<DiplomaticRelationScoreModifier>();
		if (this.IsRelationAvailableWithMyEmpire(victim))
		{
			DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(victim.Index);
			Diagnostics.Assert(diplomaticRelation != null);
			StaticString name = diplomaticRelation.State.Name;
			if (flag || name == DiplomaticRelationState.Names.ColdWar)
			{
				attitude.AddScoreModifier(this.attitudeScoreEliminatedAcquaintance, 1f);
			}
			else if (name == DiplomaticRelationState.Names.Alliance)
			{
				attitude.AddScoreModifier(this.attitudeScoreEliminatedAlly, 1f);
			}
			else if (name == DiplomaticRelationState.Names.Peace)
			{
				attitude.AddScoreModifier(this.attitudeScoreEliminatedFriend, 1f);
			}
			else if (name == DiplomaticRelationState.Names.War)
			{
				attitude.AddScoreModifier(this.attitudeScoreEliminatedEnemy, 1f);
			}
		}
	}

	private void OnThirdPartyAggressionEventRaise(Amplitude.Unity.Event.Event raisedEvent)
	{
		EventCityCaptured eventCityCaptured = raisedEvent as EventCityCaptured;
		if (eventCityCaptured != null)
		{
			this.OnCityTaken(eventCityCaptured.City.Empire, eventCityCaptured.Conqueror);
			return;
		}
		EventCityRazed eventCityRazed = raisedEvent as EventCityRazed;
		if (eventCityRazed != null)
		{
			this.OnCityTaken(eventCityRazed.Empire as global::Empire, (!eventCityRazed.DestroyingArmyIsPrivateers) ? eventCityRazed.Destroyer : null);
			return;
		}
		EventEmpireEliminated eventEmpireEliminated = raisedEvent as EventEmpireEliminated;
		if (eventEmpireEliminated != null)
		{
			global::Empire empire = eventEmpireEliminated.EliminatedEmpire as global::Empire;
			Diagnostics.Assert(empire != null);
			if (empire.Index == this.Empire.Index)
			{
				return;
			}
			int num = this.empireLastAggressorIndex[eventEmpireEliminated.EliminatedEmpire.Index];
			if (num == this.Empire.Index)
			{
				return;
			}
			if (num >= 0)
			{
				Diagnostics.Assert(num < this.game.Empires.Length);
				global::Empire empire2 = this.game.Empires[num];
				Diagnostics.Assert(empire2 != null);
				this.OnEmpireEliminated(empire, empire2);
			}
		}
	}

	private void InitializeTraitor()
	{
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.BetrayedYourAlly, out this.attitudeScoreBetrayedYourAlly))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.BetrayedYourAlly
			});
		}
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.BetrayedYourFriend, out this.attitudeScoreBetrayedYourFriend))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.BetrayedYourFriend
			});
		}
	}

	private void OnTraitorEventRaise(Amplitude.Unity.Event.Event raisedEvent)
	{
		EventDiplomaticRelationStateChange eventDiplomaticRelationStateChange = raisedEvent as EventDiplomaticRelationStateChange;
		if (eventDiplomaticRelationStateChange == null)
		{
			return;
		}
		Diagnostics.Assert(this.majorEmpires != null);
		Diagnostics.Assert(this.attitudeScores != null);
		Diagnostics.Assert(this.departmentOfForeignAffairs != null);
		global::Empire empire = eventDiplomaticRelationStateChange.Empire as global::Empire;
		global::Empire empireWithWhichTheStatusChange = eventDiplomaticRelationStateChange.EmpireWithWhichTheStatusChange;
		if (empire == this.Empire || empireWithWhichTheStatusChange == this.Empire)
		{
			return;
		}
		if (!this.IsRelationAvailableWithMyEmpire(empire) || !this.IsRelationAvailableWithMyEmpire(empireWithWhichTheStatusChange))
		{
			return;
		}
		DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(empireWithWhichTheStatusChange);
		Diagnostics.Assert(diplomaticRelation != null);
		if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.War)
		{
			return;
		}
		if (eventDiplomaticRelationStateChange.DiplomaticRelationStateName != DiplomaticRelationState.Names.War)
		{
			return;
		}
		bool flag = eventDiplomaticRelationStateChange.PreviousDiplomaticRelationStateName == DiplomaticRelationState.Names.Alliance;
		bool flag2 = eventDiplomaticRelationStateChange.PreviousDiplomaticRelationStateName == DiplomaticRelationState.Names.Peace;
		if (!flag && !flag2)
		{
			return;
		}
		AILayer_Attitude.Attitude attitude = this.GetAttitude(empire);
		Diagnostics.Assert(attitude != null && attitude.Score != null);
		Diagnostics.Assert(!flag || !flag2);
		if (flag)
		{
			attitude.AddScoreModifier(this.attitudeScoreBetrayedYourAlly, 1f);
		}
		else if (flag2)
		{
			attitude.AddScoreModifier(this.attitudeScoreBetrayedYourFriend, 1f);
		}
	}

	private void InitializeUnitsEnterMyTerritory()
	{
		Diagnostics.Assert(this.diplomaticRelationScoreModifierDatabase != null);
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.UnitsInMyTerritory, out this.attitudeScoreUnitsInMyTerritory))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.UnitsInMyTerritory
			});
		}
		this.maySeeThroughCatspaw = this.CanUseCatspaw();
	}

	private void UpdateUnitsEnterMyTerritoryModifiers(StaticString context, StaticString pass)
	{
		Diagnostics.Assert(this.refreshedUnitGuidCache != null);
		this.refreshedUnitGuidCache.Clear();
		this.refreshedRegionIndex.Clear();
		DepartmentOfTheInterior agency = this.Empire.GetAgency<DepartmentOfTheInterior>();
		Diagnostics.Assert(agency != null && agency.Cities != null);
		for (int i = 0; i < agency.Cities.Count; i++)
		{
			City city = agency.Cities[i];
			Diagnostics.Assert(city != null);
			Diagnostics.Assert(city.Empire != null && city.Empire.Index == this.Empire.Index);
			Region region = city.Region;
			Diagnostics.Assert(region != null);
			foreach (Army army in Intelligence.GetVisibleArmiesInRegion(region.Index, this.Empire))
			{
				this.UpdateArmy(army);
			}
		}
		for (int j = 0; j < agency.OccupiedFortresses.Count; j++)
		{
			int index = agency.OccupiedFortresses[j].Region.Index;
			if (!this.refreshedRegionIndex.Contains(index))
			{
				this.refreshedRegionIndex.Add(index);
				foreach (Army army2 in Intelligence.GetVisibleArmiesInRegion(index, this.Empire))
				{
					this.UpdateArmy(army2);
				}
			}
		}
		Diagnostics.Assert(this.majorEmpires != null);
		for (int k = 0; k < this.majorEmpires.Length; k++)
		{
			MajorEmpire majorEmpire = this.majorEmpires[k];
			Diagnostics.Assert(majorEmpire != null);
			if (this.Empire.Index != majorEmpire.Index)
			{
				DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(majorEmpire);
				Diagnostics.Assert(diplomaticRelation != null);
				AILayer_Attitude.Attitude attitude = this.GetAttitude(majorEmpire);
				Diagnostics.Assert(attitude != null);
				Diagnostics.Assert(attitude.UnitModifiersInfoByUnitGuid != null);
				foreach (KeyValuePair<GameEntityGUID, AILayer_Attitude.Attitude.UnitModifiersInfo> keyValuePair in attitude.UnitModifiersInfoByUnitGuid)
				{
					AILayer_Attitude.Attitude.UnitModifiersInfo value = keyValuePair.Value;
					if (value.UnitsInMyTerritoryModifierId >= 0)
					{
						if (!this.refreshedUnitGuidCache.Contains(keyValuePair.Key))
						{
							if (!attitude.Score.RemoveModifier(value.UnitsInMyTerritoryModifierId))
							{
								AILayer.LogError("Can't remove modifier {0}", new object[]
								{
									value.UnitsInMyTerritoryModifierId
								});
							}
							value.UnitsInMyTerritoryModifierId = -1;
						}
					}
				}
				if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.War && attitude.WarFatigueModifierId < 0)
				{
					attitude.WarFatigueModifierId = attitude.AddScoreModifier(this.attitudeScoreWarFatigue, 1f);
				}
				else if (diplomaticRelation.State.Name != DiplomaticRelationState.Names.War && attitude.WarFatigueModifierId >= 0)
				{
					attitude.Score.RemoveModifier(attitude.WarFatigueModifierId);
					attitude.WarFatigueModifierId = -1;
				}
			}
		}
	}

	private void UpdateArmy(Army army)
	{
		if (army.Empire.Index == this.Empire.Index)
		{
			return;
		}
		MajorEmpire majorEmpire = army.Empire as MajorEmpire;
		if (majorEmpire == null)
		{
			return;
		}
		AILayer_Attitude.Attitude attitude = this.GetAttitude(majorEmpire);
		Diagnostics.Assert(attitude != null);
		bool flag = true;
		if (!this.maySeeThroughCatspaw && army.IsPrivateers)
		{
			flag = false;
		}
		if (this.diplomacyLayer.GetPeaceWish(majorEmpire.Index))
		{
			return;
		}
		DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(majorEmpire);
		bool flag2 = diplomaticRelation.State != null && diplomaticRelation.State.Name == DiplomaticRelationState.Names.War;
		foreach (Unit unit in army.Units)
		{
			if (flag2 || !unit.CheckUnitAbility(UnitAbility.ReadonlyHarbinger, -1))
			{
				AILayer_Attitude.Attitude.UnitModifiersInfo unitModifiersInfo;
				if (!attitude.UnitModifiersInfoByUnitGuid.TryGetValue(unit.GUID, out unitModifiersInfo))
				{
					unitModifiersInfo = new AILayer_Attitude.Attitude.UnitModifiersInfo();
					attitude.UnitModifiersInfoByUnitGuid.Add(unit.GUID, unitModifiersInfo);
				}
				if (flag && unitModifiersInfo.UnitsInMyTerritoryModifierId < 0)
				{
					unitModifiersInfo.UnitsInMyTerritoryModifierId = attitude.AddScoreModifier(this.attitudeScoreUnitsInMyTerritory, 1f);
				}
				else if (!flag && unitModifiersInfo.UnitsInMyTerritoryModifierId >= 0)
				{
					if (!attitude.Score.RemoveModifier(unitModifiersInfo.UnitsInMyTerritoryModifierId))
					{
						AILayer.LogError("Can't remove modifier {0}", new object[]
						{
							unitModifiersInfo.UnitsInMyTerritoryModifierId
						});
					}
					unitModifiersInfo.UnitsInMyTerritoryModifierId = -1;
				}
				this.refreshedUnitGuidCache.Add(unit.GUID);
			}
		}
	}

	private bool CanUseCatspaw()
	{
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		return service != null && service.IsShared(DownloadableContent16.ReadOnlyName) && base.AIEntity.Empire.SimulationObject.Tags.Contains(DownloadableContent16.FactionTraitCatspaw);
	}

	private void OnVictoryConditionAlertEventRaise(Amplitude.Unity.Event.Event raisedEvent)
	{
		EventVictoryConditionAlert eventVictoryConditionAlert = raisedEvent as EventVictoryConditionAlert;
		if (eventVictoryConditionAlert == null)
		{
			return;
		}
		if (eventVictoryConditionAlert.Empire.Index == this.Empire.Index)
		{
			return;
		}
		Diagnostics.Assert(this.diplomaticRelationScoreModifierDatabase != null);
		DiplomaticRelationScoreModifierDefinition relationModifierDefinition;
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(string.Format("AttitudeScoreVictoryAlert{0}", eventVictoryConditionAlert.VictoryCondition.Name), out relationModifierDefinition))
		{
			return;
		}
		AILayer_Attitude.Attitude attitude = this.GetAttitude(eventVictoryConditionAlert.Empire);
		Diagnostics.Assert(attitude != null);
		if ((!this.SharedVictory || this.departmentOfForeignAffairs.DiplomaticRelations[eventVictoryConditionAlert.Empire.Index].State.Name != DiplomaticRelationState.Names.Alliance) && attitude.Score.CountModifiers((DiplomaticRelationScoreModifier modifier) => modifier.Definition.Name == relationModifierDefinition.Name) <= 0)
		{
			attitude.AddScoreModifier(relationModifierDefinition, 1f);
		}
	}

	private void InitializeVillageConverted()
	{
		Diagnostics.Assert(this.diplomaticRelationScoreModifierDatabase != null);
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.VillageConverted, out this.attitudeScoreVillageConverted))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.VillageConverted
			});
		}
	}

	private void OnVillageConvertedEventRaise(Amplitude.Unity.Event.Event raisedEvent)
	{
		EventVillageConverted eventVillageConverted = raisedEvent as EventVillageConverted;
		if (eventVillageConverted == null)
		{
			return;
		}
		Diagnostics.Assert(eventVillageConverted.Village != null);
		Region region = eventVillageConverted.Village.Region;
		Diagnostics.Assert(region != null);
		if (region.City == null || region.City.Empire == null || region.City.Empire.Index != this.Empire.Index)
		{
			return;
		}
		MajorEmpire majorEmpire = eventVillageConverted.Empire as MajorEmpire;
		if (majorEmpire.Index == this.Empire.Index)
		{
			return;
		}
		AILayer_Attitude.Attitude attitude = this.GetAttitude(majorEmpire);
		Diagnostics.Assert(attitude != null);
		attitude.AddScoreModifier(this.attitudeScoreVillageConverted, 1f);
		attitude.Score.RemoveModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == AILayer_Attitude.AttitudeScoreDefinitionReferences.Peaceful);
	}

	private void InitializeWarFatigue()
	{
		Diagnostics.Assert(this.diplomaticRelationScoreModifierDatabase != null);
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.WarFatigue, out this.attitudeScoreWarFatigue))
		{
			AILayer.LogError("Can't retrieve {0} modifier.", new object[]
			{
				AILayer_Attitude.AttitudeScoreDefinitionReferences.WarFatigue
			});
		}
	}

	private void UpdateWarFatigueModifiers(StaticString context, StaticString pass)
	{
		Diagnostics.Assert(this.majorEmpires != null);
		for (int i = 0; i < this.majorEmpires.Length; i++)
		{
			MajorEmpire majorEmpire = this.majorEmpires[i];
			if (this.Empire.Index != majorEmpire.Index)
			{
				DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(majorEmpire);
				Diagnostics.Assert(diplomaticRelation != null);
				AILayer_Attitude.Attitude attitude = this.GetAttitude(majorEmpire);
				Diagnostics.Assert(attitude != null);
				if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.War && attitude.WarFatigueModifierId < 0)
				{
					attitude.WarFatigueModifierId = attitude.AddScoreModifier(this.attitudeScoreWarFatigue, 1f);
				}
				else if (diplomaticRelation.State.Name != DiplomaticRelationState.Names.War && attitude.WarFatigueModifierId >= 0)
				{
					if (!attitude.Score.RemoveModifier(attitude.WarFatigueModifierId))
					{
						AILayer.LogError("Can't remove modifier {0}", new object[]
						{
							attitude.WarFatigueModifierId
						});
					}
					attitude.WarFatigueModifierId = -1;
				}
			}
		}
	}

	public global::Empire Empire
	{
		get
		{
			Diagnostics.Assert(base.AIEntity != null);
			return base.AIEntity.Empire;
		}
	}

	public AILayer_Attitude.Attitude GetAttitude(global::Empire empire)
	{
		if (empire == null)
		{
			throw new ArgumentNullException("empire");
		}
		Diagnostics.Assert(this.attitudeScores != null);
		return this.attitudeScores[empire.Index];
	}

	public AttitudeStateDefinition GetAttitudeStateFromAttitudeCategory(StaticString categoryName, float categoryScore)
	{
		IDatabase<AttitudeStateDefinition> database = Databases.GetDatabase<AttitudeStateDefinition>(false);
		Diagnostics.Assert(database != null);
		foreach (AttitudeStateDefinition attitudeStateDefinition in database)
		{
			Diagnostics.Assert(attitudeStateDefinition != null);
			if (!(attitudeStateDefinition.MainAttitudeCategory != categoryName))
			{
				if (categoryScore <= attitudeStateDefinition.MaximumValue && categoryScore >= attitudeStateDefinition.MinimumValue)
				{
					return attitudeStateDefinition;
				}
			}
		}
		return null;
	}

	public IEnumerable<DiplomaticRelationScore.ModifiersData> GetMainAttitudeModifiersFromCategory(global::Empire targetedEmpire, StaticString categoryName, float categoryScore)
	{
		AILayer_Attitude.Attitude attitude = this.GetAttitude(targetedEmpire);
		if (attitude == null)
		{
			yield break;
		}
		IRelationScoreProvider relationScoreProvider = attitude.Score;
		Diagnostics.Assert(relationScoreProvider != null);
		if (relationScoreProvider.MofifiersDatas == null)
		{
			yield break;
		}
		foreach (DiplomaticRelationScore.ModifiersData modifiersData in relationScoreProvider.MofifiersDatas)
		{
			Diagnostics.Assert(modifiersData != null && !StaticString.IsNullOrEmpty(modifiersData.Category));
			if (!(modifiersData.Category != categoryName))
			{
				if (Math.Abs(Mathf.Sign(modifiersData.TotalValue) - Mathf.Sign(categoryScore)) <= 1.401298E-45f)
				{
					yield return modifiersData;
				}
			}
		}
		yield break;
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		this.diplomaticRelationScoreModifierDatabase = Databases.GetDatabase<DiplomaticRelationScoreModifierDefinition>(false);
		Diagnostics.Assert(base.AIEntity.Empire != null);
		this.departmentOfForeignAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		Diagnostics.Assert(this.departmentOfForeignAffairs != null);
		this.departmentOfForeignAffairs.DiplomaticRelationStateChange += this.DepartmentOfForeignAffairs_DiplomaticRelationStateChange;
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.game = (service.Game as global::Game);
		Diagnostics.Assert(this.game != null && this.game.Empires != null);
		ISessionService service2 = Services.GetService<ISessionService>();
		this.SharedVictory = service2.Session.GetLobbyData<bool>("Shared", true);
		this.majorEmpires = Array.ConvertAll<global::Empire, MajorEmpire>(Array.FindAll<global::Empire>(this.game.Empires, (global::Empire match) => match is MajorEmpire), (global::Empire empire) => empire as MajorEmpire);
		this.worldPositioning = this.game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositioning != null);
		this.visibilityService = this.game.Services.GetService<IVisibilityService>();
		Diagnostics.Assert(this.visibilityService != null);
		this.gameEntityRepositoryService = this.game.Services.GetService<IGameEntityRepositoryService>();
		Diagnostics.Assert(this.diplomaticRelationScoreModifierDatabase != null);
		if (!this.diplomaticRelationScoreModifierDatabase.TryGetValue(AILayer_Attitude.AttitudeScoreDefinitionReferences.LastWar, out this.attitudeScoreLastWar))
		{
			AILayer.LogError("Can't retrieve AttitudeScoreLastWar modifier.");
		}
		this.InitializeCommonFrontier();
		this.InitializeWarFatigue();
		this.InitializeCommonDiplomaticStatus();
		this.InitializeAggression();
		this.InitializeColonization();
		this.InitializeScore();
		this.InitializeUnitsEnterMyTerritory();
		this.InitializeLivingSpaceTense();
		this.InitializeDiplomaticContract();
		this.InitializeVillageConverted();
		this.InitializePillage();
		this.InitializeSpy();
		this.InitializeOrbs();
		this.InitializeThirdPartyAggression();
		this.InitializeLongTermRelationship();
		this.InitializeAlliedWarEffort();
		this.InitializeTraitor();
		this.InitializeFortress();
		this.InitializeTerraform();
		this.InitializeCreepingNodes();
		this.InitializeKaijus();
		this.attitudeScores = new AILayer_Attitude.Attitude[this.majorEmpires.Length];
		for (int i = 0; i < this.majorEmpires.Length; i++)
		{
			MajorEmpire majorEmpire = this.majorEmpires[i];
			if (majorEmpire.Index != this.Empire.Index)
			{
				this.attitudeScores[i] = new AILayer_Attitude.Attitude(this.Empire, majorEmpire, this.majorEmpires.Length);
			}
		}
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_Attitude_UpdateCommonDiplomaticStatusModifiers", new AIEntity.AIAction(this.UpdateCommonDiplomaticStatusModifiers), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_Attitude_UpdateCommonFrontierModifiers", new AIEntity.AIAction(this.UpdateCommonFrontierModifiers), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_Attitude_UpdateWarFatigueModifiers", new AIEntity.AIAction(this.UpdateWarFatigueModifiers), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_Attitude_UpdateScoreModifiers", new AIEntity.AIAction(this.UpdateScoreModifiers), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_Attitude_UpdatePillageModifiers", new AIEntity.AIAction(this.UpdatePillageModifiers), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_Attitude_UpdateUnitsEnterMyTerritoryModifiers", new AIEntity.AIAction(this.UpdateUnitsEnterMyTerritoryModifiers), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_Attitude_UpdateLivingSpaceTenseModifiers", new AIEntity.AIAction(this.UpdateLivingSpaceTenseModifiers), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_Attitude_UpdateAgressionModifiers", new AIEntity.AIAction(this.UpdateAgressionModifiers), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_Attitude_UpdateOrbsModifiers", new AIEntity.AIAction(this.UpdateOrbsModifiers), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_Attitude_UpdateAttitudeScores", new AIEntity.AIAction(this.UpdateAttitudeScores), this, new StaticString[]
		{
			"AILayer_Attitude_UpdateModifierChanges",
			"AILayer_Attitude_UpdateCommonFrontierModifiers",
			"AILayer_Attitude_UpdateWarFatigueModifiers",
			"AILayer_Attitude_UpdateScoreModifiers",
			"AILayer_Attitude_UpdateUnitsEnterMyTerritoryModifiers",
			"AILayer_Attitude_UpdateLivingSpaceTenseModifiers",
			"AILayer_Attitude_UpdateAgressiveColonizationModifiers",
			"AILayer_Attitude_UpdateAgressionModifiers"
		});
		this.eventService = Services.GetService<IEventService>();
		Diagnostics.Assert(this.eventService != null);
		this.eventService.EventRaise += this.EventService_EventRaise;
		this.diplomacyLayer = base.AIEntity.GetLayer<AILayer_Diplomacy>();
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState != AIPlayer.PlayerState.Deactivated && base.AIEntity.AIPlayer.AIState != AIPlayer.PlayerState.Dead;
	}

	public override void Release()
	{
		base.Release();
		if (this.departmentOfForeignAffairs != null)
		{
			this.departmentOfForeignAffairs.DiplomaticRelationStateChange -= this.DepartmentOfForeignAffairs_DiplomaticRelationStateChange;
			this.departmentOfForeignAffairs = null;
		}
		if (this.eventService != null)
		{
			this.eventService.EventRaise -= this.EventService_EventRaise;
			this.eventService = null;
		}
		this.game = null;
		this.majorEmpires = null;
		this.attitudeScores = new AILayer_Attitude.Attitude[0];
		this.attitudeScoreLastWar = null;
		this.diplomaticRelationScoreModifierDatabase = null;
		this.scoresByNameBuffer.Clear();
		this.LastWarHelpInquiry.Clear();
		this.LastWarHelpTarget.Clear();
		this.diplomacyLayer = null;
	}

	public bool TryGetMainAttitudeCategory(global::Empire targetedEmpire, ref StaticString mainAttitudeCategoryName, ref float mainAttitudeCategoryScore)
	{
		AILayer_Attitude.Attitude attitude = this.GetAttitude(targetedEmpire);
		if (attitude == null)
		{
			return false;
		}
		IRelationScoreProvider score = attitude.Score;
		Diagnostics.Assert(score != null);
		if (score.MofifiersDatas == null)
		{
			mainAttitudeCategoryName = StaticString.Empty;
			mainAttitudeCategoryScore = 0f;
			return true;
		}
		Diagnostics.Assert(this.scoresByNameBuffer != null);
		this.scoresByNameBuffer.Clear();
		foreach (DiplomaticRelationScore.ModifiersData modifiersData in score.MofifiersDatas)
		{
			Diagnostics.Assert(modifiersData != null && !StaticString.IsNullOrEmpty(modifiersData.Category));
			if (!this.scoresByNameBuffer.ContainsKey(modifiersData.Category))
			{
				this.scoresByNameBuffer.Add(modifiersData.Category, 0f);
			}
			Dictionary<StaticString, float> dictionary2;
			Dictionary<StaticString, float> dictionary = dictionary2 = this.scoresByNameBuffer;
			StaticString category;
			StaticString key = category = modifiersData.Category;
			float num = dictionary2[category];
			dictionary[key] = num + modifiersData.TotalValue;
		}
		float num2 = -1f;
		foreach (KeyValuePair<StaticString, float> keyValuePair in this.scoresByNameBuffer)
		{
			float num3 = Mathf.Abs(keyValuePair.Value);
			if (num3 > num2)
			{
				num2 = num3;
				mainAttitudeCategoryName = keyValuePair.Key;
				mainAttitudeCategoryScore = keyValuePair.Value;
			}
		}
		return true;
	}

	protected bool IsRelationAvailableWithMyEmpire(global::Empire opponentEmpire)
	{
		Diagnostics.Assert(opponentEmpire != null);
		Diagnostics.Assert(this.departmentOfForeignAffairs != null);
		DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(opponentEmpire);
		Diagnostics.Assert(diplomaticRelation != null);
		return diplomaticRelation.State != null && diplomaticRelation.State.Name != DiplomaticRelationState.Names.Unknown && diplomaticRelation.State.Name != DiplomaticRelationState.Names.Dead;
	}

	protected void UpdateAttitudeScores(StaticString context, StaticString pass)
	{
		ICommunicationAIHelper service = AIScheduler.Services.GetService<ICommunicationAIHelper>();
		Diagnostics.Assert(service != null);
		Diagnostics.Assert(this.majorEmpires != null && this.attitudeScores != null);
		for (int i = 0; i < this.majorEmpires.Length; i++)
		{
			MajorEmpire majorEmpire = this.majorEmpires[i];
			if (majorEmpire.Index != this.Empire.Index)
			{
				Diagnostics.Assert(this.attitudeScores[i] != null);
				this.attitudeScores[i].EndTurnUpdate();
				service.RegisterAttitudeChange(this.Empire as MajorEmpire, majorEmpire, this.attitudeScores[i]);
			}
		}
	}

	private void DepartmentOfForeignAffairs_DiplomaticRelationStateChange(object sender, DiplomaticRelationStateChangeEventArgs eventArgs)
	{
		if (eventArgs.PreviousDiplomaticRelationState != null && eventArgs.PreviousDiplomaticRelationState.Name == DiplomaticRelationState.Names.War)
		{
			AILayer_Attitude.Attitude attitude = this.GetAttitude(eventArgs.EmpireWithWhichTheStatusChange);
			Diagnostics.Assert(attitude != null && attitude.Score != null);
			attitude.AddScoreModifier(this.attitudeScoreLastWar, 1f);
			attitude.Score.RemoveModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == AILayer_Attitude.AttitudeScoreDefinitionReferences.ArmyAggressionInNeutralRegionDuringColdWar);
			attitude.Score.RemoveModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireUnitsKilledDuringWar);
			attitude.Score.RemoveModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireUnitsKilledDuringWar);
			attitude.Score.RemoveModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireCityBesiegedDuringWar);
			attitude.Score.RemoveModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireCityBesiegedDuringWar);
			attitude.Score.RemoveModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireCitiesTakenDuringWar);
			attitude.Score.RemoveModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireCitiesTakenDuringWar);
			attitude.Score.RemoveModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireFortressesTakenDuringWar);
			attitude.Score.RemoveModifiers((DiplomaticRelationScoreModifier match) => match.Definition.Name == AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireFortressesTakenDuringWar);
		}
	}

	private void EventService_EventRaise(object sender, EventRaiseEventArgs e)
	{
		this.OnAggressionEventRaise(e.RaisedEvent);
		this.OnContractEventRaise(e.RaisedEvent);
		this.OnColonizationEventRaise(e.RaisedEvent);
		this.OnVillageConvertedEventRaise(e.RaisedEvent);
		this.OnPillageSucceedEventRaise(e.RaisedEvent);
		this.OnSpyEventRaise(e.RaisedEvent);
		this.OnOrbEventRaise(e.RaisedEvent);
		this.OnVictoryConditionAlertEventRaise(e.RaisedEvent);
		this.OnThirdPartyAggressionEventRaise(e.RaisedEvent);
		this.OnLongTermRelationshipEventRaise(e.RaisedEvent);
		this.OnAlliedWarEffortEventRaise(e.RaisedEvent);
		this.OnTraitorEventRaise(e.RaisedEvent);
		this.OnFortressEventRaised(e.RaisedEvent);
		this.OnTerraformEventRaise(e.RaisedEvent);
		this.OnCreepingNodeEventRaise(e.RaisedEvent);
		this.OnKaijuEventRaise(e.RaisedEvent);
	}

	private void WriteDictionnary(XmlWriter writer, string name, Dictionary<int, int> dictionary)
	{
		writer.WriteStartElement(name);
		writer.WriteAttributeString<int>("Count", dictionary.Count);
		foreach (KeyValuePair<int, int> keyValuePair in dictionary)
		{
			writer.WriteStartElement("KeyValuePair");
			writer.WriteAttributeString<int>("Key", keyValuePair.Key);
			writer.WriteAttributeString<int>("Value", keyValuePair.Value);
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
	}

	private void ReadDictionnary(XmlReader reader, string name, Dictionary<int, int> dictionary)
	{
		if (reader.IsStartElement(name))
		{
			int attribute = reader.GetAttribute<int>("Count");
			if (attribute > 0)
			{
				reader.ReadStartElement(name);
				for (int i = 0; i < attribute; i++)
				{
					int attribute2 = reader.GetAttribute<int>("Key");
					int attribute3 = reader.GetAttribute<int>("Value");
					reader.Skip();
					dictionary[attribute2] = attribute3;
				}
				reader.ReadEndElement(name);
				return;
			}
			reader.Skip();
		}
	}

	private DiplomaticRelationScoreModifierDefinition attitudeScoreArmyAggressionInNeutralRegionDuringColdWar;

	private List<global::Empire> empireCache;

	private DiplomaticRelationScoreModifierDefinition myEmpireCitiesTakenDuringWarDefinition;

	private DiplomaticRelationScoreModifierDefinition myEmpireCityBesiegedDuringWarDefinition;

	private DiplomaticRelationScoreModifierDefinition myEmpireUnitsKilledDuringWarDefinition;

	private DiplomaticRelationScoreModifierDefinition otherEmpireCitiesTakenDuringWarDefinition;

	private DiplomaticRelationScoreModifierDefinition otherEmpireCityBesiegedDuringWarDefinition;

	private DiplomaticRelationScoreModifierDefinition otherEmpireUnitsKilledDuringWarDefinition;

	private DiplomaticRelationScoreModifierDefinition peacefulModifierDefinition;

	private DiplomaticRelationScoreModifierDefinition regionalBuildingDestroyedDefinition;

	private DiplomaticRelationScoreModifierDefinition myEmpireCitiesBombardedDuringWarDefinition;

	private DiplomaticRelationScoreModifierDefinition otherEmpireCitiesBombardedDuringWarDefinition;

	private List<Fortress> sameRegionFortresses;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreWarWithMyAlly;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreAlliedWithMyEnemy;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreAggressiveColonization;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreCommonAllyModifier;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreCommonEnemyModifier;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreCommonFriendModifier;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreCommonFrontier;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreCreepingNodeUpgradeComplete;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreDismantleCreepingNodeSuffered;

	private DiplomaticRelationScoreModifierDefinition giftDefinition;

	private DiplomaticRelationScoreModifierDefinition negativeContractDefinition;

	private DiplomaticRelationScoreModifierDefinition positiveContractDefinition;

	private DiplomaticRelationScoreModifierDefinition blackSpotDefinition;

	private DiplomaticRelationScoreModifierDefinition forcedStatusDefinition;

	private DiplomaticRelationScoreModifierDefinition myEmpireFortressesTakenDuringWarDefinition;

	private DiplomaticRelationScoreModifierDefinition otherEmpireFortressesTakenDuringWarDefinition;

	private DiplomaticRelationScoreModifierDefinition myEmpireLostNavalRegionControlDuringWarDefinition;

	private DiplomaticRelationScoreModifierDefinition otherEmpireLostNavalRegionControlDuringWarDefinition;

	private DiplomaticRelationScoreModifierDefinition otherEmpireHasUniqueFacilityDefinition;

	private DiplomaticRelationScoreModifierDefinition fortressesOwnedInTheSameRegionDefinition;

	private DiplomaticRelationScoreModifierDefinition myEmpireFortressesTakenDuringColdWarDefinition;

	private DiplomaticRelationScoreModifierDefinition AttitudeScoreKaijuAttacked;

	private DiplomaticRelationScoreModifierDefinition AttitudeScoreKaijuPlacedNearMyCityNegative;

	private DiplomaticRelationScoreModifierDefinition AttitudeScoreKaijuPlacedNearMyCityPositive;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreLivingSpaceTense;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreLongTermAllianceModifier;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreLongTermPeaceModifier;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreMantaAspiratingInMyTerritory;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreOrbsStollen;

	private DiplomaticRelationScoreModifierDefinition attitudeScorePillageSucceed;

	private DiplomaticRelationScoreModifierDefinition attitudeScorePillaging;

	private AILayer_Attitude.ComparativeModifierRule[] comparativeModifierRule;

	private AILayer_Attitude.ComparativeModifierRule landMilitaryPowerRule;

	private AILayer_Attitude.ComparativeModifierRule navalMilitaryPowerRule;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreSpy;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreDismantleDeviceSuffered;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreTerraformedInMyTerritoryPositive;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreTerraformedInMyTerritoryNegative;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreTerraformedNearMyArmiesPositive;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreTerraformedNearMyArmiesNegative;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreEliminatedAcquaintance;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreEliminatedAlly;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreEliminatedEnemy;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreEliminatedFriend;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreCityTakenFromAlly;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreCityTakenFromEnemy;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreCityTakenFromFriend;

	private int[] empireLastAggressorIndex;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreBetrayedYourFriend;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreBetrayedYourAlly;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreUnitsInMyTerritory;

	private List<GameEntityGUID> refreshedUnitGuidCache;

	private List<int> refreshedRegionIndex;

	private bool maySeeThroughCatspaw;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreVillageConverted;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreWarFatigue;

	private DiplomaticRelationScoreModifierDefinition attitudeScoreLastWar;

	private AILayer_Attitude.Attitude[] attitudeScores;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private IDatabase<DiplomaticRelationScoreModifierDefinition> diplomaticRelationScoreModifierDatabase;

	private global::Game game;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private MajorEmpire[] majorEmpires;

	private Dictionary<StaticString, float> scoresByNameBuffer;

	private IVisibilityService visibilityService;

	private IWorldPositionningService worldPositioning;

	private IEventService eventService;

	private Dictionary<int, int> LastWarHelpInquiry;

	private Dictionary<int, int> LastWarHelpTarget;

	private bool SharedVictory;

	private AILayer_Diplomacy diplomacyLayer;

	public class Attitude : IXmlSerializable
	{
		public Attitude(global::Empire attitudeOwnerEmpire, global::Empire empire, int majorEmpireCount)
		{
			this.AttitudeOwnerEmpire = attitudeOwnerEmpire;
			this.OtherEmpire = empire;
			float defaultTurnDurationFactor = (this.OtherEmpire == null) ? 1f : this.OtherEmpire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier);
			this.Score = new DiplomaticRelationScore(defaultTurnDurationFactor, -100f, 100f);
			this.CommonEnemiesModifierIds = new int[majorEmpireCount];
			this.CommonFriendsModifierIds = new int[majorEmpireCount];
			this.CommonAlliesModifierIds = new int[majorEmpireCount];
			for (int i = 0; i < majorEmpireCount; i++)
			{
				this.CommonEnemiesModifierIds[i] = -1;
				this.CommonFriendsModifierIds[i] = -1;
				this.CommonAlliesModifierIds[i] = -1;
			}
			this.WarFatigueModifierId = -1;
			this.personalityHelper = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		}

		public global::Empire AttitudeOwnerEmpire { get; private set; }

		public global::Empire OtherEmpire { get; private set; }

		public DiplomaticRelationScore Score { get; private set; }

		public int AddScoreModifier(DiplomaticRelationScoreModifierDefinition definition, float multiplier = 1f)
		{
			IPersonalityAIHelper service = AIScheduler.Services.GetService<IPersonalityAIHelper>();
			string path = string.Format("DiplomaticRelationScoreModifier/{0}/CurveReference", definition.Name);
			string value = service.GetValue<string>(this.AttitudeOwnerEmpire, path, definition.CurveReference.ToString());
			string regitryPath = string.Format("AI/MajorEmpire/AIEntity_Empire/AILayer_Attitude/{0}", definition.Name);
			float registryValue = this.personalityHelper.GetRegistryValue<float>(this.AttitudeOwnerEmpire, regitryPath, 1f);
			return this.Score.AddModifier(definition, multiplier, registryValue, value);
		}

		public void EndTurnUpdate()
		{
			this.Score.EndTurnUpdate();
		}

		public void ReadXml(XmlReader reader)
		{
			int attribute = reader.GetAttribute<int>("EmpireIndex");
			Diagnostics.Assert(this.OtherEmpire.Index == attribute);
			reader.ReadStartElement();
			IXmlSerializable score = this.Score;
			reader.ReadElementSerializable<IXmlSerializable>("Score", ref score);
			this.WarFatigueModifierId = reader.ReadElementString<int>("WarFatigueModifierId");
			this.ReadDictionaryWithSerializable<AILayer_Attitude.Attitude.CityModifiersInfo>(reader, "CityModifiersInfoByCityGuid", this.CityModifiersInfoByCityGuid);
			int attribute2 = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("CommonAlliesModifierIds");
			Diagnostics.Assert(this.CommonAlliesModifierIds != null && this.CommonAlliesModifierIds.Length == attribute2);
			for (int i = 0; i < this.CommonAlliesModifierIds.Length; i++)
			{
				this.CommonAlliesModifierIds[i] = reader.ReadElementString<int>("ModifierId");
			}
			reader.ReadEndElement("CommonAlliesModifierIds");
			attribute2 = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("CommonEnemiesModifierIds");
			Diagnostics.Assert(this.CommonEnemiesModifierIds != null && this.CommonEnemiesModifierIds.Length == attribute2);
			for (int j = 0; j < this.CommonEnemiesModifierIds.Length; j++)
			{
				this.CommonEnemiesModifierIds[j] = reader.ReadElementString<int>("ModifierId");
			}
			reader.ReadEndElement("CommonEnemiesModifierIds");
			attribute2 = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("CommonFriendsModifierIds");
			Diagnostics.Assert(this.CommonFriendsModifierIds != null && this.CommonFriendsModifierIds.Length == attribute2);
			for (int k = 0; k < this.CommonFriendsModifierIds.Length; k++)
			{
				this.CommonFriendsModifierIds[k] = reader.ReadElementString<int>("ModifierId");
			}
			reader.ReadEndElement("CommonFriendsModifierIds");
			attribute2 = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("Frontiers");
			for (int l = 0; l < attribute2; l++)
			{
				AILayer_Attitude.Attitude.EmpireFrontier item = new AILayer_Attitude.Attitude.EmpireFrontier(-1, -1);
				reader.ReadElementSerializable<AILayer_Attitude.Attitude.EmpireFrontier>(ref item);
				this.Frontiers.Add(item);
			}
			reader.ReadEndElement("Frontiers");
			this.ReadDictionaryWithSerializable<AILayer_Attitude.Attitude.UnitModifiersInfo>(reader, "UnitModifiersInfoByUnitGuid", this.UnitModifiersInfoByUnitGuid);
			this.ReadDictionnary<int>(reader, "PillageModifierByPointOfInterestGuid", this.PillageModifierByPointOfInterestGuid);
			this.ReadDictionnary<int>(reader, "FortressModifiersInfoByFortressGuid", this.FortressModifiersInfoByFortressGuid);
			if (reader.IsStartElement("OceanRegionModifiersInfoByRegionIndex"))
			{
				attribute2 = reader.GetAttribute<int>("Count");
				if (attribute2 > 0)
				{
					reader.ReadStartElement("OceanRegionModifiersInfoByRegionIndex");
					for (int m = 0; m < attribute2; m++)
					{
						int attribute3 = reader.GetAttribute<int>("Key");
						reader.ReadStartElement("KeyValuePair");
						AILayer_Attitude.Attitude.NavalRegionModifiersInfo value = new AILayer_Attitude.Attitude.NavalRegionModifiersInfo();
						reader.ReadElementSerializable<AILayer_Attitude.Attitude.NavalRegionModifiersInfo>("Value", ref value);
						reader.ReadEndElement("KeyValuePair");
						this.OceanRegionModifiersInfoByRegionIndex.Add(attribute3, value);
					}
					reader.ReadEndElement("OceanRegionModifiersInfoByRegionIndex");
				}
				else
				{
					reader.Skip();
				}
			}
			this.ReadDictionnary<int>(reader, "FacilityModifiersByFacilityGuid", this.FacilityModifiersByFacilityGuid);
		}

		public void WriteXml(XmlWriter writer)
		{
			writer.WriteAttributeString<int>("EmpireIndex", this.OtherEmpire.Index);
			IXmlSerializable score = this.Score;
			writer.WriteElementSerializable<IXmlSerializable>("Score", ref score);
			writer.WriteElementString<int>("WarFatigueModifierId", this.WarFatigueModifierId);
			this.WriteDictionnaryWithSerializable<AILayer_Attitude.Attitude.CityModifiersInfo>(writer, "CityModifiersInfoByCityGuid", this.CityModifiersInfoByCityGuid);
			writer.WriteStartElement("CommonAlliesModifierIds");
			writer.WriteAttributeString<int>("Count", this.CommonAlliesModifierIds.Length);
			for (int i = 0; i < this.CommonAlliesModifierIds.Length; i++)
			{
				writer.WriteElementString<int>("ModifierId", this.CommonAlliesModifierIds[i]);
			}
			writer.WriteEndElement();
			writer.WriteStartElement("CommonEnemiesModifierIds");
			writer.WriteAttributeString<int>("Count", this.CommonEnemiesModifierIds.Length);
			for (int j = 0; j < this.CommonEnemiesModifierIds.Length; j++)
			{
				writer.WriteElementString<int>("ModifierId", this.CommonEnemiesModifierIds[j]);
			}
			writer.WriteEndElement();
			writer.WriteStartElement("CommonFriendsModifierIds");
			writer.WriteAttributeString<int>("Count", this.CommonFriendsModifierIds.Length);
			for (int k = 0; k < this.CommonFriendsModifierIds.Length; k++)
			{
				writer.WriteElementString<int>("ModifierId", this.CommonFriendsModifierIds[k]);
			}
			writer.WriteEndElement();
			writer.WriteStartElement("Frontiers");
			writer.WriteAttributeString<int>("Count", this.Frontiers.Count);
			for (int l = 0; l < this.Frontiers.Count; l++)
			{
				IXmlSerializable xmlSerializable = this.Frontiers[l];
				writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
			}
			writer.WriteEndElement();
			this.WriteDictionnaryWithSerializable<AILayer_Attitude.Attitude.UnitModifiersInfo>(writer, "UnitModifiersInfoByUnitGuid", this.UnitModifiersInfoByUnitGuid);
			this.WriteDictionnary<int>(writer, "PillageModifierByPointOfInterestGuid", this.PillageModifierByPointOfInterestGuid);
			this.WriteDictionnary<int>(writer, "FortressModifiersInfoByFortressGuid", this.FortressModifiersInfoByFortressGuid);
			writer.WriteStartElement("OceanRegionModifiersInfoByRegionIndex");
			writer.WriteAttributeString<int>("Count", this.OceanRegionModifiersInfoByRegionIndex.Count);
			foreach (KeyValuePair<int, AILayer_Attitude.Attitude.NavalRegionModifiersInfo> keyValuePair in this.OceanRegionModifiersInfoByRegionIndex)
			{
				writer.WriteStartElement("KeyValuePair");
				writer.WriteAttributeString<int>("Key", keyValuePair.Key);
				IXmlSerializable value = keyValuePair.Value;
				writer.WriteElementSerializable<IXmlSerializable>("Value", ref value);
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
			this.WriteDictionnary<int>(writer, "FacilityModifiersByFacilityGuid", this.FacilityModifiersByFacilityGuid);
		}

		private void WriteDictionnary<Value>(XmlWriter writer, string name, Dictionary<GameEntityGUID, Value> dictionary)
		{
			writer.WriteStartElement(name);
			writer.WriteAttributeString<int>("Count", dictionary.Count);
			foreach (KeyValuePair<GameEntityGUID, Value> keyValuePair in dictionary)
			{
				writer.WriteStartElement("KeyValuePair");
				writer.WriteAttributeString<ulong>("Key", keyValuePair.Key);
				writer.WriteAttributeString<Value>("Value", keyValuePair.Value);
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}

		private void WriteDictionnaryWithSerializable<Value>(XmlWriter writer, string name, Dictionary<GameEntityGUID, Value> dictionary) where Value : IXmlSerializable
		{
			writer.WriteStartElement(name);
			writer.WriteAttributeString<int>("Count", dictionary.Count);
			foreach (KeyValuePair<GameEntityGUID, Value> keyValuePair in dictionary)
			{
				writer.WriteStartElement("KeyValuePair");
				writer.WriteAttributeString<ulong>("Key", keyValuePair.Key);
				IXmlSerializable xmlSerializable = keyValuePair.Value;
				writer.WriteElementSerializable<IXmlSerializable>("Value", ref xmlSerializable);
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}

		private void ReadDictionnary<Value>(XmlReader reader, string name, Dictionary<GameEntityGUID, Value> dictionary)
		{
			if (reader.IsStartElement(name))
			{
				int attribute = reader.GetAttribute<int>("Count");
				if (attribute > 0)
				{
					reader.ReadStartElement(name);
					for (int i = 0; i < attribute; i++)
					{
						GameEntityGUID key = reader.GetAttribute<ulong>("Key");
						Value attribute2 = reader.GetAttribute<Value>("Value");
						reader.Skip();
						dictionary.Add(key, attribute2);
					}
					reader.ReadEndElement(name);
				}
				else
				{
					reader.Skip();
				}
			}
		}

		private void ReadDictionaryWithSerializable<Value>(XmlReader reader, string name, Dictionary<GameEntityGUID, Value> dictionary) where Value : class, IXmlSerializable
		{
			if (reader.IsStartElement(name))
			{
				int attribute = reader.GetAttribute<int>("Count");
				if (attribute > 0)
				{
					reader.ReadStartElement(name);
					for (int i = 0; i < attribute; i++)
					{
						GameEntityGUID key = reader.GetAttribute<ulong>("Key");
						reader.ReadStartElement("KeyValuePair");
						Value value = Activator.CreateInstance(typeof(Value)) as Value;
						reader.ReadElementSerializable<Value>("Value", ref value);
						reader.ReadEndElement("KeyValuePair");
						dictionary.Add(key, value);
					}
					reader.ReadEndElement(name);
				}
				else
				{
					reader.Skip();
				}
			}
		}

		public Dictionary<GameEntityGUID, AILayer_Attitude.Attitude.CityModifiersInfo> CityModifiersInfoByCityGuid = new Dictionary<GameEntityGUID, AILayer_Attitude.Attitude.CityModifiersInfo>();

		public int[] CommonAlliesModifierIds;

		public int[] CommonEnemiesModifierIds;

		public int[] CommonFriendsModifierIds;

		public List<AILayer_Attitude.Attitude.EmpireFrontier> Frontiers = new List<AILayer_Attitude.Attitude.EmpireFrontier>();

		public Dictionary<GameEntityGUID, int> PillageModifierByPointOfInterestGuid = new Dictionary<GameEntityGUID, int>();

		public Dictionary<GameEntityGUID, int> FortressModifiersInfoByFortressGuid = new Dictionary<GameEntityGUID, int>();

		public Dictionary<GameEntityGUID, int> FacilityModifiersByFacilityGuid = new Dictionary<GameEntityGUID, int>();

		public Dictionary<GameEntityGUID, AILayer_Attitude.Attitude.UnitModifiersInfo> UnitModifiersInfoByUnitGuid = new Dictionary<GameEntityGUID, AILayer_Attitude.Attitude.UnitModifiersInfo>();

		public Dictionary<int, AILayer_Attitude.Attitude.NavalRegionModifiersInfo> OceanRegionModifiersInfoByRegionIndex = new Dictionary<int, AILayer_Attitude.Attitude.NavalRegionModifiersInfo>();

		public int WarFatigueModifierId;

		private IPersonalityAIHelper personalityHelper;

		public static class Category
		{
			public static StaticString Fear = "Fear";

			public static StaticString Trust = "Trust";

			public static StaticString Envy = "Envy";

			public static StaticString War = "War";
		}

		public class CityModifiersInfo : IXmlSerializable
		{
			public CityModifiersInfo()
			{
				this.CityBesiegedModifierId = -1;
				this.CityNavalBesiegedModifierId = -1;
				this.CityTakenModifierId = -1;
			}

			public void ReadXml(XmlReader reader)
			{
				this.CityBesiegedModifierId = reader.GetAttribute<int>("CityBesiegedModifierId");
				this.CityNavalBesiegedModifierId = reader.GetAttribute<int>("CityNavalBesiegedModifierId");
				this.CityTakenModifierId = (int)reader.GetAttribute<short>("CityTakenModifierId");
				reader.ReadStartElement();
			}

			public void WriteXml(XmlWriter writer)
			{
				writer.WriteAttributeString<int>("CityBesiegedModifierId", this.CityBesiegedModifierId);
				writer.WriteAttributeString<int>("CityNavalBesiegedModifierId", this.CityNavalBesiegedModifierId);
				writer.WriteAttributeString<int>("CityTakenModifierId", this.CityTakenModifierId);
			}

			public int CityBesiegedModifierId;

			public int CityNavalBesiegedModifierId;

			public int CityTakenModifierId;
		}

		public class EmpireFrontier : IXmlSerializable
		{
			public EmpireFrontier(short empireRegionIndex, short neighbourRegionIndex)
			{
				this.EmpireRegionIndex = empireRegionIndex;
				this.NeighbourRegionIndex = neighbourRegionIndex;
				this.AttitudeModifierIndex = -1;
			}

			public void ReadXml(XmlReader reader)
			{
				this.AttitudeModifierIndex = reader.GetAttribute<int>("AttitudeModifierIndex");
				this.EmpireRegionIndex = reader.GetAttribute<short>("EmpireRegionIndex");
				this.NeighbourRegionIndex = reader.GetAttribute<short>("NeighbourRegionIndex");
				reader.ReadStartElement();
			}

			public void WriteXml(XmlWriter writer)
			{
				writer.WriteAttributeString<int>("AttitudeModifierIndex", this.AttitudeModifierIndex);
				writer.WriteAttributeString<short>("EmpireRegionIndex", this.EmpireRegionIndex);
				writer.WriteAttributeString<short>("NeighbourRegionIndex", this.NeighbourRegionIndex);
			}

			public int AttitudeModifierIndex;

			public short EmpireRegionIndex;

			public short NeighbourRegionIndex;
		}

		public class UnitModifiersInfo : IXmlSerializable
		{
			public UnitModifiersInfo()
			{
				this.UnitsInMyTerritoryModifierId = -1;
			}

			public void ReadXml(XmlReader reader)
			{
				this.UnitsInMyTerritoryModifierId = reader.GetAttribute<int>("UnitsInMyTerritoryModifierId");
				reader.ReadStartElement();
			}

			public void WriteXml(XmlWriter writer)
			{
				writer.WriteAttributeString<int>("UnitsInMyTerritoryModifierId", this.UnitsInMyTerritoryModifierId);
			}

			public int UnitsInMyTerritoryModifierId;
		}

		public class NavalRegionModifiersInfo : IXmlSerializable
		{
			public NavalRegionModifiersInfo()
			{
				this.RegionLostModifierId = -1;
				this.BothOwnFortressModifierId = -1;
			}

			public void ReadXml(XmlReader reader)
			{
				this.RegionLostModifierId = reader.GetAttribute<int>("RegionLostModifierId");
				this.BothOwnFortressModifierId = reader.GetAttribute<int>("BothOwnFortressModifierId");
				reader.ReadStartElement();
			}

			public void WriteXml(XmlWriter writer)
			{
				writer.WriteAttributeString<int>("RegionLostModifierId", this.RegionLostModifierId);
				writer.WriteAttributeString<int>("BothOwnFortressModifierId", this.BothOwnFortressModifierId);
			}

			public int RegionLostModifierId;

			public int BothOwnFortressModifierId;
		}
	}

	public static class AttitudeScoreDefinitionReferences
	{
		public static StaticString AggressiveColonization = "AttitudeScoreAggressiveColonization";

		public static StaticString ArmyAggressionInNeutralRegionDuringColdWar = "AttitudeScoreArmyAggressionInNeutralRegionDuringColdWar";

		public static StaticString AtWarWithMyAlly = "AttitudeScoreAtWarWithMyAlly";

		public static StaticString AlliedWithMyEnemy = "AttitudeScoreAlliedWithMyEnemy";

		public static StaticString CommonAllyModifier = "AttitudeScoreCommonAlly";

		public static StaticString CommonEnemyModifier = "AttitudeScoreCommonEnemy";

		public static StaticString CommonFriendModifier = "AttitudeScoreCommonFriend";

		public static StaticString CreepingNodeUpgradeComplete = "AttitudeScoreCreepingNodeUpgradeComplete";

		public static StaticString CityTakenFromAlly = "AttitudeScoreCityTakenFromAlly";

		public static StaticString CityTakenFromEnemy = "AttitudeScoreCityTakenFromEnemy";

		public static StaticString CityTakenFromFriend = "AttitudeScoreCityTakenFromFriend";

		public static StaticString CommonFrontier = "AttitudeScoreCommonFrontier";

		public static StaticString BetrayedYourAlly = "AttitudeScoreBetrayedYourAlly";

		public static StaticString BetrayedYourFriend = "AttitudeScoreBetrayedYourFriend";

		public static StaticString BlackSpot = "AttitudeScoreBlackSpot";

		public static StaticString DismantleCreepingNodeSuffered = "AttitudeScoreDismantleCreepingNodeSuffered";

		public static StaticString DismantleDeviceSuffered = "AttitudeScoreDismantleDeviceSuffered";

		public static StaticString EliminatedAcquaintance = "AttitudeScoreEliminatedAcquaintance";

		public static StaticString EliminatedAlly = "AttitudeScoreEliminatedAlly";

		public static StaticString EliminatedEnemy = "AttitudeScoreEliminatedEnemy";

		public static StaticString EliminatedFriend = "AttitudeScoreEliminatedFriend";

		public static StaticString ForcedStatus = "AttitudeScoreForcedStatus";

		public static StaticString FortressesOwnedInTheSameRegion = "AttitudeScoreFortressesOwnedInTheSameRegion";

		public static StaticString OldFortressesOwnedInTheSameRegion = "AttitudeScoreFortressInTheSameRegion";

		public static StaticString Gift = "AttitudeScoreGift";

		public static StaticString KaijuAttacked = "AttitudeScoreKaijuAttacked";

		public static StaticString KaijuPlacedNearMyCityNegative = "AttitudeScoreKaijuPlacedNearMyCityNegative";

		public static StaticString KaijuPlacedNearMyCityPositive = "AttitudeScoreKaijuPlacedNearMyCityPositive";

		public static StaticString LastWar = "AttitudeScoreLastWar";

		public static StaticString LongTermAlliance = "AttitudeScoreLongTermAlliance";

		public static StaticString LongTermPeace = "AttitudeScoreLongTermPeace";

		public static StaticString LivingSpaceTense = "AttitudeScoreLivingSpaceTense";

		public static StaticString MantaAspiratingInMyTerritory = "AttitudeScoreMantaAspiratingInMyTerritory";

		public static StaticString MyEmpireCitiesTakenDuringWar = "AttitudeScoreMyEmpireCitiesTakenDuringWar";

		public static StaticString MyEmpireCitiesBombardedDuringWar = "AttitudeScoreMyEmpireCitiesBombardedDuringWar";

		public static StaticString MyEmpireCityBesiegedDuringWar = "AttitudeScoreMyEmpireCitiesBesiegedDuringWar";

		public static StaticString MyEmpireFortressesTakenDuringColdWar = "AttitudeScoreMyEmpireFortressesTakenDuringColdWar";

		public static StaticString MyEmpireFortressesTakenDuringWar = "AttitudeScoreMyEmpireFortressesTakenDuringWar";

		public static StaticString MyEmpireLeadMilitaryPower = "AttitudeScoreMyEmpireLeadMilitaryPower";

		public static StaticString MyEmpireLeadNavalMilitaryPower = "AttitudeScoreMyEmpireLeadNavalMilitaryPower";

		public static StaticString MyEmpireLeadNavalRegionCount = "AttitudeScoreMyEmpireLeadNavalRegionCount";

		public static StaticString MyEmpireLeadRegionCount = "AttitudeScoreMyEmpireLeadRegionCount";

		public static StaticString MyEmpireLeadScore = "AttitudeScoreMyEmpireLeadScore";

		public static StaticString MyEmpireLostNavalRegionControlDuringWar = "AttitudeScoreMyEmpireLostNavalRegionControlDuringWar";

		public static StaticString MyEmpireUnitsKilledDuringWar = "AttitudeScoreMyEmpireUnitsKilledDuringWar";

		public static StaticString NegativeContract = "AttitudeScoreNegativeContract";

		public static StaticString OrbsStolen = "AttitudeScoreOrbsStolen";

		public static StaticString OtherEmpireCitiesTakenDuringWar = "AttitudeScoreOtherEmpireCitiesTakenDuringWar";

		public static StaticString OtherEmpireCitiesBombardedDuringWar = "AttitudeScoreOtherEmpireCitiesBombardedDuringWar";

		public static StaticString OtherEmpireCityBesiegedDuringWar = "AttitudeScoreOtherEmpireCitiesBesiegedDuringWar";

		public static StaticString OtherEmpireFortressesTakenDuringWar = "AttitudeScoreOtherEmpireFortressesTakenDuringWar";

		public static StaticString OtherEmpireLeadMilitaryPower = "AttitudeScoreOtherEmpireLeadMilitaryPower";

		public static StaticString OtherEmpireLeadNavalMilitaryPower = "AttitudeScoreOtherEmpireLeadNavalMilitaryPower";

		public static StaticString OtherEmpireLeadNavalRegionCount = "AttitudeScoreOtherEmpireLeadNavalRegionCount";

		public static StaticString OtherEmpireLeadRegionCount = "AttitudeScoreOtherEmpireLeadRegionCount";

		public static StaticString OtherEmpireLeadScore = "AttitudeScoreOtherEmpireLeadScore";

		public static StaticString OtherEmpireLostNavalRegionControlDuringWar = "AttitudeScoreOtherEmpireLostNavalRegionControlDuringWar";

		public static StaticString OtherEmpireUnitsKilledDuringWar = "AttitudeScoreOtherEmpireUnitsKilledDuringWar";

		public static StaticString OtherEmpireHasUniqueFacilityDefinition = "AttitudeScoreOtherEmpireHasUniqueFacility";

		public static StaticString Peaceful = "AttitudeScorePeaceful";

		public static StaticString PillageSucceed = "AttitudeScorePillageSucceed";

		public static StaticString Pillaging = "AttitudeScorePillaging";

		public static StaticString PositiveContract = "AttitudeScorePositiveContract";

		public static StaticString RegionalBuildingDestroyed = "AttitudeScoreRegionalBuildingDestroyed";

		public static StaticString Spy = "AttitudeScoreSpy";

		public static StaticString TerraformedInMyTerritoryPositive = "AttitudeScoreTerraformedInMyTerritoryPositive";

		public static StaticString TerraformedInMyTerritoryNegative = "AttitudeScoreTerraformedInMyTerritoryNegative";

		public static StaticString TerraformedNearMyArmiesPositive = "AttitudeScoreTerraformedNearMyArmiesPositive";

		public static StaticString TerraformedNearMyArmiesNegative = "AttitudeScoreTerraformedNearMyArmiesNegative";

		public static StaticString UnitsInMyTerritory = "AttitudeScoreUnitsInMyTerritory";

		public static StaticString VillageConverted = "AttitudeScoreVillageConverted";

		public static StaticString VictoryAlertGlobalScoreWhenLastTurnReached = "AttitudeScoreVictoryAlertGlobalScoreWhenLastTurnReached";

		public static StaticString VictoryAlertLastEmpireStanding = "AttitudeScoreVictoryAlertLastEmpireStanding";

		public static StaticString VictoryAlertExpansion = "AttitudeScoreVictoryAlertExpansion";

		public static StaticString VictoryAlertEconomy = "AttitudeScoreVictoryAlertEconomy";

		public static StaticString VictoryAlertDiplomacy = "AttitudeScoreVictoryAlertDiplomacy";

		public static StaticString VictoryAlertWonder = "AttitudeScoreVictoryAlertWonder";

		public static StaticString VictoryAlertMostTechnologiesDiscovered = "AttitudeScoreVictoryAlertMostTechnologiesDiscovered";

		public static StaticString VictoryAlertSupremacy = "AttitudeScoreVictoryAlertSupremacy";

		public static StaticString VictoryAlertQuest = "AttitudeScoreVictoryAlertQuest";

		public static StaticString VictoryAlertShared = "AttitudeScoreVictoryAlertShared";

		public static StaticString WarFatigue = "AttitudeScoreWarFatigue";

		public static StaticString[] All = new StaticString[]
		{
			AILayer_Attitude.AttitudeScoreDefinitionReferences.AggressiveColonization,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.ArmyAggressionInNeutralRegionDuringColdWar,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.AtWarWithMyAlly,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.AlliedWithMyEnemy,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.CommonAllyModifier,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.CommonEnemyModifier,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.CommonFriendModifier,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.CreepingNodeUpgradeComplete,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.CityTakenFromAlly,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.CityTakenFromEnemy,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.CityTakenFromFriend,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.CommonFrontier,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.BetrayedYourAlly,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.BetrayedYourFriend,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.BlackSpot,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.DismantleCreepingNodeSuffered,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.DismantleDeviceSuffered,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.EliminatedAcquaintance,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.EliminatedAlly,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.EliminatedEnemy,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.EliminatedFriend,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.ForcedStatus,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.FortressesOwnedInTheSameRegion,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.Gift,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.KaijuAttacked,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.KaijuPlacedNearMyCityNegative,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.KaijuPlacedNearMyCityPositive,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.LastWar,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.LongTermAlliance,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.LongTermPeace,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.LivingSpaceTense,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.MantaAspiratingInMyTerritory,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireCitiesTakenDuringWar,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireCityBesiegedDuringWar,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireCitiesBombardedDuringWar,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireFortressesTakenDuringWar,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireFortressesTakenDuringColdWar,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireLeadMilitaryPower,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireLeadNavalMilitaryPower,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireLeadNavalRegionCount,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireLeadRegionCount,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireLeadScore,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireLostNavalRegionControlDuringWar,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.MyEmpireUnitsKilledDuringWar,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.NegativeContract,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.OrbsStolen,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireCitiesTakenDuringWar,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireCitiesBombardedDuringWar,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireCityBesiegedDuringWar,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireFortressesTakenDuringWar,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireLeadMilitaryPower,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireLeadNavalMilitaryPower,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireLeadNavalRegionCount,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireLeadRegionCount,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireLeadScore,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireLostNavalRegionControlDuringWar,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireUnitsKilledDuringWar,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.OtherEmpireHasUniqueFacilityDefinition,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.Peaceful,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.PillageSucceed,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.Pillaging,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.PositiveContract,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.RegionalBuildingDestroyed,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.Spy,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.TerraformedInMyTerritoryPositive,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.TerraformedInMyTerritoryNegative,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.TerraformedNearMyArmiesPositive,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.TerraformedNearMyArmiesNegative,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.UnitsInMyTerritory,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.VillageConverted,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.VictoryAlertGlobalScoreWhenLastTurnReached,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.VictoryAlertLastEmpireStanding,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.VictoryAlertExpansion,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.VictoryAlertEconomy,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.VictoryAlertDiplomacy,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.VictoryAlertWonder,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.VictoryAlertMostTechnologiesDiscovered,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.VictoryAlertSupremacy,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.VictoryAlertQuest,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.VictoryAlertShared,
			AILayer_Attitude.AttitudeScoreDefinitionReferences.WarFatigue
		};
	}

	private class ComparativeModifierRule
	{
		public DiplomaticRelationScoreModifierDefinition AttitudeScoreMyEmpireLead;

		public DiplomaticRelationScoreModifierDefinition AttitudeScoreOtherEmpireLead;

		public Func<MajorEmpire, float> GetScoreAccessor;

		public float NeutralIntervalPercent;

		public Prerequisite Prerequisite;

		public float MinimumMultiplierValue;

		public float MaximumMultiplierValue;
	}
}
