using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

[Diagnostics.TagAttribute("AI")]
public class AILayer_Terraformation : AILayerWithObjective
{
	public AILayer_Terraformation() : base("Terraformation")
	{
		this.regions = new List<Region>();
		this.random = new Random();
		this.opportunityPositions = new Dictionary<int, WorldPosition>();
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.terraformDeviceService = service.Game.Services.GetService<ITerraformDeviceService>();
		this.terraformDeviceRepositoryService = service.Game.Services.GetService<ITerraformDeviceRepositoryService>();
		this.terraformDeviceRepositoryService.TerraformDeviceRepositoryChange += this.OnDevicePlaced;
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		this.aiLayerStrategy = base.AIEntity.GetLayer<AILayer_Strategy>();
		base.AIEntity.RegisterPass(AIEntity.Passes.RefreshObjectives.ToString(), "AILayer_Terraformation_RefreshObjectives", new AIEntity.AIAction(this.RefreshObjectives), this, new StaticString[0]);
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI && DepartmentOfTheInterior.CanInvokeTerraformationDevices(base.AIEntity.Empire);
	}

	protected override void RefreshObjectives(StaticString context, StaticString pass)
	{
		base.RefreshObjectives(context, pass);
		this.FillUpRegions();
		this.opportunityPositions.Clear();
		foreach (Region region in this.regions)
		{
			WorldPosition worldPosition = this.SelectPositionToTerraform(region);
			if (worldPosition.IsValid)
			{
				if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
				{
					Diagnostics.Log("ELCP {0} AILayer_Terraformation adding opportunity position in {1} at {2}", new object[]
					{
						base.AIEntity.Empire,
						region.LocalizedName,
						worldPosition
					});
				}
				this.opportunityPositions.Add(region.Index, worldPosition);
			}
		}
		this.ComputeGlobalPriority();
		base.GatherObjectives(base.ObjectiveType, false, ref this.globalObjectiveMessages);
		base.ValidateMessages(ref this.globalObjectiveMessages);
		for (int i = 0; i < this.globalObjectiveMessages.Count; i++)
		{
			GlobalObjectiveMessage globalObjectiveMessage = this.globalObjectiveMessages[i];
			this.RefreshMessagePriority(globalObjectiveMessage);
			this.regions.Remove(this.worldPositionningService.GetRegion(globalObjectiveMessage.RegionIndex));
		}
		int num = (int)base.AIEntity.Empire.GetPropertyValue(SimulationProperties.LavapoolStock);
		for (int j = 0; j < this.globalObjectiveMessages.Count; j++)
		{
			num--;
		}
		if (num > 0 && this.regions.Count > 0)
		{
			for (int k = num; k > 0; k--)
			{
				if (this.globalObjectiveMessages.Count < 5)
				{
					int nextTerraformationRegionIndex = this.GetNextTerraformationRegionIndex();
					if (nextTerraformationRegionIndex != -1)
					{
						this.CreateObjectiveFor(nextTerraformationRegionIndex);
					}
				}
			}
		}
	}

	private void CreateObjectiveFor(int regionIndex)
	{
		GlobalObjectiveMessage globalObjectiveMessage = base.GenerateObjective(regionIndex);
		this.globalObjectiveMessages.Add(globalObjectiveMessage);
		this.RefreshMessagePriority(globalObjectiveMessage);
	}

	private void RefreshMessagePriority(GlobalObjectiveMessage objectiveMessage)
	{
		objectiveMessage.GlobalPriority = base.GlobalPriority;
		objectiveMessage.LocalPriority = new HeuristicValue(0.6f);
		objectiveMessage.TimeOut = 1;
	}

	protected override bool IsObjectiveValid(GlobalObjectiveMessage objective)
	{
		if (objective.ObjectiveType == base.ObjectiveType)
		{
			Region region = this.worldPositionningService.GetRegion(objective.RegionIndex);
			return region.City != null && region.City.Empire.Index == base.AIEntity.Empire.Index && this.GetTerrainToTerraformInRegion(region) != 0 && objective.State != BlackboardMessage.StateValue.Message_Canceled;
		}
		return false;
	}

	protected override bool IsObjectiveValid(StaticString objectiveType, int regionIndex, bool checkLocalPriority = false)
	{
		return true;
	}

	public override void Release()
	{
		base.Release();
		this.worldPositionningService = null;
		this.departmentOfTheInterior = null;
		this.aiLayerStrategy = null;
		this.terraformDeviceService = null;
		if (this.terraformDeviceRepositoryService != null)
		{
			this.terraformDeviceRepositoryService.TerraformDeviceRepositoryChange -= this.OnDevicePlaced;
			this.terraformDeviceRepositoryService = null;
		}
		this.opportunityPositions.Clear();
	}

	private void FillUpRegions()
	{
		this.regions.Clear();
		for (int i = 0; i < this.departmentOfTheInterior.Cities.Count; i++)
		{
			Region region = this.departmentOfTheInterior.Cities[i].Region;
			this.AddRegion(region);
		}
	}

	private void AddRegion(Region region)
	{
		if (region == null)
		{
			return;
		}
		if (region.City != null && region.City.Empire.Index != base.AIEntity.Empire.Index)
		{
			return;
		}
		if (!this.regions.Contains(region))
		{
			this.regions.Add(region);
		}
	}

	private int GetNextTerraformationRegionIndex()
	{
		if (this.regions.Count <= 0)
		{
			return -1;
		}
		int num = -1;
		int num2 = 0;
		for (int i = 0; i < this.regions.Count; i++)
		{
			int terrainToTerraformInRegion = this.GetTerrainToTerraformInRegion(this.regions[i]);
			if (terrainToTerraformInRegion > num2)
			{
				num2 = terrainToTerraformInRegion;
				num = i;
			}
		}
		if (num >= 0)
		{
			int index = this.regions[num].Index;
			this.regions.RemoveAt(num);
			return index;
		}
		return -1;
	}

	private int GetVolcanicTerrainInRegion(Region region)
	{
		int num = 0;
		for (int i = 0; i < region.City.Districts.Count; i++)
		{
			if (this.worldPositionningService.ContainsTerrainTag(region.City.Districts[i].WorldPosition, "TerrainTagVolcanic") || this.worldPositionningService.IsWaterTile(region.City.Districts[i].WorldPosition))
			{
				num++;
			}
		}
		return num;
	}

	protected override int GetCommanderLimit()
	{
		return 5;
	}

	private void ComputeGlobalPriority()
	{
		base.GlobalPriority.Reset();
		if (this.departmentOfTheInterior.Cities.Count == 0)
		{
			base.GlobalPriority.Subtract(1f, "We should focus on getting a city!", new object[0]);
			return;
		}
		AILayer_Strategy layer = base.AIEntity.GetLayer<AILayer_Strategy>();
		base.GlobalPriority.Add(layer.StrategicNetwork.GetAgentValue("Expansion"), "Expansion strategic network score", new object[0]);
		if (this.aiLayerStrategy.IsAtWar())
		{
			base.GlobalPriority.Boost(-0.4f, "At war", new object[0]);
			return;
		}
		if (this.aiLayerStrategy.WantWarWithSomeone())
		{
			base.GlobalPriority.Boost(-0.2f, "Want war but not at war", new object[0]);
			return;
		}
		if (this.departmentOfTheInterior.Cities.Count >= 1)
		{
			base.GlobalPriority.Boost(0.4f, "I am sure you want to improve your cities", new object[0]);
		}
	}

	private int GetTerrainToTerraformInRegion(Region region)
	{
		int num = 0;
		for (int i = 0; i < region.City.Districts.Count; i++)
		{
			if (!this.worldPositionningService.ContainsTerrainTag(region.City.Districts[i].WorldPosition, "TerrainTagVolcanic") && !this.worldPositionningService.IsWaterTile(region.City.Districts[i].WorldPosition) && !this.terraformDeviceService.IsPositionNextToDevice(region.City.Districts[i].WorldPosition))
			{
				num++;
			}
		}
		return num;
	}

	private WorldPosition SelectPositionToTerraform(Region region)
	{
		City city = region.City;
		if (city != null)
		{
			if (this.IsPositionValidToTerraform(city.WorldPosition))
			{
				return city.WorldPosition;
			}
			if (city.Camp != null && this.IsPositionValidToTerraform(city.Camp.WorldPosition))
			{
				return city.Camp.WorldPosition;
			}
			int num = -1;
			int num2 = 0;
			for (int i = 0; i < city.Districts.Count; i++)
			{
				if (city.Districts[i] != null && this.IsPositionValidToTerraform(city.Districts[i].WorldPosition))
				{
					int num3 = 0;
					foreach (WorldPosition worldPosition in city.Districts[i].WorldPosition.GetNeighbours(this.worldPositionningService.World.WorldParameters))
					{
						if (this.worldPositionningService.GetDistrict(worldPosition) != null && !this.worldPositionningService.ContainsTerrainTag(worldPosition, "TerrainTagVolcanic") && !this.terraformDeviceService.IsPositionNextToDevice(worldPosition))
						{
							num3++;
						}
					}
					if (num3 > num2)
					{
						num2 = num3;
						num = i;
					}
				}
			}
			if (num >= 0)
			{
				return city.Districts[num].WorldPosition;
			}
		}
		return WorldPosition.Invalid;
	}

	private bool IsPositionValidToTerraform(WorldPosition position)
	{
		return !this.worldPositionningService.ContainsTerrainTag(position, "TerrainTagVolcanic") && this.terraformDeviceService.IsPositionValidForDevice(base.AIEntity.Empire, position) && !this.terraformDeviceService.IsPositionNextToDevice(position);
	}

	public Dictionary<int, WorldPosition> OpportunityPositions
	{
		get
		{
			return this.opportunityPositions;
		}
	}

	public void OnDevicePlaced(object sender, TerraformDeviceRepositoryChangeEventArgs eventArgs)
	{
		if (!this.IsActive())
		{
			return;
		}
		if (eventArgs.Action != TerraformDeviceRepositoryChangeAction.Add)
		{
			return;
		}
		TerraformDevice terraformDevice;
		this.terraformDeviceRepositoryService.TryGetValue(eventArgs.PillarGUID, out terraformDevice);
		Region region = this.worldPositionningService.GetRegion(terraformDevice.WorldPosition);
		if (this.opportunityPositions.ContainsKey(region.Index))
		{
			this.opportunityPositions.Remove(region.Index);
			WorldPosition worldPosition = this.SelectPositionToTerraform(region);
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				Diagnostics.Log("ELCP {0} AILayer_Terraformation registered placed terraform device in {1} at {2}, new position: {3}", new object[]
				{
					base.AIEntity.Empire,
					region.LocalizedName,
					terraformDevice.WorldPosition,
					worldPosition
				});
			}
			if (worldPosition.IsValid)
			{
				this.opportunityPositions.Add(region.Index, worldPosition);
			}
		}
	}

	private const int commanderLimit = 5;

	private AILayer_Strategy aiLayerStrategy;

	private IWorldPositionningService worldPositionningService;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private List<Region> regions;

	private Random random;

	private ITerraformDeviceService terraformDeviceService;

	private Dictionary<int, WorldPosition> opportunityPositions;

	private ITerraformDeviceRepositoryService terraformDeviceRepositoryService;
}
