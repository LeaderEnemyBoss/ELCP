using System;
using System.Collections.Generic;
using Amplitude;
using UnityEngine;

public class AIEmpireData
{
	public AIEmpireData(Empire empire)
	{
		this.Empire = empire;
	}

	public float LandMilitaryPower { get; set; }

	public float NavalMilitaryPower { get; set; }

	public float AverageUnitDesignMaximumMovement { get; set; }

	public float AverageUnitDesignProductionCost { get; set; }

	public float AverageUnitDesignMilitaryPower { get; set; }

	public Dictionary<StaticString, int> CountPerUnitType
	{
		get
		{
			return this.countPerUnitType;
		}
	}

	public Empire Empire { get; private set; }

	public int MilitaryStandardUnitCount
	{
		get
		{
			return this.LandMilitaryStandardUnitCount + this.NavalMilitaryStandardUnitCount;
		}
	}

	public int LandMilitaryStandardUnitCount { get; private set; }

	public int NavalMilitaryStandardUnitCount { get; private set; }

	public int StandardUnitCount { get; private set; }

	public bool HasShips { get; set; }

	public void Initialize()
	{
		this.departmentOfDefense = this.Empire.GetAgency<DepartmentOfDefense>();
		this.departmentOfTheInterior = this.Empire.GetAgency<DepartmentOfTheInterior>();
		this.barbarianCouncil = this.Empire.GetAgency<BarbarianCouncil>();
		this.departmentOfTreasury = this.Empire.GetAgency<DepartmentOfTheTreasury>();
		this.pirateCouncil = this.Empire.GetAgency<PirateCouncil>();
	}

	public void Update()
	{
		if (!this.HasShips)
		{
			this.HasShips = (this.departmentOfDefense.TechnologyDefinitionShipState == DepartmentOfScience.ConstructibleElement.State.Researched);
		}
		this.StandardUnitCount = 0;
		this.LandMilitaryStandardUnitCount = 0;
		this.NavalMilitaryStandardUnitCount = 0;
		this.NavalMilitaryPower = 0f;
		this.LandMilitaryPower = 0f;
		this.countPerUnitType.Clear();
		for (int i = 0; i < this.departmentOfDefense.Armies.Count; i++)
		{
			this.CountUnitInGarrison(this.departmentOfDefense.Armies[i]);
		}
		if (this.departmentOfTheInterior != null)
		{
			for (int j = 0; j < this.departmentOfTheInterior.Cities.Count; j++)
			{
				this.CountUnitInGarrison(this.departmentOfTheInterior.Cities[j]);
				if (this.departmentOfTheInterior.Cities[j].Camp != null)
				{
					this.CountUnitInGarrison(this.departmentOfTheInterior.Cities[j].Camp);
				}
			}
			for (int k = 0; k < this.departmentOfTheInterior.OccupiedFortresses.Count; k++)
			{
				this.CountUnitInGarrison(this.departmentOfTheInterior.OccupiedFortresses[k]);
			}
		}
		if (this.Empire is MajorEmpire)
		{
			MajorEmpire majorEmpire = this.Empire as MajorEmpire;
			for (int l = 0; l < majorEmpire.ConvertedVillages.Count; l++)
			{
				this.CountUnitInGarrison(majorEmpire.ConvertedVillages[l]);
			}
		}
		if (this.barbarianCouncil != null)
		{
			for (int m = 0; m < this.barbarianCouncil.Villages.Count; m++)
			{
				if (!this.barbarianCouncil.Villages[m].HasBeenConverted && !this.barbarianCouncil.Villages[m].HasBeenPacified)
				{
					this.CountUnitInGarrison(this.barbarianCouncil.Villages[m]);
				}
			}
		}
		if (this.pirateCouncil != null)
		{
			for (int n = 0; n < this.pirateCouncil.Fortresses.Count; n++)
			{
				if (!this.pirateCouncil.Fortresses[n].IsOccupied)
				{
					this.CountUnitInGarrison(this.pirateCouncil.Fortresses[n]);
				}
			}
		}
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		foreach (UnitDesign unitDesign in this.departmentOfDefense.UnitDesignDatabase.UserDefinedUnitDesigns)
		{
			if (unitDesign.Context != null && !unitDesign.Context.SimulationObject.Tags.Contains(UnitAbility.ReadonlyColonize) && !unitDesign.Context.SimulationObject.Tags.Contains(UnitAbility.ReadonlyResettle))
			{
				num2 += unitDesign.Context.GetPropertyValue(SimulationProperties.MilitaryPower);
				num3 += unitDesign.Context.GetPropertyValue(SimulationProperties.MaximumMovement);
				for (int num5 = 0; num5 < unitDesign.Costs.Length; num5++)
				{
					if (unitDesign.Costs[num5].ResourceName == DepartmentOfTheTreasury.Resources.Production)
					{
						num4 += unitDesign.Costs[num5].GetValue(this.Empire);
					}
				}
				num += 1f;
			}
		}
		this.AverageUnitDesignMilitaryPower = num2 / num;
		this.AverageUnitDesignMaximumMovement = num3 / num;
		this.AverageUnitDesignProductionCost = num4 / num;
	}

	public float ComputeResourceAvailabilityForUnit(StaticString resourceName)
	{
		Diagnostics.Assert(this.departmentOfTreasury != null);
		float num = 0f;
		if (!this.departmentOfTreasury.TryGetResourceStockValue(this.Empire.SimulationObject, resourceName, out num, false))
		{
			Diagnostics.LogWarning("{0} is not available in Empire {1}", new object[]
			{
				resourceName,
				this.Empire.Name
			});
		}
		float num2 = 0f;
		if (!this.departmentOfTreasury.TryGetNetResourceValue(this.Empire.SimulationObject, resourceName, out num2, false))
		{
			num2 = 0f;
		}
		float propertyValue = this.Empire.GetPropertyValue(SimulationProperties.ArmyUnitSlot);
		float propertyValue2 = this.Empire.GetPropertyValue(SimulationProperties.NetCityProduction);
		float num3 = propertyValue2 / this.AverageUnitDesignProductionCost;
		float num4 = num2 / (num3 * (float)this.departmentOfTheInterior.Cities.Count);
		float num5 = Mathf.Min(num / propertyValue, propertyValue * num);
		return num4 + num5;
	}

	private void CountUnitInGarrison(Garrison garrison)
	{
		this.StandardUnitCount += garrison.StandardUnits.Count;
		for (int i = 0; i < garrison.StandardUnits.Count; i++)
		{
			if (!garrison.StandardUnits[i].SimulationObject.Tags.Contains(UnitAbility.ReadonlyColonize) && !garrison.StandardUnits[i].SimulationObject.Tags.Contains(UnitAbility.ReadonlyResettle))
			{
				if (garrison.StandardUnits[i].SimulationObject.Tags.Contains(DownloadableContent16.SeafaringUnit))
				{
					this.NavalMilitaryStandardUnitCount++;
					this.NavalMilitaryPower = garrison.StandardUnits[i].GetPropertyValue(SimulationProperties.MilitaryPower);
				}
				else
				{
					this.LandMilitaryStandardUnitCount++;
					this.LandMilitaryPower = garrison.StandardUnits[i].GetPropertyValue(SimulationProperties.MilitaryPower);
				}
				StaticString descriptorNameFromType = garrison.StandardUnits[i].SimulationObject.GetDescriptorNameFromType("UnitClass");
				if (!StaticString.IsNullOrEmpty(descriptorNameFromType))
				{
					if (this.countPerUnitType.ContainsKey(descriptorNameFromType))
					{
						Dictionary<StaticString, int> dictionary2;
						Dictionary<StaticString, int> dictionary = dictionary2 = this.countPerUnitType;
						StaticString key2;
						StaticString key = key2 = descriptorNameFromType;
						int num = dictionary2[key2];
						dictionary[key] = num + 1;
					}
					else
					{
						this.countPerUnitType.Add(descriptorNameFromType, 1);
					}
				}
			}
		}
	}

	private BarbarianCouncil barbarianCouncil;

	private Dictionary<StaticString, int> countPerUnitType = new Dictionary<StaticString, int>();

	private DepartmentOfDefense departmentOfDefense;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private DepartmentOfTheTreasury departmentOfTreasury;

	private PirateCouncil pirateCouncil;
}
