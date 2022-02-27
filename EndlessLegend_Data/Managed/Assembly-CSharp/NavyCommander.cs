using System;
using System.Collections.Generic;
using System.Linq;

public class NavyCommander : BaseNavyCommander
{
	public NavyCommander(AILayer_Navy navyLayer)
	{
		this.NavyArmies = new List<NavyArmy>();
		this.NavyFortresses = new List<NavyFortress>();
		this.navyLayer = navyLayer;
	}

	public NavyRegionData NavyRegionData
	{
		get
		{
			return base.RegionData as NavyRegionData;
		}
	}

	public int NumberOfMediumSizedArmies
	{
		get
		{
			int num = this.NavyArmies.Count((NavyArmy match) => match.Role == BaseNavyArmy.ArmyRole.TaskForce);
			int num2 = this.NavyFortresses.Count((NavyFortress match) => match.ArmySize >= BaseNavyArmy.ArmyState.High);
			return num + num2;
		}
	}

	public List<NavyArmy> NavyArmies { get; set; }

	public List<NavyFortress> NavyFortresses { get; set; }

	public NavyCommander.NavyCommanderState CommanderState { get; set; }

	public float CommanderArmyNeed()
	{
		switch (this.CommanderState)
		{
		case NavyCommander.NavyCommanderState.BuildUp:
			return 0.5f;
		case NavyCommander.NavyCommanderState.Defense:
			if (this.NavyRegionData.EnemyNavalPower == 0f && this.NavyRegionData.NumberOfEnemyCityOnTheBorder == 0)
			{
				return 0.4f;
			}
			if (this.WantedNumberOfArmies() > (float)this.NumberOfMediumSizedArmies)
			{
				return 0.6f;
			}
			return 0.4f;
		case NavyCommander.NavyCommanderState.Harrass:
			return 0.4f;
		case NavyCommander.NavyCommanderState.Takeover:
			return 0.5f;
		default:
			return 0.3f;
		}
	}

	public float WantedNumberOfArmies()
	{
		switch (this.CommanderState)
		{
		case NavyCommander.NavyCommanderState.BuildUp:
			return 1f;
		case NavyCommander.NavyCommanderState.Defense:
			if (this.NavyRegionData.EnemyNavalPower == 0f && this.NavyRegionData.NumberOfEnemyCityOnTheBorder == 0)
			{
				return 0.5f;
			}
			return 1.5f;
		case NavyCommander.NavyCommanderState.Harrass:
			return 1f;
		case NavyCommander.NavyCommanderState.Takeover:
			return 1.5f;
		default:
			return 0f;
		}
	}

	public override void GenerateNavyTasks()
	{
		if (this.CommanderState == NavyCommander.NavyCommanderState.Inactive || this.CommanderState == NavyCommander.NavyCommanderState.BuildUp)
		{
			return;
		}
		this.GenerateFortressTasks();
		this.GenerateCityTasks();
	}

	public override void Initialize()
	{
		base.Initialize();
		this.aiDataRepositoryHelper = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
	}

	public void GenerateFillFortress(Fortress fortress)
	{
		NavyTask_FillFortress navyTask_FillFortress = this.navyLayer.FindTask<NavyTask_FillFortress>((NavyTask_FillFortress match) => match.TargetGuid == fortress.GUID);
		if (navyTask_FillFortress != null)
		{
			return;
		}
		NavyFortress navyFortress = this.NavyFortresses.Find((NavyFortress match) => match.Fortress.GUID == fortress.GUID);
		if (navyFortress == null)
		{
			return;
		}
		navyTask_FillFortress = new NavyTask_FillFortress(this.navyLayer, navyFortress);
		this.navyLayer.NavyTasks.Add(navyTask_FillFortress);
	}

	protected virtual void GenerateFortressTasks()
	{
		if (this.CommanderState == NavyCommander.NavyCommanderState.Inactive)
		{
			return;
		}
		PirateCouncil agency = this.NavyRegionData.WaterRegion.NavalEmpire.GetAgency<PirateCouncil>();
		if (agency != null)
		{
			foreach (Fortress fortress in agency.GetRegionFortresses(base.RegionData.WaterRegionIndex))
			{
				if (fortress.Occupant != null && fortress.Occupant.Index == base.Owner.Index)
				{
					this.GenerateFillFortress(fortress);
				}
				else
				{
					this.GenerateTakeOver(fortress);
				}
			}
		}
	}

	protected virtual void GenerateCityTasks()
	{
		if (this.CommanderState == NavyCommander.NavyCommanderState.Inactive)
		{
			return;
		}
		for (int i = 0; i < this.NavyRegionData.NeighbouringLandRegions.Count; i++)
		{
			if (this.NavyRegionData.NeighbouringLandRegions[i].City != null)
			{
				AIData_City aidata_City = null;
				if (this.aiDataRepositoryHelper.TryGetAIData<AIData_City>(this.NavyRegionData.NeighbouringLandRegions[i].City.GUID, out aidata_City) && aidata_City.NeighbourgRegions.Contains(this.NavyRegionData.WaterRegionIndex) && aidata_City.City.Empire != base.Owner && this.navyLayer.MightAttackOwner(aidata_City.City.Region, aidata_City.City.Empire) && AILayer_War.IsWarTarget(this.navyLayer.AIEntity, aidata_City.City, 0f))
				{
					this.GenerateCityBlitzTask(aidata_City.City);
				}
			}
		}
	}

	private void GenerateTakeOver(Fortress fortress)
	{
		if (fortress.Occupant != null && !this.navyLayer.MightAttackOwner(fortress.Region, fortress.Occupant))
		{
			return;
		}
		if (fortress.Occupant != null && base.Owner is MajorEmpire && this.navyLayer.diplomacyLayer.GetPeaceWish(fortress.Occupant.Index))
		{
			return;
		}
		NavyTask_Takeover navyTask_Takeover = this.navyLayer.FindTask<NavyTask_Takeover>((NavyTask_Takeover match) => match.TargetGuid == fortress.GUID);
		if (navyTask_Takeover != null)
		{
			return;
		}
		navyTask_Takeover = new NavyTask_Takeover(this.navyLayer, fortress);
		this.navyLayer.NavyTasks.Add(navyTask_Takeover);
	}

	private void GenerateCityBlitzTask(City city)
	{
		NavyTasks_Blitz navyTasks_Blitz = this.navyLayer.FindTask<NavyTasks_Blitz>((NavyTasks_Blitz match) => match.TargetGuid == city.GUID);
		if (navyTasks_Blitz != null)
		{
			return;
		}
		navyTasks_Blitz = new NavyTasks_Blitz(this.navyLayer, city);
		this.navyLayer.NavyTasks.Add(navyTasks_Blitz);
	}

	private IAIDataRepositoryAIHelper aiDataRepositoryHelper;

	private AILayer_Navy navyLayer;

	public enum NavyCommanderState
	{
		Inactive,
		BuildUp,
		Defense,
		Harrass,
		Takeover
	}
}
