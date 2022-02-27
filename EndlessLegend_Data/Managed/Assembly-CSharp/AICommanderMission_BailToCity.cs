using System;
using System.Collections.Generic;
using Amplitude;

public class AICommanderMission_BailToCity : AICommanderMissionWithRequestArmy
{
	public City TargetCity { get; set; }

	public override void Initialize(AICommander aiCommander)
	{
		base.Initialize(aiCommander);
	}

	public override void Release()
	{
		base.Release();
		this.TargetCity = null;
		this.ticket = null;
	}

	public override void SetParameters(AICommanderMissionDefinition missionDefinition, params object[] parameters)
	{
		base.SetParameters(missionDefinition, parameters);
		if (parameters.Length != 1)
		{
			Diagnostics.LogError("[AICommanderMission_BailToCity] Wrong number of parameters {0}", new object[]
			{
				parameters.Length
			});
		}
		this.TargetCity = (parameters[0] as City);
	}

	protected override AICommanderMission.AICommanderMissionCompletion GetCompletionFor(AIArmyMission.AIArmyMissionErrorCode errorCode, out TickableState tickableState)
	{
		return base.GetCompletionFor(errorCode, out tickableState);
	}

	protected override AICommanderMission.AICommanderMissionCompletion GetCompletionWhenSuccess(AIData_Army aiArmyData, out TickableState tickableState)
	{
		tickableState = TickableState.Optional;
		return AICommanderMission.AICommanderMissionCompletion.Success;
	}

	protected override void GetNeededArmyPower(out float minMilitaryPower, out bool isMaxPower, out bool perUnitTest)
	{
		isMaxPower = false;
		perUnitTest = false;
		minMilitaryPower = 1f;
	}

	protected override int GetNeededAvailabilityTime()
	{
		return 5;
	}

	protected override bool IsMissionCompleted()
	{
		AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(base.AIDataArmyGUID);
		return aidata == null && aidata.Army.IsSettler;
	}

	protected override bool MissionCanAcceptHero()
	{
		return false;
	}

	protected override void Running()
	{
		if (this.IsMissionCompleted())
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Success;
			return;
		}
		base.Running();
	}

	protected override void Success()
	{
		base.Success();
	}

	protected override bool TryComputeArmyMissionParameter()
	{
		if (this.ticket != null)
		{
			if (this.ticket.Raised)
			{
				this.ticket = null;
			}
			return false;
		}
		if (base.AIDataArmyGUID.IsValid && this.TargetCity != null && this.TargetCity.Empire == base.Commander.Empire && this.aiDataRepository.GetAIData<AIData_City>(this.TargetCity.GUID) != null)
		{
			AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(base.AIDataArmyGUID);
			if (aidata != null)
			{
				List<object> list = new List<object>();
				list.Add(this.TargetCity);
				if (this.TargetCity.MaximumUnitSlot > this.TargetCity.CurrentUnitSlot + aidata.Army.CurrentUnitSlot)
				{
					return base.TryCreateArmyMission("DefendCity_Bail", list);
				}
				if (aidata.Army.CurrentUnitSlot > 1)
				{
					GameEntityGUID[] array = new GameEntityGUID[1];
					for (int i = 0; i < aidata.Army.StandardUnits.Count; i++)
					{
						if (aidata.Army.StandardUnits[i].IsSettler && aidata.Army.StandardUnits[i].GetPropertyValue(SimulationProperties.Movement) > 0f)
						{
							array[0] = aidata.Army.StandardUnits[i].GUID;
							break;
						}
					}
					WorldPosition neighbourFirstAvailablePositionForArmyCreation = DepartmentOfDefense.GetNeighbourFirstAvailablePositionForArmyCreation(aidata.Army);
					if (neighbourFirstAvailablePositionForArmyCreation.IsValid && array[0].IsValid)
					{
						OrderTransferGarrisonToNewArmy order = new OrderTransferGarrisonToNewArmy(base.Commander.Empire.Index, base.AIDataArmyGUID, array, neighbourFirstAvailablePositionForArmyCreation, null, false, true, true);
						base.Commander.Empire.PlayerControllers.AI.PostOrder(order, out this.ticket, null);
					}
					return false;
				}
				return base.TryCreateArmyMission("ReachTarget", list);
			}
		}
		Diagnostics.Assert(AIScheduler.Services != null);
		IIntelligenceAIHelper service = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		Diagnostics.Assert(service != null);
		List<Region> list2 = new List<Region>();
		if (service.TryGetListOfRegionToExplore(base.Commander.Empire, 0.95f, ref list2))
		{
			foreach (Region region in list2)
			{
				if (AILayer_Exploration.IsRegionValidForExploration(base.Commander.Empire, region))
				{
					return base.TryCreateArmyMission("ExploreAt", new List<object>
					{
						region.Index
					});
				}
			}
			return false;
		}
		return false;
	}

	private Ticket ticket;
}
