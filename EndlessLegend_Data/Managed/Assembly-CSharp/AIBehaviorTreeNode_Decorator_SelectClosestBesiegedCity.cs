using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Decorator_SelectClosestBesiegedCity : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_SelectClosestBesiegedCity()
	{
		this.Inverted = false;
		this.Teleport = false;
	}

	[XmlAttribute]
	public bool Inverted { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) > AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		global::Empire empire = aiBehaviorTree.AICommander.Empire;
		if (empire == null)
		{
			aiBehaviorTree.ErrorCode = 10;
			return State.Failure;
		}
		if (this.Teleport)
		{
			if (army.Empire.SimulationObject.Tags.Contains("FactionTraitAffinityStrategic") && army.Empire.SimulationObject.Tags.Contains("BoosterTeleport") && !army.IsNaval && ELCPUtilities.CheckCooldownPrerequisites(army))
			{
				if (!army.StandardUnits.Any((Unit unit) => unit.SimulationObject.Tags.Contains(Unit.ReadOnlyColossus)))
				{
					goto IL_B7;
				}
			}
			return State.Failure;
		}
		IL_B7:
		City city = null;
		District district = this.worldPositionningService.GetDistrict(army.WorldPosition);
		if (district != null && district.Empire.Index == army.Empire.Index)
		{
			if (this.Inverted && district.City.BesiegingEmpire == null)
			{
				city = district.City;
				if (aiBehaviorTree.Variables.ContainsKey(this.Output_TargetVarName))
				{
					aiBehaviorTree.Variables[this.Output_TargetVarName] = city;
				}
				else
				{
					aiBehaviorTree.Variables.Add(this.Output_TargetVarName, city);
				}
				return State.Success;
			}
			if (district.City.BesiegingEmpire != null)
			{
				if (this.Teleport || this.Inverted)
				{
					return State.Failure;
				}
				city = district.City;
				if (aiBehaviorTree.Variables.ContainsKey(this.Output_TargetVarName))
				{
					aiBehaviorTree.Variables[this.Output_TargetVarName] = city;
				}
				else
				{
					aiBehaviorTree.Variables.Add(this.Output_TargetVarName, city);
				}
				return State.Success;
			}
		}
		DepartmentOfTheInterior agency = empire.GetAgency<DepartmentOfTheInterior>();
		if (agency == null)
		{
			aiBehaviorTree.ErrorCode = 10;
			return State.Failure;
		}
		int num = int.MaxValue;
		List<City> list = new List<City>();
		using (IEnumerator<City> enumerator = agency.Cities.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				City city2 = enumerator.Current;
				if (city2.BesiegingEmpire != null)
				{
					if (!this.Inverted && this.worldPositionningService.GetDistance(army.WorldPosition, city2.WorldPosition) < num && (!this.Teleport || ELCPUtilities.CanTeleportToCity(city2, army, this.worldPositionningService.GetRegion(army.WorldPosition), this.worldPositionningService, null)))
					{
						if (this.Teleport && this.encounterRepositoryService.Any((Encounter encounter) => encounter.IsGarrisonInEncounter(city2.GUID, false)))
						{
							list.Add(city2);
						}
						else
						{
							num = this.worldPositionningService.GetDistance(army.WorldPosition, city2.WorldPosition);
							city = city2;
						}
					}
				}
				else if (this.Inverted && this.worldPositionningService.GetDistance(army.WorldPosition, city2.WorldPosition) < num)
				{
					num = this.worldPositionningService.GetDistance(army.WorldPosition, city2.WorldPosition);
					city = city2;
				}
			}
		}
		if (city == null && list.Count > 0)
		{
			city = list[0];
		}
		if (city != null)
		{
			if (aiBehaviorTree.Variables.ContainsKey(this.Output_TargetVarName))
			{
				aiBehaviorTree.Variables[this.Output_TargetVarName] = city;
			}
			else
			{
				aiBehaviorTree.Variables.Add(this.Output_TargetVarName, city);
			}
			return State.Success;
		}
		return State.Failure;
	}

	[XmlAttribute]
	public bool Teleport { get; set; }

	[XmlAttribute]
	public string Output_TargetVarName { get; set; }

	public override bool Initialize(BehaviourTree behaviourTree)
	{
		IGameService service = Services.GetService<IGameService>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		this.encounterRepositoryService = service.Game.Services.GetService<IEncounterRepositoryService>();
		return base.Initialize(behaviourTree);
	}

	public override void Release()
	{
		base.Release();
		this.worldPositionningService = null;
		this.encounterRepositoryService = null;
	}

	private IWorldPositionningService worldPositionningService;

	private IEncounterRepositoryService encounterRepositoryService;
}
