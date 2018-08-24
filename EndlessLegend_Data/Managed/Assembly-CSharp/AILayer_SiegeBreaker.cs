using System;
using System.Collections;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AILayer_SiegeBreaker : AILayerWithObjective, IXmlSerializable
{
	public AILayer_SiegeBreaker() : base("SiegeBreaker")
	{
	}

	public override void ReadXml(XmlReader reader)
	{
		base.ReadXml(reader);
	}

	public override void WriteXml(XmlWriter writer)
	{
		base.WriteXml(writer);
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		this.worldPositionningService = gameService.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		base.AIEntity.RegisterPass(AIEntity.Passes.RefreshObjectives.ToString(), "AILayer_SiegeBreaker_RefreshObjectives", new AIEntity.AIAction(this.RefreshObjectives), this, new StaticString[0]);
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public override IEnumerator Load()
	{
		yield return base.Load();
		yield break;
	}

	public override void Release()
	{
		base.Release();
		this.worldPositionningService = null;
		this.departmentOfTheInterior = null;
	}

	protected override int GetCommanderLimit()
	{
		int num = 0;
		foreach (City city in this.departmentOfTheInterior.Cities)
		{
			if (city.BesiegingEmpire != null)
			{
				num++;
			}
		}
		return Math.Max(num, 1);
	}

	protected override bool IsObjectiveValid(StaticString objectiveType, int regionIndex, bool checkLocalPriority = false)
	{
		Region region = this.worldPositionningService.GetRegion(regionIndex);
		return region != null && region.City != null && region.City.Empire == base.AIEntity.Empire && region.City.BesiegingEmpire != null;
	}

	protected override void RefreshObjectives(StaticString context, StaticString pass)
	{
		base.RefreshObjectives(context, pass);
		base.GatherObjectives(AICommanderMissionDefinition.AICommanderCategory.SiegeBreaker.ToString(), ref this.globalObjectiveMessages);
		base.ValidateMessages(ref this.globalObjectiveMessages);
		for (int i = 0; i < this.departmentOfTheInterior.Cities.Count; i++)
		{
			City city = this.departmentOfTheInterior.Cities[i];
			if (city.BesiegingEmpire != null)
			{
				if (this.IsObjectiveValid(AICommanderMissionDefinition.AICommanderCategory.SiegeBreaker.ToString(), city.Region.Index, false))
				{
					if (this.globalObjectiveMessages.Find((GlobalObjectiveMessage match) => match.RegionIndex == city.Region.Index) == null)
					{
						GlobalObjectiveMessage item = base.GenerateObjective(this.departmentOfTheInterior.Cities[i].Region.Index);
						this.globalObjectiveMessages.Add(item);
					}
				}
			}
		}
		this.ComputeObjectivePriority();
		this.globalObjectiveMessages.Sort((GlobalObjectiveMessage left, GlobalObjectiveMessage right) => -1 * left.LocalPriority.CompareTo(right.LocalPriority));
	}

	private void ComputeObjectivePriority()
	{
		base.GlobalPriority.Reset();
		base.GlobalPriority.Add(1f, "(constant) always high priority", new object[0]);
		for (int i = 0; i < this.globalObjectiveMessages.Count; i++)
		{
			HeuristicValue heuristicValue = new HeuristicValue(0.5f);
			DepartmentOfForeignAffairs agency = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
			bool flag = agency.IsInWarWithSomeone();
			if (flag)
			{
				heuristicValue.Boost(0.5f, "(constant) At war", new object[0]);
			}
			Region region = this.worldPositionningService.GetRegion(this.globalObjectiveMessages[i].RegionIndex);
			if ((float)region.City.UnitsCount < (float)region.City.MaximumUnitSlot * 0.5f)
			{
				heuristicValue.Boost(0.2f, "(constant) City defense is low", new object[0]);
			}
			this.globalObjectiveMessages[i].LocalPriority = heuristicValue;
			this.globalObjectiveMessages[i].GlobalPriority = base.GlobalPriority;
			this.globalObjectiveMessages[i].TimeOut = 1;
		}
	}

	private DepartmentOfTheInterior departmentOfTheInterior;

	private IWorldPositionningService worldPositionningService;
}
