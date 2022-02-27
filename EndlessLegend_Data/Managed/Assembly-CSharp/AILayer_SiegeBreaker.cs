using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AILayer_SiegeBreaker : AILayerWithObjective, IXmlSerializable, ITickable
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
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		this.aiDataRepositoryHelper = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		base.AIEntity.RegisterPass(AIEntity.Passes.RefreshObjectives.ToString(), "AILayer_SiegeBreaker_RefreshObjectives", new AIEntity.AIAction(this.RefreshObjectives), this, new StaticString[0]);
		ITickableRepositoryAIHelper service2 = AIScheduler.Services.GetService<ITickableRepositoryAIHelper>();
		Diagnostics.Assert(service2 != null);
		service2.Register(this);
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
		this.aiDataRepositoryHelper = null;
		ITickableRepositoryAIHelper service = AIScheduler.Services.GetService<ITickableRepositoryAIHelper>();
		Diagnostics.Assert(service != null);
		service.Unregister(this);
	}

	protected override int GetCommanderLimit()
	{
		int num = 0;
		using (IEnumerator<City> enumerator = this.departmentOfTheInterior.Cities.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.BesiegingEmpire != null)
				{
					num++;
					if ((base.AIEntity.Empire.SimulationObject.Tags.Contains("FactionTraitCultists7") || base.AIEntity.Empire.SimulationObject.Tags.Contains("FactionTraitMimics1")) && num < (int)((float)base.AIEntity.Empire.GetAgency<DepartmentOfDefense>().Armies.Count * 0.75f))
					{
						num = (int)((float)base.AIEntity.Empire.GetAgency<DepartmentOfDefense>().Armies.Count * 0.75f);
					}
				}
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
		this.SiegeBreakerArmyAssignations.Clear();
		base.RefreshObjectives(context, pass);
		base.GatherObjectives(AICommanderMissionDefinition.AICommanderCategory.SiegeBreaker.ToString(), ref this.globalObjectiveMessages);
		base.ValidateMessages(ref this.globalObjectiveMessages);
		for (int i = 0; i < this.departmentOfTheInterior.Cities.Count; i++)
		{
			City city = this.departmentOfTheInterior.Cities[i];
			if (city.BesiegingEmpire != null && this.IsObjectiveValid(AICommanderMissionDefinition.AICommanderCategory.SiegeBreaker.ToString(), city.Region.Index, false) && this.globalObjectiveMessages.Find((GlobalObjectiveMessage match) => match.RegionIndex == city.Region.Index) == null)
			{
				GlobalObjectiveMessage item = base.GenerateObjective(this.departmentOfTheInterior.Cities[i].Region.Index);
				this.globalObjectiveMessages.Add(item);
			}
		}
		for (int j = 0; j < this.departmentOfTheInterior.Cities.Count; j++)
		{
			if (this.globalObjectiveMessages.Count + 2 > base.AIEntity.Empire.GetAgency<DepartmentOfDefense>().Armies.Count)
			{
				break;
			}
			City city2 = this.departmentOfTheInterior.Cities[j];
			if (city2.BesiegingEmpire != null && this.IsObjectiveValid(AICommanderMissionDefinition.AICommanderCategory.SiegeBreaker.ToString(), city2.Region.Index, false))
			{
				int num = Intelligence.GetArmiesInRegion(city2.Region.Index).Count((Army p) => p.Empire == city2.BesiegingEmpire);
				int num2 = this.globalObjectiveMessages.Count((GlobalObjectiveMessage p) => p.RegionIndex == city2.Region.Index);
				while (num2 <= num + 2 && this.globalObjectiveMessages.Count + 2 <= base.AIEntity.Empire.GetAgency<DepartmentOfDefense>().Armies.Count)
				{
					GlobalObjectiveMessage item2 = base.GenerateObjective(this.departmentOfTheInterior.Cities[j].Region.Index);
					this.globalObjectiveMessages.Add(item2);
					num2++;
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
			if (base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>().IsInWarWithSomeone())
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

	public void UpdateSiegeBreakerArmyAssignation(Army army, City city)
	{
		if (city == null || army == null)
		{
			return;
		}
		if (!this.SiegeBreakerArmyAssignations.ContainsKey(city))
		{
			this.SiegeBreakerArmyAssignations.Add(city, new List<AILayer_SiegeBreaker.ArmySupportInfo>());
		}
		AILayer_SiegeBreaker.ArmySupportInfo armySupportInfo = this.SiegeBreakerArmyAssignations[city].FirstOrDefault((AILayer_SiegeBreaker.ArmySupportInfo asu) => asu.Army.GUID == army.GUID);
		if (armySupportInfo == null)
		{
			Predicate<AILayer_SiegeBreaker.ArmySupportInfo> <>9__1;
			foreach (KeyValuePair<City, List<AILayer_SiegeBreaker.ArmySupportInfo>> keyValuePair in this.SiegeBreakerArmyAssignations)
			{
				List<AILayer_SiegeBreaker.ArmySupportInfo> value = keyValuePair.Value;
				Predicate<AILayer_SiegeBreaker.ArmySupportInfo> match;
				if ((match = <>9__1) == null)
				{
					match = (<>9__1 = ((AILayer_SiegeBreaker.ArmySupportInfo asu) => asu.Army.GUID == army.GUID));
				}
				int num = value.FindIndex(match);
				if (num >= 0)
				{
					keyValuePair.Value.RemoveAt(num);
				}
			}
			armySupportInfo = new AILayer_SiegeBreaker.ArmySupportInfo(army);
			this.SiegeBreakerArmyAssignations[city].Add(armySupportInfo);
			this.State = TickableState.NeedTick;
		}
		armySupportInfo.Update();
	}

	public bool WaitForSupport(Army army, City city, WorldPosition TargetPosition)
	{
		if (!this.SiegeBreakerArmyAssignations.ContainsKey(city))
		{
			return false;
		}
		AILayer_SiegeBreaker.ArmySupportInfo armySupportInfo = this.SiegeBreakerArmyAssignations[city].FirstOrDefault((AILayer_SiegeBreaker.ArmySupportInfo asu) => asu.Army.GUID == army.GUID);
		if (armySupportInfo == null)
		{
			return false;
		}
		if (armySupportInfo.LastMP == -1f)
		{
			return true;
		}
		foreach (AILayer_SiegeBreaker.ArmySupportInfo armySupportInfo2 in this.SiegeBreakerArmyAssignations[city])
		{
			if (armySupportInfo2.IsValid() && armySupportInfo2.Army.GUID != army.GUID && armySupportInfo2.LastMP != armySupportInfo2.CurrentMP && this.worldPositionningService.GetDistance(armySupportInfo2.Army.WorldPosition, TargetPosition) > 3)
			{
				return true;
			}
		}
		return false;
	}

	public void Tick()
	{
		if (global::Game.Time - this.lastTickTime < 10.0)
		{
			return;
		}
		this.lastTickTime = global::Game.Time;
		this.State = TickableState.Optional;
		List<City> list = new List<City>();
		foreach (KeyValuePair<City, List<AILayer_SiegeBreaker.ArmySupportInfo>> keyValuePair in this.SiegeBreakerArmyAssignations)
		{
			if (keyValuePair.Key == null || !keyValuePair.Key.GUID.IsValid || keyValuePair.Key.BesiegingEmpireIndex < 0 || keyValuePair.Key.Empire.Index != base.AIEntity.Empire.Index || keyValuePair.Value.Count == 0)
			{
				list.Add(keyValuePair.Key);
			}
			else
			{
				for (int i = 0; i < keyValuePair.Value.Count; i++)
				{
					AILayer_SiegeBreaker.ArmySupportInfo armySupportInfo = keyValuePair.Value[i];
					AIData_Army aidata_Army;
					if (!armySupportInfo.IsValid())
					{
						keyValuePair.Value.RemoveAt(i);
						i--;
					}
					else if (!this.aiDataRepositoryHelper.TryGetAIData<AIData_Army>(armySupportInfo.Army.GUID, out aidata_Army) || aidata_Army.CommanderMission == null || aidata_Army.CommanderMission.State == TickableState.NoTick)
					{
						keyValuePair.Value.RemoveAt(i);
						i--;
					}
				}
			}
		}
		foreach (City key in list)
		{
			this.SiegeBreakerArmyAssignations.Remove(key);
		}
		if (this.SiegeBreakerArmyAssignations.Count == 0)
		{
			this.State = TickableState.NoTick;
		}
	}

	public TickableState State { get; set; }

	private DepartmentOfTheInterior departmentOfTheInterior;

	private IWorldPositionningService worldPositionningService;

	private Dictionary<City, List<AILayer_SiegeBreaker.ArmySupportInfo>> SiegeBreakerArmyAssignations = new Dictionary<City, List<AILayer_SiegeBreaker.ArmySupportInfo>>();

	private IAIDataRepositoryAIHelper aiDataRepositoryHelper;

	private double lastTickTime;

	private class ArmySupportInfo
	{
		public ArmySupportInfo(Army army)
		{
			this.Army = army;
			this.LastMP = 0f;
			this.CurrentMP = -1f;
		}

		public bool IsValid()
		{
			return this.Army != null && this.Army.GUID.IsValid && this.CurrentMP > 0.01f;
		}

		public void Update()
		{
			this.LastMP = this.CurrentMP;
			this.CurrentMP = this.Army.GetPropertyValue(SimulationProperties.Movement);
		}

		public Army Army;

		public float LastMP;

		public float CurrentMP;
	}
}
