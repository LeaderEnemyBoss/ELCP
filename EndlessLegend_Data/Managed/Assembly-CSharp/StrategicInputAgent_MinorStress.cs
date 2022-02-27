using System;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class StrategicInputAgent_MinorStress : StrategicInputAgent
{
	public override void ComputeInitialValue()
	{
		base.ComputeInitialValue();
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		float num5 = 0f;
		int turn = this.endTurnService.Turn;
		if (turn < this.turnBeforeMinorRoaming)
		{
			base.InitialValue = 0f;
			return;
		}
		for (int i = 0; i < this.deparmentOfTheInterior.Cities.Count; i++)
		{
			Region region = this.deparmentOfTheInterior.Cities[i].Region;
			this.UpdateEmpireInfo(region.MinorEmpire, ref num3, ref num2, ref num5, ref num4, ref num);
			for (int j = 0; j < region.Borders.Length; j++)
			{
				Region region2 = this.worldPositioningService.GetRegion(region.Borders[j].NeighbourRegionIndex);
				if (!region2.IsOcean && !region2.IsWasteland && region2.City == null)
				{
					this.UpdateEmpireInfo(region2.MinorEmpire, ref num3, ref num2, ref num5, ref num4, ref num);
				}
			}
		}
		float initialValue = 0f;
		if (num > 0f)
		{
			float num6 = 0f;
			float num7 = 0f;
			float num8 = 0f;
			float num9 = 0f;
			float num10 = 0f;
			this.UpdateEmpireInfo(this.empire, ref num8, ref num7, ref num10, ref num9, ref num6);
			float num11 = num - num6;
			float num12 = num11 / num;
			num12 = Mathf.Min(1f, Mathf.Max(-1f, num12));
			initialValue = 0.5f * (1f + num12);
		}
		base.InitialValue = initialValue;
	}

	public override void Initialize(StrategicNetwork network, StrategicAgentDefinition agentDefinition)
	{
		base.Initialize(network, agentDefinition);
		this.empire = this.network.Entity.Empire;
		if (this.empire == null)
		{
			Diagnostics.LogError("Can't retrieve the empire.");
			return;
		}
		this.deparmentOfTheInterior = this.empire.GetAgency<DepartmentOfTheInterior>();
		IGameService service = Services.GetService<IGameService>();
		this.worldPositioningService = service.Game.Services.GetService<IWorldPositionningService>();
		this.endTurnService = Services.GetService<IEndTurnService>();
	}

	private void UpdateEmpireInfo(global::Empire empire, ref float numberOfVillage, ref float villageUnitCountAverage, ref float numberOfArmies, ref float armyUnitCountAverage, ref float maxMilitaryPower)
	{
		if (empire == null)
		{
			return;
		}
		DepartmentOfDefense agency = empire.GetAgency<DepartmentOfDefense>();
		if (agency.Armies.Count > 0)
		{
			numberOfArmies += (float)agency.Armies.Count;
			for (int i = 0; i < agency.Armies.Count; i++)
			{
				armyUnitCountAverage += (float)agency.Armies[i].UnitsCount;
				float propertyValue = agency.Armies[i].GetPropertyValue(SimulationProperties.MilitaryPower);
				if (maxMilitaryPower < propertyValue)
				{
					maxMilitaryPower = propertyValue;
				}
			}
		}
		BarbarianCouncil agency2 = empire.GetAgency<BarbarianCouncil>();
		if (agency2 != null)
		{
			for (int j = 0; j < agency2.Villages.Count; j++)
			{
				if (!agency2.Villages[j].HasBeenPacified || (agency2.Villages[j].HasBeenConverted && agency2.Villages[j].Converter != this.empire))
				{
					villageUnitCountAverage += (float)agency2.Villages[j].UnitsCount;
					numberOfVillage += 1f;
					float propertyValue2 = agency2.Villages[j].GetPropertyValue(SimulationProperties.MilitaryPower);
					if (maxMilitaryPower < propertyValue2)
					{
						maxMilitaryPower = propertyValue2;
					}
				}
			}
		}
	}

	private DepartmentOfTheInterior deparmentOfTheInterior;

	private global::Empire empire;

	private IEndTurnService endTurnService;

	private int turnBeforeMinorRoaming = 10;

	private IWorldPositionningService worldPositioningService;
}
