using System;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class AssignationData_Spy : AssignationData
{
	public AssignationData_Spy(global::Empire spyEmpire, City cityToInfiltrate) : base(cityToInfiltrate)
	{
		this.spyEmpire = spyEmpire;
		this.cityToInfiltrate = cityToInfiltrate;
		IGameService service = Services.GetService<IGameService>();
		this.visibilityService = service.Game.Services.GetService<IVisibilityService>();
	}

	public override bool CheckValidity(global::Empire owner)
	{
		return this.gameEntityRepositoryService.Contains(base.Garrison.GUID) && base.Garrison.Empire != owner;
	}

	public override void ComputeSpecialtyNeed()
	{
		base.ComputeSpecialtyNeed();
		float num = 0f;
		DepartmentOfForeignAffairs agency = this.spyEmpire.GetAgency<DepartmentOfForeignAffairs>();
		if (agency.IsEnnemy(this.cityToInfiltrate.Empire))
		{
			num = AILayer.Boost(num, 0.5f);
		}
		if (this.spyEmpire.SimulationObject.Tags.Contains("FactionTraitReplicants4"))
		{
			DepartmentOfScience agency2 = this.spyEmpire.GetAgency<DepartmentOfScience>();
			DepartmentOfScience agency3 = this.cityToInfiltrate.Empire.GetAgency<DepartmentOfScience>();
			if (agency2.GetTechnologyUnlockedCount() < agency3.GetTechnologyUnlockedCount())
			{
				num = AILayer.Boost(num, 0.2f);
			}
		}
		for (int i = 0; i < this.cityToInfiltrate.Districts.Count; i++)
		{
			if (this.visibilityService.IsWorldPositionVisibleFor(this.cityToInfiltrate.Districts[i].WorldPosition, this.spyEmpire))
			{
				num = AILayer.Boost(num, 0.2f);
				break;
			}
		}
		float num2 = 0f;
		DepartmentOfTheInterior agency4 = this.cityToInfiltrate.Empire.GetAgency<DepartmentOfTheInterior>();
		float propertyValue;
		for (int j = 0; j < agency4.Cities.Count; j++)
		{
			propertyValue = agency4.Cities[j].GetPropertyValue(SimulationProperties.Population);
			if (num2 < propertyValue)
			{
				num2 = propertyValue;
			}
		}
		propertyValue = this.cityToInfiltrate.GetPropertyValue(SimulationProperties.Population);
		num = AILayer.Boost(num, propertyValue / num2 * 0.2f);
		float f = this.cityToInfiltrate.GetPropertyValue(SimulationProperties.NetCityAntiSpy) / 100f;
		num = AILayer.Boost(num, 0.3f - Mathf.Sqrt(f));
		int num3 = (int)this.cityToInfiltrate.GetPropertyValue(SimulationProperties.RoundUpProgress);
		int num4 = (int)this.cityToInfiltrate.GetPropertyValue(SimulationProperties.RoundUpTurnToActivate);
		if (num3 + 1 >= num4)
		{
			num = 0f;
		}
		else if (num3 >= 0)
		{
			float num5 = (float)num3 / (float)num4;
			num = AILayer.Boost(num, -num5 * 0.8f);
		}
		base.GarrisonSpecialtyNeed[4] = num;
	}

	private City cityToInfiltrate;

	private global::Empire spyEmpire;

	private IVisibilityService visibilityService;
}
