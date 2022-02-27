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
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		this.worldPositionningService = gameService.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
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
		this.ComputeGlobalPriority();
		base.GatherObjectives(base.ObjectiveType, false, ref this.globalObjectiveMessages);
		base.ValidateMessages(ref this.globalObjectiveMessages);
		for (int i = 0; i < this.globalObjectiveMessages.Count; i++)
		{
			GlobalObjectiveMessage globalObjectiveMessage = this.globalObjectiveMessages[i];
			if (this.IsObjectiveValid(globalObjectiveMessage))
			{
				this.RefreshMessagePriority(globalObjectiveMessage);
			}
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
			return (region.City == null || region.City.Empire.Index == base.AIEntity.Empire.Index) && objective.State != BlackboardMessage.StateValue.Message_Canceled;
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
		int index = 0;
		int num = this.GetVolcanicTerrainInRegion(this.regions[0]);
		for (int i = 1; i < this.regions.Count; i++)
		{
			int volcanicTerrainInRegion = this.GetVolcanicTerrainInRegion(this.regions[i]);
			if (volcanicTerrainInRegion < num)
			{
				num = volcanicTerrainInRegion;
				index = i;
			}
		}
		if (this.regions[index].WorldPositions.Length == num)
		{
			return -1;
		}
		return this.regions[index].Index;
	}

	private int GetVolcanicTerrainInRegion(Region region)
	{
		int num = 0;
		WorldPosition[] worldPositions = region.WorldPositions;
		for (int i = 0; i < worldPositions.Length; i++)
		{
			if (this.worldPositionningService.ContainsTerrainTag(worldPositions[i], "TerrainTagVolcanic") || this.worldPositionningService.IsWaterTile(worldPositions[i]))
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
		}
		else
		{
			AILayer_Strategy layer = base.AIEntity.GetLayer<AILayer_Strategy>();
			base.GlobalPriority.Add(layer.StrategicNetwork.GetAgentValue("Expansion"), "Expansion strategic network score", new object[0]);
			if (this.aiLayerStrategy.IsAtWar())
			{
				base.GlobalPriority.Boost(-0.4f, "At war", new object[0]);
			}
			else if (this.aiLayerStrategy.WantWarWithSomeone())
			{
				base.GlobalPriority.Boost(-0.2f, "Want war but not at war", new object[0]);
			}
			else if (this.departmentOfTheInterior.Cities.Count >= 1)
			{
				base.GlobalPriority.Boost(0.4f, "I am sure you want to improve your cities", new object[0]);
			}
		}
	}

	private const int commanderLimit = 5;

	private AILayer_Strategy aiLayerStrategy;

	private IWorldPositionningService worldPositionningService;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private List<Region> regions = new List<Region>();

	private Random random = new Random();
}
