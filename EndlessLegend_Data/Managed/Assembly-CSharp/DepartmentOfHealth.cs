using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation;

public class DepartmentOfHealth : Agency
{
	public DepartmentOfHealth(global::Empire empire) : base(empire)
	{
	}

	public ApprovalStatus GetApprovalStatusFor(SimulationObjectWrapper simulationObjectWrapper)
	{
		StaticString descriptorNameFromType = simulationObjectWrapper.GetDescriptorNameFromType("Class");
		float num = 0f;
		List<ApprovalStatus> list;
		if (!this.approvalStatusByClass.TryGetValue(descriptorNameFromType, out list))
		{
			return null;
		}
		Diagnostics.Assert(list != null);
		if (simulationObjectWrapper.HasProperty(SimulationProperties.NetCityApproval))
		{
			num = simulationObjectWrapper.GetPropertyValue(SimulationProperties.NetCityApproval);
		}
		else if (simulationObjectWrapper.HasProperty(SimulationProperties.NetEmpireApproval))
		{
			num = simulationObjectWrapper.GetPropertyValue(SimulationProperties.NetEmpireApproval);
		}
		else
		{
			Diagnostics.LogError("Can't found the approval value for simulation object wrapper {0}", new object[]
			{
				simulationObjectWrapper.Name
			});
		}
		ApprovalStatus approvalStatus = null;
		for (int i = 0; i < list.Count; i++)
		{
			ApprovalStatus approvalStatus2 = list[i];
			Diagnostics.Assert(approvalStatus2 != null);
			if (num >= (float)approvalStatus2.MinimalApproval && num <= (float)approvalStatus2.MaximalApproval)
			{
				approvalStatus = approvalStatus2;
			}
		}
		if (approvalStatus == null)
		{
			Diagnostics.LogWarning("No approval status found for approval value {0}.", new object[]
			{
				num
			});
		}
		return approvalStatus;
	}

	public void RefreshApprovalStatus()
	{
		Diagnostics.Assert(this.departmentOfTheInterior != null);
		ReadOnlyCollection<City> cities = this.departmentOfTheInterior.Cities;
		base.Empire.Refresh(true);
		ApprovalStatus approvalStatusFor;
		ApprovalStatus approvalStatus;
		for (int i = 0; i < cities.Count; i++)
		{
			approvalStatusFor = this.GetApprovalStatusFor(cities[i]);
			if (this.approvalStatusByGameEntity.ContainsKey(cities[i].GUID))
			{
				approvalStatus = this.approvalStatusByGameEntity[cities[i].GUID];
			}
			else
			{
				approvalStatus = null;
				this.approvalStatusByGameEntity.Add(cities[i].GUID, approvalStatus);
			}
			this.SwapApprovalStatus(cities[i], approvalStatus, approvalStatusFor);
			this.approvalStatusByGameEntity[cities[i].GUID] = approvalStatusFor;
		}
		base.Empire.Refresh(true);
		approvalStatusFor = this.GetApprovalStatusFor(base.Empire);
		approvalStatus = this.currentEmpireApprovalStatus;
		this.SwapApprovalStatus(base.Empire, approvalStatus, approvalStatusFor);
		this.currentEmpireApprovalStatus = approvalStatusFor;
		base.Empire.Refresh(false);
	}

	protected override IEnumerator OnInitialize()
	{
		yield return base.OnInitialize();
		IDatabase<ApprovalStatus> approvalStatusDatabase = Databases.GetDatabase<ApprovalStatus>(false);
		ApprovalStatus[] approvalStatus = approvalStatusDatabase.GetValues();
		for (int index = 0; index < approvalStatus.Length; index++)
		{
			StaticString targetClass = approvalStatus[index].TargetClass;
			if (!this.approvalStatusByClass.ContainsKey(targetClass))
			{
				this.approvalStatusByClass.Add(targetClass, new List<ApprovalStatus>());
			}
			this.approvalStatusByClass[targetClass].Add(approvalStatus[index]);
		}
		foreach (KeyValuePair<StaticString, List<ApprovalStatus>> kvp in this.approvalStatusByClass)
		{
			this.SortApprovalStatus(kvp.Value);
		}
		base.Empire.RegisterPass("GameClientState_Turn_End", "RefreshApprovalStatus", new Agency.Action(this.GameClientState_Turn_End_RefreshApprovalStatus), new string[0]);
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		Diagnostics.Assert(base.Empire != null);
		this.departmentOfTheInterior = base.Empire.GetAgency<DepartmentOfTheInterior>();
		Diagnostics.Assert(this.departmentOfTheInterior != null);
		this.departmentOfTheInterior.CitiesCollectionChanged += this.DepartmentOfTheInterior_CitiesCollectionChanged;
		this.departmentOfTheInterior.AssimilatedFactionsCollectionChanged += this.DepartmentOfTheInterior_AssimilatedFactionsCollectionChanged;
		this.departmentOfTheInterior.PopulationRepartitionChanged += this.DepartmentOfTheInterior_PopulationRepartitionChanged;
		this.departmentOfPlanificationAndDevelopment = base.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
		Diagnostics.Assert(this.departmentOfPlanificationAndDevelopment != null);
		this.departmentOfPlanificationAndDevelopment.BoosterCollectionChange += this.DepartmentOfPlanificationAndDevelopment_BoosterCollectionChange;
		DepartmentOfIndustry departmentOfIndustry = base.Empire.GetAgency<DepartmentOfIndustry>();
		Diagnostics.Assert(departmentOfIndustry != null);
		departmentOfIndustry.AddConstructionChangeEventHandler<CityImprovementDefinition>(new DepartmentOfIndustry.ConstructionChangeEventHandler(this.DepartmentOfIndustry_ConstructionEventHandler));
		departmentOfIndustry.AddConstructionChangeEventHandler<DistrictImprovementDefinition>(new DepartmentOfIndustry.ConstructionChangeEventHandler(this.DepartmentOfIndustry_ConstructionEventHandler));
		departmentOfIndustry.AddConstructionChangeEventHandler<PointOfInterestImprovementDefinition>(new DepartmentOfIndustry.ConstructionChangeEventHandler(this.DepartmentOfIndustry_ConstructionEventHandler));
		IDatabase<ApprovalStatus> approvalStatusDatabase = Databases.GetDatabase<ApprovalStatus>(false);
		if (this.currentEmpireApprovalStatus == null)
		{
			StaticString approvalStatusName = base.Empire.GetDescriptorNameFromType("ApprovalStatus");
			if (!StaticString.IsNullOrEmpty(approvalStatusName) && !approvalStatusDatabase.TryGetValue(approvalStatusName, out this.currentEmpireApprovalStatus))
			{
				Diagnostics.LogWarning("We have found an approval status descriptor on the empire, but the approval status database does not contains it.");
			}
		}
		ReadOnlyCollection<City> cities = this.departmentOfTheInterior.Cities;
		ApprovalStatus cityStatus = null;
		for (int index = 0; index < cities.Count; index++)
		{
			if (!this.approvalStatusByGameEntity.ContainsKey(cities[index].GUID))
			{
				StaticString approvalStatusName2 = cities[index].GetDescriptorNameFromType("ApprovalStatus");
				if (!StaticString.IsNullOrEmpty(approvalStatusName2))
				{
					if (!approvalStatusDatabase.TryGetValue(approvalStatusName2, out cityStatus))
					{
						Diagnostics.LogWarning("We have found an approval status descriptor on the empire, but the approval status database does not contains it.");
					}
					else
					{
						this.approvalStatusByGameEntity.Add(cities[index].GUID, cityStatus);
					}
				}
			}
		}
		this.RefreshApprovalStatus();
		yield break;
	}

	protected override IEnumerator OnLoadGame(Amplitude.Unity.Game.Game game)
	{
		yield return base.OnLoadGame(game);
		this.RefreshApprovalStatus();
		yield break;
	}

	protected override void OnRelease()
	{
		base.OnRelease();
		DepartmentOfIndustry agency = base.Empire.GetAgency<DepartmentOfIndustry>();
		Diagnostics.Assert(agency != null);
		agency.RemoveConstructionChangeEventHandler<CityImprovementDefinition>(new DepartmentOfIndustry.ConstructionChangeEventHandler(this.DepartmentOfIndustry_ConstructionEventHandler));
		agency.RemoveConstructionChangeEventHandler<DistrictImprovementDefinition>(new DepartmentOfIndustry.ConstructionChangeEventHandler(this.DepartmentOfIndustry_ConstructionEventHandler));
		agency.RemoveConstructionChangeEventHandler<PointOfInterestImprovementDefinition>(new DepartmentOfIndustry.ConstructionChangeEventHandler(this.DepartmentOfIndustry_ConstructionEventHandler));
		if (this.departmentOfPlanificationAndDevelopment != null)
		{
			this.departmentOfPlanificationAndDevelopment.BoosterCollectionChange -= this.DepartmentOfPlanificationAndDevelopment_BoosterCollectionChange;
			this.departmentOfPlanificationAndDevelopment = null;
		}
		if (this.departmentOfTheInterior != null)
		{
			this.departmentOfTheInterior.CitiesCollectionChanged -= this.DepartmentOfTheInterior_CitiesCollectionChanged;
			this.departmentOfTheInterior.AssimilatedFactionsCollectionChanged -= this.DepartmentOfTheInterior_AssimilatedFactionsCollectionChanged;
			this.departmentOfTheInterior.PopulationRepartitionChanged -= this.DepartmentOfTheInterior_PopulationRepartitionChanged;
			this.departmentOfTheInterior = null;
		}
		this.approvalStatusByClass.Clear();
		this.approvalStatusByGameEntity.Clear();
	}

	private void DepartmentOfIndustry_ConstructionEventHandler(object sender, ConstructionChangeEventArgs e)
	{
		this.RefreshApprovalStatus();
	}

	private void DepartmentOfTheInterior_AssimilatedFactionsCollectionChanged(object sender, CollectionChangeEventArgs e)
	{
		this.RefreshApprovalStatus();
	}

	private void DepartmentOfTheInterior_CitiesCollectionChanged(object sender, CollectionChangeEventArgs e)
	{
		CollectionChangeAction action = e.Action;
		if (action == CollectionChangeAction.Remove)
		{
			City city = e.Element as City;
			Diagnostics.Assert(city != null);
			ApprovalStatus oldStatus = null;
			if (this.approvalStatusByGameEntity.ContainsKey(city.GUID))
			{
				oldStatus = this.approvalStatusByGameEntity[city.GUID];
				this.approvalStatusByGameEntity.Remove(city.GUID);
			}
			this.SwapApprovalStatus(city, oldStatus, null);
		}
		this.RefreshApprovalStatus();
	}

	private void DepartmentOfTheInterior_PopulationRepartitionChanged(object sender, PopulationRepartitionEventArgs e)
	{
		this.RefreshApprovalStatus();
	}

	private void DepartmentOfPlanificationAndDevelopment_BoosterCollectionChange(object sender, BoosterCollectionChangeEventArgs e)
	{
		this.RefreshApprovalStatus();
	}

	private IEnumerator GameClientState_Turn_End_RefreshApprovalStatus(string context, string name)
	{
		this.RefreshApprovalStatus();
		yield break;
	}

	private void SwapApprovalStatus(SimulationObjectWrapper simulationObjectWrapper, ApprovalStatus oldStatus, ApprovalStatus newStatus)
	{
		if (newStatus == oldStatus)
		{
			return;
		}
		if (oldStatus != null && oldStatus.SimulationDescriptors != null)
		{
			for (int i = 0; i < oldStatus.SimulationDescriptors.Length; i++)
			{
				simulationObjectWrapper.RemoveDescriptor(oldStatus.SimulationDescriptors[i]);
			}
		}
		if (newStatus != null && newStatus.SimulationDescriptors != null)
		{
			for (int j = 0; j < newStatus.SimulationDescriptors.Length; j++)
			{
				simulationObjectWrapper.AddDescriptor(newStatus.SimulationDescriptors[j], false);
			}
		}
	}

	private void SortApprovalStatus(List<ApprovalStatus> approvalStatus)
	{
		approvalStatus.Sort((ApprovalStatus left, ApprovalStatus right) => left.MinimalApproval.CompareTo(right.MinimalApproval));
		int num = 0;
		for (int i = 0; i < approvalStatus.Count; i++)
		{
			if (approvalStatus[i].MinimalApproval != num)
			{
				approvalStatus[i].MinimalApproval = num;
			}
			if (approvalStatus[i].MaximalApproval < approvalStatus[i].MinimalApproval)
			{
				approvalStatus.RemoveAt(i);
				i--;
			}
			else
			{
				num = approvalStatus[i].MaximalApproval;
			}
		}
	}

	private Dictionary<StaticString, List<ApprovalStatus>> approvalStatusByClass = new Dictionary<StaticString, List<ApprovalStatus>>();

	private DepartmentOfTheInterior departmentOfTheInterior;

	private DepartmentOfPlanificationAndDevelopment departmentOfPlanificationAndDevelopment;

	private Dictionary<GameEntityGUID, ApprovalStatus> approvalStatusByGameEntity = new Dictionary<GameEntityGUID, ApprovalStatus>();

	private ApprovalStatus currentEmpireApprovalStatus;
}
