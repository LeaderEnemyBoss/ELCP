using System;
using System.Collections.Generic;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class RaidersCommander : BaseNavyCommander
{
	public List<BaseNavyTask> NavyTasks
	{
		get
		{
			return this.navyTasks;
		}
	}

	public override void Initialize()
	{
		base.Initialize();
		IGameService service = Services.GetService<IGameService>();
		this.worldPositionService = service.Game.Services.GetService<IWorldPositionningService>();
		this.pirateCouncil = base.RegionData.WaterRegion.NavalEmpire.GetAgency<PirateCouncil>();
	}

	public virtual void ValidateTasks()
	{
		for (int i = this.navyTasks.Count - 1; i >= 0; i--)
		{
			if (!this.navyTasks[i].CheckValidity())
			{
				if (this.navyTasks[i].AssignedArmy != null)
				{
					this.NavyTasks[i].AssignedArmy.Unassign();
				}
				this.navyTasks.RemoveAt(i);
			}
		}
	}

	public override void GenerateNavyTasks()
	{
		this.ValidateTasks();
		this.GenerateTakeOverTasks(base.RegionData.WaterRegionIndex);
		this.GenerateLookAtTasks();
	}

	public U FindTask<U>(Func<U, bool> match) where U : BaseNavyTask
	{
		for (int i = 0; i < this.navyTasks.Count; i++)
		{
			U u = this.navyTasks[i] as U;
			if (u != null && (match == null || match(u)))
			{
				return u;
			}
		}
		return (U)((object)null);
	}

	protected void GenerateTakeOverTasks(int regionIndex)
	{
		if (this.pirateCouncil == null)
		{
			return;
		}
		Fortress fortress;
		foreach (Fortress fortress2 in this.pirateCouncil.GetRegionFortresses(regionIndex))
		{
			fortress = fortress2;
			if (fortress.Occupant != null)
			{
				RaidersTask_Takeover raidersTask_Takeover = this.FindTask<RaidersTask_Takeover>((RaidersTask_Takeover match) => match.TargetGuid == fortress.GUID);
				if (raidersTask_Takeover == null)
				{
					raidersTask_Takeover = new RaidersTask_Takeover();
					raidersTask_Takeover.Owner = base.Owner;
					raidersTask_Takeover.TargetGuid = fortress.GUID;
					raidersTask_Takeover.FortressPosition = fortress.WorldPosition;
					raidersTask_Takeover.ResponsibleCommander = this;
					this.NavyTasks.Add(raidersTask_Takeover);
				}
			}
		}
	}

	protected void GenerateNeighbourgTakeOverTasks()
	{
		RaidersRegionData raidersRegionData = base.RegionData as RaidersRegionData;
		if (raidersRegionData == null)
		{
			return;
		}
		if (raidersRegionData.CurrentRegionState.Name != RaiderState_Hostile.ReadonlyName)
		{
			return;
		}
		for (int i = 0; i < base.NeighbouringCommanders.Count; i++)
		{
			this.GenerateTakeOverTasks(base.NeighbouringCommanders[i].RegionData.WaterRegionIndex);
		}
	}

	private void GenerateLookAtTasks()
	{
		if (this.pirateCouncil == null)
		{
			return;
		}
		Fortress fortress;
		foreach (Fortress fortress2 in this.pirateCouncil.GetRegionFortresses(base.RegionData.WaterRegionIndex))
		{
			fortress = fortress2;
			if (!fortress.IsOccupied)
			{
				RaidersTask_LookAt raidersTask_LookAt = this.FindTask<RaidersTask_LookAt>((RaidersTask_LookAt match) => match.TargetGuid == fortress.GUID);
				if (raidersTask_LookAt == null)
				{
					raidersTask_LookAt = new RaidersTask_LookAt();
					raidersTask_LookAt.Owner = base.Owner;
					raidersTask_LookAt.TargetGuid = fortress.GUID;
					raidersTask_LookAt.ResponsibleCommander = this;
					this.NavyTasks.Add(raidersTask_LookAt);
				}
			}
		}
		for (int i = 0; i < base.RegionData.NeighbouringLandRegions.Count; i++)
		{
			Region region = base.RegionData.NeighbouringLandRegions[i];
			if (region.City != null)
			{
				District districtOnOcean = this.GetDistrictOnOcean(region.City);
				if (districtOnOcean != null)
				{
					RaidersTask_LookAt raidersTask_LookAt2 = this.FindTask<RaidersTask_LookAt>((RaidersTask_LookAt match) => match.TargetGuid == districtOnOcean.GUID);
					if (raidersTask_LookAt2 == null)
					{
						raidersTask_LookAt2 = new RaidersTask_LookAt();
						raidersTask_LookAt2.Owner = base.Owner;
						raidersTask_LookAt2.TargetGuid = districtOnOcean.GUID;
						raidersTask_LookAt2.ResponsibleCommander = this;
						this.NavyTasks.Add(raidersTask_LookAt2);
					}
				}
			}
		}
	}

	private District GetDistrictOnOcean(City city)
	{
		for (int i = 0; i < city.Districts.Count; i++)
		{
			if (this.worldPositionService.IsOceanTile(city.Districts[i].WorldPosition))
			{
				return city.Districts[i];
			}
		}
		return null;
	}

	private IWorldPositionningService worldPositionService;

	private PirateCouncil pirateCouncil;

	private List<BaseNavyTask> navyTasks = new List<BaseNavyTask>();
}
