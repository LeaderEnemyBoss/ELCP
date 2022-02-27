using System;
using System.Collections.Generic;
using Amplitude.Unity.AI;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class MantaZone
{
	public MantaZone(global::Empire owner)
	{
		this.owner = owner;
		this.Ruins = new List<PointOfInterest>();
		this.Regions = new List<AIRegionData>();
		this.Resources = new List<PointOfInterest>();
		this.Orbs = new List<OrbSpawnInfo>();
		this.departmentOfScience = this.owner.GetAgency<DepartmentOfScience>();
		this.departmentOfForeignAffairs = this.owner.GetAgency<DepartmentOfForeignAffairs>();
	}

	public float MantaZoneScore { get; set; }

	public List<AIRegionData> Regions { get; set; }

	public List<PointOfInterest> Ruins { get; set; }

	public List<PointOfInterest> Resources { get; set; }

	public List<OrbSpawnInfo> Orbs { get; set; }

	public AIData_Army Manta { get; private set; }

	public float CurrentMantaScore { get; set; }

	public AIHeuristicAnalyser.Context DebugContext { get; set; }

	public IEnumerable<IWorldPositionable> GetZoneTargets()
	{
		for (int index = 0; index < this.Ruins.Count; index++)
		{
			yield return this.Ruins[index];
		}
		for (int index2 = 0; index2 < this.Resources.Count; index2++)
		{
			yield return this.Ruins[index2];
		}
		for (int index3 = 0; index3 < this.Orbs.Count; index3++)
		{
			yield return this.Orbs[index3];
		}
		yield break;
	}

	public void UpdateScore()
	{
		this.UpdateLists();
		this.DebugContext = null;
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			this.DebugContext = new AIHeuristicAnalyser.Context();
			string empty = string.Empty;
			this.PrintRegions(ref empty);
		}
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < this.Orbs.Count; i++)
		{
			num2 += this.Orbs[i].CurrentOrbCount;
		}
		float num3 = 0.05f;
		float num4 = Mathf.Min(1f, num3 * num2);
		float num5 = 0.5f;
		num4 *= num5;
		num = AILayer.Boost(num, num4);
		float num6 = (float)this.Ruins.Count;
		float num7 = 0.1f;
		float num8 = Mathf.Min(1f, num7 * num6);
		float num9 = 0.3f;
		num8 *= num9;
		num = AILayer.Boost(num, num8);
		this.MantaZoneScore = num;
	}

	public void AddRegion(AIRegionData regionData)
	{
		this.UpdateLists(regionData);
		this.Regions.Add(regionData);
	}

	public void RemoveRegion(AIRegionData regionData)
	{
		for (int i = 0; i < regionData.Region.PointOfInterests.Length; i++)
		{
			PointOfInterest pointOfInterest = regionData.Region.PointOfInterests[i];
			if (pointOfInterest.Type == "ResourceDeposit")
			{
				this.Resources.Remove(pointOfInterest);
			}
			else if (pointOfInterest.Type == "QuestLocation")
			{
				this.Ruins.Remove(pointOfInterest);
			}
		}
		IGameService service = Services.GetService<IGameService>();
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		IOrbAIHelper service3 = AIScheduler.Services.GetService<IOrbAIHelper>();
		for (int j = 0; j < service3.OrbSpawns.Count; j++)
		{
			int regionIndex = (int)service2.GetRegionIndex(service3.OrbSpawns[j].WorldPosition);
			if (regionIndex == regionData.RegionIndex)
			{
				this.Orbs.Remove(service3.OrbSpawns[j]);
			}
		}
		this.Regions.Remove(regionData);
	}

	public void AssignArmy(AIData_Army armyData)
	{
		this.Manta = armyData;
	}

	public void PrintRegions(ref string data)
	{
		for (int i = 0; i < this.Regions.Count; i++)
		{
			data += string.Format("{0} ({1})", this.Regions[i].Region.LocalizedName, this.Regions[i].RegionIndex);
			if (i < this.Regions.Count - 1)
			{
				data += " ";
			}
		}
	}

	private void UpdateLists()
	{
		this.Orbs.Clear();
		this.Resources.Clear();
		this.Ruins.Clear();
		for (int i = 0; i < this.Regions.Count; i++)
		{
			this.UpdateLists(this.Regions[i]);
		}
	}

	private void UpdateLists(AIRegionData regionData)
	{
		for (int i = 0; i < regionData.Region.PointOfInterests.Length; i++)
		{
			PointOfInterest pointOfInterest = regionData.Region.PointOfInterests[i];
			if (pointOfInterest.Type == "ResourceDeposit")
			{
				if (pointOfInterest.PointOfInterestImprovement == null)
				{
					string technologyName;
					if (pointOfInterest.PointOfInterestDefinition.TryGetValue("VisibilityTechnology", out technologyName))
					{
						if (this.departmentOfScience.GetTechnologyState(technologyName) == DepartmentOfScience.ConstructibleElement.State.Researched && this.CanStopThere(pointOfInterest.WorldPosition))
						{
							this.Resources.Add(pointOfInterest);
						}
					}
				}
			}
			else if (pointOfInterest.Type == "QuestLocation")
			{
				if ((pointOfInterest.Interaction.Bits & this.owner.Bits) != this.owner.Bits)
				{
					if (this.CanStopThere(pointOfInterest.WorldPosition))
					{
						this.Ruins.Add(pointOfInterest);
					}
				}
			}
		}
		IGameService service = Services.GetService<IGameService>();
		IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
		IOrbAIHelper service3 = AIScheduler.Services.GetService<IOrbAIHelper>();
		for (int j = 0; j < service3.OrbSpawns.Count; j++)
		{
			if (service3.OrbSpawns[j].CurrentOrbCount != 0f)
			{
				int regionIndex = (int)service2.GetRegionIndex(service3.OrbSpawns[j].WorldPosition);
				if (regionIndex == regionData.RegionIndex && this.CanStopThere(service3.OrbSpawns[j].WorldPosition))
				{
					this.Orbs.Add(service3.OrbSpawns[j]);
				}
			}
		}
	}

	private bool CanStopThere(WorldPosition worldPosition)
	{
		return this.departmentOfForeignAffairs == null || this.departmentOfForeignAffairs.CanMoveOn(worldPosition, false, false);
	}

	private global::Empire owner;

	private DepartmentOfScience departmentOfScience;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;
}
